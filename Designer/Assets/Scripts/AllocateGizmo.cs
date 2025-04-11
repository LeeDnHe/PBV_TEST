using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Exoa.Effects;
using RTG;
using RuntimeInspectorNamespace;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

public class AllocateGizmo : MonoBehaviour
{
    /// <summary>
    /// A private enum which is used by the class to differentiate between different 
    /// gizmo types. An example where this enum will come in handy is when we use the 
    /// W,E,R,T keys to switch between different types of gizmos. When the W key is 
    /// pressed for example, we will call the 'SetWorkGizmoId' function passing 
    /// GizmoId.Move as the parameter.
    /// </summary>
    private enum GizmoId
    {
        Move = 1,
        Rotate = 2,
        Scale = 3,
        Universal = 4
    }

    /// <summary>
    /// The following 4 variables are references to the ObjectTransformGizmo behaviours
    /// that will be used to move, rotate and scale our objects.
    /// </summary>
    private ObjectTransformGizmo _objectMoveGizmo;
    private ObjectTransformGizmo _objectRotationGizmo;
    private ObjectTransformGizmo _objectScaleGizmo;
    private ObjectTransformGizmo _objectUniversalGizmo;

    /// <summary>
    /// The current work gizmo id. The work gizmo is the gizmo which is currently used
    /// to transform objects. The W,E,R,T keys can be used to change the work gizmo as
    /// needed.
    /// </summary>
    private GizmoId _workGizmoId;
    /// <summary>
    /// A reference to the current work gizmo. If the work gizmo id is GizmoId.Move, then
    /// this will point to '_objectMoveGizmo'. For GizmoId.Rotate, it will point to 
    /// '_objectRotationGizmo' and so on.
    /// </summary>
    private ObjectTransformGizmo _workGizmo;
    /// <summary>
    /// A reference to the target object. This is the object that will be manipulated by
    /// the gizmos and it will always be picked from the scene via a mouse click. This will
    /// be set to null when the user clicks in thin air.
    /// </summary>
    private GameObject _targetObject;
        
    public TMP_Dropdown dropdown;
    
    public Transform hierarchyTransform;
    public RuntimeInspector runtimeInspector;
    [SerializeField] private ApplyColorController applyColorController;
    
    private List<Outlinable> _outlinables = new List<Outlinable>();

    private float preValue;
    
    /// <summary>
    /// Performs all necessary initializations.
    /// </summary>
    private void Start()
    {
        // Create the 4 gizmos
        _objectMoveGizmo = RTGizmosEngine.Get.CreateObjectMoveGizmo();
        _objectRotationGizmo = RTGizmosEngine.Get.CreateObjectRotationGizmo();
        _objectScaleGizmo = RTGizmosEngine.Get.CreateObjectScaleGizmo();
        _objectUniversalGizmo = RTGizmosEngine.Get.CreateObjectUniversalGizmo();

        // Call the 'SetEnabled' function on the parent gizmo to make sure
        // the gizmos are initially hidden in the scene. We want the gizmo
        // to show only when we have a target object available.
        _objectMoveGizmo.Gizmo.SetEnabled(false);
        _objectRotationGizmo.Gizmo.SetEnabled(false);
        _objectScaleGizmo.Gizmo.SetEnabled(false);
        _objectUniversalGizmo.Gizmo.SetEnabled(false);

        // We initialize the work gizmo to the move gizmo by default. This means
        // that the first time an object is clicked, the move gizmo will appear.
        // You can change the default gizmo, by simply changing these 2 lines of
        // code. For example, if you wanted the scale gizmo to be the default work
        // gizmo, replace '_objectMoveGizmo' with '_objectScaleGizmo' and GizmoId.Move
        // with GizmoId.Scale.
        _workGizmo = _objectMoveGizmo;
        _workGizmoId = GizmoId.Move;

        runtimeInspector.InspectorValueChangedEvent += PostInspectorInputFieldValueChanged();
    }

