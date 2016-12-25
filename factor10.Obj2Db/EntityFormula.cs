using System.Linq;
using factor10.Obj2Db.Formula;

namespace factor10.Obj2Db
{
    public class EntityFormula : Entity
    {
        private EvaluateRpn _evaluator;

        public EntityFormula(entitySpec entitySpec)
            : base(entitySpec)
        {
        }

        public override void AssignValue(object[] result, object obj)
        {
            var itm = _evaluator.Eval(result);
            result[ResultSetIndex] = FieldType == typeof(double)
                ? (object) itm.Numeric
                : itm.String;
        }

        public override void ParentInitialized(Entity parent, int index)
        {
            _evaluator = new EvaluateRpn(new Rpn(Spec.formula), parent.Fields.Select(_ => _.NameAndType).ToList());
            FieldType = _evaluator.TypeEval() is RpnItemOperandNumeric
                ? typeof(double)
                : typeof(string);
        }
    }

}
