using PhantomLure.ECS;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PhantomLure.Debugging
{
    /// <summary>
    /// シーン上の Collider / Layer を参照して、
    /// ECS の GridCell.Walkable を実行時に書き換える。
    ///
    /// 判定ルール:
    /// - セル上空から真下に Raycast
    /// - 最初に当たった面の Layer が Walkable なら歩行可
    /// - それ以外なら歩行不可
    /// - 何にも当たらなければ歩行不可
    /// </summary>
    public class PhysicsGridWalkableScanner : MonoBehaviour
    {
        [Header("Layer")]
        [SerializeField] private LayerMask _walkableLayerMask;
        [SerializeField] private LayerMask _raycastLayerMask = ~0;

        [Header("Raycast")]
        [SerializeField] private float _rayStartHeight = 100f;
        [SerializeField] private float _rayDistance = 200f;

        [Header("Sampling")]
        [SerializeField] private bool _scanOnStart = true;
        [SerializeField] private bool _scanOnlyOnce = true;
        [SerializeField] private bool _useMultiSample = false;
        [SerializeField] private float _sampleInset = 0.2f;

        [Header("Cost")]
        [SerializeField] private bool _alsoSetCost = true;
        [SerializeField] private float _walkableCost = 1f;
        [SerializeField] private float _blockedCost = 0f;

        [Header("Debug")]
        [SerializeField] private bool _logSummary = true;
        [SerializeField] private bool _drawRays = false;
        [SerializeField] private Color _walkableRayColor = Color.green;
        [SerializeField] private Color _blockedRayColor = Color.red;
        [SerializeField] private float _debugRayDuration = 5f;

        private bool _scanned;

        private void Start()
        {
            if (_scanOnStart)
            {
                ScanAndApply();
            }
        }

        [ContextMenu("Scan And Apply")]
        public void ScanAndApply()
        {
            if (_scanOnlyOnce && _scanned)
                return;

            if (World.DefaultGameObjectInjectionWorld == null)
            {
                Debug.LogWarning("[PhysicsGridWalkableScanner] Default world is null.");
                return;
            }

            var world = World.DefaultGameObjectInjectionWorld;
            if (!world.IsCreated)
            {
                Debug.LogWarning("[PhysicsGridWalkableScanner] Default world is not created.");
                return;
            }

            var em = world.EntityManager;

            if (!TryGetGridEntity(em, out var gridEntity))
            {
                Debug.LogWarning("[PhysicsGridWalkableScanner] GridConfig entity was not found.");
                return;
            }

            var grid = em.GetComponentData<GridConfig>(gridEntity);

            int walkableCount = 0;
            int blockedCount = 0;

            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    int2 cell = new int2(x, y);
                    bool walkable = EvaluateCell(grid, cell);

                    GridEditUtility.SetWalkable(em, gridEntity, cell, walkable);

                    if (_alsoSetCost)
                    {
                        GridEditUtility.SetCost(
                            em,
                            gridEntity,
                            cell,
                            walkable ? _walkableCost : _blockedCost);
                    }

                    if (walkable) walkableCount++;
                    else blockedCount++;
                }
            }

            _scanned = true;

            if (_logSummary)
            {
                Debug.Log(
                    $"[PhysicsGridWalkableScanner] Scan complete. " +
                    $"Walkable={walkableCount}, Blocked={blockedCount}, " +
                    $"Grid={grid.Width}x{grid.Height}");
            }
        }

        private bool TryGetGridEntity(EntityManager em, out Entity gridEntity)
        {
            gridEntity = Entity.Null;

            using var query = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
            if (query.IsEmptyIgnoreFilter)
                return false;

            gridEntity = query.GetSingletonEntity();
            return true;
        }

        private bool EvaluateCell(in GridConfig grid, int2 cell)
        {
            if (_useMultiSample)
            {
                return EvaluateCellMultiSample(grid, cell);
            }

            float3 center = GridUtility.ToWorldCenter(grid, cell);
            return EvaluatePoint(center);
        }

        private bool EvaluateCellMultiSample(in GridConfig grid, int2 cell)
        {
            float3 center = GridUtility.ToWorldCenter(grid, cell);
            float half = grid.CellSize * 0.5f;
            float inset = math.clamp(_sampleInset, 0f, half);

            float offset = math.max(half - inset, 0f);

            float3 p0 = center + new float3(0f, 0f, 0f);
            float3 p1 = center + new float3(offset, 0f, offset);
            float3 p2 = center + new float3(-offset, 0f, offset);
            float3 p3 = center + new float3(offset, 0f, -offset);
            float3 p4 = center + new float3(-offset, 0f, -offset);

            int walkableHits = 0;
            if (EvaluatePoint(p0)) walkableHits++;
            if (EvaluatePoint(p1)) walkableHits++;
            if (EvaluatePoint(p2)) walkableHits++;
            if (EvaluatePoint(p3)) walkableHits++;
            if (EvaluatePoint(p4)) walkableHits++;

            // 5点中1点でも非Walkableが上にある場所を強く弾きたいなら多数決より厳しめがよい。
            // ここでは「全点WalkableならWalkable」とする。
            return walkableHits == 5;
        }

        private bool EvaluatePoint(float3 worldPoint)
        {
            Vector3 origin = new Vector3(worldPoint.x, worldPoint.y + _rayStartHeight, worldPoint.z);
            Vector3 direction = Vector3.down;

            if (Physics.Raycast(origin, direction, out var hit, _rayDistance, _raycastLayerMask, QueryTriggerInteraction.Ignore))
            {
                int hitLayerMask = 1 << hit.collider.gameObject.layer;
                bool walkable = (_walkableLayerMask.value & hitLayerMask) != 0;

                if (_drawRays)
                {
                    Debug.DrawLine(
                        origin,
                        hit.point,
                        walkable ? _walkableRayColor : _blockedRayColor,
                        _debugRayDuration);
                }

                return walkable;
            }

            if (_drawRays)
            {
                Debug.DrawRay(origin, direction * _rayDistance, _blockedRayColor, _debugRayDuration);
            }

            return false;
        }
    }
}
