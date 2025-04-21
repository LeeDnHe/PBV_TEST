using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 3D 공간의 그리드 포인트를 효율적으로 체크하는 스크립트
/// 웹GL에서도 효율적으로 동작하도록 최적화되었습니다.
/// </summary>
public class GridPointChecker : MonoBehaviour
{
    [Header("그리드 설정")]
    [Tooltip("그리드 간격 (미터 단위)")]
    public float gridSpacing = 0.1f;
    
    [Tooltip("그리드 영역 크기 (미터 단위)")]
    public Vector3 gridSize = new Vector3(10f, 5f, 10f);
    
    [Tooltip("그리드 영역 중심점")]
    public Vector3 gridCenter = Vector3.zero;
    
    [Header("시각화 설정")]
    [Tooltip("디버그 시각화 활성화")]
    public bool visualizeGrid = false;
    
    [Tooltip("내부 점 색상")]
    public Color insidePointColor = Color.green;
    
    [Tooltip("시각화 시 점 크기")]
    public float pointSize = 0.02f;
    
    // 모든 내부 점을 저장
    private List<Vector3> insidePoints = new List<Vector3>();
    
    // 계산 성능 향상을 위한 캐시
    private Bounds gridBounds;
    
    private void Start()
    {
        // 그리드 영역 경계 계산
        gridBounds = new Bounds(gridCenter, gridSize);
    }
    
