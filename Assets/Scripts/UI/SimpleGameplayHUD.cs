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

    private void Awake()
    {
        if (session == null)
        {
            session = FindObjectOfType<ZombieGameSession>();
        }
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
    }
}
