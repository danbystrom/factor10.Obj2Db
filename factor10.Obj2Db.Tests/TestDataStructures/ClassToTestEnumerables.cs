using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace factor10.Obj2Db.Tests.TestDataStructures
{
    public class ClassToTestEnumerables
    {
        public IEnumerable<int> Case1;
        public int[] Case2;
        public List<int> Case3;
        public IEnumerable<int> Case4 { get; set; }
        public int[] Case5 { get; set; }
        public IList<int> Case6 { get; set; }

        public ClassToTestEnumerables DeeperF;
        public ClassToTestEnumerables DeeperP { get; set; }
    }
}
