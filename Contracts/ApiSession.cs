using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Arena.Core;
using Arena.DataLayer.Utility;

namespace Arena.Custom.HDC.WebService.Contracts
{
    /// <summary>
    /// This class provides an extended API Session for SECC's custom login
    /// </summary>
    [DataContract(Namespace = "")]
    public class ApiSession
    {
        /// <summary>
        /// A unique device key for re-authenticating as this device
        /// </summary>
        [DataMember()]
        public Guid DeviceKey { get; set; }

        /// <summary>
        /// The day/time this session expires
        /// </summary>
        [DataMember]
        public DateTime DateExpires { get; set; }

        /// <summary>
        /// The SessionId
        /// </summary>
        [DataMember]
        public Guid SessionID { get; set; }
    }
}
