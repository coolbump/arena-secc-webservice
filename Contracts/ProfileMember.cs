using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Arena.Core;

namespace Arena.Custom.HDC.WebService.Contracts
{
    /// <summary>
    /// Contains the information that describes a person's status as a
    /// member of a profile.
    /// </summary>
    [DataContract(Namespace = "")]
    public struct ProfileMember
    {
        /// <summary>
        /// Create a new ProfileMember contract that can be sent to
        /// a remote API client.
        /// </summary>
        /// <param name="arena">The Profile object to create a contract for.</param>
        public ProfileMember(Core.ProfileMember arena)
        {
            Profile = new GenericReference(new Core.Profile(arena.ProfileID));
            Person = new GenericReference(arena);
            AttendanceCount = arena.AttendanceCount;
            DateActive = arena.DateActive;
            DateDormant = arena.DateDormant;
            DateInReview = arena.DateInReview;
            DatePending = arena.DatePending;
            MemberNotes = arena.MemberNotes;
            Source = new Lookup(arena.Source);
            Status = new Lookup(arena.Status);
            StatusReason = arena.StatusReason;
        }

        /// <summary>
        /// The Profile that this record pertains to.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public GenericReference Profile;

        /// <summary>
        /// The Person that this record pertains to.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public GenericReference Person;

        /// <summary>
        /// The number of occurrences in this profile that this person
        /// has attended.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int AttendanceCount;

        /// <summary>
        /// The date stamp that this person was made active in this
        /// profile.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime DateActive;

        /// <summary>
        /// The date stamp that this person was made dormant in this
        /// profile.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime DateDormant;

        /// <summary>
        /// The date stamp that this person was marked as in review
        /// in this profile.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime DateInReview;

        /// <summary>
        /// The date stamp that this person was marked as pending in
        /// this profile.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime DatePending;

        /// <summary>
        /// Any notes that have been placed on this person for this
        /// profile.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string MemberNotes;

        /// <summary>
        /// The source of this person, how they got added to the profile.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Lookup Source;

        /// <summary>
        /// The status of this person in this profile.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Lookup Status;

        /// <summary>
        /// The descriptive reason for why the person has the status they
        /// do. This may not always exist as only some status' have a
        /// reason.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string StatusReason;
    }
}
