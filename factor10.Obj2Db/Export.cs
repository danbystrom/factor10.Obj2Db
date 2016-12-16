using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace factor10.Obj2Db
{

    public class Export<T>
    {
        private readonly Entity _entity;
        public readonly ITableService TableService;

        public Export(EntitySpec entitySpec, ITableService tableService = null)
        {
            _entity = new Entity(typeof (T), entitySpec);
            TableService = tableService ?? new InMemoryTableService();
        }

        public void Run(T obj)
        {
            Run(new[] {obj});
        }

        public void Run(IEnumerable<T> objs)
        {
            var ed = new ConcurrentEntityTableDictionary(TableService, _entity);
            objs.AsParallel().ForAll(_ =>
            {
                run(ed.GetOrNew(Thread.CurrentThread.ManagedThreadId), _, Guid.Empty);
            });
            TableService.Flush();
        }

        private object[] run(Entity entity, object obj, Guid parentRowId)
        {
            var pk = Guid.NewGuid();
            var rowResult = entity.GetRow(obj);
            foreach (var q in entity.Quark(rowResult))
            {
                var subEntity = q.Entity;
                var enumerable = subEntity.GetIEnumerable(obj);
                if (enumerable != null)
                    foreach (var itm in enumerable)
                    {
                        var subResult = run(subEntity, itm, pk);
                        q.Update(subResult);
                    }
            }
            entity.Table?.AddRow(pk, parentRowId, rowResult);
            return rowResult;
        }

    }

}