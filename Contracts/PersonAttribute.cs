using Arena.Services.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Arena.Custom.HDC.WebService.Contracts
{

    public class PersonAttributeMapper
    {

        public PersonAttribute FromArena(Core.PersonAttribute attribute)
        {
            throw new NotImplementedException("This is not implemented for SECC.  Please use the default Arena methods to read person attributes.");
        }

        public ModifyResult Create(int personId, PersonAttribute attribute) {
            return Update(personId, attribute);
        }

        public ModifyResult Update(int personId, PersonAttribute attribute)
        {
            Arena.Core.Person person = new Arena.Core.Person(personId);
            var modifyResult = new ModifyResult();
            try {
                Core.Attribute coreAttribute = null;
                if (attribute.AttributeID > 0)
                {
                    coreAttribute = person.Attributes.FindByID(attribute.AttributeID);
                    if (coreAttribute == null)
                    {
                        coreAttribute = new Core.PersonAttribute(attribute.AttributeID);
                        person.Attributes.Add(coreAttribute);
                    }
                }
                else
                {
                    modifyResult.Successful = "False";
                    modifyResult.ErrorMessage = "Attribute ID is required.";
                    return modifyResult;
                }
                if (!coreAttribute.Allowed(Security.OperationType.Edit,
                    Arena.Core.ArenaContext.Current.User, person)) 
                {
                    modifyResult.Successful = "False";
                    modifyResult.ErrorMessage = "Permission denied to edit attribute.";
                    return modifyResult;
                }

                coreAttribute.AttributeName = attribute.AttributeName;
                if (coreAttribute.AttributeType == Enums.DataType.String)
                { 
                    coreAttribute.StringValue = attribute.StringValue;
                }
                if (coreAttribute.AttributeType == Enums.DataType.DateTime)
                {
                    coreAttribute.DateValue = attribute.DateValue.GetValueOrDefault();
                }
                if (coreAttribute.AttributeType == Enums.DataType.Decimal)
                {
                    coreAttribute.DecimalValue = attribute.DecimalValue.GetValueOrDefault();
                }
                if (coreAttribute.AttributeType == Enums.DataType.Int)
                {
                    coreAttribute.IntValue = attribute.IntValue.GetValueOrDefault();
                }
                person.SaveAttributes(Arena.Core.ArenaContext.Current.Organization.OrganizationID,
                    Arena.Core.ArenaContext.Current.User.Identity.Name);
                modifyResult.Successful = "True";
            } catch (Exception e)            {
                modifyResult.Successful = "False";
                modifyResult.ErrorMessage = e.Message;
            }

            return modifyResult;

        }
    }
}
