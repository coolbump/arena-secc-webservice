
using System;
using System.Configuration;
using System.Collections;
using Arena.Portal;
using Arena.Core;
using Arena.Security;
using Arena.Enums;


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
        /// supported by the server. Currently this is 1.
        /// </summary>
        /// <returns>API Version</returns>
        static public RpcVersion Version()
        {
            RpcVersion version = new RpcVersion();

            version.Major = 1;
            version.Minor = 0;

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
        /// <returns></returns>
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
        /// Retrieves basic information about the given personID. The
        /// PersonInfo structure is filled as much as allowed by the
        /// users security level.
        /// </summary>
        /// <param name="personID">The ID number of the person to get the basic personal information of.</param>
        /// <returns>Dictionary containing personal information or PersonID key = -1 when not found.</returns>
        public RpcPersonInformation GetPersonInformation(int personID)
        {
            Person person;
            Hashtable info = new Hashtable();

            //
            // Find the person in question.
            //
            person = new Person(personID);

            //
            // Build the basic information, we default to all the values
            // that might not be set. If personID is -1 (not found) then
            // we don't need to continue.
            //
            info["PersonID"] = person.PersonID;
            if (person.PersonID == -1)
                return info;

            //
            // Retrieve all the fields the user has access to.
            //
            if (person.MemberStatus != null && PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Member_Status, OperationType.View))
            {
                info["MemberStatus"] = person.MemberStatus.Value;
            }
            if (PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Record_Status, OperationType.View))
            {
                info["RecordStatus"] = person.RecordStatus;
            }
            if (person.Campus != null && PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Campus, OperationType.View))
            {
                info["Campus"] = person.Campus.CampusId;
            }
            if (PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Staff_Member, OperationType.View))
            {
                info["Staff"] = person.StaffMember;
            }
            if (PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Name, OperationType.View))
            {
                info["FirstName"] = person.FirstName;
                info["LastName"] = person.LastName;
                if (person.MiddleName != "")
                {
                    info["MiddleName"] = person.MiddleName;
                }
                if (person.NickName != "")
                {
                    info["NickName"] = person.NickName;
                }
            }
            if (PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Family_Information, OperationType.View))
            {
                info["FamilyID"] = person.FamilyId;
            }
            if (person.BirthDate.Year != 1900 && PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_BirthDate, OperationType.View))
            {
                info["BirthDate"] = person.BirthDate;
            }
            if (person.Age != -1 && PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Age, OperationType.View))
            {
                info["Age"] = person.Age;
            }
            if (PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Gender, OperationType.View))
            {
                info["Gender"] = person.Gender;
            }
            if (person.MaritalStatus != null && PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Marital_Status, OperationType.View))
            {
                info["MaritalStatus"] = person.MaritalStatus.Value;
            }
            if (person.AnniversaryDate.Year != 1900 && PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Anniversary_Date, OperationType.View))
            {
                info["Anniversary"] = person.AnniversaryDate;
            }
            if (person.GraduationDate.Year != 1900 && PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Grade, OperationType.View))
            {
                /// TODO: Calculate the grade for storage.
            }
            if (PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Contribute_Individually, OperationType.View))
            {
                info["ContributeIndividually"] = person.ContributeIndividually;
            }
            if (PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Print_Statement, OperationType.View))
            {
                info["PrintStatement"] = person.PrintStatement;
            }
            if (person.EnvelopeNumber != -1 && PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Envelope_Number, OperationType.View))
            {
                info["Envelope"] = person.EnvelopeNumber;
            }
            if (person.MedicalInformation != "" && PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Medical_Info, OperationType.View))
            {
                info["Medical"] = person.MedicalInformation;
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
        /// Retrieve the basic information about a profile. If the profile
        /// is not found, or no access is permitted to the profile, then
        /// -1 is returned in the ProfileID member.
        /// </summary>
        /// <param name="profileID">The ID number of the profile to look up.</param>
        /// <returns>Basic profile information.</returns>
        public RpcProfileInformation GetProfileInformation(int profileID) { return null; }

        /// <summary>
        /// Retrieve detailed information about a profile. If the profile is
        /// not found, or no access is permitted to the profile, then -1 is
        /// returned in the ProfileID member.
        /// </summary>
        /// <param name="profileID">The ID number of the profile to look up.</param>
        /// <returns>Detailed profile information.</returns>
        public RpcProfileDetails GetProfileDetails(int profileID) { return null; }

        /// <summary>
        /// Retrieve all the ID numbers of the profiles directly beneath
        /// this profile.
        /// </summary>
        /// <param name="profileID">The ID number of the profile in question.</param>
        /// <returns>Integer array of the child profile ID numbers.</returns>
        public int[] GetProfileChildren(int profileID) { return null; }

        /// <summary>
        /// Rerieves the profile ID numbers of all root level profiles of
        /// the given profile type.
        /// </summary>
        /// <param name="profileType">The integer value of the profile type.</param>
        /// <returns>Integer array of the root profiles.</returns>
        public int[] GetProfileRoots(int profileType) { return null; }

        /// <summary>
        /// Get the people ID numbers of all members of this profile.
        /// </summary>
        /// <param name="profileID">Profile to retrieve member list from.</param>
        /// <returns>Integer array of people IDs.</returns>
        public int[] GetProfileMembers(int profileID) { return null; }

        /// <summary>
        /// Get a list of all occurence IDs for a profile.
        /// </summary>
        /// <param name="profileID">The profile to list occurences of.</param>
        /// <returns>Integer array of occurrence IDs.</returns>
        public int[] GetProfileOccurrences(int profileID) { return null; }

        #endregion

        public int[] GetSmallGroupClusters(int clusterID) { return null; }
        public RpcSmallGroupClusterDetails GetSmallGroupClusterDetails(int clusterID) { return null; }
        public int[] GetSmallGroups(int clusterID) { return null; }
        public RpcSmallGroupDetails GetSmallGroupDetails(int groupID) { return null; }
        public int[] GetSmallGroupMembers(int groupID) { return null; }
        public int[] GetSmallGroupOccurrences(int groupID) { return null; }

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
    }

    /// <summary>
    /// This structure contains the details about a profile, not including
    /// those already specified in RpcProfileInformation. This structure
    /// follows the standard RPC retrieval and updating rules.
    /// </summary>
    public struct RpcProfileDetails
    {
        /// <summary>
        /// The profile ID this detail record is referencing.
        /// </summary>
        public int ProfileID;

        /// <summary>
        /// The person login that owns this profile.
        /// </summary>
        public string Owner;

        /// <summary>
        /// The person login that created this profile.
        /// </summary>
        public int CreatedBy;

        /// <summary>
        /// The date and time this profile as initially created.
        /// </summary>
        public DateTime DateCreated;
        
        /// <summary>
        /// The person login who last modified this profile.
        /// </summary>
        public int ModifiedBy;

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
        public DateTime BirthDate;
        public string Phone;
        public string Email;
        public int AreaID;
        public bool Staff;
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
        public string Title;

        /// <summary>
        /// The suffix of the person (II, III, Jr., etc.)
        /// </summary>
        public string Suffix;

        /// <summary>
        /// The record status, Pending, Active, etc.
        /// </summary>
        public string RecordStatus;

        /// <summary>
        /// The member status.
        /// </summary>
        public RpcLookup MemberStatus;

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
        public DateTime BirthDate;

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
    }

    /// <summary>
    /// This structure provides more detailed information about a
    /// person in the database. This is generally information that
    /// is used less often and thus a waste of bandwidth to send
    /// it everytime. This structure follows the standard RPC
    /// retrieval and update rules.
    /// </summary>
    public struct RpcPersonDetails
    {
        /// <summary>
        /// The ID number of the person this detail information refers
        /// to.
        /// </summary>
        public int PersonID;

        /// <summary>
        /// The state of the person, married, single, etc.
        /// </summary>
        public RpcLookup MaritalStatus;

        /// <summary>
        /// If the person is married this contains the ID number of
        /// the person's spouse.
        /// </summary>
        public int? SpouseID;

        /// <summary>
        /// The anniversary date of this person, it is possible for
        /// this field to be set even if the person is not "married".
        /// </summary>
        public DateTime Anniversary;

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
        public DateTime DateCreated;

        /// <summary>
        /// The name of the person who last modified this person
        /// record.
        /// </summary>
        public string Modifiedby;

        /// <summary>
        /// The date this person record was last modified.
        /// </summary>
        public DateTime DateModified;

        /// <summary>
        /// The date this person record was last verified.
        /// </summary>
        public DateTime LastVerified;

        /// <summary>
        /// The reason this person's record has been marked as
        /// inactive, assuming it is marked inactive.
        /// </summary>
        public string InactiveReason;

        /// <summary>
        /// The date this person last attended some activity at
        /// the church.
        /// </summary>
        public DateTime LastAttended;

        /// <summary>
        /// The URL to be used if this person is to be viewed
        /// in a web browser.
        /// </summary>
        public string NagivationUrl;

        /// <summary>
        /// The URL to be used to retrieve the person's image.
        /// </summary>
        public string ImageUrl;
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
    }

    /// <summary>
    /// Contains detailed information about a group cluster. This
    /// structure conforms to the standard RPC retrieval and update
    /// rules.
    /// </summary>
    public struct RpcSmallGroupClusterDetails
    {
        /// <summary>
        /// The cluster ID that this detail record provides
        /// information about.
        /// </summary>
        public int ClusterID;

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
        /// The ID number of the parent group, or I believe rather
        /// the parent group cluster, of this small group.
        /// </summary>
        public int? ParentID;

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
        /// TODO: What is this?
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
    }

    /// <summary>
    /// Contains the more detailed information about a small group.
    /// This structure conforms to the standard RPC retrieval and
    /// update rules.
    /// </summary>
    public struct RpcSmallGroupDetails
    {
        /// <summary>
        /// The ID number of the small group that this structure is
        /// providing detailed information about.
        /// </summary>
        public int GroupID;

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
        /// The URL that can be used to retrieve the image for
        /// this small group, if there is one.
        /// </summary>
        public string ImageUrl;

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
    /// to quickly get the value (as a string) and then be able
    /// to get more information, if required, via the
    /// RpcLookupDetails structure. This structure is currently
    /// read-only.
    /// </summary>
    public struct RpcLookup
    {
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

    #endregion
}
