using UnityEngine;

[ExecuteInEditMode]
public class MeshVolumeCalculator : MonoBehaviour
{
    [Header("🧪 밀도 (kg/m³)")]
    public float density = 1000f; // 기본값: 물의 밀도

    void Awake()
    {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        if (meshFilters.Length == 0)
        {
            Debug.LogError("MeshFilter를 찾을 수 없습니다!");
            return;
        }

        float totalVolume = 0f;

        foreach (MeshFilter meshFilter in meshFilters)
        {
            Mesh mesh = meshFilter.sharedMesh;
            if (mesh == null) continue;

            Matrix4x4 localToWorld = meshFilter.transform.localToWorldMatrix;
            float volume = CalculateMeshVolume(mesh, localToWorld, meshFilter.transform.position);
            totalVolume += volume;
        }

        float totalMass = totalVolume * density;

        Debug.Log($"총 부피: {totalVolume:F4} m³ | 밀도: {density} kg/m³ | 👉 질량: {totalMass:F4} kg");
    }

    float CalculateMeshVolume(Mesh mesh, Matrix4x4 transformMatrix, Vector3 objectPosition)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        float totalVolume = 0f;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 p1 = transformMatrix.MultiplyPoint3x4(vertices[triangles[i]]);
            Vector3 p2 = transformMatrix.MultiplyPoint3x4(vertices[triangles[i + 1]]);
            Vector3 p3 = transformMatrix.MultiplyPoint3x4(vertices[triangles[i + 2]]);

            // 위치 보정
            p1 -= objectPosition;
            p2 -= objectPosition;
            p3 -= objectPosition;

            float volume = SignedVolumeOfTriangle(p1, p2, p3);
            totalVolume += volume;
        }

        return Mathf.Abs(totalVolume); // 스케일 반영 완료됨
    }

    float SignedVolumeOfTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        return Vector3.Dot(p1, Vector3.Cross(p2, p3)) / 6.0f;
    }
}