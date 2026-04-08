// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using Microsoft.OpenApi.YamlReader;
using Microsoft.SemanticKernel.Text;

namespace Microsoft.SemanticKernel.Plugins.OpenApi;

/// <summary>
/// Parser for OpenAPI documents.
/// </summary>
public sealed class OpenApiDocumentParser(ILoggerFactory? loggerFactory = null)
{
    /// <summary>
    /// Parses OpenAPI document.
    /// </summary>
    /// <param name="stream">Stream containing OpenAPI document to parse.</param>
    /// <param name="options">Options for parsing OpenAPI document.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Specification of the REST API.</returns>
    public async Task<RestApiSpecification> ParseAsync(Stream stream, OpenApiDocumentParserOptions? options = null, CancellationToken cancellationToken = default)
    {
        var settings = new OpenApiReaderSettings();
        settings.AddYamlReader();

        var result = await OpenApiDocument.LoadAsync(stream, settings: settings, cancellationToken: cancellationToken).ConfigureAwait(false);

        AssertReadingSuccessful(result, options?.IgnoreNonCompliantErrors ?? false);

        return new RestApiSpecification(
            ExtractRestApiInfo(result.Document),
            CreateRestApiOperationSecurityRequirements(result.Document.Security),
            ExtractRestApiOperations(result.Document, options, _logger));
    }


    #region private

    /// <summary>
    /// Max depth to traverse down OpenAPI schema to discover payload properties.
    /// </summary>
    private const int PayloadPropertiesHierarchyMaxDepth = 10;

    /// <summary>
    /// List of supported Media Types.
    /// </summary>
    private static readonly List<string> s_supportedMediaTypes =
    [
        "application/json",
        "text/plain"
    ];

    private readonly ILogger _logger = loggerFactory?.CreateLogger(typeof(OpenApiDocumentParser)) ?? NullLogger.Instance;


    /// <summary>
    /// Parses an OpenAPI document and extracts REST API information.
    /// </summary>
    /// <param name="document">The OpenAPI document.</param>
    /// <returns>Rest API information.</returns>
    internal static RestApiInfo ExtractRestApiInfo(OpenApiDocument document)
    {
        return new RestApiInfo
        {
            Title = document.Info.Title,
            Description = document.Info.Description,
            Version = document.Info.Version
        };
    }


    /// <summary>
    /// Parses an OpenAPI document and extracts REST API operations.
    /// </summary>
    /// <param name="document">The OpenAPI document.</param>
    /// <param name="options">Options for parsing OpenAPI document.</param>
    /// <param name="logger">Used to perform logging.</param>
    /// <returns>List of Rest operations.</returns>
    private static List<RestApiOperation> ExtractRestApiOperations(OpenApiDocument document, OpenApiDocumentParserOptions? options, ILogger logger)
    {
        var result = new List<RestApiOperation>();

        foreach (List<RestApiOperation>? operations in document.Paths.Select(pathPair => CreateRestApiOperations(document,
            pathPair.Key,
            pathPair.Value,
            options,
            logger)))
        {
            result.AddRange(operations);
        }

        return result;
    }


