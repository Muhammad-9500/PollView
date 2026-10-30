namespace PollViews.Services;

public sealed record SentimentResult(string Label, double Confidence);

public sealed class SentimentAnalysisService
{
    private static readonly HashSet<string> PositiveWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "good", "better", "best", "trust", "trusted", "honest", "competent", "competence", "capacity",
        "development", "progress", "peace", "working", "works", "success", "support", "credible",
        "effective", "excellent", "improved", "improvement", "hope", "reliable", "close"
    };

    private static readonly HashSet<string> NegativeWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "bad", "poor", "worse", "worst", "corrupt", "corruption", "failure", "failed", "broken",
        "lie", "lies", "lying", "dishonest", "incompetent", "insecurity", "poverty", "hunger",
        "untrustworthy", "disappointed", "disappointing", "problem", "problems", "wrong", "lack"
    };

    public SentimentResult Analyze(IEnumerable<string> messages)
    {
        var words = messages
            .Where(message => !string.IsNullOrWhiteSpace(message))
            .SelectMany(message => message.Split([' ', ',', '.', ';', ':', '!', '?', '\'', '"', '(', ')', '/', '\\'], StringSplitOptions.RemoveEmptyEntries));

        var tokens = words.ToList();
        var positive = tokens.Count(PositiveWords.Contains);
        var negative = tokens.Count(NegativeWords.Contains);
        var score = positive - negative;
        var totalSignals = positive + negative;
        var label = score > 0 ? "Positive" : score < 0 ? "Negative" : "Neutral";
        var confidence = totalSignals == 0 ? 0.35 : Math.Min(0.98, 0.5 + (Math.Abs(score) / (double)Math.Max(1, totalSignals)) * 0.48);

        return new SentimentResult(label, Math.Round(confidence, 2));
    }
}
