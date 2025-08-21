using UnityEngine;
using TMPro;

public sealed class ScoreFlyer : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private float lifetime = 1.1f;
    [SerializeField] private float riseSpeed = 1.5f;
    [SerializeField] private float fadeSpeed = 2.5f;

    private float _t;
    private Color _c;
    private Camera _cam;

    private void Awake()
    {
        if (label) _c = label.color;
    }

    public void Setup(int points, Camera cam)
    {
        _cam = cam;
        if (!label) return;
        label.text = (points > 0 ? "+" : "") + points.ToString();
        _c = label.color;
    }

    private void Update()
    {
        // Float upward
        transform.position += Vector3.up * riseSpeed * Time.deltaTime;

        // Fade
        _t += Time.deltaTime * fadeSpeed;
        if (label)
        {
            var col = _c;
            col.a = Mathf.Clamp01(1f - _t);
            label.color = col;
        }

        if (_t * (1f / fadeSpeed) >= lifetime) Destroy(gameObject);
    }

    private void LateUpdate()
    {
        if (_cam != null)
        {
            Vector3 dir = transform.position - _cam.transform.position;
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
    }
}
