using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

public class BundleEditor
{
    public static string m_BundleTargetPath = Application.streamingAssetsPath;
    public static string AB_CONFIG_PATH = "Assets/Editor/ABConfig.asset";

    //key AB包名，value 路径，所有文件夹ab包dic
    public static Dictionary<string, string> m_AllFileDir = new Dictionary<string, string>();
    //过滤AB包
    public static List<string> m_AllFileAB = new List<string>();
    //单个prefab的ab包
    public static Dictionary<string, List<string>> m_allPrefabDir = new Dictionary<string, List<string>>();
    [MenuItem("Tools/打包")]
    public static void Build()
    {
        m_AllFileDir.Clear();
        m_AllFileAB.Clear();
        m_allPrefabDir.Clear();
        ABConfig abConfig = AssetDatabase.LoadAssetAtPath<ABConfig>(AB_CONFIG_PATH);
        foreach (ABConfig.FileDirABName fileDir in abConfig.m_AllFileDirAB)
        {
            if (m_AllFileDir.ContainsKey(fileDir.ABName))
            {
                Debug.LogError("AB包配置名字重复，请检查！");
            }
            else
            {
                m_AllFileDir.Add(fileDir.ABName, fileDir.Path);
                m_AllFileAB.Add(fileDir.Path);
            }
        }
        string[] allStr = AssetDatabase.FindAssets("t:prefab", abConfig.m_AllPrefabPath.ToArray());
        for (int i = 0;i< allStr.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(allStr[i]);
            EditorUtility.DisplayProgressBar("查找prefab", "prefab:" + path, i * 1.0f / allStr.Length);
            
            if (!ContainAllFileAB(path))
            {
                //加载prefab
                GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                //获取依赖项
                string[] allDepend = AssetDatabase.GetDependencies(path);

                List<string> allDependPath = new List<string>();
                for (int j = 0; j < allDepend.Length; j++)
                {
                    Debug.Log(allDepend[j]);
                    if (!ContainAllFileAB(allDepend[j]) && !allDepend[j].EndsWith(".cs"))
                    {
                        m_AllFileAB.Add(allDepend[j]);

                        allDependPath.Add(allDepend[j]);
                    }
                }
                if (m_allPrefabDir.ContainsKey(obj.name))
                {
                    Debug.LogError("存在相同名字的prefab!名字:"+obj.name);
                }
                else
                {
                    m_allPrefabDir.Add(obj.name, allDependPath);
                }

            }
        }

        foreach (string name in m_AllFileDir.Keys)
        {
            SetABName(name, m_AllFileDir[name]);
        }

        foreach (string name in m_allPrefabDir.Keys)
        {
            SetABName(name, m_allPrefabDir[name]);
        }

        BuildAssetBundle();

        string[] oldABNames = AssetDatabase.GetAllAssetBundleNames();
        for (int i = 0; i < oldABNames.Length; i++)
        {
            AssetDatabase.RemoveAssetBundleName(oldABNames[i], true);
            EditorUtility.DisplayProgressBar("删除AB包名", "名字：" + oldABNames[i], i * 1.0f / oldABNames.Length);
        }

        AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();

        
    }

    static void SetABName(string name,string path)
    {
        AssetImporter assetImporter = AssetImporter.GetAtPath(path);
        if (assetImporter == null)
        {
            Debug.LogError("不存在此路径文件：" + path);
        }
        else
        {
            assetImporter.assetBundleName = name;
        }

    }

    static void SetABName(string name, List<string> paths)
    {
        for (int i = 0; i < paths.Count; i++)
        {
            SetABName(name, paths[i]);
        }
    }

