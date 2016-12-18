using System;
using System.Collections.Generic;
using System.Linq;

namespace factor10.Obj2Db
{
    public class EntitySpec
    {
        public string name;
        public string externalname;
        public bool nosave;
        public List<EntitySpec> Fields = new List<EntitySpec>();

        public string aggregation;
        public string formula;
        public string where;

        private EntitySpec()
        {
        }

        public bool IsField => !Fields.Any();

        public static EntitySpec Begin(string name = null, string externalName = null)
        {
            return new EntitySpec { name = name, externalname = externalName };
        }

        public EntitySpec NotSaved()
        {
            if (Fields.Any())
                Fields.Last().nosave = true;
            else
                nosave = true;
            return this;
        }

        public EntitySpec Aggregates(string field)
        {
            if (!Fields.Any())
                throw new Exception("Can only be called on field");
            Fields.Last().aggregation = field;
            return this;
        }

        public EntitySpec Formula(string expression)
        {
            if (!Fields.Any())
                throw new Exception("Can only be called on field");
            Fields.Last().formula = expression;
            return this;
        }

        public EntitySpec Where(string whereClause)
        {
            if (Fields.Any())
                throw new Exception("Can only be called on list");
            where = whereClause;
            return this;
        }

        public EntitySpec Add(EntitySpec entitySpec)
        {
            Fields.Add(entitySpec);
            return this;
        }

        public static implicit operator EntitySpec(string name)
        {
            return new EntitySpec { name = name };
        }

    }

}
