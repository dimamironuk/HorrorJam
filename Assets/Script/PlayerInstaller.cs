using UnityEngine;
using Zenject;

public class PlayerInstaller : MonoInstaller
{
    [SerializeField] private PlayerController.Settings playerSettings;

    public override void InstallBindings()
    {
        Container.BindInstance(playerSettings).AsSingle();
    }
}