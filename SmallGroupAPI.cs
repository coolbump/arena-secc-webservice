
using System;
using System.Collections.Generic;
using System.Configuration;
using System.ServiceModel.Web;
using Arena.Core;
using Arena.Security;
using Arena.SmallGroup;

namespace Arena.Custom.HDC.WebService
{
    class SmallGroupAPI
    {
        /// <summary>
        /// Retrieve a list of all group categories in the system. If, by chance,
        /// no categories exist then an empty array is returned.
        /// </summary>
        /// <returns>Integer array of group categoryIDs.</returns>
        [WebGet(UriTemplate = "smgp/category/list")]
        public Contracts.GenericListResult<Contracts.GenericReference> GetSmallGroupCategories()
        {
            Contracts.GenericListResult<Contracts.GenericReference> list = new Contracts.GenericListResult<Contracts.GenericReference>();
            CategoryCollection categories = new CategoryCollection();


            list.Items = new List<Contracts.GenericReference>();
            list.Total = categories.Count;
            list.Max = list.Total;
            list.Start = 0;
            foreach (Category category in categories)
            {
                list.Items.Add(new Contracts.GenericReference(category));
            }

            return list;
        }

        /// <summary>
        /// Retrieve the information about a small group category.
        /// If the given category is not found then -1 is returned in the
        /// categoryID member.
        /// </summary>
        /// <param name="categoryID">The category to find information about.</param>
        /// <returns>Basic information about a group category.</returns>
        [WebGet(UriTemplate = "smgp/category/{categoryID}")]
        public Contracts.SmallGroupCategory GetSmallGroupCategory(int categoryID)
        {
            Category category = new Category(categoryID);
            Contracts.SmallGroupCategoryMapper mapper = new Arena.Custom.HDC.WebService.Contracts.SmallGroupCategoryMapper();


            if (category.CategoryID == -1)
                throw new Arena.Services.Exceptions.ResourceNotFoundException("Invalid category ID");

            return mapper.FromArena(category);
        }

        /// <summary>
        /// Retrieve a list of all group clusters at the root level of the
        /// group category. If no group clusters are contained in the category
        /// then an empty array is returned.
        /// </summary>
        /// <param name="categoryID">The parent category to find all root clusters of.</param>
        /// <returns>Integer array of clusterIDs.</returns>
        [WebGet(UriTemplate = "smgp/cluster/list?categoryID={categoryID}&clusterID={clusterID}")]
        public Contracts.GenericListResult<Contracts.GenericReference> GetSmallGroupClusters(String categoryID, String clusterID)
        {
            GroupClusterCollection clusters;
            Contracts.GenericListResult<Contracts.GenericReference> list = new Contracts.GenericListResult<Contracts.GenericReference>();


            if (categoryID != null)
            {
                clusters = new GroupClusterCollection(Convert.ToInt32(categoryID), Convert.ToInt32(ConfigurationSettings.AppSettings["Organization"]));
            }
            else if (clusterID != null)
            {
                if (RestApi.GroupClusterOperationAllowed(ArenaContext.Current.Person.PersonID, Convert.ToInt32(clusterID), OperationType.View) == false)
                    throw new Exception("Access denied.");

                clusters = new GroupClusterCollection(Convert.ToInt32(clusterID));
            }
            else
                throw new Exception("Required parameters not provided.");

            list.Start = 0;
            list.Max = list.Total = clusters.Count;
            list.Items = new List<Contracts.GenericReference>();
            clusters.Sort(delegate(GroupCluster gc1, GroupCluster gc2) { return gc1.Name.CompareTo(gc2.Name); });
            foreach (GroupCluster cluster in clusters)
            {
                if (RestApi.GroupClusterOperationAllowed(ArenaContext.Current.Person.PersonID, cluster.GroupClusterID, OperationType.View) == true)
                {
                    list.Items.Add(new Contracts.GenericReference(cluster));
                }
            }

            return list;
        }

        /// <summary>
        /// Retrieve the information about a group cluster. If the group
        /// cluster is not found then -1 is returned in the clusterID member.
        /// </summary>
        /// <param name="clusterID">The cluster to retrieve information about.</param>
        /// <returns>Basic information about the group cluster.</returns>
        [WebGet(UriTemplate = "smgp/cluster/{clusterID}")]
        public Contracts.SmallGroupCluster GetSmallGroupCluster(int clusterID)
        {
            Contracts.SmallGroupClusterMapper mapper = new Contracts.SmallGroupClusterMapper();
            GroupCluster cluster = new GroupCluster(clusterID);


            if (cluster.GroupClusterID == -1)
                throw new Arena.Services.Exceptions.ResourceNotFoundException("Invalid cluster ID");

            if (RestApi.GroupClusterOperationAllowed(ArenaContext.Current.Person.PersonID, cluster.GroupClusterID, OperationType.View) == false)
                throw new Exception("Access denied.");

            return mapper.FromArena(cluster);
        }

