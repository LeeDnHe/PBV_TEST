using UnityEngine;
using System.Collections.Generic;
using Exoa.Effects;
using RuntimeInspectorNamespace;

namespace RTG
{
    public class TotalAction : IUndoRedoAction
    {
        private bool _cleanupOnRemovedFromStack;
        private List<GameObject> _spawnedParents = new List<GameObject>();

        private List<LocalTransformSnapshot> _preChangeTransformSnapshots = new List<LocalTransformSnapshot>();
        private List<LocalTransformSnapshot> _postChangeTransformSnapshots = new List<LocalTransformSnapshot>();

        public Outlinable _preOutlinable;
        public Outlinable _postOutlinable;
        private AllocateGizmo _allocateGizmo;

        private string _preValue;
        private BoundInputField _InputField;

        private string _postValue;
        public RuntimeInspector _inspector;

        public TotalAction(List<GameObject> spawnedParents, Outlinable preOutlinable, Outlinable postOutlinable, AllocateGizmo allocateGizmo, RuntimeInspector inspector)
        {
            _spawnedParents = new List<GameObject>(spawnedParents);
            
            var preChangeTransformSnapshots = new List<LocalTransformSnapshot>();
            var postChangeTransformSnapshots = new List<LocalTransformSnapshot>();
            
            _preChangeTransformSnapshots = new List<LocalTransformSnapshot>(preChangeTransformSnapshots);
            _postChangeTransformSnapshots = new List<LocalTransformSnapshot>(postChangeTransformSnapshots);
            
            _preOutlinable = preOutlinable;
            _postOutlinable = postOutlinable;
            _allocateGizmo = allocateGizmo;
            
            _postValue = "0";
            _preValue = "0";
            
            _InputField = null;
            _inspector = inspector;
        }
        
        public void Execute()
        {
            RTUndoRedo.Get.RecordAction(this);
        }
        
        public void Undo()
        {
            if (_spawnedParents != null)
            {
                foreach (var parent in _spawnedParents)
                {
                    if(parent) parent.SetActive(false);
                }
                _cleanupOnRemovedFromStack = true;
            }
            
            foreach (var snapshot in _preChangeTransformSnapshots)
            {
                snapshot.Apply();
            }
            
            if(_preOutlinable) _preOutlinable.enabled = true;
            if(_postOutlinable) _postOutlinable.enabled = false;

            if (_allocateGizmo)
            {
                if(_preOutlinable) _allocateGizmo.AllocateGizmoObject(_preOutlinable.gameObject);
                _allocateGizmo.ChangeGizmoByInspector();   
            }

            if (_InputField)
            {
                _InputField.Text = _preValue;

                var field = _InputField.transform.parent.GetComponent<Vector3Field>();
            
                Vector3 val = (Vector3) field.Value;

                if (field.IsSameWithInputFieldX(_InputField))
                {
                    val.x = float.Parse(_preValue);
                }
                else if (field.IsSameWithInputFieldY(_InputField))
                {
                    val.y = float.Parse(_preValue);
                }
                else if (field.IsSameWithInputFieldZ(_InputField))
                {
                    val.z = float.Parse(_preValue);
                }
            
                field.Value = val;   
            }
        }

        public void Redo()
        {
            if (_spawnedParents != null)
            {
                foreach (var parent in _spawnedParents)
                {
                    if(parent) parent.SetActive(true);
                }
                _cleanupOnRemovedFromStack = false;
            }
            
            foreach (var snapshot in _postChangeTransformSnapshots)
            {
                snapshot.Apply();
            }

            if (_InputField)
            {
                _InputField.Text = _postValue;
            
                var field = _InputField.transform.parent.GetComponent<Vector3Field>();
            
                Vector3 val = (Vector3) field.Value;

                if (field.IsSameWithInputFieldX(_InputField))
                {
                    val.x = float.Parse(_postValue);
                }
                else if (field.IsSameWithInputFieldY(_InputField))
                {
                    val.y = float.Parse(_postValue);
                }
                else if (field.IsSameWithInputFieldZ(_InputField))
                {
                    val.z = float.Parse(_postValue);
                }
            
                field.Value = val;   
            }
            
            _allocateGizmo.ChangeGizmoByInspector();
            
            // if (_gameObject)
            // {
            //     _gameObject.SetActive(_postIsActive);
            //     
            //     _preIsActive = !_postIsActive;
            // }
        }

        public void OnRemovedFromUndoRedoStack()
        {
            if (_cleanupOnRemovedFromStack && _spawnedParents.Count != 0)
            {
                foreach (var parent in _spawnedParents) if(parent) GameObject.Destroy(parent);
                _spawnedParents.Clear();
            }
        }
    }
    
