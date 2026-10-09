// RuleCodes.cs — 规则引擎的返回码
//
// 非法操作是**正常流程**（拖到非法格位本来就该被拒绝），所以用返回码而不是抛异常。
// 码值**沿用我们上一版 Godot 复刻的那一套**（`d:/warpforge/scripts/rule_core.gd` 的常量）——
// 交叉验证时能直接对照。⚠️ 那份 `.gd` 是**我们自己**的复刻、**只作旁证**，**不是原版语义判据**
// （判据顺序 = ① 原版全量反编译 `D:/2/tools/decomp_full/` → ② 解包资源字段 → ③ 成品卡图卡面文字）。
// ⚠️ **2026-10-18 更正（铁律 5）**：本行原来写「码值和 `rule_core.gd` 的常量**逐一对齐**」
//   而没标它的地位 —— 容易被读成「那份 `.gd` 是基准」。**对齐是真的，基准不是它。**
using System.Collections.Generic;

namespace RuleEngine
{
    public static class RuleCodes
    {
        public const int OK = 0;
        public const int ErrBadHand = 1;          // 手牌索引非法
        public const int ErrCost = 2;             // 能量不足
        public const int ErrSlot = 3;             // 格位非法/被占
        public const int ErrNotTurn = 4;          // 非本方回合
        public const int ErrNotUnit = 5;          // 该格没有单位
        public const int ErrExhausted = 6;        // 已行动
        public const int ErrNoAttack = 7;         // 攻击力为 0 / Can't Attack
        public const int ErrSelf = 8;             // 不能攻击自己
        public const int ErrStunned = 9;          // 眩晕中无法行动
        public const int ErrTarget = 10;          // 目标不合法（Vanguard/Stealth/Flying）
        /// <summary>压制：无法执行**近战**攻击（规则书 :194）。
        /// ⚠️ 这个码值**沿用我们上一版复刻的**（`rule_core.gd:38` `ERR_PINDOWN := 11`）——
        /// 原来 11 被我们的 `ErrUnimplemented` 占着，2026-09-12 把自定义码往后挪到 13 让位。
        /// ⚠️ **2026-10-18 更正**：这里原来写「这个码值是**原版定的**」—— 但给出的唯一出处是
        /// 那份 `.gd`（**我们自己写的复刻**）⇒ **原版里到底是不是 11，还没回反编译核过**，如实记着。</summary>
        public const int ErrPindown = 11;

