using System.Text.Json;
using System.Text.Json.Serialization;
using AiEmployee.Domain.BotConfiguration;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace AiEmployee.Infrastructure.Persistence;

internal static class BotConfigurationJsonOptions
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}

internal static class ClassificationSchemaConverters
{
    private sealed class Dto
    {
        public List<string> UserTypes { get; set; } = [];
        public List<string> Intents { get; set; } = [];
        public List<string> Potentials { get; set; } = [];
    }

    public static string ToJson(ClassificationSchema v) =>
        JsonSerializer.Serialize(
            new Dto
            {
                UserTypes = v.UserTypes.ToList(),
                Intents = v.Intents.ToList(),
                Potentials = v.Potentials.ToList(),
            },
            BotConfigurationJsonOptions.Options);

    public static ClassificationSchema FromJson(string v)
    {
        var dto = JsonSerializer.Deserialize<Dto>(v, BotConfigurationJsonOptions.Options) ?? new Dto();
        return new ClassificationSchema(dto.UserTypes, dto.Intents, dto.Potentials);
    }

    public static ValueComparer<ClassificationSchema> CreateComparer() =>
        new(
            (a, b) => a != null && b != null
                && a.UserTypes.SequenceEqual(b.UserTypes, StringComparer.Ordinal)
                && a.Intents.SequenceEqual(b.Intents, StringComparer.Ordinal)
                && a.Potentials.SequenceEqual(b.Potentials, StringComparer.Ordinal),
            c => HashCode.Combine(
                c.UserTypes.Aggregate(0, (h, s) => HashCode.Combine(h, StringComparer.Ordinal.GetHashCode(s))),
                c.Intents.Aggregate(0, (h, s) => HashCode.Combine(h, StringComparer.Ordinal.GetHashCode(s))),
                c.Potentials.Aggregate(0, (h, s) => HashCode.Combine(h, StringComparer.Ordinal.GetHashCode(s)))),
            c => new ClassificationSchema(c.UserTypes.ToList(), c.Intents.ToList(), c.Potentials.ToList()));
}

internal static class AnswerKeysConverters
{
    public static string ToJson(IReadOnlyList<string> v) =>
        JsonSerializer.Serialize(v.ToList(), BotConfigurationJsonOptions.Options);

    public static IReadOnlyList<string> FromJson(string v) =>
        JsonSerializer.Deserialize<List<string>>(v, BotConfigurationJsonOptions.Options) ?? [];

    public static ValueComparer<IReadOnlyList<string>> CreateComparer() =>
        new(
            (a, b) => a != null && b != null && a.SequenceEqual(b, StringComparer.Ordinal),
            c => c.Aggregate(0, (h, x) => HashCode.Combine(h, StringComparer.Ordinal.GetHashCode(x))),
            c => c.ToList());
}

internal static class AutomationRulesConverters
{
    private sealed class Dto
    {
        public string TriggerTag { get; set; } = string.Empty;
        public string? SuppressIfTagPresent { get; set; }
        public AutomationActionKind Action { get; set; }
        public string? MarkTagOnFire { get; set; }
    }

    public static string ToJson(IReadOnlyList<AutomationRule> rules)
    {
        var list = rules.Select(r => new Dto
        {
            TriggerTag = r.TriggerTag,
            SuppressIfTagPresent = r.SuppressIfTagPresent,
            Action = r.Action,
            MarkTagOnFire = r.MarkTagOnFire,
        }).ToList();
        return JsonSerializer.Serialize(list, BotConfigurationJsonOptions.Options);
    }

    public static IReadOnlyList<AutomationRule> FromJson(string v)
    {
        var dtos = JsonSerializer.Deserialize<List<Dto>>(v, BotConfigurationJsonOptions.Options) ?? [];
        IReadOnlyList<AutomationRule> rules = dtos
            .Select(d => new AutomationRule(
                d.TriggerTag,
                d.Action,
                d.SuppressIfTagPresent,
                d.MarkTagOnFire))
            .ToList();
        return rules;
    }

    public static ValueComparer<IReadOnlyList<AutomationRule>> CreateComparer() =>
        new(
            (a, b) => a != null && b != null && a.Count == b.Count
                && a.Zip(b, (x, y) =>
                    x.TriggerTag == y.TriggerTag
                    && x.SuppressIfTagPresent == y.SuppressIfTagPresent
                    && x.Action == y.Action
                    && x.MarkTagOnFire == y.MarkTagOnFire).All(eq => eq),
            c => c.Aggregate(
                c.Count,
                (h, r) => HashCode.Combine(
                    h,
                    r.TriggerTag.GetHashCode(StringComparison.Ordinal),
                    r.Action.GetHashCode(),
                    r.SuppressIfTagPresent != null ? r.SuppressIfTagPresent.GetHashCode(StringComparison.Ordinal) : 0,
                    r.MarkTagOnFire != null ? r.MarkTagOnFire.GetHashCode(StringComparison.Ordinal) : 0)),
            c => c.Select(r => new AutomationRule(
                r.TriggerTag,
                r.Action,
                r.SuppressIfTagPresent,
                r.MarkTagOnFire)).ToList());
}
