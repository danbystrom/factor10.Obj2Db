using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace factor10.Obj2Db
{
    public interface IDataExtractCompleted
    {
        void Completed();
    }

    public class DataExtract
    {
        public readonly EntityClass TopEntity;
        public readonly ITableManager TableManager;
        public readonly Type Type;

        public DataExtract(Type type, entitySpec entitySpec, ITableManager tableManager = null, Action<string> log = null)
        {
            if (type == null)
                throw new NullReferenceException(nameof(type));
            if (entitySpec == null)
                throw new NullReferenceException(nameof(entitySpec));
            Type = type;
            TopEntity = Entity.Create(null, entitySpec, Type, log);
            TableManager = tableManager ?? new InMemoryTableManager();
        }

        public void Run(object obj)
        {
            Run(new[] { obj });
        }

        public void Run(IEnumerable<object> objs)
        {
            var ed = new ConcurrentEntityTableDictionary(TableManager, TopEntity);
            var nextRowIndex = 0;
            TableManager.Begin();
            objs.AsParallel().ForAll(_ =>
            {
                run(ed.GetOrNew(Thread.CurrentThread.ManagedThreadId), _, Guid.Empty, Interlocked.Increment(ref nextRowIndex));
                (_ as IDataExtractCompleted)?.Completed();
            });
            TableManager.End();
        }

        private object[] run(
            EntityWithTable ewt,
            object obj,
            object foreignKey,
            int rowIndex)
        {
            var rowResult = new object[ewt.Entity.EffectiveFieldCount];
            rowResult[rowResult.Length - 1] = rowIndex;
            var subRowIndex = 0;
            if (!ewt.Entity.AssignAndCheckResultPre(rowResult, obj))
                return null;
            //ewt.Entity.AssignResultPre(rowResult, obj);
            //if (!ewt.Entity.PassesFilterPre(rowResult))
            //    return null;
            var primaryKey = ewt.GetPrimaryKey(rowResult);
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
            ewt.Entity.AssignResultPost(rowResult, obj);
            if (!ewt.Entity.PassesFilterPost(rowResult))
                return null;
            ewt.Table?.AddRow(primaryKey, foreignKey, rowResult);
            return rowResult;
        }

    }

    public class DataExtract<T> : DataExtract
    {
        public DataExtract(entitySpec entitySpec, ITableManager tableManager = null, Action<string> log = null)
            : base(typeof(T), entitySpec, tableManager, log)
        {
        }

    }

}