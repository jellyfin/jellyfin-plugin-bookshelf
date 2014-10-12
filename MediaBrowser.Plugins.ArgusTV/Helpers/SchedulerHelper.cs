using ArgusTV.DataContracts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.ArgusTV.Helpers
{
    public static class SchedulerHelper
    {
        public static void AppendTitleRule(List<ScheduleRule> rules, int titleRuleTypeIndex, string text)
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

        public static void AppendSubTitleRule(List<ScheduleRule> rules, int titleRuleTypeIndex, string text)
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

        public static void AppendEpisodeNumberRule(List<ScheduleRule> rules, int titleRuleTypeIndex, string text)
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

        public static void AppendDescriptionRule(List<ScheduleRule> rules, string text)
        {
            AppendContainsRule(rules, ScheduleRuleType.DescriptionContains, ScheduleRuleType.DescriptionDoesNotContain, text);
        }

        public static void AppendProgramInfoRule(List<ScheduleRule> rules, string text)
        {
            AppendContainsRule(rules, ScheduleRuleType.ProgramInfoContains, ScheduleRuleType.ProgramInfoDoesNotContain, text);
        }

        public static void AppendOnDateAndDaysOfWeekRule(List<ScheduleRule> rules, ScheduleDaysOfWeek daysOfWeek, DateTime? onDateTime)
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

        public static void AppendAroundTimeRule(List<ScheduleRule> rules, DateTime? aroundTime)
        {
            if (aroundTime.HasValue)
            {
                rules.Add(ScheduleRuleType.AroundTime,
                    new ScheduleTime(aroundTime.Value.Hour, aroundTime.Value.Minute, aroundTime.Value.Second));
            }
        }

        public static void AppendStartingBetweenRule(List<ScheduleRule> rules, bool enabled, DateTime lowerTime, DateTime upperTime)
        {
            if (enabled)
            {
                rules.Add(ScheduleRuleType.StartingBetween,
                    new ScheduleTime(lowerTime.Hour, lowerTime.Minute, lowerTime.Second),
                    new ScheduleTime(upperTime.Hour, upperTime.Minute, upperTime.Second));
            }
        }

        public static void AppendChannelsRule(List<ScheduleRule> rules, bool notOnChannels, IList channelIds)
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

        public static void AppendCategoriesRule(List<ScheduleRule> rules, bool doNotEqual, IList categories)
        {
            AppendStringArgumentsRule(rules, doNotEqual ? ScheduleRuleType.CategoryDoesNotEqual : ScheduleRuleType.CategoryEquals, categories);
        }

        public static void AppendDirectedByRule(List<ScheduleRule> rules, IList directors)
        {
            AppendStringArgumentsRule(rules, ScheduleRuleType.DirectedBy, directors);
        }

        public static void AppendWithActorRule(List<ScheduleRule> rules, IList actors)
        {
            AppendStringArgumentsRule(rules, ScheduleRuleType.WithActor, actors);
        }

        public static void AppendNewEpisodesOnlyRule(List<ScheduleRule> rules, bool newEpisodesOnly)
        {
            if (newEpisodesOnly)
            {
                rules.Add(ScheduleRuleType.NewEpisodesOnly, true);
            }
        }

        public static void AppendNewTitlesOnlyRule(List<ScheduleRule> rules, bool newTitlesOnly)
        {
            if (newTitlesOnly)
            {
                rules.Add(ScheduleRuleType.NewTitlesOnly, true);
            }
        }

        public static void AppendSkipRepeatsRule(List<ScheduleRule> rules, bool skipRepeats)
        {
            if (skipRepeats)
            {
                rules.Add(ScheduleRuleType.SkipRepeats, true);
            }
        }


        //Private methods
        private static string GetTitleRuleExpression(List<ScheduleRule> rules, ScheduleRuleType equalsRule, ScheduleRuleType startsWithRule,
    ScheduleRuleType containsRule, ScheduleRuleType doesNotContainRule, out int typeIndex)
        {
            string expression = GetContainsExpression(rules, containsRule, doesNotContainRule);
            if (String.IsNullOrEmpty(expression))
            {
                typeIndex = TitleRuleTypeIndex.Equals;
                foreach (ScheduleRule rule in rules)
                {
                    if (rule.Type == equalsRule)
                    {
                        expression = JoinORedArguments(rule.Arguments);
                        break;
                    }
                    else if (rule.Type == startsWithRule)
                    {
                        expression = JoinORedArguments(rule.Arguments);
                        typeIndex = TitleRuleTypeIndex.StartsWith;
                        break;
                    }
                }
            }
            else
            {
                typeIndex = TitleRuleTypeIndex.Contains;
            }
            return expression;
        }

        private static string JoinORedArguments(List<object> arguments)
        {
            if (arguments.Count == 1)
            {
                return (string)arguments[0];
            }
            else
            {
                StringBuilder text = new StringBuilder();
                foreach (string argument in arguments)
                {
                    if (text.Length > 0)
                    {
                        text.Append(" OR ");
                    }
                    text.Append(argument);
                }
                return text.ToString();
            }
        }

        private static string GetContainsExpression(List<ScheduleRule> rules, ScheduleRuleType containsRule, ScheduleRuleType doesNotContainRule)
        {
            StringBuilder expression = new StringBuilder();
            foreach (ScheduleRule rule in rules)
            {
                if (rule.Type == containsRule)
                {
                    if (expression.Length > 0)
                    {
                        expression.Append(" AND ");
                    }
                    foreach (string arg in rule.Arguments)
                    {
                        expression.Append(arg).Append(" OR ");
                    }
                    if (expression.Length >= 4)
                    {
                        expression.Remove(expression.Length - 4, 4);
                    }
                }
                else if (rule.Type == doesNotContainRule)
                {
                    if (expression.Length > 0)
                    {
                        expression.Append(" ");
                    }
                    expression.Append("NOT ").Append(rule.Arguments[0]);
                }
            }
            return expression.ToString();
        }

        private enum Operator
        {
            None,
            Or,
            And,
            Not
        }

        private static void AppendORableRule(List<ScheduleRule> rules, ScheduleRuleType rule, string expression)
        {
            expression = expression.Trim();
            if (!String.IsNullOrEmpty(expression))
            {
                List<object> arguments = new List<object>();

                int index = 0;
                while (index < expression.Length)
                {
                    int operatorIndex;
                    int nextIndex;
                    Operator op = GetNextOperator(expression, index, out operatorIndex, out nextIndex);
                    if (op == Operator.None)
                    {
                        arguments.Add(expression.Substring(index).Trim());
                        rules.Add(rule, arguments.ToArray());
                        break;
                    }
                    string fragment = expression.Substring(index, operatorIndex - index).Trim();
                    if (fragment.Length > 0
                        && fragment != "AND"
                        && fragment != "OR")
                    {
                        arguments.Add(fragment);
                    }
                    index = nextIndex;
                }
            }
        }

        private static void AppendContainsRule(List<ScheduleRule> rules, ScheduleRuleType containsRule,
            ScheduleRuleType doesNotContainRule, string expression)
        {
            expression = expression.Trim();
            if (!String.IsNullOrEmpty(expression))
            {
                List<object> arguments = new List<object>();

                bool lastOperatorWasNot = false;
                int index = 0;
                while (index < expression.Length)
                {
                    int operatorIndex;
                    int nextIndex;
                    Operator op = GetNextOperator(expression, index, out operatorIndex, out nextIndex);
                    if (op == Operator.None)
                    {
                        arguments.Add(expression.Substring(index).Trim());
                        rules.Add(lastOperatorWasNot ? doesNotContainRule : containsRule, arguments.ToArray());
                        break;
                    }
                    string fragment = expression.Substring(index, operatorIndex - index).Trim();
                    if (fragment.Length > 0
                        && fragment != "AND"
                        && fragment != "OR")
                    {
                        if (lastOperatorWasNot)
                        {
                            rules.Add(doesNotContainRule, fragment);
                        }
                        else
                        {
                            arguments.Add(fragment);
                            if (op != Operator.Or)
                            {
                                rules.Add(containsRule, arguments.ToArray());
                                arguments.Clear();
                            }
                        }
                    }
                    lastOperatorWasNot = (op == Operator.Not);
                    index = nextIndex;
                }
            }
        }

        private static Operator GetNextOperator(string expression, int startIndex, out int operatorIndex, out int nextIndex)
        {
            string orOperator = " OR ";
            string andOperator = " AND ";
            string notOperator = "NOT ";

            int orOperatorIndex = expression.IndexOf(orOperator, startIndex);
            int andOperatorIndex = expression.IndexOf(andOperator, startIndex);
            int notOperatorIndex = expression.IndexOf(notOperator, startIndex);
            if (notOperatorIndex > startIndex)
            {
                notOperator = " NOT ";
                notOperatorIndex = expression.IndexOf(notOperator, startIndex);
            }
            if (orOperatorIndex >= 0
                && (andOperatorIndex < 0 || orOperatorIndex < andOperatorIndex)
                && (notOperatorIndex < 0 || orOperatorIndex < notOperatorIndex))
            {
                operatorIndex = orOperatorIndex;
                nextIndex = orOperatorIndex + orOperator.Length;
                return Operator.Or;
            }
            if (andOperatorIndex >= 0
                && (orOperatorIndex < 0 || andOperatorIndex < orOperatorIndex)
                && (notOperatorIndex < 0 || andOperatorIndex < notOperatorIndex))
            {
                operatorIndex = andOperatorIndex;
                nextIndex = andOperatorIndex + andOperator.Length;
                return Operator.And;
            }
            if (notOperatorIndex >= 0
                && (orOperatorIndex < 0 || notOperatorIndex < orOperatorIndex)
                && (andOperatorIndex < 0 || notOperatorIndex < andOperatorIndex))
            {
                operatorIndex = notOperatorIndex;
                nextIndex = notOperatorIndex + notOperator.Length;
                return Operator.Not;
            }
            operatorIndex = -1;
            nextIndex = expression.Length;
            return Operator.None;
        }

        private static void AppendStringArgumentsRule(List<ScheduleRule> rules, ScheduleRuleType ruleType, IList arguments)
        {
            ScheduleRule rule = new ScheduleRule(ruleType);
            foreach (string arg in arguments)
            {
                rule.Arguments.Add(arg);
            }
            if (rule.Arguments.Count > 0)
            {
                rules.Add(rule);
            }
        }

        private static string BuildManualScheduleName(string channelDisplayName, DateTime startTime, TimeSpan duration, ScheduleDaysOfWeek daysOfWeek)
        {
            StringBuilder name = new StringBuilder();
            if (daysOfWeek == ScheduleDaysOfWeek.None)
            {
                name.AppendFormat("{0} {1:g} ({2:00}:{3:00})", channelDisplayName, startTime, duration.Hours, duration.Minutes);
            }
            else
            {
                name.AppendFormat("{0} ", channelDisplayName);
                if ((daysOfWeek & ScheduleDaysOfWeek.Mondays) != 0)
                {
                    name.Append("Mo,");
                }
                if ((daysOfWeek & ScheduleDaysOfWeek.Tuesdays) != 0)
                {
                    name.Append("Tu,");
                }
                if ((daysOfWeek & ScheduleDaysOfWeek.Wednesdays) != 0)
                {
                    name.Append("We,");
                }
                if ((daysOfWeek & ScheduleDaysOfWeek.Thursdays) != 0)
                {
                    name.Append("Th,");
                }
                if ((daysOfWeek & ScheduleDaysOfWeek.Fridays) != 0)
                {
                    name.Append("Fr,");
                }
                if ((daysOfWeek & ScheduleDaysOfWeek.Saturdays) != 0)
                {
                    name.Append("Sa,");
                }
                if ((daysOfWeek & ScheduleDaysOfWeek.Sundays) != 0)
                {
                    name.Append("Su,");
                }
                name.Remove(name.Length - 1, 1);
                name.AppendFormat(" at {0:00}:{1:00} ({2:00}:{3:00})",
                    startTime.TimeOfDay.Hours, startTime.TimeOfDay.Minutes, duration.Hours, duration.Minutes);
            }
            return name.ToString();
        }
    }

    public static class TitleRuleTypeIndex
    {
        public new const int Equals = 0;
        public const int StartsWith = 1;
        public const int Contains = 2;
    }

}
