using Arena.DataLayer.Core;
using Arena.Services.Contracts;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Arena.Custom.SECC.Common.Util;
using Arena.Security;
using Arena.Enums;

namespace Arena.Custom.HDC.WebService.Contracts
{
    
    public class PersonMapper : BaseMapper
    {
        private List<string> _includeFields;
        private static Dictionary<string, int> _fieldSecurityMap;

        static PersonMapper()
        {
            PersonMapper._fieldSecurityMap = new Dictionary<string, int>()
            {
                { "PersonID".ToUpperInvariant(), PersonFields.Profile_PersonID },
                { "PersonGUID".ToUpperInvariant(), PersonFields.Profile_PersonID },
                { "CampusID".ToUpperInvariant(), PersonFields.Profile_Campus },
                { "CampusName".ToUpperInvariant(), PersonFields.Profile_Campus },
                { "TitleID".ToUpperInvariant(), PersonFields.Profile_Name },
                { "TitleValue".ToUpperInvariant(), PersonFields.Profile_Name },
                { "SuffixID".ToUpperInvariant(), PersonFields.Profile_Name },
                { "SuffixValue".ToUpperInvariant(), PersonFields.Profile_Name },
                { "NickName".ToUpperInvariant(), PersonFields.Profile_Name },
                { "FirstName".ToUpperInvariant(), PersonFields.Profile_Name },
                { "MiddleName".ToUpperInvariant(), PersonFields.Profile_Name },
                { "LastName".ToUpperInvariant(), PersonFields.Profile_Name },
                { "FullName".ToUpperInvariant(), PersonFields.Profile_Name },
                { "FamilyID".ToUpperInvariant(), PersonFields.Profile_Family_Information },
                { "FamilyName".ToUpperInvariant(), PersonFields.Profile_Family_Information },
                { "Age".ToUpperInvariant(), PersonFields.Profile_Age },
                { "Gender".ToUpperInvariant(), PersonFields.Profile_Gender },
                { "MedicalInformation".ToUpperInvariant(), PersonFields.Profile_Medical_Info },
                { "EnvelopeNumber".ToUpperInvariant(), PersonFields.Profile_Envelope_Number },
                { "MaritalStatusID".ToUpperInvariant(), PersonFields.Profile_Marital_Status },
                { "MaritalStatusValue".ToUpperInvariant(), PersonFields.Profile_Marital_Status },
                { "AnniversaryDate".ToUpperInvariant(), PersonFields.Profile_Anniversary_Date },
                { "MemberStatusID".ToUpperInvariant(), PersonFields.Profile_Member_Status },
                { "MemberStatusValue".ToUpperInvariant(), PersonFields.Profile_Member_Status },
                { "RecordStatusID".ToUpperInvariant(), PersonFields.Profile_Record_Status },
                { "RecordStatusValue".ToUpperInvariant(), PersonFields.Profile_Record_Status },
                { "InactiveReasonID".ToUpperInvariant(), PersonFields.Profile_Record_Status },
                { "InactiveReasonValue".ToUpperInvariant(), PersonFields.Profile_Record_Status },
                { "ContributeIndividually".ToUpperInvariant(), PersonFields.Profile_Contribute_Individually },
                { "PrintStatement".ToUpperInvariant(), PersonFields.Profile_Print_Statement },
                { "EmailStatement".ToUpperInvariant(), PersonFields.Profile_Email_Statement },
                { "BlobID".ToUpperInvariant(), PersonFields.Profile_Photo },
                { "BlobLink".ToUpperInvariant(), PersonFields.Profile_Photo },
                { "BirthDate".ToUpperInvariant(), PersonFields.Profile_Age },
                { "DateCreated".ToUpperInvariant(), PersonFields.Profile_Date_Added },
                { "DateModified".ToUpperInvariant(), PersonFields.Profile_Date_Modified },
                { "Addresses".ToUpperInvariant(), PersonFields.Profile_Addresses },
                { "FamilyMemberRoleID".ToUpperInvariant(), PersonFields.Profile_Family_Information },
                { "FamilyMemberRoleValue".ToUpperInvariant(), PersonFields.Profile_Family_Information },
                { "Phones".ToUpperInvariant(), PersonFields.Profile_Phones },
                { "GraduationDate".ToUpperInvariant(), PersonFields.Profile_Grade },
                { "Grade".ToUpperInvariant(), PersonFields.Profile_Grade },
                { "Emails".ToUpperInvariant(), PersonFields.Profile_Emails }
            };
        }

