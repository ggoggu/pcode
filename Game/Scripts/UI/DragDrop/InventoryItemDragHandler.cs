using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using PenguinPinball.Core;

public class InventoryItemDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public object itemReference; // 장비, 부적, 펭귄 데이터 참조
    public PenguinLocation sourceLocation = PenguinLocation.Inventory;
    public int sourceIndex = -1;
    
    private Vector3 originalPosition;
    private int originalSiblingIndex;
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private LayoutElement layoutElement;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        layoutElement = GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = gameObject.AddComponent<LayoutElement>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (itemReference == null)
        {
            eventData.pointerDrag = null;
            return;
        }

        originalPosition = rectTransform.position;
        originalSiblingIndex = rectTransform.GetSiblingIndex();

        if (layoutElement != null)
            layoutElement.ignoreLayout = true;

        rectTransform.SetAsLastSibling();

        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        rectTransform.position = originalPosition;

        if (layoutElement != null)
            layoutElement.ignoreLayout = false;

        rectTransform.SetSiblingIndex(originalSiblingIndex);
    }
}
