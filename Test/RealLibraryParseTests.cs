using Xunit;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using KinopoiskWhite.Extensions;

namespace Test;

// Data-driven smoke test over REAL filenames from a live Jellyfin library
// (library-filenames.txt, copied next to the test assembly). KPW_CORPUS can point
// it at a different list. If neither is present the test is a no-op.
public class RealLibraryParseTests
{
    static readonly string Corpus =
        Environment.GetEnvironmentVariable("KPW_CORPUS")
        ?? Path.Combine(AppContext.BaseDirectory, "library-filenames.txt");

    static readonly Regex JunkLeft = new(
        @"(?i:\b(rutracker|torrents|hdclub|yify|yts|selezen|ivanes|dalemake|web-?dl|web-?dlrip|bdrip|brrip|dvdrip|hdtvrip|hdrip|bluray|1080p|720p|480p|2160p|x264|x265|hevc|avc|hddvdrip)\b)");

    static IEnumerable<string> Names()
        => string.IsNullOrEmpty(Corpus) || !File.Exists(Corpus)
            ? Enumerable.Empty<string>()
            : File.ReadLines(Corpus).Where(l => !string.IsNullOrWhiteSpace(l));

    [Fact]
    public void EveryRealFilenameYieldsATitleWithoutThrowing()
    {
        var names = Names().ToList();
        if (names.Count == 0) return; // no corpus provided

        int ok = 0, noTitle = 0, threw = 0, withYear = 0;
        var crashed = new List<string>();
        var empty = new List<string>();
        var junkyTop = new List<string>();
        var noClean = new List<string>();

        foreach (var name in names)
        {
            (string, int?)[] parsed;
            try { parsed = name.ParseFileName(); }
            catch (Exception e) { threw++; crashed.Add($"{e.GetType().Name}: {name}"); continue; }

            if (parsed.Length == 0 || string.IsNullOrWhiteSpace(parsed[0].Item1))
            { noTitle++; empty.Add(name); continue; }

            ok++;
            var (title, year) = parsed[0];
            if (year != null) withYear++;
            if (JunkLeft.IsMatch(title)) junkyTop.Add($"{name}  ->  \"{title}\" ({year})");

            // The metric that matters for matching: is there AT LEAST ONE candidate
            // free of release/quality junk that the search can fall through to?
            if (parsed.All(p => JunkLeft.IsMatch(p.Item1)))
                noClean.Add($"{name}  ->  " + string.Join(" | ", parsed.Select(p => $"\"{p.Item1}\"")));
        }

        var sb = new StringBuilder();
        sb.AppendLine($"corpus              : {Corpus}");
        sb.AppendLine($"total filenames     : {names.Count}");
        sb.AppendLine($"parsed to a title   : {ok}");
        sb.AppendLine($"  ...with a year    : {withYear}");
        sb.AppendLine($"no title extracted  : {noTitle}");
        sb.AppendLine($"threw an exception  : {threw}");
        sb.AppendLine($"junk in TOP title   : {junkyTop.Count}");
        sb.AppendLine($"NO clean candidate  : {noClean.Count}   <-- the metric #3b targets");
        sb.AppendLine();
        if (crashed.Count > 0) { sb.AppendLine("== CRASHED =="); crashed.ForEach(x => sb.AppendLine(x)); sb.AppendLine(); }
        if (empty.Count > 0)   { sb.AppendLine("== NO TITLE =="); empty.ForEach(x => sb.AppendLine(x)); sb.AppendLine(); }
        if (noClean.Count > 0) { sb.AppendLine("== NO CLEAN CANDIDATE (search can't reach a junk-free title) =="); noClean.ForEach(x => sb.AppendLine(x)); sb.AppendLine(); }
        if (junkyTop.Count > 0){ sb.AppendLine("== JUNK LEFT IN TOP TITLE (first 40) =="); junkyTop.Take(40).ToList().ForEach(x => sb.AppendLine(x)); }

        File.WriteAllText(Corpus + ".report.txt", sb.ToString());

        // The hard guarantees: no crashes, and every real file yields a searchable title.
        Assert.True(threw == 0, $"{threw} filenames threw:\n" + string.Join("\n", crashed.Take(20)));
        Assert.True(noTitle == 0, $"{noTitle} filenames produced no title:\n" + string.Join("\n", empty.Take(20)));
    }
}
