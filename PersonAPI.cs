
using System;
using System.Collections.Generic;
using System.Configuration;
using System.ServiceModel.Web;
using Arena.Core;
using Arena.Security;
using Arena.SmallGroup;

namespace Arena.Custom.HDC.WebService
{
    class PersonAPI
    {
        /// <summary>
        /// Retrieve a list of all group categories in the system. If, by chance,
        /// no categories exist then an empty array is returned.
        /// </summary>
        /// <returns>Integer array of group categoryIDs.</returns>
        [WebGet(UriTemplate = "person/{id}/relationships")]
        public Contracts.GenericListResult<Contracts.PersonRelationship> GetPersonRelationships(int id)
        {
            Contracts.GenericListResult<Contracts.PersonRelationship> list = new Contracts.GenericListResult<Contracts.PersonRelationship>();
            Contracts.PersonRelationshipMapper mapper = new Contracts.PersonRelationshipMapper();
            RelationshipCollection relationships = new RelationshipCollection(id);


            list.Items = new List<Contracts.PersonRelationship>();
            list.Total = relationships.Count;
            list.Max = list.Total;
            list.Start = 0;
            foreach (Relationship relationship in relationships)
            {
                list.Items.Add(mapper.FromArena(relationship));
            }

            return list;
        }
    }
}
