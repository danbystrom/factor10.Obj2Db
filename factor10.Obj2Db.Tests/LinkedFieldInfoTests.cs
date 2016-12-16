using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using NUnit.Framework;

namespace factor10.Obj2Db.Tests
{
    [TestFixture]
    class LinkedFieldInfoTests
    {
        private class testClass
        {
            public double D;
            public int? I;
            public string P { get; set; }
        }

        private struct strut
        {
            public int Int;
        }

        [Test]
        public void TestCohersion()
        {
            var tc = new testClass();
            var lfi1 = new LinkedFieldInfo(tc.GetType(), "D");
            var lfi2 = new LinkedFieldInfo(tc.GetType(), "I");
            var lfi3 = new LinkedFieldInfo(tc.GetType(), "P");

            var result1 = lfi1.CoherseType(42);
            Assert.AreEqual("Double", result1.GetType().Name);

            var result2 = lfi2.CoherseType(41.2);
            Assert.AreEqual("Int32", result2.GetType().Name);

            var result3 = lfi3.CoherseType(41.2);
            Assert.AreEqual("String", result3.GetType().Name);

        }

        [Test, Explicit]
        public void Performance()
        {
            var tc = new testClass();
            var lfi1 = new LinkedFieldInfo(tc.GetType(), "D");
            for (var i = 0; i < 10; i++)
            {
                var d = 0.0;
                var sw = Stopwatch.StartNew();
                for (var j = 0; j < 1000000; j++)
                    d += (double) lfi1.GetValue(tc);
                Console.Write (sw.ElapsedMilliseconds.ToString() + "  ");
                sw.Restart();
                for (var j = 0; j < 1000000; j++)
                    d += (double)lfi1.GetValueSlower(tc);
                Console.WriteLine(sw.ElapsedMilliseconds.ToString());
                Console.WriteLine();
            }
        }

        [Test]
        public void Yopheidi()
        {
            var tc = new strut();
            var type = tc.GetType();

            FieldInfo fieldInfo = type.GetField("Int", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fieldInfo == null)
                throw new Exception();
            var method = new DynamicMethod("", typeof (object), new[] {typeof (object)}, type, true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, type);
            if (type.IsValueType)
                il.Emit(OpCodes.Unbox, type);
            il.Emit(OpCodes.Ldfld, fieldInfo);
            if (fieldInfo.FieldType.IsValueType)
                il.Emit(OpCodes.Box, fieldInfo.FieldType);
            il.Emit(OpCodes.Ret);
            var func = (Func<object, object>) method.CreateDelegate(typeof (Func<object, object>));

            tc.Int = 42;
            var q = func(tc);
        }

    }

}
