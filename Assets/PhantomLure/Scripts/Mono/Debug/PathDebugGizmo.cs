using PhantomLure.ECS;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PhantomLure.Debugging
{
    /// <summary>
    /// ECS の Grid / PathRequest / PathPoint を Gizmo で可視化する
    /// Scene 上の任意の GameObject に付ける
    /// </summary>
    public class PathDebugGizmo : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField] private bool _drawGrid = true;
        [SerializeField] private bool _drawBlockedCells = true;
        [SerializeField] private bool _drawWalkableCells = false;
        [SerializeField] private bool _drawCellCostText = false;

        [Header("Path")]
        [SerializeField] private bool _drawPaths = true;
        [SerializeField] private bool _drawStartGoal = true;
        [SerializeField] private bool _drawPathPoints = true;

        [Header("Filter")]
        [SerializeField] private bool _onlySelectedEntity = false;
        [SerializeField] private int _selectedEntityIndex = -1;

        [Header("Visual")]
        [SerializeField] private float _cellHeightOffset = 0.02f;
        [SerializeField] private float _pathHeightOffset = 0.15f;
        [SerializeField] private float _pointRadius = 0.12f;
        [SerializeField] private float _startGoalRadius = 0.2f;

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;

            if (World.DefaultGameObjectInjectionWorld == null)
                return;

            var world = World.DefaultGameObjectInjectionWorld;
            if (!world.IsCreated)
                return;

            var em = world.EntityManager;

            if (!TryGetGridEntity(em, out var gridEntity))
                return;

            var grid = em.GetComponentData<GridConfig>(gridEntity);
            var cells = em.GetBuffer<GridCell>(gridEntity);

            if (_drawGrid)
            {
                DrawGrid(grid, cells);
            }

            if (_drawPaths)
            {
                DrawPaths(em);
            }
        }

        private bool TryGetGridEntity(EntityManager em, out Entity gridEntity)
        {
            gridEntity = Entity.Null;

            var query = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
            if (query.IsEmptyIgnoreFilter)
            {
                query.Dispose();
                return false;
            }

            gridEntity = query.GetSingletonEntity();
            query.Dispose();
            return true;
        }

        private void DrawGrid(in GridConfig grid, DynamicBuffer<GridCell> cells)
        {
            float size = grid.CellSize;

            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    int2 cell = new int2(x, y);
                    int index = GridUtility.ToIndex(grid, cell);
                    var c = cells[index];

                    float3 center = GridUtility.ToWorldCenter(grid, cell);
                    Vector3 pos = new Vector3(center.x, center.y + _cellHeightOffset, center.z);
                    Vector3 cubeSize = new Vector3(size, 0.02f, size);

                    bool walkable = c.Walkable != 0;

                    if (walkable)
                    {
                        if (_drawWalkableCells)
                        {
                            Gizmos.color = new Color(0f, 1f, 0f, 0.08f);
                            Gizmos.DrawCube(pos, cubeSize);

                            Gizmos.color = new Color(0f, 0.5f, 0f, 0.25f);
                            Gizmos.DrawWireCube(pos, cubeSize);
                        }
                    }
                    else
                    {
                        if (_drawBlockedCells)
                        {
                            Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
                            Gizmos.DrawCube(pos, cubeSize);

                            Gizmos.color = new Color(0.5f, 0f, 0f, 0.8f);
                            Gizmos.DrawWireCube(pos, cubeSize);
                        }
                    }

#if UNITY_EDITOR
                    if (_drawCellCostText)
                    {
                        UnityEditor.Handles.color = Color.white;
                        UnityEditor.Handles.Label(
                            pos + Vector3.up * 0.03f,
                            $"{c.Cost:0.##}");
                    }
#endif
                }
            }
        }

        private void DrawPaths(EntityManager em)
        {
            using var query = em.CreateEntityQuery(
                ComponentType.ReadOnly<PathRequest>());

            using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);

            int drawCount = 0;

            for (int i = 0; i < entities.Length; i++)
            {
                if (_onlySelectedEntity && i != _selectedEntityIndex)
                    continue;

                var entity = entities[i];
                if (!em.Exists(entity))
                    continue;

                var request = em.GetComponentData<PathRequest>(entity);

                bool ready = em.HasComponent<PathReadyTag>(entity);
                bool failed = em.HasComponent<PathFailedTag>(entity);
                bool hasBuffer = em.HasBuffer<PathPoint>(entity);

                if (_drawStartGoal)
                {
                    DrawStartGoal(request, ready, failed);
                }

                if (hasBuffer)
                {
                    var path = em.GetBuffer<PathPoint>(entity);
                    DrawPathBuffer(path, ready, failed);
                }

#if UNITY_EDITOR
                DrawEntityLabel(i, request, ready, failed, hasBuffer, entity);
#endif
                drawCount++;
            }

#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 2.0f,
                $"PathDebug: entities={entities.Length}, drawn={drawCount}");
#endif
        }

        private void DrawStartGoal(in PathRequest request, bool ready, bool failed)
        {
            Vector3 start = ToVector3(request.StartWorld) + Vector3.up * _pathHeightOffset;
            Vector3 goal = ToVector3(request.GoalWorld) + Vector3.up * _pathHeightOffset;

            Gizmos.color = ready ? Color.cyan : (failed ? Color.red : Color.yellow);
            Gizmos.DrawSphere(start, _startGoalRadius);

            Gizmos.color = ready ? Color.magenta : (failed ? new Color(0.6f, 0f, 0f) : new Color(1f, 0.5f, 0f));
            Gizmos.DrawSphere(goal, _startGoalRadius);

            Gizmos.color = Color.white;
            Gizmos.DrawLine(start, goal);
        }

        private void DrawPathBuffer(DynamicBuffer<PathPoint> path, bool ready, bool failed)
        {
            if (path.Length <= 0)
                return;

            Color lineColor = ready ? Color.green : (failed ? Color.red : Color.yellow);
            Gizmos.color = lineColor;

            Vector3 prev = ToVector3(path[0].Value) + Vector3.up * _pathHeightOffset;

            if (_drawPathPoints)
            {
                Gizmos.DrawSphere(prev, _pointRadius);
            }

            for (int i = 1; i < path.Length; i++)
            {
                Vector3 next = ToVector3(path[i].Value) + Vector3.up * _pathHeightOffset;
                Gizmos.DrawLine(prev, next);

                if (_drawPathPoints)
                {
                    Gizmos.DrawSphere(next, _pointRadius);
                }

                prev = next;
            }
        }

#if UNITY_EDITOR
        private void DrawEntityLabel(
            int index,
            in PathRequest request,
            bool ready,
            bool failed,
            bool hasBuffer,
            Entity entity)
        {
            Vector3 labelPos = ToVector3(request.StartWorld) + Vector3.up * (_pathHeightOffset + 0.4f);

            string status =
                failed ? "FAILED" :
                ready ? "READY" :
                "PENDING";

            string bufferText = hasBuffer ? "PathBuffer=Yes" : "PathBuffer=No";

            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(
                labelPos,
                $"[{index}] Entity({entity.Index}:{entity.Version}) {status} {bufferText}");
        }
#endif

        private static Vector3 ToVector3(float3 v)
        {
            return new Vector3(v.x, v.y, v.z);
        }
    }
}
