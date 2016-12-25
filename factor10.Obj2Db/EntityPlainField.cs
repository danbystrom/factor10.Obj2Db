using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace factor10.Obj2Db
{
    public class EntityPlainField : Entity
    {
        public EntityPlainField(entitySpec entitySpec, LinkedFieldInfo linkedFieldInfo)
            : base(entitySpec)
        {
            TypeOfEntity = TypeOfEntity.PlainField;
            FieldInfo = linkedFieldInfo;
            FieldType = FieldInfo.FieldType;
        }

        public override void AssignValue(object[] result, object obj)
        {
            result[ResultSetIndex] = FieldInfo.GetValue(obj);
        }

    }

}
