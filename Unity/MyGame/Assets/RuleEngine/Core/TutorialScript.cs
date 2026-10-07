// TutorialScript.cs — **教程局**：关卡数据 + 执行器状态机 + 白名单闸门 + 引擎级开关
//
// ============================ 判据（唯一正本 = 反编译）============================
// 全部从 `d:/2/tools/decomp_full/` 现读（2026-10-17，写手代理 B29）：
//   · 状态机：`AiScripted__PlayScriptedTurn.c`（逐条执行 + 指针 ++）
//             · `AiScripted__UpdateTurn.c`（回合变大 ⇒ 指针归零）
//             · `AiScripted__ResetActionCounter.c` · `AiScripted__FinishedScriptedActionsInTurn.c`
//             · `AiScripted__CheckIfWaitingForPlayerAction.c` · `AiScripted__IsNextActionScriptedPlayerAction.c`
//             · `AiScripted__GetCurrentScriptedAction.c` · `BattleManager__GetCurrentTurnScriptedData.c`
//   · 白名单闸门：`BattleManager__CheckIfPlayerActionPermittedInTutorial.c`
//             + `ScriptedActionData__CheckIfMatchesActionData.c`（「谁打谁」逐条比）
//   · 关卡开关：`MatchData__ShouldShuffleDeck.c`（100/140 ⇒ 不洗）
//             · `BattleManager__GetPlayerGoesFirst.c`（100 ⇒ 关卡 `playerStarts`）
//             · `BattleManager__SetupStartingTroops.c`（+ 与 `GetInitialHandsCards` 交叉验证档位）
//             · `BattleManager__CreatePlayerDeck.c`（起手卡插牌库顶 + 抽 N 张）
//             · `BattleManager__SetupInitialMana.c`（模式基数 + 关卡增量；`GetInitialDamage`）
//             · `BattleManager._TutorialStartSequence_d__592__MoveNext.c`（**不跑换牌阶段**）
//   · 字段偏移：`d:/2/tools/il2cpp_out/dump.cs`（`TutorialStage` @42050 · `ScriptedAction` @42111 ·
//     `ScriptedActionData` @42146 · `TurnScriptedData` @42091 · `BattleActionType` @45306 ·
//     `BattleManager` @30709 的 `playerManager 0xC0 / enemyManager 0xC8 / playerMinionManager 0xE0 /
//     enemyMinionManager 0xE8 / playerGoesFirst 0x247 / aiScriptedmanager 0x250`)
//   · 数据：`Assets/RuleEngine/Resources/tutorial_stages.json`（`工具/gen_tutorial_stages.py` 生成，**别手改**）
//
// 🔴 **档位约定（本项目最容易搞反的一处，已经在四个函数上逐一核过）**：
//   原版那条链上「哪一侧」的 flag **`0` = 敌方 / `1` = 玩家**，四处一致：
//   `CreateStartingTroop(param_3)` → 1 ⇒ `playerManager 0xC0` + `playerMinionManager 0xE0`；
//   `GetInitialHandsCards(param_3)` → 1 ⇒ `TutorialStage +0x98`(= `playerStartingTroopsInHand`)；
//   `CreatePlayerDeck(param_2)` → 1 ⇒ `+0xF0` 玩家牌库 / `+0xD0` 玩家手牌 / `+0x98`；
//   `SetupInitialMana(param_2)` → 1 ⇒ `+0xA8`(= `startingPlayerMana`)。
//   ⛔ **别再按 `SetupInitialMana` 表面上那句「param_2 == 0 取 startingMana」去反推谁是谁** ——
//      它真正的判据是**玩家/敌方 manager 的字段名**（`dump.cs` @30709），不是参数名。
using System;
using System.Collections.Generic;

namespace RuleEngine
{
    // ==================================================================
    //  一、原版枚举（逐字照 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/*.cs` 签名桩）
    // ==================================================================

    /// <summary>`ScriptedActionType`（`Assembly-CSharp/ScriptedActionType.cs`）—— 脚本动作的**大类**。
    /// 🔴 **运行时读的是它（int）**，`ScriptedAction.actionType` 那串显示名是**派生物**
    /// （`ScriptedAction__UpdateName.c` = `Enum.ToString(data[0].actionType)` ＋按 sub 追加括号）
    /// ⇒ ⛔ **别去解析那串文字**。</summary>
    public enum ScriptedActionType
    {
        None = 0,
        DrawCard = 10,
        PlayCard = 20,
        Attack = 30,
        AttackFreeMode = 31,
        ChangeToRanged = 35,
        ChangeToMelee = 36,
        ActiveAbility = 40,
        PlayerChat = 50,
        PlayerChatBig = 55,
        AiChat = 60,
        AiChatBig = 65,
        PlayerChoice = 70,
        SmallTip = 80,
        RadioMessage = 90,
        EndTurn = 100,
        ClickCard = 110,
        TapCard = 120,
        ShowManaAura = 130,
        ResolveCard = 140,
        ContinueSmallTip = 150,
        LandWarlords = 160,
        ActivateHandCards = 170,
    }

    /// <summary>`ScriptedActionUnit` —— 「谁」。
    /// ⚠️ `30/31/32` 三个都指**玩家侧的部队**（`AiScripted__GetActingCard.c`：
    /// `2 < iVar1 - 0x1e` 不成立 ⇒ 30/31/32 走同一条 `GetPlayerMinionsAndRemnantInPlay`）；
    /// 区别只在**落点在督军左边吗**（见 `Matches`）。</summary>
    public enum ScriptedActionUnit
    {
        None = 0,
        PlayerWarlord = 10,
        AiWarlord = 20,
        PlayerMinion = 30,
        PlayerMinionLeft = 31,
        PlayerMinionNotLeft = 32,
        EnemyMinion = 40,
        PlayerCardInHand = 50,
        EnemyCardInHand = 60,
    }

