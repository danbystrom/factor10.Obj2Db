using System;
using System.Linq;

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

        public EntityAggregation(entitySpec entitySpec, Action<string> log)
            : base(entitySpec)
        {
            if (string.IsNullOrEmpty(entitySpec.aggregationtype))
                AggregationType = AggregationType.Sum;
            else
                AggregationType = (AggregationType) Enum.Parse(typeof(AggregationType), entitySpec.aggregationtype, true);
        }

        public override void AssignValue(object[] result, object obj)
        {
            //if(Evaluator!=null)
            //    base.AssignValue(result, obj);
        }

        public override void ParentInitialized(Entity parent, int index)
        {
            var agg = Spec.aggregation;
            var siblingEntity = parent.Lists.FirstOrDefault(_ => agg.StartsWith(_.Name + "."));
            if (siblingEntity == null)
                throw new Exception($"Unable to find entity for aggregation '{agg}'");
            var subFieldName = agg.Substring(siblingEntity.Name.Length + 1);
            var subFieldIndex = siblingEntity.Fields.FindIndex(_ => (_.Name ?? "") == subFieldName);
            if (subFieldIndex < 0)
                throw new Exception();
            SourceIndex = subFieldIndex;
            siblingEntity.AggregationFields.Add(this);
            FieldType = siblingEntity.Fields[subFieldIndex].FieldType;

            //if (!string.IsNullOrEmpty(Spec.formula))
            //    base.ParentInitialized(parent, index);
        }

    }

}
