using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace factor10.Obj2Db
{
    public class ConcurrentEntityTableDictionary
    {
        private readonly ConcurrentDictionary<int, Entity> _dic = new ConcurrentDictionary<int, Entity>();
        private readonly Entity _template;
        private readonly ITableService _tableService;

        public ConcurrentEntityTableDictionary(ITableService tableService, Entity template)
        {
            _tableService = tableService;
            _template = template;
        }

        public Entity GetOrNew(int key)
        {
            Entity entity;
            if (!_dic.TryGetValue(key, out entity))
            {
                _dic.TryAdd(key, _template.CloneWithNewTables(_tableService));
                // note that the one we tried to add is not necessarily the one that we'll get now...
                _dic.TryGetValue(key, out entity);
            }
            return entity;
        }

        public List<ITable> AllTableFragments()
        {
            return _dic.Values.SelectMany(_ => _.AllEntities(false).Select(x => x.Table)).ToList();
        } 

    }

}
