using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClosureExterns.Tests
{
    [TestClass]
    public class ClosureExternsGeneratorTest
    {
        protected class A
        {
            public string StringValue { get; set; }
            public int IntValue { get; set; }
            public Nullable<int> NullableIntValue { get; set; }
            public B B { get; set; }
            public B[] Bs { get; set; }
        }

        protected class B
        {
            public int[] IntArray { get; set; }
        }

        [TestMethod]
        public void ClosureExternsGenerator_Generate_Test()
        {
            var types = new Dictionary<string, IEnumerable<Type>> { { "Types", new Type[] { typeof(A) } } };
            var actual = ClosureExternsGenerator.Generate(types);
            var expected = @"
/** @const */
var Types = {};

// ClosureExterns.Tests.ClosureExternsGeneratorTest+B
/** @constructor
 */
Types.B = function() {};
/** @type {Array.<number>} */
Types.B.prototype.intArray = null;

// ClosureExterns.Tests.ClosureExternsGeneratorTest+A
/** @constructor
 */
Types.A = function() {};
/** @type {string} */
Types.A.prototype.stringValue = '';
/** @type {number} */
Types.A.prototype.intValue = 0;
/** @type {?number} */
Types.A.prototype.nullableIntValue = null;
/** @type {Types.B} */
Types.A.prototype.b = null;
/** @type {Array.<Types.B>} */
Types.A.prototype.bs = null;
";

            Assert.AreEqual(expected.Trim(), actual.Trim());
        }

        protected class AClass
        {
            public string StringValue { get; set; }
            public int IntValue { get; set; }
            public BClass B { get; set; }
            public BClass[] Bs { get; set; }
            public CClass C { get; set; }
        }

        protected class BClass
        {
            public int[] IntArray { get; set; }
        }

        protected class CClass
        {
            public Nullable<int> NullableInt { get; set; }
        }

        [TestMethod]
        public void ClosureExternsGenerator_GenerateWithOptions_MapType_Test()
        {
            var types = new Dictionary<string, IEnumerable<Type>> { { "", new Type[] { typeof(AClass) } } };
            var testOptions = GetTestOptions();
            testOptions.MapType = x => typeof(Boolean);
            var actual = ClosureExternsGenerator.Generate(types, testOptions);

            var expected = @"
/** @const */
var TestNamespace = {};";

            Assert.AreEqual(expected.Trim(), actual.Trim());
        }

        [TestMethod]
        public void ClosureExternsGenerator_GenerateWithOptions_NamespaceExpression_Test()
        {
            var types = new Dictionary<string, IEnumerable<Type>> { };
            var testOptions = GetTestOptions();
            testOptions.NamespaceDefinitionExpression = x => String.Format("goog.provide('{0}');", x);
            var actual = ClosureExternsGenerator.Generate(types, testOptions);

            var expected = "goog.provide('TestNamespace');";

            Assert.AreEqual(expected.Trim(), actual.Trim());
        }

        protected class DictClass
        {
            public Dictionary<string, int> DictProperty { get; set; }
        }

        [TestMethod]
        public void ClosureExternsGenerator_DictClass_Test()
        {
            var types = new Dictionary<string, IEnumerable<Type>> { { "", new Type[] { typeof(DictClass) } } };
            var actual = ClosureExternsGenerator.Generate(types);
            var expected = @"
/** @const */
var TestNamespace = {};

// ClosureExterns.Tests.ClosureExternsGeneratorTest+DictClass
/** @constructor
 */
TestNamespace.DictClass = function() {};
/** @type {Object.<string, number>} */
TestNamespace.DictClass.prototype.dictProperty = null;
";

            Assert.AreEqual(expected.Trim(), actual.Trim());
        }

        [TestMethod]
        public void ClosureExternsGenerator_GenerateWithOptions_Test()
        {
            var types = new Dictionary<string, IEnumerable<Type>> { { "TestNamespace", new Type[] { typeof(AClass) } } };
            var testOptions = GetTestOptions();
            var actual = ClosureExternsGenerator.Generate(types, testOptions);

            var expected = @"
/** @const */
var TestNamespace = {};

// ClosureExterns.Tests.ClosureExternsGeneratorTest+BClass
/** @constructor
 * @something TestNamespace.Bee
 */
TestNamespace.Bee = function TestNamespace.Bee() { };
/** @type {Array.<number>} */
TestNamespace.Bee.prototype.intArray = null;

// ClosureExterns.Tests.ClosureExternsGeneratorTest+CClass
/** @constructor
 * @something TestNamespace.C
 */
TestNamespace.C = function TestNamespace.C() { };
/** @type {?number} */
TestNamespace.C.prototype.nullableInt = null;

// ClosureExterns.Tests.ClosureExternsGeneratorTest+AClass
/** @constructor
 * @something TestNamespace.A
 */
TestNamespace.A = function TestNamespace.A() { };
/** @type {string} */
TestNamespace.A.prototype.stringValue = '';
/** @type {number} */
TestNamespace.A.prototype.intValue = 0;
/** @type {TestNamespace.Bee} */
TestNamespace.A.prototype.b = null;
/** @type {Array.<TestNamespace.Bee>} */
TestNamespace.A.prototype.bs = null;
/** @type {TestNamespace.C} */
TestNamespace.A.prototype.c = null;
";

            Assert.AreEqual(expected.Trim(), actual.Trim());
        }

        protected class C
        {
            public const string TEST_NAME = "TEST";
        }

        [TestMethod]
        public void ClosureExternsGenerator_GenerateConstantss_Test()
        {
            var types = new Dictionary<string, IEnumerable<Type>> { { "TestNamespace", new Type[] { typeof(C) } } };
            var testOptions = GetTestOptions();
            var actual = ClosureExternsGenerator.Generate(types, testOptions);

            var expected = @"
/** @const */
var TestNamespace = {};

// ClosureExterns.Tests.ClosureExternsGeneratorTest+C
/** @constructor
 * @something TestNamespace.C
 */
TestNamespace.C = function TestNamespace.C() { };

// ClosureExterns.Tests.ClosureExternsGeneratorTest+C Consts
TestNamespace.CConsts = {
/** @type {string} */
TEST_NAME: 'TEST',
}

";

            Assert.AreEqual(expected.Trim(), actual.Trim());
        }

        private static ClosureExternsOptions GetTestOptions()
        {
            return new ClosureExternsOptions()
            {
                ConstructorAnnotations = x => new string[] { "@something " + x },
                ConstructorExpression = x => "function " + x + "() { }",
                SuffixToTrimFromTypeNames = "Class",
                TryGetTypeName = x => x.Equals(typeof(BClass)) ? "Bee" : null
            };
        }
    }
}
