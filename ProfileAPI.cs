
using System;
using System.Collections.Generic;
using System.Configuration;
using System.ServiceModel.Web;
using Arena.Core;
using Arena.Security;

namespace Arena.Custom.HDC.WebService
{
    class ProfileAPI
    {
        /// <summary>
        /// Retrieve a list of profiles under the identified profile ID or
        /// at the root level of the identified profile type.
        /// </summary>
        /// <param name="profileID">The parent profile to find all child profiles of.</param>
        /// <param name="profileType">Retrieve a list of root profiles of this type.</param>
        /// <param name="start">The number at which to start retrieving records.</param>
        /// <param name="max">The number of records to return.</param>
        /// <param name="fields">If supplied, the result is an array of Profile contracts with only the supplied fields.</param>
        /// <returns>GenericListResult that contains either GenericReference objects or Profile objects.</returns>
        [WebGet(UriTemplate = "profile/list?profileID={profileID}&profileType={profileType}&start={start}&max={max}&fields={fields}")]
        public Object GetProfileList(String profileID, String profileType, int start, int max, String fields)
        {
            ProfileCollection profiles = null;
            Contracts.ProfileMapper mapper = null;
            Contracts.GenericListResult<Contracts.Profile> listP = new Contracts.GenericListResult<Contracts.Profile>();
            Contracts.GenericListResult<Contracts.GenericReference> listR = new Contracts.GenericListResult<Contracts.GenericReference>();
            int i;


            if (profileType != null)
            {
                profiles = new ProfileCollection();
                profiles.LoadChildProfiles(-1, RestApi.DefaultOrganizationID(), (Arena.Enums.ProfileType)Convert.ToInt32(profileType), ArenaContext.Current.Person.PersonID);
            }
            else if (profileID != null)
            {
                Profile profile;

                if (RestApi.ProfileOperationAllowed(ArenaContext.Current.Person.PersonID, Convert.ToInt32(profileID), OperationType.View) == false)
                    throw new Exception("Access denied.");

                profile = new Profile(Convert.ToInt32(profileID));
                profiles = profile.ChildProfiles;
            }
            else
                throw new Exception("Required parameters not provided.");

            //
            // Sort the list of profiles and determine if we are going to
            // be returning references or full objects.
            //
            profiles.Sort(delegate(Profile p1, Profile p2) { return p1.Name.CompareTo(p2.Name); });
            mapper = (string.IsNullOrEmpty(fields) ? null : new Contracts.ProfileMapper(new List<string>(fields.Split(','))));

            //
            // Prepare the appropraite list object.
            //
            if (mapper != null)
            {
                listP.Start = start;
                listP.Max = max;
                listP.Total = 0;
                listP.Items = new List<Contracts.Profile>();
            }
            else
            {
                listR.Start = start;
                listR.Max = max;
                listR.Total = 0;
                listR.Items = new List<Contracts.GenericReference>();
            }
            for (i = 0; i < profiles.Count; i++)
            {
                if (RestApi.ProfileOperationAllowed(ArenaContext.Current.Person.PersonID, profiles[i].ProfileID, OperationType.View) == false)
                    continue;

                if (mapper != null)
                {
                    if (listP.Total >= start && (max <= 0 ? true : listP.Items.Count < max))
                        listP.Items.Add(mapper.FromArena(profiles[i]));
                    listP.Total += 1;
                }
                else
                {
                    if (listR.Total >= start && (max <= 0 ? true : listR.Items.Count < max))
                       listR.Items.Add(new Contracts.GenericReference(profiles[i]));
                    listR.Total += 1;
                }
            }

            return (mapper != null ? (Object)listP : (Object)listR);
        }

        /// <summary>
        /// Retrieve the information about a profile. If the profile
        /// is not found, or no access is permitted to the profile, then
        /// an exception is thrown to the client.
        /// </summary>
        /// <param name="profileID">The ID number of the profile to look up.</param>
        /// <returns>Basic information about the profile.</returns>
        [WebGet(UriTemplate = "profile/{profileID}")]
        public Contracts.Profile GetProfileInformation(int profileID)
        {
            Contracts.ProfileMapper mapper = new Contracts.ProfileMapper();
            Profile profile = new Profile(profileID);


            if (profile.ProfileID == -1)
                throw new Arena.Services.Exceptions.ResourceNotFoundException("Invalid profile ID");

            if (RestApi.ProfileOperationAllowed(ArenaContext.Current.Person.PersonID, profile.ProfileID, OperationType.View) == false)
                throw new Exception("Access denied.");

            return mapper.FromArena(profile);
        }

        /// <summary>
        /// Find all people who are members of the profile and return their
        /// person IDs. All members are returned including in-active members. If the
        /// profile has no members then an empty array is returned.
        /// </summary>
        /// <param name="profileID">The profile to find members of.</param>
        /// <param name="start">The 0-based index to start retrieving at.</param>
        /// <param name="max">The maximum number of members to retrieve.</param>
        /// <returns>GenericListResult of ProfileMember objects.</returns>
        [WebGet(UriTemplate = "profile/{profileID}/members/list?statusID={statusID}&start={start}&max={max}")]
        public Contracts.GenericListResult<Contracts.ProfileMember> GetProfileMembers(int profileID, String statusID, int start, int max)
        {
            Contracts.GenericListResult<Contracts.ProfileMember> list = new Contracts.GenericListResult<Contracts.ProfileMember>();
            Profile profile = new Profile(profileID);
            int i, nStatusID;


            if (RestApi.ProfileOperationAllowed(ArenaContext.Current.Person.PersonID, profile.ProfileID, OperationType.View) == false)
                throw new Exception("Access denied.");

            try
            {
                nStatusID = Convert.ToInt32(statusID);
            }
            catch
            {
                nStatusID = -1;
            }

            profile.LoadMemberArray();
            list.Start = start;
            list.Max = max;
            list.Total = 0;
            list.Items = new List<Contracts.ProfileMember>();

            for (i = 0; i < profile.Members.Count; i++)
            {
                if (nStatusID != -1 && profile.Members[i].Status.LookupID != nStatusID)
                    continue;

                if (list.Total >= start && (max <= 0 ? true : list.Items.Count < max))
                    list.Items.Add(new Contracts.ProfileMember(profile.Members[i]));
                list.Total += 1;
            }

            return list;
        }

        /// <summary>
        /// Retrieve a single member of a profile. If the profile is not
        /// found, or no access is permitted to the profile, then
        /// an exception is thrown to the client.
        /// </summary>
        /// <param name="profileID">The ID number of the profile to look up.</param>
        /// <returns>Basic information about the profile.</returns>
        [WebGet(UriTemplate = "profile/{profileID}/members/{personID}")]
        public Contracts.ProfileMember GetProfileMember(int profileID, int personID)
        {
            ProfileMember member = new ProfileMember(profileID, personID);


            if (member.ProfileID == -1)
                throw new Arena.Services.Exceptions.ResourceNotFoundException("Invalid profile ID");

            if (RestApi.ProfileOperationAllowed(ArenaContext.Current.Person.PersonID, member.ProfileID, OperationType.View) == false)
                throw new Exception("Access denied.");

            return new Contracts.ProfileMember(member);
        }

    }
}
