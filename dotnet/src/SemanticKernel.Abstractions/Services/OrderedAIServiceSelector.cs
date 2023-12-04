// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Services;

using System.Collections.Generic;
using System.Linq;
using AI;
using Extensions.DependencyInjection;


/// <summary>
/// Implementation of <see cref="IAIServiceSelector"/> that selects the AI service based on the order of the model settings.
/// Uses the service id to select the preferred service provider and then returns the service and associated model settings.
/// </summary>
internal sealed class OrderedAIServiceSelector : IAIServiceSelector
{
    public static OrderedAIServiceSelector Instance { get; } = new();


    /// <inheritdoc/>
    public (T?, PromptExecutionSettings?) SelectAIService<T>(Kernel kernel, KernelFunction function, KernelArguments arguments) where T : class, IAIService
    {
        IEnumerable<PromptExecutionSettings>? executionSettings = function.ExecutionSettings;

        if (executionSettings is null || !executionSettings.Any())
        {
            T? service = kernel.Services is IKeyedServiceProvider
                ? kernel.GetAllServices<T>().LastOrDefault()
                : // see comments in Kernel/KernelBuilder for why we can't use GetKeyedService
                kernel.Services.GetService<T>();

            if (service is not null)
            {
                return (service, null);
            }
        }
        else
        {
            PromptExecutionSettings? defaultExecutionSettings = null;

            foreach (PromptExecutionSettings? model in executionSettings)
            {
                if (!string.IsNullOrEmpty(model.ServiceId))
                {
                    T? service = kernel.Services is IKeyedServiceProvider ? kernel.Services.GetKeyedService<T>(model.ServiceId) : null;

                    if (service is not null)
                    {
                        return (service, model);
                    }
                }
                else if (!string.IsNullOrEmpty(model.ModelId))
                {
                    T? service = GetServiceByModelId<T>(kernel, model.ModelId!);

                    if (service is not null)
                    {
                        return (service, model);
                    }
                }
                else
                {
                    // First execution settings with empty or null service id is the default
                    defaultExecutionSettings ??= model;
                }
            }

            if (defaultExecutionSettings is not null)
            {
                return (kernel.GetService<T>(), defaultExecutionSettings);
            }
        }

        string? names = executionSettings is not null ? string.Join("|", executionSettings.Select(model => model.ServiceId).ToArray()) : null;
        throw new KernelException(string.IsNullOrWhiteSpace(names) ? $"Service of type {typeof(T)} not registered." : $"Service of type {typeof(T)} and names {names} not registered.");
    }


    private T? GetServiceByModelId<T>(Kernel kernel, string modelId) where T : class, IAIService
    {
        IEnumerable<T> services = kernel.GetAllServices<T>();

        foreach (T? service in services)
        {
            string? serviceModelId = service.GetModelId();

            if (!string.IsNullOrEmpty(serviceModelId) && serviceModelId == modelId)
            {
                return service;
            }
        }

        return default;
    }
}
