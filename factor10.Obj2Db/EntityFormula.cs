using System;
using System.Linq;
using factor10.Obj2Db.Formula;

namespace factor10.Obj2Db
{
    public class EntityFormula : Entity
    {
        protected EvaluateRpn Evaluator;

        public EntityFormula(entitySpec entitySpec, Action<string> log)
            : base(entitySpec)
        {
            log?.Invoke($"EntityFormula ctor: {entitySpec.name}/{entitySpec.fields?.Count ?? 0} - {entitySpec.formula} ");
        }

        public override void AssignValue(object[] result, object obj)
        {
            var itm = Evaluator.Eval(result);
            result[ResultSetIndex] = FieldType == typeof(double)
                ? (object) itm.Numeric
                : itm.String;
        }

        public override void ParentInitialized(Entity parent, int index)
        {
            Evaluator = new EvaluateRpn(new Rpn(Spec.formula), parent.Fields.Select(_ => _.NameAndType).ToList());
            FieldType = Evaluator.TypeEval() is RpnItemOperandNumeric
                ? typeof(double)
                : typeof(string);
        }
    }

}
