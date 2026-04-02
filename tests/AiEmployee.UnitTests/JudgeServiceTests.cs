using AiEmployee.Domain.Interfaces;
using AiEmployee.Domain.Services;

namespace AiEmployee.UnitTests;

public class JudgeServiceTests
{
    [Fact]
    public async Task Process_Returns_AiResponse()
    {
        var fakeClient = new FakeAiClient("approved");
        var service = new JudgeService(fakeClient);

        var result = await service.Process("hello");

        Assert.Equal("approved", result);
    }

    private sealed class FakeAiClient : IAiClient
    {
        private readonly string _response;

        public FakeAiClient(string response)
        {
            _response = response;
        }

        public Task<string> AskAsync(string prompt)
        {
            return Task.FromResult(_response);
        }
    }
}
