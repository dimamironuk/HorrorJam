using Zenject;
using UnityEngine;

public class GameInstaller : MonoInstaller
{
    [SerializeField] private TaskManager taskManager;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private ClipboardController.Settings clipboardSettings;
    [SerializeField] private LocalizationManager localizationManager;
    [SerializeField] private SettingsManager settingsManager;
    public override void InstallBindings()
    {
        Container.Bind<LocalizationManager>().FromInstance(localizationManager).AsSingle();
        Container.Bind<SettingsManager>().FromInstance(settingsManager).AsSingle();
        Container.Bind<TaskManager>().FromInstance(taskManager).AsSingle();
        Container.Bind<Camera>().FromInstance(mainCamera).AsSingle();
        Container.Bind<ClipboardController.Settings>().FromInstance(clipboardSettings).AsSingle();
    }
}