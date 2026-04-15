namespace AiEmployee.Domain.BotConfiguration;

/// <summary>
/// First bot configuration: reproduces current Telegram webhook + ai-service judge/lead/automation behavior as data.
/// </summary>
public static class JudgeBotDefaults
{
    public static readonly Guid BotId = new("f7c3b5a0-1111-4111-8111-111111111111");
    public static readonly Guid PersonaId = new("f7c3b5a0-2222-4222-8222-222222222222");
    public static readonly Guid BehaviorId = new("f7c3b5a0-3333-4333-8333-333333333333");
    public static readonly Guid LanguageProfileId = new("f7c3b5a0-4444-4444-8444-444444444444");
    public static readonly Guid JudgeTranscriptWrapperTemplateId = new("f7c3b5a0-5555-4555-8555-555555555555");

    public const string JudgeTranscriptWrapperTemplateName = "JudgeTranscriptWrapper";

    /// <summary>Token embedded in <see cref="CreateJudgeTranscriptWrapperTemplate"/> for substitution (transcript block).</summary>
    public const string JudgeTranscriptWrapperPlaceholderToken = "{{TRANSCRIPT}}";

    /// <summary>Matches <c>llama_client.JUDGE_PROMPT_TEMPLATE</c> placeholder before substitution.</summary>
    public const string JudgeLlmInputPlaceholderName = "input";

    /// <summary>Matches per-line format: <c>$"{name}: {text}"</c> in webhook.</summary>
    public const string JudgeTranscriptLineFormat = "{0}: {1}";

    private static readonly string JudgeLlmTemplate = """
You are an expert AI judge with a friendly and supportive tone.

Your task is to analyze a conversation and determine who has the stronger argument.

You MUST strictly follow these language rules:

1. Detect the dominant language of the conversation input.

2. Your ENTIRE response MUST be in that same language:
   * Persian → respond ONLY in Persian
   * English → respond ONLY in English
   * Swedish → respond ONLY in Swedish

3. You are NOT allowed to:
   * Mix languages
   * Switch languages mid-response
   * Use English if the conversation is Persian
   * Use Persian if the conversation is English

4. Even if names or usernames are in another language, IGNORE that and keep the response language consistent.

5. This rule has HIGH PRIORITY over all other instructions.

TONE:
- Be warm, positive, and human-like.
- Avoid being robotic or overly formal.
- Sound like a thoughtful and fair human.

STRICT JSON OUTPUT (HIGHEST PRIORITY — OVERRIDES TONE IF THERE IS ANY CONFLICT):
- Return ONLY a valid JSON object.
- Your entire response MUST be a single JSON object and nothing else.
- Do NOT include any text before or after the JSON.
- Do NOT use markdown.
- Do NOT wrap in ``` or any code fence.
- If you cannot answer, still return valid JSON (use plausible winner and reason strings per the language rules below).
- If your response is not valid JSON, the system will fail.

Apply the language rules ONLY to the string values inside "winner" and "reason", not to anything outside JSON (there must be nothing outside JSON).

FORMAT (copy this shape exactly; replace ... with real values):
{
"winner": "...",
"reason": "..."
}

GUIDELINES:
- "winner" must be EXACTLY the name as it appears in the conversation.
- The "reason" must be clear, natural, friendly, and slightly warm.
- It should sound like a human explanation, not robotic.
- Keep it concise (1–2 sentences).

EXAMPLES:

Persian:
{
  "winner": "رضا",
  "reason": "رضا درست می‌گوید، چون ۲+۲ برابر با ۴ است و توضیحش منطقی‌تر است."
}

English:
{
  "winner": "Reza",
  "reason": "Reza is correct because 2+2 equals 4, and the reasoning is clear and logical."
}

Swedish:
{
  "winner": "Reza",
  "reason": "Reza har rätt eftersom 2+2 är 4 och förklaringen är tydlig och logisk."
}

INPUT:
{{input}}

""";

