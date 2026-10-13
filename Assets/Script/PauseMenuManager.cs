using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;

public class PauseMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject mainButtonsPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("First Selected Elements")]
    [SerializeField] private GameObject firstPauseButton;
    [SerializeField] private GameObject firstSettingsButton;

    [Header("Blur Volume")]
    [SerializeField] private Volume pauseBlurVolume;

    [Header("Player Controls (Disables rotation & movement)")]
    [SerializeField] private MonoBehaviour playerMovementScript;
    [SerializeField] private MonoBehaviour cameraLookScript;

    private bool _isPaused;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                OpenMainPauseMenu();
            }
            else
            {
                TogglePause();
            }
        }
    }

    public void TogglePause()
    {
        _isPaused = !_isPaused;

        Time.timeScale = _isPaused ? 0f : 1f;

        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(_isPaused);

        if (pauseBlurVolume != null)
            pauseBlurVolume.weight = _isPaused ? 1f : 0f;

        // Вимикаємо скрипти керування гравцем і камерою під час паузи
        if (playerMovementScript != null) playerMovementScript.enabled = !_isPaused;
        if (cameraLookScript != null) cameraLookScript.enabled = !_isPaused;

        Cursor.lockState = _isPaused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = _isPaused;

        if (_isPaused)
            OpenMainPauseMenu();
    }

    public void OpenMainPauseMenu()
    {
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        SetSelectedObject(firstPauseButton);
    }

    public void OpenSettings()
    {
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        SetSelectedObject(firstSettingsButton);
    }

    private void SetSelectedObject(GameObject obj)
    {
        if (EventSystem.current != null && obj != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(obj);
        }
    }
}