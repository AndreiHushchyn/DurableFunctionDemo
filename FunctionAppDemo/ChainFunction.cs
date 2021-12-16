using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace FunctionAppDemo;

public class ChainFunction
{
    private readonly ILogger<ChainFunction> _logger;

    public ChainFunction(ILogger<ChainFunction> logger)
    {
        _logger = logger;
    }

    [FunctionName(nameof(RunOrchestrator))]
    public async Task<List<string>> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var outputs = new List<string>
        {
            await context.CallActivityAsync<string>(nameof(SayHello), "Grodno"),
            await context.CallActivityAsync<string>(nameof(SayHello), "Seattle"),
            await context.CallActivityAsync<string>(nameof(SayHello), "London")
        };

        return outputs;
    }

    [FunctionName(nameof(SayHello))]
    public Task<string> SayHello([ActivityTrigger] string name)
    {
        _logger.LogInformation($"Saying hello to {name}.");
        return Task.FromResult($"Hello {name}!");
    }

    [FunctionName(nameof(HttpTrigger))]
    public async Task<HttpResponseMessage> HttpTrigger(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
        [DurableClient] IDurableOrchestrationClient starter)
    {
        var instanceId = await starter.StartNewAsync(nameof(RunOrchestrator));
        _logger.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        return starter.CreateCheckStatusResponse(req, instanceId);
    }
}