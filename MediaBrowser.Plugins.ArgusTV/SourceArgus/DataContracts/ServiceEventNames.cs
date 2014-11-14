using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArgusTV.DataContracts
{
    /// <summary>
    /// A polling event from the ARGUS TV Scheduler service.
    /// </summary>
    public static class ServiceEventNames
    {
        /// <summary>
        /// No arguments.
        /// </summary>
        public const string EnteringStandby = "EnteringStandby";

        /// <summary>
        /// No arguments.
        /// </summary>
        public const string SystemResumed = "SystemResumed";

        /// <summary>
        /// Two arguments: module name and key, both strings.
        /// </summary>
        public const string ConfigurationChanged = "ConfigurationChanged";

        /// <summary>
        /// No arguments.
        /// </summary>
        public const string NewGuideData = "NewGuideData";

        /// <summary>
        /// Two arguments: the ID of the schedule that was changed -- first Guid, second int.
        /// </summary>
        public const string ScheduleChanged = "ScheduleChanged";

        /// <summary>
        /// No arguments.
        /// </summary>
        public const string UpcomingRecordingsChanged = "UpcomingRecordingsChanged";

        /// <summary>
        /// No arguments.
        /// </summary>
        public const string UpcomingAlertsChanged = "UpcomingAlertsChanged";

        /// <summary>
        /// No arguments.
        /// </summary>
        public const string UpcomingSuggestionsChanged = "UpcomingSuggestionsChanged";

        /// <summary>
        /// No arguments.
        /// </summary>
        public const string ActiveRecordingsChanged = "ActiveRecordingsChanged";

        /// <summary>
        /// One arguments: the recording that started.
        /// </summary>
        public const string RecordingStarted = "RecordingStarted";

        /// <summary>
        /// One arguments: the recording that ended.
        /// </summary>
        public const string RecordingEnded = "RecordingEnded";

        /// <summary>
        /// One arguments: the live stream that started.
        /// </summary>
        public const string LiveStreamStarted = "LiveStreamStarted";

        /// <summary>
        /// One arguments: the live stream that was tuned.
        /// </summary>
        public const string LiveStreamTuned = "LiveStreamTuned";

        /// <summary>
        /// One arguments: the live stream that ended.
        /// </summary>
        public const string LiveStreamEnded = "LiveStreamEnded";

        /// <summary>
        /// Three arguments: the live stream that was aborted, the LiveStreamAbortReason and the UpcomingProgram that was the cause of the abort.
        /// </summary>
        public const string LiveStreamAborted = "LiveStreamAborted";
    }
}