    public class PostObjectSpawnAction : IUndoRedoAction
    {
        private bool _cleanupOnRemovedFromStack;
        private List<GameObject> _spawnedParents = new List<GameObject>();

        public PostObjectSpawnAction(List<GameObject> spawnedParents)
        {
            _spawnedParents = new List<GameObject>(spawnedParents);
        }

        public void Execute()
        {
            RTUndoRedo.Get.RecordAction(this);
        }

        public void Undo()
        {
            if (_spawnedParents != null)
            {
                foreach (var parent in _spawnedParents)
                {
                    if(parent) parent.SetActive(false);
                }
                _cleanupOnRemovedFromStack = true;
            }
        }

        public void Redo()
        {
            if (_spawnedParents != null)
            {
                foreach (var parent in _spawnedParents)
                {
                    if(parent) parent.SetActive(true);
                }
                _cleanupOnRemovedFromStack = false;
            }
        }

        public void OnRemovedFromUndoRedoStack()
        {
            if (_cleanupOnRemovedFromStack && _spawnedParents.Count != 0)
            {
                foreach (var parent in _spawnedParents) if(parent) GameObject.Destroy(parent);
                _spawnedParents.Clear();
            }
        }
    }

    public class PostObjectTransformsChangedAction : IUndoRedoAction
    {
        private List<LocalTransformSnapshot> _preChangeTransformSnapshots = new List<LocalTransformSnapshot>();
        private List<LocalTransformSnapshot> _postChangeTransformSnapshots = new List<LocalTransformSnapshot>();

        public PostObjectTransformsChangedAction(List<LocalTransformSnapshot> preChangeTransformSnapshots,
                                                 List<LocalTransformSnapshot> postChangeTransformSnapshots)
        {
            _preChangeTransformSnapshots = new List<LocalTransformSnapshot>(preChangeTransformSnapshots);
            _postChangeTransformSnapshots = new List<LocalTransformSnapshot>(postChangeTransformSnapshots);
        }

        public void Execute()
        {
            RTUndoRedo.Get.RecordAction(this);
        }

        public void Undo()
        {
            foreach (var snapshot in _preChangeTransformSnapshots)
            {
                snapshot.Apply();
            }
        }

        public void Redo()
        {
            foreach (var snapshot in _postChangeTransformSnapshots)
            {
                snapshot.Apply();
            }
        }

        public void OnRemovedFromUndoRedoStack()
        {
        }
    }

    public class PostGizmoTransformsChangedAction : IUndoRedoAction
    {
        private List<LocalGizmoTransformSnapshot> _preChangeTransformSnapshots = new List<LocalGizmoTransformSnapshot>();
        private List<LocalGizmoTransformSnapshot> _postChangeTransformSnapshots = new List<LocalGizmoTransformSnapshot>();

        public PostGizmoTransformsChangedAction(List<LocalGizmoTransformSnapshot> preChangeTransformSnapshots,
                                                List<LocalGizmoTransformSnapshot> postChangeTransformSnapshots)
        {
            _preChangeTransformSnapshots = new List<LocalGizmoTransformSnapshot>(preChangeTransformSnapshots);
            _postChangeTransformSnapshots = new List<LocalGizmoTransformSnapshot>(postChangeTransformSnapshots);
        }

        public void Execute()
        {
            RTUndoRedo.Get.RecordAction(this);
        }

        public void Undo()
        {
            foreach (var snapshot in _preChangeTransformSnapshots)
            {
                snapshot.Apply();
            }
        }

        public void Redo()
        {
            foreach (var snapshot in _postChangeTransformSnapshots)
            {
                snapshot.Apply();
            }
        }

        public void OnRemovedFromUndoRedoStack()
        {
        }
    }

    public class DuplicateObjectsAction : IUndoRedoAction
    {
        private List<GameObject> _rootsToDuplicate;
        private List<GameObject> _duplicateResult = new List<GameObject>();
        private bool _cleanupOnRemovedFromStack;

        public List<GameObject> DuplicateResult { get { return new List<GameObject>(_duplicateResult); } }

        public DuplicateObjectsAction(List<GameObject> rootsToDuplicate)
        {
            _rootsToDuplicate = GameObjectEx.FilterParentsOnly(rootsToDuplicate);
        }

