using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using factor10.Obj2Db.Formula;

namespace factor10.Obj2Db
{
    public sealed class EntityClass : Entity
    {
        private readonly EvaluateRpn _evaluator;
        private readonly Entity[] _fieldsThenFormulas;

        public EntityClass(entitySpec entitySpec, Type type, LinkedFieldInfo fieldInfo)
            : base(entitySpec)
        {
            FieldInfo = fieldInfo;
            FieldType = fieldInfo?.FieldType;

            if (Spec.fields == null || !Spec.fields.Any() ||
                (Spec.fields.First().name == "*" && !LinkedFieldInfo.GetAllFieldsAndProperties(type).Any()))
                Fields.Add(new EntitySolitaire(type));

            breakDownSubEntities(type);

            // move the nosave fields to be at the end of the list - this feature is not completed and has no tests
            var noSaveFields = Fields.Where(_ => _.NoSave).ToList();
            Fields.RemoveAll(_ => _.NoSave);
            SaveableFieldCount = Fields.Count;
            Fields.AddRange(noSaveFields);

            // this was to be able to serialze a contract, since "*" was digging up so much garbage...
            Fields.RemoveAll(_ =>
                _ is EntityPlainField && SqlStuff.Field2Sql(new NameAndType(null, _.FieldType), true) == null);
            Lists.RemoveAll(_ => !_.Fields.Any() && !_.Lists.Any());

            // now it's time to connect the aggregated fields
            for (var fi = 0; fi < Fields.Count; fi++)
                Fields[fi].ParentInitialized(this, fi);

            if ( !string.IsNullOrEmpty(Spec.where))
                _evaluator = new EvaluateRpn(new Rpn(Spec.where), Fields.Select(_ => _.NameAndType).ToList());

            for (var i = 0; i < Fields.Count; i++)
                Fields[i].ResultSetIndex = i;

            var fieldsThenFormulas = Fields.Where(_ => !(_ is EntityAggregation)).ToList();
            Func<Entity, int> q = w =>
            {
                if (w is EntityFormula)
                    return -1;
                if (w is EntityPlainField)
                    return 1;
                return 0;
            };
            fieldsThenFormulas.Sort((x, y) => q(y) - q(x));
            _fieldsThenFormulas = fieldsThenFormulas.ToArray();
        }

        private void breakDownSubEntities(Type type)
        {
            if (Spec.fields == null)
                return;
            foreach (var subEntitySpec in Spec.fields)
            {
                subEntitySpec.nosave |= NoSave; // propagate NoSave all the way down until we reach turtles
                foreach (var subEntity in expansionOverStar(type, subEntitySpec, new HashSet<Type>()))
                    if(subEntity is EntityClass)
                        Lists.Add((EntityClass)subEntity);
                    else
                        Fields.Add(subEntity);
            }
        }

        private static IEnumerable<Entity> expansionOverStar(
            Type masterType,
            entitySpec subEntitySpec,
            HashSet<Type> haltRecursion,
            string prefix = "",
            Type subType = null)
        {
            if (subEntitySpec.name != "*")
            {
                yield return create(masterType, subEntitySpec);
                yield break;
            }

            subType = subType ?? masterType;

            if (haltRecursion.Contains(subType))
                throw new Exception("Circular reference detected while processing inclusion of all fields ('*')");
            haltRecursion.Add(subType);

            foreach (var nameAndType in LinkedFieldInfo.GetAllFieldsAndProperties(subType))
            {
                if (nameAndType.Type == typeof(object))
                    continue;
                var spec = entitySpec.Begin(prefix + nameAndType.Name);
                var subProperties = LinkedFieldInfo.GetAllFieldsAndProperties(nameAndType.Type);
                if (LinkedFieldInfo.CheckForIEnumerable(nameAndType.Type) != null)
                {
                    spec.Add("*");
                    yield return create(masterType, spec);
                }
                else if (!subProperties.Any())
                    yield return create(masterType, spec);

                foreach (var liftedSubProperty in subProperties)
                {
                    if (liftedSubProperty.Type == typeof(object))
                        continue;
                    var propName = $"{prefix}{nameAndType.Name}.{liftedSubProperty.Name}";
                    if (LinkedFieldInfo.GetAllFieldsAndProperties(liftedSubProperty.Type).Any())
                        foreach (var q in expansionOverStar(masterType, "*", haltRecursion, propName + ".", liftedSubProperty.Type))
                            yield return q;
                    else
                        yield return create(masterType, propName);
                }
            }

            haltRecursion.Remove(subType);
        }

        public override bool PassesFilter(object[] rowResult)
        {
            return _evaluator == null || _evaluator.Eval(rowResult).Numeric > 0;
        }

        public IEnumerable GetIEnumerable(object obj)
        {
            return (IEnumerable)FieldInfo.GetValue(obj);
        }

        public object[] GetRow(object obj)
        {
            var result = new object[Fields.Count];
            foreach (var entity in _fieldsThenFormulas)
                entity.AssignValue(result, obj);
            return result;
        }


    }

}