        // ← 以下两条是本工程新增的（rule_core.gd 没有对应码）——
        //   不是因为规则不同，而是 v1 还没实现，需要让调用方**明确知道**是「没实现」而非「不允许」
        public const int ErrUnimplemented = 13;   // 该功能本版未实现（如战术卡效果）
        public const int ErrNoAbility = 14;       // 这个单位没有主动技能（卡上没写 `Ability:`）
        /// <summary>职责（`Duty`）**本局已经用过了** —— 规则书 `:181`「一次性能力；可由其他卡牌效果
        /// **装填**再次使用」。本工程新增的码（`rule_core.gd` 里没有对应码）。</summary>
        public const int ErrDutyUsed = 15;
        /// <summary>这个单位没有那条**替代行动**（`Duty` / `Pray` / `Ferocity` / `Agenda`）。
        /// 和 <see cref="ErrNoAbility"/> 分开：那条是自定的 `Ability:` 关键词，这条是原版关键词。</summary>
        public const int ErrNoAction = 16;
        /// <summary>**这一格不是「可收集的路标石残骸」** —— 收集（`clickWaystone`）只对
        /// 「**己方 · 残骸 · 带 `Waystone.`**」那一格成立。本工程新增的码。
        /// 出处见 <see cref="RuleCore.CanCollectWaystone"/>。</summary>
        public const int ErrNotWaystone = 17;
        /// <summary>**要选目标的战术卡，一个合法目标都没有**（原版 `Battle/Tips/NoTargetAvailable`）。
        ///
        /// 🔴 **2026-10-18（第三会话 · `A985⑧` 第二步「拆码」）新开的一档** —— 在此之前这一档
        ///   与「格位非法/越界」**共用一个 `ErrSlot`** ⇒ 调用方**从码上分不出因**
        ///   （原版那一支出自 `BattleManager__CanPlayCard.c:142-156`：`IsValidSpellTarget`
        ///   扫完全场一个合法目标都没有）。
        /// **谁返回它**：`EffectResolver.CanPlayTactic` 的 `spec != null` 那一支（原来返回 `ErrSlot`）。
        /// ⚠️ **不等于 `ErrTarget`**：`ErrTarget` 是**攻击**那一侧「目标不合法（Vanguard/Stealth/Flying）」，
        ///   本档是**出牌**那一侧「这张战术卡压根没有可点的目标」—— 两件事，别合并。</summary>
        public const int ErrNoTargetAvailable = 18;
        /// <summary>**棋盘放不下**（原版的 `Battle/Tips/NotEnoughRoom`）。
        ///
        /// 🔴 **2026-10-18（同一笔）新开的一档** —— 在此之前它与「格位非法/越界」**共用 `ErrSlot`**。
        /// 判据 = `BattleManager__CanPlayCard.c:104-120`：`MinionManager.IsAvailableSlot` 为假
        ///   （**棋盘满**）⇒ `NotEnoughRoom`。我们那一格是
        ///   `RuleCore.CanPlayCard` 的 `!BoardSlots.HasRoomFor(ps, slot)`（**两侧都满**，
        ///   2026-10-01 换连续无洞模型之后这是**唯一**真的落不下）。
        /// ⚠️ **「越界（含 -1）」仍然返回 `ErrSlot`** —— 那是**参数非法**（调用方的错），
        ///   本档是**规则上放不下**（对局状态），两者的「该说什么」不一样。</summary>
        public const int ErrNotEnoughRoom = 19;

