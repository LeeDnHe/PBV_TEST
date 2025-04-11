using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Exoa.Designer
{
    public class ModuleColorVariants : MonoBehaviour
    {
        [System.Serializable]
        public struct Variant
        {
            public string name;
            public Material material;
            public Color color;
        }
        public enum Type { Colors, Materials };
        public Type type;
        public List<Variant> variants;
        public List<Renderer> renderers;
        private string selectedMaterialName;
        private Color selectedColor;

        public Color SelectedColor { get => selectedColor; set => selectedColor = value; }
        public string SelectedMaterialName { get => selectedMaterialName; set => selectedMaterialName = value; }

        public void ApplyModuleMaterial(string materialName)
        {
            if (string.IsNullOrEmpty(materialName))
                return;

            HDLogger.Log("ApplyModuleMaterial materialName:" + materialName, HDLogger.LogCategory.Interior);

            for (int i = 0; i < variants.Count; i++)
            {
                if (variants[i].material != null && variants[i].material.name == materialName)
                {
                    ApplyModuleMaterial(variants[i].material);
                    return;
                }
            }
        }
        public void ApplyModuleMaterial(Material mat)
        {
            if (mat == null)
                return;

            HDLogger.Log("ApplyModuleMaterial mat.name:" + mat.name, HDLogger.LogCategory.Interior);

            selectedMaterialName = mat.name;
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] != null) renderers[i].material = mat;
            }
        }

        internal void ApplyModuleTiling(float tiling)
        {
            throw new System.NotImplementedException();
        }

        public void ApplyModuleColor(Color c)
        {
            if (c == default(Color))
                return;

            selectedColor = c;
            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] != null)
                {
                    Material tempMaterial = new Material(renderers[i].sharedMaterial);
                    tempMaterial.color = c;
                    renderers[i].sharedMaterial = tempMaterial;
                }
            }
        }
    }
}
