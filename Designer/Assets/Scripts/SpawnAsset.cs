using System;
using System.Collections;
using System.Collections.Generic;
using Exoa.Effects;
using RTG;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SpawnAsset : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public AllocateGizmo allocateGizmo;
    public AssetThumbnail assetThumbnail;
    private Image _thumbnailImage;
    
    private GameObject _asset;
    private Vector3 _worldPosition;
    
    public Shader gltfShader;

    private void Start()
    {
        _thumbnailImage = GetComponent<Image>();
    }

    public void SetAssetThumbnail(AssetThumbnail thumbnail)
    {
        assetThumbnail = thumbnail;
        _thumbnailImage.sprite = thumbnail.thumbnail;
    }
    
    // 동일한 오브젝트 여러개면 Hierarchy 창에서 클릭해도 제대로 찾을 수 없기 때문에 구분되도록 이름 정해줘야 함.
    /// <summary>
    /// 일단 임시로 생성된 시간으로 이름 할당
    /// </summary>
    /// <returns></returns>
    private string GetUniqueName(string originalName)
    {
        return $"{originalName}_{DateTime.Now.Ticks}";
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if(!assetThumbnail) return;
        
        _asset = Instantiate(assetThumbnail.assetObject);
        
        // 동일 이름 오브젝트 구분을 위해 네이밍 규칙 도입
        AdjustName(_asset);
        
        _asset.SetActive(true);
        
        // 현재 마우스(터치) 위치를 가져옴
        _worldPosition.Set(eventData.position.x, eventData.position.y, Camera.main.nearClipPlane + 5f);
        // _worldPosition.Set(eventData.position.x, eventData.position.y, 0);
    
        // 화면 좌표를 월드 좌표로 변환
        _worldPosition = Camera.main.ScreenToWorldPoint(_worldPosition);
        
        _asset.transform.position = _worldPosition;
        
        _asset.transform.position.Set(_worldPosition.x, _worldPosition.y, 0);
        
        // glTF면 적용해야 할 Shader가 달라짐.
        if (assetThumbnail.assetObject.CompareTag($"glTF"))
        {
            AdjustMaterial(_asset);
            return;
        }
			     
        foreach (MeshRenderer mat in _asset.GetComponentsInChildren<MeshRenderer>())
        {
            mat.material.shader = Shader.Find("Standard");
        } 
    }

    private void AdjustName(GameObject obj)
    {
        GameObject[] objects = GetAllObjects(obj.transform);

        foreach (var item in objects)
        {
            item.name = GetUniqueName(item.name);
        }
    }
    
    private GameObject[] GetAllObjects(Transform root)
    {
        // 모든 자식의 Renderer를 담을 리스트
        List<GameObject> objects = new List<GameObject>();

        objects.Add(root.gameObject);
        
        // 모든 자식 Transform을 재귀적으로 검사
        foreach (Transform child in root)
        {
            objects.AddRange(GetAllObjects(child));
        }

        return objects.ToArray();
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
                item.material.shader = gltfShader;
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

    public void OnPointerUp(PointerEventData eventData)
    {
        if(!assetThumbnail) return;
        
        allocateGizmo.SetOffAllOutlinables();
        allocateGizmo.AllocateGizmoObject(_asset);
        
        var list = new List<GameObject> { _asset };
        
        if (_asset.TryGetComponent(out Outlinable outlinable))
        {
            allocateGizmo.AddOutlinable(outlinable);
            outlinable.enabled = true;
            
            var totalObjectAction = new TotalAction(list, null, outlinable, allocateGizmo, allocateGizmo.runtimeInspector);
            totalObjectAction.Execute();
            
            allocateGizmo.totalAction = totalObjectAction;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if(!assetThumbnail) return;
        
        _worldPosition.Set(eventData.position.x, eventData.position.y, Camera.main.nearClipPlane + 5f);
    
        // 화면 좌표를 월드 좌표로 변환
        _worldPosition = Camera.main.ScreenToWorldPoint(_worldPosition);
        
        _asset.transform.position = _worldPosition;
    }
}
