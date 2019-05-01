﻿using MapsetChecks.objects;
using MapsetParser.objects;
using MapsetParser.objects.events;
using MapsetParser.objects.hitobjects;
using MapsetVerifier;
using MapsetVerifier.objects;
using MapsetVerifier.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MapsetChecks.checks.hit_sounds
{
    public class CheckMuted : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Category = "Hit Sounds",
            Message = "Low volume hit sounding.",
            Author = "Naxess"
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Warning Volume",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} {1}% volume {2}, this may be hard to hear over the song.",
                        "timestamp - ", "percent", "active hit object")
                    .WithCause(
                        "An active hit object is at 10% or lower volume.") },

                { "Minor Volume",
                    new IssueTemplate(Issue.Level.Minor,
                        "{0} {1}% volume {2}, this may be hard to hear over the song.",
                        "timestamp - ", "percent", "active hit object")
                    .WithCause(
                        "An active hit object is at 20% or lower volume.") },

                { "Passive",
                    new IssueTemplate(Issue.Level.Minor,
                        "{0} {1}% volume slider {2}, ensure there is no distinct sound here in the song.",
                        "timestamp - ", "percent", "tick/repeat/tail")
                    .WithCause(
                        "A passive hit object is at 10% or lower volume.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap aBeatmap)
        {
            foreach (HitObject hitObject in aBeatmap.hitObjects)
            {
                string type = hitObject is Circle ? "circle" : "slider head";
                
                if (hitObject is Circle || hitObject is Slider || hitObject is HoldNote)
                {
                    // Mania uses hitsounding differently so circles and hold notes are overridden by the object-specific volume option if it's > 0
                    // and that applies to standard and any other mode as well even though it's basically just used for mania.
                    float volume =
                        !(hitObject is Slider) && hitObject.volume > 0 && hitObject.volume != null ?
                            hitObject.volume.GetValueOrDefault() :
                            aBeatmap.GetTimingLine(hitObject.time, false, true).volume;

                    // Even if you manually put a volume less than 5%, it'll just act as if it were 5% in gameplay.
                    if (volume < 5)
                        volume = 5;
                    
                    if (volume <= 10)
                        yield return new Issue(GetTemplate("Warning Volume"), aBeatmap, Timestamp.Get(hitObject), volume, type);
                    else if (volume <= 20)
                        yield return new Issue(GetTemplate("Minor Volume"), aBeatmap, Timestamp.Get(hitObject), volume, type);

                    // Ideally passive objects like repeats and tails should be hit sounded wherever the song has distinct sounds
                    // to build consistency. This also applies to slider ticks if the sliderslide is slienced.
                    if (hitObject is Slider slider)
                    {
                        foreach (double tickTime in slider.sliderTickTimes)
                        {
                            type = "tick";
                            volume = aBeatmap.GetTimingLine(tickTime, false, true).volume;
                            if (volume <= 10)
                                yield return new Issue(GetTemplate("Passive"), aBeatmap, Timestamp.Get(tickTime), volume, type);
                        }
                        
                        type = "repeat";
                        for (int i = 0; i < slider.edgeAmount; ++i)
                        {
                            double time = Math.Floor(slider.GetCurveDuration() * i);

                            if (i == slider.edgeAmount - 1)
                            {
                                time = slider.endTime;
                                type = "tail";
                            }

                            volume = aBeatmap.GetTimingLine(time, false, true).volume;
                            if (volume <= 10)
                                yield return new Issue(GetTemplate("Passive"), aBeatmap, Timestamp.Get(time), volume, type);
                        }
                    }
                }
            }
        }
    }
}