// CardDetailPopup.cs — 卡组线最后一件：**卡片详情窗**（原版菜单版 `CardDisplayWindow`）
//
// ============================ 出处（唯一正本） ============================
// 🔴 **`资料/阶段二_卡片详情窗_原版规格.md`**（2026-09-24 建，逐节点表 + 三块面板 + 计数条 + 入口链 + 查不到的）。
//
// 两条**与直觉相反**的实证（详见正本 §〇）：
//   ① 这扇窗**不是 `bundle_menus_assets_all` 里的 prefab，是 `mainmenuwarpforge` 场景里的场景对象**
//      （出厂 `m_IsActive=false`），所以「按 prefab 找」是找不到的；
//   ② 🔴 **2026-10-12 就地订正（铁律 5）**：本行原来写「**菜单版 ≠ 战斗版**：菜单卡 523.25×832.75 px
//      （`CardUI (4)` scale 250），战斗 `CardUI Reference` scale 223.14 ⇒ **467×743**，工程里已有的
//      `Battle/CardDisplayWindow.cs` **是战斗那扇**（743px），**不能直接复用**」—— **两处都错**：
//      `CardUI Reference`（scale 223.14）**不属于这扇窗**，它是**多卡展示窗**的模板
//      （`Safe area FrontCanvas/Generic Multi Card Display Combat/Viewport/Content/CardUI Reference`）；
//      而**战斗那扇窗的 5 个卡槽同样是 scale 250、引的还是同一条 clip**
//      ⇒ **两扇窗的卡体是同一个尺寸 523.175×832.825 px**（正本 §〇·2 与 §十·4 的 2026-09-28 订正）。
//      ⇒ 「不能直接复用」这句**不成立**（正因为同源，卡格那几组数已收口到共用件 `Core/CardFan.cs`）。
//
// ============================ 结构（照正本 §一/§二/§三 抄）============================
//   · `Menu Dark Background` rect 比屏幕大（盖住任何画幅）· 色 **(0,0,0,.773)** → **点它关窗**
//     🔴 原版**全树没有关闭钮**（`Close*` 只命中 `BackgroundCloseButton`）⇒ 关 = 点遮罩 / ESC
//   · `Card Display` **584,106 → 1336,974**（752×868）· 卡本体在 `Cards/CardUI (4)/2DCard` = **523.25×832.75**
//   · `LowerSection` **300,921.34 → 1620,1058.34**
//     ├ `FlavourTextBG` 300,900.84→1620,1078.84（原版 `spr=0`、运行时由 `FlavourTextSO` 喂 ⇒ **本地没有**）
//     ├ `LoreText` 335,943.11→1585,1036.58 **fs35 auto[10,35] hAlign=Center**
//       ⚠️ **2026-10-15 就地订正（`A2111` 表三·4）**：本行原来写 `hAlign=Right` —— 与本文件
//       `BuildLoreText` 里那条判据（`m_HorizontalAlignment = 2` = Center）**自相矛盾**。
//       `Right` 是错的，两处摆放**都是 Center**。另外这一格的 fs35 是**菜单版专有**：
//       战斗版那颗是 `fs32 auto[18,32]`（`CardWinBox.LorePxBattle`）—— 判据 → `Core/CardWinBox.cs`。
//     ├ `Voice Over Button` 1650,945.51→1738.66,1034.17（88.655²，`40k_UI_bt_voicelines`）
//     └ `Show Card Text` 181.34,945.51→270,1034.17（88.655²，`40k_UI_bt_eye`）
//   · `Panel`（VLG align4）**1294,146.37 → 1744,903.35**，三块 **x=[1294,1744] 宽 450**、y **146.71 / 411.12 / 673.42**
//     ├ `Crafting Panel`（创建副本）· `Upgrade Panel`（升级）· `Alternate Art Panel`（异画）
//   · `Card Counter` **825.07,829.81 → 1094.93,909.19**（三个 TMP：`x{n}` / `"/ "` / `{n}`）
//   · `Wildcard Segment`（0,71→1920,156）+ `WIldcard Counter`（1470,71→1870,156）
//
// ============================ 没建的 / 我们挑的（逐条出声）============================
//   · **`FlavourTextBG` 那张底图本地没有**（`sprite=0`，预设喂的是 `FlavourTextSO`）⇒ 我们只画字、不画底。
//   · **`Lore Text` 的内容**：原版是**背景故事**；我们引擎里**没有 lore 字段** ⇒ 这里画**卡面效果文字**
//     （= `项目任务.md` §三 第 13-E 条第 8 项那条老账的同一件事），**如实标**。
//   · **升级表数值是我们编的**（原版在服务端 `CardTierConfig`）—— 照实拍那个数据点
//     （等级 3 = 4 张 + 2000 + 500 锻造点）做梯度，见 `CardProgress`。**用户 2026-09-22 已裁决照办。**
//   · **不扣货币**：单机资源固定 2000（同上裁决；⚠️ 2026-10-24 用户把固定值由 9999 改为 2000）⇒ 升级面板里那一格金币**永不动**。
//     🔴 **2026-10-18 就地订正（铁律 5）**：这一段原来记着「两块面板恢复后，那两颗钮本版都不改任何数
//     （升级钮 `Interactable=false`、创建副本只出声）」——**那一整段已过期**，用户当天拍板
//     「照参考图那样布置…并且**通过升级按钮进行不同等级的卡框切换**」⇒ 两颗钮**都接了真机制**：
//       · **创建副本** = 扣 1 张同稀有度通配符（本地库存，初始 99/档、可扣减、落盘）+ `owned +1`；
//       · **升级** = 多余副本够就升一级，**等级 = 卡框档**（`CardView` 把 `CardProgress.Level` 喂给
//         `CardArt.Frame/Sdf` 的 `tier`）⇒ 重画后卡框换成新档；满级 ⇒ `Content` 关 + `No Upgrade Warning` 露。
//     ⇒ 判据与落地见 `CardProgress`（存档段 / `Level` / `Wildcards`）与 `BuildCrafting` / `BuildUpgrade`。
//     🆕 **2026-10-24（主对话裁定 · 铁律 5 订正）**：「创建副本」那条链**补齐了原版的后半段** ——
//       原版 `CraftingPanel.TryCraftingInternal` 是「**发出合成请求之后、同一次调用里**就 `DoVFX()`」
//       ⇒ 我们现在 = `CraftHit` → `OnCraftClicked` → `TryCraft`（真请求：`DoCraft`，扣通配符 + owned+1）
//       → `PlayCraftVfx`（原版 `craftVFXs[]` 的 **按钮 + 前台卡**两件，`WarpforgeUiEffect.PlayWildcardCraft`）。
//       🔴 **失败 ⇒ 不播**（原版失败走 `OnFail_22_1` 的 `ShowErrorMessage(10)`，不调 `DoVFX`）。
//       ⚠️ 2026-10-18 那版「点击但不起作用、保持可点外观」**已作废**；可交互度回到原版
//       `CraftingPanel__CheckButtons.c` 的判据（**有同稀有度通配符才可点**）。详见 `OnCraftClicked`。
//       🆕 **2026-10-24（A6）补上**：原版那**另一支** ——「**传奇 且 `owned == 1` ⇒ 先弹确认窗**、
//       确认前**一件特效都不播**」（`CraftingPanel__TryCrafting.c:26-49`）现在也照做了：
//       `OnCraftClicked` 前加闸（= `TryCrafting` 那一层）→ `ShowMessagePopUp` 那扇 2 按钮窗
//       （左 `Cancel` / 右 `OK`）→ `OK` 才走 `CraftNow()`（= `TryCraftingInternal`）。见 `OnCraftClicked`。
//     ⚠️ **仍然保留的两处「我们编的」**：升级消耗表（`NeedCopies/NeedGold/Points`，原版在服务端
//     `CardTierConfig`；照实拍那个数据点做的梯度，用户 2026-09-22 已裁决照办）· 通配符的**初始**库存
//     （原版是服务端发放，没有本地发放源）。
//   · `Debug Add cards`（开发用）· `Item Information Panel`(act=F) · `Game Mode Icon` 那类 **不建**。
//   · `Alternate Art Panel`：**本地只有 7 张督军异画**（2 种风格）⇒ 不是这张卡就**没有异画可切**
//     （如实说，不给别的督军换 —— 异画**绑死在那张卡上**，见 `CardArt.AltArt` 的注释）。
//     ✅ **2026-10-12（A1699）：左右两颗 `Select Art Button` 已建**（原来缺，见 `BuildAltArrow`）——
//     它们**照原版**在 `0..N` 档之间回绕（0 = 原画）、换的是**前台那张**的立绘
//     （`CardData.artOverride`，与收藏窗 Styles 页同一份表 `CollectionWindow.AltArtCards`）。
//     🔴 **2026-10-17（D17 = 账上 A856）**：原版 `AlternateArtPanel.Initialize` 在**没有异画风格**时
//     **整块 `GameObject.SetActive(false)`**（判据 = `decomp_full/AlternateArtPanel__Initialize.c:22-38`），
//     我们原来**不管有没有都画那一块** ⇒ 已改成「没有 ⇒ 整块关」（**节点留着、只关**，同原版；
//     判据与「为什么判据是数据不是贴图」→ `HasAltArtStyle` / `BuildAltArt` 的注释）。
//     ✅ **2026-10-17 订正（铁律 5）**：本行原来写「原版 `CardDisplayOptions` 在**同一个** `CardDisplayWindow`
//     prefab 上、战斗侧 `showOptions` 传 `DisplayCardEffects(...)` ⇒ **原版战斗侧可能出这块、我们是缺口**」
//     —— 🔴 **记反了**。逐字段现核：**13 个竞技场**的 `CardDisplayWindow.options` **全是 `{0,0}` = null**
//     （`informationPanel` / `tryShowResources` 同样 13/13 为 0），**只有菜单版有**
//     （`bundle_scenes_scenes_mainmenuwarpforge`）；而 `CardDisplayWindow__ShowCard.c:159` 那句守卫
//     （`if (options == null || …) goto …`）⇒ **战斗侧永远不出这一块**。`ShowBattleCard` 传的那个
//     `showOptions` 在战斗侧是**空转**（它真正切的是 `cardEffectsGroup`）。⇒ **我们那扇不建 = 与原版一致**
//     （**不是缺口**）；同族的眼睛钮恰好相反（原版战斗版也没有、**是我们多建了一颗** ⇒ 已删）。
//     判据全文 → `资料/普查产出_1017/W_B4_战斗详情窗.md`（+ 六条断言在 `Editor/BattleScene.cs` 的 §A860）。
using System.Collections.Generic;
using DG.Tweening;          // 换位那两条补间（`CardView.TweenPose` 返回 `Tween`，旧注释写的 `CardTween.ToPose` 已于 2026-10-24 `A11` 退役）
using UnityEngine;
using RuleEngine;

namespace CardPresentation
{
    /// <summary>🆕 2026-10-18：`CardProgress` 存档的**一条**（`id` → 值）。`JsonUtility` 不支持字典 ⇒ 用数组。</summary>
    [System.Serializable]
    public class CardProgressEntry
    {
        public string id;
        public int v;
    }

    /// <summary>🆕 2026-10-18：`CardProgress` 的存档根（`Application.persistentDataPath/WarpforgeCardLevels.json`）。
    /// 形状与为什么这么存 → `CardProgress` 里「存档」那一段的注释。</summary>
    [System.Serializable]
    public class CardProgressSave
    {
        public int version = 1;
        public List<CardProgressEntry> levels = new List<CardProgressEntry>();
        public List<CardProgressEntry> owned = new List<CardProgressEntry>();
        public List<CardProgressEntry> wildcards = new List<CardProgressEntry>();
    }

    /// <summary>单机口径的卡牌进度（拥有数 / 等级）。**升级表数值是我们编的**，见文件头。</summary>
    public static class CardProgress
    {
        /// <summary>升到第 i 级**累计**要几张副本（下标 = 等级）。`MaxLevel = 4`。</summary>
        public static readonly int[] NeedCopies = { 0, 0, 2, 5, 9 };
        /// <summary>升到第 i 级要多少金币（**我们编的，不是原版**）。</summary>
        public static readonly int[] NeedGold = { 0, 0, 500, 1000, 2000 };
        /// <summary>升到第 i 级给多少锻造点（照实拍那个数据点做的梯度；**我们编的**）。</summary>
        public static readonly int[] Points = { 0, 0, 100, 250, 500 };
        public const int MaxLevel = 4;

        /// <summary>能放进卡组的张数上限（规则书：经典 **传说 ≤1**、普通/稀有/史诗 ≤2）。
        /// ⚠️ **不带卡型这一档 = 「非督军」档**（等价于 <c>DeckCap(rarity, null)</c>）。
        /// 一旦手上拿得到 <c>CardDef.Type</c>、而且那张卡可能是督军，就**必须**走下面那个重载 ——
        /// 🔴 判据 = 原版 `GetMaxCopiesInDeck` 那句 `if (cardType == 10) return 1;`（**在稀有度之前**）：
        /// `d:/2/tools/decomp_full/Everguild.LiveOps.GameplayVariablesData__GetMaxCopiesInDeck.c:7-9`。
        /// 我们池子里 **28 位督军不是传说**（epic 15 / rare 13）⇒ 只看稀有度会把它们算成 2（A894）。</summary>
        public static int DeckCap(string rarity) { return DeckCap(rarity, null); }

        /// <summary>能放进卡组的张数上限 —— **带卡型那一档**（判据同 <see cref="DeckRules.CopyLimit(string,string)"/>）。
        /// 🔴 规则**只有一处实现**：转发给 `DeckRules.CopyLimit`（`RuleEngine/Core/DeckRules.cs`）；
        /// ⛔ 别在这里再写一份 `rarity == "legendary" ? 1 : 2`（本工程「两处写同一条规则 = 迟早不一致」）。</summary>
        public static int DeckCap(string rarity, string type) { return DeckRules.CopyLimit(rarity, type); }

        static readonly Dictionary<string, int> _owned = new Dictionary<string, int>();
        static readonly Dictionary<string, int> _lvl = new Dictionary<string, int>();
        /// <summary>通配符库存（键 = **小写稀有度**：`common` / `rare` / `epic` / `legendary`）。
        /// 缺省 = <see cref="WildcardsPerRarity"/>。🆕 2026-10-18（见下面「存档」那一段）。</summary>
        static readonly Dictionary<string, int> _wild = new Dictionary<string, int>();

        // ============================================================ 🆕 2026-10-18：**存档**（等级 / 拥有数 / 通配符）
        //
        // 🔴 **起因**：用户当天拍板「照参考图那样布置『创建副本 / 升级』两块面板」+
        //   「**通过升级按钮进行不同等级的卡框切换**」⇒ 等级与通配符库存必须有落盘，
        //   否则「升了一级 / 扣了一张通配符」**关掉游戏就没了**（静默丢数据，红线）。
        //
        // 文件：`Application.persistentDataPath/WarpforgeCardLevels.json`（**玩家存档位**，不进 git）。
        // 形状（`version` 留给以后迁移；读档时只认三个数组）：
        //   { "version": 1,
        //     "levels":    [ { "id": "UM82", "v": 2 } ],
        //     "owned":     [ { "id": "UM82", "v": 12 } ],
        //     "wildcards": [ { "id": "epic", "v": 98 } ] }
        // ⚠️ **不用字典**：`JsonUtility` 不支持 `Dictionary` ⇒ 落成 `id/v` 数组（这是它的既有约束，不是我们挑的）。
        // ⚠️ **`owned` 也一起存** —— 不存的话「创建副本 +1」重开游戏就回滚，那条链等于没做。
        //   它只在**首次**由 `Owned()` 按「给足」口径算出初值，之后以存档为准。
        // ⚠️ 读写失败**不抛**：`LogWarning` 出声 + 用缺省值继续（同 `CardArt` 那套「没有美术也照跑」）。
        // 🔴 **自检里关掉落盘**：`ResetForTest()` 会把 `_persist` 置 false 并跳过读盘 —— 否则跑一次
        //   自检就会改写开发机上的玩家存档，而且下一次自检会被上一次的值污染（不可复现）。
        // ⚠️ 两个容器类放在**文件级**（不是嵌在 `CardProgress` 里）—— `JsonUtility` 对**嵌套类型**
        //   的支持在各 Unity 版本上不一致，放平最保险（它们不是 `MonoBehaviour`，不需要同名文件）。

        /// <summary>存档文件名（**只此一处**；路径 = `Application.persistentDataPath`）。</summary>
        public const string SaveFileName = "WarpforgeCardLevels.json";

        static bool _loaded;        // 懒加载过没有（无论读盘成功与否）
        static bool _persist = true; // 自检里置 false（见上面那一段）

        static string SavePath { get { return System.IO.Path.Combine(Application.persistentDataPath, SaveFileName); } }

        static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            if (!_persist) return;
            try
            {
                var path = SavePath;
                if (!System.IO.File.Exists(path)) return;
                var d = JsonUtility.FromJson<CardProgressSave>(System.IO.File.ReadAllText(path));
                if (d == null) return;
                if (d.levels != null) foreach (var e in d.levels) if (e != null && !string.IsNullOrEmpty(e.id)) _lvl[e.id] = e.v;
                if (d.owned != null) foreach (var e in d.owned) if (e != null && !string.IsNullOrEmpty(e.id)) _owned[e.id] = e.v;
                if (d.wildcards != null) foreach (var e in d.wildcards) if (e != null && !string.IsNullOrEmpty(e.id)) _wild[e.id] = e.v;
                Debug.Log("[CardProgress] 读档成功：" + path + "（等级 " + _lvl.Count + " 条 · 拥有数 " + _owned.Count
                        + " 条 · 通配符 " + _wild.Count + " 条）");
            }
            catch (System.Exception ex)
            {
                // 文件坏了 / 权限不够 ⇒ **不崩**，按缺省走，并且出声（不静默）
                Debug.LogWarning("[CardProgress] 等级存档读不出（" + SavePath + "）：" + ex.Message
                               + " ⇒ 按缺省（等级 1 / 拥有数给足 / 通配符 " + WildcardsPerRarity + "）继续");
            }
        }

