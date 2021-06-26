using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BundleEditor
{
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
