using System.Text.RegularExpressions;
using XSharper.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Data;
using System.Collections;
using System.Xml;

namespace XSharper.Core.Test
{
    
    
    /// <summary>
    ///This is a test class for UtilsTest and is intended
    ///to contain all UtilsTest Unit Tests
    ///</summary>
    [TestClass()]
    public class UtilsTest
    {


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
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///RegistryTest
        ///</summary>
        [TestMethod()]
        public void RegistryTest()
        {
            Utils.RegistrySet(@"HKLM:64\Software\XSharper\Test\Value",new[] { "AAA" });
        }
        /// <summary>
        ///A test for WildcardToRegex
        ///</summary>
        [TestMethod()]
        public void WildcardToRegexTest()
        {
            Assert.IsTrue(Regex.IsMatch("actual", Utils.WildcardToPattern("a*")));
            Assert.IsFalse(Regex.IsMatch("bactual", Utils.WildcardToPattern("a*")));

            Assert.IsTrue(Regex.IsMatch("a-b", Utils.WildcardToPattern("a*b")));
            Assert.IsFalse(Regex.IsMatch("cadabra", Utils.WildcardToPattern("a*")));
        }

        ///// <summary>
        /////A test for TransformStr
        /////</summary>
        //[TestMethod()]
        //public void TransformStrTest()
        //{
        //    string arguments = string.Empty; // TODO: Initialize to an appropriate value
        //    TransformRules trim = new TransformRules(); // TODO: Initialize to an appropriate value
        //    string expected = string.Empty; // TODO: Initialize to an appropriate value
        //    string actual;
        //    actual = Utils.TransformStr(arguments, trim);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for ToXml
        /////</summary>
        //[TestMethod()]
        //public void ToXmlTest2()
        //{
        //    DataTable dt = null; // TODO: Initialize to an appropriate value
        //    IEnumerable rows = null; // TODO: Initialize to an appropriate value
        //    string root = string.Empty; // TODO: Initialize to an appropriate value
        //    string xmlns = string.Empty; // TODO: Initialize to an appropriate value
        //    string rowElement = string.Empty; // TODO: Initialize to an appropriate value
        //    string nullValue = string.Empty; // TODO: Initialize to an appropriate value
        //    bool useAttributes = false; // TODO: Initialize to an appropriate value
        //    XmlDocument expected = null; // TODO: Initialize to an appropriate value
        //    XmlDocument actual;
        //    actual = Utils.ToXml(dt, rows, root, xmlns, rowElement, nullValue, useAttributes);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for ToXml
        /////</summary>
        //[TestMethod()]
        //public void ToXmlTest1()
        //{
        //    DataTable dt = null; // TODO: Initialize to an appropriate value
        //    string root = string.Empty; // TODO: Initialize to an appropriate value
        //    string xmlns = string.Empty; // TODO: Initialize to an appropriate value
        //    string rowElement = string.Empty; // TODO: Initialize to an appropriate value
        //    string nullValue = string.Empty; // TODO: Initialize to an appropriate value
        //    bool useAttributes = false; // TODO: Initialize to an appropriate value
        //    XmlDocument expected = null; // TODO: Initialize to an appropriate value
        //    XmlDocument actual;
        //    actual = Utils.ToXml(dt, root, xmlns, rowElement, nullValue, useAttributes);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for ToXml
        /////</summary>
        //[TestMethod()]
        //public void ToXmlTest()
        //{
        //    DataTable dt = null; // TODO: Initialize to an appropriate value
        //    string rowElement = string.Empty; // TODO: Initialize to an appropriate value
        //    XmlDocument expected = null; // TODO: Initialize to an appropriate value
        //    XmlDocument actual;
        //    actual = Utils.ToXml(dt, rowElement);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for ToVars
        /////</summary>
        //[TestMethod()]
        //public void ToVarsTest1()
        //{
        //    IEnumerable o = null; // TODO: Initialize to an appropriate value
        //    SetOfVariables expected = null; // TODO: Initialize to an appropriate value
        //    SetOfVariables actual;
        //    actual = Utils.ToVars(o);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for ToVars
        /////</summary>
        //[TestMethod()]
        //public void ToVarsTest()
        //{
        //    object[] o = null; // TODO: Initialize to an appropriate value
        //    SetOfVariables expected = null; // TODO: Initialize to an appropriate value
        //    SetOfVariables actual;
        //    actual = Utils.ToVars(o);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for ToValidCSharpName
        /////</summary>
        //[TestMethod()]
        //public void ToValidCSharpNameTest()
        //{
        //    string s = string.Empty; // TODO: Initialize to an appropriate value
        //    string expected = string.Empty; // TODO: Initialize to an appropriate value
        //    string actual;
        //    actual = Utils.ToValidCSharpName(s);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for ToTextTable
        /////</summary>
        //[TestMethod()]
        //public void ToTextTableTest3()
        //{
        //    DataTable dt = null; // TODO: Initialize to an appropriate value
        //    TableFormatOptions options = new TableFormatOptions(); // TODO: Initialize to an appropriate value
        //    string expected = string.Empty; // TODO: Initialize to an appropriate value
        //    string actual;
        //    actual = Utils.ToTextTable(dt, options);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for ToTextTable
        /////</summary>
        //[TestMethod()]
        //public void ToTextTableTest2()
        //{
        //    DataTable dt = null; // TODO: Initialize to an appropriate value
        //    string select = string.Empty; // TODO: Initialize to an appropriate value
        //    string sort = string.Empty; // TODO: Initialize to an appropriate value
        //    TableFormatOptions options = new TableFormatOptions(); // TODO: Initialize to an appropriate value
        //    string expected = string.Empty; // TODO: Initialize to an appropriate value
        //    string actual;
        //    actual = Utils.ToTextTable(dt, select, sort, options);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for ToTextTable
        /////</summary>
        //[TestMethod()]
        //public void ToTextTableTest1()
        //{
        //    DataTable dt = null; // TODO: Initialize to an appropriate value
        //    IEnumerable datarows = null; // TODO: Initialize to an appropriate value
        //    TableFormatOptions options = new TableFormatOptions(); // TODO: Initialize to an appropriate value
        //    string expected = string.Empty; // TODO: Initialize to an appropriate value
        //    string actual;
        //    actual = Utils.ToTextTable(dt, datarows, options);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for ToTextTable
        /////</summary>
        //[TestMethod()]
        //public void ToTextTableTest()
        //{
        //    DataTable dt = null; // TODO: Initialize to an appropriate value
        //    string expected = string.Empty; // TODO: Initialize to an appropriate value
        //    string actual;
        //    actual = Utils.ToTextTable(dt);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for ToHexDump
        /////</summary>
        //[TestMethod()]
        //public void ToHexDumpTest2()
        //{
        //    byte[] data = null; // TODO: Initialize to an appropriate value
        //    string expected = string.Empty; // TODO: Initialize to an appropriate value
        //    string actual;
        //    actual = Utils.ToHexDump(data);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for ToHexDump
        /////</summary>
        //[TestMethod()]
        //public void ToHexDumpTest1()
        //{
        //    byte[] data = null; // TODO: Initialize to an appropriate value
        //    int bytesPerRow = 0; // TODO: Initialize to an appropriate value
        //    string expected = string.Empty; // TODO: Initialize to an appropriate value
        //    string actual;
        //    actual = Utils.ToHexDump(data, bytesPerRow);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for ToHexDump
        /////</summary>
        //[TestMethod()]
        //public void ToHexDumpTest()
        //{
        //    byte[] data = null; // TODO: Initialize to an appropriate value
        //    int bytesPerRow = 0; // TODO: Initialize to an appropriate value
        //    bool withOffset = false; // TODO: Initialize to an appropriate value
        //    bool withChars = false; // TODO: Initialize to an appropriate value
        //    string expected = string.Empty; // TODO: Initialize to an appropriate value
        //    string actual;
        //    actual = Utils.ToHexDump(data, bytesPerRow, withOffset, withChars);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for toDisplay
        /////</summary>
        //[TestMethod()]
        //[DeploymentItem("XSharper.Core.dll")]
        //public void toDisplayTest()
        //{
        //    object p = null; // TODO: Initialize to an appropriate value
        //    TableFormatOptions tf = new TableFormatOptions(); // TODO: Initialize to an appropriate value
        //    string expected = string.Empty; // TODO: Initialize to an appropriate value
        //    string actual;
        //    actual = Utils_Accessor.toDisplay(p, tf);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for ToCSV
        /////</summary>
        //[TestMethod()]
        //public void ToCSVTest5()
        //{
        //    DataTable dt = null; // TODO: Initialize to an appropriate value
        //    string select = string.Empty; // TODO: Initialize to an appropriate value
        //    string sort = string.Empty; // TODO: Initialize to an appropriate value
        //    bool withHeader = false; // TODO: Initialize to an appropriate value
        //    char quote = '\0'; // TODO: Initialize to an appropriate value
        //    char separator = '\0'; // TODO: Initialize to an appropriate value
        //    string expected = string.Empty; // TODO: Initialize to an appropriate value
        //    string actual;
        //    actual = Utils.ToCSV(dt, select, sort, withHeader, quote, separator);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for ToCSV
        /////</summary>
        //[TestMethod()]
        //public void ToCSVTest4()
        //{
        //    DataTable dt = null; // TODO: Initialize to an appropriate value
        //    bool withHeader = false; // TODO: Initialize to an appropriate value
        //    string expected = string.Empty; // TODO: Initialize to an appropriate value
        //    string actual;
        //    actual = Utils.ToCSV(dt, withHeader);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for ToCSV
        /////</summary>
        //[TestMethod()]
        //public void ToCSVTest3()
        //{
        //    DataTable dt = null; // TODO: Initialize to an appropriate value
        //    string expected = string.Empty; // TODO: Initialize to an appropriate value
        //    string actual;
        //    actual = Utils.ToCSV(dt);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for ToCSV
        /////</summary>
        //[TestMethod()]
        //public void ToCSVTest2()
        //{
        //    DataTable dt = null; // TODO: Initialize to an appropriate value
        //    string select = string.Empty; // TODO: Initialize to an appropriate value
        //    string sort = string.Empty; // TODO: Initialize to an appropriate value
        //    bool withHeader = false; // TODO: Initialize to an appropriate value
        //    string expected = string.Empty; // TODO: Initialize to an appropriate value
        //    string actual;
        //    actual = Utils.ToCSV(dt, select, sort, withHeader);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for ToCSV
        /////</summary>
        //[TestMethod()]
        //public void ToCSVTest1()
        //{
        //    DataTable dt = null; // TODO: Initialize to an appropriate value
        //    IEnumerable rows = null; // TODO: Initialize to an appropriate value
        //    bool withHeader = false; // TODO: Initialize to an appropriate value
        //    char quote = '\0'; // TODO: Initialize to an appropriate value
        //    char separator = '\0'; // TODO: Initialize to an appropriate value
        //    string expected = string.Empty; // TODO: Initialize to an appropriate value
        //    string actual;
        //    actual = Utils.ToCSV(dt, rows, withHeader, quote, separator);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for ToCSV
        /////</summary>
        //[TestMethod()]
        //public void ToCSVTest()
        //{
        //    DataTable dt = null; // TODO: Initialize to an appropriate value
        //    bool withHeader = false; // TODO: Initialize to an appropriate value
        //    char quote = '\0'; // TODO: Initialize to an appropriate value
        //    char separator = '\0'; // TODO: Initialize to an appropriate value
        //    string expected = string.Empty; // TODO: Initialize to an appropriate value
        //    string actual;
        //    actual = Utils.ToCSV(dt, withHeader, quote, separator);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for To
        /////</summary>
        //public void ToTest1Helper<T>()
        //{
        //    object obj = null; // TODO: Initialize to an appropriate value
        //    T expected = default(T); // TODO: Initialize to an appropriate value
        //    T actual;
        //    actual = Utils.To<T>(obj);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        //[TestMethod()]
        //public void ToTest1()
        //{
        //    ToTest1Helper<GenericParameterHelper>();
        //}

