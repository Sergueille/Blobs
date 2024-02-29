using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ObjectType
{
    blob, end, diamond, inverter
}

public enum GameColor
{
    none    = 0b000,
    red     = 0b001, 
    blue    = 0b010, 
    green   = 0b100,
    magenta = 0b011,
    yellow  = 0b101,
    cyan    = 0b110,
    brown   = 0b111
}

public class GameManager : MonoBehaviour
{
    public const string LEVEL_COMPLETED_ON_COLLECTION = "LevelsCompletedOnCollection";
    public const string LAST_LEVEL_ON_COLLECTION = "LastLevelOnCollection";
    public const string MAIN_COLLECTION = "MainCollection";
    public const string COLLECTION_NAME = "CollectionName";

    public const string GAME_FINISHED = "GameFinished";

    public const string SLIDE_SENSITIVITY = "SlideSensitivity";
    public const string GLOBAL_VOLUME = "GlobalVolume";
    public const string COLORBLIND_MODE = "ColorblindMode";

    public static GameManager i;

    [NonSerialized] public LevelCollection currentCollection;
    [NonSerialized] public LevelData currentLevel;
    [NonSerialized] public int currentLevelId;
    [NonSerialized] public int levelsCompletedInCollection;
    [NonSerialized] public List<LevelObject> levelObjects;

    public Color[] colors;
    public Material[] colorMaterials;
    public Material defaultMaterial;

    [Tooltip("Proportion of the screen that will be outside the safe zone")]
    public float screenMargin = 0.1f;

    [NonSerialized] public Vector2 levelCorner;
    [NonSerialized] public float tileSize;

    public Camera mainCamera;

    public Sprite[] tileSprites;
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private GameObject blobPrefab;
    [SerializeField] private GameObject endPrefab;
    [SerializeField] private GameObject diamondPrefab;
    [SerializeField] private GameObject inverterPrefab;

    [SerializeField] private Material bgStripes;
    [SerializeField] private Material transitionStripes;
    [SerializeField] private float stripesSpeed;
    public float transitionDuration;

    [SerializeField] private float slideSensitivity;
    [SerializeField] private float globalVolume = 1;

    [SerializeField] private AudioClip[] smallBlobSounds;
    [SerializeField] private AudioClip[] bigBlobSounds;
    [SerializeField] private float smallBlobSoundsVolume;
    [SerializeField] private float bigBlobSoundsVolume;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private float clickSoundVolume;
    [SerializeField] private RandomRange clickSoundPitch;
    [SerializeField] private AudioClip rewindSound;

    public GameObject particlesPrefab;

    private List<GameObject> tiles;

    private Vector2Int lastDirection;
    private Vector2 lastMousePosition;
    private Vector2 slideStartPosition;
    private bool lastTimeMouseWasDown = false;

    private bool playSmallBlobSoundOnThisMove = false;
    private bool playBigBlobSoundOnThisMove = false;

    [SerializeField] private Slider slideSensitivitySlider;
    [SerializeField] private Slider globalVolumeSlider;
    [SerializeField] private TMP_Dropdown languageDropdown;
    [SerializeField] private Toggle colorblindToggle;

    private bool levelComplete = false; // Used to prevent submit moves during transition and beak things

    [SerializeField] private float levelCompleteDuration;
    [SerializeField] private int levelCompleteParticleCount;
    [SerializeField] private float levelCompleteParticlesScale = 1.7f;

    [SerializeField] private float endParticlesDuration = 5;
    [SerializeField] private int endParticlesPerSecond = 5;

    [SerializeField] private float forbiddenSlideZoneSize = 0.1f; // Zone on the top and bottom to prevent sliding when the used wants to quit fullscreen (screen units)

    [SerializeField] private SpriteRenderer tutoHand;
    [SerializeField] private float tutoHandMaxY = -2.3f;
    [SerializeField] private float tutoHandMinY = -3.7f;
    [SerializeField] private float tutoHandSlideDuration = 0.5f;
    [SerializeField] private float tutoHandFadeDuration = 0.2f;
    private Coroutine handCoroutine;

