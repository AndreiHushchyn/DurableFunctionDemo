using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace FunctionAppDemo.Functions;

public class FanFunction
{
    const int Workload = 10;
    private readonly ILogger<FanFunction> _logger;

    public FanFunction(ILogger<FanFunction> logger)
    {
        _logger = logger;
    }

    [FunctionName(nameof(RunFanOrchestrator))]
    public async Task<int> RunFanOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var batchCount = await context.CallActivityAsync<int>(nameof(GetBatchCount), Workload);

        if (!context.IsReplaying)
        {
            _logger.LogInformation($"Starting batch processing. at {context.CurrentUtcDateTime}");
        }
        
        var tasks = new Task<int>[batchCount];
        for (var i = 0; i < batchCount; i++)
        {
            tasks[i] = context.CallActivityAsync<int>(nameof(ProcessBatch), i);
        }

        await Task.WhenAll(tasks);

        var result = tasks.Sum(t => t.Result);

        return result;
    }

    [FunctionName(nameof(GetBatchCount))]
    public Task<int> GetBatchCount([ActivityTrigger] int workload)
    {
        var n = workload / 2;
        _logger.LogInformation($"{n} batches to process");
        return Task.FromResult(n);
    }

    [FunctionName(nameof(ProcessBatch))]
    public Task<int> ProcessBatch([ActivityTrigger] int batch)
    {
        _logger.LogInformation($"Processing batch {batch}.");
        var processedItems = new Random().Next(0, 10);
        return Task.FromResult(processedItems);
    }

    [FunctionName(nameof(TriggerFan))]
    public async Task<HttpResponseMessage> TriggerFan(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
        [DurableClient] IDurableOrchestrationClient starter)
    {
        var instanceId = await starter.StartNewAsync(nameof(RunFanOrchestrator));
        _logger.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        return starter.CreateCheckStatusResponse(req, instanceId);
    }
}