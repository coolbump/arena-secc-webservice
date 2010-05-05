
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using Arena;
using Arena.Services;

namespace Arena.Custom.HDC.WebService.Contracts
{
    [DataContract(Namespace = "")]
    public class PersonRelationship
    {
        [DataMember(EmitDefaultValue = false)]
        public int PersonID { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string FullName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int RelatedPersonID { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string RelatedFullName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public int RelationshipTypeID { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string RelationshipTypeValue { get; set; }
    }

    public class PersonRelationshipMapper : Arena.Services.Contracts.BaseMapper
    {
        public PersonRelationshipMapper()
        {
        }

        public PersonRelationship FromArena(Core.Relationship arena)
        {
            PersonRelationship relationship = new PersonRelationship();


            relationship.PersonID = arena.PersonId;
            relationship.FullName = arena.Person.FullName;
            relationship.RelatedPersonID = arena.RelatedPersonId;
            relationship.RelatedFullName = arena.RelatedPerson.FullName;

            relationship.RelationshipTypeID = arena.RelationshipTypeId;
            relationship.RelationshipTypeValue = arena.RelationshipType.Relationship;

            return relationship;
        }
    }
}
