namespace Tests;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Diagnostics;
using System.Collections.Concurrent;

public class TestBase : ILoggerFactory, IServiceProvider
{
    public JsonSerializerSettings jsonsettingsIndented = new JsonSerializerSettings()
    {
        Formatting = Formatting.Indented,
    };
    internal MyTextWriterTraceListener MyTextWriterTraceListener;
    public const string ContextPropertyLogFile = "LogFileName";

    public TestContext TestContext { get; set; }
#pragma warning disable CS8618 //non-nullable field exiting constructor
    public TestBase()
    {
        var jsonFile = Path.Combine(new FileInfo(Path.Combine(Environment.CurrentDirectory, @"..\..\..\..")).FullName, "VSCodeAzFunc", "local.settings.json");
        if (!File.Exists(jsonFile))
        {
            throw new Exception($"Can't find {jsonFile}");
        }
        var jsonSettings = File.ReadAllText($"local.settings.json");
        dynamic? settingsValues = JsonConvert.DeserializeObject(jsonSettings) ?? throw new NullReferenceException("settings");
        foreach (var kvp in settingsValues["Values"])
        {
            Environment.SetEnvironmentVariable(kvp.Name.ToString(), kvp.Value.ToString());
        }
    }
    [TestInitialize]
    public void TestInitialize()
    {
        var outfile = System.Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\Desktop\TestOutput.txt");
        TestContext.Properties[ContextPropertyLogFile] = outfile;
        this.MyTextWriterTraceListener = new MyTextWriterTraceListener(
            outfile,
            TestContext,
            MyTextWriterTraceListener.MyTextWriterTraceListenerOptions.OutputToFileAsync | MyTextWriterTraceListener.MyTextWriterTraceListenerOptions.AddDateTime
            );
    }
    [TestCleanup]
    public void TestCleanup()
    {
        MyTextWriterTraceListener.Dispose();
    }
    public void VerifyLogStrings(string strings)
    {
        this.MyTextWriterTraceListener.VerifyLogStrings(strings);
    }
    public int VerifyLogStrings(IEnumerable<string> strsExpected, bool ignoreCase = false)
    {
        return this.MyTextWriterTraceListener.VerifyLogStrings(strsExpected, ignoreCase);
    }


    public ILogger CreateLogger(string categoryName)
    {
        return new MyLogger(TestContext);
    }

    public void AddProvider(ILoggerProvider provider)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
    }

    public object? GetService(Type serviceType)
    {
        if (serviceType.Name.StartsWith("ILogger")  // "ILogger`1"
            && serviceType.GenericTypeArguments.Length > 0)
        {
            switch (serviceType.GenericTypeArguments[0].Name)
            {
                // case "DriverFunctions":
                //     var tt = new MyLogger<DriverFunctions>(TestContext);
                //     var x = tt.GetType();
                //     var xi = Activator.CreateInstance(x, TestContext);
                //     return xi;
                // case "HTTPExampleClass":
                //     return new MyLogger<HTTPExampleClass>(TestContext);
                default:
                    throw new NotImplementedException();
            }
            //var logger = CreateLogger(serviceType.GenericTypeArguments[0].Name);
            //return logger;
        }
        throw new NotImplementedException();
    }
}
public class MyLogger : ILogger
{
    private readonly TestContext _testContext;

    public MyLogger(TestContext testContext)
    {
        _testContext = testContext;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var str = formatter(state, exception);
        _testContext.WriteLine($"{str}");
        Trace.WriteLine(str);
        Console.WriteLine(str);
    }
}
public class MyLogger<T> : MyLogger, ILogger<T>
{
    public MyLogger(TestContext testContext) : base(testContext)
    {
    }

}
public class MyFunctionContext : FunctionContext
{
    public MyFunctionContext(IServiceProvider serviceProvider)
    {
        InstanceServices = serviceProvider;
    }
    public override string InvocationId => throw new NotImplementedException();

    public override string FunctionId => throw new NotImplementedException();

    public override TraceContext TraceContext => throw new NotImplementedException();

    public override BindingContext BindingContext => throw new NotImplementedException();

    public override RetryContext RetryContext => throw new NotImplementedException();

    public override IServiceProvider InstanceServices { get; set; }

    public override FunctionDefinition FunctionDefinition => throw new NotImplementedException();

