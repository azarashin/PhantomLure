using Unity.Entities;
using UnityEngine;

namespace PhantomLure.ECS
{
    public sealed class PlayerInputConfigAuthoring : MonoBehaviour
    {
        [SerializeField]
        LayerMask _groundLayerMask = ~0;

        [SerializeField] 
        float _fallbackGroundY = 0f;

        private sealed class Baker : Baker<PlayerInputConfigAuthoring>
        {
            public override void Bake(PlayerInputConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new PlayerInputConfig
                {
                    GroundLayerMask = authoring._groundLayerMask.value,
                    FallbackGroundY = authoring._fallbackGroundY
                });
            }
        }
    }
}
