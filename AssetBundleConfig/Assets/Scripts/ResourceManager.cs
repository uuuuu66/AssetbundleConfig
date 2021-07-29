using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 加载优先级
/// </summary>
public enum LoadResPriority
{
    RES_HIGHT = 0,//最高优先级
    RES_MIDDLE,//一般优先级
    RES_SLOW,//低优先级
    RES_NUM,
}

public class ResourceObj
{
    //路径对应Crc
    public uint m_Crc = 0;
    //存ResourceItem
    public ResourceItem m_ResItem = null;
    //实例化出来的gameobject
    public GameObject m_CloneObj = null;
    //是否跳场景清除
    public bool m_bClear = true;
    //储存GUID
    public long m_GUID = 0;
    //是否已经放回对象池
    public bool m_Already = false;
    //---------------------------
    //是否放到场景节点下面
    public bool m_SetSceneParent = false;
    //实例化资源加载完成回调
    public OnAsyncObjFinish m_DealFinish = null;
    //异步参数
    public object[] m_Param;
    //离线数据
    public OfflineData m_OfflineData = null;


    public void Reset()
    {
        m_Crc = 0;
        m_CloneObj = null;
        m_bClear = true;
        m_GUID = 0;
        m_Already = false;
        m_SetSceneParent = false;
        m_DealFinish = null;
        m_Param = null;
        m_OfflineData = null;
    }
}

public class AsyncLoadResParam
{
    public List<AsyncCallBack> m_CallBackList = new List<AsyncCallBack>();
    public uint m_Crc;
    public string m_Path;
    public bool m_Sprite = false;
    public LoadResPriority m_Priority = LoadResPriority.RES_SLOW;

    public void Reset()
    {
        m_CallBackList.Clear();
        m_Crc = 0;
        m_Path = "";
        m_Sprite = false;
        m_Priority = LoadResPriority.RES_SLOW;
    }
}

public class AsyncCallBack
{
    //加载完成的回调，针对objectManager
    public OnAsyncFinish m_DealFinish = null;
    public ResourceObj m_ResObj = null;
    //加载完成的回调
    public OnAsyncObjFinish m_DealObjFinish = null;

    //回调参数
    public object[] m_Param;

    public void Reset()
    {
        m_DealFinish = null;
        m_DealObjFinish = null;
        m_Param = null;
        m_ResObj = null;
    }
}

//资源加载完成回调
public delegate void OnAsyncObjFinish(string path,Object obj,params object[] param);

//实例化对象加载完成回调
public delegate void OnAsyncFinish(string path, ResourceObj resObj, params object[] param);


public class ResourceManager : Singleton<ResourceManager>
{
    protected long m_Guid = 0;
    public bool m_LoadFromAssetBundle = true;
    //缓存使用的资源列表
    public Dictionary<uint, ResourceItem> m_AssetDic { get; set; } = new Dictionary<uint, ResourceItem>();
    //缓存引用计数为0的资源列表，达到缓存最大的时候，释放这个列表里面最早没用的资源
    protected CMapList<ResourceItem> m_NoRefrenceAssetMapList = new CMapList<ResourceItem>();

    //中间类，回调类的类对象池
    protected ClassObjectPool<AsyncLoadResParam> m_AsyncLoadResParamPool = new ClassObjectPool<AsyncLoadResParam>(50);
    protected ClassObjectPool<AsyncCallBack> m_AsyncCallBackPool = new ClassObjectPool<AsyncCallBack>(100);
    //mono脚本
    protected MonoBehaviour m_Startmono;
    //正在异步加载的资源列表,所有优先级的列表
    protected List<AsyncLoadResParam>[] m_LoadingAssetList = new List<AsyncLoadResParam>[(int)LoadResPriority.RES_NUM];
    //正在异步加载的Dic
    protected Dictionary<uint, AsyncLoadResParam> m_LoadingAssetDic = new Dictionary<uint, AsyncLoadResParam>();

    //最长连续卡着加载的时间，单位微秒
    private const long MAXLOADRESTIME = 200000;

    public void Init(MonoBehaviour mono)
    {
        for (int i = 0; i < (int)LoadResPriority.RES_NUM; i++)
        {
            m_LoadingAssetList[i] = new List<AsyncLoadResParam>();

        }
        m_Startmono = mono;
        m_Startmono.StartCoroutine(AsyncLoadCor());
    }

