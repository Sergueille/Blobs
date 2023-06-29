using System.Collections.Generic;
using UnityEngine;

public class Diamond : LevelObject
{
    [SerializeField] SpriteRenderer spriteRenderer;

    protected override void DestroyImmediately()
    {
        spriteRenderer.gameObject.SetActive(false);
    }

    protected override void MoveVisual()
    {
        transform.position = GameManager.i.GetScreenPosition(data.position);
    }

    protected override void SetColorVisual(bool immediate = false)
    {
        // No color!
    }
}
