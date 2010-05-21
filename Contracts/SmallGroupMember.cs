
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
        public int PersonID;

        /// <summary>
        /// The name to be used when displaying the member's name in a list.
        /// </summary>
        [DataMember()]
        public string FullName;

        /// <summary>
        /// The small group that this membership information is for.
        /// </summary>
        [DataMember()]
        public GenericReference Group;

        /// <summary>
        /// Identifies if this member is active in the small group.
        /// </summary>
        [DataMember()]
        public bool Active;

        /// <summary>
        /// The role of this member in the small group.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public Lookup Role;

        /// <summary>
        /// The uniform number if the small group supports uniform numbers.
        /// </summary>
        [DataMember()]
        public int UniformNumber;
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
            member.Role = lMapper.FromArena(arena.Role);
            member.Group = new GenericReference(g);
            if (g.ClusterType.Category.UseUniformNumber == true)
                member.UniformNumber = arena.UniformNumber;

            return member;
        }
    }
}
