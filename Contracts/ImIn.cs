using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Arena.Custom.HDC.WebService.Contracts
{
    [DataContract(Namespace = "")]
    public class ImIn
    {
        [DataMember(EmitDefaultValue = false)]
        public Int32 PersonID { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string FirstName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string LastName { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public DateTime DateOfBirth { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string Email { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string PhoneNumber { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string PhoneType { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public Boolean IsMember { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public Boolean AgreesWithSoF { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string Campus { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string StreetAddress { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string ZipCode { get; set; }
    }
}
