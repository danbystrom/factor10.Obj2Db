using System;
using System.Collections.Generic;
using System.Linq;

namespace factor10.Obj2Db
{

    public abstract class Entity
    {
        public readonly entitySpec Spec;

        public string Name => Spec.name;
        public string ExternalName => Spec.externalname ?? Name?.Replace(".", "");
        public LinkedFieldInfo FieldInfo { get; protected set; }

        public readonly List<Entity> Fields = new List<Entity>();

        public int SaveableFieldCount { get; protected set; }
        public List<EntityClass> Lists { get; } = new List<EntityClass>();

        public readonly bool NoSave;
        public int ResultSetIndex { get; set; }

        public Type FieldType { get; set; }

        // when an aggregation field is specified, this is set in the entity that holds the aggregated result (the subentity), 
        // so that the result value can be be aggregated up to the correct value in the parent
        public readonly List<Tuple<int, int>> AggregationMapper = new List<Tuple<int, int>>();

        public NameAndType NameAndType => new NameAndType(Name, FieldType);

        protected Entity(entitySpec entitySpec)
        {
            Spec = entitySpec;
            NoSave = entitySpec.nosave;
        }

        public static EntityClass Create(Type type, entitySpec entitySpec)
        {
            return new EntityClass(
                new entitySpec
                {
                    name = entitySpec.name ?? type.Name,
                    externalname = entitySpec.externalname,
                    fields = entitySpec.fields
                },
                type,
                null);
        }

        protected static Entity create(Type type, entitySpec entitySpec)
        {
            if (!string.IsNullOrEmpty(entitySpec.aggregation))
                return new EntityAggregation(entitySpec);
            if (!string.IsNullOrEmpty(entitySpec.formula))
                return new EntityFormula(entitySpec);

            var fieldInfo = new LinkedFieldInfo(type, entitySpec.name);
            if (fieldInfo.Enumerable != null)
                return new EntityClass(entitySpec, fieldInfo.Enumerable.GetGenericArguments()[0], fieldInfo);
            if (entitySpec.fields == null || !entitySpec.fields.Any())
                return new EntityPlainField(entitySpec, fieldInfo);

            throw new Exception("Unknown error");
        }

        public virtual void AssignValue(object[] result, object obj)
        {
        }

        public virtual void ParentInitialized(Entity parent, int index)
        {
        }

        public virtual bool PassesFilter(object[] rowResult)
        {
            return true;
        }

        public override string ToString()
        {
            return Name;
        }

    }


}