// GameplayVariables.cs — **一局对战的全套参数**（四套实例：经典 / 遭遇 / 教程 / 轮抽）
//
// 🔴 **为什么要有这个类**：原版把「经典 / 冲突（Skirmish）」两套模式的差异**全部收在一个类里**
//    —— `Everguild.LiveOps.GameplayVariablesData`（**18 个字段**），由服务器按模式下发。
//    我们这边原来是**经典/冲突各自硬编码**（`DeckRules` 一套常量、引擎里 `RuleCore.StartHand` 这种散着写），
//    ⇒ 做遭遇模式时照原版收成一处：**经典/遭遇只是这份结构的两个实例**。
//    出处（唯一正本）：`资料/加时与冲突模式_原版规格.md` §2.5 —— 那张 18 字段表就是本文件的目录。
//
// ⚠️ **值的来源分三档，逐条标在字段注释里**（铁律 2：权威 = 反编译 > 解包字段 > 卡面/文案）：
//    · 【文案】= 原版**游戏内文案**逐字（`资料/索引与盘点/解包资源使用地图.md:533`，**官方**）——
//      `12-card decks / Up to 4 Legendaries (Warlord not included) / Warlords start with 10 less Health /
//       P1 3 Energy P2 4 Energy / Gain +2 Maximum Energy each turn / Overtime begins earlier /
//       No Offence card. Random Defence card. No mulligan`
//    · 【规则书】= `资料/规则书/Warpforge_Offline_Rulebook_1_5-3_中文翻译.md:57-64` ——
//      🔴 **那是粉丝实体版、不是官方**（该文件自己第 5 行写着 `Not official. Fan project.`）
//      ⇒ **只能当第二来源**，凡只有它一条支撑的，字段上都标着。
//    · 【推断】= 我们**从别的判据反推**出来的，注释里写清怎么推的。
//    · ⛔ **18 个值本身全在服务器**（`assets_full` / `解包整理/` / `d:/4` 全盘 0 命中）⇒ **抄不到**。
using System;

namespace RuleEngine
{
    /// <summary>对局模式 = 原版 `PlayModes`。**15 档全表，逐字照原版** ——
    /// 判据 = `d:/2/tools/il2cpp_out/dump.cs:46188-46205`（`enum PlayModes`，TypeDefIndex 961）：
    /// `Classic 0 · Duel 1 · PracticeLodge 2 · Dungeon 3 · Tutorial 4 · CutScene 5 · OfflinePractice 6 ·
    ///  ClosedDeck 7 · Campaign 8 · TutorialReplay 9 · Replay 10 · RankedFriendly 11 · OwnDeckTraining 12 ·
    ///  Skirmish 13 · Battle4Warpforge 14`。
    /// <para>🔴 **这不是 `MatchType`**（另一张表、另一组数字 `10/50/170/200…`）—— 两者别混
    /// （`MatchType` 见 `RuleEngine.MatchType`（`BattleContext.cs` 里那张 14 项派发表），它**由本枚举派生**）。</para>
    /// <para>⚠️ **枚举顺序/名字不许改**：它是**跨场景 · 过网 · 落盘**共用的字面量口径
    /// （`NetPendingBattle.Mode` / `MsgStart.mode` / 录像头 / 对局记录都存 `PlayModeNames.Name(…)`）。</para></summary>
    public enum GameMode
    {
        Classic = 0,
        Duel = 1,
        PracticeLodge = 2,
        Dungeon = 3,
        Tutorial = 4,
        CutScene = 5,
        /// <summary>练习（单机打 bot）—— 原版 `PracticeEvent.get_EventPlayMode`，VA `0x1808B66B0`。</summary>
        OfflinePractice = 6,
        ClosedDeck = 7,
        Campaign = 8,
        TutorialReplay = 9,
        Replay = 10,
        RankedFriendly = 11,
        /// <summary>`Deck info ▸ Practice Deck` 那条链（原版 `DeckInfoPopup.StartPracticeMatch`
        /// 调的 `StartMatch(OwnDeckTraining(0xc), …)`）。**与 `OfflinePractice 6` 不是同一档。**</summary>
        OwnDeckTraining = 12,
        /// <summary>冲突模式（原版内部叫 `FastMode`，是**真人 PvP**；单机只能走它那条 bot 退路）。</summary>
        Skirmish = 13,
        Battle4Warpforge = 14,
    }

    /// <summary>模式号的**字符串口径**（唯一一处）—— 跨场景 · 过网 · 落盘都走它。
    ///
    /// 🔴 为什么要有这一对：`MsgStart.mode` / `NetPendingBattle.Mode` / 录像头 / 对局记录里存的都是
    /// **字符串**（原版 `MatchData.playMode` 是个 `int`，我们这条链从头到尾是 `string`）。
    /// 老编码只有 `"Classic"` / `"Skirmish"` 两个词（= 能不能分辨模式），现在**就是枚举名**
    /// ⇒ 旧的录像/记录照样读得回来（这两个词都是合法的枚举名），**不用改存档格式**。
    /// ⛔ **别在别处再写一遍 `== "Skirmish"` 那种判**（两处写同一条规则 = 迟早不一致）。</summary>
    public static class PlayModeNames
    {
        /// <summary>枚举名（`Enum.ToString()`，15 档全是合法标识符）。</summary>
        public static string Name(GameMode mode) { return mode.ToString(); }

        /// <summary>字符串 → 模式号。**认不出就出声**（红线：不许静默失败）并退回 `Classic` ——
        /// 退回值与「这一局照经典那套打」自洽（`GameplayVariables.For` 也是这个兜底）。
        /// ⚠️ 数值串也认（`"6"` ⇒ `OfflinePractice`），这是 `Enum.TryParse` 的既定行为，不是我们加的。</summary>
        public static GameMode Parse(string s)
        {
            if (!string.IsNullOrEmpty(s))
            {
                GameMode m;
                if (Enum.TryParse(s, out m) && Enum.IsDefined(typeof(GameMode), m)) return m;
                UnityEngine.Debug.LogWarning("[Rule] 认不出的模式字符串「" + s + "」⇒ 按 `Classic(0)` 开这一局"
                                 + "（`PlayModeNames.Parse`；合法值 = `GameMode` 枚举那 15 档）");
            }
            return GameMode.Classic;
        }
    }

    /// <summary>
    /// 一局对战的全部参数。字段名**逐字照原版 `GameplayVariablesData`**（18 个），
    /// 末尾另加两个**原版不在这个类里、但我们同样要按模式变**的（注释里写明它原本住哪）。
    ///
    /// ⚠️ **原版有、我们引擎不读**的字段也留着（`startingManaSecond` 等）——
    ///    留着是为了「对照原版那张表时不用猜」，**用不到不等于可以删**（删了就看不出来我们漏了什么）。
    ///    🔴 **2026-10-25 订正**：本行原把 `alternativeTraitLogic` 也列在这句里 —— **它现在有两个读点**
    ///    （`EffectResolver.FinishActiveAbility` / `QuestPointStepFor`），见字段自己的注释。
    /// </summary>
    [Serializable]
    public class GameplayVariables
    {
        // ==================================================================
        //  🔴 **数值的唯一出处** —— `Classic` / `Skirmish` 两个实例、以及 `DeckRules` 那张老表，
        //     **全都从这里取**。要改一个数，**只改这里**（原来 `DeckRules` 一套常量、引擎里散着几个字面量，
        //     做遭遇模式时就会发现同一个「12」在两处 —— 那正是「两处写同一条规则 = 迟早不一致」）。
        // ==================================================================
        // 经典：**全部是改造前就在跑的值**（`RuleEngineTest` 2995 条断言 = 不许改口径的验收）
        public const int ClassicDeckSize = 30, ClassicStartingHand = 3, ClassicHandLimit = 10,
                         ClassicManaPerTurn = 1, ClassicStartingMana = 1, ClassicStartingManaSecond = 1,
                         ClassicOvertimeTurn = 10, ClassicDrawPerTurn = 1, ClassicManaAccumulation = 0,
                         ClassicWarlordLifeChange = 0,
                         ClassicMaxLegendaries = 0, ClassicCopiesOther = 2, ClassicCopiesLegendary = 1;
        // 遭遇（Skirmish）—— 逐条的来源分【文案】/【规则书】/【推断】，见下面 `Skirmish` 那个实例
        public const int SkirmishDeckSize = 12, SkirmishStartingHand = 4, SkirmishHandLimit = 8,
                         SkirmishManaPerTurn = 2, SkirmishStartingManaSecond = 2, SkirmishManaAccumulation = 1,
                         SkirmishWarlordLifeChange = -10, SkirmishMaxLegendaries = 4;

