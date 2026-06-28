using UnityEngine;
using UnityEngine.UI;

public class Battle_Unit : MonoBehaviour
{
    [Header("Stats")]
    public int maxHp = 50;
    public int currentHp;
    public int attack = 8;
    public int defense = 2;

    public int Attack => attack;
    public int Defense => defense;
    public int CurrentHp => currentHp;
    public int MaxHp => maxHp;

    public bool IsDead => currentHp <= 0;

    [Header("UI")]
    [SerializeField] private GameObject hpBarRoot;
    [SerializeField] private Slider healthBar;
    [SerializeField] private Slider easeBar;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hitSound;

    private void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        HideHpBar();
    }

    private void Update()
    {
        if (easeBar != null)
        {
            easeBar.value = Mathf.Lerp(
                easeBar.value,
                healthBar.value,
                5f * Time.deltaTime
            );
        }
    }

    public void Initialize()
    {
        currentHp = maxHp;

        hpBarRoot.SetActive(true);

        healthBar.maxValue = maxHp;
        healthBar.value = currentHp;

        easeBar.maxValue = maxHp;
        easeBar.value = currentHp;
    }

    public void TakeDamage(int damage)
    {
        currentHp -= damage;

        currentHp = Mathf.Max(0, currentHp);

        healthBar.value = currentHp;

        PlayHitSound();

        if (currentHp <= 0)
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

    public void HideHpBar()
    {
        if (hpBarRoot != null)
        {
            hpBarRoot.SetActive(false);
        }
    }
}