using System.Diagnostics;
using Microsoft.Data.SqlClient;

namespace Company.Function;
public class SqlUtility : IDisposable
{
    public SqlConnection SqlConn = new SqlConnection();
    public string LastQuery = string.Empty;
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
    public Task DoQuerySqlAsync(string query, string desc, Action<SqlDataReader> act, CancellationToken tkn = default, int timeoutSecs = 7 * 60) => Task.Run(async () =>
    {
        {
            LastQuery = query;
            Trace.WriteLine($"Executing Sql Query {query}");
            var sw = Stopwatch.StartNew();
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
                using var reader = await cmd.ExecuteReaderAsync(tkn);
                var timeQuery = sw.Elapsed;
                Trace.WriteLine($"Query done in {timeQuery.TotalSeconds:n2} secs. Parsing Query Result");
                var nCnt = 0;
                if (tkn.IsCancellationRequested)
                {
                    throw new OperationCanceledException();
                }
                while (await reader.ReadAsync(tkn))
                {
                    act(reader);
                    nCnt++;
                    if (tkn.IsCancellationRequested)
                    {
                        throw new OperationCanceledException();
                    }
                }
                var timeParse = sw.Elapsed - timeQuery;
                Trace.WriteLine($"Executed query {desc} #Results = {nCnt}. ParseTime={timeParse.TotalSeconds:n1}. TotTime= {(timeQuery + timeParse).TotalSeconds:n2} secs");
                sw.Stop();
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error getting {desc}\r\n{ex.Message}");
                throw;
            }
        }
    });


    public void Dispose()
    {
        SqlConn?.Dispose();
    }
}
