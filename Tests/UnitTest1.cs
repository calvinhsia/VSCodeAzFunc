namespace Tests;

using Microsoft.Extensions.Logging;
using Company.Function;

[TestClass]
public class UnitTest1 : TestBase
{
    [TestMethod]
    public async Task TestMethod1()
    {
        var oc = new HttpTrigger1Class(loggerFactory: this);
        var logger = CreateLogger("test");

        logger.LogInformation($"logger ");
        var req = new MyHttpRequestData(
            new MyFunctionContext(serviceProvider: this),
            new Uri("localhost:7160/api/UpdateDriverState?Driver_Id=2"));

        var resp = await oc.HttpTrigger1(req) as MyHttpResponseData;
        var str = resp!.GetResultAsString();
        TestContext.WriteLine($"{str}");
//        Assert.Fail("must fail test to see output");
    }
    [TestMethod]
    public void TestMethod2()
    {
        TestContext.WriteLine($"asdf");
        //   Assert.Fail("asdf");
    }

}