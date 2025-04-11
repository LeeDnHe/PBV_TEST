using Exoa.Cameras;
using Exoa.Common;
using Exoa.Designer.Data;
using Exoa.Effects;
using Exoa.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using static Exoa.Designer.DataModel;

namespace Exoa.Designer
{
    /// <summary>
    /// Class representing an interior designer in a Unity scene.
    /// </summary>
    /// <remarks>
    /// This class handles the creation, manipulation, and deletion of interior design elements within the scene.
    /// </remarks>
    public class InteriorDesigner : MonoBehaviour
    {
        private bool initialized;
        [HideInInspector]
        public Grid grid;
        public static UnityEvent OnChangeAnything = new UnityEvent();

        private bool overUI;
        public Material transparentMaterial;
        private GameObject ghostObject;

        private float turns;
        private float scale = 1;
        protected Vector3 lastPos;
        protected float lastPaint;
        public float delayBetweenPaint = 1f;
        public float ceilHeight = 3f;
        protected float yPosition;

        private LayerMask wallMask;
        private LayerMask wallAndFloorMask;
        private LayerMask jointMask;
        private LayerMask modulesMask;
        private LayerMask ceilMask;
        private LayerMask roomMask;

        private List<ModuleController> camGhostedObjs = new List<ModuleController>();
        private Transform lastWall;
        public float alpha = .6f;
        private bool escapePressed;

        private GameObject selectedObject;

        public float wallDetectionRadius = .8f;
        public float jointDetectionRadius = .8f;
        public float angleToAvoidWall = 140;

        private Transform modulesContainer;
        [HideInInspector]
        public GameObject currentPrefab;
        [HideInInspector]
        public ModuleController currentPrefabOptions;
        protected GameObject[] prefabs;

        protected List<SceneObject> sceneObjects;
        protected List<GameObject> moduleList;
        public float moduleRotationStep = 90f;
        protected GameObject lastObject;
        public float scaleMultiplier = 1;
        public static InteriorDesigner instance;
        private GameObject lastOutlinedObject;
        /*private string floorMapFileName;

        public string FloorMapFileName
        {
            get
            {
                return floorMapFileName;
            }

            set
            {
                floorMapFileName = value;
            }
        }*/

        private void OnDestroy()
        {
            AppController.OnAppStateChange -= OnAppStateChange;
        }

        public void Awake()
        {
            Init();
        }

        /// <summary>
        /// Initializes the object by setting up various lists and masks.
        /// </summary>
        /// <remarks>
        /// This method initializes the object by creating new lists for game objects and scene objects, 
        /// finding the grid component, and setting up event listeners for delete and move actions in the InfoPopup.
        /// It also sets up layer masks for different types of objects such as room, wall, floor, joint, modules, and ceiling.
        /// If the instance is null, it assigns the current instance to it.
        /// </remarks>
        public void Init()
        {
            if (initialized)
                return;

            moduleList = new List<GameObject>();

            //levelLoaded = false;
            sceneObjects = new List<SceneObject>();

            grid = FindObjectOfType<Grid>();

            InfoPopup.OnClickDelete.AddListener(DeleteSelectedObject);
            InfoPopup.OnClickMove.AddListener(OnClickMove);

            roomMask.value = Layers.FloorMask | Layers.WallMask | Layers.ModuleMask | Layers.ExteriorWallMask | Layers.RoofMask;
            wallMask.value = Layers.WallMask;
            wallAndFloorMask.value = Layers.FloorMask | Layers.WallMask;
            jointMask.value = Layers.JoinMask;
            modulesMask.value = Layers.ModuleMask;
            ceilMask.value = Layers.CeilMask;

            if (instance == null)
            {
                instance = this;
            }
            initialized = true;
        }

        public void Start()
        {
            modulesContainer = GameObject.Find("ModulesContainer").transform;

            LoadPrefabs();

            AppController.OnAppStateChange += OnAppStateChange;
        }

