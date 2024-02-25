using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class LiquidRenderer : MonoBehaviour
{
    [SerializeField]
    Material liquidMaterial;
    [SerializeField]
    GameObject renderQuad;
    [SerializeField]
    GameObject displayQuad;
    [SerializeField]
    Camera camera;

    [SerializeField]
    Material finalDisplayMaterial;

    bool isOddFrame = false;
    RenderTexture renderTexA;
    RenderTexture renderTexB;

    private void Start() 
    {
        AdaptToResolution();
    }

    [ContextMenu("Adapt to resolution")]
    private void AdaptToResolution() 
    {
        if (renderTexA != null) Destroy(renderTexA);
        if (renderTexB != null) Destroy(renderTexB);

        float ratio = (float)Screen.width / (float)Screen.height;
        renderQuad.transform.localScale = new Vector3(10 * ratio, 10, 1);
        displayQuad.transform.localScale = new Vector3(10 * ratio, 10, 1);
        renderTexA = new RenderTexture(Screen.width, Screen.height, 24);
        renderTexB = new RenderTexture(Screen.width, Screen.height, 24);
    }

    private void Update() 
    {
        if (isOddFrame) {
            liquidMaterial.SetTexture("_Previous", renderTexA);
            camera.targetTexture = renderTexB;
            finalDisplayMaterial.SetTexture("_MainTex", renderTexB);
        }
        else {
            liquidMaterial.SetTexture("_Previous", renderTexB);
            camera.targetTexture = renderTexA;
            finalDisplayMaterial.SetTexture("_MainTex", renderTexA);
        }

        isOddFrame = !isOddFrame;
    }
}
