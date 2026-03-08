using PhantomLure.ECS;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PhantomLure.Debugging
{
    /// <summary>
    /// Inspector / ContextMenu から PathRequest を発行するデバッグ用 MonoBehaviour
    /// 通常 Scene 側の GameObject に付けて使う
    /// </summary>
    public class PathRequestDebugButton : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private bool _usePlayerTag = true;

        [Header("Request")]
        [SerializeField] private Vector3 _startWorld = new Vector3(1f, 0f, 1f);
        [SerializeField] private Vector3 _goalWorld = new Vector3(10f, 0f, 8f);

        [Header("Options")]
        [SerializeField] private bool _overwriteStartWithTargetEntityPosition = false;
        [SerializeField] private bool _clearExistingPathBuffer = true;
        [SerializeField] private bool _logResult = true;

        [ContextMenu("Send Path Request")]
        public void SendPathRequest()
        {
            if (World.DefaultGameObjectInjectionWorld == null)
            {
                Debug.LogWarning("[PathRequestDebugButton] Default world is null.");
                return;
            }

            var world = World.DefaultGameObjectInjectionWorld;
            if (!world.IsCreated)
            {
                Debug.LogWarning("[PathRequestDebugButton] Default world is not created.");
                return;
            }

            var em = world.EntityManager;

            if (!TryGetTargetEntity(em, out var unitEntity))
            {
                Debug.LogWarning("[PathRequestDebugButton] Target entity was not found.");
                return;
            }

            float3 start = (float3)_startWorld;
            float3 goal = (float3)_goalWorld;

            if (_overwriteStartWithTargetEntityPosition)
            {
                if (em.HasComponent<Unity.Transforms.LocalTransform>(unitEntity))
                {
                    var tx = em.GetComponentData<Unity.Transforms.LocalTransform>(unitEntity);
                    start = tx.Position;
                }
                else
                {
                    Debug.LogWarning(
                        "[PathRequestDebugButton] Target entity has no LocalTransform. Using inspector StartWorld.");
                }
            }

            var request = new PathRequest
            {
                StartWorld = start,
                GoalWorld = goal
            };

            if (em.HasComponent<PathRequest>(unitEntity))
            {
                em.SetComponentData(unitEntity, request);
            }
            else
            {
                em.AddComponentData(unitEntity, request);
            }

            if (!em.HasComponent<PathRequestTag>(unitEntity))
            {
                em.AddComponent<PathRequestTag>(unitEntity);
            }

            if (em.HasComponent<PathReadyTag>(unitEntity))
            {
                em.RemoveComponent<PathReadyTag>(unitEntity);
            }

            if (em.HasComponent<PathFailedTag>(unitEntity))
            {
                em.RemoveComponent<PathFailedTag>(unitEntity);
            }

            if (em.HasComponent<PathDebugLoggedTag>(unitEntity))
            {
                em.RemoveComponent<PathDebugLoggedTag>(unitEntity);
            }

            if (_clearExistingPathBuffer && em.HasBuffer<PathPoint>(unitEntity))
            {
                var pathBuffer = em.GetBuffer<PathPoint>(unitEntity);
                pathBuffer.Clear();
            }

            if (_logResult)
            {
                Debug.Log(
                    $"[PathRequestDebugButton] Path request sent. " +
                    $"Entity=({unitEntity.Index}:{unitEntity.Version}) " +
                    $"Start={start} Goal={goal}");
            }
        }

        private bool TryGetTargetEntity(EntityManager em, out Entity entity)
        {
            entity = Entity.Null;

            if (_usePlayerTag)
            {
                using var query = em.CreateEntityQuery(ComponentType.ReadOnly<PlayerTag>());
                if (query.IsEmptyIgnoreFilter)
                    return false;

                entity = query.GetSingletonEntity();
                return true;
            }

            return false;
        }
    }
}
