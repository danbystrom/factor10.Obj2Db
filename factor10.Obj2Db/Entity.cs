using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using factor10.Obj2Db.Formula;

namespace factor10.Obj2Db
{
    public enum TypeOfEntity
    {
        Class,
        PlainField,
        Aggregation,
        Formula
    }


    public class Entity
    {
        public virtual void ParentCompleted(Entity parent, int index)
        { }

        public entitySpec Spec { get; private set; }

        public string Name => Spec.name;
        public LinkedFieldInfo FieldInfo { get; protected set; }
        public TypeOfEntity TypeOfEntity { get; set; }

        public readonly List<Entity> Fields = new List<Entity>();
        protected Entity[] _plainFields;
        protected Entity[] _formulaFields;

        public string ExternalName => Spec.externalname ?? Name?.Replace(".", "");

        public int SaveableFieldCount { get; protected set; }
        public List<EntityClass> Lists { get; } = new List<EntityClass>();

        public readonly bool NoSave;
        public int ResultSetIndex { get; set; }

        public Type FieldType { get; set; }

        public readonly List<Tuple<int, int>> AggregationMapper = new List<Tuple<int, int>>();

        public NameAndType NameAndType => new NameAndType(Name, FieldType);

        public Entity(string name, LinkedFieldInfo fieldInfo)
        {
            FieldInfo = fieldInfo;
            FieldType = fieldInfo.FieldType;
            Spec = entitySpec.Begin(name ?? FieldType.Name);
            TypeOfEntity = TypeOfEntity.PlainField;
        }

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
            if (fieldInfo.IEnumerable != null)
                return new EntityClass(entitySpec, fieldInfo.IEnumerable.GetGenericArguments()[0], fieldInfo);
            if (entitySpec.fields == null || !entitySpec.fields.Any())
                return new EntityPlainField(entitySpec, fieldInfo);

            throw new Exception("Unknown error");
        }


        public object[] GetRow(object obj)
        {
            if (TypeOfEntity == TypeOfEntity.PlainField)
                return new[] {FieldInfo.GetValue(obj)};
            var result = new object[Fields.Count];
            foreach (var entity in _plainFields)
                entity.AssignValue(result, obj);
            foreach (var entity in _formulaFields)
                entity.AssignValue(result, obj);
            return result;
        }

        public virtual void AssignValue(object[] result, object obj)
        {
            // not quite right
            result[ResultSetIndex] = FieldInfo.GetValue(obj);
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