        // ---- 1~3：卡组构筑 ----------------------------------------------------
        /// <summary>【文案】**整副**传说卡上限（不含督军）：经典 **0 = 无此限制** · 遭遇 **4**（`Up to 4 Legendaries (Warlord not included)`）。
        /// <para>🔴 **2026-10-25（`E1` · 第五段 · R3 §六·2）就地订正**：这一格原来写 `1`，与官方文案 `Event_Description/Classic/Help` = **`No Legendary card limit`** 不符。
        /// 原版的判据是**非正数 = 本模式无上限**：`DeckUtility.CanAddCard.c:105` 那道闸是 `if (0 < gameplayVariables.maxLegendaries)` ⇒ `<= 0` 直接跳过。
        /// ⇒ 经典 = **0**。今天**零行为变化**（`DeckRules.LegendaryTotalLimit(false)` 仍显式返回 `int.MaxValue`），但谁哪天改成读本字段，`0` 会**正确**地表示「无限制」，而 `1` 会静默给经典加一条「整副只能 1 张传奇」。
        /// ⚠️ ⛔ **别把 0 读成「一张都不许」** —— 本字段的 `<= 0` 语义就是「关掉这条闸」。</para></summary>
        public int maxLegendaries = ClassicMaxLegendaries;
        /// <summary>【规则书:53】普/稀/史同名上限。原版活值是 `GetMaxCopiesInDeck` 里读的那个（见 `DeckRules` 文件头）。</summary>
        public int numberOfCopiesOtherRarities = ClassicCopiesOther;
        /// <summary>【规则书:53】传说卡同名上限。</summary>
        public int numberOfCopiesLegendary = ClassicCopiesLegendary;
        /// <summary>【规则书】每边棋盘格数（= 9，两模式相同）。
        /// <para>🔴🔴 **2026-10-15（账 `A2099`）：这个字段名与原版【同名不同义】—— 接原版 SO 前必须先看这一条。**
        /// **我们的 `side` = 玩家那一侧**（我方 9 格 / 敌方 9 格）；
        /// **原版 `GameplayVariablesData.slotsPerSide`（`+0x1C`）= 左/右【半场】各几格**
        /// （序列化 4 · ctor 默认 3）**⇒ 指的不是同一件事**。
        /// 判据 → `资料/普查产出_1015战斗/B4_战斗相机与棋盘几何.md` §二 与 `资料/战斗复刻_正本.md` §2·8/§2·11。
        /// ⛔ **将来接原版 `GameplayVariablesData` 时逐字段对【语义】，别按名字对** ——
        /// 按名字对会静默把「每侧 4 格」当成「每边 9 格」。⚠️ 那个 SO 目前**不在本地 91 个 bundle 里**，
        /// 所以运行时权威值「查不到」（如实记，⛔ 没猜）。</para></summary>
        public int slotsPerSide = 9;

        // ---- 5~6：督军与卡组 ---------------------------------------------------
        /// <summary>【文案】督军生命增减：经典 0 · 遭遇 **−10**（`Warlords start with 10 less Health`）。
        /// <para>🔴 **2026-10-18（`W` 笔）：SO 是当前数字 build 的基准；经典 0 / 冲突 −10 是运行时全局增量**
        /// （判据 → `资料/普查产出_1018/R_督军10血成因.md` §2）。卡表里那 49 项 `health` 差异
        /// （47 张督军 + `BL71`/`SW13`）是 **2026 build 把 SO 统一下调**（35→25 ×39 · 40→30 ×11 ·
        /// 30→20 · 35→30），**不是**本字段造成的：原版
        /// `BattleManager__SetupWarlordsLifeAndEnchantment.c:29,53,72` 只在
        /// `GameplayData.warlordLifeChange != 0` 时走
        /// `ForceChangeMaxHealth(督军, 当前最大生命 + warlordLifeChange)` —— 每局敌我**同加同减**，
        /// 产生不出逐卡 −5（`AM3`）/ −15（`TL35`）/ 0（9 张）这种差异。
        /// ⇒ ⛔ **别拿本字段去解释单张卡的 `health`**；卡面/结算的基准值来自卡表 `cards_engine.json`。</para></summary>
        public int warlordLifeChange = ClassicWarlordLifeChange;
        /// <summary>【文案】卡组张数：经典 30 · 遭遇 **12**（`12-card decks`）。</summary>
        public int deckSize = ClassicDeckSize;

        // ---- 7：加时 ----------------------------------------------------------
        /// <summary>加时**触发线**（**回合序号阈值**：**全局回合号 `ctx.Turn` ≥ 它**）。**两个模式都是 10**。
        ///
        /// 🔴 **原版机制（2026-09-26 查实，别再按推断写）**：唯一读点 =
        /// `BattleManager._NextTurn_d__395__MoveNext.c:191` —— 把 `overtimeTurn` 包成 `Nullable&lt;int&gt;`，
        /// 再判 `hasValue &amp;&amp; overtimeTurn &lt;= turnCounter` ⇒ 它是**回合序号阈值**，**不是能量阈值**；
        /// 且**可为 null**（null ⇒ 永远不进加时）。`turnCounter` = `BattleManager` 自己那个计数器（`+0x3F8`）。
        /// ⇒ 那个**具体值在服务器，本地没有**（`资料/加时与冲突模式_原版规格.md` §1.5）。
        /// ⇒ 这里**照它的形状写成可空**（`int?`）：**「没有值」= 这个模式没有加时**。
        ///    ⚠️ **目前两个模式都有值（都是 10）** —— `null` 那一支是**照原版的形状保留的**
        ///    （服务器不下发那个值 ⇒ 一次都不判），不是给我们现在用的。
        ///    ⚠️ **别把本类型塞进 `JsonUtility`** —— 它不支持可空字段，会**静默丢掉**这一个。
        ///
        /// ⚠️ 🔴 **2026-10-20 就地订正（`W58-TURN-A` · 铁律 5）**：这里原来写「两种判据在经典模式下等价」——
        ///    **不成立**。`turnCounter` 是**全局回合号**（两人共用、每回合 +1），同常数 10 下：
        ///    · 原版（`turnCounter >= overtimeTurn`）⇒ **全局第 10 回合**（= 后手第 5 个自己的回合，经典 `MaxEnergy` 才 6）；
        ///    · 「后手 `MaxEnergy >= 10`」⇒ 后手**第 9 个自己的回合**（≈ 全局第 18 回合）。
        ///    两者只在服务器真值恰好为 18 时才重合。
        /// 🔵 **现行实现 = 照原版的形状，比【全局回合数】**（`RuleCore.BeginTurn`：`ctx.Turn >= overtimeTurn`）。
        ///    `10` 这个数**仍是推断值** —— 原版真值在服务器，本地查不到（见本文件 §六·1 那条「没查清」）。
        ///    ⚠️ 连带：遭遇模式原来靠「每回合 +2、跳过 10」在第 4 个自己的回合进加时（文案 `Overtime begins earlier`）；
        ///    改成全局回合后它与经典一样落在全局第 10 回合 ⇒ 那句「更早」**不再由这条机制复现**，
        ///    除非服务器给遭遇一个更小的 `overtimeTurn`（**未解**）。
        ///
        /// ============================================================ 2026-10-17
        /// 🔴 **「回合序号」那条判据链已读完整（三段字段定义闭合，别再重复查）**：
        ///    · 比较行 = `BattleManager._NextTurn_d__395__MoveNext.c:191-207`：`hasValue &amp;&amp; overtimeTurn &lt;= iVar10`
        ///    · `iVar10` 取自 `lVar3 + 0x3F8` ⇒ `dump.cs:30871` = **`private ObscuredInt &lt;turnCounter&gt;k__BackingField; // 0x3F8`**
        ///    · `overtimeTurn` 本身 = `dump.cs:130179` `public int overtimeTurn; // 0x28`（类 `GameplayVariablesData`）
        ///    · 它的来源 = `dump.cs:78158` `private GameplayVariablesData &lt;GameplayData&gt;k__BackingField; // 0xA8`（类 `MatchData`）
        ///    ⇒ **原版确实拿它比【回合计数器】**（字段名就叫 `turnCounter`；旁边 `0x40C` 才是 `playerTurnCounter`）。
        ///    ⚠️ **影响面（先前低估了）**：🔴 **经典下两种判据也不等价**（2026-10-20 订正：原文写「数学等价」，不成立）——
        ///       按能量（经典后手 `MaxEnergy` 走 2/3/4…）⇒ **后手第 9 个自己的回合**（≈ 全局第 18）；
        ///       按回合序号（同样 10）⇒ **全局第 10 回合**（后手第 5 个自己的回合、能量才 6）。
        ///       遭遇下同样不等价（按能量 ⇒ 第 4 个自己的回合；按回合序号 ⇒ 全局第 10）。
        ///
        /// 🔵 **用户 2026-10-17 曾拍板：仍按【能量】决定，⛔ 不改成回合序号。**
        ///    原话口径：「**进入加时回合是看后手玩家的能量是否达到或者超过 10 点，只是看回合怎么实现？
        ///    根据之前的决策，按照能量决定。**」
        ///    ⇒ 那是**当时的口径**：**规则面**（后手能量 ≥ 10）优先于**原版实现面**（`turnCounter` 阈值）。
        /// 🔴 **2026-10-20（`W58-TURN-A`）起本条已按原版形状改成【全局回合数】** —— 任务书的判据见本字段开头
        ///    那段「2026-10-20 就地订正」。⚠️ 这一改与上面 2026-10-17 的记录**方向相反**，
        ///    口径以用户/调度台最终确认为准；本代理只按任务书执行，⛔ 没把这条历史删掉。
        ///    ⛔ **别再据上一段把实现改回能量阈值** —— 那是历史证据，不是现行代码。
        ///
        /// 📦 **下面这段是「能量口径」时期的留痕（2026-09-26 ~ 2026-10-20），⛔ 别当现行行为**：
        ///    🔴 它说「遭遇也有加时，而且更早 —— 靠的就是同一个 10」（**用户 2026-09-26 定案**）：
        ///    · 遭遇 `manaPerTurn = 2` + `manaAccumulation = 1` ⇒ 后手方 `MaxEnergy` 走
        ///      **4 → 7 → 9 → 11**（**跳过 10**）⇒ **第 4 个自己的回合**就够阈值（经典要第 9 个）。
        ///    · `>=` 这个「或超过」正是为它写的 —— 写成 `== 10` 的话遭遇**永远进不了加时**。
        ///    · 效果与经典**相同**：进入后**双方每回合抽 2 张**，且**进入的那一回合就抽**。
        ///    · ⇒ 当时认为 `Overtime begins earlier` 由这一条解释掉。
        ///    🔴 **2026-10-20（`W58-TURN-A`）起不成立**：判据改成全局回合后，两个模式都落在全局第 10 回合
        ///    ⇒ 那句文案要成立，得是服务器给遭遇一个**更小的 `overtimeTurn` 真值**（**未解**）。
        ///
        /// 📌 **2026-09-26 这一段翻过一次车，留痕**（免得下一个人重蹈）：
        ///    用户当天先说「遭遇没有加时」（看视频判断），我们据此把遭遇改成 `null`；
        ///    随后用户更正「**是我搞错了，遭遇也有加时**，后手最大能量达到**或超过** 10 时进入，
        ///    遭遇可能到 **11**；进入后双方每回合抽 2 张，**进入那个回合就开始抽**」。
        ///    ⇒ 改回 10。⚠️ **教训**：实况观察要问清「看到了什么现象」，
        ///    「没看到加时」≠「没有加时」（那局可能没打到触发点）。
        /// </summary>
        public int? overtimeTurn = ClassicOvertimeTurn;

