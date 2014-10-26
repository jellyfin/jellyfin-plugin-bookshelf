using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArgusTV.DataContracts;

namespace MediaBrowser.Plugins.ArgusTV.Helpers
{
    class SchedulerHelpers
    {
        public void AppendTitleRule(List<ScheduleRule> rules, int titleRuleTypeIndex, string text)
        {
            text = text.Trim();
            if (!String.IsNullOrEmpty(text))
            {
                switch (titleRuleTypeIndex)
                {
                    case TitleRuleTypeIndex.Equals:
                        AppendORableRule(rules, ScheduleRuleType.TitleEquals, text);
                        break;

                    case TitleRuleTypeIndex.StartsWith:
                        AppendORableRule(rules, ScheduleRuleType.TitleStartsWith, text);
                        break;

                    case TitleRuleTypeIndex.Contains:
                        AppendContainsRule(rules, ScheduleRuleType.TitleContains, ScheduleRuleType.TitleDoesNotContain, text);
                        break;
                }
            }
        }

        public void AppendSubTitleRule(List<ScheduleRule> rules, int titleRuleTypeIndex, string text)
        {
            text = text.Trim();
            if (!String.IsNullOrEmpty(text))
            {
                switch (titleRuleTypeIndex)
                {
                    case TitleRuleTypeIndex.Equals:
                        AppendORableRule(rules, ScheduleRuleType.SubTitleEquals, text);
                        break;

                    case TitleRuleTypeIndex.StartsWith:
                        AppendORableRule(rules, ScheduleRuleType.SubTitleStartsWith, text);
                        break;

                    case TitleRuleTypeIndex.Contains:
                        AppendContainsRule(rules, ScheduleRuleType.SubTitleContains, ScheduleRuleType.SubTitleDoesNotContain, text);
                        break;
                }
            }
        }

        public void AppendEpisodeNumberRule(List<ScheduleRule> rules, int titleRuleTypeIndex, string text)
        {
            text = text.Trim();
            if (!String.IsNullOrEmpty(text))
            {
                switch (titleRuleTypeIndex)
                {
                    case TitleRuleTypeIndex.Equals:
                        AppendORableRule(rules, ScheduleRuleType.EpisodeNumberEquals, text);
                        break;

                    case TitleRuleTypeIndex.StartsWith:
                        AppendORableRule(rules, ScheduleRuleType.EpisodeNumberStartsWith, text);
                        break;

                    case TitleRuleTypeIndex.Contains:
                        AppendContainsRule(rules, ScheduleRuleType.EpisodeNumberContains, ScheduleRuleType.EpisodeNumberDoesNotContain, text);
                        break;
                }
            }
        }

        public void AppendDescriptionRule(List<ScheduleRule> rules, string text)
        {
            AppendContainsRule(rules, ScheduleRuleType.DescriptionContains, ScheduleRuleType.DescriptionDoesNotContain, text);
        }

        public void AppendProgramInfoRule(List<ScheduleRule> rules, string text)
        {
            AppendContainsRule(rules, ScheduleRuleType.ProgramInfoContains, ScheduleRuleType.ProgramInfoDoesNotContain, text);
        }

        public void AppendOnDateAndDaysOfWeekRule(List<ScheduleRule> rules, ScheduleDaysOfWeek daysOfWeek, DateTime? onDateTime)
        {
            if (daysOfWeek == ScheduleDaysOfWeek.None)
            {
                if (onDateTime.HasValue)
                {
                    rules.Add(ScheduleRuleType.OnDate, onDateTime.Value.Date);
                }
            }
            else
            {
                if (onDateTime.HasValue)
                {
                    rules.Add(ScheduleRuleType.DaysOfWeek, daysOfWeek, onDateTime.Value.Date);
                }
                else
                {
                    rules.Add(ScheduleRuleType.DaysOfWeek, daysOfWeek);
                }
            }
        }

        public void AppendAroundTimeRule(List<ScheduleRule> rules, DateTime? aroundTime)
        {
            if (aroundTime.HasValue)
            {
                rules.Add(ScheduleRuleType.AroundTime,
                    new ScheduleTime(aroundTime.Value.Hour, aroundTime.Value.Minute, aroundTime.Value.Second));
            }
        }

        public void AppendStartingBetweenRule(List<ScheduleRule> rules, bool enabled, DateTime lowerTime, DateTime upperTime)
        {
            if (enabled)
            {
                rules.Add(ScheduleRuleType.StartingBetween,
                    new ScheduleTime(lowerTime.Hour, lowerTime.Minute, lowerTime.Second),
                    new ScheduleTime(upperTime.Hour, upperTime.Minute, upperTime.Second));
            }
        }

        public void AppendChannelsRule(List<ScheduleRule> rules, bool notOnChannels, IList channelIds)
        {
            ScheduleRule channelsRule = new ScheduleRule(notOnChannels ? ScheduleRuleType.NotOnChannels : ScheduleRuleType.Channels);
            foreach (Guid channel in channelIds)
            {
                channelsRule.Arguments.Add(channel);
            }
            if (channelsRule.Arguments.Count > 0)
            {
                rules.Add(channelsRule);
            }
        }

        public void AppendCategoriesRule(List<ScheduleRule> rules, bool doNotEqual, IList categories)
        {
            AppendStringArgumentsRule(rules, doNotEqual ? ScheduleRuleType.CategoryDoesNotEqual : ScheduleRuleType.CategoryEquals, categories);
        }

        public void AppendDirectedByRule(List<ScheduleRule> rules, IList directors)
        {
            AppendStringArgumentsRule(rules, ScheduleRuleType.DirectedBy, directors);
        }

        public void AppendWithActorRule(List<ScheduleRule> rules, IList actors)
        {
            AppendStringArgumentsRule(rules, ScheduleRuleType.WithActor, actors);
        }

        public void AppendNewEpisodesOnlyRule(List<ScheduleRule> rules, bool newEpisodesOnly)
        {
            if (newEpisodesOnly)
            {
                rules.Add(ScheduleRuleType.NewEpisodesOnly, true);
            }
        }

        public void AppendNewTitlesOnlyRule(List<ScheduleRule> rules, bool newTitlesOnly)
        {
            if (newTitlesOnly)
            {
                rules.Add(ScheduleRuleType.NewTitlesOnly, true);
            }
        }

        public void AppendSkipRepeatsRule(List<ScheduleRule> rules, bool skipRepeats)
        {
            if (skipRepeats)
            {
                rules.Add(ScheduleRuleType.SkipRepeats, true);
            }
        }
    }
}
