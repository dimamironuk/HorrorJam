using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShelfCollapseSequence : MonoBehaviour
{
    public enum DirectionMode { FixedRight, TowardAisleCenter }

    [Header("Aisle")]
    [SerializeField] private Transform aisleCenter;
    [SerializeField] private DirectionMode directionMode = DirectionMode.FixedRight;
    [SerializeField] private bool invertDirection = false;

    [Header("Crates")]
    [SerializeField] private List<Rigidbody> allShelfCrates = new List<Rigidbody>();
    [SerializeField] private bool useRandomSubset = true;
    [SerializeField, Min(1)] private int cratesToDropCount = 6;

    [Header("Timing")]
    [SerializeField] private Vector2 dropIntervalRange = new Vector2(0.5f, 1.5f);

    [Header("Launch")]
    [SerializeField] private float pushSpeed = 3f;
    [SerializeField] private float upSpeed = 0.3f;
    [SerializeField] private float maxSpin = 2f;
    [SerializeField] private float alongAisleJitter = 0.3f;

    [Header("Anti-Stuck")]
    [SerializeField] private float clearDistance = 1.1f;
    [SerializeField] private float checkInterval = 0.3f;
    [SerializeField] private float minProgressPerCheck = 0.2f;
    [SerializeField] private int maxRescueAttempts = 3;
    [SerializeField] private float releaseTimeout = 3f;
    [SerializeField] private float rescueNudgeSideways = 0.05f;
    [SerializeField] private float rescueNudgeUp = 0.01f;
    [SerializeField] private float reenableTimeout = 1f;
    [SerializeField] private LayerMask solidMask = 0;
    [SerializeField] private float supportTolerance = 0.08f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip collapseClip;
    [SerializeField] private float soundDelay = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool enableTestKey = true;
    [SerializeField] private KeyCode testKey = KeyCode.K;

    private const float MaxHeadroomCheck = 5f;

    private PhysicMaterial _slick;
    private bool _isRunning;

    private void Awake()
    {
        _slick = new PhysicMaterial("SlickCrate")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            bounciness = 0f,
            frictionCombine = PhysicMaterialCombine.Minimum
        };
    }

    private void Update()
    {
        if (enableTestKey && Input.GetKeyDown(testKey))
            TriggerCollapse();
    }

    [ContextMenu("Trigger Collapse")]
    public void TriggerCollapse()
    {
        if (_isRunning) return;
        _isRunning = true;
        StartCoroutine(CollapseRoutine());
    }

    private IEnumerator CollapseRoutine()
    {
        if (audioSource != null && collapseClip != null)
            StartCoroutine(PlayCollapseSound());

        List<Rigidbody> selected = GetCratesToDrop();
        int remaining = selected.Count;

        for (int i = 0; i < selected.Count; i++)
        {
            Rigidbody rb = selected[i];

            if (rb == null)
            {
                remaining--;
                continue;
            }

            StartCoroutine(DropCrate(rb, () => remaining--));

            if (i < selected.Count - 1)
            {
                float pause = Random.Range(
                    Mathf.Min(dropIntervalRange.x, dropIntervalRange.y),
                    Mathf.Max(dropIntervalRange.x, dropIntervalRange.y));
                yield return new WaitForSeconds(pause);
            }
        }

        while (remaining > 0) yield return null;
        _isRunning = false;
    }

    private IEnumerator PlayCollapseSound()
    {
        yield return new WaitForSeconds(soundDelay);
        audioSource.PlayOneShot(collapseClip);
    }

    private IEnumerator DropCrate(Rigidbody rb, System.Action onDone)
    {
        Collider[] crateCols = rb.GetComponentsInChildren<Collider>();
        Vector3 dir = GetDirection(rb.position);
        Vector3 startPos = rb.position;
        float headroom = GetHeadroom(crateCols);
        float travel = GetSafeTravel(crateCols, dir);

        var originalMats = new PhysicMaterial[crateCols.Length];
        for (int i = 0; i < crateCols.Length; i++)
        {
            originalMats[i] = crateCols[i].sharedMaterial;
            crateCols[i].sharedMaterial = _slick;
        }

        List<Collider> blockers = FindBlockers(rb, crateCols, dir, travel);
        SetIgnore(crateCols, blockers, true);

        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.WakeUp();
        Launch(rb, dir, 1f, headroom);

        var wait = new WaitForFixedUpdate();
        float elapsed = 0f, checkTimer = 0f, lastProgress = 0f;
        int attempts = 0;

        while (elapsed < releaseTimeout)
        {
            yield return wait;
            elapsed += Time.fixedDeltaTime;
            checkTimer += Time.fixedDeltaTime;

            float progress = Vector3.Dot(rb.position - startPos, dir);
            if (progress >= travel) break;

            if (checkTimer >= checkInterval)
            {
                checkTimer = 0f;
                if (progress - lastProgress < minProgressPerCheck && attempts < maxRescueAttempts)
                {
                    attempts++;
                    Rescue(rb, dir, attempts, headroom, travel, startPos);
                }
                lastProgress = progress;
            }
        }

        if (Vector3.Dot(rb.position - startPos, dir) < travel)
        {
            rb.position = startPos + dir * travel + Vector3.up * Mathf.Min(0.05f, headroom * 0.5f);
            rb.WakeUp();
        }

        float t = 0f;
        while (t < reenableTimeout && StillOverlapping(crateCols, blockers))
        {
            yield return wait;
            t += Time.fixedDeltaTime;
        }

        SetIgnore(crateCols, blockers, false);
        for (int i = 0; i < crateCols.Length; i++)
            if (crateCols[i] != null) crateCols[i].sharedMaterial = originalMats[i];

        onDone?.Invoke();
    }

    private void Launch(Rigidbody rb, Vector3 dir, float speedMultiplier, float headroom)
    {
        Vector3 alongAisle = Vector3.Cross(Vector3.up, dir) *
                             Random.Range(-alongAisleJitter, alongAisleJitter);

        float up = headroom < 0.15f ? 0f : upSpeed;
        Vector3 v = dir * (pushSpeed * speedMultiplier) + Vector3.up * up + alongAisle;

        rb.AddForce(v, ForceMode.VelocityChange);
        rb.AddTorque(Random.insideUnitSphere * maxSpin, ForceMode.VelocityChange);
    }

    private void Rescue(Rigidbody rb, Vector3 dir, int attempt, float headroom, float travel, Vector3 startPos)
    {
        float up = Mathf.Min(rescueNudgeUp * attempt, headroom * 0.5f);
        float side = rescueNudgeSideways * attempt;
        float progress = Vector3.Dot(rb.position - startPos, dir);
        side = Mathf.Min(side, Mathf.Max(0f, travel - progress));

        rb.position += dir * side + Vector3.up * up;
        rb.WakeUp();
        Launch(rb, dir, 1f + 0.5f * attempt, headroom);
    }

    private Vector3 GetDirection(Vector3 cratePos)
    {
        Transform c = aisleCenter != null ? aisleCenter : transform;

        Vector3 lateral = c.right;
        lateral.y = 0f;
        lateral.Normalize();

        Vector3 dir = lateral;

        if (directionMode == DirectionMode.TowardAisleCenter)
        {
            float side = Vector3.Dot(c.position - cratePos, lateral);
            dir = lateral * (side >= 0f ? 1f : -1f);
        }

        return invertDirection ? -dir : dir;
    }

    private float GetSafeTravel(Collider[] cols, Vector3 dir)
    {
        if (cols.Length == 0 || solidMask.value == 0) return clearDistance;

        Bounds b = GetBounds(cols);
        if (Physics.BoxCast(b.center, b.extents * 0.9f, dir, out RaycastHit hit,
                            Quaternion.identity, clearDistance, solidMask,
                            QueryTriggerInteraction.Ignore))
            return Mathf.Max(0.05f, hit.distance - 0.02f);

        return clearDistance;
    }

    private float GetHeadroom(Collider[] cols)
    {
        if (cols.Length == 0) return MaxHeadroomCheck;

        Bounds b = GetBounds(cols);
        Vector3 origin = new Vector3(b.center.x, b.max.y + 0.01f, b.center.z);

        if (Physics.Raycast(origin, Vector3.up, out RaycastHit hit, MaxHeadroomCheck,
                            Physics.AllLayers, QueryTriggerInteraction.Ignore))
            return hit.distance;

        return MaxHeadroomCheck;
    }

    private List<Collider> FindBlockers(Rigidbody rb, Collider[] ownCols, Vector3 dir, float travel)
    {
        var result = new List<Collider>();
        if (ownCols.Length == 0) return result;

        Bounds b = GetBounds(ownCols);

        Vector3 center = b.center + dir * (travel * 0.5f);
        Vector3 half = b.extents * 0.95f +
                       new Vector3(Mathf.Abs(dir.x), 0f, Mathf.Abs(dir.z)) * (travel * 0.5f);

        Collider[] hits = Physics.OverlapBox(center, half, Quaternion.identity,
                                             Physics.AllLayers, QueryTriggerInteraction.Ignore);

        foreach (Collider c in hits)
        {
            if (c.attachedRigidbody == rb) continue;
            if (System.Array.IndexOf(ownCols, c) >= 0) continue;
            if (((1 << c.gameObject.layer) & solidMask.value) != 0) continue;

            if (c.bounds.max.y <= b.min.y + supportTolerance) continue;
            if (c.bounds.min.y >= b.max.y - supportTolerance) continue;

            result.Add(c);
        }
        return result;
    }

    private static Bounds GetBounds(Collider[] cols)
    {
        Bounds b = cols[0].bounds;
        for (int i = 1; i < cols.Length; i++) b.Encapsulate(cols[i].bounds);
        return b;
    }

    private static void SetIgnore(Collider[] crateCols, List<Collider> blockers, bool ignore)
    {
        foreach (Collider cc in crateCols)
        {
            if (cc == null) continue;
            foreach (Collider bc in blockers)
                if (bc != null) Physics.IgnoreCollision(cc, bc, ignore);
        }
    }

    private static bool StillOverlapping(Collider[] crateCols, List<Collider> blockers)
    {
        foreach (Collider cc in crateCols)
        {
            if (cc == null) continue;
            foreach (Collider bc in blockers)
                if (bc != null && cc.bounds.Intersects(bc.bounds)) return true;
        }
        return false;
    }

    private List<Rigidbody> GetCratesToDrop()
    {
        var list = new List<Rigidbody>(allShelfCrates);

        for (int i = 0; i < list.Count; i++)
        {
            int j = Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }

        if (!useRandomSubset || cratesToDropCount >= list.Count) return list;
        return list.GetRange(0, Mathf.Clamp(cratesToDropCount, 1, list.Count));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        foreach (Rigidbody rb in allShelfCrates)
        {
            if (rb == null) continue;
            Vector3 d = GetDirection(rb.position);
            Vector3 end = rb.position + d * clearDistance;
            Gizmos.DrawLine(rb.position, end);
            Gizmos.DrawSphere(end, 0.05f);
        }
    }
}