using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class AssetBundleManager : Singleton<AssetBundleManager>
{
    //资源关系依赖配置表，可以根据crc来找到对应资源
    protected Dictionary<uint, ResourceItem> m_ResourceItemDic = new Dictionary<uint, ResourceItem>();
    //储存已加载的AB包，key为crc
    protected Dictionary<uint, AssetBundleItem> m_AssetBundleItemDic = new Dictionary<uint, AssetBundleItem>();
    //AssetBundleItem 类对象池
    protected ClassObjectPool<AssetBundleItem> m_AssetBundleItemPool = ObjectManager.Instance.GetOrCreatClassPool<AssetBundleItem>(500);
    /// <summary>
    /// 加载AB配置表
    /// </summary>
    /// <returns></returns>
    public bool LoadAssetBundleConfig()
    {
        m_ResourceItemDic.Clear();
        string configPath = Application.streamingAssetsPath + "/assetbundleconfig";
        AssetBundle configAB = AssetBundle.LoadFromFile(configPath);
        TextAsset textAsset = configAB.LoadAsset<TextAsset>("assetbundleconfig");
        if (textAsset == null)
        {
            Debug.Log("AssetBundleConfig is no exist!");
            return false;
        }

        MemoryStream stream = new MemoryStream(textAsset.bytes);
        BinaryFormatter bf = new BinaryFormatter();
        AssetBunldeConfig abConfig = (AssetBunldeConfig)bf.Deserialize(stream);
        stream.Close();

        for (int i = 0; i < abConfig.ABList.Count; i++)
        {
            ABBase abBase = abConfig.ABList[i];
            ResourceItem item = new ResourceItem();
            item.m_Crc = abBase.Crc;
            item.m_AssetName = abBase.AssetName;
            item.m_AssetBundleName = abBase.ABName;
            item.m_DependAssetBundle = abBase.ABDependce;
            if (m_ResourceItemDic.ContainsKey(item.m_Crc))
            {
                Debug.LogError("重复的Crc 资源名：" + item.m_AssetName + " AB包名：" + item.m_AssetBundleName);
            }
            else
            {
                m_ResourceItemDic.Add(item.m_Crc, item);
            }
        }

        return true;
    }

    /// <summary>
    /// 根据路径CRC加载中间类ResourceItem
    /// </summary>
    /// <param name="crc"></param>
    /// <returns></returns>
    public ResourceItem LoadResourceAssetBundle(uint crc)
    {
        ResourceItem item = null;
        //如果没找到或者找到了为空
        if (!m_ResourceItemDic.TryGetValue(crc, out item)||item==null)
        {
            Debug.LogError(string.Format("LoadResourceAssetBundle eroor:can not fin crc {0} in AssetBundleConfig", crc.ToString()));
            return item;
        }

        if (item.m_AssetBundle != null)
        {
            return item;
        }

        item.m_AssetBundle = LoadAssetBundle(item.m_AssetName);


        //加载依赖
        if (item.m_DependAssetBundle != null)
        {
            for (int i = 0; i < item.m_DependAssetBundle.Count; i++)
            {
                LoadAssetBundle(item.m_DependAssetBundle[i]);
            }
        }

        return item;

    }
    /// <summary>
    /// 加载单个assetbundle根据名字
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    private AssetBundle LoadAssetBundle(string name)
    {
        AssetBundleItem item = null;
        uint crc = CRC32.GetCRC32(name);

        if (!m_AssetBundleItemDic.TryGetValue(crc, out item))
        {
            AssetBundle assetBundle = null;
            string fullPath = Application.streamingAssetsPath + "/" + name;

            if (File.Exists(fullPath))
            {
                assetBundle = AssetBundle.LoadFromFile(fullPath);
            }

            if (assetBundle == null)
            {
                Debug.LogError("Load AssetBundle Error:" + fullPath);
            }

            item = m_AssetBundleItemPool.Spawn(true);
            item.assetBundle = assetBundle;
            item.RefCount++;
            m_AssetBundleItemDic.Add(crc, item);
        }
        else
        {
            item.RefCount++;
        }

        return item.assetBundle;


    }

    /// <summary>
    /// 卸载
    /// </summary>
    /// <param name="item"></param>
    public void ReleaseAsset(ResourceItem item)
    {
        if (item == null)
        {
            return;
        }

        if (item.m_DependAssetBundle != null && item.m_DependAssetBundle.Count > 0)
        {
            for (int i = 0; i < item.m_DependAssetBundle.Count; i++)
            {
                UnloadAssetBundle(item.m_DependAssetBundle[i]);

            }
        }

        UnloadAssetBundle(item.m_AssetBundleName);
    }

    private void UnloadAssetBundle(string name)
    {
        AssetBundleItem item = null;
        uint crc = CRC32.GetCRC32(name);
        if (m_AssetBundleItemDic.TryGetValue(crc,out item)&&item !=null)
        {
            item.RefCount--;
            if (item.RefCount <= 0 && item.assetBundle !=null)
            {
                item.assetBundle.Unload(true);
                item.Rest();
                m_AssetBundleItemPool.Recycle(item);
                m_AssetBundleItemDic.Remove(crc);
            }
        }
    }

    /// <summary>
    /// 根据crc查找ResourceItem
    /// </summary>
    /// <param name="crc"></param>
    /// <returns></returns>
    public ResourceItem FindResourceItem(uint crc)
    {
        return m_ResourceItemDic[crc];
    }
}



public class AssetBundleItem
{
    public AssetBundle assetBundle = null;
    public int RefCount;

    public void Rest()
    {
        assetBundle = null;
        RefCount = 0;
    }
}



public class ResourceItem
{
    //资源路径的CRC
    public uint m_Crc = 0;
    //资源名字
    public string m_AssetName = string.Empty;
    //资源所在的Assetbundle
    public string m_AssetBundleName = string.Empty;
    //该资源所依赖的AssetBundle
    public List<string> m_DependAssetBundle = null;
    //该资源加载完的AB包
    public AssetBundle m_AssetBundle = null;
    //------------------------------------------------
    //资源对象
    public Object m_Obj = null;
    //资源唯一标识
    public int m_GUID = 0;
    //资源最后使用的时间
    public float m_LastUseTime = 0.0f;
    //引用计数
    protected int m_RefCount = 0;
    public int RefCount
    {
        get
        {
            return m_RefCount;
        }
        set
        {
            m_RefCount = value;
            if (m_RefCount < 0)
            {
                Debug.LogError("refcount < 0 " + m_RefCount + "，" + (m_Obj != null ? m_Obj.name : "name is null"));
            }
        }
    }

}


