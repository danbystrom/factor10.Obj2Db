﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace factor10.Obj2Db
{
    public sealed class ConcurrentEntityTableDictionary
    {
        private readonly ConcurrentDictionary<int, EntityWithTable> _dic = new ConcurrentDictionary<int, EntityWithTable>();
        private readonly EntityClass _template;
        private readonly ITableManager _tableManager;

        public ConcurrentEntityTableDictionary(ITableManager tableManager, EntityClass template)
        {
            _tableManager = tableManager;
            _template = template;
            GetOrNew(Thread.CurrentThread.ManagedThreadId);  // force creation of the first table set on current thread
        }

        public EntityWithTable GetOrNew(int key)
        {
            EntityWithTable entity;
            if (!_dic.TryGetValue(key, out entity))
            {
                _dic.TryAdd(key, new EntityWithTable(_template, _tableManager));
                // note that the one we tried to add is not necessarily the one that we'll get now...
                _dic.TryGetValue(key, out entity);
            }
            return entity;
        }

        public List<ITable> AllTableFragments()
        {
            return _dic.Values.SelectMany(_ => _.Lists.Select(x => x.Table)).ToList();
        } 

    }

}
