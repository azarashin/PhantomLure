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
        [SerializeField] private bool _addPlayerTag = true;

        [Header("Optional Debug")]
        [SerializeField] private bool _addPathPointBuffer = true;

        private class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                // Transform を持つ Entity として生成
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                if (authoring._addPlayerTag)
                {
                    AddComponent<PlayerTag>(entity);
                }

                // 経路探索結果を受けるバッファを先に付けておくと扱いやすい
                if (authoring._addPathPointBuffer)
                {
                    AddBuffer<PathPoint>(entity);
                }
            }
        }
    }
}