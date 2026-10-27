// OffensiveCards.cs — **进攻卡（= 环境效果卡）的数据入口**（§25）
//
// 数据来源：`Resources/OffensiveCards.json` —— 由 `工具/gen_offensive_cards_flat.py` 从
// **`数据/游戏数据/offensive_cards.json`**（子代理普查出来的正表）**只做形状转换**得来
// （`JsonUtility` 不支持字典，所以要摊成数组）。**判据与逐条证据全在源表里，这里一个判断都不加。**
//
// 每阵营的构成（原版 `EnviromentalEffectCardsSO` 一条 = 一个阵营）：
//   · **3 张进攻卡**（各带 `envSO` / `prefab` / `face` / `forgeLevel`）
//   · **1 张「不使用进攻卡」**（`emptyOffensiveCard`；卡名恒为 `Normal Conditions` 那一族，
//     `envSO` 为空 = 不换任何环境，只按 `blendTime` 补间环境光/雾）
//   · 一条 `defaultEnviromentalEffectVFX`（默认环境 SO）
// ⚠️ **卡名是「候选」**（`nameFrom` 标明来路）：卡引用在**远端 CCD**，本地解不出真名
//   （`cardName` 0/39）⇒ 表里给的是按「SO 名 → 贴图名」反推的候选，**别当卡面名用**。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    public static class OffensiveCards
    {
        [Serializable]
        public class Card
        {
            public int idx;             // 槽号 0/1/2（⚠️ **卡槽顺序 ≠ PnP 编号顺序**）
            public string name;         // 卡名**候选**（见文件头）
            public string nameFrom;     // 那个候选是怎么推出来的（如实标）
            public string envSO;        // 环境 SO 名（`ApplyEnvironment` 要的那一条）
            public string prefab;       // 环境 prefab 名（43 件那批）
            public string face;         // 卡面插画在原版贴图表里的名字（我们用同名的 PNG，见 `CardArt.OffensiveFace`）
            public int forgeLevel;      // 原版按锻造等级解锁（**我们全解锁**，只作记录）
            public long cardRefPathID;  // 🆕 2026-10-27（B9_E2UDW）**防御卡**的原始 PPtr PathID（**64 位**，⛔ 别用 `int`：`JsonUtility` 会静默给错值）；进攻卡不收这个字段（恒 0）
        }

        [Serializable]
        public class Army
        {
            public string army;
            public int armyId;
            public string defaultEnvSO;
            public string emptyName;    // 「不使用进攻卡」那张的卡名（`Normal Conditions` 一族）
            public string emptyFace;
            public Card[] cards;
            public Card[] defensiveCards; // 🆕 2026-10-27（B9_E2UDW）**防御卡槽**（原版 `defensiveCards(+0x30)`；账 208 的防御半边）。⚠️ `name` 恒空串（卡名解不出，见 `数据/游戏数据/defensive_cards.json` 的 `_unresolved`）
        }

        [Serializable] class File { public Army[] armies; }

        static Army[] _armies;
        static bool _tried;
        static readonly Dictionary<string, Army> _byName = new Dictionary<string, Army>();

        public static Army[] All { get { Load(); return _armies; } }
        public static bool Available { get { Load(); return _armies != null && _armies.Length > 0; } }

        /// <summary>按**我们的阵营名**取（= `CardDef.Faction`；大小写不敏感）。取不到返回 null。</summary>
        public static Army For(string faction)
        {
            Load();
            if (string.IsNullOrEmpty(faction)) return null;
            Army a;
            return _byName.TryGetValue(faction.Trim().ToLowerInvariant(), out a) ? a : null;
        }

        /// <summary>这一阵营的**可选卡列表** —— 原版 `GetEnvEffectCards` 的形状：
        /// **`[空卡, 进攻1, 进攻2, 进攻3]`**（空卡固定在**下标 0**；出处
        /// `BattleManager__GetEnvEffectCards.c:50-74` —— `shouldUseOffensive` 为真时**先** `Add(空卡)`
        /// 再 `AddRange(GetEnvCardList(...))`）。
        /// <para>🔴 **2026-10-13（`A1743`）订正**：原来这里写着「原版 `justOwnedCards:1` 会把 `forgeLevel`
        /// 超档的槽直接丢掉，**我们全解锁** ⇒ 三张都在（那是我们的落地）」—— **那笔落地已经补上了**：
        /// 闸过的那一份见 <see cref="ChoicesOwnedAt"/>（`BattleDriver.OffensiveChoicesOwned` 是它的
        /// `ForgeData` 绑定版）。**这一份 = 原版 `justOwnedCards = 0` 那一支**（**不筛**，四个调用点里的两个：
        /// `ClickChosenCardDone.c:59/:67` —— **AI 那张**），⛔ 别把它挂上闸。</para></summary>
        public static List<Card> Choices(string faction)
        {
            var outp = new List<Card>();
            var a = For(faction);
            if (a == null) return outp;
            outp.Add(new Card { idx = -1, name = a.emptyName, envSO = null, prefab = null, face = a.emptyFace,
                                nameFrom = "「不使用进攻卡」那张（原版 `emptyOffensiveCard`，没有 envSO）；"
                                         + "**原版把它放在下标 0**" });
            if (a.cards != null) foreach (var c in a.cards) outp.Add(c);
            return outp;
        }

        /// <summary>🆕 2026-10-13（`A1743`）：**闸过之后的进攻池** —— 原版
        /// `GetEnvCardList(so, useArmy, shouldUseOffensive: !=0, justOwnedCards: !=0, currentForgeLevelObtained: level)`
        /// 的**进攻分支**（`EnviromentalEffectCardsSO__GetEnvCardList.c:87`；**防御分支同式在 `:60`** ——
        /// 两处是**同一个函数的两条路**，语义一字不差）。
        /// <para>三条与原版对齐的形状：① **「不使用进攻卡」那张（`idx &lt; 0`）恒在最前、恒不过闸**
        ///   —— 原版是先 `Add(空卡)` 再 `AddRange(GetEnvCardList(...))` 两句，`justOwnedCards` **看不看它**；
        ///   ② 门槛 = **`forgeLevel &lt;= level`**（**丢槽、不占位**，比较是 `&lt;=`）；
        ///   ③ **认不出的卡留下 + 出声**（`forgeLevel &lt;= 0` 那一支）——「丢它」与「给它编个等级」都是猜，
        ///   本仓红线是「认不出要说出来」。</para>
        /// <para>⚠️ **等级那套数与防御侧【不是一套】**（⛔ 别抄 `1/20/40`）：进攻侧 SO 实读
        ///   = **Ultramarines `4/25/40`**、**其余 12 个阵营 `4/25/45`**；防御侧才是 13 个阵营**全同** `1/20/40`。
        ///   这些值随卡带着（`Resources/OffensiveCards.json` 的 `cards[].forgeLevel`），**已用 PathID 逐卡核过 13/13**
        ///   与原版 SO 一模一样（含槽序）。</para>
        /// <para>⚠️ `level` 是**形参**（不在这里读 `ForgeData`）—— 这样自检能把 4 / 25 / 40(45) 三个边界逐档钉住。</para></summary>
        public static List<Card> ChoicesOwnedAt(string faction, int level)
        {
            var all = Choices(faction);
            var outp = new List<Card>(all.Count);
            foreach (var c in all)
            {
                if (c == null) continue;
                if (IsEmpty(c)) { outp.Add(c); continue; }        // ① 空卡恒不过闸（原版两句分开写）
                int need = c.forgeLevel;
                if (need <= 0)                                     // ③ 认不出（原版最小档是 4 ⇒ 0/负都不是真值）
                {
                    Debug.LogWarning($"[OffensiveCards] 进攻卡槽 {c.idx}（{faction}·「{c.name}」）的 `forgeLevel` = {need}"
                                   + " ⇒ **这道闸不筛它**（⛔ 不猜一个等级给它）。判据 → 原版 SO "
                                   + "`EnvironmentalEffectCardsSO.json` 的 `offensiveCards[].forgeLevel`。");
                    outp.Add(c);
                    continue;
                }
                if (need <= level) outp.Add(c);                    // ② 原版 `:87`：`forgeLevel <= currentForgeLevelObtained`
            }
            return outp;
        }

        /// <summary>这张是不是「不使用进攻卡」那张（原版 `isEmptyOffensiveCard` 判的就是它）。</summary>
        public static bool IsEmpty(Card c) { return c != null && c.idx < 0; }

        static void Load()
        {
            if (_tried) return;
            _tried = true;
            var ta = Resources.Load<TextAsset>("OffensiveCards");
            if (ta == null)
            {
                Debug.LogWarning("[OffensiveCards] 读不到 `Resources/OffensiveCards.json` ⇒ 进攻卡这一条链不可用"
                               + "（跑 `工具/gen_offensive_cards_flat.py` 生成它）");
                return;
            }
            var f = JsonUtility.FromJson<File>(ta.text);
            if (f == null || f.armies == null || f.armies.Length == 0)
            {
                Debug.LogWarning("[OffensiveCards] `OffensiveCards.json` 解出来是空的 —— 不静默");
                return;
            }
            _armies = f.armies;
            foreach (var a in _armies)
                if (!string.IsNullOrEmpty(a.army)) _byName[a.army.Trim().ToLowerInvariant()] = a;
            Debug.Log($"[OffensiveCards] {_armies.Length} 个阵营 · "
                    + $"{Count()} 个卡槽（+ 每阵营 1 张「不使用进攻卡」）");
        }

        static int Count()
        {
            int n = 0;
            if (_armies != null) foreach (var a in _armies) n += a.cards != null ? a.cards.Length : 0;
            return n;
        }

        // ==================================================================
        //  🆕 2026-10-27（`B9_E2UDW`）：**轮抽**那一档的环境卡可用集合（账 208 / J-11）
        // ==================================================================

        /// <summary>
        /// 原版 **`EnviromentalEffectCardsSO.GetEnvCardListDraft(CardArmy useArmy, bool shouldUseOffensive, int currentDraftVictories)`**
        /// —— 轮抽模式下「这一轮能拿到哪几张环境卡」。
        ///
        /// <para>判据（🥇 `decomp_full/EnviromentalEffectCardsSO__GetEnvCardListDraft.c:1-120`；逐条整理见
        /// `判据/B9_特效C_施工单.tsv` 行 208）：
        /// ① 新建空表；② **`currentDraftVictories < 3 && shouldUseOffensive` ⇒ 直接返回空表**；
        /// ③ 在 `enviromentalEffects(+0x18)` 里找 `army(+0x10) == useArmy` 那条（找不到 ⇒ 空表）；
        /// ④ **防御**（`shouldUseOffensive == 0`）：`defensiveCards(+0x30)` —— **恒加 `[0]`**、`> 5` 加 `[1]`、`> 8` 加 `[2]`；
        /// ⑤ **进攻**（`shouldUseOffensive != 0`）：`offensiveCards(+0x28)` —— `> 2` 加 `[0]`、`> 5` 加 `[1]`、`> 8` 加 `[2]`；
        /// ⑥ 返回。</para>
        ///
        /// <para>⚠️ **阈值逐格照抄（`>5` / `>8` 是严格大于，⛔ 别改成 `>=`）**；也 ⛔ **别复用
        /// <see cref="ChoicesOwnedAt"/>** —— 那个按**锻造等级**筛（= `GetEnvCardList` 的语义），
        /// 与本方法按**轮抽胜场**筛是两套闸。</para>
        ///
        /// <para>观测口径（施工单 208 的验收口）：`victories = 0/2/3/6/9` ⇒ 防御可用卡数 **1/1/1/2/3**、
        /// 进攻 **0/0/1/2/3**（数据现读 = 每阵营 3 槽，13 阵营全一致）。</para>
        ///
        /// <para>@return 可用卡（**不含**「不使用进攻卡」那张：原版 `…Draft` 只往表里加 `offensiveCards` /
        /// `defensiveCards` 两个数组的元素）；取不到阵营 / 未解锁 ⇒ **空表**（不是 null）。</para>
        /// </summary>
        public static List<Card> ChoicesForDraft(string faction, bool offensive, int victories)
        {
            var outp = new List<Card>();
            if (offensive && victories < 3) return outp;        // ②
            var a = For(faction);                               // ③
            if (a == null) return outp;
            if (!offensive)
            {
                // ④ 防御：恒给 [0]，>5 给 [1]，>8 给 [2]
                if (a.defensiveCards == null)
                {
                    if (!_warnedNoDefensive)
                    {
                        _warnedNoDefensive = true;
                        Debug.LogWarning("[OffensiveCards] `OffensiveCards.json` 里这个阵营**没有 `defensiveCards`**"
                                       + "（旧版产物）⇒ 轮抽的防御卡集合恒空。修法：重跑 "
                                       + "`工具/gen_defensive_cards.py --apply` 再跑 `工具/gen_offensive_cards_flat.py`。");
                    }
                    return outp;
                }
                AddSlot(outp, a.defensiveCards, 0);
                if (victories > 5) AddSlot(outp, a.defensiveCards, 1);
                if (victories > 8) AddSlot(outp, a.defensiveCards, 2);
                return outp;
            }
            // ⑤ 进攻：>2 给 [0]，>5 给 [1]，>8 给 [2]
            if (victories > 2) AddSlot(outp, a.cards, 0);
            if (victories > 5) AddSlot(outp, a.cards, 1);
            if (victories > 8) AddSlot(outp, a.cards, 2);
            return outp;
        }

        static void AddSlot(List<Card> outp, Card[] arr, int i)
        {
            if (arr == null || i < 0 || i >= arr.Length || arr[i] == null) return;
            outp.Add(arr[i]);
        }

        static bool _warnedNoDefensive;

        /// <summary>自检口：把「阵营 × 胜场」的可用卡数打成一张表（供自检宿主断言）。
        /// <para>口径 = <see cref="ChoicesForDraft"/> **同一份**规则（⛔ 别在自检里另写阈值）。
        /// 期望（施工单 208）：防御 `victories = 0/2/3/6/9` ⇒ **1/1/1/2/3**、进攻 ⇒ **0/0/1/2/3**。</para>
        /// <para>⚠️ 本工程**无 asmdef** ⇒ 自检宿主（`Editor/*.cs`）直接 `OffensiveCards.DumpDraftAvailabilityForTest()`。</para></summary>
        public static string DumpDraftAvailabilityForTest()
        {
            var sb = new System.Text.StringBuilder();
            var armies = All;
            if (armies == null) return "[OffensiveCards] `OffensiveCards.json` 没读到（见上面的日志）⇒ 无法出表";
            int[] vs = { 0, 2, 3, 6, 9 };
            int bad = 0;
            foreach (var a in armies)
            {
                if (a == null || string.IsNullOrEmpty(a.army)) continue;
                sb.Append(a.army).Append(": ");
                for (int i = 0; i < vs.Length; i++)
                {
                    int d = ChoicesForDraft(a.army, false, vs[i]).Count;
                    int o = ChoicesForDraft(a.army, true, vs[i]).Count;
                    sb.Append("v").Append(vs[i]).Append("=防").Append(d).Append("/攻").Append(o)
                      .Append(i + 1 < vs.Length ? " " : "");
                    int wantD = 1 + (vs[i] > 5 ? 1 : 0) + (vs[i] > 8 ? 1 : 0);
                    int wantO = (vs[i] > 2 ? 1 : 0) + (vs[i] > 5 ? 1 : 0) + (vs[i] > 8 ? 1 : 0);
                    if (!DefensiveSlotsShort(a) && d != wantD) bad++;
                    if (o != wantO) bad++;
                }
                sb.AppendLine();
            }
            sb.Append("期望：防 1/1/1/2/3 · 攻 0/0/1/2/3 ；不符格数 = ").Append(bad);
            return sb.ToString();
        }

        /// <summary>该阵营的防御槽少于 3 条（数据本身就凑不满那三档）⇒ 上面那张表按「数据的实际槽数」判，
        /// ⛔ 不把数据缺口记成逻辑错。现读 13 个阵营都是 3 槽 ⇒ 恒 false。</summary>
        static bool DefensiveSlotsShort(Army a)
        {
            return a.defensiveCards == null || a.defensiveCards.Length < 3;
        }
    }
}
