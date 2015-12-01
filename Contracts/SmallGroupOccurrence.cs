
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Arena.SmallGroup;
using Arena.Core;
using Arena.Services;

namespace Arena.Custom.HDC.WebService.Contracts
{
    /// <summary>
    /// Defines the information that is needed to display the attendance of small
    /// group occurrence. 
    /// </summary>
    [DataContract(Namespace = "")]
    public class SmallGroupOccurrence
    {
        /// <summary>
        /// The ID number that identifies this occurrence.
        /// </summary>
        [DataMember()]
        public int OccurrenceID { get; set; }

        /// <summary>
        /// The name of an occurrence.
        /// </summary>
        [DataMember()]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the occurrence.
        /// </summary>
        [DataMember()]
        public string Description { get; set; }

        /// <summary>
        /// The a list of Attendees
        /// </summary>
        [DataMember()]
        public List<GenericReference> Attendees { get; set; }

        /// <summary>
        /// The start date/time of the occurrence
        /// </summary>
        [DataMember()]
        public DateTime Start { get; set; }

        /// <summary>
        /// The end date/time of the occurrence
        /// </summary>
        [DataMember()]
        public DateTime End { get; set; }

    }

    public class SmallGroupOccurrenceMapper
    {
        public SmallGroupOccurrenceMapper()
        {
        }

        /// <summary>
        /// Map an Arena group occurrence to a SmallGroupOccurance Contract
        /// </summary>
        /// <param name="arena">The Occurrence from arena</param>
        /// <returns>A new SmallGroupOccurance</returns>
        public SmallGroupOccurrence FromArena(GroupOccurrence arena)
        {
            SmallGroupOccurrence occurrence = new SmallGroupOccurrence();
            occurrence.OccurrenceID = arena.OccurrenceID;
            occurrence.Name = arena.Name;
            occurrence.Start = arena.StartTime;
            occurrence.End = arena.EndTime;
            occurrence.Description = arena.Description;
            occurrence.Attendees = new List<GenericReference>();
            foreach (var attender in arena.OccurrenceAttendances)
            {
                if (attender.Attended)
                {
                    GenericReference person = new GenericReference(attender.Person);
                    occurrence.Attendees.Add(person);
                }
            }

            return occurrence;
        }

        /// <summary>
        /// Creates a new Small Group Occurrence
        /// </summary>
        /// <param name="id">The GroupId.</param>
        /// <param name="occurrence">The occurrence.</param>
        /// <returns></returns>
        internal Services.Contracts.ModifyResult Create( int id, SmallGroupOccurrence occurrence )
        {
            Services.Contracts.ModifyResult result = new Services.Contracts.ModifyResult();
            Group group = new Group( id );

            if ( group.GroupID <= 0 )
            {
                result.Successful = "False";
                result.ErrorMessage = "Group not found.";
            }

            DayOfWeek todayDOW = DateTime.Now.DayOfWeek;
            DayOfWeek meetingDOW = DayOfWeek.Sunday;

            if ( occurrence.Start == new DateTime( 1900, 1, 1 ) || occurrence.Start == DateTime.MinValue )
            {
                switch ( group.MeetingDay.Value.ToLower() )
                {
                    case "sunday":
                        meetingDOW = DayOfWeek.Sunday;
                        break;
                    case "monday":
                        meetingDOW = DayOfWeek.Monday;
                        break;
                    case "tuesday":
                        meetingDOW = DayOfWeek.Tuesday;
                        break;
                    case "wednesday":
                        meetingDOW = DayOfWeek.Wednesday;
                        break;
                    case "thursday":
                        meetingDOW = DayOfWeek.Thursday;
                        break;
                    case "friday":
                        meetingDOW = DayOfWeek.Friday;
                        break;
                    case "saturday":
                        meetingDOW = DayOfWeek.Saturday;
                        break;
                    default:
                        result.Successful = "False";
                        result.ErrorMessage = "Meeting Date was not provided and could not be determined from the group";
                        return result;
                }

                if ( meetingDOW <= todayDOW )
                {
                    //if meeting day for this week is the current day or a day in the past assume this week's meeting
                    occurrence.Start = new DateTime( DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, group.MeetingStartTime.Hour, group.MeetingStartTime.Minute, group.MeetingStartTime.Second )
                        .AddDays( -( todayDOW - meetingDOW ) );
                }
                else
                {
                    //if meeting day has not occurred yet assume last week's meeting.
                    int daysToSubtract = 7 - meetingDOW - todayDOW;
                    occurrence.Start = new DateTime( DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, group.MeetingStartTime.Hour, group.MeetingStartTime.Minute, group.MeetingStartTime.Second )
                        .AddDays( -daysToSubtract );
                }

                occurrence.End = new DateTime( occurrence.Start.Year, occurrence.Start.Month, occurrence.Start.Day, group.MeetingEndTime.Hour, group.MeetingEndTime.Minute, group.MeetingEndTime.Second );

            }

            if ( group.Occurrences.Where( o => o.StartTime.Date == occurrence.Start.Date ).Count() > 0 )
            {
                result.Successful = "False";
                result.ErrorMessage = string.Format( "Could not create an occurrence.  An occurrence already exists for {0}", occurrence.Start.Date );
                return result;
            }

            GroupOccurrence arenaOccurrence = new GroupOccurrence();
            if ( String.IsNullOrWhiteSpace( occurrence.Name ) )
            {
                arenaOccurrence.Name = string.Format( "{0} Occurrence", group.Name );
            }
            else
            {
                arenaOccurrence.Name = occurrence.Name;
            }

            arenaOccurrence.GroupID = group.GroupID;
            arenaOccurrence.OccurrenceTypeID = 230;
            arenaOccurrence.CheckInStart = new DateTime( 1900, 1, 1 );
            arenaOccurrence.CheckInEnd = new DateTime( 1900, 1, 1 );
            arenaOccurrence.StartTime = occurrence.Start;
            arenaOccurrence.EndTime = occurrence.End;
            //arenaOccurrence.Description = "Occurrence";
            arenaOccurrence.MembershipRequired = false;

            arenaOccurrence.Save( ArenaContext.Current.User.Identity.Name );

            foreach ( var a in occurrence.Attendees )
            {
                OccurrenceAttendance attendance = new OccurrenceAttendance();
                attendance.OccurrenceID = arenaOccurrence.OccurrenceID;
                attendance.PersonID = a.ID;
                attendance.Attended = true;
                attendance.Save( ArenaContext.Current.User.Identity.Name );
            }

            result.Successful = "True";
            return result;


        }

