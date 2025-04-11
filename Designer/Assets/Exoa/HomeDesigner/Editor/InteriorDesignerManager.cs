using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using System.Collections.Generic;
using UnityEngine.UIElements;
using System;
using static Exoa.Designer.TabMenu;
using System.IO;
using System.Text;
using Exoa.Json;
using static Exoa.Designer.Data.ModuleDataModels;
using Exoa.Designer.Data;
using System.Linq;
using UnityEngine.UIElements.Expansions;
using UnityEditor.UIElements.Expansions;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using static Exoa.Designer.DataModel;

namespace Exoa.Designer
{

    public class InteriorDesignerManager : UnityEditor.EditorWindow
    {
        private Main tabsPage;
        private Main modulesPage;
        private Main toolsPage;
        private H2 pageTitle;
        private PopupField<string> newModuleCategory;
        private PopupField<string> newModuleCategory2;
        private TextField newModuleName;
        private Button createModuleBtn2;
        private List<string> meshesInSelction;
        private Div popupDiv, popupDiv2;

        public class MyObject : ScriptableObject
        {
            [SerializeField] public List<InteriorCategory> myTabList;
            [SerializeField] public List<ModuleDataModels.Module> myModuleList;
            [SerializeField] public List<string> myCategoriesFolderNameList;
            [SerializeField] public List<string> myModuleNameList;
        }


        /// <summary>
        /// Show the EditorWindow window.
        /// </summary>
        [MenuItem("Tools/Exoa/Home Designer Modules Manager")]
        public new static void Show()
        {
            InteriorDesignerManager wnd = GetWindow<InteriorDesignerManager>();
            wnd.titleContent = new GUIContent("Home Designer Modules Manager");
        }

        private SerializedObject serializedObject;
        private MyObject obj;
        private ReorderableList tabsList;
        private ReorderableList modulesList;

