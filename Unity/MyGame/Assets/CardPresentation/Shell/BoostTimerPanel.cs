// BoostTimerPanel.cs — 原版 `BoostTimerPanel`（加成倒计时面板）的**离线等价物**。
//
// ============================ 判据（唯一，别去别的文档找第二份）============================
//   · 类/字段：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/BoostTimerPanel.cs`
//       `public TextMeshPro BoostDescText; BoostTimerText; BoostItem;`
//       `SetDescription(text) => BoostDescText.text = text;`
//       `SetTimer(text)       => BoostTimerText.text = text;`
//       `Init(BoostItem)      => BoostDescText.text = "<Type> Boost <Percent>%"; UpdateTimer();`
//   · 方法体（**这是唯一的格式判据**）：`d:/2/tools/decomp_full/BoostTimerPanel__UpdateTimer.c`
//     ＋ `BoostTimerPanel__Init.c` / `__SetDescription.c` / `__SetTimer.c`（三份都是单句赋值，与上面逐字一致）。
//   · `BoostItem`：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/BoostItem.cs`
//       `enum BoostType { XP, Gold }` · `DateTime ExpirationTime` · `int PercentBoost` ·
//       `BoostType Type` · `string PlayfabItemID` · `int PlayfabHoursDuration`（继承 `PlayerItem`）。
//
// 🔴 **`UpdateTimer` 的三档不是「mm:ss」，是「00:mm:ss」** —— 施工单/账里那句
//    「≥4 天 ⇒ `n d` · ≥1 小时 ⇒ `hh:mm:ss` · 否则 `mm:ss`」是**概述**；逐句读 `.c` 得到的是：
//      ① `TotalHours < 1`：
//           `TotalSeconds <= 0` ⇒ `Format("{0}:{1}", "00", "00")` = **`00:00`**，**返回 true（= 已过期）**；
//           否则 ⇒ `Format("{0}:{1}:{2}", Hours:00, Minutes:00, Seconds:00)` = **`00:mm:ss`**
//                  （`Hours` 取自 `TimeSpan.Hours`，这一段恒 0）⇒ **短于一小时也带小时位**。
//      ② `TotalHours >= 1` 且 `Days < 4` ⇒ `floor((float)TotalHours)` 当小时 + `mm:ss` ⇒ `hh:mm:ss`。
//      ③ `Days >= 4` ⇒ `floor((float)TotalDays).ToString() + " d"`（**天位不补零**，如 `5 d`）。
//     ⚠️ `floor` 之前**先转 `float`**（`.c` 里是 `cvtsd2ss` → `floor`）；照抄这个转换，别直接用 `double`。
//     ⚠️ 其它三档**返回 false**；返回值语义 = **「这条加成到期了，调用方应把它摘掉」**
//        （`BoostMenu__UpdateAllPanelsTimers.c`：拿到 true 的进 `list`，随后 `_boostPanels.Remove` +
//         `Destroy(panel.gameObject)`）。
//
// ============================ 数据源与宿主（都不在本地，已如实记账）============================
//   · **加成条目来自服务器**：`BoostManager.InitalizeBoosterCollection()` 读 `PlayerDataManager.allBoosts`
//     （PlayFab 清单），`BoostItem.PlayfabItemID` / `PlayfabHoursDuration` 都是 PlayFab 商品字段。
//     本机没有账户目录（原版已关服）⇒ `BoostStore.Active` **恒空**，并且**每次进程第一次问就出声一次**。
//     ⛔ 不编造「默认送 X% 加成」——用户边界②「不做真实经济」。
//   · **宿主 prefab 本地不存在**：`BoostMenu` 在 `d:/2/新解包资源/assets_full` 全库只有一条命中
//     （`globalgamemanagers/MonoScript/MonoScript_2818.json` = 类资产本身），**没有任何 prefab 引用它**
//     ⇒ `AllBoostersPanel` / `BoosterPanelPrefab` 的矩形、间距（`PanelSeparation = 30`）、
//       `infoText` 那一格**都拿不到**。
//     ⇒ 本件**只建面板本体（描述一格 + 倒计时一格）**，几何由宿主传进来（`Build` 的 `descRect`/`timerRect`）；
//        **BoostMenu 那一层（逐件排布）没有宿主几何 ⇒ 未建**（写在报告「没查清」里，⛔ 不拿 30px 去凑）。
//   · 可玩性：13 份活动 SO 全过期、无加成条目 ⇒ 这条链今天**休眠**，但逻辑是活的（自检可直接喂 `BoostItem`）。
//
// ============================ 与 `BoostManager.UpdateTimer` 的区别（⛔ 别合并）============================
//   `BoostManager` 自己也有一份 `UpdateTimer`（= 顶栏那颗「最近到期」倒计时），它的格式**与这里不同**：
//   `TotalHours >= 1` 那支是 `hh:mm`（**没有秒**）、`< 1h` 那支才是 `mm:ss`、过期则 `SetActive(false)` +
//   `CancelInvoke`。判据 = `cpp2il/out_cpp2il_dev_ilspy/BoostManager.cs` 的 `UpdateTimer`。
//   ⇒ **两处写同一条规则会漂**：本件只服务 `BoostTimerPanel`；`BoostManager` 那一份**尚未复刻**（记在报告里）。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `BoostItem`（`BoostItem.cs`）的离线等价物。
    /// ⚠️ 原版继承 `PlayerItem`（PlayFab 物品基类，字段：`PlayfabItemID` 等）—— 本机没有那套物品层
    /// ⇒ 这里**只抄本类自己声明的 5 个字段**，不伪造基类。判据 = `BoostItem.cs` 的字段表。</summary>
    public sealed class BoostItem
    {
        /// <summary>⚠️ **枚举序照抄原版**（`XP = 0` · `Gold = 1`）—— `Init` 的描述串就取这个名字。</summary>
        public enum BoostType { XP = 0, Gold = 1 }

        /// <summary>到期时刻（**UTC**）。判据：`UpdateTimer` 用 `DateTime.UtcNow` 减它。</summary>
        public DateTime ExpirationTime;
        /// <summary>加成百分比（原版 `PercentBoost`）；`Init` 拼成 `"<Type> Boost <n>%"`。</summary>
        public int PercentBoost;
        public BoostType Type;
        /// <summary>PlayFab 商品 id（原版 `PlayfabItemID`；`BoostCollection.GetBoostFromId` 按它查表）。</summary>
        public string PlayfabItemId;
        /// <summary>原版 `PlayfabHoursDuration`（购买时长，小时）。</summary>
        public int PlayfabHoursDuration;

        public BoostItem() { }
        public BoostItem(string playfabItemId, BoostType boostType, int percentBoost, int hoursDuration)
        {
            PlayfabItemId = playfabItemId;
            Type = boostType;
            PercentBoost = percentBoost;
            PlayfabHoursDuration = hoursDuration;
            // 原版 ctor **不设 ExpirationTime**（由服务器下发时填）⇒ 这里也不设（= default(DateTime)）。
        }
    }

    /// <summary>本机「身上有哪些加成」的表 —— **原版这一格在服务器**（`PlayerDataManager.allBoosts`，
    /// 由 PlayFab 清单灌进 `BoostManager._boostCollection`）。
    /// 本机没有账户目录 ⇒ **恒空**；⛔ 不许在这里编「默认送加成」。
    /// 自检 / 将来的本地存档可以往 <see cref="Active"/> 里放，逻辑立刻按原版跑。</summary>
    public static class BoostStore
    {
        /// <summary>当前生效的加成（原版 `BoostManager.GetAllBoosters()` 的等价读口）。</summary>
        public static readonly List<BoostItem> Active = new List<BoostItem>();

        static bool _warnedNoSource;
        /// <summary>取当前加成（顺带清掉已过期的 —— 原版 `BoostManager.CheckBoosterExpirations` 同义）。
        /// 第一次问时出声一次：**本机没有加成来源**（服务器下发）。</summary>
        public static List<BoostItem> Snapshot(DateTime utcNow)
        {
            if (!_warnedNoSource)
            {
                _warnedNoSource = true;
                Debug.Log("[Boost] 加成条目来自服务器（原版 `PlayerDataManager.allBoosts` ← PlayFab 清单），"
                        + "本机无账户目录 ⇒ 本地表恒空、面板不显示（出声一次，⛔ 不是静默失败）。");
            }
            for (int i = Active.Count - 1; i >= 0; i--)
                if (Active[i] == null || Active[i].ExpirationTime <= utcNow) Active.RemoveAt(i);
            return Active;
        }

        /// <summary>自检用：恢复出厂（清表 + 重置出声标记）。</summary>
        public static void ResetForTest() { Active.Clear(); _warnedNoSource = false; }
    }

    /// <summary>原版 `BoostTimerPanel` 的等价实现：**描述一格 + 倒计时一格**。
    /// <para>用法：`var p = BoostTimerPanel.Build(parent, descRect, timerRect, q, descPx, timerPx);`
    /// `p.Init(boostItem);` 之后每帧（或每次 `BoostManager.OnTimerUpdated`）调 `p.UpdateTimer()`；
    /// **返回 true ⇒ 这条加成已到期，宿主应当把它摘掉**（原版 `BoostMenu.UpdateAllPanelsTimers` 的语义）。</para></summary>
    public sealed class BoostTimerPanel
    {
        /// <summary>原版 `BoostDescText`（`TextMeshPro`）—— 我们这边是 `Label`。</summary>
        public Label BoostDescText;
        /// <summary>原版 `BoostTimerText`（`TextMeshPro`）。</summary>
        public Label BoostTimerText;
        /// <summary>原版 `BoostItem BoostItem`（字段名与类型同名，照抄）。</summary>
        public BoostItem Item;
        /// <summary>面板根（`Build` 建的；手搓实例时可为 null）。</summary>
        public Transform Root;

        // ------------------------------------------------------------ 原版三个单句方法（逐句照抄）
        /// <summary>原版 `SetDescription(string)`：`BoostDescText.text = text;`</summary>
        public void SetDescription(string text)
        {
            if (BoostDescText != null) BoostDescText.SetText(text ?? string.Empty);
        }

        /// <summary>原版 `SetTimer(string)`：`BoostTimerText.text = text;`</summary>
        public void SetTimer(string text)
        {
            if (BoostTimerText != null) BoostTimerText.SetText(text ?? string.Empty);
        }

        /// <summary>原版 `Init(BoostItem)`：描述 = `"<Type> Boost <Percent>%"`，随后 `UpdateTimer()`。
        /// ⚠️ 原版**不判空**（`BoostItem` 为 null 时 `UpdateTimer` 直接抛 NRE）；我们出声 + 清空，
        ///    因为本项目红线是「不许静默失败」，而抛 NRE 会让整个窗停摆。</summary>
        public void Init(BoostItem boostItem)
        {
            Item = boostItem;
            if (boostItem == null)
            {
                Debug.LogWarning("[Boost] `BoostTimerPanel.Init(null)` —— 原版这里会 NRE；我们清空两格并出声。");
                SetDescription(string.Empty);
                SetTimer(string.Empty);
                return;
            }
            // 原版：`text = boostItem.Type.ToString(); text2 = boostItem.PercentBoost.ToString();`
            //       ⇒ `description = text + " Boost " + text2 + "%"`（`.c` 里是 Concat 三跳）。
            SetDescription(boostItem.Type.ToString() + " Boost " + boostItem.PercentBoost + "%");
            UpdateTimer();
        }

        // ------------------------------------------------------------ 倒计时
        /// <summary>原版 `UpdateTimer()`（无参，取 `DateTime.UtcNow`）。</summary>
        public bool UpdateTimer() { return UpdateTimer(DateTime.UtcNow); }

        /// <summary>同上，但时间可注入（自检用 —— 判据里的三档与过期语义都要能在不等到期的情况下验）。
        /// <returns>**true = 已过期**（原版 `BoostMenu.UpdateAllPanelsTimers` 据此移除面板）。</returns></summary>
        public bool UpdateTimer(DateTime utcNow)
        {
            if (Item == null)
            {
                // 原版此处会 `NullReferenceException`（`BoostMenu` 的调用点因此会炸整帧）。
                Debug.LogWarning("[Boost] `UpdateTimer()` 时没有 `BoostItem`（原版会 NRE）⇒ 这一帧不刷新。");
                return false;
            }
            bool expired;
            SetTimer(Format(Item.ExpirationTime - utcNow, out expired));
            return expired;
        }

        /// <summary>三档格式，逐句照 `decomp_full/BoostTimerPanel__UpdateTimer.c`（**唯一判据**）。
        /// <param name="expired">`TotalSeconds &lt;= 0` ⇒ true（原版那一支返回 1）。</param></summary>
        public static string Format(TimeSpan span, out bool expired)
        {
            expired = span.TotalSeconds <= 0.0;
            // ① `if ((float)TotalHours < DAT_1834b2bb8)` —— 该常量 = **1.0f**（`TotalHours` 与 1 小时比）。
            if ((float)span.TotalHours < 1f)
            {
                if (expired) return "00:00";                    // 原版：两个 "00" + Format("{0}:{1}")
                // 原版：`Hours`（不是 TotalHours）走同一条 "{0}:{1}:{2}"，三格都是 "00" 格式 ⇒ 恒为 "00:mm:ss"。
                return Pad(span.Hours) + ":" + Pad(span.Minutes) + ":" + Pad(span.Seconds);
            }
            // ② `if (Days < 4)` ⇒ `floor((float)TotalHours)` 当小时位，再 mm:ss。
            if (span.Days < 4)
            {
                int h = (int)Math.Floor((double)(float)span.TotalHours);
                return Pad(h) + ":" + Pad(span.Minutes) + ":" + Pad(span.Seconds);
            }
            // ③ `Days >= 4` ⇒ `floor((float)TotalDays).ToString() + " d"`（天位**不补零**）。
            int d = (int)Math.Floor((double)(float)span.TotalDays);
            return d.ToString() + " d";
        }

        /// <summary>原版那三处 `Int32.ToString("00")`。</summary>
        static string Pad(int v) { return v.ToString("00"); }

        // ------------------------------------------------------------ 建树（几何由宿主给，⛔ 不自己编）
        /// <summary>建一块面板：一个根节点 + 描述一格 + 倒计时一格。
        /// <para>⚠️ **矩形/字号必须由宿主传**：原版这件的宿主 prefab（`BoostMenu` 的 `BoosterPanelPrefab`）
        /// 本地不存在 ⇒ 我们**拿不到**原版几何（见文件头）。⛔ 不要在这里写死任何默认矩形/字号。</para></summary>
        public static BoostTimerPanel Build(Transform parent, PxRect descRect, PxRect timerRect, int q,
                                            float descFontPx, float timerFontPx, Color color,
                                            string name = "Boost Timer Panel",
                                            TmpFont.Face face = TmpFont.Face.LatinCardBody)
        {
            var p = new BoostTimerPanel();
            if (parent == null) return p;
            // 根框 = 两格的外接矩形（原版没有这层根节点 —— `BoostTimerPanel` 直接挂在 prefab 根上；
            // 我们代码建树需要一层父 ⇒ 用外接框，**不新开任何数值**）。
            var rootRect = new PxRect(Mathf.Min(descRect.x1, timerRect.x1), Mathf.Min(descRect.y1, timerRect.y1),
                                      Mathf.Max(descRect.x2, timerRect.x2), Mathf.Max(descRect.y2, timerRect.y2));
            var root = MenuDraw.Node(parent, name, rootRect);
            p.Root = root;
            p.BoostDescText = MenuDraw.Text(root, descRect, string.Empty, color, "Boost Desc Text",
                                            descFontPx, q, descRect.W, face: face);
            p.BoostTimerText = MenuDraw.Text(root, timerRect, string.Empty, color, "Boost Timer Text",
                                             timerFontPx, q, timerRect.W, face: face);
            return p;
        }
    }
}