    /// <summary>
    /// Creates REST API operation.
    /// </summary>
    /// <param name="document">The OpenAPI document.</param>
    /// <param name="path">Rest resource path.</param>
    /// <param name="pathItem">Rest resource metadata.</param>
    /// <param name="options">Options for parsing OpenAPI document.</param>
    /// <param name="logger">Used to perform logging.</param>
    /// <returns>Rest operation.</returns>
    internal static List<RestApiOperation> CreateRestApiOperations(
        OpenApiDocument document,
        string path,
        IOpenApiPathItem pathItem,
        OpenApiDocumentParserOptions? options,
        ILogger logger)
    {
        try
        {
            var operations = new List<RestApiOperation>();
            var globalServers = CreateRestApiOperationServers(document.Servers);
            var pathServers = CreateRestApiOperationServers(pathItem.Servers);

            foreach (var operationPair in pathItem.Operations)
            {
                var method = operationPair.Key.ToString();
                var operationItem = operationPair.Value;
                var operationServers = CreateRestApiOperationServers(operationItem.Servers);

                // Skip the operation parsing and don't add it to the result operations list if it's explicitly excluded by the predicate.
                if (!options?.OperationSelectionPredicate?.Invoke(new OperationSelectionPredicateContext(operationItem.OperationId,
                        path,
                        method,
                        operationItem.Description))
                    ?? false)
                {
                    continue;
                }

                try
                {
                    var operationParameters = operationItem.Parameters ?? (IList<IOpenApiParameter>)[];
                    var pathParameters = pathItem.Parameters ?? (IList<IOpenApiParameter>)[];
                    var allParameters = operationParameters
                        .Union(pathParameters, s_parameterNameAndLocationComparer)
                        .Cast<OpenApiParameter>();

                    var operation = new RestApiOperation(
                        operationItem.OperationId,
                        globalServers,
                        pathServers: pathServers,
                        operationServers: operationServers,
                        path: path,
                        method: new HttpMethod(method),
                        description: string.IsNullOrEmpty(operationItem.Description)
                            ? operationItem.Summary
                            : operationItem.Description,
                        parameters: CreateRestApiOperationParameters(operationItem.OperationId, allParameters),
                        payload: CreateRestApiOperationPayload(operationItem.OperationId, operationItem.RequestBody),
                        responses: CreateRestApiOperationExpectedResponses(operationItem.Responses).ToDictionary(static item => item.Item1, static item => item.Item2),
                        securityRequirements: CreateRestApiOperationSecurityRequirements(operationItem.Security)
                    )
                    {
                        Extensions = operationItem.Extensions is not null
                            ? CreateRestApiOperationExtensions(operationItem.Extensions, logger)
                            : new Dictionary<string, object?>(),
                        Summary = operationItem.Summary
                    };

                    operations.Add(operation);
                }
                catch (KernelException ke)
                {
                    logger.LogWarning(ke, "Error occurred creating REST API operation for {OperationId}. Operation will be ignored.", operationItem.OperationId);
                }
            }

            return operations;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error occurred during REST API operation creation.");
            throw;
        }
    }


    private static readonly ParameterNameAndLocationComparer s_parameterNameAndLocationComparer = new();


    /// <summary>
    /// Compares two <see cref="IOpenApiParameter"/> objects by their name and location.
    /// </summary>
    private sealed class ParameterNameAndLocationComparer : IEqualityComparer<IOpenApiParameter>
    {
        public bool Equals(IOpenApiParameter? x, IOpenApiParameter? y)
        {
            if (x is null || y is null)
            {
                return x == y;
            }
            return GetHashCode(x) == GetHashCode(y);
        }


        public int GetHashCode([DisallowNull] IOpenApiParameter obj)
        {
            return HashCode.Combine(obj.Name, obj.In);
        }
    }


    /// <summary>
    /// Build a list of <see cref="RestApiServer"/> objects from the given list of <see cref="OpenApiServer"/> objects.
    /// </summary>
    /// <param name="servers">Represents servers which hosts the REST API.</param>
    private static List<RestApiServer> CreateRestApiOperationServers(IList<OpenApiServer> servers)
    {
        if (servers == null || servers.Count == 0)
        {
            return [];
        }

        var result = new List<RestApiServer>(servers.Count);

        foreach (var server in servers)
        {
            var variables = server.Variables?.ToDictionary(item => item.Key, item => new RestApiServerVariable(item.Value.Default, item.Value.Description, item.Value.Enum))
                ?? new Dictionary<string, RestApiServerVariable>();
            result.Add(new RestApiServer(server.Url, variables, server.Description));
        }

        return result;
    }


    /// <summary>
    /// Build a <see cref="RestApiSecurityScheme"/> objects from the given <see cref="IOpenApiSecurityScheme"/> object.
    /// </summary>
    /// <param name="securityScheme">The REST API security scheme.</param>
    private static RestApiSecurityScheme CreateRestApiSecurityScheme(IOpenApiSecurityScheme securityScheme)
    {
        return new RestApiSecurityScheme
        {
            SecuritySchemeType = securityScheme.Type.ToString(),
            Description = securityScheme.Description,
            Name = securityScheme.Name,
            In = TryParseParameterLocation(securityScheme.In),
            Scheme = securityScheme.Scheme,
            BearerFormat = securityScheme.BearerFormat,
            Flows = CreateRestApiOAuthFlows(securityScheme.Flows),
            OpenIdConnectUrl = securityScheme.OpenIdConnectUrl
        };
    }