        /// <summary>
        /// Empty Constructor
        /// </summary>
        public PersonMapper()
        {
        }

        public PersonMapper(List<string> includeFields)
        {
            if (includeFields != null)
            {
                this._includeFields = includeFields.ConvertAll<string>((string t) => t.ToUpperInvariant());
            }
        }

        private bool ShouldShow(Arena.Core.Person arena, string name)
        {


            // First we check to see if this is yourself
            if (Arena.Core.ArenaContext.Current.Person.PersonID == arena.PersonID)
            {
                return true;
            }

            // Next we check to see if this is a family member
            if (Arena.Core.ArenaContext.Current.Person.FamilyId == arena.FamilyId)
            {
                return true;
            }

            // Group Leader Check - If the person they are fetching is one of their
            // small group members, go ahead and provide visibility
            Arena.Custom.SECC.Data.SmallGroup.GroupCollection gc = new Arena.Custom.SECC.Data.SmallGroup.GroupCollection();
            gc.LoadByLeaderPersonID(Arena.Core.ArenaContext.Current.Person.PersonID);
            foreach (Arena.SmallGroup.Group g in gc)
            {
                foreach (Arena.Core.Person groupMember in g.Members)
                {
                    if (groupMember.PersonID == arena.PersonID)
                    {
                        return true;
                    }
                }

                Arena.SmallGroup.GroupCluster cluster = g.GroupCluster;
                while (cluster != null)
                {
                    // If this is the current group's cluster leader or admin
                    if (cluster.LeaderID == arena.PersonID
                        || cluster.AdminID == arena.PersonID)
                    {
                        return true;
                    }
                    if (cluster.ParentClusterID > 0)
                    {
                        cluster = new Arena.SmallGroup.GroupCluster(cluster.ParentClusterID);
                    }
                    else { 
                        cluster = null; 
                    }
                }
            }


            if (!PersonMapper._fieldSecurityMap.ContainsKey(name.ToUpperInvariant())) {
                return false;
            }
            
            PermissionCollection permissionCollection = new PermissionCollection(ObjectType.PersonField, PersonMapper._fieldSecurityMap[name.ToUpperInvariant()]);
            if (!permissionCollection.Allowed(OperationType.View, Arena.Core.ArenaContext.Current.User))
            {
                return false;
            }
            if (this._includeFields == null)
            {
                return true;
            }
            return this._includeFields.Contains(name.ToUpperInvariant());
        }

