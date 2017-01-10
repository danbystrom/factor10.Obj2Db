using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace factor10.Obj2Db
{
    public sealed class LinkedFieldInfo
    {
        private static readonly Dictionary<string, Func<IConvertible, object>> Cohersions = new Dictionary<string, Func<IConvertible, object>>
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

        private readonly Func<IConvertible, object> _coherse;

        private readonly Func<object, object> _getValue;

        public LinkedFieldInfo(Type type, string name)
        {
            if (name == "@")
            {
                // auto-referencing
                FieldType = type;
                _getValue = _ => _;
            }
            else
            {
                var split = name.Split(".".ToCharArray(), 2);
                _fieldInfo = type.GetField(split[0], BindingFlags.Public | BindingFlags.Instance);
                if (_fieldInfo == null)
                {
                    _propertyInfo = type.GetProperty(split[0], BindingFlags.Public | BindingFlags.Instance);
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

                if (_propertyInfo != null)
                    _getValue = generateFastPropertyFetcher(type, _propertyInfo);
                else if (_fieldInfo != null)
                    _getValue = generateFastFieldFetcher(type, _fieldInfo);
            }

            IEnumerable = CheckForIEnumerable(FieldType);
            Cohersions.TryGetValue(StripNullable(FieldType).Name, out _coherse);
        }

        public static Type StripNullable(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)
                ? Nullable.GetUnderlyingType(type)
                : type;
        }

        public static Type CheckForIEnumerable(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return type;
            return type != typeof(string) && type != typeof(byte[]) // a string implements IEnumerable<Char> - but forget that
                ? type.GetInterfaces().SingleOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                : null;
        }

        private Func<object, object> generateFastPropertyFetcher(Type type, PropertyInfo propertyInfo)
        {
            try
            {
                return generateFastFetcher(type, propertyInfo.PropertyType, il =>
                {
                    il.Emit(OpCodes.Callvirt, propertyInfo.GetGetMethod());
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw new Exception($"Could not generate propery fetcher for '{type.Name}'.{propertyInfo.Name}");
            }
        }

        private Func<object, object> generateFastFieldFetcher(Type type, FieldInfo fieldInfo)
        {
            try
            {
                return generateFastFetcher(type, fieldInfo.FieldType, il =>
                {
                    il.Emit(OpCodes.Ldfld, fieldInfo);
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw new Exception($"Could not generate field fetcher for '{type.Name}'.{fieldInfo.Name}");
            }
        }

        private Func<object, object> generateFastFetcher(Type sourceObjectType, Type resultType, Action<ILGenerator> ilGenAction)
        {
            var method = new DynamicMethod("", typeof(object), new[] {typeof(object)}, sourceObjectType, true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, sourceObjectType);
            if (sourceObjectType.IsValueType)
                il.Emit(OpCodes.Unbox, sourceObjectType);
            ilGenAction(il);
            if (resultType.IsValueType)
                il.Emit(OpCodes.Box, resultType);
            il.Emit(OpCodes.Ret);
            return (Func<object, object>) method.CreateDelegate(typeof(Func<object, object>));
        }

        public static LinkedFieldInfo Null(Type type)
        {
            return new LinkedFieldInfo(type, "@");
        }

        public object GetValueSlower(object obj)
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

        public object GetValue(object obj)
        {
            if (obj == null)
                return null;
            var value = _getValue(obj);
            return Next == null ? value : Next.GetValue(value);
        }

        public object CoherseType(object obj)
        {
            var iconvertible = obj as IConvertible;
            return iconvertible != null && _coherse != null ? _coherse(iconvertible) : obj;
        }

        public static object CoherseType(Type type, object obj)
        {
            var iconvertible = obj as IConvertible;
            Func<IConvertible, object> coherse;
            Cohersions.TryGetValue(StripNullable(type).Name, out coherse);
            return iconvertible != null && coherse != null ? coherse(iconvertible) : obj;
        }

        public static List<NameAndType> GetAllFieldsAndProperties(Type type)
        {
            var list = new List<NameAndType>();
            if (type == typeof(string) || type == typeof(DateTime) || type.IsArray ||
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)))
                return list;
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(_ => _.GetIndexParameters().Length == 0);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance).Where(_ => !_.IsSpecialName);
            list.AddRange(properties.Select(_ => new NameAndType(_)));
            list.AddRange(fields.Select(_ => new NameAndType(_)));
            for (; type != null && type != typeof(object); type = type.BaseType)
                if (type.IsGenericType)
                {
                    var genTypDef = type.GetGenericTypeDefinition();
                    if (genTypDef == typeof(List<>) || genTypDef == typeof(IList<>))
                        list.RemoveAll(_ => new[] {"Capacity", "Count"}.Contains(_.Name));
                    if (genTypDef == typeof(Dictionary<,>) || genTypDef == typeof(IDictionary<,>))
                        list.RemoveAll(_ => new[] {"Comparer", "Count", "Keys", "Values"}.Contains(_.Name));
                }
            return list;
        }

        public static string FriendlyTypeName(Type type)
        {
            if (type == null)
                return null;
            var innerType = StripNullable(type);
            return innerType == type
                ? type.Name
                : innerType.Name + "?";
        }

    }

}
