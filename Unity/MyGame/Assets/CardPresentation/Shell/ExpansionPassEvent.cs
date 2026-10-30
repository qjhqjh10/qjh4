// ExpansionPassEvent.cs — 活动线 **B15**：`Everguild.LiveOps.ExpansionPassEvent` 一族的**本地等价实现**
// （纯数据 / 状态机 / 本地存档；**不含任何建树代码** —— 视图那半边在 `Shell/ExpansionPassEventWindow.cs`）。
//
// ============================ 判据（唯一来源，全部本地现读 · 2026-10-27）============================
// ① **ILSpy（方法体可读，首选）**：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/`
//      · `Everguild.LiveOps/ExpansionPassEvent.cs`（12 个方法 + `Milestones`/`PlayerScore`/`HasPremium`）
//      · `Everguild.LiveOps/ExpansionPassSave.cs`（`Score(int)` / `RewardClaimCount(int)` / `AvailableScore`）
//      · `Everguild.LiveOps/ExpansionMilestone.cs`（`isPremium` / `rewards` / `IsLimited => true`）
//      · `Everguild.LiveOps/ScoringMilestone.cs`（`State` 四个值 · `IsUnlocked` · `scoreToUnlock` / `limitRewardQuantity`）
//      · `Everguild.LiveOps/ScoringEvent.cs`（基类 `GetState` / `IsMilestoneCollected`）
//      · `ScoringDataInfo.cs`（三段分 `Current/Previous/Next`）
// ② **Ghidra（常量与签名更准，用于交叉 `GetState` / `IsMilestoneCollected` / `GetCurrentMilestoneIndex`）**：
//      `d:/2/tools/decomp_full/Everguild.LiveOps.ExpansionPassEvent__{GetState,IsMilestoneCollected,
//        GetCurrentMilestone,GetCurrentMilestoneIndex,GetCostToLevelUp,Score}.c`
// ③ **调用点（决定「谁需要哪一格」）**：
//      `d:/2/tools/decomp_full/ExpansionPassRewardItemContainer__Initialize.c:88-170`
//      （`GetState` 两次 · `GetBaseData().ScoringItem` · `GetCostToLevelUp` · `RectMask2D.enabled = (index == 0)`）
//      · `ExpansionPassPurchasePremiumPanel.cs`（`HasPremium` · `Milestones.First/Last(IsPremium)` · `ExtraPercentaje`）
//
// ============================ 🔶 本地等价物的边界（照实记，⛔ 不假装原版）============================
// · **服务器事务**：原版 `PurchaseMilestone` / `PurchaseLevelUp` / `Expire` 走 `LiveOpsUtility.SendRequest`
//   （`Method not found @180D620D0`，请求体没解出来）。本机原版**已关服**（判据 → `MEMORY.md` 的
//   `warpforge-live-game-verification`）⇒ 这三条按**本地事务**实现：校验 → 写本地存档 → 回调。
//   这是**机制不同、行为等价**（领了就没得再领、分数到阈值就解锁）⇒ 按 CLAUDE.md 铁律 18 打 `🔶 自己设计`。
// · **`HasPremium`**：原版读 `InventoryManager.HasItem(ExpansionPassData.PremiumItem)`
//   （`ExpansionPassEvent.get_HasPremium` → `SingletonBehaviour<InventoryManager>.Instance.HasItem(item)`）。
//   我们没有 `InventoryManager`（`Shell/Inventory.cs` 是**另一条线 C4 的件**，不是同一套）⇒ 本地用一个 bool。
// · **`Score(int)` 的落盘**：原版 `totalScore`/`spentScore` 在远端存档；本地落
//   `persistentDataPath/WarpforgeExpansionPass/pass.json`（照 `Shell/BattleLogData.cs` 那套
//   独立目录 + `OverrideDir` + 批处理不碰玩家真档）。
// · **里程碑数据**：原版由远端 LiveOps 下发（`ExpansionPassData.ScoringMilestones`）。本机**没有**任何一条真数据
//   ⇒ 本类**不内置任何编造的里程碑**（`Milestones` 出厂是空表）。自检要验机制时**由测试注入合成档**。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `Everguild.LiveOps.ExpansionMilestone : ScoringMilestone`（`cpp2il/Everguild.LiveOps/ExpansionMilestone.cs`）。
    /// <para>🔴 **基类不在这里**：`ScoringMilestone` / `ScoringDataInfo` / `ScoringMilestoneState` 三件由
    /// **`Shell/ScoringEvent.cs`（活动线 C2 · 2026-10-27）** 收口（原版它们就是 `Everguild.LiveOps` 的共享类型）
    /// ⇒ 本文件**不重复定义**，直接引用。`scoreToUnlock` / `limitRewardQuantity` 原版是 private 字段 + 只读属性，
    /// 写口只有基类的 `SetRaw(...)`（构造期用）。</para></summary>
    public class ExpansionMilestone : ScoringMilestone
    {
        /// <summary>= 子类 `isPremium`（`+0x20`）。**只有它 + `HasPremium` 都成立时** `GetState` 才可能出
        /// `LockedBecauseNotPremium`（判据 → `ExpansionPassEvent__GetState.c:17-46`）。</summary>
        public bool IsPremium;

        /// <summary>= 子类 `rewards`（`RewardInfo[]`）。原版 `RewardInfo = { item, quantity, rewardTier }`
        /// ⇒ 我们在 `CampaignData.RewardSpec` 上等价（`Id` / `Quantity` / `Tier`），见那份 struct 的 doc。
        /// <para>留一个具体数组（`RewardSpec[]`）而不是只留基类那个 `IReadOnlyList`：`PurchaseMilestone` 的回调签名
        /// 与 `RewardWindow.ShowPreview` 都要数组，省一次 `ToArray()` 且保证同一个实例。</para></summary>
        public CampaignData.RewardSpec[] RewardArray;

        /// <summary>原版 `ExpansionMilestone` **override** 成恒 `true`（`cpp2il/Everguild.LiveOps/ExpansionMilestone.cs:16`）
        /// —— ⛔ 别照基类那套 `limitRewardQuantity != 0` 去算（子类把它整个盖掉了）。</summary>
        public override bool IsLimited { get { return true; } }

        /// <summary>基类那个抽象属性：本子类返回 <see cref="RewardArray"/>（空 ⇒ 空表，⛔ 不返回 null）。</summary>
        public override IReadOnlyList<CampaignData.RewardSpec> Rewards
        {
            get { return RewardArray ?? Array.Empty<CampaignData.RewardSpec>(); }
        }

        public ExpansionMilestone() { SetRaw(0, 1); }

        /// <summary>`limitRewardQuantity` 默认 **1**：`IsMilestoneCollected` 的判定式是
        /// `RewardClaimCount(index) &gt;= LimitRewardQuantity`（`ExpansionPassEvent__IsMilestoneCollected.c:47-50`
        /// 的 `a &gt;= b` 惯用式）⇒ 填 0 会让「没领过」也判成已领（**原版同款算式**，⛔ 别在我们这边加 `&gt; 0` 的补丁去「修」它）。</summary>
        public ExpansionMilestone(int scoreToUnlock, int limitRewardQuantity, bool isPremium,
                                  params CampaignData.RewardSpec[] rewards)
        {
            SetRaw(scoreToUnlock, limitRewardQuantity);
            IsPremium = isPremium;
            RewardArray = rewards ?? Array.Empty<CampaignData.RewardSpec>();
        }
    }

    /// <summary>原版 `Everguild.LiveOps.ExpansionPassData : ScoringEventData` 里**本窗真正读到的四格**
    /// （`cpp2il/Everguild.LiveOps/ExpansionPassData.cs` 的属性列表 + 两个调用点）。
    /// <para>⛔ 不把整张 data 表搬进来：只有这四格有消费者（`ExpansionPassRewardItemContainer.Initialize` /
    /// `ExpansionPassPurchasePremiumPanel.Initialize` / `GetCostToLevelUp`）。</para></summary>
    public class ExpansionPassData
    {
        /// <summary>= `ScoringEventData.ScoringItem`（`ObtainableItem`）—— 进度条左侧那颗**分数图标**。
        /// 我们只留 id（`ItemDrawer` 按 id 取图 / 取占位板）。</summary>
        public string ScoringItemId;

        /// <summary>= `PremiumItem.BackgroundSprite`（`ItemDrawer` 拿它当高级档角标）。
        /// 本地是 `Resources/Art/` 里的图名，取不到就 null（调用方出声）。</summary>
        public string PremiumBadgeSprite;

        /// <summary>= `ExpansionPassData.XpPerGold`（`GetCostToLevelUp` 的分母，`ExpansionPassEvent__GetCostToLevelUp.c` 的 `+0x80`）。
        /// **0 / 负数 ⇒ 除数非法** ⇒ `GetCostToLevelUp` 会出声并返回 0（⛔ 不静默给个编出来的价）。</summary>
        public int XpPerGold;

        /// <summary>= `ExpansionPassData.PremiumPercentageBonus`（`ExtraPercentaje` 词条的参数）。</summary>
        public int PremiumPercentageBonus;
    }

    /// <summary>原版 `Everguild.LiveOps.ExpansionPassSave : ScoringEventPersistence`
    /// （`cpp2il/Everguild.LiveOps/ExpansionPassSave.cs`）。
    /// <para>`Score(int)` 的两支：**负数 ⇒ 记到 `spentScore`**（花掉的分），**非负 ⇒ 记到 `totalScore`**。</para></summary>
    public class ExpansionPassSave
    {
        public int TotalScore;
        public int SpentScore;

        /// <summary>= `rewardClaimCounter`（`Dictionary&lt;string,int&gt;`，键 = 里程碑下标的 `ToString()`）。
        /// ⚠️ `JsonUtility` **不支持 `Dictionary`** ⇒ 落盘走 <see cref="ClaimEntries"/> 那个平行数组。</summary>
        readonly Dictionary<string, int> _claims = new Dictionary<string, int>();

        /// <summary>原版 `AvailableScore => totalScore - spentScore`。</summary>
        public int AvailableScore { get { return TotalScore - SpentScore; } }

        /// <summary>原版 `Score(int amount)`：`amount &lt; 0 ⇒ spentScore -= amount；否则 totalScore += amount`。</summary>
        public void Score(int amount)
        {
            if (amount < 0) SpentScore -= amount;
            else TotalScore += amount;
        }

        /// <summary>原版 `RewardClaimCount(int level)` = `rewardClaimCounter[level.ToString()]`，缺键默认 0。</summary>
        public int RewardClaimCount(int level)
        {
            int v;
            return _claims.TryGetValue(level.ToString(), out v) ? v : 0;
        }

        /// <summary>记一次领取（= 原版服务器在 `PurchaseMilestone` 成功后回传的那份 save 里的改动）。
        /// 每次 +1（**不是** +`limitRewardQuantity` —— 原版判的是「次数 ≥ 上限」）。</summary>
        public void AddClaim(int level) { _claims[level.ToString()] = RewardClaimCount(level) + 1; }

        /// <summary>清掉某档的领取计数（自检用；正式路没有「撤销领取」）。</summary>
        public void ClearClaim(int level) { _claims.Remove(level.ToString()); }

        /// <summary>落盘用的平行数组（`JsonUtility` 不支持字典）。</summary>
        [Serializable]
        public class ClaimEntry
        {
            public string key;
            public int value;
        }

        /// <summary>导出成两个平行数组（`keys[i]` ↔ `values[i]`）。顺序 = 字典的插入序（自检要稳定就自己排）。</summary>
        public void ExportClaims(out string[] keys, out int[] values)
        {
            var k = new List<string>(_claims.Count);
            var v = new List<int>(_claims.Count);
            foreach (var kv in _claims) { k.Add(kv.Key); v.Add(kv.Value); }
            keys = k.ToArray();
            values = v.ToArray();
        }

        /// <summary>从平行数组还原（长度不等 ⇒ 出声并只吃 `min` 长度那段，⛔ 不静默错位配对）。</summary>
        public void ImportClaims(string[] keys, int[] values)
        {
            _claims.Clear();
            if (keys == null || values == null) return;
            if (keys.Length != values.Length)
            {
                Debug.LogWarning("[ExpansionPass] 存档里 claimKeys(" + keys.Length + ") 与 claimValues("
                               + values.Length + ") 长度不等 ⇒ 只按较短的那一段还原。");
            }
            int n = Math.Min(keys.Length, values.Length);
            for (int i = 0; i < n; i++)
            {
                if (string.IsNullOrEmpty(keys[i])) continue;
                _claims[keys[i]] = values[i];
            }
        }

        // ════════════════════════════════════════════════════════════════════════════════
        //  本地落盘（形状照 `Shell/BattleLogData.cs` 那一套，⛔ 别自创第二种写法）
        // ════════════════════════════════════════════════════════════════════════════════

        /// <summary>落盘目录名（`persistentDataPath` 下）。</summary>
        public const string StoreDirName = "WarpforgeExpansionPass";
        /// <summary>落盘文件名（不带路径分隔符）。</summary>
        public const string StoreFileName = "pass.json";

        /// <summary>自检用的目录改写口：设了就用它 —— **批处理/自检不该动玩家真存档**（同 `ReplayStore.OverrideDir`）。</summary>
        public static string OverrideDir;

        static string StoreDir
        {
            get
            {
                return string.IsNullOrEmpty(OverrideDir)
                     ? System.IO.Path.Combine(Application.persistentDataPath, StoreDirName)
                     : OverrideDir;
            }
        }

        /// <summary>存档文件完整路径（日志里打出来 —— 编辑器里它在 `AppData` 深处）。</summary>
        public static string StorePath { get { return System.IO.Path.Combine(StoreDir, StoreFileName); } }

        /// <summary>门：这一趟允许碰盘吗（与 `BattleLogData.StoreOn` 逐条同义）。
        /// ① `OverrideDir` 设了 ⇒ 允许；② 否则只有真在跑（`isPlaying &amp;&amp; !isBatchMode`）才允许
        /// ⇒ **11 条批处理自检一律不碰玩家真档**（结构上保证）。</summary>
        static bool StoreOn
        {
            get { return !string.IsNullOrEmpty(OverrideDir) || (Application.isPlaying && !Application.isBatchMode); }
        }

        /// <summary>盘上那一份。字段名就是 JSON 键 ⇒ 改名 = 换版本。</summary>
        [Serializable]
        class StoreDto
        {
            public int version = 1;
            public int totalScore;
            public int spentScore;
            public string[] claimKeys;
            public int[] claimValues;
        }

        /// <summary>读盘。**缺档 ⇒ 返回出厂空档**（⛔ 不吵：第一次跑是正常态，同 `BattleLogData`）。</summary>
        public static ExpansionPassSave Load()
        {
            var save = new ExpansionPassSave();
            if (!StoreOn) return save;
            string path = StorePath;
            if (!System.IO.File.Exists(path)) return save;
            string text;
            try { text = System.IO.File.ReadAllText(path); }
            catch (Exception e)
            {
                Debug.LogWarning("[ExpansionPass] 读存档失败（" + path + "）：" + e.Message + " ⇒ 按空档走，**没动文件**。");
                return save;
            }
            StoreDto dto;
            try { dto = JsonUtility.FromJson<StoreDto>(text); }
            catch (Exception e)
            {
                Debug.LogWarning("[ExpansionPass] 存档解不出内容（" + path + "）：" + e.Message + " ⇒ 按空档走，**没动文件**。");
                return save;
            }
            if (dto == null) return save;
            if (dto.version != 1)
            {
                Debug.LogWarning("[ExpansionPass] 存档版本 " + dto.version + " 不认识（本代码只认 1）⇒ 不读，按空档走：" + path);
                return save;
            }
            save.TotalScore = dto.totalScore;
            save.SpentScore = dto.spentScore;
            save.ImportClaims(dto.claimKeys, dto.claimValues);
            return save;
        }

        /// <summary>写盘。返回是否真的写成功（读盘门关着时返回 `false` 且**不报错** —— 那是设计）。
        /// 写法 = 先写 `.tmp` 再替换（原子，同 `SaveCenter.SetRaw` 那条口径）。</summary>
        public bool Save()
        {
            if (!StoreOn) return false;
            var dto = new StoreDto { totalScore = TotalScore, spentScore = SpentScore };
            ExportClaims(out dto.claimKeys, out dto.claimValues);
            string json;
            try { json = JsonUtility.ToJson(dto); }
            catch (Exception e)
            {
                Debug.LogError("[ExpansionPass] 存档序列化失败：" + e.Message + " ⇒ **没写盘**");
                return false;
            }
            string path = StorePath;
            string tmp = path + ".tmp";
            try
            {
                System.IO.Directory.CreateDirectory(StoreDir);
                System.IO.File.WriteAllText(tmp, json);
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                System.IO.File.Move(tmp, path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[ExpansionPass] 存档写盘失败（" + path + "）：" + e.Message);
                try { if (System.IO.File.Exists(tmp)) System.IO.File.Delete(tmp); } catch { }
                return false;
            }
        }

        /// <summary>自检/调试用：把盘上那份读回来（⛔ 与内存态无关，专验「真的落了盘」）。</summary>
        public static ExpansionPassSave LoadFromDiskForTest() { return Load(); }

        /// <summary>自检用：删档（只在 `OverrideDir` 指向临时目录时用）。</summary>
        public static void DeleteStoreForTest()
        {
            string path = StorePath;
            try { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); }
            catch (Exception e) { Debug.LogWarning("[ExpansionPass] 删测试档失败：" + path + "：" + e.Message); }
        }

        // ════════════════════════════════════════════════════════════════════════════════════
        //  🆕 2026-10-31（`B104_P3`）：写盘成败的**判据口** + 两条**回滚**（写盘失败时撤销内存改动）
        // ════════════════════════════════════════════════════════════════════════════════════

        /// <summary>这一趟允不允许碰盘（= 私有 `StoreOn` 的只读口）。
        /// <para>🔴 **为什么需要它**：<see cref="Save"/> 在「落盘门关着」时**恒返回 `false`**（那是设计上的空操作，
        /// 批处理自检不该碰玩家真档）；只有 `PersistOn == true` 时候的 `false` 才是**真写盘失败**。
        /// ⇒ 调用方（`Score` / `PurchaseMilestone` / `PurchaseLevelUp`）必须两者一起判，⛔ 别把「门关着」当失败。</para></summary>
        public static bool PersistOn { get { return StoreOn; } }

        /// <summary>把某档的领取计数**直接写成** <paramref name="count"/>（`&lt;= 0` ⇒ 删键）。
        /// <para>唯一的用户 = `PurchaseMilestone` 写盘失败时的**回滚**（原版失败由服务器回滚，本地只能自己撤）。
        /// ⛔ 别拿 `ClearClaim` 顶替：它删键会让「之前领过 N 次」一起丢掉。</para></summary>
        public void SetClaimCount(int level, int count)
        {
            if (count <= 0) _claims.Remove(level.ToString());
            else _claims[level.ToString()] = count;
        }

        /// <summary><see cref="Score"/> 的**逆运算**（`score &lt; 0 ⇒ spentScore += score；否则 totalScore -= score`）。
        /// <para>唯一的用户 = `PurchaseLevelUp` 写盘失败时的**回滚**。⛔ 别用 `Score(-amount)` 顶替 ——
        /// 那会走负数那支（写进 `spentScore`），与原文的净效果不同。</para></summary>
        public void Unscore(int amount)
        {
            if (amount < 0) SpentScore += amount;
            else TotalScore -= amount;
        }
    }

    /// <summary>原版 `Everguild.LiveOps.ExpansionPassEvent : ScoringEvent&lt;ExpansionPassSave, ExpansionPassData&gt;`
    /// 的**本地等价实现**（12 个方法 + 三个属性；逐一对应 `cpp2il/Everguild.LiveOps/ExpansionPassEvent.cs`）。
    /// <para>🔴 本类**不内置任何编造的里程碑**：`Milestones` 出厂空表，数据由调用方
    /// （将来的真 LiveOps 层 / 自检注入）填。⛔ 别在构造器里塞示例档冒充原版数据。</para></summary>
    public class ExpansionPassEvent
    {
        /// <summary>= 原版 `ILiveOps.GetID()`（事件 id）。本地默认给一个稳定串（存档键 / 日志用）。</summary>
        public string Id = "ExpansionPass";

        /// <summary>= 原版 `Milestones`（`GetBaseData().ScoringMilestones` 的 `Cast&lt;ExpansionMilestone&gt;().ToArray()`）。</summary>
        public List<ExpansionMilestone> Milestones = new List<ExpansionMilestone>();

        /// <summary>= `GetBaseData()` 的那份（`ExpansionPassData`）。</summary>
        public ExpansionPassData Data = new ExpansionPassData();

        /// <summary>= `GetBaseSave()` 的那份（`ExpansionPassSave`）。</summary>
        public ExpansionPassSave Save = new ExpansionPassSave();

        /// <summary>🔶 自己设计（非原版）：`HasPremium` 的本地替身。
        /// · 这是什么：原版 `HasPremium` 读 `InventoryManager.HasItem(ExpansionPassData.PremiumItem)`
        ///   （`ExpansionPassEvent.get_HasPremium` 逐句实读）。
        /// · 为什么必须自己定：我们没有那份 `InventoryManager` 存档（`Shell/Inventory.cs` 是 C4 的**另一套**件，
        ///   不是原版那个单例），而且「买过高级档」这件事在原版是**服务器库存**。
        /// · 谁定的 + 日期：调度台 2026-10-27 裁定（P3 边界：服务器库存不接）。
        /// 取值来源 = 本地存档文件里的一个布尔（本轮**不落盘**，只在进程内有效；真接库存时换成查询那一句）。</summary>
        public bool HasPremiumLocal;

        /// <summary>构造：可选注入存档 / data。缺省 = 空档 + 空 data（= 远端数据没到时的真实形态）。</summary>
        public ExpansionPassEvent(ExpansionPassSave save = null, ExpansionPassData data = null)
        {
            Save = save ?? ExpansionPassSave.Load();
            Data = data ?? new ExpansionPassData();
        }

        // ════════════════════════════════════════════════════════════════════════════════
        //  属性（原版三个）
        // ════════════════════════════════════════════════════════════════════════════════

        /// <summary>= 原版 `PlayerScore`（`=> GetBaseSave().totalScore`）。</summary>
        public int PlayerScore { get { return Save != null ? Save.TotalScore : 0; } }

        /// <summary>= 原版 `HasPremium`（见 `HasPremiumLocal` 的边界说明）。</summary>
        public bool HasPremium { get { return HasPremiumLocal; } }

        /// <summary>= 原版 `GetBaseData()`（把基类那份**转成** `ExpansionPassData`；本地直接返回我们那份）。</summary>
        public ExpansionPassData GetBaseData() { return Data; }

        /// <summary>= 原版 `GetBaseSave()`。</summary>
        public ExpansionPassSave GetBaseSave() { return Save; }

        // ════════════════════════════════════════════════════════════════════════════════
        //  状态机
        // ════════════════════════════════════════════════════════════════════════════════

        /// <summary>= 原版 `IsMilestoneCollected(ScoringMilestone)`：
        /// `index = Milestones.IndexOf(m)`；`if (!m.IsLimited) return false;`；
        /// `return Save.RewardClaimCount(index) &gt;= m.LimitRewardQuantity;`
        /// （`ExpansionPassEvent__IsMilestoneCollected.c:29-52`：`GetIndexOf` · `IsLimited` 闸 · `a >= b` 惯用式）。
        /// <para>⚠️ `IndexOf` 用的是**引用相等**（原版 `ListExtensions.GetIndexOf` + `Equals`）⇒ 传一个「值相同但不是同一个实例」
        /// 的里程碑会得 `-1` ⇒ 读 `RewardClaimCount(-1)` = 0 ⇒ 判未领。调用方必须传**表里那一个实例**。</para></summary>
        public bool IsMilestoneCollected(ExpansionMilestone milestone)
        {
            if (milestone == null) return false;
            int index = Milestones.IndexOf(milestone);
            if (!milestone.IsLimited) return false;
            return Save.RewardClaimCount(index) >= milestone.LimitRewardQuantity;
        }

        /// <summary>= 原版 `ScoringEvent.GetState`（基类，`cpp2il/Everguild.LiveOps/ScoringEvent.cs:176-195`）：
        /// 已领 ⇒ `Collected`；否则解锁（`playerScore &gt;= scoreToUnlock`）⇒ `Unlocked`；否则 `Locked`。</summary>
        public ScoringMilestoneState GetBaseState(ExpansionMilestone milestone)
        {
            if (milestone == null) return ScoringMilestoneState.Locked;
            if (IsMilestoneCollected(milestone)) return ScoringMilestoneState.Collected;
            if (milestone.IsUnlocked(PlayerScore)) return ScoringMilestoneState.Unlocked;
            return ScoringMilestoneState.Locked;
        }

        /// <summary>= 原版 `ExpansionPassEvent.GetState`（`ExpansionPassEvent__GetState.c:16-46` / ILSpy `:721-789`）：
        /// <code>
        /// if (milestone == null) return Locked;
        /// if (PlayerScore &gt;= milestone.scoreToUnlock &amp;&amp; milestone.IsPremium &amp;&amp; !HasPremium)
        ///     return LockedBecauseNotPremium;
        /// return base.GetState(milestone);
        /// </code>
        /// ⚠️ 那个 `!HasPremium` 闸**只对 `IsPremium` 的档**生效；普通档分数够了就是 `Unlocked`。</summary>
        public ScoringMilestoneState GetState(ExpansionMilestone milestone)
        {
            if (milestone == null) return ScoringMilestoneState.Locked;
            if (PlayerScore >= milestone.ScoreToUnlock && milestone.IsPremium && !HasPremium)
                return ScoringMilestoneState.LockedBecauseNotPremium;
            return GetBaseState(milestone);
        }

        /// <summary>= 原版 `<GetCurrentMilestoneIndex>b__10_0`（`ExpansionPassEvent___GetCurrentMilestoneIndex_b__10_0.c`）：
        /// **谓词** = `PlayerScore &gt;= m.scoreToUnlock`（⛔ 不是 `GetState`，与 `Unlocked` 无关）。</summary>
        public bool IsAtOrBelowScore(ExpansionMilestone m)
        {
            if (m == null) return false;
            return PlayerScore >= m.ScoreToUnlock;
        }

        /// <summary>= 原版 `GetCurrentMilestoneIndex()`（`ExpansionPassEvent__GetCurrentMilestoneIndex.c`）：
        /// `last = Milestones.LastOrDefault(m =&gt; PlayerScore &gt;= m.scoreToUnlock)`；
        /// 找到 ⇒ `idx = IndexOf(last) + 1`，再 `clamp(idx, 0, Count - 1)`；找不到 ⇒ `0`。
        /// <para>⚠️ 是 **`idx + 1`**（→「下一个要够到的档」的下标），不是 `idx` —— 那一句是 Ghidra `:39-43` 的 `iVar1 = iVar1 + 1`。</para></summary>
        public int GetCurrentMilestoneIndex()
        {
            int count = Milestones.Count;
            if (count == 0) return 0;
            int last = -1;
            for (int i = 0; i < count; i++)
                if (Milestones[i] != null && PlayerScore >= Milestones[i].ScoreToUnlock) last = i;
            if (last < 0) return 0;
            int idx = last + 1;
            if (idx < 0) return 0;
            int max = count - 1;
            return idx > max ? max : idx;
        }

        /// <summary>= 原版 `GetCurrentMilestone()` = `Milestones[GetCurrentMilestoneIndex()]`（越界 ⇒ null，出声）。</summary>
        public ExpansionMilestone GetCurrentMilestone()
        {
            int idx = GetCurrentMilestoneIndex();
            if (idx < 0 || idx >= Milestones.Count)
            {
                Debug.LogWarning("[ExpansionPass] GetCurrentMilestone：下标 " + idx + " 越界（里程碑 " + Milestones.Count + " 档）⇒ null");
                return null;
            }
            return Milestones[idx];
        }

        /// <summary>= 原版 `GetScoringInfoFromCurrentMilestone()`（`ExpansionPassEvent__GetScoringInfoFromCurrentMilestone.c`）：
        /// <code>
        /// idx  = GetCurrentMilestoneIndex();
        /// cur  = Milestones[idx];
        /// if (cur == null) { LogError("Can't find the current milestone ..."); return new ScoringDataInfo(0,0,0); }
        /// prev = Milestones.ElementAtOrDefault(idx - 1);
        /// return new ScoringDataInfo(PlayerScore, prev?.scoreToUnlock ?? 0, cur.scoreToUnlock);
        /// </code>
        /// ⚠️ 第三格原版 ILSpy 印成 `0`，但同一段有一句 `Unmanaged memory load: [v58 @ rax_v5+10]`（= 读 `cur` 的 `+0x10` = `scoreToUnlock`）
        /// ⇒ 按 **`cur.scoreToUnlock`** 实现；Ghidra 侧同一函数里也能看到对 `cur` 的 0x10 取值。</summary>
        public ScoringDataInfo GetScoringInfoFromCurrentMilestone()
        {
            int idx = GetCurrentMilestoneIndex();
            if (Milestones.Count == 0)
            {
                Debug.LogWarning("[ExpansionPass] GetScoringInfoFromCurrentMilestone：里程碑表是空的"
                               + "（原版这一格来自远端 LiveOps）⇒ 返回 (0,0,0)");
                return new ScoringDataInfo(0, 0, 0);
            }
            ExpansionMilestone cur = (idx >= 0 && idx < Milestones.Count) ? Milestones[idx] : null;
            if (cur == null)
            {
                Debug.LogError("[ERROR] Can't find the current milestone for the Expansion Pass Event.");
                return new ScoringDataInfo(0, 0, 0);
            }
            ExpansionMilestone prev = (idx - 1 >= 0 && idx - 1 < Milestones.Count) ? Milestones[idx - 1] : null;
            return new ScoringDataInfo(PlayerScore, prev != null ? prev.ScoreToUnlock : 0, cur.ScoreToUnlock);
        }

        /// <summary>= 原版 `GetCostToLevelUp(ExpansionMilestone)`（`ExpansionPassEvent__GetCostToLevelUp.c`）：
        /// `cost = Math.Max(0, (int)Math.Ceiling((scoreToUnlock - PlayerScore) / XpPerGold))`，货币 = **gold**。
        /// <para>⚠️ 原版返回 `CurrencyValue(GameCurrency.gold, cost)`；我们只留 `Amount`（唯一的消费者是
        /// `levelUpContainer.SetActive(0 &lt; cost.Amount)` + `PriceDisplayButton.Setup(cost)`，两者都只要数量 + 货币图标）。</para>
        /// <para>🔴 `XpPerGold &lt;= 0` ⇒ **除数非法**（原版会得到 `Infinity`/`NaN`）。我们出声并返回 0 ——
        /// ⛔ 不静默编一个价（红线：不许静默失败）。</para></summary>
        public int GetCostToLevelUp(ExpansionMilestone currentMilestone)
        {
            if (currentMilestone == null) return 0;
            int perGold = Data != null ? Data.XpPerGold : 0;
            if (perGold <= 0)
            {
                Debug.LogWarning("[ExpansionPass] GetCostToLevelUp：`XpPerGold` = " + perGold
                               + "（非法除数；原版这一格来自远端 LiveOps）⇒ 升级价按 0 处理（**不编价**）");
                return 0;
            }
            double need = currentMilestone.ScoreToUnlock - PlayerScore;
            int cost = (int)Math.Ceiling(need / (double)perGold);
            return Math.Max(0, cost);
        }

        // ════════════════════════════════════════════════════════════════════════════════
        //  写
        // ════════════════════════════════════════════════════════════════════════════════

        /// <summary>= 原版 `Score(int amount)`（`ExpansionPassEvent__Score.c` / ILSpy `:1151-1166`）：
        /// 负数 ⇒ `-amount` 走 spent 那一路；非负 ⇒ `+amount` 走 total 那一路（都落在 <see cref="ExpansionPassSave.Score"/>）。
        /// <para>本地多一步：写完落盘（原版由服务器存）。</para>
        /// <para>🆕 **2026-10-31（`B104_P3`）**：落盘那一句的返回值**原来被丢掉了** —— 写失败也当成功（静默）。
        /// 现在走 <see cref="PersistAfterMutation"/>：真写不下去 ⇒ **当场撤销这次加减分**并出声。</para></summary>
        /// <returns>这一改算不算成功（`false` 只在「落盘门开着却写失败」时出现；门关着 = 设计上的空操作，算成功）。</returns>
        public bool Score(int amount)
        {
            Save.Score(amount);
            return PersistAfterMutation("Score(" + amount + ")", () => Save.Unscore(amount));
        }

        /// <summary>= 原版 `PurchaseMilestone(int milestoneIndex, Action&lt;RewardInfo[]&gt; onComplete, Action&lt;string&gt; onError)`。
        /// <para>原版 = 服务器事务（`Method not found @180D620D0`，请求体没解出）；成功回调里
        /// `RewardService.Collect(result.rewards, showAnimation:true)` → `UpdateSaveGame(result.save)` → `onComplete(rewards)`；
        /// 失败回调转 `onError(error)`（`ExpansionPassEvent._003CPurchaseMilestone_003Eb__0/1` 逐句）。</para>
        /// <para>🔶 自己设计（非原版）：**本地事务**（服务器已关服，判据见文件头「本地等价物的边界」）。
        /// 校验顺序与报错文案由我们定：下标越界 / 状态不是 <c>Unlocked</c> 两档都走 `onError`（⛔ 不发奖）。
        /// 成功路 = `Save.AddClaim(index)`（+1，对应原版服务器回的 `rewardClaimCounter` 更新）→ 落盘 → `onComplete(m.Rewards)`。</para>
        /// <para>⚠️ 奖励的**实际发放**（`RewardService.Collect` = 进背包/加钱）本工程没有那条链 ⇒ 只回调奖励表，
        /// 由调用方决定怎么展示（本窗的做法 = 弹 `RewardWindow` 预览/领取窗，见 `OnRewardCollectComplete`）。</para></summary>
        public void PurchaseMilestone(int milestoneIndex, Action<CampaignData.RewardSpec[]> onComplete, Action<string> onError)
        {
            if (milestoneIndex < 0 || milestoneIndex >= Milestones.Count)
            {
                string why = "里程碑下标 " + milestoneIndex + " 越界（共 " + Milestones.Count + " 档）";
                if (onError != null) onError(why); else Debug.LogWarning("[ExpansionPass] PurchaseMilestone 失败：" + why);
                return;
            }
            ExpansionMilestone m = Milestones[milestoneIndex];
            ScoringMilestoneState st = GetState(m);
            if (st != ScoringMilestoneState.Unlocked)
            {
                string why = "里程碑 " + milestoneIndex + " 的状态是 " + st + "（只有 Unlocked 能领）";
                if (onError != null) onError(why); else Debug.LogWarning("[ExpansionPass] PurchaseMilestone 失败：" + why);
                return;
            }
            int prevClaim = Save.RewardClaimCount(milestoneIndex);   // 回滚用的**改前值**（必须在 AddClaim 之前取）
            Save.AddClaim(milestoneIndex);
            var rewards = m.RewardArray ?? Array.Empty<CampaignData.RewardSpec>();
            // 🆕 **2026-10-31（`B104_P3`）**：原来 `Save.Save()` 的返回值直接丢掉 ⇒ **写盘失败也照常 `onComplete`
            //   （= 玩家看到「已领取」，重启后却还是没领）**。现在：真写失败 ⇒ **撤销这一拍领取** + 走 `onError`，
            //   ⛔ 不回调 `onComplete`（原版这一支是服务器事务：受理失败就不发奖）。
            if (!PersistAfterMutation("PurchaseMilestone(" + milestoneIndex + ")", () => Save.SetClaimCount(milestoneIndex, prevClaim)))
            {
                string why2 = "里程碑 " + milestoneIndex + " 的领取**没能落盘**（存档写不进去）⇒ 这次不算领到";
                if (onError != null) onError(why2); else Debug.LogWarning("[ExpansionPass] PurchaseMilestone 失败：" + why2);
                return;
            }
            if (onComplete != null) onComplete(rewards);
        }

        /// <summary>= 原版 `PurchaseLevelUp(int milestoneIndex, Action onComplete, Action<string> onError)`。
        /// <para>原版 = 服务器事务（`Method not found @180D620D0`）；成功回调 `UpdateSaveGame(result.save)` + `onComplete()`。</para>
        /// <para>🔶 自己设计（非原版）：**本地事务**。因为「不做真实经济」（用户 2026-09-17 边界：资源固定 2000），
        /// 金币那一侧**不建模**；只把分数补到该档阈值（= 原版服务器那一步的净效果）。</para>
        /// <para>成功路：`cost = GetCostToLevelUp(m)`；`cost &lt;= 0` ⇒ 已经是够分或 `XpPerGold` 非法（后者已出声）⇒
        /// 不买、直接 `onError`（⛔ 不假装「买成功但什么都没变」）。否则 `Save.Score(need)`（把分补到阈值）→ 落盘 → `onComplete()`。</para></summary>
        public void PurchaseLevelUp(int milestoneIndex, Action onComplete, Action<string> onError)
        {
            if (milestoneIndex < 0 || milestoneIndex >= Milestones.Count)
            {
                string why = "里程碑下标 " + milestoneIndex + " 越界（共 " + Milestones.Count + " 档）";
                if (onError != null) onError(why); else Debug.LogWarning("[ExpansionPass] PurchaseLevelUp 失败：" + why);
                return;
            }
            ExpansionMilestone m = Milestones[milestoneIndex];
            int cost = GetCostToLevelUp(m);
            if (cost <= 0)
            {
                string why = "里程碑 " + milestoneIndex + " 的升级价是 " + cost
                           + "（已是够分，或 `XpPerGold` 非法）⇒ 不买";
                if (onError != null) onError(why); else Debug.LogWarning("[ExpansionPass] PurchaseLevelUp 失败：" + why);
                return;
            }
            int need = m.ScoreToUnlock - PlayerScore;
            if (need > 0) Save.Score(need);
            // 🆕 **2026-10-31（`B104_P3`）**：同一族 —— 写盘失败原来照样 `onComplete()`。现在撤销补分 + 走 `onError`。
            Action undo = need > 0 ? (Action)(() => Save.Unscore(need)) : null;
            if (!PersistAfterMutation("PurchaseLevelUp(" + milestoneIndex + ")", undo))
            {
                string why2 = "里程碑 " + milestoneIndex + " 的升级**没能落盘**（存档写不进去）⇒ 这次不算买到";
                if (onError != null) onError(why2); else Debug.LogWarning("[ExpansionPass] PurchaseLevelUp 失败：" + why2);
                return;
            }
            if (onComplete != null) onComplete();
        }

        /// <summary>= 原版 `Expire(Action onComplete)`（`ExpansionPassEvent__Expire.c` / ILSpy `:893-915`）。
        /// <para>原版三段，**逐段照实记**：① `base.Expire(onComplete)`；② `PlayerAvatarDataManager.Instance.RefreshExpiredBorders()`；
        /// ③ 一次服务器请求（`Method not found @180D620D0`，请求体没解出），它的回调
        /// （`_003CExpire_003Eb__0`，ILSpy `:29-36`）= `RewardService.Collect(rewards, showAnimation:true)` + `base.Expire(onComplete)`。</para>
        /// <para>🔶 自己设计（非原版）：本地没有服务器也没有 `PlayerAvatarDataManager` ⇒ ①②无对应物（只出声一次），
        /// ③ 的语义按账里的读法实现 —— **把未领的里程碑逐个走一遍 `PurchaseMilestone`，然后 `onComplete()`**。</para></summary>
        public void Expire(Action onComplete)
        {
            Debug.Log("[ExpansionPass] Expire：原版 ① `base.Expire` ② `PlayerAvatarDataManager.RefreshExpiredBorders` "
                    + "③ 服务器结算 —— 本机都没有；按本地等价物：把未领里程碑逐个领取（`PurchaseMilestone`）后回调。");
            for (int i = 0; i < Milestones.Count; i++)
            {
                if (GetState(Milestones[i]) != ScoringMilestoneState.Unlocked) continue;
                PurchaseMilestone(i, null, null);
            }
            if (onComplete != null) onComplete();
        }

        // ════════════════════════════════════════════════════════════════════════════════════
        //  🆕 2026-10-31（`B104_P3`）：落盘 + 失败回滚（追加在类尾）
        // ════════════════════════════════════════════════════════════════════════════════════

        /// <summary>把内存改动落到盘上；**真写失败就当场撤销**并出声，并告诉调用方「这次不算数」。
        /// <para>🔴 **两种 `false` 要分清**（判据 = <see cref="ExpansionPassSave.PersistOn"/>）：
        /// ① 落盘门**关着**（批处理 / 自检没注入 `OverrideDir`）⇒ `Save()` 恒 false，那是**设计上的空操作**
        ///    ⇒ 本方法返回 `true`、**不出声**；
        /// ② 落盘门**开着却写失败** ⇒ 真失败 ⇒ 跑 <paramref name="rollback"/> 撤销内存改动 + `LogError` + 返回 `false`。
        /// ⛔ 别把 ① 当失败（会把每条批处理自检都染红）。</para></summary>
        bool PersistAfterMutation(string what, Action rollback)
        {
            bool ok = Save.Save();
            if (ok || !ExpansionPassSave.PersistOn) return true;
            if (rollback != null) rollback();
            Debug.LogError("[ExpansionPass] `" + what + "` 的存档**没能落盘**（" + ExpansionPassSave.StorePath
                         + "）⇒ **已撤销这次改动**（原版这一步在服务器上；本地写不下去就当没做，⛔ 不假装成功）");
            return false;
        }
    }
}
