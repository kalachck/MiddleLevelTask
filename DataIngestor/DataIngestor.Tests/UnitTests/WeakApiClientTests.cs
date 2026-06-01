using System.Net;
using AutoFixture;
using DataIngestor.Domain.Models;
using DataIngestor.Infrastructure;
using DataIngestor.Infrastructure.Configurations.Providers;
using DataIngestor.Tests.UnitTests.TestUtils;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace DataIngestor.Tests.UnitTests;

public class WeakApiClientTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly IWeakApiConfigProvider _configProvider = Substitute.For<IWeakApiConfigProvider>();

    public WeakApiClientTests()
    {
        _configProvider.GetMetersUrl().Returns("https://api.example.com/meters");
        _configProvider.GetHeaders().Returns(new Dictionary<string, string>());
    }

    [Fact]
    public async Task FetchReadingsAsync_WhenApiReturnsValidJson_ShouldReturnDeserializedReadings()
    {
        // Arrange
        const string responseJson = """
            [
                { "type": "energy", "Name": "MeterA", "Payload": { "value": 10 } },
                { "type": "motion", "Name": "MeterB", "Payload": { "value": 20 } }
            ]
            """;
        var handler = StubHttpMessageHandler.ReturnsJson(responseJson);
        var httpClient = new HttpClient(handler);
        var sut = new WeakApiClient(httpClient, NullLogger<WeakApiClient>.Instance, _configProvider);

        // Act
        var readings = (await sut.FetchReadingsAsync()).ToList();

        // Assert
        Assert.Equal(2, readings.Count);
        Assert.Equal(SensorType.Energy, readings[0].Type);
        Assert.Equal("MeterA", readings[0].Name);
        Assert.Equal(SensorType.Motion, readings[1].Type);
        Assert.Equal("MeterB", readings[1].Name);
    }

    [Fact]
    public async Task FetchReadingsAsync_WhenApiReturnsEmptyArray_ShouldReturnEmptyEnumerable()
    {
        // Arrange
        var handler = StubHttpMessageHandler.ReturnsJson("[]");
        var httpClient = new HttpClient(handler);
        var sut = new WeakApiClient(httpClient, NullLogger<WeakApiClient>.Instance, _configProvider);

        // Act
        var readings = await sut.FetchReadingsAsync();

        // Assert
        Assert.Empty(readings);
    }

    [Fact]
    public async Task FetchReadingsAsync_WhenApiReturnsNullBody_ShouldReturnEmptyEnumerable()
    {
        // Arrange
        var handler = StubHttpMessageHandler.ReturnsJson("null");
        var httpClient = new HttpClient(handler);
        var sut = new WeakApiClient(httpClient, NullLogger<WeakApiClient>.Instance, _configProvider);

        // Act
        var readings = await sut.FetchReadingsAsync();

        // Assert
        Assert.Empty(readings);
    }

    [Fact]
    public async Task FetchReadingsAsync_WhenApiReturnsErrorStatus_ShouldReturnEmptyEnumerable()
    {
        // Arrange
        var handler = StubHttpMessageHandler.WithStatus(HttpStatusCode.InternalServerError);
        var httpClient = new HttpClient(handler);
        var sut = new WeakApiClient(httpClient, NullLogger<WeakApiClient>.Instance, _configProvider);

        // Act
        var readings = await sut.FetchReadingsAsync();

        // Assert
        Assert.Empty(readings);
    }

    [Fact]
    public async Task FetchReadingsAsync_WhenHttpThrows_ShouldReturnEmptyEnumerable()
    {
        // Arrange
        var handler = StubHttpMessageHandler.Throws(new HttpRequestException("boom"));
        var httpClient = new HttpClient(handler);
        var sut = new WeakApiClient(httpClient, NullLogger<WeakApiClient>.Instance, _configProvider);

        // Act
        var readings = await sut.FetchReadingsAsync();

        // Assert
        Assert.Empty(readings);
    }

    [Fact]
    public async Task FetchReadingsAsync_WhenApiReturnsInvalidJson_ShouldReturnEmptyEnumerable()
    {
        // Arrange
        var handler = StubHttpMessageHandler.ReturnsJson("not-json");
        var httpClient = new HttpClient(handler);
        var sut = new WeakApiClient(httpClient, NullLogger<WeakApiClient>.Instance, _configProvider);

        // Act
        var readings = await sut.FetchReadingsAsync();

        // Assert
        Assert.Empty(readings);
    }

    [Fact]
    public async Task FetchReadingsAsync_WhenInvoked_ShouldRequestConfiguredMetersUrl()
    {
        // Arrange
        var expectedUrl = "https://api.example.com/v2/meters";
        _configProvider.GetMetersUrl().Returns(expectedUrl);
        var handler = StubHttpMessageHandler.ReturnsJson("[]");
        var httpClient = new HttpClient(handler);
        var sut = new WeakApiClient(httpClient, NullLogger<WeakApiClient>.Instance, _configProvider);

        // Act
        _ = await sut.FetchReadingsAsync();

        // Assert
        var sentRequest = Assert.Single(handler.SentRequests);
        Assert.Equal(HttpMethod.Get, sentRequest.Method);
        Assert.Equal(new Uri(expectedUrl), sentRequest.RequestUri);
    }

    [Fact]
    public async Task FetchReadingsAsync_WhenHeadersConfigured_ShouldAppendHeadersToRequest()
    {
        // Arrange
        var headers = new Dictionary<string, string>
        {
            ["X-Api-Key"] = _fixture.Create<string>(),
            ["X-Tenant"] = _fixture.Create<string>(),
        };
        _configProvider.GetHeaders().Returns(headers);
        var handler = StubHttpMessageHandler.ReturnsJson("[]");
        var httpClient = new HttpClient(handler);
        var sut = new WeakApiClient(httpClient, NullLogger<WeakApiClient>.Instance, _configProvider);

        // Act
        _ = await sut.FetchReadingsAsync();

        // Assert
        var sentRequest = Assert.Single(handler.SentRequests);
        foreach (var header in headers)
        {
            Assert.True(sentRequest.Headers.Contains(header.Key));
            Assert.Equal(header.Value, sentRequest.Headers.GetValues(header.Key).Single());
        }
    }

    [Fact]
    public async Task FetchReadingsAsync_WhenCancelled_ShouldReturnEmptyEnumerable()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var handler = new StubHttpMessageHandler((_, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        var httpClient = new HttpClient(handler);
        var sut = new WeakApiClient(httpClient, NullLogger<WeakApiClient>.Instance, _configProvider);

        // Act
        var readings = await sut.FetchReadingsAsync(cts.Token);

        // Assert
        Assert.Empty(readings);
    }
}
