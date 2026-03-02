// -----------------------------
// Core（守る対象/拠点など）
// -----------------------------

using Unity.Entities;
using Unity.Mathematics;

namespace PhantomLure.ECS
{
    public struct CoreTag : IComponentData { }

    /// <summary>Core の最低限（HPと所属は Health/FactionData を使う）</summary>
    public struct CoreData : IComponentData
    {
        public float3 PositionHint; // Transformを参照しない処理用（必要なら削除OK）
    }
}