    /// <summary>`BattleActionType`（`dump.cs:45306`）—— **玩家实际做出来的那个动作**的枚举。
    /// 白名单闸门「谁打谁」那一步的第一个实参就是它
    /// （`ScriptedActionData.CheckIfMatchesActionData(BattleActionType, …)`）。
    /// 🔴 **只抄到 91 号**（原版全表）；本工程**只有下面这几档会真的传进来**，
    /// 其余留在这里是为了「对着原版那张表读闸门时不用猜」：
    /// `attack 1` · `playCardFromHand 5` · `endTurn 14` · `changeAttack 19` ·
    /// `playActiveAbility 30` · `attackContinuation 53` · `useWaystone 76`。</summary>
    public enum BattleActionType
    {
        undefined = 0,
        attack = 1,
        createMinion = 2,
        drawCard = 3,
        minionDeath = 4,
        playCardFromHand = 5,
        showEnemyCard = 6,
        reorderMinionsInPlay = 7,
        doDamage = 8,
        doMultiDamage = 9,
        heal = 10,
        playAnim = 11,
        playHeroPower = 12,
        backlash = 13,
        endTurn = 14,
        addEffect = 15,
        removeEffect = 16,
        stun = 17,
        changeHealth = 18,
        changeAttack = 19,
        summonUnit = 20,
        abilityIconAnim = 21,
        jamUnit = 22,
        createHandCard = 23,
        replaceHeroPower = 24,
        addHandEffect = 25,
        recallToHand = 26,
        transformUnit = 27,
        discardFromHand = 28,
        copyUnitInCemetery = 29,
        playActiveAbility = 30,
        playActiveAbilityAnim = 31,
        mulligan = 32,
        returnToDeck = 33,
        manaBuff = 34,
        refillMana = 35,
        remotePlayerAction = 36,
        stealFromHand = 37,
        exchangeMinion = 38,
        stealMinion = 39,
        growthAnim = 40,
        resetActions = 41,
        forceAttack = 42,
        bloodThirstAnim = 43,
        disappearUnit = 44,
        destroyUnit = 45,
        autoPlayFromHand = 46,
        removeEffectsFrom = 47,
        createDeckCard = 48,
        revealOrders = 49,
        missAction = 50,
        recallToDeck = 51,
        scriptedAttack = 52,
        attackContinuation = 53,
        resign = 54,
        chatMessage = 55,
        disappearFromHand = 56,
        displayFromHand = 57,
        followAfterResolution = 58,
        stealFromDeck = 59,
        removeFromCemetery = 60,
        clearEndOfTurnEffects = 61,
        triggerRelentless = 62,
        triggerSacrifice = 63,
        triggerRequiem = 64,
        triggerCourtesy = 65,
        forceAttackFriendly = 66,
        forceScriptedAttack = 67,
        traitTriggerAnim = 68,
        triggerCodex = 69,
        triggerPlayerCodex = 70,
        setDeckCardTop = 71,
        transformToRemnant = 72,
        reanimate = 73,
        redirectedAttack = 74,
        doMultiDamageAndDestroySelf = 75,
        useWaystone = 76,
        triggerSpiritStone = 77,
        triggerSynapse = 78,
        executeSwarm = 79,
        triggerMob = 80,
        triggerMobAdditional = 81,
        triggerPray = 82,
        triggerAmbush = 83,
        triggerUprising = 84,
        afterResolutionAttackContinuation = 85,
        showCardsWindow = 86,
        resetDuty = 87,
        triggerRegiment = 88,
        triggerSlay = 89,
        triggerTeleport = 90,
        triggerCardPlayedByStimulation = 91,
    }

    // ==================================================================
    //  二、关卡数据的 DTO（字段名**逐字照** `tutorial_stages.json`；JsonUtility 能解）
    // ==================================================================

    /// <summary>一个「谁」。四种列表（起始单位 / 起始手牌 / 动作的 acting·target）形状一样 ——
    /// 动作那两个多带 `unitType`/`unitTypeName`（`ScriptedActionUnit`）。
    /// 🔴 **`ourId` 是我们的卡 id**（生成器按 `SO 名字 → card_stats.ocrName → cards_engine.id` 链解出来的，
    /// `matchTier` 记着它是**逐字命中**还是**靠 ocrName 裁定**）；空串 = 没解出来。</summary>
    [Serializable]
    public class TutorialUnitRef
    {
        public int unitType;
        public string unitTypeName;
        public string pid;
        public string name;
        public string ourId;
        public string matchTier;
        public string note;
    }

    /// <summary>小提示的定位参数（`TutorialTipParams`；我们这一批**只读不算** —— 箭头/高亮是表现层的事）。</summary>
    [Serializable]
    public class TutorialSmallTip
    {
        public int positionReference;
        public int positionRelation;
        public bool showLeftArrow;
        public bool showRightArrow;
        public float tipDuration;
        public bool waitForTip;
        public bool tipWithContinue;
        public bool preventSkipTip;
    }

    /// <summary>一条 `ScriptedActionData` —— **闸门真正比的那一层**。</summary>
    [Serializable]
    public class TutorialActionData
    {
        public int type;                 // ScriptedActionType
        public string typeName;
        public TutorialUnitRef acting;   // actingUnitType / actingUnit
        public TutorialUnitRef target;   // targetUnitType / targetUnit
    }

    /// <summary>一条脚本动作（`ScriptedAction`）。</summary>
    [Serializable]
    public class TutorialAction
    {
        public int index;
        public string type;              // 显示名（**派生物**，别解析）
        public string text;              // 显示名全文
        public string arg;               // 括号里那半
        public float waitBefore;
        public float waitAfter;
        public string textReference;
        public bool isPCTip;
        public bool playerAction;        // ← **闸门读的就是它**
        public bool shouldHighlightElement;
        public TutorialSmallTip smallTipParams;
        public TutorialActionData[] data;

        /// <summary>动作大类（= `data[0].type`）。数据缺失时给 `None`（并**不**静默 —— 调用方会出声）。</summary>
        public ScriptedActionType Kind
        {
            get { return (data == null || data.Length == 0) ? ScriptedActionType.None : (ScriptedActionType)data[0].type; }
        }

        /// <summary>原版 `ScriptedAction.IsActivateHandCards()` —— 闸门里那一条**例外**
        /// （`playerAction == false` 时只放行它）。判据 = 动作大类是第一档 `ActivateHandCards(170)`。</summary>
        public bool IsActivateHandCards { get { return Kind == ScriptedActionType.ActivateHandCards; } }
    }

    /// <summary>一个回合的脚本（`TurnScriptedData`）。</summary>
    [Serializable]
    public class TutorialTurn
    {
        public string turnName;
        public bool scriptedTurn;
        public bool dontDrawCard;
        public bool dontDrawTalent;
        public int actionCount;
        public TutorialAction[] actions;
    }

