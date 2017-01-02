using System;
using System.Collections.Generic;
using factor10.Obj2Db.Formula;
using NUnit.Framework;

namespace factor10.Obj2Db.Tests.Formula
{
    [TestFixture]
    public class EvaluatorTests
    {
        [Test]
        public void TestBasicAddition()
        {
            var rpn = new Rpn("3+4");
            var eval = new EvaluateRpn(rpn);
            var value = eval.Eval();
            Assert.AreEqual(7, value.Numeric);
        }

        [Test]
        public void TestSubtraction()
        {
            var rpn = new Rpn("100-10-10-10");
            var eval = new EvaluateRpn(rpn);
            var value = eval.Eval();
            Assert.AreEqual(70, value.Numeric);
        }

        [Test]
        public void TestDivision()
        {
            var rpn = new Rpn("3+4/2");
            var eval = new EvaluateRpn(rpn);
            var value = eval.Eval();
            Assert.AreEqual(5, value.Numeric);
        }

        [Test]
        public void TestDivision2()
        {
            var rpn = new Rpn("1000/10/5");
            var eval = new EvaluateRpn(rpn);
            var value = eval.Eval();
            Assert.AreEqual(20, value.Numeric);
        }

        [Test]
        public void TestDivision3()
        {
            var rpn = new Rpn("1000/10*2/10");
            var eval = new EvaluateRpn(rpn);
            var value = eval.Eval();
            Assert.AreEqual(20, value.Numeric);
        }

        [Test]
        public void TestStringAddition()
        {
            var rpn = new Rpn("\"3\"+\"3\"");
            var eval = new EvaluateRpn(rpn);
            var value = eval.Eval();
            Assert.AreEqual("33", value.String);
        }

        [Test]
        public void TestNullParsing()
        {
            var rpn = new Rpn("null");
            var eval = new EvaluateRpn(rpn);
            var value = eval.Eval();
            Assert.AreEqual(null, value.String);
        }

        [Test]
        public void TestLtParsing()
        {
            var rpn = new Rpn("5<2+3");
            var eval = new EvaluateRpn(rpn);
            var value = eval.Eval();
            Assert.AreEqual(0, value.Numeric);
        }

        [Test]
        public void TestEqLtParsing()
        {
            var rpn = new Rpn("5<=2+3");
            var eval = new EvaluateRpn(rpn);
            var value = eval.Eval();
            Assert.AreEqual(1, value.Numeric);
        }

        [Test]
        public void TestThatNullIsFalse()
        {
            var rpn = new Rpn("null?1");
            var eval = new EvaluateRpn(rpn);
            var value = eval.Eval();
            Assert.AreEqual(null, value.String);
        }

        [Test]
        public void TestOrderEvaluation()
        {
            var rpn = new Rpn("12/2/3");
            var eval = new EvaluateRpn(rpn);
            var value = eval.Eval();
            Assert.AreEqual(2, value.Numeric);
        }

        [Test]
        public void TestFirstFunction()
        {
            var rpn = new Rpn("(12)/(first(0?1,0?2,0?3,1?4,1?5))");
            var eval = new EvaluateRpn(rpn);
            var value = eval.Eval();
            Assert.AreEqual(3, value.Numeric);
        }

        [Test]
        public void TestEqualOp()
        {
            var rpn = new Rpn("84==85");
            var eval = new EvaluateRpn(rpn);
            var value = eval.Eval();
            Assert.AreEqual(0, value.Numeric);
        }

        public void TestNotEqualOp()
        {
            var rpn = new Rpn("84!=85");
            var eval = new EvaluateRpn(rpn);
            var value = eval.Eval();
            Assert.AreEqual(1, value.Numeric);
        }

        public void TestNotEqualOp2()
        {
            var rpn = new Rpn("85!=85");
            var eval = new EvaluateRpn(rpn);
            var value = eval.Eval();
            Assert.AreEqual(0, value.Numeric);
        }

        public void TestNotEqualOpStr()
        {
            var rpn = new Rpn("''hej'!='då'");
            var eval = new EvaluateRpn(rpn);
            var value = eval.Eval();
            Assert.AreEqual(1, value.Numeric);
        }

        [TestCase(43, 0)]
        [TestCase(42, 1)]
        public void TestEqualOp(double variableValue, double expected)
        {
            var rpn = new Rpn("Url==42");
            Assert.AreEqual("Url 42 ==", rpn.ToString());

            var eval = new EvaluateRpn(rpn, new List<NameAndType>
            {new NameAndType("Url", typeof (double))});
            var value = eval.Eval(new object[] { variableValue });
            Assert.AreEqual(expected, value.Numeric);
        }