        ///// <summary>
        /////A test for To
        /////</summary>
        //[TestMethod()]
        //public void ToTest()
        //{
        //    object obj = null; // TODO: Initialize to an appropriate value
        //    Type pt = null; // TODO: Initialize to an appropriate value
        //    object expected = null; // TODO: Initialize to an appropriate value
        //    object actual;
        //    actual = Utils.To(obj, pt);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for Sum
        /////</summary>
        //[TestMethod()]
        //public void SumTest1()
        //{
        //    double[] r = null; // TODO: Initialize to an appropriate value
        //    double expected = 0F; // TODO: Initialize to an appropriate value
        //    double actual;
        //    actual = Utils.Sum(r);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for Sum
        /////</summary>
        //[TestMethod()]
        //public void SumTest()
        //{
        //    long[] r = null; // TODO: Initialize to an appropriate value
        //    long expected = 0; // TODO: Initialize to an appropriate value
        //    long actual;
        //    actual = Utils.Sum(r);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for SplitArgs
        /////</summary>
        //[TestMethod()]
        //public void SplitArgsTest()
        //{
        //    string commandLine = string.Empty; // TODO: Initialize to an appropriate value
        //    string[] expected = null; // TODO: Initialize to an appropriate value
        //    string[] actual;
        //    actual = Utils.SplitArgs(commandLine);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for Rethrow
        /////</summary>
        //[TestMethod()]
        //public void RethrowTest()
        //{
        //    Exception exception = null; // TODO: Initialize to an appropriate value
        //    Utils.Rethrow(exception);
        //    Assert.Inconclusive("A method that does not return a value cannot be verified.");
        //}

