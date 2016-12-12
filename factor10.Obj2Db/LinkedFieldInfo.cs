using System;
using System.Reflection;

namespace factor10.Obj2Db
{
    public class LinkedFieldInfo
    {
        public readonly FieldInfo FieldInfo;
        public readonly LinkedFieldInfo Next;

        public Type FieldType { get; private set; }

        public LinkedFieldInfo(Type type, string name)
        {
            var split = name.Split(".".ToCharArray(), 2);
            FieldInfo = type.GetField(split[0], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (FieldInfo == null)
                throw new ArgumentException($"Field '{name}' not found in type '{type.Name}'");
            if (split.Length > 1)
                Next = new LinkedFieldInfo(FieldInfo.FieldType, split[1]);
            var x = this;
            while (x.Next != null)
                x = x.Next;
            FieldType = x.FieldInfo.FieldType;
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
            if (FieldInfo == null)
                return obj;
            var value = FieldInfo.GetValue(obj);
            return Next == null ? value : Next.GetValue(value);
        }

    }

}
