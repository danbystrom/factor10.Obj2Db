using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace factor10.Obj2Db.Tests.TestData
{
    public enum AnEnum
    {
        Zero,
        One,
        Two    
    }

    public class AllPropertyTypes
    {
        public string TheString;
        public int TheInt32;
        public short TheInt16;
        public float TheFloat;
        public double TheDouble;
        public DateTime TheDateTime;
        public bool TheBool;
        public long TheInt64;
        public Guid TheId;
        public AnEnum TheEnum;
        public int? TheNullableInt;
    }
}