        private void OnEnable()
        {
            this.ApplyStyle();
            tabsList = this.rootVisualElement.Q<ReorderableList>("tabsList");
            modulesList = this.rootVisualElement.Q<ReorderableList>("modulesList");

            obj = ScriptableObject.CreateInstance<MyObject>();

            TextAsset ta = Resources.Load<TextAsset>(HDSettings.CATEGORIES_JSON);
            InteriorCategories tabs = JsonUtility.FromJson<InteriorCategories>(ta.text);

            ta = Resources.Load<TextAsset>(HDSettings.MODULES_JSON);
            ModuleList modules = JsonUtility.FromJson<ModuleList>(ta.text);

            obj.myTabList = tabs.folders;
            obj.myModuleList = modules.modules;
            obj.myCategoriesFolderNameList = obj.myTabList.Select(s => s.folder).ToList();
            obj.myModuleNameList = obj.myModuleList.Select(s => s.prefab).ToList();


            BindListsToObjects();

            pageTitle = this.rootVisualElement.Q<H2>("pageTitle");
            tabsPage = this.rootVisualElement.Q<Main>("tabsPage");
            modulesPage = this.rootVisualElement.Q<Main>("modulesPage");
            toolsPage = this.rootVisualElement.Q<Main>("toolsPage");


            Button tabsBtn = this.rootVisualElement.Q<Button>("tabsBtn");
            Button modulesBtn = this.rootVisualElement.Q<Button>("modulesBtn");
            Button toolsBtn = this.rootVisualElement.Q<Button>("toolsBtn");
            Button saveTabsBtn = this.rootVisualElement.Q<Button>("saveTabsBtn");
            Button openTabsBtn = this.rootVisualElement.Q<Button>("openTabsBtn");
            Button saveModulesBtn = this.rootVisualElement.Q<Button>("saveModulesBtn");
            Button openModulesBtn = this.rootVisualElement.Q<Button>("openModulesBtn");
            Button addNewModuleItemBtn = this.rootVisualElement.Q<Button>("addNewModuleItemBtn");
            Button addNewCategoryItemBtn = this.rootVisualElement.Q<Button>("addNewCategoryItemBtn");
            Button addMissingModulesBtn = this.rootVisualElement.Q<Button>("addMissingModulesBtn");
            Button addMissingTabsBtn = this.rootVisualElement.Q<Button>("addMissingTabsBtn");
            Button removeSelectedCategoryItemBtn = this.rootVisualElement.Q<Button>("removeSelectedCategoryItemBtn");
            Button removeSelectedModuleItemBtn = this.rootVisualElement.Q<Button>("removeSelectedModuleItemBtn");
            Button createModuleBtn = this.rootVisualElement.Q<Button>("createModuleBtn");
            createModuleBtn2 = this.rootVisualElement.Q<Button>("createModuleBtn2");
            Button generateAllThumbnailsBtn = this.rootVisualElement.Q<Button>("generateAllThumbnailsBtn");

            tabsBtn.clickable.clicked += () => SwitchPage("tabsPage", "Tabs Manager");
            modulesBtn.clickable.clicked += () => SwitchPage("modulesPage", "Modules Manager");
            toolsBtn.clickable.clicked += () => SwitchPage("toolsPage", "Tools");

            saveTabsBtn.clickable.clicked += OnClickSaveTabs;
            openTabsBtn.clickable.clicked += OnClickOpenTabs;

            saveModulesBtn.clickable.clicked += OnClickSaveModules;
            openModulesBtn.clickable.clicked += OnClickOpenModules;
            addMissingModulesBtn.clickable.clicked += OnClickAddMissingModules;
            addMissingTabsBtn.clickable.clicked += OnClickAddMissingCategories;
            createModuleBtn.clickable.clicked += OnClickCreateNewModule;
            createModuleBtn2.clickable.clicked += OnClickCreateNewModuleBasedOnSelection;
            generateAllThumbnailsBtn.clickable.clicked += OnClickGenerateAllThumbnails;
            addNewCategoryItemBtn.clickable.clicked += OnClickAddNewCategoryItemBtn;
            addNewModuleItemBtn.clickable.clicked += OnClickAddNewModuleItemBtn;
            removeSelectedModuleItemBtn.clickable.clicked += OnClickRemoveSelectedModuleItemBtn;
            removeSelectedCategoryItemBtn.clickable.clicked += OnClickRemoveSelectedCategoryItemBtn;

            popupDiv = this.rootVisualElement.Q<Div>("popupDiv");
            popupDiv2 = this.rootVisualElement.Q<Div>("popupDiv2");
            newModuleName = this.rootVisualElement.Q<TextField>("newModuleName");

            // Create a new field and assign it its value.
            CreateCategoriesPopups();

            //newModuleTab2.RegisterCallback<ChangeEvent<string>>((evt) =>
            //{
            //Debug.Log(evt.newValue);
            //});
        }

        private void OnClickRemoveSelectedCategoryItemBtn()
        {
            int index = GetSelectedIndex(tabsList);
            if (index > -1 && obj.myTabList.Count > index)
            {
                obj.myTabList.RemoveAt(index);
                BindListsToObjects();
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Could not find the element " + index + " to remove!", "Ok");
                return;
            }
        }

        private void OnClickRemoveSelectedModuleItemBtn()
        {
            int index = GetSelectedIndex(modulesList);
            if (index > -1)
            {
                obj.myModuleList.RemoveAt(index);
                BindListsToObjects();
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Could not find the element to remove!", "Ok");
                return;
            }
        }

        private int GetSelectedIndex(ReorderableList list)
        {
            int index = -1;
            VisualElement selected = list.Q<VisualElement>(null, "unity-reorderable-list__item_selected");
            if (selected != null)
            {
                VisualElement child = selected;
                do
                {
                    string name = child.name;
                    int startIndex = name.LastIndexOf('[');
                    if (startIndex > 0)
                    {
                        string subStr = name.Substring(startIndex + 1, 1);
                        index = int.Parse(subStr);
                    }
                    child = child.Children().ElementAt(0);

                } while (index == -1 && child != null);
            }
            return index;
        }

        private void OnClickAddNewCategoryItemBtn()
        {
            obj.myTabList.Add(new InteriorCategory("", ""));
            BindListsToObjects();
        }

        private void OnClickAddNewModuleItemBtn()
        {
            obj.myModuleList.Add(new ModuleDataModels.Module("", ""));
            BindListsToObjects();
        }

        private void BindListsToObjects()
        {
            if (obj == null) OnEnable();

            serializedObject = new UnityEditor.SerializedObject(obj);

            tabsList.BindProperty(serializedObject.FindProperty("myTabList"));
            modulesList.BindProperty(serializedObject.FindProperty("myModuleList"));
        }

