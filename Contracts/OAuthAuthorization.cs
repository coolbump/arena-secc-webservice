using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Arena.Services.Contracts;
using System.Diagnostics;

namespace Arena.Custom.HDC.WebService.Contracts
{
    /// <summary>
    /// This structure contains the basic information about an OAuth Scope.
    /// This structure follows the standard RPC retrieval and updating
    /// rules.
    /// </summary>
    [DataContract(Namespace = "")]
    public class OAuthAuthorization
    {
        /// <summary>
        /// Scope ID this information is referencing.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int AuthorizationId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int ClientId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int ScopeId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string LoginId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string ScopeIdentifier { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string ScopeDescription { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public bool Active { get; set; }

    }

    public class OAuthAuthorizationMapper : Arena.Services.Contracts.BaseMapper
    {
        public OAuthAuthorizationMapper()
        {
        }

        public OAuthAuthorization FromArena(Arena.Custom.SECC.OAuth.ClientAuthorization dbAuthorization)
        {
            OAuthAuthorization authorization = new OAuthAuthorization();
            authorization.AuthorizationId = dbAuthorization.AuthorizationId;
            authorization.ClientId = dbAuthorization.ClientId;
            authorization.LoginId = dbAuthorization.LoginId;
            authorization.ScopeId = dbAuthorization.ScopeId;
            authorization.ScopeIdentifier = dbAuthorization.ScopeIdentifier;
            authorization.ScopeDescription = dbAuthorization.ScopeDescription;
            authorization.Active = dbAuthorization.Active;

            return authorization;
        }

        /// <summary>
        /// Create a new authorization
        /// </summary>
        /// <param name="auth">The authorization service contract object</param>
        /// <returns></returns>
        public ModifyResult Create(OAuthAuthorization auth)
        {
            return CreateOrUpdate(auth);
        }

        /// <summary>
        /// Update an existing authorization
        /// </summary>
        /// <param name="auth">The authorization service contract object</param>
        /// <returns></returns>
        public ModifyResult Update(OAuthAuthorization auth)
        {
            return CreateOrUpdate(auth);
        }

        /// <summary>
        /// Create/Update actually shares the same method
        /// </summary>        
        /// <param name="auth">The authorization service contract object</param>
        /// <returns></returns>
        private ModifyResult CreateOrUpdate(OAuthAuthorization auth)
        {
            var modifyResult = new ModifyResult();
            if (auth.ClientId == 0)
            {
                modifyResult.Successful = "False";
                modifyResult.ErrorMessage = "ClientId must be set";
                return modifyResult;
            }

            if (auth.LoginId == null && auth.LoginId == "")
            {
                modifyResult.Successful = "False";
                modifyResult.ErrorMessage = "LoginId must be set";
                return modifyResult;
            }

            Arena.Custom.SECC.OAuth.Authorization dbAuth;

            if (auth.AuthorizationId > 0)
            {
                dbAuth = new Arena.Custom.SECC.OAuth.Authorization(auth.AuthorizationId);
            } 
            else 
            {
                dbAuth = new Arena.Custom.SECC.OAuth.Authorization();
            }

            try
            {
                dbAuth.Active = auth.Active;
                dbAuth.ClientId = auth.ClientId;
                dbAuth.LoginId = auth.LoginId;
                if (auth.ScopeId > 0)
                { 
                    dbAuth.ScopeId = auth.ScopeId;
                }
                else if(auth.ScopeIdentifier != null)
                {
                    var scope = new Arena.Custom.SECC.OAuth.Scope(auth.ScopeIdentifier);
                    if (scope != null)
                    {
                        dbAuth.ScopeId = scope.ScopeId;
                    }
                    else {
                        modifyResult.Successful = "False";
                        modifyResult.ErrorMessage = "ScopeId or ScopeIdentifier is required";
                        return modifyResult;
                    }
                }
                else
                {
                    modifyResult.Successful = "False";
                    modifyResult.ErrorMessage = "ScopeId or ScopeIdentifier is required";
                    return modifyResult;
                }

                if (!dbAuth.Allowed(Security.OperationType.Edit,
                    Arena.Core.ArenaContext.Current.User))
                {
                    modifyResult.Successful = "False";

                    StackFrame frame = new StackFrame(1);
                    modifyResult.ErrorMessage = "Permission denied to " + frame.GetMethod().Name.ToLower() + " authorization.";
                    return modifyResult;
                }

                dbAuth.Save();

                modifyResult.Successful = "True";
            }
            catch (Exception e)
            {
                modifyResult.Successful = "False";
                modifyResult.ErrorMessage = e.Message;
            }

            return modifyResult;

        }
    }
}