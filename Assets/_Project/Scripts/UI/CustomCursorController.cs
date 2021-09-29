using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CustomCursorController : MonoBehaviour {

    public StandaloneInputModule standaloneInputModule;
    public CustomCursorUIModule cursorModule;

    public RectTransform cursor;
    public Image cursorImage;
    public float speed = 64f;
    [HideInInspector] public Vector2 position;
    private bool enableCursor;
    private Vector3 lastMousePos;

    private void Awake () {
        position = new Vector2(Screen.width / 2f, Screen.height / 2f);
        lastMousePos = Input.mousePosition;
    }

    void Update () {
        Vector2 cursorDelta = InputListener.GetCursorDelta();
        enableCursor = enableCursor || cursorDelta != Vector2.zero;
        cursorImage.enabled = enableCursor;

        if(lastMousePos != Input.mousePosition) {
            enableCursor = false;
            lastMousePos = Input.mousePosition;
        }

        standaloneInputModule.enabled = !enableCursor;
        cursorModule.enabled = enableCursor;

        position += cursorDelta * speed * Time.deltaTime;
        position.x = Mathf.Clamp(position.x, 0f, Screen.width);
        position.y = Mathf.Clamp(position.y, 0f, Screen.height);
        cursor.position = position;
        cursorModule.position = position;

        if(enableCursor) {
            InputListener.GetCursorEvents(out bool isDown, out bool isPressed, out bool isUp);
            cursorModule.isCursorDown = isDown;
            cursorModule.isCursorPressed = isPressed;
            cursorModule.isCursorUp = isUp;
            cursorModule.delta = cursorDelta * speed * Time.deltaTime;
        }
    }
}
