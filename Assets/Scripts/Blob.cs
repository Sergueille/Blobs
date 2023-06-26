using System.Collections.Generic;
using System.Data.Common;
using UnityEngine;

public class Blob : LevelObject
{
    [SerializeField] private SpriteRenderer blobSprite;

    [SerializeField] private float colorTransitionDuration;
    [SerializeField] private float moveSpeed;

    public override void Init(LevelObjectData data)
    {
        this.data = data;
        blobSprite.transform.localScale = Vector3.one * GameManager.i.tileSize;
        SetColor(data.color, true);
    }

    public override void Move(Vector2Int newPosition)
    {
        if (data.position == newPosition) return;

        Vector2 newScreenPosition = GameManager.i.GetScreenPosition(newPosition);
        float distance = (newPosition - data.position).magnitude;
        data.position = newPosition;
        LeanTween.move(gameObject, newScreenPosition, moveSpeed * distance);
    }

    public void SetColor(GameColor color, bool immediate = false)
    {
        data.color = color;

        if (immediate)
            blobSprite.color = GameManager.i.colors[(int)color];
        else
            LeanTween.color(blobSprite.gameObject, GameManager.i.colors[(int)color], colorTransitionDuration);
    }
}
