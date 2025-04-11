using Exoa.Designer.Data;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Exoa.Designer
{
    public class ModuleMenuItem : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler, IPointerDownHandler

    {
        [System.Serializable]
        public class OnSelectModuleEvent : UnityEvent<ModuleMenuItem, PointerEventData> { };


        public OnSelectModuleEvent OnSelectModule;
        public OnSelectModuleEvent OnExitItemZone;

        private ModuleDataModels.Module module;

        public void OnPointerClick(PointerEventData eventData)
        {
            OnSelectModule?.Invoke(this, eventData);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnSelectModule?.Invoke(this, eventData);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (module == null || string.IsNullOrEmpty(module.prefab))
                module = AppController.Instance.GetModuleByPrefab(gameObject.name);
            if (module != null && !string.IsNullOrEmpty(module.prefab))
                InfoPopup.Instance.Show(null, module);
            else Debug.Log("Cannot find module for prefab " + gameObject.name);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            OnExitItemZone?.Invoke(this, eventData);
            InfoPopup.Instance.Hide();
        }

        // Start is called before the first frame update
        void Start()
        {

        }

    }
}
