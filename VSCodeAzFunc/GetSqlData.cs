using System.Net;
using System.Reflection.PortableExecutable;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
                string? filter = Query?["Filter"]; // &Filter=where%20CompanyName%20like%20%27%25bike%25%27
                if (filter == null)
                {
                    filter = "";
                }
                else
                {
                }
                var tableNameStr = Query?["Table"]; // &TableName=Customer
                if (tableNameStr == null)
                {
                    tableNameStr = "Customer";
                }
                if (Query?["PrettyPrint"] != null) // &PrettyPrint=1
                {
                    prettyprint = true;
                }
                var tableSchema = Query?["TableSchema"]; // https://calvinhvscode.azurewebsites.net/api/GetSqlData?TableSchema=INFORMATION_SCHEMA&Table=TABLES&NumItems=40&PrettyPrint=1
                if (tableSchema == null)
                {
                    tableSchema = "SalesLT";
                }

                response.Headers.Add("Content-Type", "application/json");

                using var sqlUtil = await SqlUtility.CreateSqlUtilityAsync();
                var query = $@"select TOP {numItems} * from {tableSchema}.{tableNameStr} {filter}";
                var jarray = new JArray();
                await sqlUtil.DoQuerySqlAsync(query, "get data", (rdr) =>
                {
                    //var obj = new
                    //{
                    //    CustomerID = rdr["CustomerID"],
                    //    LastName = rdr["LastName"],
                    //    FirstName = rdr["FirstName"],
                    //    CompanyName = rdr["CompanyName"],
                    //    EmailAddress = rdr["EmailAddress"],
                    //    Phone = rdr["Phone"],
                    //};
                    //var arr = new object[rdr.FieldCount];
                    //var vls = rdr.GetSqlValues(arr);
                    //var columns = Enumerable.Range(0, rdr.FieldCount)
                    //                        .Select(rdr.GetName)
                    //                        .ToList();

                    var js = new JObject();
                    for (var i = 0; i < rdr.FieldCount; i++)
                    {
                        var colname = rdr.GetName(i);
                        var val = rdr.GetValue(i);
                        js.Add(colname, val.ToString());
                    }
                    jarray.Add(js);
                });
                var json = JsonConvert.SerializeObject(jarray, prettyprint ? SqlUtility.jsonsettingsIndented : null);
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
