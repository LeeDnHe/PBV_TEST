#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MeshMergerEditor : MonoBehaviour
{
    [ContextMenu("Merge and Save Mesh")]
    void MergeAndSaveMesh()
    {
        MeshFilter parentMeshFilter = GetComponent<MeshFilter>();
        if (parentMeshFilter == null)
            parentMeshFilter = gameObject.AddComponent<MeshFilter>();

        MeshRenderer parentRenderer = GetComponent<MeshRenderer>();
        if (parentRenderer == null)
            parentRenderer = gameObject.AddComponent<MeshRenderer>();

        List<MeshFilter> meshFilters = new List<MeshFilter>();
        GetMeshFiltersRecursively(transform, meshFilters);

        if (meshFilters.Count == 0) return;

        List<CombineInstance> combines = new List<CombineInstance>();
        List<Material> materials = new List<Material>();
        List<GameObject> objectsToDestroy = new List<GameObject>();

        foreach (MeshFilter mf in meshFilters)
        {
            if (mf == parentMeshFilter) continue;
            Mesh mesh = mf.sharedMesh;
            if (mesh == null) continue;

            CombineInstance combine = new CombineInstance();
            combine.mesh = mesh;
            combine.transform = mf.transform.localToWorldMatrix;
            combines.Add(combine);

            MeshRenderer renderer = mf.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                materials.Add(renderer.sharedMaterial);
            }

            objectsToDestroy.Add(mf.gameObject);
        }

        // 🔥 반복문이 끝난 후 삭제
        foreach (GameObject obj in objectsToDestroy)
        {
            DestroyImmediate(obj);
        }

        // ✅ 새 Mesh 생성
        Mesh mergedMesh = new Mesh();

        // ✅ UInt32 IndexFormat 적용
        if (combines.Count > 0)
        {
            mergedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        // ✅ Mesh 병합
        mergedMesh.CombineMeshes(combines.ToArray(), false, true);

        // ✅ 병합된 Mesh 적용
        parentMeshFilter.mesh = mergedMesh;
        parentRenderer.materials = materials.ToArray();

        // **Mesh를 에셋으로 저장**
        SaveMeshAsset(mergedMesh, "MergedMesh_" + gameObject.name);
    }

    void GetMeshFiltersRecursively(Transform parent, List<MeshFilter> filters)
    {
        foreach (Transform child in parent)
        {
            MeshFilter mf = child.GetComponent<MeshFilter>();
            if (mf != null)
            {
                filters.Add(mf);
            }
            GetMeshFiltersRecursively(child, filters); // 재귀적으로 하위 객체 탐색
        }
    }

    void SaveMeshAsset(Mesh mesh, string name)
    {
        string path = "Assets/MergedMeshes/";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder("Assets", "MergedMeshes");
        }

        string assetPath = path + name + ".asset";
        AssetDatabase.CreateAsset(mesh, assetPath);
        AssetDatabase.SaveAssets();

        Debug.Log("Merged mesh saved at: " + assetPath);
    }
}
#endif
