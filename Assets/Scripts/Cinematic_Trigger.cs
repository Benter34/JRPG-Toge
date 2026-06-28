using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Cinematic_Trigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Cinematic_Manager cinematicManager;
    [SerializeField] private Transform npcTransform;
    [SerializeField] private NPC_Dialogue npcDialogue;
    [SerializeField] private NPC_Follower npcFollower;
    [SerializeField] private Transform walkDestination;
    [SerializeField] private Transform cinematicLookTarget;

    private void Awake()
    {
        if (cinematicManager == null)
            cinematicManager = FindAnyObjectByType<Cinematic_Manager>();

        if (cinematicManager == null)
            Debug.LogWarning("Cinematic_Manager not found in scene!");

        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        collider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        if (cinematicManager == null)
            return;

        if (npcTransform == null || npcDialogue == null || npcFollower == null || walkDestination == null)
            return;

        Vector3 lookPosition = cinematicLookTarget != null
            ? cinematicLookTarget.position
            : GetMidPoint(other.transform.position, npcTransform.position);

        cinematicManager.StartCinematic(
            other.transform,
            npcTransform,
            npcDialogue,
            npcFollower,
            lookPosition,
            walkDestination.position
        );

        gameObject.SetActive(false);
    }

    private static Vector3 GetMidPoint(Vector3 a, Vector3 b)
    {
        return (a + b) * 0.5f;
    }
}