        private void CreateCategoriesPopups()
        {
            popupDiv.Clear();
            popupDiv2.Clear();
            newModuleCategory = new PopupField<string>("Select a category", obj.myCategoriesFolderNameList, 0);
            newModuleCategory2 = new PopupField<string>("Select a category", obj.myCategoriesFolderNameList, 0);
            popupDiv.Add(newModuleCategory);
            popupDiv2.Add(newModuleCategory2);
            //Debug.Log("CreateTabPpopups");

        }

        private void OnSelectionChange()
        {
            string[] s = Selection.assetGUIDs;
            meshesInSelction = new List<string>();
            //Debug.Log("selection:" + s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(s[i]);
                string fullPath = Path.Combine(Directory.GetCurrentDirectory(), path);
                string ext = path.ToLower().Substring(path.LastIndexOf(".") + 1);
                if (ext == "fbx" || ext == "obj" || ext == "prefab")
                {
                    meshesInSelction.Add(path);
                }
                //Debug.Log(path + " " + ext);
            }
            createModuleBtn2.text = "Create " + meshesInSelction.Count + " module prefabs";
        }

        #region Prefab Creation
        private void OnClickCreateNewModule()
        {
            //Debug.Log("newModuleName:" + newModuleName);
            //Debug.Log("newModuleCategory:" + newModuleCategory);
            //Debug.Log("Create " + newModuleName.value + " " + newModuleCategory.value);

            CreatePrefab(newModuleName.value, newModuleCategory.value);
        }

        private void CreatePrefab(string prefaName, string tab)
        {
            string folderPath = GetFilePath(tab + " t:Folder", tab, "folder", true, "Resources/InteriorModules/");
            if (string.IsNullOrEmpty(prefaName))
            {
                EditorUtility.DisplayDialog("Error", "Please enter a prefab name", "Ok");
                return;
            }
            if (string.IsNullOrEmpty(folderPath))
                return;

            string moduleTemplate = GetFilePath("SideBySideModule_Template t:Prefab", "SideBySideModule_Template.prefab", "prefab", true, null);
            if (string.IsNullOrEmpty(moduleTemplate))
                return;


            string filePath = folderPath + "/" + prefaName + ".prefab";

            // Make sure the file name is unique, in case an existing Prefab has the same name.
            filePath = AssetDatabase.GenerateUniqueAssetPath(filePath);


            // Clone the template prefab in desctination folder
            if (!AssetDatabase.CopyAsset(moduleTemplate, filePath))
            {
                EditorUtility.DisplayDialog("Error", "Could not create prefab at " + filePath, "Ok");
                return;
            }
            EditorUtility.DisplayDialog("Done!", "Prefab created at: " + filePath, "Ok");

            AssetDatabase.Refresh();
            GameObject newPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(filePath);
            AssetDatabase.OpenAsset(newPrefab);
            Selection.activeGameObject = newPrefab;

        }

