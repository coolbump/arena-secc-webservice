
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
    /// Defines the information that is needed to display a list of small
    /// group members. 
    /// </summary>
    [DataContract(Namespace = "")]
    public class SmallGroupMember
    {
        /// <summary>
        /// The ID number that identifies this small group member. This ID
        /// number can be used to request more detailed information about
        /// the member as it is a standard PersonID.
        /// </summary>
        [DataMember()]
        public int PersonID { get; set; }

        /// <summary>
        /// The name to be used when displaying the member's name in a list.
        /// </summary>
        [DataMember()]
        public string FullName { get; set; }

        /// <summary>
        /// The primary email address of this group member.
        /// </summary>
        [DataMember()]
        public string PrimaryEmail { get; set; }

        /// <summary>
        /// The cell phone number of this group member.
        /// </summary>
        [DataMember()]
        public string CellPhone { get; set; }

        /// <summary>
        /// The home phone number of this group member.
        /// </summary>
        [DataMember()]
        public string HomePhone { get; set; }

        /// <summary>
        /// The small group that this membership information is for.
        /// </summary>
        [DataMember()]
        public GenericReference Group { get; set; }

        /// <summary>
        /// Identifies if this member is active in the small group.
        /// </summary>
        [DataMember()]
        public bool Active { get; set; }

        /// <summary>
        /// The role of this member in the small group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Lookup Role { get; set; }

        /// <summary>
        /// The uniform number if the small group supports uniform numbers.
        /// </summary>
        [DataMember()]
        public int UniformNumber { get; set; }
    }

    public class SmallGroupMemberMapper
    {
        public SmallGroupMemberMapper()
        {
        }

        public SmallGroupMember FromArena(GroupMember arena)
        {
            SmallGroupMember member = new SmallGroupMember();
            LookupMapper lMapper = new LookupMapper();
            Group g = new Group(arena.GroupID);

            member.PersonID = arena.PersonID;
            member.FullName = arena.FullName;
            member.Active = arena.Active;
            member.PrimaryEmail = arena.Emails.FirstActive;
            Core.PersonPhone phone = arena.Phones.FindByType(Arena.Core.SystemLookup.PhoneType_Cell);
            if (phone != null) {
                member.CellPhone = phone.Number;
            }
            phone = arena.Phones.FindByType(Arena.Core.SystemLookup.PhoneType_Home);
            if (phone != null) {
                member.HomePhone = phone.Number;
            }

            member.Role = lMapper.FromArena(arena.Role);
            member.Group = new GenericReference(g);
            if (g.ClusterType.Category.UseUniformNumber == true)
                member.UniformNumber = arena.UniformNumber;

            return member;
        }
    }
}
