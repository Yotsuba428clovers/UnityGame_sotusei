using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuButtonMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public Color normal;
    public Color highlighted;
    bool hovered, selected;
    Image image;
    void Awake() { image = GetComponent<Image>(); }
    void Update()
    {
        bool active = hovered || selected;
        float blend = 1 - Mathf.Exp(-12 * Time.unscaledDeltaTime);
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * (active ? 1.025f : 1), blend);
        if (image != null) image.color = Color.Lerp(image.color, active ? highlighted : normal, blend);
    }
    public void OnPointerEnter(PointerEventData e) { hovered = true; }
    public void OnPointerExit(PointerEventData e) { hovered = false; }
    public void OnSelect(BaseEventData e) { selected = true; }
    public void OnDeselect(BaseEventData e) { selected = false; }
    void OnDisable() { hovered = selected = false; }
}
