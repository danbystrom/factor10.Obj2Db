using System;
using System.Collections.Generic;
using System.Linq;

namespace factor10.Obj2Db
{
    public class entitySpec
    {
        public string name;
        public string externalname;
        public bool nosave;
        public List<entitySpec> fields;

        public string aggregation;
        public string formula;
        public string where;

        public entitySpec()
        {
        }

        public static entitySpec Begin(string name = null, string externalName = null)
        {
            return new entitySpec {name = name, externalname = externalName};
        }

        public entitySpec NotSaved()
        {
            if (Any())
                fields.Last().nosave = true;
            else
                nosave = true;
            return this;
        }

        public entitySpec Aggregates(string field)
        {
            if (string.IsNullOrEmpty(field))
                throw new ArgumentException("Empty aggregate field name");
            if (!Any())
                throw new Exception("Can only be called on field");
            fields.Last().aggregation = field;
            return this;
        }

        public entitySpec Formula(string expression)
        {
            if (string.IsNullOrEmpty(expression))
                throw new ArgumentException("Empty formula expression");
            if (!Any())
                throw new Exception("Can only be called on field");
            fields.Last().formula = expression;
            return this;
        }

        public entitySpec Where(string whereClause)
        {
            if (Any())
                throw new Exception("Where can only be called on list");
            where = whereClause;
            return this;
        }

        public entitySpec Add(entitySpec entitySpec)
        {
            if (fields == null)
                fields = new List<entitySpec>();
            fields.Add(entitySpec);
            return this;
        }

        public bool Any()
        {
            return fields != null && fields.Any();
        }

        public static implicit operator entitySpec(string name)
        {
            return new entitySpec {name = name};
        }

        public entitySpec(Entity entity)
        {
            name = entity.Spec.name;
            externalname = entity.Spec.externalname;
            nosave = entity.Spec.nosave;
            aggregation = entity.Spec.aggregation;
            formula = entity.Spec.formula;
            where = entity.Spec.where;
            foreach (var e in entity.Fields)
                Add(new entitySpec(e));
            foreach (var e in entity.Lists)
                Add(new entitySpec(e));
        }

    }

}
