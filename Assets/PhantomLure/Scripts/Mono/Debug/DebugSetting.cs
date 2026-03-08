using PhantomLure.ECS;
using Unity.Entities;
using UnityEngine;

namespace PhantomLure.Debugging
{
    public class DebugSetting : MonoBehaviour
    {
        [SerializeField]
        bool _debugEnabled;

        private bool _wasDebugEnabled;

        void Start()
        {
            SetEnabled(_debugEnabled);
            _wasDebugEnabled = _debugEnabled;
        }

        void Update()
        {
            if (_debugEnabled != _wasDebugEnabled)
            {
                SetEnabled(_debugEnabled);
                _wasDebugEnabled = _debugEnabled;
            }
        }

        /// <summary>
        /// デバッグ機能の有効・無効を切り替える。
        /// Entity の構造変更を伴うので頻繁な呼び出しは避ける。
        /// </summary>
        /// <param name="enabled"></param>
        public static void SetEnabled(bool enabled)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                return;
            }

            var em = world.EntityManager;

            // ゲートが無ければ作る（= ON にするための準備）
            if (!em.CreateEntityQuery(typeof(DebugCountGate)).TryGetSingletonEntity<DebugCountGate>(out var gateEntity))
            {
                gateEntity = em.CreateEntity(typeof(DebugCountGate));
            }

            var q = em.CreateEntityQuery(typeof(DebugCountGate));

            if (enabled)
            {
                if (!q.TryGetSingletonEntity<DebugCountGate>(out _))
                {
                    em.CreateEntity(typeof(DebugCountGate));
                }
            }
            else
            {
                // あれば全部消す（複数できても安全）
                using var arr = q.ToEntityArray(Unity.Collections.Allocator.Temp);
                em.DestroyEntity(arr);
            }
        }

        public static bool IsEnabled()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                return false;
            }

            var em = world.EntityManager;

            var q = em.CreateEntityQuery(typeof(DebugCountGate));
            return q.CalculateEntityCount() > 0;

        }
    }

}
