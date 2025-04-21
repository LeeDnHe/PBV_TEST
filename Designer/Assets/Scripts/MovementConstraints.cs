using UnityEngine;
using System.Collections.Generic;
using RTG;

/// <summary>
/// Gizmo 및 오브젝트 이동에 제약 조건을 적용하는 스크립트
/// </summary>
public class MovementConstraints : MonoBehaviour
{
    [Header("영역 제한")]
    [Tooltip("특정 영역으로 이동 제한")]
    public bool useRegionConstraint = true;
    
    [Tooltip("제한 영역 중심")]
    public Vector3 regionCenter = Vector3.zero;
    
    [Tooltip("제한 영역 크기 (미터 단위)")]
    public Vector3 regionSize = new Vector3(10f, 5f, 10f);
    
    [Tooltip("영역 표시 색상")]
    public Color regionColor = new Color(0.2f, 0.8f, 0.2f, 0.2f);
    
    [Header("충돌 처리")]
    [Tooltip("충돌 방지 기능 사용")]
    public bool useCollisionPrevention = true;
    
    [Tooltip("충돌 방지 레이어 (선택된 레이어와만 충돌 검사)")]
    public LayerMask collisionLayers = -1;
    
    [Tooltip("충돌 감지 시 오브젝트 간 최소 거리 (미터)")]
    public float minDistanceBetweenObjects = 0.1f;
    
    [Header("스냅 기능")]
    [Tooltip("그리드 스냅 사용")]
    public bool useGridSnapping = false;
    
    [Tooltip("그리드 간격 (미터 단위)")]
    public Vector3 gridSpacing = new Vector3(0.5f, 0.5f, 0.5f);
    
    // 연결된 Gizmo 및 타겟 오브젝트
    private ObjectTransformGizmo _objectMoveGizmo;
    private GameObject _targetObject;
    
    // 최종 위치 계산을 위한 임시 변수
    private Vector3 _originalPosition;
    private Vector3 _constrainedPosition;
    
    // 충돌 감지를 위한 임시 변수들
    private Collider[] _collisionBuffer = new Collider[20];
    private List<Collider> _ownColliders = new List<Collider>();
    
    // 경계 영역
    private Bounds _regionBounds;
    
    /// <summary>
    /// 초기화 및 이벤트 연결
    /// </summary>
    private void Awake()
    {
        _regionBounds = new Bounds(regionCenter, regionSize);
    }
    
    /// <summary>
    /// 연결된 기즈모 설정
    /// </summary>
    public void SetGizmo(ObjectTransformGizmo gizmo)
    {
        if (_objectMoveGizmo != null)
        {
            // 이전 이벤트 구독 해제
            _objectMoveGizmo.Gizmo.PostDragUpdate -= OnGizmoDragUpdate;
        }
        
        _objectMoveGizmo = gizmo;
        
        if (_objectMoveGizmo != null)
        {
            // 새 이벤트 구독
            _objectMoveGizmo.Gizmo.PostDragUpdate += OnGizmoDragUpdate;
        }
    }
    
    /// <summary>
    /// 대상 오브젝트 설정
    /// </summary>
    public void SetTargetObject(GameObject targetObject)
    {
        _targetObject = targetObject;
        
        // 대상 오브젝트의 콜라이더 캐싱
        _ownColliders.Clear();
        if (_targetObject != null)
        {
            Collider[] colliders = _targetObject.GetComponents<Collider>();
            _ownColliders.AddRange(colliders);
            
            foreach (Collider childCollider in _targetObject.GetComponentsInChildren<Collider>())
            {
                if (!_ownColliders.Contains(childCollider))
                    _ownColliders.Add(childCollider);
            }
        }
    }
    
