
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using Arena;
using Arena.Services;

namespace Arena.Custom.HDC.WebService.Contracts
{
    [DataContract(Namespace = "")]
    public class GenericReference
    {
        [DataMember(EmitDefaultValue = false)]
        public int ID { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string Title { get; set; }

        public GenericReference(Core.Person arena)
        {
            ID = arena.PersonID;
            Title = arena.FullName;
        }

        public GenericReference(Arena.SmallGroup.Category arena)
        {
            ID = arena.CategoryID;
            Title = arena.CategoryName;
        }

        public GenericReference(Arena.SmallGroup.GroupCluster arena)
        {
            ID = arena.GroupClusterID;
            Title = arena.Name;
        }

        public GenericReference(Arena.SmallGroup.Group arena)
        {
            ID = arena.GroupID;
            Title = arena.Name;
        }
    }
}
