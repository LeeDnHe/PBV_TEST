using UnityEngine;

namespace Exoa.Designer
{
    public class MaterialPopup : BaseFloatingPopup
    {
        public static MaterialPopup Instance;
        private Renderer currentRenderer;
        private SpaceMaterialController room;
        private BuildingMaterialController building;
        private ModuleColorVariants module;
        private MaterialPopupUI wallSettings;

        public ModuleColorVariants Module { get => module; set => module = value; }
        public BuildingMaterialController Building { get => building; set => building = value; }
        public SpaceMaterialController Room { get => room; set => room = value; }

        override protected void Awake()
        {
            Instance = this;
            wallSettings = GetComponentInChildren<MaterialPopupUI>(true);

            base.Awake();
        }



        public void ShowMode(MaterialPopupUI.Mode mode, GameObject obj)
        {
            CurrentTarget = obj.transform;
            currentRenderer = obj.GetComponent<Renderer>();

            //print("ShowMode obj:" + obj);

            building = GameObject.FindObjectOfType<BuildingMaterialController>();
            room = obj.GetComponentInParent<SpaceMaterialController>();
            module = obj.GetComponentInChildren<ModuleColorVariants>();
            wallSettings.ShowMode(mode);

            Show();
            MovePopup();
        }



    }
}