    /// <summary>一关（`TutorialStage` SO）。**字段名逐字照生成器产物** → 见 `gen_tutorial_stages.py` 的 `fieldSource`。</summary>
    [Serializable]
    public class TutorialStageData
    {
        public int stage;
        public string so;
        public string uniqueIdCampaign;
        public string bg;
        public bool playerStarts;              // ← 先手判据（`GetPlayerGoesFirst`）
        public bool hideCemetery;
        public bool hideCardsLeftInDeck;
        public bool hideLargeCardDisplay;
        public bool hideChat;
        public bool playerAlwaysWins;          // ← 「直接判玩家胜」
        public bool preventPlayerResign;
        public bool skipNormalBattleEndOnVictory;
        public bool skipNormalBattleEndOnDefeat;
        public int startingPlayerMana;
        public int startingEnemyMana;
        public int startingPlayerDamage;
        public int startingEnemyDamage;
        public string playerDeck;              // 预组牌资产名（牌组内容在 `tutorial_decks.json`）
        public string aiDeck;
        public TutorialUnitRef playerDeckForScenarios;
        public TutorialUnitRef[] playerStartingTroops;
        public TutorialUnitRef[] enemyStartingTroops;
        public TutorialUnitRef[] playerStartingTroopsInHand;
        public TutorialUnitRef[] enemyStartingTroopsInHand;
        public TutorialUnitRef playerTurnEndEnchantment;
        public TutorialUnitRef enemyTurnEndEnchantment;
        public TutorialAction actionOnPlayerResign;
        /// <summary>`preMulliganScriptedActions` —— 🔴 **一个 `TurnScriptedData` 对象，不是动作数组**。
        /// 2026-10-17 就地订正（离屏探针抓到的，铁律 5·b）：这里原来写成 `TutorialAction[]`
        /// （照生成器 `fieldSource` 那句「动作组内原序」猜的），而 `tutorial_stages.json` 实测那一格是
        /// `{turnName, scriptedTurn, dontDrawCard, dontDrawTalent, actionCount, actions[]}`
        /// —— **`JsonUtility` 遇到类型不符不会报错，只会把那一格留成 null**（静默）。
        /// ⚠️ 时机：原版 `_StartBattleSequence` 里 `IsPremulliganScriptAvailable` 为真才跑它
        /// （`ExecuteScriptedTurn(preMulliganScriptedActions, …)`）。
        /// 🔴 **实测：6 关的 `scriptedTurn` 全是 `false`** ⇒ `IsPremulliganScriptAvailable`
        ///    （`BattleManager__IsPremulliganScriptAvailable.c`：要求 `+0x18 != 0`）**恒假**
        ///    ⇒ **那 19 条动作一次都不跑**（数据仍在产物里，别以为漏了）。
        ///    ⚠️ 与之相对：`onVictory` 6 关 `scriptedTurn` **全是 true**（13 条动作）—— 那一支**我们还没接**。</summary>
        public TutorialTurn preMulligan;
        public TutorialTurn onVictory;
        public TutorialTurn onDefeat;
        public int turnCount;
        public TutorialTurn[] turns;
    }

    /// <summary>产物文件头（我们只用 `stages`；其余是给人读的判据）。</summary>
    [Serializable]
    public class TutorialStageFile
    {
        public int format;
        public int stageCount;
        public int turnCount;
        public int actionCount;
        public int playerActionCount;
        public TutorialStageData[] stages;
    }

    // ==================================================================
    //  三、关卡数据的装载（**`Core/` 这一半只拿纯数据**）
    // ==================================================================

    /// <summary>
    /// 6 关脚本的**唯一入口**（数据侧）。
    ///
    /// 🔴 **装载那一跳（`Resources.Load` + `JsonUtility.FromJson`）不在本文件里** ——
    ///    它搬去了 <c>Data/TutorialDatabase.cs</c>（与 `Data/CardDatabase.cs` 同族）。
    ///    **为什么必须搬**：本工程有一条成文规矩 —— **`Core/` 只许碰 `UnityEngine.Debug` /
    ///    `UnityEngine.Random`**（`Data/CardDatabase.cs` 文件头写着；离屏探针 `工具/ruleprobe`
    ///    就靠这条，用一份极简桩把 `Core/**` 在 net8 下编起来跑真解析器）。
    ///    2026-10-18 之前本文件**破了半条**：用 `#if UNITY_5_3_OR_NEWER` 把 `Resources`/`JsonUtility`
    ///    包起来 —— 「编得过」但规矩已经破了（`Editor/RuleEngineTest.cs` 里那条源文扫描断言就是这么抓到的）。
    ///
    /// 🔴 **换乘点是下面这个 `partial` 方法** <see cref="LoadFromUnity"/>，两边各自的行为：
    ///    · **Unity 侧**（`Data/TutorialDatabase.cs` 编在**同一个程序集**里，本工程没有 asmdef）
    ///      ⇒ 那一半有实现 ⇒ <see cref="Stages"/> 第一次被读时**真去 `Resources` 装载**；
    ///    · **探针侧**（`工具/ruleprobe` 只编 `Core/*.cs`，`Data/` **不在编译集里**）
    ///      ⇒ **C# 把「只有声明、没有实现」的 `partial void` 连同它的调用点一起抹掉**
    ///        ⇒ 这里原样落到「出声 + 空数组」，探针照旧能编、能跑（=== 老 `#else` 支那条路）。
    ///    ⚠️ 所以**不许**在 `Core/` 里写回 `Resources` / `JsonUtility` / `TextAsset`（哪怕又包一层 `#if`）。
    /// </summary>
    public static partial class TutorialData
    {
        /// <summary>`Assets/RuleEngine/Resources/tutorial_stages.json`</summary>
        public const string StagesResourcePath = "tutorial_stages";

        static TutorialStageData[] _stages;

        /// <summary>6 关（按 `stage` 升序）。取不到时是**空数组**（不抛异常 —— 调用方自己决定怎么报）。</summary>
        public static TutorialStageData[] Stages
        {
            get { if (_stages == null) Load(); return _stages; }
        }

        /// <summary>按**关号**取（1..6；越界返回 null）。</summary>
        public static TutorialStageData Stage(int stageNumber)
        {
            var all = Stages;
            for (int i = 0; i < all.Length; i++) if (all[i] != null && all[i].stage == stageNumber) return all[i];
            return null;
        }

        /// <summary>`tutorialIndex`（0..5，= 原版 `DemoDeckInfoSO.TutorialIndex`）取关。</summary>
        public static TutorialStageData ByIndex(int index)
        {
            var all = Stages;
            return (index < 0 || index >= all.Length) ? null : all[index];
        }

        /// <summary>手工装数据 —— **探针/自检**用（也是 Unity 那半边装载失败时的兜底进货口）。</summary>
        public static void Install(TutorialStageData[] stages) { _stages = stages; }

        /// <summary>
        /// **Unity 那半边**（`Resources.Load` + `JsonUtility.FromJson`）：成功时它自己 <see cref="Install"/>。
        /// 实现落在 `Data/TutorialDatabase.cs` —— 见类头注那张「谁是哪一半」的表。
        ///
        /// 🔴 这里**只有声明、没有实现**：本文件**单独编**（离屏探针只编 `Core/*.cs`）时，
        ///    C# 会把**这个声明**和**所有对它的调用**一起抹掉（不报错、不执行）
        ///    ⇒ 正好就是原来那条 `#else` 路。
        /// ⛔ **别改成 `public` / 别加返回值** —— 那就成了「必须有实现」的扩展 partial 方法，
        ///    探针立刻编不过（`CS8795`）。
        /// </summary>
        static partial void LoadFromUnity();

