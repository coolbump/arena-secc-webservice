
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using Arena.SmallGroup;
using Arena.Services;

namespace Arena.Custom.HDC.WebService.Contracts
{
    /// <summary>
    /// Defines the information about a type of cluster. This information
    /// is useful, and practically required, to properly deal with small
    /// group clusters.
    /// </summary>
    [DataContract(Namespace = "")]
    public class SmallGroupClusterType
    {
        /// <summary>
        /// Wether or not small groups of this cluster type are
        /// allowed to have occurrences.
        /// </summary>
        [DataMember()]
        public bool AllowOccurrences;

        /// <summary>
        /// Wether or not to allow new small group members to be added
        /// via a registration method.
        /// </summary>
        [DataMember()]
        public bool AllowRegistration;
        
        /// <summary>
        /// The CategoryID that this cluster type relates to.
        /// </summary>
        [DataMember()]
        public int CategoryID;
        
        /// <summary>
        /// The ID number of this ClusterType.
        /// </summary>
        [DataMember()]
        public int ClusterTypeID;
        
        /// <summary>
        /// Defined the strength of the relationship between small group
        /// members and the leader of the small group. This value is used
        /// to calculate peer values.
        /// </summary>
        [DataMember()]
        public int LeaderRelationshipStrength;
        
        /// <summary>
        /// The name of items at this level.
        /// </summary>
        [DataMember()]
        public string Name;
        
        /// <summary>
        /// The strength of the relationship beteen members of the same
        /// small group. This value is used to calculate peer values.
        /// </summary>
        [DataMember()]
        public int PeerRelationshipStrength;

        /// <summary>
        /// The list of cluster levels for this cluster type.
        /// </summary>
        [DataMember()]
        public List<SmallGroupClusterLevel> Levels;
    }

    /// <summary>
    /// Defines information about a specific cluster level. This class
    /// is embeded in the SmallGroupClusterType class and should not be
    /// used outside of that context.
    /// </summary>
    [DataContract(Namespace = "")]
    public class SmallGroupClusterLevel
    {
        /// <summary>
        /// The ClusterTypeID that this cluster level corresponds to.
        /// </summary>
        [DataMember()]
        public int ClusterTypeID;

        /// <summary>
        /// The level, as a numerical index, of this class of information.
        /// </summary>
        [DataMember()]
        public int Level;
        
        /// <summary>
        /// The name to be used at this level. This is not the name of
        /// a small group or a specific level, but it is more an identifier
        /// to describe the items at this level.
        /// </summary>
        [DataMember()]
        public string LevelName;
        
        /// <summary>
        /// Wether or not small groups will be allowed at this level.
        /// </summary>
        [DataMember()]
        public bool AllowGroups;
    }

    public class SmallGroupClusterTypeMapper : Arena.Services.Contracts.BaseMapper
    {
        public SmallGroupClusterTypeMapper()
        {
        }

        public SmallGroupClusterType FromArena(ClusterType arena)
        {
            SmallGroupClusterType type = new SmallGroupClusterType();
			SmallGroupClusterLevelMapper levelMapper = new SmallGroupClusterLevelMapper();


			type.ClusterTypeID = arena.ClusterTypeID;
			type.AllowOccurrences = arena.AllowOccurrences;
			type.AllowRegistration = arena.AllowRegistration;
			type.CategoryID = arena.CategoryID;
			type.LeaderRelationshipStrength = arena.LeaderRelationshipStrength;
			type.Name = arena.Name;
			type.PeerRelationshipStrength = arena.PeerRelationshipStrength;

            type.Levels = new List<SmallGroupClusterLevel>();
			foreach (ClusterLevel lv in arena.Levels)
			{
                type.Levels.Add(levelMapper.FromArena(lv));
			}

			return type;
        }
    }

    public class SmallGroupClusterLevelMapper : Arena.Services.Contracts.BaseMapper
    {
        public SmallGroupClusterLevelMapper()
        {
        }

        public SmallGroupClusterLevel FromArena(ClusterLevel arena)
        {
            SmallGroupClusterLevel level = new SmallGroupClusterLevel();


            level.AllowGroups = arena.AllowGroups;
            level.Level = arena.Level;
            level.LevelName = arena.LevelName;
            level.ClusterTypeID = arena.ClusterTypeID;

            return level;
        }
    }
}
