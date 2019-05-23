﻿using MapsetChecks.objects;
using MapsetParser.objects;
using MapsetParser.objects.events;
using MapsetParser.objects.hitobjects;
using MapsetParser.statics;
using MapsetVerifier;
using MapsetVerifier.objects;
using MapsetVerifier.objects.metadata;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MapsetChecks.checks.events
{
    public class CheckBreaks : BeatmapCheck
    {
        public override CheckMetadata GetMetadata() => new BeatmapCheckMetadata()
        {
            Category = "Events",
            Message = "Breaks only achievable through .osu editing.",
            Author = "Naxess"
        };
        
        public override Dictionary<string, IssueTemplate> GetTemplates()
        {
            return new Dictionary<string, IssueTemplate>()
            {
                { "Too early or late",
                    new IssueTemplate(Issue.Level.Unrankable,
                        "{0} to {1} {2}.",
                        "timestamp - ", "timestamp - ", "details")
                    .WithCause(
                        "Either the break starts less than 200 ms after the object before the end of the break, or the break ends less " +
                        "than the preemt time before the object after the start of the break.") },

                { "Too short",
                    new IssueTemplate(Issue.Level.Warning,
                        "{0} to {1} is non-functional due to being less than 650 ms.",
                        "timestamp - ", "timestamp - ")
                    .WithCause(
                        "The break is less than 650 ms in length.") }
            };
        }

        public override IEnumerable<Issue> GetIssues(Beatmap aBeatmap)
        {
            foreach (Break @break in aBeatmap.breaks)
            {
                // sometimes breaks are 1 ms off for some reason
                int leniency = 1;
                
                double minStart = 200;
                double minEnd = aBeatmap.difficultySettings.GetFadeInTime();

                double diffStart = 0;
                double diffEnd = 0;

                // checking from start of break forwards and end of break backwards ensures nothing is in between
                if (@break.time - aBeatmap.GetHitObject(@break.time)?.time < minStart)
                    diffStart = minStart - (@break.time - aBeatmap.GetHitObject(@break.time).time);

                if (aBeatmap.GetNextHitObject(@break.time)?.time - @break.endTime < minEnd)
                    diffEnd = minEnd - (aBeatmap.GetNextHitObject(@break.time).time - @break.endTime);

                if (diffStart > leniency || diffEnd > leniency)
                {
                    string issueMessage = "";

                    if (diffStart > leniency && diffEnd > leniency)
                        issueMessage = "starts " + diffStart + " ms too early and ends " + diffEnd + " ms too late";
                    else if (diffStart > leniency)
                        issueMessage = "starts " + diffStart + " ms too early";
                    else if (diffEnd > leniency)
                        issueMessage = "ends " + diffEnd + " ms too late";
                    
                    yield return new Issue(GetTemplate("Too early or late"), aBeatmap,
                        Timestamp.Get(@break.time), Timestamp.Get(@break.endTime),
                        issueMessage);
                }

                // although this currently affects nothing, it may affect things in the future
                if (@break.endTime - @break.time < 650)
                    yield return new Issue(GetTemplate("Too short"), aBeatmap,
                        Timestamp.Get(@break.time), Timestamp.Get(@break.endTime));
            }
        }
    }
}
