using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

/// <summary>
/// when Android will auto process the Texture To ETC + alpha format.
/// but when into iOS or other platform will auto use pvr or RGBA32(like pc\osx)
/// 1:find all material need use ETC texture
/// 2:ETC convert texture,and change shader of this material.
/// 
/// by chiuanwei 2014-12-03
/// </summary>
/// 

[InitializeOnLoad]
public class ETCTexturePostprocessor
{
    public class targetFolder
    {
        public string path;
        public string etcShader;
        public string iosShader;
        public int alphaMaxSize;

        public targetFolder(string p, string ES, string IS, int ams)
        {
            this.path = p;
            this.etcShader = ES;
            this.iosShader = IS;
            this.alphaMaxSize = ams;
        }
    }

    /// ==========================================
    /// ADD TARGET FOLDER TO AUTO USE ETC + ALPHA
    /// ==========================================
    private static targetFolder[] m_needProcessPaths = new targetFolder[]{
		//new targetFolder("/ETC+Alpha/AutoETC/", "Unlit/Transparent Colored ETC1", "Unlit/Transparent Colored"),
        //new targetFolder("/Image/monsters/", "tk2d/BlendVertexColor (ETC+Alpha using R channel)", "tk2d/BlendVertexColor"),
	};

    public class EtcAlphaInfo
    {
        public Texture2D SourceTexture;
        public Texture2D AlphaTexture;
        public string SourcePath;
        public string AlphaPath;
        public TextureImporter SoureTextureImporter;
        public TextureImporter AlphaTextureImporter;
        public int AlphaFormat; //0:r,1:g,2:b
        public Material mat;
        public Shader etcShader; //use for etc texture
        public Shader iosShader; //use for ios texture format & other platforms.
        public int alphaMaxSize = 1024;
    }

    static ETCTexturePostprocessor()
    {
        EditorUserBuildSettings.activeBuildTargetChanged += OnChangePlatform;
    }

    [MenuItem("TextureSetting/ETC/Change All Setting Folder (Note:change setting in ETCTexturePostprocessor.cs)")]
    static void OnChangePlatform()
    {
        ChangeETC(m_needProcessPaths);
    }

    [MenuItem("TextureSetting/ETC/Change Selected Folder")]
    static void OnChangeSelectedFolder()
    {
        if (Selection.activeObject == null)
        {
            Debug.LogError("have select some folder.");
            return;
        }

        string selFolder = AssetDatabase.GetAssetPath(Selection.activeObject);
        //Debug.Log("ETC Folder:" + selFolder + "\n"+Selection.activeObject.GetType());
        bool isFolder = Selection.activeObject.GetType() == typeof(UnityEngine.Object);
        selFolder = selFolder.Substring(6, isFolder ? selFolder.Length - 6 : selFolder.LastIndexOf('/') - 5);
        if (isFolder) selFolder += "/";
        Debug.Log("ETC Folder:" + selFolder);

        //ChangeETC(new targetFolder[]{new targetFolder(selFolder,"Unlit/Transparent Colored ETC1", "Unlit/Transparent Colored")});

        EditorSelectedFolderWindow.Create(selFolder);
    }

