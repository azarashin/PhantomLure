using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PhantomLure.ECS
{
    public class LureAuthoring : MonoBehaviour
    {
        public float Radius = 12f;
        public float Attract = 8f;    // 囮の誘引（スコア加点）
        public float Lifetime = 8f;
        public float ReliabilityHalfLife = 3f; // 時間で信頼度が落ちる
    }

    public struct Lure : IComponentData
    {
        public float Radius;
        public float Attract;
        public float Lifetime;
        public float Age;
        public float ReliabilityHalfLife;
    }

    public class LureBaker : Baker<LureAuthoring>
    {
        public override void Bake(LureAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<LureTag>(e);
            AddComponent(e, new Lure
            {
                Radius = authoring.Radius,
                Attract = authoring.Attract,
                Lifetime = authoring.Lifetime,
                Age = 0f,
                ReliabilityHalfLife = math.max(0.001f, authoring.ReliabilityHalfLife)
            });
        }
    }
}
