using System.Collections.Generic;
using factor10.Obj2Db.Tests.TestData;

namespace factor10.Obj2Db.Tests.TestDataStructures
{
    public class SneakyList : List<string>
    {
        public int NonObviousProperty;
    }

    public class TestClassWithSneakyStuff
    {
        public SomeStruct TheStruct;
        public SneakyList TheList;
        public Dictionary<int, int> DictionariesAreSneaky;
        public int[] ArraysAreSneaky;
    }

}
