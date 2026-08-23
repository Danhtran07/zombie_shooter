using UnityEngine;
using UnityEngine.UI;

public class SimpleGameplayHUD : MonoBehaviour
{
    [SerializeField] private ZombieGameSession session;
    [SerializeField] private Text hpText;
    [SerializeField] private Slider xpBar;
    [SerializeField] private Text levelText;
    [SerializeField] private Text killText;
    [SerializeField] private Text waveText;
    [SerializeField] private Text timerText;
    [SerializeField] private Text feedbackText;

    private float feedbackTimer;

    private void Awake()
    {
        if (session == null)
        {
            session = FindObjectOfType<ZombieGameSession>();
        }
    }

    private void OnEnable()
    {
        EnemyHealth.EnemyDamaged += HandleEnemyDamaged;
    }

    private void OnDisable()
    {
        EnemyHealth.EnemyDamaged -= HandleEnemyDamaged;
    }

    private void Update()
    {
        if (session == null)
        {
            return;
        }

        PlayerHealth health = session.PlayerHealth;

        if (hpText != null && health != null)
        {
            hpText.text =
                $"HP: {Mathf.CeilToInt(health.CurrentHealth)}/{Mathf.CeilToInt(health.MaxHealth)}";
        }

        if (xpBar != null)
        {
            xpBar.maxValue = Mathf.Max(1, session.XpToNextLevel);
            xpBar.value = session.Xp;
        }

        if (levelText != null)
        {
            levelText.text = $"Level: {session.Level}";
        }

        if (killText != null)
        {
            killText.text = $"Kill: {session.Kills}";
        }

        if (waveText != null)
        {
            waveText.text = $"Wave: {session.Wave}";
        }

        if (timerText != null)
        {
            int seconds = Mathf.FloorToInt(session.Elapsed);
            timerText.text = $"Time: {seconds / 60:00}:{seconds % 60:00}";
        }

        UpdateFeedbackText();
    }

    private void HandleEnemyDamaged(
        EnemyHealth enemy,
        float damage,
        bool headshot,
        bool killed)
    {
        if (feedbackText == null)
        {
            return;
        }

        feedbackTimer = killed || headshot ? 0.42f : 0.16f;
        feedbackText.text = killed
            ? headshot ? "HEADSHOT KILL" : "KILL"
            : headshot ? "HEADSHOT" : Mathf.CeilToInt(damage).ToString();

        feedbackText.fontSize = killed ? 34 : headshot ? 30 : 22;
        feedbackText.color = killed || headshot
            ? new Color(1f, 0.88f, 0.18f, 1f)
            : new Color(1f, 1f, 1f, 0.95f);
        feedbackText.enabled = true;
    }

    private void UpdateFeedbackText()
    {
        if (feedbackText == null || !feedbackText.enabled)
        {
            return;
        }

        feedbackTimer -= Time.deltaTime;
        if (feedbackTimer <= 0f)
        {
            feedbackText.enabled = false;
            return;
        }

        Color color = feedbackText.color;
        color.a = Mathf.Clamp01(feedbackTimer * 4f);
        feedbackText.color = color;
    }
}
