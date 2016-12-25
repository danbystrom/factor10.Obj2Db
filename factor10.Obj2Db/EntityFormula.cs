using System;
using System.Collections.Generic;
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
            TypeOfEntity = TypeOfEntity.Formula;
        }

        public override void AssignValue(object[] result, object obj)
        {
            result[ResultSetIndex] = _evaluator.Eval(result).Numeric;
        }

        public override void ParentCompleted(Entity parent, int index)
        {
            _evaluator = new EvaluateRpn(new Rpn(Spec.formula), parent.Fields.Select(_ => _.NameAndType).ToList());
            FieldType = _evaluator.TypeEval() is RpnItemOperandNumeric
                ? typeof(double)
                : typeof(string);
        }
    }

}
