using UnityEngine;

/// <summary>
/// Al instanciar la torre, aplica una ligera variación de color en los materiales
/// (más/menos saturación, matiz ligeramente distinto, más claro/oscuro).
/// Usa MaterialPropertyBlock para no alterar el material compartido del proyecto.
/// </summary>
[DisallowMultipleComponent]
public class TowerColorVariation : MonoBehaviour
{
    public enum ApplyMoment
    {
        Awake,
        Start,
        ManualOnly
    }

    [Header("Cuándo aplicar")]
    [SerializeField] private ApplyMoment whenToApply = ApplyMoment.Awake;

    [Header("Variación HSV (por torre instanciada)")]
    [Tooltip("Desplazamiento de matiz en grados (ej. -15 a 15 = un poco más verde/azul según la base).")]
    [SerializeField] private Vector2 hueShiftDegrees = new Vector2(-12f, 12f);

    [Tooltip("Multiplicador de saturación (1 = igual). Ej. 0.85–1.15")]
    [SerializeField] private Vector2 saturationMultiplier = new Vector2(0.85f, 1.15f);

    [Tooltip("Multiplicador de brillo/value (1 = igual). Ej. 0.9–1.1")]
    [SerializeField] private Vector2 valueMultiplier = new Vector2(0.9f, 1.1f);

    [Header("Qué pintar")]
    [Tooltip("Si está vacío, se usan MeshRenderer y SkinnedMeshRenderer en hijos (no partículas).")]
    [SerializeField] private Renderer[] targetRenderers;

    [SerializeField] private bool includeInactiveChildren = true;

    private void Awake()
    {
        if (whenToApply == ApplyMoment.Awake)
            ApplyRandomVariation();
    }

    private void Start()
    {
        if (whenToApply == ApplyMoment.Start)
            ApplyRandomVariation();
    }

    /// <summary>
    /// Aplica una variación nueva. Con ManualOnly debes llamar esto tú (p. ej. al sacar del pool).
    /// </summary>
    public void ApplyRandomVariation()
    {
        float hAdd = Random.Range(hueShiftDegrees.x, hueShiftDegrees.y) / 360f;
        float sMul = Random.Range(saturationMultiplier.x, saturationMultiplier.y);
        float vMul = Random.Range(valueMultiplier.x, valueMultiplier.y);

        Renderer[] renderers = GetTargetRenderers();
        foreach (Renderer r in renderers)
        {
            if (r == null) continue;
            ApplyToRenderer(r, hAdd, sMul, vMul);
        }
    }

    private Renderer[] GetTargetRenderers()
    {
        if (targetRenderers != null && targetRenderers.Length > 0)
            return targetRenderers;

        var all = GetComponentsInChildren<Renderer>(includeInactiveChildren);
        var list = new System.Collections.Generic.List<Renderer>();
        foreach (Renderer r in all)
        {
            if (r is MeshRenderer || r is SkinnedMeshRenderer)
                list.Add(r);
        }
        return list.ToArray();
    }

    private static void ApplyToRenderer(Renderer r, float hueAdd, float satMul, float valMul)
    {
        int count = r.sharedMaterials.Length;
        for (int i = 0; i < count; i++)
        {
            Material shared = r.sharedMaterials[i];
            if (shared == null) continue;

            if (!TryGetTintProperty(shared, out string prop, out Color baseColor))
                continue;

            Color.RGBToHSV(baseColor, out float h, out float s, out float v);
            h = Mathf.Repeat(h + hueAdd, 1f);
            s = Mathf.Clamp01(s * satMul);
            v = Mathf.Clamp01(v * valMul);
            Color newColor = Color.HSVToRGB(h, s, v);

            var block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block, i);
            block.SetColor(prop, newColor);
            r.SetPropertyBlock(block, i);
        }
    }

    private static bool TryGetTintProperty(Material mat, out string propertyName, out Color color)
    {
        // URP/HDRP Lit
        if (mat.HasProperty("_BaseColor"))
        {
            propertyName = "_BaseColor";
            color = mat.GetColor("_BaseColor");
            return true;
        }

        // Built-in y muchos shaders
        if (mat.HasProperty("_Color"))
        {
            propertyName = "_Color";
            color = mat.GetColor("_Color");
            return true;
        }

        propertyName = null;
        color = Color.white;
        return false;
    }
}