        /// <summary>装载。Unity 侧交给 <see cref="LoadFromUnity"/>（它成功时已经把数据 `Install` 进去了）；
        /// 它**没装成**（探针下没有那一半 / 或 Unity 侧那个文件不在编译集里）⇒ **出声**并把
        /// `_stages` 置空数组（不让 `Stages` 反复重试 + 反复报错）。⛔ 不静默返回空。</summary>
        public static void Load()
        {
            LoadFromUnity();
            if (_stages != null) return;
            UnityEngine.Debug.LogError("[Tutorial] 这个环境里没有 `Resources`/`JsonUtility`（离屏探针）"
                + " ⇒ 教程关数据装不上；请用 `TutorialData.Install(stages)` 手工装。"
                + "（Unity 侧由 `Data/TutorialDatabase.cs` 那半个 partial 方法装载 ——"
                + " 它没跑起来就说明那个文件不在编译集里）");
            _stages = new TutorialStageData[0];
        }
    }

    // ==================================================================
    //  四、玩家动作白名单闸门
    // ==================================================================

    /// <summary>
    /// **玩家「正想做的那个动作」** —— 闸门的输入。字段**逐条对应**
    /// `ScriptedActionData.CheckIfMatchesActionData(BattleActionType battleActionType, CardScript actingCard,
    ///  CardScript targetCard, int targetPosition)` 那四个形参。
    /// 🔴 原版拿的是 `CardScript`（有 `get_cardType()` 和那个 `+0x40` 的「是不是玩家侧」），
    ///    我们拿的是**同一件事的三个布尔**（见 `RuleCore`/`BattleDriver` 怎么填）。
    /// </summary>
    public struct TutorialAttempt
    {
        /// <summary>动作大类（原版 `BattleActionType`）。</summary>
        public BattleActionType Action;
        /// <summary>发起者卡 id（`CardDef.Id`；不指定 = null）。</summary>
        public string ActingId;
        /// <summary>发起者是不是督军（原版 `EntityScript.get_cardType(card) == 10`）。</summary>
        public bool ActingIsHero;
        /// <summary>发起者是不是玩家侧的（原版 `CardScript +0x40`）。</summary>
        public bool ActingIsPlayerSide;
        /// <summary>目标卡 id（不指定 = null）。</summary>
        public string TargetId;
        /// <summary>目标是不是督军。</summary>
        public bool TargetIsHero;
        /// <summary>目标是不是玩家侧的。</summary>
        public bool TargetIsPlayerSide;
        /// <summary>`PlayCard` 的落点（原版 `targetPosition`）。
        /// ⚠️ **`0` = 未指定**（照原版那句 `param_5 != 0` 的哨兵；我们的槽 0 是合法格，
        /// 但这里**照抄原版**判 —— 所以「落在 0 号格」时那一条 left/not-left 不判）。</summary>
        public int TargetSlot;
    }

    // ==================================================================
    //  五、执行器（单指针状态机）
    // ==================================================================

    /// <summary>
    /// **教程关卡执行器** —— 原版 `AiScripted`（挂在 `BattleManager.aiScriptedmanager +0x250`）的等价物。
    ///
    /// 形状照原版：**两条指针**（`turn` + `actionCounter`），没有栈、没有分支树：
    ///   · 回合 → `stage.turns[turn - 1]`（`GetCurrentTurnScriptedData`：`List.get_Item(turnScriptedData, turnCounter - 1)`
    ///     且要求那一项 `scriptedTurn == true`）；
    ///   · 动作 → `turn.actions[actionCounter]`；
    ///   · 每执行一条 ⇒ **指针 ++**（`PlayScriptedTurn` 末尾 `*(int *)(param_1 + 0x14) += 1`）；
    ///   · 回合号变大 ⇒ **指针归零**（`UpdateTurn`：只在 `param_2 > 当前` 时写，且同时把 `actionCounter` 清 0）。
    ///
    /// 🔴 **一局一个实例**（`BattleContext.Tutorial`），**不进任何静态缓存** —— 指针是**本局**的状态。
    /// </summary>
    public class TutorialScript
    {
        public readonly TutorialStageData Stage;

        /// <summary>当前回合号（**1 起**；= 原版 `BattleManager.turnCounter +0x3F8` 的口径）。</summary>
        public int Turn { get; private set; }

        /// <summary>本回合已跑到第几条（= 原版 `AiScripted +0x10`）。</summary>
        public int ActionCounter { get; private set; }

        // ---- 记账（断言与「如实出声」用）----
        /// <summary>真的执行过几条动作。</summary>
        public int ExecutedActions;
        /// <summary>**引擎侧没实现**的动作按大类计数 ⇒ 收工/报告里要如实列出来（不许静默失败）。</summary>
        public readonly Dictionary<ScriptedActionType, int> Unhandled = new Dictionary<ScriptedActionType, int>();
        /// <summary>已经为哪些大类**出过声**（同一条只吼一次，别把日志刷爆）。</summary>
        readonly HashSet<ScriptedActionType> _warned = new HashSet<ScriptedActionType>();

        public TutorialScript(TutorialStageData stage) { Stage = stage; }

        /// <summary>本回合的脚本（`null` = 这一回合没有脚本 —— 原版那时闸门**放行**）。</summary>
        public TutorialTurn CurrentTurn
        {
            get
            {
                if (Stage == null || Stage.turns == null) return null;
                int i = Turn - 1;                       // 原版拿的是 turnCounter - 1
                if (i < 0 || i >= Stage.turns.Length) return null;
                var t = Stage.turns[i];
                return (t != null && t.scriptedTurn) ? t : null;    // 原版要求 `scriptedTurn == true`
            }
        }

        /// <summary>本回合当前那一条（= 原版 `GetCurrentScriptedAction` 的 `ElementAtOrDefault(actionCounter)`）。</summary>
        public TutorialAction CurrentAction
        {
            get
            {
                var t = CurrentTurn;
                if (t == null || t.actions == null) return null;
                return (ActionCounter < 0 || ActionCounter >= t.actions.Length) ? null : t.actions[ActionCounter];
            }
        }

        // ------------------------------------------------------------------
        //  指针
        // ------------------------------------------------------------------

        /// <summary>原版 `AiScripted.UpdateTurn(turnNumber)`：**只在变大时写**，
        /// 且写的那一刻把 `actionCounter` 一起清零。</summary>
        public void UpdateTurn(int turnNumber)
        {
            if (Turn >= turnNumber) return;
            Turn = turnNumber;
            ActionCounter = 0;
        }

        /// <summary>原版 `AiScripted.ResetActionCounter()`（`*(param_1 + 0x10) = 0`）。</summary>
        public void ResetActionCounter() { ActionCounter = 0; }

