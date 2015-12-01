
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.ServiceModel.Web;
using Arena.Core;
using Arena.Security;
using Arena.SmallGroup;
using System.Data.SqlClient;
using Arena.DataLayer.SmallGroup;
using System.Globalization;
using Arena.DataLayer.Organization;
using System.IO;
using Arena.Custom.SECC.Common.Util;
using System.Drawing;
using System.Text;
using System.Drawing.Imaging;


namespace Arena.Custom.HDC.WebService.SECC
{
    class SmallGroupAPI
    {

        [WebGet(UriTemplate = "smgp/group/{groupID}/occurrences?start={start}&end={end}")]
        public Contracts.GenericListResult<Contracts.SmallGroupOccurrence> GetSmallGroupOccurences(int groupId, String start, String end)
        {
            var list = new Contracts.GenericListResult<Contracts.SmallGroupOccurrence>();
            list.Items = new List<Contracts.SmallGroupOccurrence>();

            Contracts.SmallGroupOccurrenceMapper mapper = new Contracts.SmallGroupOccurrenceMapper();
            Group group = new Group(groupId);

            foreach (GroupOccurrence occurrence in group.Occurrences)
            {
                if ( occurrence.StartTime >= DateTime.ParseExact( start, "yyyyMMddHHmmss", new CultureInfo( "en-us" ) )
                    && occurrence.EndTime <= DateTime.ParseExact( end, "yyyyMMddHHmmss", new CultureInfo( "en-us" ) ) )
                {
                    list.Items.Add( mapper.FromArena( occurrence ) );
                }
            }
            list.Start = 0;
            list.Total = list.Max = list.Items.Count;

            return list;
        }


        [WebGet(UriTemplate = "smgp/group/{groupID}/occurrences/{occurrenceID}")]
        public Contracts.SmallGroupOccurrence GetSmallGroupOccurrence( int groupID, int occurrenceID )
        {
            
            Contracts.SmallGroupOccurrenceMapper mapper = new Contracts.SmallGroupOccurrenceMapper();
            Group group = new Group( groupID );

            var occurrence = group.Occurrences.FirstOrDefault( o => o.OccurrenceID == occurrenceID );

            return mapper.FromArena( occurrence );

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
            {
                bool groupLeader = false;
                // Check to see if they are a leader within this cluster
                Arena.Custom.SECC.Data.SmallGroup.GroupCollection gc = new Arena.Custom.SECC.Data.SmallGroup.GroupCollection();
                gc.LoadByLeaderPersonID(ArenaContext.Current.Person.PersonID);

                foreach (Group group in gc)
                {
                    if (group.Active && group.GroupClusterID == clusterID)
                    {
                        groupLeader = true;
                        break;
                    }
                }

                if (!groupLeader) {
                    throw new Exception("Access denied.");
                }
            }

            return mapper.FromArena(cluster);
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
            GroupMember leader = new GroupMember(groupID, group.Leader);
            leader.Role = new Lookup(new Guid("029B270C-7B7A-499F-8006-CC3211C91E95"));
            group.Members.Add(leader);
            
            Boolean accessDenied = false;
            // If this person isn't the outright leader and they don't have view access
            if (group.Leader.PersonID != ArenaContext.Current.Person.PersonID &&
                RestApi.GroupClusterOperationAllowed(ArenaContext.Current.Person.PersonID, group.GroupClusterID, OperationType.View) == false) {

                accessDenied = true;

                // Do a deeper dive into each member of the group
                foreach(GroupMember gm in group.Members) {
                    if (gm.Active && gm.Role.Value == "Leader")
                    {
                        accessDenied = false;
                        break;
                    }
                }
            }
            if (accessDenied) {
                throw new Exception("Access denied.");
            }

            list.Start = start;
            list.Max = max;
            list.Total = group.Members.Count;
            list.Items = new List<Contracts.SmallGroupMember>();
                
            int i;
            for (i = start; i < group.Members.Count && (max <= 0 || i < (start + max)); i++)
            {
                list.Items.Add(mapper.FromArena(group.Members[i]));
            }

            return list;
        }

