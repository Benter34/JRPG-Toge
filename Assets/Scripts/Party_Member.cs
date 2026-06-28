using UnityEngine;

public class Party_Member : MonoBehaviour
{
    [Header("Info")]
    public string CharacterName;

    [Header("Stats")]
    public int MaxHp = 30;
    public int CurrentHp = 30;
    public int Attack = 10;
    public int Defense = 2;

    public bool IsDead => CurrentHp <= 0;

    private void Awake()
    {
        CurrentHp = MaxHp;
    }
}