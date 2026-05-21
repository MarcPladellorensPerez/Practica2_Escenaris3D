using UnityEngine;

public class LeaderBoid : MonoBehaviour
{
    public enum Mode { Manual, Wander, Waypoints }
    public Mode mode = Mode.Wander;

    [Header("Movimiento")]
    public float speed = 6f;
    public float turnSpeed = 5f;

    [Header("Wander")]
    public float wanderRadius = 3f;
    public float wanderOffset = 5f;
    public float wanderJitter = 1.5f;

    [Header("Waypoints")]
    public Transform[] waypoints;
    public float wpTolerance = 2f;
    public bool loop = true;

    [Header("Manual (click)")]
    public LayerMask clickMask;         // capa del suelo (si la dejas en 0, usará todas)
    public float arriveRadius = 0.4f;    // radio para considerar que llegó
    public float surfaceClearance = 0.5f;

    [Header("Límites (opcional)")]
    public bool confineToBounds = true;
    public Vector3 boundsCenter = Vector3.zero;
    public Vector3 boundsExtents = new Vector3(40, 40, 40);
    public FlockManager manager; // si lo asignas, hereda sus bounds

    // (Opcional) evitación simple de obstáculos para el líder
    [Header("Evitación (opcional)")]
    public bool avoidObstacles = false;
    public LayerMask obstacleMask;
    public float avoidRayLength = 6f;
    public float avoidSideAngle = 35f;

    Vector3 wanderTarget;
    int wpIndex = 0;
    Vector3? manualTarget; // destino para modo Manual

    void Start()
    {
        wanderTarget = transform.position + transform.forward * wanderOffset;

        // Hereda bounds del manager si está asignado
        if (manager != null && confineToBounds)
        {
            boundsCenter = manager.settings.boundsCenter;
            boundsExtents = manager.settings.boundsExtents;
        }

        // Seguridad: si pones Waypoints pero no hay puntos, cae a Wander
        if (mode == Mode.Waypoints && (waypoints == null || waypoints.Length == 0))
            mode = Mode.Wander;
    }

    void Update()
    {
        switch (mode)
        {
            case Mode.Manual: HandleManual(); break;
            case Mode.Wander: DoWander(); break;
            case Mode.Waypoints: DoWaypoints(); break;
        }
    }

    // ----------------------- MANUAL (CLICK) -----------------------
    void HandleManual()
    {
        bool pressed = false;

        // Old Input
        if (Input.GetMouseButtonDown(0)) pressed = true;

        // New Input System (si está activo)
#if ENABLE_INPUT_SYSTEM
    if (UnityEngine.InputSystem.Mouse.current != null &&
        UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
        pressed = true;
#endif

        if (pressed)
        {
            if (Camera.main == null)
            {
                Debug.LogWarning("LeaderBoid: No hay MainCamera. Asigna el tag 'MainCamera' a tu cámara.");
                return;
            }

            Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);

            // Si no asignas clickMask en el inspector, usa todas las capas excepto "Boids"
            int excludeBoids = LayerMask.GetMask("Boids"); // si no existe, devuelve 0
            int fallbackMask = ~excludeBoids;              // todo menos Boids (o todo si 0)
            int mask = (clickMask.value != 0) ? clickMask.value : fallbackMask;

            if (Physics.Raycast(r, out var hit, 5000f, mask))
            {
                Vector3 p = hit.point + hit.normal * surfaceClearance;

                if (confineToBounds)
                {
                    Vector3 min = boundsCenter - boundsExtents;
                    Vector3 max = boundsCenter + boundsExtents;
                    p = new Vector3(
                        Mathf.Clamp(p.x, min.x, max.x),
                        Mathf.Clamp(p.y, min.y, max.y),
                        Mathf.Clamp(p.z, min.z, max.z)
                    );
                }

                manualTarget = p;
                Debug.Log($"LeaderBoid: click HIT en {hit.collider.name}, target = {manualTarget.Value}");
            }
            else
            {
                Debug.Log("LeaderBoid: click sin impacto (¿collider? ¿mask?).");
            }
        }

        if (!manualTarget.HasValue) return;

        Vector3 to = manualTarget.Value - transform.position;
        if (to.sqrMagnitude <= arriveRadius * arriveRadius) { manualTarget = null; return; }

        Vector3 dir = to.normalized;
        if (avoidObstacles) ApplyLeaderAvoidance(ref dir);
        KeepInsideBounds(ref dir);
        RotateAndAdvance(dir);
    }


