using System.Diagnostics;
using System.Runtime.Intrinsics.Arm;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Company.Function;
public class SqlUtility : IDisposable
{
    public SqlConnection SqlConn = new SqlConnection();
    public string LastQuery = string.Empty;
    public static JsonSerializerSettings jsonsettingsIndented = new JsonSerializerSettings()
    {
        Formatting = Formatting.Indented,
    };
    public static async Task<SqlUtility> CreateSqlUtilityAsync()
    {
        var sq = new SqlUtility();
        await sq.InitializeAsync();
        return sq;
    }
    async Task InitializeAsync()
    {
        var cstr = Environment.GetEnvironmentVariable("SqlConnString");
        SqlConn.ConnectionString = cstr;
        await SqlConn.OpenAsync();
    }
    public Task<JArray> DoQuerySqlAsync(string query, string desc, Action<SqlDataReader> act, CancellationToken tkn = default, int timeoutSecs = 7 * 60) => Task.Run(async () =>
    {
        JArray jarray = new JArray();
        {
            LastQuery = query;
            Trace.WriteLine($"Executing Sql Query {query}");
            var sw = Stopwatch.StartNew();
            var timeQuery = TimeSpan.FromSeconds(0);
            var nCnt = 0;
            try
            {
                //            using var sqlconn = new SqlConnection(ConnstrRPS);
                if (SqlConn.State != System.Data.ConnectionState.Open)
                {
                    await SqlConn.OpenAsync();
                }
                var cmd = new SqlCommand(query, SqlConn);
                if (timeoutSecs > 0)
                {
                    cmd.CommandTimeout = timeoutSecs; //            cmd.CommandTimeout = 360;//seconds
                }
                if (query.IndexOf("select", StringComparison.OrdinalIgnoreCase) >= 0)
                {

                    using var reader = await cmd.ExecuteReaderAsync(tkn);
                    timeQuery = sw.Elapsed;
                    Trace.WriteLine($"Query done in {timeQuery.TotalSeconds:n2} secs. Parsing Query Result");
                    if (tkn.IsCancellationRequested)
                    {
                        throw new OperationCanceledException();
                    }
                    while (await reader.ReadAsync(tkn))
                    {
                        act(reader);
                        var js = new JObject();
                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            var colname = reader.GetName(i);
                            var val = reader.GetValue(i);
                            js.Add(colname, val.ToString());
                        }
                        jarray.Add(js);
                        nCnt++;
                        if (tkn.IsCancellationRequested)
                        {
                            throw new OperationCanceledException();
                        }
                    }
                }
                else if (query.IndexOf("insert", StringComparison.OrdinalIgnoreCase) >= 0
                        || query.IndexOf("update", StringComparison.OrdinalIgnoreCase) >= 0
                        || query.IndexOf("delete", StringComparison.OrdinalIgnoreCase) >= 0
                    )
                {
                    nCnt = await cmd.ExecuteNonQueryAsync(tkn); // rows affected
                    var jObject = new JObject();
                    jObject.Add("RowsAffected", nCnt.ToString());
                    jarray.Add(jObject);
                    timeQuery = sw.Elapsed;
                }
                else
                {
                    var obj = await cmd.ExecuteScalarAsync(tkn);
                    var jobj = new JObject();
                    jobj.Add("Scalar", obj == null ? "null" : JsonConvert.SerializeObject(obj));
                    jarray.Add(jobj);
                }
                var timeParse = sw.Elapsed - timeQuery;
                Trace.WriteLine($"Executed query {desc} #Results = {nCnt}. ParseTime={timeParse.TotalSeconds:n1}. TotTime= {(timeQuery + timeParse).TotalSeconds:n2} secs");
                sw.Stop();
            }
            catch (Exception ex)
            {
                var jobject = new JObject();
                jobject.Add("Error", ex.ToString());
                jarray.Add(jobject);
                Trace.WriteLine($"Error getting {desc}\r\n{ex.Message}");
            }
        }
        return jarray;
    });


    public void Dispose()
    {
        SqlConn?.Dispose();
    }
}
