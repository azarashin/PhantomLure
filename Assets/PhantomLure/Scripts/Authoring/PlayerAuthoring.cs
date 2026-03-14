using Unity.Entities;
using UnityEngine;

namespace PhantomLure.ECS
{
    /// <summary>
    /// Player 用の Authoring
    /// GameObject を Bake して PlayerTag 付き Entity を生成する
    /// </summary>
    public class PlayerAuthoring : MonoBehaviour
    {
        [Header("Bake")]
        public bool AddPlayerTag = true;

        [Header("Optional Debug")]
        public bool AddPathPointBuffer = true;

    }
    public class PlayerAuthoringBaker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            // Transform を持つ Entity として生成
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            if (authoring.AddPlayerTag)
            {
                AddComponent<PlayerTag>(entity);
            }

            // 経路探索結果を受けるバッファを先に付けておくと扱いやすい
            if (authoring.AddPathPointBuffer)
            {
                AddBuffer<PathPoint>(entity);
            }
 
            AddComponent(entity, new PlayerCommandData
            {
                Move = default,
                ClickRequested = 0,
                ClickWorldPosition = default
            });
        }
    }
}