    /// <summary>
    /// Safely converts a <see cref="ParameterLocation"/> value to <see cref="RestApiParameterLocation"/>.
    /// Returns <see cref="RestApiParameterLocation.Query"/> as a default if the value cannot be parsed.
    /// </summary>
    private static RestApiParameterLocation TryParseParameterLocation(ParameterLocation? parameterLocation)
    {
        var value = parameterLocation?.ToString();
        if (!string.IsNullOrEmpty(value) && Enum.TryParse(typeof(RestApiParameterLocation), value, true, out var location))
        {
            return (RestApiParameterLocation)location!;
        }

        return default;
    }


    /// <summary>
    /// Build a <see cref="RestApiOAuthFlows"/> object from the given <see cref="OpenApiOAuthFlows"/> object.
    /// </summary>
    /// <param name="flows">The REST API OAuth flows.</param>
    private static RestApiOAuthFlows? CreateRestApiOAuthFlows(OpenApiOAuthFlows? flows)
    {
        return flows is not null
            ? new RestApiOAuthFlows
            {
                Implicit = CreateRestApiOAuthFlow(flows.Implicit),
                Password = CreateRestApiOAuthFlow(flows.Password),
                ClientCredentials = CreateRestApiOAuthFlow(flows.ClientCredentials),
                AuthorizationCode = CreateRestApiOAuthFlow(flows.AuthorizationCode)
            }
            : null;
    }


    /// <summary>
    /// Build a <see cref="RestApiOAuthFlow"/> object from the given <see cref="OpenApiOAuthFlow"/> object.
    /// </summary>
    /// <param name="flow">The REST API OAuth flow.</param>
    private static RestApiOAuthFlow? CreateRestApiOAuthFlow(OpenApiOAuthFlow? flow)
    {
        return flow is not null
            ? new RestApiOAuthFlow
            {
                AuthorizationUrl = flow.AuthorizationUrl,
                TokenUrl = flow.TokenUrl,
                RefreshUrl = flow.RefreshUrl,
                Scopes = new ReadOnlyDictionary<string, string>(flow.Scopes ?? new Dictionary<string, string>())
            }
            : null;
    }


    /// <summary>
    /// Build a list of <see cref="RestApiSecurityRequirement"/> objects from the given <see cref="OpenApiSecurityRequirement"/> objects.
    /// </summary>
    /// <param name="security">The REST API security.</param>
    internal static List<RestApiSecurityRequirement> CreateRestApiOperationSecurityRequirements(IList<OpenApiSecurityRequirement>? security)
    {
        var operationRequirements = new List<RestApiSecurityRequirement>();

        if (security is not null)
        {
            foreach (var item in security)
            {
                foreach (var keyValuePair in item)
                {
                    operationRequirements.Add(new RestApiSecurityRequirement(new Dictionary<RestApiSecurityScheme, IList<string>> { { CreateRestApiSecurityScheme(keyValuePair.Key), keyValuePair.Value } }));
                }
            }
        }

        return operationRequirements;
    }


    /// <summary>
    /// Build a dictionary of extension key value pairs from the given open api extension model, where the key is the extension name
    /// and the value is either the actual value in the case of primitive types like string, int, date, etc, or a json string in the
    /// case of complex types.
    /// </summary>
    /// <param name="extensions">The dictionary of extension properties in the open api model.</param>
    /// <param name="logger">Used to perform logging.</param>
    /// <returns>The dictionary of extension properties using a simplified model that doesn't use any open api models.</returns>
    /// <exception cref="KernelException">Thrown when any extension data types are encountered that are not supported.</exception>
    private static Dictionary<string, object?> CreateRestApiOperationExtensions(IDictionary<string, IOpenApiExtension> extensions, ILogger logger)
    {
        var result = new Dictionary<string, object?>();

        foreach (var extension in extensions)
        {
            if (extension.Value is JsonNodeExtension jsonNodeExt)
            {
                var node = jsonNodeExt.Node;
                if (node is JsonValue jsonValue)
                {
                    result.Add(extension.Key, GetJsonValueAsObject(jsonValue));
                }
                else if (node is JsonObject or JsonArray)
                {
                    result.Add(extension.Key, node.ToJsonString());
                }
                else
                {
                    result.Add(extension.Key, null);
                }
            }
            else
            {
                logger.LogWarning("The type of extension property '{ExtensionPropertyName}' is not supported while trying to consume the OpenApi schema.", extension.Key);
            }
        }

        return result;
    }


