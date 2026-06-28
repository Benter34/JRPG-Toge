using UnityEngine;

public class NPC_Follower : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float followDistance = 1.5f;
    [SerializeField] private float followSpeed = 3f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("Does the sprite face right by default? If false, it faces left.")]
    [SerializeField] private bool spriteFacesRight = true;
    [Tooltip("Enable if your Animator's MoveX/MoveY are swapped (some controllers use MoveX as vertical).")]
    [SerializeField] private bool swapMoveAxes = false;
    [Tooltip("Invert the sign sent to MoveX parameter (useful if animator uses opposite X sign).")]
    [SerializeField] private bool invertMoveX = false;
    [Tooltip("Invert the sign sent to MoveY parameter (useful if animator uses opposite Y sign).")]
    [SerializeField] private bool invertMoveY = false;
    [Tooltip("Enable runtime debug logs for facing/axis sent to Animator.")]
    [SerializeField] private bool debugAnim = false;
    [Header("Boolean Direction Params")]
    [Tooltip("Use boolean direction parameters instead of MoveX/MoveY floats.")]
    [SerializeField] private bool useBooleanDirectionParameters = false;
    [SerializeField] private string paramIsUp = "IsUp";
    [SerializeField] private string paramIsDown = "IsDown";
    [SerializeField] private string paramIsLeft = "IsLeft";
    [SerializeField] private string paramIsRight = "IsRight";
    [Tooltip("Use an `IsAttacking` boolean instead of a Trigger for attack.")]
    [SerializeField] private bool useIsAttackingBool = false;
    [SerializeField] private string paramIsAttacking = "IsAttacking";

    [Header("Party")]
    [SerializeField] private Party_Manager partyManager;
    [Header("Follow Options")]
    [Tooltip("When true, mirror the target's (player) cardinal direction for animation instead of using movement-to-target direction.")]
    [SerializeField] private bool mirrorTargetDirection = false;

    private bool canFollow;
    private bool canMove = true;
    private Vector2 lastFacing = new Vector2(0f, -1f);

    private static bool HasAnimatorParameter(Animator a, string name)
    {
        if (a == null) return false;
        foreach (var p in a.parameters)
            if (p.name == name) return true;
        return false;
    }

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        if (!canFollow)
            return;

        if (!canMove)
            return;

        float distance = Vector2.Distance(
            transform.position,
            target.position
        );

        if (distance > followDistance)
        {
            Vector3 newPos = Vector2.MoveTowards(
                transform.position,
                target.position,
                followSpeed * Time.deltaTime
            );

            // Prefer mirroring the target's animator direction when enabled
            bool appliedMirror = false;
            if (mirrorTargetDirection && target != null)
            {
                var targetAnim = target.GetComponentInChildren<Animator>();
                if (targetAnim != null)
                {
                    Vector3 dir = (target.position - transform.position).normalized;

                    Vector2 snap;

                    if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
                    {
                        snap = new Vector2(Mathf.Sign(dir.x), 0f);
                    }
                    else
                    {
                        snap = new Vector2(0f, Mathf.Sign(dir.y));
                    }

                    lastFacing = snap;
                }
            }

            if (!appliedMirror)
            {
                Vector3 dir = (newPos - transform.position).normalized;
                // snap to cardinal direction for animator
                Vector2 snap;
                if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
                    snap = new Vector2(Mathf.Sign(dir.x), 0f);
                else
                    snap = new Vector2(0f, Mathf.Sign(dir.y));

                lastFacing = snap;
            }

            transform.position = newPos;

            ApplyFacingToAnimator(true);

            if (debugAnim)
            {
                Debug.Log(
                    "NPC Pos = " + transform.position +
                    " | Target Pos = " + target.position +
                    " | Facing = " + lastFacing
                );
            }
        }
        else
        {
            ApplyFacingToAnimator(false);
        }
    }

    private void ApplyFacingToAnimator(bool moving)
    {
        float ax = lastFacing.x;
        float ay = lastFacing.y;

        if (swapMoveAxes)
        {
            var tmp = ax;
            ax = ay;
            ay = tmp;
        }

        if (invertMoveX) ax = -ax;
        if (invertMoveY) ay = -ay;

        if (animator != null)
        {
            if (HasAnimatorParameter(animator, "IsMoving"))
                animator.SetBool("IsMoving", moving);

            if (useBooleanDirectionParameters)
            {
                Vector2 boolFacing = lastFacing;
                if (swapMoveAxes) boolFacing = new Vector2(lastFacing.y, lastFacing.x);
                if (invertMoveX) boolFacing.x = -boolFacing.x;
                if (invertMoveY) boolFacing.y = -boolFacing.y;

                bool isLeft = boolFacing.x < 0f;
                bool isRight = boolFacing.x > 0f;
                bool isUp = boolFacing.y > 0f;
                bool isDown = boolFacing.y < 0f;

                if (HasAnimatorParameter(animator, paramIsLeft))
                    animator.SetBool(paramIsLeft, isLeft);
                if (HasAnimatorParameter(animator, paramIsRight))
                    animator.SetBool(paramIsRight, isRight);
                if (HasAnimatorParameter(animator, paramIsUp))
                    animator.SetBool(paramIsUp, isUp);
                if (HasAnimatorParameter(animator, paramIsDown))
                    animator.SetBool(paramIsDown, isDown);
            }
            else
            {
                if (HasAnimatorParameter(animator, "MoveX"))
                    animator.SetFloat("MoveX", ax);

                if (HasAnimatorParameter(animator, "MoveY"))
                    animator.SetFloat("MoveY", ay);
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = false;
            }
        } 
    }

    // Make the NPC face a world position immediately (useful for battle facing)
    public void FaceTowards(Vector3 worldPosition)
    {
        Vector2 dir = (worldPosition - transform.position).normalized;

        Vector2 snap;
        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
            snap = new Vector2(Mathf.Sign(dir.x), 0f);
        else
            snap = new Vector2(0f, Mathf.Sign(dir.y));

        lastFacing = snap;

        ApplyFacingToAnimator(false);
    }

    public void FaceTransform(Transform t) => FaceTowards(t.position);

    public void StartFollowing()
    {
        canFollow = true;

        if (partyManager != null)
        {
            partyManager.AddMember(transform);
        }
    }

    public void StopFollowing()
    {
        canFollow = false;

        if (partyManager != null)
        {
            partyManager.RemoveMember(transform);
        }
    }

    public void SetCanMove(bool value)
    {
        canMove = value;
        if (!canMove && animator != null && HasAnimatorParameter(animator, "IsMoving"))
        {
            animator.SetBool("IsMoving", false);
        }
    }

    public void SplitFromPlayer()
    {
        StopFollowing();
        SetCanMove(false);
    }

    public bool IsFollowing()
    {
        return canFollow;
    }
}