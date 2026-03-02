using PhantomLure.ECS;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class SectorGizmoDrawer : MonoBehaviour
{
    public bool drawSectors = true;
    public bool drawSpawnPoints = true;
    public float spawnRadius = 0.3f;

    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null || !world.IsCreated)
        {
            return;
        }

        var em = world.EntityManager;

        // Sector
        if (drawSectors)
        {
            var q = em.CreateEntityQuery(
                ComponentType.ReadOnly<SectorTag>(),
                ComponentType.ReadOnly<SectorBounds>());

            using var sectors = q.ToEntityArray(Unity.Collections.Allocator.Temp);
            foreach (var e in sectors)
            {
                var b = em.GetComponentData<SectorBounds>(e);
                DrawRectXZ(b.Min, b.Max);
            }
        }

        // SpawnPoints
        if (drawSpawnPoints)
        {
            var q = em.CreateEntityQuery(
                ComponentType.ReadOnly<SectorTag>(),
                ComponentType.ReadOnly<SpawnPoint>());

            using var sectors = q.ToEntityArray(Unity.Collections.Allocator.Temp);
            foreach (var e in sectors)
            {
                var buf = em.GetBuffer<SpawnPoint>(e);
                for (int i = 0; i < buf.Length; i++)
                    Gizmos.DrawSphere(buf[i].Position, spawnRadius);
            }
        }
    }

    static void DrawRectXZ(float2 min, float2 max)
    {
        var a = new Vector3(min.x, 0, min.y);
        var b = new Vector3(max.x, 0, min.y);
        var c = new Vector3(max.x, 0, max.y);
        var d = new Vector3(min.x, 0, max.y);

        Gizmos.DrawLine(a, b);
        Gizmos.DrawLine(b, c);
        Gizmos.DrawLine(c, d);
        Gizmos.DrawLine(d, a);
    }
}