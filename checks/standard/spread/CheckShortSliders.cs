﻿using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using MapsetParser.objects.timinglines;
using MapsetParser.statics;
using MapsetVerifier;
using MapsetVerifier.objects;
using MapsetVerifier.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace MapsetChecks.checks.standard.spread
{
    public class CheckShortSliders : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Modes = new Beatmap.Mode[]
            {
                Beatmap.Mode.Standard
            },
            Difficulties = new Beatmap.Difficulty[]
            {
                Beatmap.Difficulty.Easy
            },
            Category = "Spread",
            Message = "Too short sliders.",
            Author = "Naxess"
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Too Short",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} {1} ms, expected at least {2}.",
                        "timestamp - ", "duration", "threshold")
                    .WithCause(
                        "A slider in an Easy difficulty is less than 125 ms (240 bpm 1/2).") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap aBeatmap)
        {
            // Shortest length before warning is 1/2 at 240 BPM, 125 ms.
            double timeThreshold = 125;

            foreach (Slider slider in aBeatmap.hitObjects.OfType<Slider>())
                if (slider.endTime - slider.time < timeThreshold)
                    yield return new Issue(GetTemplate("Too Short"), aBeatmap,
                        Timestamp.Get(slider),
                        (Math.Round((slider.endTime - slider.time) * 100) / 100).ToString(CultureInfo.InvariantCulture),
                        timeThreshold);
        }
    }
}
