# AssetbundleConfig
assetbundle打包方案

打包策略：
设置编辑器工具统一设置AB包名及路径管理
根据依赖关系生成不冗余AB包
根据基于Asset的全路径生成自己的依赖关系表
根据自己的依赖关系表加载AB包，编辑器下直接加载资源
不需要在打包编辑器可以直接运行游戏；不会产生冗余AB包；文件夹或文件AB包设置简单方便容易管理。
长时间未打包的情况下，打包的时候时间较长。

生成AB包：
1，根据单个文件和文件夹设置AB包
2，剔除冗余AB包
3，生成AB包配置表

AssetBundleManager：
1：读取AssetBundle配置表
2：设置中间类进行引用计数
3：根据路径加载AssetBundle
4：根据路径卸载AssetBundle
5：为ResourceManager提供加载中间类，根据中间类释放资源，查找中间类等方法





类对象池，资源池，对象池，

