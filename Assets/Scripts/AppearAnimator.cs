using System.Collections.Generic;
using UnityEngine;

public class AppearAnimator : MonoBehaviour
{
    [SerializeField] private float appearDuration = 0.8f;
    private Vector3 initialScale;
    private int state = 0; // 0: needs to appear in 2 frames, 1: needs to appear next frame, 2: done
    
    private void OnEnable()
    {
        state = 0; // Needs to appear again
    }

    private void Update()
    {
        if (state == 0)
        {
            state++;
        }
        else if (state == 1) // Delaying to second update (wait while the UIManager is initializing, wait for layout rebuild)
        {
            UIManager.i.elementsToAppear.Add(this);
            initialScale = transform.localScale;
            transform.localScale = Vector3.zero;

            state = 2;
        }
    }

    public void AppearAnimation()
    {
        LeanTween.scale(gameObject, initialScale, appearDuration).setEaseOutElastic();
    }
}