    /// <summary>
    /// Gizmo 드래그 업데이트 이벤트 처리
    /// </summary>
    private void OnGizmoDragUpdate(Gizmo gizmo, int handleId)
    {
        if (_targetObject == null)
            return;
            
        // 현재 위치 저장
        _originalPosition = _targetObject.transform.position;
        
        // 제약 조건 적용
        _constrainedPosition = _originalPosition;
        
        // 영역 제한 적용
        if (useRegionConstraint)
        {
            ApplyRegionConstraint();
        }
        
        // 충돌 방지 적용
        if (useCollisionPrevention)
        {
            ApplyCollisionPrevention();
        }
        
        // 그리드 스냅 적용
        if (useGridSnapping)
        {
            ApplyGridSnapping();
        }
        
        // 제약이 적용된 위치로 업데이트 (원래 위치와 다른 경우만)
        if (_constrainedPosition != _originalPosition)
        {
            _targetObject.transform.position = _constrainedPosition;
        }
    }
    
    /// <summary>
    /// 영역 제한 적용
    /// </summary>
    private void ApplyRegionConstraint()
    {
        // 오브젝트 경계 가져오기
        Bounds objectBounds = GetObjectBounds();
        
        // 바운드가 유효한 경우만 처리
        if (objectBounds.size.magnitude > 0.01f)
        {
            // 각 차원별로 안쪽으로 제한
            Vector3 clampedPosition = _constrainedPosition;
            
            // X축 제한
            float halfSizeX = objectBounds.size.x * 0.5f;
            clampedPosition.x = Mathf.Clamp(
                clampedPosition.x,
                _regionBounds.min.x + halfSizeX,
                _regionBounds.max.x - halfSizeX
            );
            
            // Y축 제한
            float halfSizeY = objectBounds.size.y * 0.5f;
            clampedPosition.y = Mathf.Clamp(
                clampedPosition.y,
                _regionBounds.min.y + halfSizeY,
                _regionBounds.max.y - halfSizeY
            );
            
            // Z축 제한
            float halfSizeZ = objectBounds.size.z * 0.5f;
            clampedPosition.z = Mathf.Clamp(
                clampedPosition.z,
                _regionBounds.min.z + halfSizeZ,
                _regionBounds.max.z - halfSizeZ
            );
            
            _constrainedPosition = clampedPosition;
        }
        else
        {
            // 간단한 점 제한 (바운드가 없는 경우)
            _constrainedPosition = new Vector3(
                Mathf.Clamp(_constrainedPosition.x, _regionBounds.min.x, _regionBounds.max.x),
                Mathf.Clamp(_constrainedPosition.y, _regionBounds.min.y, _regionBounds.max.y),
                Mathf.Clamp(_constrainedPosition.z, _regionBounds.min.z, _regionBounds.max.z)
            );
        }
    }
    
    /// <summary>
    /// 충돌 방지 적용
    /// </summary>
    private void ApplyCollisionPrevention()
    {
        // 오브젝트 경계 가져오기
        Bounds objectBounds = GetObjectBounds();
        
        // 주변 충돌체 감지
        int numColliders = Physics.OverlapBoxNonAlloc(
            _constrainedPosition,
            objectBounds.extents + Vector3.one * minDistanceBetweenObjects,
            _collisionBuffer,
            _targetObject.transform.rotation,
            collisionLayers
        );
        
        // 충돌이 감지된 경우
        bool hasCollision = false;
        Vector3 avoidanceDirection = Vector3.zero;
        
        for (int i = 0; i < numColliders; i++)
        {
            Collider otherCollider = _collisionBuffer[i];
            
            // 자기 자신의 콜라이더는 무시
            if (_ownColliders.Contains(otherCollider))
                continue;
                
            // 충돌 감지 및 회피 방향 계산
            Vector3 otherPosition = otherCollider.bounds.center;
            Vector3 direction = _constrainedPosition - otherPosition;
            float distance = direction.magnitude;
            
            // 충돌 거리 내에 있으면 회피 방향 누적
            float minDistance = objectBounds.extents.magnitude + 
                               otherCollider.bounds.extents.magnitude + 
                               minDistanceBetweenObjects;
                               
            if (distance < minDistance)
            {
                hasCollision = true;
                
                // 충돌을 피하기 위한 방향 계산
                direction = direction.normalized;
                float pushDistance = minDistance - distance;
                avoidanceDirection += direction * pushDistance;
            }
        }
        
        // 충돌이 감지되면 이동 위치 조정
        if (hasCollision && avoidanceDirection.magnitude > 0.01f)
        {
            _constrainedPosition += avoidanceDirection;
            
            // 다시 영역 제한 적용 (충돌 회피가 영역 밖으로 나가게 할 수 있음)
            if (useRegionConstraint)
            {
                ApplyRegionConstraint();
            }
        }
    }
    
