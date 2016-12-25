using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace factor10.Obj2Db
{

    public sealed class Export<T>
    {
        public readonly EntityClass TopEntity;
        public readonly ITableManager TableManager;

        public Export(entitySpec entitySpec, ITableManager tableManager = null)
        {
            TopEntity = Entity.Create(typeof(T), entitySpec);
            TableManager = tableManager ?? new InMemoryTableManager();
        }

        public void Run(T obj)
        {
            Run(new[] {obj});
        }

        public void Run(IEnumerable<T> objs)
        {
            var ed = new ConcurrentEntityTableDictionary(TableManager, TopEntity);
            objs.AsParallel().ForAll(_ =>
            {
                run(ed.GetOrNew(Thread.CurrentThread.ManagedThreadId), _, Guid.Empty);
            });
            TableManager.Flush();
        }

        private object[] run(EntityWithTable ewt, object obj, Guid parentRowId)
        {
            var pk = Guid.NewGuid();
            var rowResult = new object[ewt.Entity.Fields.Count];
            foreach (var aggregator in ewt.GetSubEntitities(rowResult))
            {
                var enumerable = aggregator.Entity.GetIEnumerable(obj);
                if (enumerable == null)
                    continue;
                if (aggregator.Entity.AggregationMapper.Any())
                {
                    foreach (var itm in enumerable)
                        aggregator.Entity.AggregationUpdate(rowResult, run(aggregator, itm, pk));
                }
                else
                    foreach (var itm in enumerable)
                        run(aggregator, itm, pk);
            }
            ewt.Entity.AssignValue(rowResult, obj);
            if (ewt.Entity.PassesFilter(rowResult))
                ewt.Table?.AddRow(pk, parentRowId, rowResult);
            return rowResult;
        }

    }

}