        public Arena.Services.Contracts.Person FromArena(Arena.Core.Person arena)
        {
            Arena.Services.Contracts.Person person = new Arena.Services.Contracts.Person();
            if (this.ShouldShow(arena, "PersonID"))
            {
                person.PersonID = new int?(arena.PersonID);
            }
            if (this.ShouldShow(arena, "PersonGUID"))
            {
                person.PersonGUID = new Guid?(arena.PersonGUID);
            }
            if (this.ShouldShow(arena, "PersonLink"))
            {
                Guid personGUID = arena.PersonGUID;
                person.PersonLink = string.Format("person/{0}", personGUID.ToString());
            }
            if (this.ShouldShow(arena, "OrganizationID"))
            {
                person.OrganizationID = new int?(arena.OrganizationID);
            }
            if (arena.Campus != null && this.ShouldShow(arena, "CampusID"))
            {
                person.CampusID = new int?(arena.Campus.CampusId);
            }
            if (arena.Campus != null && this.ShouldShow(arena, "CampusName"))
            {
                person.CampusName = arena.Campus.Name;
            }
            if (this.ShouldShow(arena, "TitleID"))
            {
                person.TitleID = new int?(arena.Title.LookupID);
            }
            if (this.ShouldShow(arena, "TitleValue"))
            {
                person.TitleValue = arena.Title.ToString();
            }
            if (this.ShouldShow(arena, "SuffixID"))
            {
                person.SuffixID = new int?(arena.Suffix.LookupID);
            }
            if (this.ShouldShow(arena, "SuffixValue"))
            {
                person.SuffixValue = arena.Suffix.ToString();
            }
            if (this.ShouldShow(arena, "NickName"))
            {
                person.NickName = arena.NickName;
            }
            if (this.ShouldShow(arena, "FirstName"))
            {
                person.FirstName = arena.FirstName;
            }
            if (this.ShouldShow(arena, "MiddleName"))
            {
                person.MiddleName = arena.MiddleName;
            }
            if (this.ShouldShow(arena, "LastName"))
            {
                person.LastName = arena.LastName;
            }
            if (this.ShouldShow(arena, "FullName"))
            {
                person.FullName = arena.FullName;
            }
            if (this.ShouldShow(arena, "FamilyID"))
            {
                person.FamilyID = new int?(arena.FamilyId);
            }
            if (this.ShouldShow(arena, "FamilyName"))
            {
                person.FamilyName = arena.Family().FamilyName;
            }
            if (this.ShouldShow(arena, "FamilyLink"))
            {
                int? familyID = person.FamilyID;
                person.FamilyLink = string.Format("family/{0}", familyID.ToString());
            }
            if (this.ShouldShow(arena, "Age"))
            {
                person.Age = new int?(arena.Age);
            }
            if (this.ShouldShow(arena, "Gender"))
            {
                person.Gender = arena.Gender.ToString();
            }
            if (this.ShouldShow(arena, "Notes"))
            {
                person.Notes = arena.Notes;
            }
            if (this.ShouldShow(arena, "MedicalInformation"))
            {
                person.MedicalInformation = arena.MedicalInformation;
            }
            if (this.ShouldShow(arena, "EnvelopeNumber"))
            {
                person.EnvelopeNumber = new int?(arena.EnvelopeNumber);
            }
            if (this.ShouldShow(arena, "IncludeOnEnvelope"))
            {
                person.IncludeOnEnvelope = new bool?(arena.IncludeOnEnvelope);
            }
            if (this.ShouldShow(arena, "MaritalStatusID"))
            {
                person.MaritalStatusID = new int?(arena.MaritalStatus.LookupID);
            }
            if (this.ShouldShow(arena, "MaritalStatusValue"))
            {
                person.MaritalStatusValue = arena.MaritalStatus.ToString();
            }
            if (this.ShouldShow(arena, "AnniversaryDate"))
            {
                person.AnniversaryDate = new DateTime?(arena.AnniversaryDate);
            }
            if (this.ShouldShow(arena, "MemberStatusID"))
            {
                person.MemberStatusID = new int?(arena.MemberStatus.LookupID);
            }
            if (this.ShouldShow(arena, "MemberStatusValue"))
            {
                person.MemberStatusValue = arena.MemberStatus.ToString();
            }
            if (this.ShouldShow(arena, "RecordStatusID"))
            {
                person.RecordStatusID = new int?(Convert.ToInt32(arena.RecordStatus));
            }
            if (this.ShouldShow(arena, "RecordStatusValue"))
            {
                person.RecordStatusValue = arena.RecordStatus.ToString();
            }
            if (this.ShouldShow(arena, "InactiveReasonID"))
            {
                person.InactiveReasonID = new int?(arena.InactiveReason.LookupID);
            }
            if (this.ShouldShow(arena, "InactiveReasonValue"))
            {
                person.InactiveReasonValue = arena.InactiveReason.ToString();
            }
            if (this.ShouldShow(arena, "ActiveMeter"))
            {
                person.ActiveMeter = new int?(arena.ActiveMeter);
            }
            if (this.ShouldShow(arena, "ContributeIndividually"))
            {
                person.ContributeIndividually = new bool?(arena.ContributeIndividually);
            }
            if (this.ShouldShow(arena, "PrintStatement"))
            {
                person.PrintStatement = new bool?(arena.PrintStatement);
            }
            if (this.ShouldShow(arena, "EmailStatement"))
            {
                person.EmailStatement = new bool?(arena.EmailStatement);
            }
            if (this.ShouldShow(arena, "RegionName"))
            {
                person.RegionName = arena.RegionName;
            }
            if (this.ShouldShow(arena, "BlobID"))
            {
                person.BlobID = new int?(arena.BlobID);
            }
            if (this.ShouldShow(arena, "BlobLink"))
            {
                if (arena.BlobID > 0)
                {
                    Guid gUID = arena.Blob.GUID;
                    person.BlobLink = base.BuildBlobUrl(gUID.ToString(), -1, Gender.Unknown);
                }
            }
            if (this.ShouldShow(arena, "GivingUnitID"))
            {
                person.GivingUnitID = arena.GivingUnitID;
            }
            if (this.ShouldShow(arena, "ForeignKey"))
            {
                person.ForeignKey = new int?(arena.ForeignKey);
            }
            if (this.ShouldShow(arena, "ForeignKey2"))
            {
                person.ForeignKey2 = new int?(arena.ForeignKey2);
            }
            if (this.ShouldShow(arena, "Addresses"))
            {
                foreach (Arena.Core.PersonAddress arenaAddress in arena.Addresses)
                {
                    Address address = new Address()
                    {
                        AddressID = arenaAddress.AddressID,
                        StreetLine1 = arenaAddress.Address.StreetLine1,
                        StreetLine2 = arenaAddress.Address.StreetLine2,
                        City = arenaAddress.Address.City,
                        State = arenaAddress.Address.State,
                        PostalCode = arenaAddress.Address.PostalCode,
                        Country = arenaAddress.Address.Country
                    };
                    if (arenaAddress.AddressType != null && arenaAddress.AddressType.LookupID != -1)
                    {
                        address.AddressTypeValue = arenaAddress.AddressType.Value;
                        address.AddressTypeID = arenaAddress.AddressType.LookupID;
                    }
                    if (arenaAddress.Address.Latitude != 0)
                    {
                        address.Latitude = new double?(arenaAddress.Address.Latitude);
                    }
                    if (arenaAddress.Address.Longitude != 0)
                    {
                        address.Longitude = new double?(arenaAddress.Address.Longitude);
                    }
                    address.Primary = arenaAddress.Primary;
                    person.AddAddress(address);
                }
            }
            if (this.ShouldShow(arena, "Phones"))
            {
                foreach (Arena.Core.PersonPhone arenaPhone in arena.Phones)
                {
                    Arena.Services.Contracts.Phone phone = new Arena.Services.Contracts.Phone()
                    {
                        PhoneTypeID = arenaPhone.PhoneType.LookupID,
                        PhoneTypeValue = arenaPhone.PhoneType.Value,
                        Unlisted = arenaPhone.Unlisted,
                        SMSEnabled = arenaPhone.SMSEnabled,
                        Extension = arenaPhone.Extension,
                        Number = arenaPhone.Number
                    };
                    person.AddPhone(phone);
                }
            }
            if (this.ShouldShow(arena, "AttributesLink"))
            {
                Guid? nullable = person.PersonGUID;
                person.AttributesLink = string.Format("person/{0}/attribute/list", nullable.ToString());
            }
            if (this.ShouldShow(arena, "NotesLink"))
            {
                Guid? personGUID1 = person.PersonGUID;
                person.NotesLink = string.Format("person/{0}/note/list", personGUID1.ToString());
            }
            if (this.ShouldShow(arena, "BirthDate"))
            {
                person.BirthDate = new DateTime?(arena.BirthDate);
            }
            Arena.Core.Family family = null;
            if (this.ShouldShow(arena, "FamilyMemberRoleID"))
            {
                if (family == null)
                {
                    family = arena.Family();
                }
                if (family.FamilyID != -1)
                {
                    person.FamilyMemberRoleID = new int?(family.FamilyMembers.FindByID(arena.PersonID).FamilyRole.LookupID);
                }
            }
            if (this.ShouldShow(arena, "FamilyMemberRoleValue"))
            {
                if (family == null)
                {
                    family = arena.Family();
                }
                if (family.FamilyID != -1)
                {
                    person.FamilyMemberRoleValue = family.FamilyMembers.FindByID(arena.PersonID).FamilyRole.Value;
                }
            }
            if (this.ShouldShow(arena, "DateCreated"))
            {
                person.DateCreated = new DateTime?(arena.DateCreated);
            }
            if (this.ShouldShow(arena, "DateModified"))
            {
                person.DateModified = new DateTime?(arena.DateModified);
            }
            if (this.ShouldShow(arena, "AreaID") && arena.Area != null)
            {
                person.AreaID = new int?(arena.Area.AreaID);
            }
            if (this.ShouldShow(arena, "AreaName") && arena.Area != null)
            {
                person.AreaName = arena.Area.Name;
            }
            if (this.ShouldShow(arena, "Emails"))
            {
                foreach (Arena.Core.PersonEmail active in arena.Emails.Active)
                {
                    person.AddEmail(new Email()
                    {
                        Address = active.Email
                    });
                }
            }
            if (this.ShouldShow(arena, "DisplayNotesCount"))
            {
                person.DisplayNotesCount = new int?(arena.GetDisplayNotes(Arena.Core.ArenaContext.Current.Organization.OrganizationID, Arena.Core.ArenaContext.Current.User.Identity.Name).Count);
            }
            if (this.ShouldShow(arena, "FamilyMembersCount"))
            {
                if (family == null)
                {
                    family = arena.Family();
                }
                person.FamilyMembersCount = new int?(family.FamilyMembers.Count);
            }
            return person;
        }

