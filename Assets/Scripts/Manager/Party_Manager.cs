using System.Collections.Generic;
using UnityEngine;

public class Party_Manager : MonoBehaviour
{
    [Header("Main Player")]
    [SerializeField] private Transform player;

    private readonly List<Transform> activePartyMembers = new List<Transform>();

    private void Awake()
    {
        activePartyMembers.Clear();
        activePartyMembers.Add(player);
    }

    public void AddMember(Transform member)
    {
        if (member == null) return;
        if (activePartyMembers.Contains(member)) return;

        activePartyMembers.Add(member);
    }

    public void RemoveMember(Transform member)
    {
        if (member == null) return;
        if (member == player) return;

        activePartyMembers.Remove(member);
    }

    public Transform[] GetActivePartyMembers()
    {
        return activePartyMembers.ToArray();
    }
}