        // ---- 8~13：能量 -------------------------------------------------------
        /// <summary>【推断】**先手方**的起始能量基数（两模式都 1）。
        /// 合成公式见 `RuleCore` 的回合开始那一段：
        /// `MaxEnergy = (后手 ? startingManaSecond : startingMana) + TurnCount × manaPerTurn`
        /// —— **字段语义照原版**（`BattleManager__SetupInitialMana.c:20,28,46`：先手取 `startingMana`、
        /// 后手取 `startingManaSecond`，用 `playerGoesFirst` 那个标志 XOR 选）。
        /// · 经典 (1,1,1)：第 1 个自己的回合 = **2** ——**与改造前逐字一致**
        /// · 遭遇 (1,**2**,2)：P1 第 1 回合 = 1+2 = **3** · P2 = 2+2 = **4**
        ///   —— 原版文案**原文** = `Player 1 starts at 3 Energy, Player 2 at 3+1 Energy`（🔴 2026-10-27 铁律 5 订正：旧引文写成 `P1 3 Energy P2 4 Energy`，把那个 `+1` 抹掉了；中文键同文「玩家2开始时有3+1点能量」。判据 → `资料/完全复刻/报告/B88_J1_两条悬案裁定.md` §悬案1）</summary>
        public int startingMana = ClassicStartingMana;
        /// <summary>【推断】**后手方**的起始能量基数：经典 1（⇒ 后手第 1 回合也是 2 点）·
        /// 遭遇 **2**（⇒ 后手第 1 回合 4 点，比先手多那 1 点）。
        /// 判据同 <see cref="startingMana"/> 那条（原版 `SetupInitialMana` 两个分支）。</summary>
        public int startingManaSecond = ClassicStartingManaSecond;
        /// <summary>🔴 **2026-10-20（`W58-TURN-B`）：已实现（原来是「原版有、我们不实现」）。**
        /// 原版 `BattleManager._NextTurn_d__395__MoveNext.c:296-334` 在 `turnCounter == 1` 时把它当
        /// 一次性「存款」写进**后手方**的 `PlayerManager.SetSavedMana`；
        /// `:344-352` 在那一方自己的回合 `TurnStart` **之后**兑现：
        /// `RefillCustomMana(saved)`（= **只加当前 `unusedMana`**，`PlayerManager__RefillCustomMana.c:35-41`，
        /// **不动 `totalMana`/最大能量**）。
        /// ⇒ 我们的落点 = `RuleCore.BeginTurn` 里「维护费之后」那一段：
        ///   条件 `p == Players[SecondSeat] && p.TurnCount == 1`（后手第一次自己的回合）；
        ///   `p.Energy += addedManaSecond`，**绝不加进 `MaxEnergy`**（加了会每回合都多出这份补贴）。
        /// 🔶 自己设计（非原版）：服务端 `PlayEventData.gameplayVariables` 本地取不到 ⇒ 这里只能填值。**本条 2026-10-27 就地订正（铁律 5）：原来写「两个模式都是 0」—— 不成立**，因为 `0 &lt; addedManaSecond` 是「能量累积」整组 HUD 的**唯一显隐闸**（`SetupBoardPhase.c:179-196`），而散战/教程的官方文案都卖这个机制（`Event_Description/Skirmish/Help` · `TutorialData/Tutorial1/Turn6/Tip2fast`）⇒ 至少一个模式非 0。**散战那一格本批【暂不动】**（加 1 会让后手首回合能量变成 5，与官方 `3+1`＝4 和用户实况「后手首回合上限 4」都冲突，见报告 §悬案1 的「算术矛盾」）；**教程那一格已填 1**（见 `Tutorial` 实例）。判据 → `资料/完全复刻/报告/B88_J1_两条悬案裁定.md`。</summary>
        public int addedManaSecond = 0;
        /// <summary>【文案】每回合最大能量增长：经典 1 · 遭遇 **2**（`Gain +2 Maximum Energy each turn`）。</summary>
        public int manaPerTurn = ClassicManaPerTurn;
        /// <summary>【规则书】每回合抽牌数（= 1，两模式相同）。⚠️ **加时那多抽的 1 张是另一支**
        /// （`DeckRules.OvertimeExtraDraw`），别混进来。</summary>
        public int drawCardsPerTurn = ClassicDrawPerTurn;
        /// <summary>【规则书:60，**非官方**】回合末未用完的能量最多保存几点（下回合最大能量 +它）。
        /// 经典 0（不保存）· 遭遇 **1**（规则书那句「回合末未用完的能量保存 1 点 → 下回合最大 +1」）。
        /// ⚠️ 语义（是**一次性结转**还是**永久涨上限**）规则书没写死，我们按「**一次性结转**」实现
        /// （`PlayerState.ManaCarry`）。
        /// ✅ **2026-09-26 补了一条实况佐证**：用户看原版游戏确认了这个机制，并给出了数 ——
        ///    遭遇后手 `MaxEnergy` 走 **4 → 7 → 9 → 11**（每回合 +2，再叠上上回合没用完保存的 1 点），
        ///    ⇒ **不再只有粉丝规则书一条支撑**。
        /// 🔴 **同一轮修掉一个真缺陷**：这一段代码**在此之前从来没生效过** ——
        ///    `RuleCore.EndTurn` 无条件 `p.Energy = 0`，而 `BeginTurn` 判结转用的是 `p.Energy > 0`
        ///    ⇒ `ManaCarry` 恒为 0（死代码），遭遇的曲线实际是 4 → 6 → 8 → 10。
        ///    现在改成**在 `EndTurn` 里结算**。⚠️ 只影响遭遇（经典 `manaAccumulation = 0` ⇒ 恒 0）。</summary>
        public int manaAccumulation = ClassicManaAccumulation;

