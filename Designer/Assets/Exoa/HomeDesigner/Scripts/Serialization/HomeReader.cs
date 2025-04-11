using Exoa.Designer.Utils;
using Exoa.Events;
using System.Collections.Generic;
using UnityEngine;
using static Exoa.Designer.DataModel;
using static Exoa.Events.GameEditorEvents;

namespace Exoa.Designer
{
    public class HomeReader : BaseReader, IDataReader
    {
        private bool floorMapLoaded;
        private List<FloorController> floorsList;
        private FloorMapV2 currentFloorMapFile;
        private InteriorProjectV2 currentInteriorFile;
        private InteriorDesigner id;
        private FloorMapReader fmr;
        private GameObject[] prefabs;

        /*
        public string FileName
        {
            get
            {
                return (InteriorDesigner.instance != null) ? InteriorDesigner.instance.FloorMapFileName : null;
            }

            set
            {
                if (InteriorDesigner.instance != null) InteriorDesigner.instance.FloorMapFileName = value;
            }
        }*/

        void OnDestroy()
        {
            GameEditorEvents.OnRequestClearAll -= OnRequestClearAll;
            GameEditorEvents.OnRequestFloorAction -= OnRequestFloorActionHandler;
        }
        void Awake()
        {
            GameEditorEvents.OnRequestClearAll += OnRequestClearAll;
            GameEditorEvents.OnRequestFloorAction += OnRequestFloorActionHandler;
        }

        private void OnRequestFloorActionHandler(FloorAction action, string fileName)
        {
            if (action == FloorAction.PreviewBuilding)
            {
                ReplaceAndLoad(fileName);
            }
        }

        private void OnRequestClearAll(bool clearFloorsUI, bool clearFloorMapUI, bool clearScene)
        {
            if (clearScene)
            {
                HDLogger.Log("Building Reader OnRequestClearAll floorMapLoaded:" + floorMapLoaded, HDLogger.LogCategory.Floormap);
                Clear();
            }
        }


        public void Clear()
        {
            if (floorsList != null && floorsList.Count > 0)
            {
                for (int i = 0; i < floorsList.Count; i++)
                {
                    if (floorsList[i] != null)
                    {
                        floorsList[i].gameObject.DestroyUniversal();
                    }
                }
            }
            floorsList = new List<FloorController>();
            floorMapLoaded = false;
        }

        override public string GetFolderName()
        {
            return HDSettings.EXT_INTERIOR_FOLDER;
        }

        override public void ReplaceAndLoad(string name, bool sendLoadedEvent = true)
        {
            LoadInternal(name, sendLoadedEvent);
        }

        private void LoadInternal(string name, bool sendLoadedEvent = true)
        {
            HDLogger.Log("Floor Map Reader Load:" + name, HDLogger.LogCategory.Building);

            prefabs = Resources.LoadAll<GameObject>(HDSettings.MODULES_FOLDER);

            if (string.IsNullOrEmpty(name))
            {
                AlertPopup.ShowAlert("emptyFloorMap", "Empty Floor Map", "The floor map name is empty.");
                return;
            }
            GameEditorEvents.OnRequestClearAll?.Invoke(clearFloorsUI: false, clearFloorMapUI: true, clearScene: true);
            SaveSystem.Create(SaveSystem.Mode.FILE_SYSTEM).LoadFileItem(name, GetFolderName(), (string json) =>
            {
                //FileName = name;
                DeserializeToScene(json);
                floorMapLoaded = true;
                string screenshotName = "Building_" + name + "_persp";
                ThumbnailGeneratorUtils.TakeAndSaveScreenshot(transform, screenshotName, false, new Vector3(1, -1, 1));
                GameEditorEvents.OnScreenShotSaved?.Invoke(screenshotName, MenuType.FloorMapMenu);
                GameEditorEvents.OnFileLoaded?.Invoke(GameEditorEvents.FileType.BuildingRead);
            });
        }

