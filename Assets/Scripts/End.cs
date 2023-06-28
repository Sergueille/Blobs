using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;

public class End : LevelObject
{
    [SerializeField] private GameObject[] groups;
    [SerializeField] private SpriteRenderer[] dots;

    public override void Init(LevelObjectData data)
    {
        base.Init(data);

        for (int i = 0; i < groups.Length; i++)
        {
            if (i == data.eyes - 1)
                groups[i].SetActive(true);
            else
                groups[i].SetActive(false);
        }
    }

    protected override void MoveVisual()
    {
        throw new System.Exception("Shouldn't move an end object!");
    }

    protected override void SetColorVisual(bool _immediate = false)
    {
        foreach (SpriteRenderer dot in dots)
        {
            dot.color = GameManager.i.colors[(int)data.color];
        }
    }
    
    protected override void DestroyImmediately()
    {
        foreach (SpriteRenderer dot in dots)
        {
            dot.enabled = false;
        }
    }
}
