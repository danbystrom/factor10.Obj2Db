using System;
using System.Collections.Generic;
using System.Linq;
using factor10.Obj2Db.Formula;

namespace factor10.Obj2Db
{
    public class EntityFormula : Entity
    {
        protected EvaluateRpn Evaluator;

        private bool _isBasedOnAggregation;
        public override bool IsBasedOnAggregation => _isBasedOnAggregation;

        public EntityFormula(entitySpec entitySpec, Action<string> log)
            : base(entitySpec)
        {
            log?.Invoke($"EntityFormula ctor: {entitySpec.name}/{entitySpec.fields?.Count ?? 0} - {entitySpec.formula} ");
        }

        public override void AssignResult(object[] result, object obj)
        {
            var itm = Evaluator.Eval(result);
            result[ResultSetIndex] = FieldType == typeof(double)
                ? (object) itm.Numeric
                : itm.String;
        }

        public override void ParentInitialized(EntityClass parent, int index)
        {
            Evaluator = CreateEvaluator(Spec.formula, parent.Fields);
            FieldType = Evaluator.ResultingType is RpnItemOperandNumeric
                ? typeof(double)
                : typeof(string);
            _isBasedOnAggregation = IsEvaluatorBasedOnAggregation(Evaluator, parent.Fields); //Evaluator.GetVariableIndexes().Any(_ => _ < parent.Fields.Count && parent.Fields[_].IsBasedOnAggregation);
        }

        public static EvaluateRpn CreateEvaluator(string formula, List<Entity> fields)
        {
            var fieldsNameAndTypes = fields.Select(_ => _.NameAndType).ToList();
            fieldsNameAndTypes.Add(new NameAndType("#index", typeof(int)));
            return new EvaluateRpn(new Rpn(formula), fieldsNameAndTypes);
        }

        public static bool IsEvaluatorBasedOnAggregation(EvaluateRpn evaluator, List<Entity> fields)
        {
            return evaluator.GetVariableIndexes().Any(_ => _ < fields.Count && fields[_].IsBasedOnAggregation);
        }

    }

}
