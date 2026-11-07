using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public sealed class BottleFlipController : MonoBehaviour
{
    public enum Outcome { Land, WobbleThenFall, Miss }
    public event Action<Outcome> OutcomeResolved;

    [Header("Beginner controls")]
    [Tooltip("Tap or hold and release. Automatically aims and spins toward the next platform.")]
    [SerializeField] private bool assistedAim = true;
    [Tooltip("Automatically straightens the bottle after it touches the TOP of the NEXT platform. Dragging cannot spoil an easy flip.")]
    [SerializeField] private bool easyLanding = true;
    [SerializeField, Range(0.08f, 0.4f)] private float landingAssistDuration = 0.18f;
    [SerializeField, Range(0.9f, 1.5f)] private float easyFlightTime = 1.12f;

    [Header("Manual launch (when Assisted Aim is off)")]
    [SerializeField] private float minimumCharge = 0.18f;
    [SerializeField] private float maximumCharge = 1.35f;
    [SerializeField] private float minimumForwardSpeed = 2.8f;
    [SerializeField] private float maximumForwardSpeed = 5.2f;
    [SerializeField] private float minimumUpwardSpeed = 3.0f;
    [SerializeField] private float maximumUpwardSpeed = 4.8f;
    [SerializeField] private float baseSpin = 7.5f;
    [SerializeField] private float dragSpin = 0.055f;

    [Header("Manual landing (when Easy Landing is off)")]
    [SerializeField, Range(0f, 1f)] private float uprightDot = 0.70f;
    [SerializeField] private float settledLinearSpeed = 1.25f;
    [SerializeField] private float settledAngularSpeed = 2.4f;
    [SerializeField] private float resolveDelay = 0.28f;
    [SerializeField] private float missTimeout = 4f;

    private Rigidbody body;
    private CapsuleCollider capsule;
    private Collider targetSurface;
    private Vector3 spawnPosition, targetPosition;
    private Quaternion spawnRotation;
    private bool charging, launched, resolving, hasTarget, ready;
    private bool mouseCharge;
    private float chargeStartedAt, launchedAt;
    private float originalLinearDamping, originalAngularDamping;
    private Vector2 dragAccumulation;
    private string state = "Ready";

    public Rigidbody Body => body;
    public Collider TargetSurface => targetSurface;
    public bool IsCharging => charging;
    public bool IsLaunched => launched;
    public bool CanLaunch => ready && hasTarget && !launched && !resolving;
    public string State => state;
    public bool AssistedAim => assistedAim;
    public bool EasyLanding => easyLanding;
    public float Charge01 => charging ? Mathf.Clamp01((Time.time - chargeStartedAt) / maximumCharge) : 0f;
    public float BaseOffset => (capsule.height * .5f - capsule.center.y) * Mathf.Abs(transform.lossyScale.y);

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.maxAngularVelocity = 40f;
        body.centerOfMass = new Vector3(0f, -.18f, 0f);
        originalLinearDamping = body.linearDamping;
        originalAngularDamping = body.angularDamping;
        body.isKinematic = true;
    }

    private void Update()
    {
        if (CanLaunch) ReadDesktopInput();
        if (launched && !resolving && Time.time - launchedAt > missTimeout) Resolve(Outcome.Miss);
    }

    private void ReadDesktopInput()
    {
        if (!charging && Input.GetKeyDown(KeyCode.Space)) BeginCharge(false);
        if (!charging && Input.GetMouseButtonDown(0) && !BottleFlipGameManager.PointerOverHud()) BeginCharge(true);
        if (!charging) return;
        if (mouseCharge && Input.GetMouseButton(0) && !easyLanding)
            dragAccumulation += new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        if ((mouseCharge && Input.GetMouseButtonUp(0)) || (!mouseCharge && Input.GetKeyUp(KeyCode.Space)))
            Launch(Time.time - chargeStartedAt, dragAccumulation);
    }

    private void BeginCharge(bool fromMouse)
    {
        charging = true;
        mouseCharge = fromMouse;
        chargeStartedAt = Time.time;
        dragAccumulation = Vector2.zero;
        state = "Release to flip";
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused && charging) { charging = false; state = "Ready"; }
    }

    /// <summary>Shared launch entry for keyboard, mouse, or a future XR trigger.</summary>
    public void Launch(float heldFor = .75f, Vector2 spinInput = default)
    {
        if (!CanLaunch) return;
        float charge = Mathf.InverseLerp(minimumCharge, maximumCharge, heldFor);
        body.isKinematic = false;
        body.WakeUp();
        body.linearDamping = originalLinearDamping;
        body.angularDamping = originalAngularDamping;
        float spin;
        if (assistedAim)
        {
            // No damping during an assisted throw: the ballistic calculation then matches PhysX.
            // Hold duration changes the arc height slightly, never the target position.
            body.linearDamping = 0f;
            body.angularDamping = 0f;
            float duration = easyLanding ? easyFlightTime + .08f * charge : Mathf.Lerp(.95f, 1.2f, charge);
            Vector3 delta = targetPosition - body.position;
            body.linearVelocity = delta / duration - .5f * Physics.gravity * duration;
            spin = Mathf.PI * 2f / duration;
        }
        else
        {
            body.linearVelocity = Vector3.forward * Mathf.Lerp(minimumForwardSpeed, maximumForwardSpeed, charge)
                                + Vector3.up * Mathf.Lerp(minimumUpwardSpeed, maximumUpwardSpeed, charge);
            spin = baseSpin;
        }
        if (!easyLanding) spin += Mathf.Clamp(spinInput.x * dragSpin, -3.5f, 3.5f);
        body.angularVelocity = Vector3.right * spin;
        launchedAt = Time.time;
        launched = true;
        ready = false;
        charging = false;
        state = "Flipping";
        BottleFlipAudio.PlayLaunch(body.position);
        BottleFlipHaptics.Pulse(.25f, .08f);
    }

    private void OnCollisionEnter(Collision collision) => InspectContact(collision);
    private void OnCollisionStay(Collision collision) => InspectContact(collision);

    private void InspectContact(Collision collision)
    {
        if (!launched || resolving) return;
        if (collision.collider.CompareTag("Ground") || collision.collider.CompareTag("Hazard"))
        {
            Resolve(Outcome.Miss);
            return;
        }
        // Never award points for hitting the starting pad, a wall, or a different platform.
        if (collision.collider != targetSurface) return;
        bool onTop = false;
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);
            if (contact.normal.y > .5f && contact.point.y >= targetSurface.bounds.max.y - .08f)
                onTop = true;
        }
        if (!onTop) return;
        if (easyLanding) StartCoroutine(SettleOnTarget());
        else StartCoroutine(EvaluateManualLanding());
    }

    private IEnumerator SettleOnTarget()
    {
        resolving = true;
        state = "Landing assist";
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.isKinematic = true;
        Vector3 from = body.position;
        Quaternion rotation = body.rotation;
        Vector3 destination = SurfaceSpawn(targetSurface);
        float elapsed = 0f;
        while (elapsed < landingAssistDuration)
        {
            elapsed += Time.fixedDeltaTime;
            float blend = Mathf.SmoothStep(0f, 1f, elapsed / landingAssistDuration);
            body.MovePosition(Vector3.Lerp(from, destination, blend));
            body.MoveRotation(Quaternion.Slerp(rotation, Quaternion.identity, blend));
            yield return new WaitForFixedUpdate();
        }
        body.position = destination;
        body.rotation = Quaternion.identity;
        Resolve(Outcome.Land);
    }

    private IEnumerator EvaluateManualLanding()
    {
        resolving = true;
        state = "Checking landing";
        yield return new WaitForSeconds(resolveDelay);
        if (IsSettledOnTarget()) { Resolve(Outcome.Land); yield break; }
        state = "Wobbling";
        yield return new WaitForSeconds(.6f);
        Resolve(IsSettledOnTarget() ? Outcome.Land : Outcome.WobbleThenFall);
    }

    private bool IsSettledOnTarget()
    {
        if (targetSurface == null || Vector3.Dot(transform.up, Vector3.up) < uprightDot) return false;
        var ray = new Ray(body.position, Vector3.down);
        return targetSurface.Raycast(ray, out _, BaseOffset + .15f)
            && body.linearVelocity.magnitude <= settledLinearSpeed
            && body.angularVelocity.magnitude <= settledAngularSpeed;
    }

    private void Resolve(Outcome outcome)
    {
        if (!launched) return;
        if (!body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        body.isKinematic = true;
        body.linearDamping = originalLinearDamping;
        body.angularDamping = originalAngularDamping;
        launched = charging = resolving = ready = false;
        state = outcome == Outcome.Land ? "Landed!" : "Try again";
        BottleFlipAudio.PlayOutcome(outcome, body.position);
        BottleFlipHaptics.Pulse(outcome == Outcome.Land ? .65f : .4f, .12f);
        OutcomeResolved?.Invoke(outcome);
    }

    public Vector3 SurfaceSpawn(Collider surface) => new Vector3(surface.bounds.center.x,
        surface.bounds.max.y + BaseOffset + .04f, surface.bounds.center.z);

    public void SetTarget(Collider surface)
    {
        targetSurface = surface;
        hasTarget = surface != null;
        if (hasTarget) targetPosition = SurfaceSpawn(surface);
    }

    public void ClearTarget() { targetSurface = null; hasTarget = ready = false; }

    public void SetSpawn(Vector3 position, Quaternion rotation)
    {
        spawnPosition = position;
        spawnRotation = rotation;
        ResetToSpawn();
    }

    public void ResetToSpawn()
    {
        StopAllCoroutines();
        charging = launched = resolving = false;
        body.isKinematic = false;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.isKinematic = true;
        body.position = spawnPosition;
        body.rotation = spawnRotation;
        body.linearDamping = originalLinearDamping;
        body.angularDamping = originalAngularDamping;
        ready = true;
        state = "Ready";
    }
}
