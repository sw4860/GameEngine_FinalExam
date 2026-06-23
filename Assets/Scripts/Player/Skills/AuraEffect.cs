using UnityEngine;

public class AuraEffect : MonoBehaviour
{
    private LineRenderer _lineRenderer;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private ParticleSystem _particleSystem;

    private int _vertexCount = 60;
    private float _rotationSpeed = 25f;
    private Color _centerColor = new Color(0f, 0.75f, 1f, 0.12f);
    private Color _outerColor = new Color(0f, 0.75f, 1f, 0f);
    private Color _lineColor = new Color(0f, 0.8f, 1f, 0.5f);

    private Mesh _mesh;

    private void Awake()
    {
        InitializeComponents();
    }

    private void Update()
    {
        transform.Rotate(Vector3.forward, _rotationSpeed * Time.deltaTime);
    }

    private void InitializeComponents()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        Material defaultMaterial = new Material(shader);

        _meshFilter = gameObject.AddComponent<MeshFilter>();
        _meshRenderer = gameObject.AddComponent<MeshRenderer>();
        _meshRenderer.sharedMaterial = defaultMaterial;
        _meshRenderer.sortingOrder = -1;

        _lineRenderer = gameObject.AddComponent<LineRenderer>();
        _lineRenderer.sharedMaterial = defaultMaterial;
        _lineRenderer.startWidth = 0.04f;
        _lineRenderer.endWidth = 0.04f;
        _lineRenderer.startColor = _lineColor;
        _lineRenderer.endColor = _lineColor;
        _lineRenderer.sortingOrder = 0;

        _particleSystem = gameObject.AddComponent<ParticleSystem>();
        ConfigureParticleSystem(defaultMaterial);
    }

    private void ConfigureParticleSystem(Material mat)
    {
        _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = _particleSystem.main;
        main.loop = true;
        main.duration = 1f;
        main.startLifetime = 1.0f;
        main.startSpeed = 0.15f;
        main.startSize = 0.12f;
        main.startColor = new Color(0f, 0.8f, 1f, 0.4f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = _particleSystem.emission;
        emission.rateOverTime = 20f;

        var shape = _particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radiusThickness = 0.05f;

        var velocity = _particleSystem.velocityOverLifetime;
        velocity.enabled = true;
        velocity.radial = 0.08f;

        var colorOverLifetime = _particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(0f, 0.75f, 1f), 0f), new GradientColorKey(new Color(0f, 0.5f, 1f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = grad;

        var renderer = _particleSystem.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = mat;
            renderer.sortingOrder = 1;
        }

        _particleSystem.Play();
    }

    public void Setup(float radius)
    {
        UpdateMesh(radius);
        UpdateLine(radius);
        UpdateParticles(radius);
    }

    private void UpdateMesh(float radius)
    {
        if (_mesh == null)
        {
            _mesh = new Mesh();
            _meshFilter.mesh = _mesh;
        }

        Vector3[] vertices = new Vector3[_vertexCount + 1];
        Color[] colors = new Color[_vertexCount + 1];
        int[] triangles = new int[_vertexCount * 3];

        vertices[0] = Vector3.zero;
        colors[0] = _centerColor;

        float angleStep = 360f / _vertexCount;

        for (int i = 0; i < _vertexCount; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            colors[i + 1] = _outerColor;

            int triIndex = i * 3;
            triangles[triIndex] = 0;
            triangles[triIndex + 1] = i + 1;
            triangles[triIndex + 2] = (i + 1) % _vertexCount + 1;
        }

        _mesh.vertices = vertices;
        _mesh.colors = colors;
        _mesh.triangles = triangles;
        _mesh.RecalculateBounds();
    }

    private void UpdateLine(float radius)
    {
        if (_lineRenderer == null) return;

        _lineRenderer.positionCount = _vertexCount + 1;
        _lineRenderer.useWorldSpace = false;

        float angleStep = 360f / _vertexCount;

        for (int i = 0; i <= _vertexCount; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;

            _lineRenderer.SetPosition(i, new Vector3(x, y, 0f));
        }
    }

    private void UpdateParticles(float radius)
    {
        if (_particleSystem == null) return;

        var shape = _particleSystem.shape;
        shape.radius = radius;
    }
}
