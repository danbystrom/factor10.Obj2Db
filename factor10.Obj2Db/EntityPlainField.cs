using System;

namespace factor10.Obj2Db
{
    public class EntityPlainField : Entity
    {
        public EntityPlainField(entitySpec entitySpec, LinkedFieldInfo fieldInfo, Action<string> log)
            : base(entitySpec)
        {
            log?.Invoke($"EntityPlainField ctor: {entitySpec.name}/{entitySpec.fields?.Count ?? 0} - {fieldInfo.FieldType} ");

            FieldInfo = fieldInfo;
            FieldType = FieldInfo.FieldType;
        }

        public override void AssignResult(object[] result, object obj)
        {
            result[ResultSetIndex] = FieldInfo.GetValue(obj);
        }

    }

}
