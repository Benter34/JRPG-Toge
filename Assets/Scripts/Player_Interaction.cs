using UnityEngine;

public class Player_Interaction : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactionRange = 1.5f;
    [SerializeField] private LayerMask npcLayer;
    [SerializeField] private Player_Movement playerMovement;

    [Header("Input Delay")]
    [SerializeField] private float inputCooldown = 0.25f;

    private NPC_Dialogue currentNpc;
    private bool isTalking;
    private float nextInputTime;

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Space))
        {
            return;
        }

        if (Time.time < nextInputTime)
        {
            return;
        }

        nextInputTime = Time.time + inputCooldown;

        HandleInteractionInput();
    }
    

    private void HandleInteractionInput()
    {
        if (isTalking)
        {
            bool dialogueStillRunning = currentNpc.ShowNextLine();

            if (!dialogueStillRunning)
            {
                currentNpc = null;
                isTalking = false;

                playerMovement.SetCanMove(true);
            }

            return;
        }

        TryStartDialogue();
    }

    private void TryStartDialogue()
    {
        Collider2D npcCollider = Physics2D.OverlapCircle(
            transform.position,
            interactionRange,
            npcLayer
        );

        if (npcCollider == null)
        {
            return;
        }

        NPC_Dialogue npcDialogue = npcCollider.GetComponent<NPC_Dialogue>();

        if (npcDialogue == null)
        {
            return;
        }

        if (!npcDialogue.CanStartDialogue())
        {
            return;
        }

        currentNpc = npcDialogue;
        isTalking = true;

        playerMovement.SetCanMove(false);
        currentNpc.StartDialogue();
        
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}