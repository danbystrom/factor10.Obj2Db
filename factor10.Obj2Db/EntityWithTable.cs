using System;
using System.Collections.Generic;
using System.Linq;

namespace factor10.Obj2Db
{
    public sealed class EntityWithTable
    {
        public readonly EntityClass Entity;
        public readonly ITable Table;

        public readonly List<EntityWithTable> Aggregators = new List<EntityWithTable>();
        public readonly bool HasAggregation;

        public EntityWithTable(EntityClass entity, ITableManager t, bool hasFk = false)
        {
            Entity = entity;
            if (!Entity.NoSave)
                Table = t.New(entity, hasFk);
            foreach (var e in entity.Lists)
                Aggregators.Add(new EntityWithTable(e, t, true));
            HasAggregation = Entity.AggregationMapper.Any();
        }

        public IEnumerable<EntityWithTable> GetSubEntitities(object[] result)
        {
            foreach (var aggragator in Aggregators)
                if (aggragator.HasAggregation)
                {
                    aggragator.Entity.AggregationBegin(result);
                    yield return aggragator;
                    aggragator.Entity.AggregationEnd(result);
                }
                else
                    yield return aggragator;
        }

    }


}

