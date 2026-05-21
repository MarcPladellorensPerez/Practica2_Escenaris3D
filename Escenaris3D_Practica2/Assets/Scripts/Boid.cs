using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Boid : MonoBehaviour
{
    [HideInInspector] public FlockManager manager;

    Rigidbody rb;
    Vector3 velocity;
    Vector3 desiredDir = Vector3.forward;
    float speed;

    float rulesTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody>(); // <-- cachea
        speed = Random.Range(manager.settings.minSpeed, manager.settings.maxSpeed);
        velocity = transform.forward * speed;

    }

    void Update()
    {
        // Recalcular reglas a menor frecuencia (PDF: “Rules should not be calculated every frame”)
        rulesTimer += Time.deltaTime;
        if (rulesTimer >= manager.settings.rulesInterval)
        {
            rulesTimer = 0f;
            desiredDir = ComputeSteering(); // combinación por blending
        }

        // Rotación suave (Slerp) + avance (como en el PDF)
        if (desiredDir.sqrMagnitude > 1e-4f)
        {
            Quaternion targetRot = Quaternion.LookRotation(desiredDir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, manager.settings.rotationSpeed * Time.deltaTime);
        }

        // Mantener velocidad en [min, max]
        speed = Mathf.Clamp(velocity.magnitude, manager.settings.minSpeed, manager.settings.maxSpeed);
        velocity = transform.forward * speed;
        rb.MovePosition(rb.position + transform.forward * speed * Time.deltaTime);
    }

    Vector3 ComputeSteering()
    {
        var s = manager.settings;

        // === Vecindad: igual que el pseudocódigo del PDF, iterando allBoids ===
        Vector3 cohesion = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        Vector3 separation = Vector3.zero;
        int n = 0;

        foreach (var other in manager.allBoids)
        {
            if (other == this) continue;
            float d = Vector3.Distance(other.transform.position, transform.position);
            if (d <= s.neighbourDistance)
            {
                cohesion += other.transform.position;
                alignment += other.transform.forward;
                // separación con 1/d^2 como en el PDF
                separation += (transform.position - other.transform.position) / Mathf.Max(d * d, 0.0001f);
                n++;
            }
        }

        if (n > 0)
        {
            cohesion = ((cohesion / n) - transform.position).normalized;
            alignment = (alignment / n).normalized;
        }

        // === Líder (atracción opcional) ===
        Vector3 toLeader = Vector3.zero;
        if (manager.followLeader && manager.leader != null)
        {
            toLeader = (manager.leader.position - transform.position).normalized;
        }

        // === Límites (caja) ===
        Vector3 boundsForce = Vector3.zero;
        Vector3 local = transform.position - manager.BoundsCenter;
        Vector3 ext = manager.BoundsExtents;
        // empuje hacia centro cuando se acerca a los límites (falloff)
        if (Mathf.Abs(local.x) > ext.x * 0.9f) boundsForce.x = -Mathf.Sign(local.x);
        if (Mathf.Abs(local.y) > ext.y * 0.9f) boundsForce.y = -Mathf.Sign(local.y);
        if (Mathf.Abs(local.z) > ext.z * 0.9f) boundsForce.z = -Mathf.Sign(local.z);

        // === Evitación simple de obstáculos con raycasts ===
        Vector3 avoid = ObstacleAvoidance();

        // === Blending (suma ponderada), como recomienda el PDF ===
        Vector3 dir =
              cohesion * s.wCohesion
            + alignment * s.wAlignment
            + separation * s.wSeparation
            + toLeader * s.wLeader
            + boundsForce * s.wBounds
            + avoid * s.wAvoidance;

        if (dir == Vector3.zero) dir = transform.forward;
        return dir.normalized;
    }

    Vector3 ObstacleAvoidance()
    {
        var s = manager.settings;
        float rayLen = s.rayLength;
        float sideAng = s.sideRayAngle;
        int mask = s.avoidLayers;

        RaycastHit hit;

        // Ray frontal
        if (Physics.Raycast(transform.position, transform.forward, out hit, rayLen, mask))
        {
            // intenta desviar a un lado
            Vector3 right = Quaternion.Euler(0, sideAng, 0) * transform.forward;
            Vector3 left = Quaternion.Euler(0, -sideAng, 0) * transform.forward;

            bool rightClear = !Physics.Raycast(transform.position, right, rayLen * 0.7f, mask);
            bool leftClear = !Physics.Raycast(transform.position, left, rayLen * 0.7f, mask);

            if (rightClear && !leftClear) return right.normalized;
            if (leftClear && !rightClear) return left.normalized;
            if (rightClear && leftClear) return (right + left).normalized;

            // fallback correcto: REFLEJAR o usar la normal (no -normal)
            return Vector3.Reflect(transform.forward, hit.normal).normalized; // <-- FIX
                                                                              // o: return hit.normal.normalized;
        }

        return Vector3.zero;
    }


    // (Opcional) pequeña variación aleatoria para “enriquecer” el comportamiento
    void LateUpdate()
    {
        velocity += Random.insideUnitSphere * 0.03f;
    }
}
