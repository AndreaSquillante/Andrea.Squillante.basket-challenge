using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

[RequireComponent(typeof(Rigidbody))]
public sealed class BallSurfaceResponse : MonoBehaviour
{
    [Header("Ground detection")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundLinearDrag = 0.15f;
    [SerializeField] private float groundAngularDrag = 0.25f;

    [Header("Refs")]
    [SerializeField] private BasketShotDetector basketDetector;
    [SerializeField] private ShootingPositionsManager positionsManager;
    [SerializeField] private BallOwner owner;

    private Rigidbody _rb;
    private BallLauncher _launcher;
    private int _groundContacts;
    private float _defaultLinearDrag;
    private float _defaultAngularDrag;
    private bool _basketScored;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _launcher = GetComponent<BallLauncher>();
        _defaultLinearDrag = _rb.drag;
        _defaultAngularDrag = _rb.angularDrag;
    }

    public void NotifyBasketScored()
    {
        _basketScored = true;
    }

    private void OnCollisionEnter(Collision c)
    {
        if (IsGround(c.collider.gameObject))
        {
            _groundContacts++;
            ApplyGroundDrag();

            if (!_basketScored && basketDetector != null)
            {
                if (!_basketScored && basketDetector != null && owner != null)
                    basketDetector.RegisterMiss(owner.TeamId);
            }

            // --- Respawn immediato ---
            RespawnBall();
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
    public void HardResetForNewMatch()
    {
        _basketScored = false;
        _groundContacts = 0;
        RestoreDrag();
        var rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }
    }

    private void RespawnBall()
    {
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.Sleep();

        if (positionsManager != null && _launcher != null)
        {
            Transform nextPos = positionsManager.GetNextPosition();
            if (nextPos != null)
            {
                _launcher.SetShotOrigin(nextPos);
            }
        }

        _launcher?.PrepareNextShot();
        _basketScored = false;
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
