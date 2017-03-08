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
            var nextRowIndex = 0;
            objs.AsParallel().ForAll(_ =>
            {
                run(ed.GetOrNew(Thread.CurrentThread.ManagedThreadId), _, Guid.Empty, Interlocked.Increment(ref nextRowIndex));
            });
            TableManager.Flush();
        }

        private object[] run(
            EntityWithTable ewt,
            object obj,
            object foreignKey,
            int rowIndex)
        {
            var rowResult = new object[ewt.Entity.EffectiveFieldCount];
            rowResult[rowResult.Length - 1] = rowIndex;
            var primaryKey = ewt.GetPrimaryKey();
            var subRowIndex = 0;
            foreach (var subEwt in ewt.Lists)
            {
                var enumerable = subEwt.Entity.GetIEnumerable(obj);
                var aggregator = subEwt.Aggregator;
                aggregator?.Begin();
                if (enumerable != null)
                    foreach (var itm in enumerable)
                    {
                        var subresult = run(subEwt, itm, primaryKey, subRowIndex++);
                        aggregator?.Update(subresult);
                    }
                aggregator?.End(rowResult);
            }
            ewt.Entity.AssignResult(rowResult, obj);
            if (!ewt.Entity.PassesFilter(rowResult))
                return null;
            ewt.Table?.AddRow(primaryKey, foreignKey, rowResult);
            return rowResult;
        }

    }

}