        /// <summary>原版 `AiScripted.FinishedScriptedActionsInTurn`：
        /// `turn.actions.Count &lt;= actionCounter`。⚠️ 没有回合脚本时原版会**抛**（那条路走不到）——
        /// 我们返回 `true`（= 跑完了），因为「没有脚本」在我们的调用序列里是合法状态。</summary>
        public bool FinishedScriptedActionsInTurn
        {
            get
            {
                var t = CurrentTurn;
                if (t == null || t.actions == null) return true;
                return t.actions.Length <= ActionCounter;
            }
        }

        /// <summary>原版 `AiScripted.CheckIfWaitingForPlayerAction`（**照抄它的守卫**：
        /// `actionCounter + 1 &lt; count` 才读 `actions[actionCounter].playerAction`）。</summary>
        public bool CheckIfWaitingForPlayerAction
        {
            get
            {
                var t = CurrentTurn;
                if (t == null || t.actions == null) return false;
                if (ActionCounter + 1 >= t.actions.Length) return false;
                var a = t.actions[ActionCounter];
                return a != null && a.playerAction;
            }
        }

        /// <summary>原版 `AiScripted.IsNextActionScriptedPlayerAction`（守卫是 `actionCounter &lt; count`，
        /// 读的同样是 `actions[actionCounter]`）。</summary>
        public bool IsNextActionScriptedPlayerAction { get { var a = CurrentAction; return a != null && a.playerAction; } }

        /// <summary>
        /// 原版 `AiScripted.PlayScriptedTurn`：**执行当前那一条，然后指针 ++**；
        /// 返回值 = 「本回合还有动作吗」。
        /// ⚠️ 原版执行完要 `SkippableActionWait(GetDelay(...))`（等音效 + `waitBefore/waitAfter`）——
        ///    **那是演出节拍，不在引擎里**（本工程的引擎不持帧）。
        /// </summary>
        public bool PlayScriptedTurn(BattleContext ctx)
        {
            var t = CurrentTurn;
            if (t == null || t.actions == null) return false;
            if (ActionCounter >= t.actions.Length) return false;
            var a = t.actions[ActionCounter];
            ExecuteAction(ctx, a);
            ActionCounter++;
            return ActionCounter < t.actions.Length;
        }

        /// <summary>
        /// 一条动作的**引擎侧那一半**（原版 `AiScripted.ExecuteAction`）。
        ///
        /// 🔴 **原版 22 个分支里只有 7 档会改游戏状态**，其余全是**演出**（聊天/提示/高亮/点击/音效）：
        ///    · 会改状态：`DrawCard 10` · `PlayCard 20` · `Attack 30` / `AttackFreeMode 31` ·
        ///      `ChangeToRanged 35` / `ChangeToMelee 36` · `ActiveAbility 40`（后四档走 `AddAttackAction` /
        ///      `ChangeAttackType` / `AddAutoActionToQueue`）
        ///    · 纯演出：`PlayerChat…` / `SmallTip` / `RadioMessage` / `ShowManaAura` / `ResolveCard` /
        ///      `ActivateHandCards` / `LandWarlords` / `ClickCard` / `TapCard`
        ///      （`ExecuteAction` 里它们都落到 `switchD_…_caseD_20` = **什么都不做**，只留高亮那一段）
        ///    · **没有任何分支**（落到同一条默认出口）：`PlayerChoice 70` · `EndTurn 100` · `ContinueSmallTip 150`
        ///      ⇒ 其中 `PlayerChoice` 的语义**不在这个函数里**：它由
        ///        `AiScripted.PlayerChoiceAction()`（= `actionCounter++`）在**别处**处理。
        ///
        /// ⚠️ **本批的实现边界（如实标）**：引擎能自己做完的只有 `DrawCard`（抽 1 张）；
        ///    **`PlayCard` / `Attack` / `ActiveAbility` / `ChangeToX` 四族还没接** ——
        ///    它们要「按 `ourId` 认出手牌/场上那张卡 + 算出落点/目标」，属于**下一批**的活。
        ///    ⇒ 这里**照旧消费掉这条动作（指针一定要 ++，否则整条链卡死）**，
        ///      但**逐档记账 + 出声**（`Unhandled` / `_warned`）。⛔ 绝不假装执行了。
        /// </summary>
        /// <returns>true = 引擎真的做了点什么；false = 没做（已记账 + 出声）。</returns>
        public bool ExecuteAction(BattleContext ctx, TutorialAction a)
        {
            ExecutedActions++;
            if (a == null) return false;
            switch (a.Kind)
            {
                case ScriptedActionType.DrawCard:
                    // 原版 `case 10`（`AiScripted__ExecuteAction.c:726-736`）：
                    // `AddDrawCard(bm, iVar8 == 10, globalVars + 0xb8, …)` —— 那里的 `iVar8` 在 `:730`
                    // **已经被重赋**成 `scriptedActionData[0].actingUnitType`，而 `AddDrawCard` 的第 2 个实参
                    // 是**「是不是玩家侧」**（`BattleManager__AddDrawCard.c:31-37`：真 ⇒ `playerShuffledDeck(+0x310)`，
                    // 假 ⇒ `enemyShuffledDeck(+0x318)`；与全链那套 flag 一致）。
                    // ⇒ **`acting == PlayerWarlord(10)` ⇒ 抽玩家那一边；否则抽【敌方】那一边**。
                    //   数据实测：9 条 `DrawCard` 里 8 条 `#10`、1 条 `None`（S2 第 8 回合）—— 那一条抽的是 AI。
                    // ⚠️ 原版是**入队**（`Add…Action`），我们是**当场抽** —— 可观测结果相同（那张牌进手牌）。
                    if (ctx != null) RuleCore.Draw(ctx, DrawSide(a));
                    return true;

                case ScriptedActionType.PlayerChoice:
                    // 原版 `AiScripted.PlayerChoiceAction()` = **只有 `actionCounter++`**，
                    // 而 `PlayScriptedTurn` 末尾**已经 ++ 过一次** ⇒ 这里**什么都不做**（别再加一次，
                    // 那会跳掉一条动作）。判据：`AiScripted__PlayerChoiceAction.c`。
                    return true;

                default:
                    Note(ctx, a.Kind);
                    return false;
            }
        }

        void Note(BattleContext ctx, ScriptedActionType kind)
        {
            int n;
            Unhandled.TryGetValue(kind, out n);
            Unhandled[kind] = n + 1;
            if (!_warned.Add(kind)) return;      // 同一档只吼一次
            UnityEngine.Debug.LogWarning("[Tutorial] 第 " + Turn + " 回合的脚本里有「" + kind
                + "」这一档动作，**引擎侧还没接**（这一批只接了 DrawCard / PlayerChoice）"
                + " ⇒ 这一条被**照旧消费掉**（指针 ++，不让整条链卡死），但**它对局面的作用没有发生**。"
                + " 出处 = `资料/普查产出_1017/W_B29_教程执行器.md`。");
        }

