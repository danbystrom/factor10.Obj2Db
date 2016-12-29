using System.Linq;
using factor10.Obj2Db.Formula;
using NUnit.Framework;

namespace factor10.Obj2Db.Tests.Formula
{
    [TestFixture]
    public class RpnTests
    {
        [Test]
        public void TestPlus()
        {
            var rpn = new Rpn("3+4");
            Assert.AreEqual("3 4 +", rpn.ToString());
        }

        [Test]
        public void TestMinus()
        {
            var rpn = new Rpn("3 - 4");
            Assert.AreEqual("3 4 -", rpn.ToString());
        }

        [Test]
        public void TestMultiplication()
        {
            var rpn = new Rpn(" 3*4 ");
            Assert.AreEqual("3 4 *", rpn.ToString());
        }

        [Test]
        public void TestDivision()
        {
            var rpn = new Rpn("3/4");
            Assert.AreEqual("3 4 /", rpn.ToString());
        }

        [Test]
        public void TestPlusMany()
        {
            var rpn = new Rpn("3+4+5+6");
            Assert.AreEqual("3 4 + 5 + 6 +", rpn.ToString());
        }

        [Test]
        public void TestMulPlus()
        {
            var rpn = new Rpn("3*4+5");
            Assert.AreEqual("3 4 * 5 +", rpn.ToString());
        }

        [Test]
        public void TestPlusMul()
        {
            var rpn = new Rpn("3+4*5");
            Assert.AreEqual("3 4 5 * +", rpn.ToString());
        }

        [Test]
        public void TestDivMulDiv()
        {
            var rpn = new Rpn("3/4*5/6");
            Assert.AreEqual("3 4 / 5 6 / *", rpn.ToString());
        }

        [Test]
        public void TestAndOr1()
        {
            var rpn = new Rpn("a&b|c&d");
            Assert.AreEqual("a b & c d & |", rpn.ToString());
        }

        [Test]
        public void TestAndOr2()
        {
            var rpn = new Rpn("a&(b|c)&d");
            Assert.AreEqual("a b c | & d &", rpn.ToString());
        }

        [Test]
        public void TestParentesis1()
        {
            var rpn = new Rpn("(Y)");
            Assert.AreEqual("Y", rpn.ToString());
        }

        [Test]
        public void TestParentesis2()
        {
            var rpn = new Rpn("((Y))");
            Assert.AreEqual("Y", rpn.ToString());
        }

        [Test]
        public void TestParentesis3()
        {
            var rpn = new Rpn("(3+4)*(5+6)");
            Assert.AreEqual("3 4 + 5 6 + *", rpn.ToString());
        }

        [Test]
        public void TestParentesis4()
        {
            var rpn = new Rpn("(2+2)+3");
            Assert.AreEqual("2 2 + 3 +", rpn.ToString());
        }

        [Test]
        public void TestUnaryPlus()
        {
            var rpn = new Rpn("+1");
            Assert.AreEqual("1", rpn.ToString());
        }

        [Test]
        public void TestUnaryMinus()
        {
            var rpn = new Rpn("-1");
            Assert.AreEqual("1 -", rpn.ToString());
        }

        [Test]
        public void TestUnaryMinus2()
        {
            var rpn = new Rpn("--1");
            Assert.AreEqual("1 - -", rpn.ToString());
        }

        [Test]
        public void TestUnaryMinus3()
        {
            var rpn = new Rpn("-(-1)");
            Assert.AreEqual("1 - -", rpn.ToString());
        }

        [Test]
        public void TestFunction1()
        {
            var rpn = new Rpn("sin(pi)");
            Assert.AreEqual("pi sin(", rpn.ToString());
        }

        [Test]
        public void TestNot1()
        {
            var rpn = new Rpn("!true");
            Assert.AreEqual("true !", rpn.ToString());
        }

        [Test]
        public void TestNot2()
        {
            var rpn = new Rpn("!!true");
            Assert.AreEqual("true ! !", rpn.ToString());
        }

        [Test]
        public void TestNot3()
        {
            var rpn = new Rpn("!3+4");
            Assert.AreEqual("3 ! 4 +", rpn.ToString());
        }

        [Test]
        public void TestNot4()
        {
            var rpn = new Rpn("!(3+4)");
            Assert.AreEqual("3 4 + !", rpn.ToString());
        }

        [Test]
        public void TestNotFuncNot()
        {
            var rpn = new Rpn("!x(!y)");
            Assert.AreEqual("y ! x( !", rpn.ToString());
        }

        [Test]
        public void TestFuncWithManyArguments()
        {
            var rpn = new Rpn("threesum(3+4,sqrt(9),6)");
            Assert.AreEqual("3 4 + 9 sqrt( 6 threesum(", rpn.ToString());
            Assert.AreEqual(3, ((RpnItemFunction) rpn.Result.Last()).ArgumentCount);
        }

        [Test]
        public void TestEqualOp()
        {
            var rpn = new Rpn("2+5==3+7");
            Assert.AreEqual("2 5 + 3 7 + ==", rpn.ToString());
        }

        [Test]
        public void TestQuestionOp()
        {
            var rpn = new Rpn("i==2 ? nisse");
            Assert.AreEqual("i 2 == nisse ?", rpn.ToString());
        }

        [Test]
        public void TestStringConstant()
        {
            var rpn = new Rpn("len(\"nisse\")==5");
            Assert.AreEqual("\"nisse\" len( 5 ==", rpn.ToString());
        }
        
        [Test]
        public void TestJimmysExpression()
        {
            var rpn = new Rpn("first(i1==0 ? a1, i1==1 ? tail(a2,\",\"))");
            Assert.AreEqual("i1 0 == a1 ? i1 1 == a2 \",\" tail( ? first(", rpn.ToString());
        }

        [Test]
        public void TestFancyVariableName()
        {
            var rpn = new Rpn("0@a3$_+@");
            Assert.AreEqual("0@a3$_ @ +", rpn.ToString());
        }

        [Test]
        public void TestFancyVariableName2()
        {
            var rpn = new Rpn("#index+1");
            Assert.AreEqual("#index 1 +", rpn.ToString());
        }

    }

}
