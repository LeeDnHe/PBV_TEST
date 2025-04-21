using UnityEngine;
using System.Collections.Generic;

public class AutoColliderGenerator : MonoBehaviour
{
    [Header("콜라이더 생성 설정")]
    [Tooltip("Y값 기준으로 메시를 분할할 위치 (0~1 사이 값, 0이면 맨 아래, 1이면 맨 위)")]
    public float divisionRatio = 0.5f;
    
    [Tooltip("생성된 박스 콜라이더 크기 조정 (약간 더 크게/작게)")]
    public float colliderScale = 1.02f;
    
    [Tooltip("로그 출력 여부")]
    public bool showDebugLog = true;

    /// <summary>
    /// 선택된 오브젝트에 자동으로 분리된 박스콜라이더를 생성합니다
    /// </summary>
    /// <param name="targetObject">콜라이더를 생성할 대상 오브젝트</param>
    public void GenerateSeatColliders(GameObject targetObject)
    {
        if (!targetObject)
        {
            Debug.LogError("대상 오브젝트가 없습니다.");
            return;
        }
        
        // 기존 콜라이더 제거
        Collider[] existingColliders = targetObject.GetComponents<Collider>();
        foreach (var collider in existingColliders)
        {
            DestroyImmediate(collider);
        }
        
        MeshFilter meshFilter = targetObject.GetComponent<MeshFilter>();
        if (!meshFilter || !meshFilter.sharedMesh)
        {
            Debug.LogError("대상 오브젝트에 MeshFilter가 없거나 메시가 할당되지 않았습니다.");
            return;
        }
        
        Mesh mesh = meshFilter.sharedMesh;
        
        // 로컬 좌표계의 버텍스 위치 기반 분류
        List<Vector3> seatVertices = new List<Vector3>();
        List<Vector3> backrestVertices = new List<Vector3>();
        
        // 메시 분석을 위한 버텍스 추출
        Vector3[] vertices = mesh.vertices;
        
        if (vertices.Length == 0)
        {
            Debug.LogError("메시에 버텍스가 없습니다.");
            return;
        }
        
        // Y값 범위 계산
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        
        foreach (Vector3 vertex in vertices)
        {
            minY = Mathf.Min(minY, vertex.y);
            maxY = Mathf.Max(maxY, vertex.y);
        }
        
        // 분할 기준점 계산
        float divisionY = minY + (maxY - minY) * divisionRatio;
        
        if (showDebugLog)
            Debug.Log($"메시 Y 범위: {minY} ~ {maxY}, 분할 기준점: {divisionY}");
        
        // 버텍스 분류
        foreach (Vector3 vertex in vertices)
        {
            if (vertex.y < divisionY)
                seatVertices.Add(vertex);
            else
                backrestVertices.Add(vertex);
        }
        
        if (showDebugLog)
            Debug.Log($"좌석 부분 버텍스: {seatVertices.Count}개, 등받이 부분 버텍스: {backrestVertices.Count}개");
        
        // 좌석 부분 콜라이더 생성
        if (seatVertices.Count > 0)
        {
            BoxCollider seatCollider = targetObject.AddComponent<BoxCollider>();
            SetBoxColliderFromVertices(seatCollider, seatVertices);
            seatCollider.name = "SeatCollider";
            
            if (showDebugLog)
                Debug.Log($"좌석 콜라이더 생성: 중심={seatCollider.center}, 크기={seatCollider.size}");
        }
        
        // 등받이 부분 콜라이더 생성
        if (backrestVertices.Count > 0)
        {
            BoxCollider backrestCollider = targetObject.AddComponent<BoxCollider>();
            SetBoxColliderFromVertices(backrestCollider, backrestVertices);
            backrestCollider.name = "BackrestCollider";
            
            if (showDebugLog)
                Debug.Log($"등받이 콜라이더 생성: 중심={backrestCollider.center}, 크기={backrestCollider.size}");
        }
    }
    
    /// <summary>
    /// 버텍스 집합으로부터 박스 콜라이더의 위치와 크기를 계산하여 설정합니다
    /// </summary>
    private void SetBoxColliderFromVertices(BoxCollider collider, List<Vector3> vertices)
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
    }
    
    /// <summary>
    /// 현재 선택된 오브젝트에 콜라이더를 생성합니다 (인스펙터에서 버튼으로 호출용)
    /// </summary>
    public void GenerateCollidersForCurrentObject()
    {
        GenerateSeatColliders(gameObject);
    }
    
    /// <summary>
    /// 모든 자식 오브젝트에 콜라이더를 생성합니다
    /// </summary>
    public void GenerateCollidersForAllChildren()
    {
        foreach (Transform child in transform)
        {
            if (child.TryGetComponent<MeshFilter>(out MeshFilter meshFilter) && meshFilter.sharedMesh != null)
            {
                if (showDebugLog)
                    Debug.Log($"자식 오브젝트 처리: {child.name}");
                    
                GenerateSeatColliders(child.gameObject);
            }
        }
    }
    
    /// <summary>
    /// 모든 자식 오브젝트의 MeshCollider를 Convex BoxCollider로 변환합니다
    /// </summary>
    public void ConvertMeshCollidersToBox()
    {
        MeshCollider[] meshColliders = GetComponentsInChildren<MeshCollider>();
        
        foreach (MeshCollider meshCollider in meshColliders)
        {
            GameObject obj = meshCollider.gameObject;
            Mesh mesh = meshCollider.sharedMesh;
            
            if (mesh != null)
            {
                // MeshCollider 제거
                DestroyImmediate(meshCollider);
                
                // BoxCollider 추가
                BoxCollider boxCollider = obj.AddComponent<BoxCollider>();
                boxCollider.center = mesh.bounds.center;
                boxCollider.size = mesh.bounds.size * colliderScale;
                
                if (showDebugLog)
                    Debug.Log($"MeshCollider → BoxCollider 변환: {obj.name}");
            }
        }
    }
    
    /// <summary>
    /// Addressable 에셋용 Convex MeshCollider로 변환합니다
    /// </summary>
    public void ConvertToConvexMeshColliders()
    {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        
        foreach (MeshFilter meshFilter in meshFilters)
        {
            GameObject obj = meshFilter.gameObject;
            
            // 기존 콜라이더 제거
            Collider[] colliders = obj.GetComponents<Collider>();
            foreach (Collider collider in colliders)
            {
                DestroyImmediate(collider);
            }
            
            // 새 MeshCollider 추가
            if (meshFilter.sharedMesh != null)
            {
                MeshCollider meshCollider = obj.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = meshFilter.sharedMesh;
                meshCollider.convex = true;
                
                if (showDebugLog)
                    Debug.Log($"Convex MeshCollider 생성: {obj.name}");
            }
        }
    }
} 