        /// <summary>
        /// Handles the change in the application state.
        /// </summary>
        /// <param name="state">The new state of the application.</param>
        /// <remarks>
        /// If the state is PreviewBuilding, hides the info popup, material popup, and module menu controller.
        /// Otherwise, shows the module menu controller.
        /// </remarks>
        private void OnAppStateChange(AppController.States state)
        {
            if (state == AppController.States.PreviewBuilding)
            {
                InfoPopup.Instance.Hide();
                MaterialPopup.Instance.Hide();
                ModuleMenuController.Instance.Hide();
            }
            else
            {
                ModuleMenuController.Instance.Show();
            }
        }

        private void LoadPrefabs()
        {
            prefabs = Resources.LoadAll<GameObject>(HDSettings.MODULES_FOLDER);
        }


        /// <summary>Finds the parent GameObject with a specified tag.</summary>
        /// <param name="go">The starting GameObject to search from.</param>
        /// <param name="tag">The tag to search for in the parent GameObjects.</param>
        /// <returns>The parent GameObject with the specified tag, or null if not found.</returns>
        public static GameObject FindParentObjectWithTag(GameObject go, string tag)
        {
            GameObject module = null;
            //if (tag == Tags.Room) print("go.tag:" + go.tag);
            while (go.tag != tag && go.transform.parent != null || go.name == "Collider")
            {
                go = go.transform.parent.gameObject;
                //if (tag == Tags.Room) print("go.tag:" + go.tag + " go.transform.parent:" + go.transform.parent);
            }
            //if (tag == Tags.Room) print("go.tag == tag:" + (go.tag == tag) + " go.tag:" + go.tag + " tag:" + tag);
            if (go.tag == tag)
                module = go;
            //if (tag == Tags.Room) print("module:" + module);
            return module;
        }



        public GameObject CreateObj(SceneObject so, bool isSceneObject, bool applyScaleMultiplier)
        {
            return CreateObj(so, isSceneObject, applyScaleMultiplier, modulesContainer);
        }
        public GameObject CreateObj(SceneObject so, bool isSceneObject, bool applyScaleMultiplier, Transform parent)
        {
            //print("Create Obj:" + currentPrefab.name);
            so.prefabName = currentPrefab.name;

            lastObject = Instantiate(currentPrefab);
            lastObject.name = currentPrefab.name;


            if (applyScaleMultiplier)
                lastObject.transform.localScale *= scaleMultiplier;

            if (isSceneObject)
            {
                lastObject.transform.parent = parent;
                sceneObjects.Add(so);
            }
            lastObject.transform.localPosition = so.position;
            lastObject.transform.rotation = Quaternion.Euler(so.rotation) * lastObject.GetComponent<ModuleController>().GetInitRotation();
            lastObject.transform.localScale = so.scale == Vector3.zero ? lastObject.transform.localScale : so.scale;

            ModuleColorVariants variant = lastObject.GetComponentInChildren<ModuleColorVariants>();
            if (variant != null)
            {
                if (!string.IsNullOrEmpty(so.materialVariantName))
                    variant.ApplyModuleMaterial(so.materialVariantName);
                else variant.ApplyModuleColor(so.colorVariant);
            }

            moduleList.Add(lastObject);

            OnChangeAnything.Invoke();

            return lastObject;

        }

        public Vector3 GetCenterPoint()
        {
            Bounds bounds = new Bounds();
            foreach (SceneObject so in sceneObjects)
            {
                if (so.prefabName.Contains("Cell"))
                {
                    bounds.Encapsulate(so.position);
                }
            }
            return bounds.center;
        }

        public SceneObject FindSceneObjectAtPosition(Vector3 p, float threshold = 0.1f)
        {
            foreach (SceneObject so in sceneObjects)
                if (Vector3.Distance(so.position, p) < threshold)
                    return so;
            return new SceneObject();
        }