    /// <summary>
    /// Called every frame to perform all necessary updates. In this tutorial,
    /// we listen to user input and take action. 
    /// </summary>
    private void Update()
    {
        ChangeGizmoByInspector();

        // Gizmo 드래그가 끝나는 순간 바로 Inspector 창의 값을 Update하도록 함
        // Inspector 창에서 값 수정 후 Gizmo 조작 시 Inspector 창에서 Gizmo 움직여서 생기는 변화 반영 안 되는 오류 수정
        if (RTGizmosEngine.Get.JustReleasedDrag)
        {
            runtimeInspector.RefreshInterval = 0.5f;
            runtimeInspector.RefreshDelayed();
        }
        
        // Check if the delete button was pressed in the current frame.
        if (RTInput.IsKeyPressed(KeyCode.Delete))
        {
            if (_targetObject)
            {
                Destroy(_targetObject);
                OnTargetObjectChanged(null);
                return;
            }
        }
            
        if (RTInput.WasLeftMouseButtonPressedThisFrame() && RTGizmosEngine.Get.HoveredGizmo == null)
        {
            // The left mouse button was pressed; now pick a game object and check
            GameObject pickedObject = PickGameObject();
            OnTargetObjectChanged(pickedObject);
        }

        var obj = EventSystem.current.currentSelectedGameObject;
        
        if (RTInput.IsKeyPressed(KeyCode.KeypadEnter) || RTInput.IsKeyPressed(KeyCode.Return))
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        if (!obj)
        {
            return;
        }
        
        if (!obj.CompareTag("ColorPicker"))
        {
            applyColorController.SetOffColorPicker();
        }
        
        if (obj.CompareTag("HierarchyObj"))    // Hierarchy 창에서 선택 => 오브젝트에 Gizmo 생겨야 함.
        {
            GameObject pickedObject = null;
            
            if(obj.name == "ExpandToggle") pickedObject = obj.transform.parent.parent.gameObject;
            else if (obj.name.Contains("ExpandToggles"))
            {
                if (!_targetObject) return;
                
                OnTargetObjectChanged(_targetObject);
                return;
            }
            else pickedObject = obj.transform.parent.gameObject;

            if (pickedObject.TryGetComponent(out HierarchyField _hierarchyField))
            {
                // Hierarchy 창에서 지금 실행중인 씬을 선택하면 얘도 기즈모 없으니 예외처리
                if(_hierarchyField.Data.Name == SceneManager.GetActiveScene().name) return;

                var findObj = _hierarchyField.Data.BoundTransform.gameObject;
                OnTargetObjectChanged(findObj);
            }
        }

        if (!obj.CompareTag("InputFieldObj"))
        {
            // 평소에는 Gizmo 움직인거 바로 Inspector 창에 떠야 해서 자주 refresh
            // runtimeInspector.RefreshDelayed();
            runtimeInspector.RefreshInterval = 0.5f;
            return;
        }
        
        var typeName = obj.transform.parent.GetComponent<Vector3Field>().Name;
        if (typeName.Contains("Position"))
        {
            SetWorkGizmoId(GizmoId.Move);
            
            // Inspector 창 누르면 값 바꿔야 하니까 충분한 시간 주기
            runtimeInspector.RefreshInterval = 5f;
        }
        else if (typeName.Contains("Rotation"))
        {
            SetWorkGizmoId(GizmoId.Rotate);
            
            runtimeInspector.RefreshInterval = 5f;
        }
        else if (typeName.Contains("Scale"))
        {
            SetWorkGizmoId(GizmoId.Scale);
            
            runtimeInspector.RefreshInterval = 5f;
        }
        else
        {
            SetWorkGizmoId(_workGizmoId);
        }
    }

