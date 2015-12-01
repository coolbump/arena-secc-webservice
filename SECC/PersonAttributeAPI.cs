
using System;
using System.Collections.Generic;
using System.Configuration;
using System.ServiceModel.Web;
using Arena.Security;
using Arena.SmallGroup;
using Arena.Custom.HDC.WebService.Contracts;
using Arena.Services.Contracts;
using System.IO;
using System.Xml.Serialization;

namespace Arena.Custom.HDC.WebService.SECC
{
    class PersonAttributeAPI
    {
        /// <summary>
        /// <b>POST person/{id}/attribute/update</b>
        ///
        /// Update person attributes
        /// </summary>
        /// <returns>ModifyResult.</returns>
        [WebInvoke(Method="POST",
                UriTemplate = "person/{id}/attribute/update")]
        public ModifyResult UpdatePersonAttribute(Stream input, int id)
        {

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(PersonAttribute));
            PersonAttribute attribute = (PersonAttribute)xmlSerializer.Deserialize(input);

            // Create the mapper
            Arena.Custom.HDC.WebService.Contracts.PersonAttributeMapper mapper = 
                new Arena.Custom.HDC.WebService.Contracts.PersonAttributeMapper();
            if (attribute.AttributeID > 0)
            {
                return mapper.Update(id, attribute);
            }
            else
            {
                return mapper.Create(id, attribute);
            }
        }
    }
}