        static readonly Dictionary<int, string> Names = new Dictionary<int, string>
        {
            { OK,             "OK" },
            { ErrBadHand,     "手牌索引非法" },
            { ErrCost,        "能量不足" },
            { ErrSlot,        "格位非法或被占" },
            { ErrNotTurn,     "不是你的回合" },
            { ErrNotUnit,     "该格没有单位" },
            { ErrExhausted,   "该单位本回合已行动" },
            { ErrNoAttack,    "该单位没有攻击力或不能攻击" },
            { ErrSelf,        "不能攻击自己" },
            { ErrStunned,     "该单位处于眩晕" },
            { ErrTarget,      "目标不合法（Vanguard / Stealth / Flying 限制）" },
            { ErrPindown,     "该单位被压制，无法进行近战攻击" },
            { ErrUnimplemented, "该功能本版未实现" },
            { ErrNoAbility,   "该单位没有主动技能" },
            { ErrDutyUsed,    "该单位的职责本局已经用过了" },
            { ErrNoAction,    "该单位没有这条替代行动" },
            { ErrNotWaystone, "这一格不是可收集的路标石残骸" },
            // 🔴 **2026-10-18（第三会话 · 拆码的配套）**：18/19 两档**必须有中文名** ——
            //    漏了就走下面 `Describe` 的 fallback，吐「**未知错误码 19**」。
            //    触发面是**全仓 20+ 处诊断/日志** —— 🔑 **认法 = `grep "RuleCodes.Describe"`**（⛔ 别按行号找）：
            //    `BattleDriver.cs` 的 `[Replay] … 被拒` / `[Net] 重放 … 被拒` 两条 `Debug.LogError` ·
            //    `Net/NetApply.cs` 的「这条动作被引擎拒了：…」· `Net/NetBattle.cs` 的 `Send(NetKind.Reject, …)`
            //    与两处 `Abort(…)` · `EffectResolver.cs` 强制攻击失败那两行（`ctx.Log` + `unresolved.Add`）·
            //    `TutorialScript.cs` 的 `Fail(ctx, …, "引擎拒绝了这次出牌/攻击：…")` 两处 ·
            //    `Editor/RuleEngineTest.cs` 多处 `diag` · `Editor/BattleScene.cs` 的「…被拒绝（…）」那条
            //    —— 那些地方要的是**人话**。
            //    🔴 **2026-10-19（`A1014`）就地订正（铁律 5）**：本行原来**逐处写死行号**
            //    （`BattleDriver.cs:635` / `:717` · `NetApply.cs:103` · `NetBattle.cs:998-999/1036` ·
            //    `EffectResolver.cs:1441-1442` · `TutorialScript.cs:917/945` · `BattleScene.cs:2998`）。
            //    ⚠️ **2026-10-19 记下的那几个「现读」（`:700` / `:808` / `:1019-1020` / `:1057` /
            //    `:1481-1482`）到 2026-10-20（`A1123`）现读也【全落空】了** ——
            //    这就是「连就地订正都会漂」的第三个实例（另两个见 `资料/普查产出_第八会话/` 的 `D1` 表）。
            //    ⇒ **本段从此一个行号都不写**，一律按【**日志原句 + 符号名**】认（口径同 `A1039` / `A1123`）：
            //      · `BattleDriver` 里 `[Replay] 第 {i} 条…被拒` 那条 `Debug.LogError`（放录像那条路）；
            //      · `BattleDriver` 里 `[Net] 重放第 {i} 条…被拒` 那条 `Debug.LogError`（联机重放那条路）；
            //      · `Net/NetBattle` 里 `Abort(…)` 与 `Send(NetKind.Reject, …)` 那几处；
            //      · `EffectResolver` 里强制攻击失败那两行（`ctx.Log` + `unresolved.Add`）；
            //      · `Net/NetApply.cs` 的「这条动作被引擎拒了：…」·
            //        `TutorialScript.cs` 的 `Fail(ctx, …, "引擎拒绝了这次出牌/攻击：…")` 两处 ·
            //        `Editor/BattleScene.cs` 的「…被拒绝（…）」那条。
            //    ⚠️ 定位**一律以 `grep` 为准**（⛔ 别按行号找）—— 这正是 `A1123` 要的形状。
            //    ⚠️ **玩家可见的那一行不走这里**：它走 `BattleDriver.HintForCode(rc)`，18/19 **有原版键**
            //    ⇒ 落 `Loc.T(键)` 那一档（见下面 `Terms`）—— 所以这两行修的是**诊断面**。
            //    ⛔ 下面是**给人看的中文整句**，**不是** I2 词条键 ——「`Describe` 改出键」是 `A985⑧` 第 ③ 步，本轮不做。
            { ErrNoTargetAvailable, "这张战术卡没有可选的合法目标" },
            { ErrNotEnoughRoom,     "棋盘上已经放不下了" },
        };

        public static string Describe(int code)
        {
            string s;
            return Names.TryGetValue(code, out s) ? s : "未知错误码 " + code;
        }