    public BoundInputField.OnValueChangedDelegate PostInspectorInputFieldValueChanged()
    {
        return (source, input) =>
        {
            var postClickObjectAction = new PostInspectorValueChangedAction(input, source, runtimeInspector, this);
            postClickObjectAction.Execute();
            return true;
        };
    }
        
    /// <summary>
    /// Uses the mouse position to pick a game object in the scene. Returns
    /// the picked game object or null if no object is picked.
    /// </summary>
    /// <remarks>
    /// Objects must have colliders attached.
    /// </remarks>
    private GameObject PickGameObject()
    {
        // UI 요소 위에서 클릭하면 오브젝트 요소 무시하기 위함.
        if (EventSystem.current.IsPointerOverGameObject())
        {
            // 단, Transform 변경을 위해 UI를 누른거면, Gizmo를 계속 보여주기 위해 현재 타겟 오브젝트를 반환.
            if(_targetObject) return _targetObject;
            return null;
        }
        
        // Build a ray using the current mouse cursor position
        Ray ray = Camera.main.ScreenPointToRay(RTInput.MousePosition);

        // Check if the ray intersects a game object. If it does, return it
        RaycastHit rayHit;
        if (Physics.Raycast(ray, out rayHit, float.MaxValue))
            return rayHit.collider.gameObject;

        // No object is intersected by the ray. Return null.
        return null;
    }

    // Switch between different gizmo types using Mouse click
    public void SetWorkGizmoIdByNum(int num)
    {
        if (System.Enum.IsDefined(typeof(GizmoId), num))
        {
            _workGizmoId = (GizmoId)num;
            SetWorkGizmoId(_workGizmoId);
        }
    }

    /// <summary>
    /// This function is called to change the type of work gizmo.
    /// </summary>
    private void SetWorkGizmoId(GizmoId gizmoId)
    {
        // Start with a clean slate and disable all gizmos
        _objectMoveGizmo.Gizmo.SetEnabled(false);
        _objectRotationGizmo.Gizmo.SetEnabled(false);
        _objectScaleGizmo.Gizmo.SetEnabled(false);
        _objectUniversalGizmo.Gizmo.SetEnabled(false);

        // At this point all gizmos are disabled. Now we need to check the gizmo id
        // and adjust the '_workGizmo' variable.
        _workGizmoId = gizmoId;
        if (gizmoId == GizmoId.Move) _workGizmo = _objectMoveGizmo;
        else if (gizmoId == GizmoId.Rotate) _workGizmo = _objectRotationGizmo;
        else if (gizmoId == GizmoId.Scale) _workGizmo = _objectScaleGizmo;
        else if (gizmoId == GizmoId.Universal) _workGizmo = _objectUniversalGizmo;

        // At this point, the work gizmo points to the correct gizmo based on the 
        // specified gizmo id. All that's left to do is to activate the gizmo. 
        // Note: We only activate the gizmo if we have a target object available.
        //       If no target object is available, we don't do anything because we
        //       only want to show a gizmo when a target is available for use.
            
        if (_targetObject) _workGizmo.Gizmo.SetEnabled(true);
    }

    public void OnChangeTransformSpace()
    {
        var gizmoSpace = GizmoSpace.Local;
            
        switch (dropdown.value)
        {
            case 0:
                gizmoSpace = GizmoSpace.Local;
                break;
            case 1:
                gizmoSpace = GizmoSpace.Global;
                break;
        }

        SetAllTransformSpace(gizmoSpace);
    }

    private void SetAllTransformSpace(GizmoSpace gizmoSpace)
    {
        _objectMoveGizmo.SetTransformSpace(gizmoSpace);
        _objectRotationGizmo.SetTransformSpace(gizmoSpace);
        _objectScaleGizmo.SetTransformSpace(gizmoSpace);
        _objectUniversalGizmo.SetTransformSpace(gizmoSpace);
    }
        