        public void SelectPrefab(string name)
        {
            if (prefabs == null)
                return;

            escapePressed = false;
            //print("SelectPrefab:" + name);

            foreach (GameObject p in prefabs)
            {
                if (p.name == (name))
                {
                    currentPrefab = p;
                    currentPrefabOptions = p.GetComponent<ModuleController>();
                    return;
                }
            }
            Debug.LogError("No prefab found with the name:" + name);

            scale = 1;
            turns = 0;
        }














        /// <summary>
        /// Handles some keyboard shortcuts
        /// </summary>
        private void OnGUI()
        {
            if (HDInputs.EscapePressed())
                DeleteGhost();


        }





        /// <summary>
        /// Main logic is here
        /// </summary>
        private void Update()
        {
            //print(" currentPrefabOptions:" + currentPrefabOptions + " grid:" + grid);
            if (grid == null)
                return;

            if (AppController.Instance == null ||
                AppController.Instance.State == AppController.States.PreviewBuilding)
                return;

            //HandleFullWallGhosting();

            RaycastHit hhit = new RaycastHit();
            RaycastHit modulesHitInfo;
            Ray camToMouseRay = Camera.main.ScreenPointToRay(BaseTouchInput.GetMousePosition());
            overUI = HDInputs.IsOverUI;
            bool altPressed = HDInputs.AltPressed();
            bool isTap = CameraInputs.IsTap() || CameraInputs.IsUp();
            //print("currentPrefabOptions:" + currentPrefabOptions + " currentPrefab:" + currentPrefab);



            if (!overUI && currentPrefabOptions == null && isTap)
            {
                // look for a room 
                RaycastHit hit;
                if (Physics.Raycast(camToMouseRay, out hit, 100, roomMask.value))
                {
                    GameObject m = FindParentObjectWithTag(hit.collider.gameObject, Tags.Wall);
                    //print("selected wall:" + m + " collider:" + hit.collider.gameObject);
                    if (m != null)
                    {
                        selectedObject = m;
                        AddObjectOutline(selectedObject);
                        //CameraEvents.OnRequestObjectFocus?.Invoke(hit.collider.gameObject, false);
                        MaterialPopup.Instance.ShowMode(MaterialPopupUI.Mode.InteriorWall, selectedObject);
                        InfoPopup.Instance.Hide();
                    }

                    m = FindParentObjectWithTag(hit.collider.gameObject, Tags.ExteriorWall);
                    //print("selected exterior wall:" + m + " collider:" + hit.collider.gameObject);
                    if (m != null)
                    {
                        selectedObject = m;
                        AddObjectOutline(selectedObject);
                        //CameraEvents.OnRequestObjectFocus?.Invoke(hit.collider.gameObject, false);
                        MaterialPopup.Instance.ShowMode(MaterialPopupUI.Mode.ExteriorWall, selectedObject);
                        InfoPopup.Instance.Hide();
                    }

                    m = FindParentObjectWithTag(hit.collider.gameObject, Tags.Floor);
                    //print("selected wall:" + m + " collider:" + hit.collider.gameObject);
                    if (m != null)
                    {
                        selectedObject = m;
                        AddObjectOutline(selectedObject);
                        //CameraEvents.OnRequestObjectFocus?.Invoke(hit.collider.gameObject, false);
                        MaterialPopup.Instance.ShowMode(MaterialPopupUI.Mode.Floor, selectedObject);
                        InfoPopup.Instance.Hide();
                    }

                    /* m = FindParentObjectWithTag(hit.collider.gameObject, Tags.Ceil);
                     //print("selected Ceil:" + m + " collider:" + hit.collider.gameObject);
                     if (m != null)
                     {
                         selectedObject = m;
                         AddObjectOutline(selectedObject);
                         CameraEvents.OnRequestObjectFocus?.Invoke(hit.collider.gameObject);
                         MaterialPopup.Instance.ShowMode(WallSettings.Mode.Ceiling, selectedObject);
                         InfoPopup.Instance.Hide();
                     }*/

                    m = FindParentObjectWithTag(hit.collider.gameObject, Tags.Roof);
                    //print("selected Roof:" + m + " collider:" + hit.collider.gameObject);
                    if (m != null)
                    {
                        selectedObject = m;
                        AddObjectOutline(selectedObject);
                        //CameraEvents.OnRequestObjectFocus?.Invoke(hit.collider.gameObject, false);
                        MaterialPopup.Instance.ShowMode(MaterialPopupUI.Mode.Roof, selectedObject);
                        InfoPopup.Instance.Hide();
                    }
                    m = FindParentObjectWithTag(hit.collider.gameObject, Tags.Outside);
                    //print("selected Roof:" + m + " collider:" + hit.collider.gameObject);
                    if (m != null)
                    {
                        selectedObject = m;
                        AddObjectOutline(selectedObject);
                        //CameraEvents.OnRequestObjectFocus?.Invoke(hit.collider.gameObject, false);
                        MaterialPopup.Instance.ShowMode(MaterialPopupUI.Mode.Outside, selectedObject);
                        InfoPopup.Instance.Hide();
                    }

                    // Clicking on an already placed module will show a popup with actions
                    m = FindParentObjectWithTag(hit.collider.gameObject, Tags.Module);
                    //print("selected module:" + m + " collider:" + hit.collider.gameObject);
                    if (m != null)
                    {
                        selectedObject = m;
                        if (altPressed)
                        {
                            // ALT key pressed, remove the object 
                            DeleteSelectedObject();
                        }
                        else
                        {

                            AddObjectOutline(selectedObject);
                            CameraEvents.OnRequestObjectFocus?.Invoke(selectedObject, false);
                            ModuleDataModels.Module p = AppController.Instance.GetModuleByPrefab(selectedObject.name);
                            InfoPopup.Instance.Show(selectedObject.transform, p, true, false);
                            MaterialPopup.Instance.Hide();
                        }

                    }
                }
            }
            if (!overUI && currentPrefabOptions != null)
            {

                bool isRotating = CameraModeSwitcher.Instance.IsRotating();

                // Raycast from camera to mouse position to find objects
                RaycastHit[] hits;
                if (currentPrefabOptions.isCeilTile)
                    hits = Physics.RaycastAll(camToMouseRay, 100, ceilMask.value);
                else if (currentPrefabOptions.isGroundTile)
                    hits = Physics.RaycastAll(camToMouseRay, 100, wallAndFloorMask.value).OrderByDescending(hit2 => hit2.distance).ToArray();
                else
                    hits = Physics.RaycastAll(camToMouseRay, 100, modulesMask.value).OrderByDescending(hit2 => hit2.distance).ToArray();


                bool moduleRaycast = Physics.Raycast(camToMouseRay, out modulesHitInfo, 100, modulesMask.value);
                if (hits.Length > 0)
                {
                    // modify the raycast hit point to snap on the ground
                    hhit = hits[0];
                    //print("groundAndWallHitInfo:" + groundAndWallHitInfo.collider.name);

                    Vector3 modifiedHitPoint = hhit.point;

                    if (currentPrefabOptions.isGroundTile)
                        modifiedHitPoint.y = 0;
                    else if (currentPrefabOptions.isCeilTile)
                        modifiedHitPoint.y = ceilHeight;

                    Vector3 gridPos = currentPrefabOptions.snapOnGrid ? grid.GetNearestPointOnGrid(modifiedHitPoint) : modifiedHitPoint;
                    SceneObject sceneObject = FindSceneObjectAtPosition(gridPos, 0.1f);

                    bool positionTaken = sceneObject.prefabName != null;


                    if (altPressed)
                    {
                        DeleteGhost();
                    }

                    if (!isRotating && !isTap && !escapePressed)
                    {
                        // Move the ghost around
                        if (ghostObject == null || ghostObject.name != currentPrefab.name)
                        {
                            CreateGhost(sceneObject, gridPos);
                        }

                        // Move the current ghost object to mouse position on the floor
                        ghostObject.transform.position = gridPos;
                        ghostObject.transform.rotation = Quaternion.Euler(0, 90 * turns, 0) * ghostObject.GetComponent<ModuleController>().GetInitRotation();

                        // Then snap it to other objects or walls
                        Snap(ghostObject, gridPos);
                    }
                    else
                    {
                        //print("positionTaken:" + positionTaken + " altPressed:" + altPressed + " mouseDown:" + mouseDown + " clickOnUI:" + clickOnUI + " ghostObject:" + ghostObject);
                        if (!positionTaken && !altPressed && ghostObject != null)
                        {
                            if (lastPaint > Time.time - delayBetweenPaint)
                                return;

                            if (!isTap)
                                return;

                            if (overUI)
                                return;

                            // Create the real object when we click on the room floor
                            lastPaint = Time.time;
                            sceneObject.rotation = ghostObject.transform.rotation.eulerAngles;// new Vector3(0, 90 * turns, 0);
                            sceneObject.position = ghostObject.transform.position;// gridPos;
                            sceneObject.scale = Vector3.one * scale;
                            GameObject realObject = CreateObj(sceneObject, true, true, modulesContainer);
                            DeleteGhost();
                            SetJointsTaken(realObject);
                        }
                        else if (!isTap && modulesHitInfo.collider != null && ghostObject == null)
                        {
                            GameObject m = FindParentObjectWithTag(modulesHitInfo.collider.gameObject, Tags.Module);
                            if (m != null)
                            {
                                selectedObject = m;
                                AddObjectOutline(selectedObject);
                            }
                        }
                    }
                }
                else
                {
                    if (isTap)
                    {
                        // clicking elsewhere will hide the popup
                        HideOutlineAndPopups();
                    }
                }
            }
            if (BaseTouchInput.GetMouseWentDown(1))
            {
                HideOutlineAndPopups();
            }

            // Rotate the current ghost block when pressing Left/Right
            if ((BaseTouchInput.GetKeyWentDown(KeyCode.LeftArrow) || BaseTouchInput.GetKeyWentDown(KeyCode.RightArrow)))
            {
                turns += Mathf.Round(BaseTouchInput.KeyboardXAxis() > 0 ? 1 : -1);
            }

            // Scale the current ghost block when pressing Up/Down
            if ((BaseTouchInput.GetKeyWentDown(KeyCode.UpArrow) || BaseTouchInput.GetKeyWentDown(KeyCode.DownArrow)))
            {
                scale *= BaseTouchInput.KeyboardYAxis() > 0 ? 1.1f : .9f;
                if (ghostObject != null) ghostObject.transform.localScale *= Input.GetAxis("Vertical") > 0 ? 1.1f : .9f;
            }
        }

