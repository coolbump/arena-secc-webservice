
using System;
using System.Collections.Generic;
using System.Configuration;
using System.ServiceModel.Web;
using Arena.Core;
using Arena.Security;
using Arena.SmallGroup;

namespace Arena.Custom.HDC.WebService
{
    class SystemAPI
    {
        /// <summary>
        /// Retrieve the version number of the entire system.
        /// </summary>
        /// <returns>SystemVersion object.</returns>
        [WebGet(UriTemplate = "sys/version")]
        public Contracts.SystemVersion GetSystemVersion()
        {
            return new Contracts.SystemVersion();
        }
    }
}
