using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace KinopoiskWhite.Extensions;

public static partial class StringExtensions
{

    [GeneratedRegex(@"[^\w]+|[_\s]")]
    private static partial Regex AllButWords();

    [GeneratedRegex(@"^\d+")]
    private static partial Regex LeadingDigits();

    [GeneratedRegex(@"\d+$")]
    private static partial Regex TrailingDigits();

    [GeneratedRegex(@"\.\w+$")]
    private static partial Regex FileExtension();

    [GeneratedRegex(@"((?:19|20)\d{2})")]
    private static partial Regex ByYear();

    [GeneratedRegex(@"(?i:EXTENDED|BDRIP|BRRIP|DVDRIP)")]
    private static partial Regex VideoFormats();

    public static (string, int?)[] ParseFileName(this string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return [];

        var fileName = System.IO.Path.GetFileName(path);
        var byYear = ByYear();
        var parts = byYear.Split(fileName);

        var set = new HashSet<(string, int?)>();

        for (int index = parts.Length - 1; index >= 0; index--)
        {
            if (!Regex.IsMatch(parts[index], $"^{byYear}$")) continue;

            var title = string.Join(" ", parts.Take(index));
            int year = int.Parse(parts[index]);
            set.Add((title, year));
        }

        var fullName = FileExtension().Replace(fileName, "");
        set.Add((fullName, null));

        void Add(List<(string, int?)> result, string title, int? year)
        {
            if (!result.Contains((title, year)))
                result.Add((title, year));
        }

        var ordered = set.OrderBy(x => x.Item1.Length).ThenBy(x => x.Item2 != null);
        var result = ordered.Aggregate(
            new List<(string, int?)>(),
            (result, item) =>
        {
            var (title, year) = item;

            title = AllButWords().Replace(title, " ").Trim();
            if (title == string.Empty) return result;

            Add(result, title, year);
            Add(result, title, null);

            title = VideoFormats().Replace(title, "").Trim();
            Add(result, title, year);

            title = LeadingDigits().Replace(title, "").Trim();
            Add(result, title, year);

            title = TrailingDigits().Replace(title, "").Trim();
            Add(result, title, year);

            return result;
        });

        return [.. result];
    }
}