        private void HideOutlineAndPopups()
        {
            InfoPopup.Instance.Hide();
            MaterialPopup.Instance.Hide();

            AddObjectOutline(null);
        }


        private void AddObjectOutline(GameObject go)
        {
            if (lastOutlinedObject == go)
                return;

            if (lastOutlinedObject != null)
            {
                lastOutlinedObject.GetComponent<OutlineHandler>().ShowOutline(false);
            }
            if (go != null)
            {
                OutlineHandler b = go.GetComponent<OutlineHandler>();
                if (b == null) b = go.AddComponent<OutlineHandler>();
                b.ShowOutline(true, 0);

            }
            lastOutlinedObject = go;
        }

        private void SetJointsTaken(GameObject realObject)
        {
            //Join.SetJointsTaken(jointMask, realObject.transform, jointDetectionRadius);
        }




        /// <summary>
        /// When the camera it just behind a wall, we ghost all objects close to it
        /// </summary>
        private void HandleFullWallGhosting()
        {
            RaycastHit hit;
            if (Physics.Linecast(Camera.main.transform.position, Vector3.up * .5f, out hit, wallMask.value))
            {
                Transform wall = hit.collider.transform;
                if (wall != lastWall)
                {
                    UnghostModules();
                    lastWall = wall;
                    ModuleController[] modules = GameObject.FindObjectsOfType<ModuleController>();
                    foreach (ModuleController b in modules)
                    {
                        Transform LJoint = b.transform.Find("LJoint");
                        if (LJoint == null) LJoint = b.transform;

                        if (Mathf.Abs(wall.InverseTransformPoint(LJoint.position).z) < 5 && !b.isGhost)
                        {
                            b.Ghost(transparentMaterial, .1f);
                            camGhostedObjs.Add(b);
                        }
                    }
                }
            }
            else
            {

                UnghostModules();
                lastWall = null;
            }
        }




