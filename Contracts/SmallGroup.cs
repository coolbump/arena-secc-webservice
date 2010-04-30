
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using Arena.SmallGroup;
using Arena.Core;
using Arena.Services;

namespace Arena.Custom.HDC.WebService.Contracts
{
    /// <summary>
    /// Contains the general information about a small group. This
    /// structure conforms to the standard RPC retrieval and update
    /// rules.
    /// </summary>
    [DataContract(Namespace = "")]
    public class SmallGroup
    {
        /// <summary>
        /// The ID number of the small group that this information
        /// pertains to.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int GroupID;

        /// <summary>
        /// The ID number of the parent cluster of this small group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? GroupClusterID;

        /// <summary>
        /// The group category ID of this small group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? CategoryID;

        /// <summary>
        /// The name of this small group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string Name;

        /// <summary>
        /// Flag specifying wether or not this small group is to be
        /// considered active.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public bool? Active;

        /// <summary>
        /// The Level of this small group in the type.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? ClusterLevelID;

        /// <summary>
        /// The TypeID of this small group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? ClusterTypeID;

        /// <summary>
        /// The description that has been associated with this small
        /// group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string Description;

        /// <summary>
        /// A textual description of the schedule for this small
        /// group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string Schedule;

        /// <summary>
        /// The notes about this small group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string Notes;

        /// <summary>
        /// The number of pending registrations for this
        /// small group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? RegistrationCount;

        /// <summary>
        /// The total number of members in this small group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? MemberCount;

        /// <summary>
        /// The person who is considered the leader of this small
        /// group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public GenericReference Leader;

        /// <summary>
        /// The ID number of the area that this small group resides
        /// in.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? AreaID;

        /// <summary>
        /// The small groups custom website URL. This is not the same
        /// as the NavigationUrl.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string GroupUrl;

        /// <summary>
        /// The name of the person that created this small group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string CreatedBy;

        /// <summary>
        /// The date that this small group was created on.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime? DateCreated;

        /// <summary>
        /// The name of the last person to have modified this small
        /// group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string ModifiedBy;

        /// <summary>
        /// The date on which this small group was last modified on.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime? DateModified;

        /// <summary>
        /// The URL to be used for navigating to this small group
        /// in a web browser.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string NavigationUrl;

        /// <summary>
        /// The URL that can be used to retrieve the picture for
        /// this small group, if there is one.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string PictureUrl;

        /// <summary>
        /// The average age of the members of this small group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public double? AverageAge;

        /// <summary>
        /// The average distance from the Target Location of the
        /// members of this small group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public decimal? Distance;

        /// <summary>
        /// The ID number of the address record that identifies the
        /// location at which this small group meets at.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? TargetLocationID;

        /// <summary>
        /// The lookup record which identifies the day(s) of the week
        /// that this small group meets on.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Lookup MeetingDay;

        /// <summary>
        /// The lookup record which identifies the primary age range
        /// of this small group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Lookup PrimaryAge;

        /// <summary>
        /// The lookup record which identifies the suggested marital
        /// status of this small group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Lookup PrimaryMaritalStatus;

        /// <summary>
        /// The lookup record which identifies the topic of discussion
        /// for this small group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Lookup Topic;

        /// <summary>
        /// The maximum number of members that should be allowed in
        /// this small group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int MaxMembers;

        /// <summary>
        /// Determines whether or not this small group is private or not.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public bool Private;
    }

    public class SmallGroupMapper : Arena.Services.Contracts.BaseMapper
    {
        public SmallGroupMapper()
        {
        }

        public SmallGroup FromArena(Group arena)
        {
            SmallGroup group = new SmallGroup();
            LookupMapper lMapper = new LookupMapper();


            group.GroupID = arena.GroupID;
            group.Active = arena.Active;
            group.AreaID = arena.AreaID;
            group.AverageAge = arena.AverageAge;
            group.CategoryID = arena.ClusterType.CategoryID;
            group.GroupClusterID = arena.GroupClusterID;
            group.CreatedBy = arena.CreatedBy;
            group.DateCreated = arena.DateCreated;
            group.DateModified = arena.DateModified;
            group.Description = arena.Description;
            group.Distance = arena.Distance;
            group.Leader = new GenericReference(arena.Leader);
            group.ClusterLevelID = arena.ClusterLevelID;
            group.MaxMembers = arena.MaxMembers;
            group.MeetingDay = lMapper.FromArena(arena.MeetingDay);
            group.MemberCount = arena.Members.Count;
            group.ModifiedBy = arena.ModifiedBy;
            group.Name = arena.Name;
            group.NavigationUrl = arena.NavigationUrl;
            group.Notes = arena.Notes;
            group.PictureUrl = arena.PictureUrl;
            group.PrimaryAge = lMapper.FromArena(arena.PrimaryAge);
            group.PrimaryMaritalStatus = lMapper.FromArena(arena.PrimaryMaritalStatus);
            group.Private = arena.Private;
            group.RegistrationCount = arena.RegistrationCount;
            group.Schedule = arena.Schedule;
            group.TargetLocationID = arena.TargetLocationID;
            group.Topic = lMapper.FromArena(arena.Topic);
            group.ClusterTypeID = arena.ClusterTypeID;
            group.GroupUrl = arena.GroupUrl;

            return group;
        }
    }
}
