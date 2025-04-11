using Exoa.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Exoa.Designer
{
    public class SaveSystem
    {
        public enum Mode { FILE_SYSTEM, RESOURCES, ONLINE };
        public Mode mode;

        public static string defaultSubFolderName = null;
        public static string defaultFileToOpen = null;
        private string resourcesFolderLocation = "/Resources/";

        public string ResourcesFolderLocation { get => resourcesFolderLocation; set => resourcesFolderLocation = value; }

        public SaveSystem(Mode mode)
        {
            this.mode = mode;

#if ONLINE_MODULE
            if (mode == Mode.FILE_SYSTEM && OnlineModuleSettings.GetSettings().useOnlineMode)
            {
                this.mode = Mode.ONLINE;
            }
#endif
        }



        [Serializable]
        public struct FileList
        {
            public List<string> list;
        }

        public static SaveSystem Create(Mode mode)
        {
            return new SaveSystem(mode);
        }

        public string GetBasePath(string subFolder)
        {
            string path = "";
            if (mode == Mode.RESOURCES)
                path = Application.dataPath;
            else
                path = Application.persistentDataPath + "/";

            if (!string.IsNullOrEmpty(subFolder))
                path += subFolder + "/";

            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
            }
            catch (Exception e)
            {
                Debug.LogError("Could not create folder:" + e.Message);
            }
            return path;
        }
        public void RefreshUnityDB()
        {
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
        /*
        public T LoadFileItem<T>(string fileName, string subFolderName, Action<T> pCallback = null, string ext = ".json")
        {
            HDLogger.Log("LoadFileItem " + subFolderName + "/" + fileName, HDLogger.LogCategory.FileSystem);

            string json = LoadFileItem(fileName, subFolderName, null, ext);
            T p = JsonUtility.FromJson<T>(json);
            pCallback?.Invoke(p);
            return p;
        }*/

        public void LoadFileItem(string fileName, string subFolderName, Action<string> pCallback = null, string ext = ".json")
        {
            HDLogger.Log("LoadFileItem " + GetBasePath(subFolderName) + "/" + fileName, HDLogger.LogCategory.FileSystem);

            string content = null;

            try
            {
                if (mode == Mode.RESOURCES)
                {
                    TextAsset o = Resources.Load<TextAsset>(subFolderName + "/" + fileName);
                    content = o != null ? o.text : null;

                    if (!string.IsNullOrEmpty(content))
                        pCallback?.Invoke(content);

                }
                else if (mode == Mode.FILE_SYSTEM)
                {
                    StreamReader stream = File.OpenText(GetBasePath(subFolderName) + fileName + ext);
                    content = stream.ReadToEnd();
                    stream.Close();

                    if (!string.IsNullOrEmpty(content))
                        pCallback?.Invoke(content);

                }
                else if (mode == Mode.ONLINE)
                {
#if ONLINE_MODULE
                    OnlineHelper.Instance.Load(fileName, subFolderName, ext,
                        (UnityWebRequest req, string res) =>
                        {
                            if (!string.IsNullOrEmpty(res))
                                pCallback?.Invoke(res);
                        },
                        OnlineHelper.Instance.DefaultFailCallback);
#else
                    Debug.LogError("Online Module missing!");
#endif
                }
            }
            catch (System.Exception e)
            {
                HDLogger.LogError("Error loading " + subFolderName + "/" + fileName + " " + e.Message, HDLogger.LogCategory.FileSystem);
                AlertPopup.ShowAlert("error", "Error", "Error loading " + subFolderName + "/" + fileName + " " + e.Message);
            }


        }

        public void Exists(string fileName, string subFolderName, string ext = ".png", Action<bool> pCallback = null)
        {
            bool exists = false;
            string path = null;
            if (mode == Mode.RESOURCES)
            {
                path = subFolderName + "/" + fileName;
                UnityEngine.Object o = Resources.Load(path);
                exists = o != null;
                HDLogger.Log("Exists " + path + " : " + exists, HDLogger.LogCategory.FileSystem);

                pCallback?.Invoke(exists);
            }
            else if (mode == Mode.FILE_SYSTEM)
            {
                path = GetBasePath(subFolderName) + fileName + ext;
                exists = File.Exists(path);
                HDLogger.Log("Exists " + path + " : " + exists, HDLogger.LogCategory.FileSystem);

                pCallback?.Invoke(exists);
            }
            else if (mode == Mode.ONLINE)
            {
#if ONLINE_MODULE
                OnlineHelper.Instance.Exists(fileName, subFolderName, ext,
                    (UnityWebRequest req, string res) =>
                    {
                        HDLogger.Log("Exists " + path + " : " + exists, HDLogger.LogCategory.FileSystem);

                        pCallback?.Invoke(res == "true");
                    },
                    OnlineHelper.Instance.DefaultFailCallback);
#else
                    Debug.LogError("Online Module missing!");
#endif
            }
        }

        public void LoadTextureItem(string fileName, string subFolderName,
            Action<Texture2D> callback, Action<UnityWebRequest, string> errorCallback,
            string ext = ".png",
            int width = 100, int height = 100)
        {
            HDLogger.Log("LoadTextureItem " + fileName, HDLogger.LogCategory.FileSystem);
            Texture2D tex = null;

            if (mode == Mode.RESOURCES)
            {
                tex = Resources.Load<Texture2D>(subFolderName + "/" + fileName);
                if (tex != null)
                    callback?.Invoke(tex);
                else
                    errorCallback?.Invoke(null, "File not found in Resources:" + fileName + ext);
            }
            else if (mode == Mode.ONLINE)
            {
#if ONLINE_MODULE
                OnlineHelper.Instance.Load(fileName, subFolderName, ext,
                    (UnityWebRequest req, string res) =>
                    {
                        //Debug.Log("Loading Texture " + subFolderName + "/" + fileName + ext);
                        //Debug.Log("req.downloadHandler.text" + req.downloadHandler.text);
                        byte[] bytes = req.downloadHandler.data;
                        if (bytes == null || bytes.Length < 200)
                        {
                            errorCallback?.Invoke(null, "File not found online:" + fileName + ext);
                            return;
                        }
                        //HDLogger.Log("bytes:" + bytes.Length, HDLogger.LogCategory.Online);
                        Texture2D t = new Texture2D(100, 100, TextureFormat.RGB24, false);
                        t.LoadImage(bytes);

                        callback?.Invoke(t);
                    },
                    errorCallback);
#else
                    Debug.LogError("Online Module missing!");
#endif
            }
            else if (mode == Mode.FILE_SYSTEM)
            {
                string path = GetBasePath(subFolderName) + fileName + ext;

                byte[] fileData;

                if (File.Exists(path))
                {
                    fileData = File.ReadAllBytes(path);
                    tex = new Texture2D(width, height);
                    tex.LoadImage(fileData);

                    callback?.Invoke(tex);
                }
                else
                {
                    errorCallback?.Invoke(null, "File not found in system files:" + fileName + ext);
                }
            }

        }

        public void SaveFileItem(string fileName, string subFolderName, string json, Action<string> pCallback = null)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(json);
            SaveFileItem(fileName, subFolderName, ".json", bytes, pCallback);
        }

        public void SaveFileItem(string fileName, string subFolderName, string ext, byte[] bytes, Action<string> pCallback = null)
        {
            if (bytes == null)
            {
                HDLogger.LogWarning("Warning saving " + subFolderName + "/" + fileName + ext + " nothing to save (empty content)", HDLogger.LogCategory.FileSystem);

                return;
            }
            HDLogger.Log("SaveFileItem " + GetBasePath(subFolderName) + fileName + ext, HDLogger.LogCategory.FileSystem);

            if (mode == Mode.FILE_SYSTEM)
            {
                bool success = false;
                try
                {
                    File.WriteAllBytes(GetBasePath(subFolderName) + fileName + ext, bytes);

                    if (mode == Mode.RESOURCES)
                    {
                        RefreshUnityDB();
                    }
                    success = true;
                }
                catch (System.Exception e)
                {
                    HDLogger.LogError("Error saving " + subFolderName + "/" + fileName + ext + " " + e.Message, HDLogger.LogCategory.FileSystem);
                    AlertPopup.ShowAlert("error", "Error", "Error loading " + subFolderName + "/" + fileName + ext + " " + e.Message);
                }

                if (success)
                    pCallback?.Invoke(fileName);
            }
            else if (mode == Mode.ONLINE)
            {
#if ONLINE_MODULE
                OnlineHelper.Instance.Save(fileName,
                    subFolderName, ext, bytes,
                    (UnityWebRequest req, string res) =>
                    {
                        HDLogger.Log("Saved " + fileName + " : " + res, HDLogger.LogCategory.FileSystem);

                        pCallback?.Invoke(fileName);
                    },
                    OnlineHelper.Instance.DefaultFailCallback);
#else
                    Debug.LogError("Online Module missing!");
#endif
            }
        }

        public void DeleteFileItem(string fileName, string subFolderName, Action pCallback = null, string ext = ".json")
        {
            HDLogger.Log("DeleteFileItem " + fileName, HDLogger.LogCategory.FileSystem);

            if (mode == Mode.FILE_SYSTEM)
            {
                try
                {
                    FileInfo fi = new FileInfo(GetBasePath(subFolderName) + fileName + ext);
                    fi.Delete();

                    pCallback?.Invoke();
                }
                catch (System.Exception e)
                {
                    HDLogger.LogError("Error deleting " + subFolderName + "/" + fileName + " " + e.Message, HDLogger.LogCategory.FileSystem);
                    AlertPopup.ShowAlert("error", "Error", e.Message);
                }
                RefreshUnityDB();
            }
            else if (mode == Mode.ONLINE)
            {
#if ONLINE_MODULE
                OnlineHelper.Instance.Delete(fileName, subFolderName, ext,
                    (UnityWebRequest req, string res) =>
                    {
                        HDLogger.Log("Deleted " + fileName + " : " + res, HDLogger.LogCategory.FileSystem);

                        pCallback?.Invoke();
                    },
                    OnlineHelper.Instance.DefaultFailCallback);
#else
                    Debug.LogError("Online Module missing!");
#endif
            }
        }

        public void RenameFileItem(string fileName, string newName, string subFolderName, Action pCallback = null, string ext = ".json")
        {
            HDLogger.Log("RenameFileItem " + fileName + " " + newName, HDLogger.LogCategory.FileSystem);

            if (mode == Mode.FILE_SYSTEM)
            {
                FileInfo fi = null;
                try
                {
                    fi = new FileInfo(GetBasePath(subFolderName) + fileName + ext);
                    fi.MoveTo(GetBasePath(subFolderName) + newName + ext);

                    pCallback?.Invoke();
                }
                catch (System.Exception e)
                {
                    AlertPopup.ShowAlert("error", "Error", e.Message);
                    HDLogger.LogError("Error renaming " + subFolderName + "/" + fileName + " " + e.Message, HDLogger.LogCategory.FileSystem);
                }
                RefreshUnityDB();
            }
            else if (mode == Mode.ONLINE)
            {
#if ONLINE_MODULE
                OnlineHelper.Instance.Rename(fileName, newName, subFolderName, ext,
                    (UnityWebRequest req, string res) =>
                    {
                        HDLogger.Log("Renamed " + fileName + " : " + res, HDLogger.LogCategory.FileSystem);

                        pCallback?.Invoke();
                    },
                    OnlineHelper.Instance.DefaultFailCallback);
#else
                    Debug.LogError("Online Module missing!");
#endif
            }
        }

        public void CopyFileItem(string fileName, string newName, string subFolderName, Action pCallback = null, string ext = ".json")
        {
            HDLogger.Log("CopyFileItem " + fileName + " " + newName, HDLogger.LogCategory.FileSystem);

            if (mode == Mode.FILE_SYSTEM)
            {
                FileInfo fi = null;
                try
                {
                    fi = new FileInfo(GetBasePath(subFolderName) + fileName + ext);
                    fi.CopyTo(GetBasePath(subFolderName) + newName + ext);

                    pCallback?.Invoke();
                }
                catch (System.Exception e)
                {
                    AlertPopup.ShowAlert("error", "Error", e.Message);
                    HDLogger.LogError("Error copying " + subFolderName + "/" + fileName + " " + e.Message, HDLogger.LogCategory.FileSystem);
                }
                RefreshUnityDB();
            }
            else if (mode == Mode.ONLINE)
            {
#if ONLINE_MODULE
                OnlineHelper.Instance.Copy(fileName, newName, subFolderName, ext,
                    (UnityWebRequest req, string res) =>
                    {
                        HDLogger.Log("Copied " + fileName + " : " + res, HDLogger.LogCategory.FileSystem);

                        pCallback?.Invoke();
                    },
                    OnlineHelper.Instance.DefaultFailCallback);
#else
                    Debug.LogError("Online Module missing!");
#endif
            }
        }


        public void ListFileItems(string subFolderName, Action<FileList> pCallback = null, string ext = "*.json")
        {
            FileList ll = new FileList();
            ll.list = new List<string>();

            if (mode == Mode.RESOURCES)
            {
                UnityEngine.Object[] files = Resources.LoadAll(subFolderName + "/");
                foreach (UnityEngine.Object o in files)
                {
                    ll.list.Add(o.name);
                }
                HDLogger.Log("ListFileItems " + GetBasePath(subFolderName) + ":" + ll.list.Count, HDLogger.LogCategory.FileSystem);

                pCallback?.Invoke(ll);
            }
            else if (mode == Mode.FILE_SYSTEM)
            {
                DirectoryInfo dir = new DirectoryInfo(GetBasePath(subFolderName));
                FileInfo[] info = dir.GetFiles(ext);
                foreach (FileInfo f in info)
                {
                    ll.list.Add(f.Name);
                }
                HDLogger.Log("ListFileItems " + GetBasePath(subFolderName) + ":" + ll.list.Count, HDLogger.LogCategory.FileSystem);

                pCallback?.Invoke(ll);
            }
            else if (mode == Mode.ONLINE)
            {
#if ONLINE_MODULE
                OnlineHelper.Instance.ListFiles(subFolderName,
                   (UnityWebRequest req, string res) =>
                   {
                       HDLogger.Log("res:" + res, HDLogger.LogCategory.Online);
                       FileList fileList = JsonConvert.DeserializeObject<FileList>(res);
                       if (fileList.list != null)
                       {
                           pCallback?.Invoke(fileList);
                       }
                   },
                   OnlineHelper.Instance.DefaultFailCallback);
#else
                    Debug.LogError("Online Module missing!");
#endif
            }

        }

    }
}