        [TestCase("www", 0)]
        [TestCase("why", 1)]
        public void TestNotEqualOp2Str(string variableValue, double expected)
        {
            var rpn = new Rpn("Url!='www'");
            Assert.AreEqual("Url \"www\" !=", rpn.ToString());

            var eval = new EvaluateRpn(rpn, new List<NameAndType>
            {new NameAndType("Url", typeof (string))});
            var value = eval.Eval(new object[] { variableValue });
            Assert.AreEqual(expected, value.Numeric);
        }

        [Test]
        public void TestVariables()
        {
            var rpn = new Rpn("3+(a+q)/(2+2)+3");
            Assert.AreEqual("3 a q + 2 2 + / + 3 +", rpn.ToString());
            var eval = new EvaluateRpn(rpn, new List<NameAndType>
            {new NameAndType("a", typeof (double)), new NameAndType("q", typeof (double))});
            var variables = new object[] {3.0, 5.0};
            var value = eval.Eval(variables);
            Assert.AreEqual(8, value.Numeric);
        }

        [TestCase("a", "b", "a")]
        [TestCase(null, null, null)]
        [TestCase("c", null, "c")]
        [TestCase(null, "d", "d")]
        public void TestNullCoalescing(string var1, string var2, string expected)
        {
            var rpn = new Rpn("var1??var2");
            Assert.AreEqual("var1 var2 ??", rpn.ToString());

            var eval = new EvaluateRpn(rpn, new List<NameAndType>
            {new NameAndType("var1", typeof (string)),new NameAndType("var2", typeof (string))});
            var value = eval.Eval(new object[] { var1, var2 });
            Assert.AreEqual(expected, value.String);
        }

        [TestCase("a", "b", 0)]
        [TestCase(null, null, 1)]
        [TestCase("c", null, 0)]
        [TestCase(null, "d", 0)]
        [TestCase("nisse", "nisse", 1)]
        [TestCase(5, "5", 1)]
        [TestCase("5", 5, 1)]
        [TestCase(5, 5, 1)]
        public void TestEquality(object var1, object var2, int expected)
        {
            var rpn = new Rpn("var1==var2");
            Assert.AreEqual("var1 var2 ==", rpn.ToString());

            var eval = new EvaluateRpn(rpn, new List<NameAndType>
            {new NameAndType("var1", typeof (string)),new NameAndType("var2", typeof (string))});
            var value = eval.Eval(new object[] { var1, var2 });
            Assert.AreEqual(expected, value.Numeric);
        }

        [Test]
        public void TestThatStringIsAutomaticallyConvertedToNumber()
        {
            var rpn = new Rpn("var1*2");
            Assert.AreEqual("var1 2 *", rpn.ToString());

            var eval = new EvaluateRpn(rpn, new List<NameAndType>
                {new NameAndType("var1", typeof(string))});
            var value = eval.Eval(new object[] { -1 });
            Assert.AreEqual(-2, value.Numeric);
        }

        [Test]
        public void TestThatNullabelsWorks1()
        {
            var rpn = new Rpn("var1??-1");
            Assert.AreEqual("var1 -1 ??", rpn.ToString());

            var eval = new EvaluateRpn(rpn, new List<NameAndType>
                {new NameAndType("var1", typeof(double))});
            var value = eval.Eval(new object[] { null });
            Assert.AreEqual(-1, value.Numeric);
        }

        [Test]
        public void TestThatNullabelsWorks2()
        {
            var rpn = new Rpn("var1");
            Assert.AreEqual("var1", rpn.ToString());

            var eval = new EvaluateRpn(rpn, new List<NameAndType>
                {new NameAndType("var1", typeof(double))});
            var value = eval.Eval(new object[] { null });
            Assert.AreEqual(0, value.Numeric);
            Assert.IsTrue(value.IsNull);
        }

        //[TestCase(0, "kalle", "nisse", "kalle")]
        //[TestCase(0, null, "nisse", null)]
        //[TestCase(1, "kalle", "nisse", "")]
        //[TestCase(1, "kalle", "nisse,hult", "hult")]
        //public void TestJimmysExpression(int i1, string a1, string a2, string expected)
        //{
        //    var rpn = new Rpn("first(i1==0 ? a1, i1==1 ? tail(a2,\",\"))");
        //    var x = rpn.ToString();
        //    var eval = new EvaluateRpn(rpn);
        //    var value = eval.Eval(
        //        new Dictionary<string, double> {{"i1", i1}},
        //        new Dictionary<string, string> {{"a1", a1}, {"a2", a2}});
        //    Assert.AreEqual(expected, value.String);
        //}

    }

}