        public void Execute()
        {
            if (_rootsToDuplicate.Count != 0)
            {
                var cloneConfig = ObjectCloning.DefaultConfig;

                foreach (var root in _rootsToDuplicate)
                {
                    Transform rootTransform = root.transform;
                    cloneConfig.Layer = root.layer;
                    cloneConfig.Parent = rootTransform.parent;

                    GameObject clonedRoot = ObjectCloning.CloneHierarchy(root, cloneConfig);
                    _duplicateResult.Add(clonedRoot);
                }

                RTUndoRedo.Get.RecordAction(this);
            }
        }

        public void Undo()
        {
            if (_duplicateResult != null)
            {
                foreach (var duplicateRoot in _duplicateResult)
                {
                    duplicateRoot.SetActive(false);
                }
                _cleanupOnRemovedFromStack = true;
            }
        }

        public void Redo()
        {
            if (_duplicateResult != null)
            {
                foreach (var duplicateRoot in _duplicateResult)
                {
                    duplicateRoot.SetActive(true);
                }
                _cleanupOnRemovedFromStack = false;
            }
        }

        public void OnRemovedFromUndoRedoStack()
        {
            if (_cleanupOnRemovedFromStack && _duplicateResult.Count != 0)
            {
                foreach (var duplicateRoot in _duplicateResult) GameObject.Destroy(duplicateRoot);
                _duplicateResult.Clear();
            }
        }
    }

    public class PostClickObjectAction : IUndoRedoAction
    {
        private Outlinable _preOutlinable;
        private Outlinable _postOutlinable;
        private AllocateGizmo _allocateGizmo;
        
        public PostClickObjectAction(Outlinable preOutlinable, Outlinable postOutlinable, AllocateGizmo allocateGizmo)
        {
            _preOutlinable = preOutlinable;
            _postOutlinable = postOutlinable;
            _allocateGizmo = allocateGizmo;
        }

        public void Execute()
        {
            RTUndoRedo.Get.RecordAction(this);
        }

        public void Undo()
        {
            _preOutlinable.enabled = true;
            _postOutlinable.enabled = false;
            _allocateGizmo.AllocateGizmoObject(_preOutlinable.gameObject);
            
            _allocateGizmo.ChangeGizmoByInspector();
        }

        public void Redo()
        {
            _preOutlinable.enabled = false;
            _postOutlinable.enabled = true;
            _allocateGizmo.AllocateGizmoObject(_postOutlinable.gameObject);
            
            _allocateGizmo.ChangeGizmoByInspector();
        }

        public void OnRemovedFromUndoRedoStack()
        {
        }
    }
    
    public class PostInspectorValueChangedAction : IUndoRedoAction
    {
        private string _preValue;
        private BoundInputField _InputField;
        
        private string _postValue;
        private RuntimeInspector _inspector;

        private AllocateGizmo _allocateGizmo;

        public PostInspectorValueChangedAction(string postValue, BoundInputField inputField, RuntimeInspector inspector, AllocateGizmo allocateGizmo)
        {
            _preValue = inputField.recentText;
            _postValue = postValue;
            _InputField = inputField;
            _inspector = inspector;
            _allocateGizmo = allocateGizmo;
        }
        
        public void Execute()
        {
            RTUndoRedo.Get.RecordAction(this);
        }

        public void Undo()
        {
            _InputField.Text = _preValue;

            var field = _InputField.transform.parent.GetComponent<Vector3Field>();
            
            Vector3 val = (Vector3) field.Value;

            if (field.IsSameWithInputFieldX(_InputField))
            {
                val.x = float.Parse(_preValue);
            }
            else if (field.IsSameWithInputFieldY(_InputField))
            {
                val.y = float.Parse(_preValue);
            }
            else if (field.IsSameWithInputFieldZ(_InputField))
            {
                val.z = float.Parse(_preValue);
            }
            
            field.Value = val;
            
            _allocateGizmo.ChangeGizmoByInspector();
        }

        public void Redo()
        {
            _InputField.Text = _postValue;
            
            var field = _InputField.transform.parent.GetComponent<Vector3Field>();
            
            Vector3 val = (Vector3) field.Value;

            if (field.IsSameWithInputFieldX(_InputField))
            {
                val.x = float.Parse(_postValue);
            }
            else if (field.IsSameWithInputFieldY(_InputField))
            {
                val.y = float.Parse(_postValue);
            }
            else if (field.IsSameWithInputFieldZ(_InputField))
            {
                val.z = float.Parse(_postValue);
            }
            
            field.Value = val;
            
            _allocateGizmo.ChangeGizmoByInspector();
        }
        
        public void OnRemovedFromUndoRedoStack()
        {
        }
    }
}