    /// <summary>
    /// do etc changed for the folder's materials
    /// </summary>
    /// <param name="needProcessPaths">Need process paths.</param>
    /// <param name="dontChanged">If set to <c>true</c> dont changed.</param>
    public static void ChangeETC(targetFolder[] needProcessPaths, bool dontChanged = false)
    {
        Debug.Log("ETC Auto At Platform : " + EditorUserBuildSettings.activeBuildTarget);
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android && EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
        {
            Debug.LogWarning("Not the iOS or Android platform wont changed to ETC format. ");
            return;
        }
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
        {
            for (int i = 0; i < needProcessPaths.Length; i++)
            {
                string path = Application.dataPath + needProcessPaths[i].path;
                //List<Material> temps = CollectAll<Material>(path);
                List<Material> temps = CollectAllDeep<Material>(path, "*.mat");
                Debug.Log("Found ETC Mat:" + temps.Count);
                foreach (Material one in temps)
                {
                    if (one.mainTexture == null) continue;

                    EtcAlphaInfo etc = new EtcAlphaInfo();
                    etc.mat = one;
                    etc.etcShader = Shader.Find(needProcessPaths[i].etcShader);
                    etc.iosShader = Shader.Find(needProcessPaths[i].iosShader);
                    etc.SourceTexture = one.mainTexture as Texture2D;
                    etc.SourcePath = AssetDatabase.GetAssetPath(one.mainTexture);
                    etc.SoureTextureImporter = AssetImporter.GetAtPath(etc.SourcePath) as TextureImporter;
                    etc.AlphaPath = Path.GetDirectoryName(etc.SourcePath) + "/" + Path.GetFileNameWithoutExtension(etc.SourcePath) + "_Alpha.png";
                    AssetImporter im = AssetImporter.GetAtPath(etc.AlphaPath);
                    etc.AlphaTextureImporter = im != null ? im as TextureImporter : null;
                    etc.AlphaFormat = 0;
                    etc.alphaMaxSize = needProcessPaths[i].alphaMaxSize;

                    //FIXME:check if etc texture is ready before,dont need change again for time save.
                    if (etc.SoureTextureImporter.textureFormat == TextureImporterFormat.ETC_RGB4
                        && etc.AlphaTextureImporter != null
                        && etc.AlphaTextureImporter.textureFormat == TextureImporterFormat.ETC_RGB4
                        && etc.AlphaTextureImporter.maxTextureSize == needProcessPaths[i].alphaMaxSize)
                    {
                        continue;
                    }

                    AssetDatabase.DeleteAsset(etc.AlphaPath);
                    AssetDatabase.Refresh();

                    //do process immediate.
                    ProcessETC(etc, dontChanged);
                    //release the memory
                    etc = null;
                    Resources.UnloadUnusedAssets();
                    System.GC.GetTotalMemory(true);
                    System.GC.Collect();
                }
            }
        }
        else if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
        {
            for (int i = 0; i < needProcessPaths.Length; i++)
            {
                string path = Application.dataPath + needProcessPaths[i].path;
                if (!Directory.Exists(path))
                {
                    Debug.LogError("Dont exist:" + path);
                    continue;
                }

                //List<Material> temps = CollectAll<Material>(path);
                List<Material> temps = CollectAllDeep<Material>(path, "*.mat");
                foreach (Material one in temps)
                {
                    if (one.mainTexture == null) continue;

                    EtcAlphaInfo etc = new EtcAlphaInfo();
                    etc.mat = one;
                    etc.etcShader = Shader.Find(needProcessPaths[i].etcShader);
                    etc.iosShader = Shader.Find(needProcessPaths[i].iosShader);
                    etc.SourceTexture = one.mainTexture as Texture2D;
                    etc.SourcePath = AssetDatabase.GetAssetPath(one.mainTexture);
                    etc.SoureTextureImporter = AssetImporter.GetAtPath(etc.SourcePath) as TextureImporter;
                    etc.AlphaPath = Path.GetDirectoryName(etc.SourcePath) + "/" + Path.GetFileNameWithoutExtension(etc.SourcePath) + "_Alpha.png";
                    etc.AlphaFormat = 1;

                    AssetDatabase.DeleteAsset(etc.AlphaPath);
                    AssetDatabase.Refresh();

                    //change immediate fix memory release.
                    etc.SoureTextureImporter.isReadable = true;
                    etc.SoureTextureImporter.mipmapEnabled = false;
                    etc.SoureTextureImporter.SetPlatformTextureSettings("iPhone", 2048, dontChanged ? TextureImporterFormat.RGBA32 : TextureImporterFormat.PVRTC_RGBA4);
                    AssetDatabase.ImportAsset(etc.SourcePath);
                    //change the material use etc
                    etc.mat.shader = etc.iosShader;

                    //release the memory
                    Resources.UnloadUnusedAssets();
                    System.GC.GetTotalMemory(true);
                    System.GC.Collect();
                }
            }
        }

        Debug.Log("ETC auto process end.");
    }

