using System;
using System.Collections;
using System.Collections.Generic;
using Dummiesman;
using System.IO;
using System.Text;
using RTG;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class AssetHierarchy
{
	public string assetName;
	public string url;
	[Header("MTL 파일 있는 경우")]
	public bool isHaveMTL;
	public string mtlURL;
	public List<AssetURL> assets = new List<AssetURL>();
}

[Serializable]
public class AssetURL
{
	public string name;
	public string textureURL;
	public string normalMapURL;
	public string occlusionMapURL;
	public string metallicMapURL;
	public string smoothnessMapURL;
	public string heightMapURL;
}

public class ObjFromStream : MonoBehaviour
{
	[SerializeField] public List<AssetHierarchy> AssetHierarchies = new List<AssetHierarchy>();
	public bool isUsingSpecificServer = false;

	public StringParser stringParser;
	public AllocateGizmo gizmoManager;
	
	private const string URL_PATH = "https://raw.githubusercontent.com/Youkwangchae/OBJ-file-storage/main/";
	
	//현재 적용하는 텍스쳐 개수
	//[^1]       텍스쳐맵
	//[1]       노멀맵
	//[2]       어클루전
	private const int APPLY_TEXTURE_CNT = 6;
	
	// 다운로드가 완료되었는지 확인하는 코루틴의 딜레이 초
	private WaitForSeconds DELAYTIME = new WaitForSeconds(1f);

	// 현재 진행중인 아이템 세트(obj + textures)의 남은 개수
	private static uint mQueueLength = 0;
	
	string baseUrl = "https://drive.google.com/uc?export=download";
	
	private List<string> nodeNames = new List<string>();
	private int assetIndex = 0;

	public class QueueChecker
	{
		public GameObject instantiatedObj;	// 씬에 배치될 오브젝트
		public Texture2D[] textures;		// 텍스쳐 파일들
		public bool[] isPassed;				// 텍스쳐 파일이 없어서 스킵하는지 확인
		public bool isObjectNull;			// 오브젝트 name을 서버에서 못 찾는 경우 다운로드 작업을 중단하기 위해 확인

		public QueueChecker()				// 기본 생성자
		{
			isPassed = new bool[APPLY_TEXTURE_CNT];
			textures = new Texture2D[APPLY_TEXTURE_CNT];
			isObjectNull = false;
			
			for(int i=0;i<isPassed.Length;i++) isPassed[i] = false;
		}
	}

	// 스트림으로 받아온 오브젝트를 특정 부모의 자식으로 취급하기 위해 사용한다.
	[SerializeField] private Transform[] mParentObj;
	
	// 스트림으로 받아온 오브젝트를 관리하기 위한 리스트
	private List<GameObject> mInstantiatedObj;
	
	void Start () {
		
		mInstantiatedObj = new List<GameObject>();
		
		StartDownloadFile(AssetHierarchies[^1].assetName, -1);
	}

	// 해당 함수로 다운로드를 시작한다.
	public void StartDownloadFile(string objName, int parentID = -1)
	{
		++mQueueLength;
		
		StartCoroutine(StartDownloadQueue(objName, parentID));
	}