    private void SetTransformPivot(GizmoObjectTransformPivot pivot)
    {
        _objectMoveGizmo.SetTransformPivot(pivot);
        _objectRotationGizmo.SetTransformPivot(pivot);
        _objectScaleGizmo.SetTransformPivot(pivot);
        _objectUniversalGizmo.SetTransformPivot(pivot);
    }

    private void SetCustomWorldPivot(Vector3 vector)
    {
        _objectMoveGizmo.SetCustomWorldPivot(vector);
        _objectRotationGizmo.SetCustomWorldPivot(vector);
        _objectScaleGizmo.SetCustomWorldPivot(vector);
        _objectUniversalGizmo.SetCustomWorldPivot(vector);
    }

    public void SetOffAllOutlinables()
    {
        foreach (var outlinable in _outlinables)
        {
            if(outlinable) outlinable.enabled = false;
        }
    }

    public void AddOutlinable(Outlinable outlinable)
    {
        _outlinables.Add(outlinable);
    }
    
    PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
    {
        pointerId = -1,
        position = Vector2.zero,
    };
    
    RaycastResult raycastResult = new RaycastResult();

    private Outlinable preOutlinable = null;
    private Outlinable nowOutlinable = null;

    [HideInInspector] public TotalAction totalAction = null;
    
    /// <summary>
    /// Called from the 'Update' function when the user clicks on a game object
    /// that is different from the current target object. The function takes care
    /// of adjusting the gizmo states accordingly.
    /// </summary>
    private void OnTargetObjectChanged(GameObject newTargetObject)
    {
        bool isSameObject = _targetObject == newTargetObject;

        if (_targetObject && !isSameObject)
        {
            if (_targetObject.TryGetComponent(out Outlinable outlinable))
            {
                outlinable.enabled = false;
            }
        }
        
        // Store the new target object
        _targetObject = newTargetObject;
            
        // Is the target object valid?
        if (_targetObject)
        {
            // Hierarchy 창에서도 클릭한 오브젝트에 대한 정보 나와야 함.
            HierarchyField[] hierarchyFieldArray = GetAllHierarchyFields(hierarchyTransform);
            
            foreach (var hierarchyField in hierarchyFieldArray)
            {
                if (hierarchyField.nameText.text == _targetObject.name)
                {
                    pointerEventData.position = _targetObject.transform.position;
                    raycastResult.gameObject = hierarchyField.clickListener.gameObject;
                    pointerEventData.pointerCurrentRaycast = raycastResult;
                    
                    hierarchyField.clickListener.OnPointerClick(pointerEventData);
                    break;
                }
            }
            
            // Make sure the work gizmo is enabled. We always activate the work gizmo when
            // a target object is valid. There is no need to check if the gizmo is already
            // enabled. The 'SetEnabled' call will simply be ignored if that is the case.
            _workGizmo.Gizmo.SetEnabled(true);
                
            AllocateGizmoObject(_targetObject);
                
            // 현재 Dropdown에 설정된 Transform Space 값을 따라 좌표축 설정.
            OnChangeTransformSpace();
            
            // 불러온 파일의 콜라이더 중심을 Gizmo 피봇으로 설정.
            if (_targetObject.TryGetComponent(out BoxCollider boxCollider))
            {
                SetCustomWorldPivot(boxCollider.bounds.center);
                SetTransformPivot(GizmoObjectTransformPivot.CustomWorldPivot);   
                
                runtimeInspector.RefreshDelayed();
                runtimeInspector.RefreshInterval = 0.5f;
            }
            
            if (_targetObject.TryGetComponent(out Outlinable outlinable))
            {
                // if (preOutlinable)
                // {
                //     var postClickObjectAction = new PostClickObjectAction(preOutlinable, outlinable, this);
                //     postClickObjectAction.Execute();   
                // }
                
                if (totalAction != null)
                {
                    totalAction._preOutlinable = preOutlinable;
                    totalAction._postOutlinable = outlinable;
                }
                    
                outlinable.enabled = true;
                preOutlinable = outlinable;
            }
        }
        else
        {
            // The target object is null. In this case, we don't want any gizmos to be visible
            // in the scene, so we disable all of them.
            _objectMoveGizmo.Gizmo.SetEnabled(false);
            _objectRotationGizmo.Gizmo.SetEnabled(false);
            _objectScaleGizmo.Gizmo.SetEnabled(false);
            _objectUniversalGizmo.Gizmo.SetEnabled(false);
            
            // Hierarchy 창에서 클릭 -> Delete로 해당 물체 삭제 시 오류 발생하여 예외처리
            EventSystem.current.SetSelectedGameObject(null);
        }

        if (_targetObject)
        {
            applyColorController.meshRenderer = _targetObject.TryGetComponent(out MeshRenderer meshRenderer) ? meshRenderer : null;
            
            if (!isSameObject)
            {
                if(!_targetObject.CompareTag("ColorPicker")) applyColorController.SetOffColorPicker();
            }
        }
    }

