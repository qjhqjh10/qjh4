// DraftPacks.cs — 轮抽（Draft）的卡包数据 + 组卡逻辑
//
// ⚠️ **本批只做「数据 + 组卡逻辑」这一层**：⛔ 没有 UI 窗、没有匹配、没有状态机。
//    窗口/流程那一层照原版 `DraftModeState`（Intro 0 → PayScreen 2 → SelectWarlord 5 →
//    SelectPack 10 → OngoingRun 15 → Finished 20 → Collected 22，`dump.cs:126782`）另开。
//
// ==================================================================
//  判据（第一权威 = 反编译 / 解包字段，不是规则书）
// ------------------------------------------------------------------
//  · 数据源 = `d:/2/新解包资源/assets_full/bundle_draftpacks_assets_all/MonoBehaviour/` 里
//    **882 个 `PrebuiltPack` SO**；产物 = `Unity/数据/游戏数据/draft_packs.json`
//    （生成器 `工具/gen_draft_packs.py`，可 `--check` 幂等对账）。
//  · 类与字段：`PrebuiltPack : IdentifiableSO`（`d:/2/tools/il2cpp_out/dump.cs:23093`）——
//    `packName` / `packId` / `nameRefId`(882/882 全空) / `packArmy`(`CardArmy`) /
//    `packTier`(`PackTier`) / `packEnabled` / `cardIds`(List<string>)。
//  · **每个包恒 3 张卡**（882/882 实测）⇒ `CardsPerPack = 3` 是**原版真值**。
//  · 抽包口径：原版 `GetRandomPack(warlord, …)` 只收
//    `packEnabled && packArmy == warlord.cardArmy && IsValid()` ⇒ **粒度是阵营、不是战将**。
//  · 逐轮逐位确定：原版 `ShowPacks` 每次 `Random.InitState(seed + currentDraftRound + index)`
//    （`DraftModeSelectPacksStateDemo`）⇒ 第 i 个位置的包只由 `(seed, round, i)` 决定。
//    🔴 我们用 `System.Random`（工程铁律：引擎里不用 `UnityEngine.Random`）——
//    ⚠️ 因此**与原版的具体抽中结果不同**（原版是 `UnityEngine.Random`）；我们复刻的是
//    **形状与确定性**（同 seed + 同轮次 ⇒ 同结果、可回放），不是同一串包。
//  · 组卡：原版 `PickPack` 把包内 3 张塞进 `selectedCards`，
//    `GetClosedDeckByPackIds(warlordId, packIds)` 生成 **ClosedDeck**
//    （`Battle/MatchType/ClosedDeck` = 「Draft / 选秀」）。
//  · 状态存档字段：`DraftEvent.selectedWarlord / selectedPacks / selectedCards / currentReRolls / …`
//    （`dump.cs:126817`）；`CurrentDraftRound == selectedPacks.Count` ⇒ **每选 1 包算 1 轮**。
//
// ==================================================================
//  ⚠️ 规模那三个数：哪个是原版、哪个是我们挑的
// ------------------------------------------------------------------
//  · `CardsPerPack = 3`        —— **原版真值**（882/882 包实测）。
//  · `RoundsPerDraft = 4`      —— 🔴 **我们挑的，不是原版真值**。原版由服务端下发
//    （`DraftEventData.draftRounds`，`dump.cs:127940`），**本地 0 命中**（包内 / 全机都没有）。
//    依据只有用户的描述「4 轮 × 每轮 3 个包、每轮选 1 包 ⇒ 12 张」。⛔ 别把它当原版数。
//  · `PacksPerRoundOffered = 3` —— 🔴 **同上：我们按用户描述挑的**。本地唯一的旁证是
//    `DraftModeSelectPacksStateDemo` 的列表容量 **3**（S2 报告 §③），**不等于** `packsPerDraft`。
//  真值回来的条件：拿到一次服务端 `DraftEventData`（LiveOps/PlayFab 已关服 ⇒ 目前无路）。
//
// ==================================================================
//  ⚠️ 包 id 的两个坑（数据侧已标注，别在这里「修」掉）
// ------------------------------------------------------------------
//  ① **`packId` 不是唯一键**：882 个文件里只有 855 个不同 `packId`（27 组各两份，导出重复）。
//     本类给每条一个 **`key`**（首次出现 = `packId`，之后 = `packId#2`）——`excludePackIds` /
//     `pickedPackIds` 一律传 `key`（也兼容裸 `packId`，见 `Resolve`）。
//     26 组两份内容逐字相同；**1 组不同**（`TAU_EMyamoto` = `Darkstrider`(enabled) vs
//     `E’Myamoto`(disabled)）⇒ 数据按「enabled 优先」排序，正常轮抽拿到的是 `Darkstrider`。
//  ② **包内可以自重**：`GOFF_RaininStormboyz [GOF6,GOF6,GOF20]` / `LEV_SynapticRelay [TL65,TL65,TL69]` /
//     `SAU_UnendingScourge [SAU69,SAU69,SAU53]` 是**原版数据**（包内 2 张同一张卡）⇒
//     `BuildClosedDeck` **如实保留**（同一张卡在 12 张里出现两次是合法的），⛔ 别去重。
using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>轮抽的卡包表 + 组卡逻辑。数据 → `Unity/数据/游戏数据/draft_packs.json`
    /// （生成器 `工具/gen_draft_packs.py`）。⚠️ 只覆盖「数据 + 组卡」，⛔ 不含 UI / 匹配。</summary>
    public static class DraftPacks
    {
        /// <summary>`Assets/CardPresentation/Resources/draft_packs.json`。
        /// ⚠️ 生成器默认只写 `数据/游戏数据/draft_packs.json`；要运行期能读，得加 `--emit-resources`
        /// （或把那份拷进 `Resources/`）。读不到时 <see cref="Load"/> 会**报错**（不静默）。</summary>
        public const string ResourcePath = "draft_packs";

        /// <summary>本类认得的表版本。表版本比它**低** ⇒ 缺列、出声；**高** ⇒ 代码比表旧、也出声。</summary>
        public const int SupportedVersion = 1;

        // ---------------------------------------------------------------- 规模（见文件头）

        /// <summary>每包几张卡。**原版真值**（882/882 包实测恒 3）。</summary>
        public const int CardsPerPack = 3;

        /// 🔶 自己设计（非原版）：一局轮抽场数 `RoundsPerDraft = 4` · 原版 `DraftEventData.draftRounds` 服务端下发、本地 0 命中 · TODO(谁定的)｜原注：<summary>🔴 **我们挑的，不是原版真值** —— 原版 `DraftEventData.draftRounds` 由服务端下发，
        /// 本地 0 命中（判据见文件头「规模那三个数」）。⛔ 别当原版数用。</summary>
        public const int RoundsPerDraft = 4;

        /// 🔶 自己设计（非原版）：每轮可选包数 `PacksPerRoundOffered = 3` · 原版 `packsPerDraft` 服务端下发 · TODO(谁定的)｜原注：<summary>🔴 **我们挑的，不是原版真值** —— 原版 `packsPerDraft` 同上（服务端下发）。
        /// 本地唯一旁证是 `DraftModeSelectPacksStateDemo` 的列表容量 3，但那**不等于**这个数。</summary>
        public const int PacksPerRoundOffered = 3;

        /// <summary>`RoundsPerDraft × CardsPerPack` = **12 张**（一局 ClosedDeck 的张数）。
        /// ⚠️ 12 里的「4」是我们挑的 ⇒ 这个数**跟着 `RoundsPerDraft` 变**，⛔ 别在别处硬写 12。</summary>
        public const int ExpectedDeckSize = RoundsPerDraft * CardsPerPack;

        // ---------------------------------------------------------------- DTO（字段名 = JSON 键名）

        [Serializable]
        public class FileDto
        {
            public int version;
            public int count;
            public string source;
            public string note;
            public ArmyCountDto[] armyCounts;
            public TierCountDto[] tierCounts;
            public int uniqueCardCount;
            public PackDto[] packs;
        }

        [Serializable]
        public class ArmyCountDto
        {
            public int army;
            public string armyName;
            public int total;
            public int enabled;
        }

        [Serializable]
        public class TierCountDto
        {
            public int tier;
            public string tierName;
            public int total;
            public int enabled;
        }

        /// <summary>一个 `PrebuiltPack`（原版字段的直译，见文件头）。</summary>
        [Serializable]
        public class PackDto
        {
            /// <summary>**唯一键**。同一个 `packId` 的第二份带 `#2` / `#3` 后缀（见文件头坑 ①）。
            /// `RollRound` / `BuildClosedDeck` 都认它。</summary>
            public string key;
            /// <summary>原版 `packId`。⚠️ **不唯一**（27 组各两份）—— 只用来回写存档/对账原版。</summary>
            public string packId;
            /// <summary>原版 `packName`（给人看的名字，不带编号）。</summary>
            public string packName;
            /// <summary>原版 `packArmy`（`CardArmy`：10…130）。抽包的过滤键 —— **粒度是阵营，不是战将**。</summary>
            public int army;
            /// <summary>阵营名（= `ArmyIcons.Faction(army)` 写进数据的副本）。⚠️ 运行期一律用
            /// <see cref="ArmyName"/> 走 `ArmyIcons`（唯一权威），⛔ 别直接读这一列当判据。</summary>
            public string armyName;
            /// <summary>原版 `packTier`（`PackTier`：0 Common / 1 Epic / 2 Legendary）。</summary>
            public int tier;
            /// <summary>档位名（`Common` / `Epic` / `Legendary`）。</summary>
            public string tierName;
            /// <summary>原版 `packEnabled`（0/1 → bool）。`false` ⇒ 原版 `GetRandomPack` **不会抽到**。</summary>
            public bool enabled;
            /// <summary>原版 `cardIds` —— **恒 3 条**；⚠️ **允许自重**（同一张卡出现两次是原版数据）。</summary>
            public string[] cardIds;
            /// <summary>来源 SO 文件名（`Anvil.json` / `Arts of Obscuration_-341825665526468010.json`）——
            /// 追到原始解包产物的唯一线索。⛔ 不是游戏数据。</summary>
            public string srcFile;
            /// <summary>`true` = 同一个 `packId` 的**第二份**（导出重复，见文件头坑 ①）。</summary>
            public bool duplicate;
        }

        /// <summary>`GetClosedDeckByPackIds` 的产物：一局轮抽打的那副牌。</summary>
        public class ClosedDeck
        {
            /// <summary>这副牌挂在哪位战将身上（原版 `GetClosedDeckByPackIds(warlordId, …)` 的第一个参数）。</summary>
            public int warlordId;
            /// <summary>摊平后的卡 id，**按选包顺序、包内按 `cardIds` 原序**。
            /// 长度 = 包数 × <see cref="CardsPerPack"/>（4 包 ⇒ 12）。**允许重复**（包内自重）。</summary>
            public string[] cardIds;
            /// <summary>每个包的唯一键（<see cref="PackDto.key"/>），与 <see cref="packIds"/> 一一对应。</summary>
            public string[] packKeys;
            /// <summary>每个包的原版 `packId`（回写存档 / 对账原版用）。</summary>
            public string[] packIds;
        }

        // ---------------------------------------------------------------- 载入

        static List<PackDto> _cache;
        static bool _loaded;

        /// <summary>数据读到了没有（读不到会**出声**，不静默）。</summary>
        public static bool Available { get { Load(); return _cache != null && _cache.Count > 0; } }

        /// <summary>全部 882 个包（按数据文件顺序 = `(army, packId, enabled 优先, srcFile)`）。
        /// ⚠️ 返回**只读视图**的习惯：本类不改它；调用方要改请自己 `new List(…)`。</summary>
        public static IList<PackDto> All
        {
            get { Load(); return _cache != null ? (IList<PackDto>)_cache : new PackDto[0]; }
        }

        /// <summary>包总数（882）。读不到 = 0。</summary>
        public static int Count { get { return All.Count; } }

        /// <summary>丢掉缓存 —— 编辑器里改完数据、或自检要重读时用。</summary>
        public static void InvalidateCache() { _cache = null; _loaded = false; }

        /// <summary>自检用：直接喂一份表（绕开 `Resources`）。⚠️ 用完请调 <see cref="InvalidateCache"/>。</summary>
        public static void SetPacksForTest(IList<PackDto> packs)
        {
            _cache = packs == null ? null : new List<PackDto>(packs);
            _loaded = true;
        }

        static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            var ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null)
            {
                Debug.LogError("[Draft] 读不到 `Resources/" + ResourcePath + ".json` —— "
                             + "跑 `python 工具/gen_draft_packs.py --write --emit-resources`"
                             + "（轮抽的包池会是空的）。");
                return;
            }
            _cache = Parse(ta.text);
        }

        /// <summary>解析一份表（**纯函数**：不碰缓存）。结构/版本问题会**出声**。</summary>
        public static List<PackDto> Parse(string json)
        {
            var list = new List<PackDto>();
            if (string.IsNullOrEmpty(json)) return list;

            FileDto dto;
            try { dto = JsonUtility.FromJson<FileDto>(json); }
            catch (Exception e)
            {
                Debug.LogError("[Draft] `" + ResourcePath + ".json` 解析失败：" + e.Message);
                return list;
            }
            if (dto == null || dto.packs == null)
            {
                Debug.LogError("[Draft] `" + ResourcePath + ".json` 里没有 `packs` 一列 —— 重跑生成器");
                return list;
            }

            // ---------------- 版本守卫（照 `CardDatabase.Parse` 的口径：出声、不静默兜底）----------------
            if (dto.version < SupportedVersion)
                Debug.LogWarning("[Draft] 卡包表 version=" + dto.version + " < " + SupportedVersion
                               + " —— 缺列（至少 `key` / `duplicate` 是后加的）⇒ "
                               + "同 `packId` 的重复包会退化成「一条都认不出 key」。跑一下 gen_draft_packs.py 重生成。");
            if (dto.version > SupportedVersion)
                Debug.LogWarning("[Draft] 卡包表 version=" + dto.version + " > " + SupportedVersion
                               + " —— **表比代码新**：本类不认的新列会被 `JsonUtility` 静默丢掉。先升 DraftPacks。");
            if (dto.count != dto.packs.Length)
                Debug.LogError("[Draft] 卡包表的 `count`=" + dto.count + " 与 `packs.Length`="
                             + dto.packs.Length + " 不符 —— 表被改坏了（重跑生成器，别手改）。");
            if (dto.packs.Length == 0)
                Debug.LogError("[Draft] 卡包表里一个包都没有 —— 重跑生成器");

            int badLen = 0, badKey = 0, badArmy = 0;
            for (int i = 0; i < dto.packs.Length; i++)
            {
                var p = dto.packs[i];
                if (p == null) continue;
                if (string.IsNullOrEmpty(p.key)) badKey++;
                if (p.cardIds == null || p.cardIds.Length != CardsPerPack) badLen++;
                if (ArmyIcons.IsNeutral(p.army)) badArmy++;
                list.Add(p);
            }
            if (badKey > 0)
                Debug.LogError("[Draft] " + badKey + " 个包没有 `key` —— `RollRound` / `BuildClosedDeck` 都靠它，重跑生成器");
            if (badLen > 0)
                // 🔴 不许截断/补齐：那会把「数据坏了」变成「数据能跑」
                Debug.LogError("[Draft] " + badLen + " 个包的 `cardIds` 不是 " + CardsPerPack + " 条"
                             + " —— ⛔ 本类**不补齐、不截断**，这些包会被抽到但组卡时如实少/多出卡");
            if (badArmy > 0)
                Debug.LogError("[Draft] " + badArmy + " 个包的 `army` = 0（Neutral）—— 原版 882 个包全是 10…130");
            return list;
        }

        // ---------------------------------------------------------------- 查询

        /// <summary>`CardArmy` 值 → 阵营名。唯一权威 = <see cref="ArmyIcons.Faction"/>（⛔ 不读数据里那一列）。
        /// 认不出 ⇒ `""`。</summary>
        public static string ArmyName(int army)
        {
            var n = ArmyIcons.Faction(army);
            return n ?? "";
        }

        /// <summary>原版 `PackTier` 值 → 名字。认不出 ⇒ `""`。</summary>
        public static string TierName(int tier)
        {
            switch (tier)
            {
                case 0: return "Common";
                case 1: return "Epic";
                case 2: return "Legendary";
                default: return "";
            }
        }

        /// <summary>某个阵营的包（**顺序 = 数据文件顺序**，确定性靠它）。
        /// ⚠️ **按阵营筛，不是按战将**（原版 `GetRandomPack` 比的是 `packArmy`）——
        /// 同一阵营的所有战将拿到的是同一池包。</summary>
        /// <param name="enabledOnly">`true` = 只要 `enabled` 的（= 原版 `GetRandomPack` 的口径，默认）。</param>
        public static List<PackDto> PacksForArmy(int armyId, bool enabledOnly = true)
        {
            var outp = new List<PackDto>();
            var all = All;
            for (int i = 0; i < all.Count; i++)
            {
                var p = all[i];
                if (p == null || p.army != armyId) continue;
                if (enabledOnly && !p.enabled) continue;
                outp.Add(p);
            }
            return outp;
        }

        /// <summary>`key` 或裸 `packId` → 包。认不出 ⇒ `null`。
        /// ⚠️ 先比 `key`（精确、唯一），再退回 `packId`（那 27 组会命中**数据序第一条** = enabled 优先）。</summary>
        public static PackDto Resolve(string idOrKey)
        {
            if (string.IsNullOrEmpty(idOrKey)) return null;
            var all = All;
            for (int i = 0; i < all.Count; i++)
                if (all[i] != null && all[i].key == idOrKey) return all[i];
            for (int i = 0; i < all.Count; i++)
                if (all[i] != null && all[i].packId == idOrKey) return all[i];
            return null;
        }

        // ---------------------------------------------------------------- 抽包（确定性）

        /// <summary>某一轮给出的 <paramref name="count"/> 个包。**逐轮逐位确定**：
        /// 第 i 个位置的包只由 `(seed, round, i)` 决定（照原版 `Random.InitState(seed + currentDraftRound + index)`）
        /// ⇒ 同 seed + 同轮次 ⇒ 逐位相同；换 seed ⇒ 至少一位不同。
        ///
        /// <para>🔴 **不用 `UnityEngine.Random`**（工程铁律：引擎里只用 `System.Random`）——
        /// 所以抽中的**具体包**与原版不同，复刻的是形状与可复现性，不是同一串包。</para>
        ///
        /// <para>候选池 = <see cref="PacksForArmy"/>(armyId, enabledOnly:true)，**同一轮里同一个 `packId`
        /// 只出现一次**（那 27 组导出重复不会让某个包权重翻倍），再按 <paramref name="excludePackIds"/>
        /// 剔掉已经选过的（`key` 或裸 `packId` 都认）。</para>
        ///
        /// <para>⚠️ 候选不够 `count` 个时**如实少给**（返回的条数 &lt; count），不重复凑数。</para></summary>
        /// <param name="seed">本局的种子（由开局/联机同步决定，**必须可复现**）。</param>
        /// <param name="round">第几轮。**0 起**（原版 `CurrentDraftRound = selectedPacks.Count`，也是 0 起）。</param>
        /// <param name="excludePackIds">已选过的包（`key` 或裸 `packId`）。可 `null`。</param>
        /// <param name="cardPool">查卡用的卡池（可 `null` = 用 <see cref="CardPool"/>）。
        /// 🔴 只为**离线夹具/自检造负例**留的口（见 <see cref="ResolvedCardCount(string[], IList{CardDef})"/>）；
        /// 产品路径一律传 `null`。</param>
        public static List<PackDto> RollRound(int seed, int round, int armyId, int count,
                                             ICollection<string> excludePackIds = null,
                                             IList<CardDef> cardPool = null)
        {
            var cand = new List<PackDto>();
            var seenPackId = new HashSet<string>(StringComparer.Ordinal);
            var pool = PacksForArmy(armyId, true);
            for (int i = 0; i < pool.Count; i++)
            {
                var p = pool[i];
                if (p == null || string.IsNullOrEmpty(p.packId)) continue;
                if (!seenPackId.Add(p.packId)) continue;          // 同 packId 只留第一条（数据已 enabled 优先）
                if (excludePackIds != null
                    && (excludePackIds.Contains(p.key) || excludePackIds.Contains(p.packId))) continue;
                // 🔴 2026-10-27（`B9_W23DRAFT` · 账 `卡组:290` 的**调用点**）：
                //   原版那条活过滤是三截 `packEnabled && packArmy == warlord.cardArmy && pack.IsValid()`
                //   （`Everguild.LiveOps/DraftEventBase.cs:378`）。前两截 = 上面的 `PacksForArmy(…, enabledOnly: true)`，
                //   第三截就是这里。`IsValid()` 的条件① = `GetCardLibrary().Count == cardIds.Count`
                //   （查不到的 id 原版**跳过**、不进结果表）⇒ 先用 `ResolvedCardCount` 数「查得到几张」再比条数。
                //   条件②（`IsHidden()`）的判据在远端 `FeatureConfig`、本机 0 命中 ⇒ 传 `null`（理由见
                //   `PrebuiltDecks.PackIsValid` 的 doc）。⛔ **判据只写这一处** —— 别在 `DraftModeWindow` 或
                //   `BuildPackCard` 里再判第二次（两份判据迟早不一致）。
                int resolved = ResolvedCardCount(p.cardIds, cardPool);
                if (!PrebuiltDecks.PackIsValid(p.cardIds, resolved, null))
                {
                    // ⛔ 不静默失败：现池 882/882 全 valid ⇒ 这句平时不出声；一旦出声就是「卡表/包表坏了」。
                    Debug.LogWarning("[Draft] 包 `" + p.packId + "` 过不了 `PrebuiltPack.IsValid()`：`cardIds` "
                                   + (p.cardIds == null ? 0 : p.cardIds.Length) + " 条、卡池里只查到 " + resolved
                                   + " 张 ⇒ 本轮**不放进候选池**（原版 `IsValid()` 同解）。核卡表/重跑生成器。");
                    continue;
                }
                cand.Add(p);
            }

            var outp = new List<PackDto>(count);
            for (int i = 0; i < count && cand.Count > 0; i++)
            {
                // 逐位确定：第 i 位自己一份 Random。
                // 🔴 种子的**形状**照原版（`ShowPacks` 的 `seed + currentDraftRound + index`）——
                //    即「第 (round, i) 位只由 seed/round/i 决定」。但**没有**照抄那个加法：
                //    相邻的 (round, i) 用 `seed+round+i` 会得到**同一串**种子（(0,1) 与 (1,0) 相等）⇒
                //    「换轮次结果不同」变成掷骰子（实测 1/129 的概率整轮撞上）。
                //    这里换成散列混合，**依赖关系一字不变**、碰撞概率降到可忽略。
                //    （反正 RNG 已经和原版不同：我们用 `System.Random`、原版是 `UnityEngine.Random`
                //      ⇒ 具体抽中的包本来就不一样，见文件头。）
                int slotSeed = unchecked((seed * 73856093) ^ (round * 19349663) ^ (i * 83492791));
                var rng = new System.Random(slotSeed);
                int idx = rng.Next(cand.Count);
                outp.Add(cand[idx]);
                cand.RemoveAt(idx);                              // 同一轮不重复
            }
            return outp;
        }

        // ---------------------------------------------------------------- 组卡

        /// <summary>4 个包 → 12 张的 ClosedDeck。组不出来时**报错并返回 `null`**
        /// （要自己处理错误请用 <see cref="TryBuildClosedDeck"/>）。</summary>
        public static ClosedDeck BuildClosedDeck(int warlordId, IList<string> pickedPackIds)
        {
            ClosedDeck deck;
            string err;
            if (TryBuildClosedDeck(warlordId, pickedPackIds, out deck, out err)) return deck;
            Debug.LogError("[Draft] 组不出 ClosedDeck：" + err);
            return null;
        }

        /// <summary>同 <see cref="BuildClosedDeck"/>，但把失败原因交出去（⛔ 不静默）。</summary>
        /// <param name="pickedPackIds">选中的包，**按选包顺序**（`key` 优先，裸 `packId` 也认）。</param>
        public static bool TryBuildClosedDeck(int warlordId, IList<string> pickedPackIds,
                                             out ClosedDeck deck, out string error)
        {
            deck = null;
            error = null;
            if (pickedPackIds == null || pickedPackIds.Count == 0)
            {
                error = "一个包都没选（原版至少要选 1 包；`RoundsPerDraft` = " + RoundsPerDraft + "）";
                return false;
            }

            var keys = new string[pickedPackIds.Count];
            var pids = new string[pickedPackIds.Count];
            var cards = new List<string>(pickedPackIds.Count * CardsPerPack);
            for (int i = 0; i < pickedPackIds.Count; i++)
            {
                var id = pickedPackIds[i];
                var p = Resolve(id);
                if (p == null)
                {
                    error = "第 " + (i + 1) + " 个包认不出：`" + id + "`（既不是 key 也不是 packId）";
                    return false;
                }
                // 🔴 卡数不对**不补齐、不截断** —— 如实报错，别把「数据坏了」演成「组出来了」
                if (p.cardIds == null || p.cardIds.Length != CardsPerPack)
                {
                    error = "第 " + (i + 1) + " 个包 `" + p.packId + "` 有 "
                          + (p.cardIds == null ? 0 : p.cardIds.Length) + " 张卡（原版恒 "
                          + CardsPerPack + " 张）—— 重跑生成器";
                    return false;
                }
                keys[i] = p.key;
                pids[i] = p.packId;
                for (int k = 0; k < p.cardIds.Length; k++) cards.Add(p.cardIds[k]);
            }

            deck = new ClosedDeck
            {
                warlordId = warlordId,
                cardIds = cards.ToArray(),
                packKeys = keys,
                packIds = pids,
            };
            return true;
        }

        // ---------------------------------------------------------------- 卡池（账 `卡组:290` 的调用点用）

        /// <summary>进程级卡池缓存。⚠️ `CardDatabase.Load()` **每次返回一份新的 `List` 浅拷贝**（判据 = 它在
        /// `RuleEngine/Data/CardDatabase.cs:1049-1071` 的注释）⇒ 别在逐包逐卡的循环里直接调它。</summary>
        static List<CardDef> _cardPool;

        /// <summary>本类查卡用的卡池（= `CardDatabase.Load()`，进程级缓存）。读不到 = 空表
        /// （那一路已经由 `CardDatabase.Load` **出声**，⛔ 这里不再静默兜底）。</summary>
        public static IList<CardDef> CardPool()
        {
            if (_cardPool == null) _cardPool = CardDatabase.Load();
            return _cardPool;
        }

        /// <summary>丢掉卡池缓存（编辑器改完卡表 / 自检要重读时用）。⚠️ 与 <see cref="InvalidateCache"/>
        /// （包表缓存）**是两件事**，别混。</summary>
        public static void InvalidateCardPool() { _cardPool = null; }

        /// <summary>把 <paramref name="ids"/> 逐张在卡池里查，数出**查到的张数**。
        /// = 原版 `PrebuiltPack.GetCardLibrary()` 的长度（它逐张 `FindById`，查不到的**跳过**、不进结果表），
        /// 所以「少一张」就是 `resolved &lt; ids.Length`。
        /// <para>⚠️ 空表/`null` ⇒ 0（原版 `cardIds == null` 会抛，那一路由
        /// <see cref="PrebuiltDecks.PackIsValid"/> 判 false）。</para></summary>
        public static int ResolvedCardCount(string[] ids)
        {
            return ResolvedCardCount(ids, null);
        }

        /// <summary>同上，但用**显式给的卡池**。🔴 这条是给**离线夹具/自检造负例**用的
        /// （现池 882/882 全 valid ⇒ 不造负例就验不出这条过滤）：传一份只含 2 张卡的池，
        /// 3 张卡的包就该算出 2 ⇒ `PackIsValid` 判 false。
        /// <paramref name="pool"/> = `null` ⇒ 退回 <see cref="CardPool"/>。</summary>
        public static int ResolvedCardCount(string[] ids, IList<CardDef> pool)
        {
            if (ids == null || ids.Length == 0) return 0;
            var p = pool ?? CardPool();
            int n = 0;
            for (int i = 0; i < ids.Length; i++)
                if (CardDatabase.FindById(p, ids[i]) != null) n++;
            return n;
        }
    }
}
