using UnityEngine;

public sealed class GunEffects
{
    private readonly GameObject muzzleFlashInstance;
    private readonly ParticleSystem[] muzzleFlashParticles;
    private readonly AudioSource audioSource;
    private readonly AudioClip fireSound;
    private readonly float fireVolume;

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
        PlayFireSound();
    }

    private void PlayMuzzleFlash()
    {
        if (muzzleFlashInstance == null)
        {
            return;
        }

        muzzleFlashInstance.SetActive(true);

        foreach (ParticleSystem particleSystem in muzzleFlashParticles)
        {
            particleSystem?.Play(true);
        }
    }

    private void PlayFireSound()
    {
        if (audioSource == null || fireSound == null)
        {
            return;
        }

        audioSource.PlayOneShot(fireSound, fireVolume);
    }
}