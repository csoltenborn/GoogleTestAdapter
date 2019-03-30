using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

[TestClass]
class TestSelfTest: IDisposable
{
    [TestMethod]
    void ThisTestPasses()
    {
        Console.WriteLine("ctor");
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    void ThisMethodThrowsButItsExpected()
    {
        throw new ArgumentException("Argument exception message");
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    void ThisMethodThrowsButItsNotExpected()
    {
        throw new Exception("This exception is normal exception, not ArgumentException");
    }

    public void Dispose()
    {
        Console.WriteLine("dtor");
    }

    [TestMethod]
    void ThisTestFailsWithOneLevelCallback()
    {
        throw new ArgumentException("Exception thrown from ThisTestFailsWithOneLevelCallback");
    }

    void testFunc()
    { 
        throw new Exception("Exception thrown from testFunc");
    }


    [TestMethod]
    void ThisTestFailsWithNestedCallback()
    {
        testFunc();
    }

    //[TestMethod]
    //void Timing1()
    //{
    //    Thread.Sleep(5000);
    //}

    //[TestMethod]
    //void Timing2()
    //{
    //    Thread.Sleep(10000);
    //}

    [TestMethod]
    void AssertsFails()
    {
        Console.WriteLine("AssertsFails");
        Assert.AreEqual(true, false);
    }

    [TestMethod]
    [Ignore]
    void ThisTestShouldBeIgnored()
    {
        Console.WriteLine("ignored method");
    }

}

[TestClass]
class TestSelfTest2
{
    [TestMethod]
    void ThisMethodThrowsButNotCleanup()
    {
        throw new Exception("ThisMethodThrowsButNotCleanup");
    }

    [TestCleanup]
    void Cleanup()
    {
        throw new Exception("This exception must not be visible to end-user");
    }
}


[TestClass]
class TestSelfTest3
{
    [TestMethod]
    void ThisMethodDoesNotThrowButCleanupDoes()
    {
    }

    [TestCleanup]
    void Cleanup()
    {
        throw new Exception("TestSelfTest3 - throw from cleanup");
    }
}