        // ------------------------------------------------------------------
        //  白名单闸门
        // ------------------------------------------------------------------

        /// <summary>
        /// 原版 `BattleManager.CheckIfPlayerActionPermittedInTutorial` —— **玩家这个动作准不准做**。
        ///
        /// 逐句照抄（`CheckIfPlayerActionPermittedInTutorial.c`）：
        ///   ① `turnCounter &lt; 1` ⇒ **false**（开局第一帧之前什么都不许）；
        ///   ② **没有当前回合脚本** ⇒ **true**（不管）；
        ///   ③ `actionCounter &gt;= 本回合动作数` ⇒ **true**（本回合的脚本跑完了）；
        ///   ④ 当前那条 `playerAction == false` **且不是 `ActivateHandCards`** ⇒ **false**（**闸**）；
        ///   ⑤ 否则逐条比当前那条的 `scriptedActionData[]`：命中 `PlayerChoice(70)` / `ActivateHandCards(170)`
        ///      ⇒ true；否则走 <see cref="Matches"/>（「谁打谁」）⇒ 命中即 true；
        ///   ⑥ 一条都不中 ⇒ **false**。
        /// </summary>
        public bool PermitsPlayerAction(TutorialAttempt attempt)
        {
            if (Turn < 1) return false;                       // ①
            var t = CurrentTurn;
            if (t == null || t.actions == null) return true;  // ②
            if (ActionCounter >= t.actions.Length) return true;   // ③
            var cur = t.actions[ActionCounter];
            if (cur == null) return false;
            if (!cur.playerAction && !cur.IsActivateHandCards) return false;    // ④
            if (cur.data == null || cur.data.Length == 0) return false;
            for (int i = 0; i < cur.data.Length; i++)          // ⑤
            {
                var d = cur.data[i];
                if (d == null) continue;
                if (d.type == (int)ScriptedActionType.PlayerChoice) return true;
                if (d.type == (int)ScriptedActionType.ActivateHandCards) return true;
                if (Matches(d, attempt)) return true;
            }
            return false;                                     // ⑥
        }

        /// <summary>
        /// 原版 `ScriptedActionData.CheckIfMatchesActionData` —— **逐字照抄的分支表**。
        ///
        /// 三件事依次比：**动作大类** → **发起者** → **目标**（含 `PlayCard` 的左右落点）。
        /// 🔴 原版那两张位掩码是**直接算出来的**（不是抄的近似）：
        ///   · `AttackFreeMode` 那张 `0x20000000080002` → 第 {1, 19, 53} 位
        ///     = `attack / changeAttack / attackContinuation`；
        ///   · 目标那张 `0x1004010040000000` → 第 {30, 40, 50, 60} 位
        ///     = 「**只有这四档目标要判**」（其余 unitType 原版直接 `return 1` = 认）。
        /// </summary>
        public static bool Matches(TutorialActionData d, TutorialAttempt a)
        {
            if (d == null) return false;
            int T = d.type;

            // ---- ① 动作大类 ----
            if (T < 0x25)                                    // < 37
            {
                if (T == (int)ScriptedActionType.PlayCard)
                {
                    if (a.Action != BattleActionType.playCardFromHand) return false;
                    // 落点在督军左边吗（只对 target ∈ {31,32} 判；`TargetSlot == 0` = 未指定 ⇒ 照原版跳过）
                    if (a.TargetSlot != 0 && d.target != null)
                    {
                        int tu = d.target.unitType;
                        if (tu == (int)ScriptedActionUnit.PlayerMinionLeft || tu == (int)ScriptedActionUnit.PlayerMinionNotLeft)
                        {
                            bool left = a.TargetSlot < BoardSpec.WarlordSlot;
                            if (tu == (int)ScriptedActionUnit.PlayerMinionLeft && !left) return false;
                            if (tu == (int)ScriptedActionUnit.PlayerMinionNotLeft && left) return false;
                        }
                    }
                }
                else
                {
                    switch (T - (int)ScriptedActionType.Attack)     // 0x1e = Attack
                    {
                        case 0:  // Attack(30)
                            if (a.Action != BattleActionType.attack && a.Action != BattleActionType.attackContinuation) return false;
                            break;
                        case 1:  // AttackFreeMode(31)
                            if ((int)a.Action > 53) return false;
                            if (!Bit((long)((1L << 1) | (1L << 19) | (1L << 53)), (int)a.Action)) return false;
                            break;
                        case 5:  // ChangeToRanged(35)
                        case 6:  // ChangeToMelee(36)
                            if (a.Action != BattleActionType.changeAttack && a.Action != BattleActionType.attack) return false;
                            break;
                        default:
                            return false;                            // 32/33/34 三档原版落 caseD_2 = 不认
                    }
                }
            }
            else if (T == (int)ScriptedActionType.ActiveAbility)
            {
                if (a.Action != BattleActionType.playActiveAbility) return false;
            }
            else if (T == (int)ScriptedActionType.EndTurn)
            {
                if (a.Action != BattleActionType.endTurn) return false;
            }
            else
            {
                if (T != (int)ScriptedActionType.TapCard) return false;     // 0x78 = 120
                if (a.Action == BattleActionType.useWaystone) return true;
            }

            // ---- ② 发起者（原版只对 10 / 30 / 50 三档做判断，其余 LogError 之后**不判**）----
            int au = d.acting == null ? 0 : d.acting.unitType;
            if (au == (int)ScriptedActionUnit.PlayerWarlord)
            {
                if (!a.ActingIsHero) return false;
            }
            else if (au == (int)ScriptedActionUnit.PlayerMinion || au == (int)ScriptedActionUnit.PlayerCardInHand)
            {
                if (a.ActingIsHero) return false;
                if (d.acting != null && !string.IsNullOrEmpty(d.acting.ourId)
                    && a.ActingId != null && a.ActingId != d.acting.ourId) return false;
            }
            // 其余（None / AiWarlord / EnemyMinion / EnemyCardInHand / 31 / 32）原版**不判** —— 照抄。

            // ---- ③ 目标 ----
            int tu2 = d.target == null ? 0 : d.target.unitType;
            if (tu2 < 0x15)                                              // < 21
            {
                if (tu2 == 0) return true;
                if (tu2 == (int)ScriptedActionUnit.PlayerWarlord)
                {
                    if (!a.TargetIsHero) return false;
                    return a.TargetIsPlayerSide;                         // 原版 `+0x40 != 0` ⇒ 玩家侧
                }
                if (tu2 == (int)ScriptedActionUnit.AiWarlord)
                {
                    if (!a.TargetIsHero) return false;
                    return !a.TargetIsPlayerSide;
                }
                return true;
            }
            if (tu2 > 0x3c) return true;                                 // > 60 ⇒ 原版直接认
            if (!Bit((long)((1L << 30) | (1L << 40) | (1L << 50) | (1L << 60)), tu2)) return true;

            // 30 / 40 / 50 / 60 —— 「必须不是督军 + 认得出是哪一张 + 敌我侧要对」
            if (a.TargetIsHero) return false;
            if (d.target != null && !string.IsNullOrEmpty(d.target.ourId)
                && a.TargetId != null && a.TargetId != d.target.ourId) return false;
            bool wantPlayerSide = (tu2 == (int)ScriptedActionUnit.PlayerMinion
                                || tu2 == (int)ScriptedActionUnit.PlayerCardInHand);
            return a.TargetIsPlayerSide == wantPlayerSide;
        }

