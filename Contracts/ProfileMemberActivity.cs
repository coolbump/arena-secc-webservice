using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.Serialization;
using System.Text;

namespace Arena.Custom.HDC.WebService.Contracts
{
    /// <summary>
    /// This class provides a contract to pass a single activity record
    /// of a person's profile membership .
    /// </summary>
    [DataContract(Namespace = "")]
    class ProfileMemberActivity
    {
        /// <summary>
        /// Create a new ProfileMemberActivity object given the contents
        /// of the DataRow, this is the designated initializer.
        /// </summary>
        /// <param name="dr">The DataRow as returned by GetProfileMemberActivityDetails_DT().</param>
        public ProfileMemberActivity(DataRow dr)
        {
            Profile = new GenericReference(new Core.Profile(Convert.ToInt32(dr["profile_id"])));
            Person = new GenericReference(new Core.Person(Convert.ToInt32(dr["person_id"])));
            ActivityTypeString = dr["activity_type"].ToString();
            CreatedBy = dr["created_by"].ToString();
            DateCreated = Convert.ToDateTime(dr["date_created"].ToString());
            Notes = dr["notes"].ToString();
        }

        /// <summary>
        /// A reference to the profile that this record pertains to.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public GenericReference Profile;

        /// <summary>
        /// A reference to the person that this record pertains to.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public GenericReference Person;

        /// <summary>
        /// The textual representation of the activity lookup type. There is
        /// currently no way to retrieve the lookup ID number.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String ActivityTypeString;

        /// <summary>
        /// Who created this activity.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String CreatedBy;

        /// <summary>
        /// When the activity was created (activity cannot be modified).
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime DateCreated;

        /// <summary>
        /// The notes associated with this activity.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public String Notes;
    }
}
