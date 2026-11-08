using UnityEngine;

[ExecuteInEditMode, RequireComponent(typeof(Camera))]
public class RetroImageEffect : MonoBehaviour
{
	//public float intensity;
	public Material material;
	public Vector2Int resolution = new(320, 240);

	protected Camera thisCamera;
	protected int viewportWidth;
	protected int viewportHeight;
	protected RenderTexture rt;

	void Awake()
	{	
		if (!TryGetComponent(out thisCamera))
		{
			throw new UnityException("Camera component not found!");
		}

		if (material == null)
		{
			throw new UnityException("Material not found!");
		}

		InitializeRenderTexture(Screen.width, Screen.height);
	}

	private void InitializeRenderTexture(int width, int height)
	{
		float displayScale = GetDisplayScale(width, height);

		viewportWidth = width;
		viewportHeight = height;

		rt = new RenderTexture(
		   (int)(viewportWidth / displayScale),
		   (int)(viewportHeight / displayScale),
		   24
		);
	}

	private float GetDisplayScale(int width, int height)
	{
		float emulatedAspectRatio = (float)resolution.x / (float)resolution.y;
		float actualAspectRatio = (float)width / (float)height;

		if (actualAspectRatio > emulatedAspectRatio)
        {
			// fit to height
			return (float)Screen.height / resolution.y;
        }
		else
        {
			// fit to width
			return (float)Screen.width / resolution.x;
		}
	}

    private void Update()
	{
		float emulatedAspectRatio = (float)resolution.x / (float)resolution.y;
		float actualAspectRatio = (float)Screen.width / (float)Screen.height;

		// by how much would we have to multiply
		// the emulated height to match the actual height?
		float scaleToMatchSourceHeight = actualAspectRatio / emulatedAspectRatio;

		if (scaleToMatchSourceHeight < 1.0f)
        {
			thisCamera.rect = new Rect(
				0.0f,
				(1.0f - scaleToMatchSourceHeight) / 2.0f,
				1.0f,
				scaleToMatchSourceHeight
			);
		}
		else
		{
			float scaleToMatchSourceWidth = 1.0f / scaleToMatchSourceHeight;

			thisCamera.rect = new Rect(
				(1.0f - scaleToMatchSourceWidth) / 2.0f,
				0.0f,
				scaleToMatchSourceWidth,
				1.0f
			);
		}
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		if (source.width != viewportWidth || source.height != viewportHeight)
        {
			InitializeRenderTexture(source.width, source.height);
			Debug.Log("Init render texture");
		}

		// for compatibility with the CRT shader
		material.SetInteger("_ResolutionX", resolution.x);
		material.SetInteger("_ResolutionY", resolution.y);

		Graphics.Blit(source, rt);
		Graphics.Blit(rt, destination, material);
	}
}