        static bool Bit(long mask, int i) { return i >= 0 && i < 63 && ((mask >> i) & 1L) != 0; }

        /// <summary>`DrawCard(10)` 那一档**抽哪一边**（判据与踩过的坑见 `ExecuteAction` 里那条长注）：
        /// `scriptedActionData[0].actingUnitType == PlayerWarlord(10)` ⇒ **0（玩家）**，否则 **1（敌方）**。
        /// 🔴 判据是**发起者**、⛔ **不是**「当前行动方」—— 两者在 S2 第 8 回合那一格上正好相反。</summary>
        public static int DrawSide(TutorialAction a)
        {
            var d = (a != null && a.data != null && a.data.Length > 0) ? a.data[0] : null;
            int acting = (d == null || d.acting == null) ? 0 : d.acting.unitType;
            return acting == (int)ScriptedActionUnit.PlayerWarlord ? 0 : 1;
        }

        /// <summary>`preventPlayerResign`（6 关全 false，机制照做）：true ⇒ 玩家**投降不了**。</summary>
        public bool ResignBlocked { get { return Stage != null && Stage.preventPlayerResign; } }

        /// <summary>`playerAlwaysWins`（6 关全 false，机制照做）。
        /// 判据 = 原版 `BattleManager.GetWinnerAfterBattleEnd`（`GetCurrentTutorialStage().playerAlwaysWins` ⇒ 直接给 10 = 玩家胜）。
        /// 唯一读点 = `RuleCore.CheckWinner`（**一处**，别在别处再判一遍）。</summary>
        public bool PlayerAlwaysWins { get { return Stage != null && Stage.playerAlwaysWins; } }
    }

    // ==================================================================
    //  六、引擎级开关（**每一条都只此一处**）
    // ==================================================================

    /// <summary>
    /// 教程局在**引擎里**会改的那几条开关。⛔ **别在 `RuleCore` 里散着写 `if (tutorial)`** ——
    /// 这里每一条都带原版出处，改一处就够。
    /// </summary>
    public static class TutorialRules
    {
        /// <summary>原版 `MatchData.ShouldShuffleDeck(matchType)`：
        /// `(param != 100 &amp;&amp; param != 0x8c) ? true : false` —— **`Tutorial(100)` 与 `TutorialReplay(140)` 不洗牌**，
        /// 其余 12 档都洗。唯一调用点 = `BattleManager.CreatePlayerDeck`（洗完再插起手卡）。</summary>
        public static bool ShouldShuffleDeck(MatchType matchType)
        {
            return !(matchType == MatchType.Tutorial || matchType == MatchType.TutorialReplay);
        }

        /// <summary>原版 `BattleManager.GetPlayerGoesFirst` 的 `matchType == 100` 那一支：
        /// 取 `PlayerDataManager.currentTutorialStage.playerStarts`（`TutorialStage +0x28`）。
        /// （另一支 `0x8c` 也走同一条；`0x50 EventAI` 恒 true；其余档掷硬币/看 `MatchData`。）</summary>
        public static bool PlayerStarts(TutorialStageData stage)
        {
            return stage != null && stage.playerStarts;
        }

        /// <summary>
        /// 原版 `BattleManager.SetupStartingTroops`：把关卡里的**起始单位**直接摆上棋盘。
        /// · `playerStartingTroops` → 玩家侧（`CreateStartingTroops(…, flag 1)`，落点是
        ///   `MinionManager.MoveMinionIntoSlot(mm, card, 0, /*insert*/1, …)`）；
        /// · `enemyStartingTroops` → 敌方侧（`CreateStartingTroop(…, flag 0)`，逐个）。
        /// 🔴 **摆完要 `ReassembleMinions`**（原版两个分支各调一次）—— 我们这边对应
        /// `Auras.Recompose`（棋盘变了 ⇒ 光环重算，见 `Core/Aura.cs` 那份 9 个写入点清单）。
        /// </summary>
        /// <returns>真的摆上去几个。</returns>
        public static int SetupStartingTroops(BattleContext ctx, TutorialStageData stage, Func<string, CardDef> lookup)
        {
            if (ctx == null || stage == null) return 0;
            int n = 0;
            n += PlaceTroops(ctx, stage.playerStartingTroops, 0, lookup);
            n += PlaceTroops(ctx, stage.enemyStartingTroops, 1, lookup);
            if (n > 0) Auras.Recompose(ctx);
            return n;
        }

        static int PlaceTroops(BattleContext ctx, TutorialUnitRef[] list, int side, Func<string, CardDef> lookup)
        {
            if (list == null) return 0;
            var ps = ctx.Players[side];
            int n = 0;
            for (int i = 0; i < list.Length; i++)
            {
                var d = Resolve(list[i], lookup);
                if (d == null)
                {
                    UnityEngine.Debug.LogWarning("[Tutorial] 起始单位列表里第 " + i + " 项（"
                        + (list[i] == null ? "?" : (string.IsNullOrEmpty(list[i].name) ? "<无名>" : list[i].name))
                        + "）**认不出是卡池里哪一张** ⇒ 这一只不摆（⛔ 不猜一张顶上去）。"
                        + " 判据 = `tutorial_stages.json` 的 `matchTier`/`ourId`。");
                    continue;
                }
                var u = new UnitState(ctx.NewInstance(d), false);
                u.DeployedTurn = ctx.Turn;
                int slot = BoardSlots.Insert(ps, u, 0);      // 原版落点 = 「贴督军那格起往左」，插入语义
                if (slot < 0) { UnityEngine.Debug.LogWarning("[Tutorial] 场上放不下起始单位「" + d.Name + "」"); continue; }
                n++;
                ctx.Log($"教程起始单位：{ps.Name} 的「{u.Name}」直接落场（槽 {slot}）");
                ctx.Emit(EvtKind.Deploy, side, slot, u.Name);
            }
            return n;
        }

