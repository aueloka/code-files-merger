
namespace Aueloka.CodeMerger.CSharp.Tests.Unit
{
    using System.Linq;
    using Aueloka.CodeMerger.CSharp;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CSharpFileParserTests
    {

        [TestMethod]
        public void TestWillParseAll()
        {
            string file = @"..\..\CSharp\single_namespace_file.txt";
            CSharpFileInfo fileInfo = CSharpFileParser.ParseFile(file);

            Assert.AreEqual(1, fileInfo.Imports.Count());
            Assert.AreEqual(true, fileInfo.HasMainMethod);
            Assert.AreEqual(1, fileInfo.NamespacesContent.Count());
            Assert.AreEqual(2, fileInfo.NamespacesContent.Single().Imports.Count());
            Assert.IsTrue(fileInfo.NamespacesContent.Single().Content.Contains("class Foo"));
            Assert.AreEqual("FooNamespace", fileInfo.NamespacesContent.Single().Name);
            Assert.IsFalse(fileInfo.NamespacesContent.Single().Content.Contains("using FooNamespace.Bar"));
        }

        //TODO: Add tests for file with multiple namespaces
    }
}
