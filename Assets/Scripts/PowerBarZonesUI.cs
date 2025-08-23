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

        SetZone(zonePerfect, advisor.PerfectRange);
        SetZone(zoneMake, advisor.MakeRange);
        SetZone(zoneBackboard, advisor.BackbdRange);
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
