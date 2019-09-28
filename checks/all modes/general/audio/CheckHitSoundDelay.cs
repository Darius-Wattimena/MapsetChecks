﻿using MapsetParser.objects;
using MapsetVerifierFramework;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;
using MapsetVerifierFramework.objects.resources;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MapsetChecks.checks.general.audio
{
    [Check]
    public class CheckHitSoundDelay : GeneralCheck
    {
        public override CheckMetadata GetMetadata() => new CheckMetadata()
        {
            Category = "Audio",
            Message = "Delayed hit sounds.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Ensuring hit sounds provide proper feedback for how early or late the player clicked.
                    <image>
                        https://i.imgur.com/LRpgqcJ.png
                        A hit sound which is delayed by ~10 ms, as shown in Audacity. Note that audacity shows its 
                        timeline in seconds, so 0.005 means 5 ms.
                    </image>"
                },
                {
                    "Reasoning",
                    @"
                    By having delayed hit sounds, the feedback the player receives would be misleading them into 
                    thinking they clicked later than they actually did, which contradicts the purpose of having hit 
                    sounds in the first place."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Pure Delay",
                    new IssueTemplate(Issue.Level.Problem,
                        "\"{0}\" has a {1} ms period of complete silence at the start.",
                        "path", "pure delay")
                    .WithCause(
                        "A hit sound file has a definite delay (complete silence) of at least 5 ms.") },

                { "Delay",
                    new IssueTemplate(Issue.Level.Warning,
                        "\"{0}\" has a delay of ~{2} ms, of which {1} ms is complete silence.",
                        "path", "pure delay", "delay")
                    .WithCause(
                        "A hit sound file has very low volume for ~5 ms or more.") },

                { "Minor Delay",
                    new IssueTemplate(Issue.Level.Minor,
                        "\"{0}\" has a delay of ~{2} ms, of which {1} ms is complete silence.",
                        "path", "pure delay", "delay")
                    .WithCause(
                        "Same as the regular delay, except anything between 1 to 5 ms.") },

                { "Unable to check",
                    new IssueTemplate(Issue.Level.Error,
                        "\"{0}\" {1}, so unable to check that.",
                        "path", "error")
                    .WithCause(
                        "There was an error parsing a hit sound file.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(BeatmapSet aBeatmapSet)
        {
            foreach (string hsFile in aBeatmapSet.hitSoundFiles)
            {
                string hsPath = Path.Combine(aBeatmapSet.songPath, hsFile);

                List<float[]> peaks = null;
                Exception exception = null;
                try
                {
                    peaks = Audio.GetPeaks(hsPath);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                if (exception == null)
                {
                    // Ignore muted files since they don't have anything to be delayed.
                    if (peaks?.Count > 0 && peaks.Sum(aPeak => aPeak.Sum()) > 0)
                    {
                        double maxStrength = peaks.Select(aValue => Math.Abs(aValue.Sum())).Max();

                        int delay = 0;
                        int pureDelay = 0;
                        double strength = 0;
                        while (delay + pureDelay < peaks.Count)
                        {
                            strength += Math.Abs(peaks[delay].Sum());

                            if (strength >= maxStrength / 2)
                                break;

                            strength *= 0.95;

                            // The delay added by MP3 encoding still has very slight volume where it's basically silent.
                            if (strength < 0.001)
                            {
                                strength = 0;
                                ++pureDelay;
                                ++delay;
                            }
                            else
                                ++delay;
                        }

                        if (pureDelay >= 5)
                            yield return new Issue(GetTemplate("Pure Delay"), null,
                                hsFile, $"{pureDelay:0.##}");

                        else if (delay + pureDelay >= 5)
                            yield return new Issue(GetTemplate("Delay"), null,
                                hsFile, $"{pureDelay:0.##}", $"{delay:0.##}");

                        else if (delay + pureDelay >= 1)
                            yield return new Issue(GetTemplate("Minor Delay"), null,
                                hsFile, $"{pureDelay:0.##}", $"{delay:0.##}");
                    }
                }
                else
                    yield return new Issue(GetTemplate("Unable to check"), null,
                        hsFile, exception);
            }
        }
    }
}