    /// <summary>
    /// 创建唯一的GUID
    /// </summary>
    /// <returns></returns>
    public long CreatGuid()
    {
        return m_Guid++;
    }


    /// <summary>
    /// 清空缓存，主要在跳场景的时候
    /// </summary>
    public void ClearCache()
    {
        List<ResourceItem> tempList = new List<ResourceItem>();
        foreach (ResourceItem item  in m_AssetDic.Values)
        {
            if (item.m_Clear)
            {
                tempList.Add(item);
            }
        }

        foreach (ResourceItem item in tempList)
        {
            DestroyResourceItem(item, true);
        }

        tempList.Clear();

    }

    /// <summary>
    /// 取消异步加载资源
    /// </summary>
    /// <returns></returns>
    public bool CancelLoad(ResourceObj res)
    {
        AsyncLoadResParam para = null;
        if (m_LoadingAssetDic.TryGetValue(res.m_Crc, out para) && m_LoadingAssetList[(int)para.m_Priority].Contains(para))
        {
            for (int i = para.m_CallBackList.Count; i > 0; i--)
            {
                AsyncCallBack tempCallBack = para.m_CallBackList[i];
                if (tempCallBack != null&&res==tempCallBack.m_ResObj)
                {
                    tempCallBack.Reset();
                    m_AsyncCallBackPool.Recycle(tempCallBack);
                    para.m_CallBackList.Remove(tempCallBack);
                }
            }

            if (para.m_CallBackList.Count <= 0)
            {
                para.Reset();
                m_LoadingAssetList[(int)para.m_Priority].Remove(para);
                m_AsyncLoadResParamPool.Recycle(para);
                m_LoadingAssetDic.Remove(res.m_Crc);
                return true;
            }

        }

        return false;
    }

    /// <summary>
    /// 根据ResObj增加引用计数
    /// </summary>
    /// <returns></returns>
    public int IncreaseResouceRef(ResourceObj resObj,int count = 1)
    {
        return resObj != null ? IncreaseResouceRef(resObj.m_Crc, count) : 0;
    }

    /// <summary>
    /// 根据path增加引用计数
    /// </summary>
    /// <param name="crc"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public int IncreaseResouceRef(uint crc = 0, int count = 1)
    {
        ResourceItem item = null;
        if (!m_AssetDic.TryGetValue(crc, out item) || item == null)
        {
            return 0;
        }
        item.RefCount += count;
        item.m_LastUseTime = Time.realtimeSinceStartup;

        return item.RefCount;
    }

    /// <summary>
    /// 根据ResourceObj减少引用计数
    /// </summary>
    /// <param name="resObj"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public int DecreaseResourceRef(ResourceObj resObj, int count = 1)
    {
        return resObj != null ? DecreaseResourceRef(resObj.m_Crc, count) : 0;
    }

    /// <summary>
    /// 根据路径减少引用计数
    /// </summary>
    /// <param name="crc"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    public int DecreaseResourceRef(uint crc=0, int count = 1)
    {
        ResourceItem item = null;
        if (!m_AssetDic.TryGetValue(crc, out item) || item == null)
        {
            return 0;
        }
        item.RefCount -= count;

        return item.RefCount;
    }

    /// <summary>
    /// 预加载
    /// </summary>
    /// <param name="path"></param>
    public void PreloadRes(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        uint crc = CRC32.GetCRC32(path);

        //预加载不需要记录引用计数
        ResourceItem item = GetCacheResourceItem(crc,0);
        if (item != null)
        {
            return;
        }

        Object obj = null;

#if UNITY_EDITOR
        if (!m_LoadFromAssetBundle)
        {

            item = AssetBundleManager.Instance.FindResourceItem(crc);
            if (item.m_Obj != null)
            {
                obj = item.m_Obj as Object;
            }
            else
            {
                obj = LoadAssetByEditor<Object>(path);
            }

        }
#endif

        if (obj == null)
        {
            item = AssetBundleManager.Instance.LoadResourceAssetBundle(crc);
            if (item != null && item.m_AssetBundle != null)
            {
                if (item.m_Obj != null)
                {
                    obj = item.m_Obj as Object;
                }
                else
                {
                    obj = item.m_AssetBundle.LoadAsset<Object>(item.m_AssetName);
                }
            }
        }
        CacheResource(path, ref item, crc, obj);
        //跳场景不清空缓存
        item.m_Clear = false;
        ReleaseResourece(path , false);
    }

#if UNITY_EDITOR
    protected T LoadAssetByEditor<T>(string path) where T : UnityEngine.Object
    {
        return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
    }
#endif


