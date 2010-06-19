using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using Arena.Services;

namespace Arena.Custom.HDC.WebService.Contracts
{
    [DataContract(Namespace = "")]
    public class Lookup
    {
        public Lookup(Core.Lookup arena)
        {
            ID = arena.LookupID;
            Value = arena.ToString();
        }

        [DataMember(EmitDefaultValue = false)]
        public int? ID { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string Value { get; set; }
    }

    class LookupMapper : Arena.Services.Contracts.BaseMapper
    {
        public LookupMapper()
        {
        }

        public Lookup FromArena(Core.Lookup arena)
        {
            return new Lookup(arena);
        }
    }
}
