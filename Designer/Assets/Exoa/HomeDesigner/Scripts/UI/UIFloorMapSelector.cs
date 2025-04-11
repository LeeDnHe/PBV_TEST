using Exoa.Designer.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Exoa.Designer
{
    public class UIFloorMapSelector : MonoBehaviour
    {
        public static UIFloorMapSelector instance;
        public ScrollRect scrollRect;
        public GameObject itemPrefab;
        public float itemOffset = 2;
        public RectTransform containerRect;
        private List<UISavingItem> items;
        public Button createBtn;

        public TMP_InputField filterInputField;
        private FloorMapSelectorPopup popup;

        private BaseSerializable dataSerialiazer;

        void OnDestroy()
        {

        }
        void Awake()
        {
            instance = this;
        }
        public void Start()
        {
            popup = GetComponentInParent<FloorMapSelectorPopup>();
            dataSerialiazer = GameObject.FindObjectOfType<BaseSerializable>();
            //SaveSystem.defaultSubFolderName = dataReader.GetFolderName();

            filterInputField.onValueChanged.AddListener(OnFilterChanged);
            createBtn.onClick.AddListener(OnClickCreateFloorMap);

            GetFileList();

            //StartCoroutine(OpenDefaultFile());

        }

        private void OnClickCreateFloorMap()
        {
            UISaving.instance.SaveAndExitToScene("FloorMapEditor");
        }


        private void OnFilterChanged(string arg0)
        {
            //print("OnFilterChanged " + arg0);
            for (int i = 0; i < items.Count; i++)
            {
                items[i].gameObject.SetActive(items[i].fileName.ToLower().Contains(arg0.ToLower()));
            }
        }


        protected void OnFileListChange(List<string> list)
        {
            if (list.Count == 0)
            {
                AlertPopup ap = AlertPopup.ShowAlert("noMaps", "No Maps", "You have no floor map yet, click OK to open Floor Map Designer and start creating your first floor map!", true);
                ap.OnClickOKEvent.AddListener(OnClickCreateFloorMap);
            }
            containerRect.ClearChildren();
            items = new List<UISavingItem>();

            float itemWidth = 0;

            foreach (string name in list)
            {
                GameObject inst = Instantiate(itemPrefab);
                inst.transform.SetParent(containerRect);
                RectTransform r = inst.GetComponent<RectTransform>();
                r.localScale = Vector3.one;
                itemWidth = r.sizeDelta.x;
                UISavingItem plmi = r.GetComponent<UISavingItem>();
                plmi.SetFilePath(name);
                plmi.OnSelect += OnRequestLoadFile;
                plmi.HideEditDeleteButtons();
                plmi.LoadImages(UISaving.ThumbnailPrefixes.Floormap);

                items.Add(plmi);
            }
            containerRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, items.Count * itemWidth);
            scrollRect.horizontalNormalizedPosition = 0;
        }

        private void OnRequestLoadFile(string name)
        {
            //print("OnRequestLoadFile name:" + name);
            Load(false, name, true);

            popup.Hide();
        }






        /// <summary>
        /// Load a room by name
        /// There is two ways to load : offline from Resources/Levels/ or Online
        /// </summary>
        /// <param name="askSaving"></param>
        /// <param name="name"></param>
        /// <param name="sendLoadedEvent"></param>
        /// <param name="pStrategy"></param>
        public void Load(bool askSaving, string name, bool sendLoadedEvent = true)
        {
            if (!askSaving || NoNeedToAsk())
            {
                dataSerialiazer.DeserializeToScene(dataSerialiazer.SerializeEmpty(name));
                return;
            }
            AlertPopup popup = AlertPopup.ShowAlert("changeRoom", "Change Floor Map ?", "All your previous interior will be cleared, continue ?", true, "Cancel");

            popup.OnClickOKEvent.AddListener(() =>
            {
                dataSerialiazer.DeserializeToScene(dataSerialiazer.SerializeEmpty(name));
            });
        }


        private bool NoNeedToAsk()
        {
            return false;// TODO
        }








        public void GetFileList(string arg = null)
        {
            SaveSystem.Create(SaveSystem.Mode.FILE_SYSTEM).ListFileItems(HDSettings.EXT_FLOORMAP_FOLDER, (SaveSystem.FileList l) =>
            {
                List<string> bothLists = (l.list);

                if (l.list == null)
                    bothLists = new List<string>();

                SaveSystem.Create(SaveSystem.Mode.RESOURCES).ListFileItems(HDSettings.EMBEDDED_FLOORMAP_FOLDER, (SaveSystem.FileList internalList) =>
                {
                    if (internalList.list != null)
                    {
                        bothLists.AddRange(internalList.list);
                    }
                    OnFileListChange(bothLists);
                });
            });


        }
    }
}
