﻿using MapsetParser.objects;
using MapsetParser.settings;
using MapsetVerifierFramework;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace MapsetChecks.checks.general.metadata
{
    [Check]
    public class CheckMarkerSpacing : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Metadata",
            Message = "Incorrect marker spacing.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Standardizing the way metadata is written for ranked content.
                    <image>
                        https://i.imgur.com/9w1fzvB.png
                        The romanized artist field is missing a whitespace before and after ""(CV:"". 
                        The unicode artist field is fine, though, since the characters are full-width.
                    </image>"
                },
                {
                    "Reasoning",
                    @"
                    Small deviations in metadata or obvious mistakes in its formatting or capitalization are for the 
                    most part eliminated through standardization. Standardization also reduces confusion in case of 
                    multiple correct ways to write certain fields and contributes to making metadata more consistent 
                    across official content."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Problem",
                    new IssueTemplate(Issue.Level.Problem,
                        "Missing {0} in {1}; \"{2}\".",
                        "something", "field", "field")
                    .WithCause(
                        "Some whitespace or parameter is missing from the artist or title fields where only alphabetic " +
                        "and numerical characters are involved.") },

                { "Warning",
                    new IssueTemplate(Issue.Level.Warning,
                        "Missing {0} in {1}; \"{2}\".",
                        "something", "field", "field")
                    .WithCause(
                        "Same as the other check, but can involve any type of characters.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet beatmapSet)
        {
            Beatmap beatmap = beatmapSet.beatmaps[0];
            
            List<Func<string, string>> problemTests = new List<Func<string, string>>()
            {
                field => FormalSpaceRegex(field, @"CV:"),
                field => FormalSpaceRegex(field, @"vs\."),
                field => FormalSpaceRegex(field, @"feat\."),

                field => new Regex(@"[a-zA-Z0-9]\(CV:").IsMatch(field) ? "whitespace before \"(CV:\" or full-width bracket \"（\"" : null,
                    // also check before any parenthesis CV: might have before it
                
                field => new Regex(@"(?<!(\(|（))CV:").IsMatch(field) ? "\"(\" before \"CV:\"" : null,
                    // implied from the "Character (CV: Voice Actor)" format requirement
                
                field => new Regex(@",[a-zA-Z0-9]").IsMatch(field) ? "whitespace after \",\"" : null,
                    // comma only checks trailing whitespaces
            };
            List<Func<string, string>> warningTests = new List<Func<string, string>>()
            {
                // Some artists include ampersand as part of their name.
                field => FormalSpaceRegex(field, "&"),

                // Markers only need spaces around them if both of the following are true
                // - They are not full width (i.e. "、" for comma and "：" for colon)
                // - The character after/before is not full width (i.e. most chinese/japanese characters)
                // Source: Lanturn (metadata helper at the time of writing this)

                // The regex "[一-龠]+|[ぁ-ゔ]+|[ァ-ヴー]" matches all japanese characters.
                
                field => new Regex(@"(?<! |\(|（)feat\.")                     .IsMatch(field) ? "whitespace before \"feat.\""    : null,
                field => new Regex(@"(?<! )(\(|（)feat\.")                    .IsMatch(field) ? "whitespace before \"(feat.\""   : null,
                field => new Regex(@"(?<! |[一-龠]+|[ぁ-ゔ]+|[ァ-ヴー])vs\.")  .IsMatch(field) ? "whitespace before \"vs.\""      : null,
                field => new Regex(@"(?<! |[一-龠]+|[ぁ-ゔ]+|[ァ-ヴー])&")     .IsMatch(field) ? "whitespace before \"&\""        : null,

                field => new Regex(@"CV(?!:[ 一-龠]+|[ぁ-ゔ]+|[ァ-ヴー]|：)")  .IsMatch(field) ? "whitespace after \"CV:\" or full-width colon \"：\"" : null,
                field => new Regex(@",(?![ 一-龠]+|[ぁ-ゔ]+|[ァ-ヴー])")       .IsMatch(field) ? "whitespace after \",\" or full-width comma \"、\""   : null,

                field => new Regex(@"feat\.(?! |[一-龠]+|[ぁ-ゔ]+|[ァ-ヴー])") .IsMatch(field) ? "whitespace after \"feat.\"" : null,
                field => new Regex(@"vs\.(?! |[一-龠]+|[ぁ-ゔ]+|[ァ-ヴー])")   .IsMatch(field) ? "whitespace after \"vs.\""   : null,
                field => new Regex(@"&(?! |[一-龠]+|[ぁ-ゔ]+|[ァ-ヴー])")      .IsMatch(field) ? "whitespace after \"&\""     : null,
            };

            MetadataSettings metadata = beatmap.metadataSettings;
            List<Tuple<string, string>> fields = new List<Tuple<string, string>>()
                { new Tuple<string, string>(metadata.artist,         "artist"),
                  new Tuple<string, string>(metadata.artistUnicode,  "artist unicode") };

            List<Tuple<Tuple<string, string>, string, bool>> issueMessages = new List<Tuple<Tuple<string, string>, string, bool>>();
            foreach (Tuple<string, string> field in fields)
            {
                // old osu versions didn't have unicode fields
                if (field.Item1 != null)
                {
                    foreach (Func<string, string> problemTest in problemTests)
                    {
                        string message = problemTest(field.Item1);
                        if (message != null && !issueMessages.Any(tuple => tuple.Item2 == message && tuple.Item1 == field))
                            issueMessages.Add(new Tuple<Tuple<string, string>, string, bool>(field, message, true));
                    }

                    foreach (Func<string, string> warningTest in warningTests)
                    {
                        string message = warningTest(field.Item1);
                        if (message != null && !issueMessages.Any(tuple => tuple.Item2 == message && tuple.Item1 == field))
                            issueMessages.Add(new Tuple<Tuple<string, string>, string, bool>(field, message, false));
                    }
                }
            }

            foreach (Tuple<Tuple<string, string>, string, bool> messageTuple in issueMessages)
                yield return new Issue(GetTemplate(messageTuple.Item3 ? "Problem" : "Warning"), null,
                    messageTuple.Item2, messageTuple.Item1.Item2,
                    messageTuple.Item1.Item1);
        }

        /// <summary> Returns a message describing where a space is missing given a field and what is tested against. </summary>
        private string FormalSpaceRegex(string field, string test)
        {
            if (new Regex(test + "[a-zA-Z0-9]").IsMatch(field))
                return "whitespace after \"" + test.Replace("\\", "") + "\"";

            if (new Regex("[a-zA-Z0-9]" + test).IsMatch(field))
                return "whitespace before \"" + test.Replace("\\", "") + "\"";

            return null;
        }

        /// <summary> Applies a predicate to all artist and title metadata fields. Yields an issue wherever the predicate is true. </summary>
        private IEnumerable<Issue> GetFormattingIssues(MetadataSettings settings, Func<string, bool> Func)
        {
            if (Func(settings.artist))
                yield return new Issue(GetTemplate("Wrong Format"), null,
                    "Romanized", "artist", settings.artist);

            // Unicode fields do not exist in file version 9.
            if (settings.artistUnicode != null && Func(settings.artistUnicode))
                yield return new Issue(GetTemplate("Wrong Format"), null,
                    "Unicode", "artist", settings.artistUnicode);

            if (Func(settings.title))
                yield return new Issue(GetTemplate("Wrong Format"), null,
                    "Romanized", "title", settings.title);

            if (settings.titleUnicode != null && Func(settings.titleUnicode))
                yield return new Issue(GetTemplate("Wrong Format"), null,
                    "Unicode", "title", settings.titleUnicode);
        }
    }
}
