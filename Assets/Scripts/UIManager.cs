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
        stats,
        end,
        update,
        speedrunStart,
        speedrunEnd,
        valueCount,
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

    public TextMeshProUGUI versionText;

    public TextMeshProUGUI statsText;
    public TextMeshProUGUI endStatsText;

    public TextMeshProUGUI continueBtnLabel;

    private Coroutine levelTitleCoroutine;

    private RectTransform canvasTransform;

    private bool shouldUpdateLevelListPositionNextFrame = false;

    [NonSerialized] public List<AppearAnimator> elementsToAppear = new List<AppearAnimator>();
    [SerializeField] private float timeBetweenAppearButtons = 0.2f;
    private float lastAppearTime = 0;

    public TextMeshProUGUI speedrunEndTimer;

    private void Awake()
    {
        i = this;
    }

    private void Start()
    {
        canvasTransform = gameObject.GetComponent<RectTransform>();

        for (int i = 0; i < (int)Panel.valueCount; i++)
        {
            panels[i].SetActive(false);
        }

        levelTitle.transform.position = new Vector3(0, -GameManager.i.mainCamera.orthographicSize - levelTitleScreenMargin, 0);

        versionText.text = Application.version;

        
        if (!PlayerPrefs.HasKey(GameManager.MAIN_COLLECTION)) // First time?
        {
            continueBtnLabel.text = LocalizationManager.GetValue("start_btn");
        }
        else
        {
            continueBtnLabel.text = LocalizationManager.GetValue("continue_btn");
        }
    }

    private void Update()
    {
        if (shouldUpdateLevelListPositionNextFrame)
        {
            levelListView.verticalNormalizedPosition = 1 - (float)GameManager.i.currentLevelId / GameManager.i.currentCollection.levels.Count;
            shouldUpdateLevelListPositionNextFrame = false;
        }

        // Appear elements animations
        if (elementsToAppear.Count > 0 && Time.time - lastAppearTime > timeBetweenAppearButtons)
        {
            float maxY = -float.MaxValue;
            AppearAnimator maxAnim = null;
            int maxI = -1;

            for (int i = 0; i < elementsToAppear.Count; i++) // Get the highest element (on Y axis)
            {
                float y = elementsToAppear[i].transform.position.y;

                if (y > maxY)
                {
                    maxY = y;
                    maxI = i;
                    maxAnim = elementsToAppear[i];
                }
            }

            elementsToAppear.RemoveAt(maxI);
            maxAnim.AppearAnimation(); // Animate

            lastAppearTime = Time.time; // Set timeout
        }
    }

    public void SelectPanel(string panelName)
    {
        Panel panel = (Panel)Enum.Parse(typeof(Panel), panelName, true);
        SelectPanel(panel);
    }

    public void SelectPanel(Panel panel)
    {
        if (GameManager.i.isTransitionning) return;

        GameManager.i.MakeTransition(() => {
            SelectPanelImmediately(panel);
        });
    }

    public void SelectPanelImmediately(Panel panel)
    {
        panels[(int)currentPanel].SetActive(false);
        currentPanel = panel;
        panels[(int)currentPanel].SetActive(true);

        // Cancel buttons animations
        elementsToAppear.Clear();

        // Init panels
        if (currentPanel == Panel.gameMenu)
        {
            GameManager.i.RemoveCurrentLevel();
            RefreshLevelList();
            LayoutRebuilder.ForceRebuildLayoutImmediate(canvasTransform);

            int levelCount = GameManager.i.currentCollection.levels.Count;

            levelCountText.text = $"{GameManager.i.currentLevelId + 1} / {levelCount}";
            shouldUpdateLevelListPositionNextFrame = true;
            levelListView.velocity = Vector2.zero;
        }
        else if (currentPanel == Panel.collectionList)
        {
            collectionsPathText.text = Application.persistentDataPath;

            Util.DestroyChildren(collectionList.gameObject);
            string[] fileNames = Directory.GetFiles(Application.persistentDataPath);

            // Add additional official levels
            GameObject collUI = Instantiate(collectionPrefab, collectionList);
            TextMeshProUGUI text = collUI.GetComponentInChildren<TextMeshProUGUI>();
            text.text = "Additional levels";
            Button btn = collUI.GetComponentInChildren<Button>();
            btn.onClick.AddListener(() => LoadCollectionFromResources("additional"));

            if (fileNames.Length == 0)
            {
                Instantiate(noCollectionPrefab, collectionList);
            }

            foreach (string fullPath in fileNames)
            {
                string fileName = fullPath.Split('\\', '/')[^1];
                string fileNameWithoutExtension = fileName.Split('.')[0];

                collUI = Instantiate(collectionPrefab, collectionList);
                text = collUI.GetComponentInChildren<TextMeshProUGUI>();
                text.text = fileName;
                btn = collUI.GetComponentInChildren<Button>();
                btn.onClick.AddListener(() => LoadCollectionFromFile(fileNameWithoutExtension));
            }
        }
        else if (currentPanel == Panel.stats || currentPanel == Panel.end)
        {
            GameManager.i.stats.timePlayed += (Time.time - GameManager.i.startTime) / 60; // Force update time
            GameManager.i.startTime = Time.time;

            string txt = "";
            txt += GetLine("stat_time_played", GameManager.i.stats.timePlayed);
            txt += GetLine("stat_moves", GameManager.i.stats.moves);
            txt += GetLine("stat_restarts", GameManager.i.stats.restarts);
            txt += GetLine("stat_ends", GameManager.i.stats.ends);
            txt += GetLine("stat_fusions", GameManager.i.stats.fusions);
            txt += GetLine("stat_eyes", GameManager.i.stats.extractedEyes);
            txt += GetLine("stat_inversions", GameManager.i.stats.inversions);

            statsText.text = endStatsText.text = txt;

            string GetLine(string key, object value)
            {
                if (value is float v)
                    value = Mathf.CeilToInt(v);
                    
                return LocalizationManager.GetValue(key) + ": " + value.ToString() + "\n";
            }
        }
        else if (currentPanel == Panel.speedrunEnd)
        {
            int time = Mathf.FloorToInt(Time.time - GameManager.i.speedrunStartTime);
            speedrunEndTimer.text = $"{time / 60}:{(time % 60).ToString().PadLeft(2, '0')}";
        }

        // Update musics
        AudioManager.i.SetMusic(currentPanel != Panel.ingame && currentPanel != Panel.mainMenu);
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
            {
                if (PlayerPrefs.HasKey(GameManager.WAS_IN_RESOURCE) && PlayerPrefs.GetInt(GameManager.WAS_IN_RESOURCE) > 0)
                {
                    GameManager.i.LoadCollection(LevelLoader.ReadResourcesCollection(PlayerPrefs.GetString(GameManager.COLLECTION_NAME)));
                }
                else
                {
                    GameManager.i.LoadCollection(LevelLoader.ReadCollectionFromFile(PlayerPrefs.GetString(GameManager.COLLECTION_NAME)));
                }
            }
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

    public void LoadCollectionFromResources(string fileName)
    {
        GameManager.i.LoadCollection(LevelLoader.ReadResourcesCollection(fileName));
        SelectPanel(Panel.gameMenu);
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

    public void SetLanguage(int value)
    {
        LocalizationManager.UpdateLanguage((LocalizationManager.Language)value);
    }

    public void ShowLevelTitle()
    {
        levelTitleCoroutine = StartCoroutine(ShowLevelTitleCoroutine());
    }

    public void HideLevelTitleImmediately()
    {
        if (levelTitleCoroutine != null)
        {
            LeanTween.cancel(levelTitle.gameObject);
            StopCoroutine(levelTitleCoroutine);
            levelTitle.transform.position = new Vector3(0, -GameManager.i.mainCamera.orthographicSize - levelTitleScreenMargin, 0);
        }

        levelTitleCoroutine = null;
    }

    private IEnumerator ShowLevelTitleCoroutine()
    {
        float underScreen = -GameManager.i.mainCamera.orthographicSize - levelTitleScreenMargin;
        float overScreen = -GameManager.i.mainCamera.orthographicSize + levelTitleScreenMargin;

        levelTitle.transform.position = new Vector3(0, underScreen, 0);

        yield return new WaitForSeconds(GameManager.i.transitionDuration);

        string rawTitle = GameManager.i.currentLevel.title;
        string localizedTitle = GameManager.i.currentCollection.isMainCollection ? LocalizationManager.GetLevelName(rawTitle) : rawTitle;
        levelTitle.text = Util.FirstLetterUppercase(localizedTitle);
        LeanTween.moveY(levelTitle.gameObject, overScreen, levelTitleTransitionDuration)
            .setEaseOutExpo();

        yield return new WaitForSeconds(levelTitleTransitionDuration + levelTitleDuration);

        LeanTween.moveY(levelTitle.gameObject, underScreen, levelTitleTransitionDuration)
            .setEaseInExpo();
    } 

    public void RedirectToDocs()
    {
        Application.OpenURL("https://sergueille.github.io/Blobs/Docs/levels");
    }

    public void IngameMenuBtn()
    {
        if (GameManager.i.speedrunMode) {
            GameManager.i.EndSpeedrun(false);
        }
        else {
            SelectPanel(Panel.gameMenu);
        }
    }
}
