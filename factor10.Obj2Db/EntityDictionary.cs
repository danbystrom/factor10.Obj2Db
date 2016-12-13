using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace factor10.Obj2Db
{
    public class ConcurrentEntityTableDictionary
    {
        private readonly ConcurrentDictionary<int, Entity> _dic = new ConcurrentDictionary<int, Entity>();
        private readonly Entity _template;

        public ConcurrentEntityTableDictionary(Entity template)
        {
            _template = template;
        }

        public Entity GetOrNew(int key)
        {
            Entity entity;
            if (!_dic.TryGetValue(key, out entity))
            {
                _dic.TryAdd(key, _template.CloneWithNewTables());
                // note that the one we tried to add is not necessarily the one that we'll get now...
                _dic.TryGetValue(key, out entity);
            }
            return entity;
        }

        public List<Table> AllTableFragments()
        {
            return _dic.Values.SelectMany(_ => _.AllEntities(false).Select(x => x.Table)).ToList();
        } 

    }

}
