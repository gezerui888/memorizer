using Memorizer.Controllers;
using Memorizer.UnitTests.Tools;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Memorizer.UnitTests.Controllers;

public class SearchEvalControllerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("false")]
    public async Task Disabled_endpoints_return_not_found_without_accessing_storage(string? enabled)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["SearchEval:Enabled"] = enabled })
            .Build();
        // Any storage access would fail, so this also checks that the gate precedes side effects.
        var controller = new SearchEvalController(null!, NullLogger<SearchEvalController>.Instance, configuration);
        var request = new SearchEvalController.CompareRequest("test query");

        Assert.IsType<NotFoundObjectResult>((await controller.SeedCorpus(default)).Result);
        Assert.IsType<NotFoundObjectResult>((await controller.RunEvaluation(null, default)).Result);
        Assert.IsType<NotFoundObjectResult>(await controller.DeleteCorpus(default));
        Assert.IsType<NotFoundObjectResult>(await controller.GetCorpusStatus(default));
        Assert.IsType<NotFoundObjectResult>(await controller.CompareQuery(request, default));
        Assert.IsType<NotFoundObjectResult>(await controller.CompareOrgSearch(request, default));
    }

    [Fact]
    public async Task Enabled_endpoints_keep_existing_behavior()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["SearchEval:Enabled"] = "true" })
            .Build();
        var controller = new SearchEvalController(
            new FakeWorkspaceStorage(), NullLogger<SearchEvalController>.Instance, configuration);

        Assert.IsType<OkObjectResult>(await controller.GetCorpusStatus(default));
        Assert.IsType<OkObjectResult>(await controller.DeleteCorpus(default));
        Assert.IsType<OkObjectResult>(await controller.CompareOrgSearch(new("test query"), default));
        var result = Assert.IsType<BadRequestObjectResult>((await controller.RunEvaluation(null, default)).Result);
        Assert.Contains("Corpus not seeded", System.Text.Json.JsonSerializer.Serialize(result.Value));
    }
}
