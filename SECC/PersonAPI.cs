
using System;
using System.Collections.Generic;
using System.Configuration;
using System.ServiceModel.Web;
using Arena.Core;
using Arena.Security;
using Arena.SmallGroup;
using Arena.Services.Contracts;
using System.Data.SqlClient;
using Arena.DataLayer.SmallGroup;
using System.Xml.Serialization;
using Arena.Services.Exceptions;
using Arena.Event;
using System.Text.RegularExpressions;
using Arena.Custom.SECC.Common.Util;
using Arena.Custom.SECC.Common.Data;
using System.Linq;
using System.IO;
using System.Collections.Specialized;
using System.Web;
using System.Security.Principal;

namespace Arena.Custom.HDC.WebService.SECC
{
    class PersonAPI
    {
        /// <summary>
        /// <b>POST person/{id}/attribute/update</b>
        ///
        /// Update person attributes
        /// </summary>
        /// <returns>ModifyResult.</returns>
        [WebInvoke(Method = "POST",
                UriTemplate = "person/add")]
        public Arena.Services.Contracts.ModifyResult AddPerson(System.IO.Stream stream)
        {
            
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(Arena.Services.Contracts.Person));
            Arena.Services.Contracts.Person person = (Arena.Services.Contracts.Person)xmlSerializer.Deserialize(stream);

            Contracts.PersonMapper pm = new Contracts.PersonMapper();
            return pm.Create(person);

            //Arena.Services.Contracts.Person
        }


        /// <summary>
        /// <b>POST me/fields={fields}</b>
        ///
        /// Retrieve a person from Arena
        /// \since 1.0.0
        /// </summary>
        /// <returns>List of FamilyMember objects.</returns>
        [RestApiAnonymous]
        [WebInvoke(Method = "POST",
                UriTemplate = "me?fields={fields}")]
        public Arena.Services.Contracts.Person getMe(Stream input, string fields)
        {
            string body = new StreamReader(input).ReadToEnd();
            NameValueCollection postVars = HttpUtility.ParseQueryString(body);
            
            // Verify we have a valid API key
            if (!postVars.AllKeys.Contains("api_key"))
            {
                throw new BadRequestException("Parameter api_key is required.");
            }

            // Check the API Key
            ApiApplication apiApplication = new ApiApplication(new Guid(postVars["api_key"]));
            if (apiApplication.ApplicationId == -1)
            {
                throw new ApplicationException("Invalid api_key");
            }
            try
            {

                // Make sure we have a username/password and authenticate
                if (postVars.AllKeys.Contains("username") && (postVars.AllKeys.Contains("password")))
                {
                    Int32 personId = Arena.Portal.PortalLogin.Authenticate(postVars["username"], postVars["password"], postVars["api_key"], 1);
                    if (personId > 0)
                    {
                        ArenaContext.Current.SetWebServiceProperties(ArenaContext.Current.CreatePrincipal(postVars["username"]), new Arena.Core.Person(personId));
                        return GetPerson(Arena.Core.ArenaContext.Current.Person.PersonID, fields);
                    }
                    else
                    {
                        throw new AuthenticationException("Unknown authentication error");
                    }
                }
                else
                {
                    throw new BadRequestException("Username and Password are required.");
                }
            }
            catch (Exception e)
            {
                throw new AuthenticationException(e.Message);
            }
        }
        
        /// <summary>
        /// <b>GET me?fields={fields}</b>
        ///
        /// Retrieve me information from Arena
        /// \since 1.0.0
        /// </summary>
        /// <returns>Data about the current authenticated user.</returns>
        [WebGet(UriTemplate = "me?fields={fields}")]
        public Arena.Services.Contracts.Person getMe(string fields)
        {
            return GetPerson(Arena.Core.ArenaContext.Current.Person.PersonID, fields);
        }