    /// <summary>
    /// do etc for android platform.
    /// </summary>
    /// <param name="etcInfo"></param>
    /// <param name="dontChanged"></param>
    private static void ProcessETC(EtcAlphaInfo etcInfo, bool dontChanged = false)
    {
        try
        {
            etcInfo.SoureTextureImporter.isReadable = true;
            etcInfo.SoureTextureImporter.mipmapEnabled = false;
            etcInfo.SoureTextureImporter.SetPlatformTextureSettings("Android", 2048, TextureImporterFormat.RGBA32);
            AssetDatabase.ImportAsset(etcInfo.SourcePath);

            if (dontChanged)
            {
                etcInfo.mat.shader = etcInfo.iosShader;
                return;
            }

            etcInfo.AlphaTexture = new Texture2D(etcInfo.SourceTexture.width,
                                                           etcInfo.SourceTexture.height, TextureFormat.RGBA32, false);
            Color32[] color32S = etcInfo.AlphaTexture.GetPixels32();
            Color32[] srcColor32S = etcInfo.SourceTexture.GetPixels32();

            if (etcInfo.AlphaFormat == 0)
            {
                for (int n = 0; n < color32S.Length; ++n)
                {
                    color32S[n] = new Color32(srcColor32S[n].a, 0, 0, 0);
                }
            }
            else if (etcInfo.AlphaFormat == 1)
            {
                for (int n = 0; n < color32S.Length; ++n)
                {
                    color32S[n] = new Color32(0, srcColor32S[n].a, 0, 0);
                }
            }
            else
            {
                for (int n = 0; n < color32S.Length; ++n)
                {
                    color32S[n] = new Color32(0, 0, srcColor32S[n].a, 0);
                }
            }
            etcInfo.AlphaTexture.SetPixels32(color32S);
            etcInfo.AlphaTexture.Apply(false);
            string fileName = Application.dataPath.Substring(0, Application.dataPath.Length - 6) + etcInfo.AlphaPath;
            File.WriteAllBytes(fileName, etcInfo.AlphaTexture.EncodeToPNG());
            while (!File.Exists(fileName)) ;
            AssetDatabase.Refresh(ImportAssetOptions.Default);
            etcInfo.AlphaTextureImporter = AssetImporter.GetAtPath(etcInfo.AlphaPath) as TextureImporter;
            etcInfo.AlphaTextureImporter.textureType = TextureImporterType.Advanced;
            etcInfo.AlphaTextureImporter.mipmapEnabled = false;
            etcInfo.AlphaTextureImporter.textureFormat = TextureImporterFormat.ETC_RGB4;
            etcInfo.AlphaTextureImporter.maxTextureSize = etcInfo.alphaMaxSize;
            etcInfo.AlphaTextureImporter.SetPlatformTextureSettings("Android", etcInfo.alphaMaxSize, TextureImporterFormat.ETC_RGB4);
            etcInfo.SoureTextureImporter.textureFormat = TextureImporterFormat.ETC_RGB4;
            etcInfo.SoureTextureImporter.SetPlatformTextureSettings("Android", 2048, TextureImporterFormat.ETC_RGB4);

            AssetDatabase.ImportAsset(etcInfo.SourcePath);
            AssetDatabase.ImportAsset(etcInfo.AlphaPath);

            etcInfo.AlphaTexture = AssetDatabase.LoadAssetAtPath(etcInfo.AlphaPath, typeof(Texture)) as Texture2D;

            //change the material use etc
            etcInfo.mat.shader = etcInfo.etcShader;
            etcInfo.mat.SetTexture("_AlphaTex", etcInfo.AlphaTexture);

        }
        catch (Exception e)
        {
            Debug.LogError("ETC Error:" + e.Message);
        }
    }

    private bool checkIfNeedProcess(string path)
    {
        for (int i = 0; i < m_needProcessPaths.Length; i++)
        {
            if (path.Contains(m_needProcessPaths[i].path)) return true;
        }
        return false;
    }

