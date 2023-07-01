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

    private bool doneUI = false;

    public void Init(LevelData data, int levelId)
    {
        this.data = data;
        this.levelId = levelId;

        string titleText = Util.FirstLetterUppercase(data.title);
        title.text = titleText;
    }

    private void Update()
    {
        if (!doneUI && image.position.y > -GameManager.i.mainCamera.orthographicSize - 2 && image.position.y < GameManager.i.mainCamera.orthographicSize + 2)
        {
            UIManager.i.MakeLevelUI(data, image, 30); // Do this only on screen to reduce lag
            doneUI = true;
        }
    }

    public void OnClick()
    {
        GameManager.i.MakeTransition(() => {
            UIManager.i.SelectPanelImmediately(UIManager.Panel.ingame);
            GameManager.i.MakeLevel(levelId);
        });
    }
}