        // ---- 另加：🔴 本局累计的「永久最大能量」增量（`Gain N Energy`）----------------
        /// <summary>🔴 **2026-10-20（`W58-TURN-A`）**：**本局累计的永久最大能量增量**（按下标 0/1）。
        ///
        /// <para>**为什么需要它**：原版 `PlayerManager.totalMana`（`+0x38`）被 `Gain N Energy`
        /// （`ManaBuff.createMana`）**永久抬高**；而我们的 `MaxEnergy` 是**每回合从公式重算**的
        /// （`RuleCore.BeginTurn`）⇒ 不单独存这份增量的话，下一回合就被公式覆盖掉。</para>
        ///
        /// <para>**原版语义（逐句 · 第一权威）**：
        /// · `BattleManager__ProcessManaBuff.c:38-62`：`createMana(+0x16)` 为真 ⇒ `PlayerManager__AddNewMana`；
        ///   `refillMana(+0x17)` 为真 ⇒ `PlayerManager__RefillOneMana`（只补当前，不动上限）。
        /// · `PlayerManager__AddNewMana.c`：`totalMana(+0x38)++` **无条件**；
        ///   `unusedMana(+0x4C)` 只在第 2 个实参 `filledMana` 为真时 +1（且仅当它 < 新 total）。
        /// · 那个 `filledMana` 实参在反汇编里 = `ManaBuff.refillMana`（`+0x17`）——
        ///   `0x971e11: movzx edx, byte ptr [rbx+0x17]` → `call AddNewMana`
        ///   ⇒ **只有 `createMana` 的卡：上限 +N、当前能量一点不动**（DA61 `Plasma Generator` 就是这一档）。
        ///   两张都置位的卡（全池 3 张）才是「上限 +N 且当前也 +N」。</para>
        ///
        /// <para>**为什么放在本类**：`PlayerState` / `BattleContext` 不在本代理的文件白名单内；
        /// `ctx.Vars` 是**每局一份的 `Clone()`**（`BattleContext.Vars:719`）⇒ 出不了这一局。
        /// ⚠️ 它是**可变的对局状态**、不是模式参数 —— 将来把状态搬回 `PlayerState` 时一起搬走。</para>
        /// </summary>
        public int manaBonusSeat0 = 0;
        /// <inheritdoc cref="manaBonusSeat0"/>
        public int manaBonusSeat1 = 0;

        /// <summary>读某一座位的永久最大能量增量（唯一读口，⛔ 别直接摸上面两个字段）。</summary>
        public int ManaBonus(int seat) { return seat == 0 ? manaBonusSeat0 : manaBonusSeat1; }
        /// <summary>加某一座位的永久最大能量增量（唯一写口）。</summary>
        public void AddManaBonus(int seat, int n)
        {
            if (seat == 0) manaBonusSeat0 += n; else manaBonusSeat1 += n;
        }

        // ---- 14~16：手牌与换牌 -------------------------------------------------
        /// <summary>【规则书:58，**非官方**】手牌上限：经典 10 · 遭遇 **8**。</summary>
        public int handLimit = ClassicHandLimit;
        /// <summary>**牌堆抽空时怎么办**：true ⇒ 把墓地里的初始牌**洗回**牌堆；false ⇒ 改走
        /// `BattleManager.ResolveFatigue()`（疲劳）。读点唯一 =
        /// `BattleManager._ResolveDrawCard_d__432__MoveNext.c:67`。
        ///
        /// <para>🔴 **用户 2026-10-20 拍板：卡组空了就空，之后只走疲劳扣血，不洗卡回卡组**
        /// ⇒ 本字段**固定 `false`**（两个模式）。我们的空库分支本来就一直是恒疲劳
        /// （`RuleCore.MoveDeckTopToHand` 的空库分支），**行为不变**。</para>
        ///
        /// <para>🔴 **2026-10-20（`W58-AMBUSH`）就地订正（铁律 5）**：本字段原来写 `true`，
        /// 注释称「经典模式原版的真值是 **true（洗回）**」—— **那句话没有任何本地判据**：
        /// · 它是**纯服务器值**：`GameplayVariablesData +0x48`，由 LiveOps 事件载荷
        ///   `PlayEventData.gameplayVariables`（`+0x80`）下发（`dump.cs:130187` / `:130241`）；
        /// · 类定义里**没有初始化器** ⇒ 本地缺省 = `false`（疲劳）；
        /// · `assets_full` / 旧 profile 缓存 / 关服前存档 / `d:/4` 全盘搜不到它的**值**。</para>
        ///
        /// <para>判据全文 → `资料/普查产出_1020/W58_回合流程_三处未定判据.md` §一。
        /// ⚠️ 遭遇那条 `false` 的**依据不同**（原版文案 `No mulligan` 的同族推断，不换牌也不洗回）；
        /// 现在两条都取 `false`，但别把那句推断当成经典模式的判据。</para></summary>
        public bool shouldReshuffle = false;
        /// <summary>【文案】换牌阶段总开关（**bool**，已查实：false ⇒ 连弹窗都不弹，
        /// `BattleManager__SetupInitialState.c:98-104`）。遭遇 = false（`No mulligan`）。</summary>
        public bool showMulligan = true;

        // ---- 17~18：防御卡与其它 ------------------------------------------------
        /// <summary>【原版语义已查实】**编辑模式的卡池列不列防御卡**。
        /// 读点唯一 = `DeckEditingWindow._GetCardCollection` 里那个筛选 lambda
        /// （`DeckEditingWindow___GetCardCollection_b__32_1.c:35`，筛到 `type == 0xD2` 时返回它）。
        /// 🔴 **2026-10-17 就地订正（铁律 5 · 现核）**：本行原来写「遭遇 = true（原版文案 `Random Defence card` ——
        ///   防御卡**不由玩家挑**，所以编辑卡池里**不列**它）」—— **两处都不对**：
        ///   ① **值的方向**：那个 lambda 是 `.Where` 谓词，**真 = 保留**（`return assignDefensiveCardsInEditMode;`）
        ///      ⇒ **`true` = 编辑器卡池里【列出来】`false` = 剔掉**（`资料/普查产出_1017/查证_assignDefensiveCardsInEditMode.md` §二）。
        ///   ② **那句文案不是本字段的证据** —— `Random Defence card` 讲的是**战前发放**，不是组卡（本方 §2.7c 已判）。
        ///   ⇒ 准确说法：**`Classic = false` / `Skirmish = true`**，而**我们不读它**（读者数 0），所以选哪个值都不影响行为。
        /// 🔴 **⚠️ 我们不实现它** —— 我们的卡池/编辑器一直把防御卡列出来（`useUpgradableOnly` 那一类同款）。
        ///    影响面：**预组**那条路不受影响（防御卡由生成器直接塞进 `PlayerDeck.DefensiveId`）；
        ///    受影响的是「玩家在编辑器里自建遭遇卡组」—— 而那条路**今天根本走不通**
        ///    （`DeckEditorState.Skirmish` 全仓没有任何地方赋值）。⇒ 记着，等那条路做起来再补。</summary>
        public bool assignDefensiveCardsInEditMode = false;
        /// <summary>**冲突档（Alt trait logic）开关**（原版 `PlayEventData.gameplayVariables.alternativeTraitLogic`）。
        ///
        /// <para>🔴 **2026-10-25 就地订正（铁律 5）：本行原来是 `public int alternativeTraitLogic = 0;`
        /// +「⛔ 未解（原版别的机制）。两边都填 0 —— 不假装知道」。两句都过期了：</para>
        /// <list type="bullet">
        /// <item>**类型**：原版是 **`bool`** —— `dump.cs:130190` `public bool alternativeTraitLogic; // 0x4B`
        ///   （我们的 `int` 只是当年没有判据时的占位；`==0` 与 `==false` 行为等价，但类型应当照原版）。</item>
        /// <item>**值从哪来**：LiveOps 事件载荷 `PlayEventData.gameplayVariables`（`+0x80`）**服务端下发**
        ///   ⇒ 本地**没有**序列化真值（全量反编译里这个偏移 **0 个写点**）。</item>
        /// <item>🔴 **它就是「冲突档」的开关，⛔ 不是 `PlayModes` / `MatchType`** —— `+0x4B` 全库
        ///   **只有 4 个读点、0 个写点**：`CardScript__FinishActiveAbility.c:11`（祈祷结束立刻 +1 信仰）
        ///   ＋ 任务点阈值三处（`PlayerManager__AddQuestPoints.c:28` · `PlayerManager__ResetMana.c:118` ·
        ///   `PlayerManager._SetQuestPointsWithDelay_d__43__MoveNext.c:52`）。⇒ **客户端没有任何代码**
        ///   把模式号映射到这两条行为，绑到本字段是**唯一**的绑法。</item>
        /// <item>**两个消费点**（逐条现读反编译）：
        ///   ① `CardScript__FinishActiveAbility.c:11,13,19,21` —— 能力结束、卡带 `pray(0x4a1)` ⇒ 控制者 `AddFaithMana(+1)`；
        ///   ② `PlayerManager__AddQuestPoints.c:28,33,41` —— 任务点阈值 `false` ⇒ 3（`+0x2C`）/ `true` ⇒ 4（`+0x30`）。</item>
        /// </list>
        /// <para>⇒ **`Classic = false` / `Skirmish = true`**（用户 2026-10-25 口径「经典 3 / 冲突 4、冲突祈祷立刻 +1」；
        /// 文案同向：`CardTraitDescription/questPoints` 写「每 **3** 点」、`questPointsAlt` 写「每 4 点」）。</para>
        /// <para>🔴 **2026-10-25 再次就地订正（铁律 5）：本段原来还写「官方 base `CardTraitDescription/pray`
        /// 那句『下回合开始 +1 信仰』**在原版代码里不存在**、⛔ 别照文案去补」—— 那句是【错的】。**
        /// 经典那条**确实存在**，只是**不在 C# 里、在卡数据里**：隐藏附魔 **`Pray Enchantment`**
        /// （`m_Name = Z-NC Sororitas-Pray_enchantment`，`cardType 20`，`path_id 1528516720421242597`）：
        /// 23 张带 `pray` 特性的卡在 `cardAbilities[trigger=655 (Pray)].abilityLogic[0].buffEffect`
        /// 挂 `{buffType:15 singleUseEnchantment, enchantment → Pray Enchantment}`；该附魔
        /// ability[0] `trigger 60 UnitDamaged → removeThisEnchantment`（受伤即撤）、
        /// ability[1] `trigger 31 TurnStartPlayer → buffMana{manaAmount:1, faithMana:1} + removeThisEnchantment`
        /// （自己回合开始 +1、一次性），结算落 `BattleManager__ProcessManaBuff.c:28,79 AddFaithMana(+1)`。
        /// ⇒ 判据全文 → `资料/待办判据_祈祷信仰与任务点_1025b.md` §二·④。
        /// ⚠️ **别再引 `isPraying(+0x67)` 当信仰来源** —— 全库只有一个读点
        /// `FilterMethods__MeetsConditionCriteria.c:294-296`，只服务「目标是正在祈祷的单位」这类筛选。</para>
        /// </summary>
        public bool alternativeTraitLogic = false;

