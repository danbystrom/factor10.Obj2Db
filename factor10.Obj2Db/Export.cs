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

        public Export(entitySpec entitySpec, ITableManager tableManager = null, Action<string> log = null)
        {
            TopEntity = Entity.Create(entitySpec, typeof(T), log);
            TableManager = tableManager ?? new InMemoryTableManager();
            //q(TopEntity);
        }

        private void q(EntityClass x)
        {
            foreach (var y in x.Lists)
                q(y);
            if (x.FieldInfo?.FieldType != x.FieldType)
                throw new Exception();
            foreach(var f in x.Fields)
                if (f.FieldInfo?.FieldType != f.FieldType)
                    throw new Exception();
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
            foreach (var subEwt in ewt.Lists)
            {
                var enumerable = subEwt.Entity.GetIEnumerable(obj);
                if (enumerable == null)
                    continue;
                if (subEwt.HasAggregation)
                {
                    subEwt.Entity.AggregationBegin(rowResult);
                    foreach (var itm in enumerable)
                        subEwt.Entity.AggregationUpdate(rowResult, run(subEwt, itm, pk));
                    subEwt.Entity.AggregationEnd(rowResult);
                }
                else
                    foreach (var itm in enumerable)
                        run(subEwt, itm, pk);
            }
            ewt.Entity.AssignValue(rowResult, obj);
            if (ewt.Entity.PassesFilter(rowResult))
                ewt.Table?.AddRow(pk, parentRowId, rowResult);
            return rowResult;
        }

    }

}