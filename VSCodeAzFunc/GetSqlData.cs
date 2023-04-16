using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Company.Function
{
    public class GetSqlDataClass
    {
        private readonly ILogger _logger;

        public GetSqlDataClass(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GetSqlDataClass>();
        }

        [Function(nameof(GetSqlData))]
        public async Task<HttpResponseData> GetSqlData([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            try
            {
                var Query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var prettyprint = false;
                string? numItemsStr = Query?["NumItems"];
                if (string.IsNullOrEmpty(numItemsStr))
                {
                    numItemsStr = "100";
                }
                var numItems = int.Parse(numItemsStr);
                string? filter = Query?["Filter"];
                if (filter == null)
                {
                    filter = "";
                }
                else
                {
                    filter = $"where CompanyName like '{filter}'";
                }
                if (Query?["PrettyPrint"] != null)
                {
                    prettyprint = true;
                }

                response.Headers.Add("Content-Type", "application/json");

                using var sqlUtil = await SqlUtility.CreateSqlUtilityAsync();
                var query = $@"select TOP {numItems} * from SalesLT.Customer {filter}";
                var lst = new List<object>();
                await sqlUtil.DoQuerySqlAsync(query, "get customers", (p) =>
                {
                    var obj = new
                    {
                        CustomerID = p["CustomerID"],
                        LastName = p["LastName"],
                        FirstName = p["FirstName"],
                        CompanyName = p["CompanyName"],
                        EmailAddress = p["EmailAddress"],
                        Phone = p["Phone"],
                    };
                    //                result += $"{pcid} {name}";
                    lst.Add(obj);

                });
                var json = JsonConvert.SerializeObject(lst, prettyprint ? SqlUtility.jsonsettingsIndented : null);
                await response.WriteStringAsync(json);
            }
            catch (System.Exception ex)
            {
                response.WriteString(ex.ToString());
            }

            return response;
        }
    }
}
