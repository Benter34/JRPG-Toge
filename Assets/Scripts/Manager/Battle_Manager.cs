using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Battle_Manager : MonoBehaviour
{
    private enum BattleState { None, PlayerTurn, SelectingTarget, EnemyTurn, Won, Lost }
    private enum AttackRangeType { Melee, Ranged }

    [Header("Camera")]
    [SerializeField] private Camera_Follow cameraFollow;
    [Header("Attack Timing")]
    [Tooltip("Normalized time within the attack clip when damage should be applied (0..1).")]
    [SerializeField] private float hitNormalizedTime = 0.45f;
    [Tooltip("Substring to look for in the attack animation clip name when detecting the attack clip.")]
    [SerializeField] private string attackClipNameFilter = "attack";
    private Transform originalCameraTarget;

    [Header("Battle UI")]
    [SerializeField] private GameObject battlePanel;
    [SerializeField] private TMP_Text playerHpText;
    [SerializeField] private TMP_Text battleLogText;

    [Header("Command Buttons")]
    [SerializeField] private Button attackButton;
    [SerializeField] private Button skillButton;
    [SerializeField] private Button defendButton;

    [Header("Target UI")]
    [SerializeField] private GameObject targetPanel;
    [SerializeField] private Button[] targetButtons;
    [SerializeField] private TMP_Text[] targetButtonTexts;

    [Header("Player Stats")]
    [SerializeField] private int playerMaxHp = 30;
    [SerializeField] private int playerAttack = 10;
    [SerializeField] private int playerDefense = 2;
    [SerializeField] private GameObject playerHpBarRoot;
    [SerializeField] private Slider playerHealthBar;
    [SerializeField] private Slider playerEaseBar;

    [Header("Party")]
    [SerializeField] private Party_Manager partyManager;
    [SerializeField] private Player_Movement playerMovement;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip playerHitSound;

    [Header("World Battle")]
    [SerializeField] private Screen_Fader screenFader;

    [SerializeField] private Battle_Unit oldMan;

    private int playerCurrentHp;
    private bool playerIsDefending;
    private bool selectedSkill;

    // Pending attack info used when an Animation Event triggers the damage
    private Battle_Enemy pendingAttackTarget;
    private int pendingAttackDamage;
    private bool pendingAttackApplied;

    private Vector3[] partyPositionsBeforeBattle;
    private NPC_Follower[] activeFollowers;

    private Enemy_Group currentGroup;
    private Battle_Enemy[] currentEnemies;
    private Transform[] partyMembers;
    private BattleState currentState;

    private Transform PlayerTransform => partyMembers[0];
    
    private Vector3 lastBattleDirection = Vector3.right;

    private void Update()
    {
        if (playerEaseBar != null)
        {
            playerEaseBar.value = Mathf.Lerp(
                playerEaseBar.value,
                playerHealthBar.value,
                5f * Time.deltaTime
            );
        }
    }

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        attackButton.onClick.AddListener(OnAttackButton);
        skillButton.onClick.AddListener(OnSkillButton);
        defendButton.onClick.AddListener(PlayerDefend);

        battlePanel.SetActive(false);
        targetPanel.SetActive(false);

        if (playerHpBarRoot != null)
        {
            playerHpBarRoot.SetActive(false);
        }
    }
    
    private bool IsPlayerTarget(Transform target)
    {
        return target == PlayerTransform;
    }

    public void StartBattle(Enemy_Group group)
    {
        StartCoroutine(StartBattleRoutine(group));
    }

    private void DisableFollowersDuringBattle()
    {
        activeFollowers =
            FindObjectsByType<NPC_Follower>(FindObjectsSortMode.None);

        foreach (NPC_Follower follower in activeFollowers)
        {
            if (follower.IsFollowing())
            {
                follower.SetCanMove(false);
            }
        }
    }

    private void EnableFollowersAfterBattle()
    {
        if (activeFollowers == null)
            return;

        foreach (NPC_Follower follower in activeFollowers)
        {
            follower.SetCanMove(true);
        }
    }

    private IEnumerator StartBattleRoutine(Enemy_Group group)
    {
        currentGroup = group;
        currentEnemies = group.GetEnemies();
        partyMembers = partyManager.GetActivePartyMembers();

        playerMovement.SetCanMove(false);
        DisableFollowersDuringBattle();
        SavePartyPositions();

        yield return screenFader.FadeIn();

        SetupBattlePositions();

        Animator anim =
        PlayerTransform.GetComponentInChildren<Animator>();

        if(anim != null)
        {
            anim.SetBool("IsMoving", false);
            anim.SetFloat("MoveX", 1f);
            anim.SetFloat("MoveY", 0f);
        }
            InitializeEnemies();

        playerCurrentHp = playerMaxHp;
        playerHpBarRoot.SetActive(true);

        playerHealthBar.maxValue = playerMaxHp;
        playerHealthBar.value = playerCurrentHp;

        playerEaseBar.maxValue = playerMaxHp;
        playerEaseBar.value = playerCurrentHp;

        if(oldMan != null)
        {
            oldMan.Initialize();
        }

        playerIsDefending = false;
        currentState = BattleState.PlayerTurn;

        battlePanel.SetActive(true);
        targetPanel.SetActive(false);

        battleLogText.text = "Enemies appear!";
        UpdateBattleUI();
        SetCommandButtons(true);

        yield return screenFader.FadeOut();
    }

    private void SavePartyPositions()
    {
        partyPositionsBeforeBattle = new Vector3[partyMembers.Length];

        for (int i = 0; i < partyMembers.Length; i++)
        {
            partyPositionsBeforeBattle[i] = partyMembers[i].position;
        }
    }

    private void SetupBattlePositions()
    {
        Battle_Area area = currentGroup.GetBattleArea();
        Vector3 battleCenter = currentGroup.transform.position;

        if (cameraFollow != null)
        {
            originalCameraTarget = cameraFollow.GetTarget();
            cameraFollow.SetTarget(null);
            cameraFollow.transform.position = area.GetCameraPosition(battleCenter);
        }

        for (int i = 0; i < partyMembers.Length; i++)
        {
            partyMembers[i].position = area.GetPartyPosition(battleCenter, i);
        }

        for (int i = 0; i < currentEnemies.Length; i++)
        {
            currentEnemies[i].transform.position = area.GetEnemyPosition(battleCenter, i);
        }
    }

    private void RestorePartyPositions()
    {
        for (int i = 0; i < partyMembers.Length; i++)
        {
            partyMembers[i].position = partyPositionsBeforeBattle[i];
        }
    }

    private void InitializeEnemies()
    {
        foreach (Battle_Enemy enemy in currentEnemies)
        {
            enemy.Initialize();
        }
    }

    private void OnAttackButton()
    {
        if (currentState != BattleState.PlayerTurn) return;

        selectedSkill = false;
        ShowTargetSelection();
    }

    private void OnSkillButton()
    {
        if (currentState != BattleState.PlayerTurn) return;

        selectedSkill = true;
        ShowTargetSelection();
    }

    private void ShowTargetSelection()
    {
        currentState = BattleState.SelectingTarget;

        SetCommandButtons(false);
        targetPanel.SetActive(true);

        for (int i = 0; i < targetButtons.Length; i++)
        {
            int targetIndex = i;
            bool hasEnemy = i < currentEnemies.Length && !currentEnemies[i].IsDead;

            targetButtons[i].gameObject.SetActive(hasEnemy);

            if (hasEnemy)
            {
                targetButtonTexts[i].text = currentEnemies[i].EnemyName;
                targetButtons[i].onClick.RemoveAllListeners();
                targetButtons[i].onClick.AddListener(() => SelectTarget(targetIndex));
            }
        }

        battleLogText.text = "Choose your target.";
    }

    private void SelectTarget(int enemyIndex)
    {
        if (currentState != BattleState.SelectingTarget) return;

        Battle_Enemy selectedEnemy = currentEnemies[enemyIndex];

        if (selectedEnemy.IsDead) return;

        targetPanel.SetActive(false);

        int attackPower = selectedSkill ? playerAttack * 2 : playerAttack;
        string logPrefix = selectedSkill ? "Player uses Sword Skill for" : "Player attacks for";

        StartCoroutine(PlayerAttackRoutine(selectedEnemy, attackPower, logPrefix));
    }

    private IEnumerator PlayerAttackRoutine(Battle_Enemy targetEnemy, int attackPower, string logPrefix)
    {
        Vector3 startPosition = PlayerTransform.position;

        yield return MoveToTarget(PlayerTransform, targetEnemy.transform, AttackRangeType.Melee);

        // Prepare pending damage so Animation Event can apply it precisely on hit frame
        int damage = CalculateDamage(attackPower, targetEnemy.Defense);
        pendingAttackTarget = targetEnemy;
        pendingAttackDamage = damage;
        pendingAttackApplied = false;

        // Trigger attack animation (Animation Event should call OnAttackHit)
        yield return PlayAttackAnimation(PlayerTransform);

        // If Animation Event didn't run (user didn't add event), apply damage as fallback
        if (!pendingAttackApplied && pendingAttackTarget != null)
        {
            pendingAttackTarget.TakeDamage(pendingAttackDamage);
            pendingAttackApplied = true;
        }

        battleLogText.text = logPrefix + " " + pendingAttackDamage + " damage.";
        UpdateBattleUI();

        // Clear pending
        pendingAttackTarget = null;
        pendingAttackDamage = 0;

        yield return PlayIdleAfterAttack(PlayerTransform);
        yield return MoveBack(PlayerTransform, startPosition);

        EndPlayerAction();
    }

    private void PlayerDefend()
    {
        if (currentState != BattleState.PlayerTurn) return;

        playerIsDefending = true;
        battleLogText.text = "Player defends.";

        EndPlayerAction();
    }

    private void EndPlayerAction()
    {
        SetCommandButtons(false);

        if (AreAllEnemiesDead())
        {
            WinBattle();
            return;
        }

        StartCoroutine(OldManTurnRoutine());
    }

    private IEnumerator OldManTurnRoutine()
    {
        if (oldMan == null)
        {
            currentState = BattleState.EnemyTurn;
            StartCoroutine(EnemyTurnRoutine());
            yield break;
        }

        Battle_Enemy targetEnemy = null;

        foreach (Battle_Enemy enemy in currentEnemies)
        {
            if (!enemy.IsDead)
            {
                targetEnemy = enemy;
                break;
            }
        }

        if (targetEnemy == null)
        {
            WinBattle();
            yield break;
        }

        Vector3 startPosition = oldMan.transform.position;

        battleLogText.text = "Old Man attacks!";

        yield return MoveToTarget(
            oldMan.transform,
            targetEnemy.transform,
            AttackRangeType.Melee
        );

        // Prepare pending damage so Animation Event or PlayAttackAnimation can apply it
        int damage = CalculateDamage(oldMan.Attack, targetEnemy.Defense);
        pendingAttackTarget = targetEnemy;
        pendingAttackDamage = damage;
        pendingAttackApplied = false;

        yield return PlayAttackAnimation(oldMan.transform);

        // fallback if animation didn't apply damage
        if (!pendingAttackApplied && pendingAttackTarget != null)
        {
            pendingAttackTarget.TakeDamage(pendingAttackDamage);
            pendingAttackApplied = true;
        }

        battleLogText.text = "Old Man deals " + pendingAttackDamage + " damage.";
        UpdateBattleUI();

        yield return new WaitForSeconds(0.25f);
        Animator anim = oldMan.GetComponentInChildren<Animator>();

        if(anim != null)
        {
            anim.ResetTrigger("Attack");
            anim.SetBool("IsMoving", false);
        }

        yield return MoveBack(
            oldMan.transform,
            startPosition
        );

        if (AreAllEnemiesDead())
        {
            WinBattle();
            yield break;
        }

        currentState = BattleState.EnemyTurn;

        StartCoroutine(EnemyTurnRoutine());
    }

    private IEnumerator EnemyTurnRoutine()
    {
        foreach (Battle_Enemy enemy in currentEnemies)
        {
            if (enemy.IsDead) continue;

            Transform target = GetRandomLivingPartyTarget();
            Vector3 enemyStartPosition = enemy.transform.position;

            yield return MoveToTarget(enemy.transform, target, AttackRangeType.Melee);
            yield return MoveToTarget(enemy.transform, target, AttackRangeType.Melee);

            Animator anim = enemy.GetComponentInChildren<Animator>();

            if(anim != null)
            {
                anim.SetTrigger("Attack");
            }

            yield return new WaitForSeconds(0.3f);

            yield return new WaitForSeconds(0.25f);

            int damage = CalculateDamage(enemy.Attack, playerDefense);

            if (IsPlayerTarget(target))
            {
                if (playerIsDefending)
                {
                    damage = Mathf.Max(1, damage / 2);
                }

                playerCurrentHp -= damage;

                if (playerHealthBar != null)
                {
                    playerHealthBar.value = playerCurrentHp;
                }

                PlayPlayerHitSound();

                battleLogText.text =
                    enemy.EnemyName +
                    " attacks Player for " +
                    damage +
                    " damage.";

                UpdateBattleUI();

                if (playerCurrentHp <= 0)
                {
                    LoseBattle();
                    yield break;
                }
            }
            else
            {
                Battle_Unit targetUnit =
                    target.GetComponent<Battle_Unit>();

                if (targetUnit != null)
                {
                    targetUnit.TakeDamage(damage);

                    battleLogText.text =
                        enemy.EnemyName +
                        " attacks Old Man for " +
                        damage +
                        " damage.";
                }
            }

            yield return new WaitForSeconds(0.25f);
            yield return MoveBack(enemy.transform, enemyStartPosition);

            if (playerCurrentHp <= 0)
            {
                LoseBattle();
                yield break;
            }
        }

        playerIsDefending = false;
        currentState = BattleState.PlayerTurn;
        SetCommandButtons(true);

        battleLogText.text = "Player turn.";
    }

    private Transform GetRandomLivingPartyTarget()
    {
        System.Collections.Generic.List<Transform> aliveTargets =
            new System.Collections.Generic.List<Transform>();

        foreach (Transform member in partyMembers)
        {
            if (member == PlayerTransform)
            {
                if (playerCurrentHp > 0)
                {
                    aliveTargets.Add(member);
                }
            }
            else
            {
                Battle_Unit unit =
                    member.GetComponent<Battle_Unit>();

                if (unit != null && !unit.IsDead)
                {
                    aliveTargets.Add(member);
                }
            }
        }

        if (aliveTargets.Count == 0)
            return PlayerTransform;

        return aliveTargets[
            Random.Range(0, aliveTargets.Count)
        ];
    }

    private IEnumerator MoveToTarget(Transform attacker, Transform target, AttackRangeType rangeType)
    {
        if (rangeType == AttackRangeType.Ranged)
            yield break;

        Vector3 attackPosition = GetPositionNearTarget(attacker, target);

        Animator animator = attacker.GetComponentInChildren<Animator>();

        while (Vector3.Distance(attacker.position, attackPosition) > 0.05f)
        {
            Vector3 direction = (attackPosition - attacker.position).normalized;

            lastBattleDirection = direction;

            SetAnimatorMoving(animator, direction, true);

            attacker.position = Vector3.MoveTowards(
                attacker.position,
                attackPosition,
                8f * Time.deltaTime
            );

            yield return null;
        }

        SetAnimatorFacing(animator, lastBattleDirection);
    }

    private IEnumerator MoveBack(Transform attacker, Vector3 startPosition)
    {
        Animator animator = attacker.GetComponentInChildren<Animator>();

        Vector3 lastDirection = lastBattleDirection;

        while (Vector3.Distance(attacker.position, startPosition) > 0.05f)
        {
            Vector3 direction = (startPosition - attacker.position).normalized;

            lastDirection = direction;

            SetAnimatorMoving(animator, direction, true);

            attacker.position = Vector3.MoveTowards(
                attacker.position,
                startPosition,
                8f * Time.deltaTime
            );

            yield return null;
        }

        SetAnimatorFacing(animator, -lastDirection);
    }

    private static bool HasAnimatorParameter(Animator animator, string paramName)
    {
        if (animator == null) return false;

        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.name == paramName) return true;
        }

        return false;
    }

    private static void SetAnimatorMoving(Animator animator, Vector3 direction, bool moving)
    {
        if (animator == null) return;

        if (HasAnimatorParameter(animator, "IsMoving"))
        {
            animator.SetBool("IsMoving", moving);
        }

        if (HasAnimatorParameter(animator, "MoveX"))
        {
            animator.SetFloat("MoveX", direction.x);
        }

        if (HasAnimatorParameter(animator, "MoveY"))
        {
            animator.SetFloat("MoveY", direction.y);
        }
    }

    private static void SetAnimatorFacing(Animator animator, Vector3 direction)
    {
        if (animator == null) return;

        if (HasAnimatorParameter(animator, "IsMoving"))
        {
            animator.SetBool("IsMoving", false);
        }

        if (HasAnimatorParameter(animator, "MoveX"))
        {
            animator.SetFloat("MoveX", direction.x);
        }

        if (HasAnimatorParameter(animator, "MoveY"))
        {
            animator.SetFloat("MoveY", direction.y);
        }
    }

    private IEnumerator PlayAttackAnimation(Transform actor)
    {
        Animator animator = actor.GetComponentInChildren<Animator>();

        if (animator == null)
        {
            yield return new WaitForSeconds(0.25f);
            yield break;
        }

        // Make sure locomotion params are off so Attack state can play cleanly
        if (HasAnimatorParameter(animator, "IsMoving"))
            animator.SetBool("IsMoving", false);

        // Primary: trigger Attack trigger and wait for Attack state to complete
        if (HasAnimatorParameter(animator, "Attack"))
        {
            animator.SetTrigger("Attack");

            float timeout = 2f;
            float t = 0f;

            while (t < timeout)
            {
                var st = animator.GetCurrentAnimatorStateInfo(0);

                // detect attack by state name or current clip name
                bool inAttackState = st.IsName("Attack");

                var clips = animator.GetCurrentAnimatorClipInfo(0);
                if (!inAttackState && clips.Length > 0)
                {
                    var clipName = clips[0].clip != null ? clips[0].clip.name : string.Empty;
                    if (!string.IsNullOrEmpty(clipName) && clipName.ToLower().Contains(attackClipNameFilter.ToLower()))
                        inAttackState = true;
                }

                if (inAttackState)
                {
                    // while playing attack, apply pending damage at configured hitNormalizedTime
                    while (st.normalizedTime < 1f && t < timeout)
                    {
                        if (!pendingAttackApplied && pendingAttackTarget != null && st.normalizedTime >= hitNormalizedTime)
                        {
                            pendingAttackTarget.TakeDamage(pendingAttackDamage);
                            pendingAttackApplied = true;
                            UpdateBattleUI();
                        }

                        yield return null;
                        t += Time.deltaTime;
                        st = animator.GetCurrentAnimatorStateInfo(0);
                    }

                    // ensure animator returns to idle/locomotion state
                    if (HasAnimatorParameter(animator, "IsMoving"))
                        animator.SetBool("IsMoving", false);

                    yield return null;

                    // allow one frame for transition
                    yield return null;

                    yield break;
                }

                yield return null;
                t += Time.deltaTime;
            }

            // timeout fallback
            yield break;
        }
                        // float hitNormalizedTime = 0.2f; // adjust to your hit frame
        // Fallback: boolean-style attacking param
        if (HasAnimatorParameter(animator, "IsAttacking"))
        {
            animator.SetBool("IsAttacking", true);

            // wait a short default duration (clip may be ~0.3-0.6s)
            yield return new WaitForSeconds(0.45f);

            animator.SetBool("IsAttacking", false);
            yield break;
        }

        // Final fallback short delay
        yield return new WaitForSeconds(0.25f);
    }

    private static IEnumerator PlayIdleAfterAttack(Transform actor)
    {
        Animator animator = actor.GetComponentInChildren<Animator>();

        if (animator == null)
        {
            yield return new WaitForSeconds(0.1f);
            yield break;
        }

        if (HasAnimatorParameter(animator, "IsAttacking") && HasAnimatorParameter(animator, "IsMoving"))
        {
            animator.SetBool("IsAttacking", false);
            animator.SetBool("IsMoving", false);
        }

        yield return new WaitForSeconds(0.05f);
    }

    // Called by Animation Event on the attack clip to apply damage at the correct frame.
    public void OnAttackHit()
    {
        if (pendingAttackTarget != null && !pendingAttackApplied)
        {
            pendingAttackTarget.TakeDamage(pendingAttackDamage);
            pendingAttackApplied = true;
            UpdateBattleUI();
        }
    }

    private Vector3 GetPositionNearTarget(Transform attacker, Transform target)
    {
        Vector3 directionToTarget = (target.position - attacker.position).normalized;
        float stopDistance = 0.8f;

        return target.position - directionToTarget * stopDistance;
    }

    private int CalculateDamage(int attack, int defense)
    {
        return Mathf.Max(1, attack - defense);
    }

    private bool AreAllEnemiesDead()
    {
        foreach (Battle_Enemy enemy in currentEnemies)
        {
            if (!enemy.IsDead)
                return false;
        }

        return true;
    }

    private void WinBattle()
    {
        currentState = BattleState.Won;

        battleLogText.text = "You defeated all monsters!";
        SetCommandButtons(false);
        targetPanel.SetActive(false);

        if (currentGroup != null)
        {
            currentGroup.DefeatGroup();
        }

        Invoke(nameof(EndBattle), 2f);
    }

    private void LoseBattle()
    {
        currentState = BattleState.Lost;
        playerCurrentHp = 0;

        battleLogText.text = "You were defeated...";
        UpdateBattleUI();

        SetCommandButtons(false);
        targetPanel.SetActive(false);

        Invoke(nameof(EndBattle), 2f);
    }

    private void EndBattle()
    {
        StartCoroutine(EndBattleRoutine());
    }

    private void TriggerPostBattleDialogues()
    {
        if (activeFollowers == null)
            return;

        foreach (NPC_Follower follower in activeFollowers)
        {
            if (follower.IsFollowing())
            {
                NPC_Dialogue dialogue = follower.GetComponent<NPC_Dialogue>();
                if (dialogue != null && dialogue.CanStartPostBattleDialogue())
                {
                    dialogue.StartPostBattleDialogue();
                    break; // Show only one at a time for now
                }
            }
        }
    }

    private IEnumerator EndBattleRoutine()
    {
        yield return screenFader.FadeIn();

        battlePanel.SetActive(false);
        targetPanel.SetActive(false);

        RestorePartyPositions();
        EnableFollowersAfterBattle();
        
        TriggerPostBattleDialogues();

        if (cameraFollow != null)
        {
            cameraFollow.SetTarget(originalCameraTarget);
        }

        if (playerMovement != null)
        {
            playerMovement.SetCanMove(true);
        }
        foreach (Battle_Enemy enemy in currentEnemies)
        {
            if (enemy != null)
            {
                enemy.HideHpBar();
            }
        }

        playerHpBarRoot.SetActive(false);

        if(oldMan != null)
        {
            oldMan.HideHpBar();
        }

        foreach(Battle_Enemy enemy in currentEnemies)
        {
            if(enemy != null)
            {
                enemy.HideHpBar();
            }
        }

        currentGroup = null;
        currentEnemies = null;
        partyMembers = null;
        currentState = BattleState.None;

        yield return screenFader.FadeOut();
    }

    private void UpdateBattleUI()
    {
        playerHpText.text = "Player HP: " + Mathf.Max(0, playerCurrentHp) + " / " + playerMaxHp;
    }

    private void PlayPlayerHitSound()
    {
        if (audioSource != null && playerHitSound != null)
        {
            audioSource.PlayOneShot(playerHitSound);
        }
    }

    private void SetCommandButtons(bool value)
    {
        attackButton.interactable = value;
        skillButton.interactable = value;
        defendButton.interactable = value;
    }
}