
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
using System.Linq;
using System.Data.Linq;
using Arena.Services.Exceptions;
using Arena.Custom.SECC.OAuth;
using Arena.Core;

namespace Arena.Custom.HDC.WebService
{
    class AuthAPI
    {
        /// <summary>
        /// <b>GET oauth/clientbyid?id={id}</b>
        ///
        /// Get information about a OAuth Client
        /// </summary>
        /// <returns>Client</returns>
        [WebGet(UriTemplate = "oauth/clientbyid?id={id}")]
        public OAuthClient getOAuthClientById(int id)
        {
            Client client = new Client(id);
            return (new OAuthClientMapper()).FromArena(client);
        }

        /// <summary>
        /// <b>GET oauth/clientbykey?clientApiKey={clientApiKey}</b>
        ///
        /// Get information about a OAuth Client by API Key
        /// </summary>
        /// <param name="apiKey">The API Key</param>
        /// <returns>Client</returns>
        [WebGet(UriTemplate = "oauth/clientbykey?clientApiKey={clientApiKey}")]
        [RestApiAnonymous]
        public OAuthClient GetOAuthClientByKey(String clientApiKey)
        {
            Guid clientKeyGuid = Guid.Empty;
            Guid.TryParse(clientApiKey, out clientKeyGuid);
            Client client = new Client(clientKeyGuid);
            return (new OAuthClientMapper()).FromArena(client);
        }

        /// <summary>
        /// <b>GET oauth/client/validate?clientApiKey={clientApiKey}&clientApiSecret={clientApiSecret}</b>
        ///
        /// Get information about a OAuth Client by API Key and Secret (Anonymous Method)
        /// </summary>
        /// <param name="apiKey">The API Key</param>
        /// <param name="apiSecret">The API Secret</param>
        /// <returns>Client</returns>
        [WebGet(UriTemplate = "oauth/client/validate?clientApiKey={clientApiKey}&clientApiSecret={clientApiSecret}")]
        [RestApiAnonymous]
        public OAuthClient OAuthClientValidate(String clientApiKey, String clientApiSecret)
        {
            Guid clientKeyGuid = Guid.Empty;
            Guid.TryParse(clientApiKey, out clientKeyGuid);
            Client client = new Client(clientKeyGuid);
            if (client.ApiSecret != null && client.ApiSecret.ToString().ToUpper().Equals(clientApiSecret)) 
            {
                    OAuthClientMapper mapper = new OAuthClientMapper();
                    return mapper.FromArena(client);
            }
            throw new RESTException(new Exception("Invalid API Credentials"), System.Net.HttpStatusCode.Forbidden, "Invalid API Key/Secret Combination.");
        }

        /// <summary>
        /// <b>GET oauth/client/{clientApiKey}/user/authorizations/list</b>
        ///
        /// Get all user authorizations for the current client
        /// </summary>
        /// <param name="clientApiKey">The API Key</param>
        /// <returns>Client</returns>
        [WebGet(UriTemplate = "oauth/client/{clientApiKey}/user/authorizations/list")]
        public List<OAuthAuthorization> GetOAuthUserAuthorizations(String clientApiKey)
        {
            List<ClientAuthorization> authorizations = 
                Authorization.GetUserAuthorizationsForClient(ArenaContext.Current.User.Identity.Name, clientApiKey, false);
            List<OAuthAuthorization> list = new List<OAuthAuthorization>();
            OAuthAuthorizationMapper mapper = new OAuthAuthorizationMapper();
            foreach (ClientAuthorization auth in authorizations)
            {
                list.Add(mapper.FromArena(auth));
            }
            return list;
        }


        /// <summary>
        /// <b>POST oauth/client/{clientApiKey}/user/authorizations/list</b>
        ///
        /// Update a specific user authorization for the current client
        /// </summary>
        /// <param name="apiKey">The API Key</param>
        /// <returns>ModifyResult.</returns>
        [WebInvoke(Method = "POST",
                UriTemplate = "oauth/client/{clientApiKey}/user/authorization/update")]
        public ModifyResult UpdateOAuthUserAuthorization(Stream input, int id)
        {
            /*StreamReader reader = new StreamReader(input);
            String content = reader.ReadToEnd();*/

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(OAuthAuthorization));
            OAuthAuthorization auth = (OAuthAuthorization)xmlSerializer.Deserialize(input);

            // Create the mapper
            Arena.Custom.HDC.WebService.Contracts.OAuthAuthorizationMapper mapper =
                new Arena.Custom.HDC.WebService.Contracts.OAuthAuthorizationMapper();
            if (auth.AuthorizationId > 0)
            {
                return mapper.Update(auth);
            }
            else
            {
                return mapper.Create(auth);
            }
        }
        
    }
}
