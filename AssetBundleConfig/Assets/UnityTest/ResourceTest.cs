using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class ResourceTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        TestLoadAB();
    }

    void TestLoadAB()
    {
        //通过AB包加载配置文件
        AssetBundle abcfg = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/assetbundleconfig");
        TextAsset textAsset = abcfg.LoadAsset<TextAsset>("AssetBundleConfig");
        MemoryStream stream = new MemoryStream(textAsset.bytes);
        BinaryFormatter bf = new BinaryFormatter();
        AssetBunldeConfig abConfig = (AssetBunldeConfig)bf.Deserialize(stream);
        stream.Close();
        string path = "Assets/GameData/Prefabs/Attack.prefab";
        uint crc = CRC32.GetCRC32(path);
        ABBase abBase = null;
        for (int i = 0; i < abConfig.ABList.Count; i++)
        {
            if (abConfig.ABList[i].Crc == crc)
            {
                abBase = abConfig.ABList[i];
            }
        }
        //加载ab包之前先获取依赖项
        for (int i = 0; i < abBase.ABDependce.Count; i++)
        {
            AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/" + abBase.ABDependce[i]);
        }

        AssetBundle assetBundle = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/" + abBase.ABName);
        GameObject obj = GameObject.Instantiate(assetBundle.LoadAsset<GameObject>(abBase.AssetName));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
