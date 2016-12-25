using System;
using System.Collections.Generic;

namespace factor10.Obj2Db
{
    public sealed class EntityWithTable
    {
        public readonly EntityClass Entity;
        public readonly ITable Table;

        //public readonly List<EntityWithTable> Lists = new List<EntityWithTable>();
        public readonly List<Aggregator> Lists = new List<Aggregator>();

        public EntityWithTable(EntityClass entity, ITableManager t, bool hasFk = false)
        {
            Entity = entity;
            if (!Entity.NoSave)
                Table = t.New(entity, hasFk);
            foreach (var e in entity.Lists)
                Lists.Add(new Aggregator(new EntityWithTable(e, t, true)));
        }

        public IEnumerable<Aggregator> GetSubEntitities(object[] result)
        {
            foreach (var aggragator in Lists)
            {
                aggragator.Start(result);
                yield return aggragator;
                aggragator.CoherseAggregatedValues(result);
            }
        }

    }

    public sealed class Aggregator
    {
        public EntityWithTable EntityWithTable;
        public List<Tuple<int, int>> AggregationMapper;

        public Aggregator(EntityWithTable entityWithTable)
        {
            EntityWithTable = entityWithTable;
            AggregationMapper = EntityWithTable.Entity.AggregationMapper;
        }

        public void Start(object[] result)
        {
            foreach (var p in AggregationMapper)
                result[p.Item2] = 0.0;
        }

        public void UpdateWith(object[] result, object[] subResult)
        {
            foreach (var p in AggregationMapper)
            {
                var r = (result[p.Item2] as IConvertible)?.ToDouble(null) ?? 0;
                result[p.Item2] = r + (subResult[p.Item1] as IConvertible)?.ToDouble(null) ?? 0;
            }
        }

        public void CoherseAggregatedValues(object[] result)
        {
            foreach (var p in AggregationMapper)
                result[p.Item2] = EntityWithTable.Entity.Fields[p.Item1].FieldInfo.CoherseType(result[p.Item2]);
        }

    }

}

