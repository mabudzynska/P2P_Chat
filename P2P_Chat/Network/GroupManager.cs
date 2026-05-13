using System;
using System.Collections.Generic;
using System.Text;

using System.Collections.Concurrent;

namespace P2P_Chat.Network
{
    internal class GroupManager
    {
        private readonly ConcurrentDictionary<string, Group> groups = new();

        public void CreateGroup(string groupId, string groupName, string hostName)
        {
            groups[groupId] = new Group
            {
                GroupId = groupId,
                GroupName = groupName,
                HostName = hostName
            };
        }

        public void AddMember(string groupId, string member)
        {
            if (groups.TryGetValue(groupId, out var group))
            {
                if (!group.Members.Contains(member))
                {
                    group.Members.Add(member);
                }
            }
        }

        public void RemoveMember(string groupId, string member)
        {
            if (groups.TryGetValue(groupId, out var group))
            {
                group.Members.Remove(member);
            }
        }

        public Group? GetGroup(string groupId)
        {
            groups.TryGetValue(groupId, out var group);
            return group;
        }

        public List<Group> GetGroups()
        {
            return groups.Values.ToList();
        }
    }
}