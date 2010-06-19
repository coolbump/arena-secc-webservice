
using System;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Web;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using Arena.Portal;
using Arena.Core;
using Arena.Security;
using Arena.Enums;
using Arena.Peer;
using Arena.Organization;
using Arena.SmallGroup;


namespace Arena.Custom.HDC.WebService
{
    /// <summary>
    /// Provides the core functionality of the web service. Other
    /// classes exist which provide the actual front-end to different
    /// RPC providers. Class methods provide anonymous functionality,
    /// such as checking the API version. Instance methods provide
    /// authenticated functionality, such as searching for people.
    /// In communicating with the RPC service, a few rules are followed.
    /// The standard retrieval and update rules for structures here
    /// infers that on retrieval, only fields that are valid and the
    /// person has access to are filled in, the others are not included
    /// to save bandwidth. During updates or record creation, only
    /// those fields that are supplied (not null) are updated. Any
    /// Enum, as of this version, are treated as integers so client
    /// libraries should provide local enums or other provisions to
    /// ensure the user knows what the value stands for.
    /// </summary>
    public class CoreRpc
    {
        #region Methods for working with people records.

        /// <summary>
        /// Retrieve an RpcPeerList object to identify all the peers the
        /// given personID has. If no peers are found then the peers member
        /// of the returned object will be empty. If the person is not found
        /// then an empty peers member will be returned.
        /// </summary>
        /// <param name="personID">The ID of the person who we are interested in.</param>
        /// <param name="peerCount">The number of peers to return, if more peers are available only this many will be returned.</param>
        /// <returns>A new RpcPeerList object which contains the information requested.</returns>
        public RpcPeerList GetPersonPeers(int personID, int start, int count)
        {
            ScoreCollection scores = new ScoreCollection();
            RpcPeerList list = new RpcPeerList();
            ArrayList peers = new ArrayList();
            Score s;
            int i;

            //
            // Load the peers for this person.
            //
            scores.LoadBySourcePersonId(personID, 1);

            //
            // Make sure we have valid values to work with.
            //
            if (start < 0)
                start = 0;
            if ((start + count) > scores.Count)
                count = (scores.Count - start);

            //
            // Walk each peer and add them to our array.
            //
            for (i = 0; i < count; i++)
            {
                RpcPeer peer = new RpcPeer();

                s = scores[i + start];
                peer.PersonID = s.TargetPersonId;
                peer.FullName = new Person(s.TargetPersonId).FullName;
                peer.Score = s.TotalScore;
                peer.Trend = s.UpwardTrend;
            }

            list.PersonID = personID;
            list.Peers = (RpcPeer[])peers.ToArray(typeof(RpcPeer));

            return list;
        }

        /// <summary>
        /// Retrieves all the ministry and serving profiles that
        /// this person is an active member of. Ministry profiles
        /// are returned in the "ministry" key and serving profiles
        /// are returned in the "serving" key.
        /// </summary>
        /// <param name="personID">The person we are interested in loading profiles for.</param>
        /// <returns>Returns a dictionary of keys that point to integer arrays.</returns>
        public RpcProfileList GetPersonProfiles(int personID)
        {
            Person person;
            ArrayList profileIDs;
            RpcProfileList list;
            ProfileCollection collection;
            int i;

            //
            // Find the person in question.
            //
            person = new Person(personID);
            list = new RpcProfileList();

            //
            // Load up the profiles for this person.
            //
            if (person.PersonID != -1)
            {
                if (PersonFieldOperationAllowed(ArenaContext.Current.Person.PersonID, PersonFields.Activity_Ministry_Tags, OperationType.View) == true)
                {
                    //
                    // Load all the ministry profiles for this person.
                    //
                    collection = new ProfileCollection();
                    collection.LoadMemberProfiles(DefaultOrganizationID(), ProfileType.Ministry, personID, true);
                    profileIDs = new ArrayList();
                    for (i = 0; i < collection.Count; i++)
                    {
                        profileIDs.Add(collection[i].ProfileID);
                    }
                    list.Ministry = (int[])profileIDs.ToArray(typeof(int));
                }

                if (PersonFieldOperationAllowed(ArenaContext.Current.Person.PersonID, PersonFields.Activity_Serving_Tags, OperationType.View) == true)
                {
                    //
                    // Load all the serving profiles for this person.
                    //
                    collection = new ProfileCollection();
                    collection.LoadMemberProfiles(DefaultOrganizationID(), ProfileType.Serving, personID, true);
                    profileIDs = new ArrayList();
                    for (i = 0; i < collection.Count; i++)
                    {
                        profileIDs.Add(collection[i].ProfileID);
                    }
                    list.Serving = (int[])profileIDs.ToArray(typeof(int));
                }
            }

            return list;
        }

