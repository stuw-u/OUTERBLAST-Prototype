using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CustomCursorUIModule : PointerInputModule {

    public Vector2 position;
    public Vector2 delta;
    public bool isCursorDown;
    public bool isCursorPressed;
    public bool isCursorUp;

    public string ClickInputName = "Submit";
    public RaycastResult CurrentRaycast;

    private PointerEventData pointerEventData;
    private GameObject currentLookAtHandler;

    public override void Process () {
        SetPointerPosition();
        HandleRaycast();
        HandleSelection();
    }

    private void SetPointerPosition () {
        if(pointerEventData == null) {
            pointerEventData = new PointerEventData(eventSystem);
        }
        
        pointerEventData.position = position;
        pointerEventData.delta = delta;
        pointerEventData.dragging = isCursorPressed;
    }

    private void HandleRaycast () {
        List<RaycastResult> raycastResults = new List<RaycastResult>();
        eventSystem.RaycastAll(pointerEventData, raycastResults);
        CurrentRaycast = pointerEventData.pointerCurrentRaycast = FindFirstRaycast(raycastResults);

        ProcessMove(pointerEventData);
    }

    private void HandleSelection () {
        if(pointerEventData.pointerEnter != null) {
            GameObject handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(pointerEventData.pointerEnter);

            if(currentLookAtHandler != handler) {
                currentLookAtHandler = handler;
            }

            if(currentLookAtHandler != null) {
                if(isCursorUp) {
                    ExecuteEvents.ExecuteHierarchy(currentLookAtHandler, pointerEventData, ExecuteEvents.pointerClickHandler);
                    ExecuteEvents.ExecuteHierarchy(currentLookAtHandler, pointerEventData, ExecuteEvents.pointerUpHandler);
                    ExecuteEvents.ExecuteHierarchy(currentLookAtHandler, pointerEventData, ExecuteEvents.dropHandler);
                    ExecuteEvents.ExecuteHierarchy(currentLookAtHandler, pointerEventData, ExecuteEvents.endDragHandler);
                } else if(isCursorDown) {
                    ExecuteEvents.ExecuteHierarchy(currentLookAtHandler, pointerEventData, ExecuteEvents.initializePotentialDrag);
                    ExecuteEvents.ExecuteHierarchy(currentLookAtHandler, pointerEventData, ExecuteEvents.pointerDownHandler);
                } else if(isCursorPressed) {
                    ExecuteEvents.ExecuteHierarchy(currentLookAtHandler, pointerEventData, ExecuteEvents.dragHandler);
                }
            }
        } else {
            currentLookAtHandler = null;
        }
    }
}