    /// <summary>
    /// Extracts a CLR object from a <see cref="JsonValue"/>.
    /// </summary>
    private static object? GetJsonValueAsObject(JsonValue jsonValue)
    {
        if (jsonValue.TryGetValue<int>(out var intVal))
        {
            return intVal;
        }
        if (jsonValue.TryGetValue<long>(out var longVal))
        {
            return longVal;
        }
        if (jsonValue.TryGetValue<double>(out var doubleVal))
        {
            return doubleVal;
        }
        if (jsonValue.TryGetValue<bool>(out var boolVal))
        {
            return boolVal;
        }
        if (jsonValue.TryGetValue<string>(out var strVal))
        {
            return strVal;
        }
        return jsonValue.ToJsonString();
    }


    /// <summary>
    /// Creates REST API parameters.
    /// </summary>
    /// <param name="operationId">The operation id.</param>
    /// <param name="parameters">The OpenAPI parameters.</param>
    /// <returns>The parameters.</returns>
    private static List<RestApiParameter> CreateRestApiOperationParameters(string operationId, IEnumerable<OpenApiParameter> parameters)
    {
        var result = new List<RestApiParameter>();

        foreach (var parameter in parameters)
        {
            if (parameter.In is null)
            {
                throw new KernelException($"Parameter location of {parameter.Name} parameter of {operationId} operation is undefined.");
            }

            if (parameter.Style is null)
            {
                throw new KernelException($"Parameter style of {parameter.Name} parameter of {operationId} operation is undefined.");
            }

            var restParameter = new RestApiParameter(
                parameter.Name,
                parameter.Schema.GetSchemaType(),
                parameter.Required,
                parameter.Explode,
                (RestApiParameterLocation)Enum.Parse(typeof(RestApiParameterLocation), parameter.In.ToString()!),
                (RestApiParameterStyle)Enum.Parse(typeof(RestApiParameterStyle), parameter.Style.ToString()!),
                parameter.Schema.Items?.GetSchemaType(),
                GetParameterValue(parameter.Schema.Default, "parameter", parameter.Name),
                parameter.Description,
                parameter.Schema.Format,
                parameter.Schema.ToJsonSchema()
            );

            result.Add(restParameter);
        }

        return result;
    }


    /// <summary>
    /// Creates REST API payload.
    /// </summary>
    /// <param name="operationId">The operation id.</param>
    /// <param name="requestBody">The OpenAPI request body.</param>
    /// <returns>The REST API payload.</returns>
    private static RestApiPayload? CreateRestApiOperationPayload(string operationId, IOpenApiRequestBody? requestBody)
    {
        if (requestBody?.Content is null)
        {
            return null;
        }

        var mediaType = GetMediaType(requestBody.Content) ?? throw new KernelException($"Neither of the media types of {operationId} is supported.");
        var mediaTypeMetadata = requestBody.Content[mediaType];

        var payloadProperties = GetPayloadProperties(operationId, mediaTypeMetadata.Schema);

        return new RestApiPayload(mediaType,
            payloadProperties,
            requestBody.Description,
            mediaTypeMetadata?.Schema?.ToJsonSchema());
    }


    /// <summary>
    /// Returns the first supported media type. If none of the media types are supported, an exception is thrown.
    /// </summary>
    /// <remarks>
    /// Handles the case when the media type contains additional parameters e.g. application/json; x-api-version=2.0.
    /// </remarks>
    /// <param name="content">The OpenAPI request body content.</param>
    /// <returns>The first supported media type.</returns>
    /// <exception cref="KernelException"></exception>
    private static string? GetMediaType(IDictionary<string, IOpenApiMediaType> content)
    {
        foreach (var mediaType in s_supportedMediaTypes)
        {
            foreach (var key in content.Keys)
            {
                var keyParts = key.Split(';');

                if (keyParts[0].Equals(mediaType, StringComparison.OrdinalIgnoreCase))
                {
                    return key;
                }
            }
        }
        return null;
    }


