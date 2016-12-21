using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;

namespace factor10.Obj2Db
{
    public sealed class EntityWithTable
    {
        public readonly Entity Entity;
        public readonly ITable Table;

        public readonly List<EntityWithTable> Lists = new List<EntityWithTable>();

        public EntityWithTable(Entity entity, ITableManager t, bool hasFk = false)
        {
            Entity = entity;
            if (!Entity.NoSave)
                Table = t.New(entity, hasFk);
            foreach (var e in entity.Lists)
                Lists.Add(new EntityWithTable(e, t, true));
        }

    }

}

