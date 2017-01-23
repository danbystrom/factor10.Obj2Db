using System;
using System.Collections.Generic;
using System.Linq;
using factor10.Obj2Db.Formula;

namespace factor10.Obj2Db
{
    public enum AggregationType
    {
        Sum,
        Count,
        Min,
        Max,
        Avg
    }

    public class EntityAggregation : Entity
    {
        public readonly AggregationType AggregationType;
        public int SourceIndex { get; private set; }

        private readonly EvaluateRpn _evaluator;

        public EntityAggregation(entitySpec entitySpec, Action<string> log)
            : base(entitySpec)
        {
            if (string.IsNullOrEmpty(entitySpec.aggregationtype))
                AggregationType = AggregationType.Sum;
            else
                AggregationType = (AggregationType) Enum.Parse(typeof(AggregationType), entitySpec.aggregationtype, true);
            if (string.IsNullOrEmpty(entitySpec.formula))
                return;
            _evaluator = new EvaluateRpn(new Rpn(entitySpec.formula), new List<NameAndType>
                {new NameAndType("@", typeof(double))});
        }

        public override void AssignValue(object[] result, object obj)
        {
            throw new NotImplementedException();
        }

        public override void ParentInitialized(EntityClass parent, int index)
        {
            var agg = Spec.aggregation;
            var siblingEntity = parent.Lists.FirstOrDefault(_ => agg.StartsWith(_.Name + "."));
            if (siblingEntity == null && AggregationType == AggregationType.Count)
            {
                agg += ".@";
                siblingEntity = parent.Lists.FirstOrDefault(_ => agg.StartsWith(_.Name + "."));
            }
            if (siblingEntity == null)
                throw new Exception($"Unable to find entity for aggregation '{Spec.aggregation}'");
            var subFieldName = agg.Substring(siblingEntity.Name.Length + 1);
            var subFieldIndex = siblingEntity.Fields.FindIndex(_ => (_.Name ?? "") == subFieldName);
            if (subFieldIndex < 0)
                throw new Exception();
            SourceIndex = subFieldIndex;
            siblingEntity.AggregationFields.Add(this);
            FieldType = AggregationType != AggregationType.Count ? siblingEntity.Fields[subFieldIndex].FieldType : typeof(int);
        }

        public object Evaluate(object obj)
        {
            return _evaluator?.Eval(new[] {obj}).Numeric ?? obj;
        }
    }

}
