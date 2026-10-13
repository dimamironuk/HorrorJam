using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SubtitleManager : MonoBehaviour
{
    [System.Serializable]
    public struct SubtitleLine
    {
        public float startTime;
        public float duration;
        [TextArea(2, 4)] public string text;
    }

    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Header("Voice Lines")]
    [SerializeField] private List<SubtitleLine> subtitleLines = new List<SubtitleLine>();

    public void PlaySubtitles(AudioSource audioSource = null)
    {
        StopAllCoroutines();
        StartCoroutine(SubtitleRoutine(audioSource));
    }

    private IEnumerator SubtitleRoutine(AudioSource audioSource)
    {
        if (subtitleText == null) yield break;

        subtitleText.text = "";
        subtitleText.gameObject.SetActive(true);

        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
        }

        float startRealTime = Time.time;

        foreach (var line in subtitleLines)
        {
            float targetTime = startRealTime + line.startTime;

            while (Time.time < targetTime)
            {
                yield return null;
            }

            subtitleText.text = line.text;

            yield return new WaitForSeconds(line.duration);

            subtitleText.text = "";
        }

        subtitleText.gameObject.SetActive(false);
    }
}