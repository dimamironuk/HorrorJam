using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private int gameplaySceneIndex = 1;

    [Header("UI Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject infoPanel;

    [Header("Day/Night Cycle")]
    [SerializeField] private Light directionalLight;
    [SerializeField] private float rotationSpeed = 4f;
    [SerializeField] private Color dayColor = new Color(1f, 0.9f, 0.7f);
    [SerializeField] private Color nightColor = new Color(0.05f, 0.08f, 0.2f);
    [SerializeField] private float dayIntensity = 1.2f;
    [SerializeField] private float nightIntensity = 0.05f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        OpenMain();
    }

    private void Update()
    {
        UpdateDayNightCycle();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if ((settingsPanel != null && settingsPanel.activeSelf) || (infoPanel != null && infoPanel.activeSelf))
            {
                OpenMain();
            }
        }
    }

    private void UpdateDayNightCycle()
    {
        if (directionalLight == null) return;

        directionalLight.transform.Rotate(Vector3.right * (rotationSpeed * Time.deltaTime));

        float dot = Vector3.Dot(directionalLight.transform.forward, Vector3.down);
        float t = Mathf.Clamp01((dot + 1f) / 2f);

        directionalLight.color = Color.Lerp(nightColor, dayColor, t);
        directionalLight.intensity = Mathf.Lerp(nightIntensity, dayIntensity, t);
    }

    public void PlayGame()
    {
        SceneManager.LoadScene(gameplaySceneIndex);
    }

    public void OpenMain()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (infoPanel != null) infoPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (infoPanel != null) infoPanel.SetActive(false);
    }

    public void OpenInfo()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (infoPanel != null) infoPanel.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}