        // ---- 另加：原版不在 `GameplayVariablesData` 里，但同样按模式变 ----------------
        /// <summary>起手张数：经典 3 · 遭遇 **4**。
        /// 🔴 **原版这个数不在这个类里** —— 它在 `ScenarioVariables.startingHand`（`DefaultScenario` = 3，
        /// 见 `资料/加时与冲突模式_原版规格.md` §2.6 那张表）；遭遇的 4 只有【规则书:58，非官方】一条支撑。
        /// 放这儿是因为**它同样按模式变**，一起读才不会又散出去一处。</summary>
        public int startingHand = ClassicStartingHand;

        /// <summary>**后手额外多抽几张**（先手/后手四件差异之一，默认 **1**）。
        /// 出处：原版 `ScenarioVariables.secondExtraCards`（字段偏移 **0x20** —— `startingMana` 0x18 /
        /// `startingHand` 0x1C / `secondExtraCards` 0x20 / `maxMana` 0x24 / `maxCardsInHand` 0x28，
        /// 见 `资料/战斗规则与数值_出处.md`）；消费点 `PlayerHand.GetSecondExtraCardsCount`。
        /// 判据全文 → `资料/加时与冲突模式_原版规格.md` §2.8「先手/后手的四件差异」**第 1 条**。
        /// ⚠️ 它是 `ScenarioVariables` 上的字段（**不按模式变**）⇒ 两个模式同值，`Skirmish` **不覆盖**它。
        /// ⚠️ 只发给**后手**（`ctx.SecondSeat`）—— 与防御卡那条同一个口径。</summary>
        public int secondExtraCards = 1;

        // ---- 两个实例 ----------------------------------------------------------
        static GameplayVariables _classic, _skirmish;

        /// <summary>经典（全部是我们**改造前就在跑的**值 —— 换上来**不许改变任何行为**，
        /// `RuleEngineTest` 2995 条断言就是这条的验收）。</summary>
        public static GameplayVariables Classic
        {
            get
            {
                if (_classic == null) _classic = new GameplayVariables();   // 字段默认值就是经典
                return _classic;
            }
        }

        /// <summary>冲突/遭遇（Skirmish）。逐条来源见各字段注释；
        /// **只有【文案】那几条是官方的**，其余标着【规则书】（非官方）或【推断】。</summary>
        public static GameplayVariables Skirmish
        {
            get
            {
                if (_skirmish != null) return _skirmish;
                var v = new GameplayVariables();
                v.maxLegendaries = SkirmishMaxLegendaries;            // 【文案】Up to 4 Legendaries
                v.warlordLifeChange = SkirmishWarlordLifeChange;      // 【文案】10 less Health
                v.deckSize = SkirmishDeckSize;                        // 【文案】12-card decks
                v.manaPerTurn = SkirmishManaPerTurn;                  // 【文案】+2 Maximum Energy each turn
                v.startingManaSecond = SkirmishStartingManaSecond;    // 【推断】文案 P1 3 / P2 4 的那 1 点差
                v.showMulligan = false;                               // 【文案】No mulligan
                v.shouldReshuffle = false;                            // 【文案】No mulligan
                v.assignDefensiveCardsInEditMode = true;              // 【文案】Random Defence card
                v.handLimit = SkirmishHandLimit;                      // 【规则书:58，非官方】
                v.startingHand = SkirmishStartingHand;                // 【规则书:58，非官方】
                v.manaAccumulation = SkirmishManaAccumulation;        // 【规则书:60，非官方】
                // 🔴 **2026-10-25（用户口径 + 全量反编译）：冲突档 = 本开关 `true`。**
                //    判据：`PlayerManager__AddQuestPoints.c:28,33,41` —— 任务点阈值 `false` ⇒ 3（`+0x2C`）/
                //    `true` ⇒ 4（`+0x30`）；＋ `CardScript__FinishActiveAbility.c:11,13,19,21` ——
                //    `true` ∧ 卡带 `pray(0x4a1)` ⇒ **主动能力结束立刻 +1 信仰**。
                //    ⇒ 用户 2026-10-25：「**经典 3、冲突 4**」「**冲突祈祷立刻 +1，不受伤再下回合 +1**」。
                //    ⚠️ 原版这两条行为**只由这一个字段选**（`+0x4B` 全库 4 读 0 写），`PlayModes` / `MatchType` 不参与。
                //    ⚠️ `ClosedDeck`（轮抽）是 `Skirmish.Clone()` ⇒ 会**跟着**拿到 `true`（原版真值查不到，如实标着）。
                //    判据全文 → `资料/待办判据_祈祷信仰与任务点_1025b.md` §一 / §二·①。
                v.alternativeTraitLogic = true;                       // 【用户口径 + 反编译】冲突档
                // 🔴 **遭遇也有加时**（用户 2026-09-26 定案：先看错成「没有」，当天更正「是我搞错了」）。
                //    ⚠️ **2026-10-20（`W58-TURN-A`）就地订正（铁律 5）**：上面那句「更早 —— 靠的就是同一个 10」
                //    只在**按能量判**时成立（遭遇 `manaPerTurn = 2` + `manaAccumulation = 1` ⇒ 后手
                //    `MaxEnergy` 走 **4 → 7 → 9 → 11**、第 4 个自己的回合就够 10）。
                //    现在判据改回原版的**全局回合数**（`turnCounter >= overtimeTurn`）⇒ 它与经典一样落在
                //    **全局第 10 回合**，`Overtime begins earlier` 那句**不再由这条机制复现**。
                //    ⇒ 那条文案要成立，得是服务器给遭遇一个**更小的 `overtimeTurn` 真值**（**未解**，本地没有）。
                //    ⛔ 这里一度被改成 `null`（「遭遇没有加时」）—— 那是照用户**当时看错的一次观察**改的，
                //    用户当天已更正「是我搞错了」。全过程留痕 → `overtimeTurn` 字段的注释。
                v.overtimeTurn = ClassicOvertimeTurn;               // 【推断】值是 10，见上
                _skirmish = v;
                return _skirmish;
            }
        }

