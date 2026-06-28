using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Battle_Enemy : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private string enemyName = "Slime";
    [SerializeField] private int maxHp = 20;
    [SerializeField] private int attack = 5;
    [SerializeField] private int defense = 1;

    [Header("UI")]
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private Slider healthBar;
    [SerializeField] private Slider easeBar;

    [SerializeField] private GameObject hpBarRoot;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hitSound;

    public string EnemyName => enemyName;
    public int Attack => attack;
    public int Defense => defense;
    public int CurrentHp { get; private set; }
    public int MaxHp => maxHp;
    public bool IsDead => CurrentHp <= 0;

    private void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        HideHpBar();
    }

    private void Update()
    {
        if (easeBar == null)
            return;

        easeBar.value = Mathf.Lerp(
            easeBar.value,
            healthBar.value,
            5f * Time.deltaTime
        );
    }

    public void Initialize()
    {
        CurrentHp = maxHp;

        gameObject.SetActive(true);

        hpBarRoot.SetActive(true);

        healthBar.maxValue = maxHp;
        healthBar.value = maxHp;

        easeBar.maxValue = maxHp;
        easeBar.value = maxHp;

        UpdateHpText();
    }

    public void HideHpBar()
    {
        if (hpBarRoot != null)
        {
            hpBarRoot.SetActive(false);
        }
    }

    public void TakeDamage(int damage)
    {
        CurrentHp -= damage;

        if (CurrentHp < 0)
        {
            CurrentHp = 0;
        }

        healthBar.value = CurrentHp;
        UpdateHpText();

        PlayHitSound();

        if (CurrentHp <= 0)
        {
            gameObject.SetActive(false);
        }
    }

    private void PlayHitSound()
    {
        if (audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound);
        }
    }
    private void UpdateHpText()
    {
        if (hpText == null)
            return;

        hpText.text =
            enemyName +
            " HP " +
            CurrentHp +
            "/" +
            maxHp;
    }
}