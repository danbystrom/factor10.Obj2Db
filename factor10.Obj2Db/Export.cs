using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace factor10.Obj2Db
{

    public class Export<T>
    {
        private readonly Entity _entity;
        public readonly ITableFactory TableFactory;

        public Export(EntitySpec entitySpec, ITableFactory tableFactory = null)
        {
            _entity = new Entity(typeof(T), entitySpec);
            TableFactory = tableFactory ?? new TableFactory(null);
        }
 
        public void DumpEntities(int indent, Entity entity = null)
        {
            entity = entity ?? _entity;
            System.Diagnostics.Debug.Print(new string(' ', indent) + entity.Name + " (" + entity.TypeOfEntity + ")");
            foreach (var x in entity.Fields)
                DumpEntities(indent + 1, x);
            foreach (var x in entity.Lists)
                DumpEntities(indent + 1, x);
        }

        public List<Table> Run(T obj)
        {
            return Run(new[] {obj});
        }

        public List<Table> Run(IEnumerable<T> objs)
        {
            var ed = new ConcurrentEntityTableDictionary(_entity);
            objs.AsParallel().ForAll(_ =>
            {
                run(ed.GetOrNew(Thread.CurrentThread.ManagedThreadId), _, Guid.Empty);
            });
            return ed.AllTableFragments();
        }

        private void run(Entity entity, object obj, Guid parentRowId)
        {
            var tableRow = entity.Table.AddRow(parentRowId, entity.GetRow(obj));
            foreach (var subEntity in entity.Lists)
                foreach (var itm in (IEnumerable) subEntity.FieldInfo.GetValue(obj))
                    run(subEntity, itm, tableRow.PrimaryKey);
        }

    }

}