using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Halloween2026
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class GhostController : MonoBehaviour
    {
        [SerializeField]
        private GameBootstrapper _game;

        [SerializeField]
        private SettingsService _settings;

        [SerializeField]
        private Camera _camera;

        [SerializeField, Tooltip("Pivot for camera pitch; body rotation supplies yaw.")]
        private Transform _pitchPivot;

        [SerializeField]
        private InputActionReference _moveAction;

        [SerializeField]
        private InputActionReference _lookAction;

        [SerializeField]
        private InputActionReference _primaryAction;

        [SerializeField]
        private InputActionReference _secondaryAction;

        [SerializeField, Min(0.1f)]
        private float _speed = 5f;

        private CharacterController _controller;
        private float _pitch = 12f;

        public event Action<string> ActionRequested;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            _moveAction.action.Enable();
            _lookAction.action.Enable();
            _primaryAction.action.Enable();
            _secondaryAction.action.Enable();
            _primaryAction.action.performed += OnPrimary;
            _secondaryAction.action.performed += OnSecondary;
        }

        private void Start()
        {
            _settings.Changed += ApplySettings;
            ApplySettings();
        }

        private void Update()
        {
            Vector2 movement;
            Vector2 look;
            Vector3 direction;
            float lookScale;

            if (_game.Session.State != SessionState.Running) return;

            movement = _moveAction.action.ReadValue<Vector2>();
            look = _lookAction.action.ReadValue<Vector2>();
            lookScale = _lookAction.action.activeControl?.device is Mouse ? 0.12f : 120f * Time.deltaTime;

            transform.Rotate(0f, look.x * lookScale * _settings.Sensitivity, 0f);
            _pitch = Mathf.Clamp(_pitch - look.y * lookScale * _settings.Sensitivity, -65f, 75f);
            _pitchPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);

            direction = transform.TransformDirection(new Vector3(movement.x, 0f, movement.y));
            direction = Vector3.ClampMagnitude(direction, 1f);
            _controller.Move((direction * _speed + Vector3.down * 2f) * Time.deltaTime);
        }

        private void ApplySettings()
        {
            _camera.fieldOfView = _settings.FieldOfView;
        }

        private void OnPrimary(InputAction.CallbackContext context)
        {
            if (_game.Session.State != SessionState.Running) return;

            ActionRequested?.Invoke("Primary action");
        }

        private void OnSecondary(InputAction.CallbackContext context)
        {
            if (_game.Session.State != SessionState.Running) return;

            ActionRequested?.Invoke("Secondary action");
        }

        private void OnDisable()
        {
            _primaryAction.action.performed -= OnPrimary;
            _secondaryAction.action.performed -= OnSecondary;
            _moveAction.action.Disable();
            _lookAction.action.Disable();
            _primaryAction.action.Disable();
            _secondaryAction.action.Disable();
        }

        private void OnDestroy()
        {
            if (_settings != null) _settings.Changed -= ApplySettings;
        }
    }
}
