﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using factor10.Obj2Db.Formula;

namespace factor10.Obj2Db
{
    public enum TypeOfEntity
    {
        Class,
        PlainField,
        IEnumerable,
        Aggregation,
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

        public Type FieldType;

        public ITable Table;

        public string Aggregation;
        public string Formula;

        public List<Tuple<int, int>> TemporaryAggregationMapper = new List<Tuple<int, int>>();

        private EvaluateRpn _evaluator;

        private Entity(string name, LinkedFieldInfo fieldInfo)
        {
            Name = name;
            ExternalName = name?.Replace(".", "");
            FieldInfo = fieldInfo;
            TypeOfEntity = TypeOfEntity.PlainField;
        }

        public Entity(Type type, EntitySpec entitySpec)
        {
            Name = entitySpec.Name;
            ExternalName = entitySpec.ExternalName ?? Name?.Replace(".", "");
            NoSave = entitySpec.NoSave;
            Aggregation = entitySpec.Aggregation;
            Formula = entitySpec.formula;

            if (!string.IsNullOrEmpty(Aggregation))
                TypeOfEntity = TypeOfEntity.Aggregation;
            else if (!string.IsNullOrEmpty(entitySpec.formula))
                TypeOfEntity = TypeOfEntity.Formula;
            else if (Name != null)
            {
                FieldInfo = new LinkedFieldInfo(type, Name);
                FieldType = FieldInfo.FieldType;
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

            // sort the nosave fields to be at the end of the list - this feature is not completed and has no tests
            var noSaveFields = Fields.Where(_ => _.NoSave).ToList();
            Fields.RemoveAll(_ => _.NoSave);
            SaveableFieldCount = Fields.Count;
            Fields.AddRange(noSaveFields);

            // now it's time to connect the aggregated fields
            for (var fi = 0; fi < Fields.Count; fi++)
            {
                var field = Fields[fi];
                if (field.TypeOfEntity != TypeOfEntity.Aggregation)
                    continue;
                var agg = field.Aggregation;
                var subEntity = _lists.FirstOrDefault(_ => agg.StartsWith(_.Name + "."));
                if (subEntity == null)
                    throw new Exception($"Unable to find subentity for aggregation '{agg}'");
                var subFieldName = agg.Substring(subEntity.Name.Length + 1);
                var subFieldIndex = subEntity.Fields.FindIndex(_ => (_.Name ?? "") == subFieldName);
                if (subFieldIndex < 0)
                    throw new Exception();
                subEntity.TemporaryAggregationMapper.Add(Tuple.Create(subFieldIndex, fi));
                field.FieldType = subEntity.Fields[subFieldIndex].FieldType;
            }

            //...and to construct the evaluators
            for (var fi = 0; fi < Fields.Count; fi++)
            {
                var field = Fields[fi];
                if (field.TypeOfEntity != TypeOfEntity.Formula)
                    continue;
                field._evaluator = new EvaluateRpn(
                    new Rpn(field.Formula),
                    Fields.Select(_ => Tuple.Create(_.Name, _.FieldType)).ToList());
            }

        }

        public IEnumerable GetIEnumerable(object obj)
        {
            return (IEnumerable) FieldInfo.GetValue(obj);
        }

        public object[] GetRow(object obj)
        {
            if (TypeOfEntity == TypeOfEntity.PlainField)
                return new[] {FieldInfo.GetValue(obj)};
            var result = new object[Fields.Count];
            for (var i = 0; i < result.Length; i++)
                if (Fields[i].TypeOfEntity == TypeOfEntity.PlainField)
                    result[i] = Fields[i].FieldInfo.GetValue(obj);
            for (var i = 0; i < result.Length; i++)
                if (Fields[i].TypeOfEntity == TypeOfEntity.Formula)
                    result[i] = Fields[i]._evaluator.Eval(result).Numeric;
            return result;
        }

        public IEnumerable<Entity> AllEntities(bool includeFields)
        {
            yield return this;
            if (includeFields)
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
            {
                var aggragator = new Aggregator(list, result);
                yield return aggragator;
                aggragator.CoherseAggregatedValues();
            }
        }

    }

    public class Aggregator
    {
        public object[] Result;
        public Entity Entity;

        public Aggregator(Entity entity, object[] result)
        {
            Entity = entity;
            Result = result;
            foreach (var p in Entity.TemporaryAggregationMapper)
                Result[p.Item2] = 0.0;
        }

        public void Update(object[] subResult)
        {
            foreach (var p in Entity.TemporaryAggregationMapper)
            {
                var r = (Result[p.Item2] as IConvertible)?.ToDouble(null);
                Result[p.Item2] = r + (subResult[p.Item1] as IConvertible)?.ToDouble(null);
            }
        }

        public void CoherseAggregatedValues()
        {
            foreach (var p in Entity.TemporaryAggregationMapper)
                Result[p.Item2] = Entity.Fields[p.Item1].FieldInfo.CoherseType(Result[p.Item2]);
        }
    }

}