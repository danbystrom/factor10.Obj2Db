using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace factor10.Obj2Db
{

    public sealed class DataExtract<T>
    {
        public readonly EntityClass TopEntity;
        public readonly ITableManager TableManager;

        public DataExtract(entitySpec entitySpec, ITableManager tableManager = null, Action<string> log = null)
        {
            TopEntity = Entity.Create(entitySpec, typeof(T), log);
            TableManager = tableManager ?? new InMemoryTableManager();
        }

        public void Run(T obj)
        {
            Run(new[] {obj});
        }

        public void Run(IEnumerable<T> objs)
        {
            var ed = new ConcurrentEntityTableDictionary(TableManager, TopEntity);
            var rowIndex = 0;
            objs.AsParallel().ForAll(_ =>
            {
                run(ed.GetOrNew(Thread.CurrentThread.ManagedThreadId), _, Guid.Empty, Interlocked.Increment(ref rowIndex));
            });
            TableManager.Flush();
        }

        private object[] run(
            EntityWithTable ewt,
            object obj,
            Guid parentRowId,
            int rowIndex)
        {
            var pk = Guid.NewGuid();
            var rowResult = new object[ewt.Entity.Fields.Count+1];
            rowResult[rowResult.Length - 1] = rowIndex;
            var subRowIndex = 0;
            foreach (var subEwt in ewt.Lists)
            {
                var enumerable = subEwt.Entity.GetIEnumerable(obj);
                if (enumerable == null)
                    continue;
                if (subEwt.HasAggregation)
                {
                    subEwt.Entity.AggregationBegin(rowResult);
                    foreach (var itm in enumerable)
                        subEwt.Entity.AggregationUpdate(rowResult, run(subEwt, itm, pk, subRowIndex++));
                    subEwt.Entity.AggregationEnd(rowResult);
                }
                else
                    foreach (var itm in enumerable)
                        run(subEwt, itm, pk, subRowIndex++);
            }
            ewt.Entity.AssignValue(rowResult, obj);
            if (ewt.Entity.PassesFilter(rowResult))
                ewt.Table?.AddRow(pk, parentRowId, rowResult);
            return rowResult;
        }

    }

}