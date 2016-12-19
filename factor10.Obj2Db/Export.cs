using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace factor10.Obj2Db
{

    public class Export<T>
    {
        public readonly Entity Entity;
        public readonly ITableManager TableManager;

        public Export(EntitySpec entitySpec, ITableManager tableManager = null)
        {
            Entity = new Entity(typeof (T), entitySpec);
            TableManager = tableManager ?? new InMemoryTableManager();
        }

        public void Run(T obj)
        {
            Run(new[] {obj});
        }

        public void Run(IEnumerable<T> objs)
        {
            var ed = new ConcurrentEntityTableDictionary(TableManager, Entity);
            objs.AsParallel().ForAll(_ =>
            {
                run(ed.GetOrNew(Thread.CurrentThread.ManagedThreadId), _, Guid.Empty);
            });
            TableManager.Flush();
        }

        private object[] run(Entity entity, object obj, Guid parentRowId)
        {
            var pk = Guid.NewGuid();
            var rowResult = entity.GetRow(obj);
            foreach (var aggregator in getSubEntitities(entity, rowResult))
            {
                var subEntity = aggregator.Entity;
                var enumerable = subEntity.GetIEnumerable(obj);
                if (enumerable != null)
                    foreach (var itm in enumerable)
                    {
                        var subResult = run(subEntity, itm, pk);
                        aggregator.UpdateWith(subResult);
                    }
            }
            if(entity.FilterOk(rowResult))
                entity.Table?.AddRow(pk, parentRowId, rowResult);
            return rowResult;
        }

        private IEnumerable<Aggregator> getSubEntitities(Entity entity, object[] result)
        {
            foreach (var list in entity.Lists)
            {
                var aggragator = new Aggregator(list, result);
                yield return aggragator;
                aggragator.CoherseAggregatedValues();
            }
        }

    }

    public class Aggregator
    {
        public object[] Result;
        public Entity Entity;

        public Aggregator(Entity entity, object[] result)
        {
            Entity = entity;
            Result = result;
            foreach (var p in Entity.TemporaryAggregationMapper)
                Result[p.Item2] = 0.0;
        }

        public void UpdateWith(object[] subResult)
        {
            foreach (var p in Entity.TemporaryAggregationMapper)
            {
                var r = (Result[p.Item2] as IConvertible)?.ToDouble(null) ?? 0;
                Result[p.Item2] = r + (subResult[p.Item1] as IConvertible)?.ToDouble(null) ?? 0;
            }
        }

        public void CoherseAggregatedValues()
        {
            foreach (var p in Entity.TemporaryAggregationMapper)
                Result[p.Item2] = Entity.Fields[p.Item1].FieldInfo.CoherseType(Result[p.Item2]);
        }

    }

}