        /// <summary>
        /// Updates an existing Small Group Occurrence
        /// </summary>
        /// <param name="id">The GroupId</param>
        /// <param name="occurrence">The occurrence.</param>
        /// <returns>A ModifyResult object that indicates success or failure of the call.</returns>
        internal Services.Contracts.ModifyResult Update( int id, SmallGroupOccurrence occurrence )
        {
            GroupOccurrence arenaOccurrence = new GroupOccurrence( occurrence.OccurrenceID );
            Services.Contracts.ModifyResult result = new Services.Contracts.ModifyResult();

            if ( arenaOccurrence.GroupID <= 0 )
            {
                result.Successful = "False";
                result.ErrorMessage = "Occurrence was not found.";
            }
            if ( arenaOccurrence.GroupID != id )
            {
                result.Successful = "False";
                result.ErrorMessage = "Occurrence does not belong to the current group.";
                return result;
            }

            arenaOccurrence.Name = occurrence.Name;
            arenaOccurrence.Description = occurrence.Description;
            arenaOccurrence.StartTime = occurrence.Start;
            arenaOccurrence.EndTime = occurrence.End;

            arenaOccurrence.Save( ArenaContext.Current.User.Identity.Name );

            var didNotAttend = arenaOccurrence.OccurrenceAttendances.Attendees
                        .Where( a => a.Attended )
                        .Where( a => !occurrence.Attendees.Select( oa => oa.ID ).Contains( a.PersonID ) )
                        .Select( a => a.PersonID ).ToList();

            var attendeesToAdd = occurrence.Attendees.Where( a => !arenaOccurrence.OccurrenceAttendances.Attendees.Where( aa => aa.Attended )
                                                            .Select( aa => aa.PersonID ).Contains( a.ID ) )
                                                .Select( a => a.ID );


            foreach ( var nonAttendeePersonId in didNotAttend )
            {
                var notTheAttendee = arenaOccurrence.OccurrenceAttendances.Where( a => a.PersonID == nonAttendeePersonId ).FirstOrDefault();

                notTheAttendee.Attended = false;
                notTheAttendee.Save( ArenaContext.Current.User.Identity.Name );
            }

            foreach ( var attendeePersonId in attendeesToAdd )
            {
                var attendee = arenaOccurrence.OccurrenceAttendances.Where( a => a.PersonID == attendeePersonId ).FirstOrDefault();

                if ( attendee != null )
                {
                    attendee.Attended = true;
                }
                else
                {
                    attendee = new OccurrenceAttendance();
                    attendee.OccurrenceID = arenaOccurrence.OccurrenceID;
                    attendee.PersonID = attendeePersonId;
                    attendee.Attended = true;
                }

                attendee.Save( ArenaContext.Current.User.Identity.Name );

            }

            result.Successful = "True";
            return result;
            

        }


    }
}
