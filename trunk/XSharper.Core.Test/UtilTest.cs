using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace XSharper.Core.Test
{
    /// <summary>
    /// Summary description for UtilTest
    /// </summary>
    [TestClass]
    public class UtilTest
    {
        public UtilTest()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void NumberParsing()
        {
            Assert.AreEqual(20.12m, ParsingReader.ParseNumber("  20.12m"));
            Assert.AreEqual(-20e12d, ParsingReader.ParseNumber(" -20e12d"));
            Assert.AreEqual(0xf312, ParsingReader.ParseNumber("0xf312"));
            Assert.IsNull(ParsingReader.TryParseNumber("0xf312Q"));
            Assert.IsNull(ParsingReader.TryParseNumber(""));
        }

        [TestMethod]
        public void StateBag()
        {
            object x = "a";
            x+="a";
            var sb = new StateBag();
            sb.Set(x,"p1",20);
            sb.Set("aa", "p1", 50);
            Assert.AreEqual(20,sb.Get(x,"p1",null));

            // Aa is interned
            Assert.IsTrue(string.IsInterned("aa")!=null);
            Assert.AreEqual(50, sb.Get("aa", "p1", null));

            sb.Remove("aa","p1");
            Assert.IsNull(sb.Get("aa", "p1", null));

        }
    }
}