        /// <summary>
        /// 🆕 2026-10-17（B29）：**教程局**（`GameMode.Tutorial 4` / `TutorialReplay 9`）。
        ///
        /// 🔴 **原版那 18 个值在服务器**（`GameplayVariablesData`，`MatchData +0xA8`）⇒ 教程那一档
        ///    **抄不到**。唯一有本地产物的是**另一个类** `ScenarioVariables`
        ///    （`bundle_duplicateassetisolationso_assets_all/MonoBehaviour/TutorialScenario.json` 实读：
        ///     `startingMana 1 · startingHand 3 · secondExtraCards 0 · maxMana 10 · maxCardsInHand 10 ·
        ///      clockTimeLimit 1000 · clockCountdownSec 10`）。
        ///    ⇒ 本实例 = **经典值 + 那份 `TutorialScenario` 覆盖到的三处**，
        ///      **每一处都在字段注释里标了来源**；**没覆盖到的仍是经典值**（⛔ 不假装知道）。
        ///
        /// ✅ **反编译能钉死的两条**（不是推断）：
        ///   · `showMulligan = false` —— 教程走的是 `BattleManager._TutorialStartSequence`
        ///     （**不是** `_SetupMulliganPhase`）⇒ **根本没有换牌阶段**
        ///     （`BattleManager._StartBattleSequence_d__331__MoveNext.c:88-95` 那一处二选一）；
        ///   · `deckSize` 那一格**对教程不参与校验** —— 关卡牌是 7～30 张（`tutorial_decks.json` 实读），
        ///     原版也不拿构筑规则去卡它（牌是关卡 SO 直接给的）。
        ///     这里留着 30 只是为了让 `IsSkirmish`（判据 = `deckSize &lt; 30`）保持 false。
        /// </summary>
        public static GameplayVariables Tutorial
        {
            get
            {
                if (_tutorial != null) return _tutorial;
                var v = new GameplayVariables();
                v.startingHand = TutorialStartingHand;         // 【TutorialScenario】startingHand 3
                v.secondExtraCards = TutorialSecondExtraCards; // 【TutorialScenario】secondExtraCards 0
                v.handLimit = TutorialMaxCardsInHand;          // 【TutorialScenario】maxCardsInHand 10
                v.showMulligan = false; v.manaAccumulation = 1; v.addedManaSecond = 1;   // 【反编译】教程不跑换牌阶段 ＋ 🔶 累积两格（判据见下行）
                // ⚠️ **没动的**（原版值在服务器、本地查不到）：startingMana / startingManaSecond /
                //    manaPerTurn / overtimeTurn / shouldReshuffle —— 一律沿用经典值。
                //    判据只到这里为止，⛔ 别按「教程大概应该……」去改（**这两个累积格是例外，有官方判据**）：🔶 自己设计（非原版）——服务端 `gameplayVariables` 取不到，`manaAccumulation = 1` ＋ `addedManaSecond = 1` 是按**官方教学文案**补的（`TutorialData/Tutorial1/Turn6/Tip2fast`「You can accumulate 1 Energy per turn… You will get 1 more your next turn」教的就是这条）；`addedManaSecond` 非 0 同时是「累积」那组 HUD 的唯一显隐闸（`SetupBoardPhase.c:179-196`），否则教程里的教学点永远演示不出来。判据全文 → `资料/完全复刻/报告/B88_J1_两条悬案裁定.md` §悬案1。调度台 2026-10-27 裁定。
                _tutorial = v;
                return _tutorial;
            }
        }
        static GameplayVariables _tutorial;

        /// <summary>
        /// 🆕 2026-10-24（执行代理 `E2`）：**轮抽局**（`GameMode.ClosedDeck 7`）。
        ///
        /// <para>🔴 **原版真值拿不到**：轮抽那一局的参数由服务端 `DraftEventData.gameplayVariables` 下发
        /// （判据 → `资料/普查产出_1024b/S2_轮抽模式数据.md` · `S3_轮抽窗原版规格.md` §五；
        /// 与本文件那 18 个值同一条口径 —— `GameplayVariablesData..ctor` 是空桩、全量反编译里没有一处字面量赋值
        /// ⇒ 本地 0 命中）。</para>
        ///
        /// <para>⇒ **这一档是【我们挑的】，⛔ 不是原版真值**：显式**复用 `Skirmish` 那一份**（`Skirmish.Clone()`）。
        /// 唯一依据是**结构性的**：轮抽组出来的牌**恒 12 张**（每包 3 张 × 4 轮 —— `CardsPerPack = 3` 是原版真值、
        /// `RoundsPerDraft = 4` 是我们挑的），而 `Skirmish` 是本地唯一一套「12 张」的参数
        /// ⇒ 12 张的牌**不能**按经典那套（30 张 / 起手 3 / 传说上限 1 / 有换牌）去开。</para>
        ///
        /// <para>📌 **逐字段（值 / 来源）** —— 将来拿到服务端真值就**只改这一张表**：</para>
        /// <list type="bullet">
        /// <item>`deckSize = 12` —— 原版【文案】`12-card decks`（轮抽侧另有「4 轮 × 3 张」的结构性一致）。</item>
        /// <item>`maxLegendaries = 4` —— 原版【文案】`Up to 4 Legendaries (Warlord not included)`。</item>
        /// <item>`warlordLifeChange = -10` —— 原版【文案】`Warlords start with 10 less Health`。</item>
        /// <item>`manaPerTurn = 2` —— 原版【文案】`Gain +2 Maximum Energy each turn`；
        ///   `startingManaSecond = 2` —— 【推断】（同文案 `P1 3 Energy P2 4 Energy` 的那 1 点差）。</item>
        /// <item>`showMulligan = false` · `shouldReshuffle = false` —— 原版【文案】`No mulligan`。</item>
        /// <item>`assignDefensiveCardsInEditMode = true` —— 原版【文案】`Random Defence card`
        ///   （⚠️ 该文案**不是**这个字段的证据，见字段自己的注释；且我们只读不实现）。</item>
        /// <item>`handLimit = 8` · `startingHand = 4` · `manaAccumulation = 1` —— 【规则书，**非官方**】。</item>
        /// <item>`overtimeTurn = 10` —— 【推断】（本地没有真值，见字段注释）。</item>
        /// <item>`addedManaSecond = 0` · `manaBonusSeat0/1 = 0` · `slotsPerSide = 9` ·
        ///   `numberOfCopies*` / `drawCardsPerTurn` / `secondExtraCards = 1` —— 本地缺省（与经典同值）。</item>
        /// </list>
        ///
        /// <para>⚠️ **`IsSkirmish` 在这份实例上是 `true`**（判据 = `deckSize &lt; 30`）—— 这一点**语义正确**
        /// （它答的是「12 张那套」，不是模式号）。⛔ **别拿它当模式号**：本局模式仍是 `ClosedDeck 7`
        /// （`BattleDriver.PlayMode` / `BattleContext.PlayMode`，判据是**入口窗**）。</para>
        ///
        /// <para>🔴 **为什么不直接 `return Skirmish`**：轮抽（选包组牌）与冲突（构筑 12 张）是两件事，
        /// 原版各自有自己的 `gameplayVariables` ⇒ 服务端真值到手时这两套**会分叉**。
        /// 先各给一个实例，将来改一份不会顺手改掉另一份。</para>
        /// </summary>
        public static GameplayVariables ClosedDeck
        {
            get
            {
                if (_closedDeck != null) return _closedDeck;
                // ⚠️ 克隆的是 `Skirmish` 那一份的**当前值**（惰性一次）。
                //    ⛔ 别在这里逐字段抄字面量 —— 那就是「两处写同一条规则 = 迟早不一致」。
                // ⚠️ 「惰性一次」的副作用：**第一次访问那一刻** `Skirmish` 是什么值就定死什么值。
                //    自检里有临时改过 `Skirmish` 字段的地方（`DeckScene` 那处是 try/finally 同步还原），
                //    ⛔ 别在那样的窗口里**首次**访问本属性；生产路径没有这种窗口。
                _closedDeck = Skirmish.Clone();
                return _closedDeck;
            }
        }
        static GameplayVariables _closedDeck;

        /// <summary>【TutorialScenario】`startingHand = 3`（原版那个类，不是 `GameplayVariablesData`）。</summary>
        public const int TutorialStartingHand = 3;
        /// <summary>【TutorialScenario】`secondExtraCards = 0` —— **经典是 1**，
        /// 教程这一档「后手不额外多抽」（我们把它当 0 用）。</summary>
        public const int TutorialSecondExtraCards = 0;
        /// <summary>【TutorialScenario】`maxCardsInHand = 10`（与经典同值）。</summary>
        public const int TutorialMaxCardsInHand = 10;

