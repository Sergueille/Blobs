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
    [SerializeField] private RectTransform parent;
    [SerializeField] private float timeBetweenAppear;
    [SerializeField] private float appearDuration;

    private bool appeared = false;
    private bool locked;
    private bool startedOnScreen;
    private bool onScreenInit = false;

    private static float lastAppearTime; 


    public void Init(LevelData data, int levelId)
    {
        this.data = data;
        this.levelId = levelId;

        locked = levelId > GameManager.i.levelsCompletedInCollection + 1;

        string rawTitle = GameManager.i.currentCollection.isMainCollection ? LocalizationManager.GetLevelName(data.title) : data.title;
        string titleText = Util.FirstLetterUppercase(rawTitle);
        title.text = titleText;

        lockImage.gameObject.SetActive(locked);

        parent.localScale = Vector3.zero;
    }

    private void Update()
    {
        if (!onScreenInit)
        {
            onScreenInit = true;
            startedOnScreen = IsOnScreen();

            if (!startedOnScreen)
              parent.localScale = Vector3.one;
        }

        bool onScreen = IsOnScreen();
        bool canAppear = !startedOnScreen || Time.time - lastAppearTime > timeBetweenAppear;
        
        if (!appeared && onScreen && canAppear)
        {
            lastAppearTime = Time.time;
            appeared = true;

            if (startedOnScreen)
                LeanTween.scale(parent, Vector3.one, appearDuration).setEaseOutExpo();

            if (!locked)
                UIManager.i.MakeLevelUI(data, image, 30); // Do this only on screen to reduce lag   
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

    private bool IsOnScreen()
    {
        return image.position.y > -GameManager.i.mainCamera.orthographicSize - 2 && image.position.y < GameManager.i.mainCamera.orthographicSize + 2;
    }
}
