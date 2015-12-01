using Arena.Custom.SECC.OAuth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Arena.Custom.HDC.WebService.Contracts
{
    /// <summary>
    /// This structure contains the basic information about an OAuth Client.
    /// This structure follows the standard RPC retrieval and updating
    /// rules.
    /// </summary>
    [DataContract(Namespace = "")]
    public class OAuthClient
    {
        /// <summary>
        /// Client ID this information is referencing.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int ClientID { get; set; }

        /// <summary>
        /// The name of this client.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string Name { get; set; }

        /// <summary>
        /// The client's callback/redirect URL.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string CallbackURL { get; set; }

        /// <summary>
        /// The string representation of the API Key GUID identifying
        /// this client.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string APIKey { get; set; }

        /// <summary>
        /// The string representation of the API Secret GUID identifying
        /// this client.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string APISecret { get; set; }

        /// <summary>
        /// Specifies whether or not this client is currently active.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public bool Active { get; set; }

        /// <summary>
        /// The list of scopes this client has access to.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public List<OAuthScope> Scopes { get; set; }

    }

    public class OAuthClientMapper : Arena.Services.Contracts.BaseMapper
    {
        private List<string> _includeFields;


        public OAuthClientMapper()
        {
        }


        public OAuthClientMapper(List<string> includeFields)
        {
            if (includeFields != null)
            {
                // Normalize for search
                _includeFields = new List<string>();
                foreach (string value in includeFields)
                    _includeFields.Add(value.ToUpperInvariant());
            }
        }


        private bool ShouldShow(string name)
        {
            bool status = false;

            if (_includeFields == null)
                return true;

            if (_includeFields.Contains("*"))
            {
                status = true;

                if (_includeFields.Contains(String.Concat("-", name.ToUpperInvariant())))
                    status = false;
            }
            else if (_includeFields.Contains(name.ToUpperInvariant()))
                status = true;

            return status;
        }


        public OAuthClient FromArena(Client dbClient)
        {
            OAuthClient client = new OAuthClient();
            client.ClientID = dbClient.ClientId;
            client.APIKey = dbClient.ApiKey.ToString();
            client.Active = dbClient.Active;
            client.CallbackURL = dbClient.Callback;
            client.Name = dbClient.Name;
            client.Scopes = new List<OAuthScope>();
            foreach(Scope dbScope in dbClient.Scopes)
            {
                OAuthScope scope = new OAuthScope();
                scope.Active = dbScope.Active;
                scope.Description = dbScope.Description;
                scope.Identifier = dbScope.Identifier;
                scope.ScopeID = dbScope.ScopeId;
                client.Scopes.Add(scope);
            }
            return client;
        }
    }
}