        public ModifyResult Create(Person person) {

            var modifyResult = new ModifyResult();

            // We have a minimum set of data required
            if (person.LastName == null || person.LastName.Length < 2)
            {
                ModifyValidationResult result = new ModifyValidationResult();
                result.Key = "ShortLastName";
                result.Message = "Last name must be at least 2 characters";
                modifyResult.ValidationResults.Add(result);
            }
            if (person.FirstName == null || person.FirstName.Length < 2)
            {
                ModifyValidationResult result = new ModifyValidationResult();
                result.Key = "ShortFirstName";
                result.Message = "First name must be at least 2 characters";
                modifyResult.ValidationResults.Add(result);
            }
            if (person.BirthDate == null 
                && person.HomePhone == null 
                && person.FirstActiveEmail == null)
            {
                ModifyValidationResult result = new ModifyValidationResult();
                result.Key = "NotEnoughInfo";
                result.Message = "At least one of the following fields is required: Birth Date, Home Phone, or Email Adress";
                modifyResult.ValidationResults.Add(result);
            }

            if (modifyResult.ValidationResults.Count > 0)
            {
                modifyResult.Successful = "False";
                return modifyResult;
            }
            try { 
                PersonMatch pm = new PersonMatch();
                pm.FirstName = person.FirstName;
                pm.LastName = person.LastName;
                pm.Email = person.FirstActiveEmail;
                pm.HomePhone = person.HomePhone;

                if (person.Addresses.Count > 0) {
                    pm.Address.Street = person.Addresses.FirstOrDefault().StreetLine1;
                    pm.Address.City = person.Addresses.FirstOrDefault().City;
                    pm.Address.State = person.Addresses.FirstOrDefault().State;
                    pm.Address.PostalCode = person.Addresses.FirstOrDefault().PostalCode;
                }
                Core.Person corePerson = pm.MatchOrCreateArenaPerson();
                if (corePerson != null)
                {
                    modifyResult.Successful = "True";
                    modifyResult.Link = String.Format("cust/secc/person/{0}", corePerson.PersonID);
                }
            }
            catch (Exception e)
            {
                modifyResult.Successful = "False";
                modifyResult.ErrorMessage = e.Message;
            }
            return modifyResult;
        }

        public ModifyResult Update(int personId, Person person)
        {
            throw new NotImplementedException("This is not implemented for SECC.");
        }
    }
}
