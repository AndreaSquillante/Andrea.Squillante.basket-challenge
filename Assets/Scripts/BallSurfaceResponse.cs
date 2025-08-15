using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public sealed class BallSurfaceResponse : MonoBehaviour
{
    [Header("Ground detection")]
    [SerializeField] private LayerMask groundMask; 
    [SerializeField] private float groundLinearDrag = 0.15f;
    [SerializeField] private float groundAngularDrag = 0.25f;

    [Header("Rest detection")]
    [SerializeField] private float restSpeed = 0.25f;      // m/s
    [SerializeField] private float restAngSpeed = 0.8f;    // rad/s
    [SerializeField] private float restTime = 0.7f;        // sec

    private Rigidbody _rb;
    private BallLauncher _launcher;
    private int _groundContacts;
    private float _restTimer;
    private float _defaultLinearDrag;
    private float _defaultAngularDrag;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _launcher = GetComponent<BallLauncher>();
        _defaultLinearDrag = _rb.drag;
        _defaultAngularDrag = _rb.angularDrag;
    }

    private void OnCollisionEnter(Collision c)
    {
        if (IsGround(c.collider.gameObject))
        {
            _groundContacts++;
            ApplyGroundDrag();
        }
    }

    private void OnCollisionExit(Collision c)
    {
        if (IsGround(c.collider.gameObject))
        {
            _groundContacts = Mathf.Max(0, _groundContacts - 1);
            if (_groundContacts == 0) RestoreDrag();
        }
    }

    private void FixedUpdate()
    {
        if (_groundContacts > 0)
        {
            bool slowLin = _rb.velocity.sqrMagnitude < (restSpeed * restSpeed);
            bool slowAng = _rb.angularVelocity.sqrMagnitude < (restAngSpeed * restAngSpeed);

            if (slowLin && slowAng)
            {
                _restTimer += Time.fixedDeltaTime;
                if (_restTimer >= restTime)
                {
                    // Fully stop and prepare next shot
                    _rb.velocity = Vector3.zero;
                    _rb.angularVelocity = Vector3.zero;
                    _rb.Sleep();
                    _launcher?.PrepareNextShot();
                    _restTimer = 0f;
                }
            }
            else _restTimer = 0f;
        }
        else
        {
            _restTimer = 0f;
        }
    }

    private void ApplyGroundDrag()
    {
        _rb.drag = groundLinearDrag;
        _rb.angularDrag = groundAngularDrag;
    }

    private void RestoreDrag()
    {
        _rb.drag = _defaultLinearDrag;
        _rb.angularDrag = _defaultAngularDrag;
    }

    private bool IsGround(GameObject go) => (groundMask.value & (1 << go.layer)) != 0;
}
