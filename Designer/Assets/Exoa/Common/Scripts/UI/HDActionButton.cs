using Exoa.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Exoa.Designer.UI
{
    public class HDActionButton : MonoBehaviour
    {

        public GameEditorEvents.Action action;
        private Button btn;

        void Start()
        {
            btn = GetComponent<Button>();
            btn.onClick.AddListener(OnButtonClicked);
        }

        private void OnButtonClicked()
        {
            Debug.Log("OnButtonClicked " + action);
            GameEditorEvents.OnRequestButtonAction?.Invoke(action, true);
        }
    }
}
