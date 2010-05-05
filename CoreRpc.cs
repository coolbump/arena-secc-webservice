
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
        #region Anonymous (non-authenticated) methods.

        /// <summary>
        /// Returns the version of the Arena Web Service API protocol
        /// supported by the server. Currently this is 0.1.
        /// </summary>
        /// <returns>API Version</returns>
        static public RpcVersion Version()
        {
            RpcVersion version = new RpcVersion();

            version.Major = 0;
            version.Minor = 1;
            version.ArenaVersion = new Arena.DataLayer.Utility.Database().GetArenaDatabaseVersion();

            return version;
        }

        /// <summary>
        /// Checks if the given client version is compatible with the server.
        /// </summary>
        /// <param name="major">Client's major version number.</param>
        /// <param name="minor">Client's minor version number.</param>
        /// <returns>True if it is safe for the client to communicate with the server, false otherwise.</returns>
        static public bool IsClientVersionSupported(int major, int minor)
        {
            if (major == CoreRpc.Version().Major)
                return true;

            return false;
        }

        /// <summary>
        /// Log the user in with a loginID and password. This method
        /// will create temporary authorization credentials for the user
        /// to use for the near future.
        /// </summary>
        /// <param name="loginID">The login_id the user wishes to login with.</param>
        /// <param name="password">The password used to authenticate the login.</param>
        /// <returns>Authorization key that uniquely identifies this login session.</returns>
        static public string Login(string loginID, string password)
        {
            Login loginUser;


            //
            // Verify that the users login_id and password are valid.
            //
            loginUser = new Login(loginID);
            if (loginUser.IsAccountLocked() == true || loginUser.AuthenticateInDatabase(password) == false)
                throw new UnauthorizedAccessException("Invalid username or password.");

            //
            // Generate a temporary authorization key for the user to use
            // during this session.
            //
            ArrayList paramList = new ArrayList();
            SqlParameter paramOut = new SqlParameter();
            int authorizationId;
            string apiKey;

            paramList.Add(new SqlParameter("AuthorizationId", -1));
            paramList.Add(new SqlParameter("LoginId", loginID));
            paramList.Add(new SqlParameter("Temporary", 1));
            paramList.Add(new SqlParameter("Expires", DateTime.Now.AddHours(1)));
            paramOut.ParameterName = "@ID";
            paramOut.Direction = ParameterDirection.Output;
            paramOut.SqlDbType = SqlDbType.Int;
            paramList.Add(paramOut);
            SqlDataReader reader = new Arena.DataLayer.Organization.OrganizationData().ExecuteReader(
                        "cust_hdc_webservice_sp_save_authorization", paramList);
            authorizationId = (int)((SqlParameter)(paramList[paramList.Count - 1])).Value;
            reader.Close();

            //
            // Retrieve the new GUID value for this login.
            //
            paramList = new ArrayList();
            paramList.Add(new SqlParameter("AuthorizationId", authorizationId));
            reader = new Arena.DataLayer.Organization.OrganizationData().ExecuteReader(
                        "cust_hdc_webservice_sp_get_authorizationByAuthorizationId", paramList);
            reader.Read();
            apiKey = reader["apikey"].ToString();
            reader.Close();

            return apiKey;
        }

        #endregion

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

        /// <summary>
        /// Retrieve the information about a profile. If the profile
        /// is not found, or no access is permitted to the profile, then
        /// -1 is returned in the ProfileID member.
        /// </summary>
        /// <param name="profileID">The ID number of the profile to look up.</param>
        /// <returns>Basic profile information.</returns>
        public RpcProfileInformation GetProfileInformation(int profileID)
        {
            Profile profile;
            RpcProfileInformation info;


            //
            // Load up the profile and check to see if it was found or not.
            //
            profile = new Profile(profileID);
            info = new RpcProfileInformation();
            info.ProfileID = profile.ProfileID;
            if (info.ProfileID == -1)
                return info;

            //
            // Check if the user has access to view information about the
            // profile.
            //
            if (ProfileOperationAllowed(ArenaContext.Current.Person.PersonID, profileID, OperationType.View) == true)
            {
                info.Name = profile.Name;
                info.Active = profile.Active;
                info.Type = profile.ProfileType.ToString();
                info.ProfileActiveCount = profile.ProfileActiveMemberCount;
                info.ProfileMemberCount = profile.ProfileMemberCount;
                if (profile.Campus != null && profile.Campus.CampusId != -1)
                {
                    info.CampusID = profile.Campus.CampusId;
                }
                if (profile.Notes != "")
                {
                    info.Notes = profile.Notes;
                }
                if (profile.ParentProfileID != -1)
                {
                    info.ParentID = profile.ParentProfileID;
                }
                info.ActiveCount = profile.ActiveMembers;
                info.CriticalCount = profile.CriticalMembers;
                info.NavigationUrl = profile.NavigationUrl;
                info.NoContactCount = profile.NoContactMembers;
                info.OwnerID = profile.Owner.PersonID;
                info.OwnerRelationshipStrength = profile.OwnerRelationshipStrength;
                info.PeerRelationshipStrength = profile.PeerRelationshipStrength;
                info.PendingCount = profile.PendingMembers;
                info.ReviewCount = profile.InReviewMembers;
                info.TotalCount = profile.TotalMembers;
                info.CreatedBy = profile.CreatedBy;
                info.DateCreated = profile.DateCreated;
                info.DateModified = profile.DateModified;
                info.ModifiedBy = profile.ModifiedBy;
            }

            return info;
        }

        /// <summary>
        /// Retrieve the information about a person's membership in a
        /// given profile. If the profile is not found then -1 is returned in
        /// the ProfileID and PersonID member variables. If no access is
        /// permitted to the profile then the ProfileID and PersonID member
        /// variables are the only variables filled in.
        /// </summary>
        /// <param name="profileID">The ID number of the profile to look up.</param>
        /// <param name="personID">The Id number of the person (member) to look up.</param>
        /// <returns>Basic information about a person's membership in a profile.</returns>
        public RpcProfileMemberInformation GetProfileMemberInformation(int profileID, int personID)
        {
            Profile profile;
            ProfileMember member;
            RpcProfileMemberInformation info;


            //
            // Load up the profile and check to see if it was found or not.
            //
            member = new ProfileMember(profileID, personID);
            info = new RpcProfileMemberInformation();
            info.ProfileID = member.ProfileID;
            info.PersonID = member.PersonID;
            if (info.ProfileID == -1)
                return info;

            //
            // Check if the user has access to view information about the
            // profile.
            //
            if (ProfileOperationAllowed(ArenaContext.Current.Person.PersonID, profileID, OperationType.View) == true)
            {
				info.AttendanceCount = member.AttendanceCount;
                if (member.DateActive.Year > 1901 && member.DateActive.Year != 9999)
                    info.DateActive = member.DateActive;
                if (member.DateDormant.Year > 1901 && member.DateDormant.Year != 9999)
                    info.DateDormant = member.DateDormant;
                if (member.DateInReview.Year > 1901 && member.DateInReview.Year != 9999)
                    info.DateInReview = member.DateInReview;
                if (member.DatePending.Year > 1901 && member.DatePending.Year != 9999)
                    info.DatePending = member.DatePending;
                info.MemberNotes = member.MemberNotes;
//                info.Source = new RpcLookup(member.Source);
//                info.Status = new RpcLookup(member.Status);
                info.StatusReason = member.StatusReason;

				profile = new Profile(profileID);
				info.ProfileName = profile.Name;
                if (member.NickName == "")
                    info.PersonName = member.FirstName + " " + member.LastName;
                else
                    info.PersonName = member.NickName + " " + member.LastName;
            }

            return info;
        }

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
        /// Retrieve all the ID numbers of the profiles directly beneath
        /// this profile.
        /// </summary>
        /// <param name="profileID">The ID number of the profile in question.</param>
        /// <returns>Integer array of the child profile ID numbers.</returns>
        public int[] GetProfileChildren(int profileID)
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
            list = new ArrayList();

            //
            // Check if the user has access to view information about the
            // profile.
            //
            if (ProfileOperationAllowed(ArenaContext.Current.Person.PersonID, profileID, OperationType.View) == true)
            {
                for (i = 0; i < profile.ChildProfiles.Count; i++)
                {
                    if (ProfileOperationAllowed(ArenaContext.Current.Person.PersonID, profile.ChildProfiles[i].ProfileID, OperationType.View) == true)
                    {
                        list.Add(profile.ChildProfiles[i].ProfileID);
                    }
                }
            }

            return (int[])list.ToArray(typeof(int));
        }

        /// <summary>
        /// Rerieves the profile ID numbers of all root level profiles of
        /// the given profile type.
        /// </summary>
        /// <param name="profileType">The integer value of the profile type.</param>
        /// <returns>Integer array of the root profiles.</returns>
        public int[] GetProfileRoots(int profileType)
        {
            ProfileCollection collection;
            ArrayList list;
            int i;


            collection = new ProfileCollection();
            collection.LoadChildProfiles(-1, DefaultOrganizationID(), (ProfileType)profileType, ArenaContext.Current.Person.PersonID);
            list = new ArrayList();
            for (i = 0; i < collection.Count; i++)
            {
                if (ProfileOperationAllowed(ArenaContext.Current.Person.PersonID, collection[i].ProfileID, OperationType.View) == true)
                {
                    list.Add(collection[i].ProfileID);
                }
            }

            return (int[])list.ToArray(typeof(int));;
        }

        /// <summary>
        /// Get the people ID numbers of all members of this profile.
        /// </summary>
        /// <param name="profileID">Profile to retrieve member list from.</param>
        /// <returns>Integer array of people IDs.</returns>
        public int[] GetProfileMembers(int profileID)
        {
            Profile profile;


            //
            // Load up the profile and check to see if it was found or not.
            //
            profile = new Profile(profileID);
            if (profile.ProfileID == -1)
                return new int[0];

            //
            // Check if the user has access to view information about the
            // profile.
            //
            if (ProfileOperationAllowed(ArenaContext.Current.Person.PersonID, profileID, OperationType.View) == true)
            {
                profile.LoadMemberArray();
                return (int[])profile.MemberArray.ToArray(typeof(int));
            }

            return new int[0];
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
    /// Retrieves version information about the protocol used to
    /// communicate with the RPC server. If a client and server
    /// have the same Major version but different minor versions
    /// it can be assumed safe to communicate, so long as the
    /// client assumes a Minor version of 0. If a minor version
    /// number difference is detected, optional "...Supported"
    /// version members should be checked to detect new features
    /// over the x.0 version and/or protocol changes, so long as
    /// the client minor version is greater than the server's.
    /// The server must not make any protocol changes that change
    /// existing x.0 functionality, for example changing a
    /// structure member's value from an ID number to a name. Such
    /// a change must either wait for the next Major version or be
    /// added as a new member alongside the old member, with an
    /// associated "...Supported" member in this structure. Clients
    /// which have a higher Major version number than the server
    /// may continue to communicate at the clients discretion. If
    /// the client Major version number is less than the server's
    /// it currently must not continue communication unless a call
    /// to IsClientVersionSupported(major, minor) is called first
    /// to ask the server if it's protocol is backwards compatible
    /// with the given client version.
    /// </summary>
    public struct RpcVersion
    {
        /// <summary>
        /// The major protocol version in use by this server.
        /// </summary>
        public int Major;

        /// <summary>
        /// The minor protocol version in use by this server.
        /// </summary>
        public int Minor;

        /// <summary>
        /// A string which contains the Arena version of this system.
        /// </summary>
        public string ArenaVersion;
    }

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
    /// This structure contains the basic information about a profile.
    /// This structure follows the standard RPC retrieval and updating
    /// rules.
    /// </summary>
    public struct RpcProfileInformation
    {
        /// <summary>
        /// Profile ID this information is referencing.
        /// </summary>
        public int ProfileID;

        /// <summary>
        /// The parent profile ID or -1 if this is a root profile.
        /// Note: Is that correct? Need to check on that.
        /// </summary>
        public int? ParentID;

        /// <summary>
        /// The name of this profile.
        /// </summary>
        public string Name;

        /// <summary>
        /// The string representation of the ProfileType enum identifying
        /// if this is a Personal, Ministry, Serving or Event tag.
        /// </summary>
        public string Type;

        /// <summary>
        /// Specifies wether or not this profile is currently marked as
        /// being active.
        /// </summary>
        public bool? Active;

        /// <summary>
        /// The number of active members of this profile, not including
        /// child profiles.
        /// </summary>
        public int? ProfileActiveCount;

        /// <summary>
        /// The total number of members of this profile, not including
        /// child profiles.
        /// </summary>
        public int? ProfileMemberCount;

        /// <summary>
        /// The Campus ID this profile is to the associated with.
        /// </summary>
        public int? CampusID;

        /// <summary>
        /// User entered notes about this profile.
        /// </summary>
        public string Notes;

        /// <summary>
        /// The personID that owns this profile.
        /// </summary>
        public int OwnerID;

        /// <summary>
        /// The person login that created this profile.
        /// </summary>
        public string CreatedBy;

        /// <summary>
        /// The date and time this profile as initially created.
        /// </summary>
        public DateTime DateCreated;

        /// <summary>
        /// The person login who last modified this profile.
        /// </summary>
        public string ModifiedBy;

        /// <summary>
        /// The date and time that this profile was last modified.
        /// </summary>
        public DateTime DateModified;

        /// <summary>
        /// The number of critical members of this profile and all
        /// descendents.
        /// </summary>
        public int CriticalCount;

        /// <summary>
        /// The number of active members of this profile and all
        /// descendents.
        /// </summary>
        public int ActiveCount;

        /// <summary>
        /// The number of members in review for this profile and all
        /// descendents.
        /// </summary>
        public int ReviewCount;

        /// <summary>
        /// The number of members who have not yet been contacted for
        /// this profile and all descendents.
        /// </summary>
        public int NoContactCount;

        /// <summary>
        /// The number of pending members of this profile and all
        /// descendents.
        /// </summary>
        public int PendingCount;

        /// <summary>
        /// The total number of members of this profile and all
        /// descendents.
        /// </summary>
        public int TotalCount;

        /// <summary>
        /// The URL that can be used to view this profile in the Arena
        /// portal.
        /// </summary>
        public string NavigationUrl;

        /// <summary>
        /// The strength of the relationship between the owner and
        /// members of this profile.
        /// </summary>
        public int OwnerRelationshipStrength;

        /// <summary>
        /// The strength of the relationship between members of this
        /// profile.
        /// </summary>
        public int PeerRelationshipStrength;
    }

    /// <summary>
    /// Contains the information that describes a person's status as a
    /// member of a profile.
    /// </summary>
    public struct RpcProfileMemberInformation
    {
        /// <summary>
        /// The Profile that this record pertains to.
        /// </summary>
        public int ProfileID;

        /// <summary>
        /// The Person that this record pertains to.
        /// </summary>
        public int PersonID;

        /// <summary>
        /// The number of occurrences in this profile that this person
        /// has attended.
        /// </summary>
        public int? AttendanceCount;

        /// <summary>
        /// The date stamp that this person was made active in this
        /// profile.
        /// </summary>
        public DateTime? DateActive;

        /// <summary>
        /// The date stamp that this person was made dormant in this
        /// profile.
        /// </summary>
        public DateTime? DateDormant;

        /// <summary>
        /// The date stamp that this person was marked as in review
        /// in this profile.
        /// </summary>
        public DateTime? DateInReview;

        /// <summary>
        /// The date stamp that this person was marked as pending in
        /// this profile.
        /// </summary>
        public DateTime? DatePending;

        /// <summary>
        /// Any notes that have been placed on this person for this
        /// profile.
        /// </summary>
        public string MemberNotes;

		/// <summary>
		/// The activity of the person in this profile type.
		/// </summary>
		public RpcProfileMemberActivity[] MemberActivity;

        /// <summary>
        /// The source of this person, how they got added to the profile.
        /// </summary>
//        public RpcLookup? Source;

        /// <summary>
        /// The status of this person in this profile.
        /// </summary>
//        public RpcLookup? Status;

        /// <summary>
        /// The descriptive reason for why the person has the status they
        /// do. This may not always exist as only some status' have a
        /// reason.
        /// </summary>
        public string StatusReason;

        /// <summary>
        /// The name of the profile. This is a convenience item to reduce
        /// network overhead.
        /// </summary>
        public string ProfileName;

        /// <summary>
        /// The name of the person, first and last name only. This is a
        /// convenience item to reduce network overhead.
        /// </summary>
        public string PersonName;
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
