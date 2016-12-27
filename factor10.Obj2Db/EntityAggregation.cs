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

    public class EntityAggregation : EntityFormula
    {
        public readonly AggregationType AggregationType;

        public EntityAggregation(entitySpec entitySpec, Action<string> log)
            : base(entitySpec, log)
        {
        }

        public override void AssignValue(object[] result, object obj)
        {
            if(Evaluator!=null)
                base.AssignValue(result, obj);
        }

        public override void ParentInitialized(Entity parent, int index)
        {
            var agg = Spec.aggregation;
            var subEntity = parent.Lists.FirstOrDefault(_ => agg.StartsWith(_.Name + "."));
            if (subEntity == null)
                throw new Exception($"Unable to find subentity for aggregation '{agg}'");
            var subFieldName = agg.Substring(subEntity.Name.Length + 1);
            var subFieldIndex = subEntity.Fields.FindIndex(_ => (_.Name ?? "") == subFieldName);
            if (subFieldIndex < 0)
                throw new Exception();
            subEntity.AggregationMapper.Add(Tuple.Create(subFieldIndex, index));
            FieldType = subEntity.Fields[subFieldIndex].FieldType;

            if (!string.IsNullOrEmpty(Spec.formula))
                base.ParentInitialized(parent, index);
        }

    }

}
