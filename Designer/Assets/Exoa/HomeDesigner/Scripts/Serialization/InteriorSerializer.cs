
using Exoa.Designer.Utils;
using Exoa.Events;
using Exoa.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Exoa.Designer.DataModel;
using static Exoa.Events.GameEditorEvents;

namespace Exoa.Designer
{
    public class InteriorSerializer : BaseSerializable, IDataSerializer
    {
        private Transform modulesContainer;
        private Transform globalContainer;
        public TabMenu tabMenu;
        private InteriorProjectV2 currentInteriorFile;
        private FloorMapV2 currentFloorMapFile;
        private bool initialized;
        private UIFloorsMenu floorsMenu;
        private FloorMapReader floorMapReader;
        override public string GetFolderName() => HDSettings.EXT_INTERIOR_FOLDER;
        override public GameEditorEvents.FileType GetFileType() => FileType.InteriorFile;

        void OnDestroy()
        {
            GameEditorEvents.OnFileSaved -= OnFileSaved;
            GameEditorEvents.OnFileLoaded -= OnFileLoaded;
            GameEditorEvents.OnRequestClearAll -= Clear;
            GameEditorEvents.OnRequestFloorAction -= OnRequestFloorActionHandler;
        }
        void Start()
        {
            Init();
        }
        void Init()
        {
            if (initialized) return;
            initialized = true;
            floorMapReader = GetComponent<FloorMapReader>();
            floorsMenu = GameObject.FindObjectOfType<UIFloorsMenu>();
            modulesContainer = GameObject.Find("ModulesContainer").transform;
            globalContainer = transform;// GameObject.Find("GlobalContainer").transform;

            Clear(clearScene: true);

            GameEditorEvents.OnFileSaved += OnFileSaved;
            GameEditorEvents.OnFileLoaded += OnFileLoaded;
            GameEditorEvents.OnRequestClearAll += Clear;
            GameEditorEvents.OnRequestFloorAction += OnRequestFloorActionHandler;
        }

        private void OnRequestFloorActionHandler(FloorAction action, string floorId)
        {
            switch (action)
            {
                case FloorAction.Select: SelectFloor(floorId); break;
            }
        }



        private void OnFileLoaded(FileType fileType)
        {
            /* HDLogger.Log("Interior OnFileLoaded type:" + fileType, HDLogger.LogCategory.Interior);

             if (fileType != FileType.InteriorFile)
                 return;

             BuildingMaterialController bmc = GameObject.FindObjectOfType<BuildingMaterialController>();
             if (bmc != null && currentInteriorFile != null)
             {
                 bmc.SetBuildingSetting(currentInteriorFile.settings);
             }
             if (currentInteriorFile != null && currentInteriorFile.floors != null && currentInteriorFile.floors.Count > 0)
             {
                 RoomMaterialController[] rmcs = GameObject.FindObjectsOfType<RoomMaterialController>();
                 for (int i = 0; i < rmcs.Length; i++)
                 {
                     if (currentInteriorFile != null && currentInteriorFile.floors[0].roomSettings.Count > i)
                         rmcs[i].SetRoomSetting(currentInteriorFile.floors[0].roomSettings[i]);
                 }
             }*/
        }
        private void OnFileSaved(string fileName, FileType fileType)
        {
            if (fileType != FileType.InteriorFile)
                return;


            string perspViewName = fileName.Replace(".json", "") + "_persp";
            ThumbnailGeneratorUtils.TakeAndSaveScreenshot(globalContainer, HDSettings.INTERIOR_DESIGNER_THUMBNAIL_PREFIX + perspViewName, false, new Vector3(1, -1, 1));
            GameEditorEvents.OnScreenShotSaved?.Invoke(perspViewName, MenuType.InteriorMenu);
        }

        public void Clear(bool clearFloorsUI = false, bool clearFloorMapUI = false, bool clearScene = false)
        {
            if (!clearScene) return;

            HDLogger.Log("Interior Clear", HDLogger.LogCategory.Interior);
            modulesContainer.ClearChildren();
        }

        override public object DeserializeToScene(string str)
        {
            Init();

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
                            DeserializeProjectUI(currentInteriorFile, currentFloorMapFile);
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
                                    DeserializeProjectUI(currentInteriorFile, currentFloorMapFile);
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

        private void DeserializeProjectUI(InteriorProjectV2 interiorFile, FloorMapV2 floorMapFile)
        {
            // Opening the first floor in Floor Map spaces menu
            if (interiorFile.floors != null && interiorFile.floors.Count > 0 && !string.IsNullOrEmpty(interiorFile.floorMapFile))
            {
                DeserializeFloorMapProjectUI(floorMapFile);
            }
        }
        void DeserializeInterior(InteriorLevel level, FloorController fc)
        {
            HDLogger.Log("DeserializeInteriorUI", HDLogger.LogCategory.Interior);

            List<SceneObject> sceneObjects = new List<SceneObject>();
            GameObject[] prefabs = Resources.LoadAll<GameObject>(HDSettings.MODULES_FOLDER);
            InteriorDesigner lc = GameObject.FindObjectOfType<InteriorDesigner>();

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
                        lc.CreateObj(so, true, false);
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
                    if (rmcs[i] != null && currentInteriorFile != null && level.roomSettings != null && level.roomSettings.Count > i)
                        rmcs[i].SetRoomSetting(level.roomSettings[i]);
                }
            }
        }


