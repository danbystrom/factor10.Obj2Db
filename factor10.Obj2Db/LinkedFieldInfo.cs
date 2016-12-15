using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace factor10.Obj2Db
{
    public class LinkedFieldInfo
    {
        private static Dictionary<string, Func<IConvertible, object>> _cohersions = new Dictionary<string, Func<IConvertible, object>>
        {
            {"Int32", _ => _.ToInt32(null)},
            {"Int64", _ => _.ToInt64(null)},
            {"Int16", _ => _.ToInt16(null)},
            {"Decimal", _ => _.ToDecimal(null)},
            {"DateTime", _ => _.ToDateTime(null)},
            {"Double", _ => _.ToDouble(null)},
            {"Single", _ => _.ToSingle(null)},
            {"String", _ => _.ToString(null)},
            {"Boolean", _ => _.ToBoolean(null)},
            {"Guid", _ => _}
        };

        private readonly FieldInfo _fieldInfo;
        private readonly PropertyInfo _propertyInfo;
        public readonly LinkedFieldInfo Next;
        public readonly Type IEnumerable;

        public Type FieldType { get; private set; }
        public Type FieldTypeOrInnerIfNullable { get; private set; }

        private readonly Func<IConvertible, object> _coherse;

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
            IEnumerable = checkForIEnumerable();
            FieldTypeOrInnerIfNullable = FieldType.IsGenericType && FieldType.GetGenericTypeDefinition() == typeof(Nullable<>)
                ? Nullable.GetUnderlyingType(FieldType)
                : FieldType;
            _cohersions.TryGetValue(FieldTypeOrInnerIfNullable.Name, out _coherse);
        }

        private Type checkForIEnumerable()
        {
            if (FieldType.IsGenericType && FieldType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return FieldType;
            return FieldType != typeof(string) // a string implements IEnumerable<Char> - but forget that
                ? FieldType.GetInterfaces().SingleOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                : null;
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

        public object CoherseType(object obj)
        {
            var iconvertible = obj as IConvertible;
            return iconvertible != null && _coherse != null ? _coherse(iconvertible) : obj;
        }

    }

}
