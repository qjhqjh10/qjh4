// CardAnimOrder.cs — T05 第 3 步：**「逐卡逐技能的有序演出步骤」表的运行期入口**。
//
// ─────────────────────────────────────────────────────────────────────────────
// 【这张表是什么 · 判据链】
//   原版把「一个技能播哪些动画」存在卡 SO 的 `cardAnimations[]`（= 一串 `AssetReferenceTyped<CardAnim>`），
//   由 `AbilityLogic.PlayAnim` **按序 foreach 全播**（`d:/2/tools/decomp_full/AbilityLogic__PlayAnim.c`，
//   逐句读过：外层 enumerator 走的正是 `param_1 + 0x18` = `AbilityLogic.cardAnimations`，见 `R4_waitForAnim语义.md` 证据 2）。
//   ⇒ 一次「发动技能」= **N 拍**，每拍一张独立的 `CardAnim` SO，各带自己的 `AnimInfo` 参数。
//
//   我们这边**引擎不动**（`RuleEngine` 只发一条 `EvtKind.Ability`，口径 → `任务/T05` §四①），
//   由**驱动侧**按本表把它展开成 N 条演出 —— 本文件是那张表的**唯一运行期入口**。
//
// 【数据从哪来】`Resources/card_anim_order.json`（**瘦身版**，27 KB）——
//   由 `工具/gen_card_anim_order.py` 从 `Unity/数据/游戏数据/card_anim_order.json`（全量 3.5 MB）烘焙。
//   生成期**已经把 join 做完**（`steps[].ca` → `cardAnims[]` 的参数），所以这里每拍都自足，
//   运行期不再查第二张表。瘦身方案与逐字段取舍 → 那个生成器的文件头。
//
// 🔴 **只取 `activeAbility`**（**2026-10-17 主对话裁决**，判据 → 生成器文件头那一节）：
//   `EvtKind.Ability` ↔ 该卡的**主动能力**（`activeAbility`），⛔ **不是**全部 `cardAbilities`
//   （那些是 `ThisCardPlayed`/`Slay`/`TurnEnd`/… = **别的时机**，归 `EvtKind.Trigger`/`Play`）。
//   逐卡复核 **897/897 精确相等**（与真值源 `cards_1195_2022时代.json` 的
//   `activeAbility.activeAbilityLogic[].cardAnimations[].m_AssetGUID` 逐 GUID 比）。
//   ⇒ 全量件 1112 条 ability 行里只有 **109 条是 `activeAbility`** ⇒ 本表 **93 张卡 / 158 拍**（✅ 已复刻 2026-11-03 · `B128` · 判据 = `Resources/card_anim_order.json` 现读 `stats.cards = 93` / `stats.steps = 158`；⛔ 原记的 62/99 已过期）。⚠️ 本表**源仍是 2022 快照**（`cards_1195_2022时代.json`）—— 2026-11-05 换源只换了 `cardAbilities` 那半 ⇒ **SO 直读 160 拍 vs 本表 158 是既有口径差**（不是这次引入的），⛔ 别拿 SO 数对本表数
//     （🆕 2026-10-19 W：生成器改为**按卡池过滤** + 卡名取 `cards_engine.json` 那一份 —— 原先 63/100，
//      少的那 1 张是不在卡池的源卡 `66486b3f…`；它还会挡住池内同名的 `AM68`）。
//   ⚠️ 拿全表展开会把**出场/触发时机的动画播在「发动技能」那一拍上** —— **错误演示且完全静默**，比少播更坏。
//
// 【与既有件的分工 · ⛔ 别搞混】
//   · `Core/AbilityAnimDurations.cs` = **一张卡的技能总时长**（Σ GetAnimDuration），喂给
//     `EventTiming.DurationOf(EvtKind.Ability)` —— 它管「这条事件在时间线上占多久」。
//   · **本文件** = 「这几拍分别是什么、各多长、按什么锚点播」—— 管**展开**。
//     两者的「时长」是**同一个判据**（`AnimInfo.GetAnimDuration()`），但**用途不同**，别互相顶替。
//   · `Resources/card_vfx_by_card.json` = **折叠表**（一个 (卡,事件) 一个 prefab）——
//     它是「只有一件时播哪件」的兜底，⛔ **别拿它承载顺序**（铁律 6；口径 → `PREP_T05第二三步.md` §六）。
//
// 【如实标注（铁律 3 / 11）】
//   ① **键 = 卡 id 或卡名**：原版按卡名取不到 id 时生成器留了 GUID 形式的 `id`；
//      而 `BattleEvent.CardId` 装的是**英文卡名**（`CardDef.Name`）⇒ 两个键都收（同 `AbilityAnimDurations` 的做法）。
//      逐卡命中面（现读 2026-11-03）：本表 **93 张卡 / 158 拍**（= 有 `activeAbility` 且那一拍的 `ca` 解得出参数的那些）。
//      另有 **64 条 `activeAbility` 的 `ca` 是空串**（原版没绑 CardAnim）在生成期已剔除（池内口径）。
//   ② 🔴 **「一张卡有多条 `activeAbility` 行」时【全部保留】、按行序拼**（如 `Leman Russ Vanquisher` 有 2 行）
//      —— **这就是原版的做法，不是近似**。
//      🔴 **2026-10-23（R8）就地订正（铁律 5 · 判据 = 第一权威反编译）**：这里原来写
//      「原版 `AbilityLogic.PlayAnim` 只播**当前那一条** …… 按行序拼成一条是**唯一**还留着的近似」——
//      **那句话不成立**。`RawCardScript__ResolveActiveAbility.c` 逐句读（`d:/2/tools/decomp_full/`）：
//        `lVar6 = hasAlternativeAbility ? (MeetsCriteria(normalAbilityBoardCriteria) ? activeAbility(+0x260)
//                  : alternativeAbility(+0x2B8)) : activeAbility(+0x260);`
//        `… lVar6 = *(lVar6 + 0x48);                 // ActiveAbility.activeAbilityLogic (List<AbilityLogic>)`
//        `while (i < list.Count) { AbilityLogic__PlayAbility(list[i], mgr, abilityData, 400, 0); i++; }`
//      ⇒ 原版**遍历全部 `activeAbilityLogic` 行、按行序逐个 `PlayAbility`**（每行各自 `PlayAnim` 自己那份
//        `cardAnimations[]`）⇒ **行序拼接 = 逐拍等价**，单行卡与多行卡**同一条口径**。
//      ⚠️ **真正还开着的近似不在这张表，而在【选哪一份 ActiveAbility】**：
//        上面那句三元里 `hasAlternativeAbility` 为真时，原版按 `normalAbilityBoardCriteria`(+0x2B0)
//        **二选一**（`activeAbility` / `alternativeAbility`）；出哪一份今天**已能判** —— 走
//        `EventTiming.AbilityUsesAlternative`（驱动在 `BattleDriver.cs:3883` 绑到 `CardDef.UsesAlternativeAbility`），
//        本表只装 `activeAbility` 那一份、替代支时长从表里的 `alt` 取（`SW21` 普通 0.4 / 替代 0.2）。
//        ⚠️ 2026-11-02（`B120-W4b`）订正：原句「分不出是哪一份 / 卡导出没有 `normalAbilityBoardCriteria`」**已过期**。
//   ③ **0 条**解不出 guid（全量件里 `ca` 非空但 `cardAnims` 里没有的）。
//   ④ **纯延时拍**（`vfx` 为空）在本表里**0 条** —— 那 4 个 `Wait_*` 都挂在 `cardAbilities` 行上（已按 ② 剔掉）。
// ─────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>一拍（= 原版 `AbilityLogic.cardAnimations[i]` 那一张 `CardAnim`）。
    /// 字段短名与 `Resources/card_anim_order.json` 逐字对应（生成器文件头有对照表）。</summary>
    [Serializable]
    public class CardAnimBeat
    {
        /// <summary>CardAnim 资产 GUID（诊断用；生成期已按它 join 好参数）。</summary>
        public string ca;
        /// <summary>🆕 **行归属** = 这一拍属于原版 `activeAbilityLogic` 的**第几行**（下标；`-1` = 不知道）。
        /// ✅ 已复刻（2026-11-04 · B141-W43 · `B141-SAN27-ROWID` · 判据 `AbilityLogic__PlayAbility.c:262/273`）：
        /// 原版是**每一行**各一次 `GetTargets + PlayAnim` ⇒ 每一拍只播**它那一行**的目标。
        /// <para>🔴 **为什么不能用 `ca` 反查行**：`ca` 是 **CardAnim 资产 GUID**（不是一个「能力种类」号），
        /// 而且**同一张卡的不同行会引用同一个 CardAnim** —— 池内 132 张主动能力卡里 53 张多行，
        /// 其中 **12 张跨行 `ca` 重复**（`AM28` 两行都是 `ca=70` · `DA18` 都是 `ca=60` · `SOR13` 都是 `ca=30`）
        /// ⇒ 反查**不唯一**。⛔ 别在运行期按 `ca` 猜行。</para>
        /// <para>⚠️ 数据来源 = 生成器的中间结构 `_assemble_cards` 的 `logicIndex`（`工具/gen_card_anim_order.py`），
        /// 瘦身版现读 **158 拍全部带 `li`**，其中 `li=0` 148 · `li=1` 9 · `li=2` 1；**9 张多行卡**
        /// （`AM28`/`AM35`/`DA18`/`DA20`/`DA71`/`DA75`/`SW23`/`SW8`/`UM105`）。
        /// ⚠️ 它是**`activeAbility` 那个对象**的行号（本表只收 `path == 'activeAbility'`）；
        /// 替代能力（`ab = -2`）那一份不在本表里 —— 消费端必须核 `BattleEvent.AbilityRowTargetsObject`。</para></summary>
        public int li;
        /// <summary>**播哪件** = `AnimInfo.animAdressable(0x10)` → prefab 名。
        /// 🔴 **空串 = 纯延时拍**（原版 `Wait_300ms/1s/1.5s/2s` 那 4 个）—— 没有特效，**但时长照算**
        /// （原版用它两拍之间插停顿，⛔ 别当「解不出」丢掉）。</summary>
        public string vfx;
        /// <summary>`startPosOption`（0x18）—— 起锚点，喂 `BattleDriver.ResolveAnimAnchor`。</summary>
        public int sp;
        /// <summary>`endPosOption`（0x1C）—— 终锚点。🆕 **2026-10-19 起已消费**（起→终位移，见
        /// `BattleDriver.PlayAbilityBeat` / `TickAbilityVfxMoves`；判据 `CardAnimController__Update.c`）。</summary>
        public int ep;
        /// <summary>`pointTowardsOption`（0x20）—— 朝向锚点。🆕 **2026-10-19 起已消费**
        /// （原版 `CardAnimController__Initialize` → `RotateAnim`：`transform.forward = 锚点 − 自己`）。</summary>
        public int pt;
        /// <summary>`vfxDelayTime`（0x24）</summary>
        public float vd;
        /// <summary>`timeAtStartPos`（0x2C）</summary>
        public float ts;
        /// <summary>`timeMoving`（0x30）</summary>
        public float tm;
        /// <summary>`timeAtEndPos`（0x34）</summary>
        public float te;
        /// <summary>`waitAnimToApplyEffect`（0x58）—— **是否等这拍播完**（判据 → 下面 <see cref="Waits"/>）。</summary>
        public int w;

        // ── 🆕 2026-10-19（偏差 6「主动技只演一半」）：`AnimInfo` 的「VFX movement」那一组 ──
        //   判据（`d:/2/tools/il2cpp_out/dump.cs:25219-25234` 字段表 + `CardAnimController__Update.c` /
        //   `CardAnimController__Initialize.c` 逐句读）：
        //     · `shouldMoveVFX`(0x38) —— **起→终位移的总开关**；关着时 `Update` 把控制器整个 `enabled = false`。
        //     · `orientToVelocity`(0x48) —— 朝向：`Initialize` 在「`pointTowards` 非空 且 (otv==false 或 mv==true)」
        //       时调 `RotateAnim` 把 `transform.forward` 指向锚点；`Update` 里移动期间还会按**位移方向**转。
        //     · `easeCurve`(0x40) —— 位移的进度曲线（`Update` 里 `Evaluate((t−ts)/tm)` 再 clamp 到 [0,1]）。

        /// <summary>`shouldMoveVFX`（0x38）—— 起→终位移的总开关（0/1，JsonUtility 不吃 bool 以外的空值）。
        /// ⚠️ **别用「`ep != sp` 且 `tm > 0`」当替身**（2026-10-19 实测：全表 536 条里
        /// `shouldMoveVFX=1` 而有 63 条 `start==end`、另有 8 条形状像移动但开关是关的）⇒ 必须读这一位。</summary>
        public int mv;
        /// <summary>`orientToVelocity`（0x48）—— 见上面那一段（我们卡池当前 0 条为 1，机制照写）。</summary>
        public int otv;
        /// <summary>`easeCurve`(0x40) 的关键帧，**扁平存放**：线性 = `[t0,v0,t1,v1,…]`（升序）；
        /// 非线性 = 4 数格式 `[-1,t0,v0,inSlope0,outSlope0,…]`（2026-11-02 `B117-WCURVE` 起，见 `HasSlopes`）。
        /// 空 / null = 原版默认曲线 = 线性（`AnimInfo..ctor` 就是 `AnimationCurve.Linear(0,0,1,1)`）。
        /// ⚠️ 原版 `Evaluate` 带切线（Hermite）⇒ 有斜率的走 <see cref="Ease"/> 的 Hermite 分支；旧的 2 数格式逐值不变。</summary>
        public float[] ease;

        // ── 🆕 2026-10-19（W · 偏差 6 的第 1 条）：`applyMovementOffsets` / `offsetMovementAxis` ──
        //   判据（`d:/2/tools/il2cpp_out/dump.cs` 的 `OffsetMovementCurvesByAxis` 字段表 +
        //   `CardAnimController__Update.c` / `OffsetMovementCurvesByAxis__GetRandomOffsetValues.c` 逐句读）：
        //     · 三轴各一条 `AnimationCurve`（`xAxis(0x18) / yAxis(0x28) / zAxis(0x38)`，Evaluate 的参数
        //       见下 `ont`；曲线值乘一个**每次播放随机一次**的幅度）；
        //     · 随机幅度 = 每轴 `Random.Range(minMaxXOffset.x, minMaxXOffset.y)`（0x20 / 0x30 / 0x40）
        //       —— 原版用 `UnityEngine.Random`；我们按铁律「确定性只用 `System.Random(seed)`」改用
        //       开局种子派生的独立 `System.Random`（见 `BattleDriver` 的 `_vfxOffsetRng`）。
        //     · `useNonModifiedAnimTime(0x10)`：真 = 用**原始** `t01=(t−ts)/tm` 求值；假 = 用**缓动后**的 `e`。
        //   ⚠️ 🔴 **2026-10-23 就地订正（铁律 5）**：这里原来写「**数据侧还没烘**（生成器白名单外）……
        //   下面这些字段今天**全部是默认 0**」—— **不成立**：`工具/gen_card_anim_order.py` 早已烘这 11 个字段
        //   （现读 2026-11-03 `Resources/card_anim_order.json`：158 拍里 `ao=1` **4 拍**、`mv=1` 13 拍）。
        //   **错因**：写这段的时候生成器确实还没补，补上之后没回来改这句。行为本身早已生效（消费端就绪）。
        //   ⚠️ 另一条**仍然成立**的：「`orientToVelocity` 我们卡池当前 **0 条**为 1」（158 拍现读）。
        //   攻击路径（`CardAnimClips` / `cardanim_clips.json`）那一份是 2026-10-23（`新账-83`）才烘的，
        //   两份表**各自独立**（`CardAnimBeat` vs `CardAnimPlacement`），字段名相同、**别互相顶替**。
        /// <summary>`xAxis/yAxis/zAxis` 的扁平关键帧（同 <see cref="ease"/> 的 `[t0,v0,…]` 约定）。
        /// 空 = 原版默认曲线 `(0,0)→(1,0)`（= 不偏移）。</summary>
        public float[] ofx, ofy, ofz;
        /// <summary>三轴的随机幅度区间（`minMaxXOffset.x/y` 等）。<c>mn</c>=min、<c>mx</c>=max。</summary>
        public float mnx, mxx, mny, mxy, mnz, mxz;
        /// <summary>`useNonModifiedAnimTime`（0/1）—— 见上。</summary>
        public int ont;

        /// <summary>`applyMovementOffsets`（0x49）—— 这一拍要不要叠逐轴偏移。</summary>
        public int ao;

        /// <summary>这一拍在进度 <paramref name="pEased"/> / <paramref name="t01"/> 处的偏移量
        /// （= 曲线值 × 随机幅度；见上面 `offsetMovementAxis` 那段注释）。没有数据 ⇒ 零。
        /// 判据 → `CardAnimController__Update.c:60-97`（`fVar7 = ont ? t01 : e`）。
        /// ⚠️ **求值实现只此一处** = <see cref="CardAnimOrder.OffsetAt"/>（`CardAnimPlacement.OffsetAt` 也走它）。</summary>
        public Vector3 OffsetAt(float t01, float pEased, Vector3 randomAmp)
        {
            return CardAnimOrder.OffsetAt(ao, ont, ofx, ofy, ofz, t01, pEased, randomAmp);
        }

        /// <summary>这拍的位移进度曲线（`p` = 归一化时间 0~1，返回 0~1）。
        /// 判据见 <see cref="ease"/> 的注释；没有曲线 ⇒ **恒等（线性）**。转发到
        /// <see cref="CardAnimOrder.EaseAt"/>（判据只此一处 —— `BattleDriver` 的位移泵也用那一份）。</summary>
        public float Ease(float p) { return CardAnimOrder.EaseAt(ease, p); }

        /// <summary>这拍占多久 = `AnimInfo.GetAnimDuration()`（`-= vfxDelayTime + timeAtStartPos + timeMoving + timeAtEndPos`）。
        /// 判据 = `AnimInfo__GetAnimDuration.c`（四个偏移 0x24/0x2C/0x30/0x34）＋ `Core/AbilityAnimDurations.cs` 文件头。
        /// ⚠️ **不含 `startDelay`(0x28)** —— 原版的 `GetAnimDuration` 也不算它。</summary>
        public float Duration { get { return vd + ts + tm + te; } }

        /// <summary>**是否等这拍播完再走下一拍** —— 读的是**这一拍自己的** `waitAnimToApplyEffect`。
        /// 🔴 判据（`R4_waitForAnim语义.md` §一/§四）：`PlayAnim.c:274`（单目标那一支）把这一位**原样传走**、
        /// **没有任何「最后一拍」门**；`(iVar11 == targets.Count-1)` 那个 `&&` 是**多目标循环里的最后一个目标**。
        /// ⇒ 我们（一拍只播一次）**退化成** `= waitAnimToApplyEffect`。
        /// ⛔ 别再写成「只有最后一拍才等」（那句是把 `param_4`=targets 误当成 `cardAnimations`=steps）。</summary>
        public bool Waits { get { return w != 0; } }

        /// <summary>🆕 2026-11-05（`B143`）：这一拍属于原版 `cardAbilities[ai]` 的**第几条触发/出牌行**
        /// （= `CardAnimTriggerAbility.ai`）。只有在 `Resources/card_anim_order_trigger.json` 里才有意义；
        /// 主动能力那张表（`card_anim_order.json`）缺这个键 ⇒ 读出 `0`（也正好等于它唯一的那一行）。
        /// <para>⚠️ 与 <see cref="li"/> 一起才唯一确定一行：同一张卡可以有多条同 `trigger` 的行
        /// （`SAU46` 有 3 条 `ThisCardPlayed`），而 `ca` 可能在多行之间重复 ⇒ ⛔ 不能靠 `ca` 反查。</para></summary>
        public int ai;
    }

    [Serializable]
    public class CardAnimOrderCard
    {
        public string id;
        public string name;
        /// <summary>该卡的演出步骤（**已按全量件里的行序 + `steps[].i` 升序**拼好）。</summary>
        public CardAnimBeat[] steps = new CardAnimBeat[0];
    }

    [Serializable]
    public class CardAnimOrderFile
    {
        public int schema;
        public string source;
        public string note;
        public CardAnimOrderCard[] cards = new CardAnimOrderCard[0];
    }

    // ── 🆕 2026-11-05（`B143`）：**触发 / 出牌能力**那一族（`Resources/card_anim_order_trigger.json`）──
    //   判据：原版 `BattleManager.TriggerAbilities` 遍历**所有命中 trigger 的 `cardAbilities` 行** →
    //   `AbilityLogic.PlayAbility` → `PlayAnim`（每行各播自己那份 `cardAnimations[]`）。
    //   ⛔ 与上面的 `CardAnimOrderCard`（只装 `activeAbility`）**两张表、两条路**，别互相顶替。

    /// <summary>触发演出表里的一行 = 原版 `cardAbilities[ai]`。
    /// 同一张卡可以有多条同 `trigger` 的行（`SAU46` 3 条 `ThisCardPlayed`）⇒ 消费端靠 `(ai,li)` 区分。</summary>
    [Serializable]
    public class CardAnimTriggerAbility
    {
        public int trigger;
        public string triggerName;
        /// <summary>= 原版 `cardAbilities[ai]` 的下标（`-1` = 不知道）。</summary>
        public int ai;
        /// <summary>= 原版 `abilityLogic[li]` 的下标（`-1` = 不知道）。</summary>
        public int li;
        public int chosenAbility;
        public string chosenAbilityName;
        public CardAnimBeat[] steps = new CardAnimBeat[0];
    }

    [Serializable]
    public class CardAnimTriggerCard
    {
        public string id;
        public string name;
        public CardAnimTriggerAbility[] abilities = new CardAnimTriggerAbility[0];
    }

    /// <summary>生成期统计（与生成器 `stats` 逐字对应；自检用它钉总数）。</summary>
    [Serializable]
    public class CardAnimTriggerStats
    {
        public int cards;
        public int rows;
        public int steps_raw;
        public int steps;
        public int blank_guid;
        public int unresolved_guid;
        public int pure_wait;
        public int movers;
        public int offset_beats;
        public int this_card_played_rows;
        public int cards_no_rows;
        public int not_in_pool;
    }

    [Serializable]
    public class CardAnimTriggerFile
    {
        public int schema;
        public string source;
        public string filter;
        public string note;
        public CardAnimTriggerStats stats;
        public CardAnimTriggerCard[] cards = new CardAnimTriggerCard[0];
    }

    // ── 🆕 2026-11-05（`B143` · 执行写手 `W1`）：**出牌（`ThisCardPlayed` / `trigger 0`）**那一族的表 ──
    //   判据 = `AbilityLogic__PlayAnim.c:45-60`（枚举的是**这一条** `AbilityLogic.cardAnimations`）+
    //          `PlayAbility.c:273`（每条 ability 各播一次）⇒ 打出这张卡时播的 = 它 `trigger == 0` 那些行的拍。
    //   ⛔ 与 `CardAnimTriggerCard`（全部 trigger）**同源不同表**：那张按 trigger 索引、这张只装 trigger 0
    //      且**不带 trigger 字段**（`Play` 事件不带 ability 下标 ⇒ 查表只要「这张卡的全部 ThisCardPlayed 行」）。
    //   表由 `工具/gen_card_anim_order_play.py` 生成（现读 475 卡 / 579 行 / 697 拍可播、raw 703）。

    [Serializable]
    public class CardAnimPlayRow
    {
        public int ai = -1;
        public int li = -1;
        public int chosenAbility;
        public string chosenAbilityName;
        public CardAnimBeat[] steps = new CardAnimBeat[0];
    }

    [Serializable]
    public class CardAnimPlayCard
    {
        public string id;
        public string name;
        public CardAnimPlayRow[] rows = new CardAnimPlayRow[0];
    }

    /// <summary>生成期统计（与 `gen_card_anim_order_play.py` 的 `stats` 逐字对应；自检用它钉总数）。</summary>
    [Serializable]
    public class CardAnimPlayStats
    {
        public int cards;
        public int rows;
        public int steps_raw;
        public int steps;
        public int blank_guid;
        public int unresolved_guid;
        public int pure_wait;
        public int movers;
        public int offset_beats;
        public int cards_with_trigger0;
        public int cards_no_rows;
        public int not_in_pool;
    }

    [Serializable]
    public class CardAnimPlayFile
    {
        public int schema;
        public string source;
        public string filter;
        public string note;
        public CardAnimPlayStats stats;
        public CardAnimPlayCard[] cards = new CardAnimPlayCard[0];
    }

    /// <summary>「逐卡逐技能有序演出步骤」表的运行期入口。**判据链见文件头**。</summary>
    public static class CardAnimOrder
    {
        /// <summary>位移进度曲线求值（`AnimationCurve` 扁平关键帧 → 0~1）。**判据只此一处**：
        /// `CardAnimController__Update.c` 的 `Evaluate((t−timeAtStartPos)/timeMoving)` + `clamp`。
        /// ✅ **2026-11-02（`B117-WCURVE`）已改成 Hermite**（原版 `Evaluate` 带切线）；数据侧由两个生成器
        /// `_curve_flat(..., with_slopes=True)` 每键补写 `inSlope/outSlope`（4 数格式，见 <see cref="HasSlopes"/>）。
        /// 🔴 **旧行为/根因**：2026-11-03 现读订正过——本表 movers 13 条、其中 `Atk_BulletImpact_MissileToTarget_warlord`
        /// 的切线全是 0 ⇒ 原版是平滑阶跃、不是直线（按分段线性读最大偏 **0.096**）；其余 12 条切线 == 弦斜率 ⇒ 逐值等价。
        /// **旧的两数格式仍然可用**（盘上旧数据 / 别处构造的数组）⇒ 走原来的分段线性，逐值不变。</summary>
        public static float EaseAt(float[] ease, float p)
        {
            float x = Mathf.Clamp01(p);
            if (ease == null || ease.Length < 4) return x;
            if (HasSlopes(ease)) return Mathf.Clamp01(HermiteAt(ease, x));
            for (int i = 0; i + 3 < ease.Length; i += 2)
            {
                float t0 = ease[i], v0 = ease[i + 1], t1 = ease[i + 2], v1 = ease[i + 3];
                if (x <= t1)
                {
                    if (t1 - t0 <= 1e-6f) return Mathf.Clamp01(v1);
                    return Mathf.Clamp01(Mathf.Lerp(v0, v1, (x - t0) / (t1 - t0)));
                }
            }
            return Mathf.Clamp01(ease[ease.Length - 1]);
        }

        /// <summary>🆕 W：**偏移曲线**的求值 —— 与 <see cref="EaseAt"/> 同一套 Hermite/分段线性，但：
        /// ① 空曲线返回 **0**（偏移曲线的默认是「无偏移」；位移进度曲线的默认才是线性恒等）；
        /// ② 输出**不** clamp 到 [0,1]（`CurveRange(0,-1,1,1)` 允许负值 —— 原版的偏移曲线就会往两边摆）。
        /// 判据 → `CardAnimController__Update.c` 那三句 `AnimationCurve.Evaluate`（没有 clamp）。</summary>
        public static float EaseAtRaw(float[] curve, float p)
        {
            float x = Mathf.Clamp01(p);
            if (curve == null || curve.Length < 4) return 0f;
            if (HasSlopes(curve)) return HermiteAt(curve, x);
            for (int i = 0; i + 3 < curve.Length; i += 2)
            {
                float t0 = curve[i], v0 = curve[i + 1], t1 = curve[i + 2], v1 = curve[i + 3];
                if (x <= t1)
                {
                    if (t1 - t0 <= 1e-6f) return v1;
                    return Mathf.Lerp(v0, v1, (x - t0) / (t1 - t0));
                }
            }
            return curve[curve.Length - 1];
        }

        /// <summary>🆕 2026-10-23（`新账-83`）：**逐轴偏移**的求值口 —— `applyMovementOffsets` 那一位
        /// 在 `CardAnimController.Update` 里的三句（`CardAnimController__Update.c:60-97`）：
        /// <code>
        /// fVar7 = ont ? t01 : e;                                  // useNonModifiedAnimTime
        /// x += xAxis.Evaluate(fVar7) * randomAmp.x;  // 三轴各一次（曲线输出**不** clamp）
        /// </code>
        /// <para>**判据只此一处**：`CardAnimBeat.OffsetAt` 与 `CardAnimPlacement.OffsetAt`
        /// 都转发到这里（两张表的字段名相同 ⇒ 同一份求值）。</para>
        /// <para>⚠️ 曲线为空 ⇒ 该轴 0（原版默认偏移曲线 `(0,0)→(1,0)` = 不偏移）——
        /// 与 <see cref="EaseAt"/> 的空曲线「线性恒等」是**两条不同的默认**，⛔ 别混。</para></summary>
        public static Vector3 OffsetAt(int ao, int ont, float[] ofx, float[] ofy, float[] ofz,
                                       float t01, float pEased, Vector3 randomAmp)
        {
            if (ao == 0) return Vector3.zero;
            float x = ont != 0 ? t01 : pEased;
            float cx = EaseAtRaw(ofx, x);
            float cy = EaseAtRaw(ofy, x);
            float cz = EaseAtRaw(ofz, x);
            return new Vector3(cx * randomAmp.x, cy * randomAmp.y, cz * randomAmp.z);
        }

        public const string ResourcePath = "card_anim_order";

        static Dictionary<string, CardAnimOrderCard> _byName;
        static Dictionary<string, CardAnimOrderCard> _byId;
        static CardAnimOrderCard[] _all;

        /// <summary>表载进来了没有（没载 = 资源不在；**不静默**：<see cref="LastError"/> 有话说）</summary>
        public static bool Loaded { get { EnsureLoaded(); return _all != null && _all.Length > 0; } }
        /// <summary>表里几张卡</summary>
        public static int Count { get { EnsureLoaded(); return _all != null ? _all.Length : 0; } }
        public static string LastError { get; private set; }

        /// <summary>**没命中**的次数与最近一次是谁（诊断；自检盯它）。
        /// 「没命中」= 这张卡不在表里 ⇒ 调用方走**原样（一拍）**，⛔ 不硬扩。</summary>
        public static int FallbackHits { get; private set; }
        public static string LastFallbackCard { get; private set; }

        /// <summary>已经为哪几个键出过声（去重；只打一次）。</summary>
        static readonly HashSet<string> _warned = new HashSet<string>();
        /// <summary>出声上限 —— 超过就只累计数（一局里几十张卡逐条刷屏没有意义）。</summary>
        const int WarnCap = 16;

        static void EnsureLoaded()
        {
            if (_all != null) return;
            var ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null)
            {
                LastError = "`Resources/" + ResourcePath + ".json` 载不到 —— 跑一次 "
                          + "`python 工具/gen_card_anim_order.py`";
                _all = new CardAnimOrderCard[0];
                _byName = new Dictionary<string, CardAnimOrderCard>();
                _byId = new Dictionary<string, CardAnimOrderCard>();
                return;
            }
            CardAnimOrderFile f = null;
            try { f = JsonUtility.FromJson<CardAnimOrderFile>(ta.text); }
            catch (Exception e) { LastError = "`" + ResourcePath + ".json` 解析失败：" + e.Message; }
            var items = f != null && f.cards != null ? f.cards : new CardAnimOrderCard[0];
            _all = items;
            _byName = new Dictionary<string, CardAnimOrderCard>(items.Length);
            _byId = new Dictionary<string, CardAnimOrderCard>(items.Length);
            foreach (var it in items)
            {
                if (it == null) continue;
                // 重名 / 重 id 就取先到的那个（同 `AbilityAnimDurations` 的口径）
                if (!string.IsNullOrEmpty(it.name) && !_byName.ContainsKey(it.name)) _byName[it.name] = it;
                if (!string.IsNullOrEmpty(it.id) && !_byId.ContainsKey(it.id)) _byId[it.id] = it;
            }
            if (LastError == null && items.Length == 0)
                LastError = "`" + ResourcePath + ".json` 里一条都没有";
        }

        /// <summary>按**卡 id 或卡名**取这张卡的**有序演出步骤**，追加进 <paramref name="into"/>，
        /// 返回追加的条数（`0` = 这张卡不在表里 ⇒ 调用方走原样）。
        ///
        /// ⚠️ **两个键都要**（同 `AbilityAnimDurations.TryGet`）：`BattleEvent.CardId` 装的是
        /// **`CardDef.Name`（英文卡名）**，而本表的 `id` 是卡 id ⇒ 先查 id、再查卡名。
        ///
        /// ⚠️ **本方法会出声**（每张卡一次，去重 + 有上限），不必再在外面重打一遍。</summary>
        public static int TryGetBeats(string cardIdOrName, List<CardAnimBeat> into)
        {
            EnsureLoaded();
            if (into == null) return 0;
            if (string.IsNullOrEmpty(cardIdOrName)) return 0;

            CardAnimOrderCard c = null;
            if (!_byId.TryGetValue(cardIdOrName, out c) || c == null)
                if (!_byName.TryGetValue(cardIdOrName, out c)) c = null;

            if (c != null && c.steps != null && c.steps.Length > 0)
            {
                into.AddRange(c.steps);
                return c.steps.Length;
            }

            FallbackHits++;
            LastFallbackCard = cardIdOrName;
            if (Loaded && _warned.Count < WarnCap && _warned.Add(cardIdOrName))
                Debug.Log($"[Battle] 逐卡演出表里没有 `{cardIdOrName}` ⇒ 这一条 `EvtKind.Ability` 走**原样（一拍）**"
                        + "（= 旧行为：只播 `VfxMap.Ability` 那一件，⛔ 不硬扩成 N 拍）。"
                        + "原因三种：① **它只有 `cardAbilities`（别的时机）没有 `activeAbility`**（绝大多数卡 —— 这不是缺陷）"
                        + " ② 它的 `activeAbility` 那一拍 `ca` 是空串（原版没绑 CardAnim）"
                        + " ③ 它不在 2022 快照 / 不在我们卡池。");
            return 0;
        }

        /// <summary>表里全部条目（自检对账用）。</summary>
        public static IEnumerable<CardAnimOrderCard> Items { get { EnsureLoaded(); return _all; } }

        /// <summary>表里**有**这个键（id 或卡名）—— 与 <see cref="TryGetBeats"/> 分开，
        /// 是因为自检要区分「表里有这张卡」和「查表命中不了」。</summary>
        public static bool Has(string key)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(key)) return false;
            return _byId.ContainsKey(key) || _byName.ContainsKey(key);
        }

        /// <summary>自检/诊断：清零计数器（不动已载的表）。</summary>
        public static void ResetCounters()
        {
            FallbackHits = 0; LastFallbackCard = null; _warned.Clear();
        }

        // ── 🆕 2026-11-02（执行代理 `B117-WCURVE`）：带斜率的关键帧格式（Hermite） ──
        //   数据（两个生成器的 `_curve_flat(..., with_slopes=True)`）：
        //     · 旧的 2 数格式 `[t0,v0,t1,v1,…]`（**线性曲线仍然用它**，逐字节不变）；
        //     · 新的 4 数格式 `[-1, t0,v0,inSlope0,outSlope0, t1,v1,inSlope1,outSlope1, …]`
        //       —— **只在曲线非线性时**才有（线性曲线两种求值逐值相同，没必要多存 4 格）。
        //   判据（第一权威）：原版 `CardAnimController__Update.c:36-97` 的
        //     `easeCurve.Evaluate((t−timeAtStartPos)/timeMoving)` / 三轴 `Evaluate(...)` ——
        //     Unity `AnimationCurve.Evaluate` 是**分段三次 Hermite**（`weightedMode` 全 0 ⇒ 直接取
        //     `inSlope/outSlope`，`assets_full` 逐键现读：`weightedMode=0` 全表；`m_PreInfinity/PostInfinity=2`）。
        //   🔴 **首格 `-1` 是标记、不是时间**（时间永远 ≥ 0）。它把两种格式做成**长度奇偶互斥**：
        //     4 数格式 = `1 + 4n`（`% 4 == 1`），旧的 2 数格式 = `2m`（`% 4 ∈ {0,2}`）⇒ **两个集合不可能相交**，
        //     所以不需要额外版本列（`BattleDriver` 直接拿 `float[]` 调 `EaseAt`，没有地方传版本号）。
        //   ⚠️ 这里**只多做一件事**：把 `CardAnimOrder` 自己写的格式判对；⛔ 别拿它去读
        //     `minionMovementInfo` 那族曲线（它们仍是 2 数格式、消费方是 `BattleDriver.MinionCurve`）。

        /// <summary>4 数格式的首格标记（见上面那段）。</summary>
        const float SlopeTag = -1f;

        /// <summary>这个扁平数组是不是 4 数格式（每键 `t,v,inSlope,outSlope` + 首格 `-1`）。
        /// 两个条件同时成立才算：**长度 = `1+4n`**（`% 4 == 1`，与旧格式的偶数长度互斥）且首格 == <see cref="SlopeTag"/>。</summary>
        public static bool HasSlopes(float[] a)
        {
            return a != null && a.Length >= 9 && (a.Length % 4) == 1 && a[0] == SlopeTag;
        }

        /// <summary>4 数格式的 Hermite 求值（Unity `AnimationCurve.Evaluate` 的分段形状）：
        /// `h00·v0 + h10·dt·out0 + h01·v1 + h11·dt·in1`（`u = (x−t0)/dt`）。
        /// ⚠️ **切线必须乘以段长 `dt`** —— 原版的斜率单位是「每秒」，不是「每段」；漏乘 `dt` 只在
        /// `dt ≠ 1` 的曲线上才露馅（本表绝大多数 `dt = 1`，那正是它最容易静默错的地方）。
        /// 调用方：<see cref="EaseAt"/>（输出 clamp）与 <see cref="EaseAtRaw"/>（**不** clamp）。</summary>
        static float HermiteAt(float[] a, float x)
        {
            for (int o = 1; o + 6 < a.Length; o += 4)          // 每键 4 格、从第 2 格（index 1）起
            {
                float t0 = a[o], v0 = a[o + 1], out0 = a[o + 3];
                float t1 = a[o + 4], v1 = a[o + 5], in1 = a[o + 6];
                if (x <= t1)
                {
                    float dt = t1 - t0;
                    if (dt <= 1e-6f) return v1;                // 零长段（同时刻两键）⇒ 取右键值（同旧行为）
                    float u = (x - t0) / dt, u2 = u * u, u3 = u2 * u;
                    return (2f * u3 - 3f * u2 + 1f) * v0 + (u3 - 2f * u2 + u) * dt * out0
                         + (-2f * u3 + 3f * u2) * v1 + (u3 - u2) * dt * in1;
                }
            }
            return a[a.Length - 3];                            // 末键的 `value`（末键布局 t,v,in,out ⇒ out 在最后一格）
        }

        // ── 🆕 2026-11-05（`B143`）：**触发 / 出牌能力**的逐卡有序演出（`Resources/card_anim_order_trigger.json`）──
        //   判据：原版 `BattleManager.TriggerAbilities` 遍历所有命中 trigger 的 `cardAbilities` 行 →
        //   `AbilityLogic.PlayAbility` → `PlayAnim`（`B143-R2`；每行各播自己那份 `cardAnimations[]`）。
        //   ⛔ 与上面的 `TryGetBeats`（只装 `activeAbility`）**两张表、两条路**，别互相顶替。
        //   表由 `工具/gen_card_anim_order_trigger.py` 生成（2026-11-05 换源后现读 **807 卡 / 1,024 行 / 1,319 raw 拍 / 1,309 可播拍**）。
        //   ⚠️ **行数口径**：`1,024` = 「(abilityIndex, logicIndex) 组数」（本表 `abilities[]` 的粒度）；
        //   **ability 级行数是 928**（`cardAbilities[]` 条数）—— 两个数都对，只是粒度不同，⛔ 别当成谁写错了。

        public const string TriggerResourcePath = "card_anim_order_trigger";

        static CardAnimTriggerCard[] _trigAll;
        static Dictionary<string, CardAnimTriggerCard> _trigById, _trigByName;
        static bool _trigTried;
        static readonly HashSet<string> _trigWarned = new HashSet<string>();

        /// <summary>触发表载入失败的原因（载入成功 = null）。</summary>
        public static string TriggerLastError { get; private set; }
        /// <summary>触发表「没命中」次数与最近一次是谁（诊断；自检盯它）。</summary>
        public static int TriggerFallbackHits { get; private set; }
        /// <summary>最近一次没命中的键（`卡#trigger`）。</summary>
        public static string TriggerLastFallback { get; private set; }

        /// <summary>触发演出表载进来了没有。</summary>
        public static bool TriggerLoaded { get { EnsureTriggerLoaded(); return _trigAll != null && _trigAll.Length > 0; } }
        /// <summary>表里几张卡（现读 726）。</summary>
        public static int TriggerCount { get { EnsureTriggerLoaded(); return _trigAll != null ? _trigAll.Length : 0; } }
        /// <summary>表里全部条目（自检对账用）。</summary>
        public static IEnumerable<CardAnimTriggerCard> TriggerItems { get { EnsureTriggerLoaded(); return _trigAll; } }

        static void EnsureTriggerLoaded()
        {
            if (_trigTried) return;
            _trigTried = true;
            var ta = Resources.Load<TextAsset>(TriggerResourcePath);
            if (ta == null)
            {
                TriggerLastError = "`Resources/" + TriggerResourcePath + ".json` 载不到 —— 跑一次 "
                                 + "`python -I -X utf8 工具/gen_card_anim_order_trigger.py --write`";
                _trigAll = new CardAnimTriggerCard[0];
                _trigById = new Dictionary<string, CardAnimTriggerCard>();
                _trigByName = new Dictionary<string, CardAnimTriggerCard>();
                return;
            }
            CardAnimTriggerFile f = null;
            try { f = JsonUtility.FromJson<CardAnimTriggerFile>(ta.text); }
            catch (Exception e) { TriggerLastError = "`" + TriggerResourcePath + ".json` 解析失败：" + e.Message; }
            var items = f != null && f.cards != null ? f.cards : new CardAnimTriggerCard[0];
            _trigAll = items;
            _trigById = new Dictionary<string, CardAnimTriggerCard>(items.Length);
            _trigByName = new Dictionary<string, CardAnimTriggerCard>(items.Length);
            foreach (var it in items)
            {
                if (it == null) continue;
                // 重名 / 重 id 取先到的那个（同 `TryGetBeats` 的口径）
                if (!string.IsNullOrEmpty(it.id) && !_trigById.ContainsKey(it.id)) _trigById[it.id] = it;
                if (!string.IsNullOrEmpty(it.name) && !_trigByName.ContainsKey(it.name)) _trigByName[it.name] = it;
            }
            if (TriggerLastError == null && items.Length == 0)
                TriggerLastError = "`" + TriggerResourcePath + ".json` 里一条都没有";
        }

        static CardAnimTriggerCard FindTriggerCard(string cardIdOrName)
        {
            if (string.IsNullOrEmpty(cardIdOrName)) return null;
            CardAnimTriggerCard c = null;
            if (_trigById.TryGetValue(cardIdOrName, out c) && c != null) return c;
            if (_trigByName.TryGetValue(cardIdOrName, out c)) return c;
            return null;
        }

        /// <summary>按「卡 id 或卡名 + trigger」取**该 trigger 下所有行**的有序拍，追加进 <paramref name="into"/>；
        /// 返回追加条数（`0` = 没有这一行 / 这一行每一拍都是空引用）。
        /// <para>⚠️ 两个键都收（`BattleEvent.CardId` 装的是英文卡名，同 <see cref="TryGetBeats"/>）。</para></summary>
        public static int TryGetTriggerBeats(string cardIdOrName, int trigger, List<CardAnimBeat> into)
        {
            return TryGetTriggerBeats(cardIdOrName, trigger, -1, into);
        }

        /// <summary>同上，但只取 `cardAbilities[abilityIndex]` 那一行（`abilityIndex &lt; 0` = 不筛）。
        /// <para>⚠️ `abilityIndex` 只到「ability 行」；**同一条 ability 下仍可能有多条 `logic` 行**
        /// （`AM76` 的 `ai=1` 自己就有 `li=0/1/2` 三行，各 1 拍）⇒ 要唯一定位一行必须再用
        /// <see cref="TryGetTriggerBeats(string,int,int,int,List{CardAnimBeat})"/> 的 `logicIndex`；
        /// ⛔ 不能靠拍上的 `ca` 反查（跨行会重复）。</para></summary>
        public static int TryGetTriggerBeats(string cardIdOrName, int trigger, int abilityIndex, List<CardAnimBeat> into)
        {
            return TryGetTriggerBeats(cardIdOrName, trigger, abilityIndex, -1, into);
        }

        /// <summary>同上，但用 `(abilityIndex, logicIndex)` **唯一确定一行**（各自 `&lt; 0` = 该维不筛）。
        /// <para>⚠️ 为什么必须有这一档：`ai` 是 `cardAbilities[]` 的下标，它**不含** `abilityLogic[]` 的层
        /// —— 同一条 ability 可以有多条 logic 行（`AM76`：`ai=1` 的 `li=0/1/2` 各 1 拍）⇒ 只传 `ai`
        /// 会把它们一起返回（那是对的用法，但**定位不到某一行**）。</para></summary>
        public static int TryGetTriggerBeats(string cardIdOrName, int trigger, int abilityIndex, int logicIndex, List<CardAnimBeat> into)
        {
            EnsureTriggerLoaded();
            if (into == null) return 0;
            var c = FindTriggerCard(cardIdOrName);
            int n = 0, rowsMatched = 0;
            if (c != null && c.abilities != null)
            {
                for (int i = 0; i < c.abilities.Length; i++)
                {
                    var ab = c.abilities[i];
                    if (ab == null || ab.trigger != trigger) continue;
                    if (abilityIndex >= 0 && ab.ai != abilityIndex) continue;
                    if (logicIndex >= 0 && ab.li != logicIndex) continue;
                    rowsMatched++;
                    if (ab.steps != null) { into.AddRange(ab.steps); n += ab.steps.Length; }
                }
            }
            if (rowsMatched > 0) return n;      // 行在、但每拍都是空引用 ⇒ 返回 0 且**不报「没这张卡」**
            TriggerFallbackHits++;
            TriggerLastFallback = (cardIdOrName ?? "?") + "#" + trigger;
            if (TriggerLoaded && _trigWarned.Count < WarnCap && _trigWarned.Add(TriggerLastFallback))
                Debug.Log("[Battle] 触发演出表里没有 `" + cardIdOrName + "` 的 trigger " + trigger
                        + " ⇒ 这一条走**原样（不扩拍）**（⛔ 不硬扩；三种正常原因：① 这张卡没有触发/出牌能力 "
                        + "② 有这一行但每拍的 CardAnim 都是空串 ③ 它不在表里）");
            return 0;
        }

        /// <summary>这张卡在触发表里有几条该 `trigger` 的行（`0` = 没有）。自检/诊断用。</summary>
        public static int TriggerRowCount(string cardIdOrName, int trigger)
        {
            EnsureTriggerLoaded();
            var c = FindTriggerCard(cardIdOrName);
            if (c == null || c.abilities == null) return 0;
            int n = 0;
            for (int i = 0; i < c.abilities.Length; i++)
                if (c.abilities[i] != null && c.abilities[i].trigger == trigger) n++;
            return n;
        }

        /// <summary>表里有没有这个键（卡 id 或卡名）。</summary>
        public static bool TriggerHas(string cardIdOrName)
        {
            EnsureTriggerLoaded();
            return FindTriggerCard(cardIdOrName) != null;
        }

        /// <summary>自检/诊断：清零触发表的计数器（不动已载的表）。</summary>
        public static void ResetTriggerCounters()
        {
            TriggerFallbackHits = 0; TriggerLastFallback = null; _trigWarned.Clear();
        }

        // ── 🆕 2026-11-05（`B143` · 执行写手 `W1`）：**出牌（`ThisCardPlayed`）**那一拍的运行期读取口 ──
        //   `Resources/card_anim_order_play.json`（`工具/gen_card_anim_order_play.py` 产出）。
        //   判据 / 表形状 / 与另两张表的边界 → 上面 `CardAnimPlayCard` 那一段的注释。
        //   ⛔ **只读新表**：既有 `TryGetBeats`（activeAbility 那张）与 `TryGetTriggerBeats`（全部 trigger 那张）
        //      一个字都不动 —— 三张表、三条路，行集各不相同（94 卡只在 activeAbility 表、61 卡在旧 `play` 桶
        //      但不在全量件里 …现读差异见 `判据/B143_Play时长三类卡.md` 与生成器 `--diff-old`）。

        public const string PlayResourcePath = "card_anim_order_play";

        static CardAnimPlayCard[] _playAll;
        static Dictionary<string, CardAnimPlayCard> _playById, _playByName;
        static bool _playTried;
        static readonly HashSet<string> _playWarned = new HashSet<string>();

        /// <summary>出牌表载入失败的原因（载入成功 = null）。</summary>
        public static string PlayLastError { get; private set; }
        /// <summary>出牌表「没命中」次数与最近一次是谁（诊断；自检盯它）。</summary>
        public static int PlayFallbackHits { get; private set; }
        /// <summary>最近一次没命中的卡键。</summary>
        public static string PlayLastFallback { get; private set; }

        /// <summary>出牌表载进来了没有。</summary>
        public static bool PlayLoaded { get { EnsurePlayLoaded(); return _playAll != null && _playAll.Length > 0; } }
        /// <summary>表里几张卡（现读 475）。</summary>
        public static int PlayCount { get { EnsurePlayLoaded(); return _playAll != null ? _playAll.Length : 0; } }
        /// <summary>表里全部条目（自检对账用）。</summary>
        public static IEnumerable<CardAnimPlayCard> PlayItems { get { EnsurePlayLoaded(); return _playAll; } }
        /// <summary>表头统计（`stats` 那一块；自检拿它钉总数与生成器对账）。载不到 ⇒ `null`。</summary>
        public static CardAnimPlayStats PlayStats { get; private set; }

        static void EnsurePlayLoaded()
        {
            if (_playTried) return;
            _playTried = true;
            var ta = Resources.Load<TextAsset>(PlayResourcePath);
            if (ta == null)
            {
                PlayLastError = "`Resources/" + PlayResourcePath + ".json` 载不到 —— 跑一次 "
                              + "`python -I -X utf8 工具/gen_card_anim_order_play.py --write`";
                _playAll = new CardAnimPlayCard[0];
                _playById = new Dictionary<string, CardAnimPlayCard>();
                _playByName = new Dictionary<string, CardAnimPlayCard>();
                return;
            }
            CardAnimPlayFile f = null;
            try { f = JsonUtility.FromJson<CardAnimPlayFile>(ta.text); }
            catch (Exception e) { PlayLastError = "`" + PlayResourcePath + ".json` 解析失败：" + e.Message; }
            var items = f != null && f.cards != null ? f.cards : new CardAnimPlayCard[0];
            PlayStats = f != null ? f.stats : null;
            _playAll = items;
            _playById = new Dictionary<string, CardAnimPlayCard>(items.Length);
            _playByName = new Dictionary<string, CardAnimPlayCard>(items.Length);
            foreach (var it in items)
            {
                if (it == null) continue;
                if (!string.IsNullOrEmpty(it.id) && !_playById.ContainsKey(it.id)) _playById[it.id] = it;
                if (!string.IsNullOrEmpty(it.name) && !_playByName.ContainsKey(it.name)) _playByName[it.name] = it;
            }
            if (PlayLastError == null && items.Length == 0)
                PlayLastError = "`" + PlayResourcePath + ".json` 里一条都没有";
        }

        static CardAnimPlayCard FindPlayCard(string cardIdOrName)
        {
            if (string.IsNullOrEmpty(cardIdOrName)) return null;
            CardAnimPlayCard c = null;
            if (_playById.TryGetValue(cardIdOrName, out c) && c != null) return c;
            if (_playByName.TryGetValue(cardIdOrName, out c)) return c;
            return null;
        }

        /// <summary>按「卡 id 或卡名」取**这张卡全部 `ThisCardPlayed` 行的有序拍**，追加进 <paramref name="into"/>；
        /// 返回追加条数（`0` = 表里没有这张卡 / 它没有 trigger-0 行 / 每一拍都是空引用）。
        ///
        /// <para>⚠️ **行序 = `(ai, li)` 升序**（= 原版遍历 `cardAbilities` / `abilityLogic` 的次序，生成器已排好）；
        /// 同一条 ability 下多条 logic 行**都会返回**（⛔ 不要按 `ca` 去重 —— 跨行 `ca` 会重复，见
        /// `TryGetTriggerBeats` 的 doc）。</para>
        /// <para>⚠️ **返回 0 不等于「这张卡没有出牌动画」**：三种正常原因（不在表里 / 没有 trigger-0 行 /
        /// 每拍 `ca` 都是空串）都在 fallback 日志里，且**只出声一次**（`WarnCap` 上限）。</para></summary>
        public static int TryGetPlayBeats(string cardIdOrName, List<CardAnimBeat> into)
        {
            EnsurePlayLoaded();
            if (into == null) return 0;
            var c = FindPlayCard(cardIdOrName);
            int n = 0, rowsMatched = 0;
            if (c != null && c.rows != null)
            {
                for (int i = 0; i < c.rows.Length; i++)
                {
                    var r = c.rows[i];
                    if (r == null) continue;
                    rowsMatched++;
                    if (r.steps != null) { into.AddRange(r.steps); n += r.steps.Length; }
                }
            }
            if (rowsMatched > 0) return n;      // 行在、但每拍都是空引用 ⇒ 返回 0 且**不报「没这张卡」**
            PlayFallbackHits++;
            PlayLastFallback = cardIdOrName ?? "?";
            if (PlayLoaded && _playWarned.Count < WarnCap && _playWarned.Add(PlayLastFallback))
                Debug.Log("[Battle] 出牌演出表里没有 `" + cardIdOrName + "` 的 ThisCardPlayed 行"
                        + " ⇒ 这一条走**原样（不扩拍）**（⛔ 不硬扩；三种正常原因：① 这张卡没有 trigger-0 行 "
                        + "② 有行但每拍的 CardAnim 都是空串 ③ 它不在表里）");
            return 0;
        }

        /// <summary>这张卡在出牌表里有几行（`0` = 没有）。自检/诊断用。</summary>
        public static int PlayRowCount(string cardIdOrName)
        {
            EnsurePlayLoaded();
            var c = FindPlayCard(cardIdOrName);
            if (c == null || c.rows == null) return 0;
            int n = 0;
            for (int i = 0; i < c.rows.Length; i++) if (c.rows[i] != null) n++;
            return n;
        }

        /// <summary>自检复位（⛔ 引擎里别调）。</summary>
        public static void ResetPlayForTest()
        {
            PlayFallbackHits = 0; PlayLastFallback = null; _playWarned.Clear();
        }
    }
}