        // ==================================================================
        //  🆕 2026-10-18（`A985⑧` · **第一步**）：**引擎码 → 原版 I2 词条键**（`Battle/Tips/*`）
        // ==================================================================
        //
        // 🔴 **要做什么**（`资料/普查产出_1018/G5_战斗本地化剩余.md:162-168` 那三步，**按序**）：
        //   ① 先把能 **1:1** 的几条在**调用点**接原版键（**本步**）；
        //   ② 再把 `ErrNoAttack` / `ErrTarget`（以及这里发现的 `ErrSlot`）**拆细**（`RuleEngine/`，
        //      另开一轮）；
        //      ✅ **2026-10-18（第三会话）：`ErrSlot` 那一半【已做】** —— 拆出
        //      `ErrNoTargetAvailable` / `ErrNotEnoughRoom` 两档，`Terms` 也随之从 2 条变 4 条
        //      （见下面那张表）。⚠️ **`ErrNoAttack` / `ErrTarget` 那一半【还没做】**，如实标着。
        //   ③ 最后才谈「`Describe` 要不要改成**出键**」（那一步会动 `RuleEngine` 的消费面）。
        //
        // ⛔ **本笔只做了 ① 里【本文件能做的那一半】：把映射建出来。**
        //    · **没有**改 `Describe`（那是第 ③ 步 —— 上面 `Names` 与 `Describe` 一个字没动）；
        //    · **没有**接调用点 —— 那些落点在 `CardPresentation/Battle/BattleDriver.cs`
        //      （① 玩家动作被引擎拒那条路 = `SetHint(HintForCode(rc))`；② 技能面板那一处 =
        //      `CardText.Phrase("NO LEGAL TARGET")`），**不在本代理的文件白名单里** ⇒ 只报告，不动手。
        //      🔴 **2026-10-19（`A1014`）就地订正（铁律 5）**：本行**原来写的是**
        //      「`SetHint(RuleCodes.Describe(rc))`，`@ ~:6916` 出牌被拒那条路 + `:7149` ……」——
        //      **那三个行号现读全落空**（`:6916` 是「选择答案已清空」、`:7149` 是催促提示，
        //      两处都与之无关），而且 `SetHint(RuleCodes.Describe(rc))` **这个写法全仓已不存在**
        //      ⇒ **改成按符号名认**（判据按 `HintForCode` / `NO LEGAL TARGET` 去 grep）；
        //      ⛔ **别再往这里写死新行号**（同 `A1039` 的口径）。
        //    · 调用点该长成的样子（给下一位）：`SetHint(key != null && Loc.HasEntry(key) ? Loc.T(key) : RuleCodes.Describe(rc))`
        //      （`key = RuleCodes.TermKey(rc)`；落地那一份 = `BattleDriver.HintForCodeWithKey`）。
        //      🔴 **2026-10-18（第四会话 · 双语③ 波 0）就地订正（铁律 5）**：本行**原来写的是**
        //      `Loc.T(RuleCodes.TermKey(rc)) ?? RuleCodes.Describe(rc)` —— **那是错的**，而且与
        //      `BattleDriver.HintForCodeWithKey` 的正确写法**并存**（= 工程红线「两处写同一条规则，迟早不一致」）。
        //      ⚠️ **2026-10-19（`A1014`）**：这里原来把它写成 `BattleDriver.cs:7771` —— **那个行号落空**。
        //      🔴 **2026-10-20（`A1123`）再订正（铁律 5）**：上一句记的「`HintForCodeWithKey` 现读在 `:8043`」
        //      **今天也落空了** ⇒ **这两处从此只按符号名认**：`BattleDriver.HintForCodeWithKey`（有键那一档）/
        //      `BattleDriver.HintForCode`（无键那一档）。⛔ 别再往这里写任何行号。错在哪：
        //      `Loc.T` **从不返回 null**（空键回 `""`、**缺键回【键名本身】**，见 `Core/Loc.cs` 的 `T()` doc）
        //      ⇒ `??` 那一半**永不触发** —— 无键那些码会拿到 `""`（提示行**全空** = 本工程最忌讳的静默失败）、
        //      有键但表里没有的码会**把键名印到界面上**。⇒ 必须**先判键、再判表**（`Loc.HasEntry` 是**唯一**
        //      一个「键在不在表里」的公开查询口）。下面 `TermKey` 的 doc 里同样那一句也一并订正了。
        //      🆕 同批（波 0）：那四条 `Battle/Tips/*` 键**已经收进 `Core/Loc.cs` 的表** ⇒ 「键**今天没有值**」
        //      那一句**从本批起不成立**（有键那几条现在走 `Loc.T`；`Describe` 仍是无键码那一档的出路）。
        //
        // 🔴 **原版那五条键的【落点】—— 逐支现读，第一权威**
        //   （`d:/2/tools/decomp_full/BattleManager__CanPlayCard.c`）：
        //   | 原版分支（判据符号） | 行 | 词条键 |
        //   |---|---|---|
        //   | `PlayerManager.HasEnoughMana` 为假 | `:83-95` | `Battle/Tips/NotEnoughMana` |
        //   | `MinionManager.IsAvailableSlot` 为假（**棋盘满**） | `:104-120` | `Battle/Tips/NotEnoughRoom` |
        //   | `IsValidSpellTarget` 扫完**一个合法目标都没有** | `:142-156` | `Battle/Tips/NoTargetAvailable` |
        //   | `(param_3 & isPlayer) == 0`（**不是你的回合**） | `:159-170` | `Battle/Tips/NotYourTurn` |
        //   | `*(bm + 0x350) + 0x18 > 0`（还有阻塞动作在跑） | `:31-48`（走 `ShowHeadsUpMessage`） | `Battle/Tips/PleaseWait` |
        //   中间那三支走 `BattleTipController.NotifyCantDoAction`，第一支与最后一支走
        //   `ShowHeadsUpMessage` —— **两条不同的提示通道**（落调用点时别混）。
        //   **键名怎么读出来的**：方法体里那 5 个 `DAT_18428axxx` 是**字面量指针**，
        //   `RVA = 地址 − 0x180000000`（`0x428a5e8` / `0x428a6e0` / `0x428a4f8` / `0x428a7d0` /
        //   `0x428a9b8`），查 `d:/2/tools/il2cpp_out/stringliteral.json` **5/5 命中**
        //   （`d:/4/CLAUDE.md` §二 那条「`_DAT_xxxxxxxx` 常量是能读的」的同一套办法）。
        //
        // ⚠️ **值一条都没有**：`Battle/Tips/*` 这 24 条键**本地只有名字**（I2 词条表在**远端 CCD**，
        //   全量扫过零 value —— `资料/普查产出_1018/W6_战斗本地化交件.md` §… + `Core/Tooltip.cs` 头注）。
        //   ⇒ 就算调用点接上 `Loc.T(key)`，**今天显示的仍然是我们的中文兜底**
        //     （`Loc.T` 查不到键时返回兜底/键名）。这一句是**如实标注**，不是缺陷。

