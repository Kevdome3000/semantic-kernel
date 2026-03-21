// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Http;
using Microsoft.SemanticKernel.Plugins.OpenApi;
using Moq;
using SemanticKernel.Functions.UnitTests.OpenApi.TestResponses;
using Xunit;

namespace SemanticKernel.Functions.UnitTests.OpenApi;

public sealed class RestApiOperationRunnerTests : IDisposable
{
    /// <summary>
    /// A mock instance of the authentication callback.
    /// </summary>
    private readonly Mock<AuthenticateRequestAsyncCallback> _authenticationHandlerMock;

    /// <summary>
    /// An instance of HttpMessageHandlerStub class used to get access to various properties of HttpRequestMessage sent by HTTP client.
    /// </summary>
    private readonly HttpMessageHandlerStub _httpMessageHandlerStub;

    /// <summary>
    /// An instance of HttpClient class used by the tests.
    /// </summary>
    private readonly HttpClient _httpClient;


    /// <summary>
    /// Creates an instance of a <see cref="RestApiOperationRunnerTests"/> class.
    /// </summary>
    public RestApiOperationRunnerTests()
    {
        _authenticationHandlerMock = new Mock<AuthenticateRequestAsyncCallback>();

        _httpMessageHandlerStub = new HttpMessageHandlerStub();

        _httpClient = new HttpClient(_httpMessageHandlerStub);
    }


    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    [InlineData("GET")]
    public async Task ItCanRunCreateAndUpdateOperationsWithJsonPayloadSuccessfullyAsync(string method)
    {
        // Arrange
        _httpMessageHandlerStub.ResponseToReturn.Content = new StringContent("fake-content", Encoding.UTF8, MediaTypeNames.Application.Json);

        var httpMethod = new HttpMethod(method);

        var operation = new RestApiOperation(
            "fake-id",
            [new RestApiServer("https://fake-random-test-host")],
            "fake-path",
            httpMethod,
            "fake-description",
            [],
            new Dictionary<string, RestApiExpectedResponse>(),
            []
        );

        var payload = new
        {
            value = "fake-value",
            attributes = new
            {
                enabled = true
            }
        };

        var arguments = new KernelArguments
        {
            { "payload", JsonSerializer.Serialize(payload) },
            { "content-type", "application/json" }
        };

        var sut = new RestApiOperationRunner(_httpClient, _authenticationHandlerMock.Object);

        // Act
        var result = await sut.RunAsync(operation, arguments);

        // Assert
        Assert.NotNull(_httpMessageHandlerStub.RequestUri);
        Assert.Equal("https://fake-random-test-host/fake-path", _httpMessageHandlerStub.RequestUri.AbsoluteUri);

        Assert.Equal(httpMethod, _httpMessageHandlerStub.Method);

        Assert.NotNull(_httpMessageHandlerStub.ContentHeaders);
        Assert.Contains(_httpMessageHandlerStub.ContentHeaders, h => h.Key == "Content-Type" && h.Value.Contains("application/json; charset=utf-8"));

        var messageContent = _httpMessageHandlerStub.RequestContent;
        Assert.NotNull(messageContent);
        Assert.NotEmpty(messageContent);

        var deserializedPayload = await JsonNode.ParseAsync(new MemoryStream(messageContent));
        Assert.NotNull(deserializedPayload);

        var valueProperty = deserializedPayload["value"]?.ToString();
        Assert.Equal("fake-value", valueProperty);

        var attributesProperty = deserializedPayload["attributes"];
        Assert.NotNull(attributesProperty);

        var enabledProperty = attributesProperty["enabled"]?.AsValue();
        Assert.NotNull(enabledProperty);
        Assert.Equal("true", enabledProperty.ToString());

        Assert.NotNull(result);

        Assert.Equal("fake-content", result.Content);

        Assert.Equal("application/json; charset=utf-8", result.ContentType);

        _authenticationHandlerMock.Verify(x => x(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }


    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    [InlineData("GET")]
    public async Task ItCanRunCreateAndUpdateOperationsWithPlainTextPayloadSuccessfullyAsync(string method)
    {
        // Arrange
        _httpMessageHandlerStub.ResponseToReturn.Content = new StringContent("fake-content", Encoding.UTF8, MediaTypeNames.Text.Plain);

        var httpMethod = new HttpMethod(method);

        var operation = new RestApiOperation(
            "fake-id",
            [new RestApiServer("https://fake-random-test-host")],
            "fake-path",
            httpMethod,
            "fake-description",
            [],
            new Dictionary<string, RestApiExpectedResponse>(),
            []
        );

        var arguments = new KernelArguments
        {
            { "payload", "fake-input-value" },
            { "content-type", "text/plain" }
        };

        var sut = new RestApiOperationRunner(_httpClient, _authenticationHandlerMock.Object);

        // Act
        var result = await sut.RunAsync(operation, arguments);

        // Assert
        Assert.NotNull(_httpMessageHandlerStub.RequestUri);
        Assert.Equal("https://fake-random-test-host/fake-path", _httpMessageHandlerStub.RequestUri.AbsoluteUri);

        Assert.Equal(httpMethod, _httpMessageHandlerStub.Method);

        Assert.NotNull(_httpMessageHandlerStub.ContentHeaders);
        Assert.Contains(_httpMessageHandlerStub.ContentHeaders, h => h.Key == "Content-Type" && h.Value.Contains("text/plain; charset=utf-8"));

        var messageContent = _httpMessageHandlerStub.RequestContent;
        Assert.NotNull(messageContent);
        Assert.NotEmpty(messageContent);

        var payloadText = Encoding.UTF8.GetString(messageContent, 0, messageContent.Length);
        Assert.Equal("fake-input-value", payloadText);

        Assert.NotNull(result);

        Assert.Equal("fake-content", result.Content);

        Assert.Equal("text/plain; charset=utf-8", result.ContentType);

        _authenticationHandlerMock.Verify(x => x(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task ItShouldAddHeadersToHttpRequestAsync()
    {
        // Arrange
        var parameters = new List<RestApiParameter>
        {
            new("X-HS-1",
                "string",
                true,
                false,
                RestApiParameterLocation.Header,
                RestApiParameterStyle.Simple),
            new("X-HA-1",
                "array",
                true,
                false,
                RestApiParameterLocation.Header,
                RestApiParameterStyle.Simple),
            new("X-HA-2",
                "array",
                true,
                false,
                RestApiParameterLocation.Header,
                RestApiParameterStyle.Simple),
            new("X-HB-1",
                "boolean",
                true,
                false,
                RestApiParameterLocation.Header,
                RestApiParameterStyle.Simple),
            new("X-HB-2",
                "boolean",
                true,
                false,
                RestApiParameterLocation.Header,
                RestApiParameterStyle.Simple),
            new("X-HI-1",
                "integer",
                true,
                false,
                RestApiParameterLocation.Header,
                RestApiParameterStyle.Simple),
            new("X-HI-2",
                "integer",
                true,
                false,
                RestApiParameterLocation.Header,
                RestApiParameterStyle.Simple),
            new("X-HN-1",
                "number",
                true,
                false,
                RestApiParameterLocation.Header,
                RestApiParameterStyle.Simple),
            new("X-HN-2",
                "number",
                true,
                false,
                RestApiParameterLocation.Header,
                RestApiParameterStyle.Simple),
            new("X-HD-1",
                "string",
                true,
                false,
                RestApiParameterLocation.Header,
                RestApiParameterStyle.Simple),
            new("X-HD-2",
                "string",
                true,
                false,
                RestApiParameterLocation.Header,
                RestApiParameterStyle.Simple),
            new("X-HD-3",
                "string",
                true,
                false,
                RestApiParameterLocation.Header,
                RestApiParameterStyle.Simple)
        };

        var operation = new RestApiOperation(
            "fake-id",
            [new RestApiServer("https://fake-random-test-host")],
            "fake-path",
            HttpMethod.Get,
            "fake-description",
            parameters,
            new Dictionary<string, RestApiExpectedResponse>(),
            []
        );

        var arguments = new KernelArguments
        {
            ["X-HS-1"] = "fake-header-value",
            ["X-HA-1"] = "[1,2,3]",
            ["X-HA-2"] = new Collection<string> { "3", "4", "5" },
            ["X-HB-1"] = "true",
            ["X-HB-2"] = false,
            ["X-HI-1"] = "10",
            ["X-HI-2"] = 20,
            ["X-HN-1"] = 5698.4567,
            ["X-HN-2"] = "5698.4567",
            ["X-HD-1"] = "2023-12-06T11:53:36Z",
            ["X-HD-2"] = new DateTime(2023,
                12,
                06,
                11,
                53,
                36,
                DateTimeKind.Utc),
            ["X-HD-3"] = new DateTimeOffset(2023,
                12,
                06,
                11,
                53,
                36,
                TimeSpan.FromHours(-2))
        };

        var sut = new RestApiOperationRunner(_httpClient, _authenticationHandlerMock.Object, "fake-agent");

        // Act
        await sut.RunAsync(operation, arguments);

        // Assert - 13 headers: 12 from the test and the User-Agent added internally
        Assert.NotNull(_httpMessageHandlerStub.RequestHeaders);
        Assert.Equal(14, _httpMessageHandlerStub.RequestHeaders.Count());

        Assert.Contains(_httpMessageHandlerStub.RequestHeaders, h => h.Key == "User-Agent" && h.Value.Contains("fake-agent"));
        Assert.Contains(_httpMessageHandlerStub.RequestHeaders, h => h.Key == "X-HS-1" && h.Value.Contains("fake-header-value"));
        Assert.Contains(_httpMessageHandlerStub.RequestHeaders, h => h.Key == "X-HA-1" && h.Value.Contains("1,2,3"));
        Assert.Contains(_httpMessageHandlerStub.RequestHeaders, h => h.Key == "X-HA-2" && h.Value.Contains("3,4,5"));
        Assert.Contains(_httpMessageHandlerStub.RequestHeaders, h => h.Key == "X-HB-1" && h.Value.Contains("true"));
        Assert.Contains(_httpMessageHandlerStub.RequestHeaders, h => h.Key == "X-HB-2" && h.Value.Contains("false"));
        Assert.Contains(_httpMessageHandlerStub.RequestHeaders, h => h.Key == "X-HI-1" && h.Value.Contains("10"));
        Assert.Contains(_httpMessageHandlerStub.RequestHeaders, h => h.Key == "X-HI-2" && h.Value.Contains("20"));
        Assert.Contains(_httpMessageHandlerStub.RequestHeaders, h => h.Key == "X-HN-1" && h.Value.Contains("5698.4567"));
        Assert.Contains(_httpMessageHandlerStub.RequestHeaders, h => h.Key == "X-HN-2" && h.Value.Contains("5698.4567"));
        Assert.Contains(_httpMessageHandlerStub.RequestHeaders, h => h.Key == "X-HD-1" && h.Value.Contains("2023-12-06T11:53:36Z"));
        Assert.Contains(_httpMessageHandlerStub.RequestHeaders, h => h.Key == "X-HD-2" && h.Value.Contains("2023-12-06T11:53:36Z"));
        Assert.Contains(_httpMessageHandlerStub.RequestHeaders, h => h.Key == "X-HD-3" && h.Value.Contains("2023-12-06T11:53:36-02:00"));
        Assert.Contains(_httpMessageHandlerStub.RequestHeaders, h => h.Key == "Semantic-Kernel-Version");
    }


    [Fact]
    public async Task ItShouldAddUserAgentHeaderToHttpRequestIfConfiguredAsync()
    {
        // Arrange
        var parameters = new List<RestApiParameter>
        {
            new(
                "fake-header",
                "string",
                true,
                false,
                RestApiParameterLocation.Header,
                RestApiParameterStyle.Simple)
        };

        var operation = new RestApiOperation(
            "fake-id",
            [new RestApiServer("https://fake-random-test-host")],
            "fake-path",
            HttpMethod.Get,
            "fake-description",
            parameters,
            new Dictionary<string, RestApiExpectedResponse>(),
            []
        );

        var arguments = new KernelArguments
        {
            { "fake-header", "fake-header-value" }
        };

        var sut = new RestApiOperationRunner(_httpClient, _authenticationHandlerMock.Object, "fake-user-agent");

        // Act
        await sut.RunAsync(operation, arguments);

        // Assert
        Assert.NotNull(_httpMessageHandlerStub.RequestHeaders);
        Assert.Equal(3, _httpMessageHandlerStub.RequestHeaders.Count());

        Assert.Contains(_httpMessageHandlerStub.RequestHeaders, h => h.Key == "fake-header" && h.Value.Contains("fake-header-value"));
        Assert.Contains(_httpMessageHandlerStub.RequestHeaders, h => h.Key == "User-Agent" && h.Value.Contains("fake-user-agent"));
        Assert.Contains(_httpMessageHandlerStub.RequestHeaders, h => h.Key == "Semantic-Kernel-Version");
    }


    [Fact]
    public async Task ItShouldBuildJsonPayloadDynamicallyAsync()
    {
        // Arrange
        _httpMessageHandlerStub.ResponseToReturn.Content = new StringContent("fake-content", Encoding.UTF8, MediaTypeNames.Application.Json);

        List<RestApiPayloadProperty> payloadProperties =
        [
            new("name",
                "string",
                true,
                []),
            new("attributes",
                "object",
                false,
                [
                    new RestApiPayloadProperty("enabled",
                        "boolean",
                        false,
                        [])
                ])
        ];

        var payload = new RestApiPayload(MediaTypeNames.Application.Json, payloadProperties);

        var operation = new RestApiOperation(
            "fake-id",
            [new RestApiServer("https://fake-random-test-host")],
            "fake-path",
            HttpMethod.Post,
            "fake-description",
            [],
            new Dictionary<string, RestApiExpectedResponse>(),
            [],
            payload
        );

        var arguments = new KernelArguments
        {
            { "name", "fake-name-value" },
            { "enabled", true }
        };

        var sut = new RestApiOperationRunner(_httpClient, _authenticationHandlerMock.Object, enableDynamicPayload: true);

        // Act
        var result = await sut.RunAsync(operation, arguments);

        // Assert
        Assert.NotNull(_httpMessageHandlerStub.ContentHeaders);
        Assert.Contains(_httpMessageHandlerStub.ContentHeaders, h => h.Key == "Content-Type" && h.Value.Contains("application/json; charset=utf-8"));

        var messageContent = _httpMessageHandlerStub.RequestContent;
        Assert.NotNull(messageContent);
        Assert.NotEmpty(messageContent);

        var deserializedPayload = await JsonNode.ParseAsync(new MemoryStream(messageContent));
        Assert.NotNull(deserializedPayload);

        var name = deserializedPayload["name"]?.ToString();
        Assert.Equal("fake-name-value", name);

        var attributes = deserializedPayload["attributes"];
        Assert.NotNull(attributes);

        var enabled = attributes["enabled"]?.ToString();
        Assert.NotNull(enabled);
        Assert.Equal("true", enabled);
    }


    [Fact]
    public async Task ItShouldBuildJsonPayloadDynamicallyUsingPayloadMetadataDataTypesAsync()
    {
        // Arrange
        _httpMessageHandlerStub.ResponseToReturn.Content = new StringContent("fake-content", Encoding.UTF8, MediaTypeNames.Application.Json);

        List<RestApiPayloadProperty> payloadProperties =
        [
            new("name",
                "string",
                true,
                []),
            new("attributes",
                "object",
                false,
                [
                    new RestApiPayloadProperty("enabled",
                        "boolean",
                        false,
                        []),
                    new RestApiPayloadProperty("cardinality",
                        "number",
                        false,
                        []),
                    new RestApiPayloadProperty("coefficient",
                        "number",
                        false,
                        []),
                    new RestApiPayloadProperty("count",
                        "integer",
                        false,
                        []),
                    new RestApiPayloadProperty("params",
                        "array",
                        false,
                        [])
                ])
        ];

        var payload = new RestApiPayload(MediaTypeNames.Application.Json, payloadProperties);

        var operation = new RestApiOperation(
            "fake-id",
            [new RestApiServer("https://fake-random-test-host")],
            "fake-path",
            HttpMethod.Post,
            "fake-description",
            [],
            new Dictionary<string, RestApiExpectedResponse>(),
            [],
            payload
        );

        var arguments = new KernelArguments
        {
            { "name", "fake-string-value" },
            { "enabled", "true" },
            { "cardinality", 8 },
            { "coefficient", "0.8" },
            { "count", 1 },
            { "params", "[1,2,3]" }
        };

        var sut = new RestApiOperationRunner(_httpClient, _authenticationHandlerMock.Object, enableDynamicPayload: true);

        // Act
        var result = await sut.RunAsync(operation, arguments);

        // Assert
        var messageContent = _httpMessageHandlerStub.RequestContent;
        Assert.NotNull(messageContent);

        var deserializedPayload = await JsonNode.ParseAsync(new MemoryStream(messageContent));
        Assert.NotNull(deserializedPayload);

        var name = deserializedPayload["name"]?.GetValue<JsonElement>();
        Assert.NotNull(name);
        Assert.Equal(JsonValueKind.String, name.Value.ValueKind);
        Assert.Equal("fake-string-value", name.ToString());

        var attributes = deserializedPayload["attributes"];
        Assert.True(attributes is JsonObject);

        var enabled = attributes["enabled"]?.GetValue<JsonElement>();
        Assert.NotNull(enabled);
        Assert.Equal(JsonValueKind.True, enabled.Value.ValueKind);

        var cardinality = attributes["cardinality"]?.GetValue<JsonElement>();
        Assert.NotNull(cardinality);
        Assert.Equal(JsonValueKind.Number, cardinality.Value.ValueKind);
        Assert.Equal("8", cardinality.Value.ToString());

        var coefficient = attributes["coefficient"]?.GetValue<JsonElement>();
        Assert.NotNull(coefficient);
        Assert.Equal(JsonValueKind.Number, coefficient.Value.ValueKind);
        Assert.Equal("0.8", coefficient.Value.ToString());

        var count = attributes["count"]?.GetValue<JsonElement>();
        Assert.NotNull(count);
        Assert.Equal(JsonValueKind.Number, coefficient.Value.ValueKind);
        Assert.Equal("1", count.Value.ToString());

        var parameters = attributes["params"];
        Assert.NotNull(parameters);
        Assert.True(parameters is JsonArray);
    }


    [Fact]
    public async Task ItShouldBuildJsonPayloadDynamicallyResolvingArgumentsByFullNamesAsync()
    {
        // Arrange
        _httpMessageHandlerStub.ResponseToReturn.Content = new StringContent("fake-content", Encoding.UTF8, MediaTypeNames.Application.Json);

        List<RestApiPayloadProperty> payloadProperties =
        [
            new("upn",
                "string",
                true,
                []),
            new("receiver",
                "object",
                false,
                [
                    new RestApiPayloadProperty("upn",
                        "string",
                        false,
                        []),
                    new RestApiPayloadProperty("alternative",
                        "object",
                        false,
                        [
                            new RestApiPayloadProperty("upn",
                                "string",
                                false,
                                [])
                        ])
                ]),
            new("cc",
                "object",
                false,
                [
                    new RestApiPayloadProperty("upn",
                        "string",
                        false,
                        [])
                ])
        ];

        var payload = new RestApiPayload(MediaTypeNames.Application.Json, payloadProperties);

        var operation = new RestApiOperation(
            "fake-id",
            [new RestApiServer("https://fake-random-test-host")],
            "fake-path",
            HttpMethod.Post,
            "fake-description",
            [],
            new Dictionary<string, RestApiExpectedResponse>(),
            [],
            payload
        );

        var arguments = new KernelArguments
        {
            { "upn", "fake-sender-upn" },
            { "receiver.upn", "fake-receiver-upn" },
            { "receiver.alternative.upn", "fake-receiver-alternative-upn" },
            { "cc.upn", "fake-cc-upn" }
        };

        var sut = new RestApiOperationRunner(
            _httpClient,
            _authenticationHandlerMock.Object,
            enableDynamicPayload: true,
            enablePayloadNamespacing: true);

        // Act
        var result = await sut.RunAsync(operation, arguments);

        // Assert
        Assert.NotNull(_httpMessageHandlerStub.ContentHeaders);
        Assert.Contains(_httpMessageHandlerStub.ContentHeaders, h => h.Key == "Content-Type" && h.Value.Contains("application/json; charset=utf-8"));

        var messageContent = _httpMessageHandlerStub.RequestContent;
        Assert.NotNull(messageContent);
        Assert.NotEmpty(messageContent);

        var deserializedPayload = await JsonNode.ParseAsync(new MemoryStream(messageContent));
        Assert.NotNull(deserializedPayload);

        //Sender props
        var senderUpn = deserializedPayload["upn"]?.ToString();
        Assert.Equal("fake-sender-upn", senderUpn);

        //Receiver props
        var receiver = deserializedPayload["receiver"];
        Assert.NotNull(receiver);

        var receiverUpn = receiver["upn"]?.AsValue();
        Assert.NotNull(receiverUpn);
        Assert.Equal("fake-receiver-upn", receiverUpn.ToString());

        var alternative = receiver["alternative"];
        Assert.NotNull(alternative);

        var alternativeUpn = alternative["upn"]?.AsValue();
        Assert.NotNull(alternativeUpn);
        Assert.Equal("fake-receiver-alternative-upn", alternativeUpn.ToString());

        //CC props
        var carbonCopy = deserializedPayload["cc"];
        Assert.NotNull(carbonCopy);

        var ccUpn = carbonCopy["upn"]?.AsValue();
        Assert.NotNull(ccUpn);
        Assert.Equal("fake-cc-upn", ccUpn.ToString());
    }


    [Fact]
    public async Task ItShouldThrowExceptionIfPayloadMetadataDoesNotHaveContentTypeAsync()
    {
        // Arrange
        var operation = new RestApiOperation(
            "fake-id",
            [new RestApiServer("https://fake-random-test-host")],
            "fake-path",
            HttpMethod.Post,
            "fake-description",
            [],
            new Dictionary<string, RestApiExpectedResponse>(),
            []
        );

        KernelArguments arguments = new() { { RestApiOperation.PayloadArgumentName, "fake-content" } };

        var sut = new RestApiOperationRunner(
            _httpClient,
            _authenticationHandlerMock.Object,
            enableDynamicPayload: true);

        // Act
        var exception = await Assert.ThrowsAsync<KernelException>(async () => await sut.RunAsync(operation, arguments));

        Assert.Contains("No media type is provided", exception.Message, StringComparison.InvariantCulture);
    }


    [Fact]
    public async Task ItShouldThrowExceptionIfContentTypeArgumentIsNotProvidedAsync()
    {
        // Arrange
        var operation = new RestApiOperation(
            "fake-id",
            [new RestApiServer("https://fake-random-test-host")],
            "fake-path",
            HttpMethod.Post,
            "fake-description",
            [],
            new Dictionary<string, RestApiExpectedResponse>(),
            []
        );

        KernelArguments arguments = new() { { RestApiOperation.PayloadArgumentName, "fake-content" } };

        var sut = new RestApiOperationRunner(
            _httpClient,
            _authenticationHandlerMock.Object,
            enableDynamicPayload: false);

        // Act
        var exception = await Assert.ThrowsAsync<KernelException>(async () => await sut.RunAsync(operation, arguments));

        Assert.Contains("No media type is provided", exception.Message, StringComparison.InvariantCulture);
    }


    [Fact]
    public async Task ItShouldUsePayloadArgumentForPlainTextContentTypeWhenBuildingPayloadDynamicallyAsync()
    {
        // Arrange
        _httpMessageHandlerStub.ResponseToReturn.Content = new StringContent("fake-content", Encoding.UTF8, MediaTypeNames.Text.Plain);

        var payload = new RestApiPayload(MediaTypeNames.Text.Plain, []);

        var operation = new RestApiOperation(
            "fake-id",
            [new RestApiServer("https://fake-random-test-host")],
            "fake-path",
            HttpMethod.Post,
            "fake-description",
            [],
            new Dictionary<string, RestApiExpectedResponse>(),
            [],
            payload
        );

        var arguments = new KernelArguments
        {
            { "payload", "fake-input-value" }
        };

        var sut = new RestApiOperationRunner(_httpClient, _authenticationHandlerMock.Object, enableDynamicPayload: true);

        // Act
        var result = await sut.RunAsync(operation, arguments);

        // Assert
        Assert.NotNull(_httpMessageHandlerStub.ContentHeaders);
        Assert.Contains(_httpMessageHandlerStub.ContentHeaders, h => h.Key == "Content-Type" && h.Value.Contains("text/plain; charset=utf-8"));

        var messageContent = _httpMessageHandlerStub.RequestContent;
        Assert.NotNull(messageContent);
        Assert.NotEmpty(messageContent);

        var payloadText = Encoding.UTF8.GetString(messageContent, 0, messageContent.Length);
        Assert.Equal("fake-input-value", payloadText);
    }


    [Theory]
    [InlineData(MediaTypeNames.Text.Plain)]
    [InlineData(MediaTypeNames.Application.Json)]
    public async Task ItShouldUsePayloadAndContentTypeArgumentsIfDynamicPayloadBuildingIsNotRequiredAsync(string contentType)
    {
        // Arrange
        _httpMessageHandlerStub.ResponseToReturn.Content = new StringContent("fake-content", Encoding.UTF8, MediaTypeNames.Text.Plain);

        var operation = new RestApiOperation(
            "fake-id",
            [new RestApiServer("https://fake-random-test-host")],
            "fake-path",
            HttpMethod.Post,
            "fake-description",
            [],
            new Dictionary<string, RestApiExpectedResponse>(),
            []
        );

        var arguments = new KernelArguments
        {
            { "payload", "fake-input-value" },
            { "content-type", $"{contentType}" }
        };

        var sut = new RestApiOperationRunner(_httpClient, _authenticationHandlerMock.Object, enableDynamicPayload: false);

        // Act
        var result = await sut.RunAsync(operation, arguments);

        // Assert
        Assert.NotNull(_httpMessageHandlerStub.ContentHeaders);
        Assert.Contains(_httpMessageHandlerStub.ContentHeaders, h => h.Key == "Content-Type" && h.Value.Contains($"{contentType}; charset=utf-8"));

        var messageContent = _httpMessageHandlerStub.RequestContent;
        Assert.NotNull(messageContent);
        Assert.NotEmpty(messageContent);

        var payloadText = Encoding.UTF8.GetString(messageContent, 0, messageContent.Length);
        Assert.Equal("fake-input-value", payloadText);
    }


    [Fact]
    public async Task ItShouldBuildJsonPayloadDynamicallyExcludingOptionalParametersIfTheirArgumentsNotProvidedAsync()
    {
        // Arrange
        _httpMessageHandlerStub.ResponseToReturn.Content = new StringContent("fake-content", Encoding.UTF8, MediaTypeNames.Application.Json);

        List<RestApiPayloadProperty> payloadProperties =
        [
            new("upn",
                "string",
                false,
                [])
        ];

        var payload = new RestApiPayload(MediaTypeNames.Application.Json, payloadProperties);

        var operation = new RestApiOperation(
            "fake-id",
            [new RestApiServer("https://fake-random-test-host")],
            "fake-path",
            HttpMethod.Post,
            "fake-description",
            [],
            new Dictionary<string, RestApiExpectedResponse>(),
            [],
            payload
        );

        var arguments = new KernelArguments();

        var sut = new RestApiOperationRunner(
            _httpClient,
            _authenticationHandlerMock.Object,
            enableDynamicPayload: true,
            enablePayloadNamespacing: true);

        // Act
        var result = await sut.RunAsync(operation, arguments);

        // Assert
        var messageContent = _httpMessageHandlerStub.RequestContent;
        Assert.NotNull(messageContent);
        Assert.NotEmpty(messageContent);

        var deserializedPayload = await JsonNode.ParseAsync(new MemoryStream(messageContent));
        Assert.NotNull(deserializedPayload);

        var senderUpn = deserializedPayload["upn"]?.ToString();
        Assert.Null(senderUpn);
    }


    [Fact]
    public async Task ItShouldBuildJsonPayloadDynamicallyIncludingOptionalParametersIfTheirArgumentsProvidedAsync()
    {
        // Arrange
        _httpMessageHandlerStub.ResponseToReturn.Content = new StringContent("fake-content", Encoding.UTF8, MediaTypeNames.Application.Json);

        List<RestApiPayloadProperty> payloadProperties =
        [
            new("upn",
                "string",
                false,
                [])
        ];

        var payload = new RestApiPayload(MediaTypeNames.Application.Json, payloadProperties);

        var operation = new RestApiOperation(
            "fake-id",
            [new RestApiServer("https://fake-random-test-host")],
            "fake-path",
            HttpMethod.Post,
            "fake-description",
            [],
            new Dictionary<string, RestApiExpectedResponse>(),
            [],
            payload
        );

        var arguments = new KernelArguments { ["upn"] = "fake-sender-upn" };

        var sut = new RestApiOperationRunner(
            _httpClient,
            _authenticationHandlerMock.Object,
            enableDynamicPayload: true,
            enablePayloadNamespacing: true);

        // Act
        var result = await sut.RunAsync(operation, arguments);

        // Assert
        var messageContent = _httpMessageHandlerStub.RequestContent;
        Assert.NotNull(messageContent);
        Assert.NotEmpty(messageContent);

        var deserializedPayload = await JsonNode.ParseAsync(new MemoryStream(messageContent));
        Assert.NotNull(deserializedPayload);

        var senderUpn = deserializedPayload["upn"]?.ToString();
        Assert.Equal("fake-sender-upn", senderUpn);
    }


    [Fact]
    public async Task ItShouldAddRequiredQueryStringParametersIfTheirArgumentsProvidedAsync()
    {
        // Arrange
        _httpMessageHandlerStub.ResponseToReturn.Content = new StringContent("fake-content", Encoding.UTF8, MediaTypeNames.Application.Json);

        var firstParameter = new RestApiParameter(
            "p1",
            "string",
            true, //Marking the parameter as required
            false,
            RestApiParameterLocation.Query,
            RestApiParameterStyle.Form);

        var secondParameter = new RestApiParameter(
            "p2",
            "integer",
            true, //Marking the parameter as required
            false,
            RestApiParameterLocation.Query,
            RestApiParameterStyle.Form);

        var operation = new RestApiOperation(
            "fake-id",
            [new RestApiServer("https://fake-random-test-host")],
            "fake-path",
            HttpMethod.Get,
            "fake-description",
            [firstParameter, secondParameter],
            new Dictionary<string, RestApiExpectedResponse>(),
            []
        );

        var arguments = new KernelArguments
        {
            { "p1", "v1" },
            { "p2", 28 }
        };

        var sut = new RestApiOperationRunner(_httpClient, _authenticationHandlerMock.Object);

        // Act
        var result = await sut.RunAsync(operation, arguments);

        // Assert
        Assert.NotNull(_httpMessageHandlerStub.RequestUri);
        Assert.Equal("https://fake-random-test-host/fake-path?p1=v1&p2=28", _httpMessageHandlerStub.RequestUri.AbsoluteUri);
    }


    [Fact]
    public async Task ItShouldAddNotRequiredQueryStringParametersIfTheirArgumentsProvidedAsync()
    {
        // Arrange
        _httpMessageHandlerStub.ResponseToReturn.Content = new StringContent("fake-content", Encoding.UTF8, MediaTypeNames.Application.Json);

        var firstParameter = new RestApiParameter(
            "p1",
            "string",
            false, //Marking the parameter as not required
            false,
            RestApiParameterLocation.Query,
            RestApiParameterStyle.Form);

        var secondParameter = new RestApiParameter(
            "p2",
            "string",
            false, //Marking the parameter as not required
            false,
            RestApiParameterLocation.Query,
            RestApiParameterStyle.Form);

        var operation = new RestApiOperation(
            "fake-id",
            [new RestApiServer("https://fake-random-test-host")],
            "fake-path",
            HttpMethod.Get,
            "fake-description",
            [firstParameter, secondParameter],
            new Dictionary<string, RestApiExpectedResponse>(),
            []
        );

        var arguments = new KernelArguments
        {
            {
                "p1", new DateTime(2023,
                    12,
                    06,
                    11,
                    53,
                    36,
                    DateTimeKind.Utc)
            },
            { "p2", "v2" }
        };

        var sut = new RestApiOperationRunner(_httpClient, _authenticationHandlerMock.Object);

        // Act
        var result = await sut.RunAsync(operation, arguments);

        // Assert
        Assert.NotNull(_httpMessageHandlerStub.RequestUri);
        Assert.Equal("https://fake-random-test-host/fake-path?p1=2023-12-06T11%3a53%3a36Z&p2=v2", _httpMessageHandlerStub.RequestUri.AbsoluteUri);
    }


    [Fact]
    public async Task ItShouldSkipNotRequiredQueryStringParametersIfNoArgumentsProvidedAsync()
    {
        // Arrange
        _httpMessageHandlerStub.ResponseToReturn.Content = new StringContent("fake-content", Encoding.UTF8, MediaTypeNames.Application.Json);

        var firstParameter = new RestApiParameter(
            "p1",
            "string",
            false, //Marking the parameter as not required
            false,
            RestApiParameterLocation.Query,
            RestApiParameterStyle.Form);

        var secondParameter = new RestApiParameter(
            "p2",
            "string",
            true, //Marking the parameter as required
            false,
            RestApiParameterLocation.Query,
            RestApiParameterStyle.Form);

        var operation = new RestApiOperation(
            "fake-id",
            [new RestApiServer("https://fake-random-test-host")],
            "fake-path",
            HttpMethod.Get,
            "fake-description",
            [firstParameter, secondParameter],
            new Dictionary<string, RestApiExpectedResponse>(),
            []
        );

        var arguments = new KernelArguments
        {
            { "p2", "v2" } //Providing argument for the required parameter only
        };

        var sut = new RestApiOperationRunner(_httpClient, _authenticationHandlerMock.Object);

        // Act
        var result = await sut.RunAsync(operation, arguments);

        // Assert
        Assert.NotNull(_httpMessageHandlerStub.RequestUri);
        Assert.Equal("https://fake-random-test-host/fake-path?p2=v2", _httpMessageHandlerStub.RequestUri.AbsoluteUri);
    }


    [Fact]
    public async Task ItShouldThrowExceptionIfNoArgumentProvidedForRequiredQueryStringParameterAsync()
    {
        // Arrange
        _httpMessageHandlerStub.ResponseToReturn.Content = new StringContent("fake-content", Encoding.UTF8, MediaTypeNames.Application.Json);

        var parameter = new RestApiParameter(
            "p1",
            "string",
            true, //Marking the parameter as required
            false,
            RestApiParameterLocation.Query,
            RestApiParameterStyle.Form);

        var operation = new RestApiOperation(
            "fake-id",
            [new RestApiServer("https://fake-random-test-host")],
            "fake-path",
            HttpMethod.Get,
            "fake-description",
            [parameter],
            new Dictionary<string, RestApiExpectedResponse>(),
            []
        );

        var arguments = new KernelArguments(); //Providing no arguments

        var sut = new RestApiOperationRunner(_httpClient, _authenticationHandlerMock.Object);

        // Act and Assert
        await Assert.ThrowsAsync<KernelException>(() => sut.RunAsync(operation, arguments));
    }


    [Theory]
    [InlineData(MediaTypeNames.Application.Json)]
    [InlineData(MediaTypeNames.Application.Xml)]
    [InlineData(MediaTypeNames.Text.Plain)]
    [InlineData(MediaTypeNames.Text.Html)]
    [InlineData(MediaTypeNames.Text.Xml)]
    [InlineData("text/csv")]
    [InlineData("text/markdown")]
    public async Task ItShouldReadContentAsStringSuccessfullyAsync(string contentType)
    {
        // Arrange
        _httpMessageHandlerStub.ResponseToReturn.Content = new StringContent("fake-content", Encoding.UTF8, contentType);

        var operation = new RestApiOperation(
            "fake-id",
            [new RestApiServer("https://fake-random-test-host")],
            "fake-path",
            HttpMethod.Post,
            "fake-description",
            [],
            new Dictionary<string, RestApiExpectedResponse>(),
            []
        );

        var arguments = new KernelArguments
        {
            { "payload", JsonSerializer.Serialize(new { value = "fake-value" }) },
            { "content-type", "application/json" }
        };

        var sut = new RestApiOperationRunner(_httpClient, _authenticationHandlerMock.Object);

        // Act
        var result = await sut.RunAsync(operation, arguments);

        // Assert
        Assert.NotNull(result);

        Assert.Equal("fake-content", result.Content);

        Assert.Equal($"{contentType}; charset=utf-8", result.ContentType);
    }


    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/gif")]
    [InlineData("image/svg+xml")]
    [InlineData("image/bmp")]
    [InlineData("image/x-icon")]
    public async Task ItShouldReadContentAsBytesSuccessfullyAsync(string contentType)
    {
        // Arrange
        _httpMessageHandlerStub.ResponseToReturn.Content = new ByteArrayContent([00, 01, 02]);
        _httpMessageHandlerStub.ResponseToReturn.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var operation = new RestApiOperation(
            "fake-id",
            [new RestApiServer("https://fake-random-test-host")],
            "fake-path",
            HttpMethod.Post,
            "fake-description",
            [],
            new Dictionary<string, RestApiExpectedResponse>(),
            []
        );

        var arguments = new KernelArguments
        {
            { "payload", JsonSerializer.Serialize(new { value = "fake-value" }) },
            { "content-type", "application/json" }
        };

        var sut = new RestApiOperationRunner(_httpClient, _authenticationHandlerMock.Object);

        // Act
        var result = await sut.RunAsync(operation, arguments);

        // Assert
        Assert.NotNull(result);

        Assert.Equal(new byte[] { 00, 01, 02 }, result.Content);

        Assert.Equal($"{contentType}", result.ContentType);
    }


    [Fact]
    public async Task ItShouldThrowExceptionForUnsupportedContentTypeAsync()
    {
        // Arrange
        _httpMessageHandlerStub.ResponseToReturn.Content = new StringContent("fake-content", Encoding.UTF8, "fake/type");

        var operation = new RestApiOperation(
            "fake-id",
            [new RestApiServer("https://fake-random-test-host")],
            "fake-path",
            HttpMethod.Post,
            "fake-description",
            [],
            new Dictionary<string, RestApiExpectedResponse>(),
            []
        );

        var arguments = new KernelArguments
        {
            { "payload", JsonSerializer.Serialize(new { value = "fake-value" }) },
            { "content-type", "application/json" }
        };

        var sut = new RestApiOperationRunner(_httpClient, _authenticationHandlerMock.Object);

        // Act & Assert
        var kernelException = await Assert.ThrowsAsync<KernelException>(() => sut.RunAsync(operation, arguments));
        Assert.Equal("The content type `fake/type` is not supported.", kernelException.Message);
        Assert.Equal("POST", kernelException.Data["http.request.method"]);
        Assert.Equal("https://fake-random-test-host/fake-path", kernelException.Data["url.full"]);
        Assert.Equal("{\"value\":\"fake-value\"}", kernelException.Data["http.request.body"]);
    }


    [Fact]
    public async Task ItShouldReturnRequestUriAndContentAsync()
    {
        // Arrange
        _httpMessageHandlerStub.ResponseToReturn.Content = new StringContent("fake-content", Encoding.UTF8, MediaTypeNames.Application.Json);

        List<RestApiPayloadProperty> payloadProperties =
        [
            new("name",
                "string",
                true,
                []),
            new("attributes",
                "object",
                false,
                [
                    new RestApiPayloadProperty("enabled",
                        "boolean",
                        false,
                        [])
                ])
        ];

        var payload = new RestApiPayload(MediaTypeNames.Application.Json, payloadProperties);

        var operation = new RestApiOperation(
            "fake-id",
            [new RestApiServer("https://fake-random-test-host")],
            "fake-path",
            HttpMethod.Post,
            "fake-description",
            [],
            new Dictionary<string, RestApiExpectedResponse>(),
            [],
            payload
        );

        var arguments = new KernelArguments
        {
            { "name", "fake-name-value" },
            { "enabled", true }
        };

        var sut = new RestApiOperationRunner(_httpClient, _authenticationHandlerMock.Object, enableDynamicPayload: true);

        // Act
        var result = await sut.RunAsync(operation, arguments);

        // Assert
        Assert.NotNull(result.RequestMethod);
        Assert.Equal(HttpMethod.Post.Method, result.RequestMethod);
        Assert.NotNull(result.RequestUri);
        Assert.Equal("https://fake-random-test-host/fake-path", result.RequestUri.AbsoluteUri);
        Assert.NotNull(result.RequestPayload);
        Assert.IsType<JsonObject>(result.RequestPayload);
        Assert.Equal("{\"name\":\"fake-name-value\",\"attributes\":{\"enabled\":true}}", ((JsonObject)result.RequestPayload).ToJsonString());
    }


    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.Accepted)]
    [InlineData(HttpStatusCode.Created)]
    [Theory]
    public async Task ItShouldHandleNoContentAsync(HttpStatusCode statusCode)
    {
        // Arrange
        _httpMessageHandlerStub!.ResponseToReturn = new HttpResponseMessage(statusCode);

        List<RestApiPayloadProperty> payloadProperties =
        [
            new("name",
                "string",
                true,
                []),
            new("attributes",
                "object",
                false,
                [
                    new RestApiPayloadProperty("enabled",
                        "boolean",
                        false,
                        [])
                ])
        ];

        var payload = new RestApiPayload(MediaTypeNames.Application.Json, payloadProperties);

        var operation = new RestApiOperation(
            "fake-id",
            [new RestApiServer("https://fake-random-test-host")],
            "fake-path",
            HttpMethod.Post,
            "fake-description",
            [],
            new Dictionary<string, RestApiExpectedResponse>(),
            [],
            payload
        );

        var arguments = new KernelArguments
        {
            { "name", "fake-name-value" },
            { "enabled", true }
        };

        var sut = new RestApiOperationRunner(_httpClient, _authenticationHandlerMock.Object, enableDynamicPayload: true);

        // Act
        var result = await sut.RunAsync(operation, arguments);

        // Assert
        Assert.NotNull(result.RequestMethod);
        Assert.Equal(HttpMethod.Post.Method, result.RequestMethod);
        Assert.NotNull(result.RequestUri);
        Assert.Equal("https://fake-random-test-host/fake-path", result.RequestUri.AbsoluteUri);
        Assert.NotNull(result.RequestPayload);
        Assert.IsType<JsonObject>(result.RequestPayload);
        Assert.Equal("{\"name\":\"fake-name-value\",\"attributes\":{\"enabled\":true}}", ((JsonObject)result.RequestPayload).ToJsonString());
    }


    [Fact]
    public async Task ItShouldSetHttpRequestMessageOptionsAsync()
    {
        // Arrange
        _httpMessageHandlerStub.ResponseToReturn.Content = new StringContent("fake-content", Encoding.UTF8, MediaTypeNames.Application.Json);

        List<RestApiPayloadProperty> payloadProperties =
        [
            new("name",
                "string",
                true,
                []),
            new("attributes",
                "object",
                false,
                [
                    new RestApiPayloadProperty("enabled",
                        "boolean",
                        false,
                        [])
                ])
        ];

        var payload = new RestApiPayload(MediaTypeNames.Application.Json, payloadProperties);

        var operation = new RestApiOperation(
            "fake-id",
            [new RestApiServer("https://fake-random-test-host")],
            "fake-path",
            HttpMethod.Post,
            "fake-description",
            [],
            new Dictionary<string, RestApiExpectedResponse>(),
            [],
            payload
        );

        var arguments = new KernelArguments
        {
            { "name", "fake-name-value" },
            { "enabled", true }
        };

        var options = new RestApiOperationRunOptions
        {
            Kernel = new Kernel(),
            KernelFunction = KernelFunctionFactory.CreateFromMethod(() => false),
            KernelArguments = arguments
        };

        var sut = new RestApiOperationRunner(_httpClient, _authenticationHandlerMock.Object, enableDynamicPayload: true);

        // Act
        var result = await sut.RunAsync(operation, arguments, options);

        // Assert
        var requestMessage = _httpMessageHandlerStub.RequestMessage;
        Assert.NotNull(requestMessage);
        Assert.True(requestMessage.Options.TryGetValue(OpenApiKernelFunctionContext.KernelFunctionContextKey, out var kernelFunctionContext));
        Assert.NotNull(kernelFunctionContext);
        Assert.Equal(options.Kernel, kernelFunctionContext.Kernel);
        Assert.Equal(options.KernelFunction, kernelFunctionContext.Function);
        Assert.Equal(options.KernelArguments, kernelFunctionContext.Arguments);
    }


    [Theory]
    [MemberData(nameof(RestApiOperationRunnerExceptions))]
    public async Task ItShouldIncludeRequestDataWhenOperationExecutionFailsAsync(Type expectedExceptionType, string expectedExceptionMessage, Exception expectedException)
    {
        // Arrange
        _httpMessageHandlerStub.ExceptionToThrow = expectedException;

        var operation = new RestApiOperation(
            "fake-id",
            [new RestApiServer("https://fake-random-test-host")],
            "fake-path",
            HttpMethod.Post,
            "fake-description",
            [],
            new Dictionary<string, RestApiExpectedResponse>(),
            []
        );

        var arguments = new KernelArguments
        {
            { "payload", JsonSerializer.Serialize(new { value = "fake-value" }) },
            { "content-type", "application/json" }
        };

        var sut = new RestApiOperationRunner(_httpClient, _authenticationHandlerMock.Object);

        // Act & Assert
        var actualException = await Assert.ThrowsAsync(expectedExceptionType, () => sut.RunAsync(operation, arguments));
        Assert.Equal(expectedExceptionMessage, actualException.Message);
        Assert.Equal("POST", actualException.Data["http.request.method"]);
        Assert.Equal("https://fake-random-test-host/fake-path", actualException.Data["url.full"]);
        Assert.Equal("{\"value\":\"fake-value\"}", actualException.Data["http.request.body"]);
        Assert.NotNull(actualException.Data["http.request.options"]);
    }


    /// <summary>
    /// Exceptions to thrown by <see cref="RestApiOperationRunner"/>.
    /// </summary>
    public static TheoryData<Type, string, Exception> RestApiOperationRunnerExceptions => new()
    {
        { typeof(HttpOperationException), "An error occurred during the HTTP operation.", new HttpOperationException("An error occurred during the HTTP operation.") },
        { typeof(OperationCanceledException), "The operation was canceled.", new OperationCanceledException("The operation was canceled.") },
        { typeof(KernelException), "A critical kernel error occurred.", new KernelException("A critical kernel error occurred.") }
    };


    [Fact]
    public async Task ItShouldUseCustomHttpResponseContentReaderAsync()
    {
        // Arrange
        var operation = new RestApiOperation(
            "fake-id",
            [new RestApiServer("https://fake-random-test-host")],
            "fake-path",
            HttpMethod.Get,
            "fake-description",
            [],
            new Dictionary<string, RestApiExpectedResponse>(),
            []
        );

        var expectedCancellationToken = new CancellationToken();

        async Task<object?> ReadHttpResponseContentAsync(HttpResponseContentReaderContext context, CancellationToken cancellationToken)
        {
            Assert.Equal(expectedCancellationToken, cancellationToken);

            return await context.Response.Content.ReadAsStreamAsync(cancellationToken);
        }

        var sut = new RestApiOperationRunner(_httpClient, _authenticationHandlerMock.Object, httpResponseContentReader: ReadHttpResponseContentAsync);

        // Act
        var response = await sut.RunAsync(operation, [], cancellationToken: expectedCancellationToken);

        // Assert
        Assert.IsAssignableFrom<Stream>(response.Content);
    }


    [Fact]
    public async Task ItShouldUseDefaultHttpResponseContentReaderIfCustomDoesNotReturnAnyContentAsync()
    {
        // Arrange
        _httpMessageHandlerStub.ResponseToReturn.Content = new StringContent("fake-content", Encoding.UTF8, MediaTypeNames.Application.Json);

        var operation = new RestApiOperation(
            "fake-id",
            [new RestApiServer("https://fake-random-test-host")],
            "fake-path",
            HttpMethod.Get,
            "fake-description",
            [],
            new Dictionary<string, RestApiExpectedResponse>(),
            []
        );

        var readerHasBeenCalled = false;

        Task<object?> ReadHttpResponseContentAsync(HttpResponseContentReaderContext context, CancellationToken cancellationToken)
        {
            readerHasBeenCalled = true;
            return Task.FromResult<object?>(null); // Return null to indicate that no content is returned
        }

        var sut = new RestApiOperationRunner(_httpClient, _authenticationHandlerMock.Object, httpResponseContentReader: ReadHttpResponseContentAsync);

        // Act
        var response = await sut.RunAsync(operation, []);

        // Assert
        Assert.True(readerHasBeenCalled);
        Assert.Equal("fake-content", response.Content);
    }


    [Fact]
    public async Task ItShouldDisposeContentStreamAndHttpResponseContentMessageAsync()
    {
        // Arrange
        var operation = new RestApiOperation(
            "fake-id",
            [new RestApiServer("https://fake-random-test-host")],
            "fake-path",
            HttpMethod.Get,
            "fake-description",
            [],
            new Dictionary<string, RestApiExpectedResponse>(),
            []
        );

        HttpResponseMessage? responseMessage = null;
        Stream? contentStream = null;

        async Task<object?> ReadHttpResponseContentAsync(HttpResponseContentReaderContext context, CancellationToken cancellationToken)
        {
            responseMessage = context.Response;
            contentStream = await context.Response.Content.ReadAsStreamAsync(cancellationToken);
            return contentStream;
        }

        var sut = new RestApiOperationRunner(_httpClient, _authenticationHandlerMock.Object, httpResponseContentReader: ReadHttpResponseContentAsync);

        // Act
        var response = await sut.RunAsync(operation, []);

        // Assert
        var stream = Assert.IsAssignableFrom<Stream>(response.Content);
        Assert.True(stream.CanRead);
        Assert.True(stream.CanSeek);

        stream.Dispose();

        // Check that the content stream and the response message are disposed
        Assert.Throws<ObjectDisposedException>(() => responseMessage!.Version = Version.Parse("1.1.1"));
        Assert.False(contentStream!.CanRead);
        Assert.False(contentStream!.CanSeek);
    }


    [Fact]
    public async Task ItShouldUseRestApiOperationPayloadPropertyArgumentNameToLookupArgumentsAsync()
    {
        // Arrange
        _httpMessageHandlerStub.ResponseToReturn.Content = new StringContent("fake-content", Encoding.UTF8, MediaTypeNames.Application.Json);

        List<RestApiPayloadProperty> payloadProperties =
        [
            new("name",
                "string",
                true,
                []) { ArgumentName = "alt-name" },
            new("attributes",
                "object",
                false,
                [
                    new RestApiPayloadProperty("enabled",
                        "boolean",
                        false,
                        []) { ArgumentName = "alt-enabled" }
                ])
        ];

        var payload = new RestApiPayload(MediaTypeNames.Application.Json, payloadProperties);

        var operation = new RestApiOperation(
            "fake-id",
            [new RestApiServer("https://fake-random-test-host")],
            "fake-path",
            HttpMethod.Post,
            "fake-description",
            [],
            new Dictionary<string, RestApiExpectedResponse>(),
            [],
            payload
        );

        var arguments = new KernelArguments
        {
            { "alt-name", "fake-name-value" },
            { "alt-enabled", true }
        };

        var options = new RestApiOperationRunOptions
        {
            Kernel = new Kernel(),
            KernelFunction = KernelFunctionFactory.CreateFromMethod(() => false),
            KernelArguments = arguments
        };

        var sut = new RestApiOperationRunner(_httpClient, _authenticationHandlerMock.Object, enableDynamicPayload: true);

        // Act
        var result = await sut.RunAsync(operation, arguments, options);

        // Assert
        var requestContent = _httpMessageHandlerStub.RequestContent;
        Assert.NotNull(requestContent);

        var deserializedPayload = await JsonNode.ParseAsync(new MemoryStream(requestContent));
        Assert.NotNull(deserializedPayload);

        var nameProperty = deserializedPayload["name"]?.ToString();
        Assert.Equal("fake-name-value", nameProperty);

        var attributesProperty = deserializedPayload["attributes"];
        Assert.NotNull(attributesProperty);

        var enabledProperty = attributesProperty["enabled"]?.AsValue();
        Assert.NotNull(enabledProperty);
        Assert.Equal("true", enabledProperty.ToString());
    }


    [Fact]
    public async Task ItShouldUseRestApiOperationPayloadPropertyNameToLookupArgumentsIfNoArgumentNameProvidedAsync()
    {
        // Arrange
        _httpMessageHandlerStub.ResponseToReturn.Content = new StringContent("fake-content", Encoding.UTF8, MediaTypeNames.Application.Json);

        List<RestApiPayloadProperty> payloadProperties =
        [
            new("name",
                "string",
                true,
                []) { ArgumentName = "alt-name" },
            new("attributes",
                "object",
                false,
                [
                    new RestApiPayloadProperty("enabled",
                        "boolean",
                        false,
                        []) { ArgumentName = "alt-enabled" }
                ])
        ];

        var payload = new RestApiPayload(MediaTypeNames.Application.Json, payloadProperties);

        var operation = new RestApiOperation(
            "fake-id",
            [new RestApiServer("https://fake-random-test-host")],
            "fake-path",
            HttpMethod.Post,
            "fake-description",
            [],
            new Dictionary<string, RestApiExpectedResponse>(),
            [],
            payload
        );

        var arguments = new KernelArguments
        {
            { "name", "fake-name-value" },
            { "enabled", true }
        };

        var options = new RestApiOperationRunOptions
        {
            Kernel = new Kernel(),
            KernelFunction = KernelFunctionFactory.CreateFromMethod(() => false),
            KernelArguments = arguments
        };

        var sut = new RestApiOperationRunner(_httpClient, _authenticationHandlerMock.Object, enableDynamicPayload: true);

        // Act
        var result = await sut.RunAsync(operation, arguments, options);

        // Assert
        var requestContent = _httpMessageHandlerStub.RequestContent;
        Assert.NotNull(requestContent);

        var deserializedPayload = await JsonNode.ParseAsync(new MemoryStream(requestContent));
        Assert.NotNull(deserializedPayload);

        var nameProperty = deserializedPayload["name"]?.ToString();
        Assert.Equal("fake-name-value", nameProperty);

        var attributesProperty = deserializedPayload["attributes"];
        Assert.NotNull(attributesProperty);

        var enabledProperty = attributesProperty["enabled"]?.AsValue();
        Assert.NotNull(enabledProperty);
        Assert.Equal("true", enabledProperty.ToString());
    }


    [Fact]
    public async Task ItShouldUseUrlHeaderAndPayloadFactoriesIfProvidedAsync()
    {
        // Arrange
        _httpMessageHandlerStub.ResponseToReturn.Content = new StringContent("fake-content", Encoding.UTF8, MediaTypeNames.Application.Json);

        List<RestApiPayloadProperty> payloadProperties =
        [
            new("name",
                "string",
                true,
                [])
        ];

        var payload = new RestApiPayload(MediaTypeNames.Application.Json, payloadProperties);

        var expectedOperation = new RestApiOperation(
            "fake-id",
            [new RestApiServer("https://fake-random-test-host")],
            "fake-path",
            HttpMethod.Post,
            "fake-description",
            [],
            new Dictionary<string, RestApiExpectedResponse>(),
            [],
            payload
        );

        var expectedArguments = new KernelArguments();

        var expectedOptions = new RestApiOperationRunOptions
        {
            Kernel = new Kernel(),
            KernelFunction = KernelFunctionFactory.CreateFromMethod(() => false),
            KernelArguments = expectedArguments
        };

        bool createUrlFactoryCalled = false;
        bool createHeadersFactoryCalled = false;
        bool createPayloadFactoryCalled = false;

        Uri CreateUrl(RestApiOperation operation, IDictionary<string, object?> arguments, RestApiOperationRunOptions? options)
        {
            createUrlFactoryCalled = true;
            Assert.Same(expectedOperation, operation);
            Assert.Same(expectedArguments, arguments);
            Assert.Same(expectedOptions, options);

            return new Uri("https://fake-random-test-host-from-factory/");
        }

        IDictionary<string, string>? CreateHeaders(RestApiOperation operation, IDictionary<string, object?> arguments, RestApiOperationRunOptions? options)
        {
            createHeadersFactoryCalled = true;
            Assert.Same(expectedOperation, operation);
            Assert.Same(expectedArguments, arguments);
            Assert.Same(expectedOptions, options);

            return new Dictionary<string, string> { ["header-from-factory"] = "value-of-header-from-factory" };
        }

        (object Payload, HttpContent Content)? CreatePayload(
            RestApiOperation operation,
            IDictionary<string, object?> arguments,
            bool enableDynamicPayload,
            bool enablePayloadNamespacing,
            RestApiOperationRunOptions? options)
        {
            createPayloadFactoryCalled = true;
            Assert.Same(expectedOperation, operation);
            Assert.Same(expectedArguments, arguments);
            Assert.True(enableDynamicPayload);
            Assert.True(enablePayloadNamespacing);
            Assert.Same(expectedOptions, options);

            var json = """{"name":"fake-name-value"}""";

            return ((JsonObject)JsonObject.Parse(json)!, new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json));
        }

        var sut = new RestApiOperationRunner(
            _httpClient,
            enableDynamicPayload: true,
            enablePayloadNamespacing: true,
            urlFactory: CreateUrl,
            headersFactory: CreateHeaders,
            payloadFactory: CreatePayload);

        // Act
        var result = await sut.RunAsync(expectedOperation, expectedArguments, expectedOptions);

        // Assert
        Assert.True(createUrlFactoryCalled);
        Assert.True(createHeadersFactoryCalled);
        Assert.True(createPayloadFactoryCalled);

        // Assert url factory
        Assert.NotNull(_httpMessageHandlerStub.RequestUri);
        Assert.Equal("https://fake-random-test-host-from-factory/", _httpMessageHandlerStub.RequestUri.AbsoluteUri);

        // Assert headers factory
        Assert.NotNull(_httpMessageHandlerStub.RequestHeaders);
        Assert.Equal(3, _httpMessageHandlerStub.RequestHeaders.Count());

        Assert.Contains(_httpMessageHandlerStub.RequestHeaders, h => h.Key == "header-from-factory" && h.Value.Contains("value-of-header-from-factory"));
        Assert.Contains(_httpMessageHandlerStub.RequestHeaders, h => h.Key == "User-Agent" && h.Value.Contains("Semantic-Kernel"));
        Assert.Contains(_httpMessageHandlerStub.RequestHeaders, h => h.Key == "Semantic-Kernel-Version");

        // Assert payload factory
        var messageContent = _httpMessageHandlerStub.RequestContent;
        Assert.NotNull(messageContent);

        var deserializedPayload = await JsonNode.ParseAsync(new MemoryStream(messageContent));
        Assert.NotNull(deserializedPayload);

        var nameProperty = deserializedPayload["name"]?.ToString();
        Assert.Equal("fake-name-value", nameProperty);

        Assert.NotNull(result.RequestPayload);
        Assert.IsType<JsonObject>(result.RequestPayload);
        Assert.Equal("""{"name":"fake-name-value"}""", ((JsonObject)result.RequestPayload).ToJsonString());
    }


    public class SchemaTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[]
            {
                "default",
                new[]
                {
                    ("400", new RestApiExpectedResponse("fake-content", "fake-content-type", KernelJsonSchema.Parse(ResourceResponseProvider.LoadFromResource("FakeResponseSchema.json")))),
                    ("default", new RestApiExpectedResponse("Default response content", "application/json", KernelJsonSchema.Parse(ResourceResponseProvider.LoadFromResource("DefaultResponseSchema.json"))))
                }
            };
            yield return new object[]
            {
                "200",
                new[]
                {
                    ("200", new RestApiExpectedResponse("fake-content", "fake-content-type", KernelJsonSchema.Parse(ResourceResponseProvider.LoadFromResource("FakeResponseSchema.json")))),
                    ("default", new RestApiExpectedResponse("Default response content", "application/json", KernelJsonSchema.Parse(ResourceResponseProvider.LoadFromResource("DefaultResponseSchema.json"))))
                }
            };
            yield return new object[]
            {
                "2XX",
                new[]
                {
                    ("2XX", new RestApiExpectedResponse("fake-content", "fake-content-type", KernelJsonSchema.Parse(ResourceResponseProvider.LoadFromResource("FakeResponseSchema.json")))),
                    ("default", new RestApiExpectedResponse("Default response content", "application/json", KernelJsonSchema.Parse(ResourceResponseProvider.LoadFromResource("DefaultResponseSchema.json"))))
                }
            };
            yield return new object[]
            {
                "2XX",
                new[]
                {
                    ("2XX", new RestApiExpectedResponse("fake-content", "fake-content-type", KernelJsonSchema.Parse(ResourceResponseProvider.LoadFromResource("FakeResponseSchema.json")))),
                    ("default", new RestApiExpectedResponse("Default response content", "application/json", KernelJsonSchema.Parse(ResourceResponseProvider.LoadFromResource("DefaultResponseSchema.json"))))
                }
            };
            yield return new object[]
            {
                "200",
                new[]
                {
                    ("default", new RestApiExpectedResponse("Default response content", "application/json", KernelJsonSchema.Parse(ResourceResponseProvider.LoadFromResource("DefaultResponseSchema.json")))),
                    ("2XX", new RestApiExpectedResponse("fake-content", "fake-content-type", KernelJsonSchema.Parse(ResourceResponseProvider.LoadFromResource("2XXFakeResponseSchema.json")))),
                    ("200", new RestApiExpectedResponse("fake-content", "fake-content-type", KernelJsonSchema.Parse(ResourceResponseProvider.LoadFromResource("200FakeResponseSchema.json"))))
                }
            };
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }


    [Theory]
    [ClassData(typeof(SchemaTestData))]
    public async Task ItShouldReturnExpectedSchemaAsync(string expectedStatusCode, params (string, RestApiExpectedResponse)[] responses)
    {
        var operation = new RestApiOperation(
            "fake-id",
            [new RestApiServer("https://fake-random-test-host")],
            "fake-path",
            HttpMethod.Get,
            "fake-description",
            [],
            responses.ToDictionary(item => item.Item1, item => item.Item2),
            []
        );

        var sut = new RestApiOperationRunner(_httpClient, _authenticationHandlerMock.Object);

        // Act
        var result = await sut.RunAsync(operation, []);

        Assert.NotNull(result);
        var expected = responses.First(r => r.Item1 == expectedStatusCode).Item2.Schema;
        Assert.Equal(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(result.ExpectedSchema));
    }


    [Theory]
    [InlineData("application/json;x-api-version=2.0", "application/json")]
    [InlineData("application/json ; x-api-version=2.0", "application/json")]
    [InlineData(" application/JSON; x-api-version=2.0", "application/json")]
    [InlineData(" TEXT/PLAIN ; x-api-version=2.0", "text/plain")]
    public async Task ItShouldNormalizeContentTypeArgumentAsync(string actualContentType, string normalizedContentType)
    {
        // Arrange
        _httpMessageHandlerStub.ResponseToReturn.Content = new StringContent("fake-content", Encoding.UTF8, MediaTypeNames.Text.Plain);

        var operation = new RestApiOperation(
            "fake-id",
            [new RestApiServer("https://fake-random-test-host")],
            "fake-path",
            HttpMethod.Post,
            "fake-description",
            [],
            new Dictionary<string, RestApiExpectedResponse>(),
            []
        );

        var arguments = new KernelArguments
        {
            { "payload", "fake-input-value" },
            { "content-type", actualContentType }
        };

        var sut = new RestApiOperationRunner(_httpClient, _authenticationHandlerMock.Object, enableDynamicPayload: false);

        // Act
        var result = await sut.RunAsync(operation, arguments);

        // Assert
        Assert.NotNull(_httpMessageHandlerStub.ContentHeaders);
        Assert.Contains(_httpMessageHandlerStub.ContentHeaders, h => h.Key == "Content-Type" && h.Value.Any(h => h.StartsWith(normalizedContentType, StringComparison.InvariantCulture)));
    }


    [Fact]
    public async Task ItShouldProvideValidContextToRestApiOperationResponseFactoryAsync()
    {
        // Arrange
        _httpMessageHandlerStub.ResponseToReturn.Content = new StringContent("fake-content", Encoding.UTF8, MediaTypeNames.Text.Plain);

        RestApiOperationResponseFactoryContext? factoryContext = null;
        RestApiOperationResponse? factoryInternalResponse = null;
        CancellationToken? factoryCancellationToken = null;

        async Task<RestApiOperationResponse> RestApiOperationResponseFactory(RestApiOperationResponseFactoryContext context, CancellationToken cancellationToken)
        {
            factoryContext = context;
            factoryInternalResponse = await context.InternalFactory(context, cancellationToken);
            factoryCancellationToken = cancellationToken;

            return factoryInternalResponse;
        }

        var operation = new RestApiOperation(
            "fake-id",
            [new RestApiServer("https://fake-random-test-host")],
            "fake-path",
            HttpMethod.Post,
            "fake-description",
            [],
            new Dictionary<string, RestApiExpectedResponse>(),
            []
        );

        var arguments = new KernelArguments
        {
            { "payload", "fake-input-value" },
            { "content-type", "text/plain" }
        };

        var sut = new RestApiOperationRunner(_httpClient, responseFactory: RestApiOperationResponseFactory);

        using var cancellationTokenSource = new CancellationTokenSource();

        var cancellationToken = cancellationTokenSource.Token;

        // Act
        var response = await sut.RunAsync(operation, arguments, cancellationToken: cancellationToken);

        // Assert
        Assert.NotNull(factoryContext);
        Assert.Same(operation, factoryContext.Operation);
        Assert.Same(_httpMessageHandlerStub.RequestMessage, factoryContext.Request);
        Assert.Same(_httpMessageHandlerStub.ResponseToReturn, factoryContext.Response);

        Assert.Same(factoryInternalResponse, response);

        Assert.Equal(cancellationToken, factoryCancellationToken);
    }


    [Fact]
    public async Task ItShouldWrapStreamContentIntoHttpResponseStreamAsync()
    {
        // Arrange
        _httpMessageHandlerStub.ResponseToReturn.Content = new StringContent("fake-content", Encoding.UTF8, MediaTypeNames.Text.Plain);

        var factoryStream = new MemoryStream();

        async Task<RestApiOperationResponse> RestApiOperationResponseFactory(RestApiOperationResponseFactoryContext context, CancellationToken cancellationToken)
        {
            return await Task.FromResult(new RestApiOperationResponse(factoryStream, MediaTypeNames.Text.Plain));
        }

        var operation = new RestApiOperation(
            "fake-id",
            [new RestApiServer("https://fake-random-test-host")],
            "fake-path",
            HttpMethod.Post,
            "fake-description",
            [],
            new Dictionary<string, RestApiExpectedResponse>(),
            []
        );

        var arguments = new KernelArguments
        {
            { "payload", "fake-input-value" },
            { "content-type", "text/plain" }
        };

        var sut = new RestApiOperationRunner(_httpClient, responseFactory: RestApiOperationResponseFactory);

        // Act
        var response = await sut.RunAsync(operation, arguments);

        // Assert
        var httpResponseStream = Assert.IsType<HttpResponseStream>(response.Content);

        // Assert that neither the HttResponseMessage nor stream returned by factory is disposed
        _httpMessageHandlerStub.ResponseToReturn!.Version = Version.Parse("1.1.1");
        Assert.True(factoryStream!.CanRead);
        Assert.True(factoryStream!.CanSeek);

        // Dispose the response stream
        httpResponseStream.Dispose();

        // Assert both the stream and the response message are disposed
        Assert.Throws<ObjectDisposedException>(() => _httpMessageHandlerStub.ResponseToReturn!.Version = Version.Parse("1.1.1"));
        Assert.False(httpResponseStream!.CanRead);
        Assert.False(httpResponseStream!.CanSeek);
    }


    [Fact]
    public async Task ItShouldNotWrapStreamContentIntoHttpResponseStreamIfItIsAlreadyOfHttpResponseStreamTypeAsync()
    {
        // Arrange
        _httpMessageHandlerStub.ResponseToReturn.Content = new StringContent("fake-content", Encoding.UTF8, MediaTypeNames.Text.Plain);

#pragma warning disable CA2000 // Dispose objects before losing scope
        await using var httpResponseStream = new HttpResponseStream(new MemoryStream(), new HttpResponseMessage());
#pragma warning restore CA2000 // Dispose objects before losing scope

        async Task<RestApiOperationResponse> RestApiOperationResponseFactory(RestApiOperationResponseFactoryContext context, CancellationToken cancellationToken)
        {
            return await Task.FromResult(new RestApiOperationResponse(httpResponseStream, MediaTypeNames.Text.Plain));
        }

        var operation = new RestApiOperation(
            "fake-id",
            [new RestApiServer("https://fake-random-test-host")],
            "fake-path",
            HttpMethod.Post,
            "fake-description",
            [],
            new Dictionary<string, RestApiExpectedResponse>(),
            []
        );

        var arguments = new KernelArguments
        {
            { "payload", "fake-input-value" },
            { "content-type", "text/plain" }
        };

        var sut = new RestApiOperationRunner(_httpClient, responseFactory: RestApiOperationResponseFactory);

        // Act
        var response = await sut.RunAsync(operation, arguments);

        // Assert
        Assert.Same(httpResponseStream, response.Content);
    }


    [Fact]
    public async Task ItShouldAllowRequestWhenNoValidationOptionsConfiguredAsync()
    {
        // Arrange - no validation options (default behavior)
        var operation = new RestApiOperation(
            "test",
            [new RestApiServer("http://internal-service:8080")],
            "/api/data",
            HttpMethod.Get,
            "test operation",
            [],
            new Dictionary<string, RestApiExpectedResponse>(),
            []
        );

        var sut = new RestApiOperationRunner(_httpClient, _authenticationHandlerMock.Object);

        // Act & Assert - should not throw
        await sut.RunAsync(operation, []);
    }


    [Fact]
    public async Task ItShouldBlockRequestWithDisallowedSchemeAsync()
    {
        // Arrange
        var operation = new RestApiOperation(
            "test",
            [new RestApiServer("http://api.example.com")],
            "/api/data",
            HttpMethod.Get,
            "test operation",
            [],
            new Dictionary<string, RestApiExpectedResponse>(),
            []
        );

        var validationOptions = new RestApiOperationServerUrlValidationOptions();
        // Default AllowedSchemes is ["https"], so "http" should be blocked.

        var sut = new RestApiOperationRunner(_httpClient, _authenticationHandlerMock.Object, serverUrlValidationOptions: validationOptions);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RunAsync(operation, []));
        Assert.Contains("http", exception.Message);
        Assert.Contains("not allowed", exception.Message);
    }


