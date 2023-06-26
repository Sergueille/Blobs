using System.Collections.Generic;
using UnityEngine;

public abstract class LevelObject : MonoBehaviour
{
    public LevelObjectData data;

    public abstract void Init(LevelObjectData data);
    public abstract void Move(Vector2Int newPosition);
}
