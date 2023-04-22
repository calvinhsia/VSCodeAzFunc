namespace Tests;

using Microsoft.Extensions.Logging;
using Company.Function;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Reflection;
using System.Diagnostics;
using System.Net.Http;
using Newtonsoft.Json;

[TestClass]
public class UnitTest1 : TestBase
{
    //https://learn.microsoft.com/en-us/dotnet/core/tutorials/testing-library-with-visual-studio-code?pivots=dotnet-7-0
    [TestMethod]
    public async Task TestGetWordData()
    {
        var oc = new GetWordDataClass(loggerFactory: this);
        oc.random = new Random(1);
        var logger = CreateLogger("test");

        logger.LogInformation($"logger ");
        var req = new MyHttpRequestData(
            new MyFunctionContext(serviceProvider: this),
            new Uri($"localhost:7160/api/GetWordData?Driver_Id=2")
        );

        var resp = await oc.GetWordData(req) as MyHttpResponseData;
        var str = resp!.GetResultAsString();
        logger.LogInformation($"{str}");
        VerifyLogStrings(new[] {
            """RandWord": "DISCONTINUED""",
            """Grid": "_D_DITIENNUS_OC_""",
            """GridWithRandomLetters": "DDDDITIENNUSJOCM"""
        });
    }

    [TestMethod]
    public async Task TestHTTPTrigger()
    {
        var oc = new HttpTrigger1Class(loggerFactory: this);
        var logger = CreateLogger("test");

        logger.LogInformation($"logger ");
        var req = new MyHttpRequestData(
            new MyFunctionContext(serviceProvider: this),
            new Uri($"localhost:7160/api/UpdateDriverState?Driver_Id=2")
        );

        var resp = await oc.HttpTrigger1(req) as MyHttpResponseData;
        var str = resp!.GetResultAsString();
        Trace.WriteLine($"{str}");
        //               Assert.Fail("must fail test to see output in OutputPane (Test Explorer (Test runner Output))");
        VerifyLogStrings(new[] {
            "Word of the day:",
            """name": "Accessories","""
        });
    }

    [TestMethod]
    public async Task TestInvokeinCloud()
    {
        var url = @"https://calvinhvscode.azurewebsites.net/api/HttpTrigger1";
        var httpClient = new HttpClient();
        var res = await httpClient.GetAsync(url);
        var data = await res.Content.ReadAsStringAsync();
        Trace.WriteLine(data);
        VerifyLogStrings(new[] {
            "Word of the day:",
            """name": "Accessories","""
        });
    }

    [TestMethod]
    public async Task TestGetSqlData()
    {
        var oc = new GetSqlDataClass(loggerFactory: this);
        var logger = CreateLogger(nameof(TestGetSqlData));
        logger.LogInformation($"Starting {nameof(TestGetSqlData)}");
        var req = new MyHttpRequestData(
            new MyFunctionContext(serviceProvider: this),
            new Uri($"localhost:7160/api/GetSqlData?Table=Customer&NumItems=10&PrettyPrint=1&TableName=Customer&Filter=where%20CompanyName%20like%20%27%25bike%25%27")
        );
        var resp = await oc.GetSqlData(req) as MyHttpResponseData;
        var data = resp!.GetResultAsString();
        Trace.WriteLine(data);
        var json = JsonConvert.DeserializeObject(data);
    }

    [TestMethod]
    public async Task TestGetSqlDataWithSqlStatement()
    {
        var oc = new GetSqlDataClass(loggerFactory: this);
        var logger = CreateLogger(nameof(TestGetSqlData));
        logger.LogInformation($"Starting {nameof(TestGetSqlData)}");
        var req = new MyHttpRequestData(
            new MyFunctionContext(serviceProvider: this),
            new Uri($"localhost:7160/api/GetSqlData?PrettyPrint=1Sql=Select * from Customer")
        );
        var resp = await oc.GetSqlData(req) as MyHttpResponseData;
        var data = resp!.GetResultAsString();
        Trace.WriteLine(data);
        var json = JsonConvert.DeserializeObject(data);
        VerifyLogStrings(new[] {
            """FirstName": "Orlando""",
            """SalesPerson": "adventure-works\\pamela0"""
            });
    }

