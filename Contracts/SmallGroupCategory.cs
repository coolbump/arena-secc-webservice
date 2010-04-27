using System;
using System.Collections.Generic;
using System.Text;
using Arena.SmallGroup;
using Arena.Core;
using Arena.Services;

namespace Arena.Custom.HDC.WebService.Contracts
{
    /// Retrieve the basic information about a group category. This
    /// structure follows the standard RPC retrieval and update rules.
    /// </summary>
    public struct SmallGroupCategory
    {
        /// <summary>
        /// The unique ID number that identifies this group category.
        /// </summary>
        public int? CategoryID;

        /// <summary>
        /// The name of this group category.
        /// </summary>
        public string Name;

        /// <summary>
        /// Flag to indicate if this group category allows registrations.
        /// I really don't remember what group registrations are for and
        /// would love to update this documentation.
        /// </summary>
        public bool? AllowRegistrations;

        /// <summary>
        /// Flag to indicate if this category allows bulk update operations
        /// to be performed on the members of any small groups of this type
        /// of category. I think.
        /// </summary>
        public bool? AllowBulkUpdate;

        /// <summary>
        /// Wether or not the history of this category type is private. I
        /// would assume this is a person's history in the group. I am
        /// guessing, that this keeps the history private from fellow group
        /// members but not the leader, but I may be wrong.
        /// </summary>
        public bool? HistoryIsPrivate;

        /// <summary>
        /// Wether membership in any small group of this category type should
        /// count as small group membership. For example, a team group
        /// probably does not count as a small group.
        /// </summary>
        public bool? CreditAsSmallGroup;

        /// <summary>
        /// Flag indicating if small groups of this category type should be
        /// assigned to a specific area.
        /// </summary>
        public bool? UsesArea;

        /// <summary>
        /// Allow, Assign, or some such, uniform number. Generally this flag
        /// would only be used if this group category has to do with teams.
        /// </summary>
        public bool? UseUniformNumber;

        /// <summary>
        /// The default role of new members in small groups of this type of
        /// category.
        /// </summary>
        public Lookup DefaultRole;

        /// <summary>
        /// The caption to be used with the PrimaryAge member of the
        /// RpcSmallGroupInformation structure.
        /// </summary>
        public string AgeGroupCaption;

        /// <summary>
        /// The caption to be used with the Description member of the
        /// RpcSmallGroupInformation structure.
        /// </summary>
        public string DescriptionCaption;

        /// <summary>
        /// The caption to be used with the LeaderID member of the
        /// RpcSmallGroupInformation structure.
        /// </summary>
        public string LeaderCaption;

        /// <summary>
        /// The caption to be used with the TargetLocationID member
        /// of the RpcSmallGroupInformation structure.
        /// </summary>
        public string LocationTargetCaption;

        /// <summary>
        /// The caption to be used with the PrimaryMaritalStatus member
        /// of the RpcSmallGroupInformation structure.
        /// </summary>
        public string MaritalPreferenceCaption;

        /// <summary>
        /// The caption to be used with the MaximumMembers member of the
        /// RpcSmallGroupInformation structure.
        /// </summary>
        public string MaximumMembersCaption;

        /// <summary>
        /// The caption to be used with the MeetingDay member of the
        /// RpcSmallGroupInformation structure.
        /// </summary>
        public string MeetingDayCaption;

        /// <summary>
        /// The caption to be used with the Name member of the
        /// RpcSmallGroupInformation structure.
        /// </summary>
        public string NameCaption;

        /// <summary>
        /// The caption to be used with the Notes member of the
        /// RpcSmallGroupInformation structure.
        /// </summary>
        public string NotesCaption;

        /// <summary>
        /// The caption to be used with the ParentID member of the
        /// RpcSmallGroupInformation structure.
        /// </summary>
        public string ParentCaption;

        /// <summary>
        /// The caption to be used with the PictureUrl member of the
        /// RpcSmallGroupInformation structure.
        /// </summary>
        public string PictureCaption;

        /// <summary>
        /// The caption to be used with the Schedule member of the
        /// RpcSmallGroupInformation structure.
        /// </summary>
        public string ScheduleCaption;

        /// <summary>
        /// The caption to be used with the Topic member of the
        /// RpcSmallGroupInformation structure.
        /// </summary>
        public string TopicCaption;

        /// <summary>
        /// The caption to be used with the TypeID member of the
        /// RpcSmallGroupInformation structure.
        /// </summary>
        public string TypeCaption;

        /// <summary>
        /// The caption to be used with the Url member of the
        /// RpcSmallGroupInformation structure.
        /// </summary>
        public string UrlCaption;

        /// <summary>
        /// An array of RpcLookups which list the valid roles members
        /// are allowed to take on for small groups of this category
        /// type.
        /// </summary>
        public List<Lookup> ValidRoles;
    }

    class SmallGroupCategoryMapper : Arena.Services.Contracts.BaseMapper
    {
        public SmallGroupCategoryMapper()
        {
        }

        public SmallGroupCategory FromArena(Category arena)
        {
            SmallGroupCategory category = new SmallGroupCategory();
            LookupMapper lMapper = new LookupMapper();
            List<Lookup> roles = new List<Lookup>();


            category.CategoryID = arena.CategoryID;
            category.AgeGroupCaption = arena.AgeGroupCaption;
            category.AllowBulkUpdate = arena.AllowBulkUpdates;
            category.AllowRegistrations = arena.AllowRegistrations;
            category.CreditAsSmallGroup = arena.CreditAsSmallGroup;
            category.DefaultRole = lMapper.FromArena(arena.DefaultRole);
            category.DescriptionCaption = arena.DescriptionCaption;
            category.HistoryIsPrivate = arena.HistoryIsPrivate;
            category.LeaderCaption = arena.LeaderCaption;
            category.LocationTargetCaption = arena.LocationTargetCaption;
            category.MaritalPreferenceCaption = arena.MaritalPreferenceCaption;
            category.MaximumMembersCaption = arena.MaximumMembersCaption;
            category.MeetingDayCaption = arena.MeetingDayCaption;
            category.Name = arena.CategoryName;
            category.NameCaption = arena.NameCaption;
            category.NotesCaption = arena.NotesCaption;
            category.ParentCaption = arena.ParentCaption;
            category.PictureCaption = arena.PictureCaption;
            category.ScheduleCaption = arena.ScheduleCaption;
            category.TopicCaption = arena.TopicCaption;
            category.TypeCaption = arena.TypeCaption;
            category.UrlCaption = arena.UrlCaption;
            category.UsesArea = arena.UsesArea;
            category.UseUniformNumber = arena.UseUniformNumber;
            foreach (Core.Lookup lkup in arena.ValidRoles)
            {
                roles.Add(lMapper.FromArena(lkup));
            }
            category.ValidRoles = roles;

            return category;
        }
    }
}
