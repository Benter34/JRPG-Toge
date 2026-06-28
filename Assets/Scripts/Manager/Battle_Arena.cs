using UnityEngine;

public class Battle_Area : MonoBehaviour
{
    [Header("Party Formation Offsets")]
    [SerializeField] private Vector2[] partyOffsets =
    {
        new Vector2(-2.5f, 0.6f),
        new Vector2(-2.5f, -0.6f),
        new Vector2(-3.6f, 0.6f),
        new Vector2(-3.6f, -0.6f)
    };

    [Header("Enemy Formation Offsets")]
    [SerializeField] private Vector2[] enemyOffsets =
    {
        new Vector2(2.5f, 0f),
        new Vector2(3.5f, 0.8f),
        new Vector2(3.5f, -0.8f),
        new Vector2(4.5f, 0.8f),
        new Vector2(4.5f, -0.8f),
        new Vector2(5.5f, 0f)
    };

    public Vector3 GetPartyPosition(Vector3 battleCenter, int index)
    {
        if (index >= partyOffsets.Length)
            return battleCenter;

        Vector2 offset = partyOffsets[index];
        return battleCenter + new Vector3(offset.x, offset.y, 0f);
    }

    public Vector3 GetEnemyPosition(Vector3 battleCenter, int index)
    {
        if (index >= enemyOffsets.Length)
            return battleCenter;

        Vector2 offset = enemyOffsets[index];
        return battleCenter + new Vector3(offset.x, offset.y, 0f);
    }

    public Vector3 GetCameraPosition(Vector3 battleCenter)
    {
        return new Vector3(battleCenter.x, battleCenter.y, -10f);
    }
}