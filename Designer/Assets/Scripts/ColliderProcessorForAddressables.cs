using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Addressable 에셋 자동 콜라이더 처리 및 최적화를 위한 도구
/// </summary>
public class ColliderProcessorForAddressables : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("콜라이더 설정")]
    [Tooltip("로그 출력 여부")]
    public bool showDebugLog = true;
    
    [Tooltip("콜라이더 크기 조정 (1보다 크면 여유 공간 추가)")]
    public float colliderScale = 1.02f;
    
    [Tooltip("Convex MeshCollider 사용")]
    public bool useConvexMeshCollider = true;
    
    [Header("좌석용 설정")]
    [Tooltip("좌석 검색 조건 (이름에 포함된 텍스트)")]
    public string[] seatNameIdentifiers = { "seat", "chair" ,"rest"};
    
    [Tooltip("Y축 기준 분할 비율 (0~1)")]
    public float divisionRatio = 0.3f;
    
    [Tooltip("좌석은 항상 박스콜라이더로 분할")]
    public bool alwaysUseSplitBoxForSeats = true;
    
    [Header("성능 최적화")]
    [Tooltip("버텍스 수 기준 (이 수치 이상이면 BoxCollider 사용)")]
    public int vertexThresholdForBox = 1000;
    
    /// <summary>
    /// 현재 오브젝트와 모든 자식 오브젝트에 콜라이더를 처리합니다
    /// </summary>
    public void ProcessAllColliders()
    {
        int processedCount = 0;
        int seatCount = 0;
        
        // 현재 오브젝트와 모든 자식 오브젝트 처리
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        
        foreach (MeshFilter meshFilter in meshFilters)
        {
            GameObject obj = meshFilter.gameObject;
            
            if (IsSeatObject(obj))
            {
                ProcessSeatObject(obj);
                seatCount++;
            }
            else
            {
                ProcessRegularObject(obj);
            }
            
            processedCount++;
        }
        
        if (showDebugLog)
            Debug.Log($"콜라이더 처리 완료: 총 {processedCount}개 오브젝트 (좌석: {seatCount}개)");
    }
    
    /// <summary>
    /// 오브젝트가 좌석인지 확인합니다
    /// </summary>
    private bool IsSeatObject(GameObject obj)
    {
        string objName = obj.name.ToLower();
        
        foreach (string identifier in seatNameIdentifiers)
        {
            if (objName.Contains(identifier.ToLower()))
                return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 일반 오브젝트에 적절한 콜라이더를 생성합니다
    /// </summary>
    private void ProcessRegularObject(GameObject obj)
    {
        // 기존 콜라이더 제거
        Collider[] existingColliders = obj.GetComponents<Collider>();
        foreach (var collider in existingColliders)
        {
            DestroyImmediate(collider);
        }
        
        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
        if (!meshFilter || !meshFilter.sharedMesh)
            return;
            
        Mesh mesh = meshFilter.sharedMesh;
        
        // 버텍스 수에 따라 적절한 콜라이더 선택
        if (mesh.vertexCount > vertexThresholdForBox)
        {
            // 복잡한 메시는 BoxCollider 사용
            BoxCollider boxCollider = obj.AddComponent<BoxCollider>();
            boxCollider.center = mesh.bounds.center;
            boxCollider.size = mesh.bounds.size * colliderScale;
            
            if (showDebugLog)
                Debug.Log($"BoxCollider 생성: {obj.name} (버텍스 수: {mesh.vertexCount})");
        }
        else if (useConvexMeshCollider)
        {
            // 단순한 메시는 Convex MeshCollider 사용
            MeshCollider meshCollider = obj.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
            meshCollider.convex = true;
            
            if (showDebugLog)
                Debug.Log($"Convex MeshCollider 생성: {obj.name} (버텍스 수: {mesh.vertexCount})");
        }
        else
        {
            // 기본 BoxCollider 사용
            BoxCollider boxCollider = obj.AddComponent<BoxCollider>();
            boxCollider.center = mesh.bounds.center;
            boxCollider.size = mesh.bounds.size * colliderScale;
            
            if (showDebugLog)
                Debug.Log($"BoxCollider 생성: {obj.name}");
        }
    }
    
    /// <summary>
    /// 좌석 오브젝트에 분할된 BoxCollider를 생성합니다
    /// </summary>
    private void ProcessSeatObject(GameObject obj)
    {
        if (!alwaysUseSplitBoxForSeats && useConvexMeshCollider)
        {
            // 좌석이지만 분할하지 않고 Convex MeshCollider 사용
            ProcessRegularObject(obj);
            return;
        }
        
        // 기존 콜라이더 제거
        Collider[] existingColliders = obj.GetComponents<Collider>();
        foreach (var collider in existingColliders)
        {
            DestroyImmediate(collider);
        }
        
        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
        if (!meshFilter || !meshFilter.sharedMesh)
            return;
            
        Mesh mesh = meshFilter.sharedMesh;
        Vector3[] vertices = mesh.vertices;
        
        if (vertices.Length == 0)
            return;
            
        // Y값 기준으로 버텍스 분할
        List<Vector3> seatVertices = new List<Vector3>();
        List<Vector3> backrestVertices = new List<Vector3>();
        
        // Y값 범위 계산
        float minY = vertices.Min(v => v.y);
        float maxY = vertices.Max(v => v.y);
        float divisionY = minY + (maxY - minY) * divisionRatio;
        
        // 버텍스 분류
        foreach (Vector3 vertex in vertices)
        {
            if (vertex.y < divisionY)
                seatVertices.Add(vertex);
            else
                backrestVertices.Add(vertex);
        }
        
        // 좌석 부분 콜라이더 생성
        if (seatVertices.Count > 0)
        {
            BoxCollider seatCollider = obj.AddComponent<BoxCollider>();
            SetBoxColliderFromVertices(seatCollider, seatVertices, "Seat");
        }
        
        // 등받이 부분 콜라이더 생성
        if (backrestVertices.Count > 0)
        {
            BoxCollider backrestCollider = obj.AddComponent<BoxCollider>();
            SetBoxColliderFromVertices(backrestCollider, backrestVertices, "Backrest");
        }
        
        if (showDebugLog)
            Debug.Log($"좌석 분할 콜라이더 생성: {obj.name} (좌석: {seatVertices.Count}개, 등받이: {backrestVertices.Count}개 버텍스)");
    }
    
    /// <summary>
    /// 버텍스 집합으로부터 박스 콜라이더 설정
    /// </summary>
    private void SetBoxColliderFromVertices(BoxCollider collider, List<Vector3> vertices, string colliderName)
    {
        if (vertices.Count == 0)
            return;
            
        // 바운딩 박스 계산
        Vector3 min = vertices[0];
        Vector3 max = vertices[0];
        
        foreach (Vector3 vertex in vertices)
        {
            min = Vector3.Min(min, vertex);
            max = Vector3.Max(max, vertex);
        }
        
        // 박스 콜라이더 설정
        Vector3 center = (min + max) / 2f;
        Vector3 size = max - min;
        
        // 약간의 여유 추가
        size *= colliderScale;
        
        collider.center = center;
        collider.size = size;
        collider.name = colliderName;
    }
    
    /// <summary>
    /// 에디터에서 테스트 실행용 버튼
    /// </summary>
    [ContextMenu("모든 콜라이더 처리")]
    private void ProcessAllCollidersMenu()
    {
        ProcessAllColliders();
    }
#endif
} 