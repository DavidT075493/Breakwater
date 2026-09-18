using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;

public class InputManager : MonoBehaviour
{
    public static InputManager instance;

    public PlayerInput input;

    public Vector2 movementInput;
    public Vector2 mousePos;

    public static Dictionary<string, ActionType> actions = new Dictionary<string, ActionType>();

    InputAction aim;

    float lastMouseTime;
    float lastStickTime;

    public float stickRadius = 0.1f;
    public float stickDeadzone = 0.15f;

    public float mouseScroll;

    public float holdDelay = 0.35f;
    public float repeatRate = 0.08f;

    float upHoldTime;
    float downHoldTime;
    float upRepeatTimer;
    float downRepeatTimer;

    public float buildModeCursorSpeed, inventoryCursorSpeed;

    Mouse virtualMouse;
    bool usingVirtualMouse;

    Vector2 joystickScreenOffset;

    public static bool usingController;
    Vector2 dir;

    InputDevice lastDevice;

    /*
    public static InputDevice lastDevice;

    void OnEnable()
    {
        InputSystem.onAnyButtonPress.Call(OnAnyButton);
    }

    void OnAnyButton(InputControl control)
    {
        lastDevice = control.device;
    }
    */

    void OnEnable()
    {
    }

    void Awake()
    {
        if (!instance)
            instance = this;
        else return;

        foreach (InputAction a in input.actions)
            actions.TryAdd(a.name, new ActionType(false, false, false));

        aim = input.actions.FindAction("Look");

        mousePos = new Vector2(Screen.width / 2f, Screen.height / 2f);
        mousePos -= new Vector2(0, 50);

        if (virtualMouse == null)
            virtualMouse = InputSystem.AddDevice<Mouse>();

        InputSystem.EnableDevice(virtualMouse);

        InputState.Change(virtualMouse.position, mousePos);
        InputState.Change(virtualMouse.delta, Vector2.zero);
    }

    void OnMovement(InputValue value)
    {
        movementInput = value.Get<Vector2>();
    }