    /// <summary>
    /// 同步加载资源，针对给ObjectManager的接口
    /// </summary>
    /// <param name="path"></param>
    /// <param name="resObj"></param>
    /// <returns></returns>
    public ResourceObj LoadResource(string path,ResourceObj resObj)
    {
        if (resObj == null)
        {
            return null;
        }

        uint crc = resObj.m_Crc == 0 ? CRC32.GetCRC32(path) : resObj.m_Crc;
        //获取缓存
        ResourceItem item = GetCacheResourceItem(crc);

        if (item != null)
        {
            resObj.m_ResItem = item;
            return resObj;
        }

        Object obj = null;
#if UNITY_EDITOR
        if (!m_LoadFromAssetBundle)
        {
            item = AssetBundleManager.Instance.FindResourceItem(crc);
            if (item.m_Obj != null)
            {
                obj = item.m_Obj as Object;
            }
            else
            {
                obj = LoadAssetByEditor<Object>(path);
            }
        }
#endif

        if (obj == null)
        {
            item = AssetBundleManager.Instance.LoadResourceAssetBundle(crc);
            if (item != null && item.m_AssetBundle != null)
            {
                if (item.m_Obj != null)
                {
                    obj = item.m_Obj as Object;
                }
                else
                {
                    obj = item.m_AssetBundle.LoadAsset<Object>(item.m_AssetName);
                }
            }
        }

        CacheResource(path, ref item, crc, obj);
        resObj.m_ResItem = item;
        item.m_Clear = resObj.m_bClear;
        return resObj;
    }

    /// <summary>
    /// 同步资源加载，外部直接调用，仅加载不需要实例化的资源如Texture，音频等等
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="path"></param>
    /// <returns></returns>
    public T LoadResource<T>(string path) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }
        uint crc = CRC32.GetCRC32(path);
        ResourceItem item = GetCacheResourceItem(crc);
        if (item != null)
        {
            return item.m_Obj as T;
        }

        T obj = null;

#if UNITY_EDITOR
        if (!m_LoadFromAssetBundle)
        {
           
            item = AssetBundleManager.Instance.FindResourceItem(crc);
            if (item.m_Obj != null)
            {
                obj = item.m_Obj as T;
            }
            else
            {
                obj = LoadAssetByEditor<T>(path);
            }
           
        }
