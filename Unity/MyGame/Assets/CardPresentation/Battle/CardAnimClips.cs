// CardAnimClips.cs — 逐卡「技能动画」的**播放参数**（`AnimInfo` 里那几个本地没进运行期的字段）
//
// ─────────────────────────────────────────────────────────────────────────────
// 【为什么需要这一层】
//   `card_vfx_by_card.json`（运行期那张）只带 **prefab 名 + 时长**（`VfxMap.ResolvedVfx`），
//   而原版播一拍技能动画还要三样位置参数：`startPosOption` / `endPosOption` / `pointTowardsOption`
//   与一个开关 `shouldMoveVFX`（判据 → `AnimInfo` 的字段表 `dump.cs:25205-25243`，逐档语义 → `Battle/AnimAnchors.cs`）。
//   这三样在**运行期 Resources 里没有**：
//     · `Resources/card_anim_order.json` 只收 `activeAbility`（62 张 / 99 拍，见 `CardAnimOrder` 文件头）；
//     · `CardDef.AnimTimes`（引擎数据）只有时间与 trigger 名，**没有**那三个 option；
//     · `数据/游戏数据/animinfo_lookup.json` 有，但它只覆盖 418 条、且不在 Assets 里。
//
// 【数据从哪来（2026-10-23 起）】**`Resources/cardanim_clips.json`** ——
//   由 `工具/gen_cardanim_clips.py` 从 `assets_full/bundle_*cardanims*_assets_all/MonoBehaviour/
//   <CardAnim>.json` 的 `animInfo` 烘出来（**幂等**：连跑两遍 md5 相同）。现读 **1,067 行**。
//   ⚠️ 工作副本另有一份在 `Unity/数据/游戏数据/cardanim_clips.json`（**逐字节相同**，给工具/人看）。
//   🔴 **键 = `CardAnim` 资产名（`a`），⛔ 不是 prefab 名**：同一个 prefab 名会被多张 CardAnim
//   共用、参数却不同（现读 **215 组**，例 `Trait_GainQuest` 之于 `Trait_GainQuest_self`(sp=0) 与
//   `…_enemywarlord`(sp=4)）⇒ 按 prefab 名查必错。运行期那一侧由 `VfxMap.ResolvedVfx.Anim`
//   （= `CardPick.A`）把 CardAnim 名交出来。
//
// 【取不到怎么办】**出声 + 显式降级**（⛔ 没有「代码里的兜底表」—— 两份数据迟早不一致）：
//   · `Resources/cardanim_clips.json` 载不到 / 解析失败 ⇒ `Debug.LogError` **一次**，`Loaded = false`；
//   · 每一条查不到的 CardAnim ⇒ `Debug.LogWarning`（去重、上限 16）；
//   · **降级后玩家看到什么**：技能动画那一件**照播**，但它的起点/终点/朝向都退回
//     `sp = ep = pt = 0`（`PositionOption.self` = **施法者自己那一格**）且**不位移**
//     ⇒ 本该飞向目标 / 落在敌方督军 / 落到棋盘中心的那类件，会**贴在单位身上**播完。
//     ⛔ 不会静默：控制台有 error + warning，自检断 `CardAnimClips.Loaded`。
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using RuleEngine;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>一条 `AnimInfo` 的「位置/移动」参数（判据见文件头）。</summary>
    public struct CardAnimPlacement
    {
        /// <summary>`startPosOption`（= `PositionOption` 数值）。</summary>
        public int StartPos;
        /// <summary>`endPosOption`。</summary>
        public int EndPos;
        /// <summary>`pointTowardsOption`。</summary>
        public int PointTowards;
        /// <summary>`shouldMoveVFX` —— 起→终位移的总开关（⛔ 别用 `sp != ep` 当替身）。</summary>
        public bool Move;
        /// <summary>`vfxDelayTime`（秒）。</summary>
        public float VfxDelay;
        /// <summary>`timeAtStartPos`（秒）—— `PlayAbilityBeat` 的 `ts`。</summary>
        public float Ts;
        /// <summary>`timeMoving`（秒）—— `PlayAbilityBeat` 的 `tm`。</summary>
        public float Tm;
        /// <summary>`timeAtEndPos`（秒）—— **播放不用**它；只给 `GetAnimDuration()`（= `vd+ts+tm+te`）复核用
        /// （`CardAnimClips.DurationOf`；自检拿它核 `VfxMap.SharedRangedVfxDuration` 那几条「同 prefab 名多 CardAnim」）。</summary>
        public float Te;
    }

    /// <summary>一拍要播的技能动画（= <see cref="CardAnimPlacement"/> + `VfxMap` 解出来的名字/时长）。</summary>
    public struct CardAnimClip
    {
        /// <summary>`CardAnim` 资产名（查 <see cref="CardAnimPlacement"/> 的键）。</summary>
        public string Anim;
        /// <summary>prefab 名（空 = **纯计时拍**，原版合法：`Wait_1s` 那种，只占时长不出件）。</summary>
        public string Prefab;
        /// <summary>相对事件起播的偏移（秒）= 组内 `Offset` + 自身 `vfxDelayTime`（`VfxMap.ResolvedVfx.T`）。</summary>
        public float T;
        /// <summary>这一拍演多久（秒，`AnimInfo.GetAnimDuration()`；0 = 没有逐卡时间）。</summary>
        public float Duration;
        public CardAnimPlacement Place;
        public bool IsTimingOnly { get { return string.IsNullOrEmpty(Prefab); } }
    }

    /// <summary>`VfxMap.UnitAttack` / `VfxMap.UnitDamaged` 两桶的运行期取数口。</summary>
    public static class CardAnimClips
    {
        /// <summary>`Resources` 里那张表的路径（**没有扩展名**）。生成器 → `工具/gen_cardanim_clips.py`。</summary>
        public const string ResourcePath = "cardanim_clips";

        // ---- 运行时那份 JSON 的形状（`JsonUtility` 只吃数组，不吃字典）----
        [Serializable] public class RtRow
        {
            public string a;      // CardAnim 名（**主键**）
            public string n;      // prefab 名（诊断用）
            public int sp, ep, pt, mv;
            public float vd, ts, tm, te;
        }
        [Serializable] public class RtFile
        {
            public int schema;
            public int count;
            public RtRow[] rows;
        }

        static Dictionary<string, CardAnimPlacement> _table;
        static bool _tried;
        /// <summary>表**载进来了**（false = 降级：见文件头那一段）。</summary>
        public static bool Loaded { get; private set; }
        /// <summary>表里几条（= `cardanim_clips.json` 的 `count`；载不到 ⇒ 0）。</summary>
        public static int TableCount { get { EnsureLoaded(); return _table.Count; } }
        /// <summary>自检用：因为**表里没有**而退回默认参数的次数（去重后的 key 数）。</summary>
        public static int MissingLookups { get { return _spoke.Count; } }

        static void EnsureLoaded()
        {
            if (_tried) return;
            _tried = true;
            _table = new Dictionary<string, CardAnimPlacement>(StringComparer.Ordinal);
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
            {
                Debug.LogError($"[CardAnimClips] `Resources/{ResourcePath}.json` **没载到** ⇒ 技能动画的"
                             + "位置/移动参数**全部退回默认**（sp=ep=pt=0 = 施法者自己、不位移）—— "
                             + "**显式降级**，玩家会看到「该飞向目标/落在别处的件贴在单位身上播完」。"
                             + "重跑：\"D:/2/Warpforge_tools/py312/python.exe\" d:/4/Unity/工具/gen_cardanim_clips.py");
                return;
            }
            RtFile dto;
            try { dto = JsonUtility.FromJson<RtFile>(asset.text); }
            catch (Exception e)
            {
                Debug.LogError($"[CardAnimClips] `Resources/{ResourcePath}.json` 解析失败：{e.Message} ⇒ 同上降级");
                return;
            }
            if (dto == null || dto.rows == null || dto.rows.Length == 0)
            {
                Debug.LogError($"[CardAnimClips] `Resources/{ResourcePath}.json` 是空的 / 形状不对 ⇒ 同上降级");
                return;
            }
            for (int i = 0; i < dto.rows.Length; i++)
            {
                var r = dto.rows[i];
                if (r == null || string.IsNullOrEmpty(r.a)) continue;
                _table[r.a] = new CardAnimPlacement
                {
                    StartPos = r.sp, EndPos = r.ep, PointTowards = r.pt,
                    Move = r.mv != 0, VfxDelay = r.vd, Ts = r.ts, Tm = r.tm, Te = r.te,
                };
            }
            Loaded = true;
            Debug.Log($"[CardAnimClips] 技能动画位置参数表已载入：{_table.Count} 行"
                    + $"（`Resources/{ResourcePath}.json`；键 = `CardAnim` 名 —— 同 prefab 名不同参数的 215 组各存各的）");
        }

        static readonly HashSet<string> _spoke = new HashSet<string>();
        const int SpeakCap = 16;
        static void SpeakMissing(string cardAnim)
        {
            if (_spoke.Count >= SpeakCap || !_spoke.Add(cardAnim ?? "")) return;
            Debug.LogWarning($"[CardAnimClips] 技能动画 `{cardAnim}` 的位置参数**表里没有** "
                           + "⇒ 退回「施法者自己 + 不位移」（⛔ 不拿别的拍的参数顶）。"
                           + (Loaded ? "（表载进来了，是这一条缺）" : "（**表整张没载到** —— 见上面的 error）"));
        }

        /// <summary>某条 `CardAnim` 的 `AnimInfo.GetAnimDuration()`（= `vd + ts + tm + te`）。
        /// 表里没有 ⇒ **-1**（调用方自己决定要不要出声 —— 这里不当第二个「缺件」口径）。
        /// <para>用途：`VfxMap.SharedRangedVfxDuration` 里那几条**同 prefab 名被多张 CardAnim 共用**的
        /// （现读 `BulletImpact_2shot_trail_chaos` 0.75 vs `_quick` 0.3 · `BulletImpact_1shot_trail`
        /// 0.3 / 0.75 / 0.21）—— 自检拿它证明我们取的是**正常那一张**（`Atk_…`），不是 `_quick`。</para></summary>
        public static float DurationOf(string cardAnim)
        {
            var p = PlacementOf(cardAnim);
            return p.HasValue ? (p.Value.VfxDelay + p.Value.Ts + p.Value.Tm + p.Value.Te) : -1f;
        }

        /// <summary>🆕 **2026-10-23（P1 · 演出时序 ④）：远程攻击那一件的「原版时长」
        /// = 逐件 `AnimInfo.GetAnimDuration()`（`vd + ts + tm + te`，**四字段无条件直加**）。</summary>
        ///
        /// <para>**判据（第一权威）** = `d:/2/tools/decomp_full/AnimInfo__GetAnimDuration.c`
        /// （四个字段直接相加、没有任何条件分支）；调用点
        /// `BattleManager._ResolveAnimInfoRangeAttack_d__468__MoveNext.c:111-124` 用 `WaitForSeconds(...)`
        /// 等它，**等完才** `ReceiveDamage` ⇒ **这一件演多久 = 伤害晚多久**。</para>
        ///
        /// <para>**为什么需要这一档**：`VfxMap.ChainDuration` 只覆盖两档 ——
        /// ① 逐卡 `CardDef.AnimTimes`（只收 `cardAbilities[].abilityLogic[].cardAnimations[]`）·
        /// ② 共享件表（`SharedRangedVfxDuration` 的 7 个 prefab 名）。
        /// 而逐卡远程件大多挂在卡 SO 的**顶层** `customRangedAttackParticles` 上、**不进 `AnimTimes`**
        /// ⇒ 现读 **441 个（卡,件）组合**两档都查不到、落回 `EventTiming.RangedVfxFallback` 0.75
        /// （其中 **265 个真值 ≠ 0.75**：186 早 / 79 晚，极端 **−2.85 s** —— Exorcist 真值 3.6
        /// vs 我们 0.75）。本条补的就是**第三档**：按 `ResolvedVfx.Anim`（CardAnim 名）查本表。
        /// 🔴 **⛔ 别按 prefab 名查** —— 现读 **215 组**同名不同参（见文件头）。</para>
        ///
        /// <para>**返回值契约（与 `VfxMap.ChainDuration` 不同，注意）**：
        /// **≥ 0 = 真值**（**`0` 也是真值** —— 现读 4 张 Tau 无人机共用的
        /// `Atk_BulletImpact_Tau_Pulse Drone Dual` 四字段**全是 0**，原版那一下就是 `WaitForSeconds(0)`）；
        /// **&lt; 0 = 判据为空**（那几件的 CardAnim 不在表里 / 有一件没有 CardAnim 名 ⇒ 调用方按老口径兜底并出声）。
        /// ⛔ 别把「0」当「查不到」用 —— 那 4 张卡会被无端推迟 0.75 s。</para>
        ///
        /// <para>⚠️ **不吃 `rng`**：与 `VfxMap.ChainDuration` 同一条口径（队尾是**纯函数**、只按「按序全播」
        /// 那条路算）。⛔ 别把生产的 `randomAbilityChoice` 随机源传进来 —— 那会**多抽一次**、
        /// 把后面真正要播的那一组抽偏。</para></summary>
        public static float RangedChainDuration(string evt, string faction, string cardIdOrName,
                                               IReadOnlyList<CardDef.CardAnimTime> times)
        {
            if (string.IsNullOrEmpty(evt)) return -1f;

            // ① 前两档（逐卡 `AnimTimes` → 共享件表）：走 `VfxMap` 自己的口，⛔ 别在这儿再写一份 join。
            float d = VfxMap.ChainDuration(evt, faction, cardIdOrName, times);
            if (d > 0f) return d;

            // ② 第三档：按 **CardAnim 名**查本表（`vd+ts+tm+te`）。
            //    终点 = `T`（组内起点偏移，单候选恒 0）+ 该件自己的时长 —— 与 `ChainDuration` 的
            //    `max(T + Duration)` 同形（原版队列一条一条等完才弹下一条）。
            var items = VfxMap.ResolveAllTimed(evt, faction, cardIdOrName, times);
            if (items == null || items.Length == 0) return -1f;
            float end = 0f;
            int known = 0;
            for (int i = 0; i < items.Length; i++)
            {
                float a = DurationOf(items[i].Anim);     // -1 = 表里没有那条 CardAnim
                if (a < 0f) return -1f;                  // 有一件判据为空 ⇒ 整条链的收尾也是空的，⛔ 别拿半条当真值
                known++;
                float e2 = Mathf.Max(0f, items[i].T) + a;
                if (e2 > end) end = e2;
            }
            if (known == 0) return -1f;
            ChainTruthHits++;
            return end;
        }

        /// <summary>🆕 2026-10-23（P1）：<see cref="RangedChainDuration"/> 的**第三档真的被用到**的次数
        /// （自检口 —— 用来证明「不是前两档顺手算出来的」，也证明它没退回常数）。</summary>
        public static int ChainTruthHits { get; private set; }

        /// <summary>某个 `CardAnim` 的位置参数（查不到 ⇒ `null` **并出声一次**）。</summary>
        public static CardAnimPlacement? PlacementOf(string cardAnim)
        {
            if (string.IsNullOrEmpty(cardAnim)) return null;
            EnsureLoaded();
            CardAnimPlacement p;
            if (_table.TryGetValue(cardAnim, out p)) return p;
            SpeakMissing(cardAnim);
            return null;
        }

        /// <summary>一张卡在某个桶里要播的那些拍（**按原版 `abilityLogic` 顺序**）。
        /// <paramref name="times"/> = 那张卡的 `CardDef.AnimTimes`（null = 没有逐候选时间）。
        /// <para>⚠️ **只查逐卡那一档**（`perCardOnly: true`）：这两桶原版没有通用件。</para>
        /// <para>⚠️ 没有候选 ⇒ **空数组**（调用方直接 for，别判 null）。</para></summary>
        public static CardAnimClip[] Resolve(string evt, string faction, string cardIdOrName,
                                             IReadOnlyList<CardDef.CardAnimTime> times)
        {
            if (string.IsNullOrEmpty(evt)) return NoClips;
            var items = VfxMap.ResolveAllTimed(evt, faction, cardIdOrName, times,
                                               null, perCardOnly: true);
            if (items.Length == 0) return NoClips;

            var buf = new CardAnimClip[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                var it = items[i];
                var place = PlacementOf(it.Anim);
                buf[i] = new CardAnimClip
                {
                    Anim = it.Anim,
                    Prefab = it.Name,
                    T = it.T,
                    Duration = it.Duration,
                    // 查不到 ⇒ 出声（PlacementOf 里）后按「自己 + 不移动」（显式降级，见文件头）
                    Place = place ?? new CardAnimPlacement(),
                };
            }
            return buf;
        }

        static readonly CardAnimClip[] NoClips = new CardAnimClip[0];

        // ===================== 卡体 `AnimationClip`（原版 `DoAnim` 的第一个参数）=====================
        //  🔴 **本 build 恒为 null —— 这是数据事实，不是没写**。判据（2026-10-23 现读）：
        //     `assets_full` 里含 `minionAnimationClipInfo` 的资产 **1,099** 个，`enable` 为 1 的 **0 个**；
        //     `minionClips` 非空的只有 2 个（`Atk_Gauss_Tesla` / `…_Annihilator`），而那两条的
        //     `animationClip` 都是 **null 引用**且自身 `enable` 也是 0。
        //     原版 `BattleManager._ResolveAnim_d__466__MoveNext.c:166-171` 那条分支是
        //     「`enable == 0` ⇒ **不调** `ResolveAnimAnimationClip`」⇒ 这份数据上原版的卡体 clip
        //     **一次都不会播**（铁律 11 例外①：判据是空的）。
        //  ⚠️ **⛔ 不许拿 `WarpforgeVFX/Animations/*.anim` 顶**：那 94 条全是**特效 prefab 内部**的
        //     曲线（`Vanguard Frame Animation` 绑的是 `material._EmissionColor` / `m_IsActive`），
        //     挂到卡体上**什么都不会动**，那是静默错（比不播更坏）。
        //  ⇒ `Card3DAnimationController` 的 `AnimationClip` 那一路**完整实现、可用**
        //     （自检用 `BuildProbeClip` 证明），生产路径走 `CardAnimPlayback.Prefab` 那一路。
        static bool _spokeNoBodyClip;
        /// <summary>这张卡有没有卡体 `AnimationClip`。**今天恒 `null`**（原因见上面那一大段）。</summary>
        public static AnimationClip BodyClipOf(string cardIdOrName)
        {
            if (!_spokeNoBodyClip)
            {
                _spokeNoBodyClip = true;
                Debug.Log("[CardAnimClips] 卡体 `AnimationClip` 这一档**本 build 没有数据**"
                        + "（1,099 个含 `minionAnimationClipInfo` 的资产里 `enable` 全为 0，"
                        + "唯二声明了 `minionClips` 的那两条 `animationClip` 也是 null；判据见本文件那一节）"
                        + " ⇒ `DoAnim` 走「技能动画 prefab + 时长」那一路");
            }
            return null;
        }
    }
}
