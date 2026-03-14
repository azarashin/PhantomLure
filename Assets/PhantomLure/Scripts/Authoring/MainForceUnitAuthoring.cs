using PhantomLure.ECS;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PhantomLure.Authoring
{
    public class MainForceUnitAuthoring : MonoBehaviour
    {
        [SerializeField] float _moveSpeed = 3.5f;
        [SerializeField] float _stoppingDistance = 0.1f;

        private class Baker : Baker<MainForceUnitAuthoring>
        {
            public override void Bake(MainForceUnitAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent<MainForceTag>(entity);

                AddComponent(entity, new MoveSpeed
                {
                    Value = authoring._moveSpeed
                });

                AddComponent(entity, new MoveTarget
                {
                    Position = float3.zero,
                    StoppingDistance = authoring._stoppingDistance
                });

                AddComponent(entity, new MoveState
                {
                    IsMoving = false
                });
            }
        }
    }
}
