using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace factor10.Obj2Db
{
    public enum TypeOfEntity
    {
        Class,
        PlainField,
        IEnumerable,
    }

    public class EntitySpec
    {
        public string Name;
        public string ExternalName;
        public List<EntitySpec> Fields = new List<EntitySpec>();

        private EntitySpec()
        {
        }

        public bool IsField => !Fields.Any();

        public static EntitySpec Begin(string name = null, string externalName = null)
        {
            return new EntitySpec {Name = name, ExternalName = externalName};
        }

        public EntitySpec Add(EntitySpec entitySpec)
        {
            Fields.Add(entitySpec);
            return this;
        }

        public static implicit operator EntitySpec(string name)
        {
            return new EntitySpec {Name = name};
        }

    }

    public class Entity
    {
        public readonly string Name;
        public readonly string ExternalName;
        public readonly string TypeName;
        public LinkedFieldInfo FieldInfo { get; private set; }
        public readonly TypeOfEntity TypeOfEntity;

        public readonly List<Entity> Fields = new List<Entity>();
        public List<Entity> Lists { get; private set; } = new List<Entity>();

        public ITable Table;

        private Entity(LinkedFieldInfo fieldInfo)
        {
            FieldInfo = fieldInfo;
        }

        public Entity(Type type, EntitySpec entitySpec)
        {
            Name = entitySpec.Name;
            ExternalName = entitySpec.ExternalName ?? entitySpec.Name?.Replace(".", "");

            if (Name != null)
            {
                FieldInfo = new LinkedFieldInfo(type, Name);
                var ienum = FieldInfo.CheckForIEnumerable();
                if (ienum != null)
                {
                    TypeOfEntity = TypeOfEntity.IEnumerable;
                    type = ienum.GetGenericArguments()[0];
                    if (entitySpec.IsField)
                        Fields.Add(new Entity(LinkedFieldInfo.Null(type)));
                }
                else if (entitySpec.IsField)
                    TypeOfEntity = TypeOfEntity.PlainField;
                else
                    TypeOfEntity = TypeOfEntity.Class;
            }

            foreach (var subEntity in entitySpec.Fields)
            {
                var pe = new Entity(type, subEntity);
                (pe.TypeOfEntity == TypeOfEntity.IEnumerable ? Lists : Fields).Add(pe);
            }

            TypeName = type.Name;
        }

        public object[] GetRow(object obj)
        {
            if (TypeOfEntity == TypeOfEntity.PlainField)
                return new[] { FieldInfo.GetValue(obj) };
            var result = new object[Fields.Count];
            for (var i = 0; i < result.Length; i++)
                result[i] = Fields[i].FieldInfo.GetValue(obj);
            return result;
        }

        public IEnumerable<Entity> AllEntities(bool includeFields)
        {
            yield return this;
            if(includeFields)
                foreach (var x in Fields.SelectMany(_ => _.AllEntities(true)))
                    yield return x;
            foreach (var x in Lists.SelectMany(_ => _.AllEntities(includeFields)))
                yield return x;
        }

        public Entity CloneWithNewTables(ITableService tableService, bool hasForeignKey = false)
        {
            var clone = (Entity) MemberwiseClone();
            if (clone.TypeOfEntity != TypeOfEntity.PlainField)
                clone.Table = tableService.New(this, hasForeignKey);
            clone.Lists = clone.Lists.Select(_ => _.CloneWithNewTables(tableService, true)).ToList();
            return clone;
        }

    }

}