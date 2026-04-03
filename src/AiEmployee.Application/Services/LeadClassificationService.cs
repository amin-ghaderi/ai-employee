using AiEmployee.Application.Interfaces;

namespace AiEmployee.Application.Services;

public sealed class LeadClassificationService
{
    private readonly IAiClient _aiClient;

    public LeadClassificationService(IAiClient aiClient)
    {
        _aiClient = aiClient;
    }

    public async Task<(string userType, string intent, string potential)> ClassifyAsync(Dictionary<string, string> answers)
    {
        var prompt = BuildPrompt(answers);

        var result = await _aiClient.ClassifyLeadAsync(prompt);

        return (result.UserType, result.Intent, result.Potential);
    }

    private string BuildPrompt(Dictionary<string, string> answers)
    {
        var goal = answers.ContainsKey("goal") ? answers["goal"] : "";
        var experience = answers.ContainsKey("experience") ? answers["experience"] : "";

        return $@"
You are an AI that classifies users.

User answers:

* Goal: {goal}
* Experience: {experience}

Classify into:

user_type: beginner | intermediate | advanced
intent: learning | buying | networking
potential: low | medium | high

Return ONLY JSON:

{{
""user_type"": ""..."",
""intent"": ""..."",
""potential"": ""...""
}}
";
    }
}
