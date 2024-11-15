using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LanguageManager : MonoBehaviour
{
    public static LanguageManager Instance;

    private const string LOCALE_SETTING_KEY = "AccessibilityLocale";
    void Start()
    {
        if(Instance)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetLocale(SettingsManager.Instance.GetString(LOCALE_SETTING_KEY));
        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
    }

    private void OnDestroy()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
    }

    private void OnSelectedLocaleChanged(Locale locale)
    {
        SaveLocale();
    }

    public Locale CurrentLocale() => LocalizationSettings.SelectedLocale;

    public void SetLocale(string locale) => SetLocale(LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(locale)));

    public void SetLocale(Locale locale)
    {
        LocalizationSettings.SelectedLocale = locale;
    }

    private void SaveLocale()
    {
        SettingsManager.Instance.SetString(LOCALE_SETTING_KEY, LocalizationSettings.SelectedLocale.Identifier.Code);
    }
}
