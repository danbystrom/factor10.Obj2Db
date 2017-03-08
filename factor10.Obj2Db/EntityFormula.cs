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

        public override void AssignResult(object[] result, object obj)
        {
            var itm = Evaluator.Eval(result);
            result[ResultSetIndex] = FieldType == typeof(double)
                ? (object) itm.Numeric
                : itm.String;
        }

        public override void ParentInitialized(EntityClass parent, int index)
        {
            var fieldsNameAndTypes = parent.Fields.Select(_ => _.NameAndType).ToList();
            fieldsNameAndTypes.Add(new NameAndType("#index", typeof(int)));
            Evaluator = new EvaluateRpn(new Rpn(Spec.formula), fieldsNameAndTypes);
            FieldType = Evaluator.ResultingType is RpnItemOperandNumeric
                ? typeof(double)
                : typeof(string);
        }
    }

}
