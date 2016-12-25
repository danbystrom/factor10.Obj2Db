﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using factor10.Obj2Db.Formula;

namespace factor10.Obj2Db
{
    public sealed class EntityClass : Entity
    {
        private readonly EvaluateRpn _whereClause;
        private readonly Entity[] _fieldsThenFormulas;

        public EntityClass(entitySpec entitySpec, Type type, LinkedFieldInfo fieldInfo)
            : base(entitySpec)
        {
            FieldInfo = fieldInfo;
            FieldType = fieldInfo?.FieldType;

            if (!Spec.Any() || isStarExpansionAndNoRealSubProperties(type))
            {
                var enumerable = LinkedFieldInfo.CheckForIEnumerable(type);
                //if (LinkedFieldInfo.CheckForIEnumerable(type) == null)
                    Fields.Add(new EntitySolitaire(type));
                //else
                    //throw new Exception("Working on this case...");
                //else
                //    Fields.Add(new EntityClass(entitySpec.Begin(), enumerable, null));
            }

            breakDownSubEntities(type);

            // move the nosave fields to always be at the end of the list - this feature is not completed and has no tests
            var noSaveFields = Fields.Where(_ => _.NoSave).ToList();
            Fields.RemoveAll(_ => _.NoSave);
            SaveableFieldCount = Fields.Count;
            Fields.AddRange(noSaveFields);

            // this is temporary - to be able to serialze a contract with "*" since it was digging up so much garbage...
            // need to investigae each "garbage" occurrence and handle it more elegantly
            Fields.RemoveAll(_ => _ is EntityPlainField && SqlStuff.Field2Sql(_.NameAndType, true) == null);
            Lists.RemoveAll(_ => !_.Fields.Any() && !_.Lists.Any());

            // now it's time to connect the aggregated fields
            for (var fi = 0; fi < Fields.Count; fi++)
                Fields[fi].ParentInitialized(this, fi);

            if ( !string.IsNullOrEmpty(Spec.where))
                _whereClause = new EvaluateRpn(new Rpn(Spec.where), Fields.Select(_ => _.NameAndType).ToList());

            for (var i = 0; i < Fields.Count; i++)
                Fields[i].ResultSetIndex = i;

            var fieldsThenFormulas = Fields.Where(_ => !(_ is EntityAggregation)).ToList();
            Func<Entity, int> typeComparer = _ => _ is EntityFormula ? -1 : 1;
            fieldsThenFormulas.Sort((x, y) => typeComparer(y) - typeComparer(x));
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

        private bool isStarExpansionAndNoRealSubProperties(Type type)
        {
            return Spec.fields.First().name == "*" && !LinkedFieldInfo.GetAllFieldsAndProperties(type).Any();
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
            return _whereClause == null || _whereClause.Eval(rowResult).Numeric > 0;
        }

        public IEnumerable GetIEnumerable(object obj)
        {
            return (IEnumerable)FieldInfo.GetValue(obj);
        }

        public override void AssignValue(object[] result, object obj)
        {
            foreach (var entity in _fieldsThenFormulas)
                entity.AssignValue(result, obj);
        }

        public void AggregationBegin(object[] result)
        {
            foreach (var p in AggregationMapper)
                result[p.Item2] = 0.0;
        }

        public void AggregationUpdate(object[] result, object[] subResult)
        {
            foreach (var p in AggregationMapper)
            {
                var r = (result[p.Item2] as IConvertible)?.ToDouble(null) ?? 0;
                result[p.Item2] = r + (subResult[p.Item1] as IConvertible)?.ToDouble(null) ?? 0;
            }
        }

        public void AggregationEnd(object[] result)
        {
            foreach (var p in AggregationMapper)
                result[p.Item2] = Fields[p.Item1].FieldInfo.CoherseType(result[p.Item2]);
        }

    }

}