        #endregion

        #region Methods for working with profile records.

		public RpcProfileMemberActivity[] GetProfileMemberActivity(int profileID, int personID)
		{
			ArrayList list = new ArrayList();

            //
            // Check if the user has access to view information about the
            // profile.
            //
            if (ProfileOperationAllowed(ArenaContext.Current.Person.PersonID, profileID, OperationType.View) == true)
			{
				DataTable dt;
				StringBuilder sb = new StringBuilder();
				RpcProfileMemberActivity activity;
				Profile profile = new Profile(profileID);

				dt = new Arena.DataLayer.Core.ProfileMemberActivityData().GetProfileMemberActivityDetails_DT(DefaultOrganizationID(), profile.ProfileType, profile.Owner.PersonID, personID);
				foreach (DataRow dr in dt.Rows)
				{
					activity = new RpcProfileMemberActivity();

					activity.activity_type = dr["activity_type"].ToString();
					activity.created_by = dr["created_by"].ToString();
					activity.date_created = Convert.ToDateTime(dr["date_created"].ToString());
					activity.notes = dr["notes"].ToString();
					activity.person_id = Convert.ToInt32(dr["person_id"].ToString());
					activity.profile_id = Convert.ToInt32(dr["profile_id"].ToString());
					activity.profile_name = dr["profile_name"].ToString();

					list.Add(activity);
				}
			}

			return (RpcProfileMemberActivity[])list.ToArray(typeof(RpcProfileMemberActivity));
		}

        /// <summary>
        /// Get a list of all occurence IDs for a profile.
        /// </summary>
        /// <param name="profileID">The profile to list occurences of.</param>
        /// <returns>Integer array of occurrence IDs.</returns>
        public int[] GetProfileOccurrences(int profileID)
        {
            Profile profile;
            ArrayList list;
            int i;


            //
            // Load up the profile and check to see if it was found or not.
            //
            profile = new Profile(profileID);
            if (profile.ProfileID == -1)
                return new int[0];
            list = new ArrayList(profile.Occurrences.Count);

            //
            // Check if the user has access to view information about the
            // profile.
            //
            if (ProfileOperationAllowed(ArenaContext.Current.Person.PersonID, profileID, OperationType.View) == true)
            {
                for (i = 0; i < profile.Occurrences.Count; i++)
                {
                    list.Add(profile.Occurrences[i].OccurrenceID);
                }
            }

            return (int[])list.ToArray(typeof(int));
        }

        #endregion

        #region Methods for working with small group records.

        /// <summary>
        /// Find all occurrences of the given small group. If the small group
        /// currently has no occurrences then an empty array is returned.
        /// </summary>
        /// <param name="groupID">The small group whose occurrences we are interested in.</param>
        /// <returns>Integer array of occurenceIDs.</returns>
		public int[] GetSmallGroupOccurrences(int groupID)
		{
			ArrayList list = new ArrayList();
			Group group = new Group(groupID);


            if (GroupClusterOperationAllowed(ArenaContext.Current.Person.PersonID, group.GroupClusterID, OperationType.View) == true)
			{
				foreach (GroupOccurrence occurrence in group.Occurrences)
				{
					list.Add(occurrence.OccurrenceID);
				}
			}

			return (int[])list.ToArray(typeof(int));
		}

