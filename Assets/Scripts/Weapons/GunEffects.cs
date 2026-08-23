using UnityEngine;

public sealed class GunEffects
{
    private readonly GameObject muzzleFlashInstance;
    private readonly ParticleSystem[] muzzleFlashParticles;
    private readonly AudioSource audioSource;
    private readonly AudioClip fireSound;
    private readonly float fireVolume;
    private readonly Light muzzleLight;
    private float muzzleLightTimer;

    public GunEffects(
        Transform muzzle,
        GameObject muzzleFlashPrefab,
        AudioSource audioSource,
        AudioClip fireSound,
        float fireVolume)
    {
        this.audioSource = audioSource;
        this.fireSound = fireSound;
        this.fireVolume = fireVolume;

        if (muzzle != null)
        {
            muzzleLight = muzzle.GetComponentInChildren<Light>(true);
            if (muzzleLight == null)
            {
                GameObject lightObject = new GameObject("MuzzleLight");
                lightObject.transform.SetParent(muzzle, false);
                muzzleLight = lightObject.AddComponent<Light>();
            }

            muzzleLight.type = LightType.Point;
            muzzleLight.range = 4.5f;
            muzzleLight.intensity = 0f;
            muzzleLight.color = new Color(1f, 0.72f, 0.34f);
            muzzleLight.enabled = false;
        }

        if (muzzleFlashPrefab == null)
        {
            Debug.LogWarning("[Gun] Muzzle Flash Prefab chưa được gán.");
            return;
        }

        muzzleFlashInstance = Object.Instantiate(muzzleFlashPrefab, muzzle);
        muzzleFlashInstance.transform.localPosition = Vector3.zero;
        muzzleFlashInstance.transform.localRotation = Quaternion.identity;
        muzzleFlashParticles =
            muzzleFlashInstance.GetComponentsInChildren<ParticleSystem>(true);
        muzzleFlashInstance.SetActive(false);
    }

    public void Play()
    {
        PlayMuzzleFlash();
        PlayMuzzleLight();
        PlayFireSound();
    }

    public void Tick(float deltaTime)
    {
        if (muzzleLight == null || muzzleLightTimer <= 0f)
        {
            return;
        }

        muzzleLightTimer -= deltaTime;
        float t = Mathf.Clamp01(muzzleLightTimer / 0.045f);
        muzzleLight.intensity = 8f * t;

        if (muzzleLightTimer <= 0f)
        {
            muzzleLight.intensity = 0f;
            muzzleLight.enabled = false;
        }
    }

    private void PlayMuzzleFlash()
    {
        if (muzzleFlashInstance == null)
        {
            return;
        }

        muzzleFlashInstance.SetActive(true);
        muzzleFlashInstance.transform.localRotation =
            Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        foreach (ParticleSystem particleSystem in muzzleFlashParticles)
        {
            particleSystem?.Play(true);
        }
    }

    private void PlayMuzzleLight()
    {
        if (muzzleLight == null)
        {
            return;
        }

        muzzleLight.enabled = true;
        muzzleLight.intensity = 8f;
        muzzleLightTimer = 0.045f;
    }

    private void PlayFireSound()
    {
        if (audioSource == null || fireSound == null)
        {
            return;
        }

        audioSource.pitch = Random.Range(0.96f, 1.04f);
        audioSource.PlayOneShot(fireSound, fireVolume);
    }
}
