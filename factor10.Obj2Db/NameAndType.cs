using System;
using System.Reflection;

namespace factor10.Obj2Db
{
    public sealed class NameAndType
    {
        public readonly string Name;
        public readonly Type Type;

        public NameAndType(string name, Type type)
        {
            Name = name;
            Type = type;
        }

        public NameAndType(PropertyInfo propertyInfo)
            : this(propertyInfo.Name, propertyInfo.PropertyType)
        {
        }

        public NameAndType(FieldInfo fieldInfo)
            : this(fieldInfo.Name, fieldInfo.FieldType)
        {
        }

    }

}
