using System.Collections.Generic;
using UnityEngine;

public class Inverter : LevelObject
{
    [SerializeField] GameObject visual;

    [Tooltip("°/s")]
    [SerializeField] private float rotationSpeed;

    private void Update()
    {
        visual.transform.eulerAngles = new Vector3(0, 0, Time.time * rotationSpeed);
    }

    protected override void DestroyImmediately()
    {
        visual.gameObject.SetActive(false);
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
