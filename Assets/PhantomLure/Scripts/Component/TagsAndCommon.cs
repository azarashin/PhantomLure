using Unity.Entities;

namespace PhantomLure.ECS
{
    public struct EnemyTag : IComponentData { }
    public struct LureTag : IComponentData { }
    public struct ObjectiveTag : IComponentData { }
    public struct PlayerTag : IComponentData {}
    public struct SectorTag : IComponentData {}
    public struct CoreTag   : IComponentData {}

    /// <summary>FlowFieldや密度など、グリッド運用に必要な動的バッファを持つシングルトン</summary>
    public struct FlowFieldRuntime : IComponentData
    {
        public int TargetCellIndex;     // 現在のflowの目的セル（グリッドindex）
        public float NextRebuildTime;   // 次回再計算時刻
    }

    /// <summary>セルごとのコスト（障害物/地形/密度を合成するための基礎）</summary>
    public struct CellBaseCost : IBufferElementData
    {
        public float Value; // 通常は1.0、障害物なら非常に大きい値など
    }

    /// <summary>セルごとの密度（agents数）</summary>
    public struct CellDensity : IBufferElementData
    {
        public int Count;
    }

    /// <summary>目的地までの距離コスト（Flow Fieldで計算）</summary>
    public struct CellDistance : IBufferElementData
    {
        public int Value; // intで十分（BFS/Dijkstraの簡易版）
    }

    /// <summary>セルごとの進行方向（Flow Field）</summary>
    public struct CellFlowDir : IBufferElementData
    {
        public Unity.Mathematics.float2 Dir; // XZ方向
    }
}