        /// <summary>
        /// **这个返回码对应原版哪一条提示词条**（`Battle/Tips/&lt;名&gt;`）——
        /// **判不出来时返回 `null`**（⛔ 不猜，见下面那张「为什么不映射」的表）。
        ///
        /// 用法（**落点在表现层，本文件只提供映射**）：
        /// <c>SetHint(key != null &amp;&amp; Loc.HasEntry(key) ? Loc.T(key) : RuleCodes.Describe(rc))</c>
        /// （`key = RuleCodes.TermKey(rc)`；落地那一份 = `BattleDriver.HintForCodeWithKey`）。
        /// 🔴 **2026-10-18（第四会话）就地订正**：本行原来写 <c>Loc.T(RuleCodes.TermKey(rc)) ?? RuleCodes.Describe(rc)</c>
        /// —— **错的**：`Loc.T` 缺键返回**键名本身**、**从不返回 null** ⇒ `??` 那一半永不触发（详见上面那一处）。
        /// ⚠️ 「这些键今天没有值」**已过期**（波 0 起那四条键都在 `Core/Loc.cs` 表里）；
        /// 兜底那一半仍然**必须留** —— 它是「无键码（如 `ErrSlot`）/ 键不在表里」那一档的唯一出路。
        /// ⚠️ **别拿它替换 `Describe`**：AI / 网络层 / 教程层 / 自检都在用 `Describe`
        ///   （`grep \`RuleCodes.Describe\` 全仓 **20+ 处**），那些地方要的是**人话**、不是键名。
        /// </summary>
        /// <returns>词条键；**没有 1:1 对应物**时 <c>null</c>。</returns>
        public static string TermKey(int code)
        {
            string s;
            return Terms.TryGetValue(code, out s) ? s : null;
        }

