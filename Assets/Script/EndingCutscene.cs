using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndingCutscene : MonoBehaviour
{
    [Header("Player Control & Camera")]
    [SerializeField] private MonoBehaviour playerMovementScript;
    [SerializeField] private Transform playerCameraTransform;
    [SerializeField] private Transform lookTargetBehind;

    [Header("Cutscene Elements")]
    [SerializeField] private GameObject shadowJumpscareObject;
    [SerializeField] private Light playerFlashlight;
    [SerializeField] private Light flashLight;

    [Header("Audio")]
    [SerializeField] private AudioSource jumpscareAudioSource;
    [SerializeField] private AudioClip jumpscareClip;

    [Header("UI & Screen Fade")]
    [SerializeField] private GameObject gameplayUI;               // Весь HUD/UI гри (приціл, завдання тощо)
    [SerializeField] private CanvasGroup fadeScreenGroup;         // Чорна панель
    [SerializeField] private GameObject showOverTextObject;      // Текст "SHOW OVER"

    [Header("Timings & Scene Transition")]
    [SerializeField] private float rotationDuration = 1.2f;       // Тривалість розвороту камери
    [SerializeField] private float dropCameraDuration = 0.6f;     // Тривалість падіння камери
    [SerializeField] private float textHoldDuration = 3.0f;        // Час показу тексту перед меню
    [SerializeField] private int mainMenuSceneIndex = 0;          // Індекс сцени головного меню

    private bool _isEndingStarted = false;

    public void StartEndingSequence()
    {
        if (_isEndingStarted) return;
        _isEndingStarted = true;

        StartCoroutine(EndingRoutine());
    }

    private IEnumerator EndingRoutine()
    {
        // 1. Блокуємо керування та ВИМИКАЄМО весь ігровий UI
        if (playerMovementScript != null)
            playerMovementScript.enabled = false;

        if (gameplayUI != null)
            gameplayUI.SetActive(false);

        yield return new WaitForSeconds(0.2f);

        // 2. Дуже плавний розворот камери назад (Mathf.SmoothStep)
        if (playerCameraTransform != null && lookTargetBehind != null)
        {
            Quaternion startRotation = playerCameraTransform.rotation;
            Vector3 direction = (lookTargetBehind.position - playerCameraTransform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            float elapsed = 0f;
            while (elapsed < rotationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / rotationDuration);
                playerCameraTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                yield return null;
            }
            playerCameraTransform.rotation = targetRotation;
        }

        yield return new WaitForSeconds(0.15f);

        // 3. Блискавка + Скример + Крик
        if (flashLight != null)
            flashLight.intensity = 150000f;

        if (shadowJumpscareObject != null)
        {
            shadowJumpscareObject.transform.position = playerCameraTransform.position + playerCameraTransform.forward * 1.8f;
            shadowJumpscareObject.transform.rotation = Quaternion.LookRotation(-playerCameraTransform.forward);
            shadowJumpscareObject.SetActive(true);
        }

        if (jumpscareAudioSource != null && jumpscareClip != null)
        {
            jumpscareAudioSource.PlayOneShot(jumpscareClip);
        }

        yield return new WaitForSeconds(0.2f);

        // 4. Гасне ліхтарик та світло
        if (playerFlashlight != null)
            playerFlashlight.enabled = false;

        if (flashLight != null)
            flashLight.intensity = 0f;

        // 5. Плавне падіння камери на підлогу
        if (playerCameraTransform != null)
        {
            Vector3 startPos = playerCameraTransform.position;
            Vector3 targetPos = startPos + Vector3.down * 1.2f + playerCameraTransform.forward * 0.3f;
            Quaternion startRot = playerCameraTransform.rotation;
            Quaternion targetRot = startRot * Quaternion.Euler(20f, 0f, 45f);

            float elapsed = 0f;
            while (elapsed < dropCameraDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / dropCameraDuration);
                playerCameraTransform.position = Vector3.Lerp(startPos, targetPos, t);
                playerCameraTransform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }
        }

        yield return new WaitForSeconds(0.3f);

        // 6. Плавне затемнення екрана у чорний колір
        if (fadeScreenGroup != null)
        {
            float fadeElapsed = 0f;
            float fadeDuration = 1.2f;
            while (fadeElapsed < fadeDuration)
            {
                fadeElapsed += Time.deltaTime;
                fadeScreenGroup.alpha = Mathf.SmoothStep(0f, 1f, fadeElapsed / fadeDuration);
                yield return null;
            }
            fadeScreenGroup.alpha = 1f;
        }

        yield return new WaitForSeconds(0.5f);

        // 7. Поява тексту "SHOW OVER" з плавним розмиттям (якщо є CanvasGroup на тексті)
        if (showOverTextObject != null)
        {
            showOverTextObject.SetActive(true);

            if (showOverTextObject.TryGetComponent<CanvasGroup>(out var textGroup))
            {
                textGroup.alpha = 0f;
                float textElapsed = 0f;
                while (textElapsed < 1f)
                {
                    textElapsed += Time.deltaTime;
                    textGroup.alpha = Mathf.SmoothStep(0f, 1f, textElapsed);
                    yield return null;
                }
                textGroup.alpha = 1f;
            }
        }

        // 8. Пауза перед переходом у меню
        yield return new WaitForSeconds(textHoldDuration);

        // 9. Завантаження Головного Меню (Index 0)
        SceneManager.LoadScene(mainMenuSceneIndex);
    }
}