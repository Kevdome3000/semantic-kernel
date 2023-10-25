// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Functions;

using System.Collections.Generic;
using System.Linq;
using AI;
using Diagnostics;
using Services;


/// <summary>
/// Implementation of <see cref="IAIServiceSelector"/> that selects the AI service based on the order of the model settings.
/// Uses the service id to select the preferred service provider and then returns the service and associated model settings.
/// </summary>
internal class OrderedIAIServiceSelector : IAIServiceSelector
{
    /// <inheritdoc/>
    public (T?, AIRequestSettings?) SelectAIService<T>(IAIServiceProvider serviceProvider, IReadOnlyList<AIRequestSettings>? modelSettings) where T : IAIService
    {
        if (modelSettings is null || modelSettings.Count == 0)
        {
            var service = serviceProvider.GetService<T>(null);

            if (service is not null)
            {
                return (service, null);
            }
        }
        else
        {
            AIRequestSettings? defaultRequestSettings = null;

            foreach (var model in modelSettings)
            {
                if (!string.IsNullOrEmpty(model.ServiceId))
                {
                    var service = serviceProvider.GetService<T>(model.ServiceId);

                    if (service is not null)
                    {
                        return (service, model);
                    }
                }
                else
                {
                    // First request settings with empty or null service id is the default
                    defaultRequestSettings ??= model;
                }
            }

            if (defaultRequestSettings is not null)
            {
                var service = serviceProvider.GetService<T>(null);

                if (service is not null)
                {
                    return (service, defaultRequestSettings);
                }
            }
        }

        var names = string.Join("|", modelSettings.Select(model => model.ServiceId).ToArray());
        throw new SKException($"Service of type {typeof(T)} and name {names ?? "<NONE>"} not registered.");
    }
}
