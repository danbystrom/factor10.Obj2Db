using System;
using System.Collections.Generic;
using factor10.Obj2Db.Formula;
using NUnit.Framework;

namespace factor10.Obj2Db.Tests.Formula
{
    [TestFixture]
    public class CompiledEvaluatorTests
    {
        [Test]
        public void TestBasicAddition()
        {
            var rpn = new Rpn("3+4");
            var eval = new CompileRpn(rpn);
            var value = eval.Evaluate(null);
            Assert.AreEqual(7, value);
        }

        [Test]
        public void TestSubtraction()
        {
            var rpn = new Rpn("100-10-10-10");
            var eval = new CompileRpn(rpn);
            var value = eval.Evaluate(null);
            Assert.AreEqual(70, value);
        }

        [Test]
        public void TestDivision()
        {
            var rpn = new Rpn("3+4/2");
            var eval = new CompileRpn(rpn);
            var value = eval.Evaluate(null);
            Assert.AreEqual(5, value);
        }

        [Test]
        public void TestDivision2()
        {
            var rpn = new Rpn("1000/10/5");
            var eval = new CompileRpn(rpn);
            var value = eval.Evaluate(null);
            Assert.AreEqual(20, value);
        }

        [Test]
        public void TestDivision3()
        {
            var rpn = new Rpn("1000/10*2/10");
            var eval = new CompileRpn(rpn);
            var value = eval.Evaluate(null);
            Assert.AreEqual(20, value);
        }

        //[Test]
        //public void TestStringAddition()
        //{
        //    var rpn = new Rpn("\"3\"+\"3\"");
        //    var eval = new CompileRpn(rpn);
        //    var value = eval.Evaluate(null);
        //    Assert.AreEqual("33", value.String);
        //}

        //[Test]
        //public void TestNullParsing()
        //{
        //    var rpn = new Rpn("null");
        //    var eval = new CompileRpn(rpn);
        //    var value = eval.Evaluate(null);
        //    Assert.AreEqual(null, value.String);
        //}

        [Test]
        public void TestLtParsing()
        {
            var rpn = new Rpn("5<2+3");
            var eval = new CompileRpn(rpn);
            var value = eval.Evaluate(null);
            Assert.AreEqual(0, value);
        }

        [Test]
        public void TestEqLtParsing()
        {
            var rpn = new Rpn("5<=2+3");
            var eval = new CompileRpn(rpn);
            var value = eval.Evaluate(null);
            Assert.AreEqual(1, value);
        }

        //[Test]
        //public void TestThatNullIsFalse()
        //{
        //    var rpn = new Rpn("null?1");
        //    var eval = new CompileRpn(rpn);
        //    var value = eval.Evaluate(null);
        //    Assert.AreEqual(null, value.String);
        //}

        [Test]
        public void TestOrderEvaluation()
        {
            var rpn = new Rpn("12/2/3");
            var eval = new CompileRpn(rpn);
            var value = eval.Evaluate(null);
            Assert.AreEqual(2, value);
        }

        //[Test]
        //public void TestFirstFunction()
        //{
        //    var rpn = new Rpn("(12)/(first(0?1,0?2,0?3,1?4,1?5))");
        //    var eval = new CompileRpn(rpn);
        //    var value = eval.Evaluate(null);
        //    Assert.AreEqual(3, value);
        //}

        public void TestNotEqualOp()
        {
            var rpn = new Rpn("84!=85");
            var eval = new CompileRpn(rpn);
            var value = eval.Evaluate(null);
            Assert.AreEqual(1, value);
        }

        public void TestNotEqualOp2()
        {
            var rpn = new Rpn("85!=85");
            var eval = new CompileRpn(rpn);
            var value = eval.Evaluate(null);
            Assert.AreEqual(0, value);
        }

        //public void TestNotEqualOpStr()
        //{
        //    var rpn = new Rpn("''hej'!='då'");
        //    var eval = new CompileRpn(rpn);
        //    var value = eval.Evaluate(null);
        //    Assert.AreEqual(1, value);
        //}

        [TestCase(43, 0)]
        [TestCase(42, 1)]
        public void TestEqualOp(double variableValue, double expected)
        {
            var rpn = new Rpn("Url==42");
            Assert.AreEqual("Url 42 ==", rpn.ToString());

            var eval = new CompileRpn(rpn, new List<NameAndType>
                {new NameAndType("Url", typeof(double))});
            var value = eval.Evaluate(new object[] { variableValue });
            Assert.AreEqual(expected, value);
        }

        //[TestCase("www", 0)]
        //[TestCase("why", 1)]
        //public void TestNotEqualOp2Str(string variableValue, double expected)
        //{
        //    var rpn = new Rpn("Url!='www'");
        //    Assert.AreEqual("Url \"www\" !=", rpn.ToString());

        //    var eval = new EvaluateRpn(rpn, new List<Tuple<string, Type>>
        //    {Tuple.Create("Url", typeof (string))});
        //    var value = eval.Eval(new object[] { variableValue });
        //    Assert.AreEqual(expected, value);
        //}

        [Test]
        public void TestVariables()
        {
            var rpn = new Rpn("3+(a+q)/(2+2)+3");
            Assert.AreEqual("3 a q + 2 2 + / + 3 +", rpn.ToString());
            var eval = new CompileRpn(rpn, new List<NameAndType>
            {new NameAndType("a", typeof (double)), new NameAndType("q", typeof (double))});
            var variables = new object[] {3.0, 5.0};
            var value = eval.Evaluate(variables);
            Assert.AreEqual(8, value);
        }

        //[TestCase(0, "kalle", "nisse", "kalle")]
        //[TestCase(0, null, "nisse", null)]
        //[TestCase(1, "kalle", "nisse", "")]
        //[TestCase(1, "kalle", "nisse,hult", "hult")]
        //public void TestJimmysExpression(int i1, string a1, string a2, string expected)
        //{
        //    var rpn = new Rpn("first(i1==0 ? a1, i1==1 ? tail(a2,\",\"))");
        //    var x = rpn.ToString();
        //    var eval = new CompileRpn(rpn);
        //    var value = eval.Eval(
        //        new Dictionary<string, double> {{"i1", i1}},
        //        new Dictionary<string, string> {{"a1", a1}, {"a2", a2}});
        //    Assert.AreEqual(expected, value.String);
        //}

    }

}