        ///// <summary>
        /////A test for RemoveBackslash
        /////</summary>
        //[TestMethod()]
        //public void RemoveBackslashTest()
        //{
        //    string ret = string.Empty; // TODO: Initialize to an appropriate value
        //    string expected = string.Empty; // TODO: Initialize to an appropriate value
        //    string actual;
        //    actual = Utils.RemoveBackslash(ret);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for ReadCSVRow
        /////</summary>
        //[TestMethod()]
        //public void ReadCSVRowTest1()
        //{
        //    TextReader reader = null; // TODO: Initialize to an appropriate value
        //    Nullable<char> quote = new Nullable<char>(); // TODO: Initialize to an appropriate value
        //    char separator = '\0'; // TODO: Initialize to an appropriate value
        //    bool trimNonQuoted = false; // TODO: Initialize to an appropriate value
        //    string[] expected = null; // TODO: Initialize to an appropriate value
        //    string[] actual;
        //    actual = Utils.ReadCSVRow(reader, quote, separator, trimNonQuoted);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for ReadCSVRow
        /////</summary>
        //[TestMethod()]
        //public void ReadCSVRowTest()
        //{
        //    TextReader reader = null; // TODO: Initialize to an appropriate value
        //    string[] expected = null; // TODO: Initialize to an appropriate value
        //    string[] actual;
        //    actual = Utils.ReadCSVRow(reader);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for ReadBytes
        /////</summary>
        //[TestMethod()]
        //public void ReadBytesTest1()
        //{
        //    Stream stream = null; // TODO: Initialize to an appropriate value
        //    int count = 0; // TODO: Initialize to an appropriate value
        //    bool exactCount = false; // TODO: Initialize to an appropriate value
        //    byte[] expected = null; // TODO: Initialize to an appropriate value
        //    byte[] actual;
        //    actual = Utils.ReadBytes(stream, count, exactCount);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for ReadBytes
        /////</summary>
        //[TestMethod()]
        //public void ReadBytesTest()
        //{
        //    Stream stream = null; // TODO: Initialize to an appropriate value
        //    byte[] expected = null; // TODO: Initialize to an appropriate value
        //    byte[] actual;
        //    actual = Utils.ReadBytes(stream);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for Range
        /////</summary>
        //[TestMethod()]
        //public void RangeTest1()
        //{
        //    int min = 0; // TODO: Initialize to an appropriate value
        //    int max = 0; // TODO: Initialize to an appropriate value
        //    IEnumerable<int> expected = null; // TODO: Initialize to an appropriate value
        //    IEnumerable<int> actual;
        //    actual = Utils.Range(min, max);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for Range
        /////</summary>
        //[TestMethod()]
        //public void RangeTest()
        //{
        //    int min = 0; // TODO: Initialize to an appropriate value
        //    int max = 0; // TODO: Initialize to an appropriate value
        //    int step = 0; // TODO: Initialize to an appropriate value
        //    IEnumerable<int> expected = null; // TODO: Initialize to an appropriate value
        //    IEnumerable<int> actual;
        //    actual = Utils.Range(min, max, step);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for QuoteArgs
        /////</summary>
        //[TestMethod()]
        //public void QuoteArgsTest()
        //{
        //    string[] args = null; // TODO: Initialize to an appropriate value
        //    string expected = string.Empty; // TODO: Initialize to an appropriate value
        //    string actual;
        //    actual = Utils.QuoteArgs(args);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for QuoteArg
        /////</summary>
        //[TestMethod()]
        //public void QuoteArgTest()
        //{
        //    string arg = string.Empty; // TODO: Initialize to an appropriate value
        //    string expected = string.Empty; // TODO: Initialize to an appropriate value
        //    string actual;
        //    actual = Utils.QuoteArg(arg);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for PrefixEachLine
        /////</summary>
        //[TestMethod()]
        //public void PrefixEachLineTest()
        //{
        //    string prefix = string.Empty; // TODO: Initialize to an appropriate value
        //    string text = string.Empty; // TODO: Initialize to an appropriate value
        //    string expected = string.Empty; // TODO: Initialize to an appropriate value
        //    string actual;
        //    actual = Utils.PrefixEachLine(prefix, text);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for ParseNumber
        /////</summary>
        //[TestMethod()]
        //public void ParseNumberTest()
        //{
        //    string s = string.Empty; // TODO: Initialize to an appropriate value
        //    object expected = null; // TODO: Initialize to an appropriate value
        //    object actual;
        //    actual = Utils.ParseNumber(s);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for Or
        /////</summary>
        //[TestMethod()]
        //public void OrTest()
        //{
        //    bool[] bools = null; // TODO: Initialize to an appropriate value
        //    object expected = null; // TODO: Initialize to an appropriate value
        //    object actual;
        //    actual = Utils.Or(bools);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for MoveFile
        /////</summary>
        //[TestMethod()]
        //public void MoveFileTest()
        //{
        //    string source = string.Empty; // TODO: Initialize to an appropriate value
        //    string destination = string.Empty; // TODO: Initialize to an appropriate value
        //    bool overwrite = false; // TODO: Initialize to an appropriate value
        //    CopyFileCallback callback = null; // TODO: Initialize to an appropriate value
        //    Utils.MoveFile(source, destination, overwrite, callback);
        //    Assert.Inconclusive("A method that does not return a value cannot be verified.");
        //}

