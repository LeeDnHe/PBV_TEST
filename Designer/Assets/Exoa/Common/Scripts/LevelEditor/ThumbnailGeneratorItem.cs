using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Exoa.Designer.Utils
{
    public class ThumbnailGeneratorUtils
    {
       
        /// <summary>
        /// Checks if a file exists in the specified directories.
        /// </summary>
        /// <param name="fileName">The name of the file to check.</param>
        /// <param name="callback">Callback function to handle the result of the existence check.</param>
        /// <remarks>
        /// This method first checks if the file exists in the external file system directory.
        /// If the file does not exist in the external directory, it then checks in the embedded resources directory.
        /// The result of the existence check is passed to the callback function.
        /// </remarks>
        public static void Exists(string fileName, Action<bool> callback)
        {
            HDLogger.Log("Thumb Exists:" + fileName, HDLogger.LogCategory.Screenshot);
            SaveSystem.Create(SaveSystem.Mode.FILE_SYSTEM)
                .Exists(fileName, HDSettings.EXT_THUMBNAIL_FOLDER, ".png", (bool exists) =>
            {
                if (!exists)
                {
                    SaveSystem.Create(SaveSystem.Mode.RESOURCES).Exists(fileName, HDSettings.EMBEDDED_THUMBNAIL_FOLDER, ".png", (bool exists2) =>
                    {
                        callback?.Invoke(exists2);
                    });
                }
                else
                {
                    callback?.Invoke(exists);
                }
            });
        }

        /// <summary>
        /// Loads a texture from a file and invokes the callback with the loaded texture.
        /// If the texture is not found, a backup texture is loaded instead.
        /// </summary>
        /// <param name="fileName">The name of the file to load the texture from.</param>
        /// <param name="callback">The callback to invoke with the loaded texture.</param>
        /// <param name="errorCallback">The callback to invoke in case of an error.</param>
        public static void Load(string fileName, Action<Texture2D> callback, Action<UnityWebRequest, string> errorCallback)
        {
            HDLogger.Log("Thumb Load:" + fileName, HDLogger.LogCategory.Screenshot);
            SaveSystem.Create(SaveSystem.Mode.FILE_SYSTEM)
                .LoadTextureItem(fileName, HDSettings.EXT_THUMBNAIL_FOLDER, (Texture2D t) =>
                {
                    if (t == null)
                    {
                        LoadBackupTexture(fileName, callback, errorCallback);
                    }
                    else
                    {
                        callback?.Invoke(t);
                    }
                }, (UnityWebRequest req, string res) =>
                {
                    LoadBackupTexture(fileName, callback, errorCallback);
                }, ".png");

        }

        /// <summary>
        /// Loads a backup texture from the specified file name.
        /// </summary>
        /// <param name="fileName">The name of the file to load the texture from.</param>
        /// <param name="callback">The callback action to be invoked upon successful loading of the texture.</param>
        /// <param name="errorCallback">The callback action to be invoked if an error occurs during loading.</param>
        /// <remarks>
        /// This method first attempts to load the texture from the specified file name. If the texture is not found and the file name is not "EmptyThumb",
        /// it recursively attempts to load the "EmptyThumb" texture as a fallback.
        /// </remarks>
        private static void LoadBackupTexture(string fileName, Action<Texture2D> callback, Action<UnityWebRequest, string> errorCallback)
        {
            HDLogger.Log("LoadBackupTexture in Resources:" + fileName, HDLogger.LogCategory.FileSystem);

            SaveSystem.Create(SaveSystem.Mode.RESOURCES)
                .LoadTextureItem(fileName, HDSettings.EMBEDDED_THUMBNAIL_FOLDER, (Texture2D t2) =>
                {
                    callback?.Invoke(t2);
                }, (UnityWebRequest req, string res) =>
                {
                    if (fileName != "EmptyThumb")
                        LoadBackupTexture("EmptyThumb", callback, errorCallback);
                },
                ".png");
        }



        /// <summary>
        /// Takes a screenshot of a 3D model and saves it as a PNG file.
        /// </summary>
        /// <param name="target">The transform of the 3D model to capture.</param>
        /// <param name="fileName">The name of the file to save the screenshot as.</param>
        /// <param name="orthographic">Flag indicating if the screenshot should be taken in orthographic mode.</param>
        /// <param name="direction">The direction of the preview camera.</param>
        /// <remarks>
        /// This method sets up the background color, readability of the texture, orthographic mode, and preview direction before generating the model preview.
        /// It then encodes the texture to PNG format and saves it to
        public static void TakeAndSaveScreenshot(Transform target, string fileName, bool orthographic, Vector3 direction)
        {
            RuntimePreviewGenerator.BackgroundColor = HDSettings.THUMBNAIL_BACKGROUND;
            RuntimePreviewGenerator.MarkTextureNonReadable = false;
            RuntimePreviewGenerator.OrthographicMode = orthographic;
            RuntimePreviewGenerator.PreviewDirection = direction;

            Texture2D tex = RuntimePreviewGenerator.GenerateModelPreview(target, 256, 256);

            try
            {
                byte[] _bytes = tex.EncodeToPNG();

                //Debug.Log("Saving Thumbnail path:" + filaName);

                SaveSystem.Create(SaveSystem.Mode.FILE_SYSTEM)
                    .SaveFileItem(fileName.Replace(".png", ""),
                    HDSettings.EXT_THUMBNAIL_FOLDER, ".png", _bytes);
            }
            catch (Exception e) { Debug.LogError(e.Message); }

#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif

        }

        /// <summary>Duplicates a file from one location to another.</summary>
        /// <param name="v1">The source file path.</param>
        /// <param name="v2">The destination file path.</param>
        /// <exception cref="Exception">Thrown when an error occurs during the duplication process.</exception>
        public static void Duplicate(string v1, string v2)
        {
            try
            {
                SaveSystem.Create(SaveSystem.Mode.FILE_SYSTEM).CopyFileItem(v1, v2, HDSettings.EXT_THUMBNAIL_FOLDER, null, ".png");
            }
            catch (Exception e) { Debug.LogError(e.Message); }

        }
    }
}
