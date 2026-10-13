using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.HighDefinition;
using TMPro;
using Zenject;

public class SettingsManager : MonoBehaviour
{
    [Header("Camera & HDRP")]
    [SerializeField] private Camera mainCamera;
    private HDAdditionalCameraData _hdCameraData;

    [Header("Section: GAMEPLAY")]
    [SerializeField] private UIHorizontalSelector displayModeSelector;
    [SerializeField] private UIHorizontalSelector maxFpsSelector;
    [SerializeField] private UIHorizontalSelector resolutionSelector;
    [SerializeField] private UIHorizontalSelector dlssSelector;
    [SerializeField] private UIHorizontalSelector vsyncSelector;
    [SerializeField] private UIHorizontalSelector languageSelector;

    [Header("Section: GRAPHICS")]
    [SerializeField] private UIHorizontalSelector overallQualitySelector;
    [SerializeField] private UIHorizontalSelector shadowsSelector;
    [SerializeField] private UIHorizontalSelector texturesSelector;
    [SerializeField] private UIHorizontalSelector postProcessingSelector;

    [Header("Section: AUDIO")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TextMeshProUGUI masterVolumeText;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private TextMeshProUGUI musicVolumeText;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TextMeshProUGUI sfxVolumeText;
    [SerializeField] private Slider ambienceVolumeSlider;
    [SerializeField] private TextMeshProUGUI ambienceVolumeText;

    [Header("Section: CONTROLS")]
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private TextMeshProUGUI mouseSensitivityValueText;
    [SerializeField] private UIHorizontalSelector invertYSelector;

    [InjectOptional] private LocalizationManager _localizationManager;

    private List<Resolution> _filteredResolutions = new List<Resolution>();
    private readonly int[] _fpsLimits = { 30, 60, 120, 144, 240, -1 };

    private readonly string[] _gameplayRows = { "Section_GAMEPLAY", "Row_DisplayMode", "Row_MaxFPS", "Row_Resolution", "Row_DLSS", "Row_VSync", "Row_Language" };
    private readonly string[] _graphicsRows = { "Section_GRAPHICS", "Row_OverallQuality", "Row_Shadows", "Row_Textures", "Row_PostProcessing" };
    private readonly string[] _audioRows = { "Section_AUDIO", "Row_MasterVolume", "Row_MusicVolume", "Row_SFXVolume", "Row_Ambience" };
    private readonly string[] _controlsRows = { "Section_CONTROLS", "Section_MOUSE", "Row_MouseSensitivity", "Row_InvertY", "Section_KEYBINDINGS", "Row_Key_Interact", "Row_Key_Flashlight" };

    private bool _isInitializing = false;

    private void Reset() => AutoAssignAll();
    private void OnValidate() => AutoAssignAll();

    private void Awake()
    {
        AutoAssignAll();

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null) _hdCameraData = mainCamera.GetComponent<HDAdditionalCameraData>();
    }

    private void OnEnable()
    {
        InitializeAndLoad();
    }

    private void Start()
    {
        InitializeAndLoad();
    }

    private void InitializeAndLoad()
    {
        _isInitializing = true;

        AutoAssignAll();

        SafeExecute(SetupDisplayMode, "DisplayMode");
        SafeExecute(SetupResolutions, "Resolutions");
        SafeExecute(SetupDLSS, "DLSS");
        SafeExecute(SetupVSync, "VSync");
        SafeExecute(SetupMaxFPS, "MaxFPS");
        SafeExecute(SetupLanguage, "Language");
        SafeExecute(SetupQuality, "Quality");
        SafeExecute(SetupSliders, "Sliders");

        SubscribeToEvents();
        OpenTab(0);

        _isInitializing = false;
    }

