using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelUI : MonoBehaviour
{
    public LevelData data;
    public int levelId;

    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private RectTransform image;

    public void Init(LevelData data, int levelId)
    {
        this.data = data;
        this.levelId = levelId;

        string titleText = char.ToUpper(data.title[0]) + data.title.Substring(1, data.title.Length - 1).ToLower();
        title.text = titleText;
        UIManager.i.MakeLevelUI(data, image, 30);
    }

    public void OnClick()
    {
        GameManager.i.MakeTransition(() => {
            UIManager.i.SelectPanelImmediately(UIManager.Panel.ingame);
            GameManager.i.MakeLevel(levelId);
        });
    }
}
