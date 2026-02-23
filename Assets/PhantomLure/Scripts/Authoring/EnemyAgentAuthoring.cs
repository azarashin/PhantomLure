using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

namespace PhantomLure.ECS
{
    public class EnemyAgentAuthoring : MonoBehaviour
    {
        [Header("Movement")]
        public float BaseSpeed = 4f;
        public float SeparationRadius = 1.2f;
        public float SeparationWeight = 1.0f;

        [Header("Squad")]
        public int SquadId = 0;

        [Header("Decision")]
        public float DecisionInterval = 1.0f; // スコアリング更新周期（遅いほど鈍い）
        public float SightRadius = 18f;
        public float DetachmentRatio = 0.35f; // 囮に釣られる割合（分遣）
    }

    public struct EnemyAgent : IComponentData
    {
        public float BaseSpeed;
        public float SeparationRadius; // この範囲より他の敵が小さいと反発する
        public float SeparationWeight; // 他の敵に対する反発力
    }

    public struct SquadMember : IComponentData
    {
        public int SquadId; // 隊番号
        public byte IsDetached; // 0=本隊、1=分遣（囮追跡など）
    }

    public struct EnemyDecision : IComponentData
    {
        public float DecisionInterval; // 次の判断をするまでの時間（秒）
        public float NextDecisionTime; // 次に判断する時刻
        public float SightRadius; // 視界の範囲。偽の目標地点がこれより近いかどうかで重みが変わる。
        public float DetachmentRatio; // 部隊を分割する確率

        public Entity CurrentTarget;
        public byte TargetIsLure; // Objective or Lure(0の時本物、1の時偽物)
    }

    public class EnemyAgentBaker : Baker<EnemyAgentAuthoring>
    {
        public override void Bake(EnemyAgentAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<EnemyTag>(e);

            AddComponent(e, new EnemyAgent
            {
                BaseSpeed = authoring.BaseSpeed,
                SeparationRadius = authoring.SeparationRadius,
                SeparationWeight = authoring.SeparationWeight
            });

            AddComponent(e, new SquadMember
            {
                SquadId = authoring.SquadId,
                IsDetached = 0
            });

            AddComponent(e, new EnemyDecision
            {
                DecisionInterval = authoring.DecisionInterval,
                NextDecisionTime = 0f,
                SightRadius = authoring.SightRadius,
                DetachmentRatio = authoring.DetachmentRatio,
                CurrentTarget = Entity.Null,
                TargetIsLure = 0
            });
        }
    }
}