        /// <summary>按模式取实例。**15 档里只有 `Skirmish 13` 用遭遇那一套**，
        /// 🆕 2026-10-17 起 **`Tutorial 4` 与 `TutorialReplay 9` 用教程那一套**，
        /// 🆕 2026-10-24 起 **`ClosedDeck 7`（轮抽）用自己的那一档**（= 复用遭遇那套的 12 张参数），
        /// 其余 11 档全走经典 —— 这是我们**只有四份实例**的必然结果（原版那 18 个值是**服务器按模式下发**的，
        /// `资料/加时与冲突模式_原版规格.md` §2.5 ⇒ 本地抄不到更多份）。
        /// 判据：15 档里**原版上**带「12 张 · 4 传说 · 督军 −10 生命 · 无换牌」这套的只有 `Skirmish`
        /// （= `FastMode`，原版 `MatchType.FastMode 200` 那一支）；`OfflinePractice 6` /
        /// `OwnDeckTraining 12` 是**用自己 30 张的牌打练习**，`Classic 0` 是排位 ⇒ 都不是快攻那套。
        /// `ClosedDeck 7` 的判据见 <see cref="ClosedDeck"/>（**我们挑的**：轮抽牌恒 12 张）。
        /// 教程那两档的判据见 <see cref="Tutorial"/>。
        /// ⚠️ **这里是「模式号 → 规则参数」的唯一一处**：⛔ 别在别处再写 `mode == Skirmish ? … : …`。</summary>
        public static GameplayVariables For(GameMode mode)
        {
            if (mode == GameMode.Skirmish) return Skirmish;
            // 🆕 2026-10-24（`E2`）：**轮抽局（12 张）单开一档** —— ⛔ 别让它靠 `default` 掉进经典。
            //   掉进经典 = 这套参数说「30 张 / 起手 3 / 传说上限 1 / 有换牌」，而轮抽**恒交 12 张**
            //   ⇒ 静默错口径（`IsSkirmish` 也会变成 false，凡按它分档的地方一起错）。
            //   🔴 原版真值在服务端、本地拿不到 ⇒ 本档**复用 Skirmish**（我们挑的），见 `ClosedDeck` 的 doc。
            if (mode == GameMode.ClosedDeck) return ClosedDeck;
            if (mode == GameMode.Tutorial || mode == GameMode.TutorialReplay) return Tutorial;
            return Classic;
        }

        /// <summary>这套参数算不算遭遇模式（判据 = 卡组张数，**不是**「是不是那个实例」——
        /// 免得有人手搓一份改了几个字段的 `GameplayVariables` 就判错）。</summary>
        public bool IsSkirmish { get { return deckSize < 30; } }

        public GameplayVariables Clone()
        {
            return (GameplayVariables)MemberwiseClone();
        }
    }

    // ==============================================================================================
    //  🆕 2026-10-27（批次 9 · 执行代理 `B9_W13CDW` · 账 `HUD:750` / `HUD:756` / `HUD:758`）：
    //  原版 **`VarsGlobal`** 里「打出的卡摆到哪」那一组 —— **两条展示位公式的唯一出处**。
    //
    //  🔴 **为什么住在本文件 / 为什么单开一个类**：施工单 K-15 写的是「补进 `GameplayVariables`」，
    //     但**原版那 8 个值不在 `GameplayVariablesData` 里**（那是本文件上面那个 18 字段的按模式参数表），
    //     而在**另一个类** `VarsGlobal`（资产 `m_Name = GlobalVariables`，PathID 451，
    //     在 `Warpforge_Data/sharedassets0.assets`，**48 个字段**，**不按模式变**）。
    //     ⇒ 这里**单开一个类**、与 `GameplayVariables` 并列；⛔ 别把那 8 个值塞进 `GameplayVariables` 的字段里 ——
    //     那会重演 `slotsPerSide` 那次「同名不同义」的错（按名字对字段对出来的静默错）。
    //
    //  📌 **这些值不是我们挑的、也不是猜的**：`工具/read_varsglobal.py` 从上面那个 .assets 的原始字节
    //     按**声明顺序**解出来（整表 → `资料/VarsGlobal_原版数值.md` §一）；
    //     偏移 → 字段名的对应由两条**独立锚点**反证（`minionInPlayScale(+0x68) = 0.75` ·
    //     `cardInHandMovingScale(+0x44) = 1.1`）—— 判据 → `资料/普查产出_1024/D6_进攻卡展示缩放.md` §二。
    // ==============================================================================================
    /// <summary>原版 `VarsGlobal`（全局显示参数）中与「打出的卡展示位 / 展示缩放」有关的 8 个值。
    ///
    /// <para>消费点（原版，两处**逐字同一套式子**）：
    /// `CardScript.GetEnemyCardPlayedDisplayPosition()`（`HUD:756`）与
    /// `CardScript.GetOffensiveCardPlayedDisplayPosition()`（`HUD:758`）；
    /// 缩放那条 = `CardScript.GetEnemyCardPlayedDisplayScale()`。</para>
    ///
    /// <para>⚠️ **本类今天没有调用者**（施工单 K-15 的落点 = `CardPresentation/Battle/BattleDriver.cs`
    /// 的 reveal 链 / `DisplayEnemyCardInCenter`，那是**另一个写手**的文件 ⇒ 本代理只备好判据与唯一公式，
    /// 接线在那边做）。见 `资料/完全复刻/报告/B9_W13CDW_展示位与加载态.md` 的「改动请求」。</para>
    /// </summary>
    public static class VarsGlobal
    {
        // ---- 展示缩放（`HUD:750` / `HUD:756` 共用同一条原版函数）------------------------
        /// <summary>【原版实读】`VarsGlobal.enemyCardDisplayScale`（`+0x48`）= **1.4** ——
        /// 打出的卡居中展示时的**绝对 `localScale`**（不是「手牌缩放 × 倍率」）。</summary>
        public const float EnemyCardDisplayScale = 1.4f;
        /// <summary>【原版实读】`VarsGlobal.enemyCardScaleMobileMultiplier`（`+0x4c`）= **1.3** ——
        /// 小屏档（`GameStaticData.smallScreenUI(+0x11c)` 为真）再乘一次 ⇒ 合 **1.82**。</summary>
        public const float EnemyCardScaleMobileMultiplier = 1.3f;

        // ---- 敌方牌展示位偏移（`HUD:756`）----------------------------------------------
        /// <summary>【原版实读】`VarsGlobal.enemyCardDisplayYoffset`（`+0x50`）= **24** —— 公式里是**减**（往下）。</summary>
        public const float EnemyCardDisplayYOffset = 24f;
        /// <summary>【原版实读】`VarsGlobal.enemyCardDisplayXoffset`（`+0x54`）= **−26** —— 桌面档。</summary>
        public const float EnemyCardDisplayXOffset = -26f;
        /// <summary>【原版实读】`VarsGlobal.enemyCardDisplayMobileXoffset`（`+0x58`）= **25** —— 小屏档换掉上一格
        /// （⚠️ **符号与桌面档相反**，不是笔误：原版两支各自读一个字段）。</summary>
        public const float EnemyCardDisplayMobileXOffset = 25f;

        // ---- 进攻（战术）牌展示位偏移（`HUD:758` / `HUD:750`）---------------------------
        /// <summary>【原版实读】`VarsGlobal.offensiveCardDisplayYoffset`（`+0x5c`）= **30**。</summary>
        public const float OffensiveCardDisplayYOffset = 30f;
        /// <summary>【原版实读】`VarsGlobal.offensiveCardDisplayXoffset`（`+0x60`）= **−25** —— 桌面档。</summary>
        public const float OffensiveCardDisplayXOffset = -25f;
        /// <summary>【原版实读】`VarsGlobal.offensiveCardDisplayMobileXoffset`（`+0x64`）= **−25**
        /// （与桌面档**同值**，但**是另一个字段**：`CardScript__GetOffensiveCardPlayedDisplayPosition.c:36` 读 `+0x64`
        /// —— 写成「移动档沿用桌面值」在**今天**看不出差别，将来服务端改一侧就静默错）。</summary>
        public const float OffensiveCardDisplayMobileXOffset = -25f;

