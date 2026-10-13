using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class BlackoutSequence : MonoBehaviour
{
    [Header("Lighting & Volume")]
    [SerializeField] private Volume blackoutVolume;
    [SerializeField] private GameObject warehouseLamps;

    [Header("Lightning Monster Enable")]
    [SerializeField] private LightningController lightningController; 

    [Header("Audio")]
    [SerializeField] private AudioSource blackoutSfxSource;
    [SerializeField] private AudioClip blackoutClip;
    [SerializeField] private AudioSource voiceAudioSource;

    [Header("Subtitles Integration")]
    [SerializeField] private SubtitleManager subtitleManager;

    [Header("Shelf Collapse Link")]
    [SerializeField] private ShelfCollapseSequence shelfCollapse;

    public void TriggerSequence()
    {
        StartCoroutine(BlackoutRoutine());
    }

    private IEnumerator BlackoutRoutine()
    {
        if (blackoutVolume != null)
        {
            blackoutVolume.weight = 1f;
        }

        if (warehouseLamps != null)
        {
            warehouseLamps.SetActive(false);
        }

        if (lightningController != null)
        {
            lightningController.EnableMonsterSpawns();
        }

        if (blackoutSfxSource != null && blackoutClip != null)
        {
            blackoutSfxSource.PlayOneShot(blackoutClip);
        }

        if (shelfCollapse != null)
        {
            shelfCollapse.TriggerCollapse();
        }

        yield return new WaitForSeconds(0.6f);

        if (subtitleManager != null)
        {
            subtitleManager.PlaySubtitles(voiceAudioSource);
        }
    }
}