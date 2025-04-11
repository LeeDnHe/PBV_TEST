using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Exoa.Designer.DataModel;

namespace Exoa.Designer
{
    public class TabMenu : MonoBehaviour
    {
        public RectTransform container;
        public GameObject prefab;
        private static InteriorCategories data;
        private static bool initialized;


        void Start()
        {
            Init();

            int ex = 0;
            container.ClearChildren();

            foreach (InteriorCategory tab in data.folders)
            {
                GameObject inst = Instantiate(prefab, container);
                inst.transform.localScale = Vector3.one;
                TMP_Text t = inst.GetComponentInChildren<TMP_Text>();
                Button b = inst.GetComponentInChildren<Button>();
                Image i = inst.GetComponentInChildren<Image>();

                t.text = tab.name;
                t.enabled = true;
                i.enabled = true;
                b.enabled = true;
                b.onClick.AddListener(() =>
                {
                    GameObject[] prefabs = Resources.LoadAll<GameObject>(HDSettings.MODULES_FOLDER + tab.folder);
                    ModuleMenuController.Instance.DisplayPrefabs(prefabs);
                });
                if (ex == 0) b.onClick.Invoke();
                inst.SetActive(true);
                ex++;
            }
        }

        private static void Init()
        {
            if (initialized) return;
            initialized = true;

            TextAsset ta = Resources.Load<TextAsset>(HDSettings.CATEGORIES_JSON);
            data = JsonUtility.FromJson<InteriorCategories>(ta.text);
        }

        public static GameObject FindModuleByName(string name)
        {
            Init();
            foreach (InteriorCategory tab in data.folders)
            {
                GameObject prefab = Resources.Load<GameObject>(HDSettings.MODULES_FOLDER + tab.folder + "/" + name);
                if (prefab != null)
                    return prefab;
            }
            return null;
        }
    }
}
