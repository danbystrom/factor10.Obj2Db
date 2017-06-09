using System;
using System.Collections.Generic;

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
        public Type FieldType { get; protected set; }

        public readonly HashSet<int> ReliesOnIndexes = new HashSet<int>();

        public NameAndType NameAndType => new NameAndType(Name, FieldType);

        protected Entity(entitySpec entitySpec)
        {
            Spec = entitySpec;
            NoSave = entitySpec.nosave;
        }

        public static EntityClass Create(EntityClass parent, entitySpec entitySpec, Type type, Action<string> log, bool throwOnCircularReference = true)
        {
            return new EntityClass(
                parent,
                new entitySpec
                {
                    name = entitySpec.name ?? type.Name,
                    externalname = entitySpec.externalname,
                    fields = entitySpec.fields,
                    where = entitySpec.where
                },
                type,
                null,
                log,
                throwOnCircularReference);
        }

        protected static Entity create(EntityClass parent, entitySpec entitySpec, Type type, Action<string> log, bool throwOnCircularReference)
        {
            log?.Invoke($"create: {type.Name} - {entitySpec.name}");

            if (!string.IsNullOrEmpty(entitySpec.aggregation))
                return new EntityAggregation(entitySpec, log);
            if (!string.IsNullOrEmpty(entitySpec.formula))
                return new EntityFormula(entitySpec, log);

            var fieldInfo = new LinkedFieldInfo(type, entitySpec.name);
            if (fieldInfo.IEnumerable != null)
                return new EntityClass(parent, entitySpec, fieldInfo.IEnumerable.GetGenericArguments()[0], fieldInfo, log, throwOnCircularReference);
            if (!entitySpec.AnyNotStar())
                return new EntityPlainField(entitySpec, fieldInfo, log);

            throw new Exception("Unknown error");
        }

        public virtual void AssignResult(object[] result, object obj)
        {
        }

        public virtual void ParentInitialized(EntityClass parent, int index)
        {
        }

        //public virtual bool PassesFilter(object[] rowResult)
        //{
        //    return true;
        //}

        public override string ToString()
        {
            return Name;
        }

        public object CoherseType(object obj)
        {
            return FieldInfo != null
                ? FieldInfo.CoherseType(obj)
                : LinkedFieldInfo.CoherseType(FieldType, obj);
        }

        public virtual bool IsBasedOnAggregation => false;

    }


}