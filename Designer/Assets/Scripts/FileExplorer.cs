using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class FileExplorer : MonoBehaviour
{
    private const string apiUrl = "https://json-api-eg.run.goorm.site/";

    [System.Serializable]
    public class FolderData
    {
        public string[] folders;
        public string[] files;
    }

    [System.Serializable]
    public class ApiResponse
    {
        public string status;
        public Dictionary<string, FolderData> data;
    }

    void Start()
    {
        StartCoroutine(GetFileStructure());
    }

    IEnumerator GetFileStructure()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // JSON 파싱
                string jsonResponse = request.downloadHandler.text;
                ApiResponse response = JsonConvert.DeserializeObject<ApiResponse>(jsonResponse);

                if (response.status == "success")
                {
                    DisplayFileStructure(response.data);
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

    void DisplayFileStructure(Dictionary<string, FolderData> fileStructure)
    {
        foreach (var entry in fileStructure)
        {
            string folderPath = entry.Key;
            FolderData folderData = entry.Value;

            Debug.Log($"Folder: {folderPath}");
            // if (folderData.folders.Length > 0)
            // {
            //     Debug.Log("  Subfolders: " + string.Join(", ", folderData.folders));
            // }
            // if (folderData.files.Length > 0)
            // {
            //     Debug.Log("  Files: " + string.Join(", ", folderData.files));
            // }
        }
    }
}
