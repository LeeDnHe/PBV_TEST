using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Exoa.Designer
{
    public class ModuleMenuScrollRect : ScrollRect, IPointerExitHandler
    {
        public override void OnDrag(PointerEventData eventData)
        {
            base.OnDrag(eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            OnEndDrag(eventData);
            StopMovement();
        }
    }
}
