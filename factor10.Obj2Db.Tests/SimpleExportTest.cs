﻿using System.Collections.Generic;
using System.Linq;
using factor10.Obj2Db.Tests.TestData;
using NUnit.Framework;

namespace factor10.Obj2Db.Tests
{
    [TestFixture]
    public class SimpleExportTest
    {
        private readonly TheTop _td = new TheTop
        {
            FirstName = "nisse",
            SomeStruct = new SomeStruct {X = 3, Y = 4},
            Strings = new List<string> {"Kalle", "Nisse", "Sture"},
            Structs = new List<SomeStruct> {new SomeStruct {X = 5, Y = 6}, new SomeStruct {X = 7, Y = 8}}
        };

        [Test]
        public void TestSimpleProperties()
        {
            var export = new DataExtract<TheTop>(entitySpec.Begin()
                .Add("FirstName")
                .Add("SomeStruct.X"));
            export.Run(_td);
            var tables = export.TableManager.GetWithAllData();
            CollectionAssert.AreEqual(new object[] {"nisse", 3}, tables.Single().Rows.Single().Columns);
        }

        [Test]
        public void TestSimplePropertiesAndIEnumerableOverPrimitive()
        {
            var export = new DataExtract<TheTop>(entitySpec.Begin()
                .Add("Strings"));
            export.Run(_td);
            var table = export.TableManager.GetWithAllData().Last();
            CollectionAssert.AreEquivalent(_td.Strings, table.Rows.SelectMany(_ => _.Columns));
        }

        [Test]
        public void TestSimplePropertiesAndIEnumerableOverStruct()
        {
            var export = new DataExtract<TheTop>(entitySpec.Begin()
                .Add(entitySpec.Begin("Structs")
                    .Add("X")
                    .Add("Y")));
            export.Run(_td);
            var table = export.TableManager.GetWithAllData().Last();
            CollectionAssert.AreEquivalent(new[] {5, 6, 7, 8}, table.Rows.SelectMany(_ => _.Columns));
        }

    }

}