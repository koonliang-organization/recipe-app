using FluentAssertions;
using Infrastructure.Persistence.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Infrastructure.Persistence.Tests;

public class ImageStorageServiceTests
{
    [Fact]
    public async Task UploadImage_ReturnsUrl_ForValidBase64()
    {
        var svc = new ImageStorageService(new NullLogger<ImageStorageService>());
        var base64 = Convert.ToBase64String(new byte[] { 1, 2, 3 }); // "AQID"
        var url = await svc.UploadImageAsync(base64, "file123");
        url.Should().Contain("file123").And.EndWith(".jpg");
    }

    [Fact]
    public async Task UploadImage_StripsDataUrlPrefix()
    {
        var svc = new ImageStorageService(new NullLogger<ImageStorageService>());
        var raw = Convert.ToBase64String(new byte[] { 10, 20, 30, 40 });
        var prefixed = $"data:image/png;base64,{raw}";
        var url = await svc.UploadImageAsync(prefixed, "img");
        url.Should().Contain("img");
    }

    [Fact]
    public async Task UploadImage_Throws_OnInvalidBase64()
    {
        var svc = new ImageStorageService(new NullLogger<ImageStorageService>());
        await Assert.ThrowsAsync<ArgumentException>(() => svc.UploadImageAsync("not_base64", "bad"));
    }

    [Fact]
    public async Task UploadImage_Throws_OnEmpty()
    {
        var svc = new ImageStorageService(new NullLogger<ImageStorageService>());
        await Assert.ThrowsAsync<ArgumentException>(() => svc.UploadImageAsync(" ", "empty"));
    }

    [Fact]
    public async Task DeleteImage_DoesNotThrow()
    {
        var svc = new ImageStorageService(new NullLogger<ImageStorageService>());
        await svc.DeleteImageAsync("http://example.com/x.jpg");
    }
}

