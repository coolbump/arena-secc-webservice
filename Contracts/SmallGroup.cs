
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using Arena.SmallGroup;
using Arena.Core;
using Arena.Services;
using System.Security.Cryptography;

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
        public int GroupID { get; set; }

        /// <summary>
        /// The ID number of the parent cluster of this small group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? GroupClusterID { get; set; }

        /// <summary>
        /// The group category ID of this small group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? CategoryID { get; set; }

        /// <summary>
        /// The name of this small group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string Name { get; set; }

        /// <summary>
        /// Flag specifying wether or not this small group is to be
        /// considered active.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public bool? Active { get; set; }

        /// <summary>
        /// The Level of this small group in the type.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? ClusterLevelID { get; set; }

        /// <summary>
        /// The TypeID of this small group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? ClusterTypeID { get; set; }

        /// <summary>
        /// The description that has been associated with this small
        /// group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string Description { get; set; }

        /// <summary>
        /// A textual description of the schedule for this small
        /// group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string Schedule { get; set; }

        /// <summary>
        /// The notes about this small group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string Notes { get; set; }

        /// <summary>
        /// The number of pending registrations for this
        /// small group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? RegistrationCount { get; set; }

        /// <summary>
        /// The total number of members in this small group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? MemberCount { get; set; }

        /// <summary>
        /// The person who is considered the leader of this small
        /// group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public GenericReference Leader { get; set; }

        /// <summary>
        /// The ID number of the area that this small group resides
        /// in.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? AreaID { get; set; }

        /// <summary>
        /// The small groups custom website URL. This is not the same
        /// as the NavigationUrl.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string GroupUrl { get; set; }

        /// <summary>
        /// The name of the person that created this small group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string CreatedBy { get; set; }

        /// <summary>
        /// The date that this small group was created on.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime? DateCreated { get; set; }

        /// <summary>
        /// The name of the last person to have modified this small
        /// group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string ModifiedBy { get; set; }

        /// <summary>
        /// The date on which this small group was last modified on.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime? DateModified { get; set; }

        /// <summary>
        /// The URL to be used for navigating to this small group
        /// in a web browser.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string NavigationUrl { get; set; }

        /// <summary>
        /// The URL that can be used to retrieve the picture for
        /// this small group, if there is one.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string PictureUrl { get; set; }

        /// <summary>
        /// The average age of the members of this small group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public double? AverageAge { get; set; }

        /// <summary>
        /// The average distance from the Target Location of the
        /// members of this small group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public decimal? Distance { get; set; }

        /// <summary>
        /// The ID number of the address record that identifies the
        /// location at which this small group meets at.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? TargetLocationID { get; set; }

        /// <summary>
        /// The lookup record which identifies the day(s) of the week
        /// that this small group meets on.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Lookup MeetingDay { get; set; }

        /// <summary>
        /// The lookup record which identifies the primary age range
        /// of this small group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Lookup PrimaryAge { get; set; }

        /// <summary>
        /// The lookup record which identifies the suggested marital
        /// status of this small group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Lookup PrimaryMaritalStatus { get; set; }

        /// <summary>
        /// The lookup record which identifies the topic of discussion
        /// for this small group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Lookup Topic { get; set; }

        /// <summary>
        /// The maximum number of members that should be allowed in
        /// this small group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int MaxMembers { get; set; }

        /// <summary>
        /// Determines whether or not this small group is private or not.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public bool Private { get; set; }
    }

    public class SmallGroupMapper : Arena.Services.Contracts.BaseMapper
    {
        private List<string> _includeFields;


        public SmallGroupMapper()
        {
        }


        public SmallGroupMapper(List<string> includeFields)
        {
            if (includeFields != null)
            {
                // Normalize for search
                _includeFields = new List<string>();
                foreach (string value in includeFields)
                    _includeFields.Add(value.ToUpperInvariant());
            }
        }


        private bool ShouldShow(string name)
        {
            if (_includeFields == null)
                return true;

            return _includeFields.Contains(name.ToUpperInvariant());
        }


        public SmallGroup FromArena(Group arena)
        {
            SmallGroup group = new SmallGroup();
            LookupMapper lMapper = new LookupMapper();


            if (ShouldShow("GroupID") == true)
                group.GroupID = arena.GroupID;

            if (ShouldShow("Active") == true)
                group.Active = arena.Active;

            if (ShouldShow("AreaID") == true)
                group.AreaID = arena.AreaID;

            if (ShouldShow("AverageAge") == true)
                group.AverageAge = arena.AverageAge;

            if (ShouldShow("CategoryID") == true)
                group.CategoryID = arena.ClusterType.CategoryID;

            if (ShouldShow("GroupClusterID") == true)
                group.GroupClusterID = arena.GroupClusterID;

            if (ShouldShow("CreatedBy") == true)
                group.CreatedBy = arena.CreatedBy;

            if (ShouldShow("DateCreated") == true)
                group.DateCreated = arena.DateCreated;

            if (ShouldShow("DateModified") == true)
                group.DateModified = arena.DateModified;

            if (ShouldShow("Description") == true)
                group.Description = arena.Description;

            if (ShouldShow("Distance") == true)
                group.Distance = arena.Distance;

            if (ShouldShow("Leader") == true)
                group.Leader = new GenericReference(arena.Leader);

            if (ShouldShow("ClusterLevelID") == true)
                group.ClusterLevelID = arena.ClusterLevelID;

            if (ShouldShow("MaxMembers") == true)
                group.MaxMembers = arena.MaxMembers;

            if (ShouldShow("MeetingDay") == true)
                group.MeetingDay = lMapper.FromArena(arena.MeetingDay);

            if (ShouldShow("MemberCount") == true)
                group.MemberCount = arena.Members.Count;

            if (ShouldShow("Modifiedby") == true)
                group.ModifiedBy = arena.ModifiedBy;

            if (ShouldShow("Name") == true)
                group.Name = arena.Name;

            if (ShouldShow("NavigationUrl") == true)
                group.NavigationUrl = arena.NavigationUrl;

            if (ShouldShow("Notes") == true)
                group.Notes = arena.Notes;

            if (ShouldShow("PictureUrl") == true) {
                if (arena.ImageBlob != null && arena.ImageBlob.BlobID > 0) {
                    group.PictureUrl = getImageThumbnailUrl(arena.ImageBlob);
                }
            }
            
            if (ShouldShow("PrimaryAge") == true)
                group.PrimaryAge = lMapper.FromArena(arena.PrimaryAge);

            if (ShouldShow("PrimaryMaritalStatus") == true)
                group.PrimaryMaritalStatus = lMapper.FromArena(arena.PrimaryMaritalStatus);

            if (ShouldShow("Private") == true)
                group.Private = arena.Private;

            if (ShouldShow("RegistrationCount") == true)
                group.RegistrationCount = arena.RegistrationCount;

            if (ShouldShow("Schedule") == true)
                group.Schedule = arena.Schedule;

            if (ShouldShow("TargetLocationID") == true)
                group.TargetLocationID = arena.TargetLocationID;

            if (ShouldShow("Topic") == true)
                group.Topic = lMapper.FromArena(arena.Topic);

            if (ShouldShow("ClusterTypeID") == true)
                group.ClusterTypeID = arena.ClusterTypeID;

            if (ShouldShow("GroupUrl") == true)
                group.GroupUrl = arena.GroupUrl;

            return group;
        }

        public static String getImageThumbnailUrl(Utility.ArenaImage blob)
        {
            /*
            // This is used for ensuring we have a different URL for each image to
            // workaround caches
            byte[] hash = MD5.Create().ComputeHash(blob.ByteArray);
            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return blob.ThumbnailUrl(200, 200, 200) + "&" + sb.ToString();
            */
            return blob.ThumbnailUrl(200, 200, 200);
        }
    }
}