    /// <summary>
    /// 그리드 스냅 적용
    /// </summary>
    private void ApplyGridSnapping()
    {
        _constrainedPosition.x = Mathf.Round(_constrainedPosition.x / gridSpacing.x) * gridSpacing.x;
        _constrainedPosition.y = Mathf.Round(_constrainedPosition.y / gridSpacing.y) * gridSpacing.y;
        _constrainedPosition.z = Mathf.Round(_constrainedPosition.z / gridSpacing.z) * gridSpacing.z;
    }
    
    /// <summary>
    /// 오브젝트의 경계 계산
    /// </summary>
    private Bounds GetObjectBounds()
    {
        Bounds result = new Bounds();
        
        if (_targetObject == null)
            return result;
            
        // 콜라이더가 있는 경우 콜라이더 바운드 사용
        Collider[] colliders = _targetObject.GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            result = colliders[0].bounds;
            
            for (int i = 1; i < colliders.Length; i++)
            {
                result.Encapsulate(colliders[i].bounds);
            }
            
            return result;
        }
        
        // 콜라이더가 없는 경우 렌더러 바운드 사용
        Renderer[] renderers = _targetObject.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            result = renderers[0].bounds;
            
            for (int i = 1; i < renderers.Length; i++)
            {
                result.Encapsulate(renderers[i].bounds);
            }
            
            return result;
        }
        
        // 바운드를 계산할 수 없는 경우 작은 사이즈의 바운드 반환
        result.center = _targetObject.transform.position;
        result.extents = Vector3.one * 0.1f;
        
        return result;
    }
    
    /// <summary>
    /// 영역 시각화 (디버그)
    /// </summary>
    private void OnDrawGizmos()
    {
        if (useRegionConstraint)
        {
            Gizmos.color = regionColor;
            Gizmos.DrawCube(regionCenter, regionSize);
            
            Gizmos.color = new Color(regionColor.r, regionColor.g, regionColor.b, 0.8f);
            Gizmos.DrawWireCube(regionCenter, regionSize);
        }
    }
    
    /// <summary>
    /// AllocateGizmo 스크립트와 연동 (런타임에 연결)
    /// </summary>
    public void ConnectWithAllocateGizmo(AllocateGizmo allocateGizmo)
    {
        if (allocateGizmo == null)
            return;
            
        // AllocateGizmo 스크립트에서 모든 기즈모 가져와서 이벤트 연결
        SetGizmo(allocateGizmo.GetComponent<ObjectTransformGizmo>());
    }
    
    /// <summary>
    /// 충돌 감지 테스트
    /// </summary>
    [ContextMenu("충돌 감지 테스트")]
    public void TestCollisionDetection()
    {
        if (_targetObject == null)
        {
            Debug.LogWarning("타겟 오브젝트가 없습니다.");
            return;
        }
        
        Bounds objectBounds = GetObjectBounds();
        
        int numColliders = Physics.OverlapBoxNonAlloc(
            _targetObject.transform.position,
            objectBounds.extents + Vector3.one * minDistanceBetweenObjects,
            _collisionBuffer,
            _targetObject.transform.rotation,
            collisionLayers
        );
        
        Debug.Log($"감지된 충돌체 수: {numColliders}");
        
        for (int i = 0; i < numColliders; i++)
        {
            if (!_ownColliders.Contains(_collisionBuffer[i]))
            {
                Debug.Log($"충돌 감지: {_collisionBuffer[i].name}");
            }
        }
    }
} 