using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;

public class End : LevelObject
{
    public override void Init(LevelObjectData data)
    {
        base.Init(data);
    }

    protected override void MoveVisual()
    {
        throw new System.Exception("Shouldn't move an end object");
    }

    protected override void SetColorVisual(bool immediate = false)
    {
        // TODO
    }
    
    protected override void DestroyImmediately()
    {
        // TODO
    }
}
