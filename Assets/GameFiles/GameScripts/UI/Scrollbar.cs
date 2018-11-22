using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


/// <summary>
/// Functional replacement of UnityEngine.UI.Scrollbar
/// Controls a (vertical) scroll bar to be used in conjuction with a Scroll Rect.
/// </summary>
public class Scrollbar : MonoBehaviour {

    public RectTransform targetContent;

    /// <summary>
    /// Call this function via the scroll rect to update the scrollbar position
    /// </summary>
    public void DirectUpdate(Vector2 pos) {
        SetPosition(1f - pos.y);
    }

    /// <summary>
    /// Call this function via events on the scrollbar to snap 
    /// the scrollbar to the mouse position (and scroll the scroll rect accordingly)
    /// </summary>
    public void MouseUpdate() {
        SetPosition(GetMouseY());
    }

    private float GetMouseY() {
        Vector2 mousePos = Input.mousePosition;
        RectTransform rec = GetComponent<RectTransform>();
        Camera wc = GetComponentInParent<Canvas>().worldCamera;
        Vector2 lp;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rec, mousePos, wc, out lp);
        float y = lp.y;

        RectTransform slideArea = transform.GetChild(0).GetComponent<RectTransform>();
        RectTransform handle = slideArea.GetChild(0).GetComponent<RectTransform>();

        float yOffset = handle.rect.height / 2f;
        float yRange = slideArea.rect.height;

        return Mathf.Clamp01((-yOffset - y) / yRange);
    }

    private void SetPosition(float y) {
        RectTransform contentHolder = targetContent.parent.GetComponent<RectTransform>();
        RectTransform slideArea = transform.GetChild(0).GetComponent<RectTransform>();
        RectTransform handle = slideArea.GetChild(0).GetComponent<RectTransform>();

        float handleY = (.5f - y) * slideArea.rect.height;
        handle.localPosition = new Vector3(0f, handleY, 0f);

        float contentRange = targetContent.rect.height - contentHolder.rect.height;
        float contentY = y * contentRange;
        targetContent.localPosition = new Vector3(0f, contentY, 0f);
    }
}
