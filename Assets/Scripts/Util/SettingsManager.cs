using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    public event Action<string, bool> BoolSettingChanged;
    public event Action<string, int> IntSettingChanged;
    public event Action<string, float> FloatSettingChanged;
    public event Action<string, string> StringSettingChanged;

    [Header("Bool Settings")]
    public List<string> BoolSettings;
    public List<bool> BoolDefaultValues;
    [Header("Int Settings")]
    public List<string> IntSettings;
    public List<int> IntDefaultValues;
    [Header("Float Settings")]
    public List<string> FloatSettings;
    public List<float> FloatDefaultValues;
    [Header("String Settings")]
    public List<string> StringSettings;
    public List<string> StringDefaultValues;

    public readonly Dictionary<string, bool> BoolValues = new Dictionary<string, bool>();
    public readonly Dictionary<string, int> IntValues = new Dictionary<string, int>();
    public readonly Dictionary<string, float> FloatValues = new Dictionary<string, float>();
    public readonly Dictionary<string, string> StringValues = new Dictionary<string, string>();


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
        Initialize();
    }

    private void Initialize()
    {
        if (BoolSettings.Count != BoolDefaultValues.Count)
        {
            Debug.LogWarning($"Detected mismatch between length of BoolSettings and BoolDefaultValues");
        }
        if (IntSettings.Count != IntDefaultValues.Count)
        {
            Debug.LogWarning($"Detected mismatch between length of IntSettings and IntDefaultValues");
        }
        if (FloatSettings.Count != FloatDefaultValues.Count)
        {
            Debug.LogWarning($"Detected mismatch between length of FloatSettings and FloatDefaultValues");
        }
        if (StringSettings.Count != StringDefaultValues.Count)
        {
            Debug.LogWarning($"Detected mismatch between length of StringSettings and StringDefaultValues");
        }
        for (int i = 0; i < BoolSettings.Count; i++)
        {
            LoadBoolSettingFromSystem(BoolSettings[i]);
        }
        for (int i = 0; i < IntSettings.Count; i++)
        {
            LoadIntSettingFromSystem(IntSettings[i]);
        }
        for (int i = 0; i < FloatSettings.Count; i++)
        {
            LoadFloatSettingFromSystem(FloatSettings[i]);
        }
        for (int i = 0; i < StringSettings.Count; i++)
        {
            LoadStringSettingFromSystem(StringSettings[i]);
        }
    }

    #region Get
    public bool GetBool(string setting)
    {
        if (!BoolSettings.Contains(setting))
        {
            Debug.LogWarning($"Bool Setting \"{setting}\" not managed");
            return false;
        }
        if (!BoolValues.ContainsKey(setting))
        {
            var i = BoolSettings.IndexOf(setting);
            return BoolDefaultValues[i];
        }
        return BoolValues[setting];
    }
    public int GetInt(string setting)
    {
        if (!IntSettings.Contains(setting))
        {
            Debug.LogWarning($"Int Setting \"{setting}\" not managed");
            return 0;
        }
        if (!IntValues.ContainsKey(setting))
        {
            var i = IntSettings.IndexOf(setting);
            return IntDefaultValues[i];
        }
        return IntValues[setting];
    }
    public float GetFloat(string setting)
    {
        if (!FloatSettings.Contains(setting))
        {
            Debug.LogWarning($"Float Setting \"{setting}\" not managed");
            return 0;
        }
        if (!FloatValues.ContainsKey(setting))
        {
            var i = FloatSettings.IndexOf(setting);
            return FloatDefaultValues[i];
        }
        return FloatValues[setting];
    }
    public string GetString(string setting)
    {
        if (!StringSettings.Contains(setting))
        {
            Debug.LogWarning($"String Setting \"{setting}\" not managed");
            return null;
        }
        if (!StringValues.ContainsKey(setting))
        {
            var i = StringSettings.IndexOf(setting);
            return StringDefaultValues[i];
        }
        return StringValues[setting];
    }
    #endregion

    #region Set
    public void SetBool(string setting, bool val, bool doNotSave = false)
    {
        if (!BoolSettings.Contains(setting))
        {
            Debug.LogWarning($"Bool Setting \"{setting}\" not managed");
            return;
        }
        bool? currentValue = BoolValues.ContainsKey(setting) ? BoolValues[setting] : (bool?)null;

        BoolValues[setting] = val;
        if (!currentValue.HasValue || val != currentValue.Value)
        {
            BoolSettingChanged?.Invoke(setting, BoolValues[setting]);
        }
        if(!doNotSave)
        {
            SaveBoolSettingToSystem(setting);
        }
    }
    public void SetInt(string setting, int val, bool doNotSave = false)
    {
        if (!IntSettings.Contains(setting))
        {
            Debug.LogWarning($"Int Setting \"{setting}\" not managed");
            return;
        }
        int? currentValue = IntValues.ContainsKey(setting) ? IntValues[setting] : (int?)null;

        IntValues[setting] = val;
        if (!currentValue.HasValue || val != currentValue.Value)
        {
            IntSettingChanged?.Invoke(setting, IntValues[setting]);
        }
        if (!doNotSave)
        {
            SaveIntSettingToSystem(setting);
        }
    }
    public void SetFloat(string setting, float val, bool doNotSave = false)
    {
        if (!FloatSettings.Contains(setting))
        {
            Debug.LogWarning($"Float Setting \"{setting}\" not managed");
            return;
        }
        float? currentValue = FloatValues.ContainsKey(setting) ? FloatValues[setting] : (float?)null;

        FloatValues[setting] = val;
        if (!currentValue.HasValue || val != currentValue.Value)
        {
            FloatSettingChanged?.Invoke(setting, FloatValues[setting]);
        }
        if (!doNotSave)
        {
            SaveFloatSettingToSystem(setting);
        }
    }
    public void SetString(string setting, string val, bool doNotSave = false)
    {
        if (!StringSettings.Contains(setting))
        {
            Debug.LogWarning($"String Setting \"{setting}\" not managed");
            return;
        }
        string currentValue = StringValues.ContainsKey(setting) ? StringValues[setting] : null;

        StringValues[setting] = val;
        if (currentValue is null || val != currentValue)
        {
            StringSettingChanged?.Invoke(setting, StringValues[setting]);
        }
        if (!doNotSave)
        {
            SaveStringSettingToSystem(setting);
        }
    }
    #endregion

    #region Load
    private void LoadBoolSettingFromSystem(string setting)
    {
        var i = BoolSettings.IndexOf(setting);
        if (i < 0)
        {
            Debug.LogWarning($"Bool Setting \"{setting}\" not managed");
            return;
        }

        bool fromSystem = PlayerPrefs.GetInt(setting, BoolDefaultValues[i] ? 1 : 0) != 0;
        SetBool(setting, fromSystem, doNotSave: true);
    }
    private void LoadIntSettingFromSystem(string setting)
    {
        var i = IntSettings.IndexOf(setting);
        if(i < 0)
        {
            Debug.LogWarning($"Int Setting \"{setting}\" not managed");
            return;
        }

        int fromSystem = PlayerPrefs.GetInt(setting, IntDefaultValues[i]);
        SetInt(setting, fromSystem, doNotSave: true);
    }
    private void LoadFloatSettingFromSystem(string setting)
    {
        var i = FloatSettings.IndexOf(setting);
        if (i < 0)
        {
            Debug.LogWarning($"Float Setting \"{setting}\" not managed");
            return;
        }

        float fromSystem = PlayerPrefs.GetFloat(setting, FloatDefaultValues[i]);
        SetFloat(setting, fromSystem, doNotSave: true);
    }
    private void LoadStringSettingFromSystem(string setting)
    {
        var i = StringSettings.IndexOf(setting);
        if (i < 0)
        {
            Debug.LogWarning($"String Setting \"{setting}\" not managed");
            return;
        }

        string fromSystem = PlayerPrefs.GetString(setting, StringDefaultValues[i]);
        SetString(setting, fromSystem, doNotSave: true);
    }
    #endregion

    #region Save
    private void SaveBoolSettingToSystem(string setting)
    {
        if (!BoolSettings.Contains(setting))
        {
            Debug.LogWarning($"Bool Setting \"{setting}\" not managed");
            return;
        }
        if (!BoolValues.ContainsKey(setting))
        {
            // If we don't have a value, don't write anything, it's basically like preserving the default
            return;
        }
        PlayerPrefs.SetInt(setting, BoolValues[setting] ? 1 : 0);
    }
    private void SaveIntSettingToSystem(string setting)
    {
        if(!IntSettings.Contains(setting))
        {
            Debug.LogWarning($"Int Setting \"{setting}\" not managed");
            return;
        }
        if(!IntValues.ContainsKey(setting))
        {
            // If we don't have a value, don't write anything, it's basically like preserving the default
            return;
        }
        PlayerPrefs.SetInt(setting, IntValues[setting]);
    }
    private void SaveFloatSettingToSystem(string setting)
    {
        if (!FloatSettings.Contains(setting))
        {
            Debug.LogWarning($"Float Setting \"{setting}\" not managed");
            return;
        }
        if (!FloatValues.ContainsKey(setting))
        {
            // If we don't have a value, don't write anything, it's basically like preserving the default
            return;
        }
        PlayerPrefs.SetFloat(setting, FloatValues[setting]);
    }
    private void SaveStringSettingToSystem(string setting)
    {
        if (!StringSettings.Contains(setting))
        {
            Debug.LogWarning($"String Setting \"{setting}\" not managed");
            return;
        }
        if (!StringValues.ContainsKey(setting))
        {
            // If we don't have a value, don't write anything, it's basically like preserving the default
            return;
        }
        PlayerPrefs.SetString(setting, StringValues[setting]);
    }
    #endregion

}
