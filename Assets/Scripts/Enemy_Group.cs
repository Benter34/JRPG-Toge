using UnityEngine;

public class Enemy_Group : MonoBehaviour
{
    [Header("Battle")]
    [SerializeField] private Battle_Manager battleManager;
    [SerializeField] private Battle_Area battleArea;

    [Header("Enemies")]
    [SerializeField] private Battle_Enemy[] enemies;

    private bool playerInRange;
    private bool battleStarted;

    private void Update()
    {
        if (!playerInRange) return;
        if (battleStarted) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            battleStarted = true;
            battleManager.StartBattle(this);
        }
    }

    public Battle_Enemy[] GetEnemies()
    {
        return enemies;
    }

    public Battle_Area GetBattleArea()
    {
        return battleArea;
    }

    public void DefeatGroup()
    {
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = true;
        Debug.Log("Press Space to attack enemy group.");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerInRange = false;
    }
}