using UnityEngine;
using UnityEngine.InputSystem;

namespace PhantomLure.Mono
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMover : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionReference _moveAction;

        [Header("Move")]
        [SerializeField] private float _moveSpeed = 4.5f;
        [SerializeField] private float _acceleration = 20f;
        [SerializeField] private float _deceleration = 25f;
        [SerializeField] private float _rotationSpeed = 720f;

        [Header("Gravity")]
        [SerializeField] private float _gravity = -20f;
        [SerializeField] private float _groundedStickForce = -2f;

        private CharacterController _controller;
        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            if (_moveAction != null)
            {
                _moveAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (_moveAction != null)
            {
                _moveAction.action.Disable();
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            Vector2 moveInput = ReadMoveInput();
            Vector3 desiredMove = new Vector3(moveInput.x, 0f, moveInput.y);

            // カメラ基準にしたくないのでワールドXZ基準。
            // カメラ基準にしたい場合は Camera.main の forward/right を使って変換。
            if (desiredMove.sqrMagnitude > 1f)
            {
                desiredMove.Normalize();
            }

            Vector3 targetHorizontalVelocity = desiredMove * _moveSpeed;

            float accel = desiredMove.sqrMagnitude > 0.0001f ? _acceleration : _deceleration;
            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetHorizontalVelocity, accel * dt);

            // 接地判定
            if (_controller.isGrounded)
            {
                if (_verticalVelocity < 0f)
                {
                    _verticalVelocity = _groundedStickForce;
                }
            }
            else
            {
                _verticalVelocity += _gravity * dt;
            }

            Vector3 motion = _horizontalVelocity;
            motion.y = _verticalVelocity;

            // CharacterController.Move が衝突を考慮して移動してくれる
            _controller.Move(motion * dt);

            RotateToMoveDirection(dt);
        }

        private Vector2 ReadMoveInput()
        {
            if (_moveAction == null)
            {
                return Vector2.zero;
            }

            return _moveAction.action.ReadValue<Vector2>();
        }

        private void RotateToMoveDirection(float dt)
        {
            Vector3 flatVelocity = _horizontalVelocity;
            flatVelocity.y = 0f;

            if (flatVelocity.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(flatVelocity.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                _rotationSpeed * dt
            );
        }
    }
}