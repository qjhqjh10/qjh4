// LiveOpsLocalCatalog.cs —— `A1853②`：活动文案的【离线活动实例提供者】（**等价物，如实标**）
//
// ============================ 这是什么 / 不是什么 ============================
// 原版的活动文案链：`LiveOpsManager` 从**服务端**（PlayFab / CCD）拿到一个 `ILiveOps` 事件实例，
// 再由 `LiveOpsAssetUtility.GetLabel(liveOps, tag)` 从**该事件自己的** `EventLabels` 里按键取词条
// （反编译：`d:/2/tools/decomp_full/Everguild.LiveOps.LiveOpsAssetUtility__GetLabel.c` /
//   `…__GetEventLabel.c` / `…LabelArg__get_Arg.c`；枚举 `Everguild.LiveOps.EventLabelReferenceType`）。
//
// 服务端关了 ⇒ 「**当前开着哪个事件**」这一句**本地没有真值**。但**事件定义本身**在本机：
//   `d:/2/新解包资源/assets_full/bundle_cosmeticsso_assets_all/MonoBehaviour/*.json`
//   里 **13 个活动 SO**（4 Parent + 4 Scoring + 4 Premium Item Offer + 1 已停的 Dark Zealots），
//   每个都带 `eventId` / `dates` / `drawData.EventLabels` / `drawData.EventAssets` / `drawData.EventComponents`。
// 本类 = 把这 13 份 SO **逐字段抄成离线等价物**，并回答三类问题：
//   ① `eventId → Event_Description 键`（16 个键，逐条见 <see cref="EventDescriptionKeys"/>）；
//   ② `eventId → 日期`（`startDate` / `endDate` / `expirable` / `expireDate`）；
//   ③ `eventId → 图片`（`EventAssets` 的 `key`(= `EventAssetReferenceType`) / `m_SubObjectName` / GUID / 所在 bundle）；
//   ④ 🆕 **2026-10-25（R14）**: `eventId → 排期/入口字段` —— `priority`（排序键，降序）· `showInMainMenu` ·
//      `location.type` + `location.parent.targetId` · `drawData` 的 4 个 bool（`ForceRun` /
//      `ShowWindowOnTrigger` / `ShowWindowOnExpire` / `HideContainer`）。
//      这 8 格此前**没抄** ⇒ `LiveOpsHandler.PriorityOf()` 全 0、排序键空转；现在逐份照 SO 抄足。
//
// 🔴 **等价物、不是真值**（如实标的边界）：
//   · 「当前活动」= 本类按 **`DateTime` 落在 `[startDate, endDate)`** 选，**多个在期时取 `startDate` 最晚的那个**。
//     原版是**服务端下发**哪一场 ⇒ 我们的选择**只是等价物**，⛔ 不冒充原版语义。
//   · 只有 `enabled && uploadToLive` 的 SO 才可能被选中（SO 自己的字段；`Dark Zealots` 与
//     `Expansion Orks1 Premium Item Offer` 两份是 `enabled = 0`）。
//   · 🔴 **用户边界「过期活动不做」**（`Sunset Popup` 那条，`任务线/远端挂起线` `P04`）：
//     过期事件（`now > endDate`）**一律不选**、**不拿它顶空态** —— 取不到就是**空态 + 出声**。
//     ⛔ 本类**没有**「回退到最近一个过期活动」这条路（那会被当成「当前活动」显示）。
//   · **报价 / 商店实例**仍属服务端（`PurchasePremiumOffer*` 三条只在这里的 `Premium Item Offer` SO 上）；
//     本类只给出**词条键 + 实参**，不编排购买链。
//
// ============================ 数据出处（可复查坐标）============================
// 逐份 SO 的 `m_Name` 记在 `LiveOpsLocalEvent.SoName`；文件 = 同名 `.json`
// （例 `d:/2/新解包资源/assets_full/bundle_cosmeticsso_assets_all/MonoBehaviour/Expansion Orks1 Scoring Event.json`）。
// `EventAssets` 的 bundle 名来自 `Unity/数据/索引/guid_map.tsv`（GUID → bundle）；
// 取不到 bundle 的照实写 `"?"`（⛔ 不猜；例 `ba6b95f7…` = EC Parent 的 `OpenSound`）。
// ⚠️ **本轮没有解**「`EventComponents` 的 GUID → 具体 prefab 名」—— 只存 GUID（见报告「没查清」）。
//
// ============================ 16 个键 / 12 个承载面 ============================
// 16 个 `Event_Description/*` 键里：
//   · **2 条已接**（2026-10-17）：`ExpansionPass/{Title,GeneralHelp}` → `SkirmishEventWindow` 的 Help 弹窗；
//     ⛔ 本类**不重复接**它们（只把它们当数据收录）。
//   · **12 条 = 本批接的承载面**（<see cref="Surface"/>）。
//   · **2 条按用户边界不做**：`LiveOps/DarkZealots/{Title,Description}` —— 该 SO `enabled = 0`（已停）。
// 12 个承载面逐个对应原版 tag（`EventLabelReferenceType`）：
//   ScoringTitle(0) · ScoringHelp(12) · ScoringEndDescription(11) · ParentIntroTitle(0) · ParentIntroDescription(1) ·
//   ExpansionEventTitleArg(作为 0/1/10/11 的 `{0}`) · TabTitle(4) · PremiumOffer{Title(0),SubTitle(2),Description(1)} ·
//   EC/SW EventTitle（同样只当 `{0}` 实参）—— 见 <see cref="Surfaces"/>。
//
// ============================ 谁在用 ============================
// · `Shell/BaseEventPopup.cs`      —— 弹窗标题/正文在调用方**没给键**时按变体问本类（`TryResolvePopupLabels`）。
// · `Shell/LiveOpsEventWindow.cs`  —— Help 弹窗两条词条的**缺省实现**（子类声明 `LocalLiveOpsEventId` 时生效）。
// · `Shell/ParentEventWindow.cs`   —— 页签文字的**缺省值**（`TabLabelTerm`，没有事件 ⇒ 仍用节点名）。
// · `Shell/BaseOfferPopup.cs`      —— `Title` / `Category` / `Descripton` 的出厂值在**调用方没给**时问本类
//                                    （`TryResolvePremiumOffer`；没有在期报价 ⇒ 仍用 prefab 出厂英文）。
// · 🆕 `Shell/SkirmishEventWindow.cs` —— **活动在期判据**（`HasActiveSkirmishEvent` → `TrySelectActiveFastMode`）：
//                                    不在期 ⇒ 左列不画 `Reward Display`、改画经典式 `Ranked Division Info`
//                                    （判据 → `资料/普查产出_1024b/S1_小冲突非活动期界面.md`）。
// · 静态断言 → `Editor/MainMenuScene.cs` 的 `A1853②` 段（13 份 SO / 16 键 / 12 承载面 / 过期空态）；
//              🆕 FastMode 判据 → `Editor/SkirmishWindowAssertE15.cs`（D 组）。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>活动实例的四大类（前三个 = 本地 13 份 SO 的脚本类型；第四个本地**没有数据**）。</summary>
    public enum LiveOpsLocalKind
    {
        /// <summary>`… Parent Event`（`eventId` 如 `ExpansionOrks1` / `ECRelease` / `SWRelease`）。</summary>
        Parent = 0,
        /// <summary>`… Scoring Event`（`eventId` 如 `ExpansionOrks1Scoring`）。</summary>
        Scoring = 1,
        /// <summary>`… Premium Item Offer`（`eventId` 如 `SWRelease_Premium_Item_Offer`）。</summary>
        PremiumItemOffer = 2,
        /// <summary>🆕 **2026-10-24（E15）**：**FastMode / 遭遇战（Skirmish）活动** —— 原版
        /// `SkirmishEventWindow : LiveOpsEventWindow&lt;IFastModeEvent&gt;`（`dump.cs:63944`），它的 `LiveOp`
        /// 就是一场 FastMode 活动实例；**没有活动就没有这扇窗**（`TryOpen/RefreshScore` 拿空实例会抛）。
        /// ⚠️ 本地 13 份 SO 里**一份 FastMode 都没有**（见 <see cref="LiveOpsLocalCatalog.FastModeEvents"/>，
        /// 表空）⇒ 判据在当前数据下恒 false。⛔ 那不是「写死 false」：判据 = 扫那张表。</summary>
        FastMode = 3,
    }

    /// <summary>`Everguild.LiveOps.EventLabelReferenceType`（**逐值照官方枚举**，`d:/2/Warpforge_code/…/EventLabelReferenceType.cs`）。</summary>
    public enum LiveOpsLabelRef
    {
        Title = 0,
        Description = 1,
        SubTitle = 2,
        Badge = 3,
        NavigationBarTitle = 4,
        WelcomeTitle = 5,
        EndTitle = 10,
        EndDescription = 11,
        Help = 12,
    }

    /// <summary>一个 `LabelArg`（`Everguild.LiveOps.LabelArg`）：`argType 0 = Value`（字面量）/ `1 = Key`（另一条词条）。
    /// 判据 = `d:/2/tools/decomp_full/Everguild.LiveOps.LabelArg__get_Arg.c`（`== 1` ⇒ I2 `LocalizedString` 隐转）。</summary>
    public sealed class LiveOpsLocalArg
    {
        /// <summary>true = `Key`（`Value` 是 I2 词条键）；false = 字面量。</summary>
        public bool IsKey;
        public string Value;
        public override string ToString() { return (IsKey ? "Key:" : "Value:") + (Value ?? ""); }
    }

    /// <summary>一条 `EventLabel`（`key` = <see cref="LiveOpsLabelRef"/>；`value.mTerm` = 词条键；`Args` = 实参）。</summary>
    public sealed class LiveOpsLocalLabel
    {
        public int Key;
        public string Term;
        public LiveOpsLocalArg[] Args;
        public LiveOpsLabelRef Ref { get { return (LiveOpsLabelRef)Key; } }
    }

    /// <summary>`EventAssetReferenceType`（逐值照官方枚举）。给「`key` 数字 → 用途名」用。</summary>
    public enum LiveOpsAssetRef
    {
        Icon = 0,
        BackgroundImage = 1,
        ForegroundImage = 2,
        HeaderImage = 3,
        FooterImage = 4,
        ArmyImage = 5,
        RankingIcon = 6,
        FullScreenImage = 7,
        OpenSound = 8,
        OverIcon = 9,
        MainMenuContainerBackground = 10,
        MainMenuContainerForeground = 11,
        GameModeIcon = 12,
    }

    /// <summary>`EventComponentReferenceType`（逐值照官方枚举）。</summary>
    public enum LiveOpsComponentRef
    {
        Container = 0,
        SubContainer = 1,
        MainWindow = 10,
        Tab = 11,
        Popup = 12,
        TabButton = 13,
        TriggerPopup = 20,
        ExpirePopup = 21,
        EventBadge = 25,
        HelpPopup = 30,
        MainMenuContainer = 35,
    }

    /// <summary>一条 `EventAssets`：`Key` = <see cref="LiveOpsAssetRef"/>；`SubObjectName` 可能为空（原 SO 如此）。</summary>
    public sealed class LiveOpsLocalAsset
    {
        public int Key;
        public string SubObjectName;
        public string Guid;
        /// <summary>`Unity/数据/索引/guid_map.tsv` 查到的 bundle 名；查不到 = `"?"`（如实记，不猜）。</summary>
        public string Bundle;
        public LiveOpsAssetRef Ref { get { return (LiveOpsAssetRef)Key; } }
    }

    /// <summary>一个活动实例（= 一份 SO 的离线等价物）。</summary>
    public sealed class LiveOpsLocalEvent
    {
        public string EventId;
        /// <summary>SO 的 `m_Name`（= 文件名去掉 `.json`，可复查坐标）。</summary>
        public string SoName;
        public LiveOpsLocalKind Kind;
        /// <summary>SO 的 `enabled`（`Dark Zealots` / `Expansion Orks1 Premium Item Offer` 为 false）。</summary>
        public bool Enabled;
        /// <summary>SO 的 `uploadToLive`。</summary>
        public bool UploadToLive;
        public long StartMs;
        public long EndMs;
        public bool Expirable;
        public long ExpireMs;
        public LiveOpsLocalLabel[] Labels;
        public LiveOpsLocalAsset[] Assets;
        /// <summary>`drawData.EventComponents` 的 key（<see cref="LiveOpsComponentRef"/>）→ GUID。</summary>
        public KeyValuePair<int, string>[] Components;

        public DateTime StartUtc { get { return FromMs(StartMs); } }
        public DateTime EndUtc { get { return FromMs(EndMs); } }
        public DateTime ExpireUtc { get { return FromMs(ExpireMs); } }

        /// <summary>可被选中 = SO 自己说「启用且上过 live」。</summary>
        public bool Selectable { get { return Enabled && UploadToLive; } }

        /// <summary>在期判据 = `start ≤ now < end`（右开）。⚠️ `expireDate` **不参与**「在期」——
        /// 那是结束**之后**的过期弹窗窗口，按用户边界不做。</summary>
        public bool IsInPeriod(DateTime utc)
        {
            return utc >= StartUtc && utc < EndUtc;
        }

        /// <summary>`now ≥ end` = 已过期（用户边界：不显示、不拿它顶空态）。</summary>
        public bool IsExpired(DateTime utc) { return utc >= EndUtc; }

        public bool TryGetLabel(int key, out LiveOpsLocalLabel label)
        {
            if (Labels != null)
                for (int i = 0; i < Labels.Length; i++)
                    if (Labels[i] != null && Labels[i].Key == key) { label = Labels[i]; return true; }
            label = null;
            return false;
        }

        public bool HasLabel(int key) { LiveOpsLocalLabel l; return TryGetLabel(key, out l); }

        /// <summary>按 `Term` 找标签（一个事件里同一词条可能出现两次：例 `ExpansionPass/Title` 在 0 与 10）。</summary>
        public bool HasTerm(string term)
        {
            if (Labels == null || string.IsNullOrEmpty(term)) return false;
            for (int i = 0; i < Labels.Length; i++)
                if (Labels[i] != null && Labels[i].Term == term) return true;
            return false;
        }

        /// <summary>找某个词条出现在哪个标签的 `Args` 里（= 它只是 `{0}` 实参，不单独显示）。</summary>
        public bool TryGetArgTerm(string term, out LiveOpsLocalArg arg)
        {
            arg = null;
            if (Labels == null || string.IsNullOrEmpty(term)) return false;
            for (int i = 0; i < Labels.Length; i++)
            {
                var l = Labels[i];
                if (l == null || l.Args == null) continue;
                for (int j = 0; j < l.Args.Length; j++)
                    if (l.Args[j] != null && l.Args[j].IsKey && l.Args[j].Value == term) { arg = l.Args[j]; return true; }
            }
            return false;
        }

        public LiveOpsLocalAsset FindAsset(int key)
        {
            if (Assets != null)
                for (int i = 0; i < Assets.Length; i++)
                    if (Assets[i] != null && Assets[i].Key == key) return Assets[i];
            return null;
        }

        public string AssetName(int key)
        {
            var a = FindAsset(key);
            return a != null && !string.IsNullOrEmpty(a.SubObjectName) ? a.SubObjectName : null;
        }

        static DateTime FromMs(long ms) { return DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime; }

        public override string ToString() { return EventId + "(" + SoName + ")"; }

        // ============================================================ SO 字段（2026-10-25 · R14 补抄）
        // 🔴 这 8 格此前**没搬**（本类只抄了 id / enabled / uploadToLive / 日期 / 词条 / 资产 / 组件），
        //    后果：`LiveOpsHandler.PriorityOf()` 全返回 0 ⇒ R12 接好的排序键**空转**。
        // 判据 = 每份 SO 的同名 JSON 字段（不是枚举里推出来的）：
        //   `priority` · `showInMainMenu` · `location.type` · `location.parent.targetId`
        //   · `drawData.{ForceRun, ShowWindowOnTrigger, ShowWindowOnExpire, HideContainer}`。
        // SO 路径 = `d:/2/新解包资源/assets_full/bundle_cosmeticsso_assets_all/MonoBehaviour/<SoName>.json`。
        // ⚠️ `EventLocationType`（原版枚举，`EventLocationType.cs`）：`Main = 0` / `None = 1` / `Parent = 2`。
        // ⚠️ `loadableReference` 的另外两格（`genericObjectReference` 的 `m_FileID/m_PathID`、
        //    `reference` 的 `m_PathID`）是 **Unity 资产引用**，离线目录没有对应资产 ⇒ 只抄 `targetId`（判据见报告 §五）。
        // 📌 **没抄但已经查清的两格（13/13 完全相同，给 C1-W2 直接取用，⛔ 别再去解包一遍）**：
        //    `drawData.TriggerPopupOptions` = { `PopupLabel` 20, `ActionRule` 1, `TriggerBeforeOnboardingEnds` 0, `MarkOnScreenClose` 1 }
        //    `drawData.ExpirePopupOptions`  = { `PopupLabel` 21, 同上三项 }
        //    `drawData.showPreviousPrice` = 0（13/13）；`previousPrice` 全是空 PPtr（`m_PathID = 0`）。

        /// <summary>SO 的 `priority`（= `LiveOpsEventData.priority`，反编译里读 `+0x40`）。排序键，**降序**。</summary>
        public int Priority;
        /// <summary>SO 的 `showInMainMenu`。13 份实测：只有 `ExpansionUM1` / `SWRelease` 两个 Parent 是 true。</summary>
        public bool ShowInMainMenu;
        /// <summary>SO 的 `location.type`（int 存 <see cref="LiveOpsLocationType"/>；⛔ 不编枚举名，值照 SO）。</summary>
        public int LocationType;
        /// <summary>SO 的 `location.parent.targetId`（`LoadableReference&lt;ParentEventData&gt;` 的 id 串）。
        /// `LocationType != Parent` 时为 null（SO 里是空串 + `m_PathID = 0`）。</summary>
        public string LocationParentId;
        /// <summary>SO 的 `drawData.ForceRun`（13/13 = false）。</summary>
        public bool ForceRun;
        /// <summary>SO 的 `drawData.ShowWindowOnTrigger`（Parent 事件 = true，其余 false）。</summary>
        public bool ShowWindowOnTrigger;
        /// <summary>SO 的 `drawData.ShowWindowOnExpire`（Scoring 事件 = true，其余 false）。</summary>
        public bool ShowWindowOnExpire;
        /// <summary>SO 的 `drawData.HideContainer`（13/13 = false）。</summary>
        public bool HideContainer;
    }

    /// <summary>原版 `EventLocationType`（**逐值照官方枚举**，`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/EventLocationType.cs`）。
    /// ⚠️ 只给读数用；本地目录里存的是 int（见 <see cref="LiveOpsLocalEvent.LocationType"/>）。</summary>
    public enum LiveOpsLocationType
    {
        Main = 0,
        None = 1,
        Parent = 2,
    }

    /// <summary>`Event_Description/*` 活动文案的**离线活动实例提供者**（等价物，如上）。</summary>
    public static class LiveOpsLocalCatalog
    {
        // ============================================================ 数据：13 个活动 SO
        // 🔴 **逐值照 SO 抄**（不换算、不四舍五入）：日期是 SO 的毫秒；`Args` 的 `argType` 0/1 照原样。
        //    SO 路径 = `d:/2/新解包资源/assets_full/bundle_cosmeticsso_assets_all/MonoBehaviour/<SoName>.json`。
        static readonly LiveOpsLocalEvent[] _events =
        {
            // ---------------------------------------------------------------- 已停（enabled=0，按用户边界不选）
            E("DarkZealots", "Dark Zealots Parent Event", LiveOpsLocalKind.Parent, false, false,
              1713520800000, 1714730400000, false, 0, 0, false, 0, null, false, true, false, false,
              new[]
              {
                  L((int)LiveOpsLabelRef.Title,       "Event_Description/LiveOps/DarkZealots/Title"),
                  L((int)LiveOpsLabelRef.Description, "Event_Description/LiveOps/DarkZealots/Description"),
              },
              new[]
              {
                  A((int)LiveOpsAssetRef.Icon,            "40k_main_bt_event",           "163d3c1d41a7f41588ce53bbbb2669e2", "liveopsicons_assets_all.bundle"),
                  A((int)LiveOpsAssetRef.ForegroundImage, "UI_Deck_Warlord_Ghallaron",   "589a2bdca3f94ad4c9c7705474420a56", "uiwarlords_assets_all.bundle"),
              },
              C(LiveOpsComponentRef.MainWindow,  "9a26697357770d8458298d89467fd459"),
              C(LiveOpsComponentRef.TriggerPopup, "e4c251967ff1a79468e8bd25b86b15b8")),

            // ---------------------------------------------------------------- Orks1（2025-04-03 ~ 2025-05-01）
            E("ExpansionOrks1", "Expansion Orks1 Parent Event", LiveOpsLocalKind.Parent, true, true,
              1743685200000, 1746090000000, false, 0, 3, false, 0, null, false, true, false, false,
              new[]
              {
                  L((int)LiveOpsLabelRef.Title, "Event_Description/Expansion/General/JoinTheExpansionEvent"),
                  L((int)LiveOpsLabelRef.Description, "Event_Description/Expansion/General/IntroEventDescription",
                    K("Event_Description/Expansion/ORKExp1/EventTitle"), K("Armies/Goff")),
              },
              new[]
              {
                  A((int)LiveOpsAssetRef.Icon,             "40k_campaign_Expansion Pass", "3a8a1016497754066b40d88caeee722b", "liveopsicons_assets_all.bundle"),
                  A((int)LiveOpsAssetRef.ForegroundImage,  "",                            "74a5cd20cd646dd478f06af44d186145", "uiwarlords_assets_all.bundle"),
                  A((int)LiveOpsAssetRef.FullScreenImage,  "",                            "9eb7ea55a2a504ab593b5b1034e6aeb6", "liveopsmenuimages_assets_all.bundle"),
                  A((int)LiveOpsAssetRef.OpenSound,        "",                            "11c215f6b9e254461a3ec5d917c4f147", "soundcollection_assets_all.bundle"),
                  A((int)LiveOpsAssetRef.OverIcon,         "",                            "5037de524241841dcb39def0f8200334", "armyicons_assets_all.bundle"),
              },
              C(LiveOpsComponentRef.MainWindow,   "9a26697357770d8458298d89467fd459"),
              C(LiveOpsComponentRef.TriggerPopup, "44c1eb3a347b840fd9e2e67ba85b7d02")),

            E("ExpansionOrks1_Premium_Item_Offer", "Expansion Orks1 Premium Item Offer", LiveOpsLocalKind.PremiumItemOffer, false, false,
              1745107200000, 1745107200000, false, 0, 0, false, 2, "ExpansionOrks1", false, false, false, false,
              new[]
              {
                  L((int)LiveOpsLabelRef.Title, "Event_Description/ExpansionPass/PurchasePremiumOfferTitle",
                    K("Armies/Goff_short")),
                  L((int)LiveOpsLabelRef.SubTitle, "Event_Description/ExpansionPass/PurchasePremiumOfferSubtitle",
                    K("Armies/Goff_short")),
                  L((int)LiveOpsLabelRef.Description, "Event_Description/ExpansionPass/PurchasePremiumOfferDescription",
                    K("Card_Name/GOF77"), K("Card_Name/GOF91"), V("14"), V("20")),
                  // 另有一条 `ShopOffers/plusValueOffer`（label 3，`{0}` = 100）—— 不属于 `Event_Description/*`，照 SO 收录。
                  L((int)LiveOpsLabelRef.Badge, "ShopOffers/plusValueOffer", V("100")),
              },
              new[]
              {
                  A((int)LiveOpsAssetRef.BackgroundImage, "", "117e7654c91ec124db66c365be480065", "liveopsmenuimages_assets_all.bundle"),
              },
              C(LiveOpsComponentRef.Container, "a033c0b722070408bb23fe113258b894"),
              C(LiveOpsComponentRef.Popup,     "3c7a2561b7c174784a9e6f1a97231492")),

            E("ExpansionOrks1Scoring", "Expansion Orks1 Scoring Event", LiveOpsLocalKind.Scoring, true, true,
              1743685200000, 1746090000000, true, 1748768400000, 5, false, 2, "ExpansionOrks1", false, false, true, false,
              new[]
              {
                  L((int)LiveOpsLabelRef.Title, "Event_Description/ExpansionPass/Title"),
                  L((int)LiveOpsLabelRef.NavigationBarTitle, "Event_Description/ExpansionPass/TabTitle"),
                  L((int)LiveOpsLabelRef.EndTitle, "Event_Description/ExpansionPass/Title",
                    K("Event_Description/Expansion/ORKExp1/EventTitle")),
                  L((int)LiveOpsLabelRef.EndDescription, "Event_Description/Expansion/General/EndEventDescription",
                    K("Event_Description/Expansion/ORKExp1/EventTitle")),
                  L((int)LiveOpsLabelRef.Help, "Event_Description/ExpansionPass/GeneralHelp", V("20")),
              },
              new[]
              {
                  A((int)LiveOpsAssetRef.BackgroundImage, "", "83dd7da425c23429880b30f8963c5847", "liveopsmenuimages_assets_all.bundle"),
                  A((int)LiveOpsAssetRef.Icon,            "", "3a8a1016497754066b40d88caeee722b", "liveopsicons_assets_all.bundle"),
                  A((int)LiveOpsAssetRef.ArmyImage,       "", "5037de524241841dcb39def0f8200334", "armyicons_assets_all.bundle"),
                  A((int)LiveOpsAssetRef.ForegroundImage, "", "74a5cd20cd646dd478f06af44d186145", "uiwarlords_assets_all.bundle"),
              },
              C(LiveOpsComponentRef.Tab,         "e6914cbdd45814f5f9bd40a854c12218"),
              C(LiveOpsComponentRef.ExpirePopup, "be526b22c0924f54991b776d86261fed"),
              C(LiveOpsComponentRef.HelpPopup,   "2d7ec1a46a19245798bf28a5d1a030bb")),

            // ---------------------------------------------------------------- UM1（2025-12-18 ~ 2026-01-08）
            E("ExpansionUM1", "Expansion UM1 Parent Event", LiveOpsLocalKind.Parent, true, true,
              1766073600000, 1767862800000, false, 0, 42, true, 0, null, false, true, false, false,
              new[]
              {
                  L((int)LiveOpsLabelRef.Title, "Event_Description/Expansion/General/JoinTheExpansionEvent"),
                  L((int)LiveOpsLabelRef.Description, "Event_Description/Expansion/General/IntroEventDescription",
                    K("Event_Description/Expansion/ORKExp1/EventTitle"), K("Armies/Ultramarines")),
              },
              new[]
              {
                  A((int)LiveOpsAssetRef.Icon,                     "40k_campaign_Expansion Pass", "3a8a1016497754066b40d88caeee722b", "liveopsicons_assets_all.bundle"),
                  A((int)LiveOpsAssetRef.ForegroundImage,          "",                            "5403f54083531407aac23aa07df8af36", "uiwarlords_assets_all.bundle"),
                  A((int)LiveOpsAssetRef.FullScreenImage,          "",                            "a31accf1a6bbd4d29a3ade18a5329c09", "liveopsmenuimages_assets_all.bundle"),
                  A((int)LiveOpsAssetRef.OverIcon,                 "",                            "5c839d39738144fc784b3666a64db57f", "armyicons_assets_all.bundle"),
                  A((int)LiveOpsAssetRef.MainMenuContainerBackground, "",                          "93cda5b09dc454964bd61a51c4cc2606", "liveopsmenuimages_assets_all.bundle"),
              },
              C(LiveOpsComponentRef.MainWindow,       "9a26697357770d8458298d89467fd459"),
              C(LiveOpsComponentRef.TriggerPopup,     "44c1eb3a347b840fd9e2e67ba85b7d02"),
              C(LiveOpsComponentRef.MainMenuContainer, "b28a4a686d3cc4366af8977a6ab69b5e")),

            E("ExpansionUM1_Premium_Item_Offer", "Expansion UM1 Premium Item Offer", LiveOpsLocalKind.PremiumItemOffer, true, true,
              1766073600000, 1767862800000, false, 0, 0, false, 2, "ExpansionUM1", false, false, false, false,
              new[]
              {
                  L((int)LiveOpsLabelRef.Title, "Event_Description/ExpansionPass/PurchasePremiumOfferTitle",
                    K("Armies/Ultramarines")),
                  L((int)LiveOpsLabelRef.SubTitle, "Event_Description/ExpansionPass/PurchasePremiumOfferSubtitle",
                    K("Armies/Ultramarines")),
                  L((int)LiveOpsLabelRef.Description, "Event_Description/ExpansionPass/PurchasePremiumOfferDescription",
                    K("Card_Name/UM88"), K("Card_Name/UM100"), V("14"), V("20")),
                  L((int)LiveOpsLabelRef.Badge, "ShopOffers/plusValueOffer", V("100")),
              },
              new[]
              {
                  A((int)LiveOpsAssetRef.BackgroundImage, "", "117e7654c91ec124db66c365be480065", "liveopsmenuimages_assets_all.bundle"),
              },
              C(LiveOpsComponentRef.Container, "a033c0b722070408bb23fe113258b894"),
              C(LiveOpsComponentRef.Popup,     "3c7a2561b7c174784a9e6f1a97231492")),

            E("ExpansionUM1Scoring", "Expansion UM1 Scoring Event", LiveOpsLocalKind.Scoring, true, true,
              1766073600000, 1767862800000, true, 1767862800000, 5, false, 2, "ExpansionUM1", false, false, true, false,
              new[]
              {
                  L((int)LiveOpsLabelRef.Title, "Event_Description/ExpansionPass/Title"),
                  L((int)LiveOpsLabelRef.NavigationBarTitle, "Event_Description/ExpansionPass/TabTitle"),
                  // 🔴 原 SO 的 `{0}` 也指 `ORKExp1/EventTitle`（UM1 没有自己的 EventTitle 词条）——
                  //    **照抄原数据，⛔ 别「修正」**（我们的任务是复刻，不是给原版改文案）。
                  L((int)LiveOpsLabelRef.EndTitle, "Event_Description/ExpansionPass/Title",
                    K("Event_Description/Expansion/ORKExp1/EventTitle")),
                  L((int)LiveOpsLabelRef.EndDescription, "Event_Description/Expansion/General/EndEventDescription",
                    K("Event_Description/Expansion/ORKExp1/EventTitle")),
                  L((int)LiveOpsLabelRef.Help, "Event_Description/ExpansionPass/GeneralHelp", V("20")),
              },
              new[]
              {
                  A((int)LiveOpsAssetRef.BackgroundImage, "", "aeae5fff5e8e946488af4875fd233e72", "liveopsmenuimages_assets_all.bundle"),
                  A((int)LiveOpsAssetRef.Icon,            "", "3a8a1016497754066b40d88caeee722b", "liveopsicons_assets_all.bundle"),
                  A((int)LiveOpsAssetRef.ArmyImage,       "", "5c839d39738144fc784b3666a64db57f", "armyicons_assets_all.bundle"),
                  A((int)LiveOpsAssetRef.ForegroundImage, "", "5403f54083531407aac23aa07df8af36", "uiwarlords_assets_all.bundle"),
              },
              C(LiveOpsComponentRef.Tab,         "e6914cbdd45814f5f9bd40a854c12218"),
              C(LiveOpsComponentRef.ExpirePopup, "be526b22c0924f54991b776d86261fed"),
              C(LiveOpsComponentRef.HelpPopup,   "2d7ec1a46a19245798bf28a5d1a030bb")),

            // ---------------------------------------------------------------- EC（2025-05-22 ~ 2025-06-20）
            E("ECRelease", "Release Emperors Children Parent Event", LiveOpsLocalKind.Parent, true, true,
              1747931400000, 1750410000000, false, 0, 3, false, 0, null, false, true, false, false,
              new[]
              {
                  L((int)LiveOpsLabelRef.Title, "Event_Description/Expansion/General/JoinTheExpansionEvent"),
                  L((int)LiveOpsLabelRef.Description, "Event_Description/Expansion/General/IntroEventDescription",
                    K("Event_Description/Expansion/ORKExp1/EventTitle"), K("Armies/EmperorsChildren")),
              },
              new[]
              {
                  A((int)LiveOpsAssetRef.Icon,             "40k_campaign_Expansion Pass", "3a8a1016497754066b40d88caeee722b", "liveopsicons_assets_all.bundle"),
                  A((int)LiveOpsAssetRef.ForegroundImage,  "",                            "eecd840c5d425e74ebfd196bf08beaf5", "uiwarlords_assets_all.bundle"),
                  A((int)LiveOpsAssetRef.FullScreenImage,  "",                            "a9f38a84a8bd63449a4cfe4c55d2c02e", "liveopsmenuimages_assets_all.bundle"),
                  A((int)LiveOpsAssetRef.OpenSound,        "",                            "ba6b95f76b1003141b098fc85bed0a3b", "?"),
                  A((int)LiveOpsAssetRef.OverIcon,         "",                            "9fcb1488cd4be4902b1fbac59e1ab3b8", "armyicons_assets_all.bundle"),
              },
              C(LiveOpsComponentRef.MainWindow,   "9a26697357770d8458298d89467fd459"),
              C(LiveOpsComponentRef.TriggerPopup, "44c1eb3a347b840fd9e2e67ba85b7d02")),

            E("ECRelease_Premium_Item_Offer", "Release Emperors Children Premium Item Offer", LiveOpsLocalKind.PremiumItemOffer, true, true,
              1747931400000, 1750410000000, false, 0, 0, false, 2, "ECRelease", false, false, false, false,
              new[]
              {
                  L((int)LiveOpsLabelRef.Title, "Event_Description/ExpansionPass/PurchasePremiumOfferTitle",
                    K("Armies/EmperorsChildren")),
                  L((int)LiveOpsLabelRef.SubTitle, "Event_Description/ExpansionPass/PurchasePremiumOfferSubtitle",
                    K("Armies/EmperorsChildren")),
                  L((int)LiveOpsLabelRef.Description, "Event_Description/ExpansionPass/PurchasePremiumOfferDescription",
                    K("Card_Name/EC57"), K("Card_Name/EC21"), V("14"), V("20")),
                  L((int)LiveOpsLabelRef.Badge, "ShopOffers/plusValueOffer", V("100")),
              },
              new[]
              {
                  A((int)LiveOpsAssetRef.BackgroundImage, "", "117e7654c91ec124db66c365be480065", "liveopsmenuimages_assets_all.bundle"),
              },
              C(LiveOpsComponentRef.Container, "a033c0b722070408bb23fe113258b894"),
              C(LiveOpsComponentRef.Popup,     "3c7a2561b7c174784a9e6f1a97231492")),

            E("ECReleaseScoring", "Release Emperors Children Scoring Event", LiveOpsLocalKind.Scoring, true, true,
              1747931400000, 1750410000000, false, 0, 5, false, 2, "ECRelease", false, false, true, false,
              new[]
              {
                  L((int)LiveOpsLabelRef.Title, "Event_Description/BattlePass/Title"),
                  L((int)LiveOpsLabelRef.NavigationBarTitle, "Event_Description/ExpansionPass/TabTitle"),
                  L((int)LiveOpsLabelRef.EndTitle, "Event_Description/BattlePass/Title",
                    K("Event_Description/BattlePass/EC/EventTitle")),
                  L((int)LiveOpsLabelRef.EndDescription, "Event_Description/Expansion/General/EndEventDescription",
                    K("Event_Description/BattlePass/EC/EventTitle")),
                  L((int)LiveOpsLabelRef.Help, "Event_Description/BattlePass/GeneralHelp", V("20")),
              },
              new[]
              {
                  A((int)LiveOpsAssetRef.BackgroundImage, "", "96c24f52fbe94444498d475a9e20f28c", "liveopsmenuimages_assets_all.bundle"),
                  A((int)LiveOpsAssetRef.Icon,            "", "3a8a1016497754066b40d88caeee722b", "liveopsicons_assets_all.bundle"),
                  A((int)LiveOpsAssetRef.ArmyImage,       "", "9fcb1488cd4be4902b1fbac59e1ab3b8", "armyicons_assets_all.bundle"),
                  A((int)LiveOpsAssetRef.ForegroundImage, "", "ebc37ddeba3d945a68b1dec953298a3f", "uiwarlords_assets_all.bundle"),
              },
              C(LiveOpsComponentRef.Tab,         "e6914cbdd45814f5f9bd40a854c12218"),
              C(LiveOpsComponentRef.ExpirePopup, "be526b22c0924f54991b776d86261fed"),
              C(LiveOpsComponentRef.HelpPopup,   "2d7ec1a46a19245798bf28a5d1a030bb")),

            // ---------------------------------------------------------------- SW（2025-10-23 ~ 2025-11-21）
            E("SWRelease", "Release Space Wolves Parent Event", LiveOpsLocalKind.Parent, true, true,
              1761228000000, 1763715600000, false, 0, 42, true, 0, null, false, true, false, false,
              new[]
              {
                  // 🔴 SW Parent 的标题**不是** `JoinTheExpansionEvent`，而是它自己那条
                  //    `BattlePass/SW/EventTitle`（判据 = SO 的 `EventLabels[0]`，逐字）。
                  L((int)LiveOpsLabelRef.Title, "Event_Description/BattlePass/SW/EventTitle"),
                  L((int)LiveOpsLabelRef.Description, "Event_Description/Expansion/General/IntroEventDescription",
                    K("Event_Description/BattlePass/SW/EventTitle"), K("Armies/SpaceWolves")),
              },
              new[]
              {
                  A((int)LiveOpsAssetRef.Icon,                     "40k_campaign_Expansion Pass", "3a8a1016497754066b40d88caeee722b", "liveopsicons_assets_all.bundle"),
                  A((int)LiveOpsAssetRef.ForegroundImage,          "",                            "1efbc2d559acef84d89100bff583b7d1", "uiwarlords_assets_all.bundle"),
                  A((int)LiveOpsAssetRef.FullScreenImage,          "",                            "658140aa40a494782ba762ef88933626", "liveopsmenuimages_assets_all.bundle"),
                  A((int)LiveOpsAssetRef.OpenSound,                "",                            "baa1a292a3fe04b6097feefbfea91919", "soundcollection_assets_all.bundle"),
                  A((int)LiveOpsAssetRef.OverIcon,                 "",                            "cd1608dfc27c748e88545ea269e9b0e8", "armyicons_assets_all.bundle"),
                  A((int)LiveOpsAssetRef.MainMenuContainerBackground, "",                          "ad9725ac046204b7fbdee84edc37f65c", "liveopsmenuimages_assets_all.bundle"),
              },
              C(LiveOpsComponentRef.MainWindow,       "9a26697357770d8458298d89467fd459"),
              C(LiveOpsComponentRef.TriggerPopup,     "44c1eb3a347b840fd9e2e67ba85b7d02"),
              C(LiveOpsComponentRef.MainMenuContainer, "b28a4a686d3cc4366af8977a6ab69b5e")),

            E("SWRelease_Premium_Item_Offer", "Release Space Wolves Premium Item Offer", LiveOpsLocalKind.PremiumItemOffer, true, true,
              1761228000000, 1763715600000, false, 0, 0, false, 2, "SWRelease", false, false, false, false,
              new[]
              {
                  L((int)LiveOpsLabelRef.Title, "Event_Description/ExpansionPass/PurchasePremiumOfferTitle",
                    K("Armies/SpaceWolves")),
                  L((int)LiveOpsLabelRef.SubTitle, "Event_Description/ExpansionPass/PurchasePremiumOfferSubtitle",
                    K("Armies/SpaceWolves")),
                  L((int)LiveOpsLabelRef.Description, "Event_Description/ExpansionPass/PurchasePremiumOfferDescription",
                    K("Card_Name/SW2"), K("Card_Name/SW57"), V("14"), V("20")),
                  L((int)LiveOpsLabelRef.Badge, "ShopOffers/plusValueOffer", V("100")),
              },
              new[]
              {
                  A((int)LiveOpsAssetRef.BackgroundImage, "", "117e7654c91ec124db66c365be480065", "liveopsmenuimages_assets_all.bundle"),
              },
              C(LiveOpsComponentRef.Container, "a033c0b722070408bb23fe113258b894"),
              C(LiveOpsComponentRef.Popup,     "3c7a2561b7c174784a9e6f1a97231492")),

            E("SWReleaseScoring", "Release Space Wolves Scoring Event", LiveOpsLocalKind.Scoring, true, true,
              1761228000000, 1763715600000, true, 1766275200000, 5, false, 2, "SWRelease", false, false, true, false,
              new[]
              {
                  L((int)LiveOpsLabelRef.Title, "Event_Description/BattlePass/Title"),
                  L((int)LiveOpsLabelRef.NavigationBarTitle, "Event_Description/ExpansionPass/TabTitle"),
                  L((int)LiveOpsLabelRef.EndTitle, "Event_Description/BattlePass/Title",
                    K("Event_Description/BattlePass/SW/EventTitle")),
                  L((int)LiveOpsLabelRef.EndDescription, "Event_Description/Expansion/General/EndEventDescription",
                    K("Event_Description/BattlePass/SW/EventTitle")),
                  L((int)LiveOpsLabelRef.Help, "Event_Description/BattlePass/GeneralHelp", V("20")),
              },
              new[]
              {
                  A((int)LiveOpsAssetRef.BackgroundImage, "", "7a8857538eb704477a94d1a36f510da0", "liveopsmenuimages_assets_all.bundle"),
                  A((int)LiveOpsAssetRef.Icon,            "", "3a8a1016497754066b40d88caeee722b", "liveopsicons_assets_all.bundle"),
                  A((int)LiveOpsAssetRef.ArmyImage,       "", "cd1608dfc27c748e88545ea269e9b0e8", "armyicons_assets_all.bundle"),
                  A((int)LiveOpsAssetRef.ForegroundImage, "", "fca5216814e21154b8cd794423d0c3d9", "uiwarlords_assets_all.bundle"),
              },
              C(LiveOpsComponentRef.Tab,         "e6914cbdd45814f5f9bd40a854c12218"),
              C(LiveOpsComponentRef.ExpirePopup, "be526b22c0924f54991b776d86261fed"),
              C(LiveOpsComponentRef.HelpPopup,   "2d7ec1a46a19245798bf28a5d1a030bb")),
        };

        // ============================================================ 16 键 / 12 承载面清单
        /// <summary>16 个 `Event_Description/*` 键（= 13 个 SO 的 `EventLabels` 里出现过的全部；顺序 = 本类文档那张表）。</summary>
        public static readonly string[] EventDescriptionKeys =
        {
            "Event_Description/ExpansionPass/Title",                       // 已接（Skirmish Help 标题）
            "Event_Description/ExpansionPass/GeneralHelp",                 // 已接（Skirmish Help 正文）
            "Event_Description/BattlePass/Title",                          // 12 承载面 #1
            "Event_Description/BattlePass/GeneralHelp",                    // #2
            "Event_Description/Expansion/General/EndEventDescription",     // #3
            "Event_Description/Expansion/General/IntroEventDescription",   // #4
            "Event_Description/Expansion/General/JoinTheExpansionEvent",   // #5
            "Event_Description/Expansion/ORKExp1/EventTitle",              // #6（只当 {0} 实参）
            "Event_Description/ExpansionPass/TabTitle",                    // #7
            "Event_Description/ExpansionPass/PurchasePremiumOfferTitle",   // #8
            "Event_Description/ExpansionPass/PurchasePremiumOfferSubtitle",// #9
            "Event_Description/ExpansionPass/PurchasePremiumOfferDescription", // #10
            "Event_Description/BattlePass/EC/EventTitle",                  // #11（只当 {0} 实参）
            "Event_Description/BattlePass/SW/EventTitle",                  // #12（SW Parent 的标题 + {0}）
            // 按用户边界【不做】的两条（`Dark Zealots` SO `enabled = 0`）：
            "Event_Description/LiveOps/DarkZealots/Title",
            "Event_Description/LiveOps/DarkZealots/Description",
        };

        /// <summary>按用户边界（过期活动不做）**不接**的两条键 —— 数据仍收录，供对账。</summary>
        public static readonly string[] ExcludedExpiredKeys =
        {
            "Event_Description/LiveOps/DarkZealots/Title",
            "Event_Description/LiveOps/DarkZealots/Description",
        };

        /// <summary>12 个承载面（= 16 键 − 已接 2 − 用户边界不做 2）。</summary>
        public enum Surface
        {
            /// <summary>记分活动 Help 弹窗标题（label 0/10；ORK/UM = `ExpansionPass/Title`，EC/SW = `BattlePass/Title`）。</summary>
            ScoringHelpTitle = 0,
            /// <summary>记分活动 Help 弹窗正文（label 12）。</summary>
            ScoringHelpBody = 1,
            /// <summary>活动结束弹窗正文（label 11，`Base Expiration Popup`）。</summary>
            ScoringEndDescription = 2,
            /// <summary>Parent Event 介绍标题（label 0）。</summary>
            ParentIntroTitle = 3,
            /// <summary>Parent Event 介绍正文（label 1）。</summary>
            ParentIntroDescription = 4,
            /// <summary>`Expansion/ORKExp1/EventTitle` —— 只当上面两条的 `{0}` 实参（label 1/10/11 的 Args）。</summary>
            ExpansionEventTitleArg = 5,
            /// <summary>Parent Event 左侧页签（label 4 = `ExpansionPass/TabTitle`）。</summary>
            ScoringTabTitle = 6,
            /// <summary>Premium Item Offer 弹窗标题（label 0）。</summary>
            PremiumOfferTitle = 7,
            /// <summary>Premium Item Offer 弹窗副标题 / Category（label 2）。</summary>
            PremiumOfferSubtitle = 8,
            /// <summary>Premium Item Offer 弹窗正文（label 1）。</summary>
            PremiumOfferDescription = 9,
            /// <summary>`BattlePass/EC/EventTitle` —— EC 的 `{0}` 实参（label 1/10/11 的 Args）。</summary>
            ECEventTitleArg = 10,
            /// <summary>`BattlePass/SW/EventTitle` —— SW Parent 标题（label 0）+ `{0}` 实参。</summary>
            SWEventTitleArg = 11,
        }

        /// <summary>一个承载面的规格：它对应哪条原版 label、能落在哪几类事件上、主键是什么。</summary>
        public sealed class SurfaceSpec
        {
            public Surface Surface;
            public string Name;
            public int LabelKey;
            public LiveOpsLocalKind[] Kinds;
            /// <summary>「主键」= 该承载面在 U1 报告里对应那一条 `Event_Description/*` 键
            /// （同一承载面在不同活动上可能解析成同族的另一条 —— 例 ORK vs EC 的 pass 标题）。</summary>
            public string PrimaryTerm;
            /// <summary>除了 `PrimaryTerm` 之外，这个承载面还可能解析成哪条键（逐条有来源）。</summary>
            public string[] AlternateTerms;
        }

        public static readonly SurfaceSpec[] Surfaces =
        {
            S(Surface.ScoringHelpTitle, "记分活动 Help 弹窗标题", (int)LiveOpsLabelRef.Title,
              new[] { LiveOpsLocalKind.Scoring }, "Event_Description/BattlePass/Title",
              "Event_Description/ExpansionPass/Title"),
            S(Surface.ScoringHelpBody, "记分活动 Help 弹窗正文", (int)LiveOpsLabelRef.Help,
              new[] { LiveOpsLocalKind.Scoring }, "Event_Description/BattlePass/GeneralHelp",
              "Event_Description/ExpansionPass/GeneralHelp"),
            S(Surface.ScoringEndDescription, "活动结束弹窗正文", (int)LiveOpsLabelRef.EndDescription,
              new[] { LiveOpsLocalKind.Scoring }, "Event_Description/Expansion/General/EndEventDescription"),
            S(Surface.ParentIntroTitle, "Parent Event 介绍标题", (int)LiveOpsLabelRef.Title,
              new[] { LiveOpsLocalKind.Parent }, "Event_Description/Expansion/General/JoinTheExpansionEvent",
              "Event_Description/BattlePass/SW/EventTitle"),
            S(Surface.ParentIntroDescription, "Parent Event 介绍正文", (int)LiveOpsLabelRef.Description,
              new[] { LiveOpsLocalKind.Parent }, "Event_Description/Expansion/General/IntroEventDescription"),
            // 实参承载面：label 本身不单独显示，只在别的词条里当 `{0}`；`LabelKey` 无意义（-1）。
            S(Surface.ExpansionEventTitleArg, "`{0}` 实参（ORK 扩展标题）", -1,
              new[] { LiveOpsLocalKind.Parent, LiveOpsLocalKind.Scoring },
              "Event_Description/Expansion/ORKExp1/EventTitle"),
            S(Surface.ScoringTabTitle, "Parent Event 左侧页签", (int)LiveOpsLabelRef.NavigationBarTitle,
              new[] { LiveOpsLocalKind.Scoring }, "Event_Description/ExpansionPass/TabTitle"),
            S(Surface.PremiumOfferTitle, "报价弹窗标题", (int)LiveOpsLabelRef.Title,
              new[] { LiveOpsLocalKind.PremiumItemOffer }, "Event_Description/ExpansionPass/PurchasePremiumOfferTitle"),
            S(Surface.PremiumOfferSubtitle, "报价弹窗副标题 / Category", (int)LiveOpsLabelRef.SubTitle,
              new[] { LiveOpsLocalKind.PremiumItemOffer }, "Event_Description/ExpansionPass/PurchasePremiumOfferSubtitle"),
            S(Surface.PremiumOfferDescription, "报价弹窗正文", (int)LiveOpsLabelRef.Description,
              new[] { LiveOpsLocalKind.PremiumItemOffer }, "Event_Description/ExpansionPass/PurchasePremiumOfferDescription"),
            S(Surface.ECEventTitleArg, "`{0}` 实参（EC 发布活动标题）", -1,
              new[] { LiveOpsLocalKind.Parent, LiveOpsLocalKind.Scoring },
              "Event_Description/BattlePass/EC/EventTitle"),
            S(Surface.SWEventTitleArg, "`{0}` 实参（SW 发布活动标题）", -1,
              new[] { LiveOpsLocalKind.Parent, LiveOpsLocalKind.Scoring },
              "Event_Description/BattlePass/SW/EventTitle"),
        };

        public const int SurfaceCount = 12;

        // ============================================================ 公共读口
        public static IReadOnlyList<LiveOpsLocalEvent> Events { get { return _events; } }
        public static int Count { get { return _events.Length; } }

        public static LiveOpsLocalEvent Find(string eventId)
        {
            if (string.IsNullOrEmpty(eventId)) return null;
            for (int i = 0; i < _events.Length; i++)
                if (_events[i].EventId == eventId) return _events[i];
            return null;
        }

        /// <summary>「现在」的唯一读口（窗口缺省值用它；自检**不要**改它 —— 断言一律传显式 `DateTime`）。</summary>
        public static DateTime NowUtc { get { return DateTime.UtcNow; } }

        /// <summary>选一个**在期**事件：只考虑 `Selectable`（`enabled && uploadToLive`）且 `start ≤ now &lt; end` 的；
        /// 多个时取 `StartMs` 最大的那个（= 最近开始的）。**⛔ 过期事件永不入选**。
        /// <para>取不到 ⇒ false + <paramref name="why"/> 说明原因（调用方要**出声**，⛔ 不静默）。</para></summary>
        public static bool TrySelectActive(LiveOpsLocalKind kind, DateTime utc, out LiveOpsLocalEvent ev, out string why)
        {
            return TrySelectActive(new[] { kind }, utc, out ev, out why);
        }

        public static bool TrySelectActive(LiveOpsLocalKind[] kinds, DateTime utc, out LiveOpsLocalEvent ev, out string why)
        {
            return TryPickEvent(kinds, utc, null, out ev, out why);
        }

        /// <summary>在期事件里挑一个：`has` = 该事件是否满足这个读口的条件（null = 不筛）；
        /// 多个满足 ⇒ `StartMs` 最大者。**⛔ 过期事件永不入选**（`IsInPeriod` 那道闸）。</summary>
        static bool TryPickEvent(LiveOpsLocalKind[] kinds, DateTime utc, Func<LiveOpsLocalEvent, bool> has,
                                 out LiveOpsLocalEvent ev, out string why)
        {
            return TryPickEventIn(_events, kinds, utc, has, out ev, out why);
        }

        /// <summary>🆕 **2026-10-24（E15）**：上一行的**通用形态** —— 同一套挑选规则跑在**任意一张表**上
        /// （`_events` = 那 13 份 SO；`_fastModeEvents` = FastMode 表，当前空）。
        /// 🔑 **这个抽取就是「FastMode 判据不是写死 false」的落地**：两张表走**同一个函数**
        /// （自检 D2 用「`_events` 上同一函数能选出 Scoring」来证明这条链是活的）。</summary>
        static bool TryPickEventIn(LiveOpsLocalEvent[] table, LiveOpsLocalKind[] kinds, DateTime utc,
                                   Func<LiveOpsLocalEvent, bool> has, out LiveOpsLocalEvent ev, out string why)
        {
            // 🔴 2026-10-25（E2 · C1-W1）：挑选规则的**唯一实现**已搬去 `LiveOpsLocalHandler.SelectInTable`
            // （判据 `资料/完全复刻/判据/日常与活动.md` §四 第 3 步「把『当前活动』的选择从 catalog 挪进 handler」）。
            // 这里**只转发**，⛔ 不再留第二份规则（CLAUDE.md §三：两处写同一条规则 = 迟早不一致）。
            // 行为不变：kinds → IsInPeriod → has → blocked（运营维）→ Selectable → StartMs 最大者。
            var list = new List<LiveOpsLocalEvent>(table == null ? 0 : table.Length);
            if (table != null) list.AddRange(table);
            return LiveOpsLocalHandler.SelectInTable(list, kinds, utc, has,
                                                     LiveOpsLocalHandler.Default.BlockedIds, out ev, out why);
        }

        // ============================================================ 🆕 FastMode（遭遇战）活动：本地表空
        //
        // 原版：`LiveOpsManager` 从**服务端**（PlayFab / CCD）拿 `ILiveOps` 实例；
        //   `SkirmishEventWindow : LiveOpsEventWindow<IFastModeEvent>`（`dump.cs:63944`）的 `LiveOp`
        //   就是一场 FastMode 活动 —— **活动不在期 ⇒ 那扇窗根本开不出来**
        //   （`TryOpen` / `RefreshScore` 拿空实例走 `FUN_1803f47a0()` = 抛）。
        //   用户口径（2026-10-24）：「活动进度 / 说明 / 胜利数 / 5 档里程碑 / 收集 —— 那是**活动期**才有的；
        //   平常应该和经典大概一样」。判据与实机证据 → `资料/普查产出_1024b/S1_小冲突非活动期界面.md`。
        //
        // 本地：`_events` 那 13 份 SO 是 Parent / Scoring / Premium Item Offer 三类，**没有 FastMode 类**；
        //   解包资源里也**没有** FastMode 的 SO（`cosmeticsso` 那 13 份逐份看过；关服前 Player.log 里
        //   那几场 `Revival*`（含 `RevivalSkirmish`）的 SO **本地不存在**）。
        //   ⇒ 本表**当前为空**。⛔ **这不是「写死 false」**：下面两个读口扫的就是这张表，
        //     哪天把 FastMode 的 SO 抄进来（`E(...)` 那一族助手直接能用），判据立刻按
        //     `Selectable && IsInPeriod` 生效。空表这件事**每次都出声**（`_warnedNoFastMode`，每进程一次）。
        static readonly LiveOpsLocalEvent[] _fastModeEvents = { };

        /// <summary>FastMode（遭遇战）活动实例表 —— 见上一段的落地说明。**当前 0 条**。</summary>
        public static IReadOnlyList<LiveOpsLocalEvent> FastModeEvents { get { return _fastModeEvents; } }
        /// <summary>FastMode 活动实例条数（**现读**；自检 D1 用它，⛔ 别把「0」写死进断言文案以外的地方）。</summary>
        public static int FastModeEventCount { get { return _fastModeEvents.Length; } }

        /// <summary>🔴 **自检注入口**（⛔ 只给 `Editor/SkirmishWindowAssertE15.cs` 的 B 组）：非 null ⇒
        /// **顶替** <see cref="FastModeEvents"/> 那张表（读口一句都不用改）。
        /// <para>为什么要有它：本地表空 ⇒ 「活动期」那一支**没有任何数据能走到**；只能塞一条**合成事件**
        /// （标成 `E15 合成`、⛔ 不是伪造原版活动）来证明**这条判据链是活的** —— 否则「读表」和「写死 false」
        /// 在本地**观察不到差别**（自证）。用完必须还原成 `null`。</para></summary>
        public static LiveOpsLocalEvent[] FastModeEventsOverrideForTest;

        /// <summary>🆕 **2026-10-24（E15）**：「现在有没有一场在期的 FastMode（遭遇战）活动」——
        /// **真判据**（扫 `_fastModeEvents` + `Selectable` + `IsInPeriod`，与其余三类**同一个挑选函数**）。
        /// 取不到 ⇒ false + <paramref name="why"/>（调用方**出声**，⛔ 不静默；`SkirmishEventWindow` 用它决定左列画哪一版）。</summary>
        public static bool HasActiveFastModeEvent(DateTime utc, out LiveOpsLocalEvent ev, out string why)
        {
            return TrySelectActiveFastMode(utc, out ev, out why);
        }

        /// <summary>见上一行。⚠️ 表空时**每进程出声一次**（`_warnedNoFastMode`）—— 「判不出」要看得见。</summary>
        public static bool TrySelectActiveFastMode(DateTime utc, out LiveOpsLocalEvent ev, out string why)
        {
            var table = FastModeEventsOverrideForTest ?? _fastModeEvents;
            if (TryPickEventIn(table, new[] { LiveOpsLocalKind.FastMode }, utc, null, out ev, out why))
                return true;

            why = "**本机没有 FastMode（遭遇战）活动实例** —— 原版 `SkirmishEventWindow` 的 `LiveOp` 是"
                + " `IFastModeEvent`（`dump.cs:63944`），由**服务端**下发；本地 13 份活动 SO 只有 "
                + "Parent / Scoring / Premium Item Offer 三类，FastMode 表 = **"
                + table.Length + " 条**（"
                + (table.Length == 0 ? "数据源拿不到" : "都不在期/不可选") + "）"
                + "⇒ 判据当前为 false。⚠️ 这不是写死 false，是表里没数据（见 `FastModeEvents` 那段注释）。";
            if (!_warnedNoFastMode)
            {
                _warnedNoFastMode = true;
                Debug.LogWarning("[LiveOpsLocal] " + why);
            }
            return false;
        }

        /// <summary>解析一个承载面。`utc` 落在某个**在期且可选中**的事件里才返回 true；
        /// 否则 false + <paramref name="why"/>（= 空态；**⛔ 不回退到过期活动**）。
        /// <para>「哪个事件」= 满足该承载面条件的在期事件里 **`StartMs` 最大**的那个
        /// （条件 = 有这条 label / 这个实参在它的 `Args` 里）—— 例 EC 的 `{0}` 只在 Scoring 上，
        /// 而 Parent 与 Scoring 同一开始时刻 ⇒ **必须按「有没有这条」筛，⛔ 不能先选事件再查**。</para></summary>
        public static bool TryResolveSurface(Surface surface, DateTime utc,
                                             out LiveOpsLocalSurfaceValue value, out string why)
        {
            value = null;
            var spec = Surfaces[(int)surface];

            // 实参型承载面：只在别的标签的 `Args` 里出现。
            if (spec.LabelKey < 0)
            {
                LiveOpsLocalEvent aev;
                if (!TryPickEvent(spec.Kinds, utc, e => e.TryGetArgTerm(spec.PrimaryTerm, out _), out aev, out why))
                    return false;
                LiveOpsLocalArg arg;
                aev.TryGetArgTerm(spec.PrimaryTerm, out arg);
                value = new LiveOpsLocalSurfaceValue
                {
                    Event = aev, LabelKey = -1, Term = arg.Value, Args = null,
                    ArgTexts = null, Text = TermText(arg.Value),
                };
                why = null;
                return true;
            }

            LiveOpsLocalEvent ev;
            if (!TryPickEvent(spec.Kinds, utc, e => e.HasLabel(spec.LabelKey), out ev, out why))
                return false;
            LiveOpsLocalLabel label;
            ev.TryGetLabel(spec.LabelKey, out label);
            value = new LiveOpsLocalSurfaceValue
            {
                Event = ev,
                LabelKey = spec.LabelKey,
                Term = label.Term,
                Args = label.Args,
                ArgTexts = ResolveArgs(label.Args),
                Text = TermText(label.Term),
            };
            why = null;
            return true;
        }

        /// <summary>承载面的文案（取不到 ⇒ **空串**；`⛔ 不印键名、⛔ 不拿出厂英文顶**）。</summary>
        public static string SurfaceTerm(Surface surface, DateTime utc)
        {
            LiveOpsLocalSurfaceValue v; string why;
            return TryResolveSurface(surface, utc, out v, out why) ? v.Text : "";
        }

        /// <summary>Help 弹窗标题（`LiveOpsEventWindow` 缺省实现用）：给了 `eventId` 才查，
        /// label 优先 `WelcomeTitle(5)`、没有就 `Title(0)`（原版 `GetLabel(tag)` 取不到就是空）。
        /// 🔴 **只在事件可选中且在期时返回**（用户边界「过期活动不做」；过期 ⇒ null = 空态）。</summary>
        public static string HelpTitleTerm(string eventId)
        {
            var ev = Find(eventId);
            if (ev == null || !ev.Selectable || !ev.IsInPeriod(NowUtc)) return null;
            if (ev.HasLabel((int)LiveOpsLabelRef.WelcomeTitle)) return LabelTerm(ev, (int)LiveOpsLabelRef.WelcomeTitle);
            if (ev.HasLabel((int)LiveOpsLabelRef.Title)) return LabelTerm(ev, (int)LiveOpsLabelRef.Title);
            return null;
        }

        /// <summary>Help 弹窗正文（label 12 = `Help`）。没有 / 不在期 ⇒ null。</summary>
        public static string HelpDescriptionTerm(string eventId)
        {
            var ev = Find(eventId);
            return ev != null && ev.Selectable && ev.IsInPeriod(NowUtc)
                   && ev.HasLabel((int)LiveOpsLabelRef.Help) ? LabelTerm(ev, (int)LiveOpsLabelRef.Help) : null;
        }

        /// <summary>`EventPopupVariant` → 这对标签该用哪两条词条（`BaseEventPopup` 的缺省值用）。
        /// <para>逐值判据 = `Shell/BaseEventPopup.cs` 文件头那张「变体 × tag」表（六个 prefab 实读）：
        /// Base/FullScreen = 0/1 · EventHelp 两种 = 0/12 · LiveopEventHelp = 5/12 · BaseExpiration = 10/11。</para>
        /// <para>⚠️ 13 个 SO 里**没有任何一条 label 5** ⇒ `LiveopEventHelp` 的标题解析为 null（原版同义：
        /// `GetLabel(WelcomeTitle)` 取不到就写空串）。</para></summary>
        public static bool TryResolvePopupLabels(EventPopupVariant variant, DateTime utc,
                                                 out string titleTerm, out string[] titleArgs,
                                                 out string descriptionTerm, out string[] descriptionArgs,
                                                 out LiveOpsLocalEvent ev, out string why)
        {
            titleTerm = null; titleArgs = null; descriptionTerm = null; descriptionArgs = null;
            LiveOpsLocalKind[] kinds;
            int titleKey, descKey;
            switch (variant)
            {
                case EventPopupVariant.BaseEvent:
                case EventPopupVariant.FullScreenAnnouncement:
                    kinds = new[] { LiveOpsLocalKind.Parent };
                    titleKey = (int)LiveOpsLabelRef.Title;
                    descKey = (int)LiveOpsLabelRef.Description;
                    break;
                case EventPopupVariant.EventHelp:
                case EventPopupVariant.EventHelpSmallImage:
                    kinds = new[] { LiveOpsLocalKind.Parent, LiveOpsLocalKind.Scoring };
                    titleKey = (int)LiveOpsLabelRef.Title;
                    descKey = (int)LiveOpsLabelRef.Help;
                    break;
                case EventPopupVariant.LiveopEventHelp:
                    kinds = new[] { LiveOpsLocalKind.Scoring };
                    titleKey = (int)LiveOpsLabelRef.WelcomeTitle;
                    descKey = (int)LiveOpsLabelRef.Help;
                    break;
                case EventPopupVariant.BaseExpiration:
                    kinds = new[] { LiveOpsLocalKind.Scoring };
                    titleKey = (int)LiveOpsLabelRef.EndTitle;
                    descKey = (int)LiveOpsLabelRef.EndDescription;
                    break;
                default:
                    kinds = new LiveOpsLocalKind[0];
                    titleKey = descKey = -1;
                    break;
            }
            if (kinds.Length == 0) { ev = null; why = "未知变体 " + variant; return false; }

            // 两条 label **各自挑事件**：同一变体的标题/正文可能落在不同事件上（例 `EventHelp` 的 kinds
            // 同时含 Parent/Scoring，而 Parent 没有 label 12）—— 各挑「有这一条」的在期事件，
            // 与原版「同一个 liveOps 事件取两条 label」的差别如实记在这里：**今天的等价物**。
            bool any = false;
            LiveOpsLocalEvent tev = null, dev = null;
            string tWhy = null, dWhy = null;
            LiveOpsLocalLabel l;
            if (titleKey >= 0)
            {
                if (TryPickEvent(kinds, utc, e => e.HasLabel(titleKey), out tev, out tWhy)
                    && tev.TryGetLabel(titleKey, out l))
                {
                    titleTerm = l.Term; titleArgs = ResolveArgs(l.Args); any = true;
                }
            }
            if (descKey >= 0)
            {
                if (TryPickEvent(kinds, utc, e => e.HasLabel(descKey), out dev, out dWhy)
                    && dev.TryGetLabel(descKey, out l))
                {
                    descriptionTerm = l.Term; descriptionArgs = ResolveArgs(l.Args); any = true;
                }
            }
            ev = tev ?? dev;
            if (!any)
                why = (titleKey >= 0 ? "标题：" + tWhy + "；" : "")
                    + (descKey >= 0 ? "正文：" + dWhy : "");
            else why = null;
            return any;
        }

        /// <summary>在期的 `Premium Item Offer`（`BaseOfferPopup` 的出厂值用）：标题/副标题/正文**已经填好 `{0}`**。
        /// 取不到 ⇒ false（调用方保留 prefab 出厂英文，**⛔ 不编价签/文案**）。</summary>
        public static bool TryResolvePremiumOffer(DateTime utc, out string title, out string subtitle,
                                                  out string description, out LiveOpsLocalEvent ev)
        {
            title = subtitle = description = null;
            string why;
            if (!TrySelectActive(LiveOpsLocalKind.PremiumItemOffer, utc, out ev, out why))
            {
                // 「取不到就出声」—— **每次进程只第一次**（`why` 里带时刻，不能拿整串做去重键），
                // ⛔ 不改画面：调用方仍用 prefab 出厂英文。
                if (!_warnedNoPremiumOffer)
                {
                    _warnedNoPremiumOffer = true;
                    Debug.LogWarning("[LiveOpsLocal] `Premium Item Offer` 目录里没有在期事件（" + why
                                     + "）⇒ 报价三栏保留 prefab 出厂英文"
                                     + "（⛔ 不编、⛔ 不拿过期报价顶；用户边界：过期活动不做）");
                }
                return false;
            }
            LiveOpsLocalLabel l;
            if (ev.TryGetLabel((int)LiveOpsLabelRef.Title, out l)) title = FillArgs(l.Term, ResolveArgs(l.Args));
            if (ev.TryGetLabel((int)LiveOpsLabelRef.SubTitle, out l)) subtitle = FillArgs(l.Term, ResolveArgs(l.Args));
            if (ev.TryGetLabel((int)LiveOpsLabelRef.Description, out l)) description = FillArgs(l.Term, ResolveArgs(l.Args));
            return true;
        }

        /// <summary>在期记分活动的左侧页签名（label 4 = `ExpansionPass/TabTitle`）。取不到 ⇒ `fallback` + 出声一次。</summary>
        public static string ScoringTabTitle(DateTime utc, string fallback)
        {
            LiveOpsLocalSurfaceValue v; string why;
            if (TryResolveSurface(Surface.ScoringTabTitle, utc, out v, out why) && !string.IsNullOrEmpty(v.Text))
                return v.Text;
            WarnOnce("[LiveOpsLocal] 页签名取不到（" + why + "）⇒ 回退「" + fallback + "」"
                     + "（= 原版 `GetLabel(NavigationBarTitle)` 取不到时的空态；⛔ 不编活动文案）");
            return fallback;
        }

        // ============================================================ 词条取值 / 实参
        /// <summary>一条词条的显示值：**手写 `Table` → bulk（`Event_Description/*` 在这里）→ 官方 13 语言表（中/英）**。
        /// 三处都没有 ⇒ 空串 + 出声一次（⛔ 不印键名）。</summary>
        public static string TermText(string term)
        {
            if (string.IsNullOrEmpty(term)) return "";
            if (Loc.HasBulk(term)) return Loc.BulkOr(term, "");
            if (Loc.HasEntry(term)) return Loc.T(term);
            string v = Loc.OfficialCurrent(term);
            if (!string.IsNullOrEmpty(v)) return v;
            // 非中/英档：13 语言那一列（缺列 ⇒ 回退英文列）。
            v = Loc.Lang13Current(term);
            if (!string.IsNullOrEmpty(v)) return v;
            v = Loc.Lang13Of(term, "en");
            if (!string.IsNullOrEmpty(v)) return v;
            WarnOnce("[LiveOpsLocal] 词条 `" + term + "` 在三处表里都没有 ⇒ 留空（⛔ 不印键名、⛔ 不拿别的键顶）");
            return "";
        }

        /// <summary>`LabelArg[]` → `string[]`（`Key` 走 <see cref="TermText"/>、`Value` 原样）。</summary>
        public static string[] ResolveArgs(LiveOpsLocalArg[] args)
        {
            if (args == null || args.Length == 0) return null;
            var r = new string[args.Length];
            for (int i = 0; i < args.Length; i++)
                r[i] = args[i] == null ? "" : (args[i].IsKey ? TermText(args[i].Value) : (args[i].Value ?? ""));
            return r;
        }

        /// <summary>把词条里的 `{0}` / `{1}` 换成实参。**没替换掉的占位符出声**（⛔ 别把 `{0}` 印到界面上）。</summary>
        public static string FillArgs(string term, string[] args)
        {
            string v = TermText(term);
            if (string.IsNullOrEmpty(v) || args == null) return v;
            for (int i = 0; i < args.Length; i++) v = v.Replace("{" + i + "}", args[i] ?? "");
            if (v.IndexOf('{') >= 0 && v.IndexOf('}') > v.IndexOf('{'))
                WarnOnce("[LiveOpsLocal] 词条 `" + term + "` 里还有没替换掉的占位符（" + v + "）"
                         + " —— 原版由 `EventLabel.Args` 填（`LabelArg` argType 0 字面量 / 1 另一条词条）");
            return v;
        }

        // ============================================================ 内部
        static string LabelTerm(LiveOpsLocalEvent ev, int key)
        {
            LiveOpsLocalLabel l;
            return ev.TryGetLabel(key, out l) ? l.Term : null;
        }

        /// <summary>类型筛选（`kinds` 里有没有 `k`）。⚠️ 2026-10-25：从 `static` 放开成 `public static` ——
        /// 挑选规则的唯一实现搬进了 `LiveOpsLocalHandler.SelectInTable`，它要用这两个助手（⛔ 不为它抄第二份）。</summary>
        public static bool KindIn(LiveOpsLocalKind k, LiveOpsLocalKind[] kinds)
        {
            if (kinds == null) return false;
            for (int i = 0; i < kinds.Length; i++) if (kinds[i] == k) return true;
            return false;
        }

        /// <summary>把 `kinds` 拼成一句话（错误文案用）。见 `KindIn` 上的说明（2026-10-25 放开成 public）。</summary>
        public static string KindNames(LiveOpsLocalKind[] kinds)
        {
            if (kinds == null || kinds.Length == 0) return "?";
            var s = "";
            for (int i = 0; i < kinds.Length; i++) s += (i > 0 ? "/" : "") + kinds[i];
            return s;
        }

        static readonly HashSet<string> _warned = new HashSet<string>();
        /// <summary>`Premium Item Offer` 无在期事件那条出声**每次进程只发一次**
        /// （消息里带时刻 ⇒ 不能拿整串当去重键）。`ResetWarnedForTest` 一并复位。</summary>
        static bool _warnedNoPremiumOffer;
        /// <summary>🆕 2026-10-24（E15）：`FastMode（遭遇战）活动表为空` 那条出声**每进程一次**。</summary>
        static bool _warnedNoFastMode;
        static void WarnOnce(string msg)
        {
            if (_warned.Add(msg)) Debug.LogWarning(msg);
        }

        /// <summary>自检用：清空「已经出过声」的去重表（同族先例 `Loc.ResetMissingWarnedForTest`）。</summary>
        public static void ResetWarnedForTest() { _warned.Clear(); _warnedNoPremiumOffer = false; _warnedNoFastMode = false; }

        // ---- 数据表的构造助手（只是让上面那 13 份 SO 读起来像表，不改任何值）----
        // 🔴 **`priority` 起的 8 个形参 = 2026-10-25（R14）追加**，逐个照 SO 同名 JSON 字段抄、⛔ 不换算：
        //   ① priority → SO `priority`
        //   ② showInMainMenu → SO `showInMainMenu`
        //   ③ locationType → SO `location.type`（<see cref="LiveOpsLocationType"/>，只是数字照抄）
        //   ④ locationParentId → SO `location.parent.targetId`（其余情况传 null）
        //   ⑤ forceRun → SO `drawData.ForceRun`
        //   ⑥ showWindowOnTrigger → SO `drawData.ShowWindowOnTrigger`
        //   ⑦ showWindowOnExpire → SO `drawData.ShowWindowOnExpire`
        //   ⑧ hideContainer → SO `drawData.HideContainer`
        // 真值逐份见报告 `资料/完全复刻/报告/R14_LiveOps数据与调度_第2轮.md` §四。
        static LiveOpsLocalEvent E(string eventId, string soName, LiveOpsLocalKind kind,
                                   bool enabled, bool uploadToLive,
                                   long startMs, long endMs, bool expirable, long expireMs,
                                   int priority, bool showInMainMenu,
                                   int locationType, string locationParentId,
                                   bool forceRun, bool showWindowOnTrigger,
                                   bool showWindowOnExpire, bool hideContainer,
                                   LiveOpsLocalLabel[] labels, LiveOpsLocalAsset[] assets,
                                   params KeyValuePair<int, string>[] components)
        {
            return new LiveOpsLocalEvent
            {
                EventId = eventId, SoName = soName, Kind = kind,
                Enabled = enabled, UploadToLive = uploadToLive,
                StartMs = startMs, EndMs = endMs, Expirable = expirable, ExpireMs = expireMs,
                Priority = priority, ShowInMainMenu = showInMainMenu,
                LocationType = locationType, LocationParentId = locationParentId,
                ForceRun = forceRun, ShowWindowOnTrigger = showWindowOnTrigger,
                ShowWindowOnExpire = showWindowOnExpire, HideContainer = hideContainer,
                Labels = labels, Assets = assets, Components = components,
            };
        }

        static LiveOpsLocalLabel L(int key, string term, params LiveOpsLocalArg[] args)
        {
            return new LiveOpsLocalLabel { Key = key, Term = term, Args = args };
        }

        static LiveOpsLocalArg K(string term) { return new LiveOpsLocalArg { IsKey = true, Value = term }; }
        static LiveOpsLocalArg V(string value) { return new LiveOpsLocalArg { IsKey = false, Value = value }; }

        static LiveOpsLocalAsset A(int key, string subObjectName, string guid, string bundle)
        {
            return new LiveOpsLocalAsset { Key = key, SubObjectName = subObjectName, Guid = guid, Bundle = bundle };
        }

        static KeyValuePair<int, string> C(LiveOpsComponentRef key, string guid)
        {
            return new KeyValuePair<int, string>((int)key, guid);
        }

        static SurfaceSpec S(Surface surface, string name, int labelKey, LiveOpsLocalKind[] kinds,
                             string primaryTerm, params string[] alternateTerms)
        {
            return new SurfaceSpec
            {
                Surface = surface, Name = name, LabelKey = labelKey, Kinds = kinds,
                PrimaryTerm = primaryTerm, AlternateTerms = alternateTerms,
            };
        }
    }

    /// <summary>一个承载面在某个时刻解析出来的东西（自检 / 窗口读口）。</summary>
    public sealed class LiveOpsLocalSurfaceValue
    {
        public LiveOpsLocalEvent Event;
        public int LabelKey;
        /// <summary>词条键（`⛔ 不是显示文本`；显示值看 <see cref="Text"/>）。</summary>
        public string Term;
        public LiveOpsLocalArg[] Args;
        public string[] ArgTexts;
        /// <summary>当前语档下的显示文本（缺键 ⇒ 空串）。</summary>
        public string Text;
    }
}