        /// <summary>
        /// Retrieve the information about the specified small group
        /// cluster type.
        /// </summary>
        /// <param name="clusterTypeID">The ID of the cluster type to retrieve.</param>
        /// <returns>SmallGroupClusterType which identifies the requested clusterTypeID.</returns>
        [WebGet(UriTemplate = "smgp/cluster/type/{clusterTypeID}")]
        public Contracts.SmallGroupClusterType GetSmallGroupClusterType(int clusterTypeID)
        {
            Contracts.SmallGroupClusterTypeMapper mapper = new Contracts.SmallGroupClusterTypeMapper();
            ClusterType type = new ClusterType(clusterTypeID);


            if (type.ClusterTypeID == -1)
                throw new Arena.Services.Exceptions.ResourceNotFoundException("Invalid cluster type ID");

            return mapper.FromArena(type);
        }

        /// <summary>
        /// Retrieve a list of small groups which reside under the parent group
        /// cluster. If no small groups are found then an empty array is returned.
        /// </summary>
        /// <param name="clusterID">The parent cluster to find small groups under.</param>
        /// <returns>An integer array of small groups under the parent cluster.</returns>
        [WebGet(UriTemplate = "smgp/group/list?clusterID={clusterID}")]
        public Contracts.GenericListResult<Contracts.GenericReference> GetSmallGroups(int clusterID)
        {
            Contracts.GenericListResult<Contracts.GenericReference> list = new Contracts.GenericListResult<Contracts.GenericReference>();
            GroupCollection groups = new GroupCollection(clusterID);


            if (RestApi.GroupClusterOperationAllowed(ArenaContext.Current.Person.PersonID, clusterID, OperationType.View) == false)
                throw new Exception("Access denied.");

            list.Start = 0;
            list.Max = list.Total = groups.Count;
            list.Items = new List<Contracts.GenericReference>();
            foreach (Group group in groups)
            {
                list.Items.Add(new Contracts.GenericReference(group));
            }

            return list;
        }

        /// <summary>
        /// Retrieves information about the small group. If the small
        /// group is not found then -1 is returned in the groupID member.
        /// </summary>
        /// <param name="GroupID">The small group to retrieve information about.</param>
        /// <returns>Basic information about the small group.</returns>
        [WebGet(UriTemplate = "smgp/group/{groupID}")]
        public Contracts.SmallGroup GetSmallGroup(int groupID)
        {
            Contracts.SmallGroupMapper mapper = new Contracts.SmallGroupMapper();
            Group group = new Group(groupID);


            if (group.GroupID == -1)
                throw new Arena.Services.Exceptions.ResourceNotFoundException("Invalid group ID");

            if (RestApi.GroupClusterOperationAllowed(ArenaContext.Current.Person.PersonID, group.GroupClusterID, OperationType.View) == false)
                throw new Exception("Access denied.");

            return mapper.FromArena(group);
        }

        /// <summary>
        /// Find all people who are members of the small group and return their
        /// person IDs. All members are returned including in-active members. If the
        /// small group has no members then an empty array is returned.
        /// </summary>
        /// <param name="groupID">The small group to find members of.</param>
        /// <param name="start">The 0-based index to start retrieving at.</param>
        /// <param name="max">The maximum number of members to retrieve.</param>
        /// <returns>Integer array of personIDs.</returns>
        [WebGet(UriTemplate = "smgp/group/{groupID}/members?start={start}&max={max}")]
        public Contracts.GenericListResult<Contracts.SmallGroupMember> GetSmallGroupMembers(int groupID, int start, int max)
        {
            Contracts.GenericListResult<Contracts.SmallGroupMember> list = new Contracts.GenericListResult<Contracts.SmallGroupMember>();
            Contracts.SmallGroupMemberMapper mapper = new Contracts.SmallGroupMemberMapper();
            Group group = new Group(groupID);
            int i;


            if (RestApi.GroupClusterOperationAllowed(ArenaContext.Current.Person.PersonID, group.GroupClusterID, OperationType.View) == false)
                throw new Exception("Access denied.");

            group.LoadMemberArray();
            list.Start = start;
            list.Max = max;
            list.Total = group.Members.Count;
            list.Items = new List<Contracts.SmallGroupMember>();

            for (i = start; i < group.Members.Count && (max > 0 ? i < (start + max) : true); i++)
            {
                list.Items.Add(mapper.FromArena(group.Members[i]));
            }

            return list;
        }

    }
}
