using UnityEngine;
using UnityEngine.InputSystem;

namespace AdvancedRogueLikeandPuzzleSystem
{
    /// <summary>
    /// Scene-level configuration and input owner. Holds every InputAction the game polls; any
    /// action left unbound in the inspector gets a default keyboard/mouse + gamepad (+ touch)
    /// binding in Awake, built from the KeyCode fields. Also switches between the TPS and FPS
    /// cameras and between the PC and mobile HUD, and validates the scene setup on Start.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public ControllerType controllerType;
        public CameraType cameraType;
        public static GameManager Instance;

        [Header("PC Control Settings: (Ignore here if you select Mobile Controller Type)")]
        public KeyCode Keycode_Interact;
        public KeyCode Keycode_Sprint;
        public KeyCode Keycode_Jump;
        public KeyCode Keycode_CameraSwitch;
        public KeyCode Keycode_Using_Potion_Heal;
        public KeyCode Keycode_Using_Potion_Mana;
        public KeyCode Keycode_Inventory = KeyCode.I;
        public KeyCode Keycode_Map = KeyCode.M;
        public GameObject MainCamera_TPS;
        public GameObject MainCamera_FPS;

        public InputAction moveAction;
        public InputAction sprintAction;
        public InputAction jumpAction;
        public InputAction cameraSwitchAction;
        public InputAction interactAction;
        public InputAction mouseAction;
        public InputAction mouseRightAction;
        public InputAction escapeAction;
        public InputAction usingHealPotionAction;
        public InputAction usingManaPotionAction;
        public InputAction mouseDeltaAction;
        public InputAction mouseScrollAction;
        public InputAction gamepadZoomAction;
        public InputAction inventoryAction;
        public InputAction mapAction;

        public void Awake()
        {
            Instance = this;

            if (moveAction == null || moveAction.bindings.Count == 0)
            {
                moveAction = new InputAction(type: InputActionType.Value, binding: "<Gamepad>/leftStick");
                moveAction.AddCompositeBinding("2DVector")
                    .With("Up", "<Keyboard>/w")
                    .With("Down", "<Keyboard>/s")
                    .With("Left", "<Keyboard>/a")
                    .With("Right", "<Keyboard>/d");
            }
            moveAction.Enable();

            if (mouseAction == null || mouseAction.bindings.Count == 0)
            {
                mouseAction = new InputAction(type: InputActionType.Button);
                mouseAction.AddBinding("<Mouse>/leftButton");
                mouseAction.AddBinding("<Touchscreen>/primaryTouch/press");
                mouseAction.AddBinding("<Gamepad>/rightTrigger");
            }
            mouseAction.Enable();

            if (mouseRightAction == null || mouseRightAction.bindings.Count == 0)
            {
                mouseRightAction = new InputAction(type: InputActionType.Button);
                mouseRightAction.AddBinding("<Mouse>/rightButton");
                mouseRightAction.AddBinding("<Gamepad>/leftTrigger");
            }
            mouseRightAction.Enable();

            if (mouseScrollAction == null || mouseScrollAction.bindings.Count == 0)
            {
                mouseScrollAction = new UnityEngine.InputSystem.InputAction(type: UnityEngine.InputSystem.InputActionType.Value, binding: "<Mouse>/scroll");
            }
            mouseScrollAction.Enable();

            if (gamepadZoomAction == null || gamepadZoomAction.bindings.Count == 0)
            {
                gamepadZoomAction = new InputAction(type: InputActionType.Value, binding: "<Gamepad>/dpad");
            }
            gamepadZoomAction.Enable();

            if (mouseDeltaAction == null || mouseDeltaAction.bindings.Count == 0)
            {
                mouseDeltaAction = new UnityEngine.InputSystem.InputAction(type: UnityEngine.InputSystem.InputActionType.Value, binding: "<Mouse>/delta");
                mouseDeltaAction.AddBinding("<Gamepad>/rightStick");
            }
            mouseDeltaAction.Enable();

            if (escapeAction == null || escapeAction.bindings.Count == 0)
            {
                escapeAction = new UnityEngine.InputSystem.InputAction(type: UnityEngine.InputSystem.InputActionType.Button, binding: "<Keyboard>/escape");
                escapeAction.AddBinding("<Gamepad>/start");
            }
            escapeAction.Enable();

            if (interactAction == null || interactAction.bindings.Count == 0)
            {
                interactAction = new UnityEngine.InputSystem.InputAction(type: UnityEngine.InputSystem.InputActionType.Button, binding: "<Keyboard>/" + Keycode_Interact.ToString());
                interactAction.AddBinding("<Gamepad>/buttonSouth");
            }
            interactAction.Enable();

            if (sprintAction == null || sprintAction.bindings.Count == 0)
            {
                sprintAction = new UnityEngine.InputSystem.InputAction(type: UnityEngine.InputSystem.InputActionType.Button, binding: "<Keyboard>/" + Keycode_Sprint.ToString());
                sprintAction.AddBinding("<Gamepad>/leftStickPress");
            }
            sprintAction.Enable();

            if (jumpAction == null || jumpAction.bindings.Count == 0)
            {
                jumpAction = new UnityEngine.InputSystem.InputAction(type: UnityEngine.InputSystem.InputActionType.Button, binding: "<Keyboard>/" + Keycode_Jump.ToString());
                jumpAction.AddBinding("<Gamepad>/buttonNorth");
            }
            jumpAction.Enable();

            if (cameraSwitchAction == null || cameraSwitchAction.bindings.Count == 0)
            {
                cameraSwitchAction = new UnityEngine.InputSystem.InputAction(type: UnityEngine.InputSystem.InputActionType.Button, binding: "<Keyboard>/" + Keycode_CameraSwitch.ToString());
                cameraSwitchAction.AddBinding("<Gamepad>/buttonEast");
            }
            cameraSwitchAction.Enable();

            if (usingHealPotionAction == null || usingHealPotionAction.bindings.Count == 0)
            {
                usingHealPotionAction = new UnityEngine.InputSystem.InputAction(type: UnityEngine.InputSystem.InputActionType.Button, binding: "<Keyboard>/" + Keycode_Using_Potion_Heal.ToString().Replace("Alpha", ""));
                usingHealPotionAction.AddBinding("<Gamepad>/dpad/left");
            }
            usingHealPotionAction.Enable();

            if (usingManaPotionAction == null || usingManaPotionAction.bindings.Count == 0)
            {
                usingManaPotionAction = new UnityEngine.InputSystem.InputAction(type: UnityEngine.InputSystem.InputActionType.Button, binding: "<Keyboard>/" + Keycode_Using_Potion_Mana.ToString().Replace("Alpha", ""));
                usingManaPotionAction.AddBinding("<Gamepad>/dpad/right");
            }
            usingManaPotionAction.Enable();

            if (inventoryAction == null || inventoryAction.bindings.Count == 0)
            {
                inventoryAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/" + Keycode_Inventory.ToString().Replace("Alpha", ""));
                inventoryAction.AddBinding("<Gamepad>/select");
            }
            inventoryAction.Enable();

            if (mapAction == null || mapAction.bindings.Count == 0)
            {
                mapAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/" + Keycode_Map.ToString().Replace("Alpha", ""));
                mapAction.AddBinding("<Gamepad>/rightStickPress");
            }
            mapAction.Enable();
        }

