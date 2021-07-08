using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ResourceManager : Singleton<ResourceManager>
{
    public bool m_LoadFromAssetBundle = false;
    //缓存使用的资源列表
    public Dictionary<uint, ResourceItem> m_AssetDic { get; set; } = new Dictionary<uint, ResourceItem>();
    //缓存引用计数为0的资源列表，达到缓存最大的时候，释放这个列表里面最早没用的资源
    protected CMapList<ResourceItem> m_NoRefrenceAssetMapList = new CMapList<ResourceItem>();
#if UNITY_EDITOR
    protected T LoadAssetByEditor<T>(string path) where T : UnityEngine.Object
    {
        return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
    }
#endif

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
            obj = LoadAssetByEditor<T>(path);
            item = AssetBundleManager.Instance.FindResourceItem(crc);
        }
#endif

        if (obj == null)
        {
            item = AssetBundleManager.Instance.LoadResourceAssetBundle(crc);
            if (item != null&&item.m_AssetBundle!=null)
            {
                obj = item.m_AssetBundle.LoadAsset<T>(item.m_AssetName);
            }
        }
        CacheResource(path,ref item,crc,obj);
        return obj;
    }

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
    protected void DestoryResourceItem(ResourceItem item, bool destroyCache = false)
    {
        if (item == null || item.RefCount > 0)
        {
            return;
        }

        //如果删了还要缓存就加到双向链表去
        if (!destroyCache)
        {
            m_NoRefrenceAssetMapList.InsertToHead(item);
            return;
        }

        //如果不缓存，先判断有没有，有的话就走下面释放没有就return掉
        if (!m_AssetDic.Remove(item.m_Crc))
        {
            return;
        }

        //释放assetbundle引用
        AssetBundleManager.Instance.ReleaseAsset(item);

        if (item.m_Obj != null)
        {
            item.m_Obj = null;
        }

    }

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