        private void OnClickCreateNewModuleBasedOnSelection()
        {
            if (meshesInSelction == null || meshesInSelction.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "No mesh selected in the project panel", "Ok");
                return;
            }
            //Debug.Log("OnClickCreateNewModuleBasedOnSelection");
            //Debug.Log("Create count:" + meshesInSelction.Count + " in folder:" + newModuleTab2.value);
            int count = 0;
            for (int i = 0; i < meshesInSelction.Count; i++)
            {
                if (meshesInSelction[i] != null)
                {
                    EditorUtility.DisplayProgressBar("Creating Module Prefabs", "Processing " + meshesInSelction[i], i / meshesInSelction.Count);

                    CreatePrefabFromMesh(meshesInSelction[i], newModuleCategory2.value);
                    count++;
                }
            }
            EditorUtility.ClearProgressBar();
            if (count > 0)
            {
                EditorUtility.DisplayDialog("Done!", "Prefab created: " + count, "Ok");
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "No prefab have been created, please select obj/fbx/prefab models in the project window first!", "Ok");
            }
        }

        private void CreatePrefabFromMesh(string meshPath, string category)
        {
            //Debug.Log("CreatePrefabFromMesh path:" + meshPath);

            string folderPath = GetFilePath(category + " t:Folder", category, "folder", true, "Resources/InteriorModules/");
            if (string.IsNullOrEmpty(folderPath))
                return;

            string moduleTemplate = GetFilePath("SideBySideModule_Template t:Prefab", "SideBySideModule_Template.prefab", "prefab", true, null);
            if (string.IsNullOrEmpty(moduleTemplate))
                return;

            string prefabName = Path.GetFileNameWithoutExtension(meshPath);
            string prefabPath = folderPath + "/" + prefabName + ".prefab";
            //Debug.Log("prefabName:" + prefabName);
            //Debug.Log("prefabPath:" + prefabPath);

            // Make sure the file name is unique, in case an existing Prefab has the same name.
            prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);

            // Clone the template prefab in desctination folder
            if (!AssetDatabase.CopyAsset(moduleTemplate, prefabPath))
            {
                EditorUtility.DisplayDialog("Error", "Could not create prefab at " + prefabPath, "Ok");
                return;
            }
            AssetDatabase.Refresh();

            GameObject prefabGo = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            GameObject meshGo = AssetDatabase.LoadAssetAtPath<GameObject>(meshPath);

            GameObject prefabInst = (GameObject)PrefabUtility.InstantiatePrefab(prefabGo);
            GameObject meshInst = Instantiate(meshGo, prefabInst.transform);
            Bounds bounds = meshInst.GetBoundsRecursive();
            //Debug.Log("bounds:" + bounds);

            PrefabUtility.ApplyPrefabInstance(prefabInst, InteractionMode.AutomatedAction);
            DestroyImmediate(prefabInst);


            AssetDatabase.Refresh();
            Selection.activeGameObject = prefabGo;
        }

        private GameObject PrepareObject(GameObject prefabName, string type, int layer)
        {
            GameObject containerGo = new GameObject(prefabName.name);
            containerGo.transform.position = prefabName.transform.position;
            containerGo.transform.SetParent(prefabName.transform.parent);

            containerGo.layer = ((int)layer);
            prefabName.layer = containerGo.layer;
            prefabName.name = "Render";
            prefabName.transform.SetParent(containerGo.transform);
            prefabName.transform.localPosition = Vector3.zero;


            return containerGo;
        }


        #endregion

        private void OnClickAddMissingCategories()
        {
            string folderPath = GetFilePath("InteriorModules t:Folder", "InteriorModules", "folder");
            if (string.IsNullOrEmpty(folderPath))
                return;

            obj.myCategoriesFolderNameList = obj.myTabList.Select(s => s.folder).ToList();

            DirectoryInfo rootDir = new DirectoryInfo(folderPath);
            DirectoryInfo[] info = rootDir.GetDirectories();
            List<string> addedToList = new List<string>();

            foreach (DirectoryInfo f in info)
            {
                bool contained = obj.myCategoriesFolderNameList.Contains(f.Name);
                //Debug.Log("Folder " + f.Name + " is in list ? " + contained);

                if (!contained)
                {
                    obj.myTabList.Add(new InteriorCategory(f.Name, f.Name));
                    addedToList.Add(f.Name);
                }
            }


            if (addedToList.Count > 0)
            {
                BindListsToObjects();
                EditorUtility.DisplayDialog("Done!", "These list entries have been added: " + addedToList.Dump(), "Ok");
            }
            else
            {
                EditorUtility.DisplayDialog("Done!", "No new folder detected inside InteriorModules/", "Ok");
            }


            CreateMissingFolders();
        }
        private void CreateMissingFolders()
        {
            obj.myCategoriesFolderNameList = obj.myTabList.Select(s => s.folder).ToList();
            List<string> list = obj.myCategoriesFolderNameList;

            string folderPath = GetFilePath("InteriorModules t:Folder", "InteriorModules", "folder");
            if (string.IsNullOrEmpty(folderPath))
                return;


            DirectoryInfo rootDir = new DirectoryInfo(folderPath);
            DirectoryInfo[] info = rootDir.GetDirectories();
            List<string> folderNameList = new List<string>();
            List<string> addedToFolder = new List<string>();

            foreach (DirectoryInfo f in info)
            {
                folderNameList.Add(f.Name);
            }


            foreach (string s in list)
            {
                bool contained = folderNameList.Contains(s);
                //Debug.Log("List item " + s + " exists in folders ? " + contained);
                if (!contained)
                {
                    rootDir.CreateSubdirectory(s);
                    addedToFolder.Add(s);
                }
            }

            if (addedToFolder.Count > 0)
            {
                EditorUtility.DisplayDialog("Done!", "These folders have been created under InteriorModules/: " + addedToFolder.Dump(), "Ok");
                AssetDatabase.Refresh();
            }
        }
        private void OnClickAddMissingModules()
        {
            string folderPath = GetFilePath("InteriorModules t:Folder", "InteriorModules", "folder");
            if (string.IsNullOrEmpty(folderPath))
                return;

            obj.myModuleNameList = obj.myModuleList.Select(s => s.prefab).ToList();

            DirectoryInfo rootDir = new DirectoryInfo(folderPath);
            DirectoryInfo[] info = rootDir.GetDirectories();
            List<string> addedToList = new List<string>();

            foreach (DirectoryInfo d in info)
            {
                FileInfo[] info2 = d.GetFiles("*.prefab");
                foreach (FileInfo f in info2)
                {
                    string file = f.Name.Replace(".prefab", "");
                    bool contained = obj.myModuleNameList.Contains(file);
                    //Debug.Log("File " + file + " is in list ? " + contained);

                    if (!contained)
                    {
                        obj.myModuleList.Add(new ModuleDataModels.Module(file, file));
                        addedToList.Add(file);
                    }
                }
            }

            if (addedToList.Count > 0)
            {
                BindListsToObjects();
                EditorUtility.DisplayDialog("Done!", "These list entries have been added: " + addedToList.Dump(), "Ok");
            }
            else
            {
                EditorUtility.DisplayDialog("Done!", "No new module prefabs detected inside InteriorModules/", "Ok");
            }

            CheckMissingPrefabs();
        }

        private void CheckMissingPrefabs()
        {
            string folderPath = GetFilePath("InteriorModules t:Folder", "InteriorModules", "folder");
            if (string.IsNullOrEmpty(folderPath))
                return;

            obj.myModuleNameList = obj.myModuleList.Select(s => s.prefab).ToList();

            DirectoryInfo rootDir = new DirectoryInfo(folderPath);
            DirectoryInfo[] info = rootDir.GetDirectories();
            List<string> fileNameList = new List<string>();
            List<string> missingPrefabs = new List<string>();

            foreach (DirectoryInfo d in info)
            {
                FileInfo[] info2 = d.GetFiles("*.prefab");
                foreach (FileInfo f in info2)
                {
                    string file = f.Name.Replace(".prefab", "");
                    fileNameList.Add(file);
                }
            }

            foreach (string s in obj.myModuleNameList)
            {
                if (string.IsNullOrEmpty(s))
                    continue;

                bool contained = fileNameList.Contains(s);
                //Debug.Log("List item " + s + " exists in files ? " + contained);
                if (!contained)
                {
                    missingPrefabs.Add(s);
                }
            }

            if (missingPrefabs.Count > 0)
            {
                EditorUtility.DisplayDialog("Warning!", "These prefabs are missing inside InteriorModules/: " + missingPrefabs.Dump(), "Ok");
            }
        }

        private void OnClickGenerateAllThumbnails()
        {
            string filePath = GetFilePath("InteriorModuleThumbnails t:Folder", "InteriorModuleThumbnails", "folder");
            if (string.IsNullOrEmpty(filePath))
                return;

            //Debug.Log("filePath:" + filePath);
            //return;

            RuntimePreviewGenerator.BackgroundColor = HDSettings.THUMBNAIL_BACKGROUND;
            RuntimePreviewGenerator.MarkTextureNonReadable = false;
            GameObject[] prefabs = Resources.LoadAll<GameObject>(HDSettings.MODULES_FOLDER);
            if (prefabs.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", "No module prefabs found, make sure you have them created under a Resources folder called " + HDSettings.MODULES_FOLDER, "Ok");
                return;
            }
            int createdCount = 0;
            foreach (GameObject prefab in prefabs)
            {
                Texture2D tex = RuntimePreviewGenerator.GenerateModelPreview(prefab.transform, 256, 256);

                try
                {
                    byte[] _bytes = tex.EncodeToPNG();
                    string path = Path.Combine(filePath, prefab.name + ".png");

                    File.WriteAllBytes(path, _bytes);
                    createdCount++;
                }
                catch (Exception e) { Debug.Log(e.Message); }
            }

            AssetDatabase.Refresh();
            if (createdCount > 0)
            {
                EditorUtility.DisplayDialog("Done!", "Thumbnails created: " + createdCount, "Ok");
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "No thumbnails created, check the console errors!", "Ok");
            }

        }



        private void OnClickOpenModules()
        {
            string filePath = GetFilePath("interior_modules t:TextAsset", "interior_modules.json");
            if (string.IsNullOrEmpty(filePath))
                return;

            string path = Path.Combine(Application.dataPath, "../");
            path = Path.Combine(path, filePath);
            System.Diagnostics.Process.Start(path);
        }

        private void OnClickSaveModules()
        {
            string filePath = GetFilePath("interior_modules t:TextAsset", "interior_modules.json");
            if (string.IsNullOrEmpty(filePath))
                return;

            ModuleList modules = new ModuleList();
            modules.modules = obj.myModuleList;
            string content = JsonConvert.SerializeObject(modules, Formatting.Indented, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });

            if (content != null && content.Length > 2)
            {
                File.WriteAllText(filePath, content, Encoding.UTF8);
                EditorUtility.DisplayDialog("Saved!", "File saved to:" + filePath, "Ok");

            }

            CheckMissingPrefabs();
        }



        private void OnClickOpenTabs()
        {
            string filePath = GetFilePath("interior_categories t:TextAsset", "interior_categories.json");
            if (string.IsNullOrEmpty(filePath))
                return;

            string path = Path.Combine(Application.dataPath, "../");
            path = Path.Combine(path, filePath);
            System.Diagnostics.Process.Start(path);
        }

        private void OnClickSaveTabs()
        {
            string filePath = GetFilePath("interior_categories t:TextAsset", "interior_categories.json");
            if (string.IsNullOrEmpty(filePath))
                return;

            InteriorCategories tabs = new InteriorCategories();
            tabs.folders = obj.myTabList;
            string content = JsonConvert.SerializeObject(tabs, Formatting.Indented, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });

            if (content != null && content.Length > 2)
            {
                File.WriteAllText(filePath, content, Encoding.UTF8);
                EditorUtility.DisplayDialog("Saved!", "File saved to:" + filePath, "Ok");

            }
            string folderPath = GetFilePath("InteriorModules t:Folder", "InteriorModules", "folder");
            if (string.IsNullOrEmpty(folderPath))
                return;

            CreateMissingFolders();
            CreateCategoriesPopups();
        }

        private string GetFilePath(string searchStr, string endsWith, string fileOrFolder = "file", bool alertIfNotFound = true, string resourcesFolder = "Resources/")
        {
            List<string> guids = new List<string>(AssetDatabase.FindAssets(searchStr, new[] { "Assets" }));

            for (int i = 0; i < guids.Count; i++)
            {
                guids[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!guids[i].EndsWith(endsWith))
                {
                    guids.RemoveAt(i);
                    i--;
                }
            }
            if (alertIfNotFound)
            {
                if (guids == null || guids.Count == 0)
                {
                    string resolution = resourcesFolder == null ? ", did you removed it ?" : ", please create it in your " + resourcesFolder + " folder!";
                    EditorUtility.DisplayDialog("Error", "Could not find the " + fileOrFolder + " " + endsWith + " in the project" + resolution, "Ok");
                    return null;
                }
                if (guids.Count > 1)
                {
                    EditorUtility.DisplayDialog("Error", "You have multiple " + fileOrFolder + "s in your project called " + endsWith + ", please keep only a single one!", "Ok");
                    return null;
                }
            }
            if (guids == null || guids.Count == 0)
                return null;
            return guids[0];
        }

        private void SwitchPage(string v, string title)
        {
            //Debug.Log("Switch Page:" + v);
            tabsPage.style.display = v == "tabsPage" ? DisplayStyle.Flex : DisplayStyle.None;
            modulesPage.style.display = v == "modulesPage" ? DisplayStyle.Flex : DisplayStyle.None;
            toolsPage.style.display = v == "toolsPage" ? DisplayStyle.Flex : DisplayStyle.None;
            pageTitle.text = title;
        }
    }
}