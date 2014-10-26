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
    /// All the possible schedule rules.
    /// </summary>
    /// <seealso cref="ScheduleTime"/>
    /// <seealso cref="ScheduleDaysOfWeek"/>
    public enum ScheduleRuleType
    {
        /// <summary>
        /// OBSOLETE! Don't use this rule anymore, it's only here for backwards compatibility.
        /// </summary>
        TvChannels,
        /// <summary>
        /// Record on the given channel(s). Arguments are one or more channel IDs.
        /// </summary>
        Channels,
        /// <summary>
        /// Do not record on the given channel(s). Arguments are one or more channel IDs.
        /// </summary>
        NotOnChannels,
        /// <summary>
        /// Record the timeslot closest to the given start time (usually combined with TitleEquals).
        /// </summary>
        AroundTime,
        /// <summary>
        /// Record a program when its start time is between the two given times.
        /// </summary>
        StartingBetween,
        /// <summary>
        /// Record on this specific date (mutually exclusive with DaysOfWeek).
        /// </summary> 
        OnDate,
        /// <summary>
        /// Record on these days of the week (mutually exclusive with OnDate),
        /// with second optional starting date argument.
        /// </summary> 
        DaysOfWeek,
        /// <summary>
        /// Record program with the given title.
        /// </summary> 
        TitleEquals,
        /// <summary>
        /// Record program with the given sub-title.
        /// </summary> 
        SubTitleEquals,
        /// <summary>
        /// Record program who's sub-title starts with the given text.
        /// </summary> 
        SubTitleStartsWith,
        /// <summary>
        /// Record program who's sub-title contains the given text. If more than one text
        /// is given an OR will be applied.
        /// </summary> 
        SubTitleContains,
        /// <summary>
        /// Record program who's sub-title does not contain the given text, only valid in
        /// combination with at least one SubTitleContains rule.
        /// </summary> 
        SubTitleDoesNotContain,
        /// <summary>
        /// Record program with the given episode number.
        /// </summary> 
        EpisodeNumberEquals,
        /// <summary>
        /// Record program who's episode number contains the given text. If more than one text
        /// is given an OR will be applied.
        /// </summary>
        EpisodeNumberContains,
        /// <summary>
        /// Record program who's episode number does not contain the given text, only valid in
        /// combination with at least one EpisodeNumberContains rule. 
        /// </summary>
        EpisodeNumberDoesNotContain,
        /// <summary>
        /// Record program who's episode number starts with the given text.
        /// </summary>
        EpisodeNumberStartsWith,
        /// <summary>
        /// Record program who's title starts with the given text.
        /// </summary> 
        TitleStartsWith,
        /// <summary>
        /// Record program who's title contains the given text. If more than one text
        /// is given an OR will be applied.
        /// </summary> 
        TitleContains,
        /// <summary>
        /// Record program who's title does not contain the given text, only valid in
        /// combination with at least one TitleContains rule.
        /// </summary> 
        TitleDoesNotContain,
        /// <summary>
        /// Record program who's description contains the given text. If more than one text
        /// is given an OR will be applied.
        /// </summary> 
        DescriptionContains,
        /// <summary>
        /// Record program who's description does not contain the given text, only valid in
        /// combination with at least one DescriptionContains rule.
        /// </summary> 
        DescriptionDoesNotContain,
        /// <summary>
        /// Record programs from any of the categories given as arguments.
        /// </summary> 
        CategoryEquals,
        /// <summary>
        /// Don't record programs in any of the categories given as arguments.
        /// </summary> 
        CategoryDoesNotEqual,
        /// <summary>
        /// Record program directed by given name.
        /// </summary> 
        DirectedBy,
        /// <summary>
        /// Record program which has the actor by the given name.
        /// </summary> 
        WithActor,
        /// <summary>
        /// Only record first-run (non-repeat) programs if the argument to this rule is true.
        /// </summary> 
        SkipRepeats,
        /// <summary>
        /// If this is true, don't include programs that were already recorded before (based on title/sub-title/episode number).
        /// </summary> 
        NewEpisodesOnly,
        /// <summary>
        /// If this is true, don't include programs that were already recorded before (based on title only).
        /// </summary> 
        NewTitlesOnly,
        /// <summary>
        /// If this rule is added the schedule is a manual schedule with the given datetime
        /// and duration (ScheduleTime) arguments.  When this rule is specified the TvChannels
        /// rule must be present with exactly one channel.  The DaysOfWeek rule with one argument
        /// is optional, and no other rules are allowed.
        /// </summary> 
        ManualSchedule,
        /// <summary>
        /// Record program who's title, episode or description contains the given text. If more than one text
        /// is given an OR will be applied.
        /// </summary> 
        ProgramInfoContains,
        /// <summary>
        /// Record program who's title, episode or description does not contain the given text,
        /// only valid in combination with at least one ProgramInfoContains rule.
        /// </summary> 
        ProgramInfoDoesNotContain
    }
}
