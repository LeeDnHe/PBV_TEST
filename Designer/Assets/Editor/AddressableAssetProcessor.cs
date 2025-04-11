#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using System.Collections.Generic;
using Exoa.Effects;

public class AddressableAssetProcessor : EditorWindow
{
    private Object modelFile;
    private Sprite thumbnailSprite;
    private string fileExtension = "";
    private bool shouldProcessFile = false;
    private string destinationPath = "Assets/Models/AddressablePrefab";
    private string thumbnailsPath = "Assets/Sprites/Thumbnails";
    
    [MenuItem("Tools/Addressable Asset Processor")]
    public static void ShowWindow()
    {
        GetWindow<AddressableAssetProcessor>("Addressable Asset Processor");
    }

    private void OnGUI()
    {
        GUILayout.Label("Addressable Asset Creator", EditorStyles.boldLabel);
        
        modelFile = EditorGUILayout.ObjectField("OBJ/GLTF 파일", modelFile, typeof(GameObject), false);
        
        if (modelFile != null)
        {
            string assetPath = AssetDatabase.GetAssetPath(modelFile);
            fileExtension = Path.GetExtension(assetPath).ToLower();
            shouldProcessFile = fileExtension == ".obj" || fileExtension == ".gltf" || fileExtension == ".glb";
            
            if (!shouldProcessFile)
            {
                EditorGUILayout.HelpBox("OBJ, GLTF 또는 GLB 파일만 지원합니다.", MessageType.Warning);
            }
        }
        
        // 썸네일 스프라이트 필드 추가
        thumbnailSprite = EditorGUILayout.ObjectField("썸네일 스프라이트", thumbnailSprite, typeof(Sprite), false) as Sprite;
        
        EditorGUILayout.Space();
        
        GUI.enabled = modelFile != null && shouldProcessFile;
        if (GUILayout.Button("자동 처리 및 어드레서블 생성"))
        {
            ProcessModelFile();
        }
        GUI.enabled = true;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space();
        
        if (GUILayout.Button("번들 빌드"))
        {
            BuildAddressableBundle();
        }
        
        if (GUILayout.Button("WebGL 폴더에 복사"))
        {
            CopyToWebGLFolder();
        }
        
        // 스프라이트 미리보기 추가
        if (thumbnailSprite != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("썸네일 미리보기:");
            Rect previewRect = GUILayoutUtility.GetRect(100, 100);
            EditorGUI.DrawPreviewTexture(previewRect, thumbnailSprite.texture);
        }
    }

    private void ProcessModelFile()
    {
        string assetPath = AssetDatabase.GetAssetPath(modelFile);
        
        // 1. 프리팹을 Unpack Completely
        GameObject instance = PrefabUtility.InstantiatePrefab(modelFile as GameObject) as GameObject;
        if (instance == null)
        {
            Debug.LogError("프리팹을 인스턴스화할 수 없습니다.");
            return;
        }
        
        // 2. 메시 합치기
        MergeMeshes(instance);
        
        // 3. Transform을 (0,0,0)으로 설정
        instance.transform.position = Vector3.zero;
        
        // 4. Box Collider 추가
        BoxCollider boxCollider = instance.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = instance.AddComponent<BoxCollider>();
        }
        
        // 5. Outlinable 스크립트 추가
        Outlinable outlinable = instance.GetComponent<Outlinable>();
        if (outlinable == null)
        {
            outlinable = instance.AddComponent<Outlinable>();
        }
        
        // 6. Asset Thumbnail 추가
        AssetThumbnail thumbnail = instance.GetComponent<AssetThumbnail>();
        if (thumbnail == null)
        {
            thumbnail = instance.AddComponent<AssetThumbnail>();
        }
        
        // 썸네일 스프라이트 설정
        if (thumbnailSprite != null)
        {
            thumbnail.thumbnail = thumbnailSprite;
        }
        else
        {
            // 썸네일이 없을 경우 자동으로 생성
            AutoGenerateThumbnail(instance, thumbnail);
        }
        
