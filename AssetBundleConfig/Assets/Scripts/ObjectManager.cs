using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ObjectManager : Singleton<ObjectManager>
{

    //对象池节点
    public Transform RecyclePoolTrs;
    //场景节点
    public Transform SceneTrs;
    //对象池key是crc
    protected Dictionary<uint, List<ResourceObj>> m_ObjectPoolDic = new Dictionary<uint, List<ResourceObj>>();
    //暂存ResObj的Dic
    protected Dictionary<int, ResourceObj> m_ResourceObjDic = new Dictionary<int, ResourceObj>();
    //ResourceObj类对象池
    protected ClassObjectPool<ResourceObj> m_ResourceObjClassPool = null;
    //根据异步的guid储存ResourceObj，来判断是否正在异步加载
    protected Dictionary<long, ResourceObj> m_AsyncResObjs = new Dictionary<long, ResourceObj>();



    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="recycleTrs">回收节点</param>
    /// <param name="sceneTrs">场景默认节点</param>
    public void Init(Transform recycleTrs,Transform sceneTrs)
    {
        m_ResourceObjClassPool= GetOrCreatClassPool<ResourceObj>(1000);
        RecyclePoolTrs = recycleTrs;
        SceneTrs = sceneTrs;
    }

    /// <summary>
    /// 清空对象池
    /// </summary>
    public void ClearCache()
    {
        List<uint> tempList = new List<uint>();
        foreach (uint key in m_ObjectPoolDic.Keys)
        {
            List<ResourceObj> st = m_ObjectPoolDic[key];
            for (int i = st.Count-1; i>=0 ; i++)
            {
                ResourceObj resObj = st[i];
                if (!System.Object.ReferenceEquals(resObj.m_CloneObj, null)&&resObj.m_bClear)
                {
                    GameObject.Destroy(resObj.m_CloneObj);
                    m_ResourceObjDic.Remove(resObj.m_CloneObj.GetInstanceID());
                    resObj.Reset();
                    m_ResourceObjClassPool.Recycle(resObj);
                }
            }

            if (st.Count <= 0)
            {
                tempList.Add(key);
            }
        }

        for (int i = 0; i < tempList.Count; i++)
        {
            uint temp = tempList[i];
            if (m_ObjectPoolDic.ContainsKey(temp))
            {
                m_ObjectPoolDic.Remove(temp);
            }
        }

        tempList.Clear();
    }
 
    /// <summary>
    /// 从对象池取出obj
    /// </summary>
    /// <param name="crc"></param>
    /// <returns></returns>
    protected ResourceObj GetObjFromPool(uint crc)
    {
        List<ResourceObj> st = null;
        if (m_ObjectPoolDic.TryGetValue(crc, out st) && st != null&&st.Count>0)
        {
            //resourceManager的引用计数
            ResourceManager.Instance.IncreaseResouceRef(crc);
            ResourceObj resObj = st[0];
            st.RemoveAt(0);
            GameObject obj = resObj.m_CloneObj;
            //判空比==效率高
            if (!System.Object.ReferenceEquals(obj, null))
            {
                resObj.m_Already = false;

#if UNITY_EDITOR
                if (obj.name.EndsWith("(Recycle)"))
                {
                    obj.name = obj.name.Replace("(Recycle)", "");
                }
#endif
            }
            return resObj;
        }
        return null;
    }

    /// <summary>
    /// 取消异步加载
    /// </summary>
    /// <param name="guid"></param>
    public void CancelLoad(long guid)
    {
        ResourceObj resObj = null;
        if (m_AsyncResObjs.TryGetValue(guid, out resObj)&&ResourceManager.Instance.CancelLoad(resObj))
        {
            m_AsyncResObjs.Remove(guid);
            resObj.Reset();
            m_ResourceObjClassPool.Recycle(resObj);
        }
    }

    /// <summary>
    /// 是否正在异步加载
    /// </summary>
    /// <param name="guid"></param>
    /// <returns></returns>
    public bool IsingAsyncLoad(long guid)
    {
        return m_AsyncResObjs[guid] != null;
    }

    /// <summary>
    /// 该对象是否是对象池创建的
    /// </summary>
    /// <returns></returns>
    public bool IsObjectManagerCreat(GameObject obj)
    {
        ResourceObj resObj = m_ResourceObjDic[obj.GetInstanceID()];
        return resObj == null ? false : true;
    }

    /// <summary>
    /// 预加载Gameobject,就是加载完之后卸载
    /// </summary>
    /// <param name="path">路径</param>
    /// <param name="count">预加载个数</param>
    /// <param name="clear">跳场景是否清除 </param>
    public void PreloadGameObject(string path, int count = 1, bool clear = false)
    {
        List<GameObject> tempGameObjectList = new List<GameObject>();
        for (int i = 0; i < count; i++)
        {
            GameObject obj = InstantiateObject(path, false, bClear: clear);
            tempGameObjectList.Add(obj);
        }

        for (int i = 0; i < count; i++)
        {
            GameObject obj = tempGameObjectList[i];
            ReleaseObject(obj);
            obj = null;
        }

        tempGameObjectList.Clear();
    }

    /// <summary>
    /// 同步加载
    /// </summary>
    /// <param name="path">路径</param>
    /// <param name="bClear">是否跳场景清空</param>
    /// <returns></returns>
    public GameObject InstantiateObject(string path,bool setSceneObj = false,bool bClear = true)
    {
        uint crc = CRC32.GetCRC32(path);
        //先从池子里获取这个resourceobj
        ResourceObj resourceObj = GetObjFromPool(crc);
        if (resourceObj == null)
        {
            //没有就从池子生成
            resourceObj = m_ResourceObjClassPool.Spawn(true);
            resourceObj.m_Crc = crc;
            resourceObj.m_bClear = bClear;
            //ResourceManager提供加载方法
            resourceObj = ResourceManager.Instance.LoadResource(path, resourceObj);

            if (resourceObj.m_ResItem.m_Obj != null)
            {
                resourceObj.m_CloneObj = GameObject.Instantiate(resourceObj.m_ResItem.m_Obj) as GameObject;

            }
        }
        //是否要把它放到场景下面
        if (setSceneObj)
        {
            resourceObj.m_CloneObj.transform.SetParent(SceneTrs, false);
        }

        int tempID = resourceObj.m_CloneObj.GetInstanceID();
        if (!m_ResourceObjDic.ContainsKey(tempID))
        {
            m_ResourceObjDic.Add(tempID, resourceObj);
        }
        
        return resourceObj.m_CloneObj;
    }

    /// <summary>
    /// 异步对象加载
    /// </summary>
    /// <param name="path"></param>
    /// <param name="fealFinish"></param>
    /// <param name="priority"></param>
    /// <param name="setSceneObject"></param>
    /// <param name="bClear"></param>
    /// <param name="param"></param>
    public long InstantiateObjectAsync(string path,OnAsyncObjFinish dealFinish,LoadResPriority priority,bool setSceneObject = false,bool bClear = true,params object[] param)
    {
        if (string.IsNullOrEmpty(path))
        {
            return 0;
        }

        uint crc = CRC32.GetCRC32(path);
        ResourceObj resObj = GetObjFromPool(crc);
        if (resObj != null)
        {
            if (setSceneObject)
            {
                resObj.m_CloneObj.transform.SetParent(SceneTrs, false);
            }

            if (dealFinish != null)
            {
                dealFinish(path, resObj.m_CloneObj, param);
            }
            return resObj.m_GUID;
        }

        long guid = ResourceManager.Instance.CreatGuid();
        resObj = m_ResourceObjClassPool.Spawn(true);
        resObj.m_Crc = crc;
        resObj.m_SetSceneParent = setSceneObject;
        resObj.m_bClear = bClear;
        resObj.m_DealFinish = dealFinish;
        resObj.m_Param = param;
        //调用ResourceManager异步加载借口
        ResourceManager.Instance.AsyncLoadResource(path, resObj, OnLoadResourceObjFinish, priority);
        return guid;
    }

    /// <summary>
    /// 资源加载完成回调
    /// </summary>
    /// <param name="path">路径</param>
    /// <param name="resObj">中间类</param>
    /// <param name="param">参数</param>
    void OnLoadResourceObjFinish(string path,ResourceObj resObj,params object[] param)
    {
        if (resObj == null)
        {
            return;
        }

        if (resObj.m_ResItem.m_Obj == null)
        {
#if UNITY_EDITOR
            Debug.LogError("异步资源加载的资源为空：" + path);
#endif
        }
        else
        {
            resObj.m_CloneObj = GameObject.Instantiate(resObj.m_ResItem.m_Obj) as GameObject;
        }
        //加载完成就从正在加载的异步中移除
        if (m_AsyncResObjs.ContainsKey(resObj.m_GUID))
        {
            m_AsyncResObjs.Remove(resObj.m_GUID);
        }
        //
        if (resObj.m_CloneObj != null && resObj.m_SetSceneParent)
        {
            resObj.m_CloneObj.transform.SetParent(SceneTrs, false);
        }

        if (resObj.m_DealFinish != null)
        {
            int tempID = resObj.m_CloneObj.GetInstanceID();
            if (!m_ResourceObjDic.ContainsKey(tempID))
            {
                m_ResourceObjDic.Add(tempID, resObj);
            }

            resObj.m_DealFinish(path, resObj.m_CloneObj, resObj.m_Param);
        }
    }


    /// <summary>
    /// 回收资源
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="maxCacheCount"></param>
    /// <param name="destroyCache"></param>
    /// <param name="recycleParent"></param>
    public void ReleaseObject(GameObject obj, int maxCacheCount = -1, bool destroyCache = false, bool recycleParent = true)
    {
        if (obj == null)
        {
            return;
        }

        ResourceObj resObj = null;
        int tempID = obj.GetInstanceID();
        if (!m_ResourceObjDic.TryGetValue(tempID, out resObj))
        {
            Debug.Log(obj.name + "对象不是objectManager创建的！");
            return;
        }

        if (resObj == null)
        {
            Debug.LogError("缓存的ResourceObj为空！");
            return;
        }

        if (resObj.m_Already)
        {
            Debug.LogError("该对象已经放回对象池了，检查自己是否清空引用");
            return;
        }
#if UNITY_EDITOR
        obj.name += "(Recycle)";
#endif
        List<ResourceObj> st = null;
        if (maxCacheCount == 0)
        {
            m_ResourceObjDic.Remove(tempID);
            ResourceManager.Instance.ReleaseResourece(resObj, destroyCache);
            resObj.Reset();
            m_ResourceObjClassPool.Recycle(resObj);
        }
        else//回收到对象池
        {
            //看池子里有没有这个资源
            if (!m_ObjectPoolDic.TryGetValue(resObj.m_Crc, out st) || st == null)
            {
                st = new List<ResourceObj>();
                m_ObjectPoolDic.Add(resObj.m_Crc, st);
            }

            if (resObj.m_CloneObj)
            {
                if (recycleParent)
                {
                    resObj.m_CloneObj.transform.SetParent(RecyclePoolTrs);
                }
                else
                {
                    resObj.m_CloneObj.SetActive(false);
                }
            }

            if (maxCacheCount < 0 || st.Count < maxCacheCount)
            {
                st.Add(resObj);
                resObj.m_Already = true;
                //ResourceManager 做一个引用计数
                ResourceManager.Instance.DecreaseResourceRef(resObj);
            }
            else//达到了最大的缓存个数
            {
                m_ResourceObjDic.Remove(tempID);
                ResourceManager.Instance.ReleaseResourece(resObj, destroyCache);
                resObj.Reset();
                m_ResourceObjClassPool.Recycle(resObj);
            }


        }
    }


    #region 类对象池的使用
    protected Dictionary<Type, object> m_ClassPoolDic = new Dictionary<Type, object>();
    /// <summary>
    /// 创建雷队想吃，创建完之后外面可以保存ClassObjectPool<T>,然后调用spawn和recycle来创建回收类对象
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="maxcount"></param>
    /// <returns></returns>
    public ClassObjectPool<T> GetOrCreatClassPool<T>(int maxcount) where T : class, new()
    {
        //存下类型
        Type type = typeof(T);
        object outObj = null;
        //如果不包含这个类型，或者取出来之后没有东西
        if (!m_ClassPoolDic.TryGetValue(type, out outObj) || outObj == null)
        {
            ClassObjectPool<T> newPool = new ClassObjectPool<T>(maxcount);
            //加入
            m_ClassPoolDic.Add(type, newPool);
            return newPool;
        }

        //如果在里面直接返回
        return outObj as ClassObjectPool<T>;
    }

    /// <summary>
    /// 从对象池中取T对象，可以直接调用 ClassObjectPool<T> XXX = ObjectManager.Instance.GetOrCreatClassPool<T>(number);来取
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="maxcount"></param>
    /// <returns></returns>
    public T NewClassObjectFromPool<T>(int maxcount) where T : class, new()
    {
        ClassObjectPool<T> pool = GetOrCreatClassPool<T>(maxcount);
        if (pool == null)
        {
            return null;
        }

        return pool.Spawn(true);
    }
    #endregion

}
