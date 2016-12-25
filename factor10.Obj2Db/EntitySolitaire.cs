using System;

namespace factor10.Obj2Db
{
    public class EntitySolitaire : Entity
    {
        public EntitySolitaire(Type type)
            :base(entitySpec.Begin("value"))
        {
            FieldInfo = LinkedFieldInfo.Null(type);
            FieldType = FieldInfo.FieldType;
        }

        public override void AssignValue(object[] result, object obj)
        {
            result[ResultSetIndex] = FieldInfo.GetValue(obj);
        }

    }

}
