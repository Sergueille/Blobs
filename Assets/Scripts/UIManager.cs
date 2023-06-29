using System;
using System.Collections.Generic;
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
        valueCount
    }

    public static UIManager i;

    public GameObject[] panels;
    public Panel currentPanel;

    public ScrollRect levelListView;
    public RectTransform levelList;
    public GameObject levelPrefab;
    public TextMeshProUGUI levelCountText;

    private void Awake()
    {
        i = this;
    }

    private void Start()
    {
        for (int i = 0; i < (int)Panel.valueCount; i++)
        {
            if (i == (int)currentPanel)
                panels[i].SetActive(true);
            else
                panels[i].SetActive(false);
        }
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
            levelCountText.text = $"{GameManager.i.currentLevelId + 1} / {GameManager.i.currentCollection.Count}";
            levelListView.verticalNormalizedPosition = 1 - ((float)GameManager.i.currentLevelId / GameManager.i.currentCollection.Count);
            levelListView.velocity = Vector2.zero;
        }
    }

    public void RefreshLevelList()
    {
        Util.DestroyChildren(levelList.gameObject);

        for (int i = 0; i < GameManager.i.currentCollection.Count; i++)
        {
            LevelUI ui = Instantiate(levelPrefab, levelList).GetComponent<LevelUI>();
            ui.Init(GameManager.i.currentCollection[i], i);
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
        GameManager.i.currentCollection = LevelLoader.ReadMainCollection();
        SelectPanel(Panel.gameMenu);
    }

    public void Continue()
    {
        if (!PlayerPrefs.HasKey(GameManager.LAST_LEVEL_ID) || PlayerPrefs.GetInt(GameManager.LAST_LEVEL_ID) < 0)
        {
            GameManager.i.currentLevelId = 0;
            GameManager.i.currentCollection = LevelLoader.ReadMainCollection();
        }
        else
        {
            GameManager.i.currentLevelId = PlayerPrefs.GetInt(GameManager.LAST_LEVEL_ID);
            GameManager.i.currentCollection = LevelLoader.ReadMainCollection();
        }

        GameManager.i.MakeTransition(() => {
            SelectPanelImmediately(Panel.ingame);
            GameManager.i.MakeLevel(GameManager.i.currentLevelId);
        });
    }
}
