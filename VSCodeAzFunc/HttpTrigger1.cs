using System.Diagnostics;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Company.Function
{
    public class HttpTrigger1Class
    {
        private readonly ILogger _logger;

        public HttpTrigger1Class(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<HttpTrigger1Class>();
        }

        [Function(nameof(HttpTrigger1))]
        public async Task< HttpResponseData> HttpTrigger1([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            try 
            {
                var dict = new DictionaryLib.DictionaryLib(DictionaryLib.DictionaryType.Small);
                _logger.LogInformation("C# HTTP trigger function processed a request.");

                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

                await Task.Yield();
                response.WriteString($"MyAZFunc v2 {DateTime.Now}. Word of the day: '{dict.RandomWord()}'");

                var sqlResult = await GetSqlDataAsync(response);

            }
            catch (Exception ex)
            {
                response.WriteString(ex.ToString());
            }


            return response;
        }

        public static async Task<string> GetSqlDataAsync(HttpResponseData response)
        {
            var result = string.Empty;
            using var sqlUtil = await SqlUtility.CreateSqlUtilityAsync();
            var query = @"select * from SalesLT.ProductCategory";
            var lst = new List<object>();
            await sqlUtil.DoQuerySqlAsync(query, "get prods", (p) =>
            {
                var pcid = p["ProductCategoryID"];
                var parent = p["ParentProductCategoryID"];
                var name = p["name"];
                var rowguid = p["rowguid"];
                var ModifiedDate = p["ModifiedDate"];
                Trace.WriteLine($"{name}");
                result += $"{pcid} {name}";
                lst.Add(new {
                    pcid,
                    name, 
                    ModifiedDate
                });
            });
            var json = JsonConvert.SerializeObject(lst, SqlUtility.jsonsettingsIndented);
            await response.WriteStringAsync(json);
            return result;

        }
    }
}
