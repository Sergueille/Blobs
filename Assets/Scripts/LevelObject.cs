using System.Collections.Generic;
using UnityEngine;

public abstract class LevelObject : MonoBehaviour
{
    public LevelObjectData data;

    protected GameColor oldColor;
    protected Vector2Int oldPosition;
    protected bool shouldBeDestroyed = false;

    public bool isDestroyed = false;

    protected List<GameColor> particlesToInstantiate;

    public virtual void Init(LevelObjectData data)
    {
        this.data = data;
        transform.position = GameManager.i.GetScreenPosition(data.position);
        gameObject.transform.localScale = Vector3.one * GameManager.i.tileSize;
        SetColorVisual(true);

        isDestroyed = false;
        oldColor = data.color;
        oldPosition = data.position;
        particlesToInstantiate = new List<GameColor>();
    }

    public void Move(Vector2Int newPosition)
    {
        data.position = newPosition;
    }

    public void DestroyObject()
    {
        shouldBeDestroyed = true;
        isDestroyed = true;
    }
    
    public void SetColor(GameColor color)
    {
        data.color = color;
    }

    public void MakeParticlesOnApply(GameColor color)
    {
        particlesToInstantiate.Add(color);
    }

    public virtual void ApplyChanges()
    {
        if (data.position != oldPosition)
            MoveVisual();
        oldPosition = data.position;

        if (data.color != oldColor)
            SetColorVisual(false);
        oldColor = data.color;

        if (shouldBeDestroyed)
            DestroyImmediately();
        shouldBeDestroyed = false;

        foreach (GameColor color in particlesToInstantiate)
        {
            GameManager.i.CreateParticles(color, GameManager.i.GetScreenPosition(data.position));
        }

        particlesToInstantiate.Clear();
    }

    protected abstract void MoveVisual();
    protected abstract void DestroyImmediately();
    protected abstract void SetColorVisual(bool immediate = false);
}
