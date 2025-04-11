using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class DisplayFileTree : MonoBehaviour
{
    public GameObject folderPrefab; // 폴더 나타낼 버튼 Prefab
    public GameObject filePrefab;
    public Transform contentParent; // Scroll View Content에 연결
    public GameObject backBtn;      // 뒤로가기 버튼
    public Sprite emptyFolder;
    private string apiUrl = "https://json-api-eg.run.goorm.site";
    private Dictionary<string, FolderData> fileStructure = new Dictionary<string, FolderData>();
    private string currentPath = "/"; // 현재 경로
 
    [Header("JSON 로컬 사용 시")]
    public bool isJSONLocatedInLocal = false;
    private string jsonFilePath;    // 로컬 JSON 파일 경로

    [System.Serializable]
    public class FolderData
    {
        public string[] files;
        public string[] folders;
        public string parent;
    }

    [System.Serializable]
    public class ApiResponse
    {
        public string status;
        public Dictionary<string, FolderData> data;
    }

    void Start()
    {
        backBtn.SetActive(false);

        if (isJSONLocatedInLocal)
        {
            jsonFilePath = Path.Combine(Application.streamingAssetsPath, "testJSON.json");
            StartCoroutine(GetFileStructureByLocalJSON());
        }
        else
            StartCoroutine(GetFileStructure());
    }

    IEnumerator GetFileStructureByLocalJSON()
    {
        if (!File.Exists(jsonFilePath))
        {
            Debug.LogError($"JSON 파일을 찾을 수 없습니다: {jsonFilePath}");
            yield break;
        }

        string jsonResponse = File.ReadAllText(jsonFilePath);
        ApiResponse response = JsonConvert.DeserializeObject<ApiResponse>(jsonResponse);

        if (response.status == "success")
        {
            fileStructure = response.data;

            foreach (var entry in fileStructure)
            {
                string folderPath = entry.Key;
                FolderData folderData = entry.Value;

                foreach (var file in folderData.files)
                    Debug.Log($"Folder: {folderPath} file: {file}");
            }

            DisplayFolderContents(currentPath);
        }
        else
        {
            Debug.LogError("JSON 파일 상태가 'success'가 아닙니다.");
        }

        yield return null;
    }

    IEnumerator GetFileStructure()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                ApiResponse response = JsonConvert.DeserializeObject<ApiResponse>(jsonResponse);

                if (response.status == "success")
                {
                    fileStructure = response.data;
                    
                    foreach (var entry in fileStructure)
                    {
                        string folderPath = entry.Key;
                        FolderData folderData = entry.Value;

                        foreach (var file in folderData.files)
                            Debug.Log($"Folder: {folderPath} "+"file : "+file);
                    }
                    
                    DisplayFolderContents(currentPath);
                }
                else
                {
                    Debug.LogError("API Error: " + response.status);
                }
            }
            else
            {
                Debug.LogError("Error: " + request.error);
            }
        }
    }

    void DisplayFolderContents(string path)
    {
        // 기존 버튼 제거
        foreach (Transform child in contentParent)
        {
            if(child.gameObject == backBtn) continue;
            Destroy(child.gameObject);
        }
        
        if (path == "/") backBtn.SetActive(false);
        else backBtn.SetActive(true);

        // 현재 경로 파일 및 폴더 표시
        if (fileStructure.TryGetValue(path, out var currentFolder))
        {
            // 폴더 버튼 생성
            foreach (var folder in currentFolder.folders)
            {
                GameObject folderButton = Instantiate(folderPrefab, contentParent);

                // 빈 폴더 체크
                if (fileStructure.TryGetValue(path+"/"+folder, out var innerFolder))
                {
                    if (innerFolder.files.Length == 0 && innerFolder.folders.Length == 0)
                    {
                        folderButton.GetComponent<Image>().sprite = emptyFolder;
                    }
                }

                folderButton.GetComponentInChildren<Text>().text = folder;
                folderButton.GetComponent<Button>().onClick.AddListener(() =>
                {
                    if (path == "/") OnFolderClick(folder);
                    else OnFolderClick(path + "/" + folder);
                });
            }

            // 파일 버튼 생성
            foreach (var file in currentFolder.files)
            {
                GameObject fileButton = Instantiate(filePrefab, contentParent);
                fileButton.GetComponentInChildren<Text>().text = file;
                fileButton.GetComponent<Button>().onClick.AddListener(() => OnFileClick(path + "/" + file));
            }
        }
        else
        {
            Debug.LogError("Path not found in structure: " + path);
        }
    }

    public void BackBtnClick()
    {
        if (fileStructure.TryGetValue(currentPath, out var currentFolder))
        {
            if (currentFolder.parent == "/workspace")
            {
                OnFolderClick("/");
                return;
            }
            
            var tempSplit = currentFolder.parent.Split("/workspace");

            var parentPathSplit = tempSplit[1].Remove(0, 1);

            var splitIndex = parentPathSplit.LastIndexOf("/", StringComparison.Ordinal);

            if (tempSplit[1].StartsWith("/") && splitIndex > 0)
            {
                OnFolderClick(parentPathSplit.Remove(splitIndex, parentPathSplit.Length - splitIndex));   
            }
            else
            {
                OnFolderClick("/");
            }
        }
    }

    void OnFolderClick(string folderPath)
    {
        currentPath = folderPath;
        DisplayFolderContents(currentPath);
    }

    void OnFileClick(string filePath)
    {
        StartCoroutine(GetFileContent(filePath));
    }

    IEnumerator GetFileContent(string filePath)
    {
        string fileUrl = $"{apiUrl}?file={filePath}";

        using (UnityWebRequest request = UnityWebRequest.Get(fileUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string fileContent = request.downloadHandler.text;
                Debug.Log($"File Content of {fileUrl}:\n{fileContent}");
            }
            else
            {
                Debug.LogError("Error fetching file: " + request.error);
            }
        }
    }
}