    void Update()
    {
        InputSystem.onAnyButtonPress.CallOnce(ctrl =>
        {
            lastDevice = ctrl.device;
        });
        if (!player.instance && SceneManager.GetActiveScene().name == "overworld") return;

        foreach (InputAction a in input.actions)
            actions[a.name] = new ActionType(a.WasPressedThisFrame(), a.IsPressed(), a.WasReleasedThisFrame());

        Vector2 stick = Gamepad.current != null
            ? Gamepad.current.rightStick.ReadValue()
            : Vector2.zero;

        //left stick in inventory
        if (MouseCursor.pointerMode && Gamepad.current != null 
            && !(menu.instance && (menu.instance.mapMaximized || menu.instance.waitingForReady)))
        {
            stick = new Vector2(Mathf.Clamp(stick.x + Gamepad.current.leftStick.ReadValue().x, -1, 1),
                Mathf.Clamp(stick.y + Gamepad.current.leftStick.ReadValue().y, -1, 1));
        }

        bool usingMouse = Mouse.current != null &&
                          Mouse.current.delta.ReadValue().sqrMagnitude > 0.01f;

        bool usingStick = stick.magnitude > stickDeadzone;

        if (usingMouse) lastMouseTime = Time.time;
        if (usingStick) lastStickTime = Time.time;

        usingVirtualMouse = lastStickTime > lastMouseTime;

        if (!usingVirtualMouse && Mouse.current != null)
        {
            mousePos = Mouse.current.position.ReadValue();
        }
        else if (usingVirtualMouse && Gamepad.current != null)
        {
            //inventory pointer
            if (MouseCursor.pointerMode)
            {
                Vector2 stickDelta = stick * inventoryCursorSpeed * Time.deltaTime * ForceAspectRatio.GetNormalizedScreenSize(Camera.main);
                mousePos += (Vector2)stickDelta;
            }
            // aim
            else if (!gridDisplay.buildMode)
            {
                Vector2 playerScreenPos =
                    Camera.main.WorldToScreenPoint(player.instance.transform.position);

                if (usingStick)
                {
                    dir = stick;
                    
                    joystickScreenOffset =
                        Vector2.Lerp(
                            joystickScreenOffset,
                            dir.normalized * (stickRadius * ForceAspectRatio.GetNormalizedScreenSize(Camera.main)),
                            (5 + (10 * dir.magnitude)) * Time.deltaTime
                        );
                }


                // Always recompute mousePos relative to player
                mousePos = playerScreenPos + joystickScreenOffset;
            }
            // build mode
            else
            {
                Vector2 playerScreenPos =
                    Camera.main.WorldToScreenPoint(player.instance.transform.position);

                // Initialize offset the first frame we enter joystick mode
                if (usingStick)
                    joystickScreenOffset += stick * buildModeCursorSpeed * Time.deltaTime * ForceAspectRatio.GetNormalizedScreenSize(Camera.main);

                // Apply offset in screen space
                Vector2 desiredScreenPos = playerScreenPos + joystickScreenOffset;

                // Clamp to screen
                desiredScreenPos.x = Mathf.Clamp(desiredScreenPos.x, 0, Screen.width);
                desiredScreenPos.y = Mathf.Clamp(desiredScreenPos.y, 0, Screen.height);

                // Convert to world
                Vector3 worldMousePos =
                    Camera.main.ScreenToWorldPoint(
                        new Vector3(
                            desiredScreenPos.x,
                            desiredScreenPos.y,
                            Camera.main.WorldToScreenPoint(player.instance.transform.position).z
                        )
                    );

                // Clamp relative to player in world space
                Vector2 relative =
                    (Vector2)worldMousePos -
                    (Vector2)player.instance.transform.position;

                float buildDist = HandMove.instance.buildDist;
                if (HandMove.instance.selectedItem &&
                    HandMove.instance.selectedItem.overrideBuildDist != -1)
                    buildDist = HandMove.instance.selectedItem.overrideBuildDist;

                relative = new Vector2(Mathf.Clamp(relative.x, -buildDist / 2, buildDist / 2), Mathf.Clamp(relative.y, -buildDist / 2, buildDist / 2));

                // Save final world + screen positions
                HandMove.instance.mousePosition =
                    (Vector2)player.instance.transform.position + relative;

                mousePos = Camera.main.WorldToScreenPoint(
                    HandMove.instance.mousePosition
                );

                // Update stored offset so it survives camera/player motion
                joystickScreenOffset = mousePos - playerScreenPos;
            }
        }

        if (!usingVirtualMouse && Mouse.current != null)
        {
            mousePos = Mouse.current.position.ReadValue();
            joystickScreenOffset = Vector2.zero;
        }

        mousePos.x = Mathf.Clamp(mousePos.x, 0, Screen.width);
        mousePos.y = Mathf.Clamp(mousePos.y, 0, Screen.height);

        mouseScroll = 0;
        if (!(inventory.instance && inventory.instance.inventoryOpen))
        {
            if (Mathf.Abs(input.actions["Scroll"].ReadValue<Vector2>().y) <= 1)
                mouseScroll += input.actions["Scroll"].ReadValue<Vector2>().y;

            HandleScroll(input.actions["Scroll Left"], ref upHoldTime, ref upRepeatTimer, 1);
            HandleScroll(input.actions["Scroll Right"], ref downHoldTime, ref downRepeatTimer, -1);
        }
        UpdateVirtualMouse();

        usingController = lastDevice is Gamepad || lastStickTime > lastMouseTime;
    }

    void UpdateVirtualMouse()
    {
        if (!usingVirtualMouse || virtualMouse == null || Gamepad.current == null)
            return;

        bool leftClick = actions["Inventory Click L"].started;
        bool rightClick = actions["Inventory Click R"].started;
        bool shift = actions["Shift Click"].started;

        uint buttons = 0;
        if (MouseCursor.pointerMode)
        {
            if (leftClick) buttons |= 1u << 0;
            if (rightClick) buttons |= 1u << 1;
            if (shift && usingController) buttons |= 1u << 0;
        }
        var mouseState = new MouseState
        {
            position = mousePos,
            scroll = new Vector2(0f, mouseScroll * 1.5f),
            buttons = (ushort)buttons
        };

        InputSystem.QueueStateEvent(virtualMouse, mouseState);
    }

    void HandleScroll(
        InputAction action,
        ref float holdTime,
        ref float repeatTimer,
        int direction)
    {
        if (!usingController) return;

        if (action.WasPressedThisFrame())
        {
            mouseScroll += direction;
            holdTime = 0f;
            repeatTimer = 0f;
            return;
        }

        if (action.IsPressed())
        {
            holdTime += Time.unscaledDeltaTime;

            if (holdTime >= holdDelay)
            {
                repeatTimer += Time.unscaledDeltaTime;

                if (repeatTimer >= repeatRate)
                {
                    mouseScroll += direction;
                    repeatTimer = 0f;
                }
            }
        }
        else
        {
            holdTime = 0f;
            repeatTimer = 0f;
        }
    }
}
public struct ActionType
{
    public bool started, held, released;

    public ActionType(bool started, bool held, bool released)
    {
        this.started = started;
        this.held = held;
        this.released = released;
    }
}