    public void AllocateGizmoObject(GameObject obj)
    {
        if (!_workGizmo.Gizmo.IsEnabled)
        {
            _workGizmo.Gizmo.SetEnabled(true);
        }
        
        _objectMoveGizmo.SetTargetObject(obj);
        _objectRotationGizmo.SetTargetObject(obj);
        _objectScaleGizmo.SetTargetObject(obj);
        _objectUniversalGizmo.SetTargetObject(obj);
        
        if (obj.TryGetComponent(out BoxCollider boxCollider))
        {
            SetCustomWorldPivot(boxCollider.bounds.center);
            SetTransformPivot(GizmoObjectTransformPivot.CustomWorldPivot);   
        }
        
        _targetObject = obj;

        // Hierarchy 창에서도 클릭한 오브젝트에 대한 정보 나와야 함.
        HierarchyField[] hierarchyFieldArray = GetAllHierarchyFields(hierarchyTransform);
            
        foreach (var hierarchyField in hierarchyFieldArray)
        {
            if (hierarchyField.nameText.text == _targetObject.name)
            {
                pointerEventData.position = _targetObject.transform.position;
                raycastResult.gameObject = hierarchyField.clickListener.gameObject;
                pointerEventData.pointerCurrentRaycast = raycastResult;
                    
                hierarchyField.clickListener.OnPointerClick(pointerEventData);
                break;
            }
        } 
    }
    
    private List<HierarchyField> GetAllHierarchyFieldsRecursive(Transform root, List<HierarchyField> fields)
    {
        // 비활성화된 부모라도 자식이 존재할 수 있음 (activeInHierarchy 체크 X)
        if (root.TryGetComponent(out HierarchyField field))
        {
            fields.Add(field);
        }

        // childCount를 안전하게 처리 (비활성화된 오브젝트 포함)
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            GetAllHierarchyFieldsRecursive(child, fields);
        }

        return fields;
    }

    private HierarchyField[] GetAllHierarchyFields(Transform root)
    {
        return GetAllHierarchyFieldsRecursive(root, new List<HierarchyField>()).ToArray();
    }

    public void ChangeGizmoByInspector()
    {
        if (_workGizmo.Gizmo.IsEnabled && _targetObject)
        {
            if (_targetObject.gameObject.activeSelf)
            {
                if (_targetObject.TryGetComponent(out BoxCollider boxCollider))
                {
                    SetCustomWorldPivot(boxCollider.bounds.center);
                    SetTransformPivot(GizmoObjectTransformPivot.CustomWorldPivot);
                }   
            }
            else
            {
                _workGizmo.Gizmo.SetEnabled(false);       
            }
        }
        else if (!_workGizmo.Gizmo.IsEnabled && _targetObject)
        {
            if(_targetObject.gameObject.activeSelf) _workGizmo.Gizmo.SetEnabled(true);
        }
    }
}