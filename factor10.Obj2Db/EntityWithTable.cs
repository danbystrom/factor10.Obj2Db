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

        public EntityWithTable(EntityClass entity, ITableManager t, bool hasFk = false)
        {
            Entity = entity;
            if (!Entity.NoSave)
                Table = t.New(entity, hasFk);
            foreach (var e in entity.Lists)
                Lists.Add(new EntityWithTable(e, t, true));
            if (entity.AggregationFields.Any())
                Aggregator = new Aggregator(entity, entity.AggregationFields.ToArray());
            //var a = entity.Fields.OfType<EntityAggregation>().ToArray();
            //if(a.Any())
            //    Aggregator = new Aggregator(entity, a);
        }

    }

}


