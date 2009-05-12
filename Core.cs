using System;
using System.Configuration;
using System.Collections;
using Arena.Portal;
using Arena.Core;
using Arena.Security;
using Arena.Enums;


namespace Arena.Custom.HDC.WebService
{
    public class Core
    {
        #region int[] Version()
        //
        // Version
        //
        static public int Version()
        {
            return 1;
        }
        #endregion

        #region int[] FindPeople(credentials, query)
        //
        // GetPersonID
        //
        static public int[] FindPeople(IDictionary credentials, IDictionary query)
        {
            PersonCollection people;
            ArrayList personIDs;
            Login loginUser;
            int i;

            //
            // Attempt to login.
            //
            loginUser = LoginForCredentials(credentials);

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
        #endregion

        #region IDictionary GetPersonInformation(credentials, personID)
        //
        // GetPersonInformation
        //
        static public IDictionary GetPersonInformation(IDictionary credentials, int personID)
        {
            Login loginUser;
            Person person;
            Hashtable info = new Hashtable();

            //
            // Log the user in and find the person in question.
            //
            loginUser = LoginForCredentials(credentials);
            person = new Person(personID);

            //
            // Build the basic information, we default to all the values
            // that might not be set. If personID is -1 (not found) then
            // we don't need to continue.
            //
            info["PersonID"] = person.PersonID;
            info["Age"] = -1;
            info["BirthDate"] = new DateTime(1900, 1, 1);
            if (person.PersonID == -1)
                return info;

            //
            // Retrieve all the fields the user has access to.
            //
            if (PersonFieldOperationAllowed(loginUser.PersonID, PersonFields.Profile_Name, OperationType.View))
            {
                info["FirstName"] = person.FirstName;
                info["LastName"] = person.LastName;
                info["MiddleName"] = person.MiddleName;
                info["NickName"] = person.NickName;
            }
            if (PersonFieldOperationAllowed(loginUser.PersonID, PersonFields.Profile_BirthDate, OperationType.View))
            {
                info["BirthDate"] = person.BirthDate;
            }
            if (PersonFieldOperationAllowed(loginUser.PersonID, PersonFields.Profile_Age, OperationType.View))
            {
                info["Age"] = person.Age;
            }

            return info;
        }
        #endregion

        #region IDictionary GetPersonContactInformation(credentials, personID)
        //
        // GetPersonContactInformation
        //
        static public IDictionary GetPersonContactInformation(IDictionary credentials, int personID)
        {
            Login loginUser;
            Person person;
            Hashtable contact = new Hashtable(), hash;
            ArrayList addressList, phoneList, emailList;
            int i;

            //
            // Log the user in and find the person in question.
            //
            loginUser = LoginForCredentials(credentials);
            person = new Person(personID);

            if (PersonFieldOperationAllowed(loginUser.PersonID, PersonFields.Profile_Addresses, OperationType.View) == true)
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

            if (PersonFieldOperationAllowed(loginUser.PersonID, PersonFields.Profile_Phones, OperationType.View) == true)
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

            if (PersonFieldOperationAllowed(loginUser.PersonID, PersonFields.Profile_Emails, OperationType.View) == true)
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

            return contact;
        }
        #endregion

        #region int[] GetPersonProfiles(credentials, personID)
        //
        // GetPersonProfiles
        //
        static public int[] GetPersonProfiles(IDictionary credentials, int personID)
        {
            Login loginUser;
            Person person;
            ArrayList personIDs;
            ProfileCollection collection;
            int i;

            //
            // Log the user in and find the person in question.
            //
            loginUser = LoginForCredentials(credentials);
            person = new Person(personID);

            //
            // Load up the profiles for this person.
            //
            collection = new ProfileCollection();
            collection.LoadMemberProfiles(DefaultOrganizationID(), ProfileType.Ministry, personID, true);

            //
            // Load all the profile IDs and return them as an
            // integer array.
            //
            personIDs = new ArrayList(collection.Count);
            for (i = 0; i < collection.Count; i++)
            {
                personIDs.Add(collection[i].ProfileID);
            }

            return (int[])personIDs.ToArray(typeof(int));
        }
        #endregion

        #region Login LoginForCredentials(credentials)
        //
        // Attempt to log the given user in using the credentials provided.
        // Upon a successful login an Arena Login object is returned to identify
        // the user. If the credentials are not valid then an exception is thrown.
        //
        static private Login LoginForCredentials(IDictionary credentials)
        {
            Login loginUser;

            loginUser = new Login((string)credentials["UserName"]);
//            if (loginUser.IsAccountLocked() == true || loginUser.AuthenticateInDatabase((string)credentials["Password"]) == false)
//                throw new UnauthorizedAccessException("Invalid username or password.");

            return loginUser;
        }
        #endregion

        #region Private methods for validating security.
        //
        // Check if the given personID has access for the operation to the
        // given person field.
        //
        static private bool PersonFieldOperationAllowed(int personID, int field, OperationType operation)
        {
            PermissionCollection permissions;

            //
            // Load the permissions.
            //
            permissions = new PermissionCollection(ObjectType.PersonField, field);
            
            return PermissionsOperationAllowed(permissions, personID, operation);
        }

        //
        // Check if the given personID has access for the operation to the
        // given profile (tag).
        //
        static private bool ProfileOperationAllowed(int personID, int profileID, OperationType operation)
        {
            PermissionCollection permissions;

            //
            // Load the permissions.
            //
            permissions = new PermissionCollection(ObjectType.Tag, profileID);

            return PermissionsOperationAllowed(permissions, personID, operation);
        }

        //
        // Determine if the given personID(subject) is allowed to perform the
        // operation in the PermissionCollection.
        //
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


        //
        // Retrieve the default organization ID for this
        // web service. This is retrieved via the Organization
        // application setting in the web.config file.
        //
        static public int DefaultOrganizationID()
        {
            return Convert.ToInt32(ConfigurationSettings.AppSettings["Organization"]);
        }
    }
}
