using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using DG.Tweening;

public class IntroCutscene : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Transform cameraHolder;

    [SerializeField] private CanvasGroup mainGameHUD;
    [SerializeField] private CanvasGroup blindfoldCanvasGroup;
    [SerializeField] private Volume drowsinessVolume;

    [SerializeField] private AudioSource phoneAudioSource;
    [SerializeField] private SubtitleManager subtitleManager;

    [SerializeField] private Vector3 deskCameraRotation = new Vector3(35f, -10f, 5f);
    [SerializeField] private Vector3 standingCameraRotation = Vector3.zero;

    private void Start()
    {
        StartCoroutine(CutsceneSequence());
    }

    private IEnumerator CutsceneSequence()
    {
        if (playerController != null)
        {
            playerController.SetControlActive(false);
        }

        if (mainGameHUD != null)
        {
            mainGameHUD.alpha = 0f;
        }

        if (cameraHolder != null)
        {
            cameraHolder.localRotation = Quaternion.Euler(deskCameraRotation);
        }

        if (blindfoldCanvasGroup != null)
        {
            blindfoldCanvasGroup.alpha = 1f;
        }

        if (drowsinessVolume != null)
        {
            drowsinessVolume.weight = 1f;
        }

        yield return new WaitForSeconds(1.5f);

        if (blindfoldCanvasGroup != null)
        {
            yield return blindfoldCanvasGroup.DOFade(0.4f, 0.8f).SetUpdate(true).WaitForCompletion();
            yield return blindfoldCanvasGroup.DOFade(0.9f, 0.4f).SetUpdate(true).WaitForCompletion();
        }

        if (subtitleManager != null && phoneAudioSource != null)
        {
            subtitleManager.PlaySubtitles(phoneAudioSource);
        }
        else if (phoneAudioSource != null)
        {
            phoneAudioSource.Play();
        }

        yield return new WaitForSeconds(0.5f);

        if (blindfoldCanvasGroup != null)
        {
            blindfoldCanvasGroup.DOFade(0f, 2.0f).SetUpdate(true);
        }

        if (cameraHolder != null)
        {
            cameraHolder.DOLocalRotate(standingCameraRotation, 3.0f)
                .SetEase(Ease.OutSine)
                .SetUpdate(true);
        }

        if (drowsinessVolume != null)
        {
            DOTween.To(() => drowsinessVolume.weight, x => drowsinessVolume.weight = x, 0f, 4.5f)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }

        yield return new WaitForSeconds(3.5f);

        if (playerController != null)
        {
            playerController.SetControlActive(true);
        }

        if (mainGameHUD != null)
        {
            mainGameHUD.DOFade(1f, 1.0f).SetUpdate(true);
        }
    }
}