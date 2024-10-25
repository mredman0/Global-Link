using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public float UnmutedVolume;
    public bool Muted;

    // Start is called before the first frame update
    void Start()
    {
        if(Instance)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        UnmutedVolume = SettingsManager.Instance.GetFloat(VOLUME_SETTING_KEY);
        Muted = SettingsManager.Instance.GetBool(MUTED_SETTING_KEY);

        SetFinalVolume();

        SettingsManager.Instance.FloatSettingChanged += OnFloatSettingChanged;
        SettingsManager.Instance.BoolSettingChanged += OnBoolSettingChanged;
    }

    private void OnDestroy()
    {
        SettingsManager.Instance.FloatSettingChanged -= OnFloatSettingChanged;
        SettingsManager.Instance.BoolSettingChanged -= OnBoolSettingChanged;
    }

    private const string VOLUME_SETTING_KEY = "AudioVolume";
    private void OnFloatSettingChanged(string setting, float val)
    {
        if(setting == VOLUME_SETTING_KEY)
        {
            UnmutedVolume = val;
            SetFinalVolume();
        }
    }

    private const string MUTED_SETTING_KEY = "AudioMute";
    private void OnBoolSettingChanged(string setting, bool val)
    {
        if(setting == MUTED_SETTING_KEY)
        {
            Muted = val;
            SetFinalVolume();
        }
    }

    private void SetFinalVolume() => AudioListener.volume = UnmutedVolume * (Muted ? 0 : 1);
}