    static void BuildAssetBundle()
    {
        string[] allBundles = AssetDatabase.GetAllAssetBundleNames();
        //K路径和v包名
        Dictionary<string, string> resPathDic = new Dictionary<string, string>();
        for (int i = 0; i < allBundles.Length; i++)
        {
            string[] allBundlePath = AssetDatabase.GetAssetPathsFromAssetBundle(allBundles[i]);
            for (int j = 0; j < allBundlePath.Length; j++)
            {
                if (allBundlePath[j].EndsWith(".cs"))
                {
                    continue;
                }
                Debug.Log("此AB包：" + allBundles[i] + "下面包含的资源文件路径" + allBundlePath[j]);
                resPathDic.Add(allBundlePath[j], allBundles[i]);
            }
        }

        DeleteAB();
        //生成自己的配置表
        WriteData(resPathDic);

        BuildPipeline.BuildAssetBundles(m_BundleTargetPath, BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
    }

    /// <summary>
    /// 写配置
    /// </summary>
    /// <param name="resPathDic"></param>
    static void WriteData(Dictionary<string,string> resPathDic)
    {
        

        AssetBunldeConfig config = new AssetBunldeConfig();
        config.ABList = new List<ABBase>();
        foreach (string path in resPathDic.Keys)
        {
            ABBase abBase = new ABBase();
            abBase.Path = path;
            abBase.Crc = CRC32.GetCRC32(path);
            abBase.ABName = resPathDic[path];
            abBase.AssetName = path.Remove(0, path.LastIndexOf("/") + 1);//资源名
            abBase.ABDependce = new List<string>();
            string[] resDependce = AssetDatabase.GetDependencies(path);
            for (int i = 0; i < resDependce.Length; i++)
            {
                string tempPath = resDependce[i];
                if (tempPath == path || path.EndsWith(".cs"))
                {
                    continue;
                }
                string abName = "";
                if (resPathDic.TryGetValue(tempPath, out abName))
                {
                    if (abName == resPathDic[path])
                    {
                        continue;
                    }
                    if (!abBase.ABDependce.Contains(abName))
                    {
                        abBase.ABDependce.Add(abName);
                    }
                }

            }
            config.ABList.Add(abBase);
        }

        //写入XML
        string xmlPath = Application.dataPath + "/AssetbundleConfig.xml";
        if (File.Exists(xmlPath))
        {
            File.Delete(xmlPath);
        }
        FileStream fs = new FileStream(xmlPath, FileMode.Create, FileAccess.ReadWrite,FileShare.ReadWrite);
        StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
        XmlSerializer xs = new XmlSerializer(config.GetType());
        xs.Serialize(sw, config);
        sw.Close();
        fs.Close();
        //写入二进制
        string bytePath = m_BundleTargetPath + "/AssetBundleConfig.bytes";
        FileStream fsb = new FileStream(bytePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(fsb, config);
        fsb.Close();
    }

    /// <summary>
    /// 删除这次打包流程中没有的AB包（以前打的包现在弃用了
    /// </summary>
    static void DeleteAB()
    {
        string[] allBundlesName = AssetDatabase.GetAllAssetBundleNames();
        DirectoryInfo direction = new DirectoryInfo(m_BundleTargetPath);
        FileInfo[] files = direction.GetFiles("*", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            if (ConatinABName(files[i].Name, allBundlesName) || files[i].Name.EndsWith(".meta"))
            {
                continue;
            }
            else
            {
                Debug.Log("此AB包已经被删或者改名了：" + files[i].Name);
                if (File.Exists(files[i].FullName))
                {
                    File.Delete(files[i].FullName);
                }
            }
        }
    }

    /// <summary>
    /// 遍历文件夹里的文件名与设置的所有AB包进行检查判断
    /// </summary>
    /// <param name="name"></param>
    /// <param name="strs"></param>
    /// <returns></returns>
    static bool ConatinABName(string name, string[] strs)
    {
        for (int i = 0; i < strs.Length; i++)
        {
            if (name == strs[i])
            {
                return true;
            }
        }
        return false;
    }


    /// <summary>
    /// 判断是否在打文件夹包的时候打过了
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    static bool ContainAllFileAB(string path)
    {
        for (int i = 0; i < m_AllFileAB.Count; i++)
        {
            if (path == m_AllFileAB[i] || path.Contains(m_AllFileAB[i]))
            {
                return true;
            }
        }
        return false;
    }
}