    public override IDictionary<object, object> Items { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public override IInvocationFeatures Features => throw new NotImplementedException();
}
public class MyHttpRequestData : HttpRequestData
{
    private readonly FunctionContext _functionContext;
    private readonly Uri _uri;

    public MyHttpRequestData(FunctionContext functionContext, Uri uri) : base(functionContext)
    {
        _functionContext = functionContext;
        _uri = uri;
    }
    public override Stream Body => throw new NotImplementedException();

    public override HttpHeadersCollection Headers => throw new NotImplementedException();

    public override IReadOnlyCollection<IHttpCookie> Cookies => throw new NotImplementedException();

    public override Uri Url => _uri;

    public override IEnumerable<ClaimsIdentity> Identities => throw new NotImplementedException();

    public override string Method => throw new NotImplementedException();

    public override HttpResponseData CreateResponse()
    {
        var response = new MyHttpResponseData(_functionContext);
        return response;
    }
}
public class MyHttpResponseData : HttpResponseData
{
    public MyHttpResponseData(FunctionContext functionContext) : base(functionContext)
    {
        Body = new MemoryStream();
        Headers = new HttpHeadersCollection();
    }
    public override HttpStatusCode StatusCode { get; set; }
    public override HttpHeadersCollection Headers { get; set; }
    public override Stream Body { get; set; }
    public string GetResultAsString()
    {
        var buffer = new byte[Body.Length];
        Body.Seek(0, SeekOrigin.Begin);
        Body.Read(buffer, offset: 0, (int)Body.Length);
        var str = Encoding.UTF8.GetString(buffer);
        return str;
    }
    public override HttpCookies Cookies => throw new NotImplementedException();
}

    public class MyTextWriterTraceListener : TextWriterTraceListener, IDisposable
    {
        private readonly string LogFileName;
        private readonly TestContext testContext;
        private readonly MyTextWriterTraceListenerOptions options;
        public List<string> _lstLoggedStrings;
        readonly ConcurrentQueue<string> _qLogStrings;

        private Task taskOutput;
        private CancellationTokenSource ctsBatchProcessor;

        [Flags]
        public enum MyTextWriterTraceListenerOptions
        {
            None = 0x0,
            AddDateTime = 0x1,
            /// <summary>
            /// Some tests take a long time and if they fail, it's difficult to examine any output
            /// Also, getting the test output is very cumbersome: need to click on the Additional Output, then right click/Copy All output, then paste it somewhere
            /// Plus there's a bug that the right-click/copy all didn't copy all in many versions.
            /// Turn this on to output to a file in real time. Open the file in VS to watch the test progress
            /// </summary>
            OutputToFile = 0x2,
            /// <summary>
            /// Output to file while running takes a long time. Do it in batches every second. Ensure disposed
            /// so much faster (20000 lines takes minutes vs <500 ms)
            /// </summary>
            OutputToFileAsync = 0x4
        }

