
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
using System.Web;
using System.Collections.Specialized;
using Arena.Custom.SECC.Common.Data.Auth;

namespace Arena.Custom.HDC.WebService.SECC
{
    class AuthAPI
    {

        [WebInvoke(Method = "POST",
                UriTemplate = "login")]
        [RestApiAnonymous]
        public Contracts.ApiSession login(Stream input)
        {
            Core.ApiSession coreApiSession = null;
            Contracts.ApiSession contractApiSession = new Contracts.ApiSession();
            string body = new StreamReader(input).ReadToEnd();
            NameValueCollection postVars = HttpUtility.ParseQueryString(body);
            
            // Determine which kind of login attempt we have
            if (!postVars.AllKeys.Contains("api_key"))
            {
                throw new BadRequestException("Parameter api_key is required.");
            }
            try
            {
                
                // If username is passed
                if (postVars.AllKeys.Contains("username"))
                {
                    if (!postVars.AllKeys.Contains("password"))
                    {
                        throw new BadRequestException("Parameter password is required for username authentication.");
                    }

                    // Do a normal Arena API login
                    var api = new Arena.Services.ArenaAPI();
                    input.Seek(0, SeekOrigin.Begin);
                    coreApiSession = api.Login(input);

                    // Test scenario for authenticating with device id
                    /*coreApiSession.DateExpires = DateTime.Now.AddMinutes(1);
                    coreApiSession.Save(postVars["username"]);*/

                    // If we have a device id, register this device and issue a key for it
                    if (postVars.AllKeys.Contains("device_id"))
                    {
                        // First check to see if this device id is already associated with your account
                        Device device = new Device(postVars["device_id"]);
                        if (device.AuthDeviceId > 0)
                        {
                            // Make sure this device is owned by the current person
                            if (device.PersonId != coreApiSession.CurrentPerson.PersonID)
                            {
                                // Delete the old one and create a new one
                                device.Delete();
                                device = new Device();
                                device.PersonId = coreApiSession.CurrentPerson.PersonID;
                            }
                            // Generate a new guid
                            device.DeviceKey = Guid.NewGuid();
                        }
                        else
                        {
                            device.PersonId = coreApiSession.CurrentPerson.PersonID;
                        }
                        device.DeviceId = postVars["device_id"];
                        device.DeviceName = postVars["device_name"];
                        device.LoginId = postVars["username"];
                        device.LastLogin = DateTime.Now;
                        device.Save(postVars["username"]);
                        device.Active = true;
                        contractApiSession.DeviceKey = device.DeviceKey;
                    }
                }
                
                // Do device authentication
                if (postVars.AllKeys.Contains("device_key"))
                {
                    // First validate the API key
                    ApiApplication apiApp = new ApiApplication(new Guid(postVars["api_key"]));
                    if (apiApp.ApplicationId <= 0)
                    {
                        throw new AuthenticationException("Invalid api_key for device key authentication.");
                    }

                    // Now validatate the device key
                    Device device = new Device(new Guid(postVars["device_key"]));
                    if (device.Active==false || device.AuthDeviceId <= 0 || device.DeviceId != postVars["device_id"])
                    {
                        throw new AuthenticationException("Invalid device id/key pair.");
                    }

                    // Setup the API Session and save it
                    coreApiSession = new Arena.Core.ApiSession();
                    coreApiSession.SetupSession(device.Person, apiApp.ApiSecret, device.LoginId, apiApp);

                    // Test scenario for authenticating with device id
                    /*coreApiSession.DateExpires = DateTime.Now.AddMinutes(1);
                    coreApiSession.Save(device.LoginId);*/

                    // Update the Last Login time for the device
                    device.LastLogin = DateTime.Now;
                    // Generate a new guid
                    device.DeviceKey = Guid.NewGuid();
                    device.Save(device.LoginId);

                    // Update the API Session that we are returning
                    contractApiSession.DeviceKey = device.DeviceKey;
                    contractApiSession.SessionID = coreApiSession.SessionID;
                    contractApiSession.DateExpires = coreApiSession.DateExpires;
                }

            } catch (Exception e)
            {
                throw new AuthenticationException(e.Message);
            }

            Arena.Core.ApiSession.SetSession(coreApiSession);

            // Copy the values for mapping back to the custom contract
            contractApiSession.SessionID = coreApiSession.SessionID;
            contractApiSession.DateExpires = coreApiSession.DateExpires;
            return contractApiSession;
        }

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
            Guid clientProvidedSecret = Guid.Empty;
            Guid.TryParse(clientApiKey, out clientKeyGuid);
            Guid.TryParse( clientApiSecret, out clientProvidedSecret );
            Client client = new Client(clientKeyGuid);
            if (client.ApiSecret != null && client.ApiSecret.Equals(clientProvidedSecret)) 
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
        public GenericListResult<OAuthAuthorization> GetOAuthUserAuthorizations(String clientApiKey)
        {
            List<ClientAuthorization> authorizations = 
                Authorization.GetUserAuthorizationsForClient(ArenaContext.Current.User.Identity.Name, clientApiKey, false);
            GenericListResult<OAuthAuthorization> list = new GenericListResult<OAuthAuthorization>();
            list.Items = new List<OAuthAuthorization>();
            OAuthAuthorizationMapper mapper = new OAuthAuthorizationMapper();
            foreach (ClientAuthorization auth in authorizations)
            {
                list.Items.Add(mapper.FromArena(auth));
            }
            list.Total = list.Max = list.Items.Count();
            list.Start = 0;
            
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
        public ModifyResult UpdateOAuthUserAuthorization(String clientApiKey, OAuthAuthorization auth)
        {
            Arena.Custom.SECC.OAuth.Client client = new Arena.Custom.SECC.OAuth.Client(new Guid(clientApiKey));
            if (auth.ClientId != client.ClientId)
            {
                throw new ResourceNotFoundException("Client API Key mismatch.");
            }

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
