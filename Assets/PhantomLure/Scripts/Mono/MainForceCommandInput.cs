using PhantomLure.ECS;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PhantomLure.Presentation
{
    public class MainForceCommandInput : MonoBehaviour
    {
        [SerializeField]
        Camera _camera;

        [SerializeField]
        LayerMask _groundMask = ~0;

        [SerializeField]
        float _rayDistance = 1000.0f;

        [SerializeField]
        bool _createDebugMarker = true;

        [SerializeField]
        float _debugMarkerLifeTime = 1.0f;

        private EntityManager _entityManager;

        private void Start()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (World.DefaultGameObjectInjectionWorld == null)
            {
                Debug.LogError("Default World が見つかりません。");
                enabled = false;
                return;
            }

            _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        private void Update()
        {
            if (_camera == null)
            {
                return;
            }

            if (Mouse.current == null)
            {
                return;
            }

            if (!Mouse.current.rightButton.wasPressedThisFrame)
            {
                return;
            }

            Vector2 screenPosition = Mouse.current.position.ReadValue();
            Ray ray = _camera.ScreenPointToRay(screenPosition);

            if (!Physics.Raycast(ray, out RaycastHit hit, _rayDistance, _groundMask))
            {
                return;
            }

            Entity commandEntity = _entityManager.CreateEntity();
            _entityManager.AddComponentData(commandEntity, new MainForceMoveCommand
            {
                Destination = hit.point
            });

            if (_createDebugMarker)
            {
                Entity debugEntity = _entityManager.CreateEntity();
                _entityManager.AddComponentData(debugEntity, new MainForceCommandDebug
                {
                    Position = hit.point,
                    LifeTime = _debugMarkerLifeTime
                });
            }
        }
    }
}