    [TestMethod]
    public async Task TestGetSqlDataWithCreateTable()
    {
        /*
        create table foo (name varchar(20))
        insert into foo (name) values ('myname')
        select * from foo
        drop table foo
        */
        var cmds = new[]
        {
            "create table foo (name varchar(20))",
            "insert into foo (name) values ('myname')",
            "select * from foo",
            "drop table foo",
            "drop table foo" // cause an error
        };
        var oc = new GetSqlDataClass(loggerFactory: this);
        var logger = CreateLogger(nameof(TestGetSqlData));
        logger.LogInformation($"Starting {nameof(TestGetSqlData)}");
        foreach (var cmd in cmds)
        {
            var req = new MyHttpRequestData(
                new MyFunctionContext(serviceProvider: this),
                new Uri($"localhost:7160/api/GetSqlData?PrettyPrint=1&Sql={cmd}")
            );
            var resp = await oc.GetSqlData(req) as MyHttpResponseData;
            var data = resp!.GetResultAsString();
            logger.LogInformation(data);
            var json = JsonConvert.DeserializeObject(data);
        }
        VerifyLogStrings(new[] {
            "Cannot drop the table 'foo', because it does not exist or you do not have permission"
            });
    }


    [TestMethod]
    public async Task TestGetSqlDataWithSqlStatementWithError()
    {
        var oc = new GetSqlDataClass(loggerFactory: this);
        var logger = CreateLogger(nameof(TestGetSqlData));
        logger.LogInformation($"Starting {nameof(TestGetSqlData)}");
        var req = new MyHttpRequestData(
            new MyFunctionContext(serviceProvider: this),
            new Uri($"localhost:7160/api/GetSqlData?PrettyPrint=1&Sql=Select aeafasdfasdf* from Customer")
        );
        var resp = await oc.GetSqlData(req) as MyHttpResponseData;
        var data = resp!.GetResultAsString();
        Trace.WriteLine(data);
        var json = JsonConvert.DeserializeObject(data);
        //       "Error": "Microsoft.Data.SqlClient.SqlException (0x80131904): Incorrect syntax near the keyword 'from'.\r\n   at Microsoft.Data.SqlClient.SqlConnection.OnError(SqlException exception, Boolean breakConnection, Action`1 wrapCloseInAction)\r\n   at Microsoft.Data.SqlClient.SqlInternalConnection.OnError(SqlException exception, Boolean breakConnection, Action`1 wrapCloseInAction)\r\n   at Microsoft.Data.SqlClient.TdsParser.ThrowExceptionAndWarning(TdsParserStateObject stateObj, Boolean callerHasConnectionLock, Boolean asyncClose)\r\n   at Microsoft.Data.SqlClient.TdsParser.TryRun(RunBehavior runBehavior, SqlCommand cmdHandler, SqlDataReader dataStream, BulkCopySimpleResultSet bulkCopyHandler, TdsParserStateObject stateObj, Boolean& dataReady)\r\n   at Microsoft.Data.SqlClient.SqlDataReader.TryConsumeMetaData()\r\n   at Microsoft.Data.SqlClient.SqlDataReader.get_MetaData()\r\n   at Microsoft.Data.SqlClient.SqlCommand.FinishExecuteReader(SqlDataReader ds, RunBehavior runBehavior, String resetOptionsString, Boolean isInternal, Boolean forDescribeParameterEncryption, Boolean shouldCacheForAlwaysEncrypted)\r\n   at Microsoft.Data.SqlClient.SqlCommand.CompleteAsyncExecuteReader(Boolean isInternal, Boolean forDescribeParameterEncryption)\r\n   at Microsoft.Data.SqlClient.SqlCommand.InternalEndExecuteReader(IAsyncResult asyncResult, Boolean isInternal, String endMethod)\r\n   at Microsoft.Data.SqlClient.SqlCommand.EndExecuteReaderInternal(IAsyncResult asyncResult)\r\n   at Microsoft.Data.SqlClient.SqlCommand.EndExecuteReaderAsync(IAsyncResult asyncResult)\r\n   at Microsoft.Data.SqlClient.SqlCommand.EndExecuteReaderAsyncCallback(IAsyncResult asyncResult)\r\n   at System.Threading.Tasks.TaskFactory`1.FromAsyncCoreLogic(IAsyncResult iar, Func`2 endFunction, Action`1 endAction, Task`1 promise, Boolean requiresSynchronization)\r\n--- End of stack trace from previous location ---\r\n   at Company.Function.SqlUtility.<>c__DisplayClass5_0.<<DoQuerySqlAsync>b__0>d.MoveNext() in c:\\Users\\calvinh\\source\\repos\\VSCodeAzFunc\\VSCodeAzFunc\\SqlUtil.cs:line 44\r\n--- End of stack trace from previous location ---\r\n   at Company.Function.GetSqlDataClass.GetSqlData(HttpRequestData req) in c:\\Users\\calvinh\\source\\repos\\VSCodeAzFunc\\VSCodeAzFunc\\GetSqlData.cs:line 69\r\nClientConnectionId:09234512-a7c8-4fdb-a7ba-f56ca6c78cb6\r\nError Number:156,State:1,Class:15"
        VerifyLogStrings(new[] {
            "Microsoft.Data.SqlClient.SqlException",
            });
    }

