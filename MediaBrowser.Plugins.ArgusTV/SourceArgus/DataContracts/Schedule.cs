/*
 *	Copyright (C) 2007-2014 ARGUS TV
 *	http://www.argus-tv.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA.
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Script.Serialization;

namespace ArgusTV.DataContracts
{
	/// <summary>
	/// A schedule.
	/// </summary>
	public class Schedule : ScheduleBase<ScheduleRule>
	{
        /// <summary>
        /// The number of minutes to start the recording before the program start time, or null for the default.
        /// </summary>
        [ScriptIgnore]
        public double? PreRecordMinutes
        {
            get { return this.PreRecordSeconds / 60.0; }
            set
            {
                this.PreRecordSeconds = value.HasValue ? (int?)Math.Round(value.Value * 60) : null;
            }
        }

        /// <summary>
        /// The number of minutes to stop the recording past the program stop time, or null for the default.
        /// </summary>
        [ScriptIgnore]
        public double? PostRecordMinutes
        {
            get { return this.PostRecordSeconds / 60.0; }
            set
            {
                this.PostRecordSeconds = value.HasValue ? (int?)Math.Round(value.Value * 60) : null;
            }
        }
    }

    /// <summary>
    /// Schedule class that easily serializes as e.g. JSON.
    /// </summary>
    public class SerializableSchedule : ScheduleBase<SerializableSchedule.SerializableRule>
    {
        /// <summary>
        /// A schedule-rule serializable with 'string' type and arguments.
        /// </summary>
        public class SerializableRule
        {
            /// <summary>
            /// The type of the rule as a string.
            /// </summary>
            public string Type { get; set; }

            /// <summary>
            /// A list of string arguments for the rule.
            /// </summary>
            public List<string> Arguments { get; set; }
        }
    }

    /// <summary>
    /// Base schedule class.
    /// </summary>
    /// <typeparam name="TRule">The type of a schedule rule.</typeparam>
    public abstract class ScheduleBase<TRule>
        where TRule: class
    {
        /// <summary>
        /// The default constructor.
        /// </summary>
        public ScheduleBase()
        {
            this.ChannelType = ChannelType.Television;
            this.ScheduleType = ScheduleType.Recording;
            this.SchedulePriority = SchedulePriority.Normal;
            this.KeepUntilMode = KeepUntilMode.UntilSpaceIsNeeded;
            this.Rules = new List<TRule>();
            this.ProcessingCommands = new List<ScheduleProcessingCommand>();
        }

        /// <summary>
        /// The unique ID of the schedule.
        /// </summary>
        public Guid ScheduleId { get; set; }

        /// <summary>
        /// The unique integer ID of the schedule.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The name of the schedule.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The channel-type of this schedule.
        /// </summary>
        public ChannelType ChannelType { get; set; }

        /// <summary>
        /// The type of the schedule.
        /// </summary>
        public ScheduleType ScheduleType { get; set; }

        /// <summary>
        /// The priority of the schedule.
        /// </summary>
        public SchedulePriority SchedulePriority { get; set; }

        /// <summary>
        /// Defines how long to keep this recording before deleting it.
        /// </summary>
        public KeepUntilMode KeepUntilMode { get; set; }

        /// <summary>
        /// Defines how long to keep this recording before deleting it (see KeepUntilMode).
        /// </summary>
        public int? KeepUntilValue { get; set; }

        /// <summary>
        /// The rules that make up the schedule.
        /// </summary>
        public List<TRule> Rules { get; set; }

        /// <summary>
        /// The number of seconds to start the recording before the program start time, or null for the default.
        /// </summary>
        public int? PreRecordSeconds { get; set; }

        /// <summary>
        /// The number of seconds to stop the recording past the program stop time, or null for the default.
        /// </summary>
        public int? PostRecordSeconds { get; set; }

        /// <summary>
        /// Is this schedule active?
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Is this a one-time schedule?
        /// </summary>
        public bool IsOneTime { get; set; }

        /// <summary>
        /// The unique ID of the file format for this schedule, or null for the default.
        /// </summary>
        public Guid? RecordingFileFormatId { get; set; }

        /// <summary>
        /// The time the record was last modified.
        /// </summary>
        public DateTime LastModifiedTime { get; set; }

        /// <summary>
        /// INTERNAL USE ONLY.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// The collection of the schedule's processing commands.
        /// </summary>
        public List<ScheduleProcessingCommand> ProcessingCommands { get; set; }
    }

    /// <summary>
    /// Handy extensions on schedules and their rules.
    /// </summary>
    public static class ScheduleExtensions
    {
        /// <summary>
        /// Convert the schedule to a more serializable-friendly one (e.g for JSON).
        /// </summary>
        /// <param name="schedule">The schedule to convert.</param>
        /// <returns>The serializable schedule.</returns>
        public static SerializableSchedule ToSerializable(this Schedule schedule)
        {
            SerializableSchedule serializableSchedule = null;

            if (schedule != null)
            {
                serializableSchedule = new SerializableSchedule()
                {
                    ChannelType = schedule.ChannelType,
                    Id = schedule.Id,
                    IsActive = schedule.IsActive,
                    IsOneTime = schedule.IsOneTime,
                    KeepUntilMode = schedule.KeepUntilMode,
                    KeepUntilValue = schedule.KeepUntilValue,
                    LastModifiedTime = schedule.LastModifiedTime,
                    Name = schedule.Name,
                    PostRecordSeconds = schedule.PostRecordSeconds,
                    PreRecordSeconds = schedule.PreRecordSeconds,
                    ProcessingCommands = schedule.ProcessingCommands,
                    RecordingFileFormatId = schedule.RecordingFileFormatId,
                    ScheduleId = schedule.ScheduleId,
                    SchedulePriority = schedule.SchedulePriority,
                    ScheduleType = schedule.ScheduleType,
                    Version = schedule.Version,
                    Rules = new List<SerializableSchedule.SerializableRule>()
                };

                schedule.Rules.ForEach(r => serializableSchedule.Rules.Add(ToSerializableRule(r)));
            }

            return serializableSchedule;
        }

        /// <summary>
        /// Convert the serializable schedule to a normal one, with typed rules.
        /// </summary>
        /// <param name="serializableSchedule">The serializable schedule.</param>
        /// <returns>A schedule with typed rules.</returns>
        public static Schedule ToSchedule(this SerializableSchedule serializableSchedule)
        {
            Schedule schedule = null;

            if (serializableSchedule != null)
            {
                schedule = new Schedule()
                {
                    ChannelType = serializableSchedule.ChannelType,
                    Id = serializableSchedule.Id,
                    IsActive = serializableSchedule.IsActive,
                    IsOneTime = serializableSchedule.IsOneTime,
                    KeepUntilMode = serializableSchedule.KeepUntilMode,
                    KeepUntilValue = serializableSchedule.KeepUntilValue,
                    LastModifiedTime = serializableSchedule.LastModifiedTime,
                    Name = serializableSchedule.Name,
                    PostRecordSeconds = serializableSchedule.PostRecordSeconds,
                    PreRecordSeconds = serializableSchedule.PreRecordSeconds,
                    ProcessingCommands = serializableSchedule.ProcessingCommands,
                    RecordingFileFormatId = serializableSchedule.RecordingFileFormatId,
                    ScheduleId = serializableSchedule.ScheduleId,
                    SchedulePriority = serializableSchedule.SchedulePriority,
                    ScheduleType = serializableSchedule.ScheduleType,
                    Version = serializableSchedule.Version
                };

                serializableSchedule.Rules.ForEach(r => schedule.Rules.Add(ToRule(r)));
            }

            return schedule;
        }

        /// <summary>
        /// Add a new rule (with optional arguments).
        /// </summary>
        /// <param name="scheduleRules">The schedule rules collection.</param>
        /// <param name="type">The type of the rule to add.</param>
        /// <param name="args">Optional arguments to the rule.</param>
        public static void Add(this List<ScheduleRule> scheduleRules, ScheduleRuleType type, params object[] args)
        {
            scheduleRules.Add(new ScheduleRule(type, args));
        }

        /// <summary>
        /// Find a rule with a given type.
        /// </summary>
        /// <param name="scheduleRules">The schedule rules collection.</param>
        /// <param name="ruleType">The type to search for.</param>
        /// <returns>The schedule rule of the given type, or null if no rule of the given type was found.</returns>
        public static ScheduleRule FindRuleByType(this List<ScheduleRule> scheduleRules, ScheduleRuleType ruleType)
        {
            return scheduleRules.FirstOrDefault(r => r.Type == ruleType);
        }

        private static SerializableSchedule.SerializableRule ToSerializableRule(this ScheduleRule from)
        {
            SerializableSchedule.SerializableRule result = null;
            if (from != null)
            {
                result = new SerializableSchedule.SerializableRule()
                {
                    Type = from.Type.ToString(),
                    Arguments = new List<string>()
                };
                switch (from.Type)
                {
                    case ScheduleRuleType.Channels:
                    case ScheduleRuleType.NotOnChannels:
                        ArgumentsToSerializable<Guid>(result, from);
                        break;

                    case ScheduleRuleType.AroundTime:
                    case ScheduleRuleType.StartingBetween:
                        from.Arguments.ForEach(a => result.Arguments.Add(new DateTime(((ScheduleTime)a).Ticks, DateTimeKind.Local).ToString("HH:mm:ss")));
                        break;

                    case ScheduleRuleType.ManualSchedule:
                        if (from.Arguments.Count > 0)
                        {
                            result.Arguments.Add(((DateTime)from.Arguments[0]).ToString("yyyy-MM-ddTHH:mm:sszzz"));
                        }
                        if (from.Arguments.Count > 1)
                        {
                            result.Arguments.Add(new DateTime(((ScheduleTime)from.Arguments[1]).Ticks, DateTimeKind.Local).ToString("HH:mm:ss"));
                        }
                        break;

                    case ScheduleRuleType.OnDate:
                        if (from.Arguments.Count > 0)
                        {
                            result.Arguments.Add(((DateTime)from.Arguments[0]).ToString("yyyy-MM-ddTHH:mm:sszzz"));
                        }
                        break;

                    case ScheduleRuleType.DaysOfWeek:
                        if (from.Arguments.Count > 0)
                        {
                            result.Arguments.Add(((int)from.Arguments[0]).ToString());
                        }
                        if (from.Arguments.Count > 1)
                        {
                            result.Arguments.Add(((DateTime)from.Arguments[1]).ToString("yyyy-MM-ddTHH:mm:sszzz"));
                        }
                        break;

                    case ScheduleRuleType.TitleDoesNotContain:
                    case ScheduleRuleType.SubTitleDoesNotContain:
                    case ScheduleRuleType.EpisodeNumberDoesNotContain:
                    case ScheduleRuleType.DescriptionDoesNotContain:
                    case ScheduleRuleType.ProgramInfoDoesNotContain:
                    case ScheduleRuleType.TitleStartsWith:
                    case ScheduleRuleType.SubTitleStartsWith:
                    case ScheduleRuleType.EpisodeNumberStartsWith:
                    case ScheduleRuleType.TitleEquals:
                    case ScheduleRuleType.SubTitleEquals:
                    case ScheduleRuleType.EpisodeNumberEquals:
                    case ScheduleRuleType.TitleContains:
                    case ScheduleRuleType.SubTitleContains:
                    case ScheduleRuleType.EpisodeNumberContains:
                    case ScheduleRuleType.DescriptionContains:
                    case ScheduleRuleType.ProgramInfoContains:
                    case ScheduleRuleType.CategoryEquals:
                    case ScheduleRuleType.CategoryDoesNotEqual:
                    case ScheduleRuleType.DirectedBy:
                    case ScheduleRuleType.WithActor:
                        ArgumentsToSerializable<string>(result, from);
                        break;

                    case ScheduleRuleType.SkipRepeats:
                    case ScheduleRuleType.NewEpisodesOnly:
                    case ScheduleRuleType.NewTitlesOnly:
                        ArgumentsToSerializable<bool>(result, from);
                        break;
                }
            }
            return result;
        }

        private static void ArgumentsToSerializable<T>(SerializableSchedule.SerializableRule to, ScheduleRule from)
        {
            foreach (object arg in from.Arguments)
            {
                to.Arguments.Add(((T)arg).ToString());
            }
        }

        private static ScheduleRule ToRule(this SerializableSchedule.SerializableRule from)
        {
            ScheduleRule result = null;
            if (from != null)
            {
                result = new ScheduleRule((ScheduleRuleType)Enum.Parse(typeof(ScheduleRuleType), from.Type));
                switch (result.Type)
                {
                    case ScheduleRuleType.Channels:
                    case ScheduleRuleType.NotOnChannels:
                        from.Arguments.ForEach(a => result.Arguments.Add(new Guid(a)));
                        break;

                    case ScheduleRuleType.AroundTime:
                    case ScheduleRuleType.StartingBetween:
                        from.Arguments.ForEach(a => result.Arguments.Add(ParseScheduleTime(a)));
                        break;

                    case ScheduleRuleType.OnDate:
                        if (from.Arguments.Count > 0)
                        {
                            result.Arguments.Add(ParseIsoDate(from.Arguments[0]));
                        }
                        break;

                    case ScheduleRuleType.ManualSchedule:
                        if (from.Arguments.Count > 0)
                        {
                            result.Arguments.Add(ParseIsoDate(from.Arguments[0]));
                        }
                        if (from.Arguments.Count > 1)
                        {
                            result.Arguments.Add(ParseScheduleTime(from.Arguments[1]));
                        }
                        break;

                    case ScheduleRuleType.DaysOfWeek:
                        if (from.Arguments.Count > 0)
                        {
                            result.Arguments.Add(Enum.Parse(typeof(ScheduleDaysOfWeek), from.Arguments[0]));
                        }
                        if (from.Arguments.Count > 1)
                        {
                            result.Arguments.Add(ParseIsoDate(from.Arguments[1]));
                        }
                        break;

                    case ScheduleRuleType.TitleDoesNotContain:
                    case ScheduleRuleType.SubTitleDoesNotContain:
                    case ScheduleRuleType.EpisodeNumberDoesNotContain:
                    case ScheduleRuleType.DescriptionDoesNotContain:
                    case ScheduleRuleType.ProgramInfoDoesNotContain:
                    case ScheduleRuleType.TitleStartsWith:
                    case ScheduleRuleType.SubTitleStartsWith:
                    case ScheduleRuleType.EpisodeNumberStartsWith:
                    case ScheduleRuleType.TitleEquals:
                    case ScheduleRuleType.SubTitleEquals:
                    case ScheduleRuleType.EpisodeNumberEquals:
                    case ScheduleRuleType.TitleContains:
                    case ScheduleRuleType.SubTitleContains:
                    case ScheduleRuleType.EpisodeNumberContains:
                    case ScheduleRuleType.DescriptionContains:
                    case ScheduleRuleType.ProgramInfoContains:
                    case ScheduleRuleType.CategoryEquals:
                    case ScheduleRuleType.CategoryDoesNotEqual:
                    case ScheduleRuleType.DirectedBy:
                    case ScheduleRuleType.WithActor:
                        from.Arguments.ForEach(a => result.Arguments.Add(a));
                        break;

                    case ScheduleRuleType.SkipRepeats:
                    case ScheduleRuleType.NewEpisodesOnly:
                    case ScheduleRuleType.NewTitlesOnly:
                        from.Arguments.ForEach(a => result.Arguments.Add(bool.Parse(a)));
                        break;
                }
            }
            return result;
        }

        private static ScheduleTime ParseScheduleTime(string source)
        {
            string[] parts = source.Split(':');
            int hours = int.Parse(parts[0]);
            int mins = int.Parse(parts[1]);
            int secs = (parts.Length > 1) ? int.Parse(parts[2]) : 0;
            return new ScheduleTime(hours, mins, secs);
        }

        private static DateTime ParseIsoDate(string source)
        {
            string[] formats = new string[] { "yyyy-MM-ddTHH:mm:sszzz", "yyyy-MM-ddTHH:mm:ss", "yyyyMMddTHHmmss", "yyyyMMdd", "yyyy-MM-dd" };
            return DateTime.ParseExact(source, formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowLeadingWhite|DateTimeStyles.AllowTrailingWhite|DateTimeStyles.AssumeLocal);
        }
    }
}
