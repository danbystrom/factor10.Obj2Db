using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using factor10.Obj2Db.Formula;

namespace factor10.Obj2Db
{
    public sealed class EntityClass : Entity
    {
        private readonly WhereClause _whereClause;
        //private readonly bool _isWhereClauseBasedOnAggregation;
        private readonly Entity[] _fieldsThenNonAggregatedFormulas;
        private readonly Entity[] _aggregatedFormulas;
        public readonly List<EntityAggregation> AggregationFields = new List<EntityAggregation>();

        public readonly int EffectiveFieldCount;
        public int ParentEffectiveFieldCount { get; private set; }
        public Type ForeignKeyType { get; private set; }
        public string ForeignKeyName { get; private set; }
        public readonly string TableName;

        public readonly int PrimaryKeyIndex;
        public string PrimaryKeyName => PrimaryKeyIndex < 0 ? "id_" : Fields[PrimaryKeyIndex].Name;

        public EntityClass(
            EntityClass parent,
            entitySpec entitySpec,
            Type type,
            LinkedFieldInfo fieldInfo,
            Action<string> log,
            bool throwOnCircularReference)
            : base(entitySpec)
        {
            log?.Invoke(
                $"EntityClass ctor: {entitySpec.name}/{entitySpec.fields?.Count ?? 0} - {type?.Name} - {fieldInfo?.FieldType} - {fieldInfo?.IEnumerable?.Name}");

            TableName = parent != null && Spec.externalname == null
                ? string.Join("_", parent.TableName, ExternalName)
                : ExternalName;

            FieldInfo = fieldInfo;
            FieldType = fieldInfo?.FieldType;

            if (!Spec.Any() || isStarExpansionAndNoRealSubProperties(type))
            {
                // not sure if this should be allowed...
                Fields.Add(new EntitySolitaire(type));
            }

            breakDownSubEntities(type, log, throwOnCircularReference);

            // move the nosave fields to always be at the end of the list
            var noSaveFields = Fields.Where(_ => _.NoSave).ToList();
            Fields.RemoveAll(_ => _.NoSave);
            SaveableFieldCount = Fields.Count;
            Fields.AddRange(noSaveFields);

            // this is temporary - to be able to serialze a contract with "*" since it was digging up so much garbage...
            // need to investigae each "garbage" occurrence and handle it more elegantly
            Fields.RemoveAll(_ => _ is EntityPlainField && SqlHelpers.Field2Sql(_.NameAndType.Name, _.NameAndType.Type, false, 0, true) == null);
            Lists.RemoveAll(_ => !_.Fields.Any() && !_.Lists.Any());

            if (Fields.Count(_ => _.Spec.primarykey) > 1)
                throw new Exception("There may be no more than one primary key field");
            PrimaryKeyIndex = Fields.FindIndex(_ => _.Spec.primarykey);

            EffectiveFieldCount = Fields.Count + 1;
            for (var fi = 0; fi < Fields.Count; fi++)
                Fields[fi].ParentInitialized(this, fi);
            for (var li = 0; li < Lists.Count; li++)
                Lists[li].ParentInitialized(this, li);

            for (var i = 0; i < Fields.Count; i++)
                Fields[i].ResultSetIndex = i;

            var fieldsThenFormulas = Fields.Where(_ => !(_ is EntityAggregation)).ToList();
            SortWithFormulasLast(fieldsThenFormulas);
            _fieldsThenNonAggregatedFormulas = fieldsThenFormulas.Where(_ => !_.IsBasedOnAggregation).ToArray();
            _aggregatedFormulas = fieldsThenFormulas.Where(_ => _.IsBasedOnAggregation).ToArray();

            if (!string.IsNullOrEmpty(Spec.where))
                _whereClause = new WhereClause(Spec.where, Fields, _fieldsThenNonAggregatedFormulas);

            if (PrimaryKeyIndex >= 0 && Fields[PrimaryKeyIndex].IsBasedOnAggregation)
                throw new Exception($"The primary key must not be based on an aggregation (table '{TableName}')");
        }

        public static void SortWithFormulasLast(List<Entity> list)
        {
            Func<Entity, int> typeComparer = _ => _ is EntityFormula ? -1 : 1;
            list.Sort((x, y) => typeComparer(y) - typeComparer(x));
        }