        // 파일 확장자에 따라 태그 지정
        if (fileExtension == ".gltf" || fileExtension == ".glb")
        {
            instance.tag = "glTF";
        }
        
        // 7. 프리팹 생성 및 저장 - AddressablePrefab 폴더에 저장
        if (!Directory.Exists(destinationPath))
        {
            // 중간 디렉토리가 없을 경우 생성
            if (!Directory.Exists("Assets/Models"))
            {
                AssetDatabase.CreateFolder("Assets", "Models");
            }
            AssetDatabase.CreateFolder("Assets/Models", "AddressablePrefab");
            AssetDatabase.Refresh();
        }
        
        string newPrefabName = Path.GetFileNameWithoutExtension(assetPath);
        string prefabPath = $"{destinationPath}/{newPrefabName}.prefab";
        
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        DestroyImmediate(instance);
        
        if (prefab != null)
        {
            // 8 & 9. 어드레서블로 설정하고 그룹 지정
            MakeAddressable(prefab, fileExtension);
            
            // 썸네일 스프라이트도 어드레서블로 설정 (썸네일이 있는 경우)
            if (thumbnailSprite != null)
            {
                MakeThumbnailAddressable(thumbnailSprite, fileExtension);
            }
            
            EditorUtility.DisplayDialog("완료", $"{newPrefabName} 파일이 어드레서블 에셋으로 성공적으로 생성되었습니다.", "확인");
        }
    }

    private void MergeMeshes(GameObject obj)
    {
        // MeshMergerEditor 컴포넌트 추가
        MeshMergerEditor meshMerger = obj.GetComponent<MeshMergerEditor>();
        if (meshMerger == null)
        {
            meshMerger = obj.AddComponent<MeshMergerEditor>();
        }
        
        // 메소드 호출
        System.Type type = meshMerger.GetType();
        System.Reflection.MethodInfo method = type.GetMethod("MergeAndSaveMesh", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (method != null)
        {
            method.Invoke(meshMerger, null);
        }
        else
        {
            Debug.LogError("MergeAndSaveMesh 메소드를 찾을 수 없습니다.");
        }
    }

    private void AutoGenerateThumbnail(GameObject obj, AssetThumbnail thumbnailComponent)
    {
        // Thumbnails 폴더 생성
        if (!Directory.Exists(thumbnailsPath))
        {
            if (!Directory.Exists("Assets/Sprites"))
            {
                AssetDatabase.CreateFolder("Assets", "Sprites");
            }
            AssetDatabase.CreateFolder("Assets/Sprites", "Thumbnails");
            AssetDatabase.Refresh();
        }

        // 카메라 생성하여 오브젝트 스크린샷 찍기
        Camera tempCamera = new GameObject("TempCamera").AddComponent<Camera>();
        tempCamera.clearFlags = CameraClearFlags.SolidColor;
        tempCamera.backgroundColor = new Color(0.8f, 0.8f, 0.8f, 1f); // 밝은 회색 배경
        tempCamera.orthographic = true;
        
        // 오브젝트 바운딩 박스 계산
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogWarning("렌더러를 찾을 수 없어 썸네일을 생성할 수 없습니다.");
            DestroyImmediate(tempCamera.gameObject);
            return;
        }
        
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }
        
        // 카메라 위치 및 설정 조정
        tempCamera.transform.position = bounds.center + new Vector3(0, 0, -10);
        tempCamera.orthographicSize = Mathf.Max(bounds.size.x, bounds.size.y) * 0.6f;
        
        // 렌더링을 위한 RenderTexture 생성
        RenderTexture rt = new RenderTexture(256, 256, 24);
        tempCamera.targetTexture = rt;
        tempCamera.Render();
        
        // RenderTexture에서 Texture2D로 변환
        RenderTexture.active = rt;
        Texture2D texture = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        texture.Apply();
        
        // 임시 카메라 정리
        tempCamera.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(tempCamera.gameObject);
        
        // 텍스처를 PNG로 저장
        byte[] bytes = texture.EncodeToPNG();
        string fileName = obj.name + "_Thumbnail.png";
        string filePath = Path.Combine(thumbnailsPath, fileName);
        File.WriteAllBytes(filePath, bytes);
        AssetDatabase.ImportAsset(filePath);
        
        // 저장된 텍스처의 Sprite 설정 변경
        TextureImporter importer = AssetImporter.GetAtPath(filePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.SaveAndReimport();
            
            // 스프라이트 로드하여 썸네일 컴포넌트에 할당
            Sprite generatedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(filePath);
            thumbnailComponent.thumbnail = generatedSprite;
            thumbnailSprite = generatedSprite; // 전역 변수에도 할당하여 어드레서블 등록에 사용
        }
    }

    private void MakeAddressable(GameObject prefab, string extension)
    {
        // Addressables 설정 가져오기
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        
        if (settings == null)
        {
            Debug.LogError("Addressable Asset Settings를 찾을 수 없습니다. 먼저 Addressables 창에서 설정을 초기화해주세요.");
            return;
        }
        
        // 확장자에 따라 그룹 이름 지정
        string groupName = extension == ".obj" ? "obj" : "gltf";
        
        // 지정된 그룹 찾기 또는 생성
        AddressableAssetGroup group = settings.FindGroup(groupName);
        if (group == null)
        {
            group = settings.CreateGroup(groupName, false, false, true, 
                new List<AddressableAssetGroupSchema>(), 
                typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
        }
        
        // 프리팹의 에셋 경로 가져오기
        string assetPath = AssetDatabase.GetAssetPath(prefab);
        
        // GUID 가져오기
        string guid = AssetDatabase.AssetPathToGUID(assetPath);
        
        // 어드레서블 에셋 엔트리 만들기
        AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);
        
        // 주소 설정
        entry.address = prefab.name;
        
        // 라벨 추가
        entry.labels.Add(groupName);
        
        // 변경사항 저장
        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
    }

    private void MakeThumbnailAddressable(Sprite sprite, string extension)
    {
        if (sprite == null) return;
        
        // Addressables 설정 가져오기
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        
        if (settings == null) return;
        
        // 썸네일용 그룹 이름
        string groupName = "Thumbnails";
        
        // 지정된 그룹 찾기 또는 생성
        AddressableAssetGroup group = settings.FindGroup(groupName);
        if (group == null)
        {
            group = settings.CreateGroup(groupName, false, false, true, 
                new List<AddressableAssetGroupSchema>(), 
                typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
        }
        
        // 스프라이트의 에셋 경로 가져오기
        string assetPath = AssetDatabase.GetAssetPath(sprite);
        
        // GUID 가져오기
        string guid = AssetDatabase.AssetPathToGUID(assetPath);
        
        // 어드레서블 에셋 엔트리 만들기
        AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);
        
        // 주소 설정 (프리팹 이름_Thumbnail)
        entry.address = Path.GetFileNameWithoutExtension(assetPath);
        
        // 라벨 추가
        entry.labels.Add("thumbnail");
        
        // 변경사항 저장
        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
    }

    private void BuildAddressableBundle()
    {
        AddressableAssetSettings.BuildPlayerContent();
        EditorUtility.DisplayDialog("번들 빌드 완료", "어드레서블 번들 빌드가 완료되었습니다.", "확인");
    }

    private void CopyToWebGLFolder()
    {
        string sourceDir = "Library/com.unity.addressables/aa/WebGL/WebGL";
        string destDir = "MyServerData/WebGL";
        
        // 목적지 디렉토리 확인 및 생성
        if (!Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
        }
        
        // 파일 복사 로직
        try
        {
            foreach (string dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourceDir, destDir));
            }

            foreach (string filePath in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(filePath, filePath.Replace(sourceDir, destDir), true);
            }
            
            EditorUtility.DisplayDialog("복사 완료", "WebGL 폴더로 파일 복사가 완료되었습니다.", "확인");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"파일 복사 중 오류 발생: {ex.Message}");
            EditorUtility.DisplayDialog("오류", $"파일 복사 중 오류가 발생했습니다: {ex.Message}", "확인");
        }
    }
}
#endif