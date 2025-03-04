using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Splines;

[RequireComponent(typeof(SplineContainer))]
[RequireComponent(typeof(VisualEffect))] // Asegura que este script solo puede existir con un VFX Graph

public class SplineComputeController : MonoBehaviour
{
    [Header("Referencias (Se detectan automáticamente si no se asignan)")]
    public VisualEffect vfx; // Referencia al VFX Graph
    public ComputeShader computeShader; // Referencia al ComputeShader
    public SplineContainer spline; // Referencia a la SplineContainer de Unity

    [Header("Configuración de Partículas")]
    [Min(1)] public int particleCount = 100; // Número de partículas, mínimo 1

    private GraphicsBuffer splineBuffer;
    private Vector4[] splinePoints;
    private int kernel; // Kernel ID

    void Awake()
    {
        if (!vfx) vfx = GetComponent<VisualEffect>();
        if (!spline) spline = GetComponent<SplineContainer>();
        if (!computeShader) computeShader = Resources.Load<ComputeShader>("SplineCompute");

        if (!vfx || !spline || !computeShader)
        {
            Debug.LogError("[SplineComputeController] Faltan referencias importantes.");
            enabled = false;
            return;
        }

        splineBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, particleCount, sizeof(float) * 4);
        vfx.SetGraphicsBuffer("SplineBuffer", splineBuffer);
        vfx.SetInt("ParticleCount", particleCount);

        kernel = computeShader.FindKernel("SplineCompute");

        GenerateSplineBuffer();
    }

    void Start()
    {
        // Obtener el kernel del ComputeShader
        kernel = computeShader.FindKernel("SplineCompute");

        // Crear buffer alineado con float4
        splineBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, particleCount, sizeof(float) * 4);

        // Generar los puntos de la spline real
        GenerateSplineBuffer();

        // Pasar buffer al VFX Graph
        vfx.SetGraphicsBuffer("SplineBuffer", splineBuffer);
        vfx.SetInt("ParticleCount", particleCount);
    }

    void Update()
    {
        // Si ParticleCount es 0 o menor, evitar errores
        if (particleCount <= 0) return;

        // Pasar variables al ComputeShader
        computeShader.SetInt("ParticleCount", particleCount);
        computeShader.SetBuffer(kernel, "SplineBuffer", splineBuffer);

        // Ejecutar ComputeShader
        int threadGroups = Mathf.CeilToInt(particleCount / 8.0f);
        computeShader.Dispatch(kernel, threadGroups, 1, 1);
    }

    void GenerateSplineBuffer()
    {
        // Validar que la spline tenga datos antes de generar el buffer
        if (spline == null || spline.Spline == null)
        {
            Debug.LogError("[SplineComputeController] La SplineContainer no tiene una spline válida.");
            enabled = false;
            return;
        }

        splinePoints = new Vector4[particleCount];

        for (int i = 0; i < particleCount; i++)
        {
            float t = (float)i / (particleCount - 1);
            Vector3 pos = spline.Spline.EvaluatePosition(t);
            splinePoints[i] = new Vector4(pos.x, pos.y, pos.z, 1.0f);
        }

        splineBuffer.SetData(splinePoints);
    }

    void OnDestroy()
    {
        if (splineBuffer != null)
            splineBuffer.Release();
    }
}
