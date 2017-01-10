﻿using factor10.Obj2Db.Tests.TestData;
using NUnit.Framework;

namespace factor10.Obj2Db.Tests
{
    [TestFixture]
    public class InverseEntityTests
    {
        [Test]
        public void Test()
        {
            var x = new DataExtract<AllPropertyTypes>(entitySpec.Begin().Add("*"));
            var inverse = new entitySpec(x.TopEntity);
        }

        [Test]
        public void Test2()
        {
            var x = new DataExtract<AllPropertyTypes>(
                entitySpec.Begin()
                    .Add("TheString")
                    .Add("TheInt32")
                    .Add("formula1").Formula("val(TheString)")
                    .Add("formula2").Formula("str(TheInt32)")
                    .Add("formula3").Formula("7"));
            var inverse = new entitySpec(x.TopEntity);
        }

    }

}
