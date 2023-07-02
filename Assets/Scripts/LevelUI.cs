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
    [SerializeField] private RectTransform lockImage;

    private bool doneUI = false;
    private bool locked;

    public void Init(LevelData data, int levelId)
    {
        this.data = data;
        this.levelId = levelId;

        locked = levelId > GameManager.i.levelsCompletedInCollection + 1;

        string rawTitle = GameManager.i.currentCollection.isMainCollection ? LocalizationManager.GetLevelName(data.title) : data.title;
        string titleText = Util.FirstLetterUppercase(rawTitle);
        title.text = titleText;

        lockImage.gameObject.SetActive(locked);
    }

    private void Update()
    {
        if (!locked)
            if (!doneUI && image.position.y > -GameManager.i.mainCamera.orthographicSize - 2 && image.position.y < GameManager.i.mainCamera.orthographicSize + 2)
            {
                UIManager.i.MakeLevelUI(data, image, 30); // Do this only on screen to reduce lag
                doneUI = true;
            }
    }

    public void OnClick()
    {
        if (!locked)
            GameManager.i.MakeTransition(() => {
                UIManager.i.SelectPanelImmediately(UIManager.Panel.ingame);
                GameManager.i.MakeLevel(levelId);
            });
    }
}
