using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace FunctionAppDemo;

public class FanFunction
{
    private readonly ILogger<FanFunction> _logger;

    public FanFunction(ILogger<FanFunction> logger)
    {
        _logger = logger;
    }

    [FunctionName(nameof(RunFanOrchestrator))]
    public async Task<List<string>> RunFanOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var outputs = new List<string>
        {
            await context.CallActivityAsync<string>(nameof(SayFanHello), "Grodno"),
            await context.CallActivityAsync<string>(nameof(SayFanHello), "Seattle"),
            await context.CallActivityAsync<string>(nameof(SayFanHello), "London")
        };

        return outputs;
    }

    [FunctionName(nameof(SayFanHello))]
    public Task<string> SayFanHello([ActivityTrigger] string name)
    {
        _logger.LogInformation($"Saying hello to {name}.");
        return Task.FromResult($"Hello {name}!");
    }

    [FunctionName(nameof(HttpTrigger))]
    public async Task<HttpResponseMessage> HttpTrigger(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
        [DurableClient] IDurableOrchestrationClient starter)
    {
        var instanceId = await starter.StartNewAsync(nameof(RunFanOrchestrator));
        _logger.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        return starter.CreateCheckStatusResponse(req, instanceId);
    }
}