    public bool colorblindMode = false;

    public Statistics stats;
    [NonSerialized] public float startTime;

    [SerializeField] private TextMeshProUGUI moveCountText;
    public int moveCount;

    [NonSerialized]
    public bool isTransitionning;

    private void Awake()
    {
        i = this;
        tiles = new List<GameObject>();
    }

    private IEnumerator Start()
    {
        transitionStripes.SetFloat("_Discard", 1);

        tutoHand.gameObject.SetActive(false);

        LocalizationManager.Init();
        UpdateSettings();

        stats = Statistics.Load();
        startTime = Time.time;

        moveCountText.gameObject.SetActive(false);  

        yield return ScreenParticlesCoroutine();

        UIManager.i.SelectPanel("MainMenu");
    }

    private void Update()
    {
        bgStripes.SetFloat("_Shift", Time.time * stripesSpeed + 1);
        transitionStripes.SetFloat("_Shift", Time.time * stripesSpeed + 1);

        if (currentLevel == null) return;

        // Key controls
        Vector2Int direction = Vector2Int.zero;
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.A)) direction = Vector2Int.left;
        if (Input.GetKeyDown(KeyCode.D)) direction = Vector2Int.right;
        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.W)) direction = Vector2Int.up;
        if (Input.GetKeyDown(KeyCode.S)) direction = Vector2Int.down;

        // Slide controls
        if (Input.GetMouseButton(0))
        {
            if (lastTimeMouseWasDown)
            {
                if (slideStartPosition.y < Screen.height * (1 - forbiddenSlideZoneSize)
                 && slideStartPosition.y > Screen.height * forbiddenSlideZoneSize) // Not in forbidden zone
                {
                    Vector2 delta = (Vector2)Input.mousePosition - lastMousePosition;
                    float dist = delta.magnitude / Screen.height;

                    if (dist / Time.deltaTime > slideSensitivity)
                    {
                        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                        {
                            if (delta.x > 0) direction = Vector2Int.right;
                            else             direction = Vector2Int.left;
                        }
                        else
                        {
                            if (delta.y > 0) direction = Vector2Int.up;
                            else             direction = Vector2Int.down;
                        }
                    }
                }
            }
            else
            {
                slideStartPosition = Input.mousePosition;
            }

            if (direction == lastDirection)
                direction = Vector2Int.zero;

            if (direction != Vector2Int.zero)
                lastDirection = direction;

            if (direction == Vector2Int.down)
                HideTutorial();

            lastMousePosition = Input.mousePosition;
            lastTimeMouseWasDown = true;
        }
        else
        {
            lastTimeMouseWasDown = false;
            lastDirection = Vector2Int.zero;
        }

        if (direction != Vector2Int.zero)
        {
            stats.moves++;
            stats.Save();

            moveCount++;

            playSmallBlobSoundOnThisMove = false;
            playBigBlobSoundOnThisMove = false;

            int count = direction.y == 0 ? currentLevel.size.x : currentLevel.size.y;

            for (int i = 0; i < count; i++)
                MoveAllOneTile(direction);

            UpdateObjects();

            if (playBigBlobSoundOnThisMove)
            {
                AudioClip clip = bigBlobSounds[UnityEngine.Random.Range(0, bigBlobSounds.Length)];
                AudioSource.PlayClipAtPoint(clip, mainCamera.transform.position, globalVolume * bigBlobSoundsVolume);
            }
            else if (playSmallBlobSoundOnThisMove)
            {
                AudioClip clip = smallBlobSounds[UnityEngine.Random.Range(0, smallBlobSounds.Length)];
                AudioSource.PlayClipAtPoint(clip, mainCamera.transform.position, globalVolume * smallBlobSoundsVolume);
            }

            foreach (LevelObject obj in levelObjects)
            {
                if (obj is Blob)
                {
                    (obj as Blob).lastFusionPosition = Vector2Int.one * -1; // Reset this variable
                    (obj as Blob).stoppedByDiamond = true; // Set this to true to prevent loosing an eye on movement start
                }
            }

            // Check if level is finished
            bool finished = true;
            foreach (LevelObject obj in levelObjects)
            {
                if (obj != null && obj is End && !obj.isDestroyed)
                {
                    finished = false;
                    break;
                }
            }

            if (finished)
                OnLevelComplete();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartLevel();
        }

        /*
        if (Input.GetKeyDown(KeyCode.RightArrow)) 
        {
            currentLevelId += 1;
            currentLevelId %= currentCollection.levels.Count;
            MakeLevel(currentLevelId);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            currentLevelId += currentCollection.levels.Count - 1;
            currentLevelId %= currentCollection.levels.Count;
            MakeLevel(currentLevelId);
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            currentCollection = LevelLoader.ReadMainCollection();
            currentLevelId = 0;
            MakeLevel(currentLevelId);
        }
        */

        // Show move count
        if (PlayerPrefs.HasKey(GAME_FINISHED) || !currentCollection.isMainCollection)
        {
            moveCountText.gameObject.SetActive(true);

            moveCountText.text = $"<size=100>{moveCount}</size>/{currentLevel.moveCount}";

            Color color = moveCount > currentLevel.moveCount ? Color.red : Color.white;
            moveCountText.color = color;
        }
        else
        {
            moveCountText.gameObject.SetActive(false);
        }
    }

    public void LoadCollection(LevelCollection collection)
    {
        currentCollection = collection;

        string completedKey = LEVEL_COMPLETED_ON_COLLECTION + currentCollection.fileName;
        string lastKey = LAST_LEVEL_ON_COLLECTION + currentCollection.fileName;

        if (PlayerPrefs.HasKey(completedKey))
        {
            currentLevelId = PlayerPrefs.GetInt(lastKey);
            levelsCompletedInCollection = PlayerPrefs.GetInt(completedKey);
        }
        else
        {
            currentLevelId = 0;
            levelsCompletedInCollection = 0;
        }
    }

    public void RestartLevel()
    {
        if (isTransitionning) return;

        AudioSource.PlayClipAtPoint(rewindSound, mainCamera.transform.position, globalVolume);
        MakeLevelWithTransition(currentLevelId);

        stats.restarts++;
        stats.Save();
    }

    private void OnLevelComplete()
    {
        if (levelComplete) return;

        StartCoroutine(OnLevelCompleteCoroutine());
    }

    private IEnumerator OnLevelCompleteCoroutine()
    {
        levelComplete = true;

        levelsCompletedInCollection = Mathf.Max(levelsCompletedInCollection, currentLevelId);
        PlayerPrefs.SetInt(LEVEL_COMPLETED_ON_COLLECTION + currentCollection.fileName, levelsCompletedInCollection);

        yield return new WaitForSeconds(levelCompleteDuration);

        // Show end!
        if (currentCollection.isMainCollection && currentLevelId == currentCollection.levels.Count - 1)
        {
            OnMainCollectionFinished();
        }
        else
        {
            currentLevelId += 1;
            currentLevelId %= currentCollection.levels.Count;
            MakeLevelWithTransition(currentLevelId);
        }
    }

    public void MakeLevelWithTransition(int levelIndex)
    {
        MakeTransition(() => {
            MakeLevel(levelIndex);
        });
    }

    public void MakeTransition(Action callback)
    {
        if (isTransitionning) {
            Debug.Log("Fired transition while already active!");
        }

        isTransitionning = true;
        LeanTween.value(1, 0, transitionDuration / 2).setEaseInOutExpo().setOnUpdate(val => {
            float actualVal = val < 0.01 ? 0 : val;
            transitionStripes.SetFloat("_Discard", actualVal);
        }).setOnComplete(() => {
            transitionStripes.SetFloat("_Discard", 0);

            UIManager.i.HideLevelTitleImmediately();
            HideTutorial();
            callback();

            LeanTween.value(0, 1, transitionDuration / 2).setEaseInOutExpo().setOnUpdate(val => {
                transitionStripes.SetFloat("_Discard", val);
            }).setOnComplete(() => {
                transitionStripes.SetFloat("_Discard", 1);
                isTransitionning = false;
            });
        });
    }

    public void MakeLevel(int levelIndex)
    {
        RemoveCurrentLevel();

        levelComplete = false;

        PlayerPrefs.SetInt(LAST_LEVEL_ON_COLLECTION + currentCollection.fileName, levelIndex);
        PlayerPrefs.SetInt(MAIN_COLLECTION, currentCollection.isMainCollection ? 1 : 0);
        PlayerPrefs.SetString(COLLECTION_NAME, currentCollection.fileName);

        currentLevelId = levelIndex;
        currentLevel = currentCollection.levels[levelIndex];

        moveCount = 0;

        if (currentLevelId == 0 && currentCollection.isMainCollection)
            ShowTutorial();

        UIManager.i.ShowLevelTitle();

        // Get sizes
        Vector2 safeZoneSize = new Vector2(
            mainCamera.orthographicSize * 2 * Screen.width / Screen.height * (1 - screenMargin - screenMargin),
            mainCamera.orthographicSize * 2 * (1 - screenMargin - screenMargin)
        );

        tileSize = Mathf.Min(safeZoneSize.x / currentLevel.size.x, safeZoneSize.y / currentLevel.size.y);

        levelCorner = new Vector2(
            -tileSize * currentLevel.size.x / 2,
            -tileSize * currentLevel.size.y / 2
        );

        for (int x = 0; x < currentLevel.size.x; x++)
        {
            for (int y = 0; y < currentLevel.size.y; y++)
            {
                Vector3 pos = GetScreenPosition(x, y);
                pos.z = 1; // Tiles are behind to make colorblind materials display in front

                SpriteRenderer tile = Instantiate(tilePrefab, pos, Quaternion.identity).GetComponent<SpriteRenderer>();

                int spriteIndex = GetSpriteIndexOfTile(currentLevel, x, y);

                tile.sprite = tileSprites[spriteIndex];
                tile.transform.localScale = Vector3.one * (tileSize + 0.005f);

                tiles.Add(tile.gameObject);
            }
        }

        levelObjects = new List<LevelObject>();
        foreach (LevelObjectData data in currentLevel.objects)
        {
            GameObject prefab = null;

            if (data.type == ObjectType.blob)
            {
                prefab = blobPrefab;
            }
            else if (data.type == ObjectType.end)
            {
                prefab = endPrefab;
            }
            else if (data.type == ObjectType.diamond)
            {
                prefab = diamondPrefab;
            }
            else if (data.type == ObjectType.inverter)
            {
                prefab = inverterPrefab;
            }

            LevelObjectData copiedData = data.Clone(); 

            LevelObject newObject = Instantiate(prefab).GetComponent<LevelObject>();
            newObject.Init(copiedData);
            levelObjects.Add(newObject);
        }
    }

    public int GetSpriteIndexOfTile(LevelData data, int x, int y)
    {
        bool GetTileInData(int x, int y)
        {
            if (x < 0 || x >= data.size.x || y < 0 || y >= data.size.y)
                return false;

            return data.data[x + y * data.size.x];
        }

        bool current = GetTileInData(x, y);
        bool up = GetTileInData(x, y + 1);
        bool down = GetTileInData(x, y - 1);
        bool right = GetTileInData(x + 1, y);
        bool left = GetTileInData(x - 1, y);

        if (current)
        {
            if      (!up && !down && !left && !right) return 0;

            else if (up  && !down && !left && !right) return 1;
            else if (!up && !down && left  && !right) return 2;
            else if (!up && down  && !left && !right) return 3;
            else if (!up && !down && !left && right ) return 4;

            else if (!up && down  && left  && !right) return 5;
            else if (!up && down  && !left && right ) return 6;
            else if (up  && !down && !left && right ) return 7;
            else if (up  && !down && left  && !right) return 8;

            else return 9;
        }
        else
        {
            if      (!up && down  && left  && !right) return 11;
            else if (up  && !down && left  && !right) return 12;
            else if (up  && !down && !left && right ) return 13;
            else if (!up && down  && !left && right ) return 14;

            else if (!up && down  && left  && right ) return 15;
            else if (up  && down  && left  && !right) return 16;
            else if (up  && !down && left  && right ) return 17;
            else if (up  && down  && !left && right ) return 18;

            else if (up  && down  && left  && right ) return 19;

            else return 10;
        }
    }

    public void RemoveCurrentLevel()
    {
        foreach (GameObject go in tiles)
        {
            Destroy(go);
        }

        tiles.Clear();

        if (levelObjects != null)
        {
            foreach (LevelObject obj in levelObjects)
            {
                if (obj != null)
                    Destroy(obj.gameObject);
            }

            levelObjects = null;
        }

        currentLevel = null;
    }

    /// <summary>
    /// Returns the center of the given tile
    /// </summary>
    public Vector2 GetScreenPosition(Vector2Int coord)
    {
        return levelCorner + ((Vector2)coord + Vector2.one / 2) * tileSize;
    }

    public Vector2 GetScreenPosition(int x, int y)
    {
        return GetScreenPosition(new Vector2Int(x, y));
    }

    public bool GetTile(int x, int y)
    {
        if (x < 0 || x >= currentLevel.size.x || y < 0 || y >= currentLevel.size.y)
            return false;

        return currentLevel.data[x + y * currentLevel.size.x];
    }

    public bool GetTile(Vector2Int pos)
    {
        return GetTile(pos.x, pos.y);
    }

    public LevelObject GetObject(int x, int y)
    {
        if (x < 0 || x >= currentLevel.size.x || y < 0 || y >= currentLevel.size.y)
            return null;

        foreach (LevelObject obj in levelObjects)
            if (obj.data.position.x == x && obj.data.position.y == y && !obj.isDestroyed)
                return obj;

        return null;
    }

    public LevelObject GetObject(Vector2Int pos)
    {
        return GetObject(pos.x, pos.y);
    }

    public T GetObject<T>(int x, int y) where T : LevelObject
    {
        if (x < 0 || x >= currentLevel.size.x || y < 0 || y >= currentLevel.size.y)
            return null;

        foreach (LevelObject obj in levelObjects)
            if (obj.data.position.x == x && obj.data.position.y == y && !obj.isDestroyed && obj is T)
                return obj as T;

        return null;
    }
    
    public T GetObject<T>(Vector2Int pos) where T : LevelObject
    {
        return GetObject<T>(pos.x, pos.y);
    }
    

    /// <summary>
    /// Move all objects by one tile
    /// </summary>
    private void MoveAllOneTile(Vector2Int direction)
    {
        void Move(int x, int y)
            => MoveBlobOneTile(GetObject<Blob>(x, y), new Vector2Int(x, y), direction);

        if (direction.x == 0)
        {
            for (int x = 0; x < currentLevel.size.x; x++)
            {
                if (direction.y == -1)
                {
                    for (int y = 0; y < currentLevel.size.y; y++)
                        Move(x, y);
                }
                else
                {
                    for (int y = currentLevel.size.y - 1; y >= 0; y--)
                        Move(x, y);
                }
            }
        }
        else
        {
            for (int y = 0; y < currentLevel.size.y; y++)
            {
                if (direction.x == -1)
                {
                    for (int x = 0; x < currentLevel.size.x; x++)
                        Move(x, y);
                }
                else
                {
                    for (int x = currentLevel.size.x; x >= 0; x--)
                        Move(x, y);
                }
            }
        }
    }

    private void MoveBlobOneTile(Blob currentBlob, Vector2Int objectPos, Vector2Int direction)
    {
        if (currentBlob == null) return; // No object
        if (currentBlob.data.eyes == 0) return; // Can't move without eye

        Vector2Int target = objectPos + direction;

        if (!GetTile(target)) return; // There is a wall

        if (currentBlob.lastFusionPosition != objectPos && currentBlob.data.color != GameColor.none) // Ignore diamond if i'm an eye alone or if i am fusing
        {
            Diamond diamond = GetObject<Diamond>(objectPos);
            if (diamond != null) // I'm on a diamond
            {
                if (!currentBlob.stoppedByDiamond) // If i am not already stopped (to prevent loosing many eyes)
                {
                    currentBlob.AddEyes(-1);
                    currentBlob.MakeParticlesOnApply(currentBlob.data.color);
                    currentBlob.stoppedByDiamond = true;

                    playBigBlobSoundOnThisMove = true;

                    stats.extractedEyes++;
                    stats.Save();

                    Blob newBlob = Instantiate(blobPrefab).GetComponent<Blob>();
                    newBlob.Init(new LevelObjectData{
                        position = objectPos,
                        color = GameColor.none,
                        eyes = 1,
                        type = ObjectType.blob
                    });
                    levelObjects.Add(newBlob);

                    currentBlob = newBlob; // Simulate the new blob instead of the ont that is stuck
                }
                else return; // Stop here
            }
        }

        currentBlob.stoppedByDiamond = false;

        Blob otherBlob = GetObject<Blob>(target);
        if (otherBlob != null) // Fusion with another blob
        {
            currentBlob.MakeParticlesOnApply(currentBlob.data.color);
            currentBlob.MakeParticlesOnApply(otherBlob.data.color);

            currentBlob.AddEyes(otherBlob.data.eyes);
            currentBlob.SetColor(otherBlob.data.color | currentBlob.data.color);

            otherBlob.DestroyObject();
            currentBlob.lastFusionPosition = target;
            currentBlob.Move(target);

            playBigBlobSoundOnThisMove = true;

            stats.fusions++;
            stats.Save();

            TestEnd(currentBlob); // Retest end after fusion
            return;
        }

        End otherEnd = GetObject<End>(target);
        if (otherEnd != null) // End!
        {
            currentBlob.Move(target);
            TestEnd(currentBlob);

            return;
        }

        Inverter otherInverter = GetObject<Inverter>(target);
        if (otherInverter != null && !currentBlob.stoppedByDiamond)
        {
            currentBlob.SetColor((GameColor)((int)~currentBlob.data.color & 0b111));
            currentBlob.MakeParticlesOnApply(currentBlob.data.color);

            stats.inversions++;
            stats.Save();

            playBigBlobSoundOnThisMove = true;
        }

        currentBlob.Move(target);

        // I'm moving, so I will stop, so play small sound
        playSmallBlobSoundOnThisMove = true;
    }

    private void TestEnd(Blob blob)
    {
        End end = GetObject<End>(blob.data.position.x, blob.data.position.y);
        if (end == null) return;

        if (end.data.color == blob.data.color && end.data.eyes == blob.data.eyes)
        {
            blob.MakeParticlesOnApply(end.data.color);
            blob.MakeParticlesOnApply(end.data.color); // Twice for more particles!
            playBigBlobSoundOnThisMove = true;
            end.DestroyObject();
            blob.DestroyObject();

            stats.ends++;
            stats.Save();
        }
    }

    /// <summary>
    /// Send events to the objects
    /// </summary>
    private void UpdateObjects()
    {
        foreach (LevelObject obj in levelObjects)
        {
            if (obj != null)
                obj.ApplyChanges();
        }
    }

    public void CreateParticles(GameColor color, Vector2 position, float scale = 1)
    {
        ColoredParticleSystem ps = Instantiate(particlesPrefab, position, Quaternion.identity).GetComponent<ColoredParticleSystem>();
        ps.transform.position = new Vector3(
            ps.transform.position.x,
            ps.transform.position.y,
            -5
        );
        ps.transform.localScale = Vector3.one * scale;
        ps.Play(color);
    }

    public void UpdateSettings()
    {
        globalVolume = GetSettingFloat(GLOBAL_VOLUME, 1.0f);
        globalVolumeSlider.value = globalVolume;

        slideSensitivity = GetSettingFloat(SLIDE_SENSITIVITY, 0.7f);
        slideSensitivitySlider.value = slideSensitivity;

        colorblindMode = GetSettingBool(COLORBLIND_MODE, false);
        colorblindToggle.isOn = colorblindMode;

        LocalizationManager.UpdateLanguage((LocalizationManager.Language)GetSettingInt(LocalizationManager.LANGUAGE_KEY, (int)LocalizationManager.Language.systemLanguage));
        languageDropdown.value = (int)LocalizationManager.currentLanguage;
    }

    public float GetSettingFloat(string settingName, float defaultValue)
    {
        if (PlayerPrefs.HasKey(settingName))
        {
            return PlayerPrefs.GetFloat(settingName);
        }
        else return defaultValue;
    }

    public int GetSettingInt(string settingName, int defaultValue)
    {
        if (PlayerPrefs.HasKey(settingName))
        {
            return PlayerPrefs.GetInt(settingName);
        }
        else return defaultValue;
    }

    public bool GetSettingBool(string settingName, bool defaultValue)
    {
        if (PlayerPrefs.HasKey(settingName))
        {
            return PlayerPrefs.GetInt(settingName) > 0;
        }
        else return defaultValue;
    }

    public void SetGlobalVolume(float volume)
    {
        PlayerPrefs.SetFloat(GLOBAL_VOLUME, volume);
        UpdateSettings();
    }

    public void SetSlideSensitivity(float val)
    {
        PlayerPrefs.SetFloat(SLIDE_SENSITIVITY, val);
        UpdateSettings();
    }

    public void SetColorblindMode(bool val)
    {
        PlayerPrefs.SetInt(COLORBLIND_MODE, val ? 1 : 0);
        UpdateSettings();
    }

    public void PlayClickSound()
    {
        AudioSource source = new GameObject().AddComponent<AudioSource>();
        source.clip = clickSound;
        source.volume = clickSoundVolume * globalVolume;
        source.maxDistance = 200;
        source.minDistance = 200;

        float pitch = clickSoundPitch.GetRandom();

        source.pitch = pitch;
        source.loop = false;
        source.Play();
        Destroy(source.gameObject, clickSound.length / pitch + 1); // Delay to make sure
    }

    public void ShowTutorial()
    {
        if (handCoroutine != null)
            StopCoroutine(handCoroutine);
        handCoroutine = StartCoroutine(TutorialCoroutine());
    }

    public void HideTutorial()
    {
        if (handCoroutine == null) return;

        StopCoroutine(handCoroutine);
        LeanTween.cancel(tutoHand.gameObject);
        LeanTween.alpha(tutoHand.gameObject, 0, tutoHandFadeDuration);
        tutoHand.gameObject.SetActive(false);
        handCoroutine = null;
    }

    private IEnumerator TutorialCoroutine()
    {        
        tutoHand.gameObject.SetActive(true);

        while (true)
        {
            tutoHand.color = new Color(1, 1, 1, 0);
            tutoHand.transform.position = new Vector3(0, tutoHandMaxY, 0);

            LeanTween.alpha(tutoHand.gameObject, 1, tutoHandFadeDuration);
            yield return new WaitForSeconds(tutoHandFadeDuration);
            LeanTween.moveY(tutoHand.gameObject, tutoHandMinY, tutoHandSlideDuration).setEaseInOutExpo();
            yield return new WaitForSeconds(tutoHandSlideDuration);
            LeanTween.alpha(tutoHand.gameObject, 0, tutoHandFadeDuration);
            yield return new WaitForSeconds(tutoHandFadeDuration);
        }
    }

    public Material GetColorMaterial(GameColor color)
    {
        if (colorblindMode)
        {
            return colorMaterials[(int)color];
        }
        else return defaultMaterial;
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            float timeSpent = Time.time - startTime;
            stats.timePlayed += timeSpent / 60;
            stats.Save();
            startTime = Time.time;
        }
        else
        {
            startTime = Time.time;
        }
    }

    private void OnApplicationQuit()
    {
        float timeSpent = Time.time - startTime;    
        stats.timePlayed += timeSpent / 60;
        stats.Save();
    }   

    private void OnMainCollectionFinished()
    {
        MakeTransition(() => {
            RemoveCurrentLevel();
            UIManager.i.SelectPanelImmediately(UIManager.Panel.end); // Statistics are set by UIManager
            StartCoroutine(ScreenParticlesCoroutine());
            PlayerPrefs.SetInt(GAME_FINISHED, 1);
        });
    }

    private IEnumerator ScreenParticlesCoroutine()
    {
        for (int i = 0; i < endParticlesDuration * endParticlesPerSecond; i++)
        {
            Vector2 randomPos = new Vector2(
                UnityEngine.Random.Range(-mainCamera.orthographicSize, mainCamera.orthographicSize) * Screen.width / Screen.height,
                UnityEngine.Random.Range(-mainCamera.orthographicSize, mainCamera.orthographicSize)
            );

            CreateParticles((GameColor)UnityEngine.Random.Range((int)GameColor.red, (int)GameColor.brown + 1), randomPos, levelCompleteParticlesScale);

            yield return new WaitForSeconds(1.0f / (float)endParticlesPerSecond);
        }
    }
}

