using Azure.Core.Diagnostics;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;

namespace Company.Function
{
    public class Secrets
    {
        public double ElapsedSecs = 0.0;
        public Task GetSecretsAsync(ILogger logger, bool ShowDetails = false, Action<SecretProperties>? act = null)
        {
            //https://github.com/Azure/azure-sdk-for-net/issues/4645
            //Environment.SetEnvironmentVariable("AzureServicesAuthConnectionString", "RunAs=Developer;DeveloperTool=VisualStudio");
            // Setup a listener to monitor logged events.
            using AzureEventSourceListener listener = AzureEventSourceListener.CreateConsoleLogger();
            var taskGetSecrets = Task.Run(async () =>
            {
                SecretClientOptions optionsSecret = new SecretClientOptions()
                {
                    Retry =
                    {
                            Delay= TimeSpan.FromSeconds(8),
                            MaxDelay = TimeSpan.FromSeconds(16),
                            MaxRetries = 5,
                            Mode = RetryMode.Exponential,
                            NetworkTimeout = TimeSpan.FromSeconds(10),

                     }
                };
                var kvaultURI = Environment.GetEnvironmentVariable("MyAzureKeyVaultURI"); // https://calvinhtestkeyvault.vault.azure.net/
                //var aztokenprovider = new AzureServiceTokenProvider // Microsoft.Azure.Services.AppAuthentication has been retired and is no longer supported or maintained. It is replaced by the Azure Identity client library available for .NET, Java, TypeScript and Python.
                var userAssignedClientId = "77b9ea1f-7731-4e29-9723-8a6384cc70b2";
                var optionsCred = new DefaultAzureCredentialOptions()
                {
                    Diagnostics =
                    {
                            LoggedHeaderNames = { "x-ms-request-id" },
                            LoggedQueryParameters = { "api-version" },
                            IsLoggingContentEnabled = true
                    },
                    ManagedIdentityClientId = userAssignedClientId
                };
                try
                {
                    var clientSecret = new SecretClient(new Uri(kvaultURI!), new DefaultAzureCredential(optionsCred), optionsSecret);

                    var sw = Stopwatch.StartNew();
                    KeyVaultSecret secret = await clientSecret.GetSecretAsync("MyFirstSecret");
                    string secretValue = secret.Value;
                    sw.Stop();
                    ElapsedSecs = sw.Elapsed.TotalSeconds;
                    logger.LogInformation($"Got secret {secret.Name} = {secretValue} in {sw.Elapsed.TotalSeconds:n1} seconds");
                    if (ShowDetails)
                    {
                        var computerName = Environment.GetEnvironmentVariable("COMPUTERNAME")!;
                        //                      if (computerName.StartsWith("CALVINH")) // only want to show secrets when running locally
                        {
                            var secprops = clientSecret.GetPropertiesOfSecretsAsync(); // returns AsyncPageable
                            await foreach (Page<SecretProperties> page in secprops.AsPages())
                            {
                                foreach (var secprop in page.Values)
                                {
                                    act?.Invoke(secprop);
                                }
                                //page.ContinuationToken == null indicates no more pages
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Can't get secret {ex}");
                    /*
Can't get secret Azure.Identity.CredentialUnavailableException: DefaultAzureCredential failed to retrieve a token from the included credentials. See the troubleshooting guide for more information. https://aka.ms/azsdk/net/identity/defaultazurecredential/troubleshoot
- EnvironmentCredential authentication unavailable. Environment variables are not fully configured. See the troubleshooting guide for more information. https://aka.ms/azsdk/net/identity/environmentcredential/troubleshoot
- ManagedIdentityCredential authentication unavailable. Multiple attempts failed to obtain a token from the managed identity endpoint.
- Process "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\CommonExtensions\Microsoft\Asal\TokenService\Microsoft.Asal.TokenService.exe" has failed with unexpected error: TS003: Error, TS001: This account 'calvinh@microsoft.com' needs re-authentication. Please go to Tools->Options->Azure Services Authentication, and re-authenticate the account you want to use..
- Please run 'az login' to set up account

fixed: VS=>Tools->Options->Azure Services Authentication, and re-authenticate the account you want to use
                     */
                    throw;
                }
            });
            return taskGetSecrets;
        }
    }
}
