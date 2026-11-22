using UnityEngine;
using UnityEngine.EventSystems;

namespace Bubbles
{
    public sealed class HoldSettingsButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, IDragHandler, ISubmitHandler
    {
        public ParentSettings owner;
        private Vector2 start;
        public void OnPointerDown(PointerEventData e)
        {
            start = e.position;
            owner.StartHold();
        }

        public void OnPointerUp(PointerEventData e) => owner.CancelHold();
        public void OnPointerExit(PointerEventData e) => owner.CancelHold();
        public void OnDrag(PointerEventData e)
        {
            if (Vector2.Distance(start, e.position) > 12)
            {
                owner.CancelHold();
            }
        }

        public void OnSubmit(BaseEventData e)
        { /* キーボードの長押しはParentSettingsで処理する。 */
        }
    }
}
