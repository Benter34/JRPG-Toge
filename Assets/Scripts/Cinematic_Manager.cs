using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Cinematic_Manager : MonoBehaviour
{
    [SerializeField] private float cutsceneMoveSpeed = 3.5f;
    
    [Header("Camera")]
    [SerializeField] private Camera_Follow cameraFollow;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float lookDuration = 2f;
    [SerializeField] private float moveDuration = 1f;
    [SerializeField] private float distanceDurationFactor = 0.1f; // seconds per unit distance

    [Header("Fade")]
    [SerializeField] private Screen_Fader screenFader;

    [Header("Dialogue")]
    [SerializeField] private float dialogueDelay = 0.5f;

    [Header("End Message")]
    [SerializeField] private GameObject thankYouPanel;
    [SerializeField] private TMP_Text thankYouText;
    [SerializeField] private string thankYouMessage = "Thank you for playing bre";
    [SerializeField] private float thankYouDuration = 3f;

    private bool isCinematicActive;
    private Transform originalCameraTarget;
    private Vector3 originalCameraPosition;


    public void StartCinematic(
        Transform playerTransform,
        Transform npcTransform,
        NPC_Dialogue npcDialogue,
        NPC_Follower npcFollower,
        Vector3 fixedLookPosition,
        Vector3 walkDestination)
    {
        if (isCinematicActive)
            return;

        if (cameraFollow == null || cameraTransform == null)
        {
            Debug.LogWarning("Cinematic_Manager needs Camera_Follow and Camera Transform assigned.");
            return;
        }

        StartCoroutine(CinematicRoutine(
            playerTransform,
            npcTransform,
            npcDialogue,
            npcFollower,
            fixedLookPosition,
            walkDestination
        ));
    }

    private IEnumerator CinematicRoutine(
    Transform playerTransform,
    Transform npcTransform,
    NPC_Dialogue npcDialogue,
    NPC_Follower npcFollower,
    Vector3 fixedLookPosition,
    Vector3 walkDestination)
    {
        isCinematicActive = true;

        originalCameraTarget = cameraFollow.GetTarget();

        Player_Movement playerMovement = playerTransform.GetComponent<Player_Movement>();

        // Disable player
        if (playerMovement != null)
            playerMovement.SetCanMove(false);

        // Stop follower
        if (npcFollower != null)
        {
            npcFollower.SetCanMove(false);
            npcFollower.StopFollowing();
            npcFollower.FaceTowards(playerTransform.position);
        }

        // ==========================
        // Freeze Camera
        // ==========================

        FreezeCameraAt(fixedLookPosition);

        yield return new WaitForSeconds(lookDuration);

        // ==========================
        // Dialogue
        // ==========================

        if (npcDialogue != null)
        {
            npcDialogue.StartSplitDialogue();

            while (!npcDialogue.IsDialogueFinished())
                yield return null;
        }

        // ==========================
        // Camera kembali follow player
        // ==========================

        cameraFollow.SetTarget(playerTransform);

        yield return null;

        // ==========================
        // Jalan bareng
        // ==========================

        Coroutine playerMove = StartCoroutine(
            MoveActorTo(
                playerTransform,
                walkDestination,
                cutsceneMoveSpeed));

        Coroutine npcMove = StartCoroutine(
            MoveFollowerToPlayer(
                npcTransform,
                playerTransform,
                walkDestination,
                cutsceneMoveSpeed));

        yield return playerMove;
        yield return npcMove;

        // Stop animasi jalan
        UpdateMoveAnimation(playerTransform, Vector2.zero, false);
        UpdateMoveAnimation(npcTransform, Vector2.zero, false);

        // ==========================
        // Fade Hitam
        // ==========================

        if (screenFader != null)
            yield return screenFader.FadeIn();

        // ==========================
        // Credit
        // ==========================

        if (thankYouPanel != null)
        {
            thankYouPanel.SetActive(true);

            if (thankYouText != null)
                thankYouText.text = thankYouMessage;
        }

        yield return new WaitForSeconds(thankYouDuration);

        // ==========================
        // Main Menu
        // ==========================

        SceneManager.LoadScene("Main_Menu");
    }

    private IEnumerator MoveFollowerToPlayer(
    Transform npc,
    Transform player,
    Vector3 destination,
    float speed)
    {
        Animator anim = npc.GetComponentInChildren<Animator>();

        while (true)
        {
            Vector3 dir = (destination - player.position).normalized;

            Vector3 offset = -dir * 0.8f;

            Vector3 targetPos = player.position + offset;

            Vector3 moveDir = (targetPos - npc.position).normalized;

            if (anim != null)
            {
                anim.SetBool("IsMoving", true);
                anim.SetFloat("MoveX", moveDir.x);
                anim.SetFloat("MoveY", moveDir.y);
            }

            npc.position = Vector3.MoveTowards(
                npc.position,
                targetPos,
                speed * Time.deltaTime
            );

            if (Vector3.Distance(player.position, destination) < 0.05f)
                break;

            yield return null;
        }

        if (anim != null)
        {
            anim.SetBool("IsMoving", false);
        }
    }

    private void FreezeCameraAt(Vector3 position)
    {
        cameraFollow.SetTarget(null);
        Vector3 cameraPosition = new Vector3(position.x, position.y, cameraTransform.position.z);
        cameraTransform.position = cameraPosition;
    }

    private IEnumerator MoveActorTo(Transform actor, Transform target, float moveSpeed, bool followBehind = false)
    {
        if (actor == null || target == null)
            yield break;

        while (true)
        {
            Vector3 destination;

            if (followBehind)
            {
                Vector3 dir = (target.position - actor.position).normalized;
                destination = target.position - dir * 0.8f;
            }
            else
            {
                destination = target.position;
            }

            actor.position = Vector3.MoveTowards(
                actor.position,
                destination,
                moveSpeed * Time.deltaTime
            );

            if (Vector3.Distance(actor.position, destination) < 0.05f)
                break;

            yield return null;
        }
    }

    private IEnumerator MoveActorTo(
    Transform actor,
    Vector3 destination,
    float speed)
    {
        if (actor == null)
            yield break;

        Animator anim = actor.GetComponentInChildren<Animator>();

        while (Vector3.Distance(actor.position, destination) > 0.05f)
        {
            Vector3 dir = (destination - actor.position).normalized;

            // Animasi jalan
            if (anim != null)
            {
                anim.SetBool("IsMoving", true);
                anim.SetFloat("MoveX", dir.x);
                anim.SetFloat("MoveY", dir.y);
            }

            actor.position = Vector3.MoveTowards(
                actor.position,
                destination,
                speed * Time.deltaTime
            );

            yield return null;
        }

        // Idle
        if (anim != null)
        {
            anim.SetBool("IsMoving", false);
        }

        actor.position = destination;
    }

    private IEnumerator ShowThankYouMessage()
    {
        if (thankYouPanel != null)
        {
            if (thankYouText != null)
                thankYouText.text = thankYouMessage;

            thankYouPanel.SetActive(true);
        }

        yield return new WaitForSeconds(thankYouDuration);

        if (thankYouPanel != null)
            thankYouPanel.SetActive(false);
    }

    private void UpdateMoveAnimation(
    Transform actor,
    Vector2 direction,
    bool moving)
    {
        Animator animator =
            actor.GetComponentInChildren<Animator>();

        if (animator == null)
            return;

        animator.SetBool("IsMoving", moving);
        animator.SetFloat("MoveX", direction.x);
        animator.SetFloat("MoveY", direction.y);
    }
}
