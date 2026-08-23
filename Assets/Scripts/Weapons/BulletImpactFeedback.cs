using UnityEngine;

public class BulletImpactFeedback : MonoBehaviour
{
    private static BulletImpactFeedback instance;

    [Header("Pools")]
    [SerializeField] private ObjectPool enemyImpactPool;
    [SerializeField] private ObjectPool worldImpactPool;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] enemyImpactSounds;
    [SerializeField] private AudioClip[] worldImpactSounds;
    [SerializeField, Range(0f, 1f)] private float impactVolume = 0.7f;
    [SerializeField, Range(0f, 1f)] private float headshotVolume = 1f;

    private AudioClip fallbackEnemyImpact;
    private AudioClip fallbackWorldImpact;

    public static BulletImpactFeedback Instance => instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            enabled = false;
            return;
        }

        instance = this;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.65f;

        fallbackEnemyImpact = CreateNoiseClip("SoftImpact", 0.075f, 150f, 0.5f);
        fallbackWorldImpact = CreateNoiseClip("HardImpact", 0.055f, 720f, 0.35f);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public void Play(
        Vector3 point,
        Vector3 normal,
        bool hitEnemy,
        bool headshot,
        bool killed)
    {
        Quaternion rotation = normal.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(normal)
            : Quaternion.identity;

        ObjectPool pool = hitEnemy ? enemyImpactPool : worldImpactPool;
        if (pool != null)
        {
            GameObject effect = pool.Spawn(point, rotation);
            if (effect != null)
            {
                float scale = killed ? 1.35f : headshot ? 1.2f : 1f;
                effect.transform.localScale = Vector3.one * scale;
            }
        }

        PlayImpactSound(point, hitEnemy, headshot, killed);
    }

    private void PlayImpactSound(
        Vector3 point,
        bool hitEnemy,
        bool headshot,
        bool killed)
    {
        if (audioSource == null)
        {
            return;
        }

        AudioClip clip = PickClip(
            hitEnemy ? enemyImpactSounds : worldImpactSounds
        );

        if (clip == null)
        {
            clip = hitEnemy ? fallbackEnemyImpact : fallbackWorldImpact;
        }

        audioSource.transform.position = point;
        audioSource.pitch = killed ? 0.86f : headshot ? 1.12f : Random.Range(0.94f, 1.06f);
        audioSource.PlayOneShot(
            clip,
            headshot || killed ? headshotVolume : impactVolume
        );
    }

    private static AudioClip PickClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
        {
            return null;
        }

        return clips[Random.Range(0, clips.Length)];
    }

    private static AudioClip CreateNoiseClip(
        string name,
        float duration,
        float toneFrequency,
        float noiseAmount)
    {
        const int sampleRate = 22050;
        int sampleCount = Mathf.Max(1, Mathf.CeilToInt(sampleRate * duration));
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = 1f - i / (float)sampleCount;
            envelope *= envelope;

            float tone = Mathf.Sin(t * toneFrequency * Mathf.PI * 2f);
            float noise = Random.Range(-1f, 1f);
            samples[i] = Mathf.Lerp(tone, noise, noiseAmount) * envelope;
        }

        AudioClip clip = AudioClip.Create(
            name,
            sampleCount,
            1,
            sampleRate,
            false
        );
        clip.SetData(samples, 0);
        return clip;
    }
}
