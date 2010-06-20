using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Arena.Core;
using Arena.DataLayer.Utility;

namespace Arena.Custom.HDC.WebService.Contracts
{
    /// <summary>
    /// This class provides the information required to determine what
    /// version the Arena system is running to a client.
    /// </summary>
    [DataContract(Namespace = "")]
    public class SystemVersion
    {
        /// <summary>
        /// Build the information contained in the SystemVersion class.
        /// At some point we should probably have a .h file that the Assembly
        /// file includes as well to set version information.
        /// </summary>
        public SystemVersion()
        {
            ArenaVersion = ArenaContext.Current.GetType().Assembly.GetName().Version.ToString();
            DatabaseVersion = new Arena.DataLayer.Utility.Database().GetArenaDatabaseVersion();
            ApiVersion = "0.4";
        }

        /// <summary>
        /// This variable contains the Arena Codebase version as a string, in
        /// the format of major.minor.revision.build, e.g. 2009.2.400.1401
        /// </summary>
        [DataMember()]
        public string ArenaVersion;

        /// <summary>
        /// This variable contains the Arena Database version as a string, in
        /// the format of major.minor.revision.build, e.g. 2009.2.400.01401
        /// </summary>
        [DataMember()]
        public string DatabaseVersion;

        /// <summary>
        /// This variable contains the API version as a string, in the format
        /// of major.minor[.revision[.build]], e.g. 0.4 (which for comparison
        /// should be considered to be 0.4.0.0).
        /// </summary>
        [DataMember()]
        public string ApiVersion;
    }
}
