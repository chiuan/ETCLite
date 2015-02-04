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
	public struct targetFolder
	{
		public string path;
		public string etcShader;
		public string iosShader;

		public targetFolder(string p,string ES,string IS){
			this.path = p;
			this.etcShader = ES;
			this.iosShader = IS;
		}
	}

	/// ==========================================
	/// ADD TARGET FOLDER TO AUTO USE ETC + ALPHA
	/// ==========================================
	private static targetFolder[] m_needProcessPaths = new targetFolder[]{
		new targetFolder("/ETC+Alpha/AutoETC/", "Unlit/Transparent Colored ETC1", "Unlit/Transparent Colored"),
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
	}

	static ETCTexturePostprocessor() {
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
		if(Selection.activeObject == null){
			Debug.LogError("have select some folder.");
			return;
		}

		string selFolder = AssetDatabase.GetAssetPath(Selection.activeObject);
		//Debug.Log("ETC Folder:" + selFolder + "\n"+Selection.activeObject.GetType());
		bool isFolder = Selection.activeObject.GetType() == typeof(UnityEngine.Object);
		selFolder = selFolder.Substring(6, isFolder ? selFolder.Length - 6 : selFolder.LastIndexOf('/') - 5);
		if(isFolder) selFolder += "/";
		Debug.Log("ETC Folder:" + selFolder);

		//ChangeETC(new targetFolder[]{new targetFolder(selFolder,"Unlit/Transparent Colored ETC1", "Unlit/Transparent Colored")});

		EditorSelectedFolderWindow.Create(selFolder);
	}

	/// <summary>
	/// do etc changed for the folder's materials
	/// </summary>
	/// <param name="needProcessPaths">Need process paths.</param>
	/// <param name="dontChanged">If set to <c>true</c> dont changed.</param>
	public static void ChangeETC(targetFolder[] needProcessPaths,bool dontChanged = false) {
		Debug.Log("ETC Auto At Platform : " + EditorUserBuildSettings.activeBuildTarget);
		if(EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android && EditorUserBuildSettings.activeBuildTarget != BuildTarget.iPhone)
		{
			Debug.LogWarning("Not the iOS or Android platform wont changed to ETC format. ");
			return;
		}
		if(EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
		{
			List<EtcAlphaInfo> _etcAlphaInfos = new List<EtcAlphaInfo>();

			for(int i=0; i< needProcessPaths.Length;i++)
			{
				string path = Application.dataPath + needProcessPaths[i].path;
				List<Material> temps = CollectAll<Material>(path);
				foreach(Material one in temps)
				{
					if(one.mainTexture == null) continue;

					EtcAlphaInfo etc =new EtcAlphaInfo();
					etc.mat = one;
					etc.etcShader = Shader.Find(needProcessPaths[i].etcShader);
					etc.iosShader = Shader.Find(needProcessPaths[i].iosShader);
					etc.SourceTexture = one.mainTexture as Texture2D;
					etc.SourcePath = AssetDatabase.GetAssetPath(one.mainTexture);
					etc.SoureTextureImporter = AssetImporter.GetAtPath(etc.SourcePath) as TextureImporter;
					etc.AlphaPath =Path.GetDirectoryName(etc.SourcePath)+"/"+ Path.GetFileNameWithoutExtension(etc.SourcePath) +"_Alpha.png";
					etc.AlphaFormat = 0;

					AssetDatabase.DeleteAsset(etc.AlphaPath);

					_etcAlphaInfos.Add(etc);
				}
			}
			AssetDatabase.Refresh();

			for (int i = 0; i < _etcAlphaInfos.Count; ++i)
			{
				try
				{
					_etcAlphaInfos[i].SoureTextureImporter.isReadable=true;
					_etcAlphaInfos[i].SoureTextureImporter.mipmapEnabled = false;
					_etcAlphaInfos[i].SoureTextureImporter.SetPlatformTextureSettings("Android",2048,TextureImporterFormat.RGBA32);
					AssetDatabase.ImportAsset(_etcAlphaInfos[i].SourcePath);

					if(dontChanged)
					{
						_etcAlphaInfos[i].mat.shader = _etcAlphaInfos[i].iosShader;
						continue;
					}
					
					_etcAlphaInfos[i].AlphaTexture = new Texture2D(_etcAlphaInfos[i].SourceTexture.width,
					                                               _etcAlphaInfos[i].SourceTexture.height, TextureFormat.RGBA32, false);
					Color32[] color32S = _etcAlphaInfos[i].AlphaTexture.GetPixels32();
					Color32[] srcColor32S = _etcAlphaInfos[i].SourceTexture.GetPixels32();
					
					if (_etcAlphaInfos[i].AlphaFormat == 0)
					{
						for (int n = 0; n < color32S.Length; ++n)
						{
							color32S[n] = new Color32(srcColor32S[n].a, 0, 0, 0);
						}                            
					}
					else if (_etcAlphaInfos[i].AlphaFormat == 1)
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
					_etcAlphaInfos[i].AlphaTexture.SetPixels32(color32S);
					_etcAlphaInfos[i].AlphaTexture.Apply(false);
					string fileName = Application.dataPath.Substring(0, Application.dataPath.Length - 6) +
						_etcAlphaInfos[i].AlphaPath;
					File.WriteAllBytes(fileName, _etcAlphaInfos[i].AlphaTexture.EncodeToPNG());
					while (!File.Exists(fileName)) ;
					AssetDatabase.Refresh(ImportAssetOptions.Default);
					_etcAlphaInfos[i].AlphaTextureImporter =
						AssetImporter.GetAtPath(_etcAlphaInfos[i].AlphaPath) as TextureImporter;
					while (_etcAlphaInfos[i].AlphaTextureImporter == null)
					{
						_etcAlphaInfos[i].AlphaTextureImporter =
							AssetImporter.GetAtPath(_etcAlphaInfos[i].AlphaPath) as TextureImporter;
					}
					_etcAlphaInfos[i].AlphaTextureImporter.textureType = TextureImporterType.Advanced;
					_etcAlphaInfos[i].AlphaTextureImporter.mipmapEnabled = false;
					_etcAlphaInfos[i].AlphaTextureImporter.SetPlatformTextureSettings("Android", 2048,TextureImporterFormat.ETC_RGB4);                      
					_etcAlphaInfos[i].SoureTextureImporter.SetPlatformTextureSettings("Android",2048,TextureImporterFormat.ETC_RGB4);
					
					AssetDatabase.ImportAsset(_etcAlphaInfos[i].SourcePath);
					AssetDatabase.ImportAsset(_etcAlphaInfos[i].AlphaPath);
					
					_etcAlphaInfos[i].AlphaTexture = AssetDatabase.LoadAssetAtPath(_etcAlphaInfos[i].AlphaPath, typeof(Texture)) as Texture2D;

					//change the material use etc
					_etcAlphaInfos[i].mat.shader = _etcAlphaInfos[i].etcShader;
					_etcAlphaInfos[i].mat.SetTexture("_AlphaTex",_etcAlphaInfos[i].AlphaTexture);
				}
				catch (Exception e)
				{
					Debug.LogError("ETC Error:"+e.Message);
				}
			}
		}
		else if(EditorUserBuildSettings.activeBuildTarget == BuildTarget.iPhone)
		{
			List<EtcAlphaInfo> _etcAlphaInfos = new List<EtcAlphaInfo>();
			
			for(int i=0; i< needProcessPaths.Length;i++)
			{
				string path = Application.dataPath + needProcessPaths[i].path;
				if(!Directory.Exists(path))
				{
					Debug.LogError("Dont exist:"+path);
					continue;
				}

				List<Material> temps = CollectAll<Material>(path);
				foreach(Material one in temps)
				{
					if(one.mainTexture == null) continue;
					
					EtcAlphaInfo etc =new EtcAlphaInfo();
					etc.mat = one;
					etc.etcShader = Shader.Find(needProcessPaths[i].etcShader);
					etc.iosShader = Shader.Find(needProcessPaths[i].iosShader);
					etc.SourceTexture = one.mainTexture as Texture2D;
					etc.SourcePath = AssetDatabase.GetAssetPath(one.mainTexture);
					etc.SoureTextureImporter = AssetImporter.GetAtPath(etc.SourcePath) as TextureImporter;
					etc.AlphaPath =Path.GetDirectoryName(etc.SourcePath)+"/"+ Path.GetFileNameWithoutExtension(etc.SourcePath) +"_Alpha.png";
					etc.AlphaFormat = 1;
					
					AssetDatabase.DeleteAsset(etc.AlphaPath);
					
					_etcAlphaInfos.Add(etc);
				}
			}
			AssetDatabase.Refresh();
			
			for (int i = 0; i < _etcAlphaInfos.Count; ++i)
			{
				_etcAlphaInfos[i].SoureTextureImporter.isReadable=true;
				_etcAlphaInfos[i].SoureTextureImporter.mipmapEnabled = false;
				_etcAlphaInfos[i].SoureTextureImporter.SetPlatformTextureSettings("iPhone",2048,dontChanged ? TextureImporterFormat.RGBA32 : TextureImporterFormat.PVRTC_RGBA4);

				AssetDatabase.ImportAsset(_etcAlphaInfos[i].SourcePath);

				//change the material use etc
				_etcAlphaInfos[i].mat.shader = _etcAlphaInfos[i].iosShader;
			}
		}

		Debug.Log("ETC auto process end.");
	}

	private bool checkIfNeedProcess(string path)
	{
		for(int i=0;i<m_needProcessPaths.Length;i++)
		{
			if(path.Contains(m_needProcessPaths[i].path)) return true;
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
			string filePath = file.Replace(Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/') + 1),"");
			//Debug.Log ("file ="+filePath);
			Object o = AssetDatabase.LoadMainAssetAtPath(filePath);
			//Debug.Log (o.GetType());
			if(o.GetType() != typeof(T))
			{
				Debug.LogWarning("Asset is not " + typeof(T) + ": " + filePath);
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
	};

	/// ==========================================
	/// NEED MODIFY THIS SHADERS LIST
	/// ==========================================
	public string[] iOSShaders = new string[] {
		"Unlit/Transparent Colored", 
	};

	static EditorSelectedFolderWindow window;
	public int selEtcIndex = 0;
	public int selIOSIndex = 0;
	private string selFolder = "";
	private bool dontChanged = false;
	
	public static void Create(string selFolder)
	{
		if(window == null)
			window = (EditorSelectedFolderWindow)GetWindow(
				typeof (EditorSelectedFolderWindow),
				true,
				"Setting ETC Option",
				true
				);
		window.selFolder = selFolder;
		window.Show();
	}
	
	void OnGUI()
	{
		GUILayout.BeginHorizontal();

		GUILayout.BeginVertical();
		GUILayout.Label("Select ETC Shader:");
		selEtcIndex = GUILayout.SelectionGrid( selEtcIndex, etcShaders, 1,GUILayout.Height(25));
		GUILayout.EndVertical();

		GUILayout.BeginVertical();
		GUILayout.Label("Select iOS Shader:");
		selIOSIndex = GUILayout.SelectionGrid( selIOSIndex, iOSShaders, 1,GUILayout.Height(25));
		GUILayout.EndVertical();

		GUILayout.EndHorizontal();

		//if dont need changed?
		GUILayout.Space(15);
		dontChanged = GUILayout.Toggle(dontChanged,"Dont Use ETC and Use True Color.",GUILayout.Height(25));

		GUILayout.Space(25);
		if(GUILayout.Button("Process",GUILayout.Height(40)))
		{
			ETCTexturePostprocessor.ChangeETC(new ETCTexturePostprocessor.targetFolder[]{
				new ETCTexturePostprocessor.targetFolder(selFolder, etcShaders[selEtcIndex], iOSShaders[selIOSIndex])
			},dontChanged);
			window.Close();
		}
	}
}


#endregion





