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

    /// <summary>一条动作自带的那支音效（原版 `ScriptedAction.sound`，类型 `SoundAsset`）。
    ///
    /// 🆕 **2026-10-18（`A940` 尾账 · 音效那一档补上）**——**这之前 DTO 里没有它**，
    /// 而 `tutorial_stages.json` 里**每一条动作都已经带着这一格**（生成器一直在写，
    /// 只是 `JsonUtility` 反序列化时**没有对应字段 ⇒ 静默丢掉**，同 `preMulligan` 那次
    /// 「类型不符不报错、只留 null」是同一族）。
    /// 判据：`d:/2/tools/decomp_full/ScriptedActionCampaignData__GetSoundAsset.c` ——
    ///   `clip = GetBundledSound(this, name, bundle)` 写进 `+0x10`、**`volume` 写死 `0x3f800000` = 1.0**。
    /// 🔴 **`volume` 这一格原版运行时【不采信 SO 里的值】** —— `GetSoundAsset` 把它硬编码成 1.0；
    ///   播放端 `BattleManager__PlayNextSoundInQueue.c` 用的是
    ///   `AudioSource.PlayOneShot(clip, AudioListener.volume * soundAsset.volume)`。
    ///   ⇒ 本字段留着**只作记账/对账**（数据里 82/95 条是 `0.0`），**播放时按 1.0**。
    /// ⚠️ 顺手更正一条流传的说法：工程里那句「`sound.volume` 原样保留：0 就是静音」
    ///   （`gen_tutorial_stages.py` 的 `crossCheckNote`）**只对 SO 序列化那一层成立**，
    ///   运行时被 `GetSoundAsset` 覆盖成 1.0 ⇒ **不是静音**（见上面那份 `.c`）。</summary>
    [Serializable]
    public class TutorialSound
    {
        public string pid;       // AudioClip 的 PathID（生成器给）
        public string name;      // 片段名（**没有就播不了**：空串是常态）
        public float volume;     // SO 里那一格；⚠️ 运行时被 `GetSoundAsset` 覆盖成 1.0
    }

    /// <summary>一条脚本动作（`ScriptedAction`）。</summary>
    [Serializable]
    public class TutorialAction
    {
        public int index;
        public string type;              // 显示名（**派生物**，别解析）
        public string text;              // 显示名全文
        public string arg;               // 括号里那半
        /// <summary>🆕 2026-10-18：这一条动作自带的音效（`ScriptedAction.sound`）。
        /// ⚠️ **数据里 462 条动作【全都有】这一格**（95 条有真名字）—— 见 <see cref="TutorialSound"/>。</summary>
        public TutorialSound sound;
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

        /// <summary>
        /// 🔴 🆕 2026-10-18（A938）：**这一处调用压根没有目标卡** —— 原版
        /// `ScriptedActionData.CheckIfMatchesActionData` 的第 4 个形参（`targetCard`）是 **null**，
        /// 而那个函数在目标那一关之前有这么一句：
        /// `cVar3 = UnityEngine_Object__op_Inequality(param_4, 0); if (cVar3 == '\0') return 1;`
        /// （`ScriptedActionData__CheckIfMatchesActionData.c`：`param_4` 就是 targetCard）
        /// ⇒ **目标那一关整段不判、直接认**。
        /// 用它的是「**这一格能不能动手**」那种**还没有目标**的查询
        /// （原版 `CanAttackCard` / `CanUseActiveAbility` / `CanShowPotentialTargets` /
        ///  `EndTurnClick` / `CanPlayCard` 那几处传的都是 null）。
        ///
        /// ⚠️ **默认 `false` = 目标已知、必须判** —— 老的两条断言走的就是这一档，
        ///    ⛔ 别改默认值（改了会让「目标写的是督军、玩家却点了别处」这种越权悄悄通过）。
        /// </summary>
        public bool NoTargetYet;
    }

    // ==================================================================
    //  五、执行器（单指针状态机）
    // ==================================================================

    /// <summary>
    /// 🆕 2026-10-18（A938）：**一条脚本动作在引擎上落成了什么** —— 第三条记账口
    /// （<see cref="TutorialScript.Executed"/>）要的那一份事实。
    ///
    /// 🔴 **为什么不是直接把 `AiAction` 传出去**：`AiAction` 在 `RuleEngine/Data/SimpleAI.cs` 里，
    ///    而 `Core/` **不许引 `Data/`** —— 离屏探针（`工具/ruleprobe`）只编 `Core/*.cs`，
    ///    这里一旦出现 `AiAction`，探针当场编不过（`CS0246`）。⇒ 只带**基本类型 + `Core/` 自己的枚举**，
    ///    由表现层（`BattleDriver`）翻译成 `AiAction` / 录像里的伪 kind。
    ///
    /// ⚠️ `Kind` 是**脚本动作大类**（`ScriptedActionType`），不是 `AiActionKind`。
    /// </summary>
    public struct ScriptedActionDone
    {
        /// <summary>引擎真的做了事的那一档（演出那几档**不发**这一份 —— `ok=false`）。</summary>
        public ScriptedActionType Kind;
        /// <summary>发起方座位（0 = 玩家侧 / 1 = AI 侧）。-1 = 不适用。</summary>
        public int Seat;
        /// <summary>`PlayCard`：打到手牌的**第几张**。其余档 = -1。</summary>
        public int HandIdx;
        /// <summary>`PlayCard`：请求的落点格；`Attack` / `ChangeTo*`：发起者所在格。其余 = -1。</summary>
        public int Slot;
        /// <summary>`Attack`：目标在哪一方。其余 = -1。</summary>
        public int TargetP;
        /// <summary>`Attack`：目标格。其余 = -1。</summary>
        public int TargetSlot;
        /// <summary>`Attack`：这一刀用的是不是远程。其余 = false。</summary>
        public bool Ranged;
        /// <summary>`ChangeToRanged` / `ChangeToMelee`：改成哪一档（<see cref="UnitState.AttackTypeMelee"/>
        /// / <see cref="UnitState.AttackTypeRanged"/>）。其余 = 0。</summary>
        public int AttackType;
    }

    /// <summary>
    /// **教程关卡执行器** —— 原版 `AiScripted`（挂在 `BattleManager.aiScriptedmanager +0x250`）的等价物。
    ///
    /// 形状照原版：**两条指针**（`turn` + `actionCounter`），没有栈、没有分支树：
    ///   · 回合 → `stage.turns[turn - 1]`（`GetCurrentTurnScriptedData`：`List.get_Item(turnScriptedData, turnCounter - 1)`
    ///     且要求那一项 `scriptedTurn == true`）；
    ///   · 动作 → `turn.actions[actionCounter]`；
    ///   · 每执行一条 ⇒ **指针 ++**（`PlayScriptedTurn` 末尾 `inc [rbx+0x10]` ——
    ///     ⚠️ **教程口径是 `+0x10`**；`+0x14` 是**战役**那份重载的偏移，⛔ 别混（2026-10-18 更正，铁律 5，
    ///     判据 = VA 反汇编 `0x180944060`，见 `PlayScriptedTurn` 的注释）；
    ///     🔴 **而且只有「脚本自己执行」的那一支才 ++** —— 玩家自己做的那一条**不执行、不 ++**）；
    ///   · 回合号变大 ⇒ **指针归零**（`UpdateTurn`：只在 `param_2 > 当前` 时写，且同时把 `actionCounter` 清 0）。
    ///
    /// 🔴 **一局一个实例**（`BattleContext.Tutorial`），**不进任何静态缓存** —— 指针是**本局**的状态。
    /// </summary>
    public class TutorialScript
    {
        // ------------------------------------------------------------------
        //  🆕 2026-10-18（A938）：**第三条记账口**
        // ------------------------------------------------------------------

        /// <summary>
        /// **脚本执行器在引擎边界上发的那一条事件**（形状照 `SimpleAI.Executed`，见
        /// `RuleData/SimpleAI.cs:1152`）。**每执行完一条脚本动作**发一次，带上
        /// <see cref="ScriptedActionDone"/>（引擎上真的做了什么）+ `ok`（有没有改动局面）。
        ///
        /// 🔴 **为什么要有它（工程红线）**：本地录像录的是「**起始条件 + 动作流**」
        /// （判据 → `CardPresentation/Battle/ReplayStore.cs` 文件头）。脚本驱动的这些动作
        /// **既不是玩家挑的、也不是 AI 挑的**（原版没有这个概念）⇒ 不记的话
        /// **教程局录像回放时从那里开始演成另一局**。两个老口（玩家 `BattleDriver.LocalAct` /
        /// AI `SimpleAI.Executed`）一个都盖不到这条链。
        ///
        /// ⚠️ **只有真的改了引擎状态的那几档才带 `ok = true`** —— 演出档（`SmallTip` / 聊天 /
        ///   高亮 / `EndTurn` 那个 no-op）会发一条 `ok = false` 的，**不产生记账动作**。
        /// ⚠️ 订阅方（`BattleDriver.OnTutorialScriptExecuted`）**必须**在 `DetachStaticHooks` 里摘掉
        ///   （见 `BattleDriver.ForEachStaticHook` 那张清单）。
        /// </summary>
        public static System.Action<BattleContext, TutorialAction, ScriptedActionDone, bool> Executed;

        /// <summary>
        /// 🆕 2026-10-18（A938）：**`PlayCard` 落点**的解析口。
        ///
        /// 原版取的是**当时的鼠标位置**再换算成最近的合法格
        /// （`UnityEngine.Rendering.MousePositionDebug.GetMouseClickPosition()` →
        ///  `MinionManager.GetClosestAvailableSlot(mm, 位置)`，`AiScripted__ExecuteAction.c:856-869`）。
        ///
        /// 🔴 **2026-10-18（审查 K5/R9 整改）**：原来这里只写「我们退化成 AI 那套」——
        ///    **产品里是有真鼠标的** ⇒ 那个说法只对**批处理 / 自检**成立。现在两条路分开：
        ///      · **产品（有指针）**：`BattleDriver.Begin` 里装的那个 lambda **照原版取指针**
        ///        → `BoardLayout.TryResolveSlot` 命中哪一格 → `DropLandingSlot` 换成引擎的真实落点；
        ///      · **批处理 / 自检 / 指针不在棋盘上**：`SimpleAI.NextDeploySlot`（= 我们 AI 那套落点）；
        ///      · **没有订阅方**（离屏探针）：<see cref="NearestFreeSlot"/>（离督军最近的空格）。
        ///    ⛔ **后两条都不是原版行为**，只是这条链在那些环境里取不到指针。
        /// </summary>
        public static System.Func<BattleContext, int, int> ResolveDeploySlot;

        public readonly TutorialStageData Stage;

        /// <summary>当前回合号（**1 起**；= 原版 `BattleManager.turnCounter +0x3F8` 的口径）。</summary>
        public int Turn { get; private set; }

        /// <summary>本回合已跑到第几条（= 原版 `AiScripted +0x10`）。</summary>
        public int ActionCounter { get; private set; }

        // ---- 记账（断言与「如实出声」用）----
        /// <summary>真的执行过几条动作。</summary>
        public int ExecutedActions;
        /// <summary>**引擎侧没实现**的动作按大类计数 ⇒ 收工/报告里要如实列出来（不许静默失败）。
        /// ⚠️ **口径只有一条**：「这一档引擎侧**还没接**」。🔴 2026-10-18（A938）**更正**：
        /// 演出档（`SmallTip` / 聊天 / 高亮 / `EndTurn` …）**不算**在这里 —— 它们在原版里
        /// 本来就没有改局面的分支（见 <see cref="Note"/>），把它们记进来会让这一格的含义变成
        /// 「凡是没改局面都算」⇒ **真·没接的那一档会被淹掉**。那些走 <see cref="StageActions"/>。</summary>
        public readonly Dictionary<ScriptedActionType, int> Unhandled = new Dictionary<ScriptedActionType, int>();
        /// <summary>🆕 2026-10-18（A938）：**照原版本来就是「什么都不做」的那几档**（演出 / 高亮 / 音效）
        /// 按大类计数。⚠️ 与 <see cref="Unhandled"/> 分开记才分得清
        /// 「**还没接**」与「**原版就没有**」—— 混在一起，前者会被后者淹掉。</summary>
        public readonly Dictionary<ScriptedActionType, int> StageActions = new Dictionary<ScriptedActionType, int>();
        /// <summary>🆕 2026-10-18（A938）：**认不出「脚本指的是哪一张卡」/ 引擎拒了** 的动作按大类计数。
        /// ⚠️ 与 <see cref="Unhandled"/> 分开记：那是「这一档还没实现」（只有 `ActiveAbility`），
        /// 这是「实现了、但这一条**数据指不明白**」—— 两者都是「局面没变」，但原因不同，混在一起会互相掩盖。</summary>
        public readonly Dictionary<ScriptedActionType, int> Unresolved = new Dictionary<ScriptedActionType, int>();
        /// <summary>已经为哪些大类**出过声**（同一条只吼一次，别把日志刷爆）。</summary>
        readonly HashSet<ScriptedActionType> _warned = new HashSet<ScriptedActionType>();
        /// <summary>已经出过声的「没有执行」理由（`大类|理由`）—— 同一句只吼一次。</summary>
        readonly HashSet<string> _warnedWhy = new HashSet<string>();
        /// <summary>🆕 2026-10-18（审查 K12）：脚本里出现过几条**空动作**（`actions[i] == null`）。
        /// 与 `Unhandled` / `Unresolved` / `StageActions` 分开记 —— 它的成因是「数据里有洞」，不是「没实现」。</summary>
        public int NullActions;
        bool _nullWarned;

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
        /// 🆕 2026-10-18（A938）：原版 `AiScripted.PlayerChoiceAction()` —— **玩家把脚本要求的那一步做完了**
        /// ⇒ 指针 +1。
        ///
        /// 判据（`AiScripted__PlayerChoiceAction.c` 全文两行）：`*(int *)(param_1 + 0x10) += 1;`
        /// 调用点（`BattleManager__FinishResolvingAction.c:168-182`）：先 `CheckIfWaitingForPlayerAction`
        /// 为真（= 当前那条确实是一条**等玩家**的动作）、**再** ++、**再**重启脚本协程。
        ///
        /// ⚠️ **这一点与 `PlayScriptedTurn` 末尾那次 ++ 不是一回事**：那次是「脚本自己做完了一条」，
        ///    这次是「玩家做完了脚本等着他做的那一条」。两处各自加一次，**别合**。
        /// ⚠️ 本方法**只推指针**；「推完之后接着跑脚本」是调用方的事（`BattleDriver.DriveTutorialScript`）。
        /// </summary>
        public void AdvancePlayerAction() { ActionCounter++; }

        /// <summary>
        /// 原版 `AiScripted.PlayScriptedTurn`（**教程**那份重载）：**指针没到末尾才做**；
        ///   · 当前那条是**脚本自己执行**的（`playerAction == false`）⇒ `ExecuteAction` + **指针 ++**；
        ///   · 当前那条是**玩家自己做**的（`playerAction == true`）⇒ **什么都不做、指针也不动**，返回 false。
        /// 返回值 = 「本回合还有动作吗」（原版两条支都返回 false）。
        /// ⚠️ 原版执行完要 `SkippableActionWait(GetDelay(...))`（等音效 + `waitBefore/waitAfter`）——
        ///    **那是演出节拍，不在引擎里**（本工程的引擎不持帧）。
        ///
        /// 🔴 **2026-10-18 更正（铁律 5）：** 本注释原来写「原版**无条件** `ExecuteAction` + `*(+0x14) += 1`，
        ///    玩家动作那一支只是在**调用方**（协程）里被停住」。**实际不是** —— 判据 = 反汇编
        ///    **VA `0x180944060`**（教程那份重载；`decomp_full/AiScripted__PlayScriptedTurn.c` 落盘的
        ///    是**战役**那份、用 `+0x14`，教程那份的 `.c` 在本地产物里**缺失**）：
        /// ```
        /// 1809440ad  cmp  [rbx+0x10], eax        ; ★ +0x10 = actionCounter（教程口径，不是 +0x14）
        /// 180944125  cmp  byte ptr [rax+0x40], r9b   ; ScriptedAction.playerAction@0x40
        /// 180944129  jne  0x180944171                ; playerAction != 0 ⇒ 跳
        /// 18094412b  call 0x180940dc0                ; 否则才 ExecuteAction
        /// 180944130  inc  dword ptr [rbx+0x10]       ; ★ 只有这一支 ++
        /// 180944171  call 0x180944290                ; = ShowPlayerActionAnim
        /// 180944176  xor al,al … ret                 ; ★ return false：没有 ExecuteAction、没有 ++
        /// ```
        /// **错因**：只读了落盘的那份 `.c`（战役重载），没按「`.c` 读不出来 ≠ 拿不到（VA 反汇编）」去反汇编。
        /// **代价**：据此写下过一条「我们偏离了原版、请主对话裁」的取舍 —— **那条取舍不成立**
        ///    （我们的行为**本来就是**照原版的），已在 `BattleDriver` 与交件报告里一并订正。
        /// ⚠️ 原版 `+0x10` / 战役 `+0x14` 这一对偏移，全工程**别再把教程口径写成 `+0x14`**。
        /// ⚠️ **K2（归 A940 表现层，调用点就在这一支里）**：玩家动作那一支原版还会
        ///    `ShowPlayerActionAnim(manager, action)` + 关 `ScreenHighlightPosition`（`0x1809440f3`）；
        ///    本轮**没做**（⛔ 不在本批白名单），调用点在这里。
        /// </summary>
        public bool PlayScriptedTurn(BattleContext ctx)
        {
            var t = CurrentTurn;
            if (t == null || t.actions == null) return false;
            if (ActionCounter >= t.actions.Length) return false;
            var a = t.actions[ActionCounter];
            // 🔴 **守卫在函数里**（照原版 `0x180944125-0x180944176`）：玩家自己做的那一条
            //   ⇒ **不执行、不 ++**，当场返回 false。
            //   ⛔ 别把它挪回调用方 —— 调用方那个判据只用来决定「停 / 放输入」，不是唯一防线
            //   （反例就是本工程自己踩的：`PlayScriptedTurn` 直接调时把玩家那一档**替玩家执行了一次**）。
            if (a == null || a.playerAction) return false;
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
        ///      🔴 **`EndTurn(100)` 在数据里有 73 条，但它在这里是 no-op** —— `ExecuteAction` **没有**
        ///         `case 100`（落到 `:1253` 的通用收尾）。⛔ 别把它实现成「真结束回合」：
        ///         回合推进靠的是 `_PlayAi` 收尾那条 `AddEndTurnAction`（`…_PlayAi_d__399__MoveNext.c` 的
        ///         `case 8`），不是脚本。
        ///
        /// ⚠️ **本批的实现边界（2026-10-18 更新，A938）**：六档里
        ///    `PlayCard(45)` · `Attack(34)` · `AttackFreeMode(1)` · `ChangeToRanged(13)` · `ChangeToMelee(8)` **接上了**；
        ///    `ActiveAbility` **6 关数据里 0 条** ⇒ **只留桩 + 出声**（⛔ 不凭空造）。
        ///    认不出「脚本指的是哪一张卡」时 —— **如实出声、不执行**，指针照旧 ++（整条链不卡死）。
        ///    ⛔ **绝不退回 `list[0]`**（原版 `GetChosenCard` 会那样做，那会**静默打错目标**）。
        /// </summary>
        /// <returns>true = 引擎真的改了局面；false = 没改（已记账 + 出声）。</returns>
        public bool ExecuteAction(BattleContext ctx, TutorialAction a)
        {
            ExecutedActions++;
            // 🔴 **K12（审查）**：`a == null` 原来是**静默**消费掉的（指针照 ++、既不进 `Unhandled` 也不进
            //   `Unresolved`、不出声）—— 那正是「不许静默失败」那条红线要挡的东西（数据里真混进一条空动作，
            //   表现是「这一条什么都没发生」而日志一个字都没有）。⇒ 记账 + 出声。
            if (a == null)
            {
                NullActions++;
                if (_nullWarned) return false;
                _nullWarned = true;
                UnityEngine.Debug.LogWarning("[Tutorial] 脚本里有一条**空动作**（`actions[i]` 是 null）——"
                    + " 这一条**什么都没做**，指针照 ++（不让整条链卡死）。"
                    + " 判据 = `tutorial_stages.json` 的 `actions[]`（生成器产物；真出现就说明生成器或手改出了洞）。");
                return false;
            }
            var done = new ScriptedActionDone { Kind = ScriptedActionType.None, Seat = -1, HandIdx = -1,
                                                Slot = -1, TargetP = -1, TargetSlot = -1 };
            bool ok = ExecuteCore(ctx, a, ref done);
            var cb = Executed;                 // 先取出来：回调里改订阅也不影响这一下
            if (cb != null) cb(ctx, a, done, ok);
            return ok;
        }

        /// <summary>开关本体（`ExecuteAction` 只负责「记账 + 发事件」那两件外套）。</summary>
        bool ExecuteCore(BattleContext ctx, TutorialAction a, ref ScriptedActionDone done)
        {
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
                    done.Kind = ScriptedActionType.DrawCard; done.Seat = DrawSide(a);
                    return true;

                case ScriptedActionType.PlayCard:
                    return RunPlayCard(ctx, a, ref done);

                case ScriptedActionType.Attack:
                case ScriptedActionType.AttackFreeMode:
                    return RunAttack(ctx, a, ref done);

                case ScriptedActionType.ChangeToRanged:
                case ScriptedActionType.ChangeToMelee:
                    return RunChangeAttackType(ctx, a, ref done);

                case ScriptedActionType.ActiveAbility:
                    // 🔴 **6 关数据里这一档是 0 条**（`tutorial_stages.json` 逐条 dump 过）⇒ **只留桩**。
                    //    原版那一支（`ExecuteAction.c:388-609`）走 `AddAutoActionToQueue(playActiveAbility)`，
                    //    我们对应 `RuleCore.UseAbility` —— 现在**没有数据能验**，⛔ 不凭空造一套出来。
                    Stub(ctx, a.Kind, "6 关数据里这一档 **0 条**，本轮只留桩");
                    return false;

                case ScriptedActionType.PlayerChoice:
                    // 原版 `AiScripted.PlayerChoiceAction()` = **只有 `actionCounter++`**，
                    // 而 `PlayScriptedTurn` 末尾**已经 ++ 过一次** ⇒ 这里**什么都不做**（别再加一次，
                    // 那会跳掉一条动作）。判据：`AiScripted__PlayerChoiceAction.c`。
                    // ⚠️ **审查（2026-10-18）**：原来这一支 `return true`，于是 `Executed` 会发一条
                    //   `ok = true, Kind = None` —— 与本类文档「**只有真的改了引擎状态才 `ok = true`**」
                    //   自相矛盾（订阅方会以为「有一条动作落地了」）。⇒ 改成 **`false`**：
                    //   它的语义本来就是「这一档在 `ExecuteAction` 里什么都不做」。
                    return false;

                default:
                    Note(ctx, a.Kind);
                    return false;
            }
        }

        // ------------------------------------------------------------------
        //  六档的实现（2026-10-18 A938）
        // ------------------------------------------------------------------

        /// <summary>`PlayCard(107 条，其中脚本自己执行 45 条)` ——
        /// 原版 `AiScripted__ExecuteAction.c:738-1017`：取 acting 那张牌（数据里全是 `EnemyCardInHand(60)`
        /// = AI 手上那张）→ 用**鼠标位置**换算落点 → `AddAutoActionToQueue(BattleAction{playCardFromHand,…})`。
        /// 我们当场打（本条链上没有动作队列，可观测结果相同）。</summary>
        bool RunPlayCard(BattleContext ctx, TutorialAction a, ref ScriptedActionDone done)
        {
            var d = First(a);
            if (ctx == null || d == null) { Fail(ctx, a.Kind, "这条动作没有 `data[0]`"); return false; }
            int seat = TutorialRules.SideOfUnitType(TutUnitType(d.acting));
            if (seat < 0) { Fail(ctx, a.Kind, "发起者 `actingUnitType` 认不出（`" + TutTypeName(d.acting) + "`）"); return false; }
            string why;
            int handIdx = TutorialRules.FindHandIndex(ctx, d.acting, out why);
            if (handIdx < 0) { Fail(ctx, a.Kind, why); return false; }
            int slot = DeploySlot(ctx, seat);
            if (slot < 0) { Fail(ctx, a.Kind, "这一侧的棋盘上没有可落的位置（`ResolveDeploySlot` 返回 -1）"); return false; }
            int code = RuleCore.PlayCard(ctx, seat, handIdx, slot);
            if (code != RuleCodes.OK) { Fail(ctx, a.Kind, "引擎拒绝了这次出牌：" + RuleCodes.Describe(code)); return false; }
            done.Kind = ScriptedActionType.PlayCard; done.Seat = seat; done.HandIdx = handIdx; done.Slot = slot;
            return true;
        }

        /// <summary>`Attack(61)` / `AttackFreeMode(18)` —— 原版两档**共用同一段 body**
        /// （`AiScripted__ExecuteAction.c:1020-1145` 的 `case 0x1e: case 0x1f:`），差别只在**玩家侧校验**。
        /// 🔴 打法**不是脚本字段**：原版读 `*(actingCard + 0x120)`
        /// （`:1139` 的 `uVar18` → `AddAttackAction`）⇒ 我们走 `RuleCore.DeclareAttackByCurrentType`。
        /// ⚠️ 原版 `targetUnitType` 为 0（没写目标）时 `GetChosenCard` 收 null ⇒ 那一条进 `AddAttackAction`
        /// 会走不到（`param_3 == 0` 早退）。我们照实：认不出目标就**出声 + 不执行**。</summary>
        bool RunAttack(BattleContext ctx, TutorialAction a, ref ScriptedActionDone done)
        {
            var d = First(a);
            if (ctx == null || d == null) { Fail(ctx, a.Kind, "这条动作没有 `data[0]`"); return false; }
            string why;
            int aSide, aSlot;
            if (!TutorialRules.FindBoardUnit(ctx, d.acting, out aSide, out aSlot, out why))
            { Fail(ctx, a.Kind, "认不出**发起者**：" + why); return false; }
            int tSide, tSlot;
            if (!TutorialRules.FindBoardUnit(ctx, d.target, out tSide, out tSlot, out why))
            { Fail(ctx, a.Kind, "认不出**目标**：" + why); return false; }
            if (aSide == tSide)
            { Fail(ctx, a.Kind, "脚本把目标写成了**自己那一侧**的卡（发起方 " + aSide + "）—— 不执行"); return false; }

            bool ranged = RuleCore.CurrentAttackIsRanged(ctx, aSide, aSlot);
            int code = RuleCore.DeclareAttackByCurrentType(ctx, aSide, aSlot, tSide, tSlot);
            if (code != RuleCodes.OK)
            { Fail(ctx, a.Kind, "引擎拒绝了这次攻击：" + RuleCodes.Describe(code)); return false; }
            done.Kind = a.Kind; done.Seat = aSide; done.Slot = aSlot;
            done.TargetP = tSide; done.TargetSlot = tSlot; done.Ranged = ranged;
            return true;
        }

        /// <summary>`ChangeToRanged(17)` / `ChangeToMelee(8)` —— 原版 `:1146-1202` / `:1203-1251`：
        /// 取 acting 那张卡 → `CardScript.ChangeAttackType(card, 2|1, true)`。
        /// 🔴 **这不是「演出」，是 `Attack` 的前置**：`Attack(30)` 读的就是被它改掉的那一格
        /// （`+0x120`，见 `RuleCore.DeclareAttackByCurrentType`）。</summary>
        bool RunChangeAttackType(BattleContext ctx, TutorialAction a, ref ScriptedActionDone done)
        {
            var d = First(a);
            if (ctx == null || d == null) { Fail(ctx, a.Kind, "这条动作没有 `data[0]`"); return false; }
            string why;
            int side, slot;
            if (!TutorialRules.FindBoardUnit(ctx, d.acting, out side, out slot, out why))
            { Fail(ctx, a.Kind, "认不出**要改打法的卡**：" + why); return false; }
            int type = a.Kind == ScriptedActionType.ChangeToRanged
                     ? UnitState.AttackTypeRanged : UnitState.AttackTypeMelee;
            if (!RuleCore.SetCurrentAttackType(ctx, side, slot, type))
            { Fail(ctx, a.Kind, "那一格上没有单位（`SetCurrentAttackType` 返回 false）"); return false; }
            done.Kind = a.Kind; done.Seat = side; done.Slot = slot; done.AttackType = type;
            return true;
        }

        /// <summary>落点（见 <see cref="ResolveDeploySlot"/>）—— 没有订阅方时用 Core 自己的退路。</summary>
        static int DeploySlot(BattleContext ctx, int seat)
        {
            var f = ResolveDeploySlot;
            if (f != null) return f(ctx, seat);
            return NearestFreeSlot(ctx, seat);
        }

        /// <summary>
        /// 🔴 **这一条不是原版行为**（如实标）：原版取的是**当时的鼠标位置**再换算最近合法格
        /// （`AiScripted__ExecuteAction.c:856-869`）。我们这个环境没有真鼠标 ⇒ 退化成
        /// 「**离督军最近的那个空格**」（先左后右）。表现层通常会把 <see cref="ResolveDeploySlot"/>
        /// 换成 `SimpleAI.NextDeploySlot`（AI 那套落点），那时走的不是这一条。
        /// </summary>
        static int NearestFreeSlot(BattleContext ctx, int seat)
        {
            if (ctx == null || seat < 0 || seat > 1) return -1;
            var board = ctx.Players[seat].Board;
            for (int d = 1; d <= BoardSpec.SlotsPerSide; d++)
            {
                for (int k = 0; k < 2; k++)
                {
                    int slot = k == 0 ? BoardSpec.WarlordSlot - d : BoardSpec.WarlordSlot + d;
                    if (!BoardSpec.IsDeployable(slot)) continue;
                    if (board[slot] == null) return slot;
                }
            }
            return -1;
        }

        static TutorialActionData First(TutorialAction a)
        {
            return (a != null && a.data != null && a.data.Length > 0) ? a.data[0] : null;
        }

        static int TutUnitType(TutorialUnitRef r) { return r == null ? 0 : r.unitType; }

        static string TutTypeName(TutorialUnitRef r)
        {
            if (r == null) return "null";
            return (r.unitTypeName ?? "?") + (string.IsNullOrEmpty(r.ourId) ? "" : ("/" + r.ourId));
        }

        /// <summary>「这一档引擎侧还没接」（只有 `ActiveAbility` 走得进来）。</summary>
        void Stub(BattleContext ctx, ScriptedActionType kind, string why)
        {
            int n; Unhandled.TryGetValue(kind, out n); Unhandled[kind] = n + 1;
            if (!_warned.Add(kind)) return;      // 同一档只吼一次
            UnityEngine.Debug.LogWarning("[Tutorial] 第 " + Turn + " 回合的脚本里有「" + kind
                + "」这一档动作，**引擎侧只留了桩**：" + why
                + " ⇒ 这一条被**照旧消费掉**（指针 ++，不让整条链卡死），但**它对局面的作用没有发生**。"
                + " 判据 = `d:/2/tools/decomp_full/AiScripted__ExecuteAction.c:388-609`。");
        }

        /// <summary>「认不出脚本指的是哪一张卡 / 引擎拒了」—— 按**理由**去重出声，并逐档计数。</summary>
        void Fail(BattleContext ctx, ScriptedActionType kind, string why)
        {
            int n; Unresolved.TryGetValue(kind, out n); Unresolved[kind] = n + 1;
            string key = kind + "|" + why;
            if (!_warnedWhy.Add(key)) return;
            UnityEngine.Debug.LogWarning("[Tutorial] 第 " + Turn + " 回合第 " + ActionCounter + " 条「" + kind
                + "」**没有执行**：" + why
                + " ⇒ 这一条被消费掉（指针照 ++），但局面**没有变**。⛔ 不拿别的卡顶上去"
                + "（原版 `GetChosenCard` 认不出会退回 `list[0]` —— 那会**静默打错目标**，我们不照抄）。");
        }

        /// <summary>原版 `AiScripted.PlayScriptedTurn` 的 `default` 出口 = 纯演出档，什么都不做。
        /// ⚠️ **不记账也不出声**：那是**照原版做的**（原版也没分支），不是「没接」。
        /// 🔴 **2026-10-18（A938）**：计数从 `Unhandled` 挪到 `StageActions` 了 ——
        ///    原来那一条断言（B29）断的是「`AttackFreeMode` 进 `Unhandled`」，而 `Unhandled` 的
        ///    本意是「引擎侧**没实现**」；演出档混进去之后，**真·没接的那一档会被淹掉**
        ///    （6 关数据里演出档有 180+ 条，`Unhandled` 永远非空）。</summary>
        void Note(BattleContext ctx, ScriptedActionType kind)
        {
            int n;
            StageActions.TryGetValue(kind, out n);
            StageActions[kind] = n + 1;
            if (!_warned.Add(kind)) return;      // 同一档只吼一次
            UnityEngine.Debug.LogWarning("[Tutorial] 第 " + Turn + " 回合的脚本里有「" + kind
                + "」这一档动作 —— 它在原版 `ExecuteAction` 里**本来就没有改局面的分支**"
                + "（演出 / 高亮 / 音效那一类，`AiScripted__ExecuteAction.c:1253` 的通用收尾）"
                + " ⇒ 引擎侧什么都不做**是对的**，**这不属于「还没接」**。"
                + " 若你以为它是「还没接」，那是记错了 —— 未接的那几档只有 `ActiveAbility`（见 `Unhandled`）。");
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
            // 🔴 **先看「这一处调用有没有目标卡」** —— 原版 `:…` 那句 `if (param_4 == null) return 1;`
            //    就在这个位置（发起者那一关之后、目标那一关之前）。⛔ 挪到任何别处都会改变语义。
            if (a.NoTargetYet) return true;

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

        // ==================================================================
        //  🆕 2026-10-18（A938）：脚本动作的「谁 / 打谁」→ 我们的座位与格号
        // ==================================================================

        /// <summary>
        /// `ScriptedActionUnit` → **我们的座位**（判据 = `AiScripted__GetActingCard.c` 那张表：
        /// `10 PlayerWarlord → GetHero(bm,1)` · `20 AiWarlord → GetHero(bm,0)` ·
        /// `30/31/32 → GetPlayerMinionsAndRemnantInPlay` · `40 → GetEnemyMinionsAndRemnantInPlay` ·
        /// `50 → GetFriendlyHandRef` · `60 → GetEnemyHandRef`）。
        ///
        /// 🔴 **「玩家侧 = 座位 0」**：原版那条链上 `playerManager` 是 `BattleManager +0xC0`
        /// （我们 `ctx.Players[0]`），而教程局**必定**是玩家持 0 号位 ——
        /// `BattleDriver.BeginTutorial` 第一句就是 `SetMySeat(0)`（`BattleDriver.cs:2172`）。
        /// 返回 **-1 = 这一档认不出**（原版在这里 `CustomDebug.LogWarning` 之后给 null）。</summary>
        public static int SideOfUnitType(int unitType)
        {
            switch ((ScriptedActionUnit)unitType)
            {
                case ScriptedActionUnit.PlayerWarlord:
                case ScriptedActionUnit.PlayerMinion:
                case ScriptedActionUnit.PlayerMinionLeft:
                case ScriptedActionUnit.PlayerMinionNotLeft:
                case ScriptedActionUnit.PlayerCardInHand:
                    return 0;
                case ScriptedActionUnit.AiWarlord:
                case ScriptedActionUnit.EnemyMinion:
                case ScriptedActionUnit.EnemyCardInHand:
                    return 1;
                default:
                    return -1;      // 0 = None / 其余
            }
        }

        /// <summary>
        /// 脚本里的「谁」→ **场上的格号**（`ourId` 精确匹配）。
        ///  · `10 / 20`（督军）：**按类型就定得下来**，不需要 id（原版 `GetHero` 也是按侧取的）；
        ///  · `30 / 31 / 32 / 40`（部队）：按 `ourId` 在那一侧的**非督军**格里找，
        ///    ⚠️ **认不出就返回 false 并给出理由** —— ⛔ **不退回 `list[0]`**
        ///    （原版 `GetChosenCard` 认不出时退回 `list[0]`，那会**静默打错目标**；铁律 2/11 那条「认不出就说认不出」）。
        ///  · 其余类型（手牌 `50/60` 用 <see cref="FindHandIndex"/>）返回 false。
        /// </summary>
        public static bool FindBoardUnit(BattleContext ctx, TutorialUnitRef r, out int side, out int slot, out string why)
        {
            side = -1; slot = -1; why = null;
            if (ctx == null) { why = "没有对局"; return false; }
            if (r == null) { why = "脚本这一格没有写 `unitType`"; return false; }
            switch ((ScriptedActionUnit)r.unitType)
            {
                case ScriptedActionUnit.PlayerWarlord: side = 0; slot = BoardSpec.WarlordSlot; return true;
                case ScriptedActionUnit.AiWarlord:     side = 1; slot = BoardSpec.WarlordSlot; return true;
                case ScriptedActionUnit.PlayerMinion:
                case ScriptedActionUnit.PlayerMinionLeft:
                case ScriptedActionUnit.PlayerMinionNotLeft:
                case ScriptedActionUnit.EnemyMinion:
                    break;
                case ScriptedActionUnit.PlayerCardInHand:
                case ScriptedActionUnit.EnemyCardInHand:
                    why = "这一条要的是**手牌**里的卡，不是场上的（`" + r.unitTypeName + "`）"; return false;
                default:
                    why = "`unitType = " + r.unitType + "` 这一档原版是 `LogWarning` + null（认不出）"; return false;
            }
            side = ((ScriptedActionUnit)r.unitType == ScriptedActionUnit.EnemyMinion) ? 1 : 0;
            if (string.IsNullOrEmpty(r.ourId))
            {
                why = "数据里这一条的 `ourId` 是**空的**（原版 SO 那个字段没名字 ⇒ 产物解不出我们的卡 id）"
                    + "，脚本写的是「" + (string.IsNullOrEmpty(r.name) ? "<无名>" : r.name) + "」"
                    + " —— 认不出是哪一张就**不猜**";
                return false;
            }
            var board = ctx.Players[side].Board;
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = board[s];
                if (u == null || u.IsWarlord || u.Card == null) continue;
                if (u.Card.Id == r.ourId) { slot = s; return true; }
            }
            why = "场上**没有**「" + r.ourId + "」（" + (string.IsNullOrEmpty(r.name) ? "?" : r.name)
                + "）这一张 —— 脚本说的那张此刻不在 " + (side == 0 ? "玩家" : "AI") + " 那一侧";
            return false;
        }

        /// <summary>
        /// 脚本里的「谁」→ **手牌下标**（`50` 玩家手牌 / `60` 敌方手牌）。规则同
        /// <see cref="FindBoardUnit"/>：`ourId` 精确匹配，认不出就 false + 理由（⛔ 不退回 `list[0]`）。
        /// ⚠️ 同一张卡在手牌里有多份时取**下标最小的那一份** —— 原版 `GetChosenCard` 也是按列表顺序取第一个。</summary>
        public static int FindHandIndex(BattleContext ctx, TutorialUnitRef r, out string why)
        {
            why = null;
            if (ctx == null) { why = "没有对局"; return -1; }
            if (r == null) { why = "脚本这一格没有写 `unitType`"; return -1; }
            int side = SideOfUnitType(r.unitType);
            if (side < 0) { why = "`unitType = " + r.unitType + "` 认不出是哪一侧的手牌"; return -1; }
            var kind = (ScriptedActionUnit)r.unitType;
            if (kind != ScriptedActionUnit.PlayerCardInHand && kind != ScriptedActionUnit.EnemyCardInHand)
            { why = "`" + r.unitTypeName + "` 不是手牌那一档"; return -1; }
            if (string.IsNullOrEmpty(r.ourId))
            {
                why = "数据里这一条的 `ourId` 是**空的**（脚本写的是「"
                    + (string.IsNullOrEmpty(r.name) ? "<无名>" : r.name) + "」）—— 认不出是哪一张就不猜";
                return -1;
            }
            var hand = ctx.Players[side].Hand;
            for (int i = 0; i < hand.Count; i++)
                if (hand[i] != null && hand[i].Card != null && hand[i].Card.Id == r.ourId) return i;
            why = (side == 0 ? "玩家" : "AI") + "手上**没有**「" + r.ourId + "」（"
                + (string.IsNullOrEmpty(r.name) ? "?" : r.name) + "）这一张";
            return -1;
        }
    }
}
