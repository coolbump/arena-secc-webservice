
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
        /// Retrieve a list of all person relationships for the given person ID.
        /// </summary>
        /// <returns>List of PersonRelationship objects.</returns>
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


        /// <summary>
        /// Retrieve a list of all small group membership for the person.
        /// Security is taken into consideration, so if the logged in user does not
        /// have permission to see the group, it will not be returned.
        /// </summary>
        /// <returns>GenericListResult of SmallGroupMember objects.</returns>
        [WebGet(UriTemplate = "person/{id}/groupmembership")]
        public Contracts.GenericListResult<Contracts.SmallGroupMember> GetPersonSmallGroupMembership(int id)
        {
            Contracts.GenericListResult<Contracts.SmallGroupMember> list = new Contracts.GenericListResult<Contracts.SmallGroupMember>();
            Contracts.SmallGroupMemberMapper mapper = new Contracts.SmallGroupMemberMapper();
            Contracts.SmallGroupMember member;
            CategoryCollection cc = new CategoryCollection();
            GroupCollection gc = new GroupCollection();
            GroupMember gm;


            //
            // If they are requesting membership for a person, get the list
            // of groups this person is a member of. Does not return groups
            // this person is a leader of.
            //
            list.Items = new List<Contracts.SmallGroupMember>();
            foreach (Category c in cc)
            {
                gc = new GroupCollection();
                gc.LoadByPersonID(id, c.CategoryID);

                foreach (Group g in gc)
                {
                    if (RestApi.GroupClusterOperationAllowed(ArenaContext.Current.Person.PersonID, g.GroupClusterID, OperationType.View) == false)
                        continue;

                    gm = new GroupMember(g.GroupID, id);
                    member = mapper.FromArena(new GroupMember(g.GroupID, id));
                    if (member.Group.ID == -1)
                        continue;

                    list.Items.Add(mapper.FromArena(gm));
                }
            }

            list.Total = list.Max = list.Items.Count;
            list.Start = 0;

            return list;
        }


        /// <summary>
        /// Retrieve a list of all small groups that this person is a leader of.
        /// Security is taken into consideration, so if the logged in user does not
        /// have permission to see the group, it will not be returned.
        /// </summary>
        /// <returns>GenericListResult of SmallGroupMember objects.</returns>
        [WebGet(UriTemplate = "person/{id}/groupleadership")]
        public Contracts.GenericListResult<Contracts.GenericReference> GetPersonSmallGroupLeadership(int id)
        {
            Contracts.GenericListResult<Contracts.GenericReference> list = new Contracts.GenericListResult<Contracts.GenericReference>();
            GroupCollection gc = new GroupCollection();


            //
            // If they are requesting membership for a person, get the list
            // of groups this person is a member of. Does not return groups
            // this person is a leader of.
            //
            list.Items = new List<Contracts.GenericReference>();
            gc.LoadByLeaderPersonID(id);
            foreach (Group g in gc)
            {
                if (RestApi.GroupClusterOperationAllowed(ArenaContext.Current.Person.PersonID, g.GroupClusterID, OperationType.View) == false)
                    continue;

                list.Items.Add(new Contracts.GenericReference(g));
            }

            list.Total = list.Max = list.Items.Count;
            list.Start = 0;

            return list;
        }
    }
}
