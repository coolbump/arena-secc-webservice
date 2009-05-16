
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
    /// RPC providers.
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
        public CoreRpc(IDictionary credentials)
        {
            currentLogin = LoginForCredentials(credentials);
        }

        /// <summary>
        /// Returns the version of the Arena Web Service API protocol
        /// supported by the server. Currently this is 1.
        /// </summary>
        /// <returns>API Version</returns>
        static public int Version()
        {
            return 1;
        }

        /// <summary>
        /// Queries the database to find all matching personIDs, which
        /// are returned as an integer array. Only a single type of
        /// search is performed in order of precedence by: Name and
        /// birthdate; name; phone; email; birthdate; area; profiles.
        /// </summary>
        /// <param name="query">Provides the filter to use when searching for people.</param>
        /// <returns></returns>
        public int[] FindPeople(IDictionary query)
        {
            PersonCollection people;
            ArrayList personIDs;
            int i;

            //
            // Find all the people matching the query.
            //
            people = new PersonCollection();
            if (query["FirstName"] != null && query["LastName"] != null)
                people.LoadByName((string)query["FirstName"], (string)query["LastName"]);

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
        public IDictionary GetPersonInformation(int personID)
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
        public IDictionary GetPersonContactInformation(int personID)
        {
            Person person;
            Hashtable contact = new Hashtable(), hash;
            ArrayList addressList, phoneList, emailList;
            int i;

            //
            // Find the person in question.
            //
            person = new Person(personID);
            contact["PersonID"] = person.PersonID;

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
                        hash = new Hashtable();
                        if (person.Addresses[i].AddressType != null)
                            hash["AddressType"] = person.Addresses[i].AddressType.Value;
                        hash["Primary"] = person.Addresses[i].Primary;
                        if (person.Addresses[i].Address.StreetLine1 != "")
                            hash["StreetLine1"] = person.Addresses[i].Address.StreetLine1;
                        if (person.Addresses[i].Address.StreetLine2 != "")
                            hash["StreetLine2"] = person.Addresses[i].Address.StreetLine2;
                        if (person.Addresses[i].Address.City != "")
                            hash["City"] = person.Addresses[i].Address.City;
                        if (person.Addresses[i].Address.State != "")
                            hash["State"] = person.Addresses[i].Address.State;
                        if (person.Addresses[i].Address.PostalCode != "")
                            hash["PostalCode"] = person.Addresses[i].Address.PostalCode;
                        if (person.Addresses[i].Address.Area != null)
                            hash["AreaID"] = person.Addresses[i].Address.Area.AreaID;
                        if (person.Addresses[i].Address.Latitude != 0)
                            hash["Latitude"] = person.Addresses[i].Address.Latitude;
                        if (person.Addresses[i].Address.Longitude != 0)
                            hash["Longitude"] = person.Addresses[i].Address.Longitude;
                        if (person.Addresses[i].Notes != "")
                            hash["Notes"] = person.Addresses[i].Notes;

                        addressList.Add(hash);
                    }
                    contact["Addresses"] = addressList;
                }

                if (PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Phones, OperationType.View) == true)
                {
                    //
                    // Build all the phones.
                    //
                    phoneList = new ArrayList(person.Phones.Count);
                    for (i = 0; i < person.Phones.Count; i++)
                    {
                        hash = new Hashtable();
                        if (person.Phones[i].PhoneType != null)
                            hash["PhoneType"] = person.Phones[i].PhoneType.Value;
                        if (person.Phones[i].Number != "")
                            hash["Number"] = person.Phones[i].Number;
                        if (person.Phones[i].Extension != "")
                            hash["PhoneType"] = person.Phones[i].Extension;
                        hash["Unlisted"] = person.Phones[i].Unlisted;
                        hash["SMS"] = person.Phones[i].SMSEnabled;

                        phoneList.Add(hash);
                    }
                    contact["Phones"] = phoneList;
                }

                if (PersonFieldOperationAllowed(currentLogin.PersonID, PersonFields.Profile_Emails, OperationType.View) == true)
                {
                    //
                    // Build all the emails.
                    //
                    emailList = new ArrayList(person.Emails.Count);
                    for (i = 0; i < person.Emails.Count; i++)
                    {
                        hash = new Hashtable();
                        if (person.Emails[i].Email != "")
                            hash["Email"] = person.Emails[i].Email;
                        if (person.Emails[i].Notes != "")
                            hash["Notes"] = person.Emails[i].Notes;
                        hash["Active"] = person.Emails[i].Active;

                        emailList.Add(hash);
                    }
                    contact["Emails"] = emailList;
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
        public IDictionary GetPersonProfiles(int personID)
        {
            Person person;
            ArrayList profileIDs;
            Hashtable hash;
            ProfileCollection collection;
            int i;

            //
            // Find the person in question.
            //
            person = new Person(personID);
            hash = new Hashtable();
            
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
                    hash["ministry"] = profileIDs.ToArray(typeof(int));
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
                    hash["serving"] = profileIDs.ToArray(typeof(int));
                }
            }

            return hash;
        }

        public IDictionary GetProfileInformation(IDictionary credentials, int profileID) { return null; }
        public int[] GetProfileChildren(int profileID) { return null; }
        public int[] GetProfileRoots(int profileType) { return null; }
        public int[] GetProfileMembers(int profileID) { return null; }
        public int[] GetProfileOccurrences(int profileID) { return null; }

        public int[] GetSmallGroupClusters(int clusterID) { return null; }
        public IDictionary GetSmallGroupClusterDetails(int clusterID) { return null; }
        public int[] GetSmallGroups(int clusterID) { return null; }
        public IDictionary GetSmallGroupDetails(int groupID) { return null; }
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
        private Login LoginForCredentials(IDictionary credentials)
        {
            Login loginUser;

            loginUser = new Login((string)credentials["UserName"]);
//            if (loginUser.IsAccountLocked() == true || loginUser.AuthenticateInDatabase((string)credentials["Password"]) == false)
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
}