        /// <summary>
        /// <b>GET person/{id}</b>
        ///
        /// Retrieve a person from Arena
        /// \since 1.0.0
        /// </summary>
        /// <returns>List of FamilyMember objects.</returns>
        [WebGet(UriTemplate = "person/{id}?fields={fields}")]
        public Arena.Services.Contracts.Person GetPerson(int id, string fields)
        { 
            List<string> strs;
            Arena.Core.Person arenaPersonFromID = new Arena.Core.Person(id);
            if (arenaPersonFromID == null)
            {
                throw new ResourceNotFoundException("Invalid person id");
            }
            if (!string.IsNullOrEmpty(fields))
            {
                char[] chrArray = new char[] { ',' };
                strs = new List<string>(fields.Split(chrArray));
            }
            else
            {
                strs = null;
            }
            Contracts.PersonMapper personMapper = new Contracts.PersonMapper(strs);
            
            return personMapper.FromArena(arenaPersonFromID);
        }

        /// <summary>
        /// <b>GET person/{id}/familymembers</b>
        ///
        /// Retrieve a list of all familymembers for the given person ID.
        /// \since 1.0.0
        /// </summary>
        /// <returns>List of FamilyMember objects.</returns>
        [WebGet(UriTemplate = "person/{id}/familymembers")]
        public Contracts.GenericListResult<Contracts.FamilyMember> GetPersonFamilyMembers(int id)
        {
            Contracts.GenericListResult<Contracts.FamilyMember> list = new Contracts.GenericListResult<Contracts.FamilyMember>();
            Contracts.FamilyMemberMapper mapper = new Contracts.FamilyMemberMapper();

            var person = new Arena.Core.Person(id);

            list.Items = new List<Contracts.FamilyMember>();
            list.Max = list.Total = person.Family().FamilyMembers.Count;
            list.Start = 0;
            foreach (FamilyMember member in person.Family().FamilyMembers)
            {
                list.Items.Add(mapper.FromArena(member));
            }

            return list;
        }

        /// <summary>
        /// <b>GET person/{id}/primaryemail</b>
        ///
        /// Get a person's primary email address.
        /// \since 1.0.0
        /// </summary>
        /// <returns>Primary email address.</returns>
        [WebGet(UriTemplate = "person/{id}/primaryemail")]
        public Email GetPersonPrimaryEmail(int id)
        {
            var retEmail = new Email();
            var person = new Arena.Core.Person(id);
            if (person.Emails.Active.Count > 0) {
                // Get the person's first active email
                PersonEmail email = person.Emails.Active[0];
                foreach(PersonEmail eml in person.Emails.Active)
                {
                    // Now just make sure we have the one with the lowest order
                    if (eml.Order < email.Order) {
                        email = eml;
                    }
                }
                retEmail.Address = email.Email.ToString();
            }
            return retEmail;
        }


