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
using System.Text;

namespace ArgusTV.DataContracts
{
    /// <summary>
    /// All standard configuration keys.
    /// </summary>
    public static class ConfigurationKey
    {
        /// <summary>
        /// ARGUS TV Scheduler configuration keys.
        /// </summary>
        public static class Scheduler
        {
            /// <summary>
            /// The default number of pre-record seconds for schedules.
            /// </summary>
            public const string PreRecordsSeconds = "PreRecord";
            /// <summary>
            /// The default number of post-record seconds for schedules.
            /// </summary>
            public const string PostRecordsSeconds = "PostRecord";
            /// <summary>
            /// The user's preferred guide source (see GuideSource enumeration).
            /// </summary>
            public const string PreferredGuideSource = "PreferredGuideSource";
            /// <summary>
            /// The keep until mode to use for new schedules (see KeepUntilMode enumeration).
            /// </summary>
            public const string DefaultKeepUntilMode = "DefaultKeepUntilMode";
            /// <summary>
            /// The keep until value to use for new schedules (goes with DefaultKeepUntilMode).
            /// </summary>
            public const string DefaultKeepUntilValue = "DefaultKeepUntilValue";
            /// <summary>
            /// Boolean setting to control combining of consecutive recordings.
            /// </summary>
            public const string CombineConsecutiveRecordings = "CombineConsecutiveRecordings";
            /// <summary>
            /// Boolean setting to control automatic combining of consecutive recordings (when no free card is found).
            /// </summary>
            public const string AutoCombineConsecutiveRecordings = "AutoCombineConsecutiveRecordings";
            /// <summary>
            /// Boolean setting to control combining of consecutive recordings to only occur when the
            /// recordings are on the same channel.
            /// </summary>
            public const string CombineRecordingsOnlyOnSameChannel = "CombineRecordingsOnlyOnSameChannel";
            /// <summary>
            /// The minimum amount of diskspace needed to allow recordings (in megabytes).
            /// </summary>
            public const string MinimumFreeDiskSpaceInMB = "MinimumFreeDiskSpaceInMB";
            /// <summary>
            /// The amount of diskspace to keep free by the cleanup process (in megabytes).
            /// </summary>
            public const string FreeDiskSpaceInMB = "FreeDiskSpaceInMB";
            /// <summary>
            /// One or more mailadresses (separated by a ;) of people to notify in case of problems.
            /// </summary>
            public const string AdministratorEmail = "AdministratorEmail";
            /// <summary>
            /// The from address to use when sending e-mails.
            /// </summary>
            public const string EmailSender = "EmailSender";
            /// <summary>
            /// The SMTP server host name that is used to send out notification mails.
            /// </summary>
            public const string SmtpServer = "SmtpServer";
            /// <summary>
            /// The SMTP server port to use.
            /// </summary>
            public const string SmtpPort = "SmtpPort";
            /// <summary>
            /// Enable the use of SSL for SMTP.
            /// </summary>
            public const string SmtpEnableSsl = "SmtpEnableSsl";
            /// <summary>
            /// The SMTP user name to use.
            /// </summary>
            public const string SmtpUserName = "SmtpUserName";
            /// <summary>
            /// The SMTP password to use.
            /// </summary>
            public const string SmtpPassword = "SmtpPassword";
            /// <summary>
            /// Swap recorder priorities for recordings to avoid conflicts with live streaming.
            /// </summary>
            public const string SwapRecorderTunerPriorityForRecordings = "SwapRecorderTunerPriorityForRecordings";
            /// <summary>
            /// Format to which a finished recording's files are renamed. This format is the default format used for one-time television recordings
            /// and defines the full path and filename (without extension!). The following variables can be used:
            /// <list type="table">
            /// <listheader><term>Variable</term><description>Substituted by</description></listheader>
            /// <item><term>%%CHANNEL%%</term><description>The channel's name.</description></item>
            /// <item><term>%%SCHEDULE%%</term><description>The name of the schedule the program was recorded from.</description></item>
            /// <item><term>%%CATEGORY%%</term><description>The program's category or '#' if it hasn't got one.</description></item>
            /// <item><term>%%TITLE%%</term><description>The program's title (without episode title and/or number).</description></item>
            /// <item><term>%%LONGTITLE%%</term><description>The full program's title with episode title and/or number.</description></item>
            /// <item><term>%%EPISODETITLE%%</term><description>The program's episode title or '#' if it hasn't got one.</description></item>
            /// <item><term>%%EPISODENUMBERDISPLAY%%</term><description>The program's episode number as displayed.</description></item>
            /// <item><term>%%EPISODENUMBER%%</term><description>The program's episode number or '#' if it hasn't got one.</description></item>
            /// <item><term>%%EPISODENUMBER2%%</term><description>The program's episode number in two digits or '00' if it hasn't got one.</description></item>
            /// <item><term>%%EPISODENUMBER3%%</term><description>The program's episode number in three digits or '000' if it hasn't got one.</description></item>
            /// <item><term>%%SERIES%%</term><description>The program's series/season number or '#' if it hasn't got one.</description></item>
            /// <item><term>%%SERIES2%%</term><description>The program's series/season number in two digits or '00' if it hasn't got one.</description></item>
            /// <item><term>%%DATE%%</term><description>The program's airing date in the format yyyy-MM-dd.</description></item>
            /// <item><term>%%YEAR%%</term><description>The program's airing year.</description></item>
            /// <item><term>%%MONTH%%</term><description>The program's airing month as a number.</description></item>
            /// <item><term>%%DAY%%</term><description>The program's airing day.</description></item>
            /// <item><term>%%DAYOFWEEK%%</term><description>The program's airing day of the week.</description></item>
            /// <item><term>%%HOURS%%</term><description>The program's airing time's hours (24-hour format).</description></item>
            /// <item><term>%%HOURS12%%</term><description>The program's airing time's hours (12-hour format with AM/PM).</description></item>
            /// <item><term>%%MINUTES%%</term><description>The program's airing time's minutes.</description></item>
            /// </list>
            /// </summary>
            public const string OneTimeRecordingsFileFormat = "OneTimeRecordingsFileFormat";
            /// <summary>
            /// Format to which a finished recording's files are renamed. This format is the default format used for recordings made by a
            /// repeating television schedule and defines the full path and filename (without extension!). The following variables can be used:
            /// <list type="table">
            /// <listheader><term>Variable</term><description>Substituted by</description></listheader>
            /// <item><term>%%CHANNEL%%</term><description>The channel's name.</description></item>
            /// <item><term>%%SCHEDULE%%</term><description>The name of the schedule the program was recorded from.</description></item>
            /// <item><term>%%CATEGORY%%</term><description>The program's category or '#' if it hasn't got one.</description></item>
            /// <item><term>%%TITLE%%</term><description>The program's title (without episode title and/or number).</description></item>
            /// <item><term>%%LONGTITLE%%</term><description>The full program's title with episode title and/or number.</description></item>
            /// <item><term>%%EPISODETITLE%%</term><description>The program's episode title or '#' if it hasn't got one.</description></item>
            /// <item><term>%%EPISODENUMBERDISPLAY%%</term><description>The program's episode number as displayed.</description></item>
            /// <item><term>%%EPISODENUMBER%%</term><description>The program's episode number or '#' if it hasn't got one.</description></item>
            /// <item><term>%%EPISODENUMBER2%%</term><description>The program's episode number in two digits or '00' if it hasn't got one.</description></item>
            /// <item><term>%%EPISODENUMBER3%%</term><description>The program's episode number in three digits or '000' if it hasn't got one.</description></item>
            /// <item><term>%%SERIES%%</term><description>The program's series/season number or '#' if it hasn't got one.</description></item>
            /// <item><term>%%SERIES2%%</term><description>The program's series/season number in two digits or '00' if it hasn't got one.</description></item>
            /// <item><term>%%DATE%%</term><description>The program's airing date in the format yyyy-MM-dd.</description></item>
            /// <item><term>%%YEAR%%</term><description>The program's airing year.</description></item>
            /// <item><term>%%MONTH%%</term><description>The program's airing month as a number.</description></item>
            /// <item><term>%%DAY%%</term><description>The program's airing day.</description></item>
            /// <item><term>%%DAYOFWEEK%%</term><description>The program's airing day of the week.</description></item>
            /// <item><term>%%HOURS%%</term><description>The program's airing time's hours (24-hour format).</description></item>
            /// <item><term>%%HOURS12%%</term><description>The program's airing time's hours (12-hour format with AM/PM).</description></item>
            /// <item><term>%%MINUTES%%</term><description>The program's airing time's minutes.</description></item>
            /// </list>
            /// </summary>
            public const string SeriesRecordingsFileFormat = "SeriesRecordingsFileFormat";
            /// <summary>
            /// Format to which a finished recording's files are renamed. This format is the default format used for recordings made by
            /// a radio schedule and defines the full path and filename (without extension!). The following variables can be used:
            /// <list type="table">
            /// <listheader><term>Variable</term><description>Substituted by</description></listheader>
            /// <item><term>%%CHANNEL%%</term><description>The channel's name.</description></item>
            /// <item><term>%%SCHEDULE%%</term><description>The name of the schedule the program was recorded from.</description></item>
            /// <item><term>%%CATEGORY%%</term><description>The program's category or '#' if it hasn't got one.</description></item>
            /// <item><term>%%TITLE%%</term><description>The program's title (without episode title and/or number).</description></item>
            /// <item><term>%%LONGTITLE%%</term><description>The full program's title with episode title and/or number.</description></item>
            /// <item><term>%%EPISODETITLE%%</term><description>The program's episode title or '#' if it hasn't got one.</description></item>
            /// <item><term>%%EPISODENUMBERDISPLAY%%</term><description>The program's episode number as displayed.</description></item>
            /// <item><term>%%EPISODENUMBER%%</term><description>The program's episode number or '#' if it hasn't got one.</description></item>
            /// <item><term>%%EPISODENUMBER2%%</term><description>The program's episode number in two digits or '00' if it hasn't got one.</description></item>
            /// <item><term>%%EPISODENUMBER3%%</term><description>The program's episode number in three digits or '000' if it hasn't got one.</description></item>
            /// <item><term>%%SERIES%%</term><description>The program's series/season number or '#' if it hasn't got one.</description></item>
            /// <item><term>%%SERIES2%%</term><description>The program's series/season number in two digits or '00' if it hasn't got one.</description></item>
            /// <item><term>%%DATE%%</term><description>The program's airing date in the format yyyy-MM-dd.</description></item>
            /// <item><term>%%YEAR%%</term><description>The program's airing year.</description></item>
            /// <item><term>%%MONTH%%</term><description>The program's airing month as a number.</description></item>
            /// <item><term>%%DAY%%</term><description>The program's airing day.</description></item>
            /// <item><term>%%DAYOFWEEK%%</term><description>The program's airing day of the week.</description></item>
            /// <item><term>%%HOURS%%</term><description>The program's airing time's hours (24-hour format).</description></item>
            /// <item><term>%%HOURS12%%</term><description>The program's airing time's hours (12-hour format with AM/PM).</description></item>
            /// <item><term>%%MINUTES%%</term><description>The program's airing time's minutes.</description></item>
            /// </list>
            /// </summary>
            public const string RadioRecordingsFileFormat = "RadioRecordingsFileFormat";
            /// <summary>
            /// Check for new beta versions in update check (donators only).
            /// </summary>
            public const string IncludeBetaVersionsInUpdateCheck = "IncludeBetaVersionsInUpdateCheck";
            /// <summary>
            /// Date and time the next update check is due.
            /// </summary>
            public const string NextUpdateCheck = "NextUpdateCheck";
            /// <summary>
            /// Information about the most recent online version (in XML).
            /// </summary>
            public const string OnlineVersionInfo = "OnlineVersionInfo";
            /// <summary>
            /// The number of minutes to wake up the system before a scheduled recording or post-processing command.
            /// </summary>
            public const string WakeupBeforeEventMinutes = "WakeupBeforeEventMinutes";
            /// <summary>
            /// Create video thumbnail files for TV recordings (.thmb jpeg files).
            /// </summary>
            public const string CreateVideoThumbnails = "CreateVideoThumbnails";
            /// <summary>
            /// Create .arg metadata files for recordings, even on NTFS volumes.
            /// </summary>
            public const string AlwaysCreateMetadataFiles = "AlwaysCreateMetadataFiles";
            /// <summary>
            /// Number of days to keep old EPG data (in the past). Default is 1, meaning EPG data from two days ago and before is deleted.
            /// </summary>
            public const string KeepOldEpgDataDays = "KeepOldEpgDataDays";
            /// <summary>
            /// The percentage of a recording that needs to be watched (without pre/post-recording) to consider the recording fully watched (default 90).
            /// </summary>
            public const string FullyWatchedPercentage = "FullyWatchedPercentage";
        }

        /// <summary>
        /// Messenger configuration keys.
        /// </summary>
        public static class Messenger
        {
            /// <summary>
            /// The MSN account to use for the bot.
            /// </summary>
            public const string MsnAccount = "MsnAccount";
            /// <summary>
            /// The MSN password to use.
            /// </summary>
            public const string MsnPassword = "MsnPassword";
            /// <summary>
            /// The number of minutes before a programstart that an alert will be send by IMBot.
            /// </summary>
            public const string MinutesBeforeAlert = "MinutesBeforeAlert";
            /// <summary>
            /// The MSN addresses (separated by a ;) to add to the bot's contact-list.
            /// </summary>
            public const string MsnContactList = "MsnContactList";
            /// <summary>
            /// The MSN addresses (separated by a ;) to *not* send alerts to.
            /// </summary>
            public const string MsnAlertFilterList = "MsnAlertFilterList";
            /// <summary>
            /// The MSN addresses (separated by a ;) to *not* send notifications to.
            /// </summary>
            public const string MsnNotificationFilterList = "MsnNotificationFilterList";
        }
    }
}