        ///// <summary>
        /////A test for LowercaseFirstLetter
        /////</summary>
        //[TestMethod()]
        //public void LowercaseFirstLetterTest()
        //{
        //    string input = string.Empty; // TODO: Initialize to an appropriate value
        //    string expected = string.Empty; // TODO: Initialize to an appropriate value
        //    string actual;
        //    actual = Utils.LowercaseFirstLetter(input);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for LoadAllReferences
        /////</summary>
        //[TestMethod()]
        //public void LoadAllReferencesTest()
        //{
        //    Utils.LoadAllReferences();
        //    Assert.Inconclusive("A method that does not return a value cannot be verified.");
        //}

        ///// <summary>
        /////A test for IsNull
        /////</summary>
        //[TestMethod()]
        //public void IsNullTest()
        //{
        //    object oTest = null; // TODO: Initialize to an appropriate value
        //    bool expected = false; // TODO: Initialize to an appropriate value
        //    bool actual;
        //    actual = Utils.IsNull(oTest);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for IsEqual
        /////</summary>
        //[TestMethod()]
        //public void IsEqualTest()
        //{
        //    object o1 = null; // TODO: Initialize to an appropriate value
        //    object o2 = null; // TODO: Initialize to an appropriate value
        //    bool expected = false; // TODO: Initialize to an appropriate value
        //    bool actual;
        //    actual = Utils.IsEqual(o1, o2);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for IIf
        /////</summary>
        //[TestMethod()]
        //public void IIfTest()
        //{
        //    bool condition = false; // TODO: Initialize to an appropriate value
        //    object ifTrue = null; // TODO: Initialize to an appropriate value
        //    object ifFalse = null; // TODO: Initialize to an appropriate value
        //    object expected = null; // TODO: Initialize to an appropriate value
        //    object actual;
        //    actual = Utils.IIf(condition, ifTrue, ifFalse);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for GetInstalledNETVersions
        /////</summary>
        //[TestMethod()]
        //public void GetInstalledNETVersionsTest()
        //{
        //    Version[] expected = null; // TODO: Initialize to an appropriate value
        //    Version[] actual;
        //    actual = Utils.GetInstalledNETVersions();
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for GetEncoding
        /////</summary>
        //[TestMethod()]
        //public void GetEncodingTest()
        //{
        //    string encoding = string.Empty; // TODO: Initialize to an appropriate value
        //    Encoding expected = null; // TODO: Initialize to an appropriate value
        //    Encoding actual;
        //    actual = Utils.GetEncoding(encoding);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for GetCORSystemDirectory
        /////</summary>
        //[TestMethod()]
        //public void GetCORSystemDirectoryTest()
        //{
        //    string expected = string.Empty; // TODO: Initialize to an appropriate value
        //    string actual;
        //    actual = Utils.GetCORSystemDirectory();
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for GenRandom
        /////</summary>
        //[TestMethod()]
        //public void GenRandomTest()
        //{
        //    int min = 0; // TODO: Initialize to an appropriate value
        //    int max = 0; // TODO: Initialize to an appropriate value
        //    int expected = 0; // TODO: Initialize to an appropriate value
        //    int actual;
        //    actual = Utils.GenRandom(min, max);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for GenGoodRandom
        /////</summary>
        //[TestMethod()]
        //public void GenGoodRandomTest()
        //{
        //    int min = 0; // TODO: Initialize to an appropriate value
        //    int max = 0; // TODO: Initialize to an appropriate value
        //    int expected = 0; // TODO: Initialize to an appropriate value
        //    int actual;
        //    actual = Utils.GenGoodRandom(min, max);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for FitWidth
        /////</summary>
        //[TestMethod()]
        //public void FitWidthTest()
        //{
        //    string s = string.Empty; // TODO: Initialize to an appropriate value
        //    int maxLen = 0; // TODO: Initialize to an appropriate value
        //    EllipsisLocation loca = new EllipsisLocation(); // TODO: Initialize to an appropriate value
        //    string expected = string.Empty; // TODO: Initialize to an appropriate value
        //    string actual;
        //    actual = Utils.FitWidth(s, maxLen, loca);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for FindType
        /////</summary>
        //[TestMethod()]
        //public void FindTypeTest()
        //{
        //    string ts = string.Empty; // TODO: Initialize to an appropriate value
        //    Type expected = null; // TODO: Initialize to an appropriate value
        //    Type actual;
        //    actual = Utils.FindType(ts);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for FindNETFrameworkDirectory
        /////</summary>
        //[TestMethod()]
        //public void FindNETFrameworkDirectoryTest()
        //{
        //    Version v = null; // TODO: Initialize to an appropriate value
        //    DirectoryInfo expected = null; // TODO: Initialize to an appropriate value
        //    DirectoryInfo actual;
        //    actual = Utils.FindNETFrameworkDirectory(v);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for FindInstanceCreator
        /////</summary>
        //[TestMethod()]
        //public void FindInstanceCreatorTest()
        //{
        //    Type type = null; // TODO: Initialize to an appropriate value
        //    Utils.FastCreateInstance expected = null; // TODO: Initialize to an appropriate value
        //    Utils.FastCreateInstance actual;
        //    actual = Utils.FindInstanceCreator(type);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for EscapeXml
        /////</summary>
        //[TestMethod()]
        //public void EscapeXmlTest()
        //{
        //    string text = string.Empty; // TODO: Initialize to an appropriate value
        //    string expected = string.Empty; // TODO: Initialize to an appropriate value
        //    string actual;
        //    actual = Utils.EscapeXml(text);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for EnsureBackslash
        /////</summary>
        //[TestMethod()]
        //public void EnsureBackslashTest1()
        //{
        //    string ret = string.Empty; // TODO: Initialize to an appropriate value
        //    Backslash backSlash = new Backslash(); // TODO: Initialize to an appropriate value
        //    string expected = string.Empty; // TODO: Initialize to an appropriate value
        //    string actual;
        //    actual = Utils.EnsureBackslash(ret, backSlash);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for EnsureBackslash
        /////</summary>
        //[TestMethod()]
        //public void EnsureBackslashTest()
        //{
        //    string ret = string.Empty; // TODO: Initialize to an appropriate value
        //    string expected = string.Empty; // TODO: Initialize to an appropriate value
        //    string actual;
        //    actual = Utils.EnsureBackslash(ret);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for DebugBreak
        /////</summary>
        //[TestMethod()]
        //public void DebugBreakTest()
        //{
        //    Utils.DebugBreak();
        //    Assert.Inconclusive("A method that does not return a value cannot be verified.");
        //}

