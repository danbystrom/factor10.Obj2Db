using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using factor10.Obj2Db.Formula;

namespace factor10.Obj2Db
{
    public enum TypeOfEntity
    {
        Class,
        PlainField,
        Aggregation,
        Formula
    }

    public class Entity
    {
        public readonly EntitySpec Spec;

        public string Name => Spec.name;
        public readonly string ExternalName;
        public readonly string TypeName;
        public readonly LinkedFieldInfo FieldInfo;
        public readonly TypeOfEntity TypeOfEntity;

        public readonly List<Entity> Fields = new List<Entity>();
        private readonly Entity[] _plainFields;
        private readonly Entity[] _formulaFields;

        public readonly int SaveableFieldCount;
        public List<Entity> Lists { get; } = new List<Entity>();

        public readonly bool NoSave;
        public int ResultSetIndex { get; private set; }

        public Type FieldType { get; private set; }

        public readonly List<Tuple<int, int>> TemporaryAggregationMapper = new List<Tuple<int, int>>();

        private EvaluateRpn _evaluator;

        private Entity(string name, LinkedFieldInfo fieldInfo)
        {
            Spec = EntitySpec.Begin(name);
            ExternalName = name?.Replace(".", "");
            FieldInfo = fieldInfo;
            TypeOfEntity = TypeOfEntity.PlainField;
        }

        public Entity(Type type, EntitySpec entitySpec)
        {
            Spec = entitySpec;
            ExternalName = entitySpec.externalname ?? Name?.Replace(".", "");
            NoSave = entitySpec.nosave;

            if (!string.IsNullOrEmpty(Spec.aggregation))
                TypeOfEntity = TypeOfEntity.Aggregation;
            else if (!string.IsNullOrEmpty(Spec.formula))
                TypeOfEntity = TypeOfEntity.Formula;
            else if (Name == null)
                Spec = EntitySpec.Begin(type.Name);
            else
            {
                FieldInfo = new LinkedFieldInfo(type, Name);
                FieldType = FieldInfo.FieldType;
                if (FieldInfo.IEnumerable != null)
                {
                    type = FieldInfo.IEnumerable.GetGenericArguments()[0];
                    if (entitySpec.IsField || (entitySpec.Fields.First().name == "*" && !getAllFieldsAndProperties(type).Any()))
                        Fields.Add(new Entity(null, LinkedFieldInfo.Null(type)));
                }
                else if (entitySpec.IsField)
                    TypeOfEntity = TypeOfEntity.PlainField;
                else
                    throw new Exception("Unknown error");
            }

            TypeName = type.Name;

            breakDownSubEntities(type, entitySpec);

            // move the nosave fields to be at the end of the list - this feature is not completed and has no tests
            var noSaveFields = Fields.Where(_ => _.NoSave).ToList();
            Fields.RemoveAll(_ => _.NoSave);
            SaveableFieldCount = Fields.Count;
            Fields.AddRange(noSaveFields);

            // now it's time to connect the aggregated fields
            for (var fi = 0; fi < Fields.Count; fi++)
            {
                var field = Fields[fi];
                if (field.TypeOfEntity != TypeOfEntity.Aggregation)
                    continue;
                var agg = field.Spec.aggregation;
                var subEntity = Lists.FirstOrDefault(_ => agg.StartsWith(_.Name + "."));
                if (subEntity == null)
                    throw new Exception($"Unable to find subentity for aggregation '{agg}'");
                var subFieldName = agg.Substring(subEntity.Name.Length + 1);
                var subFieldIndex = subEntity.Fields.FindIndex(_ => (_.Name ?? "") == subFieldName);
                if (subFieldIndex < 0)
                    throw new Exception();
                subEntity.TemporaryAggregationMapper.Add(Tuple.Create(subFieldIndex, fi));
                field.FieldType = subEntity.Fields[subFieldIndex].FieldType;
            }

            var fieldInfosForEvaluator = Fields.Select(_ => Tuple.Create(_.Name, _.FieldType)).ToList();
            //...and to construct the evaluators
            foreach (var field in Fields.Where(field => field.TypeOfEntity == TypeOfEntity.Formula))
                field._evaluator = new EvaluateRpn(new Rpn(field.Spec.formula), fieldInfosForEvaluator);

            if (TypeOfEntity == TypeOfEntity.Class && !string.IsNullOrEmpty(Spec.where))
                _evaluator = new EvaluateRpn(new Rpn(Spec.where), fieldInfosForEvaluator);

            for (var i = 0; i < Fields.Count; i++)
                Fields[i].ResultSetIndex = i;
            _plainFields = Fields.Where(_ => _.TypeOfEntity == TypeOfEntity.PlainField).ToArray();
            _formulaFields = Fields.Where(_ => _.TypeOfEntity == TypeOfEntity.Formula).ToArray();
        }

