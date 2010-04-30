
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
        public Contracts.GenericListResult GetSmallGroupCategories()
        {
            Contracts.GenericListResult list = new Contracts.GenericListResult();
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
        [WebGet(UriTemplate = "smgp/category/{CategoryID}")]
        public Contracts.SmallGroupCategory GetSmallGroupCategory(int CategoryID)
        {
            Category category = new Category(CategoryID);
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
        [WebGet(UriTemplate = "smgp/cluster/list?CategoryID={CategoryID}&ClusterID={ClusterID}")]
        public Contracts.GenericListResult GetSmallGroupClusters(String CategoryID, String ClusterID)
        {
            GroupClusterCollection clusters;
            Contracts.GenericListResult list = new Contracts.GenericListResult();


            if (CategoryID != null)
            {
                clusters = new GroupClusterCollection(Convert.ToInt32(CategoryID), Convert.ToInt32(ConfigurationSettings.AppSettings["Organization"]));
            }
            else if (ClusterID != null)
            {
                if (RestApi.GroupClusterOperationAllowed(ArenaContext.Current.Person.PersonID, Convert.ToInt32(ClusterID), OperationType.View) == false)
                    throw new Exception("Access denied.");

                clusters = new GroupClusterCollection(Convert.ToInt32(ClusterID));
            }
            else
                throw new Exception("Required parameters not provided.");

            list.Start = 0;
            list.Max = list.Total = clusters.Count;
            list.Items = new List<Contracts.GenericReference>();
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
        [WebGet(UriTemplate = "smgp/cluster/{ClusterID}")]
        public Contracts.SmallGroupCluster GetSmallGroupCluster(int ClusterID)
        {
            Contracts.SmallGroupClusterMapper mapper = new Contracts.SmallGroupClusterMapper();
            GroupCluster cluster = new GroupCluster(ClusterID);


            if (cluster.GroupClusterID == -1)
                throw new Arena.Services.Exceptions.ResourceNotFoundException("Invalid cluster ID");

            if (RestApi.GroupClusterOperationAllowed(ArenaContext.Current.Person.PersonID, cluster.GroupClusterID, OperationType.View) == false)
                throw new Exception("Access denied.");

            return mapper.FromArena(cluster);
        }

        /// <summary>
        /// Retrieve a list of small groups which reside under the parent group
        /// cluster. If no small groups are found then an empty array is returned.
        /// </summary>
        /// <param name="clusterID">The parent cluster to find small groups under.</param>
        /// <returns>An integer array of small groups under the parent cluster.</returns>
        [WebGet(UriTemplate = "smgp/group/list?ClusterID={ClusterID}")]
        public Contracts.GenericListResult GetSmallGroups(int clusterID)
        {
            Contracts.GenericListResult list = new Contracts.GenericListResult();
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
        [WebGet(UriTemplate = "smgp/group/{GroupID}")]
        public Contracts.SmallGroup GetSmallGroup(int GroupID)
        {
            Contracts.SmallGroupMapper mapper = new Contracts.SmallGroupMapper();
            Group group = new Group(GroupID);


            if (group.GroupID == -1)
                throw new Arena.Services.Exceptions.ResourceNotFoundException("Invalid group ID");

            if (RestApi.GroupClusterOperationAllowed(ArenaContext.Current.Person.PersonID, group.GroupClusterID, OperationType.View) == false)
                throw new Exception("Access denied.");

            return mapper.FromArena(group);
        }

    }
}
