using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class LocalizationManager : MonoBehaviour
{
    public TMP_Dropdown dropdown;
    // Start is called before the first frame update
    void Start()
    {
        //初始化選項
        Initialize();
        
    }
    void Initialize()
    {
        dropdown.ClearOptions();
        List<string> options = new List<string>();
        foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
        {
            options.Add(locale.Identifier.CultureInfo.NativeName);
        }
        dropdown.AddOptions(options);

        var currentlocal = LocalizationSettings.SelectedLocale;
        int index = LocalizationSettings.AvailableLocales.Locales.IndexOf(currentlocal);
        if (index >= 0)
        {
            dropdown.value = index;
        }
        dropdown.onValueChanged.AddListener(OnDropdownChanged);    
    }

    void OnDropdownChanged(int index)
    {
        var local = LocalizationSettings.AvailableLocales.Locales[index];
        LocalizationSettings.SelectedLocale = local;   
    }
}
