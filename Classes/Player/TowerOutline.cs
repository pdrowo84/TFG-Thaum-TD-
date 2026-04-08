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

        // Determinar índice de la capa Ignore Raycast (fallback a 2 si no existe)
        int ignoreLayer = LayerMask.NameToLayer("Ignore Raycast");
        if (ignoreLayer < 0) ignoreLayer = 2;

        // Intentar resolver un shader seguro para el outline (con fallbacks)
        Shader outlineShader = Shader.Find("Unlit/Color")
                              ?? Shader.Find("Universal Render Pipeline/Unlit")
                              ?? Shader.Find("Sprites/Default")
                              ?? Shader.Find("Standard");

        if (outlineShader == null)
        {
            Debug.LogWarning("TowerOutline: No se encontró shader para outline. Se omite la creación del outline en esta instancia (evita crash en build).");
            // Liberar arrays por seguridad
            outlineObjects = null;
            outlineMaterials = null;
            return;
        }

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

            // Asegurarnos de que el outline NO bloquee raycasts
            outlineObj.layer = ignoreLayer;
            SetLayerRecursively(outlineObj, ignoreLayer);

            // Copiar la malla
            MeshFilter outlineFilter = outlineObj.AddComponent<MeshFilter>();
            outlineFilter.sharedMesh = originalFilter.sharedMesh;

            // Crear material de outline seguro
            Material outlineMat = null;
            try
            {
                outlineMat = new Material(outlineShader) { color = color };
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"TowerOutline: fallo al crear material de outline: {ex.Message}. Se omite este outline.");
                Destroy(outlineObj);
                continue;
            }

            // Asignar renderer y material
            MeshRenderer outlineRenderer = outlineObj.AddComponent<MeshRenderer>();
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

            // Invertir triángulos para que se vea desde fuera
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

        // Destruir objetos de outline y materiales creados
        for (int idx = 0; idx < outlineObjects.Length; idx++)
        {
            GameObject outlineObj = outlineObjects[idx];
            if (outlineObj != null)
            {
                Destroy(outlineObj);
            }
            // destruir material creado
            if (outlineMaterials != null && outlineMaterials.Length > idx)
            {
                var mat = outlineMaterials[idx];
                if (mat != null)
                {
#if UNITY_EDITOR
                    DestroyImmediate(mat);
#else
                    Destroy(mat);
#endif
                }
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

    // Fija la capa recursivamente para un objeto y sus hijos
    private void SetLayerRecursively(GameObject go, int layer)
    {
        if (go == null) return;
        go.layer = layer;
        foreach (Transform child in go.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}