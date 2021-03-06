﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace factor10.Obj2Db
{
    public class entitySpec
    {
        public string name;
        public string externalname;
        public bool nosave;
        public bool primarykey;
        public List<entitySpec> fields;

        public string aggregation;
        public string aggregationtype;
        public string formula;
        public string where;

        public string type;

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

        public entitySpec Aggregates(string field, string aggregationtype = "sum")
        {
            if (string.IsNullOrEmpty(field))
                throw new ArgumentException("Empty aggregate field name");
            ensureField();
            fields.Last().aggregation = field;
            fields.Last().aggregationtype = aggregationtype;
            return this;
        }

        public entitySpec Formula(string expression)
        {
            if (string.IsNullOrEmpty(expression))
                throw new ArgumentException("Empty formula expression");
            ensureField();
            fields.Last().formula = expression;
            return this;
        }

        public entitySpec Where(string whereClause)
        {
            ensureList();
            where = whereClause;
            return this;
        }

        public entitySpec PrimaryKey()
        {
            ensureField();
            fields.Last().primarykey = true;
            return this;
        }

        private void ensureList()
        {
            if (Any())
                throw new Exception("Where can only be called on list");
        }

        private void ensureField()
        {
            if (!Any())
                throw new Exception("Where can only be called on field");
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

        public bool AnyNotStar()
        {
            return Any() && !(fields.Count == 1 && fields.First().name == "*");
        }

        public static implicit operator entitySpec(string name)
        {
            var split = name.Split('|');
            return new entitySpec
            {
                name = split[0],
                externalname = split.Length > 1 ? split[1] : null
            };
        }

        public entitySpec(Entity entity)
        {
            name = entity.Name;
            externalname = entity.ExternalName != name ? entity.ExternalName : null;
            nosave = entity.NoSave;
            aggregation = entity.Spec.aggregation;
            formula = entity.Spec.formula;
            where = entity.Spec.where;
            type = LinkedFieldInfo.FriendlyTypeName(entity.FieldType);
            foreach (var e in entity.Fields)
                Add(new entitySpec(e));
            foreach (var e in entity.Lists)
                Add(new entitySpec(e));
        }

    }

}
