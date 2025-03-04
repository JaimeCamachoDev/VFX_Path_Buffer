using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Splines;

public class SplineComputeControllerDeadSpace : MonoBehaviour
{
    [SerializeField] private VisualEffect vfx;
    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private SplineContainer spline;
    [SerializeField] private int particleCount = 50;

    private GraphicsBuffer splineBuffer;
    private Vector4[] splinePoints;
    private int kernelID;

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

        kernelID = computeShader.FindKernel("SplineCompute");

        GenerateSplineBuffer();
    }

    void GenerateSplineBuffer()
    {
        if (spline == null || spline.Spline == null)
        {
            Debug.LogError("[SplineComputeController] La SplineContainer no tiene una spline válida.");
            enabled = false;
            return;
        }

        float totalLength = spline.Spline.GetLength();
        splinePoints = new Vector4[particleCount];

        float stepSize = totalLength / (particleCount - 1);

        float accumulatedDistance = 0f;
        Vector3 lastPos = spline.Spline.EvaluatePosition(0);
        splinePoints[0] = new Vector4(lastPos.x, lastPos.y, lastPos.z, 1.0f);

        int index = 1;

        for (float t = 0.02f; t < 1f; t += 0.02f)
        {
            Vector3 currentPos = spline.Spline.EvaluatePosition(t);
            accumulatedDistance += Vector3.Distance(lastPos, currentPos);

            if (accumulatedDistance >= stepSize)
            {
                splinePoints[index] = new Vector4(currentPos.x, currentPos.y, currentPos.z, 1.0f);
                lastPos = currentPos;
                accumulatedDistance = 0f;
                index++;

                if (index >= particleCount)
                    break;
            }
        }

        // Corrección: Convertir ambos valores al mismo tipo antes de compararlos
        Vector3 finalPos = spline.Spline.EvaluatePosition(1);
        Vector3 firstPos = new Vector3(splinePoints[0].x, splinePoints[0].y, splinePoints[0].z);

        if (finalPos != firstPos)
        {
            splinePoints[particleCount - 1] = new Vector4(finalPos.x, finalPos.y, finalPos.z, 1.0f);
        }

        splineBuffer.SetData(splinePoints);
    }

    void Update()
    {
        if (computeShader && splineBuffer != null)
        {
            computeShader.SetFloat("TimeValue", Time.time);
            computeShader.SetBuffer(kernelID, "SplineBuffer", splineBuffer);
            computeShader.Dispatch(kernelID, particleCount / 8 + 1, 1, 1);
        }
    }

    void OnDestroy()
    {
        if (splineBuffer != null)
        {
            splineBuffer.Dispose();
            splineBuffer = null;
        }
    }
}