    [TestMethod]
    public async Task TestGetSqlDataCustomerBike()
    {
        var oc = new GetSqlDataClass(loggerFactory: this);
        var logger = CreateLogger(nameof(TestGetSqlData));
        logger.LogInformation($"Starting {nameof(TestGetSqlData)}");
        var req = new MyHttpRequestData(
            new MyFunctionContext(serviceProvider: this),
            new Uri($"localhost:7160/api/GetSqlData?Table=Customer&NumItems=10&Filter=%25bik%25")
        );
        var resp = await oc.GetSqlData(req) as MyHttpResponseData;
        var data = resp!.GetResultAsString();
        Trace.WriteLine(data);
        var json = JsonConvert.DeserializeObject(data);
    }


    [TestMethod]
    public async Task TestGetSecretsAsync()
    {
        var s = new Secrets();
        await s.GetSecretsAsync(logger: new MyLogger(TestContext), ShowDetails: true, act: (s) =>
        {
            Trace.WriteLine($"Sec prop {s.Name}  {s.ExpiresOn}");
        });
        VerifyLogStrings(new String[] {
            $"Got secret",
            "Sec prop MyFirstSecret",
            "Sec prop MySecondSecret"
        });
        //        Assert.Fail("must fail test to see output in OutputPane (Test Explorer (Test runner Output))");
    }



    [TestMethod]
    public async Task TestShowWPFUI()
    {
        await MyWindow.ShowDataInWindow(async (win) =>
        {
            await Task.Yield();
            var oc = new MyControl();
            win.MyUserControl.Content = oc;
        });

    }
    class MyControl : UserControl
    {
        TextBox _txtStatus;
        public MyControl()
        {
            var strxaml =
$@"<Grid
xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
xmlns:l=""clr-namespace:{GetType().Namespace};assembly={System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location)}"" 
        >
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height=""Auto""/>
                <RowDefinition Height=""*""/>
            </Grid.RowDefinitions>
            <StackPanel Grid.Row=""0"" HorizontalAlignment=""Left"" Height=""28"" VerticalAlignment=""Top"" Orientation=""Horizontal"" Margin=""0,20,0,0"">
                <Label Content=""# Events""/>
                <TextBox Text=""{{Binding NumEvents}}"" Width = ""50"" />
                <Label Content=""Driver_Id""/>
                <TextBox Text=""{{Binding Driver_Id}}"" Width = ""50"" />
                <CheckBox Content=""UpdateCheckPoint"" IsChecked = ""{{Binding UpdateCheckPoint}}"" ToolTip=""Call UpdateCheckPoint for each event processed""/>
                <CheckBox x:Name=""chkProcessEvents"" Content=""ProcessEvents"" ToolTip=""Listen for events and Log received events. Can use multiple machines""/>
                <Button x:Name = ""btnSendEvents"" Width = ""100"" Content=""SendEvents"" ToolTip=""Send event to update driver specified latitude by 1 degree""/>
            </StackPanel>
            <TextBox Grid.Row=""1"" x:Name=""_txtStatus"" FontFamily=""Consolas"" FontSize=""10""
            IsReadOnly=""True"" VerticalScrollBarVisibility=""Auto"" HorizontalScrollBarVisibility=""Auto"" IsUndoEnabled=""False"" VerticalAlignment=""Top""/>
        </Grid>
    </Grid>
";
            var grid = (System.Windows.Controls.Grid)(XamlReader.Parse(strxaml));
            this.Content = grid;
            _txtStatus = (TextBox)grid.FindName("_txtStatus");

        }
        public void AddStatusMsg(string msg, params object[] args)
        {
            if (_txtStatus != null)
            {
                // we want to read the threadid 
                //and time immediately on current thread
                var dt = string.Format("[{0,13}],TID={1,3},",
                    DateTime.Now.ToString("hh:mm:ss:fff"),
                    Environment.CurrentManagedThreadId);
                _txtStatus.Dispatcher.BeginInvoke(
                    new Action(() =>
                    {
                        // this action executes on main thread
                        if (args.Length == 0) // in cases the msg has embedded special chars like "{"
                        {
                            var str = string.Format(dt + "{0}" + Environment.NewLine, new object[] { msg });
                            _txtStatus.AppendText(str);
                        }
                        else
                        {
                            var str = string.Format(dt + msg + "\r\n", args);
                            _txtStatus.AppendText(str);
                        }
                        _txtStatus.ScrollToEnd();
                    }));
            }
        }

    }

}