    /// <summary>
    /// 지정한 영역의 모든 그리드 점 중 오브젝트 내부에 있는 점을 찾습니다
    /// </summary>
    /// <param name="targetObject">내부 점을 찾을 대상 오브젝트</param>
    public List<Vector3> FindPointsInsideObject(GameObject targetObject)
    {
        insidePoints.Clear();
        
        if (!targetObject)
            return insidePoints;
            
        // 대상 오브젝트의 콜라이더 가져오기
        Collider[] colliders = targetObject.GetComponents<Collider>();
        if (colliders.Length == 0)
        {
            Debug.LogWarning($"오브젝트 '{targetObject.name}'에 콜라이더가 없습니다.");
            return insidePoints;
        }
        
        // 콜라이더 바운드 계산 (최적화를 위해)
        Bounds objectBounds = GetCombinedBounds(colliders);
        
        // 바운드가 겹치는 영역만 계산
        Bounds overlapBounds = new Bounds();
        bool boundsOverlap = GetOverlapBounds(gridBounds, objectBounds, out overlapBounds);
        
        if (!boundsOverlap)
        {
            Debug.Log($"오브젝트 '{targetObject.name}'와 그리드 영역이 겹치지 않습니다.");
            return insidePoints;
        }
        
        // 오버랩 영역의 그리드 범위 계산 (성능 최적화를 위해 필요한 영역만 체크)
        int minX = Mathf.FloorToInt((overlapBounds.min.x - gridCenter.x + gridSize.x/2) / gridSpacing);
        int maxX = Mathf.CeilToInt((overlapBounds.max.x - gridCenter.x + gridSize.x/2) / gridSpacing);
        int minY = Mathf.FloorToInt((overlapBounds.min.y - gridCenter.y + gridSize.y/2) / gridSpacing);
        int maxY = Mathf.CeilToInt((overlapBounds.max.y - gridCenter.y + gridSize.y/2) / gridSpacing);
        int minZ = Mathf.FloorToInt((overlapBounds.min.z - gridCenter.z + gridSize.z/2) / gridSpacing);
        int maxZ = Mathf.CeilToInt((overlapBounds.max.z - gridCenter.z + gridSize.z/2) / gridSpacing);
        
        // 범위 제한
        Vector3Int gridDimensions = GetGridDimensions();
        minX = Mathf.Max(0, minX);
        maxX = Mathf.Min(gridDimensions.x - 1, maxX);
        minY = Mathf.Max(0, minY);
        maxY = Mathf.Min(gridDimensions.y - 1, maxY);
        minZ = Mathf.Max(0, minZ);
        maxZ = Mathf.Min(gridDimensions.z - 1, maxZ);
        
        // 그리드 점 체크 (오버랩 영역만)
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    Vector3 gridPoint = GridIndexToWorldPoint(new Vector3Int(x, y, z));
                    
                    // 내부 점 체크 - 성능을 위해 간단한 체크
                    if (IsPointInsideColliders(gridPoint, colliders))
                    {
                        insidePoints.Add(gridPoint);
                    }
                }
            }
        }
        
        return insidePoints;
    }
    
    /// <summary>
    /// 좌석 등의 특수 오브젝트에 대해 최적화된 내부 점 체크
    /// </summary>
    public List<Vector3> FindPointsInsideSeat(GameObject seatObject)
    {
        insidePoints.Clear();
        
        if (!seatObject)
            return insidePoints;
        
        // 이름으로 콜라이더 찾기 (좌석 전용 로직)
        BoxCollider seatCollider = null;
        BoxCollider backrestCollider = null;
        
        Collider[] colliders = seatObject.GetComponents<Collider>();
        foreach (var collider in colliders)
        {
            if (collider.name == "SeatCollider")
                seatCollider = collider as BoxCollider;
            else if (collider.name == "BackrestCollider")
                backrestCollider = collider as BoxCollider;
        }
        
        // 좌석 부분 처리
        if (seatCollider != null)
        {
            Bounds seatBounds = seatCollider.bounds;
            FindPointsInBoxCollider(seatCollider, ref insidePoints);
        }
        
        // 등받이 부분 처리
        if (backrestCollider != null)
        {
            FindPointsInBoxCollider(backrestCollider, ref insidePoints);
        }
        
        return insidePoints;
    }
    
    /// <summary>
    /// BoxCollider 내부의 그리드 점을 빠르게 찾습니다 (최적화된 방법)
    /// </summary>
    private void FindPointsInBoxCollider(BoxCollider boxCollider, ref List<Vector3> points)
    {
        Bounds bounds = boxCollider.bounds;
        
        // 바운드가 겹치는 영역만 계산
        Bounds overlapBounds = new Bounds();
        bool boundsOverlap = GetOverlapBounds(gridBounds, bounds, out overlapBounds);
        
        if (!boundsOverlap)
            return;
            
        // 오버랩 영역의 그리드 범위 계산
        int minX = Mathf.FloorToInt((overlapBounds.min.x - gridCenter.x + gridSize.x/2) / gridSpacing);
        int maxX = Mathf.CeilToInt((overlapBounds.max.x - gridCenter.x + gridSize.x/2) / gridSpacing);
        int minY = Mathf.FloorToInt((overlapBounds.min.y - gridCenter.y + gridSize.y/2) / gridSpacing);
        int maxY = Mathf.CeilToInt((overlapBounds.max.y - gridCenter.y + gridSize.y/2) / gridSpacing);
        int minZ = Mathf.FloorToInt((overlapBounds.min.z - gridCenter.z + gridSize.z/2) / gridSpacing);
        int maxZ = Mathf.CeilToInt((overlapBounds.max.z - gridCenter.z + gridSize.z/2) / gridSpacing);
        
        // 범위 제한
        Vector3Int gridDimensions = GetGridDimensions();
        minX = Mathf.Max(0, minX);
        maxX = Mathf.Min(gridDimensions.x - 1, maxX);
        minY = Mathf.Max(0, minY);
        maxY = Mathf.Min(gridDimensions.y - 1, maxY);
        minZ = Mathf.Max(0, minZ);
        maxZ = Mathf.Min(gridDimensions.z - 1, maxZ);
        
        // BoxCollider는 항등 변환(단위 회전)일 경우 더 빠른 계산 가능
        bool isAxisAligned = boxCollider.transform.rotation.eulerAngles == Vector3.zero;
        
        // 그리드 점 체크 (오버랩 영역만)
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    Vector3 gridPoint = GridIndexToWorldPoint(new Vector3Int(x, y, z));
                    
                    // 빠른 내부 체크 (단순 경계 체크만으로 충분)
                    if (isAxisAligned)
                    {
                        if (bounds.Contains(gridPoint))
                            points.Add(gridPoint);
                    }
                    else
                    {
                        // 회전된 박스는 지역 좌표계로 변환하여 체크
                        Vector3 localPoint = boxCollider.transform.InverseTransformPoint(gridPoint);
                        Vector3 halfSize = boxCollider.size * 0.5f;
                        
                        if (localPoint.x >= -halfSize.x && localPoint.x <= halfSize.x &&
                            localPoint.y >= -halfSize.y && localPoint.y <= halfSize.y &&
                            localPoint.z >= -halfSize.z && localPoint.z <= halfSize.z)
                        {
                            points.Add(gridPoint);
                        }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 그리드 인덱스를 월드 좌표로 변환
    /// </summary>
    private Vector3 GridIndexToWorldPoint(Vector3Int index)
    {
        Vector3 origin = gridCenter - gridSize * 0.5f;
        return new Vector3(
            origin.x + index.x * gridSpacing,
            origin.y + index.y * gridSpacing,
            origin.z + index.z * gridSpacing
        );
    }
    
    /// <summary>
    /// 그리드 차원 (x, y, z 방향 그리드 포인트 수) 계산
    /// </summary>
    private Vector3Int GetGridDimensions()
    {
        return new Vector3Int(
            Mathf.FloorToInt(gridSize.x / gridSpacing) + 1,
            Mathf.FloorToInt(gridSize.y / gridSpacing) + 1,
            Mathf.FloorToInt(gridSize.z / gridSpacing) + 1
        );
    }
    
    /// <summary>
    /// 두 바운드 간의 겹치는 영역 계산
    /// </summary>
    private bool GetOverlapBounds(Bounds a, Bounds b, out Bounds overlap)
    {
        overlap = new Bounds();
        
        if (!a.Intersects(b))
            return false;
            
        Vector3 min = Vector3.Max(a.min, b.min);
        Vector3 max = Vector3.Min(a.max, b.max);
        
        overlap.SetMinMax(min, max);
        return true;
    }
    
    /// <summary>
    /// 여러 콜라이더의 결합된 바운드 계산
    /// </summary>
    private Bounds GetCombinedBounds(Collider[] colliders)
    {
        if (colliders.Length == 0)
            return new Bounds();
            
        Bounds result = colliders[0].bounds;
        
        for (int i = 1; i < colliders.Length; i++)
        {
            result.Encapsulate(colliders[i].bounds);
        }
        
        return result;
    }
    
    /// <summary>
    /// 점이 콜라이더 내부에 있는지 확인 (성능 최적화 버전)
    /// </summary>
    private bool IsPointInsideColliders(Vector3 point, Collider[] colliders)
    {
        foreach (Collider collider in colliders)
        {
            // BoxCollider는 매우 빠른 계산이 가능
            if (collider is BoxCollider boxCollider)
            {
                // 단위 회전(축 정렬) 박스는 더 빠른 계산
                if (boxCollider.transform.rotation.eulerAngles == Vector3.zero)
                {
                    if (boxCollider.bounds.Contains(point))
                        return true;
                }
                else
                {
                    // 회전된 박스는 지역 좌표계로 변환
                    Vector3 localPoint = boxCollider.transform.InverseTransformPoint(point);
                    Vector3 halfSize = boxCollider.size * 0.5f;
                    
                    if (localPoint.x >= -halfSize.x && localPoint.x <= halfSize.x &&
                        localPoint.y >= -halfSize.y && localPoint.y <= halfSize.y &&
                        localPoint.z >= -halfSize.z && localPoint.z <= halfSize.z)
                    {
                        return true;
                    }
                }
            }
            // MeshCollider는 ClosestPoint 메서드로 간접 확인 (WebGL에서도 작동)
            else if (collider is MeshCollider meshCollider && meshCollider.convex)
            {
                Vector3 closestPoint = meshCollider.ClosestPoint(point);
                float distance = Vector3.Distance(closestPoint, point);
                
                // 매우 작은 거리면 내부로 간주 (완벽하진 않지만 WebGL에서도 작동)
                if (distance < 0.001f)
                    return true;
            }
            // 기타 콜라이더
            else if (collider.bounds.Contains(point))
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 내부 점 시각화 (디버그 모드)
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!visualizeGrid || insidePoints.Count == 0)
            return;
            
        Gizmos.color = insidePointColor;
        
        foreach (Vector3 point in insidePoints)
        {
            Gizmos.DrawSphere(point, pointSize);
        }
        
        // 그리드 영역 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(gridCenter, gridSize);
    }
    
    /// <summary>
    /// 테스트용 현재 씬의 전체 좌석에 대한 절점 체크
    /// </summary>
    [ContextMenu("테스트: 모든 좌석 절점 체크")]
    public void TestAllSeats()
    {
        insidePoints.Clear();
        
        // 씬에서 "seat" 또는 "chair"가 이름에 포함된 오브젝트 찾기
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int seatCount = 0;
        
        foreach (GameObject obj in allObjects)
        {
            string objName = obj.name.ToLower();
            if (objName.Contains("seat") || objName.Contains("chair"))
            {
                List<Vector3> seatPoints = FindPointsInsideSeat(obj);
                Debug.Log($"좌석 '{obj.name}'에서 {seatPoints.Count}개의 내부 절점 발견");
                seatCount++;
            }
        }
        
        Debug.Log($"총 {seatCount}개의 좌석 체크 완료, 총 {insidePoints.Count}개의 내부 절점 발견");
    }
} 