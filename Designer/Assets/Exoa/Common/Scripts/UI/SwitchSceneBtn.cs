using Exoa.Common;
using Exoa.Designer;
using UnityEngine;
using UnityEngine.UI;

namespace Exoa.Events
{
    public class SwitchSceneBtn : MonoBehaviour
    {
        private Button btn;
        public string sceneName;
        public KeyCode key = KeyCode.W;

        void Start()
        {
            btn = GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(LoadUnityLevel);
        }
        public void LoadUnityLevel()
        {
            UISaving.instance.SaveAndExitToScene(sceneName);
        }
        void Update()
        {
            if (BaseTouchInput.GetKeyWentDown(key) && BaseTouchInput.GetKeyIsHeld(KeyCode.LeftControl))
            {
                LoadUnityLevel();
            }
        }
    }
}
