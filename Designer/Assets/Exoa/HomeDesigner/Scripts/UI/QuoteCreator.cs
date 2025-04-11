using Exoa.Designer.Data;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Exoa.Designer
{
    public class QuoteCreator : MonoBehaviour
    {
        public RectTransform container;
        public GameObject prefab;
        public Button createSubmissionBtn;
        private List<ModuleDataModels.SubmissionsItem> items;
        private float total;

        void Start()
        {
            InteriorDesigner.OnChangeAnything.AddListener(Create);
            createSubmissionBtn?.onClick.AddListener(OnClickCreateQuote);
        }

        private void OnClickCreateQuote()
        {
            // HERE IS THE LIST OF ITEMS IN THE ROOM
            // items.ToArray();

            // YOUR LOGIC TO SEND THE QUOTE GOES HERE
        }


        void Create()
        {
            items = new List<ModuleDataModels.SubmissionsItem>();

            ModuleController[] modules = GameObject.FindObjectsOfType<ModuleController>();

            Dictionary<string, int> modulesCount = new Dictionary<string, int>();
            foreach (ModuleController b in modules)
            {
                if (b.isGhost)
                    continue;

                int val;
                if (modulesCount.TryGetValue(b.gameObject.name, out val))
                {
                    modulesCount[b.gameObject.name]++;
                }
                else
                {
                    modulesCount[b.gameObject.name] = 1;
                }
            }
            container.ClearChildren();
            total = 0;
            GameObject inst = null;
            foreach (KeyValuePair<string, int> pair in modulesCount)
            {
                ModuleDataModels.Module p = AppController.Instance.GetModuleByPrefab(pair.Key);
                if (p.prefab == null) continue;

                inst = Instantiate<GameObject>(prefab, container);
                inst.transform.FindChildRecursiveComp<TMP_Text>("name").text = p.title + " x" + pair.Value;
                inst.transform.FindChildRecursiveComp<TMP_Text>("price").text = p.price.ToString("0.00") + "$";
                total += Mathf.RoundToInt((p.price) * pair.Value);

                items.Add(new ModuleDataModels.SubmissionsItem(p, pair.Value));
            }
            inst = Instantiate<GameObject>(prefab, container);
            inst.transform.FindChildRecursiveComp<TMP_Text>("name").text = "Total";
            inst.transform.FindChildRecursiveComp<TMP_Text>("name").fontStyle = FontStyles.Bold;
            inst.transform.FindChildRecursiveComp<TMP_Text>("price").fontStyle = FontStyles.Bold;
            inst.transform.FindChildRecursiveComp<TMP_Text>("price").text = total.ToString("0.00") + "$";
        }
    }
}
