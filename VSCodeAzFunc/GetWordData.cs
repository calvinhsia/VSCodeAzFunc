using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Company.Function
{
    public class GetWordDataClass
    {
        private readonly ILogger _logger;
        public Random? random = null;

        public GetWordDataClass(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GetWordDataClass>();
            if (random == null)
            {
                random = new Random();
            }
        }

        [Function(nameof(GetWordData))]
        public async Task<HttpResponseData> GetWordData([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
        {
            var response = req.CreateResponse(HttpStatusCode.OK);
            try
            {
                response.Headers.Add("Content-Type", "application/json");
                var Query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                var sw = Stopwatch.StartNew();
                (string randWord, string grid) = CreateGrid();
                sw.Stop();
                _logger.LogInformation($"{nameof(GetWordData)} RandWord = {randWord} #secs = {sw.Elapsed.TotalSeconds:n3}");

                var jObject = new JObject();
                jObject.Add("RandWord", randWord);
                jObject.Add("Grid", grid);
                jObject.Add("Calculated in ", sw.Elapsed.TotalSeconds.ToString("n3"));

                await response.WriteStringAsync(JsonConvert.SerializeObject(jObject, SqlUtility.jsonsettingsIndented));
            }
            catch (System.Exception ex)
            {
                var jobject = new JObject();
                jobject.Add("Error", ex.ToString());
                var json = JsonConvert.SerializeObject(jobject, SqlUtility.jsonsettingsIndented);
                await response.WriteStringAsync(json);
            }


            return response;
        }
        int nRows = 4;
        int nCols = 4;
        (string randWord, string grid) CreateGrid()
        {
            var dict = new DictionaryLib.DictionaryLib(DictionaryLib.DictionaryType.Small, random: random); // https://learn.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-entities?tabs=csharp
            var directions = Enumerable.Range(0, 8).ToArray();
            var randWord = string.Empty;
            var resGrid = string.Empty;
            var isGood = false;
            while (!isGood)
            {
                while (true)
                {
                    randWord = dict.RandomWord();
                    if (randWord.Length > 9 && randWord.Length < nRows * nCols)
                    {
                        break;
                    }
                }
                var arrGrid = new char[nRows, nCols];
                Func<int, int, int, bool>? recurlam = null;
                recurlam = (r, c, ndxw) =>
                {
                    arrGrid[r, c] = randWord[ndxw];
                    if (ndxw == randWord.Length - 1)
                    {
                        isGood = true;
                        return true;
                    }
                    directions = directions.OrderBy(x => random!.Next()).ToArray();
                    for (var idir = 0; idir < 7; idir++)
                    {
                        isGood = true;
                        var newr = r;
                        var newc = c;
                        switch (directions[idir])
                        {
                            case 0:
                                newr -= 1;
                                newc -= 1;
                                break;
                            case 1:
                                newr -= 1;
                                break;
                            case 2:
                                newr -= 1;
                                newc += 1;
                                break;
                            case 3:
                                newc -= 1;
                                break;
                            case 4:
                                newc += 1;
                                break;
                            case 5:
                                newr += 1;
                                newc -= 1;
                                break;
                            case 6:
                                newr += 1;
                                break;
                            case 7:
                                newr += 1;
                                newc += 1;
                                break;
                        }
                        if (newr < 0 || newr >= nRows || newc < 0 || newc >= nCols)
                        {
                            isGood = false;
                        }
                        else
                        {
                            if (arrGrid[newr, newc] != '\0')
                            {
                                isGood = false;
                            }
                        }
                        if (isGood)
                        {
                            if (recurlam!(newr, newc, ndxw + 1))
                            {
                                break;
                            }
                            else
                            {
                                isGood = false;
                            }
                        }
                    }
                    if (!isGood)
                    {
                        arrGrid[r, c] = '\0';
                    }
                    return isGood;
                };
                recurlam(0, 0, 0);
                if (isGood)
                {
                    for (int i = 0; i < nRows; i++)
                    {
                        for (int j = 0; j < nCols; j++)
                        {
                            var c = arrGrid[i, j];
                            if (c == '\0')
                            {
                                c = '_';
                            }
                            resGrid += c;
                        }
                    }
                }
            }
            return (randWord, resGrid);
        }
    }
}
