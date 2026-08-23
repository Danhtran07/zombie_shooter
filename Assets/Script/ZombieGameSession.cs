using UnityEngine;

public class ZombieGameSession : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private Gun weapon;
    [SerializeField] private float secondsPerWave = 30f;

    private int kills;
    private int level = 1;
    private int xp;
    private int xpToNextLevel = 5;
    private float elapsed;

    public int Kills => kills;
    public int Level => level;
    public int Xp => xp;
    public int XpToNextLevel => xpToNextLevel;
    public int Wave => Mathf.Max(1, Mathf.FloorToInt(elapsed / secondsPerWave) + 1);
    public float Elapsed => elapsed;
    public PlayerHealth PlayerHealth => playerHealth;

    private void OnEnable()
    {
        EnemyHealth.EnemyKilled += HandleEnemyKilled;
    }

    private void OnDisable()
    {
        EnemyHealth.EnemyKilled -= HandleEnemyKilled;
    }

    private void Awake()
    {
        if (playerHealth == null)
        {
            playerHealth = FindObjectOfType<PlayerHealth>();
        }

        if (playerCombat == null)
        {
            playerCombat = FindObjectOfType<PlayerCombat>();
        }

        if (weapon == null)
        {
            weapon = FindObjectOfType<Gun>();
        }
    }

    private void Update()
    {
        if (playerHealth != null && playerHealth.IsDead)
        {
            return;
        }

        elapsed += Time.deltaTime;
    }

    private void HandleEnemyKilled(EnemyHealth enemy)
    {
        kills++;
        AddXp(enemy != null ? enemy.XpReward : 1);
    }

    private void AddXp(int amount)
    {
        xp += Mathf.Max(0, amount);

        while (xp >= xpToNextLevel)
        {
            xp -= xpToNextLevel;
            LevelUp();
        }
    }

    private void LevelUp()
    {
        level++;
        xpToNextLevel = Mathf.CeilToInt(xpToNextLevel * 1.35f + 2f);

        if (weapon != null)
        {
            weapon.AddDamageMultiplier(1.1f);
            weapon.AddFireRateMultiplier(1.06f);
        }
    }
}
