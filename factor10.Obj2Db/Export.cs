using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace factor10.Obj2Db
{

    public class Export<T>
    {
        private readonly ProcessedEntity _entity;
         
        public Export(Entity entity)
        {
            _entity = new ProcessedEntity(typeof(T), entity);
        }
 
        public void DumpEntities(int indent, ProcessedEntity entity = null)
        {
            entity = entity ?? _entity;
            System.Diagnostics.Debug.Print(new string(' ', indent) + entity.Name + " (" + entity.TypeOfEntity + ")");
            foreach (var x in entity.Fields)
                DumpEntities(indent + 1, x);
            foreach (var x in entity.Lists)
                DumpEntities(indent + 1, x);
        }

        public IEnumerable<Table> Run(T obj)
        {
            return Run(new[] {obj});
        }

        public List<Table> Run(IEnumerable<T> objs)
        {
            foreach(var obj in objs)
               run(_entity, obj, Guid.Empty);
            return _entity.GetAll().Select(_ => _.Table).Where(_ => _ != null).ToList();
        }

        private void run(ProcessedEntity entity, object obj, Guid parentRowId)
        {
            var tableRow = entity.Table.AddRow(parentRowId, entity.GetRow(obj));
            foreach (var subEntity in entity.Lists)
                foreach (var itm in (IEnumerable) subEntity.FieldInfo.GetValue(obj))
                    run(subEntity, itm, tableRow.PrimaryKey);
        }

    }

}