        /// <summary>
        /// 原版 `BattleManager.CreatePlayerDeck` 里**教程专有**的那一段：
        ///   ① 把 `{player,enemy}StartingTroopsInHand` 那几张**逐张插到牌库顶**
        ///      （`List.Insert(deck, 0, card)`，按列表**原序**逐张插 ⇒ 插完之后**牌库顶是列表的最后一项**）；
        ///   ② 再**抽 N 张**（N = 那一批的张数，`AddDrawCard` 逐次）⇒ 它们进手牌。
        ///
        /// 🔴 **次序换算**：原版「牌库顶 = 下标 0」（它还拿 `get_Item(deck, 0)` 去关掉牌堆顶那张的显示），
        ///    而我们的 `RuleCore.Draw` 是**从末尾抽**（`rule_core.pop_back` 的口径，见那只函数）。
    ///    ⇒ 要得到**同样的抽牌顺序**，我们把这几张**按原序追加到末尾**：末尾 = 我们的牌库顶。
        ///    验算（列表 [a,b,c]）：原版插完 = [c,b,a,…] ⇒ 抽出来是 c,b,a；我们追加完 = […,a,b,c] ⇒
        ///    也是 c,b,a ✓。
        ///
        /// ⚠️ **原版是「入队抽」不是「当场抽」**（`AddDrawCard`）—— 我们当场抽（本条链上没有动作队列），
        ///    可观测结果相同（那批牌进手牌）。真 Play 发现手牌张数不对时**回来看这里**。
        /// </summary>
        public static int SetupInitialHand(BattleContext ctx, TutorialStageData stage, int side,
                                           TutorialUnitRef[] list, Func<string, CardDef> lookup)
        {
            if (ctx == null || list == null || list.Length == 0) return 0;
            var ps = ctx.Players[side];
            int put = 0;
            for (int i = 0; i < list.Length; i++)
            {
                var d = Resolve(list[i], lookup);
                if (d == null)
                {
                    UnityEngine.Debug.LogWarning("[Tutorial] 起手卡列表里第 " + i + " 项认不出是卡池里哪一张 ⇒ "
                        + "这一张不插（⛔ 不猜）—— 第 " + (stage == null ? 0 : stage.stage) + " 关 "
                        + (side == 0 ? "玩家" : "对手") + " 的手牌会少一张。");
                    continue;
                }
                ps.Deck.Add(ctx.NewInstance(d));      // 追加到末尾 = 我们的「牌库顶」
                put++;
            }
            ctx.Log($"教程起手：{ps.Name} 的 {put} 张指定起手卡插到牌库顶（原版 `CreatePlayerDeck`）");
            int drew = 0;
            for (int i = 0; i < put; i++)
            {
                if (ps.Deck.Count == 0) break;        // 牌库空 ⇒ 别去抽（抽会走疲劳，原版那一路不会到）
                RuleCore.Draw(ctx, side);
                drew++;
            }
            return drew;
        }

        /// <summary>
        /// 原版 `BattleManager.SetupInitialMana(param_1, param_2)` 里那一档：
        /// `MaxEnergy = 模式基数(startingMana/startingManaSecond) + 关卡增量(startingPlayerMana/startingEnemyMana)`，
        /// 另外 `CardScript.GetInitialDamage(督军, startingPlayerDamage/…Damage)` **加在督军身上**。
        ///
        /// ⚠️ **6 关的四个值全是 0**（`tutorial_stages.json` 实测）⇒ 这一段今天**不改变任何局面**；
        ///    仍然照做（铁律 11），并把「真是 0」这件事打出来。
        /// ⚠️ 我们的能量是**每回合由 `BeginTurn` 重算**的 ⇒ 「起始法力」在引擎里只能落成
        ///    **一份每回合都加的增量**（`TutorialScript` 的 `ManaBonus`），见 `RuleCore.BeginTurn`。
        /// </summary>
        public static void SetupInitialManaAndDamage(BattleContext ctx, TutorialStageData stage)
        {
            if (ctx == null || stage == null) return;
            for (int side = 0; side < 2; side++)
            {
                int mana = side == 0 ? stage.startingPlayerMana : stage.startingEnemyMana;
                int dmg = side == 0 ? stage.startingPlayerDamage : stage.startingEnemyDamage;
                var ps = ctx.Players[side];
                if (dmg != 0 && ps.Warlord != null)
                {
                    ps.Warlord.Health -= dmg;
                    ctx.Log($"教程起始伤害：{ps.Name} 的督军先吃 {dmg} 点（原版 `GetInitialDamage`）"
                          + $" —— 剩 {ps.Warlord.Health}");
                }
                ctx.Log($"教程初始法力：{ps.Name} 的模式基数 + 关卡增量 {mana}"
                      + "（本轮 6 关全 0 ⇒ 这一项今天不改变局面，但机制照做）");
            }
        }

        /// <summary>某一侧的起始法力增量（见 <see cref="SetupInitialManaAndDamage"/>）。</summary>
        public static int ManaBonus(TutorialStageData stage, int side)
        {
            if (stage == null) return 0;
            return side == 0 ? stage.startingPlayerMana : stage.startingEnemyMana;
        }

        /// <summary>`ctx.CardPool` 里按 **`CardDef.Id`** 找一张 —— 教程数据里的 `ourId` 就是我们卡表的稳定 id
        /// （`gen_tutorial_stages.py` 就是照 `cards_engine.json` 的 `id` 解的）。
        /// ⚠️ **只按 id 精确匹配**：⛔ 不许退回按名字模糊找（`ocrName` 那两条裁定就是这么来的，
        /// 而它们是「按 PnP 卡图 OCR 出来的名字对上的」，**不是原版明确指认**）。
        /// 找不到 ⇒ 返回 null，调用方**出声**。</summary>
        public static Func<string, CardDef> PoolLookup(BattleContext ctx)
        {
            return delegate (string id)
            {
                if (ctx == null || ctx.CardPool == null || string.IsNullOrEmpty(id)) return null;
                for (int i = 0; i < ctx.CardPool.Count; i++)
                {
                    var c = ctx.CardPool[i];
                    if (c != null && c.Id == id) return c;
                }
                return null;
            };
        }

        /// <summary>`ourId` → `CardDef`。**认不出就返回 null**（调用方负责出声）——
        /// ⛔ 绝不按名字模糊匹配一张「看着像」的顶上去（`ocrName` 那两条裁定就是这么来的）。</summary>
        static CardDef Resolve(TutorialUnitRef r, Func<string, CardDef> lookup)
        {
            if (r == null || string.IsNullOrEmpty(r.ourId) || lookup == null) return null;
            return lookup(r.ourId);
        }
    }
}
