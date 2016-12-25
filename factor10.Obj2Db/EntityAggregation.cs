using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace factor10.Obj2Db
{
    public class EntityAggregation : Entity
    {
        public EntityAggregation(entitySpec entitySpec)
            : base(entitySpec)
        {
            TypeOfEntity = TypeOfEntity.Aggregation;
        }

        public override void ParentCompleted(Entity parent, int index)
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
        }

    }

}
