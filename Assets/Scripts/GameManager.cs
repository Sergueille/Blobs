using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public enum ObjectType
{
    blob, end
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
    public static GameManager i;

    [NonSerialized] public List<LevelData> currentCollection;
    [NonSerialized] public LevelData currentLevel;
    [NonSerialized] public int currentLevelId;
    [NonSerialized] public LevelObject[] levelObjects; // Stored in order

    public Color[] colors;

    [Tooltip("Proportion of the screen that will be outside the safe zone")]
    public float screenMargin = 0.1f;

    [NonSerialized] public Vector2 levelCorner;
    [NonSerialized] public float tileSize;

    [SerializeField] private Camera mainCamera;

    public Sprite[] tileSprites;
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private GameObject blobPrefab;
    [SerializeField] private GameObject endPrefab;

    [SerializeField] private Material bgStripes;
    [SerializeField] private Material transitionStripes;
    [SerializeField] private float stripesSpeed;
    [SerializeField] private float transitionDuration;

    [SerializeField] private float slideSensitivity;

    public GameObject particlesPrefab;

    private List<GameObject> tiles;

    private Vector2Int lastDirection;
    private Vector2 lastMousePosition;
    private bool lastTimeMouseWasDown = false;

    private void Awake()
    {
        i = this;
        tiles = new List<GameObject>();
    }

    private void Start()
    {
        currentCollection = LevelLoader.ReadMainCollection();
        currentLevelId = 0;
        MakeLevel(currentLevelId);
        transitionStripes.SetFloat("_Discard", 1);
    }

    private void Update()
    {
        bgStripes.SetFloat("_Shift", Time.time * stripesSpeed + 1);
        transitionStripes.SetFloat("_Shift", Time.time * stripesSpeed + 1);

        if (currentLevel == null) return;

        // Key controls
        Vector2Int direction = Vector2Int.zero;
        if (Input.GetKeyDown(KeyCode.Q)) direction = Vector2Int.left;
        if (Input.GetKeyDown(KeyCode.D)) direction = Vector2Int.right;
        if (Input.GetKeyDown(KeyCode.Z)) direction = Vector2Int.up;
        if (Input.GetKeyDown(KeyCode.S)) direction = Vector2Int.down;

        // Slide controls
        if (Input.GetMouseButton(0))
        {
            if (lastTimeMouseWasDown)
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

            if (direction == lastDirection)
                direction = Vector2Int.zero;

            if (direction != Vector2Int.zero)
                lastDirection = direction;

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
            int count = direction.y == 0 ? currentLevel.size.x : currentLevel.size.y;

            for (int i = 0; i < count; i++)
                MoveAllOneTile(direction);

            UpdateObjects();

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

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            currentLevelId += 1;
            currentLevelId %= currentCollection.Count;
            MakeLevel(currentLevelId);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            currentLevelId += currentCollection.Count - 1;
            currentLevelId %= currentCollection.Count;
            MakeLevel(currentLevelId);
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            currentCollection = LevelLoader.ReadMainCollection();
            currentLevelId = 0;
            MakeLevel(currentLevelId);
        }
    }

    public void RestartLevel()
    {
        MakeLevelWithTransition(currentLevelId);
    }

    private void OnLevelComplete()
    {
        currentLevelId += 1;
        currentLevelId %= currentCollection.Count;
        MakeLevelWithTransition(currentLevelId);
    }

    public void MakeLevelWithTransition(int levelIndex)
    {
        MakeTransition(() => {
            MakeLevel(levelIndex);
        });
    }

    public void MakeTransition(Action callback)
    {
        LeanTween.value(1, 0, transitionDuration / 2).setEaseInOutExpo().setOnUpdate(val => {
            float actualVal = val < 0.01 ? 0 : val;
            transitionStripes.SetFloat("_Discard", actualVal);
        }).setOnComplete(() => {
            transitionStripes.SetFloat("_Discard", 0);
            callback();

            LeanTween.value(0, 1, transitionDuration / 2).setEaseInOutExpo().setOnUpdate(val => {
                transitionStripes.SetFloat("_Discard", val);
            }).setOnComplete(() => {
                transitionStripes.SetFloat("_Discard", 1);
            });
        });
    }

    public void MakeLevel(int levelIndex)
    {
        RemoveCurrentLevel();

        currentLevelId = levelIndex;
        currentLevel = currentCollection[levelIndex];

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
                SpriteRenderer tile = Instantiate(tilePrefab, GetScreenPosition(x, y), Quaternion.identity).GetComponent<SpriteRenderer>();

                int spriteIndex = GetSpriteIndexOfTile(currentLevel, x, y);

                tile.sprite = tileSprites[spriteIndex];
                tile.transform.localScale = Vector3.one * (tileSize + 0.005f);

                tiles.Add(tile.gameObject);
            }
        }

        levelObjects = new LevelObject[currentLevel.objects.Length];
        int i = 0;
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

            LevelObjectData copiedData = data.Clone(); 

            LevelObject newObject = Instantiate(prefab).GetComponent<LevelObject>();
            newObject.Init(copiedData);
            levelObjects[i] = newObject;

            i++;
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

    public T GetObject<T>(int x, int y) where T : LevelObject
    {
        if (x < 0 || x >= currentLevel.size.x || y < 0 || y >= currentLevel.size.y)
            return null;

        foreach (LevelObject obj in levelObjects)
            if (obj.data.position.x == x && obj.data.position.y == y && !obj.isDestroyed && obj is T)
                return obj as T;

        return null;
    }

    public LevelObject GetObject(Vector2Int pos)
    {
        return GetObject(pos.x, pos.y);
    }

    /// <summary>
    /// Move all objects by one tile
    /// </summary>
    private void MoveAllOneTile(Vector2Int direction)
    {
        void Move(int x, int y)
            => MoveObjectOneTile(new Vector2Int(x, y), direction);

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

    private void MoveObjectOneTile(Vector2Int objectPos, Vector2Int direction)
    {
        LevelObject current = GetObject(objectPos);
        if (current == null) return; // No object

        if (current is Blob)
        {
            Vector2Int target = objectPos + direction;

            if (!GetTile(target)) return; // There is a wall

            LevelObject onTarget = GetObject(target);
            if (onTarget != null) // There is another object
            {
                Blob currentBlob = current as Blob;

                if (onTarget is Blob)
                {
                    // Fusion!
                    Blob other = onTarget as Blob;

                    currentBlob.MakeParticlesOnApply(currentBlob.data.color);
                    currentBlob.MakeParticlesOnApply(other.data.color);

                    currentBlob.AddEyes(other.data.eyes);
                    currentBlob.SetColor(other.data.color | current.data.color);

                    other.DestroyObject();

                    MoveObject(objectPos, target);

                    TestEnd(currentBlob);
                    return;
                }
                else if (onTarget is End)
                {
                    MoveObject(objectPos, target);
                    TestEnd(currentBlob);

                    return;
                }
            }

            MoveObject(objectPos, target); // The object can move successfully
        }
    }

    private void TestEnd(Blob blob)
    {
        End end = GetObject<End>(blob.data.position.x, blob.data.position.y);
        if (end == null) return;

        if (end.data.color == blob.data.color && end.data.eyes == blob.data.eyes)
        {
            blob.MakeParticlesOnApply(end.data.color);
            blob.MakeParticlesOnApply(end.data.color); // Twice for more particles!
            end.DestroyObject();
            blob.DestroyObject();
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

    private void MoveObject(Vector2Int from, Vector2Int to)
    {
        LevelObject obj = GetObject(from);
        obj.Move(to);
    }

    public void CreateParticles(GameColor color, Vector2 position)
    {
        ColoredParticleSystem ps = Instantiate(particlesPrefab, position, Quaternion.identity).GetComponent<ColoredParticleSystem>();
        ps.transform.position = new Vector3(
            ps.transform.position.x,
            ps.transform.position.y,
            -5
        );
        ps.Play(color);
    }
}

public class LevelData
{
    public string title;
    public Vector2Int size;
    public bool[] data;
    public LevelObjectData[] objects;
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
