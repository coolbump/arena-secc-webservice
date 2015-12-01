
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using Arena;
using Arena.Services;

namespace Arena.Custom.HDC.WebService.Contracts
{
    [DataContract(Namespace = "")]
    public class FamilyMember
    {
        [DataMember(EmitDefaultValue = false)]
        public int PersonID { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string FullName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int RoleTypeID { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string RoleTypeValue { get; set; }
    }

    public class FamilyMemberMapper : Arena.Services.Contracts.BaseMapper
    {
        public FamilyMemberMapper()
        {
        }

        public FamilyMember FromArena(Core.FamilyMember arena)
        {
            FamilyMember familyMember = new FamilyMember();

            familyMember.PersonID = arena.PersonID;
            familyMember.FullName = arena.FullName;

            familyMember.RoleTypeID = arena.FamilyRole.LookupID;
            familyMember.RoleTypeValue = arena.FamilyRole.Value;

            return familyMember;
        }
    }
}