        ///// <summary>
        /////A test for CreateInstance
        /////</summary>
        //[TestMethod()]
        //public void CreateInstanceTest()
        //{
        //    Type t = null; // TODO: Initialize to an appropriate value
        //    object expected = null; // TODO: Initialize to an appropriate value
        //    object actual;
        //    actual = Utils.CreateInstance(t);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for CopyFile
        /////</summary>
        //[TestMethod()]
        //public void CopyFileTest1()
        //{
        //    string source = string.Empty; // TODO: Initialize to an appropriate value
        //    string destination = string.Empty; // TODO: Initialize to an appropriate value
        //    bool overwrite = false; // TODO: Initialize to an appropriate value
        //    bool move = false; // TODO: Initialize to an appropriate value
        //    CopyFileCallback callback = null; // TODO: Initialize to an appropriate value
        //    Utils.CopyFile(source, destination, overwrite, move, callback);
        //    Assert.Inconclusive("A method that does not return a value cannot be verified.");
        //}

        ///// <summary>
        /////A test for CopyFile
        /////</summary>
        //[TestMethod()]
        //public void CopyFileTest()
        //{
        //    string source = string.Empty; // TODO: Initialize to an appropriate value
        //    string destination = string.Empty; // TODO: Initialize to an appropriate value
        //    bool overwrite = false; // TODO: Initialize to an appropriate value
        //    CopyFileCallback callback = null; // TODO: Initialize to an appropriate value
        //    Utils.CopyFile(source, destination, overwrite, callback);
        //    Assert.Inconclusive("A method that does not return a value cannot be verified.");
        //}