        #endregion

        #region Private methods for validating security.
        /// <summary>
        /// Determines if the personID has access to perform the
        /// indicated operation on the person field in question.
        /// </summary>
        /// <param name="personID">The ID number of the person whose security access we are checking.</param>
        /// <param name="field">The ID number of the PersonField that the user wants access to.</param>
        /// <param name="operation">The type of access the user needs to proceed.</param>
        /// <returns>true/false indicating if the operation is allowed.</returns>
        static private bool PersonFieldOperationAllowed(int personID, int field, OperationType operation)
        {
            PermissionCollection permissions;

            //
            // Load the permissions.
            //
            permissions = new PermissionCollection(ObjectType.PersonField, field);

            return PermissionsOperationAllowed(permissions, personID, operation);
        }

        /// <summary>
        /// Determines if the personID has access to perform the
        /// indicated operation on the profile in question.
        /// </summary>
        /// <param name="personID">The ID number of the person whose security access we are checking.</param>
        /// <param name="profileID">The ID number of the profile the user wants access to.</param>
        /// <param name="operation">The type of access the user needs to proceed.</param>
        /// <returns>true/false indicating if the operation is allowed.</returns>
        static private bool ProfileOperationAllowed(int personID, int profileID, OperationType operation)
        {
            PermissionCollection permissions;

            //
            // Load the permissions.
            //
            permissions = new PermissionCollection(ObjectType.Tag, profileID);

            return PermissionsOperationAllowed(permissions, personID, operation);
        }

		/// <summary>
		/// Determines if the personID has access to perform the indicated operation
		/// on the small group cluster in question.
		/// </summary>
		/// <param name="personID">The ID number of the person whose security access we are checkin.</param>
		/// <param name="clusterID">The ID number of the profile the user wants access to.</param>
		/// <param name="operation">The type of access the user needs to proceed.</param>
		/// <returns>true/false indicating if the operation is allowed.</returns>
		static private bool GroupClusterOperationAllowed(int personID, int clusterID, OperationType operation)
		{
			PermissionCollection permissions;

			//
			// Load the permissions.
			//
			permissions = new PermissionCollection(ObjectType.Group_Cluster, clusterID);

			return PermissionsOperationAllowed(permissions, personID, operation);
		}

        /// <summary>
        /// Checks the PermissionCollection class to determine if the
        /// indicated operation is allowed for the person identified by
        /// their ID number.
        /// </summary>
        /// <param name="permissions">The collection of permissions to check. These should be object permissions.</param>
        /// <param name="personID">The ID number of the user whose security access we are checking.</param>
        /// <param name="operation">The type of access the user needs to proceed.</param>
        /// <returns>true/false indicating if the operation is allowed.</returns>
        static private bool PermissionsOperationAllowed(PermissionCollection permissions, int personID, OperationType operation)
        {
            RoleCollection roles;
            int i;

            //
            // Check if the person has direct permission.
            //
            if (permissions.ContainsSubjectOperation(SubjectType.Person, personID, operation) == true)
                return true;

            //
            // Now check all roles for the given person.
            //
            roles = new RoleCollection(DefaultOrganizationID(), personID);
            for (i = 0; i < roles.Count; i++)
            {
                if (permissions.ContainsSubjectOperation(SubjectType.Role, roles[i].RoleID, operation) == true)
                    return true;
            }

            return false;
        }
        #endregion

        #region Generic convenience methods.

        /// <summary>
        /// Retrieve the default organization ID for this web
        /// service. This is retrieved via the "Organization"
        /// application setting in the web.config file.
        /// </summary>
        /// <returns>An integer indicating the organization ID.</returns>
        static public int DefaultOrganizationID()
        {
            return Convert.ToInt32(ConfigurationSettings.AppSettings["Organization"]);
        }

