using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Arena.Custom.HDC.WebService.Contracts
{
    /// <summary>
    /// This structure contains the basic information about an OAuth Scope.
    /// This structure follows the standard RPC retrieval and updating
    /// rules.
    /// </summary>
    [DataContract(Namespace = "")]
    public class OAuthScope
    {
        /// <summary>
        /// Scope ID this information is referencing.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int ScopeID { get; set; }

        /// <summary>
        /// The identifier of this scope.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string Identifier { get; set; }

        /// <summary>
        /// The description of this scope.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string Description { get; set; }

        /// <summary>
        /// Specifies whether or not this client is currently active.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public bool Active { get; set; }
    }

    public class OAuthScopeMapper : Arena.Services.Contracts.BaseMapper
    {
        private List<string> _includeFields;


        public OAuthScopeMapper()
        {
        }


        public OAuthScopeMapper(List<string> includeFields)
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


        public OAuthClient FromArena(Arena.Custom.SECC.OAuth.Client dbClient)
        {
            OAuthClient client = new OAuthClient();

            return client;
        }
    }
}
