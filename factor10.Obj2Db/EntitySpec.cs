using System;
using System.Collections.Generic;
using System.Linq;

namespace factor10.Obj2Db
{
    public class EntitySpec
    {
        public string Name;
        public string ExternalName;
        public bool NoSave;
        public List<EntitySpec> Fields = new List<EntitySpec>();

        public string Aggregation;
        public string formula;

        private EntitySpec()
        {
        }

        public bool IsField => !Fields.Any();

        public static EntitySpec Begin(string name = null, string externalName = null)
        {
            return new EntitySpec { Name = name, ExternalName = externalName };
        }

        public EntitySpec NotSaved()
        {
            if (Fields.Any())
                Fields.Last().NoSave = true;
            else
                NoSave = true;
            return this;
        }

        public EntitySpec Aggregates(string field)
        {
            if (!Fields.Any())
                throw new Exception("Can only be called on field");
            Fields.Last().Aggregation = field;
            return this;
        }

        public EntitySpec Formula(string formula)
        {
            if (!Fields.Any())
                throw new Exception("Can only be called on field");
            Fields.Last().formula = formula;
            return this;
        }

        public EntitySpec Add(EntitySpec entitySpec)
        {
            Fields.Add(entitySpec);
            return this;
        }

        public static implicit operator EntitySpec(string name)
        {
            return new EntitySpec { Name = name };
        }

    }

}
