/*
https://docs.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-code-constraints
*/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace FunctionAppDemo.Functions;

public class NotDeterministicFunction
{
    [FunctionName(nameof(NotDeterministic))]
    public async Task<List<string>> NotDeterministic([OrchestrationTrigger] IDurableOrchestrationContext context)
    {
        var outputs = new List<string>();

        var guid = Guid.NewGuid();
        var guid2 = context.NewGuid();

        outputs.Add(await context.CallActivityAsync<string>(nameof(ChainFunction.SayHello), "Grodno" + guid));

        await Task.Delay(10);

        outputs.Add(await context.CallActivityAsync<string>(nameof(ChainFunction.SayHello), "Seattle" + guid2));
        outputs.Add(await context.CallActivityAsync<string>(nameof(ChainFunction.SayHello), "London"));

        return outputs;
    }
}