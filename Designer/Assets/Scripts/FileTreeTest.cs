using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.UI;

[System.Serializable]
public class FileNode
{
    public string name; // 파일/폴더 이름
    public string type; // "file" 또는 "folder"
    public string url;  // 파일 경로 (파일일 경우만)
    public List<FileNode> contents; // 하위 폴더/파일 리스트
}

public class FileTreeTest : MonoBehaviour
{
    public Transform fileListParent;
    public GameObject folderPrefab;
    public GameObject filePrefab;

    private string apiUrl = "https://json-api-eg.run.goorm.site/";

    void Start()
    {
        StartCoroutine(FetchFileStructure());
    }

    IEnumerator FetchFileStructure()
    {
        UnityWebRequest request = UnityWebRequest.Get(apiUrl);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string jsonResponse = request.downloadHandler.text;
            FileExplorer.ApiResponse response = JsonConvert.DeserializeObject<FileExplorer.ApiResponse>(jsonResponse);

            if (response.status == "success")
            {
                DisplayFileTree(response.data);
            }
            else
            {
                Debug.LogError("API Error: " + response.status);
            }
        }
        else
        {
            Debug.LogError($"Failed to fetch file structure: {request.error}");
        }
    }
    
    void DisplayFileTree(Dictionary<string, FileExplorer.FolderData> fileStructure)
    {
        foreach (var entry in fileStructure)
        {
            string folderPath = entry.Key;
            FileExplorer.FolderData folderData = entry.Value;

            Debug.Log($"Folder: {folderPath}");
            if (folderData.folders.Length > 0)
            {
                for (int i = 0; i < folderData.folders.Length; i++)
                {
                    GameObject folderButton = Instantiate(folderPrefab, fileListParent);
                    folderButton.GetComponentInChildren<Text>().text = folderData.folders[i];

                    Transform folderContents = folderButton.transform.Find("Contents");
                    folderContents.gameObject.SetActive(false);

                    folderButton.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        folderContents.gameObject.SetActive(!folderContents.gameObject.activeSelf);
                    });   
                }

                // foreach (var child in node.contents)
                // {
                //     DisplayFileTree(child, folderContents);
                // }
            }
            if (folderData.files.Length > 0)
            {
                for (int i = 0; i < folderData.files.Length; i++)
                {
                    GameObject fileButton = Instantiate(filePrefab, fileListParent);
                    fileButton.GetComponentInChildren<Text>().text = folderData.files[i];

                    fileButton.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        // StartCoroutine(LoadFile(folderData.url));
                    });   
                }
            }
        }
    }

    IEnumerator LoadFile(string fileUrl)
    {
        UnityWebRequest request = UnityWebRequest.Get(fileUrl);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"Loaded file: {fileUrl}");
            // obj/gltf 파일 로드 로직 추가
        }
        else
        {
            Debug.LogError($"Failed to load file: {request.error}");
        }
    }
}
