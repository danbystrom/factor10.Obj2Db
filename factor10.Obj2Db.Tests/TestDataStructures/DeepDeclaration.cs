using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using factor10.Obj2Db.Tests.TestData;

namespace factor10.Obj2Db.Tests.TestDataStructures
{
    public class Level3
    {
        public SomeStruct Ss1;
        public SomeStruct Ss2;
    }

    public class Level2
    {
        public Level3 X1;
        public Level3 X2;
    }

    class DeepDeclaration
    {
        public Level2 TheFirst;
        public Level2 TheSecond;
    }
}
