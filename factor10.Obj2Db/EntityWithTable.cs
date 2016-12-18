using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace factor10.Obj2Db
{
    public class EntityWithTable
    {
        public readonly Entity Entity;
        public readonly ITable Table;

        public readonly List<EntityWithTable> List = new List<EntityWithTable>();
         
        public EntityWithTable(Entity entity, ITableService t)
        {
            //Entity = entity;
            //Table = new Table(t);
            //foreach(var e in entity.)
        }

    }

}