        [WebInvoke(Method="POST", 
                UriTemplate= "smgp/group/{id}/occurrence/update")]
        public Arena.Services.Contracts.ModifyResult UpdateSmallGroupOccurrence(System.IO.Stream stream, int id)
        {
            System.Xml.Serialization.XmlSerializer xmlSerializer = new System.Xml.Serialization.XmlSerializer( typeof( Contracts.SmallGroupOccurrence ) );
            Contracts.SmallGroupOccurrence occurrence = (Contracts.SmallGroupOccurrence)xmlSerializer.Deserialize( stream );

            Contracts.SmallGroupOccurrenceMapper mapper = new Contracts.SmallGroupOccurrenceMapper();
            try
            {
                if ( occurrence.OccurrenceID > 0 )
                {
                    return mapper.Update( id, occurrence );
                }
                else
                {
                    return mapper.Create( id, occurrence );
                }
            }
            catch ( Exception ex )
            {
                return new Services.Contracts.ModifyResult() { Successful = false.ToString(), 
                    ErrorMessage = string.Format( "{0} - {1}", ex.GetType().ToString(), ex.Message ) };
            }

        }


        [WebInvoke(Method = "POST",
                UriTemplate = "smgp/group/{id}/photo/update")]
        public Arena.Services.Contracts.ModifyResult UpdateSmallGroupPhoto(System.IO.Stream stream, int id)
        {
            try
            {
                Arena.SmallGroup.Group group = new Arena.SmallGroup.Group(id);

                Boolean accessDenied = false;
                // If this person isn't the outright leader and they don't have edit access
                if (group.Leader.PersonID != ArenaContext.Current.Person.PersonID &&
                    RestApi.GroupClusterOperationAllowed(ArenaContext.Current.Person.PersonID, group.GroupClusterID, OperationType.Edit) == false)
                {

                    accessDenied = true;

                    // Do a deeper dive into each member of the group
                    foreach (GroupMember gm in group.Members)
                    {
                        if (gm.Active && gm.Role.Value == "Leader")
                        {
                            accessDenied = false;
                            break;
                        }
                    }
                }
                if (accessDenied)
                {
                    throw new Exception("Access denied.");
                }

                MultipartParser parser = new MultipartParser(stream);
                if (parser.Success)
                {

                    // Make sure this is a real image
                    MemoryStream ms = new MemoryStream(parser.FileContents);
                    String myStream = Encoding.ASCII.GetString(ms.ToArray());
                    Image image = Image.FromStream(ms);

                    ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
                    ImageCodecInfo codec = codecs.First(c => c.FormatID == image.RawFormat.Guid);

                    // If we have an existing image, delete it
                    if (group.ImageBlob.BlobID > 0)
                    {
                        group.ImageBlob.Delete();
                    }

                    // Create a new file
                    Utility.ArenaImage arenaImage = new Utility.ArenaImage();

                    // Update everything
                    arenaImage.ByteArray = parser.FileContents;
                    arenaImage.MimeType = codec.MimeType;
                    arenaImage.OriginalFileName = Path.GetFileNameWithoutExtension(parser.Filename) + "." + codec.FormatDescription.ToLower().Replace("jpeg", "jpg");
                    arenaImage.FileExtension = codec.FormatDescription.ToLower().Replace("jpeg", "jpg");

                    // Save the file
                    group.ImageBlob = arenaImage;
                    group.ImageBlob.Save(ArenaContext.Current.User.Identity.Name);
                    group.Save(ArenaContext.Current.User.Identity.Name);
                }

                return new Services.Contracts.ModifyResult()
                {
                    Link = Contracts.SmallGroupMapper.getImageThumbnailUrl(group.ImageBlob),
                    Successful = true.ToString()
                };
            }
            catch (Exception ex)
            {
                return new Services.Contracts.ModifyResult()
                {
                    Successful = false.ToString(),
                    ErrorMessage = string.Format("{0} - {1}", ex.GetType().ToString(), ex.Message)
                };
            }

        }
    }
}
