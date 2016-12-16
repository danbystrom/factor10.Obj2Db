using System;
using System.Collections.Generic;
using System.Linq;

namespace factor10.Obj2Db
{
    public enum TypeOfEntity
    {
        Class,
        PlainField,
        IEnumerable,
        Formula
    }


    public class Entity
    {
        public readonly string Name;
        public readonly string ExternalName;
        public readonly string TypeName;
        public LinkedFieldInfo FieldInfo { get; }
        public TypeOfEntity TypeOfEntity { get; private set; }

        public readonly List<Entity> Fields = new List<Entity>();
        public readonly int SaveableFieldCount;
        private List<Entity> _lists = new List<Entity>();

        public readonly bool NoSave;

        public ITable Table;

        public string Aggregation;
        public List<Tuple<int, int>> TemporaryAggregationMapper = new List<Tuple<int, int>>();

        private Entity(string name, LinkedFieldInfo fieldInfo)
        {
            Name = name;
            ExternalName =name?.Replace(".", "");
            FieldInfo = fieldInfo;
        }

        public Entity(Type type, EntitySpec entitySpec)
        {
            Name = entitySpec.Name;
            ExternalName = entitySpec.ExternalName ?? Name?.Replace(".", "");
            NoSave = entitySpec.NoSave;
            Aggregation = entitySpec.Aggregation;

            if (Name != null)
            {
                FieldInfo = new LinkedFieldInfo(type, Name);
                if (FieldInfo.IEnumerable != null)
                {
                    TypeOfEntity = TypeOfEntity.IEnumerable;
                    type = FieldInfo.IEnumerable.GetGenericArguments()[0];
                    if (entitySpec.IsField)
                        Fields.Add(new Entity(null, LinkedFieldInfo.Null(type)));
                }
                else if (entitySpec.IsField)
                    TypeOfEntity = TypeOfEntity.PlainField;
                else
                    TypeOfEntity = TypeOfEntity.Class;
            }

            TypeName = type.Name;

            foreach (var subEntitySpec in entitySpec.Fields)
            {
                subEntitySpec.NoSave |= NoSave; // propagate NoSave all the way down until we reach turtles
                var subEntity = new Entity(type, subEntitySpec);
                (subEntity.TypeOfEntity == TypeOfEntity.IEnumerable ? _lists : Fields).Add(subEntity);
            }

            // pull aggregations one level up
            foreach (var subEntity in _lists)
                for (var i = 0; i < subEntity.Fields.Count; i++)
                {
                    var subField = subEntity.Fields[i];
                    if (subField.Aggregation != null)
                    {
                        TemporaryAggregationMapper.Add(Tuple.Create(i, Fields.Count));
                        Fields.Add(new Entity(subField.Name, LinkedFieldInfo.Null(subField.FieldInfo.FieldType))
                        {
                            TypeOfEntity = TypeOfEntity.Formula,
                        });
                    }
                }

            // sort the nosave fields to be at the end of the list - this feature is not completed and has no tests
            var noSaveFields = Fields.Where(_ => _.NoSave).ToList();
            Fields.RemoveAll(_ => _.NoSave);
            SaveableFieldCount = Fields.Count;
            Fields.AddRange(noSaveFields);
        }

        public object[] GetRow(object obj)
        {
            if (TypeOfEntity == TypeOfEntity.PlainField)
                return new[] {FieldInfo.GetValue(obj)};
            var result = new object[Fields.Count];
            for (var i = 0; i < result.Length; i++)
                result[i] = Fields[i].TypeOfEntity != TypeOfEntity.Formula
                    ? Fields[i].FieldInfo.GetValue(obj)
                    : 0;
            return result;
        }

        public IEnumerable<Entity> AllEntities(bool includeFields)
        {
            yield return this;
            if(includeFields)
                foreach (var x in Fields.SelectMany(_ => _.AllEntities(true)))
                    yield return x;
            foreach (var x in _lists.SelectMany(_ => _.AllEntities(includeFields)))
                yield return x;
        }

        public Entity CloneWithNewTables(ITableService tableService, bool hasForeignKey = false)
        {
            var clone = (Entity) MemberwiseClone();
            if (clone.TypeOfEntity != TypeOfEntity.PlainField && !NoSave)
                clone.Table = tableService.New(this, hasForeignKey);
            clone._lists = clone._lists.Select(_ => _.CloneWithNewTables(tableService, true)).ToList();
            return clone;
        }

        public IEnumerable<Aggregator> Quark(object[] result)
        {
            foreach (var list in _lists)
                yield return new Aggregator {Entity = list, ParentEntity = this, Result = result};
        }

    }

    public class Aggregator
    {
        public object[] Result;
        public Entity ParentEntity;
        public Entity Entity;

        public void Update(object[] subResult)
        {
            foreach (var p in ParentEntity.TemporaryAggregationMapper)
            {
                var r = (Result[p.Item2] as IConvertible)?.ToDouble(null);
                Result[p.Item2] = r+ (subResult[p.Item1] as IConvertible)?.ToDouble(null);
            }
        }


    }

}