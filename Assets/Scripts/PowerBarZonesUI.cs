using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class PowerBarZonesUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ShotPowerAdvisor advisor;
    [SerializeField] private Image barBackground;
    [SerializeField] private Image zonePerfect;
    [SerializeField] private Image zoneMake;
    [SerializeField] private Image zoneBackboard;
    [SerializeField] private Image fill;

    [Header("Colors")]
    [SerializeField] private Color perfectColor = new Color(0.2f, 1f, 0.2f, 0.6f);
    [SerializeField] private Color makeColor = new Color(1f, 0.92f, 0.2f, 0.5f);
    [SerializeField] private Color backboardColor = new Color(0.6f, 0.6f, 1f, 0.45f);

    private RectTransform _bgRt;

    private void Awake()
    {
        _bgRt = barBackground ? barBackground.rectTransform : null;
        ApplyColors();
    }

    private void OnEnable() { StartCoroutine(RefreshNextFrame()); }
    private IEnumerator RefreshNextFrame() { yield return null; RefreshZones(); }
    private void OnRectTransformDimensionsChange() { if (isActiveAndEnabled) RefreshZones(); }


    public void RefreshZones()
    {
        if (!advisor || _bgRt == null) return;

        advisor.Recompute();

        var p = advisor.PerfectRange;
        var m = advisor.MakeRange;
        var b = advisor.BackbdRange;

        // UI clipping (non-distruttivo per la logica di gioco)
        if (p.valid && m.valid) m.min = Mathf.Max(m.min, p.max);
        if (m.valid && b.valid) b.min = Mathf.Max(b.min, m.max);

        // Invalida bande troppo sottili (evita 1.00-1.00 visive)
        const float eps = 0.005f;
        if (p.valid && (p.max - p.min) < eps) p.valid = false;
        if (m.valid && (m.max - m.min) < eps) m.valid = false;
        if (b.valid && (b.max - b.min) < eps) b.valid = false;

        SetZone(zonePerfect, p);
        SetZone(zoneMake, m);
        SetZone(zoneBackboard, b);

        Debug.Log(
            $"Ranges (UI-clipped): perfect=({p.valid},{p.min:F2}-{p.max:F2})  " +
            $"make=({m.valid},{m.min:F2}-{m.max:F2})  " +
            $"back=({b.valid},{b.min:F2}-{b.max:F2})"
        );
    }



    private void SetZone(Image img, ShotPowerAdvisor.Range01 r)
    {
        if (!img) return;
        var rt = img.rectTransform;

        if (!r.valid)
        {
            img.enabled = false;
            return;
        }

        img.enabled = true;

        float h = _bgRt.rect.height;
        float yMin = r.min * h;
        float yMax = r.max * h;
        float height = Mathf.Max(2f, yMax - yMin);

        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(0f, height);
        rt.anchoredPosition = new Vector2(0f, yMin);
    }

    private void ApplyColors()
    {
        if (zonePerfect) zonePerfect.color = perfectColor;
        if (zoneMake) zoneMake.color = makeColor;
        if (zoneBackboard) zoneBackboard.color = backboardColor;
    }

    public void SetFill01(float value01)
    {
        if (fill) fill.fillAmount = Mathf.Clamp01(value01);
    }
}
