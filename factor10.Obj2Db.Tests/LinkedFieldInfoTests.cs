﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;

namespace factor10.Obj2Db.Tests
{
    [TestFixture]
    public class LinkedFieldInfoTests
    {
#pragma warning disable 649
        private class TestClassData
        {
            public double D;
            public int? I;
            public string P { get; set; }
            public double? NullableDoubleProperty { get; set; }
        }

        private struct TestStructData
        {
            public double D;
            public int? I;
            public string P { get; set; }
            public double? NullableDoubleProperty { get; set; }
        }

        private readonly LinkedFieldInfo lfiC1 = new LinkedFieldInfo(typeof(TestClassData), "D");
        private readonly LinkedFieldInfo lfiC2 = new LinkedFieldInfo(typeof(TestClassData), "I");
        private readonly LinkedFieldInfo lfiC3 = new LinkedFieldInfo(typeof(TestClassData), "P");
        private readonly LinkedFieldInfo lfiC4 = new LinkedFieldInfo(typeof(TestClassData), "NullableDoubleProperty");

        private readonly LinkedFieldInfo lfiS1 = new LinkedFieldInfo(typeof(TestStructData), "D");
        private readonly LinkedFieldInfo lfiS2 = new LinkedFieldInfo(typeof(TestStructData), "I");
        private readonly LinkedFieldInfo lfiS3 = new LinkedFieldInfo(typeof(TestStructData), "P");
        private readonly LinkedFieldInfo lfiS4 = new LinkedFieldInfo(typeof(TestStructData), "NullableDoubleProperty");

        [Test]
        public void TestCohersionViaClass()
        {
            var result1 = lfiC1.CoherseType(42.1);
            Assert.AreEqual("Double", result1.GetType().Name);
            Assert.AreEqual(42.1, (double) result1, 1E-10);

            var result2 = lfiC2.CoherseType(41.8);
            Assert.AreEqual("Int32", result2.GetType().Name);
            Assert.AreEqual(42, result2);

            var result3 = lfiC3.CoherseType(41.2);
            Assert.AreEqual("String", result3.GetType().Name);
            Assert.AreEqual("41.2", result3);

            var result4 = lfiC4.CoherseType("67.7");
            Assert.AreEqual("Double", result4.GetType().Name);
            Assert.AreEqual(67.7, (double) result4, 1E-10);
        }

        [Test]
        public void TestCohersionViaFromStruct()
        {
            var result1 = lfiS1.CoherseType(42.1);
            Assert.AreEqual("Double", result1.GetType().Name);
            Assert.AreEqual(42.1, (double) result1, 1E-10);

            var result2 = lfiS2.CoherseType(41.8);
            Assert.AreEqual("Int32", result2.GetType().Name);
            Assert.AreEqual(42, result2);

            var result3 = lfiS3.CoherseType(41.2);
            Assert.AreEqual("String", result3.GetType().Name);
            Assert.AreEqual("41.2", result3);

            var result4 = lfiS4.CoherseType("67.7");
            Assert.AreEqual("Double", result4.GetType().Name);
            Assert.AreEqual(67.7, (double) result4, 1E-10);
        }

        [Test, Explicit]
        public void Performance()
        {
            var tc = new TestClassData();
            var lfi1 = new LinkedFieldInfo(tc.GetType(), "D");
            for (var i = 0; i < 10; i++)
            {
                var d = 0.0;
                var sw = Stopwatch.StartNew();
                for (var j = 0; j < 1000000; j++)
                    d += (double) lfi1.GetValue(tc);
                Console.Write(sw.ElapsedMilliseconds + "  ");
                sw.Restart();
                for (var j = 0; j < 1000000; j++)
                    d += (double) lfi1.GetValueSlower(tc);
                Console.WriteLine(sw.ElapsedMilliseconds.ToString());
                Console.WriteLine();
            }
        }

        [Test]
        public void TestThatAtNameReturnsTheObjectItself()
        {
            var lfi = new LinkedFieldInfo(typeof(TestClassData), "@");
            var x = new TestClassData();
            Assert.IsTrue(ReferenceEquals(x, lfi.GetValue(x)));
            Assert.IsNull(lfi.IEnumerable);
            Assert.AreEqual(typeof(TestClassData), lfi.FieldType);
        }

        [Test]
        public void TestThatAtNameReturnsTheObjectItself2()
        {
            var lfi = new LinkedFieldInfo(typeof(List<int>), "@");
            var x = typeof(List<int>);
            Assert.IsTrue(ReferenceEquals(x, lfi.GetValue(x)));
            Assert.AreEqual(typeof(IEnumerable<int>), lfi.IEnumerable);
            Assert.AreEqual(typeof(List<int>), lfi.FieldType);
        }

    }

}