	// 다운로드를 시작한다.
	private IEnumerator StartDownloadQueue(string objName, int parentID)
	{
		// 새로 다운로드 시작한다
		// queueData에서 다운로드 확인에 필요한 데이터들을 체크한다.
		QueueChecker queueData = new QueueChecker();

		// 오브젝트 다운로드 시작.
		StartCoroutine(DownloadObj(objName, queueData));

		for (int j = 0; j < AssetHierarchies[^1].assets.Count; j++)
		{
			if (j != 0)
			{
				queueData.textures = new Texture2D[APPLY_TEXTURE_CNT];
				
				for(int i=0;i<queueData.isPassed.Length;i++) 
					queueData.isPassed[i] = false;
			}
			
			// 텍스쳐 다운로드 시작.
			for (int i = 0; i < APPLY_TEXTURE_CNT; i++)
			{
				StartCoroutine(DownloadTexture(objName, queueData, i, j));
			}

			int iterator;

			while (true)
			{
				if (queueData.isObjectNull)
				{
					Debug.Log("서버에서 " + objName+"를 찾을 수 없음");
					
					--mQueueLength;
					yield return DELAYTIME;
					yield break;
				}

				// 오브젝트 다운로드 했는지 확인.
				if (queueData.instantiatedObj == null)
				{
					Debug.Log("obj 다운로드 중...");
						
					yield return DELAYTIME;
					continue;
				}

				// 텍스쳐 다운로드 했는지 확인.
				for (iterator = 0; iterator < APPLY_TEXTURE_CNT; ++iterator)
				{
					if(queueData.isPassed[iterator]) continue;

					if (queueData.textures[iterator] == null)
					{
						Debug.Log(iterator+" 번째 이미지 다운로드중..");
						
						yield return DELAYTIME;
						break;
					}
				}
				
				if(iterator != APPLY_TEXTURE_CNT) continue;

				// 여기서 오브젝트를 활성화시키고, 텍스쳐를 입힌다.
				
				// URP Lit Standard 쉐이더 코드 참조
				//https://github.com/Unity-Technologies/MeasuredMaterialLibraryURP/blob/master/Assets/Measured%20Materials%20Library/ClearCoat/Shaders/Lit.shader
				
				// SetFloat 함수 (메탈릭 스무스 값)
				//https://forum.unity.com/threads/set-smoothness-of-material-in-script.381247/

				if (queueData.instantiatedObj.transform.GetChild(j).TryGetComponent(out MeshRenderer meshRenderer))
				{
					//(가능)텍스쳐 변경
					meshRenderer.material.mainTexture = queueData.textures[0];
				
					//(가능)노멀맵 변경
					meshRenderer.material.SetTexture("_BumpMap", queueData.textures[1]);
				
					//(가능)오클루전맵 변경
					meshRenderer.material.SetTexture("_OcclusionMap", queueData.textures[2]);
				
					// 메탈릭 변경
					meshRenderer.material.SetTexture("_MetallicGlossMap", queueData.textures[3]);
				
					//메탈릭 스무스 값 설정 (0~1)
					meshRenderer.material.SetFloat("_Smoothness", 0f);
				
					// roughness 반영
					meshRenderer.material.SetTexture("_SpecGlossMap", queueData.textures[4]);
				
					// height map 반영
					meshRenderer.material.SetTexture("_ParallaxMap", queueData.textures[5]);
				}
				
				// MeshRenderer renderer = queueData.instantiatedObj.transform.GetChild(j).GetComponent<MeshRenderer>();
				
				// var newMtl = new Material(Shader.Find("Standard (Specular setup)"));
				// renderer.material = newMtl;
				
				// 기본 Standard 쉐이더 코드에서 Texture로는 다음 설정들이 있다.
				// _MainTex : mainTexture
				// _MetallicGlossMap : metallic
				// _BumpMap : normalMap
				// _ParallaxMap : Height Map
				// _OcclusionMap
				// _EmissionMap : Emission
				// _DetailMask : Detail Mask
				// _DetailNormalMap : NormalMap
				
				// 리스트에 해당 오브젝트를 관리하기 위해 레퍼런스를 넣는다.
				mInstantiatedObj.Add(queueData.instantiatedObj);
				
				// 만약 parentID가 -1이 아니라면 (의도하여 부모에게 넣으라고 했다면) 해당 부모 오브젝트의 자식으로 처리한다.
				if (parentID != -1)
				{
					queueData.instantiatedObj.transform.parent = mParentObj[parentID];
				}

				// 오브젝트를 활성화시킨다.
				// 클릭하면 Gizmo를 띄우기 위해 콜라이더를 집어넣는다.
				// queueData.instantiatedObj.SetActive(true);
				// gizmoManager.MakeGizmo(queueData.instantiatedObj);
				--mQueueLength;
				break;
			}	
		}
		
		queueData.instantiatedObj.SetActive(true);
		// 클릭하면 Gizmo를 띄우기 위해 콜라이더를 집어넣는다.
		queueData.instantiatedObj.AddComponent<BoxCollider>();
		
		CheckNodeName(queueData.instantiatedObj);
		
		AdjustColliderToFitAll(queueData.instantiatedObj);
		
		// root노드를 부모로하는 오브젝트로 설정
		queueData.instantiatedObj.transform.SetParent(null);
	}
	