        /// <summary>
        /// <b>GET person/{id}/previousids</b>
        /// 
        // Get a person's previous (old) PersonIDs
        /// </summary>
        /// <param name="id">The person's current PersonId.</param>
        /// <returns>A list of the person's previous PersonIds</returns>
        [WebGet(UriTemplate = "person/{id}/previousids")]
        public Contracts.GenericListResult<int> GetPersonPreviousIds( int id )
        {
            List<int> previousIds = Arena.Custom.SECC.OAuth.PersonMerges.GetOldPersonIds( id );

            Contracts.GenericListResult<int> list = new Contracts.GenericListResult<int>();
            list.Items = previousIds;
            list.Max = list.Total = previousIds.Count;
            list.Start = 0;

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
        [WebGet(UriTemplate = "person/{id}/groupleadership/list?clusterTypeId={clusterTypeId}&start={start}&max={max}")]
        public Contracts.GenericListResult<Contracts.SmallGroup> GetPersonSmallGroupLeadership(int id, int clusterTypeId, int start, int max)
        {
            Contracts.GenericListResult<Contracts.SmallGroup> list = new Contracts.GenericListResult<Contracts.SmallGroup>();
            list.Items = new List<Contracts.SmallGroup>();
            list.Start = start;


            // Instantiate the mapper
            Contracts.SmallGroupMapper sgm = new Contracts.SmallGroupMapper();

            Arena.Custom.SECC.Data.SmallGroup.GroupCollection gc = new Arena.Custom.SECC.Data.SmallGroup.GroupCollection();
            gc.LoadByLeaderPersonID(ArenaContext.Current.Person.PersonID);

            foreach (Arena.SmallGroup.Group g in gc)
            {
                if (clusterTypeId > 0 && g.ClusterTypeID != clusterTypeId)
                {
                    continue;
                }

                // Make sure they have group cluster info if they aren't querying for themselves.
                if (ArenaContext.Current.Person.PersonID != id) { 
                    if (RestApi.GroupClusterOperationAllowed(ArenaContext.Current.Person.PersonID, g.GroupClusterID, OperationType.View) == false)
                        continue;
                }

                // Make sure this person is an active member of the group
                bool active = true;
                foreach (Arena.SmallGroup.GroupMember gm in g.Members)
                {
                    if (!gm.Active && gm.PersonID == ArenaContext.Current.Person.PersonID)
                    {
                        active = false;
                    }
                }
                if (!active)
                {
                    continue;
                }

                if (list.Total >= start && (list.Items.Count < max || max <= 0))
                    list.Items.Add(sgm.FromArena(g));
            }
            list.Max = list.Total = list.Items.Count;

            return list;
        }


        /// <summary>
        /// Retrieve a list of all event tags that this person is in.
        /// </summary>
        /// <param name="id">The ID number of the person to retrieve membership of.</param>
        /// <param name="start">The start index to begin retrieving records at.</param>
        /// <param name="max">The maximum number of records to retrieve.</param>
        /// <returns>GenericListResult of GenericReference objects.</returns>
        [WebGet(UriTemplate = "person/{id}/event/list")]
        public Contracts.GenericListResult<Contracts.GenericReference> GetPersonEvents(int id)
        {
            EventProfileCollection eventProfileCollection = new EventProfileCollection();
            eventProfileCollection.LoadMemberEventProfiles(Arena.Core.ArenaContext.Current.Organization.OrganizationID, id, false);
            //EventProfileCollection.RemoveInactiveProfiles();

            Contracts.GenericListResult<Contracts.GenericReference> events = new Contracts.GenericListResult<Contracts.GenericReference>();
            events.Items = new List<Contracts.GenericReference>();
            Contracts.EventMapper eventMapper = new Contracts.EventMapper();
            foreach (EventProfile eProfile in eventProfileCollection)
            {
                if (eProfile.DisplayAttendedOnly)
                {
                    if (new Arena.DataLayer.Core.OccurrenceData().GetProfileAttendance(eProfile.ProfileID, id, Arena.Core.ArenaContext.Current.Organization.OrganizationID) > 0)
                        events.Items.Add(new Contracts.GenericReference(eProfile));
                }
                else
                {
                    events.Items.Add(new Contracts.GenericReference(eProfile));
                }
            }
            events.Max = events.Total = events.Items.Count;
            events.Start = 0;

            return events;
        }

        /// <summary>
        /// <b>POST person/imin</b>
        ///
        /// Update person attributes
        /// </summary>
        /// <returns>ModifyResult.</returns>
        [WebInvoke(Method = "POST",
                UriTemplate = "person/imin")]
        [RestApiAnonymous]
        public Arena.Services.Contracts.ModifyResult ImIn(Contracts.ImIn imIn)
        {
            ModifyResult result = new ModifyResult();
            result.Successful = true.ToString();
            result.Link = "/person/1234";

            // First Name
            if (String.IsNullOrEmpty(imIn.FirstName))
            {
                result.Successful = false.ToString();
                result.ValidationResults.Add(new ModifyValidationResult() { Key = "FirstNameMissing", Message = "First Name is required." });
                result.Link = null;
            }

            // Last Name
            if (String.IsNullOrEmpty(imIn.LastName))
            {
                result.Successful = false.ToString();
                result.ValidationResults.Add(new ModifyValidationResult() { Key = "LastNameMissing", Message = "Last Name is required." });
                result.Link = null;
            }

            // DOB
            if (imIn.DateOfBirth == null || imIn.DateOfBirth == default(DateTime))
            {
                result.Successful = false.ToString();
                result.ValidationResults.Add(new ModifyValidationResult() { Key = "DOBMissingOrInvalid", Message = "Date of Birth required and must be valid." });
                result.Link = null;
            }

            // Phone Number
            if (String.IsNullOrEmpty(imIn.PhoneNumber))
            {
                result.Successful = false.ToString();
                result.ValidationResults.Add(new ModifyValidationResult() { Key = "PhoneNumberMissing", Message = "Phone Number is required." });
                result.Link = null;
            }
            else
            {
                // Match the phone number format
                if (!Regex.Match(imIn.PhoneNumber, @"^(\([2-9]\d\d\)|[2-9]\d\d) ?[-.,]? ?[2-9]\d\d ?[-.,]? ?\d{4}$").Success)
                {
                    result.Successful = false.ToString();
                    result.ValidationResults.Add(new ModifyValidationResult() { Key = "PhoneNumberInvalid", Message = "Phone Number format is invalid: (502) 253-8000" });
                    result.Link = null;
                }
            } 
            
            // Email
            if (!String.IsNullOrEmpty(imIn.Email))
            {
                // No more required email
                /*    result.Successful = false.ToString();
                    result.ValidationResults.Add(new ModifyValidationResult() { Key = "EmailMissing", Message = "Email is required." });
                    result.Link = null;
                }
                else
                {*/
                // Match the email format
                if (!Regex.Match(imIn.Email, @"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$").Success)
                {
                    result.Successful = false.ToString();
                    result.ValidationResults.Add(new ModifyValidationResult() { Key = "EmailInvalid", Message = "Email format is invalid: person@secc.org" });
                    result.Link = null;
                }
            }

            
            // Phone Type
            Lookup phoneType = new Lookup();
            // Get all the phone number types
            LookupCollection lc = new LookupCollection();
            lc.LoadByType(38);
            String activeTypes = lc.Where(e => e.Active).Select(e => e.Value).Aggregate((current, next) => current + ", " + next);

            if (String.IsNullOrEmpty(imIn.PhoneType))
            {
                result.Successful = false.ToString();
                result.ValidationResults.Add(new ModifyValidationResult() { Key = "PhoneTypeInvalid", Message = "Phone type is invalid (" + activeTypes + ")" });
                result.Link = null;
            } 
            else 
            {
                phoneType = lc.Where(e => e.Value == imIn.PhoneType).FirstOrDefault();

                if (phoneType == null)
                {
                    result.Successful = false.ToString();
                    result.ValidationResults.Add(new ModifyValidationResult() { Key = "PhoneTypeInvalid", Message = "Phone type is invalid (" + activeTypes + ")" });
                    result.Link = null;
                }
            }

            // Validate the campus
            if (String.IsNullOrEmpty(imIn.Campus))
            {
                result.Successful = false.ToString();
                result.ValidationResults.Add(new ModifyValidationResult() { Key = "CampusInvalid", Message = "Campus is required." });
                result.Link = null;
            }



            if (result.Successful == true.ToString())
            {

                Arena.Custom.SECC.Common.Data.Person.ImIn imInTarget = new Arena.Custom.SECC.Common.Data.Person.ImIn();
                AutoMapper.Mapper.CreateMap<Contracts.ImIn, Arena.Custom.SECC.Common.Data.Person.ImIn>();
                AutoMapper.Mapper.Map<Contracts.ImIn, Arena.Custom.SECC.Common.Data.Person.ImIn>(imIn, imInTarget);
                Arena.Custom.SECC.Common.Util.ImIn imInUtil = new Arena.Custom.SECC.Common.Util.ImIn();
                if (!imInUtil.process(imInTarget))
                {
                    result.Successful = false.ToString();
                    result.ValidationResults.Add(new ModifyValidationResult() { Key = "ImInGeneralError", Message = "Something went wrong processing the I'm In request." });
                }

            }

            return result;

        }

    }
}
