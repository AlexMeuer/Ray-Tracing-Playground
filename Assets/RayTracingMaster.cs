using UnityEngine;

public class RayTracingMaster : MonoBehaviour
{
    public ComputeShader rayTracingShader;
    public Texture skyboxTexture;
    public Light directionalLight;
    [Range(1, 8)]
    public  int bounces = 8;
    [HideInInspector]
    public uint currentSample = 0;

    [Range(0.0f, 1.0f)] public float specularMul = 0.04f;
    [Range(0.0f, 1.0f)] public float albedoMul = 0.8f;

    private RenderTexture _target;
    private Camera _camera;
    private Material _addMaterial;
    private static readonly int Sample = Shader.PropertyToID("_Sample");


    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        SetShaderParameters();
        Render(destination);
    }
    
    private void Update()
    {
        if (!transform.hasChanged && !directionalLight.transform.hasChanged) return;
        currentSample = 0;
        transform.hasChanged = false;
    }

    private void SetShaderParameters()
    {
        rayTracingShader.SetMatrix("camera_to_world", _camera.cameraToWorldMatrix);
        rayTracingShader.SetMatrix("camera_inverse_projection", _camera.projectionMatrix.inverse);
        rayTracingShader.SetTexture(0, "skybox_texture", skyboxTexture);
        rayTracingShader.SetVector("pixel_offset", new Vector2(Random.value, Random.value));
        rayTracingShader.SetInt("ray_bounces", bounces);
        var l = directionalLight.transform.forward;
        rayTracingShader.SetVector("directional_light", new Vector4(l.x, l.y, l.x, directionalLight.intensity));
        rayTracingShader.SetVector("specular_mul", Vector3.one * specularMul);
        rayTracingShader.SetVector("albedo_mul", Vector3.one * albedoMul);
    }

    private void Render(RenderTexture destination)
    {
        // Make sure we have a current render target
        InitRenderTexture();

        // Set the target and dispatch the compute shader
        rayTracingShader.SetTexture(0, "result", _target);
        var threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
        var threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
        rayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        // Blit the result texture to the screen
        if (_addMaterial == null)
            _addMaterial = new Material(Shader.Find("Hidden/AddShader"));
        _addMaterial.SetFloat(Sample, currentSample);
        Graphics.Blit(_target, destination, _addMaterial);
        ++currentSample;
    }

    private void InitRenderTexture()
    {
        if (_target != null && _target.width == Screen.width && _target.height == Screen.height) return;
        
        // Release render texture if we already have one
        if (_target != null)
            _target.Release();

        // Get a render target for Ray Tracing
        _target = new RenderTexture(Screen.width, Screen.height, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        _target.enableRandomWrite = true;
        _target.Create();

        currentSample = 0;
    }
}