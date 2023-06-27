using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;

public class End : LevelObject
{

    public override void Init(LevelObjectData data)
    {
        this.data = data;
        // TODO
    }

    public override void Move(Vector2Int newPosition)
    {
        throw new System.Exception("Shouldn't move an end object");
    }

    public void SetColor(GameColor color, bool immediate = false)
    {
        // TODO
    }
    

    public override void DestroyObject()
    {
        // TODO
    }
}
