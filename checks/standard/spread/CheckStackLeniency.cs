﻿using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using MapsetParser.objects.timinglines;
using MapsetParser.statics;
using MapsetVerifierFramework;
using MapsetVerifierFramework.objects;
using MapsetVerifierFramework.objects.attributes;
using MapsetVerifierFramework.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace MapsetChecks.checks.standard.spread
{
    [Check]
    public class CheckStackLeniency : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Modes = new Beatmap.Mode[]
            {
                Beatmap.Mode.Standard
            },
            Difficulties = new Beatmap.Difficulty[]
            {
                Beatmap.Difficulty.Easy,
                Beatmap.Difficulty.Normal,
                Beatmap.Difficulty.Hard,
                Beatmap.Difficulty.Insane,
            },
            Category = "Spread",
            Message = "Perfect stacks too close in time.",
            Author = "Naxess",

            Documentation = new Dictionary<string, string>()
            {
                {
                    "Purpose",
                    @"
                    Preventing objects from perfectly, or almost perfectly, overlapping when close in time for easy to insane difficulties."
                },
                {
                    "Reasoning",
                    @"
                    Objects stacked perfectly on top of each other close in time is read almost ambigiously to a single object, even for moderately 
                    experienced players. The lower in difficulty you get, the more beneficial it is to simply use a regular stack or overlap instead
                    as trivializing readability gets more important."
                }
            }
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Problem",
                    new IssueTemplate(Issue.Level.Problem,
                        "{0} Stack leniency should be at least {1}.",
                        "timestamp - ", "stack leniency")
                    .WithCause(
                        "Two objects are overlapping perfectly and are less than 1/1, 1/1, 1/2, or 1/4 apart (assuming 160 BPM), for E/N/H/I respectively.") },

                { "Problem Failed Stack",
                    new IssueTemplate(Issue.Level.Problem,
                        "{0} Failed stack, objects are practically perfectly stacked.",
                        "timestamp - ")
                    .WithCause(
                        "Same as the other check, except applies to non-stacked objects within 4 px of one another.") },

                { "Warning",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} Stack leniency should be at least {1}.",
                        "timestamp - ", "stack leniency")
                    .WithCause(
                        "Same as the other check, except only appears for insane difficulties, as this becomes a guideline.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap aBeatmap)
        {
            double[] snapping = new double[] { 1, 1, 0.5, 0.25 };

            for (int diffIndex = 0; diffIndex < snapping.Length; ++diffIndex)
            {
                double timeGap = snapping[diffIndex] * 60000 / 160d;

                List<Stackable> iteratedObjects = new List<Stackable>();
                foreach (Stackable hitObject in aBeatmap.hitObjects.OfType<Stackable>())
                {
                    iteratedObjects.Add(hitObject);
                    foreach (Stackable otherHitObject in aBeatmap.hitObjects.OfType<Stackable>().Except(iteratedObjects))
                    {
                        if (otherHitObject.time - hitObject.time < timeGap)
                        {
                            if (hitObject.Position == otherHitObject.Position)
                            {
                                int requiredStackLeniency =
                                    (int)Math.Ceiling((otherHitObject.time - hitObject.time) /
                                        (aBeatmap.difficultySettings.GetFadeInTime() * 0.1));

                                string template = diffIndex == (int)Beatmap.Difficulty.Insane ? "Warning" : "Problem";

                                yield return new Issue(GetTemplate(template), aBeatmap,
                                    Timestamp.Get(hitObject, otherHitObject), requiredStackLeniency)
                                    .ForDifficulties((Beatmap.Difficulty)diffIndex);
                            }

                            // Objects not stacked within 4 px of one another are considered failed stacks.
                            else if ((hitObject.Position - otherHitObject.Position).LengthSquared() < 16)
                            {
                                yield return new Issue(GetTemplate("Problem Failed Stack"), aBeatmap,
                                    Timestamp.Get(hitObject, otherHitObject))
                                    .ForDifficulties((Beatmap.Difficulty)diffIndex);
                            }
                        }
                    }
                }
            }
        }
    }
}
