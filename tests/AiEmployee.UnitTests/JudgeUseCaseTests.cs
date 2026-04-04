using AiEmployee.Application.Dtos;
using AiEmployee.Application.Interfaces;
using AiEmployee.Application.UseCases;

namespace AiEmployee.UnitTests;

public class JudgeUseCaseTests
{
    [Fact]
    public async Task Execute_Returns_JudgmentResult_From_Ai_PassingWinnerThrough()
    {
        var fakeClient = new FakeAiClient(new JudgmentResultDto
        {
            Winner = "Ali",
            Reason = "Ali provided stronger argument",
        });
        var useCase = new JudgeUseCase(fakeClient);

        var result = await useCase.Execute("u1", "hello");

        Assert.Equal("Ali", result.Winner);
        Assert.Equal("Ali provided stronger argument", result.Reason);
    }

    [Fact]
    public async Task Execute_PassesThroughWinner_WithoutNormalization()
    {
        var fakeClient = new FakeAiClient(new JudgmentResultDto
        {
            Winner = "Sam Taylor",
            Reason = "Clearer reasoning on the disputed point.",
        });
        var useCase = new JudgeUseCase(fakeClient);

        var result = await useCase.Execute("u1", "hello");

        Assert.Equal("Sam Taylor", result.Winner);
        Assert.Equal("Clearer reasoning on the disputed point.", result.Reason);
    }

    private sealed class FakeAiClient : IAiClient
    {
        private readonly JudgmentResultDto _dto;

        public FakeAiClient(JudgmentResultDto dto)
        {
            _dto = dto;
        }

        public Task<JudgmentResultDto> JudgeAsync(string userId, string text)
        {
            return Task.FromResult(_dto);
        }

        public Task<LeadClassificationDto> ClassifyLeadAsync(string prompt)
        {
            return Task.FromResult(new LeadClassificationDto());
        }
    }
}
