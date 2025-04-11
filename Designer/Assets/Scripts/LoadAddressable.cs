using System.Collections;
using System.Collections.Generic;
using Exoa.Effects;
using RuntimeInspectorNamespace;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class LoadAddressable : MonoBehaviour
{
    public GameObject startPopup;
    public GameObject downloadPopup;
    public GameObject loadingPopup;
    public TMP_Text byteText;
    public Slider downloadSlider;
    public TMP_Text perText;
    
    public Shader gltfShader;
    public RecycledListView recycledListView;
    
    public List<AssetLabelReference> addressableLabels = new List<AssetLabelReference>();
    
    public List<AssetThumbnail> addressableThumbnails = new List<AssetThumbnail>();
    public Transform thumbnailContainer;
    
    private AsyncOperationHandle _handle;

    public void StartBtn()
    {
	    StartCoroutine(LoadAllAssets());
	    startPopup.SetActive(false);
    }

    IEnumerator LoadAllAssets()
    {
	    List<AsyncOperationHandle<IList<GameObject>>> handles = new List<AsyncOperationHandle<IList<GameObject>>>();
	    
	    foreach (AssetLabelReference key in addressableLabels)
	    {
		    // Addressable 다운로드 요청
		    AsyncOperationHandle<IList<GameObject>> handle = Addressables.LoadAssetsAsync<GameObject>(key.labelString, null);
		    handles.Add(handle);
		    
		    yield return handle;
	    }
	    
	    foreach (AsyncOperationHandle<IList<GameObject>> handle in handles)
	    {
		    if (handle.Status == AsyncOperationStatus.Succeeded)
		    {
			    var standardShader = Shader.Find("Standard");
			    
			    foreach (var prefab in handle.Result)
			    {
				    var obj = Instantiate(prefab);

				    // 등록된 썸네일 있으면 따로 기록해두기 
				    if (obj.TryGetComponent(out AssetThumbnail thumbnail))
				    {
					    addressableThumbnails.Add(thumbnail);
					    thumbnail.assetObject = obj;
				    }

				    if (obj.TryGetComponent(out Outlinable outlinable))
				    {
					    outlinable.enabled = false;
				    }
				    
				    // glTF면 적용해야 할 Shader가 달라짐.
				    if (obj.CompareTag($"glTF"))
				    {
					    AdjustMaterial(obj);
					    continue;
				    }
			    
				    foreach (MeshRenderer mat in obj.GetComponentsInChildren<MeshRenderer>())
				    {
					    foreach (var material in mat.materials)
					    {
						    material.shader = standardShader;
					    }
				    }   
			    }
		    }
		    else
		    {
			    Debug.Log($"Failed to load Addressables with handle: {handle}");
		    }
		    
		    // 메모리 해제
		    // 근데 해제하면 MeshFilter의 Mesh Missing 오류 발생해서 일단 주석처리.
		    // Addressables.Release(handle);
		    
		    // 에셋들 썸네일 등록
		    SetThumbnail();
	    }
	    
	    for (int i = 0; i < addressableThumbnails.Count; i++)
	    {
		    addressableThumbnails[i].assetObject.SetActive(false);
		    
		    // 초기에 DB에서 불러온 오브젝트는 안 보이게 설정.
		    RuntimeInspectorUtils.IgnoredTransformsInHierarchy.Add(addressableThumbnails[i].assetObject.transform);
	    }
	    
	    recycledListView.UpdateItems();
	    
	    // 오브젝트들이 렌더링 할 시간 확보
	    yield return new WaitForSeconds(0.5f);
	    
	    // Default에 대해 Culling Mask를 해제해 이제 볼 수 있게 됨.
	    Camera.main.cullingMask |= LayerMask.GetMask("Default");
    }

    private void SetThumbnail()
    {
	    for (int i = 0; i < addressableThumbnails.Count; i++)
	    {
		    if(i == thumbnailContainer.childCount) return;
		    thumbnailContainer.transform.GetChild(i).GetComponent<SpawnAsset>().SetAssetThumbnail(addressableThumbnails[i]);
	    }
    }

    private void NextShow(string name)
    {
	    AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(name, new Vector3(-5, 0, 0), Quaternion.identity);    
	    
	    handle.Completed += (op) =>
	    {
		    if (op.Status != AsyncOperationStatus.Succeeded) return;
		    
		    var standardShader = Shader.Find("Standard");
	    
		    // 불러온 obj 파일이 분홍색인 오류 수정
		    GameObject obj = op.Result;
	    
		    // glTF면 적용해야 할 Shader가 달라짐.
		    if (obj.CompareTag($"glTF"))
		    {
			    AdjustMaterial(obj);
			    return;   
		    }
	    
		    foreach (MeshRenderer mat in obj.GetComponentsInChildren<MeshRenderer>())
		    {
			    foreach (var material in mat.materials)
			    {
				    material.shader = standardShader;
			    }
		    }
	    };
    }

    public void DownloadBtn()
    {
	    StartCoroutine(DownloadAllAssets());
	    downloadPopup.SetActive(false);
    }

    IEnumerator DownloadAllAssets()
    {
	    _handle = Addressables.DownloadDependenciesAsync("glTF");
	    StartCoroutine(ShowPercentage());

	    yield return _handle;
	    yield return new WaitForSeconds(1f);
	    
	    loadingPopup.SetActive(false);
	    NextShow("");
	    Addressables.Release(_handle);
    }

    IEnumerator ShowPercentage()
    {
	    loadingPopup.SetActive(true);
	    yield return new WaitUntil(() => _handle.IsValid());

	    while (_handle.PercentComplete < 1)
	    {
		    downloadSlider.value = _handle.PercentComplete;
		    perText.text = $"{_handle.PercentComplete * 100:F2}%";
		    yield return null;
	    }
	    downloadSlider.value = downloadSlider.maxValue;
	    perText.text = "100%";
    }
    
    private void AdjustMaterial(GameObject obj)
    {
    	// 모든 Renderer를 재귀적으로 가져오기
	    MeshRenderer[] renderers = GetAllRenderers(obj.transform);

    	if (renderers.Length > 0)
    	{
    		// 모든 Renderer에 올바른 shader 할당
    		foreach (MeshRenderer item in renderers)
    		{
			    foreach (var material in item.materials)
			    {
				    material.shader = gltfShader;				    				    
			    }
    			// item.material.shader = gltfShader;
    		}
    	}
    	else
    	{
    		Debug.LogWarning("Renderer가 없습니다.");
    	}
    }
    
    private MeshRenderer[] GetAllRenderers(Transform root)
    {
    	// 모든 자식의 Renderer를 담을 리스트
    	List<MeshRenderer> renderers = new List<MeshRenderer>();

    	if (root.TryGetComponent(out MeshRenderer item))
    	{
    		renderers.Add(item);
    	}
        
    	// 모든 자식 Transform을 재귀적으로 검사
    	foreach (Transform child in root)
    	{
    		renderers.AddRange(GetAllRenderers(child));
    	}

    	return renderers.ToArray();
    }
    
    public void LoadByAddress()
    {
        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync("Test/Bullbar");
        
        handle.Completed += (op) =>
        {
            Debug.Log(op.Status);
            
            if (op.Status != AsyncOperationStatus.Succeeded) return;

            // 불러온 obj 파일이 분홍색인 오류 수정
            GameObject obj = op.Result;

            foreach (MeshRenderer mat in obj.GetComponentsInChildren<MeshRenderer>())
            {
                mat.material.shader = Shader.Find("Standard");
            }
        };
        
        // 쓰지 않는건 메모리 해제.
        // Addressables.Release(handle);
    }
}
