using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    // Try to avoid renaming or reordering enum values (values and names used in the editor)
    public enum Panel {
        ingame,
        gameMenu,
        mainMenu,
        collectionList,
        settings,
        resetConfirmation,
        valueCount
    }

    public static UIManager i;

    public GameObject[] panels;
    [NonSerialized] public Panel currentPanel;

    public ScrollRect levelListView;
    public RectTransform levelList;
    public GameObject levelPrefab;
    public TextMeshProUGUI levelCountText;

    public TextMeshProUGUI collectionsPathText;
    public GameObject collectionPrefab;
    public GameObject noCollectionPrefab;
    public RectTransform collectionList;

    public GameObject errorMessageParent;
    public TextMeshProUGUI errorMessageText;

    public TextMeshProUGUI levelTitle;
    public float levelTitleDuration;
    public float levelTitleTransitionDuration;
    public float levelTitleScreenMargin;

    private void Awake()
    {
        i = this;
    }

    private void Start()
    {
        if (!PlayerPrefs.HasKey(GameManager.MAIN_COLLECTION)) // Do not show menu on first time
        {
            currentPanel = Panel.ingame;
            Continue();
        }
        else
        {
            currentPanel = Panel.mainMenu;
        }

        for (int i = 0; i < (int)Panel.valueCount; i++)
        {
            if (i == (int)currentPanel)
                panels[i].SetActive(true);
            else
                panels[i].SetActive(false);
        }

        levelTitle.transform.position = new Vector3(0, -GameManager.i.mainCamera.orthographicSize - levelTitleScreenMargin, 0);
    }

    public void SelectPanel(string panelName)
    {
        Panel panel = (Panel)Enum.Parse(typeof(Panel), panelName, true);
        SelectPanel(panel);
    }

    public void SelectPanel(Panel panel)
    {
        GameManager.i.MakeTransition(() => {
            SelectPanelImmediately(panel);
        });
    }

    public void SelectPanelImmediately(Panel panel)
    {
        panels[(int)currentPanel].SetActive(false);
        currentPanel = panel;
        panels[(int)currentPanel].SetActive(true);

        // Init panels
        if (currentPanel == Panel.gameMenu)
        {
            GameManager.i.RemoveCurrentLevel();
            RefreshLevelList();
            LayoutRebuilder.ForceRebuildLayoutImmediate(levelList);
            levelCountText.text = $"{GameManager.i.currentLevelId + 1} / {GameManager.i.currentCollection.levels.Count}";
            levelListView.verticalNormalizedPosition = 1 - ((float)GameManager.i.currentLevelId / GameManager.i.currentCollection.levels.Count);
            levelListView.velocity = Vector2.zero;
        }
        else if (currentPanel == Panel.collectionList)
        {
            collectionsPathText.text = Application.persistentDataPath;

            Util.DestroyChildren(collectionList.gameObject);
            string[] fileNames = Directory.GetFiles(Application.persistentDataPath);

            if (fileNames.Length == 0)
            {
                Instantiate(noCollectionPrefab, collectionList);
            }

            foreach (string fullPath in fileNames)
            {
                string fileName = fullPath.Split('\\', '/')[^1];
                string fileNameWithoutExtension = fileName.Split('.')[0];

                GameObject collUI = Instantiate(collectionPrefab, collectionList);
                TextMeshProUGUI text = collUI.GetComponentInChildren<TextMeshProUGUI>();
                text.text = fileName;
                Button btn = collUI.GetComponentInChildren<Button>();
                btn.onClick.AddListener(() => LoadCollectionFromFile(fileNameWithoutExtension));
            }
        }
    }

    public void RefreshLevelList()
    {
        Util.DestroyChildren(levelList.gameObject);

        for (int i = 0; i < GameManager.i.currentCollection.levels.Count; i++)
        {
            LevelUI ui = Instantiate(levelPrefab, levelList).GetComponent<LevelUI>();
            ui.Init(GameManager.i.currentCollection.levels[i], i);
        }
    }

    public void MakeLevelUI(LevelData level, RectTransform parent, float margin)
    {
        Util.DestroyChildren(parent.gameObject);

        Vector2 safeZoneSize = new Vector2(
            parent.rect.width - margin * 2,
            parent.rect.height - margin * 2
        );

        float tileSize = Mathf.Min(safeZoneSize.x / level.size.x, safeZoneSize.y / level.size.y);

        Vector2 levelCorner = new Vector2(
            -tileSize * level.size.x / 2,
            -tileSize * level.size.y / 2
        );

        for (int x = 0; x < level.size.x; x++)
        {
            for (int y = 0; y < level.size.y; y++)
            {
                int spriteIndex = GameManager.i.GetSpriteIndexOfTile(level, x, y);
                MakeTile(x, y, Color.white, spriteIndex);
            }
        }

        foreach (LevelObjectData data in level.objects)
        {
            if (data.type == ObjectType.blob)
            {
                MakeTile(data.position.x, data.position.y, GameManager.i.colors[(int)data.color], 0);
            }
        }

        void MakeTile(int x, int y, Color color, int spriteIndex)
        {
            if (spriteIndex == 10) return;

            Image tile = new GameObject().AddComponent<Image>();
            tile.transform.SetParent(parent, false);
            RectTransform rt = tile.gameObject.GetComponent<RectTransform>();
            rt.sizeDelta = Vector3.one * (tileSize + 0.005f);
            rt.anchoredPosition = new Vector2(levelCorner.x + (x + 0.5f) * tileSize, levelCorner.y + (y + 0.5f) * tileSize);
            rt.localScale = Vector3.one;

            tile.color = color;
            tile.sprite = GameManager.i.tileSprites[spriteIndex];
        }
    }

    public void GoToMainLevelList()
    {
        GameManager.i.LoadCollection(LevelLoader.ReadMainCollection());
        SelectPanel(Panel.gameMenu);
    }

    public void Continue()
    {
        if (PlayerPrefs.HasKey(GameManager.MAIN_COLLECTION))
        {
            if (PlayerPrefs.GetInt(GameManager.MAIN_COLLECTION) > 0)
                GameManager.i.LoadCollection(LevelLoader.ReadMainCollection());
            else
                GameManager.i.LoadCollection(LevelLoader.ReadCollectionFromFile(PlayerPrefs.GetString(GameManager.COLLECTION_NAME)));
        }
        else
        {
            GameManager.i.LoadCollection(LevelLoader.ReadMainCollection());
        }

        GameManager.i.MakeTransition(() => {
            SelectPanelImmediately(Panel.ingame);
            GameManager.i.MakeLevel(GameManager.i.currentLevelId);
        });
    }

    public void LoadCollectionFromFile(string fileName)
    {
        GameManager.i.LoadCollection(LevelLoader.ReadCollectionFromFile(fileName));
        SelectPanel(Panel.gameMenu);
    }

    public void ShowErrorMessage(string content)
    {
        errorMessageParent.SetActive(true);
        errorMessageText.text = content;
    }

    public void HideErrorMassage()
    {
        errorMessageParent.SetActive(false);
    }

    public void ResetAll()
    {
        PlayerPrefs.DeleteAll();
        Application.Quit();
    }

    public void ShowLevelTitle()
    {
        StartCoroutine(ShowLevelTitleCoroutine());
    }

    private IEnumerator ShowLevelTitleCoroutine()
    {
        float underScreen = -GameManager.i.mainCamera.orthographicSize - levelTitleScreenMargin;
        float overScreen = -GameManager.i.mainCamera.orthographicSize + levelTitleScreenMargin;

        levelTitle.transform.position = new Vector3(0, underScreen, 0);

        yield return new WaitForSeconds(GameManager.i.transitionDuration);

        levelTitle.text = Util.FirstLetterUppercase(GameManager.i.currentLevel.title);
        LeanTween.moveY(levelTitle.gameObject, overScreen, levelTitleTransitionDuration)
            .setEaseOutExpo();

        yield return new WaitForSeconds(levelTitleTransitionDuration + levelTitleDuration);

        LeanTween.moveY(levelTitle.gameObject, underScreen, levelTitleTransitionDuration)
            .setEaseInExpo();
    } 
}
