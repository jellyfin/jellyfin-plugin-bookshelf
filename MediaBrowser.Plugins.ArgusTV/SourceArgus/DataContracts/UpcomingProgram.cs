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

namespace ArgusTV.DataContracts
{
    /// <summary>
    /// An upcoming program.
    /// </summary>
    public partial class UpcomingProgram : IProgramSummary
	{
        /// <summary>
        /// The unique ID of the upcoming program.
        /// </summary>
        public Guid UpcomingProgramId { get; set; }

        /// <summary>
        /// The unique integer ID of the upcoming program.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The channel this program is shown on.
        /// </summary>
        public Channel Channel { get; set; }

        /// <summary>
        /// The ID of the schedule this upcoming program belongs to.
        /// </summary>
        public Guid ScheduleId { get; set; }

        /// <summary>
        /// The integer ID of the schedule this upcoming program belongs to.
        /// </summary>
        public int ScheduleIntId { get; set; }

        /// <summary>
        /// The priority of this program (inherited from its schedule by default).
        /// </summary>
        public UpcomingProgramPriority Priority { get; set; }

        /// <summary>
        /// True if this program is part of a series of programs (e.g. episodes).
        /// </summary>
        public bool IsPartOfSeries { get; set; }

        /// <summary>
        /// True if this program is cancelled and should be ignored.
        /// </summary>
        public bool IsCancelled { get; set; }

        /// <summary>
        /// The reason the upcoming program was flagged as cancelled.
        /// </summary>
        public UpcomingCancellationReason CancellationReason { get; set; }

        /// <summary>
        /// The pre-recording span in seconds.
        /// </summary>
        public int PreRecordSeconds { get; set; }

        /// <summary>
        /// The post-recording span in seconds.
        /// </summary>
        public int PostRecordSeconds { get; set; }

        /// <summary>
        /// Null if this is a manual recording or the ID of the guide program of this upcoming program.
        /// </summary>
        public Guid? GuideProgramId { get; set; }

        /// <summary>
        /// Null if this is a manual recording or the integer ID of the guide program of this upcoming program.
        /// </summary>
        public int? GuideProgramIntId { get; set; }

        /// <summary>
        /// The title of the upcoming program.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The start time of the upcoming program.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// The stop time of the upcoming program.
        /// </summary>
        public DateTime StopTime { get; set; }

        /// <summary>
        /// The start time of the upcoming program (UTC).
        /// </summary>
        public DateTime StartTimeUtc { get; set; }

        /// <summary>
        /// The stop time of the upcoming program (UTC).
        /// </summary>
        public DateTime StopTimeUtc { get; set; }

        /// <summary>
        /// The episode title of the upcoming program, or null.
        /// </summary>
        public string SubTitle { get; set; }

        /// <summary>
        /// The category description of the upcoming program.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// True if this is a repeat showing.
        /// </summary>
        public bool IsRepeat { get; set; }

        /// <summary>
        /// True if this is a premiere showing.
        /// </summary>
        public bool IsPremiere { get; set; }

        /// <summary>
        /// The program's flags defining things like aspect ratio, SD or HD,...
        /// </summary>
        public GuideProgramFlags Flags { get; set; }

        /// <summary>
        /// The episode number (as a string) of the upcoming program.
        /// </summary>
        public string EpisodeNumberDisplay { get; set; }

        /// <summary>
        /// The rating description of the upcoming program.
        /// </summary>
        public string Rating { get; set; }

        /// <summary>
        /// Null or the star rating between 0.0 and 1.0 of the upcoming program.
        /// </summary>
        public double? StarRating { get; set; }

        /// <summary>
        /// The unique checksum of the of the upcoming program's description.
        /// </summary>
        public string DescriptionChecksum { get; set; }

        /// <summary>
        /// The ID of the already queued upcoming program that's a duplicate of this program, in case the
        /// schedule is a recording schedule containing a NewEpisodesOnly or NewTitlesOnly rule type.
        /// </summary>
        public Guid? AlreadyQueuedProgramId { get; set; }

        #region Miscellaneous

        /// <summary>
        /// The duration of the upcoming program, without pre- and post-padding.
        /// </summary>
        public TimeSpan Duration
        {
            get { return this.StopTimeUtc - this.StartTimeUtc; }
        }

        /// <summary>
        /// The actual start time of the upcoming program (includes pre-padding).
        /// </summary>
        public DateTime ActualStartTime
        {
            get { return this.StartTime.AddSeconds(-this.PreRecordSeconds); }
        }

