using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace XSharper.Core.Test
{
    /// <summary>
    /// Summary description for ParserTest
    /// </summary>
    [TestClass]
    public class ParserTest
    {
        private BasicEvaluationContext _ev = new BasicEvaluationContext();
        public ParserTest()
        {
            _ev.Variables["v_str"] = "Hello";
            _ev.Variables["v_t"] = "T";
            _ev.Variables["v_int"] = 1;
            _ev.Objects["o_str"] = "Obj";
            _ev.Objects["o_int"] = 100;
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
        public void TestNumParsing()
        {
            Assert.AreEqual(0x22,ParsingReader.ParseNumber("0x22"));
            Assert.AreEqual(0x22L, ParsingReader.ParseNumber("0x22l"));
            Assert.AreEqual(-112.21, ParsingReader.ParseNumber("-112.21"));
            Assert.IsInstanceOfType(ParsingReader.ParseNumber("500.12m"), typeof(decimal));
            Assert.IsInstanceOfType(ParsingReader.ParseNumber("500.12f"), typeof(float));
            Assert.IsInstanceOfType(ParsingReader.ParseNumber("0x500ul"), typeof(ulong));
            Assert.IsInstanceOfType(ParsingReader.ParseNumber("0x500l"), typeof(long));
        }


        [TestMethod,ExpectedException(typeof(ParsingException))]
        public void Fail1()
        {
            _ev.Eval<int>("1 2 +");
        }
        [TestMethod, ExpectedException(typeof(ParsingException))]
        public void Fail2() 
        {
            _ev.Eval<int>("(1 2 +)");
        }

        [TestMethod]
        public void CommonArray()
        {
            var ve = new VarsWithExpand();
            var res = ve.Eval("new [] { 1,2}");
            Assert.IsInstanceOfType(res, typeof(int[]));
            Assert.IsTrue(((int[])res)[1]==2);

        }
        [TestMethod]
        public void ExpressionParsing()
        {
            Assert.AreEqual(2, _ev.Eval("((int[]){'1','2','3'})[1]"));
            Assert.AreEqual(6, _ev.Eval<int>("new int[] {1,2,3}.Length*2"));
            Assert.AreEqual(5, _ev.Eval("((int[])(new long[] { '1','2','5','10'}))[2]"));
            Assert.AreEqual(20, _ev.Eval("(25+1)-6"));
            Assert.AreEqual(10, _ev.Eval("(25+1)+(-6)+{-10,10}[0]"));
            Assert.AreEqual(false, _ev.Eval("(bool)null"));
            Assert.AreEqual(false, _ev.Eval("'aa'=='AA'"));
            Assert.AreEqual(true, _ev.Eval("'aa'=='aa'"));
            Assert.AreEqual(false, _ev.Eval("'12'>15"));
            Assert.AreEqual(true, _ev.Eval("(char)'12'>15"));
            Assert.AreEqual(50, _ev.Eval("null??50"));
            Assert.AreEqual(20, _ev.Eval<int>("30-(Math.Max(3,5)*Math.Abs(-2))"));
            Assert.AreEqual(true, _ev.Eval("char.IsDigit('9')"));
            Assert.AreEqual(10.11, _ev.Eval("(int)5.44+5.11"));
            Assert.AreEqual("HelloObjHello", _ev.Eval("$v_str+o_str+${v_undefined|v_str}"));
            Assert.AreEqual(5, _ev.Eval("$v_str.Length"));
            Assert.AreEqual("System.Int32", _ev.Eval("(String)5.GetType().FullName"));
            Assert.AreEqual("System.Int32", _ev.Eval("typeof(int).FullName"));
            Assert.AreEqual(true, _ev.Eval("true || (1/string.empty.length) || true"));
            Assert.AreEqual(true, _ev.Eval("false || true && true && false || true"));
            Assert.AreEqual(false, _ev.Eval("false || true && true && false"));
            Assert.AreEqual(true, _ev.Eval("false || true && true && !false"));
            Assert.AreEqual("2", _ev.Eval("(string)(long)(double)'2.2'"));
            Assert.AreEqual("32", _ev.Eval("(string)0x20"));
            Assert.AreEqual("train",_ev.Eval<string>(@"
                 ( ( $v_t == 'B' ) ? 'bus' : 
                   ( $v_t == 'A' ) ? 'airplane' : 
	               ( $v_t == 'T' ) ? 'train' : 
	               ( $v_t == 'C' ) ? 'car' : 
	               ( $v_t == 'H' ) ? 'horse' : 
                    'feet' );"));
            
        }
    }
}
