using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace factor10.Obj2Db
{
    public class LinkedFieldInfo
    {
        private readonly FieldInfo _fieldInfo;
        private readonly PropertyInfo _propertyInfo;
        public readonly LinkedFieldInfo Next;

        public Type FieldType { get; private set; }

        public LinkedFieldInfo(Type type, string name)
        {
            var split = name.Split(".".ToCharArray(), 2);
            _fieldInfo = type.GetField(split[0], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (_fieldInfo == null)
            {
                _propertyInfo = type.GetProperty(split[0], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (_propertyInfo == null)
                    throw new ArgumentException($"Field or property '{name}' not found in type '{type.Name}'");
                if (split.Length > 1)
                    Next = new LinkedFieldInfo(_propertyInfo.PropertyType, split[1]);
            }
            else if (split.Length > 1)
                Next = new LinkedFieldInfo(_fieldInfo.FieldType, split[1]);
            var x = this;
            while (x.Next != null)
                x = x.Next;
            FieldType = x._fieldInfo?.FieldType ?? x._propertyInfo.PropertyType;
        }

        private LinkedFieldInfo()
        {
        }

        public static LinkedFieldInfo Null(Type type)
        {
            return new LinkedFieldInfo {FieldType = type};
        }

        public object GetValue(object obj)
        {
            if (obj == null)
                return null;
            object value;
            if (_fieldInfo != null)
                value = _fieldInfo.GetValue(obj);
            else if (_propertyInfo != null)
                value = _propertyInfo.GetValue(obj);
            else
                return obj;
            return Next == null ? value : Next.GetValue(value);
        }

        public Type CheckForIEnumerable()
        {
            // this need to be checked for nested properties - pretty sure it won't work!!!
            var ft = _fieldInfo?.FieldType ?? _propertyInfo.PropertyType;
            return ft != typeof (string) // a string implements IEnumerable<Char> - but forget that
                ? ft.GetInterfaces().SingleOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof (IEnumerable<>))
                : null;
        }

    }

}
