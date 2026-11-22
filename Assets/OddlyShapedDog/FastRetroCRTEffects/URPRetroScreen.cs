using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class URPRetroScreen : MonoBehaviour
{
    public Camera sourceCamera;
    public Material material;
    public Vector2Int resolution = new(320, 240);
    public KeyCode toggleKey = KeyCode.F2;
    public bool startEnabled = true;

    RenderTexture rt;
    RawImage img;
    bool effectOn;

    void Start()
    {
        img = GetComponent<RawImage>();

        rt = new RenderTexture(resolution.x, resolution.y, 24)
        {
            filterMode = FilterMode.Point
        };

        material.SetInteger("_ResolutionX", resolution.x);
        material.SetInteger("_ResolutionY", resolution.y);

        img.texture = rt;
        img.material = material;

        SetEffect(startEnabled);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            SetEffect(!effectOn);
    }

    public void SetEffect(bool on)
    {
        effectOn = on;
        img.enabled = on;
        sourceCamera.targetTexture = on ? rt : null;
    }

    void OnDestroy()
    {
        if (sourceCamera) sourceCamera.targetTexture = null;
        if (rt) rt.Release();
    }
}