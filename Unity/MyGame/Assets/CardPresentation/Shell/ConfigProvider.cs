// ConfigProvider.cs — 原版 `Everguild.LiveOps.Config.ConfigProvider<T>`（泛型配置基类）的**本地等价**
//
// ============================ 这是什么、判据在哪 ============================
// 🔴 原版这一层是**纯本地**的（**不是服务端**）—— 真体逐句（产物 `D:/2/tools/decomp_generic_trial/out_b94/`
//    `ConfigProvider_object_.{Initialize__16826b0,UnpackInternal__1682730,OnDestroy__16826e0,get_Key__1682930}.c`
//    + ILSpy `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/Everguild.LiveOps.Config/ConfigProvider.cs`）：
//
//      Initialize(Dictionary<string,string> titleData)
//          = `config = UnpackInternal(titleData);`                       // 一行转调（ILSpy `Initialize`）
//
//      protected virtual T UnpackInternal(Dictionary<string,string> titleData)
//          = `config = ReflectionHelper.New<T>();`                        // ★ 先 new 一个默认实例
//            `if (titleData != null) {`
//              `  string key = this.Key;`                                 // ★ get_Key = new T() 后读 IConfigData.ConfigKey
//              `  if (titleData.TryGetValue(key, out string json)) {`
//              `    config = JsonWrapper.DeserializeObjectWithType<T>(json);` // ★ 反序列化覆盖
//              `    <cache>.Write(1);`                                    // ★ 缓存写（FUN_180002340 slot 1）
//              `    return config; } }`
//            `Debug.Log("Couldn't find Config of type " + typeof(T).ToString());` // ★ 未命中：只打一行日志
//            `return config;`
//
//      protected virtual void OnDestroy()
//          = `if (config != null) <cache>.Clear(2);`                      // ★ 有解过配置才清缓存（slot 2）
//
//      Key       = `new T().ConfigKey;`
//      Config    = `config;`            ConfigType = `typeof(T);`
//
//    ⇒ 🔴 **旧账把 `UnpackInternal` / `OnDestroy` 判成「服务端 D」是错的**：全程本地、**零后端调用**
//      （`Debug.Log` 之外没有任何外部依赖）。本条按真体重判为「可本地复刻（B）」（铁律 5 就地订正）。
//    ⇒ 上游 `ConfigManager.Unpack(titleData)` 遍历子 `IConfigProvider`、各调一次 `Initialize(titleData)`，
//      再 `Configs[provider.ConfigType] = provider`；`ConfigManager.GetConfig<T>()` 从那张 `Configs` 表取。
//
// ⚠️ 本地**没有** `JsonWrapper`（Newtonsoft 多态反序列化）⇒ `DeserializeObjectWithType<T>` 用
//    `UnityEngine.JsonUtility.FromJson<T>` 顶（T 必须是 `[Serializable]` 类）；这不是原版的实现，是本地替身。
// 🔶 自己设计（非原版）：原版 `UnpackInternal` 成功那一跳与 `OnDestroy` 那一跳背后是 IL2CPP 泛型缓存助手
//    `FUN_180002340(slot 1 / slot 2, <classRef>)`，**宿主类未解**（R-M §四 G4）⇒ 用本地静态表 `ConfigCache` 顶
//    （键 = `typeof(T)`）；语义 = 成功解出就登记、销毁时若解过就摘掉。调度台 Y6 2026-10-29 定。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `Everguild.LiveOps.Config.IConfigData`（ILSpy 真身同形）：
    /// `ConfigKey` 是唯一的数据成员；`AfterUnpack` / `CleanUp` 在接口上**就是空默认实现**（行为零）。</summary>
    public interface IConfigData
    {
        /// <summary>= 原版 `IConfigData.ConfigKey`（`ConfigProvider.Key` 读的就是它）。</summary>
        string ConfigKey { get; }

        /// <summary>原版接口默认实现 = 空体（`ConfigProvider.UnpackInternal` 真体里**没有**调它 ⇒ 行为零）。</summary>
        void AfterUnpack() { }

        /// <summary>原版接口默认实现 = 空体（同上）。</summary>
        void CleanUp() { }
    }

    /// <summary>原版 `Everguild.LiveOps.Config.IConfigProvider`（ILSpy 真身同形）。</summary>
    public interface IConfigProvider
    {
        string Key { get; }
        IConfigData Config { get; }
        Type ConfigType { get; }
        void Initialize(Dictionary<string, string> titleData);
    }

    /// <summary>🔶 自己设计（非原版）：`ConfigProvider` 内部那本「已解出的配置」缓存。
    /// 原版是 IL2CPP 泛型缓存助手 `FUN_180002340(slot 1/2, ...)`，**宿主类未解**（R-M §四 G4）⇒
    /// 用一张静态表顶替（键 = `typeof(T)`）。三档语义照真体：`Write`（`UnpackInternal` 命中时登记）·
    /// `Clear`（`OnDestroy` 时若解过就摘）· `Get`（= `ConfigManager.GetConfig<T>()` 的本地等价，供消费者读回）。
    /// 调度台 Y6 2026-10-29 定。</summary>
    public static class ConfigCache
    {
        static readonly Dictionary<Type, IConfigProvider> _byType = new Dictionary<Type, IConfigProvider>();

        /// <summary>原版 `UnpackInternal` 里 `<cache>.Write(1)` 的本地等价。</summary>
        public static void Write(IConfigProvider provider)
        {
            if (provider != null && provider.ConfigType != null) _byType[provider.ConfigType] = provider;
        }

        /// <summary>原版 `OnDestroy` 里 `<cache>.Clear(2)` 的本地等价。</summary>
        public static void Clear(Type configType)
        {
            if (configType != null) _byType.Remove(configType);
        }

        /// <summary>= `ConfigManager.GetConfig&lt;T&gt;()`（从 `Configs` 表按 `typeof(T)` 取，取不到返回 null + 出声）。</summary>
        public static T Get<T>() where T : class, IConfigData
        {
            IConfigProvider p;
            if (_byType.TryGetValue(typeof(T), out p) && p != null) return p.Config as T;
            return null;
        }

        /// <summary>自检用：清空缓存表。</summary>
        public static void ResetForTest() { _byType.Clear(); }
    }

    /// <summary>原版 `Everguild.LiveOps.Config.ConfigProvider&lt;T&gt; : MonoBehaviour, IConfigProvider`
    /// （约束 `where T : class, IConfigData`）。真体见文件头。</summary>
    public abstract class ConfigProvider<T> : MonoBehaviour, IConfigProvider
        where T : class, IConfigData
    {
        /// <summary>原版 `protected T config;`（`+0x20`）。</summary>
        protected T config;

        /// <summary>原版 `public string Key`（`get_Key__1682930.c`）：`new T()` 后读 `IConfigData.ConfigKey`。
        /// ⚠️ 原版**每次都现造一个 T**（真体 `get_Key` 里 `ReflectionHelper.New&lt;T&gt;()`）—— 这里照抄。</summary>
        public string Key
        {
            get
            {
                var n = NewT();
                return n == null ? null : n.ConfigKey;
            }
        }

        /// <summary>原版 `public virtual IConfigData Config =&gt; config;`。</summary>
        public virtual IConfigData Config { get { return config; } }

        /// <summary>原版 `public Type ConfigType =&gt; typeof(T);`。</summary>
        public Type ConfigType { get { return typeof(T); } }

        /// <summary>原版 `public virtual void Initialize(Dictionary&lt;string,string&gt; titleData)`
        /// （`Initialize__16826b0.c`，全文 = 一行转调）：`config = UnpackInternal(titleData);`。</summary>
        public virtual void Initialize(Dictionary<string, string> titleData)
        {
            config = UnpackInternal(titleData);
        }

        /// <summary>原版 `protected virtual T UnpackInternal(Dictionary&lt;string,string&gt; titleData)`
        /// （`UnpackInternal__1682730.c`）：new 默认实例 → 按 `Key` 取 JSON → 反序列化覆盖 → 写缓存 → 返回；
        /// 取不到 ⇒ `Debug.Log("Couldn't find Config of type " + typeof(T))` + 返回默认实例。**纯本地**。</summary>
        protected virtual T UnpackInternal(Dictionary<string, string> titleData)
        {
            config = NewT();
            if (titleData != null)
            {
                string key = Key;
                string json;
                if (key != null && titleData.TryGetValue(key, out json))
                {
                    // 原版 = `JsonWrapper.DeserializeObjectWithType<T>(json)`（Newtonsoft 多态）⇒
                    // 本地用 `JsonUtility.FromJson<T>` 顶（⚠️ 差异：T 必须是 [Serializable] 类）。
                    config = string.IsNullOrEmpty(json) ? NewT() : JsonUtility.FromJson<T>(json);
                    ConfigCache.Write(this);
                    return config;
                }
            }
            Debug.Log("Couldn't find Config of type " + typeof(T).ToString());
            return config;
        }

        /// <summary>原版 `protected virtual void OnDestroy()`（`OnDestroy__16826e0.c`）：`config != null` ⇒ 清缓存。</summary>
        protected virtual void OnDestroy()
        {
            if (config != null) ConfigCache.Clear(typeof(T));
        }

        /// <summary>原版 `ReflectionHelper.New&lt;T&gt;()` 的本地等价（`Activator.CreateInstance&lt;T&gt;()`）。
        /// ⚠️ T 需要有公开无参构造；没有 ⇒ 运行时抛（与原版反射造实例同一条语义）。</summary>
        static T NewT()
        {
            return Activator.CreateInstance<T>();
        }
    }
}