#endif

        if (obj == null)
        {
            item = AssetBundleManager.Instance.LoadResourceAssetBundle(crc);
            if (item != null&&item.m_AssetBundle!=null)
            {
                if (item.m_Obj != null)
                {
                    obj = item.m_Obj as T;
                }
                else
                {
                    obj = item.m_AssetBundle.LoadAsset<T>(item.m_AssetName);
                }
            }
        }
        CacheResource(path,ref item,crc,obj);
        return obj;
    }

    /// <summary>
    /// 根据ResourceObj卸载资源
    /// </summary>
    /// <param name="resObj"></param>
    /// <param name="destroyObj"></param>
    /// <returns></returns>
    public bool ReleaseResourece(ResourceObj resObj, bool destroyObj = false)
    {
        if (resObj == null)
        {
            return false;
        }

        ResourceItem item = null;
        if (!m_AssetDic.TryGetValue(resObj.m_Crc, out item) || item == null)
        {
            Debug.LogError("AssetDic里不存在该资源：" + resObj.m_CloneObj.name + " 可能释放了多次");
        }
        GameObject.Destroy(resObj.m_CloneObj);

        item.RefCount--;
        DestroyResourceItem(item, destroyObj);
        return true;
    }

    /// <summary>
    /// 不需要实例化的资源的卸载
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="destroyObj"></param>
    /// <returns></returns>
    public bool ReleaseResourece(Object obj, bool destroyObj=false)
    {
        if (obj == null)
        {
            return false;
        }

        //找到item
        ResourceItem item = null;
        foreach (ResourceItem res in m_AssetDic.Values)
        {
            if (res.m_GUID == obj.GetInstanceID())
            {
                item = res;
            }
        }

        if (item == null)
        {
            Debug.LogError("AssetDic里不存在该资源：" + obj.name + " 可能释放了多次");
            return false;
        }

        item.RefCount--;
        DestroyResourceItem(item, destroyObj);
        return true;
    }



    /// <summary>
    /// 不需要实例化的资源卸载根据路径
    /// </summary>
    /// <param name="path"></param>
    /// <param name="destroyObj"></param>
    /// <returns></returns>
    public bool ReleaseResourece(string path, bool destroyObj = false){
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        uint crc = CRC32.GetCRC32(path);

        //找到item
        ResourceItem item = null;
        if (!m_AssetDic.TryGetValue(crc, out item) || item == null)
        {
            Debug.LogError("AssetDic里不存在该资源：" + path + " 可能释放了多次");
        }

        item.RefCount--;
        DestroyResourceItem(item, destroyObj);
        return false;
    }
    /// <summary>
    /// 缓存加载的资源
    /// </summary>
    /// <param name="path"></param>
    /// <param name="item"></param>
    /// <param name="crc"></param>
    /// <param name="obj"></param>
    /// <param name="addrefcount"></param>
    void CacheResource(string path, ref ResourceItem item, uint crc, Object obj, int addrefcount = 1)
    {
        //缓存太多，清楚最早没有使用的资源
        WashOut();

        if (item == null)
        {
            Debug.LogError("ResourceItem is null,path: " + path);
        }
        if (obj == null)
        {
            Debug.LogError("ResourceLoad Fail: " + path);

        }
        item.m_Obj = obj;
        item.m_GUID = obj.GetInstanceID();
        item.m_LastUseTime = Time.realtimeSinceStartup;
        item.RefCount += addrefcount;
        ResourceItem oldItem = null;
        if (m_AssetDic.TryGetValue(item.m_Crc, out oldItem))
        {
            m_AssetDic[item.m_Crc] = item;
        }
        else
        {
            m_AssetDic.Add(item.m_Crc,item);
        }
    }

    /// <summary>
    /// 缓存太多清除最早没有使用的资源
    /// </summary>
    protected void WashOut()
    {
        //当当前内存使用大于80%进行清除最早没用的资源
        //{
        //    if (m_NoRefrenceAssetMapList.Size() <= 0)
        //    {
        //        break;
        //    }
        //    ResourceItem item = m_NoRefrenceAssetMapList.Back();
        //    DestoryResourceItem(item, true);
        //    m_NoRefrenceAssetMapList.Pop();
        //}
    }

    /// <summary>
    /// 回收一个资源
    /// </summary>
    /// <param name="item"></param>
    /// <param name="destroy"></param>
    protected void DestroyResourceItem(ResourceItem item, bool destroyCache = false)
    {
        if (item == null || item.RefCount > 0)
        {
            return;
        }

        //如果删了还要缓存就加到双向链表去
        if (!destroyCache)
        {
            //放回去之后占内存的obj和assetbundle是不清空的
            m_NoRefrenceAssetMapList.InsertToHead(item);
            return;
        }

        if (!m_AssetDic.Remove(item.m_Crc))
        {
            return;
        }

        //释放assetbundle引用
        AssetBundleManager.Instance.ReleaseAsset(item);
        //清空资源对应的对象池
        ObjectManager.Instance.ClearPoolObject(item.m_Crc);


        if (item.m_Obj != null)
        {
            item.m_Obj = null;
#if UNITY_EDITOR
            Resources.UnloadUnusedAssets();
#endif
        }


    }

    /// <summary>
    /// 从资源池获取缓存资源
    /// </summary>
    /// <param name="crc"></param>
    /// <param name="addrefcount"></param>
    /// <returns></returns>
    ResourceItem GetCacheResourceItem(uint crc,int addrefcount = 1)
    {
        ResourceItem item = null;
        if (m_AssetDic.TryGetValue(crc, out item))
        {
            if (item != null)
            {
                item.RefCount+=addrefcount;
                item.m_LastUseTime = Time.realtimeSinceStartup;

                if (item.RefCount <= 1)
                {
                    m_NoRefrenceAssetMapList.Remove(item);
                }
            }
        }
        return item;
    }

    /// <summary>
    /// 异步加载资源(仅仅是不需要实例化的资源，例如音频，图片等）
    /// </summary>
    public void AsyncLoadResource(string path,OnAsyncObjFinish dealFinish,LoadResPriority priority,uint crc = 0, params object[] param)
    {
        if (crc == 0)
        {
            crc = CRC32.GetCRC32(path);
        }

        ResourceItem item = GetCacheResourceItem(crc);
        if (item != null)
        {
            if (dealFinish != null)
            {
                dealFinish(path, item.m_Obj, param);
            }
            return;
        }

        //判断是否在加载中
        AsyncLoadResParam para = null;
        if (!m_LoadingAssetDic.TryGetValue(crc, out para)||para == null)
        {
            para = m_AsyncLoadResParamPool.Spawn(true);
            para.m_Crc = crc;
            para.m_Path = path;
            para.m_Priority = priority;
            m_LoadingAssetDic.Add(crc, para);
            //异步队列
            m_LoadingAssetList[(int)priority].Add(para);

        }

        //往回调列表里面加回调
        AsyncCallBack callBack = m_AsyncCallBackPool.Spawn(true);
        callBack.m_DealObjFinish = dealFinish;
        callBack.m_Param = param;
        para.m_CallBackList.Add(callBack);
    }

    /// <summary>
    /// 针对Objectmanager的异步加载接口
    /// </summary>
    /// <param name="path"></param>
    /// <param name="resObj"></param>
    /// <param name="dealFinish"></param>
    /// <param name="priority"></param>
    public void AsyncLoadResource(string path,ResourceObj resObj,OnAsyncFinish dealFinish,LoadResPriority priority)
    {
        ResourceItem item = GetCacheResourceItem(resObj.m_Crc);
        //缓存中有
        if (item != null)
        {
            resObj.m_ResItem = item;
            dealFinish?.Invoke(path, resObj);
            return;
        }

        //判断是否在加载中
        AsyncLoadResParam para = null;
        if (!m_LoadingAssetDic.TryGetValue(resObj.m_Crc, out para) || para == null)
        {
            para = m_AsyncLoadResParamPool.Spawn(true);
            para.m_Crc = resObj.m_Crc;
            para.m_Path = path;
            para.m_Priority = priority;
            m_LoadingAssetDic.Add(resObj.m_Crc, para);
            //异步队列
            m_LoadingAssetList[(int)priority].Add(para);
        }


        //往回调列表里面加回调
        AsyncCallBack callBack = m_AsyncCallBackPool.Spawn(true);
        callBack.m_DealFinish = dealFinish;
        callBack.m_ResObj = resObj;
        para.m_CallBackList.Add(callBack);
    }

    /// <summary>
    /// 异步加载
    /// </summary>
    /// <returns></returns>
    IEnumerator AsyncLoadCor()
    {
        List<AsyncCallBack> callBackList = null;
        //上一次yiled的时间
        long lastYiledTime = System.DateTime.Now.Ticks;
        while (true)
        {
            bool haveYield = false;
            

            for (int i = 0; i <(int) LoadResPriority.RES_NUM; i++)
            {
                List<AsyncLoadResParam> loadingList = m_LoadingAssetList[i];
                if (loadingList.Count <= 0)
                {
                    continue;
                }

                AsyncLoadResParam loadingItem = loadingList[0];
                loadingList.RemoveAt(0);
                callBackList = loadingItem.m_CallBackList;
                Object obj = null;
                ResourceItem item = null;
#if UNITY_EDITOR
                if (!m_LoadFromAssetBundle)
                {
                    obj = LoadAssetByEditor<Object>(loadingItem.m_Path);
                    //模拟异步加载
                    yield return new WaitForSeconds(0.5f);
                    item = AssetBundleManager.Instance.FindResourceItem(loadingItem.m_Crc);
                }
#endif
                if (obj == null)
                {
                    item = AssetBundleManager.Instance.LoadResourceAssetBundle(loadingItem.m_Crc);
                    if (item != null&&item.m_AssetBundle!=null)
                    {
                        AssetBundleRequest abRequest = null;
                        if (loadingItem.m_Sprite)
                        {
                            abRequest = item.m_AssetBundle.LoadAssetAsync<Sprite>(item.m_AssetName);
                        }
                        else
                        {
                            abRequest = item.m_AssetBundle.LoadAssetAsync(item.m_AssetName);
                        }
                        
                        yield return abRequest;
                        if (abRequest.isDone)
                        {
                            obj = abRequest.asset;
                        }
                        lastYiledTime = System.DateTime.Now.Ticks;
                    }
                }

                CacheResource(loadingItem.m_Path, ref item, loadingItem.m_Crc, obj, callBackList.Count);

                for (int j = 0; j < callBackList.Count; j++)
                {
                    AsyncCallBack callBack = callBackList[j];

                    if (callBack != null && callBack.m_DealFinish != null && callBack.m_ResObj != null)
                    {
                        ResourceObj tempResObj = callBack.m_ResObj;
                        tempResObj.m_ResItem = item;
                        callBack.m_DealFinish(loadingItem.m_Path, tempResObj, tempResObj.m_Param);
                        callBack.m_DealFinish = null;
                        tempResObj = null;
                    }

                    if (callBack != null && callBack.m_DealObjFinish != null)
                    {
                        callBack.m_DealObjFinish(loadingItem.m_Path, obj,callBack.m_Param);
                        callBack.m_DealObjFinish = null;
                    }

                    callBack.Reset();
                    m_AsyncCallBackPool.Recycle(callBack);
                }

                obj = null;
                callBackList.Clear();
                m_LoadingAssetDic.Remove(loadingItem.m_Crc);

                loadingItem.Reset();
                m_AsyncLoadResParamPool.Recycle(loadingItem);

                if (System.DateTime.Now.Ticks - lastYiledTime > MAXLOADRESTIME)
                {
                    yield return null;
                    lastYiledTime = System.DateTime.Now.Ticks;
                    haveYield = true;
                }
            }

            if (!haveYield || System.DateTime.Now.Ticks - lastYiledTime > MAXLOADRESTIME)
            {
                lastYiledTime = System.DateTime.Now.Ticks;
                yield return null;
            }
        }
    }
}

