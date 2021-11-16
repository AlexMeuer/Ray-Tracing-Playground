using UnityEngine;
using UnityEngine.Serialization;

public class RayTracingMaster : MonoBehaviour
{
    public ComputeShader rayTracingShader;
    public Texture skyboxTexture;
    [Range(1, 8)]
    public  int bounces = 8;
    [HideInInspector]
    public uint currentSample = 0;

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
        if (!transform.hasChanged) return;
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