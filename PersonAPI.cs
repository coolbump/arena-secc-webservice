
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
        /// <param name="id">The ID number of the person to retrieve membership of.</param>
        /// <param name="start">The start index to begin retrieving records at.</param>
        /// <param name="max">The maximum number of records to retrieve.</param>
        /// <returns>GenericListResult of SmallGroupMember objects.</returns>
        [WebGet(UriTemplate = "person/{id}/groupmembership/list?start={start}&max={max}")]
        public Contracts.GenericListResult<Contracts.SmallGroupMember> GetPersonSmallGroupMembership(int id, int start, int max)
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
            list.Start = start;
            list.Max = max;
            foreach (Category c in cc)
            {
                gc = new GroupCollection();
                gc.LoadByPersonID(id, c.CategoryID);

                foreach (Group g in gc)
                {
                    if (RestApi.GroupClusterOperationAllowed(ArenaContext.Current.Person.PersonID, g.GroupClusterID, OperationType.View) == false)
                        continue;

                    if (list.Total >= start && list.Items.Count < max)
                    {
                        gm = new GroupMember(g.GroupID, id);
                        member = mapper.FromArena(new GroupMember(g.GroupID, id));
                        if (member.Group.ID == -1)
                            continue;

                        list.Items.Add(mapper.FromArena(gm));
                    }

                    list.Total += 1;
                }
            }

            return list;
        }


        /// <summary>
        /// Retrieve a list of all small groups that this person is a leader of.
        /// Security is taken into consideration, so if the logged in user does not
        /// have permission to see the group, it will not be returned.
        /// </summary>
        /// <param name="id">The ID number of the person to retrieve membership of.</param>
        /// <param name="start">The start index to begin retrieving records at.</param>
        /// <param name="max">The maximum number of records to retrieve.</param>
        /// <returns>GenericListResult of GenericReference objects.</returns>
        [WebGet(UriTemplate = "person/{id}/groupleadership/list?start={start}&max={max}")]
        public Contracts.GenericListResult<Contracts.GenericReference> GetPersonSmallGroupLeadership(int id, int start, int max)
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
            list.Start = start;
            list.Max = max;
            foreach (Group g in gc)
            {
                if (RestApi.GroupClusterOperationAllowed(ArenaContext.Current.Person.PersonID, g.GroupClusterID, OperationType.View) == false)
                    continue;

                if (list.Total >= start && list.Items.Count < max)
                    list.Items.Add(new Contracts.GenericReference(g));
            }

            return list;
        }


        /// <summary>
        /// Retrieve a list of all profiles that this person is a member of.
        /// Security is taken into consideration, so if the logged in user does not
        /// have permission to see the tag, it will not be returned.
        /// </summary>
        /// <param name="id">The ID number of the person to retrieve membership of.</param>
        /// <param name="inactive">Wether or not to include inactive membership information.</param>
        /// <param name="type">The profile type (personal, ministry, serving) to retrieve membership of.</param>
        /// <param name="start">The start index to begin retrieving records at.</param>
        /// <param name="max">The maximum number of records to retrieve.</param>
        /// <returns>GenericListResult of ProfileMember objects.</returns>
        [WebGet(UriTemplate = "person/{id}/profilemembership/list?type={type}&inactive={inactive}&start={start}&max={max}")]
        public Contracts.GenericListResult<Contracts.ProfileMember> GetPersonProfileMembership(int id, int type, string inactive, int start, int max)
        {
            Contracts.GenericListResult<Contracts.ProfileMember> list = new Contracts.GenericListResult<Contracts.ProfileMember>();
            ProfileCollection pmc = new ProfileCollection();
            bool activeOnly = true;
            int i;


            //
            // Check if they want to include inactive records.
            //
            try
            {
                if (Convert.ToInt32(inactive) == 1)
                    activeOnly = false;
            }
            catch { }

            //
            // Check general.
            //
            if (type == (int)Enums.ProfileType.Ministry && RestApi.PersonFieldOperationAllowed(ArenaContext.Current.Person.PersonID, PersonFields.Activity_Ministry_Tags, OperationType.View) == false)
                throw new Exception("Access denied");
            else if (type == (int)Enums.ProfileType.Serving && RestApi.PersonFieldOperationAllowed(ArenaContext.Current.Person.PersonID, PersonFields.Activity_Serving_Tags, OperationType.View) == false)
                throw new Exception("Access denied");
            else if (type != (int)Enums.ProfileType.Personal && type != (int)Enums.ProfileType.Ministry && type != (int)Enums.ProfileType.Serving)
                throw new Exception("Access denied");

            //
            // If they are requesting membership in their own personal profiles
            // then retrieve those, otherwise retrieve the general profile
            // information.
            //
            if (type == (int)Enums.ProfileType.Personal)
                pmc.LoadMemberPrivateProfiles(RestApi.DefaultOrganizationID(), ArenaContext.Current.Person.PersonID, id, activeOnly);
            else
                pmc.LoadMemberProfiles(RestApi.DefaultOrganizationID(), (Enums.ProfileType)type, id, activeOnly);

            list.Items = new List<Contracts.ProfileMember>();
            list.Start = start;
            list.Max = max;
            foreach (Profile p in pmc)
            {
                if (RestApi.ProfileOperationAllowed(ArenaContext.Current.Person.PersonID, p.ProfileID, OperationType.View) == false)
                    continue;

                if (list.Total >= start && list.Items.Count < max)
                    list.Items.Add(new Contracts.ProfileMember(new ProfileMember(p.ProfileID, id)));
                list.Total += 1;
            }

            return list;
        }
    }
}