        /// <summary>
        /// Create a ghost object
        /// </summary>
        private void CreateGhost(SceneObject sceneObject, Vector3 gridPos)
        {
            //print("CreateGhost " + sceneObject.prefabName);

            if (ghostObject != null)
                DestroyImmediate(ghostObject);

            sceneObject.rotation = new Vector3(0, moduleRotationStep * turns, 0);
            sceneObject.position = gridPos;
            sceneObject.scale = Vector3.one * scale;
            CreateObj(sceneObject, false, true, null);
            ghostObject = lastObject;
            ghostObject.ApplyLayerRecursively("Ghost");
            ghostObject.GetComponent<ModuleController>().isGhost = true;

            MeshRenderer[] renderers = ghostObject.GetComponentsInChildren<MeshRenderer>();

            foreach (MeshRenderer r in renderers)
            {
                r.material = transparentMaterial;
                r.material.color = new Color(1, 1, 1, alpha);
            }
        }



        /// <summary>
        /// Delete the current ghost object
        /// </summary>
        public void DeleteGhost()
        {
            currentPrefab = null;
            currentPrefabOptions = null;
            escapePressed = true;
            if (ghostObject != null)
                DestroyImmediate(ghostObject);
        }



        /// <summary>
        /// Unghost all modules that were ghosted with HandleFullWallGhosting()
        /// </summary>
        private void UnghostModules()
        {
            for (int i = 0; i < camGhostedObjs.Count; i++)
            {
                if (camGhostedObjs[i] != null)
                    camGhostedObjs[i].Unghost();
                camGhostedObjs.RemoveAt(i);
                i--;
            }
        }