        private void breakDownSubEntities(Type type, Action<string> log, bool throwOnCircularReference)
        {
            if (Spec.fields == null)
                return;
            foreach (var subEntitySpec in Spec.fields)
            {
                subEntitySpec.nosave |= NoSave; // propagate NoSave all the way down until we reach turtles
                foreach (var subEntity in expansionOverStar(log, this, type, subEntitySpec, new HashSet<Type>(), throwOnCircularReference))
                    if (subEntity is EntityClass)
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
            Action<string> log,
            EntityClass parent,
            Type masterType,
            entitySpec subEntitySpec,
            HashSet<Type> detectCircularRef,
            bool throwOnCircularReference,
            string prefix = "",
            Type subType = null)
        {
            if (subEntitySpec.name != "*")
            {
                yield return create(parent, subEntitySpec, masterType, log, throwOnCircularReference);
                yield break;
            }

            subType = subType ?? masterType;

            if (detectCircularRef.Contains(subType))
                if (throwOnCircularReference)
                    throw new Exception("Circular reference detected while processing inclusion of all fields ('*')");
                else
                {
                    yield return new EntityClass(parent, entitySpec.Begin(prefix.TrimEnd(".".ToCharArray())), masterType, null, log, true);
                    yield break;
                }
            detectCircularRef.Add(subType);

            foreach (var nameAndType in LinkedFieldInfo.GetAllFieldsAndProperties(subType))
            {
                if (nameAndType.Type == typeof(object))
                    continue;
                var spec = entitySpec.Begin(prefix + nameAndType.Name);
                var subProperties = LinkedFieldInfo.GetAllFieldsAndProperties(nameAndType.Type);
                if (LinkedFieldInfo.CheckForIEnumerable(nameAndType.Type) != null)
                {
                    spec.Add("*");
                    yield return create(parent, spec, masterType, log, throwOnCircularReference);
                }
                else if (!subProperties.Any())
                    yield return create(parent, spec, masterType, log, throwOnCircularReference);

                foreach (var liftedSubProperty in subProperties)
                {
                    if (liftedSubProperty.Type == typeof(object))
                        continue;
                    var propName = $"{prefix}{nameAndType.Name}.{liftedSubProperty.Name}";
                    if (LinkedFieldInfo.GetAllFieldsAndProperties(liftedSubProperty.Type).Any())
                        foreach (var q in expansionOverStar(log, parent, masterType, "*", detectCircularRef, throwOnCircularReference, propName + ".", liftedSubProperty.Type))
                            yield return q;
                    else
                        yield return create(parent, entitySpec.Begin(propName).Add("*"), masterType, log, throwOnCircularReference);
                    //yield return create(propName, masterType, log);
                }
            }

            detectCircularRef.Remove(subType);
        }

        public override void ParentInitialized(EntityClass parent, int index)
        {
            ParentEffectiveFieldCount = parent.EffectiveFieldCount;
            ForeignKeyType = parent.PrimaryKeyIndex < 0 ? typeof(Guid) : parent.Fields[parent.PrimaryKeyIndex].FieldType;
            ForeignKeyName = string.Join("_", parent.TableName, parent.PrimaryKeyName);
        }

        public bool AssignAndCheckResultPre(object[] rowResult, object obj)
        {
            if (_whereClause == null)
            {
                foreach (var entity in _fieldsThenNonAggregatedFormulas)
                    entity.AssignResult(rowResult, obj);
                return true;
            }
            foreach (var entity in _whereClause.UsedEntities)
                entity.AssignResult(rowResult, obj);
            if (!_whereClause.PassesFilterPre(rowResult))
                return false;
            foreach (var entity in _whereClause.UnusedEntities)
                entity.AssignResult(rowResult, obj);
            return true;
        }

        public bool PassesFilterPost(object[] rowResult)
        {
            return _whereClause == null || _whereClause.PassesFilterPost(rowResult);
        }

        public IEnumerable GetIEnumerable(object obj)
        {
            return (IEnumerable)FieldInfo.GetValue(obj);
        }

        public void AssignResultPost(object[] result, object obj)
        {
            foreach (var entity in _aggregatedFormulas)
                entity.AssignResult(result, obj);
        }

        public IEnumerable<EntityClass> AllEntityClasses()
        {
            yield return this;
            foreach (var child in Lists.SelectMany(_ => _.AllEntityClasses()))
                yield return child;
        }

    }

}