        private void Update()
        {
            if (cameraSwitchAction != null && cameraSwitchAction.WasReleasedThisFrame())
            {
                SwitchTheCamera();
            }
        }

        private Camera fpsCamera;
        private Camera FPSCamera
        {
            get
            {
                if (fpsCamera == null && MainCamera_FPS != null)
                {
                    fpsCamera = MainCamera_FPS.GetComponent<Camera>();
                }
                return fpsCamera;
            }
        }

        public void ZoomFovIfFPSCameraActive()
        {
            if(cameraType == CameraType.FPS && FPSCamera != null)
            {
                FPSCamera.fieldOfView = 45;
            }
        }

        public void ZoomOutFovIfFPSCameraActive()
        {
            if (cameraType == CameraType.FPS && FPSCamera != null)
            {
                FPSCamera.fieldOfView = 65;
            }
        }

        public void SwitchTheCamera()
        {
            if (cameraType == CameraType.TPS)
            {
                cameraType = CameraType.FPS;
            }
            else
            {
                cameraType = CameraType.TPS;
            }
            ApplyCameraMode();
        }

        private void ApplyCameraMode()
        {
            bool tps = cameraType == CameraType.TPS;

            if (MainCamera_TPS != null) MainCamera_TPS.SetActive(tps);
            if (MainCamera_FPS != null) MainCamera_FPS.SetActive(!tps);

            if (HeroController.instance != null && HeroController.instance.characterHead != null)
            {
                HeroController.instance.characterHead.localScale = tps ? Vector3.one : Vector3.zero;
            }
        }

        private void Start()
        {
            Time.timeScale = 1;

            ValidateSceneSetup();

            if (controllerType == ControllerType.KeyboardMouse)
            {
                if (GameCanvas_Controller.instance != null) GameCanvas_Controller.instance.Configure_For_PCConsole();
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            else if (controllerType == ControllerType.Mobile)
            {
                if (GameCanvas_Controller.instance != null) GameCanvas_Controller.instance.Configure_For_Mobile();
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.Confined;
            }

            ApplyCameraMode();
        }

        private void ValidateSceneSetup()
        {
            if (HeroController.instance == null)
                Debug.LogError("[ARPS] No HeroController in the scene. Add Prefabs/Character/AdvancedTPSPlayer.", this);

            if (GameCanvas_Controller.instance == null)
                Debug.LogError("[ARPS] No GameCanvas_Controller in the scene. Add Prefabs/Managers/GameCanvas.", this);

            if (AudioManager.instance == null)
                Debug.LogError("[ARPS] No AudioManager in the scene. Add Prefabs/Managers/GameManager (the AudioManager lives on it).", this);

            if (MainCamera_TPS == null)
                Debug.LogError("[ARPS] GameManager: 'Main Camera TPS' is not assigned.", this);

            if (MainCamera_FPS == null)
                Debug.LogError("[ARPS] GameManager: 'Main Camera FPS' is not assigned.", this);

            if (controllerType == ControllerType.Mobile && SimpleJoystick.Instance == null)
                Debug.LogError("[ARPS] Controller Type is Mobile but no SimpleJoystick is in the scene. The GameCanvas prefab carries it.", this);
        }

        public void ShakeTheCamera(float amount)
        {
            if (cameraType == CameraType.TPS)
            {
                AdvancedTPSCamera.Instance.fCamShakeImpulse = amount;
            }
            else if (cameraType == CameraType.FPS)
            {
                AdvancedFPSCamera.Instance.fCamShakeImpulse = amount;
            }
        }

        public Transform GetCurrentCamera()
        {
            if (cameraType == CameraType.TPS)
            {
                return MainCamera_TPS.transform;
            }
            else if (cameraType == CameraType.FPS)
            {
                return MainCamera_FPS.transform;
            }
            return null;
        }
    }

    public enum ControllerType
    {
        KeyboardMouse,
        Mobile
    }

    public enum CameraType
    {
        TPS,
        FPS
    }
}