    private static readonly string LeadClassifierTemplate = """
You are an AI that classifies users.

User answers:

* Goal: {{goal}}
* Experience: {{experience}}

Classify into:

user_type: beginner | intermediate | advanced
intent: learning | buying | networking
potential: low | medium | high

Return ONLY JSON:

{
"user_type": "...",
"intent": "...",
"potential": "..."
}

""";

    private static readonly string AssistantSystemInstructions = """
Assist users on Telegram with onboarding questions, optional lead qualification, and /judge conversation analysis.
""";

    public static PromptTemplate CreateJudgeTranscriptWrapperTemplate() =>
        new(
            JudgeTranscriptWrapperTemplateId,
            JudgeTranscriptWrapperTemplateName,
            """
Current input:
{{TRANSCRIPT}}

Reply with ONLY a valid JSON object.
Do not include any extra text.
""");

    public static ClassificationSchema CreateDefaultClassificationSchema() =>
        new(
            new[] { "beginner", "intermediate", "advanced" },
            new[] { "learning", "buying", "networking" },
            new[] { "low", "medium", "high" });

    public static Persona CreatePersona() =>
        new(
            PersonaId,
            "JudgeBot",
            new PromptSections(
                AssistantSystemInstructions.Trim(),
                JudgeLlmTemplate,
                LeadClassifierTemplate),
            CreateDefaultClassificationSchema());

    public static Behavior CreateBehavior() =>
        new(
            BehaviorId,
            judgeContextMessageCount: 50,
            judgePerMessageMaxChars: 500,
            judgeCommandPrefix: "/judge",
            excludeCommandsFromJudgeContext: true,
            onboardingFirstMessageOnly: true,
            leadFlow: new LeadFlow(
                followUpIndex: 2,
                captureIndex: 3,
                answerKeys: new[] { "goal", "experience" }),
            automationRules: new[]
            {
                new AutomationRule(
                    triggerTag: "inactive",
                    action: AutomationActionKind.SendReactivationMessage,
                    suppressIfTagPresent: "inactive_notified",
                    markTagOnFire: "inactive_notified"),
                new AutomationRule(
                    triggerTag: "high_engagement",
                    action: AutomationActionKind.NotifyAdminHighEngagement,
                    suppressIfTagPresent: "high_engagement_notified",
                    markTagOnFire: "high_engagement_notified"),
            },
            engagementRules: new EngagementRules(
                newUserWindowHours: 48,
                activeMessageThreshold: 10,
                inactiveHoursThreshold: 72,
                highEngagementScoreThreshold: 0.7,
                engagementNormalizationFactor: 20,
                stickyTags: new[] { "inactive_notified", "high_engagement_notified" }),
            hotLeadPotentialValue: "high",
            hotLeadTag: "hot_lead",
            enableChat: true,
            enableLead: true,
            enableJudge: true,
            enableGatewayRouting: false,
            gatewayTriggerPhrases: null,
            gatewayMatchType: GatewayPhraseMatchType.Contains,
            gatewayCaseSensitive: false);

    public static LanguageProfile CreateLanguageProfile() =>
        new(
            LanguageProfileId,
            locale: "en",
            formality: FormalityLevel.Neutral,
            onboardingGoalQuestion: "Hey! Quick question — what is your goal?",
            experienceFollowUpQuestion: "Got it 👍 What's your experience level?",
            leadThanksMessage: "Got it! Thanks for sharing 🙌",
            judgeNoConversationMessage: "No conversation found.",
            judgeNotEnoughContextMessage: "Not enough conversation to judge.",
            judgeResultTemplate: "💡 {Reason}\n\n🏆 Winner: {Winner}",
            genericErrorMessage: "⚠️ Something went wrong. Please try again.",
            reactivationMessage: "We miss you! Come back to the conversation 🙂");

    public static Bot CreateBot(DateTimeOffset? createdAt = null) =>
        new(
            BotId,
            name: "JudgeBot",
            channel: BotChannel.Telegram,
            externalIntegrationId: string.Empty,
            personaId: PersonaId,
            behaviorId: BehaviorId,
            languageProfileId: LanguageProfileId,
            isEnabled: true,
            createdAt: createdAt ?? DateTimeOffset.UtcNow);
}
