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

    [System.NonSerialized] public List<LevelData> currentCollection;
    [System.NonSerialized] public LevelData currentLevel;
    [System.NonSerialized] public int currentLevelId;
    [System.NonSerialized] public LevelObject[] levelObjects; // Stored by their positions (x + y * sizeX)
    [System.NonSerialized] public LevelObject[] allLevelObjects; // Stored in order

    public Color[] colors;

    [Tooltip("Proportion of the screen that will be outside the safe zone")]
    public float screenMargin = 0.1f;

    [System.NonSerialized] public Vector2 levelCorner;
    [System.NonSerialized] public float tileSize;

    [SerializeField] private Camera mainCamera;

    [SerializeField] private Sprite[] tileSprites;
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private GameObject blobPrefab;
    [SerializeField] private GameObject endPrefab;

    public GameObject particlesPrefab;

    private List<GameObject> tiles;

    private void Awake()
    {
        i = this;
        tiles = new List<GameObject>();
    }

    private void Start()
    {
        currentCollection = LevelLoader.ReadMainCollection();
        currentLevelId = 0;
        MakeLevel(currentCollection[currentLevelId]); // TEST
    }

    private void Update()
    {
        // TODO: slide controls

        Vector2Int direction = Vector2Int.zero;
        if (Input.GetKeyDown(KeyCode.Q)) direction = Vector2Int.left;
        if (Input.GetKeyDown(KeyCode.D)) direction = Vector2Int.right;
        if (Input.GetKeyDown(KeyCode.Z)) direction = Vector2Int.up;
        if (Input.GetKeyDown(KeyCode.S)) direction = Vector2Int.down;

        if (direction != Vector2Int.zero)
        {
            int count = direction.y == 0 ? currentLevel.size.x : currentLevel.size.y;

            for (int i = 0; i < count; i++)
                MoveAllOneTile(direction);

            UpdateObjects();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            MakeLevel(currentCollection[currentLevelId]);
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            currentLevelId += 1;
            currentLevelId %= currentCollection.Count;
            MakeLevel(currentCollection[currentLevelId]);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            currentLevelId += currentCollection.Count - 1;
            currentLevelId %= currentCollection.Count;
            MakeLevel(currentCollection[currentLevelId]);
        }
    }

    public void MakeLevel(LevelData level)
    {
        RemoveCurrentLevel();
        currentLevel = level;

        // Get sizes
        Vector2 safeZoneSize = new Vector2(
            mainCamera.orthographicSize * 2 * Screen.width / Screen.height * (1 - screenMargin - screenMargin),
            mainCamera.orthographicSize * 2 * (1 - screenMargin - screenMargin)
        );

        tileSize = Mathf.Min(safeZoneSize.x / level.size.x, safeZoneSize.y / level.size.y);

        levelCorner = new Vector2(
            -tileSize * level.size.x / 2,
            -tileSize * level.size.y / 2
        );

        for (int x = 0; x < level.size.x; x++)
        {
            for (int y = 0; y < level.size.y; y++)
            {
                SpriteRenderer tile = Instantiate(tilePrefab, GetScreenPosition(x, y), Quaternion.identity).GetComponent<SpriteRenderer>();

                int spriteIndex;
                bool current = GetTile(x, y);
                bool up = GetTile(x, y + 1);
                bool down = GetTile(x, y - 1);
                bool right = GetTile(x + 1, y);
                bool left = GetTile(x - 1, y);

                if (current)
                {
                    if      (!up && !down && !left && !right) spriteIndex = 0;

                    else if (up  && !down && !left && !right) spriteIndex = 1;
                    else if (!up && !down && left  && !right) spriteIndex = 2;
                    else if (!up && down  && !left && !right) spriteIndex = 3;
                    else if (!up && !down && !left && right ) spriteIndex = 4;

                    else if (!up && down  && left  && !right) spriteIndex = 5;
                    else if (!up && down  && !left && right ) spriteIndex = 6;
                    else if (up  && !down && !left && right ) spriteIndex = 7;
                    else if (up  && !down && left  && !right) spriteIndex = 8;

                    else spriteIndex = 9;
                }
                else
                {
                    if      (!up && down  && left  && !right) spriteIndex = 11;
                    else if (up  && !down && left  && !right) spriteIndex = 12;
                    else if (up  && !down && !left && right ) spriteIndex = 13;
                    else if (!up && down  && !left && right ) spriteIndex = 14;

                    else if (!up && down  && left  && right ) spriteIndex = 15;
                    else if (up  && down  && left  && !right) spriteIndex = 16;
                    else if (up  && !down && left  && right ) spriteIndex = 17;
                    else if (up  && down  && !left && right ) spriteIndex = 18;

                    else if (up  && down  && left  && right ) spriteIndex = 19;

                    else spriteIndex = 10;
                }

                tile.sprite = tileSprites[spriteIndex];
                tile.transform.localScale = Vector3.one * tileSize;

                tiles.Add(tile.gameObject);
            }
        }

        allLevelObjects = new LevelObject[level.objects.Length];
        int i = 0;
        levelObjects = new LevelObject[level.size.x * level.size.y];
        foreach (LevelObjectData data in level.objects)
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
            levelObjects[data.position.x + data.position.y * currentLevel.size.x] = newObject;
            allLevelObjects[i] = newObject;

            i++;
        }
    }

    public void RemoveCurrentLevel()
    {
        foreach (GameObject go in tiles)
        {
            Destroy(go);
        }

        if (allLevelObjects != null)
        {
            foreach (LevelObject obj in allLevelObjects)
            {
                if (obj != null)
                    Destroy(obj.gameObject);
            }
        }
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

        return levelObjects[x + y * currentLevel.size.x];
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

                    DestroyLevelObject(target);
                }
                else if (onTarget is End)
                {
                    End other = onTarget as End;

                    if (other.data.color == currentBlob.data.color && other.data.eyes == currentBlob.data.eyes)
                    {
                        currentBlob.MakeParticlesOnApply(onTarget.data.color);
                        currentBlob.MakeParticlesOnApply(onTarget.data.color); // TWce for more particles!
                        DestroyLevelObject(target);
                        MoveObject(objectPos, target);
                        DestroyLevelObject(target);
                        return;
                    }
                }
            }

            MoveObject(objectPos, target); // The object can move successfully
        }
    }

    /// <summary>
    /// Send events to the objects
    /// </summary>
    private void UpdateObjects()
    {
        foreach (LevelObject obj in allLevelObjects)
        {
            if (obj != null)
                obj.ApplyChanges();
        }
    }

    private void MoveObject(Vector2Int from, Vector2Int to)
    {
        int fromIndex = from.x + from.y * currentLevel.size.x;
        int toIndex = to.x + to.y * currentLevel.size.x;

        levelObjects[fromIndex].Move(to);

        levelObjects[toIndex] = levelObjects[fromIndex];
        levelObjects[fromIndex] = null;
    }

    private void DestroyLevelObject(Vector2Int pos)
    {
        int index = pos.x + pos.y * currentLevel.size.x;
        levelObjects[index].DestroyObject();
        levelObjects[index] = null;
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
