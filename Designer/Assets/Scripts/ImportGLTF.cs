using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class ImportGLTF : MonoBehaviour
{
    // https://raw.githubusercontent.com/<username>/<repository>/<branch>/<path-to-file>
    // https://raw.githubusercontent.com/Youkwangchae/GLTF-file-storage/main/big_country_labs_style_spoiler_wing/scene.gltf

    public List<string> gltfAssets = new List<string>();
    private GLTFast.GltfAsset gltfLoader;
    private GLTFast.GltfBoundsAsset gltfBoundsLoader;
    private int assetIndex = 0;
    private List<Bounds> boundsList = new List<Bounds>();
    private List<string> nodeNames = new List<string>();
    
    // Start is called before the first frame update
    void Start()
    {
        gltfLoader = gameObject.GetComponent<GLTFast.GltfAsset>();
        gltfBoundsLoader = gameObject.GetComponent<GLTFast.GltfBoundsAsset>();
        GetGltfFile();
    }

    private void CheckNodeName()
    {
        if (nodeNames.Contains(gameObject.transform.GetChild(assetIndex).name))
        {
            gameObject.transform.GetChild(assetIndex).name += assetIndex;
        }
        
        nodeNames.Add(gameObject.transform.GetChild(assetIndex).name);
    }

    private async void GetGltfFile()
    {
        try
        {
            if (assetIndex == gltfAssets.Count)
            {
                int assetCount = gltfAssets.Count;
                for (int i = 0; i < assetCount; i++)
                {
                    AdjustColliderToFitAll(i, boundsList[i]);
                }

                // root노드를 부모로하는 오브젝트로 설정
                for (int i = 0; i < assetCount; i++)
                {
                    gameObject.transform.GetChild(assetCount - i - 1).SetParent(null);
                }
                return;
            }
            
            await gltfBoundsLoader.Load(gltfAssets[assetIndex]);
            
            boundsList.Add(gltfBoundsLoader.Bounds);

            if(assetIndex < gameObject.transform.childCount)
                gameObject.transform.GetChild(assetIndex).AddComponent<BoxCollider>();
            
            CheckNodeName();
        
            assetIndex++;
        
            GetGltfFile();
        }
        catch (Exception e)
        {
            return;
        }
    }
    
    private void AdjustColliderToFitAll(int num, Bounds bounds)
    {
        Transform child = gameObject.transform.GetChild(num);
        BoxCollider boxCollider = child.GetComponent<BoxCollider>();
        
        // Debug.Log("Combined Size : " +bounds.size);
            
        // Box Collider의 크기와 위치 조정
        boxCollider.center = child.InverseTransformPoint(bounds.center); 
        boxCollider.size = child.InverseTransformVector(bounds.size);
        
        // 재귀적으로 콜라이더 붙이기
        AddAllColliders(child);
    }

    private void AddAllColliders(Transform root)
    {
        // 콜라이더가 있는지 확인
        if (root.TryGetComponent(out BoxCollider item))
        {
            
        }
        else
        {
            // 없으면 추가
            root.AddComponent<BoxCollider>();
        }
        
        // 모든 자식 Transform을 재귀적으로 검사
        foreach (Transform child in root)
        {
            AddAllColliders(child);
        }
    }
    

}
