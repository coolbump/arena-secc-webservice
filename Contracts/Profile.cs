using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Arena.Custom.HDC.WebService.Contracts
{
    /// <summary>
    /// This structure contains the basic information about a profile.
    /// This structure follows the standard RPC retrieval and updating
    /// rules.
    /// </summary>
    [DataContract(Namespace = "")]
    public class Profile
    {
        /// <summary>
        /// Profile ID this information is referencing.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int ProfileID;

        /// <summary>
        /// The parent profile ID or -1 if this is a root profile.
        /// Note: Is that correct? Need to check on that.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int ParentID;

        /// <summary>
        /// The name of this profile.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string Name;

        /// <summary>
        /// The string representation of the ProfileType enum identifying
        /// if this is a Personal, Ministry, Serving or Event tag.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string ProfileTypeString;

        /// <summary>
        /// The numeric representation of the ProfileType enum identifying
        /// if this is a Personal, Ministry, Serving or Event tag.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int ProfileType;

        /// <summary>
        /// Specifies wether or not this profile is currently marked as
        /// being active.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public bool Active;

        /// <summary>
        /// The number of active members of this profile, not including
        /// child profiles.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int ProfileActiveMemberCount;

        /// <summary>
        /// The total number of members of this profile, not including
        /// child profiles.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int ProfileMemberCount;

        /// <summary>
        /// The Campus ID this profile is to the associated with.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int CampusID = -1;

        /// <summary>
        /// User entered notes about this profile.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string Notes;

        /// <summary>
        /// The person that owns this profile.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public GenericReference Owner;

        /// <summary>
        /// The person login that created this profile.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string CreatedBy;

        /// <summary>
        /// The date and time this profile as initially created.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime DateCreated;

        /// <summary>
        /// The person login who last modified this profile.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string ModifiedBy;

        /// <summary>
        /// The date and time that this profile was last modified.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime DateModified;

        /// <summary>
        /// The number of critical members of this profile and all
        /// descendents.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int CriticalMembers;

        /// <summary>
        /// The number of active members of this profile and all
        /// descendents.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int ActiveMembers;

        /// <summary>
        /// The number of members in review for this profile and all
        /// descendents.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int InReviewMembers;

        /// <summary>
        /// The number of members who have not yet been contacted for
        /// this profile and all descendents.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int NoContactMembers;

        /// <summary>
        /// The number of pending members of this profile and all
        /// descendents.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int PendingMembers;

        /// <summary>
        /// The total number of members of this profile and all
        /// descendents.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int TotalMembers;

        /// <summary>
        /// The URL that can be used to view this profile in the Arena
        /// portal.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string NavigationUrl;

        /// <summary>
        /// The strength of the relationship between the owner and
        /// members of this profile.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int OwnerRelationshipStrength;

        /// <summary>
        /// The strength of the relationship between members of this
        /// profile.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int PeerRelationshipStrength;
    }

    public class ProfileMapper : Arena.Services.Contracts.BaseMapper
    {
        private List<string> _includeFields;


        public ProfileMapper()
        {
        }


        public ProfileMapper(List<string> includeFields)
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
            bool status = false;

            if (_includeFields == null)
                return true;

            if (_includeFields.Contains("*"))
            {
                status = true;

                if (_includeFields.Contains(String.Concat("-", name.ToUpperInvariant())))
                    status = false;
            }
            else if (_includeFields.Contains(name.ToUpperInvariant()))
                status = true;

            return status;
        }


        public Profile FromArena(Core.Profile arena)
        {
            Profile profile = new Profile();


            if (ShouldShow("ProfileID") == true)
                profile.ProfileID = arena.ProfileID;

            if (ShouldShow("ParentID") == true)
                profile.ParentID = arena.ParentProfileID;

            if (ShouldShow("Name") == true)
                profile.Name = arena.Name;

            if (ShouldShow("ProfileType") == true)
                profile.ProfileType = (int)arena.ProfileType;

            if (ShouldShow("ProfileTypeString") == true)
                profile.ProfileTypeString = arena.ProfileType.ToString();

            if (ShouldShow("Active") == true)
                profile.Active = arena.Active;

            if (ShouldShow("ProfileActiveMemberCount") == true)
                profile.ProfileActiveMemberCount = arena.ProfileActiveMemberCount;

            if (ShouldShow("ProfileMemberCount") == true)
                profile.ProfileMemberCount = arena.ProfileMemberCount;

            if (ShouldShow("CampusID") == true && arena.Campus != null)
                profile.CampusID = arena.Campus.CampusId;

            if (ShouldShow("Notes") == true)
                profile.Notes = arena.Notes;

            if (ShouldShow("OwnerID") == true)
                profile.Owner = new GenericReference(arena.Owner);

            if (ShouldShow("CreatedBy") == true)
                profile.CreatedBy = arena.CreatedBy;

            if (ShouldShow("DateCreated") == true)
                profile.DateCreated = arena.DateCreated;

            if (ShouldShow("ModifiedBy") == true)
                profile.ModifiedBy = arena.ModifiedBy;

            if (ShouldShow("DateModified") == true)
                profile.DateModified = arena.DateModified;

            if (ShouldShow("CriticalMembers") == true)
                profile.CriticalMembers = arena.CriticalMembers;

            if (ShouldShow("ActiveMembers") == true)
                profile.ActiveMembers = arena.ActiveMembers;

            if (ShouldShow("InReviewMembers") == true)
                profile.InReviewMembers = arena.InReviewMembers;

            if (ShouldShow("NoContactMembers") == true)
                profile.NoContactMembers = arena.NoContactMembers;

            if (ShouldShow("PendingMembers") == true)
                profile.PendingMembers = arena.PendingMembers;

            if (ShouldShow("TotalMembers") == true)
                profile.TotalMembers = arena.TotalMembers;

            if (ShouldShow("NavigationUrl") == true)
                profile.NavigationUrl = arena.NavigationUrl;

            if (ShouldShow("OwnerRelationshipStrength") == true)
                profile.OwnerRelationshipStrength = arena.OwnerRelationshipStrength;

            if (ShouldShow("PeerRelationshipStrength") == true)
                profile.PeerRelationshipStrength = arena.PeerRelationshipStrength;

            return profile;
        }
    }
}