    [Fact]
    public async Task ItShouldAllowRequestWithAllowedSchemeAsync()
    {
        // Arrange
        var operation = new RestApiOperation(
            "test",
            [new RestApiServer("https://api.example.com")],
            "/api/data",
            HttpMethod.Get,
            "test operation",
            [],
            new Dictionary<string, RestApiExpectedResponse>(),
            []
        );

        var validationOptions = new RestApiOperationServerUrlValidationOptions();

        var sut = new RestApiOperationRunner(_httpClient, _authenticationHandlerMock.Object, serverUrlValidationOptions: validationOptions);

        // Act & Assert - should not throw
        await sut.RunAsync(operation, []);
    }


    [Fact]
    public async Task ItShouldBlockRequestNotMatchingAllowedBaseUrlsAsync()
    {
        // Arrange
        var operation = new RestApiOperation(
            "test",
            [new RestApiServer("https://evil.com")],
            "/steal-data",
            HttpMethod.Get,
            "test operation",
            [],
            new Dictionary<string, RestApiExpectedResponse>(),
            []
        );

        var validationOptions = new RestApiOperationServerUrlValidationOptions
        {
            AllowedBaseUrls = [new Uri("https://api.example.com")]
        };

        var sut = new RestApiOperationRunner(_httpClient, _authenticationHandlerMock.Object, serverUrlValidationOptions: validationOptions);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RunAsync(operation, []));
        Assert.Contains("not allowed", exception.Message);
        Assert.Contains("does not match", exception.Message);
    }


    [Fact]
    public async Task ItShouldAllowRequestMatchingAllowedBaseUrlsAsync()
    {
        // Arrange
        var operation = new RestApiOperation(
            "test",
            [new RestApiServer("https://api.example.com")],
            "/users",
            HttpMethod.Get,
            "test operation",
            [],
            new Dictionary<string, RestApiExpectedResponse>(),
            []
        );

        var validationOptions = new RestApiOperationServerUrlValidationOptions
        {
            AllowedBaseUrls = [new Uri("https://api.example.com")]
        };

        var sut = new RestApiOperationRunner(_httpClient, _authenticationHandlerMock.Object, serverUrlValidationOptions: validationOptions);

        // Act & Assert - should not throw
        await sut.RunAsync(operation, []);
    }


    [Fact]
    public async Task ItShouldBlockCloudMetadataEndpointAsync()
    {
        // Arrange - simulate SSRF targeting cloud metadata
        var operation = new RestApiOperation(
            "test",
            [new RestApiServer("https://169.254.169.254")],
            "/latest/meta-data/",
            HttpMethod.Get,
            "test operation",
            [],
            new Dictionary<string, RestApiExpectedResponse>(),
            []
        );

        var validationOptions = new RestApiOperationServerUrlValidationOptions
        {
            AllowedBaseUrls = [new Uri("https://api.example.com")]
        };

        var sut = new RestApiOperationRunner(_httpClient, _authenticationHandlerMock.Object, serverUrlValidationOptions: validationOptions);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RunAsync(operation, []));
    }


    [Fact]
    public async Task ItShouldAllowCustomSchemesWhenConfiguredAsync()
    {
        // Arrange
        var operation = new RestApiOperation(
            "test",
            [new RestApiServer("http://api.example.com")],
            "/api/data",
            HttpMethod.Get,
            "test operation",
            [],
            new Dictionary<string, RestApiExpectedResponse>(),
            []
        );

        var validationOptions = new RestApiOperationServerUrlValidationOptions
        {
            AllowedSchemes = ["http", "https"],
            AllowedBaseUrls = [new Uri("http://api.example.com")]
        };

        var sut = new RestApiOperationRunner(_httpClient, _authenticationHandlerMock.Object, serverUrlValidationOptions: validationOptions);

        // Act & Assert - should not throw
        await sut.RunAsync(operation, []);
    }


    /// <summary>
    /// Disposes resources used by this class.
    /// </summary>
    public void Dispose()
    {
        _httpMessageHandlerStub.Dispose();

        _httpClient.Dispose();
    }


    private sealed class HttpMessageHandlerStub : DelegatingHandler
    {
        public HttpRequestHeaders? RequestHeaders => RequestMessage?.Headers;

        public HttpContentHeaders? ContentHeaders => RequestMessage?.Content?.Headers;

        public byte[]? RequestContent { get; private set; }

        public Uri? RequestUri => RequestMessage?.RequestUri;

        public HttpMethod? Method => RequestMessage?.Method;

        public HttpRequestMessage? RequestMessage { get; private set; }

        public HttpResponseMessage ResponseToReturn { get; set; }

        public Exception? ExceptionToThrow { get; set; }


        public HttpMessageHandlerStub()
        {
            ResponseToReturn = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, MediaTypeNames.Application.Json)
            };
        }


        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            RequestMessage = request;
            RequestContent = request.Content is null
                ? null
                : await request.Content.ReadAsByteArrayAsync(cancellationToken);

            return await Task.FromResult(ResponseToReturn);
        }
    }
}
