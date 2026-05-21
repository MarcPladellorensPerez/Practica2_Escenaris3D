using System.Collections.Generic;
using UnityEngine;

public class FlockManager : MonoBehaviour
{
    [Header("Settings")]
    public BoidSettings settings;
    public GameObject boidPrefab;

    [Header("Líder")]
    public Transform leader;               // Asignable en escena
    public bool followLeader = true;

    [Header("Spawn")]
    public Vector3 spawnExtents = new Vector3(10, 10, 10);

    [HideInInspector] public List<Boid> allBoids = new List<Boid>();

    void Start()
    {
        if (boidPrefab == null || settings == null)
        {
            Debug.LogError("Faltan BoidPrefab o Settings en FlockManager.");
            enabled = false; return;
        }

        // Instanciación estilo "Flocking Manager" del PDF
        for (int i = 0; i < settings.numBoids; i++)
        {
            Vector3 pos = transform.position + new Vector3(
                Random.Range(-spawnExtents.x, spawnExtents.x),
                Random.Range(-spawnExtents.y, spawnExtents.y),
                Random.Range(-spawnExtents.z, spawnExtents.z));
            Vector3 dir = Random.onUnitSphere; // dir.y = Mathf.Clamp(dir.y, -0.6f, 0.6f);
            var boidGO = Instantiate(boidPrefab, pos, Quaternion.LookRotation(dir, Vector3.up), transform);
            var boid = boidGO.GetComponent<Boid>();
            boid.manager = this;
            allBoids.Add(boid);
        }
    }

    // Acceso rápido desde los Boids
    public Vector3 BoundsCenter => settings.boundsCenter;
    public Vector3 BoundsExtents => settings.boundsExtents;
}
