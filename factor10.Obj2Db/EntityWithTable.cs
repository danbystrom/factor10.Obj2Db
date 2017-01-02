using System;
using System.Collections.Generic;
using System.Linq;

namespace factor10.Obj2Db
{
    public sealed class EntityWithTable
    {
        public readonly EntityClass Entity;
        public readonly ITable Table;

        public readonly List<EntityWithTable> Lists = new List<EntityWithTable>();
        public readonly Aggregator Aggregator;

        public EntityWithTable(
            EntityClass entity,
            ITableManager t,
            bool isTopTable = true)
        {
            Entity = entity;
            if (!Entity.NoSave)
                Table = t.New(entity, isTopTable, !Entity.Lists.Any(), -1);
            foreach (var e in entity.Lists)
                Lists.Add(new EntityWithTable(e, t, false));
            if (entity.AggregationFields.Any())
                Aggregator = new Aggregator(entity.AggregationFields.ToArray(), entity.ParentEffectiveFieldCount);
        }

        public object GetPrimaryKey()
        {
            if (Table?.IsLeafTable ?? true)
                return null;
            return Guid.NewGuid();
        }

    }

}


