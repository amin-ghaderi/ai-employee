using AiEmployee.Application.Options;
using AiEmployee.Infrastructure.Options;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AiEmployee.UnitTests;

public sealed class PublicBaseUrlProviderTests
{
    private static PublicBaseUrlProvider Create(string? publicBaseUrl, string environmentName)
    {
        var options = Options.Create(new AppOptions { PublicBaseUrl = publicBaseUrl });
        var env = new StubHostEnvironment(environmentName);
        return new PublicBaseUrlProvider(options, env);
    }

    [Fact]
    public void GetPublicBaseUrl_Development_allows_http()
    {
        var p = Create("http://127.0.0.1:5155", Environments.Development);
        Assert.Equal("http://127.0.0.1:5155", p.GetPublicBaseUrl());
    }

    [Fact]
    public void GetPublicBaseUrl_Development_allows_https()
    {
        var p = Create("https://abc.ngrok-free.app", Environments.Development);
        Assert.Equal("https://abc.ngrok-free.app", p.GetPublicBaseUrl());
    }

    [Fact]
    public void GetPublicBaseUrl_Production_requires_https()
    {
        var p = Create("http://insecure.example.com", Environments.Production);
        var ex = Assert.Throws<InvalidOperationException>(() => p.GetPublicBaseUrl());
        Assert.Contains("HTTPS", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetPublicBaseUrl_Production_accepts_https()
    {
        var p = Create("https://secure.example.com/path/", Environments.Production);
        Assert.Equal("https://secure.example.com/path", p.GetPublicBaseUrl());
    }

    [Fact]
    public void GetPublicBaseUrl_trims_and_strips_trailing_slash()
    {
        var p = Create("  https://host/  ", Environments.Development);
        Assert.Equal("https://host", p.GetPublicBaseUrl());
    }

    [Fact]
    public void GetPublicBaseUrl_empty_returns_null()
    {
        var p = Create("", Environments.Production);
        Assert.Null(p.GetPublicBaseUrl());
    }

    [Fact]
    public void GetPublicBaseUrl_invalid_absolute_throws()
    {
        var p = Create("not-a-uri", Environments.Development);
        Assert.Throws<InvalidOperationException>(() => p.GetPublicBaseUrl());
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public StubHostEnvironment(string environmentName) =>
            EnvironmentName = environmentName;

        public string ApplicationName { get; set; } = "Test";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = "";
        public string EnvironmentName { get; set; }
    }
}