public class LevelCollection
{
    public List<LevelData> levels;
    public string name;
    public string fileName;
    public string info;
    public bool isMainCollection;
    public bool isResourcesCollection;
}

public class LevelData
{
    public string title;
    public Vector2Int size;
    public bool[] data;
    public LevelObjectData[] objects;
    public int moveCount;
}

public class LevelObjectData
{
    public ObjectType type;
    public GameColor color;
    public int eyes;
    public Vector2Int position;

    public LevelObjectData Clone()
    {
        return (LevelObjectData)MemberwiseClone();
    }
}

public struct Statistics
{
    public float timePlayed;
    public int fusions;
    public int inversions;
    public int restarts;
    public int extractedEyes;
    public int ends;
    public int moves;

    public static Statistics Load()
    {
        return new Statistics {
            timePlayed = GameManager.i.GetSettingFloat("timePlayed", 0),
            fusions = GameManager.i.GetSettingInt("fusions", 0),
            inversions = GameManager.i.GetSettingInt("inversions", 0),
            restarts = GameManager.i.GetSettingInt("restarts", 0),
            extractedEyes = GameManager.i.GetSettingInt("extractedEyes", 0),
            ends = GameManager.i.GetSettingInt("ends", 0),
            moves = GameManager.i.GetSettingInt("moves", 0),
        };
    }

    public void Save()
    {
        PlayerPrefs.SetFloat("timePlayed", timePlayed);
        PlayerPrefs.SetInt("fusions", fusions);
        PlayerPrefs.SetInt("inversions", inversions);
        PlayerPrefs.SetInt("restarts", restarts);
        PlayerPrefs.SetInt("extractedEyes", extractedEyes);
        PlayerPrefs.SetInt("ends", ends);
        PlayerPrefs.SetInt("moves", moves);
    }
}
