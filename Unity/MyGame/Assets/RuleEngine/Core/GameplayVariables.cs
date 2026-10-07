// GameplayVariables.cs — **一局对战的全套参数**（经典 / 遭遇 两套实例）
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
    /// ⚠️ **原版有、我们引擎不读**的字段也留着（`startingManaSecond` / `alternativeTraitLogic` …）——
    ///    留着是为了「对照原版那张表时不用猜」，**用不到不等于可以删**（删了就看不出来我们漏了什么）。
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
                         ClassicMaxLegendaries = 1, ClassicCopiesOther = 2, ClassicCopiesLegendary = 1;
        // 遭遇（Skirmish）—— 逐条的来源分【文案】/【规则书】/【推断】，见下面 `Skirmish` 那个实例
        public const int SkirmishDeckSize = 12, SkirmishStartingHand = 4, SkirmishHandLimit = 8,
                         SkirmishManaPerTurn = 2, SkirmishStartingManaSecond = 2, SkirmishManaAccumulation = 1,
                         SkirmishWarlordLifeChange = -10, SkirmishMaxLegendaries = 4;

        // ---- 1~3：卡组构筑 ----------------------------------------------------
        /// <summary>【文案】传说卡上限（不含督军）：经典 1 · 遭遇 **4**（`Up to 4 Legendaries (Warlord not included)`）。</summary>
        public int maxLegendaries = ClassicMaxLegendaries;
        /// <summary>【规则书:53】普/稀/史同名上限。原版活值是 `GetMaxCopiesInDeck` 里读的那个（见 `DeckRules` 文件头）。</summary>
        public int numberOfCopiesOtherRarities = ClassicCopiesOther;
        /// <summary>【规则书:53】传说卡同名上限。</summary>
        public int numberOfCopiesLegendary = ClassicCopiesLegendary;
        /// <summary>【规则书】每边棋盘格数（= 9，两模式相同）。</summary>
        public int slotsPerSide = 9;

        // ---- 5~6：督军与卡组 ---------------------------------------------------
        /// <summary>【文案】督军生命增减：经典 0 · 遭遇 **−10**（`Warlords start with 10 less Health`）。</summary>
        public int warlordLifeChange = ClassicWarlordLifeChange;
        /// <summary>【文案】卡组张数：经典 30 · 遭遇 **12**（`12-card decks`）。</summary>
        public int deckSize = ClassicDeckSize;

        // ---- 7：加时 ----------------------------------------------------------
        /// <summary>加时**触发线**（**我们用的是能量阈值**：后手方最大能量 **≥ 它**）。**两个模式都是 10**。
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
        /// ⚠️ **经典那一支我们用的仍是能量阈值**（「后手方最大能量 ≥ 10」）—— 那是**改造前就在跑的老规矩**
        ///    （用户 2026-09-17 给的判据 = 规则书「后手玩家最大能量达 10」），全套断言盯着它，**不能动**。
        ///    两种判据在**经典模式**下等价（`manaPerTurn = 1` ⇒ 能量随回合单调增，序号阈值 ⇔ 能量阈值）；
        ///    原版那个回合序号真值拿到之后，才谈得上把机制也换掉（见 `项目任务.md` §三 第 2 条）。
        ///
        /// ============================================================ 2026-10-17
        /// 🔴 **「回合序号」那条判据链已读完整（三段字段定义闭合，别再重复查）**：
        ///    · 比较行 = `BattleManager._NextTurn_d__395__MoveNext.c:191-207`：`hasValue && overtimeTurn <= iVar10`
        ///    · `iVar10` 取自 `lVar3 + 0x3F8` ⇒ `dump.cs:30871` = **`private ObscuredInt <turnCounter>k__BackingField; // 0x3F8`**
        ///    · `overtimeTurn` 本身 = `dump.cs:130179` `public int overtimeTurn; // 0x28`（类 `GameplayVariablesData`）
        ///    · 它的来源 = `dump.cs:78158` `private GameplayVariablesData <GameplayData>k__BackingField; // 0xA8`（类 `MatchData`）
        ///    ⇒ **原版确实拿它比【回合计数器】**（字段名就叫 `turnCounter`；旁边 `0x40C` 才是 `playerTurnCounter`）。
        ///    ⚠️ **影响面（先前低估了）**：经典下两种判据数学等价；**遭遇下不等价** ——
        ///       按能量（`MaxEnergy` 走 `4→7→9→11`）⇒ **第 3 个自己的回合**就进加时；按回合序号（同样 10）⇒ **第 10 个回合**。
        ///
        /// 🔵 **用户 2026-10-17 拍板：仍按【能量】决定，⛔ 不改成回合序号。**
        ///    原话口径：「**进入加时回合是看后手玩家的能量是否达到或者超过 10 点，只是看回合怎么实现？
        ///    根据之前的决策，按照能量决定。**」
        ///    ⇒ 这是**一处有意的偏离**：**规则面**（后手能量 ≥ 10）优先于**原版实现面**（`turnCounter` 阈值）。
        ///    ⛔ **别再按上面那条「原版是回合序号」去改代码** —— 那是**证据**，不是**口径**；口径归用户。
        ///    ⚠️ 因此「遭遇的加时落在第 3 个自己的回合」是**有意的**，不是缺陷。
        ///
        /// 🔴 **遭遇模式：也有加时，而且是「更早」—— 靠的就是同一个 10**
        ///    （**用户 2026-09-26 定案**，逐条见 `RuleCore.BeginTurn` 那段的断言）：
        ///    · 遭遇 `manaPerTurn = 2` + `manaAccumulation = 1` ⇒ 后手方 `MaxEnergy` 走
        ///      **4 → 7 → 9 → 11**（**跳过 10**）⇒ **第 4 个自己的回合**就够阈值（经典要第 9 个）。
        ///    · `>=` 这个**「或超过」**正是为它写的 —— 写成 `== 10` 的话遭遇**永远进不了加时**。
        ///    · 效果与经典**相同**：进入后**双方每回合抽 2 张**，且**进入的那一回合就抽**。
        ///    · ⇒ 原版文案那句 `Overtime begins earlier` **由这一条解释掉了**，**不用另发明常数**。
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
        ///   —— 正好是原版文案那句 `P1 3 Energy P2 4 Energy`</summary>
        public int startingMana = ClassicStartingMana;
        /// <summary>【推断】**后手方**的起始能量基数：经典 1（⇒ 后手第 1 回合也是 2 点）·
        /// 遭遇 **2**（⇒ 后手第 1 回合 4 点，比先手多那 1 点）。
        /// 判据同 <see cref="startingMana"/> 那条（原版 `SetupInitialMana` 两个分支）。</summary>
        public int startingManaSecond = ClassicStartingManaSecond;
        /// <summary>⚠️ **原版有、我们不实现**：`BattleManager._NextTurn` 在 `turnCounter == 1` 时
        /// 把它当一次性「存款」写进后手方的 `PlayerManager.SetSavedMana`（`:306,:312`）。
        /// 我们**不用它** —— 遭遇模式那两组数（P1 3 / P2 4）**已经被 `startingMana` / `startingManaSecond`
        /// 完全解释掉了**，再加一份会重复补贴。留着是为了对照原版那张 18 字段表（值也在服务器，本地没有）。</summary>
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

        // ---- 14~16：手牌与换牌 -------------------------------------------------
        /// <summary>【规则书:58，**非官方**】手牌上限：经典 10 · 遭遇 **8**。</summary>
        public int handLimit = ClassicHandLimit;
        /// <summary>【原版语义已查实】**牌堆抽空时怎么办**：true ⇒ 把弃牌堆**洗回**牌堆；
        /// false ⇒ 改走 `BattleManager.ResolveFatigue()`（疲劳）。
        /// 读点唯一 = `BattleManager._ResolveDrawCard_d__432__MoveNext.c:67`。
        /// 🔴 **⚠️ 我们不实现这个字段** —— 我们的抽空**一律走疲劳**（`P1 牌库抽空 —— 疲劳 N 点伤害`）。
        ///    而经典模式原版的真值是 **true（洗回）** ⇒ **这是一条既有的偏离，与遭遇模式无关**，
        ///    先如实记着（它不影响本字段在遭遇里的取值：遭遇也是 false）。
        /// ⚠️ 遭遇那条取 false 来自原版文案 `No mulligan` 的**同族推断**（不换牌、也不洗回），
        ///    不像 `showMulligan` 那样有直接读点支撑。</summary>
        public bool shouldReshuffle = true;
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
        /// <summary>⛔ **未解**（原版别的机制）。两边都填 0 —— **不假装知道**。</summary>
        public int alternativeTraitLogic = 0;

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
                // 🔴 **遭遇也有加时，而且「更早」—— 靠的就是同一个 10**（用户 2026-09-26 定案）。
                //    遭遇 `manaPerTurn = 2` + `manaAccumulation = 1` ⇒ 后手 MaxEnergy 走 **4 → 7 → 9 → 11**
                //    （**跳过 10**）⇒ 第 4 个自己的回合就够阈值。⚠️ 所以判定那边必须是 `>=`（「达到**或超过**」）——
                //    写成 `== 10` 的话遭遇**永远进不了加时**。效果与经典相同：进入后双方每回合抽 2 张、当回合就抽。
                //    ⛔ 这里一度被改成 `null`（「遭遇没有加时」）—— 那是照用户**当时看错的一次观察**改的，
                //    用户当天已更正「是我搞错了」。全过程留痕 → `overtimeTurn` 字段的注释。
                v.overtimeTurn = ClassicOvertimeTurn;
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
                v.showMulligan = false;                        // 【反编译】教程不跑换牌阶段（见上面那段）
                // ⚠️ **没动的**（原版值在服务器、本地查不到）：startingMana / startingManaSecond /
                //    manaPerTurn / overtimeTurn / shouldReshuffle —— 一律沿用经典值。
                //    判据只到这里为止，⛔ 别按「教程大概应该……」去改。
                _tutorial = v;
                return _tutorial;
            }
        }
        static GameplayVariables _tutorial;
        /// <summary>【TutorialScenario】`startingHand = 3`（原版那个类，不是 `GameplayVariablesData`）。</summary>
        public const int TutorialStartingHand = 3;
        /// <summary>【TutorialScenario】`secondExtraCards = 0` —— **经典是 1**，
        /// 教程这一档「后手不额外多抽」（我们把它当 0 用）。</summary>
        public const int TutorialSecondExtraCards = 0;
        /// <summary>【TutorialScenario】`maxCardsInHand = 10`（与经典同值）。</summary>
        public const int TutorialMaxCardsInHand = 10;

        /// <summary>按模式取实例。**15 档里只有 `Skirmish 13` 用遭遇那一套**，
        /// 🆕 2026-10-17 起 **`Tutorial 4` 与 `TutorialReplay 9` 用教程那一套**，其余 12 档全走经典 ——
        /// 这是我们**只有三份实例**的必然结果（原版那 18 个值是**服务器按模式下发**的，
        /// `资料/加时与冲突模式_原版规格.md` §2.5 ⇒ 本地抄不到第三份）。
        /// 判据：15 档里带「12 张 · 4 传说 · 督军 −10 生命 · 无换牌」这套的只有 `Skirmish`
        /// （= `FastMode`，原版 `MatchType.FastMode 200` 那一支）；`OfflinePractice 6` /
        /// `OwnDeckTraining 12` 是**用自己 30 张的牌打练习**，`Classic 0` 是排位 ⇒ 都不是快攻那套。
        /// 教程那两档的判据见 <see cref="Tutorial"/>。
        /// ⚠️ **这里是「模式号 → 规则参数」的唯一一处**：⛔ 别在别处再写 `mode == Skirmish ? … : …`。</summary>
        public static GameplayVariables For(GameMode mode)
        {
            if (mode == GameMode.Skirmish) return Skirmish;
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
}
