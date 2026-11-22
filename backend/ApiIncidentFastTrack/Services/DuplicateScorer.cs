using System.Globalization;
using System.Text;
using ApiIncidentFastTrack.Enums;

namespace ApiIncidentFastTrack.Services;

// The fields the duplicate check compares, loaded once per candidate incident.
public sealed record DuplicateCandidate(
    int Id,
    string? Title,
    string? Description,
    IncidentStatus Status,
    DateTime CreatedAt,
    IncidentType? IncidentType,
    int? StoreId,
    int? LocationId,
    DateTime? IncidentDate,
    string CustomerName,
    string? CustomerAddress,
    int CreatedByUserId);

// Pure scoring logic (no database), decided 2026-11-22 (CLAUDE.md, "Duplicates").
// Each matching criterion adds points; Threshold or more = possible duplicate.
// A criterion only matches when both incidents have a value: two empty fields are not "the same".
public static class DuplicateScorer
{
    public const int Threshold = 9;

    private const int CustomerNamePoints = 3;
    private const int IncidentDatePoints = 2;
    private const int LocationPoints = 2;
    private const int IncidentTypePoints = 2;
    private const int CustomerAddressPoints = 2;
    private const int TextPoints = 2;
    private const int StorePoints = 1;
    private const int ReporterPoints = 1;

    private const double SimilarTextThreshold = 0.85;   // name, address
    private const double SharedWordsThreshold = 0.5;    // title + description

    public static int Score(DuplicateCandidate a, DuplicateCandidate b)
    {
        var score = 0;

        if (Similarity(a.CustomerName, b.CustomerName) >= SimilarTextThreshold) score += CustomerNamePoints;
        if (a.IncidentDate is DateTime da && b.IncidentDate is DateTime db && da.Date == db.Date) score += IncidentDatePoints;
        if (a.LocationId is not null && a.LocationId == b.LocationId) score += LocationPoints;
        if (a.IncidentType is not null && a.IncidentType == b.IncidentType) score += IncidentTypePoints;
        if (Similarity(a.CustomerAddress, b.CustomerAddress) >= SimilarTextThreshold) score += CustomerAddressPoints;
        if (SharedWords($"{a.Title} {a.Description}", $"{b.Title} {b.Description}") >= SharedWordsThreshold) score += TextPoints;
        if (a.StoreId is not null && a.StoreId == b.StoreId) score += StorePoints;
        if (a.CreatedByUserId == b.CreatedByUserId) score += ReporterPoints;

        return score;
    }

    // 0..1, where 1 is identical after normalizing. Based on edit distance: how many letters must
    // be added, removed or replaced to turn one text into the other, relative to the longer one.
    // "joao silva" vs "joao silvo": 1 edit out of 10 letters -> 0.9.
    public static double Similarity(string? left, string? right)
    {
        var a = Normalize(left);
        var b = Normalize(right);

        if (a.Length == 0 || b.Length == 0)
        {
            return 0;
        }

        var distance = EditDistance(a, b);
        return 1.0 - (double)distance / Math.Max(a.Length, b.Length);
    }

    // 0..1: of all distinct words used in either text, the share that appear in both.
    // Words of 1-2 letters ("a", "de", "o") are ignored: they say nothing about the event.
    public static double SharedWords(string? left, string? right)
    {
        var a = Words(left);
        var b = Words(right);

        if (a.Count == 0 || b.Count == 0)
        {
            return 0;
        }

        var shared = a.Count(b.Contains);
        var all = a.Union(b).Count();
        return (double)shared / all;
    }

    // "  João  SILVA, Rua Nº5 " -> "joao silva rua n 5": no accents, lower case,
    // punctuation becomes a space, single spaces between words.
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        // FormD splits "ã" into "a" + a combining tilde, which is then dropped.
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
        }

        return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static HashSet<string> Words(string? text)
        => Normalize(text)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2)
            .ToHashSet();

    // Levenshtein distance, keeping only two rows of the table in memory.
    private static int EditDistance(string a, string b)
    {
        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];

        for (var j = 0; j <= b.Length; j++)
        {
            previous[j] = j;
        }

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;

            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(
                    Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + cost);
            }

            (previous, current) = (current, previous);
        }

        return previous[b.Length];
    }
}