        override public object DeserializeToScene(string str)
        {
            Clear();

            HDLogger.Log("DeserializeToScene", HDLogger.LogCategory.Interior);

            currentInteriorFile = DataModel.DeserializeInteriorJsonFile(str);
            //InteriorDesigner.instance.FloorMapFileName = currentInteriorFile.floorMapFile;

            SaveSystem externalFolderSS = SaveSystem.Create(SaveSystem.Mode.FILE_SYSTEM);
            SaveSystem internalFolderSS = SaveSystem.Create(SaveSystem.Mode.RESOURCES);

            externalFolderSS.Exists(currentInteriorFile.floorMapFile, HDSettings.EXT_FLOORMAP_FOLDER, ".json", (bool exists) =>
            {
                if (exists)
                {
                    externalFolderSS.LoadFileItem(currentInteriorFile.floorMapFile,
                        HDSettings.EXT_FLOORMAP_FOLDER, (string json) =>
                    {
                        currentFloorMapFile = DataModel.DeserializeFloorMapJsonFile(json);
                        DeserializeFloorMap(currentInteriorFile, currentFloorMapFile);
                    });
                }
                else
                {
                    internalFolderSS.Exists(currentInteriorFile.floorMapFile,
                        HDSettings.EMBEDDED_FLOORMAP_FOLDER, ".json", (bool exists2) =>
                    {
                        if (exists2)
                        {
                            internalFolderSS.LoadFileItem(currentInteriorFile.floorMapFile, HDSettings.EMBEDDED_FLOORMAP_FOLDER, (string json) =>
                            {
                                currentFloorMapFile = DataModel.DeserializeFloorMapJsonFile(json);
                                DeserializeFloorMap(currentInteriorFile, currentFloorMapFile);
                            });
                        }
                        else
                        {
                            AlertPopup.ShowAlert("fileNotFound", "Error", "The floor map file could not be found");
                        }
                    });
                }
            });

            return currentInteriorFile;
        }

        private void DeserializeFloorMap(InteriorProjectV2 currentInteriorFile, FloorMapV2 currentFloorMapFile)
        {
            FloorMapReader fmr = GetFloorMapReader();
            List<FloorController> fcs = (List<FloorController>)fmr.DeserializeToScene(currentFloorMapFile, -1);
            for (int i = 0; i < fcs.Count; i++)
            {
                InteriorLevel level = currentInteriorFile.GetInteriorLevelByUniqueId(fcs[i].LevelData.uniqueId);
                DeserializeInterior(level, fcs[i]);
            }
        }

        void DeserializeInterior(InteriorLevel level, FloorController fc)
        {
            HDLogger.Log("DeserializeInteriorUI", HDLogger.LogCategory.Interior);

            List<SceneObject> sceneObjects = new List<SceneObject>();

            InteriorDesigner lc = GetInteriorDesigner();

            if (level.sceneObjects != null)
            {
                foreach (SceneObject so in level.sceneObjects)
                {
                    GameObject prefab = TabMenu.FindModuleByName(so.prefabName);
                    //Debug.Log("so.prefabName:" + so.prefabName + " prefab:" + prefab);
                    if (prefab != null)
                    {
                        lc.currentPrefab = prefab;
                        lc.currentPrefabOptions = prefab.GetComponent<ModuleController>();
                        lc.CreateObj(so, true, false, fc.modulesContainer);
                    }
                    else
                    {
                        Debug.LogError("Could not find module " + so.prefabName);
                    }
                }
            }

            lc.DeleteGhost();


            // Set materials 
            BuildingMaterialController bmc = fc.GetBuildingMaterialController();
            if (bmc != null && currentInteriorFile != null)
            {
                bmc.SetBuildingSetting(currentInteriorFile.settings);
            }
            if (currentInteriorFile != null && currentInteriorFile.floors != null && currentInteriorFile.floors.Count > 0)
            {
                List<SpaceMaterialController> rmcs = fc.GetSpaceMaterialControllers();
                for (int i = 0; i < rmcs.Count; i++)
                {
                    if (currentInteriorFile != null && level.roomSettings != null && level.roomSettings.Count > i)
                        rmcs[i].SetRoomSetting(level.roomSettings[i]);
                }
            }
        }

        override public void Unload()
        {
            if (GetInteriorDesigner() != null)
            {
                Destroy(id);
                id = null;
                Destroy(fmr);
                fmr = null;
            }
        }

        private InteriorDesigner GetInteriorDesigner()
        {
            if (id != null) return id;
            id = GetComponent<InteriorDesigner>();
            if (id != null) return id;
            id = gameObject.AddComponent<InteriorDesigner>();
            id.Init();
            id.enabled = false;

            return id;
        }
        private FloorMapReader GetFloorMapReader()
        {
            if (fmr != null) return fmr;
            fmr = GetComponent<FloorMapReader>();
            if (fmr != null) return fmr;
            fmr = gameObject.AddComponent<FloorMapReader>();

            return fmr;
        }

    }
}