        /// <summary>
        /// **已确认 1:1 的那四条** —— 判据是原版同一个方法体里的四支（逐支见上方那张表）。
        ///
        /// ✅ **2026-10-18（第三会话 · `A985⑧` 第二步「拆码」）：原来是两条，本笔补到四条。**
        ///   原来 `NoTargetAvailable` / `NotEnoughRoom` **进不来**，理由是「我们那两个因共用一个
        ///   `ErrSlot`，随手挑一个键 = 静默错报原因」—— **拆码之后那个理由消失了**：
        ///     · `NoTargetAvailable` ← <see cref="ErrNoTargetAvailable"/> ✅ 1:1
        ///       （`CanPlayCard.c:142-156` ↔ `EffectResolver.CanPlayTactic` 的 `spec != null` 支）。
        ///     · `NotEnoughRoom` ← <see cref="ErrNotEnoughRoom"/> ✅ 1:1
        ///       （`CanPlayCard.c:104-120` ↔ `RuleCore.CanPlayCard` 的 `!BoardSlots.HasRoomFor`）。
        ///   ⚠️ **`ErrSlot` 今天仍然 `null`**（它只剩「参数非法 / 越界」这一层意思，原版没有对应词条）——
        ///     别因为「现在它变窄了」就顺手给它塞一个键。
        ///   · `NotEnoughMana` ← <see cref="ErrCost"/> ✅ 1:1（全仓这个码**只在两处**返回，
        ///     两处都是「能量不够」：`RuleCore.CanPlayCard:1326` · `EffectResolver.CanPlayTactic:877`）。
        ///   · `NotYourTurn` ← <see cref="ErrNotTurn"/> ✅ 1:1（原版那一支就是 `(param_3 &amp; isPlayer) == 0`）。
        ///     ⚠️ 我们这一个码**还多盖了**「`ctx.IsOver`」这一档（对局已结束）—— 那时提示行根本不会走，
        ///     如实标着，不是缺陷。
        ///   · `PleaseWait` / `InvalidTarget` / `CantAttack*` / `AttackBlock*` /
        ///     `AttackGiantKiller` / `HandFull` / `DamageFatigue*` / `MuteEnemyChat` —— 同理：
        ///     我们这边**码太粗**（`ErrNoAttack` / `ErrTarget` 各盖好几档）或**没有那一档**
        ///     （只有一个 `PlayerInputFrozen` 布尔，分不出 `PleaseWait` / `PendingBerzerk`）
        ///     ⇒ 全部**不映射**，见 `资料/普查产出_1018/G5_战斗本地化剩余.md` §4·B 那张分档表。
        ///     （`A985⑧` 第二步剩下的那一半 —— `ErrNoAttack` / `ErrTarget` 也要拆 —— **还没做**。）
        /// </summary>
        static readonly Dictionary<int, string> Terms = new Dictionary<int, string>
        {
            { ErrCost,              "Battle/Tips/NotEnoughMana"    },   // 原版 `CanPlayCard.c:83-95`
            { ErrNotTurn,           "Battle/Tips/NotYourTurn"      },   // 原版 `CanPlayCard.c:159-170`
            { ErrNoTargetAvailable, "Battle/Tips/NoTargetAvailable" },  // 原版 `CanPlayCard.c:142-156`
            { ErrNotEnoughRoom,     "Battle/Tips/NotEnoughRoom"    },   // 原版 `CanPlayCard.c:104-120`
        };

        public static bool IsOk(int code) { return code == OK; }
    }
}
