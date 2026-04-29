using UnityEngine;

/// <summary>
/// Borde visual al seleccionar torre: copia de malla escalada + material dedicado URP.
/// En build, Shader.Find a menudo falla o asigna shaders incompatibles; por eso se usa
/// Resources/Shaders/TowerOutline (incluido siempre en la build).
/// </summary>
public class TowerOutline : MonoBehaviour
{
    private const string OutlineShaderResourcePath = "Shaders/TowerOutline";
    private const string OutlineShaderName = "Game/TowerOutline";

    private GameObject[] outlineObjects;
    private Material[] outlineMaterials;
    private bool isOutlineActive = false;
    private Color currentColor;

    public void EnableOutline(Color color, float width)
    {
        if (isOutlineActive) return;

        currentColor = color;

        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
        outlineObjects = new GameObject[meshRenderers.Length];
        outlineMaterials = new Material[meshRenderers.Length];

        int ignoreLayer = LayerMask.NameToLayer("Ignore Raycast");
        if (ignoreLayer < 0) ignoreLayer = 2;

        Shader outlineShader = Resources.Load<Shader>(OutlineShaderResourcePath);
        if (outlineShader == null)
            outlineShader = Shader.Find(OutlineShaderName);

        bool useDedicatedOutline = outlineShader != null && outlineShader.name == OutlineShaderName;

        if (!useDedicatedOutline)
        {
            outlineShader = Shader.Find("Unlit/Color")
                            ?? Shader.Find("Universal Render Pipeline/Unlit")
                            ?? Shader.Find("Sprites/Default")
                            ?? Shader.Find("Standard");
        }

        if (outlineShader == null)
        {
            Debug.LogWarning("TowerOutline: No shader de outline (¿falta Resources/Shaders/TowerOutline.shader?). Se omite outline.");
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

            GameObject outlineObj = new GameObject($"{originalRenderer.name}_Outline");
            outlineObj.transform.SetParent(originalRenderer.transform);
            outlineObj.transform.localPosition = Vector3.zero;
            outlineObj.transform.localRotation = Quaternion.identity;
            outlineObj.transform.localScale = Vector3.one * (1f + width);


            outlineObj.layer = ignoreLayer;
            SetLayerRecursively(outlineObj, ignoreLayer);

            MeshFilter outlineFilter = outlineObj.AddComponent<MeshFilter>();

            if (useDedicatedOutline)
            {
                outlineFilter.sharedMesh = originalFilter.sharedMesh;
            }
            else
            {
                outlineFilter.sharedMesh = originalFilter.sharedMesh;
                Mesh outlineMesh = new Mesh();
                outlineMesh.vertices = originalFilter.sharedMesh.vertices;
                outlineMesh.triangles = originalFilter.sharedMesh.triangles;
                outlineMesh.normals = originalFilter.sharedMesh.normals;
                outlineMesh.uv = originalFilter.sharedMesh.uv;

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
            }

            Material outlineMat = null;
            try
            {
                outlineMat = new Material(outlineShader);
                ApplyOutlineColor(outlineMat, color);

                if (!useDedicatedOutline && outlineMat.HasProperty("_Cull"))
                    outlineMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Front);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"TowerOutline: fallo al crear material: {ex.Message}. Se omite.");
                Destroy(outlineObj);
                continue;
            }

            MeshRenderer outlineRenderer = outlineObj.AddComponent<MeshRenderer>();
            outlineRenderer.material = outlineMat;
            outlineMaterials[i] = outlineMat;

            outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            outlineRenderer.receiveShadows = false;

            outlineObjects[i] = outlineObj;
        }

        isOutlineActive = true;
    }

    private static void ApplyOutlineColor(Material mat, Color color)
    {
        if (mat == null) return;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);
        mat.color = color;
    }

    public void UpdateColor(Color newColor)
    {
        if (!isOutlineActive || outlineMaterials == null)
            return;

        currentColor = newColor;

        foreach (Material mat in outlineMaterials)
        {
            if (mat != null)
                ApplyOutlineColor(mat, newColor);
        }
    }

    public void DisableOutline()
    {
        if (!isOutlineActive || outlineObjects == null)
            return;

        for (int idx = 0; idx < outlineObjects.Length; idx++)
        {
            GameObject outlineObj = outlineObjects[idx];
            if (outlineObj != null)
                Destroy(outlineObj);

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

    private void SetLayerRecursively(GameObject go, int layer)
    {
        if (go == null) return;
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
