using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PhantomLure.ECS
{
    /// <summary>
    /// ユーザの入力を読み込む
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class PlayerInputSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<PlayerTag>();
            RequireForUpdate<PlayerCommandData>();
        }

        protected override void OnUpdate()
        {
            float2 move = ReadMoveInput();

            byte clickRequested = 0;
            float3 clickWorldPosition = default;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (TryGetMouseWorldPosition(out clickWorldPosition))
                {
                    clickRequested = 1;
                }
            }

            foreach (var command in SystemAPI.Query<RefRW<PlayerCommandData>>().WithAll<PlayerTag>())
            {
                command.ValueRW.Move = move;
                command.ValueRW.ClickRequested = clickRequested;

                if (clickRequested != 0)
                {
                    command.ValueRW.ClickWorldPosition = clickWorldPosition;
                }
            }
        }

        private static float2 ReadMoveInput()
        {
            float2 move = float2.zero;

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                {
                    move.y += 1f;
                }

                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                {
                    move.y -= 1f;
                }

                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                {
                    move.x -= 1f;
                }

                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                {
                    move.x += 1f;
                }
            }

            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                float2 stick = gamepad.leftStick.ReadValue();
                move += stick;
            }

            float lenSq = math.lengthsq(move);
            if (lenSq > 1f)
            {
                move = math.normalize(move);
            }

            return move;
        }

        private bool TryGetMouseWorldPosition(out float3 worldPos)
        {
            worldPos = default;

            var camera = Camera.main;
            var mouse = Mouse.current;

            if (camera == null || mouse == null)
            {
                return false;
            }

            var ray = camera.ScreenPointToRay(mouse.position.ReadValue());

            int layerMask = ~0;
            float fallbackGroundY = 0f;

            if (SystemAPI.TryGetSingleton<PlayerInputConfig>(out var config))
            {
                layerMask = config.GroundLayerMask;
                fallbackGroundY = config.FallbackGroundY;
            }

            if (Physics.Raycast(ray, out var hit, 10000f, layerMask, QueryTriggerInteraction.Ignore))
            {
                worldPos = hit.point;
                return true;
            }

            var plane = new Plane(Vector3.up, new Vector3(0f, fallbackGroundY, 0f));
            if (plane.Raycast(ray, out float enter))
            {
                worldPos = ray.GetPoint(enter);
                return true;
            }

            return false;
        }
    }
}