//双向链表，经常用的在头，不常用的就到底，清缓存的时候从后往前清
public class DoubleLinkListNode<T> where T : class, new()
{
    //前一个节点
    public DoubleLinkListNode<T> prev = null;
    //后一个节点
    public DoubleLinkListNode<T> next = null;
    //当前节点
    public T t = null;

}

//双向链表结构
public class DoubleLinkList<T> where T : class, new()
{
    //表头
    public DoubleLinkListNode<T> Head = null;

    //表尾
    public DoubleLinkListNode<T> Tail = null;

    //双向链表结构类对象池
    protected ClassObjectPool<DoubleLinkListNode<T>> m_DoubleLinkNodePool = ObjectManager.Instance.GetOrCreatClassPool<DoubleLinkListNode<T>>(500);

    public int m_Count = 0;
    public int Count
    {
        get
        {
            return m_Count;
        }
    }

    /// <summary>
    /// 添加一个节点到头部
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public DoubleLinkListNode<T> AddToHeader(T t)
    {
        DoubleLinkListNode<T> pList = m_DoubleLinkNodePool.Spawn(true);
        pList.next = null;
        pList.prev = null;
        pList.t = t;
        return AddToHeader(pList);
    }

    /// <summary>
    /// 添加一个节点到头部
    /// </summary>
    /// <param name="pNode"></param>
    /// <returns></returns>
    public DoubleLinkListNode<T> AddToHeader(DoubleLinkListNode<T> pNode)
    {
        if (pNode == null)
        {
            return null;
        }

        pNode.prev = null;
        if (Head == null)
        {
            Head = Tail = pNode;
        }
        else
        {
            pNode.next = Head;
            Head.prev = pNode;
            Head = pNode;
        }

        m_Count++;
        return Head;
    }
    /// <summary>
    /// 添加节点到尾部
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public DoubleLinkListNode<T> AddToTail(T t)
    {
        DoubleLinkListNode<T> pList = m_DoubleLinkNodePool.Spawn(true);
        pList.next = null;
        pList.prev = null;
        pList.t = t;
        return AddToTail(pList);
    }
    /// <summary>
    /// 添加节点到尾部
    /// </summary>
    /// <param name="pNode"></param>
    /// <returns></returns>
    public DoubleLinkListNode<T> AddToTail(DoubleLinkListNode<T> pNode)
    {
        if (pNode == null)
        {
            return null;
        }

        pNode.next = null;
        if (Tail == null)
        {
            Head = Tail = pNode;
        }
        else
        {
            pNode.prev = Tail;
            Tail.next = pNode;
            Tail = pNode;
        }
        m_Count++;
        return Tail;
    }

