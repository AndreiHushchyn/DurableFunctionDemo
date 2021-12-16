using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DurableTask.Core.Exceptions;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace FunctionAppDemo.Functions;

public class ChainFunctionWithRetry
{
    private readonly ILogger<ChainFunction> _logger;

    public ChainFunctionWithRetry(ILogger<ChainFunction> logger)
    {
        _logger = logger;
    }

    [FunctionName(nameof(RunChainOrchestratorWithRetry))]
    public async Task<List<string>> RunChainOrchestratorWithRetry([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var retryAttempt = context.GetInput<int>();

        if (!context.IsReplaying && retryAttempt > 0)
        {
            _logger.LogInformation($"Started retry orchestration. Attempt {retryAttempt}");
        }

        var retryOptions = new RetryOptions(TimeSpan.FromSeconds(3), 10)
        {
            MaxNumberOfAttempts = 3,
            Handle = IsTransientException
        };

        try
        {
            var outputs = new List<string>
            {
                await context.CallActivityWithRetryAsync<string>(nameof(LegacyCall), retryOptions, "Grodno"),
                await context.CallActivityWithRetryAsync<string>(nameof(LegacyCall), retryOptions, "Seattle"),
                await context.CallActivityWithRetryAsync<string>(nameof(LegacyCall), retryOptions, "London")
            };

            _logger.LogInformation("Processing finished.");

            return outputs;
        }
        catch (FunctionFailedException wrappingException) when
            (wrappingException.InnerException is RestClientApiException)
        {
            if (IsTransientException(wrappingException.InnerException) && retryAttempt < 3)
            {
                var waitInterval = context.CurrentUtcDateTime.Add(TimeSpan.FromSeconds(3));
                await context.CreateTimer(waitInterval, CancellationToken.None);
                context.ContinueAsNew(++retryAttempt);
                return null;
            }

            _logger.LogError("Processing failed!");
            throw;
        }
    }

    private static bool IsTransientException(Exception exception)
    {
        if (exception is TaskFailedException taskException)
        {
            exception = taskException.InnerException;
        }

        if (exception is RestClientApiException responseException)
        {
            return responseException.StatusCode switch
            {
                HttpStatusCode.InternalServerError => true,
                _ => false
            };
        }

        return false;
    }

    [FunctionName(nameof(LegacyCall))]
    public Task<string> LegacyCall([ActivityTrigger] string name)
    {
        var r = Random.Shared.NextDouble();
        HttpStatusCode status;

        switch (r)
        {
            case > 0.5 and < 0.99:
                status = HttpStatusCode.InternalServerError;
                break;
            case >= 0.99:
                status = HttpStatusCode.BadRequest;
                break;
            default:
                _logger.LogInformation($"Saying hello to {name}.");
                return Task.FromResult($"Hello {name}!");
        }

        var exception = new RestClientApiException($"Legacy service failed with error {status}.")
        {
            StatusCode = status
        };

        throw exception;
    }

    [FunctionName(nameof(TriggerChainWithRetry))]
    public async Task<HttpResponseMessage> TriggerChainWithRetry(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
        [DurableClient] IDurableOrchestrationClient starter)
    {
        var instanceId = await starter.StartNewAsync(nameof(RunChainOrchestratorWithRetry));
        _logger.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        return starter.CreateCheckStatusResponse(req, instanceId);
    }
}