    private void SafeExecute(Action action, string name)
    {
        try
        {
            action?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SettingsManager] Warning in {name}: {e.Message}");
        }
    }

    private void AutoAssignAll()
    {
        Transform root = transform.root;

        displayModeSelector = GetOrAddSelector(root, "Row_DisplayMode");
        maxFpsSelector = GetOrAddSelector(root, "Row_MaxFPS");
        resolutionSelector = GetOrAddSelector(root, "Row_Resolution");
        dlssSelector = GetOrAddSelector(root, "Row_DLSS");
        vsyncSelector = GetOrAddSelector(root, "Row_VSync");
        languageSelector = GetOrAddSelector(root, "Row_Language");

        overallQualitySelector = GetOrAddSelector(root, "Row_OverallQuality");
        shadowsSelector = GetOrAddSelector(root, "Row_Shadows");
        texturesSelector = GetOrAddSelector(root, "Row_Textures");
        postProcessingSelector = GetOrAddSelector(root, "Row_PostProcessing");
        invertYSelector = GetOrAddSelector(root, "Row_InvertY");

        FindSliderData(root, "Row_MasterVolume", ref masterVolumeSlider, ref masterVolumeText);
        FindSliderData(root, "Row_MusicVolume", ref musicVolumeSlider, ref musicVolumeText);
        FindSliderData(root, "Row_SFXVolume", ref sfxVolumeSlider, ref sfxVolumeText);
        FindSliderData(root, "Row_Ambience", ref ambienceVolumeSlider, ref ambienceVolumeText);
        FindSliderData(root, "Row_MouseSensitivity", ref mouseSensitivitySlider, ref mouseSensitivityValueText);

        SetupTabButtons(root);
    }

    private UIHorizontalSelector GetOrAddSelector(Transform root, string rowName)
    {
        Transform row = FindDeepChild(root, rowName);
        if (row == null) return null;

        UIHorizontalSelector selector = row.GetComponentInChildren<UIHorizontalSelector>(true);
        if (selector == null)
        {
            Transform box = FindDeepChild(row, "SelectorBox") ?? row;
            selector = box.gameObject.GetComponent<UIHorizontalSelector>();
            if (selector == null)
            {
                selector = box.gameObject.AddComponent<UIHorizontalSelector>();
            }
        }
        selector.AutoAssignReferences();
        selector.InitButtons();
        return selector;
    }

    private void FindSliderData(Transform root, string rowName, ref Slider slider, ref TextMeshProUGUI label)
    {
        Transform row = FindDeepChild(root, rowName);
        if (row != null)
        {
            if (slider == null) slider = row.GetComponentInChildren<Slider>(true);
            if (label == null)
            {
                TextMeshProUGUI[] texts = row.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in texts)
                {
                    if (!t.name.Equals("Text_Label", StringComparison.OrdinalIgnoreCase))
                    {
                        label = t;
                        break;
                    }
                }
            }
        }
    }

    private void SetupTabButtons(Transform root)
    {
        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            string n = btn.name;
            if (n.Equals("Tab_GAMEPLAY", StringComparison.OrdinalIgnoreCase) || n.Equals("GAMEPLAY", StringComparison.OrdinalIgnoreCase))
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OpenTab(0));
            }
            else if (n.Equals("Tab_GRAPHICS", StringComparison.OrdinalIgnoreCase) || n.Equals("GRAPHICS", StringComparison.OrdinalIgnoreCase))
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OpenTab(1));
            }
            else if (n.Equals("Tab_AUDIO", StringComparison.OrdinalIgnoreCase) || n.Equals("AUDIO", StringComparison.OrdinalIgnoreCase))
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OpenTab(2));
            }
            else if (n.Equals("Tab_CONTROLS", StringComparison.OrdinalIgnoreCase) || n.Equals("CONTROLS", StringComparison.OrdinalIgnoreCase))
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OpenTab(3));
            }
        }
    }

    public void OpenTab(int tabIndex)
    {
        SetRowsState(_gameplayRows, tabIndex == 0);
        SetRowsState(_graphicsRows, tabIndex == 1);
        SetRowsState(_audioRows, tabIndex == 2);
        SetRowsState(_controlsRows, tabIndex == 3);
    }

    private void SetRowsState(string[] rowNames, bool active)
    {
        Transform root = transform.root;
        foreach (string name in rowNames)
        {
            Transform obj = FindDeepChild(root, name);
            if (obj != null)
            {
                obj.gameObject.SetActive(active);
            }
        }
    }

    private Transform FindDeepChild(Transform parent, string name)
    {
        if (parent == null) return null;
        foreach (Transform child in parent)
        {
            if (child.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return child;
            Transform result = FindDeepChild(child, name);
            if (result != null)
                return result;
        }
        return null;
    }

    private void SetupDisplayMode()
    {
        if (displayModeSelector == null) return;
        List<string> options = new List<string> { "Fullscreen", "Borderless", "Windowed" };

        int currentMode = Screen.fullScreenMode switch
        {
            FullScreenMode.ExclusiveFullScreen => 0,
            FullScreenMode.FullScreenWindow => 1,
            FullScreenMode.Windowed => 2,
            _ => 0
        };

        displayModeSelector.SetOptions(options, currentMode);
    }

    private void SetupResolutions()
    {
        if (resolutionSelector == null) return;

        List<string> options = new List<string>();
        _filteredResolutions.Clear();
        int currentResIndex = 0;

        Resolution[] allResolutions = Screen.resolutions;
        if (allResolutions != null && allResolutions.Length > 0)
        {
            for (int i = 0; i < allResolutions.Length; i++)
            {
                Resolution res = allResolutions[i];
                if (!_filteredResolutions.Any(r => r.width == res.width && r.height == res.height))
                {
                    _filteredResolutions.Add(res);
                    options.Add($"{res.width} x {res.height}");

                    if (res.width == Screen.width && res.height == Screen.height)
                    {
                        currentResIndex = _filteredResolutions.Count - 1;
                    }
                }
            }
        }

        if (options.Count == 0)
        {
            options.Add($"{Screen.width} x {Screen.height}");
        }

        resolutionSelector.SetOptions(options, currentResIndex);
    }

    private void SetupDLSS()
    {
        if (dlssSelector == null) return;

        List<string> options = new List<string> { "Off", "Quality", "Balanced", "Performance", "Ultra Performance" };
        int currentIndex = 0;

        if (_hdCameraData != null && _hdCameraData.allowDeepLearningSuperSampling)
        {
            currentIndex = (int)_hdCameraData.deepLearningSuperSamplingQuality;
            currentIndex = Mathf.Clamp(currentIndex, 0, options.Count - 1);
        }

        dlssSelector.SetOptions(options, currentIndex);
    }

    private void SetupVSync()
    {
        if (vsyncSelector == null) return;
        List<string> options = new List<string> { "Off", "On" };
        vsyncSelector.SetOptions(options, QualitySettings.vSyncCount > 0 ? 1 : 0);
    }

    private void SetupMaxFPS()
    {
        if (maxFpsSelector == null) return;
        List<string> options = new List<string> { "30", "60", "120", "144", "240", "Unlimited" };

        int targetFps = Application.targetFrameRate;
        int index = Array.IndexOf(_fpsLimits, targetFps);
        if (index == -1) index = 5;

        maxFpsSelector.SetOptions(options, index);
    }

    private void SetupLanguage()
    {
        if (languageSelector == null) return;
        List<string> options = new List<string> { "English", "Українська" };
        int currentLang = PlayerPrefs.GetInt("LanguageIndex", 0);
        languageSelector.SetOptions(options, currentLang);
    }

    private void SetupQuality()
    {
        List<string> qualityOptions = new List<string> { "Low", "Medium", "High", "Ultra" };
        int currentQuality = QualitySettings.GetQualityLevel();

        if (overallQualitySelector != null) overallQualitySelector.SetOptions(qualityOptions, currentQuality);
        if (shadowsSelector != null) shadowsSelector.SetOptions(qualityOptions, currentQuality);
        if (texturesSelector != null) texturesSelector.SetOptions(qualityOptions, currentQuality);
        if (postProcessingSelector != null) postProcessingSelector.SetOptions(qualityOptions, currentQuality);
    }

    private void SetupSliders()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = 0f;
            masterVolumeSlider.maxValue = 100f;
            masterVolumeSlider.value = AudioListener.volume * 100f;
            UpdateSliderLabel(masterVolumeText, $"{Mathf.RoundToInt(masterVolumeSlider.value)}%");
            masterVolumeSlider.onValueChanged.RemoveAllListeners();
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.minValue = 0f;
            musicVolumeSlider.maxValue = 100f;
            musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 100f);
            UpdateSliderLabel(musicVolumeText, $"{Mathf.RoundToInt(musicVolumeSlider.value)}%");
            musicVolumeSlider.onValueChanged.RemoveAllListeners();
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0f;
            sfxVolumeSlider.maxValue = 100f;
            sfxVolumeSlider.value = PlayerPrefs.GetFloat("SFXVolume", 100f);
            UpdateSliderLabel(sfxVolumeText, $"{Mathf.RoundToInt(sfxVolumeSlider.value)}%");
            sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        }

        if (ambienceVolumeSlider != null)
        {
            ambienceVolumeSlider.minValue = 0f;
            ambienceVolumeSlider.maxValue = 100f;
            ambienceVolumeSlider.value = PlayerPrefs.GetFloat("AmbienceVolume", 100f);
            UpdateSliderLabel(ambienceVolumeText, $"{Mathf.RoundToInt(ambienceVolumeSlider.value)}%");
            ambienceVolumeSlider.onValueChanged.RemoveAllListeners();
            ambienceVolumeSlider.onValueChanged.AddListener(SetAmbienceVolume);
        }

        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.minValue = 0.1f;
            mouseSensitivitySlider.maxValue = 10f;
            float currentSens = PlayerPrefs.GetFloat("MouseSensitivity", 2.0f);
            mouseSensitivitySlider.value = currentSens;
            UpdateSliderLabel(mouseSensitivityValueText, currentSens.ToString("F1"));
            mouseSensitivitySlider.onValueChanged.RemoveAllListeners();
            mouseSensitivitySlider.onValueChanged.AddListener(SetMouseSensitivity);
        }

        if (invertYSelector != null)
        {
            List<string> options = new List<string> { "Off", "On" };
            int inverted = PlayerPrefs.GetInt("InvertY", 0);
            invertYSelector.SetOptions(options, inverted);
        }
    }

    private void SubscribeToEvents()
    {
        UnsubscribeFromEvents();

        if (displayModeSelector != null) displayModeSelector.OnValueChanged += SetDisplayMode;
        if (resolutionSelector != null) resolutionSelector.OnValueChanged += SetResolution;
        if (dlssSelector != null) dlssSelector.OnValueChanged += SetDLSSMode;
        if (vsyncSelector != null) vsyncSelector.OnValueChanged += SetVSync;
        if (maxFpsSelector != null) maxFpsSelector.OnValueChanged += SetMaxFPS;
        if (languageSelector != null) languageSelector.OnValueChanged += SetLanguage;
        if (overallQualitySelector != null) overallQualitySelector.OnValueChanged += SetOverallQuality;
        if (invertYSelector != null) invertYSelector.OnValueChanged += SetInvertY;
    }

    private void UnsubscribeFromEvents()
    {
        if (displayModeSelector != null) displayModeSelector.OnValueChanged -= SetDisplayMode;
        if (resolutionSelector != null) resolutionSelector.OnValueChanged -= SetResolution;
        if (dlssSelector != null) dlssSelector.OnValueChanged -= SetDLSSMode;
        if (vsyncSelector != null) vsyncSelector.OnValueChanged -= SetVSync;
        if (maxFpsSelector != null) maxFpsSelector.OnValueChanged -= SetMaxFPS;
        if (languageSelector != null) languageSelector.OnValueChanged -= SetLanguage;
        if (overallQualitySelector != null) overallQualitySelector.OnValueChanged -= SetOverallQuality;
        if (invertYSelector != null) invertYSelector.OnValueChanged -= SetInvertY;
    }

    private void OnDestroy() => UnsubscribeFromEvents();

    public void SetDisplayMode(int index)
    {
        if (_isInitializing) return;
        switch (index)
        {
            case 0: Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen; break;
            case 1: Screen.fullScreenMode = FullScreenMode.FullScreenWindow; break;
            case 2: Screen.fullScreenMode = FullScreenMode.Windowed; break;
        }
    }

    public void SetResolution(int index)
    {
        if (_isInitializing) return;
        if (index < 0 || index >= _filteredResolutions.Count) return;
        Resolution res = _filteredResolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreenMode);
    }

    public void SetDLSSMode(int index)
    {
        if (_isInitializing) return;
        if (_hdCameraData == null) return;

        if (index == 0)
        {
            _hdCameraData.allowDeepLearningSuperSampling = false;
        }
        else
        {
            _hdCameraData.allowDeepLearningSuperSampling = true;
            uint quality = index switch
            {
                1 => 1,
                2 => 2,
                3 => 0,
                4 => 3,
                _ => 1
            };
            _hdCameraData.deepLearningSuperSamplingQuality = quality;
        }
    }

    public void SetVSync(int index)
    {
        if (_isInitializing) return;
        QualitySettings.vSyncCount = index;
    }

    public void SetMaxFPS(int index)
    {
        if (_isInitializing) return;
        if (index >= 0 && index < _fpsLimits.Length)
        {
            Application.targetFrameRate = _fpsLimits[index];
        }
    }

    public void SetLanguage(int index)
    {
        if (_isInitializing) return;
        PlayerPrefs.SetInt("LanguageIndex", index);
        _localizationManager?.SetLanguage(index);
    }

    public void SetOverallQuality(int index)
    {
        if (_isInitializing) return;
        QualitySettings.SetQualityLevel(index, true);
    }

    public void SetMasterVolume(float value)
    {
        if (_isInitializing) return;
        AudioListener.volume = value / 100f;
        UpdateSliderLabel(masterVolumeText, $"{Mathf.RoundToInt(value)}%");
    }

    public void SetMusicVolume(float value)
    {
        if (_isInitializing) return;
        PlayerPrefs.SetFloat("MusicVolume", value);
        UpdateSliderLabel(musicVolumeText, $"{Mathf.RoundToInt(value)}%");
    }

    public void SetSFXVolume(float value)
    {
        if (_isInitializing) return;
        PlayerPrefs.SetFloat("SFXVolume", value);
        UpdateSliderLabel(sfxVolumeText, $"{Mathf.RoundToInt(value)}%");
    }

    public void SetAmbienceVolume(float value)
    {
        if (_isInitializing) return;
        PlayerPrefs.SetFloat("AmbienceVolume", value);
        UpdateSliderLabel(ambienceVolumeText, $"{Mathf.RoundToInt(value)}%");
    }

    public void SetMouseSensitivity(float value)
    {
        if (_isInitializing) return;
        PlayerPrefs.SetFloat("MouseSensitivity", value);
        UpdateSliderLabel(mouseSensitivityValueText, value.ToString("F1"));
    }

    public void SetInvertY(int index)
    {
        if (_isInitializing) return;
        PlayerPrefs.SetInt("InvertY", index);
    }

    private void UpdateSliderLabel(TextMeshProUGUI labelText, string text)
    {
        if (labelText != null) labelText.text = text;
    }
}