    /// <summary>
    /// 移除节点
    /// </summary>
    /// <param name="pNode"></param>
    public void RemoveNode(DoubleLinkListNode<T> pNode)
    {
        if (pNode == null)
        {
            return;
        }

        if (pNode == Head)
        {
            Head = pNode.next;
        }

        if (pNode == Tail)
        {
            Tail = pNode.prev;
        }

        if (pNode.prev != null)
        {
            pNode.prev.next = pNode.next;
        }
        if (pNode.next != null)
        {
            pNode.next.prev = pNode.prev;
        }

        pNode.next = pNode.prev = null;
        pNode.t = null;
        m_DoubleLinkNodePool.Recycle(pNode);
        m_Count--;
    }

    /// <summary>
    /// 把某个节点移动到头部
    /// </summary>
    /// <param name="pNode"></param>
    public void MoveToHead(DoubleLinkListNode<T> pNode)
    {
        if (pNode == null || pNode == Head)
        {
            return;
        }

        
        if (pNode.prev == null && pNode.next == null)
        {
            return;
        }

        if (pNode == Tail)
        {
            Tail = pNode.prev;
        }


        if (pNode.prev != null)
        {
            pNode.prev.next = pNode.next;
        }

        if (pNode.next != null)
        {
            pNode.next.prev = pNode.prev;
        }

        pNode.prev = null;
        pNode.next = Head;
        Head.prev = pNode;
        Head = pNode;
        if (Tail == null)
        {
            Tail = Head;
        }

    }
}

