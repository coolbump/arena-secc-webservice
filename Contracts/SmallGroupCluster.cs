﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using Arena.Core;
using Arena.SmallGroup;

namespace Arena.Custom.HDC.WebService.Contracts
{
    /// <summary>
    /// Contains the basic information about a group cluster.
    /// </summary>
    [DataContract(Namespace = "")]
    public class SmallGroupCluster
    {
        /// <summary>
        /// The Group Cluster ID that this information pertains to.
        /// </summary>
        [DataMember(EmitDefaultValue = true)]
        public int ClusterID { get; set; }

        /// <summary>
        /// The parent cluster ID of this cluster.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? ParentClusterID {get; set;}

        /// <summary>
        /// The group category ID of this cluster.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? CategoryID { get; set; }

        /// <summary>
        /// The name of this cluster.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string Name { get; set; }

        /// <summary>
        /// Flag which indicates wether or not this group cluster is
        /// active.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public bool? Active { get; set; }

        /// <summary>
        /// Retrieve the level of this group cluster.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? ClusterLevelID { get; set; }

        /// <summary>
        /// Retrieve the cluster type of this group cluster.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? ClusterTypeID { get; set; }

        /// <summary>
        /// Description of this group cluster.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string Description { get; set; }

        /// <summary>
        /// Notes that relate to this group cluster.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string Notes { get; set; }

        /// <summary>
        /// The number of child clusters under this cluster.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? ClusterCount { get; set; }

        /// <summary>
        /// The number of pending registrations in this group
        /// cluster and its descendents. This property is
        /// read-only.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? RegistrationCount { get; set; }

        /// <summary>
        /// The number of members in this group cluster and its
        /// descendents. This property is read-only.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? MemberCount { get; set; }

        /// <summary>
        /// The number of small groups in this group cluster and
        /// its descendents. This property is read-only.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? GroupCount { get; set; }

        /// <summary>
        /// The person who is the administrator of
        /// this group cluster.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public GenericReference Admin { get; set; }

        /// <summary>
        /// The person who is considered the leader of
        /// this group cluster.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public GenericReference Leader { get; set; }

        /// <summary>
        /// The Area ID that this group cluster belongs to.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int? AreaID { get; set; }

        /// <summary>
        /// The URL for this group cluster. This is not the same
        /// as the NavigationUrl. This is more like a groups
        /// website that might be outside the Arena system.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string ClusterUrl { get; set; }

        /// <summary>
        /// The name of the person who created this small group
        /// cluster.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string CreatedBy { get; set; }

        /// <summary>
        /// The date this group cluster was created on.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime? DateCreated { get; set; }

        /// <summary>
        /// The name of the last person to have modified this small
        /// group cluster.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string ModifiedBy { get; set; }

        /// <summary>
        /// The date this group cluster was last modified on.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public DateTime? DateModified { get; set; }

        /// <summary>
        /// The URL that can be used to navigate to this group
        /// cluster in a web browser.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string NavigationUrl { get; set; }

        /// <summary>
        /// The URL that can be used to retrieve the group cluster's
        /// image.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public string ImageUrl { get; set; }

        public SmallGroupCluster()
        {
            ClusterID = -1;
        }
    }

    class SmallGroupClusterMapper : Arena.Services.Contracts.BaseMapper
    {
        public SmallGroupClusterMapper()
        {
        }

        public SmallGroupCluster FromArena(GroupCluster arena)
        {
            SmallGroupCluster cluster = new SmallGroupCluster();


            cluster.ClusterID = arena.GroupClusterID;
            if (cluster.ClusterID == -1)
                return cluster;

            cluster.Active = arena.Active;
            cluster.Admin = new GenericReference(arena.Admin);
            cluster.AreaID = arena.Area.AreaID;
            cluster.CategoryID = arena.ClusterType.CategoryID;
            cluster.ClusterTypeID = arena.ClusterTypeID;
            cluster.CreatedBy = arena.CreatedBy;
            cluster.DateCreated = arena.DateCreated;
            cluster.DateModified = arena.DateModified;
            cluster.Description = arena.Description;
            if (arena.ImageBlob.BlobID != -1)
            {
                cluster.ImageUrl = this.BuildBlobUrl(arena.ImageBlob.GUID.ToString(), -1, Arena.Enums.Gender.Unknown);
            }
            cluster.Leader = new GenericReference(arena.Leader);
            cluster.ClusterLevelID = arena.ClusterLevelID;
            cluster.ModifiedBy = arena.ModifiedBy;
            cluster.Name = arena.Name;
            if (String.IsNullOrEmpty(arena.NavigationUrl) == false)
                cluster.NavigationUrl = arena.NavigationUrl;
            if (String.IsNullOrEmpty(arena.Notes) == false)
                cluster.Notes = arena.Notes;
            cluster.ParentClusterID = arena.ParentClusterID;
            if (String.IsNullOrEmpty(arena.ClusterUrl) == false)
                cluster.ClusterUrl = arena.ClusterUrl;

            //
            // Get the counts from Arena.
            //
            SqlParameter groupCount, memberCount, unassignedRegCount, assignedRegCount;
            ArrayList paramList = new ArrayList();

            groupCount = new SqlParameter("@GroupCount", SqlDbType.Int);
            groupCount.Direction = ParameterDirection.Output;
            memberCount = new SqlParameter("@MemberCount", SqlDbType.Int);
            memberCount.Direction = ParameterDirection.Output;
            unassignedRegCount = new SqlParameter("@UnassignedRegCount", SqlDbType.Int);
            unassignedRegCount.Direction = ParameterDirection.Output;
            assignedRegCount = new SqlParameter("@AssignedRegCount", SqlDbType.Int);
            assignedRegCount.Direction = ParameterDirection.Output;

            paramList.Add(new SqlParameter("@ParentClusterID", arena.GroupClusterID));
            paramList.Add(new SqlParameter("@ActiveOnly", 1));
            paramList.Add(new SqlParameter("@OrganizationId", ArenaContext.Current.Organization.OrganizationID));
            paramList.Add(groupCount);
            paramList.Add(memberCount);
            paramList.Add(unassignedRegCount);
            paramList.Add(assignedRegCount);

            new Arena.DataLayer.Organization.OrganizationData().ExecuteNonQuery("smgp_sp_get_counts", paramList);
            cluster.GroupCount = (int)groupCount.Value;
            cluster.MemberCount = (int)memberCount.Value;
            cluster.RegistrationCount = (int)unassignedRegCount.Value;
            cluster.ClusterCount = arena.ChildClusters.Count;

            return cluster;
        }
    }
}