    /// <summary>
    /// Create collection of expected responses for the REST API operation for the supported media types.
    /// </summary>
    /// <param name="responses">Responses from the OpenAPI endpoint.</param>
    private static IEnumerable<(string, RestApiExpectedResponse)> CreateRestApiOperationExpectedResponses(OpenApiResponses responses)
    {
        foreach (var response in responses)
        {
            if (response.Value.Content is null || response.Value.Content.Count == 0)
            {
                continue;
            }

            var mediaType = GetMediaType(response.Value.Content);

            if (mediaType is not null)
            {
                var matchingSchema = response.Value.Content[mediaType].Schema;
                var description = response.Value.Description ?? matchingSchema?.Description ?? string.Empty;

                yield return (response.Key, new RestApiExpectedResponse(description, mediaType, matchingSchema?.ToJsonSchema()));
            }
        }
    }


    /// <summary>
    /// Returns REST API payload properties.
    /// </summary>
    /// <param name="operationId">The operation id.</param>
    /// <param name="schema">An OpenAPI document schema representing request body properties.</param>
    /// <param name="level">Current level in OpenAPI schema.</param>
    /// <returns>The REST API payload properties.</returns>
    private static List<RestApiPayloadProperty> GetPayloadProperties(string operationId, IOpenApiSchema? schema, int level = 0)
    {
        if (schema is null)
        {
            return [];
        }

        if (level > PayloadPropertiesHierarchyMaxDepth)
        {
            throw new KernelException($"Max level {PayloadPropertiesHierarchyMaxDepth} of traversing payload properties of {operationId} operation is exceeded.");
        }

        var result = new List<RestApiPayloadProperty>();

        if (schema.Properties is null)
        {
            return result;
        }

        var requiredProperties = schema.Required ?? new HashSet<string>();

        foreach (var propertyPair in schema.Properties)
        {
            var propertyName = propertyPair.Key;

            var propertySchema = propertyPair.Value;

            var property = new RestApiPayloadProperty(
                propertyName,
                propertySchema.GetSchemaType(),
                requiredProperties.Contains(propertyName),
                GetPayloadProperties(operationId, propertySchema, level + 1),
                propertySchema.Description,
                propertySchema.Format,
                propertySchema.ToJsonSchema(),
                GetParameterValue(propertySchema.Default, "payload property", propertyName));

            result.Add(property);
        }

        return result;
    }


    /// <summary>
    /// Returns parameter value.
    /// </summary>
    /// <param name="valueMetadata">The value metadata as a JsonNode.</param>
    /// <param name="entityDescription">A description of the type of entity we are trying to get a value for.</param>
    /// <param name="entityName">The name of the entity that we are trying to get the value for.</param>
    /// <returns>The parameter value.</returns>
    private static object? GetParameterValue(JsonNode? valueMetadata, string entityDescription, string entityName)
    {
        if (valueMetadata is null)
        {
            return null;
        }

        if (valueMetadata is JsonValue jsonValue)
        {
            return GetJsonValueAsObject(jsonValue);
        }

        return valueMetadata.ToJsonString();
    }


    /// <summary>
    /// Asserts the successful reading of OpenAPI document.
    /// </summary>
    /// <param name="readResult">The reading results to be checked.</param>
    /// <param name="ignoreNonCompliantErrors">Flag indicating whether to ignore non-compliant errors.
    /// If set to true, the parser will not throw exceptions for non-compliant documents.
    /// Please note that enabling this option may result in incomplete or inaccurate parsing results.
    /// </param>
    private void AssertReadingSuccessful(ReadResult readResult, bool ignoreNonCompliantErrors)
    {
        if (readResult.Diagnostic.Errors.Any())
        {
            var title = readResult.Document.Info?.Title;
            var errors = string.Join(";", readResult.Diagnostic.Errors);

            if (!ignoreNonCompliantErrors)
            {
                var exception = new KernelException($"Parsing of '{title}' OpenAPI document complete with the following errors: {errors}");
                _logger.LogError(exception,
                    "Parsing of '{Title}' OpenAPI document complete with the following errors: {Errors}",
                    title,
                    errors);
                throw exception;
            }

            _logger.LogWarning("Parsing of '{Title}' OpenAPI document complete with the following errors: {Errors}", title, errors);
        }
    }

    #endregion


}
