using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

public class ClassicUnitSuiteInfo : UnitSuiteInfo
{
    public Type type;
    public object classInstance;
    public MethodInfo[] testInitMethods;
    public MethodInfo[] testCleanupMethods;


    override public IEnumerable<UnitSuiteInfo> GetSuites()
    {
        var asm = Assembly.GetExecutingAssembly();
        var testTypes = asm.GetTypes().Where(t => t.GetCustomAttribute<TestClassAttribute>() != null).ToList();
        var rl = testTypes.Select(x => new ClassicUnitSuiteInfo() { SuiteName = x.Name, type = x }).ToList();

        for (int i = 0; i < rl.Count; i++)
        {
            Type type = rl[i].type;
            bool remove = false;

            if (type.GetCustomAttribute<IgnoreAttribute>() != null)
                remove = true;

            if (remove)
            {
                testTypes.RemoveAt(i);
                i--;
            }
        }
        return rl.Cast<UnitSuiteInfo>();
    }

    override public IEnumerable<UnitTestInfo> GetUnitTests()
    {
        var methods = GetMethodsWithAttribute<TestMethodAttribute>(type).ToList();
        var rl = methods.Select(x => new TraditionalUnitTestInfo() { UnitTestName = x.Name, method = x, suite = this }).ToList();
        testInitMethods = GetMethodsWithAttribute<TestInitializeAttribute>(type);
        testCleanupMethods = GetMethodsWithAttribute<TestCleanupAttribute>(type);

        foreach (var uti in rl)
        { 
            uti.ignored = uti.method.GetCustomAttribute<IgnoreAttribute>() != null;
            TestMethodAttribute tmattr = uti.method.GetCustomAttribute<TestMethodAttribute>();
            uti.sourceCodePath = tmattr.File;
            uti.line = tmattr.Line + 1;
            ExpectedExceptionAttribute eeattr = uti.method.GetCustomAttribute<ExpectedExceptionAttribute>();
            if (eeattr != null)
                uti.ExceptionType = eeattr.ExceptionType;
        }

        return rl.Cast<UnitTestInfo>();
    }

    static MethodInfo[] GetMethodsWithAttribute<T>(Type type) where T : Attribute
    {
        var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Where(x => x.GetCustomAttribute<T>() != null).ToArray();
        return methods;
    }
}

public class TraditionalUnitTestInfo : UnitTestInfo
{
    public ClassicUnitSuiteInfo suite;
    public MethodInfo method;

    override public void InvokeTest(bool isLastMethod, TestResults localTestResults)
    {
        bool bInvokeSucceeded = false;

        if (suite.classInstance == null)
            suite.classInstance = Activator.CreateInstance(suite.type);

        try
        {
            // Call all [TestInitialize] methods
            foreach (var mi in suite.testInitMethods)
                mi.Invoke(suite.classInstance, null);

            method.Invoke(suite.classInstance, null);
            bInvokeSucceeded = true;

        }
        finally
        {
            // Call all [TestCleanup] methods
            foreach (var mi in suite.testCleanupMethods)
            {
                try
                {
                    mi.Invoke(suite.classInstance, null);
                }
                catch (Exception ex)
                {
                    // If we are already in exception, no need for second
                    // exception from cleanup, but otherwise throw
                    // also cleanup exceptions.
                    if (bInvokeSucceeded)
                        throw ex;
                }
            }

            if (isLastMethod)
            {
                IDisposable disp = suite.classInstance as IDisposable;
                suite.classInstance = null;

                if (disp != null)
                    disp.Dispose();
            }
        }
    }
}


public class TestMethodAttribute : Attribute
{
    public readonly string File;
    public readonly string MethodName;
    public readonly int Line;

    public TestMethodAttribute( [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0 )
    {
        File = file;
        MethodName = member;
        Line = line;
    }

    public override string ToString() { return File + "(" + Line + "):" + MethodName; }
}


public class IgnoreAttribute : Attribute
{
}


public class TestClassAttribute : Attribute
{
}

public class TestInitializeAttribute : Attribute
{
}

public class TestCleanupAttribute : Attribute
{
}

public class ExpectedExceptionAttribute : Attribute
{
    public Type ExceptionType;

    public ExpectedExceptionAttribute(Type type)
    {
        ExceptionType = type;
    }
}