    // ----------------------- WANDER -----------------------
    void DoWander()
    {
        // Wander 3D: jitter del objetivo en un "círculo" adelantado
        wanderTarget += new Vector3(
            Random.Range(-1f, 1f) * wanderJitter,
            Random.Range(-1f, 1f) * wanderJitter,
            Random.Range(-1f, 1f) * wanderJitter);

        Vector3 ahead = transform.position + transform.forward * wanderOffset;
        Vector3 target = ahead + (wanderTarget - ahead).normalized * wanderRadius;

        Vector3 dir = (target - transform.position).normalized;

        if (avoidObstacles) ApplyLeaderAvoidance(ref dir);
        KeepInsideBounds(ref dir);

        RotateAndAdvance(dir);
    }

    // ----------------------- WAYPOINTS -----------------------
    void DoWaypoints()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            mode = Mode.Wander;
            return;
        }

        Vector3 to = waypoints[wpIndex].position - transform.position;
        if (to.magnitude <= wpTolerance)
        {
            wpIndex++;
            if (wpIndex >= waypoints.Length) wpIndex = loop ? 0 : waypoints.Length - 1;
            return;
        }

        Vector3 dir = to.normalized;

        if (avoidObstacles) ApplyLeaderAvoidance(ref dir);
        KeepInsideBounds(ref dir);

        RotateAndAdvance(dir);
    }

    // ----------------------- Helpers -----------------------
    void RotateAndAdvance(Vector3 dir)
    {
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dir, Vector3.up),
            turnSpeed * Time.deltaTime);

        transform.Translate(Vector3.forward * speed * Time.deltaTime, Space.Self);
    }

    // Empuja suavemente al líder hacia dentro cuando se acerca al borde de la caja
    bool KeepInsideBounds(ref Vector3 dir)
    {
        if (!confineToBounds) return false;

        Vector3 nextPos = transform.position + dir.normalized * speed * Time.deltaTime;
        Vector3 local = nextPos - boundsCenter;
        Vector3 ext = boundsExtents;

        float m = 0.92f; // empieza a corregir al 92% del tamaño
        Vector3 push = Vector3.zero;

        if (Mathf.Abs(local.x) > ext.x * m) push.x = -Mathf.Sign(local.x);
        if (Mathf.Abs(local.y) > ext.y * m) push.y = -Mathf.Sign(local.y);
        if (Mathf.Abs(local.z) > ext.z * m) push.z = -Mathf.Sign(local.z);

        if (push != Vector3.zero)
        {
            Vector3 back = push.normalized;
            dir = Vector3.Slerp(dir.normalized, back, 0.75f);
            return true;
        }
        return false;
    }

    // Evitación simple para el líder (raycast frontal + laterales)
    void ApplyLeaderAvoidance(ref Vector3 dir)
    {
        RaycastHit hit;
        Vector3 fwd = transform.forward;

        if (Physics.Raycast(transform.position, fwd, out hit, avoidRayLength, obstacleMask))
        {
            Vector3 right = Quaternion.Euler(0, avoidSideAngle, 0) * fwd;
            Vector3 left = Quaternion.Euler(0, -avoidSideAngle, 0) * fwd;

            bool rightClear = !Physics.Raycast(transform.position, right, avoidRayLength * 0.7f, obstacleMask);
            bool leftClear = !Physics.Raycast(transform.position, left, avoidRayLength * 0.7f, obstacleMask);

            if (rightClear && !leftClear) { dir = right; return; }
            if (leftClear && !rightClear) { dir = left; return; }
            if (rightClear && leftClear) { dir = (right + left).normalized; return; }

            dir = Vector3.Reflect(fwd, hit.normal).normalized; // fallback
        }
    }

}
