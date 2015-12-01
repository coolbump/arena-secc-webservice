
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using Arena.SmallGroup;
using Arena.Core;
using Arena.Services;
using AutoMapper;

namespace Arena.Custom.HDC.WebService.Contracts
{
    /// <summary>
    /// Defines an event
    /// </summary>
    [DataContract(Namespace = "")]
    public class Event
    {
        /// <summary>
        /// The ID number that identifies this event.
        /// </summary>
        [DataMember()]
        public string Id { get; set; }

        /// <summary>
        /// The contact email for the event
        /// </summary>
        [DataMember()]
        public string ContactEmail { get; set; }

        /// <summary>
        /// The contact person's name for the event
        /// </summary>
        [DataMember()]
        public string ContactName { get; set; }

        /// <summary>
        /// The contact phone number
        /// </summary>
        [DataMember()]
        public string ContactPhone { get; set; }

        /// <summary>
        /// The DateRange of the event
        /// </summary>
        [DataMember()]
        public string DateRangeString { get; set; }

        /// <summary>
        /// Label for the description
        /// </summary>
        [DataMember()]
        public string DescriptionLabel { get; set; }

        /// <summary>
        /// Details for the event
        /// </summary>
        [DataMember()]
        public string Details { get; set; }

        /// <summary>
        /// End date for the event
        /// </summary>
        [DataMember()]
        public DateTime End { get; set; }

        /// <summary>
        /// Link for the event
        /// </summary>
        [DataMember()]
        public string ExternalLink { get; set; }

        /// <summary>
        /// Image URL
        /// </summary>
        [DataMember()]
        public String Image { get; set; }

        /// <summary>
        /// Location Name where the event is being hosted
        /// </summary>
        [DataMember()]
        public String Location { get; set; }

        /// <summary>
        /// Id for the location
        /// </summary>
        [DataMember()]
        public int LocationId { get; set; }

        /// <summary>
        /// Array of messages pertaining to the event
        /// </summary>
        [DataMember()]
        public string[] Messages { get; set;  }

        /// <summary>
        /// Number of projected attendees
        /// </summary>
        [DataMember()]
        public int ProjectedAttendance { get; set; }

        /// <summary>
        /// Boolean indicating whether or not registration is active
        /// </summary>
        [DataMember()]
        public bool RegistrationActive { get; set; }

        /// <summary>
        /// Image URL for registration
        /// </summary>
        [DataMember()]
        public String RegistrationImage { get; set; }

        /// <summary>
        /// Instructions for registration 
        /// </summary>
        [DataMember()]
        public string RegistrationInstructions { get; set; }

        /// <summary>
        /// Label for registration
        /// </summary>
        [DataMember()]
        public string RegistrationLabel { get; set; }

        /// <summary>
        /// Boolean indicating that registration is limited
        /// </summary>
        [DataMember()]
        public bool RegistrationLimited { get; set; }

        /// <summary>
        /// Maximum number of individuals
        /// </summary>
        [DataMember()]
        public int RegistrationMaximumIndividuals { get; set; }

        /// <summary>
        /// Maximum number of registrations
        /// </summary>
        [DataMember()]
        public int RegistrationMaximumRegistrants { get; set; }

        /// <summary>
        /// Start date for registration
        /// </summary>
        [DataMember()]
        public DateTime RegistrationStart { get; set; }

        /// <summary>
        /// Start date for the event
        /// </summary>
        [DataMember()]
        public DateTime Start { get; set; }

        /// <summary>
        /// Title of the event
        /// </summary>
        [DataMember()]
        public string Title { get; set; }

        /// <summary>
        /// Topic area of the event
        /// </summary>
        [DataMember()]
        public Lookup TopicArea { get; set; }

        /// <summary>
        /// Total required cost and fees for an event
        /// </summary>
        [DataMember()]
        public decimal TotalRequiredCostsAndFees { get; set; }

        /// <summary>
        /// The type of the event
        /// </summary>
        [DataMember()]
        public Lookup Type { get; set; }

        /// <summary>
        /// The visibility of the event
        /// </summary>
        [DataMember()]
        public Lookup VisibilityType { get; set; }
    }

    public class EventMapper
    {
        public EventMapper()
        {
            Mapper.CreateMap<Arena.Core.Lookup, Contracts.Lookup>();

            Mapper.CreateMap<Arena.Event.EventProfile, Event>()
                .ForMember(dest => dest.Location, opt => opt.ResolveUsing(src => src.Location != null ? src.Location.LocationName : null))
                .ForMember(dest => dest.Id, opt => opt.ResolveUsing(src => src.ProfileID));
        }

        public Event FromArena(Arena.Event.EventProfile arena)
        {
            Event myEvent = Mapper.Map<Event>(arena);
            return myEvent;
        }
    }
}