	private void CheckNodeName(GameObject obj)
	{
		if (nodeNames.Contains(obj.name))
		{
			obj.name += assetIndex;
		}
        
		nodeNames.Add(obj.name);
	}
	
	private void AdjustColliderToFitAll(GameObject obj)
	{
		BoxCollider boxCollider = obj.GetComponent<BoxCollider>();
        
		// 모든 Renderer를 재귀적으로 가져오기
		MeshFilter[] renderers = GetAllRenderers(obj.transform);

		if (renderers.Length > 0)
		{
			// 첫 번째 Renderer의 Bounds로 초기화
			Bounds combinedBounds = renderers[0].mesh.bounds;

			// 모든 Renderer의 Bounds를 합산
			foreach (MeshFilter item in renderers)
			{
				combinedBounds.Encapsulate(item.mesh.bounds);
			}
            
			// Debug.Log("Combined Size : " +combinedBounds.size);
            
			// Box Collider의 크기와 위치 조정
			boxCollider.center = combinedBounds.center; 
			boxCollider.size = combinedBounds.size;
		}
		else
		{
			Debug.LogWarning("Renderer가 없습니다. Collider 크기를 조정할 수 없습니다.");
		}
	}

	private MeshFilter[] GetAllRenderers(Transform root)
	{
		// 모든 자식의 Renderer를 담을 리스트
		List<MeshFilter> renderers = new List<MeshFilter>();

		if (root.TryGetComponent(out MeshFilter item))
		{
			renderers.Add(item);
		}
		
		// 콜라이더가 있는지 확인
		if (root.TryGetComponent(out BoxCollider collider))
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
			renderers.AddRange(GetAllRenderers(child));
		}