        /// <summary>
        /// Snap the current ghost object to closest wall, joints, and block it inside the room
        /// </summary>
        private void Snap(GameObject obj, Vector3 mousePos)
        {
            Join.SnapToWalls(wallMask, obj.transform, mousePos, wallDetectionRadius, angleToAvoidWall);
            Join.SnapToJointsLeftRight(jointMask, obj.transform, mousePos, jointDetectionRadius);
            Join.SnapToJointsFacing(jointMask, obj.transform, mousePos, jointDetectionRadius);
            Join.SnapInsideRoom(obj.transform);
        }






        /// <summary>
        /// Remove an object from the room
        /// </summary>
        /// <param name="obj"></param>
        private void DeleteObj(GameObject obj)
        {
            //print("DeleteObj grid:" + grid + " obj:" + obj);
            if (grid == null || obj == null)
            {
                Debug.LogError("Cound not delete object");
                return;
            }
            Vector3 gridPos = grid.GetNearestPointOnGrid(obj.transform.position);
            SceneObject sceneObject = FindSceneObjectAtPosition(gridPos);
            sceneObjects.Remove(sceneObject);
            DestroyImmediate(obj);

            OnChangeAnything.Invoke();
        }














        /// <summary>
        /// Clicking the "delete" button on the popup, or pressing alt + click on the object
        /// </summary>
        private void DeleteSelectedObject()
        {
            DeleteObj(selectedObject);
            selectedObject = null;
            InfoPopup.Instance.Hide();
        }



        /// <summary>
        ///  Clicking the "move" button on the popup
        ///  will recreate a ghost object that is editable
        /// </summary>
        private void OnClickMove()
        {
            escapePressed = false;
            SelectPrefab(selectedObject.name);
            ghostObject = selectedObject;
            ghostObject.ApplyLayerRecursively("Ghost");
            ModuleController m = ghostObject.GetComponent<ModuleController>();
            m.isGhost = true;
            m.Ghost(transparentMaterial, alpha);
        }


    }
}
