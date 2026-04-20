using UnityEngine;

public sealed class SfxManager : MonoBehaviour
{
    public static SfxManager Instance { get; private set; }

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] [Range(0f, 1f)] private float masterVolume = 1f;

    [Header("Common Sfx")]
    [SerializeField] private AudioClip cardClickClip;
    [SerializeField] private AudioClip playButtonClip;
    [SerializeField] private AudioClip stageIntroClip;
    [SerializeField] private AudioClip correctClip;
    [SerializeField] private AudioClip wrongClip;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = masterVolume;
    }

    public void SetVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);

        if (audioSource != null)
            audioSource.volume = masterVolume;
    }

    public void Play(AudioClip clip)
    {
        if (clip == null || audioSource == null)
            return;

        audioSource.clip = clip;
        audioSource.volume = masterVolume;
        audioSource.Play();
    }

    public void PlayOneShot(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || audioSource == null)
            return;

        audioSource.PlayOneShot(clip, Mathf.Clamp01(masterVolume * volumeScale));
    }

    public void PlayCardClick()
    {
        PlayOneShot(cardClickClip);
    }

    public void PlayPlayButton()
    {
        PlayOneShot(playButtonClip);
    }

    public void PlayStageIntro()
    {
        PlayOneShot(stageIntroClip);
    }

    public void PlayCorrect()
    {
        PlayOneShot(correctClip);
    }

    public void PlayWrong()
    {
        PlayOneShot(wrongClip);
    }

    public void Stop()
    {
        if (audioSource == null)
            return;

        audioSource.Stop();
    }
}
