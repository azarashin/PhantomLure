using PhantomLure.ECS;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PhantomLure.Authoring
{
    public class MainForceUnitAuthoring : MonoBehaviour
    {
        [SerializeField]
        MainForceFormationAnchorAuthoring _anchorAuthoring;

        [SerializeField]
        float _moveSpeed = 3.5f;

        [SerializeField]
        float _stoppingDistance = 0.1f;

        [SerializeField]
        int _formationIndex = 0;

        [SerializeField]
        float _personalSpaceRadius = 0.6f;

        [SerializeField]
        float _neighborRadius = 1.5f;

        [SerializeField]
        float _repulsionStrength = 4.0f;

        [SerializeField]
        float _falloffDistance = 0.35f;

        [SerializeField]
        float _maxRepulsionSpeed = 2.0f;

        private class Baker : Baker<MainForceUnitAuthoring>
        {
            public override void Bake(MainForceUnitAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent<MainForceTag>(entity);

                AddComponent(entity, new MoveSpeed
                {
                    Value = math.max(0.01f, authoring._moveSpeed)
                });

                AddComponent(entity, new MoveTarget
                {
                    Position = authoring.transform.position,
                    StoppingDistance = math.max(0.01f, authoring._stoppingDistance)
                });

                AddComponent(entity, new MoveState
                {
                    IsMoving = false
                });

                AddComponent(entity, new FormationIndex
                {
                    Value = math.max(0, authoring._formationIndex)
                });

                if (authoring._anchorAuthoring != null)
                {
                    Entity anchorEntity = GetEntity(authoring._anchorAuthoring, TransformUsageFlags.Dynamic);

                    AddComponent(entity, new FormationMember
                    {
                        AnchorEntity = anchorEntity
                    });
                }

                AddComponent(entity, new MainForceSocialForceAgent
                {
                    PersonalSpaceRadius = math.max(0.05f, authoring._personalSpaceRadius),
                    NeighborRadius = math.max(authoring._personalSpaceRadius, authoring._neighborRadius),
                    RepulsionStrength = math.max(0.0f, authoring._repulsionStrength),
                    FalloffDistance = math.max(0.01f, authoring._falloffDistance),
                    MaxRepulsionSpeed = math.max(0.0f, authoring._maxRepulsionSpeed)
                });
            }
        }
    }
}
