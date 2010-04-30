using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using Arena.Services;

namespace Arena.Custom.HDC.WebService.Contracts
{
    [DataContract(Namespace = "")]
    public class GenericListResult
    {
		[DataMember]
		public List<GenericReference> Items { get; set; }
		[DataMember]
		public int Total { get; set; }
		[DataMember]
		public int Max { get; set; }
		[DataMember]
		public int Start { get; set; }
    }
}