        public MyTextWriterTraceListener(string LogFileName, TestContext testContext, MyTextWriterTraceListenerOptions options = MyTextWriterTraceListenerOptions.OutputToFileAsync)
        {
            if (string.IsNullOrEmpty(LogFileName))
            {
                throw new InvalidOperationException("Log filename is null");
            }
            this.LogFileName = LogFileName;
            this.testContext = testContext;
            this.options = options;
            _lstLoggedStrings = new List<string>();
            OutputToLogFileWithRetryAsync(() =>
            {
                File.WriteAllText(LogFileName, string.Empty);
            }, testContext).Wait();
            File.Delete(LogFileName);
            Trace.Listeners.Clear(); // else Debug.Writeline can cause infinite recursion because the Test runner adds a listener.
            Trace.Listeners.Add(this);
            if (this.options.HasFlag(MyTextWriterTraceListenerOptions.OutputToFileAsync))
            {
                _qLogStrings = new ConcurrentQueue<string>();
                this.taskOutput = Task.Run(async () =>
                {
                    try
                    {
                        this.ctsBatchProcessor = new CancellationTokenSource();
                        bool fShutdown = false;
                        while (!fShutdown)
                        {
                            if (this.ctsBatchProcessor.IsCancellationRequested)
                            {
                                fShutdown = true; // we need to go through one last time to clean up
                            }
                            var lstBatch = new List<string>();
                            while (!_qLogStrings.IsEmpty)
                            {
                                if (_qLogStrings.TryDequeue(out var msg))
                                {
                                    lstBatch.Add(msg);
                                }
                            }
                            if (lstBatch.Count > 0)
                            {
                                await MyTextWriterTraceListener.OutputToLogFileWithRetryAsync(() =>
                                {
                                    File.AppendAllLines(LogFileName, lstBatch);
                                }, testContext);
                            }
                            try
                            {
                                await Task.Delay(TimeSpan.FromSeconds(1), this.ctsBatchProcessor.Token);
                            }
                            catch (OperationCanceledException)
                            {
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                    }
                });
            }
        }

        public override void Write(object? o)
        {
            Write(o!.ToString());
        }
        public override void Write(string? message)
        {
            if (message == null)
            {
                return;
            }
            _lstLoggedStrings.Add(message);
            var dt = string.Empty;
            if (this.options.HasFlag(MyTextWriterTraceListenerOptions.AddDateTime))
            {
                dt = string.Format("[{0}],",
                    DateTime.Now.ToString("hh:mm:ss:fff")
                    ) + $"{Thread.CurrentThread.ManagedThreadId,2} ";
            }
            message = dt + message.Replace("{", "{{").Replace("}", "}}");
            this.testContext.WriteLine(message);
            if (this.options.HasFlag(MyTextWriterTraceListenerOptions.OutputToFileAsync))
            {
                _qLogStrings.Enqueue(message);
            }

            if (this.options.HasFlag(MyTextWriterTraceListenerOptions.OutputToFile))
            {
                try
                {
                    if (this.taskOutput != null)
                    {
                        this.taskOutput.Wait();
                        if (!this.taskOutput.IsCompleted) // faulted?
                        {
                            Trace.WriteLine(("Output Task faulted"));
                        }
                    }
                    this.taskOutput = Task.Run(async () =>
                    {
                        await OutputToLogFileWithRetryAsync(() =>
                        {
                            File.AppendAllText(LogFileName, message + Environment.NewLine);
                        }, testContext);
                    });
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        public static async Task OutputToLogFileWithRetryAsync(Action actWrite, TestContext tcontext)
        {
            var nRetry = 0;
            var success = false;
            while (nRetry++ < 10)
            {
                try
                {
                    actWrite();
                    success = true;
                    break;
                }
                catch (IOException)
                {
                }
                await Task.Delay(TimeSpan.FromSeconds(0.3));
            }
            if (!success)
            {
                tcontext?.WriteLine($"Error writing to log #retries ={nRetry}");
            }
        }

        public override void WriteLine(object? o)
        {
            Write(o!.ToString());
        }
        public override void WriteLine(string? message)
        {
            Write(message);
        }
        public void WriteLine(string str, params object[] args)
        {
            Write(string.Format(str, args));
        }
        public int VerifyLogStrings(IEnumerable<string> strsExpected, bool ignoreCase = false)
        {
            int numFailures = 0;
            var firstFailure = string.Empty;
            bool IsIt(string strExpected, string strActual)
            {
                var hasit = false;
                if (!string.IsNullOrEmpty(strActual))
                {
                    if (ignoreCase)
                    {
                        hasit = strActual.ToLower().Contains(strExpected.ToLower());
                    }
                    else
                    {
                        hasit = strActual.Contains(strExpected);
                    }
                }
                return hasit;
            }
            foreach (var str in strsExpected)
            {
                if (!_lstLoggedStrings.Where(s => IsIt(str, s)).Any())
                {
                    numFailures++;
                    if (string.IsNullOrEmpty(firstFailure))
                    {
                        firstFailure = str;
                    }
                    WriteLine($"Expected '{str}'");
                }
            }
            Assert.AreEqual(0, numFailures, $"1st failure= '{firstFailure}'");
            return numFailures;
        }

        public int VerifyLogStrings(string strings, bool ignoreCase = false)
        {
            var strs = strings.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return VerifyLogStrings(strs, ignoreCase);
        }
        protected override void Dispose(bool disposing)
        {
            Trace.Listeners.Remove(this);
            if (this.taskOutput != null)
            {
                this.ctsBatchProcessor.Cancel();
                this.taskOutput.Wait();
            }
            base.Dispose(disposing);
        }
    }


