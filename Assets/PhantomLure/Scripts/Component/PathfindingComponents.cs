using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace PhantomLure.ECS
{
    // -----------------------------
    // Grid 定義
    // -----------------------------

    /// <summary>
    /// グリッド全体の設定
    /// </summary>
    public struct GridConfig : IComponentData
    {
        public int Width;
        public int Height;
        public float CellSize;
        public float3 Origin;   // 左下(0,0)セルのワールド原点
    }

    /// <summary>
    /// 各セルの情報
    /// 1次元配列で Width * Height 個持つ想定
    /// </summary>
    public struct GridCell : IBufferElementData
    {
        public byte Walkable;   // 1 = 通行可, 0 = 通行不可
        public float Cost;      // 基本移動コスト（通常 1）
    }

    // -----------------------------
    // Path Request / Result
    // -----------------------------

    /// <summary>
    /// 経路探索要求
    /// RequestTag が付いた entity を PathfindingSystem が処理する
    /// </summary>
    public struct PathRequest : IComponentData
    {
        public float3 StartWorld;
        public float3 GoalWorld;
    }

    /// <summary>
    /// リクエスト未処理マーク
    /// </summary>
    public struct PathRequestTag : IComponentData { }

    /// <summary>
    /// 経路探索完了マーク
    /// </summary>
    public struct PathReadyTag : IComponentData { }

    /// <summary>
    /// 経路が見つからなかった
    /// </summary>
    public struct PathFailedTag : IComponentData { }

    /// <summary>
    /// ワールド座標の経路点
    /// </summary>
    public struct PathPoint : IBufferElementData
    {
        public float3 Value;
    }

    // -----------------------------
    // 内部用ノード
    // -----------------------------

    public struct AStarNode
    {
        public int ParentIndex;
        public float G;
        public float H;
        public float F => G + H;
        public byte Open;
        public byte Closed;
    }
}
