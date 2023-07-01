using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonAnimator : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Image img;
    [SerializeField] private Image icon;

    [SerializeField] private float clickDuration;
    [SerializeField] private float bounceDuration;
    [SerializeField] private float scaleAmount;

    [SerializeField] private bool spinIcon;
    [SerializeField] private float spinDuration;

    public void Click()
    {
        if (spinIcon)
        {
            LeanTween.value(icon.gameObject, 0, -360, spinDuration).setEaseOutCubic().setOnUpdate(val => {
                icon.transform.eulerAngles = new Vector3(0, 0, val);
            });
        }
    }

    private void Bounce()
    {
        LeanTween.cancel(img.gameObject);
        LeanTween.scale(img.gameObject, Vector3.one, bounceDuration).setEaseOutElastic();
        GameManager.i.PlayClickSound();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        LeanTween.cancel(img.gameObject);
        LeanTween.scale(img.gameObject, new Vector3(1 - scaleAmount / 2, 1 - scaleAmount, 1), clickDuration);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Bounce();
    }
}
