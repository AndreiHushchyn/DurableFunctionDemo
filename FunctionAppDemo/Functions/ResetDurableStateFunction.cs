/*
 * https://dev.to/cgillum/resetting-your-durable-task-hubs-azure-storage-state-2ome
 */

using System.Net.Http;
using System.Threading.Tasks;
using DurableTask.AzureStorage;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace FunctionAppDemo.Functions;

public class ResetDurableStateFunction
{
    private readonly INameResolver _nameResolver;
    private readonly ILogger<ResetDurableStateFunction> _logger;

    public ResetDurableStateFunction(INameResolver nameResolver, ILogger<ResetDurableStateFunction> logger)
    {
        _nameResolver = nameResolver;
        _logger = logger;
    }

    [FunctionName(nameof(ResetDurableState))]
    public async Task<HttpResponseMessage> ResetDurableState(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestMessage req,
        [DurableClient] IDurableClient client)
    {
        var connString = _nameResolver.Resolve("AzureWebJobsStorage");
        var settings = new AzureStorageOrchestrationServiceSettings
        {
            StorageConnectionString = connString,
            TaskHubName = client.TaskHubName,
        };

        var storageService = new AzureStorageOrchestrationService(settings);

        _logger.LogInformation("Deleting all storage resources for task hub {taskHub}.", settings.TaskHubName);
        await storageService.DeleteAsync();

        // Wait for a minute since Azure Storage won't let us immediately recreate resources with the same names as before.
        //_logger.LogInformation("The delete operation completed. Waiting before recreating.");
        //await Task.Delay(TimeSpan.FromSeconds(30));
        //log.LogInformation("Recreating storage resources for task hub {taskHub}.", settings.TaskHubName);
        //await storageService.CreateIfNotExistsAsync();

        return req.CreateResponse(System.Net.HttpStatusCode.OK);
    }
}