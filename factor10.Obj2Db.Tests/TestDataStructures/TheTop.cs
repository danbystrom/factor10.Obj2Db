using System;
using System.Collections.Generic;
using System.Text;

namespace factor10.Obj2Db.Tests.TestData
{
    public class TheTop
    {
        public string FirstName;
        public int Int;
        public double Double;

        public SomeStruct SomeStruct;

        public List<string> Strings;
        public List<SomeStruct> Structs;

        public List<TheTop> SelfList;
        public TheTop Self;
    }
}