        private void DeserializeFloorMapProjectUI(FloorMapV2 floorMapProject)
        {
            if (floorMapProject.floors == null)
                return;

            // Filling settings menu
            if (floorMapProject.settings.wallsHeight != 0)
            {
                AppController.Instance.SetFloorMapSettings(floorMapProject.settings);
            }


            // Showing all floors in the Floors menu
            for (int i = 0; i < floorMapProject.floors.Count; i++)
            {
                FloorMapLevel floor = floorMapProject.floors[i];
                floor.GenerateUniqueId();
                floorMapProject.floors[i] = floor;
                floorsMenu.CreateNewUIItem(floorMapProject.floors[i]);
            }
            //setting the first level as current
            if (floorMapProject.floors != null && floorMapProject.floors.Count > 0)
            {
                SelectFloor(0);
            }


        }
        private void SelectFloor(int floorIndex)
        {
            floorsMenu.CurrentFloorId = currentFloorMapFile.floors[floorIndex].uniqueId;
            List<FloorController> fcs = (List<FloorController>)floorMapReader.DeserializeToScene(currentFloorMapFile, floorIndex);
            DeserializeInterior(currentInteriorFile.GetInteriorLevelByUniqueId(floorsMenu.CurrentFloorId), fcs[0]);
        }

        private void SelectFloor(string floorId)
        {
            floorsMenu.CurrentFloorId = floorId;
            List<FloorController> fcs = (List<FloorController>)floorMapReader.DeserializeToScene(currentFloorMapFile, floorId);
            DeserializeInterior(currentInteriorFile.GetInteriorLevelByUniqueId(floorId), fcs[0]);
        }





        #region SERIALIZATION

        override public string SerializeScene()
        {
            HDLogger.Log("[InteriorSerializer] SerializeScene", HDLogger.LogCategory.Interior);

            if (currentInteriorFile == null)
                return "";

            // Saving the current interior
            int foundIndex = -1;
            if (currentInteriorFile.floors != null && currentInteriorFile.floors.Count > 0)
            {
                for (int i = 0; i < currentInteriorFile.floors.Count; i++)
                {
                    //print("currentInteriorFile.floors[i].floorUniqueId:" + currentInteriorFile.floors[i].floorUniqueId);

                    if (currentInteriorFile.floors[i].floorUniqueId == floorsMenu.CurrentFloorId ||
                        currentInteriorFile.floors[i].floorUniqueId == null)
                    {
                        foundIndex = i;
                    }
                }
            }
            // Get the floor to save
            FloorController fc = GetComponentInChildren<FloorController>();

            if (fc == null)
            {
                HDLogger.LogError("Could not find a floor to save", HDLogger.LogCategory.General);
                return null;
            }
            InteriorLevel level = new InteriorLevel();
            level.floorUniqueId = floorsMenu.CurrentFloorId;
            List<SceneObject> sol = new List<SceneObject>();

            for (int j = 0; j < modulesContainer.transform.childCount; j++)
            {
                SceneObject so = new SceneObject();
                Transform t = modulesContainer.transform.GetChild(j);
                ModuleColorVariants variant = t.gameObject.GetComponentInChildren<ModuleColorVariants>();
                if (variant != null)
                {
                    //print("variant.SelectedColor:" + variant.SelectedColor);
                    //print("variant.SelectedMaterialName:" + variant.SelectedMaterialName);

                    so.colorVariant = variant.SelectedColor;
                    so.materialVariantName = variant.SelectedMaterialName;
                }
                so.position = t.localPosition;
                so.rotation = t.eulerAngles - t.GetComponent<ModuleController>().initRotation;
                so.scale = t.localScale;
                so.prefabName = t.gameObject.name;
                sol.Add(so);
            }
            level.sceneObjects = sol;
            level.roomSettings = new List<RoomSetting>();
            List<SpaceMaterialController> rmcs = fc.GetSpaceMaterialControllers();
            for (int j = 0; j < rmcs.Count; j++)
            {
                level.roomSettings.Add((RoomSetting)rmcs[j].GetRoomSetting());
            }
            if (foundIndex > -1)
                currentInteriorFile.floors[foundIndex] = level;
            else currentInteriorFile.floors.Add(level);


            BuildingMaterialController bmc = fc.GetBuildingMaterialController();
            if (bmc != null)
                currentInteriorFile.settings = (BuildingSetting)bmc.GetBuildingSetting();

            return JsonConvert.SerializeObject(currentInteriorFile, Formatting.Indented, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });
        }

        override public bool IsSceneEmpty()
        {
            return modulesContainer.childCount == 0;
        }

        public string SerializeEmpty()
        {
            InteriorProjectV2 project = new InteriorProjectV2();
            project.version = "v2";
            project.settings = new BuildingSetting();
            project.floors = new List<InteriorLevel>();
            project.floors.Add(new InteriorLevel());
            return JsonConvert.SerializeObject(project, Formatting.Indented, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });
        }

        override public string SerializeEmpty(string floorMapFileName)
        {
            InteriorProjectV2 project = new InteriorProjectV2();
            project.version = "v2";
            project.settings = new BuildingSetting();
            project.floorMapFile = floorMapFileName;
            project.floors = new List<InteriorLevel>();
            project.floors.Add(new InteriorLevel());
            return JsonConvert.SerializeObject(project, Formatting.Indented, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });
        }
        #endregion
    }
}
