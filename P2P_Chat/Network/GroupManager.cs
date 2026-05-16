using System;
using System.Collections.Generic;
using System.Text;

// Użycie ConcurrentDictionary zapewnia bezpieczeństwo wątkowe przy operacjach na grupach
using System.Collections.Concurrent;

namespace P2P_Chat.Network
{
    /* Klasa odpowiedzialna za zarządzanie grupami czatowymi w pamięci aplikacji */
    internal class GroupManager
    {
        // Słownik przechowujący grupy, gdzie kluczem jest unikalny identyfikator grupy (GroupId)
        private readonly ConcurrentDictionary<string, Group> groups = new();

        /* Tworzy nową grupę lub aktualizuje istniejącą na podstawie otrzymanych danych */
        public void CreateGroup(string groupId, string groupName, string hostName)
        {
            groups[groupId] = new Group
            {
                GroupId = groupId,
                GroupName = groupName,
                HostName = hostName
            };
        }

        /* Dodaje nowego członka (nick) do listy osób w konkretnej grupie */
        public void AddMember(string groupId, string member)
        {
            // Sprawdzenie czy grupa o danym ID istnieje w słowniku
            if (groups.TryGetValue(groupId, out var group))
            {
                // Dodaj członka tylko jeśli nie ma go jeszcze na liście (unikamy duplikatów)
                if (!group.Members.Contains(member))
                {
                    group.Members.Add(member);
                }
            }
        }

        /* Usuwa użytkownika z listy członków danej grupy */
        public void RemoveMember(string groupId, string member)
        {
            if (groups.TryGetValue(groupId, out var group))
            {
                group.Members.Remove(member);
            }
        }

        /* Pobiera obiekt konkretnej grupy na podstawie jej identyfikatora */
        public Group? GetGroup(string groupId)
        {
            // Próbuje pobrać grupę; jeśli nie znajdzie, zwróci null
            groups.TryGetValue(groupId, out var group);
            return group;
        }

        /* Zwraca listę wszystkich zarejestrowanych grup (używane do odświeżania GUI) */
        public List<Group> GetGroups()
        {
            return groups.Values.ToList();
        }
    }
}