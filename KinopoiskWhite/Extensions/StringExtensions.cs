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

    // Square/curly tag groups like [torrents.ru], [YTS.AM], {group}. Parentheses are
    // left alone because they usually hold the year, e.g. "Title (1975)".
    [GeneratedRegex(@"\[[^\]]*\]|\{[^}]*\}")]
    private static partial Regex BracketTags();

    // Release / quality / source / codec tokens. Applied to the raw title (dots and
    // dashes intact) so a clean variant exists even when there is no year to act as a
    // boundary, e.g. "Druk.BDRemux.1080p" -> "Druk". Conservative on purpose — only
    // well-known tokens, so real title words are left alone.
    [GeneratedRegex(@"(?i:\b(?:2160p|1440p|1080p|720p|576p|540p|480p|2160|1080|720|480|4k|web[.\-]?dlrip|web[.\-]?dl|webrip|bdremux|bdrip|brrip|dvdrip|hdtvrip|hdtv|hdrip|tvrip|satrip|dvdscr|camrip|bluray|blu[.\-]?ray|remux|hddvdrip|hddvd|x264|x265|h[.\-]?264|h[.\-]?265|hevc|avc|xvid|divx|aac|ac3|dts|flac|10bit|imax|hdr10|hdr|dolby|atmos|open[.\s_\-]?matte|atvp|amzn|dsnp|hulu|ddp?5[.\s_]?1|ddp?7[.\s_]?1|ddp?2[.\s_]?0|ddp5|dd5|ddp|eac3|truehd|extended|unrated|proper|repack|remastered|rus|eng|ukr|multi|dub|dubbed|barm|hdclub|yify|yts|eniahd|hidt|selezen|dalemake|rutracker)\b)")]
    private static partial Regex ReleaseTokens();

    // Heuristic trailing release-group token: an ALL-CAPS run (ATVP, YIFY), something
    // containing a digit (DDP5), or a CamelCase group ending in two caps (EniaHD, HiDt).
    // Bare numbers are NOT stripped — they are often part of the title ("Часть 1",
    // "Vol 2"). Case-sensitive on purpose so normal Title-case words survive.
    [GeneratedRegex(@"\s+(?:[A-Z]{2,}[A-Za-z0-9]*|[A-Za-z][A-Za-z0-9]*[0-9][A-Za-z0-9]*|[A-Za-z]+[A-Z]{2}[A-Za-z0-9]*)$")]
    private static partial Regex TrailingGroup();

    // Aggressively declutter a raw title: normalise underscores, strip known
    // release/quality/source tokens, then iteratively drop trailing release-group
    // tokens (ATVP, DDP5, EniaHD, barm, ...) that otherwise poison KinoPoisk search.
    private static string Declutter(string rawTitle)
    {
        var s = ReleaseTokens().Replace(rawTitle.Replace('_', ' '), " ");
        s = AllButWords().Replace(s, " ").Trim();

        string prev;
        do { prev = s; s = TrailingGroup().Replace(s, "").Trim(); }
        while (s != prev && s.Length > 0);

        return s;
    }

    public static (string, int?)[] ParseFileName(this string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return [];

        var fileName = System.IO.Path.GetFileName(path);
        fileName = BracketTags().Replace(fileName, " ");
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
            var rawTitle = item.Item1;

            title = AllButWords().Replace(title, " ").Trim();
            if (title == string.Empty) return result;

            Add(result, title, year);
            Add(result, title, null);

            // A junk-free variant: strip release/quality tokens and trailing release
            // groups, so a clean title exists even without a year to cut the boundary.
            var cleaned = Declutter(rawTitle);
            if (cleaned != string.Empty && cleaned != title)
            {
                Add(result, cleaned, year);
                Add(result, cleaned, null);
            }

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