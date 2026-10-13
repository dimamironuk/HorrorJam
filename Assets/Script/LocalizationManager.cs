using UnityEngine;

public class LocalizationManager : MonoBehaviour
{
    public int CurrentLanguageIndex { get; private set; }

    public void SetLanguage(int index)
    {
        CurrentLanguageIndex = index;
        PlayerPrefs.SetInt("LanguageIndex", index);
        PlayerPrefs.Save();
    }
}