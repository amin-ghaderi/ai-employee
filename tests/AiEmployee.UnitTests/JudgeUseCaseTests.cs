using AiEmployee.Application.BotConfig;
using AiEmployee.Application.Dtos;
using AiEmployee.Application.Interfaces;
using AiEmployee.Application.Options;
using AiEmployee.Application.Prompting;
using AiEmployee.Application.UseCases;
using AiEmployee.Domain.BotConfiguration;
using AiEmployee.Domain.Entities;
using Microsoft.Extensions.Options;

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
        var fakeRepo = new FakeJudgmentRepository();
        var useCase = CreateUseCase(fakeClient, fakeRepo);

        var result = await useCase.Execute("c1", "u1", "hello", CreateDefaultConfig());

        Assert.Equal("Ali", result.Winner);
        Assert.Equal("Ali provided stronger argument", result.Reason);
        Assert.Single(fakeRepo.Saved);
        Assert.Equal("c1", fakeRepo.Saved[0].ConversationId);
        Assert.Equal("u1", fakeRepo.Saved[0].UserId);
        Assert.Equal("hello", fakeRepo.Saved[0].InputText);
    }

    [Fact]
    public async Task Execute_PassesThroughWinner_WithoutNormalization()
    {
        var fakeClient = new FakeAiClient(new JudgmentResultDto
        {
            Winner = "Sam Taylor",
            Reason = "Clearer reasoning on the disputed point.",
        });
        var fakeRepo = new FakeJudgmentRepository();
        var useCase = CreateUseCase(fakeClient, fakeRepo);

        var result = await useCase.Execute("c1", "u1", "hello", CreateDefaultConfig());

        Assert.Equal("Sam Taylor", result.Winner);
        Assert.Equal("Clearer reasoning on the disputed point.", result.Reason);
        Assert.Single(fakeRepo.Saved);
    }

    private static JudgeUseCase CreateUseCase(FakeAiClient fakeClient, FakeJudgmentRepository fakeRepo)
    {
        var options = Options.Create(new AiOptions { UseFullJudgePrompt = false });
        var convRepo = new StubConversationRepository();
        return new JudgeUseCase(
            fakeClient,
            fakeRepo,
            options,
            convRepo,
            new PromptBuilder());
    }

    private static JudgeBotConfiguration CreateDefaultConfig() =>
        new(
            JudgeBotDefaults.CreateBot(),
            JudgeBotDefaults.CreatePersona(),
            JudgeBotDefaults.CreateBehavior(),
            JudgeBotDefaults.CreateLanguageProfile(),
            JudgeBotDefaults.CreateJudgeTranscriptWrapperTemplate());

    private sealed class StubConversationRepository : IConversationRepository
    {
        public Task<Conversation?> GetByIdAsync(string id) => Task.FromResult<Conversation?>(null);

        public Task SaveAsync(Conversation conversation) => Task.CompletedTask;
    }

    private sealed class FakeAiClient : IAiClient
    {
        private readonly JudgmentResultDto _dto;

        public FakeAiClient(JudgmentResultDto dto)
        {
            _dto = dto;
        }

        public Task<JudgmentResultDto> JudgeAsync(string userId, string text) =>
            Task.FromResult(_dto);

        public Task<JudgmentResultDto> JudgeWithFullPromptAsync(string userId, string prompt) =>
            Task.FromResult(_dto);

        public Task<LeadClassificationDto> ClassifyLeadAsync(string prompt) =>
            Task.FromResult(new LeadClassificationDto());
    }

    private sealed class FakeJudgmentRepository : IJudgmentRepository
    {
        public List<Judgment> Saved { get; } = new();

        public Task SaveAsync(Judgment judgment)
        {
            Saved.Add(judgment);
            return Task.CompletedTask;
        }
    }
}