    public static List<T> CollectAll<T>(string path) where T : Object
    {
        List<T> l = new List<T>();
        string[] files = Directory.GetFiles(path);

        foreach (string file in files)
        {
            if (file.Contains(".meta")) continue;
            /*
            T asset = (T) AssetDatabase.LoadAssetAtPath(file, typeof(T));
            if (asset == null)
            {
                //throw new Exception("Asset is not " + typeof(T) + ": " + file);
                Debug.LogWarning("Asset is not " + typeof(T) + ": " + file);
                continue;
            }
            l.Add(asset);
            */
            string filePath = file.Replace(Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/') + 1), "");
            //Debug.Log ("file ="+filePath);
            Object o = AssetDatabase.LoadMainAssetAtPath(filePath);
            //Debug.Log (o.GetType());
            if (o.GetType() != typeof(T))
            {
                Debug.LogWarning("Asset is not " + typeof(T) + ": " + filePath);
                continue;
            }
            l.Add(o as T);
        }
        return l;
    }

    public static List<T> CollectAllDeep<T>(string path, string pattern) where T : Object
    {
        List<T> l = new List<T>();
        string[] files = Directory.GetFiles(path, pattern, SearchOption.AllDirectories);

        foreach (string file in files)
        {
            if (file.Contains(".meta")) continue;
            string filePath = file.Replace(Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/') + 1), "");
            Object o = AssetDatabase.LoadMainAssetAtPath(filePath);
            if (o.GetType() != typeof(T))
            {
                //throw new Exception("Asset is not " + typeof(T) + ": " + file);
                Debug.LogWarning("Asset is not " + typeof(T) + ": " + file);
                continue;
            }
            l.Add(o as T);
        }
        return l;
    }


}

#region ----------------------- Popup Editor Selected Folder ETC Setting ----------------------

/// <summary>
/// for user to set the ETC shader and iOS shader
/// </summary>
class EditorSelectedFolderWindow : EditorWindow
{
    /// ==========================================
    /// NEED MODIFY THIS SHADERS LIST
    /// ==========================================
    public string[] etcShaders = new string[] {
		"Unlit/Transparent Colored ETC1", 
        "tk2d/BlendVertexColor (ETC+Alpha using R channel)",
	};

    /// ==========================================
    /// NEED MODIFY THIS SHADERS LIST
    /// ==========================================
    public string[] iOSShaders = new string[] {
		"Unlit/Transparent Colored", 
        "tk2d/BlendVertexColor",
	};

    ///all max size of this alpha texture.
    public string[] sizes = new string[] { "64", "256", "512", "1024", "2048" };

    static EditorSelectedFolderWindow window;
    public int selEtcIndex = 0;
    public int selIOSIndex = 0;
    private string selFolder = "";
    private bool dontChanged = false;
    private int sizeIndex = 3;

    public static void Create(string selFolder)
    {
        if (window == null)
            window = (EditorSelectedFolderWindow)GetWindow(
                typeof(EditorSelectedFolderWindow),
                true,
                "Setting ETC Option",
                true
                );
        window.selFolder = selFolder;
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));

        GUILayout.BeginVertical(GUILayout.ExpandHeight(true));
        GUILayout.Label("Select ETC Shader:");
        selEtcIndex = GUILayout.SelectionGrid(selEtcIndex, etcShaders, 1, GUILayout.MinHeight(100));
        GUILayout.EndVertical();

        GUILayout.BeginVertical();
        GUILayout.Label("Select iOS Shader:");
        selIOSIndex = GUILayout.SelectionGrid(selIOSIndex, iOSShaders, 1, GUILayout.MinHeight(100));
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();

        //if dont need changed?
        GUILayout.Space(15);
        dontChanged = GUILayout.Toggle(dontChanged, "Dont Use ETC and Use True Color.", GUILayout.Height(25));
        GUILayout.Space(10);
        sizeIndex = GUILayout.SelectionGrid(sizeIndex, sizes, 6, GUILayout.MinHeight(50));

        GUILayout.Space(25);
        if (GUILayout.Button("Process", GUILayout.Height(40)))
        {
            ETCTexturePostprocessor.ChangeETC(new ETCTexturePostprocessor.targetFolder[]{
				new ETCTexturePostprocessor.targetFolder(selFolder, etcShaders[selEtcIndex], iOSShaders[selIOSIndex],int.Parse(sizes[sizeIndex]))
			}, dontChanged);
            window.Close();
        }
    }
}


#endregion





