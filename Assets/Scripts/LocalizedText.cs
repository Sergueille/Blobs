using System.Collections.Generic;
using UnityEngine;
using TMPro;

[ExecuteAlways]
public class LocalizedText : MonoBehaviour
{
    private TextMeshProUGUI text;
    public string key;

    [ExecuteAlways]
    private void Awake()
    {
        text = gameObject.GetComponent<TextMeshProUGUI>();
        UpdateText();
    }

    [ExecuteAlways]
    private void Update()
    {
        UpdateText();
    }

    public void UpdateText()
    {
        text.text = LocalizationManager.GetValue(key);
    }

    [ContextMenu("Reload locals")]
    public void ReloadLocals()
    {
        LocalizationManager.LoadCSV();
        UpdateText();
    }
}