        /// <summary>
        /// The actual stop time of the upcoming program (includes post-padding).
        /// </summary>
        public DateTime ActualStopTime
        {
            get { return this.StopTime.AddSeconds(this.PostRecordSeconds); }
        }

        /// <summary>
        /// The actual start time (UTC) of the upcoming program (includes pre-padding).
        /// </summary>
        public DateTime ActualStartTimeUtc
        {
            get { return this.StartTimeUtc.AddSeconds(-this.PreRecordSeconds); }
        }

        /// <summary>
        /// The actual stop time (UTC) of the upcoming program (includes post-padding).
        /// </summary>
        public DateTime ActualStopTimeUtc
        {
            get { return this.StopTimeUtc.AddSeconds(this.PostRecordSeconds); }
        }

        /// <summary>
        /// The actual duration of the upcoming program (includes pre- and post-padding).
        /// </summary>
        public TimeSpan ActualDuration
        {
            get { return this.ActualStopTimeUtc - this.ActualStartTimeUtc; }
        }

        /// <summary>
        /// Build a full program title for display purposes.
        /// </summary>
        /// <returns>A full title containing all episode information.</returns>
        public string CreateProgramTitle()
        {
            return GuideProgram.CreateProgramTitle(this.Title, this.SubTitle, this.EpisodeNumberDisplay);
        }

        /// <summary>
        /// Build a full episode title for display purposes.
        /// </summary>
        /// <returns>An full episode title containing all episode information.</returns>
        public string CreateEpisodeTitle()
        {
            return GuideProgram.CreateEpisodeTitle(this.SubTitle, this.EpisodeNumberDisplay);
        }

        /// <summary>
        /// Create a unique upcoming program ID for an upcoming program that records an
        /// actual guide program.
        /// </summary>
        /// <param name="guideProgramId">The unique ID of the guide program.</param>
        /// <param name="channelId">The unique ID of the channel this program is scheduled on.</param>
        /// <returns>The unique upcoming program ID.</returns>
        public static Guid GetUniqueUpcomingProgramId(Guid guideProgramId, Guid channelId)
        {
            byte[] bytes1 = guideProgramId.ToByteArray();
            byte[] bytes2 = channelId.ToByteArray();
            for (int index = 0; index < bytes1.Length; index++)
            {
                bytes1[index] ^= bytes2[index];
            }
            return new Guid(bytes1);
        }

        /// <summary>
        /// Create a unique upcoming program integer ID for an upcoming program that records an
        /// actual guide program.
        /// </summary>
        /// <param name="guideProgramIntId">The unique integer ID of the guide program.</param>
        /// <param name="channelIntId">The unique integer ID of the channel this program is scheduled on.</param>
        /// <returns>The unique upcoming program integer ID.</returns>
        public static int GetUniqueUpcomingProgramIntId(int guideProgramIntId, int channelIntId)
        {
            return ((channelIntId & 0xffff) << 16) + (guideProgramIntId & 0xffff);
        }

        /// <summary>
        /// Create a unique upcoming program ID for an upcoming program that records a manual
        /// schedule.
        /// </summary>
        /// <param name="scheduleId">The unique ID of the manual schedule.</param>
        /// <param name="startTime">The start time of the upcoming program.</param>
        /// <returns>The unique upcoming program ID.</returns>
        public static Guid GetUniqueUpcomingProgramId(Guid scheduleId, DateTime startTime)
        {
            byte[] bytes = scheduleId.ToByteArray();
            bytes[0] ^= (byte)(startTime.Ticks >> 56);
            bytes[2] ^= (byte)(startTime.Ticks >> 48);
            bytes[4] ^= (byte)(startTime.Ticks >> 40);
            bytes[6] ^= (byte)(startTime.Ticks >> 32);
            bytes[8] ^= (byte)(startTime.Ticks >> 24);
            bytes[10] ^= (byte)(startTime.Ticks >> 16);
            bytes[12] ^= (byte)(startTime.Ticks >> 8);
            bytes[14] ^= (byte)startTime.Ticks;
            return new Guid(bytes);
        }

        /// <summary>
        /// Create a unique upcoming program integer ID for an upcoming program that records a
        /// manual schedule.
        /// </summary>
        /// <param name="scheduleIntId">The unique integer ID of the manual schedule.</param>
        /// <param name="startTime">The start time of the upcoming program.</param>
        /// <returns>The unique upcoming program ID.</returns>
        public static int GetUniqueUpcomingProgramIntId(int scheduleIntId, DateTime startTime)
        {
            return ((scheduleIntId & 0xffff) << 16) + ((int)TimeSpan.FromTicks(startTime.Ticks).TotalMinutes & 0xffff);
        }

        #endregion
    }
}
