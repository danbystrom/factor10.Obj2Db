﻿namespace factor10.Obj2Db
{
    public class EntityPlainField : Entity
    {
        public EntityPlainField(entitySpec entitySpec, LinkedFieldInfo linkedFieldInfo)
            : base(entitySpec)
        {
            FieldInfo = linkedFieldInfo;
            FieldType = FieldInfo.FieldType;
        }

        public override void AssignValue(object[] result, object obj)
        {
            result[ResultSetIndex] = FieldInfo.GetValue(obj);
        }

    }

}