        ///// <summary>
        /////A test for ConvertTimeSpan
        /////</summary>
        //[TestMethod()]
        //[DeploymentItem("XSharper.Core.dll")]
        //public void ConvertTimeSpanTest()
        //{
        //    string Timeout = string.Empty; // TODO: Initialize to an appropriate value
        //    Nullable<TimeSpan> expected = new Nullable<TimeSpan>(); // TODO: Initialize to an appropriate value
        //    Nullable<TimeSpan> actual;
        //    actual = Utils_Accessor.ConvertTimeSpan(Timeout);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for convertString
        /////</summary>
        //[TestMethod()]
        //[DeploymentItem("XSharper.Core.dll")]
        //public void convertStringTest()
        //{
        //    string text = string.Empty; // TODO: Initialize to an appropriate value
        //    Type pt = null; // TODO: Initialize to an appropriate value
        //    object expected = null; // TODO: Initialize to an appropriate value
        //    object actual;
        //    actual = Utils_Accessor.convertString(text, pt);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for CompareIgnoreCase
        /////</summary>
        //[TestMethod()]
        //public void CompareIgnoreCaseTest()
        //{
        //    string str1 = string.Empty; // TODO: Initialize to an appropriate value
        //    string str2 = string.Empty; // TODO: Initialize to an appropriate value
        //    int expected = 0; // TODO: Initialize to an appropriate value
        //    int actual;
        //    actual = Utils.CompareIgnoreCase(str1, str2);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for Compare
        /////</summary>
        //[TestMethod()]
        //public void CompareTest()
        //{
        //    string str1 = string.Empty; // TODO: Initialize to an appropriate value
        //    string str2 = string.Empty; // TODO: Initialize to an appropriate value
        //    int expected = 0; // TODO: Initialize to an appropriate value
        //    int actual;
        //    actual = Utils.Compare(str1, str2);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for CommonBase
        /////</summary>
        //[TestMethod()]
        //public void CommonBaseTest()
        //{
        //    object p = null; // TODO: Initialize to an appropriate value
        //    Type common = null; // TODO: Initialize to an appropriate value
        //    Type expected = null; // TODO: Initialize to an appropriate value
        //    Type actual;
        //    actual = Utils.CommonBase(p, common);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for Coalesce
        /////</summary>
        //[TestMethod()]
        //public void CoalesceTest()
        //{
        //    object[] par = null; // TODO: Initialize to an appropriate value
        //    object expected = null; // TODO: Initialize to an appropriate value
        //    object actual;
        //    actual = Utils.Coalesce(par);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for And
        /////</summary>
        //[TestMethod()]
        //public void AndTest()
        //{
        //    bool[] bools = null; // TODO: Initialize to an appropriate value
        //    object expected = null; // TODO: Initialize to an appropriate value
        //    object actual;
        //    actual = Utils.And(bools);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for AllEnumValuesToString
        /////</summary>
        //[TestMethod()]
        //public void AllEnumValuesToStringTest1()
        //{
        //    string typename = string.Empty; // TODO: Initialize to an appropriate value
        //    string separator = string.Empty; // TODO: Initialize to an appropriate value
        //    string expected = string.Empty; // TODO: Initialize to an appropriate value
        //    string actual;
        //    actual = Utils.AllEnumValuesToString(typename, separator);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}

        ///// <summary>
        /////A test for AllEnumValuesToString
        /////</summary>
        //[TestMethod()]
        //public void AllEnumValuesToStringTest()
        //{
        //    Type t = null; // TODO: Initialize to an appropriate value
        //    string separator = string.Empty; // TODO: Initialize to an appropriate value
        //    string expected = string.Empty; // TODO: Initialize to an appropriate value
        //    string actual;
        //    actual = Utils.AllEnumValuesToString(t, separator);
        //    Assert.AreEqual(expected, actual);
        //    Assert.Inconclusive("Verify the correctness of this test method.");
        //}
    }
}
