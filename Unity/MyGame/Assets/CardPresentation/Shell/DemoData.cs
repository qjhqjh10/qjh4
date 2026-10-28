// DemoData.cs — 原版 `DemoData`（教程/演示进度实体）＋ 它的落盘桥。
// 账 `其他_基础设施` 497（`SetTutorialState`）/ 511（`TestResetTutorial`）/ 348（`AllTutorialsCompleted`）/
// 379（`FirstTwoTutorialCompleted`）四条的共同落点。
//
// ============================ 判据（一手） ============================
//  · 字段表 = `cpp2il/out_cpp2il_dev_ilspy/DemoData.cs:6-30`（**10 个 public 字段**，本文件逐字照抄名字与序）：
//      `Dictionary<string,bool> tutorialsCompleted` · `bool gameAfterTutorialPlayed` · `bool suscriptionPopupShowed`
//      · `bool suscribed` · `string currentDeck` · `bool canEnterDraftMode`
//      · `LoadableReference<CosmeticItem> playerTitle` · `bool feedbackSeen` · `bool feedbackSent`
//  · `DemoData__.ctor.c` = `tutorialsCompleted = new Dictionary<string,bool>()`（其余取零值）。
//  · `DemoData__IsTutorialCompleted.c` = `tutorialsCompleted.ContainsKey(index.ToString())` ⇒ 返回其 bool
//    （字典为 null ⇒ 走 NRE 帮助函数 `FUN_1803f47a0`）。
//  · `DemoData__ResetTutorial.c` = **换一张新字典** + `gameAfterTutorialPlayed = false`。
//  · `DemoData__ToggleCanEnterDraftMode.c` = `canEnterDraftMode = option;`（一句）。
//  · 落盘 key = `SaveKeys.DemoData`（原版 `PlayerDataKey.DemoData` = 第 15 条）。
//
// ============================ 落盘形态（调度台 2026-10-29 裁定） ============================
// 🔶 自己设计（非原版）：DemoData 落盘形态 · 原版 `BinaryFormatter` 直接序列化 `Dictionary<string,bool>`，
//    而本仓存档层 `SaveCenter` 走 `JsonUtility` —— **JsonUtility 不支持 Dictionary**（会静默写成 `{}`）
//    ⇒ 用「键值对数组」`DemoDataEntry[] { key, value }` 桥接，其余 9 格字段名与类型照原版。
//    · 理由：本地存档是**我们自己的体系**（不是服务器），这只是**存档实现的差异**，等价 DTO 桥接即可；
//      改动面只有本文件的 `ToFile/FromFile` 两个方向。
//    · 调度台 2026-10-29 裁定。
//  · ⚠️ 唯一的非逐字格：原版 `playerTitle` 是 `LoadableReference<CosmeticItem>`（可空资产引用）。
//    本仓没有 `CosmeticItem` 这个类型 ⇒ 这一格落成 **`string playerTitle`**（= 引用的 targetId 串），
//    反序列化时旧档里没有这一格 ⇒ null。⚠️ 本格**今天没有读者**（原版改头衔的那条链我们没建）⇒ 只做原样保存。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>落盘用的键值对（见文件头「落盘形态」）。<c>struct</c>，JsonUtility 对 struct 数组可序列化。</summary>
    [Serializable]
    public struct DemoDataEntry
    {
        public string key;
        public bool value;
    }

    /// <summary>原版 `DemoData`（运行时形态，字段名/序照原版）。
    /// <para>🔴 `tutorialsCompleted` 是 <see cref="Dictionary{TKey,TValue}"/>（原版就是这个类型）——
    /// 它**不进 JsonUtility**，落盘走 <see cref="DemoDataStore"/> 的 `ToFile/FromFile`。</para></summary>
    [Serializable]
    public class DemoData
    {
        public Dictionary<string, bool> tutorialsCompleted = new Dictionary<string, bool>();
        public bool gameAfterTutorialPlayed;
        public bool suscriptionPopupShowed;
        public bool suscribed;
        public string currentDeck;
        public bool canEnterDraftMode;
        /// <summary>= 原版 `LoadableReference&lt;CosmeticItem&gt; playerTitle` 的等价物（存 targetId 串；见文件头）。
        /// ⚠️ 今天**没有读者**（改头衔那条链没建）。</summary>
        public string playerTitle;
        public bool feedbackSeen;
        public bool feedbackSent;

        /// <summary>= 原版 `DemoData.IsTutorialCompleted(int)`：键 = `index.ToString()`（**原版就是按下标当键**，
        /// 不是 stageId —— 见文件头判据）。字典为 null ⇒ 原版走 NRE；本仓返回 false（红线：不崩宿主）。</summary>
        public bool IsTutorialCompleted(int tutorialIndex)
        {
            if (tutorialsCompleted == null) return false;
            bool v;
            return tutorialsCompleted.TryGetValue(tutorialIndex.ToString(), out v) && v;
        }

        /// <summary>= 原版 `DemoData.ResetTutorial()`：**换一张新字典** + `gameAfterTutorialPlayed = false`。
        /// ⚠️ 其余 8 格**不动**（原版 `.c` 只写这两句 —— ⛔ 别「顺手」清干净）。</summary>
        public void ResetTutorial()
        {
            tutorialsCompleted = new Dictionary<string, bool>();
            gameAfterTutorialPlayed = false;
        }

        /// <summary>= 原版 `DemoData.ToggleCanEnterDraftMode(bool)`：`canEnterDraftMode = option;`（一句）。</summary>
        public void ToggleCanEnterDraftMode(bool option) { canEnterDraftMode = option; }
    }

    /// <summary>落盘载体（= `DemoData` 的 JsonUtility 可序列化形状，见文件头「落盘形态」）。</summary>
    [Serializable]
    public class DemoDataFile
    {
        public DemoDataEntry[] tutorialsCompleted;
        public bool gameAfterTutorialPlayed;
        public bool suscriptionPopupShowed;
        public bool suscribed;
        public string currentDeck;
        public bool canEnterDraftMode;
        public string playerTitle;
        public bool feedbackSeen;
        public bool feedbackSent;
    }

    /// <summary>`DemoData` 的**唯一读写口**（⛔ 别在别处直接 `SaveCenter.Set(SaveKeys.DemoData, …)` ——
    /// 两份写同一个 key 会互相清空对方的格子，`PracticeModePopup` 那条注释警告的就是这件事）。</summary>
    public static class DemoDataStore
    {
        /// <summary>上一次失败的人话（成功时是空串）。</summary>
        public static string LastError { get; private set; } = "";

        static DemoData _current;
        static bool _loaded;

        /// <summary>当前的内存态（第一次读时从 `SaveKeys.DemoData` 载入；缺档 ⇒ 出厂值 + 空字典）。</summary>
        public static DemoData Current
        {
            get { Load(); return _current; }
        }

        /// <summary>从存档重新载入（= 原版 `PlayerDataManager.demoData` 的赋值那一跳）。</summary>
        public static DemoData Load()
        {
            if (_loaded && _current != null) return _current;
            var f = SaveCenter.Get<DemoDataFile>(SaveKeys.DemoData);
            _current = FromFile(f);
            _loaded = true;
            return _current;
        }

        /// <summary>落盘（= 原版把 `demoData` 写回存档那一跳）。返回是否**真的**写成功。</summary>
        public static bool Save()
        {
            LastError = "";
            Load();
            bool ok = SaveCenter.Set(SaveKeys.DemoData, ToFile(_current));
            if (!ok)
            {
                LastError = "没写进存档：" + (SaveCenter.LastWriteError ?? "（`SaveCenter` 没给原因）");
                Debug.LogWarning("[DemoData] " + LastError);
            }
            return ok;
        }

        /// <summary>读-改-写（**所有写入方都走这里**，⛔ 别再各写各的）。</summary>
        public static bool Mutate(Action<DemoData> change)
        {
            Load();
            if (change != null) change(_current);
            return Save();
        }

        // ---------------------------------------------------------------- 教程完成位（账 348/379/497/511 用）
        /// <summary>查一个教程键的完成位。**找不到 ⇒ 返回 false 且 <paramref name="found"/> = false**
        /// （调用方据此决定要不要去读旧档兜底 —— 迁移期用，见 `MenuTutorialFlow.IsCompleted`）。</summary>
        public static bool TryGetTutorial(string key, out bool found)
        {
            Load();
            found = false;
            if (string.IsNullOrEmpty(key) || _current.tutorialsCompleted == null) return false;
            bool v;
            if (!_current.tutorialsCompleted.TryGetValue(key, out v)) return false;
            found = true;
            return v;
        }

        /// <summary>写一个教程完成位（`true` 才写进字典；原版的字典是「完成过的那些 key」）。</summary>
        public static bool SetTutorial(string key, bool value)
        {
            if (string.IsNullOrEmpty(key)) return false;
            return Mutate(d =>
            {
                if (d.tutorialsCompleted == null) d.tutorialsCompleted = new Dictionary<string, bool>();
                d.tutorialsCompleted[key] = value;
            });
        }

        /// <summary>= 原版 `PlayerDataManager.TestResetTutorial()` 里的 `DemoData.ResetTutorial(demoData)` 那一跳
        /// （换新字典 + `gameAfterTutorialPlayed = false`）。⚠️ 原版另一半 `UploadOnlineDemoData()` = **P3**（不联网，出声）。</summary>
        public static bool ResetTutorial()
        {
            bool ok = Mutate(d => d.ResetTutorial());
            Debug.Log("[DemoData] `ResetTutorial()`：换新字典 + `gameAfterTutorialPlayed = false`"
                    + "（原版 `DemoData__ResetTutorial.c` 两句；另一半 `UploadOnlineDemoData` = P3，本地不上报）");
            return ok;
        }

        // ---------------------------------------------------------------- 形态转换（见文件头「落盘形态」）
        public static DemoDataFile ToFile(DemoData d)
        {
            if (d == null) return new DemoDataFile();
            DemoDataEntry[] arr = null;
            if (d.tutorialsCompleted != null && d.tutorialsCompleted.Count > 0)
            {
                arr = new DemoDataEntry[d.tutorialsCompleted.Count];
                int i = 0;
                foreach (var kv in d.tutorialsCompleted)
                    arr[i++] = new DemoDataEntry { key = kv.Key, value = kv.Value };
            }
            return new DemoDataFile
            {
                tutorialsCompleted = arr,
                gameAfterTutorialPlayed = d.gameAfterTutorialPlayed,
                suscriptionPopupShowed = d.suscriptionPopupShowed,
                suscribed = d.suscribed,
                currentDeck = d.currentDeck,
                canEnterDraftMode = d.canEnterDraftMode,
                playerTitle = d.playerTitle,
                feedbackSeen = d.feedbackSeen,
                feedbackSent = d.feedbackSent,
            };
        }

        public static DemoData FromFile(DemoDataFile f)
        {
            var d = new DemoData();
            if (f == null) return d;
            if (f.tutorialsCompleted != null)
            {
                for (int i = 0; i < f.tutorialsCompleted.Length; i++)
                {
                    var e = f.tutorialsCompleted[i];
                    if (string.IsNullOrEmpty(e.key)) continue;
                    d.tutorialsCompleted[e.key] = e.value;
                }
            }
            d.gameAfterTutorialPlayed = f.gameAfterTutorialPlayed;
            d.suscriptionPopupShowed = f.suscriptionPopupShowed;
            d.suscribed = f.suscribed;
            d.currentDeck = f.currentDeck;
            d.canEnterDraftMode = f.canEnterDraftMode;
            d.playerTitle = f.playerTitle;
            d.feedbackSeen = f.feedbackSeen;
            d.feedbackSent = f.feedbackSent;
            return d;
        }

        /// <summary>自检用：丢掉进程内状态（⛔ 不动 `SaveCenter` 里的档）。</summary>
        public static void ResetForTest()
        {
            _current = null;
            _loaded = false;
            LastError = "";
        }
    }
}
