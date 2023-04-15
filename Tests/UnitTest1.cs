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
        var oc = new GetSqlDataClass(loggerFactory:this);
        var logger = CreateLogger(nameof(TestGetSqlData));
        logger.LogInformation($"Starting {nameof(TestGetSqlData)}");
        var req = new MyHttpRequestData(
            new MyFunctionContext(serviceProvider:this),
            new Uri($"localhost:7160/api/GetSqlData?Table=Customer&NumItems=10")
        );
        var resp = await oc.GetSqlData(req) as MyHttpResponseData;
        var data = resp!.GetResultAsString();
        Trace.WriteLine(data);
        var json = JsonConvert.DeserializeObject(data);
    }
    [TestMethod]
    public async Task TestGetSqlDataCustomerBike()
    {
        var oc = new GetSqlDataClass(loggerFactory:this);
        var logger = CreateLogger(nameof(TestGetSqlData));
        logger.LogInformation($"Starting {nameof(TestGetSqlData)}");
        var req = new MyHttpRequestData(
            new MyFunctionContext(serviceProvider:this),
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