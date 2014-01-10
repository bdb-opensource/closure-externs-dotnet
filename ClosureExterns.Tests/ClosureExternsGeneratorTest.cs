using System;
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
            public B B { get; set; }
            public B[] Bs { get; set; }
        }

        protected class B
        {
            public int[] IntArray { get; set; }
        }


        [TestMethod]
        public void ClosureExternsGenerator_Generate()
        {
            var types = new Type[] { typeof(A) };
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
/** @type {Types.B} */
Types.A.prototype.b = null;
/** @type {Array.<Types.B>} */
Types.A.prototype.bs = null;>";

            Assert.AreEqual(expected.Trim(), actual.Trim()); 
        }
    }
}
