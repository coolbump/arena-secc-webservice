
using System;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Text;
using Arena.Portal;
using Arena.Core;
using Arena.Security;
using Arena.Enums;
using Arena.Peer;


//
// To allow cross-domain support we need to not use any
// dictionary methods. Also need to test if JSONP (cross-domain
// requests) allows for returning objects (dicts, arrays).
// So a single Login method is needed that returns an authorization
// key that is used in the rest of the methods.
//
// When providing the authorization key, if the username/password
// is already found (or rather the username is found matching the
// auth key, and the password matches the user in the login table,
// then the same key is returned and the "valid" period is extended.
//

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
        /// <summary>
        /// The currently authenticated Arena.Login user.
        /// </summary>
        private Login currentLogin;

        /// <summary>
        /// Creates an instance of the WebService.Core class with the given
        /// login credentials. If the credentials are invalid then an
        /// exception is raised.
        /// </summary>
        /// <param name="credentials">Provides the login credentials needed to authenticate the user.</param>
        public CoreRpc(RpcCredentials credentials)
        {
            currentLogin = LoginForCredentials(credentials);
        }

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

        #endregion

        #region Methods for working with people records.

        /// <summary>
        /// Queries the database to find all matching personIDs, which
        /// are returned as an integer array. Only a single type of
        /// search is performed in order of precedence by: Name and
        /// birthdate; name; phone; email; birthdate; area; profiles.
        /// </summary>
        /// <param name="query">Provides the filter to use when searching for people.</param>
        /// <returns>Integer array containing Person ID numbers that match the query.</returns>
        public int[] FindPeople(RpcPeopleQuery query)
        {
            PersonCollection people;
            ArrayList personIDs;
            int i;

            //
            // Find all the people matching the query.
            //
            people = new PersonCollection();
            if (query.FirstName != null && query.LastName != null)
                people.LoadByName(query.FirstName, query.LastName);

            //
            // Build the array of person IDs.
            //
            personIDs = new ArrayList(people.Count);
            for (i = 0; i < people.Count; i++)
            {
                personIDs.Add(people[i].PersonID);
            }

            return (int[])personIDs.ToArray(typeof(int));
        }

        /// <summary>
        /// Retrieves information about the given personID. The
        /// RpcPersonInformation structure is filled as much as allowed by the
        /// users security level.
        /// </summary>
        /// <param name="personID">The ID number of the person to get the basic personal information of.</param>
        /// <returns>Dictionary containing personal information or PersonID key = -1 when not found.</returns>
        public RpcPersonInformation GetPersonInformation(int personID)
        {
            RpcPersonInformation info;
            Person person;

            //
            // Find the person in question.
            //
            person = new Person(personID);
            info = new RpcPersonInformation();

            //
            // Build the basic information, we default to all the values
            // that might not be set. If personID is -1 (not found) then
            // we don't need to continue.
            //
            info.PersonID = person.PersonID;
            if (person.PersonID == -1)
                return info;

            //
            // Add in the fields everybody can see.
            //
            info.CreatedBy = person.CreatedBy;
            info.Modifiedby = person.ModifiedBy;
            info.DateCreated = person.DateCreated;
            info.DateModified = person.DateModified;
            info.NagivationUrl = person.NavigationUrl;

            //
            // Retrieve all the fields the user has access to.
            //
            if (person.MemberStatus.LookupID != -1 && PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Member_Status, OperationType.View))
            {
                info.MemberStatus = new RpcLookup(person.MemberStatus);
            }
            if (PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Record_Status, OperationType.View))
            {
                info.RecordStatus = person.RecordStatus.ToString();
            }
            if (person.Campus != null && PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Campus, OperationType.View))
            {
                info.CampusID = person.Campus.CampusId;
            }
            if (PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Staff_Member, OperationType.View))
            {
                info.Staff = person.StaffMember;
            }
            if (PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Name, OperationType.View))
            {
                info.FirstName = person.FirstName;
                info.LastName = person.LastName;
                if (person.MiddleName != "")
                {
                    info.MiddleName = person.MiddleName;
                }
                if (person.NickName != "")
                {
                    info.NickName = person.NickName;
                }
                if (person.Title.LookupID != -1)
                {
                    info.Title = new RpcLookup(person.Title);
                }
                if (person.Suffix.LookupID != -1)
                {
                    info.Suffix = new RpcLookup(person.Suffix);
                }
            }
            if (PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Family_Information, OperationType.View))
            {
                ArrayList members = new ArrayList();

                foreach (FamilyMember fm in person.Family().FamilyMembers)
                {
                    RpcFamilyMember member = new RpcFamilyMember();

                    member.PersonID = fm.PersonID;
                    member.Role = new RpcLookup(fm.FamilyRole);
                    member.FullName = fm.FullName;

                    members.Add(member);
                }

                info.FamilyID = person.FamilyId;
                info.FamilyMembers = (RpcFamilyMember[])members.ToArray(typeof(RpcFamilyMember));
            }
            if (person.BirthDate.Year != 1900 && PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_BirthDate, OperationType.View))
            {
                info.BirthDate = person.BirthDate;
            }
            if (person.Age != -1 && PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Age, OperationType.View))
            {
                info.Age = person.Age;
            }
            if (PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Gender, OperationType.View))
            {
                info.Gender = person.Gender.ToString();
            }
            if (person.GraduationDate.Year != 1900 && PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Grade, OperationType.View))
            {
                info.Grade = Person.CalculateGradeLevel(person.GraduationDate, Convert.ToDateTime(new Organization.OrganizationSetting(DefaultOrganizationID(), "GradePromotionDate").Value));
            }
            if (PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Activity_Activity_Level, OperationType.View))
            {
                info.ActiveMeter = person.ActiveMeter;
            }
            if (person.AnniversaryDate.Year != 1900 && PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Anniversary_Date, OperationType.View))
            {
                info.Anniversary = person.AnniversaryDate;
            }
            if (PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Contribute_Individually, OperationType.View))
            {
                info.ContributeIndividually = person.ContributeIndividually;
            }
            if (person.EnvelopeNumber != -1 && PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Envelope_Number, OperationType.View))
            {
                info.EnvelopeNumber = person.EnvelopeNumber;
            }
            if (person.Blob.BlobID != -1 && PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Photo, OperationType.View))
            {
                info.ImageUrl = BaseUrl() + "CachedBlob.aspx?guid=" + person.Blob.GUID;
            }
            if (person.InactiveReason.LookupID != -1 && PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Record_Status, OperationType.View))
            {
                info.InactiveReason = new RpcLookup(person.InactiveReason);
            }
            if (person.LastAttended.Year != 1900 && PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Attendance_Recent_Attendance, OperationType.View))
            {
                info.LastAttended = person.LastAttended;
            }
            if (person.DateLastVerified.Year != 1900 && PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Date_Verified, OperationType.View))
            {
                info.LastVerified = person.DateLastVerified;
            }
            if (person.MedicalInformation != "" && PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Medical_Info, OperationType.View))
            {
                info.MedicalInformation = person.MedicalInformation;
            }
            if (PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Print_Statement, OperationType.View))
            {
                info.PrintStatement = person.PrintStatement;
            }
            if (person.Spouse() != null && PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Marital_Status, OperationType.View))
            {
                info.SpouseID = person.Spouse().PersonID;
            }
            if (PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Peers, OperationType.View))
            {
                ArrayList peers = new ArrayList();
                ScoreCollection scores = new ScoreCollection();

                scores.LoadBySourcePersonId(personID, 1);
                foreach (Score s in scores)
                {
                    RpcPeer peer = new RpcPeer();

                    peer.PersonID = s.TargetPersonId;
                    peer.FullName = new Person(s.TargetPersonId).FullName;
                    peer.Score = s.TotalScore;
                    peer.Trend = s.UpwardTrend;
                }

                info.PeerCount = scores.Count;
                info.Peers = (RpcPeer[])peers.ToArray(typeof(RpcPeer));
            }
            if (PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Relationships, OperationType.View))
            {
                ArrayList relationships = new ArrayList();

                foreach (Relationship pr in person.Relationships)
                {
                    RpcRelationship r = new RpcRelationship();

                    r.PersonID = pr.RelatedPersonId;
                    r.FullName = pr.RelatedPerson.FullName;
                    r.Relationship = pr.RelationshipType.Relationship;

                    relationships.Add(r);
                }

                info.Relationships = (RpcRelationship[])relationships.ToArray(typeof(RpcRelationship));
            }

            return info;
        }

        /// <summary>
        /// Retrieves the contact information associated with the
        /// personID. Only information that the user has permission
        /// to is retrieved.
        /// </summary>
        /// <param name="personID">The ID number of the person to get the contact information of.</param>
        /// <returns>Dictionary containing personal information or PersonID key = -1 when not found.</returns>
        public RpcPersonContactInformation GetPersonContactInformation(int personID)
        {
            Person person;
            RpcPersonContactInformation contact;
            RpcAddress address;
            RpcPhone phone;
            RpcEmail email;
            ArrayList addressList, phoneList, emailList;
            int i;


            //
            // Find the person in question.
            //
            person = new Person(personID);
            contact = new RpcPersonContactInformation();
            contact.PersonID = person.PersonID;

            //
            // If the person was found then load up any contact
            // information we have.
            //
            if (person.PersonID == -1)
            {
                if (PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Addresses, OperationType.View) == true)
                {
                    //
                    // Build all the addresses.
                    //
                    addressList = new ArrayList(person.Addresses.Count);
                    for (i = 0; i < person.Addresses.Count; i++)
                    {
                        address = new RpcAddress();
                        address.ID = person.Addresses[i].AddressID;
                        if (person.Addresses[i].AddressType != null)
                            address.Type = person.Addresses[i].AddressType.Value;
                        address.Primary = person.Addresses[i].Primary;
                        if (person.Addresses[i].Address.StreetLine1 != "")
                            address.StreetLine1 = person.Addresses[i].Address.StreetLine1;
                        if (person.Addresses[i].Address.StreetLine2 != "")
                            address.StreetLine2 = person.Addresses[i].Address.StreetLine2;
                        if (person.Addresses[i].Address.City != "")
                            address.City = person.Addresses[i].Address.City;
                        if (person.Addresses[i].Address.State != "")
                            address.State = person.Addresses[i].Address.State;
                        if (person.Addresses[i].Address.PostalCode != "")
                            address.PostalCode = person.Addresses[i].Address.PostalCode;
                        if (person.Addresses[i].Address.Area != null)
                            address.AreaID = person.Addresses[i].Address.Area.AreaID;
                        if (person.Addresses[i].Address.Latitude != 0)
                            address.Latitude = person.Addresses[i].Address.Latitude;
                        if (person.Addresses[i].Address.Longitude != 0)
                            address.Longitude = person.Addresses[i].Address.Longitude;
                        if (person.Addresses[i].Notes != "")
                            address.Notes = person.Addresses[i].Notes;

                        addressList.Add(address);
                    }

                    contact.Addresses = (RpcAddress[])addressList.ToArray(typeof(RpcAddress));
                }

                if (PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Phones, OperationType.View) == true)
                {
                    //
                    // Build all the phones.
                    //
                    phoneList = new ArrayList(person.Phones.Count);
                    for (i = 0; i < person.Phones.Count; i++)
                    {
                        phone = new RpcPhone();
                        if (person.Phones[i].PhoneType != null)
                            phone.Type = person.Phones[i].PhoneType.Value;
                        if (person.Phones[i].Number != "")
                            phone.Number = person.Phones[i].Number;
                        if (person.Phones[i].Extension != "")
                            phone.Ext = person.Phones[i].Extension;
                        phone.Unlisted = person.Phones[i].Unlisted;
                        phone.Sms = person.Phones[i].SMSEnabled;

                        phoneList.Add(phone);
                    }

                    contact.Phones = (RpcPhone[])phoneList.ToArray(typeof(RpcPhone));
                }

                if (PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Emails, OperationType.View) == true)
                {
                    //
                    // Build all the emails.
                    //
                    emailList = new ArrayList(person.Emails.Count);
                    for (i = 0; i < person.Emails.Count; i++)
                    {
                        email = new RpcEmail();
                        email.ID = person.Emails[i].EmailId;
                        if (person.Emails[i].Email != "")
                            email.Email = person.Emails[i].Email;
                        if (person.Emails[i].Notes != "")
                            email.Notes = person.Emails[i].Notes;
                        email.Active = person.Emails[i].Active;

                        emailList.Add(email);
                    }

                    contact.Emails = (RpcEmail[])emailList.ToArray(typeof(RpcEmail));
                }
            }

            return contact;
        }

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
                if (PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Activity_Ministry_Tags, OperationType.View) == true)
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

                if (PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Activity_Serving_Tags, OperationType.View) == true)
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
            if (ProfileOperationAllowed(currentLogin.PersonID, profileID, OperationType.View) == true)
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
            if (ProfileOperationAllowed(currentLogin.PersonID, profileID, OperationType.View) == true)
            {
                for (i = 0; i < profile.ChildProfiles.Count; i++)
                {
                    if (ProfileOperationAllowed(currentLogin.PersonID, profile.ChildProfiles[i].ProfileID, OperationType.View) == true)
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
            collection.LoadChildProfiles(-1, DefaultOrganizationID(), (ProfileType)profileType, currentLogin.PersonID);
            list = new ArrayList();
            for (i = 0; i < collection.Count; i++)
            {
                if (ProfileOperationAllowed(currentLogin.PersonID, collection[i].ProfileID, OperationType.View) == true)
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
            if (ProfileOperationAllowed(currentLogin.PersonID, profileID, OperationType.View) == true)
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
            if (ProfileOperationAllowed(currentLogin.PersonID, profileID, OperationType.View) == true)
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
        /// Retrieve a list of all group categories in the system. If, by chance,
        /// no categories exist then an empty array is returned.
        /// </summary>
        /// <returns>Integer array of group categoryIDs.</returns>
        public int[] GetSmallGroupCategories() { return null; }

        /// <summary>
        /// Retrieve the information about a small group category.
        /// If the given category is not found then -1 is returned in the
        /// categoryID member.
        /// </summary>
        /// <param name="categoryID">The category to find information about.</param>
        /// <returns>Basic information about a group category.</returns>
        public RpcSmallGroupCategoryInformation? GetSmallGroupCategoryInformation(int categoryID) { return null; }

        /// <summary>
        /// Retrieve a list of all group clusters at the root level of the
        /// group category. If no group clusters are contained in the category
        /// then an empty array is returned.
        /// </summary>
        /// <param name="categoryID">The parent category to find all root clusters of.</param>
        /// <returns>Integer array of clusterIDs.</returns>
        public int[] GetSmallGroupRootClusters(int categoryID) { return null; }

        /// <summary>
        /// Retrieve a list of small group clusters that reside underneath
        /// the parent cluster ID. If there are no group clusters beneath
        /// the parent then an empty array is returned. If this happens the
        /// client should make a call to GetSmallGroups to check for any
        /// small groups under the cluster.
        /// </summary>
        /// <param name="clusterID">The parent clusterID to find clusters under.</param>
        /// <returns>An integer array of group clusters.</returns>
        public int[] GetSmallGroupClusters(int clusterID) { return null; }

        /// <summary>
        /// Retrieve the information about a group cluster. If the group
        /// cluster is not found then -1 is returned in the clusterID member.
        /// </summary>
        /// <param name="clusterID">The cluster to retrieve information about.</param>
        /// <returns>Basic information about the group cluster.</returns>
        public RpcSmallGroupClusterInformation? GetSmallGroupClusterInformation(int clusterID) { return null; }

        /// <summary>
        /// Retrieve a list of small groups which reside under the parent group
        /// cluster. If no small groups are found then an empty array is returned.
        /// </summary>
        /// <param name="clusterID">The parent cluster to find small groups under.</param>
        /// <returns>An integer array of small groups under the parent cluster.</returns>
        public int[] GetSmallGroups(int clusterID) { return null; }

        /// <summary>
        /// Retrieves information about the small group. If the small
        /// group is not found then -1 is returned in the groupID member.
        /// </summary>
        /// <param name="groupID">The small group to retrieve information about.</param>
        /// <returns>Basic information about the small group.</returns>
        public RpcSmallGroupInformation? GetSmallGroupInformation(int groupID) { return null; }

        /// <summary>
        /// Find all people who are members of the small group and return their
        /// person IDs. All members are returned including in-active members. If the
        /// small group has no members then an empty array is returned.
        /// </summary>
        /// <param name="groupID">The small group to find members of.</param>
        /// <returns>Integer array of personIDs.</returns>
        public int[] GetSmallGroupMembers(int groupID) { return null; }

        /// <summary>
        /// Find all occurrences of the given small group. If the small group
        /// currently has no occurrences then an empty array is returned.
        /// </summary>
        /// <param name="groupID">The small group whose occurrences we are interested in.</param>
        /// <returns>Integer array of occurenceIDs.</returns>
        public int[] GetSmallGroupOccurrences(int groupID) { return null; }

        #endregion

        #region Private methods for validating security.
        /// <summary>
        /// This method attempts to log the session in given the users
        /// credentials. Currently this is done by a username/password
        /// each web request, but later might include some cached
        /// method of authenticating.
        /// </summary>
        /// <param name="credentials">Provides the login credentials needed to authenticate the user.</param>
        /// <returns>Login class for the authenticated user. Raises UnauthorizedAccessException on invalid login.</returns>
        private Login LoginForCredentials(RpcCredentials credentials)
        {
            Login loginUser;

            loginUser = new Login(credentials.UserName);
            //            if (loginUser.IsAccountLocked() == true || loginUser.AuthenticateInDatabase(credentials.Password) == false)
            //                throw new UnauthorizedAccessException("Invalid username or password.");

            return loginUser;
        }

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
    /// Provides the authentication credentials required to
    /// call any of the instance methods. Currently the only
    /// authentication scheme supported in this version is
    /// username/password authentication. Later versions will
    /// support a single login and then use a session key to
    /// continue working in that session.
    /// </summary>
    public struct RpcCredentials
    {
        /// <summary>
        /// The login id (username) to use for authenticating this
        /// session.
        /// </summary>
        public string UserName;
        /// <summary>
        /// Password associated with the login id.
        /// </summary>
        public string Password;
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
    /// Defines the information needed to specify a single mailing
    /// address. When retrieving all available fields are filled
    /// in. When updating an existing address only non-null fields
    /// are updated. When adding a new address only non-null fields
    /// are used.
    /// </summary>
    public struct RpcAddress
    {
        public int ID;
        public string Type;
        public bool? Primary;
        public string StreetLine1;
        public string StreetLine2;
        public string City;
        public string State;
        public string PostalCode;
        public int? AreaID;
        public double? Latitude;
        public double? Longitude;
        public string Notes;
    }

    /// <summary>
    /// Defines an e-mail address in the system. When retrieving an
    /// e-mail address all available fields are filled in. When
    /// updating an address only non-null fields are updated and
    /// only non-null fields are used when creating a new e-mail
    /// address.
    /// </summary>
    public struct RpcEmail
    {
        public int ID;
        public string Email;
        public string Notes;
        public bool? Active;
    }

    /// <summary>
    /// Specifies the information needed to communicate a single
    /// phone number over RPC. On retrieval all available fields
    /// are filled in. When updating or creating a new phone number
    /// entry only non-null fields are used. Since no ID number is
    /// tied to phone number entries the Type must be set correctly
    /// to identify which phone they are adding or updating.
    /// </summary>
    public struct RpcPhone
    {
        public string Type;
        public string Number;
        public string Ext;
        public bool? Unlisted;
        public bool? Sms;
    }

    /// <summary>
    /// Identifies the contact information for a person. Each
    /// person can have zero, one or multiple postal addresses,
    /// e-mails and phone numbers. If access is denied to a
    /// specific type of contact information then that field
    /// may be completely unlisted.
    /// </summary>
    public struct RpcPersonContactInformation
    {
        public int PersonID;
        public RpcAddress[] Addresses;
        public RpcEmail[] Emails;
        public RpcPhone[] Phones;
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
    /// The information used in querying the database to find people
    /// records. Not all fields are used in a query, to see which fields
    /// are used and the order of the search see the FindPeople method.
    /// </summary>
    public struct RpcPeopleQuery
    {
        public string FirstName;
        public string LastName;
        public DateTime? BirthDate;
        public string Phone;
        public string Email;
        public int? AreaID;
        public bool? Staff;
    }

    /// <summary>
    /// Retrieve the basic information about a person. This structure
    /// follows the standard RPC retrieval and update rules.
    /// </summary>
    public struct RpcPersonInformation 
    {
        /// <summary>
        /// The person ID this information is for.
        /// </summary>
        public int PersonID;

        /// <summary>
        /// The person's First Name.
        /// </summary>
        public string FirstName;

        /// <summary>
        /// The person's Middle name.
        /// </summary>
        public string MiddleName;

        /// <summary>
        /// The person's Nick Name.
        /// </summary>
        public string NickName;

        /// <summary>
        /// The person's Last name.
        /// </summary>
        public string LastName;

        /// <summary>
        /// The title of the person (Mr., Mrs., etc.)
        /// </summary>
        public RpcLookup? Title;

        /// <summary>
        /// The suffix of the person (II, III, Jr., etc.)
        /// </summary>
        public RpcLookup? Suffix;

        /// <summary>
        /// The record status, Pending, Active, etc.
        /// </summary>
        public string RecordStatus;

        /// <summary>
        /// The member status.
        /// </summary>
        public RpcLookup? MemberStatus;

        /// <summary>
        /// The ID number of the campus this person is considered to
        /// be directly a part of.
        /// </summary>
        public int? CampusID;

        /// <summary>
        /// Boolean flag indicating if this person is a staff member.
        /// </summary>
        public bool? Staff;

        /// <summary>
        /// The Family ID number this person belongs to.
        /// </summary>
        public int? FamilyID;

        /// <summary>
        /// The person's date of birth.
        /// </summary>
        public DateTime? BirthDate;

        /// <summary>
        /// The persons age if available.
        /// </summary>
        public int? Age;

        /// <summary>
        /// The person's grade, if applicable.
        /// </summary>
        public int? Grade;

        /// <summary>
        /// The gender of this person as a string representation.
        /// </summary>
        public string Gender;

        /// <summary>
        /// The state of the person, married, single, etc.
        /// </summary>
        public RpcLookup? MaritalStatus;

        /// <summary>
        /// If the person is married this contains the ID number of
        /// the person's spouse.
        /// </summary>
        public int? SpouseID;

        /// <summary>
        /// The anniversary date of this person, it is possible for
        /// this field to be set even if the person is not "married".
        /// </summary>
        public DateTime? Anniversary;

        /// <summary>
        /// Flag specifying if this person contributes individually
        /// or as a family unit.
        /// </summary>
        public bool? ContributeIndividually;

        /// <summary>
        /// Flag specifying if this person is to be included when
        /// printing giving statements.
        /// </summary>
        public bool? PrintStatement;

        /// <summary>
        /// The envolope # of the person, if available.
        /// </summary>
        public int? EnvelopeNumber;

        /// <summary>
        /// Any medical information associated with the person.
        /// </summary>
        public string MedicalInformation;

        /// <summary>
        /// This value determines how active this person is.
        /// </summary>
        public int? ActiveMeter;

        /// <summary>
        /// The name of the person who created this person record.
        /// </summary>
        public string CreatedBy;

        /// <summary>
        /// The date this person record was created.
        /// </summary>
        public DateTime? DateCreated;

        /// <summary>
        /// The name of the person who last modified this person
        /// record.
        /// </summary>
        public string Modifiedby;

        /// <summary>
        /// The date this person record was last modified.
        /// </summary>
        public DateTime? DateModified;

        /// <summary>
        /// The date this person record was last verified.
        /// </summary>
        public DateTime? LastVerified;

        /// <summary>
        /// The reason this person's record has been marked as
        /// inactive, assuming it is marked inactive.
        /// </summary>
        public RpcLookup? InactiveReason;

        /// <summary>
        /// The date this person last attended some activity at
        /// the church.
        /// </summary>
        public DateTime? LastAttended;

        /// <summary>
        /// The URL to be used if this person is to be viewed
        /// in a web browser.
        /// </summary>
        public string NagivationUrl;

        /// <summary>
        /// The URL to be used to retrieve the person's image.
        /// </summary>
        public string ImageUrl;

        /// <summary>
        /// Other family members of this person's family. This
        /// list contains the ID of the person and not the full
        /// information about the person.
        /// </summary>
        public RpcFamilyMember[] FamilyMembers;

        /// <summary>
        /// The total number of peers for this person. Whatever this
        /// number is only the top 10 are put in the Peers member. You
        /// can retrieve more with the GetPersonPeers method.
        /// </summary>
        public int? PeerCount;

        /// <summary>
        /// The top 10 peers of this person. You can use the PeerCount
        /// member to determine if there are more peers to be retrieved.
        /// </summary>
        public RpcPeer[] Peers;

        /// <summary>
        /// The relationships that have been defined for this person.
        /// </summary>
        public RpcRelationship[] Relationships;
    }

    /// <summary>
    /// Retrieve the basic information about a group category. This
    /// structure follows the standard RPC retrieval and update rules.
    /// </summary>
    public struct RpcSmallGroupCategoryInformation
    {
        /// <summary>
        /// The unique ID number that identifies this group category.
        /// </summary>
        public int CategoryID;

        /// <summary>
        /// The name of this group category.
        /// </summary>
        public string Name;

        /// <summary>
        /// Flag to indicate if this group category allows registrations.
        /// I really don't remember what group registrations are for and
        /// would love to update this documentation.
        /// </summary>
        public bool? AllowRegistrations;

        /// <summary>
        /// Flag to indicate if this category allows bulk update operations
        /// to be performed on the members of any small groups of this type
        /// of category. I think.
        /// </summary>
        public bool? AllowBulkUpdate;

        /// <summary>
        /// Wether or not the history of this category type is private. I
        /// would assume this is a person's history in the group. I am
        /// guessing, that this keeps the history private from fellow group
        /// members but not the leader, but I may be wrong.
        /// </summary>
        public bool? HistoryIsPrivate;

        /// <summary>
        /// Wether membership in any small group of this category type should
        /// count as small group membership. For example, a team group
        /// probably does not count as a small group.
        /// </summary>
        public bool? CreditAsSmallGroup;

        /// <summary>
        /// Flag indicating if small groups of this category type should be
        /// assigned to a specific area.
        /// </summary>
        public bool? UsesArea;

        /// <summary>
        /// Allow, Assign, or some such, uniform number. Generally this flag
        /// would only be used if this group category has to do with teams.
        /// </summary>
        public bool? UseUniformNumber;

        /// <summary>
        /// The default role of new members in small groups of this type of
        /// category.
        /// </summary>
        public RpcLookup DefaultRole;

        /// <summary>
        /// The caption to be used with the PrimaryAge member of the
        /// RpcSmallGroupInformation structure.
        /// </summary>
        public string AgeGroupCaption;

        /// <summary>
        /// The caption to be used with the Description member of the
        /// RpcSmallGroupInformation structure.
        /// </summary>
        public string DescriptionCaption;

        /// <summary>
        /// The caption to be used with the LeaderID member of the
        /// RpcSmallGroupInformation structure.
        /// </summary>
        public string LeaderCaption;

        /// <summary>
        /// The caption to be used with the TargetLocationID member
        /// of the RpcSmallGroupInformation structure.
        /// </summary>
        public string LocationTargetCaption;

        /// <summary>
        /// The caption to be used with the PrimaryMaritalStatus member
        /// of the RpcSmallGroupInformation structure.
        /// </summary>
        public string MaritalPreferenceCaption;

        /// <summary>
        /// The caption to be used with the MaximumMembers member of the
        /// RpcSmallGroupInformation structure.
        /// </summary>
        public string MaximumMembersCaption;

        /// <summary>
        /// The caption to be used with the MeetingDay member of the
        /// RpcSmallGroupInformation structure.
        /// </summary>
        public string MeetingDayCaption;

        /// <summary>
        /// The caption to be used with the Name member of the
        /// RpcSmallGroupInformation structure.
        /// </summary>
        public string NameCaption;

        /// <summary>
        /// The caption to be used with the Notes member of the
        /// RpcSmallGroupInformation structure.
        /// </summary>
        public string NotesCaption;

        /// <summary>
        /// The caption to be used with the ParentID member of the
        /// RpcSmallGroupInformation structure.
        /// </summary>
        public string ParentCaption;

        /// <summary>
        /// The caption to be used with the PictureUrl member of the
        /// RpcSmallGroupInformation structure.
        /// </summary>
        public string PictureCaption;

        /// <summary>
        /// The caption to be used with the Schedule member of the
        /// RpcSmallGroupInformation structure.
        /// </summary>
        public string ScheduleCaption;

        /// <summary>
        /// The caption to be used with the Topic member of the
        /// RpcSmallGroupInformation structure.
        /// </summary>
        public string TopicCaption;

        /// <summary>
        /// The caption to be used with the TypeID member of the
        /// RpcSmallGroupInformation structure.
        /// </summary>
        public string TypeCaption;

        /// <summary>
        /// The caption to be used with the Url member of the
        /// RpcSmallGroupInformation structure.
        /// </summary>
        public string UrlCaption;

        /// <summary>
        /// An array of RpcLookups which list the valid roles members
        /// are allowed to take on for small groups of this category
        /// type.
        /// </summary>
        public RpcLookup[] ValidRoles;
    }

    /// <summary>
    /// Contains the basic information about a group cluster. This
    /// structure follows the standard RPC retrieval and update
    /// rules.
    /// </summary>
    public struct RpcSmallGroupClusterInformation
    {
        /// <summary>
        /// The Group Cluster ID that this information pertains to.
        /// </summary>
        public int ClusterID;

        /// <summary>
        /// The parent cluster ID of this cluster.
        /// </summary>
        public int? ParentID;

        /// <summary>
        /// The group category ID of this cluster.
        /// </summary>
        public int? CategoryID;

        /// <summary>
        /// The name of this cluster.
        /// </summary>
        public string Name;

        /// <summary>
        /// Flag which indicates wether or not this group cluster is
        /// active.
        /// </summary>
        public bool? Active;

        /// <summary>
        /// Retrieve the LevelID of this group cluster.
        /// TODO: What is this?
        /// </summary>
        public int? LevelID;

        /// <summary>
        /// Retrieve the TypeID of this group cluster.
        /// TODO: What is this?
        /// </summary>
        public int? TypeID;

        /// <summary>
        /// Description of this group cluster.
        /// </summary>
        public string Description;

        /// <summary>
        /// Notes that relate to this group cluster.
        /// </summary>
        public string Notes;

        /// <summary>
        /// The number of pending registrations in this group
        /// cluster and its descendents. This property is
        /// read-only.
        /// </summary>
        public int? RegistrationCount;

        /// <summary>
        /// The number of members in this group cluster and its
        /// descendents. This property is read-only.
        /// </summary>
        public int? MemberCount;

        /// <summary>
        /// The ID of the person who is the administrator of
        /// this group cluster.
        /// </summary>
        public int? AdminID;

        /// <summary>
        /// The ID of the person who is considered the leader of
        /// this group cluster.
        /// </summary>
        public int? LeaderID;

        /// <summary>
        /// The Area ID that this group cluster belongs to.
        /// </summary>
        public int? AreaID;

        /// <summary>
        /// The URL for this group cluster. This is not the same
        /// as the NavigationUrl. This is more like a groups
        /// website that might be outside the Arena system.
        /// </summary>
        public string Url;

        /// <summary>
        /// The name of the person who created this small group
        /// cluster.
        /// </summary>
        public string CreatedBy;

        /// <summary>
        /// The date this group cluster was created on.
        /// </summary>
        public DateTime DateCreated;

        /// <summary>
        /// The name of the last person to have modified this small
        /// group cluster.
        /// </summary>
        public string ModifiedBy;

        /// <summary>
        /// The date this group cluster was last modified on.
        /// </summary>
        public DateTime DateModified;

        /// <summary>
        /// The URL that can be used to navigate to this group
        /// cluster in a web browser.
        /// </summary>
        public string NavigationUrl;

        /// <summary>
        /// The URL that can be used to retrieve the group cluster's
        /// image.
        /// </summary>
        public string ImageUrl;
    }

    /// <summary>
    /// Contains the general information about a small group. This
    /// structure conforms to the standard RPC retrieval and update
    /// rules.
    /// </summary>
    public struct RpcSmallGroupInformation
    {
        /// <summary>
        /// The ID number of the small group that this information
        /// pertains to.
        /// </summary>
        public int GroupID;

        /// <summary>
        /// The ID number of the parent cluster of this small group.
        /// </summary>
        public int? ClusterID;

        /// <summary>
        /// The group category ID of this small group.
        /// </summary>
        public int? CategoryID;

        /// <summary>
        /// The name of this small group.
        /// </summary>
        public string Name;

        /// <summary>
        /// Flag specifying wether or not this small group is to be
        /// considered active.
        /// </summary>
        public bool? Active;

        /// <summary>
        /// The LevelID of this small group.
        /// TODO: What is this?
        /// </summary>
        public int? LevelID;

        /// <summary>
        /// The TypeID of this small group.
        /// TODO: What is this? Is this the CategoryID or a lookup?
        /// </summary>
        public int? TypeID;

        /// <summary>
        /// The description that has been associated with this small
        /// group.
        /// </summary>
        public string Description;

        /// <summary>
        /// A textual description of the schedule for this small
        /// group.
        /// </summary>
        public string Schedule;

        /// <summary>
        /// The notes about this small group.
        /// </summary>
        public string Notes;

        /// <summary>
        /// The number of pending registrations for this
        /// small group.
        /// </summary>
        public int? RegistrationCount;

        /// <summary>
        /// The total number of members in this small group.
        /// </summary>
        public int? MemberCount;

        /// <summary>
        /// The person who is considered the leader of this small
        /// group.
        /// </summary>
        public int? LeaderID;

        /// <summary>
        /// The ID number of the area that this small group resides
        /// in.
        /// </summary>
        public int? AreaID;

        /// <summary>
        /// The small groups custom website URL. This is not the same
        /// as the NavigationUrl.
        /// </summary>
        public string Url;

        /// <summary>
        /// The name of the person that created this small group.
        /// </summary>
        public string CreatedBy;

        /// <summary>
        /// The date that this small group was created on.
        /// </summary>
        public DateTime DateCreated;

        /// <summary>
        /// The name of the last person to have modified this small
        /// group.
        /// </summary>
        public string ModifiedBy;

        /// <summary>
        /// The date on which this small group was last modified on.
        /// </summary>
        public DateTime DateModified;

        /// <summary>
        /// The URL to be used for navigating to this small group
        /// in a web browser.
        /// </summary>
        public string NavigationUrl;

        /// <summary>
        /// The URL that can be used to retrieve the picture for
        /// this small group, if there is one.
        /// </summary>
        public string PictureUrl;

        /// <summary>
        /// The average age of the members of this small group.
        /// </summary>
        public double? AverageAge;

        /// <summary>
        /// The average distance from the Target Location of the
        /// members of this small group.
        /// </summary>
        public decimal? Distance;

        /// <summary>
        /// The ID number of the address record that identifies the
        /// location at which this small group meets at.
        /// </summary>
        public int? TargetLocationID;

        /// <summary>
        /// The lookup record which identifies the day(s) of the week
        /// that this small group meets on.
        /// </summary>
        public RpcLookup MeetingDay;

        /// <summary>
        /// The lookup record which identifies the primary age range
        /// of this small group.
        /// </summary>
        public RpcLookup PrimaryAge;

        /// <summary>
        /// The lookup record which identifies the suggested marital
        /// status of this small group.
        /// </summary>
        public RpcLookup PrimaryMaritalStatus;

        /// <summary>
        /// The lookup record which identifies the topic of discussion
        /// for this small group.
        /// </summary>
        public RpcLookup Topic;

        /// <summary>
        /// The maximum number of members that should be allowed in
        /// this small group.
        /// </summary>
        public int MaximumMembers;
    }

    /// <summary>
    /// Identifies an Arena lookup value. Not all details are
    /// specified in this structure, only those which are needed
    /// to quickly get the value (as a string) as well as determine
    /// the other allowed types via the TypeID, etc. This structure
    /// is currently read-only.
    /// </summary>
    public struct RpcLookup
    {
        /// <summary>
        /// Create an instance of the RpcLookup structure by taking the
        /// needed information from the Arena Lookup class.
        /// </summary>
        /// <param name="lookup">The Arena Lookup class to pull data from.</param>
        public RpcLookup(Lookup lookup)
        {
            this.LookupID = lookup.LookupID;
            this.TypeID = lookup.LookupTypeID;
            this.Value = lookup.Value;
        }

        /// <summary>
        /// The unique ID number of this lookup value.
        /// </summary>
        public int LookupID;

        /// <summary>
        /// The category of this lookup, which can be used to
        /// retrieve all other values.
        /// </summary>
        public int TypeID;

        /// <summary>
        /// The textual value of this lookup.
        /// </summary>
        public string Value;
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

    /// <summary>
    /// Identifies a single member of a family by his or her
    /// ID number.
    /// </summary>
    public struct RpcFamilyMember
    {
        /// <summary>
        /// The ID of the person who is being identified.
        /// </summary>
        public int PersonID;

        /// <summary>
        /// A string which identifies the formal name of this person, this
        /// name can and should be used when displaying the name of the
        /// person in a list to be chosen from for navigating to this person.
        /// </summary>
        public string FullName;

        /// <summary>
        /// Identifies the role of the person in the family.
        /// </summary>
        public RpcLookup Role;
    }

    /// <summary>
    /// Identifies a single member of a family by his or her
    /// RpcPersonInformation structure.
    /// </summary>
    public struct RpcFamilyMemberInformation
    {
        /// <summary>
        /// The person who is being identified.
        /// </summary>
        public RpcPersonInformation Person;

        /// <summary>
        /// A string which identifies the formal name of this person, this
        /// name can and should be used when displaying the name of the
        /// person in a list to be chosen from for navigating to this person.
        /// </summary>
        public string FullName;

        /// <summary>
        /// Identifies the role of the person in the family.
        /// </summary>
        public RpcLookup Role;
    }

    /// <summary>
    /// Identifies a single relationship to the person ID in this
    /// structure. The specific type of relationship is identified
    /// by the Relationship lookup member.
    /// </summary>
    public struct RpcRelationship
    {
        /// <summary>
        /// Identifies the person by ID number of this relationship.
        /// </summary>
        public int PersonID;

        /// <summary>
        /// A string which identifies the formal name of this person, this
        /// name can and should be used when displaying the name of the
        /// person in a list to be chosen from for navigating to this person.
        /// </summary>
        public string FullName;

        /// <summary>
        /// The lookup which identifies a given relationship.
        /// </summary>
        public string Relationship;
    }

    /// <summary>
    /// This structure identifies a family unit. It provides what
    /// little information is stored for a family in addition to
    /// the entire RpcPersonInformation structure for each member
    /// of the family.
    /// </summary>
    public struct RpcFamily
    {
        /// <summary>
        /// The ID number which identifies this family.
        /// </summary>
        public int FamilyID;

        /// <summary>
        /// The name of this family, this is not neccessarily the
        /// same as the last name of the family.
        /// </summary>
        public string FamilyName;

        /// <summary>
        /// This array identifies the members of this family.
        /// </summary>
        public RpcFamilyMemberInformation[] FamilyMembers;
    }

    #endregion
}
