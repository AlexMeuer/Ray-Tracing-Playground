using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public struct Sphere
{
    public const int Size = 40; // bytes
    public Vector3 Position;
    public float Radius;
    public Vector3 Albedo;
    public Vector3 Specular;
}

public class RayTracingMaster : MonoBehaviour
{
    public ComputeShader rayTracingShader;
    public Texture skyboxTexture;
    public Light directionalLight;
    public Vector2 sphereRadius = new Vector2(3.0f, 8.0f);
    public uint spheresMax = 100;
    public float spherePlacementRadius = 100.0f;
    public bool antialias = true;
    [Range(1, 8)]
    public  int bounces = 8;
    [HideInInspector]
    public uint currentSample = 0;

    private RenderTexture _target;
    private Camera _camera;
    private Material _addMaterial;
    private ComputeBuffer _sphereBuffer;
    private static readonly int Sample = Shader.PropertyToID("_Sample");


    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        currentSample = 0;
        SetUpScene();
    }

    private void OnDisable()
    {
        _sphereBuffer?.Release();
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
        if (antialias)
        {
            rayTracingShader.SetVector("pixel_offset", new Vector2(Random.value, Random.value));
        }
        rayTracingShader.SetInt("ray_bounces", bounces);
        var l = directionalLight.transform.forward;
        rayTracingShader.SetVector("directional_light", new Vector4(l.x, l.y, l.x, directionalLight.intensity));
        rayTracingShader.SetInt("num_spheres", _sphereBuffer.count); // Getting size from shader is not supported on Metal (mac).
        rayTracingShader.SetBuffer(0, "spheres", _sphereBuffer);
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

    private void SetUpScene()
    {
        var spheres = new List<Sphere>();
        
        // Add a number of random spheres
        for (var i = 0; i < spheresMax; ++i)
        {
            var randomPos = Random.insideUnitCircle * spherePlacementRadius;
            var sphere = new Sphere
            {
                Radius = sphereRadius.x + Random.value * (sphereRadius.y - sphereRadius.x)
            };
            sphere.Position = new Vector3(randomPos.x, sphere.Radius, randomPos.y);
            
            // Reject spheres that are intersecting others
            if ((
                from other in spheres
                let minDist = sphere.Radius + other.Radius
                where Vector3.SqrMagnitude(sphere.Position - other.Position) < minDist * minDist
                select other
                ).Any())
            {
                continue;
            }
            
            // Albedo and specular color
            var color = Random.ColorHSV();
            var metal = Random.value < 0.5f;
            sphere.Albedo = metal ? Vector3.zero : new Vector3(color.r, color.g, color.b);
            sphere.Specular = metal ? new Vector3(color.r, color.g, color.b) : Vector3.one * 0.04f;
            
            // Add the sphere to the list
            spheres.Add(sphere);
        }

        _sphereBuffer = new ComputeBuffer(spheres.Count, Sphere.Size);
        _sphereBuffer.SetData(spheres);
    }
}