public class CMapList<T> where T : class, new()
{
    DoubleLinkList<T> m_DLink = new DoubleLinkList<T>();
    Dictionary<T, DoubleLinkListNode<T>> m_FindMap = new Dictionary<T, DoubleLinkListNode<T>>();

    ~CMapList()
    {
        Clear();
    }

    /// <summary>
    /// 清空列表
    /// </summary>
    public void Clear()
    {
        while (m_DLink.Tail != null)
        {
            Remove(m_DLink.Tail.t);
        }
    }

    /// <summary>
    /// 插入一个节点到表头
    /// </summary>
    /// <param name="t"></param>
    public void InsertToHead(T t)
    {
        DoubleLinkListNode<T> node = null;
        if (m_FindMap.TryGetValue(t, out node) && node != null)
        {
            m_DLink.AddToHeader(node);
            return;
        }
        m_DLink.AddToHeader(t);
        m_FindMap.Add(t, m_DLink.Head);
    }

    /// <summary>
    /// 从表尾弹出节点
    /// </summary>
    public void Pop()
    {
        if (m_DLink.Tail != null)
        {
            Remove(m_DLink.Tail.t);
        }
    }

    public void Remove(T t)
    {
        DoubleLinkListNode<T> node = null;
        if (!m_FindMap.TryGetValue(t, out node)||node==null)
        {
            return;
        }
        m_DLink.RemoveNode(node);
        m_FindMap.Remove(t);
    }

    /// <summary>
    /// 获取尾部节点
    /// </summary>
    /// <returns></returns>
    public T Back()
    {
        return m_DLink.Tail == null ? null : m_DLink.Tail.t;
    }

    /// <summary>
    /// 返回节点个数
    /// </summary>
    /// <returns></returns>
    public int Size()
    {
        return m_FindMap.Count;
    }

    /// <summary>
    /// 查找是否存在该节点
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public bool Find(T t)
    {
        DoubleLinkListNode<T> node = null;
        if (!m_FindMap.TryGetValue(t, out node) || node == null)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    /// <summary>
    /// 刷新某个节点把节点移动到头部
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public bool Refresh(T t)
    {
        DoubleLinkListNode<T> node = null;
        if (!m_FindMap.TryGetValue(t, out node) || node == null)
        {
            return false;
        }
        m_DLink.MoveToHead(node);
        return true;
    }
}