        private void breakDownSubEntities(Type type, EntitySpec entitySpec)
        {
            foreach (var subEntitySpec in entitySpec.Fields)
            {
                subEntitySpec.nosave |= NoSave; // propagate NoSave all the way down until we reach turtles
                foreach (var subEntity in expansionOverStar(type, subEntitySpec, new HashSet<Type>()))
                    (subEntity.TypeOfEntity == TypeOfEntity.Class ? Lists : Fields).Add(subEntity);
            }
        }

        private static IEnumerable<Entity> expansionOverStar(
            Type masterType, 
            EntitySpec subEntitySpec, 
            HashSet<Type> haltRecursion, 
            string prefix = "",
            Type subType = null)
        {
            if (subEntitySpec.name != "*")
            {
                yield return new Entity(masterType, subEntitySpec);
                yield break;
            }

            subType = subType ?? masterType;

            foreach (var nameAndType in getAllFieldsAndProperties(subType))
            {
                var spec = EntitySpec.Begin(prefix + nameAndType.Name);
                var subProperties = getAllFieldsAndProperties(nameAndType.Type);
                var ienumerableType = LinkedFieldInfo.CheckForIEnumerable(nameAndType.Type);
                if (ienumerableType != null || !subProperties.Any())
                {
                    if (haltRecursion.Contains(subType))
                        throw new Exception("Circular reference detected while processing inclusion of all fields ('*')");
                    haltRecursion.Add(subType);

                    if (ienumerableType != null)
                        spec.Add("*");
                    yield return new Entity(subType, spec);

                    haltRecursion.Remove(subType);
                }

                foreach (var liftedSubProperty in subProperties)
                {
                    var propName = $"{prefix}{nameAndType.Name}.{liftedSubProperty.Name}";
                    if (getAllFieldsAndProperties(liftedSubProperty.Type).Any())
                    {
                        foreach (var q in expansionOverStar(masterType, "*", haltRecursion, propName + ".", liftedSubProperty.Type))
                            yield return q;
                    }
                    else
                        yield return new Entity(masterType, propName);
                }
            }

        }

        private static List<NameAndType> getAllFieldsAndProperties(Type type)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(_ => !_.GetGetMethod().IsSpecialName);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance).Where(_ => !_.IsSpecialName);
            var list = new List<NameAndType>();
            list.AddRange(properties.Select(_ => new NameAndType(_)));
            list.AddRange(fields.Select(_ => new NameAndType(_)));
            return list;
        }

        public IEnumerable GetIEnumerable(object obj)
        {
            return (IEnumerable) FieldInfo.GetValue(obj);
        }

        public object[] GetRow(object obj)
        {
            if (TypeOfEntity == TypeOfEntity.PlainField)
                return new[] {FieldInfo.GetValue(obj)};
            var result = new object[Fields.Count];
            foreach (var entity in _plainFields)
                result[entity.ResultSetIndex] = entity.FieldInfo.GetValue(obj);
            foreach (var entity in _formulaFields)
                result[entity.ResultSetIndex] = entity._evaluator.Eval(result).Numeric;
            return result;
        }

        public bool PassesFilter(object[] rowResult)
        {
            if (TypeOfEntity != TypeOfEntity.Class || _evaluator == null)
                return true;
            return _evaluator.Eval(rowResult).Numeric > 0;
        }

        public override string ToString()
        {
            return Name;
        }

    }

}