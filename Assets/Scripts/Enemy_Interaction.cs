using UnityEngine;

public class Enemy_Interaction : MonoBehaviour
{
    [Header("Battle")]
    [SerializeField] private Battle_Manager battleManager;
    [SerializeField] private Battle_Area battleArea;

    private bool playerInRange;
    private bool battleStarted;

    private void Update()
    {
        if (!playerInRange)
        {
            return;
        }

        if (battleStarted)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartBattle();
        }
    }

    private void StartBattle()
    {
        battleStarted = true;
    }

    public void DefeatEnemy()
    {
        Destroy(gameObject);
    }

    public Battle_Area GetBattleArea()
    {
        return battleArea;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        playerInRange = true;
        Debug.Log("Press Space to attack Slime");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        playerInRange = false;
    }
}