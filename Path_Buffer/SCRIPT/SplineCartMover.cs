using UnityEngine;
using Unity.Cinemachine; // Asegurar que el espacio de nombres Cinemachine está incluido
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class SplineCartMover : MonoBehaviour
{
    public float Speed = 0.1f; // Velocidad del carrito
    private CinemachineSplineCart splineCart;

    private void OnEnable()
    {
        splineCart = GetComponent<CinemachineSplineCart>();

#if UNITY_EDITOR
        EditorApplication.update += EditorUpdate; // Ejecutar en Editor sin Play
#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.update -= EditorUpdate;
#endif
    }

    private void EditorUpdate()
    {
        if (splineCart == null || !Application.isPlaying)
        {
            MoveCart();
            SceneView.RepaintAll(); // Refrescar la vista del Editor
        }
    }

    private void MoveCart()
    {
        if (splineCart == null) return;

        splineCart.SplinePosition += Speed * Time.deltaTime;
        splineCart.SplinePosition = Mathf.Repeat(splineCart.SplinePosition, 1f); // Mantener dentro del rango 0-1
    }
}
