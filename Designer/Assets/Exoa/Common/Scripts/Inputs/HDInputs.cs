using Exoa.Common;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Exoa.Designer
{
    public class HDInputs : MonoBehaviour
    {
        public static bool ControlKey => (Event.current != null && Event.current.control && Event.current.type == EventType.KeyDown);
        public static bool IsOverUI => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();


        [Header("Key Map")]
        public static KeyCode resetCamera = KeyCode.F;
        public static KeyCode save = KeyCode.S;
        public static KeyCode switchPerspective = KeyCode.Space;
        public static KeyCode openSaveFolder = KeyCode.D;
        public static KeyCode toggleGizmos = KeyCode.G;
        public static KeyCode toggleRoofs = KeyCode.R;
        public static KeyCode toggleExteriorWalls = KeyCode.E;
        public static KeyCode escape = KeyCode.Escape;
        public static KeyCode leftAlt = KeyCode.LeftAlt;

        public static bool ResetCamera()
        {
            return BaseTouchInput.GetKeyWentDown(resetCamera) && !IsOverUI;
        }

        public static bool SavePressed()
        {
            return BaseTouchInput.GetKeyWentDown(save) && ControlKey;
        }

        public static bool OpenSaveFolderPressed()
        {
            return BaseTouchInput.GetKeyWentDown(openSaveFolder) && ControlKey;
        }


        public static bool ChangePlanMode()
        {
            return BaseTouchInput.GetKeyWentDown(switchPerspective) && !IsOverUI;
        }

        public static bool ToggleGizmo()
        {
            return BaseTouchInput.GetKeyWentDown(toggleGizmos) && !IsOverUI;
        }

        public static bool ToggleExteriorWalls()
        {
            return BaseTouchInput.GetKeyWentDown(toggleExteriorWalls) && !IsOverUI;
        }

        public static bool ToggleRoof()
        {
            return BaseTouchInput.GetKeyWentDown(toggleRoofs) && !IsOverUI;
        }

        public static bool ReleaseDrag()
        {
            return BaseTouchInput.GetMouseWentUp(0);
        }

        public static bool OptionPress()
        {
            return BaseTouchInput.GetMouseWentDown(1);
        }

        public static bool EscapePressed()
        {
            return BaseTouchInput.GetKeyWentDown(escape);
        }

        public static bool AltPressed()
        {
            return BaseTouchInput.GetKeyIsHeld(leftAlt);
        }
    }
}
