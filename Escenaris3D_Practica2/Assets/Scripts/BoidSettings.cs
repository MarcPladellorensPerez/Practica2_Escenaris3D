using UnityEngine;

[CreateAssetMenu(menuName = "Flocking/BoidSettings")]
public class BoidSettings : ScriptableObject
{
    [Header("Conteo")]
    public int numBoids = 100;

    [Header("Vecindad")]
    public float neighbourDistance = 3.5f;
    public float separationDistance = 1.2f;

    [Header("Pesos (blending)")]
    public float wSeparation = 1.6f;
    public float wAlignment = 1.0f;
    public float wCohesion = 1.1f;
    public float wLeader = 1.25f;
    public float wAvoidance = 2.0f;
    public float wBounds = 0.6f;

    [Header("Velocidades")]
    public float minSpeed = 2.0f;
    public float maxSpeed = 6.0f;
    public float rotationSpeed = 6.0f;

    [Header("Frecuencia de reglas")]
    [Tooltip("Segundos entre recomputar reglas (no cada frame, ver notas del PDF).")]
    public float rulesInterval = 0.1f;

    [Header("Evitación simple (raycasts)")]
    public float rayLength = 2.0f;
    public float sideRayAngle = 25f;
    public LayerMask avoidLayers;

    [Header("Límites (caja)")]
    public Vector3 boundsCenter = Vector3.zero;
    public Vector3 boundsExtents = new Vector3(20, 20, 20);
}