        /// <summary>
        /// Retrieve the base url (the portion of the URL without the last path
        /// component, that is the filename and query string) of the current
        /// web request.
        /// </summary>
        /// <returns>Base url as a string.</returns>
        static public string BaseUrl()
        {
            StringBuilder url = new StringBuilder();
            string[] segments;
            int i;


            url.Append(HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority));
            segments = HttpContext.Current.Request.Url.Segments;
            for (i = 0; i < segments.Length - 1; i++)
            {
                url.Append(segments[i]);
            }

            return url.ToString();
        }

        #endregion
    }

    #region Data structures used in RPC communication

    /// <summary>
    /// Collection of profile IDs of the different profile types.
    /// Generally this would be used when retrieving the profiles
    /// of a person, but I suppose it could be used elsewhere too.
    /// </summary>
    public struct RpcProfileList
    {
        /// <summary>
        /// Integer array of ministry profile IDs.
        /// </summary>
        public int[] Ministry;

        /// <summary>
        /// Integer array of serving profile IDs.
        /// </summary>
        public int[] Serving;

        /// <summary>
        /// Integer array of event profile IDs.
        /// TODO: Need to support event profiles. How is security
        /// handled in this case?
        /// </summary>
        public int[] Event;
    }

	/// <summary>
	/// The Profile Member Activity contains the activity of a profile member
	/// for a given profile type and owner. The query to retrieve this
	/// information retrieves all activity where the owner of the profile now
	/// matches the owner of the profile when the activity was created and
	/// the type of profile is the same. For example, if I am the owner of 3
	/// different profiles and somebody retrieves their profile activity in
	/// one of those 3 profiles, they get the activity of all 3.
	/// </summary>
	public struct RpcProfileMemberActivity
	{
		/// <summary>
		/// The ID number of the profile this activity is for.
		/// </summary>
		public int profile_id;

		/// <summary>
		/// The ID number of the person this activity is for.
		/// </summary>
		public int person_id;

		/// <summary>
		/// The date that this activity was created.
		/// </summary>
		public DateTime date_created;

		/// <summary>
		/// The login name of the person who created this activity.
		/// </summary>
		public string created_by;

		/// <summary>
		/// The name of the profile this activity relates to. This name
		/// is static. It is the name of the profile when the activity
		/// was created, not neccessarily the current name of the
		/// profile.
		/// </summary>
		public string profile_name;

		/// <summary>
		/// The type of activity this item is.
		/// </summary>
		public string activity_type;

		/// <summary>
		/// The general notes for this activity, which can be either a
		/// system formatted string or a user defined textual string.
		/// </summary>
		public string notes;
	}

    /// <summary>
    /// Identifies a single peer by its person ID, formal name and
    /// the peer level. In general, the formal name should be used
    /// for displaying and the Level should be used for sorting with
    /// higher numbers displayed first.
    /// </summary>
    public struct RpcPeer
    {
        /// <summary>
        /// The personID of the peer identified by this structure.
        /// </summary>
        public int PersonID;

        /// <summary>
        /// A string which identifies the formal name of this person, this
        /// name can and should be used when displaying the name of the
        /// person in a list to be chosen from for navigating to this person.
        /// </summary>
        public string FullName;

        /// <summary>
        /// The peer score value of this person.
        /// </summary>
        public int Score;

        /// <summary>
        /// True if this peer has an upward trend, false otherwise.
        /// </summary>
        public bool Trend;
    }

    /// <summary>
    /// This structure identifies a list of peers and the person
    /// to whom the list belongs.
    /// </summary>
    public struct RpcPeerList
    {
        /// <summary>
        /// The person to identify who this peer list belongs to.
        /// </summary>
        public int PersonID;

        /// <summary>
        /// The list of peers associated with this person.
        /// </summary>
        public RpcPeer[] Peers;
    }

    #endregion
}
