using System.Collections.Generic;
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
    private void Start()
    {
        LevelLoader.ReadMainCollection();
    }
}

public class LevelData
{
    public string title;
    public Vector2Int size;
    public bool[] data;
    public LevelObject[] objects;
}

public class LevelObject
{
    public ObjectType type;
    public GameColor color;
    public int eyes;
    public Vector2Int position;
}
