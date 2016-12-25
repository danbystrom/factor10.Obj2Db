using System;
using System.Collections.Generic;

namespace factor10.Obj2Db
{
    public sealed class EntityWithTable
    {
        public readonly EntityClass Entity;
        public readonly ITable Table;

        //public readonly List<EntityWithTable> Lists = new List<EntityWithTable>();
        public readonly List<Aggregator> Aggregators = new List<Aggregator>();

        public EntityWithTable(EntityClass entity, ITableManager t, bool hasFk = false)
        {
            Entity = entity;
            if (!Entity.NoSave)
                Table = t.New(entity, hasFk);
            foreach (var e in entity.Lists)
                Aggregators.Add(new Aggregator(new EntityWithTable(e, t, true)));
        }

        public IEnumerable<Aggregator> GetSubEntitities(object[] result)
        {
            foreach (var aggragator in Aggregators)
                if (aggragator.HasAggragation)
                {
                    aggragator.Start(result);
                    yield return aggragator;
                    aggragator.CoherseAggregatedValues(result);
                }
                else
                    yield return aggragator;
        }

    }

    public sealed class Aggregator
    {
        public EntityWithTable EntityWithTable;
        private readonly List<Tuple<int, int>> _aggregationMapper;
        public bool HasAggragation => _aggregationMapper.Count != 0;

        public Aggregator(EntityWithTable entityWithTable)
        {
            EntityWithTable = entityWithTable;
            _aggregationMapper = EntityWithTable.Entity.AggregationMapper;
        }

        public void Start(object[] result)
        {
            foreach (var p in _aggregationMapper)
                result[p.Item2] = 0.0;
        }

        public void UpdateWith(object[] result, object[] subResult)
        {
            foreach (var p in _aggregationMapper)
            {
                var r = (result[p.Item2] as IConvertible)?.ToDouble(null) ?? 0;
                result[p.Item2] = r + (subResult[p.Item1] as IConvertible)?.ToDouble(null) ?? 0;
            }
        }

        public void CoherseAggregatedValues(object[] result)
        {
            foreach (var p in _aggregationMapper)
                result[p.Item2] = EntityWithTable.Entity.Fields[p.Item1].FieldInfo.CoherseType(result[p.Item2]);
        }

    }

}

