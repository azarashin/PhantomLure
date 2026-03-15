using PhantomLure.ECS;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PhantomLure.Authoring
{
    public class MainForceFormationAnchorAuthoring : MonoBehaviour
    {
        [SerializeField]
        float _moveSpeed = 4.0f;

        [SerializeField]
        float _arriveDistance = 0.25f;

        [SerializeField]
        int _columnCount = 3;

        [SerializeField]
        float _spacingSide = 1.6f;

        [SerializeField]
        float _spacingBack = 2.0f;

        [SerializeField]
        float _slotCatchUpDistance = 3.0f;

        [SerializeField]
        float _slowDownDistance = 1.25f;

        [SerializeField]
        float _maxCatchUpMultiplier = 1.75f;

        private class Baker : Baker<MainForceFormationAnchorAuthoring>
        {
            public override void Bake(MainForceFormationAnchorAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new MainForceFormationAnchor
                {
                    Position = authoring.transform.position,
                    Forward = math.forward(),
                    Destination = authoring.transform.position,
                    MoveSpeed = authoring._moveSpeed,
                    ArriveDistance = authoring._arriveDistance,
                    IsMoving = false
                });

                AddComponent(entity, new MainForceFormationSettings
                {
                    ColumnCount = math.max(1, authoring._columnCount),
                    SpacingSide = authoring._spacingSide,
                    SpacingBack = authoring._spacingBack,
                    SlotCatchUpDistance = authoring._slotCatchUpDistance,
                    SlowDownDistance = math.max(0.01f, authoring._slowDownDistance),
                    MaxCatchUpMultiplier = math.max(1.0f, authoring._maxCatchUpMultiplier)
                });
            }
        }
    }
}
