using UnityEngine;

/// <summary>
/// Añade un borde/outline visual a una torre seleccionada usando duplicado de malla
/// </summary>
public class TowerOutline : MonoBehaviour
{
    private GameObject[] outlineObjects;
    private Material[] outlineMaterials;
    private bool isOutlineActive = false;
    private Color currentColor;

    public void EnableOutline(Color color, float width)
    {
        if (isOutlineActive) return;

        currentColor = color;

        // Obtener todos los MeshRenderers de la torre (incluyendo hijos)
        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
        outlineObjects = new GameObject[meshRenderers.Length];
        outlineMaterials = new Material[meshRenderers.Length];

        for (int i = 0; i < meshRenderers.Length; i++)
        {
            MeshRenderer originalRenderer = meshRenderers[i];
            MeshFilter originalFilter = originalRenderer.GetComponent<MeshFilter>();

            if (originalFilter == null || originalFilter.sharedMesh == null)
                continue;

            // Crear objeto duplicado para el outline
            GameObject outlineObj = new GameObject($"{originalRenderer.name}_Outline");
            outlineObj.transform.SetParent(originalRenderer.transform);
            outlineObj.transform.localPosition = Vector3.zero;
            outlineObj.transform.localRotation = Quaternion.identity;
            outlineObj.transform.localScale = Vector3.one * (1f + width);

            // Copiar la malla
            MeshFilter outlineFilter = outlineObj.AddComponent<MeshFilter>();
            outlineFilter.sharedMesh = originalFilter.sharedMesh;

            // Crear material de outline sólido
            MeshRenderer outlineRenderer = outlineObj.AddComponent<MeshRenderer>();
            Material outlineMat = new Material(Shader.Find("Unlit/Color"));
            outlineMat.color = color;
            outlineRenderer.material = outlineMat;
            outlineMaterials[i] = outlineMat;

            // Renderizar detrás del objeto original
            outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            outlineRenderer.receiveShadows = false;

            // Invertir normales para que el outline se vea desde fuera
            Mesh outlineMesh = new Mesh();
            outlineMesh.vertices = originalFilter.sharedMesh.vertices;
            outlineMesh.triangles = originalFilter.sharedMesh.triangles;
            outlineMesh.normals = originalFilter.sharedMesh.normals;
            outlineMesh.uv = originalFilter.sharedMesh.uv;

            // Invertir triangulos para que se vea desde fuera
            int[] triangles = outlineMesh.triangles;
            for (int j = 0; j < triangles.Length; j += 3)
            {
                int temp = triangles[j];
                triangles[j] = triangles[j + 1];
                triangles[j + 1] = temp;
            }
            outlineMesh.triangles = triangles;
            outlineMesh.RecalculateNormals();

            outlineFilter.mesh = outlineMesh;
            outlineObjects[i] = outlineObj;
        }

        isOutlineActive = true;
    }

    /// <summary>
    /// Actualiza el color del outline sin recrear los objetos
    /// </summary>
    public void UpdateColor(Color newColor)
    {
        if (!isOutlineActive || outlineMaterials == null)
            return;

        currentColor = newColor;

        // Actualizar el color de todos los materiales del outline
        foreach (Material mat in outlineMaterials)
        {
            if (mat != null)
            {
                mat.color = newColor;
            }
        }
    }

    public void DisableOutline()
    {
        if (!isOutlineActive || outlineObjects == null)
            return;

        // Destruir objetos de outline
        foreach (GameObject outlineObj in outlineObjects)
        {
            if (outlineObj != null)
            {
                Destroy(outlineObj);
            }
        }

        outlineObjects = null;
        outlineMaterials = null;
        isOutlineActive = false;
    }

    void OnDestroy()
    {
        DisableOutline();
    }
}