        // ---- 两条展示位公式（唯一一份）------------------------------------------------
        /// <summary>**「打出的卡展示位」= 手牌锚的世界坐标 + `right × Xoffset` − `up × Yoffset`。**
        ///
        /// <para>原版两个方法（`HUD:756` 敌方牌 / `HUD:758` 进攻牌）**除了读哪三个偏移字段之外逐行相同**
        /// ⇒ 只写这一份（施工单 K-15 明写「⛔ 别写两份」）。
        /// 判据：`decomp_full/CardScript__GetEnemyCardPlayedDisplayPosition.c:60-75` 与
        /// `decomp_full/CardScript__GetOffensiveCardPlayedDisplayPosition.c:60-75`。</para>
        ///
        /// <para>🔴 **`right` / `up` 不是数据，是引擎常量**：那两处读的是 `DAT_1842da2a8` 的
        /// static_fields `+0x3c` / `+0x18`，而 `DAT_1842da2a8` **就是 `UnityEngine.Vector3` 的 Il2CppClass**
        /// （静态字段表 `zero 0x0 / one 0xC / up 0x18 / right 0x3C` —— `d:/2/tools/il2cpp_out/dump.cs:537034-537039`；
        /// 类身份三条独立反证 → `资料/普查产出_1011/审查_W1.md:113`）
        /// ⇒ 值 = `(1,0,0)` / `(0,1,0)`，**不需要从任何解包实例里读**
        /// （派活包那句「6 个展示偏移要先从解包实例取」把这 6 个 float 也算成了待读数据）。</para>
        ///
        /// <para>⚠️ **基线 `handAnchorWorld` = 原版 `BattleManager.enemyHand`（`+0xD8`）的 `transform.position`**
        /// —— 敌方牌与**进攻牌共用同一个敌方手牌锚**（原版代码就是这么写的；
        /// 字段表 `dump.cs:30739-30741`：`playerHand // 0xD0` / `enemyHand // 0xD8`）。
        /// 揭示卡本身也是 `battleCards.CreateCard(card, enemyHand.transform, …)` 建的 ⇒ 同一个父。</para>
        ///
        /// <para>⚠️ **两档 y 别写成同一个**（施工单 K-15 的原话）：敌方牌是 `−24`、进攻牌是 `−30`。</para>
        /// </summary>
        /// <param name="handAnchorWorld">敌方手牌锚的**世界坐标**（原版 `BattleManager.enemyHand.transform.position`）。</param>
        /// <param name="offensive">true ⇒ 进攻/战术牌那一套偏移（`HUD:758`）；false ⇒ 敌方牌那一套（`HUD:756`）。</param>
        /// <param name="smallScreen">原版 `GameStaticData.smallScreenUI(+0x11c)`（我们这边没有这一档 ⇒ 恒传 false）。</param>
        public static WorldPoint PlayedCardDisplayPosition(
            WorldPoint handAnchorWorld, bool offensive, bool smallScreen)
        {
            float x = offensive
                ? (smallScreen ? OffensiveCardDisplayMobileXOffset : OffensiveCardDisplayXOffset)
                : (smallScreen ? EnemyCardDisplayMobileXOffset : EnemyCardDisplayXOffset);
            float y = offensive ? OffensiveCardDisplayYOffset : EnemyCardDisplayYOffset;
            // 原版逐项：x = 手牌锚.x + right.x·X − up.x·Y（right.x = 1、up.x = 0）
            //           y = 手牌锚.y + right.y·X − up.y·Y（right.y = 0、up.y = 1）⇒ 减 Y
            //           z = 手牌锚.z（right/up 的 z 分量都是 0）
            return new WorldPoint(handAnchorWorld.x + x, handAnchorWorld.y - y, handAnchorWorld.z);
        }

        /// <summary>把上面那条公式的世界偏移换成**屏幕 px**（@1080p）。
        ///
        /// <para>【派生】换算系数 = **14.835 px / 世界单位** @1080p：战斗 HUD 那台 `Canvas` 是
        /// **Screen Space - Camera**（`m_RenderMode = 1`，⚠️ 枚举 `Overlay=0 / ScreenSpaceCamera=1 / WorldSpace=2`）、
        /// `m_PlaneDistance = 100`、UI 相机 FOV 40 ⇒ `1080 / (2 × 100 × tan20°) = 14.835`；
        /// 连带 `lossyScale(HandArea) = 108 × 1/14.835 = 7.28`。
        /// 判据 → `资料/手牌布局_原版算法与参数.md` §四（单位桥）＋
        /// `C:/Users/qjh36/.claude/projects/d--4/memory/warpforge-original-3d-size-bridge.md`。</para>
        ///
        /// <para>✅ **坐标系 = 原版 UGUI 那一套：左上为原点、`y 向下为正`**（与本仓 `LayoutSpace.FromPixel`
        /// 同一个约定，`Core/LayoutSpace.cs:214` 头注：「原版是 UGUI，参数都是 1920×1080 设计像素、左上为原点、
        /// y 向下」）⇒ 世界里的「**减 `up × Y`**」（往下）在屏幕上就是 **`y` 增大**，所以本函数**原样返回 `+Y`**
        /// （⛔ 别在这里再翻一次符号 —— 那会和 `RevealStartPx/EndPx` 那一套口径打架）。</para>
        ///
        /// <para>⚠️ **这是「偏移量」的换算，不是「绝对位置」** —— 绝对位置还要加手牌锚自己在屏幕上的落点，
        /// 而 `enemyHand` 的锚点是**整条链里今天唯一还缺的那一格**（见报告「没查清的部分」）。</para>
        /// </summary>
        /// <param name="screenHeightPx">目标屏高（1080 ⇒ 系数原样；其它分辨率按比例）。</param>
        /// <returns>(dx, dy) 设计像素，**y 向下为正**（可直接加到 `LayoutSpace.FromPixel` 的入参上）。</returns>
        public static PxOffset PlayedCardDisplayOffsetPx(
            bool offensive, bool smallScreen, float screenHeightPx)
        {
            const float pxPerUnitAt1080 = 14.835f;
            float k = pxPerUnitAt1080 * (screenHeightPx / 1080f);
            float x = offensive
                ? (smallScreen ? OffensiveCardDisplayMobileXOffset : OffensiveCardDisplayXOffset)
                : (smallScreen ? EnemyCardDisplayMobileXOffset : EnemyCardDisplayXOffset);
            float y = offensive ? OffensiveCardDisplayYOffset : EnemyCardDisplayYOffset;
            // y 向下为正（同 `LayoutSpace.FromPixel`）⇒ 世界里的「减 up·Y」（往下）= 屏幕上 +Y
            return new PxOffset(x * k, y * k);
        }
    }

    // ================================================================================================
    //  🆕 2026-10-28（`B93_F5` · 账 `战斗逻辑驱动:411`）
    //  = 原版 `BattleManager.IsClosedDeckGame(MatchType chosenMatchType)` 的唯一等价谓词。
    //  判据（🥇 全文 10 行）= `d:/2/tools/decomp_full/BattleManager__IsClosedDeckGame.c:1-10`：
    //      if ((matchType != 0x6e /*110 ClosedDeck*/) && (matchType != 0x78 /*120 ClosedDeckBot*/)) return 0;
    //      return 1;
    //  ⇒ 真值集 = **{110, 120}**（与我们 `MatchType` 表的两个值逐值吻合）。
    //
    //  ⚠️ **为什么挂在扩展方法上**：逻辑上它属于 `MatchTypes`（`BattleContext.cs`），而那个文件本批不属本代理
    //    ⇒ 先在这里给**唯一一份**实现。⛔ 不要在调用点手写 `t == ClosedDeck || t == ClosedDeckBot`
    //    （两处写同一条规则 = 迟早不一致）；F4 把它并进 `MatchTypes` 类尾之后，删掉本类即可。
    //  ⚠️ **别拿它替换语义不同的谓词**：`MatchTypes.UsesDefencePool`（{50,80,110,120}）·
    //    `IsAgainstAI`（10 档）· `IsAgainstOnlineBot`（11 档）各自是不同的真值集 ⇒ 只有「问这是不是轮抽局」才用它。
    //    「被内联 ⇒ 0 调用点也不判 D」那条口径见 `报告/B92_B3`：原版这个 `public static` 在可读四源里 0 调用点。
    // ================================================================================================
    public static class MatchTypeExtensions
    {
        /// <summary>= 原版 `BattleManager.IsClosedDeckGame`：`110 ClosedDeck` 或 `120 ClosedDeckBot` ⇒ 真。
        /// <para>⚠️ 比之前务必取 <see cref="MatchTypes.Effective"/> 那一侧（原版比的也是翻转后的 `matchType`）。
        /// ⚠️ 这两个值在 `GameMode` 上只有一档（`ClosedDeck 7`）—— 110→120 只改 `MatchType`、不改 `PlayMode`
        /// （`BattleContext.MatchTypes.Effective`），所以「是不是轮抽局」问 `MatchType` 才覆盖 110 与 120 两种。</para></summary>
        public static bool IsClosedDeckGame(this MatchType t)
        {
            return t == MatchType.ClosedDeck        // 110 (0x6e)
                || t == MatchType.ClosedDeckBot;    // 120 (0x78)
        }
    }
}