        static void Save()
        {
            if (!_persist) return;
            try
            {
                var d = new CardProgressSave();
                foreach (var kv in _lvl) d.levels.Add(new CardProgressEntry { id = kv.Key, v = kv.Value });
                foreach (var kv in _owned) d.owned.Add(new CardProgressEntry { id = kv.Key, v = kv.Value });
                foreach (var kv in _wild) d.wildcards.Add(new CardProgressEntry { id = kv.Key, v = kv.Value });
                System.IO.File.WriteAllText(SavePath, JsonUtility.ToJson(d, true));
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[CardProgress] 等级存档写不出（" + SavePath + "）：" + ex.Message
                               + " ⇒ 本次改动只在内存里（关掉游戏会丢）");
            }
        }

        /// <summary>拥有数。**口径「给足」**（用户 2026-09-22 裁决）：够 `卡组上限 + 升满所需`。
        /// <para>🔴 **2026-10-17（A894）卡型那一档的残留（如实标注，别当成已做）**：本函数**吃不到
        /// `CardDef.Type`**（缓存 `_owned` 按 id 存、`AddOwned` 那一路也没有卡型），所以这里的「卡组上限」
        /// 走的是**不带卡型**那一档 ⇒ **对 28 张非传说督军，`Owned` 会比「按督军档」多 1**（11 而不是 10）。
        /// 要真收口得让 `Owned/AddOwned/Spares/CanUpgrade` 这一族都带上卡型（= 要么改签名、
        /// 要么按 id 反查 `CardDef`）—— **不在 A894 白名单里，已记进
        /// `资料/普查产出_1017/W_B31_徽记与上限.md`**。⚠️ 但**印在卡面/卡片详情窗上的那个数
        /// （`min(拥有, 卡组上限)`）是对的** —— 它的**上限**那一项已经走 `DeckCap(rarity, type)`。</para></summary>
        public static int Owned(string id, string rarity)
        {
            EnsureLoaded();
            int v;
            if (_owned.TryGetValue(id, out v)) return v;
            v = DeckCap(rarity) + (NeedCopies[MaxLevel] - NeedCopies[1]);
            _owned[id] = v;
            return v;
        }
        public static void AddOwned(string id, string rarity, int n)
        {
            _owned[id] = Owned(id, rarity) + n;
            Save();                                   // 🆕 2026-10-18：落盘（见「存档」那一段）
        }
        /// <summary>这张卡的等级 —— **等价于原版 `RawCardScript.get_CardTier()` 的 1 基写法**（1..`MaxLevel`）。
        ///
        /// <para>🔴 **2026-10-18 改判：缺省 = 1（不再是 `MaxLevel`）。** 用户当天拍板
        /// 「照参考图布置『创建副本 / 升级』两块面板」+「**通过升级按钮进行不同等级的卡框切换**」——
        /// 缺省若仍是 4，**每张卡一上来就满级** ⇒ `CanUpgrade` 恒假 ⇒ 升级钮永远是灰的、
        /// 卡框也永远只有 tier4 一档，「升级换框」这条链**根本走不到**。
        /// ⇒ 按原版口径定案：**新卡一律 Tier1**（`CardTier{Tier1=0..Tier4=3}`，显示等级 = 内部值 + 1；
        /// 判据 `UpgradePanel__CheckUpgradeRequirements.c` 的 `CardTier + 2` = 「升到第 N 级」）。</para>
        /// <para>**历史（保留是为了别再翻回去）**：2026-10-12 用户曾裁定「默认最高级」⇒ 缺省 1 → `MaxLevel`，
        /// 理由是当时**不做升级机制**、不想让面板印出「升级到第 2 级」而后面的卡框却永远是最高档 ——
        /// 那是「两处数打架」的权宜之计。2026-10-18 升级真做了、卡框也真跟着等级走了 ⇒ **那个前提消失**，
        /// 两处现在是**同一个数**（`CardView` 直接把本函数喂给 `CardArt.Frame/Sdf` 的 `tier` 形参）。</para>
        ///
        /// <para>⚠️ **存档**：2026-10-18 起有落盘（`WarpforgeCardLevels.json`，见上面「存档」那一段）。
        /// 读档失败 / 没有存档 ⇒ 回这个缺省 1。写入点 = `SetLevel`（唯一调用方 `DoUpgrade`）。</para>
        ///
        /// <para>⚠️ **连带后果（都已核实过）**：缺省 1 之后 `CanUpgrade` 在「给足」口径下（`Spares` = 9）
        /// **恒真** ⇒ `BoosterPackOpenWindow.ReadyForLevelUp`（开包那枚「可升级」角标）**由恒假变回可按**。</para></summary>
        public static int Level(string id)
        {
            EnsureLoaded();
            int v;
            if (string.IsNullOrEmpty(id)) return 1;
            return _lvl.TryGetValue(id, out v) ? Mathf.Clamp(v, 1, MaxLevel) : 1;
        }
        public static void SetLevel(string id, int l)
        {
            if (string.IsNullOrEmpty(id)) return;
            _lvl[id] = Mathf.Clamp(l, 1, MaxLevel);
            Save();                                   // 🆕 2026-10-18：落盘
        }

        // ============================================================ 🆕 2026-10-18：**通配符库存**
        //
        // 原版：创建副本消耗**同阵营同稀有度 1 张通配符**（`CardServices__CheckForCardCrafting.c`：
        //   按 army+rarity 找通配符且数量 > 0；`CraftingPanel__CheckButtons.c`：有价但没通配符 ⇒ 钮 `interactable=false`）。
        // 我们：**没有发放源**（原版那套走服务端）⇒ 给每档一个**初始库存**、之后**可扣减**并落盘。
        //   ⛔ 这不是「伪造服务端」：初始值是我们挑的（= 本工程「资源给足」那条单机口径的同一档做法），
        //   扣减是真扣、扣到 0 钮就灰（与原版同一支）。

        /// <summary>每档稀有度的**初始**通配符库存（= 2026-09-28 用户拍板那个 `99` 的口径）。
        /// 只作「没有存档时的初值」，之后以 <see cref="Wildcards"/> 的实读为准。</summary>
        public const int WildcardsPerRarity = 99;

        /// <summary>这张卡那种稀有度的通配符还有几张（**只此一处**）。稀有度大小写不敏感。</summary>
        public static int Wildcards(string rarity)
        {
            EnsureLoaded();
            int v;
            var k = (rarity ?? "").ToLowerInvariant();
            return _wild.TryGetValue(k, out v) ? Mathf.Max(0, v) : WildcardsPerRarity;
        }

        /// <summary>扣 1 张通配符；不够返回 false（**不改任何数**）。点了「创建副本」成功时才调。</summary>
        public static bool TrySpendWildcard(string rarity)
        {
            int have = Wildcards(rarity);
            if (have <= 0) return false;
            _wild[(rarity ?? "").ToLowerInvariant()] = have - 1;
            Save();
            return true;
        }

        /// <summary>多余副本数 = 拥有 − min(拥有, 卡组上限)（**原版 `CardCounterDisplay.Initialize` 的判据**）。</summary>
        public static int Spares(string id, string rarity)
        {
            int o = Owned(id, rarity), cap = DeckCap(rarity);
            return o - Mathf.Min(o, cap);
        }
        /// <summary>升到下一级够不够（**只吃多余副本**，同原版）。</summary>
        public static bool CanUpgrade(string id, string rarity)
        {
            int l = Level(id);
            return l < MaxLevel && Spares(id, rarity) >= NeedCopies[l + 1];
        }

        /// <summary>自检用：清空内存态，**并且从此不读盘、不落盘**（见上面「存档」那一段 ——
        /// 自检绝不能改写开发机上的玩家存档，也不能被上一次的值污染）。
        /// 🆕 2026-10-18：`_wild` 一起清；`_loaded = true` 表示「已按出厂态处理、不必再读盘」。</summary>
        public static void ResetForTest()
        {
            _owned.Clear(); _lvl.Clear(); _wild.Clear();
            _persist = false;      // 本进程后续所有 `Save()` 变成空操作
            _loaded = true;        // 跳过 `EnsureLoaded()` 的读盘
        }

        /// <summary>🆕 **2026-10-24**：自检用 —— 把某一档通配符库存**设成绝对值**
        /// （用来摆「0 张」那一档，验「没有万能卡 ⇒ **不许播特效**」，见 `Editor/CollectionScene.cs`）。
        /// <para>与 <see cref="ResetForTest"/> 同一族（那个管清零 / 关落盘，本方法管摆单档）；
        /// ⚠️ 只有 `Editor/**` 的自检该调它 —— ⛔ 别在运行时路径里拿它当「发卡 / 发通配符」的发放源
        /// （通配符的发放源在原版是服务端，我们这边如实标注为「初始 99/档」）。</para>
        /// <para>不落盘（`Save()` 不在这里调）：自检里 `_persist` 已是 false；就算不是，摆库存也不该改玩家存档。</para></summary>
        public static void SetWildcardsForTest(string rarity, int n)
        {
            _wild[(rarity ?? "").ToLowerInvariant()] = Mathf.Max(0, n);
        }
    }

    /// <summary>`CardDetailPopup : GameWindow` —— 点一张卡开的那扇详情窗（原版菜单版 `CardDisplayWindow`）。</summary>
    public class CardDetailPopup : GameWindow
    {
        // 层：**高于收藏窗那一整片**（它最高到 `CollectionWindow.QFltHit = 3043`）；
        //    低于 `Deck info Popup`(3120+) / `Deck Selection Popup`(3125+) / `PromptPopup`(3140+)
        //    —— 提示窗要能压在**所有**窗之上。
        //
        // 🔴 **2026-09-28 重排（原来画错了）**：原来是 `QCd = 3110` 而卡格在 `3009–3017`
        //    ⇒ **遮罩画在卡格【之上】⇒ 整叠卡被压暗了一半**（拿实拍量出来的：前台卡名峰值
        //    只有 131/255，而原版同一处是 232/255 —— 131 正好 = 白 ×(1−0.773) 在**线性空间**下
        //    编码回 sRGB；判据全文 → 正本 §十·5）。
        //    原版那棵树的**兄弟序**是 `Menu Dark Background`(0) → … → `Card Display`
        //    ⇒ **遮罩在最底下**。现在三段分明：
        //    **遮罩 3105 < 卡格 3106–3114 < 本窗的钮/字/面板 3115+**，整段仍在收藏窗与卡组编辑之上。
        // ✅ **2026-10-11（A252）可见性收窄**：这两行原是 2026-10-04（A47 接线批）**整行**放宽成 `public` 的；
        //   本件只留**本窗之外真被引用**的那一个 —— `QCdHit`（`Editor/CollectionScene.cs` 的卡片详情自检）。
        //   量法（可复跑）：脚本扫全工程 **301** 个 `.cs` 的限定名 `CardDetailPopup.<常量>`、**剔注释**、
        //   剔本文件；另核过两件会「看着对、其实编不过」的事：① 同文件里的另一个类 `CardProgress` **不**用它们；
        //   ② **没有任何类从本窗派生**（`class X : CardDetailPopup` 全 0）—— 所以「回 `const`」是安全的。
        const int QShade = 3105;            // 3105 整屏遮罩（`Menu Dark Background`）
        const int QCardStack = 3106;        // 3106 卡格那一叠（`Card Display`）
        const int QCd = 3115;               // 3115 本窗的钮 / 字 / 面板
        const int QCdRow = 3116;
        const int QCdText = 3117;
        public const int QCdHit = 3118;     // ✅ 留 `public`：`Editor/CollectionScene.cs` 的自检引用（1 处）
        // 🆕 **2026-10-12（A1699）`QCdIcon = 3119`**：只给**异画那两颗圆钮的箭头**用。
        //   为什么要单开一档：那两颗钮是**三层同心**（圆底盘 74.39² / 黄底 56.86×58.13 / 箭头同框），
        //   而本工程**透明件同渲染队列时「谁盖谁」不可控**（按到相机的 3D 距离排，见 `CLAUDE.md` §三
        //   与 `资料/已知的坑.md`）⇒ 三层必须**各占一个递增的队列**。
        //   同族的现成先例 = 收藏窗那两颗圆钮（`CollectionWindow.BuildStyleArrow`：圆底 `QPagePanel`
        //   < 黄底 `QPageRow` < 箭头 `QPageOverlay`，注释里记着「同队列会把箭头盖掉、而矩形断言全绿」）。
        //   ⚠️ 它**高于命中区那一档**（3118）是**有意的**：命中区那颗 quad 是 `alpha = 0` 的透明件
        //   （见本文件的 `Hit(...)`），画面的次序与它无关；`PointerLayer` 只按「注册成命中区的那些 quad」
        //   挑，所以下面那颗 `ArrowHit` 照样被点得到。
        const int QCdIcon = 3119;

        // ---- 几何（**两处摆放共用一份** → `CardWinBox`；本文件只留别名，值不再各写一套）----
        public static readonly Color ShadeColor = CardWinBox.ShadeColor;
        public const float CardL = CardWinBox.CardL, CardT = CardWinBox.CardT;
        public const float CardR = CardWinBox.CardR, CardB = CardWinBox.CardB;   // `Card Display` 752×868
        public const float LowerT = CardWinBox.LowerT, LowerB = CardWinBox.LowerB;
        public const float LowerL = CardWinBox.LowerL, LowerR = CardWinBox.LowerR;
        public const float LoreBgT = CardWinBox.LoreBgT, LoreBgB = CardWinBox.LoreBgB;
        public const float LoreL = CardWinBox.LoreL, LoreT = CardWinBox.LoreT;
        public const float LoreR = CardWinBox.LoreR, LoreB = CardWinBox.LoreB, LorePx = CardWinBox.LorePx;
        /// <summary>两个圆钮各 **88.655²**（`pivot` 在 LowerSection 内水平两端 ±734.33）。</summary>
        public const float BtnS = CardWinBox.BtnS;
        public const float VoiceCx = CardWinBox.VoiceCx, EyeCx = CardWinBox.EyeCx, BtnCy = CardWinBox.BtnCy;
        /// <summary>`Panel`（VLG align4）与三块面板的 y（**布局组跑之前的模板位按 ±1px 抄**，正本 §二）。</summary>
        public const float PanL = 1294f, PanR = 1744f, PanT = 146.37f, PanB = 903.35f;
        public const float CraftT = 146.71f, CraftB = 410.44f;
        public const float UpgT = 411.12f, UpgB = 672.74f;
        public const float AltT = 673.42f, AltB = 903.01f;
        /// <summary>🆕 **2026-10-18（用户拍板）**：右栏三块面板之间的**统一间距**（设计 px）。
        ///
        /// <para>用户原话：「**创建副本和升级那两个方框，应该保持一定的距离。对于有异画的战将那里
        /// 有三个方框，它们的距离也要保持好。**」⇒ 三块**两两同距**，顺序/高度都不动。</para>
        ///
        /// <para>**判据来源（两路都量过，取哪一路写在下面）**：
        /// ① **参考图**（`C:/Users/qjh36/Desktop/warpforge参考图/点击卡片查看详情的参考.png`，1577×887，
        ///    设计 1920×1080 ⇒ 系数 1920/1577 = **1.2175**）—— 逐行扫「面板色」得到**可见边框**：
        ///    上面那块 `y 186..382` 图像 px、下面那块 `y 469..678` ⇒ 可见空隙 **87 图像 px ≈ 106 设计 px**
        ///    （≈ 面板高的 0.43 倍；两块可见高 196/210 图像 px = 239/256 设计 px）。
        /// ② **prefab 三组 rect**（`Crafting Panel 146.71..410.44` · `Upgrade 411.12..672.74` ·
        ///    `Alternate Art 673.42..903.01`）—— 两两只差 **0.68 px** ⇒ 那三组是**外框（含阴影/边饰）**，
        ///    可见框在外框之内、彼此**贴着**（就是用户看到的「两个方框黏在一起」）。
        /// 🔴 **为什么不直接取 106**：用户同一条要求里还有「**高度不变**」，三个高度和 = **754.94**，
        ///    再塞两条 106 ⇒ 第三块底部 = 146.71 + 754.94 + 212 = **1113.65 > 1080**（出血到屏幕外）。
        /// ⚠️ **本值 40 是折中（如实标注，⛔ 不冒充原版）**：第三块底部 = **983.01**，
        ///    离屏顶 146.71 · 离屏底 **97**（上下留白接近）；两两间距都是 **40.68**（= 40 + 原来那 0.68 外框差）。
        ///    真要做到参考图那一档，得先允许压面板高度（那是另一条用户口径，⛔ 本笔不擅自改）。</para></summary>
        public const float PanelGap = 40f;
        /// <summary>🆕 **2026-10-12（A1699）异画面板那两颗换画圆钮**（`Select Art Button Left/Right`）。
        ///
        /// <para>🔴 **尺寸与位置取自【运行时实况 dump】**（`资料/原版实拍/menu_forge_1016/runtime_ui_dump_menu_forge.tsv:154,157`）——
        /// 场景 JSON 那条路**量不到**：`Alternate Art Panel` 整条父链的 `m_LocalScale = 0,0`
        /// （动画/隐藏态，`工具/menu_rect.py` 实测全零，正本 §六·3 也记了这一条）
        /// ⇒ 照铁律 4「**实况与解包字段冲突时以实况为准**」。
        /// 实读（`pos` 列 = `m_AnchoredPosition`，锚点/pivot 都是 `(0.5,0.5)` ⇒ 相对**面板中心**）：
        /// `Right` = `(171.9, 65.0)` · `Left` = **`(−177.1, 65.0)`** · 两者 `m_SizeDelta` 都是 `(74.4, 75.6)`。
        /// ⚠️ **那两个 x 不对称**（−177.1 / +171.9，差 4.2px）—— **原版就是这样，别「顺手对称化」**。
        /// 子件 `Background` / `Icon` 是 **stretch 锚点**（`(0.114,0.124)-(0.873,0.888)` + `sd (0.4,0.4)`、
        /// `pos (−0.1, 0.3)` ≈ 居中）⇒ 解出来 **56.86×58.13**，与收藏窗那两颗圆钮的
        /// `CollectionWindow.StyleArrowIconW/H = 56.86 / 58.13` **同值**（同一件预制体）。
        /// 贴图三层（实读同两行）：根节点自己 = `UI_Button_Round_background`
        /// （Simple + `preserveAspect`、sprite 237² ⇒ `PA=1` 内接画 74.39²）·
        /// 子件 `Background` = `40k_general_bt_yellow` · 子件 `Icon` = `40k_general_bt_arrow`。</para></summary>
        public const float AltBtnW = 74.39f, AltBtnH = 75.61f;
        public const float AltArrowW = 56.86f, AltArrowH = 58.13f;
        /// <summary>两颗钮的**中心**（屏 px；由本窗自己的 `Panel` 矩形 + 上面那两个原版 offset 解出）。
        /// 换算只有这一处 —— 下面的 `BuildAltArrow` 与将来任何断言都读它。</summary>
        public const float AltBtnCxL = (PanL + PanR) * 0.5f - 177.1f;   // 1341.9
        public const float AltBtnCxR = (PanL + PanR) * 0.5f + 171.9f;   // 1690.9
        public const float AltBtnCy = (AltT + AltB) * 0.5f - 65.0f;     // 723.215
        /// <summary>🆕 **2026-10-14（账 `A1837`）**：`Alternate Art Panel` 的 **`Current Style`** 那一格 ——
        /// 原版喂的是**图**（`AlternateArtPanel` 的 `+0x30 styleImageContainer`，一颗 `private Image`），
        /// 我们原来印的是**文字**「风格名 (N/M)」（**我们挑的** → `资料/普查产出_第二十三会话/YI_恢复两块面板与异画钮.md` §七·3）。
        ///
        /// <para>**矩形判据（三路互证，全部 2026-10-14 现读；全文 → `资料/普查产出_第二十九会话/A1837_风格格换图.md` §一）**：
        /// ① **场景资产** `bundle_scenes_scenes_mainmenuwarpforge/RectTransform/RectTransform_1309.json` ——
        ///    经「`AlternateArtPanel` 的 `styleImageContainer` PPtr `2237` → `GameObject/Current Style.json` 的
        ///    `m_Component=[1309,1039,2237]` → `RectTransform_1309.m_Father=1284`（= `Alternate Art Panel`）」
        ///    这一条链闭合对上：`anchor (0.5,0.5)` · **`aPos (0, −26.8)`** · **`sd (406.022, 101.505)`** ·
        ///    `pivot (0.5,0.5)` · `m_Children` 空；
        /// ② 🔴 **运行时实况 dump**（铁律 4 的第一手）`资料/原版实拍/menu_forge_1016/runtime_ui_dump_menu_forge.tsv:151`：
        ///    `…/Panel/Alternate Art Panel ▸ Current Style ▸ pos 0.0,−26.8 ▸ rect 406.0,101.5 ▸ IMG`；
        /// ③ **树 dump** `资料/说明书/04_界面UI/主菜单全树.md:157` 的 `Current Style [356,1304 406x102]`
        ///    （那份 dump 以**屏心为原点** ⇒ `x_abs = x + 960` · `y_顶向下 = y − 540`；本笔在 7 个节点上验过）。
        /// ⇒ 屏（1920×1080 · **左上原点 · y 向下**）：**x 1315.989 → 1722.011 · y 764.264 → 865.769**。</para>
        /// <para>**图档照原版**：`m_Type = 0 (Simple)` · **`m_PreserveAspect = 0`**（`MonoBehaviour_2237.json` 实读
        /// —— ⛔ **别按习惯开 PA**：这一格是**固定矩形画**；`A1808` §三·2 当时记的「PA 值没查」本笔销掉）。
        /// ⚠️ 框比 `4.00002` ≈ 图比 `4.0` ⇒ 开不开 PA 画出来**逐位相同**，但**原版值就是不收边**，别写成 `true`。</para>
        /// <para>📌 它与那颗价签（`Buy Original Card Button`）**中心逐位相同**（都是 `(1519, 815.017)`）——
        /// 不是巧合：原版这两件**互斥**（见 `BuildAltArt` 里 `canBuyOriginal` 那一段）。</para></summary>
        public const float AltStyleImgL = 1315.99f, AltStyleImgT = 764.26f,
                           AltStyleImgR = 1722.01f, AltStyleImgB = 865.77f;
        /// <summary>`Card Counter` 825.07,829.81→1094.93,909.19；`Duplicate Counter` 底 840,834.21→1080,904.79。</summary>
        public const float CcBgL = 840f, CcBgT = 834.21f, CcBgR = 1080f, CcBgB = 904.79f;
        public const float CcX1L = 869.41f, CcX1R = 942.69f, CcY1 = 846.61f, CcY2 = 917.19f, CcX1Px = 52.6f;
        public const float CcSlL = 942.69f, CcSlR = 970.71f, CcSlPx = 57f;
        public const float CcX2L = 970.71f, CcX2R = 1014.80f, CcX2T = 856.90f, CcX2B = 906.90f;
        public const float CcImL = 1014.80f, CcImR = 1056.74f, CcImT = 853f, CcImB = 903f;
        /// <summary>🔴 **2026-10-13（A404）**：三颗计数 TMP 的**自适应容器宽** = 原版那一颗的 `m_SizeDelta.x`
        /// （TMP 的折行/自适应容器就是这个 `sizeDelta`；判据 → `资料/阶段二_卡片详情窗_原版规格.md:90-92` 的 `sd`）。
        /// ⚠️ **「容器 = `m_SizeDelta.x`」只对【点锚点】的件成立** —— TMP 真读的是**解析后的 `rect.width`**
        /// （`ComputeMarginSize`），stretch 的件（anchors `(0,0)-(1,1)`、`sd = 0`）容器 = **父宽**。
        /// 本颗（`Duplicate Counter` 那三件）都是点锚点 ⇒ 这两个数同值；⚠️ `Single Counter` 那颗**不是**（见 `CcSgWrapX`）。
        /// ⚠️ 与上面那三个 rect **不是同一个数**：rect 由 `CcX1L/R` 等给出（原版坐标只抄到 0.01 ⇒ 宽 73.28），
        /// 而原版 `sd.x` 是 **73.283**（差 0.003px）—— 这里取**原版 `sd`**（喂 TMP 容器的是它，不是画 rect 那个）。</summary>
        public const float CcWrapX1 = 73.283f, CcWrapSl = 28.02f, CcWrapX2 = 44.09f;
        /// <summary>🔴 **2026-10-13（A574）**：三颗计数 TMP 的 **`m_fontSizeBase`**（= 自适应二分的起点；
        /// 与 `autoMinPx` / `wrapPx` **同量纲** = 画布 px）。逐颗现读原版 MB：
        /// `Counter` **36.0**（pid `1892`）· `Slash` **36.0**（pid `1848`）· `Duplicates text` **35.0**（pid `1823`）
        /// （`d:/2/新解包资源/assets_full/bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/`）。
        /// ⚠️ **三颗不是同一个数**（36 / 36 / 35）—— 铁律 5·c：别拿一个顶其余的。
        /// ⛔ 不传（= 原来那样）⇒ base 落到「调用方那一档」的标称字号（52.6 / 57 / 52.6）；
        /// 它**只影响二分起点**、渲染差 ≤ 0.05 fontSize 单位（`TextMeshPro.cs:2148-2149` 起手 `Clamp(base,min,max)`），
        /// **但确实不是 0**，且原版值现成 —— `Label.SetAutoFitBox` 的 `basePx` 参数就是为它加的（A305 ①）。</summary>
        public const float CcBaseX1 = 36f, CcBaseSl = 36f, CcBaseX2 = 35f;
        /// <summary>🔴 **2026-10-13（A691）**：`spares == 0` 那一支走的**另一棵树** ——
        /// `Card Counter/Single Counter`（**170×50**，`m_AnchoredPosition = (0,0)` ⇒ **居中**在 `Card Counter` 里）。
        /// 判据 = **本笔现读原版 prefab**（`bundle_scenes_scenes_mainmenuwarpforge`）：
        /// `RectTransform_1531`（GO `Single Counter`，`m_IsActive = **False**` = 出厂关着）·
        /// anchors `(0.5,0.5)-(0.5,0.5)` · pos `(0,0)` · **`m_SizeDelta = (170, 50)`** · pivot `(0.5,0.5)` · scale 1 · rot 0；
        /// 父链 = `RectTransform_1596`（`Card Counter`，`m_SizeDelta = (269.857, 79.37)`、pos `(0,−329.5)`）。
        /// ⇒ 绝对矩形 `875, 844.50 → 1045, 894.50`（`UguiRect.Child` 复算；与正本 §三 末行同值）。
        /// ⚠️ **它和 `CcBgL/T/R/B` 那一套（=`Duplicate Counter`，240×70.586）不是同一棵树** ——
        /// 本笔之前我们只把 `Duplicate Counter` **改了个名**，几何/字号/结构全部沿用 ⇒ 两棵树被当成一棵。</summary>
        public const float CcSgL = 875f, CcSgT = 844.5f, CcSgR = 1045f, CcSgB = 894.5f;
        /// <summary>🔴 **2026-10-13（A691）**：`Single Counter/Counter` 那颗 TMP 的**逐字段原版值**
        /// （本笔现读 MB `pid 1893` ← `CardCounterDisplay.singleCardCounter`，其 GO = `Counter_227` → `RectTransform_1151`）：
        /// `m_fontSize = **35.0**` · `m_fontSizeMin = **25.0**` · `m_fontSizeMax = **35.0**` · `m_fontSizeBase = **36.0**` ·
        /// `m_enableAutoSizing = 1` · `m_HorizontalAlignment = **2**(Center)` · `m_TextWrappingMode = **0**` · `m_text = "x2"`。
        /// ⚠️ `CcSgWrapX` = **容器宽**：那颗 TMP 的 RT（`1151`）是 **stretch**（anchors `(0,0)-(1,1)`、`sd (0,0)`）
        /// ⇒ 它的 TMP 容器 = **解析后的 170**、⛔ **不是 `m_SizeDelta.x = 0`**
        /// （同族提醒见 `CcWrapX1` 的注释：那一族的 `sd.x` 才是容器宽 —— **两支的读法不同，别互相套**）。
        /// ⚠️ **三颗 `Duplicate Counter` 的值一个都不能套过来**（35 ≠ 52.6 · 25 ≠ 18 · 容器 170 ≠ 73.283/44.09）。</summary>
        public const float CcSgPx = 35f, CcSgMin = 25f, CcSgMax = 35f, CcSgBase = 36f, CcSgWrapX = 170f;
        /// <summary>通配符条：`Segment` 0,71→1920,156 · 底 1550,91.5→1870,135.5 · 四组 30×44 图标 + 41×44 数字。</summary>
        public const float WcBgL = 1550f, WcBgT = 91.5f, WcBgR = 1870f, WcBgB = 135.5f;
        public const float WcStep = 75f, WcX0 = 1565f;
        /// <summary>数字格的标称字号 —— 原版 `m_fontSize = 32.6`。🔴 **2026-10-17（`A1229`）收口**：
        /// 值只在 `DeckRuntime.WcCountFontPx` 一处，本窗 / 收藏窗 / 卡组编辑窗三处都引用它。</summary>
        public const float WcPx = DeckRuntime.WcCountFontPx;

        static readonly Vector4 RedBorder = new Vector4(42f, 363f, 655f, 81f);
        const float RedTexW = 1100f, RedTexH = 701f;
        const float BtnTexW = 410f, BtnTexH = 124f;

        /// <summary>正在展示的卡（`CardData` 是 struct ⇒ 另用一个 bool 判「有没有」）。</summary>
        public CardDef Card;
        public bool HasCard;

        // ============================================================ 卡片那一叠（1 主卡 + 8 相关卡）
        //
        // 🔴 **扇形位姿 / 分层 / 着色那三组数，判据只此一份 = `Core/CardFan.cs`**（2026-09-28 收口）。
        //    为什么：**原版是同一个脚本 `CardDisplayWindow`、两处摆放**（菜单版 = 本窗，
        //    战斗版 = `Battle/CardDisplayWindow.cs`），两组数两边都要用 ⇒ 写两份迟早不一致。
        //    真值来源（原版那条 legacy clip `Card Display Open`）、槽 5–8 的外推规则、
        //    分层为什么要「每格一个队列」、相关卡为什么要压暗到 0.65 —— **全在那个文件的注释里**。
        //    ⚠️ 顺带订正一处：本文件 2026-09-27 记的槽 3 转角 **12.648°** 是错的，clip 原文复算是 **12.750°**。

        /// <summary>卡位个数：**1 主卡 + 8 相关卡**（原版是 1+4；**8 这一档是我们挑的**，用户授权的冗余）。</summary>
        public const int Slots = CardFan.Slots;
        /// <summary>换位动画时长 —— 原版 `CardDisplayWindow.relatedCardSwapTime`（字段 `+0xC0`）。</summary>
        public const float SwapTime = CardFan.SwapTime;
        /// <summary>槽名（照原版命名：**前台那张不带后缀**，其余 `CardUI (1..8)`）。</summary>
        public static string SlotName(int i) { return CardFan.SlotName(i); }
        /// <summary>命中区所在的 z 阶梯（**只给命中区用** —— 它是 alpha=0 的透明件，不影响画面；
        /// `PointerLayer` 同队列时**取 z 小的那个**，所以前台那张要最小）。</summary>
        const float HitZStep = 0.08f;

        // 🔴 **「把整格卡搬到同一个队列」那段（含「为什么必须走 `.material`」那个坑）已收口到
        //    `CardFan.SetCardQueue`** —— 两扇窗共用一份。卡格的队列基数见 `QCardStack`（本窗）。

        /// <summary>每个位姿槽上的卡（下标 = **位姿槽**，0 = 前台位）。</summary>
        public readonly CardView[] SlotViews = new CardView[Slots];
        readonly CardDef[] _slotDefs = new CardDef[Slots];
        bool _swapping;

        /// <summary>位姿槽上有几张卡（= 1 + 相关卡数，最多 `Slots`）。</summary>
        public int SlotCount { get; private set; }
        /// <summary>**前台那张**（永远是位姿槽 0）。原版 `CardSwapFinished` 之后 lore / 语音认的就是它。</summary>
        public CardDef FrontDef { get { return _slotDefs[0]; } }
        /// <summary>换位动画在播没有（= 原版那个 `swappingCards` 闸）。</summary>
        public bool IsSwapping { get { return _swapping; } }
        /// <summary>位姿槽 `i` 上的那张卡（越界返回 null）。</summary>
        public CardView SlotView(int i) { return (i >= 0 && i < Slots) ? SlotViews[i] : null; }
        /// <summary>位姿槽 `i` 上那张卡的卡表项（越界返回 null）。</summary>
        public CardDef SlotDef(int i) { return (i >= 0 && i < Slots) ? _slotDefs[i] : null; }
        public int SlotIndexOf(string cardId)
        {
            for (int i = 0; i < Slots; i++) if (_slotDefs[i] != null && _slotDefs[i].Id == cardId) return i;
            return -1;
        }

        /// <summary>效果文字条（`LowerSection/FlavourTextBG` + `LoreText`）现在露着没有（原版 `Show Card Text` 切它）。</summary>
        public bool LoreVisible = true;
        /// <summary>前台那张的官方风味文本（没有 = null ⇒ 整块不亮；🆕 2026-10-17 `A1802`）。自检/上层读它。</summary>
        public string ShownLore { get { return _loreText; } }
        /// <summary>下缘那两颗钮的**整组节点**（图 + 命中区一起开/关）—— 建不建由卡决定，
        /// 判据只此一份 → `CardButtons`（原版 `SetCardVoiceOver` / `showCardTextButton`）。</summary>
        Transform _voiceBox, _eyeBox;
        /// <summary>最近一次开的（自检用）。</summary>
        public static CardDetailPopup LastOpened;
        /// <summary>最近一次播的语音文件（自检用）。</summary>
        public string LastVoiceFile { get; private set; }

        public Transform ShadeHit { get { return Find(transform, "BackgroundHit"); } }
        public Transform VoiceHit { get { return Find(transform, "VoiceHit"); } }
        public Transform EyeHit { get { return Find(transform, "ShowTextHit"); } }
        public Transform CraftHit { get { return Find(transform, "CraftHit"); } }
        public Transform UpgradeHit { get { return Find(transform, "UpgradeHit"); } }
        /// <summary>🆕 **2026-10-18**：升级面板拆成两颗并列钮之后的**「降级」那颗**的命中区（自检用）。
        /// ⚠️ `UpgradeHit` 那个名字**没改**（`Editor/CollectionScene.cs` 既有断言按它取节点）——
        /// 它现在指的是**「升级」那颗**（= 原来那颗钮的语义位置）。</summary>
        public Transform DowngradeHit { get { return Find(transform, "DowngradeHit"); } }
        /// <summary>🆕 异画面板那两颗换画圆钮的命中区（自检用；`side` = `"Left"` / `"Right"`）。
        /// ⚠️ 名字**必须带左右** —— 两颗各有一个，同名的话 `Find` 只会拿到第一颗。</summary>
        public Transform AltArrowHit(string side) { return Find(transform, "ArrowHit " + side); }
        /// <summary>🆕 异画面板那颗价签的命中区（自检用）。</summary>
        public Transform BuyHit { get { return Find(transform, "BuyHit"); } }

        // ============================================================ 🆕 异画当前档（A1699）
        //
        // 原版字段 = `AlternateArtPanel.currentStateIndex`（`dump.cs` 的 `+0x50`）——
        // **0 = 原画，1..N = 这张卡的第 N 种异画**（不是 0..N-1！）。
        // 判据（`d:/2/tools/decomp_full/`，本笔逐字读过）：
        //   · `AlternateArtPanel__SwapStyle.c:25` `*piVar1 += (isLeft ^ 1) * 2 − 1`
        //     ⇒ **left(+1) → −1 · right(0) → +1**；
        //   · `:30` `index < 0 ⇒ index = list.Count`、`:36` `Count < index ⇒ index = 0` ⇒ **回绕**；
        //   · `:47` `if (index == 0)` 取 `+0x58`（= `nonAlternateArtCard` = **原画**）、
        //     `:60` 否则取 `list[index − 1]`（**1 基**，`lVar9 = iVar2 + (−1)`）——
        //     这就是「0 = 原画」的铁证；
        //   · `AlternateArtPanel__Initialize.c:82-95` 扫 `list` 找「当前这张卡的风格」并把它写成
        //     **`下标 + 1`** ⇒ 起来的时候落在**这张卡真正在用的那种异画**上。

        /// <summary>当前选的异画档：**0 = 原画 · 1..N = 这张卡的第 N 种异画**（原版 `currentStateIndex`）。
        /// 换卡（`ShowCard`）时归零（原版每开一次窗重新 `Initialize`）。</summary>
        public int AltStyleIndex { get; private set; }

        /// <summary>这张卡**有几种异画**（= `AltStylesOf(Card.Id).Count`；自检用）。
        /// ⚠️ 与「贴图加载出来没有」无关 —— 判据同 <see cref="HasAltArtStyle"/>。</summary>
        public int AltStyleCount { get { return AltStylesOf(Card != null ? Card.Id : null).Count; } }

        public Transform Counter { get { return Find(transform, "Card Counter"); } }
        /// <summary>升级面板的 `No Upgrade Warning`（满级时才露）。</summary>
        public Transform NoUpgradeWarn { get { return Find(transform, "No Upgrade Warning"); } }
        /// <summary>三块面板的 `Title` 文本（自检用）。</summary>
        public string TitleOf(string which) { var t = Find(transform, which + " Title"); return t != null ? TextOf(t) : "(无)"; }

        static Transform Find(Transform root, string name)
        {
            if (root == null) return null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }
        static string TextOf(Transform t)
        {
            var l = t != null ? t.GetComponent<Label>() : null;
            return l != null ? (l.Text ?? "") : "";
        }

        public static CardDetailPopup Create(WindowsManager mgr)
        {
            var go = new GameObject("Card Displayer Menu For Menu");
            var win = go.AddComponent<CardDetailPopup>();
            win.type = WindowType.Popup;                 // 实证 type=1
            // ⚠️ 原版 MB 里 `windowsPlacement = 0 (None)`，但它的**实际父节点是 `3 - PopUp Holder`**
            //    （= 我们的 `WindowsPlacement.Popup`）⇒ 我们按**实际父链**挂，**不按那个字段**
            //    （同 `资料/阶段二_战斗入口_原版规格.md` 开头那条「别按 placement 去猜它挂哪」）。
            win.placement = WindowsPlacement.Popup;
            win.closeOnEsc = true;                       // 实证 closeOnESC=1
            // 🆕 **2026-10-18（音效线缺口 `AUD` §三 B1）**：原版根 MB（`CardDisplayWindow`，pid **2033**）
            // `closeSound = PPtr(fileID 6, pid 3524674544779823761)` = cue **`Close Card Display`**
            // （FX 组）。我们的 `GameWindow.closeSound` 是 `AudioClip`、而 cue 带 `timeToPlayAgain`
            // 参数 ⇒ 走 `WindowSounds` 那张 cue 表（同 FTUE 开窗音的先例），并**关掉基类默认关窗音**
            // （否则会多叠一声 `ButtonClick Back` —— 原版只响 `Close Card Display` 这一条）。
            win.useDefaultCloseSoundIfNull = false;      // 原版字段实读 = 1；1 只在 closeSound 为空时兜底
            win.extraScaleSmallScreen = 1f;
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        /// <summary>开/换一张卡。**复用同一个窗**（原版 `ShowCard` 里 `WindowsManager.OpenWindow(this)` 就是这个意思）。
        ///
        /// <para>🔴 **2026-10-12（A217②）就地订正（铁律 5）**：本方法原来**靠「同窗再开 ⇒ 重建内容」换卡**
        /// （`Manager.OpenWindow(this)` → `GameWindow.TryOpen` → `Open()` → `Build()`），
        /// 并在 `WindowsManager` 里如实标注过这条偏离（"换了会静默坏"）。
        /// A217② 把 `TryOpen` 按原版改成**按 `CurrentState` 分三档**（`Open` 支**一个字段都不写就 return**）
        /// ⇒ **再开不重建**（照原版），所以「换卡」必须**自己显式来一次** —— 下面那句 `Build()` 就是它。
        /// 这与原版同构：原版靠的是**窗自己 override 的 `TryOpen`/`Open` 里那一段刷新**
        /// （`decomp_full/…/CardDisplayWindow__ShowCard.c` 那条链每一扇自己要刷的窗都要有这一层）。</para>
        ///
        /// <para>⚠️ **两处如实标注（铁律 3）**：
        /// ① **首开那一次会 `Build()` 两遍**（`OpenWindow` → `TryOpen` 的 `Closed` 支 → `Open()` → `Build()`，
        ///    回来再走下面这一句）—— **有意如此**：按 `CurrentState` 去猜「这一次会不会重建」
        ///    就等于把判据抄成第二份，而 `TryOpen` 里万一早退了，换卡会**静默失败**。
        ///    `Build()` 是**幂等**的（首句把窗根的子件全销毁重建），代价 = 一屏 9 张卡面建两遍（一帧内、无动画）。
        /// ② **`Build()` 必须排在 `OpenWindow` 【之后】**：`Build()` 里那些 `MenuDraw.Text` / TMP 用法
        ///    要窗**已激活**（批处理与运行时同理：未激活对象上量不出 TMP 尺寸 —— 见 `CLAUDE.md` §三那条）。
        ///    窗在 `ShowCard` 之前一定是关着的（新建的那一扇 `CurrentState = Closed`）⇒ 先开再建。</para></summary>
        public void ShowCard(CardDef card)
        {
            Card = card;
            HasCard = card != null;
            LoreVisible = true;                 // 原版每次 `ShowCard` 都把 `loreObjectBG` 复位
            // 🆕 A1699：异画也复位回**原画**（原版每开一次窗都重跑 `AlternateArtPanel.Initialize`，
            // 而 `Initialize` 把 `currentStateIndex` 从 0 起算、再扫出「这张卡在用的那种」）。
            // ⚠️ 我们**不照抄「落在该卡在用的那种」那一步**：本地 7 张异画**都是未装备的收藏品**
            //（原版那一步读的是服务端「当前装备的卡」）⇒ 起来一律是**原画**，如实标注。
            AltStyleIndex = 0;
            if (Manager != null)
            {
                Manager.OpenWindow(this);       // 还开着就复用同一扇（原版 `WindowsManager.OpenWindow(this)`）
                Build();                        // ⟵ 🔴 换卡靠这一句，⛔ 不是靠「再开一次会重建」
                LastOpened = this;
                return;
            }
            // 没有 `WindowsManager`（自检里可能）⇒ 照旧自给自足（`Build()` 前先激活，理由同 ②）
            gameObject.SetActive(true);
            Build();
            LastOpened = this;
        }

        public override void Open()
        {
            // 🆕 **2026-10-18（音效线缺口 `AUD` §三 A1）**：原版 `Card Displayer Menu For Menu` 根 MB
            // （`CardDisplayWindow : GameWindow`，pid **2033**）`openSound = PPtr(fileID 6,
            // pid 4006604330728631164)` = cue **`CardExpand`**（clip `CardExpand`，pitch 0.8–1.1）。
            // 消费点 = 继承来的 `GameWindow.TryOpen`（`SoundManager.Play2D(..., MixerType 0 = FX)`）。
            // ⚠️ 我们的 `openSound` 是 `AudioClip`、cue 带 pitch 区间与节流 ⇒ 走 `WindowSounds`。
            WindowSounds.PlayFx(WindowSounds.CardExpandCue, "[CardDetail] `Card Displayer Menu For Menu` 开窗音");
            Build();
        }

        // ============================================================ 关窗（🆕 2026-10-18 · 账 `N115` · 写手 W19）
        //
        // 🔴 **为什么要覆写 `Close()` / `Hide()`**（两条，都是本窗自己的事）：
        //
        // ① **出声** —— 本窗原来**不覆写**这两条，于是「关窗到底跑没跑」在日志里**一个字都没有**。
        //    真机那次能看到的只有紧随其后的 `WindowsManager.ShowPreviousWindow` 那一句
        //    `[Win] \`Collection Menu Variant\`：TryOpen 命中 Background 支`（`Shell/WindowsManager.cs` 的
        //    `ShowPreviousWindow` ⑥），而**那句话是「关窗【成功】之后的必然结果」**（= 底下的收藏窗从
        //    `Background` 回 `Open`）—— 同一句在关 `Deck info Popup` 时也会出现（实据：
        //    `_tmp_view/click_log.txt` 18:35:04 那一下命中 `CloseHit`/`Deck info Popup`，随后的日志**逐字相同**）。
        //    ⇒ `N115` 把它读成了「点击只把收藏窗提回前台」= **把成功读成了失败**。
        //    这一条只加日志（红线：不许静默失败），**不改任何行为**：关窗之后下一次真机日志里，
        //    `[CardDetail] Close/Hide …` 在不在，一眼就能分开「关窗跑了」与「关窗根本没跑」。
        //
        // ② **把「相关卡扇形」的临时状态收干净**（任务书第 4 条）。换位那一叠补间是
        //    `CardView.TweenPose` / `CardFan.TweenTint(v, …)`，两者的 `CardTween.Use(..., link)` 默认带
        //    `SetLink(link.gameObject)`（`Core/CardTween.cs` 的 `SetLink(link.gameObject)`）⇒ **正常情况下**物体 `SetActive(false)` 时
        //    DOTween 自己会杀。
        //    （`CardView.TweenPose` 那条**另外**还 `SetTarget(transform)` —— `DOTween.To` 建出来的补间默认没有 target，
        //      不设的话外面 `transform.DOKill()` 掐不到它；旧入口 `CardTween.ToPose` 已于 2026-10-24 `A11` 退役，
        //      判据见 `SwapToFront` 里那一段。）
        //    但 `LinkEnabled` **可以**被 `WF_NO_SETLINK=1` 关掉（那是 `CardTween` 的 A/B 开关，
        //    见 `CardTween.LinkEnabled` 的注释）—— 那一档下补间**活得比窗长**：0.25 s 后的 `OnComplete`
        //    （`SwapToFront` 里那一段）会去写 `_slotDefs` / `Find(transform, …)` / `RefreshButtons()`；
        //    玩家若在那一刻已经重开（`Build()` 把整棵子树销毁重建），那些引用就是**已销毁对象**
        //    ⇒ `MissingReferenceException` + 前台指针错乱。⇒ 关窗/藏窗那一拍**显式**杀掉 + 复位 `_swapping`
        //    （否则关了窗之后 `IsSwapping` 还报 `true`，自检与上层读到的都是一个假状态）。
        public override void Close()
        {
            ResetFanState("Close");
            // 🆕 **2026-10-18（音效线缺口 `AUD` §三 B1）**：原版关窗音 = cue **`Close Card Display`**
            //（根 MB pid 2033 的 `closeSound = PPtr(fileID 6, pid 3524674544779823761)`）。
            // ⚠️ 闸 = `gameObject.activeSelf`：原版 `GameWindow.Close` 首句是
            //   `!gameObject.activeSelf ⇒ return`（那一刻**不播音**）⇒ 关一扇本来就关着的窗别出声。
            // ⚠️ 基类那一侧 `useDefaultCloseSoundIfNull` 已在 `Create()` 里置 false（防叠默认音）。
            if (gameObject.activeSelf)
                WindowSounds.PlayFx(WindowSounds.CloseCardDisplayCue, "[CardDetail] `Card Displayer Menu For Menu` 关窗音");
            base.Close();
        }

        public override void Hide()
        {
            ResetFanState("Hide");
            base.Hide();
        }

        /// <summary>关窗 / 藏窗那一拍把「相关卡扇形」的临时状态收干净（判据见上面那两段）。
        /// ⚠️ **不销毁子件** —— 下一次 `Build()` 本来就会把整棵子树销毁重建（本窗的既定做法），
        /// 这里只收**补间 + 记账**，所以关窗那一帧的画面不受影响。</summary>
        void ResetFanState(string why)
        {
            bool wasUp = gameObject.activeSelf;          // = 这一次是「真的关掉一扇开着的窗」还是空转（见下面那句日志的闸）
            bool wasSwapping = _swapping;
            for (int i = 0; i < Slots; i++)
            {
                var v = SlotViews[i];
                if (v == null) continue;                    // 空槽 / 已销毁（Unity 假 null）
                v.transform.DOKill();                       // 位姿补间（`CardView.TweenPose` 那条的 target = Transform，见 `SetTarget`）
                DOTween.Kill(v);                            // 着色补间（`CardFan.TweenTint` 的 target = CardView）
            }
            _swapping = false;
            // ⚠️ **窗口本来就不活 ⇒ 不出声**（照 `GameWindow.Close` / `Hide` 首句那道守卫的口径：
            //    `CloseAllWindows()` 逐扇 `Close()` 撞上已藏起来的窗是**正常路径**，原版那一刻也静默早退）。
            if (!wasUp) return;
            Debug.Log($"[CardDetail] {why}：关窗清理 —— "
                      + (wasSwapping ? "**杀掉了还在播的换位补间**" : "没有在播的换位补间")
                      + $" · `_swapping` 复位 · 扇形 {SlotCount} 格的临时状态收掉"
                      + "（子件不在这里销毁：下次 `Build()` 整棵重建，所以不留残影）");
        }

        // ============================================================ 建

        void Build()
        {
            var root = transform;
            for (int i = root.childCount - 1; i >= 0; i--) CollectionWindow.DestroySafe(root.GetChild(i).gameObject);
            _voiceBox = null; _eyeBox = null;      // 旧的那两颗随子件一起没了 ⇒ 引用也要清
            if (!HasCard) return;

            // 1) 压暗 + 点遮罩关窗（原版唯一的「关」）
            //    🔴 队列 `QShade`（**3105**）—— **必须低于卡格**（`QCardStack = 3106`）：
            //    原版那棵树里 `Menu Dark Background` 是**第一个孩子**（画在最下），卡在它上面。
            //    🔴 **2026-10-18（P1 §三 · 报告第 1 条）**：视觉层与**点击区**从这一份矩形走 ——
            //    原版那颗「点它关」的图形就是这块 **超画幅** `-1327.3,-746.18 → 3247.3,1826.18` 的
            //    `Menu Dark Background` 自身；我们原来的点击区写死 `1920×1080`
            //    ⇒ **超宽屏两侧点不到（关不掉窗）**（21:9 可见宽 = 2520 设计 px，两侧各 300px）。
            //    判据 → `资料/普查产出_1018/P1_弹窗排版_卡组卡牌.md` §三 与 §七 S1。
            var shadeRect = new PxRect(CardWinBox.MaskL, CardWinBox.MaskT, CardWinBox.MaskR, CardWinBox.MaskB);
            MenuDraw.Rect(root, CardArt.Solid(), shadeRect, "Menu Dark Background", QShade, ShadeColor);
            // 🔴 **2026-10-04（A47 接线批）订正档号：`QCdHit − 1`(3117) → `QShade`(3105)** ——
            //   规矩 = 「压暗层的命中区落在**压暗层自己那一档**，且严格低于本窗任何内容命中区档」
            //   （判据 → `MenuDraw.ShadeHit` 的注释 · `资料/待办判据_阶段二与联机.md` §A25·补（一））。
            //   ⚠️ 3117 恰好是 `QCdText`（**文字那一档**）——「内容档 − 1」看着像派生，其实落在别的层上。
            MenuDraw.ShadeHit(root, shadeRect, QShade, QCdHit, () => Close(), "BackgroundHit");

            // 2) 卡片那一叠（1 主卡 + 8 相关卡 · 扇形）—— 判据与真值 → `CardFan`
            var cardNode = MenuDraw.Node(root, "Card Display", new PxRect(CardL, CardT, CardR, CardB));
            BuildCardStack(cardNode);

            // 3) `LowerSection`：效果文字条 + 语音钮 + 「显示卡面文字」钮
            var lower = MenuDraw.Node(root, "LowerSection", new PxRect(LowerL, LowerT, LowerR, LowerB));
            // ⚠️ `FlavourTextBG` 那张底图**本地有**（13 张按阵营的 `40K_display_Flavortext <阵营>.png`，
            //    工程里还没导 —— §三 第 22 条 第 2 项）⇒ **现在只画字**
            var bg = MenuDraw.Node(lower, "FlavourTextBG", new PxRect(LowerL, LoreBgT, LowerR, LoreBgB));
            BuildLoreText(bg);

            // 两颗钮：**整组一个节点**（图 + 命中区一起开/关）—— 建不建由卡决定，见 `RefreshButtons`
            float vx = VoiceCx - BtnS * 0.5f, ex = EyeCx - BtnS * 0.5f, vy = BtnCy - BtnS * 0.5f;
            _voiceBox = MenuDraw.Node(lower, "Voice Over Button", new PxRect(vx, vy, vx + BtnS, vy + BtnS));
            var voiceQ = MenuDraw.Rect(_voiceBox, CardArt.Ui("40k_UI_bt_voicelines"),
                          new PxRect(vx, vy, vx + BtnS, vy + BtnS), "Image", QCdRow, null, true);
            // 🆕 A17：原版 `Card Displayer Menu For Menu>…>Voice Over Button` 是 SpriteSwap
            //（高亮 `40k_UI_bt_voicelines_hover`，普查 §块 1 那两条 ❌ 之一）
            // 🆕 **2026-10-18（音效线缺口 `AUD` §三 C4）**：原版 `Voice Over Button` = 空 cue +
            // **`useDefaultAudioClipIfNull = 0`** ⇒ **静音**（它只播语音本身）⇒ 关掉按钮音。
            var voiceHit = Hit(_voiceBox, "VoiceHit", new PxRect(vx, vy, vx + BtnS, vy + BtnS), () => PlayVoice(), QCdHit,
                voiceQ, "40k_UI_bt_voicelines");
            var voiceWb = voiceHit != null ? voiceHit.GetComponent<WindowButton>() : null;
            if (voiceWb != null) voiceWb.playClickSound = false;
            _eyeBox = MenuDraw.Node(lower, "Show Card Text", new PxRect(ex, vy, ex + BtnS, vy + BtnS));
            // 🔴 **2026-10-03 就地更正**：这里**不变**常态图 —— 直接读原版 prefab
            //（`bundle_scenes_scenes_mainmenuwarpforge` → `Card Displayer Menu For Menu`）：
            // **`Show Card Text` = `40k_UI_bt_eye`（256×256）· HL = `40k_UI_bt_eye_hover` · P = `40k_UI_bt_eye_pressed`**
            // ⇒ 我们画的这张**本来就是对的**（普查 §块 1 第 14 行同值），只是**一直没接换图**。
            // ⚠️ 别和 **`40k_bt_eye`（88×87，另一张）** 混 —— 那张是 `LiveOpsEventWindow` 卡组四圆钮用的。
            var eyeQ = MenuDraw.Rect(_eyeBox, CardArt.Ui("40k_UI_bt_eye"),
                          new PxRect(ex, vy, ex + BtnS, vy + BtnS), "Image", QCdRow, null, true);
            // 🆕 **2026-10-18（音效线缺口 `AUD` §三 C5）**：原版 `Show Card Text`（眼睛）= 空 cue +
            // **`useDefaultAudioClipIfNull = 0`** ⇒ **静音** ⇒ 关掉按钮音。
            var eyeHit = Hit(_eyeBox, "ShowTextHit", new PxRect(ex, vy, ex + BtnS, vy + BtnS), ToggleLore, QCdHit,
                eyeQ, "40k_UI_bt_eye");
            var eyeWb = eyeHit != null ? eyeHit.GetComponent<WindowButton>() : null;
            if (eyeWb != null) eyeWb.playClickSound = false;
            RefreshLore();
            RefreshButtons();
            if (CardArt.Ui("40k_UI_bt_voicelines") == null)
                Debug.Log("[CardDetail] `40k_UI_bt_voicelines` 不在 `Resources/Art/ui/` ⇒ **语音钮画不出来**（不摆白方块）");

            // 4) 三块面板
            var panel = MenuDraw.Node(root, "Card Options Panel", new PxRect(0f, 0f, 1920f, 1080f));
            var vlg = MenuDraw.Node(panel, "Panel", new PxRect(PanL, PanT, PanR, PanB));
            // ============================================================ 🔴 2026-10-12（A1699）：两块面板**恢复**
            //
            // **用户原话（本笔是照他下的命令做的）**：「「创建副本 / 升级」两块面板：维持你 9-27 的停用，
            //   还是恢复？**请你恢复**。我说的是不需要升级卡牌系统，**默认最高级就是不能点击升级卡牌按钮**。」
            // ⇒ ① **UI 都恢复**（照原版版式）；② **「升级」那颗钮做成【不可交互】**
            //    （**不是**把面板删掉 —— 见 `BuildUpgrade` 末尾那一句，以及它引的原版判据）；
            // ③ 「创建副本」那颗钮**点下去出声**（**本版不实现真机制**，见 `OnCraftClicked`）。
            //
            // 本行原来是一条 `Debug.Log`（"本作不做升级/合成 ⇒ 两块面板都不建"），2026-09-27 用户拍板的
            //   那条口径**已被 2026-10-12 这句取代**。旧口径的两条理由（`CardArt.TierOf` 一律最高档、
            //   本作全解锁）**仍然成立**，但它们现在只解释「钮为什么点不动」，不再解释「面板为什么不在」。
            //   📌 `Craftable()` / `WildcardIconFor()` 这两个判据（上一轮为「创建副本」做的）**现在有调用点了**。
            //   ⚠️ 顺序照原版 `Panel` 那个 VLG 的次序（创建副本 146.71 → 升级 411.12 → 异画 673.42）；
            //   我们按绝对矩形摆、不吃布局组，所以次序只影响 `Find` 的先后与生成的兄弟序。
            if (Craftable(Card.Rarity)) BuildCrafting(vlg);
            else Debug.Log("[CardDetail] 这张卡的稀有度是「" + Card.Rarity + "」⇒ **「创建副本」那块整块不建**"
                         + "（判据 = `Craftable`，等价于原版 `CraftingPanel.CheckButtons` 里"
                         + " `CheckForCardCrafting` 给出空价 ⇒ `gameObject.SetActive(0)`，见 `Craftable` 的注释）");
            BuildUpgrade(vlg);
            BuildAltArt(vlg);
            BuildCounter(panel);
            BuildWildcards(panel);
        }

        // ============================================================ 卡片那一叠（扇形 + 相关卡）

        // 🔴 **命中区的判据只此一份 → `CardFan`**（原版 `CardUI/2DCard/UI Collider` 的
        //    `m_SizeDelta(-0.2,-0.44)` + `m_AnchoredPosition.y=-0.02` ⇒ **两轴比例不同 + 中心下移 5px**）。
        //    ⚠️ **不要再在本文件里放一个 `HitRatio` 常量** —— 2026-10-07 之前那只 `0.9043` 是 **x** 那个比，
        //    双轴同用 + 居中 ⇒ y 轴多吃两条窄带（A156）。用 `CardFan.HitW/HitH/HitCy`。

        /// <summary>把「1 主卡 + N 相关卡」按**原版那条 clip 的扇形**摆出来（判据与真值 → `CardFan`）。
        /// ⚠️ 位姿槽 0 = 前台（原版 `cardUIs[0]`：屏心 (960,480) · scale 250 · 转角 0）。</summary>
        void BuildCardStack(Transform cardNode)
        {
            var cards = new List<CardDef>();
            if (Card != null) cards.Add(Card);
            // 相关卡：判据只此一份（`RelatedCards.Find`）—— 与原版那 4 个 `relatedCard1..4` 字段的关系见那个文件头
            cards.AddRange(RelatedCards.Find(Card, Slots - 1));
            SlotCount = Mathf.Min(cards.Count, Slots);
            for (int i = 0; i < Slots; i++)
            {
                SlotViews[i] = null; _slotDefs[i] = null;
                if (i >= SlotCount) continue;
                var def = cards[i];
                float w = CardFan.Wpx(i), h = CardFan.Hpx(i);
                float cx = CardFan.Cx(i), cy = CardFan.Cy(i);           // 屏坐标（左上原点；原版 pos.y 向上 ⇒ 取负）
                var px = new PxRect(cx - w * 0.5f, cy - h * 0.5f, cx + w * 0.5f, cy + h * 0.5f);
                var node = MenuDraw.Node(cardNode, SlotName(i), px);

                // 🔴 **命中区【先建】、且【不看 `CardView.Create` 的返回值】**（A157，2026-10-07）：
                //   原来 `if (v == null) { …; continue; }` 排在下面 ⇒ `CardView.Create` 失败的那一格
                //   **连命中区一起没有**，点上去会穿透它落到压暗层 `BackgroundHit` 上**把窗关掉**。
                //   原版每一格都挂自己的 `UI Collider`（吃射线的是它，不是卡面那些图）——
                //   **「这一格点不点得到」与「这一格的卡面画不画得出来」是两件事**：
                //   卡面画不出来时，玩家的那一下仍然该被这一格吃掉（回调 `SwapToFront` 自己会闸掉），
                //   ⛔ 不能让一次渲染失败变成「点卡片 = 关窗」。⇒ 命中区与 `CardView` 解耦。
                // ⚠️ 卡面那张建不出来时**照旧出声**（`LogWarning`，不静默）—— 见下面那一支。
                BuildCardHit(node, i);

                var cd = BattleDriver.ToCardData(def, def.Faction);
                // 🆕 **2026-10-12（A1699）异画预览：前台那张吃 `CardData.artOverride`**。
                //   原版 = `AlternateArtPanel__SetStyle.c:47-52` 的 `BasicCardUI.SetCardImage(前台那张, 该风格图)`
                //   —— `currentCardUI`（`+0x60`）就是 `CardDisplayOptions.Initialize` 传进来的那张（= `cardUIs[0]`）。
                //   🔴 **口径 = 与另外 4 处【逐字相同】的那一个**：`CardData.artOverride` 非空 ⇒
                //   `CardView.ArtTexture` 用它、不再按 `artId` 取（`Core/CardView.cs` 的 `ArtTexture`），
                //   抠图清单也跟着换成异画那份（`CardPresentation/Core/CardView.cs`）；另两处消费点 = `Battle/BattleDriver.cs`
                //   （进攻卡整张卡面用它）与收藏窗 Styles 页 `Shell/CollectionWindow.cs`。
                //   ⚠️ **必须在 `CardView.Create` 【之前】设**（`Create` 那一趟就按它建了立绘层；
                //   口径同 Styles 页那条注释「`d.artOverride` 是在 `Create` 之前设好的」）。
                if (i == 0)
                {
                    var alt = AltArtTexture();
                    if (alt != null) cd.artOverride = alt;
                }
                var v = CardView.Create(node, cd, SlotName(i));
                if (v == null)
                {
                    Debug.LogWarning("[CardDetail] 第 " + i + " 格（" + def.Name + "）的 `CardView` 建不出来"
                                   + " —— 那一格**没画卡面，但命中区照样在**（点它不会关窗，出声）");
                    continue;
                }
                v.gameObject.SetActive(true);
                v.SetPose(MenuDraw.Local(node, px.x1, px.y1, px.x2, px.y2), CardFan.Rot(i),
                          h / (CardView.Height * 108f));
                v.SetData(cd);
                v.SetFace(CardFace.Full);
                v.SetHighlight(CardHighlightState.Normal);
                // **前台白 / 相关卡压暗到 0.65**（原版 `cardInBackGroundColorTint`）——
                // ⚠️ **必须排在 `SetHighlight` 之后**（那个自己也写 `SetTint`，谁后调谁生效）。
                CardFan.ApplySlotTint(v, i == 0);
                // 🔴 **2026-10-22（卡牌线 `T04`）：大图档「角色压框」的两轴门控。**
                //    ① <c>ArtOverdraw</c> = **这一格是不是前台**（槽 0；换位时由 `SwapToFront` 改）；
                //    ② <c>UseOverDraw</c> = **这张卡允不允许溢出**（逐卡 `CardDef.UseOverDraw`
                //       ← `cards_engine.json` 的 `useOverDraw` 列；建卡时定，换位不改）。
                //    合成判据**只在 `CardView.ArtFrontVisible` 一处**（⛔ 别在这儿再拼一遍）。
                //    原版判据 → `CardDisplayWindow__CardOverdrawHandle.c:14-33`：`idx != 0` 一律 `Small`；
                //    `idx == 0` 且 `useOverDraw != 0` 才 `Big`（本窗与原版同一个 `CardDisplayWindow` 族）。
                //    ⚠️ 缺卡表项时按 `true`（= 本笔之前的行为，不静默变严）。
                v.ArtOverdraw = (i == 0);
                v.UseOverDraw = def != null ? def.UseOverDraw : true;
                SlotViews[i] = v; _slotDefs[i] = def;
            }
            // 🔴 **每格一个独立的渲染队列**（越靠前台越高）—— 判据与坑（为什么必须走 `.material`）→ `CardFan`。
            //    ⚠️ **这一段必须落在【遮罩之上】**（`QShade = 3105`）：原来卡格在 `3009–3017`、遮罩在 `3110`
            //    ⇒ **整叠卡被压暗了一半**（2026-09-28 实拍量出来的，见本文件头部那段订正）。
            CardFan.ApplyQueues(SlotViews, SlotCount, QCardStack);
            _swapping = false;
            Debug.Log($"[CardDetail] 卡片那一叠：**{SlotCount} 格**（1 主卡 + {Mathf.Max(0, SlotCount - 1)} 相关卡）"
                    + " —— 扇形：槽 0–4 照原版 clip `Card Display Open`，槽 5–8 是**我们外推的**（正本 §8·7）");
        }

        /// <summary>第 `i` 格的点击区（原版 `CardUI/2DCard/UI Collider` —— **每格一个**），
        /// 矩形判据只此一份 → `CardFan.HitW/HitH/HitCy`。
        /// **前台那格也要**：它的回调在闸②直接 return，作用是「吃掉这一下」，
        /// 否则会落到 `BackgroundHit` 上**把窗关掉**；<b>与卡面有没有画出来无关</b>（A157）—— 见调用点的注释。
        /// 🔴 z 阶梯：`PointerLayer.HitButton` 同队列时**取 z 小的那个** ⇒ 越靠前台 z 越小。
        ///
        /// <para>🔴 **2026-10-17（`T09` 卡体批派生件 `EW12` 现核）：本窗这一格有一处【已知偏离】——
        /// 命中区**不跟卡的扇形角转**。本件**修不了**（修点不在白名单）；真修点见下面 ③。**</para>
        /// <para>**① 本窗这一叠卡是【转】的**（⚠️ 别以为壳侧不转）：`BuildCardStack` 把 `CardFan.Rot(i)`
        /// （`0° / 2.510° / 5.890° / 12.750° / … / 32.4°`）交给了 `CardView.SetPose`；
        /// 而原版那颗 `UI Collider` 是 `2DCard` 的**子件**、**随卡一起转**
        /// （判据 → `资料/普查产出_1016并行/S7_卡体终点形态_报告.md` §一·B）。
        /// ⇒ 槽 1–8 上这个轴对齐框与**画出来的卡**对不上：θ = 32.4°（槽 8）时两块命中区的
        /// **最大距离（Hausdorff）= 0.6273 卡单位**（槽 8 自己那档 **86.4 px**、按前台档 156.8 px）·
        /// **对称差 = 命中区面积的 38.2%**（两边各 19.1%）；槽 1（2.510°）= **0.0624 卡单位**
        /// ≈ **14.5 px** / 4.6%；槽 0（0°）**差 0**。
        /// 🔴 **复算订正（本件现算）**：`EW9` 报告 §③③ 那式 `(w/2)·sinθ + (h/2)(cosθ−1)` 用的是
        /// **卡身**尺寸、且**只是【纵向】**那一半；按**命中矩形** `1.8927×2.8913` 复算纵向 0.2820 ·
        /// 横向 0.6273（**更大**）⇒ 按大的记。</para>
        /// <para>**② 为什么本文件里改不出来**：判命中的**不在本文件** —— `Shell/PointerLayer.cs` 的
        /// `CollectHits`（那两句 `Mathf.Abs(px − c.x) > half.x`）与它唯一的实现 `HitBoxPx`
        /// （`center` = `ToDesignPixel(q.transform.position)` · `half` = `q.WorldW/WorldH × lossyScale`）
        /// **是一套【轴对齐】模型、不读任何旋转** —— 连它自己那套「命中区 ≠ 画出来那块」的登记表
        /// （`RegisterHitRect` / `ApplyHitRectOverride`）也只是按比例换 `center`/`half`，**仍然轴对齐**。
        /// ⇒ 把 `hit.localRotation` 拧成 `CardFan.Rot(i)` 对 `HitBoxPx` 那两个读数**逐位不变 = 零效果**
        /// （子 quad 建在 `Vector3.zero`、`WorldW/H` 与 `lossyScale` 都不含旋转）⇒ 那样写只是**假装修了**。</para>
        /// <para>**③ 真修点（已进接线清单）**：给 `PointerLayer` 加一条**有向矩形**路径 ——
        /// 原版 `Graphic.Raycast` 本来就是 `RectTransformUtility.RectangleContainsScreenPoint(...)`，
        /// 它把屏幕点**逆变换进该 `RectTransform` 的局部空间** ⇒ **天然含旋转**。
        /// 那一步做完，本窗只需把 `hit.localRotation` 设成 `CardFan.Rot(i)`（与卡的扇形角**同一个数**）；
        /// ⚠️ 届时还差原版 `ap(0,−0.02)` 那 5px 的**方向**：现在这句把下移加在**屏幕 y** 上
        /// （`CardFan.HitCy` = 卡心 + 5px），原版加在**卡的局部系**上
        /// ⇒ 旋转档下中心偏 `5·sinθ`（θ = 32.4° 时 **≤2.68 px**）。</para></summary>
        void BuildCardHit(Transform node, int i)
        {
            float hy = CardFan.HitCy(i);                       // 点击区中心**比卡心低**（原版 `ap.y = −0.02`）
            float hw = CardFan.HitW(i) * 0.5f, hh = CardFan.HitH(i) * 0.5f;
            float fx = CardFan.Cx(i);
            var hx = new PxRect(fx - hw, hy - hh, fx + hw, hy + hh);
            var hit = Hit(node, "CardHit " + i, hx, () => SwapToFront(i));
            if (hit != null)
                hit.localPosition = new Vector3(hit.localPosition.x, hit.localPosition.y, i * HitZStep);
        }

        // 🔴 **「相关卡是谁」那段（点名那一支 + 池子那一支）已收口到 `Core/RelatedCards.cs`**
        //    —— 两扇窗共用一份。那里也写着**原版其实走的是卡上自带的 `relatedCard1..4` 四个字段**
        //    （值在服务端，本地拿不到 ⇒ 我们这套是替身），判据全文 → 正本 §十·1。

        /// <summary>风味条那一行字（原版 `LoreText`）。**空 = 这张卡没有官方 Lore** —— 那时整块底板不亮
        /// （原版 `SetCardLore.c:24-27` 的 `SetActive(loreObjectBG, !IsNullOrEmpty(flavourText))`）。
        /// 🆕 2026-10-17（`A1802` / `A1500①`）：来源 = `Loc.Lore(卡 id)`（官方 `Warlord_Lore/*`），
        /// ⛔ **不再是卡面效果文字**。</summary>
        string _loreText;

        /// <summary>效果文字条那行字。原版这里是 **Lore 背景故事**。
        /// ⚠️ 换位收尾（原版 `CardSwapFinished` 的 `SetCardLore(新前台)`）会**重建**它 ⇒ 抽成独立方法。
        /// 🔴 **2026-09-28：原版是【居中】不是右对齐** —— `LoreText` 的 TMP
        /// `m_HorizontalAlignment = 2`（Center；位标志 Left=1/Center=2/Right=4），实拍也一致
        /// （正本 §十·5 第 4 条）。`MenuDraw.Text` 默认居中 ⇒ **不要**再 `AlignRight`。
        /// 🔴 **2026-10-17（`A1802` + `A1500①`，本件现改）**：那行字的来源换成**官方 Lore**
        /// （`Loc.Lore(卡 id)` → `Warlord_Lore/<卡 id>`，数据件 `Resources/lore_zh+en.json`）。
        /// **没有 lore ⇒ 底板 + 字一起不建**（原版 `SetCardLore.c:24-27`），⛔ 不像原来那样拿
        /// `BattleDriver.FaceTextFull(def).body`（卡面效果文字）顶上去 —— 那是**另一回事**，会静默误导。
        /// 覆盖缺口：我们卡池 1127 张里 128 张没有官方键（**2025-07-30 快照之后**的新卡）⇒ 它们照原版
        /// 「无风味文本」处理（底板关掉），并且 `Loc` 会逐键出声一次。</summary>
        void BuildLoreText(Transform bg)
        {
            var def = FrontDef ?? Card;
            // ① 那行字：**官方 Lore**（没有 ⇒ 整块不亮，见下面 ③）
            _loreText = def != null ? Loc.Lore(def.Id) : null;
            if (bg == null) return;
            for (int i = bg.childCount - 1; i >= 0; i--) CollectionWindow.DestroySafe(bg.GetChild(i).gameObject);
            if (string.IsNullOrEmpty(_loreText))
            {
                _loreText = null;
                bg.gameObject.SetActive(false);
                return;
            }
            // ② 底板：**按阵营**选图（原版 `FlavourTextSO.GetClanFlavorBackground`）—— 认的是**当前前台**那张；
            //    换位收尾会重建本方法 ⇒ 阵营跟着换。⚠️ 原版 `m_PreserveAspect = 0`（实读）⇒ **拉伸**，别开 keepAspect。
            var tex = CardArt.FlavorBg(def != null ? def.Faction : null);
            if (tex != null)
                MenuDraw.Rect(bg, tex, new PxRect(LowerL, LoreBgT, LowerR, LoreBgB), "Image", QCdRow, null, false);
            else
                Debug.LogWarning("[CardDetail] 阵营「" + (def != null ? def.Faction : "?")
                               + "」没有风味底图（`CardArt.FlavorBg` 取不到）⇒ **只画字**（不静默）");
            // ③ 那行字
            // 🆕 **2026-10-12（A333 + A336③）**：上限 = 原版 `m_fontSizeMax`（**35** = 标称 `LorePx`，本来就对）·
            //   base = 原版 `m_fontSizeBase` = **36.0**。判据 = 本窗那一颗 `LoreText` 的 MB 实读
            //   `bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_1866.json`
            //   （`m_text` 是那张卡的风味文本、`fs 35.0 · auto[10.0~35.0] · base 36.0 · 折行 1`；
            //   正本 §一 记的 `LoreText … fs35 hAlign=Right` 与 `fs` 这一格吻合 ⇒ 同一颗）。
            var r = new PxRect(LoreL, LoreT, LoreR, LoreB);
            MenuDraw.Text(bg, r, _loreText, Color.white, "LoreText", LorePx, QCdText, LoreR - LoreL, 10f, LorePx, 36f, face: TmpFont.Face.LatinUi);
        }

        /// <summary>点第 `idx` 格 ⇒ **和前台那张两两换位**（原版 `CardDisplayWindow.ChangeCardPosition`，
        /// 253 行逐行读过 —— 判据全文 → 正本 §9·6）。
        /// **三个闸**（任一中就整个 return，什么都不做）：① 上一次没播完（`swappingCards`）·
        /// ② 点的是前台自己 · ③ 有 `Animation` 在播。
        /// ⚠️ 闸③ **本工程不适用** —— 原版那条摆位用的 `Animation` 我们没有（我们按 `CardFan` 的常量直接摆），
        /// 这一点是**如实说明的偏离**。
        /// ⚠️ 层级对调照原版是 `SetSiblingIndex` **两两互换**（**不是「提到最前」**）；
        /// 我们**另外**还要按新槽位重排**渲染队列**（这套的前后由队列决定，见 `CardFan`）。
        /// ⚠️ 收尾还要**跟着换着色**（原版 `ChangeCardPosition.c:203-221`）。</summary>
        public void SwapToFront(int idx)
        {
            if (_swapping)
            {
                Debug.Log("[CardDetail] 换位被挡：上一次还没播完（原版闸① `swappingCards`）");
                return;
            }
            if (idx <= 0 || idx >= Slots) return;                  // 闸②：点的是前台自己（位姿槽 0）
            var a = SlotViews[0]; var b = SlotViews[idx];
            if (a == null || b == null) return;
            _swapping = true;

            // 🔴 **2026-10-22（卡牌线 `T04`）：换位【一开始】就换「谁能溢出」。**
            //    原版 `CardDisplayWindow__ChangeCardPosition.c:75-79`（亲读）——
            //    `BasicCardUI__SetImageOverDraw(cardInFront, 0)` ⇒ 让位那张收回；
            //    `BasicCardUI__SetImageOverDraw(被点那张, 它的 useOverDraw)` ⇒ 新前台按**它自己的**卡表值决定。
            //    本窗这里只改「前台」那一轴（`ArtOverdraw`）；逐卡那一轴（`UseOverDraw`）在 `BuildCardStack`
            //    建卡时就按卡表定死了 ⇒ 两轴相与的结果与原版那句**同义**（逐卡为 0 的卡换到前台照样不溢出）。
            //    ⚠️ 位置就是这里（原版那两句在两条补间**之前**），⛔ 别挪进 `OnComplete`
            //    （原版 `CardSwapFinished` 只管 lore / 语音 / 颜色）。
            b.ArtOverdraw = true;     // 被点那张 → 即将成为前台
            a.ArtOverdraw = false;    // 让位那张 → `CardImageController` 状态 0

            var pa = a.transform.position; var pb = b.transform.position;
            float ra = a.transform.localEulerAngles.z, rb = b.transform.localEulerAngles.z;
            // 缩放跟着【槽位】走：被点那张 → 前台档（= 前台那张现在的**基础**缩放）；前台那张 → 让位的槽位档。
            // 🔴 **取 `BaseScale`、⛔ 不是 `localScale.x`** —— `localScale` 含高亮系数（`_hlMul`），
            //    而 `TweenPose` 的 `scale` 形参正是写 `_baseScale` 的那一个（同 `SetPose`）。
            float sa = a.BaseScale, sb = b.BaseScale;

            // 六条 tween 全用同一个时长（原版 `relatedCardSwapTime` = 0.25s）：
            // 被点那张 → 前台位；前台那张 → 被点卡的槽位。
            //
            // 🔴 **2026-10-24（`A11` · 铁律 5 就地订正）：位姿补间从 `CardTween.ToPose` 换成 `CardView.TweenPose`。**
            //   判据 = `Core/CardView.TweenPose` 的 doc（`T04`）：`CardTween.ToPose` 是
            //   `DOMove/DOLocalRotate/DOScale`，**直写 `transform.localScale`**，而本类的缩放是
            //   「**基础缩放 × 高亮系数**」（`BaseScale` / `ApplyScale`）⇒ 补间期间 `_baseScale` 一直停在
            //   **原槽位**的值上：换位一结束，任何一次 `SetPose`（重排 / 切分辨率）或 `SetHighlightScale`
            //   都会把卡**弹回旧缩放**（静默；图上只表现为「换位后位置对、大小不对」）。
            //   战斗侧同名逻辑 `Battle/CardDisplayWindow.cs` 早已在 `T04` 改掉，**本窗一直挂着**（账在
            //   `资料/普查产出_1024/A3_手牌卡面五件.md` §六）⇒ 本笔收口。
            //
            //   🔴 **ease 的约定在两个入口上是【反】的（⛔ 别对调）**：
            //     · `CardTween.ToPose` = 一个 Sequence + 3 条 Join ⇒ 只能传 **`Linear`**
            //       （Sequence 的 ease 是整条时间轴的：传别的 = 在子补间的 OutQuad 之上再弯一次）；
            //     · `CardView.TweenPose` = **单条** `DOTween.To`（每帧经 `SetPose` 写）⇒ 传 **`OutQuad`**
            //       （= 原版那三条单补间各自吃的 `DOTween.defaultEaseType` = 6）。
            //   判据：原版 `CardDisplayWindow__ChangeCardPosition.c` 全文一个 `DOTween.Sequence()` + 六条
            //   `Join`、**没有** `SetEase`（`grep SetEase` 0 命中）⇒ 每条子补间各走默认 OutQuad
            //   （`DOTween.dll` 反编译：`Sequence.Setup` 把序列自己的 `easeType` 硬编码成 `Linear`、
            //   `DoApplyTween` 在 Linear 时整段跳过缓动；`Tweener.Setup` 才取 `DOTween.defaultEaseType`）。
            //   改坏法：这里退回 `Linear` ⇒ 半程位移 0.5（OutQuad = 0.75）；退回 `CardTween.ToPose(…, OutQuad)`
            //   ⇒ 0.9375（二次弯曲）。
            //
            //   ⚠️ **`pos` 是【局部】位**（`TweenPose` 同 `SetPose`）：两张卡各自挂在自己的位姿节点下
            //   （`MenuDraw.Node`）⇒ 目标**世界**位必须经**目标卡自己的父件**反算回局部。
            //   今天父件是纯平移（rotation / scale 恒等）⇒ 与直接传世界位等价，但⛔ 别因此省掉这一步
            //   （父链一有旋转/缩放就分家 —— 同 `CardView.TweenPose` doc 里那条 ⚠️）。
            Vector3 pbInB = b.transform.parent != null ? b.transform.parent.InverseTransformPoint(pa) : pa;
            Vector3 paInA = a.transform.parent != null ? a.transform.parent.InverseTransformPoint(pb) : pb;

            void FinishSwap()
            {
                // 层级对调：**两两互换** sibling 序（照原版 `SetSiblingIndex`）
                int ia = a.transform.GetSiblingIndex(), ib = b.transform.GetSiblingIndex();
                a.transform.SetSiblingIndex(ib);
                b.transform.SetSiblingIndex(ia);
                // 记账：两个**位姿槽**上的卡对调（前台永远是位姿槽 0）
                var td = _slotDefs[0]; _slotDefs[0] = _slotDefs[idx]; _slotDefs[idx] = td;
                var tv = SlotViews[0]; SlotViews[0] = SlotViews[idx]; SlotViews[idx] = tv;
                // 🔴 **队列要按【新的槽位】重排**（sibling 序换了、我们这套的**前后由渲染队列决定**）
                //    —— 不重排的话「刚换到前台的那张」还带着原来那个较低的号、会被身后的卡盖住。
                CardFan.ApplyQueues(SlotViews, SlotCount, QCardStack);
                _swapping = false;
                // 收尾（原版 `CardSwapFinished`）：开闸（上面已做）· 更新前台指针（上面已做）·
                // **重设 lore**（`SetCardLore`）· **重设语音 / 眼睛钮**（`SetCardVoiceOver` 那两条）。
                BuildLoreText(Find(transform, "FlavourTextBG"));
                RefreshLore();
                RefreshButtons();
                Debug.Log("[CardDetail] 换位完成：前台现在是「" + (FrontDef != null ? FrontDef.Name : "?")
                        + "」（收尾重设了 lore / 语音 / 眼睛钮 —— 原版 `CardSwapFinished`）");
            }

            // ① 被点那张 → 前台位（`FinishSwap` 挂在它身上：原版换位收尾也是等这一条补间跑完）
            var twB = b.TweenPose(pbInB, ra, sa, SwapTime, DG.Tweening.Ease.OutQuad);
            if (twB != null) twB.OnComplete(FinishSwap); else FinishSwap();   // 兜底：`TweenPose` 在 `duration<=0` 时返回 null
            // ② 前台那张 → 被点卡的槽位（同上：单条补间 ⇒ `OutQuad`，⛔ 不是 Sequence 的 `Linear`）
            a.TweenPose(paInA, rb, sb, SwapTime, DG.Tweening.Ease.OutQuad);
            // 着色跟着换（原版 `ChangeCardPosition.c:203-221` 那两条 tween，同一时长）：
            // 去前台那张 → 白，让出前台那张 → `cardInBackGroundColorTint`(0.65)。
            CardFan.TweenTint(b, CardFan.BackTint, CardFan.FrontTint, SwapTime);
            CardFan.TweenTint(a, CardFan.FrontTint, CardFan.BackTint, SwapTime);
            Debug.Log($"[CardDetail] 换位：第 {idx} 格 ⇄ 前台（{SwapTime}s = 原版 `relatedCardSwapTime`）");
        }

        /// <summary>这张卡**能不能「创建副本」（合成）** —— 决定那块面板建不建。
        ///
        /// 🔴 **2026-09-27 用户指出：`special` 那批不能合成**。而那 50 张**不是「第五档稀有度」**，
        ///   是 **防御卡 39 + 药剂卡 6（`Combat Elixir`）+ 秘仪 5（`Secret`）** 在源数据里
        ///   **没有稀有度被塞的占位值**（`cards_engine.json` 实测：`special` 全是 `type=defence/tactic`）。
        ///   ⇒ **判据 = 稀有度是不是那四档之一**（与 `WildcardIconFor` 同一张表）。
        ///
        /// ⚠️ **原版运行时长什么样，关服查不到**（可能整块隐藏、也可能是个灰掉的按钮）
        ///   ⇒ 这里选**整块不建**，**不造一个假的禁用态**（同 `CLAUDE.md`「宁可没有，不可错着显示」）。
        ///   真要改成「建出来但点不动」是一行的事 —— 先把口径记在这儿。</summary>
        static bool Craftable(string rarity)
        {
            switch ((rarity ?? "").ToLowerInvariant())
            {
                case "common": case "rare": case "epic": case "legendary": return true;
                default: return false;
            }
        }

        // ---- ① 创建副本 ----
        /// <summary>万能卡图标 —— **四档稀有度各一张**（原版图集 `40k_general_wildcard_*_small`）。
        /// 🔴 **稀有度是四档**（`common/rare/epic/legendary`）—— 卡池里那个 `special`
        ///   （**防御卡 39 + 战术卡 11**，`cards_engine.json` 实测）**不是第五档**，
        ///   是「这张卡没有稀有度」被源数据塞了个占位值（2026-09-27 用户指出、逐条核过）。
        /// 🔴 **判据只此一处** —— 通配符计数条（`BuildWildcards`）与「创建副本」按钮的图标（`BuildCrafting`）共用它。</summary>
        static readonly string[] WildcardIcons =
        {
            "40k_general_wildcard_common_small", "40k_general_wildcard_rare_small",
            "40k_general_wildcard_epic_small",   "40k_general_wildcard_legendary_small",
        };

        /// <summary>🆕 **2026-10-18**：与 <see cref="WildcardIcons"/> **逐下标对应**的稀有度键
        /// （计数条那 4 格的数字现在读**真库存** `CardProgress.Wildcards(rarity)`，不再是写死的 `99`）。
        /// ⚠️ 两个数组的次序**必须同序** —— 改一个就要改另一个（下面 `BuildWildcards` 按下标同时用）。
        /// 🔴 **2026-10-24**：它同时是「稀有度 → 原版 `craftVFXs` 的 1 基 rarity」的**唯一判据**
        /// （<see cref="CraftRarityIndex"/>）—— ⛔ 别在调用点另写一份 switch。</summary>
        static readonly string[] WildcardRarities = { "common", "rare", "epic", "legendary" };

        /// <summary>稀有度键 → 原版 `CraftingPanel.craftVFXs[]` 的 **1 基 rarity 号**
        /// （1=Common · 2=Rare · 3=Epic · 4=Legendary）。认不出（`special` / 空）⇒ **0**。
        ///
        /// <para>判据（只此一处）：<see cref="WildcardRarities"/> 的下标 + 1。原版那张表的键就是
        /// `UpgradeFXByRarity.rarity`（场景 `MainMenu Warpforge.unity` 的 `craftVFXs[]`，逐行实读 →
        /// `WarpforgeUiEffect.WildcardCraftPrefab` 的文件头 ①）。`special` 那一档原版**不能合成**
        /// （<see cref="Craftable"/> 只放行四档，2026-09-27 用户指出）⇒ 走不到这里。</para></summary>
        static int CraftRarityIndex(string rarity)
        {
            return System.Array.IndexOf(WildcardRarities, (rarity ?? "").ToLowerInvariant()) + 1;
        }

        /// <summary>按稀有度取万能卡图标。**认不出时返回原版 prefab 的默认那张** ——
        /// 原版 `Craft/WC icon` 的 `m_Sprite` 存的就是 `40k_general_wildcard_epic_small`，
        /// 而 `CraftingPanel` 上有 `wildcardIcon` / `wildcardSprites` 两个字段
        /// ⇒ **运行时是脚本按卡换的**；**防御卡那 39 张运行时画哪张，关服查不到**
        /// ⇒ 不猜，用 prefab 的原值，并如实说出来。
        /// <para>🔴 **2026-10-18：本函数当前【没有生产调用点】**（出声、不静默）—— 用户当天拍板
        /// 「创建副本按钮去掉数字，只留『创建副本』四个字」⇒ `BuildCrafting` 里那颗 `Craft Icon` 不建了。
        /// **留着而不是删掉**：它是「按卡的稀有度选万能卡图标」这**一条判据的唯一一份**（连同
        /// <see cref="WildcardIcons"/> 的同序约定），原版那颗钮将来若要恢复（判据 rect 记在
        /// `资料/普查产出_1018/W5_升级与副本_落地.md` §4）就照它建回来；⛔ 别再另写一份 switch。</para></summary>
        static string WildcardIconFor(string rarity)
        {
            switch ((rarity ?? "").ToLowerInvariant())
            {
                case "common":    return WildcardIcons[0];
                case "rare":      return WildcardIcons[1];
                case "epic":      return WildcardIcons[2];
                case "legendary": return WildcardIcons[3];
                default:          return WildcardIcons[2];   // = prefab 原值（`_epic_small`）
            }
        }

        void BuildCrafting(Transform panel)
        {
            var p = PanelBox(panel, "Crafting Panel", CraftT, CraftB);
            // 🔴 **2026-10-18 用户拍板：这一块的两句英文改中文** —— 走**官方本地化表**那两条（⛔ 不写死字面量）：
            //   `MenuDeck/Craft/CraftDescription` = **「创建这张牌的副本」** ·
            //   `MenuDeck/Craft/CraftCost` = **「这将消耗一个通配符」**（判据 = `Resources/official_loc_13lang.json`
            //   的 `zh` 列，2026-10-18 现读；`Loc.T` 那三档兜底的最后一段就是这张表）。
            //   ⚠️ 中文串传 `Face.Cjk`（同本文件「将此张牌降级或升级」那一处）—— 混排兜底虽然也画得出汉字，
            //   但字号是按 `ActualFace` 的 em 换算的 ⇒ 纯中文串走 CJK 面才与本仓其它中文同一档。
            Title(p, "Crafting Title", Loc.T("MenuDeck/Craft/CraftDescription"),
                  1307.5f, 165.17f, 1726f, 244.29f, 42f, TmpFont.Face.Cjk);
            var b = new PxRect(1384f, 244.29f, 1654f, 304.95f);
            // ⚠️ `UI_Button_Mulligan` **不是九宫格图**（实读 `Sprite/UI_Button_Mulligan.json`：`m_Border = None`，
            //    图 410×124）—— 原版标 `type=Sliced` 但**没有 border 就等于拉伸** ⇒ 我们直接走 `Rect`。
            //    （走 `Nine` 会吐一串「border 比图还大，退回单块」的警告，而结果一样。）
            var craftBg = MenuDraw.Rect(p, CardArt.MenuUi("UI_Button_Mulligan"), b, "Craft Bg", QCdRow);
            // 🔴🔴 **2026-10-18 用户拍板（逐字）：「创建副本按钮去掉数字，只留『创建副本』四个字」**
            //   ⇒ 原来那颗钮上的两件**不再建**：
            //     ① `Craft Text`（「1」，原版 `Button Text`，rect `1383.96,249.22→1506.84,299.88` @ 40px）；
            //     ② `Craft Icon`（原版 `WC icon` = `40k_general_wildcard_<稀有度>_small`，
            //        rect `1506.88,249.22→1653.99,299.88`，`Simple + preserveAspect` ⇒ 实绘 40.7×50.66）。
            //   ⚠️ **文案是【用户指定】的，不是原版词条**（原版那颗钮上只有 `1` + 通配符图标、没有字）
            //      ⇒ 用**字面量**「创建副本」（用户 2026-10-18 指定）；⛔ 别往 `official_loc` 里塞这条键。
            //   ⚠️ 字号 **36** = 与同一列那两颗用户同一天定的「降级 / 升级」同一档（⛔ 别另挑一个数）。
            //   ⚠️ 那两件原版的 rect 判据留着（上面两段注释已删，出处另存 → `普查产出_1018/W5_升级与副本_落地.md` §4）：
            //      将来若要恢复，照那两组 rect + `WildcardIconFor(rarity)` 建回来即可。
            MenuDraw.Text(p, b, "创建副本", Color.white, "Craft Label", 36f, QCdText);
            // 🔴 **2026-10-18（A1126 · A1 档）**：这一处原来**没传 autosize 实参** ⇒ 固定 40px。
            //   判据 = 逐颗现读原版那一颗（`bundle_scenes_scenes_mainmenuwarpforge`，按 `m_text` 认人：
            //   `MonoBehaviour_1830.json` ← `RectTransform_1501` ← GO `Explanation`，本窗是**场景对象**、不是 prefab）：
            //   `'This will consume a wildcard'` · `m_fontSize` **40.0** · `m_fontSizeBase` **36.0** ·
            //   **`m_enableAutoSizing = 1`** · `m_fontSizeMin` **10.0** · `m_fontSizeMax` **40.0** ·
            //   对齐 `Center/Middle` · **`m_TextWrappingMode = 1`**（折行）⇒ 与本处传 `wrapPx` 同档。
            //   ⚠️ 原版那一颗的 `m_SizeDelta = (0,0)` + 祖先链上 `m_LocalScale = (0,0)`（动画/隐藏态）
            //   ⇒ **原版框宽量不到**，本处的 `1307.5 → 1726`（418.5）**是我们摆的**（铁律 3：查不到就说查不到）。
            //   ✅ **2026-10-12（A1699）就地订正（铁律 5）**：本处原来记着「这条调用**当前没有生产路径**
            //   …… `BuildCrafting` 全库**零调用点**」—— **已过期**：两块面板按用户 2026-10-12 的口径恢复，
            //   `Build()` 里现在会调 `BuildCrafting`（稀有度可合成的卡）⇒ **这一句是活的**，
            //   `Editor/ShellScene.cs` 那条「读源码文本」的静态断言之外，本处现在也走得通了。
            //   📌 也一并销掉那句「『要恢复：把下面那两行注释掉的条件去掉即可』与现状不符」——
            //   恢复**已经做完**，那句话（连同它指出的事实）都不必再留。
            // 🔴 **2026-10-18 就地订正（铁律 5 · 用户当天拍板「照参考图那样布置两块面板」）**：
            //   本行原来印的是 `This will consume a wildcard  Not implemented in this build*`
            //   （2026-10-12「只出声、不做机制」那版留下的标识）。**那个前提已经不成立**：
            //   创建副本现在是**真做**的（扣通配符 + 拥有数 +1 + 落盘，见 `DoCraft`）⇒ **原样印回原版文案**。
            //   判据：原版 TMP `MonoBehaviour_1830.json`（GO `Explanation`）= `'This will consume a wildcard'` ·
            //   `fs 40 · auto[10~40] · base 36 · 折行 1`；本地化键 `MenuDeck/Craft/CraftCost` 同文。
            //   ⛔ 别再把 `*` 缀回来 —— 那个记号是「本版未实现」，现在实现了还缀着就是**反向的谎**。
            //   ⚠️ 框宽 `1307.5 → 1726`（418.5）**是我们摆的**（原版那一颗 `m_SizeDelta=(0,0)` +
            //   祖先链 `m_LocalScale=(0,0)` ⇒ **原版框宽量不到**，铁律 3：查不到就说查不到）。
            // 🆕 **2026-10-18（用户拍板）：文案改中文** —— 走**官方本地化键** `MenuDeck/Craft/CraftCost`
            //   （zh 列 = **「这将消耗一个通配符」**；与上面那条 title 同一张表、同一套取法）。
            //   ⚠️ **四档 TMP 实参逐值不动**（`wrap 418.5 · min 10 · max 40 · base 36` —— 判据仍是原版那一颗
            //   `MonoBehaviour_1830.json`，见上）；只把 `face` 换成 `Cjk`（中文串，理由同 title 那一处）。
            MenuDraw.Text(p, new PxRect(1307.5f, 315.5f, 1726f, 378.79f),
                          Loc.T("MenuDeck/Craft/CraftCost"),
                          Color.white, "Craft Explanation", 40f, QCdText,
                          1726f - 1307.5f, 10f, 40f, 36f, face: TmpFont.Face.Cjk);
            MenuDraw.Rect(p, CardArt.MenuUi("40K_generic_bt_info"), new PxRect(1740.65f, 161.91f, 1782f, 203.26f),
                          "Craft Info Icon", QCdRow, null, true);
            // 🔴🔴 **2026-10-24 改判（主对话裁定 · 铁律 5 就地订正）**：本段原来的口径是
            //   2026-10-18 用户那句「**点击副本，但是不起作用**」⇒ 空实现 + `Interactable` 恒 `true`。
            //   现行口径 = **「请求 + 特效」一起做**（判据 → `资料/任务线/特效线/任务总表.md` 那条
            //   「锻造按钮的完整链路」）：**⛔ 不合成却播特效 = 假反馈** ⇒ 点一下要**真发合成请求**
            //   （`DoCraft`：扣 1 张同稀有度通配符 + `owned +1`，**不动货币** —— 单机资源固定 2000），
            //   **同一拍**再播原版 `craftVFXs` 那两件（见 `OnCraftClicked` / `PlayCraftVfx`）。
            //   ⇒ 可交互度**回到原版 `CraftingPanel__CheckButtons.c` 的判据**：
            //     `interactable = CheckForCardCrafting(...)`（**有同稀有度通配符才可点**）。
            //   ⚠️ **按钮音照旧保留**：用户 2026-10-18 追加「这个继续保留音效啊」；cue 归真 =
            //     原版 `…/Crafting Panel/Content/Craft` 的 `m_onClickSound = ButtonClick Positive`
            //     （音效线缺口 `AUD` §三 B2）。
            var craftHit = Hit(p, "CraftHit", b, () => { OnCraftClicked(); }, QCdHit, craftBg, "UI_Button_Mulligan",
                              clickCue: WindowSounds.PositiveCue);
            var craftWb = craftHit != null ? craftHit.GetComponent<WindowButton>() : null;
            if (craftWb != null)
            {
                // 原版 `CheckButtons`：没有通配符 ⇒ `Selectable.interactable = false`（灰掉、点不派发）。
                int have = CardProgress.Wildcards(Card.Rarity);
                craftWb.Interactable = have > 0;
                craftWb.playClickSound = true;    // 用户 2026-10-18 追加：**保留按钮音**（⛔ 别再关掉）
                if (have <= 0)
                    Debug.Log("[CardDetail] 「创建副本」那颗钮 = **不可交互**（「" + Card.Rarity + "」通配符 0 张）"
                            + " —— 判据 = 原版 `CraftingPanel__CheckButtons.c` 的 `set_interactable(CheckForCardCrafting())`");
            }
            else Debug.LogWarning("[CardDetail] 「创建副本」那颗钮的 `WindowButton` 拿不到 ⇒ "
                                + "**「请求 + 特效」没落地**（不静默）");
        }

        // ---- ② 升级 ----
        void BuildUpgrade(Transform panel)
        {
            var p = PanelBox(panel, "Upgrade Panel", UpgT, UpgB);
            // 🆕 **2026-10-18（用户拍板「照参考图那样布置两块面板」）**：内容照原版装进 `Content` 节点。
            //   原版字段表（`d:/2/tools/decomp_full/UpgradePanel__CheckUpgradeRequirements.c`）：
            //     `+0x30 Content` · `+0x38 No Upgrade Warning` · `+0x40` 那颗钮；`:113-119` 三行 =
            //     `SetActive(Content, next != null)` / `SetActive(No Upgrade Warning, next == null)` / 满级 return。
            //   🔴 **2026-10-18 当天第二轮改判（见本函数末尾那一大段）**：用户把一颗钮改成「降级 / 升级」两颗
            //   ⇒ **`Content` 改为常显**（满级时还得点得到「降级」），**`No Upgrade Warning` 仍只在满级露**
            //   但文字挪到空出来的 `Explanation` 那一行。⛔ 别再照原版那条「满级 ⇒ `Content` 整组关」改回去。
            var content = MenuDraw.Node(p, "Content", new PxRect(PanL, UpgT, PanR, UpgB));
            int lv = CardProgress.Level(Card.Id);
            bool maxed = lv >= CardProgress.MaxLevel;
            bool minned = lv <= 1;
            // 🆕 **2026-10-18（用户当天拍板，逐字）**：「**上面的文字『将此张牌升级至等级x』改为『将此张牌降级或升级』**」。
            //   ⚠️ 这句**是用户指定的文案、原版没有** ⇒ 用**字面量**（⛔ 不往 `official_loc` 里塞键，也别再去
            //   `MenuDeck/Craft/Upgrade` 那条链里取 —— 那条原版词条已被这句话**取代**，不是「暂时没本地化」）。
            //   `face: Face.Cjk` = 纯中文串走本仓默认那一面（见 `Title` 的 `face` 形参注释）。
            Title(content, "Upgrade Title", "将此张牌降级或升级",
                  1307.5f, 437.28f, 1726f, 515.77f, 41.4f, TmpFont.Face.Cjk);
            // ============================================================ 🆕 2026-10-18：**一颗钮 → 两颗并列**
            //
            // 用户原话：「下面的按钮从**一个按钮改为两个『降级』『升级』并列**，可以点击这两个按钮实现卡框的
            //   降级和升级」。
            // 几何：**原单颗 `Upgrade` 钮那一条带**原样让给这两颗 —— `1384 .. 1654.38 × 524.96 .. 585.31`
            //   （判据 = 运行时 dump `runtime_ui_dump_menu_forge.tsv`，见 `W5` 报告 §4.3）。两颗**等宽**，
            //   中间留 `gap = 36`（= 本壳「一排并列两三颗钮」的既有值，`Shell/DeckInfoPopup.cs` 的
            //   `BtnGap`；⚠️ 原版**没有**这一对钮 ⇒ 间距只能沿用本壳既有档，⛔ 别另挑一个数）。
            var b = new PxRect(1384f, 524.96f, 1654.38f, 585.31f);
            const float gap = 36f;
            float halfW = (b.W - gap) * 0.5f;                       // 117.19
            var bd = new PxRect(b.x1, b.y1, b.x1 + halfW, b.y2);    // 降级：1384.00 .. 1501.19
            var bu = new PxRect(b.x2 - halfW, b.y1, b.x2, b.y2);    // 升级：1537.19 .. 1654.38
            // ⚠️ **`Interactable` 必须排在 `Hit(...)` 之后**（`Hit` 内部才 `Bind` 那颗画出来的底图）：
            //   `GrayTargets()` = 四组去重（`_nine` + `target` + 本钮子树 + `TintOn`），而 `target` 是 `Bind` 才设的
            //   ⇒ 排在前面，头一次 `RefreshGray()` 只灰到那颗**透明的命中区 quad**、`GrayedForTest` 随即为真
            //   ⇒ 后面再设都早退 ⇒ **画面上一像素都不变**（静默）。同族先例 `Shell/CampaignRewardWindow.cs`。
            //
            // ① **降级**：`SetLevel(level−1)`（下限 1）⇒ 重画 ⇒ 卡框降一档；**等级 = 1 时置灰**（用户口径）。
            var downBg = MenuDraw.Rect(content, CardArt.MenuUi("UI_Button_Mulligan"), bd, "Downgrade Bg", QCdRow);
            var downHit = Hit(content, "DowngradeHit", bd, () => { DoDowngrade(); }, QCdHit, downBg,
                              "UI_Button_Mulligan", clickCue: WindowSounds.PositiveCue);
            MenuDraw.Text(downHit, bd, "降级", Color.white, "Downgrade Text", 36f, QCdText);
            var downWb = downHit != null ? downHit.GetComponent<WindowButton>() : null;
            if (downWb != null)
            {
                downWb.Interactable = !minned;
                if (minned)
                    Debug.Log("[CardDetail] 「降级」那颗钮 = **不可交互**（已经是 1 级 = 最低档；"
                            + "用户 2026-10-18 口径：最低级**不提示**、只把钮灰掉）");
            }
            else Debug.LogWarning("[CardDetail] 「降级」那颗钮的 `WindowButton` 拿不到 ⇒ **『到底了就点不动』没落地**（不静默）");

            // ② **升级**：`SetLevel(level+1)`（上限 4）⇒ 重画 ⇒ 卡框升一档；**等级 = 4 时置灰**（用户口径）。
            var upBg = MenuDraw.Rect(content, CardArt.MenuUi("UI_Button_Mulligan"), bu, "Upgrade Bg", QCdRow);
            var upHit = Hit(content, "UpgradeHit", bu, () => { DoUpgrade(); }, QCdHit, upBg,
                            "UI_Button_Mulligan", clickCue: WindowSounds.PositiveCue);
            MenuDraw.Text(upHit, bu, "升级", Color.white, "Upgrade Text", 36f, QCdText);
            var upWb = upHit != null ? upHit.GetComponent<WindowButton>() : null;
            if (upWb != null)
            {
                upWb.Interactable = !maxed;
                if (maxed)
                    Debug.Log("[CardDetail] 「升级」那颗钮 = **不可交互**（已经是 " + CardProgress.MaxLevel + " 级 = 最高档）");
            }
            else Debug.LogWarning("[CardDetail] 「升级」那颗钮的 `WindowButton` 拿不到 ⇒ **『满级就点不动』没落地**（不静默）");

            // 🔴 **2026-10-18 用户拍板（逐字）：「在下面的文字『你将获得……』这个去掉」** ——
            //   整条 `Explanation/Text Explanation`（`You will obtain: +N`）**不建**，`W5` 刚补的那颗
            //   `Icon Forge Points Drawer Variant`（锻造点图标）**一并去掉**（用户原话点名「含锻造点图标那一组」）。
            //   ⛔ 别再把这两件建回来；`CardProgress.Points` 那一列仍在（别处与以后可能还要用）。
            //
            // 🔴 **同批附带的一处让位（如实标注，见报告 §3）**：原单颗钮**按钮内部**那四件
            //   （`cards/quantity` + `cards/icon` + `cost/quantity` + `cost/icon`，rect 全落在
            //   `1405.70 .. 1643.25 × 527.54 .. 579.54`）**随那颗钮的内容一起不建** ——
            //   用户要求「一个按钮改为两个并列按钮」占的正是那一条带，四件的 rect 与新两颗钮
            //   **几何上不可共存**（会直接压在『降级』『升级』两个字上）。⛔ 不是「顺手删的」。
            MenuDraw.Rect(content, CardArt.MenuUi("40K_generic_bt_info"), new PxRect(1740.65f, 426.42f, 1782f, 467.77f),
                          "Upgrade Info Icon", QCdRow, null, true);

            // `No Upgrade Warning`：**满级才露**（原版那件与 `Content` 同级；⚠️ **rect 已挪**，见下）
            var w = MenuDraw.Node(p, "No Upgrade Warning", new PxRect(PanL, UpgT, PanR, UpgB));
            // 🆕 **2026-10-12（A333 + A336③）**：上限 = 原版 `m_fontSizeMax` = **40**（= 标称，本来就对）·
            //   base = **36.0**。判据 = `bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_1867.json`
            //   （`m_text = 'Maximum card tier reached'` · `fs 40.0 · auto[25.0~40.0] · base 36.0 · 折行 1`
            //   —— 全库唯一一颗，与我们的文案逐字相同 ⇒ 就是它）。
            // 🆕 **2026-10-18**：rect 从原版的 `1314,430.62 → 1724,644.92` 挪到**原来 `Explanation` 那一行**
            //   （`1309 .. 1729 × 594.57 .. 659.37`）—— 理由见下面那一大段（两颗钮常显，原 rect 会压住它们）。
            //   TMP 那四档（`40 · auto[25~40] · base 36 · 折行 1`）**逐值不动**，只换框。
            // 🆕 **2026-10-19（`A1708` 复核欠账 · 静态复核）**：那四档 + 框都已是**原版值**，剩下的是
            //   「渲出来多大 / 块多高」。这里用**字体资产自己的度量**算了一遍（不是估）：
            //     · 面 = `Face.LatinCardBody` → `Pragati-Regular SDF.json`
            //       （`bundle_fonts_assets_all/MonoBehaviour/`；`m_FaceInfo` = pointSize **95** · lineHeight **90** ·
            //        ascent **70** · descent **−20** · scale 1.0）；
            //     · 文案 `Maximum card tier reached` 的 `m_HorizontalAdvance` 求和 = **867.3125** 字体单位；
            //       TMP 的换算（`TMP_Text.cs:7183`：`fontScale = fontSize / pointSize × scale × 0.1`(3D)）
            //       ⇒ **40 px 档：文本宽 ≈ 365.18 px**（< 换行宽 **410**）· **单行行高 ≈ 37.89 px**
            //       （≈ ascent − descent = 90 × 40/95）。
            //     · 自适应的框 = `SetAutoFitBox(wrap 410, 高 r.H = 64.8, min 25, max 40, base 36)`
            //       ⇒ 40 px 时**宽 365 < 410、块高 37.9 < 64.8** ⇒ **不会触发缩字号**：渲出来就是
            //       **fontSize 40 px、一行、块高 ≈37.9 px**（余量 ≈26.9 px）。
            //   ⚠️ **这不是「跑过渲染」**：真机/自检里仍可用 `Label.FontPxNow` + `DumpSizes()` 抽一次
            //      （判据口径同 `Editor/BattleScene.cs` §4.6c：`FontPxNow` 落在 [25,40] 且 ≈40）。
            //      静态这一遍的意义 = 把「估的字号」换成**可复算的推导**；字距是 0、字符串里无连字对。
            MenuDraw.Text(w, new PxRect(1309f, 594.57f, 1729f, 659.37f), "Maximum card tier reached",
                          Color.white, "No Upgrade Title", 40f, QCdText, 410f, 25f, 40f, 36f, face: TmpFont.Face.LatinCardBody);
            // 🔴 **2026-10-18 再改（铁律 5 · 用户当天拍板「一个按钮改为两个……可以点击这两个按钮实现卡框的
            //   降级和升级」）⇒ 这一对【不再互斥】**（上一个版本那两句 `content.SetActive(!maxed)` /
            //   `w.SetActive(maxed)` 已作废）：
            //
            //   ① `Content` **常显**（`active = true`）—— 满级（4 级）时「**降级**」还得点得到，
            //      把整组关掉就等于「满级之后再也回不去」= 用户要求的「降级」在最高档失效（功能缺口）。
            //   ② `No Upgrade Warning` 照旧**只在满级露**（用户口径：「按现在逻辑保留『满级提示』那一支」），
            //      但它的**文字位置挪到原来 `Explanation` 那一行**（`1309 .. 1729 × 594.57 .. 659.37`，
            //      判据 = `W5` 报告 §4.3 的 `Explanation` 行 rect）—— 那一行按用户要求**已经去掉了**
            //      （见上面「你将获得」那段），正好空出来；不挪的话它会**压在『降级』『升级』两颗钮上**
            //      （原 rect `1314,430.62 → 1724,644.92`，单行落在 y≈537，而两颗钮就在 `524.96 .. 585.31`）。
            //   ⚠️ 原版这两件互斥的口径**只在「只有一颗升级钮」时成立** —— 用户的改法（两颗钮）让
            //      「满级 ⇒ 只能看不能动」不再成立 ⇒ 这一处按**用户口径**覆盖原版，如实标注在报告里。
            content.gameObject.SetActive(true);
            w.gameObject.SetActive(maxed);
            // 🆕 **2026-10-18（用户拍板：三块面板之间要留一致的间距）**：整块下移一个 `PanelGap`。
            //   ⚠️ 必须排在**所有子件建完之后**（见 `ShiftPanelDown` 的注释）；⛔ 别改成「建的时候就把 rect 逐个 +gap」——
            //   那样要改十几处绝对 y，漏一处就是错位。
            ShiftPanelDown(p, UpgT, UpgB, PanelGap);
        }

        // ---- ③ 异画（Alternate Art）----

        /// <summary>这张卡**有没有异画风格** —— 等价于原版 `RawCardScript.HasAlternativeArtStyles(card)`，
        /// 也就是 `AlternateArtPanel.Initialize` 拿来决定**整块 `SetActive`** 的那个布尔。
        ///
        /// <para>🔴 **判据是【数据】，不是「图加载出来没有」**：原版那句查的是服务端配置
        /// （`RawCardScript__HasAlternativeArtStyles.c` → `CardsSpecialConfig.CanShowAACard`，
        /// LiveOps 配置，**本地取不到**）⇒ 用**同一批数据**的本地副本 =
        /// `CollectionWindow.AltArtCards`（本机 7 张督军异画，出处 = `工具/import_original_art.py` 的 `ALT_ART`）。
        /// ⛔ **别改成 `CardArt.AltArt(id) != null`** —— 那是「贴图加载成功没有」，
        /// 一旦 `Resources/Art/` 缺图就会把**所有**卡的这一块都关掉（把「没有原版美术」误当成「没有异画」）。</para></summary>
        public static bool HasAltArtStyle(string cardId)
        {
            return AltStylesOf(cardId).Count > 0;
        }

        /// <summary>🆕 **2026-10-12（A1699）**：这张卡的那几种异画风格（**按 `CollectionWindow.AltArtCards`
        /// 的次序**）。
        ///
        /// <para>🔴 **本表只有一份** —— `Shell/CollectionWindow.cs` 的 `AltArtCards`（7 张，
        /// 出处 = `工具/import_original_art.py` 的 `ALT_ART`）。**这里只是按卡 id 筛一遍，不是新写一张表**
        /// （本仓红线「两处写同一条规则 = 迟早不一致」；收藏窗第 4 页签 Styles 那套是**同一份数据**的另一个视图）。
        /// 🔴 **2026-10-14 就地订正（铁律 5 · 账 `A1808`/`A1830`）**：原文写「7 张 · **2 种风格**」—— **不成立**，
        /// 那 7 张**全是 `HammerAndBolter` 同一档**（判据 → `CollectionWindow.StyleLogoName` 头注）⇒ 本表
        /// 每张卡实际**最多 1 条**。</para>
        /// ⛔ **别在这里自己枚举 `Resources/Art/altarts/`，也别改成 `CardArt.AltArt(id) != null`** ——
        /// 后者是「贴图加载成功没有」，理由同 <see cref="HasAltArtStyle"/> 的注释。</para>
        /// <para>⚠️ 本地 7 张里**每张卡只有 1 种风格**（`AA_HB` ×6 / `v2` ×1）⇒ 实际总是 0 或 1 张；
        /// 但原版的 `currentStateIndex` 是**按张卡的风格表**走的（1 基）⇒ 这里照**列表**建模、不写死「1 种」，
        /// 🔴 **2026-10-14 就地订正（铁律 5 · 账 `A1808`/`A1830`）**：原来这里写「将来远端 CCD 那批 `alternateartstyles`
        /// 到手时不用改结构」—— **那批已经在手了**（两张风格 logo 已导进 `Resources/Art/ui_menu/`），
        /// 而且**本地 7 张卡的 `style` 全是 `HammerAndBolter` 同一档** ⇒ 张卡的风格表实际最多 1 条
        /// （判据 → `CollectionWindow.StyleLogoName` 头注）。列表建模**保留**（结构不用改，正是这句的用处）。</para></summary>
        public static List<CollectionWindow.AltArtCard> AltStylesOf(string cardId)
        {
            var list = new List<CollectionWindow.AltArtCard>();
            if (string.IsNullOrEmpty(cardId)) return list;
            foreach (var a in CollectionWindow.AltArtCards) if (a.CardId == cardId) list.Add(a);
            return list;
        }

        /// <summary>当前异画档要喂给卡面的那张图 —— **0 档（原画）返回 `null`**（= 不改 `artOverride`）。
        /// ⚠️ 取不到图时返回 `null` 并**出声**（红线：不许静默 —— 否则「切了没反应」没人知道为什么）。</summary>
        Texture2D AltArtTexture()
        {
            if (AltStyleIndex <= 0 || Card == null) return null;
            var tex = CardArt.AltArt(Card.Id);
            if (tex == null)
                Debug.LogWarning("[CardDetail] 异画立绘取不到：「" + Card.Id + "」的第 " + AltStyleIndex
                               + " 档（`CardArt.AltArt` 返回 null）⇒ **卡面退回普通立绘**（不静默）");
            return tex;
        }

        void BuildAltArt(Transform panel)
        {
            var p = PanelBox(panel, "Alternate Art Panel", AltT, AltB);
            // 这张卡的异画风格表（**只此一份**，见 `AltStylesOf`）；`has` = 原版那句
            // `RawCardScript.HasAlternativeArtStyles(card)` 的等价物（决定整块 `SetActive`）。
            var styles = AltStylesOf(Card != null ? Card.Id : null);
            var has = styles.Count > 0;
            AltStyleIndex = has ? Mathf.Clamp(AltStyleIndex, 0, styles.Count) : 0;   // 0..Count（原版 1 基那一段）
            // 🆕 **2026-10-14（账 `A1837`）**：「可购买原画」（= 原版 `AlternateArtPanel.TogglePurchaseBasicCard`
            //   的那个人布尔）—— 它**与上面那颗风格 logo 互斥**（判据/推导全文 → 本方法末尾
            //   `buyRoot.gameObject.SetActive(canBuyOriginal)` 那一段）。
            //   原版表达式 = `(style == 0) && !拥有那张的原画版 && 拥有一张它的异画`；
            //   本工程里**第二项恒假** ⇒ 结论恒 `false`（理由：`CardProgress.Owned` 是「给足」口径。
            //   ⚠️ 短路的顺序有意义：`has` 为假时 `Card` 可能是 null，**不能让第三个条件被求值**。
            bool canBuyOriginal = has && AltStyleIndex == 0
                                  && CardProgress.Owned(Card.Id, Card.Rarity) <= 0;   // 原版 `!Inv.HasItem(+0x58)`

            // ---- ① 左右两颗换画圆钮（原版 `Alternate Art Panel` 的 `Select Art Button Left/Right`）----
            // 判据链（`d:/2/tools/decomp_full/`，本笔逐字读过）：
            //   `CardDisplayOptions__Initialize.c:31`（第三块 = `+0x40 alternateArtPanel`）
            //   → `AlternateArtPanel__Start.c`（给两颗钮各挂一个 `NavigationButtonClick(bool isLeft)`）
            //   → `AlternateArtPanel__NavigationButtonClick.c:9`（先两颗都 `interactable = 0`、换完再开回来）
            //   → `AlternateArtPanel__SwapStyle.c:25`（`index ±1`，`:30/:36` 回绕）
            //   → `AlternateArtPanel__SetStyle.c:47-52`（`BasicCardUI.SetCardImage(前台那张, 该风格图)`）。
            // ⚠️ 这两颗钮**只在有异画时才建得出画面**（整块 `SetActive(has)` 那一句在末尾）——
            //    但节点**照建**（原版也是建好、整块关），这样自检两向都断得到。
            BuildAltArrow(p, true);
            BuildAltArrow(p, false);

            // ⚠️ 原版这一格的 `Title` 印的是**升级文案**（复制粘贴 bug）⇒ **我们不抄那个 bug**，
            //    照这一格自己的意思写（**出声**）。
            Title(p, "AltArt Title", has ? "Alternate art (owned)" : "Alternate art", 1307.5f, 690f, 1726f, 760f, 31.7f);
            // ---- ② `Current Style` 那一格 = **原版的图**（账 `A1837` · 2026-10-14 落地）----
            // 判据链（`d:/2/tools/decomp_full/`，逐行读过）：
            //   `AlternateArtPanel__SetStyle.c`：`uVar5 = AlternateArtStylesUtilities.GetStyleIcon(style)`
            //   → `UnityEngine_UI_Image__set_sprite(*(param_1 + 0x30), uVar5)`；而 `+0x30` 那颗 Image 的
            //   场景真身 = **`Current Style`**（`private Image styleImageContainer`，`m_Children` 空
            //   ⇒ 图长在它自己身上，**全树没有 TMP**）。
            //   ⇒ 喂哪张 = **那一档 `style` 的 logo**：**原画档（`style 0`）→ `WarpForgeLogo_Style` 徽标 ·
            //     异画档（`5`）→ `Cell_Shaded_Alternative_Art_Style`**（映射与判据 → `CollectionWindow.StyleLogoName` 头注）。
            //   `AlternateArtPanel__Initialize.c:82-95`：**看原画时传的就是 `0`**（`iVar7 = iVar6 = 0`）、
            //   看异画才传该异画自己的 style ⇒ 我们这一侧 `AltStyleIndex == 0` ⇔ 原画档 ⇒ `StyleDefault`。
            // ⚠️ **改前是文字**「风格名 (N/M)」，框 `1307.5,762→1726,800` 与文案**都是我们挑的**；
            //   **文字这一支现在只当兜底**（图载不到 ⇒ **出声** + 退回画它 —— 红线：不许静默失败）。
            string curStyle = AltStyleIndex == 0 ? CollectionWindow.StyleDefault
                                                 : styles[AltStyleIndex - 1].Style;
            var _styleTex = CollectionWindow.StyleLogo(curStyle);
            if (_styleTex != null)
            {
                // 🔴 **队列取 `QCdText`(3117)，⛔ 不是 `QCdRow`(3116)** —— 因为这一格**整块落在
                //   面板底板 `Background`（`PanelBox` 画的，**也是 `QCdRow`**）之内**：
                //   本仓**没有 uGUI** ⇒ 每个 quad 都躺在**同一个设计平面上（z=0）**，而透明队列是按
                //   「到相机的 3D 距离」排的 ⇒ **同队列 + 同平面 = 顺序不可靠**（这条踩过：铺满屏的图
                //   互相盖错、卡组编辑侧栏被底板盖死 ⇒ `CLAUDE.md` §三「分层要用渲染队列，不能用 z」）
                //   ⇒ 必须**严格大于**底板那一层。
                //   **同族先例（不是我自造的层级）**：收藏窗 Styles 页那颗同名的 `Art Style Logo`
                //   （`CollectionWindow.cs`）用的就是**它那边的文字层 `QPageText`**(3033)，
                //   而那页的行/底是 `QPageRow`(3032) —— 同一个梯子、同样的相对关系。
                //   ⚠️ 这一格与价签**互斥**（见下面 `canBuyOriginal` 那一段）⇒ 它还**不会**去压价签；
                //      几何上也没有别的件与它相交（`Title` 到 760 为止 · 两钮/锁到 761 为止）。
                MenuDraw.Rect(p, _styleTex,
                              new PxRect(AltStyleImgL, AltStyleImgT, AltStyleImgR, AltStyleImgB),
                              "Current Style", QCdText, null, keepAspect: false);   // 原版 = Simple · PA = 0
            }
            else
            {
                Debug.LogWarning("[CardDetail] 风格 logo 图载不到（style=" + curStyle
                                 + "）⇒ 退回画风格名 —— 查 `Resources/Art/ui_menu/` 与 `CollectionWindow.StyleLogoName`");
                string cur = !has ? "No alternate art"
                           : AltStyleIndex == 0 ? "Original art  (0/" + styles.Count + ")"
                           : CollectionWindow.StyleLabel(styles[AltStyleIndex - 1].Style)
                             + "  (" + AltStyleIndex + "/" + styles.Count + ")";
                MenuDraw.Text(p, new PxRect(1307.5f, 762f, 1726f, 800f), cur, Color.white, "Current Style", 28f, QCdText, face: TmpFont.Face.LatinCardBody);
            }
            // 🔴 **异画绑死在那张卡上**（`AlternateArtCard.GetIdForClonedCard` 对原卡 id 做一次 `String.Replace`）
            //    ⇒ 这张卡没有异画就是没有，不给别的督军换。本地只有 7 张督军异画。
            //
            // 🆕 **2026-10-14（账 `A1837`）**：这颗价签**整棵**收进一个同名根节点里 —— 原版
            //   `priceDisplayButton`（`AlternateArtPanel` 的 `+0x40`，PPtr **`2261`**）**就长在
            //   `Buy Original Card Button` 这个 GO 上**（那一包的 `GameObject/Buy Original Card Button.json`
            //   的 `m_Component = [1500(RT), 2261]`）⇒ 原版 `SetActive` 关的是**整棵**（含它的命中区）。
            float bw = 328.71f, bh = 73.38f;
            float bx = (PanL + PanR) * 0.5f - bw * 0.5f;             // 原版锚 (0,0) + `aPos.x = 225` ⇒ 屏 1519 为中心
            // 🔴 `by` **820 → 778.33**：`YI_恢复两块面板与异画钮.md` §七·4 记的那条偏离，**本笔销账**。
            //   判据 = 原版锚 `(0,0)`（面板左下）· `aPos (225, 88.0)` · `sd (328.7,73.4)` · `pivot (0.5,0.5)`
            //   ⇒ 顶边 = `AltB − 88.00 − bh/2` = `903.01 − 88 − 36.69` = **778.33**。四路同值：
            //   ① **运行时实况 dump** `资料/原版实拍/menu_forge_1016/runtime_ui_dump_menu_forge.tsv:160`
            //      （`Buy Original Card Button ▸ pos 225.0,88.0 ▸ rect 328.7,73.4`）；
            //   ② 树 dump `资料/说明书/04_界面UI/主菜单全树.md:163`（`[395,1318 329x73]` ⇒ 屏 **1355, 778.33**）；
            //   ③ 场景 JSON（`RectTransform_1500`）复算；④ §七·4 自己那次实读。
            //   ⚠️ `bx` 原式是 `PanL + (PanR−PanL−bw)*0.5`（与上式差 **1.3e-3 px**）—— 本笔一并写成**中心式**，
            //      与原版那条锚/algo 同形（`(PanL+PanR)/2 − bw/2`）。
            //   🔴 **为什么现在可以改了（旧理由为什么失效）**：那次不动它的理由是
            //      「动它会与上面那行 `Current Style` **文字**（762..800）撞上 ⇒ 把一个偏离换成一个更差的重叠」——
            //      而**那一行字本笔已经删掉**（换成原版**原位**的图 `764.26..865.77`），并且
            //      **原位本来就是「图与价签同中心、互斥两态」**（两者中心逐位都是 `(1519, 815.017)`，
            //      判据见下面 `canBuyOriginal` 那一段）⇒ 旧理由指的那一对东西已经不存在了。
            float by = AltB - 88.0f - bh * 0.5f;                     // = 778.33
            var buyRoot = MenuDraw.Node(p, "Buy Original Card Button", new PxRect(bx, by, bx + bw, by + bh));
            var buyBg = MenuDraw.Rect(buyRoot, CardArt.MenuUi("UI_Button_Mulligan"), new PxRect(bx, by, bx + bw, by + bh),
                          "Buy Bg", QCdRow);
            // 🔴 **2026-10-09（A1126 · A2 档）**：这一处原来**没传 autosize 实参** ⇒ 固定 32px。
            //   判据 = 原版 `Alternate Art Panel/Buy Original Card Button/Generic UI Button/Price Display/text`
            //   （= 画面上**可见**的那一颗；那颗钮底下另一颗 `Button Text` 是 `INACT` 空串）：
            //   **`m_enableAutoSizing = 1`** · `m_fontSizeMin = **12**` · `m_fontSizeMax = **54**` ·
            //   `m_fontSizeBase = **39**` · **折行 = 0**。
            //   ⚠️ 我们这一格的框 `328.71 × 73.38` 与原版那颗钮**逐位相同** ⇒ 就是它。
            //   ⚠️ 本窗刻度 = 原版刻度 ⇒ 四格照抄；⚠️ 折行 0 ⇒ 紧跟一句 `SetWrapping(false)`。
            var buyLb = MenuDraw.Text(buyRoot, new PxRect(bx, by, bx + bw, by + bh), has ? "Alternate art" : "No alternate art",
                          Color.white, "Buy Original Card", 32f, QCdText, bw, 12f, 54f, 39f, face: TmpFont.Face.LatinCardBody);
            if (buyLb != null) buyLb.SetWrapping(false);            // 原版折行=0
            // 🔴 **2026-10-17（账 `A1704`）显隐照原版**：`AlternateArtPanel__SetStyle.c` 的末段是
            //    `UnityEngine.GameObject.SetActive(*(param_1 + 0x38) /* lockIcon */, !HasItem(当前那张))`
            //    —— **全解锁 ⇒ 锁恒关**。本工程「拥有」只有 `CardProgress.Owned` 这一处（给足口径，
            //    任何 id 都 ≥ 1，推导全文见下面 `canBuyOriginal` 那一段）⇒ 这里恒判 false。
            //    **节点保留**（原版也是建好再 `SetActive`，自检两向都断得到），⛔ 别删节点。
            //    ⚠️ 真要露的那一支（未拥有）原版 rect = `Lock Background 65.8×124.9
            //      @1716.6,722.06→1782.4,846.98` + `Lock icon 28.1×42.2`；我们这颗仍是
            //      `50×50 @1310,690`（几何未核）—— 只影响「未拥有」那一支，本轮**不动几何**
            //      （判据 → `资料/普查产出_第二十三会话/YI_恢复两块面板与异画钮.md` §八·1）。
            var lockIcon = MenuDraw.Rect(p, CardArt.MenuUi("WF_Lock_Icon_Simple"),
                                         new PxRect(1310f, 690f, 1360f, 740f), "Lock Icon", QCdRow, null, true);
            bool lockOwnerHasItem = Card != null && CardProgress.Owned(Card.Id, Card.Rarity) > 0;
            if (lockIcon != null) lockIcon.gameObject.SetActive(!lockOwnerHasItem);   // 本作恒 false

            // 🆕 **2026-10-12（A1699）：这颗价签**原来**一个命中区都没有**（`Buy Bg` 只是画了一张图）
            //   ⇒ 画面上立着一颗「看起来能点、点下去什么都不发生」的钮（本仓红线：不许静默失败）。
            //   原版它**有** onClick（`AlternateArtPanel.Start.c:39-42` 把 `PriceDisplayButton.OnClick`
            //   接到 `BuyOriginalCard`），而那条链**本地走不通**：`BuyOriginalCard.c` 是
            //   `WindowsManager.ShowPopUp(<确认弹窗>)` + `PlayfabWrapper.GenericCloudScriptHandler(0x385, …)`
            //   —— **服务端 CloudScript**，而且**价格 / 余额 / 持有物全在远端**（正本 §六·7 那几条老账）。
            //   ⇒ 按本笔的口径：**接上命中区 + 出声**（⛔ 不留哑钮，也不假装买成功）。
            // ⚠️ **2026-10-14（`A1837`）**：那两句「不留哑钮」的前提是**它会被画出来** —— 而按下面那段
            //   互斥判据，本工程**恒走「价签关」那一支** ⇒ 这颗命中区与 `Buy Bg` / 那行字**一起关着**。
            //   本笔**没有删掉**这一段（判据/出声都留着，`p` 换成 `buyRoot`），理由同 `A856` 那条：
            //   原版也是**建好、`SetActive` 关**，节点留在树上（自检两向都断得到）。
            Hit(buyRoot, "BuyHit", new PxRect(bx, by, bx + bw, by + bh),
                () => { OnBuyOriginalCardClicked(); }, QCdHit, buyBg, "UI_Button_Mulligan");

            // 🔴 **2026-10-14（账 `A1837` · 简报 §四·1 的「互斥」）：「风格 logo」与「这颗价签」永远不同时出现。**
            //   判据 = `d:/2/tools/decomp_full/AlternateArtPanel__TogglePurchaseBasicCard.c`（逐行读过）：
            //     `UnityEngine_Behaviour__set_enabled(*(param_1 + 0x30) /* styleImageContainer */, param_2 ^ 1)`
            //     `... + 0x40 /* priceDisplayButton */ 那个 GO：SetActive(param_2)`
            //   —— `param_2` = 「可购买」⇒ **可购买 ⇒ logo `enabled=false`、价签开**；**不可购买 ⇒ 反过来**。
            //   ⚠️ 那一支里还有第二道闸：`price != 0` 才算真能买（`ItemsConfig.GetOriginalArtPrice(rarity)`，
            //      **远端 LiveOps 配置**）；`price == 0` ⇒ 价签关 + **logo 又开回来**。
            //   🔴 **「可购买」在本工程恒 `false`（按代码里的真实判据推，不是拍脑袋）** ——
            //      `AlternateArtPanel__SetStyle.c` 末尾那段（逐行读过）是：
            //        `(style == 0 /*原画档*/) && !Inv.HasItem(那张卡的**原画版** /*+0x58*/)
            //         && Inv.Any(该卡的异画表, x => Inv.HasItem(x))`
            //      · 第一项在 `AltStyleIndex == 0`（= 原画档）时**成立**；
            //      · 🔴 **第二项本工程恒假** —— 本窗「拥有」的判据**只有 `CardProgress.Owned` 这一处**
            //        （同族使用者可互证：`Shell/DeckInfoPopup.cs` 的 `CheckOwnership`、
            //        `Shell/CollectionWindow.cs`、`Shell/BoosterPackOpenWindow.cs`），而它是
            //        **「给足」口径**：`DeckCap(rarity) + (NeedCopies[MaxLevel] − NeedCopies[1])` **任何 id 都 ≥ 1**
            //        （用户 2026-09-22 裁决「全解锁 / 拥有数给足」）⇒ `!HasItem(那张)` **恒假**；
            //      · ⇒ 第三项**够不着**，本版也**没有异画拥有表**可读（如实记，⛔ 不编一个）。
            //   ⇒ 照原版这一支：**logo 开（上面已画）· 价签整棵 `SetActive(false)`**。
            //   ⚠️ **另一支（可购买 ⇒ 价签开、logo 关）本版永远走不到** ⇒ 那条链上的
            //      `GetOriginalArtPrice` / `PriceDisplayButton.Setup` **无从验**（⛔ 不假装买成功）。
            //   ⚠️ 顺带：`+0x38`（`lockIcon`）的判据是同一次调用里的 `SetActive(!HasItem(当前那张))` ——
            //      **同一条「恒拥有」⇒ 原版在我们这种全解锁态下锁恒关**。
            //      ✅ **2026-10-17（`A1704`）本笔已落地**：上面那颗 `Lock Icon` 节点照建、**恒 `SetActive(false)`**
            //      （判据与理由写在那一段注释里）。仍**未动**的只有它的**几何**（`50×50 @1310,690` ↔
            //      原版 `Lock Background` 1716.6,722.06→1782.4,846.98）—— 只影响「未拥有」那一支。
            buyRoot.gameObject.SetActive(canBuyOriginal);

            // 🔴 **2026-10-17（D17 = 账上 A856）：「没有异画 ⇒ 整块关」** —— 判据 = 第一权威（反编译方法体）
            //    `d:/2/tools/decomp_full/AlternateArtPanel__Initialize.c` **:22-38**（现读）：
            //      · 卡拿不到 / `RawCardScript.HasAlternativeArtStyles(card) == false`（`cVar4 == '\0'`）
            //        ⇒ 填两个 0（`+0x48` / `+0x58`）之后 **`UnityEngine.GameObject.SetActive(gameObject, 0)`** ——
            //        **整块关**（不是「画成 0 张」）；
            //      · 有异画风格那一支才 `SetActive(…, 1)`（`:35-38`），再按 `IsAlternateArt` 分「原画 / 异画」两支取值。
            //    ⚠️ **注意不是「不建那几件」**：原版是**建好、整块关**（`SetActive(false)` 的节点留在树上）——
            //      我们照这个来（`Build()` 里子件照建，最后关根节点），自检才能两向都断（见 `CollectionScene.Run`）。
            //    ⚠️ 改前我们**不管有没有异画都画那一块**（画的是 `Alternate art / 0 of 0 / No alternate art`）——
            //      那几句「没有异画」的文案**现在都是死支**（面板被关掉，谁也看不见），**留着**是因为
            //      `has` 只有两个取值、不值得为它拆一遍版面。
            p.gameObject.SetActive(has);
            // 🆕 **2026-10-18（用户拍板：有异画的战将那里三块方框的间距也要一致）**：
            //   第三块（异画）下移 **两个** `PanelGap` ⇒ 三块两两同距（与上面 `BuildUpgrade` 那一处成对）。
            //   ⚠️ 排在 `SetActive` 之后：显隐与几何两件事互不影响，但**几何必须与是否显示无关**
            //   （否则「只有两张卡有异画」时那一次的位姿会飘）。
            ShiftPanelDown(p, AltT, AltB, PanelGap * 2f);
        }

        /// <summary>异画面板上一颗换画圆钮（`left` = 左钮）。三层**各占一个递增的渲染队列**（见 `QCdIcon`）。
        /// 左钮的箭头**镜像**（原版 `UIFlippable(m_Horizontal=1)`，同 `CollectionWindow.BuildStyleArrow`）。
        /// 🔴 **接线按位置**：左 = 上一个(−1)、右 = 下一个(+1) —— 与收藏窗 Styles 页那份**同一条口径**
        /// （`Shell/CollectionWindow.cs` 的 `leftStyleButton` 那条注释：脚本字段名 `leftStyleButton` 挂的其实是**右边**那颗
        /// ⇒ 那里也是按位置接的）。</summary>
        void BuildAltArrow(Transform p, bool left)
        {
            string side = left ? "Left" : "Right";
            float cx = left ? AltBtnCxL : AltBtnCxR;
            var r = new PxRect(cx - AltBtnW * 0.5f, AltBtnCy - AltBtnH * 0.5f,
                               cx + AltBtnW * 0.5f, AltBtnCy + AltBtnH * 0.5f);
            // 圆底盘画在**节点自己**身上（原版实读：`Select Art Button Left` 自己带
            //   `Image[UI_Button_Round_background]`、Simple + `preserveAspect`；同 `CollectionWindow.BuildStyleArrow`
            //   引 A1149 那条「它不是子件 `Background Round`」）。
            var round = MenuDraw.Rect(p, CardArt.MenuUi("UI_Button_Round_background"), r,
                                      "Select Art Button " + side, QCdRow, null, true);
            var node = round != null ? round.transform : MenuDraw.Node(p, "Select Art Button " + side, r);
            float ix = cx - AltArrowW * 0.5f, iy = AltBtnCy - AltArrowH * 0.5f;
            var ir = new PxRect(ix, iy, ix + AltArrowW, iy + AltArrowH);
            // 子件两层**必须逐级高一层队列** —— 三层同心（74.39² 圆底 / 56.86×58.13 黄底 / 同框箭头），
            //   同队列时「谁盖谁」由到相机的距离决定 ⇒ 黄底或箭头会凭空消失（同族实拍先例：
            //   `CollectionWindow.BuildStyleArrow` 那条「两颗钮里只看得见黄底、箭头没出现，而矩形断言全绿」）。
            var face = MenuDraw.Rect(node, CardArt.MenuUi("40k_general_bt_yellow"), ir, "Background", QCdText, null, true);
            var arrow = MenuDraw.Rect(node, CardArt.MenuUi("40k_general_bt_arrow"), ir, "Icon", QCdIcon, null, true);
            // 左钮**镜像**（原版 `UIFlippable(m_Horizontal=1)`）—— uv 的 u 从 1 到 0
            if (arrow != null && left) arrow.SetUvRect(new Rect(1f, 0f, -1f, 1f));
            // 命中区：① 名字**带左右**（两颗各一个，同名只会拿到第一颗）；② **先建、且不看上面建没建出来**
            //   —— 「这一格点不点得到」与「这一格的图画不画得出来」是两件事（同 `BuildCardHit` 那条 A157）。
            // 悬停/按下换图：原版那颗 `EverguildButton` 的 `m_Transition = 2 (SpriteSwap)`、
            //   `m_TargetGraphic` = 子件 `Background`（`40k_general_bt_yellow`）⇒ 把黄底那层当 `target`，
            //   图名走 `40k_general_bt_yellow`（高亮图 `_hover` / 按下图 `_pressed` 本地都在，
            //   出处 = 同 `CollectionWindow` 那两颗圆钮的接法）。取不到时 `WindowButton` 会记进
            //   `MissingSwapArt` / `MissingPressedArt`（自检会红），**不静默画成没反应**。
            Hit(node, "ArrowHit " + side, r,
                () => { ChangeAltStyle(left ? -1 : +1); }, QCdHit, face != null ? face : round,
                "40k_general_bt_yellow", clickCue: WindowSounds.DefaultCloseCue);
        }

        // ---- 计数条（`Card Counter`，原版 §三）----
        void BuildCounter(Transform panel)
        {
            var c = MenuDraw.Node(panel, "Card Counter", new PxRect(825.07f, 829.81f, 1094.93f, 909.19f));
            // 🔴 **2026-10-13（A691）**：`spares > 0` / `== 0` 在原版是**两棵不同的树**（不是同一棵改个名）——
            //   判据 = `decomp_full/CardCounterDisplay__Initialize.c`：`spares > 0` ⇒
            //   `singleCounterContent.SetActive(0)` + `duplicateCounterContent.SetActive(1)`（0x38 / 0x20）；
            //   `spares == 0` ⇒ **反过来**，并且**只填 `singleCardCounter`**（0x40，`SetSingleCounter` 同一个写法）。
            //   ⇒ 框、字号、auto 区间、结构**各是各的**（见 `CcSg*` 那一族常量）。
            bool dup = CardProgress.Spares(Card.Id, Card.Rarity) > 0;
            var rBox = dup ? new PxRect(CcBgL, CcBgT, CcBgR, CcBgB)      // `Duplicate Counter` 240×70.586
                           : new PxRect(CcSgL, CcSgT, CcSgR, CcSgB);     // `Single Counter`    170×50
            var box = MenuDraw.Node(c, dup ? "Duplicate Counter" : "Single Counter", rBox);
            MenuDraw.Rect(box, CardArt.MenuUi("40K_main_deck_card_counter"), rBox, "Background", QCdRow, null, true);
            // 🔴 **2026-10-17（A894）**：喂**带卡型那一档** —— 督军（`Type == "hero"`）的原版上限是
            //   **恒定 1**（`GetMaxCopiesInDeck` 那句在稀有度判断之前的 `return 1`，判据见 `CardProgress.DeckCap`）。
            //   ⚠️ **残留（如实标注）**：本段下面那个 `owned` 走 `CardProgress.Owned`，而它的「给足」公式
            //   （= 卡组上限 + 9）吃的是**不带卡型**那一档（`_owned` 缓存按 id 存、`AddOwned` 那一路也没有卡型）
            //   ⇒ **对 28 张非传说督军，这颗印的「多余副本」会比「按督军档」多 1**（9 变 10）。
            //   本颗**印进画面**的那个数（`x{inDeck}` = `min(拥有, 上限)`）**是对的**。
            int cap = CardProgress.DeckCap(Card.Rarity, Card.Type);
            int owned = CardProgress.Owned(Card.Id, Card.Rarity);
            int inDeck = Mathf.Min(owned, cap);
            int spares = owned - inDeck;
            // 🔴 **2026-10-13（A404）**：原来这三处 `wrapPx` 传的是 `0f` —— 而 `MenuDraw.Text` 的自适应那一段
            //   **整段写在 `if (wrapPx > 0f)` 里面**（`Shell/MenuDraw.cs` 的 `Text(...)`）⇒ 只传 `autoMinPx`
            //   等于传了**死实参**：这三颗**从来没开过自适应**（字段有、画面没有 —— 静默失败）。
            //   补上 `wrapPx` = **原版那一颗的 `m_SizeDelta.x`**（`73.283` / `28.02` / `44.09`，
            //   判据 → `资料/阶段二_卡片详情窗_原版规格.md:90-92`；原版三颗都带 `auto`）。
            //   ⚠️ 传 `wrapPx` 会**顺带把折行模式开成 `Normal(1)`**（`SetWrapWidth` 无条件写，见
            //   `TmpFont.SetWrapWidthRect`），而**原版这三颗都是 `折行=0`（`NoWrap`）**
            //   （逐颗实读：MB pid `1892` / `1848` / `1823`，`bundle_scenes_scenes_mainmenuwarpforge`，
            //   `m_TextWrappingMode = 0`）⇒ 紧接着 `SetWrapping(false)` 还原自己那一档
            //   （成对写法同 A205 / A34-F4 那一族；`SetWrapping` 顺带把版面推下去）。
            // 🔴 **2026-10-13（A574）**：三颗都补上第 10 参 `autoBasePx` = 原版那一颗的 **`m_fontSizeBase`**
            //   （36 / 36 / 35，逐颗现读 MB pid `1892` / `1848` / `1823`，见 `CcBase*` 那几个常量）。
            // 🔴 **2026-10-13（A575）**：三颗的**水平对齐档**照原版逐颗接 —— 原版 `m_HorizontalAlignment`
            //   = **4 / 2 / 1**（TMP 枚举 `Left=1, Center=2, Right=4, Justified=8, Flush=0x10`、
            //   `Geometry=0x20`，判据 = `Runtime/TMP/TMP_Text.cs:74-77`）⇒ **第 1 颗 `Right` · 第 2 颗 `Center` ·
            //   第 3 颗 `Left`**。而我们三颗一律是 `TmpFont.NewText` 的**出厂档 `Center`**
            //   （`MenuDraw.Text` 全程不设 `alignment`）⇒ 第 1、3 颗**在格内的落位**与原版不同（半格量级）。
            //   做法照本仓惯例走 **`MenuDraw.AlignLeft/AlignRight`（挪整块）**，⛔ **不是**去改 TMP 的
            //   `alignment` —— `Label.AlignLeftOn` 的注释写着：改 `alignment` **管不了折行之后每一行在块内怎么排**。
            //   ⚠️ **顺序不能反**：`SetWrapping` 会把版面推下去（`Battle/Label.cs` 的 `SetWrapping` 头那条「要在对齐/量宽之前调」「要在对齐/量宽之前调」）
            //   ⇒ **先 `SetWrapping(false)`、后 `Align*`**。
            //   ⚠️ 第 2 颗（`Slash`，原版 `Center`）**不调** `Align*` —— 调了反而偏（铁律 5·c：别一个档顶三颗）。
            //   ✅ **2026-10-13（A691）已另建**：上面这两件（base / 对齐）取的是原版 **`Duplicate Counter`**
            //      那一棵树的三个节点（pid 1892/1848/1823）—— 而 `spares == 0` 时原版走的是**另一棵树**
            //      `Card Counter/Single Counter`（本笔现读原版 prefab：框 **875,844.50→1045,894.50** ·
            //      `Counter` **fs35** · auto[**25~35**] · base **36** · `H=2(Center)`）⇒ **本笔照原版另建**
            //      （见下面 `if (!dup)` 那一支 + `CcSg*` 常量族）。
            //      原来我们只把 `Duplicate Counter` **改了个名**、几何/字号/结构全部沿用（= 两棵树当成一棵），
            //      那是 WM2 §七·1 记下的偏离，**本笔销账**。
            var rC = new PxRect(CcX1L, CcY1, CcX1R, CcY2);
            if (!dup)
            {
                // `Single Counter/Counter`（原版 MB pid `1893` · GO `Counter_227` · RT `1151`）：
                //   fs **35** · auto[**25~35**] · base **36** · `m_HorizontalAlignment = 2 (Center)` = 出厂档
                //   ⇒ **不调 `Align*`**（⛔ 别把下面那颗 `Counter` 的 `AlignRight` 抄过来 —— 铁律 5·c）；
                //   容器宽 = **170**（那颗 RT 是 stretch ⇒ 容器 = 解析后的父宽，见 `CcSgWrapX`）。
                //   文案 = **`x{0}` 填 `min(拥有, 卡组上限)`**：`Initialize.c` 这一支走的是 `System_String__Format(DAT_18425ce10, iVar2)`
                //   —— **和 `spares > 0` 支填 `cardCounter`（0x28）用的是同一个格式串符号**；
                //   该字面量 = `stringliteral.json` 里 RVA `0x425CE10` 的 `'x{0}'`（`0x18425ce10 − ImageBase 0x180000000`）。
                //   ⚠️ 多余副本数那个 `{0}`（RVA `0x4265D10`）**只喂 `duplicateCardCounter`**，这一支不印它。
                // ⚠️ 这一支**没有** `Slash` / `Duplicates text` / `Duplicate image` —— 原版那三件只长在
                //   `Duplicate Counter` 那棵树上（`Initialize.c` 只 `SetActive` 两支的根，不会给这一支补件）。
                // ⚠️ **如实标注（`资料/普查产出_1013/WM3_SingleCounter.md` §七·1）**：原版这颗 MB `1893`
                //   还有 **`m_VerticalAlignment = 1024 (Bottom)`**（对照：`Duplicate Counter` 那三颗都是 `512 (Middle)`）。
                //   ✅ **2026-10-16（A712 阶段 2）销账**：那条「要真做需要在 `Battle/Label.cs` 加一个 `AlignBottomOn`、
                //   白名单里没有那个文件 ⇒ 只报不改」**已过期** —— A712 阶段 1 把**垂直档那个口**
                //   （`Label.SetVAlign(档, 框高)`，`AlignBottomOn` 只是当时设想的名字）落进了 `Battle/Label.cs`
                //   ⇒ 本处现在**真落**（见下面那句调用；框高 = `Single Counter` 170×50 的 50）。
                var lbSg = MenuDraw.Text(box, rBox, "x" + inDeck, Color.white, "Counter",
                              CcSgPx, QCdText, CcSgWrapX, CcSgMin, CcSgMax, CcSgBase, face: TmpFont.Face.LatinCardBody);
                // 成对写法同上（传 `wrapPx` 顺带把折行模式开成 `Normal(1)`，而原版这颗 `m_TextWrappingMode = 0`）。
                if (lbSg != null) lbSg.SetWrapping(false);
                // 🆕 **2026-10-16（A712 阶段 2）**：纵向档 = 原版这颗 `Single Counter/Counter` 的
                //   **`m_VerticalAlignment = 1024 (Bottom)`**（判据 = 上面那条如实标注里的 MB `1893` 直读；
                //   原版 prefab 框 **875,844.50→1045,894.50** ⇒ 170×50）。`Bottom` 吃框高 ⇒ 传 `rBox`。
                //   文案恒 `"x"+在卡组里的张数`（单行，且上面刚 `SetWrapping(false)`）⇒ 不踩多行那个坑。
                MenuDraw.SetVAlign(lbSg, Label.VAlign.Bottom, rBox);
                return;
            }
            var lbC = MenuDraw.Text(box, rC, "x" + inDeck, Color.white, "Counter",
                          CcX1Px, QCdText, CcWrapX1, 25f, 0f, CcBaseX1, face: TmpFont.Face.LatinCardBody);
            if (lbC != null)
            {
                lbC.SetWrapping(false);
                MenuDraw.AlignRight(lbC, rC);        // 原版 `…/Counters/Counter` 的 `align = 4 (Right)`
            }
            {   // ⟵ 走到这里就是 `dup == true`（`!dup` 那一支上面已 `return`）；这个花括号只是收 `Slash`/`Duplicates text`/
                //    `Duplicate image` 三个 `var` 的作用域，**不是** `if` 体（A691 把原来的 `if (dup)` 换成早返回，
                //    免得把那一大段整块缩进一级、把 A574/A575 的 diff 冲掉）。
                var rSl = new PxRect(CcSlL, CcY1, CcSlR, CcY2);
                var lbSl = MenuDraw.Text(box, rSl, "/ ", Color.white, "Slash", CcSlPx, QCdText,
                              CcWrapSl, 25f, 0f, CcBaseSl, face: TmpFont.Face.LatinCardBody);
                if (lbSl != null) lbSl.SetWrapping(false);   // 原版 `align = 2 (Center)` = 出厂档 ⇒ 不调 `Align*`
                var rX2 = new PxRect(CcX2L, CcX2T, CcX2R, CcX2B);
                var lbX2 = MenuDraw.Text(box, rX2, spares.ToString(), Color.white,
                              "Duplicates text", CcX1Px, QCdText, CcWrapX2, 18f, 0f, CcBaseX2, face: TmpFont.Face.LatinCardBody);
                if (lbX2 != null)
                {
                    lbX2.SetWrapping(false);
                    MenuDraw.AlignLeft(lbX2, rX2);   // 原版 `…/Slash/Duplicates text` 的 `align = 1 (Left)`
                }
                MenuDraw.Rect(box, CardArt.MenuUi("40k_general_icon_card_amount"),
                              new PxRect(CcImL, CcImT, CcImR, CcImB), "Duplicate image", QCdRow, null, true);
            }
        }

        // ---- 通配符条（`Wildcard Segment` + `WIldcard Counter`）----
        void BuildWildcards(Transform panel)
        {
            // 🔴 **2026-10-18（用户报「万能卡图标消失 / 数字外方框形制不对」· 写手 W12 查实）**：
            //   原来这条底板走 `Rect`（`Image.Type = Simple` 拉伸、**无 tint = 白**）⇒ 画成一条**亮白弧**；
            //   原版是 **Sliced 九宫 `m_Border 15,15,15,15`** + **`m_Color (0.462,0.462,0.462,1)` 灰**
            //   （判据 = `WIldcard Display/Background` 的 prefab 值；同族已按这条改过的两处 =
            //    `Deck/DeckRuntime.cs`（`N57`）与 `Shell/CollectionWindow.cs`，⛔ 三处口径必须同一份）。
            //   贴图 44×39（= 九宫 UV 的源尺寸），框 320×44。
            //   🔴 **同时修的一处真缺陷**：底板与那 4 颗图标原来**同为 `QCdRow`(3116) + 同一平面**
            //   ⇒ 「谁盖谁」不可控（本仓既有坑）—— 实际表现就是**图标被底板盖掉**、而数字在
            //   `QCdText`(3117) 高于底板所以活着。⇒ 底板留在 `QCdRow`、**图标抬到 `QCdText`**（下一处）。
            MenuDraw.Nine(panel, CardArt.MenuUi("40k_topmarquee_currency_display_BW"),
                          new PxRect(WcBgL, WcBgT, WcBgR, WcBgB), new Vector4(15f, 15f, 15f, 15f),
                          44f, 39f, QCdRow, new Color(0.462f, 0.462f, 0.462f, 1f), true, "Wildcard Bg");
            for (int i = 0; i < WildcardIcons.Length; i++)
            {
                float x = WcX0 + WcStep * i;
                // 🔴 队列 `QCdRow` → **`QCdText`(3117)**：必须**严格高于**底板那条（3116），否则又会被它盖掉。
                //   ⚠️ 与数字同一档（3117）**不冲突**：几何上不重叠 —— 图标占 `x .. x+30`，
                //      数字框从 `x+30` 起（`WcCountWrapPx = 41` 宽，下一格在 `x+75`）⇒ 两者**边界相接、零重叠**。
                MenuDraw.Rect(panel, CardArt.MenuUi(WildcardIcons[i]), new PxRect(x, WcBgT, x + 30f, WcBgB),
                              "Wildcard Icon " + i, QCdText, null, true);
                // 🔴 **2026-09-27 补 `keepAspect`（PA 普查抓的）**：原版 `WIldcard Counter/Counters/*/Icon`
                //   4 件全是 PA=1 + Simple（RT1211/1354/1344/1583），贴图 **42×51 / 41×51** 塞进 30×44
                //   ⇒ 原版实绘 **30×36.4（37.3）**，我们拉伸成 30×**44** ⇒ **高 ×1.18~1.21**。
                //   ⚠️ 同一件在收藏窗（`Shell/CollectionWindow.cs` 里那处 `keepAspect` 补）与卡组编辑（`Deck/DeckRuntime.cs` 里那处 `keepAspect` 补）也是同一错，三处一起修。
                // 🔴 **2026-10-18 改判（铁律 5）**：这里原来写死 `DeckRuntime.WcCountText`（= `"99"` 常量）。
                //   **那条口径作废** —— 用户当天拍板「照参考图布置两块面板」⇒ 创建副本会**真扣**通配符
                //   （`DoCraft` → `TrySpendWildcard`），数字还钉在 99 就是**画的和用的对不上**。
                //   ⇒ 本窗 4 格改读**真库存** `CardProgress.Wildcards(rarity)`（落盘，见「存档」那一段）。
                //   ⚠️ **本窗之外还欠两处**：`Shell/CollectionWindow.cs` 与 `Deck/DeckRuntime.cs` 里
                //   那两条计数仍读 `DeckRuntime.WcCountText`（同一个 `"99"` 常量；**不在本笔白名单**）
                //   ⇒ 扣过之后那两处会与本窗**不一致**，已报回调度台。
                //   原版语义：那 4 个数是 `WildcardDisplay` 的库存直出、跟着**指针悬停那张卡**的阵营走。
                MenuDraw.Text(panel, new PxRect(x + 30f, WcBgT, x + 30f + DeckRuntime.WcCountWrapPx, WcBgB),
                              CardProgress.Wildcards(WildcardRarities[i]).ToString(), Color.white,
                              "Wildcard Count " + i, WcPx, QCdText,
                              // 🔴 **2026-10-17（`A1229`）**：这一处原来**没传四格**（另两个宿主传了）——
                              //   现按同一份共用常量补上。判据 = 原版
                              //   `WIldcard Counter/Counters/{Common,Rare,Epic,Legendary}/Counter`
                              //   **四颗逐值相同**：`m_fontSize 32.6` · `auto[10~38]` · `base 45` · 折行 1 · 框 41×44
                              //   （出处 → `R5_包装层菜单族.md` §2 #1 · `R6` §2·F #38）。
                              DeckRuntime.WcCountWrapPx, DeckRuntime.WcCountAutoMinPx,
                              DeckRuntime.WcCountAutoMaxPx, DeckRuntime.WcCountAutoBasePx, face: TmpFont.Face.LatinCardBody);
            }
            var fac = DeckRuntime.FactionIcon(Card.Faction);
            if (!string.IsNullOrEmpty(fac))
                MenuDraw.Rect(panel, CardArt.MenuUi(fac), new PxRect(1470f, 71f, 1550f, 156f),
                              "Army Icon", QCdRow, null, true);
        }

        // ============================================================ 行为

        /// <summary>点遮罩以外的东西不该关窗 —— 这条给自检/上层用。</summary>
        public void ToggleLore()
        {
            LoreVisible = !LoreVisible;
            RefreshLore();
            Debug.Log("[CardDetail] `Show Card Text` ⇒ 风味文本条 " + (LoreVisible ? "显示" : "藏起")
                    + (string.IsNullOrEmpty(_loreText) ? "（⚠️ 这张卡**没有官方 Lore** ⇒ 底板本来就不亮）" : ""));
        }
        /// <summary>风味条（`LowerSection/FlavourTextBG` + `LoreText`）**现在**露着没有。
        /// 🔴 两个条件**缺一不可**：① 眼睛钮的开关（`LoreVisible`，原版 `Show Card Text`）·
        /// ② **这张卡真的有官方 Lore**（原版 `SetCardLore.c:24-27`：`SetActive(loreObjectBG, !IsNullOrEmpty(flavourText))`）。
        /// ② 由 <see cref="BuildLoreText"/> 写进 `_loreText`（没有就置 null 并把 bg 关掉）。</summary>
        void RefreshLore()
        {
            var bg = Find(transform, "FlavourTextBG");
            if (bg != null) bg.gameObject.SetActive(LoreVisible && !string.IsNullOrEmpty(_loreText));
        }

        /// <summary>下缘那两颗钮**按卡决定建不建**（原版 `ShowCard` 与 `CardSwapFinished` 里那两条
        /// `SetActive`）。判据只此一份 → `CardButtons`（那里写了原版出处与我们的等价物：
        /// **语音钮** = 这张卡有没有声音；**眼睛钮** = `type` 是 `unit`/`hero`）。
        /// ⚠️ 换位收尾要跟着换 —— 认的是**新前台**那张（原版 `CardSwapFinished` 同款）。</summary>
        void RefreshButtons()
        {
            if (_voiceBox != null) _voiceBox.gameObject.SetActive(HasCard && CardButtons.HasVoice(Card));
            if (_eyeBox != null) _eyeBox.gameObject.SetActive(HasCard && CardButtons.HasTextButton(Card));
        }

        /// <summary>🆕 **2026-10-12（A1699）切异画档** —— 原版 `AlternateArtPanel.NavigationButtonClick(bool isLeft)`
        /// → `SwapStyle(bool isLeft)`（判据全文 → `AltStyleIndex` 那一节的注释）。
        ///
        /// <para>口径逐条照原版：**① 只在「有异画风格」时动**（没有时那两颗钮的原版实现是整块关着的）；
        /// **② `index ± 1`**（`dir`）+ **③ 在 `0..Count` 上回绕**（0 = 原画，1..Count = 该卡的第 N 种异画）；
        /// **④ 换完重画** —— 原版是 `SetStyle` 里 `BasicCardUI.SetCardImage(前台那张, 该风格图)`，
        /// 我们这一侧等价物 = 重跑 `Build()`（`CardView` 的立绘层在 `Create` 那一刻按 `artOverride` 建，
        /// 见 `BuildCardStack` 里那句）。</para>
        /// <para>⚠️ **原版 `NavigationButtonClick` 那两下 `set_interactable(0)` / `(1)` 我们没有复刻**：
        /// 它挡的是「切图是异步加载、切到一半再点会串档」；我们这侧是**同步取 `Resources` 缓存**
        /// （`CardArt.AltArt` → `Get` 走 `_cache`），不存在那个窗口 —— **如实标注为偏离**，
        /// ⛔ 不要为了「像」而加一个没有对象的闸。</para></summary>
        public void ChangeAltStyle(int dir)
        {
            var styles = AltStylesOf(Card != null ? Card.Id : null);
            if (styles.Count == 0)
            {
                Debug.Log("[CardDetail] 切异画：这张卡**一种异画都没有** ⇒ 什么都不做"
                        + "（原版那一支里整块面板本来就是关着的；出声，不静默）");
                return;
            }
            int n = styles.Count + 1;                                  // 0 = 原画 + N 种异画
            AltStyleIndex = ((AltStyleIndex + dir) % n + n) % n;       // 回绕（原版 `SwapStyle.c:30/:36`）
            string which = AltStyleIndex == 0
                ? "原画"
                : CollectionWindow.StyleLabel(styles[AltStyleIndex - 1].Style);
            Debug.Log("[CardDetail] 切异画 → 第 " + AltStyleIndex + "/" + styles.Count + " 档「" + which
                    + "」（原版 `AlternateArtPanel.SwapStyle`：`index ±1` 后回绕 · 0 = 原画；"
                    + "换的是**前台那张**的图 —— 判据 `SetStyle.c:47-52` 的 `BasicCardUI.SetCardImage`）");
            RebuildKeepingState();
        }

        /// <summary>点「创建副本」那颗钮 —— 🔴 **2026-10-24 改判（主对话裁定）：真发请求 + 同一拍播特效**。
        ///
        /// <para>**为什么不单接特效**：用户 2026-10-18 曾拍板「点击副本，但是不起作用」；2026-10-24
        /// 主对话按「**⛔ 不合成却播特效 = 假反馈**」把它改成**两半一起做**（判据 →
        /// `资料/任务线/特效线/任务总表.md` 那条「锻造按钮的完整链路（请求 + 特效）」）。</para>
        ///
        /// <para>**链路的形状照原版**（`CraftingPanel__TryCraftingInternal.c`）：
        /// ① **先发请求**（`CardServices__CraftCard` 的本地等价物 = <see cref="DoCraft"/>：
        ///   扣 1 张同稀有度通配符 + `owned +1` + 落盘）；② **同一次调用里**再
        ///   `CraftingPanel__DoVFX()`（`CraftingPanel__TryCraftingInternal.c:51`）—— **不回调、不等服务端**。
        /// 失败那一支原版走 `OnFail_22_1`（`ErrorPopupWindowController.ShowErrorMessage(10)`）、**不播** ⇒
        /// 我们这里同样是「请求失败 ⇒ **一件特效都不播**」，只出声。</para>
        ///
        /// <para>⚠️ **货币不扣**：单机资源固定 2000（用户口径「不做真实经济」）；这里动的只有**通配符**，
        /// 那正是原版 `This will consume a wildcard` 说的那一样。通配符库存是本地存档
        /// （`CardProgress.Wildcards`，初始 99/档、没有服务端发放源 —— 如实标注的偏离）。</para>
        ///
        /// <para>🔴 **2026-10-24（A6）补齐原版的【闸】**（`CraftingPanel__TryCrafting.c:26-49` 逐句）：
        /// <code>
        /// if (card.Rarity != 4) goto normal;                       // 4 = Legendary
        /// if (InventoryManager.GetOwnedCount(card) != 1) goto normal;
        /// WindowsManager.ShowPopUp(MenuShop/ExtraLegendaryWarning, localizeTexts:1, closeOnEsc:0,
        ///                          [Cancel(OnPress=null), OK(OnPress = &lt;TryCrafting&gt;b__21_0)])
        /// return;                                                  // ← 这一次**不合成、不播**
        /// normal: TryCraftingInternal();
        /// </code>
        /// ⇒ 本方法现在只做**那一层闸**：传奇且拥有数 **== 1** ⇒ 弹确认窗后 `return`；其余一律
        /// <see cref="CraftNow"/>（= `TryCraftingInternal`）。⛔ 别把闸写进 <see cref="TryCraft"/> /
        /// <see cref="DoCraft"/> —— 原版那两半在 `TryCraftingInternal` 里，**确认窗的 OK 就是直调它**，
        /// 闸再写一份会让 OK 自己又弹一次窗（死循环）。
        ///
        /// <para>⚠️ **原版条件是 `== 1`**（`!= 1 ⇒ normal`）—— ⛔ 不是 `&lt;= 1` / `&lt; 2`。</para></summary>
        public void OnCraftClicked()
        {
            if (NeedsLegendaryCraftConfirm())
            {
                ShowLegendaryCraftConfirm();
                return;                                  // = 原版那一支的 `return`（不合成、不播）
            }
            CraftNow();
        }

        /// <summary>= 原版 `CraftingPanel.TryCraftingInternal`（**闸之后** —— 不需要确认、或确认窗点了 OK 时走）：
        /// ① 先发请求（<see cref="TryCraft"/> → <see cref="DoCraft"/>）；② **同一次调用里**播那两件特效。
        /// <para>⛔ 别在它里面再判一次「要不要确认」—— 原版 `TryCraftingInternal` 没有那道闸（OK 的回调
        /// `<TryCrafting>b__21_0` 调的就是它本身，判了会再弹一次窗）。</para></summary>
        void CraftNow()
        {
            int rarity;
            if (!TryCraft(out rarity)) return;
            PlayCraftVfx(rarity);
        }

        /// <summary>原版 `CraftingPanel.TryCrafting` 那道闸的**唯一判据**：
        /// `rarity == Legendary(4)` **且** `GetOwnedCount(card) == 1`。
        /// <para>稀有度换算只走 <see cref="CraftRarityIndex"/>（1..4，legendary = 4）、
        /// 拥有数取 <see cref="CardProgress.Owned"/> —— ⛔ 别在调用点另写一份 `rarity == "legendary"`。</para></summary>
        bool NeedsLegendaryCraftConfirm()
        {
            return Card != null
                && CraftRarityIndex(Card.Rarity) == 4
                && CardProgress.Owned(Card.Id, Card.Rarity) == 1;
        }

        /// <summary>弹那扇确认窗 = 原版 `WindowsManager.ShowPopUp(正文, localizeTexts:1, closeOnEsc:0,
        /// primaryButton, secondButton)`；宿主 = `PopUpGameWindow` 的 **2 按钮版**（`MessagePopupWindow2Buttons`）。
        /// <para>两颗钮**照原版顺序**：**左 = `Cancel`**（= `primaryButton`，原版 `OnPress = null`，
        /// 只会被 `PopUpWindowButtonPressed` 那个「每颗钮都挂」的监听关窗）· **右 = `OK`**
        /// （= `secondButton`，`OnPress` = `<TryCrafting>b__21_0` ⇒ `TryCraftingInternal`）。</para>
        /// <para>🔴 **2026-10-24（审查修复代理 `F3` · 铁律 5 就地订正 · 审 `P2-2`）**：本段原来写
        /// 「我们的 `WindowsManager.ShowMessagePopUp` **会复用还开着的那一扇**（原版 `LoadPopup` 是
        /// **先 `CloseWindow` + `Destroy` 旧的、再按按钮数 `Instantiate`**）⇒ 若场上还开着一扇 **1 按钮版**实例，
        /// 这颗 `OK` 就画不出来」—— **【已过期，那条机制差不存在了】**。</para>
        /// <para>**现状**（见紧接下面 `ShowLegendaryCraftConfirm` 开头那个 🆕 段）：`A11` 已在同一天把
        /// `ShowMessagePopUp` **照原版改成「每次销毁旧的 + 按按钮数重建」**（判据逐句 → 那边方法的 doc）
        /// ⇒ 正常路径上**不会**再出现「1 钮版实例 + 要两颗钮 ⇒ `OK` 画不出来」。
        /// 下面那段前置关闭**降级为第二道闸**（万一将来那一边又退化成「复用」才用得上）。</para>
        /// <para>**历史**（改前为什么这么写）：旧的复用语义下 `1 钮版` 实例会被留下来、`OK` 画不出来
        /// ——`A6` 当年就是这么发现的；当时用「先关掉逼它重开」绕过去，并**把那处机制差出声**（⛔ 不是静默绕开）。
        /// **错因**：这一段写于 `A11` 改口径之前，改完没有回来订正这几行（两句说法打架 → 铁律 5 / 6）。</para></summary>
        void ShowLegendaryCraftConfirm()
        {
            var mgr = WindowsManager.Get();
            // 🆕 **2026-10-24（`A11`）：这里降级成【第二道闸】** —— 原版
            //   `WindowsManager._LoadPopup_d__55__MoveNext.c:20-45` 每次都 `CloseWindow` + `Destroy` 旧的、
            //   再按按钮数挑 prefab `Instantiate`；`WindowsManager.ShowMessagePopUp` 已在同一天照它改完
            //   （判据与逐句对照 → 那边方法的 doc）⇒ **正常路径上这一段不会触发**。
            //   留着它只为一件事：万一将来有人把 `ShowMessagePopUp` 改回「复用同一扇」，这里还能兜住
            //   （1 钮版实例 + 要两颗钮 ⇒ OK 画不出来，A6 当年就是这么发现的）。
            var stale = mgr.popUpWindow as PopUpGameWindow;
            if (stale != null && stale.gameObject.activeSelf && !stale.TwoButtons)
            {
                Debug.Log("[CardDetail] 场上的消息弹窗是 **1 按钮版** ⇒ 先关掉，好按「两颗钮」重开一扇"
                        + "（⚠️ 这是**第二道闸**：`WindowsManager.ShowMessagePopUp` 现在照原版每次销毁重建，"
                        + "正常不会走到这里 —— 走到这里说明那一边又退化成「复用」了）");
                mgr.HidePopUp(true);
                // ⚠️ 关掉之后还要**销毁**：原版那一句 `Object.Destroy` 就是这个意思
                //   （我们的 `Close()` 只 `SetActive(false)`，不销毁 ⇒ 不销毁就会在锚点下留一具尸体）。
                CollectionWindow.DestroySafe(stale.gameObject);
            }
            CraftConfirmPopup = mgr.ShowMessagePopUp(
                KeyLegendaryCraftWarning,
                PopUpGameWindow.KeyCancel, HideCraftConfirm,                       // 左 = Cancel：只关窗
                KeyCraftConfirmOk, () => { CraftNow(); HideCraftConfirm(); });     // 右 = OK：先合成 → 再关窗
        }

        /// <summary>传奇合成确认窗的那三句文案的**术语键** —— 反编译 `CraftingPanel__TryCrafting.c` 里
        /// `ShowPopUp` 的四个实参逐个对上（**地址表实读**，办法见 `资料/全量反编译_入口与用法.md` ⑤：
        /// `stringliteral.json` 的 `address` 就是 RVA、`DAT_0x184XXXXXX = 0x180000000 + address`）：
        /// <list type="bullet">
        /// <item>`DAT_1842d24e0`（正文）= <c>MenuShop/ExtraLegendaryWarning</c></item>
        /// <item>`DAT_1842be120`（左钮 `primaryButton`）= <c>MainMenu/General/Cancel</c></item>
        /// <item>`DAT_1842be718`（右钮 `secondButton`）= <c>MainMenu/General/OK</c></item>
        /// </list>
        /// <para>⚠️ 正文那条键**在 `MenuShop/` 命名空间下**（原版就是这么复用的，⛔ 不是我们挑的）；
        /// 官方本地化表里中英文都齐（`Core/Loc.cs` 有这条：zh「请记住，您的卡组中只能使用每张传奇牌的
        /// 一份副本。……」/ en「You can only use one copy of each legendary card in your deck. ……」）。
        /// ⚠️ 原版 `localizeTexts = 1` ⇒ 这三条都过 `LocalizationManager.GetTranslation`
        /// （我们的 `PopUpGameWindow.Term`）。</para></summary>
        public const string KeyLegendaryCraftWarning = "MenuShop/ExtraLegendaryWarning";
        public const string KeyCraftConfirmOk = "MainMenu/General/OK";

        /// <summary>最近一次那扇**传奇合成确认窗**（自检读它；关掉之后是 `null`）。
        /// 类型 = <see cref="PopUpGameWindow"/>（原版 `popupWindowTwoButtons` 的宿主类）。</summary>
        public PopUpGameWindow CraftConfirmPopup { get; private set; }

        /// <summary>关掉确认窗 —— = 原版 `PopUpGameWindow.PopUpWindowButtonPressed`（`ConfigurePopUp` 给
        /// **每一颗**钮都挂的那个第二监听）→ `WindowsManager.HidePopUp`。
        /// <para>⚠️ 我们的 `WindowButton.onClick` 不会自动关窗（原版那颗钮的 `OnPress` 与「关窗」是
        /// **两个监听**，我们只能把两件事都写进回调里）⇒ Cancel 那颗也走这里。</para>
        /// <para>🔴 关不掉**出声**（红线）：`HidePopUp` 的判据是「顶窗就是它」，万一有人在我们头上又开了
        /// 一扇，它就不关 —— 那种情况不许说成「已经关掉了」。</para></summary>
        public void HideCraftConfirm()
        {
            var win = CraftConfirmPopup;
            CraftConfirmPopup = null;
            if (win == null) return;                           // 本来就没开（幂等）
            var mgr = win.Manager != null ? win.Manager : WindowsManager.Instance;
            if (mgr != null) mgr.HidePopUp();                  // 判据只留 `WindowsManager.HidePopUp` 一处
            else win.Close();                                  // 没有管理器（自检边角）⇒ 走最底那一层，别把模态卡死
            if (win != null && win.gameObject != null && win.gameObject.activeSelf)
                Debug.LogWarning("[CardDetail] 合成确认窗**没关掉**（`WindowsManager.HidePopUp` 没命中 ——"
                               + "多半是它上面又开了一扇窗）⇒ 窗仍开着，⛔ 不是已关。");
        }

        /// <summary>「创建副本」的**请求那一半**（= 原版 `CraftingPanel.TryCraftingInternal` 的前半段）。
        /// 成功返回 `true` 并给出 **1 基 rarity**（= `craftVFXs` 的下标 + 1，判据
        /// <see cref="CraftRarityIndex"/>）；失败**出声、返回 `false`**（调用方据此不播特效）。
        /// <para>唯一扣减口 = <see cref="DoCraft"/>（⛔ 别在这里另写一份扣减）。</para></summary>
        bool TryCraft(out int rarity)
        {
            rarity = 0;
            if (Card == null)
            {
                Debug.LogWarning("[CardDetail] 「创建副本」**请求不发**：窗上没有卡（`Card == null`）");
                return false;
            }
            rarity = CraftRarityIndex(Card.Rarity);
            if (rarity < 1)
            {
                rarity = 0;
                Debug.LogWarning("[CardDetail] 「创建副本」**请求不发**：稀有度「" + Card.Rarity
                               + "」不在原版 `craftVFXs` 的 1..4 里（`special` 那档原版整块不建）");
                return false;
            }
            if (!DoCraft())
            {
                // 失败原因与后续提示已在 DoCraft 里出声（原版这一支 = `OnFail_22_1` 的 ErrorPopup）。
                // 🔴 这里**必须**返回 false：原版失败**不调 `DoVFX`** ⇒ 我们也不许播（否则就是假反馈）。
                Debug.Log("[CardDetail] 「创建副本」请求失败 ⇒ **不播特效**"
                        + "（判据 = 原版 `TryCraftingInternal` 只在请求发出后调 `DoVFX`；失败走 `OnFail`）");
                return false;
            }
            return true;
        }

        /// <summary>「创建副本」的**特效那一半** = 原版 `CraftingPanel.DoVFX()`（逐句 →
        /// `普查产出_1023e/A31_特效接线.md` §2·1 与 `WarpforgeVFX/Runtime/WarpforgeUiEffect.cs` 文件头）：
        /// **两件**——按钮那道（`ParticleButton`）与前台卡那道（`ParticleCard`），各按 rarity 选 prefab。
        /// <para>落点照原版取**目标节点当时的世界位置**：按钮 = `Craft Bg` 节点；卡 = **前台那张**
        /// （`SlotViews[0]`，扇形槽 0）。⛔ 原版 `SetParent` 保**世界缩放**，所以这里传的父节点必须是
        /// **窗根**（`lossyScale = 1`）—— `WarpforgeUiEffect.PlayUi` 的护栏会替我们出声。</para></summary>
        void PlayCraftVfx(int rarity)
        {
            var btn = Find(transform, "Craft Bg");
            var front = SlotViews[0];
            var root = transform;                    // 窗根（`WindowsManager.AttachToAnchor` 把它归到 Holder，缩放 = 1）
            Vector3 btnWorld = btn != null ? btn.position : Vector3.zero;
            Vector3 cardWorld = front != null ? front.transform.position : btnWorld;
            if (btn == null || front == null)
                Debug.LogWarning("[CardDetail] 「创建副本」特效的落点少了一个："
                               + (btn == null ? "`Craft Bg` 不在" : "") + (btn == null && front == null ? " / " : "")
                               + (front == null ? "前台卡 `SlotViews[0]` 为空" : "")
                               + " ⇒ 缺的那件照旧不播（不静默）");
            // 两件都是 `WarpforgeUiEffect.PlayWildcardCraft`（= 原版 `craftVFXs[]` 表 + `.PlayUi` 组合口）：
            //   · onButton: true  = `Wildcard_Use_Button <rarity> <档名>`
            //   · onButton: false = `Wildcard_Use_Card   <rarity> <档名>`
            // ⚠️ 表里 **rarity 5** 是原版原值（Button 复用 4 / Card 回落 1）；我们四档才能合成 ⇒ 走不到。
            var pBtn = WarpforgeVFX.WarpforgeUiEffect.PlayWildcardCraft(rarity, true, root, btnWorld);
            var pCard = WarpforgeVFX.WarpforgeUiEffect.PlayWildcardCraft(rarity, false, root, cardWorld);
            if (pBtn == null || pCard == null)
                Debug.LogWarning("[CardDetail] 「创建副本」特效**没播全**（"
                               + (pBtn == null ? "按钮那件 = null" : "按钮那件 OK") + " / "
                               + (pCard == null ? "卡那件 = null" : "卡那件 OK")
                               + "）—— 最常见的原因是效果库缺条目 / prefab 引用为空（`Tools > Warpforge > 生成效果库`）");
            else
                Debug.Log("[CardDetail] 「创建副本」特效：rarity " + rarity + " ⇒ 「"
                        + WarpforgeVFX.WarpforgeUiEffect.WildcardCraftPrefab(rarity, true) + "」@ 钮 · 「"
                        + WarpforgeVFX.WarpforgeUiEffect.WildcardCraftPrefab(rarity, false) + "」@ 前台卡"
                        + "（判据 = 原版 `CraftingPanel__DoVFX.c`：Instantiate 在目标节点世界位置、保世界缩放）");
        }

        /// <summary>创建副本：**扣 1 张同稀有度通配符 + 拥有数 +1 + 落盘 + 重画**（= 原版 `CardServices.CraftCard` 的一半）。
        ///
        /// <para>🔴 **2026-10-24 就地订正（铁律 5）：本方法重新有了 UI 调用点** ——
        /// 链路 = `CraftHit` → `OnCraftClicked` → `TryCraft` → **本方法** → `PlayCraftVfx`
        /// （主对话裁定「请求 + 特效一起做」；2026-10-18 那版「不由 UI 调用」已作废，见 `OnCraftClicked`）。</para>
        ///
        /// <para>原版那一支（`CardServices__CheckForCardCrafting.c` / `CraftingPanel__TryCrafting.c`）：
        /// 按 **army + rarity** 找通配符且数量 &gt; 0 ⇒ 扣 1、`owned +1`（拥有数无上限）。
        /// ⚠️ **我们的等价物少一个维度**：通配符库存只按 **稀有度** 记（4 档），**不区分阵营** ——
        /// 因为详情窗底下那条通配符计数条本来就只画 4 格（原版那 4 格跟着**指针悬停那张卡**的阵营走，
        /// 单机没有发放源）。⇒ **如实标注为一处偏离**，⛔ 别当成原版语义。
        /// 🔴 **2026-10-24（A6）就地订正（铁律 5）**：原来这里写「原版『传奇且 `owned==1` 先弹确认窗』
        /// 那一支**没做**」—— **现在做了**，但**不在本方法里**：闸在 `OnCraftClicked`（= 原版
        /// `TryCrafting` 那一层）。本方法对应的是 `TryCraftingInternal`（闸**之后**）⇒ 它**不该**再判一次
        /// （确认窗的 OK 直调这条链，再判会自己又弹一次窗）。</para>
        /// <para>返回 `false` = **请求没发出去**（空卡 / 通配符 0 / 扣减失败）。调用方（`TryCraft`）据此
        /// **不播特效** —— 原版失败走 `OnFail_22_1`、**不调 `DoVFX`**。</para></summary>
        public bool DoCraft()
        {
            if (Card == null) return false;
            if (CardProgress.Wildcards(Card.Rarity) <= 0)
            {
                Debug.LogWarning("[CardDetail] 创建副本**失败**：「" + Card.Name + "」那种稀有度（" + Card.Rarity
                        + "）的通配符 = 0 —— 原版 `CraftingPanel__CheckButtons.c` 同支：没通配符 ⇒ 钮 `interactable=false`；"
                        + "走到这里说明有人绕过了灰钮（本方法照样拒绝、**不播特效**）");
                return false;
            }
            // 🔴 扣减的结果**必须判**：`TrySpendWildcard` 在竞争 / 存档异常时可能返回 false，
            //   不判就会「没扣成功却 +1 张卡」（静默送卡）。原版那一支是「服务端受理才 owned+1」。
            if (!CardProgress.TrySpendWildcard(Card.Rarity))
            {
                Debug.LogWarning("[CardDetail] 创建副本**失败**：扣通配符那一步没成功（「" + Card.Rarity
                               + "」现有 " + CardProgress.Wildcards(Card.Rarity) + " 张）⇒ 不做后面的 +1、不播特效");
                return false;
            }
            CardProgress.AddOwned(Card.Id, Card.Rarity, 1);
            Debug.Log("[CardDetail] 创建副本：「" + Card.Name + "」拥有数 → "
                      + CardProgress.Owned(Card.Id, Card.Rarity)
                      + "；扣 1 张「" + Card.Rarity + "」通配符（剩 " + CardProgress.Wildcards(Card.Rarity) + "）"
                      + " —— 原版 `CardServices.CraftCard`：同阵营同稀有度 1 张通配符 ⇒ owned +1；"
                      + "⚠️ 我们只按稀有度记库存（不分阵营），以本地库存等价服务端那半");
            RebuildKeepingState();
            return true;
        }

        /// <summary>🆕 **2026-10-12（A1699）**：点异画面板那颗价签（`Buy Original Card`）。
        /// 原版那条链**本地走不通** —— `AlternateArtPanel__BuyOriginalCard.c` 是
        /// `WindowsManager.ShowPopUp(<确认弹窗>)` + `PlayfabWrapper.GenericCloudScriptHandler(0x385, …)`
        /// （**服务端 CloudScript**），价格还来自 `ItemsConfig.GetOriginalArtPrice`（LiveOps 配置，本地没有）。
        /// ⇒ 照本笔口径：**接上命中区 + 出声**（⛔ 不留一颗「看起来能点、点下去什么都不发生」的钮）。</summary>
        public void OnBuyOriginalCardClicked()
        {
            Debug.Log("[CardDetail] **本版未实现：购买原卡面**（`Buy Original Card`）—— 那颗钮点了**什么都不做**"
                    + "（原版走 `PriceDisplayButton` → `AlternateArtPanel.BuyOriginalCard` → "
                    + "`PlayfabWrapper.GenericCloudScriptHandler` = **服务端**，且价格在 "
                    + "`ItemsConfig.GetOriginalArtPrice`（LiveOps 配置，本地取不到））。");
        }

        /// <summary>**降级**：等级 −1（下限 1）⇒ 重画 ⇒ 卡框降一档。
        ///
        /// <para>🆕 **2026-10-18 用户拍板**（逐字：「下面的按钮从一个按钮改为两个『降级』『升级』并列，
        /// 可以点击这两个按钮实现卡框的**降级和升级**」）。判据与「升级」完全对称：
        /// 卡框档 = 等级（1 基），重算点只在 `CardView.Build()` 里那句 `CardProgress.Level(d.artId)`。</para>
        ///
        /// <para>⚠️ **不消耗任何东西**（副本 / 金币都不退也不扣）：升级那一路本来就只做「够不够」的检查、
        /// 不真扣（单机资源固定 2000 —— 2026-10-24 由 9999 改为 2000），降级自然也**只改等级**。⛔ 别为降级加一套退款逻辑（原版没有这一支）。</para></summary>
        public bool DoDowngrade()
        {
            if (Card == null) return false;
            int before = CardProgress.Level(Card.Id);
            if (before <= 1)
            {
                Debug.Log("[CardDetail] 已经是最低档（1 级）⇒ 降不了（用户口径：最低级**不提示**、只把钮灰掉）");
                return false;
            }
            CardProgress.SetLevel(Card.Id, before - 1);
            int after = CardProgress.Level(Card.Id);
            Debug.Log("[CardDetail] " + Card.Name + " 等级 → Lv" + after + "（卡框档 = tier" + after + "）"
                    + " —— 「降级」那颗钮：Lv" + before + "→Lv" + after + "，不扣也不退任何资源");
            RebuildKeepingState();
            return true;
        }

        /// <summary>升级：够副本才升（**只吃多余副本**，同原版 `拥有数 >= 卡组上限 + 需要张数`）。
        ///
        /// <para>🆕 **2026-10-18**：那颗钮不再写死不可交互（改判见 `BuildUpgrade` 里那段）——
        /// 本方法的调用链现在是**活的**：`UpgradeHit` → `DoUpgrade` → `SetLevel +1`（落盘）
        /// → `RebuildKeepingState()` 重画 ⇒ **卡框换到新档**（`CardView` 按 `CardProgress.Level` 取 `CardArt.Frame/Sdf` 的 `tier`）。
        /// ⚠️ 卡框档 = 等级（1 基），重算点在 `CardView.Build()` 里那一句
        /// `int frameTier = … CardProgress.Level(d.artId)` —— **不在这里另算一份**。</para>
        ///
        /// <para>🆕 **2026-10-18（同一天第二轮）**：升级钮现在与「降级」**并列**，置灰判据按用户口径
        /// = **只看等级上限**（`lv == 4` ⇒ 灰）——「多余副本够不够」仍在本方法里把着（不够就出声、不升级），
        /// 只是**不再拿它决定钮灰不灰**（用户点名的是等级那一档）。</para></summary>
        public bool DoUpgrade()
        {
            int before = CardProgress.Level(Card.Id);
            if (before >= CardProgress.MaxLevel)
            {
                Debug.Log("[CardDetail] 已经满级（" + CardProgress.MaxLevel + " 级）⇒ 升不了");
                return false;
            }
            if (!CardProgress.CanUpgrade(Card.Id, Card.Rarity))
            {
                Debug.Log("[CardDetail] 副本不够 ⇒ 升不了（多余副本 "
                          + CardProgress.Spares(Card.Id, Card.Rarity) + " < 需要 "
                          + CardProgress.NeedCopies[before + 1] + "）");
                return false;
            }
            CardProgress.SetLevel(Card.Id, before + 1);
            int after = CardProgress.Level(Card.Id);
            // 🆕 2026-10-18（用户要求「升级要有可见反馈」）：这一行是落点 —— 重画之后卡框会换成新档。
            //   日志格式按用户 2026-10-18 指定：`[CardDetail] <卡名> 等级 → Lv{N}（卡框档 = tierK）`。
            Debug.Log("[CardDetail] " + (Card != null ? Card.Name : "?") + " 等级 → Lv" + after
                      + "（卡框档 = tier" + after + "）"
                      + " —— 「升级」那颗钮：Lv" + before + "→Lv" + after
                      + "（**不扣货币**：单机资源固定 2000（2026-10-24 由 9999 改为 2000）；升级表数值**是我们编的**）");
            RebuildKeepingState();
            return true;
        }

        /// <summary>播这张卡的单位语音（复用 `VoiceLines` —— **只此一处**，不另写后缀表）。
        /// ⚠️ 认的是**前台那张**（`FrontDef`）—— 原版换位收尾会 `SetCardVoiceOver(新前台)`
        /// （判据 → 正本 §9·6 ⑤），所以点了相关卡之后这颗钮该播**新前台**的语音。</summary>
        public bool PlayVoice()
        {
            LastVoiceFile = null;
            var c = FrontDef ?? Card;
            if (c == null) return false;
            string key = !string.IsNullOrEmpty(c.Id) ? c.Id : c.Name;
            if (!VoiceLines.Has(key))
            {
                Debug.Log("[CardDetail] 「" + c.Name + "」没有单位语音（`VoiceLines.Has` 为假）—— 这次没声音");
                return false;
            }
            string file, text;
            if (!VoiceLines.TryPick(key, VoiceLines.ForVoiceOver, null, out file, out text)) return false;
            var clip = VoiceLines.Clip(file);
            if (clip == null) return false;
            WarpforgeVFX.WFSoundPlayer.Play(clip, true, 1f, 1f);
            LastVoiceFile = file;
            return true;
        }

        void RebuildKeepingState()
        {
            Build();
            LastOpened = this;
            LogRebuildPanels();          // 🆕 2026-10-18：防回归（用户真机报过「点升级后两颗按钮底图消失」）
        }

        /// <summary>🆕 **2026-10-18 防回归日志** —— 每次**整窗重建**之后打一行三块面板的在/缺。
        ///
        /// <para>用户当天真机报：「点『升级』之后，升级面板里**两颗按钮的底图不见了、只剩『降级』『升级』四个字**」。
        /// 根因 = 面板底板与面板内容**同渲染队列（3116）+ 同一平面** ⇒ 谁盖谁不可控（详见 `PanelBox` 那一处注释）；
        /// 已把底板降到 `QCd`(3115)。这一行日志的价值：**底图/命中区一旦又没建出来，日志里当场看得见**
        /// （`Downgrade/Upgrade = 无`），⛔ 不必再靠「用户看截图发现」。</para>
        ///
        /// <para>第二行顺带把**两颗底图**的**渲染队列与贴图**打出来 —— 队列掉到面板底板那一档（或贴图空）
        /// 就是同一族缺陷复发；⛔ 别把它当噪音删掉。</para></summary>
        void LogRebuildPanels()
        {
            string crafting = FindNewest("Crafting Panel") != null ? "有" : "无";
            string down = FindNewest("DowngradeHit") != null ? "有" : "无";
            string up = FindNewest("UpgradeHit") != null ? "有" : "无";
            Debug.Log("[CardDetail] 面板重建：Crafting=" + crafting + " · Downgrade=" + down + " · Upgrade=" + up);
            Debug.Log("[CardDetail] 面板重建·按钮底图：" + BgInfo("Downgrade Bg") + " · " + BgInfo("Upgrade Bg"));
        }

        /// <summary>在**最新那一棵**子树里按名找节点。
        /// 🔴 **为什么不能用 `Find`**：运行时 `Build()` 用的是 `Object.Destroy`（**帧末才真销毁**）⇒
        /// 重建后的**同一帧里新旧两棵树并存**，`GetComponentsInChildren` 会先撞上**旧的那颗**（同名）。
        /// `Build()` 是「先销毁旧子件、再按序新建」⇒ **新那棵一定排在后面** ⇒ 从最后一个子件往前找即可。</summary>
        Transform FindNewest(string name)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                foreach (var t in transform.GetChild(i).GetComponentsInChildren<Transform>(true))
                    if (t.name == name) return t;
            return null;
        }

        /// <summary>`LogRebuildPanels` 用：一颗底图节点的「在不在 + 渲染队列 + 贴图名」。</summary>
        string BgInfo(string nodeName)
        {
            var t = FindNewest(nodeName);
            var q = t != null ? t.GetComponent<ImageQuad>() : null;
            if (q == null) return nodeName + "=**无**";
            return nodeName + "=有（q" + q.RenderQueue + " · tex="
                   + (q.Texture != null ? q.Texture.name : "**空**") + "）";
        }

        // ============================================================ 小工具

        Transform PanelBox(Transform panel, string name, float y1, float y2)
        {
            var p = MenuDraw.Node(panel, name, new PxRect(PanL, y1, PanR, y2));
            // 🔴 **原版这里确实是 `UI_Deck_Information_Back` + `type=Sliced`**（2026-09-24 实读
            //    `工具/menu_dump.py … "Crafting Panel"` 确认），但那块面板只有 **450 宽**，
            //    而这张图的 `m_Border=(42,363,655,81)` 左+右 = **697 > 450** ⇒ **九宫格根本放不下**
            //    （原版 Unity 会把它挤成「左右边框各占一半」，我们这套 `CreateNineSlice` 会**退回单块**）。
            //    ⇒ 我们**显式选「单块拉伸」**（不静默、也不吐一串警告），**如实记成一处偏差**，
            //    见 `项目资料` 那边的 §三 第 15 条。
            var tex = CardArt.MenuUi("UI_Deck_Information_Back");
            if (tex == null) Debug.LogWarning("[CardDetail] 面板底图取不到：UI_Deck_Information_Back");
            // 🔴 **2026-10-18 修（用户真机报「点『升级』后两颗按钮底图消失、只剩字」）**：
            //   这块**面板底板**原来画在 `QCdRow`(3116) —— **和面板里所有内容同一个队列**
            //   （按钮底图 / 图标 / 计数条底 … 全是 3116），而本工程**同队列 + 同一平面（z 全 0）时
            //   「谁盖谁」不可控**（`CLAUDE.md` §三「分层要用渲染队列，不能用 z」· `资料/已知的坑.md` 那条
            //   「同一条渲染队列里故意重叠的两件 = 谁盖谁不定」；本文件 `BuildWildcards` 的万能卡图标
            //   被同队列底板盖掉就是同一族的现行实例）。
            //   ⇒ 重建整窗之后渲染件重新注册，先后一翻 ⇒ **底板盖住了按钮底图**；文字在 `QCdText`(3117)
            //   高于底板，所以「只剩字浮着」—— 与用户描述的现场逐条吻合。
            //   办法 = **底板降到 `QCd`(3115)**：它本来就是「面板」那一档（见上面那几个常量的注释
            //   「3115 本窗的钮 / 字 / 面板」），面板里**任何**内容都在它之上 ⇒ 与重建次数/注册顺序无关。
            //   ⛔ 别改回 `QCdRow`；也⛔ 别去抬每一颗内容（要抬的地方太多，且以后新加一件又会踩）。
            else MenuDraw.Rect(p, tex, new PxRect(PanL, y1, PanR, y2), "Background", QCd);
            return p;
        }

        /// <summary>🆕 **2026-10-18**：把**一整块面板**（底板 + 全部子件）整体下移 `dy` 设计 px
        /// （用户口径：三块面板之间要留出**一致**的间距 —— 见 `PanelGap`）。
        ///
        /// <para>🔴 **为什么挪父节点就够**：本壳的件是**绝对设计坐标**建的（`MenuDraw.Local` 在**建的那一刻**
        /// 就把「绝对矩形 − 父节点位置」写进 `localPosition`）⇒ 子件的相对位置已经定死，
        /// **挪父节点 = 整块一起挪**，⛔ 不用去逐个改 rect（那才会漏、会与建站点的常量打架）。</para>
        ///
        /// <para>⚠️ 必须在**这一块的所有子件都建完之后**调 —— 建的过程中父节点还在原位，先挪会把后半截建歪。
        /// ⚠️ 尺寸不变（`y2 − y1` 一个字节没动），只改中心 ⇒ 面板自身的高宽仍是 prefab 值。</para></summary>
        static void ShiftPanelDown(Transform p, float y1, float y2, float dy)
        {
            if (p == null || Mathf.Approximately(dy, 0f)) return;
            MenuDraw.ApplyPxRect(p, p.parent, new PxRect(PanL, y1 + dy, PanR, y2 + dy));
        }

        void Title(Transform p, string nodeName, string text, float x1, float y1, float x2, float y2, float px,
                   TmpFont.Face face = TmpFont.Face.LatinCardBody)
        {
            // 限宽换行 + 自适应（原版这几处都是 `auto(min-max)` 的 TMP）
            // 🆕 **2026-10-12（A333 + A336③）**：三处面板标题的原版读数**逐值相同** ——
            //   `m_fontSizeMax = **42**`（三颗都 42，**其中两颗的标称字号不是 42**：41.4 / 31.7 ⇒
            //   旧写法把 `fontPx` 当上限，**天花板比原版矮**）· `m_fontSizeBase = **36.0**` · `m_fontSizeMin = 10`。
            //   判据 = `bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/` 逐颗按文案现读：
            //     `'Create a copy of this card'` → `MonoBehaviour_1772.json` = `fs 42.0 · auto[10~42] · base 36.0`
            //     `'Upgrade this card\nto level {'` → `MonoBehaviour_1883.json` = `fs 41.4 · auto[10~42] · base 36.0`
            //     （另一份 `MonoBehaviour_1776.json` 是 `fs 31.7 · auto[10~42] · base 36.0` = **异画面板那一颗**）
            // 🆕 **2026-10-18**：`face` 形参是为「升级面板那句**用户指定的中文标题**」加的（默认仍是
            //   `LatinCardBody` ⇒ 另外两处标题**逐位不变**）。中文那一处传 `Face.Cjk` —— 混排兜底虽然
            //   能画汉字，但字号换算是按 `ActualFace` 的 em 走的（`Label.TmpFontSize`），纯中文串走
            //   CJK 面才与本仓其它中文（默认面）同一档。⛔ 别改成「一律 Cjk」：那会动另外两处英文标题的行盒。
            MenuDraw.Text(p, new PxRect(x1, y1, x2, y2), text, Color.white, nodeName, px, QCdText, x2 - x1, 10f,
                          42f, 36f, face: face);
        }

        Transform Hit(Transform parent, string name, PxRect r, System.Action onClick, int q = QCdHit,
                      ImageQuad target = null, string art = null, string hoverArt = null,
                      string clickCue = null)
        {
            var hit = MenuDraw.Node(parent, name, r);
            var quad = ImageQuad.Create(hit, CardArt.Solid(), Vector3.zero, LayoutSpace.Px(r.H),
                                        new Vector2(0.5f, 0.5f), "Hit");
            if (quad != null)
            {
                quad.SetAspect(r.W / Mathf.Max(1e-6f, r.H));
                quad.SetTint(new Color(0f, 0f, 0f, 0f));
                quad.SetRenderQueue(q);
            }
            var wb = hit.gameObject.AddComponent<WindowButton>();
            wb.onClick = onClick;
            wb.clickCue = clickCue;
            if (target != null) wb.Bind(target, art, hoverArt);
            return hit;
        }
        Transform Hit(Transform parent, string name, PxRect r, System.Func<bool> onClick, int q)
        {
            return Hit(parent, name, r, () => { onClick(); }, q);
        }
    }
}