		return renderers.ToArray();
	}
	
	private IEnumerator DownloadObj(string objName, QueueChecker queueData)
	{
		string finalURL = URL_PATH + objName + ".obj";

		if (isUsingSpecificServer)
		{
			if (!String.IsNullOrEmpty(AssetHierarchies[^1].url))
			{
				finalURL = AssetHierarchies[^1].url;
			}	
		}
		else
			finalURL = $"{baseUrl}&id={AssetHierarchies[^1].url}";
		
		if (finalURL.Contains("view"))
			finalURL = $"{baseUrl}&id={stringParser.ExtractBetween(finalURL, "d/", "/view")}";
		
		using (UnityWebRequest www = UnityWebRequest.Get(finalURL))
		{
			yield return www.Send();

			if (www.isNetworkError || www.isHttpError)
			{
				Debug.Log(www.error);
				queueData.isObjectNull = true;
			}
			else
			{
				if (AssetHierarchies[^1].isHaveMTL)
				{
					string mtlPath = URL_PATH + objName + ".mtl";

					if (isUsingSpecificServer)
					{
						if (!String.IsNullOrEmpty(AssetHierarchies[^1].mtlURL))
						{
							mtlPath = AssetHierarchies[^1].mtlURL;
						}
					}
					else
						mtlPath = $"{baseUrl}&id={AssetHierarchies[^1].mtlURL}";
					
					if (mtlPath.Contains("view"))
						mtlPath = $"{baseUrl}&id={stringParser.ExtractBetween(mtlPath, "d/", "/view")}";
					
					UnityWebRequest mtlRequest = UnityWebRequest.Get(mtlPath);
					yield return mtlRequest.SendWebRequest();

					if (mtlRequest.result != UnityWebRequest.Result.Success)
					{
						Debug.LogError($"Error: {mtlRequest.error}");
						yield break;
					}
					
					var textStream = new MemoryStream(Encoding.UTF8.GetBytes(www.downloadHandler.text));
					var mtlStream = new MemoryStream(Encoding.UTF8.GetBytes(mtlRequest.downloadHandler.text));

					queueData.instantiatedObj = new OBJLoader().Load(textStream, mtlStream);
					queueData.instantiatedObj.name = objName;
				}
				else
				{
					var textStream = new MemoryStream(Encoding.UTF8.GetBytes(www.downloadHandler.text));
					queueData.instantiatedObj = new OBJLoader().Load(textStream);
					queueData.instantiatedObj.name = objName;
				}
			}
		}
	}

	private IEnumerator DownloadTexture(string objName, QueueChecker queueData, int imageID, int assetID = 0)
	{
		// Debug.Log("텍스쳐 이미지 다운로드 시작");
		
		string finalURL = URL_PATH + objName;

		if (isUsingSpecificServer)
		{
			switch (imageID)
			{
				case 0:
					if (!String.IsNullOrEmpty(AssetHierarchies[^1].assets[assetID].textureURL))
						finalURL = AssetHierarchies[^1].assets[assetID].textureURL;
					else
						finalURL += "";
						// finalURL += "_tex0.png";
					break;
				case 1:
					if (!String.IsNullOrEmpty(AssetHierarchies[^1].assets[assetID].normalMapURL))
						finalURL = AssetHierarchies[^1].assets[assetID].normalMapURL;
					else
						finalURL += "";
						// finalURL += "_norm0.png";
					break;
				case 2:
					if (!String.IsNullOrEmpty(AssetHierarchies[^1].assets[assetID].occlusionMapURL))
						finalURL = AssetHierarchies[^1].assets[assetID].occlusionMapURL;
					else
						finalURL += "";
						// finalURL += "_ao0.png";
					break;
				case 3:
					if (!String.IsNullOrEmpty(AssetHierarchies[^1].assets[assetID].metallicMapURL))
						finalURL = AssetHierarchies[^1].assets[assetID].metallicMapURL;
					else
						finalURL += "";
						// finalURL += "_metal0.png";
					break;
				case 4:
					if (!String.IsNullOrEmpty(AssetHierarchies[^1].assets[assetID].smoothnessMapURL))
						finalURL = AssetHierarchies[^1].assets[assetID].smoothnessMapURL;
					else
						finalURL += "";
						// finalURL += "_smooth0.png";
					break;
				case 5:
					if (!String.IsNullOrEmpty(AssetHierarchies[^1].assets[assetID].heightMapURL))
						finalURL = AssetHierarchies[^1].assets[assetID].heightMapURL;
					else
						finalURL += "";
						// finalURL += "_height0.png";
					break;
			}

			// 빈칸 입력한 경우 예외처리
			if (finalURL == URL_PATH + objName)
			{
				queueData.isPassed[imageID] = true;
				yield break;
			}
		}
		else
		{
			switch (imageID)
			{
				case 0:
					finalURL = $"{baseUrl}&id={AssetHierarchies[^1].assets[assetID].textureURL}";
					break;
				case 1:
					finalURL = $"{baseUrl}&id={AssetHierarchies[^1].assets[assetID].normalMapURL}";
					break;
				case 2:
					finalURL = $"{baseUrl}&id={AssetHierarchies[^1].assets[assetID].occlusionMapURL}";
					break;
				case 3:
					finalURL = $"{baseUrl}&id={AssetHierarchies[^1].assets[assetID].metallicMapURL}";
					break;
				case 4:
					finalURL = $"{baseUrl}&id={AssetHierarchies[^1].assets[assetID].smoothnessMapURL}";
					break;
				case 5:
					finalURL = $"{baseUrl}&id={AssetHierarchies[^1].assets[assetID].heightMapURL}";
					break;
			}	
			// 빈칸 입력한 경우 예외처리
			if ($"{baseUrl}&id=" == finalURL)
            {
                queueData.isPassed[imageID] = true;
                yield break;
            }
		}

		if (finalURL.Contains("view"))
			finalURL = $"{baseUrl}&id={stringParser.ExtractBetween(finalURL, "d/", "/view")}";

		using (UnityWebRequest www = UnityWebRequest.Get(finalURL))
		{
			yield return www.Send();

			if (www.isNetworkError || www.isHttpError)
			{
				Debug.Log(www.error);
				queueData.isPassed[imageID] = true;
			}
			else
			{
				Texture2D tex = new Texture2D(2, 2);
				tex.LoadImage(www.downloadHandler.data);
				queueData.textures[imageID] = tex;
			}
		}
	}
}
