// BattleDriver.cs — 规则引擎 ←→ 表现层的**唯一连接点**
//
// 对应 `CardEffects` 在特效那边的角色：`CardPresentation` 不知道规则，`RuleEngine` 不知道画面，
// 两边都只跟这里打交道。
//
// 职责：
//   · 开一局（两个阵营的牌组）→ 把引擎状态同步成卡牌视图
//   · 玩家输入：拖手牌上场（**费用不够拖不上去**）/ 点自己的单位选中 / 点敌方单位攻击 / 结束回合
//   · 对手回合：跑 `SimpleAI`，每步之间留个延时，让玩家看得清
//   · HUD：回合归属、能量、结束回合按钮、胜负
using System.Collections.Generic;
using CardPresentation;
using CardPresentation.Net;      // 🆕 联机（`NetBattle` / `NetPendingBattle` / `NetProtocol`）
using DG.Tweening;               // 🆕 2026-09-30：reveal 那段「飞到 HUD 钮」用 `DOMove/DOFade`（同 `CardTween`）
using RuleEngine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;     // 🆕 2026-10-01：`Volume`（后期下游 `BattlePostFx`）

namespace CardPresentation
{
    public class BattleDriver : MonoBehaviour, INetBattleHost
    {
        /// <summary>🔴 **打完一局之后的出口落点** —— 原版 `BattleManager.MAIN_MENU_SCENE_NAME`
        /// （`dump.cs:30712` = `private const string MAIN_MENU_SCENE_NAME = "MainMenu Warpforge";`），
        /// 由 `BattleManager.LeaveBattle()` → `EverguildSceneManager.LoadScene(…)`（`LeaveBattle.c:58`）加载。
        /// 本仓那个场景的文件名 = **`MainMenu`**（`Assets/CardPresentation/Scenes/MainMenu.unity`，
        /// 已在 Build Settings 里）—— **与壳那条路是同一个名字**：`Deck/DeckRuntime.cs` 的 `BackToMenu` 的
        /// `SceneManager.LoadScene("MainMenu")`。⛔ 别在这里另立一个名字。
        /// ⚠️ 原版是 `MainMenu Warpforge`、我们是 `MainMenu` —— 差的是**场景命名**（本仓场景清单另一套），
        ///    不是「回错地方」：两边都是**主菜单场景**。</summary>
        public const string MainMenuSceneName = "MainMenu";

        // ---- 场景引用（由 BattleScene 建好）----
        public Camera cam;
        public BoardLayout playerBoard;
        public BoardLayout enemyBoard;
        public HandLayout hand;
        public CardInteraction interaction;
        public Transform boardRoot;
        /// <summary>**HUD 的公共根**（`BuildHud` 里建，identity，挂在本对象下）。
        /// 存在的唯一理由是**震镜头**：原版震主相机、不震 UI（HUD 是 Canvas，另在别的相机上）；
        /// 我们单相机 + 世界空间 HUD，要复刻那个观感就得整块 HUD 能整体反向跟一下相机。
        /// 见 `CardFeel.ShakeCamera` 与 `ScreenShake` 那一段注释。</summary>
        public Transform hudRoot;
        /// <summary>**3D 战场的透视相机**（`BoardCamera`，原版值；2026-09-19 起战场是真 3D）。
        /// 只在震镜头时被推一下 —— 3D 战场画在它上面，不推的话挨打时**背景纹丝不动、卡牌在动**。
        /// 没有 3D 战场（退回烘图）时它是 null。见 `CardFeel.ShakeCamera` 的 `extraCam`。</summary>
        public Camera boardCam;
        /// <summary>**场上的卡走不走真 3D 落点**（`ArenaSlots`）。2026-09-20 加。
        /// `BattleScene` 在 3D 战场建出来（`boardCam != null`）时置真。
        /// ⚠️ **退回烘图时必须为 false** —— 那种情况下 `ArenaLayer` 上没有任何相机，
        /// 把卡挂过去就是**两台相机都不画**（静默消失，最坏的一种失败）。</summary>
        public bool use3DBoard;
        public BattleBackdrop backdrop;
        /// <summary>攻击方式选择器（原版 `Drag Attack Selector`）。没有就退化成无按钮（自检里能空跑）</summary>
        public AttackSelector selector;
        /// <summary>选目标反馈：准星 + 弧线（原版 `NoCanvas2D/Attack Target Reticle`）。没有就只靠卡面高亮</summary>
        public TargetReticle reticle;
        /// <summary>技能卡面板（原版 `ActiveSkillDesc`）。没有就不显示技能详情</summary>
        public SkillPanel skillPanel;

        // ---- 🆕 2026-10-12（A175）：`Auto Zoom` 那格的消费者（原版 `CombatAutoZoom`）----
        //
        // 原版那个组件住在**战场场景**里（13 个战场各一份，`MonoBehaviour_5231.json` 等），
        // 引用同一场景里的 `CombatCameraZoom`；`BattleManager._FinishMulliganFinalPhase` 在换牌结束那一刻
        // 调它的 `Initialize()`。我们的战场是**运行时实例化**的 prefab（`ArenaPrefabData` 只带
        // 雾/环境光/相机光学那套值，没有脚本组件那一层）⇒ 挂在 driver 上，是等价落点：
        // 它需要的外部两样 —— **战场相机**（`boardCam`）与**棋盘人数**（`Ctx.Players[*].Board`）—— 都在这里。
        // ⚠️ `boardCam == null`（退回烘图那一档）⇒ **不建**并出声：那时没有 3D 战场相机，缩放无处可落。
        CombatAutoZoom _autoZoom;
        /// <summary>上一轮喂给消费者的两侧人数（原版那两条 `currentEnemySize/currentPlayerSize` 的
        /// 「上一次值」）——**初值必须是 0**，与原版新场景里的零初始化一致：换牌阶段场上没人，
        /// 原版也就**不会**发第一次事件（发了的话开局取景会当场跳一下）。</summary>
        readonly int[] _autoZoomSeen = { 0, 0 };
        bool _autoZoomNoCameraNoted;

        /// <summary>`Auto Zoom` 的消费者（**自检口**，原版那件组件在战场场景里 —— 见上面那段注释）。
        /// 没有 3D 战场相机时是 null。</summary>
        public CombatAutoZoom AutoZoom { get { return _autoZoom; } }

        // ---- 阵营配色（只用在**没有阵营卡框图**时的占位卡面上；有原版卡框就轮不到它）----
        public static readonly Color EmberColor = new Color(0.85f, 0.38f, 0.25f);
        public static readonly Color TideColor = new Color(0.28f, 0.55f, 0.85f);

        /// <summary>
        /// 先挑的两个**原版阵营**（2026-09-12 用户指定：13 个一次铺开风险太大，先做通两个）。
        /// `Start()` 不带参数就会用这两个开局。
        /// </summary>
        public const string DefaultFactionA = "Ultramarines";
        public const string DefaultFactionB = "Goff";

        /// <summary>
        /// 阵营 → 占位卡面底色。**只有卡框图缺了才用得上**（`CardArt.Frame(faction)` 取到图就整卡换原版三层）。
        /// 我们自己那两个阵营的色是**我们挑的**；原版这 13 个是按阵营主色取的近似值（**不是原版数值**，
        /// 原版是整张卡框图，没有「底色」这个字段）。
        /// </summary>
        public static Color FactionColor(string faction)
        {
            switch (faction)
            {
                case StarterCards.EmberFaction: return EmberColor;
                case StarterCards.TideFaction:  return TideColor;
                case "Ultramarines":    return new Color(0.20f, 0.38f, 0.78f);
                case "Goff":            return new Color(0.35f, 0.62f, 0.24f);
                case "SaimHann":        return new Color(0.85f, 0.75f, 0.30f);
                case "AstraMilitarum":  return new Color(0.45f, 0.50f, 0.33f);
                case "BlackLegion":     return new Color(0.30f, 0.30f, 0.34f);
                case "DarkAngels":      return new Color(0.22f, 0.48f, 0.30f);
                case "Genestealers":    return new Color(0.45f, 0.30f, 0.62f);
                case "Sautekh":         return new Color(0.20f, 0.65f, 0.60f);
                case "Sororitas":       return new Color(0.72f, 0.22f, 0.25f);
                case "Leviathan":       return new Color(0.58f, 0.42f, 0.62f);
                case "TauEmpire":       return new Color(0.30f, 0.62f, 0.75f);
                case "SpaceWolves":     return new Color(0.55f, 0.62f, 0.72f);
                case "EmperorsChildren":return new Color(0.72f, 0.35f, 0.62f);
                default:                return new Color(0.55f, 0.55f, 0.62f);
            }
        }

        /// <summary>灵族 Saim-Hann —— 唯一会显示 <b>灵魂石</b> 那一组的阵营（原版 `RawCardScript.UsesSpiritStone`：
        /// `*(int*)(督军卡 + 0x2c) == 30`）。</summary>
        public const string SpiritStoneFaction = "SaimHann";
        /// <summary>战斗修女 —— 唯一会显示 <b>信仰</b> 那一组的阵营（原版 `UsesFaith`：`+0x2c == 80`）。</summary>
        public const string FaithFaction = "Sororitas";
        /// <summary>暗黑天使 —— 唯一会显示 <b>任务点</b> 那一组 HUD 的阵营（原版 `UsesQuestPoints`：`+0x2c == 110`）。</summary>
        public const string QuestPointsFaction = "DarkAngels";

        /// <summary>
        /// 这一方**要不要显示任务点**（`QuestPointsHolder` + 接片 + `QPText`）。
        ///
        /// 🔴 **2026-09-13 更正（张冠李戴，已修）**：原来我们无条件摆给全部 13 个阵营。
        /// 原版是**按督军的阵营开关**的 ——
        /// **权威出处（机器码级）**：`PlayerManager.ResetMana` 里三条 3 字节 getter 拿
        /// 督军卡的 `rawCard+0x2c` 跟 `CardArmy` 枚举比，实参直接喂 `ManaTypeHolder.Toggle`：
        /// <code>
        ///   83 79 2c XX  0f 94 c0  c3      ; cmp dword [rcx+0x2c], XX  /  sete al  /  ret
        ///   RVA 0x94c3a0 → cmp …,0x1e(30 = SaimHann)    → ToggleSpiritStoneMana（灵魂石）
        ///   RVA 0x94c380 → cmp …,0x50(80 = Sororitas)   → ToggleFaithMana      （信仰）
        ///   RVA 0x94c390 → cmp …,0x6e(110= DarkAngels)  → ToggleQuestPoints    （**任务点**）
        /// </code>
        /// 三个 `Toggle*` **各只有一个调用点**，全在同一个函数（`ResetMana`）里；
        /// `ManaTypeHolder__Toggle.c:17` 就是 `SetActive(gameObject, param_2)`。
        /// 枚举数值见 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardArmy.cs`。
        ///
        /// ⚠️ **别拿运行时 dump 当反证**：`runtime_ui_dump_Battle_Arena_1.tsv:246` 里 QP 是
        ///    `active=True`，但那一局**还没跑 `ResetMana`**（无手牌/无阵营），证明不了什么。
        ///    实况旁证：`资料/战斗规格/战斗重建_0827/video_check_0828/README.md:22`
        ///    「quest_zoom.png = 任务点区（**非 DA 局=无图标**）」。
        ///
        /// ✅ **2026-09-15 更正**：这条原来写「**灵族（灵魂石）和修女会（信仰）这两组我们根本没做** ——
        ///    引擎连计数器都没有，`UI_Gem_Eldar` / `40k_Battle_Display_Faith` 也没进 `Resources/`」——
        ///    **三条全不成立**（那是不知哪一轮留下的旧话）：
        ///    · 计数器在（`RuleEngine/Core/PlayerState.Faith` 信仰 / `PlayerState.SpiritStones` 灵魂石），写读口都全；
        ///      ⚠️ **2026-10-07 现读订正（铁律 5）**：原写 `:48` / `:49` —— 那两行是**注释**
        ///      （`faithMana` / `spiritStoneMana` 的出处说明），真计数器是 `:60` `public int Faith;` /
        ///      `:61` `public int SpiritStones;`（`资料/阵营推进_清单与交接.md:264` 写的 `:60/61` 一直是对的）；
        ///    · 贴图也进了 `Resources/Art/ui/`；
        ///    · 两块 HUD 就在本文件 `BuildHud` 里建（信仰 / 灵魂石那一组），**按值显隐**。
        ///    ⇒ 「阵营资源还没做」这句话**已经没有任何活文件命中**（原来引的那条对账表条目也已删）。
        /// </summary>
        public static bool ShowsQuestPoints(string faction)
        {
            return faction == QuestPointsFaction;
        }

        // ---- 状态 ----
        public BattleContext Ctx { get; private set; }
        /// <summary>本机是几号座位（**绝对编号**）：单机恒 0 · 联机客机 = 1（`SetMySeat` 改它）。
        /// 🔴 它是**唯一**决定「哪一侧画在下面 / `_my*` 认谁」的东西 —— 别在别处再写死座位号。
        /// 判据与由来（为什么不是「两端都当 0 号位」的镜像）→ `资料/联机P2P_设计与交接.md` §五·3。</summary>
        int _me = 0;
        /// <summary>我是几号玩家（0 基）。结算面板判胜负要用。</summary>
        public int MyIndex { get { return _me; } }

        // ==================================================================
        //  联机（正本 → `资料/联机P2P_设计与交接.md` §六 N4）
        //
        //  接法：**本地动作乐观落地**（自己那一回合只有自己在动 ⇒ 「一个行动方 + TCP 有序」
        //  天然保证两端顺序一致）→ 落地后交给 `NetBattle` 发对面；
        //  对面的动作由 `NetBattle` 收下来调 `ApplyRemoteAction` 落到同一个引擎上。
        //  🔴 **不做回滚**（引擎没有快照）：引擎要是拒了对面的动作 ⇒ **出声中止**，不假装没事。
        // ==================================================================

        /// <summary>联机局才有（单机 = null）。</summary>
        public NetBattle Net { get { return _net; } }
        NetBattle _net;
        // 🔴 **2026-10-09（`A1127`）：字段 `_shuffleDecks` 已删** —— 它原来是**产品往字段里写**的一格
        //   （`BeginFromPendingCore` 里 `= !pb.NoShuffle`），与同族的 `ForceFirstSeat`（`A1100`）/
        //   `_noAiMulligan`（`A1110`）是**同一族跨局泄漏**的形状（产品写、无处清）
        //   ⇒ 照那两条的先例收口成 **`Begin(shuffle:)` 显式形参**（判据见那个形参的注释）。
        //   ⚠️ 它当时读的是 `NetPendingBattle.NoShuffle` —— **死字段**（只声明、全仓无赋值 ⇒ 恒 `false`）
        //   ⇒ `shuffle` 恒 `true`，**本改动行为零变化**。判据原文 → `资料/普查产出_第九会话/P3_引擎小改族.md`。
        /// <summary>联机局：**对面换牌不跑 AI** —— 由主机定序后下发（`RuleCore.Mulligan` 会掷 `ctx.Rng`）。
        /// 🔴 **2026-10-19（`A1110`）**：这是**本局**的状态，由 `Begin(noAiMulligan:)` **每局显式复位**
        ///   （调用方 = `BeginFromPendingCore`，联机开局 / 重连重建 / 放录像三档传 `true`）。
        ///   ⚠️ **⛔ 不许在产品别处写它**（尤其别写回 `BeginFromPendingCore` 里原来那一句）——
        ///   它曾经**只被写、无处清** ⇒ 打完一局联机 / 放完一局录像之后的**下一局单机**沿用 `true`
        ///   ⇒ **对面（AI）的换牌整段不跑**（`OpenMulligan` 那道闸），而 `RuleCore.Mulligan` 吃 `ctx.Rng`
        ///   ⇒ 整局随机流跟着变（跨局泄漏，破坏「对局必须可复现」）。</summary>
        bool _noAiMulligan = false;
        /// <summary>自检用（`A1110`）：这一局「对面换牌不跑 AI」这一格的值（每局由 `Begin` 复位）。
        /// ⚠️ 断言**别只断这一格**（那是「看字段被写过」）—— 正判据是**可观测行为**：同一颗种子下，
        ///   先让这台 driver 走一局联机 / 放录像那条路（`BeginFromPendingCore`），再开一局**普通单机**，
        ///   **AI 的换牌必须照旧跑起来**（局面签名与「没先走那一局」时相同）。</summary>
        public bool NoAiMulliganForTest { get { return _noAiMulligan; } }

        public void AttachNet(NetBattle nb)
        {
            // 🆕 2026-10-17（B13）：**换/拆的时候先把旧的摘干净** —— 会话（`NetRuntime.Session`）活得比一局久，
            //    不摘的话上一局那个 `NetBattle` 还挂在同一个会话上，下一局对面一掉线/一离开
            //    就会**多弹一次窗**（说错话）。摘的是它自己那三个回调（`NetBattle.Detach`）。
            if (_net != null && !ReferenceEquals(_net, nb)) _net.Detach();
            _net = nb;
        }

        /// <summary>这一帧该不该由**本机的 AI** 去驱动对面 —— **联机局永远不该**
        /// （对面那一侧是网络的活：`NetTick()` 把对面对作落地）。
        /// 🔴 单独做成一个判据（而不是把 `_net == null` 散在 `Update` 里）：这样自检能**直接问它**，
        ///    盯的是「做判断的那个人」，不是「我另写一份判断」。</summary>
        public bool AiShouldDriveOpponent { get { return _net == null; } }

        /// <summary>🆕 2026-10-17（B17·A901）：**这一帧的对局时钟该不该被「联机闸」按住** ——
        /// 对手掉线期间为真（原版 `ClockManager+0x85` 那一格，判据全文 → `TickClock` 里那段注）。
        ///
        /// 🔴 做成一个**判据**（而不是把 `_net != null &amp;&amp; …` 散在 `TickClock` 里）的理由
        /// 与 <see cref="AiShouldDriveOpponent"/> 一模一样：**自检能直接问它**，而且它同时是
        /// 「**单机局这条闸恒不按下**」的公开证人 —— `_net == null` 时**短路**，与加这一句之前**逐字等价**。</summary>
        public bool NetClockPaused { get { return _net != null && _net.ClockPaused; } }

        /// <summary>🔴🆕 2026-10-18（`A915`）：**对手掉线/等待重连期间 ⇒ 玩家禁操作**。
        ///
        /// 判据 = 原版 `BattleManager__Update.c` 开头那道整帧闸（`:104-115`）：
        ///   `ConnectionStatus`（`+0x28c`）∉ `{0 Connected, 3 Disconnected}` ⇒ **整个 `Update` 返回**
        ///   （唯一例外：`+0x510 matchFinishedAndWaitingToLeave` 为真 —— 那一档是「点一下离开战场」）。
        ///   ⚠️ 那颗重连弹窗**是并行的另一件事**（`WindowsManager.ShowPopUp`，Shell 那一层），
        ///      ⛔ **不是**这道闸；把「有没有弹窗」当判据就错了。
        ///
        /// **我们的取值**：`NetClockPaused`（= `NetBattle.ClockPaused`）—— 原版
        /// `BattleManager__ShowDisconnectionPopup.c:27` 那一刻就是 `ClockManager.PauseClock()`，
        /// 而 `NetBattle` 的 `ClockPaused` 正是照它做的（判据写在那一段注释里）。
        /// ⚠️ **单机局 `_net == null` ⇒ 恒 false** ⇒ 普通对局一个字节都不变。
        /// ⚠️ **两处落地**（缺一不可）：① 本类 `Update`（棋盘/HUD/回合驱动那一串）；
        ///   ② `CardInteraction.Frozen`（**手牌拖拽不走本类**，那件有自己的 `Update`）。
        /// </summary>
        public bool PlayerInputFrozen { get { return _inputFrozenForTest || NetClockPaused; } }
        bool _inputFrozenForTest;
        /// <summary>自检用：把「禁操作」这一档直接拨过去（批处理里没有真掉线）。
        /// ⚠️ 它**同时**喂给手牌那一层（见 `Begin` 里接的那个委托）⇒ 两处同源，⛔ 别各拨各的。</summary>
        public void SetPlayerInputFrozenForTest(bool on) { _inputFrozenForTest = on; }
        /// <summary>自检用：现在是不是「玩家禁操作」那一档。</summary>
        public bool PlayerInputFrozenForTest { get { return PlayerInputFrozen; } }

        /// <summary>`NetBattle` 要往提示行写一句人话（`SetHint` 是私有的，别处拿不到）。</summary>
        public void NetSay(string s) { SetHint(s); }

        /// <summary>本地动作的唯一包装：**先抓面板答案**（引擎一结算就把队列吃空了）→ 落地 → 上报对面。
        /// 单机下 `_net == null` ⇒ 与老代码**一字不差**。</summary>
        int LocalAct(AiAction act, System.Func<int> apply)
        {
            if (_net != null) _net.CaptureLocalAnswers(Ctx, act);
            // 🆕 2026-09-27（录像）：**面板答案要在 `apply()` 之前抓** —— 引擎一结算就把队列吃空了
            //   （与 `NetBattle.CaptureLocalAnswers` 同一条规矩，只是我们这份单机也要）。
            int[] picks = (_rec != null && Ctx.ChoosePicks.Count > 0) ? Ctx.ChoosePicks.ToArray() : null;
            string[] pickIds = (_rec != null && Ctx.ChooseCardIds.Count > 0) ? Ctx.ChooseCardIds.ToArray() : null;
            int code = apply();
            // 🆕 2026-10-17（A904）：**引擎动作一落地就清账**（报「引擎替你挑了几处」+ 清掉没人取的答案）。
            //   放这里 = 玩家所有动作（出牌 / 攻击 / 技能 / 收集灵魂石）**同一个口**，不会漏；
            //   ⚠️ 必须在 `RecAct` 之前（那个用上面那份**执行前快照**，与这里无关）也行，
            //   但排在 `apply()` 紧后面最清楚：一次引擎调用 = 一次清账。
            ReportUnaskedChoices();
            if (_net != null && code == RuleCodes.OK) _net.OnLocalAction(act);
            if (code == RuleCodes.OK) RecAct(act, _me, picks, pickIds);      // 录像：落地成功才记
            return code;
        }

        /// <summary>
        /// 把权威动作流里的**一条**落到引擎上（引擎那一段在 `NetApply.Apply` —— **只有那一份**，
        /// 联机自检也走它）。**座位是绝对的**（0 = 主机 / 1 = 客机）⇒ 这里**不翻任何座位**
        /// （镜像那套已作废，见 `NetProtocol.Fingerprint` 的注释）。
        /// </summary>
        public int ApplyLoggedAction(MsgAction m)
        {
            if (Ctx == null || m == null) return RuleCodes.ErrBadHand;
            if (NetApply.EndsTurn(m)) ClearSelection();
            int code = NetApply.Apply(Ctx, m, _me, s => Debug.Log("[Net] " + s));
            if (code != RuleCodes.OK) return code;

            if (NetApply.IsMulliganDone(m))
            {
                if (_mulligan != null) _mulligan.Close();
                if (interaction != null) interaction.enabled = true;
                ResetClock();
                SetHint("换牌完成，开打");
            }
            if (NetApply.EndsTurn(m)) { _aiTimer = aiStepDelay; _aiSteps = 0; _aiRejected.Clear(); }

            // 🆕 2026-09-30：**进攻卡那一条落地之后，环境要补跑一次**。
            //   为什么：本机是**后手**时，自己那条防御卡先落地、`BeginBattleAfterSetup` 已经跑过一遍，
            //   而那时**先手方那条进攻卡还没到**（`Ctx.OffensiveChosen` 还是 false ⇒ 环境没应用）
            //   ⇒ 它到了之后必须补跑，否则**后手那台永远不换环境**（两端画面/指纹都不一样）。
            //   ⚠️ `BeginOffensiveRevealOrApply` 自己有闩（`_offensivePhaseApplied`），重复调是安全的。
            if (NetApply.IsOffensivePick(m)) BeginOffensiveRevealOrApply();

            RefreshAll(); UpdateHud(); ReportUnaskedChoices();
            if (NetApply.EndsTurn(m) || NetApply.IsMulliganDone(m)) NetAfterTurnStart();
            // 🔴 **2026-10-19（`A1099`）**：**权威流也要进录像** —— 这一句补上的是联机局里
            //   **对面那一半**（对手的动作 + 换牌定序那三条伪动作），它们**只从这条口落地**
            //   （`NetBattle.cs` 那三个调用点），原来一个账都没有 ⇒ 联机录像只有本机那一半。
            // 🔴 **位置=本方法【末尾】**（`return` 之前）：`PlayReplay` 那次逐条对轨迹的 `DeepHash`
            //   就发生在**本方法返回之后** ⇒ 放末尾与它取**同一个程序点**；放中间会因为
            //   `RefreshAll / UpdateHud / NetAfterTurnStart` 的调用次序而**理论上**产生偏差
            //   （今天单机路径证明这几处不改哈希，但没必要冒这个险）。
            // ⚠️ **代价（如实说）**：它排在 `code != RuleCodes.OK` 那道早退**之后** ⇒
            //   被引擎拒的动作**不记**（那也对：录像记的是「**真的落了地的**什么」，
            //   回放时才不会在同一个地方再被拒一次、把分叉点往后推）。
            RecMsg(m);
            return code;
        }

        /// <summary>`INetBattleHost`：对面投降 ⇒ 本机判胜（`Ctx.ForfeitedBy` 记成对面）。
        ///
        /// <para>🆕 **2026-10-18（A913）**：多带一个**理由码**（原版 `DeadHero(bool isPlayerDying, BattleResult)`
        /// 的第二个实参）。本口**服务三档不同的理由**，所以码必须由调用方给：
        /// · 收到对面那条 `NetKind.Resign` ⇒ <see cref="BattleResult.Forfeit"/>(2)
        ///   （原版 `ReceiveEnemyForfeit.c:37` 就是写死的 2）；
        /// · 对面掉线倒计时到点 ⇒ <see cref="BattleResult.Disconnect"/>(3)
        ///   （原版 `_d__322__MoveNext.c:144` `DeadHero(对面, 3)`）；
        /// · 对面没投降就把台关了（收到 `bye`）⇒ 也是 3（原版那一档本来就走掉线那条链）。
        /// ⚠️ 码**只进 `ctx.Events` 的日志**（`RuleCore.Forfeit` 里那两句），⛔ 不进制指纹/回放。</para></summary>
        public void NetRemoteResign(BattleResult reason)
        {
            if (Ctx == null || Ctx.IsOver) return;
            RecRaw(RecKindForfeit, 1 - _me);          // 🆕 录像：投降也是一条要重放的动作
            RuleCore.Forfeit(Ctx, 1 - _me, reason);
            RefreshAll(); UpdateHud();
        }

        /// <summary>🆕 **2026-10-18（A914 第四续）**：`INetBattleHost.NetSelfResign` —— **本机自己**判负。
        ///
        /// <para>什么时候用：**本机**这一端掉了线、30 秒重连没成功（`NetBattle.ReconnectCountdownExpired`
        /// 里「本机是断线那一端」那一支）。判据 = 原版
        /// `BattleManager__FailedToReconnectAfterDisconnect.c:26-27`：
        /// `AddResignAction(param_1, 1)` + `DeadHero(param_1, 1, 3)` —— 第二个实参 `1` = **本机死**
        /// （对比：对面掉线那一端是 `…_d__322__MoveNext.c:144` 的 `DeadHero(对面, 3)`）。</para>
        ///
        /// <para>🔴 与 <see cref="NetRemoteResign"/> 的差别**只有座位号**（`_me` vs `1 - _me`）——
        /// 记账口**同一套**（`RecRaw(RecKindForfeit)` = 原版 `AddResignAction` + 本地录像那一口）。</para>
        ///
        /// <para>🆕 **2026-10-18（A913）**：同样多带一个**理由码**。今天唯一调用点 =
        /// `NetBattle.ReconnectCountdownExpired` 的「本机是断线那一端」⇒ <see cref="BattleResult.Disconnect"/>(3)
        /// （原版 `FailedToReconnectAfterDisconnect.c:26-27` 的 `DeadHero(param_1, 1, 3)`）。</para></summary>
        public void NetSelfResign(BattleResult reason)
        {
            if (Ctx == null || Ctx.IsOver) return;
            RecRaw(RecKindForfeit, _me);              // 录像：与上面同一族，只是演员是**本机**
            RuleCore.Forfeit(Ctx, _me, reason);
            RefreshAll(); UpdateHud();
        }

        // ==================================================================
        //  本地录像（用户 2026-09-27 拍板：「做，我们需要录像」）
        // ==================================================================
        //
        // 🔴 **这是加功能、不是复刻** —— 原版整套在 PlayFab 服务器上（判据 →
        //    `资料/普查产出_0927/回放_入口与数据链.md`）。我们录的是**「动作流 + 起始条件」**
        //    （与原版 `BattleRecordData` 同一个形状），落地与重放**走联机那两条现成的路**：
        //    落一条 = `NetApply.Apply`（唯一实现）· 重放一串 = `ApplyLoggedAction`。
        //
        // 🔴 **每一处会动引擎的地方都要记**（漏一处 = 回放**悄悄走样**）—— 清单（改动时照这张表核对）：
        //    ① **AI 换牌**（`OpenMulligan` 里那条，`!_noAiMulligan` 才跑）
        //    ② **玩家换牌**（`OnMulliganDone`）
        //    ③ **换牌结束**（同处，`EndMulligan` + `BeginTurn`）
        //    ④ **玩家动作**（全部走 `LocalAct` —— 出牌 / 技能 / 攻击 / 收集灵魂石）
        //    ⑤ **AI 动作**（`DriveAiTurn` 里 `SimpleAI.ExecuteAction` 那条）
        //    ⑥ **回合推进**（两条**都不走 `LocalAct`**：AI 那条 + 玩家 `EndTurn` 那条）
        //    ⑦ **投降**（`RecKindForfeit`，走 `NetApply` 之外的一条，因为那边没有这个 kind）
        //    ⑧ 🆕 **2026-10-18（A938）教程脚本自己执行的那几条**（`TutorialScript.Executed` = **第三个口**）——
        //       这条链**既不是玩家挑的、也不是 AI 挑的**，两个老口一个都盖不到 ⇒ 不记的话
        //       教程局录像**从那里开始演成另一局**。`PlayCard` / `Attack` 翻成 `AiAction` 走正常那条；
        //       `DrawCard` / `ChangeTo*` 走两个**伪 kind**（`RecKindTutorialDraw 201` /
        //       `RecKindTutorialAttackType 202`，与 `RecKindForfeit` 同族，见 `PlayReplay` 里那一支）。
        //    ⑨ 🆕（同上）**脚本回合结束**那一跳（`DriveAiTurn` 里教程那一支调 `EndTurnAndAdvance`）——
        //       它本来就在 `EndTurnAndAdvance` 里记（清单 ⑥），**不另记**。
        //    ⚠️ 还有一处**故意不记**：`AutoEndTurnIfStuck` 走的是**同一条** `EndTurn` 路 ⇒ 记在 ⑥ 那一处就够。
        //    ⚠️ 「等玩家」的那 110 条**不在这条链上**：它们由玩家自己做，照旧走 `LocalAct`（清单 ④）。
        //
        // 🔴 **怎么知道录全了**：结算时记下 `NetProtocol.StateHash(Ctx)`；回放完**再算一次对比**，
        //    不一致就**出声**（红线：不许静默失败）—— 那说明有动作没录到，回放看到的是**另一局**。
        /// <summary>录像里的「投降」伪 kind（`NetApply` 那边没有这一种，重放时单独处理）。</summary>
        const int RecKindForfeit = 200;

        ReplayRecord _rec;
        /// <summary>本局正在录的那份（**没在录 = null**）。自检读口。</summary>
        public ReplayRecord Recording { get { return _rec; } }
        /// <summary>最近一次存盘的录像文件名（null = 没存）。</summary>
        public string LastReplayFile { get; private set; }
        /// <summary>要不要录（默认开；自检里关掉就能只跑引擎）。</summary>
        public static bool RecordReplays = true;

        /// <summary>🔴 **录像专用**的强哈希：`NetProtocol.StateHash` 只数**张数**（牌库几张、手牌几张），
        /// **不数牌的身份** —— 洗牌结果不同、或手牌换了一批，它照样相等（2026-09-27 实测：分叉了却报「一致」）。
        /// 这把把「手牌 / 牌库前几张 / 场上单位 / 督军血 / 能量」的身份都算进去，
        /// **只给录像的对账用**（联机那边用 `Fingerprint`，别动它）。</summary>
        static int DeepHash(BattleContext ctx)
        {
            if (ctx == null) return 0;
            unchecked
            {
                int h = 17;
                h = h * 31 + ctx.Turn; h = h * 31 + ctx.Active; h = h * 31 + ctx.Winner;
                // ⚠️ 把「引擎问过几次选择 / 面板答了几次」也算进去 —— 它俩不等就说明 RNG 流已经错位
                //    （`TakePick` 在队列空时走 `ctx.Rng.Next`，多问一次就多抽一次）
                h = h * 31 + ctx.ChooseSites * 131 + ctx.ChooseAnswered * 137;
                for (int p = 0; p < 2; p++)
                {
                    var ps = ctx.Players[p];
                    h = h * 31 + ps.Deck.Count + ps.Hand.Count * 7 + ps.Discard.Count * 13 + ps.Energy * 17;
                    for (int i = 0; i < ps.Hand.Count; i++)
                    {
                        h = h * 31 + (ps.Hand[i] != null && ps.Hand[i].Card != null
                                      ? ps.Hand[i].Card.Id.GetHashCode() : 0);
                        h = h * 31 + (ps.Hand[i] != null ? ps.Hand[i].Id * 211 : 0);   // ⚠️ 实例号也要对
                    }
                    for (int i = 0; i < Mathf.Min(5, ps.Deck.Count); i++)
                        h = h * 31 + (ps.Deck[i] != null && ps.Deck[i].Card != null
                                      ? ps.Deck[i].Card.Id.GetHashCode() : 0);
                    if (ps.Warlord != null) h = h * 31 + ps.Warlord.Health * 101;
                    for (int sl = 0; sl < RuleEngine.BoardSpec.Size; sl++)
                    {
                        var u = ps.Board[sl];
                        h = h * 31 + (u != null && u.Card != null ? u.Card.Id.GetHashCode() : 0);
                        if (u != null) { h = h * 31 + u.Health * 103; h = h * 31 + u.Instance.Id * 223; }   // ⚠️ 实例号
                    }
                }
                return h;
            }
        }

        /// <summary>引擎事件流（`ActionLog`）最后一条的一句话 —— 录像对账用（诊断分叉）。</summary>
        static string EvtTail(BattleContext ctx) { var l = ctx != null ? ctx.ActionLog : null; return l == null || l.Count == 0 ? "<空>" : EvtText(l[l.Count - 1]); }
        static string EvtTailList(BattleContext ctx)
        {
            var l = ctx != null ? ctx.ActionLog : null;
            if (l == null || l.Count == 0) return "<空>";
            var sb = new System.Text.StringBuilder();
            for (int i = Mathf.Max(0, l.Count - 4); i < l.Count; i++) sb.Append(EvtText(l[i])).Append(" | ");
            return sb.ToString();
        }
        static string EvtText(BattleEvent e) { return e == null ? "null" : $"{e.Kind}#p{e.Player}s{e.Slot}<-p{e.TargetPlayer}s{e.TargetSlot}:{e.CardId}"; }

        /// <summary>🆕 2026-09-27：**局面速写** —— 录像对账用（诊断分叉）。
        /// 为什么要有它：哈希只能告诉你「第 N 条之后不一样」，**告诉不了你「哪里不一样」**。
        /// 2026-09-27 实测就是靠它一眼看出「录的时候 P1 的 2 号格站着 `Ravenwing Bikes`，
        /// 回放时那一格是空的」—— 光看两个 `int` 相等/不等是查不出来的。
        /// ⚠️ 只给人看：**不参与任何判据**（`DeepHash` 才是判据）。
        /// ⚠️ 含 `ChooseSites/ChooseAnswered`：这两个数不等就说明「面板问了几次」已经错位
        ///    （`TakePick` 队列空时会多抽一次 `ctx.Rng`，见 `EffectResolver.TakePick` 那一支）</summary>
        static string StateBrief(BattleContext ctx)
        {
            if (ctx == null) return "<无对局>";
            var sb = new System.Text.StringBuilder();
            sb.Append("T").Append(ctx.Turn).Append(" 行动").Append(ctx.Active).Append(" 胜").Append(ctx.Winner)
              .Append(" 问").Append(ctx.ChooseSites).Append("答").Append(ctx.ChooseAnswered)
              .Append(" 队列").Append(ctx.ChoosePicks.Count).Append('+').Append(ctx.ChooseCardIds.Count);
            for (int p = 0; p < 2; p++)
            {
                var ps = ctx.Players[p];
                sb.Append(" ⏐P").Append(p + 1).Append(" 能").Append(ps.Energy)
                  .Append(" 手").Append(ps.Hand.Count).Append(" 库").Append(ps.Deck.Count)
                  .Append(" 弃").Append(ps.Discard.Count).Append(" 任务").Append(ps.QuestPoints)
                  .Append(" 督军").Append(ps.Warlord != null ? ps.Warlord.Health.ToString() : "-")
                  .Append(" [");
                for (int s = 0; s < RuleEngine.BoardSpec.Size; s++)
                {
                    var u = ps.Board[s];
                    sb.Append(s).Append(':');
                    if (u == null) sb.Append('-');
                    else sb.Append(u.Name).Append('(').Append(u.Health)
                           .Append(u.Exhausted ? "·已动" : "").Append(')');
                    sb.Append(' ');
                }
                sb.Append(']');
            }
            return sb.ToString();
        }

        /// <summary>`List` 的安全取值（老录像里没有这个字段 ⇒ 长度对不上时别抛）。</summary>
        static string At(List<string> l, int i) { return (l != null && i >= 0 && i < l.Count) ? l[i] : "<这条没录>"; }

        /// <summary>开局：把「重建这一局要的全部东西」记下来（种子 / 模式 / 双方卡组 / 战场 / 名字 / 先手 /
        /// 🆕 哪一个座位是电脑）。
        /// ⚠️ 收的 `d0`/`d1` 是**座位 0 / 座位 1** 的卡组（`Begin` 那两个参数就是这个口径）。
        /// 🆕 **2026-10-19（`A1087`）**：`botSeat` = **本局实际喂给 `RuleCore.NewBattle` 的那一个值**
        ///   （与 `Begin` 里那句**共用同一个局部量**，⛔ 别在这儿重算一遍 —— 两处各算 = 迟早不一致）。</summary>
        void RecBegin(PlayerDeck d0, PlayerDeck d1, string f0, string f1, int seed, string mode, string arena,
                      int botSeat)
        {
            if (!RecordReplays) { _rec = null; return; }
            LastReplayFile = null;
            _rec = new ReplayRecord
            {
                savedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                mySeat = _me,
                myHero = "", foeHero = "",                 // 结算时补（此刻还没抽出督军）
                // 🆕 2026-10-18（A938）：教程局要把**关号**也记下来（重建这一局少不得它，见那个字段的注释）。
                //   ⚠️ 哨兵是 **`0` = 不是教程局**（不是 `-1`）—— 老录像的 JSON 里没这个键，
                //      读出什么取决于 `JsonUtility` 跑不跑初始化器（**本地无判据**）⇒ 取 `0` 才两条路都安全。
                tutorialStage = _tutorialStage != null ? _tutorialStage.stage : 0,
                // 🆕 2026-10-19（A1087）：开局这一格也要记（存**值 + 2** —— 哨兵纪律见 `ReplayStore.botSeatPlus2`）。
                botSeatPlus2 = botSeat + 2,
                start = new MsgStart
                {
                    seed = seed,
                    mode = mode,
                    arena = arena,
                    myDeckSeat = 0,
                    hostDeckJson = d0 != null ? JsonUtility.ToJson(d0) : "",
                    clientDeckJson = d1 != null ? JsonUtility.ToJson(d1) : "",
                    hostFaction = f0,
                    clientFaction = f1,
                    hostFirst = Ctx != null ? Ctx.FirstSeat : 0,
                    myName = ProfileData.PlayerName,
                    foeName = _net != null ? NetMatchmaking.FoeName : "",
                },
            };
            // 🆕 2026-10-11（A985⑬ / A1295）：录像**头**落一条【只落盘】的结果记录。
            //   ⛔ 不进 `MsgAction`、**不上网** —— 那个结构同时是网络上那个结构，加字段 = 把码送上网（与原版相反）。
            //   开局这一刻还没有结果 ⇒ `Undefined`、`seat` 不写；这条同时充当「这份录像是从头记的」记号。
            ReplayStore.WriteResultHead(_rec, BattleResult.Undefined, -1);
        }

        /// <summary>记一条 `AiAction`（玩家与 AI 共用）。`actor` 是**绝对座位**。</summary>
        void RecAct(AiAction act, int actor, int[] picks = null, string[] pickIds = null)
        {
            if (_rec == null || act == null) return;
            var m = NetProtocol.ToWire(act, null, _rec.actions.Count);
            m.actor = actor;                                   // 🔴 绝对座位（别落在 `ToWire` 的默认值上）
            m.picks = picks; m.pickIds = pickIds;
            _rec.actions.Add(m);
            _rec.trace.Add(DeepHash(Ctx));   // 逐动作轨迹（**总是记**：4 字节/条，它只够定位到第几条）
            // 🔴 **黑匣子**（那两个字符串）**只在 `ReplayStore.VerboseTrace` 开着时记** ——
            //    它们才是「读得出哪里不一样」的那份（见 `StateBrief`），但每局要多十几 KB。
            //    ⇒ 平时关（一局约 9 KB）、**自检与查问题时开**（`ReplayStore.VerboseTrace = true`）。
            if (ReplayStore.VerboseTrace)
            {
                _rec.traceLogTail.Add(EvtTail(Ctx));
                _rec.traceState.Add(StateBrief(Ctx));
            }
        }

        /// <summary>记一条**非 `AiAction`** 的（换牌 / 投降 / 🆕 进攻卡 / 防御卡）。
        /// 🆕 2026-09-30：加了 `envSlot/envSO/defId` 三个 —— 进攻卡/防御卡那两条要走同一条录像流
        /// （否则「两端不一致」和「回放走样」会同时发生）。</summary>
        void RecRaw(int kind, int actor, int[] marks = null,
                    int envSlot = -1, string envSO = null, string defId = null)
        {
            if (_rec == null) return;
            _rec.actions.Add(new MsgAction { seq = _rec.actions.Count, kind = kind, actor = actor, marks = marks,
                                             envSlot = envSlot, envSO = envSO, defId = defId });
            _rec.trace.Add(DeepHash(Ctx));   // 逐动作轨迹（**总是记**：4 字节/条，它只够定位到第几条）
            // 🔴 **黑匣子**（那两个字符串）**只在 `ReplayStore.VerboseTrace` 开着时记** ——
            //    它们才是「读得出哪里不一样」的那份（见 `StateBrief`），但每局要多十几 KB。
            //    ⇒ 平时关（一局约 9 KB）、**自检与查问题时开**（`ReplayStore.VerboseTrace = true`）。
            if (ReplayStore.VerboseTrace)
            {
                _rec.traceLogTail.Add(EvtTail(Ctx));
                _rec.traceState.Add(StateBrief(Ctx));
            }
        }

        /// <summary>🆕 **2026-10-19（`A1099`）**：把**从权威动作流落下来的一条 `MsgAction`** 原样记进录像。
        ///
        /// <para>🔴 **为什么非有它不可**：`ApplyLoggedAction` 是**联机局里对面那一侧唯一的落地口**
        /// （`NetBattle.cs` 三处：主机收客机动作 / 客机收主机广播 / `AppendLogAndApply` 的换牌定序
        /// 那几条伪动作），而它原来**一个记账口都没有** ⇒ 联机录像只录到**本机那一半**
        /// （对手的动作、双方换牌一条都不进）⇒ 灌到第一条就「不是你的回合」、**放不出来**
        /// （`RecKind*` 那几条只在线上走，不走 `LocalAct`）。</para>
        ///
        /// <para>🔴 **只拷内容、`seq` 用录像自己的计数器**（同 <see cref="RecAct"/> / <see cref="RecRaw"/>）：
        /// 线上的 `seq` 是**主机定序**的（客机那份 `Log` 还会补 `null` 占位）⇒ 照抄会把录像的序号打乱。
        /// `preApplied` **不拷** —— 它在录像里恒 `false` = 「回放时这一条要真落地」
        /// （`PlayReplay` 不看它，但带上 `true` 会把这个结构的意思说反）。
        /// ⚠️ **必须新建对象**：`m` 落地之后还会被 `NetBattle` 改字段（`m.seq` / `m.actor` / `m.preApplied`，
        /// 见 `NetBattle.cs` 主机收动作那一支）—— 直接把 `m` 塞进 `_rec.actions` 会被**事后改掉**。
        /// ⚠️ 三个数组字段（`picks` / `pickIds` / `marks`）是**按引用**拷的（与 `RecRaw` 收 `marks` 同一个手法）；
        /// 现有各调用点**没有任何一处就地改这些数组**（只整只替换），所以引用共享是安全的。</para>
        ///
        /// <para>⚠️ **与 `RecAct` / `RecRaw` 的分工**：那两条服务**本机自己**走的路
        /// （`LocalAct` / `SimpleAI.Executed` / 教程执行器 / 投降）；这一条服务**权威流**
        /// （对手那一半 + 换牌定序）。`NetKind.Applied` 对**本机自己**的动作带 `preApplied = true`
        /// ⇒ `NetBattle` 收到后**不落地**、也就走不到这里 ⇒ **不会与本机的 `LocalAct` 双记**。</para></summary>
        void RecMsg(MsgAction m)
        {
            if (_rec == null || m == null) return;
            _rec.actions.Add(new MsgAction
            {
                seq = _rec.actions.Count,
                kind = m.kind, actor = m.actor, handId = m.handId, handIdx = m.handIdx, slot = m.slot,
                targetP = m.targetP, targetSlot = m.targetSlot, ranged = m.ranged, altKeyword = m.altKeyword,
                picks = m.picks, pickIds = m.pickIds, marks = m.marks,
                envSlot = m.envSlot, envSO = m.envSO, defId = m.defId,
            });
            // 逐动作轨迹：与 `RecAct` / `RecRaw` **同一条纪律**（`trace` 与 `actions` 一一对应，
            // `PlayReplay` 拿它定位分叉点）—— 少记一次 = 后面每一条都对不上号。
            _rec.trace.Add(DeepHash(Ctx));
            if (ReplayStore.VerboseTrace)
            {
                _rec.traceLogTail.Add(EvtTail(Ctx));
                _rec.traceState.Add(StateBrief(Ctx));
            }
        }

        /// <summary>结算：补上结果与指纹 → 落盘 → 把文件名交给对局历史那条记录。
        /// ⚠️ **只在这一处落盘**（`Restart()` 会把没打完的那份丢掉，那是**故意**的：原版也只存打完的局）。</summary>
        void RecFinish(string myHero, string foeHero)
        {
            if (_rec == null || Ctx == null) return;
            _rec.myHero = myHero ?? ""; _rec.foeHero = foeHero ?? "";
            _rec.result = Ctx.Winner == 3 ? (int)BattleLogData.Outcome.Draw
                        : Ctx.Winner == _me + 1 ? (int)BattleLogData.Outcome.Victory
                        : (int)BattleLogData.Outcome.Defeat;
            _rec.finalHash = NetProtocol.StateHash(Ctx);      // 回放时拿它验「演的是不是同一局」
            // 🆕 2026-10-11（A985⑬ / A1295）：**落盘之前**把这一局的结束理由码写进**尾**那条记录。
            //   码只活在 `ctx.Events` 里那一行（`[BattleResult] …`）、**不在 `ctx` 的任何字段上**
            //   ⇒ 必须从事件表里取（`LastResultFromEvents`），⛔ 别给 `ctx` 加字段
            //   （那会进 `Fingerprint` 的 `Events.Count`，把回放自证弄坏）。
            var resultCode = ReplayStore.LastResultFromEvents(Ctx.Events, out int resultSeat);
            ReplayStore.WriteResultTail(_rec, resultCode, resultSeat);
            LastReplayFile = ReplayStore.Save(_rec);
            var keep = _rec; _rec = null;
            Debug.Log($"[Replay] 这一局录了 {keep.actions.Count} 条动作 · 终局指纹 {keep.finalHash}");

            // 顺手把录像挂到对局历史那条记录上（对局历史那一行点「回放」就是播它）
            BattleLogData.AttachReplay(LastReplayFile);
        }

        /// <summary>
        /// **放一份录像**（用户 2026-09-27 拍板要的）。
        /// 走的是**重连重放那条路**：从 `MsgStart` 重建这一局 → 把动作逐条灌回引擎
        /// （`NetReplay` 也是这么干的，只是它从网络拿 `MsgStart`）。
        /// 返回 true = 演完了**而且指纹对得上**；false = 没演成 / 演完发现**不是同一局**（都出声）。
        /// </summary>
        public bool PlayReplay(ReplayRecord rec)
        {
            if (rec == null || rec.start == null)
            {
                Debug.LogWarning("[Replay] 这份录像读不出来（`null` 或没有开局头）");
                return false;
            }
            var pb = NetPendingBattle.FromStart(rec.start, isHost: rec.mySeat == 0);
            if (pb == null) { Debug.LogWarning("[Replay] 开局头解不出（种子/卡组对不上）"); return false; }
            Debug.Log($"[Replay] 开播：{rec.savedAt} · {rec.myHero} vs {rec.foeHero} · "
                    + $"{rec.actions.Count} 条动作 · 本机座位 {pb.MySeat}"
                    + (rec.TutorialStageIndex >= 0 ? $" · **教程第 {rec.tutorialStage} 关**" : ""));

            bool keepRecording = RecordReplays;
            RecordReplays = false;                 // 🔴 **放的时候别录**（否则会把回放自己录成新的一局）
            try
            {
                // 🔴 **2026-10-12（A387）：回放局不再借「联机局」那张标签。**
                //   · 原来这里走的是 `BeginFromPendingCore` 的写死值 `"联机局"` ⇒ 提示行成了
                //     「卡组存档读不出来（联机局）—— 本局自动凑了一副」。**回放不是联机局** ——
                //     原版 `MatchType.Replay = 160` 是**独立的一档**（`ReplayHud.Setup` 按
                //     `matchType == 0xA0` 开关那一排回放钮，判据 → `资料/普查产出_0927/回放_入口与数据链.md` §B·0）。
                //   · 传 `null` = 这一档**没有**「卡组存档读不出来」这回事：录像里的牌就是
                //     **录制那一刻**的牌（录进去是空的就是空的），照着演就是了，没什么可交代的。
                //     ⚠️ 真出问题照样出声：录的那副牌**不合法 / 展开不出来**时，`ResolveDeck` 自己会给
                //       `_deckNotice`（还会 `LogError`），那条**不经过** `deckNote`。
                //   · 🔴 **`deckNote` 与「对局记录」无关**（A 表那句「回放写进对局记录的文案错」不准确）：
                //     全仓只有两处用它 —— `Begin` 的 `_deckNotice`（提示行）与 `SetHint`。
                //     `RecordBattleLog`（那一张本地对局记录表）**根本不收这个参数**
                //     （实读 `BattleLogData.Match` 的字段：结果 / 两边督军名 / 玩家名 / 骷髅 / 模式 / 回放文件名）。
                //   · ⚠️ **待判据**：回放局该不该有一句自己的提示（比如「这是回放」）——
                //     我们**没有**这个文案的判据（原版没有这条提示行），**不发明**，留空。
                // 🔴 🆕 **2026-10-18（A938）：教程局【不能】走 `BeginFromPendingCore`。**
                //   它按「种子 + 两副牌」重建 —— 而教程局**少洗一次牌、起始单位与起手由关卡摆**、
                //   连先手都照关卡（`RuleCore.NewBattle` 那六条开关**全读 `ctx.Tutorial`**，
                //   而这条路上 `tutorial` 是 null）⇒ 放出来的**一开始就不是同一局**。
                //   ⇒ 走关卡那条现成的入口 `BeginTutorial(关号)`（录像头里记着关号，见 `ReplayRecord`）。
                //   ⚠️ 之后 `_replaySession = true` 那道闸还会**把执行器自驱停掉** ——
                //     脚本动作已经在动作流里了，再自驱一遍 = 演两遍（`DriveTutorialScript` 里有那一句）。
                // 🆕 **2026-10-19（`A1087`）**：**开局那两格状态都照录像头** —— 先手照 `pb.FirstSeat`（既有）、
                //   「哪一个座位是电脑」照 `rec.BotSeatForNewBattle`（新加的那一格）。
                //   ⛔ **别在这里现算 `AiShouldDriveOpponent`**：**放录像时 `_net` 恒 null** ⇒ 一份记着
                //   「-1（对手是真人）」的录像会被当成「1（座位 1 是电脑）」⇒ `RuleCore.GoesSecondCard`
                //   给后手发**另一张**防御卡、还多掷一次随机 ⇒ 整条动作流错位。
                //   （老录像那一格读出 `null` ⇒ 退回老口径，行为逐字不变。）
                if (rec.TutorialStageIndex >= 0) BeginTutorial(rec.TutorialStageIndex, rec.BotSeatForNewBattle);
                else BeginFromPendingCore(pb, attachNet: false, deckNote: null, botSeatOverride: rec.BotSeatForNewBattle);
                // 🔴 **2026-10-12（A381）：放录像的时候也别【结账】**。
                // ⚠️ **置真必须在 `BeginFromPendingCore` 之后** —— 它里面走 `Begin(...)`，
                //    而 `Begin` 会把 `_replaySession` 清零（新开一局 = 不是回放）。
                // 🔴 **整场回放期间一直留着**（不在 `finally` 里清）：动作是**一口气灌完**的
                //    （`Ctx.IsOver` 在那一趟结束时就是 true），而画面靠 `_timeline` 慢慢演 ——
                //    清早了，下一帧 `Update()` → `UpdateHud()` 就会把这一局**再结一次账**
                //    （第二条对局记录 + 结算面板与开门视频 + 三条每日任务 + 一次骷髅）。
                //    清掉的唯一入口 = 开新一局（`Begin`）。
                _replaySession = true;
                _replayDivergence = -1;      // 🆕 审查（§9·3）：回放前清零 —— 这一格是**断言用的失败位**
                // 🆕 2026-10-11（A985⑬ / A1295）：这一局的结束理由码从**录像尾**那条【只落盘】记录里读。
                //   ⚠️ 老录像没有那两条记录 ⇒ 读出 `Undefined` ⇒ 行为**逐字不变**。
                var replayResultCode = ReplayStore.ReadResultCode(rec);
                for (int i = 0; i < rec.actions.Count; i++)
                {
                    var m = rec.actions[i];
                    if (m == null) continue;
                    if (m.kind == RecKindForfeit)
                    {
                        // 🆕 2026-10-18（A913）：**不拿 `Forfeit`(2) 顶替** —— 那会在重放时
                        //   静默说错一句话：把「掉线判弃权」演成「投降」。
                        // 🔴 **2026-10-11 就地订正（A985⑬ / A1295）：上面原来那句「理由码在录像里
                        //   没有记」【已经不成立】** —— 码现在记在录像**头 / 尾**两条【只落盘】的
                        //   `[BattleResult]` 记录里（⛔ 不进 `MsgAction`、不上网：那个结构同时是
                        //   **网络上**那个结构，加字段 = 把码送上网，与原版相反）。老录像没有那两条
                        //   ⇒ `replayResultCode` 读出 `Undefined` ⇒ **行为逐字不变**。
                        RuleCore.Forfeit(Ctx, m.actor, replayResultCode);
                        continue;
                    }
                    // 🆕 2026-10-18（A938）：脚本那两条**伪 kind**（`DrawCard` / `ChangeTo*`）——
                    //   `AiActionKind` 里没有对应项、`NetApply` 也不认，所以自己落。
                    if (ApplyTutorialRaw(Ctx, m)) continue;
                    int code = ApplyLoggedAction(m);
                    if (code != RuleCodes.OK)
                        Debug.LogError($"[Replay] 第 {i} 条（kind={m.kind}，actor={m.actor}）被拒："
                                     + RuleCodes.Describe(code));
                    // 🔴 **逐条对轨迹**：第一条对不上就是分叉点（光比终局指纹只知道「不一样」、不知道从哪开始）
                    if (rec.trace != null && i < rec.trace.Count)
                    {
                        int fin = DeepHash(Ctx);
                        if (fin != rec.trace[i])
                        {
                            // 🆕 2026-10-18（审查 · 交件 §9·3）：**置失败位**（第一次分叉处）。
                            //   原来这里只 `LogError` —— 那样「轨迹中途分叉、终局 `StateHash` 恰好相等」时
                            //   自检仍会全绿（`StateHash` 只数张数）⇒ 自检口径不闭合。
                            //   现在 `Editor/BattleScene.cs` ⑨ 节**直接断这一格**（`ReplayDivergenceForTest < 0`）。
                            if (_replayDivergence < 0) _replayDivergence = i;
                            Debug.LogError($"[Replay] **分叉点 = 第 {i} 条**（kind={m.kind}，actor={m.actor}，"
                                         + $"handIdx={m.handIdx}，slot={m.slot}，targetP={m.targetP}，"
                                         + $"targetSlot={m.targetSlot}，ranged={m.ranged}，alt={m.altKeyword}）："
                                         + $"录的时候这条做完是 {rec.trace[i]}，现在做完是 {fin}");
                            // 🔴 **2026-09-27 修**：这里原来打的是 `rec.traceLogTail`（**整个 List**）——
                            //    屏幕上只有 `System.Collections.Generic.List`1[System.String]`，等于没打。
                            //    要的是**这一条**那一格（`At(...)` 兼作老录像的越界保护）。
                            Debug.LogError($"[Replay] 录的 log 尾 {At(rec.traceLogTail, i)} ／ 现在 log 尾 {EvtTail(Ctx)}");
                            // 🆕 **局面速写**：哈希看不出「哪里不一样」，这一行看得出。
                            Debug.LogError($"[Replay] 录的局面：{At(rec.traceState, i)}");
                            Debug.LogError($"[Replay] 现在的局面：{StateBrief(Ctx)}");
                            // 🆕 把分叉点前后几条**录下来的动作**摊开 —— 用来判断「录的时候是谁把局面改成那样的」
                            var around = new System.Text.StringBuilder();
                            for (int k = Mathf.Max(0, i - 3); k <= Mathf.Min(i + 2, rec.actions.Count - 1); k++)
                            {
                                var mk = rec.actions[k];
                                if (mk == null) continue;
                                around.Append(k == i ? "▶" : " ").Append(k).Append(":kind").Append(mk.kind)
                                      .Append(" a").Append(mk.actor).Append(" hand").Append(mk.handIdx)
                                      .Append(" slot").Append(mk.slot).Append(" →P").Append(mk.targetP)
                                      .Append('@').Append(mk.targetSlot)
                                      .Append(mk.altKeyword != null ? " [" + mk.altKeyword + "]" : "")
                                      .Append("　");
                            }
                            Debug.LogError($"[Replay] 分叉点附近**录下来的**动作：{around}");
                            Debug.LogError($"[Replay] 回放侧最后几条引擎事件：{EvtTailList(Ctx)}");
                            break;
                        }
                    }
                }
                int got = NetProtocol.StateHash(Ctx);
                bool same = got == rec.finalHash;
                Debug.Log($"[Replay] 演完了：{Ctx.Turn} 回合 · 终局指纹 {got}"
                        + (same ? " = 录制时的 ⇒ **同一局**" : $" ≠ 录制时的 {rec.finalHash} ⇒ **不是同一局**")
                        + (same ? "" : "（说明有动作没录全 —— 如实报出来，别当演对了）"));
                return same;
            }
            finally { RecordReplays = keepRecording; }
        }

        /// <summary>放一份录像（按文件名）。</summary>
        public bool PlayReplay(string fileName)
        {
            var rec = ReplayStore.Load(fileName);
            return rec != null && PlayReplay(rec);
        }


        /// <summary>`INetBattleHost`：`NetBattle` 收到 `resume` 时调它（重连）。</summary>
        public void NetReplayFromNet(MsgStart start, List<MsgAction> actions) { NetReplay(start, actions); }

        /// <summary>重连：**从种子重建 + 全量重放**（正本 §5·6 —— 不许增量补）。
        /// 重放的是**权威动作流里的每一条**（对面那条翻座位、本机那条不翻）。
        ///
        /// <para>🔴 **2026-10-19（`A1099`）：这一趟【也会开一份新录像】，而且是有意为之。**
        /// 语义说清楚（⛔ 不许含糊）：<see cref="RecBegin"/> 的语义**从「开局」扩成「开局 / 重连重建」**
        /// —— 这一趟走 `BeginFromPendingCore` → `Begin` → `RecBegin`，只要 `RecordReplays` 还是真，
        /// 就会**新开一份 `_rec`**（头 = 重建出来的这一局：种子 / 两副牌 / 先手都照开局包）。
        /// 而下面那个循环紧接着把**权威流的每一条**通过 `ApplyLoggedAction` 灌进来，
        /// 那些条目现在**都会进这份新录像**（`A1099` 补的那个记账口）⇒ **这份录像是【从头到尾完整的】**，
        /// 不是从中间开始的残档（结算时 `RecFinish` 照样落盘）。</para>
        ///
        /// <para>⚠️ **为什么这不是副作用、而是正确结果**：录像的契约是「**起始条件 + 动作流**」，
        /// 而重连这一趟的起始条件 = **重建出来的这一局**（= 原局的开局条件）、动作流 = **全量重放**
        /// ⇒ 它与「现场录的那一份」应当**逐条相同**（同一条重建路、同一个引擎）。
        /// 相形之下**今天**（`A1099` 之前）那一份 `_rec` 只收得到**重连之后、本机自己**的动作
        /// ⇒ 一份**从中间开始的残档**，放出来必然报「不是同一局」—— 那才是缺陷。</para>
        ///
        /// <para>🔴 **与「放录像」那一趟的分别是刻意的**：`PlayReplay` 显式把 `RecordReplays` 关掉
        /// （`finally` 再还原）⇒ 它那一趟 `RecBegin` 直接 `_rec = null`、一条都不记；
        /// 本方法**不关**（重连是**真的在打这一局**，该记）。这两句判断**别对调**。</para></summary>
        public void NetReplay(MsgStart start, List<MsgAction> actions)
        {
            bool host = _net != null && _net.IsHost;
            var pb = NetPendingBattle.FromReplay(start, host);
            Debug.Log($"[Net] 重连重放：重建这一局（种子 {pb.Seed}）并重放 {actions.Count} 条动作");
            // 🔴 **2026-10-12（A387）**：与 `BeginFromDeckLibrary` 的联机那一档**同一个实情**
            //   （重连重放的不是录像，是**这一局**——开场包同样带两副牌）。原来这两处都借
            //   `BeginFromPendingCore` 里写死的 `"联机局"`；现在理由按实情说：卡组随开局包下发。
            BeginFromPendingCore(pb, attachNet: false,
                                 deckNote: "联机局·下发里没有卡组");      // 重建（`Net` 保持挂着）
            // 🔴 **2026-10-19（`A1099`）：出声** —— 上面那句说明里讲的「这一趟会新开一份录像」不许静默。
            //   `RecordReplays == false` 时（只有自检/回放那两档会那样）如实说「这一趟不记」。
            if (_rec != null)
                Debug.Log($"[Replay] 重连重建：这一局的录像**从头重录**（权威流全量 {actions.Count} 条"
                        + " —— 重建出来的这一局 + 全量重放，与现场那份应当逐条相同）");
            else
                Debug.Log($"[Replay] 重连重建：这一趟**不记录像**（`RecordReplays=false`）"
                        + " —— 权威流那 {actions.Count} 条一条都不会进录像");
            for (int i = 0; i < actions.Count; i++)
            {
                var m = actions[i];
                if (m == null) continue;
                // 🔴 **每一条都要重放**（重建之后连本机自己那条也要重来一遍 —— 本地状态整个丢掉了）
                int code = ApplyLoggedAction(m);
                if (code != RuleCodes.OK)
                    Debug.LogError($"[Net] 重放第 {i} 条（kind={m.kind}，actor={m.actor}）被拒：{RuleCodes.Describe(code)}");
            }
            RefreshAll(); UpdateHud();
            SetHint("已重连并追平");
            NetAfterTurnStart();
        }

        /// <summary>每回合开始（换边之后）要做的一件事：**对一次状态指纹**。</summary>
        void NetAfterTurnStart()
        {
            if (_net != null) _net.SendFingerprint();
        }

        void NetTick() { if (_net != null) _net.Tick(); }

        string _myFaction = DefaultFactionA;
        string _foeFaction = DefaultFactionB;
        /// <summary>这一局的种子。重开时 +1（引擎只用 `System.Random(seed)`，对局可复现）。</summary>
        int _seed = 20260911;
        /// <summary>本局我方用的**存档卡组**（卡组编辑器里当前选中的那套）。null = 没编过 / 读不出来。
        /// **必须留着** —— `Restart()` 要照原样再来一局，不记的话「按 R 重开」就变成自动凑的牌了
        /// （和 `_myFaction` 一个道理：那是「这一局才有」之外的状态，跨局要显式带过去）。</summary>
        PlayerDeck _myDeckSrc, _foeDeckSrc;
        /// <summary>开局那句「本局用的是哪副牌 / 多少张没上场」。提示行空着时显示它（见 `SetHint`）。
        /// 空串 = 没什么要交代的。**这是我们加的** —— 原版没有这一行（原版全卡种都能上场）。</summary>
        string _deckNotice = "";
        /// <summary>`Begin` 收到的那个 `deckNote` **原样**（没包成句子）。只有一个用处：**自检**钉 A387
        /// （「回放局不许再借联机局那张标签」）—— 比 `_deckNotice` 那个**成品句子**强：
        /// 那句**只有在 `ResolveDeck` 没给出人话时才会被生成**（牌有名字时 `deckNote` 压根用不上）
        /// ⇒ 拿成品句子比 = 同一份代码在不同录像上结论不同（**弱断言**，铁律 12 那条「别写恒真的」）。
        /// ⚠️ **它不属于对局记录**（`RecordBattleLog` 不收这个参数）—— 只喂 `_deckNotice`。</summary>
        string _deckNote;
        /// <summary>HUD 是不是已经建过了（**只能建一次**，见 `BuildHud`）</summary>
        bool _hudBuilt;
        /// <summary>本局的卡池（`CardDatabase.Load()` 那一份）。战斗日志要把卡名翻成中文名，所以留着</summary>
        List<CardDef> _pool;
        /// <summary>墓地/战斗日志面板（原版 `CemeteryLogPanel`）。入口是敌方名牌上那颗按钮</summary>
        BattleLogPanel _logPanel;
        /// <summary>敌方名牌上的「看日志」按钮（原版 `ShowCemeteryBtn`，图 `40k_UI_bt_battlelog`）</summary>
        ImageQuad _cemeteryBtn;
        /// <summary>日志面板的内容缓存（刷新时重建，新的在前）</summary>
        readonly List<BattleLogPanel.Entry> _logEntries = new List<BattleLogPanel.Entry>();

        /// <summary>**场上**那几张卡的视图（自检用）。
        /// ⚠️ 别拿 `boardRoot.GetComponentsInChildren` 代替 —— 自检场景里 `boardRoot` 是**整个场景根**，
        ///    手牌也挂在下面，那样会把「场上分层」验成一片红或一片绿（第一次就是这么写错的）。</summary>
        public IEnumerable<CardView> BoardViews()
        {
            foreach (var kv in _myUnits) if (kv.Value != null) yield return kv.Value;
            foreach (var kv in _foeUnits) if (kv.Value != null) yield return kv.Value;
        }

        /// <summary>**某一个槽**上那张卡的视图（自检用；没有则 null）。
        /// 🔴 2026-09-20 加：验「手牌打出去之后 3D 卡体在不在」必须**盯住那一张** ——
        /// 用 `BoardViews()` 通检会被「督军 / AI 出的牌（出生就在场上，本来就有 3D 体）」蒙过去，
        /// 那条断言一直是绿的，而真玩的时候自己打出去的兵是平面贴纸。</summary>
        public CardView BoardViewAt(int slot, bool mine = true)
        {
            CardView v;
            return (mine ? _myUnits : _foeUnits).TryGetValue(slot, out v) ? v : null;
        }

        readonly Dictionary<int, CardView> _myUnits = new Dictionary<int, CardView>();
        readonly Dictionary<int, CardView> _foeUnits = new Dictionary<int, CardView>();

        // 🔴 **2026-10-01：单位 → 视图**（身份表，与上面那两张「格号 → 视图」并排存在）。
        //
        //   为什么要多这一张：棋盘改成**连续无洞**模型之后（`RuleEngine/Core/BoardSlots.cs`），
        //   **一个单位会换格号** —— 出牌插在它前面、或它前面的人死了，它就整体内外移一格
        //   （原版 `ReassembleMinions` 把它们摆回各自的下标）。而 `_myUnits` 是按**格号**索引的
        //   ⇒ 只看格号的话，`SyncBoard` 会把「挪走的那张」当成「这格空了」**销毁**、
        //   再在别处**重建一张新的**（瞬移 + 丢动画 + 丢关键词框/残骸体那些挂在视图上的状态）。
        //   有了身份表才能认出「这是同一张卡，只是换了格」⇒ 把视图**搬**过去（并补一次移动补间）。
        readonly Dictionary<UnitState, CardView> _myUnitViewByUnit = new Dictionary<UnitState, CardView>();
        readonly Dictionary<UnitState, CardView> _foeUnitViewByUnit = new Dictionary<UnitState, CardView>();

        Dictionary<UnitState, CardView> UnitViewMap(int owner)
        {
            return owner == _me ? _myUnitViewByUnit : _foeUnitViewByUnit;
        }

        // 🔴 **2026-10-01：「已经从棋盘上摘掉、但演出还没播」的视图**（按格号存）。
        //
        //   为什么必须单独存一档：连续棋盘下**离场会让外侧的人整体补位**—— 那一格的键
        //   **马上**就要给新主人用，而阵亡/回手的演出是在事件时间线上**晚一拍**（阵亡 0.85 s）才播的。
        //   ⇒ 「谁已经不在了」这件事必须在**摘键的那一刻**记下来，不能指望 0.85 s 之后
        //      `views[slot]` 还是它（那时候多半已经换成补位上来的那个人了 —— 会**溶掉一张活的卡**）。
        readonly Dictionary<int, CardView> _myFading = new Dictionary<int, CardView>();
        readonly Dictionary<int, CardView> _foeFading = new Dictionary<int, CardView>();

        Dictionary<int, CardView> FadingMap(int owner)
        {
            return owner == _me ? _myFading : _foeFading;
        }

        /// <summary>「已经把格号让出去、演出还没播」的视图张数（自检用）。
        /// ⚠️ 与 `DyingCount` 不是一回事：阵亡事件在时间线上要等一拍（`EventTiming`），
        /// 轮到播之前它**先在这一档**（`PlayDeathFeel` 从这儿把它取走）。</summary>
        public int FadingCount
        {
            get
            {
                int n = 0;
                foreach (var kv in _myFading) if (kv.Value != null) n++;
                foreach (var kv in _foeFading) if (kv.Value != null) n++;
                return n;
            }
        }

        /// <summary>这一格有没有「排着队还没播」的离场演出（阵亡 / 回手 / 回牌库）。
        /// 摘键时用它决定：这张视图是**留着等演出**，还是当场销毁。</summary>
        bool PendingExitFor(int owner, int slot)
        {
            for (int i = 0; i < _timeline.Count; i++)
            {
                var e = _timeline[i].evt;
                if (e.Player == owner && e.Slot == slot
                    && (e.Kind == EvtKind.Death || e.Kind == EvtKind.Return)) return true;
            }
            return false;
        }

        /// <summary>把某一格里那个**已经不在场上**的视图摘下来（`into` = 等演出的那一档）。
        /// 没有排队演出的直接销毁 —— 留着一张「引擎里没有对应单位」的卡在场上就是幽灵。</summary>
        void DetachStaleView(int owner, Dictionary<int, CardView> views, int slot, CardView v)
        {
            views.Remove(slot);
            if (v == null) return;
            if (animateFeel && PendingExitFor(owner, slot))
            {
                var fade = FadingMap(owner);
                CardView old;
                if (fade.TryGetValue(slot, out old) && old != null && old != v) Kill(old.gameObject);
                fade[slot] = v;
                return;
            }
            Kill(v.gameObject);
        }

        /// <summary>把 <paramref name="v"/> 挂在**别的格号**上的那些键摘掉（一格一个键）。</summary>
        static void RemoveSlotKeysExcept(Dictionary<int, CardView> views, CardView v, int keep)
        {
            List<int> stale = null;
            foreach (var kv in views)
                if (kv.Key != keep && kv.Value == v) (stale ?? (stale = new List<int>())).Add(kv.Key);
            if (stale == null) return;
            foreach (int k in stale) views.Remove(k);
        }

        /// <summary>这张视图是不是**已经登记过主人**（在 `byUnit` 的值里）——
        /// 是的话「这一格的键上挂着它」就不能当成「无主视图」来认领（它属于别人）。</summary>
        static bool ViewBelongsToSomeone(Dictionary<UnitState, CardView> byUnit, CardView v)
        {
            foreach (var kv in byUnit) if (kv.Value == v) return true;
            return false;
        }

        /// <summary>这张视图的主人**此刻还在棋盘上**吗？—— 摘键 / 顶键之前用它兜一道底：
        /// 活着的单位的视图**绝不能**被当成「离场遗留」销毁掉（那会凭空少一张卡）。</summary>
        bool ViewOwnedByLiveUnit(UnitState[] board, Dictionary<UnitState, CardView> byUnit, CardView v)
        {
            for (int i = 0; i < board.Length; i++)
            {
                if (board[i] == null) continue;
                CardView mapped;
                if (byUnit.TryGetValue(board[i], out mapped) && mapped == v) return true;
            }
            return false;
        }

        readonly List<CardView> _handViews = new List<CardView>();

        Label _turnLabel, _energyLabel, _endTurnLabel, _resultLabel, _hintLabel;
        /// <summary>「等待提示」（原版 `WaitText`）—— 对手思考时那条。🆕 2026-09-17</summary>
        WaitBanner _waitBanner;
        // 阵营资源（信仰 / 灵魂石）—— 2026-09-13 第三十三轮。物件**照建**、靠 `SetActive` 切显隐
        // （照原版 `ManaTypeHolder.Toggle` 的做法；判据见 `ShowsSpiritStone` / `ShowsFaith` / `ShowsQuestPoints`）
        ImageQuad _myFaithIcon, _foeFaithIcon, _myStoneIcon, _foeStoneIcon, _myStoneGem, _foeStoneGem;
        Label _myFaithText, _foeFaithText, _myStoneText, _foeStoneText;
        /// <summary>任务点数字（原版 `QPText`，'0/3'）。
        /// 🔴 **2026-09-16 更正**：这里原来写「**引擎没有任务点机制** → 恒为 0/3」—— **两半都要修**：
        ///   ① 引擎**有**任务点机制（`PlayerState.QuestPoints`，DarkAngels 那一族在用；
        ///      出处见那个字段的注释）；
        ///   ② 🔴 **2026-09-18 二次更正：「恒为 0/3」这条也不成立，是我上一轮核错了。**
        ///      `Hud(root, "0/3", …)` 那个只是**建标签时的初始文本**；`UpdateHud()` 里
        ///      （`:4371`）每个回合都在 `SetText($"{me.QuestPoints}/3")` —— **接上了**。
        ///      而 `UpdateHud()` **挂在 `AdvanceTimeline` 上也跟着调**（`:2734`，注释写着
        ///      「批处理里没有 Update() 循环，HUD 得在这里刷」）⇒ **批处理里也是活值**。
        ///      ⚠️ 上一轮的错因：只看到**构造那一行**的字符串，没看**谁在后面覆写它**。
        ///      ⚠️ 自检那条 `QpText == "0/3"` 能过，是因为**这一局双方真的一分都没有**，
        ///         **不是**因为写死 —— 拿它当「写死」的证据就是**把巧合当判据**。
        /// 而且**只有暗黑天使显示**（见 <see cref="ShowsQuestPoints"/>）。
        /// ⚠️ 仍然悬着的一小件：「上限 **3** 从哪来」还没在卡面/规则书上坐实（数字是活的，但那个 3 是写死的）。</summary>
        Label _qpTextMe, _qpTextFoe;
        /// <summary>加时标记（原版 `OvertimeIndicator`）。**默认关着**，进加时后由引擎的 `ctx.IsOvertime` 点亮</summary>
        ImageQuad _overtime;
        // 🆕 2026-09-20：加时全屏 splash（原版 `OvertimeUi.overtimeSplashCanvasGroup` / `OvertimeSplashText`）
        GameObject _overtimeSplashRoot;
        ImageQuad _overtimeSplashBg, _overtimeSplashBand, _overtimeSplashIcon;
        Label _overtimeSplashText;
        /// <summary>加时 splash 那颗字的**原版词条键**（`Label.Create` 与字号那一跳都取它）。
        /// 判据（`Localize.mTerm` + TMP 原文 `OVERTIME!`）→ `Core/Loc.cs` 那一块，⛔ 别在这儿抄第二份。</summary>
        public const string OvertimeTerm = "Battle/Overtime/Title";
        /// <summary>splash 的淡入/停留/淡出计时（秒）。<b>负数 = 没在播</b>。</summary>
        float _overtimeAnimT = -1f;
        /// <summary>「已经播过一次」——原版 `IsOvertime` 置 true 后不再判，我们也不重播</summary>
        bool _overtimeFired;

        // ==================================================================
        //  🆕 2026-10-18（§8b 批 B · 2a / 2b）：**Canvas 级那三件**（原版都在 `BattleHud/Canvas` 下）
        //    · `Aspect Ratio Filler{Top,Bottom}` —— 两枚纯黑 quad（13/13 战场都有，原版也在屏外）
        //    · `UI Error Message Controller (MUST BE ENABLED)` —— 错误/疲劳横幅（5 条一池）
        //    · `ScreenAspectRatioController` —— 比例超出 [4:3, 22:9] 时给**两台**相机写 `Camera.rect`
        //  三件的判据全在各自文件头（`ScreenAspectRatioController.cs` / `ErrorMessageBanner.cs`）
        //  ＋ 交接报告 §2a/§2b；**本文件只管「建出来 + 接上触发」**。
        // ==================================================================

        /// <summary>原版 `Aspect Ratio Filler` 的两枚黑边 quad（判据 → <see cref="BuildAspectRatioFillers"/>）</summary>
        ImageQuad _aspectFillerTop, _aspectFillerBottom;
        /// <summary>错误横幅（原版 `UI Error Message Controller`，判据 → `ErrorMessageBanner.cs` 文件头）</summary>
        ErrorMessageBanner _errorBanner;
        /// <summary>比例控制器（原版 `ScreenAspectRatioController`，判据 → 那一件的文件头）</summary>
        ScreenAspectRatioController _aspectRatio;

        /// <summary>自检口：错误横幅（⛔ 生产路径别拿它弹东西 —— 走 `ShowError` 那条链）。</summary>
        public ErrorMessageBanner ErrorBanner { get { return _errorBanner; } }
        /// <summary>自检口：比例控制器。</summary>
        public ScreenAspectRatioController AspectRatioController { get { return _aspectRatio; } }

        // ---- 原版 `Aspect Ratio Filler{Top,Bottom}` 的四个数（`RectTransform_{3021,2916}.json` 实读，**未取整**）----
        /// <summary>`sizeDelta`（两枚同值）。</summary>
        public const float AspectFillerW = 3252.123046875f, AspectFillerH = 1842.02001953125f;
        /// <summary>Top 的 `anchoredPosition.x`（**0.00067** 那一档 —— 不是 0，⛔ 别取整）。</summary>
        public const float AspectFillerTopX = 0.0006713899783790112f;
        /// <summary>Bottom 的 `anchoredPosition.x`。</summary>
        public const float AspectFillerBottomX = -3.051800013054162e-05f;
        /// <summary>两枚的 `anchoredPosition.y`（±1641.10，未取整）。</summary>
        public const float AspectFillerTopY = 1641.0999755859375f;
        /// <inheritdoc cref="AspectFillerTopY"/>
        public const float AspectFillerBottomY = -1641.0999755859375f;

        /// <summary>疲劳横幅的两条**原版词条键**（`BattleManager._ResolveFatigue_d__587` 实读：
        /// `Battle/Tips/DamageFatigue` = 自己牌库空、`…Enemy` = 对手牌库空）。
        /// ⚠️ 本地 84 个 bundle 里**没有 I2 语言表** ⇒ 这两条键**一个 value 都没有**，
        /// 今天实际走的是 <see cref="DamageFatigueText"/> 的兜底句（同 `HandFullText` 那两态）。</summary>
        public const string DamageFatigueTerm = "Battle/Tips/DamageFatigue";
        /// <inheritdoc cref="DamageFatigueTerm"/>
        public const string DamageFatigueEnemyTerm = "Battle/Tips/DamageFatigueEnemy";

        /// <summary>「这一方的疲劳值我们已经弹过到几了」——**我们的记账位**（疲劳判据里那道闸，见
        /// <see cref="NoteFatigueBanners"/>）。索引 = 0/1。</summary>
        readonly int[] _fatigueShown = new int[2];
        /// <summary>敌方能量数字（原版 `EnemyMana/ManaText`）</summary>
        Label _foeEnergyLabel;
        EndPanel _endPanel;
        /// <summary>本局的**结算账做过了没有** —— 🔴 **2026-10-12（A382）起它就是那道闩**。
        /// 原来没有这个字段：闩是 `!_endPanel.Visible`，于是 ① `_endPanel == null` 时整块**静默跳过**、
        /// ② 那个「可见性」本来也不是闩（靠的是新一局开局时 `UpdateHud` 的 `else` 支顺手 `Hide()`）。
        /// **`Begin()` 里清零** ⇒ 「一局一张账」。⚠️ 结算那一整块在 `UpdateHud()` 里，
        /// ⛔ 别在别处再判一次 `Ctx.IsOver` 记账。</summary>
        bool _settled;
        /// <summary>这个驱动一共**记过几次结算账**（每局至多一次；回放局**不记**）。
        /// 自检口 = <see cref="SettleCount"/>（「放一局录像不会多记一笔账」就盯它）。</summary>
        int _settleCount;
        /// <summary>这一局是不是**放录像放出来的**（🔴 **2026-10-12（A381）**）。
        /// true ⇒ 结算那一整块（日常推进 / 对局记录 / 录像收尾 / 结算面板 + 开门视频）**整块不做** ——
        /// 判据 = 原版 `ChallengeLogMgr.LogMatchEnd` **只在真打完时叫**，回放不是「真打完」。
        /// ⚠️ **由 `Begin()` 清零**（新开一局 = 不再是回放），`PlayReplay` 在重建之后置真、
        ///   **整场回放期间一直留着** —— ⛔ **不能在那次调用返回时就清掉**：清掉的话下一帧 `Update()`
        ///   那一趟 `UpdateHud()` 又会把账记一遍，正是这条账要修的东西。</summary>
        bool _replaySession;
        /// <summary>🔴 **2026-10-16（W22）：结算后的出口已经走出去了没有**（一局只走一次）。
        /// 原版那条路的收尾是 `BattleManager.LeaveBattle()` → 加载主菜单场景
        /// （`LeaveBattle.c:58`；触发 = `BattleManager__Update.c:116-122` 的
        /// `matchFinishedAndWaitingToLeave &amp;&amp; (左键任意处 || ESC)`）。
        /// 这里这个闩干两件事：① 不让同一帧/相邻帧**重复加载**场景；② 给自检一个**可读的口**
        /// （批处理下真的 `LoadScene` 会被跳过，见 <see cref="LeaveBattle"/>）。
        /// **`Begin()` 里清零** —— 与 `_settled` 同一条纪律：本局的账不跨局。</summary>
        bool _leaving;
        /// <summary>这个驱动**一共走过几次出口**（一局至多一次；`_leaving` 那道闩挡住重复）。
        /// 自检口 = <see cref="LeaveCount"/> —— 「同一帧/相邻帧只走一次」那条断言比它
        /// （与 `_settleCount` 同一个形状：**累计**计数，`Begin()` 里**不**清）。</summary>
        int _leaveCount;
        /// <summary>设置面板（原版 `BattleSettingsPanel`）—— **投降按钮就在里面**</summary>
        SettingsPanel _settingsPanel;
        /// <summary>右上角那颗设置按钮（原版 `SettingsBtn`，x[1808.0,1871.9] y[9.2,73.1]）</summary>
        ImageQuad _settingsBtn;
        // 🆕 2026-09-18：`ChatPopup`（原版 `VoiceLinesPopupSelector`）——
        //   ⚠️ `ChatButton` 那个对象上**挂了两个组件**：`Button`(MB 5291) 的 `onClick → BattleManager.ClickChat`
        //      **和** `PlayerStateToggle`(MB 4089) 的 `selectedBool='EnableWarlordVOs'`。
        //      我们**只接开面板那条**（有 `m_OnClick` 实据）；语音开关那条**待实况确认**，见
        //      `资料/语音线_原版规格与ASR管道.md` §1.7。
        ImageQuad _chatBtn;

        /// <summary>🆕 2026-09-29（§25）：HUD 那颗**进攻卡（环境）**钮。
        /// 显隐判据 = 原版 `!isEmptyOffensiveCard`（选定那张卡 ≠ 本阵营空卡）；点它**只弹展示窗**、
        /// 不换环境（判据 → `资料/加时与冲突模式_原版规格.md` 的进攻卡那一节）。</summary>
        ImageQuad _offensiveBtn;

        /// <summary>🆕 **2026-10-12（A423）**：HUD 那颗**重置自动镜头**钮（原版 `BattleHud.resetCameraZoomButton` `+0xa8`，
        /// 节点 `CenterCameraButton`）。判据全文 → <see cref="ToggleCameraResetButton"/>。
        /// <para>⚠️ 图与位置**早就摆对了**（`BuildHud` 里那句 `HudAbs(…, 17.9, 568.2, 64.44, 61.85, "CenterCameraButton")`）——
        /// 缺的是**行为**：驱动里没有引用、点了没反应、也没有显隐（原来一直亮着）。
        /// `Battle/CombatAutoZoom.cs` / `Battle/CombatCameraZoom.cs` 文件头里
        /// 「全仓 `ResetCamera` 0 命中 ⇒ 我们 HUD 没有那颗钮」说的是**后半**（链不在），不是「图不在」。</para></summary>
        ImageQuad _cameraResetBtn;

        /// <summary>🆕 **2026-10-18（A985③）**：敌方名牌上那颗「对手档案 / 联盟面板」钮
        /// —— 原版 `BattleHud.alliancePanelOpenButton`，节点就是 `LeftArea/EnemyInfo` **本身**
        /// （不是另起一颗图标）。rect / 命中区 / 「它没有贴图」的逐条判据 → `BuildHudExtras` 里建它那一段。
        /// <para>⚠️ 它是**全透明**的占位 quad（原版那颗 `m_TargetGraphic` 是 `NonDrawingGraphic`，
        /// 一个像素都不画）⇒ ⛔ 别拿「看不见」当「建错了」。</para></summary>
        ImageQuad _allianceBtn;
        /// <summary>🆕 **2026-10-18（A985③）**：原版 `FrontCanvas/Alliance Panel`（组件 `BattleAlliancePanel`）
        /// 那扇**对手档案**窗。⚠️ **它是「面板」不是「窗」**（原版根组件是普通 `MonoBehaviour`、
        /// **不是 `GameWindow`**）⇒ 不注册进 `WindowsManager`。建成后**默认关着**（照原版 `Awake`）。</summary>
        AlliancePanelWindow _alliancePanel;
        ChatPopupPanel _chatPopup;
        float _chatCooldown;                      // 原版 `CHAT_INTERACTABLE_COOLDOWN = 4f`
        CardDisplayWindow _cardDisplay;
        /// <summary>🆕 2026-09-29：**点开大卡窗的那张牌**（手牌那张 / 棋盘上那个单位）。
        /// 「指针移开就关」那条要用它 —— 原版守的是**那张卡自己的** `CardCollider.OnPointerExit`。</summary>
        CardView _cardWinSource;
        /// <summary>🆕 2026-09-29：**开窗/关窗发生在哪一帧**（同帧防打架用）。
        /// 手牌那套轻点事件（`CardInteraction`）与这里的点击路由**不是一个来源**、各自读同一个鼠标
        /// ⇒ 同一帧里可能「先关后开」或「先开后关」。两个戳就是为了掐掉这两种。</summary>
        int _cardWinOpenedFrame = -1, _cardWinClosedFrame = -1;

        /// <summary>「一次摊开多张」的展示窗（原版 `UIMultiCardDisplay`）。平时关着</summary>
        MultiCardDisplay _multiCards;
        /// <summary>多张展示窗（自检用）</summary>
        public MultiCardDisplay MultiCards { get { return _multiCards; } }
        /// <summary>卡牌放大展示窗（自检要读它的 Visible / ShownTitle）。</summary>
        public CardDisplayWindow CardDisplay { get { return _cardDisplay; } }
        /// <summary>结算面板（自检要读它的 Visible / ShownSkulls）。</summary>
        public EndPanel End { get { return _endPanel; } }
        /// <summary>自检用：本局**结算账做过了没有**（A382 起它就是那道闩）。</summary>
        public bool Settled { get { return _settled; } }
        /// <summary>自检用：这个驱动一共**记过几次结算账**（每局至多一次）。
        /// 「放一局录像不会再记一遍账」这条断言就比它：`PlayReplay` 前后这个数**不该变**。</summary>
        public int SettleCount { get { return _settleCount; } }
        /// <summary>自检用：这一局是不是**回放局**（`PlayReplay` 放出来的）。
        /// ⚠️ 它是「整场回放期间」都为真，不是「正在灌动作那一瞬」。</summary>
        public bool ReplaySession { get { return _replaySession; } }

        /// <summary>🔴 **结算后的出口闸门**（= 原版 `BattleManager.matchFinishedAndWaitingToLeave`）。
        /// 判据**只有一份**（`EndPanel.ExitReady` → `BattleDoors.Finished`），这里只是转发 ——
        /// 对应原版那个字段就住在 `BattleManager` 上（`+0x510`，`dump.cs:30916`）。
        /// ⚠️ **拿不到结算面板时闸门恒关**（`_endPanel == null` ⇒ false）：那一档面板根本没显示、
        ///    也就没有「开门视频播完」这回事 —— **不许**在这里放行（放行 = 一局刚开始就能点走）。</summary>
        public bool ExitReady { get { return _endPanel != null && _endPanel.ExitReady; } }

        /// <summary>自检用：出口**走出去了没有**（`LeaveBattle` 已经跑过）。</summary>
        public bool LeaveRequested { get { return _leaving; } }

        /// <summary>自检用：这个驱动**一共走过几次出口**（一局至多一次）。</summary>
        public int LeaveCount { get { return _leaveCount; } }
        /// <summary>这局里**敌方督军降到过的最低生命**。🆕 **2026-10-06（A147/A148）改了用途**：
        /// 它**不再**算骷髅数（那件事搬去 `_foeSkullCount`，判据写在那个字段上）—— 现在**只**给结算面板
        /// 副标题那行字用（`EndPanel.Show` 的 `minFoeWarlordHealth`）。生命只会往下走（治疗会回，但
        /// 「首次得到」不回退），所以取最小值就够，不用记历史。</summary>
        int _foeWarlordMinHp = int.MaxValue;
        /// <summary>敌方视角的同一件事：**我方督军降到过的最低生命**。🆕 2026-09-27 加 ——
        /// 只给「对局历史」那条记录算**对面拿了几颗骷髅**用（`Shell/BattleLogData.cs` 的 `EnemySkulls`）。
        /// 判据：原版 `BattleScoreManager.GetEnemySkullCount(int ownLife)` —— 逐档
        /// `if (threshold &lt; ownLife) 不计数`（`BattleScoreManager__GetEnemySkullCount.c:28`）⇒
        /// **计数条件 = `ownLife &lt;= threshold`**，与 `DeckRules.SkullsFor` 的 `&lt;=` **逐字等价**
        /// （那行严格小于是**否定分支**，不是另一条口径 —— 2026-10-06 核）。</summary>
        int _myWarlordMinHp = int.MaxValue;
        /// <summary>🔴 **2026-10-06（A147/A148）**：**这局拿了几颗骷髅** = **已达成**的里程碑档数。
        ///
        /// 原版判据（`d:/2/tools/decomp_full/`，第一权威）：
        ///   · 这个数 = `BattleScoreManager.GetSkullCount()` → `Enumerable.Count(milestones, 谓词)`；
        ///     谓词 `BattleScoreManager.__c___&lt;GetSkullCount>b__3_0.c` 读的就是
        ///     `HealthThresholdData.AlreadyAccomplished`（字段 +0x14）⇒ **逐档「已达成」标志的计数**；
        ///   · 那些标志**只在「敌方督军生命变化」的信号里被置位**：`BattleScoreManager__CheckThresholds.c:22`
        ///     `if (!AlreadyAccomplished &amp;&amp; signal.health &lt;= threshold) { AlreadyAccomplished = 1;
        ///     UpdateMilestonesCount(index); }`；而信号源 `BattleEventsController.CheckHealth` 是拿当前生命与
        ///     **缓存值**比、**不等才发**（`BattleEventsController__Initialize` 用当前生命播种那个缓存值）；
        ///   · **开局一个都没置** ⇒ `BattleScoreUiManager__Initialize.c:13` 收尾 `UpdateMilestonesCount(0xffffffff)`
        ///     ⇒ 文案 `System_String__Format("x{0}", index+1)` = **`x0`**（`BattleScoreUiManager__UpdateMilestonesCount.c:13`）。
        /// ⇒ 原版 = **事件驱动 + 单调不回退**；我们原来是**拿「最低生命」反推**（`SkullsFor(_foeWarlordMinHp)`），
        ///   两者只在**开局那一格**分叉：遭遇局（`GameplayVariables.SkirmishWarlordLifeChange = -10`）起始生命
        ///   15/20 ⇒ 旧口径开局就 `SkullsFor(20) = 1` ⇒ 显示 `x1`，而原版那时是 `x0`（**真偏离**，A114 查出）。
        ///   经典局 56 个督军的起始生命 ∈ {25,30,35,40}，**没有一个 ≤ 20** ⇒ 两边都是 `x0`、**经典局零回归**。
        /// ⚠️ 「已达成不回退」照旧成立：这里只增不减，治疗回血不会把骷髅扣回去。
        /// ⚠️ **没证死的一半**（如实记）：原版遭遇局开局到底是 `x0` 还是 `x1`，取决于
        ///   `BattleEventsController.Initialize` 播种缓存 与「遭遇 −10 生命」的**先后**，本地查不到
        ///   ⇒ 按「原版 `Initialize` 写 `x0` 是硬的」做（另一半见 `资料/普查产出_1006/A129_A114_查证.md`）。</summary>
        int _foeSkullCount;
        /// <summary>**上一次观察到的敌方督军生命**（原版那条信号里的「缓存值」）。
        /// `int.MinValue` = **还没播种**：第一帧只把它设成当时的生命、**不触发**里程碑 ——
        /// 等价于原版 `Initialize` 用当前生命播种缓存（所以开局那一下不算「变化」）。
        /// `Begin()` 每局重置。</summary>
        int _foeSkullHpSeen = int.MinValue;
        Label _handLabel, _myText, _enemyText;
        Label _pileLabel, _foePileLabel;

        // 原版 UI 图（`Resources/Art/ui/`，没有就是 null —— 退回纯文字 HUD）
        ImageQuad _endTurnBg, _energyGem, _energyGemEmpty, _myPlate, _enemyPlate;
        ImageQuad _foeEnergyGem, _foeEnergyGemEmpty;      // 敌方那颗能量水晶（原来**根本没画**）
        ImageQuad _myEnergyPlate, _foeEnergyPlate;        // 水晶底下那块底板 `Card Frame Cost Icon`
        ImageQuad _myQuestIcon, _foeQuestIcon;            // 任务点纹章（**只有暗黑天使显示**，见 ShowsQuestPoints）
        ImageQuad _myQuestJoin, _foeQuestJoin;            // 任务点连到水晶上的小接片
        ImageQuad _myPile, _foePile;
        /// <summary>🆕 2026-09-26：牌堆卡背底下那层 **SDF**（原版 `Cardback Shadow SDF`）。
        /// 逐值 → <see cref="DeckSdfPx"/>；取不到掩码时为 null（那层不画，牌堆本体照旧）。</summary>
        ImageQuad _myDeckSdf, _foeDeckSdf;
        ImageQuad _myDeckPlate, _foeDeckPlate, _myDeckLight, _foeDeckLight;
        ImageQuad _myDeckSizePlate, _foeDeckSizePlate;    // 牌库张数底板 `40K_display`
        ImageQuad[] _playedPips;                          // 本回合已出牌数：最多三枚 `40k_general_bt_yellow`
        ImageQuad _skullIcon;                             // 我方名牌上的里程碑骷髅
        Label _skullScore;                                // 骷髅旁边那个 `x N`
        ImageQuad _handPlate;                             // 手牌数底板 `40K_display`
        /// <summary>本回合**我**打出了几张牌（原版 `CardsPlayedInTurn1..3`）。回合开始清零。</summary>
        int _cardsPlayedThisTurn;

        // ---- 牌堆那一套的尺寸（px @1920×1080 → 世界单位，108 px/单位）----
        // 出处：运行时 dump `runtime_ui_dump_Battle_Arena_1.tsv` 里 `PlayerDeck` 的子树
        /// <summary>牌堆底板 `UI_Deck_Background`：`PlayerDeck` 自己的 230×230</summary>
        const float DeckPlatePx = 230f;
        /// <summary>卡背：`Cardback` 的 `sizeDelta` 2.1739 × 3.1364 ⇒ @100 px/单位 = 217×314 px。
        /// ⚠️ **2026-10-19（A1161）就地订正（铁律 5）**：本句原来写「**父节点 scale 100**」——
        /// 那前提不成立（链上三级 `m_LocalScale` 全是 1.0，判据 → 下面 `DeckSdfPx` 的注释）。
        /// ⛔ **值不动**，只把「那个 100 在哪」写对。</summary>
        const float DeckCardPx = 314f;

        /// <summary>🆕 2026-09-26：牌堆那层 **SDF** 的高度（px）。
        ///
        /// 🔴 逐值出处 = 原版预制体 `Cardback Container` 下**两个兄弟节点自己的 sizeDelta**
        /// （`bundle_battleprefabs_vfxandmisc_assets_all/GameObject/`，2026-09-26 实读）：
        ///   · `Cardback`              = **2.1739 × 3.1364** ⇒ @100 px/单位 = 217.39 × 313.64 px（= 上面那个 314）
        ///   · `Cardback Shadow SDF`   = **2.9212 × 3.8122** ⇒ @100 = **292.12 × 381.22 px**
        ///   · 两个都是 `anchoredPos (0,0)` / `pivot (.5,.5)` ⇒ **同心**，SDF 比卡背大 **1.34376 / 1.21548 倍**
        /// ⚠️ 与「收藏窗卡背格」那处的**倍数不同**（那边 337.5/250 = 1.35、550.8/405 = 1.36）——
        ///    两处各自的 rect 不一样，**别拿一个值当全部**（铁律 5·c）。
        /// 本常量按同一比例从 `DeckCardPx` 推：`314 × 3.8122 / 3.1364 = 381.66`。
        /// 📌 旁证：`资料/战斗UI_原版对账表.md:90` 记的「原版 292×381」与上面逐值吻合。
        /// <para>🔴 **2026-10-19（A1161）就地订正（铁律 5）**：上面那句「@容器 scale 100」
        /// （以及 `DeckCardPx` 上、`BuildHud` 里建牌堆那一截注释里的同一句）
        /// **不成立** —— 本 prefab 链上 `Cardback` / `Cardback Container` / `2DCard` 的
        /// **`m_LocalScale` 全是 (1,1,1)**。本件**现读复核**（不是转述）：
        /// `d:/2/新解包资源/assets_full/bundle_staticgeneralassets_assets_all/RectTransform/`
        /// 里从 `RectTransform_-4585763702919738994`（`Cardback`，`sizeDelta` 2.1739×3.1364）
        /// 沿 `m_Father` 逐级解到根 —— 父 = `Cardback Container`（`sizeDelta` 1×1）→ 父 = `2DCard`
        /// （2.0927×3.3313）→ 根（`m_Father.m_PathID = 0`）；**三级 `m_LocalScale` 全是 1.0**
        /// （`Cardback Shadow SDF` 那一支同样如此）。
        /// ⇒ **×100（= 314 px 那个换算）不在这一层 prefab 里**，来自**别处**：第八会话 `X2` 记的是
        /// 「**更上游的场景侧缩放**」—— ⚠️ 本件只核到「**不在这一层**」，**没核到「在哪一层」**，
        /// 要钉死得跑实况或读场景侧那一级的 `m_LocalScale`（⛔ 别把「更上游」写成已核实的结论）。
        /// 出处 → `资料/普查产出_第八会话/X2_诊断SDF比卡背红.md:49-51`。
        /// ⛔ **只改注释**：`DeckCardPx` / `DeckSdfPx` / `CardbackRectAspect` 的值一个都不动。</para></summary>
        const float DeckSdfPx = DeckCardPx * (3.8122f / 3.1364f);

        /// <summary>🔴 **2026-10-19（A1150）**：原版 `2DCard/Cardback Container/Cardback` **自己那个 rect**
        /// 的 **宽 × 高**（**卡单位** —— 与 `CardView.Width/Height` 同一套量纲，⛔ 不是 px）。
        /// 本件**现读**出处：`d:/2/新解包资源/assets_full/bundle_staticgeneralassets_assets_all/RectTransform/`
        /// `RectTransform_-4585763702919738994.json`（其 `m_GameObject` = `GameObject/Cardback_7876992375092988302.json`，
        /// `m_Name = "Cardback"`）的 `m_SizeDelta` = **(2.1739, 3.1364)**；另一份副本
        /// （`RectTransform_-616306181444777464.json`）同值。
        /// ⚠️ 它与**卡身** `CardView.Width × Height`（**2.0927 × 3.3313**）**不是同一个矩形**：卡背节点
        /// 比卡身**宽 3.88%**（2.1739 vs 2.0927）、**矮 5.85%**（3.1364 vs 3.3313）。敌方手牌画的是卡背
        /// （原版 `PlayerHand__SetupCardInHand.c:45` → `ShowCardBack(!isPlayer)`）⇒ 尺寸的**框**取**这一个**。</summary>
        const float CardbackNodeRectW = 2.1739f, CardbackNodeRectH = 3.1364f;

        /// <summary>原版 `Cardback Container` 下两个兄弟节点**自己的** rect 宽高比（见 `DeckSdfPx` 的注释）。
        /// 🔴 **2026-10-19（B2）就地订正（铁律 5）**：这里原来写「按它们定形状、**不要**用贴图自己的比例」
        /// —— **那条结论是错的**（它的前提「节点比例 ≠ 贴图比例」不成立：节点比例 == sprite 的
        /// `m_Rect` 比例，见 `FitDeckPile` 的注释）。现在这两个常量的用法变成：
        /// · `CardbackRectAspect` = **牌堆那个节点框的宽高比**（喂给 `CardbackFace.Fit` 当 `boxW/boxH`，
        ///   即 `DeckCardPx × CardbackRectAspect`）—— 仍是「照原版节点」，只是**还要再走第二段**；
        /// · `DeckSdfRectAspect` = 同上，给 SDF 那一层用（SDF 那批 `padding` 恒 0、**不必**走第二段，
        ///   见 `Core/CardbackFace.cs` 文件头的「哪些贴图走这条」）。
        /// <para>⚠️ **2026-10-19（A1150）**：本常量的**分子分母提到了上面那两个具名常量**（`CardbackNodeRectW/H`）
        /// —— 同一个数别写两份（敌方手牌那一处也要用它们，判据见那条注释）。</para></summary>
        const float CardbackRectAspect = CardbackNodeRectW / CardbackNodeRectH;
        const float DeckSdfRectAspect = 2.9212f / 3.8122f;
        /// <summary>回合灯：`YourTurnImage` 的 anchor 占底板的 9.9%×15% → 矩形 22.8×34.5，
        /// 但贴图 60×59 是 **KEEP_ASPECT** 缩进这个矩形 → 实绘 **23.4×23.4**。
        /// ⚠️ 2026-09-12 改：原来是 34.5（把矩形的高当成了图的高），比原版大 47%。</summary>
        const float DeckLightPx = 23.4f;
        /// <summary>回合灯相对**牌堆中心**的偏移（px）。⚠️ 2026-09-13 更正：原来写「`anchor 0.8285 / 0.126`
        /// 在 230² 底板上折算」= (75.6, −86)，那是**把锚点矩形的中心当成了灯的中心**，漏了 `anchoredPosition`。
        /// 原样错出来的后果：**灯偏低 66 px、偏左 8 px**（被摆到牌堆右下角去了）。
        /// 正确的算法（dump `runtime_ui_dump_drive_0912.tsv` `YourTurnImage`）：
        /// `anchorMin/Max (0.779,0.051)-(0.878,0.201)` → 锚点矩形 x[179.17,201.94] y[11.73,46.23]（230² 里）
        /// → 中心 (190.56,28.98)，**再加 `anchoredPosition (7.9, 65.8)`** → 灯中心 (198.46, 94.78)
        /// → 相对牌堆中心 (115,115) 即 **(83.46, −20.2)**。绝对 rect x[1801.5,1825.3] y[967.4,1003.0]
        /// 见 `子代理读报_back右区_0827.md:179-180`，与上面算出来的对得上。
        /// 敌方那份：`子代理读报_back右区_0827.md:193-194` x[1765.5,1786.3] y[−106.5,−75.4] → 相对它自己的
        /// 200² 牌堆中心 **(73.6, −9)**。⚠️ 那对数是**静态**值（整棵 `EnemyDeck` 子树在屏外被 park），
        /// 但 park 只动根、不动子树里的局部坐标，所以照用。</summary>
        const float MyLightDxPx = 83.46f, MyLightDyPx = -20.2f;
        const float FoeLightDxPx = 73.6f, FoeLightDyPx = -9f;
        static float Px(float px) { return px / 108f; }

        /// <summary>两边牌堆的锚点（归一化）。**判据只有这一份** —— 建、重贴、放灯都用它。
        /// ⚠️ 2026-09-12 改：原来是 (0.845, 0.235 / 0.790)（右侧中段）。原版实测 `PlayerDeck` 230×230
        ///    绝对 x[1615,1845] y[850,1080]（从上）—— **贴着屏幕右下角**，底边正好压在屏幕下沿；
        ///    敌方牌堆对称贴在右上角（`EnemyDeck` 200×200）。
        ///    出处：`战斗界面JSON权威表_0827.md` 的绝对坐标表（和 dump 里 `RightArea` 那一族的锚点一致）。
        ///    换算：x01 = 1730/1920，玩家 y01 = 1 − 965/1080（中心 115 px 从下）。</summary>
        const float MyDeckX01 = 0.90104f, MyDeckY01 = 0.10648f;
        const float FoeDeckX01 = 0.90104f, FoeDeckY01 = 0.90741f;
        /// <summary>敌方牌堆底板小一号（原版 `EnemyDeck` 200×200，我方 230×230）</summary>
        const float FoeDeckPlatePx = 200f;

        // ---- 牌库张数底板：原版 `Player Deck Size Container` / `EnemyDeck` 下同名那个 ----
        // 是一块横条，**贴在牌堆正上方**（锚到牌堆顶边、中心再往外 50/60 px），不是牌堆的一部分。
        // 出处：`RectTransform_3318.json`（我）/ `MonoBehaviour_4275.json`（我那边的高由 fitter 定）/
        //       `RectTransform_2658.json`（敌，整条 scale=(1,−1) 竖直镜像）/ `MonoBehaviour_5165.json`；
        //       `资料/战斗规格/战斗重建_0827/子代理读报_back右区_0827.md:181`；
        //       运行时 dump `runtime_ui_dump_drive_0912.tsv:310,321`（sizeDelta 已写成 `20.0,59.1` / `20.0,52.0`）。
        /// <summary>张数底板实绘高度：我 238.5/4.0346479415893555 = 59.11；敌 210/4.0346 = 52.05。
        /// 宽度 = 父宽×0.95 + 20（我 238.5 / 敌 210），**原版 `sizeDelta.y` 是 0** —— 高度由
        /// `AspectRatioFitter`(WidthControlsHeight) 算出来，只看 sizeDelta 会以为它是 0 高。</summary>
        const float MyDeckSizePx = 59.11f, FoeDeckSizePx = 52.05f;
        /// <summary>张数底板中心离牌堆中心多远（px）：我 230/2 + 50 = 165、敌 200/2 + 60 = 160。
        /// 出处同上（原版 `anchoredPosition.y` = 50 / 60）。</summary>
        const float DeckSizeDyMine = 165f, DeckSizeDyFoe = 160f;
        /// <summary>张数底板中心相对牌堆中心的**横向**偏移（px）：
        /// 我 0.95×230/2 − 15 − 115 = −20.75、敌 0.95×200/2 + 0 − 100 = −5（原版 `anchoredPosition.x` = −15 / 0）。</summary>
        const float DeckSizeDxMine = -20.75f, DeckSizeDxFoe = -5f;
        /// <summary>张数底板那张图的颜色 —— 原版 `m_Color` = **(1,1,1,0.6941177)**，是半透明的</summary>
        const float DeckSizeAlpha = 0.6941177f;

        // ---- 本回合已出牌数：原版 `LeftArea/CardsPlayedInTurnHolder` + 3 枚 `CardsPlayedInTurn1..3` ----
        // 出处：`RectTransform_2722.json`（holder）/ `RectTransform_3470,3238,2740.json`（三枚）/
        //       `MonoBehaviour_5039.json`（`HorizontalLayoutGroup`：spacing 9、LowerCenter）/
        //       `MonoBehaviour_4406,4381,5015.json`（图）；dump `runtime_ui_dump_drive_0912.tsv:101-104`。
        /// <summary>三枚小方块：`40k_general_bt_yellow`（71×71，PreserveAspect=0）实绘 20×20 px。
        /// 间距 9 → 相邻中心差 29 px。</summary>
        const float PlayedPipPx = 20f, PlayedPipGapPx = 9f;
        /// <summary>第一枚的左下角相对屏幕左下角的位置（px）：holder 底边 = 540 − 28.178 = 511.822，
        /// 三枚居中排在 105.057 宽的 holder 里 → 左起第一枚 x = (105.057 − 78)/2 = 13.5285。</summary>
        const float PlayedPipX0Px = 13.5285f, PlayedPipY0Px = 511.822f;

        // ---- 我方名牌上的里程碑骷髅：原版 `LeftArea/PlayerInfo/Milestones` 子树 ----
        // 出处：`子代理读报_back左区_0827.md:56-58`（绝对 rect，**权威表那两行 x 是错的，见那里「矛盾1」**）/
        //       `RectTransform_2783,3549,3467.json` / `MonoBehaviour_5234,3785.json`；dump `:130-132`。
        /// <summary>骷髅 `MatchSkulls Icon`：rect 65.39×54.14、图 `40k_battle_Win Skull`(66×73)、
        /// PreserveAspect=1 → 实绘 54.14 高。绝对 x[160.7,226.1] y[929.5,983.7]（从上）→ 中心 (193.4, 956.6)。</summary>
        const float SkullIconX01 = 0.10073f, SkullIconY01 = 0.11426f, SkullIconPx = 54.14f;
        /// <summary>分数 `MatchSkulls Score`：绝对 x[225.4,319.9] y[936.1,983.4] →
        /// 文本框左缘 x01 = 225.4/1920、中线 y01 = 1 − 959.75/1080。原版 **H=左对齐 / V=Midline**、字号 fs 35
        /// （我们的档位 1 档 ≈ 7 px 大写高 → 35 px 约合 3.3 档，取 3 —— 取 4 会明显偏大）。</summary>
        const float SkullScoreX01 = 0.11740f, SkullScoreY01 = 0.11134f;

        // ---- 手牌数底板：原版 `BottomAnchor/PlayerArea/HandArea/CardsInHandText/Bg (1)` ----
        // 出处：`RectTransform_3212.json`（rect 288.16×89.56、localScale 0.009）/
        //       `RectTransform_3400.json`（父 `CardsInHandText` 的 localScale 0.925926）/
        //       `Transform_1401.json`（祖父 `HandArea` 是**纯 Transform**，scale 108）/
        //       `MonoBehaviour_4418.json`（图 `40K_display`、α 0.6941177、PreserveAspect=1）。
        /// <summary>整条缩放链 108 × 0.925926 × 0.009 = **0.9**（正好），所以实绘
        /// 288.16×0.9 = 259.3 宽、89.56×0.9 = 80.6 高；PreserveAspect=1 + 图比例 3.946 →
        /// **宽度顶满**、实绘高 = 259.3/3.946 = 65.7。
        /// ⚠️ 权威表 `:119` 记的「288.2×89.6 绝对像素」是漏乘 0.009 的直读。
        /// ⚠️ 实况没验到：这两个节点在 dump 里 `activeInHierarchy=False`（dump `:336-337,344-345`），
        ///    而且它的祖先链是纯 Transform、**算不出绝对位置** → 位置是**我们挑的**（贴在既有手牌标签上）。</summary>
        const float HandPlatePx = 65.7f;

        /// <summary>END TURN 按钮的中心（归一化）。**判据只有这一份** —— 建按钮、建文字、
        /// 以及换分辨率重贴，三处都读它。
        /// ⚠️ 踩过（2026-09-12）：建的时候改到右侧了，但换分辨率重贴那段**把旧坐标又写了一遍**，
        /// 结果按钮在右边、文字留在右下角（截图抓到的）。</summary>
        const float EndTurnX01 = 0.96263f, EndTurnY01 = 0.57819f;

        // ---- 右侧能量区（原版 `Energy And turn holder` 那一竖排）----
        // **判据只有这一份** —— 建、换分辨率重贴都读它。出处：`资料/战斗规格/战斗重建_0827/战斗界面JSON权威表_0827.md`
        // B 节「能量水晶区」的 chain_rect 绝对坐标（x[1826.3,1900.8] 这种）。
        // 换算：x01 = 中心x/1920，y01 = 1 − 中心y(从上)/1080。
        /// <summary>我方能量水晶 `PlayerMana`：x[1827.8,1903.9] y[517.1,594.1]</summary>
        const float MyEnergyX01 = 0.97180f, MyEnergyY01 = 0.48556f;
        /// <summary>敌方能量水晶 `EnemyMana`：x[1826.3,1900.8] y[249.8,327.4] —— **原来我们压根没画这一颗**</summary>
        const float FoeEnergyX01 = 0.97060f, FoeEnergyY01 = 0.73278f;
        /// <summary>能量底板 `Energy Player`（图 `Card Frame Cost Icon`）：实绘 94.6 × 91.3 px</summary>
        const float EnergyPlateH = 91.3f;
        const float MyEnergyPlateX01 = 0.97352f, MyEnergyPlateY01 = 0.48569f;
        const float FoeEnergyPlateX01 = 0.97232f, FoeEnergyPlateY01 = 0.73292f;
        /// <summary>任务点 `QuestPointsHolder`（97.7²）：我方水晶**下方** x[1817.0,1914.7] y[595.4,693.1]、
        /// 敌方水晶**上方** x[1816.1,1913.8] y[150.2,247.9]。
        /// ⚠️ 2026-09-12 更正：原来写的是 0.52019 / 0.69907 —— 那是把 `BackgroundJoin`（接片）
        /// 的子偏移当成了 holder 的偏移，两个图标都贴在**水晶内侧**。现在按上面的绝对坐标摆。</summary>
        const float QuestPx = 97.7f;
        const float MyQuestX01 = 0.97180f, MyQuestY01 = 0.40347f;
        const float FoeQuestX01 = 0.97133f, FoeQuestY01 = 0.81569f;
        /// <summary>任务点接片 `BackgroundJoin`（35.2×23.9），在水晶与任务点之间</summary>
        const float QuestJoinPx = 23.9f;
        const float MyQuestJoinY01 = 0.43807f, FoeQuestJoinY01 = 0.78200f;
        /// <summary>张数文字离牌堆中心多远（归一化高度）：165 px / 1080。出处见 `BuildHud` 牌堆那段</summary>
        const float DeckLabelDy01 = 165f / 1080f;

        // ---- 阵营资源（信仰 / 灵魂石）—— 2026-09-13 第三十三轮 ----------------------------
        // 出处：运行时 dump `runtime_ui_dump_drive_0912.tsv`，`Energy And turn holder/{Player,Enemy}Mana`
        // 子树下的 `FaithHolder` / `SpiritStoneHolder`（**anchorMin=anchorMax=(0,0)** ⇒
        // `anchoredPosition` 是相对**父物体左下角**的偏移，父物体 = 能量水晶 `{Player,Enemy}Mana`）。
        //
        // 换算（`X01 = cx/1920`、`Y01 = 1 - cy/1080`，`cy` 用 dump 的**自上而下**坐标）：
        //   · 我方 FaithHolder  anchored (38.0, −57.1) @水晶左下 → 中心 (1865.8, 651.2)
        //   · 敌方 FaithHolder  anchored (39.5, 134.8) @水晶左下 → 中心 (1865.8, 192.6)
        //   · 我方 SpiritStone  anchored (42.3, −50.2)               → 中心 (1870.1, 644.3)
        //   · 敌方 SpiritStone  anchored (42.3, 127.6)               → 中心 (1868.6, 199.8)
        // 🔎 **换算的独立佐证**：任务点数字 `_qpTextMe` 落在 (1865.85, **644.0**)，而这两个 holder 落在
        //    651.2 / 644.3 —— **同一个槽位**。原版这三件本来就是**按阵营互斥**的
        //    （`ManaTypeHolder.Toggle` → `SetActive`，任务点只给暗黑天使），位置重合是对的。
        const float FaithW = 117.9f, FaithH = 149.3f;      // sprite `40k_Battle_Display_Faith`
        const float StoneW = 112.1f, StoneH = 116.6f;      // sprite `UI_Energy_Eldar`
        const float StoneGem = 51.0f;                      // 子物体 `SpiritStone`：sprite `UI_Gem_Eldar`
        const float MyFaithX01 = 0.97177f, MyFaithY01 = 0.39704f;
        const float FoeFaithX01 = 0.97177f, FoeFaithY01 = 0.82167f;
        const float MyStoneX01 = 0.97401f, MyStoneY01 = 0.40343f;
        const float FoeStoneX01 = 0.97323f, FoeStoneY01 = 0.81500f;

        int _selectedSlot = -1;
        /// <summary>定下来的打法（原版 `attackType`）。`None` = 还没选</summary>
        AttackKind _command = AttackKind.None;
        /// <summary>按住了棋盘上的哪个单位、**哪一侧**（松手没拖够 = 开/关大卡展示窗；拖够距离 = 弹攻击选择器）。
        /// 🔴 2026-09-28 照原版改的 —— 判据与出处见 `Update` 里 ② 那一段的注释</summary>
        int _pressSlot = -1, _pressSide = -1;
        Vector3 _pressWorld;
        float _aiTimer;

        /// <summary>AI 这回合已经走了几步（防死循环）。超过 <see cref="AiStepLimit"/> 就收手并报警 ——
        /// 一步一条动作的循环里，只要有一条动作**执行成功但不改变状态**就会原地打转，
        /// 而那种情况在真机上表现为「卡住不动」，没有日志（红线：不许静默失败）。</summary>
        int _aiSteps;

        /// <summary>🆕 2026-09-29（Q6 后半段）：**本回合被引擎拒过的动作**（退次优用的排除名单）。
        /// 每成功一条就清空；连着被拒 <see cref="SimpleAI.MaxRejectedActions"/> 条才收手。
        /// ⚠️ 它**跨帧存活**（`DriveAiTurn` 一帧只走一步），所以是字段不是局部变量。</summary>
        readonly List<AiAction> _aiRejected = new List<AiAction>();

        /// <summary>AI 一回合最多走几步。正常一局远到不了（手牌 + 单位数就那么多）。</summary>
        const int AiStepLimit = 40;

        /// <summary>AI 每步之间的间隔（秒）—— 太快玩家看不清发生了什么</summary>
        public float aiStepDelay = 0.55f;

        /// <summary>
        /// 对手难度（设置面板里可改）。语义照原版 `DeckDifficultyLevel`
        /// （`SuperEasy=0 / Easy=5 / Normal=10 / Hard=15`，见 `资料/AI_原版反编译_0917.md` §五·二）。
        ///
        /// **它只调一件事**：原版 `TweakAvailableActions` 那三个旋钮（`minSkips / maxSkips / skipChance`）
        /// —— 也就是「把 AI **本来想做的事**，按最低分往下随机砍掉多少」。
        /// 原版没有搜索深度这回事（`AI` 是 1 层贪心 + 随机砍动作），我们照它来。
        ///
        /// ⚠️ **四个档的具体取值是我们配的**（原版那三个数在 `AIBotsConfig` 的资产里、本地没有，
        ///    和 `ScoringCriteria` 同一种情况）—— 结构照原版、数值我们配，见 `SimpleAI.KnobsOf`。
        /// </summary>
        public AiDifficulty aiDifficulty = AiDifficulty.Normal;

        // ==================================================================
        //  回合时钟（原版 `ClockManager` / `Countdown`）
        //  数值**全部有出处**，见每个字段的注释；机制也照反编译的方法体来（不是我们编的）。
        // ==================================================================
        /// <summary>每回合基准时长（秒）。出处：`DefaultScenario.json:19` `clockTimeLimit = 60`
        /// （`bundle_duplicateassetisolationso_assets_all/MonoBehaviour/`）。
        /// ⚠️ 这**只是「没有 matchType 覆盖」时的默认值** —— 本局实际用多少见 `TurnSecondsForThisMatch`。</summary>
        public float turnSeconds = 60f;

        /// <summary>本局的 matchType。原版拿它覆盖一整套数值（时钟、换牌倒计时跳不跳…）。
        /// 🔴 **我们取 80 = EventAI**（原版「对 AI 打一局」的那个模式）。理由：它是唯一一个
        /// **「对 AI」且换牌倒计时仍然生效**的模式（只有 `0x32`(50, PracticeOffline) 会整段跳过倒计时）
        /// ⇒ 与我们按用户要求做出来的换牌倒计时自洽（见 `mulliganSeconds`）。</summary>
        public int matchType = 80;

        /// <summary>本局实际的回合时长（秒）—— **照原版 `ClockManager.GetTotalTime` 的三步结构**：
        ///  ① 先取 `ScenarioVariables.clockTimeLimit`（= 60，`turnSeconds`）；
        ///  ② 某个「缩时」标志为真时改取 `clockTimeLimitReduced`（= 10，我们走 `reducedTurnSeconds` 那套）；
        ///  ③ **最后按 matchType 覆盖**：`0x50`(80, EventAI) → **240 s**、`0x32`(50, PracticeOffline) → **600 s**
        ///     （`ClockManager__GetTotalTime.c:17-36`；两个常量在 DLL 的 `.rdata` 里，
        ///      已用 `工具/read_literal.py` 复核：`0x1834b31c4` = **240.0**、`0x1834b2ed8` = **600.0**）。
        /// 🔴 **2026-09-17 用户拍板「按原版设计执行」** —— 原来这里写的是
        ///    「600 s 等于没有压力，所以默认取 60」，那是**按我们的口味改了原版的取值**，已去掉。
        ///    ⇒ 本局（matchType 80）= **240 s**。</summary>
        public float TurnSecondsForThisMatch
        {
            get
            {
                float v = turnSeconds;                  // ① ScenarioVariables 的默认
                if (matchType == 80) v = 240f;          // ③ EventAI
                else if (matchType == 50) v = 600f;     // ③ PracticeOffline
                return v;
            }
        }
        /// <summary>「缩时」时长（原版 `clockTimeLimitReduced = 10`，同上 :20）。
        /// 触发条件是「上一回合**超时且整回合零动作**且**不是对 AI**」（`EndTurnClick.c:135-152`）——
        /// 我们这局是对 AI，按原版判定**永远不会进缩时**，所以只留字段、不写死逻辑（写注释不写死代码）。</summary>
        public float reducedTurnSeconds = 10f;
        /// <summary>总时长走完后再显示的倒计时秒数（原版 `clockCountdownSec = 15`，同上 :21）。
        /// 这一轮走完就**自动结束回合**（原版 `ClockManager__Update.c:110-141` → `EndTurnClick(true)`，无惩罚）。</summary>
        public float countdownSeconds = 15f;
        /// <summary>剩这么多秒开始催（原版 `timeToHurryUp = 35.0`）。原版是发一句语音
        /// （`DisplayHurryUpChatMessage`，每回合一次）；我们没接音频，**改成数字变色** ——
        /// ⚠️ 变色是**我们挑的表现**，不是原版的做法。</summary>
        public float hurryUpSeconds = 35f;

        /// <summary>「能量累积」那盏灯亮不亮 —— **原版判据 = `0 &lt; GameplayVariablesData.manaAccumulation`**
        /// （`BattleManager__SetupBoardPhase.c:181/196` 调 `PlayerManager.SetAccumulationMana(0 &lt; *(int*)(vars+0x34))`；
        ///  那个字段是 `Everguild/LiveOps/GameplayVariablesData.cs:32 public int manaAccumulation`）。
        /// 🔴 **这个数是 LiveOps（服务端下发）的，本地拿不到**（与 `overtimeTurn` 同一类）
        /// ⇒ **判据是原版的、这个默认值 0 是我们挑的**（0 = 关，与实况 dump 拍到的 OFF 那张一致）。</summary>
        public int manaAccumulation = 0;

        /// <summary>本回合还剩多少秒（走表用）。`_clockInCountdown` = 已经进「超时后的 15 秒」那一段</summary>
        float _clockLeft;
        bool _clockInCountdown;
        /// <summary>本回合的 `hurry` 语音已经说过了吗（原版 `ClockManager` 的 `latch_0xb8`）——
        /// 由 `ResetClock()` 复位（= 原版 `StartTimer` 里那句 `0xb8 = 0`）。**每回合只播一次。**</summary>
        bool _hurrySaidThisTurn;
        /// <summary>玩家这个回合做了几个动作 —— 原版缩时判定要用（见 `reducedTurnSeconds`）</summary>
        int _actionsThisTurn;
        Label _clockLabel;

        /// <summary>把自己的手牌索引找出来（落点校验要用）</summary>
        /// <summary>
        /// 这张视图在**引擎手牌**里排第几（-1 = 不在手里）。
        ///
        /// 🔴 第 7 行第 4 步（2026-09-18）：**先按实例身份反查**（`CardView.Inst` 在 `Hand` 里排第几）——
        /// 只按位置查的话，手牌在这期间变了（抽牌 / 弃牌 / 效果改手牌）就会指到**另一张**上，
        /// 而那正是「点了一张、打出另一张」那种静默错。
        /// </summary>
        int HandIndexOf(CardView v)
        {
            if (v == null || Ctx == null) return -1;
            if (v.Inst != null)
            {
                int k = Ctx.Players[_me].Hand.IndexOf(v.Inst);
                if (k >= 0) return k;
            }
            return _handViews.IndexOf(v);      // 退路：位置（`Inst` 为空的视图 —— 场上/墓地那些）
        }

        /// <summary>
        /// 进 Play 模式自动开一局。
        ///
        /// ⚠️ 两件事必须在这里做：
        ///   1. **重新 `LayoutSpace.Apply(cam)`** —— `LayoutSpace.Cam` 是静态字段，
        ///      **静态状态不进 Play 模式**（编辑器里建场景时设过，运行时是 null）。
        ///      不重设的话所有归一化坐标会退回默认宽高比，超宽屏/4:3 上全部错位。
        ///   2. 卡牌是**运行时生成**的（不烘进场景，所以 Battle.unity 只有 79 KB），
        ///      不调 `Begin()` 打开就是一块空场。
        /// </summary>
        void Start()
        {
            if (cam != null) LayoutSpace.Apply(cam);
            // 🆕 2026-09-26：**联机对局靠 `NetRuntime` 收包**（它 `DontDestroyOnLoad` 跨场景活着），
            //    这一句是兜底 —— 直接打开 `Battle.unity` 按 Play（没经过壳）时也要有一台来泵。
            NetRuntime.Ensure();
            // 背景：场景里存的是建好的 quad，但组件上的私有引用不进序列化，运行时得重绑一次
            if (backdrop != null) backdrop.Build();
            // ⚠️ **`HookAnimFxShake` / `HookAnimFxCards` 不在这里** —— 2026-09-19 挪进 `Begin()`
            //    （批处理不走 `Start`，而自检要量「钩子接上了没有」⇒ 两条路必须同源）。
            if (Ctx == null)
            {
                // 🆕 2026-09-27：**回放优先** —— 从对局历史点「回放」时，菜单那边把录像挂进
                //   `ReplayStore.PendingPlay` 再切场景；战场这边开场景时先问它有没有。
                //   （与联机那条 `NetPendingBattle.Current` 是同一个路数。）
                var pendingReplay = ReplayStore.TakePending();
                if (pendingReplay != null) PlayReplay(pendingReplay);
                else BeginFromDeckLibrary();
            }
        }

        /// <summary>AnimFX 模块要的「出手卡 / 目标卡」（原版 `controller.actingCard/targetCard`）。
        ///
        /// 怎么实现：`WFEffectCards.Resolver` 收的是一个**正在播的播放器**，但「为哪两张卡播的」
        /// 是**发起那一刻**才知道的 —— 所以用「**当前上下文**」：`PlaySignal` 在调 `FireEvent`
        /// **之前**把这一场填好（`BuildCardContext`），播完清掉。
        /// 之所以成立：`CardEffects.FireEvent` → `WarpforgeEffectPlayer.Play` → `BuildModules`
        /// **整条是同步的**，模块读的时候上下文还在。
        ///
        /// ⚠️ 我们不传 `player` 只用当前上下文 ⇒ **嵌套播放**（一个特效里又起一个）会读到外层上下文。
        ///    目前没有这种调用；真出现时改成按 player 查表。
        /// ⚠️ **其余的 AnimFX 钩子还没接**（各自要的下游不同，见 `资料/AnimFX_实现与接线.md`）。</summary>
        void HookAnimFxCards()
        {
            WarpforgeVFX.WFEffectCards.Resolver = _ => _animfxCtx;
            // `ScaleByTarget` 用的是「往模块实例的两个字段里填」的形状（它在数据装配时拿不到卡），
            // 所以这里转发同一份上下文 —— 两处**同源**，别各查一次（CLAUDE.md 三·5）。
            WarpforgeVFX.WFModuleScaleByTarget.CardResolver = m =>
            {
                if (m == null) return;
                m.actingCard = _animfxCtx.actingCard;
                m.targetCard = _animfxCtx.targetCard;
            };
            // 🆕 2026-10-01（§三 第 26 条 · ⑥ 的接线那半）：**补间模块也要同一份卡上下文** ——
            //   原版是 `BuildSequence(tweenAnims[i], controller.actingCard, controller.targetCard)`
            //   （`AnimFXModuleTween.PlayAnimCoroutine_d__7__MoveNext.c:44-45`）。
            //   ⚠️ 上面那两处已经转发过 `_animfxCtx`，这里是**第三处**：**同源，别各查一次**
            //      （CLAUDE.md 三·5）。下游 `UnitTweenRuntime` 再把「该播哪一串」接到 `UnitTweens.json`。
            WarpforgeVFX.WFModuleTween.CardResolver = m =>
            {
                if (m == null) return;
                m.actingCard = _animfxCtx.actingCard;
                m.targetCard = _animfxCtx.targetCard;
            };
            // 碰撞模块的卡上下文 —— 同样从 `_animfxCtx` 转发（`targetIsWarlord` 用格位判）。
            // ✅ **2026-10-01：`ColliderLookup` 也接上了**（见下面那句 + `ParticleCollider`）。
            //    这里原来的三条理由**都已作废**，逐条留个痕（铁律 5）：
            //    ① 「`*FromCamera` 原版怎么定的查不到」—— 2026-09-18 就更正过：7 个都是**场景里手摆的固定
            //       Transform**（`BattleParticleColliderManager` 的 7 个字段，`GetColliderTransform.c` 逐值对上
            //       `+0x20…+0x50`），坐标从 `07_场景/battlearena1/` 读出；
            //    ② 「要接得走『格位节距 149.3 px』那座桥」—— 那条 px 旁证 **2026-09-20 已作废**；真桥是
            //       `slotZ = (z + 6.698) / 7.664 × 1.621`，**只取比值**（同一世界系里两个距离之比）；
            //    ③ 「我们战场只摆了烘平的背景图 ⇒ 对应物要先建」—— 战场**已是真 3D**（`Arena3D`，09-20 落地）⇒
            //       现在按 `ArenaSlots` 的兵线现算就行（`ParticleCollider` 里那 7 个空物体 = 原版那 7 个物体的替身）。
            WarpforgeVFX.WFModuleCollisions.ContextResolver = m =>
            {
                if (m == null) return;
                m.actingCard = _animfxCtx.actingCard;
                m.targetCard = _animfxCtx.targetCard;
                m.actingIsPlayer = _animfxCtx.actingIsPlayer;
                m.targetIsPlayer = _animfxCtx.targetIsPlayer;
                m.targetIsWarlord = _animfxLastEvent.TargetSlot == RuleEngine.BoardSpec.WarlordSlot;
            };
            // 🆕 2026-10-01（§三 第 9 条 · ①）：**`ColliderLookup` 接上了**（原来一直是 null ⇒ 粒子碰撞平面
            //   一条也加不上、只记 `DroppedPlanes`）。7 个替身物体 + 坐标桥见 `ParticleCollider`。
            WarpforgeVFX.WFModuleCollisions.ColliderLookup = id => ParticleCollider(id);
            // 两条**兵线中心** —— `ScaleByTarget.ChangeShapeAngle` 要它（原版读的是
            // `BattleParticleColliderManager` 的 `playerMinionCollider` / `enemyMinionCollider`）。
            // 🔑 **不另摆空物体**：兵线的定义本来就在 `BoardLayout` / `ArenaSlots` 上。
            // 🔴 **2026-10-12（A368）**：这一支原来**写死 2D**（`playerBoard.SlotPosition`），
            //   而消费方拿它算的是**比值**（`lineD ÷ cardD`）⇒ 真 3D 下两张卡是战场世界坐标、
            //   兵线却是 HUD 正交平面的点 —— **两个量不在同一个世界系**。
            //   ⇒ 现在**转发到 `TryMinionLines`**（与 `PositionParticleColliders` 共用那**一处**判据，
            //   铁律「两处写同一条规则 = 迟早不一致」）；3D 支的写法见那个方法。
            WarpforgeVFX.WFModuleScaleByTarget.MinionLines = TryMinionLines;

            // 🆕 2026-10-01（§三 第 9 条 · ⑤）：`MoveParticlesToTarget` —— **把粒子吸向那一侧的灵石锚点**
            //   （原版 `BattleManager → PlayerManager.GetSpiritStoneManaTransform()`；唯一那条效果
            //    `Remnant Aeldari Collect particles` 正是灵族收集灵石那一下）。
            //   🔴 **目标在 HUD 空间**（灵石图标是 HUD 上那两枚 quad），而粒子在**棋盘相机**的空间里
            //   ⇒ 两台相机都要给它：模块里做 `WorldToViewportPoint` →（换深度）→ `ViewportToWorldPoint`
            //     （判据 = `CamerasConversionHelper__ConvertPositionBetweenCameras.c:17-27`；原版调用点那句还带
            //      `desiredZPosition − boardCamera.position.z` 当深度）。**不接相机它就会吸到错的地方**（且会出声）。
            //   ⚠️ 用的是 `_myStoneIcon` / `_foeStoneIcon` 两个**现成引用** —— 不另摆空物体，
            //     与 `MinionLines` 同一条做法（CLAUDE.md 三：有引用就用引用）。
            WarpforgeVFX.WFModuleMoveParticlesToTarget.FromCamera = () => cam;
            WarpforgeVFX.WFModuleMoveParticlesToTarget.ToCamera = () => boardCam;
            WarpforgeVFX.WFModuleMoveParticlesToTarget.ResolveTarget = () =>
            {
                var q = _animfxCtx.actingIsPlayer ? _myStoneIcon : _foeStoneIcon;
                return q != null ? q.transform : null;
            };

            // ---- 卡背（原版 `BattleManager.GetCardback(bool isPlayer)`）----
            //
            // 🔴 **这一条原来根本没接**（2026-09-19 补）：`WFModuleCardback.CardbackResolver`
            //    全工程**只有声明、从没有赋值点** ⇒ `ResolveCardback()` 返 null ⇒
            //    `Initialize` 在 `:120` 早退（原版「拿不到卡背」那条路：LogError + 一个粒子都不改）
            //    ⇒ **卡背相关的 18 个效果全都不出声**（`CreateCard*` 家族 + `DeckBuff_MoveToTop`
            //    + `Sau_ReconstitutionProtocol` + `Wild Rider Chieftain`，30 实例）。
            //    根因与两条修法（运行期接线 / 导出器补 tsa）见
            //    `资料/普查产出_0918/孤儿待办_五条查证.md` §一 + 下面的 `CardBackSprite`。
            //
            // 原版取的是**玩家档案里的卡背装饰品**（`PlayerDataManager.GetCosmeticItem` →
            // `CosmeticItemCardback.GetCardBackSprites().Item1`）；我们单机没有档案 ⇒
            // 退化成**按那一方的阵营取**我们已经有的 4 张 `Art/cards/back_<faction>.png`。
            // ⚠️ 走 `CardArt.CardBack`（**牌堆、敌方手牌用的是同一张** ⇒ 同源，别另挑一张）。
            // ⚠️ 实参语义照原版：那是 **`actingCard.isPlayer`**（为谁的卡播的），不是「我是谁」——
            //    `true` = 我方那张、`false` = 敌方那张。
            WarpforgeVFX.WFModuleCardback.CardbackResolver = isPlayer =>
                CardBackSprite(isPlayer ? _myFaction : _foeFaction);

            // 🆕 2026-10-01（§三 第 31 条 · 第 2 件）：**`InstanceParticleAdjacent` 接上了**
            //   （全库 1 实例 / 1 效果：`BulletImpact_deathspinner_arc_alt`）。
            //
            // 原版 `ExecuteEffect`（`AnimFXInstanceParticleAdjacent__ExecuteEffect.c:42-104`）：
            //   `units = BattleManager.GetAdjacentUnits(controller.targetCard)` → `foreach (unit)`
            //   `CreateAnimFromAnimFxController(animInfo, actingCard.transform, unit.transform,
            //      unit.transform, actingCard, unit, null, false)`。
            //
            // 🔴 **「播在哪」不用猜** —— 判据在 `BattleManager__GetAnimTransform.c` 的 switch：
            //    `case 0` 取**第一个**传入的 Transform、`case 1`/`case 2` 取**第二个**。
            //    这份 CardAnim 是 `startPosOption = endPosOption = 1`
            //    （`Deathspinner Slice Card Target.json`），而传进去的第二个正是 `unit.transform`
            //    ⇒ **起点和终点都是那个相邻单位**（所以 `shouldMoveVFX = 0` 自洽、`timeAtStartPos = 0.5`
            //    就是「在单位处停 0.5 s」）⇒ 我们**挂在每个相邻单位身上、不做位移**。
            //
            // ⚠️ 相邻判定**只读 `BoardSpec.AdjacentSlots`**（`RuleEngine/Core/Aura` 里 `AdjacentSlots` 的出处注释明写
            //    「只读它、别另写」）；「哪一方」= **被打那张卡**的所属方（原版传的就是 targetCard）。
            // ⚠️ 所以这里按 `TargetSlot` 取，**不是**出手卡那一格 —— 两者只在攻击事件上不同，
            //    与 `BuildCardContext` 里 `targetCard` 的取法**同一条判据**。
            WarpforgeVFX.WFModuleInstanceParticleAdjacent.OnExecute = (player, module) =>
            {
                if (module == null || string.IsNullOrEmpty(module.prefabName)) return;
                if (!_animfxCtx.valid) return;                    // 没有卡上下文 = 这一拍不该有它
                var e = _animfxLastEvent;
                bool onTarget = e.Kind == EvtKind.Attack;          // 其余事件 acting/target 同源
                int side = onTarget ? e.TargetPlayer : e.Player;
                int slot = onTarget ? e.TargetSlot : e.Slot;
                AdjacentUnitViews(side == _me, slot, _adjViews);
                for (int i = 0; i < _adjViews.Count; i++)
                    WarpforgeVFX.WarpforgeEffectPlayer.Play(module.prefabName, _adjViews[i].transform, Vector3.zero);
            };

            // 🆕 2026-10-01（§三 第 31 条 · 第 1 件 · **卡材质那一半**）：`ChangeMaterial` 的三个回调。
            //   判据（**原版逐行读过**）：`BattleCardUI__SetCardMaterial.c` / `…__RestoreOriginalMaterial.c`
            //   —— **全在【卡 3D 体】那一个 Renderer 上**（原版 `+0x180`）；
            //   我们这边的对应物 = `CardView._body3D`，那条判据**只写在 `CardView` 里**
            //   （`SetCardMaterial` / `RestoreOriginalMaterial` / `SetCardImageTo3DBase`）——
            //   这里只做「`Transform` → `CardView`」那一跳，**不重复实现一遍**（CLAUDE.md 三·5）。
            WarpforgeVFX.WFModuleChangeMaterial.SetCardMaterial = (card, m, initWithImage) =>
            {
                var v = CardViewOf(card);
                return v != null ? v.SetCardMaterial(m, initWithImage) : null;
            };
            WarpforgeVFX.WFModuleChangeMaterial.RestoreOriginalMaterial = card =>
            {
                var v = CardViewOf(card);
                if (v != null) v.RestoreOriginalMaterial();
            };
            WarpforgeVFX.WFModuleChangeMaterial.CardTexture = card =>
            {
                var v = CardViewOf(card);
                return v != null ? v.CardImageTexture : null;
            };
        }

        /// <summary>**`Transform` → 它属于哪张卡**（原版那两个回调收的就是 `actingCard`）。
        ///
        /// 卡根上就挂着 `CardView`（`AnimFxCardViewAt` 给的正是 `v.transform`）⇒ `GetComponentInParent`
        /// 一次就命中（它包含自身）。**这不是热路径**（只在 `ChangeMaterial` 那 3 个效果上走），
        /// 所以不做缓存 —— 缓存反而会在卡视图重建时留下悬空引用。
        /// 取不到返回 `null`，调用方**出声**（`WFModuleChangeMaterial` 那边会 `WarnNoCardContext`）。</summary>
        CardView CardViewOf(Transform t)
        {
            if (t == null) return null;
            return t.GetComponentInParent<CardView>();
        }

        // ============================================================ 后期（LUT / Bloom）下游
        // 🆕 2026-10-01（§三 第 31 条 · 第 3 件）：**`WFModulePostProcess`（52 实例 / 52 效果）的下游**。
        //   原版那一层是 `PostFXController` + `LUTBlender`（两个 MonoBehaviour，判据与四条如实标注
        //   写在 `BattlePostFx.cs` 文件头）。这里只负责：**战场一载进来就把那条链接上**。
        //   ⚠️ **必须挂在「载完战场之后」**（那个全局 `Volume` 在战场 prefab 上）——
        //      这也是为什么它不在 `Begin` 开头的 `HookAnimFx*` 那一串里。
        BattlePostFx _postFx;

        void AttachPostFx(GameObject arena)
        {
            DetachPostFx();
            if (arena == null) return;
            var vol = arena.GetComponentInChildren<Volume>(true);
            if (vol == null)
            {
                Debug.LogWarning("[Battle] 🔴 这个战场 prefab 里没有 `Volume` ⇒ 原版的 Bloom/Vignette 与 "
                               + "**LUT 链整条不生效**（跑 `-executeMethod ArenaBuilder.BuildArenaPrefabs` 重建）");
                return;
            }
            _postFx = new BattlePostFx();
            bool ready = _postFx.Attach(vol);
            WarpforgeVFX.WFModulePostProcess.OnPostFx = r => _postFx.Handle(r);
            Debug.Log($"[Battle] 后期下游接上：`{vol.name}` 上的 Volume · LUT 链 {(ready ? "就绪" : "**没就绪**")}"
                    + $" · `ColorLookup` 是运行时补出来的 {_postFx.MissingColorLookup} 次"
                    // 🔴 **2026-10-12 订正（A293 · 同源错记的第三处）**：这一句原来写「补的那种 =
                    //   原版静态 LUT **也是空的** 6 场之一」—— **前半句是错的**。
                    //   真相（逐场现读 13 场 × 2 份 `ColorLookup`）：原版 **13/13 场都有 `ColorLookup`**
                    //   （`active = 1` · `contribution.m_Value = 1.0` · `texture.m_OverrideState = 1`）；
                    //   我们补的那 6 场（arena1/2/3 · astramilitarum · blacklegion · darkangels）在原版里
                    //   指的是**共享的那一张** `LUT Normal`（`PathID 382974660631151556`，在
                    //   `battlesharedresources` 包里；`m_FileID = 9`、`arena2` 是 `10` —— 那是 externals
                    //   表下标，别当常量）；另外 **7 场**（aeldari · emperorschildren · genestealers ·
                    //   leviathan · sororitas · spacewolves · tauviorla）`m_FileID = 0` = 包内自带。
                    //   ⇒ **不是「原版也是空的」**，是「原版的槽本来就在、我们构建时没建那个槽」。
                    //   错因 = 只量了 `battlearena1` 一场就写成通用结论（铁律 5·c）。
                    //   判据只此一处 = `BattlePostFx` 文件头第 ② 条；
                    //   逐场实读 → `资料/普查产出_1010/丁_A219_A177尾_A293_A289.md` §②。
                    + "（补的那种 = 原版那 6 场自己包里**没有**专属 LUT ⇒ 指的是共享的 `LUT Normal`，"
                    + "见 `BattlePostFx` 文件头第 ② 条）");
        }

        void DetachPostFx()
        {
            // 静态回调该摘就摘（与 `SimpleAI.Executed` 同一条纪律）——
            // 不摘的话下一个场景里 `OnPostFx` 还指着已销毁的这个 driver。
            WarpforgeVFX.WFModulePostProcess.OnPostFx = null;
            if (_postFx != null) { _postFx.Detach(); _postFx = null; }
        }

        /// <summary>自检用：这一局的后期下游（没接上返回 null）。</summary>
        public BattlePostFx PostFxForTest { get { return _postFx; } }

        /// <summary>`AdjacentSlots` 的复用缓冲（它 `into.Clear()` 后就写，别每次 new）。</summary>
        readonly List<int> _adjBuf = new List<int>();
        readonly List<CardView> _adjViews = new List<CardView>();

        /// <summary>**某一格左右相邻格里「活着的」单位视图** —— 原版 `BattleManager.GetAdjacentUnits`。
        ///
        /// 🔴 判据：相邻格**只读 `RuleEngine.BoardSpec.AdjacentSlots`**
        ///    （`RuleEngine/Core/Aura` 里 `AdjacentSlots` 的出处注释明写「只读它、别另写」—— 别在表现层重算 `slot ± 1`）。
        /// ⚠️ **空格与已阵亡的都不算**：阵亡的卡会被从 `_myUnits/_foeUnits` 里摘掉
        ///    （见 `:1470` 那条注释），所以「查得到视图」本身就是「这个单位还在场上」。
        /// 抽成方法是为了自检能**直接量它**，而不是去跑一整条特效链。</summary>
        /// <param name="mine">true = 我方那一侧（`_myUnits`）· false = 敌方（`_foeUnits`）</param>
        public void AdjacentUnitViews(bool mine, int slot, List<CardView> into)
        {
            into.Clear();
            if (slot < 0) return;
            _adjBuf.Clear();
            RuleEngine.BoardSpec.AdjacentSlots(slot, _adjBuf);
            for (int i = 0; i < _adjBuf.Count; i++)
            {
                var v = BoardViewAt(_adjBuf[i], mine);
                if (v != null) into.Add(v);
            }
        }

        // ============================================================ 粒子碰撞平面（原版 7 个场景物体）
        //
        // 判据（全部实读，别改）：
        //  · **位置/缩放**：`资料/AnimFX_实现与接线.md` §11.6 d) 那张表（原版 `BattleParticleColliderManager`
        //    的 7 个 `[SerializeField] Transform`，从 `07_场景/battlearena1/` 的 GameObject/Transform 读出）。
        //  · **旋转**（2026-10-01 补读，那张表里没有）：`Transform/Transform_<pid>.json` 逐个解出来 ——
        //      `Floor`         identity                  ⇒ **法线 +Y**（水平面）
        //      `Player`/`PlayerWarlord`   180° about (0,−0.707,−0.707) ⇒ **法线 +Z**（朝敌方）
        //      `PWF`/`Enemy`/`EnemyWarlord`/`GenericTarget`  −90° about X ⇒ **法线 −Z**（朝我方）
        //    ⚠️ `PWF`（From Camera）**与敌方同朝向** —— 名字是用途名不是算法（§11.6 e 也这么写）。
        //  · **坐标桥**：`slotZ = (z + 6.698) / 7.664 × 1.621` ⇒ **只取比值**（两条兵线都在同一个世界系里，
        //    所以这个比值对任何一套坐标都成立）：落点 = 我方兵线 + (该值 / 1.621) × (敌方兵线 − 我方兵线)。
        //  · **X = 督军槽中心**（原版那 7 个物体的 x 都是 0）：我们的兵线取 `x = 0`、`z` 取**兵线那一排**的
        //    （⚠️ **不能用督军槽自己的 z** —— 它带 `heroExtraOffset`（我方 −0.42 / 敌方 −0.75），
        //     两边偏移不同会把两条兵线的间距算歪）。
        Transform[] _pColliders;
        static readonly int[] PColliderIds = { 0, 5, 7, 8, 10, 11, 15 };
        static readonly float[] PColliderZ = { 1.417f, 0f, -0.089f, -0.089f, 1.621f, 1.463f, 1.463f };
        /// <summary>平面法线朝哪边：`+1` 朝敌方 · `−1` 朝我方 · `0` 水平（`Floor`）。</summary>
        static readonly int[] PColliderUp = { 0, +1, +1, -1, -1, -1, -1 };
        static readonly float[] PColliderScale = { 1f, 2.5f, 2.5f, 2.5f, 2.5f, 2.5f, 2.5f };
        static readonly string[] PColliderNames = {
            "Floor Position Reference", "Player Minions Particle Collision", "Player Warlord Particle Collision",
            "Player Warlord From Camera Particle Collision", "Enemy Minions Particle Collision",
            "Enemy warlord Particle Collision", "Generic Target" };
        /// <summary>粒子碰撞平面**建出来了没有**（自检用）。</summary>
        public bool ParticleCollidersBuilt { get { return _pColliders != null; } }
        /// <summary>自检用：某一块平面的世界位置/朝向（`id` 不在表里返回 false）。</summary>
        public bool ParticleColliderAt(int id, out Vector3 pos, out Vector3 up)
        {
            pos = Vector3.zero; up = Vector3.up;
            var t = ParticleCollider(id);
            if (t == null) return false;
            pos = t.position; up = t.up;
            return true;
        }

        /// <summary>原版 `BattleParticleColliderManager.GetColliderTransform(id)` 的替身。
        /// **第一次要的时候才建**（7 个空物体，没有渲染器 —— 它们只当粒子碰撞的「平面」用）；
        /// **每次查都重算位置**：换战场 / 缩放变了要跟着走，而且 `GenericTarget` 会被下游改掉
        /// （`WFModuleCollisions` 会写它的 `position`/`up`）⇒ 下一次查必须复位。</summary>
        Transform ParticleCollider(int id)
        {
            // 🔴 **2026-10-11（A218）判「不改」**（这一处**故意**保持**裸 `Transform`**，⛔ 别顺手补 `RectTransform`）：
            //    判据 = **原版这一族本来就是裸 `Transform`** —— `bundle_scenes_scenes_battlearena1` 实读：
            //    根 `Particle colliders`（go_pid 575）= **`Transform`**、它下面那 7 个 `Generic Target`
            //    （`Generic Target` go_pid 138）= **`Transform`**（不是 `RectTransform`）。
            //    它们**只当粒子碰撞的「平面」**（没有渲染器、靠 `position`/`up` 定位），**没有任何矩形语义**
            //    ⇒ 写 `sizeDelta` 只会造一个**没有判据的数**（铁律 3）。
            if (_pColliders == null)
            {
                var root = new GameObject("Particle colliders");
                root.transform.SetParent(transform, false);
                _pColliders = new Transform[PColliderIds.Length];
                for (int i = 0; i < _pColliders.Length; i++)
                {
                    var go = new GameObject(PColliderNames[i]);
                    go.transform.SetParent(root.transform, false);
                    _pColliders[i] = go.transform;
                }
            }
            PositionParticleColliders();
            for (int i = 0; i < PColliderIds.Length; i++)
                if (PColliderIds[i] == id) return _pColliders[i];
            Debug.LogWarning($"[Battle] 粒子碰撞平面收到不认识的 id={id}（原版 7 个：0/5/7/8/10/11/15）");
            return null;
        }

        /// <summary>**两条兵线中心**（原版 `BattleParticleColliderManager` 的 `playerMinionCollider` /
        /// `enemyMinionCollider` 那两个 `Transform` 的 `position`）。
        ///
        /// 🔴 **判据只此一处** —— 两个消费方都走它：`PositionParticleColliders`（粒子碰撞那 7 个平面）
        ///   与 `WFModuleScaleByTarget.MinionLines`（锥角修正那一跳）。别在调用点各写一份
        ///   （CLAUDE.md 三：两处写同一条规则 = 迟早不一致）。
        /// 🔑 **不另摆空物体**：兵线的定义本来就在 `BoardLayout` / `ArenaSlots` 上。
        /// 返回 false = 两个世界系都拿不到（两个调用方各自按原版「缺引用」那一支处理，**不静默**）。
        ///
        /// 🔴 **2026-10-12（A368）：3D 那一支原来只有 `PositionParticleColliders` 有**，
        ///   而锥角那一路（`WFModuleScaleByTarget.MinionLines`）**只会取 2D 的点** ⇒ **量纲错**：
        ///   `WFModuleScaleByTarget.ChangeShapeAngle`（`:206-208`）算的是
        ///   `lineD = |pLine − eLine|` **÷** `cardD = |targetCard.position − actingCard.position|` 这个**比值**，
        ///   而真 3D 下两张卡的位置是**战场世界坐标**（`ArenaSlots.RootPosition`，那一套 182.14 px/单位）
        ///   ⇒ 兵线距必须是**同一个世界系**里的距离。
        ///   两套数各自「看着有依据」：2D 支给 `2.241` 我们世界单位（= 原版屏上 242 px），
        ///   3D 支给 `|EnemyZ − PlayerZ| = 7.698` —— **差 3.4 倍，静默算错**。
        ///   **旁证（原版那两个量是 3D 世界坐标）**：`WFModuleScaleByTarget.cs` 文件头列出的三个常量里
        ///   有一个是 abs 掩码 `FF FF FF 7F`（`0x1834B2E60`），**用在「兵线距 = |玩家线 − 敌方线|」上、
        ///   只取 Z 轴** ⇒ 原版那条兵线距就是**战场世界系**的距离。
        /// </summary>
        bool TryMinionLines(out Vector3 pLine, out Vector3 eLine)
        {
            if (boardCam != null)
            {
                // 真 3D：兵线 = 那一排的 z（**不带督军位多出来的那个 z 偏移**）+ x=0
                pLine = new Vector3(0f, 0f, ArenaSlots.Position(BoardLayout.WarlordSlot + 1, false).z);
                eLine = new Vector3(0f, 0f, ArenaSlots.Position(BoardLayout.WarlordSlot + 1, true).z);
                return true;
            }
            if (playerBoard != null && enemyBoard != null)
            {
                // 没有 3D 战场（`CardBaseDemo` 那类）：退回正交平面上的两条兵线 —— 桥只取比值，照样成立。
                //   两行中心线相距 0.2241 归一化 × `LayoutSpace.DesignHeight`(10) = **2.241 我们世界单位**
                //   = 原版屏上那 **242 px**（出处 `资料/AnimFX_实现与接线.md` §11.6 d)）。
                //   ⚠️ 这一支**只在「没有 3D 战场」时才成立** —— 那时卡也活在这同一个正交平面里。
                pLine = playerBoard.SlotPosition(BoardLayout.WarlordSlot);
                eLine = enemyBoard.SlotPosition(BoardLayout.WarlordSlot);
                return true;
            }
            pLine = eLine = Vector3.zero;
            return false;
        }

        /// <summary>自检用：把**当前挂着的** `WFModuleScaleByTarget.MinionLines` 问一遍
        /// （走的**就是模块实际用的那一个委托**，不是在这儿重写一遍判据）。
        /// 断言「3D 板下兵线取的是战场世界系的点」就盯它：
        /// 3D 下 `|pLine − eLine|` 必须 = `|EnemyZ − PlayerZ|`（= 7.698），
        /// 2D 板（没有透视相机）下是正交平面那两条线（= 2.241）—— A368。</summary>
        public bool MinionLinesForTest(out Vector3 pLine, out Vector3 eLine)
        {
            var f = WarpforgeVFX.WFModuleScaleByTarget.MinionLines;
            if (f == null) { pLine = eLine = Vector3.zero; return false; }
            return f(out pLine, out eLine);
        }

        void PositionParticleColliders()
        {
            Vector3 pLine, eLine;
            if (!TryMinionLines(out pLine, out eLine)) return;   // 两个世界系都拿不到 ⇒ 照旧不摆（原版那一支）

            Vector3 fwd = eLine - pLine;
            float dist = fwd.magnitude;
            if (dist < 1e-4f) return;
            fwd /= dist;

            for (int i = 0; i < _pColliders.Length; i++)
            {
                var t = _pColliders[i];
                if (t == null) continue;
                t.position = pLine + fwd * (PColliderZ[i] / 1.621f * dist);
                Vector3 up = PColliderUp[i] > 0 ? fwd : (PColliderUp[i] < 0 ? -fwd : Vector3.up);
                t.rotation = Quaternion.FromToRotation(Vector3.up, up);
                t.localScale = Vector3.one * PColliderScale[i];
            }
        }

        /// <summary>阵营卡背 → `Sprite`。
        ///
        /// 为什么要有它：`CardArt.CardBack` 给的是 `Texture2D`，而 `tsa.AddSprite` 要的是
        /// **`Sprite`**（原版那边 `GetCardBackSprites().Item1` 本来就是 sprite）。
        /// **按阵营缓存一份** —— 每个模块实例各 `Sprite.Create` 一张会白占内存。
        /// ⚠️ 阵营名拿不到（或那张 `back_*.png` 不存在）时返回 **null** ⇒ 模块按原版
        ///   「拿不到卡背」处理（LogError + 计数，**不静默**）。</summary>
        readonly Dictionary<string, Sprite> _cardBackSprites = new Dictionary<string, Sprite>();

        Sprite CardBackSprite(string faction)
        {
            if (string.IsNullOrEmpty(faction)) return null;
            Sprite sp;
            if (_cardBackSprites.TryGetValue(faction, out sp)) return sp;
            var tex = CardArt.CardBack(faction);
            if (tex == null) { _cardBackSprites[faction] = null; return null; }
            sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            sp.name = "CardBack_" + faction;
            _cardBackSprites[faction] = sp;
            return sp;
        }

        /// <summary>最近一条正在播的事件（`BuildCardContext` 用它算 `targetIsWarlord`）。</summary>
        RuleEngine.BattleEvent _animfxLastEvent;

        WarpforgeVFX.WFEffectCardContext _animfxCtx;

        /// <summary>把一条事件的「谁打谁」翻成模块要的上下文。</summary>
        WarpforgeVFX.WFEffectCardContext BuildCardContext(BattleEvent e)
        {
            var ctx = new WarpforgeVFX.WFEffectCardContext
            {
                valid = true,
                actingIsPlayer = e.Player == _me,
                targetIsPlayer = e.TargetPlayer == _me,
                actingCard = AnimFxCardViewAt(e.Player, e.Slot),
            };
            // 攻击：被打的那张才是 target；其余事件原版两者同源（就是那张卡自己）
            ctx.targetCard = e.Kind == EvtKind.Attack
                ? AnimFxCardViewAt(e.TargetPlayer, e.TargetSlot)
                : ctx.actingCard;
            return ctx;
        }

        Transform AnimFxCardViewAt(int player, int slot)
        {
            if (slot < 0) return null;
            var d = player == _me ? _myUnits : _foeUnits;
            CardView v;
            return d.TryGetValue(slot, out v) && v != null ? v.transform : null;
        }

        /// <summary>接上 AnimFX 屏震模块的下游。
        ///
        /// 为什么要有这一层：`WarpforgeVFX` 那层**不认识相机**（它只管特效），所以模块只
        /// 「报一次解析好的预设参数」，震哪儿由表现层决定 —— 见 `WFModuleScreenShake.OnShake`。
        /// 原版是 `AnimFXModuleScreenShake`（434 实例 / **416 效果**）→ `CameraShakerManager.DoShake`
        /// → Cinemachine 震**主相机**；我们单相机 ⇒ 落地成「相机 + `HudRoot` 同向平移」，
        /// 也就是 `CardFeel.ShakeCamera`（做法与推导见它的注释）。
        ///
        /// ⚠️ **幅度换算是我们推的**：原版看的是 `amplitude × |direction|`（`CardFeel.ShakeWorldAmplitude`
        ///    就是按 `Shake Hit Small` 的 2.0 × |(0,0.2,0.2)| = 0.5657 推出来的），
        ///    所以这里**用同一个基准归一**，别的 preset 按比例放大缩小。
        /// ⚠️ **原版的 `rawSignal`（Beautify 波形）没复刻** —— 见 `shake_presets.json` 的条目标注。</summary>
        void HookAnimFxShake()
        {
            WarpforgeVFX.WFModuleScreenShake.OnShake = req =>
            {
                if (cam == null || hudRoot == null) return;
                float mag = req.amplitude * req.direction.magnitude;          // 原版口径
                float worldAmp = CardFeel.ShakeWorldAmplitude * (mag / 0.5657f);  // 归一到 Shake Hit Small
                CardFeel.ShakeCamera(cam, hudRoot, worldAmp, req.delay, boardCam);
            };
        }

        /// <summary>挂「取督军 / 认座位」两个**补间解析口**（`UnitTweenRuntime.HeroBySeat` / `SeatOf`）。
        ///
        /// 🔴 **2026-10-13（A515）：这两句原来挂在 `BuildHud()` 里** —— 而 `BuildHud` 被 `_hudBuilt`
        ///    闩住（HUD 结构只能建一次）⇒ **同一个 driver 第二次 `Begin()` 时那两句不跑**，
        ///    可 `DetachStaticHooks()`（`OnDestroy` 第一句）**已经**把这两格置 null 了
        ///    （它们在 `ForEachStaticHook` 那 19 槽清单里）⇒ 二次 `Begin` 只接回 **17/19** 条，
        ///    **静默少两条**（`UnitTweenRuntime.ResolveHero` 会在 `HeroBySeat == null` 时直接返回 null，
        ///    下游只表现为「补间定位不到督军」）。
        ///    挪到这里，与 <see cref="HookAnimFxShake"/> / <see cref="HookAnimFxCards"/> **同一条纪律**：
        ///    钩子绑的是**这个 driver 实例**，所以**每次 `Begin` 都要重挂**（赋值本身幂等，直接覆盖静态字段）。
        ///
        /// ⚠️ `UnitTweenRuntime.Install()` **仍留在 `BuildHud` 里**：它自己那两条（`WFModuleTween.OnInvoke`
        ///    / `UnitTweenTable.HeroOf`）绑的是 `UnitTweenRuntime` 的**静态方法**、**不指着 driver**，
        ///    所以 `Installed` 那个闩**不该动**（理由见 `ForEachStaticHook` 里那段注）；它只在**第一次**
        ///    `Begin` 真装一次 —— 那是对的，装了就一直有效。这两个解析口与 `Install()` 谁先谁后都无所谓
        ///    （`ResolveHero` 是**调用时**才读它们）。</summary>
        void HookUnitTweenResolvers()
        {
            // 只有驱动知道视图：
            //   · `HeroBySeat` = 该座位督军那一格（`BoardSpec.WarlordSlot`）的视图
            //   · `SeatOf`     = 这个 transform 属于哪一方（扫一遍自己的视图表；表很小）
            UnitTweenRuntime.HeroBySeat = seat =>
            {
                var v = ViewAt(seat, RuleEngine.BoardSpec.WarlordSlot);
                return v != null ? v.transform : null;
            };
            UnitTweenRuntime.SeatOf = tr =>
            {
                if (tr == null) return -1;
                for (int p = 0; p < 2; p++)
                    for (int s = 0; s < RuleEngine.BoardSpec.Size; s++)
                    {
                        var v = ViewAt(p, s);
                        if (v != null && (v.transform == tr || tr.IsChildOf(v.transform))) return p;
                    }
                return -1;
            };
        }

        /// <summary>
        /// **按 Play 时的开局**：读卡组编辑器里当前选中的那套 → 开一局。
        ///
        /// 抽成独立方法是为了**自检能走同一条路** —— 批处理下 `AddComponent` 不触发 `Start`，
        /// 不这样的话「卡组库 → 对局」这段连接就永远没被验过（而那正是这一段的意义）。
        /// </summary>
        /// <summary>
        /// 联机局的开局：**照主机那份参数开**（`资料/联机P2P_设计与交接.md` §六 N3）。
        /// 🆕 **2026-10-13（A410）**：**放录像也走这个方法**（`PlayReplay` 传 `attachNet: false`）——
        /// 所以「这一趟是哪一档」得按实情判（`attachNet` + `_net`），别在方法体里写死某一种局的字眼。
        /// 🔴 **两端跑的是同一套绝对座位编号**（主机 = 0 / 客机 = 1）：
        ///   · `Begin(myDeck:, foeDeck:)` 那两个参数**指的是座位 0 / 座位 1 的牌**（不是「我 / 对面」）；
        ///   · 本机是几号由 `SetMySeat` 定 —— 视图那一侧靠 `_me` 自己翻（驱动里 100 处 `_me` 全是相对的）；
        ///   · 先手由主机定（`ForceFirstSeat`，绝对座位），不是本地投硬币。
        /// ⚠️ **第一版是「两端都把自己当 0 号位」（镜像）—— 那条路走不通**：自检跑到第 40 步分叉，
        ///    根因是 `Unstable`（随机自爆）的候选单位表按座位顺序拼、镜像后同一个随机下标选中不同的单位。
        ///    教训全文 → `NetProtocol.Fingerprint` 的注释。
        /// </summary>
        /// <param name="deckNote">见 `Begin` 的同名参数 —— 它是**「卡组为什么没读出来」的理由**，
        /// 只在 `ResolveDeck` 没给出人话（`myNotice` 为空 = 这一方的牌**不是**从玩家存档里读出来的）时才会被用上，
        /// 用上时的句式是固定的：「卡组存档读不出来（<paramref name="deckNote"/>）—— 本局自动凑了一副」。
        /// 🔴 **别拿它当「本局是什么局」的标签**（**A387**，2026-10-12）：回放局原来在这里传 `"联机局"`，
        ///   于是提示行成了「卡组存档读不出来（联机局）—— 本局自动凑了一副」——
        ///   **回放不是联机局**：原版 `MatchType.Replay = 160` 是**独立的一档**
        ///   （`ReplayHud.Setup` 按 `matchType == 0xA0` 开关整排回放钮，判据 →
        ///   `资料/普查产出_0927/回放_入口与数据链.md` §B·0）。⇒ 两个入口现在各传各的**实情**：
        ///   联机 = `"联机局·下发里没有卡组"`、回放 = `null`（那一档没有「读不出来」这回事）。
        ///   ⚠️ **这一句人话是我们加的**（原版没有这条提示行，见 `SetHint` 的文件头）——
        ///   原版能给到的判据只到「回放是独立的一档」，**没有**「回放该显示哪句话」的判据。</param>
        /// <param name="botSeatOverride">🆕 **2026-10-19（`A1087`）**：本局「哪一个座位是电脑」——
        /// 放录像时由 `PlayReplay` 从**录像头**（`ReplayRecord.BotSeatForNewBattle`）取来。
        /// `null` = 调用方没说 ⇒ `Begin` 退回老口径 `AiShouldDriveOpponent`（**联机开局 / 重连重建
        /// 都该是它**：那两档里 `_net` 挂着，判据本来就对）。⛔ **不许在这里写死 `1` 或 `-1`**
        /// —— 两处写同一条规则 = 迟早不一致。</param>
        public void BeginFromPendingCore(NetPendingBattle pb, bool attachNet, string deckNote,
                                         int? botSeatOverride = null)
        {
            if (pb == null) { Debug.LogError("[Net] `BeginFromPendingCore(null)` —— 不开局"); return; }
            SetMySeat(pb.MySeat);
            // 🔴 **2026-10-09（`A1127`）订正**：这里原来是 `_shuffleDecks = !pb.NoShuffle;` —— 与
            //   `A1100`（`ForceFirstSeat`）/ `A1110`（`_noAiMulligan`）**同一族的跨局泄漏形状**：
            //   产品往一个字段里写、而那个字段无处清。⇒ 那个字段已删，改走**显式形参**（见下面那句 `Begin(...)`）。
            //   ⚠️ `NetPendingBattle.NoShuffle` 今天仍是**死字段**（只声明、全仓无赋值）⇒ 这里恒传 `true`。
            // 🔴 **2026-10-19（A1100）订正**：这里原来是 `ForceFirstSeat = pb.FirstSeat;` ——
            //   而 `ForceFirstSeat` 是**自检的钉子**（那个字段的注释自己写着「钉住的是自检，不是产品」）
            //   ⇒ 产品往它里面写 = **跨局泄漏**：打完一局**联机** / 放完一局**录像**之后，**下一局单机**
            //   会沿用上一局的**绝对座位**、先手**不掷硬币**（`Restart()` 也一样）。铁律：对局必须可复现。
            //   ⇒ 先手改由**显式形参**交给 `Begin`（见下面那一句调用）。
            // 🔴 **2026-10-19（`A1110`）订正**：这里原来是 `_noAiMulligan = true;` —— 与 `ForceFirstSeat`
            //   那一条**一模一样**的跨局泄漏：**只在这里被写、无处清** ⇒ 打完一局**联机** / 放完一局
            //   **录像**之后再开一局**单机**时，`OpenMulligan` 里那道闸还是「关」的 ⇒ **对面（AI）的换牌
            //   整段不跑** —— 而 `RuleCore.Mulligan` 吃 `ctx.Rng` ⇒ **这一局的随机流与「同一颗种子新开
            //   一局」不同**（铁律：对局必须可复现）。
            //   ⇒ 与 `A1100` 同一个形状：改走**显式形参**（下面那句 `Begin(...)` 的 `noAiMulligan: true`），
            //     而 `Begin` **每局显式复位**这一格。
            //   ⛔ **别把这一句写回来**（写回来 = 每局复位被它覆盖 = 泄漏重开）。
            // 🆕 **2026-10-15（A383）**：模式号**从开局包来** —— 联机是主机下发、回放是录像头里那一格
            //   （原版 `MatchData.playMode` 也是随开局参数一起下来的）。
            //   · `pb.PlayMode` = 模式号（老录像里那两个字 `"Classic"`/`"Skirmish"` 照样读得回来；
            //     认不出的串由 `PlayModeNames.Parse` **出声**并退回 `Classic`）；
            //   · `pb.Vars` = 规则参数，**只看这副牌**（与单机那条路同一判据，§2.7 —— 这样
            //     「12 张的牌从练习窗开出去」那种局面重放时不会按 30 张重建）。
            var vars = pb.Vars;
            // 🔴 **2026-10-13（A410）**：这一句原来**写死**「联机开局」——
            //   可**三个入口走的是同一个方法**：`attachNet: true` = 真联机开局（`BeginFromDeckLibrary()` 里
            //   `NetPendingBattle.Take()` 那一支）、`attachNet: false` + `_net != null` = **重连重建**
            //   （`NetReplay()`，那一支的 `Net` 是**保持挂着**的）、`attachNet: false` + `_net == null`
            //   = **放录像**（`PlayReplay()`）
            //   ⇒ 回放局在日志里也自称「联机开局」。
            //   判据：原版 `MatchType.Replay = 160` 是**独立的一档**（与 A387 同一条 —— `ReplayHud.Setup`
            //   按 `matchType == 0xA0` 开关那一排回放钮）⇒ **回放不是联机局**，而且它连 `[Net]` 都不是。
            //   ✅ **2026-10-15（A383）已接** —— `pb.ModeStr` 那条通道**不再只认两个字符串**：
            //      它现在装的是 `GameMode` 的枚举名（15 档），上面 `pb.PlayMode` / `pb.Vars` 两处读它。
            string kindNet = attachNet ? "[Net] 联机开局"
                           : _net != null ? "[Net] 联机重建（重连）"
                           : "[Replay] 回放开局";
            Debug.Log($"{kindNet}：种子 {pb.Seed} · 模式 {pb.ModeStr}（`PlayModes` = {(int)pb.PlayMode}）"
                    + $" · **本机座位 {pb.MySeat}** · "
                    + $"先手座位 {pb.FirstSeat}（{(pb.FirstSeat == pb.MySeat ? "我" : "对面")}）· 战场 {pb.Arena}");
            Begin(myFaction: pb.Seat0Faction, foeFaction: pb.Seat1Faction, seed: pb.Seed,
                  myDeck: pb.Seat0Deck, foeDeck: pb.Seat1Deck, deckNote: deckNote, vars: vars,
                  playMode: pb.PlayMode, botSeatOverride: botSeatOverride,
                  firstSeatOverride: pb.FirstSeat,      // 🆕 A1100（⛔ 不再写 `ForceFirstSeat`）
                  noAiMulligan: true,                   // 🆕 A1110（⛔ 不再写 `_noAiMulligan`）
                  shuffle: !pb.NoShuffle);              // 🆕 A1127（⛔ 不再写 `_shuffleDecks`）
            // ⚠️ `Begin` 收的 `myFaction/foeFaction` 是**座位 0/1** 的阵营，而 `_myFaction/_foeFaction`
            //    这后面全是**视图侧**用（`owner == _me ? _my : _foe`）⇒ 客机（`_me == 1`）要换回来。
            if (_me == 1) { var t = _myFaction; _myFaction = _foeFaction; _foeFaction = t; }
            if (attachNet)
            {
                var sess = NetRuntime.Instance != null ? NetRuntime.Instance.Session : null;
                var nb = NetBattle.Attach(this, sess, pb);
                if (pb.Raw != null) nb.RememberStart(pb.Raw);
                if (sess != null) sess.EnterBattle(nb.LastSeq);
            }
        }

        /// <summary>
        /// 🆕 2026-09-26：**联机局收摊**（离开战场 / 退出时）。
        /// 两件必须做的事（不做的话**下一局连不上**，而且不报错）：
        /// ① 把大厅消息的处理权**还给 `NetRuntime`**（对局里它是关着的，见 `LobbyHandled`）；
        /// ② 清掉 `NetMatchmaking` 里那一局的卡组/标志（不然下一局会带着上一局那副牌去开局）。
        ///
        /// 🆕 **2026-10-12（A388）**：**第一句永远是「把挂出去的静态钩子摘干净」**
        /// （<see cref="DetachStaticHooks"/>）—— 它必须在下面那条 `if (_net == null) return;` **之前**：
        /// 单机局 `_net` 恒 null ⇒ 放它后面等于**单机局一条都不摘**（而单机才是主路径）。
        /// </summary>
        void OnDestroy()
        {
            // 🔴 **2026-10-12（A388）**：原来这里只有两句（`DetachPostFx()` + `SimpleAI.Executed = null`），
            //   账上点名的那三条（`WFEffectCards.Resolver` / `WFModuleCollisions.ColliderLookup` /
            //   `WFModuleScaleByTarget.MinionLines`）与同一族的其余十几条**一条都没摘**。
            //   ⇒ 现在整串走一处判据（清单在 `ForEachStaticHook`）。
            DetachStaticHooks();
            if (_net == null) return;
            // 🆕 2026-10-17（B13·A881①）：**走的时候给对面捎一句话**。
            //    原来这一条路**不说 `bye`** ⇒ 对面只能靠 **10 秒心跳超时**（`NetSession.SilentTimeoutMs`）
            //    才发现我们没了，而那 10 秒里对面**什么都看不到**。
            //    判据（原版在「销毁 / 退出应用」这条路上也是**主动退房间**，不是等对面超时）：
            //      · `NetworkCustomManager__OnDestroy.c:13-19` —— `PhotonNetwork.connected` 就 `Disconnect()`
            //        （Photon 那一断会让**对面收到 `PlayerDisconnected`** ⇒ 他那边走「等对手重连」那条链）；
            //      · `PlayerDataManager__QuitApplication.c:23-35` —— 退出应用时显式 `NetworkCustomManager.LeaveRoom()`。
            //    ⚠️ `LeaveNetRoom` 自带两道闸（上面那句 `_net == null`；以及 `State == Off` ⇒ 已经离开过）
            //        ⇒ 正常收工（`LeaveBattle` → `LeaveNetRoom`）之后再销毁**不会重发**。
            LeaveNetRoom();
            AttachNet(null);      // 再顺手把会话上那三个回调摘掉（同 `AttachNet` 的注：别让上一局继续说错话）
            if (NetRuntime.Instance != null) NetRuntime.Instance.LobbyHandled = true;
            NetMatchmaking.Reset();
            Debug.Log("[Net] 离开战场：大厅消息处理权已还给 `NetRuntime`，联机匹配状态已清");
        }

        // ============================================================ 静态钩子的「摘」
        // 🔴 **2026-10-12（A388）**：这一场往**静态字段**上挂了一整串下游钩子
        //   （`HookAnimFxShake` / `HookAnimFxCards` / `BuildHud` 的补间解析口 / `AttachPostFx` /
        //    `Begin` 里的 `SimpleAI.Executed`）—— 它们**全绑在【这个 driver 实例】上**
        //   （lambda 捕 `this`、方法组绑 `this`）。`OnDestroy` 不摘的话，场景一卸载 / 对象一销毁，
        //   这些钩子**还指着已销毁的 driver**：下一个场景里只要有模块被调到
        //   （`CardBaseDemo`、VFX 试播、下一局新 driver 还没 `Begin` 的那一帧），拿到的就是上一场的实例 ——
        //   字段还在内存里 ⇒ **不报错、静默用错东西**（在 `OnDestroy` 里补一句就断根）。
        //
        // ⚠️ **「摘的时候会不会摘到别人的」—— 数过了，不会**：全仓 grep，这些静态钩子的**赋值点只有两处**：
        //   ① 本文件（上面列的四个挂点）；② `WarpforgeArena1/Editor/AnimFXCheck.cs`
        //      （编辑器自检，它自己存 `prev` 再还回去，与运行期互不重叠）。
        //   **没有第二个生产宿主**挂它们 ⇒ 这里无条件置 null 是安全的（与既有的 `DetachPostFx` 同一条纪律）。
        // ⚠️ **不许静默**：模块那一侧「钩子没挂」都是**出声**的（各 `WFModule*.cs` 里 `LogWarning` +
        //   计数，例如 `WFModuleCardback` 的「要挂 `CardbackResolver`」），所以摘掉之后真有人要用，
        //   日志里看得见 —— 不是闷掉。

        /// <summary>把这一场挂出去的**静态钩子**逐条走一遍 —— **清单只写这一处**
        /// （<see cref="DetachStaticHooks"/> 与 <see cref="StaticHookCount"/> 都从它来；铁律：别写第二份名单）。
        /// 每一步给两个口：「现在挂着的是哪一条」+「怎么把它置 null」。</summary>
        static void ForEachStaticHook(System.Action<System.Delegate, System.Action> visit)
        {
            // ---- ① `HookAnimFxCards()`（`Begin` 里调）----
            visit(WarpforgeVFX.WFEffectCards.Resolver,
                  () => WarpforgeVFX.WFEffectCards.Resolver = null);
            visit(WarpforgeVFX.WFModuleScaleByTarget.CardResolver,
                  () => WarpforgeVFX.WFModuleScaleByTarget.CardResolver = null);
            visit(WarpforgeVFX.WFModuleTween.CardResolver,
                  () => WarpforgeVFX.WFModuleTween.CardResolver = null);
            visit(WarpforgeVFX.WFModuleCollisions.ContextResolver,
                  () => WarpforgeVFX.WFModuleCollisions.ContextResolver = null);
            // 🔴 **A388 点名的第 2 条**：粒子碰撞平面那 7 个替身物体的查表
            visit(WarpforgeVFX.WFModuleCollisions.ColliderLookup,
                  () => WarpforgeVFX.WFModuleCollisions.ColliderLookup = null);
            // 🔴 **A388 点名的第 3 条**：两条兵线中心（方法组，直接绑 `this`）
            visit(WarpforgeVFX.WFModuleScaleByTarget.MinionLines,
                  () => WarpforgeVFX.WFModuleScaleByTarget.MinionLines = null);
            visit(WarpforgeVFX.WFModuleMoveParticlesToTarget.FromCamera,
                  () => WarpforgeVFX.WFModuleMoveParticlesToTarget.FromCamera = null);
            visit(WarpforgeVFX.WFModuleMoveParticlesToTarget.ToCamera,
                  () => WarpforgeVFX.WFModuleMoveParticlesToTarget.ToCamera = null);
            visit(WarpforgeVFX.WFModuleMoveParticlesToTarget.ResolveTarget,
                  () => WarpforgeVFX.WFModuleMoveParticlesToTarget.ResolveTarget = null);
            visit(WarpforgeVFX.WFModuleCardback.CardbackResolver,
                  () => WarpforgeVFX.WFModuleCardback.CardbackResolver = null);
            visit(WarpforgeVFX.WFModuleInstanceParticleAdjacent.OnExecute,
                  () => WarpforgeVFX.WFModuleInstanceParticleAdjacent.OnExecute = null);
            visit(WarpforgeVFX.WFModuleChangeMaterial.SetCardMaterial,
                  () => WarpforgeVFX.WFModuleChangeMaterial.SetCardMaterial = null);
            visit(WarpforgeVFX.WFModuleChangeMaterial.RestoreOriginalMaterial,
                  () => WarpforgeVFX.WFModuleChangeMaterial.RestoreOriginalMaterial = null);
            visit(WarpforgeVFX.WFModuleChangeMaterial.CardTexture,
                  () => WarpforgeVFX.WFModuleChangeMaterial.CardTexture = null);
            // ---- ② `HookAnimFxShake()` ----
            visit(WarpforgeVFX.WFModuleScreenShake.OnShake,
                  () => WarpforgeVFX.WFModuleScreenShake.OnShake = null);
            // ---- ③ 两个补间解析口（🔴 **2026-10-13（A515）起挂在 `Begin()` 的 `HookUnitTweenResolvers()`**，
            //      原来挂在 `BuildHud()` 里 ⇒ 被 `_hudBuilt` 闩住、二次 `Begin` 接不回来）----
            //    ⚠️ `UnitTweenRuntime.Install()` 自己那两条（`WFModuleTween.OnInvoke` / `UnitTweenTable.HeroOf`）
            //    **不指着 driver**（绑的是 `UnitTweenRuntime` 的静态方法）⇒ 不在清单里；
            //    `Installed` 那个闩是**幂等**的，别去动它（动了就得管「重装」那一路）。
            visit(UnitTweenRuntime.HeroBySeat, () => UnitTweenRuntime.HeroBySeat = null);
            visit(UnitTweenRuntime.SeatOf,     () => UnitTweenRuntime.SeatOf = null);
            // ---- ④ `AttachPostFx()`（后期 / LUT 那一条）----
            visit(WarpforgeVFX.WFModulePostProcess.OnPostFx,
                  () => WarpforgeVFX.WFModulePostProcess.OnPostFx = null);
            // ---- ⑤ `Begin()` 里挂的录像钩子 ----
            visit(SimpleAI.Executed, () => SimpleAI.Executed = null);
            // ---- ⑥ 🆕 `Begin()` 里挂的**教程执行器**那两条（2026-10-18 A938）----
            //   `Executed` = 第三条记账口（脚本动作也要能被回放）；
            //   `ResolveDeploySlot` = 脚本出牌的落点（原版取鼠标位置，我们退化成 AI 那套，见 `TutorialScript`）。
            visit(TutorialScript.Executed, () => TutorialScript.Executed = null);
            visit(TutorialScript.ResolveDeploySlot, () => TutorialScript.ResolveDeploySlot = null);
        }

        /// <summary>
        /// 🔴 **2026-10-12（A388）：把这一场挂出去的静态钩子全部摘掉**（清单 = <see cref="ForEachStaticHook"/>）。
        /// `OnDestroy` 的第一句就是它；自检也直接调它（见 <see cref="StaticHookCount"/>）。
        ///
        /// ⚠️ **会连带影响「本场之外」的用法**：摘完之后，若还有特效在别的场景里播（没有 driver 的宿主），
        ///    那些模块会**出声**说「钩子没挂」（不是静默）—— 这是**对的**：那时确实没有战场可查。
        /// ⚠️ 幂等，可以调两次（第二次返回 0）。
        /// 返回：**这一趟真摘掉了几条**（自检比「摘前 N 条 → 摘掉 N 条 → 现存 0 条」用它）。
        /// </summary>
        public int DetachStaticHooks()
        {
            int n = 0;
            ForEachStaticHook((cur, clear) => { if (cur != null) { clear(); n++; } });
            // 后期那一条**除了**那个静态回调，`_postFx` 手里还攥着两张 RT 与一份 Material 实例 ——
            // 那半走它自己的 `DetachPostFx()`（不带条件调也行：它幂等，那句静态赋值与上面清单里那条**同一条**）
            if (_postFx != null) DetachPostFx();
            return n;
        }

        /// <summary>自检用：上面那张清单里**现在还挂着几条**。两条断言一起才咬得住 A388：
        ///   · `Begin` 之后 ≥ 16（挂上去了；数不是判据，只是挡住「一条都没挂」）；
        ///   · <see cref="DetachStaticHooks"/> 之后（或组件真被卸载之后）**必须 0** ——
        ///     这一条同时是「**加了新钩子却忘了在清单里加一条**」的探针（清单只此一处）。</summary>
        public static int StaticHookCount
        {
            get { int n = 0; ForEachStaticHook((cur, clear) => { if (cur != null) n++; }); return n; }
        }

        /// <summary>🆕 **2026-10-13（A515）** 自检用：上面那张清单一共有**几槽**（= 「该重挂的」全集）。
        /// 它当**分母**用 —— 那条断言是「任何一次 `Begin()` 之后 `StaticHookCount` 必须 == 本值」
        /// （见 `Editor/BattleScene.cs` 的 A515 段）。⚠️ 用分母而**不写死 19**：将来往清单里再加一条钩子，
        /// 那条断言会自动盯着它，不必回来改数字（写死的话「加了新钩子却忘了重挂」反而不会被抓到）。</summary>
        public static int StaticHookSlots
        {
            get { int n = 0; ForEachStaticHook((cur, clear) => n++); return n; }
        }

        /// <summary>本机是几号座位（**绝对编号**）。单机恒 0；联机客机 = 1。</summary>
        public void SetMySeat(int seat)        {
            if (seat != 0 && seat != 1) { Debug.LogError("[Net] `SetMySeat(" + seat + ")` 只认 0/1"); return; }
            _me = seat;
            Debug.Log($"[Net] 本机座位 = {_me}（视图侧跟着它翻：`_me` 那一侧画在下面）");
        }

        public void BeginFromDeckLibrary()
        {
            // 🔴 **单机恒座位 0** —— 上一局可能是联机客机（`_me = 1`），不重置的话视图会一直反着
            //    （自己的牌画在对面、`FirstSeat` 也判反）。
            SetMySeat(0);
            // 🆕 2026-10-17（B29）：**教程局**（`Tutorial Mode Menu` 那颗 `Play Tutorial` 那条链）——
            //   通道里有东西就**整条走它**：关卡数据 / 两副牌 / 执行器全在 `BeginTutorial` 里现取。
            //   ⚠️ 位置在**联网那一支之前**：教程是单机模式，不可能同时是联机局，先判它更直白。
            //   ⚠️ 通道**读一次就清**（同 `NetPendingBattle.Take` / `TakePendingPlayMode` 的先例）。
            int tutIndex = TakePendingTutorialStage();
            if (tutIndex >= 0) { BeginTutorial(tutIndex); return; }
            // 🆕 2026-09-26（N3）：**联机局** —— 主机算好的那份开局参数在等着（`NetBattle` 放进去的）
            //    ⇒ 整条走它：种子/两副牌/谁先手/战场全是主机定的，本地一样都不许自己算。
            //    判据 → `资料/联机P2P_设计与交接.md` §六 N3/N4。
            var pbNet = NetPendingBattle.Take();
            // 🔴 **2026-10-12（A387）**：`deckNote` 现在**每个入口各传各的实情**（原班是写死 `"联机局"`，
            //   回放局借这条道走 ⇒ 提示行也跟着自称联机局）。联机这一档的实情 =
            //   **卡组是随开局包下发的那一份**；真走到「读不出来」那一支（`Seat0Deck == null`，
            //   即 `hostDeckJson` 是空的）说明**下发里根本没有卡组** —— 就这么说，
            //   别只丢一个「联机局」当理由（那句话读起来像「因为这是联机局所以读不出来」）。
            //   ⚠️ 这半句是**我们**的措辞：原版没有这条提示行（见 `SetHint` 文件头）。
            if (pbNet != null) { BeginFromPendingCore(pbNet, attachNet: true, deckNote: "联机局·下发里没有卡组"); return; }

            // 🔴 **本局用哪副牌**：在选卡组窗里挑了**预组**的话走那条通道（**读一次就清**）。
            //    原版对等物 = `MatchData.SetPlayerDeck(DeckAndWarlordData)` —— 它收的就是一个 `CardDeck`，
            //    而预组也是 `CardDeck`，所以原版根本不需要分支；我们只能在这里补一条。
            //    判据与出处 → `资料/预组卡组_原版规格.md` §五之七。
            string note = null;
            PlayerDeck saved;
            var pre = PrebuiltDecks.TakePendingBattleDeck();
            if (pre != null)
            {
                saved = pre;
                Debug.Log("[Battle] 本局用**预组卡组**「" + pre.Name + "」（不走 `DeckLibrary.Current`）");
            }
            else
            {
                saved = PickSavedDeck(out note);
            }
            // 🆕 2026-09-26：**本局模式 = 这副牌自己带的模式**（原版 `CardDeck.gameMode`，挂在卡组上）。
            //   ⚠️ 原来这里是「预组那条路从 `PendingSource.gameMode` 读、手编那条路**恒经典**」——
            //      现在玩家自建的遭遇卡组也带模式了（`PlayerDeck.GameMode` + 落盘），两条路**合成一条判据**：
            //      **只看这副牌**，别在别处再判一次（判据 → `资料/加时与冲突模式_原版规格.md` §2.7）。
            //      `PickSavedDeck` 是从磁盘读的，所以「选了哪套」必须在切场景前落盘（`CollectionData.Select`）。
            var vars = GameplayVariables.For(
                (saved != null && saved.IsSkirmish) ? GameMode.Skirmish : GameMode.Classic);
            // 🆕 **2026-10-15（A383）**：**本局真正的模式号**（原版 `MatchData.playMode`）——
            //   入口窗在开战前放进 `_pendingPlayMode`，这里**读一次就清**。
            //   ⚠️ 它与上面那行**不是同一个判据**（别合成一个）：
            //     · `vars` = 「30 张还是 12 张」那套**数值参数** ⇒ 判据是**这副牌**（§2.7）；
            //     · `playMode` = 「玩家从哪扇窗进来的」⇒ 判据是**入口窗**（原版 `IPlayEvent.EventPlayMode`）。
            //   四扇窗今天各是：练习 `OfflinePractice 6`（VA 0x1808B66B0）· `Practice Deck`
            //   `OwnDeckTraining 12`（`DeckInfoPopup.StartPracticeMatch` 的 `StartMatch(0xc,…)`）·
            //   遭遇战 `Skirmish 13`（`FastModeBaseEvent`）· 排位 `Classic 0`
            //   （`RankedV2Event.get_EventPlayMode` VA 0x1804BD440 = `33 C0 C3` = `xor eax,eax; ret`）。
            //   通道空（自检直接调这一条 / 没入口窗声明）⇒ 退回「这副牌自己带的模式」= 今天的行为。
            GameMode playMode = TakePendingPlayMode() ?? (saved != null && saved.IsSkirmish
                                                          ? GameMode.Skirmish : GameMode.Classic);
            Debug.Log($"[Battle] 本局模式：{vars.deckSize} 张（{(vars.IsSkirmish ? "遭遇 Skirmish" : "经典 Classic")}）"
                    + $"· 卡组 {(saved != null ? "「" + saved.Name + "」" : "（自动凑）")}"
                    + $"· 入口模式号 = {playMode}（{(int)playMode}）");
            // 🆕 2026-09-26：**本局的种子必须每局都不一样** —— 否则「投硬币决定先后手」是假的：
            //   原来这条没传 `seed` ⇒ 用的是 `Begin` 的**默认常量** `20260911` ⇒ **每一局的硬币都落在同一面**
            //   （玩家永远同一边；实测自检里就是「P1 恒先手」）。原版那枚硬币是**每局现抽**的
            //   （`SearchOpponentManager.StartBattle` 抽完写进 `MatchData.playerGoesFirstRandomInt`，
            //    PvP 里再经 Photon 同步 ⇒ 两端同一枚）。
            //   ⚠️ **对局仍可复现**（工程红线）：种子**打进日志**，照它重开就是同一局。
            //   ⚠️ 自检可以钉住它（`ForceSeed`）—— 见那个字段的注释。
            int seed = ForceSeed ?? unchecked((int)(System.DateTime.Now.Ticks & 0x7FFFFFFF));
            Debug.Log($"[Battle] 本局种子 {seed}（记下来就能复现这一局 —— **谁先手由它决定**）");
            // 🆕 2026-10-04（A10 那条「练习对手」链的收口）：**练习赛的对手卡组**走静态待读通道 ——
            //   壳里「选对手卡组」那一步把它放进去，这里开局时**读一次（读完就清**，下一局不会还带着它）。
            //   通道空 ⇒ 给 null ⇒ 对手照旧自动凑（原版 `PracticeModePopup.BattleButtonOnClick` 那条
            //   `enemyDeck = null` 的档，模式号 6 = OfflinePractice）。
            //   判据 = 原版 `MatchMakerManager.StartMatch(…, playerDeck, **enemyDeck**, …)`：
            //   `DeckInfoPopup__StartPracticeMatch.c` 把选牌窗回调回来那一副传在 **第 4 个实参位**。
            //   ⚠️ 我们是**跨场景**开战（壳 → `Battle.unity`），原版是一次调用里直传 ⇒ 只能走静态通道
            //   （同 `PrebuiltDecks._pending` 那条先例）。
            Begin(seed: seed, myDeck: saved, foeDeck: PracticeModePopup.TakePendingOpponentDeck(),
                  deckNote: note, vars: vars, playMode: playMode);
        }

        // ==================================================================
        //  🆕 2026-10-17（B29）：**教程局的开局链**（`Play Tutorial` → 真打）
        // ==================================================================
        //  原版这一步：`TutorialModePopup.BattleButtonOnClick` → `MatchMakerManager.StartMatch(…,
        //    playMode: 4 /* PlayModes.Tutorial */, playerDeck: **选中那关的预组牌**, enemyDeck: null, …)`
        //    （那一串实参 = `TutorialModePopup__BattleButtonOnClick.c`，`(iVar1>>0x1f & 2)+4` 恒 = 4）。
        //  我们这条链**跨场景**（壳 `LoadScene("Battle")` → 战场 `Start` → `BeginFromDeckLibrary`），
        //  两段之间只有静态字段过得去 ⇒ 照 `_pendingPlayMode` / `PrebuiltDecks._pending` 的先例，
        //  加一条**读一次就清**的静态通道。

        /// <summary>教程窗点 `Play Tutorial` 时放进来的是**哪一关**（`tutorialIndex` 0..5；
        /// 原版 = `DemoDeckInfoSO.TutorialIndex`）。`-1` = 没有在等的教程局。
        /// ⚠️ 只装「第几关」这一个 int —— 关卡数据、两副牌、执行器全在 <see cref="BeginTutorial"/> 里现取，
        /// 免得这条通道跨场景带着一堆对象。</summary>
        static int _pendingTutorialStage = -1;
        public static void SetPendingTutorialStage(int stageIndex) { _pendingTutorialStage = stageIndex; }
        /// <summary>读一次就清（`-1` = 这一局不是从教程窗来的）。</summary>
        public static int TakePendingTutorialStage()
        {
            int v = _pendingTutorialStage;
            _pendingTutorialStage = -1;
            return v;
        }

        /// <summary>教程局的**固定种子** —— 教程**不洗牌**（`MatchData.ShouldShuffleDeck(100) == false`），
        /// 所以这里要的不是「每局不一样」而是「每次都一样」（教程本来就该是确定的一局）。
        /// ⚠️ 与普通局那条路（`DateTime.Now.Ticks` 派生）**有意不同**，别把它统一过去。</summary>
        public const int TutorialSeed = 20261017;

        /// <summary>`Resources/tutorial_decks.json` 里一方的牌（`工具/gen_prebuilt_decks.py` 生成，**别手改**）。</summary>
        [System.Serializable]
        public class TutorialDeckSideDto
        {
            public string deckId;
            public string name;
            public string faction;
            /// <summary>我们的卡 id（督军那一张；空串 = 没解出来 —— 那次开不了局，会出声）。</summary>
            public string heroId;
            public string defensiveId;
            /// <summary>我们的卡 id 列表（**原序**；牌库顺序就是它）。</summary>
            public string[] cardIds;
            public string so;
        }
        [System.Serializable]
        public class TutorialDeckStageDto
        {
            public int stage;
            public TutorialDeckSideDto player;
            public TutorialDeckSideDto ai;
        }
        [System.Serializable]
        public class TutorialDecksFileDto
        {
            public int format;
            public int stageCount;
            public TutorialDeckStageDto[] stages;
        }

        /// <summary>
        /// 🆕 2026-10-17（B29）：**按关卡开一局真教程**。
        ///
        /// 这一条把三样东西拼起来（判据逐条见 `资料/普查产出_1017/W_B29_教程执行器.md`）：
        ///   ① **关卡数据** `tutorial_stages.json` → `TutorialData.ByIndex(index)`（先手 / 起始单位 /
        ///      起手卡 / 初始法力 / `playerAlwaysWins`…）；
        ///   ② **两副牌** `tutorial_decks.json` → 我们的 `CardDef`（⛔ **不走 `ResolveDeck` 的构筑校验**，
        ///      关卡牌是 7～30 张的关卡牌）；
        ///   ③ **执行器** <see cref="TutorialScript"/>（一局一个实例）⇒ 经 `Begin(…, tutorial:)` 进
        ///      `RuleCore.NewBattle`，引擎侧的六条开关全在那边落位。
        ///
        /// ⚠️ **取不到就停手并出声，⛔ 不退化成「一场没有教程的普通局」**（那会让玩家拿到一局
        ///    「自称教程、其实只是打 bot」的对局 —— 本项目红线：不许静默失败）。
        ///
        /// 🆕 **2026-10-19（`A1087`）**：`botSeatOverride` 透传给 `Begin`（放录像时按**录像头**那一格，
        ///    见 `PlayReplay`）；不传 = 老口径 `AiShouldDriveOpponent`（教程局只可能是单机 ⇒ 恒 `1`）。</summary>
        public void BeginTutorial(int stageIndex, int? botSeatOverride = null)
        {
            SetMySeat(0);
            var stage = TutorialData.ByIndex(stageIndex);
            if (stage == null)
            {
                Debug.LogError("[Tutorial] 取不到第 " + (stageIndex + 1) + " 关的关卡数据"
                    + "（`Resources/" + TutorialData.StagesResourcePath + ".json` 没装载上，或关号越界）"
                    + " —— **不开局**（⛔ 不拿普通局冒充教程局）");
                return;
            }

            var pool = CardDatabase.Load();
            if (pool == null || pool.Count == 0)
                Debug.LogWarning("[Tutorial] 卡池是空的（`Resources/cards_engine.json` 没加载上）—— "
                               + "起始单位/起手卡会一批都认不出来");

            var decks = LoadTutorialDecks();
            var dd = FindTutorialDeck(decks, stageIndex);
            if (dd == null)
            {
                Debug.LogError("[Tutorial] `" + TutorialDecksAssetPath + ".json` 里没有第 " + (stageIndex + 1)
                             + " 关的牌 —— **不开局**（⛔ 不拿别的关的牌顶上去）");
                return;
            }

            List<CardDef> mine, foe;
            var deckPlayer = BuildTutorialDeck(dd.player, pool, "玩家", out mine);
            var deckFoe = BuildTutorialDeck(dd.ai, pool, "对手", out foe);
            if (mine.Count == 0 || foe.Count == 0)
            {
                // 没有督军就开不了局（`BuildPlayer` 会退化成默认督军 —— 那是「一局看着像教程、其实没有督军」）
                Debug.LogError("[Tutorial] 第 " + (stageIndex + 1) + " 关有一边的牌解不出来"
                             + "（玩家 " + mine.Count + " 张 / 对手 " + foe.Count + " 张）—— **不开局**。"
                             + " 两副牌的 SO = " + (dd.player == null ? "?" : dd.player.so) + " / "
                             + (dd.ai == null ? "?" : dd.ai.so));
                return;
            }

            string myFaction = WarlordFaction(deckPlayer, pool);
            string foeFaction = WarlordFaction(deckFoe, pool);
            var script = new TutorialScript(stage);
            Debug.Log("[Tutorial] 开第 " + (stageIndex + 1) + " 关（`" + stage.so + "`）："
                    + "我方 「" + deckPlayer.Name + "」" + mine.Count + " 张（" + myFaction + "）· "
                    + "对手 「" + deckFoe.Name + "」" + foe.Count + " 张（" + foeFaction + "）· "
                    + (stage.playerStarts ? "玩家先手" : "**AI 先手**")
                    + " · 不洗牌 · 不换牌 · 关卡 " + (stage.turns == null ? 0 : stage.turns.Length) + " 回合"
                    + "（原版 = `MatchMakerManager.StartMatch(…, PlayModes.Tutorial = 4, …)`）");

            Begin(myFaction: myFaction, foeFaction: foeFaction, seed: TutorialSeed,
                  myDeck: deckPlayer, foeDeck: deckFoe, deckNote: null,
                  vars: GameplayVariables.Tutorial, playMode: GameMode.Tutorial,
                  tutorial: script, exactMine: mine, exactFoe: foe, botSeatOverride: botSeatOverride);

            // ============================================================
            //  🆕 2026-10-18（A940）：**教程表现层**在这一刻挂上（原版 = `_TutorialStartSequence` 那一支）
            // ============================================================
            var ov = EnsureTutorialOverlay();
            if (ov != null)
            {
                ov.HideAll();
                // ---- 督军两拍落场（**不是动作档**！原版 `BattleManager.TutorialStartSequence` 协程 d__592：
                //      玩家督军先落 → `WaitForSeconds` → 敌方督军 → `CombatCameraZoom.Initialize`）----
                // ⚠️ 引擎侧两个督军在 `RuleCore.NewBattle` 里**同时**就位（那是引擎的事）；
                //    这里的「两拍」**纯表现**：先藏起敌方督军的视图，第一拍过后再显（见 `TickTutorialView`）。
                // ⚠️ 秒数**是我们挑的**（原版那两条 `WaitForSeconds` 的实参没落进 `.c`）。
                var foeW = BoardViewAt(BoardSpec.WarlordSlot, false);
                if (foeW != null) foeW.gameObject.SetActive(false);
                _tutWarlordBeat = 0f; _tutBeatDone = false;
                ov.SetSkip(true);
                Debug.Log("[Tutorial] 表现层：督军两拍（敌方督军先藏，"
                        + $"{TutWarlordBeatSeconds}s 后显 —— 秒数是我们挑的）· 跳过闸已备好（`SetSkip` 只归零计时起点，"
                        + "那颗钮的真身在设置面板里、显隐跟着面板走 —— ✅ 2026-10-18 第三会话订正：原句写「跳过钮已亮」，过时）"
                        + " · `InitTutorialTip` 那一层**建了但没亮**（🔴 原版谁启用它**查不到**，R2 §6·7 ⇒ 我们不猜）");
            }
        }

        /// <summary>`Assets/RuleEngine/Resources/tutorial_decks.json`（与 `tutorial_stages.json` 同目录）。</summary>
        public const string TutorialDecksAssetPath = "tutorial_decks";

        /// <summary>读 `tutorial_decks.json`。取不到 ⇒ **出声**并返回 null（调用方停手）。</summary>
        static TutorialDecksFileDto LoadTutorialDecks()
        {
            var asset = Resources.Load<TextAsset>(TutorialDecksAssetPath);
            if (asset == null)
            {
                Debug.LogError("[Tutorial] 找不到 `Resources/" + TutorialDecksAssetPath + ".json` —— "
                    + "跑一下 `python d:/4/Unity/工具/gen_prebuilt_decks.py`（那一份里才有教程那 12 副牌）");
                return null;
            }
            var f = JsonUtility.FromJson<TutorialDecksFileDto>(asset.text);
            if (f == null || f.stages == null || f.stages.Length == 0)
            {
                Debug.LogError("[Tutorial] `" + TutorialDecksAssetPath + ".json` 解出来是空的");
                return null;
            }
            return f;
        }

        static TutorialDeckStageDto FindTutorialDeck(TutorialDecksFileDto f, int stageIndex)
        {
            if (f == null || f.stages == null) return null;
            int want = stageIndex + 1;                 // 产物里的 `stage` 是 1..6，我们这边 `tutorialIndex` 是 0..5
            for (int i = 0; i < f.stages.Length; i++)
                if (f.stages[i] != null && f.stages[i].stage == want) return f.stages[i];
            return null;
        }

        /// <summary>一方那副关卡牌 → 我们的 `CardDef` 列表（**督军在前**，其余按 `cardIds` 原序 ⇒
        /// 也就是**牌库顺序**，因为教程**不洗牌**）。
        /// 🔴 **只按稳定 id 精确查**（`CardDatabase.DeckLookup` 先查 id，查不到才退回按名字 + 阵营）——
        ///    ⛔ 不按名字模糊找一张「看着像」的顶上去。
        /// ⚠️ 认不出的条目**逐条出声**并丢掉（`tutorial_decks.json` 的 `missing` 列本来就记着有洞：
        ///    原版 id 表里没有的卡号本地没有任何 id→名 的表能补，那份产物已如实标过）。</summary>
        static PlayerDeck BuildTutorialDeck(TutorialDeckSideDto dto, List<CardDef> pool, string who,
                                            out List<CardDef> cards)
        {
            cards = new List<CardDef>();
            if (dto == null) return null;
            var lookup = CardDatabase.DeckLookup(pool, dto.faction);
            var deck = new PlayerDeck(dto.name, dto.heroId, dto.defensiveId,
                                      new List<string>(dto.cardIds ?? new string[0]), (int)GameMode.Tutorial);
            var hero = string.IsNullOrEmpty(dto.heroId) ? null : lookup(dto.heroId);
            if (hero == null || hero.Type != "hero")
            {
                Debug.LogWarning("[Tutorial] " + who + "的督军 `" + dto.heroId + "` 在卡池里**找不到**"
                               + "（或不是 `hero`）—— 这一关开不了（⛔ 不用别的卡顶）。");
                cards.Clear();
                return deck;
            }
            cards.Add(hero);
            int missing = 0;
            for (int i = 0; i < deck.CardIds.Count; i++)
            {
                var c = lookup(deck.CardIds[i]);
                if (c == null) { missing++; continue; }
                cards.Add(c);
            }
            if (missing > 0)
                Debug.Log("[Tutorial] " + who + "那副「" + dto.name + "」里有 " + missing + "/"
                        + deck.CardIds.Count + " 张**我们的卡池里没有**（原版 id 表没覆盖到，"
                        + "`tutorial_decks.json` 的 `missing` 列已记）—— 这一局这些牌不存在。");
            return deck;
        }

        /// <summary>
        /// 卡组编辑器里**当前选中的那套**（`DeckLibrary.Current`，落在 `DeckStore` 那个本地文件里）。
        /// 返回 null = 没编过，走自动凑；<paramref name="note"/> 非空 = **存档读不出来**，
        /// 这句人话会被 <see cref="Begin"/> 说在提示行上（`deckNote` 参数）。
        ///
        /// ⚠️ 只读不写；也**不吞错** —— 存档坏了就明说，不能让玩家以为打的是自己编的那副。
        /// </summary>
        public static PlayerDeck PickSavedDeck(out string note)
        {
            var lib = DeckLibrary.Load();
            // 🔴 **2026-10-18（`G9`）：`note` 的出处从 `LastError` 换成【词条】**。
            //   背景：`G8` 把 `LastError` 从「中文整句」改成了**诊断串**（`DeckStore` 不再拼人话），
            //   而这句话会被 `Begin` 拼进**玩家看得见**的提示行
            //   （「卡组存档读不出来（<note>）—— 本局自动凑了一副」）⇒ 拿诊断串当主文案，
            //   那一行会印英文/技术诊断。
            //   ✅ 人话的**唯一出口** = `Loc.T(lib.LastLoadIssueTerm)` ——
            //   词条键从**错误码**来（`DeckLibrary.LastLoadIssueTerm` → `DeckStore.TermKeyOf`），
            //   与「同一语义不许两条路径」一致：⛔ 别去读 `LastError` 那串字。
            //    `null` = **没有话要说**：「还没编过」与「第一次跑（存档文件不在）」都不是失败
            //    （原版卡组存在服务器上，本来就没有这一档）⇒ ⛔ 别为它编一条。
            note = lib.LastLoadIssueTerm == null ? null : Loc.T(lib.LastLoadIssueTerm);
            return lib.Current;
        }

        // ==================================================================
        //  开局
        // ==================================================================

        /// <summary>
        /// 开局。
        /// </summary>
        /// <param name="myFaction">我方阵营。null = 不改（默认 <see cref="DefaultFactionA"/>）。
        /// ⚠️ 给了 <paramref name="myDeck"/> 时**以那个卡组的督军阵营为准** —— 督军决定阵营。</param>
        /// <param name="foeFaction">对手阵营。null = 用默认（<see cref="DefaultFactionB"/>），
        /// 但若那正好和我方撞了，会**换一个**（免得开局先打内战，这条是我们挑的）。</param>
        /// <param name="myDeck">我方卡组（卡组编辑器存的那套）。**null = 按卡池自动凑一副**。</param>
        /// <param name="foeDeck">对手卡组。null = 自动凑。</param>
        /// <param name="deckNote">卡组**读不出来**时的人话**理由**（`PickSavedDeck` 的 note）。
        /// 只在「这一方的牌不是从玩家存档里读出来的」时才会被用上，句式固定：
        /// `卡组存档读不出来（<paramref name="deckNote"/>）—— 本局自动凑了一副`。
        /// null = 没这回事（**别拿它当「本局是什么局」的标签** —— 那条错见 `BeginFromPendingCore`，A387）。
        /// ⚠️ 原样存进 <see cref="RawDeckNoteForTest"/>（自检钉 A387 用）。</param>
        /// <param name="playMode">🆕 2026-10-15（A383）：**本局真正的模式号**（原版 `MatchData.playMode`）。
        /// 由入口窗经 <see cref="SetPendingPlayMode"/> 进来、或由开局包（联机/回放）带进来。
        /// **不传**（`null`）= 没入口窗声明 ⇒ 退回「这副牌自己带的模式」（与 `vars` 同一条判据，§2.7）
        /// —— 老调用点（自检、`Restart` 之外的既有入口）因此**行为一字不改**。
        /// ⛔ 它**不是** `vars` 的别名：`vars` = 数值参数（30/12 张那一套），这是模式号。</param>
        /// <param name="tutorial">🆕 2026-10-17（B29）：**教程关卡的执行器**（`null` = 不是教程局）。
        /// 给了它同时改三件事：① `Ctx.Tutorial` 落位（引擎侧的六条开关全在 `RuleCore.NewBattle` 里读它）；
        /// ② 先手照关卡（`stage.playerStarts`，⛔ 不掷硬币）；③ **两边卡组走 `exactMine`/`exactFoe`**
        /// （教程关卡牌是 7～30 张的关卡牌，**过不了构筑校验** ⇒ 不能走 `ResolveDeck`）。
        /// 出处与逐条判据 → `资料/普查产出_1017/W_B29_教程执行器.md`。</param>
        /// <param name="exactMine">**原样拿这两副牌开这一局**（不校验、不洗、不补防御卡）—— 只有教程那条链会传。
        /// 传了 `exactMine`/`exactFoe` 而 `tutorial` 是 `null` ⇒ 当成没传（出声），免得有人拿它绕开构筑校验。</param>
        public void Begin(string myFaction = null, string foeFaction = null, int seed = 20260911,
                          PlayerDeck myDeck = null, PlayerDeck foeDeck = null, string deckNote = null,
                          GameplayVariables vars = null, GameMode? playMode = null,
                          TutorialScript tutorial = null,
                          List<CardDef> exactMine = null, List<CardDef> exactFoe = null,
                          // 🆕 2026-10-19（A1087）：**本局「哪一个座位是电脑」由调用方指定**（放录像时 =
                          //   录像头那一格，见 `PlayReplay()`）。不传（`null`）= 退回老口径
                          //   `AiShouldDriveOpponent`，与加这个参数之前**逐字等价**。
                          int? botSeatOverride = null,
                          // 🆕 2026-10-19（A1100）：**本局先手也由调用方指定**（联机开局 / 重连重建 /
                          //   放录像时 = 开局包那一格 `pb.FirstSeat`，调用点 = `BeginFromPendingCore`）。
                          //   不传（`null`）= `ForceFirstSeat ?? FirstSeatForSeed(seed)` = 老口径，**逐字等价**。
                          //   🔴 **为什么要开这个形参**：`ForceFirstSeat` 是**自检的钉子**（见那个字段的注释），
                          //     而 `BeginFromPendingCore` 原来**往它里面写**（`= pb.FirstSeat`）⇒ 打一局联机 /
                          //     放一局录像之后，**下一局单机**沿用上一局的绝对座位、**先手不掷硬币**
                          //     （跨局泄漏；对局可复现受损）⇒ 产品改走**显式形参**，那个字段退回「只许自检写」。
                          int? firstSeatOverride = null,
                          // 🆕 2026-10-19（`A1110`）：**本局「对面换牌不跑 AI」也由调用方指定**
                          //   （联机开局 / 重连重建 / 放录像三档 = `true`，调用点 = `BeginFromPendingCore`）。
                          //   不传（`false`）= 单机老口径（AI 自己换牌），与加这个参数之前**逐字等价**。
                          //   🔴 **为什么要开这个形参**：那个字段原来在 `BeginFromPendingCore` 里**被写 `true`、
                          //     无处清** ⇒ 打完一局联机 / 放完一局录像之后的**下一局单机**沿用 `true`
                          //     ⇒ **对面的 AI 换牌整段不跑**（`OpenMulligan` 那道闸），而 `RuleCore.Mulligan`
                          //     吃 `ctx.Rng` ⇒ 整局随机流跟着变（对局可复现受损）。
                          //   ⇒ 现在**每局由 `Begin` 显式复位**（见下面「本局一次的闩」那一段）。
                          bool noAiMulligan = false,
                          // 🆕 2026-10-09（`A1127`）：**本局「发牌要不要洗」也由调用方指定**
                          //   （联机开局 / 重连重建 / 放录像三档 = `!pb.NoShuffle`，调用点 = `BeginFromPendingCore`）。
                          //   不传（`true`）= 老口径（洗），与加这个参数之前**逐字等价**。
                          //   🔴 **为什么要开这个形参**：`_shuffleDecks` 原来是产品往字段里写的那一格，
                          //     而与 `ForceFirstSeat` / `_noAiMulligan` **同一族跨局泄漏的形状**
                          //     ⇒ 照 `A1100` / `A1110` 的先例改成显式形参（`A1127`）。
                          //   ⚠️ `NetPendingBattle.NoShuffle` 今天仍是**死字段**（只声明、全仓无赋值 ⇒ 恒 `false`）
                          //     ⇒ `shuffle` 恒 `true`、**本改动行为零变化**；将来谁真给它赋值，这条路已经是干净的。
                          bool shuffle = true)
        {
            if (tutorial == null && (exactMine != null || exactFoe != null))
            {
                Debug.LogWarning("[Battle] `exactMine/exactFoe` 只有在教程局（`tutorial != null`）下才认 —— "
                               + "这一局按没传处理（照常走 `ResolveDeck` 的构筑校验）。");
                exactMine = null; exactFoe = null;
            }
            // 🔴 **AnimFX 那几个下游钩子在这里挂**（2026-09-19 从 `Start()` 挪过来）：
            //    原来只在 `Start()` 里挂，而**批处理下 `Start()` 不会被调用**（`BattleScene.Run`
            //    自己 `AddComponent` 之后直接调 `Begin`）⇒ 自检里那几个钩子**恒为 null**，
            //    「接上了没有」这件事**没有任何尺子能量**（2026-09-19 加卡背断言时正好撞上：
            //    断言报 null，而那不是没接、是**这条路径根本没走到**）。
            //    ⇒ 移进 `Begin`：真 Play 模式（Start → BeginFromDeckLibrary → Begin）与
            //      自检（直接 Begin）**走同一条路**（本工程记过的那条规矩：抽成独立方法就是为了这个）。
            //    ⚠️ 赋值是**幂等**的（都是直接覆盖静态字段），Start 里那份已删。
            HookAnimFxShake();
            HookAnimFxCards();
            // 🆕 **2026-10-13（A515）**：这两个补间解析口也**每次 `Begin` 都要重挂** ——
            //   原来挂在 `BuildHud()` 里、被 `_hudBuilt` 闩住 ⇒ 二次 `Begin` 只接回 17/19 条静态钩子。
            HookUnitTweenResolvers();

            // 🆕 **2026-10-18（A985⑦）**：`Battle/Tips/HandFull` 那三张「见过没有」的表**每局清一次**
            //    （不清的话上一局见过的实例会让这一局「首次判据」恒假 —— 静默）。
            ResetHandFullWatch();

            if (myFaction != null) _myFaction = myFaction;
            if (foeFaction != null) _foeFaction = foeFaction;
            _seed = seed;
            // 🆕 2026-09-26：**本局模式**（经典 / 遭遇）。不传 = 沿用上一局的（首局 = 经典）。
            //   ⚠️ 与 `_seed` 同一条纪律：`Restart()` 也要把它带过去，否则「重开一局」会**悄悄退回经典**
            //      （12 张的遭遇牌按 30 张的规则打，牌库当场抽干）。
            // 🆕 2026-09-26：**本局模式**（经典 / 遭遇）。**不传 = 经典**（不是「沿用上一局」）——
            //   ⚠️ 一开始写成「沿用」是**错的**：那样一旦开过一局遭遇，之后所有 `Begin()`（自检里几十处、
            //      `Restart` 之外的所有入口）都会留在遭遇模式里，12 张的规则去跑 30 张的牌。
            //      `Restart()` 会**显式**把 `_vars` 传回来，所以「重开一局保持模式」照样成立。
            _vars = vars ?? GameplayVariables.Classic;
            _myDeckSrc = myDeck;           // 留着给 `Restart()`
            _foeDeckSrc = foeDeck;
            // 🆕 2026-10-17（B29）：教程局的关卡 + 那两副原样牌，同样留着给 `Restart()`。
            //   ⚠️ `_tutorialStage` 是「本局关卡」这件事的**唯一记账口**（`Restart` 靠它判要不要新建执行器）。
            _tutorialStage = tutorial == null ? null : tutorial.Stage;
            _exactMyCards = tutorial == null ? null : exactMine;
            _exactFoeCards = tutorial == null ? null : exactFoe;

            // 🔴 **2026-10-06（A147/A148）**：骷髅那四格是**本局**的账，每局必须清零。
            //   原来 `_foeWarlordMinHp` / `_myWarlordMinHp` **从来不重置**（只在 `UpdateHud` 里取最小值）⇒
            //   按 R `Restart()`（= 再调一次 `Begin`）时它们带着**上一局**的账 —— HUD 那个 `x N` 与结算面板
            //   会显示上一局的骷髅数。与 `_vars` 同一条纪律（那个字段的注释里写着同一个坑）。
            //   `_foeSkullHpSeen = int.MinValue` = **重新播种**：新一局第一次观察到的生命照旧不算「变化」。
            _foeWarlordMinHp = int.MaxValue;
            _myWarlordMinHp = int.MaxValue;
            _foeSkullCount = 0;
            _foeSkullHpSeen = int.MinValue;
            // 🔴 **2026-10-12（A381/A382）**：这一局的**结算账**也在这里清（与上面那四格同一条纪律：
            //   本局的账不跨局）：
            //   · `_settled = false` ⇒ 新一局可以、且只能记一次账（原来那道闩是「结算面板恰好还没弹」）；
            //   · `_replaySession = false` ⇒ **新开一局 = 不再是回放局**（放完录像按 R 重开的那一局，
            //     账要照记）。⚠️ `PlayReplay` 内部就是 `BeginFromPendingCore` → `Begin`，
            //     所以它是**在 `Begin` 之后**再把这一格置真的（顺序不能反）。
            //   ⚠️ `_settleCount` **不在这里清** —— 它是自检用来比「有没有多记一笔」的**累计**计数。
            _settled = false;
            _replaySession = false;
            // 🆕 2026-10-18（A940）：**教程表现层的本局账也在这里清**（与上面同一条纪律：本局的账不跨局）——
            //   表现层的载体**建一次就一直在 `hudRoot` 下**（`BuildHud` 只建一次 HUD）⇒
            //   上一局亮的提示/高亮/光标/标注**必须显式收掉**，否则会在下一局压在战场上（静默）。
            //   ✅ **2026-10-18（第三会话）订正（铁律 5）**：本行原来把「**跳过钮**」也列在这几个里 ——
            //     钮已搬进设置面板（显隐跟着面板走），`HideAll` 现在**够不到它**（`TutorialOverlay` 里
            //     连 `_skipGo` 都没了）⇒ 从这一列里去掉；`HideAll` 里保住的只是 `_skipArmed = false`
            //     那个计时起点（见 `TutorialOverlay.HideAll` 的注释）。
            if (_tutOverlay != null) _tutOverlay.HideAll();
            _tutVisReported = false; _tutBeatDone = false; _tutWarlordBeat = 0f;
            _tutViewTurn = -1; _tutViewCounter = -1; _tutAnchorSlot = -1; _tutHandIdx = -1;
            // 🆕 2026-10-18（`Z6`）：一局一次 —— 「等提示」那条状态别串到下一局
            _tutTipUp = false; _tutTipElapsed = 0f; _tutTipLimit = 0f;
            // 🆕 2026-10-18（A939）：战后脚本也一局一次
            _postActions = null; _postStep = 0; _postTimer = 0f; _postStarted = false;
            // 🆕 2026-10-18（A915）：一局一次 —— 「禁操作」那个自检档别串到下一局
            _inputFrozenForTest = false;
            // 🔴 **2026-10-16（W22）**：出口的闩也在这里清（与 `_settled` 同一条纪律：本局的状态不跨局）——
            //   不清的话按 R 重开的那一局**再也走不出去**（`LeaveBattle` 第一句就 `return`）。
            _leaving = false;
            // 🔴 **2026-10-19（`A1110`）**：**「一局一次」的那几个闩也在每局开头【显式复位】**
            //   （与上面 `_settled` / `_replaySession` / `_leaving` 同一条纪律：**本局的账不跨局**）。
            //   这一族原来**一个清点都没有** ⇒ 同一台 driver（`Restart()` / 任何再 `Begin`）的
            //   **第 2 局起，进攻卡那一段【整段静默跳过】**：
            //   · `_offensivePhaseDone`（`BeginOffensivePhaseIfAny` 的**第一道闸**）—— 不清 ⇒ 第 2 局
            //     **连「选进攻卡 / 选防御卡」那个面板、以及 AI 那一侧的随机选卡都不跑**
            //     （后手那张防御卡也就不会换进手牌）；
            //   · `_offensivePhaseApplied`（`BeginOffensiveRevealOrApply` 的**第二道闸**）—— 不清 ⇒ 第 2 局
            //     **进攻卡那一段（含 `ApplyOffensiveEnvOnce`，即**环境生效**那一步）整段跳过**
            //     ⇒ 战场环境**永远停在第 1 局那一套**（静默、且一路留着）；
            //   · reveal 那三格（`_revealStage` / `_revealCard` / `_revealT`）—— 不清 ⇒ 上一局若还在
            //     **揭示中途**就开新局，第 2 局的 reveal 会被 `_revealStage >= 0` 那道闸挡掉；而且那张
            //     **临时揭示卡会一直留在 `hudRoot` 下**（`Begin` 其余清理够不着它：它不在 `_dying` /
            //     `_returning` / 任何牌区里）。
            //   ⚠️ **判据（为什么这里是「每局清」而不是像 `ForceFirstSeat` 那样「产品一条都不许写」）**：
            //     这几格是**真·每局一次**的状态 —— 每一局都要重新判一次「这一局是谁先手 / 选了哪张进攻卡」，
            //     不像 `ForceFirstSeat` 那样是**自检的钉子**（那条辨析见那个字段的注释）。
            //   ⚠️ **不许静默**：复位只是让那两道闸**重新有机会**跑（它们各自的条件判据一句没改）；
            //     真该跑而没跑时，`BeginOffensivePhaseIfAny` / `ApplyOffensiveEnvOnce` 里那些
            //     `Debug.LogWarning` 照旧出声。
            _offensivePhaseDone = false;
            _offensivePhaseApplied = false;
            _revealStage = -1;
            _revealT = 0f;
            if (_revealCard != null) { Kill(_revealCard.gameObject); _revealCard = null; }
            // 🔴 那颗**进攻卡钮**也拨回「本局还没定」这一档（`BuildHud` 建完就是关着的，
            //   判据 = 原版 `BattleHud.Initialize` 先 `SetActive(false)`）—— 不收回来的话，第 2 局的
            //   **换牌阶段整段**会亮着**上一局**那颗钮（那段时间里还没人定卡）。⛔ **判据仍然只有
            //   `ApplyOffensiveButtonVisibility()` 一处**（见它的注释）—— 这里只是把它拨回出厂态，不另判。
            if (_offensiveBtn != null) _offensiveBtn.gameObject.SetActive(false);
            // 🔴 **2026-10-19（`A1110`）第一半**：**「对面换牌不跑 AI」也每局复位**（形参进、字段归零）。
            //   原来它只在 `BeginFromPendingCore` 里被写成 `true`、**无处清** ⇒ 联机 / 放录像之后再开
            //   单机，AI 的换牌整段不跑（而 `RuleCore.Mulligan` 吃 `ctx.Rng` ⇒ 随机流与同种子新开一局不同）。
            //   ⛔ **别把它挪回 `BeginFromPendingCore` 去写**（那段注释里写着为什么）。
            _noAiMulligan = noAiMulligan;
            // 🔴 顺带把结算面板**显式**收掉：闸门（`ExitReady`）是 `面板显示着 && 视频播完`，
            //   而面板原来只在 `UpdateHud` 的「没打完」那一支里**懒收** —— 新一局的第一帧里
            //   它可能还开着、闸门还开着（那一帧里点一下 = 刚开局就回主菜单）。这条路径今天不可达
            //   （`Restart()` 自己先 `Hide()`、别的入口都是重进场景），但把不变式**就近**写死更省心。
            if (_endPanel != null) _endPanel.Hide();

            // 🆕 **2026-10-14（A660）：每局开始把输入层还回来**。
            //   原版 `_CloseBattleDoors` 在结算门开始播时 `TouchInputManager.Instance.Toggle(false)`
            //   （判据见 `UpdateHud` 结算那一块里的引用），**它自己不还** —— 原版靠「新一局 = 重进
            //   `BattleScene` 场景」自然复位（`TouchInputManager` 是那个场景里的一个组件）。
            //   我们**复用同一个 driver / 同一个场景**（`Restart()` 也只是再调一次 `Begin`）
            //   ⇒ 必须在这里**显式**还回来；不然结算之后镜头再也拖不动（静默、且一路留在那个状态）。
            //   ⚠️ **用 `Current`（只读口）而不是 `Ensure()`**：照原版那句 `if (instance != null)` 的写法 ——
            //     那件组件缺席时原版也**什么都不做**（它从不自己建）；我们跟着不建，
            //     免得在自检里凭空多出一个 `TouchInputManager` 根物件（那会动到「场上有哪些物件」这类前提）。
            //     真对局里它一定在（`CombatCameraZoom.PollPointerSource` 每帧 `Ensure()`）。
            //   ⚠️ 幂等（`Toggle` 就一句 `enabled = option`，见 `Battle/TouchInputManager.cs` 的 286-291）。
            if (TouchInputManager.Current != null) TouchInputManager.Current.Toggle(true);

            // 🆕 2026-09-26：**牌数与模式对不上就出声**（不许静默失败）。
            //   会撞上的场景：一副 12 张的遭遇牌被当成经典开（牌库两回合抽干、看起来像 bug）。
            //   ✅ 2026-09-26 起**两条路都能带模式**了：预组副（`PrebuiltDecks.ToPlayerDeck` 抄了 `gameMode`）
            //      与玩家自建副（`PlayerDeck.GameMode` + 落盘）—— **判据只有一个：这副牌自己**。
            //      所以这条守卫现在是**真的异常**（不是「那条路还没做」）。
            if (myDeck != null && myDeck.CardIds != null && myDeck.CardIds.Count != _vars.deckSize
                // 🆕 2026-10-17（B29）：**教程局不适用**（关卡牌是 7～30 张的关卡牌，原版也不校验张数）
                && tutorial == null)
                Debug.LogWarning($"[Battle] ⚠️ 卡组张数（{myDeck.CardIds.Count}）和本局模式对不上"
                               + $"（{( _vars.IsSkirmish ? "遭遇 12 张" : "经典 30 张")}）"
                               + " —— 牌库会提前抽干。"
                               + $"（这副牌自己的 `GameMode` = {myDeck.GameMode}；"
                               + " 模式不匹配说明卡组存的时候和现在开的模式不是同一个。）");

            // 新一局从「正在播」开始 —— 暂停态**不跨局**带过去（不然重开一局会像卡死）
            SetReplayPaused(false);

            // ⚠️ **重开一局必须清这个** —— 上一局摆过的槽还占着的话，新一局往那些槽拖会被弹回来
            //（`CardInteraction._placed` 是跨局留着的，它只认识槽号，不认识这是第几局）。
            // 踩到它的地方：`BattleScene` 里第二次 `Begin()` 之后第一张牌就落不下去。
            if (interaction != null) interaction.ClearPlaced();

            // ⚠️ **上一局还在消散的卡也要清掉** —— 它们已经被从 `_myUnits/_foeUnits` 里摘出去了
            //    （那是阵亡消散的必要条件，见 `PlayDeathFeel`），所以 `SyncBoard` 管不到它们。
            //    不清的话重开一局时屏幕上会留着上局的半透明残影，而且 `DyingCount` 也一直不是 0。
            for (int i = 0; i < _dying.Count; i++) if (_dying[i] != null) Kill(_dying[i].gameObject);
            _dying.Clear();
            // 🆕 2026-09-29：**正在回手的那几张**同理（`PlayReturnFeel` 也把它们摘出了字典）
            for (int i = 0; i < _returning.Count; i++) if (_returning[i] != null) Kill(_returning[i].gameObject);
            _returning.Clear();
            // 🆕 2026-10-01：**「已经把键让出去了、等演出」的那几张**同理（`DetachStaleView` 摘的）
            foreach (var kv in _myFading) if (kv.Value != null) Kill(kv.Value.gameObject);
            foreach (var kv in _foeFading) if (kv.Value != null) Kill(kv.Value.gameObject);
            _myFading.Clear(); _foeFading.Clear();
            _myUnitViewByUnit.Clear(); _foeUnitViewByUnit.Clear();   // 身份表跨局必须清（上局的单位对象全换了）
            _previewMoved.Clear(); _previewOwner = -1; _previewRequested = -1;

            // ⚠️ **上一局没播完的事件也要清掉**（`_timeline` 是跨局留着的）。
            //    这些事件**只带格位号、不带「这是第几局」** —— 上一局排在未来的那条 `Death P2@0`
            //    到点时会去杀**新一局站在同一格的那张卡**（表现层）。
            //    2026-09-13 实测撞上：自检第 15 节刚摆好的受击者，在命中之前就被上一局的阵亡事件
            //    弄进了消散表（`DyingCount` 对不上、`FoeUnits` 里那张卡凭空消失）。
            //    ⚠️ 附带一个更阴的：队列是**按时间有序**的，一条「未来」的事件会把后面过期的全堵住
            //       （`AdvanceTimeline` 只从队头弹）—— 两个毛病合起来能让整条时间线哑掉。
            _timeline.Clear();

            // 卡池是**原版那 1131 张**（`cards_engine.json`）—— 不再是 `StarterCards` 那 26 张自设计的。
            // `StarterCards` 还留着：`RuleEngineTest` 里那批规则用例还在用它（那些卡是专门为了
            // 覆盖关键词/触发而设计的，原版卡替不了），而且它是「卡池可以换」这件事的活证明。
            var pool = CardDatabase.Load();
            _pool = pool;                     // 战斗日志要把卡名翻成中文名（`Zh`）
            if (pool.Count == 0)
                Debug.LogError("[Battle] 卡池是空的（`Resources/cards_engine.json` 没加载上）—— 这局没法打");

            // ---- 我方阵营：**玩家编的那副牌说了算** ----
            // 规则书:43「1 督军 + 1 防御卡 + 30 张阵营卡」，`DeckRules.Validate` 也是拿**督军的阵营**
            // 去比每一张卡（`SameFaction`）—— 所以「督军的阵营」就是这副牌的阵营，这里不另立判据。
            if (myDeck != null)
            {
                var wf = WarlordFaction(myDeck, pool);
                if (wf != null) _myFaction = wf;
                else Debug.LogWarning($"[Battle] 卡组「{myDeck.Name}」的督军 `{myDeck.WarlordId}`"
                                    + $" 在卡池里找不到 —— 阵营照旧用 {_myFaction}");
            }
            // 对手：默认 Goff；玩家自己选的就是 Goff 时让开，免得开局先打一场内战。
            // ⚠️ **这是我们挑的** —— 原版由匹配系统配对手，单机没有匹配对象。
            //    只在**没显式指定**对手（`foeFaction == null`）时生效，显式传了就以调用方为准。
            if (foeFaction == null && _myFaction == _foeFaction)
                _foeFaction = _foeFaction == DefaultFactionB ? DefaultFactionA : DefaultFactionB;

            string myNotice;
            List<CardDef> myCards, foeCards;
            if (exactMine != null && exactFoe != null)
            {
                // 🆕 2026-10-17（B29）：**教程局：两边都是关卡指定的那副**。
                // 🔴 **必须绕开 `ResolveDeck`** —— 它会先 `DeckRules.Validate`（30/12 张 + 阵营 + 传说上限），
                //    而教程关卡牌是 **7～30 张**的关卡牌（`tutorial_decks.json` 实读：S1 玩家 7 张、AI 0 张…）
                //    ⇒ 校验必挂 ⇒ **静默退回自动凑的那副**，于是「教程用的不是教程那副牌」。
                //    原版也不拿构筑规则去卡它（牌是关卡 SO 直接给的）。
                myCards = new List<CardDef>(exactMine);
                foeCards = new List<CardDef>(exactFoe);
                myNotice = "";
                Debug.Log($"[Battle] 教程局：两边卡组**按关卡原样**上（我 {myCards.Count} 张 / 对手 {foeCards.Count} 张）"
                        + " —— 不校验、不补防御卡、不洗牌");
            }
            else
            {
                myCards = ResolveDeck(PoolFor(pool, _myFaction), _myFaction, myDeck, seed + 1, "我", out myNotice);
                foeCards = ResolveDeck(PoolFor(pool, _foeFaction), _foeFaction, foeDeck, seed + 2, "对手", out _);
            }
            // ⚠️ 2026-09-12：`StarterDeck` 现在**会混进能打的战术卡**（原来那开关没实现，自动凑的牌
            //    一张战术都没有，实战里永远看不到战术）。要退回「只有单位卡」就把 `ResolveDeck`
            //    里那一处传 `unitsOnly: true`。

            // 提示行只说**我方**那副 —— 对手那副是自动凑的，不用跟玩家交代
            _deckNotice = myNotice;
            _deckNote = deckNote;      // 原样留着（自检钉 A387 用；`Restart()` 要照原样带回去）
            if (string.IsNullOrEmpty(_deckNotice) && !string.IsNullOrEmpty(deckNote))
                _deckNotice = $"卡组存档读不出来（{Short(deckNote, 26)}）—— 本局自动凑了一副";

            // `cardPool: pool` —— `create` 造牌要从**整个卡池**按阵营 + 兵种筛候选
            //（`Create three Ultramarines Vehicles` 那 18 张不可能都在牌库里）。
            // 不传的话造牌会如实报「这一局没有卡池」然后什么都不做。
            // 🆕 2026-09-26：把**本局参数**交给引擎（起手张数 / 手牌上限 / 能量增长 / 督军生命增减 /
            //   加时阈值 / 换牌开关全从它读）。`RuleCore.NewBattle` 里还会再压一道
            //   `openMulligan && Vars.showMulligan`（遭遇模式 `No mulligan`）—— 两处都要，
            //   因为**自检不经过这里**（它直接调 `NewBattle`）。
            // 🆕 2026-09-26：**谁先手** —— 原版是客户端开局自算的
            //   （`BattleManager.SetupBoardPhase → SupportMethods.GetPlayerGoesFirstWithInitiative`，
            //    判据 → `资料/加时与冲突模式_原版规格.md` §2.8）。原版那一条的**顺序**是：
            //    ① 模式特例（EventAI 恒我 先手 / 教学·战役看关卡字段）
            //    ② 督军 `initiative`（`RawCardScript+0x12C`，`Undefined=0/Low=10/…/VeryHigh=40`）高者先手
            //    ③ 平局 → 缓存 ④ `MatchData.playerIdGoesFirst`（**建房/发起挑战者先手**）⑤ 都没有 ⇒ **掷硬币**
            //   ✅ **我们只做 ⑤，而且是「决定」不是「缺口」**（🔴 **用户 2026-09-26 拍板**，原话：
            //     「先手后手还是需要投硬币，因为通过投硬币决定先后手**更加公平**，而且**不需要为每一个督军都设置先攻值**」
            //     ·「或许可以为**全部督军设置先攻值为 1**」）⇒ 等价于「**所有督军的 `initiative` 相同**」
            //     ⇒ 原版那条比先攻值的分支在我们这里**永远平局**、自然落到硬币。⛔ 别再去补那个字段。
            //   ⚠️ **必须在 `NewBattle` 之前算**，而且要**用对局种子**（对局可复现是工程红线）。
            //   ⚠️ 换先手会连带改四件事（起始能量 / 防御卡给谁 / 加时判哪一边 / 谁先出牌）——
            //      那四处现在全走 `ctx.FirstSeat` / `ctx.SecondSeat`，**别再写死座位号**。
            //   ⏭ **还没做的**：投硬币的**表现**（动画/UI/音效）—— 我们现在只有结果，屏幕上什么都没有（`项目任务.md` §〇）。
            // 🆕 2026-10-19（A1100）：**三级优先** —— ① 调用方显式指定（联机 / 重连重建 / 放录像）
            //   ＞ ② 自检的钉子 `ForceFirstSeat` ＞ ③ **掷硬币**（`FirstSeatForSeed`，真 Play 那条路）。
            //   ⛔ **产品一条都不许写 ②** —— 那正是本笔要修的跨局泄漏（`BeginFromPendingCore` 原来写它）。
            int firstSeat = firstSeatOverride ?? ForceFirstSeat ?? FirstSeatForSeed(seed);
            // 🆕 2026-10-17（B29）：**教程局的先手由关卡说了算** —— 原版 `BattleManager.GetPlayerGoesFirst`
            //   的 `matchType == 100` 那一支就是取 `PlayerDataManager.currentTutorialStage.playerStarts`
            //   （`GetPlayerGoesFirst.c` 的 `LAB_18096a4b5` → `*(byte*)(stage + 0x28)`）。
            //   ⇒ ⛔ **这里不掷硬币**（掷了会和关卡打架），也⛔ 不理会 `ForceFirstSeat`（自检要覆盖就改关卡数据）。
            if (tutorial != null)
            {
                firstSeat = TutorialRules.PlayerStarts(tutorial.Stage) ? 0 : 1;
                Debug.Log($"[Battle] 教程局：先手 = {(firstSeat == 0 ? "玩家" : "AI")}"
                        + $"（关卡 `playerStarts = {tutorial.Stage.playerStarts}`）—— **不掷硬币**");
            }
            // 🔴 **2026-10-19（A1087）：本局「哪一个座位是电脑」的判据只算这一次** ——
            //   下面两处（**喂引擎** + **记进录像头**）共用这一个局部量。
            //   · `botSeatOverride` 非空 ⇒ **照它**（放录像时它来自录像头那一格，见 `PlayReplay`）；
            //   · 空 ⇒ 老口径 `AiShouldDriveOpponent`（`= _net == null`）= A1070 原样、**逐字等价**。
            //   ⛔ **别在这句里现算 `AiShouldDriveOpponent`**：**放录像时 `_net` 恒 null** ⇒ 一份记着
            //   「对手是真人（-1）」的录像会被当成「座位 1 是电脑（1）」⇒ 后手那张防御卡与随机流
            //   全错位（`RuleCore.GoesSecondCard`）⇒ 回放演成另一局。
            int botSeatThisGame = botSeatOverride ?? (AiShouldDriveOpponent ? 1 : -1);
            // 🆕 A1127：这一格的 `shuffle` 改由 `Begin` 的形参来（原来是字段 `_shuffleDecks` —— 跨局泄漏形状，已删）
            Ctx = RuleCore.NewBattle(myCards, foeCards, seed, shuffle: shuffle, cardPool: pool,
                                     openMulligan: mulliganEnabled, vars: _vars, firstSeat: firstSeat,
                                     tutorial: tutorial, botSeat: botSeatThisGame);   // A1070：单机（无联机层）⇒ 座位 1 是电脑
            // 🔴 **2026-10-15（A383）**：**本局真正的模式号落位**。
            //   ⚠️ 只能落在这里、**不能**塞进 `RuleCore.NewBattle` 的语义里 —— 那一层收的是
            //   「卡组 / 种子 / 参数」，模式号是**入口窗**的事（见 `_pendingPlayMode` 那段）。
            //   没显式给 ⇒ 退回「这副牌自己带的模式」：与 `_vars` **同一条判据**（§2.7），
            //   所以「自检直接调 `Begin`」与「老调用点没传」这两条路**与今天逐字一致**。
            Ctx.PlayMode = playMode ?? (Ctx.Vars.IsSkirmish ? GameMode.Skirmish : GameMode.Classic);
            _playMode = Ctx.PlayMode;
            Debug.Log($"[Battle] 本局模式号 = {Ctx.PlayMode}（{PlayModeNames.Name(Ctx.PlayMode)} · "
                    + $"`PlayModes` = {(int)Ctx.PlayMode}）· `MatchType` = {Ctx.MatchType}"
                    + $"（{(int)Ctx.MatchType}）"
                    + (playMode == null ? " —— ⚠️ **没有入口窗声明**，退回到「这副牌自己带的模式」" : ""));
            Debug.Log($"[Battle] 谁先手：{Ctx.Players[firstSeat].Name}（**投硬币**决定的 —— 用户 2026-09-26 拍板："
                    + "一律投硬币，等价于「所有督军的 `initiative` 相同」；原版那条顺序见 `资料/加时与冲突模式_原版规格.md` §2.8）");

            // 🆕 2026-09-27：**开局就把录像的头记下来**（种子 / 模式 / 双方卡组 / 先手）——
            // 必须在 `NewBattle` 之后（先手是它定的）。⛔ 别挪到 `Begin` 开头：那时 `Ctx.FirstSeat` 还没有。
            RecBegin(myDeck, foeDeck, myFaction, foeFaction, seed,
                     // 🆕 2026-10-15（A383）：录像头存的是**本局真正的模式号**（枚举名）——
                     //   重放时 `FromStart` → `NetPendingBattle.PlayMode` 把它读回来，
                     //   所以「重放的是哪一档」与「当时打的是哪一档」**同一格字段**（原版
                     //   `MatchData.playMode` 也是随局存下去的那个数）。老录像里那两个字照样读得回。
                     PlayModeNames.Name(Ctx.PlayMode),
                     UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                     // 🆕 2026-10-19（A1087）：开局那一格「谁是电脑」也进录像头（与上面 `NewBattle` 共用同一个值）。
                     botSeatThisGame);
            // 🆕 2026-09-27（录像）：**AI 那半挂到引擎边界上**（`SimpleAI.Executed`）——
            //   AI 的动作有两个入口（产品的 `NextAction`+`ExecuteAction`、自检/兼容层的 `PlayTurn`），
            //   挂在调用点会漏掉一半。⚠️ 只记「对面那一侧」的动作（`ctx.Active != _me`），
            //   免得哪个自检替玩家走路时把玩家的动作也记成对面的。
            SimpleAI.Executed = OnAiExecuted;
            // 🆕 2026-10-18（A938）：**教程执行器**那两条（第三条记账口 + 脚本出牌的落点）。
            //   ⚠️ 与 `SimpleAI.Executed` 同一条纪律：挂在这里（不是 `Start()`），
            //      自检直接调 `Begin` 的那条路才走得通；摘在 `DetachStaticHooks`（上面那张清单 ⑥）。
            TutorialScript.Executed = OnTutorialScriptExecuted;
            // 🔴 **脚本出牌的落点** —— 原版取的是**当时的鼠标位置**再换算成最近的合法格
            //   （`UnityEngine.Rendering.MousePositionDebug.GetMouseClickPosition()` →
            //    `MinionManager.GetClosestAvailableSlot(manager, 位置)`，`AiScripted__ExecuteAction.c:856-869`；
            //    ⚠️ 那里读的是 `bm + 0xE8` = **enemyMinionManager**，与「数据里 acting 全是 `EnemyCardInHand`」自洽）。
            //   🆕 **2026-10-18（审查 K5/R9 整改）**：**产品里照原版取指针**（我们本来就有鼠标），
            //     只有**批处理 / 自检**（没有帧循环、指针恒是死点）才退化。两条路**都在下面写明**：
            //       · **原版那条（产品）**：指针 → 命中哪一格（`BoardLayout.TryResolveSlot`，
            //         它是 3D 战场那条判据的唯一入口）→ 再经 `DropLandingSlot` 换成引擎真实落点；
            //       · **退化的那条（批处理 / 自检）**：`SimpleAI.NextDeploySlot`（我们 AI 那套落点）。
            //     ⛔ **退化那条不是原版行为** —— 只是这条链在批处理里没法取指针。
            TutorialScript.ResolveDeploySlot = (c, seat) =>
            {
                if (!Application.isBatchMode)
                {
                    var bl = (seat == _me) ? playerBoard : enemyBoard;
                    int hit;
                    if (bl != null && bl.TryResolveSlot(WorldPointer(), out hit))
                    {
                        int land = interaction != null && interaction.DropLandingSlot != null
                                 ? interaction.DropLandingSlot(hit) : hit;
                        if (land >= 0) return land;
                    }
                    // 指针不在棋盘上（或取不到落点）⇒ 也走下面那条退化，⛔ 不返回 -1 把这一手卡死
                }
                return SimpleAI.NextDeploySlot(c, seat);
            };

            BuildHud();

            // 落点合法性**由这里说了算** —— 表现层只问这一个委托
            interaction.CanDropAtSlot = (slot, card) =>
            {
                if (Ctx == null || Ctx.IsOver) return false;
                if (Ctx.Active != _me) return false;                 // 对手回合不能出牌
                int idx = HandIndexOf(card);
                if (idx < 0) return false;
                // 🔴 🆕 2026-10-18（A938）：**教程白名单闸门 · 第 ① 点**（原版 `BattleManager__CanPlayCard.c:59`
                //   那一次 `CheckIfPlayerActionPermittedInTutorial(5 /*playCardFromHand*/, …)`）。
                //   ⚠️ 这一处是个**查询口**（悬停预览每帧都问），所以**不出声** ——
                //     真被拒的提示由两个地方给：松手时 `CardInteraction` 判 `EngineRefused` → `cantdo`，
                //     以及 `DoPlay` 开头那一次（第 ② 点）。
                if (!TutorialPermits(TutAttemptPlay(idx, slot))) return false;
                return RuleCore.CanPlayCard(Ctx, _me, idx, slot) == RuleCodes.OK;
            };
            // 战术卡能落到**敌方半场**（`Deal 3 damage to an enemy` 打的就是敌方单位）——
            // 不接这个引用的话，敌方目标的战术卡拖过去一律弹回来
            // 🔴 **2026-10-01：落点 → 真正落点**（连续棋盘；`BoardSlots.Resolve` 是**同一份判据**，
            //    引擎 `PlayCard` 里那次 `Insert` 读的也是它 ⇒ 两边永远是同一个答案）。
            //    自检里 Ctx 可能还没建/已结束 ⇒ 退化成恒等（表现层自己的自检照样能跑）。
            interaction.DropLandingSlot = (slot) =>
            {
                if (Ctx == null || Ctx.IsOver || Ctx.Active != _me) return slot;
                int side, index;
                if (!BoardSlots.Resolve(Ctx.Players[_me], slot, out side, out index)) return -1;
                return BoardSlots.SlotOf(side, index);
            };
            // 🆕 2026-10-01：「让位」预览的开关（拖拽中由 `CardInteraction.Update` 每帧推过来）
            interaction.OnDropPreview -= OnDropPreview;
            interaction.OnDropPreview += OnDropPreview;
            interaction.foeBoard = enemyBoard;
            // ⚠️ 先 `-=` 再 `+=`：`Begin()` 会被调多次（重开一局），不清的话每开一局就多挂一份，
            //    落位回调会跑 N 遍（第二遍起 `HandIndexOf` 找不到牌、还会报错刷屏）。
            interaction.OnDeployed -= OnCardDeployed;
            interaction.OnDeployed += OnCardDeployed;
            // 🔴🆕 **2026-10-18（第十五轮 · `G5`）：`OnReturned` 是本批新接的** ——
            //   它 = 「这一拖**没落成**、卡回弹到手牌」（`CardInteraction.Release` 的 `else` 支，
            //   `CardInteraction.Release` 的 `else` 支）。原版在**同一个时刻**会出一句文字提示
            //   （`Battle/Tips/DragToTarget`，判据 → `Loc.cs` 那一块的整段注释）—— 我们过去
            //   **只有 `cantdo` 语音、没有这行字**（= 复刻缺漏）。
            //   ⚠️ 与 `OnIllegalAction` **不是二选一**：松手落在格位上却被引擎拒时**两条都会发**
            //     （原版 `NotifyCantDoAction` 本身就是「语音 + 文字」一次调用，见 `DragToTargetTerm` 的 doc）。
            interaction.OnReturned -= OnCardReturned;
            interaction.OnReturned += OnCardReturned;
            // 轻点卡牌 → 开关展示窗（原版 `BasicCardUI.ToggleOpenCardDisplayOnTouch`）
            interaction.OnTapped -= OnCardTapped;
            interaction.OnTapped += OnCardTapped;
            // 🔴 **玩家做了非法操作 → 督军说一句「我不能这么做」**（原版 `ChatMessage.ICantDoThat` = 枚举 3）。
            //    原版链路：`BattleManager` 的 14 个「操作被拒」点 → `BattleTipController.NotifyCantDoAction`
            //    → `DisplayLocalChatMessage(vlc, 3, skipCanChat=1)`。**我们目前只接了「出牌被拒」这一条**
            //    （对应原版 `CanPlayCard.c:172`），其余 13 条（技能/路标石/选中目标/结束回合…）**还没接** —— 见
            //    `资料/语音线_原版规格与ASR管道.md` §1.3 的 14 点清单。
            //    先 `-=` 再 `+=`：`Begin()` 会被调多次（重开一局），不清会每局多挂一份。
            interaction.OnIllegalAction -= OnIllegalAction;
            interaction.OnIllegalAction += OnIllegalAction;
            // 🔴🆕 2026-10-18（`A915`）：手牌那一层**不走本类**，禁操作得单独传过去。
            //   ⚠️ 委托里读的是本类**同一个** `PlayerInputFrozen`（两处同源，⛔ 别各写一条判断）。
            interaction.Frozen = () => PlayerInputFrozen;

            // 🆕 2026-09-30（§27 架构）：**战场运行时实例化**（施工图 → `资料/§27架构_施工图.md`）。
            //   判据：`ArenaByArmy.SceneFor(督军阵营)`；批处理/自检那一轮由 `WF_ARENA` 覆盖
            //   —— 两处都在 `ArenaRuntimeLoader.ResolveArenaKey`（判据只留一处）。
            //   原来战场是**烘进 `Battle_<场>.unity`** 的（13 份场景）；现在一份 `Battle.unity` + 13 件 prefab。
            //   ⚠️ 放在这里、**不放在 `Begin` 末尾**：下面换牌那条分支会 `return`（那也是一条正常开局路径）。
            {
                var arenaRoot = GameObject.Find("Arena3D");
                var loader = arenaRoot != null
                           ? arenaRoot.GetComponent<CardPresentation.ArenaRuntimeLoader>() : null;
                if (loader == null)
                    Debug.LogWarning("[Battle] 🔴 场景里没有 `Arena3D` + `ArenaRuntimeLoader` ⇒ 这一局**没有 3D 战场**"
                                   + "（现在只有 `Battle.unity` 这一份场景；跑 `-executeMethod BattleScene.BuildAndSaveScene` 重建）");
                else
                {
                    string key = CardPresentation.ArenaRuntimeLoader.ResolveArenaKey(_myFaction);
                    bool ok = loader.Load(key, boardCam);
                    Debug.Log($"[Battle] §27 战场：`{key}`（我方阵营 `{_myFaction}`）"
                            + (ok ? $" · 实例 `{loader.Current.name}`（第 {loader.LoadCount} 次载入）"
                                  : " —— **没载入**（上面应有出声）"));
                    // 🆕 2026-10-01（§三 第 31 条 · 第 3 件）：**战场那个全局 `Volume` 一载进来就接后期下游**
                    //   （`WFModulePostProcess` 52 实例）。⚠️ 只能挂在这儿：`Volume` 在战场 prefab 上，
                    //   `Begin` 开头那时还没实例化。
                    if (ok) AttachPostFx(loader.Current);
                }
            }

            // 🆕 2026-10-12（A175）：**Auto Zoom 的消费者**（原版 `CombatAutoZoom`）—— 判据与落点见 `_autoZoom` 那段注释。
            //   ⚠️ 放在**这里**（战场那一段之后、换牌那条 `return` 之前）：`boardCam` 到这里才定下来，
            //   而换牌分支会提前 return（那也是一条正常开局路径）。
            SetupAutoZoom();

            // 换牌阶段（原版抽完起手牌先换牌，换完才 `StartBattlePhase`）：
            // **先不发能量、不抽第 1 张** —— 那两件事在 `BeginTurn` 里，等玩家点完「完成换牌」再做。
            if (Ctx.MulliganOpen)
            {
                RefreshAll();                  // 手牌要先摆好 —— 换牌按钮是贴着卡摆的，得知道卡在哪
                OpenMulligan();
                UpdateHud();
                SetHint(_deckNotice);
                return;
            }

            RuleCore.BeginTurn(Ctx);       // 先手第 1 回合：能量 2、抽 1
            SyncTutorialTurn();            // 🆕 A938：回合变了 ⇒ 脚本指针归零（原版 `UpdateTurn`）
            ResetClock();                  // 第 1 回合的表也得上（原版 `ClockManager.StartTimer`）
            RefreshAll();
            UpdateHud();
            // 🆕 2026-10-12（A175）：**没有换牌阶段的那条路**（遭遇模式 `No mulligan` 等）也要叫一次 ——
            //   原版 `CombatAutoZoom.Initialize()` 的两个调用点（`_FinishMulliganFinalPhase` /
            //   `_TutorialStartSequence`）都是「真开打那一刻」，这里是它在我们这条路上的等价物
            //   （有换牌那段走 `BeginBattleAfterSetup`，见那边的同一条）。`Initialize()` 自己幂等。
            InitializeAutoZoom();
            SetHint("");                   // 提示行空着时显示开局那句「本局用的是哪副牌」
        }

        /// <summary>
        /// **投降**（原版 `BattleResult.Forfeit`）：立刻判对手胜，不看督军血量。
        /// 规则在 `RuleCore.Forfeit`（判过了不再改）。UI 入口将来挂在设置面板上，
        /// 现在**键盘 `F`** 也能投 —— 自检里走的是这个公开方法。
        /// </summary>
        /// <summary>
        /// 设置面板那一路的输入。返回 true = **这一帧的点击被它接管了**，回合逻辑别再处理。
        ///
        /// ⚠️ 边沿只能在这里**各耗一次**（`ReleasedThisFrame()` / `ClickedThisFrame()` 都是
        ///    「给一次就没了」），在 Update 里先耗掉，回合那段就再也收不到那一下了。所以：
        ///    · 面板**开着**：无条件接管（模态）
        ///    · 面板关着：**只有指针在设置按钮上**才接管，其余一律放行
        /// </summary>
        bool HandleSettings()
        {
            if (_settingsPanel != null && _settingsPanel.Visible)
            {
                // 🆕 2026-09-19 三根音量滑块：**按下即定位、按住拖动**。
                //    顺序要紧：先让滑块有机会**接住**这一帧的指针，接住了就**不能**再把它当点击
                //    转给按钮（否则在滑块上按一下会顺带触发别的命中）。
                bool captured = _settingsPanel.PointerFrame(WorldPointer(), PointerHeld());
                if (captured) _pressCaptured = true;    // 这一次按住归滑块 ⇒ 松手那一帧不算点击
                // 🔴 A462：四颗（Resign / Difficulty / Auto Zoom / Close）**都在抬起那一帧**触发
                //    —— 原版全是 uGUI `Selectable`（`EverguildButton` / `EverguildToggle` /
                //    `UnityEngine.UI.Button`），走 `IPointerClickHandler`。⛔
                //    `PointerFrame`（滑块那条）**不动** —— 原版 `Slider` 就是「按下即跳 + 按住拖」。
                if (ReleasedThisFrame()) SettingsClickAt(WorldPointer());
                return true;
            }
            if (_settingsBtn == null) return false;
            // ⚠️ **本行没有 `activeSelf` 守卫**（同族那颗 `_cameraResetBtn` 有，见 `HandleCameraResetButton`）。
            //    如实标注（🆕 **2026-10-18（A964 前置）** 只读现核查出）：生产里**不可达** ——
            //    本钮出厂就亮着、全仓没有关它的路径 ⇒ **是隐患、不是活缺陷**；但 A964 的探针
            //    （`HudButtonActiveForTest` / `SetHudButtonActiveForTest`）能把它摆成关着 ⇒ 那时本行会**认账**。
            //    ⛔ 别顺手补守卫：那会改行为，而现有断言里没有这一条（要改另开一件、另配断言）。
            // 🆕 **2026-10-19（A964②）**：命中区改成**原版那个** —— `rect` 63.874² 按 `m_RaycastPadding`
            //   (−23.13,−38.6,−26.9,−25.81) **四边外扩** ⇒ **113.90×128.28 px**（判据与算式只此一份 →
            //   `HitPaddedRect` / `SettingsBtnHit`）。⛔ 原来那句 `_settingsBtn.Contains(...)` 量的是
            //   **画出来**的 63.87² ⇒ 原版真正可点的那四边全是「看着在钮外、却点得动」的区域，我们全是死区。
            if (!SettingsBtnHit(WorldPointer())) return false;
            // 🔴 A462：抬起（原版 `BattleHud.settingsButton` = `EverguildButton`，
            //    `BattleHud__Awake.c:48-55` 把它绑在 `m_OnClick` 上）。
            if (ReleasedThisFrame()) _settingsPanel.Show();
            return true;
        }

        /// <summary>
        /// 设置面板开着时的一次点击 —— **真实输入与自检走的是同一条判定**
        /// （自检拿按钮的世界坐标喂进来，不直接调 `Forfeit` / `CycleAiDifficulty`）。
        /// </summary>
        public bool SettingsClickAt(Vector3 w)
        {
            if (_settingsPanel == null || !_settingsPanel.Visible) return false;
            if (_settingsPanel.HitResign(w)) { _settingsPanel.Hide(); Forfeit(); return true; }
            // 🆕 **2026-10-18（A991）**：「跳过教程」那颗钮在这里 —— 原版
            //   `BattleSettingsWindow__SkipTutorialButtonOnClick.c`（方法体亲读）是**两行、这个先后**：
            //   `WindowsManager.CloseWindow(&lt;设置窗&gt;)` **然后** `BattleManager.ClickSkip(bm, 0)`
            //   ⇒ 我们照抄：**先 `Hide()`（= 关设置窗）、再 `SkipTutorialFromSettings()`（= 跳过语义）**。
            //   那颗钮原来建在 `TutorialOverlay` 的 HUD 子树上（落点错）⇒ A991 搬进了本面板。
            //   ⚠️ **非教程局这一下也照样被吃掉**（原版那颗钮常驻、`ClickSkip` 在非教程局只 `LogError`）
            //   —— 语义那一半会出声（`ApplyTutorialSkip` 的 `LogWarning`），⛔ 别在这儿加「教程局才认」的闸。
            if (_settingsPanel.HitSkipTutorial(w)) { _settingsPanel.Hide(); SkipTutorialFromSettings(); return true; }
            if (_settingsPanel.HitDifficulty(w)) { CycleAiDifficulty(); return true; }
            // 🆕 2026-10-12（A445）：**「Auto Zoom」那一行回到「抬起」这条链上**。
            //   原版那颗开关 = `BattleSettingsPanel/Auto Zoom Toggle`（组件 `EverguildToggle`，继承
            //   `Toggle`/`Selectable`）⇒ 走 `IPointerClickHandler`，**抬起**那一帧才触发 ——
            //   与本方法其余三颗（Resign / Difficulty / Close）**同一条链**。
            //   ⚠️ A424 当初把它落在 `SettingsPanel.PointerFrame`（**按下**那一帧）是文件所有权逼出来的权宜
            //   （那时本文件不在那件活的白名单里）；H8 报告 §四·2 给了确切的那一行，本件照它挪回来。
            //   锚定判据 = `BattleSettingsWindow__OnAutoZoomChanged.c`（写 `useCombatAutoZoom` 后
            //   `FindObjectOfType<CombatAutoZoom>().ResetCameraZoomUIAction()`），那条链在
            //   `SettingsPanel.ToggleAutoZoomFromPanel` 里，判据只此一处。
            if (_settingsPanel.HitAutoZoom(w)) { _settingsPanel.ToggleAutoZoomFromPanel(); return true; }
            if (_settingsPanel.HitClose(w)) { _settingsPanel.Hide(); return true; }
            return true;      // 点面板别处：吃掉（不穿透到棋盘），但不做事
        }

        /// <summary>🆕 2026-09-29（§25）：HUD 那颗**进攻卡（环境）**钮 —— 点它**只弹展示窗**。
        /// 原版两个入口（`BattleHud.OffensiveButtonClicked` / `DisplayOffensiveCards`）转的是**同一个**
        /// `BattleManager.DisplayOffensiveCard()` → `CardDisplayWindow.ShowCard(..., showOptions:false, ...)`：
        /// 里面只有卡面/相关卡/文本，**不改任何战场状态、也不发网络包**。
        /// 🔴 **别把它做成「点了就换环境」** —— 那会变成我们的设计（判据 → `项目任务.md` §三 第 25 条）。
        /// ✅ **2026-10-01：展示窗接上了**（原来只出声）—— 卡面用 `OffensiveCardData` 现拼，
        ///    ⚠️ 它是「**有画、有名**的空壳」：费用/攻血/效果文字原版那份在远端 CCD
        ///    （`cardName` 本地 0/39 解不出）；`def` 传 null ⇒ 相关卡那 9 格算不出来（只画主卡，既定行为）。</summary>
        bool HandleOffensiveButton()
        {
            if (_offensiveBtn == null || !_offensiveBtn.gameObject.activeSelf) return false;
            if (!_offensiveBtn.Contains(WorldPointer())) return false;
            // 🔴 A462：**抬起**（原版 `BattleHud.offensiveCardButton` = `EverguildButton`，
            //    `BattleHud__Initialize.c:50-57` 把它绑在 `m_OnClick` 上 ⇒ `IPointerClickHandler`）。
            if (!ReleasedThisFrame()) return false;
            return OpenOffensiveCardWindow();
        }

        /// <summary>那颗钮按下去**做的那件事**（单独拿出来 —— 自检直接调它，不用模拟指针）。
        /// 原版那条链是**纯展示**：`DisplayOffensiveCard()` → `ShowCard(showOptions:false)`，
        /// **不改战场状态、不发网络包** ⇒ 这里只开窗 + 打一行日志。</summary>
        public bool OpenOffensiveCardWindow()
        {
            var c = ChosenOffensiveCard();
            if (c == null)
            {
                Debug.LogWarning("[Battle] 进攻卡钮亮着，但**查不到选的是哪一张**"
                               + $"（`Ctx.OffensiveSlotIdx={Ctx.OffensiveSlotIdx}`）⇒ 展示窗不开（不许静默）");
                return false;
            }
            if (_cardDisplay == null) return false;
            var d = OffensiveCardData(c, OffensiveCardFaction(c));   // ⚠️ 卡面插画要按**那张卡自己的阵营**取
            _cardDisplay.Show(d);                 // 原版是 Show（不是 Toggle）：再点一次就是再开一次
            AfterCardWinToggle(null);
            Debug.Log($"[Battle] 进攻卡展示窗：`{d.title}`（原版 `DisplayOffensiveCard` → "
                    + "`ShowCard(showOptions:false)`；⚠️ 费用/攻血/效果文字在远端 CCD ⇒ 卡面是空壳）");
            return true;
        }

        /// <summary>这一局**本机**选定的那张进攻卡。
        /// 🔴 判据是**我们记下来的那张卡的身份**：`Ctx.OffensiveSlotIdx`（槽号）+ `Ctx.OffensiveEnvSO`
        ///   （原版那边是 `matchData.offensiveCardId`，我们存的就是这两样）。
        /// ⚠️ **本阵营优先、全域兜底** —— 自检里那台 driver 的阵营（`Ember`）在进攻卡表里**根本没有数据**
        ///   （测试是拿 Ultramarines 的表驱动的），只按本阵营查会查不到（2026-10-01 实测踩到）。
        /// ⚠️ 槽号 &lt; 0 = 选了「不使用进攻卡」那一张 ⇒ 那颗钮本来就不出现（原版 `isEmptyOffensiveCard` 判据）。</summary>
        OffensiveCards.Card ChosenOffensiveCard()
        {
            int idx = Ctx.OffensiveSlotIdx;
            string so = Ctx.OffensiveEnvSO;
            var mine = OffensiveCards.Choices(_myFaction);
            OffensiveCards.Card byIdx = null;
            for (int i = 0; i < mine.Count; i++)
            {
                if (mine[i] == null || mine[i].idx != idx) continue;
                if (string.IsNullOrEmpty(so) || mine[i].envSO == so) return mine[i];   // 槽号 + 环境都对上 = 就是它
                if (byIdx == null) byIdx = mine[i];
            }
            if (byIdx != null) return byIdx;

            var all = OffensiveCards.All;
            if (all != null)
                foreach (var a in all)
                {
                    if (a == null || a.cards == null) continue;
                    foreach (var c in a.cards)
                        if (c != null && c.idx == idx && (string.IsNullOrEmpty(so) || c.envSO == so)) return c;
                }
            return null;
        }

        /// <summary>这张进攻卡属于**哪个阵营**（面板/展示窗要用它对卡面插画：
        /// `CardArt.OffensiveFace(阵营, 槽号)`）。按**对象身份**在 `OffensiveCards.All` 里找；
        /// 那张「不使用进攻卡」的空卡每次都是新对象 ⇒ 找不到时退回 `_myFaction`
        /// （⚠️ 空卡那一路那颗钮本来就不出现 ⇒ 走不到这里）。</summary>
        string OffensiveCardFaction(OffensiveCards.Card c)
        {
            var all = OffensiveCards.All;
            if (c != null && all != null)
                foreach (var a in all)
                {
                    if (a == null || a.cards == null || string.IsNullOrEmpty(a.army)) continue;
                    foreach (var x in a.cards)
                        if (x != null && ReferenceEquals(x, c)) return a.army;
                }
            return _myFaction;
        }

        // ==================================================================
        //  🆕 2026-10-12（A423）：HUD 那颗「重置自动镜头」钮
        // ==================================================================

        /// <summary>原版那颗钮的 **rect**（= RT `3487` 的 `sizeDelta`，px）与它的**命中矩形**。
        /// <para>命中矩形 = rect 按 `Image.m_RaycastPadding`（`MonoBehaviour_4796.json`）四边各 **外扩**
        /// —— 原版那一格是 **`(−8,−8,−8,−8)`**（UGUI 分量序 **L,B,R,T**）。
        /// 🔴 **2026-10-18（A964 前置）就地订正（铁律 5）**：本行原文写「**负 = 往里缩**」，**符号是错的**；
        /// **UGUI 里负 padding = 把命中矩形【往外扩】**。两个**独立**的本地判据（都能在本机读原文）：
        ///   ① 官方测试 `Library/PackageCache/com.unity.ugui@27635d171b1a/Tests/Runtime/UGUI/EventSystem/`
        ///      `GraphicRaycasterTests.cs:82-101`：`raycastPadding = (−50,−50,−50,−50)`，指针摆在 **rect 外 60 px**
        ///      ⇒ 断言 **命中了**（若负值是往里缩，那一击必然落空）；
        ///   ② 官方 editor 侧 `…/Editor/UGUI/UI/GraphicEditor.cs` 的 `DrawRect`：
        ///      `p0 = rect.x + offset.x` · `p2.x = rect.xMax − offset.z` ⇒ 负 offset **各自向外**；
        ///      同一函数也服务 `RectMask2D.cs:178-183` ⇒ `m_Padding` **同符号**。
        ///   写法参照（都是外扩口径，与本次订正一致）：`Shell/MenuDraw.PaddedRect`（`:474`「正值缩小、负值扩大」）·
        ///   `Shell/BoosterInfoPopup.cs`（`(−15)` ⇒ 外扩）· `Editor/CollectionScene.cs` 那处 `(−20)×4`（56.86 → 96.86）。
        /// ⚠️ **错因**：把「负 = 缩」当成了常识，没去读 uGUI 那两条本地判据就写进注释、再据此定值。</para>
        /// <para>⇒ **64.443×61.846 → 80.443×77.846**（原来那个 `48.443×45.846` 是**错的**：
        /// **每边少 8 px、面积少约 42%** ⇒ 症状正是「**看着在钮上、点不动**」，与 `A964` 要抓的是同一族）。</para></summary>
        const float CameraResetRectPxW = 64.4429931640625f, CameraResetRectPxH = 61.84600830078125f;
        /// <summary>原版 `m_RaycastPadding` 每**一侧**的量（px）。原版那格是 `(−8,−8,−8,−8)`：
        /// **L=B=R=T=8**（⛔ 不是「每轴 16」—— 8 是单侧量，两轴同值所以合成一条）。</summary>
        const float CameraResetPadPx = 8f;
        // 命中矩形 = 原版的「rect 四边各向外 + 8」（⛔ 别写死 80.443 / 77.846，要现算）。
        const float CameraResetHitPxW = CameraResetRectPxW + CameraResetPadPx * 2f;   // = 80.442993…
        const float CameraResetHitPxH = CameraResetRectPxH + CameraResetPadPx * 2f;   // = 77.846008…
        /// <summary>原版 `DOPunchScale` 的三个实参（`.rdata` 直读，`工具/read_literal.py`，见
        /// <see cref="ToggleCameraResetButton"/> 的判据表）：punch = `Vector3.one × 0.2` · 时长 **0.5 s** ·
        /// vibrato **10** · elasticity **1.0**。</summary>
        public const float CameraResetPunchScale = 0.2f, CameraResetPunchTime = 0.5f, CameraResetPunchElasticity = 1f;
        public const int CameraResetPunchVibrato = 10;

        /// <summary>那一层**显隐 + 出现时弹一下**（原版 `BattleHud.ToggleResetAutoCameraZoom(bool active)`）。
        ///
        /// <para>=== 判据（全是实读；⛔ 没有一个是推的）===</para>
        /// <list type="bullet">
        /// <item><b>节点 / 几何</b> = `bundle_scenes_scenes_battlearena1` 的 `RectTransform_3487/3544.json` +
        ///   `GameObject/CenterCameraButton.json`：它在
        ///   `BattleHud/Canvas/BackCanvas/Safe area BackCanvas/LeftArea/Left Anchor/CenterCameraButton`；
        ///   父 `Left Anchor` 锚 (0,0)→(0,1) · pivot (0,0.5) · `sizeDelta (100,0)` ⇒ 1920×1080 画布上宽 100 的整列；
        ///   本件锚 (0.5,0.5) · pivot (0.5,0.5) · `anchoredPosition (0.150757, −59.097)` ·
        ///   `sizeDelta (64.4429931640625, 61.84600830078125)`
        ///   ⇒ **rect x[17.929, 82.372] · y(从上)[568.174, 630.020]**（与 `BuildHud` 里那几个数逐值对上；
        ///   实拍 dump `runtime_ui_dump_drive_0912.tsv` 也印着 `0.2,−59.1` / `64.4,61.8`）。</item>
        /// <item><b>图</b> = `MonoBehaviour_4796.json`（= 那颗 `Button` 的 `m_TargetGraphic`）：
        ///   sprite **`40k_UI_bt_center_camera`**（237×237）· `m_Type 0`(Simple) · **`m_PreserveAspect 1`**
        ///   ⇒ 画出来的是**内接**的 61.846×61.846；`m_RaycastTarget 1` · `m_RaycastPadding (−8,−8,−8,−8)`。</item>
        /// <item><b>钮</b> = `MonoBehaviour_4733.json`：`m_Transition 1`(ColorTint) · `m_Interactable 1` ·
        ///   `m_SpriteState.m_HighlightedSprite = 40k_UI_bt_voicelines_hover` / `…Pressed = 40k_UI_bt_voicelines_pressed`。
        ///   ⚠️ 它那条 `m_OnClick` 里**只有一条 `m_Target = null` 的残留**（`BattleManager.ClickChat`）⇒
        ///   **实际是代码挂的**：`BattleHud.Initialize` 用 `UIGenericEventCatcher.SourceDelegate`
        ///   把 **`DoResetCameraZoom`** 加到那颗钮的 onClick 上（`BattleHud__Initialize.c:58-63`）。</item>
        /// <item><b>显隐</b>（`BattleHud__Initialize.c:68-80` + `BattleHud__ToggleResetAutoCameraZoom.c` 逐句）：
        ///   ① `Initialize` 里**先 `SetActive(false)`** 再 `DOTween.Kill(transform)`；
        ///   ② `ToggleResetAutoCameraZoom(active)` = `SetActive(active)` → `Kill(transform)` →
        ///      **`active` 时**再 `DOPunchScale`。三个常量 `.rdata` 直读：`0x1834b2bb0 = 0.2` ·
        ///      `0x1834b2bb4 = 0.5` · `0x1834b2bb8 = 1.0`；那个被乘的静态 `Vector3` 是 **`Vector3.one`**
        ///      （判据：`DAT_1842da2a8` 那个全局在 `CustomTypes__SerializeVector3.c:19` 里就是 `Vector3` 类，
        ///      静态块 `+0x0C` 正是 `one`（`zero@0` · `one@0xC` · `up@0x18` …），读的是 `+0xc`/`+0x14` ⇒ `.x/.z`）。</item>
        /// <item><b>什么时候出现 / 收起</b>（三处调用点，全实读）：
        ///   ① `CombatCameraZoom.LateUpdate`：`if (ScrollDelta != 0 || TouchPressedSecondary)` ⇒
        ///      `ToggleResetAutoCameraZoom(allowManualControl)` —— **玩家一滚轮 / 一按右键它就出现**；
        ///   ② `CombatCameraZoom.SetZoomLevel(…, force:true)` 与 `CombatAutoZoom.SetZoomLevel(…, force:true)` ⇒
        ///      `ToggleResetAutoCameraZoom(false)` —— **重置/重算那一刻收起来**；
        ///   ③ `CombatCameraZoom.ToggleManualCameraControl(bool)`（本 build 无调用点）⇒ `option &amp;&amp; allowManualControl`。
        ///   ⇒ **它是「手动动过镜头才出现」那一类**（⛔ 不是常亮、也不是开局就在）。</item>
        /// <item><b>点它发生什么</b> = `BattleHud.DoResetCameraZoom()`（`BattleHud__DoResetCameraZoom.c`：
        ///   把 `+0xc0` 那条 `public Action ResetCameraZoom` Invoke 出来）⇒ `CombatAutoZoom.ResetCameraZoomUIAction()`
        ///   ⇒ `SetZoomLevel(max(currentEnemySize, currentPlayerSize), force:true)`
        ///   = 清手动档 + 按当前人数重算 zoom + 收起本钮。见 <see cref="ResetCameraZoomClick"/>。</item>
        /// </list>
        /// <para>⚠️ 那条「`Initialize` 里先关着」有一条**实拍旁证**：`资料/原版实拍/arena_0920/` 那 **13 份**实拍 dump
        /// 里 `CenterCameraButton` **13/13** 都是 `True` —— 但**同一批里 `OffensiveButton` 也是 13/13 `True`**，
        /// 而 `BattleHud.Initialize` 同样把它关掉（`+0x70`，`BattleHud__Initialize.c:43-45`）⇒ 那不可能是
        /// 「`Initialize` 跑过之后」的状态（两张同族节点都没关）⇒ **那批 dump 是 `Initialize` 跑之前**的快照，
        /// 不构成反证。判「开局关着 / 动过才出现」的**第一权威仍是方法体**。
        /// ⚠️ 真 Play 能验的那一条：**滚一下滚轮 ⇒ 那颗钮出现；点它 ⇒ 消失**（`资料/真Play待验清单.md` 的口径）。</para>
        /// <para>⛔ **别把 `active` 写成「反正一直显示」** —— 显隐本身就是「玩家有没有手动控过镜头」那一个状态的可视化，
        /// 一直亮 = 那颗钮在撒谎。</para></summary>
        public void ToggleCameraResetButton(bool active)
        {
            if (_cameraResetBtn == null) return;
            var go = _cameraResetBtn.gameObject;
            go.SetActive(active);
            // 原版那两句 `DG_Tweening_DOTween__Kill(transform,…)` —— **每次**都调（先杀掉上一次没播完的 punch）。
            // `DOTween.Kill(target)`（`complete` 缺省 = false）与 `transform.DOKill()` 同义。
            DG.Tweening.DOTween.Kill(_cameraResetBtn.transform);
            if (!active) return;
            CameraResetPunchCount++;
            _cameraResetBtn.transform.localScale = Vector3.one;   // 先归位（同 `Battle/TargetReticle.PunchIfKindChanged` 那一句）
            var tw = _cameraResetBtn.transform
                     .DOPunchScale(Vector3.one * CameraResetPunchScale, CameraResetPunchTime,
                                   CameraResetPunchVibrato, CameraResetPunchElasticity)
                     .SetUpdate(CardTween.Mode);
            if (CardTween.LinkEnabled) tw.SetLink(go);            // 不绑 = 视图销毁后 DOTween 每帧记一条告警（见 `CardTween.Use` 的注释）
        }

        /// <summary>这一下点在那颗钮的**命中矩形**上吗 —— 真实输入与自检走的是同一条判定
        /// （自检拿 `CameraResetButtonWorldPos` 喂进来）。
        /// <para>判据 = 原版 `Image` 的 `m_RaycastTarget = 1` + **`m_RaycastPadding = (−8,−8,−8,−8)`**
        /// （UGUI：命中矩形 = `rectTransform.rect` 按 padding 收/放，**负 = 往外扩** ——
        /// 符号判据与两点出处见 <see cref="CameraResetHitPxW"/> 上面那段）
        /// ⇒ **80.443 × 77.846**（⛔ 原来按「往里缩」做的 48.443×45.846 是错的），见 <see cref="CameraResetHitPxW"/>。</para>
        /// <para>⛔ **别拿 `ImageQuad.Contains` 顶替它** —— 那个量的是**画出来多大**（PA=1 内接的 61.846 正方形），
        /// 不是「点哪儿算中」（原版那两个矩形不一样：那个 **64.443** 宽的 rect 才是命中基准，
        /// 外扩 8 之后是 **80.443** —— 画出来的 61.846 比它**小**，所以「看着在钮上、点不动」不可能由本式造成，
        /// 反过来「看着在钮外、却点得动」才是外扩的正常表现）。</para>
        /// <para>钮关着的时候**恒不命中**（原版 `SetActive(false)` 的节点收不到射线）。</para></summary>
        public bool CameraResetButtonHit(Vector3 w)
        {
            if (_cameraResetBtn == null || !_cameraResetBtn.gameObject.activeSelf) return false;
            var l = _cameraResetBtn.transform.InverseTransformPoint(w);   // 世界 → 本件局部（`ImageQuad` 的原点在中心，见 `Contains`）
            return Mathf.Abs(l.x) <= Px(CameraResetHitPxW) * 0.5f
                && Mathf.Abs(l.y) <= Px(CameraResetHitPxH) * 0.5f;
        }

        /// <summary>那颗钮的点击入口（真实输入那一侧）。原版 = uGUI `Button.onClick` → `BattleHud.DoResetCameraZoom()`。
        /// 规矩与本文件其余钮一致（`HandleOffensiveButton`）：**先看命中区、再耗那个抬起沿**。</summary>
        bool HandleCameraResetButton()
        {
            if (_cameraResetBtn == null || !_cameraResetBtn.gameObject.activeSelf) return false;
            if (!CameraResetButtonHit(WorldPointer())) return false;
            // 🔴 A462：**抬起**（原版 `BattleHud.resetCameraZoomButton` = `EverguildButton`，
            //    `BattleHud__Initialize.c:62-66` 绑 `m_OnClick`）。
            if (!ReleasedThisFrame()) return false;
            return ResetCameraZoomClick();
        }

        /// <summary>点那颗钮**做的那件事** —— 单独拿出来：**真实输入与自检走同一条判定**
        /// （自检拿 `CameraResetButtonWorldPos` 喂进来，⛔ 不直接调 `ForceRefresh`）。
        /// <para>原版那条链（`BattleHud__DoResetCameraZoom.c` 逐句）：那条 `Action` 的 `Invoke`
        /// ⇒ `CombatAutoZoom.ResetCameraZoomUIAction()` ⇒ `SetZoomLevel(Max(敌,我), force:true)`。
        /// 而 `CombatAutoZoom.Initialize()` 已经把 `ResetCameraZoomUIAction` 订阅在 `RaiseResetCameraZoom` 那条
        /// `Action` 上（= 原版 `BattleHud.ResetCameraZoom` `+0xc0`）⇒ **这一跳就是它**，⛔ 别在这儿另写一次重算。</para>
        /// <para>⚠️ 收尾那一刻它会**把本钮自己收起来**（`force` 那支调 `RaiseToggleResetCameraZoomUi(false)`）。</para></summary>
        public bool ResetCameraZoomClick()
        {
            if (_autoZoom == null)
            {
                // 不许静默：原版那颗钮住在 `BattleHud` 上、`ResetCameraZoom` 由 `CombatAutoZoom` 填 ——
                // 没有那件组件时那颗钮本来也不该是可点的（这一支只在「HUD 建了、3D 相机没有」时走到）。
                Debug.LogWarning("[Battle] 点了「重置自动镜头」钮，但本局**没有** `CombatAutoZoom`"
                               + "（没有 3D 战场相机 ⇒ 建不出来）⇒ 这一下没有效果");
                return false;
            }
            _autoZoom.RaiseResetCameraZoom();
            return true;
        }

        // ---- 自检口（⛔ 只读，不给生产用）----

        /// <summary>自检用：那颗钮建出来了没有。</summary>
        public bool CameraResetButtonBuilt { get { return _cameraResetBtn != null; } }
        /// <summary>自检用：那颗钮现在**显示着**吗（= 原版 `SetActive` 那一格）。</summary>
        public bool CameraResetButtonVisible { get { return _cameraResetBtn != null && _cameraResetBtn.gameObject.activeSelf; } }
        /// <summary>自检用：那颗钮中心的**世界坐标**（把它喂给 `CameraResetButtonHit` / `ResetCameraZoomClick`）。</summary>
        public Vector3 CameraResetButtonWorldPos
        {
            get { return _cameraResetBtn != null ? _cameraResetBtn.transform.position : Vector3.zero; }
        }
        /// <summary>自检用：那颗钮**画出来**的尺寸（px）= `ImageQuad` 的 `WorldW/WorldH × 108`
        /// （原版 PA=1 ⇒ 期望 61.846×61.846，**内接**进那个 64.443×61.846 的 rect）。</summary>
        public Vector2 CameraResetButtonDrawnPx
        {
            get { return _cameraResetBtn != null
                       ? new Vector2(_cameraResetBtn.WorldW, _cameraResetBtn.WorldH) * 108f
                       : Vector2.zero; }
        }
        /// <summary>自检用：那张图的贴图名（判「就是 `40k_UI_bt_center_camera`」）。</summary>
        public string CameraResetButtonArt
        {
            get { return _cameraResetBtn != null && _cameraResetBtn.Texture != null
                       ? _cameraResetBtn.Texture.name : null; }
        }
        /// <summary>自检用：显隐钩子**接上了没有**（= 原版 `BattleHud.ToggleResetAutoCameraZoom` 那一条）。
        /// 没接的话 `CombatCameraZoom` 那三处只会「出声一次」—— 那种静默失败是红线。</summary>
        public bool CameraResetHookWired
        {
            get { return _autoZoom != null && _autoZoom.ToggleResetCameraZoomUi != null; }
        }
        /// <summary>自检用：那颗钮的 punch 播了几次（原版是**每次显示时一下**）。</summary>
        public int CameraResetPunchCount { get; private set; }

        // ==================================================================
        //  🆕 2026-10-18（A985③）：敌方名牌那颗「对手档案」钮 + 它开的那扇窗
        //  rect / 「它为什么没有贴图」/ 命中区那三条判据 → `BuildHudExtras` 里建它那一段（只此一份）。
        // ==================================================================

        // ==================================================================
        //  🆕 2026-10-19（A964②）：**HUD 那几颗 uGUI `Image` 钮的命中矩形** = 原版 `rect` 按 `m_RaycastPadding` 收/放
        //
        //  判据一律 = 解包资源，**13 个战场逐场核过、逐位相同**：
        //   · `rect` = `bundle_scenes_scenes_battlearena1/RectTransform/RectTransform_<pid>.json` 的 `m_SizeDelta`
        //   · `pad`  = 同场景 `MonoBehaviour/MonoBehaviour_<pid>.json` 的 `m_RaycastPadding`（分量序 **L,B,R,T**）
        //   · 符号   = **负值【外扩】/ 正值【内缩】**，且命中基准是**节点自己的 `RectTransform.rect`**
        //              （**不是**画出来的网格）。三条**本地** uGUI 源码判据（`com.unity.ugui` 内置包，
        //              路径前缀 `D:/Unity/Hub/Editor/6000.3.23f1/Editor/Data/Resources/PackageManager/BuiltInPackages/`）：
        //              ① `Runtime/UGUI/UI/Core/GraphicRaycaster.cs:327` ——
        //                 `RectTransformUtility.RectangleContainsScreenPoint(graphic.rectTransform, …, graphic.raycastPadding)`
        //              ② `Runtime/UGUI/UI/Core/Graphic.cs:188-196` —— `raycastPadding` 的 doc：
        //                 **X = Left / Y = Bottom / Z = Right / W = Top**（本文件的 `padL,padB,padR,padT` 就是这个序）
        //              ③ `Tests/Runtime/UGUI/EventSystem/GraphicRaycasterTests.cs:83-101`
        //                 （`GraphicRaycasterUsesGraphicPadding`）：`raycastPadding = (−50,−50,−50,−50)` 之后，
        //                 把指针放到 rect **外面 60 px** ⇒ 断言「**必须命中**」⇒ **负 = 外扩**（铁证）。
        //              同族先例 → `CameraResetButtonHit`（那一颗 2026-10-18 就是这么改的）
        //  🔴 **该场景 739 个带 `m_RaycastPadding` 的 Graphic 里【只有 17 个非零】**，且**13 个战场包
        //     逐包核过：每包都是 17 个、逐节点逐位相同**（现读可复现：按 `m_RaycastPadding` 非零筛
        //     `MonoBehaviour/`、再用 `m_GameObject` 反查节点名）—— 本文件用到的 4 颗都在那 17 个里；
        //     其余 13 个的处置（在谁名下、归哪件账、哪些是别的文件的欠账）→
        //     `资料/普查产出_第十二会话/V2_A964命中区.md` §一/§四。
        //  ⛔ **别拿 `ImageQuad.Contains` 顶替** —— 那个量的是**画出来多大**（PA=1 时是内接的那个矩形），
        //     与「点哪儿算中」不是同两个矩形（同上那条先例的注释）。
        // ==================================================================

        /// <summary>原版 uGUI `Image` 的**命中矩形**判据（**只此一份**）：`world` 落在
        /// 「节点自己的 rect 按 `m_RaycastPadding` 收/放」那个矩形里吗。
        /// <para>🔴 **算式不在这里** —— 转发全工程那唯一一份 `MenuDraw.PaddedHitRect`
        /// （它自己再转发 `PaddedRect`；**符号口径与「pad 比框还大」的退化守卫都在那一边**）。
        /// ⛔ 别在这里再抄一遍 `x1 + pad.x / x2 − pad.z` —— CLAUDE.md §三：两处写同一条规则 = 迟早不一致。</para>
        /// <para>`pad*` 分量序 **L,B,R,T**、**负值外扩**（判据见上面那段）；px 一律**画布像素**。
        /// ⚠️ 坐标系要跳一下：`PxRect` 是**左上原点、y 向下**，而 quad 的局部 y 向上。</para>
        /// <para>⚠️ **本函数不含 `activeSelf` 守卫** —— 那是各调用点自己的语义
        /// （`HandleSettings` 那一颗**故意**不判，A964 的 `E4` 白名单台账正盯着它），⛔ 别在这里一刀切。</para></summary>
        static bool HitPaddedRect(ImageQuad q, Vector3 world, float rectWpx, float rectHpx,
                                  float padL, float padB, float padR, float padT)
        {
            if (q == null) return false;
            var l = q.transform.InverseTransformPoint(world);   // 世界 → 本件局部（原点在中心，同 `ImageQuad.Contains`）
            float ppu = 1f / Px(1f);                           // = 108：取 `Px` 的倒数 ⇒ 不写第二份 108
            float lx = l.x * ppu, ly = -l.y * ppu;             // 本件局部 px（**左上原点、y 向下**，与 `PxRect` 同帧）
            var hit = MenuDraw.PaddedHitRect(
                new PxRect(-rectWpx * 0.5f, -rectHpx * 0.5f, rectWpx * 0.5f, rectHpx * 0.5f),
                new Vector4(padL, padB, padR, padT));
            return lx >= hit.x1 && lx <= hit.x2 && ly >= hit.y1 && ly <= hit.y2;
        }

        // ---- ① 设置钮 `RightArea/SettingsBtn`（`MonoBehaviour_4012` · `RectTransform_2585`）----
        //  四个 padding 分量**互不相同**（原版就长这样）⇒ 命中框 113.90×128.28，且它**不居中**在
        //  那颗 quad 上（相对中心偏右 1.885 / 偏下 6.395 px）—— 这是 `PaddedRect` 的**自然结果**，
        //  ⛔ 不是我们另外加的一个偏移项（所以这里也没有第二个算式可抄错）。
        const float SettingsBtnRectW = 63.874f, SettingsBtnRectH = 63.874f;
        const float SettingsBtnPadL = -23.13f, SettingsBtnPadB = -38.6f,
                    SettingsBtnPadR = -26.9f, SettingsBtnPadT = -25.81f;
        /// <summary>自检用：设置钮**命中矩形**的 px 尺寸（= rect 按 padding 收/放 ⇒ 原版 **113.90×128.28**；
        /// ⛔ 不是画出来那个 63.87²）。</summary>
        public static Vector2 SettingsBtnHitPx
        {
            get { return new Vector2(SettingsBtnRectW - SettingsBtnPadL - SettingsBtnPadR,
                                     SettingsBtnRectH - SettingsBtnPadB - SettingsBtnPadT); }
        }
        /// <summary>自检用：那颗钮命中框中心相对 quad 中心偏多少 px（x 右 / y 上）。
        /// ⛔ 别当成「恒 0」—— 四边 padding 不等宽时它不为零（本颗 **(+1.885, −6.395)**）。</summary>
        public static Vector2 SettingsBtnHitOffsetPx
        {
            get { return new Vector2((SettingsBtnPadL - SettingsBtnPadR) * 0.5f,
                                     (SettingsBtnPadB - SettingsBtnPadT) * 0.5f); }
        }
        /// <summary>设置钮的命中了没有（真实输入 → `HandleSettings`）。
        /// ⚠️ **不判 `null` / `activeSelf`** —— 调用点自己先判过（那颗钮**故意**没有 `activeSelf` 守卫）。</summary>
        public bool SettingsBtnHit(Vector3 w)
        {
            return HitPaddedRect(_settingsBtn, w, SettingsBtnRectW, SettingsBtnRectH,
                                 SettingsBtnPadL, SettingsBtnPadB, SettingsBtnPadR, SettingsBtnPadT);
        }

        // ---- ② 聊天（语音条）钮 `PlayerInfo/ChatButton`（`MonoBehaviour_5007` · `RectTransform_2984`）----
        const float ChatBtnRectW = 64.443f, ChatBtnRectH = 61.846f;
        const float ChatBtnPadL = -8f, ChatBtnPadB = -8f, ChatBtnPadR = -8f, ChatBtnPadT = -8f;
        /// <summary>自检用：聊天钮**命中矩形**的 px 尺寸（四边各外扩 8 ⇒ **80.443×77.846**）；
        /// ⚠️ 画出来的是 128² 贴图 `PA=1` **内接**的 61.846 正方 —— 两个数**不相等**，⛔ 别混。</summary>
        public static Vector2 ChatBtnHitPx
        {
            get { return new Vector2(ChatBtnRectW - ChatBtnPadL - ChatBtnPadR,
                                     ChatBtnRectH - ChatBtnPadB - ChatBtnPadT); }
        }
        /// <summary>聊天钮的命中了没有（真实输入 → `HandleChatPopup` 的关着那一半）。</summary>
        public bool ChatBtnHit(Vector3 w)
        {
            return HitPaddedRect(_chatBtn, w, ChatBtnRectW, ChatBtnRectH,
                                 ChatBtnPadL, ChatBtnPadB, ChatBtnPadR, ChatBtnPadT);
        }

        // ---- ③ 战斗日志（墓地）钮 `EnemyInfo/ShowCemeteryBtn`（`MonoBehaviour_4020` · `RectTransform_2586`）----
        const float CemeteryBtnRectW = 64.478f, CemeteryBtnRectH = 64.17f;
        const float CemeteryBtnPadL = -8f, CemeteryBtnPadB = -8f, CemeteryBtnPadR = -8f, CemeteryBtnPadT = -8f;
        /// <summary>自检用：日志钮**命中矩形**的 px 尺寸（四边各外扩 8 ⇒ **80.478×80.170**）。</summary>
        public static Vector2 CemeteryBtnHitPx
        {
            get { return new Vector2(CemeteryBtnRectW - CemeteryBtnPadL - CemeteryBtnPadR,
                                     CemeteryBtnRectH - CemeteryBtnPadB - CemeteryBtnPadT); }
        }
        /// <summary>日志钮的命中了没有（真实输入 → `HandleBattleLog` 的关着那一半）。</summary>
        public bool CemeteryBtnHit(Vector3 w)
        {
            return HitPaddedRect(_cemeteryBtn, w, CemeteryBtnRectW, CemeteryBtnRectH,
                                 CemeteryBtnPadL, CemeteryBtnPadB, CemeteryBtnPadR, CemeteryBtnPadT);
        }

        // ---- ④ 结束回合钮 `Clock/TurnBtn`（`MonoBehaviour_4543` · `RectTransform_2913`）----
        //  ⚠️ 原版那颗的**子件** `TurnBtn/TurnText` 是 `m_RaycastTarget = 0` ⇒ **收不到射线**
        //     ⇒ 命中区**只**由底图这颗定（`HitEndTurn` 里那半句「按文字判」是我们美术缺图时的兜底，
        //     原版没有那条路，见那里的注释）。
        const float EndTurnBtnRectW = 130.702f, EndTurnBtnRectH = 80.432f;
        const float EndTurnBtnPadL = -14.19f, EndTurnBtnPadB = -30.3f,
                    EndTurnBtnPadR = -26.19f, EndTurnBtnPadT = -18.4f;
        /// <summary>自检用：结束回合钮**命中矩形**的 px 尺寸（⇒ **171.082×129.132**，
        /// 比画出来那个 130.7×80.4 大不少）。</summary>
        public static Vector2 EndTurnBtnHitPx
        {
            get { return new Vector2(EndTurnBtnRectW - EndTurnBtnPadL - EndTurnBtnPadR,
                                     EndTurnBtnRectH - EndTurnBtnPadB - EndTurnBtnPadT); }
        }
        /// <summary>自检用：那颗钮命中框中心相对 quad 中心偏多少 px（x 右 / y 上）⇒ 本颗 **(+6.0, −5.95)**。</summary>
        public static Vector2 EndTurnBtnHitOffsetPx
        {
            get { return new Vector2((EndTurnBtnPadL - EndTurnBtnPadR) * 0.5f,
                                     (EndTurnBtnPadB - EndTurnBtnPadT) * 0.5f); }
        }

        /// <summary>那颗钮的 rect（px · 左上原点 · 1920×1080）= `EnemyInfo` 的五元组
        /// （判据 = `RectTransform_2687.json`，13 场逐位相同）。</summary>
        const float AllianceBtnXPx = 50f, AllianceBtnYPx = 28f;
        const float AllianceBtnWPx = 260f, AllianceBtnHPx = 75f;
        /// <summary>原版那颗钮的 `m_RaycastPadding`（`MonoBehaviour_4095.json`）——
        /// UGUI 分量序 **L,B,R,T**、**负值 = 把命中矩形【往外扩】**（符号判据见 `CameraResetHitPxW`）。</summary>
        const float AllianceBtnPadL = 24.780000686645508f;
        const float AllianceBtnPadB = 18.450000762939453f;
        const float AllianceBtnPadR = 25.290000915527344f;
        const float AllianceBtnPadT = 0f;

        /// <summary>那颗钮的**命中矩形**（px · 左上原点）= 上面那个 rect 按 padding 四边**外扩**
        /// ⇒ 实测 **x[25.22, 335.29] · y[28, 121.45]**（310.07 × 93.45）。
        /// <para>🔴 **本算式只此一份**：建那张占位 quad（几何 = 它）· 真实命中判定 · 自检三处都走它，
        /// ⛔ 别在调用点各算一遍（两处写同一条规则 = 迟早不一致）。</para>
        /// <para>⛔ 别把 padding 的符号当「往里缩」—— 那会砍掉原版真正可点的那四边
        /// （同族 `A964` 那条「看着在钮上、点不动」就是这么来的）。</para></summary>
        public static PxRect AllianceButtonHitRectPx
        {
            get
            {
                return new PxRect(AllianceBtnXPx - AllianceBtnPadL,          // 左：rect 左缘 − L（负 L = 左扩）
                                  AllianceBtnYPx - AllianceBtnPadT,          // 上：rect 上缘 − T
                                  AllianceBtnXPx + AllianceBtnWPx + AllianceBtnPadR,
                                  AllianceBtnYPx + AllianceBtnHPx + AllianceBtnPadB);
            }
        }

        /// <summary>世界坐标打在那颗钮的**命中矩形**上吗 —— **真实输入与自检走的是同一条判定**。
        /// <para>⚠️ 这里**可以**直接用 `ImageQuad.Contains`（与本文件那颗 `CenterCameraButton` 的处境不同）：
        /// 本件**原版就没有贴图**（`NonDrawingGraphic`，一个像素都不画）⇒ 不存在「画出来的尺寸 ≠ 命中基准」
        /// 那件事 —— 我们特意把那张 quad 的几何**拉成命中矩形本身**（见 `BuildHudExtras` 里那一段）。
        /// ⛔ 别顺手改成「按画出来的那 260×75 判」：那等于把原版外扩出去的四边砍掉。</para>
        /// <para>钮关着的时候**恒不命中**（原版 `SetActive(false)` 的节点收不到射线，同族 A940 那条口径）。</para></summary>
        public bool AllianceButtonHit(Vector3 w)
        {
            if (_allianceBtn == null || !_allianceBtn.gameObject.activeSelf) return false;
            return _allianceBtn.Contains(w);
        }

        /// <summary>联盟面板「名字」那一格传什么 —— 🔴 **只此一份**（真实点击与自检都读它）。
        /// <para>原版那一格 = `BattleManager.matchData` 里**对手的名字**
        /// （`BattleHud__AlliancePanelOpenButton.c` 取 `+0x10`）⇒ 我们的等价物：</para>
        /// <list type="bullet">
        /// <item>**联机局** = 对端真名（`NetMatchmaking.FoeName`）—— 与原版那一格**逐字等价**；</item>
        /// <item>**单机局** = **名牌上印的那一串**（`FoePlateText`，单机时就是阵营名）——
        ///   即**玩家刚刚点下去的那块牌**上写着的同一个字符串。
        ///   ⛔ 我们**没有 bot 的名字数据源** ⇒ 取「玩家实际看到的那一串」，⛔ **不编造**一个新名字
        ///   （本工程红线：不静默失败、不说谎）。</item>
        /// </list>
        /// <para>⛔ 也**不给 bot 编联盟名 / 称号 / 评级** —— 那三格照原版单机形态恒为空
        /// （`Open(name, "", "")` ⇒ 称号关 + `Alliance` 组关 + 「还没有加入任何联盟」那行开，
        ///  那**正是**原版单机情境的形态）。</para>
        /// <para>⚠️ 判「有没有对端名」的那个谓词**与名牌那一行同一个**（`UpdateHud` 里写 `_enemyText` 那一句）——
        /// ⛔ 别简化成「只要 `_net != null`」：那样「联机但还没握手/对端名为空」的局会把**空串**当名字传进去。</para></summary>
        public string AlliancePanelName
        {
            get
            {
                return (_net != null && !string.IsNullOrEmpty(NetMatchmaking.FoeName))
                       ? NetMatchmaking.FoeName
                       : (FoePlateText ?? "");
            }
        }

        /// <summary>`Alliance Panel` 那扇窗这一路的输入。规矩同 `HandleSettings` / `HandleChatPopup`：
        /// **开着 ⇒ 无条件接管（模态，⛔ 不许落到棋盘上）**；关着 ⇒ **只在指针落在那颗钮上**才接管。
        /// <para>**关窗的沿 = 抬起**：原版 `backgroundCloseButton` 是 uGUI `Button`，
        /// `BattleAlliancePanel__Awake.c` 把 `CloseButtonClick` 加进它的 `m_OnClick`
        /// ⇒ `IPointerClickHandler` ⇒ **抬起那一帧**才触发（⛔ 不是 `ClickedThisFrame`）。</para>
        /// <para>**点窗内 ⇒ 什么都不做**：原版 `BG` / `BGFrame` 那几张 Image 的 `m_RaycastTarget = 1`
        /// 把它们上面的射线吃掉了，传不到底下那颗 `Close Background` ——
        /// 我们这半边用 `AlliancePanelWindow.HitBody`（判据 = 那层**吸收区用的同一个矩形**）。</para>
        /// <para>⚠️ 那扇窗自己在 `Build` 里建的 `ShadeHit` 挂的是 `WindowButton`，
        /// **只有外壳那套 `PointerLayer` 会派发**；战场里不走那一条路 ⇒ **关窗这一手必须由这里接**</para></summary>
        bool HandleAlliancePanel()
        {
            if (_alliancePanel != null && _alliancePanel.Visible)
            {
                if (ReleasedThisFrame() && !_alliancePanel.HitBody(WorldPointer()))
                    _alliancePanel.Close();
                return true;                       // 模态：这一帧就此打住
            }
            if (_allianceBtn == null || !_allianceBtn.gameObject.activeSelf) return false;
            if (!AllianceButtonHit(WorldPointer())) return false;
            if (!ReleasedThisFrame()) return false;         // 原版那颗钮 = uGUI `Button` ⇒ 抬起
            if (_alliancePanel == null)
            {
                // 只可能在建场那一步出问题 —— 说出来，别静默吞掉这一下
                Debug.LogWarning("[Battle] 点了敌方名牌，但 `AlliancePanelWindow` 没建起来 ⇒ 这一下没有效果");
                return true;
            }
            _alliancePanel.Open(AlliancePanelName, "", "");  // 称号 / 联盟名恒空 = 原版单机形态
            return true;
        }

        // ---- 自检口（⛔ 只读，不给生产用）----

        /// <summary>自检用：那颗钮 / 那扇窗建出来了没有。</summary>
        public bool AllianceButtonBuilt { get { return _allianceBtn != null; } }
        public bool AlliancePanelBuilt { get { return _alliancePanel != null; } }
        /// <summary>自检用：那扇窗现在开着吗。</summary>
        public bool AlliancePanelVisible { get { return _alliancePanel != null && _alliancePanel.Visible; } }
        /// <summary>自检用：那扇窗本体（两态 / 名字左缘那几条断言要直接调它的 `Open`）。</summary>
        public AlliancePanelWindow AlliancePanelRef { get { return _alliancePanel; } }
        /// <summary>自检用：那颗钮中心的**世界坐标**（把它喂给 `AllianceButtonHit` / 那一路输入闸）。</summary>
        public Vector3 AllianceButtonWorldPos
        {
            get { return _allianceBtn != null ? _allianceBtn.transform.position : Vector3.zero; }
        }
        /// <summary>自检用：那张占位 quad **画出来**的尺寸（px）—— 应**恒等于命中矩形**
        /// （原版没有贴图 ⇒ 没有「画出来的 ≠ 命中基准」那件事，见 `AllianceButtonHit`）。</summary>
        public Vector2 AllianceButtonDrawnPx
        {
            get { return _allianceBtn != null
                       ? new Vector2(_allianceBtn.WorldW, _allianceBtn.WorldH) * 108f
                       : Vector2.zero; }
        }
        /// <summary>自检用：那张占位图的染色（应 **α = 0**）。
        /// 🧨 改坏法：把 `SetTint(…, 0f)` 去掉或写成不透明 ⇒ 敌方名牌上多出一块白板。</summary>
        public Color AllianceButtonTint
        {
            get { return _allianceBtn != null ? _allianceBtn.Tint : Color.white; }
        }

        /// <summary>`ChatPopup` 的点击。规矩和设置面板一样：
        /// **开着 ⇒ 无条件接管（模态）**；关着 ⇒ **只在指针落在 `ChatButton` 上**才接管。
        /// （两条沿各自只能在这里耗一次 —— 见 `HandleSettings` 上面那段注释。）
        ///
        /// 🔴 **2026-10-13（A462）：开着那一段是【两种不同的触发沿】，拆成两半 —— ⛔ 别合并。**
        /// · **条外关闭** = 全屏关闭区 `CloseChatPopup`：原版挂的是 `EventTrigger`，
        ///   `m_Delegates[].eventID = 2 = PointerDown`（判据见 `PollInputEdges` 上面那一段）
        ///   ⇒ 走 `ClickedThisFrame()`，**按下那一帧就收**。
        /// · **选台词** = 6 颗 `ChatButton (1)..(5)` / `ChatButton`，组件 `ChatPopupButton : EverguildButton`
        ///   （`ChatPopupButton.cs:7`）⇒ uGUI `onClick`，**抬起那一帧**才说那句话。
        /// ⚠️ 两半的**命中优先级不动**（原版靠 `GraphicRaycaster` 排序，谁在上没读 ⇒ 保持我们现有算法）。</summary>
        bool HandleChatPopup()
        {
            if (_chatPopup != null && _chatPopup.Visible)
            {
                var wp = WorldPointer();
                if (ClickedThisFrame())
                {
                    // 条外那一按**已经把面板关掉了** ⇒ 吞掉**同一次按住**的松手沿
                    // （理由同 `HandleBattleLog`：原版那块全屏关闭区在最上层，底下那颗 `ChatButton`
                    //   收不到这次按下 ⇒ 松手不该把它又打开）。
                    if (_chatPopup.PointerDownAt(wp)) _swallowNextRelease = true;
                    return true;
                }
                if (ReleasedThisFrame() && _chatPopup.Visible)
                    SpeakChatAt(_chatPopup.PointerUpAt(wp));
                return true;
            }
            // 🆕 2026-10-18（A940）：**藏起来的钮不许还能点**。原版那颗是 uGUI 按钮，
            //   被 `SetActive(false)` 之后**收不到点击**（`TutorialSetup.c:52-60` 就是 `SetActive`）；
            //   我们这是 `ImageQuad`，**不渲染 ≠ 不响应** ⇒ 这里必须显式判 activeSelf，
            //   否则教程局里那颗钮**看不见却还点得开**（静默、而且看起来像「点了没反应」的反面）。
            // 🆕 **2026-10-19（A964②）**：命中区 = 原版 `rect` 64.443×61.846 按 `m_RaycastPadding`
            //   (−8,−8,−8,−8) 四边外扩 ⇒ **80.443×77.846**（原来 `Contains` 量的是 PA=1 内接的 61.846 正方）。
            if (_chatBtn == null || !_chatBtn.gameObject.activeSelf
                || !ChatBtnHit(WorldPointer())) return false;
            // 🔴 A462：**抬起**（原版那颗 `ChatButton` 也是 `EverguildButton` ⇒ `m_OnClick`）。
            if (!ReleasedThisFrame()) return false;
            if (_chatCooldown > 0f)
            {
                // 原版冷却中按钮 `interactable = false`（按不动）—— **说出来**，别静默吞掉
                Debug.Log($"[Battle] `ChatButton` 冷却中（还剩 {_chatCooldown:F1}s）—— 原版也是按不动");
                return true;
            }
            _chatPopup.Show();
            return true;
        }

        /// <summary>「让我方督军说 `ChatPopup` 第 `hit` 句」那一步（**冷却也在这一处**）——
        /// `ChatClickAt` 与 `HandleChatPopup` 的抬起半**共用它**，别在两处各写一遍。
        /// `hit &lt; 0` = 没点到钮（`false`）。</summary>
        bool SpeakChatAt(int hit)
        {
            if (hit < 0) return false;
            if (SpeakChat(hit)) _chatCooldown = ChatPopupPanel.Cooldown;   // 原版说完进 4 秒冷却
            return true;
        }

        /// <summary>一次**完整**的点击落在 `ChatPopup` 上（按下 + 抬起在同一个点上）——
        /// **真实输入与自检走同一条判定**（自检拿钮的世界坐标喂进来，不直接调 `SpeakChat`），
        /// 只是真实那一路把它拆成了**两帧两个入口**（见 `HandleChatPopup`）。
        /// 返回「这一下被面板吃掉了没有」。</summary>
        public bool ChatClickAt(Vector3 w)
        {
            if (_chatPopup == null || !_chatPopup.Visible) return false;
            if (_chatPopup.PointerDownAt(w)) return true;   // 面板外 ⇒ 面板自己关掉了（原版那条全屏关闭区）
            SpeakChatAt(_chatPopup.PointerUpAt(w));
            return true;
        }

        /// <summary>让**我方督军**说 `ChatPopup` 第 `idx` 个钮那句话
        /// （原版 `BattleManager.DisplayWarlordRegularChatMessage` → 枚举 5+idx）。
        /// 返回说成了没有 —— **false 要在调用方报出来，不许静默**。</summary>
        public bool SpeakChat(int idx)
        {
            if (_unitChat == null || !VoiceLines.Ready || Ctx == null) return false;
            if (idx < 0 || idx >= VoiceLines.ForChatButton.Length) return false;
            var w = Ctx.Players[_me] != null ? Ctx.Players[_me].Warlord : null;
            if (w == null || w.Card == null) return false;

            string file, text, ev;
            // 随机源传 null —— 与别处同规矩：**不消耗 `Ctx.Rng`**
            if (!VoiceLines.TryPick(w.Card.Id, new[] { VoiceLines.ForChatButton[idx] }, null,
                                    out file, out text, out ev))
                return false;
            var clip = VoiceLines.Clip(file);
            if (clip == null) return false;

            _unitChat.Speak(_me, w.Card.Id, "Chat" + idx, ArtKey(w.Card), CardDisplayName(w.Card, null), text, clip, file);
            return true;
        }

        /// <summary>自检用：`ChatPopup` 面板</summary>
        public ChatPopupPanel ChatPopup { get { return _chatPopup; } }
        /// <summary>自检用：`ChatButton` 的剩余冷却秒数</summary>
        public float ChatCooldownLeft { get { return _chatCooldown; } }

        /// <summary>自检用：设置面板 / 设置按钮</summary>
        public SettingsPanel Settings { get { return _settingsPanel; } }

        /// <summary>自检用：回放条（原版 `ReplayButtons`）</summary>
        public ReplayBar Replay { get { return _replayBar; } }

        // ==================================================================
        //  单位语音条（原版 `Unit Chat/PlayerChatDisplay` · `EnemyChatDisplay`）
        //
        //  形状与数值全在 `UnitChatPanel.cs` 头里；数据在 `Core/VoiceLines.cs` + `voice_lines.json`
        //  （由 `工具/import_original_audio.py` 从原版解包资源生成）。
        //  🔴 **事件 → 台词后缀的对应是我们定的**：原版只留下了「有哪些台词」，
        //     没留下「什么时机播哪一条」（`资料/战斗UI_原版对账表.md` §三·〇 :127）。
        // ==================================================================

        UnitChatPanel _unitChat;

        /// <summary>自检用：单位语音条</summary>
        public UnitChatPanel UnitChat { get { return _unitChat; } }

        /// <summary>一条引擎事件 → 一句台词。**只有部署 / 攻击 / 阵亡三种事件会说话**。</summary>
        void SpeakFor(BattleEvent e)
        {
            if (_unitChat == null || !VoiceLines.Ready) return;
            if (e.Player < 0 || e.Player > 1) return;

            string[] order;
            switch (e.Kind)
            {
                case EvtKind.Deploy: order = VoiceLines.ForDeploy; break;
                case EvtKind.Attack: order = VoiceLines.ForAttack; break;
                case EvtKind.Death:  order = VoiceLines.ForDeath;  break;
                default: return;
            }

            // 先看棋盘上那个人还在不在。**阵亡那一条他已经离场了** ⇒ 退回按卡名找；
            // ⚠️ 用**带阵营**的那个重载 —— 原版有跨阵营同名卡，按名字裸找会拿错那一张。
            CardDef card = null;
            var board = Ctx.Players[e.Player].Board;
            if (BoardSpec.IsValid(e.Slot) && board[e.Slot] != null) card = board[e.Slot].Card;
            if (card == null && !string.IsNullOrEmpty(e.CardId))
                card = CardDatabase.Find(_pool, e.CardId, e.Player == _me ? _myFaction : _foeFaction);
            if (card == null) return;

            string file, text;
            // ⚠️ 随机源传 **null**：**不消耗 `Ctx.Rng`**（那会把同一局的随机序列挪位，
            //    按种子写死期望值的自检会集体漂移）⇒ 同一局完全可复现；督军那种台词池也取第一条。
            if (!VoiceLines.TryPick(card.Id, order, null, out file, out text)) return;
            var clip = VoiceLines.Clip(file);
            if (clip == null) return;      // 表里有、音频没导进来 —— `VoiceLines.Clip` 已经报过警告了

            _unitChat.Speak(e.Player == _me ? 0 : 1, card.Id, e.Kind.ToString(),
                            ArtKey(card), CardDisplayName(card, null), text, clip, file);
        }

        /// <summary>投降时说一句（原版 `concede` 那一族台词；只有督军有）。</summary>
        void SpeakConcede(int who)
        {
            if (_unitChat == null || !VoiceLines.Ready) return;
            var w = Ctx != null && who >= 0 && who < 2 ? Ctx.Players[who].Warlord : null;
            var card = w != null ? w.Card : null;
            if (card == null) return;

            string file, text;
            if (!VoiceLines.TryPick(card.Id, VoiceLines.ForConcede, null, out file, out text)) return;
            var clip = VoiceLines.Clip(file);
            if (clip == null) return;
            _unitChat.Speak(who == _me ? 0 : 1, card.Id, "Concede", ArtKey(card), CardDisplayName(card, null), text, clip, file);
        }

        /// <summary>🆕 **2026-10-18（第十五轮 · `G5`）**：原版「操作被拒」那族**文字**提示的两条键
        /// —— **只此一份**（⛔ 别在调用点手写字符串）。
        ///
        /// <para>🔴 **为什么单开常量**：原版的 `BattleTipController.NotifyCantDoAction(text, flag)`
        /// （`d:/2/tools/decomp_full/BattleTipController__NotifyCantDoAction.c:5-14`）是**一次调用出两样**：
        /// 先 `VoiceLinesController.DisplayLocalChatMessage(vlc, 3 /*ICantDoThat*/, skipCanChat:1)`
        /// （= 我们已有的 `SpeakCantDo`），**再** `ShowHeadsUpMessage(text, flag)`（= 那行**文字**）。
        /// 我们过去只做了语音那一半 ⇒ 玩家**听得到、看不见**。本批把这两条加上。</para>
        ///
        /// <para>判据（两条都是**载波②**：键在代码字面量里、prefab 上零 `Localize`，见 `Loc.cs` 那一块）：</para>
        /// <list type="bullet">
        ///   <item><see cref="DragToTargetTerm"/> = `0x428A030`，
        ///   消费点 = `BattleManager.Update` 的两处（`BattleManager__Update.c:642-672` 手牌拖回 /
        ///   `:1196-1213` 瞄准松手落空）⇒ 我们落在 `OnCardReturned`（`CardInteraction.Release` 的 `else` 支）。</item>
        ///   <item><see cref="UnitNotReadyTerm"/> = `0x428AAB0`，
        ///   消费点 = `BattleManager.CanUseActiveAbility`（`BattleManager__CanUseActiveAbility.c:116-132`），
        ///   判据字段 = `CardScript + 0x230` = `canAct`（`ObscuredBool`）⇒ 我们落在 `OpenCommand` 那两道闸。</item>
        /// </list>
        /// </summary>
        public const string DragToTargetTerm = "Battle/Tips/DragToTarget";
        /// <inheritdoc cref="DragToTargetTerm"/>
        public const string UnitNotReadyTerm = "Battle/Tips/UnitNotReady";
        /// <summary>🆕 **2026-10-18（A985⑦）**：`Battle/Tips/HandFull` —— **「有一张牌因为手牌满而进不了手牌」**。
        /// <para>🔴 **原版判据（逐句读过的反编译，`d:/2/tools/decomp_full/`）**：把牌放进手牌的那**三支**
        /// 协程是同一个形状 —— `PlayerHand._AddDrawnCardToHand_d__37`（抽牌）、
        /// `_AddCardNotDrawnToHand_d__39`（造牌）、`_AddCardNotDrawnToHandAtIndex_d__40`：
        /// <c>if (Hand.Count &lt; BattleManager.MaxCardsInHand()) { 进手牌 }</c>
        /// <c>else { CustomDebug.Log(...); CardScript.SetCardStateInCemetery(那张); … }</c>
        /// `else` 支里 <c>GetTermTranslation(0x428A218)</c> → `BattleTipController.ShowHeadsUpMessage(...)`
        /// ⇒ **那一张不进手牌、直接进墓地，并当场弹这句提示**。
        /// ⚠️ 三支里只有 `d__37` 多一条 `PlayerHand.isPlayer`（`+0x28`）闸；本件**不做那半条**
        /// （两侧任一满手都出声，与 `d__39/40` 一致）。</para>
        /// <para>🔴 **值在远端 I2 表**（本地 84 个 bundle 里没有本地化包）⇒ **正式文案拿不到**；
        /// 兜底见 <see cref="HandFullText"/>。</para></summary>
        public const string HandFullTerm = "Battle/Tips/HandFull";

        /// <summary>`Battle/Tips/HandFull` 的**文案**。
        /// <para>🔴 **`Core/Loc.cs` 那张表是权威**（同族两条 `Battle/Tips/{DragToTarget,UnitNotReady}` 就在那儿）
        /// —— 本件**不在那份文件的白名单里** ⇒ 这里只做**转发**：表里有这条键就走它（**两档都对**），
        /// 没有才用下面那两句。📌 那张表补上这一条之后，这段兜底**自动失效、这里一个字都不用改**。</para>
        /// <para>⚠️ **英文列是我们写的、⛔ 不是「原版就是这样」**：原版显示串在远端 I2 表，本地取不到
        /// （铁律 11 例外①）。中文列是我们自拟的直白说法。</para>
        /// <para>📌 **`public` 是给自检当期望值用的**（断言端只能拿到 `HintText` 这个字符串，
        /// 而这条文案**必须与实现同源**，⛔ 别在断言里另抄一份 —— 「两处写同一条规则」）。</para></summary>
        public static string HandFullText()
        {
            if (Loc.HasEntry(HandFullTerm)) return CardText.Term(HandFullTerm);
            return CardText.Zh ? "手牌已满，抽到的牌直接进弃牌堆"
                               : "Hand full - the drawn card goes to the discard pile";
        }

        /// <summary>玩家做了一次**非法操作**（出牌被打回来）→ 让**我方督军**说一句
        /// （原版 `ChatMessage.ICantDoThat` = 枚举 3，链路见 `OnIllegalAction` 的订阅处）。
        ///
        /// 🔴 **闸门（原版就有）**：**正在播语音时不插播** ——
        ///    `BattleTipController.NotifyCantDoAction` 只在 `IsAnyVoiceLinePlaying() == false` 时播。
        ///    没有这道闸，玩家连着乱拖会把语音叠成一团。
        /// ⚠️ `cantdo` **只有督军那一族有**（全池 54 条）⇒ 拿不到就**什么也不播**。
        ///    **不回落**成出场台词 —— 原版取不到词条时落回 `defaultChatSound`，回落成 `line`
        ///    会播「出场台词」，**那不是原版行为**（见 `VoiceLines.ForCantDo` 的注释）。</summary>
        public void SpeakCantDo()
        {
            if (_unitChat == null || !VoiceLines.Ready) return;
            if (_unitChat.IsSpeaking) return;                 // 原版闸门：不打断正在说的
            var w = Ctx != null && Ctx.Players != null ? Ctx.Players[_me].Warlord : null;
            var card = w != null ? w.Card : null;
            if (card == null) return;

            string file, text;
            // 随机源传 null —— 与别处同规矩：**不消耗 `Ctx.Rng`**（否则同一局的随机序列会漂）
            if (!VoiceLines.TryPick(card.Id, VoiceLines.ForCantDo, null, out file, out text)) return;
            var clip = VoiceLines.Clip(file);
            if (clip == null) return;
            _unitChat.Speak(0, card.Id, "CantDo", ArtKey(card), CardDisplayName(card, null), text, clip, file);
        }

        /// <summary>**本回合剩不到 `hurryUpSeconds` 秒**时，我方督军说一句 `hurry`
        /// （原版 `ChatMessage.Bored` = 2；触发链与闸门见 `VoiceLines.ForHurry` 的注释）。
        ///
        /// 🔴 **每回合一次**（原版 `latch_0xb8`，由 `ClockManager.StartTimer` 复位）——
        ///    这里同理：`_hurrySaidThisTurn` 在 `ResetClock()` 里清掉，而 `ResetClock` 的三个调用点
        ///    （开局 `:851` · 转手 `:1323` · 又轮到玩家 `:2877`）**正是「轮到我方」的时刻**。
        /// ⚠️ 已经越过阈值还继续掉（`_clockLeft` 继续变小）时**不重复触发** —— 靠那个 latch。
        /// ⚠️ 闸门：**正在播语音时不插播**（原版 `CanChat`），与 `SpeakCantDo` 同一条。
        /// ⚠️ 失败就**什么都不播、也不静默**：返回 false，调用方看不到就退化成「没接」，
        ///    所以这里把「拿不到词条」明确返回出去（自检会盯着这条路径）。</summary>
        public bool SpeakHurry()
        {
            if (_unitChat == null || !VoiceLines.Ready) return false;
            if (_unitChat.IsSpeaking) return false;               // 原版闸门：不打断正在说的
            var w = Ctx != null && Ctx.Players != null ? Ctx.Players[_me].Warlord : null;
            var card = w != null ? w.Card : null;
            if (card == null) return false;

            string file, text;
            if (!VoiceLines.TryPick(card.Id, VoiceLines.ForHurry, null, out file, out text)) return false;
            var clip = VoiceLines.Clip(file);
            if (clip == null) return false;
            _unitChat.Speak(0, card.Id, "Hurry", ArtKey(card), CardDisplayName(card, null), text, clip, file);
            return true;
        }

        /// <summary>`CardInteraction.OnIllegalAction` 的处理器 —— 只是转一道手，方便自检直接点名调
        /// <see cref="SpeakCantDo"/>（不必真的去模拟一次拖拽）。</summary>
        void OnIllegalAction(CardView card) { SpeakCantDo(); }

        // ==================================================================
        //  回放条（原版 `ReplayButtons`）
        //
        //  🔴 **这四个按钮接什么是我们挑的** —— 原版那个组件在什么模式出现、每个钮干什么，
        //     反编译与场景 JSON 里**都查不到**（`资料/战斗UI_原版对账表.md` §三·〇 明写）。
        //     我们接的是「本局的时间控制」：重开一局 / 暂停 / 继续 / 单步。
        //     —— 为什么不摆四个点了没反应的图：本工程的红线是「不许静默失败」。
        // ==================================================================

        ReplayBar _replayBar;
        bool _replayPaused;

        /// <summary>现在是不是暂停着（暂停 = 事件时间线 / 回合计时 / AI 步进**三处一起停**）</summary>
        public bool ReplayPaused { get { return _replayPaused; } }

        /// <summary>暂停 / 继续。写完顺手把 Play/Pause 那两枚图的显隐换过来（原版就是互斥的两张图）。</summary>
        public void SetReplayPaused(bool on)
        {
            _replayPaused = on;
            if (_replayBar != null) _replayBar.SetPlaying(!on);
        }

        /// <summary>
        /// 单步：把**排在最前**的那条待播事件立刻播掉（暂停时用）。
        /// 返回 false = 没有待播的事件（这时**推进一帧**，让「单步」不至于点了没反应）。
        /// </summary>
        public bool StepReplaySignal()
        {
            if (_timeline.Count == 0) { AdvanceTimeline(1f / 30f); return false; }
            int best = 0;
            for (int i = 1; i < _timeline.Count; i++)
                if (_timeline[i].at < _timeline[best].at) best = i;
            var p = _timeline[best];
            _timeline.RemoveAt(best);
            PlaySignal(p.evt);
            return true;
        }

        /// <summary>回放条的一次点击。返回 true = 这一帧到此为止（和设置面板同一个形状）。
        /// 🔴 A462：**抬起**（原版那四颗 `Replay`/`Play`/`Pause`/`StepPlay` 都是 `EverguildButton`）。</summary>
        bool HandleReplayBar()
        {
            if (_replayBar == null || !ReleasedThisFrame()) return false;
            return ReplayClickAt(WorldPointer());
        }

        /// <summary>
        /// 回放条上点一下 —— **真实输入与自检走的是同一条判定**
        /// （自检拿按钮的世界坐标喂进来，不直接调 `Restart` / `SetReplayPaused`）。
        /// </summary>
        public bool ReplayClickAt(Vector3 world)
        {
            if (_replayBar == null) return false;
            // 🔴 **2026-09-27：整条不显示时**一律不接**点**。原版 `ReplayHud` 平时是 `Holder.SetActive(false)`，
            //    而 `ReplayBar.Hit` 对 `Play`/`Pause` 那一对是**只判矩形、不判 activeSelf**的
            //    （见那边的注释：不这么写「停着的时候点它永远返回 None」）⇒ 不加这道闸，
            //    普通对局里点屏幕那个位置会**看不见地**触发暂停/单步（典型静默）。
            if (!_replayBar.HolderVisible) return false;
            switch (_replayBar.Hit(world))
            {
                case ReplayBar.Btn.Replay: Restart(); return true;
                case ReplayBar.Btn.Play:   SetReplayPaused(false); return true;
                case ReplayBar.Btn.Pause:  SetReplayPaused(true); return true;
                case ReplayBar.Btn.Step:
                    SetReplayPaused(true);          // 单步 = 先停下（和视频编辑器的习惯一致）
                    StepReplaySignal();
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 🆕 2026-09-27：**我们那套「本局时间控制」的键盘入口**。
        /// 🔴 它**不是复刻** —— 原版那条回放条的四个钮是**回放的播放控制**
        /// （`ClickRestartReplay`/`ClickPlayReplay`/`ClickPauseReplay`/`ClickNextStepReplay`），
        /// 而**只有回放局**才显示那一条（`matchType == 0xA0`，判据 → `Battle/ReplayBar.cs` 文件头）。
        /// ⇒ 我们**从回放条上把它们摘下来、挪到键盘**，好让界面与原版一致：
        ///   `Space` = 暂停 / 继续 · `.` = 单步 · `R` = 重开一局（**结算面板上那句承诺的就是它**，另有一处）。
        /// </summary>
        bool HandleTimeControlKeys()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return false;
            if (kb.spaceKey.wasPressedThisFrame)
            {
                SetReplayPaused(!_replayPaused);
                Debug.Log("[Replay] 键盘 `Space` ⇒ " + (_replayPaused ? "**暂停**" : "**继续**")
                        + "（**这是我们自己的时间控制**，不是原版那四颗回放钮）");
                UpdateHud();
                return true;
            }
            if (kb.periodKey.wasPressedThisFrame)
            {
                SetReplayPaused(true);
                bool had = StepReplaySignal();
                Debug.Log("[Replay] 键盘 `.` ⇒ **单步**（" + (had ? "推进了一条待播事件" : "没有待播事件，推进一帧") + "）");
                UpdateHud();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 换下一档对手难度（设置面板那颗钮）。**只影响 AI 的「跳动作」旋钮** ——
        /// 照原版 `TweakAvailableActions`（越简单 = 越低分的动作越容易被随机砍掉）。
        /// 换完把新档写回按钮上，并打一行日志（自检照它断言）。
        /// </summary>
        public void CycleAiDifficulty()
        {
            aiDifficulty = SettingsPanel.NextDifficulty(aiDifficulty);
            if (_settingsPanel != null) _settingsPanel.SetDifficulty(aiDifficulty);
            Debug.Log($"[Battle] 对手难度 → {SettingsPanel.DifficultyName(aiDifficulty)}（{aiDifficulty}）");
        }

        // ==================================================================
        //  墓地 / 战斗日志（原版 `CemeteryLogPanel` + `ShowCemeteryBtn`）
        // ==================================================================

        /// <summary>
        /// 日志面板的开合。和设置面板同一套路：面板开着时**先吃掉点击**（点哪儿都关，含压暗层），
        /// 不穿透到棋盘。
        /// </summary>
        bool HandleBattleLog()
        {
            if (_logPanel != null && _logPanel.Visible)
            {
                // 关面板时那张悬停卡跟着收（原版它是面板的子节点 ⇒ 面板一关就看不见了）
                // 🔴 A462：这一支**保持「按下」**，**别一起改成抬起** —— 原版关这一窗的不是 uGUI 按钮：
                //    场景 `GameObject/shade.json`（父链 `shade <- CemeteryLogPanel`）上的组件是
                //    **`EventTrigger`**，`m_Delegates[].eventID = 2 = PointerDown`（判据 → `PollInputEdges`
                //    上面那一段）。改成抬起 = 与原版不符（点一下要等松手才收）。
                if (ClickedThisFrame())
                {
                    // 🆕 2026-10-18（`A940` 尾账 `Z2`）：**点在某一行上 ⇒ 那是「点第 N 行」那一手**，
                    //    ⛔ **不关面板**（原版那 10 颗 `CemeterySliderUI` 是 uGUI 按钮，画在压暗层**之上**
                    //    ⇒ 点行这件事根本传不到 `shade` 那个 `EventTrigger`）。
                    //    ⚠️ 顺序：**先判行、后关面板** —— 反过来的话点行会当场把面板收掉。
                    if (TryClickLogRow(WorldPointer())) { UpdateHud(); return true; }
                    _logPanel.Hide(); HideLogCard();
                    // 🔴 这一按**已经把面板关掉了** ⇒ 吞掉**同一次按住**的松手沿：底下那颗「日志钮」
                    //    在按下那一刻**根本收不到**（原版那块 `shade` 是全屏最上层的 `EventTrigger`，
                    //    `StandaloneInputModule` 只把 click 发给按下时命中的那一件）⇒
                    //    不吞的话松手那一帧会把它**又打开**（画面闪一下 = 看着像「点了没反应」）。
                    _swallowNextRelease = true;
                }
                return true;
            }
            if (_cemeteryBtn == null) return false;
            // 🆕 2026-10-18（A940）：同那颗聊天钮 —— **藏起来的钮不许还能点**
            //   （教程局里它被 `TutorialSetup` 无条件藏；不判 activeSelf 的话**看不见还点得开**）。
            if (!_cemeteryBtn.gameObject.activeSelf) return false;
            // 🆕 **2026-10-19（A964②）**：命中区 = 原版 `rect` 64.478×64.170 按 `m_RaycastPadding`
            //   (−8,−8,−8,−8) 四边外扩 ⇒ **80.478×80.170**（原来 `Contains` 量的是画出来的 64.5²）。
            if (!CemeteryBtnHit(WorldPointer())) return false;
            // 🔴 A462：**抬起**（原版 `GameObject/ShowCemeteryBtn.json` 上 `EverguildButton` 的
            //    `m_OnClick.m_Calls[0].m_MethodName = "ShowCemeteryLogBtn"`；那条全反编译里零调用点
            //    —— 因为它只活在序列化事件里）。
            if (ReleasedThisFrame()) ShowBattleLog();
            return true;
        }

        /// <summary>自检用：打开日志面板（先刷新内容再显示）。
        /// **批处理下没有鼠标**，真实输入那条路（`HandleBattleLog`）走不通，得能直接调。</summary>
        public void ShowBattleLog()
        {
            // ⚠️ **先显示、再灌内容** —— TMP 在未激活的对象上建不出字形，
            //    第一版顺序反了，打开后整块板一个字的都没有（面板内部 `Show` 也会重刷一次）
            if (_logPanel != null) _logPanel.Show();
            RefreshBattleLog();
        }

        // ==================================================================
        //  开局换牌（原版 `Mulligan` 子树 / `MulliganManager` / `PlayerHand.FinishMulligan`）
        //
        //  规则书 :46「换牌（Mulligan）| 可弃回任意起手牌后重洗补抽」。
        //  **原版流程 ↔ 我们的对应**（2026-09-17 照反编译逐条对过，出处见 `MulliganPanel.cs` 头部）：
        //    `_SetupMulliganPhase` / `MulliganManager.ActivateMulligan`  → 我们 `OpenMulligan()`
        //        （含 `Shade.SwitchShade` 压暗；原版那层的颜色/透明度没查到，**我们这层是我们挑的**）
        //    `ProcessMulliganDone`（同帧失活按钮组 + 销毁每张卡的 `MulliganFrame`）→ `_mulligan.Close()`
        //    `BattleManager.ClickMulliganDone`（记玩家换牌；**单机再让 `AI.GetAiMulliganCards` 决定对面**）
        //        → `RuleCore.Mulligan`。✅ **2026-09-17：对面那半照原版规则做了**（`SimpleAI.AiMulliganIndices`，
        //        规则 = `mulliganOption==whispersOfChaos(30)` 保留、否则 `manaCost > 4` 换掉，
        //        出处 `资料/AI_原版反编译_0917.md` §二）—— 原来这里写的是「我们一张都不换」。
        //    `PlayerHand.FinishMulligan`（逐张补牌 + 淡入 + 等它播完）→ `RefreshAll()` 里的发牌/重排补间
        //    `ShuffleDeck`（原版在 final phase 才洗）→ 我们合进了 `RuleCore.Mulligan`（弃牌回库 → 重洗 → 补抽）
        //    `StartBattlePhase` → `RuleCore.BeginTurn`（回合才真正开始、这时才发能量）
        //  ⚠️ **我们省掉的（全是表现层，且多数拿不到参数）**：等对手那一行（`SetWaitingForEnemy` /
        //    `mulliganWaitText`）· `BlockingOverlay` 转圈 · `FinishMulliganFinalPhase` 里那两次
        //    `WaitForSeconds(globalVars+0xa4 / +0x20)` —— 那两个数在 `VarsGlobal` 里，**本地拿不到**
        //    （在 `项目任务.md`「⛔ 永久拿不到」那一栏）。
        //    ⇒ 我们是一口气同步做完的：**语义一致、节奏不同**，别把它读成「原版也这么顺」。
        //
        //  ⚠️ `mulliganEnabled` 默认**关**：批处理自检里那一大堆用例都是「Begin 之后直接就是回合 1」
        //    （能量 2、手牌 4），开了换牌就得每个用例先换一副牌才能验 —— 和 `animateFeel`/`animateRelayout`
        //    同一条规矩：**存场景那一路打开**，自检要验的时候自己显式打开。
        // ==================================================================

        /// <summary>开局要不要进换牌阶段（原版单机是进的；我们的自检默认跳过）</summary>
        public bool mulliganEnabled = false;

        MulliganPanel _mulligan;

        /// <summary>换牌倒计时总秒数。原版字段 = **`VarsGlobal.mulliganTimeLimit`**（float）——
        /// `BattleManager.&lt;MulliganCountdown>` 从 `globalVars + 0x28` 取（`_MulliganCountdown_d__347__MoveNext.c:38`），
        /// 按 `VarsGlobal.cs` 的字段声明顺序推，`+0x28` 正好落在它上面。
        /// ✅ **值 = 25.0 秒（原版）**：`VarsGlobal` 资产（`m_Name = GlobalVariables`，PathID 451）在
        /// `Warpforge_Data/sharedassets0.assets`，**本地两份安装的副本逐字节一致**；
        /// 读法见 `工具/read_varsglobal.py`（字段名取签名桩的声明顺序，值取原始字节）。
        /// ⚠️ 以前这里写「数值拿不到、默认 30 是我们挑的」—— **那是因为抽取管线漏了这个资产**
        /// （它所在的 .assets 没有 type tree，UnityPy 读不出字段名），不是它不存在。
        /// ⚠️ 另一条：原版**离线练习局（matchType 0x32）整段跳过倒计时**（同一个协程开头 `return`）。
        ///   我们这局是单机自建，按用户 2026-09-17 的要求**照可玩形态做出来**（默认开）。</summary>
        public float mulliganSeconds = 25f;

        /// <summary>换牌还剩多少秒（走表用）</summary>
        float _mulliganLeft;
        /// <summary>已经写到按钮上的秒数（避免每帧重复 SetText）</summary>
        int _mulliganShownSec = -1;

        /// <summary>换牌倒计时走表。**照原版 `BattleManager._MulliganCountdown` 那支协程**：
        ///   · 逐秒 `-1`（原版门控：`UIstate == 0xb` 且非「等重连」；我们没有重连概念，面板开着就走）
        ///   · **`&lt; 10` 秒**才把剩余秒数写到「完成换牌」那颗钮上（`MulliganManager.SetMulliganTimer`）
        ///   · **`&lt; 1` 秒**自动完成 —— 原版调 `MulliganManager.ProcessMulliganDone()`，**等价于玩家点完成**
        /// ⚠️ 格式串**已查到**（2026-09-17）：`SetMulliganTimer` = `String.Concat("0:0", 秒数)`
        /// ⇒ 最后十秒按钮上写 **`0:09`…`0:01`**（前缀写死 `0:0`，这也正是阈值取 `&lt;10` 的原因）。
        /// 返回 true = 这一帧到此为止（和玩家点「完成换牌」走同样的收尾，别让同帧接着跑回合逻辑）。</summary>
        bool TickMulligan(float dt)
        {
            if (!InMulligan || _mulligan == null || !_mulligan.Visible) return false;

            _mulliganLeft -= dt;
            int s = Mathf.CeilToInt(Mathf.Max(0f, _mulliganLeft));

            if (s < 1)
            {
                Watch.Mark("换牌：倒计时到 0 自动完成");
                _mulligan.SetDoneText(MulliganPanel.DoneLabel);     // 先把按钮的字复原
                _mulligan.HandleClick(_mulligan.DoneWorldPos);      // = 玩家点「完成换牌」同一条路
                return true;
            }

            if (s < 10 && s != _mulliganShownSec)
            {
                _mulliganShownSec = s;
                // 🔴 **显示格式是原版的**：`SetMulliganTimer` = `String.Concat("0:0", 秒数)`
                //    —— 那个字面量 `StringLiteral_20746` 就是 **"0:0"**（2026-09-17 用
                //    `Il2CppDumper` 的 `stringliteral.json` + `script.json` 的 `ScriptString[20745]` 查到）。
                //    ⚠️ **这也解释了阈值为什么是 `< 10`** —— 前缀写死 `0:0`，秒数一上两位数就串成 "0:012"。
                //    ⇒ 最后十秒按钮上是 **0:09 … 0:01**。
                _mulligan.SetDoneText("0:0" + s);
            }
            return false;
        }

        /// <summary>自检用：推一次换牌倒计时（`dt` 给 1 s 就是「过了一秒」）</summary>
        public bool TickMulliganForTest(float dt) { return TickMulligan(dt); }
        /// <summary>自检用：换牌还剩多少秒</summary>
        public float MulliganSecondsLeft { get { return _mulliganLeft; } }

        /// <summary>自检用：换牌面板</summary>
        public MulliganPanel Mulligan { get { return _mulligan; } }
        /// <summary>自检用：现在是不是在换牌阶段</summary>
        public bool InMulligan { get { return Ctx != null && Ctx.MulliganOpen; } }
        /// <summary>自检用：中上那行回合标签显示着没有（换牌阶段应当藏着）</summary>
        public bool TurnLabelVisible { get { return _turnLabel != null && _turnLabel.gameObject.activeSelf; } }
        /// <summary>自检用：等待提示（原版 `WaitText`）</summary>
        public WaitBanner Wait { get { return _waitBanner; } }

        /// <summary>处理换牌阶段的一次点击。返回 true = 这次点击被换牌吃掉了</summary>
        bool HandleMulligan()
        {
            if (_mulligan == null || !_mulligan.Visible) return false;
            // ⚠️ **键盘 = 完成换牌**：`Enter` / `空格`。
            //    🔴 **2026-09-17 更正**：原来这里写「**原版只有按钮**（`MulliganManager.ClickMulliganDone`），
            //    这一条是我们加的保险」—— **记错了**。原版**本来就有**这条键盘路径：
            //    `MulliganManager__Update.c`：`Input.GetKeyDown(0x20=Space)` / `GetKeyDown(0xd=Enter)`
            //    → 门控 `buttonsGroup.activeSelf == true` → `ProcessMulliganDone`（= 点「完成换牌」）。
            //    ⇒ 我们这条**不是独创**，与原版一致；差别只在门控（原版看按钮组在不在，我们看面板可不可见）。
            //    （原来那句的错因：只看了 `ClickMulliganDone` 那个按钮处理器，没看 `Update`。）
            if (Keyboard.current != null &&
                (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
            {
                _mulligan.HandleClick(_mulligan.DoneWorldPos);
                return true;
            }
            // 🔴 A462：**抬起**（原版三颗换牌钮都是 uGUI `Button`：每张卡的 `MulliganFrame.changeCardButton`
            //    = `EverguildButton` · `MulliganContinueButton` / `HideMulliganButton` = `Button`，
            //    `m_OnClick → MulliganManager.ClickMulliganDone` / `ToggleMulliganVisibility`）。
            if (ReleasedThisFrame()) _mulligan.HandleClick(WorldPointer());
            return true;      // 换牌阶段：这一帧的输入全归它，不往下传
        }

        void OpenMulligan()
        {
            if (_mulligan == null) return;

            // 对手换牌：**照原版 `AI.GetAiMulliganCards` 的规则**（2026-09-17 反编译解开 ——
            // 正本 `资料/AI_原版反编译_0917.md` §二）：`mulliganOption == whispersOfChaos(30)` 保留、
            // 否则 **`manaCost > 4` 换掉**、其余保留。判据**只此一处**（`SimpleAI.AiMulliganIndices`），
            // 驱动层不重写第二份。
            //
            // 🔴 **2026-09-17 更正**：这一段原来写的是「对面那半**我们一张都不换**」，理由是
            // 「`AI` 类的方法体一个都没反编译 ⇒ 规则无从照抄（有据的偏离）」。
            // **那个理由已经不成立了** —— `AI` 整类 41 个方法体当天全部反编译出来，
            // `AI__GetAiMulliganCards.c` 逐字可读。同时 `BattleManager__ClickMulliganDone.c` 的单机分支
            // 调 `AI.GetAiMulliganCards(hand)` 这一条**仍然成立**（玩家那份存 `matchData+0x80`、
            // 对面存 `+0x88`）⇒ 原版单机局确实会问 AI。
            // ⚠️ **我们唯一对不上的一支**：原版那条 `whispersOfChaos` 分支在我们卡池里**没有对应物**
            //    （我们的 `type` 只有 unit / tactic / hero / defence）—— 不是被砍掉，是无对应物。
            // 🆕 2026-09-26（N4）：**联机局不在这里算对面** —— 换牌由主机定序（`RuleCore.Mulligan` 会掷
            //    `ctx.Rng`，两端必须按同一顺序调）；本机提交后等 `mulligan.sync` 落地，见 `OnMulliganDone`。
            if (!_noAiMulligan)
            {
                // 🆕 2026-09-27（录像）：**AI 的换牌也要记**（它掷的是 `ctx.Rng`，重放时必须按同一顺序来）
                var aiMarks = SimpleAI.AiMulliganIndices(Ctx, 1 - _me);
                RuleCore.Mulligan(Ctx, 1 - _me, aiMarks);
                RecRaw(NetActionKind.Mulligan, 1 - _me, aiMarks != null ? aiMarks.ToArray() : new int[0]);
            }
            _mulligan.OnDone = OnMulliganDone;
            _mulligan.SetDoneText(MulliganPanel.DoneLabel);   // 开面板时按钮字复原（上一局可能停在秒数上）
            // 🆕 2026-09-26：**把「你先手 / 你后手」写进面板** —— 原版唯一一处「先手/后手」的表现
            //   （`MulliganManager.ActivateMulligan` 按 `playerGoesFirst` 二选一词条；原版**没有硬币动画**）。
            //   判据 → `资料/加时与冲突模式_原版规格.md` §2.8。
            _mulligan.SetTurnText(Ctx != null && Ctx.FirstSeat != _me);
            _mulligan.Open(new List<CardView>(_handViews));
            _mulliganLeft = mulliganSeconds;                  // 倒计时从总秒数起（原版 `globalVars+0x28`）
            _mulliganShownSec = -1;
            interaction.enabled = false;      // 换牌阶段不让拖牌/悬停/轻点（卡要待在原地，别动来动去）
            SetHint("换牌中：点牌上的「换」标记要替换的牌，然后点「完成换牌」");
        }

        void OnMulliganDone(List<int> marks)
        {
            // 🆕 2026-09-26（N4）：**联机局：本地先不落地** —— 换牌由主机定序后下发
            //   （`RuleCore.Mulligan` 掷 `ctx.Rng`，两端顺序必须一致）。本机只提交、等同步。
            if (_net != null)
            {
                // 🔴 **2026-10-18（双语③ 波 1·P4）**：`等待对手…` 原来写死中文 ⇒ **英文档露中文**。
                //   复用**已在表**的 `Battle/Mulligan/WaitEnemy`（EN `Waiting for enemy` / ZH「等待对手」，
                //   原版词条，同 `Battle/WaitBanner` 里 `Loc.T(WaitTerm)` 那一处）—— 中文档**逐字不变**（末那个 `…`
                //   是**我们加的**，留在外面；⛔ 别把它并进词条）。
                if (_mulligan != null) _mulligan.SetDoneText(Loc.T("Battle/Mulligan/WaitEnemy") + "…");
                _net.OnLocalMulligan(marks != null ? marks.ToArray() : new int[0]);
                SetHint("换牌已提交，等主机定序…");
                return;
            }
            int n = RuleCore.Mulligan(Ctx, _me, marks);
            if (n < 0) Debug.LogWarning("[Battle] 换牌被拒（不在换牌阶段）—— 这不该发生");
            // 🆕 2026-09-27（录像）：玩家的换牌 + 「换牌结束」各记一条（顺序照 `OnMulliganDone` 的实际顺序）
            RecRaw(NetActionKind.Mulligan, _me, marks != null ? marks.ToArray() : new int[0]);
            RuleCore.EndMulligan(Ctx);
            RecRaw(NetActionKind.MulliganDone, _me);
            _mulligan.Close();
            interaction.enabled = true;

            // 🆕 2026-09-29（§25）：**换牌之后、开局之前那一段** —— 选进攻卡（先手）/ 选防御卡（后手）。
            //   原版 `FinishMulliganFirstPhase` → `SetupEnviromentalEffectPhase` → `ChooseCardMenu.Setup(...)`；
            //   面板开着就先别开打，选完在 `OnOffensiveDone` 里接着走。
            if (BeginOffensivePhaseIfAny()) return;

            BeginBattleAfterSetup();
            SetHint(n > 0 ? $"换掉了 {n} 张" : "");
        }

        /// <summary>换牌之后那一段的收尾（原版 `FinishMulliganFinalPhase` → `StartBattlePhase`）——
        /// 拆出来是因为中间可能插「选进攻卡」那个面板（选完才走到这儿）。</summary>
        void BeginBattleAfterSetup()
        {
            RuleCore.BeginTurn(Ctx);          // 换完才真正开打（原版 `StartBattlePhase`）
            SyncTutorialTurn();               // 🆕 A938：回合变了 ⇒ 脚本指针归零（原版 `UpdateTurn`）
            // 🆕 2026-10-12（A175）：**换牌结束那一刻** —— 原版 `CombatAutoZoom.Initialize()` 就在这里
            //   （`BattleManager._FinishMulliganFinalPhase_d__351__MoveNext.c:232`；那一段同一个位置还叫了
            //   `CombatCameraZoom.Initialize`，我们那半没做、见 `Battle/CombatAutoZoom.cs` 文件头 A）。
            InitializeAutoZoom();
            ResetClock();
            RefreshAll();
            // 🆕 2026-09-18：**开局独白**（原版 `BattleManager.StartBattlePhase` 末了起的
            // `ShowHeroesIntroMessage` 那条协程）。放在 `BeginTurn` 之后 —— 原版就是在这个位置。
            StartIntroMonologue();
            // 🆕 2026-09-29（§25）：进攻卡**每场只生效一次**，就在这儿（原版
            // `_ApplyOffensiveAndDefensiveEffects` 的唯一调用点 = `FinishMulliganFinalPhase`，
            // **不在回合结算里** —— 别写成「每回合」）。
            // 🆕 2026-09-30：改成走 `BeginOffensiveRevealOrApply()` —— 有进攻卡就先演 reveal
            // （原版那条协程的 state 0~3），空卡直接生效；演完由 `AdvanceTimeline` 里的
            // `TickOffensiveReveal` 接着走到 `ApplyOffensiveEnvOnce()`。
            BeginOffensiveRevealOrApply();
        }

        // ==================================================================
        //  🆕 2026-09-29（§25）：进攻卡 / 防御卡那一段
        // ==================================================================
        //  判据（逐句读过，正本 → `资料/加时与冲突模式_原版规格.md` 的进攻卡那一节 + 判据文件 §三 第 25 条）：
        //   · 时机：**换牌之后、战斗开始之前**（`FinishMulliganFirstPhase` → `SetupEnviromentalEffectPhase`）
        //   · 先手方选**进攻卡**，列表 **`[空卡, 进攻1, 进攻2, 进攻3]`**（空卡固定在下标 0）
        //   · 后手方选**防御卡**（3 张、没有空卡）· 两台各弹各的
        //   · AI 那侧 = `AI.GetAiEnvEffectCard`：**均匀随机**，**可能抽到空卡**
        //   · 超时**不是强制的**（计时器只把按钮字改成 `0:SS`，<3 时才替玩家按完成）
        /// 🔴 **2026-10-19（`A1110`）**：这个闩也**必须每局复位**（`Begin` 里那段「本局一次的闩」）——
        ///   它原来是 `BeginOffensivePhaseIfAny` 的**第一道闸**、且**永不复位** ⇒ 同一台 driver 的
        ///   **第 2 局连面板与 AI 那一侧的随机选卡都不跑**，于是 `Ctx.OffensiveChosen` 恒 `false`
        ///   ⇒ 后面 `BeginOffensiveRevealOrApply` 也跟着整段不跑（**环境永远不换**，静默）。
        bool _offensivePhaseDone;

        /// <summary>要不要跑「选进攻卡」那一段（原版 `SetupEnviromentalEffectPhase`）。
        /// 默认 **true**（真机走这条路）；**批处理自检里关掉** —— 与 `mulliganEnabled` 同一条规矩：
        /// 那一段会**弹面板并停住**（等玩家点「继续」），自检要的是「回合状态当场精确」
        /// （2026-09-29 实测：不停住的话 `BeginTurn` 不跑 ⇒ 「这时才发能量 / 抽第 1 张」两条会假红）。
        /// 自检要验这一段时用两个钩子：`RuleCore.ChooseOffensiveCard` + `SimulateApplyOffensiveEnv`。</summary>
        public bool offensivePhaseEnabled = true;

        /// <summary>要不要开「选进攻卡」那个面板。返回 true = 面板开了（流程在 `OnOffensiveDone` 里接着走）。</summary>
        bool BeginOffensivePhaseIfAny()
        {
            if (_offensivePhaseDone) return false;
            // 🔴 **2026-10-09（`A1128`）订正**：闩原来排在闸**之前** ⇒ 自检里「关掉这一段」的调用
            //   **照样把闩闩上**（同族泄漏的温床）。改成**闸之后**：关掉就**不闩**。
            if (!offensivePhaseEnabled) return false;    // 自检关掉它（见那个字段的注释）
            _offensivePhaseDone = true;                  // 一局只来一次
            if (!OffensiveCards.Available) return false;

            // 🆕 2026-09-30：**联机局不再整段跳过** —— 原来这里 `return false` 并出声（那时同步链还没做）。
            //   现在走 `NetActionKind.OffensivePick` / `DefensivePick` 那两条（**我们自己设计的**：
            //   原版两台各弹各的面板、**没有这条同步包** ⇒ 不同步的话两端会各切各的环境、
            //   后手那张防御卡也只有一端有，指纹迟早不一致）。
            //   ⚠️ **联机局里不替对面「AI 随机挑」** —— 对面是真人，他自己会选、选完发过来。
            if (_net == null)
            {
                // ① **AI 那一侧先定**（原版 `AI.GetAiEnvEffectCard`：均匀随机，可能抽到空卡）
                if (Ctx.FirstSeat != _me) PickOffensiveForAi(Ctx.FirstSeat);
                if (Ctx.SecondSeat != _me) PickDefensiveForAi(Ctx.SecondSeat);
            }

            // 🔴 **两家共用的那份列表 = 【后手那一方】的阵营**（2026-10-01 查实，原来我们用的是 `_myFaction`）
            //   原版 `GetEnvEffectCards` 的 army **恒取后手那一边**：
            //     `playerGoesFirst ? enemyManager(+0xc8) : playerManager(+0xc0)` → `heroCard→rawCard→army`
            //   （`BattleManager__GetEnvEffectCards.c:38-48`；`+0x247 = playerGoesFirst` 由
            //     `BattleManager__get_playerGoesFirst.c` 那三行坐实；另有两处独立印证见报告）
            //   ⇒ **本机是先手时，面板列的是【对手阵营】的 3 张进攻卡 + 对手的空卡**。
            //   ⚠️ 判据全文（含三处印证与两个标题词条的 `DAT_` 实解）→
            //      `资料/普查产出_0930/进攻防御卡_面板语义.md`。
            string poolFaction = FactionOf(Ctx.SecondSeat);

            // ② 本机是**先手**：弹「选进攻卡」（`[空卡, 3 张]`，标题词条 `offensive`）
            if (Ctx.FirstSeat == _me)
            {
                var list = OffensiveCards.Choices(poolFaction);
                if (list.Count < 2) return false;         // 原版：列表 <2 张时**不弹菜单**，直接取 list[0]
                var views = new List<CardView>();
                for (int i = 0; i < list.Count; i++)
                    views.Add(CardView.Create(_choosePanel.transform, OffensiveCardData(list[i], poolFaction),
                                              "Offensive_" + i));
                _choosePanel.OnDone = OnOffensiveDone;
                // `uniqueId = "offensive"` —— 原版就是拿它拼词条 key（`Battle/ChooseCard/Instructions-offensive`）。
                // ⚠️ 文案仍是**我们的**（原版词条在远端本地化表，本地一张都没有 ⇒ 查表必然落空、走这里的兜底）。
                _choosePanel.Open(views, "选择进攻卡", "offensive");
                SetHint("选择进攻卡（先手）—— 选完点「继续」");
                return true;
            }

            // ③ 本机是**后手**：弹「**选防御卡**」（该阵营 3 张、**没有空卡**，标题词条 `defensive`）
            //   ✅ 2026-10-01 补上（原来只出声跳过）。判据 → `资料/普查产出_0930/进攻防御卡_面板语义.md` §【1】【3】。
            //   ⚠️ 列表 = **本阵营**的防御卡（本机就是后手方那一边 ⇒ 与「恒取后手方阵营」是同一件事）。
            //   ⚠️ 卡面用 `ToCardData(CardDef,…)` —— 防御卡**本来就在我们卡池里**（39 张，照片/数值/效果文字齐全）。
            var defs = DefensiveChoices(_myFaction);
            if (defs.Count == 0)
            {
                Debug.LogWarning("[Battle] 本机是后手，但**卡池里没有这一阵营的防御卡** ⇒ 不弹面板（如实出声，不静默）");
                return false;
            }
            {
                var views = new List<CardView>();
                for (int i = 0; i < defs.Count; i++)
                    views.Add(CardView.Create(_choosePanel.transform, ToCardData(defs[i], _myFaction),
                                              "Defensive_" + i));
                _choosePanel.OnDone = OnDefensiveDone;
                _choosePanel.Open(views, "选择防御卡", "defensive");
                SetHint("选择防御卡（后手）—— 选完点「继续」");
                return true;
            }
        }

        /// <summary>自检用：进攻卡面板用的是**哪一方**的阵营（原版 `GetEnvEffectCards` 恒取**后手那一边**，
        /// 判据 → `资料/普查产出_0930/进攻防御卡_面板语义.md`）。</summary>
        public string OffensivePoolFaction { get { return Ctx != null ? FactionOf(Ctx.SecondSeat) : null; } }
        /// <summary>自检用：本机自己的阵营（用来验「池子**不是**自己的」）。</summary>
        public string MyFactionForTest { get { return _myFaction; } }
        /// <summary>自检用：某阵营的**防御卡那 3 张**（原版 `EnviromentalEffectCardsSO.defensiveCards`）。</summary>
        public List<CardDef> DefensiveChoicesForTest(string faction) { return DefensiveChoices(faction); }

        /// <summary>本阵营的**防御卡那 3 张**（原版 `EnviromentalEffectCardsSO.defensiveCards` 那一族）。
        /// 🔴 **不另建数据表** —— 判据是「`数据/游戏数据/defensive_cards_39.json` 的 13×3 与我们卡池里
        /// 同阵营的 `defence` 卡**逐张对上 38/39**，唯一差异是名字写法（表里 `Rusted Vent` / 池里
        /// `Rusted Vents`，同一张）」⇒ 直接用卡池，少一处会漂的第二份。</summary>
        List<CardDef> DefensiveChoices(string faction)
        {
            var outp = new List<CardDef>();
            if (_pool == null) return outp;
            foreach (var c in CardDatabase.OfFaction(_pool, faction))
                if (c != null && c.Type == "defence") outp.Add(c);
            return outp;
        }

        /// <summary>进攻卡 → 一张「能画出来」的卡面数据。
        /// ⚠️ **只有插画与卡名候选**：进攻卡**不在我们的卡池里**（`cards_engine.json` 一张都没有），
        ///    费用/攻血/效果文字**原版那份在远端 CCD**（`cardName` 本地 0/39）⇒ 画面上是「有画、有名」的空壳，
        ///    如实标注（`OffensiveCards.Card.nameFrom` 记着那个名字是怎么推出来的）。</summary>
        static CardData OffensiveCardData(OffensiveCards.Card c, string faction)
        {
            return new CardData
            {
                id = "offensive:" + faction + ":" + c.idx,
                title = string.IsNullOrEmpty(c.name) ? "（卡名未定）" : c.name,
                cost = 0, melee = 0, ranged = 0, health = 0, armor = 0,
                keywords = "", isUnit = false,
                frame = new Color(0.55f, 0.55f, 0.62f),
                faction = faction, rarity = "common", type = "tactic",
                artOverride = CardArt.OffensiveFace(faction, c.idx),
            };
        }

        void OnOffensiveDone(List<int> picked)
        {
            var list = OffensiveCards.Choices(FactionOf(Ctx.SecondSeat));   // 🔴 与面板同一份（后手方阵营）
            int i = (picked != null && picked.Count > 0) ? picked[0] : 0;   // 原版：没选就按空卡（下标 0）
            if (i < 0 || i >= list.Count) i = 0;
            var c = list[i];
            RuleCore.ChooseOffensiveCard(Ctx, _me, c.idx, EnvSOFor(c, FactionOf(Ctx.SecondSeat)));
            LogAndSendEnvPick(NetActionKind.OffensivePick, _me, c.idx, EnvSOFor(c, FactionOf(Ctx.SecondSeat)), null);
            _choosePanel.Close();
            BeginBattleAfterSetup();
            SetHint("");
        }

        /// <summary>🆕 2026-09-30：进攻卡 / 防御卡那一选 —— **一次做完两件记账**：
        /// ① 记进**本地录像**（`RecRaw`）② **联机局**发给对面（`OnLocalRawAction`）。
        /// 🔴 为什么合成一个函数：漏掉任何一半都是**静默**的（回放走样 / 两端不一致），
        /// 而这两件事的判据完全一样 —— 一处写、一处改。
        /// ⚠️ `actorSeat != _me`（AI 那一侧）时**不发网络包** —— 那是本机替 AI 算的，
        /// 联机局里根本不走这条（对面是真人，见 `BeginOffensivePhaseIfAny` 的守卫）。</summary>
        void LogAndSendEnvPick(int kind, int actorSeat, int slot, string envSO, string defId)
        {
            RecRaw(kind, actorSeat, null, slot, envSO, defId);
            if (actorSeat == _me && _net != null)
                _net.OnLocalRawAction(new MsgAction { kind = kind, envSlot = slot, envSO = envSO, defId = defId });
        }

        /// <summary>后手方选完防御卡 → 记下并**把那张换进手牌**（原版 `ClickChosenCardDone` 之后那条链：
        /// 防御卡那一路的落点是「**后手方手牌**」）。
        /// ⚠️ 我们开战时已经由 `DeckBuilder` 随机补了一张（= 原版 `AddGoesSecondCardToDeck` 那条路）
        /// ⇒ `RuleCore.SetDefensiveCard` 会**先把原来那张摘掉再加新的**（不然手里会有两张）。</summary>
        void OnDefensiveDone(List<int> picked)
        {
            var defs = DefensiveChoices(_myFaction);
            int i = (picked != null && picked.Count > 0) ? picked[0] : 0;   // 原版：没选就取第 0 张
            if (i < 0 || i >= defs.Count) i = 0;
            RuleCore.ChooseDefensiveCard(Ctx, _me, i);
            if (defs.Count > 0) RuleCore.SetDefensiveCard(Ctx, _me, defs[i]);
            LogAndSendEnvPick(NetActionKind.DefensivePick, _me, i,
                              null, defs.Count > 0 ? defs[i].Id : null);
            _choosePanel.Close();
            BeginBattleAfterSetup();
            SetHint("");
        }

        /// <summary>AI 那一侧的进攻卡：**均匀随机**（原版 `AI.GetAiEnvEffectCard`，`Random` 那一路可能抽到空卡）。
        /// ⚠️ 列表与面板**同一份**（后手方阵营 —— 见 `BeginOffensivePhaseIfAny` 里那段判据）。</summary>
        void PickOffensiveForAi(int seat)
        {
            string f = FactionOf(Ctx.SecondSeat);
            var list = OffensiveCards.Choices(f);
            if (list.Count == 0) return;
            int i = Ctx.Rng.Next(list.Count);
            var c = list[i];
            RuleCore.ChooseOffensiveCard(Ctx, seat, c.idx, EnvSOFor(c, f));
            LogAndSendEnvPick(NetActionKind.OffensivePick, seat, c.idx, EnvSOFor(c, f), null);
        }

        /// <summary>AI 那一侧的防御卡：**均匀随机取一张**（原版 `AI.GetAiEnvEffectCard` 那条随机路；
        /// `GetEnvEffectCards` 的 arm 就是后手方阵营 ⇒ AI 是后手时就是它自己的阵营）。
        /// ✅ 2026-10-01：原来这里只记「未选」并出声（那时我们**没有防御卡的数据**）；
        ///   现在数据齐了（= 卡池里该阵营的 `defence` 卡，见 `DefensiveChoices`）⇒ 照原版随机取一张。</summary>
        void PickDefensiveForAi(int seat)
        {
            var defs = DefensiveChoices(FactionOf(seat));
            if (defs.Count == 0)
            {
                Debug.LogWarning($"[Battle] AI（P{seat + 1}）是后手，但卡池里没有这一阵营的防御卡 ⇒ 记「未选」");
                RuleCore.ChooseDefensiveCard(Ctx, seat, -1);
                LogAndSendEnvPick(NetActionKind.DefensivePick, seat, -1, null, null);
                return;
            }
            int i = Ctx.Rng.Next(defs.Count);
            RuleCore.ChooseDefensiveCard(Ctx, seat, i);
            RuleCore.SetDefensiveCard(Ctx, seat, defs[i]);
            LogAndSendEnvPick(NetActionKind.DefensivePick, seat, i, null, defs[i].Id);
        }

        string FactionOf(int seat) { return seat == _me ? _myFaction : _foeFaction; }

        /// <summary>那张进攻卡对应的**环境 SO 名**。空卡（`idx &lt; 0`）那一路用**本阵营的 default**
        /// （原版 `GetEnviromentalEffect`：空卡命中的就是 `defaultEnviromentalEffectVFX`）。</summary>
        static string EnvSOFor(OffensiveCards.Card c, string faction)
        {
            if (c != null && !OffensiveCards.IsEmpty(c) && !string.IsNullOrEmpty(c.envSO)) return c.envSO;
            var a = OffensiveCards.For(faction);
            return a != null ? a.defaultEnvSO : "";
        }

        /// <summary>把选定的进攻卡**生效一次**（原版 `_ApplyOffensiveAndDefensiveEffects`）。
        /// 🔴 **2026-10-01 订正那三个时长**（原来记的「2 s / 1 s / **4 s**」里第三个是错的）——
        ///   逐段读过 `BattleManager._ApplyOffensiveAndDefensiveEffects_d__337__MoveNext.c` +
        ///   `BattleManager__.ctor.c:190-210`（那几个 `WaitForSeconds` 字段就是在那儿建的），
        ///   再用 `工具/read_literal.py` 把常量**实读**出来：
        ///   | 步骤 | 字段 / 实参 | 常量 | 实读 |
        ///   |---|---|---|---|
        ///   | `DisplayRevealedCard(...)` 之后等 | `manager+0x488` | `DAT_1834b2bbc` | **2.0** |
        ///   | `DestroyCardOffensive(卡, t)` 的 t | 实参 | `DAT_1834b2bb8` | **1.0** |
        ///   | 那之后补等（`fVar2 ×`） | `DAT_1834b2bb4` | **0.5** |
        ///   | `ApplyEnvEffect` 之后（**非空卡**）再等 | `manager+0x4a0` | `DAT_1834b2e8c` | **3.0**（不是 4） |
        ///   ⚠️ **这四步我们【还没做】**（现在是一步到位、直接 `ApplyEnvEffect`）；记在
        ///   `项目任务.md` §三 第 25 条的「reveal 动画」那一项里。
        /// ⚠️ **战场那一半（换雾/环境光/环境 prefab）归【战场场景线】**（`项目任务.md` §三 第 30 条）
        /// —— 这里只把「该切到哪条环境 SO」算出来交给它，自己**不碰战场**。 </summary>
        void ApplyOffensiveEnvOnce()
        {
            if (!Ctx.OffensiveChosen) return;
            string so = Ctx.OffensiveEnvSO;
            // 🆕 那颗钮的显隐（原版 `d__337:146-155`：判据 = 选定卡 id ≠ 空卡 id ⇒ 才 `SetActive(true)`）
            //   🔴 2026-09-30：判据抽成 `ApplyOffensiveButtonVisibility()`（**只此一处判**）——
            //     因为 reveal 那条路要在 **2.0 s 之后**单独调它一次（原版顺序如此，见那段注释）。
            bool useCard = ApplyOffensiveButtonVisibility();
            if (!useCard)
            {
                Debug.Log("[Battle] 进攻卡：这一局选的是**不使用进攻卡**（`Normal Conditions` 那一张）"
                        + " ⇒ HUD 那颗钮**不出现**（原版 `isEmptyOffensiveCard` 那条判据），环境也不动");
                return;
            }
            // 🆕 2026-09-30：**战场侧接上了**（§三 第 30 条 · 4 环境）。
            // 判据 = 原版 `ApplyEnvironment(so, instant:false)`：同一条不重播 · 补间雾与环境光混合 ·
            //        `scenarioObjects` 有值就 `Instantiate(prefab, Camera.main.position/rotation)`。
            // 🆕 2026-09-30 晚：那族 `IScenarioEnvironmentBlendeable` **已经接了**（`EnvironmentApplier` 里
            //    「实例侧 direction=true」＋「场景侧 direction=`SO.defaultScenarioObjectsState`」两半，
            //    以及撤环境那条「回调计数到齐才 Destroy」）—— 判据（逐句读方法体）→
            //    `资料/加时与冲突模式_原版规格.md` 的 2026-09-30 那一节。
            //    ⚠️ **仍没接**：`ScenarioParticleSpawnerBlender`（4 个实例，要原版 `ParticleSystemAreaSpawner*`）。
            if (_envApplier == null) _envApplier = gameObject.AddComponent<EnvironmentApplier>();
            var envItem = EnvironmentConditions.Find(so);
            if (envItem == null)
            {
                Debug.LogWarning($"[Battle] 进攻卡选定的环境 `{so}` **在 `Resources/EnvironmentConditions.json` 里查不到**"
                               + " ⇒ 环境不切（不许静默）。跑一次 `python 工具/gen_environment_conditions.py`。");
                return;
            }
            _envApplier.Apply(envItem, instant: false);
            Debug.Log($"[Battle] 进攻卡环境已应用：`{so}`（先手 P{Ctx.OffensiveSeat + 1}）"
                    + $" · blendTime={envItem.blendTime:F1}s"
                    + $" · 物件={(EnvironmentConditions.HasPrefab(envItem) ? envItem.prefabName : "无（只补间雾/环境光）")}"
                    + $" · fog={envItem.fogDensity:F4} · ambientBlend={envItem.ambientBlend:F3}");
        }

        // ==================================================================
        //  🆕 2026-09-30（§25）：**reveal 动画** —— 原版那条协程的等价物
        // ==================================================================
        //  判据 —— 三段都逐行读过（常量用 `工具/read_literal.py` 实读）：
        //    · 外层状态机 `BattleManager._ApplyOffensiveAndDefensiveEffects_d__337__MoveNext.c`
        //    · `CardScript._DestroyCardOffensive_d__270__MoveNext.c`
        //    · `CardScript._DisplayOffensiveCardInCenter_d__264__MoveNext.c`
        //
        //  **外层**（照 `param_1+0x10` 那几个 state 逐段抄）：
        //    state 0  非空卡才 `DisplayRevealedCard(…)` ⇒ 等 `manager+0x488` = **2.0 s**
        //    state 1  HUD `+0x70` 按「选定卡 ≠ 空卡」`SetActive(true)`；`+0x78` 按 `HasPlayedThirdCardInTurn`
        //             ⇒ 起 `DestroyCardOffensive(卡, DAT_1834b2bb8 = 1.0)`；那条协程**内部只等 `t × 0.5 = 0.5 s`**
        //               （1.0 是 `DOMove` 那条**补间**的时长、不阻塞）⇒ **外层等 0.5 s**
        //    state 2  `ApplyEnvEffect(…)` ⇒ 非空卡再等 `manager+0x4a0` = **3.0 s**
        //    state 3  结束
        //  **内层**（`DisplayOffensiveCardInCenter(卡, 0.3, 0.3)`，与外层那 2.0 s **并发**）：
        //    `DOMove(目标位, 0.15)`(ease3) → `DORotate(…, 0.1)` → 等 `0.3 × 0.66 = 0.198`
        //    → `DOScale(目标缩放, 0.3)`(ease3) → 等 `0.3 × 2.6 = 0.78` → 显示创建者文字 + 高亮
        //    → 再等 0.78 → 藏 2D 面            （合计 1.758 s，正好塞进那 2.0 s）
        //
        //  🔴🆕 **2026-10-18（第十五轮 · `G5`）：上面那句「显示创建者文字」本批钉死了消费面 ——
        //    我们那一格落在【黑名单文件】里，所以只记判据、本笔不动**：
        //    · 调用点就是这条协程（`CardScript._DisplayOffensiveCardInCenter_d__264__MoveNext.c:86-88`）：
        //      `BattleCardUI.DisplayCreatedByText(cardUI, *(int*)(card + 0x228) /* = CardStateOptions */)`；
        //    · 文字 = `SupportMethods.GetCreatedByText(创建者卡名)`
        //      = `GetTranslation("Battle/HUD/CreatedBy")(0x4288580).Replace("{0}", 名字)`
        //      （`SupportMethods__GetCreatedByText.c` 亲读；`{0}` 字面量 = `0x4265D10`）；
        //    · **显隐判据**（`BattleCardUI__DisplayCreatedByText.c:9-50`）：节点 `CreatedByText` **先关**，
        //      只有 `CardStateOptions ∈ {inHandShowing = 8, inHandPlaying = 10}` **且**
        //      `card.creator(+0x248 → +0x268) != null` **且**创建者不是本地方（`+0x40 == 0`）时才点亮
        //      （另有一个入口：`BattleCardUI.SetObjectVisibility` 里同一个判断）。
        //      节点本身在 13 个 arena 的每张 `BattleCardUI` 上都有（tree: `CreatedByText (inactive) …
        //      text:'Created by someone fancy'`）。
        //    · 🔴 **我们的落点两处都在黑名单里**：① 那句字要挂在**卡视图**上（= `Core/CardView.cs`）；
        //      ② 它要读「这张卡是谁造的」，而 `RuleEngine/` 里**根本没有这个字段**
        //      （本批全仓 grep `Creator|createdBy|CreatedBy` ⇒ **0 命中**）
        //      ⇒ 要做得先加引擎字段 + 动卡视图。**本笔停手，如实记**（⛔ 没在本类里另起一层补丁——
        //      那会变成「同一个语义两条路径」，而且卡视图的生命周期不归本类管）。
        //
        //  🔴 **三处如实标注**（铁律 3：查不到就写查不到，不许把猜的写成「原版就是这样」）：
        //    ① **落点位姿没能完全解出来** —— 原版那两个目标来自
        //       `GetOffensiveCardPlayedDisplayPosition` / `GetEnemyCardPlayedDisplayScale`，它们依赖
        //       **两个静态对象**（`DAT_1842da2a8 + 0xb8` 上的两组 Vector3）与 **`BattleManager + 0xd8`**
        //       那个 transform ⇒ 本地解不出**基准位/基准缩放**。
        //       ✅ **但缩放那一半的乘数有真值**：`GetEnemyCardPlayedDisplayScale` 非移动端 = `基准 ×
        //       VarsGlobal.enemyCardDisplayScale(+0x48) = **1.4**`（移动端再 × `+0x4c = 1.3`）
        //       —— 判据 `资料/VarsGlobal_原版数值.md:35-36`。
        //    ② **1.0 s 那一段的目标 = HUD 那颗进攻卡钮的位置**（原版 `BattleHud.OffensiveButtonTransform()`），
        //       **不是墓地** —— 2026-09-30 亲读反编译订正（文档里原来写的「DOMove(墓地)」是错的）。
        //    ③ **推进方式用显式 Lerp、不走 DOTween** —— 批处理没有帧循环，`AdvanceTimeline` 泵一次推一步，
        //       真机 `Update` 与自检 `Step` 走的是同一段代码、同一组数（同 `EnvironmentApplier` 的口径）。

        /// <summary>要不要演 reveal（真机 true）。关掉时 `BeginBattleAfterSetup` 直接生效 ——
        /// 与 `offensivePhaseEnabled` 同一条规矩（批处理要「当场精确的状态」时把它关掉）。</summary>
        public bool offensiveRevealEnabled = true;

        // 外层四段（原版实读值，出处见上面那段注释）
        const float RevealHoldDur  = 2.0f;    // `manager+0x488` / `DAT_1834b2bbc`
        const float RevealFlyHold  = 0.5f;    // `DAT_1834b2bb4`（`t × 0.5`；外层真正等的那一段）
        const float RevealTailDur  = 3.0f;    // `manager+0x4a0` / `DAT_1834b2e8c`（非空卡才等）
        /// <summary>`DestroyCardOffensive(卡, t)` 的 `t`（`DAT_1834b2bb8` = 1.0）—— 它是**补间**时长，
        /// 所以这一段的**实际等待**是 `RevealFlyHold`（0.5），不是 1.0。列在这里是为了和原版那张表对得上。</summary>
        const float RevealFlyDur   = 1.0f;
        // 内层（`DisplayOffensiveCardInCenter`；与外层那 2.0 s 并发）
        const float RevealMoveDur  = 0.15f;   // `DAT_1834b3178`
        const float RevealRotDur   = 0.10f;   // `DAT_1834b2dc4`
        const float RevealWait1    = 0.198f;  // `DAT_1834b3184` × 0.66
        const float RevealScaleDur = 0.30f;   // 第三实参
        const float RevealWait2    = 0.78f;   // `DAT_1834b3190` × 2.6
        const float RevealStartScale = 0.01f; // 原版 `localScale = 场上值 × 0.01`（`DAT_1834b2dbc` 实读）

        /// <summary>🔴 **我们挑的**：起始「场中央」与落到展示位的屏幕像素坐标。
        /// 原版的基准位 = `BattleManager+0xd8` 那个 transform + 两个静态向量 × `VarsGlobal` 的偏移
        /// ⇒ **那三个东西本地都没解出来**（见上面 ① ）。</summary>
        static readonly Vector2 RevealStartPx = new Vector2(960f, 660f);
        static readonly Vector2 RevealEndPx   = new Vector2(960f, 520f);

        /// <summary>落到的缩放。**半真半推导，写明**：真值只有那个乘数 **1.4**（`VarsGlobal.enemyCardDisplayScale`，
        /// `GetEnemyCardPlayedDisplayScale` 非移动端就是拿它乘基准）；**基准查不到** ⇒ 我们按已有的「大卡」口径取
        /// `CardDisplayWindow` 那 5 个槽的原版 `m_LocalScale = 250`（= 250 设计像素宽），
        /// 而 `CardView` 自然宽 = 1 世界单位 = **108** 设计像素 ⇒ `1.4 × 250 / 108 ≈ 3.24`。
        /// ⚠️ 所以这个数**是推导值、不是原版读出来的**。</summary>
        const float RevealEndScale = 1.4f * 250f / 108f;

        int _revealStage = -1;         // -1 = 没在跑
        float _revealT;
        CardView _revealCard;
        Vector3 _revealFromWorld, _revealToWorld;
        /// <summary>「进攻卡那一段」跑过了没有 —— 见 `BeginOffensiveRevealOrApply` 的注释
        /// （联机局要能被调两次，所以这个闩**只在真的定下来之后**才闩上）。
        /// 🔴 **2026-10-19（`A1110`）**：它是**本局**的闩 —— 由 `Begin` 每局**显式复位**（`= false`），
        ///   与同族的 `_offensivePhaseDone` / `_revealStage` / `_revealCard` 一起（见 `Begin` 里
        ///   那段「本局一次的闩」）。⚠️ 原来**永不复位** ⇒ 同一台 driver 的**第 2 局起这一整段静默跳过**
        ///   （环境永远停在第 1 局那一套）。</summary>
        bool _offensivePhaseApplied;

        /// <summary>自检用：reveal 跑到第几段（-1 = 没在跑）。</summary>
        public int RevealStageForTest { get { return _revealStage; } }
        /// <summary>自检用：揭示用的那张卡（没在跑时为 null）。</summary>
        public CardView RevealCardForTest { get { return _revealCard; } }
        /// <summary>自检用：这一局选定的进攻卡**不是空卡**（原版 `d__337` state 0 那个 `+0x30`）。</summary>
        public bool OffensiveIsNonEmpty
        { get { return Ctx != null && Ctx.OffensiveChosen && Ctx.OffensiveSlotIdx >= 0; } }
        /// <summary>自检用：reveal 那四段的时长（原版实读值，断言直接盯它们）。</summary>
        public static float[] RevealDurationsForTest
        { get { return new[] { RevealHoldDur, RevealFlyHold, RevealTailDur, RevealFlyDur }; } }

        /// <summary>那颗钮的显隐（原版 `d__337:146-155`：判据 = 选定卡 ≠ 空卡）。**只此一处判**。
        /// 返回「这一局用了进攻卡」。</summary>
        bool ApplyOffensiveButtonVisibility()
        {
            bool useCard = Ctx.OffensiveSlotIdx >= 0;
            if (_offensiveBtn != null) _offensiveBtn.gameObject.SetActive(useCard);
            return useCard;
        }

        /// <summary>换牌之后那一段的**分岔**：有进攻卡 ⇒ 先演 reveal；空卡 ⇒ 直接生效。
        /// （原版 `d__337` 的 `if (*(char *)(param_1 + 0x30) != '\0')` 就是这一岔。）
        ///
        /// 🆕 2026-09-30 · **联机局要能被调两次**：本机是**后手**时，自己那条防御卡先落地、
        /// `BeginBattleAfterSetup` 已经跑过一遍 —— 而那时**先手方那条进攻卡还没到**
        /// （`Ctx.OffensiveChosen` 还是 false）⇒ 这里**直接返回**；等它到了
        /// （`ApplyLoggedAction` 里 `NetApply.IsOffensivePick` 那条）**再调一次**。
        /// 所以 `_offensivePhaseApplied` 这个闩**只在真的定下来之后**才闩上 —— 早闩的话后手那台永不换环境。</summary>
        void BeginOffensiveRevealOrApply()
        {
            if (_revealStage >= 0) return;          // 正在演
            if (_offensivePhaseApplied) return;     // 已经跑过（一局一次）
            if (!Ctx.OffensiveChosen) return;       // 还没人定 ⇒ 等（联机局里对面那条到了会再调）
            _offensivePhaseApplied = true;
            if (!offensiveRevealEnabled || !OffensiveIsNonEmpty)
            {
                ApplyOffensiveEnvOnce();            // 空卡 / 自检关掉 reveal：直接生效
                return;
            }
            var c = ChosenOffensiveCard();
            if (c == null)
            {
                // ⚠️ 查不到就**别演**（`OffensiveCardData` 不判空 ⇒ 硬演会空引用），但**要出声**
                Debug.LogWarning("[Battle] 进攻卡 reveal：选定的那张卡在**本机的进攻卡表里查不到**"
                               + $"（槽 {Ctx.OffensiveSlotIdx} / 环境 `{Ctx.OffensiveEnvSO}`）"
                               + " ⇒ 不演 reveal，环境照常生效（不许静默）");
                ApplyOffensiveEnvOnce();
                return;
            }
            var d = OffensiveCardData(c, OffensiveCardFaction(c));
            _revealCard = CardView.Create(hudRoot, d, "OffensiveReveal");
            _revealFromWorld = LayoutSpace.FromPixel(RevealStartPx.x, RevealStartPx.y);
            _revealToWorld = LayoutSpace.FromPixel(RevealEndPx.x, RevealEndPx.y);
            _revealCard.transform.position = _revealFromWorld;
            _revealCard.transform.localRotation = Quaternion.identity;   // 原版那个目标旋转**没解出来**（见 ① ）
            _revealCard.transform.localScale = Vector3.one * RevealStartScale;
            _revealStage = 0;
            _revealT = 0f;
            Debug.Log("[Battle] 进攻卡 reveal 开始（原版 `_ApplyOffensiveAndDefensiveEffects`）"
                    + $" —— 卡 `{d.title}` · 四段时长 {RevealHoldDur}/{RevealFlyDur}+{RevealFlyHold}/{RevealTailDur} s");
        }

        /// <summary>reveal 的泵（由 `AdvanceTimeline` 每帧推一次；真机与自检同一个入口）。</summary>
        void TickOffensiveReveal(float dt)
        {
            if (_revealStage < 0) return;
            _revealT += dt;
            if (_revealCard != null) ApplyRevealInnerPose(_revealT);

            if (_revealStage == 0 && _revealT >= RevealHoldDur)
            {
                // state 1：**HUD 那颗钮的显隐**（原版排在那 2.0 s **之后**）。
                // 🔴 原来这一步在 `ApplyOffensiveEnvOnce` 里 ⇒ **比原版早**；现在挪到这儿，
                //    而 `ApplyOffensiveEnvOnce` 仍然调同一个函数（判据只写一处）。
                ApplyOffensiveButtonVisibility();
                StartRevealFlight();                 // `DestroyCardOffensive(卡, 1.0)` 那一段
                _revealStage = 1; _revealT = 0f;
            }
            else if (_revealStage == 1 && _revealT >= RevealFlyHold)
            {
                if (_revealCard != null) { Kill(_revealCard.gameObject); _revealCard = null; }
                ApplyOffensiveEnvOnce();             // state 2 的 `ApplyEnvEffect(…)`（**函数体没动**，只改了调用点）
                _revealStage = 2; _revealT = 0f;
            }
            else if (_revealStage == 2 && _revealT >= RevealTailDur)
            {
                _revealStage = -1;                   // state 3：结束
                Debug.Log("[Battle] 进攻卡 reveal 结束（原版 state 3）");
            }
        }

        /// <summary>内层那三段补间的**显式插值**（不用 DOTween，理由见上面 ③ ）。
        /// ⚠️ 原版三处 `SetEase(3)` 里的 **3** 没有直接出处（同族枚举只有 `1=Linear`、`9=OutCubic`
        /// 有实据 ⇒ 3 是**推的 OutSine**）—— 我们用线性，如实标。</summary>
        void ApplyRevealInnerPose(float t)
        {
            var tr = _revealCard.transform;
            float mp = Mathf.Clamp01(t / RevealMoveDur);
            tr.position = Vector3.Lerp(_revealFromWorld, _revealToWorld, mp);
            float sp = Mathf.Clamp01((t - RevealWait1) / RevealScaleDur);
            tr.localScale = Vector3.one * Mathf.Lerp(RevealStartScale, RevealEndScale, sp);
        }

        /// <summary>`DestroyCardOffensive(卡, t)` 那一段：卡**飞到 HUD 那颗进攻卡钮**上并变淡
        /// （判据 = `CardScript._DestroyCardOffensive_d__270__MoveNext.c`：`DOMove` 的目标是
        /// `BattleHud.OffensiveButtonTransform()` 的位置 —— **不是墓地**）。
        /// ⚠️ 原版这一步还带 `SetCardStateInCemetery` + `StopAnimsInPlay` + `DissolveWholeCard`；我们这边那张
        /// 揭示卡是**临时视图**（不在任何牌区里）⇒ 等价物就是「淡出 + 收掉」，不另造牌区状态。</summary>
        void StartRevealFlight()
        {
            if (_revealCard == null) return;
            var tr = _revealCard.transform;
            Vector3 to = (_offensiveBtn != null) ? _offensiveBtn.transform.position : tr.position;
            CardTween.Use(tr.DOMove(to, RevealFlyDur), Ease.InOutSine, _revealCard);
            // ⚠️ 只在材质**真有 `_Color`** 时才补间（原版那批材质上带着内置 Standard 的残留值，
            //    没有这个属性的 shader 上 `DOFade` 只会刷警告 —— 见 `CLAUDE.md` §三那条）。
            var mr = _revealCard.GetComponentInChildren<MeshRenderer>();
            if (mr != null && mr.material != null && mr.material.HasProperty("_Color"))
                CardTween.Use(mr.material.DOFade(0f, RevealFlyDur), Ease.InOutSine, _revealCard);
        }

        /// <summary>自检用：手动把 reveal 推进一步（批处理没有帧循环）。</summary>
        public void AdvanceOffensiveRevealForTest(float dt) { TickOffensiveReveal(dt); }
        /// <summary>自检用：手动起 reveal（真机由 `BeginBattleAfterSetup` 起）。</summary>
        public void BeginOffensiveRevealForTest() { BeginOffensiveRevealOrApply(); }

        /// <summary>环境执行器（惰性建；原版是 `ScenarioEnvironmentConditionsManager`，挂在 BattleManager 上）。</summary>
        EnvironmentApplier _envApplier;

        // ==================================================================
        //  🆕 2026-09-18 开局独白（原版 `VoiceLinesController.ShowHeroesIntroMessage`）
        //
        //  规格**全部来自全量反编译**，见 `资料/语音线_原版规格与ASR管道.md` §1.5：
        //   · **严格「先手 → 后手」串行** —— 先手那条**播完**（原版 `yield WaitForSeconds(GetCurrentClipLength())`）
        //     才播后手。**这就是为什么这里要一个状态机、而不是一次播两条。**
        //   · 除「等当前语音播完」外**没有任何额外秒数**，无随机、无计数。
        //   · **`vs*` 的查表不在协程里**，在 `ChatManager`：`GetCustomIntro(己方, 对方督军)`
        //     → 失败 `GetCustomIntroByArmy(己方, 对方.army)` → 再失败退回普通 `intro`。
        //     我们用 `VoiceLines.ForVersus(对方督军, 对方阵营)` 一次给出这个顺序（它末尾就带 `intro`）。
        //   · **同督军对局**（`AreSameWarlords()`）改用 `mirror` 那一族 → `VoiceLines.ForMirror`。
        //
        //  ⚠️ **两条我们没做的（照实记着，不是静默）**：
        //    ① 原版这段时间 `popUpEnabled = false`（`CanChat` 会拒）—— 我们**还没有聊天按钮**，
        //       所以这条没有落点；等 `ChatPopup` 做出来时要一并接。
        //    ② 原版**教程局 / campaign boss 局根本不 StartCoroutine**（不播 intro）——
        //       我们**没有教程/战役对局**这个概念，所以这条暂时不适用；将来做第 16 行时要补。
        //  ⚠️ **主讲不只限督军**（`customIntroChatData` 挂在所有卡上），但 `intro`/`mirror` 这两族
        //     在全池各只有 54 条、**都是督军**（普通单位只有 `line`）⇒ 这里只让督军开口是**对的**。
        // ==================================================================

        /// <summary>-1 = 不做 / 已做完；0 = 该先手说；1 = 该后手说。</summary>
        int _introStage = -1;
        /// <summary>先手是哪一方（原版 `IsPlayerFirstIntro`，取不到时默认玩家先）。</summary>
        int _introFirst = -1;

        /// <summary>开一段新的开局独白。**正常由 `FinishMulligan` 调**；
        /// `public` 是**给自检用的**（让它可以不解一整局就点名触发，不必模拟换牌流程）。</summary>
        public void StartIntroMonologue()
        {
            _introStage = -1;
            if (_unitChat == null || !VoiceLines.Ready || Ctx == null) return;
            _introFirst = Ctx.Active;             // 开打时轮到谁，谁就先说
            if (_introFirst < 0 || _introFirst > 1) return;
            _introStage = 0;
        }

        /// <summary>在 `Update` 里推 —— **等上一条播完再放下一条**（原版那条串行就靠这个）。</summary>
        void TickIntroMonologue()
        {
            if (_introStage < 0) return;
            if (_introStage > 1) { _introStage = -1; return; }
            // 判据：气泡还在（`ShownSide != -1`）或音频还在响 ⇒ 还不到下一条。
            // ⚠️ 两个都判 —— 气泡有**最短 2 秒**、音频可能更长/更短，单看任一个都会抢拍。
            if (_unitChat.IsSpeaking || _unitChat.ShownSide != -1) return;
            int side = _introStage == 0 ? _introFirst : 1 - _introFirst;
            _introStage++;                        // 先自增：下面这一说要等它播完才轮到下一条
            SpeakIntro(side);
        }

        /// <summary>让 `side` 的督军说开场白（原版 `ShowHeroIntroMessage`）。</summary>
        void SpeakIntro(int side)
        {
            if (_unitChat == null || !VoiceLines.Ready || Ctx == null) return;
            if (side < 0 || side > 1) return;
            var me = Ctx.Players[side];
            var foe = Ctx.Players[1 - side];
            var w = me != null ? me.Warlord : null;
            if (w == null || w.Card == null) return;
            var fw = foe != null ? foe.Warlord : null;
            var fc = fw != null ? fw.Card : null;

            // 同督军对局 → `mirror`（原版 `AreSameWarlords()` 分支）；否则走 `vs*` 那条回落链
            // （`ForVersus` 的返回顺序就是原版的回落链，末尾带 `intro`）
            var order = (fc != null && fc.Id == w.Card.Id)
                ? VoiceLines.ForMirror
                : VoiceLines.ForVersus(fc != null ? fc.Name : null, fc != null ? fc.Faction : null);

            string file, text, ev;
            // 随机源传 null —— 与别处同规矩：**不消耗 `Ctx.Rng`**
            if (!VoiceLines.TryPick(w.Card.Id, order, null, out file, out text, out ev)) return;
            var clip = VoiceLines.Clip(file);
            if (clip == null) return;
            _unitChat.Speak(side, w.Card.Id, "Intro", ArtKey(w.Card), CardDisplayName(w.Card, null), text, clip, file);
        }

        /// <summary>自检用：这一局的独白推到第几步了（-1 = 没在推 / 已推完）。</summary>
        public int IntroStage { get { return _introStage; } }

        /// <summary>自检用：走**和真实点击同一条路**点某张牌的「换」。
        /// 返回「这次点击被面板收下了吗」—— **不是**「现在标着没有」（那要用 `Mulligan.IsMarked`）</summary>
        public bool SimulateMulliganToggle(int i)
        {
            if (_mulligan == null || !_mulligan.Visible) return false;
            return _mulligan.HandleClick(_mulligan.CardBtnWorldPos(i));
        }

        /// <summary>自检用：点「完成换牌」（真实路径：走面板的命中判定）</summary>
        public bool SimulateMulliganDone()
        {
            if (_mulligan == null || !_mulligan.Visible) return false;
            _mulligan.HandleClick(_mulligan.DoneWorldPos);
            return true;
        }

        /// <summary>自检用：点那颗「眼睛」（原版 `HideMulliganButton`）—— 走和真实点击同一条路。
        /// 原版那一下会**同时**收起：卡片上的换牌按钮 + **压暗层**（`ShowMulliganElements(false)`）</summary>
        public bool SimulateMulliganEye()
        {
            if (_mulligan == null || !_mulligan.Visible) return false;
            return _mulligan.HandleClick(_mulligan.EyeWorldPos);
        }

        // ==================================================================
        //  选牌 / 选效果面板（原版 `ChooseCardMenu`）—— 2026-09-14
        // ==================================================================
        //
        // **它解决什么**：引擎里三个「本该问玩家」的点（选牌 / 三选一 / 选效果）原来**全是
        // `ctx.Rng` 等概率自动挑**（`DoChooseCard` 的注释自己写着「表现层的选牌 UI 是另一件」）。
        // 全池 **62 张**卡会走到那里（普查：`资料/选牌_数据与规格.md` §乙；⚠️ 更正：原来指 `资料/选牌_受影响卡普查.md`，2026-10-10 已并入）。
        //
        // **接法**（引擎侧见 `BattleContext.ChoosePicks` 的注释）：
        //   玩家**发起动作之前**，把这张卡里「本该问玩家」的每一处按**结算顺序**问一遍，
        //   把答案排进 `ctx.ChoosePicks`；引擎结算到那一步就出队用。
        //   ⚠️ **候选必须现取**（普查 §四第 7 条）：`deck` 组里 `draw` 那类会洗牌库 ⇒ 不能预存。
        //   ⚠️ **AI 回合不问** —— 让 AI 停下来没人会点它。
        //   ⚠️ **没覆盖到的会被如实报出来**：结算完 `ChooseSites > ChooseAnswered` 就说明有几次
        //      是引擎替玩家挑的。那种事**不报错**，所以这里主动写进战斗日志（红线：不许静默）。

        ChoosePanel _choosePanel;

        /// <summary>自检用：选牌面板</summary>
        public ChoosePanel Choose { get { return _choosePanel; } }

        /// <summary>正在被问的那张手牌的下标（`_pendingInst` 取不到时的兜底 —— 见 `PendingCard`）。</summary>
        int _pendingIdx = -1;
        /// <summary>
        /// 🆕 第 7 行第 4 步：正在被问的**那一份**（面板可能开着好几帧，手牌中途会变 ——
        /// 只握下标的话，变一次就指到**另一张**上了）。
        /// 🆕 2026-10-17（A904）：**主动技能那一批 ask 也写它**（写的是场上那个单位的实例）——
        /// `PendingCard` 靠它取「正在结算的那张卡」，面板标题链认的就是它
        /// （原版 `ChooseCardMenu__SetUpTitleText` 收的 `actingCardId`）。
        /// </summary>
        CardInstance _pendingInst;
        List<EffectOp> _pendingAsks = new List<EffectOp>();      // 这一批要问玩家的那几处（按结算顺序）
        int _pendingAsk;

        /// <summary>
        /// 🆕 2026-10-17（A904）：**这一批 ask 问完之后要做什么** —— 两种来源：
        ///   · 出牌 —— `BeginPlay` 设成「真的把这张牌打出去」（`DoPlay`）
        ///   · **主动技能** —— `Resolve` 设成「真的把这一手打出去」（`DoResolve`）
        /// 为什么要做成回调：原版的 ask 是**结算协程走到那一步才挂起等玩家**
        /// （`BattleManager._ChooseCardMethod_d__449__MoveNext.c:44-48`），我们引擎是同步的，
        /// 只能在动作边界上对齐 ⇒ 「问完接着做哪一个动作」必须显式带着走，不能写死成出牌。
        /// </summary>
        System.Action _afterAsks;

        /// <summary>
        /// 🆕 2026-10-17（A904）：上一次「报过账」时的 `ctx.ChooseSites / ChooseAnswered` ——
        /// 靠它做到「一次动作里被调多次也只说一遍」。
        /// 🔴 **必须和这两个计数的清零时机对上**（`BeginChoiceBatch`）：出牌那一批清计数 ⇒ 这里也归零；
        /// 技能那一批不清 ⇒ 这里记**当前值**。对不上就会**静默不报**（水位高过计数）。
        /// </summary>
        int _settledSites, _settledAnswered;

        readonly List<CardView> _chooseViews = new List<CardView>();

        /// <summary>🆕 2026-09-14：这一处 `choosecard` **实际摆给玩家的候选**（可能是抽出来的 3 张，
        /// 见 <see cref="ChooseShowMax"/>）。**不是** `choosecard` 时是 null。
        /// `OnChooseDone` 靠它把「点了第几张」翻回**卡的身份**（`CardDef.Id`）交给引擎
        /// —— 见 `BattleContext.ChooseCardIds`。</summary>
        List<CardDef> _askCands;

        /// <summary>
        /// 🔴 **规则书英文版 `:475`**：「Whenever a card uses the word "choose", **randomly select 3
        /// cards** from the set of possibilities and the player chooses which one to keep/draw/resolve.
        /// Return unchosen cards drawn from your deck and shuffle.」
        /// 中文版 `:233` 同义。用户 2026-09-14 也点名了这条（「先从大量卡牌里随机抽一些、一般是三个」）。
        ///
        /// ⚠️ **只对「大池子」抽**（`pool` = 全卡池 1130 张 · `deck` = 自己牌库）——
        ///    手牌 / 对手手牌 / 阵亡堆**不抽**：那几个池子本来就小，而且**自己手牌是看得见的**
        ///    （抽掉两张会让「选一张手牌降费」变得莫名其妙）。
        ///    **这一条是我们挑的**，规则书没分来源，如实标着。见
        ///    `资料/卡牌效果or句_审计.md` §四。
        /// </summary>
        const int ChooseShowMax = 3;

        /// <summary>从 <paramref name="n"/> 个候选里**随机抽 <paramref name="k"/> 个**（不放回）。
        /// 用 `ctx.ShowRng`（**不是** `ctx.Rng` —— 理由见 `BattleContext.ShowRng` 的注释）。</summary>
        List<CardDef> DrawForDisplay(List<CardDef> cands, int k)
        {
            var idx = new List<int>();
            for (int i = 0; i < cands.Count; i++) idx.Add(i);
            for (int i = 0; i < k && i < idx.Count; i++)
            {
                int j = i + Ctx.ShowRng.Next(idx.Count - i);
                int t = idx[i]; idx[i] = idx[j]; idx[j] = t;
            }
            var outp = new List<CardDef>();
            for (int i = 0; i < k && i < idx.Count; i++) outp.Add(cands[idx[i]]);
            return outp;
        }

        /// <summary>自检用：现在正在问第几处（0 起）</summary>
        public int ChooseAskIndex { get { return _pendingAsk; } }
        /// <summary>自检用：面板上摆着几张候选</summary>
        public int ChooseOptionCount { get { return _choosePanel != null ? _choosePanel.CardCount : 0; } }
        /// <summary>自检用：第 i 张候选的卡名（面板上显示的就是它）</summary>
        public string ChooseOptionName(int i)
        {
            if (i < 0 || i >= _chooseViews.Count || _chooseViews[i] == null) return "<无>";
            return _chooseViews[i].Data.title;
        }

        /// <summary>自检用：第 i 张候选的**英文 id**（`CardData.id` —— 立绘文件名认的就是它）。
        /// ⚠️ 和 <see cref="ChooseOptionName"/> 的区别：那个是**画在卡面上的标题**，
        ///    中文卡会变成 `正义之怒`；要断言「是哪张卡」得用这个。</summary>
        public string ChooseOptionId(int i)
        {
            if (i < 0 || i >= _chooseViews.Count || _chooseViews[i] == null) return "<无>";
            return _chooseViews[i].Data.id;
        }

        /// <summary>
        /// 自检用（2026-10-17 · F6 #5/#6）：第 <paramref name="i"/> 张候选的**稳定卡号**
        /// （`CardDef.Id` —— `cards_engine.json` 里那个键）。**引擎真正入队的就是它**
        /// （<see cref="OnChooseDone"/> → `Ctx.ChooseCardIds`），所以「引擎用的是**面板给的那一张**吗」
        /// 这一类断言只能拿它去比 `h.Card.Id`。
        ///
        /// ⚠️ **别拿 <see cref="ChooseOptionId"/> 的返回值和 `CardDef.Id` 比** —— 那个返回的是
        ///    `CardData.id`，而本仓约定 `CardData.id` = **卡名**（`ToCardData` 里写的就是 `id = c.Name`；
        ///    卡面标题、立绘配对都用它）⇒ 拿它比 `CardDef.Id` **恒不相等**（两条红就是这么来的）。
        /// ⚠️ 只有 `choosecard` 会填 `_askCands`；`chooseone` / `chooseeffect` 的候选是**合成卡**
        ///    （没有 `CardDef` 可指）⇒ 这里返回 `&lt;无卡号&gt;`（**出声**，不静默给空串）。
        /// </summary>
        public string ChooseOptionStableId(int i)
        {
            if (_askCands == null) return "<无卡号>";
            if (i < 0 || i >= _askCands.Count || _askCands[i] == null) return "<无卡号>";
            return _askCands[i].Id;
        }

        /// <summary>
        /// 自检用（A904）：这张卡上的 ask 点**各归哪一档**（判据 = `AskOwners`，与面板实际用的是同一个）——
        /// 返回形如 `"play=1"` / `"spirit=1"` / `"alt:agenda=1"` / `"play=1,alt:agenda=1"` 的一句话
        /// （按**首次出现**的顺序，一档一项）。
        /// 🔴 **断它 = 断「面板什么时候问」这件事本身** —— 比断某一个界面好：这张表才是判据。
        /// 取不到这张卡 / 它没有 ask 点 ⇒ 返回 `&lt;无此卡&gt;` / `&lt;无 ask 点&gt;`（**不会**静默返回空串）。
        /// </summary>
        public string AskScopesForTest(string cardName)
        {
            if (Ctx == null) return "<无对局>";
            var c = CreatePool.FindByName(Ctx.CardPool, cardName);
            if (c == null) return "<无此卡>";
            var all = RuleCore.PlayerChooseOps(c);
            if (all == null || all.Count == 0) return "<无 ask 点>";

            var owners = AskOwners(c, all);
            var order = new List<string>();
            var cnt = new Dictionary<string, int>();
            for (int i = 0; i < owners.Length; i++)
            {
                string o = owners[i] ?? "play";
                int n;
                if (!cnt.TryGetValue(o, out n)) order.Add(o);
                cnt[o] = n + 1;
            }
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < order.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(order[i]).Append('=').Append(cnt[order[i]]);
            }
            return sb.ToString();
        }

        /// <summary>面板开着时**吃掉这一帧的输入**（和换牌那条同一个规矩）
        /// 🔴 A462：**抬起**（原版 `ChooseCardMenu` 的 `ContinueButton` / `HideChooseButton` 都是
        /// uGUI `Button`，每张卡的 `SelectCardButtonFrame.selectButton` = `EverguildButton`）。
        /// ⚠️ `CardChoicePanel.HandleClick` 里「点卡本身也能选」那一支是**我们加的**（原版是卡上那颗 `Select` 钮）
        /// —— 它没有原版沿的判据，**跟着整行一起改到抬起**（⛔ 别顺手还原成钮）。</summary>
        bool HandleChoose()
        {
            if (_choosePanel == null || !_choosePanel.Visible) return false;
            if (ReleasedThisFrame()) _choosePanel.HandleClick(WorldPointer());
            return true;
        }

        // ------------------------------------------------------------------
        //  🆕 2026-10-17（A904）：**这一处 ask 属于谁** ——
        //  「哪一次引擎调用会把它出队用掉」。答错就会被**别的** ask 点吃掉（静默错位）。
        // ------------------------------------------------------------------
        //
        // **原版判据**（反编译，逐条）：
        //   · `BattleManager.ChooseCardMethod(BattleAction, Action<int>)` 是个**迭代器**
        //     （`il2cpp_out/dump.cs:31768`）——**全工程只有 4 个调用点**，而且**全在「结算到那一步」**：
        //     `_ResolvePlayCardFromHand_d__447__MoveNext.c:607`（非指向性出牌）· `:827`（小人落地之后，
        //     且外面套着 `CanUseSpiritStone / HasDefaultTrait(10)` 这道闸）· `:1248`（指向性法术动画之后）·
        //     `_ResolvePlayActiveAbility_d__479__MoveNext.c:367`（**主动技能**）。
        //   · 每个调用点后面紧跟着 `StartCoroutine(...)` + `return 1` ⇒ **结算协程在此挂起**
        //     （例：`_d__447:827-832`）。玩家的答案由单槽字段
        //     （`ChooseCardMenu.selectedCard` → `BattleManager.cardChosenInSelection`）经
        //     `Action<int>` 回调写回 `battleAction.actionValueTens`，协程才继续。
        //   · ⇒ **原版没有「出牌前一次性问完 + 答案排队」这回事**（那一层根本不存在）。
        //
        // **我们的做法**：引擎是同步的（`ResolveOps` 一口气跑完），没法在结算中途停下来 ⇒
        //   只能在**动作边界**上对齐：**一次动作 = 一次引擎调用 = 一批 ask 点**。
        //   分档表（实测全池 1126 张，见报告）：
        //
        //   | 触发者 | 谁会出队用它 | 我们什么时候问 |
        //   |---|---|---|
        //   | `play`（卡面正文 / `Rally:` / `Deploy:` …） | `RuleCore.PlayCard` | 出牌前（**同一动作内**，差半拍 —— 如实标着） |
        //   | `spirit`（`N [Spirit Stone]:`） | `PlayCard` → `ResolveSpiritAbility` | 同上，**且只在这次付得起时问**（原版那道闸就是 `CanUseSpiritStone`） |
        //   | `oath`（`Oath N:`） | `UseOathAbility` | 玩家点那格技能时（**与触发同拍**） |
        //   | `alt:<关键词>`（`Duty`/`Pray`/`Ferocity`/`Agenda`） | `UseAlternative` | 同上 |
        //
        // 🔴 **为什么必须分档**（这就是那一格账的病）：`BeginPlay` 原来把这张卡**所有** ask 点
        //    一次问完并排进 `ctx.ChoosePicks`。而 `Master Zacharial` 的 `Agenda:` ·
        //    `Suppressor` 的 `Oath 1:` · `Azrael` / `Watcher in the Dark` 的议程**要玩家事后主动点
        //    那格技能**，可能**整局不发动** ⇒ 那格答案一直留在队里，被**下一个** ask 点
        //    （可能是对手 / AI 的）吃掉（`TakePick` 不认是谁在挑）
        //    ⇒ **此后每一处选择全部错位、而且不报错**。
        //    `Farseer` / `Farseer Skyrunner` 是另一头：灵魂石能力在**部署时自动结算**，但
        //    **付不起就整段不结算**（`EffectResolver` 里 `have < op.Cost` 那一句）⇒ 同样是留一格答案在队里。
        //
        // **全池普查**（1126 张 · 离线真解析器探针，见报告）：ask 点**挂在能力正文里**的只有
        //   **6 张** —— `Farseer` · `Farseer Skyrunner`（灵魂石）· `Suppressor`（誓约）·
        //   `Azrael` · `Watcher in the Dark` · `Master Zacharial`（议程）。
        //   ✅ **没有一张「混装」**（同一张牌既有 play 又有 ability 的 ask）· 反方向核 0 漏判。
        //   ⚠️ 普查文档 `资料/普查产出_1017/W_B14_选牌一族.md` 写的是 **4 张**（漏了 `Azrael`
        //      与 `Watcher in the Dark`）—— 已在报告里订正。

        /// <summary>`EffectOp` 的文案比较键 —— 去掉开头的「`&lt;前缀&gt;: `」再逐字比。
        /// 🔴 **为什么要去前缀**：两种来源的 `Source` 一个带前缀一个不带（实测）——
        ///    `Master Zacharial` 的 ask 是 `"Agenda: Choose a card…"`，而
        ///    `CardDef.TriggerOps("agenda")[0].Source` 是 `"Choose a card…"`；
        ///    `Suppressor` 两边都不带前缀。不去前缀就认不出 `Agenda:` 那一族。
        /// ⚠️ 前缀长度限 32 字：再长就不是关键词前缀、是正文里的冒号了。</summary>
        static string NormSrc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Trim();
            int c = s.IndexOf(": ", System.StringComparison.Ordinal);
            if (c > 0 && c < 32) s = s.Substring(c + 2).Trim();
            return s;
        }

        static string AskKey(EffectOp op)
        {
            return op == null ? null : (op.Verb ?? "") + "\u0001" + NormSrc(op.Source);
        }

        /// <summary>这一条 op 是不是「本该问玩家」的那三种（与 `RuleCore.PlayerChooseOps` 同一判据）。</summary>
        static bool IsAskOp(EffectOp op)
        {
            return op != null && !op.RandomPick
                && (op.Verb == "choosecard" || op.Verb == "chooseone" || op.Verb == "chooseeffect");
        }

        /// <summary>
        /// 把这张卡的 ask 点分档 —— 返回**与 <paramref name="asks"/> 同序同长**的归属表
        /// （`"play"` / `"spirit"` / `"oath"` / `"alt:agenda"` …）。
        ///
        /// 🔴 **按 `Verb + 归一化文案` 比、不按对象比**：`PlayerChooseOps` 与 `CardDef.Collect*`
        ///    各调一次 `EffectText.Parse(Desc)`，**是两份不同的对象**（引用比恒不相等）。
        /// 🔴 **认领有名额**：能力正文里同一条文案出现 N 次，最多只认领 N 条 ask
        ///    （不会把同名的那一条正文之外的 ask 也一起吃进来）—— 名额用尽后剩下的算 `play`。
        ///    ⚠️ **已知边界**：若某张牌的同一条文案**既**在 `Rally:` 里**又**在 `Oath:`
        ///    里，名额会先到先得、可能认错一条。**全池 0 张**（本轮普查「混装 0 张」），
        ///    真出现了有兜底：`ReportUnaskedChoices` 的剩余检测会把「问了没人取」当场报出来。
        /// ⚠️ **战术卡一律算 `play`**：它们的整条 `desc` 由 `PlayTactic` 一次结算
        ///    （`ResolveSpiritAbility` 的注释：「战术卡 / 天赋卡**不走这里**」）⇒ 不分档。
        /// </summary>
        static string[] AskOwners(CardDef card, List<EffectOp> asks)
        {
            var owners = new string[asks.Count];
            for (int i = 0; i < asks.Count; i++) owners[i] = "play";
            if (card == null || !card.IsUnit) return owners;

            // ---- ① 把能力正文里的 ask 点收成「名额表」 ----
            var quota = new Dictionary<string, int>();
            var who = new Dictionary<string, string>();
            AddQuota(quota, who, card.SpiritOps, "spirit");
            AddQuota(quota, who, card.OathOps, "oath");
            foreach (string k in RuleCore.AlternativeActions)          // duty / pray / ferocity / agenda
                AddQuota(quota, who, card.TriggerOps(k), "alt:" + k);
            if (quota.Count == 0) return owners;

            // ---- ② 按名额认领 ----
            for (int i = 0; i < asks.Count; i++)
            {
                string key = AskKey(asks[i]);
                int n;
                if (key == null || !quota.TryGetValue(key, out n) || n <= 0) continue;
                quota[key] = n - 1;
                owners[i] = who[key];
            }
            return owners;
        }

        static void AddQuota(Dictionary<string, int> quota, Dictionary<string, string> who,
                             IReadOnlyList<EffectOp> body, string tag)
        {
            if (body == null) return;
            foreach (var o in body)
            {
                if (!IsAskOp(o)) continue;
                string key = AskKey(o);
                if (key == null) continue;
                int n;
                quota.TryGetValue(key, out n);
                quota[key] = n + 1;
                who[key] = tag;
            }
        }

        /// <summary>归属标签的人话（出声用）。</summary>
        static string OwnerLabel(string tag)
        {
            if (tag == null) return "?";
            if (tag == "spirit") return "灵魂石能力（`N [Spirit Stone]:`）";
            if (tag == "oath") return "誓约能力（`Oath N:`）";
            if (tag.StartsWith("alt:", System.StringComparison.Ordinal))
                return "替代行动（`" + tag.Substring(4) + "`）";
            return tag;
        }

        /// <summary>
        /// **出这一次牌**该问的那几处（按结算顺序）。能力那一档不在里面
        /// （`oath` / `alt:` 随玩家点那格技能的时机问；见上面那张分档表）。
        /// ⚠️ **被排除掉的每一处都会在这里出声**（灵魂石不够 / 推迟到技能那一刻）——
        ///    调用方不用另写一句汇总，免得同一件事在战斗日志里说两遍。
        /// </summary>
        List<EffectOp> PlayAsks(CardDef card)
        {
            var outp = new List<EffectOp>();
            var all = RuleCore.PlayerChooseOps(card);
            if (all == null || all.Count == 0) return outp;

            var owners = AskOwners(card, all);
            string name = card != null ? card.Name : "?";
            for (int i = 0; i < all.Count; i++)
            {
                string o = owners[i];
                if (o == null || o == "play") { outp.Add(all[i]); continue; }

                // ---- 灵魂石能力：**它属于「这一次出牌」**（`PlayCard` 里 `ResolveSpiritAbility`
                //      排在 Rally 之前，`:1183`）⇒ 照旧在出牌前问，但**只在这次真的付得起时问**。
                //      🔴 付费判据与引擎同一句（= `EffectResolver` 里那句 `have < op.Cost` ⇒ 整段不结算），
                //         **原版那道闸是 `CanUseSpiritStone`**（`_d__447__MoveNext.c:723` —— 正是
                //         「能付才走到 `NeedsToChooseFromPool`」）。
                //      ⚠️ 这是**明知故犯地重复了一条引擎判据**，理由三句：
                //        ① 它只有一行、语义就是卡面印的那几个字（`N [Spirit Stone]`）；
                //        ② 不重复的代价是「每一次带灵魂石能力的牌都白问一遍」——而**开局 0 石头是常态**
                //           （实测：`Farseer` 每局第一次落地的常态就是付不起）；
                //        ③ 判错时有**兜底**：`ReportUnaskedChoices` 会把「问了没人取」当场报出来。
                if (o == "spirit")
                {
                    int have = Ctx != null ? Ctx.Players[_me].SpiritStones : 0;
                    if (have >= all[i].Cost) { outp.Add(all[i]); continue; }
                    Ctx.Log($"（选牌面板：「{name}」的灵魂石能力要 {all[i].Cost} 点灵魂石、"
                          + $"现在只有 {have} 点 —— **这次不会发动**，这一处不问）");
                    continue;
                }

                // ---- oath / alt：**出牌时不问**（原版是在 `_ResolvePlayActiveAbility` 里才问的）
                Ctx.Log($"（选牌面板：「{name}」的这一处挂在**{OwnerLabel(o)}**上 —— "
                      + "出牌时**不问**，等那格技能真的发动时再问）");
            }
            return outp;
        }

        /// <summary>
        /// **这一次主动技能**该问的那几处（判据见上面那张分档表）。
        /// `alt` = 替代行动关键词（没有传 null）· `oath` = 这一手走誓约那一条。
        /// 三个来源（`alt` → `oath` → `Ability:`）在 `Resolve` 里是**互斥**的，所以只挑对应那一档。
        /// ⚠️ `Ability:`（`EffectSpec`）那一档实测全池 **0 张带 ask 点**（它的正文是封闭文法），
        ///    所以 `want == "ability"` 时恒为空 —— 保留这支是为了「没有就被如实说出来」，不是死代码。
        /// </summary>
        List<EffectOp> AbilityAsks(UnitState u, string alt, bool oath)
        {
            var outp = new List<EffectOp>();
            if (u == null || u.Card == null) return outp;
            var all = RuleCore.PlayerChooseOps(u.Card);
            if (all == null || all.Count == 0) return outp;

            string want = alt != null ? "alt:" + alt : (oath ? "oath" : "ability");
            var owners = AskOwners(u.Card, all);
            for (int i = 0; i < all.Count; i++)
                if (owners[i] == want) outp.Add(all[i]);
            return outp;
        }

        /// <summary>开一批 ask：清队列 → 摆第一处 → 问完由 `_afterAsks` 接着做那一次动作。
        /// <paramref name="freshCounters"/> = 要不要连 `ChooseSites/ChooseAnswered` 一起清零
        /// （出牌那一批 = **是**，与改这一版之前**逐字相同**；技能那一批 = **否**，理由见 `BeginChoiceBatch`）。</summary>
        void BeginAsk(List<EffectOp> asks, System.Action after, bool freshCounters)
        {
            _pendingAsks = asks;
            _pendingAsk = 0;
            _afterAsks = after;
            BeginChoiceBatch(freshCounters);
            ShowAsk();
        }

        /// <summary>开一批新的选择：清队列（出牌那一批还要清计数）+ 把「报过账」的水位对上。
        ///
        /// 🔴 **技能那一批【不清计数】** —— 两个理由：
        ///   ① `ctx.ChooseSites / ChooseAnswered` 是**累计**语义：`DeepHash` 把它们算进
        ///      **录像对账哈希**（`BattleDriver.DeepHash`），而重放那条路（`NetApply.Apply`、
        ///      `ApplyLoggedAction`）**从不**调 `ResetChoices` ⇒ 多一处清零 =
        ///      多一处「录/放两边这两个数不一样 ⇒ 假报分叉」的风险。
        ///      （**出牌**那一处本来就在清，保持原样、不扩大这种不对称。）
        ///   ② 技能那一批**不需要**清队列：`ReportUnaskedChoices` 已经在**每一次动作之后**
        ///      把没人取的答案清干净了（那才是「清队」的判据所在），这里再清一次只是复述。
        /// 水位跟着走：清计数的那一批归零；不清的那一批记**当前值**（只报从现在起新增的）。</summary>
        void BeginChoiceBatch(bool freshCounters)
        {
            if (freshCounters)
            {
                Ctx.ResetChoices();
                _settledSites = 0; _settledAnswered = 0;
                return;
            }
            Ctx.ChoosePicks.Clear();
            Ctx.ChooseCardIds.Clear();
            _settledSites = Ctx.ChooseSites;
            _settledAnswered = Ctx.ChooseAnswered;
        }

        /// <summary>玩家要出一张牌 —— **先把这一次出牌该问的问完**，再真的打出去。</summary>
        void BeginPlay(CardView card, int idx, int slot)
        {
            var hand = Ctx.Players[_me].Hand;
            var def = (idx >= 0 && idx < hand.Count) ? hand[idx].Card : null;
            var asks = PlayAsks(def);                        // 第 7 行第 2 步：手牌存实例
            // ⚠️ 被推迟/被跳过的每一处，`PlayAsks` 里都**出声**过了（不在这儿补一句汇总，
            //    同一件事在战斗日志里说两遍只会让人以为发生了两次）。

            if (asks == null || asks.Count == 0)
            {
                DoPlay(card, idx, slot);
                return;
            }

            var v = card; int i0 = idx, s0 = slot;
            var inst = (idx >= 0 && idx < hand.Count) ? hand[idx] : null;
            _pendingIdx = idx; _pendingInst = inst;
            // 🔴 **续跑时要按「那一份」重算下标**（面板开着这段时间里手牌可能变过）——
            //    判据与原 `NextAsk` 逐字相同，只是搬进闭包、在字段被清掉之前把值捕获下来。
            BeginAsk(asks, () =>
            {
                int i = (inst != null) ? Ctx.Players[_me].Hand.IndexOf(inst) : i0;
                if (i < 0) i = i0;
                DoPlay(v, i, s0);
            }, true);
        }

        /// <summary>正在被问的那张手牌（面板要按它的名字查效果池 / 取它的插图）。</summary>
        CardDef PendingCard
        {
            get
            {
                if (Ctx == null) return null;
                // 第 7 行第 4 步：优先按**那一份**取（手牌中途变了也不会指错）
                if (_pendingInst != null && _pendingInst.Card != null) return _pendingInst.Card;
                var h = Ctx.Players[_me].Hand;
                return (_pendingIdx >= 0 && _pendingIdx < h.Count) ? h[_pendingIdx].Card : null;
            }
        }

        /// <summary>
        /// 面板上的一个「选项」—— **不一定是卡池里的卡**。
        ///
        /// `chooseone` / `chooseeffect`（载荷型）/ `become` 的候选是**卡面自带的文本或载荷**，
        /// 不是卡。照原版的形状**合成成一张卡**画进同一个面板：
        ///   · 实据 ①：`battle.gd:3758` 那段（用户 **2026-08-28** 定调）——
        ///     「**原版=卡片式**（候选=真实卡面，点选即结算；**非文本按钮列**）」，
        ///     且同一段里写明了兜底法：`候选: 文本→卡面映射 (可解析卡名=真实卡; 否则文本兜底卡)`。
        ///   · 实据 ②：原版 `EnviromentalEffectCardsSO.GetEnvCardList` 返回的是
        ///     **`List&lt;RawCardScript>`** —— 选效果那些选项在原版里**就是卡**。
        ///   · 实据 ③：用户 **2026-09-13** 给的界面线索 —— 选效果那张面板的**插图就是那张战术卡自己的插图**，
        ///     只是**下面的效果文字换成三个选项**。
        /// ⇒ 所以：**能查成卡名的就用真卡**（`Hunting Wolf` / `Righteous Fury` 这些），
        ///    查不到的就合成一张「**用源卡的插图 + 选项文字当效果文字**」的卡。
        /// ⚠️ **哪部分是「我们挑的」**：合成的顺序/位置沿用共享壳那一套（原版 `GetEnvCardList`
        ///    返回的元素离线全是 null ⇒ **没有实况卡面可对**）；费用一律不画（`cost = -1`）。
        /// </summary>
        CardView MakeChoiceCard(string optText, CardDef source, string tag)
        {
            var real = CreatePool.FindByName(Ctx.CardPool, optText);
            if (real != null)
            {
                var rf = string.IsNullOrEmpty(real.Faction) && source != null ? source.Faction : real.Faction;
                return CardView.Create(_choosePanel.transform, ToCardData(real, rf), tag + real.Name);
            }

            var d = new CardData
            {
                // `id` 决定立绘文件名（`Art/cards/art_<id>.png`）—— 用**源卡**的，
                // 这就是用户说的「插图就是那张战术卡自己的插图」。
                id = source != null ? source.Name : optText,
                title = optText,
                cost = -1,                 // 不是真卡 ⇒ 不画费用六边形（`InfoTexture` 里 `cost >= 0` 才画）
                melee = 0, ranged = 0, health = 0, armor = 0,
                // ⚠️ **效果文字位留空** —— 选项文字已经顶在**卡名**那位了，两边都写会在卡面上
                //    把同一句话印两遍（2026-09-14 截图看出来的：`Deploy a Grey Hunter` 上下一各一遍）。
                //    这里等于「**这张候选卡的名字就是它要做的事**」（`battle.gd` 的兜底卡也是
                //    `name = opt`，只是它 desc 又写了一份）。
                keywords = "",
                isUnit = false,            // 走**战术卡**那套文字位置：选项都是「做一件事」，不是单位
                frame = FactionColor(source != null ? source.Faction : null),
                faction = source != null ? source.Faction : null,
                rarity = null,             // 不画稀有度宝石（合成的卡没有稀有度）
                subtype = "",
            };
            return CardView.Create(_choosePanel.transform, d, tag + optText);
        }

        void ShowAsk()
        {
            ClearChooseViews();
            _askCands = null;
            var op = _pendingAsks[_pendingAsk];
            bool hasOptions = false;
            string title = ChoosePanel.DefaultTitle;      // 选牌：**实况值**（`Battle/ChooseCard/Instructions`）

            if (op.Verb == "choosecard")
            {
                string srcName, detail, why;
                var cands = RuleCore.ChooseCardCandidates(Ctx, _me, op, out srcName, out detail, out why);
                if (why == null && cands != null && cands.Count > 0)
                {
                    // 🔴 **先随机抽 3 张再给玩家挑**（规则书 `:475`，见 `ChooseShowMax`）。
                    bool bigPool = op.ChooseSrc == "pool" || op.ChooseSrc == "deck";
                    if (bigPool && cands.Count > ChooseShowMax)
                    {
                        int all = cands.Count;
                        cands = DrawForDisplay(cands, ChooseShowMax);
                        Ctx.Log($"（选牌面板：{srcName}里共 {all} 张候选 —— 按规则书 `:475` "
                              + $"**随机抽出 {cands.Count} 张**给玩家挑）");
                    }
                    _askCands = cands;
                    // ⚠️ 挂在**面板自己**下面 —— `CardChoicePanel` 是按 localPosition 排这一行的
                    foreach (var c in cands)
                        _chooseViews.Add(CardView.Create(_choosePanel.transform,
                                                         ToCardData(c, c.Faction), "Choose_" + c.Name));
                    hasOptions = true;
                }
                else
                {
                    // 候选空在这一族里是**正常结局**（规则书 :233）—— 引擎那边也会如实报，这里别开空面板
                    Ctx.Log($"（选牌面板：这次在{srcName}里没有候选 —— {why}；这一处不问了）");
                }
            }
            else if (op.Verb == "chooseone")
            {
                // 「三选一」`Choose one: A; B or C` —— 候选是**卡面自带的 2~3 项**（`op.Payload`，`|` 分隔）。
                // 出处：`battle.gd:3679`（`AI=自动第一项; 玩家=弹窗`）—— 原版**玩家是要弹窗的**。
                var opts = RuleCore.ChooseOneOptions(op);
                if (opts != null)
                    foreach (var s in opts)
                        if (!string.IsNullOrWhiteSpace(s))
                            _chooseViews.Add(MakeChoiceCard(s.Trim(), PendingCard, "ChooseOne_"));
                hasOptions = _chooseViews.Count > 0;
                // 🔴 **2026-09-18：标题不再分叉** —— 原来这里写 `title = ChoosePanel.ChooseOneTitle`
                //    （「选择一项」），那是**我们自造的**。原版 `ChooseCardMenu` 只有一个标题对象
                //    （`ChooseText`，TMP 文本 `Choose one card`），运行时按
                //    `"Battle/ChooseCard/Instructions-" + actingCardId` **换词条**（查不到就回落到无后缀那条）
                //    —— 见 `ChooseCardMenu__SetUpTitleText.c`。⇒ 统一用 `DefaultTitle`。
                //    证据与出处：`资料/普查产出_0918/第18行_UI三小条_规格.md` §④。
                if (!hasOptions) Ctx.Log("（选牌面板：这一处 `chooseone` 一个选项都没解出来 —— 不问了）");
            }
            else if (op.Verb == "chooseeffect")
            {
                // 🔴 **2026-10-17（A905）删掉了 `ChooseEffectIsHand(op)` 那条短路。**
                //    它原来写的是「`Infinite Biomorphologies` 的『给手牌里的全部部队』这一版没做
                //    ⇒ 开了面板也没用」—— **那个理由已经过期**：引擎侧 **2026-09-16 就做完了**
                //    （`DoChooseEffect` 的 `handScope` 分支 → `RuleCore.AttachHandEffect`
                //     → **`CardInstance.HandEffects`**（每个实例一份）→ `RuleCore.ApplyHandBuffs`），
                //    只有面板这一侧还挂着「不问了」
                //    ⚠️ **2026-10-18（`A886` ①）就地订正**：这句原来写的是
                //      「`GrantHandBuff` → **`ctx.HandBuffs`** → `ApplyHandBuffs`」——
                //      **`ctx.HandBuffs` 这张对局级的表已经不在了**（`A885` 搬走，
                //      理由与判据写在 `RuleEngine/Core/BattleContext` 里那段 `A885` 订正痕「这里不再有那张表」）；
                //      旧名 `GrantHandBuff` 也已改名 `RuleCore.AttachHandEffect`。
                //    ⇒ 玩家**永远选不了那三项**、引擎按 `ctx.Rng` 等概率挑（**静默替玩家做决定**）。
                //    ⚠️ `hand` 与 `self` / `give` 的**唯一**差别在**结算落点**（给手牌 vs 给目标），
                //       **开面板这件事一模一样** ⇒ 不该在这里分叉。
                //    ⚠️ 提示语仍走 `DefaultTitle`（原版 `ChooseCardMenu` 只有一个标题对象，
                //       运行时按 `Battle/ChooseCard/Instructions-<uniqueId>` 换词条 —— 见 `:SetUpTitleText`）。
                //    判据全文：`资料/普查产出_1017/W_B14_选牌一族.md` §⑥。
                // 池子按**正在结算的那张卡的名字**查（`ctx.PlayingCard`）—— 要在**打出之前**问，
                // 所以这里用**手牌里那张**的名字（两者是同一张，同一局内不会变）。
                var pc = PendingCard;
                var pool = RuleCore.ChooseEffectOptions(pc != null ? pc.Name : null);
                if (pool == null || pool.Length == 0)
                {
                    Ctx.Log($"（选牌面板：「{(pc != null ? pc.Name : "?")}」**没登记效果池** —— 这一处不问了）");
                }
                else
                {
                    foreach (var e in pool)
                    {
                        var text = !string.IsNullOrEmpty(e.Card) ? e.Card : e.Label;
                        _chooseViews.Add(MakeChoiceCard(text, pc, "ChooseEffect_"));
                    }
                    hasOptions = _chooseViews.Count > 0;
                }
                // 🔴 **2026-09-18：标题不再分叉**（同上面 `chooseone` 那一处）——
                //    原来这里写 `title = ChoosePanel.ChooseEffectTitle`（「选择一个效果」），是我们自造的。
            }
            else
            {
                // ⚠️ **如实报**：没有面板的 ask 点 ⇒ 仍然由引擎等概率挑（**不许静默**）
                Ctx.Log($"（选牌面板：`{op.Verb}` 这一族**还没有面板** —— 这一处仍由引擎等概率挑）");
            }

            if (!hasOptions)
            {
                // 跳过这一处：**不往队列里塞东西**（塞了会让后面那处**用错答案**）
                NextAsk();
                return;
            }

            _choosePanel.OnDone = OnChooseDone;
            // `uniqueId` = **正在结算的那张卡**的 id（原版 `SetUpTitleText` 收的就是 actingCardId）——
            // `PendingCard` 就是它；取不到（没有来源卡那种）就传 null ⇒ 回落 `DefaultTitle`（不静默编词条）。
            _choosePanel.Open(_chooseViews, title, PendingCard != null ? PendingCard.Id : null);
            SetHint(op.Verb == "choosecard" ? "选一张牌，然后点「继续」" : "选一项，然后点「继续」");
        }

        void OnChooseDone(List<int> picks)
        {
            if (picks != null && picks.Count > 0)
            {
                Ctx.ChoosePicks.Enqueue(picks[0]);
                // 🔴 **两条队列必须同进同出**（`BattleContext.ChooseCardIds` 的 ①）——
                //    `choosecard` 这一处**额外**报一个「选中的是哪张卡」（`CardDef.Id`）。
                //    ⚠️ 报 `Id` 而不是只报下标：面板可能只摆了**抽出来的 3 张**
                //       （规则书 `:475`），下标对不上引擎手里的完整候选表。
                //    别的 ask 点（`chooseone` / `chooseeffect`）没有这一格、也不该有。
                if (_askCands != null)
                    Ctx.ChooseCardIds.Enqueue(picks[0] >= 0 && picks[0] < _askCands.Count
                                              ? _askCands[picks[0]].Id : "");
            }
            _askCands = null;
            _choosePanel.Close();
            NextAsk();
        }

        void NextAsk()
        {
            _pendingAsk++;
            if (_pendingAsk < _pendingAsks.Count) { ShowAsk(); return; }

            // 🆕 2026-10-17（A904）：接着做的那一次动作**由 `BeginAsk` 带来**（出牌 / 主动技能两种）——
            //    先把值捕获下来再清字段，不然闭包跑的时候拿到的是空的。
            var go = _afterAsks; _afterAsks = null;
            _pendingIdx = -1; _pendingInst = null;
            _pendingAsks = new List<EffectOp>(); _pendingAsk = 0;
            ClearChooseViews();

            if (go == null)
            {
                // 理论上到不了（每一批都由 `BeginAsk` 开）——**出声**，别静默吞掉这一手
                Debug.LogError("[Battle] 选牌面板这一批 ask 没有后续动作（`BeginAsk` 没设 `_afterAsks`）"
                             + " —— 这一手**不会落地**");
                return;
            }
            go();
        }

        void ClearChooseViews()
        {
            for (int i = 0; i < _chooseViews.Count; i++)
                if (_chooseViews[i] != null) Kill(_chooseViews[i].gameObject);
            _chooseViews.Clear();
        }

        /// <summary>自检用：走**和真实点击同一条路**选第 <paramref name="i"/> 个候选</summary>
        public bool SimulateChoosePick(int i)
        {
            if (_choosePanel == null || !_choosePanel.Visible) return false;
            return _choosePanel.HandleClick(_choosePanel.CardWorldPos(i));
        }

        /// <summary>自检用：走**和真实点击同一条路**点「继续」</summary>
        public bool SimulateChooseDone()
        {
            if (_choosePanel == null || !_choosePanel.Visible) return false;
            _choosePanel.HandleClick(_choosePanel.DoneWorldPos);
            return true;
        }

        /// <summary>
        /// 自检用：走**面板那条真实路径**打出手牌第 <paramref name="idx"/> 张（会先把该问的问完）。
        /// ⚠️ 和 <see cref="SimulatePlay"/> 的区别就在这儿 —— 那个**直接调引擎、绕过面板**，
        ///    所以拿它验不了「面板真的弹出来」这件事。
        /// </summary>
        public bool SimulatePlayViaPanel(int idx, int slot)
        {
            var v = HandViewAt(idx);
            if (v == null) return false;
            BeginPlay(v, idx, slot);
            return true;
        }

        /// <summary>自检用：看日志面板</summary>
        public BattleLogPanel BattleLog { get { return _logPanel; } }
        /// <summary>自检用：看日志那颗按钮现在用的图（应 `40k_UI_bt_battlelog`）</summary>
        public string CemeteryBtnTex
        {
            get { return (_cemeteryBtn != null && _cemeteryBtn.Texture != null) ? _cemeteryBtn.Texture.name : "<无>"; }
        }

        /// <summary>自检用：某个补摆件的中心（按原版 1920×1080 绝对 px，**y 从上**）。
        /// 找不到返回 (-1,-1) —— 这样断言能直接和资料里的 rect 比。</summary>
        public Vector2 HudExtraPosPx(string name)
        {
            for (int i = 0; i < _hudExtras.Count; i++)
            {
                var q = _hudExtras[i];
                if (q == null || q.name != name) continue;
                var n = LayoutSpace.ToNormalized(q.transform.localPosition);
                return new Vector2(n.x * 1920f, (1f - n.y) * 1080f);
            }
            return new Vector2(-1f, -1f);
        }

        /// <summary>🆕 **2026-10-13（A513）** 自检用：某个补摆件中心的**世界坐标**（细口径那一档用它）。
        /// 与 <see cref="HudExtraPosPx"/> 的分工：那个是**粗口径**（px、1.5 px 容差），
        /// 这个是**细口径**（直接给 `transform.position`，供 1e-4 世界单位档的断言比，
        /// 语义同 <see cref="CameraResetButtonWorldPos"/>）—— 它**不假设 `hudRoot` 自己在原点**
        /// （断言侧配 `hudRoot.TransformPoint(...)`，与 `HudAbs` → `HudImageTex` → `ImageQuad.Create`
        /// 写的那个 `localPosition` 逐字同一条换算）。
        /// 找不到返回 `Vector3.zero`（断言侧会表现为「偏得离谱」⇒ 红，不会静默通过）。</summary>
        public Vector3 HudExtraWorldPos(string name)
        {
            for (int i = 0; i < _hudExtras.Count; i++)
            {
                var q = _hudExtras[i];
                if (q != null && q.name == name) return q.transform.position;
            }
            return Vector3.zero;
        }

        /// <summary>自检用：这一批补摆件里**取不到图**的（应当一个都没有 —— 缺图就是静默失败）</summary>
        public int HudExtrasMissingArt()
        {
            int n = 0;
            for (int i = 0; i < _hudExtras.Count; i++)
                if (_hudExtras[i] == null || _hudExtras[i].Texture == null) n++;
            return n;
        }

        /// <summary>自检用：某件比 HUD 图那层（`HudImageZ`）**靠前多少**（正数 = 更靠前 = 压在默认层上面）。
        /// 自检拿它钉 z 序 —— 同 z 的两张图谁压谁由渲染顺序决定，**看图看不出来**。</summary>
        public float HudExtraZDelta(string name)
        {
            for (int i = 0; i < _hudExtras.Count; i++)
            {
                var q = _hudExtras[i];
                if (q == null || q.name != name) continue;
                return HudImageZ - q.transform.localPosition.z;
            }
            return 0f;
        }

        // ==================================================================
        //  称号（原版 `PlayerProfileUIController.SetProfileTitle`）
        // ==================================================================

        /// <summary>
        /// 设置双方的称号（`null` / 空串 = 没有称号）。
        /// 🔴 **判据照原版**（`decomp_full/PlayerProfileUIController__SetProfileTitle.c`，机器码级）：
        ///     `SetActive(titleGO, !string.IsNullOrEmpty(title))` —— 称号为空时
        ///     **底条和文字一起关**（不是「画一条空底条」）。
        /// 实况印证：`runtime_ui_dump_drive_0912.tsv` 里 `TitleBackground` 的 `activeSelf = False`
        /// （原版关服、玩家档案没有称号）⇒ **默认就该是关着的**。
        /// ⚠️ 单机没有玩家资料（原版那套在服务端）⇒ 正常流程**永远**传 null 进来，
        ///    这一件在单机里恒不显示。留着它是为了**显式表达原版那条判据**（不许静默失败）。
        /// </summary>
        public void SetTitle(string me, string foe)
        {
            _titleMeText = me ?? "";
            _titleFoeText = foe ?? "";
            ApplyTitle(_titleMe, _titleBgMe, _titleMeText);
            ApplyTitle(_titleFoe, _titleBgFoe, _titleFoeText);
        }

        static void ApplyTitle(Label l, ImageQuad bg, string text)
        {
            bool show = !string.IsNullOrEmpty(text);
            // ⚠️ **先激活、再写字**：TMP 在**非激活**对象上量不出尺寸（`CLAUDE.md` §三 那条坑：
            //    没激活时 `textBounds` 是垃圾，会把标签宽度顶到上限、一个字看不见）。
            if (l != null) { l.gameObject.SetActive(true); l.SetText(text); l.gameObject.SetActive(show); }
            if (bg != null) bg.gameObject.SetActive(show);
        }

        /// <summary>自检用：称号那一层现在显示着没有（`true` = 我方）。判据见 `SetTitle`。</summary>
        public bool TitleVisible(bool mine)
        {
            var l = mine ? _titleMe : _titleFoe;
            return l != null && l.gameObject.activeSelf;
        }
        /// <summary>自检用：称号底条那一层现在显示着没有（应**与文字同步**）。</summary>
        public bool TitleBgVisible(bool mine)
        {
            var b = mine ? _titleBgMe : _titleBgFoe;
            return b != null && b.gameObject.activeSelf;
        }
        /// <summary>自检用：称号那段字现在的文本</summary>
        public string TitleTextOf(bool mine) { return mine ? _titleMeText : _titleFoeText; }
        /// <summary>自检用：称号文字的**中心**（原版 1920×1080 绝对 px，y 从上）</summary>
        public Vector2 TitlePosPx(bool mine)
        {
            var l = mine ? _titleMe : _titleFoe;
            if (l == null) return new Vector2(-1f, -1f);
            var n = LayoutSpace.ToNormalized(l.transform.localPosition);
            return new Vector2(n.x * 1920f, (1f - n.y) * 1080f);
        }

        /// <summary>自检用：任务点数字的文本（原版 `QPText`）。⚠️ 引擎没有任务点 → 恒为 '0/3'。
        /// ⚠️ 它**只在暗黑天使的局里可见**（物件照建、`SetActive` 切）—— 要判显隐请看
        /// <see cref="QuestPointsVisible"/></summary>
        public string QpText { get { return _qpTextMe != null ? _qpTextMe.Text : "<无>"; } }

        /// <summary>
        /// 自检用：这一方的**任务点那一组**（纹章 + 接片 + 数字）现在可不可见。
        ///
        /// 原版是 `ManaTypeHolder.Toggle` → `SetActive`，**只有暗黑天使为真**
        /// （见 <see cref="ShowsQuestPoints"/> 的机器码级出处）。
        /// ⚠️ 三件必须**一起**开关 —— 只切一半是**静默**的错（画面看着「有东西」，
        /// 但那东西不该在），所以这里要求三个全真才算「可见」。
        /// </summary>
        public bool QuestPointsVisible(bool mine)
        {
            var icon = mine ? _myQuestIcon : _foeQuestIcon;
            var join = mine ? _myQuestJoin : _foeQuestJoin;
            var text = mine ? _qpTextMe   : _qpTextFoe;
            if (icon == null || join == null || text == null) return false;
            return icon.gameObject.activeSelf && join.gameObject.activeSelf && text.gameObject.activeSelf;
        }

        /// <summary>
        /// **阵营资源那两件显不显示** —— 判据的**唯一一处**（灵魂石 / 信仰；任务点见 <see cref="ShowsQuestPoints"/>）。
        ///
        /// ✅ **2026-09-18 定案：原版就是「按阵营查表」，而且那张表我们早就有了。**
        /// 本轮读到了**调用方** —— 原注释写「谁调 `ManaManager` 那三个 —— 0 命中」，**那句作废**：
        /// `PlayerManager__ResetMana.c:100-145` 里就是三条
        /// `ManaManager.Toggle{SpiritStone,Faith,QuestPoints}Mana(manager, RawCardScript.UsesX(督军卡), 值)`，
        /// 而 `RawCardScript__Uses{SpiritStone,Faith,QuestPoints}.c` **各只有 7 行**、只做一件事：
        /// <code>
        ///   UsesSpiritStone(x) { return *(int*)(x + 0x2c) == 0x1e; }   // 30  = SaimHann
        ///   UsesFaith(x)       { return *(int*)(x + 0x2c) == 0x50; }   // 80  = Sororitas
        ///   UsesQuestPoints(x) { return *(int*)(x + 0x2c) == 0x6e; }   // 110 = DarkAngels
        /// </code>
        /// ⇒ **`+0x2c` 就是阵营 id**，三个数与本文件 `ShowsQuestPoints` 注释里那三条 `cmp` **完全一致**
        ///   ⇒ 原版判据 = **阵营是不是这三个**，**与「当前有没有值」无关**。
        ///
        /// 🔴 **原来用「有值就显示」（`value > 0`）是错的，而且错在「静默」那一类**：
        ///   SaimHann 玩家**开局 0 灵魂石**时原版**会显示 `0`**，我们**整组不显示**（打起来才突然冒出来）；
        ///   反方向不会发生（别的阵营拿不到灵魂石 / 信仰）⇒ 是**该出现的不出现**，不是多显示。
        /// ⚠️ 三件**互斥**（三个阵营两两不同）⇒ 不会同时出现。
        /// </summary>
        public static bool ShowsSpiritStone(string faction) { return faction == SpiritStoneFaction; }
        /// <inheritdoc cref="ShowsSpiritStone"/>
        public static bool ShowsFaith(string faction) { return faction == FaithFaction; }

        /// <summary>自检用：这一方的信仰那一组（图 + 数字）现在可不可见。**两件必须一起开关**。</summary>
        public bool FaithVisible(bool mine)
        {
            var icon = mine ? _myFaithIcon : _foeFaithIcon;
            var text = mine ? _myFaithText : _foeFaithText;
            if (icon == null || text == null) return false;
            return icon.gameObject.activeSelf && text.gameObject.activeSelf;
        }
        /// <summary>自检用：这一方的灵魂石那一组（底板 + 宝石 + 数字）现在可不可见。**三件一起开关**。</summary>
        public bool SpiritStoneVisible(bool mine)
        {
            var icon = mine ? _myStoneIcon : _foeStoneIcon;
            var gem = mine ? _myStoneGem : _foeStoneGem;
            var text = mine ? _myStoneText : _foeStoneText;
            if (icon == null || gem == null || text == null) return false;
            return icon.gameObject.activeSelf && gem.gameObject.activeSelf && text.gameObject.activeSelf;
        }
        /// <summary>自检用：信仰那个数字显示的是什么</summary>
        public string FaithText(bool mine)
        {
            var t = mine ? _myFaithText : _foeFaithText;
            return t != null ? t.Text : "<无>";
        }
        /// <summary>自检用：灵魂石那个数字显示的是什么</summary>
        public string SpiritStoneText(bool mine)
        {
            var t = mine ? _myStoneText : _foeStoneText;
            return t != null ? t.Text : "<无>";
        }
        /// <summary>自检用：灵石**图标**那一枚 quad（`MoveParticlesToTarget` 的吸附目标就是它）。
        /// 拿不到返回 null（没建 / 该阵营不显示）。</summary>
        public Transform StoneIconForTest(bool mine)
        {
            var q = mine ? _myStoneIcon : _foeStoneIcon;
            return q != null ? q.transform : null;
        }
        /// <summary>自检用：信仰/灵魂石那几张图取到了没有（取不到 = 美术没同步进来）</summary>
        public string FaithTex { get { return (_myFaithIcon != null && _myFaithIcon.Texture != null) ? _myFaithIcon.Texture.name : "<无>"; } }
        public string StoneGemTex { get { return (_myStoneGem != null && _myStoneGem.Texture != null) ? _myStoneGem.Texture.name : "<无>"; } }

        void SetFactionResourceVisible(bool myFaith, bool foeFaith, bool myStone, bool foeStone)
        {
            if (_myFaithIcon != null) _myFaithIcon.gameObject.SetActive(myFaith);
            if (_foeFaithIcon != null) _foeFaithIcon.gameObject.SetActive(foeFaith);
            if (_myFaithText != null) _myFaithText.gameObject.SetActive(myFaith);
            if (_foeFaithText != null) _foeFaithText.gameObject.SetActive(foeFaith);
            if (_myStoneIcon != null) _myStoneIcon.gameObject.SetActive(myStone);
            if (_foeStoneIcon != null) _foeStoneIcon.gameObject.SetActive(foeStone);
            if (_myStoneGem != null) _myStoneGem.gameObject.SetActive(myStone);
            if (_foeStoneGem != null) _foeStoneGem.gameObject.SetActive(foeStone);
            if (_myStoneText != null) _myStoneText.gameObject.SetActive(myStone);
            if (_foeStoneText != null) _foeStoneText.gameObject.SetActive(foeStone);
        }


        /// <summary>自检用：加时标记在不在（**默认应当是关着的** —— 我们还没有加时机制）</summary>
        public bool OvertimeVisible
        {
            get { return _overtime != null && _overtime.gameObject.activeSelf; }
        }
        /// <summary>自检用：加时标记那张图取到了没有</summary>
        public string OvertimeTex
        {
            get { return (_overtime != null && _overtime.Texture != null) ? _overtime.Texture.name : "<无>"; }
        }
        /// <summary>自检用：加时**全屏 splash** 在不在（原版 `OvertimeSplashText`）</summary>
        public bool OvertimeSplashVisible
        {
            get { return _overtimeSplashRoot != null && _overtimeSplashRoot.activeSelf; }
        }
        /// <summary>自检用：splash 当前的透明度（原版那串 DOTween 淡入/停/淡出的终值）</summary>
        public float OvertimeSplashAlpha
        {
            get { return _overtimeSplashText != null ? _overtimeSplashText.color.a : -1f; }
        }
        /// <summary>自检用：splash 上那行字**渲染出来多大**（世界单位）。
        /// 盯的是「字号没算错」—— 踩过：把原版的 `m_fontSize = 80` 当成本工程的单位用，
        /// 结果一个字母占了大半屏（≈335 px）。屏宽 = 17.78 世界单位。</summary>
        public Vector2 OvertimeSplashTextSize
        {
            get { return _overtimeSplashText != null
                       ? new Vector2(_overtimeSplashText.WorldW, _overtimeSplashText.WorldH)
                       : Vector2.zero; }
        }
        /// <summary>🆕 **2026-10-18（W6）**：splash 上那行字**写的是什么**（原版词条 `Battle/Overtime/Title`）。</summary>
        public string OvertimeSplashText { get { return _overtimeSplashText != null ? _overtimeSplashText.Text : null; } }

        /// <summary>
        /// 把引擎**留档的**战斗日志（`Ctx.ActionLog`）翻成人话喂给面板 —— **新的在前**
        /// （原版就是从最新一条往下排）。卡名走 `Zh` 翻中文，查不到就原样显示英文（不静默丢）。
        ///
        /// ⚠️ 目标卡名必须**读事件里记下来的那个**（`BattleEvent.TargetCardId`）——
        ///    留档以后再回看时，那个格位早就换人了，去棋盘上查会查到错误的对象。
        /// </summary>
        void RefreshBattleLog()
        {
            // 🔴🔴 **2026-10-18（第十五轮 · `G5`）就地订正（铁律 5）：这三条结论查实了，⛔ 别再照旧的读。**
            //
            // **① 「0 颗挂 `Localize`」成立；「本地零词条」不成立。** 原版这一族的键**在代码里** ——
            //    `d:/2/tools/il2cpp_out/stringliteral.json`（`RVA = 地址 − 0x180000000`）里
            //    `Battle/Cemetery/` 前缀实测 **19 条**（= `资料/已知的坑.md` #20 的**第二种载体**）：
            //      `ActionAttackMelee` · `ActionAttackRanged` · `ActionAbility` · `ActionTargetedAbility` ·
            //      `ActionYouPlay` · `ActionOpponentPlays` · `ActionTargetedYouPlay` ·
            //      `ActionTargetedOpponentPlay` · `ActionYouPlayAmbush` · `ActionOpponentPlaysAmbush` ·
            //      `ActionYouDraw` · `ActionOpponentDraws` · `ActionSecretOrder` ·
            //      `ActionDisplayYourCardInHand` · `ActionDisplayEnemyCardInHand` ·
            //      `ActionYouCollectSpiritStone` · `ActionEnemyCollectsSpiritStone` · `ActionExitAmbush` ·
            //      `AmbushedTroop`。
            //
            // **② 🔴 它们挂在【活类】上，⛔ 不是 `G9` 判死掉的那个 `CemeteryLogManager`。** 本批逐条解出来了：
            //    · 值全部由 **`CemeteryManager.GetActionText`**（`decomp_full/CemeteryManager__GetActionText.c`）
            //      的 `switch(actionType)` 选出来 —— 本批把那个方法体里 **21 个 `DAT_` 逐个解成了词条名**
            //      （`RVA = 地址 − 0x180000000`，实读）。枚举 → 词条（`iVar1 = *param_2`，结构里第一个 int）：
            //      `5`→`ActionAttackMelee` · `6`→`ActionAttackRanged` · `0xF`→`ActionAbility` /
            //      `ActionTargetedAbility`（按 `param_2[10]` 那个引用是否为 null 二选一）·
            //      `0x14`→`ActionYouDraw` / `ActionOpponentDraws` ·
            //      `0x19`→`ActionSecretOrder` · `0x1E`→`ActionDisplay{Your,Enemy}CardInHand` ·
            //      `0x23`→`Action{You,Enemy}CollectSpiritStone` · `0x28`→`ActionExitAmbush` ·
            //      `10`→**`ActionYouPlay` / `ActionOpponentPlays` / `ActionTargetedYouPlay` /
            //      `ActionTargetedOpponentPlay` / `ActionYouPlayAmbush` / `ActionOpponentPlaysAmbush`（六选一）**；
            //      `CemeteryManager.GetActionWithParams` 里那条是 `AmbushedTroop`。
            //      拼法：`GetActionWithParams` 里 `String.Format` 的四个片段实读 = `<b><link="1,`(`0x42D28D8`) ·
            //      `"><u>`(`0x424F8F8`) · `</u></link></b>`(`0x42CFBE8`) + `RawCardScript.GetLocalizedCardName`。
            //    · **活类的证据链**：`CemeteryManager` = **TypeDefIndex 720**（`dump.cs:37046`），
            //      `cemeteryActions`（`+0x30`，`CemeteryLogSlider[]`）在 **13 个战场的场景里逐场都是满的**
            //      （arena1 实读：`bundle_scenes_scenes_battlearena1/MonoBehaviour_4491.json` 的
            //      `cemeteryActions` = 一整串非零 `m_PathID`；`emptyText` 那颗才是 0）；而
            //      `AddActionToCemetery` 就是把 `GetActionText` 的结果写进**那些行节点的 TMP**
            //      （`CemeteryManager__AddActionToCemetery.c:166-168`：取 `row[+0x28]` 那颗 TMP 的
            //      `set_text` 虚表槽 `+0x558`，并按 `+0x20` 那颗 `Image` 换 `playerActionBg`/`enemyActionBg`）。
            //      上游也在跑：`BattleManager._ResolveAttack_d__438__MoveNext.c:1243` 与
            //      `…_ResolvePlayActiveAbility_d__479__MoveNext.c` 都直接调 `AddAttackActionToCemetery`。
            //      ⇒ **这是发行版真在跑的一条链**（`G9` 判死的 `CemeteryLogManager` 是**另一个类**、
            //      TypeDefIndex 766，两者别混 —— 见 `Battle/BattleLogPanel.cs` 的 `Z2`/`Z3` 那两段）。
            //
            // **③ 但【今天还不能接】，卡在两处（都不是「影响小所以不做」）：**
            //    · 🔴 **原版记的是「谁做了什么动作」，我们 `RefreshBattleLog` 记的是【动作的后果】**
            //      （伤害 / 阵亡 / 回手 / 触发）。**现有事件里能直接推出原版键的只有 6 条**：
            //      `Attack`（+`Ranged`）→ `ActionAttackMelee`/`ActionAttackRanged` ·
            //      `Play`（+`Player`）→ `ActionYouPlay`/`ActionOpponentPlays` ·
            //      `CollectWaystone`（+`Player`）→ `ActionYouCollectSpiritStone`/`ActionEnemyCollectsSpiritStone`。
            //      原版那 **6 条 `Play` 细分**（靶向 2 + 伏击 2）我们**分不出来**（`BattleEvent` 的 `Play`
            //      不带目标、不带「这是伏击」），`Ability` → `ActionAbility`/`ActionTargetedAbility`
            //      那个二选一分不出来（要看 `param_2[10]` 那个目标引用）—— 都缺字段。
            //      剩下 **7 条**（`ActionYouDraw`/`ActionOpponentDraws` · `ActionSecretOrder` ·
            //      `ActionDisplay{Your,Enemy}CardInHand` · `ActionExitAmbush` · `AmbushedTroop`）
            //      **我们引擎根本不发对应事件** —— `RuleEngine/Core/BattleEvent.cs` 的 `EvtKind` 只有 12 种、
            //      **没有 `Draw` / `AmbushExit` / `SecretOrder`**（本批实点过全仓 `EvtKind.` 的发出点，逐种计数：
            //      Combat 那 12 种里 `GainQuest` 等三种也各有发出点，但抽牌/撤伏击/密令**一个都没有**）。
            //      ⇒ 要接全，**先要在 `RuleEngine/` 加 3 种事件 + 给 `Play`/`Ability` 补目标与伏击标记**
            //      （那是**别人手里的文件**，⛔ 不在本笔白名单）。
            //    · 🔴 **19 条键的文案值（中英）本地一条都没有** —— 原版显示串在**远端 I2 表**，
            //      本地 84 个 bundle 里没有 `localization_assets_all.bundle`；`数据/本地化/i18n/zh_CN.csv`
            //      按英文源串精确查过（`draws a card` / `you play` / `secret order` / `collects a…` 等）**0 命中**
            //      ⇒ 接上去 = **两列全是我们编的**。
            //    · 另外原版的日志是**整块面板**（`shade` + `DOLocalMoveX(initialX −1200 → finalX 87)`
            //      + 每行一张 `playerActionBg`/`enemyActionBg` 底图）—— 与我们这 8 行的形状也不同。
            //  ⇒ **结论：要做（不是「不做」），先做哪几条 → 先加那 3 种引擎事件、再把日志按原版重新分类**
            //    （表现层重构，`RefreshBattleLog` 就是落点）；**判据已全部就位**（上面那张枚举表 + 19 条键）。
            //  ⚠️ 上面那 12 条中文模板**照旧写死中文** ⇒ **英文档下战斗日志仍是中文**（这是同一个待办的
            //    另一半：它们没有原版 `mTerm`，接的时候要和这次重构一起决定键名，⛔ 别先自造一批键）。
            if (_logPanel == null || Ctx == null) return;
            _logEntries.Clear();
            var log = Ctx.ActionLog;
            for (int i = log.Count - 1; i >= 0 && _logEntries.Count < 8; i--)
            {
                var e = log[i];
                string who = e.Player == _me ? "我方" : "敌方";
                string card = Zh(e.CardId);
                string line;
                switch (e.Kind)
                {
                    case EvtKind.Play:
                        line = $"{who}打出「{card}」"; break;
                    case EvtKind.Deploy:
                        line = $"{who}「{card}」进入格位 {e.Slot + 1}"; break;
                    case EvtKind.Attack:
                        line = $"{who}「{card}」{(e.Ranged ? "远程" : "近战")}攻击「{Zh(e.TargetCardId)}」"; break;
                    case EvtKind.Hit:
                        line = e.Amount < 0
                             ? $"{who}「{card}」回复 {-e.Amount} 点生命"
                             : $"{who}「{card}」受到 {e.Amount} 点伤害";
                        break;
                    case EvtKind.Death:
                        line = $"{who}「{card}」阵亡"; break;
                    case EvtKind.Return:
                        // 回手/回牌库**不是阵亡** —— 日志上分开写（和 `PlayReturnFeel` 一个口径）
                        line = $"{who}「{card}」离开格位（回手牌或牌库）"; break;
                    case EvtKind.Ability:
                        line = $"{who}「{card}」发动技能"; break;
                    case EvtKind.Trigger:
                        line = $"{who}「{card}」触发「{e.Keyword ?? "效果"}」"; break;
                    default:
                        line = $"{who}「{card}」"; break;
                }
                _logEntries.Add(new BattleLogPanel.Entry
                {
                    CardId = e.CardId,
                    LinkText = card,        // ⚠️ **文字里印的是中文名**（`Zh(CardId)`）—— 链接包的是它，不是 `CardId`
                    Text = $"回合 {e.Turn}　{line}",
                    // 🆕 2026-09-29：**底板按「谁做的动作」换**（原版三张 `40k_battlelog_display_*`）。
                    //   判据 = `BattleEvent.Player`（0/1 = 归属方；`-1` = 没有归属 ⇒ neutral）。
                    Side = e.Player < 0 ? BattleLogPanel.RowSide.Neutral
                         : (e.Player == _me ? BattleLogPanel.RowSide.Player : BattleLogPanel.RowSide.Enemy),
                });
            }
            _logPanel.SetEntries(_logEntries);
        }

        /// <summary>卡名的**显示名** —— 过 <see cref="CardText.Zh"/> 那道语言闸的**唯一入口**。
        /// 有中文且**当前语档是中文** ⇒ 中文名，否则英文名（`c == null` ⇒ 原样返回 <paramref name="fallback"/>）。
        /// <para>🔴 **2026-10-18（A991/②③ 同一批）就地修一处同族缺陷（铁律 5）**：本文件原来有 **三处**
        /// 直接读 `CardDef.NameZh`、**绕过 `CardText.Zh`** ⇒ **英文档下照样印中文**
        /// （`Core/CardText.cs` 上一轮补的是它自己那两个口；这三处是同一个缺陷的另外几处）：
        /// ① <c>Zh(string)</c>（卡名 → 显示名，日志/计数那一族）；② <c>HeroDisplayName</c>（督军显示名）；
        /// ③ 聊天气泡那一族 8 个调用点（`_unitChat.Speak` 的 `fallbackName`）。</para>
        /// <para>⛔ **别再在调用点直接写 `card.NameZh`** —— 判据只有 `CardText.Name(id, nameZh)` 一处
        /// （它内部就是 `CardText.Zh` 那道闸），本方法只是把「按 `CardDef` 取」这件小事收成一份。</para></summary>
        static string CardDisplayName(CardDef c, string fallback)
        {
            if (c == null) return fallback;
            return CardText.Name(c.Name, c.NameZh);
        }

        /// <summary>卡名 → **显示名**。卡池里没有就**原样返回英文**（不静默丢成空串）。
        /// 🔴 **2026-10-18 就地补语言闸（铁律 5）**：本方法原来**无条件**返回 `d.NameZh` —— 那等于
        /// **绕过 `CardText.Zh`**（英文档下这一支照样印中文）⇒ 现在转发 <see cref="CardDisplayName"/>。
        /// ⚠️ **卡池里查不到时仍回英文原名**（与原行为一致）；查得到但那张卡没有中文名时，
        /// 会再落到 `CardText.Name(id)` 兜底（我们自己那 26 张卡走它，中文档下因此**比原来更准**）。</summary>
        string Zh(string name)
        {
            if (string.IsNullOrEmpty(name) || _pool == null) return name ?? "";
            return CardDisplayName(CardDatabase.Find(_pool, name), name);
        }

        // ==================================================================
        //  战斗日志：悬停卡名 ⇒ 弹一张卡
        //  原版那条链（全量反编译）：`CemeteryManager.CheckCardLink`（对行文字 `FindIntersectingLink` 命中）
        //  → `GetLinkID` → 索引它的动作表 → `DisplayCard` → `BasicCardUI.SetRawCardData`。
        //  我们这版：链接 ID = **英文卡名**（原版是「`0/1,动作索引`」那种动作表下标 —— 我们没有那张表，
        //  如实记这条差异）；可见文字（中文名）照原版用 `<b><u>` 包着（在 `BattleLogPanel.Linkify`）。
        // ==================================================================
        CardView _logCard;
        string _logCardKey;

        /// <summary>自检用：日志那张悬停卡现在开着吗（开着 = 它的卡名，没开 = null）</summary>
        public string LogHoverCardKey
        {
            get { return (_logCard != null && _logCard.gameObject.activeSelf) ? _logCardKey : null; }
        }

        /// <summary>自检用：日志面板上悬停到某一行的卡名链接（直接喂世界坐标 —— 批处理没有鼠标）。
        /// **和鼠标那条路调的是同一个 `TickLogCard`**。返回「现在弹着吗」。</summary>
        public bool SimulateLogHover(Vector3 wp) { return TickLogCard(wp); }

        /// <summary>自检用：日志面板那块（量它的位置 / 把行锚点换算过来用）。</summary>
        public BattleLogPanel LogPanel { get { return _logPanel; } }

        /// <summary>日志面板上悬停到卡名链接 ⇒ 弹卡。**和鼠标那条路调的是同一个函数**（自检直接喂世界坐标）。
        /// 返回「现在弹着吗」。</summary>
        public bool TickLogCard(Vector3 wp)
        {
            if (_logPanel == null || !_logPanel.Visible) { HideLogCard(); return false; }
            string key = _logPanel.LinkKeyAt(wp, cam);
            if (string.IsNullOrEmpty(key)) { HideLogCard(); return false; }
            if (_logCard != null && _logCard.gameObject.activeSelf && key == _logCardKey) return true;
            return ShowLogCard(key);
        }

        /// <summary>弹那一张卡。几何照原版（`CemeteryGroup/CardUI (1)`，**位置 100% 序列化、运行期只切
        /// `SetActive`**）：卡体 **226.0×359.8 px**，中心在面板左缘**左 53.04**、面板竖中线**上 20.49**。
        /// ⚠️ 卡池里找不到就**出声、不弹**（不静默、也不弹一张空卡 —— 空卡会走 `CardView` 的「空卡位」分支）。
        /// <paramref name="row"/> ≥ 0 = 这次是**点第 N 行**来的 ⇒ 卡跟着那一行的 y 摆
        /// （⚠️ 🔴 **2026-10-18（`G9`）**：那一句出自**死类** `CemeteryLogManager.ClickCemeterySlider`，
        /// 发行版不做这件事 —— 见 `BattleLogPanel.RowCardLocalPos` 的注解；这里如实标，⛔ 别读成「原版也这样」）。</summary>
        bool ShowLogCard(string key, int row = -1)
        {
            var def = FindCardByName(key);
            if (def == null)
            {
                Debug.LogWarning($"[Battle] 日志里那张卡「{key}」在卡池和这一局的牌里都找不到 ⇒ **不弹卡**（不静默）");
                HideLogCard();
                return false;
            }
            if (_logCard == null)
            {
                _logCard = CardView.Create(_logPanel.transform, ToCardData(def, def.Faction), "LogHoverCard");
                if (_logCard == null)
                {
                    // 🔴 **2026-10-10（A185）如实标注：这一支是【死支路】** —— `Core/CardView.cs` 的 `Create`
                    //    **无条件 `return v`**（全函数只有一条 `return`、没有任何 null 路径）⇒ 这个 `if` 永不成立。
                    //    ⚠️ **故意保留**（裁定 2026-10-07）：⛔ 不给生产类开「让 `Create` 返回 null」的测试注入口
                    //      —— 那等于为了让断言能红而往产品代码里加后门；而「防静默」这条守卫本身**写法是对的**，
                    //      将来 `Create` 真有了 null 路径，它立刻就是必要的那一句。
                    //    ⇒ 它**不是**「忘了删的代码」，**别删**。判据 → `项目任务.md` §三 第 29 条 **A185**。
                    Debug.LogWarning("[Battle] 日志那张悬停卡建不出来（`CardView.Create` 返回 null）—— 不静默");
                    return false;
                }
                _logCard.SetFace(CardFace.Full);
                _logCard.SetHighlight(CardHighlightState.Normal);
                // 压在面板整组（−4.0 一带、队列 4000）之上 —— 面板自己的图走 `OverlayQueue`
                CardFan.SetCardQueue(_logCard, BattleLogPanel.OverlayQ + 1);
            }
            _logCard.gameObject.SetActive(true);
            _logCard.SetData(ToCardData(def, def.Faction));
            float scale = BattleLogPanel.CardBodyH / (CardView.Height * 108f);
            _logCard.SetPose(row >= 0 ? _logPanel.RowCardLocalPos(row, BattleLogPanel.ZHoverCard)
                                      : _logPanel.HoverCardLocalPos(BattleLogPanel.ZHoverCard), 0f, scale);
            _logCardKey = key;
            return true;
        }

        void HideLogCard()
        {
            if (_logCard != null) _logCard.gameObject.SetActive(false);
            _logCardKey = null;
        }

        // ==================================================================
        //  🆕 2026-10-18（`A940` 尾账 `Z2` + `Z3`）：**「点日志面板第 N 行」**
        // ==================================================================
        //
        //  🔴🔴 **2026-10-18（`G9`）判据【就地订正】（铁律 5）—— 原判据引用的是【死代码】。**
        //    上一轮（`G4`）的判据是 `CemeteryLogManager__ClickCemeterySlider.c`。本轮把它读完、并顺着
        //    核了整条链，结论：**`CemeteryLogManager`（`dump.cs:38986` TypeDefIndex 766）这一族
        //    在发行版里【没有实例】**。四条独立证据（全文写在 `Battle/BattleLogPanel.cs` 那段
        //    「🆕 2026-10-18（`A940` 尾账 `Z2`/`Z3`）」里，此处只留结论）：
        //      ① 全库 24.7 万文件扫 `cemeteryGroup` / `centralCemetery` ⇒ **0 命中**；
        //      ② 面板上那颗 `MonoBehaviour_4491` 是**另一个类 `CemeteryManager`**（TypeDefIndex 720）；
        //         13 个战场的 `CemeteryLogPanel` **组件数都是 6、形状全同**（逐场比过）⇒ 都没有它；
        //      ③ 行节点的序列化 uGUI 事件指向 `CemeteryLogSlider.OnSliderChanged`/`OnDragEnd`
        //         —— **这两个方法在元数据里不存在**（那条链是残留）；
        //      ④ 素材侧没有「选中行高亮」的图（96 条 pid→名字索引里日志相关的只有
        //         `40k_UI_bt_battlelog` / `display_{player,enemy,neutral}` / 四条 `frame_*`）。
        //
        //  ✅ **发行版里真正跑的那条（活判据）**：
        //   · `BattleManager__CanShowCemetery.c:37-70` —— 非教程局**恒 1**（不看开关）；
        //     教程局才读 `currentTutorialStage + 0x29`（= `TutorialStage.hideCemetery`）：`== 0 ⇒ 1`、否则 0。
        //     ⚠️ 它前面还有一道 `ObscuredBool(+0x25c)` 闸（R2 §6·9 记「从哪来未查清」）——
        //        我们这边**没有对应物** ⇒ 不建（如实记，见 `ApplyTutorialVisibility`）。
        //     🔴 **但它的唯一消费者是那个死类**（`ClickCemeterySlider.c:75`）⇒ 本闸门在我们这边
        //     **没有原版活消费点**，是「照那个字段语义做的」；如实标。
        //   · **点行弹卡** = `CemeteryManager__ClickCardLink.c`（悬停版 = `CheckCardLink.c`）：
        //     逐行对 TMP 做 `FindIntersectingLink` → `GetLinkID()` → `Split(',')` →
        //     `BattleCardManager.GetCardFromUniqueId` → **`DisplayCard`（一颗 `BasicCardUI` = 一张卡）**。
        //
        //  🔴 **落地边界（如实记）**：
        //    · **弹【一张】卡** —— 与活判据一致（上一轮那套「三张 + 整摞跟行走 + 选中行高亮」
        //      出自死类 ⇒ **不做**，理由与偏移量实读值见 `BattleLogPanel.cs` 那段注释）。
        //    · **命中区比原版宽**：原版只认「行内文字上的链接」，我们认**整行底板的矩形**
        //      （`RowAt`）—— 批处理喂不了真鼠标，而这条入口是上一轮建的；要收窄得动
        //      `TryClickLogRow` 与它那几条断言 ⇒ 记成账，⛔ 不在本笔（`G9`）。

        /// <summary>`hideCemetery` 这一档的**闸门**（原版 `BattleManager.CanShowCemetery`）。
        /// **非教程局恒 true**（原版那一支直接 `return 1`，连开关都不看）；
        /// 教程局里 `hideCemetery == 0 ⇒ true`、`!= 0 ⇒ false`。
        /// ⚠️ 6 关 `hideCemetery` **全 0** ⇒ 今天恒 true（零可见影响），但闸门本身是**真判据**、
        ///    不是空壳：把它反过来（`hideCemetery==1 ⇒ false`）断言会当场红。</summary>
        public bool CemeteryRowClickAllowed
        {
            get
            {
                var st = Ctx != null && Ctx.Tutorial != null ? Ctx.Tutorial.Stage : null;
                if (st == null) return true;          // 非教程局：原版 `CanShowCemetery` 直接 return 1
                return !st.hideCemetery;
            }
        }

        /// <summary>自检用：`CanShowCemetery` 现在算出来真不真。</summary>
        public bool CemeteryClickAllowedForTest { get { return CemeteryRowClickAllowed; } }

        /// <summary>**点的这一下落在日志面板的某一行上就办掉它**。
        /// 🔴 **2026-10-18（`G9`）**：这一段原来是照**死类** `CemeteryLogManager.ClickCemeterySlider` 写的
        /// （发行版里那个类没有实例 ⇒ 那一条链不会跑）；删了那句误导的「原版 `ClickCemeterySlider`」，
        /// **发行版**里「点行弹卡」的活判据 = `CemeteryManager__ClickCardLink.c`（见上面 `Z2`/`Z3` 那段）。
        /// 返回 true = 这一下**被吃掉了**（调用方**不要再**把它当「点面板外 = 关面板」）。
        ///
        /// 分支逐条：
        ///   · 面板没开 / 没点在任何一行上 ⇒ false（**没接手**）；
        ///   · 命中的那一行**没提卡**（空行 / 那一行动作没有卡）⇒ **吃掉但什么都不做**
        ///     （发行版 `ClickCardLink` 那条链在 `GetCardFromUniqueId` 取不到卡时也是直接 `return`）；
        ///   · `hideCemetery` 把闸门关上 ⇒ **吃掉、不弹卡**。⚠️ 这条闸门的**唯一原版消费者就是那个死类**
        ///     （`ClickCemeterySlider.c:75`）⇒ 它是「照那个字段的语义做的」，我们没有它的活消费点（如实标）。
        /// </summary>
        public bool TryClickLogRow(Vector3 wp)
        {
            if (_logPanel == null || !_logPanel.Visible) return false;
            int row = _logPanel.RowAt(wp);
            if (row < 0) return false;
            if (!CemeteryRowClickAllowed)
            {
                // ⛔ 不静默：这一档只有「教程关把 hideCemetery 打开」时才可能为假（数据里 6 关全 0）
                Debug.Log($"[Battle] 日志第 {row + 1} 行的点击**被 `hideCemetery` 挡掉**"
                        + "（该字段语义的出处 = `CemeteryLogManager.ClickCemeterySlider.c:75-76`："
                        + "`CanShowCemetery` 假就整个 return；⚠️ 那个类是死类，见 `G9` 的订正）");
                return true;
            }
            _logPanel.SelectRow(row);
            string key = _logPanel.RowCardKey(row);
            if (string.IsNullOrEmpty(key))
            {
                Debug.Log($"[Battle] 点了日志第 {row + 1} 行，但这一行没提卡（空行 / 那不是卡的动作）⇒ 不弹卡");
                HideLogCard();
                return true;
            }
            Debug.Log($"[Battle] 点日志第 {row + 1} 行 ⇒ 弹这张卡（发行版的活判据 = "
                    + "`CemeteryManager.ClickCardLink`：行内链接 → `BattleManager.DisplayCard`（一张卡））");
            ShowLogCard(key, row);
            return true;
        }

        /// <summary>自检用：直接点第 `i` 行（批处理里 `WorldPointer()` 是死点，走 `TryClickLogRow` 喂不进去）。
        /// **判据与产品那条路同一个函数**（喂的是那一行底板的**真实世界中心**，不是编一个点）。</summary>
        public bool ClickLogRowForTest(int i)
        {
            if (_logPanel == null || !_logPanel.Visible) return false;
            Vector3 wp;
            if (!_logPanel.RowCenterWorld(i, out wp)) return false;
            return TryClickLogRow(wp);
        }
        /// <summary>自检用：日志面板上「最近一次点中的那一行」（`-1` = 没点过）。</summary>
        public int LogSelectedRow { get { return _logPanel != null ? _logPanel.SelectedRow : -1; } }

        /// <summary>按**卡名**找卡表项：先查卡池，再查**这一局双方手里的牌**（手牌 / 牌库 / 弃牌堆 / 场上）。
        ///
        /// 🔴 **为什么必须有第二步**：日志里那些卡**本来就是这一局里的卡**（事件是它们发出来的），
        ///   而**自设计阵营**（`StarterCards` 那两套）**不在 `_pool` 里** ⇒ 只查卡池会「找不到」。
        ///   2026-09-29 实测就是这么栽的：`Ember Archer`（自设计阵营）在卡池里 0 命中，
        ///   而它明明就在棋盘上。⚠️ 跨阵营同名卡按**先手牌/牌库、再两边**的顺序取第一张 ——
        ///   日志那行本身也没记阵营（`BattleEvent.CardId` 只有名字），这是能拿到的最准的一份。</summary>
        CardDef FindCardByName(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            var d = _pool != null ? CardDatabase.Find(_pool, key) : null;
            if (d != null) return d;
            if (Ctx == null) return null;
            for (int p = 0; p < 2; p++)
            {
                var ps = Ctx.Players[p];
                if (ps == null) continue;
                foreach (var inst in ps.Hand) if (inst != null && inst.Card != null && inst.Card.Name == key) return inst.Card;
                foreach (var inst in ps.Deck) if (inst != null && inst.Card != null && inst.Card.Name == key) return inst.Card;
                foreach (var inst in ps.Discard) if (inst != null && inst.Card != null && inst.Card.Name == key) return inst.Card;
                for (int s = 0; s < BoardSpec.Size; s++)
                {
                    var u = ps.Board[s];
                    if (u != null && u.Card != null && u.Card.Name == key) return u.Card;
                }
            }
            return null;
        }

        /// <summary>自检用：模拟点右上角设置按钮。
        /// ⚠️ **不能要求指针在按钮上** —— 批处理下没有鼠标，`WorldPointer()` 是个死点，
        ///    真实输入那条路（`HandleSettings`）才需要判指针，自检这条只验「按下去会开」。</summary>
        public bool SimulateOpenSettings()
        {
            if (_settingsPanel == null) return false;
            _settingsPanel.Show();
            return _settingsPanel.Visible;
        }

        /// <summary>自检用：设置按钮本身是好的吗？—— 贴图对不对、它自己的命中矩形认不认自己</summary>
        public bool SettingsBtnReady
        {
            get
            {
                return _settingsBtn != null && _settingsBtn.Texture != null
                    && _settingsBtn.Texture.name == "UI_Settings_Icon"
                    && _settingsBtn.Contains(_settingsBtn.transform.position);
            }
        }

        public void Forfeit()
        {
            if (Ctx == null || Ctx.IsOver) return;
            RecRaw(RecKindForfeit, _me);          // 🆕 录像：投降也是一条要重放的动作
            // 🆕 2026-10-18（A913）：**本机点那颗「投降」钮** = 原版 `ClickExitBattle.c:59` 那一跳
            //   （`:47` 先 `BattleCommsManager.SendForfeit()`、`:49` `AddResignAction(1)`、
            //    然后 `DeadHero(我, BattleResult.Forfeit = 2)`）⇒ 码就是 **2**。
            //   ⚠️ **码不上网**（原版协议里从来没有它，见 `BattleResult` 的注）：对面收到
            //   `NetKind.Resign` 之后由**它自己**填 2（`ReceiveEnemyForfeit.c:37` 就是写死的 2）
            //   ⇒ 下面 `OnLocalResign()` 一个字节都不改。
            RuleCore.Forfeit(Ctx, _me, BattleResult.Forfeit);
            // 🆕 2026-09-26（N4）：联机局要把「我投降了」发对面（对面收到后 `Forfeit(ctx, 对面)`）
            if (_net != null) _net.OnLocalResign();
            SpeakConcede(_me);        // 认输也有台词（原版 `concede` 那一族）
            RefreshAll();
            UpdateHud();
        }

        // ==================================================================
        //  🔴 2026-10-16（W22）：打完一局之后的**出口**（原版 `BattleManager.Update` 收尾那两句）
        // ==================================================================
        //  判据全文 → `资料/普查产出_1016/判据_结算后出口.md`（第一权威 = 反编译）。三件事：
        //   ① **出口长什么样**：**鼠标左键点屏幕任意处** 或 **按 ESC**
        //      （`BattleManager__Update.c:116-122`；`GetMouseButtonDown(0)` **无坐标判定**、
        //       `0x1b` = 27 = `KeyCode.Escape`）。
        //   ② **什么时候才认这一下**：闸门（`+0x510` = `matchFinishedAndWaitingToLeave`）在
        //      **开门视频播完 + 0.15s** 那一刻置位（`_CloseBattleDoors_d__393__MoveNext.c:51-56 → :72`）。
        //      那一句整个被闸门包着（`if (*(char *)(param_1 + 0x510) != '\0') && (…)`）⇒
        //      **置位之前点/按都不理**。
        //   ③ **走哪儿去**：`BattleManager.LeaveBattle()` → 加载**主菜单场景**（`LeaveBattle.c:58`）。
        //  ⛔ **结算屏上没有按钮、也没有提示文字**（原版那棵子树零按钮零文字，两条独立实据见判据 §2.3）
        //     ⇒ 这里**不加按钮、不加文字**（铁律 11：与原版不符的要【完全复刻】）。
        //  ⛔ **也不是「等几秒自动回菜单」** —— 原版**没有**那条定时器（判据 §2.4：回主菜单的
        //     `LoadScene` 四处全部是显式触发）⇒ **不点就不走**，这里也不加。

        /// <summary>
        /// 结算后的出口那一支（**照原版 `BattleManager.Update` 的收尾那两句**）：
        /// 闸门开了之后，**左键任意处** 或 **ESC** ⇒ <see cref="LeaveBattle"/>。
        ///
        /// <para>🔴 **返回 true = 这一帧被它接管了**（同 `HandleSettings` 那一族的约定）。</para>
        /// <para>⚠️ **闸门没开时它连输入边沿都不读** —— 照原版那句 `if (*(char *)(param_1 + 0x510) == '\0') return;`。
        /// 这不是省事：`ClickedThisFrame()` 是**用掉就没了**的边沿，闸门关着时提前把它吃掉，
        /// 同一帧里别的处理者会以为「这一帧没点过」。</para>
        /// </summary>
        public bool HandleEndBattleExit()
        {
            if (!ExitReady) return false;
            // 原版 `Input.GetMouseButtonDown(0)` = **按下沿**、任意位置（这条边沿的语义见 `ClickedThisFrame`）
            bool clicked = ClickedThisFrame();
            bool esc = EscPressed();
            if (!clicked && !esc) return false;
            LeaveBattle();
            return true;
        }

        /// <summary>ESC 那一刻（原版 `UnityEngine_Input__GetKeyDownInt(0x1b)`；`0x1b` = 27 = `KeyCode.Escape`）。
        /// ⚠️ **批处理里 `Keyboard.current == null`**（没有键盘设备，同 `Mouse.current` 那条）⇒ 这一支
        /// 在自检里恒 false，所以照 `PointerHeldForTest` 那一族的先例给一个**自检钉死口**
        /// （<see cref="EscapePressedForTest"/> 给了值就按它算；**生产恒为 null**）。</summary>
        static bool EscPressed()
        {
            if (EscapePressedForTest.HasValue) return EscapePressedForTest.Value;
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        }

        /// <summary>自检专用：把「ESC 按下了」钉死（`null` = 走真实设备 —— **生产恒为 null**）。
        /// ⚠️ 它是**电平**不是边沿（设备那边 `wasPressedThisFrame` 才是一次边沿）—— 自检按需要显式设/清。</summary>
        public static bool? EscapePressedForTest;

        /// <summary>自检用：走一次**出口那一支的输入闸**（= `Update` 的 `Ctx.IsOver` 支里那一句；
        /// 判据只有一份，⛔ 别在自检里另写一遍「闸门开了没有」）。</summary>
        public bool TickEndBattleExitForTest() { return HandleEndBattleExit(); }

        /// <summary>
        /// 离开战场、回主菜单（= 原版 `BattleManager.LeaveBattle()`）。
        ///
        /// <para>判据：`BattleManager__LeaveBattle.c:58` `EverguildSceneManager__LoadScene("MainMenu Warpforge")`
        /// —— 落点 = **主菜单场景**（<see cref="MainMenuSceneName"/>），**不是**回原来那个模式窗；
        /// 原版打排位也是**回到主菜单之后**才演结果（`RankedMenuContainer__RefreshContentDisplay.c`）。</para>
        ///
        /// <para>🔴 **2026-10-17（B8）补上了「离开房间」那一跳**（原来记的是「我们没接」）——
        /// 原版这一步在**加载场景之前**，判据 = `BattleManager__LeaveBattle.c:38-47`；
        /// 逐跳落点与证据 → <see cref="LeaveNetRoom"/>。</para>
        ///
        /// <para>⚠️ **原版这里还有一件事我们没做**（如实记，⛔ 别当成「已经复刻完整了」）：
        /// 教程前两局（`PlayerDataManager.FirstTwoTutorialCompleted() == false`）**不加载主菜单**，
        /// 改走 `Everguild_MatchMakerManager.StartMatch(..., 4, ...)`（`LeaveBattle.c:49-63`）——
        /// 我们**没有那两局的教程链**，⇒ 恒走主菜单这一支。</para>
        ///
        /// <para>⚠️ **批处理下 `LoadScene` 那一句被跳过**（照本仓先例 `Shell/PracticeModePopup.cs` 的 `StartBotBattle` 里那句批处理闸
        /// 与 `Deck/DeckRuntime.cs` 的 `BackToMenu` 里那句批处理闸）—— 批处理里真换场景会把自检自己的场景掀掉。
        /// **这一档只记账 + 出声**；「场景真的换了」那一下归 **真 Play**（`资料/真Play待验清单.md`）。</para>
        /// </summary>
        public void LeaveBattle()
        {
            if (_leaving) return;              // 一局只走一次（原版走完那一句就 `return` 了）
            _leaving = true;
            _leaveCount++;
            LeaveNetRoom();                    // 原版：**先离开房间**再走（`BattleManager__LeaveBattle.c:38-47`）
            Debug.Log("[Battle] 结算后出口：闸门已开 ⇒ **离开战场**，加载主菜单场景 `" + MainMenuSceneName
                    + "`（原版 `BattleManager.LeaveBattle` 那一句是 "
                    + "`EverguildSceneManager.LoadScene(\"MainMenu Warpforge\")`，`BattleManager__LeaveBattle.c:58`）");
            if (Application.isBatchMode)
            {
                Debug.Log("[Battle] （批处理：不切场景，只记账 —— 真 Play 里这一句才是 "
                        + "`SceneManager.LoadScene(\"" + MainMenuSceneName + "\")`）");
                return;
            }
            UnityEngine.SceneManagement.SceneManager.LoadScene(MainMenuSceneName);
        }

        /// <summary>自检用：**「离开房间」那一跳走过几次**（累计，`Begin()` 里不清 —— 同 `_leaveCount`）。
        /// 只有真的走通了（发得出那句话、关得了台）才 +1。</summary>
        public int LeaveRoomCount { get { return _leaveRoomCount; } }
        int _leaveRoomCount;

        /// <summary>联机局：**离开房间**（= 原版 `BattleNetworkManager.LeaveBattleRoom`）。
        /// 逐跳判据（全量反编译，2026-10-17 B8 现读，`d:/2/tools/decomp_full/`）：
        /// <list type="number">
        /// <item>`BattleManager__LeaveBattle.c:38` `if (BattleNetworkManager.Instance != null)` —— 有联机管理器才走这一跳；</item>
        /// <item>`:39` `if (state != 100)` —— **已经离开过就不再来一遍**（`:15` 那句 `state = 100` 就在 `LeaveBattleRoom` 的头上）；</item>
        /// <item>`:40` `CustomDebug.LogWarning` —— **出声**，不是静默；</item>
        /// <item>`:46` `BattleNetworkManager.LeaveBattleRoom(force: 0)`。</item>
        /// <item>`BattleNetworkManager__LeaveBattleRoom.c:13-15` 先 `Log`、再 `state = 100`；
        ///   `:35-44` 判 `BattleManager.IsNetworkedGame()`（**不是联机局就直接 `return`**）
        ///   ⇒ `NetworkCustomManager.RemoveRoomAfterLeaving()`（只在 `force == 0` 时做）；
        ///   `:46-49` `NetworkCustomManager.LeaveRoom()`（`PhotonNetwork.LeaveRoom` = 真的退出那个房间）。</item>
        /// </list>
        /// <para>我们这一侧的等价落点：**`_net != null` 就是「联机局」这一个判据**（单机局它恒 null，
        /// 同一个判据见 `AiShouldDriveOpponent`），而 `NetSession.Close(say: true, …)` 正好就是
        /// 「**捎一句 `bye` 给对面** + 关台」这两下（`Net/NetSession` 里处理对面 `bye` 那一段，**不新增协议消息**）。</para>
        /// <para>⛔ **别改成「只发不关」**：`Close` 里那句 `SetState(NetState.Off, …)` 同时担着
        /// 「已离开」那道闸（= 原版那个 `100`）⇒ 去掉它，重入时就会再发一遍 `bye`。</para>
        /// <para>🔴 **2026-10-18 就地订正（铁律 5）**：本段原来写着「对面收到 `bye` 之后 `NetSession`
        /// 会 `SetState(Closed, why)` 再回调 `OnClosed` —— 而 **`OnClosed` 全仓零接线**（只有定义与
        /// 三处 `Invoke`，生产侧没人订阅）⇒ 对面那台**收得到、玩家看不到**；同一族的还有 `OnPeerLost`
        /// （掉线那条）也零接线」—— **这两句今天不成立**：那两条**早就接上了** ——
        /// `Net/NetBattle.WireSession()` 里那两行（ `_s.OnPeerLost += HandlePeerLost` /
        /// `_s.OnClosed += HandlePeerClosed`，整套「对面掉线 / 主动离开」的反应都在那一节），
        /// 大厅那一层另有 `Net/NetMatchmaking` 里挂 `OnPeerLost` / `OnClosed` 那两行。⇒ 对面那台**收得到、也看得到**
        /// （`Editor/NetBattleTest.cs` 的 M⑨ 那两条断言测的就是「弹了一条给人的提示」）。
        /// ⚠️ 顺带：原来那句「原版那半边**没查到**」**也已过期** —— 判据见 `Net/NetBattle.cs` 那一节头部
        /// （`BattleNetworkManager__SetOpponentDisconnected.c` → `…__ShowDisconnectionPopUp.c` →
        /// `WindowsManager.ShowPopUp`，文案键 `Battle/HUD/WaitOpponentConnectionMsg`；且原版**没有**
        /// 「对面主动离开」的专属窗，两条共用同一条链）。⛔ 但「弹出什么」那一层仍是**我们挑的**措辞。</para>
        /// </summary>
        void LeaveNetRoom()
        {
            if (_net == null) return;          // = 原版 `BattleNetworkManager.Instance != null` 的**另一支**（单机局）
            var s = _net.Session;
            if (s == null)
            {
                Debug.LogWarning("[Net] 这局挂着联机层但**没有会话**（`NetBattle.Session == null`）—— "
                               + "「离开房间」这一跳走不了，**对面收不到通知**。不静默，如实说。");
                return;
            }
            if (s.State == NetState.Off) return;   // = 原版 `if (state != 100)`：已经离开过就不再走一遍
            // 🔴 **2026-10-19（P6d · A1038）**：传的是**词条键**（`NetSession.Close` 的 `reason` 已改成
            //    「词条键」语义）—— 线上只发键、收侧 `NetWireText.Unpack` 按**它自己**的语言取词；
            //    本机状态字（`SetState(Off, …)`）也走同一个键 ⇒ ⛔ 两半都不留裸串。
            //    ⚠️ 键名 `Wire/PeerLeftMatch` 是**本族第 8 条**（原计划 7 条不含这一处）——
            //      ZH 列 = 这一行的原话逐字 ⇒ **可见文案零变化**（⛔ 没并进 `Wire/PeerDone`：
            //      那一条是「对面结束了这一局」，并过去等于**悄悄改掉这一跳的措辞**）。
            s.Close(true, "Settings/Online/Wire/PeerLeftMatch");
            _leaveRoomCount++;
            Debug.Log("[Net] 离开房间：已给对面捎一句 `bye` 并关台"
                    + $"（本局累计 {_leaveRoomCount} 次；落点 = `BattleManager__LeaveBattle.c:46` → "
                    + "`BattleNetworkManager__LeaveBattleRoom.c:46-49` 的 `NetworkCustomManager.LeaveRoom`）");
        }

        /// <summary>
        /// 重开一局（`Restart`）。
        /// 🔴 **2026-10-16（W22）更正**：这个方法原来那行文档写着「结算面板上那句『按 R 再来一局』就是它」——
        /// **那句话已经删了**（原版结算屏零文字，而且原版**根本没有「再来一局」**，判据见
        /// `资料/普查产出_1016/判据_结算后出口.md` §2.3 / §2.6）。`R` 现在是**我们自己的调试键**
        /// （`真Play待验清单.md` D13 早就标着「我们自己的」；反编译里唯一的重开键在**回放模式**：
        /// `BattleManager__Update.c:1336-1371` 的 `'A'` / 左方向键）。⛔ 别拿它当原版行为。
        /// 阵营不变，**种子 +1** —— 同一副牌、不同的抽牌顺序，不然每次重开都一模一样。
        /// ⚠️ **卡组也要原样带过去**（`_myDeckSrc`）：不带的话重开一局就变成自动凑的牌了，
        ///    玩家会以为自己在打自己编的那副（2026-09-12 接卡组库时撞到的）。
        /// </summary>
        public void Restart()
        {
            // 🆕 2026-09-26（N4）：**联机局不能单方面重开** —— 对面还在这一局里。
            //    如实说，不静默（红线），也不装作重开了。
            if (_net != null)
            {
                SetHint("联机局不能自己重开 —— 对面还在这一局里");
                Debug.LogWarning("[Net] 联机局收到「按 R 重开」—— **拒绝**（两端会打岔）；"
                               + "要重开得两边都退回菜单再连一次");
                return;
            }
            if (_endPanel != null) _endPanel.Hide();     // 上一局的结算面板先收掉（HUD 复用，不清会叠着）
            // 🔴 **2026-10-12（A387 的同一族）**：`deckNote` 也要**照原样带过去** ——
            //    上面那句把「用的是哪副牌」带过去了（`_myDeckSrc`），它却原来没带 ⇒ 存档读不出来那一局，
            //    按 R 重开之后提示行就**不再说**为什么是自动凑的了（账还在、话没了 = 静默）。
            //    ⚠️ 它只在「卡组读不出来」那一支被用上（牌正常时 `ResolveDeck` 自己那句盖过它）。
            // 🆕 2026-10-17（B29）：**教程局要重开也一样** —— 关卡数据照旧带过去，但
            //   **执行器必须新建一个**（`TutorialScript` 装着「第几回合第几条」那两条指针；
            //    复用同一个实例的话，重开一局会从上一局停下的地方接着跑 —— **静默且必错**）。
            var tutStage = _tutorialStage;
            Begin(_myFaction, _foeFaction, _seed + 1, _myDeckSrc, _foeDeckSrc, deckNote: _deckNote, vars: _vars,
                  // 🆕 2026-10-15（A383）：**模式号也要照原样带过去** —— 与 `_seed` / `_vars`
                  //   同一条纪律：不带的话「按 R 重开」会**悄悄退回经典那一档**
                  //   （结算的骷髅账 + 对局记录 + 录像头三处跟着变，静默）。
                  playMode: _playMode,
                  tutorial: tutStage == null ? null : new TutorialScript(tutStage),
                  exactMine: tutStage == null ? null : _exactMyCards,
                  exactFoe: tutStage == null ? null : _exactFoeCards);
        }

        /// <summary>🆕 2026-10-17（B29）：**本局的教程关卡**（`null` = 不是教程局）。
        /// 由 `Begin` 写入、`Restart` 读它去**新建**一个执行器（⛔ 不是复用 —— 见 `Restart` 里那段注）。</summary>
        TutorialStageData _tutorialStage;
        /// <summary>教程局那两副**按关卡原样**上场的牌（`Begin` 存着给 `Restart` 用）。</summary>
        List<CardDef> _exactMyCards, _exactFoeCards;

        /// <summary>🆕 2026-09-26：**本局参数**（经典 / 遭遇…）。逐字段见 <see cref="GameplayVariables"/>。
        /// 由 `Begin` 写入、`Restart` 原样带过去；`Ctx.Vars` 就是它。</summary>
        GameplayVariables _vars = GameplayVariables.Classic;
        /// <summary>本局参数（自检/HUD 读用）。</summary>
        public GameplayVariables Vars { get { return _vars; } }

        // ==================================================================
        //  🆕 2026-10-15（A383）：**本局真正的模式号**（原版 `MatchData.playMode`）
        // ==================================================================
        //  在此之前，「本局模式」在本类里**只有二值**（`_vars.IsSkirmish ? Skirmish : Classic`）——
        //  那是「用经典参数还是遭遇参数」，**不是「这一局是哪个模式」**。
        //  原版知道模式的是**入口窗**：`MatchMakerManager.FindMatch` 取
        //  `IPlayEvent` slot 0 = `EventPlayMode`，写进 `MatchData.playMode`（判据全文 →
        //  `资料/普查产出_1014/RO_战场与窗口判据三件.md` §二）。
        //  我们这条开战链**跨场景**（壳 `LoadScene` → 战场 `Start` → `BeginFromDeckLibrary`），
        //  两段之间只有静态字段过得去 ⇒ 照 `PrebuiltDecks._pending` / `PracticeModePopup._pendingOpponent`
        //  的先例，加一条**读一次就清**的静态通道。

        /// <summary>入口窗在**开战那一刻**放进来的模式号（`null` = 这一路没声明）。
        /// 🔴 放进来的时机 = **开战前最后一步**（练习窗是 `StartBotBattle`；遭遇/排位两扇是
        /// `OnSearchFinished`，基类紧接着就调 `StartBotBattle`）—— ⛔ **别提前到「点 Battle!」**：
        /// 那之后还有 12 秒搜索，玩家取消再换一扇窗就串了。</summary>
        static GameMode? _pendingPlayMode;

        /// <summary>自检用：通道里现在是什么（`null` = 空）。</summary>
        public static GameMode? PendingPlayMode { get { return _pendingPlayMode; } }

        /// <summary>入口窗放「本局是哪个模式」。</summary>
        public static void SetPendingPlayMode(GameMode mode) { _pendingPlayMode = mode; }

        /// <summary>开局读一次（**读完就清** —— 下一局不该还带着它）。没有给 `null`。</summary>
        public static GameMode? TakePendingPlayMode()
        {
            var m = _pendingPlayMode;
            _pendingPlayMode = null;
            return m;
        }

        /// <summary>本局模式（`Begin` 写入、`Restart` 原样带过去）。默认 = 经典。
        /// ⛔ **别拿 `Vars.IsSkirmish` 当它**（那是规则参数，不是模式号 —— 见上面那一段）。</summary>
        GameMode _playMode = GameMode.Classic;
        /// <summary>本局模式号（自检/结算/HUD 读用）。</summary>
        public GameMode PlayMode { get { return _playMode; } }

        /// <summary>🆕 2026-09-26：**「本局谁先手」的唯一算法** —— 由种子决定（`0` = 我方先手）。
        ///
        /// 🔴 **只此一处**：`Begin` 用它；**自检找种子时也用它**（别在测试里把公式再抄一遍 ——
        /// 两处写同一条规则迟早不一致，这是这工程的旧账）。
        /// 用户 2026-09-26 拍板「一律投硬币」（判据 → `资料/加时与冲突模式_原版规格.md` §2.8）。
        /// </summary>
        public static int FirstSeatForSeed(int seed)
        {
            return new System.Random(seed ^ 0x5F3759DF).Next(2);
        }

        /// <summary>自检用：**钉住本局种子**（`null` = 真 Play 那条路 —— 按时间派生、每局不同）。
        /// 为什么要有它：`BeginFromDeckLibrary` 现在**每局换种子**（不换的话「投硬币」永远同一面 = 假的），
        /// 而自检要的是**可复现** ⇒ 钉住种子让它每次落在同一面；**9b / 9c 各钉一个**，
        /// 于是「我方先手」「我方后手」两条路**都被确定性地覆盖**（比随机落在哪一面强）。</summary>
        public int? ForceSeed;

        /// <summary>自检用：**钉住本局谁先手**（`null` = 照常按种子掷硬币）。
        ///
        /// 为什么要有它：换先手会连带改**起始能量 / 防御卡给谁 / 加时判哪一边 / 谁先出牌**四件事，
        /// 而有一批「回合流程」的自检**写死了「我方在第 1 回合行动」**（那是先手才成立的账）⇒
        /// 掷了硬币之后它们会**随机红**。给它们一个**显式钉住**的入口，比把十几条断言都改成按角色分支稳。
        /// 🔴 **钉住的是自检，不是产品**：真 Play（`ForceFirstSeat == null`）照旧掷硬币；
        ///    而**掷硬币那半边另有断言覆盖**（`BattleScene` 9b/9c 把它清成 `null` 之后按角色断）。
        /// ⚠️ 语义：`ForceFirstSeat = 0` ⇒ **我方（座位 0）先手**。
        ///
        /// <para>🔴 **2026-10-19（A1100）—— 这个字段只许自检写**（它被产品写过一次，是本工程踩过的坑）：
        ///   `BeginFromPendingCore` 原来在这里写 `= pb.FirstSeat`，而它**没有清除口** ⇒
        ///   打完一局**联机** / 放完一局**录像**之后，**下一局单机**（`Restart()` 或任何 `Begin`）
        ///   会**沿用上一局的绝对座位** —— 先手**不掷硬币**（对局可复现受损）。
        ///   ⇒ 产品现在走 `Begin(firstSeatOverride:)` 那条**显式形参**（联机开局 / 重连重建 / 放录像
        ///   三档都走它，调用点只此一处）⇒ **这个字段一条产品写入都不许再有**
        ///   （判据：全仓 `grep ForceFirstSeat` —— 写点只该剩 `BattleScene.cs` 那几处自检）。</para>
        /// <para>⚠️ 所以它**不属于**「每局开头清一次」那一族（`Begin` 里 `_settled` / `_vars` / `_leaving`
        ///   那一串）：自检把它当**钉子的载体** —— `BattleScene.BuildScene` 建 driver 时钉一次（`= 0`），
        ///   之后那一整轮里几十次 `Begin` 都靠它。**每局清一次 = 把那几十条「回合流程」断言变成随硬币红绿。**
        ///   要清就清**调用方**那一侧（9b / 9c 就是这么拔钉子的）。</para></summary>
        public int? ForceFirstSeat;

        /// <summary>
        /// 这个阵营的卡池在哪。
        /// 我们自己设计的两套（`Ember` / `Tide`）**不在原版卡池里** —— 它们由 `StarterCards` 造，
        /// 是专门为覆盖关键词/触发而设计的（`RuleEngineTest` 里那批用例靠它们）。
        /// 其余阵营一律走原版卡池。
        /// </summary>
        static List<CardDef> PoolFor(List<CardDef> pool, string faction)
        {
            if (faction == StarterCards.EmberFaction || faction == StarterCards.TideFaction)
                return StarterCards.Of(faction);
            return pool;
        }

        /// <summary>
        /// 一方用哪副牌：**给了合法卡组就用它，否则按卡池自动凑一副**（并**说清楚为什么**）。
        /// 不合法/凑不出来都退回自动凑 —— 宁可打一局「不是你要的那副」，也不能开不了局。
        /// </summary>
        /// <param name="notice">给**画面提示行**的一句人话：用的是哪副牌 / 为什么退了。
        /// 走自动凑时是空串（那是默认行为，不用交代）。调用方只显示己方那句。</param>
        static List<CardDef> ResolveDeck(List<CardDef> pool, string faction, PlayerDeck saved,
                                         int seed, string who, out string notice)
        {
            notice = "";
            if (saved != null)
            {
                // ⚠️ **解析卡组引用只有一处**（`CardDatabase.DeckLookup`，2026-09-13 第三十三轮）：
                //    先按**稳定 id**（新存档写的就是 id），再退回**卡名 + 阵营**（旧存档）。
                //    2026-09-13 那次的坑：卡组里存的是卡名，而**原版有跨阵营同名卡**
                //（`Terminator` / `Terminator Champion` / `Maulerfiend` 这 3 组），只按名字查会撞上
                //    **另一个阵营**那张，于是 `Validate` 判 `WrongFaction`、**一副合法卡组被打回自动凑**（静默降级）。
                //    ⚠️ 2026-10-17 订正：这句的例子原来还带一个 `Bladeguard Veteran` —— 那组的「同名」
                //    是 09-13 一次错改名的产物（`UM34` 被照 PnP 卡图文件名改名），10-17 已撤回，现在它叫
                //    `Bladeguard Lieutenant` ⇒ 同名组 4 → 3（判据 `资料/普查产出_1017/W_B16_教程数据缺口.md` §①）。
                // 🔴 **2026-09-26 修**：这里原来**没传模式**（第三参默认 `false` = 经典）⇒
                //    **12 张的遭遇牌一律被按经典的 30 张判 ⇒ `TooFewCards` ⇒ 静默退回自动凑的 30 张**。
                //    实据（`BattleScene` 自检日志）：`[Battle] 我的卡组「自检·自建遭遇牌」不合法（卡组张数不够）`
                //    —— 而那就是一副 12 张的遭遇牌。**模式从这副牌自己来**（`PlayerDeck.IsSkirmish`，
                //    判据 → `资料/加时与冲突模式_原版规格.md` §2.7）。
                //    ⚠️ 这一条**不修的话**：玩家自建的遭遇卡组永远进不了对局（打的是自动凑的 30 张），
                //      「能开一局」那几条断言照样全绿 —— 因为它们只量了**模式**、没量**用的是哪副牌**。
                var err = DeckRules.Validate(saved, CardDatabase.DeckLookup(pool, faction), saved.IsSkirmish);
                if (err == DeckError.None)
                {
                    var skipped = new List<string>();
                    // `rng` 只喂「卡组没带防御卡时随机补一张」那一处（原版 `AddGoesSecondCardToDeck` 的兜底）；
                    // 用**这一方的种子**（调用方传的 `seed+1`/`seed+2`）⇒ 两边不会补到同一张，且对局可复现。
                    var list = DeckBuilder.FromDeck(pool, saved, skipped, faction, new System.Random(seed));
                    // 🆕 2026-09-26：卡组**没带**防御卡时 `FromDeck` 会补一张（照原版兜底）——
                    //   在这里把它捞出来，用于**对玩家说清楚**（手里多的那张哪来的）。
                    //   卡组带了就保持 null，提示行不加那句。
                    CardDef autoDefence = null;
                    if (string.IsNullOrEmpty(saved.DefensiveId))
                        foreach (var c in list)
                            if (c != null && c.Type == "defence") { autoDefence = c; break; }
                    if (skipped.Count > 0)
                        Debug.LogWarning($"[Battle] {who}的卡组「{saved.Name}」里有 {skipped.Count} 张"
                                       + "**引擎还不能结算、上不了场**的卡，已丢掉："
                                       + string.Join("、", Limit(skipped, 6).ToArray())
                                       + " —— 防御卡与「效果解析不了」的战术卡，这是**已知**的，不是 bug");
                    if (list.Count >= 2)
                    {
                        // 这句是要**玩家**看到的：这副牌没有全上场，别以为打的是自己编的那 30 张。
                        // 措辞跟着「实际会丢什么」改过两次，**每次改都得回到这里**：
                        //   · 2026-09-12：`FromDeck` 开始收战术卡了 ⇒ 从「所有战术卡」改成「防御卡 + 解析不了的战术卡」
                        //   · 2026-09-13（第三十三轮）：**防御卡也进对局了** ⇒ 只剩「解析不了的战术卡」。
                        // ⚠️ 上一次没跟上：截图里防御卡明明在手上，提示行还在说「防御卡…没上场」——
                        //    对玩家说错话比不说更糟。**这段文字与 `FromDeck` 的取舍是一对，改一处要改两处。**
                        notice = $"本局用你编的「{Short(saved.Name, 14)}」"
                               + (skipped.Count > 0
                                  ? $"·{skipped.Count} 张（效果本版解析不了的战术卡）没上场" : "")
                               // 🆕 2026-09-26：**卡组没带防御卡时，本局会替你补一张**（照原版
                               // `AddGoesSecondCardToDeck` 的兜底）—— 这句是**给玩家看的**：
                               // 手里多出一张他没编过的牌，不说清楚他会以为是 bug。
                               // 判据 → `资料/加时与冲突模式_原版规格.md` §2.7c。
                               + (autoDefence != null
                                  ? $"·**没带防御卡 ⇒ 本局补了一张「{autoDefence.Name}」**（原版就是这么兜底的）" : "");
                        return list;
                    }
                    Debug.LogError($"[Battle] {who}的卡组「{saved.Name}」展开之后只剩 {list.Count} 张，打不了");
                    notice = $"你的卡组「{Short(saved.Name, 14)}」展开后只剩 {list.Count} 张能上场"
                           + " —— 本局退回自动凑的一副";
                }
                else
                {
                    // 🔴 **2026-10-18（`G9`）**：`DeckRules.Describe` 从这一天起只出**词条键**
                    //   （引擎层不再产人话）⇒ 这一行**必须**过 `DeckRuntime.DeckErrorText` 取词条，
                    //   ⛔ 否则玩家看到的是 `MenuDeck/Error/TooFewCards` 这种键名。
                    Debug.LogError($"[Battle] {who}的卡组「{saved.Name}」不合法（{DeckRules.Describe(err)}）");
                    notice = $"你的卡组「{Short(saved.Name, 14)}」不合法（{DeckRuntime.DeckErrorText(err)}）"
                           + " —— 本局退回自动凑的一副";
                }
                Debug.LogWarning($"[Battle] {who}退回**按卡池自动凑**的一副（阵营 {faction}）");
            }

            // `unitsOnly: false` —— 自动凑的牌组也带**能打的战术卡**（约占 1/3）。
            // ⚠️ 2026-09-12 之前那个开关是**没实现的**，所以自动凑的牌一张战术都没有，
            //    实战里永远看不到战术卡（引擎自检全绿 ≠ 打起来会用到）。
            return DeckBuilder.StarterDeck(pool, faction, DeckBuilder.ClassicDeckSize,
                                          new System.Random(seed), unitsOnly: false);
        }

        /// <summary>
        /// 这副牌的**阵营 = 它督军的阵营**。查不到（没督军 / 督军不在池子里）返回 null，调用方别动阵营。
        /// 判据和 `DeckRules.Validate` 里那条 `SameFaction(卡.阵营, 督军.阵营)` 是同一条 ——
        /// 只是这里把它用在「开局用哪个阵营」上。
        /// </summary>
        static string WarlordFaction(PlayerDeck d, List<CardDef> pool)
        {
            if (d == null || string.IsNullOrEmpty(d.WarlordId)) return null;
            var w = CardDatabase.DeckLookup(pool)(d.WarlordId);
            return (w != null && !string.IsNullOrEmpty(w.Faction)) ? w.Faction : null;
        }

        /// <summary>截断到 <paramref name="n"/> 个字（提示行**不换行**，太长会横着铺出屏幕）。
        /// 卡组名是玩家自己起的，长度不可控。</summary>
        static string Short(string s, int n)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= n ? s : s.Substring(0, n) + "…";
        }

        static List<string> Limit(List<string> src, int n)
        {
            var outList = new List<string>();
            for (int i = 0; i < src.Count && i < n; i++) outList.Add(src[i]);
            if (src.Count > n) outList.Add("…");
            return outList;
        }

        /// <summary>**在棋盘上轻点一个单位** ⇒ 开/关大卡展示窗（原版 `CardScript.OnTouchUpAsButton` 的 C 段）。
        /// 我方**和对手**的单位都给开 —— 原版棋盘段没有敌我判断（那条 `isPlayer==false ⇒ 只有 spellType==0xE6`
        /// 的守卫在**手牌段 A**）。窗里会自动带上「谁给我加的 buff」那块 `EffectList`（有 buff 才出）。
        /// 判据与出处 → `资料/待办判据_战场与战斗视图.md` §8b。</summary>
        void ToggleUnitCard(int side, int slot)
        {
            if (_cardDisplay == null || Ctx == null) return;
            var u = Ctx.Players[side].Board[slot];
            if (u == null) return;
            // 顺带把「谁给我加的 buff」那几行算出来（原版同一条链：`DisplayCard` → `ShowBattleCard` → `DisplayCardEffects`）。
            // ✅ **2026-09-29 查实：手牌那条路【原版也不显示】**（原来这里写「没查」）——
            //    ① `ShowBattleCard` / `DisplayCardEffects` 里**没有任何手牌/棋盘分支**，读的是**被展示那张卡
            //       自己**的 `EntityScript.activeEffects`（`DisplayCardEffects.c:34` → `GetCardEffectsToShow`）；
            //    ② 手牌的正规入口（`BasicCardUI.CardClicked` → `CardDisplayWindow.ShowCard`）吃的是
            //       `RawCardScript`，**结构上就没有单卡实例效果表**，而且那条路**从不碰效果树**；
            //    ③ 即便手牌卡走了 `ShowBattleCard`，它的 `activeEffects` 也在**回手那一刻被清空**
            //       （`CardScript.ResetToValuesInHand` 整表 Clear）⇒ 空 ⇒ 整组 `SetActive(false)`。
            //    ⚠️ **仍然开着的一条边界**（铁律 11，别当没有）：`CardScript.AddEffect` 只排除 cardState 5/6/11
            //       ⇒ **在手(1)也能给卡加 effect**，那种卡走双击那条链在原版**是会显示的**；
            //       我们的 `TempBuffs` 挂在 `UnitState`（场上单位）上、手牌没有等价物 ⇒ 这一支**没有等价物**，
            //       已记成待办（判据 → `资料/待办判据_战场与战斗视图.md` §8b）。
            // 🆕 2026-10-18（A940）：`hideLargeCardDisplay`（原版 `BattleManager__DisplayCard.c:44-50`：
            //   `!='\0' ⇒ return`，跳过 `CardDisplayWindow__ShowBattleCard`）—— 教程局里这一窗**不开**。
            //   ⚠️ 只挡这两条「显示某张卡」的路（棋盘轻点 / 手牌轻点）；进攻卡那颗钮走的是
            //     原版另一个方法（`DisplayOffensiveCard`），不在那个读数点的管辖里 ⇒ 不动它。
            if (!TutorialAllowsCardDisplay) return;
            _cardDisplay.Toggle(ToCardData(u, side == _me ? _myFaction : _foeFaction), u.Card,
                                CardDisplayWindow.RowsOf(u.TempBuffs));
            AfterCardWinToggle(BoardViewAt(slot, side == _me));
        }

        /// <summary>「同一帧」判据 —— ⚠️ **批处理里恒为 false**：`Time.frameCount` 在自检里**不推进**
        /// （整段自检都在同一帧）⇒ 不排除的话那两条守卫会**永久生效**，那几条路在自检里永远走不到
        /// （2026-09-29 实测：`BattleScene` 因此红了 5 条 + 1 处空引用中断）。
        /// 它们本来就是**真运行时的同帧竞态**守卫（手牌轻点与这里的点击路由是**两个来源**、读同一次鼠标）。</summary>
        static bool SameFrame(int f)
        {
            return !Application.isBatchMode && f == Time.frameCount;
        }

        /// <summary>🆕 2026-09-29：开/关之后登记「这一下是谁开的」与「哪一帧」——
        /// 「指针移开就关」要盯**那张卡**，同帧防打架要那两个帧戳。只有一条判据，两个入口共用。</summary>
        void AfterCardWinToggle(CardView source)
        {
            if (_cardDisplay.Visible) { _cardWinSource = source; _cardWinOpenedFrame = Time.frameCount; }
            else { _cardWinSource = null; _cardWinClosedFrame = Time.frameCount; }
        }

        /// <summary>手牌**拖出去、松手时没落成**（卡回弹到手牌）⇒ 提示行写一句
        /// `Battle/Tips/DragToTarget`（原版在同一时刻出这句，判据 → <see cref="DragToTargetTerm"/>）。
        ///
        /// 🔴 **2026-10-18（第十五轮 · `G5`）为什么是这个回调**：`CardInteraction.Release` 的
        /// `else` 支（`Hand/CardInteraction.Release`）**两种落空**都记在一处 ——
        /// `MissedSlot`（松手时没命中任何格位）与 `EngineRefused`（落在格位上、引擎说打不了）；
        /// 而原版那两处 `DragToTarget` 的判据正是「**指针下没有合法目标**」（见 `Loc.cs` 那条键的注释）
        /// ⇒ **两种落空都算**，`OnReturned` 恰好就是它们。
        ///
        /// ⚠️ **轻点（按下→松开几乎没动）也算一次「没落成」** —— 那一刻 `Release` 先发 `OnReturned`、
        ///    紧跟着发 `OnTapped`（`:540` 然后 `:549`，**同一次调用、先后的两行**）⇒
        ///    `OnCardTapped` 里把这一行**收回**（轻点是「开卡面展示窗」，不是一次失败的拖拽）。
        ///    ⛔ **别改成按帧判**：两个回调是同一次 `Release` 里同步发出来的，这里没有帧序问题。
        ///    🧨 **改坏法**：把 `OnCardTapped` 里那句 `SetHint("")` 删掉 ⇒ 轻点一张手牌也会写
        ///    「拖到目标上再松手」，而画面上根本没拖过。</summary>
        void OnCardReturned(CardView card)
        {
            SetHint(Loc.T(DragToTargetTerm));
        }

        /// <summary>手牌被**轻点**了（按下→松开几乎没动）。原版这个动作就是开关卡牌展示窗。</summary>
        void OnCardTapped(CardView card)
        {
            // 🔴🆕 **2026-10-18（第十五轮 · `G5`）**：先把上一行（`OnCardReturned` 写的
            //   「拖到目标上再松手」）**收回** —— 轻点不是一次失败的拖拽，它开的是卡面展示窗。
            //   ⚠️ 放在**所有早退之前**：`SameFrame` / `TutorialAllowsCardDisplay` 那两道闸
            //   只决定「开不开窗」，不改变「这一下是轻点」这个事实。
            SetHint("");
            if (_cardDisplay == null || card == null) return;
            // 同一帧里刚被「点遮罩空白」关掉 ⇒ 这一下**不再开**：两个来源读的是同一次鼠标
            //（`CardInteraction` 自己读、不经过这里），不掐的话表现就是「点了没反应 / 一闪」。
            if (SameFrame(_cardWinClosedFrame)) return;
            // 🆕 2026-10-18（A940）：`hideLargeCardDisplay`（同 `ToggleUnitCard` 那一处，判据只此一处）
            if (!TutorialAllowsCardDisplay) return;
            _cardDisplay.Toggle(card.Data, DefOf(card));
            AfterCardWinToggle(card);
        }

        /// <summary>视图 → **引擎卡表项**（展示窗算「相关卡」要用它 —— 判据 → `RelatedCards`）。
        /// 手牌按实例查；查不到给 null（窗里就只显示主卡、并**出声** —— 不许静默）。</summary>
        CardDef DefOf(CardView v)
        {
            if (v == null || Ctx == null) return null;
            int i = HandIndex(v);
            var hand = Ctx.Players[_me].Hand;
            if (i >= 0 && hand != null && i < hand.Count && hand[i] != null) return hand[i].Card;
            Debug.Log("[Battle] 点了「" + (v.Data.title ?? "?") + "」但它不在手牌里（`HandIndex` = " + i
                    + "）⇒ 相关卡算不出来，展示窗只显示主卡");
            return null;
        }

        /// <summary>🆕 2026-09-29：**指针移开就关**那一条（原版 `CardCollider.OnPointerExit` →
        /// `CardScript.OnTouchExit`，唯一守卫是 `displayingCardFlag`）。每帧判；自检直接调它
        /// （批处理里没有 `Update`）。返回 true = 这一帧刚关掉。
        ///
        /// 🔴 **我们按实情收了一处**：守卫 = 「指针既不在**点开它的那张牌**上、也不在**窗的地界**里」——
        ///   原版只守前者，照字面做的话窗里的语音/眼睛钮**永远点不到**（它们在卡外的下缘那条上）。
        ///   详情与出处 → `CardDisplayWindow.ContainsPointer` 的注释。
        /// ⚠️ 刚开窗那一帧不判（指针可能还没落到卡上）—— 同帧判据见 `SameFrame`（**批处理里恒 false**）。</summary>
        public bool TickCardWinPointerExit(Vector3 wp)
        {
            if (_cardDisplay == null || !_cardDisplay.Visible) return false;
            if (SameFrame(_cardWinOpenedFrame)) return false;
            bool onSource = _cardWinSource != null && _cardWinSource.Contains(wp);
            if (onSource || _cardDisplay.ContainsPointer(wp)) return false;
            _cardDisplay.Hide();
            _cardWinSource = null;
            _cardWinClosedFrame = Time.frameCount;
            UpdateHud();
            return true;
        }

        /// <summary>放大窗开着时，**这一下点击归谁**（判据只此一处 —— `Update` 与自检都问它）。
        /// 返回 true = 这一下被窗吃掉了（别往下走）。三处落点照原版 `CardDisplayWindow`：
        /// ① **语音钮**（`voiceOverButton`）② **卡格** ⇒ 换位
        /// （点前台那张 = 原版闸② 「什么都不做」，但**这一下也要吃掉** —— 原版那张卡自己的
        /// `UI Collider` 会把点击挡住）③ **遮罩空白 ⇒ 关窗**（原版 `BackgroundCloseButton` /
        /// `OnBackgroundClick`；🆕 **2026-09-29 接上**，原来记的是「我们没接」）。
        /// <para>🔴 **2026-10-17（B8）删掉了原来的「② 眼睛钮」那一支** —— 战斗版原版**没有**
        /// `showCardTextButton`（`{m_PathID:0}`，13/13 竞技场逐份实读；`ToggleCardState` 在全量反编译里
        /// **只有定义、零调用点**）⇒ 那支连同 `CardDisplayWindow.HitEye/ToggleLore/LoreVisible` 三个桩
        /// 一并删干净。判据全文 → `CardDisplayWindow.cs` 的文件头 + `Editor/BattleScene.cs` 的 A860 那一节。</para></summary>
        public bool HandleDisplayWindowClick(Vector3 wp)
        {
            if (_cardDisplay == null || !_cardDisplay.Visible) return false;
            if (_cardDisplay.HitVoice(wp))
            {
                if (!_cardDisplay.PlayVoice())
                    Debug.Log("[Battle] 「放大窗·语音」这张卡没有单位语音 —— **没播**（不静默失败）");
                return true;
            }
            int slot = _cardDisplay.HitSlot(wp);
            if (slot >= 0) { _cardDisplay.SwapToFront(slot); return true; }
            // 🆕 2026-09-29：**点遮罩空白 = 关窗**（原来这里是 `return false`「不拦截」）——
            //   原版 `BackgroundCloseButton.OnPointerClick` → `CardDisplayWindow.OnBackgroundClick` → `Close`，
            //   链有直证（判据 → `资料/待办判据_战场与战斗视图.md` §8b）。
            //   ⚠️ 同帧防打架：这一下如果**就是刚开窗那一下**（手牌轻点与这里的点击路由不是一个来源），
            //   照字面关会把刚开的窗立刻关掉 ⇒ 见 `_cardWinOpenedFrame`。
            if (SameFrame(_cardWinOpenedFrame)) return false;
            _cardDisplay.Hide();
            _cardWinSource = null;
            _cardWinClosedFrame = Time.frameCount;
            return true;
        }

        /// <summary>
        /// **点在我方牌堆那一块里没有**（px @1920×1080）。牌堆中心 = `MyDeckX01/MyDeckY01` 那对归一化锚点，
        /// 边长 = `DeckPlatePx`（230）。判据只此一处 —— 窗口与 `Update` 都问它。
        /// </summary>
        public static bool HitMyDeckPile(Vector3 world)
        {
            float px = world.x * EndPanel.PxPerUnit + 960f;
            float py = 540f - world.y * EndPanel.PxPerUnit;
            float cx = MyDeckX01 * 1920f, cy = (1f - MyDeckY01) * 1080f, h = DeckPlatePx * 0.5f;
            return Mathf.Abs(px - cx) <= h && Mathf.Abs(py - cy) <= h;
        }

        /// <summary>
        /// **「结束回合」那一块被点到了没有**（原版 `Clock/TurnBtn`）。
        /// 🔴 **判据只此一处** —— `Update` 松手那一路与自检**都问它**（⛔ 别在各调用点再拼一遍）。
        /// 有原版按钮底图就按它判，没有（美术目录被删）就退回按文字判 ——
        /// ⚠️ **只有前一档有原版判据**：原版那颗 `Clock/TurnBtn/TurnText` 是 **`m_RaycastTarget = 0`**
        /// （`MonoBehaviour_3971.json`）⇒ 它**收不到射线**，命中区**只**由底图那颗 `Image` 定。
        /// 🆕 **2026-10-19（A964②）**：底图那一档的命中区改成**原版那个** = `rect` 130.702×80.432 按
        /// `m_RaycastPadding` (−14.19,−30.3,−26.19,−18.4) 收/放 ⇒ **171.082×129.132 px**
        /// （算式只此一份 → `HitPaddedRect`）；⛔ 原来 `Contains` 量的是画出来的 130.7×80.4、
        /// 原版真正可点的那四边全成了死区。文字那一档**故意保持原样**（原版没有这条路，见上面那条）。
        /// </summary>
        public bool HitEndTurn(Vector3 world)
        {
            if (_endTurnBg != null)
                return HitPaddedRect(_endTurnBg, world, EndTurnBtnRectW, EndTurnBtnRectH,
                                     EndTurnBtnPadL, EndTurnBtnPadB, EndTurnBtnPadR, EndTurnBtnPadT);
            return _endTurnLabel != null && _endTurnLabel.Contains(world);
        }

        // ---- 🆕 2026-10-18（A964① ⑤ · ⑦）：牌堆 / 结束回合钮的只读口（⛔ 只给自检，不给生产用）----
        //  形状照 `CameraResetButtonDrawnPx`（中心 + 实绘尺寸一对）。
        //  ⛔ `HitMyDeckPile` 的口径**没改**（它仍是**静态**判据、按 `DeckPlatePx` 那个正方框判）
        //     —— 改命中＝改行为，要另开一件、另配断言。

        /// <summary>我方牌堆**底板**（`MyDeckPlate`）的世界中心 —— 与 `HitMyDeckPile` 硬写的
        /// `MyDeckX01/MyDeckY01` 是同一个点（探针拿它量「命中区 ⊇ 实绘」）。</summary>
        public Vector3 MyDeckPlateWorldPos
        {
            get { return _myDeckPlate != null ? _myDeckPlate.transform.position : Vector3.zero; }
        }

        /// <summary>我方牌堆底板**实绘**的世界宽 × 高（换成 px 再 × `EndPanel.PxPerUnit`）。
        /// ⚠️ 同义的 `DeckPlateWorldW/H` 早就有（A20 收口件），这里只补一对 `Vector2` 版，
        /// 让探针与 `CameraResetButtonDrawnPx` **一个形状**。</summary>
        public Vector2 MyDeckPlateDrawnSize
        {
            get { return _myDeckPlate != null
                       ? new Vector2(_myDeckPlate.WorldW, _myDeckPlate.WorldH)
                       : Vector2.zero; }
        }

        /// <summary>结束回合钮的世界坐标（自检照着它点 —— 走的是和真实点击同一条 <see cref="HitEndTurn"/>）。
        /// 取**底图那颗** `_endTurnBg`（字 `_endTurnLabel` 压在它正中，中心同一个）。</summary>
        public Vector3 EndTurnWorldPos
        {
            get { return _endTurnBg != null ? _endTurnBg.transform.position : Vector3.zero; }
        }

        /// <summary>结束回合钮**实绘**的 px 尺寸（判据 = `_endTurnBg` 那颗 quad 的
        /// `WorldW/WorldH × 108`）。原版那个 rect 是 130.7×80.4（贴图 182×112 的 1.625 比例 × 高 80.4）
        /// —— 对不上就是 `E2` 要报的东西。</summary>
        public Vector2 EndTurnDrawnPx
        {
            get { return _endTurnBg != null
                       ? new Vector2(_endTurnBg.WorldW, _endTurnBg.WorldH) * EndPanel.PxPerUnit
                       : Vector2.zero; }
        }

        /// <summary>自检用：这一下点到的**棋盘单位**在第几号槽（`side` = `_me` 我方 / `1 - _me` 对手）。
        /// 走的是真实输入同一条私有 `HitSlot`（⛔ 不另写一份判定）。</summary>
        public int HitUnitSlotForTest(int side, Vector3 world)
        {
            return HitSlot(side == _me ? _myUnits : _foeUnits, world);
        }

        /// <summary>
        /// 摊开**我方牌库**（第 13 行那个「多张一起看」的窗口）。
        /// ⚠️ **入口是我们挑的**（原版从 `BattleManager.ResolveAction` 打开，见 `MultiCardDisplay.cs` 文件头 ⑤）。
        /// </summary>
        public void ShowMyDeck(bool on)
        {
            if (_multiCards == null || Ctx == null) return;
            if (!on) { _multiCards.Hide(); return; }
            var cards = new List<CardData>();
            var deck = Ctx.Players[_me].Deck;
            for (int i = 0; i < deck.Count; i++)
                if (deck[i] != null && deck[i].Card != null) cards.Add(ToCardData(deck[i].Card, _myFaction));
            _multiCards.Show(cards, "你的牌库");
        }

        void OnCardDeployed(CardView card, int slot)
        {
            if (_cardDisplay != null) _cardDisplay.Hide();   // 这张牌已经上场了，展示窗别留着
            if (_multiCards != null) _multiCards.Hide();     // 多张那个窗同理
            int idx = HandIndexOf(card);
            if (idx < 0)
            {
                Debug.LogError("[Battle] 落位回调找不到这张牌的手牌索引 —— 引擎和画面不同步了");
                return;
            }
            // 🆕 先把这张卡里「本该问玩家」的问完（一处都没有就直接打出去）—— `BeginPlay` 里分流
            BeginPlay(card, idx, slot);
        }

        /// <summary>真的把这张牌打出去（面板问完之后由 `NextAsk` 调；不用问时 `BeginPlay` 直接调）。</summary>
        void DoPlay(CardView card, int idx, int slot)
        {
            // 🔴 🆕 2026-10-18（A938）：**教程白名单闸门 · 第 ② 点** —— 原版 `IsValidSpellTarget` 那一跳
            //    （出牌链路里**结算前**那一次；① 那处是「这一格能不能落」的查询口，会随指针每帧问）。
            //    ⛔ 这一处**必须出声**：走到这儿说明玩家**真的**打出去了。
            if (!TutorialPermits(TutAttemptPlayResolve(card, idx, slot)))
            { RejectByTutorial("打出手牌第 " + idx + " 张 → 槽 " + slot); return; }
            // 战术卡：**不落格位** —— 它打出去就没了（效果已经结算完），视图直接销毁。
            // 单位卡才走下面「从手牌变成场上单位」那条路。
            bool tactic = !card.Data.isUnit;
            // 🆕 2026-09-26（N4）：联机局里这条动作要能发对面 ⇒ 走 `LocalAct`（单机下与老代码一字不差）
            var playAct = new AiAction { Kind = AiActionKind.PlayCard, HandIdx = idx, Slot = slot };
            // 🔴 2026-10-01：**先记下要打的是哪一份实例** —— 棋盘改成连续模型之后，
            //    `slot` 只是「拖到哪一格」（= 插入位置），**它真正落在哪一格要问引擎**
            //    （插进中间会把别人推出去；这张牌自己的效果又可能再插/挪人）。
            var playedInst = (idx >= 0 && idx < Ctx.Players[_me].Hand.Count) ? Ctx.Players[_me].Hand[idx] : null;
            int code = LocalAct(playAct, () => RuleCore.PlayCard(Ctx, _me, idx, slot));
            if (code != RuleCodes.OK)
            {
                Debug.LogError($"[Battle] 引擎拒绝了这次落位（{HintForCode(code)}）—— "
                             + "校验委托和实际出牌用的不是同一份判据");
                return;
            }
            _cardsPlayedThisTurn++;             // 本回合已出牌数（原版 `CardsPlayedInTurn1..3` 那三枚灯）
            // 🆕 2026-10-01：「这一次松手要把牌打下去」这个标记用完了 —— 让位预览那边不再需要它兜着
            if (interaction != null) interaction.DropAccepted = false;

            // 🔴 2026-10-01：把「请求的落点」换成「引擎里它**真正在**的那一格」（按实例身份找）。
            //    找不到 = 它刚上场就被自己的效果弄没了（极端情况）⇒ 保持请求值，让 `SyncBoard` 去收尾。
            int land = SlotOfInstance(_me, playedInst);
            if (land >= 0 && land != slot)
            {
                Debug.Log($"[Battle] 落点回填：请求槽 {slot} → 实际槽 {land}"
                        + "（连续棋盘：插进中间会把后面的单位整体推出去一格）");
                slot = land;
            }

            _handViews.Remove(card);
            if (tactic)
            {
                Kill(card.gameObject);          // 批处理下 Destroy 不生效，`Kill` 会走 DestroyImmediate
                RefreshAll();
                UpdateHud();
                ReportUnaskedChoices();
                ContinueTutorialScript();       // 🆕 A938：玩家做完那一步 ⇒ 脚本指针 +1
                AutoEndTurnIfStuck();
                return;
            }

            // 这张卡从手牌变成场上单位：视图也搬过去，别重建（重建会丢落位动画）
            var landedUnit = _ctx_CurrentUnit(slot);
            card.SetData(ToCardData(landedUnit, _myFaction));
            // 🔴 **换展示场景**：出牌是「搬视图」不是「重建视图」⇒ 出生时是**手牌那一套**
            //    （卡框/费用/宝石/卡名/效果文字）。场上按原版只有立绘+数值+徽标，这一步少不了 ——
            //    漏了的话「自己打出去的兵在场上仍带着卡框」而督军是对的（督军出生就在场上）。
            card.SetFace(CardFace.Board);
            _myUnits[slot] = card;
            RegisterUnitView(_me, landedUnit, card);   // 🆕 2026-10-01：登记身份（它以后换格靠它认人）
            card.transform.SetParent(boardRoot, true);

            // 登场特效**不在这儿播** —— 引擎在 `PlayCard` 里已经发了一条 Deploy 事件，
            // 下面这次 `RefreshAll()` 会把它翻译成特效（表现层只有那一个出口）。
            // 这样 AI 出的牌也带着卡名，两边走的是同一条路。
            RefreshAll();
            UpdateHud();
            ReportUnaskedChoices();
            ContinueTutorialScript();       // 🆕 A938：玩家做完那一步 ⇒ 脚本指针 +1
            AutoEndTurnIfStuck();
        }

        /// <summary>
        /// **一次动作结算完之后清账**（🆕 2026-10-17（A904）扩成两半）。**每次引擎动作之后都要调。**
        ///
        /// ① **引擎替玩家挑了几处**（`ChooseSites &gt; ChooseAnswered`）—— 既有那一句，判据不变；
        ///    现在只报**新**增的（水位见 `_settledSites`），所以在一次动作里被调多次也只会说一遍。
        /// ② 🔴 **「清队」** —— `ctx.ChoosePicks` / `ChooseCardIds` 里**还剩着**的答案
        ///    （这一次动作没人来取）。`TakePick` / `TakePickCard` **不认这条答案属于哪个 ask 点**
        ///    （`EffectResolver` 的 `case "hand"` / `case "enemyhand"` 那两处）⇒ 留着它就**一定会被下一个 ask 点吃掉**
        ///    （而那个 ask 点**可能是对手 / AI 的**）⇒ 此后每一处选择**全部错位、而且不报错**。
        ///    所以：**当场报出来 + 清掉**（⚔️ 这一条就是 A904 里「归属」那一半的落地）。
        ///
        /// ⚠️ **这不是「引擎替玩家挑」那件事**（两件事要分开看）：
        ///    · ① 是「该问没问」，答案由引擎掷骰子；
        ///    · ② 是「问了没人取」，答案白问（引擎照样掷骰子）—— 两者都会走到 `ctx.Rng`。
        /// ✅ 2026-09-14：四族（`choosecard` / `chooseone` / `chooseeffect` / `become`）**都有面板了**，
        ///    剩下会漏的还是「**ask 点不在被问的那张卡 desc 里**」的那些
        ///    （事件层的监听正文、`When …` 之类 —— 见 `EffectResolver.PlayerChooseOps` 的注释）。
        /// </summary>
        void ReportUnaskedChoices()
        {
            if (Ctx == null) return;

            // ---- ① 引擎替玩家挑了几处（只报新增的）----
            int sites = Ctx.ChooseSites, answered = Ctx.ChooseAnswered;
            int missed = sites - answered, reported = _settledSites - _settledAnswered;
            if (missed > reported)
                Ctx.Log($"⚠️ 这次结算里有 **{missed - reported} 处选择是引擎替你挑的**"
                      + $"（本该问 {sites} 处、面板问了 {answered} 处）");
            _settledSites = sites; _settledAnswered = answered;

            // ---- ② 剩在队里的答案：报出来 + 清掉（不清就一定会被下一个 ask 点吃掉）----
            int left = Ctx.ChoosePicks.Count + Ctx.ChooseCardIds.Count;
            if (left == 0) return;
            Ctx.Log($"⚠️ 选牌面板：有 **{left} 格答案没被用掉**"
                  + "（这一条动作里对应的那个 ask 点没发生，或者它自己没轮到）——"
                  + "**已清空**，免得被下一处选择当成自己的答案（那会让此后每一处选择全部错位）");
            Ctx.ChoosePicks.Clear();
            Ctx.ChooseCardIds.Clear();
        }

        UnitState _ctx_CurrentUnit(int slot)
        {
            return Ctx.Players[_me].Board[slot];
        }

        /// <summary>把 `_myUnits` 里那张刚出场的视图**登记进身份表**（`SyncBoard` 里新建视图时也会登记，
        /// 但出牌这条路是**搬视图**、不走那一段 ⇒ 这里补一次，否则「它下一回合换格」时认不出是谁）。</summary>
        void RegisterUnitView(int owner, UnitState u, CardView v)
        {
            if (u == null || v == null) return;
            UnitViewMap(owner)[u] = v;
        }

        /// <summary>**按实例身份**找这一份牌现在在哪一格（找不到 = 不在场上，返回 -1）。
        /// 🔴 2026-10-01：连续棋盘模型下「拖到哪一格」≠「落在哪一格」，落点必须回来问引擎。
        /// 判据只此一处 —— 别在别处再写一遍「扫棋盘找实例」。</summary>
        int SlotOfInstance(int owner, RuleEngine.CardInstance inst)
        {
            if (inst == null || Ctx == null) return -1;
            var b = Ctx.Players[owner].Board;
            for (int s = 0; s < BoardSpec.Size; s++)
                if (b[s] != null && b[s].Instance == inst) return s;
            return -1;
        }

        // ==================================================================
        //  每帧
        // ==================================================================

        void Update()
        {
            // 🔴 **第一句就先把这一帧的两条沿算掉**（A462）—— 理由见 `PollInputEdges` 的注释：
            //    下面的分支会提前 return，边沿若算在分支里，松手会被推迟一帧、变成凭空的点击。
            //    ⚠️ 放在 `Ctx == null` 那道闸**之前**：那一档 `Update` 整个返回，
            //      边沿不刷的话 `_held` 会停在上一局的最后状态 ⇒ **新开一局的第一帧会凭空多一条边沿**。
            PollInputEdges();
            if (Ctx == null) return;
            NetTick();          // 🆕 2026-09-26（N4）：联机局收包 + 落地对面的动作（自检里显式调 `NetTick`）

            // 🆕 2026-09-20 加时：引擎一旦把 `IsOvertime` 置真就播一次。
            // 原版那道 `if (!IsOvertime)` 闸决定了**只播一次**（`BattleManager._NextTurn`）。
            if (Ctx.IsOvertime && !_overtimeFired) ShowOvertime();
            TickOvertime(Time.deltaTime);   // 批处理下 deltaTime = 0 ⇒ 自检直接调 `TickOvertime`

            // 🆕 2026-10-01：「让位」预览 —— 拖拽中把场上单位推向「插进去之后」的位置
            //（原版 `MinionManager.ReassembleMinionsWhilePlayingUnit` 每帧那条；只在 `animateFeel` 开时走）
            TickShufflePreview(Time.deltaTime);

            // 🆕 2026-09-20 悬停信息层（原版 `EverguildTooltipTrigger` 挂在卡面数值容器与 HUD 计数上）
            TickTooltip();

            // 🆕 2026-09-29 战斗日志：悬停行内卡名 ⇒ 弹一张卡（原版 `CemeteryManager.CheckCardLink`）
            TickLogCard(WorldPointer());

            // 🔴🆕 2026-10-18（`A915`）：**对手掉线/等待重连期间，玩家禁操作** ——
            //   判据 = 原版 `BattleManager__Update.c` 开头那道**整帧闸**（`:104-115`，逐句读过）：
            //     ```
            //     iVar17 = *(int *)(param_1 + 0x28c);           // = BattleManager.ConnectionStatus
            //     if ((iVar17 != 0) && (iVar17 != 3)) {          // 0 = Connected · 3 = Disconnected
            //         if (*(char *)(param_1 + 0x510) == '\0') return;   // 没到「等待离开」⇒ **整帧返回**
            //         …（只有 matchFinishedAndWaitingToLeave 那一档才接着走「点一下就离开」）
            //     }
            //     ```
            //     ⇒ 状态 ∈ {1 Reconnecting, 2 WaitingForOtherPlayerToReconnect, 4 EnemyForfeit,
            //       5 DisconnectedAfterTryingToReconnect} 时**这一帧什么都不做**。
            //     ⚠️ **那颗重连弹窗是【并行现象】，不是这道闸**：它由 `NetRuntime.ShowPopUp` 走 Shell 那一层，
            //        与 `BattleDriver.Update` 无关（判据 → `Net/NetBattle` 里那段「逐环节 + 没有回滚」的注释块）。
            //   **我们的等价物 = `NetClockPaused`**（它读 `NetBattle.ClockPaused`，而那一位正是原版
            //    `ShowDisconnectionPopup` 那一刻 `PauseClock()` 置的；判据 → `BattleDriver.TickClock` 里那段）。
            //   ⚠️ **单机局 `_net == null` ⇒ 恒 false ⇒ 这一句是空操作**（与加它之前逐字节等价）。
            //   ⚠️ **排在这里**：上面那些是「环境每帧跑的东西 + `NetTick`」，下面才是**玩家输入与回合驱动** ——
            //      原版那道闸拦的就是后面这些；批处理自检也走同一条路。
            //   ⛔ **不是**复用 `CardInteraction.enabled`（那个位已经用作换牌语义，
            //      见 `Hand/CardInteraction.cs` 里 `:255` / 拖拽那几处）—— 另开一个只读的口传给手牌那一层。
            //   ⚠️ 手牌拖拽**不走 `BattleDriver`**（`CardInteraction` 自己是个 `MonoBehaviour`，
            //      有自己的 `Update`）⇒ 那边**另有一道**（`CardInteraction.Frozen`，`Begin` 里接上）。
            //   ⚠️ **对局已经打完时不拦**：原版那道闸自己就带一个例外（`+0x510`
            //      `matchFinishedAndWaitingToLeave` 为真就接着往下走，那一段是「点一下离开战场」）。
            //      我们的等价物 = `Ctx.IsOver`（终局那一支的入口）。不加这个例外的话，
            //      打到一半掉线、随后对局结束 ⇒ 玩家**出不去**（软锁）。
            if (PlayerInputFrozen && !Ctx.IsOver) { UpdateHud(); return; }

            // 多张展示窗开着 ⇒ **先吃掉点击**（原版 `UIMultiCardDisplay`：`Continue` 与背景都能关）。
            // ⚠️ 排在回放条**之前** —— 窗开着的时候它就是最上面那一层。
            if (TickMultiCards()) return;

            // 🆕 2026-09-29：放大窗 —— **指针移开就关**（原版 `CardCollider.OnPointerExit` →
            //    `CardScript.OnTouchExit`，唯一守卫是 `displayingCardFlag`；原来我们走的是「再点一下关」）。
            //    判据只此一处，见 `TickCardWinPointerExit`。
            if (TickCardWinPointerExit(WorldPointer())) return;

            // 放大窗开着时：**这一下点击先交给窗**（卡格换位 / 语音钮 / 眼睛钮 —— 判据只此一处）
            if (TickCardDisplayClick()) return;

            // 回放条（原版 `ReplayButtons`）：先吃掉点击 —— 暂停 / 单步 / 重开都在这一下里做完
            if (HandleReplayBar()) { UpdateHud(); return; }

            // 事件时间线：每帧推 —— 动作才有节奏，不是同一帧全点着
            // ⚠️ **暂停时这一条不推**（和下面的时钟、AI 一起停 —— 「暂停」就是停这三处）
            if (!_replayPaused) AdvanceTimeline(Time.deltaTime);

            // 换牌阶段：**在最前面**（这时对局还没开始，下面那些结算/回合逻辑一条都不该跑）
            // 倒计时要排在面板点击**之前**：到 0 自动完成时走的是和点「完成换牌」**同一条收尾**
            //（原版也是 `ProcessMulliganDone`），所以这一帧必须就此打住。
            if (TickMulligan(Time.deltaTime)) { UpdateHud(); return; }
            if (HandleMulligan()) { UpdateHud(); return; }
            if (HandleChoose()) { UpdateHud(); return; }     // 🆕 选牌面板开着时也吃掉这一帧的输入

            if (Ctx.IsOver)
            {
                // ⚠️ **这里不是结算**：账全在 `UpdateHud()` 里记（那才是「结算那一块」的唯一落点，
                //   回放闸与面板那两道都在那儿 —— A381/A382）。这一支只管「终局帧的输入」。
                UpdateHud();
                // 🔴 **2026-10-16（W22）：原版的出口**（判据全文 → `HandleEndBattleExit` 上面那一段）：
                //   闸门（开门视频播完 + 0.15s）开了之后，**左键任意处 / ESC** ⇒ 回主菜单场景。
                //   ⚠️ **必须有这一句** —— 在此之前全仓从战场回主菜单的 `LoadScene` **一处都没有**，
                //     玩家打完一局**出不去**（只能按 R 重开，而 R 是我们自己的调试键）。
                if (HandleEndBattleExit()) return;
                // ⚠️ **`R` 是我们自己的调试键**（原版没「再来一局」，见 `Restart` 的文档）——
                //   它原来接的是结算面板上那句提示文字，**那句已经删了**（原版那屏零文字），
                //   但键**照旧留着**（自检与手工验收都要用），只是不再对外承诺。
                // 🔴 回放局按 R 也一样：那一下走 `Restart()` → `Begin()`，`Begin` 会把
                //   `_replaySession` 清零 ⇒ **新开的那一局是正常对局**（账照记）。
                if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) Restart();
                return;
            }

            // 设置面板 / 设置按钮：**两个回合都能用**（原版随时能开）
            if (HandleSettings()) { UpdateHud(); return; }
            // 🆕 2026-09-27：我们那套时间控制的**键盘**入口（`Space` / `.`）—— 原版那四颗回放钮
            // 在普通对局里是**不显示**的（`ReplayHud.Setup()`），所以功能挪到这里，见方法注释。
            if (HandleTimeControlKeys()) return;
            if (HandleChatPopup()) { UpdateHud(); return; }   // 🆕 `ChatPopup`（模态，同设置面板）
            if (HandleOffensiveButton()) { UpdateHud(); return; }   // 🆕 2026-09-29（§25）进攻卡钮
            // 🆕 2026-10-12（A423）：HUD 那颗「重置自动镜头」钮（原版 `BattleHud.resetCameraZoomButton`）。
            // 排在这里：它和进攻卡钮同族（都是 HUD 上「点了立刻做一件事」的小钮），且两块命中区不重叠。
            if (HandleCameraResetButton()) { UpdateHud(); return; }
            if (HandleBattleLog()) { UpdateHud(); return; }
            // 🆕 2026-10-18（A985③）：原版 `BattleHud.alliancePanelOpenButton`（= **敌方名牌本身**）
            //   与它开的那扇对手档案窗。排在这里：两块命中区不重叠
            //   （日志钮 y[135.9, 200.1]、名牌那颗钮 y[28, 121.45]）。
            if (HandleAlliancePanel()) { UpdateHud(); return; }

            // 暂停时：面板照常能开（上面两条），但时钟与两个回合的驱动都停
            if (!_replayPaused)
            {
                TickClock(Time.deltaTime);

                // 🆕 2026-10-18（A940）：**教程表现层**的显式步进（淡入 / 督军两拍 / 跳过钮）。
                // ⚠️ 它**不走 `Update` 里那套补间族**（那族挂在 `AdvanceTimeline` 那个泵上）——
                //    这两个口在批处理里都不跑 ⇒ 自检另有 `TickTutorialViewForTest`（显式喂 dt）。
                TickTutorialView(Time.deltaTime);
                // 🔴 **2026-10-18（A991）：这里原来有一句 `if (HandleTutorialSkip()) return;` —— 已删。**
                //   跳过钮现在建在**设置面板**里（原版 `SkipTutorial Button` 就在 `BattleSettingsPanel/
                //   Bottom buttons`），入口是 `SettingsClickAt`（面板开着时那条模态链）——
                //   上面那句 `HandleSettings()` 早就把开着的设置窗整帧接管了 ⇒ 原来这条**根本到不了**。
                //   ⛔ 别把它加回来（那是「两个入口」）。
                // 🆕 2026-10-18（`Z6`）：提示挂着时**点一下就能消**（原版 `TutorialTipScript` 那一侧
                //   清 `WaitingForTutorialTipFlag`；`minTimeBeforeSkip` 那 1 秒内不算，但仍吃掉这一下）。
                // ⚠️ 排在跳过钮**之后** —— 那颗是具体按钮，优先（别让提示把它那一口吃掉）。
                if (HandleTutorialTipDismiss()) return;

                if (Ctx.Active == _me) DrivePlayerTurn();
                // 🔴 **联机局：对面那一侧**绝不能**跑 AI** —— 那是网络的活（`NetTick()` 把对面对作落地）。
                //    不拦这一条的话，AI 会和网络**同时**给对面出招 ⇒ 两边立刻打岔。
                else if (AiShouldDriveOpponent) DriveAiTurn();
            }

            UpdateHud();
        }

        /// <summary>🆕 2026-10-13（A462）：`Update` 里的「多卡摊开窗」那一支 —— **抽成方法只是为了让自检
        /// 走同一条判据**（`TickMultiCardsForTest`），判据一个字都没动。返回 true = 这一帧就此打住。
        /// 🔴 **松手那一帧**才关（原来判的是「按下」）—— 原版这一窗的两个关闭入口
        /// （背景 `Menu Dark Background` 上的 `BackgroundCloseButton`、`Close` 钮）
        /// 都是 uGUI `IPointerClickHandler` / `EverguildButton` ⇒ 抬起。</summary>
        bool TickMultiCards()
        {
            if (_multiCards == null || !_multiCards.Visible) return false;
            bool tapped = ReleasedThisFrame();
            Vector3 wp = WorldPointer();
            bool onDeck = HitMyDeckPile(wp);
            if (tapped) _multiCards.Hide();
            UpdateHud();
            if (tapped && onDeck) ShowMyDeck(true);   // 点牌堆本身 = 关掉（别立刻又开一次）
            return true;
        }

        /// <summary>🆕 2026-10-13（A462）：`Update` 里的「放大窗」那一支 —— 同上，抽出来只为自检能走真判据。
        /// 🔴 **松手那一帧**（原来判「按下」）—— 那四条支路原版全是 uGUI 点击：
        /// `backgroundButton`(`BackgroundCloseButton`) · `voiceOverButton`/`showCardTextButton`
        /// (`EverguildButton`) · 卡格 `UIGenericEventCatcher.IPointerClickHandler`。
        /// 返回 true = 这一下被窗吃掉了（**只有三条全真才返回 true** —— 窗开着但没人接，照旧往下走）。</summary>
        bool TickCardDisplayClick()
        {
            if (_cardDisplay == null || !_cardDisplay.Visible) return false;
            return ReleasedThisFrame() && HandleDisplayWindowClick(WorldPointer());
        }

        /// <summary>轮到玩家时把表拨回去（原版 `ClockManager.StartTimer` 的等价物）。</summary>
        void ResetClock()
        {
            _clockLeft = TurnSecondsForThisMatch;
            _clockInCountdown = false;
            _hurrySaidThisTurn = false;             // 原版 `ClockManager.StartTimer`：`latch_0xb8 = 0`
            _actionsThisTurn = 0;
            _cardsPlayedThisTurn = 0;               // 新回合：三枚「已出牌数」灯灭掉（原版也只在出牌后亮）
            UpdateClockLabel();
        }

        /// <summary>走表。只有**玩家的回合**走（原版时钟也只给行动方看）。</summary>
        void TickClock(float dt)
        {
            if (Ctx == null || Ctx.IsOver || Ctx.Active != _me) return;
            // 🆕 2026-10-17（B17·A901）：**对手掉线期间，对局时钟停走**。
            //   判据 = 原版 `BattleManager__ShowDisconnectionPopup.c:27` 那一刻的 **`ClockManager.PauseClock()`**
            //   （它把 `ClockManager+0x85` 置 1；`ClockManager__Update.c:36` 与 `ClockManager__ClockRunning.c:5`
            //   **都拿这一格当闸** ⇒ 表不走、`hurry` 也不喊）；对手回来那一刻
            //   `BattleManager__SuccessfulReconnection.c:51` 的 `ClockManager.UnpauseClock()` 放回 0。
            //   ⚠️ **单机局 `_net == null` ⇒ `NetClockPaused` 恒 false ⇒ 这一句是空操作** ——
            //     单机路径的行为**一个字节都没变**（自检里有一条盯着它，`NetBattleTest` §10①）。
            //   ⚠️ 闸只此一处：别的路径（AI 那一侧、表现层）**一概不动** —— 原版停的也只有这一个表。
            if (NetClockPaused) return;
            if (_clockLeft <= 0f) return;

            _clockLeft -= dt;
            // 🆕 2026-09-21：**本回合剩不到 35 秒 → 我方督军说一句 `hurry`**（每回合一次）。
            //    判据链与闸门见 `VoiceLines.ForHurry`；`Ctx.Active != _me` 上面已经挡过 ⇒ 天然只对我方。
            if (!_hurrySaidThisTurn && _clockLeft <= hurryUpSeconds)
            {
                _hurrySaidThisTurn = true;
                SpeakHurry();
            }
            if (_clockLeft <= 0f)
            {
                if (!_clockInCountdown)
                {
                    // 总时长走完 → 换成那 15 秒倒计时（原版 `ClockManager__Update.c:110-141`）
                    _clockInCountdown = true;
                    _clockLeft = countdownSeconds;
                }
                else
                {
                    // 倒计时也走完 → **自动结束回合**，没有额外惩罚（原版 `EndTurnClick(timeOutFlag=true)`）
                    _clockLeft = 0f;
                    UpdateClockLabel();
                    EndPlayerTurn();
                    return;
                }
            }
            UpdateClockLabel();
        }

        /// <summary>把秒数写到 END TURN 按钮里那行字上。
        /// ⚠️ 位置是**我们挑的**（原版 `ClockManager.clockText` 是 `Clock/TurnBtn` 子树里的一个 TMP，
        /// dump 里只列到 `TurnText` 和那个空节点，取不到它的 rect）；写法 m:ss / 倒计时直接写秒数。</summary>
        void UpdateClockLabel()
        {
            if (_clockLabel == null) return;
            int sec = Mathf.CeilToInt(Mathf.Max(0f, _clockLeft));
            _clockLabel.SetText(_clockInCountdown ? sec.ToString()
                                                  : $"{sec / 60}:{sec % 60:00}");
            _clockLabel.SetColor(_clockInCountdown ? new Color(1f, 0.35f, 0.30f)
                               : sec <= hurryUpSeconds ? new Color(1f, 0.72f, 0.30f)
                               : new Color(0.85f, 0.88f, 0.95f));
        }

        // ---- 玩家回合 ----

        void DrivePlayerTurn()
        {
            // 🔴 **2026-10-18（A938）：教程局里脚本这一帧推一条**（原版 `_NextTurn` 起的那条协程，
            //   `_NextTurn_d__395__MoveNext.c:427-431` —— 玩家回合也跑脚本）。
            //   ⚠️ 放在 `cam == null` 那道早退**之前**：批处理里没有相机，放后面的话
            //     自检根本推不动脚本（「脚本从没被驱动过」正是 A938 的老毛病）。
            //   ⚠️ 只有 `Advanced`（脚本真的做了一条）才把这一帧的输入吞掉；
            //     `WaitingForActor` / `Exhausted` 时**必须把输入放过去** —— 否则玩家永远点不下去。
            if (DriveTutorialScript() == TutorialStep.Advanced) return;

            // 🔴 🆕 2026-10-18（A938）：**教程白名单闸门 · 第 ⑭ 点** —— 原版 `BattleManager__Update`
            //   里那两条逐帧的「别做 / 等一下」：`CheckIfWaitingToUseAbility`（`:182`）与
            //   `CheckIfWaitingToChangeAttack`（`:1066`）。它们管的是「玩家已经开着的那个指挥状态
            //   **还该不该开着**」—— 脚本换了一条动作之后，上一个状态就该收掉。
            TutorialFrameGuard();

            if (cam == null) return;

            // 正在拖手牌时不接「选单位/打人」的点击 —— 那一下是 `CardInteraction` 的
            //（两边都读同一个鼠标，不挡的话拖牌时会顺带选中底下的单位）
            if (interaction != null && interaction.IsDragging)
            {
                // 手牌拖拽优先 —— 两边读同一个鼠标，不挡的话拖牌时会顺带指挥底下的单位
                CancelCommand();
                return;
            }

            Vector3 world = WorldPointer();

            // ① **选择器开着**：喂指针（压着谁就放大 1.3 倍 + 亮黄圈）+ **悬停即选中**
            //
            // 🔴 **2026-10-13（A462）照原版改 —— 原版这一处【根本不是点击】。**
            //    三颗钮的组件是 `CardDisplayAttackTypeButton`，它只实现 `IPointerEnter/ExitHandler`
            //    （`CardDisplayAttackTypeButton.cs:12`）；`__Update.c:20-30` 那三格一齐为真就发：
            //      `inputOverButton(+0x92)` && `sendInput(+0x90)` && `!isInputOverSent(+0x91)`
            //    ⇒ **指针悬停即选中**。`sendInput` 由 `Toggle(true)` → `StartSafeTouch` 打一个
            //    **0.1s 安全窗**（`__Toggle.c:15-21` + `<StartSafeTouch>d__37__MoveNext.c:17,31`：
            //      先置 0、等 `disableTimeAfterPointerExit`(=0.1) 再置 1）；指针一离开那颗钮
            //      （`OnPointerExit`）就把 `isInputOverSent` 清掉、并给**其它**钮重开安全窗。
            //    ⚠️ 安全窗/「一次进入只发一次」那两格在 `AttackSelector` 里（它才是持有 `Hovered` 的那件）。
            if (selector != null && selector.Visible)
            {
                selector.UpdatePointer(world);
                // ⛔ **不碰拖拽流程**（`TryDraggingFromBoard` 那一段没验全，见 WA462 §五·2）：
                //    这里只换触发沿 —— 把「点一下定下来」换成「悬停到某一格且安全窗走完」。
                var hot = selector.Hovered;
                if (hot != AttackKind.None && selector.HoverPickReady)
                {
                    selector.MarkHoverPicked();
                    CommitCommand(hot);
                    return;
                }
                // 槽外取消：**保持原样**（原版没有等价物，WA462 §四·3 —— ⛔ 别顺手删/别顺手改沿）
                if (ClickedThisFrame() && !selector.ContainsBar(world))
                {
                    ClearSelection();
                    // 这一按已经被选择器用掉了（取消）⇒ 吞掉**同一次按住**的松手沿：
                    // 不吞的话松手那一帧会继续往下打，压在「结束回合 / 选目标」上就会**顺带点掉它**。
                    _swallowNextRelease = true;
                }
                return;
            }

            // ② **按住棋盘上的单位**：松手没拖够 = **开/关大卡展示窗**；拖够阈值 = 弹攻击选择器
            //    🔴 **2026-09-28 照原版改的** —— 原来「没拖够也弹选择器」是我们为鼠标顺手加的偏离
            //      （当时那行注释自己写着「原版短按在那边是『开卡展窗』，我们还没有那个窗」—— 现在有了）。
            //    原版判据（反编译全文 → `资料/待办判据_战场与战斗视图.md` §8b）：
            //      · 轻点 = `CardScript.OnTouchUpAsButton` **C 段**（state 2/3/0x11）→ `BattleManager.DisplayCard`
            //        → `CardDisplayWindow.ShowBattleCard`（**这条链里就调 `DisplayCardEffects`**）；
            //        **棋盘段没有敌我判断**（`isPlayer` 守卫在**手牌段**）⇒ 对手单位也给开。
            //      · 拖拽 = 原版打开三选一的**唯一**入口（`TryDraggingFromBoard` → `StartAttackFrom`
            //        → `UnitOnBoardAttackTypeSelector.Toggle`）；阈值 = 原版 `accumulatedDragForMinDistance`。
            //      · 敌方**拖不动**（`OnTouchDrag` 里有 `isPlayer` 闸）⇒ 只有轻点那条路。
            if (BoardPress(world, PointerHeldRaw())) return;

            // ③ 正在选目标 → **每帧**把准星挪到指针压着的那个合法目标上（原版「我现在指着谁」）
            //    放在下面的两条沿之前 —— 它是持续反馈，不是只在点击那一下更新
            if (_selectedSlot >= 0 && _command != AttackKind.None)
            {
                UpdateReticle(world);
                // 技能卡面板：指针按在面板上会铺蓝色那层（原版 `LightPressed`）
                if (skillPanel != null && skillPanel.Visible) skillPanel.SetPointer(world, PointerHeldRaw());
            }

            // 🔴 **2026-10-13（A462）：③′ / ③ / ④ 走【抬起】，⑤ 走【按下】—— 两条沿并存，⛔ 别合并。**
            //    · ③′ 点我方牌堆 = **我们挑的入口**（原版那窗由 `BattleManager.ResolveAction` 开）⇒ 跟着这一行改沿。
            //    · ③ 结束回合 = 原版 uGUI：那颗 `TurnBtn`（`MonoBehaviour_4895`）由
            //      `ClockManager.EndTurnClick` 接在 `BattleHud` 的 `m_OnClick` 上 ⇒ 抬起。
            //    · ④ 选目标 = 点棋盘单位 = `CardCollider.IPointerClickHandler` ⇒ 抬起
            //      （`CardCollider__…OnPointerClick.c:48` → `CardScript.OnTouchUpAsButton()`）。
            //    · ⑤ 记 `_pressSlot` = **我们自己的中间态**（原版 `CardCollider` 也在
            //      `OnPointerDown` 记 `pointerDownPosition`）⇒ **必须留在按下那一帧**：
            //      `BoardPress` 靠「`held == false` 那一帧」判轻点，挪到松手帧会让
            //      `_pressSlot` 刚赋值就 `!held` ⇒ **「轻点开卡窗」与「拖拽弹选择器」的分流静默坏掉**。
            if (ReleasedThisFrame())
            {
                // ③′ 点**我方牌堆** → 把牌库摊开看
                if (HitMyDeckPile(world)) { ShowMyDeck(true); return; }

                // ③ 结束回合按钮（有原版按钮底图就按图判，没有就按文字判）
                if (HitEndTurn(world))
                {
                    EndPlayerTurn();
                    return;
                }

                // ④ 已经定好打法 → 这一下是选目标
                if (_selectedSlot >= 0 && _command != AttackKind.None)
                {
                    // 点的那一侧由 `CommandTargetSide()` 定（替代行动可能点**自己人**）
                    int side = CommandTargetSide();
                    int t = HitSlot(side == _me ? _myUnits : _foeUnits, world);
                    if (t >= 0) { Resolve(_command, t); return; }
                    ClearSelection();
                    return;
                }
                return;     // 抬起这一下没落在任何一件上 ⇒ 到此为止（原版：没人接就算了）
            }

            if (!ClickedThisFrame()) return;

            // ⑤ 点棋盘上的单位 → **先记下来**（松手照上面 ② 分流：轻点开大卡窗 / 拖够弹选择器）
            // 🔴 **正在等选目标时【不许记】**：那时按下这一下是 ④ 的起手，记了 `_pressSlot` ⇒
            //    松手那一帧会被上面那行 `BoardPress` 吃掉（它一接管就 return）⇒ **④ 永远轮不到**、
            //    攻击目标点不动。判据与 ④ 那一支**共用同一个条件**（别另抄一份）。
            if (_selectedSlot < 0 || _command == AttackKind.None)
            {
                int pick = HitSlot(_myUnits, world);
                if (pick >= 0) { _pressSlot = pick; _pressSide = _me; _pressWorld = world; return; }
                //    对手单位：原版棋盘段**没有敌我判断**（唯一的 `isPlayer` 守卫在**手牌段**）⇒ 也能开窗；
                //    但拖不动（`OnTouchDrag` 的 `isPlayer` 闸）⇒ 只走轻点那条（② 里按 `side` 判）
                int foePick = HitSlot(_foeUnits, world);
                if (foePick >= 0) { _pressSlot = foePick; _pressSide = 1 - _me; _pressWorld = world; }
            }
        }

        /// <summary>棋盘上「按住某个单位之后」怎么分流 —— **判据只此一处**（`Update` 的 ② 段与批处理自检共用）。
        /// `held` = 左键现在还按着吗。返回 true = 这一下处理完了（`Update` 就该 return）。
        /// 分流照原版：**轻点 = 开/关大卡窗** · **拖够 = 弹三选一**（只对我方 —— 原版敌方拖不动）。</summary>
        bool BoardPress(Vector3 world, bool held)
        {
            if (_pressSlot < 0) return false;
            bool dragged = (world - _pressWorld).magnitude > AttackSelector.DragThresholdWorld;
            int s = _pressSlot, side = _pressSide;
            if (!held)
            {
                // 松手 ⇒ 轻点：开/关大卡窗（**拖过的松手不算轻点** —— 原版拖拽起手就把卡面收了）
                _pressSlot = -1; _pressSide = -1;
                if (!dragged) ToggleUnitCard(side, s);
                return true;
            }
            if (side == _me && dragged)
            {
                _pressSlot = -1; _pressSide = -1;
                OpenCommand(s);
            }
            return true;
        }

        /// <summary>指针现在**按着**（= 按住，**不是**「按下那一沿」）。
        /// <para>🔴 **2026-10-14（A651）改名**：原来叫 `PointerDown()` —— 那个名字与 `ClickedThisFrame()`
        /// （真正的**按下沿**）、以及原版 `EventTrigger` 的 `eventID 2 = PointerDown` **三者撞在一起**，
        /// 读调用点的人会以为它是沿。它的体**一直是 `isPressed`**（按住），故改名为 `PointerHeldRaw`。</para>
        /// <para>⚠️ **与 <see cref="PointerHeld"/> 的分工**：那一个是**同一件事**、但多一个自检钉死口
        /// （`PointerHeldForTest`）。这里**刻意不合并成一个**：`BoardPress` / 技能卡面板这两处原来
        /// 走的就是「真设备」那一支，合并会让自检钉死的值**多影响两条调用链** ⇒ 行为会变。
        /// 两个名字现在**共用同一份设备读取**（`PointerHeldRaw`），不会再出现「函数体抄两份」。</para>
        /// <para>调用点只有两处（`BoardPress(…, PointerHeldRaw())` 与 `skillPanel.SetPointer(…, PointerHeldRaw())`）。</para></summary>
        static bool PointerHeldRaw()
        {
            return Mouse.current != null && Mouse.current.leftButton.isPressed
                || Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed;
        }

        /// <summary>手牌开始拖了 → 把指挥状态收干净（别让它挂着等松手）</summary>
        void CancelCommand()
        {
            if (_selectedSlot >= 0 || _pressSlot >= 0 || (selector != null && selector.Visible))
                ClearSelection();
        }

        /// <summary>
        /// 弹出**攻击方式选择器**（原版的 `Drag Attack Selector`）。
        ///
        /// 给几项由引擎说了算：
        ///   · 近战 —— 近战攻击力 &gt; 0
        ///   · 远程 —— 远程攻击力 &gt; 0（**这才是原版的规则**：玩家自己选，不是我们按数值替他选。
        ///     以前那套「远程攻击力更高就自动用远程」是权宜之计，现在退回成**只给 AI 用**）
        ///   · 主动技能 —— 卡上有 `Ability:` 且 `CanStartAbility` 放行
        /// 一个都没有就不弹，直接说清楚为什么。
        /// </summary>
        void OpenCommand(int slot)
        {
            ClearSelection();

            var u = Ctx.Players[_me].Board[slot];
            if (u == null) return;

            // 🆕 2026-09-25 **点残骸 = 收集灵魂石**（原版 `PlayerActions.clickWaystone = 6`
            //   → `BattleActionType.useWaystone = 76`）。
            //   🔴 **必须排在下面那两道 `Exhausted` / `IsStunned` 闸【之前】** ——
            //      残骸造出来时就是 `Exhausted = true`（引擎那一段有注释），放后面的话
            //      会先被 `THIS UNIT ALREADY ACTED` 挡掉 ⇒ **灵魂石永远收不了，而且不报错**。
            //   ⚠️ 收集**不是**单位的行动：它不花行动、也不看这单位本回合动没动过
            //      （原版这条判定里没有那两条，见 `RuleCore.CanCollectWaystone` 的注释）。
            if (RuleCore.CanCollectWaystone(Ctx, _me, slot) == RuleCodes.OK)
            {
                // 🔴 🆕 2026-10-18（A938）：**教程白名单闸门 · 第 ⑧ 点** —— 原版 `BattleManager__CanUseWaystone.c`
                //   里那一次 `CheckIfPlayerActionPermittedInTutorial`（`BattleActionType.useWaystone = 76`）。
                if (!TutorialPermits(TutAttemptWaystone(slot))) { RejectByTutorial("收集灵魂石"); return; }
                var wsAct = new AiAction { Kind = AiActionKind.CollectWaystone, Slot = slot };
                int rc = LocalAct(wsAct, () => RuleCore.CollectWaystone(Ctx, _me, slot));
                // 🆕 2026-10-18（`A985⑧` · 第一步）：**这一处是「玩家动作被引擎拒」三处里唯一直接写
                //   提示行的**（另两处 `DoPlay` / `DoResolve` 是开发者日志）⇒ 走 `HintForCode`
                //   （有原版键就用键、没键仍走 `Describe`，兜底与那个「`??` 的洞」见它的 doc）。
                if (rc != RuleCodes.OK) SetHint(HintForCode(rc));
                else ContinueTutorialScript();     // 🆕 A938：玩家做完那一步 ⇒ 脚本指针 +1
                return;
            }

            // 🔴🆕 **2026-10-18（第十五轮 · `G5`）就地订正**：这两句原来走 `CardText.Phrase`
            //   （键 = 英文原文，`Phrases` 表里那 17 条**没有对应 `mTerm`** 的兜底短语之一）。
            //   **上一句「这个单位已经行动过了」的原版键其实在本地** —— `Battle/Tips/UnitNotReady`
            //   （`0x428AAB0`；消费点与判据字段见 `UnitNotReadyTerm` 的 doc：原版问的是
            //   `CardScript.canAct`，我们这一个 `Exhausted` 正是它的等价物）⇒ 第一句改走词条。
            //   ⚠️ 第二句（`STUNNED`）**保持 `Phrase` 不动**：`Battle/Tips/` 那 24 条里**没有**
            //      眩晕这一档，原版眩晕单位走的是**同一句** `UnitNotReady`（同一个 `canAct` 闸）——
            //      照原版照做会**丢掉「因为眩晕」这个信息**，而这是我们自己加的一句更准的话。
            //      如实记：**这一处我们比原版多说了一个字**（不是缺漏，是刻意保留）。
            if (u.Exhausted) { SetHint(Loc.T(UnitNotReadyTerm)); return; }
            if (u.IsStunned) { SetHint(CardText.Phrase("STUNNED")); return; }

            bool melee = RuleCore.FieldAttack(Ctx, _me, u, false) > 0;
            bool ranged = RuleCore.FieldAttack(Ctx, _me, u, true) > 0;
            // 「主动技能」这一格**两种来源**：
            //   ① 我们自定的 `Ability:`（封闭文法，v1 的近似）
            //   ② 🆕 **原版的替代行动**（`Duty` / `Pray` / `Ferocity` / `Agenda`，2026-09-13 A2）
            // 原版本来就只有**一个**主动技能按钮，这两者在原版里是同一个位置 ⇒ 合成一格。
            string alt = AltActionOf(u);
            bool oath = HasOath(u) && RuleCore.CanUseOathAbility(Ctx, _me, slot) == RuleCodes.OK;
            bool skill = (u.HasAbility && RuleCore.CanStartAbility(Ctx, _me, slot) == RuleCodes.OK)
                      || (alt != null && RuleCore.CanUseAlternative(Ctx, _me, slot, alt) == RuleCodes.OK)
                      || oath;   // 🆕 2026-09-16 誓约能力也占这一格（原版只有一个主动技能按钮）

            if (!melee && !ranged && !skill)
            {
                SetHint(CardText.Phrase("THIS UNIT CANNOT ACT"));
                return;
            }

            // 🔴 🆕 2026-10-18（A938）：**教程白名单闸门 · 第 ⑥⑦ 点** —— 原版 `CanAttackCard` 与
            //   `CanUseActiveAbility`（各自的 `CheckIfPlayerActionPermittedInTutorial` 调用点）。
            //   它们管的是「**这一格给不给开**」：脚本这一回合要是只许「攻击某一只」，
            //   那这一格的**技能按钮就不该出现**（拦在数值上、不在结算上 —— 和原版同一层）。
            //   ⚠️ 与别的点不同，这一处**结果直接影响按钮列表**，所以拦住时**出声**（否则看起来像没反应）。
            if (melee || ranged)
            {
                if (!TutorialPermits(TutAttemptAction(slot, BattleActionType.attack)))
                {
                    if (!skill) { RejectByTutorial("指挥这一格攻击（`CanAttackCard`）"); return; }
                    melee = false; ranged = false;
                }
            }
            if (skill && !TutorialPermits(TutAttemptAction(slot, BattleActionType.playActiveAbility)))
            {
                if (!melee && !ranged) { RejectByTutorial("让这一格放主动技能（`CanUseActiveAbility`）"); return; }
                skill = false;
            }
            if (!melee && !ranged && !skill)
            { SetHint(CardText.Phrase("THIS UNIT CANNOT ACT")); return; }

            _selectedSlot = slot;
            _myUnits[slot].SetHighlight(CardHighlightState.Selected);

            var opts = new List<AttackSelector.Option>();
            if (melee) opts.Add(new AttackSelector.Option { Kind = AttackKind.Melee, Enabled = true });
            if (skill) opts.Add(new AttackSelector.Option
            {
                Kind = AttackKind.Ability, Enabled = true,
                // 角标是**数值**（原版 `ValueText` 就是个大数字）。完整文字写在中间那条提示行上 ——
                // 原版是弹 `ActiveSkillDesc` 技能卡（名字/费用/描述/可选目标数），我们还没做
                // ⚠️ 替代行动没有「一个数字」（它的正文在 `TriggerOps` 里）⇒ 角标留空。
                // 🆕 誓约有数字：就是它要付的能量（`Oath N:` 的 N）。
                Badge = u.Ability != null ? u.Ability.Amount.ToString()
                      : oath ? u.Card.OathCost.ToString() : "",
                // 🆕 2026-09-25 **这一格的底图按卡换**：带替代行动/誓约的卡用它自己那张专属按钮图
                //    （原版就是运行时给 `buttonIcon` 赋值的；五张图已导进 `Resources/Art/ui/`）。
                //    没有替代行动的卡 ⇒ null ⇒ 落回 `AttackSelector.IconName` 那张占位图。
                IconName = AttackSelector.AltIconFor(alt, oath),
            });
            if (ranged) opts.Add(new AttackSelector.Option { Kind = AttackKind.Ranged, Enabled = true });

            if (selector != null)
                selector.Show(opts, CardText.Phrase("CHOOSE ACTION"),
                              // 技能效果写在条**上方** —— 中间那条提示行正好被按钮压住
                              skill ? CardText.Name(u.Name) + ": " + ActiveActionText(u, alt, oath) : null,
                              // 🆕 2026-09-29：**整条挂到被拖的那个单位身上**（原版展开时
                              //   `set_position(Get2DWorldPosFromBoardPos(被拖单位.position))`）。
                              //   取不到视图就退回屏幕中心（`Show` 里 `atWorld = null` 那条）。
                              BoardViewAt(slot) != null ? BoardViewAt(slot).transform.position
                                                        : (Vector3?)null,
                              // 🆕 2026-09-29：那圈黄圈挂**这个单位上一次用的打法**（原版 `unit.attackType`；
                              //   0 = 还没打过 ⇒ 不亮）。判据 → `AttackSelector.RefreshPicked`。
                              u.LastAttackType);
            SetHint("");
        }

        /// <summary>
        /// 这个单位的**替代行动**关键词（`Duty` / `Pray` / `Ferocity` / `Agenda`；没有 = null）。
        /// 实测全卡池**没有一张卡同时带两个**（见 `RuleCore.AvailableAlternative` 的注释）⇒ 取第一个就够。
        /// 🔴 **判据不在这里** —— 转发到 `RuleCore.AlternativeActionKeyword`（原来两边各写一遍；
        ///    技能路的致死预览也要用它挑「这一手到底走哪条来源」，再写第二份迟早不一致）。
        /// </summary>
        string AltActionOf(UnitState u) { return RuleCore.AlternativeActionKeyword(u); }

        /// <summary>🆕 2026-09-16 这个单位**有没有誓约能力**（卡面 `Oath N: …`）。
        /// 有就是「主动技能」那一格可以走誓约那条路（判据仍在引擎：`RuleCore.CanUseOathAbility`）。
        /// ⚠️ 和替代行动/`Ability:` **共用同一格按钮**（原版就只有一个主动技能按钮），
        ///    互斥优先级写在 `Resolve` 里（alt → oath → `Ability:`）。</summary>
        static bool HasOath(UnitState u)
        {
            return u != null && u.Card != null && u.Card.OathOps.Count > 0;
        }

        /// <summary>「主动技能」那一格该写什么效果文字（三种来源共用）。</summary>
        static string ActiveActionText(UnitState u, string alt, bool oath)
        {
            if (alt != null)
            {
                string body = u.Card.TriggerText(alt);
                return RuleCore.AlternativeActionName(alt) + (string.IsNullOrEmpty(body) ? "" : "：" + body);
            }
            // 誓约：正文在 `OathOps` 的第一条（`Source` 就是卡面那一句，含 `Oath N:` 前缀）
            if (oath) return u.Card.OathOps[0].Source;
            return u.Ability != null ? CardText.Effect(u.Ability) : "";
        }

        /// <summary>
        /// 这一手要点的目标在**哪一方**（玩家号；`-1` = 不用点）。
        /// 🔴 **判据只此一处** —— 点亮合法目标 / 准星 / 点击 / 结算**四处**都读它；
        /// 各写一遍的话迟早自相矛盾（「这半边亮着，点下去却没反应」）。
        /// </summary>
        int CommandTargetSide()
        {
            if (_selectedSlot < 0 || _command == AttackKind.None) return -1;
            if (_command != AttackKind.Ability) return 1 - _me;          // 近战/远程永远点敌方
            var u = Ctx.Players[_me].Board[_selectedSlot];
            if (u == null) return -1;
            string alt = AltActionOf(u);
            if (alt != null) return RuleCore.AlternativeTargetSide(Ctx, _me, _selectedSlot, alt);
            // 🆕 2026-09-16 誓约能力**不用点目标**（正文里的 `Deal 3 damage` 这种由引擎按
            // 「未写目标 = 默认一个敌方单位」处理，见 `EffectResolver` 里那几处 `(未写目标…)`）
            if (HasOath(u) && RuleCore.CanUseOathAbility(Ctx, _me, _selectedSlot) == RuleCodes.OK) return -1;
            return u.Ability != null && EffectTargets.NeedsPick(u.Ability.Target) ? 1 - _me : -1;
        }

        /// <summary>定下打法 → 收选择器 → 把合法目标点亮</summary>
        void CommitCommand(AttackKind kind)
        {
            // 🔴 🆕 2026-10-18（A938）：**教程白名单闸门 · 第 ⑨⑬ 点** —— 原版
            //   `CanShowPotentialTargets`（点亮合法目标之前那一次）与
            //   `UnitOnBoardAttackTypeSelector__AttackButtonClick`（点三选一那颗钮那一次）。
            //   我们这两件事**发生在同一个方法里**（选择器收起 → 点亮目标），
            //   ⇒ 一处 OR 进去 = 两点一起生效。⛔ 别在 `HighlightTargets` 里再写一遍。
            //   ⚠️ 到这一步玩家**已经点定了打法** ⇒ 拒了要**出声**。
            if (!TutorialPermits(TutAttemptResolve(kind, _selectedSlot, -1)))
            {
                RejectByTutorial(kind == AttackKind.Ability ? "选定「主动技能」这一档"
                                                            : "选定一种攻击打法");
                ClearSelection();
                return;
            }
            _command = kind;
            if (selector != null) selector.Hide();

            // 🔴 🆕 2026-10-18（审查 K4/R6）：**教程里「玩家选了这一档打法」就是脚本等的「换打法」那一步。**
            //   放在闸门之后、`CommandTargetSide()` 之前 —— 那一步做完指针就往下走，
            //   所以**「不换打法就直接打」不可能发生**（要打必须先选打法，而选打法就把这一步做了）。
            TutorialCompleteChangeAttack(kind);

            // 只有「主动技能」才可能有技能卡面板（`u` 非空就意味着这次是放技能）
            var u = kind == AttackKind.Ability ? Ctx.Players[_me].Board[_selectedSlot] : null;

            // 不用选目标的（治疗 / 抽牌 / 直接打督军）：选完就放，没有「选谁」这一步，
            // 也就不弹面板（一闪而过等于没有）
            // ⚠️ 判据用 `CommandTargetSide() < 0`（**两种来源共用一处**）——
            //    原来这里写的是 `u.Ability != null && !NeedsPick(u.Ability.Target)`，
            //    那个只认 `Ability:` 那一族，替代行动（`Duty:` 等）会走不进来。
            if (u != null && CommandTargetSide() < 0)
            {
                Resolve(kind, -1);
                return;
            }

            // 点亮合法目标，顺便拿到个数 —— 面板和中间那行提示**共用这一个数**
            int n = HighlightTargets();
            if (u != null && u.Ability != null) ShowSkillPanel(u, n);   // 替代行动没有 `EffectSpec`，不弹这个面板

            // 🆕 2026-09-29 照原版：**进入选目标状态就把准星点亮**（不是「指针压在合法目标上才亮」）——
            // 原版那六个 `ToggleCrosshair(true,false)` 调用点全在「开始选目标」那几支，判据见 `ShowReticleNow`。
            ShowReticleNow();
        }

        /// <summary>
        /// 弹技能卡面板（原版 `ActiveSkillDesc`）。内容全从**施放者那张卡的 `Ability`** 来 ——
        /// 面板不认识规则，判据也不在它那儿。
        /// </summary>
        void ShowSkillPanel(UnitState u, int targets)
        {
            if (skillPanel == null || u == null) return;
            // 卡名当技能名 —— 原版有独立的技能名（`NameText` = "Fire Arrow"），我们还没那个字段
            skillPanel.Show(CardText.Name(u.Name), u.Ability, targets);
        }

        /// <summary>把**合法的**目标标出来。返回个数（技能卡面板和提示行**共用这一个数**）</summary>
        int HighlightTargets()
        {
            int n = 0;
            // 这一手要点的在**哪一侧**：近战/远程永远敌方；替代行动看正文
            // （`Pray: Give Shield to a friendly unit` 点的是**自己人**）。
            int side = CommandTargetSide();
            var views = side == _me ? _myUnits : _foeUnits;
            var other = side == _me ? _foeUnits : _myUnits;

            // 先熄掉**另一侧**的底光 —— 上一次打法点亮过的会留在卡面上
            foreach (var kv in other)
                if (kv.Value != null) kv.Value.SetTargetGem(TargetGem.None);

            for (int t = 0; t < BoardSpec.Size; t++)
            {
                CardView v;
                bool have = views.TryGetValue(t, out v) && v != null;
                bool legal = LegalTargetCode(t) == RuleCodes.OK;

                // 不合法的一律**熄掉底光** —— 不然上个打法点亮的那些会留在卡面上
                if (have) v.SetTargetGem(legal ? GemForCommand() : TargetGem.None);
                if (!legal || !have) continue;

                v.SetHighlight(CardHighlightState.ValidTarget);
                n++;
            }

            string what = _command == AttackKind.Ability ? "ABILITY"
                        : (_command == AttackKind.Ranged ? "RANGED" : "MELEE");
            SetHint(CardText.Phrase(what) + " - " +
                    (n > 0 ? CardText.Phrase("PICK A TARGET") + " (" + n + ")"
                           : CardText.Phrase("NO LEGAL TARGET")));
            return n;
        }

        /// <summary>
        /// 当前打法点亮哪颗数值格的底光。**技能没有那一档** —— 原版 `Base Attack Counters` 下
        /// 只有近战/远程两个 `Highlight`（见 `CardView.SetTargetGem`）。技能靠准星和弧线的金色表达。
        /// </summary>
        TargetGem GemForCommand()
        {
            return _command == AttackKind.Ranged ? TargetGem.Ranged
                 : _command == AttackKind.Ability ? TargetGem.None
                 : TargetGem.Melee;
        }

        /// <summary>
        /// 敌方槽位 `t` 在当前打法下合不合法（返回引擎码）。
        /// **判据只有这一份** —— 点亮合法目标（`HighlightTargets`）和准星（`UpdateReticle`）都调它。
        /// 各写一遍的话迟早自相矛盾：卡亮着、准星却不认。
        /// </summary>
        int LegalTargetCode(int t)
        {
            if (Ctx == null || _selectedSlot < 0 || t < 0 || t >= BoardSpec.Size) return RuleCodes.ErrTarget;
            int side = CommandTargetSide();
            if (side < 0) return RuleCodes.ErrTarget;                       // 这一手不用点目标
            if (Ctx.Players[side].Board[t] == null) return RuleCodes.ErrTarget;
            // 🔴 🆕 2026-10-18（A938）：**教程白名单闸门 · 第 ③④⑤⑨ 点** —— 原版那五处
            //   （`IsValidSpellTarget` / `IsValidAttackTarget` / `IsValidActiveAbilityTarget` /
            //    `IsValidActiveAbilityTargetTutorial` / `CanShowPotentialTargets`）**都挂在同一族
            //   「这个目标能不能点」的判定上**，而它们在我们这边**收敛成了这一个方法**
            //   （文件名下那句注释：点亮合法目标与准星都调它 —— 判据只此一处）。
            //   ⇒ 一处 OR 进去 = 五处一起生效；⛔ 别在 `HighlightTargets` / `UpdateReticle` 里再各写一遍。
            //   ⚠️ 这一处也是**查询口**（每帧问）⇒ 不出声；被拒的提示由第 ⑪ 点（`DoResolve`）给。
            if (!TutorialPermits(TutAttemptTarget(side, t))) return RuleCodes.ErrTarget;
            if (_command != AttackKind.Ability)
                return RuleCore.IsValidTarget(Ctx, _me, _selectedSlot, side, t,
                                              _command == AttackKind.Ranged);

            // 主动技能那一格：**两种来源各问各的判据**（都在引擎里，界面不自己判）
            var u = Ctx.Players[_me].Board[_selectedSlot];
            string alt = AltActionOf(u);
            if (alt != null)
                return RuleCore.CanPickAlternativeTarget(Ctx, _me, _selectedSlot, alt, t)
                     ? RuleCodes.OK : RuleCodes.ErrTarget;
            return RuleCore.CanUseAbility(Ctx, _me, _selectedSlot, t);
        }

        /// <summary>
        /// 准星跟着指针走。指针不在**合法**目标上就收起来 ——
        /// 不显示「你正指着一个打不了的人」，那比不显示更误导。
        /// </summary>
        // 🆕 2026-09-29：指针当前压着的那个目标（原版 `CardHighlight` 的 `selectedTargetInBoard` 那一态）。
        //    指针一离开就要把它退回 `ValidTarget`、并把「会打死它」那个图标关掉。
        CardView _reticleTarget;

        void UpdateReticle(Vector3 world)
        {
            if (reticle == null) return;
            SyncReticleCameras();
            _pointerWorld = world;

            // 状态没开（没选人 / 没选打法）⇒ 收起。原版也是**状态结束才灭**
            // （`StopTracking` / `CancelActionStates` / `ResolveEndTurn`），不是「指针离开目标就灭」。
            CardView me;
            if (_selectedSlot < 0 || _command == AttackKind.None ||
                !_myUnits.TryGetValue(_selectedSlot, out me) || me == null)
            {
                reticle.Hide();
                ClearReticleTarget();
                return;
            }

            // ① 准星：**一直跟着指针走**（原版 `BattleManager.Update` → `MoveCrosshair` 每帧喂），
            //    弧线终点 = 指针射线打到**地板 / 敌兵平面**最近的那个命中点（原版 `UpdateTrail`）。
            //    🔴 2026-09-29 改：以前是「指针压在合法目标上才 `Show`，否则 `Hide`」——**与原版相反**。
            if (!reticle.Visible) reticle.Show(me.transform.position, reticle.ResolveAim(world), _command);
            else reticle.Aim(me.transform.position, world, _command);

            // ② 「指针压着哪个**合法目标**」那一档（`selectedTargetInBoard` 橙 + 「这一下会打死它」图标）
            //    **仍然是指针驱动**的 —— 它是悬停反馈，与准星亮不亮是两件事。
            int side = CommandTargetSide();
            var views = side == _me ? _myUnits : _foeUnits;
            int t = HitSlot(views, world);
            CardView foe;
            if (t < 0 || LegalTargetCode(t) != RuleCodes.OK ||
                !views.TryGetValue(t, out foe) || foe == null)
            {
                ClearReticleTarget();
                return;
            }

            SetReticleTarget(side, t, foe);
        }

        /// <summary>指针最后停在哪个世界坐标（准星跟着它走；`CommitCommand` 那一刻也要用它定落点）。</summary>
        Vector3 _pointerWorld;

        // ── 瞄准时的「抬手 / 后撤」（原版 `CardScript.Update` → `OrientToTargetingDirection`）────
        //  原版**只在 `cardState == inPlayAminingAttack(=17)` 时**每帧跑（`CardScript__Update.c:6`：
        //  `if (cardState(+0x228) == 0x11) OrientToTargetingDirection();`）。
        //  每帧 `localPosition = lerp(当前, 基准 + LeanOffset, clamp01(dt / timeToChargeAttack))`。
        //  🔴 这就是以前被我们**误当成「攻击前摇」**的那 0.35 s —— 它发生在**瞄准期间**，
        //     不在攻击序列里（攻击序列是 `CardFeel.MeleeAttack`，命中在 t=0.1）。
        bool _leanActive;
        int _leanSlot = -1;
        Vector3 _leanHome;

        /// <summary>每帧推一次（挂在 `AdvanceTimeline` 这个泵上 —— 批处理没有帧循环，见它的注释）。</summary>
        void TickTargetingLean(float dt)
        {
            bool aiming = _selectedSlot >= 0 && _command != AttackKind.None;
            if (aiming && !_leanActive)
            {
                CardView v0;
                if (!_myUnits.TryGetValue(_selectedSlot, out v0) || v0 == null) return;
                // 正在被别的补间摆着的卡不抢（落场/回手那几段还在跑）
                if (DG.Tweening.DOTween.IsTweening(v0.transform)) return;
                _leanActive = true;
                _leanSlot = _selectedSlot;
                _leanHome = v0.transform.localPosition;      // 抬手之前的静止位
            }
            if (!_leanActive) return;

            CardView v;
            if (!_myUnits.TryGetValue(_leanSlot, out v) || v == null) { _leanActive = false; return; }

            if (!aiming && DG.Tweening.DOTween.IsTweening(v.transform))
            {
                // 攻击/挨打那几条序列已经接管了这个 transform（段4 自己会回位）⇒ 让开，别抢
                _leanActive = false;
                return;
            }

            Vector3 to = aiming ? _leanHome + CardFeel.LeanOffset(true) : _leanHome;
            float t = Mathf.Clamp01(dt / CardFeel.ChargeTime);
            var now = Vector3.Lerp(v.transform.localPosition, to, t);
            if (!aiming && (now - _leanHome).sqrMagnitude < 1e-8f) { now = _leanHome; _leanActive = false; }
            v.transform.localPosition = now;
        }

        /// <summary>
        /// **进入选目标状态就点亮准星**（原版六个 `ToggleCrosshair(true, false)` 调用点全在
        /// `BattleManager` 的「开始选目标」那几支：`StartAttackFrom` / `StartSpellFrom` / `StartTrackingFrom` /
        /// `StartTargetedActiveAbility` / `StartChoosingAbilityTargetByClick` / `EnemyTargetingAnim`）。
        /// 🔴 我们原来要等指针压到合法目标才亮 —— **与原版相反**，2026-09-29 照原版改
        /// （判据全文 → `资料/待办判据_战场与战斗视图.md` Q7 那张表第 6 条）。
        /// </summary>
        void ShowReticleNow()
        {
            if (reticle == null || _selectedSlot < 0 || _command == AttackKind.None) return;
            SyncReticleCameras();
            CardView me;
            if (!_myUnits.TryGetValue(_selectedSlot, out me) || me == null) return;
            reticle.Show(me.transform.position, reticle.ResolveAim(_pointerWorld), _command);
        }

        /// <summary>把准星要的两台相机同步过去。**每次用之前同步一次** ——
        /// `boardCam` 是外部直接赋值的公开字段（没有 setter），只在 `SetCamera` 里同步会漏掉
        /// 「先设相机、后建 3D 战场」那个顺序。</summary>
        void SyncReticleCameras()
        {
            if (reticle == null) return;
            reticle.hudCam = cam;
            reticle.boardCam = boardCam != null ? boardCam : cam;
        }

        /// <summary>指针压着的那个目标 —— 原版 `selectedTargetInBoard`（橙 `#FF8400` ×1.05）
        /// **外加**「这一下会打死它」那个图标（原版 `CardHighlight.minionWillDieObject`）。
        ///
        /// 🔴 **两条判据都不在这里重写**：能不能打 = `LegalTargetCode`（`UpdateReticle` 进来之前已判过）；
        ///   打不打得死 = `RuleCore.WouldKillByEntries` —— 伤害条目由引擎那两份聚合口给
        ///   （攻击路 `RuleCore.AttackDamageEntries` · 技能路 `RuleCore.ActiveAbilityDamageEntries`）。
        /// 🆕 **2026-10-08（`W8b3`）：主动技能那一路【接上了】，而且判死改成原版的【逐条】口径。**
        ///   改之前两条与原版不符的，都已改掉：
        ///   ① 🔴 **技能路整条被排除**（旧判据 `_command != AttackKind.Ability`）⇒ 技能路那层
        ///      **永远不亮**。原版是**算的**：`CardHighlight__ToggleCombatPreviewHighlight.c:86-92`
        ///      的技能支走 `EntityScript.GetActiveAbilityDamage`，结果同样喂给判死那个函数。
        ///   ② 🔴 **判死把「已求和的那一个数」当成一条打**（旧判据 `RuleCore.WouldKill` →
        ///      `DamageAfterReduction`）。原版判死是**逐条**跑的
        ///      （`CardScript__EnoughPendingDamageToDieWithDamageValues.c:115-156`）：
        ///      `dodge`/`shield` 只跳过**第 0 条**、`invulnerable` 跳过任意条、
        ///      **护甲每条各扣一次**。
        ///      ⇒ 逐条口径的全文与出处 → `RuleCore.DamageAfterReductionOne` 上面那一段。
        ///
        /// 🔴🔴 **2026-10-09（`A1107`，用户拍板）：条目次序与拆分都改成照【真实结算】那一套。**
        ///   原版自己有三套写法、且互不相同，而 `dodge`/`Shield` 只挡**第 0 条** ⇒
        ///   「预览/登记」两套对「目标带盾/闪避 **且** 攻方带星镖」给出**相反**的「会不会死」：
        ///     · **预览** `CardHighlight__ToggleCombatPreviewHighlight.c:62-81` = `[主伤害, 星镖, 标记光]`（**改前我们照的**）；
        ///     · **登记** `BattleManager__RecordAttackPendingDamage.c:59-84` = `[星镖, 标记光, 主伤害]`（`pendingDamage` 台账，只喂判死）；
        ///     · 🔴 **真实结算** `BattleManager._ResolveAttack_d__438__MoveNext.c` = **星镖 `:618` → 主伤害 `:1116` → 标记光 `:1251`**
        ///       （用户原话「**星镖—主攻击—标记光/易伤**」）。
        ///   ⇒ 现在照**结算**：`RuleCore.AttackDamageEntries` = **`[星镖, 主伤害, 标记光]`** —— 三段**各自独立**
        ///   （标记光**不并进**主伤害：结算那边它是自己一次 `ReceiveDamage`，护甲对它**另扣一次**）。
        ///   **预测的职责是与实际一致**，照抄原版预览（或那张登记台账）的次序 = 照抄一个预测不准的算法。
        ///   🔴 **这是对原版预览代码的【故意偏离】**（铁律 11 例外②），出处 = 用户裁决，
        ///   判据全文 → `资料/普查产出_第六会话/W_dodge整条.md` §⑥3；⛔ 别「改回原版」。
        ///
        /// ⚠️ **合计版仍是 `ApplyDamage` 读的那一份，没动** —— 它是「**单次 `Hurt` 一个数**」的口径，
        ///    攻击落地则是**三次** `Hurt`（星镖 / 主伤害 / 标记光各一次）⇒ 两者形状本来就不同，
        ///    **别拿合计版当「落地序列」的基准**（那是错的基准）。
        ///    ⚠️ 仍**跟着我们自己的实现走**的一处：标记光**只在远程**计入（原版 `:1252` 那个 `== 2` 同）——
        ///    否则会多报一条**不会发生**的伤害。</summary>
        void SetReticleTarget(int side, int slot, CardView view)
        {
            if (_reticleTarget != null && _reticleTarget != view)
            {
                _reticleTarget.SetHighlight(CardHighlightState.ValidTarget);   // 上一个退回「只是合法目标」
                _reticleTarget.SetWillDie(false);
            }
            _reticleTarget = view;
            view.SetHighlight(CardHighlightState.SelectedTargetInBoard);

            bool willDie = false;
            if (Ctx != null && _selectedSlot >= 0 && _command != AttackKind.None)
            {
                var u = Ctx.Players[side].Board[slot];
                var atk = Ctx.Players[_me].Board[_selectedSlot];
                if (u != null && atk != null) willDie = RuleCore.WouldKillByEntries(u, DamageEntriesFor(side, slot));
            }
            view.SetWillDie(willDie);
        }

        /// <summary>这一手打在 `(side, slot)` 那个单位上的**全部伤害条目**（原版那张 `List&lt;int&gt;`）。
        /// 技能路与攻击路各一个聚合口，两份判据的全文在 `RuleCore` 那两个函数上面
        /// （`ActiveAbilityDamageEntries` / `AttackDamageEntries`）。</summary>
        List<int> DamageEntriesFor(int side, int slot)
        {
            if (_command == AttackKind.Ability)
                return RuleCore.ActiveAbilityDamageEntries(Ctx, _me, _selectedSlot, side, slot);
            return RuleCore.AttackDamageEntries(Ctx, _me, _selectedSlot, side, slot,
                                                _command == AttackKind.Ranged);
        }

        /// <summary>指针离开目标：两样一起收（退回 `ValidTarget` · 关掉图标）。</summary>
        void ClearReticleTarget()
        {
            if (_reticleTarget == null) return;
            _reticleTarget.SetHighlight(CardHighlightState.ValidTarget);
            _reticleTarget.SetWillDie(false);
            _reticleTarget = null;
        }

        /// <summary>自检用：准星现在压着的那个目标（没有 = null）。</summary>
        public CardView ReticleTargetView { get { return _reticleTarget; } }

        /// <summary>打出去（攻击或放技能）。`targetSlot` &lt; 0 = 不需要选目标的技能。返回引擎码。
        ///
        /// 🆕 2026-10-17（A904）：**主动技能那一格的 ask 点在这里才问**（不再在出牌时预问）——
        /// 判据 = 原版 `_ResolvePlayActiveAbility_d__479__MoveNext.c:367`
        /// （`ChooseCardMethod` 的 4 个调用点之一，紧跟着 `StartCoroutine` + `return 1` 挂起）。
        /// 所以本方法现在可能**只是把这一手收下、把面板开起来**（返回 `OK`），
        /// 真正的引擎调用由 `BeginAsk` 的续跑在面板关掉之后调 `DoResolve`。</summary>
        int Resolve(AttackKind kind, int targetSlot)
        {
            int slot = _selectedSlot;

            if (kind == AttackKind.Ability && Ctx != null && BoardSpec.IsValid(slot))
            {
                // 这一格技能有**三种来源**（替代行动 → 誓约 → `Ability:`，优先级见 `DoResolve`）——
                // 分档要**与它挑的是同一条**，否则会去问另一条正文里的 ask 点。
                var u = Ctx.Players[_me].Board[slot];
                string alt = AltActionOf(u);
                bool oath = alt == null && HasOath(u)
                            && RuleCore.CanUseOathAbility(Ctx, _me, slot) == RuleCodes.OK;
                var asks = AbilityAsks(u, alt, oath);
                if (asks.Count > 0)
                {
                    int sl = slot, ts = targetSlot;
                    // `PendingCard` 要认得出「正在结算的是哪张卡」（面板取插图 / 查效果池 / 拼标题词条）
                    _pendingIdx = -1;
                    _pendingInst = u != null ? u.Instance : null;
                    // ⚠️ 第 3 个实参 = **不清计数**（理由见 `BeginChoiceBatch`：录像对账哈希认这两个数）
                    BeginAsk(asks, () => DoResolve(AttackKind.Ability, ts, sl), false);
                    return RuleCodes.OK;      // 这一手已经收下（引擎调用在面板关掉之后）
                }
            }

            return DoResolve(kind, targetSlot, slot);
        }

        /// <summary>真的把这一手打出去（面板问完之后由 `NextAsk` 调；不用问时 `Resolve` 直接调）。
        /// `slot` **由调用方带进来**，不读 `_selectedSlot` —— 面板开着的那段时间里选择可能已经被清掉。</summary>
        int DoResolve(AttackKind kind, int targetSlot, int slot)
        {
            // 🔴 🆕 2026-10-18（A938）：**教程白名单闸门 · 第 ⑪ 点** —— 原版 `AllowResolveAttack`
            //    （`BattleManager__AllowResolveAttack.c` 里那一次 `CheckIfPlayerActionPermittedInTutorial`）。
            //    走到这儿说明玩家已经点定了打法与目标 ⇒ 拒了要**出声**。
            if (!TutorialPermits(TutAttemptResolve(kind, slot, targetSlot)))
            {
                RejectByTutorial(kind == AttackKind.Ability ? "放主动技能" : "发起攻击");
                ClearSelection();
                return RuleCodes.ErrUnimplemented;
            }
            int code;
            // 🆕 2026-09-26（N4）：联机局要把**这一手是什么**发对面 ⇒ 先攒成一条 `AiAction`，
            //    再走 `LocalAct` 落地（单机下 `LocalAct` 只是直接调那个 lambda，行为一字不差）。
            var act = new AiAction
            {
                Kind = kind == AttackKind.Ability ? AiActionKind.ActiveAbility
                     : (kind == AttackKind.Ranged ? AiActionKind.AttackRanged : AiActionKind.AttackMelee),
                Slot = slot,
                TargetP = 1 - _me,                 // 本机视角：目标永远在对面那一侧
                TargetSlot = targetSlot,
                Ranged = kind == AttackKind.Ranged,
            };
            if (kind == AttackKind.Ability)
            {
                // 主动技能那一格有**三种来源**（见 `OpenCommand`）：替代行动 → 誓约 → `Ability:`
                var u = Ctx.Players[_me].Board[slot];
                string alt = AltActionOf(u);
                if (alt != null) { act.AltKeyword = alt; act.TargetSlot = targetSlot; }
                else if (HasOath(u) && RuleCore.CanUseOathAbility(Ctx, _me, slot) == RuleCodes.OK)
                    act.AltKeyword = "oath";
                code = LocalAct(act, () =>
                {
                    if (alt != null) return RuleCore.UseAlternative(Ctx, _me, slot, alt, targetSlot);
                    if (act.AltKeyword == "oath") return RuleCore.UseOathAbility(Ctx, _me, slot);
                    return RuleCore.UseAbility(Ctx, _me, slot, targetSlot);
                });
            }
            else code = LocalAct(act, () =>
                RuleCore.DeclareAttack(Ctx, _me, slot, 1 - _me, targetSlot, kind == AttackKind.Ranged));

            if (code != RuleCodes.OK) Debug.Log($"[Battle] 这一手打不出去：{HintForCode(code)}");

            // 技能**正在结算** → 面板铺白那层（原版 `ShowActingLight()`），
            // 紧接着 `ClearSelection` 会带着这层白淡出，不是「啪」地消失
            if (kind == AttackKind.Ability && skillPanel != null) skillPanel.SetActing();

            ClearSelection();
            RefreshAll();
            ContinueTutorialScript();       // 🆕 A938：玩家做完那一步 ⇒ 脚本指针 +1
            AutoEndTurnIfStuck();
            return code;
        }

        /// <summary>
        /// 写提示行。**传空 = 回到「休息态」那句**（`_deckNotice`：本局用的是哪副牌 / 为什么退了）。
        ///
        /// 为什么要有这个中转：开局那句必须**一直在**，不然玩家一悬停一选目标就被抹掉了 ——
        /// 而「你这副牌有 N 张没上场」正是要他看见的事（红线：不许静默失败）。
        /// 所以把它做成提示行的**默认文字**，游戏过程中的临时提示盖在它上面、用完自动落回来。
        ///
        /// ⚠️ **原版没有这一行**（原版全卡种都能上场，没什么要交代的）—— 这是我们加的。
        /// ⚠️ 唯一要**真的清空**的地方是结算（那时要收干净，不然会从结算面板底下透出来），
        ///    那里直接调 `_hintLabel.SetText("")`。
        /// </summary>
        void SetHint(string s)
        {
            if (_hintLabel == null) return;
            _hintLabel.SetText(string.IsNullOrEmpty(s) ? _deckNotice : s);
        }

        /// <summary>
        /// 🆕 **2026-10-18（`A985⑧` · 第一步）**：**引擎码 → 给玩家看的那一句**。
        ///
        /// 规则（判据 → <see cref="RuleCodes.TermKey"/> 的 doc + `资料/普查产出_1018/G5_战斗本地化剩余.md` §4·B）：
        ///   · `RuleCodes.TermKey(rc)` **非 null** **且** `Loc.HasEntry(那个键)` 为真 ⇒ 走**原版词条**（`Loc.T(键)`）；
        ///   · 其余（**没有键** 或 **键不在词条表里**）⇒ `RuleCodes.Describe(rc)`（我们那 18 条中文整句）。
        ///
        /// 🔴 **兜底那一半必须留**：`Battle/Tips/*` 那族原版键**本地一个 value 都没有**
        ///   （I2 词条表在远端 CCD）；而 **`TermKey` 今天映射了四条**（`ErrCost` / `ErrNotTurn` /
        ///   `ErrNoTargetAvailable` / `ErrNotEnoughRoom`，其余一律 `null`）。
        ///   ✅ **2026-10-09（`A1018①`）就地订正**：这四条键原来这里写「**一条都不在 `Loc` 表里**」——
        ///   那是**加键之前**的事实，**今天已经在表里**（`Core/Loc.cs` 那一节 `Battle/Tips/NotEnough*`
        ///   那四条：`NotEnoughMana` / `NotYourTurn` / `NoTargetAvailable` / `NotEnoughRoom`）
        ///   ⇒ 这四条码**走的是词条值**、不是 `Describe`。⛔ 但 `Describe` 那半边**照样不许删** ——
        ///   「无键码（如 `ErrSlot`）/ 键不在表里」那一档仍然只能靠它兜。
        ///
        /// 🔴 **2026-10-18（第三会话）修掉的真缺陷**：这一句原来是
        ///   <c>key != null ? Loc.T(key) : RuleCodes.Describe(rc)</c> —— **只判「有没有键」、没判
        ///   「表里有没有这条」**，而 `Loc.T` **缺键时返回【键名本身】**（见 `Loc.T` 的 doc）
        ///   ⇒ 上面那四条键让提示行**印出 `Battle/Tips/NotEnoughRoom` 这种字符串**（比「未知错误码 19」
        ///   更难懂）—— 而且**四条全都印键名**、一条人话都没有（`ErrCost` 付不起时也一样）。
        ///   ⇒ 现在多一道 `Loc.HasEntry(键)`：**键在表里才取词条**，否则回人话。
        ///
        /// ⛔ **不许写成 <c>Loc.T(RuleCodes.TermKey(rc)) ?? RuleCodes.Describe(rc)</c>** ——
        ///   那是 `RuleCodes.cs` 头注里导出的「该长成的样子」，**它有个洞**：`Loc.T` **从不返回 null**
        ///   （空键返回 `""`、缺键返回**键名本身**）⇒ 无键那些码会得到 `""`
        ///   （提示行**全空**，正是本工程最忌讳的静默失败），而 `??` 那一半**永远不会触发**。
        ///   ⇒ 所以这里**先判键、再判表**（`Editor/BattleScene.cs` 的 §3b 有一组断言钉住这两态）。
        ///
        /// ⚠️ **调用点只有「玩家动作被引擎拒」那三处**（`DoPlay` / 收集灵魂石 / `DoResolve`）。
        ///   ⛔ `RuleCodes.Describe` 其余调用点**一个都没动** —— AI / 网络层 / 教程层 / 自检要的是
        ///   **人话诊断**（`grep` 全仓 20+ 处），那些地方把它换成键名是**退化**。
        ///
        /// ⚠️ **与同族 `HandFullText` 的写法现在一致了（原来不一致，如实标过）**：两条都走
        ///   `if (Loc.HasEntry(键)) … else 兜底`。差别只剩**兜底那一句的来源** ——
        ///   本条 = `RuleCodes.Describe` 的通用整句；那条 = 手写的那句「手牌已满…」。两条都对。
        /// </summary>
        public static string HintForCode(int rc)
        {
            return HintForCodeWithKey(rc, RuleCodes.TermKey(rc));
        }

        /// <summary>
        /// 🆕 **2026-10-18（第三会话）**：**两态判据本身**，键由调用方给（`HintForCode` 只是把
        /// `RuleCodes.TermKey(rc)` 喂进来）。
        ///
        /// <para>🔴 **为什么要开这个口（⛔ 不是为了好看）**：`Terms` 那四条键**原来一条都不在 `Loc` 表里**
        /// ⇒ **「有键且表里有值」这一态从单参那条路根本构造不出来**，而 `Loc` 那张表是 `Core/Loc.cs`
        /// 的私有字段、**没有任何「按测试插一条」的口**（公开查询口只有 `HasEntry` / `EnOf`，写入口一个都没有）。
        /// ✅ **2026-10-09（`A1018①`）就地订正**：那四条键**今天已在 `Loc` 表**（`Core/Loc.cs` 那一节
        /// `Battle/Tips/NotEnough*` 那四条，见 `HintForCode` 的 doc）⇒ 上面那句只剩历史意义，
        /// 但这个口**照旧留着**：它让自检能**指名**喂一个确实在表里的键（现用 `BattleDriver.HandFullTerm`）
        /// 去钉「**键在表里 ⇒ 印词条值、⛔ 不是键名**」那一态。</para>
        /// <para>⛔ **产品代码一律走单参那条**（`Editor/BattleScene.cs` §3b 有断言钉着两态；
        /// 那个口只被自检调用）。</para>
        /// </summary>
        public static string HintForCodeWithKey(int rc, string key)
        {
            // 🔴 缺键、或键不在词条表里 ⇒ **回 `Describe` 的人话**（⛔ 不是印键名、⛔ 不是印空串）。
            //    ⛔ 别改回 `Loc.T(key) ?? Describe(rc)`：`Loc.T` **从不返回 null**（缺键回键名本身），
            //      `??` 那一半永不触发 ⇒ 界面会印 `Battle/Tips/NotEnoughRoom`（2026-10-18 修掉的那个退化）。
            return (key != null && Loc.HasEntry(key)) ? Loc.T(key) : RuleCodes.Describe(rc);
        }

        void ClearSelection()
        {
            if (_selectedSlot >= 0)
            {
                CardView v;
                if (_myUnits.TryGetValue(_selectedSlot, out v) && v != null)
                    v.SetHighlight(CardHighlightState.Normal);
            }
            _selectedSlot = -1;
            _command = AttackKind.None;
            _pressSlot = -1; _pressSide = -1;
            if (selector != null) selector.Hide();
            if (reticle != null) reticle.Hide();
            // 🆕 2026-09-29：指针压着的那一档（`selectedTargetInBoard` + 「会打死它」图标）也跟着收
            //    —— 下面那个 foreach 会把所有敌方单位刷回 `Normal`，这里先把图标关掉、把引用清空。
            ClearReticleTarget();
            if (skillPanel != null) skillPanel.Hide();
            foreach (var kv in _foeUnits) if (kv.Value != null)
            {
                kv.Value.SetHighlight(CardHighlightState.Normal);
                kv.Value.SetTargetGem(TargetGem.None);    // 底光也要熄
            }
            if (_hintLabel != null) SetHint("");          // 落回「休息态」那句（开局那副牌）
        }

        void EndPlayerTurn()
        {
            // 🔴 🆕 2026-10-18（A938）：**教程白名单闸门 · 第 ⑩ 点** —— 原版 `EndTurnClick`
            //    （`ClockManager__EndTurnClick.c` 里那一次 `CheckIfPlayerActionPermittedInTutorial`）。
            //    放这儿 = 两个调用点（结束回合按钮 / `AutoEndTurnIfStuck` 那条自动收尾）**一次盖住**；
            //    后者在脚本没跑完时自己已经早退了（见那边），所以这一句真正拦的就是**玩家点的那一下**。
            if (!TutorialPermits(TutAttemptEndTurn())) { RejectByTutorial("结束回合"); return; }
            ClearSelection();
            // 🆕 2026-09-26（N4）：联机局要把「我结束回合」发对面（对面收到后照样 `EndTurn` + `BeginTurn`）
            var endAct = new AiAction { Kind = AiActionKind.EndTurn };
            if (_net != null) _net.CaptureLocalAnswers(Ctx, endAct);
            // 🆕 2026-09-27（录像）：玩家结束回合**同样不走 `LocalAct`**（上面那两个直调）⇒ 单独记一条
            int[] endPicks = (_rec != null && Ctx.ChoosePicks.Count > 0) ? Ctx.ChoosePicks.ToArray() : null;
            string[] endPickIds = (_rec != null && Ctx.ChooseCardIds.Count > 0) ? Ctx.ChooseCardIds.ToArray() : null;
            RuleCore.EndTurn(Ctx);
            // ⚠️ 换边之后**必须再 BeginTurn** —— 它才是「给当前行动方发能量、抽牌、解疲劳」的那一步。
            //    少了这一步，对手整个回合都是 0 能量，一张牌都出不来（踩过：AI 场上永远只有督军）。
            RuleCore.BeginTurn(Ctx);
            SyncTutorialTurn();           // 🆕 A938：回合变了 ⇒ 脚本指针归零（原版 `UpdateTurn`）
            _aiTimer = aiStepDelay;
            _aiSteps = 0;                 // 对手的新回合 → 步数清零
            _aiRejected.Clear();          // 同上：排除名单也只在本回合内有效
            // 🆕 2026-10-17（A904）：回合推进也会结算到 ask 点 ⇒ 清账（同 `EndTurnAndAdvance`）
            ReportUnaskedChoices();
            RefreshAll();
            if (_net != null) _net.OnLocalAction(endAct);
            RecAct(endAct, _me, endPicks, endPickIds);      // 🆕 录像：玩家这条结束回合
            ContinueTutorialScript();     // 🆕 A938：玩家做完那一步（= 结束回合）⇒ 脚本指针 +1
            NetAfterTurnStart();          // 🆕 每回合开始对一次状态指纹（联机才有）
        }

        /// <summary>🆕 2026-10-18（A938）：**第 ⑭ 点**（原版 `BattleManager.Update` 那两条逐帧判定，
        /// 见 `DrivePlayerTurn` 里的调用点）。把「已经开着的指挥状态」按闸门收掉 ——
        /// 脚本换了一条动作之后，上一个「选人 → 选打法」的状态就不该继续挂着。
        /// ⚠️ 非教程局 / 本回合没脚本 / 脚本跑完了 ⇒ **恒空转**（与加这一句之前逐字等价）。</summary>
        void TutorialFrameGuard()
        {
            var tut = Ctx != null ? Ctx.Tutorial : null;
            if (tut == null || tut.CurrentTurn == null) return;
            if (tut.FinishedScriptedActionsInTurn) return;
            if (_selectedSlot < 0 || _command == AttackKind.None) return;
            if (!TutorialPermits(TutAttemptResolve(_command, _selectedSlot, -1))) ClearSelection();
        }

        /// <summary>
        /// 🔴 🆕 2026-10-18（审查 K4/R6）：**原版那两半，在我们这边合成一处。**
        ///
        /// 原版 `BattleManager__Update.c:1055-1085`：`cVar13 = CheckIfWaitingToChangeAttack(...)`；
        /// **非零就把这一刀拦下来**（跳过 `AddAttackAction`）—— 那是第一半；
        /// 第二半是「这一步玩家怎么做出来」：`UnitOnBoardAttackTypeSelector__AttackButtonClick.c`
        /// → `CardScript.ChangeAttackType(card, …)` → `FinishResolvingAction`（⇒ 指针 +1）。
        ///
        /// 我们这边：**玩家选打法那一刻 = `CommitCommand`**，它同时就是原版那颗按钮那一下
        /// ⇒ 在这里**做那一步**（写卡上持久的 `CurrentAttackType` + 推指针）。
        /// 于是「脚本等着换打法时玩家直接把这一刀打出去」**不可能发生** ——
        /// 要打必须先选打法，而选打法就把这一步做了（**与原版那句闸门等效**，只是拦在更早的一层）。
        ///
        /// ⚠️ **原版口径**：`ScriptedActionData.CheckIfMatchesActionData` 对 `ChangeTo*` **同时接受**
        ///    `changeAttack(19)` 与 `attack(1)`（`case 5/6`）⇒ 我们**不按档位区分近战/远程**，选了就算（照原版）。
        /// ⚠️ **已知边界（数据里不出现）**：若 `ChangeTo*` 后面紧跟一条**不用点目标**的攻击，
        ///    同一次手势会推两次指针（`DoResolve` 末尾那次也算一次）—— 6 关数据里 `ChangeTo*` 后面
        ///    跟的是 `SmallTip` / 需要点目标的 `Attack`，撞不上。
        /// </summary>
        void TutorialCompleteChangeAttack(AttackKind kind)
        {
            var tut = Ctx != null ? Ctx.Tutorial : null;
            if (tut == null || _replaySession) return;       // 非教程局 / 回放局：一个字都不做
            if (_selectedSlot < 0 || kind == AttackKind.Ability) return;
            var cur = tut.CurrentAction;
            if (cur == null || !cur.playerAction) return;    // 只对「等玩家做」的那一条生效
            if (cur.Kind != ScriptedActionType.ChangeToRanged
                && cur.Kind != ScriptedActionType.ChangeToMelee) return;

            int type = kind == AttackKind.Ranged ? UnitState.AttackTypeRanged : UnitState.AttackTypeMelee;
            bool ok = RuleCore.SetCurrentAttackType(Ctx, _me, _selectedSlot, type);
            if (!ok)
            {
                Debug.LogWarning("[Tutorial] 脚本等着「换打法」，可这一格上没有单位 ⇒ 这一步没落地"
                                + "（`SetCurrentAttackType` 返回 false）—— 如实出声，不静默。");
                return;
            }
            Ctx.Log($"[Tutorial] 玩家选了「{(kind == AttackKind.Ranged ? "远程" : "近战")}」⇒ 就是脚本等的"
                  + "「换打法」那一步（原版 `UnitOnBoardAttackTypeSelector__AttackButtonClick`"
                  + " → `CardScript.ChangeAttackType` → `FinishResolvingAction`）");
            ContinueTutorialScript();
        }

        // ==================================================================
        //  🆕 2026-10-18（A940）：**教程表现层**（原版 `battlearena1` 那棵 `Tutorial` 子树）
        // ==================================================================
        //  判据表 = `资料/普查产出_1018/R2_教程族现核.md` **§4「层 × 场景 × 出现条件」**（每格带 `文件:行号`）；
        //  载体 = `Battle/TutorialOverlay.cs`（**一件装六层**，与原版那个 `Tutorial` 根同构）。
        //
        //  ⚠️ 三条如实标（⛔ 别当成「原版就是这样」）：
        //   ① **原版那一整棵子树 13 个战场里都有、非教程局是 `inactive`**（R2 §4 表头）；
        //      我们是**按需建**（只有教程局才建，`EnsureTutorialOverlay`）—— 不是同一件事。
        //   ② 原版那些演出**节拍**靠协程 `WaitForSeconds`；本工程**引擎不持帧**（批处理更没有帧循环）
        //      ⇒ 全部走**显式步进**（`TickTutorialView(dt)`，由 `Update` 与自检喂）。
        //   ③ 定位链 `AiScripted.GetTipPosition(positionReference, positionRelation, referenceUnit)`
        //      我们只还原了**枚举那一段**（锚到哪个 HUD 元素 + 左/右/上）—— **像素偏移是我们挑的**。
        TutorialOverlay _tutOverlay;
        float _tutWarlordBeat;                       // 督军两拍：第一拍已过、等第二拍
        bool _tutBeatDone;                           // 两拍跑完了
        int _tutViewTurn = -1, _tutViewCounter = -1; // 已经做过表现的那一条（换条才重做）
        /// <summary>督军两拍的第二拍延迟（秒）—— 🔴 **我们挑的**：原版那两条 `WaitForSeconds`
        /// 的**实参没落进 `.c`**（`BattleManager._TutorialStartSequence_d__592__MoveNext.c:45` 只有
        /// `UnityEngine_WaitForSeconds___ctor(uVar4)`，值在寄存器/栈上没被还原）⇒ ⛔ 这个数不是判据。</summary>
        const float TutWarlordBeatSeconds = 0.6f;

        /// <summary>教程表现层（**只读口**，自检用）。没建 / 非教程局 ⇒ null。</summary>
        public TutorialOverlay TutorialView { get { return _tutOverlay; } }

        /// <summary>只给教程局建一次（建在 `hudRoot` 下，与原版那棵树同父级）。
        /// ⚠️ `BuildHud` 有 `_hudBuilt` 闩 ⇒ 不能挂在那儿（教程局可能不是第一局）；这里自己判重。</summary>
        TutorialOverlay EnsureTutorialOverlay()
        {
            if (Ctx == null || Ctx.Tutorial == null) return null;
            if (_tutOverlay != null) return _tutOverlay;
            if (hudRoot == null)
            {
                Debug.LogWarning("[Tutorial] 教程表现层建不出来：`hudRoot` 是 null（HUD 还没建）——"
                    + " 这一局的提示/高亮/光标/标注/跳过钮**都不会出现**（⛔ 不静默）。");
                return null;
            }
            _tutOverlay = TutorialOverlay.Create(hudRoot);
            // 🔴 **2026-10-18：这里原来有一句 `_tutOverlay.SkipPanel = _settingsPanel;`（A991 收口时加的），已删。**
            //   跳过钮已于 A991 搬进 `Battle/SettingsPanel.cs`（原版那颗的父链就是
            //   `BattleSettingsPanel/Bottom buttons`），而 `TutorialOverlay` 上留下的那几个**纯转发口**
            //   （`SkipPanel` / `SkipVisible` / `SkipWorldPos` / `ClickSkipAt`，零像素）**全仓已零代码调用**
            //   （2026-10-18 grep：只剩 `TutorialOverlay.cs` 自己那几行「【已删】」留痕，
            //    与 `Editor/BattleScene.cs` 里两条「原来如此」的订正注释 —— 都是【已删】的记载，不是活代码）
            //   ⇒ 这一格**不再注入**（钮的真身 `_settingsPanel` 由本文件自己持有，不需要转一道手）。
            //   ✅ **2026-10-18（第三会话）订正（铁律 5）**：本行原来写「那四个口本身**还在** `TutorialOverlay.cs` 里
            //      ⇒ 要删干净得另派一件」—— **那四个口已经删掉了**（`Battle/TutorialOverlay.cs` 里只剩下
            //      「【已删】」的留痕：`:116` / `:131` / `:214` / `:221` / `:346-347` / `:519`）。
            //      ⇒ 本行过时，就地改掉。（同族过时注释：`Editor/BattleScene.cs` §⑨ 那条原写「那几个口只是**转发**」。）
            Debug.Log("[Tutorial] 表现层挂到 `HudRoot` 下（原版 = `battlearena1` 场景里的 `Tutorial` 根，13 个战场同构）");
            return _tutOverlay;
        }

        /// <summary>
        /// 🆕 2026-10-18（`Z5`）：**原版小提示那个横向偏移量**（`AiScripted.GetTipPosition` 里的 `fVar9`）。
        ///
        /// 🔴 **就地订正（铁律 5）**：`TutorialOverlay` 头部原来写着「`GetTipPosition` / `GetHorizontalOffset`
        ///   的方法体拿不到（`.c` 里只有签名）」—— **两句都不成立**，两份方法体**都在**
        ///   （`d:/2/tools/decomp_full/AiScripted__GetTipPosition.c` · `…__GetHorizontalOffset.c`，逐行读过）。
        ///   错因：当时大概是查了 `Warpforge_code/` 那份**方法体为空的签名桩**。
        ///
        /// **读出来的规矩（→ 就照这个实现）**：
        ///   · `fVar9` 初值 = `AlternateArtCard.CardImage`（`+0x50`，`AssetReferenceTyped&lt;Sprite&gt;`）的
        ///     **`+0x34`**；
        ///   · `positionReference ∈ {11,12,13} ∪ {21,22,23}`（督军的
        ///     `Melee/Range/Health` 那三档）⇒ 改用 **`+0x38`**；
        ///   · 若某个全局开关为真 ⇒ **再乘 `1.3`**（`DAT_1834b33f0`，`GameAssembly.dll` 常量池实读）。
        ///     🔴 **2026-10-18（`G9`）：那个开关【定位到了】= `GameStaticData.smallScreenUI`**
        ///     （详见 `TutSmallScreenFactor` 的注释）；我们已有同构的一格
        ///     （`CardPresentation.SmallScreenUI.Enabled`）⇒ **按原版乘上去**（原来记的是「没定位 ⇒ 不乘」）。
        ///   · 收尾三行：`LeftOf(10) ⇒ x −= fVar9` · `RightOf(20) ⇒ x += fVar9` ·
        ///     `Above(30) ⇒ y += fVar9 × **0.5**`（`DAT_1834b2bb4` 实读 = 0.5）。
        ///  ⚠️ **我们落地时把 `+0x34`/`+0x38` 读成「那张卡的宽 / 高」**（`CardView.Width` / `CardView.Height`）——
        ///    那两个字段是在 `AssetReferenceTyped&lt;Sprite&gt;` 上、我们**没有**同构的载体
        ///    ⇒ 「宽/高」是**按用途推断**的（它俩的唯一用途就是长度），⛔ 不算硬判据。
        ///    🔁 **2026-10-18（`G9`）再查一轮：仍然定不死，但把范围收窄了两条**（如实记）：
        ///      ① 那两个字段**是相邻的 4 字节浮点**（`+0x34` / `+0x38`，即一个 `Vector2` 的 x/y）
        ///         —— 这一点坐实了「它是一对尺寸」的形状；
        ///      ② 🔴 **但同一个标量同时驱动【两个轴】**：`LeftOf/RightOf` 用它做**横向**偏移、
        ///         `Above` 用它 ×0.5 做**纵向**偏移，**取的是同一个 `fVar9`**
        ///         ⇒ 「x←宽、y←高」那种逐轴配对**不成立**，真相是「**按锚点档二选一**」：
        ///         督军那三档（11/12/13/21/22/23）取 `+0x38`、其余取 `+0x34`。
        ///      ③ ⛔ **别再拿反编译里那个符号名当载体**：Ghidra 把它标成
        ///         `AlternateArtCard__get_CardImage`，而 `dump.cs` 的 `AlternateArtCard.get_CardImage`
        ///         返回 `AssetReferenceTyped&lt;Sprite&gt;`（`+0x34` 在 `PlayerItem` 上是 `autoAssign` 那个
        ///         **bool**、`+0x38` 是 `uniqueId` 那个**字符串指针**）⇒ **符号与内容不符**（坑表 #17）。
        ///         真凶 = 那个 VA(`0x180508E50`) **被 10+ 个方法共用**（`dump.cs` 里逐个可见），
        ///         它是个共享的「读某个字段就返回」的短体 ⇒ **符号名不可信**，⛔ 别照着它继续推。
        ///      ⇒ 结论：**「宽/高」仍是按用途的推断，只是现在知道它是「一对尺寸、按锚点档二选一」**。
        /// </summary>
        static float TutorialTipOffset(int positionReference)
        {
            bool sub = (positionReference >= 11 && positionReference <= 13)
                    || (positionReference >= 21 && positionReference <= 23);
            float v = sub ? CardView.Height : CardView.Width;
            if (SmallScreenUI.Enabled) v *= TutSmallScreenFactor;
            return v;
        }

        /// <summary>`AiScripted.GetHorizontalOffset` / `GetTipPosition` 里那个 **`1.3`**
        /// （常量 `DAT_1834b33f0`：VA `0x1834b33f0` → `GameAssembly.dll` 的 `.rdata` / RVA `0x34b33f0`
        /// → 文件偏移 `0x34b0ff0`，四字节 `66 66 a6 3f` = **`1.29999995f`**；本机实读）。
        ///
        /// <para>🔴 **2026-10-18（`G9`）：它前面那个全局开关【定位到了】** —— 原来记的是
        /// 「`*(DAT_18427be00 + 0xb8) + 0x11c` **没定位** ⇒ 我们不乘」。逐条落定：
        ///   · `DAT_18427be00` = **`GameStaticData` 那个类** —— 同一批方法里紧挨着的
        ///     `*(int *)(DAT_18427be00 + 0xe4) == 0 → il2cpp_runtime_class_init(...)` 就是 il2cpp 的
        ///     class-init 检查（同族写法见 `CardDeck__CanAddCard.c` 里它紧挨着
        ///     `GameStaticData__IsNeutralArmy` 那一段）；
        ///   · `+0xb8` = `Il2CppClass.static_fields`，于是 `+0x11c` 就是**静态字段表里的第 N 格**；
        ///   · `d:/2/tools/il2cpp_out/dump.cs` 的 `GameStaticData` 字段表实读：
        ///     `0x118 RemoveCloudscript` · `0x119 DemoInitialSetup` · `0x11A UseAzureFunctions` ·
        ///     `0x11B hiFPS` · **`0x11C smallScreenUI`** · `0x11D touchInput` —— **逐格对得上**。
        ///   · **谁写它**：`GameStaticData.__cctor.c:207`（出厂 **0**）·
        ///     `GraphicsTab__SmallScreenToggleClick.c:12`（设置窗 Graphics 页那颗开关，逐句实读：
        ///      `static_fields+0x11c = param`、紧跟 `+0x12e = 1` = `smallUIChosenManually`）·
        ///     `PlayerDataManager__LoadPlayerData.c:193` / `LoadSharedDefaultValues.c:52` /
        ///     `LoadStarterData.c:57`（从玩家存档读）。
        ///   ⇒ **它就是一个真·用户设置**（不是分辨率/环境推出来的），默认关。
        ///   ✅ 我们**已经有这一格**：`CardPresentation.SmallScreenUI.Enabled`
        ///     （`Shell/TransformScalerBySmallScreenUI.cs`，注释里就写着它 = 原版这一格；
        ///      `CombatCameraZoom` / `DeckRuntime` 里读 `SmallScreenUI.Enabled` 那两处已在别处消费它）
        ///     ⇒ **照原版乘上去**（铁律 11：判据有了就得做）。</para>
        ///
        /// <para>⚠️ 自检影响：`Editor/BattleScene.cs` 的 `Run` 一开头就把这一格**压成关**、
        /// 收尾按原值放回（A175 那条纪律）⇒ 本节那些摆位断言**不受玩家偏好影响**。
        /// 另配了一条**两态**断言（`TutorialTipOffsetForTest`）把这两档钉住。</para></summary>
        public const float TutSmallScreenFactor = 1.3f;

        /// <summary>自检用：原版那个横向偏移量（`AiScripted.GetTipPosition` 的 `fVar9`），按 `positionReference`
        /// 那一档算出来 —— **和产品那条路同一个函数**（⛔ 别在自检里另写一遍算式）。</summary>
        public static float TutorialTipOffsetForTest(int positionReference) { return TutorialTipOffset(positionReference); }

        /// <summary>
        /// 原版 `PositionReference`（`dump.cs` **TypeDefIndex 478**）→ **锚点元素的世界坐标 + 尺寸**。
        /// 🔴 只落地数据里**真正用到**的那些值（61 条 `SmallTip` 用到 0/10/13/21/22/23/30/40/60/80，
        /// 39 条高亮用到 0/30/40/60/80/90）；认不出的值 ⇒ 退回屏幕中心并**出声一次**。
        /// 🔁 **2026-10-18（`G9`）把「真正用到」逐档数了一遍**（判据 = `RuleEngine/Resources/tutorial_stages.json`
        /// 的 449 条动作，`smallTipParams.positionReference` 全量计数；**这是可复算的**）：
        ///   · `SmallTip`(80) **n=61** —— `0×3 · 10×5 · 13×2 · 21×3 · 22×1 · 23×4 · 30×4 · 40×27 · 60×9 · 80×3`；
        ///   · 其余动作类型里出现的档：`EndTurn`(100) n=73 里 `90×12` · `PlayCard` n=107 里 `30×1 / 90×4` ·
        ///     `ChangeToRanged` n=17 里 `10×1` · `ActivateHandCards` n=6 里 `10×2` ·
        ///     `AiChat`/`PlayerChat`/`RadioMessage` 各 `90×1/1/0`；其余类型**全 0**。
        ///   · 🔴 **`100 = ActiveAbility` 【0 条】**（449 条里一次都没用到）——
        ///     这就是「`ActiveAbility` 那个锚点我们没做」那一笔的**最终结论**：
        ///     原版那一支取 `bm+0x168` 那颗钮 + `x -= 1.5`（`DAT_1834b3090` 实读），
        ///     **我们这边没有那一颗物件**；而数据里这一档**一条都没有** ⇒
        ///     **保持「退回屏幕中心 + 出声」是零可见影响的**（判据在此，⛔ 不是「忘了做」）。
        ///   · 同理 `50 EnemyCardInHand` / `70 EnemyMana` / `110 ChatButton` / `120 Cemetery` 也是 **0 条**
        ///     —— 其中 110/120 我们**照样实现了**（锚点物件存在、只是数据里没人用），
        ///     50/70 走 `default` 档（我们那边确实没有对应物件）。
        /// ⚠️ 「`*Melee/Range/Health`」（11/12/13/21/22/23）在卡面上是**同一张卡的那一排数值格**
        /// （原版 `IsReferenceCardElement` 把它们指到卡上的子元素）—— 我们只到**卡面**这一层，
        /// 三个子锚用卡宽的比例分开（**近似**，注释里如实标）。
        ///
        /// 🆕 2026-10-18（`Z5`）：**逐档照 `GetTipPosition` 的实参补上原版的定位**（方法体在本地，见
        /// <see cref="TutorialTipOffset"/> 那段）。本轮补进来的三处**都是硬判据**：
        ///   · **`EndTurn(90)`**：原版**不取任何物件**，直接写死 **`(8.1, 0.5, 从 BattleHud 取 z)`**
        ///     （`AiScripted__GetTipPosition.c` 里 `case 0x5a`：先 `LogError` 一句**再照样写死**）
        ///     ⇒ 我们照抄这两个数（z 取 `_endTurnBg` 的）。
        ///   · **`ChatButton(110)`**：取 `bm+0x140` 那颗钮的 position，`x -= 0.7`（`DAT_1834b3188` 实读）、`y += 0.5`。
        ///   · **`Cemetery(120)`**：取 `bm+0x110` 那颗钮的 position，`x -= 0.5`。
        ///   ⚠️ **`ActiveAbility(100)`**：原版取 `bm+0x168` 那颗钮的 position、`x -= 1.5`
        ///     （`DAT_1834b3090` 实读）—— 可我们这边**没有那一颗物件**（技能钮是 `AttackSelector` 里的选项）
        ///     ⇒ **认不出就不仿**，退回屏幕中心 + 出声一次（原版那一档数据里 0 条，零影响）。
        /// </summary>
        bool TutorialAnchor(int positionReference, out Vector3 center, out Vector2 size)
        {
            center = Vector3.zero; size = new Vector2(0.4f, 0.4f);
            var a = (TutAnchor)positionReference;
            CardView v = null; float sub = 0f;         // sub = 在卡面内向左偏多少（-1..1 个半宽）
            switch (a)
            {
                case TutAnchor.Center: return true;                                   // 屏幕中心
                case TutAnchor.PlayerWarlordMelee:  v = BoardViewAt(BoardSpec.WarlordSlot, true); sub = -0.55f; break;
                case TutAnchor.PlayerWarlordRange:  v = BoardViewAt(BoardSpec.WarlordSlot, true); sub = -0.18f; break;
                case TutAnchor.PlayerWarlordHealth: v = BoardViewAt(BoardSpec.WarlordSlot, true); sub = 0.55f; break;
                case TutAnchor.PlayerWarlord:       v = BoardViewAt(BoardSpec.WarlordSlot, true); break;
                case TutAnchor.EnemyWarlordHealth:  v = BoardViewAt(BoardSpec.WarlordSlot, false); sub = 0.55f; break;
                case TutAnchor.EnemyWarlord:        v = BoardViewAt(BoardSpec.WarlordSlot, false); break;
                case TutAnchor.EndTurn:
                    // 🔴 原版**写死的两个数**（不是取 `_endTurnBg` 的矩形）—— 判据 → 本方法上面那段。
                    //    🔁 **顺手核过一遍它靠不靠谱**（2026-10-18）：我们那颗钮在 `EndTurnX01 = 0.96263`
                    //    ⇒ 世界 x = (0.96263 − 0.5) × 17.778 = **8.224**，与原版写死的 **8.1** 只差 0.12
                    //    ⇒ 那两个数**确实是「END TURN 那颗钮」的粗略位置**（x 对得上）。
                    //    ⚠️ y **对不上**（我们那颗在 ±0.78 一带，原版写 0.5）—— 而原版那一支前面还有一句
                    //      `CustomDebug.LogError`（**「这一档本不该走到」**）⇒ 这两个数是**兜底常量**、
                    //      不是量出来的锚。我们**照抄原版**（判据优先），并如实标这条差异。
                    center = new Vector3(8.1f, 0.5f,
                                         _endTurnBg != null ? _endTurnBg.transform.position.z : 0f);
                    size = _endTurnBg != null ? new Vector2(_endTurnBg.WorldW, _endTurnBg.WorldH) : size;
                    return true;
                case TutAnchor.PlayerMana:
                    if (_energyGem == null) return false;
                    center = _energyGem.transform.position; size = new Vector2(_energyGem.WorldW, _energyGem.WorldH); return true;
                case TutAnchor.ChatButton:
                    if (_chatBtn == null) return false;
                    // 原版 `x -= 0.7` / `y += 0.5`（两个常量都在 `GameAssembly.dll` 常量池里实读过）
                    center = _chatBtn.transform.position + new Vector3(-0.7f, 0.5f, 0f);
                    size = new Vector2(_chatBtn.WorldW, _chatBtn.WorldH); return true;
                case TutAnchor.Cemetery:
                    if (_cemeteryBtn == null) return false;
                    center = _cemeteryBtn.transform.position + new Vector3(-0.5f, 0f, 0f);   // 原版 `x -= 0.5`
                    size = new Vector2(_cemeteryBtn.WorldW, _cemeteryBtn.WorldH); return true;
                case TutAnchor.PlayerCardInHand:
                {
                    // 手牌那一排的**中心**（原版 60 = 「玩家手牌里那一张」，对应哪一张见 `playerAction` 那一条的 acting）
                    int idx = _tutHandIdx >= 0 && _tutHandIdx < _handViews.Count ? _tutHandIdx : _handViews.Count / 2;
                    if (_handViews.Count == 0 || idx < 0 || idx >= _handViews.Count) return false;
                    v = _handViews[idx]; break;
                }
                case TutAnchor.PlayerMinion:  v = BoardViewAt(_tutAnchorSlot, true);  break;
                case TutAnchor.EnemyMinion:   v = BoardViewAt(_tutAnchorSlot, false); break;
                default:
                    if (_tutAnchorWarned.Add(positionReference))
                        Debug.LogWarning($"[Tutorial] `PositionReference = {positionReference}` 这一档我们**没有锚点**"
                            + " ⇒ 表现层退到**屏幕中心**（原版按 `GetTipPosition` 定位；我们只还原了数据里用到的那些值）。");
                    return true;
            }
            if (v == null) return false;
            var ls = v.transform.lossyScale;
            size = new Vector2(CardView.Width * Mathf.Abs(ls.x), CardView.Height * Mathf.Abs(ls.y));
            center = v.transform.position + new Vector3(size.x * 0.5f * sub, 0f, 0f);
            return true;
        }
        readonly HashSet<int> _tutAnchorWarned = new HashSet<int>();
        /// <summary>当前这条动作「指哪一格」（`acting` 优先，没有就 `target`）—— 给 30/40 那两个锚用。</summary>
        int _tutAnchorSlot = -1;
        /// <summary>当前这条动作指手牌第几张（60 那个锚）—— 认不出就是 -1。</summary>
        int _tutHandIdx = -1;

        // ------------------------------------------------------------------
        //  `hide*` 五条 + `TutorialSetup` 那条无条件藏（逐条见 R2 §4 表）
        // ------------------------------------------------------------------

        /// <summary>
        /// **教程关卡那五个 `hide*` 开关的落地**（R2 §4 逐条给了读数点与消费者）。
        /// ⚠️ 每帧算一遍是**故意的**：这些元素也被 `UpdateHud` 每帧写，藏/显必须跟着局走；
        ///    非教程局**逐条还原成显示**（`_tutVisDone` 只是为了让「零效果」的档只出声一次）。
        ///
        /// | 开关 | 6 关值 | 读数点 | 我们这边的消费者 |
        /// |---|---|---|---|
        /// | `hideCemetery` | **全 0** | `BattleManager__CanShowCemetery.c:61`（`stage+0x29`）；🔴 **唯一的原版消费者 = 死类 `CemeteryLogManager.ClickCemeterySlider.c:75`**（该族在发行版里没有实例 —— `G9` 实证，见 `BattleLogPanel` 的 `Z2`/`Z3` 段）⇒ 这一档**没有原版活消费点**；我们照字段语义做了闸门（`CemeteryRowClickAllowed`），如实标 |
        /// | `hideCardsLeftInDeck` | **全 1** | `BattleManager__CanDisplayDeckSize.c:43-46`（`stage+0x2a`）；**两个消费者** = `DeckManager__DisplayDeckSize.c:32-58` 与 `PlayerHand__ShowHandSize.c:29-48` | ✅ **两个都接**：牌库数（`_pileLabel`/`_foePileLabel` + 两块底板）**与**手牌数（`_handLabel` + `_handPlate`） |
        /// | `hideLargeCardDisplay` | **全 0** | `BattleManager__DisplayCard.c:44-50`（`stage+0x2b`；`!='\0' ⇒ return`） | 三个开窗口全过 `TutorialAllowsCardDisplay` |
        /// | `hideChat` | **全 1** | `BattleManager__TutorialSetup.c:52-60`（`stage+0x2c`；`chatButton.gameObject.SetActive(hideChat == 0)`） | ✅ `_chatBtn`（🔴 那是**打开语音弹窗那颗按钮**，**不是**聊天气泡面板） |
        /// | `bg` | 6 关全 `newBg_Tutorial` | ⛔ **载体查不到** | ⛔ 没做（R2 §4 表末：`assets_full` 全库 0 命中） |
        ///
        /// ⚠️ 另有 `TutorialSetup.c:~64` 把 **`cemeteryButton` 无条件 `SetActive(false)`**（与 `hideCemetery` 无关）
        ///    ⇒ 教程局里那颗钮**一律藏**（这是一条**会看到效果**的差别，尽管 `hideCemetery` 6 关全 0）。
        /// </summary>
        void ApplyTutorialVisibility()
        {
            var st = Ctx != null && Ctx.Tutorial != null ? Ctx.Tutorial.Stage : null;
            bool tut = st != null;

            // ---- ① `hideCardsLeftInDeck`（两个消费者一起）----
            bool hideSize = tut && st.hideCardsLeftInDeck;
            SetVis(_pileLabel, !hideSize); SetVis(_foePileLabel, !hideSize);
            SetVis(_myDeckSizePlate, !hideSize); SetVis(_foeDeckSizePlate, !hideSize);
            SetVis(_handLabel, !hideSize); SetVis(_handPlate, !hideSize);

            // ---- ② `hideChat`（是**那颗按钮**，不是气泡面板）----
            SetVis(_chatBtn, !(tut && st.hideChat));

            // ---- ③ `TutorialSetup` 的那条：教程局里 `cemeteryButton` **无条件藏** ----
            SetVis(_cemeteryBtn, !tut);

            // ---- ④ 三条「6 关全 0/查不到」的档：机制照做、如实出声一次 ----
            if (tut && !_tutVisReported)
            {
                _tutVisReported = true;
                Debug.Log($"[Tutorial] `hide*` 落位（第 {st.stage} 关）：`hideCemetery={st.hideCemetery}`"
                    + "（⚠️ 原版唯一消费者是日志面板那颗滑块，我们的 `BattleLogPanel` **没有那颗滑块**"
                    + " ⇒ 这一档在我们这边**没有落点**）· `hideCardsLeftInDeck={st.hideCardsLeftInDeck}`"
                    + "（牌库数 + 手牌数**两处都接**）· `hideLargeCardDisplay={st.hideLargeCardDisplay}`"
                    + " · `hideChat={st.hideChat}`（那颗**聊天按钮**）"
                    + " · `bg=\"{st.bg}\"`（⛔ 载体查不到 ⇒ **没做**）"
                    + " · `skipNormalBattleEndOnVictory={st.skipNormalBattleEndOnVictory}`"
                    + "/`…Defeat={st.skipNormalBattleEndOnDefeat}`"
                    + "（**接上了**：`TutorialSkipsNormalEnd()` → `EndPanel.Show(…, skipSequence:)` —— 把「开门+视频」"
                    + "那一段**一帧推完**；⚠️ 它的**读数点本地拿不到**，语义按字段名 + R2 的结论定，见那个方法的注释）");
            }
        }
        bool _tutVisReported;
        static void SetVis(Component c, bool on) { if (c != null && c.gameObject.activeSelf != on) c.gameObject.SetActive(on); }

        /// <summary>`hideLargeCardDisplay`（原版 `BattleManager__DisplayCard.c:44-50`：`!='\0' ⇒ return`）
        /// —— 教程局里**大卡展示窗不开**。三个开窗口（`ShowUnitCardWindow` / `ToggleUnitCard` / 手牌那张）
        /// 都问这一个判据（⛔ 别在三个调用点各写一遍）。</summary>
        bool TutorialAllowsCardDisplay
        {
            get { return !(Ctx != null && Ctx.Tutorial != null && Ctx.Tutorial.Stage.hideLargeCardDisplay); }
        }

        /// <summary>
        /// 🆕 2026-10-18（A940 收尾）：`skipNormalBattleEndOnVictory` / `…OnDefeat`
        /// （`TutorialStage` 的 **`+0x60` / `+0x70`**，`dump.cs:42066/42068`）——
        /// **赢/输对应的那一个为真 ⇒ 跳过「正常结算演出」那一段**。
        ///
        /// 🔴 **读数点本地拿不到（如实记，⛔ 不假装知道）**：R2 §4 把消费者记成
        /// `BattleManager__BattleFinished.c` 的 `BasicBattleEndSequence` 分支；我把
        /// `BattleManager__BattleFinished.c` · `BattleManager__BasicBattleEndSequence.c` ·
        /// `BattleManager._BasicBattleEndSequence_d__391__MoveNext.c` **三份都读了** ——
        /// **没有一份读 `stage+0x60/+0x70`**（也没有 `GetCurrentTutorialStage`）；
        /// 全库交叉搜「`GetCurrentTutorialStage` 之后 800 字符内出现 `+ 0x60)` / `+ 0x70)`」**零命中**
        /// ⇒ 疑似方法体缺失（同「教程那份 `PlayScriptedTurn` 的 `.c` 缺失」那一族）。
        /// 🔁 **换了第二种搜法复核过**（2026-10-18）：**文件级共现**（`GetCurrentTutorialStage` 与
        /// `0x60`/`0x70` 同时出现在同一份 `.c` 里）命中 3 份 ——
        /// `BattleManager._SetupMulliganPhase_d__341__MoveNext.c:113`（
        /// `*(lVar5 + 0xd0) + 0x60`，**基址是 `+0xD0` 那个指针、不是 stage**）·
        /// `BattleManager__DeadHero.c` 与 `BattleManager__GetWinnerAfterBattleEnd.c`
        /// （**两处读的都是 `stage + 0xB8`** = `playerAlwaysWins`，它们的 `+0x70` 读在**卡**上）
        /// ⇒ **3 份全是「不同基址」的假命中**，结论不变。
        /// **落地语义**（跳掉演出、直接进结算面板）是按**字段名 + R2 的结论**定的，见 `EndPanel.Show`。
        /// ⚠️ 6 关两个值**全 false** ⇒ 今天零可见影响；⚠️ 平局（`Winner == 3`）**两个都不算**。
        /// </summary>
        bool TutorialSkipsNormalEnd()
        {
            var st = Ctx != null && Ctx.Tutorial != null ? Ctx.Tutorial.Stage : null;
            if (st == null) return false;
            if (Ctx.Winner == _me + 1) return st.skipNormalBattleEndOnVictory;
            if (Ctx.Winner == 1 - _me + 1) return st.skipNormalBattleEndOnDefeat;
            return false;                       // 平局 / 没结束 ⇒ 两个都不适用
        }
        /// <summary>自检用：那两个开关现在算出来真不真（产品里由 `UpdateHud` 的结算段读）。</summary>
        public bool TutorialSkipsNormalEndForTest { get { return TutorialSkipsNormalEnd(); } }

        /// <summary>教程里「跳过这一关」——`Overlay` 那一颗钮判「点在它身上」，这里判「准不准按」。</summary>
        public bool TutorialSkipClicked { get { return _tutOverlay != null && _tutOverlay.SkipClickedByPlayer; } }
        /// <summary>自检用：把「点在跳过钮上」这一下喂进去（产品里由 `Update` 的抬起沿喂）。</summary>
        public bool TutorialTrySkipForTest() { return _tutOverlay != null && _tutOverlay.TrySkip(); }

        // ------------------------------------------------------------------
        //  动作档 → 表现（原版 `ExecuteAction` 里那几支 + `_TutorialStartSequence`）
        // ------------------------------------------------------------------

        /// <summary>
        /// 🔴 **K2（审查）**：原版 `AiScripted.PlayScriptedTurn` 在「这一条是玩家动作」那一支里调的是
        /// **`0x180944290 = ShowPlayerActionAnim`**（同一支还会关 `ScreenHighlightPosition`，见 VA 行 `1809440f3`）。
        /// **调用点本来在执行器里**，可 `RuleEngine/Core/*` 本批**冻结**（另有写手）
        /// ⇒ 我们把它挂在**调用方** `DriveTutorialScript` 的 `WaitingForActor` 那一支上 ——
        /// **语义相同**（就是「脚本停住、轮到玩家」的那一刻），并配了「同一条只做一次」的闩。
        /// 内容 = 那一条的表现（高亮 / 指点光标 / 小提示）**亮起来**。
        /// </summary>
        void ShowPlayerActionAnim(TutorialAction a)
        {
            var ov = EnsureTutorialOverlay();
            if (ov == null || a == null) return;
            Ctx.Log($"[Tutorial] 玩家动作出场（原版 `ShowPlayerActionAnim`，RVA `0x180944290`）："
                  + $"第 {Ctx.Tutorial.Turn} 回合第 {Ctx.Tutorial.ActionCounter} 条 `{a.type}`");
            ApplyTutorialActionView(a, null, true);
        }

        // ==================================================================
        //  🆕 2026-10-18（`A940` 尾账）：**动作音效 + 聊天五档 + 「等提示」**
        // ==================================================================

        /// <summary>教程动作的自带音效放这一条（原版 `SoundManager` 那一套里的 2D 支）。
        /// 🔴 按需建：**只有真的播过音效的局**才会有这一个 `AudioSource`。</summary>
        AudioSource _tutSfx;

        AudioSource EnsureTutorialSfx()
        {
            if (_tutSfx != null) return _tutSfx;
            if (hudRoot == null) return null;
            _tutSfx = hudRoot.gameObject.AddComponent<AudioSource>();
            _tutSfx.playOnAwake = false;
            _tutSfx.spatialBlend = 0f;                 // 原版 `Play2D`
            _tutSfx.outputAudioMixerGroup = WarpforgeAudio.VoicesGroup;
            return _tutSfx;
        }

        /// <summary>
        /// 放**这一条脚本动作自带的音效**（`ScriptedAction.sound`，`SoundAsset`）。
        ///
        /// 🔴 **判据链（逐份读过，别照「数据里 volume 是 0 就静音」那条旧说法）**：
        ///   · `ScriptedActionCampaignData__GetSoundAsset.c`：新建 `SoundAsset` 时
        ///     `+0x10 = GetBundledSound(name)`、**`+0x18 = 0x3f800000`（音量硬编码 1.0）**
        ///     —— SO 里那一格 `volume`（数据里 82/95 是 0.0）**运行时不被采信**；
        ///   · `BattleManager__PlayNextSoundInQueue.c:…`：出队时
        ///     `AudioSource.PlayOneShot(clip, AudioListener.volume * soundAsset.volume)` ⇒ 就是 **×1.0**。
        ///   ⇒ 我们照做：`PlayOneShot(clip, AudioListener.volume)`。
        ///   ⚠️ 那条**旧说法**（生成器 `crossCheckNote` 里写着「`sound.volume` 0 就是静音」）**已就地订正**，
        ///     更正痕迹留在 `工具/gen_tutorial_stages.py` 与产物里那一行。
        ///
        /// 🔴 **播放点的出路（如实记）**：`BattleManager__PlaySoundAsset` 在**全量反编译里零调用点**
        ///   （它只是把 `SoundAsset` 入队 `+0x428`），推它进去的那一处**方法体本地拿不到**。
        ///   ⇒ **我们把「这一条动作执行/出场」当作播放点**（一拍一条），这与
        ///   `GetCurrentWaitTime` 会把音的**时长并进这一拍的延迟**是自洽的。
        ///
        /// 🔴 **2026-10-18（`A899`）订正（铁律 5）：** 本注释原来在这句后面还写着
        ///   「**教程那份 `AiScripted.GetCurrentWaitTime` 重载同样缺失**（落盘的是战役那份、用 `+0x14`）」
        ///   —— **这句不成立**。
        ///   错因：把「战役侧与教程侧各走一个函数」误当成了「同一个函数有两份重载、教程那份没落盘」。
        ///   实际：`GetCurrentWaitTime` **全库只有一个签名**（`d:/2/tools/il2cpp_out/dump.cs:24668`，`param_3` 是
        ///   `List&lt;ScriptedActionCampaignData&gt;`），**落盘的这份就是它、也就是战役侧在用的那个**；
        ///   教程侧走的是**另一个函数** `AiScripted.GetDelay`（`+0x24 waitAfter` / `+0x20 waitBefore`）。
        ///   ⇒ **两份判据都在本地**（`decomp_full/AiScripted__GetCurrentWaitTime.c:14-43` ·
        ///      `decomp_full/AiScripted__GetDelay.c:14-43`）。
        ///   时长语义那三条、以及 `+0x28` 是**两张不同的表**（`ScriptedActionCampaignData.waitTime @0x28`
        ///   vs `ScriptedAction.textReference @0x28`）这件事，**判据正本 = `RuleEngine/Core/TutorialScript.cs`
        ///   的 `PlayScriptedTurn` 注释里 `A899` 那一节** —— ⛔ 这里不抄第二份（铁律 6）。
        ///   🔑 顺带钉住的一条：`CanSkipAction` 判的那 5 档（`0x32/0x37/0x3C/0x41/0x5A`）**恰好等于**
        ///   本文件 `IsTutorialChatKind` 的五档（见那个方法的注释，两处互为锚点）。
        ///   ⚠️ 本件**只动注释**；那一拍的真正落码归 `A940`（`_postTimer` 一族）。
        /// ⚠️ **聊天那五档不走这里** —— 它们的 `sound` 就是那句台词的 VO，由 `UnitChatPanel.Speak` 自己播
        ///   （再走一次 = 同一句放两遍）。
        /// </summary>
        void PlayTutorialSound(TutorialAction a)
        {
            var s = a != null ? a.sound : null;
            if (s == null || string.IsNullOrEmpty(s.name)) return;
            if (IsTutorialChatKind(a.Kind)) return;        // 见上：聊天自己会播
            var src = EnsureTutorialSfx();
            if (src == null) return;
            var clip = Resources.Load<AudioClip>(VoiceLines.ClipRoot + s.name);
            if (clip == null)
            {
                if (_tutSoundWarned.Add(s.name))
                    Debug.LogWarning($"[Tutorial] 音效 `{s.name}` 取不到（找 `Resources/{VoiceLines.ClipRoot}{s.name}.ogg`）"
                        + " ⇒ 这一条**没声**。跑 `工具/import_original_audio.py` 补齐"
                        + "（教程那一批 43 条在 `素材/Warpforge原版/音频/督军语音/AudioClip/`）。");
                return;
            }
            src.PlayOneShot(clip, AudioListener.volume);   // 音量恒 1.0（判据见上）
        }
        readonly HashSet<string> _tutSoundWarned = new HashSet<string>();

        /// <summary>是不是「聊天那几档」（原版 `ExecuteAction` 里走 `DisplayChatBox` / `ShowRadioMessage` 的那几支）。
        /// 🔑 **2026-10-18（`A899`）：这五档与原版 `AiScripted.CanSkipAction` 的 5 档【恰好是同一组】——两处互为锚点。**
        ///   判据（`decomp_full/AiScripted__CanSkipAction.c`）：它判 `action.scriptedActionData[0].actionType`（`+0x10`）
        ///   ∈ {`0x32`,`0x37`,`0x3C`,`0x41`,`0x5A`} = {50, 55, 60, 65, 90} =
        ///   `PlayerChat` / `PlayerChatBig` / `AiChat` / `AiChatBig` / `RadioMessage`。
        ///   ⇒ **改动其中一边，另一边会立刻显得可疑**（那正是这条对照的用处）。
        ///   时长语义那三条的判据正本 = `RuleEngine/Core/TutorialScript.cs` 的 `PlayScriptedTurn` 注释（`A899` 一节）。</summary>
        static bool IsTutorialChatKind(ScriptedActionType k)
        {
            return k == ScriptedActionType.PlayerChat || k == ScriptedActionType.PlayerChatBig
                || k == ScriptedActionType.AiChat || k == ScriptedActionType.AiChatBig
                || k == ScriptedActionType.RadioMessage;
        }

        /// <summary>脚本这一条动作指的**那个人**是哪一张卡（`acting` 优先，没有就 `target`）——
        /// 聊天那五档要用它的立绘/卡名。认不出 ⇒ `null`（**出声**由调用方负责）。
        /// ⚠️ 返回 `CardDef`（不是 `CardInstance`）：场上那一格是 `UnitState`（`.Card` 才是定义），
        ///    手牌那一格是 `CardInstance` —— 两种都能给出 `CardDef`，那就统一到它。</summary>
        CardDef TutorialSpeakingCard(TutorialAction a, out int side, out string why)
        {
            side = -1; why = null;
            var d0 = (a != null && a.data != null && a.data.Length > 0) ? a.data[0] : null;
            var r = d0 != null && d0.acting != null && d0.acting.unitType != 0 ? d0.acting
                  : (d0 != null ? d0.target : null);
            if (r == null) { why = "这一条动作的 `acting`/`target` 两格都空"; return null; }
            // 两侧的判定只用 `unitType`（原版 `GetActingCard` 那张表）——**手牌那一档也算**：
            //   聊天的发起者可能是手牌里的一张（S1 第 4 回合那条 `PlayerChat` 就是 `Primaris Intercessor`）。
            side = TutorialRules.SideOfUnitType(r.unitType);
            if (side < 0) { why = "`unitType = " + r.unitType + "` 认不出是哪一侧"; return null; }
            int slot;
            if (TutorialRules.FindBoardUnit(Ctx, r, out side, out slot, out why))
            {
                if ((ScriptedActionUnit)r.unitType == ScriptedActionUnit.PlayerWarlord ||
                    (ScriptedActionUnit)r.unitType == ScriptedActionUnit.AiWarlord)
                    return Ctx.Players[side].Warlord != null ? Ctx.Players[side].Warlord.Card : null;
                var u = Ctx.Players[side].Board[slot];
                return u != null ? u.Card : null;
            }
            int hi = TutorialRules.FindHandIndex(Ctx, r, out why);
            if (hi >= 0) { var ci = Ctx.Players[side].Hand[hi]; return ci != null ? ci.Card : null; }
            // ⚠️ 认不出「哪一张」**不影响「谁说」** —— 原版 `DisplayChatBox` 拿不到卡也是照说的
            //    （它只用卡取立绘；取不到就画不出头像，气泡照样在）。⇒ 这里出声但**不拦**。
            return null;
        }

        /// <summary>
        /// 🆕 **聊天五档**（`PlayerChat 50` / `PlayerChatBig 55` / `AiChat 60` / `AiChatBig 65` /
        /// `RadioMessage 90`）× **三颗气泡**（原版 `Unit Chat/{PlayerChatDisplay, EnemyChatDisplay, RadioChat}`）。
        ///
        /// 🔴 **判据（逐份读过）**：
        ///   · **序号**：`VoiceLinesController__ShowRadioMessage.c:28` 明确传
        ///     `UnitsVoiceLinesPanel__ShowChatBox(panel, **2**, data)`；而
        ///     `UnitsVoiceLinesPanel__GetChatBox.c`：`0 ⇒ +0x20` / `1 ⇒ +0x28` / `2 ⇒ +0x30`
        ///     ⇒ **2 = `RadioChat`**（其余 ≥3 直接抛异常）。
        ///   · **谁说的**：`VoiceLinesController__DisplayChatBox.c:38` 传的是 `card+0x40`（是不是玩家）
        ///     ⇒ 聊天档的**两侧**由 `acting.unitType` 那一侧定（`PlayerXxx ⇒ 0` / `AiXxx ⇒ 1`）。
        ///   · **说什么**：`ScriptedAction.GetLocalizedText(action)` → 远端 I2 表按
        ///     `textReference`（`Tutorial1/Turn1/Ventris1` 这类键）取词条；**本地没有那张表**
        ///     （84 个 bundle 里零命中）⇒ 落回 SO 内嵌的 `arg`（= 数据里那句话本身，见
        ///     `TutorialAction.arg`）。**中文同理**（原版客户端根本没有中文表）。
        ///   · **配音**：`DisplayChatBox` 第 4 个实参 = `(action + 0x18)` 里那个 `SoundAsset` 的 clip
        ///     ⇒ 就是 `action.sound`（本批新接进 DTO）。
        ///
        /// ⚠️ **`PlayerChatBig` / `AiChatBig` 走的是 `BattleManager.DisplayBigChat`**（原版另有
        ///   `BigChat` 那一层，`ExecuteAction.c:335`）；**数据里这 6 关一条都没有**（实测 0 条）
        ///   ⇒ 我们**按同一颗气泡处理**并出声（⛔ 不凭空造那一层）。
        /// </summary>
        void SpeakTutorialChat(TutorialAction a)
        {
            if (a == null) return;
            if (_unitChat == null)
            {
                Debug.LogWarning("[Tutorial] 聊天五档要 `UnitChatPanel`，可它是 null ⇒ 这一句**没显示**（不静默）；"
                    + "语音条那一件在 `BuildHud` 里建，非教程局的 HUD 也在。");
                return;
            }
            if (a.Kind == ScriptedActionType.PlayerChatBig || a.Kind == ScriptedActionType.AiChatBig)
                Debug.Log("[Tutorial] 这一条是 `*ChatBig`（原版 `DisplayBigChat` 那一层我们没做）——"
                        + " 按同一颗气泡显示（6 关数据里这 2 档实测 0 条）。");

            string why;
            int side;
            var who = TutorialSpeakingCard(a, out side, out why);
            if (side < 0) side = a.Kind == ScriptedActionType.AiChat ? 1 : 0;      // 兜底：按档位那一侧
            if (a.Kind == ScriptedActionType.RadioMessage) side = 2;               // 电台：原版就是第 3 颗

            AudioClip clip = null;
            var s = a.sound;
            if (s != null && !string.IsNullOrEmpty(s.name))
            {
                clip = Resources.Load<AudioClip>(VoiceLines.ClipRoot + s.name);
                if (clip == null && _tutSoundWarned.Add(s.name))
                    Debug.LogWarning($"[Tutorial] 台词 `{s.name}` 的音取不到（`Resources/{VoiceLines.ClipRoot}{s.name}.ogg`）"
                        + " ⇒ 气泡照显示、**只是没声**。");
            }
            // 🔴 卡名（`who?.NameZh`）**走语言闸** —— 它是 `Speak` 的 `fallbackName`，
            //   而那一格**会显示**（`UnitChatPanel.Speak`：`text` 空 ⇒ 印 `fallbackName`）——
            //   同 `CardDisplayName` 的 doc 里那第 ③ 条。
            string text = string.IsNullOrEmpty(a.arg)
                        ? CardDisplayName(who, null) : a.arg;
            _unitChat.Speak(side, who != null ? who.Id : ("TutChat" + a.index),
                            "Tut" + a.Kind,
                            who != null ? ArtKey(who) : null,
                            CardDisplayName(who, null),
                            text, clip, s != null ? s.name : null);
            Ctx.Log($"[Tutorial] 聊天第 {side + 1} 颗气泡（{(side == 2 ? "RadioChat" : side == 0 ? "Player" : "Enemy")}）"
                  + $"：`{a.Kind}`「{text}」" + (clip != null ? "" : "（**没声**）"));
        }

        /// <summary>自检用：直接喂一条聊天动作进去（产品那条路在 `ApplyTutorialActionView` 里）。</summary>
        public void SpeakTutorialChatForTest(TutorialAction a) { SpeakTutorialChat(a); }

        // ------------------------------------------------------------------
        //  🆕 `Z6`：`waitForTip` 那 61 条的「等」
        // ------------------------------------------------------------------

        /// <summary>
        /// 🔴 **原版语义**（`SmallTipParams`/`TutorialTipParams` 的 `waitForTip`，61/61 全 true）：
        ///   提示弹出来之后**脚本停在原地**，直到这一条提示被消掉
        ///   （`BattleManager__ShowTutorialTip.c:8-16`：`action.smallTipData.waitForTip ⇒
        ///    `set_WaitingForTutorialTipFlag(bm, 1)`）。**消掉**由 `TutorialTipScript` 那一侧发生
        ///   —— 它那两个常量是我们手上仅有的判据：`minTimeBeforeSkip = 1.0`（刚弹出来这 1 秒内不许消）
        ///   与 `tipDuration`（数据里 50 s / 120 s）。
        ///
        /// ⛔ **我们原来「不等」**（提示照显示、脚本下一帧就往下走）—— 那是**没做**，不是取舍。
        /// ✅ 现在按上面那两条常量做：**到点自动消**（`tipDuration`）+ **玩家点一下也能消**（≥ `minTimeBeforeSkip`）。
        /// ⚠️ **批处理里没有帧循环** ⇒ 这个计时器**显式步进**（`TickTutorialView(dt)`，自检直接喂），
        ///    ⛔ 不是「等一帧」。
        /// ⚠️ **`tipDuration` 50/120 秒是我们照数据来的**（没改）—— 它同时是「自动消」的上限，
        ///    玩家点一下就能提前过（不然一关 15 条提示会变成十几分钟）。
        /// </summary>
        float _tutTipElapsed;     // 这一条提示已经显示了多久（秒）
        float _tutTipLimit;       // 这一条的 `tipDuration`（秒）
        bool _tutTipUp;           // 有一条 `waitForTip` 的提示正挂着、脚本等它

        /// <summary>自检用：脚本现在**是不是卡在某条提示上**（`waitForTip` 那个「等」）。</summary>
        public bool TutorialWaitingForTip { get { return _tutTipUp; } }
        /// <summary>自检用：这条提示已经显示多久了。</summary>
        public float TutorialTipElapsed { get { return _tutTipElapsed; } }
        /// <summary>自检用：这条提示的自动消上限（= 数据里的 `tipDuration`）。</summary>
        public float TutorialTipLimit { get { return _tutTipLimit; } }
        /// <summary>原版 `TutorialTipScript.minTimeBeforeSkip = 1.0`（复用 `TutorialOverlay` 那一个常量，
        /// ⛔ 别在这儿另立一个数）。</summary>
        const float TutTipMinBeforeDismiss = TutorialOverlay.MinTimeBeforeSkip;
        /// <summary>🔴 **我们挑的兜底**：数据里 `tipDuration` 是 0 或负数时用这个（实测 61 条**全是 50 / 120**,
        /// 所以这条分支今天一次都不会走；写出来是为了「数据脏了不许把脚本永久卡死」）。</summary>
        const float TutTipDefaultLimit = 10f;

        /// <summary>**把当前这条提示消掉**（原版：`TutorialTipScript` 那一侧把
        /// `WaitingForTutorialTipFlag` 清回去）。返回 true = 真的消掉了（脚本下一帧就能往下走）。
        /// `force = true` 时**不看** `minTimeBeforeSkip`（给「到点自动消」那一支用）。</summary>
        public bool DismissTutorialTip(bool force = false)
        {
            if (!_tutTipUp) return false;
            if (!force && _tutTipElapsed < TutTipMinBeforeDismiss)
            {
                Debug.Log($"[Tutorial] 这一条提示才显示了 {_tutTipElapsed:F2}s（原版 `minTimeBeforeSkip = "
                        + $"{TutTipMinBeforeDismiss}s` 之内不许消）—— 这一下**不算**。");
                return false;
            }
            _tutTipUp = false;
            _tutTipElapsed = 0f; _tutTipLimit = 0f;
            if (_tutOverlay != null) _tutOverlay.SetTip(false, null, false, Vector3.zero, 0f, TutRelation.Exact);
            Ctx.Log($"[Tutorial] 提示消掉 ⇒ 脚本可以往下走（原版清 `WaitingForTutorialTipFlag`）");
            return true;
        }
        /// <summary>自检用：喂一下「消提示」这一手（产品里由 `Update` 的抬起沿喂）。</summary>
        public bool DismissTutorialTipForTest(bool force) { return DismissTutorialTip(force); }

        /// <summary>产品输入：日志/聊天/跳过那些之后，**点一下就把提示消掉**（原版 `TutorialTipScript` 那一支）。
        /// 返回 true = 这一下被提示吃掉了。</summary>
        bool HandleTutorialTipDismiss()
        {
            if (!_tutTipUp) return false;
            if (Ctx == null || Ctx.Tutorial == null) return false;
            if (!ReleasedThisFrame()) return false;
            DismissTutorialTip();      // 按早了（1 秒闸）⇒ 这一下仍被吃掉（别让它穿到棋盘上）
            return true;
        }

        // ------------------------------------------------------------------
        //  🆕 2026-10-18（`A939`）：**战后脚本**（`onVictoryScriptedActions` / `onDefeatScriptedActions`）
        // ------------------------------------------------------------------
        //
        //  判据（逐份读过，`d:/2/tools/decomp_full/`）：
        //   · **数据**：每关 SO 的 `onVictoryScriptedActions(+0x58)` / `onDefeatScriptedActions(+0x68)`，
        //     产物 = `TutorialStageData.onVictory` / `onDefeat`（`TurnScriptedData`，**不是动作数组**）。
        //     实测 6 关：`onVictory` 三个字段全齐（`scriptedTurn=true`、共 **13 条**）、
        //     `onDefeat` **6 关全 `scriptedTurn=false` 且 0 条**。
        //   · **门**：`BattleManager__IsPostBattleScriptAvailable.c` —— 有教程关时
        //     `isVictory ? stage+0x58 : stage+0x68`，要求那一个 `TurnScriptedData` 的
        //     `+0x18`（`scriptedTurn`）真 **且** `+0x20`（`actions`）`Count > 0`。
        //     ⇒ 6 关**全开**（对照：`preMulligan` 那条门 `IsPremulliganScriptAvailable` 要求 `+0x18`
        //       真，而 6 关全 false ⇒ **恒假、那 19 条原版也不跑**，见 `TutorialStageData.preMulligan`）。
        //   · **跑在哪**：`BattleManager__BattleFinished.c:80-112` —— 战斗结束后判那个门，
        //     取 `stage+0x58/+0x68` → `BattleManager.ExecuteScriptedTurn(bm, data, 1, 0, onDone)` 起协程
        //     （`isPlayer = 1`、`resetActionCounter = 0`），**与 `AiScripted` 是同一台执行器**。
        //   · **13 条是什么**：逐条 dump 过 —— `AiChat(60)×7` · `PlayerChat(50)×5` · `RadioMessage(90)×1`，
        //     **一条都不改局面**（`playerAction` 全 false、`ExecuteCore` 里全是演出档）
        //     ⇒ **引擎侧一个字节都不动**，要的只是**按节拍把它们演出来**（聊天五档 + 音效）。
        //     节拍用数据自带的 `waitBefore` / `waitAfter`（实测 0.2 / 0.3 / 2.2 / 2.8 / 3.5 / 4.0 / 5.0 / 5.5）。
        //
        //  🔴 **一处如实交代的边界（未查清）**：原版那一支在门开时是 **`ExecuteScriptedTurn` 顶掉
        //     `BasicBattleEndSequence`**（`BattleFinished` 里那是 if/else，不是先后），
        //     而「结算演出那一段」的**收尾挂在脚本的 `onDone` 回调**上 —— 那个回调
        //     （`UIGenericEventCatcher_SourceDelegate___ctor(uVar8, lVar5, DAT_18429a120, 0)`）
        //     **本地读不出它指谁** ⇒ 「脚本跑完 ⇒ 再放开门视频」这条链**钉不死**。
        //     ⇒ 我们**照旧先出结算面板**（`_endPanel.Show`，它自带开门视频），脚本**与它并行**演进
        //       （⛔ 没假装知道「原版是先演完 ending 再开门」）。这一条**记成待办**，不静默。
        //     ⚠️ 6 关 `skipNormalBattleEnd*` **全 false** ⇒ 就算原版真的跳过了那一段，
        //        我们这边也**只是多播了一段视频**（不会少东西）。

        /// <summary>战后脚本那 13 条（跑的时候才有值）。</summary>
        TutorialAction[] _postActions;
        /// <summary>跑到第几条（`0` 起；`== Length` = 跑完）。</summary>
        int _postStep;
        /// <summary>离下一条还有多少秒（数据自带的 `waitAfter` + 下一条的 `waitBefore`）。</summary>
        float _postTimer;
        /// <summary>这一局起过没有（一局一次；`Begin` 清零）。</summary>
        bool _postStarted;

        /// <summary>原版 `BattleManager.IsPostBattleScriptAvailable` 的等价物
        /// （判据逐条写在上面那段注释里）。
        /// ⚠️ 只有**教程局**才可能为真（非教程局 `GetCurrentTutorialStage` 是 null ⇒ 原版恒 false）。</summary>
        public bool PostBattleScriptAvailable
        {
            get
            {
                var st = Ctx != null && Ctx.Tutorial != null ? Ctx.Tutorial.Stage : null;
                if (st == null || !Ctx.IsOver) return false;
                var turn = (Ctx.Winner == _me + 1) ? st.onVictory : st.onDefeat;   // 平局走 onDefeat（原版同理：不是胜就是负）
                if (turn == null || !turn.scriptedTurn) return false;             // 门的第一半（`+0x18`）
                return turn.actions != null && turn.actions.Length > 0;           // 门的第二半（`+0x20` 的 Count > 0）
            }
        }
        /// <summary>自检用：门算出来真不真。</summary>
        public bool PostBattleScriptAvailableForTest { get { return PostBattleScriptAvailable; } }
        /// <summary>自检用：这一局起过战后脚本没有。</summary>
        public bool PostBattleScriptStarted { get { return _postStarted; } }
        /// <summary>自检用：战后脚本一共几条 / 跑到第几条（`0` = 还没跑第一条）。</summary>
        public int PostBattleScriptCount { get { return _postActions != null ? _postActions.Length : 0; } }
        public int PostBattleScriptStep { get { return _postStep; } }
        /// <summary>自检用：跑完了没有。</summary>
        public bool PostBattleScriptDone { get { return _postStarted && _postActions != null && _postStep >= _postActions.Length; } }

        /// <summary>起战后脚本（原版 `BattleFinished` 里那句 `ExecuteScriptedTurn(stage+0x58, isPlayer:1, resetActionCounter:0, …)`）。
        /// 返回 true = 真的起了。**幂等**（第二局/第二次调用不会重起）。</summary>
        public bool StartPostBattleScript()
        {
            if (_postStarted) return false;
            if (!PostBattleScriptAvailable) return false;
            var st = Ctx.Tutorial.Stage;
            var turn = (Ctx.Winner == _me + 1) ? st.onVictory : st.onDefeat;
            _postActions = turn.actions;
            _postStep = 0;
            _postStarted = true;
            // 第一拍的等待 = 第一条自己的 `waitBefore`（原版那台协程也是先 `SkippableActionWait` 再演）
            _postTimer = _postActions[0] != null ? Mathf.Max(0f, _postActions[0].waitBefore) : 0f;
            Ctx.Log($"[Tutorial] 战后脚本起来了（原版 `IsPostBattleScriptAvailable` + `BattleFinished:80-112`）："
                  + $"第 {st.stage} 关 · **{_postActions.Length} 条**（"
                  + (Ctx.Winner == _me + 1 ? "`onVictory`" : "`onDefeat`") + "）· 全是聊天/电台演出 ⇒ 引擎侧不动");
            return true;
        }
        /// <summary>自检用：直接起（产品里由结算段那句 `StartPostBattleScript()` 起）。</summary>
        public bool StartPostBattleScriptForTest() { return StartPostBattleScript(); }

        /// <summary>战后脚本那个泵（挂在 `AdvanceTimeline` 上：真机每帧、批处理靠 `BattleScene.Step`）。
        /// 一拍一条，间隔 = 上一条的 `waitAfter` + 下一条的 `waitBefore`（**数据自带的节拍**）。</summary>
        void TickPostBattleScript(float dt)
        {
            if (!_postStarted || _postActions == null) return;
            if (_postStep >= _postActions.Length) return;
            _postTimer -= dt;
            if (_postTimer > 0f) return;
            var a = _postActions[_postStep];
            _postStep++;
            // ⚠️ 走的是**表现那条链**（`ApplyTutorialActionView`）—— 它自己会放音效、演聊天五档；
            //    ⛔ 不碰引擎（这 13 条本来就是演出档，`ExecuteCore` 里一条分支都没有）。
            ApplyTutorialActionView(a, null, false);
            float after = a != null ? Mathf.Max(0f, a.waitAfter) : 0f;
            float beforeNext = (_postStep < _postActions.Length && _postActions[_postStep] != null)
                             ? Mathf.Max(0f, _postActions[_postStep].waitBefore) : 0f;
            _postTimer = after + beforeNext;
            if (_postStep >= _postActions.Length)
                Ctx.Log("[Tutorial] 战后脚本跑完了（" + _postActions.Length + " 条）");
        }
        /// <summary>自检用：显式推这个泵（批处理里 `Step()` 已经推了；这里给「只想推它」的断言用）。</summary>
        public void TickPostBattleScriptForTest(float dt) { TickPostBattleScript(dt); }

        /// <summary>
        /// 把**一条脚本动作**的表现做出来（原版：`ExecuteAction` 的 `SmallTip` 那一支 +
        /// `EnableTutorialHighlight` / `ScreenHighlightPosition`）。
        /// <paramref name="done"/> 非空 = 这条**脚本刚执行完**（`PlayCard`/`Attack`/…）⇒ 用它的格号当锚点。
        /// </summary>
        void ApplyTutorialActionView(TutorialAction a, ScriptedActionDone? done, bool waiting)
        {
            var ov = EnsureTutorialOverlay();     // 幂等（第一次才真建）；非教程局返回 null
            if (ov == null || a == null) return;
            var st = Ctx.Tutorial.Stage;

            // ---- ⓪ 🆕 2026-10-18（`A940` 尾账 · 音效）：**这一条动作自带的那支音** ----
            //   原版 `ScriptedAction.sound`（`SoundAsset`）；播放点与音量的判据 → `PlayTutorialSound`。
            //   ⚠️ 排在最前面：这一条动作的**每一拍**都放一次（含高亮/提示/出牌那一拍）。
            //   ⚠️ 聊天那五档在 `PlayTutorialSound` 里被跳过（它们的 `sound` 就是那句话的 VO，
            //      由下面的 `SpeakTutorialChat` 自己播 —— 再放一次 = 同一句两遍）。
            PlayTutorialSound(a);

            // ---- 锚点的「指谁」：acting 优先、没有就 target（`PositionReference` 30/40/50/60 用它）----
            _tutAnchorSlot = -1; _tutHandIdx = -1;
            var d0 = (a.data != null && a.data.Length > 0) ? a.data[0] : null;
            if (d0 != null)
            {
                TutorialUnitRef r = d0.acting != null && d0.acting.unitType != 0 ? d0.acting : d0.target;
                if (r != null)
                {
                    int side, slot; string why;
                    if (TutorialRules.FindBoardUnit(Ctx, r, out side, out slot, out why)) _tutAnchorSlot = slot;
                    else _tutHandIdx = TutorialRules.FindHandIndex(Ctx, r, out why);
                }
            }

            // ---- ① 小提示（`SmallTip 80`；原版 `BattleManager__ShowTutorialTip`）----
            if (a.Kind == ScriptedActionType.SmallTip)
            {
                var tp = a.smallTipParams;
                int pref = tp != null ? tp.positionReference : 0;
                int rel = tp != null ? tp.positionRelation : 0;
                Vector3 c; Vector2 sz;
                if (!TutorialAnchor(pref, out c, out sz)) { c = Vector3.zero; sz = new Vector2(0.4f, 0.4f); }
                // ⚠️ 箭头那两格 6 关全 false（实测 0/0）⇒ 传下去也永远不会亮（机制照做）
                ov.SetTip(true, a.arg, tp != null && tp.tipWithContinue, c, TutorialTipOffset(pref), (TutRelation)rel);
                if (tp != null && (tp.showLeftArrow || tp.showRightArrow))
                    Debug.Log($"[Tutorial] 这一条提示要箭头（左={tp.showLeftArrow} 右={tp.showRightArrow}）——"
                            + $" 6 关 430 条动作里实测**全是 false**，本工程第一次真的亮起箭头（第 {st.stage} 关）");
                // 🆕 2026-10-18（`Z6`）：`waitForTip` ⇒ **脚本停在这条提示上**（判据见 `_tutTipUp` 的注释）。
                //   ⚠️ 只有 `waitForTip == true` 才等（数据 61/61 全 true，但判据照写，别写死）。
                if (tp != null && tp.waitForTip)
                {
                    _tutTipUp = true;
                    _tutTipElapsed = 0f;
                    _tutTipLimit = tp.tipDuration > 0f ? tp.tipDuration : TutTipDefaultLimit;
                    var waitMsg = $"[Tutorial] 提示挂着、脚本**停在这儿等**（原版 `set_WaitingForTutorialTipFlag(1)`；"
                                + $"`waitForTip=true` · `tipDuration={_tutTipLimit:F1}s` · "
                                + $"`minTimeBeforeSkip={TutTipMinBeforeDismiss:F1}s`）";
                    Ctx.Log(waitMsg);
                    // 🔴 同一句话**也要出声**：`Ctx.Log` 只往内存 `Events` 里加、**从不 `Debug.Log`**
                    //    （`RuleEngine/Core/BattleContext.Log`）⇒ 批处理 / 自检里这句话**一个字都看不到**，
                    //    而这一句正是「脚本为什么停住」的唯一读数 ⇒ 症状会表现成「指针**无缘无故**停住」
                    //    （诊断 → `资料/普查产出_1018/DC_教程推进21红.md` §6·1；与「不许静默失败」同族）。
                    //    ⚠️ **只加日志、不改逻辑** —— 那个 `_tutTipUp` 早退有逐字判据，⛔ 别动。
                    Debug.Log(waitMsg);
                }
                return;                            // 小提示是**独占**的一拍（原版 `waitForTip` 会等它）
            }

            // ---- ② 教学标注四块（`ActivateHandCards 170` 前后；原版 `CardScript.ShowWarlordActiveAbilityForTutorial`）----
            //     ⚠️ 「前后」= 这一档**出现的那一段**；换到别的档就收掉（原版有 `Hide…` 那一支）。
            if (a.Kind == ScriptedActionType.ActivateHandCards)
            {
                ov.SetAnnotations(true, TutorialAnnotationTexts(), TutorialAnnotationAnchors());
                return;
            }
            ov.SetAnnotations(false, null, null);

            // ---- ②′ 🆕 2026-10-18（`A940` 尾账 · 聊天五档）----
            //   原版 `ExecuteAction` 的 `PlayerChat/AiChat/*ChatBig/RadioMessage` 那几支
            //   → `VoiceLinesController.DisplayChatBox` / `ShowRadioMessage`（判据 → `SpeakTutorialChat`）。
            //   ⚠️ **不 return** —— 原版那几支走的是 `switchD_…_caseD_20`（= 通用收尾），
            //      高亮那一段在它**之后**（`ExecuteAction.c:1253-1272`）⇒ 照旧往下走。
            if (IsTutorialChatKind(a.Kind)) SpeakTutorialChat(a);

            // ---- ③ 高亮（`shouldHighlightElement`，6 关 39 条）----
            //    原版：脚本执行那一支是「先关高亮 → ExecuteAction → 该亮的自己再亮」
            //    （`ExecuteAction.c:1253-1272` → `EnableTutorialHighlight`）；玩家那一支是 `ShowPlayerActionAnim`。
            if (a.shouldHighlightElement)
            {
                var tp = a.smallTipParams;
                int pref = tp != null && tp.positionReference != 0 ? tp.positionReference
                         : TutorialAnchorFromAction(d0);
                Vector3 c; Vector2 sz;
                if (TutorialAnchor(pref, out c, out sz)) ov.SetHighlight(true, c, sz);
                else ov.SetHighlight(false, Vector3.zero, Vector2.zero);
            }
            else ov.SetHighlight(false, Vector3.zero, Vector2.zero);

            // ---- ④ 指点光标（原版 `CheckForTutorialPointer`，只在 `playerAction == 1` 时走）----
            if (waiting)
            {
                Vector3 c; Vector2 sz;
                int pref = TutorialAnchorFromAction(d0);
                if (TutorialAnchor(pref, out c, out sz)) ov.SetPointer(true, c);
                else ov.SetPointer(false, Vector3.zero);
            }
            else ov.SetPointer(false, Vector3.zero);
        }

        /// <summary>没有 `smallTipParams` 时，从动作自己推锚点（`acting`/`target` 的 `unitType` 就是那一套枚举）。</summary>
        static int TutorialAnchorFromAction(TutorialActionData d)
        {
            if (d == null) return 0;
            var r = d.acting != null && d.acting.unitType != 0 ? d.acting : d.target;
            if (r == null) return 0;
            switch ((ScriptedActionUnit)r.unitType)
            {
                case ScriptedActionUnit.PlayerWarlord: return (int)TutAnchor.PlayerWarlord;
                case ScriptedActionUnit.AiWarlord: return (int)TutAnchor.EnemyWarlord;
                case ScriptedActionUnit.PlayerMinion:
                case ScriptedActionUnit.PlayerMinionLeft:
                case ScriptedActionUnit.PlayerMinionNotLeft: return (int)TutAnchor.PlayerMinion;
                case ScriptedActionUnit.EnemyMinion: return (int)TutAnchor.EnemyMinion;
                case ScriptedActionUnit.PlayerCardInHand: return (int)TutAnchor.PlayerCardInHand;
                case ScriptedActionUnit.EnemyCardInHand: return (int)TutAnchor.EnemyCardInHand;
                default: return 0;
            }
        }

        /// <summary>教学标注四块的字 = **原版那四块自己的词条**。
        /// 🔴 **2026-10-18（第十二轮 · W6）就地订正（铁律 5）**：这里原来写
        /// 「原版那四块是 TMP + **远端 I2 词条表，本地没有** ⇒ 这四句是我们按卡面语义写的」——
        /// **「本地没有」不成立，四块逐块找得到**：
        ///   `Battle/Tips/{MeleeAttack,RangeAttack,HealthPoints,EnergyCost}`，挂在
        ///   `Card Display Window &lt; Card Display &lt; TutorialObjs &lt; UnitObjs` 的
        ///   `MeleeText` / `RangedText` / `HealthText` / `EnergyText` 四颗上，
        ///   TMP 原文 = **`Melee Attack` / `Ranged Attack` / `Health Points` / `Energy Cost`**
        ///   （`bundle_scenes_scenes_battlearena1/MonoBehaviour/{4051,5287,4840,…}.json`，13 个 arena 各一份）。
        /// **错因**：同 `资料/已知的坑.md` #20 那一族（**只扫一个包 / 只看一个载体 ⇒ 说成「本地没有」**）。
        /// ⇒ 现在走 `Loc.T`，键名照原版 `mTerm`（中文那一列的口径 → `Core/Loc.cs`）。
        /// ⚠️ **位置仍是我們挑的**（`TutorialAnnotationAnchors`）—— 那一半不变。</summary>
        string[] TutorialAnnotationTexts()
        {
            return new[] { Loc.T("Battle/Tips/MeleeAttack"), Loc.T("Battle/Tips/RangeAttack"),
                           Loc.T("Battle/Tips/HealthPoints"), Loc.T("Battle/Tips/EnergyCost") };
        }
        Vector3[] TutorialAnnotationAnchors()
        {
            var w = _myUnits.ContainsKey(BoardSpec.WarlordSlot) ? _myUnits[BoardSpec.WarlordSlot] : null;
            var p = w != null ? w.transform.position : Vector3.zero;
            var e = _energyGem != null ? _energyGem.transform.position : Vector3.zero;
            return new[] { p + new Vector3(-1.0f, 0.5f, 0f), p + new Vector3(1.0f, 0.5f, 0f),
                           p + new Vector3(0f, -0.9f, 0f), e };
        }

        /// <summary>
        /// 每帧步进：表现层的淡入淡出 + **督军两拍落场** + 跳过钮。
        /// ⚠️ 由 `Update` 喂 dt（**不挂 `Update` 里的补间族** —— 那些在 `AdvanceTimeline` 那个泵上，
        ///    而本工程批处理里两个都不会跑，所以自检另有 `TickTutorialViewForTest`）。
        /// </summary>
        void TickTutorialView(float dt)
        {
            if (_tutOverlay != null) _tutOverlay.Tick(dt);
            if (Ctx == null || Ctx.Tutorial == null) return;
            if (_tutOverlay == null) return;

            // ---- 🆕 2026-10-18（`Z6`）：`waitForTip` 那一条的计时 ----
            //   ⚠️ **显式步进**（本工程批处理没有帧循环）⇒ 自检直接喂 `TickTutorialViewForTest(dt)`。
            //   ⚠️ **到点自动消**（`tipDuration`，原版 `TutorialTipScript` 那一格）。
            if (_tutTipUp)
            {
                _tutTipElapsed += Mathf.Max(0f, dt);
                if (_tutTipLimit > 0f && _tutTipElapsed >= _tutTipLimit)
                    DismissTutorialTip(true);       // 到点自动消（不看 `minTimeBeforeSkip`）
            }

            // ---- 督军两拍（原版 `BattleManager.TutorialStartSequence`：玩家督军 → WaitForSeconds → 敌方督军）----
            //  ⚠️ 原版那两条 `WaitForSeconds` 的实参没落进 `.c` ⇒ 秒数是**我们挑的**（见常量注释）。
            //  ⚠️ 我们这边两个督军在 `RuleCore.NewBattle` 里**同时就位**（引擎态），
            //     所以「两拍」**只是表现**：先把敌方督军的视图藏起来、第二拍再显。
            if (!_tutBeatDone)
            {
                _tutWarlordBeat += Mathf.Max(0f, dt);
                var foeW = BoardViewAt(BoardSpec.WarlordSlot, false);
                if (_tutWarlordBeat < TutWarlordBeatSeconds)
                {
                    if (foeW != null && foeW.gameObject.activeSelf) foeW.gameObject.SetActive(false);
                }
                else
                {
                    if (foeW != null && !foeW.gameObject.activeSelf) foeW.gameObject.SetActive(true);
                    _tutBeatDone = true;
                }
            }

            // ---- 跳过钮（教程局常显）----
            _tutOverlay.SetSkip(true);
        }

        /// <summary>自检用：显式喂 dt（批处理没有帧循环）。</summary>
        public void TickTutorialViewForTest(float dt) { TickTutorialView(dt); }

        // ---- 自检读口（`hide*` 那一族的**盘上事实**：藏住了没有）----
        static bool Vis(Component c) { return c != null && c.gameObject.activeSelf; }
        /// <summary>牌库数那一族（**两个消费者里的第一个**）：两边的牌库数文字 + 两块底板。</summary>
        public bool DeckSizeVisibleForTest
        { get { return Vis(_pileLabel) && Vis(_foePileLabel) && Vis(_myDeckSizePlate) && Vis(_foeDeckSizePlate); } }
        /// <summary>**第二个消费者**：手牌数（文字 + 底板）。⚠️ 审查点名「别只做牌库那一半」。</summary>
        public bool HandSizeVisibleForTest { get { return Vis(_handLabel) && Vis(_handPlate); } }
        /// <summary>`hideChat` 的对象 = **那颗聊天按钮**（不是气泡面板）。</summary>
        public bool ChatButtonVisibleForTest { get { return Vis(_chatBtn); } }
        /// <summary>`TutorialSetup.c:~64` 无条件藏的那一颗（与 `hideCemetery` 无关）。</summary>
        public bool CemeteryButtonVisibleForTest { get { return Vis(_cemeteryBtn); } }
        /// <summary>`hideLargeCardDisplay`（原版 `DisplayCard.c:44-50`）。</summary>
        public bool AllowsCardDisplayForTest { get { return TutorialAllowsCardDisplay; } }
        /// <summary>自检用：`ctx.Events` 里有没有那句话（K2 的「玩家动作出场」靠它做**可观测**的判据）。</summary>
        public bool TutorialEventsContain(string s)
        {
            if (Ctx == null || Ctx.Events == null || string.IsNullOrEmpty(s)) return false;
            for (int i = 0; i < Ctx.Events.Count; i++)
                if (Ctx.Events[i] != null && Ctx.Events[i].Contains(s)) return true;
            return false;
        }
        /// <summary>自检用：把一条脚本动作的**表现**直接喂进去（`ActivateHandCards` 那种要真打到那一关才轮得到的）。
        /// ⚠️ 它只走表现那一半（`ApplyTutorialActionView`），**不碰引擎**。</summary>
        public void ApplyTutorialActionViewForTest(TutorialAction a) { ApplyTutorialActionView(a, null, true); }

        /// <summary>
        /// **教程「跳过」那一颗钮的语义那一半**（原版：`BattleSettingsWindow__SkipTutorialButtonOnClick.c`
        /// → `WindowsManager.CloseWindow` + `BattleManager__ClickSkip.c`）。
        ///
        /// 🔴 **2026-10-18（A991）：那颗钮已经搬进【设置面板】**（`SettingsPanel` 的 `SkipCxPx` 那一段，
        /// 面板内中心 (+171.7, −310.5)、300×90）—— 本方法只剩**语义那一半**；「关窗」那一半由调用点
        /// （<see cref="SettingsClickAt"/>）显式做，**先后照原版**：先 `CloseWindow`、再 `ClickSkip`
        /// （`SkipTutorialButtonOnClick.c` 方法体里就是这两行、这个顺序）。
        ///
        /// 🔴 **2026-10-18 就地订正（铁律 5）**：本段原来有过两版说法 —— 先是「⚠️ **没查清**：那两处对不上
        /// （**场景节点**在 `Bottom buttons/SkipTutorial Button [22,1375 300x90]`「**HUD 根下**」，而
        /// **处理函数的名字**却是 `BattleSettingsWindow__…`）」；后来（`Z7` 那轮）又改写成「那**是两颗不同的钮**：
        /// 设置窗里一颗，HUD 底部那颗的处理器活在**场景的序列化 `onClick`** 里（同 `ClickCemeterySlider`
        /// 那一族：全量反编译零调用点）」。
        /// ⇒ **两版都不成立**，逐条现核（铁律 2，13 个战场场景各一份）：
        ///   · **全库只有一颗钮**：每场的 `GameObject/` 里只有一份 `SkipTutorial Button`
        ///     （判据 = `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena*/GameObject/SkipTutorial Button.json`，13/13）；
        ///   · **它在设置窗里，不在 HUD 根下**：按 `m_Father` 逐跳解父链（arena1）= `SkipTutorial Button`
        ///     → `Bottom buttons` → **`BattleSettingsPanel`** → `Safe area FrontCanvas` → `FrontCanvas` → `Canvas`
        ///     （起点 = `bundle_scenes_scenes_battlearena1/RectTransform/RectTransform_3271.json`，
        ///     同一棵树的文字版见 `资料/说明书/01_战斗_对战/2D层_battlearena1全树.md:718-722`——它在
        ///     `BattleSettingsPanel`（`:673`）之下，**不是** `Tutorial`（`:733`）之下）；
        ///   · **序列化 `onClick` 是空表**：那颗钮的 uGUI `Button`（`MonoBehaviour_4945.json`）的
        ///     `m_OnClick.m_PersistentCalls.m_Calls` = `[]`（13/13 同）。
        ///   ⇒ **它的处理器就只有代码接的那一处**：`BattleSettingsWindow.skipTutorialButton` →
        ///     `SkipTutorialButtonOnClick`（`d:/2/tools/decomp_full/BattleSettingsWindow__SkipTutorialButtonOnClick.c`：
        ///     `WindowsManager.CloseWindow(&lt;设置窗&gt;)` + `BattleManager.ClickSkip(bm, 0)`）—— 所以「场景节点」
        ///     与「处理函数名」**本来就是同一颗钮**（先关设置窗、再跳过），当年那个「对不上」是**我们自己看错**。
        ///   ✅ **2026-10-18（A991）落点已对齐**：我们原来把那颗钮建在 `TutorialOverlay` 自己那棵 HUD
        ///     子树上（摆位还标着「我们挑的」）—— **那是落点错**；现在它建在 `SettingsPanel` 里
        ///     （判据：面板绝对框 + `Bottom buttons` 两颗的绝对矩形，见 `SettingsPanel` 文件头那组 `Skip*` 常量）。
        ///     ⛔ 别把这条读回「原版 HUD 上一颗 + 设置窗里一颗」，也⛔ 别把钮建回 HUD。
        ///
        /// 🔴 **`ClickSkip` 到底做什么（判据现读）**：`BattleManager__ClickSkip.c`：
        /// `if (MatchData.playMode == 100 /*Tutorial*/) DeadHero(bm, 0, 4); else LogError`
        /// ⇒ **「跳过教程」= 把我方督军判死（死因 4）**，对局当场结束。
        /// （非教程局它只 `LogError` ⇒ 这一跳**只在教程局有效**；但**那颗钮本身原版是常驻的**
        ///  —— 所以本件**不加「只在教程局才建/才显示」的闸**，行为差异由 `ApplyTutorialSkip` 出声。）
        /// ⚠️ 「第二个实参 `0` = 玩家侧」这条口径与 R2 §4 里 `DeadHero(对面, 1, 3)` 那条注一致
        ///   （那里写「`1` = 本机死」是**联机局**的口径；教程局里 `0` = `playerManager` = 玩家）。
        ///
        /// ⚠️ **一处如实记的差异（本轮没动，交主对话裁）**：我们这条链上还留着 `TutorialOverlay.TrySkip`
        ///   的「按早了不算」闸（`_shownFor >= minTimeBeforeSkip`，1.0 s）。**原版那颗钮没有这道闸** ——
        ///   `minTimeBeforeSkip` 是 `TutorialTipScript` 的字段（`il2cpp.h` 的 `TutorialTipScript_Fields`：
        ///   `minTimeBeforeSkip` / `preventSkipTip` / `inPlayClickTime`），管的是**提示自己**什么时候能被消，
        ///   和设置窗那颗钮无关。⛔ **本轮不删**：那条闸被 `Editor/BattleScene` 里 `SimulateDropPreview` 那一族测试口的断言钉住了，
        ///   而那个宿主**不在本件白名单里**（要删得两边一起改）。
        ///   📌 实际影响 ≈ 0：设置窗要玩家先点齿轮才开得出来，那时 `_shownFor` 早就过 1 秒。
        /// </summary>
        void SkipTutorialFromSettings()
        {
            if (Ctx == null || Ctx.IsOver) return;
            var ov = _tutOverlay;
            if (ov != null && !ov.TrySkip()) return;      // 上面那条「按早了不算」的闸
            ApplyTutorialSkip();
        }

        /// <summary>「跳过教程」的**语义那一半**（原版 `BattleManager__ClickSkip.c`）：
        /// `MatchData.playMode == 100 /*Tutorial*/` ⇒ `DeadHero(bm, 0, 4)` ⇒ **把我方督军判死**、对局当场结束；
        /// 其余档原版**只 `LogError`**（⇒ 这颗钮只在教程局有效，我们照做：非教程局什么都不做 + 出声）。</summary>
        void ApplyTutorialSkip()
        {
            if (Ctx == null || Ctx.IsOver) return;
            if (Ctx.MatchType != MatchType.Tutorial)
            {
                Debug.LogWarning("[Tutorial] `ClickSkip` 在**非教程局**上什么都不做（原版那一支是 `LogError`）"
                    + $" —— 本局 `MatchType = {Ctx.MatchType}（{(int)Ctx.MatchType}）`。");
                return;
            }
            Debug.Log("[Tutorial] 「跳过教程」⇒ 按原版 `BattleManager.ClickSkip`："
                    + "**把我方督军判死**（`DeadHero(bm, 0, 4)`，`MatchData.playMode == 100` 那一支）");
            // 🆕 **2026-10-18（A913）如实记一处差异**：原版这一跳是 `DeadHero(bm, 0, 4)` ——
            //   第三个实参 **4 = `BattleResult.WinButton`**（"跳过"），而我们走的是「把督军血置 0 + `CheckWinner`」，
            //   **不经过 `RuleCore.Forfeit`** ⇒ **码 4 在我们引擎里没有落点**。
            //   ⛔ 为什么不改成 `RuleCore.Forfeit(Ctx, _me, BattleResult.WinButton)`：
            //   我们的 `Forfeit` 会**额外写 `Ctx.ForfeitedBy`**，而原版这一跳**没有** `AddResignAction`
            //   （`ClickSkip.c` 里只有那一次 `DeadHero`）⇒ 照套会让**结算面板的副标题说错话**
            //   （`EndPanel` 按 `ForfeitedBy` 说「我方投降」/「督军倒下」）—— 那是**新造**一个偏离。
            //   ⇒ 现状**更忠实**，但「码 4」这条信息确实丢了；要不要另开一个「判负但不算弃权」的口
            //   = 交主对话裁（已写进 A913 报告「判据不足」那一节）。
            var w = Ctx.Players[_me].Warlord;
            if (w != null) w.Health = 0;
            RuleCore.CheckWinner(Ctx);
            RefreshAll(); UpdateHud();
        }
        /// <summary>自检用：直接走「跳过」的语义那一半（产品里由 `SettingsClickAt` 喂，
        /// 2026-10-18 A991 起 —— 钮在设置面板里）。</summary>
        public void ApplyTutorialSkipForTest() { ApplyTutorialSkip(); }

        /// <summary>没牌可出、也没技能可放、也没人能攻击了 → 别让玩家干等，自动结束回合</summary>
        void AutoEndTurnIfStuck()
        {
            if (Ctx.IsOver || Ctx.Active != _me) return;
            // ⚠️ 用 `HasAnyAction`（**不掷骰、不吃难度旋钮**）—— 难度那套会「随机砍掉最低分的动作」，
            //    拿它判「玩家卡住了」会**误判**（玩家明明还能出牌，却被砍成只剩 endTurn）。
            if (SimpleAI.HasAnyAction(Ctx)) return;
            // 🔴 2026-10-18（A938）**教程局除外**：脚本没跑完就替玩家收回合 = **整条脚本链当场跑偏**
            //    （原版也不会 —— 等玩家的那一条在 `_ExecuteScriptedTurn` 里是把协程**停住**，
            //     没有「替他做」这一档）。脚本跑完了照常自动收（否则玩家真卡住时出不去）。
            var tut = Ctx.Tutorial;
            if (tut != null && tut.CurrentTurn != null && !tut.FinishedScriptedActionsInTurn) return;
            Debug.Log("[Battle] 没牌可出、没技能可放也没人能打 —— 自动结束回合");
            EndPlayerTurn();
        }

        // ==================================================================
        //  🆕 2026-10-18（A938）：**教程局的执行器接线**（把 `Ctx.Tutorial` 真正挂进回合循环）
        // ==================================================================
        //
        //  判据（逐句读过的原版那几份协程 / 方法体，`d:/2/tools/decomp_full/`）：
        //   · **脚本每一帧「推一条」** —— `AiScripted.PlayScriptedTurn`（**教程**那份重载）。
        //     🔴 **2026-10-18 更正（铁律 5）：** 这里原来写「**无条件** `ExecuteAction(当前那条)` 然后
        //     `*(+0x14) += 1`」。**实际不是** —— 判据 = 反汇编 **VA `0x180944060`**（教程那份重载；
        //     `decomp_full/AiScripted__PlayScriptedTurn.c` 落盘的是**战役**那份、用 `+0x14`）：
        //       `180944125 cmp byte ptr [rax+0x40], r9b`（`ScriptedAction.playerAction`）
        //       → `180944129 jne 0x180944171`（**玩家动作 ⇒ 跳，不执行**）
        //       → `18094412b call 0x180940dc0`（`ExecuteAction`，只有脚本自己执行那支才走到）
        //       → `180944130 inc dword ptr [rbx+0x10]`（**只有这一支 ++**，教程口径 **`+0x10`**）
        //       → `180944171 call 0x180944290`（= `ShowPlayerActionAnim`）
        //       → `180944176 xor al,al; ret`（**返回 false；没有 ExecuteAction、没有 ++**）
        //     **错因**：只读了落盘的那份 `.c`（战役重载），没按「`.c` 读不出来 ≠ 拿不到（VA 反汇编）」去反汇编。
        //     **代价**：据此写过一条「我们偏离了原版、请裁」的取舍 —— **那条不成立，我们的行为本来就跟原版一致**。
        //     ⛔ 顺带：别再把教程口径写成 `+0x14`。
        //   · **等玩家的那一条 ⇒ 脚本原地停住** —— `_ExecuteScriptedTurn_d__597__MoveNext.c:236-246`：
        //     协程调 `PlayScriptedTurn`（**它自己会分岔**）、再看 `IsNextActionScriptedPlayerAction`（`cVar3`）——
        //     **真 ⇒ 直接 `return 0`（协程结束、不再往下走）**。⚠️ 那句「无条件调 `PlayScriptedTurn`」
        //     之所以没坏事，**正是因为守卫在 `PlayScriptedTurn` 里面**（两处是互补的，⛔ 不是二选一）。
        //   · **玩家做完了 ⇒ 指针 +1 并重启脚本** —— `BattleManager__FinishResolvingAction.c:168-182`：
        //     `CheckIfWaitingForPlayerAction` 为真 ⇒ `AiScripted.PlayerChoiceAction()`（= `*(+0x10) += 1`）
        //     ⇒ `ExecuteScriptedTurn(...)` 起协程。
        //   · **玩家回合也要跑脚本** —— `_NextTurn_d__395__MoveNext.c:427-431`：
        //     `IsScriptedStageAvailable` ⇒ `ExecuteScriptedTurn(GetCurrentTurnScriptedData(), …)`。
        //
        //  🔑 **`playerAction` 到底是谁做**（这一条是本轮**从数据实测**出来的，判据比注释硬）：
        //     430 条动作里 `playerAction == true` 的那 110 条，`acting.unitType` **无一例外**是
        //     `PlayerWarlord(10)` / `PlayerMinion(30)` / `PlayerCardInHand(50)` / `None(0)`
        //     —— **没有一条是 `AiXxx`**。⇒ `playerAction = true` 的语义就是
        //     「**这一条要玩家自己做**」；`false` = 「脚本自己做」。
        //     ⇒ 所以「AI 回合要不要等玩家」这件事**在数据里根本不出现**（下面那支出声是防御性的）。
        //
        //  ⚠️ 原版那些 `SkippableActionWait` / 音效 / 高亮是**演出节拍**，不在这一批（那是 A940）——
        //     我们这里「**每帧一步**」。
        public enum TutorialStep
        {
            /// <summary>不是教程局 / 本回合没有脚本 ⇒ **一切照旧**（常规驱动接着走）。</summary>
            NotOwned,
            /// <summary>脚本这一帧推了一条（多半是它自己执行的那种）。</summary>
            Advanced,
            /// <summary>当前这条要**玩家自己做**（`playerAction = true`）⇒ 脚本停在这儿等。</summary>
            WaitingForActor,
            /// <summary>本回合的脚本跑完了（指针过了末尾）⇒ 交给常规驱动收尾。</summary>
            Exhausted,
        }

        /// <summary>本回合**有脚本**吗（原版 `GetCurrentTurnScriptedData` 非空的那一支）。</summary>
        public bool TutorialOwnsThisTurn
        {
            get { return Ctx != null && Ctx.Tutorial != null && Ctx.Tutorial.CurrentTurn != null; }
        }

        /// <summary>🆕 2026-10-18（A938）：**回合变了 ⇒ 把脚本指针更新过去**。
        /// 原版 `AiScripted.UpdateTurn(turnNumber)`：只在**变大**时写，写的那一刻把 `actionCounter` 清 0
        /// （`AiScripted__UpdateTurn.c`）。调用点 = **四个 `RuleCore.BeginTurn` 各跟一次**
        /// （原版那五处协程启动点里，回合开始那一处是 `_NextTurn_d__395:431`）。
        /// ⚠️ 非教程局恒空转（`Ctx.Tutorial == null`）—— 与加这一句之前逐字等价。</summary>
        void SyncTutorialTurn()
        {
            if (Ctx == null || Ctx.Tutorial == null) return;
            Ctx.Tutorial.UpdateTurn(Ctx.Turn);
        }
        /// <summary>自检用：显式把脚本指针同步到当前回合（批处理里 `Begin` 那条链会自己调一次；
        /// 这一条给「自己 `new TutorialScript(...)` 之后直接推」的用例用 —— ⛔ 判据与上面同一处。</summary>
        public void SyncTutorialTurnForTest() { SyncTutorialTurn(); }

        /// <summary>
        /// 🔴 **教程脚本这一帧的「一步」**（等价物 = 原版 `_ExecuteScriptedTurn` 协程里那一跳）。
        /// `Update` 的两条回合驱动（`DrivePlayerTurn` / `DriveAiTurn`）各叫一次。
        ///
        /// ⛔ **它只推进、不判断「谁该动」** —— 那是调用方的事：AI 那一侧遇到
        /// <see cref="TutorialStep.WaitingForActor"/> 就该收尾交回合，玩家那一侧则要**把输入放过去**
        /// （否则玩家永远点不下去，自锁）。
        /// </summary>
        public TutorialStep DriveTutorialScript()
        {
            var tut = Ctx != null ? Ctx.Tutorial : null;
            if (tut == null || tut.CurrentTurn == null) return TutorialStep.NotOwned;
            // 🔴 **回放局里执行器不许自驱** —— 脚本动作**已经在动作流里**了，这里再跑一遍
            //   就是「同一个动作演两遍」（回放的终局指纹会当场对不上）。
            if (_replaySession) return TutorialStep.NotOwned;
            // 🆕 2026-10-18（`Z6`）：**上一条 `SmallTip` 的「等」还没走完 ⇒ 脚本停在原地**
            //   （原版：`ShowTutorialTip` 置 `WaitingForTutorialTipFlag`，协程等它被清掉）。
            //   ⛔ **不返回 `WaitingForActor`** —— 那在 AI 那一侧会被当成「这条要玩家做」而**收回合**
            //      （`DriveAiTurn` 的收尾支）；返回 `Advanced` 才是「脚本还占着、这一帧别再往下」。
            //   ⚠️ 计时器在 `TickTutorialView` 里显式走（批处理没有帧循环）。
            if (_tutTipUp) return TutorialStep.Advanced;
            if (tut.IsNextActionScriptedPlayerAction)
            {
                // 🔴 🆕 2026-10-18（A940）：**K2 的落点** —— 脚本停住、轮到玩家那一刻
                //   调一次 `ShowPlayerActionAnim`（原版在 `PlayScriptedTurn` 的那一支里，
                //   我们挂在这儿：`RuleEngine/Core/*` 本批冻结）。**同一条只做一次**（闩在 turn+counter 上）。
                if (_tutViewTurn != tut.Turn || _tutViewCounter != tut.ActionCounter)
                {
                    _tutViewTurn = tut.Turn; _tutViewCounter = tut.ActionCounter;
                    ShowPlayerActionAnim(tut.CurrentAction);
                    // ⚠️ 原版那一支**同时关掉高亮**（VA `1809440f3`：`ScreenHighlightPosition.*`）——
                    //    除非这条动作自己写着 `shouldHighlightElement`（那就在 `ShowPlayerActionAnim` 里重新亮）。
                }
                return TutorialStep.WaitingForActor;
            }
            if (tut.FinishedScriptedActionsInTurn) return TutorialStep.Exhausted;
            // 脚本自己执行的那一支：原版**先关高亮**再 `ExecuteAction`（高亮由动作自己再亮，见 `ApplyTutorialActionView`）
            if (_tutOverlay != null) _tutOverlay.SetHighlight(false, Vector3.zero, Vector2.zero);
            StepTutorialScript();
            return TutorialStep.Advanced;
        }

        /// <summary>执行脚本当前那一条（一帧一条）—— 记账口那三个字段要在**执行前**抓。</summary>
        void StepTutorialScript()
        {
            // 🔴 与 `LocalAct` 同一条规矩：**面板答案要在引擎结算之前抓**（一结算队列就空了）。
            _tutPicks = (_rec != null && Ctx.ChoosePicks.Count > 0) ? Ctx.ChoosePicks.ToArray() : null;
            _tutPickIds = (_rec != null && Ctx.ChooseCardIds.Count > 0) ? Ctx.ChooseCardIds.ToArray() : null;
            Ctx.Tutorial.PlayScriptedTurn(Ctx);
            ReportUnaskedChoices();
            RefreshAll();
            UpdateHud();
        }
        int[] _tutPicks;
        string[] _tutPickIds;

        /// <summary>
        /// 🆕 2026-10-18（A938）：**玩家把脚本等着他做的那一步做完了** ⇒ 指针 +1。
        ///
        /// 判据（`BattleManager__FinishResolvingAction.c:168-182`）：先 `CheckIfWaitingForPlayerAction`
        /// 为真（当前那条确实是等玩家的）、**再** `PlayerChoiceAction()`（= `*(+0x10) += 1`）。
        /// ⚠️ **`CheckIfWaitingForPlayerAction` 自带的守卫照抄**（`actionCounter + 1 &lt; count`，
        ///   `AiScripted__CheckIfWaitingForPlayerAction.c`）—— 也就是说**本回合最后一条**不推进；
        ///   那没关系：回合一换 `UpdateTurn` 就把指针归零了（数据实测：36 个回合的快照里，
        ///   最后一条不是 `EndTurn` 的只有 S2 第 5 回合那一条 `PlayerChoice`，它本来就放行一切）。
        /// </summary>
        void ContinueTutorialScript()
        {
            var tut = Ctx != null ? Ctx.Tutorial : null;
            if (tut == null) return;
            if (!tut.CheckIfWaitingForPlayerAction) return;
            tut.AdvancePlayerAction();
            // 🔴 **2026-10-18（`REV_W_四写手.md` F2 · 铁律 5）**：这句原来**只有 `Ctx.Log`** ——
            //    而 `RuleEngine/Core/BattleContext.Log` **只往内存 `Events` 加、从不 `Debug.Log`**
            //    ⇒ **批处理里一个字都看不到**（症状表现为「指针无缘无故动了/没动」，只能靠读口反猜）。
            //    `DC_教程推进21红.md` §6·1 点名的**就是这两句**（另一句 `Z6` 那句 `WB` 已补）⇒ 这里补上，**只加日志、逻辑不动**。
            var doneMsg = $"[Tutorial] 玩家做完了脚本等着的那一步 ⇒ 指针 +1（第 {tut.Turn} 回合第 {tut.ActionCounter} 条）";
            Ctx.Log(doneMsg);
            Debug.Log(doneMsg);
        }

        /// <summary>
        /// 🆕 2026-10-18（A938）：**玩家的动作准不准做** —— 原版
        /// `BattleManager.CheckIfPlayerActionPermittedInTutorial` 在我们这边的落点。
        ///
        /// 🔴 **作用域只有「教程局 + 人类座位」**：
        ///   · 原版那 14 个调用点**每一处**都先过 `IsTutorialMatch(bm)`，且在
        ///     `CanPlayCard` 那一处还外加 `*(char*)(actingCard + 0x40) != 0`（**玩家侧**才问）
        ///     ⇒ 非教程局 / AI 的卡**一次都不问**；
        ///   · ⛔ **绝不能塞进 `RuleCore.Can*`** —— 那些判定 AI 也在用（`SimpleAI.GetAvailableActions`），
        ///     塞进去会把 AI 一起闸死（脚本驱动的那些动作本来就不该被闸）。
        ///     原版也挂在 `BattleManager`（UI 侧），不在 `CardScript`。
        /// 返回 **true = 放行**。非教程局**恒 true**（与加这一句之前逐字等价）。
        /// </summary>
        public bool TutorialPermits(TutorialAttempt attempt)
        {
            var tut = Ctx != null ? Ctx.Tutorial : null;
            if (tut == null) return true;
            return tut.PermitsPlayerAction(attempt);
        }

        /// <summary>闸门拒了 ⇒ **出声**（原版 `BattleManager` 那 14 处 → `BattleTipController.NotifyCantDoAction`
        /// → 督军说一句「我不能这么做」；我们走同一条 `SpeakCantDo`）。
        /// ⚠️ 另加一条 `Debug.Log`：批处理 / 自检里 `VoiceLines.Ready` 常常是假，
        ///   `SpeakCantDo` 会**静默**什么都不做 —— 那条日志是「它真的拦了」的凭据。</summary>
        void RejectByTutorial(string what)
        {
            Debug.Log($"[Tutorial] 闸门拦下：{what} —— 这一回合的脚本只允许它写的那一步"
                    + $"（原版 `CheckIfPlayerActionPermittedInTutorial`，第 {Ctx.Tutorial.Turn} 回合第 "
                    + $"{Ctx.Tutorial.ActionCounter} 条）");
            SpeakCantDo();
        }

        // ---- 闸门输入端：把「玩家正想做的那件事」翻成 `TutorialAttempt`（原版那 14 处的实参）----

        /// <summary>手上第 `handIdx` 张的「发起者」三个字段（原版 `CanPlayCard` 的第 2 个实参就是它）。</summary>
        void TutActingOfHand(int handIdx, out string id, out bool isHero)
        {
            id = null; isHero = false;
            var hand = Ctx.Players[_me].Hand;
            if (handIdx < 0 || handIdx >= hand.Count || hand[handIdx] == null) return;
            var c = hand[handIdx].Card;
            if (c == null) return;
            id = c.Id; isHero = c.Type == "hero";
        }

        /// <summary>场上某一格的「发起者」/「目标」三个字段。</summary>
        void TutFill(BattleContext ctx, int side, int slot, bool asTarget, ref TutorialAttempt a)
        {
            var u = (ctx != null && BoardSpec.IsValid(slot)) ? ctx.Players[side].Board[slot] : null;
            if (u == null || u.Card == null) return;
            if (asTarget)
            {
                a.TargetId = u.Card.Id; a.TargetIsHero = u.IsWarlord;
                a.TargetIsPlayerSide = side == _me;
            }
            else
            {
                a.ActingId = u.Card.Id; a.ActingIsHero = u.IsWarlord;
                a.ActingIsPlayerSide = side == _me;
            }
        }

        /// <summary>出牌：原版 `CanPlayCard(bm, 手牌那张, …, targetPosition)` 的实参。
        /// ⚠️ **目标卡传的是 null**（原版那一处的第 4 个实参就是 `0`）⇒ `NoTargetYet = true`；
        ///    但 `TargetSlot` 照传 —— 落点那一条 left/not-left 是**第 ① 步**里判的（不是目标那一关）✓。</summary>
        TutorialAttempt TutAttemptPlay(int handIdx, int slot)
        {
            // 🔴 **`TargetSlot` 传 `0`**（= 原版的「未指定」哨兵），⛔ 不传 hover 槽号 ——
            //   审查 K6/R7 实测：原版 `BattleManager__CanPlayCard.c:56-59` 把 `targetPosition` 那个实参
            //   **掩码成 0**（`in_stack_..b8 &= 0xffffffff00000000`）传进闸门，而
            //   `CheckIfMatchesActionData` 的 left/right 判断要求 `param_5 != 0`（`:34`）
            //   ⇒ **原版在 14 个调用点上从不判左右**（`PlayerMinionLeft(31)` 那 6 条 + `NotLeft(32)` 那 1 条，共 **7 条**）。
            //   我们原来把 hover 槽号传了进去 = **比原版严** ⇒ 现在照原版放宽。
            //   ⚠️ 左/右那一条**不是丢掉**：它挪到第 ② 点（`TutAttemptPlayResolve`，**我们额外加的那一处**），
            //     那里 `slot` 是**真的落点**、目标也认得出来 —— 如实标：**那一处比原版严，是我们自己的**。
            var a = new TutorialAttempt { Action = BattleActionType.playCardFromHand, ActingIsPlayerSide = true,
                                          TargetSlot = 0, NoTargetYet = true };
            string id; bool hero;
            TutActingOfHand(handIdx, out id, out hero);
            a.ActingId = id; a.ActingIsHero = hero;
            return a;
        }

        /// <summary>出牌**真的落地那一刻**的实参（第 ② 点，= 原版 `IsValidSpellTarget` 那一处的形状）。
        /// 🔴 与第 ① 点（`CanPlayCard`，那个查询口）差别有**两处**：
        ///   ① **战术卡的目标此刻是知道的**（落点就是「效果打谁」）⇒ 目标那一关**要判**
        ///      （⛔ 单位卡不判目标 —— 原版那两处传的都是 null）；
        ///   ② **`TargetSlot` 传真的落点** ⇒ 脚本里 `PlayerMinionLeft(31)`/`NotLeft(32)` 那 7 条的左右判断
        ///      在这一处生效（⚠️ **原版没有这一处** —— 这是我们的额外一道，**如实标「比原版严」**）。</summary>
        TutorialAttempt TutAttemptPlayResolve(CardView card, int handIdx, int slot)
        {
            var a = TutAttemptPlay(handIdx, slot);
            if (!BoardSpec.IsValid(slot)) return a;
            a.TargetSlot = slot;                       // ← 见上面 ②（第 ① 点照原版传 0）
            bool tactic = card != null && !card.Data.isUnit;
            if (!tactic) return a;
            var mine = Ctx.Players[_me].Board[slot];
            var foe = Ctx.Players[1 - _me].Board[slot];
            if (foe != null) { TutFill(Ctx, 1 - _me, slot, true, ref a); a.NoTargetYet = false; }
            else if (mine != null) { TutFill(Ctx, _me, slot, true, ref a); a.NoTargetYet = false; }
            // 空格（不需要点目标的那种战术）⇒ 目标那一关回到原版 null 那一档
            return a;
        }

        /// <summary>「这一格能不能发起攻击 / 放技能」——原版 `CanAttackCard` / `CanUseActiveAbility` 的实参。
        /// 🔴 **这一处还不知道打谁** ⇒ `NoTargetYet = true`（原版那两处传的 `targetCard` 就是 null）。
        /// ⛔ 别把它当成 `false`：那样「脚本写的是打督军、玩家去点别处」会被**提前挡在开选择器那一步**，
        ///    而脚本那一步就**永远做不出来了**（`playerAction=true` 的那一条等不到 ⇒ 整关卡死）。</summary>
        TutorialAttempt TutAttemptAction(int slot, BattleActionType kind)
        {
            var a = new TutorialAttempt { Action = kind, NoTargetYet = true };
            TutFill(Ctx, _me, slot, false, ref a);
            return a;
        }

        /// <summary>「点某一格当目标 / 点亮合法目标」——原版 `IsValid*Target` / `CanShowPotentialTargets` 的实参。
        /// 🔴 这一处**目标已知** ⇒ `NoTargetYet = false`（目标那一关**要判**，这是脚本「打哪一只」的落点）。</summary>
        TutorialAttempt TutAttemptTarget(int side, int slot)
        {
            var a = new TutorialAttempt { Action = _command == AttackKind.Ability
                                                     ? BattleActionType.playActiveAbility
                                                     : BattleActionType.attack,
                                          TargetSlot = slot };
            TutFill(Ctx, _me, _selectedSlot, false, ref a);
            TutFill(Ctx, side, slot, true, ref a);
            return a;
        }

        /// <summary>结束回合 —— 原版 `EndTurnClick` 的实参。</summary>
        TutorialAttempt TutAttemptEndTurn()
        {
            return new TutorialAttempt { Action = BattleActionType.endTurn };
        }

        /// <summary>「真的把这一手打出去」—— 原版 `AllowResolveAttack` / `UnitOnBoardAttackTypeSelector__AttackButtonClick`
        /// 的实参。`targetSlot &lt; 0`（这一手不用点目标 / 还没点）= 原版传 null `targetCard` ⇒ `NoTargetYet = true`。</summary>
        TutorialAttempt TutAttemptResolve(AttackKind kind, int slot, int targetSlot)
        {
            var a = new TutorialAttempt
            {
                Action = kind == AttackKind.Ability ? BattleActionType.playActiveAbility : BattleActionType.attack,
                TargetSlot = targetSlot,
                NoTargetYet = targetSlot < 0,
            };
            TutFill(Ctx, _me, slot, false, ref a);
            if (targetSlot >= 0) TutFill(Ctx, 1 - _me, targetSlot, true, ref a);
            return a;
        }

        /// <summary>收灵魂石 —— 原版 `CanUseWaystone` 的实参（对应 `BattleActionType.useWaystone = 76`，
        /// 而 `Matches` 里只有 `TapCard(120)` 那一档认它）。</summary>
        TutorialAttempt TutAttemptWaystone(int slot)
        {
            var a = new TutorialAttempt { Action = BattleActionType.useWaystone };
            TutFill(Ctx, _me, slot, false, ref a);
            return a;
        }

        // ==================================================================
        //  🆕 2026-10-18（A938）：**第三条记账口** —— 脚本动作的接收端
        // ==================================================================
        //
        //  判据 → `CardPresentation/Battle/ReplayStore.cs` 文件头那张「每一处会动引擎的地方都要记」的清单
        //  第 ⑧ 条。挂法照 `SimpleAI.Executed`（`Begin` 里装、`DetachStaticHooks` 里摘）。
        //  ⚠️ 两个**伪 kind**（与 `RecKindForfeit` 同族）：`DrawCard` / `ChangeTo*` 在 `AiActionKind`
        //     里没有对应项（`NetApply` 也不认），只能走 `RecRaw` + `PlayReplay` 里单独一支。
        //     `PlayCard` / `Attack` **能**翻成 `AiAction` ⇒ 走正常那条（`NetApply.Apply` 认得，
        //     联机重连那条路也照旧）。
        /// <summary>录像里的伪 kind：脚本抽一张（`actor` = 抽哪一侧）。</summary>
        const int RecKindTutorialDraw = 201;
        /// <summary>录像里的伪 kind：脚本改攻击型（`marks = {格号, 1|2}`，`actor` = 哪一侧）。</summary>
        const int RecKindTutorialAttackType = 202;

        void OnTutorialScriptExecuted(BattleContext ctx, TutorialAction a, ScriptedActionDone done, bool ok)
        {
            if (ctx == null || ctx != Ctx) return;
            // 🔴 🆕 2026-10-18（A940）：**表现那一半不吃 `ok`** —— `SmallTip` / `ActivateHandCards` /
            //   高亮那几档在引擎侧**本来就是 no-op**（`ok = false`），可它们的表现**必须做**。
            //   ⚠️ 顺序：先表现、后记账（记账两半互不影响，但表现里会读 `BattleContext` 的当前态）。
            ApplyTutorialActionView(a, done, false);
            if (_rec == null || !ok) return;   // 没在录 / 演出档 ⇒ 不记
            switch (done.Kind)
            {
                case ScriptedActionType.DrawCard:
                    RecRaw(RecKindTutorialDraw, done.Seat, null);
                    break;
                case ScriptedActionType.ChangeToRanged:
                case ScriptedActionType.ChangeToMelee:
                    RecRaw(RecKindTutorialAttackType, done.Seat, new[] { done.Slot, done.AttackType });
                    break;
                case ScriptedActionType.PlayCard:
                    RecAct(new AiAction { Kind = AiActionKind.PlayCard, HandIdx = done.HandIdx, Slot = done.Slot },
                           done.Seat, _tutPicks, _tutPickIds);
                    break;
                case ScriptedActionType.Attack:
                case ScriptedActionType.AttackFreeMode:
                    RecAct(new AiAction { Kind = done.Ranged ? AiActionKind.AttackRanged : AiActionKind.AttackMelee,
                                          Slot = done.Slot, TargetP = done.TargetP, TargetSlot = done.TargetSlot,
                                          Ranged = done.Ranged },
                           done.Seat, _tutPicks, _tutPickIds);
                    break;
            }
        }

        /// <summary>把一条脚本伪 kind 落回引擎（`PlayReplay` 用）。返回 true = 认得这个 kind。</summary>
        static bool ApplyTutorialRaw(BattleContext ctx, MsgAction m)
        {
            if (m.kind == RecKindTutorialDraw) { RuleCore.Draw(ctx, m.actor); return true; }
            if (m.kind == RecKindTutorialAttackType && m.marks != null && m.marks.Length >= 2)
            { RuleCore.SetCurrentAttackType(ctx, m.actor, m.marks[0], m.marks[1]); return true; }
            return false;
        }

        /// <summary>自检用：`Update` 那条回合驱动里「脚本这一步」的入口（不走 `cam` 那道早退）。</summary>
        public TutorialStep DriveTutorialScriptForTest() { return DriveTutorialScript(); }

        /// <summary>自检用：把「玩家做完了」那一跳暴露出来（原版 `FinishResolvingAction` 那一半）。</summary>
        public void ContinueTutorialScriptForTest() { ContinueTutorialScript(); }

        /// <summary>自检用：闸门（同一个口，生产路径也走它）。</summary>
        public bool TutorialPermitsForTest(TutorialAttempt attempt) { return TutorialPermits(attempt); }

        /// <summary>自检用：这一局是不是**回放局**（那一道闩同时管「记账」与「教程执行器自驱」两件事）。</summary>
        public bool ReplaySessionForTest { get { return _replaySession; } }

        /// <summary>🆕 2026-10-18（审查 · 交件 §9·3）：**上一次 `PlayReplay` 的逐条轨迹首处分叉的条号**
        /// （`-1` = 一条都没分叉）。它是**失败位** —— 自检直接断它，别只看终局 `NetProtocol.StateHash`
        /// （那个只数张数，分叉了也可能相等）。</summary>
        public int ReplayDivergenceForTest { get { return _replayDivergence; } }
        int _replayDivergence = -1;


        // ---- 对手回合 ----

        // ── AI 演出：准星自己滑到目标上（原版 `BattleManager.EnemyTargetingAnim`）──────────────
        //  判据（2026-09-29 逐句读过 `BattleManager__EnemyTargetingAnim.c`，全文 61 行）：
        //   ① 起点 = **施法者的世界位置**（存进 `pointerCursorStart`，+0x320）。
        //   ② 终点 = 目标的 2D 位置（`Get2DPosOfCard(target, useEffectAnchor: 1)`：目标卡或它的
        //      `EffectAnchor` → 屏幕 → UI 空间）。
        //   ③ `ToggleCrosshair(true, instant:false)` ⇒ **淡入**（我们 `Show` 那套）。
        //   ④ `PrepareMovement(起点, attackType)` ⇒ 准星**瞬移**到起点（`set_position`，**无补间**），
        //      同时设弧线点与配色（`…__PrepareMovement.c:16-37`）。
        //   ⑤ `DoCrosshairMove(终点, VarsGlobal.targettingAnimTime = 0.5)` ⇒ `DOMove`，
        //      **不带 `SetEase`** ⇒ DOTween 默认缓动（`…__DoCrosshairMove.c:21-28`；全库没人改过
        //      `DOTween.defaultEaseType`）。全程**没有**音效 / 相机 / 卡动作 / 等待。
        //   ⑥ 期间 `OnUpdate` 每帧重画拖尾（`AutoMovingCrosshair`：从起点连到准星**当前**位置）。
        //   ⑦ 调用方：演出 → `WaitForSeconds(0.5)` → 结算 —— **补间与等待并行**，准星滑到位与结算同一刻。
        //  🔴 **只有 AI 那一侧有这段**：三个调用点的守卫都是「施法方 `isPlayer == false`」
        //    （`…_ResolvePlayActiveAbility_d__479:524` · `…_ResolvePlayCardFromHand_d__447:482` ·
        //     `…_ResolveAttack_d__438:823,1476`）。玩家那条路是每帧直接给位置、没有补间。
        //  ⚠️ 收尾那个 `ToggleCrosshairOff`（演完立刻灭）**属推断** —— 两个回调的方法指针在 `.c` 里
        //    解不出名字，只知道它们是该类仅有的两个无参方法。照它实现（否则准星会一直亮着）。
        AiAction _aiAnimAct;                       // 正在为哪条动作做演出（null = 没在演）
        Vector3 _aiAnimFrom, _aiAnimTo;
        AttackKind _aiAnimKind;
        float _aiAnimT;

        /// <summary>这条动作值不值得先演一下准星？原版三个调用点 = 主动技能 / 从手牌打出带目标 / 攻击；
        /// 共同守卫是**受击方是真人**（`attacker.isPlayer==0 &amp;&amp; IsAgainstHuman()`）。</summary>
        bool WantsTargetingAnim(AiAction act)
        {
            if (act == null || act.TargetSlot < 0 || act.TargetP < 0) return false;
            if (act.TargetP == _me) return true;        // 目标在真人那一侧 —— 正是原版那条守卫
            return false;
        }

        /// <summary>起演（原版 `BattleManager.EnemyTargetingAnim` 逐句照做）。返回 true = **本帧别执行**，
        /// 演完由 <see cref="FinishAiTargetingAnim"/> 执行同一条动作。</summary>
        bool StartAiTargetingAnim(AiAction act)
        {
            // ⚠️ 只在真机演：批处理**没有帧循环**，这个演出靠 `AdvanceTimeline` 泵 —— 泵不动就永远演不完
            //    ⇒ 会把 AI 回合**卡死**。自检与 `-wfdrive` 走的是 `SimulateAiTurn`（`SimpleAI.PlayTurn`），
            //    本来就不经过这里，所以关掉它不影响任何一条自检。
            if (!animateFeel || reticle == null || !WantsTargetingAnim(act)) return false;

            var target = ViewAt(act.TargetP, act.TargetSlot);
            if (target == null) return false;
            // 施法者：原版「从手牌打出」那条取的是**该方的督军**（`GetHero()`），其余取行动单位
            CardView caster = act.Kind == AiActionKind.PlayCard
                            ? ViewAt(1 - _me, BoardSpec.WarlordSlot)
                            : ViewAt(1 - _me, act.Slot);
            if (caster == null) return false;

            AttackKind kind = act.Kind == AiActionKind.AttackMelee ? AttackKind.Melee
                            : act.Kind == AiActionKind.AttackRanged ? AttackKind.Ranged
                            : AttackKind.Ability;

            SyncReticleCameras();
            _aiAnimFrom = caster.transform.position;
            _aiAnimFrom.z = TargetReticle.Z;
            _aiAnimTo = reticle.ResolveAim(target.transform.position);
            _aiAnimTo.z = TargetReticle.Z;
            _aiAnimKind = kind;
            _aiAnimT = 0f;
            _aiAnimAct = act;
            // 原版 `PrepareMovement`：**先瞬移到起点**（弧线这时退化成一个点，与原版一致）
            reticle.Show(_aiAnimFrom, _aiAnimFrom, kind);
            return true;
        }

        /// <summary>演完 → 灭准星 → **执行那条动作**（原版：演出 → 等 0.5 s → 结算）。</summary>
        void FinishAiTargetingAnim()
        {
            var act = _aiAnimAct;
            _aiAnimAct = null;
            if (reticle != null) reticle.Hide();
            if (act == null) return;

            if (AfterAiAction(act, SimpleAI.ExecuteAction(Ctx, act))) return;
            EndTurnAndAdvance(1 - _me);
            ResetClock();
            RefreshAll();
        }

        /// <summary>每帧推进（挂在 `AdvanceTimeline` 这个泵上 —— 同 `TickTargetingLean` 那条理由）。</summary>
        void TickAiTargetingAnim(float dt)
        {
            if (_aiAnimAct == null) return;
            _aiAnimT += dt;
            float dur = CardFeel.EnemyTargetingAnimTime;
            float p = dur <= 0f ? 1f : Mathf.Clamp01(_aiAnimT / dur);
            // 原版 `DoCrosshairMove` **没有 `SetEase`** ⇒ DOTween 默认缓动 = OutQuad（不是线性！）
            float e = 1f - (1f - p) * (1f - p);
            if (reticle != null)
                reticle.AnimTo(_aiAnimFrom, Vector3.Lerp(_aiAnimFrom, _aiAnimTo, e), _aiAnimKind);
            if (p >= 1f) FinishAiTargetingAnim();
        }

        void DriveAiTurn()
        {
            // 🔴 **2026-10-18（A938）：教程局里这一回合有脚本 ⇒ 脚本接管整个 AI 回合。**
            //   判据 = `_PlayAi_d__399__MoveNext.c:68-151`：只要 `GetCurrentTurnScriptedData` 非空，
            //   `_PlayAi` 走的就是 `PlayScriptedTurn` 那一支（**不会**去调 `AI.PlayTurn`），
            //   脚本跑完之后落到收尾那条 `AddEndTurnAction`（`case 8`）—— 也就是**交回合**。
            //   ⚠️ 「等玩家」这一档在 AI 回合里本不该出现（数据实测：110 条 `playerAction=true`
            //     的 `acting` 全是玩家侧，没有一条落在 AI 的回合里）⇒ 真撞上就**出声**，
            //     ⛔ 不静默、也不原地等（那会把这一局挂死）。
            if (TutorialOwnsThisTurn)
            {
                _aiTimer -= Time.deltaTime;
                if (_aiTimer > 0f) return;
                _aiTimer = aiStepDelay;
                var step = DriveTutorialScript();
                if (step == TutorialStep.Advanced) return;
                // ⚠️ `NotOwned` 在这儿只有一个来源：**回放局**（`_replaySession` ⇒ 执行器不许自驱，
                //    脚本动作已经在动作流里了）⇒ 这一帧**什么都不做**，⛔ 别当成「脚本跑完了」去收回合
                //    （那会把回放推到下一回合、把整条动作流错位）。
                if (step == TutorialStep.NotOwned) return;
                if (step == TutorialStep.WaitingForActor)
                    Debug.LogWarning("[Tutorial] 这一条脚本动作要「玩家自己做」，可现在是 **AI 的回合**"
                        + "（第 " + Ctx.Tutorial.Turn + " 回合第 " + Ctx.Tutorial.ActionCounter + " 条）——"
                        + " 等一下玩家也等不到（他这会儿点不了）。按收尾处理：**结束 AI 回合**。"
                        + " 数据实测这一档本不该出现，出现了就是数据或接线出了问题。");
                EndTurnAndAdvance(1 - _me);      // 脚本跑完 / 这一条没法做 ⇒ 收尾（原版 `case 8`）
                ResetClock();
                RefreshAll();
                return;
            }

            _aiTimer -= Time.deltaTime;
            if (_aiTimer > 0f) return;
            _aiTimer = aiStepDelay;

            // 🆕 演出在演 ⇒ 这一步不挑动作（演完 `TickAiTargetingAnim` 会把那条动作执行掉）
            if (_aiAnimAct != null) return;

            // **一步 = 一条动作**（原版 `AI.PlayTurn` 就是「一次调用做一步」，循环在 BattleManager 的协程里）。
            // 挑分最高的那条 → 执行 → 下一帧再挑。
            //
            // 🔴 **2026-09-17 重写**：老版本把顺序写死成「先出牌（出到没得出）→ 再技能 → 再攻击」。
            //    原版**没有这个顺序** —— 出牌 / 攻击 / 技能都在**同一张动作表**上按分挑，
            //    所以「够斩杀了就直接打脸」「该先解场而不是先把牌出完」这类都自然成立。
            AiAction act;
            if (SimpleAI.NextAction(Ctx, aiDifficulty, _aiRejected, out act))
            {
                // 🆕 2026-09-29（原版 `BattleManager.EnemyTargetingAnim`）：**打出去之前先演一下准星**
                if (StartAiTargetingAnim(act)) return;
                if (AfterAiAction(act, SimpleAI.ExecuteAction(Ctx, act))) return;
            }

            // 没动作可做（或刚被拒）→ 交给玩家
            EndTurnAndAdvance(1 - _me);
            ResetClock();                     // 又轮到玩家 → 把表拨回去
            RefreshAll();
        }

        /// <summary>一条 AI 动作执行完之后该做什么（排除名单 / 步数上限 / 交回合）。
        /// **两条入口共用**：`DriveAiTurn` 直接执行那一条，与演出演完那条
        /// （<see cref="FinishAiTargetingAnim"/>）—— 两处各写一份迟早不一致，
        /// 而 `_aiSteps` 那个上限正是「AI 跑飞」的守门员。
        /// 返回 **true = 保持 AI 回合**（下一帧接着挑），false = 调用方落到「交回合」那三行。</summary>
        bool AfterAiAction(AiAction act, bool ok)
        {
            if (ok)
            {
                _aiRejected.Clear();        // 走成一条 ⇒ 排除名单清空（下一步重新挑最好的）
                if (++_aiSteps > AiStepLimit)
                {
                    Debug.LogWarning($"[Battle] AI 这回合已经走了 {_aiSteps} 步，超过上限 {AiStepLimit} "
                                   + "—— 收手结束回合（疑似有动作执行成功但状态没变）");
                    return false;
                }
                RefreshAll();       // 特效由引擎事件带出来（`PlaySignals`）
                // 🆕 2026-10-17（A904）：AI 那条动作也要清账 —— **它正是「下一个 ask 点」里最危险的那个**
                //   （面包板上的答案会被 AI 的动作吃掉，而那种错位不报错）。
                ReportUnaskedChoices();
                return true;
            }
            // 🆕 2026-09-29（Q6 后半段）：**不再一拒就收手** —— 把这条记进排除名单，
            // 下一帧挑**次优**的那条（原版是直接 `break`，用户 2026-09-29 点名要改）。
            // 连着被拒 `MaxRejectedActions` 条才收手（并且**如实报出来**，红线：不许静默失败）。
            _aiRejected.Add(act);
            if (_aiRejected.Count < SimpleAI.MaxRejectedActions)
            {
                Debug.LogWarning($"[Battle] AI 的动作被引擎拒绝（第 {_aiRejected.Count} 条，"
                               + "下一步挑次优重试）：" + act);
                return true;                // 保持在 AI 回合，下一帧接着挑
            }
            Debug.LogWarning($"[Battle] AI 连着被拒 {_aiRejected.Count} 条动作"
                           + $"（上限 {SimpleAI.MaxRejectedActions}）—— 收手结束回合");
            return false;
        }

        /// <summary>结束 `seat` 的回合，并把下一位的开局推起来（`BeginTurn` 才是发能量/抽牌那一步）。
        ///
        /// 🔴 **「回合推进」这条录像只在这一个方法里记** —— 它有**两个调用点**
        /// （产品的 `DriveAiTurn` 与自检的 `SimulateAiTurn`），**两处都不走 `LocalAct`**。
        /// 散在调用点各记一次，早晚漏一处；漏了的表现是：**回放停在那一步不动**（而录制时一切正常）。
        /// ⚠️ 玩家的结束回合**不走这里**（它另外还要重置时钟、AI 计时器），在那边单独记 ✓。</summary>
        void EndTurnAndAdvance(int seat)
        {
            // ⚠️ **先落地、后记账** —— `trace` 里那一条要是「这条动作做完之后」的状态，回放才逐条对得上。
            RuleCore.EndTurn(Ctx);
            RuleCore.BeginTurn(Ctx);
            SyncTutorialTurn();           // 🆕 A938：回合变了 ⇒ 脚本指针归零（原版 `UpdateTurn`）
            // 🆕 2026-10-17（A904）：回合推进**同样会结算到 ask 点**（回合末/回合初的触发）
            //   ⇒ 这个口也要清账，否则答案会活到下一个人的动作里去。
            ReportUnaskedChoices();
            RecAct(new AiAction { Kind = AiActionKind.EndTurn }, seat);
        }

        /// <summary>🆕 2026-09-27（录像）：`SimpleAI` 每执行完一条动作调一次 —— 记 AI 走的那一步。
        /// ⚠️ **演员认 `ctx.Active`**（执行那一刻的行动方），**不是**写死 `1 - _me` ——
        /// 自检里有一条「两边都由 AI 代走」的路（`SimpleAI.PlayTurn` 按 `ctx.Active` 走），
        /// 写死座位会把玩家的动作记成对面的 ⇒ 回放演成另一局。
        /// ⚠️ **不会与 `LocalAct` 那条重复记**：玩家的动作走 `RuleCore.*` 直调（不过 `ExecuteAction`），
        /// 而这条只接 `ExecuteAction` 的出口。
        /// ⚠️ 记的时机是**执行之后**（`ok == true` 才记）—— 被引擎拒的动作本来就没发生。</summary>
        void OnAiExecuted(BattleContext ctx, AiAction act, bool ok)
        {
            if (!ok || _rec == null || ctx == null || ctx != Ctx) return;
            // 🔴 **答案从动作上那个「执行前快照」拿**，别在这儿读 `ctx.ChoosePicks` ——
            //    引擎结算时已经把它吃空了，读到的是空 ⇒ 录下来是空 ⇒ 回放改走 `Rng` 兜底。
            RecAct(act, ctx.Active, act.CapturedPicks, act.CapturedPickIds);
        }

        // ==================================================================
        //  视图同步
        // ==================================================================

        // ==================================================================
        //  🆕 2026-10-12（A175）：`Auto Zoom` 的消费者 —— 「接上」这三跳
        // ==================================================================

        /// <summary>建/接上那件消费者（`Begin` 里、战场那一段之后调一次）。判据 → <see cref="_autoZoom"/> 的注释。</summary>
        void SetupAutoZoom()
        {
            if (boardCam == null)
            {
                // 没有 3D 战场相机 ⇒ 缩放无处可落。**出声**（不许静默失败），但只说一次。
                if (!_autoZoomNoCameraNoted)
                {
                    _autoZoomNoCameraNoted = true;
                    Debug.Log("[Battle] 没有 3D 战场相机（`boardCam == null`，退回烘图那一档）"
                            + " ⇒ 不建 `CombatAutoZoom`：**本局 `Auto Zoom` 那一格没有效果**（原版那台相机是场景里的 `BoardCamera`）");
                }
                return;
            }
            if (_autoZoom == null)
            {
                _autoZoom = gameObject.AddComponent<CardPresentation.CombatAutoZoom>();
                Debug.Log("[Battle] 🆕 `Auto Zoom` 的消费者建出来了（原版 `CombatAutoZoom`；"
                        + "落点说明见 `BattleDriver._autoZoom` 那段注释）");
            }
            _autoZoom.boardCamera = boardCam;
            _autoZoom.ResetForBattle();                    // 新一局 = 新场景那一档（状态回出厂、取景写回不缩放）
            // 🆕 **2026-10-12（A423）**：把原版 `BattleHud.ToggleResetAutoCameraZoom(bool)` 那一条**接上**
            //   （= `CombatAutoZoom.ToggleResetCameraZoomUi` 的落点，它再转发给 `CombatCameraZoom` 那三处调用点）。
            //   🔴 **必须接**：不接的话那三处只会「第一次出声一次」—— 那种静默失败是工程红线；
            //   接上之后「玩家一动镜头 ⇒ HUD 那颗重置钮出现」这条链才真的成立。
            _autoZoom.ToggleResetCameraZoomUi = ToggleCameraResetButton;
            // 新一局 = 原版新场景 ⇒ 那颗钮回到 `BattleHud.Initialize` 那一档（**关着**）。
            //   `ResetForBattle` 也刚把 `manualCamera` 清成 false ⇒ 与「玩家还没动过镜头」自洽。
            ToggleCameraResetButton(false);
            // 🔴 **显式挂事件**（不赌 `OnEnable` 在批处理下跑不跑 —— 判据与理由见 `CombatAutoZoom.AttachMinionEvent`）。
            //    幂等：`OnEnable` 真跑过也只是重复调一次。
            _autoZoom.AttachMinionEvent();
            _autoZoomSeen[0] = _autoZoomSeen[1] = 0;       // 与原版新场景里的零初始化一致（见字段注释）
        }

        /// <summary>= 原版 `BattleManager._FinishMulliganFinalPhase` 里那一句 `CombatAutoZoom.Initialize()`
        /// （换牌结束 / 真开打那一刻）。两条开局路径各叫一次，`Initialize()` 自己幂等。</summary>
        void InitializeAutoZoom()
        {
            if (_autoZoom != null) _autoZoom.Initialize();
        }

        /// <summary>把「某一侧场上几个人」喂给消费者 —— **原版 `MinionManager.RefreshOccupationSlots.c` 的等价物**
        /// （那是全反编译里 `MinionManager.OnMinionAddedOrRemoved` 的**唯一 Invoke 点**）。
        /// <para>原版那一段逐句：先把两半的占位表刷完，再 `iVar6 = 右半.Count; iVar7 = 左半.Count;
        /// if (iVar6 &lt;= iVar7) iVar6 = iVar7;` ⇒ 载荷 = **较忙那一半的人数**（不是总人数、也不含督军），
        /// 第二个实参 = 「这个 `MinionManager` 是不是本地玩家那一个」（`Object.op_Equality(manager+0xe0, this)`）。
        /// 我们的人数判据 = `BoardSlots.CountOnSide`（全工程唯一那一条；督军格不在两侧里）。</para>
        /// <para>⚠️ **只在人数真的变了才抬** —— 原版那两个字段也是「上次的值」，`RefreshOccupationSlots`
        /// 只在 `InsertMinion`/`RemoveMinion` 时被调 ⇒ 等价。放在 `RefreshAll()` 里而不是钉 9 个棋盘写入点：
        /// 棋盘写入点散在全工程，漏一个就是**静默**（`Core/Aura.cs` 的光环钩子就是为同一个原因才那么列的）。</para></summary>
        void TickAutoZoom()
        {
            if (_autoZoom == null || Ctx == null) return;
            for (int seat = 0; seat < 2; seat++)
            {
                int n = Mathf.Max(BoardSlots.CountOnSide(Ctx.Players[seat], BoardSlots.Left),
                                  BoardSlots.CountOnSide(Ctx.Players[seat], BoardSlots.Right));
                if (n == _autoZoomSeen[seat]) continue;    // 「上次的值」没变 ⇒ 原版也不会抬
                _autoZoomSeen[seat] = n;
                CombatAutoZoom.RaiseMinionAddedOrRemoved(n, seat == _me);
            }
        }

        // ==================================================================
        //  🆕 2026-10-18（A985⑦）：`Battle/Tips/HandFull` —— 「手牌满了，那张牌没进手牌、直接进弃牌堆」
        // ==================================================================
        //
        //  🔴 **原版判据 / 文案口径** → `HandFullTerm` + `HandFullText` 两条 doc（⛔ 别在这儿抄第二份）。
        //
        //  🔴 **我们怎么「看见」这件事**（引擎**没有**给它发事件 —— `RuleCore.EnforceHandLimit` 只 `ctx.Log`，
        //     而 `RuleEngine/**` 不在本件白名单里 ⇒ 加不了 `ctx.Emit`）⇒ 用**实例身份**判：
        //     `EnforceHandLimit` 把 `Hand` 末尾那一份**挪进** `Discard`，而那一份
        //     **从未在任何一次同步里出现在手牌里、也没在棋盘上**（它是在**同一个引擎调用里**
        //     「加进手牌 → 立刻被弃」，两次 `RefreshAll` 之间根本看不见）。
        //     另外三条进弃牌堆的路 —— 战术卡结算（`EffectResolver.PlayTactic`）、单位/残骸阵亡
        //     （`RuleCore` 里单位/残骸阵亡那两处）—— 那一份**都先在手牌或棋盘上出现过**，因此被排除。
        //     再叠一条「**这一侧的手牌确实满着**」（= 原版那个 `Hand.Count < MaxCardsInHand` 的否定），
        //     把残留的误报面（潮涌压在下面的牌、同一结算里造出来又打掉的牌）压到可忽略。
        //     ⚠️ **如实记**：这是**旁证式观测**，不是引擎给的事件。更干净的做法是在
        //     `RuleCore.EnforceHandLimit` 里补一个计数器或事件 —— 那要 `RuleEngine/**` 的所有权。
        //     ⛔ 不许因为「这里绕」就退回「什么都不显示」：那正是本件要修的**静默失败**。
        //
        //  ⚠️ **同族那两条的落点**（本件照它们的**形态**做，都是 `SetHint`）：
        //     `DragToTarget` → `OnCardReturned`（真回调）· `UnitNotReady` → `OpenCommand`（真判据字段）。
        //     本条没有「一个回调就能挂」的位置（抽牌散在六处引擎路径里）⇒ 走同步点时判。

        /// <summary>已经**在手牌里见过**的实例（判据见上面那一段）。</summary>
        readonly HashSet<CardInstance> _handSeen = new HashSet<CardInstance>();
        /// <summary>已经**在棋盘上见过**的实例（同上）。</summary>
        readonly HashSet<CardInstance> _boardSeen = new HashSet<CardInstance>();
        /// <summary>已经**算过账**的弃牌堆条目（`Discard` 是累计列表 ⇒ 只判新增的那些）。</summary>
        readonly HashSet<CardInstance> _discardSeen = new HashSet<CardInstance>();

        /// <summary>换局时清账（`Begin` 里调）。⚠️ 不清的话上一局见过的实例会让这一局的首次判据失效。</summary>
        void ResetHandFullWatch()
        {
            _handSeen.Clear(); _boardSeen.Clear(); _discardSeen.Clear();
        }

        /// <summary>每轮同步点判一次「有没有牌因为手牌满而进不了手牌」（判据与理由见上面那一段）。
        /// ⚠️ **顺序要紧**：① 先把「现在看得见的」收进两张表，② 再判新进弃牌堆的那些 ——
        ///    反过来的话，这一轮**刚打出去/刚登场**的那一份会被当成「从没进过手牌也没上过场」。</summary>
        void WatchHandFull()
        {
            if (Ctx == null || Ctx.Players == null || Ctx.Players[0] == null || Ctx.Players[1] == null) return;
            int limit = Ctx.Vars.handLimit;

            // ① 现在看得见的（手牌 / 棋盘）
            for (int seat = 0; seat < 2; seat++)
            {
                var ps = Ctx.Players[seat];
                if (ps.Hand != null)
                    for (int i = 0; i < ps.Hand.Count; i++)
                        if (ps.Hand[i] != null) _handSeen.Add(ps.Hand[i]);
                if (ps.Board != null)
                    for (int i = 0; i < ps.Board.Length; i++)
                        if (ps.Board[i] != null && ps.Board[i].Instance != null) _boardSeen.Add(ps.Board[i].Instance);
            }

            // ② 新进弃牌堆的那些里，找「从没在手牌/棋盘上出现过」的那一种
            for (int seat = 0; seat < 2; seat++)
            {
                var ps = Ctx.Players[seat];
                if (ps.Discard == null) continue;
                for (int i = 0; i < ps.Discard.Count; i++)
                {
                    var inst = ps.Discard[i];
                    if (inst == null || !_discardSeen.Add(inst)) continue;          // 老条目
                    if (_handSeen.Contains(inst) || _boardSeen.Contains(inst)) continue;   // 手牌/棋盘上见过 ⇒ 不是这条
                    if (ps.Hand.Count < limit) continue;                            // 手牌没满 ⇒ 不可能是「手牌满被弃」
                    SetHint(HandFullText());
                    Debug.Log($"[Battle] 手牌已满 ⇒「{(inst.Card != null ? inst.Card.Name : "?")}」进不了手牌、"
                            + $"直接进弃牌堆（{ps.Name} 手 {ps.Hand.Count}/{limit}）—— 提示行走 `{HandFullTerm}`"
                            + "（原版 `PlayerHand._AddDrawnCardToHand_d__37` 那个 `else` 支）");
                }
            }
        }

        public void RefreshAll()
        {
            // ① 先把引擎**这一轮发生的事**翻译成特效。
            //    ⚠️ 必须赶在同步视图之前 —— 阵亡单位这一格马上就要空了，
            //       事件里带着格位号，趁现在把它变成世界坐标最省事。
            PlaySignals();

            SyncBoard(_me, _myUnits, true);
            SyncBoard(1 - _me, _foeUnits, false);
            SyncHand();
            SyncFoeHand();

            TickAutoZoom();  // 🆕 A175：棋盘同步完就喂人数（原版 `MinionManager.RefreshOccupationSlots`）

            UpdateHud();     // 批处理里没有 Update() 循环，HUD 得在这里刷，不然截图上是旧值

            // 🆕 **2026-10-18（A985⑦）**：`Battle/Tips/HandFull`。排在**最后** —— 它是这一轮**最后写的**
            //    那一句提示（同族另两条是事件驱动、本来就与这个点互斥）。
            WatchHandFull();
        }

        // ==================================================================
        //  特效：引擎事件流 → 特效
        //
        //  以前这里靠**对比同步前后两份战场快照**猜「谁挨打了、谁死了」。
        //  猜得出这两件事，但猜不出「谁发动了技能」「谁触发了效果」——
        //  那两类特效一直接不上，就是因为引擎里没有这两种事件（2026-09-12 补上）。
        //  现在引擎把发生的事全写进 `Ctx.Signals`，这边只做翻译。
        // ==================================================================

        readonly List<BattleEvent> _signalBuf = new List<BattleEvent>();

        /// <summary>自检用：最近一次 drain 里播放过的事件（不接特效也能断言「引擎说了什么」）</summary>
        public readonly List<BattleEvent> LastSignals = new List<BattleEvent>();

        void PlaySignals()
        {
            if (Ctx == null || Ctx.Signals.Count == 0) return;

            _signalBuf.Clear();
            Ctx.DrainSignals(_signalBuf);

            LastSignals.Clear();
            LastSignals.AddRange(_signalBuf);

            // 🆕 2026-10-18（§8b · 2b）：**疲劳 → 错误横幅**。判据（为什么要自己挑、六道闸是什么）
            //   全在 `NoteFatigueBanners` 的 doc 里。⚠️ 放在**排时间线之前**：原版那条提示也在
            //   「扣血之前」（`_ResolveFatigue` 里 `ShowError` → `WaitForSeconds(0.5)` → 才 `DealMultiDamage`）。
            NoteFatigueBanners(_signalBuf);

            // **不立刻全播** —— 按 `EventTiming` 排一条时间线，让动作一段段来。
            // 以前是同一帧把整串一起点着，看着就是「一坨特效」，读不出「谁先谁后」。
            BattleEvent prev = null;
            float t = _clock;
            for (int i = 0; i < _signalBuf.Count; i++)
            {
                var e = _signalBuf[i];
                t += EventTiming.DelayBetween(prev, e);
                _timeline.Add(new PendingSignal { evt = e, at = t });
                t += EventTiming.DurationOf(e);   // 带事件的重载：攻击要分远近两档（见那两个重载的注释）
                prev = e;
            }
            AdvanceTimeline(0f);        // 延迟为 0 的那几条**这一帧**就播，不用等下一帧
        }

        struct PendingSignal { public BattleEvent evt; public float at; }
        readonly List<PendingSignal> _timeline = new List<PendingSignal>();
        float _clock;

        /// <summary>还没播的事件条数（自检用：推到 0 = 这一段动作演完了）</summary>
        public int TimelinePending { get { return _timeline.Count; } }

        /// <summary>事件时间线的当前时刻（秒）。自检按它**细推**到某个事件刚发生的时刻 ——
        /// 补间是刚起步的，一次 `Step(0.45f)` 会把整段弹跳跳过去（踩过，见 `BattleScene` 第 15 节）</summary>
        public float Clock { get { return _clock; } }

        /// <summary>还没播的事件，按到点时刻排好（自检排查用：16 条里到底排了些什么）</summary>
        public string TimelineDump()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < _timeline.Count && i < 24; i++)
            {
                var e = _timeline[i].evt;
                sb.Append("      @").Append(_timeline[i].at.ToString("F2")).Append(' ').Append(e.Kind)
                  .Append(" P").Append(e.Player + 1).Append('@').Append(e.Slot);
                if (e.Kind == EvtKind.Attack) sb.Append(" →P").Append(e.TargetPlayer + 1).Append('@').Append(e.TargetSlot);
                if (!string.IsNullOrEmpty(e.CardId)) sb.Append(' ').Append(e.CardId);
                sb.Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>
        /// 推进事件时间线。Play 模式由 `Update` 每帧推；**批处理没有帧循环**，
        /// 自检里由 `BattleScene.Step()` 显式推。
        /// </summary>
        public void AdvanceTimeline(float dt)
        {
            _clock += dt;

            // 结算面板的「开门」视频也吃这个 dt —— 它和事件时间线一样，
            // 真机靠 `Update`、批处理靠 `BattleScene.Step` 手动推（同一个泵，不另开一条路）
            if (_endPanel != null) _endPanel.Advance(dt);
            // 单位语音条的气泡停留时间也走这个泵（同一个理由）
            if (_unitChat != null) _unitChat.Advance(dt);

            // 🆕 2026-09-29：准星的淡入淡出也走这个泵（原版在 `TargetReticleController.Update` 里，
            //   批处理没有帧循环 ⇒ 不推的话准星会卡在半透明上，截图与断言都不对）。
            if (reticle != null) reticle.TickFade(dt);

            // 🆕 2026-09-29：**瞄准时的「抬手/后撤」**也走这个泵（原版在 `CardScript.Update` 里跑，
            //   只有 `cardState == inPlayAminingAttack` 那一支）。同一条理由：批处理没有帧循环。
            TickTargetingLean(dt);

            // 🆕 2026-09-29：**AI 那条准星演出**也走这个泵（原版靠 `TargetReticleController` 的
            //    `DOMove` 帧循环推进；批处理没有帧循环 ⇒ 不推就永远演不完、AI 回合会**卡死**）。
            TickAiTargetingAnim(dt);

            // 🆕 2026-10-18（A939）：**战后脚本**（`onVictory` 那 13 条聊天/电台）也走这个泵 ——
            //  同 `TickIntroMonologue` 的理由：真机靠 `Update`、批处理靠 `BattleScene.Step` 显式推。
            TickPostBattleScript(dt);

            // 🆕 2026-09-18：**开局独白也走这个泵**。            // 🔴 **不能挂在 `Update` 里** —— 批处理**没有帧循环**，`Update` 根本不跑，
            //    而自检是靠 `BattleScene.Step → AdvanceTimeline` 推的（`Step` 的注释里写着这个坑）。
            //    挂在 `Update` 里的话，真包能跑、**自检永远推不动**，那就是个只在一种环境下活的实现。
            // ⚠️ 必须排在 `_unitChat.Advance` **之后** —— 它判「上一条播完没有」靠的就是气泡的最新状态。
            TickIntroMonologue();

            // 🆕 2026-09-30（§25）：**进攻卡的 reveal 动画**也走这个泵（原版那条协程靠 Unity 的
            //   `yield WaitForSeconds` 自己走；批处理没有帧循环 ⇒ 不推就永远走不完、
            //   `ApplyOffensiveEnvOnce` 一辈子不会被调到）。同 `TickIntroMonologue` 的理由与位置。
            TickOffensiveReveal(dt);

            // 🆕 2026-09-18：`ChatPopup` 的淡入淡出 + `ChatButton` 的 4 秒冷却。
            //    🔴 **和独白同一条理由挂在这里**：批处理没有帧循环，`Update` 不跑，
            //       挂在 `Update` 里就成了「真包能跑、自检永远推不动」的实现。
            if (_chatPopup != null) _chatPopup.Advance(dt);
            if (_chatCooldown > 0f) _chatCooldown = Mathf.Max(0f, _chatCooldown - dt);

            // 🆕 2026-10-18（§8b · 2b）：**错误横幅**（原版 `UI Error Message Controller` 那三条 tween）
            //   也走这个泵 —— 同 `TickOvertime` 的理由：批处理没有帧循环，挂在 `Update` 里就
            //   「真包能跑、自检永远推不动」。三段时长（0.15 / 2.0 / 0.25）在 `ErrorMessageBanner` 里。
            if (_errorBanner != null) _errorBanner.Advance(dt);

            // ⚠️ 取**「到点的事件里最早的那条」**，而不是只看队头：
            //    队头要是排在未来（上一局的残留、或者新一批事件的起始时刻比待播的那条还早），
            //    后面那些**早就该播**的会一直堵着（2026-09-13 撞到过，见 `Begin` 里那段注释）。
            //    同时到点的按「先来先播」——扫的时候用严格小于，所以并列时保留下标小的那条。
            //    条数很少（几十条），线性扫足够。
            int guard = 0;
            while (guard++ < 256)
            {
                int best = -1;
                for (int i = 0; i < _timeline.Count; i++)
                    if (_timeline[i].at <= _clock && (best < 0 || _timeline[i].at < _timeline[best].at))
                        best = i;
                if (best < 0) break;

                var p = _timeline[best];
                _timeline.RemoveAt(best);
                PlaySignal(p.evt);
            }
        }

        /// <summary>
        /// 丢掉还没播的事件。**只给「一口气跑完几百回合」那种脚本推进用** ——
        /// 那种路径下每回合的几十条事件会堆到一起，一次全点着既慢又看不出什么。
        /// </summary>
        public void DropSignals()
        {
            if (Ctx == null) return;
            Ctx.ClearSignals();
            LastSignals.Clear();
            _timeline.Clear();
        }

        /// <summary>
        /// 🆕 2026-09-29（Q 批第 6 条）：**残骸体那三条原版音效**（出现 / 收集 / 被打掉 × 两阵营）。
        /// ⚠️ `public` 是**给自检调**的（批处理里没有帧循环，`PlaySignal` 那条路要走事件泵）——
        ///    自检拿合成事件直接调它，把「哪条事件 → 哪条音效」这层钉住。
        ///
        /// 判据 → `RemnantSfx` 的文件头（六条 cue ↔ 五条 clip 的完整表）。
        /// 🔴 **这里靠的是【棋盘状态】而不是事件字段** —— `BattleEvent` 上**没有**「这是不是残骸」这一项，
        ///    而残骸的 `CardId` 与原卡**是同一个**（引擎里「翻面成残骸」沿用同一个实例）⇒ 光看 CardId 分不出。
        ///    好在状态本身可分：
        ///      · `Death` 且**这一格现在立着残骸** ⇒ 刚**翻面成残骸** ⇒ **出现**
        ///        （引擎 `RuleCore` 那段：`ps.Board[slot] = rem`，事件发出来时棋盘已经换好了）
        ///      · `Death` 且这一格**空了**、而死的那张卡带 `Waystone` / `Remnant` ⇒ 它本来就是残骸、这回真没了 ⇒ **被打掉**
        ///        （同上一段：`IsRemnant` 为真时走 `else` 分支，格子清空、进弃牌堆）
        ///      · `CollectWaystone` ⇒ **收集**（`Slot` = 石头原来那一格）
        /// ⚠️ 阵营判据与 `RemnantPrefabOf` **同一套**（**看关键词，不看阵营** —— 池子里两者等价，但关键词更抗以后加卡）。
        /// ⚠️ **两处都缺一行就是静默少一件**（红线） ⇒ 取不到 clip 时 `RemnantSfx.Play` 会出声。
        /// </summary>
        public void PlayRemnantSfx(BattleEvent e)
        {
            if (e == null || Ctx == null) return;
            if (e.Player < 0 || e.Player > 1) return;

            if (e.Kind == EvtKind.CollectWaystone)
            {
                // 收集：只有灵族有「点石头」这一手（死灵的「收集」是 `reanimate` 效果，不走这个事件）
                RemnantSfx.Play(RemnantSfx.Moment.Collect, true, BoardWorld(e.Player, e.Slot));
                return;
            }
            if (e.Kind != EvtKind.Death) return;

            var board = Ctx.Players[e.Player].Board;
            var now = BoardSpec.IsValid(e.Slot) ? board[e.Slot] : null;

            if (now != null && now.IsRemnant)
            {
                RemnantSfx.Play(RemnantSfx.Moment.ToRemnant, now.Has(KeywordTable.Waystone),
                                BoardWorld(e.Player, e.Slot));
                return;
            }
            if (now == null && !string.IsNullOrEmpty(e.CardId))
            {
                var card = CardDatabase.Find(_pool, e.CardId,
                                             e.Player == _me ? _myFaction : _foeFaction);
                if (card != null && (card.Has(KeywordTable.Waystone) || card.Has(KeywordTable.Remnant)))
                    RemnantSfx.Play(RemnantSfx.Moment.Death, card.Has(KeywordTable.Waystone),
                                    BoardWorld(e.Player, e.Slot));
            }
        }

        /// <summary>某一格的世界坐标（取那块卡体）—— 取不到就返回 null（`RemnantSfx` 会退成 2D）。</summary>
        Vector3? BoardWorld(int side, int slot)
        {
            if (!BoardSpec.IsValid(slot)) return null;
            var v = BoardViewAt(slot, side == _me);
            return v != null ? (Vector3?)v.transform.position : null;
        }

        /// <summary>一条引擎事件 → 一个特效。（事件种类和特效名的对应在 `VfxMap` 里）</summary>
        void PlaySignal(BattleEvent e)
        {
            SpeakFor(e);          // 单位语音条（原版 `Unit Chat`）—— 和特效同一个事件流，不另开一条路
            PlayRemnantSfx(e);    // 🆕 2026-09-29：残骸体那三条原版音效（出现 / 收集 / 被打掉）

            string evt;
            switch (e.Kind)
            {
                case EvtKind.Play:    evt = VfxMap.PlayCard; break;
                case EvtKind.Deploy:  evt = VfxMap.Deploy; break;
                case EvtKind.Attack:  evt = e.Ranged ? VfxMap.AttackRanged : VfxMap.AttackMelee; break;
                // 🆕 2026-09-29：**治疗**走另一件（原版 `BattleAnims.heal` → `Healing_Circles`）。
                // 我们这边没有独立的治疗事件 —— 它就是 `Hit` 且 `Amount < 0`（和 `PlayHitFeel` 里
                // 飘字那个正负号是同一条判据）。
                case EvtKind.Hit:     evt = e.Amount < 0 ? VfxMap.Heal : VfxMap.Hit; break;
                // 🔴 **阵亡没有「通用事件特效」**（2026-10-01 改）—— 原版阵亡那一下**只有**卡位生成的
                //    `Card 3D Death Explosion`（走 `PlayFeel` → `PlayDeathFeel` → `CardFeel.DeathExplosion`）；
                //    这里原来挂的 `VfxMap.Death`（`Explosion Fenrisian Monstrosities`）是**它的替代品**
                //    （那时那件 prefab 不是 addressable、取不到）。两件同时播 = **原版没有的第二下**。
                //    ⇒ 替代品现在只在**退回分支**里播（效果库里真没有那件时，见 `CardFeel.DeathExplosion`）。
                case EvtKind.Death:   evt = null; break;
                case EvtKind.Ability: evt = VfxMap.Ability; break;
                case EvtKind.Trigger: evt = VfxMap.Trigger; break;
                // 🆕 2026-09-15：这四个原来**结构上就播不出来** —— switch 里没有它们的 case，
                //    落到 `default: return`。见 `VfxMap.ByEvent` 里那四行的出处。
                case EvtKind.Return:     evt = VfxMap.Return;     break;
                case EvtKind.GainFaith:  evt = VfxMap.GainFaith;  break;
                case EvtKind.GainSpirit: evt = VfxMap.GainSpirit; break;
                case EvtKind.GainQuest:  evt = VfxMap.GainQuest;  break;
                // 🆕 2026-09-25 收集灵魂石（灵族）：**格位上**那一条（`Slot` = 石头原来那一格）——
                //    ⚠️ 它**不在** `IsResourceUiEvent` 里，走下面普通那条路（按格位取世界坐标）。
                case EvtKind.CollectWaystone: evt = VfxMap.CollectWaystone; break;
                default: return;
            }

            // **手感补间**（和特效同一时刻起，参数在 `CardFeel` 里、逐条有出处）。
            // ⚠️ 必须赶在 `SyncBoard` 之前 —— 阵亡那一格马上就要空了，视图还在的只有现在。
            if (animateFeel) PlayFeel(e);

            // 🆕 2026-10-01：**trait 的 FromCode 粒子**（见 `PlayTraitParticles` 的头注释）。
            // ⚠️ 必须放在下面 `evt == null` 那道 early-return **之前** —— 阵亡那一条也会走这里。
            PlayTraitParticles(e);

            // 这一条事件**只有手感补间、没有通用特效**（目前只有阵亡，见上面那个 case）。
            // ⚠️ 必须在 `PlayFeel` **之后**返回 —— 阵亡的消散/爆散体就是 `PlayFeel` 里起的。
            if (evt == null) return;

            // 🔴 **阵营资源这三件是 UI 特效**（原版台账判 `UI`：「计数 UI 闪光」，挂 HUD 上的资源图标），
            //    而且它们的 `Slot` **本来就是 -1**（`BattleEvent.GainFaith` 的注释：是「给玩家」的）
            //    ⇒ **绝不能走下面那道 `slot < 0 → return`** —— 只加 case 不加这一段的化，
            //      它们还是永远不播（2026-09-15 查实的「结构上播不出来」就是这个）。
            if (IsResourceUiEvent(e.Kind))
            {
                Vector2 at = ResourceIcon01(e.Kind, e.Player == _me);
                _animfxLastEvent = e;
            _animfxCtx = BuildCardContext(e);          // AnimFX 模块要用「为哪两张卡播的」
                CardEffects.FireEvent(evt, LayoutSpace.ToWorld(at.x, at.y),
                                      e.Player == _me ? _myFaction : _foeFaction, e.CardId);
                _animfxCtx = default;
                return;
            }

            // Attack 打在**目标**那一格（原版也是弹着点，不是抬手那一下）；
            // 其余事件都发生在自己那一格
            int owner = e.Kind == EvtKind.Attack ? e.TargetPlayer : e.Player;
            int slot  = e.Kind == EvtKind.Attack ? e.TargetSlot   : e.Slot;
            if (slot < 0) return;                       // 不在场上（比如从牌库直接进弃牌堆）

            var layout = owner == _me ? playerBoard : enemyBoard;
            string faction = owner == _me ? _myFaction : _foeFaction;

            // 濒死的单位已经不在 `_myUnits/_foeUnits` 里了（视图也要等 SyncBoard 才清），
            // 但格位坐标只跟棋盘几何有关 —— 直接问 layout，不依赖视图
            _animfxLastEvent = e;
            _animfxCtx = BuildCardContext(e);              // AnimFX 模块要用「为哪两张卡播的」
            CardEffects.FireEvent(evt, layout.SlotPosition(slot), faction, e.CardId);

            // 🆕 2026-09-29：**登场其实是两件叠加** —— 共享 `CardPrefab.normalSummon`（那张卡淡入，
            //   就是上面的 `VfxMap.Deploy`）**加上**按阵营/逐卡的**召唤法阵**
            //   （原版 `AeldariSummon` → `BlueSummonCircle` · `Tau_Summon` → `Tau_SummonCircle` …；
            //    判据 → `数据/游戏数据/card_vfx_by_card.json` 的 `generic.deploySummonCandidates`）。
            //   我们那条 `VfxMap.Resolve` 一次只回一个名字 ⇒ 法阵在这里**单独补一发**。
            //   ⚠️ 本地只有那 4 个候选，其余阵营的召唤动画**在远端包** ⇒ `SummonCircleOf` 回 null，不放（留白）。
            if (e.Kind == EvtKind.Deploy)
            {
                string sc = VfxMap.SummonCircleOf(faction);
                if (sc != null) CardEffects.Fire(sc, layout.SlotPosition(slot));
            }
            _animfxCtx = default;
        }

        /// <summary>「挂在 HUD 上、不是挂在格位上」的三件资源事件。见 `PlaySignal` 里那段注释。</summary>
        static bool IsResourceUiEvent(EvtKind k)
        {
            return k == EvtKind.GainFaith || k == EvtKind.GainSpirit || k == EvtKind.GainQuest;
        }

        /// <summary>HUD 上那个**资源计数图标**的归一化位置（我方 / 敌方各一套）。
        /// 🔴 坐标**复用 HUD 画图标时用的同一组常量**（`MyFaithX01` 等）—— 别另抄一份，
        ///    否则图标挪了、特效还留在原地（「两处写同一条规则」）。</summary>
        static Vector2 ResourceIcon01(EvtKind k, bool mine)
        {
            if (k == EvtKind.GainFaith)
                return new Vector2(mine ? MyFaithX01 : FoeFaithX01, mine ? MyFaithY01 : FoeFaithY01);
            if (k == EvtKind.GainSpirit)
                return new Vector2(mine ? MyStoneX01 : FoeStoneX01, mine ? MyStoneY01 : FoeStoneY01);
            return new Vector2(mine ? MyQuestX01 : FoeQuestX01, mine ? MyQuestY01 : FoeQuestY01);
        }

        // ==================================================================
        //  手感补间：引擎事件 → 卡自己的动作
        //
        //  和特效是**两回事**：特效是「在哪一格播哪个 prefab」，这里是「那张卡自己怎么动」。
        //  参数全在 `CardFeel` 里，逐条标了出处（原版 clip / UnitTweenSO / 卡预制体字段 /
        //  反编译常量），**3D→2D 的迁移那几处也标了是「我们改的」**。
        //
        //  ⚠️ `animateFeel` 默认关：批处理自检要**当场精确**的坐标（和 `HandLayout.animateRelayout`
        //     同一条规矩），存场景那一路上打开（`BattleScene.BuildAndSaveScene`）。
        // ==================================================================

        /// <summary>打开手感补间。批处理自检里由用例显式打开，见 `BattleScene` 第 13 节</summary>
        public bool animateFeel = false;

        void PlayFeel(BattleEvent e)
        {
            switch (e.Kind)
            {
                case EvtKind.Attack: PlayAttackFeel(e); break;
                case EvtKind.Hit:    PlayHitFeel(e);    break;
                case EvtKind.Death:  PlayDeathFeel(e);  break;
                case EvtKind.Return: PlayReturnFeel(e); break;
                // 🆕 2026-09-29：「这个词条刚触发」⇒ 卡上**那一位徽标脉冲一下**
                // （原版 `CardScript.ActivateTriggerTraitAnim` → `BattleCardUI.HighlightTraitIcon(traitId)`）。
                case EvtKind.Trigger: PlayTraitPulse(e); break;
            }
        }

        /// <summary>刚触发的那个词条 ⇒ 找到它那一位徽标、脉冲一次。
        /// 配对的键 = `BattleEvent.Keyword`（引擎发 `EvtKind.Trigger` 时写的规范键）。
        /// ⚠️ 认不出（那个词条本来就没有图标）**是正常的**，不是错 —— 所以只**每个词条提示一次**，
        ///    免得 `Talent` 那种刷满日志；绝不猜着去脉冲别的一位。</summary>
        void PlayTraitPulse(BattleEvent e)
        {
            if (e.Player < 0) return;
            var v = ViewAt(e.Player, e.Slot);
            if (v == null) return;                    // 视图不在（刚死的 / 刚被挪走的）⇒ 不表演

            // 🆕 2026-09-29：**这个关键词真的触发了** ⇒ 那张「触发类状态框」也演一下。
            //   原版 = `CardScript.ActivateTriggerTraitAnim` → `BattleCardUI.DisplayTriggerAnim`
            //   （全量反编译里**唯一同时调 `HighlightTraitIcon`** 的地方 —— 而「徽标脉动」我们早就有了，
            //    所以这一处原本只缺框那一半）。判据 → `Core/TraitFrames.cs` 文件头。
            var tf = v.GetComponent<TraitFrames>();
            if (tf != null && tf.DisplayTrigger(e.Keyword))
                Debug.Log($"[Battle] 「{e.Keyword}」触发 ⇒ 状态框上场（原版 `DisplayTriggerAnim`）");

            if (v.PulseBadgeByKeyword(e.Keyword)) return;

            string kw = e.Keyword ?? "";
            if (_pulseMissWarned.Add(kw))
                Debug.Log($"[BattleDriver] 「{kw}」触发时卡上没有对应的徽标位 ⇒ 不脉冲（该词条本来就没有图标，属正常）");
        }
        static readonly HashSet<string> _pulseMissWarned = new HashSet<string>();

        // ==================================================================
        //  🆕 2026-10-01：trait 的「**从代码播**」粒子
        //     （原版 `CardScript.ActivateTraitParticlesFromCode` / `…InTarget`）
        // ==================================================================
        //  判据（8 个调用点与 trait id 都从指令流实读）→ `资料/待办判据_战场与战斗视图.md` 的「trait 粒子」段；
        //  绑定表与「和 `TraitFrames` 那两本字典不是一条链」的说明 → `Core/TraitParticles.cs` 的文件头。
        //  🔴 **触发口 = 「事件 → 该查哪几个 trait」**（原版是在那几个方法里**点名调**的，我们这边用事件流近似）：
        //    · `EvtKind.Ability` ← `UsedActiveAbility`（trait id `0x4f1` = ferocity）
        //    · `EvtKind.Attack`  ← `_ResolveAttack` 里连调的四个（sniper / markerlight / longrange / stomp）
        //    · `EvtKind.Trigger` ← 关键词真的触发（`swarm` / `synapse` 走这一格）
        //  ⚠️ **两个已知的覆盖缺口**（如实记，别当「做完了」）：
        //    ① `swarm` 合并那一下**不发 `EvtKind.Trigger`**（`RuleCore.TrySwarmMerge` 走的是
        //       `BroadcastKeywordEvent` —— 那是发给「写有效果的监听者」的，不会给自己发）⇒
        //       **卡面没有触发正文的 swarm 单位**现在拿不到这一格；
        //    ② `huntMark` 那条是 `…InTarget`，prefab **判据不足**（映射表里没有 `…InTarget` 结尾的 CardAnim）
        //       ⇒ 整条没接（按铁律 3：宁可留白，不猜）。
        //    两条都记在判据文件里（铁律 11：**记着**，不是不做）。
        void PlayTraitParticles(BattleEvent e)
        {
            string[] want = null;
            switch (e.Kind)
            {
                case EvtKind.Ability: want = new[] { KeywordTable.Ferocity }; break;
                case EvtKind.Attack:  want = AttackTraitParticles; break;
                case EvtKind.Trigger:
                    if (!string.IsNullOrEmpty(e.Keyword)) want = new[] { e.Keyword.ToLowerInvariant() };
                    break;
            }
            if (want == null || e.Player < 0 || !BoardSpec.IsValid(e.Slot)) return;

            var u = Ctx != null ? Ctx.Players[e.Player].Board[e.Slot] : null;
            if (u == null) return;                    // 不在场上（已离场 / 已挪走）⇒ 不表演
            var v = ViewAt(e.Player, e.Slot);
            if (v == null) return;

            foreach (var t in want)
                if (u.Has(t)) TraitParticles.Play(t, v.transform, why: e.Kind.ToString());
        }
        // `sniper` / `markerlight` / `stomp` 在 `KeywordTable` 里**没有常量**（只出现在规范化表里），
        // 所以这里写字面量 —— **拼写必须与规范化后的键逐字相同**（`CardDef.cs` 那张表）。
        static readonly string[] AttackTraitParticles =
            { "sniper", "markerlight", KeywordTable.LongRange, "stomp" };

        CardView ViewAt(int owner, int slot)
        {
            var views = owner == _me ? _myUnits : _foeUnits;
            CardView v;
            return views.TryGetValue(slot, out v) ? v : null;
        }

        /// <summary>攻击：先蓄力（`timeToChargeAttack` 0.35s），再朝目标冲一下
        /// （`DoPushBack` 的形状 + `Recoil Normal Tween` 的幅度）</summary>
        void PlayAttackFeel(BattleEvent e)
        {
            // 🔴 **记下攻击方的近战攻击** —— 挨打后坐的幅度原版是 `GetPushBackFactor(GetUnitSize(攻击方))`
            //    （判据与出处见 `CardFeel.PushBackMagnitude`），而 `EvtKind.Hit` 是发给**受击方**的、
            //    **不带攻击者**。表现层是先收 `Attack` 再收 `Hit`（`RuleCore.Attack` 里就是这个顺序），
            //    所以在这里存一份给下一条 `Hit` 用 —— 不用为这件事改引擎的伤害链。
            //    ⚠️ 放在 early-return **之前**：视图不在（比如攻击方那一刻刚被移走）也照样要记。
            var attacker = (e.Player >= 0 && e.Player < Ctx.Players.Length && e.Slot >= 0
                            && e.Slot < Ctx.Players[e.Player].Board.Length)
                ? Ctx.Players[e.Player].Board[e.Slot] : null;
            _lastAttackerMelee = attacker != null ? attacker.Attack : -1;
            _lastAttackerSide = e.Player;

            var v = ViewAt(e.Player, e.Slot);
            if (v == null) return;

            // 🔴 **2026-09-29 这里有一处真缺陷，顺手修掉**：原来目标位置取的是
            //    `layout.SlotPosition(e.TargetSlot)` —— 那是 **2D 屏幕布局**的坐标
            //    （`BoardLayout.SlotPosition` = `LayoutSpace.ToWorld`，单位是 HUD 的 108 px/单位），
            //    而场上的卡活在 **3D 战场的原版世界单位**里（`ArenaSlots`，见 `BoardLayout` 文件头那句
            //    「真 3D 那条路不吃这一套了」）⇒ 那个 `dir` 是**两个坐标系相减**出来的，
            //    只在 2D 兜底布局下才有意义。改成**两边都取真身的 `transform.position`** ——
            //    原版也是这么取的（出手点 = 攻方 transform、目标点 = 受击方 `transform.position`）。
            var tv = ViewAt(e.TargetPlayer, e.TargetSlot);
            if (tv == null) return;                 // 目标视图不在（刚被移走）⇒ 不表演，不是错

            // 出手前那个**静止位**（瞄准时卡会「抬手」，见 `TickTargetingLean`；段4 回的是它）
            Vector3 home = LeanHomeOf(e.Player, e.Slot, v);

            if (e.Ranged)
            {
                // 🔴 **远程没有「冲到目标身上」这一段**（原版 `_ResolveAttackRangedAnim` 只有
                //    `DOLocalMove(originalLocalPosInPlay, attackStepTime)` 回正，**没有任何目标点公式**）。
                //    我们不做位移 —— 抬手那点偏移由 `TickTargetingLean` 的归位收掉。
                //    起手 → VFX 发出 = 2 × `attackStepTime` = 0.2s，那一条在 `EventTiming.AttackStepRanged` 里。
                return;
            }

            // 近战：原版那条三段式（0.4s 全程、**命中在 t = 0.1**）——
            // 命中时刻由 `EventTiming.MeleeImpactLag = 0` 保证（见那份文档 §一）。
            CardFeel.MeleeAttack(v.transform,
                                 CardFeel.MeleeEndPos(v.transform.position, tv.transform.position, e.Player == _me),
                                 home);
        }

        /// <summary>出手前那个**静止位**（局部坐标）。瞄准时卡被 <see cref="TickTargetingLean"/> 抬起来了，
        /// 攻击序列的段4 要回到**没抬手之前**那一处，不是出手那一帧的位置。
        /// 顺手把瞄准那条路让开（`_leanActive = false`）—— 两个东西不能同时改同一个 transform。</summary>
        Vector3 LeanHomeOf(int owner, int slot, CardView v)
        {
            Vector3 home = (_leanActive && _leanSlot == slot && owner == _me)
                         ? _leanHome : v.transform.localPosition;
            _leanActive = false;
            return home;
        }

        /// <summary>上一次 `Attack` 的攻击方：近战攻击（-1 = 没有/取不到）+ 在哪一侧。
        /// 给挨打后坐的幅度用，见 `PlayAttackFeel` 里的注释。</summary>
        int _lastAttackerMelee = -1;
        int _lastAttackerSide = -1;

        /// <summary>挨打：位置弹一下 + 转一下（`Impact Light Tween`），
        /// 顺便把伤害数值飘出来（`InBattleDamageCounter Variation 1` 的时间轴）</summary>
        void PlayHitFeel(BattleEvent e)
        {
            var v = ViewAt(e.Player, e.Slot);
            var layout = e.Player == _me ? playerBoard : enemyBoard;
            // 🔴🔴 **2026-10-11（A359）这一处是 `PlayAttackFeel` 那个缺陷的【同族漏网】**：
            //    原版这枚飘字是**挂在受击那张卡上的** `CardDamageCounterController`（字段
            //    `damageCounterParent`，见 `CardFeel.PopNumber` 的注释）⇒ 基准点 = **那张卡自己**。
            //    我们这里原来取 `layout.SlotPosition(e.Slot)` —— 那是 **2D 屏幕布局**的点
            //    （`LayoutSpace.ToWorld`，屏上 708 / 466 px），而场上的卡活在 **3D 战场**里
            //    （`ArenaSlots.RootPosition`，投影到屏上 631~657 px）⇒ 3D 下飘字**偏低 51~72 px**。
            //    改法与同族的 `PlayAttackFeel`（`:5875-5881`：那处 `dir` 是**两个坐标系相减**出来的）
            //    同一个口径：取**这张卡在屏幕上真正画在哪** = `BoardLayout.DropTargetWorld(e.Slot)`
            //    （`BoardLayout.DropTargetWorld`；**非 3D 时它逐位等于 `SlotPosition`** ⇒ 2D 路不动）。
            //    **判据只此一处**（铁律 6）—— 别在这里再算一次投影；`Editor/BattleScene.cs` 那条
            //    「飘在挨打那张卡上」的断言**必须跟着换同一个口**（两处一起改，否则那条先红）。
            //    🧨 **改坏法**：换回 `layout.SlotPosition(e.Slot)` ⇒ 飘字回落到 2D 行线（低 51~72 px）
            //    ⇒ `BattleScene.cs` 那条飘字位置断言当场红；在 2D 兜底布局下两种写法**完全等价**（不报错）。
            Vector3 at = layout.DropTargetWorld(e.Slot);

            if (v != null)
            {
                // 「背离攻击者」= 往**自己那半场的外侧**弹。攻击永远来自对面半场，
                // 所以这个方向不需要事件里再带攻击者是谁。
                Vector3 away = e.Player == _me ? Vector3.down : Vector3.up;
                // 幅度：**攻击方**的近战档（`CardFeel.PushBackMagnitude`）。
                // ⚠️ **只在「挨打方跟攻击方不是同一侧」时才用** —— 攻击永远跨半场，而**疲劳**伤害
                //    （`RuleCore` 里给玩家自己的督军发的那条 `Hit`）没有攻击方，
                //    不加这个护栏就会把**上一刀**的攻击方档位套上去。
                int melee = (e.Player != _lastAttackerSide) ? _lastAttackerMelee : -1;
                CardFeel.HitReact(v.transform, away, Mathf.Abs(e.Amount) >= CardFeel.HeavyHitDamage,
                                  0f, melee);
            }

            // 震镜头（原版卡预制体 `meleeHitCameraShakePreset` → preset `Shake Hit Small`）。
            // ⚠️ **放在 `v != null` 外面**：原版这条是**打人那张卡**的 `CardScript`
            //    （`ResolveAttackAnimationEffects`）触发的，与「被打的那张视图还在不在」无关。
            // 做法与「为什么是相机 + HUD 根一起动」见 `CardFeel.ShakeCamera` 的注释。
            CardFeel.ShakeCamera(cam, hudRoot, CardFeel.ShakeWorldAmplitude, 0f, boardCam);

            // 伤害 0 = 被挡下（原版也发事件）—— 那一条不飘字，免得屏幕上冒出「-0」
            if (e.Amount != 0)
                LastPop = CardFeel.PopNumber(transform, at + PopOffset,
                                             e.Amount, e.Amount < 0);
        }

        /// <summary>伤害飘字相对**基准点**（受击那张卡所在格）的偏移。
        /// 出处 = `CardFeel` 的 `Catalog` 里那句「**飘字的字号 / 颜色 / 位置偏移**（`PopNumber` 里那几个字面量）」
        /// ⇒ **这一项是我们挑的**（原版查不到等价字段：那枚计数器的父节点是卡自己的
        /// `damageCounterParent`，偏移烘在 prefab 里）。
        /// 🔴 **判据只此一处**（铁律 6）：`PlayHitFeel` 用它摆飘字，`Editor/BattleScene.cs` 那条
        /// 「飘在挨打那张卡上」的位置断言**读同一个常量** —— 两处各抄一份的话，偏移一改，
        /// 断言就变成「拿旧靶判新位置」（2026-10-11 A359 顺手收口：原来它内联在下面那句实参里）。</summary>
        public static readonly Vector3 PopOffset = new Vector3(0f, CardFeel.ToOurs(0.35f), -0.4f);

        /// <summary>最近一次飘出来的数值（自检断言用 —— 飘字 1.83 s 后自己销毁，截图上看不出它来过）</summary>
        public Label LastPop { get; private set; }

        /// <summary>阵亡消散。
        /// ⚠️ **必须把视图从 `_myUnits/_foeUnits` 里摘掉** —— 不然 `SyncBoard` 发现引擎里那一格空了，
        ///    当场就把视图 `Kill` 了，消散一帧都看不见（批处理里更是直接 `DestroyImmediate`）。</summary>
        void PlayDeathFeel(BattleEvent e)
        {
            var views = e.Player == _me ? _myUnits : _foeUnits;
            var fade = FadingMap(e.Player);
            CardView v = null;
            bool fromFade = false;
            if (fade.TryGetValue(e.Slot, out v) && v != null) fromFade = true;
            else if (!views.TryGetValue(e.Slot, out v) || v == null) return;

            // 🆕 2026-09-25 **「引擎说它死了、可那一格还站着人」= 它变成残骸了 ⇒ 不播阵亡消散、不摘视图。**
            //
            // 原版那条路根本不是普通阵亡：`BattleManager.ResolveDestroyUnit` 里先问
            // `SupportMethods.HasToTransformIntoRemnant`（`remnant(1050) || waystone(1140)`），
            // 命中就走 `AddTransformIntoRemnant` —— 画面上是**同一张卡**被盖上残骸体
            // （`BattleCardUI.CreateRemnantBody` + `RemnantBody.ToggleBody3D(false)`），**那张卡不消散**。
            //
            // ⚠️ 没有这道守卫的后果（**很难查**）：`SyncBoard` 刚给同一张视图盖上残骸体，
            //    0.85 s 后这条阵亡事件轮到播放，又把它溶解销毁 ⇒ 残骸**闪一下没了、再新建一张视图**，
            //    看上去像「残骸随机消失」。而两条路径各自看都「对」。
            // ⚠️ 判据用**棋盘那一格还有没有东西**，而不是 `IsRemnant` —— 因为这条事件是在
            //    `EventTiming` 排的**延迟队列**里，轮到播的时候棋盘可能又变过了；
            //    「这一格还站着人」才是「别溶解」的充分理由（格子上真有别人也算，不能溶掉一张活的卡）。
            // ⚠️ 判据 = **这一格上站的是不是「同一张卡翻了个面的残骸」**，而不是原来那句
            //    「这一格里有没有人」—— 🔴 **2026-10-01 改模型时这一条必须跟着改**：
            //    棋盘连续无洞之后，普通阵亡**当场就会有人补位进来**（`BoardSlots.RemoveAt`），
            //    按「有没有人」判的话**所有内侧阵亡都不会演消散**（画面上人「啪」地消失，
            //    而自检里那些洞状夹具不会有这个现象 ⇒ 只有真人玩才看得出来）。
            //    现在按**身份**判：只有「那一格站着的确实是它的残骸」才不溶解。
            if (!fromFade && Ctx != null && BoardSpec.IsValid(e.Slot))
            {
                var occ = Ctx.Players[e.Player].Board[e.Slot];
                if (occ != null && occ.IsRemnant && occ.Card != null && occ.Card.Name == e.CardId)
                    return;      // 翻面成了残骸 ⇒ 同一张卡继续站着（原版 `RemnantBody3D` 那一路）
            }

            fade.Remove(e.Slot);
            // ⚠️ 只摘**它自己**那一格 —— 这一格里现在挂的可能是补位上来的新主人（别把它一起摘了）
            CardView occupied;
            if (views.TryGetValue(e.Slot, out occupied) && occupied == v) views.Remove(e.Slot);
            _dying.Add(v);
            // 督军格的阵亡**慢一倍多**（原版 `deathTimeWarlordDuration 0.5` vs 小兵 0.2，见 `CardFeel.DeathDissolve`）
            // 🆕 2026-09-29：表现**照原版重做**了 —— 不是溶解，是「关掉 3D 体 + 在卡位生成死亡爆散体 + 抖一下」
            //   （判据与被卡住的那一环 → `CardFeel.DeathExplosion`）。
            bool warlord = e.Slot == BoardSpec.WarlordSlot;
            var tw = CardFeel.DeathExplosion(v, 0f, () => { _dying.Remove(v); Kill(v.gameObject); }, warlord);
            if (tw == null) { _dying.Remove(v); Kill(v.gameObject); }   // 没补间（例如 DOTween 不可用）就直接销毁
        }

        /// <summary>
        /// **回手 / 回牌库**（`Return a friendly Vehicle to your hand`）—— 把视图从格位上摘掉。
        ///
        /// 和 <see cref="PlayDeathFeel"/> 的区别只有一条，但很要紧：**不播消散**。
        /// 它不是死了，是回手牌了 —— 播阵亡特效是**错的画面**。
        /// （引擎侧也不会把它放进弃牌堆／阵亡登记表，两边对得上。）
        ///
        /// ✅ **「飞回手牌」那条动画原版是什么，2026-09-17 查到了**（原来这里写「没查到」）：
        ///    `BattleCardUI.PlayBackToHandAnimation`（`decomp_out/BattleCardUI__PlayBackToHandAnimation.c`）=
        ///    **把「手牌 → 战场」那条 clip 倒放**：
        ///      `Animation.Rewind()` → 取那条 clip 的 `AnimationState` →
        ///      **`speed = −1`**（字面量 `_DAT_1834b2bc8`，本机用 `工具/read_literal.py` 读出 **−1.0**）→
        ///      **`time = AnimationState.length`**（从末尾起；⚠️ 不是 `AnimationClip.length`，值相同但**措辞**要准）→
        ///      `Play()`；随后 `StartCoroutine(DelayedResetMaterial(0.55))` 复位材质。
        /// ✅ **2026-09-29 做完了**：实现 = `CardFeel.ReturnToHand`（**同一条时间轴用代码喂值** ——
        ///    我们这套 2D 卡没有 `Animation` 组件、更没有那条 clip）。逐帧判据（含反向四个时刻与
        ///    `DOMove(up × localScale.x × 3.0, 0.208 s, 线性)`）都写在那个方法的注释里，本文件不抄第二份。
        /// ⚠️ **回牌库也走这条倒放**（原版两个调用者：`ResolveRecallToHand` / `ResolveRecallToDeck`）。
        /// </summary>
        void PlayReturnFeel(BattleEvent e)
        {
            var views = e.Player == _me ? _myUnits : _foeUnits;
            var fade = FadingMap(e.Player);
            CardView v;
            // 🔴 2026-10-01：回手/回牌库**也会让外侧的人补位** ⇒ 键可能已经被顶掉了，先去 `_fading` 找
            //   （键被顶掉那一刻由 `DetachStaleView` 存下来；没顶掉的仍挂在 `views` 上）
            if (fade.TryGetValue(e.Slot, out v) && v != null) fade.Remove(e.Slot);
            else if (!views.TryGetValue(e.Slot, out v) || v == null) return;
            // ⚠️ 只摘**它自己**那一格（这一格可能已经换了新主人）
            CardView occupied;
            if (views.TryGetValue(e.Slot, out occupied) && occupied == v) views.Remove(e.Slot);

            // 🆕 2026-09-29：**倒放 `Card Hand To Board`**（原版 `BattleCardUI.PlayBackToHandAnimation`）——
            //   原来这里是「当场摘掉」。⚠️ 与阵亡同一条道理：**必须先从字典里摘掉**，否则 `SyncBoard`
            //   发现引擎里那一格空了，当场就把它 `Kill` 了，动画一帧都看不见。
            //   摘掉之后它成了孤儿 ⇒ 挂进 `_returning`，重开一局时由 `Begin` 那一段统一清掉。
            //   ⚠️ **回牌库也走同一条倒放**（原版两个调用者：`ResolveRecallToHand` 与 `ResolveRecallToDeck`）。
            if (animateFeel)
            {
                _returning.Add(v);
                var tw = CardFeel.ReturnToHand(v, 0f, () => { _returning.Remove(v); Kill(v.gameObject); });
                if (tw != null) return;
                _returning.Remove(v);       // 没补间（DOTween 不可用）⇒ 落到下面直接销毁
            }
            Kill(v.gameObject);
        }

        /// <summary>正在回手的视图（已经从 `_myUnits/_foeUnits` 里摘掉了）。自检断言用。</summary>
        readonly List<CardView> _returning = new List<CardView>();
        public int ReturningCount { get { return _returning.Count; } }
        public CardView ReturningView(int i) { return i >= 0 && i < _returning.Count ? _returning[i] : null; }

        /// <summary>正在消散的视图（已经不在 `_myUnits/_foeUnits` 里了）。自检断言用</summary>
        readonly List<CardView> _dying = new List<CardView>();
        public int DyingCount { get { return _dying.Count; } }
        public CardView DyingView(int i) { return i >= 0 && i < _dying.Count ? _dying[i] : null; }

        /// <summary>这一格有没有**排着队还没播**的阵亡事件 —— `SyncBoard` 靠它决定
        /// 「现在能不能把这个视图销毁掉」（见那里面的注释：销毁早了消散就演不出来）</summary>
        bool DeathPendingFor(int owner, int slot)
        {
            for (int i = 0; i < _timeline.Count; i++)
            {
                var e = _timeline[i].evt;
                if (e.Kind == EvtKind.Death && e.Player == owner && e.Slot == slot) return true;
            }
            return false;
        }

        /// <summary>
        /// 销毁视图。
        /// ⚠️ **编辑器模式下 `Object.Destroy` 不生效**（它要等下一帧，而编辑/批处理里没有帧循环），
        ///    必须用 `DestroyImmediate`。踩过：批处理自检里旧的手牌视图全留在画面上，
        ///    看起来像「手牌莫名其妙多出好几张」。
        /// </summary>
        static void Kill(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }

        /// <summary>某一格上那个单位**该站的位置与缩放**。
        /// 🔴 **判据只此一处** —— `SyncBoard` 落位与「让位」预览（`TickShufflePreview`）都读它，
        /// 免得多处各算一套坐标（那种分叉本工程吃过好几次）。
        /// 两套走法：真 3D 战场用 `ArenaSlots`（逐值来自原版 `MinionManager`，判据 = `ArenaSlots` 唯一出处）；
        /// 没有 3D 战场时退回烘好的背景图那套正交坐标 —— 那一层没有别的相机，
        /// 这时候把卡挂到 `ArenaLayer` 会**两台相机都不画**（静默消失）。</summary>
        void PoseFor(bool mine, int slot, UnitState u, out Vector3 pos, out float scale)
        {
            if (use3DBoard)
            {
                // 站在地面上、y=0、卡根旋转 identity、缩放 = `desiredScale`（玩家 0.36 / 敌 0.69）。
                // ⚠️ **屏幕位置和原来那套正交坐标几乎重合**（两行 65.4%/42.7% vs 原版实测
                //    65.6%/43.1%）⇒ **命中判定仍然按屏幕空间走，不用改**。
                bool foe = !mine;
                float sc = (u != null && u.IsWarlord) ? ArenaSlots.HeroScale(foe) : ArenaSlots.CardScale(foe);
                pos = ArenaSlots.RootPosition(slot, foe, sc);
                scale = sc;
            }
            else
            {
                var layout = mine ? playerBoard : enemyBoard;
                pos = layout.SlotPosition(slot);
                scale = layout.placedScale * LayoutSpace.Scale;
            }
        }

        void SyncBoard(int owner, Dictionary<int, CardView> views, bool mine)
        {
            var board = Ctx.Players[owner].Board;
            var layout = mine ? playerBoard : enemyBoard;
            var byUnit = UnitViewMap(owner);
            // ⚠️ 原来这里是「是不是 Tide，不是就是 Ember」的二选一 —— 一接原版阵营就会
            //    全部掉进 Ember 那个色。改成查表（`FactionColor`）。
            var frame = FactionColor(owner == _me ? _myFaction : _foeFaction);

            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = board[s];
                CardView v = null;
                bool hasView = false;

                if (u != null)
                {
                    // ① **先按身份找它的视图**（换格 / 刚上场都走这一条）。
                    //    ⚠️ Unity 的 `!= null` 判的是「对象还在不在」，销毁过的视图这里会回落成 false。
                    CardView byId;
                    if (byUnit.TryGetValue(u, out byId) && byId != null) { v = byId; hasView = true; }
                    else if (views.TryGetValue(s, out byId) && byId != null
                             && !ViewOwnedByLiveUnit(board, byUnit, byId))
                    {
                        // ② 退路：这一格的键上挂着一张**主人已经不在场上了**的视图 —— 认领它。
                        //    两个常见来源：**首次同步**（还没登记身份），以及 🔴 **变残骸**
                        //    （原版是「同一张卡翻个面」⇒ 引擎那边是个**新的 `UnitState`**，
                        //     身份表里查不到它，但视图必须是原来那张 ⇒ 这里正好接住）。
                        v = byId; hasView = true;
                    }
                }
                else
                {
                    views.TryGetValue(s, out v);      // 可能为 null（已销毁）
                }

                if (u == null)
                {
                    if (v == null) { views.Remove(s); continue; }
                    // 🆕 2026-09-26：这一格空了 ⇒ 「未行动」绿光也要灭。
                    //   ⚠️ **必须在这里关**：下面那条 `continue`（阵亡事件还排在时间线上、视图要再站 0.85 s）
                    //      —— 不关的话那张正在消散的卡会**一直亮着绿光**。
                    v.SetCanAct(false);
                    // ⚠️ **这一格刚空、但离场演出还排在时间线上时，先别销毁。**
                    //    `PlaySignals` 只**排期**、不当场播（见 `EventTiming`）—— 引擎里人已经死了，
                    //    而画面上那张卡还要再站 0.85 s 才轮到「阵亡」那一刻。
                    //    第一版就是在这里立刻销毁的：`DyingCount` 恒为 0、消散一帧都没演出来，
                    //    而**截图上看不出**（只看到「人没了」）。
                    if (animateFeel && PendingExitFor(owner, s)) continue;

                    Kill(v.gameObject); views.Remove(s);
                    continue;
                }

                // 🔴 **2026-10-01：这个单位换格号了吗**（棋盘是连续无洞的 —— 它前面插了人 / 补位，
                //    见 `RuleEngine/Core/BoardSlots.cs`）。按**身份**把视图从旧格号搬到新格号，
                //    千万别当「这格空了」销毁重建 —— 重建会瞬移、丢动画、丢关键词框/残骸体那些
                //    挂在视图上的状态。判据 = `byUnit`（单位 → 视图）。
                //
                //    ⚠️ **补位会把「上一任」的键顶掉**：那一格里原来那张卡（已经离场、演出还排在队里）
                //       必须**先摘下来存到 `_fading`**，否则 0.85 s 后演阵亡时 `views[slot]` 已经是
                //       新主人了 —— 会把**一张活着的卡**溶掉（`PlayDeathFeel` 从那一档里找）。
                bool moved = false;
                if (hasView)
                {
                    CardView at;
                    if (views.TryGetValue(s, out at) && at != null && at != v
                        && !ViewOwnedByLiveUnit(board, byUnit, at))
                        DetachStaleView(owner, views, s, at);

                    RemoveSlotKeysExcept(views, v, s);
                    if (!views.ContainsKey(s) || views[s] != v) { views[s] = v; moved = true; }
                    // 掉血/疲劳/关键词要反映到卡面（原来这段挂在「这一格本来就有视图」那一支）
                    v.SetData(ToCardData(u, owner == _me ? _myFaction : _foeFaction));
                }
                else
                {
                    // 这一格的键上要是还挂着**已经不在场上的**视图 ⇒ 先摘掉，别让它变幽灵
                    CardView at;
                    if (views.TryGetValue(s, out at) && at != null
                        && !ViewOwnedByLiveUnit(board, byUnit, at))
                        DetachStaleView(owner, views, s, at);

                    var data = ToCardData(u, owner == _me ? _myFaction : _foeFaction);
                    data.frame = u.IsWarlord ? new Color(0.95f, 0.82f, 0.35f) : frame;   // 督军描金
                    // 🔴 **场上用 `CardFace.Board`**：原版在 inPlay 类状态把**整张 `2DCard` 关掉**
                    //    （`Card2DController.Toggle(false)`），只留 `Board Elements` 那棵子树 ——
                    //    插图/卡框/费用/稀有度宝石/卡名/阵营行/兵种行/效果底板/效果文字**一起没有**，
                    //    只剩攻/血/甲 + 7 槽关键词徽标。见 `CardFace` 的注释与出处。
                    v = CardView.Create(boardRoot, data, $"{(mine ? "My" : "Foe")}Unit_{s}_{u.Name}",
                                        CardFace.Board);
                    views[s] = v;
                    byUnit[u] = v;      // 🆕 2026-10-01：登记身份（上面那段换格靠它认人）

                    // 🆕 2026-09-29 **督军落场：整组徽标 0 → 1 淡回来**
                    // （原版 `CardScript.<HeroLandIntoField>` 里那句 `FadeAllTraitsIcons(1.0f, 0.3f)`
                    //  —— 那也是全量反编译里 `FadeAllTraitsIcons` 唯一的调用点）。
                    // ⚠️ **只在开了手感补间时演**（和这一节别的动作同一条规矩）：批处理没有帧循环，
                    //    演到一半会停在 α=0 ⇒ 截图里督军的徽标整个不见（而断言看不出来）。
                    if (animateFeel && u.IsWarlord)
                    {
                        v.SetBadgeAlpha(0f);
                        v.FadeBadges(CardFeel.HeroLandBadgeAlpha, CardFeel.HeroLandBadgeFadeTime);
                    }
                }

                Vector3 pose;
                float poseScale;
                PoseFor(mine, s, u, out pose, out poseScale);
                if (use3DBoard) v.SetLayer(ArenaSlots.ArenaLayer);   // 换 layer：改由透视相机画

                // 🔴 2026-10-01：**换格 = 补一次位移补间**（原版 `ReassembleMinions` →
                //    `CardScript.UpdateMinionInPlayPosition` 就是一句 `DOLocalMove(新位, 时长)`）。
                //    时长 = `minionReassembleTime (0.15) ÷ 1.5 = 0.1 s`（见 `CardFeel.Reassemble`）。
                //    ⚠️ 批处理里 `animateFeel` 是关的 ⇒ 必须**落位**（断言要的是精确坐标）。
                if (moved && animateFeel) CardFeel.Reassemble(v, pose);
                else v.SetPose(pose, 0f, poseScale);
                v.SetHighlight(CardHighlightState.Normal);

                // 🆕 2026-09-29：**状态框**（原版 `BattleCardUI` 的两本字典那一套；
                //   判据全文 → `Core/TraitFrames.cs` 的文件头）。原版挂在 `CardScript.AddEffect`
                //   那几个分支上（trait 被加上那一刻），我们引擎不发那个事件 ⇒ 在这里**按集合对差**
                //   （`RefreshAll` 就在动作结算之后跑，时机与原版一致）。
                var tf = TraitFrames.Attach(v);
                if (tf != null && u != null) tf.SyncKeywords(u.Keywords.Keys);

                // 🆕 2026-09-25：**残骸体**（原版 `RemnantBody3D <阵营>`）——
                //   灵族 = 一枚漂浮的灵魂石（`Spirit Stone Idle`）· 死灵 = 一张碎裂的卡（`Card Remnant`）。
                //   ⚠️ 必须排在 `SetData` **之后**：`SetData`/`SetFace` 会按形态开关各层，
                //      先盖残骸体再 `SetData` 的话，原卡卡身会被它重新打开。
                //   ⚠️ 撤销那一路只在**确实还挂着**时才调（不然每帧对每张卡白跑一遍开关）。
                string remPrefab = RemnantPrefabOf(u);
                if (remPrefab != null) v.SetRemnantBody(true, remPrefab);
                else if (v.RemnantBodyVisible) v.SetRemnantBody(false, null);

                // 🆕 2026-09-26：**「未行动」绿光**（原版 `CardScript.ActivateMinion` → `SetActive(CanAttackNow())`
                //    → `BattleCardUI.canAttackAnim`，偏移 `+0x140`）。
                //   🔴 **判据只写一份** = `RuleCore.CanActNow`（它和 `DeclareAttack` 共用同一段检查）；
                //      这里**只问「画不画」**，别在这儿重写疲劳/眩晕/配额那几条。
                //   ⚠️ 问的是「**至少有一种打法**能打」—— 压制（`pindown`）只禁近战，
                //      所以近战/远程各问一次（`CanActNow` 内部就是这两问）。
                //   ⚠️ 挂在这里（`SyncBoard`）是因为**每一个改动棋盘/回合的地方最后都会走到它**
                //      —— 和光环那 9 个写入点 + `BeginTurn` 是同一条纪律。
                v.SetCanAct(RuleCore.CanActNow(Ctx, owner, s));
            }
        }

        // ==================================================================
        //  「让位」预览（拖拽中，2026-10-01）
        // ==================================================================
        //
        // 原版 `MinionManager__ReassembleMinionsWhilePlayingUnit.c`（由 `BattleManager__Update.c:575` **逐帧**调）：
        //   ① 求出「这张牌会插到该侧的哪个下标」（那函数里是 `GetClosestAvailableSlot`，再被
        //      `InsertMinion` 里的 `AdjustedSlot` 夹紧 —— 我们两处合成 `BoardSlots.Resolve` 一份判据）；
        //   ② **下标 ≥ 插入点的单位整体 +1**（`:43-45` 那句 `iVar8 = iVar9 + 1`）——
        //      ⚠️ 它**不改数据**，只是把它们**补间**到「插进去之后」的位置
        //      （`CardScript.UpdateMinionInPlayPosition`，时长 `minionReassembleTime ÷ 1.5 = 0.1 s`）；
        //   ③ 外加 `SetCardShadow(落点格, active:1, rotate:1)` —— 那半条 = 已经做掉的落点指示 (b)。
        //
        // 🔴 **只在 `animateFeel` 打开时走**：批处理自检要**精确坐标**（拖拽那几条断言盯着
        //    `localPosition`），预览一插进来就会把它们推成浮点数。
        int _previewOwner = -1;        // 哪一方的棋盘在预览（-1 = 没有）
        int _previewRequested = -1;    // 请求的落点格号（-1 = 没有）
        /// <summary>预览期间**被推开过**的那几张视图 —— 收工时要一次性把它们补间送回真格位。</summary>
        readonly List<CardView> _previewMoved = new List<CardView>();

        /// <summary>驱动层接 `CardInteraction.OnDropPreview`：记住「现在拖到哪一格」。
        /// `which == null` / `slot &lt; 0` = 收工（松手、取消、重开一局都走这一条）。</summary>
        void OnDropPreview(BoardLayout which, int requestedSlot)
        {
            if (which == null || requestedSlot < 0)
            {
                EndPreviewReturn();
                _previewOwner = -1;
                _previewRequested = -1;
                return;
            }
            _previewOwner = (which == playerBoard) ? _me : (1 - _me);
            _previewRequested = requestedSlot;
        }

        /// <summary>
        /// 预览收工时把被推开的单位送回**真格位**。两条路分开走，别混：
        ///   · **取消**（拖回手牌 / 拖到空白处）⇒ 真格位还是原来那些 ⇒ **补一次 `DOLocalMove` 送回去**；
        ///   · **放下去**（`interaction.DropAccepted`）⇒ 引擎马上就会把它插进去，
        ///     那时它们**本来就该站在预览位上**。这里**别动**（动了会先弹回原位、再被插出去 = 抖两下）。
        /// </summary>
        void EndPreviewReturn()
        {
            if (_previewMoved.Count == 0) return;
            if (interaction != null && interaction.DropAccepted) { _previewMoved.Clear(); return; }
            for (int i = 0; i < _previewMoved.Count; i++)
            {
                var v = _previewMoved[i];
                if (v == null) continue;
                int owner, slot;
                if (!FindBoardViewSlot(v, out owner, out slot)) continue;
                Vector3 pos; float sc;
                PoseFor(owner == _me, slot, Ctx.Players[owner].Board[slot], out pos, out sc);
                if ((v.transform.localPosition - pos).sqrMagnitude > 1e-6f) CardFeel.Reassemble(v, pos);
            }
            _previewMoved.Clear();
        }

        /// <summary>这张视图现在挂在谁家的第几格上（找不到 = 已经不在场上了）。</summary>
        bool FindBoardViewSlot(CardView v, out int owner, out int slot)
        {
            foreach (var kv in _myUnits) if (kv.Value == v) { owner = _me; slot = kv.Key; return true; }
            foreach (var kv in _foeUnits) if (kv.Value == v) { owner = 1 - _me; slot = kv.Key; return true; }
            owner = -1; slot = -1;
            return false;
        }

        /// <summary>预览时这个单位**暂时**该站哪一格（没有预览 = 它自己那一格）。</summary>
        int PreviewSlotFor(int owner, int slot)
        {
            if (_previewOwner != owner || _previewRequested < 0 || Ctx == null) return slot;
            int side, insertAt;
            if (!BoardSlots.Resolve(Ctx.Players[owner], _previewRequested, out side, out insertAt)) return slot;
            if (BoardSlots.SideOf(slot) != side) return slot;
            if (BoardSlots.IndexOf(slot) < insertAt) return slot;       // 插入点以前的不动
            int moved = BoardSlots.IndexOf(slot) + 1;
            if (moved >= BoardSpec.SlotsPerSide) return slot;           // 已经是最外那一格 ⇒ 无处可让
            return BoardSlots.SlotOf(side, moved);
        }

        /// <summary>拖拽中每帧：把场上单位**推向**预览位（只在拖拽期间跑）。
        /// ⚠️ 这里**只改 `localPosition`**，不碰缩放/旋转 —— 原版那句 `DOLocalMove` 也只动位移。
        ///
        /// 🔴 **只在「预览中」时跑** —— 别让它每帧无条件地把所有卡往格位上拽：
        /// 攻击前冲 / 挨打后坐 / 飘字那些补间**改的也是 `localPosition`**，每帧拽一次会把它们**当场抹平**
        /// （而且只在真人玩的时候看得出来，自检里 `animateFeel` 是关的）。
        /// 收工那一下走 `EndPreviewReturn()`（一次 `DOLocalMove`），不在这儿续着拽。</summary>
        void TickShufflePreview(float dt)
        {
            if (!animateFeel || Ctx == null || _previewRequested < 0) return;

            float k = 1f - Mathf.Exp(-18f * dt);      // 指数趋近：帧率无关
            for (int i = 0; i < 2; i++)
            {
                int owner = i == 0 ? _me : 1 - _me;
                bool mine = owner == _me;
                var views = mine ? _myUnits : _foeUnits;
                foreach (var kv in views)
                {
                    var v = kv.Value;
                    if (v == null) continue;
                    Vector3 pos; float sc;
                    PoseFor(mine, PreviewSlotFor(owner, kv.Key), Ctx.Players[owner].Board[kv.Key], out pos, out sc);
                    var tr = v.transform;
                    if ((tr.localPosition - pos).sqrMagnitude < 1e-6f) continue;   // 已经在位 ⇒ 没被推过
                    if (!_previewMoved.Contains(v)) _previewMoved.Add(v);
                    tr.localPosition = Vector3.Lerp(tr.localPosition, pos, k);
                }
            }
        }

        /// <summary>
        /// 这一格该盖哪一具残骸体（不是残骸 ⇒ `null`）。
        /// **判据是卡上的关键词，不是阵营** —— `Waystone.` → 灵族那具（漂浮的灵魂石）、
        /// `Remnant.` → 死灵那具（碎裂的卡）。
        /// 全卡池里 `Waystone.` 只出现在 SaimHann（24 张）、`Remnant.` 只出现在 Sautekh（36 张）
        /// （2026-09-25 实测）⇒ 与「查 `CardArmy`」等价，但**写关键词更抗以后加卡**。
        /// 出处（原版那两具叫什么、长什么样）→ `资料/查证_useWaystone_语义.md` §五 的资产表。
        /// </summary>
        static string RemnantPrefabOf(UnitState u)
        {
            if (u == null || !u.IsRemnant) return null;
            if (u.Has(KeywordTable.Waystone)) return RemnantBodyAeldari;
            if (u.Has(KeywordTable.Remnant)) return RemnantBodyNecrons;
            return null;
        }

        /// <summary>残骸体 prefab 名（= `WarpforgeVFX/Prefabs/` 下的文件名 = 效果库的键）。</summary>
        const string RemnantBodyAeldari = "RemnantBody3D Aeldari";
        /// <summary>见 <see cref="RemnantBodyAeldari"/>。</summary>
        const string RemnantBodyNecrons = "RemnantBody3D Necrons";

        /// <summary>
        /// 手牌同步：**引擎的手牌顺序是权威**，视图跟着走。
        /// 先按顺序把现有的视图和引擎手牌一一配对（同名的多个也逐个配），
        /// 配不上的（被打出/超上限弃掉的）销毁，缺的新建。
        /// </summary>
        void SyncHand()
        {
            var h = Ctx.Players[_me].Hand;

            var pool = new List<CardView>(_handViews);
            var ordered = new List<CardView>(h.Count);
            _dealt.Clear();                     // 这一轮新进来的牌（发牌入场要用，见下面）

            foreach (var inst in h)
            {
                var card = inst.Card;
                // 🔴 第 7 行第 4 步：**先按实例身份配对**（手里两张同名卡时才分得开谁是谁），
                //    配不上再退回按卡名（老行为：新建的视图 / 换过 def 的那些）。
                CardView match = null;
                for (int i = 0; i < pool.Count; i++)
                {
                    if (pool[i] != null && ReferenceEquals(pool[i].Inst, inst)) { match = pool[i]; pool.RemoveAt(i); break; }
                }
                if (match == null)
                {
                    for (int i = 0; i < pool.Count; i++)
                    {
                        if (pool[i] != null && pool[i].Data.id == card.Name) { match = pool[i]; pool.RemoveAt(i); break; }
                    }
                }
                if (match == null)
                {
                    // 🔴 **2026-10-17（D1）**：**手牌是【唯一】不印阵营行的场景**（另一个是场上）。
                    //    ——「手牌 + 场上不印，其余都印」（**用户 2026-09-22 裁定**，2026-10-17 由他指认
                    //    实拍复核过：`点击卡片查看详情的参考.png` 里卡名下的**橙字 `萨姆-罕` 就是阵营行**；
                    //    `战斗截图参考.png` 里手牌、场上都没有那一行）。
                    //    ⇒ 这里是**手牌卡的唯一创建入口**，必须显式传 `CardFace.Hand`
                    //      （`CardView.Create` 的缺省档是 `Full`，而 `Full` **带**阵营行 ⇒ 不传就是印）。
                    //    判据收口在 `CardView.FaceShowsArmy` 一处；断言在 `Editor/CardBaseDemo.cs` 的
                    //    `AssertArmyLine()`（⚠️ 它**不在** `_run_8_checks.sh` 那 11 条里 ⇒ 见 `项目任务.md`）。
                    match = CardView.Create(transform, ToCardData(card, _myFaction), "Hand_" + card.Name,
                                            CardFace.Hand);
                    _dealt.Add(match);
                }
                match.Inst = inst;             // 这一份 = 这张视图（第 7 行第 4 步）
                ordered.Add(match);
            }

            foreach (var dead in pool) if (dead != null) Kill(dead.gameObject);

            _handViews.Clear();
            _handViews.AddRange(ordered);
            interaction.SetCards(new List<CardView>(_handViews));

            // ---- 「临时卡」角标（规则书 :183/:229）--------------------------------
            // **判据来自引擎**（`BattleContext.IsEphemeral` —— 它同时管「卡自己带关键词」和
            // 「造出来的复制被标记成临时」两条路），表现层不自己算。
            // ⚠️ 这一句**必须在 `SetCards` 之后、`RefreshHandPlayable` 附近**：
            //    `match` 可能是**复用**的旧视图（`pool` 里捞出来的），它的角标状态是**上一轮的**
            //    —— 不每轮重刷的话，一张临时卡打出去之后，接手它那个视图的普通卡会**一直带着角标**。
            for (int i = 0; i < _handViews.Count && i < h.Count; i++)
                if (_handViews[i] != null) _handViews[i].ShowEphemeral(Ctx.IsEphemeral(h[i]));

            // ---- 🆕 2026-10-18（`A985④`）「由谁造出来的」（原版 `BattleCardUI.DisplayCreatedByText`）----
            // **判据来自引擎**（`BattleContext.CreatedByOf`；它记的是「效果造这张牌时正在结算的那张卡」，
            //   与原版 `CardScript.createdBy` 那三处写点同义 —— 见 `BattleContext._createdBy` 那一段），
            //   表现层不自己算。
            // ⚠️ 与上面那条角标**同一条理由**（也同一点位）：`match` 可能是**复用**来的旧视图，
            //   那行字是**上一轮的** —— 不每轮重推的话，「造出来的」那张打出去之后，接手它那个视图的
            //   普通卡会**一直带着这行字**。`SetCreatedBy` 内部按「名字没变就不动手」去重。
            // ⚠️ 这里推的是**手牌**这一档：原版那两个点亮状态（`inHandShowing=8` / `inHandPlaying=10`）
            //   都属于手牌，`SetCreatedBy` 的注释里如实标了我们没有状态机这条近似。
            for (int i = 0; i < _handViews.Count && i < h.Count; i++)
                if (_handViews[i] != null) _handViews[i].SetCreatedBy(Ctx.CreatedByOf(h[i]));

            RefreshHandPlayable();

            // **发牌入场**（`CardFeel.DealIn`）：新抽到的牌从**牌堆**飞进手里。
            // ⚠️ 必须排在 `SetCards` 之后 —— 那一步才把牌摆到手牌的位置上，
            //    在这之前读到的「目标位置」是卡自己的原点（原点在屏幕中心，会飞错方向）。
            if (animateFeel && _dealt.Count > 0)
            {
                var deck = LayoutSpace.ToWorld(MyDeckX01, MyDeckY01);
                foreach (var v in _dealt) if (v != null) CardFeel.DealIn(v, deck);
            }
        }

        /// <summary>这一轮新进手牌的视图（`SyncHand` 每轮重填）。自检断言用</summary>
        readonly List<CardView> _dealt = new List<CardView>();
        public int DealtCount { get { return _dealt.Count; } }
        public CardView DealtView(int i) { return i >= 0 && i < _dealt.Count ? _dealt[i] : null; }

        // ==================================================================
        //  敌方手牌 —— 原版 `PlayerHand` MB 4350 + `CardsHorizontalLayout` MB 4053
        //
        //  🔴 **2026-09-17 新加**：之前**这一整件都没有** —— 所以 `VarsGlobal.timeToDrawEnemyCard`
        //     一直没有落点（不是「单机用不上」，是**缺件**）。原版规格：`资料/敌方手牌_原版规格.md`。
        //
        //  原版那排牌**显示的是卡背**（`PlayerHand__SetupCardInHand.c:45` → `ShowCardBack(!isPlayer)`），
        //  所以我们这边用 `ImageQuad` 画卡背、**不建 `CardView`**（省掉立绘/卡框/文字那一整套）。
        //  ⚠️ 原版抽牌那一瞬还会绕 Y 翻 180°（`TurnCardAround`）；我们是 2D，**没做那一下**。
        // ==================================================================

        /// <summary>敌方手牌区（原版 MB 4053 那一份）。由 `BattleScene` 建好接上；没有就不建敌方手牌</summary>
        public HandLayout foeHand;

        readonly List<ImageQuad> _foeHandViews = new List<ImageQuad>();
        readonly List<ImageQuad> _foeDealt = new List<ImageQuad>();

        /// <summary>敌方手牌视图数（自检断言用）</summary>
        public int FoeHandCount { get { return _foeHandViews.Count; } }
        /// <summary>这一轮新进敌方手牌的视图数（发牌入场用）</summary>
        public int FoeDealtCount { get { return _foeDealt.Count; } }
        public ImageQuad FoeHandViewAt(int i) { return i >= 0 && i < _foeHandViews.Count ? _foeHandViews[i] : null; }

        /// <summary>敌方手牌那张卡的屏幕高度（世界单位）= 卡高 × `m_scale 0.54` × 分辨率缩放</summary>
        static float FoeCardWorldH
        {
            get { return CardView.Height * HandLayout.EnemyCardScale * LayoutSpace.Scale; }
        }

        /// <summary>敌方手牌那张**卡背**（不是卡身）的屏幕高度（世界单位）——
        /// = 卡背节点自己那个 rect 的高 × `m_scale 0.54` × 分辨率缩放。
        ///
        /// <para>🔴 **2026-10-19（A1150）**：原版敌方手牌露出来的是 **卡背节点**
        /// （`2DCard/Cardback Container/Cardback` = `CardbackNodeRectW × CardbackNodeRectH`，
        /// 见那个常量的注释），而我们原来把它画成了**卡身**那个矩形
        /// （`FoeCardWorldH` / `CardView.Width ÷ CardView.Height`）—— 两个不是同一个矩形：
        /// 卡身比卡背节点**窄 3.74%**（2.0927 vs 2.1739）、**高 6.21%**（3.3313 vs 3.1364）；
        /// 再加上第二段 `padding` 内缩，两边的画心差得更多。</para>
        ///
        /// <para>⚠️ **手牌那一层的 px 桥是「1 卡单位 = 1 世界单位」**（`CardView.Width × cardScale`
        /// = 手牌卡宽 165 px @108 px/单位；见 `HandLayout` 文件头）—— 与**牌堆**那一层
        /// （`DeckCardPx` 的 ×100）**不是同一个桥**，⛔ 别混用（铁律 5·c）。</para></summary>
        static float FoeCardBackNodeWorldH
        {
            get { return CardbackNodeRectH * HandLayout.EnemyCardScale * LayoutSpace.Scale; }
        }

        /// <summary>敌方手牌卡背的**几何**（两段式）—— 尺寸与画心偏置都出自它。
        /// 框 = 卡背节点自己那个 rect（高 `FoeCardBackNodeWorldH`、宽 = 高 × `CardbackRectAspect`），
        /// 算法 → `Core/CardbackFace.cs`（**本仓唯一一份口径**，⛔ 别在这里另写一套）。
        /// 返回 `false` = 没有可用的几何（贴图为 null / 框非正）⇒ 调用方**不改几何、不挪位**
        /// （此时四个出参是 `Fit` 给的退化值：`w=boxW`、`h=boxH`、偏置 0）。</summary>
        static bool FoeHandBackGeom(Texture2D tex, out float w, out float h, out float dx, out float dyUp)
        {
            float boxH = FoeCardBackNodeWorldH;
            return CardbackFace.Fit(tex, boxH * CardbackRectAspect, boxH, out w, out h, out dx, out dyUp);
        }

        void SyncFoeHand()
        {
            if (foeHand == null) return;
            var h = Ctx.Players[1 - _me].Hand;
            _foeDealt.Clear();

            // 🔴 **2026-10-19（A1150）**：卡背那一格的**尺寸 / 比例 / 画心偏置**一律出自
            //   `CardbackFace.Fit`（两段式，本仓唯一一份口径 → `Core/CardbackFace.cs`）；
            //   取图仍走 `CardArt.CardBack`（缺图那边会出声），本处不再自己 `SetAspect(卡身比例)`。
            //   ⚠️ 一把牌里每张用的都是**同一张**卡背（按 `_foeFaction` 取）⇒ 几何算**一次**就够。
            var backTex = CardArt.CardBack(_foeFaction);
            float backW, backH, backDx, backDyUp;
            bool backGeom = FoeHandBackGeom(backTex, out backW, out backH, out backDx, out backDyUp);

            // 卡背视图按需增减（手牌只会一张张长；打到上限就停）
            while (_foeHandViews.Count < h.Count)
            {
                // 给的是**卡背节点**的高（`FoeCardBackNodeWorldH`）—— `A1150` 之前给的是卡身的高。
                var q = ImageQuad.Create(foeHand.transform, backTex, Vector3.zero,
                                         backGeom ? backH : FoeCardWorldH, new Vector2(0.5f, 0.5f),
                                         "FoeHand_" + _foeHandViews.Count);
                if (q == null) break;                       // 没卡背图就整个不建（别静默建一半）
                // 画心**不是**「铺满节点」：节点比例 == 该 sprite 的 `m_Rect` 比例 ⇒ 第一段（定框）
                // 是空操作，真正起作用的是第二段（按 `padding` 内缩 + 挪位）。
                if (backGeom) q.SetAspect(backW / backH);
                _foeHandViews.Add(q);
                _foeDealt.Add(q);
            }
            while (_foeHandViews.Count > h.Count)
            {
                var q = _foeHandViews[_foeHandViews.Count - 1];
                _foeHandViews.RemoveAt(_foeHandViews.Count - 1);
                if (q != null) Kill(q.gameObject);
            }

            int n = _foeHandViews.Count;
            for (int i = 0; i < n; i++)
            {
                var q = _foeHandViews[i];
                if (q == null) continue;
                var tr = q.transform;
                var rot = Quaternion.Euler(0f, 0f, foeHand.RotationAt(i, n));
                // 画心 = 槽位 + 偏置。🔴 偏置要**跟着张角转进卡自己的局部系**：原版那一跳是那颗
                // `Image` 的**网格**在自己的 rect 里按 `padding` 内缩 —— 网格当然跟着节点一起转；
                // 直接加在父件系里（不转）会让偏置与卡的姿态不一致（`A1150`）。
                // ⚠️ `rot * (dx, dyUp, 0)`：`dyUp` 向上为正（uGUI 的方向）—— 手牌这一层的世界坐标
                //    y 也是**向上**为正（`HandLayout.SlotPosition` → `LayoutSpace.ToWorld(nx, yFromBottom)`）
                //    ⇒ 这里**直接加**，⛔ 别反号（同 `FitDeckPile` 那条）。
                tr.localPosition = foeHand.SlotPosition(i, n) + rot * new Vector3(backDx, backDyUp, 0f);
                tr.localRotation = rot;
                tr.localScale = Vector3.one;
            }

            // 发牌入场：从**敌方牌堆**飞进手牌，时长 = `VarsGlobal.timeToDrawEnemyCard` = 0.15
            if (animateFeel && _foeDealt.Count > 0)
            {
                var deck = LayoutSpace.ToWorld(FoeDeckX01, FoeDeckY01);
                foreach (var q in _foeDealt) if (q != null) CardFeel.DealInQuad(q, deck);
            }
        }

        /// <summary>付不起/不能打的牌置灰 —— **判据全部来自引擎**，这里不自己算费用</summary>
        void RefreshHandPlayable()
        {
            bool myTurn = Ctx.Active == _me && !Ctx.IsOver;
            var p = Ctx.Players[_me];
            bool hasSlot = p.HasFreeSlot();

            for (int i = 0; i < _handViews.Count && i < p.Hand.Count; i++)
            {
                if (_handViews[i] == null) continue;
                var inst = p.Hand[i];              // 第 7 行第 2 步：手牌存实例
                var card = inst.Card;
                bool playable = myTurn && card.IsUnit && card.Cost <= p.Energy && hasSlot;
                _handViews[i].SetHighlight(playable ? CardHighlightState.Normal
                                                    : CardHighlightState.Unplayable);
                // 🆕 2026-09-19：原版卡面那圈 **`_Outline` 高亮描边**（打得出去才有，按 trait 分色、补间 0.2s）。
                //    判据只在 `CardHighlight.OutlineOf` 一处；这里把**只有引擎知道的**两样算好传进去：
                //      · `inst.DrawnThisTurn` —— 这一**份**是不是本回合从牌库抽到的（`Teleport` 那条要用它）；
                //      · `oathOk` —— 原版判据是 `manaLeft >= cost + oath值`（`CardDef.OathCost` 就是我们这边那个 N）。
                bool oathOk = card.OathCost > 0 && p.Energy >= card.Cost + card.OathCost;
                _handViews[i].SetOutline(CardHighlight.OutlineOf(card, playable, inst.DrawnThisTurn, oathOk));
            }
        }

        CardData ToCardData(UnitState u, string faction)
        {
            var fb = u.Card != null ? FaceTextFull(u.Card) : new FaceBody(DescribeKeywords(u), "desc");
            // 🆕 场上的 buff/debuff 徽标（原版 `BattleCardUI.UpdateTraitIcons`）——
            //    喂的是**当前**关键词表（加/减益、光环、限时增益到期全跟着变），
            //    顺序用卡面效果文字当提示（玩家在卡上读到的词序），判据与出处见 `Core/Badges.cs`。
            //    另外两样（角标的「带不带数值」、图标的「激活/未激活」）也都照原版那两条判据传进去。
            var badges = Badges.For(u.Keywords, fb.body,
                                    u.Card != null ? u.Card.NumericKeywords : null,
                                    key => KeywordActive(u, key));
            return new CardData
            {
                // `id` 保持英文 —— 它兼着**显示名 / 配对**的活（`SyncHand` 按它对名字），**别拿它取图**
                id = u.Name,
                // 立绘/抠图清单的文件键：**引擎卡 id**（2026-09-15 起立绘按 id 命名）
                artId = u.Card != null ? ArtKey(u.Card) : null,
                title = CardText.Name(u.Name, u.Card != null ? u.Card.NameZh : null),
                cost = -1,
                melee = u.Attack,
                ranged = u.RangedAttack,
                health = u.Health,
                armor = u.Armor,          // 场上是**当前**护甲（会被效果改），原版卡面显示的也是当前值
                keywords = fb.body,
                badges = badges,
                isUnit = true,
                frame = FactionColor(faction),
                faction = faction,
                rarity = u.Card != null ? u.Card.Rarity : null,   // 卡框按稀有度分四档
                subtype = u.Card != null ? u.Card.Subtype : "",     // 卡面下方的兵种行（`RaceText`）
            };
        }

        /// <summary>
        /// 立绘 / 抠图清单的**文件键**。
        /// · **原版卡**：引擎卡 id（`UM82` 这种）—— 2026-09-15 起立绘按 id 命名
        ///   （原来按卡名，同名跨阵营会互相覆盖，见 `CardData.artId`）。
        /// · **我们自己设计的那 26 张**：没有引擎 id（也不在卡表里），`import_original_art.py`
        ///   的 `PORTRAITS` 就是按**卡名**导的 ⇒ 用卡名。
        /// ⚠️ 判据只有 `FromOriginalPool` **一处**，别在别处再写第二份。
        /// </summary>
        public static string ArtKey(CardDef c)
        {
            if (c == null) return null;
            return c.FromOriginalPool ? c.Id : c.Name;
        }

        /// <summary>
        /// 🆕 2026-09-29 **徽标的「激活 / 未激活」**（原版 `CardTrait.IsActive(card)`，
        /// 就是 `BoardTraitIcon.Initialize` 的第 4 个参数）。
        ///
        /// 🔴 **原版只有两个子类覆写了它**（`d:/2/tools/decomp_full/`，逐个读过）：
        /// · `CardTraitDuty__IsActive.c:19` = `return *(char *)(card + 0x4c) == 0;`
        ///   —— `0x4C` = `EntityScript.usedActiveAbility`（`dump.cs` 字段表）
        ///   ⇒ **本回合用过主动能力 ⇒ Duty 变灰**。
        /// · `CardTraitOath__IsActive.c:26` = `CardScript.CanUseOathAbility(card)`
        ///   ⇒ **当前激活不了誓约（次数用完 / 不是本回合上场 / 付不起那 N 点能量）⇒ Oath 变灰**。
        /// · 其余**全部恒 true** —— `CardTrait__IsActive.c` 的符号被 Ghidra 撞到了别的函数上
        ///   （正文是 `return 1;`），基类返回 false 是不可能的（那会让**所有**徽标都变灰）⇒ 按 true 落地。
        ///   ⚠️ `CardTraitFerocity__IsActive.c` **同一个撞击符号**，所以 `Ferocity` 那一条**判不了**，
        ///   我们按「基类恒 true」处理，如实记在这儿（不猜）。
        ///
        /// ⚠️ **别拿 `Exhausted` 顶替 `Duty` 那一条** —— 攻击也会置 `Exhausted`，而原版只认「主动能力」。
        /// </summary>
        bool KeywordActive(UnitState u, string key)
        {
            if (key == KeywordTable.Duty) return !u.UsedActiveAbilityThisTurn;
            if (key == "oath")
            {
                int ow, sl;
                if (!FindOnBoard(u, out ow, out sl)) return true;   // 不在场上 ⇒ 原版「非 CardScript ⇒ true」
                return RuleCore.CanUseOathAbility(Ctx, ow, sl) == RuleCodes.OK;
            }
            return true;
        }

        /// <summary>在棋盘上找这个单位，拿它的 owner / slot（徽标判据要问引擎）。
        /// 同一个 `UnitState` 在场上只会出现一次（引擎的实例身份），所以按引用比就够，不必比名字。</summary>
        bool FindOnBoard(UnitState u, out int owner, out int slot)
        {
            for (int p = 0; p < 2; p++)
            {
                var b = Ctx.Players[p].Board;
                for (int s = 0; s < b.Length; s++)
                    if (ReferenceEquals(b[s], u)) { owner = p; slot = s; return true; }
            }
            owner = -1; slot = -1;
            return false;
        }

        /// <summary>⚠️ public static 是给 `CardFaceProbe`（单卡渲染量尺）用的：卡面数据必须**只有这一条路**。</summary>
        public static CardData ToCardData(CardDef c, string faction)
        {
            return new CardData
            {
                id = c.Name,                       // 同上：显示名/配对用它
                title = CardText.Name(c.Name, c.NameZh),
                cost = c.Cost,
                melee = c.Attack,
                ranged = c.RangedAttack,
                health = c.Health,
                armor = c.KwValue(KeywordTable.Armour),   // 手牌里显示的是卡面印的护甲
                keywords = FaceTextFull(c).body,          // 记号已换成 `<sprite …>`
                artId = ArtKey(c),
                isUnit = c.IsUnit,
                frame = FactionColor(faction),
                faction = faction,
                rarity = c.Rarity,                 // 卡框按稀有度分四档
                subtype = c.Subtype,               // 卡面下方的兵种行（**印不印由 CardView.SubtypeLine 判**）
                type = c.Type,                     // 判「印不印兵种行」要用它（`hero`/`defence` 也印）
            };
        }

        /// <summary>卡面效果正文 + 它出自哪个字段 + 卡表稳定 id（**图标计划表按 id 查**）。</summary>
        public struct FaceBody
        {
            public string body;
            /// <summary>`"descZh"` 或 `"desc"`（空正文时是 `"desc"`）</summary>
            public string field;
            public FaceBody(string b, string f) { body = b; field = f; }
            /// <summary>⚠️ 故意留的隐式转换：调用方（自检里那些字符串断言）当它是 string 用。</summary>
            public static implicit operator string(FaceBody f) { return f.body; }
        }

        /// <summary>
        /// 卡面那行小字 —— **卡面文案只有这一条路**（`CardFaceProbe` 也走它，另写一份迟早不一致）。
        /// · **原版卡**（`FromOriginalPool`）：写**它自己的效果原文**
        ///   （`DescZh` 有**且当前语档是中文**就用中文，否则英文 `Desc`）—— 原版卡面上印的就是这段字。
        /// · **我们自己设计的 26 张**：写**引擎结算得到的关键词**（`Desc` 对我们那 26 张是风味文字，不是效果）。
        /// 两边都**把引擎结算不了的部分打 `*` 附在后面**（红线：不许静默失败）。
        ///
        /// 🔴 **卡面图标就在这一处换掉**（`card_icon_plan.json` 按**稳定 id + 字段名**查，
        /// 见 `资料/卡面图标_现状与缺口.md` §二之三）。为什么放在这里：
        /// **所有**画这段文字的地方（卡面 / 放大展示窗 / 单卡探针）都经过 `CardData.keywords` ——
        /// 分散到各个渲染器里做，迟早漏一处，变成「有的地方有图标有的地方没有」。
        /// 查不到 / 计划表里记的是缺口 ⇒ **原样返回**（红线：宁可难看，不给错图标）。
        /// </summary>
        public static FaceBody FaceTextFull(CardDef c)
        {
            if (c == null) return new FaceBody("", "desc");
            if (!c.FromOriginalPool) return new FaceBody(DescribeKeywords(c), "desc");

            // 🔴 **2026-10-18（同批顺带修，铁律 5）**：原来写 `bool zh = !string.IsNullOrEmpty(c.DescZh);`
            //   —— 那是「**这张卡有没有中文**」，**不是**「**当前语档是不是中文**」⇒ **绕过 `CardText.Zh`**：
            //   切成 English 之后卡名（`ToCardData` 走 `CardText.Name`）变英文、而**卡面正文仍印中文**
            //   （半中半英）。这是 `Core/CardText.cs` 那一族缺陷的**第三处**，见 `CardDisplayName` 的 doc。
            //   ⚠️ `zh` 同时决定 `CardIcons.Rewrite` 取哪一份图标计划（`descZh` / `desc`）——
            //   英文档下本来就该用英文那一份 ⇒ 这一改**顺带把图标也对齐**。
            bool zh = CardText.Zh && !string.IsNullOrEmpty(c.DescZh);
            string body = zh ? c.DescZh : c.Desc;
            string notes = string.Join(" ", UnimplementedNotes(c).ToArray());
            if (string.IsNullOrEmpty(body)) return new FaceBody(notes, "desc");

            // 🔴 **图标就在这一处换掉**（`card_icon_plan.json` 按**稳定 id + 字段名**查）。
            //    查不到 / 计划表里记的是缺口 ⇒ **原样返回**（红线：宁可难看，不给错图标）。
            string core = CardIcons.Rewrite(c.Id, zh ? "descZh" : "desc",
                                            string.IsNullOrEmpty(notes) ? body : body + "  " + notes);
            // 🆕 **关键词段补在最前面**（原版卡面就是「关键词 → 效果文字」这个顺序，
            //    见 `资料/PnP卡图_逐张对账_0915.md` §四·E：1130 张里 306 张的关键词
            //    在 `desc` 里一个字都没有）。
            //    ⚠️ 判据用**原始 body**，不能用 `core` —— 那里面已经多了 `<sprite …>` 标签，
            //       会把它自己补的那一段又判成「已经印过」。
            //    🔴 **`c.NumericKeywords` 必须传**（`A1341`）：`KeywordSegment` 判「这个词印不印数字」
            //       走 `Badges.CarriesValue` 的三档并集，其中**「卡面原文里写了数字」那一档的来源就是这个
            //       `c.NumericKeywords`** —— 不传（吃默认的 `null`）⇒ 那一档**静默失效**、
            //       与徽标（`Badges.For`）那一路裂开。⛔ 只改 `KeywordSegment` 的签名而不改这一行
            //       = 静默偏一半（`A1332` 立的账，`A1341` 两处同批改）。
            string seg = CardText.KeywordSegment(c.Keywords, zh, body, c.NumericKeywords);
            return new FaceBody(seg + core, zh ? "descZh" : "desc");
        }

        /// <summary>关键词 → 卡面上那行小字（只列**引擎真的会结算**的）。
        /// 文案走 `CardText`：拿得到中文字体就是中文，拿不到就是大写英文。</summary>
        static string DescribeKeywords(UnitState u)
        {
            var parts = new List<string>();
            if (u.Has(KeywordTable.Vanguard)) parts.Add(CardText.Keyword(KeywordTable.Vanguard));
            if (u.Has(KeywordTable.Flying)) parts.Add(CardText.Keyword(KeywordTable.Flying));
            if (u.Has(KeywordTable.Stealth)) parts.Add(CardText.Keyword(KeywordTable.Stealth));
            if (u.Has(KeywordTable.LongRange)) parts.Add(CardText.Keyword(KeywordTable.LongRange));
            if (u.Armor > 0) parts.Add(CardText.Keyword(KeywordTable.Armour) + " " + u.Armor);
            if (u.HasShield) parts.Add(CardText.Keyword(KeywordTable.Shield));
            parts.AddRange(EffectNotes(u.Card));
            parts.AddRange(UnimplementedNotes(u));
            return string.Join(" ", parts.ToArray());
        }

        static string DescribeKeywords(CardDef c)
        {
            var parts = new List<string>();
            if (c.Has(KeywordTable.Vanguard)) parts.Add(CardText.Keyword(KeywordTable.Vanguard));
            if (c.Has(KeywordTable.Flying)) parts.Add(CardText.Keyword(KeywordTable.Flying));
            if (c.Has(KeywordTable.Stealth)) parts.Add(CardText.Keyword(KeywordTable.Stealth));
            if (c.Has(KeywordTable.LongRange)) parts.Add(CardText.Keyword(KeywordTable.LongRange));
            if (c.Has(KeywordTable.Armour)) parts.Add(CardText.Keyword(KeywordTable.Armour) + " " + c.KwValue(KeywordTable.Armour));
            if (c.Has(KeywordTable.Shield)) parts.Add(CardText.Keyword(KeywordTable.Shield));
            parts.AddRange(EffectNotes(c));
            parts.AddRange(UnimplementedNotes(c));
            return string.Join(" ", parts.ToArray());
        }

        /// <summary>
        /// 触发类关键词 + 主动技能 → 卡面小字（`RALLY DMG1 FOE` / `ABILITY HEAL2 OWN`）。
        ///
        /// ⚠️ 带了关键词但**效果原文解析不出来**的，照样列出来、但打成 `*`
        ///    （`RALLY*`）。「关键词实现了」和「这张卡的效果能跑」是两件事 ——
        ///    不标的话玩家会以为它有作用，那比不显示更不诚实。
        /// </summary>
        static List<string> EffectNotes(CardDef c)
        {
            var list = new List<string>();
            if (c == null) return list;

            string[] triggers =
            {
                KeywordTable.Rally, KeywordTable.Strike, KeywordTable.Slay,
                KeywordTable.Backlash, KeywordTable.Penitence,
            };
            foreach (var kw in triggers)
            {
                if (!c.Has(kw)) continue;
                var spec = c.Effect(kw);
                list.Add(spec != null ? CardText.Keyword(kw) + " " + CardText.Effect(spec)
                                      : CardText.Keyword(kw) + "*");
            }

            if (c.Has(KeywordTable.Ability))
                list.Add(c.Ability != null
                         ? CardText.Keyword(KeywordTable.Ability) + " " + CardText.Effect(c.Ability)
                         : CardText.Keyword(KeywordTable.Ability) + "*");

            return list;
        }

        /// <summary>
        /// 卡上带了但**本版引擎不结算**的关键词，用 `*` 标出来。
        /// 不标的话玩家会以为它有作用 —— 这比直接不显示更诚实。
        /// </summary>
        static List<string> UnimplementedNotes(UnitState u)
        {
            var list = new List<string>();
            foreach (var kv in u.Card.Keywords)
                if (!KeywordTable.Implemented.Contains(kv.Key))
                    list.Add(CardText.Keyword(kv.Key) + "*");
            return list;
        }

        static List<string> UnimplementedNotes(CardDef c)
        {
            var list = new List<string>();
            foreach (var kv in c.Keywords)
                if (!KeywordTable.Implemented.Contains(kv.Key))
                    list.Add(CardText.Keyword(kv.Key) + "*");
            // 战术卡的**效果文字**能不能结算，和「关键词实现了没有」是两件事：
            // 关键词全实现了，效果照样可能解析不出来（原版卡面是英文自然语言）。
            // 解析不了的就在卡面打 `*` 说明白 —— 红线：不许静默失败。
            // 判据共用 `EffectText.IsFullyParsed`（和 `CanPlayTactic` / `DeckBuilder.TacticPlayable` 同一份）。
            //
            // 🔴 **2026-09-16：不再只判战术卡**（原来是 `if (c.Type == "tactic")`）——
            //    单位卡 / 督军卡 / 防御卡的问题**卡面永远看不见**，而那正是红线要挡的东西。
            //    实测（`_tmp_view/willrun_mechanism.md`）：改之后多出的 `*` 落在
            //    **unit 7 张 + defence 3 张**（hero 56/56 全通），面很小、值得。
            //    ⚠️ 判据同时换成 `EffectText.WillRunOps`（**按卡类型问对的层**，见那边的注释）——
            //       单位卡/督军卡**不许走 `IsFullyParsed(c.Desc)`**：它们的 `desc` 带
            //       `Rally:` / `When <事件>,` 前缀，主解析器永远判不「完全解析」，
            //       那是**问错了层**（2026-09-16 中文对账那轮 12 条假阳性的来源）。
            if (c.Type == "tactic" || c.Type == "defence")
            {
                // ⚠️ 两条**合成一句**（别打两个 `*`）—— 有的卡两样都占
                //    （`Mekaniak` 那种：解析得出来、载荷没机制）。
                bool parsed = EffectText.IsFullyParsed(c.Desc);
                if (!parsed) list.Add("效果本版结算不了*");
                else if (MechanismFails(c)) list.Add("效果本版没作用*");
            }
            else if (MechanismFails(c))
            {
                // `unit` / `hero`：只判「会执行的那一层有没有机制」，
                // **不判** `IsFullyParsed`（理由见上）。
                list.Add("效果本版没作用*");
            }
            return list;
        }

        // ── 「解析得出来、但载荷没有机制」那一条（2026-09-15 补）

        /// <summary>
        /// 🔴 **卡面那个 `*` 原来只看「解析得出来没有」，不看「载荷有没有机制」**
        /// ⇒ ③ 栏那几张（能打出去、但打了什么也不会发生，如 `Mekaniak`）**卡面不打 `*`**，
        /// 只有覆盖率报表看得见 —— 正是红线禁止的「玩家以为它有作用」。
        /// 判据转调 <see cref="EffectText.OpHasMechanism"/>（**与覆盖率报表同源，不写第二份**）。
        ///
        /// ⚠️ 判据源换成了 **`EffectText.WillRunOps`**（2026-09-16）——
        ///    原来是 `EffectText.Parse(c.Desc)`，那对**单位卡是问错了层**
        ///    （它们的正文在事件层/触发层里，主解析器的输出是没人执行的残渣）。
        ///    对 `tactic`/`defence` 两者**是同一份东西**（`WillRunOps` 内部就走主解析器）。
        /// ⚠️ `createPool` 传**全卡池** —— 覆盖率报表也是这么传的
        /// （`RuleEngineTest.cs` 三处 `EffectText.Coverage(pool, t, pool)`），两边必须一致。
        /// ⚠️ 结果**按卡 id 缓存**：卡面每次刷新都会问一遍，而解析一段自然语言不便宜。
        /// </summary>
        static readonly Dictionary<string, bool> _mechFail = new Dictionary<string, bool>();

        static bool MechanismFails(CardDef c)
        {
            if (c == null || string.IsNullOrEmpty(c.Id)) return false;
            bool fail;
            if (_mechFail.TryGetValue(c.Id, out fail)) return fail;

            var pool = CardDatabase.Load();            // 懒加载 + 之后走缓存
            var ops = EffectText.WillRunOps(c);
            fail = false;
            foreach (var op in ops)
            {
                string why; bool imprecise;
                if (!EffectText.OpHasMechanism(op, c.Faction, pool, out why, out imprecise))
                { fail = true; break; }
            }
            _mechFail[c.Id] = fail;
            return fail;
        }

        // ==================================================================
        //  HUD
        // ==================================================================

        void BuildHud()
        {
            // ⚠️ **只能建一次**：HUD 的结构不随对局变，变的只是字。
            //    重开一局（「按 R 重开」）会再走一遍 `Begin()` → `BuildHud()`，
            //    再建一份的话屏幕上会**叠两层 HUD**，而且**旧的那个结算面板成了孤儿**——
            //    `_endPanel` 已经指向新建的那个，旧面板再也没人 `Hide()`，就一直挂在画面上
            //    （2026-09-12 接「再来一局」时截图抓到的：新一局已经开打，上一局的
            //     「对局结束 / 三个骷髅 / 2/3」还压在战场上）。
            if (_hudBuilt) return;
            _hudBuilt = true;

            // 🔴 **2026-10-11（A218）判「不改」**（这一处**故意**保持**裸 `Transform`**）：
            //    判据 = 原版这一件就是裸 `Transform` —— `bundle_scenes_scenes_battlearena1` 实读：
            //    `BattleHud`（go_pid 239）= **`Transform`**（HUD 那一整棵树是它的孙辈，uGUI 的 `Canvas`
            //    才在它下面），而不是 `RectTransform`。
            //    ⚠️ 它与「窗口根」不同族：我们的 `HudRoot` 就是原版 `BattleHud` 那一级（**不是** `Canvas`）
            //    ⇒ 补 `RectTransform` + 编一个尺寸 = 造一个**与原版相反**的类型（铁律 3：查不到就别编）。
            var hudGo = new GameObject("HudRoot");
            hudGo.transform.SetParent(transform, false);
            hudRoot = hudGo.transform;
            var root = hudRoot;      // 下面所有 HUD 件都挂这个根（原来直接挂 `transform`）
            CardArt.Load();
            // 🆕 2026-10-01（§三 第 26 条 · ⑥ 的接线那半）：**把补间钩子挂上** ——
            //   原来 `WFModuleTween.OnInvoke` 全仓没人赋值 ⇒ 138 个 `AnimFXModuleTween` 的请求**全落在
            //   `DroppedRequests` 里**（不静默，但也没人接）。判据与契约 → `Core/UnitTweenRuntime.cs`。
            // 🔴 **2026-10-13（A515）**：那两个解析口（`UnitTweenRuntime.HeroBySeat` / `SeatOf`）
            //   原来就挂在这一行附近 —— **已挪进 `Begin()`**（`HookUnitTweenResolvers()`）。
            //   原因：`BuildHud` 被上面那个 `_hudBuilt` 闩住 ⇒ 挂在这儿只对**第一次** `Begin()` 生效，
            //   而 `DetachStaticHooks()` 会把它们置 null ⇒ 重开一局 / 重连重建时那两个口是 null。
            //   它们要的只是「驱动知道视图」，与 HUD 结构无关 ⇒ 归到 `HookAnimFx*` 那一族。
            // ⚠️ `Install()` **留在这儿**：它装的两条不指着 driver（理由见 `ForEachStaticHook` 的 ③ 段）。
            UnitTweenRuntime.Install();

            // 🔴 **2026-09-17 下移**：原来是 `0.965`（距顶 38 px）—— 那正好在**敌方手牌**那一排里
            //    （敌手卡区是屏幕顶部 0~194 px、水平正中，见 `HandLayout.EnemyBaselineY`），
            //    截图里三张卡背把「第 N 回合」压掉了一半。
            //    ⚠️ **这一行本身是我们自加的**（原版没有 `TurnLabel`）⇒ 该让位的是它，不是敌方手牌。
            //    新位置 0.81（距顶 205 px）：在敌手之下、敌方棋盘（卡顶 357 px）之上。
            _turnLabel = Hud(root, "", 0.5f, 0.810f, 4,
                             new Color(0.95f, 0.95f, 0.98f), new Vector2(0.5f, 1f), "TurnLabel");

            // ---- 等待提示（原版 `WaitText`）----
            // 位置/尺寸照原版字段（锚 (0.5,1) + (7,−175.1)、1344×79.4），理由与两处「我们挑的」见 `WaitBanner.cs` 文件头。
            _waitBanner = WaitBanner.Create(root);

            // 🔴 **2026-09-27：回放控制条的显隐改对了** —— 原版 `ReplayHud.Setup()` 只做一件事：
            //    `objHolder.SetActive(BattleManager.matchType == 0xA0)`（`0xA0 = 160 = MatchType.Replay`）。
            //    我们**从不进回放局** ⇒ 传 false：**那 4 颗钮在普通对局里【不该出现】**（原来一直摆着，是错的）。
            //    判据 → `资料/普查产出_0927/回放_界面真值.md` §B/§C 与 `ReplayBar.cs` 文件头。
            //    ⚠️ 我们原来那套「本局时间控制」（暂停/单步/重开）**挪到键盘**了，见 `Update` 里的按键段。

            // ---- 名牌：原版左上是对手、左下是自己 ----
            // ⚠️ **2026-09-13 更正：原来这两个位置是错的**。旧值（`EnemyInfo (157,108)` / `PlayerInfo (32,977)`）
            //    抄的是 `FrontCanvas/Alliance Panel` 底下**另一份** `EnemyInfo`/`PlayerInfo`（`activeInHierarchy=False`）
            //    的实例，**不是** `LeftArea` 下 HUD 那一份 —— 两份实例不是一个东西。错出来的后果：
            //    我方名牌**偏右 44 px、偏高 37 px**，敌方名牌**偏右 ~168 px**（截图一量就看得出来），
            //    而且我方那个高度正好让里程碑骷髅压住「生命 N」那行字。
            //
            // 🔴🆕 **2026-10-18（第十五轮 · `G5`）：`FrontCanvas/Alliance Panel` 那块【要不要建】本批核了 ——
            //   结论：要做，但**不是本地化**那一档，本笔只记判据（⛔ 没动手建整扇窗）。**
            //   · 它**不是「单机下永远不显示」**：`(inactive)` 只是**出厂默认**，真正控制它的是
            //     `BattleHud.AlliancePanelOpenButton`（`BattleHud__AlliancePanelOpenButton.c`）——
            //     HUD 上那颗钮点了就 `BattleAlliancePanel.Open(name, allianceName, avatar, …)`，
            //     里面 `GameObject.SetActive(true)` + `CanvasGroup` `DOFade` + `DOScale`（判据逐句读过）。
            //   · 它显示的是**对手的档案**：`BattleManager.matchData`（`+0x10` 名字 / `+0x50` 联盟名 /
            //     `+0x40`、`+0x48` 头像 id …）。**无联盟**那一支才是 `Battle/AlliancePanel/NotInAnAlliance`：
            //     节点 `NotInaAllianceText`（`bundle_scenes_scenes_battlearena1/MonoBehaviour_4475.json`，
            //     TMP 静态原文 = **`This player is is still not part of an Alliance`**，注意原版那个 `is is` 重复）；
            //     有联盟时走 `Alliance Name` / `Alliance Badge Drawer` / `Alliance Rating Display`。
            //   · ⇒ **我们单机的 bot 对手没有档案（没有头像/联盟）**，接上去就**恒走 `NotInAnAlliance` 那一支**
            //     ——那正是原版单机情境下的形态，所以**该建**；但整扇窗（815×475 · Background/BGFrame/
            //     EnemyInfo/Avatar Item Small/Alliance 一组/关闭钮）是**一件新的 UI 活**，
            //     ⛔ 不属本笔「本地化剩余」，已如实记进交件报告。
            //   · 面板的节点判据已就位：`资料/说明书/01_战斗_对战/2D层_battlearena1全树.md:756-779`
            //     （`Alliance Panel (inactive) [-107,1022 815x475]`，13 个战场同构）。
            // 正确的绝对 rect（出处：`资料/战斗规格/战斗重建_0827/子代理读报_back左区_0827.md:46,56,57,69`）：
            //   `NameBackground` 435.7×126.3、`PreserveAspect=1` → 实绘 382.3×126.3（**居中于 rect**）：
            //     我方 rect x[−38.1,397.6] y[951.4,1077.7] → 实绘左缘 −11.4、中心 y(从上) 1014.55
            //     敌方 rect x[−37.9,397.9] y[ 15.6, 141.9] → 实绘左缘 −11.2、中心 y(从上)   78.75
            //   ⇒ **两边都贴着屏幕左缘、还各自出血 11 px（原版就长这样）**，不是我们原来那样离左边 33/157 px。
            //   `PlayerNameText` 文本框 x[112.6,361.9] y[977.0,1021.9]、**H=居中**、fs 35 →
            //     文字中心 (237.25, 999.45)；`EnemyNameText` x[72,435.7] y[40.9,86.9]（左右 margin 43.26/70.58）
            //     → 有效文字中心 (240.19, 63.9)。所以文字**要按中心摆**，不是左对齐。
            var dim = new Color(0.86f, 0.88f, 0.93f);
            // 名牌尺寸：原版 `NameBackground` 435.7×126.3，贴图 `UI_Player_Frame` 是 442×146（比例 3.027），
            // PreserveAspect 后实际绘 **382.3×126.3** → worldHeight = 126.3/108 = 1.1694。
            // ⚠️ 原来给的是 0.75（= 81 px 高），比原版**小 36%**。
            _enemyPlate = HudImage(root, "UI_Player_Frame", -0.005833f, 0.927083f,
                                   new Vector2(0f, 0.5f), 126.3f / 108f, "EnemyPlate");
            // 🔴 **2026-10-10（A1213①）**：框/四格 ← 原版 `LeftArea/EnemyInfo/EnemyName/EnemyNameText`
            //    （`RectTransform/RectTransform_3568.json`；框 290.94×36.84 是**解算后**的屏幕矩形
            //     —— 那颗的父 `EnemyName`（`RectTransform_3083.json`）自带 `m_LocalScale = 0.8`，
            //     见 `Hud` 的 doc 那张表）。
            _enemyText = Hud(root, "", 0.125099f, 0.940833f, 3, dim, new Vector2(0.5f, 0.5f), "EnemyPlateText",
                             290.94f, 36.84f, 2f, 35f, 36f, 0);
            _myPlate = HudImage(root, "UI_Player_Frame", -0.005938f, 0.060602f,
                                new Vector2(0f, 0.5f), 126.3f / 108f, "PlayerPlate");
            // 框/四格 ← 原版 `LeftArea/PlayerInfo/PlayerName/PlayerNameText`
            // （`RectTransform/RectTransform_3016.json`；框 199.38×35.93 同为**解算后**的屏幕矩形，
            //  父 `PlayerName` 也带 `m_LocalScale = 0.8`）
            _myText = Hud(root, "", 0.123568f, 0.074583f, 3, dim, new Vector2(0.5f, 0.5f), "PlayerPlateText",
                          199.38f, 35.93f, 2f, 35f, 36f, 0);

            // ---- 我方名牌上的**里程碑**：原版 `LeftArea/PlayerInfo/Milestones` = `BattleScoreUiManager` ----
            // 一个骷髅 + `x N`（`MatchSkulls Icon` / `MatchSkulls Score`）。原版还有 tooltip
            // （`Tips/Hud/Skulls`，`MonoBehaviour_4941.json`）—— 我们没做 tooltip。
            // 位置用**原版绝对坐标**（不是挂在我们的名牌上算的）：见上面 `SkullIconX01` 那组常量的注释。
            // ⚠️ 顺带查出来的偏差（**没动它**）：我们的名牌比原版高 37 px、右 44 px ——
            //    原版 `NameBackground` 是以 `PlayerInfo` 为**中心**摆的（渲染宽 382.4 → 左缘 −11.5、
            //    中心 y 从下 65.5），我们把它按 `PlayerInfo` 的**左缘**(x=32) + 中心 y=102.6 摆了。
            //    要改的话改 `_myPlate`/`_myText` 那两行；里程碑按绝对值摆，将来对齐了也不用动。
            // ⚠️ 骷髅要给一个**比 HUD 图更近的 z** —— 它跟名牌（`_myPlate`）几乎重叠，
            //    同 z 就是同一个透明队列、距离也一样，谁压谁由渲染顺序决定。第一版就这么被名牌整个盖住了
            //    （截图放大才看出来：`x0` 在、骷髅没了）。见 `HudImageZ` 的注释。
            _skullIcon = HudImageTex(root, CardArt.Ui("40k_battle_Win_Skull"), SkullIconX01, SkullIconY01,
                                     new Vector2(0.5f, 0.5f), Px(SkullIconPx), "MatchSkullsIcon",
                                     HudImageZ - 0.05f);
            // 🔴 **2026-10-10（A1213①）**：框/四格 ← 原版 `LeftArea/PlayerInfo/Milestones/MatchSkulls Score`
            //    （`RectTransform/RectTransform_3467.json`；框 94.46×47.31 = **纯 `m_SizeDelta`**
            //    —— 那颗 `anchorMin == anchorMax == (0.5,0.5)`，不拉伸）。
            //    ⚠️ 本颗是全 17 颗里**唯一锚点不是 `(0.5,0.5)`** 的（`anchor=(0,0.5)` = 左中）
            //    ⇒ 接上自适应之后**左缘逐位不动、右缘随字号动**（见 `Hud` 的 doc）。
            _skullScore = Hud(root, "", SkullScoreX01, SkullScoreY01, 3, Color.white,
                              new Vector2(0f, 0.5f), "MatchSkullsScore",
                              94.46f, 47.31f, 18f, 35f, 36f);
            // 🆕 **2026-10-16（A712 阶段 2）**：纵向档 = 原版 `MatchSkulls Score` 的 **`V = Midline`**
            //   —— 判据 = 上面 `SkullScoreX01` 那条 doc 自己逐字写着「原版 **H=左对齐 / V=Midline**、字号 fs 35」
            //   （出处 `子代理读报_back左区_0827.md` + `MonoBehaviour_5234,3785.json`）。
            //   `Midline` 不吃框高 ⇒ 只传档。文案恒 `"x"+骷髅数`（单行，`Label` 恒 `NoWrap`）。
            if (_skullScore != null) _skullScore.SetVAlign(Label.VAlign.Midline);

            // ---- 左下：能量宝石（原版 `40k_battle_energy_full/empty`）+ 数量 ----
            // ---- 能量 / 结束回合：**右侧一竖排**（原版 `RightArea/Right Anchor/Energy And turn holder`）----
            //
            // ⚠️ 2026-09-12 改：原来这两样都放在**左下角**（注释还写着「原版 Energy Player 也在左下角」——
            //    那句是没有出处的）。原版实测是一条**靠右的竖排**，从上到下：
            //      EnemyMana   97.7×97.7  绝对 x[1827.8,1903.9] y[249.8,327.4]（从上）
            //      Clock/TurnBtn 130.7×80.4  x[1782.9,1913.6] y[415.3,495.8]
            //      PlayerMana  97.7×97.7  绝对 x[1827.8,1903.9] y[517.1,594.1]
            //    出处：`资料/战斗规格/战斗重建_0827/战斗界面JSON权威表_0827.md:149-156`（绝对坐标表）
            //      + 运行时 dump `Energy And turn holder` 的 sizeDelta（本机重跑过，两者一致）。
            //    换算：x01 = 中心x/1920，y01 = 1 − 中心y(从上)/1080。
            // 大底板先铺（`HudImageZ` 让它比水晶远，压在水晶底下）
            //   原版 `Energy And turn holder` 自己的 rect：pos(5.6,−2.6) 尺寸 302.1×480.8、
            //   anchor(1,0.5) → 中心 x = 1920+5.6 = 1925.6（**右侧出血 156 px，原版就这样**）。
            //   ⚠️ 用 `HudDecorZ`（比别的图更远）—— 不然它会压住能量水晶，见那个常量的注释
            HudImageTex(root, CardArt.Ui("UI_Energy_Holder_big"), 1.00292f, 0.49759f,
                        new Vector2(0.5f, 0.5f), 480.8f / 108f, "EnergyHolderBig", HudDecorZ);
            // ⚠️ **2026-09-29 更正**：上面那句注释里「`Energy And turn holder` 自己的 rect：尺寸 302.1×480.8」
            //    —— **302.1×480.8 是它子级 `Background` 的尺寸，不是 holder 的**（holder 真身 **238.79×356.58**，
            //    见 `RectTransform_3359.json`；`Lights` 的 `m_SizeDelta` 还是 **(0,0)（拉伸占位）**，
            //    真尺寸 70.68×71.32 得由父框 × 锚区比推）。**我们画的仍是那张 `UI_Energy_Holder_big` 底图本身**，
            //    所以数值没受影响 —— 改的只是注释（`资料/战斗UI_原版对账表.md:37` 也有同一处误标）。
            // 🆕 2026-09-29：那 6 枚小光点（同一父节点下的 `Lights`）—— 见 `BuildEnergyLights`。
            BuildEnergyLights(root);
            //   任务点（`QuestPointsHolder` 97.7×97.7）：**我方在水晶下方、敌方在水晶上方**，
            //   接片 `UI_Quest_Points_Joint` 夹在水晶和任务点中间。
            //   ⚠️ 2026-09-12 更正：原来两个图标的位置用的是接片的偏移，都摆到了水晶**内侧**。
            //
            //   🔴 2026-09-13 更正（**张冠李戴，已修**）：这一组**只属于暗黑天使**。
            //      **权威出处（机器码级）**：原版 `PlayerManager.ResetMana` 里三条 getter 拿
            //      督军卡的 `rawCard+0x2c` 跟阵营枚举比 ——
            //      `cmp …,0x1e`(30=`SaimHann`)→`ToggleSpiritStoneMana` ·
            //      `0x50`(80=`Sororitas`)→`ToggleFaithMana` ·
            //      `0x6e`(110=`DarkAngels`)→`ToggleQuestPoints`；
            //      三个 `Toggle*` 各只有这一个调用点，实参直接喂 `ManaTypeHolder.Toggle`
            //      （`ManaTypeHolder__Toggle.c:17` = `SetActive(gameObject, param_2)`）。
            //      枚举数值见 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardArmy.cs`。
            //      ⚠️ 场景默认态确实是 QP 显示、Faith/SpiritStone 隐藏，但**那一局还没跑 ResetMana** ——
            //      开打之后只有暗黑天使看得见它。我们原来无条件摆给全部 13 个阵营，那是错的。
            //      ⇒ 判据收在 <see cref="ShowsQuestPoints"/> 一处，自检直接钉它。
            //      ⚠️ **做法照原版**：原版是 `ManaTypeHolder.Toggle` → `SetActive(gameObject, 布尔)`
            //      （`ManaTypeHolder__Toggle.c:17`）—— **物件照建、只切显隐**，不是「不建」。
            //      我们也照这样：位置断言还能量到它（量不到就没法钉版面了）。
            bool meQp = ShowsQuestPoints(_myFaction);
            bool foeQp = ShowsQuestPoints(_foeFaction);
            _myQuestIcon = HudImageTex(root, CardArt.Ui("UI_Quest_Points"), MyQuestX01, MyQuestY01,
                        new Vector2(0.5f, 0.5f), QuestPx / 108f, "PlayerQuestPoints", HudDecorZ + 0.05f);
            _foeQuestIcon = HudImageTex(root, CardArt.Ui("UI_Quest_Points"), FoeQuestX01, FoeQuestY01,
                        new Vector2(0.5f, 0.5f), QuestPx / 108f, "EnemyQuestPoints", HudDecorZ + 0.05f);
            _myQuestJoin = HudImageTex(root, CardArt.Ui("UI_Quest_Points_Joint"), MyQuestX01, MyQuestJoinY01,
                        new Vector2(0.5f, 0.5f), QuestJoinPx / 108f, "PlayerQuestJoin", HudDecorZ + 0.04f);
            _foeQuestJoin = HudImageTex(root, CardArt.Ui("UI_Quest_Points_Joint"), FoeQuestX01, FoeQuestJoinY01,
                        new Vector2(0.5f, 0.5f), QuestJoinPx / 108f, "EnemyQuestJoin", HudDecorZ + 0.04f);
            _myQuestIcon.gameObject.SetActive(meQp);
            _myQuestJoin.gameObject.SetActive(meQp);
            _foeQuestIcon.gameObject.SetActive(foeQp);
            _foeQuestJoin.gameObject.SetActive(foeQp);

            // ---- 阵营资源：信仰 / 灵魂石（2026-09-13 第三十三轮）----
            // ⚠️ 和任务点**抢同一个槽位**（都挂在水晶底下、位置重合，见上面那组常量的「独立佐证」），
            //    但三者按阵营互斥：任务点=暗黑天使 · 信仰=修女 · 灵魂石=灵族。
            // ✅ **2026-09-18 判据定案：按阵营**（原来那段「那一层没被反编译 ⇒ 显隐是我们挑的 ⇒ 有值就显示」
            //    已经作废 —— 调用方一直就在 `PlayerManager__ResetMana.c`，`Uses*` 就是 `阵营 id == 30/80/110`）。
            //    详见 `ShowsSpiritStone` 的注释。
            _myFaithIcon = HudImageTex(root, CardArt.Ui("40k_Battle_Display_Faith"), MyFaithX01, MyFaithY01,
                        new Vector2(0.5f, 0.5f), FaithH / 108f, "PlayerFaithHolder", HudDecorZ + 0.05f);
            _foeFaithIcon = HudImageTex(root, CardArt.Ui("40k_Battle_Display_Faith"), FoeFaithX01, FoeFaithY01,
                        new Vector2(0.5f, 0.5f), FaithH / 108f, "EnemyFaithHolder", HudDecorZ + 0.05f);
            // 🔴 **2026-10-10（A1213①）**：这四颗的框/四格 ← 原版
            //    `…/Right Anchor/Energy And turn holder/{Player,Enemy}Mana/{FaithHolder,SpiritStoneHolder}/…`
            //    （`RectTransform_3246`(我信仰) · `_3090`(敌信仰) · `_2975`(我灵魂石) · `_2906`(敌灵魂石)）。
            //    ⚠️ **敌我两颗的高度不一样**（88.40 vs 94.19 · 136.40 vs 63.00），**不是抄错**：
            //    两颗的父 holder 带 `m_LocalScale = (1,-1,1)`（镜像），而那一颗的 `anchorMin.y = 0`、
            //    `anchorMax.y = 0.855 / 0.519` ⇒ `sizeDelta.y` 那个**负内缩**被镜像翻到另一侧
            //    ⇒ 解算出来的高不同（原版就长这样，见逐颗表）。
            _myFaithText = Hud(root, "0", MyFaithX01, MyFaithY01, 4, new Color(1f, 1f, 1f),
                               new Vector2(0.5f, 0.5f), "PlayerFaithText",
                               57.43f, 94.19f, 8f, 40f, 36f);
            _foeFaithText = Hud(root, "0", FoeFaithX01, FoeFaithY01, 4, new Color(1f, 1f, 1f),
                                new Vector2(0.5f, 0.5f), "EnemyFaithText",
                                57.43f, 88.40f, 8f, 40f, 36f);

            _myStoneIcon = HudImageTex(root, CardArt.Ui("UI_Energy_Eldar"), MyStoneX01, MyStoneY01,
                        new Vector2(0.5f, 0.5f), StoneH / 108f, "PlayerSpiritStoneHolder", HudDecorZ + 0.05f);
            _foeStoneIcon = HudImageTex(root, CardArt.Ui("UI_Energy_Eldar"), FoeStoneX01, FoeStoneY01,
                        new Vector2(0.5f, 0.5f), StoneH / 108f, "EnemySpiritStoneHolder", HudDecorZ + 0.05f);
            // 石头那颗宝石（原版 `SpiritStone`，51×63，挂在 holder 中心偏 (−2.8, −2.8)）
            _myStoneGem = HudImageTex(root, CardArt.Ui("UI_Gem_Eldar"), MyStoneX01, MyStoneY01,
                        new Vector2(0.5f, 0.5f), StoneGem / 108f, "PlayerSpiritStone", HudDecorZ + 0.04f);
            _foeStoneGem = HudImageTex(root, CardArt.Ui("UI_Gem_Eldar"), FoeStoneX01, FoeStoneY01,
                        new Vector2(0.5f, 0.5f), StoneGem / 108f, "EnemySpiritStone", HudDecorZ + 0.04f);
            // ⚠️ 文字位置按 **holder 中心** 摆：dump 里 `SpiritStoneText` 是**拉伸锚点**
            //    （anchorMin/Max (0.075,0)-(0.934,0.855)、sizeDelta (−43.6,−36.7)），中心≈holder 中心。
            // 框/四格 ← 原版 `…/{Player,Enemy}Mana/SpiritStoneHolder/SpiritStoneText`（见上面那四颗的注释）
            _myStoneText = Hud(root, "0", MyStoneX01, MyStoneY01, 4, new Color(1f, 1f, 1f),
                               new Vector2(0.5f, 0.5f), "PlayerSpiritStoneText",
                               52.80f, 63.00f, 8f, 40f, 36f);
            _foeStoneText = Hud(root, "0", FoeStoneX01, FoeStoneY01, 4, new Color(1f, 1f, 1f),
                                new Vector2(0.5f, 0.5f), "EnemySpiritStoneText",
                                52.80f, 136.40f, 8f, 40f, 36f);
            // 开局一律藏着 —— 具体的显隐每帧按值定（`RefreshHud`）
            SetFactionResourceVisible(false, false, false, false);

            //   能量底板 `Card Frame Cost Icon`（原版 `Energy Player`，实绘 94.6×91.3）
            //   —— 两块水晶底下各垫一块。图在 `Resources/Art/ui_deck/`（和卡面费用格同一张）。
            _myEnergyPlate = HudImageTex(root, CardArt.DeckUi("Card_Frame_Cost_Icon"), MyEnergyPlateX01, MyEnergyPlateY01,
                        new Vector2(0.5f, 0.5f), EnergyPlateH / 108f, "PlayerEnergyPlate", HudDecorZ + 0.1f);
            _foeEnergyPlate = HudImageTex(root, CardArt.DeckUi("Card_Frame_Cost_Icon"), FoeEnergyPlateX01, FoeEnergyPlateY01,
                        new Vector2(0.5f, 0.5f), EnergyPlateH / 108f, "EnemyEnergyPlate", HudDecorZ + 0.1f);

            var gold = new Color(1f, 0.86f, 0.42f);
            // 我方水晶
            _energyGem = HudImage(root, "40k_battle_energy_full", MyEnergyX01, MyEnergyY01,
                                  new Vector2(0.5f, 0.5f), 0.72f, "EnergyGem");
            _energyGemEmpty = HudImage(root, "40k_battle_energy_empty", MyEnergyX01, MyEnergyY01,
                                       new Vector2(0.5f, 0.5f), 0.72f, "EnergyGemEmpty");
            // 数字压在宝石上（原版 `ManaText` 就框在 `Energy Player` 上，不是并排）
            // 🔴 **2026-10-10（A1213①）**：框/四格 ← 原版 `…/{Player,Enemy}Mana/ManaHolder/ManaText`
            //    （`RectTransform_2994`(我) · `_2780`(敌)；框 **73.90×82.48** vs **77.34×80.95** ——
            //     敌我那两颗的 holder 尺寸本来就不同：`RectTransform_2790.json`(我 `PlayerMana`) 的
            //     `m_SizeDelta = (-1.8120, 1.9700)` vs `RectTransform_3479.json`(敌 `EnemyMana`) 的
            //     `(0.2520, -0.8300)` ⇒ **不是抄错**）。
            //    ⚠️ 这颗文案是 `10/10`（5 字），框只有 74~77 px 宽 ⇒ **折行档 = 原版 `1`**（会折行）
            //    —— 原版就开着折行、且 `m_fontSizeMax = 40`（我 39.15 是**缩过**的收敛值，说明原版那颗框略紧），
            //    我们这一侧字体行盒更高（`Noto` 1.437 em vs 原版 `Pragati` 0.947 em）⇒ 收敛值会与原版不同。
            _energyLabel = Hud(root, "", MyEnergyX01, MyEnergyY01, 4, gold, new Vector2(0.5f, 0.5f), "EnergyLabel",
                               73.90f, 82.48f, 8f, 40f, 36f);
            // 敌方水晶：原版有（`EnemyMana`，也带 `ManaText`），**我们原来一颗都没画** ——
            // 于是玩家看不到对手还剩多少能量，只能靠猜。
            _foeEnergyGem = HudImage(root, "40k_battle_energy_full", FoeEnergyX01, FoeEnergyY01,
                                     new Vector2(0.5f, 0.5f), 0.72f, "FoeEnergyGem");
            _foeEnergyGemEmpty = HudImage(root, "40k_battle_energy_empty", FoeEnergyX01, FoeEnergyY01,
                                          new Vector2(0.5f, 0.5f), 0.72f, "FoeEnergyGemEmpty");
            _foeEnergyLabel = Hud(root, "", FoeEnergyX01, FoeEnergyY01, 4, gold,
                                  new Vector2(0.5f, 0.5f), "FoeEnergyLabel",
                                  77.34f, 80.95f, 8f, 40f, 36f);
            // ⚠️ x01 从 0.017 挪到 0.075：手牌数底板有 **259 px 宽**，还摆在屏幕左缘的话整块板会有一半在屏幕外
            //    （第一版就是这样，截图里只看得见板子右半边）。原版那个计数在 `HandArea`（手牌区）里、不在屏幕角上，
            //    挪进来既让板进画面、也更接近原版的位置。
            // 🔴 **2026-10-10（A1213①）：本颗【故意】没给框 + 四格（全 17 颗里唯一一颗）—— 判据在下面。**
            //    原版那颗是 `scenes_battlearena1 ▸ CardsInHandText`（`RectTransform/RectTransform_3400.json`，
            //    `m_SizeDelta = (2.34, 0.64)`、`auto[0.5~3.0] base36`、折行 1；出厂 `m_IsActive = 0`）。
            //    ⛔ **四格是「缩放假」里的值**：它的父链是**纯 `Transform`**——
            //    `HandArea m_LocalScale = 108`（`Transform/Transform_1401.json`）→ `CardsInHandText 0.925926`
            //    ⇒ 「1 局域单位 = 100 px」。其余 16 颗的局域单位 = 1 px，**照抄同一套口径会把字号压到 3 px**
            //    （我们这一侧的 px 口径是 `fontSize × 0.0948 × 108`，**不是**原版那条链）。
            //    ⛔ 而换算需要的那条单位桥**本仓还没建立**，且两条候选读法互相打架：
            //      · 按「1 fontSize = 1 局域单位」⇒ em = 3.0 × 100 = **300 px**，塞不进 64 px 的框（不自洽）；
            //      · 按我们字体那条 0.0948 的比值 ⇒ em = 3.0 × 0.0948 × 100 = **28.4 px** ✓（自洽）
            //        —— 但**其余 16 颗要的是「1 fontSize = 1 局域单位」才自洽**，两条**不能同时成立**。
            //    ⇒ 按本仓红线（**不许自己发明口径**）**只报不动**；判据与两条候选读法全文 →
            //    `资料/普查产出_第十一会话/PH_A1213Hud补框.md` §4（那一节还记了「框若按 234×64 px 算」的旁证）。
            //    ⚠️ **只补框不补四格是没意义的** —— 框的唯一目的就是给自适应用（见 `Hud` 的 doc）。
            _handLabel = Hud(root, "", 0.075f, 0.158f, 3, dim, new Vector2(0f, 0f), "HandLabel");
            // 手牌数底板：原版 `CardsInHandText/Bg (1)`（图**也是** `40K_display`，α 0.6941177）。
            // 实绘 259.3×65.7 px（缩放链 108×0.925926×0.009 —— 见常量注释）。
            // ⚠️ **位置是我们挑的**：原版那个节点在 dump 里 `activeInHierarchy=False`（没验到实况），
            //    祖先链还是纯 Transform（算不出绝对坐标）→ 让它跟着手牌标签走（`PlaceHandPlate`）。
            // ⚠️ 锚点必须是**中心** —— `PlaceHandPlate` 算的是标签的中心，锚 (0,0) 会把整块板
            //    顶到右上方去（第一版就是这么错的，截图里板在字的上面）
            _handPlate = HudImage(root, "40K_display", 0.075f, 0.158f,
                                  new Vector2(0.5f, 0.5f), Px(HandPlatePx), "HandPlate");
            if (_handPlate != null) _handPlate.SetTint(new Color(1f, 1f, 1f, DeckSizeAlpha));

            // ---- END TURN：原版 `Clock/TurnBtn` 130.7×80.4，**在右侧能量区中段**（不是右下角）----
            //     贴图 `UI_Button_End_Turn_Normal_wide` 是 182×112（比例 1.625），
            //     所以只给高度：80.4/108 = 0.74444 世界单位 → 宽自动 = 80.4×1.625 = 130.7 ✓
            //     ⚠️ 位置读 `EndTurnX01/Y01`（**判据只有那一份**，换分辨率重贴也读它）
            _endTurnBg = HudImage(root, "UI_Button_End_Turn_Normal_wide", EndTurnX01, EndTurnY01,
                                  new Vector2(0.5f, 0.5f), 80.4f / 108f, "EndTurnBg");
            // 文字压在按钮正中（原版 `TurnBtn/TurnText` 就是这个关系，两边读同一份坐标）
            // 🔴 **2026-10-18（第十三轮 · G2b）**：这行字的**权威**是原版词条
            // `Battle/HUD/EndTurn`（载波 = `ClockManager.SetEndTurnText` 里的字面量；判据 + 中英两列
            // 见 `Core/Loc.cs` 那一块）。这里只是**建的时候先填一次**，之后每次 `UpdateHud` 都会重取
            // （理由见那一处：换语言要当场变）。
            // 🔴 **2026-10-10（A1213①）**：框/四格 ← 原版 `…/Clock/TurnBtn/TurnText`
            //    （`RectTransform/RectTransform_3420.json`；框 119.08×80.43、`auto[8~31] base36`、折行 1）。
            //    ⚠️ 原版这颗的 `m_fontSize = 31` = `m_fontSizeMax`（收敛到顶）⇒ 我们这一侧也会往 31 涨
            //    —— 而**命中测试读 `WorldW/WorldH`**（见上面 `Contains` 的用法），字变大命中区就跟着变大
            //    （原版就是「命中区 = 文字块」这个模型，见 `Label.Contains`）。
            _endTurnLabel = Hud(root, CardText.Phrase("END TURN"), EndTurnX01, EndTurnY01, 3,
                                new Color(1f, 1f, 1f), new Vector2(0.5f, 0.5f), "EndTurnButton",
                                119.08f, 80.43f, 8f, 31f, 36f);
            // 回合时钟：写在按钮**下半部分**（原版 `ClockManager.clockText` 也在 `Clock/TurnBtn` 子树里）。
            // 按钮 80.4 px 高 = 0.744 世界单位，往下让 0.021 ≈ 23 px，正好落在按钮下半。
            _clockLabel = Hud(root, "", EndTurnX01, EndTurnY01 + 0.021f, 2,
                              new Color(0.85f, 0.88f, 0.95f), new Vector2(0.5f, 0.5f), "TurnClock");

            // ---- 牌堆：照原版 `PlayerDeck` 那一套摆 ----
            //
            // ⚠️ **原版的牌堆不是「好几张叠起来」** —— 运行时 dump
            // （`资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_Battle_Arena_1.tsv`）
            // 里 `PlayerDeck` 是 230×230，底下四样东西：
            //   `DeckAndEnergyImage`  图 `UI_Deck_Background`  —— 底板
            //   `YourTurnImage` / `NotYourTurnImage`
            //                         图 `40k_DeckHolder_light_green` / `_light_red` —— **回合灯**
            //                         （anchor 0.779–0.878 × 0.051–0.201，在底板右下角）
            //   `Player Deck Size Container`（图 `40K_display`）→ `Player Deck Size Tex` —— 张数
            //   `Cardback Container` → `Cardback`(2.17×3.14 @100 px/单位 = **217×314 px**)
            //     ⚠️ **2026-10-19（A1161）**：这一行原来写「（scale 100）」＋「2.17×3.14×100」——
            //     那个「×100」说的是同一件不成立的事：链上 `Cardback` / `Cardback Container` / `2DCard`
            //     三级 `m_LocalScale` **全是 1.0**（本件现读复核）⇒ **×100 不在这一层**
            //     （判据/出处全文 → `DeckSdfPx` 的注释）。
            //                                     + `Cardback Shadow SDF`(292×381 px)
            // 230 px = 2.13 世界单位、314 px = 2.91 世界单位（108 px/单位）。
            // 张数文字原来压在一个 `40K_display` 小板上，我们直接用文字（那张图没在用的集合里）。
            // 🔴 **2026-09-27 修（PA 普查 §三 第 1 条）：原来只给「高」，宽会**超出原版的框**。**
            //    实据（`battlearena1` 直读）：`RightArea/{Player,Enemy}Deck/DeckAndEnergyImage` 是 **PA=1**（Simple），
            //    框 **230×229.85**（方）/ **200×199.85**，图 `UI_Deck_Background` **364×346**（横，比例 1.0520）
            //    ⇒ 原版**按宽定**、实绘 **230×218.63**（我方）/ **200×190.11**（敌方）。
            //    我们原来按高给 230 ⇒ 实绘 **241.96×230**（**宽出框 11.96px、高出 11.4px = +5.2%**）。
            //    判据 = `ImageQuad.FitHeight`（uGUI `GetDrawingDimensions` 同一条算法）。
            var deckBg = CardArt.Ui("UI_Deck_Background");
            float deckBgAspect = (deckBg != null && deckBg.height > 0)
                               ? (float)deckBg.width / deckBg.height : (364f / 346f);
            _myDeckPlate = HudImageTex(root, deckBg, MyDeckX01, MyDeckY01,
                                       new Vector2(0.5f, 0.5f),
                                       Px(ImageQuad.FitHeight(DeckPlatePx, DeckPlatePx, deckBgAspect)),
                                       "MyDeckPlate");
            _foeDeckPlate = HudImageTex(root, deckBg, FoeDeckX01, FoeDeckY01,
                                        new Vector2(0.5f, 0.5f),
                                        Px(ImageQuad.FitHeight(FoeDeckPlatePx, FoeDeckPlatePx, deckBgAspect)),
                                        "FoeDeckPlate");
            // 底板再往后一点，别把卡背盖住（`HudImageTex` 已经把图放到文字后面了）
            if (_myDeckPlate != null) _myDeckPlate.transform.localPosition += new Vector3(0f, 0f, 0.02f);
            if (_foeDeckPlate != null) _foeDeckPlate.transform.localPosition += new Vector3(0f, 0f, 0.02f);

            // 🆕 2026-09-26：牌堆卡背**底下那层 SDF**（原版 `Cardback Shadow SDF`）——
            //   比卡背大一圈（292×381 vs 217×314），露在外面那圈就是牌堆的「厚度/投影」。
            //   两层**与卡背同心**（原版三个节点 anchoredPos 都是 (0,0)、pivot (.5,.5)）。
            //   ⚠️ 层次靠 **z** 排（这块 HUD 一直用 z，见 `HudImageTex` 的注释：相机看 +Z、z 越大越远）：
            //     底板 = 默认+0.02（更远）· **SDF = 默认+0.01** · 卡背 = 默认。
            //     ⇒ SDF 夹在底板与卡背之间 —— 不会盖住卡背，也压在底板之上。
            //   ⚠️ 取不到掩码（该阵营没有默认卡背 / 图没导）⇒ **那层不画**，牌堆本体照旧。
            _myDeckSdf = MakeDeckSdf(root, _myFaction, MyDeckX01, MyDeckY01, "MyDeckSdf");
            _foeDeckSdf = MakeDeckSdf(root, _foeFaction, FoeDeckX01, FoeDeckY01, "FoeDeckSdf");

            _myPile = HudImageTex(root, CardArt.CardBack(_myFaction), MyDeckX01, MyDeckY01,
                                  new Vector2(0.5f, 0.5f), Px(DeckCardPx), "MyDeck");
            _foePile = HudImageTex(root, CardArt.CardBack(_foeFaction), FoeDeckX01, FoeDeckY01,
                                   new Vector2(0.5f, 0.5f), Px(DeckCardPx), "FoeDeck");
            // 🔴 **2026-10-19（B2）就地订正（铁律 5）—— 这一段原来的前提是【错的】**。
            //    原文写：「两张**贴图**的宽高比与这两个 rect 都不一样（`_Main` 707×996、`_SDF` 100×130.5），
            //    而且逐张卡背还会变 ⇒ 按贴图比例画，两者的比值会跟着『这次用的是哪张卡背』浮动」
            //    —— **前提不成立**：那两个**节点 rect 的比例 == 两张 sprite 的 `m_Rect` 比例**
            //    （`2.1739/3.1364 = 0.693120` vs `707/1020 = 0.693137`；`2.9212/3.8122 = 0.766277` vs
            //    `100/130.5 = 0.766284` —— 都到 5 位有效数字重合）⇒ **原版一处都没拉伸**，
            //    节点比例与「这次用哪张卡背」**无关**；画心只是**按 `padding` 内缩**。
            //    ⇒ 当年那两条「按节点比例定形状」的结论（一律 `SetAspect(CardbackRectAspect)`）
            //      **把牌堆画成了拉伸**（最坏 `All_Early Backer` 宽 +16.1% / 高 +10.0%）。
            //    现在照原版**两段式**：定框（`m_Rect` 比例）+ 按 `padding` 内缩 ⇒ 见 `FitDeckPile`
            //    （算式/判据全文 → `Core/CardbackFace.cs`，本仓唯一一份）。
            //    ⚠️ 与 CLAUDE.md「战斗侧是另一个模型（`ImageQuad` 恒等比、PA=0）」**不冲突** ——
            //    PA=0 仍是另一个模型，只是「节点比例 == `m_Rect` 比例」这条事实把 A20 当年的困惑解释掉了。
            FitDeckPile(_myPile, CardArt.CardBack(_myFaction), MyDeckX01, MyDeckY01);
            FitDeckPile(_foePile, CardArt.CardBack(_foeFaction), FoeDeckX01, FoeDeckY01);

            // 回合灯：底板右下角（位置在 `PlaceDeckLights` 里统一摆 —— 换分辨率要重贴）
            _myDeckLight = HudImageTex(root, CardArt.Ui("40k_DeckHolder_light_green"), MyDeckX01, MyDeckY01,
                                       new Vector2(0.5f, 0.5f), Px(DeckLightPx), "MyDeckLight");
            _foeDeckLight = HudImageTex(root, CardArt.Ui("40k_DeckHolder_light_green"), FoeDeckX01, FoeDeckY01,
                                        new Vector2(0.5f, 0.5f), Px(DeckLightPx), "FoeDeckLight");
            PlaceDeckLights();

            // ---- 张数底板 + 张数：原版 `Player Deck Size Container`（图 `40K_display`）----
            // 底板是一块**半透明**横条（`m_Color.a = 0.6941177`，不设就成一块实心白板），
            // 贴在牌堆正上方；文字压在同一块板上（原版 `Player Deck Size Tex` 居中），
            // 所以板和字**用同一个锚点**，别再各摆各的。
            // ⚠️ 原来只写了文字、没画底板，而且文字摆在我/敌牌堆的**正中心上方**（少了那 −20.75 / −5 px）。
            var sizeTint = new Color(1f, 1f, 1f, DeckSizeAlpha);
            float mySizeX = MyDeckX01 + DeckSizeDxMine / 1920f, mySizeY = MyDeckY01 + DeckSizeDyMine / 1080f;
            float foeSizeX = FoeDeckX01 + DeckSizeDxFoe / 1920f, foeSizeY = FoeDeckY01 - DeckSizeDyFoe / 1080f;
            _myDeckSizePlate = HudImage(root, "40K_display", mySizeX, mySizeY,
                                        new Vector2(0.5f, 0.5f), Px(MyDeckSizePx), "MyDeckSizePlate");
            _foeDeckSizePlate = HudImage(root, "40K_display", foeSizeX, foeSizeY,
                                         new Vector2(0.5f, 0.5f), Px(FoeDeckSizePx), "FoeDeckSizePlate");
            if (_myDeckSizePlate != null) _myDeckSizePlate.SetTint(sizeTint);
            if (_foeDeckSizePlate != null) _foeDeckSizePlate.SetTint(sizeTint);

            // 🔴 **2026-10-10（A1213①）**：框/四格 ← 原版 `RightArea/{Player,Enemy}Deck/Player Deck Size
            //    Container/Player Deck Size Tex`（`RectTransform_2604`(我) · `_3406`(敌)）。
            //    ⚠️ **框高不是序列化字段**：那颗 TMP 的 `m_SizeDelta.y = **0**` —— 它靠父容器
            //    `Player Deck Size Container` 的 **`AspectRatioFitter`（宽控高 4.03465）**撑出高度，
            //    打开 uGUI 的写法是 `m_AspectMode = WidthControlsHeight` ⇒ 框 = 容器 × 锚区比。
            //    真值（`python 工具/menu_dump.py bundle_scenes_scenes_battlearena1 --rt 3292 --depth 4 --no-sprite`
            //    那一趟现读、`⚙ARF` 已回写）：**我 219.42×44.93** · **敌 193.20×39.56**。
            //    ⛔ **拿序列化的 `sizeDelta.y = 0` 当框高会把字号压到 `m_fontSizeMin`（10）**。
            //    ⚠️ `m_fontSizeBase = 49.63`（**≠ 标称**，本族唯一一颗）—— 它是自适应二分的起点，如实照传。
            _pileLabel = Hud(root, "", mySizeX, mySizeY, 3, dim,
                             new Vector2(0.5f, 0.5f), "MyPileLabel",
                             219.42f, 44.93f, 10f, 42f, 49.63f);
            _foePileLabel = Hud(root, "", foeSizeX, foeSizeY, 3, dim,
                                new Vector2(0.5f, 0.5f), "FoePileLabel",
                                193.20f, 39.56f, 10f, 42f, 49.63f);

            // ---- 本回合已出牌数：原版 `CardsPlayedInTurnHolder` 里的三枚小方块 ----
            // 三枚 `40k_general_bt_yellow` 各 20×20、间距 9，居中排在 holder 里、底对齐；
            // holder 贴在屏幕**左缘中点**（`LeftArea` 锚 (0,0.5)，pos (0,−28.178)）。
            // ⚠️ 原版**没出牌时整块是关着的**（dump 里 holder 与三枚 `activeSelf=False`），我们也照做：
            //    出第 N 张牌就点亮第 N 枚，最多三枚（只有三个节点，第四张不显示 —— 原版也是这样）。
            _playedPips = new ImageQuad[3];
            for (int i = 0; i < 3; i++)
            {
                float cx = PlayedPipX0Px + i * (PlayedPipPx + PlayedPipGapPx) + PlayedPipPx * 0.5f;
                float cy = PlayedPipY0Px + PlayedPipPx * 0.5f;
                _playedPips[i] = HudImage(root, "40k_general_bt_yellow", cx / 1920f, cy / 1080f,
                                          new Vector2(0.5f, 0.5f), Px(PlayedPipPx), "CardsPlayedInTurn" + (i + 1));
                if (_playedPips[i] != null) _playedPips[i].gameObject.SetActive(false);
            }

            // 提示行放在**两行棋盘中间那条缝**里（玩家行上沿 0.483 / 对手行下沿 0.530）
            _hintLabel = Hud(root, "", 0.5f, 0.507f, 3,
                             new Color(0.75f, 0.78f, 0.85f), new Vector2(0.5f, 0.5f), "HintLabel");

            _resultLabel = Hud(root, "", 0.5f, 0.5f, 7,
                               new Color(1f, 0.9f, 0.4f), new Vector2(0.5f, 0.5f), "ResultLabel");

            // 设置按钮（原版 `SettingsBtn` 63.9²，图 `UI_Settings_Icon`）——
            // 权威表绝对坐标 x[1808.0,1871.9] y[9.2,73.1] → 中心 (1839.95, 41.15)。
            _settingsBtn = HudImage(root, "UI_Settings_Icon", 0.95831f, 0.96190f,
                                    new Vector2(0.5f, 0.5f), 63.87f / 108f, "SettingsBtn");
            // 设置面板（投降按钮在里面；🆕 2026-09-17 起**对手难度**那一行也在里面）
            _settingsPanel = SettingsPanel.Create(root, Forfeit, CycleAiDifficulty);
            _settingsPanel.SetDifficulty(aiDifficulty);      // 开局面板还没开，先把当前档写上去
            // 🔴 **2026-10-18：这里原来还有第二处幂等注入 `_tutOverlay.SkipPanel = _settingsPanel;`（已删）。**
            //   A991 当时加它是为了对付「两件创建顺序不固定」，而那两个转发口现在**全仓零调用** ⇒ 注入再无意义。
            //   ✅ **2026-10-18（第三会话）订正**：原来这里写「还剩哪四个口没删」—— **那四个口已经删光了**
            //     （`TutorialOverlay.cs` 里只剩「【已删】」留痕）⇒ 这一句过时，已改。
            //   完整理由 → 见 `EnsureTutorialOverlay` 里那一段。

            // 回放条（原版 `ReplayButtons`，左上角那 4 枚）—— 坐标悬案 2026-09-17 已复核，
            // 结论与「这四个钮接什么」都写在 `ReplayBar.cs` 文件头。
            // 🔴 **2026-09-27：显隐照原版改对了** —— 原版 `ReplayHud.Setup()` 只做一件事：
            //    `objHolder.SetActive(BattleManager.matchType == 0xA0)`（`0xA0 = 160 = MatchType.Replay`）。
            //    我们**从不进回放局** ⇒ 传 `false`：**这 4 枚在普通对局里【不该出现】**（原来一直摆着，是错的）。
            //    判据 → `资料/普查产出_0927/回放_界面真值.md` §B/§C。
            //    ⚠️ 我们原来接在这 4 枚上的那套**本局时间控制**没删 —— 挪到键盘了（`HandleTimeControlKeys`）。
            _replayBar = ReplayBar.Create(root);
            _replayBar.Setup(false);

            // 单位语音条（原版 `Unit Chat`：我方的在左下、敌方的在左上）——
            // 形状/数值/「哪些是我们挑的」都在 `UnitChatPanel.cs` 文件头。
            _unitChat = UnitChatPanel.Create(root);

            // ---- 敌方名牌上的「墓地/战斗日志」按钮 + 面板（2026-09-13）----
            // 原版 `EnemyInfo/ShowCemeteryBtn`：64.48×64.17 px、绝对 x[52.0,116.4] y[135.9,200.1]
            // → 中心 (84.2, 168)；图 `40k_UI_bt_battlelog`（119×119 → 实绘 0.54×）。
            // 出处：`资料/战斗规格/战斗重建_0827/子代理读报_back左区_0827.md:79`（rect）与 `:193`（图）。
            _cemeteryBtn = HudImage(root, "40k_UI_bt_battlelog", 84.2f / 1920f, 1f - 168f / 1080f,
                                    new Vector2(0.5f, 0.5f), 64.5f / 108f, "ShowCemeteryBtn");
            _logPanel = BattleLogPanel.Create(root);

            // 结算面板：原版 `EndBattlePanel`。它自己管显示/隐藏，平时是关着的。
            _endPanel = EndPanel.Create(root);
            // 卡牌放大展示窗：原版 `CardDisplayWindow`。轻点卡牌开关，平时关着。
            _cardDisplay = CardDisplayWindow.Create(root);
            // 多张一起看的展示窗：原版 `UIMultiCardDisplay`（`Generic Multi Card Display Combat`）。
            // 布局数值与出处见 `MultiCardDisplay.cs` 文件头；**入口（点我方牌堆）是我们挑的**。
            _multiCards = MultiCardDisplay.Create(root);

            // 「原版有、我们原来缺」的那批 HUD 件（2026-09-13 补摆，见那个方法的注释）
            BuildHudExtras(root);

            // 开局换牌面板（原版 `Mulligan` 子树）。平时是关着的，进换牌阶段才 Open
            _mulligan = MulliganPanel.Create(root);

            // 🆕 选牌 / 选效果面板（原版 `ChooseCardMenu`）。平时关着，玩家出的卡要「问」时才开
            _choosePanel = ChoosePanel.Create(root);

            // ---- 🆕 2026-10-18（§8b 批 B · 2a / 2b）：**原版 Canvas 直子那三件** ----
            // 原版层级：`BattleHud/Canvas/{Aspect Ratio Filler Top, Aspect Ratio Filler Bottom,
            //            BackCanvas, FrontCanvas, BattleDoors, Tutorial highlight, Anim Anchors,
            //            UI Error Message Controller (MUST BE ENABLED)}` —— `RT_2759.m_Children` 实读。
            // 我们的 `HudRoot` = 原版 `BattleHud` 那一级（没有 uGUI Canvas 节点）⇒ 三件都挂它下。
            BuildAspectRatioFillers(root);
            BuildErrorBanner(root);
            BuildScreenAspectRatioController();
        }

        // ==================================================================
        //  「原版有、我们原来缺」的 HUD 件（2026-09-13 补摆）
        //
        //  清单与绝对坐标出自 `资料/战斗UI_原版对账表.md` **§三点五③**（2026-09-13 全面位置核对
        //  之后建的），逐件的原始出处写在下面每一条上。
        //
        //  ⚠️ 这一组**全是显示件，不接交互** —— 原版那三个按钮的 `m_OnClick` 在场景里**全为空**
        //     （`子代理读报_back左区_0827.md:169`：运行时才绑），我们这边没有对应功能可绑。
        //     **按下去没反应 = 原版此刻的状态**，不是我们漏了。
        // ==================================================================

        /// <summary>这批补摆件（自检按它核对「摆了几件 / 用的哪张图 / 在哪」）</summary>
        readonly List<ImageQuad> _hudExtras = new List<ImageQuad>();
        public int HudExtraCount { get { return _hudExtras.Count; } }

        // ---- 玩家信息块那两层：头衔底条 + 称号文字（2026-09-24）----
        // 显隐**同一个判据**（原版 `PlayerProfileUIController.SetProfileTitle`），见 `SetTitle`。
        ImageQuad _titleBgMe, _titleBgFoe;
        Label _titleMe, _titleFoe;
        string _titleMeText = "", _titleFoeText = "";

        /// <summary>补摆件的清单：名字 | 图 | 中心（按原版 1920×1080 绝对 px，y 从**上**）</summary>
        public string HudExtraReport()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < _hudExtras.Count; i++)
            {
                var q = _hudExtras[i];
                if (q == null) continue;
                var n = LayoutSpace.ToNormalized(q.transform.localPosition);
                sb.Append($"     {q.name,-22} {(q.Texture != null ? q.Texture.name : "<无图>"),-32}"
                        + $" 中心 ({(n.x * 1920f):F1}, {((1f - n.y) * 1080f):F1})px\n");
            }
            return sb.ToString();
        }

        /// <summary>
        /// 照「原版 1920×1080 绝对矩形」摆一张 HUD 图：x 从**左**、y 从**上**、w/h 是宽高 px
        /// —— 和 `战斗UI_原版对账表.md` / `子代理读报_*` 里的写法**逐字一致**。
        /// ⚠️ 代码里那套 `x01/y01` 是「中心点归一化 + y 从**下**」，两者换算只写在这一处
        ///    （以前手算 `84.2f / 1920f, 1f - 168f / 1080f` 这种，抄错一个数就要重对一遍）。
        /// 图按**高度**摆放、宽度由贴图自身比例定（`ImageQuad` 就是这么做的）—— 原版这几件都是
        /// `PreserveAspect=1`，行为一致。
        /// </summary>
        ImageQuad HudAbs(Transform root, string artName, float x, float y, float w, float h,
                         string name, float z = HudImageZ)
        {
            float cx = (x + w * 0.5f) / 1920f;
            float cy = 1f - (y + h * 0.5f) / 1080f;
            var q = HudImageTex(root, CardArt.Ui(artName), cx, cy, new Vector2(0.5f, 0.5f), h / 108f, name, z);
            if (q != null) _hudExtras.Add(q);
            return q;
        }

        void BuildHudExtras(Transform root)
        {
            // ---- 头衔底条 `TitleBackground`：311×42，图**原生 1:1** + 称号文字 ----
            // 出处：`子代理读报_back左区_0827.md:47`（我 x[54.2,365.2] y[1028.5,1070.5]）
            //      与 `:70`（敌 x[54.5,365.5] y[92.8,134.8]）。它在 `NameBackground` **中部偏下 35 px**。
            // 🔴 **2026-10-14（A531）四个 px 写原版未取整值**（同 A513 那一档的口径）——
            //    判据 = 原版 `RectTransform_3560.json`（我，挂 GO 386）/ `RectTransform_3437.json`（敌，挂 GO 508）
            //    ＋**父链走完**（我：`3560 → 3189(PlayerName 18,-0.005 stretch) → 3319(PlayerInfo (31.7001953125,28) 260×75)
            //    → 四层 stretch → 裸 Transform`；敌同理走 `EnemyInfo (50,-28) → EnemyName`）：
            //    · 我 x[54.2001953125, 365.2001953125] y[1028.5051536560059, 1070.5051536560059]，中心 (209.7001953125, 1049.5051536560059)
            //    · 敌 x[54.5, 365.5] y[92.75257110595703, 134.75257110595703]，中心 (210.0, 113.75257110595703)
            //    ⚠️ 原来写的是 `54.2 / 1028.5`（我）与 `92.8`（敌）——都是 `子代理读报_*_0827.md` 里**取整到 0.1** 的写法
            //    ⇒ 我 y 偏 0.0052 px、敌 y 偏 **0.0474 px** = 4.4e-4 世界单位，**超过 A423/A513 那一档 1e-4 的阈值**
            //    （1 px = 1/108 世界单位，阈值 ≈ 0.011 px）。⚠️ `w/h` 本来就是整数（311/42，与 sprite 原生 1:1）。
            // 🔴 **2026-09-24 改：z 从 `HudDecorZ`（最远那层）提到名牌**前面****
            //    原版同级顺序（直读 `RectTransform_3189.json` 的 `m_Children`）=
            //      `[NameBackground(3293), TitleBackground(3560), Avatar Item Small(2709), PlayerNameText(3016)]`
            //    —— UGUI **后出现的兄弟画在上面** ⇒ 底条在名牌**之上**、头像块又在底条之上。
            //    实拍（`桌面/战斗截图参考.png`）也是这个样：称号那根条压在名牌下半截上。
            //    ⛔ 旧注释写的「它就是名牌的底、要压在名牌下面」**没有出处**，已按同级顺序推翻。
            //    （原来那次的真实事故是「和名牌同 z ⇒ 谁压谁不确定」，不是「原版在下面」。）
            _titleBgMe = HudAbs(root, "UI_PlayerFrame_TitleBackground", 54.2001953125f, 1028.5051536560059f, 311f, 42f,
                                 "TitleBackground_Me", HudImageZ - 0.02f);
            _titleBgFoe = HudAbs(root, "UI_PlayerFrame_TitleBackground", 54.5f, 92.75257110595703f, 311f, 42f,
                                 "TitleBackground_Foe", HudImageZ - 0.02f);

            // ---- 称号文字（原版 GO 名也叫 `EnemyTitle`，两侧同参数）----
            // 出处 `子代理读报_back左区_0827.md:48`（我）`:71`（敌）+ 直读 `MonoBehaviour_3797/3887.json`：
            //   我 x[110.0,344.2] y[1021.0,1067.1]（框 234.2×46.0）· 敌 x[110.3,344.5] y[85.3,131.3]
            //   TMP：出厂 `m_text` 就是占位串 `"Title Text"` · fs **30.55**（autosize 2→35）·
            //        H 居中 · V Midline · `m_fontColor` = (1.0, 0.6306, 0.4198) ≈ **#FFA16B 橙**
            // 🔴 **2026-10-14（A531）中心也写原版未取整值**（同 A513 一档）——
            //    判据 = `RectTransform_2893.json`（我，GO 815）/ `RectTransform_3134.json`（敌，GO 1121）+ 父链：
            //    · 我 x[109.9865249246, 344.2313929200] y[1021.0443820953, 1067.0911102891] ⇒ **中心 (227.10895892232656, 1044.0677461922169)**
            //    · 敌 x[110.2863296121, 344.5311976075] y[85.2917995453, 131.3385277390] ⇒ **中心 (227.40876360982656, 108.31516364216805)**
            //    ⚠️ 原来写 `227.1 / 1044.05`（我）与 `227.4 / 108.3`（敌）—— 又是 0827 报告里取整到 0.01/0.1 的数
            //    ⇒ 偏 0.009~0.018 px = 8e-5~1.6e-4 世界单位（**已经在 1e-4 那一档的边缘**）。
            // 🔴 **显隐判据**（`PlayerProfileUIController__SetProfileTitle.c`，机器码级）：
            //    `SetActive(titleGO, !string.IsNullOrEmpty(title))` ⇒ **没有称号 ⇒ 连底条一起关**。
            //    实况 dump 印证（`runtime_ui_dump_drive_0912.tsv`）：`TitleBackground` 的
            //    `activeSelf = False`（原版关服、玩家没有称号）⇒ 我们默认也**不显示**。
            // ⚠️ 所以这一件**不是「画一行字上去」**（§13-E 第 7 条当时是这么理解的）——
            //    原版无资料时**整块都不出现**。单机没有玩家资料 ⇒ 默认关；`SetTitle` 留好了接线点。
            var titleOrange = new Color(1.0f, 0.6306f, 0.4198f);
            // 🔴 **2026-10-10（A1213①）**：框/四格 ← 原版两侧的 `…/TitleBackground/EnemyTitle`
            //    （`RectTransform/RectTransform_2893.json`(我) · `_3134.json`(敌)；
            //     框 **187.40×36.84**、`auto[2~35] base36`、**折行 0**）。
            //    ⚠️ 原版这两颗的 `m_fontSize = 30.55` **< `m_fontSizeMax = 35`** ⇒ 原版当时**是被框压下来的**
            //    （收敛值不是上限）—— 我们这一侧同样会往 35 涨、由框高决定落点（见 `Hud` 的 doc）。
            _titleMe = Hud(root, "", 227.10895892232656f / 1920f, 1f - 1044.0677461922169f / 1080f, 3, titleOrange,
                           new Vector2(0.5f, 0.5f), "TitleText_Me",
                           187.40f, 36.84f, 2f, 35f, 36f, 0);
            _titleFoe = Hud(root, "", 227.40876360982656f / 1920f, 1f - 108.31516364216805f / 1080f, 3, titleOrange,
                            new Vector2(0.5f, 0.5f), "TitleText_Foe",
                            187.40f, 36.84f, 2f, 35f, 36f, 0);
            if (_titleMe != null) _titleMe.SetGlyphHeight(TitleFontPx / 108f);
            if (_titleFoe != null) _titleFoe.SetGlyphHeight(TitleFontPx / 108f);
            // 🆕 **2026-10-16（A712 阶段 2）**：纵向档 = 原版两侧 `EnemyTitle` 的 **`V Midline`**
            //   —— 判据 = 上面 `:8364-8365` 逐字写着「H 居中 · **V Midline** · `m_fontColor` = (1.0, 0.6306, 0.4198)」，
            //   直读 `MonoBehaviour_3797.json`（我）/`MonoBehaviour_3887.json`（敌）。`Midline` 不吃框高。
            //   ⚠️ 本件**单机恒传空串**（`SetTitle(null, null)` ⇒ 整块不显示）⇒ 今天无可见差；
            //   而且 `Label` 恒 `NoWrap`（`BuildTmp` 硬写）⇒ 不踩「多行 + 档位」那个坑（见 W16 报告 §五·1）。
            if (_titleMe != null) _titleMe.SetVAlign(Label.VAlign.Midline);
            if (_titleFoe != null) _titleFoe.SetVAlign(Label.VAlign.Midline);
            SetTitle(null, null);          // 单机没有玩家资料 ⇒ 与实况一致：整块不显示

            // ---- 头像块 `Avatar Item Small` ----
            // 出处：`子代理读报_back左区_0827.md:49`（容器 x[-19.7,136] y[948.1,1084.6]，**左缘出屏 19.7 px**）
            //      + 运行时 dump（`runtime_ui_dump_drive_0912.tsv:123-126`）。
            // ⚠️ **三处得按 dump 修，光看容器 rect 会做错**：
            //   ① 它是 `PlayerName` 的**子节点**、排在 `NameBackground` **后面** → **画在名牌上面**
            //      （所以 z 要比 HUD 图那层**更靠前**；同 z 的话谁压谁由渲染顺序定，不确定 —— 这个坑踩过）
            //   ② 真正画的 `Border` 在 `Image Container` 里，而那个容器是 `size(0,-37.4)` 的 stretch
            //      → 容器实绘 155.64×99.1，**但 Border 自己的 rect 还不是它**（见 ③）
            //   ③ `Border` 自己 = `:52` 我 x[-20.7,135.0] y[960.0,1059.1] · `:75` 敌 x[-19.4,136.3] y[24.5,123.6]
            //      （都是 155.7×99.1，但**顶边是 960.0 / 24.5，不是容器的 948.1 / 12.3**）
            // 🔴 **2026-09-24 裁定（三处旧说法一并作废）**：
            //   · ① `SetAspect(155.64/99.1)` **删掉** —— 那是把图**横向拉伸 1.76×**
            //        （原版 `m_PreserveAspect = 1`，见 `MonoBehaviour_4606.json:40`；13 个战场里用这张图的
            //          Image 共 39 个，**39/39 全是 PA=1**）。旧注释「原版没有 preserveAspect」是错的。
            //   · ② `Border` 的 RT **`m_LocalScale = (1.25,1.25,1.25)`**（直读
            //        `bundle_scenes_scenes_battlearena1/RectTransform/RectTransform_2691.json`）
            //        ⇒ 实绘 rect = 155.64×99.1 × 1.25 = **194.55×123.88**，缩放绕 pivot(0.5,0.5)、**中心不变**。
            //        **「1.25 生不生效」的实况证据**：主菜单顶栏是同一个 sprite + 同一个 1.25，原版实拍
            //        `资料/原版参照图/Unity参照管线_0825/shots_ui/menu_full_0825.png` 里量到盾形框宽 **≈111 px**
            //        （不缩放只有 91.5 px、×1.25 是 116 px）⇒ **缩放是活的**。
            //   · ③ 等比适配那个 rect（sprite `Player Profile Border` 256×286 ⇒ 比例 0.8951）
            //        ⇒ **实绘 110.88 × 123.88**（= 110.9×123.9）。
            //   中心 = `Border` rect 的中心：**我 (57.15,1009.55)** · **敌 (58.45,74.05)**；
            //   实绘左上角 = 中心 − 实绘/2：**我 (1.71,947.61)** · **敌 (3.01,12.11)**。
            //   ⛔ 作废：用容器中心 (58.15,997.65)（**偏高 11.9 px**）· 只建我方（**原版敌我各一个**）。
            // 🔴 **2026-10-14（A531）上面那六个数一律换成未取整值**（同 A513 那一档的口径）——
            //   这一件**不是「照抄原版 rect」**（`Border` 自己的 rect 是 155.64×99.125，直接拿它画会矮 24.8 px），
            //   而是**按上面那条换算从原版精确值重算一遍**；判据逐层都是原版直读：
            //     `RectTransform_2691.json`（我，GO 158）/ `RectTransform_3263.json`（敌，GO 862）
            //     的 `m_LocalScale = (1.25,1.25,1.25)`（✅ 直读 JSON，不是转述）+ **父链走完**：
            //     · 我 `Border` rect x[-20.6688079834,134.9711914062] y[959.9701792985,1059.0953716325]
            //       ⇒ 尺寸 155.63999938964844 × 99.1251923339398，中心 **(57.15119171142578, 1009.5327754654946)**
            //     · 敌 `Border` rect x[-19.3690032959,136.2709960938] y[24.5175959855,123.6427883195]
            //       ⇒ 中心 **(58.45099639892578, 74.08019215250636)**
            //     ⇒ ×1.25 后按 256/286 等比（**宽受限**）⇒ **实绘 110.90930610790465 × 123.90649041742475**
            //     ⇒ 左上角 **我 (1.6965386574734538, 947.5795302567823)** · **敌 (2.9963433449734396, 12.126946943793968)**
            //   ⚠️ 换算链与旧写法**逐条相同**（`h` = 实绘高、`w` = 实绘宽、中心不变）—— 只有**输入值**从未取整换成精确值：
            //     实绘高 123.88 → **123.9064904**（+0.0265）；中心 我 (57.15,1009.55) → **(57.15119171, 1009.53277547)**、
            //     敌 (58.45,74.05) → **(58.45099640, 74.08019215)**（旧值都是 0827 报告里取整到 0.1 的数）。
            //   ⚠️ `m_LocalScale 1.25` 绕 pivot(0.5,0.5) 缩 ⇒ **中心不变**（这就是「中心不变」那一句的依据）。
            // ⚠️ 原版场景态里 `avatarImage`（头像立绘）是 **m_Enabled=0** —— 所以**只摆框、不摆立绘**
            //    （那本来由 `ItemDrawer` 按玩家资料运行时灌，单机没有资料）。这一件因此和名牌自带的
            //    盾形**几乎重合**（同一个位置、同一个造型）—— 但它是**独立节点**，原版有、我们原来没有。
            //   ⛔ `w` 在这里经 `HudAbs` **只参与算中心**（实绘宽由贴图比例定）—— 别把它读成「裁剪框」。
            HudAbs(root, "Player_Profile_Border", 1.6965386574734538f, 947.5795302567823f, 110.90930610790465f, 123.90649041742475f,
                   "AvatarItemSmall_Me", HudImageZ - 0.05f);
            HudAbs(root, "Player_Profile_Border", 2.9963433449734396f, 12.126946943793968f, 110.90930610790468f, 123.90649041742478f,
                   "AvatarItemSmall_Foe", HudImageZ - 0.05f);

            // ---- 三个边角按钮（图都在；`m_OnClick` 原版也是空的）----
            // `ChatButton`（玩家名牌下）：**64.443×61.846 @x[50.9202,115.3632] y[880.154,942.0]**；
            //   图 `40k_UI_bt_voicelines` 128×128 → 实绘 0.50×（`子代理读报_back左区_0827.md:59`）。
            //   ⚠️ 名字叫 Chat，但这**一个对象上挂了两个组件**（2026-09-18 更正）：
            //      ① `Button`(MB 5291) 的 `m_OnClick → BattleManager.ClickChat` ⇒ **开 `ChatPopup` 面板**（我们接了）
            //      ② `PlayerStateToggle`(MB 4089) 的 `selectedBool='EnableWarlordVOs'` ⇒ 敌语音开关（**没接，待实况确认**）
            //      旧报告那句「与 6 个聊天钮完全无关，勿混」**已被推翻** —— 见 `资料/语音线_原版规格与ASR管道.md` §1.7.0。
            // 🔴 **2026-10-13（A513）这四个 px 必须写原版未取整值**（判据 = `RectTransform_2984.json`
            //   ——`ChatButton` 挂在 GO 85 上：`m_AnchoredPosition (19.219999313354492, 110.0)` ·
            //   `m_SizeDelta (64.44300079345703, 61.84600067138672)` · `anchorMin=anchorMax=(0,0)` ·
            //   `pivot (0,0)`；父链走完 `2984(ChatButton) → 3319(PlayerInfo (31.7001953125,28.0) 260×75)
            //   → 2849/3498/2684/2759（**全是 stretch + 零偏移**）→ 1395（裸 `Transform`，localPosition 0)`
            //   ⇒ 绝对 x[50.9201953125,115.36319610595703] y[880.15399932861328,942.0]，
            //   **中心 = (83.14169570922852, 911.0769996643066)**）。
            //   原来写的是 `50.9 / 880.2 / 64.44 / 61.85`（0.1 px 取整）⇒ 中心 (83.1200, 911.1250)，
            //   比原版偏 0.0217 / 0.048 px = **2.0e-4 / 4.4e-4 世界单位** —— 正好超过 A423 那一档的
            //   **1e-4 世界单位（≈0.011 px）** 阈值（收红的是 `Editor/BattleScene.cs` 的 A513 细口径断言，
            //   两个轴的符号与量级逐位吻合）。⛔ **别去放宽那条阈值**：0.011 px 是「照原版精确值写」的提醒
            //   （同族粗口径 `HudExtraPosPx` 用 1.5 px，两档并存是对的）—— 要绿就给这里写精确值。
            //   ⚠️ 画出来的是**内接**的 **61.846×61.846**（图 `40k_UI_bt_voicelines` 128×128 正方形 + `PA=1`
            //     ⇒ **宽受限**；同族那句说明在本文件 `:2495`），命中区 = 这个内接框
            //     （`ImageQuad.Contains` 比的是 `WorldW/WorldH`，而 `WorldW = WorldH × 贴图比例`）
            //     —— **不是** 64.443×61.846。
            //     🔴 **2026-10-14（A531）就地订正**：这一行原文写「画出来的是 64.443×61.846、命中区就是这一整个 rect」，
            //     与本文件 `:2495` 自己那句「画出来的是**内接**的 61.846×61.846」**互相打架** ⇒ 按后者（有 `PA=1` 的算法依据）。
            //     ⚠️ 这一条只影响注释表述：`h` 那 0.004 px 的改动在两种读法下都不影响任何断言。
            //   ⚠️ 这里的 `64.443f` / `61.846f` 与那份 JSON 的 double **逐位对上**（float32 恰好等值），
            //      但与 A423 那颗 `CenterCameraButton` 的 `64.4429931640625 / 61.84600830078125`
            //      **不是同一串**（同族两条断言各钉各的，⛔ 别把两串并成一个常量）。
            _chatBtn = HudAbs(root, "40k_UI_bt_voicelines", 50.9201953125f, 880.15399932861328f, 64.443f, 61.846f, "ChatButton");
            // `CenterCameraButton` **64.443×61.846** @x[17.9293,82.3723] y[568.174,630.020]；图 237×237 → 0.27×（`:94`）
            // 🆕 **2026-10-12（A423）**：把**行为**接上（原来只摆了图：不可点、也一直亮着）。
            //   原版那条链的三处调用点、显隐条件、点击效果与常量出处，全在 `ToggleCameraResetButton` 的判据表里。
            // 🔴 **2026-10-12（A423 收红那轮）这四个 px 必须写原版未取整值**（判据 = `RectTransform_3487` 的
            //   `sizeDelta (64.4429931640625, 61.84600830078125)` + `anchoredPosition (0.150757, −59.097)`
            //   反推的矩形，与上面 `CameraResetRectPxW/H` 两个常量**同源**；`:2402` 的注释也是这几个数）。
            //   原来写的是 `17.9 / 568.2 / 64.44 / 61.85`（0.1 px 取整）⇒ 中心 (50.12, 599.125)，
            //   比原版 (50.1508, 599.097) 偏 0.0308 / 0.028 px = **2.85e-4 / 2.59e-4 世界单位**
            //   —— 正好超过 A423 那条 **1e-4 世界单位（≈0.011 px）** 的阈值 ⇒ 那条就这么红的
            //   （`Editor/BattleScene.cs` 的 A423 段，两个轴的符号与量级逐位吻合）。
            //   ⛔ **别去放宽那条阈值**：0.011 px 是「照原版精确值写」的提醒（同族粗口径 `HudExtraPosPx`
            //   用的是 1.5 px，两档并存是对的）—— 要绿就给这里写精确值。
            //   ⚠️ 画出来的仍是 **61.846×61.846**（`m_PreserveAspect = 1` 内接、按高度画）：h 只动了 0.004 px，
            //   另两条 A423 断言（内接尺寸 < 0.05 px、命中区那两个常量）都不受影响。
            //   🔴 **2026-10-18（A964 前置）订正**：那两个常量的值今天改了 —— 命中区从错的 48.443×45.846
            //     改成 **80.443×77.846**（`m_RaycastPadding` 负值 = **外扩**，判据见 `CameraResetHitPxW` 上面那段）。
            //     上面「h 只动 0.004 px」与「内接尺寸」两条结论**不受这次改符号影响**（那两个量的是画出来的尺寸）。
            _cameraResetBtn = HudAbs(root, "40k_UI_bt_center_camera", 17.9293f, 568.174f, 64.443f, 61.846f, "CenterCameraButton");
            // 原版 `BattleHud.Initialize` 里对它先 `SetActive(false)`（+ `DOTween.Kill`）——
            // **它只在玩家手动动过镜头之后才出现**（判据见 `ToggleCameraResetButton`）。
            if (_cameraResetBtn != null) _cameraResetBtn.gameObject.SetActive(false);
            // `OffensiveButton` 109.01×106.94 @x[0,109] y[446.9,553.8]；图 128×124 → 0.85×（`:95`）
            // 🔴 **2026-10-14（A531）四个 px 写原版未取整值** —— 判据 = `RectTransform_3161.json`（挂 GO 376）
            //   `anchoredPosition (0, -13.800000190734863)` · `sizeDelta (109.00800323486328, 106.94300079345703)` ·
            //   `anchorMin = anchorMax = (0,0.5)` · `pivot (0,0)` ＋ 父链（`3161 → 3544「Left Anchor」
            //   → 四层 stretch → 裸 Transform`）⇒ 绝对 **x[0, 109.00800323486328] y[446.85699939727783, 553.8000001907349]**，
            //   中心 **(54.50400161743164, 500.32849979400635)**（`w/h` 也从取整的 109.01/106.94 换成精确值）。
            //   ⚠️ 与上一颗 `CenterCameraButton` **同一条「Left Anchor」父链**（同一族、同一份算法），
            //     所以「y 靠左锚点算」这件事有个已验的旁证：粗口径断言 `At("OffensiveButton", 54.5, 500.35)` 用的就是这个中心。
            // 🆕 2026-09-29（§25）：**那颗钮的显隐是有判据的**（原来我们画上去就一直亮着）——
            //   原版 `BattleHud.Initialize` 里先 `SetActive(false)`，之后**只有** `_ApplyOffensiveAndDefensiveEffects`
            //   会把它打开（判据 = 选定的那张卡 id **≠ 空卡 id**，`d__337:146-155`）；
            //   `isEmptyOffensiveCard` 也不「恒真」，它就是「id 等于本阵营空卡的 id」。
            //   ⇒ 我们照做：建完先关着，`ApplyOffensiveEnvOnce` 里按同一条判据开。
            _offensiveBtn = HudAbs(root, "40k_battle_icon_environmental", 0f, 446.85699939727783f, 109.00800323486328f, 106.94300079345703f, "OffensiveButton");
            if (_offensiveBtn != null) _offensiveBtn.gameObject.SetActive(false);

            // ---- 敌方名牌那颗「对手档案 / 联盟面板」钮（原版 `BattleHud.alliancePanelOpenButton`）----
            //  🆕 2026-10-18（A985③）。判据（逐字段亲读；**第一权威 = 解包资源 + 反编译**）：
            //
            //  🔴 **原版那颗钮就是敌方名牌本身，不是另起一颗图标** ——
            //   · `bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4934.json`（= `BattleHud`）
            //     的字段 `"alliancePanelOpenButton"` = `m_PathID 4686`；
            //   · 4686 = 挂在 `BackCanvas/Safe area BackCanvas/LeftArea/EnemyInfo`（GO 280）上的那颗
            //     uGUI `Button`（`MonoBehaviour_4686.json`：`m_Interactable 1` · `m_Transition 0` ·
            //     `m_OnClick` 里 **0 条** —— 运行时用 `UIGenericEventCatcher.SourceDelegate` 把
            //     `BattleHud.AlliancePanelOpenButton` 挂上去，`BattleHud__Initialize.c` 逐句）；
            //   · **13/13 战场同构**：每份 `BattleHud` 的该字段都落在**自己那份** `EnemyInfo` 上
            //     （13 个 `bundle_scenes_scenes_battlearena*` 逐场核过）。
            //
            //  📐 **rect**（判据 = `RectTransform_2687.json`，13 场逐位相同）：
            //   `anchorMin = anchorMax = (0,1)` · `m_Pivot = (0,1)` · `m_AnchoredPosition = (50, −28)` ·
            //   `m_SizeDelta = (260, 75)`；父链 `2687 → 2849(LeftArea) → 3498(Safe area BackCanvas)`
            //   `→ 2684(BackCanvas) → 2759(Canvas)` —— **中间三级全是 stretch + 零偏移**
            //   ⇒ **绝对 x[50, 310] · y[28, 103]**（1920×1080 · 左上原点）。
            //   （独立旁证：同一条链上 `ShowCemeteryBtn` 的 `ap (34.2, −140)` 反推出的中心 (84.2, 168)
            //    与本文件 `_cemeteryBtn` 那几个**已验**值逐位吻合。）
            //
            //  🎨 **贴图 = 没有**（⚠️ 这一格**不是「查不到」**，是**原版本来就没有**）：
            //   那颗钮的 `m_TargetGraphic` = `MonoBehaviour_4095.json`，它的 `m_Script` 指向
            //   `bundle_Waprforge_monoscripts/MonoScript/MonoScript_-2844744054636863780.json`
            //   = **`UnityEngine.UI.Extensions.NonDrawingGraphic`**，而
            //   `NonDrawingGraphic__OnPopulateMesh.c` 的**方法体是空的** ⇒ 它一个像素都不画，
            //   纯命中区（同族的 GO 名就叫 `UI Collider`）。⛔ **别去补一张图标**。
            //
            //  🎯 **命中区 = rect 按 `m_RaycastPadding` 四边外扩**（`MonoBehaviour_4095.json`：
            //   `(−24.780000686645508, −18.450000762939453, −25.290000915527344, 0)`，UGUI 分量序
            //   **L,B,R,T**、**负 = 往外扩**；符号判据与两条本地出处 → `CameraResetHitPxW` 上面那一段。
            //   13 场这一格逐位相同）⇒ **x[25.22, 335.29] · y[28, 121.45] = 310.07 × 93.45**。
            //
            //  👁 **显隐**：`BattleHud.Initialize` 只给它挂监听、**没有 `SetActive(false)`**
            //   （与同族 `offensiveCardButton` / `resetCameraZoomButton` 那两颗**不同**）⇒ **开局就亮着**。
            //
            //  🖱 **点它做什么**（`BattleHud__AlliancePanelOpenButton.c` 逐句）：取 `BattleManager.matchData`
            //   的名字 / 称号 / 联盟名 / 头像 / 评级 → `BattleAlliancePanel.Open(...)`。
            //
            //  ⚠️ **我们这边用一张全透明的 `ImageQuad` 占位**（= 原版那个 `NonDrawingGraphic` 的等价物）：
            //   本工程的点击一律是「自己算 `Contains(世界坐标)`」，`ImageQuad` 上根本没有 `raycastTarget`
            //   那个口子（同族先例逐字相同 → `Battle/ChatPopupPanel.cs` 的 `_close`）
            //   ⇒ 干脆把**命中矩形当成这张 quad 的几何**（`SetAspect` 拉成 310.07×93.45）——
            //   于是 `Contains` 就是命中判定，⛔ 不必再写第二份矩形。
            var alHit = AllianceButtonHitRectPx;
            _allianceBtn = HudImageTex(root, CardArt.Solid(), alHit.CX / 1920f, 1f - alHit.CY / 1080f,
                                       new Vector2(0.5f, 0.5f), Px(alHit.H), "AlliancePanelOpenButton");
            if (_allianceBtn != null)
            {
                _allianceBtn.SetAspect(alHit.W / alHit.H);          // 画出来的矩形 == 命中矩形（见上）
                _allianceBtn.SetTint(new Color(1f, 1f, 1f, 0f));    // ⛔ 别画出来（原版 `NonDrawingGraphic`）
            }
            else
            {
                // 不静默：取不到 `CardArt.Solid()` 时这颗钮会**整个缺席**（名牌点不动）
                Debug.LogWarning("[Battle] `AlliancePanelOpenButton` 建不出来（占位贴图取不到？）"
                               + "⇒ 原版「点敌方名牌开对手档案」那个入口**缺席**");
            }
            // 那扇窗本体（原版 `FrontCanvas/Alliance Panel`，组件 `BattleAlliancePanel`）。
            //   ⚠️ **它不是 `GameWindow`** ⇒ 不注册进 `WindowsManager`；出厂就关着（照原版 `Awake`）。
            _alliancePanel = AlliancePanelWindow.Create(root);

            // `ChatPopup` 面板本身（原版在 `FrontCanvas/Safe area/Unit Chat` 下，默认 `m_IsActive = false`）
            //   版面与逐节点坐标见 `资料/语音线_原版规格与ASR管道.md` §1.7.1；实现见 `Battle/ChatPopupPanel.cs`
            _chatPopup = ChatPopupPanel.Create(root);

            // ---- 任务点数字 `QPText '0/3'`：fs 40.5、**Bold**、白、H 居中 / V Capline ----
            // 出处：`子代理读报_back右区_0827.md:133`（敌 x[1840.9,1889.1] y[176.1,221.3]）
            //      与 `:154`（我 x[1841.8,1889.9] y[621.4,666.6]）。
            // ⚠️ 中心正好等于**任务点 holder 的中心**（我 (1865.9,644.2) / 敌 (1865.5,198.9)）
            //    —— 所以它画在那颗任务点图标上，不是另起一块。
            // 🔴 **2026-10-14（A531）中心写原版未取整值** —— 判据 = `RectTransform_3483.json`（我，GO 766）/
            //    `RectTransform_2607.json`（敌，GO 529）＋父链。两颗的**raw 值逐位相同**（同一份模板：`anchor (0.25624,0.27149)-(0.74631,0.73258)`
            //    · `pos (-0.14413070678710938, 0.10377883911132812)` · `size (0.28826314210891724, 0.20755687355995178)`），
            //    差别**全来自各自的 holder**（我 `QuestPointsHolder 1817.0..1914.7 / 595.4..693.1`、敌 `1816.1..1913.8 / 150.2..247.9`）：
            //    · 我 x[1841.7521903841, 1889.9241535099] y[621.3601940204, 666.6195685596] ⇒ **中心 (1865.8381719470285, 643.9898812899978)**
            //    · 敌 x[1840.8874767114, 1889.0594398372] y[176.0887886580, 221.3481631972] ⇒ **中心 (1864.9734582742763, 198.7184759276015)**
            //    ⚠️ 原来写 `1865.85 / 644.0`（我）与 `1865.0 / 198.7`（敌）—— 还是 0827 报告里取整到 0.1 的数
            //    （偏 0.010~0.027 px = 9.5e-5~2.5e-4 世界单位，**两个轴都在 1e-4 那一档的边上**）。
            // ✅ **2026-09-13 第三十三轮：任务点机制有了**（`PlayerState.QuestPoints`）。
            //    那批 DarkAngels 卡的卡面在「Gain N」后面画的正是 `questPointsN` 图标
            //    （OCR 把图标丢了、只留 `Gain 1`，有几张还被误标成 `[Energy]`）——
            //    所以这个数字**现在显示的是真值**，格式 `X/3`（规则书 `:199`「每获得 **3** 点任务」）。
            //    ⚠️ 数字**只在暗黑天使那一方**才显示（见上面 `ShowsQuestPoints` 的机器码级出处）。
            // ⚠️ 做法同原版：**照建、SetActive 切**——这样 `QpText` 与 `QuestIconPos` 在
            //    非暗黑天使的局里仍然量得到（自检要钉版面），只是看不见。
            var white = new Color(1f, 1f, 1f);
            // ⚠️ 这两个局部量在 `BuildHud` 里也有一份（那边管纹章和接片）—— 这里是**另一个方法**，
            //    不能共用局部量；判据本身只有 `ShowsQuestPoints` 一处，两处都调它，不算「写两份」。
            bool meQp = ShowsQuestPoints(_myFaction);
            bool foeQp = ShowsQuestPoints(_foeFaction);
            // 🔴 **2026-10-10（A1213①）**：框/四格 ← 原版 `…/{Player,Enemy}Mana/QuestPointsHolder/QPText`
            //    （`RectTransform/RectTransform_3483.json`(我) · `_2607.json`(敌)；
            //     框 **48.17×45.26**、`auto[15.79~40.5] base36`、折行 1）。
            //    ⚠️ 文案 `0/3`（3 字）塞在 48.17 px 宽的框里、字号上限 40.5 —— 原版就是这么紧
            //    （`m_fontSize = 40.5` = 上限 ⇒ 原版那时装得下）；我们这一侧字体更宽/行盒更高，
            //    落点会与原版不同 ⇒ 交给断言波 + 真 Play 量（见 `Hud` 的 doc）。
            _qpTextMe = Hud(root, "0/3", 1865.8381719470285f / 1920f, 1f - 643.9898812899978f / 1080f, 4, white,
                            new Vector2(0.5f, 0.5f), "QPText_Me",
                            48.17f, 45.26f, 15.79f, 40.5f, 36f);
            _qpTextFoe = Hud(root, "0/3", 1864.9734582742763f / 1920f, 1f - 198.7184759276015f / 1080f, 4, white,
                             new Vector2(0.5f, 0.5f), "QPText_Foe",
                             48.17f, 45.26f, 15.79f, 40.5f, 36f);
            _qpTextMe.gameObject.SetActive(meQp);
            _qpTextFoe.gameObject.SetActive(foeQp);
            // 🆕 **2026-10-16（A712 阶段 2）**：纵向档 = 原版 `QPText '0/3'` 的 **`V Capline`**
            //   —— 判据 = 上面 `:8504` 那一行自己逐字写着「fs 40.5、**Bold**、白、H 居中 / **V Capline**」
            //   （出处 `子代理读报_back右区_0827.md:133`（敌）`:154`（我）+ `RectTransform_3483/2607.json`）。
            //   `Capline` 不吃框高 ⇒ 只传档。文案恒 `"{任务点}/3"`（单行）。
            if (_qpTextMe != null) _qpTextMe.SetVAlign(Label.VAlign.Capline);
            if (_qpTextFoe != null) _qpTextFoe.SetVAlign(Label.VAlign.Capline);

            // ---- `Energy Accumulation`（能量累积那盏灯）：77.786×80.113 ----
            // 出处：`子代理读报_back右区_0827.md:142`（**敌** x[1746.7,1824.4] y[247.9,328.0]，102×102 → 0.763×）
            // ⚠️ **这一行原来写的是**「与我方那个是**同一个相对位置**（holder 中心的 (-81.3, +0.5)）」——
            //    **2026-10-14（A531）就地订正：不是同一个位置**，我方是 `(-80.2, +0.5)`（见下）。
            // 🔴 **2026-10-14（A531）逐件取原版父链：上面那句「同一个相对位置」【是错的】** ——
            //   两侧 `anchoredPosition` **不一样**：`RectTransform_2939.json`（敌 OFF，GO 679）= `(-81.30000305175781, 0.5)`，
            //   而 `RectTransform_2719.json`（**我** OFF，GO 977）= **`(-80.19999694824219, 0.5)`**（两证：
            //   ① 0827 报告 `:162` 自己写的就是「我 x[1748.6,1826.4]」；② **实况 dump** 同一条
            //   `runtime_ui_dump_drive_0912.tsv:287`（我）/`:258`（敌）—— 我 `(-80.2,0.5)`、敌 `(-81.3,0.5)`）。
            //   ⇒ 我方那盏灯原来**照抄了敌方的 x**（1746.7），比原版**偏左 1.864 px**（= 1.7e-2 世界单位，
            //     是「取整」误差的 20 倍）—— 这一笔不是取整，是**真偏离**。今天一并订正。
            //   精确矩形（父链走完，同 A513 那条算法）：
            //   · 我 `pos (-80.19999694824219,0.5)` `size (-22.213993072509766,-19.886978149414062)` `anchor (0,0)-(1,1)`
            //     ＋父链（`Energy Accumulation(100×100) → ManaHolder → PlayerMana → …`）
            //     ⇒ x[1748.5645137293218, 1826.3505206568] y[515.5400514454191, 595.6530732960]，中心 **(1787.457517193, 555.596562371)**
            //   · 敌同式 ⇒ x[1746.6571888152716, 1824.4431957428] y[247.9047392226712, 328.0177610733]，中心 **(1785.550192279, 287.961250148)**
            //   ⚠️ `w/h`（旧 77.8/80.1）也换成精确值 77.78600692749023 / 80.11302185058594。
            //   ⚠️ **ON / OFF 两颗 rect 逐位相同**（`RectTransform_3424`/`3152` 与 `2719`/`2939` 同参数，
            //     运行时切显隐）⇒ 只算一套，⛔ 别给 ON 另写一组数。
            // 🔴 **2026-09-17 更正**：这里原来写「ON（`40k_battle_energy_full`）什么时候显示**没查到**
            //    （切换逻辑不在本地）⇒ 固定摆 OFF 那张，**这是我们挑的**」——
            //    **判据现在查到了**：`BattleManager__SetupBoardPhase.c:181/196` 是
            //    `PlayerManager.SetAccumulationMana(0 < *(int*)(vars + 0x34))`，那个字段是
            //    **`Everguild/LiveOps/GameplayVariablesData.manaAccumulation`**（int，
            //    签名桩 `Everguild/LiveOps/GameplayVariablesData.cs:32`）⇒ **判据 = `0 < manaAccumulation`**。
            //    ⚠️ **但那个数值是 LiveOps（服务端下发）的，本地拿不到**（与 `overtimeTurn` 同一类）。
            //    ⇒ 现在照原版写成**字段 + 原判据**，默认 0（= 关，与实况 dump 拍到的 OFF 那张一致）；
            //      **数值本身仍是我们挑的**，判据不是。
            string accumArt = manaAccumulation > 0 ? "40k_battle_energy_full" : "40k_battle_energy_empty";
            HudAbs(root, accumArt, 1746.6571888152716f, 247.9047392226712f, 77.78600692749023f, 80.11302185058594f, "EnergyAccumulation_Foe");
            HudAbs(root, accumArt, 1748.5645137293218f, 515.5400514454191f, 77.78600692749023f, 80.11302185058594f, "EnergyAccumulation_Me");

            // ---- 加时标记 `OvertimeIndicator`：68.625×70.992，图 `40k_icon_overtime`（preserveAspect=1）----
            // 出处：`子代理读报_back右区_0827.md:171`（x[1718.9,1787.5] y[341.5,412.5]，
            //       174×180 → 0.394×）。它在能量 holder 内、时钟左边。
            // 🔴 **2026-10-14（A531）四个 px 写原版未取整值** —— 判据 = `RectTransform_3565.json`（挂 GO 360）
            //   `anchoredPosition (-170.60000610351562, 46.5)` · `sizeDelta (68.625, 70.99199676513672)` ·
            //   `anchorMin = anchorMax = (1,0.5)` · `pivot (0.5,0.5)` ＋父链（`3565 → 3359「Energy And turn holder」
            //   (238.79×356.58) → 2965「Right Anchor」(100×100，右中) → 3514/3498/2684（stretch）→ 裸 Transform`）
            //   ⇒ 绝对 **x[1718.8823928833008, 1787.5073928833008] y[341.50400161743164, 412.49599838256836]**，
            //   中心 **(1753.1948928833008, 377.0)**。⚠️ 旧值 `1718.9 / 341.5 / 68.62 / 70.99` 又是 0827 报告取整后的数
            //   （偏 0.018 / 0.004 px = 1.6e-4 / 3.7e-5 世界单位 —— x 那一轴超 A423/A513 那档 1e-4 阈值）。
            //   ⚠️ y 的**中心正好是 377.0**（父 holder 与 clock 对齐），但那不代表四个 px 可以是取整值。
            // ⚠️ **默认关着**：原版也只在加时里出现（`OvertimeUi.DisplayOvertime`：淡入 1s/停 1s/淡出 1s）。
            //    🆕 2026-09-20：**加时机制已经接上了**（引擎 `ctx.IsOvertime`，判据见 `BattleContext.IsOvertime`）
            //    —— 它现在会在对局里真的亮起来，不再是「摆着好看」。
            _overtime = HudAbs(root, "40k_icon_overtime", 1718.8823928833008f, 341.50400161743164f, 68.625f, 70.99199676513672f, "OvertimeIndicator");
            if (_overtime != null) _overtime.gameObject.SetActive(false);

            BuildOvertimeSplash(root);
        }

        /// <summary>加时**全屏 splash**（原版 `OvertimeSplashText`）。层级与数值全部来自**实况 dump**
        /// `资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_drive_0912.tsv`：
        /// <code>
        /// FrontCanvas/Safe area FrontCanvas/AboveShader/OvertimeSplashText   stretch 全屏 · 默认 inactive
        ///   ├ Background       anchor stretch · sizeDelta (716.6, 699.9) · 色 (0,0,0,0.533)
        ///   ├ Text Container   anchor stretch · sizeDelta (2.0, -981.0) · 图 40k_dsplay_overtime
        ///   │   └ Text         "OVERTIME!" 白 · **fontSize 80** · 居中
        ///   │       └ Image    181.3×187.3 · pivot 右中 · 锚在 Text 左缘再 -15 px · 图 40k_icon_overtime
        /// </code>
        /// 字号来源：`07_场景/battlearena1/MonoBehaviour/MonoBehaviour_3973.json` 的
        /// `m_text = "OVERTIME!"` / `m_fontSize = 80.0` / `m_fontColor = (1,1,1,1)`。
        /// ⚠️ 我们**不是 uGUI**（HUD 是 quad + TMP），所以「anchor stretch + sizeDelta」在这里
        /// **换算成了实际像素尺寸**（stretch 轴的实际尺寸 = 屏 + sizeDelta）。
        /// ⚠️ **推断（不是实读）**：那个 `Image` 的 dump 是 `anchor(0,0.5) / pivot(1,0.5) / pos(-15,0)`，
        /// 而 `Text` 的 rect 宽度在 dump 里是 0（TMP 自适应）⇒ 真实语义只能按「**图标贴在文字左边**」实现。</summary>
        void BuildOvertimeSplash(Transform root)
        {
            // 相机看 +Z（z 越大越远）；HUD 文字在 z=0、图 0.3、装饰 0.6 ⇒ splash 要**最靠前**，取负
            const float zBg = -0.50f, zBand = -0.51f, zText = -0.52f, zIcon = -0.53f;

            _overtimeSplashRoot = new GameObject("OvertimeSplashText", typeof(RectTransform));
            _overtimeSplashRoot.transform.SetParent(root, false);
            // 🔴 **2026-10-11（A218）**：这一件写 `sizeDelta` —— 判据 = 原版同名件 `OvertimeSplashText` 实读：
            //    `RectTransform` · `anchor (0,0)-(1,1)` · `sizeDelta (0,0)`（`bundle_scenes_scenes_battlearena1`，
            //    2026-10-11 现读）⇒ **绝对矩形 (0,0)-(1920,1080)**（stretch 拿父 = 全屏）。
            //    它下面那三层的绝对尺寸才由各自的 `stretch + sizeDelta` 算（见下面 ① / ② 那两行），
            //    ⛔ 别把 `bgW/bgH`（2096.6×1779.9）当成这一层 —— 那是**子件** `Background` 的实际尺寸。
            //    改坏法：删掉 `SetPxSize` ⇒ `Editor/BattleScene.cs` §A218「`OvertimeSplashText` = 整屏矩形」红。
            MenuDraw.SetPxSize(_overtimeSplashRoot.transform, LayoutSpace.DesignPxW, LayoutSpace.DesignPxH);
            var rt = _overtimeSplashRoot.transform;

            // ① 全屏黑罩：stretch + sizeDelta(716.6, 699.9) ⇒ 实际 (1920+716.6) × (1080+699.9)
            float bgW = 1920f + 716.6f, bgH = 1080f + 699.9f;
            _overtimeSplashBg = ImageQuad.Create(rt, Texture2D.whiteTexture, new Vector3(0f, 0f, zBg),
                                                 bgH / 108f, new Vector2(0.5f, 0.5f), "Background");
            if (_overtimeSplashBg != null)
            {
                _overtimeSplashBg.SetAspect(bgW / bgH);
                _overtimeSplashBg.SetTint(new Color(0f, 0f, 0f, 0.533f));
            }

            // ② 中间那条横带：stretch + sizeDelta(2.0, -981.0) ⇒ 实际 1922 × 99
            //    ⚠️ 原图只有 5×198（竖条）且 **没有 9-slice 边框**（`m_Border=(0,0,0,0)`，
            //       新解包 `bundle_atlasindividual_assets_battleatlasui/Sprite/40k_dsplay_overtime.json`）
            //       ⇒ 原版也是这么拉开的，照做。
            float bandW = 1920f + 2f, bandH = 1080f - 981f;
            var bandTex = CardArt.Ui("40k_dsplay_overtime");
            if (bandTex == null) Debug.LogWarning("[Battle] 🔴 找不到 `40k_dsplay_overtime` —— 加时 splash 的横带画不出来");
            _overtimeSplashBand = ImageQuad.Create(rt, bandTex, new Vector3(0f, 0f, zBand),
                                                   bandH / 108f, new Vector2(0.5f, 0.5f), "Text Container");
            if (_overtimeSplashBand != null) _overtimeSplashBand.SetAspect(bandW / bandH);

            // ③ `OVERTIME!`（白、fontSize 80、居中）
            // 🔴 **2026-10-18（第十二轮 · W6）**：文案走**原版词条** `Battle/Overtime/Title`
            //    （判据：`bundle_scenes_scenes_battlearena1/MonoBehaviour_4745.json` 的 `Localize.mTerm`
            //     + 同族 TMP 逐字 `OVERTIME!`；13 个 arena 每场 1 颗）—— 原来这里是写死的英文 `"OVERTIME!"`。
            _overtimeSplashText = Label.Create(rt, Loc.T(OvertimeTerm), new Vector3(0f, 0f, zText), 1,
                                               Color.white, new Vector2(0.5f, 0.5f), "Text");
            if (_overtimeSplashText != null)
            {
                // 🔴 **不能照抄原版那个 `m_fontSize = 80`** —— 那是**原版自己画布**的单位。
                //    （2026-09-20 踩过：写成 `SetCapHeight(80 * WorldCapPerFontSize)` 之后
                //     「OVERTIME!」一个字母占了大半屏 ≈335 px。）跨工程能对齐的只有**渲染出来的高度**：
                //    原版 fontSize 80 ⇒ 大写高 ≈ 0.72 em = **57.6 px**（`TmpFont` 里那条
                //    「拉丁大写高约 0.72 em」），本工程 108 px = 1 世界单位 ⇒ **0.5333 世界单位**。
                //    ⚠️ **2026-10-18（W6）**：语种要跟着走 —— 中文档那格是「加时！」，按**汉字**（1 em）量，
                //    写死 0.72 em 会小 28% ⇒ 转发到 `Label.SetScriptHeight`（唯一那份语种判据）。
                const float origFontSize = 80f;         // `MonoBehaviour_3973.json` 的 m_fontSize
                _overtimeSplashText.SetScriptHeight(Loc.T(OvertimeTerm), origFontSize, 108f);
            }

            // ④ 字左边那枚图标 181.3×187.3（贴到文字的左侧、再往左 15 px）
            //    ⚠️ 位置靠 `LayoutHudLabels` 之后按文字实际宽度重算 —— 见 `PlaceOvertimeIcon`
            _overtimeSplashIcon = ImageQuad.Create(rt, CardArt.Ui("40k_icon_overtime"),
                                                   new Vector3(0f, 0f, zIcon), 187.3f / 108f,
                                                   new Vector2(1f, 0.5f), "Image");
            if (_overtimeSplashIcon != null) _overtimeSplashIcon.SetAspect(181.3f / 187.3f);

            _overtimeSplashRoot.SetActive(false);
        }

        /// <summary>把 splash 那枚图标摆到 `OVERTIME!` 的左边。
        /// 原版：`anchor(0,0.5) / pivot(1,0.5) / pos(-15,0)` ⇒ 右边缘贴着文字的左边缘、再往左 15 px。
        /// 文字宽度只有建完才知道 ⇒ 每次亮起来之前重算一次。</summary>
        void PlaceOvertimeIcon()
        {
            if (_overtimeSplashIcon == null || _overtimeSplashText == null) return;
            float halfW = _overtimeSplashText.WorldW * 0.5f;
            var p = _overtimeSplashIcon.transform.localPosition;
            _overtimeSplashIcon.transform.localPosition = new Vector3(-halfW - 15f / 108f, 0f, p.z);
        }

        /// <summary>进加时那一下：**`OvertimeIndicator` 与全屏 splash 一起亮、一起灭**。
        /// 原版 `OvertimeUi.DisplayOvertime`（`decomp_out/OvertimeUi__DisplayOvertime.c`）：
        /// <code>
        /// overtimeIndicatorIcon.gameObject.SetActive(true);      // = GO 4408「OvertimeIndicator」
        /// overtimeSplashCanvasGroup.gameObject.SetActive(true);  // = GO 3592「OvertimeSplashText」
        /// DOTween.Sequence().Join(DOFade(icon,1,fadeTime)).Join(DOFade(splash,1,fadeTime))
        ///                   .AppendInterval(fadeTime).Append(DOFade(icon,0,fadeTime)).AppendCallback(λ)
        /// </code>
        /// ⇒ **淡入 1.0 s → 停留 1.0 s → 淡出 1.0 s**（`fadeTime` 取资产值 **1.0**，不是 ctor 默认 0.5）。
        /// ⚠️ **两个都是「闪一下就走」，不是常亮** —— 这是代码写的，不是我挑的。
        /// ⚠️ 我们不用 DOTween（批处理下没有帧循环），改成手推的计时；`TickOvertime` 也能被自检直接推。</summary>
        public void ShowOvertime()
        {
            if (_overtimeFired) return;
            _overtimeFired = true;

            if (_overtime != null) _overtime.gameObject.SetActive(true);
            if (_overtimeSplashRoot != null)
            {
                PlaceOvertimeIcon();
                _overtimeSplashRoot.SetActive(true);
            }
            _overtimeAnimT = 0f;
            SetOvertimeAlpha(0f);

            // 音效：原版 `OvertimeUi.enteringOvertimeSound` = AudioCue `OvertimeStart`
            //（参数照抄资产 `bundle_soundcollection_assets_all/MonoBehaviour/OvertimeStart.json`：
            //  `minPitch = maxPitch = 1.0` · `minVolume = maxVolume = 1.0` · 单个 clip · 2D）⇒ **不加随机**。
            // ⚠️ 音频本地原本是**缺 setup 头的 FSB5 裸流**（`av.open` 报 EOFError、播不了）；**2026-09-22 已能重建**：
            //    `工具/rebuild_overtime_start_ogg.py` → `Resources/Art/audio/sfx/OvertimeStart.wav`。
            var overtimeClip = WarpforgeVFX.WFSoundBank.Clip("OvertimeStart");
            if (overtimeClip != null) WarpforgeVFX.WFSoundPlayer.Play(overtimeClip, is2d: true);
            else Debug.LogWarning("[Battle] 🔴 加时 splash 亮了，但 `OvertimeStart` 音频没加载到 —— "
                                + "跑 `python 工具/rebuild_overtime_start_ogg.py` 重建（不许静默）");
        }

        void SetOvertimeAlpha(float a)
        {
            var w = new Color(1f, 1f, 1f, a);
            if (_overtimeSplashBg   != null) _overtimeSplashBg.SetTint(new Color(0f, 0f, 0f, 0.533f * a));
            if (_overtimeSplashBand != null) _overtimeSplashBand.SetTint(w);
            if (_overtimeSplashIcon != null) _overtimeSplashIcon.SetTint(w);
            if (_overtimeSplashText != null) _overtimeSplashText.SetColor(w);
            if (_overtime           != null) _overtime.SetTint(w);
        }

        /// <summary>加时 splash 的淡入/停留/淡出（原版 1.0 / 1.0 / 1.0 秒）。`dt` 单位秒。</summary>
        public void TickOvertime(float dt)
        {
            if (_overtimeAnimT < 0f) return;
            const float F = 1.0f;                       // fadeTime，原版资产值
            _overtimeAnimT += dt;
            float t = _overtimeAnimT, a;
            if (t < F)             a = t / F;                       // 淡入
            else if (t < 2f * F)   a = 1f;                          // 停留
            else if (t < 3f * F)   a = 1f - (t - 2f * F) / F;       // 淡出
            else
            {
                a = 0f; _overtimeAnimT = -1f;
                if (_overtimeSplashRoot != null) _overtimeSplashRoot.SetActive(false);
                if (_overtime != null) _overtime.gameObject.SetActive(false);
            }
            SetOvertimeAlpha(a);
        }

        // ==================================================================
        //  🆕 2026-10-18（§8b · 2a）`Aspect Ratio Filler{Top,Bottom}` —— 两枚纯黑 quad
        // ==================================================================

        /// <summary>建原版 **`Aspect Ratio Filler Top` / `Aspect Ratio Filler Bottom`**（**13/13 战场都有**）。
        ///
        /// <para>🔴 **原版判据（逐位，全是实读）**：
        /// `assets_full/bundle_scenes_scenes_battlearena1/GameObject/Aspect Ratio Filler {Top,Bottom}.json`
        /// ＋ `RectTransform/RectTransform_{3021,2916}.json` ＋ `MonoBehaviour/MonoBehaviour_{5038,5206}.json`：
        /// · `RectTransform`：`anchor = pivot = (0.5,0.5)` · `sizeDelta` **3252.123046875 × 1842.02001953125** ·
        ///   Top `anchoredPosition (0.0006713899783790112, 1641.0999755859375)`、
        ///   Bottom `(−3.051800013054162e-05, −1641.0999755859375)` · 父 = Canvas 自己的 RT（`RectTransform_2759`）；
        /// · `Image`：`m_Sprite` = **null**（`m_PathID 0`）· `m_Color` = **(0,0,0,1)** ·
        ///   `m_RaycastTarget` = **0** · `m_Type = 0`(Simple) · `m_Material` = null。
        /// ⇒ 我们这边 = `Texture2D.whiteTexture`（配 `SetTint` 画纯色，同 `_overtimeSplashBg` 那套）
        /// ＋ **不接命中**（本工程没有 uGUI raycastTarget 这一层 —— 这两枚也从没进过任何命中判定）。</para>
        ///
        /// <para>⚠️ **它们在屏外 —— 这与原版一致，不是漏画**：中心距画布中心 ±1641.10、高 1842.02
        /// ⇒ 内缘在 `1641.0999755859375 − 1842.02/2 = 720.089…`，而 16:9 画布半高只有 540
        /// ⇒ **下缘落在屏幕上缘之外 180.09 px**。原版也要窄到 `< 4:3`（`CanvasScaler` 是
        /// `ScaleWithScreenSize` + `match-width`，画布会变高）才可能露出来；**我们这套坐标的可见高恒
        /// 10 世界单位**（`LayoutSpace`）⇒ 照值摆它们**恒在屏外**。⇒ 本件建它们的意义 =
        /// **存在性与参数逐值一致**（原版 13/13 都有这两颗，`m_IsActive` 都是 true）；
        /// 真正的黑边由 <see cref="BuildScreenAspectRatioController"/> 写相机 rect + `GL.Clear` 产生。</para>
        ///
        /// <para>⚠️ **分层（等价物，如实标）**：原版这两颗是 Canvas 的**头两个子件**（`RT 2759.m_Children[0..1]`，
        /// 排在 `BackCanvas`/`FrontCanvas`/`BattleDoors` 之前）⇒ **画在所有 UI 之下**。
        /// 我们没有 uGUI 的兄弟序 ⇒ 等价物 = **渲染队列**（`CLAUDE.md` §三）+ 一个**很靠后**的 z：
        /// 队列 `2999`（HUD 全族是默认 3000 起）、z = `+5`（相机看 +Z ⇒ z 越大越远）。</para>
        /// ✍️ 该藏的时候：**永远不用藏** —— 原版这两颗 `m_IsActive` 13/13 都是 `true`、也没有任何代码开关它们
        /// （全库按名 grep 0 命中，见报告 §五）。它们「看不见」是**几何**造成的，不是显隐。</summary>
        void BuildAspectRatioFillers(Transform root)
        {
            const int QFiller = 2999;         // 比 HUD 全族的 3000 小 ⇒ 垫在最底下
            const float ZFiller = 5f;         // 相机看 +Z ⇒ z 越大越远（HUD 文字 0 / 图 0.3 / 装饰 0.6）

            // 自上而下的 px：Top = 540 − 1641.0999755859375、Bottom = 540 + 1641.0999755859375
            float topY = LayoutSpace.DesignPxH * 0.5f - AspectFillerTopY;
            float botY = LayoutSpace.DesignPxH * 0.5f - AspectFillerBottomY;
            float topCx = LayoutSpace.DesignPxW * 0.5f + AspectFillerTopX;
            float botCx = LayoutSpace.DesignPxW * 0.5f + AspectFillerBottomX;

            _aspectFillerTop = BuildOneAspectFiller(root, "Aspect Ratio Filler Top", topCx, topY, QFiller, ZFiller);
            _aspectFillerBottom = BuildOneAspectFiller(root, "Aspect Ratio Filler Bottom", botCx, botY, QFiller, ZFiller);
        }

        ImageQuad BuildOneAspectFiller(Transform root, string name, float cxPx, float cyPx, int queue, float z)
        {
            var q = ImageQuad.Create(root, Texture2D.whiteTexture, LayoutSpace.FromPixel(cxPx, cyPx),
                                     Px(AspectFillerH), new Vector2(0.5f, 0.5f), name);
            if (q == null)
            {
                Debug.LogWarning("[Battle] 🔴 `" + name + "` 建不出来（`ImageQuad.Create` 回了 null）"
                               + " —— 原版 13/13 战场都有这一颗，别静默");
                return null;
            }
            q.SetAspect(AspectFillerW / AspectFillerH);          // 原版是 `m_Type = 0`(Simple) + 没有图 ⇒ 按矩形拉伸
            q.SetTint(new Color(0f, 0f, 0f, 1f));                // `m_Color = (0,0,0,1)`
            q.SetRenderQueue(queue);
            q.transform.localPosition += new Vector3(0f, 0f, z);
            return q;
        }

        // ==================================================================
        //  🆕 2026-10-18（§8b · 2a 的机制那半 + 2b）两件挂 `BattlePrefab` / `Canvas` 上的件
        // ==================================================================

        /// <summary>建原版 **`ScreenAspectRatioController`**（挂 GO `BattlePrefab` 上）。
        /// 我们挂在**驱动自己那个 GO**（= 场景根 `Battle`，就是原版 `BattlePrefab` 的对应物）。
        /// <para>⚠️ **批处理下 `Start` 跑不跑没有定论**（本赛季的既有口径）⇒ 建完**显式调一次
        /// `Attach()`**（它自己幂等，见那件的文件头 A）。</para>
        /// <para>✍️ 该藏的时候：这件**没有显隐**（原版也没有）—— 它只写 `Camera.rect`；
        /// 比例回到 [min,max] 之内时它把 rect 写回**整幅 (0,0,1,1)**（就是「藏」）。</para></summary>
        void BuildScreenAspectRatioController()
        {
            if (cam == null && boardCam == null)
            {
                Debug.LogWarning("[Battle] 两台相机都是 null ⇒ **不建 `ScreenAspectRatioController`** —— "
                               + "比例超出 [4:3, 22:9] 时的黑边这一档没有落点（原版那件挂在 `BattlePrefab` 上）。");
                return;
            }
            _aspectRatio = gameObject.AddComponent<ScreenAspectRatioController>();
            // 原版 `targetCameras = [Camera_1461(3D BoardCamera), Camera_1462(UI 相机)]` —— **顺序照原版**
            _aspectRatio.SetTargetCameras(boardCam, cam);
            _aspectRatio.Attach();
        }

        /// <summary>建原版 **`UI Error Message Controller (MUST BE ENABLED)`**（判据 → `ErrorMessageBanner.cs`）。
        /// <para>✍️ 该藏的时候：**5 条各自演完自己关**（`0.15 + 2.0 + 0.25 = 2.25 s` ⇒ `SetActive(false)`，
        /// 同原版 `OnComplete`）；建出来时**全关着**（原版 `Awake` 也是：模板先关、再从它克隆 5 份）。
        /// 原版**没有**任何「换局清横幅」的代码 ⇒ 我们也不加（自检口 <see cref="ErrorMessageBanner.HideAll"/>
        /// 留着但**生产路径不调**）。</para></summary>
        void BuildErrorBanner(Transform root)
        {
            _errorBanner = ErrorMessageBanner.Create(root);
        }

        // ==================================================================
        //  🆕 2026-10-18（§8b · 2b）疲劳 → 错误横幅
        // ==================================================================

        /// <summary>`Battle/Tips/DamageFatigue{,Enemy}` 的**文案**（原版是 `String.Format(词条, 疲劳数)`）。
        /// <para>🔴 **`Core/Loc.cs` 那张表是权威** —— 本件只做**转发**（同 <see cref="HandFullText"/> 的口径）：
        /// 表里有这条键就走它、并按 `{0}` 填疲劳数；没有才用下面那两句兜底。
        /// 📌 那张表补上这两条之后，兜底**自动失效、这里一个字都不用改**。</para>
        /// <para>⚠️ **兜底两句是我们写的、⛔ 不是「原版就是这样」**：原版显示串在**远端 I2 表**
        /// （本地 84 个 bundle 里没有 `localization_assets_all.bundle`）⇒ 正式文案拿不到（铁律 11 例外①）。</para>
        /// <para>⚠️ 不是简单的 `return`：`string.Format` 遇到没有 `{0}` 的串**照旧返回原串**是错觉 ——
        /// 串里若有别的花括号占位符（`{1}` …）它会**抛 `FormatException`** ⇒ 先判 `{0}` 在不在。</para></summary>
        /// <param name="mine">true = 我方牌库抽空、false = 对手那侧（原版两条键就是这么分的）。</param>
        /// <param name="fatigue">疲劳值（= 本次受到的伤害）。</param>
        public static string DamageFatigueText(bool mine, int fatigue)
        {
            return DamageFatigueTextWithKey(mine ? DamageFatigueTerm : DamageFatigueEnemyTerm, mine, fatigue);
        }

        /// <summary>🆕 **两态判据本身**，键由调用方给（同 `HintForCodeWithKey` 那条先例）。
        /// <para>🔴 **为什么要开这个口**：本件要用的那两条键（`Battle/Tips/DamageFatigue{,Enemy}`）
        /// **今天仍然不在 `Loc` 表里**（原版显示串在**远端 I2 表**，本地 0 个 value）
        /// ⇒ **走单参那条路，「有键且表里有值」这一态根本构造不出来**，而 `Loc` 那张表**没有任何
        /// 「按测试插一条」的口**（公开口只有 `HasEntry` / `EnOf`）⇒ 自检要钉「键在表里 ⇒ 走词条、
        /// ⛔ 不是走兜底句」就必须能直接喂一个**确实在表里**的键。</para>
        /// <para>✅ **2026-10-09（`A1018①`）就地订正**：这里原来写「**`Battle/Tips/*` 那族键今天
        /// 一条都不在 `Loc` 表里**」—— 那句**今天不成立**（表里已有十几条 `Battle/Tips/*`，
        /// 只是**不含本件要用的那两条**）⇒ 收窄成「本件要用的那两条」。</para>
        /// <para>⛔ **产品代码一律走单参那条**。</para></summary>
        public static string DamageFatigueTextWithKey(string key, bool mine, int fatigue)
        {
            string s = (key != null && Loc.HasEntry(key))
                     ? CardText.Term(key)
                     : (mine ? (CardText.Zh ? "牌库已空 —— 督军受到 {0} 点疲劳伤害"
                                            : "Deck is empty - the warlord takes {0} fatigue damage")
                             : (CardText.Zh ? "对手牌库已空 —— 其督军受到 {0} 点疲劳伤害"
                                            : "Enemy deck is empty - their warlord takes {0} fatigue damage"));
            return s != null && s.Contains("{0}") ? string.Format(s, fatigue) : s;
        }

        /// <summary>把**本次 drain 到的事件**里那几条疲劳挑出来弹横幅（`PlaySignals` 每批调一次）。
        /// <para>🔴 **为什么要自己挑**：我们引擎把疲劳**并进了 `EvtKind.Hit`**
        /// （判据 = `RuleEngine/Core/BattleEvent.cs` 里 `EvtKind.Hit` 的 doc：「**含护盾挡下（Amount = 0）和疲劳**」；
        /// 发出点 = `RuleCore` 里那句 `ctx.Emit(EvtKind.Hit, p, BoardSpec.WarlordSlot, ps.Warlord.Name, amount: ps.Fatigue)`）
        /// —— **没有专用事件种类**，而 `RuleEngine/` **不在本件的白名单里**（⛔ 不许为它加一种）。
        /// 原版那条链是 `BattleManager._ResolveFatigue` 直接调 `UIMessageController.ShowError`。</para>
        /// <para>判据（六道闸，全在下面那段 if 里逐条写了理由）：
        /// <c>Kind == Hit</c> ＋ <c>Slot == 4</c>(督军) ＋ <c>Amount ≥ 1</c> ＋
        /// <c>Amount == 引擎里那一方的 Fatigue</c> ＋ <c>那一方牌库为空</c> ＋
        /// <c>同批里没有一条 Attack 打的就是这一方的督军</c> ＋ <c>这个疲劳值还没弹过</c>。</para>
        /// <para>⚠️ **这是我们的近似判据**（原版不需要猜 —— 它在那条协程里）⇒ 如实标两条已知偏差：
        /// ① 它可能把「牌库刚好空的同一刻、一次伤害量正好等于疲劳值的攻击」误判成疲劳；
        /// ② 一次抽多张而牌库空两次时，两条事件**在 drain 时读到的 `Fatigue` 都是最后一个值**
        /// ⇒ 只有最后那一条过得了 `e.Amount == ps.Fatigue` 那道闸，**只会弹一条横幅**（值是对的）。
        /// 真值只能等引擎侧单开一种事件（要动 `RuleEngine/`，不在本件白名单）。</para></summary>
        void NoteFatigueBanners(List<BattleEvent> batch)
        {
            if (Ctx == null || batch == null) return;
            for (int i = 0; i < batch.Count; i++)
            {
                var e = batch[i];
                if (e == null || e.Kind != EvtKind.Hit) continue;
                if (e.Player < 0 || e.Player > 1) continue;
                if (e.Slot != BoardSpec.WarlordSlot) continue;      // 只有**督军**挨的那一条（槽位恒 4）
                if (e.Amount < 1) continue;                          // 0 = 被挡下，不是疲劳

                var ps = Ctx.Players[e.Player];
                if (ps == null) continue;
                if (ps.Fatigue < _fatigueShown[e.Player]) _fatigueShown[e.Player] = 0;   // 新一局（疲劳回 0）⇒ 记账跟着清
                if (e.Amount != ps.Fatigue) continue;                // 疲劳伤害的数值**恒等于**那个计数
                if (ps.Deck == null || ps.Deck.Count != 0) continue; // 只有牌库空才谈疲劳
                if (e.Amount <= _fatigueShown[e.Player]) continue;   // 这个值已经弹过（防同一值弹两次）

                bool looksLikeAttack = false;
                for (int k = 0; k < batch.Count; k++)
                {
                    var a = batch[k];
                    if (a != null && a.Kind == EvtKind.Attack
                        && a.TargetPlayer == e.Player && a.TargetSlot == BoardSpec.WarlordSlot)
                    { looksLikeAttack = true; break; }
                }
                if (looksLikeAttack) continue;                       // 那一刀打的也是这个督军 ⇒ 不算疲劳

                _fatigueShown[e.Player] = e.Amount;
                if (_errorBanner != null)
                    _errorBanner.ShowError(DamageFatigueText(e.Player == _me, e.Amount));
                else
                    Debug.LogWarning("[Battle] 疲劳 " + e.Amount + " 点发生了，但**错误横幅没建出来**"
                                   + " ⇒ 原版这一刻会弹 `Battle/Tips/DamageFatigue`（不许静默）");
            }
        }

        /// <summary>自检口：直接喂一个合成事件走一遍疲劳判据（批处理里构造真疲劳要打完一整副牌库）。
        /// ⛔ **产品代码不调它**；它调的是**同一个** <see cref="NoteFatigueBanners"/>。</summary>
        public void NoteFatigueBannersForTest(List<BattleEvent> batch) { NoteFatigueBanners(batch); }

        /// <summary>自检口：那一方的疲劳记账值（「弹到几了」）。</summary>
        public int FatigueShownForTest(int player)
        {
            return (player >= 0 && player < _fatigueShown.Length) ? _fatigueShown[player] : -1;
        }

        /// <summary>
        /// 把两盏回合灯摆到牌堆底板的右下角。
        /// ⚠️ **必须可重入** —— `ReanchorHud` 换分辨率时会把所有 HUD 图拉回各自的锚点，
        ///    那会把灯上的偏移抹掉，所以那边也要再调一次。这里是从零算出来的，调几次都一样。
        /// </summary>
        void PlaceDeckLights()
        {
            float z = HudImageZ - 0.01f;          // 比卡背再靠前一点，别被压住
            if (_myDeckLight != null)
            {
                var p = LayoutSpace.ToWorld(MyDeckX01, MyDeckY01)
                      + new Vector3(Px(MyLightDxPx), Px(MyLightDyPx), 0f);
                _myDeckLight.transform.localPosition = new Vector3(p.x, p.y, z);
            }
            if (_foeDeckLight != null)
            {
                var p = LayoutSpace.ToWorld(FoeDeckX01, FoeDeckY01)
                      + new Vector3(Px(FoeLightDxPx), Px(FoeLightDyPx), 0f);
                _foeDeckLight.transform.localPosition = new Vector3(p.x, p.y, z);
            }
        }

        /// <summary>把手牌数底板摆到**手牌标签的后面**（原版就是「文字居中压在板上」的关系）。
        /// ⚠️ 位置是**我们挑的** —— 原版那两个节点在 dump 里 `activeInHierarchy=False`、祖先链又算不出
        ///    绝对坐标（见 `HandPlatePx` 的注释）。所以不写死坐标，跟着标签走：标签换文案/换分辨率，
        ///    板也自己跟过去。`ReanchorHud` 换分辨率后要再调一次（它会把板拉回自己的锚点）。</summary>
        void PlaceHandPlate()
        {
            if (_handPlate == null || _handLabel == null) return;
            var p = _handLabel.transform.localPosition;      // 标签锚在**左下角**（anchor 0,0）
            var c = LayoutSpace.ToNormalized(new Vector3(p.x + _handLabel.WorldW * 0.5f,
                                                         p.y + _handLabel.WorldH * 0.5f, 0f));
            _handPlate.SetAnchorPosition(c.x, c.y);
        }

        /// <summary>牌堆的回合灯。原版是**两张图**（绿/红），不是同一张染色 —— 换贴图而不是换 `_Color`</summary>
        void SetDeckLight(ImageQuad q, bool lit)
        {
            if (q == null) return;
            var tex = CardArt.Ui(lit ? "40k_DeckHolder_light_green" : "40k_DeckHolder_light_red");
            if (tex != null && q.Texture != tex) q.SetTexture(tex);
        }

        // ---- 自检用：牌堆那几张图现在的实际尺寸/贴图（**截图看不出「尺寸对不对」**）----
        /// <summary>牌堆底板的世界高度（应 ≈ 230/108 = 2.13）</summary>
        public float DeckPlateWorldH { get { return _myDeckPlate != null ? _myDeckPlate.WorldH : 0f; } }
        /// <summary>牌堆底板的**渲染宽度** —— 自检用它钉住「原版 PA=1 内接 ⇒ 230×218.63」
        /// （🔴 2026-09-27 修：原来只给高 ⇒ 宽 241.96、**超出原版框 11.96px**；判据见 `ImageQuad.FitHeight`）。</summary>
        public float DeckPlateWorldW { get { return _myDeckPlate != null ? _myDeckPlate.WorldW : 0f; } }
        public float FoeDeckPlateWorldW { get { return _foeDeckPlate != null ? _foeDeckPlate.WorldW : 0f; } }
        public float FoeDeckPlateWorldH { get { return _foeDeckPlate != null ? _foeDeckPlate.WorldH : 0f; } }
        /// <summary>卡背的世界高度（应 ≈ 314/108 = 2.91）</summary>
        public float DeckCardWorldH { get { return _myPile != null ? _myPile.WorldH : 0f; } }

        /// <summary>🆕 2026-09-26：牌堆那层 **SDF**（自检用）。取不到掩码时为 null。</summary>
        public ImageQuad MyDeckSdfQuad { get { return _myDeckSdf; } }
        /// <summary>见 <see cref="MyDeckSdfQuad"/>。敌方那侧。</summary>
        public ImageQuad FoeDeckSdfQuad { get { return _foeDeckSdf; } }
        /// <summary>我方牌堆卡背那块（自检比「SDF 比卡背大多少 / 谁在前」用）。</summary>
        public ImageQuad MyPileQuad { get { return _myPile; } }
        /// <summary>我这边的回合灯现在是哪张图</summary>
        public string MyDeckLightTex
        {
            get { return (_myDeckLight != null && _myDeckLight.Texture != null) ? _myDeckLight.Texture.name : "<无>"; }
        }
        /// <summary>对手那边的回合灯现在是哪张图</summary>
        public string FoeDeckLightTex
        {
            get { return (_foeDeckLight != null && _foeDeckLight.Texture != null) ? _foeDeckLight.Texture.name : "<无>"; }
        }
        /// <summary>自检用：我方回合灯的实际位置（归一化）。原版绝对中心 **(1813.46, 985.2)** → (0.94451, 0.08778)
        /// —— 2026-09-13 之前我们摆的是 (1805.6, 1051)（漏了 `anchoredPosition`，偏低 66 px）</summary>
        public Vector2 MyDeckLightPos01
        {
            get { return _myDeckLight != null ? LayoutSpace.ToNormalized(_myDeckLight.transform.localPosition) : Vector2.zero; }
        }

        // ---- 自检用：牌库张数底板 / 本回合已出牌数 / 里程碑（这几个的毛病截图看不出来：
        //      半透明板画成实心、板没画、出牌灯该亮不亮，都「看着挺正常」）----
        /// <summary>牌库张数底板用的图（应 `40K_display`）</summary>
        public string DeckSizePlateTex
        {
            get { return (_myDeckSizePlate != null && _myDeckSizePlate.Texture != null) ? _myDeckSizePlate.Texture.name : "<无>"; }
        }
        /// <summary>牌库张数底板的世界高度（我 59.11/108 = 0.5473、敌 52.05/108 = 0.4819）</summary>
        public float MyDeckSizePlateWorldH { get { return _myDeckSizePlate != null ? _myDeckSizePlate.WorldH : 0f; } }
        public float FoeDeckSizePlateWorldH { get { return _foeDeckSizePlate != null ? _foeDeckSizePlate.WorldH : 0f; } }
        /// <summary>张数底板那张图的透明度（原版 0.6941177 —— 画成 1 就是一块实心白板）</summary>
        public float DeckSizePlateAlpha { get { return _myDeckSizePlate != null ? _myDeckSizePlate.Tint.a : 0f; } }
        /// <summary>张数文字的世界宽度 —— 要比底板窄才塞得下（原版文字是 TMP 自动缩字号塞进去的）</summary>
        public float PileLabelWorldW { get { return _pileLabel != null ? _pileLabel.WorldW : 0f; } }
        /// <summary>本回合我已经出了几张牌</summary>
        public int CardsPlayedThisTurn { get { return _cardsPlayedThisTurn; } }
        /// <summary>三枚「已出牌数」灯里有几枚是亮的</summary>
        public int PlayedPipsOn
        {
            get
            {
                int n = 0;
                if (_playedPips != null)
                    foreach (var p in _playedPips) if (p != null && p.gameObject.activeSelf) n++;
                return n;
            }
        }
        /// <summary>第 i 枚「已出牌数」灯用的是哪张图</summary>
        public string PlayedPipTex(int i)
        {
            return (_playedPips != null && i >= 0 && i < _playedPips.Length && _playedPips[i] != null
                    && _playedPips[i].Texture != null) ? _playedPips[i].Texture.name : "<无>";
        }
        /// <summary>里程碑骷髅用的图（应 `40k_battle_Win Skull`）</summary>
        public string SkullIconTex
        {
            get { return (_skullIcon != null && _skullIcon.Texture != null) ? _skullIcon.Texture.name : "<无>"; }
        }
        /// <summary>里程碑骷髅的中心（归一化；原版 (193.4, 956.6) 从上 → 0.10073 / 0.11426）</summary>
        public Vector2 SkullIconPos01
        {
            get { return _skullIcon != null ? LayoutSpace.ToNormalized(_skullIcon.transform.localPosition) : Vector2.zero; }
        }
        /// <summary>里程碑分数那行字（`x N`）</summary>
        public string SkullScoreText { get { return _skullScore != null ? _skullScore.Text : null; } }
        /// <summary>骷髅那张图的 z。**必须比 HUD 图的默认 z（`HudImageZ` = 0.3）更近** ——
        /// 它跟名牌几乎重叠，同 z 就会被名牌整个盖住（2026-09-13 第一版就是这样：`x0` 在、骷髅没了，
        /// 截图放大才看出来）。**断言钉住它**，别让人改回去。</summary>
        public float SkullIconZ { get { return _skullIcon != null ? _skullIcon.transform.localPosition.z : 999f; } }
        /// <summary>手牌数底板用的图（也应 `40K_display`）</summary>
        public string HandPlateTex
        {
            get { return (_handPlate != null && _handPlate.Texture != null) ? _handPlate.Texture.name : "<无>"; }
        }
        /// <summary>手牌数底板的世界高度（259.3/3.946 = 65.7 px → 0.6083）</summary>
        public float HandPlateWorldH { get { return _handPlate != null ? _handPlate.WorldH : 0f; } }
        /// <summary>手牌数底板的中心（归一化）—— 自检拿它验「整块板在屏幕内」</summary>
        public Vector2 HandPlatePos01
        {
            get { return _handPlate != null ? LayoutSpace.ToNormalized(_handPlate.transform.localPosition) : Vector2.zero; }
        }
        /// <summary>自检用：两块名牌的**左缘中点**（归一化）。原版是 −0.005938 / −0.005833
        /// （两边都贴着屏幕左缘、实绘左缘 −11.4 px）—— 2026-09-13 之前用的是另一份实例的坐标，偏右 44 / 168 px</summary>
        public Vector2 MyPlatePos01
        {
            get { return _myPlate != null ? LayoutSpace.ToNormalized(_myPlate.transform.localPosition) : Vector2.zero; }
        }
        public Vector2 EnemyPlatePos01
        {
            get { return _enemyPlate != null ? LayoutSpace.ToNormalized(_enemyPlate.transform.localPosition) : Vector2.zero; }
        }
        /// <summary>自检用：里程碑骷髅的**下沿**（归一化 y，从下往上算）</summary>
        public float SkullBottomY01 { get { return SkullIconY01 - (SkullIconPx * 0.5f) / 1080f; } }
        /// <summary>自检用：我方名牌那行字的**中心**（归一化 y）。
        /// 骷髅的下沿必须明显在它上面 —— 2026-09-13 用户点名「图标不该压住文字」，这就是那条判据
        /// （原版：骷髅下沿 983.7(从上) vs 文字中心 999.45 → 骷髅整个在文字中心线以上 15.8 px）</summary>
        public float MyPlateTextCenterY01
        {
            get { return _myText != null ? LayoutSpace.ToNormalized(_myText.transform.localPosition).y : 0f; }
        }

        // ---- 自检用：右侧能量区那几件（**截图看不出「贴图对不对/谁上谁下」**）----
        /// <summary>敌方能量水晶现在用的贴图（`40k_battle_energy_full` / `_empty`）</summary>
        public string FoeEnergyGemTex
        {
            get { return (_foeEnergyGem != null && _foeEnergyGem.Texture != null) ? _foeEnergyGem.Texture.name : "<无>"; }
        }
        /// <summary>自检读：回合时钟那行字（`1:00`；进了倒计时那段是纯秒数）</summary>
        public string ClockText { get { return _clockLabel != null ? _clockLabel.Text : null; } }
        /// <summary>自检读：还剩多少秒</summary>
        public float ClockLeft { get { return _clockLeft; } }
        /// <summary>自检读：是不是已经进了「超时后的倒计时」那一段</summary>
        public bool ClockCountingDown { get { return _clockInCountdown; } }
        /// <summary>自检用：把表按秒推（批处理下没有真实帧循环）</summary>
        public void TickClockForTest(float dt) { TickClock(dt); }

        /// <summary>🆕 2026-10-17（B17·A901）：自检用 —— 给这台驱动**摆一个已经开着的真局面**，
        /// 并把回合表拨回满（`ResetClock` = 原版 `ClockManager.StartTimer` 的等价物）。
        /// 为什么需要它：验「对手掉线时表停不走 / 回来之后接着走」要靠 `TickClockForTest` 推表，
        /// 而 `TickClock` 的头两道闸要 `Ctx != null &amp;&amp; !Ctx.IsOver &amp;&amp; Ctx.Active == _me`
        /// 且 `_clockLeft &gt; 0` —— 联机自检（`NetBattleTest`）**没有** `interaction` / 棋盘 / 相机，
        /// 走不了产品入口 `Begin(...)`（那一条会中途炸，先例见该文件 §A530 的 `NoteProbeThrow`）。
        /// ⚠️ **只给自检用**：生产路径一处都不调它（产品入口是 `Begin` / `BeginFromDeckLibrary` / `NetReplay`）。</summary>
        public void SetCtxForTest(BattleContext ctx) { Ctx = ctx; ResetClock(); }

        /// <summary>`hurry` 语音本回合说过没有（原版 `ClockManager` 的 `latch_0xb8`）——
        /// 自检靠它验「过 35 秒只播一次、跨回合复位」。</summary>
        public bool HurrySaidThisTurn { get { return _hurrySaidThisTurn; } }

        /// <summary>敌方能量数字（`2/2` 这种）—— 那颗水晶原来**根本没画**</summary>
        public string FoeEnergyText { get { return _foeEnergyLabel != null ? _foeEnergyLabel.Text : null; } }
        /// <summary>🆕 2026-09-27：**我方**能量数字 —— `FoeEnergyText` 的对称件。
        /// 联机客机视角那一段靠「两侧文字跟着 `_me` 换」来验 `UpdateHud` 认的是哪一方
        /// （`UpdateHud` 里 `me = Ctx.Players[_me]`）。</summary>
        public string MyEnergyText { get { return _energyLabel != null ? _energyLabel.Text : null; } }
        /// <summary>🆕 2026-09-27：我方牌堆/弃牌那行字 —— 给客机视角那段用。
        /// 🔴 **2026-10-18（W6）**：内容是**原版拼法** `&lt;词条&gt;: &lt;张数&gt;`（`Battle/HUD/CardsLeft`），
        /// 不再是自造的 `DECK n  DISC m`（**弃牌数已不在这两格上** —— 原版这两颗节点各只有一个数字）。</summary>
        public string MyPileText { get { return _pileLabel != null ? _pileLabel.Text : null; } }
        /// <summary>🆕 **2026-10-18（W6）**：我方手牌计数那行字（原版 `Battle/HUD/CardsInHand`，拼法同上）。</summary>
        public string MyHandText { get { return _handLabel != null ? _handLabel.Text : null; } }
        /// <summary>🆕 2026-09-27：敌方牌堆/弃牌那行字。</summary>
        public string FoePileText { get { return _foePileLabel != null ? _foePileLabel.Text : null; } }
        /// <summary>🆕 **2026-10-18（第十三轮 · G2b）**：`END TURN` 那颗钮上现在印的字。
        /// 权威 = 原版词条 `Battle/HUD/EndTurn`（`CardText.Phrase("END TURN")` → `Loc.T`）。
        /// ⚠️ 它**每次 `UpdateHud` 都重取**（换语言之后要变）⇒ 自检可以切语档后 `RefreshAll()` 再读它。</summary>
        public string EndTurnText { get { return _endTurnLabel != null ? _endTurnLabel.Text : null; } }
        /// <summary>🆕 **2026-10-18（第十三轮 · G2b）**：回合行那一整串（`第 N 回合   YOUR TURN` /
        /// `TURN N   YOUR TURN`）。两段各有权威：数字那段 = `CardText.TurnLabel`（我们自己的写法，
        /// 原版没有对应词条）、归属那段 = `CardText.Phrase("YOUR TURN"/"ENEMY TURN")`
        /// → 原版 `Battle/HUD/{YourTurn,EnemyTurn}`。</summary>
        public string TurnLineText { get { return _turnLabel != null ? _turnLabel.Text : null; } }
        /// <summary>🆕 2026-09-27：名牌那一行（`阵营  HP n`）—— 我方那份。</summary>
        public string MyPlateText { get { return _myText != null ? _myText.Text : null; } }
        /// <summary>🆕 2026-09-27：名牌那一行 —— 对面那份。</summary>
        public string FoePlateText { get { return _enemyText != null ? _enemyText.Text : null; } }
        /// <summary>水晶底下那块底板用的图（应为 `Card_Frame_Cost_Icon`）</summary>
        public string MyEnergyPlateTex
        {
            get { return (_myEnergyPlate != null && _myEnergyPlate.Texture != null) ? _myEnergyPlate.Texture.name : "<无>"; }
        }
        public string FoeEnergyPlateTex
        {
            get { return (_foeEnergyPlate != null && _foeEnergyPlate.Texture != null) ? _foeEnergyPlate.Texture.name : "<无>"; }
        }
        /// <summary>水晶在屏幕上的归一化位置（x01 / y01，y 从**下**算）</summary>
        public Vector2 MyEnergyPos01 { get { return PosOf(_energyGem); } }
        public Vector2 FoeEnergyPos01 { get { return PosOf(_foeEnergyGem); } }
        /// <summary>任务点那两张图的归一化位置 —— 我方应在水晶**下方**、敌方在**上方**</summary>
        public Vector2 QuestIconPos(bool mine)
        {
            // 🔴 2026-09-17：这里原来是 `transform.Find("PlayerQuestPoints" / "EnemyQuestPoints")`
            //    —— **按名字直查子节点**。HUD 现在统一挂在 `hudRoot` 下（为震镜头加的），
            //    而 `Transform.Find` **不递归** ⇒ 那条会**静默**返回 null、位置全变成 (-1,-1)。
            //    改成直接用建的时候就存下来的引用（同一批对象，而且不再依赖名字）。
            return PosOf(mine ? _myQuestIcon : _foeQuestIcon);
        }
        static Vector2 PosOf(Component c)
        {
            return c == null ? new Vector2(-1f, -1f) : LayoutSpace.ToNormalized(c.transform.localPosition);
        }

        // HUD 的每个字都是**世界空间**的一块 quad，位置在建立时算好就固定了 ——
        // 而 `LayoutSpace.VisibleWidth` 跟着宽高比走，所以切分辨率时必须重算，
        // 不然 4:3 建、16:9 拍的时候文字会整片偏到左边（批处理自检里踩过）。
        readonly List<Label> _hudLabels = new List<Label>();
        readonly List<Vector2> _hudSpots = new List<Vector2>();
        readonly List<ImageQuad> _hudImages = new List<ImageQuad>();
        readonly List<Vector2> _hudImageSpots = new List<Vector2>();
        /// <summary>能量座上那 6 枚常亮小光点（原版 `Energy And turn holder/Lights`）—— 自检要量它们的矩形。</summary>
        readonly List<ImageQuad> _energyLights = new List<ImageQuad>();
        float _hudWidth = -1f;

        /// <summary>那 6 枚光点（自检用；判据 = 原版那 6 个 RT 的绝对矩形）。</summary>
        public IReadOnlyList<ImageQuad> EnergyLights { get { return _energyLights; } }

        /// <summary>手牌数那一格的原版词条键（**只此一份**）。判据 → `UpdateHud` 里那段注释 + `Core/Loc.cs`。</summary>
        public const string HandCountTerm = "Battle/HUD/CardsInHand";
        /// <summary>牌库数那一格的原版词条键（**只此一份**）。</summary>
        public const string DeckCountTerm = "Battle/HUD/CardsLeft";

        /// <summary>原版那两颗计数节点的拼法：**`GetTermTranslation(词条) + ": " + 数字`**
        /// （分隔符 = `0x42C7DF8` 那个字面量 `": "`，两条 `.c` 方法体逐句见 `UpdateHud` 里那段注释）。
        /// 🔴 **只此一份** —— 手牌/我方牌库/敌方牌库三处都走它，⛔ 别在调用点各拼一遍。</summary>
        public static string CountText(string term, int n) { return Loc.T(term) + ": " + n; }

        /// <summary>HUD 上的一行字。
        ///
        /// <para>🔴 **2026-10-10（A1213①）：后 6 个形参是新增的「原版框 + 四格」—— 原来一个都没有。**
        /// 病灶（普查代理 `R6` §2·A / §4 现读）：我们这颗**从不写 `sizeDelta`**
        /// （`Label` 只在 `SetWrapWidth` / `SetAutoFitBox` 里写它，本助手两个都不调），
        /// 而原版**每一颗 HUD 文字都有真框、而且 `m_enableAutoSizing = 1`** ⇒
        /// 「接 autosize」这件事在原版那一侧是**有框可用**的，我们这一侧**连框都没有**。</para>
        ///
        /// <para>**为什么给它框不会挪动文字（本件最大的风险点，判据是结构性的）**：
        /// 框写在 **TMP 子节点的 `sizeDelta`** 上（`TmpFont.SetWrapWidthRect` = `sizeDelta = (w, 0)`，
        /// `SetAutoFitBox` 再补 `sizeDelta.y`），而**最终摆位**由 `Label.RefreshBounds()` 一锤定音：
        /// 它按 `textBounds` 反算 `_tmp.rectTransform.localPosition =
        /// (-anchor.x·W - b.min.x, -anchor.y·H - b.min.y + vOffset)`
        /// —— **框的 pivot / `anchoredPosition` / `sizeDelta` 从不进入这条算式**
        /// ⇒ 文字块的锚点**逐位停在 `Label` 节点的 `localPosition` 上**，与框多大、轴心在哪**无关**。
        /// 换句话说：**框天然是「以本节点为中心」的**（与 `MenuDraw.Text` 那条
        /// 「`Local(parent, 矩形)` = 矩形中心 − 父件位置」是同一个约定）。
        /// ⇒ **本件不改任何一个 `x01/y01`、不动 `anchor`、不新增/改动 `SetVAlign` 与 `Align*On`**。</para>
        ///
        /// <para>🔴 **会变的东西（如实登记，不是缺陷）**：**字号**。
        /// `SetAutoFitBox` 开的是**真自适应**（TMP 的 `m_enableAutoSizing`），它会
        /// **在 `[fontSizeMin, fontSizeMax]` 里收敛**（装得下就涨到 max、装不下就缩 ——
        /// `TextMeshPro.GenerateTextMesh()` 的两支：`#region Check Auto-Sizing (Upper Font Size Bounds)`
        /// 与 `#region Text Auto-Sizing (Text greater than vertical bounds)`）。
        /// ⇒ **字块尺寸会跟着变**，这正是接 autosize 的目的（原版那 17 颗的字号本来就是自适应出来的）。
        /// ⚠️ 但**收敛结果我们这一侧量不到**（本波不跑 Unity）⇒ 交给后面的断言波 + 真 Play。
        /// ⚠️ 副作用两处：① `Label.Contains`（END TURN 的命中区）读 `WorldW/WorldH`
        /// —— 字变大命中区就跟着变大（**原版也是这个模型**）；② 非 `(0.5,0.5)` 锚的那一颗
        /// （`MatchSkullsScore`，锚 `(0, 0.5)`）**左缘固定、右缘随字号动**。</para>
        ///
        /// <para>**逐颗判据** —— 🔴 **2026-10-10（`F4` 报、调度台裁）就地订正**：原文写「**本表 = 本件唯一的数字正本**」，
        /// 那句会造成一个真实的错：**自检若照它办（不另存期望值），就成了「拿实现证明实现」= 自证**（本仓明令禁止）。
        /// ⇒ 正确的分工：**数字正本 = 【原版 prefab 的字段原文】**（用下面那条 `menu_dump` 命令**现读**；
        /// 逐颗值另见 `资料/普查产出_第十一会话/PH_A1213Hud补框.md`）· **本表 = 实现侧的说明**（它是**转述**，
        /// 会漂、也可能抄错）；**自检则必须自己留一份期望值**（`Editor/BattleScene.cs` 的 `CheckHudBox` 就是那么做的）。
        /// ⛔ 在调用点别另抄一份**实现值**。</para>
        ///
        /// <para>全部**现读** `bundle_scenes_scenes_battlearena1`（13 场同构，字段逐值相同，只差节点 pid）：
        /// 四格 = 原版那颗 TMP 的 `m_fontSizeMin` / `m_fontSizeMax` / `m_fontSizeBase` / `m_TextWrappingMode`
        /// **原文**（与 `R6` §2·A 逐值吻合）；框 = 那颗 `RectTransform` 的**解算后屏幕矩形（1920×1080 画布 px）**。
        /// 原版那 13 场里，多数框是**拉伸锚点**（`anchorMin ≠ anchorMax`，`sizeDelta` 是内缩量）⇒
        /// **必须按父链解算**，⛔ 不能直读 `m_SizeDelta`。解算口径（uGUI 语义，逐级）：
        /// `rect.size(局域) = sizeDelta + anchorDiff ⊙ 父局域尺寸` · `父局域 = 父屏幕 ÷ lossyScale(父)` ·
        /// `屏幕尺寸 = 局域尺寸 × lossyScale(自己)`。命令（可复现）：
        /// `python 工具/menu_dump.py bundle_scenes_scenes_battlearena1 --rt &lt;父&gt; --depth 5 --no-sprite`，
        /// 或用本件留在 `资料/普查产出_第十一会话/PH_A1213Hud补框.md` 里的逐颗表。
        /// ⚠️ `MyPileLabel`/`FoePileLabel` 那两颗的**框高来自 `AspectRatioFitter`（宽控高 4.03465）**
        /// —— 序列化 `sizeDelta.y` 是 **0**，拿它当框高会把字号压到 min（`⚙ARF` 那几个数是要回写之后的）。</para>
        ///
        /// <para>⚠️ `SetAutoFitBox` **内部无条件把折行开成 `Normal`(1)**
        /// （它调 `SetWrapWidth`，而那个写死 `textWrappingMode = Normal`）⇒
        /// 原版那一档是 **`0`** 的四颗（两张名牌 + 两张称号）必须由 `wrapMode` 显式还原成原版那一档。
        /// 本件 17 颗里没有 `3`（`PreserveWhitespaceNoWrap`）那一档，所以走 `SetWrappingMode(int)` 的 0/1 就够。</para>
        ///
        /// <para>🔴 **17 颗里【16 颗】在本件落地、【1 颗】故意没做**：`HandLabel`
        /// （原版 `CardsInHandText`）—— 它那颗的四格是**「缩放假」里的值**（父链是纯 `Transform`、`HandArea`
        /// 带 `m_LocalScale = 108`），照同一套口径会静默把字号压到 3 px；而换算要的单位桥本仓没有、
        /// 两条候选读法互相打架 ⇒ 按红线「不许自己发明口径」**只报不动**。
        /// 判据全文在那个调用点的注释 + `资料/普查产出_第十一会话/PH_A1213Hud补框.md` §4。</para>
        /// </summary>
        /// <param name="boxWpx">原版那颗 TMP 的**框宽**（画布 px；`0` = 不给框、行为与加这些参数之前逐位相同）。</param>
        /// <param name="boxHpx">原版那颗 TMP 的**框高**（画布 px；同上）。</param>
        /// <param name="autoMinPx">原版 `m_fontSizeMin` **原文**。</param>
        /// <param name="autoMaxPx">原版 `m_fontSizeMax` **原文**。</param>
        /// <param name="autoBasePx">原版 `m_fontSizeBase` **原文**（自适应二分的起点）。</param>
        /// <param name="wrapMode">原版 `m_TextWrappingMode` **原文**（`0`/`1`；本族用不到 `3`）。</param>
        Label Hud(Transform root, string text, float x01, float y01, int scale,
                  Color c, Vector2 anchor, string name,
                  float boxWpx = 0f, float boxHpx = 0f,
                  float autoMinPx = 0f, float autoMaxPx = 0f, float autoBasePx = 0f,
                  int wrapMode = 1)
        {
            var l = Label.Create(root, text, LayoutSpace.ToWorld(x01, y01), scale, c, anchor, name);
            if (l != null && boxWpx > 0f && boxHpx > 0f && autoMaxPx > 0f)
            {
                // 框 + 四格。⚠️ `SetAutoFitBox` 只在**已激活**的对象上量得出尺寸（CLAUDE.md §三那条坑）——
                //    HUD 这一族建的时候是活的（`HudRoot` 就在 `transform` 下），所以这里安全。
                l.SetAutoFitBox(Px(boxWpx), Px(boxHpx), autoMinPx, autoMaxPx, autoBasePx);
                // ⛔ 上面那一句顺带把折行开成 `Normal`(1) ⇒ 原版是 `0` 的必须还原（见本方法 doc 末尾那条）。
                //    值没变时 `SetWrappingMode` 自己早退（不白重排一次），所以无条件调是安全的。
                l.SetWrappingMode(wrapMode);
            }
            _hudLabels.Add(l);
            _hudSpots.Add(new Vector2(x01, y01));
            return l;
        }

        /// <summary>HUD 上的一张图。**没有那张图就返回 null**（删掉美术目录也能跑）</summary>
        ImageQuad HudImage(Transform root, string artName, float x01, float y01,
                           Vector2 anchor, float worldHeight, string name)
            => HudImageTex(root, CardArt.Ui(artName), x01, y01, anchor, worldHeight, name);

        /// <summary>HUD 图统一往后放这么多（相机看 +Z，z 越大越远）—— 文字在 z=0，图在后面</summary>
        const float HudImageZ = 0.3f;

        /// <summary>**装饰性**底板再往后一层。⚠️ HUD 图原本全在同一个 z，而它们是同一个透明队列、
        /// 距离也一样 —— 谁压谁由渲染顺序决定，**不确定**。右侧能量区那张大底板
        /// （`UI_Energy_Holder_big`，一张几乎铺满那一片的金属板）就是这么把能量水晶压住的
        /// （2026-09-12 截图抓到：水晶只剩一块灰板）。要压在谁底下就给它更大的 z。</summary>
        const float HudDecorZ = 0.6f;

        /// <summary>称号那行字的**标称**字号（原版序列化 `m_fontSize` = **30.55 画布像素**；见 `MonoBehaviour_3797.json`）。
        /// 🔴 **2026-10-10（`F4` · 铁律 5）就地订正**：原文写「称号那行字的字号」—— **只对了一半**：
        /// 它只是 `SetGlyphHeight` 的**标称**（原版字段原文），**不是**那一格**渲染出来的**字号 ——
        /// 那一颗开着自适应，真正写进 TMP 的是 `cur × 原版字段 / nomPx`（本仓 `nomPx = 27.60px`），
        /// 再由 `Clamp(base, min, max)` 定，**并且 `m_characterCount == 0` 时根本不缩**（单机那一格恒空）。
        /// ⚠️ 别拿它去喂 `Label.SetFontSize`（那会大 2.7 倍）—— 走 `SetGlyphHeight(px/108)`。</summary>
        public const float TitleFontPx = 30.55f;

        /// <summary>🆕 **2026-10-19（B2）**：牌堆那张卡背 = 原版 **两段式**的忠实还原。
        ///
        /// <para>原版那个节点 `2DCard/Cardback Container/Cardback` 自己的 rect = **2.1739×3.1364**
        /// （`@100` = **217.39×313.64 px** ⇒ 本类的 `DeckCardPx × CardbackRectAspect` / `DeckCardPx`），
        /// 且带 **`m_PreserveAspect = 0`**（实读）—— 而**它自己的比例与那张 sprite 的 `m_Rect` 比例
        /// 到 5 位有效数字重合**（`0.693120` vs `0.693137`）⇒ 「定框」那一半是**空操作**，
        /// 原版画的是**等比缩小的整张卡背**：画心 = 节点 × (`textureRect` / `m_Rect`)，再按 `padding` 偏。</para>
        ///
        /// <para>⛔ **不是「拿 PNG 铺满节点」** —— 那是一次**拉伸**（最坏 `Cardback_All_Early Backer`
        /// 宽 **+16.1%** / 高 **+10.0%**；实况用的 `UM_Warlord_Tigurius` 高 +7.5%）。
        /// 算式与判据全文 → `Core/CardbackFace.cs`（本仓唯一一份）。</para>
        ///
        /// <para>🔴 **幂等**：每次先 `SetAnchorPosition(x01,y01)`（= 回到基准，⛔ **不要在现位置上累加** ——
        /// `ReanchorHud` 每切一次分辨率就会调一次本函数，累加会一次偏一点、静默）。
        /// 取不到卡背图 / 该图没登记 ⇒ `Fit` 返回 false ⇒ **不改几何**（缺图 `CardArt.CardBack` 那边已出声）。</para></summary>
        void FitDeckPile(ImageQuad q, Texture2D tex, float x01, float y01)
        {
            if (q == null) return;
            float w, h, dx, dyUp;
            if (!CardbackFace.Fit(tex, DeckCardPx * CardbackRectAspect, DeckCardPx,
                                  out w, out h, out dx, out dyUp))
                return;
            q.SetAnchorPosition(x01, y01);                      // ① 先摆回**基准**（幂等；保留 z）
            q.SetWorldHeight(Px(h));                            // ② 画心高（= 节点高 × texRectH/1020）
            q.SetAspect(w / h);                                 // ③ 比例（等比 —— 两轴系数相等）
            // ④ 偏移：画心**不居中**（按 `padding` 偏）。`SetAnchorPosition` 给的 y 向上 ⇒ `dyUp` 直接加。
            q.transform.localPosition += new Vector3(Px(dx), Px(dyUp), 0f);
        }

        /// <summary>🆕 2026-09-26：牌堆那层 **SDF**（原版 `Cardback Shadow SDF`）。
        /// 🔴 **它不能用 `HudImageTex` 的默认材质** —— 那个是 `Sprites/Default`（普通贴图），
        ///    而这一层要的是**原版 SDF shader**（`Everguild/FX/Card Highlight And Shadow`，
        ///    见 `CardView.CardbackSdfMaterialBase`）。所以建完要换成自己的材质。
        /// 取不到掩码（该阵营没有默认卡背 / 图没导）或取不到 shader ⇒ 返回 **null**（那层不画，牌堆本体照旧）。</summary>
        ImageQuad MakeDeckSdf(Transform root, string faction, float x01, float y01, string name)
        {
            var tex = CardArt.CardBackSdf(faction);
            if (tex == null) return null;
            var baseMat = CardView.CardbackSdfMaterialBase();
            if (baseMat == null) return null;          // 那边已经报过警告（不静默）
            var q = HudImageTex(root, tex, x01, y01, new Vector2(0.5f, 0.5f), Px(DeckSdfPx),
                                name, HudImageZ + 0.01f);
            if (q == null) return null;
            // 🔴 **2026-10-19（B2）**：见 `DeckSdfRectAspect` 的注释。**这一层保持原样**是对的 ——
            //   `_SDF` 那批 233/233 的 `m_Rect` == `textureRect`（`padding` 只差图集取整的 0.5px）
            //   ⇒ 原版「第二段」在本层是**空操作**，严格套公式反而会把那 0.5px 的量化噪声画出来
            //   （横挪 1.7px、尺寸差 0.34%）。理由全文 → `Core/CardbackFace.cs` 文件头「哪些贴图走这条」。
            q.SetAspect(DeckSdfRectAspect);            // 节点自己的比例（== 该 sprite 的 `m_Rect` 比例）
            var m = new Material(baseMat);             // ⚠️ 每层一份：共享会让敌我两边抢同一张贴图
            m.mainTexture = tex;
            q.SetMaterial(m);
            return q;
        }

        ImageQuad HudImageTex(Transform root, Texture2D tex, float x01, float y01,
                              Vector2 anchor, float worldHeight, string name, float z = HudImageZ)
        {
            var q = ImageQuad.Create(root, tex, LayoutSpace.ToWorld(x01, y01),
                                     worldHeight, anchor, name);
            if (q != null)
            {
                // ⚠️ 往后放一点（相机看 +Z，z 越大越远）：HUD 文字在 z=0，
                //    同 z 的话谁压谁看渲染顺序，实测按钮底图会把文字盖住（踩过）
                q.transform.localPosition += new Vector3(0f, 0f, z);
                _hudImages.Add(q);
                _hudImageSpots.Add(new Vector2(x01, y01));
            }
            return q;
        }

        /// <summary>🆕 2026-09-29：**能量座上那 6 枚小光点**（原版 `Right Anchor/Energy And turn holder/Lights`）。
        ///
        /// **它们是常亮静态装饰**（**不是**能量刻度 —— 能量数字在 `PlayerMana/ManaText`，能量「格」的视觉是
        /// `Energy Player` + `Energy Player Accumulation ON/OFF`）。判定依据：**全量搜不到任何驱动** ——
        /// 场景里对这 6 个 MB/RT 的精确 PathID 引用 0 命中 · `stringliteral.json` 0 · 反编译字符串 0 ·
        /// `Lights` 无 Animator 且 `BattleHud` 的 8 个 clip 不含它 · 13 张实拍 dump 全是 `activeSelf=true`。
        /// （要证伪只能进原版实机加 color/alpha 探针 ⇒ 已记进 `真Play待验清单`。）
        ///
        /// **逐枚矩形（绝对屏幕 px · y 向下）** —— 出处 `bundle_scenes_scenes_battlearena1/RectTransform/`
        /// 的 `RectTransform_{3404(Lights),3018,3026,2718,2638,3381,3085}.json`，**13 个战场包逐场核过、完全同构**：
        /// `Lights` 父框 (1798.05, 324.68) 70.68×71.32（它自己的 `m_SizeDelta` 是 (0,0) —— 拉伸占位，
        /// 真尺寸由父框 238.79×356.58 × 锚区比 0.296/0.2 推出来）。
        /// 六枚**同一张图** `Glow UI W40K`（123×123），**靠 `Image.color` 区分**。
        /// ⚠️ 图在 `Resources/Art/ui_menu/`（**不在**战斗那批 `ui/`）⇒ 走 `CardArt.MenuUi` 那三档兜底。
        /// ⚠️ 原版这些 Image 是 `m_Type=0` **PA=0** ⇒ **拉满各自那个小矩形**（不按 123×123 的比例）。
        /// 判据全文 → `资料/待办判据_战场与战斗视图.md` §8b。</summary>
        void BuildEnergyLights(Transform root)
        {
            var tex = CardArt.MenuUi("Glow_UI_W40K");
            if (tex == null)
            {
                Debug.LogWarning("[Battle] `Glow_UI_W40K` 取不到（找过 `Resources/Art/ui_menu|ui_deck|ui/`）"
                               + "⇒ 能量座上那 6 枚光点**不建**（不摆白方块）—— 见 `BuildEnergyLights`");
                return;
            }
            AddEnergyLight(root, tex, "Glow Green Eye", 1839.19f, 370.51f, 19.32f, 18.78f, new Color(0.736f, 1f, 0f, 1f));
            AddEnergyLight(root, tex, "Glow Green 2",   1803.22f, 346.63f, 12.77f, 12.65f, new Color(0.592f, 1f, 0f, 1f));
            AddEnergyLight(root, tex, "Glow Orange 1",  1829.97f, 346.82f, 10.00f,  9.30f, new Color(1f, 0.767f, 0f, 1f));
            AddEnergyLight(root, tex, "Glow Orange 2",  1840.15f, 353.45f,  8.73f,  8.17f, new Color(1f, 0.767f, 0f, 1f));
            AddEnergyLight(root, tex, "Glow Orange 3",  1832.02f, 361.72f,  8.24f,  6.55f, new Color(1f, 0.767f, 0f, 1f));
            AddEnergyLight(root, tex, "Glow Orange 4",  1855.89f, 359.85f, 10.10f,  8.83f, new Color(1f, 0.767f, 0f, 1f));
        }

        /// <summary>一枚光点：`x/y` = **左上角**（px，y 向下），`w/h` = 尺寸（px）。</summary>
        void AddEnergyLight(Transform root, Texture2D tex, string name,
                            float x, float y, float w, float h, Color c)
        {
            float cx = x + w * 0.5f, cy = y + h * 0.5f;
            var q = HudImageTex(root, tex, cx / 1920f, 1f - cy / 1080f, new Vector2(0.5f, 0.5f), h / 108f, name);
            if (q == null) return;
            q.SetAspect(w / h);        // 原版 PA=0 ⇒ 拉满那个矩形
            q.SetTint(c);              // 原版逐枚的 `Image.color`（同一张图靠颜色区分）
            _energyLights.Add(q);
        }

        void ReanchorHud()
        {
            if (Mathf.Approximately(_hudWidth, LayoutSpace.VisibleWidth)) return;
            _hudWidth = LayoutSpace.VisibleWidth;
            for (int i = 0; i < _hudLabels.Count; i++)
                if (_hudLabels[i] != null)
                    _hudLabels[i].transform.localPosition =
                        LayoutSpace.ToWorld(_hudSpots[i].x, _hudSpots[i].y);
            for (int i = 0; i < _hudImages.Count; i++)
                if (_hudImages[i] != null)
                    _hudImages[i].SetAnchorPosition(_hudImageSpots[i].x, _hudImageSpots[i].y);

            // 🆕 **2026-10-19（B2）**：牌堆卡背的**画心也不在锚点上**（尺寸按 `texRect/m_Rect` 缩、
            //   位置按 `padding` 偏，见 `FitDeckPile`）—— 上面那一轮 `SetAnchorPosition` 把它拉回了锚点 ⇒
            //   这里必须重来一次（**同一个理由**，就在下面 `PlaceDeckLights` 那两行旁边）。
            //   ⚠️ `FitDeckPile` 自身幂等（先回基准再加偏移）⇒ 重复调用不会累积偏移。
            FitDeckPile(_myPile, CardArt.CardBack(_myFaction), MyDeckX01, MyDeckY01);
            FitDeckPile(_foePile, CardArt.CardBack(_foeFaction), FoeDeckX01, FoeDeckY01);

            // 牌堆的回合灯不是贴在锚点上的（要偏到牌堆底板的右下角），上面那一轮会把它拉回中心
            PlaceDeckLights();
            // 手牌数底板同理（它是跟着手牌标签的中心摆的）
            PlaceHandPlate();

            // 格位底片和槽带同理（它们也是用 VisibleWidth 算的）
            if (playerBoard != null) playerBoard.EnsureMarkers();
            if (enemyBoard != null) enemyBoard.EnsureMarkers();
            if (backdrop != null) backdrop.Refresh();
            // 攻击方式选择器同理 —— 它的按钮位置也按 VisibleWidth/Scale 算
            if (selector != null) selector.RefreshLayout();
            // 技能卡面板取的是**屏幕 30%×30% 的 anchor**，宽高比跟着屏幕走 —— 同理
            if (skillPanel != null) skillPanel.RefreshLayout();
            // 回放条同理（它按 1920×1080 的绝对矩形定，要跟着 VisibleWidth 重算）
            if (_replayBar != null) _replayBar.RefreshLayout();
            // 语音条同理
            if (_unitChat != null) _unitChat.RefreshLayout();
        }

        void UpdateHud()
        {
            if (_turnLabel == null || Ctx == null) return;
            ReanchorHud();
            // 🆕 2026-10-18（A940）：教程那五个 `hide*` 在这里落（每帧算一遍 —— 那些元素也被本函数每帧写）。
            // ⚠️ 非教程局逐条还原成显示 ⇒ 普通对局与加这一句之前**逐字等价**。
            ApplyTutorialVisibility();

            var me = Ctx.Players[_me];
            var foe = Ctx.Players[1 - _me];

            string who = Ctx.IsOver ? "GAME OVER"
                       : (Ctx.Active == _me ? "YOUR TURN" : "ENEMY TURN");
            // 换牌阶段**不显示回合行** —— 对局还没开始，写「第 0 回合 你的回合」是误导
            //（原版这一阶段显示的是 `MulliganText/TurnText` 那一行，文案在 I2 里、本地没有）
            bool showTurn = !InMulligan;
            if (_turnLabel.gameObject.activeSelf != showTurn) _turnLabel.gameObject.SetActive(showTurn);
            if (showTurn) _turnLabel.SetText(CardText.TurnLabel(Ctx.Turn) + "   " + CardText.Phrase(who));

            // ---- 等待提示（原版 `WaitText`）----
            // 🔴 **原版的触发时机查不到**（反编译与场景 JSON 都没有）⇒ 我们接的是
            //    「**不是我的回合、且不在换牌/结算**」，也就是对手思考的那段时间。
            //    `SetVisible` 自己会去重（这个函数每帧跑）。
            if (_waitBanner != null)
                _waitBanner.SetVisible(!InMulligan && !Ctx.IsOver && Ctx.Active != _me);

            _energyLabel.SetText($"{me.Energy}/{me.MaxEnergy}");
            // 🔴 **2026-10-18（第十二轮 · W6）改文案（复刻偏差）**：原版这两格**不是** `HAND n` / `DECK n DISC m`。
            //    原版的拼法（**唯一判据 = 反编译方法体 + 二进制字面量**）：
            //      · `PlayerHand.ShowHandSize`（`d:/2/tools/decomp_full/PlayerHand__ShowHandSize.c`）=
            //        `GetTermTranslation(<0x4288398>)` + `": "` + 手牌数；
            //      · `DeckManager.DisplayDeckSize`（同目录 `.c`）= `GetTermTranslation(<0x4288490>)` + `": "` + 牌库数。
            //    两条 `_DAT_` 常量在 `d:/2/tools/il2cpp_out/stringliteral.json` 里逐条读出：
            //      `0x4288398` = **`Battle/HUD/CardsInHand`** · `0x4288490` = **`Battle/HUD/CardsLeft`**
            //      （分隔符 `_DAT_1842c7df8` = `0x42C7DF8` = **`": "`**）。
            //    ⇒ 那两颗节点 prefab 上**一颗 `Localize` 都没有**（原版走代码字面量）⇒
            //      **「按 `mTerm` 扫」对它们是无效否定**（`资料/已知的坑.md` #20 的第二/第三种载体），
            //      当年「0 词条」那条结论就是这么来的（**本批推翻**）。
            //    ⚠️ **`DISC` 那一段同时去掉**：原版这两颗节点各只有**一个**数字（没有弃牌计数），
            //      我们那三段式是自造的。弃牌数在别处仍有（战争日志/牌堆视图），这一格按原版收窄。
            _handLabel.SetText(CountText(HandCountTerm, me.Hand.Count));
            // ⚠️ 教程里 `hideCardsLeftInDeck` 会把这一族的对象**整个 `SetActive(false)`**；
            //    而 `PlaceHandPlate` 要量标签的尺寸 —— **没激活的 TMP 量不出尺寸**（本工程记过的坑）
            //    ⇒ 藏起来的时候不摆它（重新显示时本函数会再摆一次，不缺帧）。
            if (_handLabel.gameObject.activeSelf) PlaceHandPlate();   // 底板跟着标签走（原版：文字居中压在板上）
            // 🔴 **2026-09-28 用户拍板：名牌那格印【名字】** —— 原版那个节点就叫 `EnemyNameText`
            //    （`BattleDriver.cs` 里 `EnemyNameText` 的实读注 的出处），我们原来印「阵营 + HP n」是因为**没有名字数据源**。
            //    现在：我方 = `ProfileData.PlayerName`（默认「玩家123」，档案窗可改）；
            //         敌方 = 联机局的对端名（`NetMatchmaking.FoeName`），**单机局留空不编**（同 `EnemyName` 的口径）。
            //    ⚠️ **名字不随座位变**（我就是我、对手就是对手）⇒ 它不再能判「翻座位」，
            //       `BattleScene` 那两条断言已改成别的判据（座位方向本来就有能量/牌堆两条更硬的）。
            _myText.SetText(ProfileData.PlayerName + "   " + CardText.Faction(_myFaction));
            _enemyText.SetText((_net != null && !string.IsNullOrEmpty(NetMatchmaking.FoeName)
                                ? NetMatchmaking.FoeName + "   " : "") + CardText.Faction(_foeFaction));
            // 记「降到过的最低生命」—— 两边各记一份（我方那份只给对局历史用，见字段注释）
            if (foe.Warlord.Health < _foeWarlordMinHp) _foeWarlordMinHp = foe.Warlord.Health;
            if (me.Warlord.Health < _myWarlordMinHp) _myWarlordMinHp = me.Warlord.Health;
            // ---- 名牌上的里程碑 `MatchSkulls Score` = `x N`（原版 `BattleScoreUiManager.UpdateMilestonesCount`）----
            // 🔴 **2026-10-06（A147）改口径**：从「拿最低生命反推」改成**原版那套「事件驱动 + 已达成不回退」**。
            //   逐条判据（`d:/2/tools/decomp_full/`，第一权威）写在 `_foeSkullCount` 那个字段的注释上。
            //   一句话：**开局必是 `x0`**（原版 `Initialize` 调 `UpdateMilestonesCount(0xffffffff)`），
            //   之后**只在「敌方督军生命发生变化」时**才可能涨，且只增不减。
            //   ⚠️ **就地纠正本行原来的两句注释**（都已不成立）：
            //     ① 「原版那个方法体被剥空了（只有字段）」—— 那是当时查的**签名桩**（`Warpforge_code/` 里
            //        方法体本来就是空的）；全量反编译里 `BattleScoreUiManager__UpdateMilestonesCount.c`
            //        / `BattleScoreManager__CheckThresholds.c` / `__Initialize.c` **都在**。
            //     ② 「x3 是已达成数还是总数在原版数据里证不出来」—— **证得出来**：数 = **已达成档数**
            //        （`GetSkullCount` 数的是 `AlreadyAccomplished`），文案 = `"x" + (index+1)`。
            int foeHpNow = foe.Warlord.Health;
            if (_foeSkullHpSeen == int.MinValue)
                _foeSkullHpSeen = foeHpNow;              // 播种（原版 `Initialize` 用当前生命填缓存）⇒**不算变化**
            else if (foeHpNow != _foeSkullHpSeen)        // 原版只在「生命变了」这条信号里检查（`CheckHealth` 比缓存值）
            {
                _foeSkullHpSeen = foeHpNow;
                int reached = DeckRules.SkullsFor(foeHpNow);              // 该生命下**已达成**的档数
                if (reached > _foeSkullCount) _foeSkullCount = reached;   // 已达成不回退（只增不减）
            }
            if (_skullScore != null) _skullScore.SetText("x" + _foeSkullCount);
            // 本回合已出牌数：出几张亮几枚（原版只有三枚节点，第四张不显示）
            if (_playedPips != null)
                for (int i = 0; i < _playedPips.Length; i++)
                    if (_playedPips[i] != null) _playedPips[i].gameObject.SetActive(i < _cardsPlayedThisTurn);
            _pileLabel.SetText(CountText(DeckCountTerm, me.Deck.Count));
            _foePileLabel.SetText(CountText(DeckCountTerm, foe.Deck.Count));

            // 能量宝石：有能量亮、没能量灭（原版两张图）
            bool hasEnergy = me.Energy > 0 && Ctx.Active == _me && !Ctx.IsOver;
            if (_energyGem != null) _energyGem.gameObject.SetActive(hasEnergy);
            if (_energyGemEmpty != null) _energyGemEmpty.gameObject.SetActive(!hasEnergy);
            // 敌方那颗同理（判据同一份，只是主语换成对手）
            bool foeHasEnergy = foe.Energy > 0 && Ctx.Active != _me && !Ctx.IsOver;
            if (_foeEnergyGem != null) _foeEnergyGem.gameObject.SetActive(foeHasEnergy);
            if (_foeEnergyGemEmpty != null) _foeEnergyGemEmpty.gameObject.SetActive(!foeHasEnergy);
            if (_foeEnergyLabel != null) _foeEnergyLabel.SetText($"{foe.Energy}/{foe.MaxEnergy}");

            // ---- 阵营资源：**按阵营**显示（判据只一处：`ShowsSpiritStone` / `ShowsFaith`）----
            // 物件是**照建**的，这里只切显隐 —— 和原版 `ManaTypeHolder.Toggle` 同一个做法。
            // 🆕 2026-09-18：判据从「有值就显示」换成**按阵营**（原版 `RawCardScript.Uses*` 就是
            //    `督军卡 + 0x2c` 跟 30/80/110 比）⇒ **0 值也照样显示 `0`**，与原版一致。
            //    出处见 `ShowsSpiritStone` 的注释（含三条 7 行的方法体）。
            bool myFaith = ShowsFaith(_myFaction);
            bool foeFaith = ShowsFaith(_foeFaction);
            bool myStone = ShowsSpiritStone(_myFaction);
            bool foeStone = ShowsSpiritStone(_foeFaction);
            SetFactionResourceVisible(myFaith, foeFaith, myStone, foeStone);
            // ⚠️ 每处都判 null：**美术没同步进来时 `CardArt.Ui` 返回 null**（删掉美术目录也能跑，
            //    这是本工程一贯的约定），不判的话开一局就 NPE。
            if (myFaith && _myFaithText != null) _myFaithText.SetText(me.Faith.ToString());
            if (foeFaith && _foeFaithText != null) _foeFaithText.SetText(foe.Faith.ToString());
            if (myStone && _myStoneText != null) _myStoneText.SetText(me.SpiritStones.ToString());
            if (foeStone && _foeStoneText != null) _foeStoneText.SetText(foe.SpiritStones.ToString());

            // 任务点数字：真值 `X/3`（2026-09-13 第三十三轮起；原来是写死的 "0/3"）
            if (_qpTextMe != null) _qpTextMe.SetText($"{me.QuestPoints}/3");
            if (_qpTextFoe != null) _qpTextFoe.SetText($"{foe.QuestPoints}/3");

            bool myTurn = Ctx.Active == _me && !Ctx.IsOver;
            // 🔴 **2026-10-18（第十三轮 · G2b）：字**每次刷 HUD 时都重取一遍。
            //   为什么必须在这儿（而不是只在 `BuildHud` 里写一次）：这颗钮的文案**走词条**
            //   （`CardText.Phrase("END TURN")` → `Battle/HUD/EndTurn`，见 `Core/Loc.cs`），
            //   而**换语言**之后已画出来的字不会自己变 ⇒ 只写一次的话，英文档下这颗钮**仍是中文**
            //   （`RefreshAll` → `UpdateHud` 是换语言后那条重画链，自检也走它）。
            //   ⚠️ `Label.SetText` 对**同一串**是早退（`Label.SetText`）⇒ 每帧调不产生任何重排/重建。
            //   📌 原版也是**每次回合切换时重设**（`ClockManager.SetEndTurnText` 自己就调
            //      `GetTranslation`），不是出厂写死 —— 行为同族。
            _endTurnLabel.SetText(CardText.Phrase("END TURN"));
            _endTurnLabel.SetColor(myTurn ? new Color(1f, 0.85f, 0.35f) : new Color(0.35f, 0.35f, 0.40f));
            if (_endTurnBg != null)
                _endTurnBg.SetTint(myTurn ? Color.white : new Color(0.42f, 0.44f, 0.50f));

            // 牌堆的回合灯：轮到自己亮绿、否则红（原版两张图 `40k_DeckHolder_light_green/_red`）
            SetDeckLight(_myDeckLight, myTurn);
            SetDeckLight(_foeDeckLight, Ctx.Active != _me && !Ctx.IsOver);

            if (Ctx.IsOver)
            {
                string r = Ctx.Winner == 3 ? "DRAW" : (Ctx.Winner == _me + 1 ? "YOU WIN" : "YOU LOSE");
                // 结算面板接管这块文字（面板自己有标题）—— 留着的话中心会和面板标题撞成两处
                _resultLabel.SetText(_endPanel == null ? CardText.Phrase(r) : "");
                // 结算：这里要**真的清空**（不走 `SetHint`）—— 落回「开局那句牌组说明」的话，
                // 它会从结算面板底下透出来（提示行 z=3，结算面板盖在中间）
                if (_hintLabel != null) _hintLabel.SetText("");
                // 🔴🔴 **2026-10-12：这一块的【闩】与【闸】都换掉了**（原来那一行是
                //   `if (_endPanel != null && !_endPanel.Visible)`）：
                //   · **A382（闩）**：原来整块（日常推进 / 对局记录 / 录像收尾 / 结算面板）**全挂在
                //     `_endPanel != null` 上** ⇒ 面板拿不到时**静默全跳过**（不记账、不报错、什么都不做）。
                //     现在：**账与面板分家** —— 闩 = 显式字段 `_settled`（`Begin()` 清零 ⇒ 一局一张账），
                //     面板真的取不到时**出声**（下面那句 `LogError`），账照记。
                //     （原来的 `!_endPanel.Visible` 本来也不是闩 —— 它靠的是「新一局一开局，
                //       `UpdateHud` 的 `else` 支会把面板 `Hide()`」这个副作用。）
                //   · **A381（闸）**：回放局（`_replaySession`）**整块不做**。判据 = 原版
                //     `ChallengeLogMgr.LogMatchEnd` **只在真打完时叫**（`资料/普查产出_1011/R1_每日骷髅与登录卡.md` §二），
                //     而回放不是「真打完」。不设这道闸时：放一局录像会**再写一条对局记录 + 再弹结算面板与
                //     开门视频 + 再推三条每日任务 + 再加一次骷髅**（`PlayReplay` 只关掉了「再录一份」，
                //     没关「再结一次账」——`Ctx.IsOver` 在那一趟灌完动作之后就是 true）。
                if (!_settled)
                {
                    _settled = true;      // 本局的账只做一次（`Begin()` 清零）
                    if (_replaySession)
                    {
                        // ⛔ **不静默**（红线）：这条出口是**故意**的，说出来
                        Debug.Log("[Replay] 这一局的结算**整块跳过**（回放不是「真打完」）："
                                + "不记对局记录 / 不推日常与骷髅 / 不收尾录像 / 不弹结算面板与开门视频");
                    }
                    else
                    {
                        _settleCount++;
                        // 🆕 2026-10-18（A939）：**战后脚本**（原版 `BattleFinished:80-112` 那一支）。
                        // ⚠️ 回放局**不在此列**（上面 `_replaySession` 那一支已经整块跳过）——
                        //    回放不是「真打完」，原版那条链也不会在回放里跑。
                        // ⚠️ 门恒真才起（`PostBattleScriptAvailable`：教程关 + `scriptedTurn` + actions 非空）；
                        //    非教程局 / 6 关之外**一次都不会起**。⚠️ 它**不阻塞**结算面板（边界见上面那段注释）。
                        StartPostBattleScript();
                        if (_endPanel == null)
                        {
                            // ⛔ **不许静默失败**：面板没了要说出来，但**账照记**（下面三条）——
                            //   原来是「面板为 null ⇒ 连账都不记」，一声不响（A382）。
                            Debug.LogError("[Battle] 结算面板拿不到（`_endPanel` 为 null）⇒ **面板与开门视频"
                                         + "这次不会出现**；日常推进 / 对局记录 / 录像收尾**照常做**"
                                         + "（这一块从此不再依赖面板存在，见 A382）");
                        }
                        else
                        {
                            // 🔴 **2026-10-06（A148）**：第 3 个实参从「敌方督军最低生命」换成**已达成档数**
                            //   （= 原版 `BattleScoreManager.GetSkullCount()`，与 HUD 那个 `x N` 同源、同一格字段）。
                            //   最低生命照旧传进去 —— 它现在**只**喂面板副标题那行字（原版那行字我们没查到出处，
                            //   是我们自加的说明，见 `EndPanel.Show` 的 `<param>`）。
                            _endPanel.Show(Ctx.Winner, _me, _foeSkullCount, Ctx.Turn, Ctx.ForfeitedBy,
                                           _foeWarlordMinHp == int.MaxValue ? 30 : _foeWarlordMinHp,
                                           // 🆕 2026-10-18（A940 收尾）：教程那两个「跳过正常结算演出」的开关
                                           // （`TutorialStage +0x60/+0x70`；判据与那条「读数点本地拿不到」的缺口
                                           //  写在 `EndPanel.Show` 的 `<param name="skipSequence">` 上）
                                           skipSequence: TutorialSkipsNormalEnd());
                            // 🔴 **2026-10-14（A660）：结算门动画期间**冻住输入层**** ——
                            //   原版 `BattleManager._CloseBattleDoors` 协程逐句
                            //   （`d:/2/tools/decomp_full/BattleManager._CloseBattleDoors_d__393__MoveNext.c`）：
                            //   `StopTracking(true)`（`:22`）→ 结果物件 `SetActive(true)`（`:26`）→
                            //   `BattleHud.ToggleWithAnimation(false, …)`（`:29`，把 HUD 动画收起）→
                            //   `EndBattleDoors.SetupDoor(…)`（`:41`，= 我们 `_endPanel.Show` 里头那段
                            //   「准备开门视频 + 奖励跟视频同时出」）→
                            //   → **`TouchInputManager__Toggle(instance, 0, 0)`（`:50`）** → `yield WaitForSeconds(len)`（`:52`）。
                            //   我们的等价动作 = 把 `Battle/TouchInputManager` 这件**停摆**
                            //   （它的 `Toggle` 语义是「停跑 `Update`」、**不是「清零」**——见那件的文件头 E）。
                            //   ⚠️ **`Update()` 里那条「`Ctx.IsOver` ⇒ 只留终端那两件（按 R 重开 / 点·ESC 离开）」的闸挡不住这一层**：
                            //     `CombatCameraZoom.LateUpdate → TickBody` 是**它自己的 Update 循环**，
                            //     每帧读 `TouchInputManager.ScrollDelta / TouchPressedSecondary / TouchDragDelta`
                            //     ⇒ 不冻这一层的话，结算动画期间**还能拖着镜头跑**（原版这时候已经冻了）。
                            //   ⚠️ **不按 `video` 分支**：原版那句 Toggle 在 `SetupDoor` 之后、与片长无关
                            //     ⇒ 即使这一局没有开门视频（`len <= 0`）也照样冻。
                            //   ⚠️ **用 `Current`（只读口）+ 判空**，照原版那句 `if (instance != null)` ——
                            //     那件组件缺席时原版也什么都不做（它从不自己建），我们跟着不建
                            //     （不给自检凭空添一个根物件）。真对局里它一定在。
                            //   ⚠️ 复位在 `Begin()`（原版靠重进场景复位，我们复用同一个 driver ⇒ 显式还）。
                            if (TouchInputManager.Current != null) TouchInputManager.Current.Toggle(false);
                        }
                        // 🆕 2026-09-23：**打完一局 → 任务进度动**（原版也是这条链：对局回来 `MissionChallengeProgress` 累加）。
                        // 战果**由引擎记**（`BattleContext.DamageToEnemy` / `TroopsPlayed`），这里只消费。
                        // 判据「赢没赢」与上面那行文字**同源**（`Ctx.Winner == _me + 1`），不另写一套。
                        //
                        // 🆕 **2026-10-11（批次 · A375）：第 4 / 5 个实参是补上的**（在此之前这条链**只推三条每日任务**，
                        //   **骷髅一颗都没往日常那边送过** ⇒ 骷髅卡只能挂一个出厂 mock；判据 = 原版
                        //   `ChallengeLogMgr.LogMatchEnd` → `BattleEndSignal(matchData, gameMode, **GetSkullCount()**, isWin)`
                        //   → `SkullsCount.OnBattleEnd` → `UpdateProgress(…, shouldOverride: false)` = **累加**）。
                        //   · 第 4 个 = **`_foeSkullCount`** —— `d:/2/tools/decomp_full/` 逐环核过，它就是原版
                        //     `BattleScoreManager.GetSkullCount()` 的**同一格字段**（与 HUD 的 `x N`、结算面板、
                        //     对局记录**四处同源**；它自己是用 `DeckRules.SkullsFor` 算的）。
                        //     ⛔ **别在这里再算一遍档位**（铁律 6）——尤其是**别拿 `_foeWarlordMinHp` 去反推**：
                        //     那个字段**已经不算骷髅了**（2026-10-06 A147/A148 改的口径，见它的字段注释）。
                        //   · 第 5 个 = 本局的 **`PlayModes` 号**（原版 `MatchData.GetMilestones()` 按它决定
                        //     「这一局有没有里程碑」⇒ 那 6 个模式一颗都不给）。
                        //     ✅ **2026-10-15（A383）已接真模式号** —— 原来这里传的是「经典参数 / 遭遇参数」
                        //     那一对推出来的 13/0（**两个都能分辨的档**），现在传的是 `Ctx.PlayMode`
                        //     = 入口窗真正声明的那一档。四扇入口窗今天各是：
                        //     练习（`PracticeEvent.get_EventPlayMode`，VA 0x1808B66B0）= `OfflinePractice 6` ·
                        //     `Deck info ▸ Practice Deck`（`StartMatch(0xc,…)`）= `OwnDeckTraining 12` ·
                        //     遭遇战（`FastModeBaseEvent`）= `Skirmish 13` ·
                        //     排位（`RankedV2Event.get_EventPlayMode` VA 0x1804BD440 = `xor eax,eax; ret`）= `Classic 0`。
                        //     `ModeGivesSkulls` 那张表里给骷髅的是 `{0,3,6,7,10,11,12,13,14}` 九个
                        //     ⇒ **上面这四档全在「给」那一组**，所以本笔改动**不改变任何一局的骷髅产出**
                        //     （它改的是「传得对不对」，不是「这一局的账」）。
                        DailyData.OnBattleEnd(Ctx.Winner == _me + 1, Ctx.DamageToEnemy[_me], Ctx.TroopsPlayed[_me],
                                              _foeSkullCount,
                                              (int)Ctx.PlayMode);
                        // 🆕 2026-09-27：**同一处再写一条本地对局记录**（用户当天拍板要做）。
                        // 原版这一步在服务器（每局结束写 `PlayerDataManager.battleLogData`）——
                        // 本地没有服务器 ⇒ 由我们记，**这是加功能、不是复刻**（判据 → `Shell/BattleLogData.cs` 文件头）。
                        RecordBattleLog();
                        // 🆕 2026-09-27：**录像也在这一处收尾**（用户当天拍板「做，我们需要录像」）——
                        // 原版 `BattleManager.SaveMatchWinner` 也是结算时才把录制交上去。
                        // ⚠️ 顺序要紧：`RecFinish` 会把录像文件名挂到**刚写的那条**对局记录上（`AttachReplay`）。
                        RecFinish(HeroDisplayName(me), HeroDisplayName(foe));
                    }
                }
            }
            else
            {
                _resultLabel.SetText("");
                if (_endPanel != null && _endPanel.Visible) _endPanel.Hide();
            }
        }

        /// <summary>结算时写一条**本地对局记录**（数据源 = `Shell/BattleLogData.cs`）。
        ///
        /// 🔴 **原版这一步是【服务器】做的** —— 每局结束写 `PlayerDataManager.battleLogData`
        /// （判据 → `资料/普查产出_0927/档案窗_BattleLog与页签按钮.md` §B·3）⇒ 本地没有这个源。
        /// 用户 **2026-09-27 拍板**：由我们在结算这一处记。**这是加功能、不是复刻**
        /// （`Shell/BattleLogData.cs` 文件头如实标着）。
        ///
        /// 判据**与结算面板同源**，一处都不另写：结果 = `Ctx.Winner`（`3` = 平局）·
        /// 我方骷髅 = **已达成档数**（`_foeSkullCount`，= 原版 `BattleScoreManager.GetSkullCount()`，
        /// 与 HUD 那个 `x N`、结算面板**同一格字段** —— 2026-10-06 起不再用「最低生命反推」）。
        /// 对面骷髅 = `DeckRules.SkullsFor(我方督军降到过的最低生命)` —— 判据是原版
        /// `BattleScoreManager.GetEnemySkullCount(int ownLife)`：它逐档 `if (threshold &lt; ownLife) 不计数`
        /// （`BattleScoreManager__GetEnemySkullCount.c:28`）⇒ **计数条件 = `ownLife &lt;= threshold`**，
        /// 与 `SkullsFor` 的 `&lt;=` **逐字等价**（那行严格小于是**否定分支**，不是另一条口径 ⇒ **不用照改**）。
        /// ⚠️ 唯一还差的一点（如实记）：原版传的是**当时那个督军的当前生命**，我们传「降到过的最低生命」——
        /// **只有「被打下去又治回来」才会分叉**（原版那一刻会算得少一颗，我们不会）。本轮**没改**这一条。
        /// ⚠️ 原来这里那句注释「`SkullsFor(30)` 与 `SkullsFor(int.MaxValue)` 同值 0」已随改口径删掉
        /// （`OwnSkulls` 不再有这个兜底）。
        ///
        /// ⚠️ **捞不着的一律留空、不编**（红线）：联盟名（我们没有联盟那一套）·
        /// 段位分（本地没有段位数据）· **对面玩家名**（单机打 bot 没有名字，只有联机局才有真名）。
        /// </summary>
        void RecordBattleLog()
        {
            var me = Ctx.Players[_me];
            var foe = Ctx.Players[1 - _me];
            BattleLogData.Add(new BattleLogData.Match
            {
                Result = Ctx.Winner == 3 ? BattleLogData.Outcome.Draw
                       : Ctx.Winner == _me + 1 ? BattleLogData.Outcome.Victory
                       : BattleLogData.Outcome.Defeat,
                OwnHeroName = HeroDisplayName(me), EnemyHeroName = HeroDisplayName(foe),
                OwnName = ProfileData.PlayerName,
                // 单机打 bot **留空**（原版那格是服务端账号 id，本地没有对等物）；
                // 联机局才有对面的真名 —— 源只有一处：`NetMatchmaking.FoeName`（别自己取机器名）
                EnemyName = _net != null ? NetMatchmaking.FoeName : "",
                PlayerClan = "", EnemyClan = "",
                OwnSkulls = _foeSkullCount,
                EnemySkulls = DeckRules.SkullsFor(_myWarlordMinHp == int.MaxValue ? 30 : _myWarlordMinHp),
                OwnScore = "", EnemyScore = "",
                // 模式：原版 `matchType` → 本地化键那条映射**本地查不到**（`BattleLogData.Mode` 的注释）
                // ⇒ 存的就是**本工程唯一的模式字符串口径**（`PlayModeNames.Name`；`NetPendingBattle` 的
                // `ModeStr` 与录像头同款）。
                // 🔴 **2026-10-15（A383）订正**：原来是 `Ctx.Vars.IsSkirmish ? "Skirmish" : "Classic"`
                //   —— 那问的是「用哪一套参数」，**不是「这一局是哪个模式」**（练习局因此也印 `Classic`）。
                //   现在印 `Ctx.PlayMode` 的枚举名（练习局 = `OfflinePractice`、遭遇 = `Skirmish`…）。
                //   ⚠️ 如实标：这一格是**给人看的字符串**（`MatchLogRow` 直接画上去），而原版那一档是
                //      **本地化过的**模式名（本地没有那张表）⇒ 我们印的是**枚举名**，不是原版那句话。
                Mode = PlayModeNames.Name(Ctx.PlayMode),
                Pinned = false,
                // 回放**编号**恒 -1：原版那个数是**服务器分配的**，本地没有对等物
                //（判据 → `BattleLogData.Match.RecordingIndex` 的注释；「有没有录像」认的是 `ReplayFile` 那一格）。
                // 🔴 **2026-10-12 订正（铁律 5）**：这句原来写「**回放没做**（§三 第 18 条 第 6 件）
                //    ⇒ 没有编号可比。行上那颗 `ReplayButton` 本来就会如实出声」—— **前半句是错的**：
                //    回放 **2026-09-27 就做完了**（`RecFinish` 把文件名挂到刚写的那条记录上 =
                //    `BattleLogData.AttachReplay`；行上那颗钮走 `Shell/MatchLogRow.cs` 的 `OnReplay` **真的会播**）。
                //    错因 = 那句是回放还没做时写的，做完之后没回头改。
                RecordingIndex = -1,
            });
        }

        /// <summary>督军名的**显示名**（有中文**且当前语档是中文**就用中文 —— 与卡面 / 单位发言同一条规矩，
        /// 判据只此一处 = <see cref="CardDisplayName"/>）。卡池里查不到就原样回英文，**不静默丢成空串**。
        /// 🔴 **2026-10-18 就地补语言闸（铁律 5）**：本方法原来写 `string.IsNullOrEmpty(c.NameZh) ? c.Name : c.NameZh`
        /// —— 那是**直接读 `NameZh`**，英文档下照样印中文（`CardText.Zh` 那道闸被绕过）。</summary>
        static string HeroDisplayName(PlayerState p)
        {
            var c = (p != null && p.Warlord != null) ? p.Warlord.Card : null;
            if (c == null) return "";
            return CardDisplayName(c, c.Name);
        }

        // ==================================================================
        //  输入
        // ==================================================================

        public void SetCamera(Camera c)
        {
            cam = c;
            // 🆕 2026-09-29：准星的**双平面求交**要两台相机 —— 原版 `boardCamera.WorldToScreenPoint`
            // + `hudCamera.ScreenPointToRay`（`TargetReticleController__UpdateTrail.c`）。
            // 判据与两个平面的来源 → `TargetReticle.ResolveAim` 上面那一段注释。
            if (reticle != null) { reticle.hudCam = c; reticle.boardCam = boardCam != null ? boardCam : c; }
        }

        // ==================================================================
        //  悬停信息层（原版 `EverguildTooltipTrigger` + `EverguildTooltipManager`）
        // ==================================================================
        // 原版做法（全量反编译，见 `CardPresentation/Core/Tooltip.cs` 头部那一段）：
        //   · 触发器 `OnPointerEnter` **立刻**显示 / `OnPointerExit` **立刻**隐藏（无延迟）
        //   · tooltip 出现在**触发器自己的位置** + offset，**不跟随鼠标**
        //   · 按下鼠标键时 `Manager.Update` 会 Hide（原版那道闸照做）
        // 挂点与逐值（节点树，`资料/战斗规格/战斗重建_0827/子代理读报_2dcard_0827.md:93-104`）：
        //   · Health 容器 `Tips/HealthTip`   **anchor 10** offset (73.05, 0)
        //   · Ranged 容器 `Tips/RangedAttackTip` **anchor 15** offset (−53.54, 0)
        //   · Melee  容器 `Tips/MeleeAttackTip`  **anchor 15** offset (−49.33, 0)
        //   · Cost   容器 `Tips/CostTip`      **anchor 10** offset (52.6, 0)
        //   🔴 **护甲容器原版就没有 tooltip**（`子代理读报_2dcard_0827.md:95`：「此容器无任何脚本/无 tooltip」）
        //      ⇒ 我们**也不给它做**（照原版，别自作主张补一个）。
        // ⚠️ offset 原版是 UI 空间的 px，我们按 108 px/单位换算 —— 这一步是近似，如实标。
        const float TipPx = 108f;

        void TickTooltip()
        {
            var mouse = Mouse.current;
            if (mouse == null) { Tooltip.Hide(); return; }
            // 原版 `Manager.Update`：鼠标按下就收起来
            if (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame) { Tooltip.Hide(); return; }
            TickTooltipAt(WorldPointer());
        }

        /// <summary>自检入口：批处理没有鼠标 ⇒ 直接喂一个世界坐标。
        /// **和鼠标那条路调的是同一个函数**（不是第二份实现）。返回「有没有显示出来」。</summary>
        public bool TickTooltipAt(Vector3 wp)
        {
            // 🆕 2026-09-21：**关键词的 tooltip 先判** —— 它比数值那圈命中区更精确
            //    （关键词段在卡面中部的文字区，与卡底的数值圈基本不重叠；真重叠时以关键词为准）。
            foreach (var v in _handViews) if (TickTraitTip(v, wp)) return true;
            foreach (var kv in _myUnits) if (TickTraitTip(kv.Value, wp)) return true;
            foreach (var kv in _foeUnits) if (TickTraitTip(kv.Value, wp)) return true;
            if (_cardDisplay != null && _cardDisplay.Visible && TickTraitTip(_cardDisplay.Card, wp)) return true;

            foreach (var v in _handViews) if (TickCardTip(v, wp)) return true;
            foreach (var kv in _myUnits) if (TickCardTip(kv.Value, wp)) return true;
            foreach (var kv in _foeUnits) if (TickCardTip(kv.Value, wp)) return true;
            if (_cardDisplay != null && _cardDisplay.Visible && TickCardTip(_cardDisplay.Card, wp)) return true;

            // ---- HUD 计数（原版挂点：`Milestones`→`Tips/Hud/Skulls`、能量/信仰/灵魂石/任务点各自一个）----
            // 🔴 **`A1086②`（2026-10-09）：照原版把敌我拆开** —— 原版 `Tips/Hud/*Count` 是
            //    **敌我各一条键**（`EverguildTooltipTrigger.text`，各 13 颗；`Core/Loc.cs` 那一块有逐条计数），
            //    而本处原来两个半支**共用同一条** `Player*` ⇒ **对面的图标现在取 `TipText.Foe*`**
            //    （键 `Tips/Hud/Opponent{Energy,Faith,SpiritStone,QP}Count`）。
            //    ⚠️ `Skulls` **原版只有一条**（无 `OpponentSkulls`，双方共用同一颗里程碑）⇒ 不动。
            if (HitTip(_skullIcon, wp, TipText.Skulls)) return true;
            if (HitTip(_myEnergyPlate, wp, TipText.Energy) || HitTip(_foeEnergyPlate, wp, TipText.FoeEnergy)) return true;
            if (HitTip(_myQuestIcon, wp, TipText.QuestPoints) || HitTip(_foeQuestIcon, wp, TipText.FoeQuestPoints)) return true;
            if (HitTip(_myFaithIcon, wp, TipText.Faith) || HitTip(_foeFaithIcon, wp, TipText.FoeFaith)) return true;
            if (HitTip(_myStoneIcon, wp, TipText.SpiritStone) || HitTip(_foeStoneIcon, wp, TipText.FoeSpiritStone)) return true;

            Tooltip.Hide();
            return false;
        }

        /// <summary>
        /// 🆕 2026-09-21：卡面**关键词**的 tooltip（悬停那枚图标 / 那个词时弹）。
        ///
        /// **原版这条链**（全量反编译，见 `资料/tooltip_原版规格与实现.md` §五）：
        /// 关键词段整项套 `&lt;link=&lt;DefinedTrait枚举名>>`（`GameStaticData__TraitNameToString.c:84-109`）
        /// → `TextTooltipController` 每帧 `TMP_TextUtilities.FindIntersectingLink` 命中
        /// → `EverguildTraitTooltipItem`（比基础版多 **图标 + 标题**）。
        /// 我们这条：`CardText.KeywordSegment` 套 `&lt;link=规范键>`（卡面组装那一侧）→
        /// `CardView.LinkAt` → `TmpFont.LinkAt`（TMP 自己命中）→ 这里出文案。
        ///
        /// ⚠️ **认得才弹**：`TipText.ByLink` 查不到就返回 null（键是空的）⇒ **不弹面板、不编一句话**（红线）；
        /// 规则书里没有条目但名字凑得出来的（自造词 `ability`），**出名字 + 如实写「规则书里没有这个词的条目」**。
        /// </summary>
        bool TickTraitTip(CardView v, Vector3 wp)
        {
            if (v == null || !v.gameObject.activeSelf) return false;
            string key = v.LinkAt(wp, cam);
            if (string.IsNullOrEmpty(key)) return false;
            string body = TipText.ByLink(key);
            if (string.IsNullOrEmpty(body)) return false;
            // 面板摆在**那一层文字**的位置上（不是鼠标位置）—— 原版 tooltip 也是「跟着触发器、不跟鼠标」
            Tooltip.Show(body, v.KeywordAnchor);
            return true;
        }

        bool HitTip(ImageQuad q, Vector3 wp, string text)
        {
            if (q == null || !q.gameObject.activeSelf || !q.Contains(wp)) return false;
            Tooltip.Show(text, q.transform.position);
            return true;
        }

        /// <summary>卡面数值的 tooltip。锚点/偏移逐值照原版；**护甲没有 tooltip**（原版就没有）。</summary>
        bool TickCardTip(CardView v, Vector3 wp)
        {
            if (v == null || !v.gameObject.activeSelf) return false;
            int s = v.StatAt(wp);
            if (s == CardView.StatNone || s == CardView.StatArmour) return false;
            Vector3 at = v.StatWorld(s);
            switch (s)
            {
                case CardView.StatMelee:  Tooltip.Show(TipText.Melee,  at, 15, new Vector3(-49.33f / TipPx, 0f, 0f)); return true;
                case CardView.StatRanged: Tooltip.Show(TipText.Ranged, at, 15, new Vector3(-53.54f / TipPx, 0f, 0f)); return true;
                case CardView.StatHealth: Tooltip.Show(TipText.Health, at, 10, new Vector3( 73.05f / TipPx, 0f, 0f)); return true;
                case CardView.StatCost:   Tooltip.Show(TipText.Cost,   at, 10, new Vector3( 52.60f / TipPx, 0f, 0f)); return true;
            }
            return false;
        }

        Vector3 WorldPointer()
        {
            if (PointerWorldForTest.HasValue) return PointerWorldForTest.Value;   // 自检钉死（生产恒 null）
            Vector2 sp = PointerScreen();
            // 🔴 **2026-10-14（A659）：照原版 `BattleManager.GetMousePerspectivePos` 那道「在不在屏幕内」的守卫。**
            //   指针跑到窗口外时**归零**，⛔ **不外推** —— 外推会给出一个界外的世界点，
            //   下游拿它做命中判定就会「明明指着屏幕外、却命中了场上的东西」。
            //   原版逐句（`d:/2/tools/decomp_full/BattleManager__GetMousePerspectivePos.c:26-27`）：
            //     `0.0 <= x && x < Screen.width && 0.0 <= y && y < Screen.height` 才 `ScreenToWorldPoint`，
            //     否则 `return Vector2.zero`（`:40-44`；那是**世界坐标**的零，见
            //     `Core/LayoutSpace.IsInsideScreen` 的注释——判据与「为什么不塞进 `ScreenToWorld`」都写在那儿）。
            //   ⚠️ **为什么这条今天才补**：真机上拖到窗口外时 `Mouse.current.position` 会报**负值/超宽**，
            //     `Hand/CardInteraction.PointerWorldSafe` 那条「世界坐标超过可见区 ±1.5 倍就丢帧」的启发式
            //     正是为同一族症状打的补丁（它的注释记着实测抓到过反推屏幕 x ≈ **-4127 px** 的一帧）
            //     ⇒ 那边是**世界空间**的事后过滤，这边补的是**屏幕空间**的源头守卫。
            //   ⚠️ **自检不受影响**：批处理下 `Screen` = 640×480、屏幕点恒 (0,0)（在屏幕内），
            //     而且自检要么钉 `PointerWorldForTest`（在上一句就返回了）、要么直喂世界坐标。
            if (!LayoutSpace.IsInsideScreen(sp)) return Vector3.zero;
            return LayoutSpace.ScreenToWorld(sp, cam);
        }

        static Vector2 PointerScreen()
        {
            if (Mouse.current != null) return Mouse.current.position.ReadValue();
            if (Touchscreen.current != null) return Touchscreen.current.primaryTouch.position.ReadValue();
            return Vector2.zero;
        }

        // ==================================================================
        //  输入的两条沿（🆕 2026-10-13 · A462）
        //
        //  🔴 **为什么要有这一层**：原版**没有一处**拿「按住不放」当点击 —— 它的点击全来自
        //     uGUI `Selectable` / `IPointerClickHandler`（`Button.OnPointerClick` → `m_OnClick`），
        //     而那条链在 **`StandaloneInputModule.ReleaseMouse()`（松手那一帧）** 才 `Execute`
        //     （判据 = 本机自带源码 `com.unity.ugui/.../UI/Core/Button.cs:110-116`、
        //      `.../EventSystem/InputModules/StandaloneInputModule.cs:208-217`；
        //      `Selectable.OnPointerDown:1201-1212` 只做选中/视觉态，**不触发 `onClick`**）。
        //     我们原来只有一个**按下沿** latch（`ClickedThisFrame`）⇒ 每一颗钮都早了一帧：
        //     「按下去、拖到别处再松手」在本工程会触发，在原版**不会**。
        //     逐处判据（15 处代码调用，一处一档）→ `资料/普查产出_1013/WA462_输入入口分类.md` §三。
        //
        //  ⚠️ **两处入口原版本身就是「按下」（别一起改掉）** —— 它们是 `EventTrigger`，
        //     `m_Delegates[].eventID = 2`，而 `2 = PointerDown`（判据 = 本机
        //     `com.unity.ugui/.../EventSystem/EventTriggerType.cs:24`：
        //     `PointerEnter=0 · PointerExit=1 · PointerDown=2 · PointerUp=3 · PointerClick=4`）：
        //       · 战斗日志面板的背板 `shade`（`M CemeteryManager.HideCemeteryLogBtn`，场景 `GameObject/shade.json`）
        //       · `ChatPopup` 的**条外关闭区** `CloseChatPopup`（`VoiceLinesPopupSelector.Hide`）
        //     ⚠️ 我们的两份旧文档把这一格写成了 `PointerClick` —— **那是错的**（`PointerClick` 是 4）：
        //       `资料/语音线_原版规格与ASR管道.md:329` · `资料/战斗规格/战斗重建_0827/子代理读报_front交互层_0827.md:288`。
        //       按那个错值读，会把这两处**方向正好读反**。
        // ==================================================================

        /// <summary>这一帧「按下」了吗（= 按住状态的**上升沿**）。只由 <see cref="PollInputEdges"/> 写。</summary>
        bool _downEdge;
        /// <summary>这一帧「松手」了吗（= 按住状态的**下降沿**）。只由 <see cref="PollInputEdges"/> 写。</summary>
        bool _upEdge;
        /// <summary>上一帧按着没有 —— 两条沿都靠它（= 原来那个 `_clickLatch` 的同一个意思）。</summary>
        bool _held;
        /// <summary>这一次**按住**被「持续拖拽件」接住了（只有三根音量滑块会置它）。
        /// 置了 ⇒ 松手那一帧**不算点击**（原版 uGUI 靠 `StandaloneInputModule` 的
        /// `eligibleForClick` 被拖动清掉来做同一件事）。在 <see cref="PollInputEdges"/> 里清。</summary>
        bool _pressCaptured;

        /// <summary>🔴 **这一次按住的松手沿要吞掉。** 只有「**按下那一帧就把一个模态关掉/取消掉**」
        /// 的三处会置它（日志面板背板 · `ChatPopup` 条外关闭 · 攻击选择器的槽外取消）。
        ///
        /// **为什么必须有**：原版那几处的「按下」入口都是**全屏最上层**的节点（`shade` /
        /// `CloseChatPopup` / 选择器底板），底下那颗 HUD 钮**根本收不到那次按下** ——
        /// `StandaloneInputModule` 只把 click 发给 `pointerPress`（按下那一刻命中的那一件）。
        /// 不吞的话：按住日志钮 = 关日志，同一按的**松手沿又会把它打开**
        /// （画面闪一下、看着像「点了没反应」）。</summary>
        bool _swallowNextRelease;

        /// <summary>
        /// 每帧**最先**算一次两条沿（`Update` 的第一句）。
        ///
        /// 🔴 **必须在最前面算、而且和后面谁读到无关**：`Update` 里有不少**提前 return** 的分支
        /// （最典型 = `BoardPress` 一接管这个回合就 return），
        /// 若把边沿算在某个分支里面，那次松手就会被推迟到**下一帧**才被看见
        /// （`_held` 还停在 `true`）⇒ 松手之后指针底下有什么就打什么，**一次凭空的点击**。
        /// 把「这一帧的边沿」变成帧的状态，这类问题就**结构上不可能发生**。
        /// </summary>
        void PollInputEdges()
        {
            bool held = PointerHeld();
            _downEdge = held && !_held;
            if (_downEdge) _swallowNextRelease = false;      // 新的一次按住 ⇒ 上一次的「吞」作废
            _upEdge = !held && _held && !_pressCaptured && !_swallowNextRelease;
            if (!held) { _pressCaptured = false; _swallowNextRelease = false; }   // ⚠️ 清在**算完之后**
            _held = held;
        }

        /// <summary>**按下**那一帧（一次按住只给一次）。名字保留（`Clicked…` 读起来像「点击」，实际是**按下沿**）。
        /// ⚠️ 它的语义**一个字都没改**（还是按下沿）；改的是**谁还在用它**：
        /// 2026-10-13（A462）之后，凡对应原版 uGUI 的钮都改走 <see cref="ReleasedThisFrame"/>，
        /// 全文件只剩 **4 处**还在用它（两处原版本来就是按下 + 选择器槽外取消 + `_pressSlot` 那个中间态）
        /// —— `Editor/BattleScene.cs` 的 `★ A462` 段有一条**源级断言**钉住这个数。</summary>
        bool ClickedThisFrame()
        {
            if (!_downEdge) return false;
            _downEdge = false;                      // 用掉就没了：同一次按住只算一次
            LogClickBeat();
            return true;
        }

        /// <summary>**松手**那一帧（= 原版 uGUI `Button.onClick` / `IPointerClickHandler` 的触发沿）。
        /// 与 <see cref="ClickedThisFrame"/> 同一套边沿、一样的「用掉就没了」。</summary>
        bool ReleasedThisFrame()
        {
            if (!_upEdge) return false;
            _upEdge = false;
            LogClickBeat();
            return true;
        }

        /// <summary>🆕 **真实点击记录**（用户 2026-09-24 要的；与 `PointerLayer` / `DeckRuntime` 那两条同源）。
        ///    ⚠️ 战场里**没有单一命中表**（卡 / 手牌 / HUD / 面板各判各的）⇒ 这一条主要靠
        ///    「这一帧的日志」说话；另附世界坐标，方便对到是哪张卡/哪块地盘。
        ///    🔴 **两条沿都调它** —— 改沿之后若只在按下那一支记，改到抬起的那 12 处
        ///    **一条记录都不会有**（记录会少掉一大半，而画面看上去一切正常 = 典型静默）。</summary>
        void LogClickBeat()
        {
            if (!ClickLog.Enabled) return;
            var wp = WorldPointer();
            ClickLog.Begin(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, "BattleDriver",
                           LayoutSpace.ToPixel(wp));
            ClickLog.Hit("世界坐标 (" + wp.x.ToString("F2") + ", " + wp.y.ToString("F2") + ", "
                         + wp.z.ToString("F2") + ")"
                         + " —— 战场没有统一的命中表，**实际吃到的是哪一件看下面这帧的日志**");
        }

        /// <summary>指针**按着**（不带 latch）。滑块拖动要用它 —— 拖动是持续状态，不是一次点击。
        /// <para>⚠️ **它和 <see cref="PointerHeldRaw"/>（约 `:5310`）是同一件事**（都是「按住」）——
        /// 差别只有：这一个多一个**自检钉死口**（`PointerHeldForTest`）。两个名字**共用同一份设备读取**
        /// （本方法就一句转发），所以不会再出现「同一段 `isPressed` 抄两份」。
        /// 🔴 **2026-10-14（A651）**：另一半原来叫 `PointerDown()`，是个**有歧义的名字**
        /// （与 `ClickedThisFrame()` 的「按下沿」、原版 `EventTriggerType.PointerDown` 撞名）⇒ 已改名。</para></summary>
        static bool PointerHeld()
        {
            if (PointerHeldForTest.HasValue) return PointerHeldForTest.Value;   // 自检钉死（生产恒 null）
            return PointerHeldRaw();
        }

        // ---- 两条沿的自检口（⛔ 生产路径一个都不调）----
        //
        // 🔴 批处理里 `Mouse.current == null`（没有鼠标）⇒ `PointerHeld()` 恒 false
        //    ⇒ **两条沿一条都验不了**；`WorldPointer()` 也恒指着屏幕那一角 ⇒ 命中区断言全是空的。
        //    所以这一族口子只做两件事：把「按住/松手」和「指针在哪」钉死，剩下的照旧走真判据。

        /// <summary>自检专用：把「按住 / 松手」钉死（`null` = 走真实设备 —— **生产恒为 null**）。</summary>
        public static bool? PointerHeldForTest;

        /// <summary>自检专用：把指针的世界坐标钉死（`null` = 走真实设备的屏幕坐标）。</summary>
        public static Vector3? PointerWorldForTest;

        /// <summary>自检专用：走一次**边沿的那一步**（= `Update` 的第一句）。
        /// 调它一次 = 过了**一帧**；两条沿按 `PointerHeldForTest` 取值。</summary>
        public void PollInputEdgesForTest() { PollInputEdges(); }

        /// <summary>自检专用：这一帧是不是**按下**（**会消耗掉它** —— 和内部调用同一条路）。</summary>
        public bool ClickedThisFrameForTest() { return ClickedThisFrame(); }

        /// <summary>自检专用：这一帧是不是**松手**（同上，会消耗）。</summary>
        public bool ReleasedThisFrameForTest() { return ReleasedThisFrame(); }

        /// <summary>自检专用：这一次按住有没有被滑块接住（`_pressCaptured`）。</summary>
        public bool PressCapturedForTest { get { return _pressCaptured; } }

        // ---- 🆕 2026-10-13（A462）「两条沿」那一族的自检口 ----
        //
        // ⚠️ 全是**转发**（判据只有一份，在 `Update` / 那几个 `Handle*` / 两个 `Tick*` 里），
        //    ⛔ 一个判据都没复制 —— 否则自检验的就是它自己那一份了。
        //    返回值的语义与 `Update` 里那一段**一致**：true = 这一帧被它接管了。

        /// <summary>自检用：跑一次「多卡摊开窗」那一支的输入闸。</summary>
        public bool TickMultiCardsForTest() { return TickMultiCards(); }
        /// <summary>自检用：跑一次「放大窗」那一支的输入闸。</summary>
        public bool TickCardDisplayForTest() { return TickCardDisplayClick(); }
        /// <summary>自检用：跑一次战斗日志 / 墓地钮那一路的输入闸。</summary>
        public bool TickLogInputForTest() { return HandleBattleLog(); }
        /// <summary>自检用：跑一次 `ChatPopup` 那一路的输入闸。</summary>
        public bool TickChatInputForTest() { return HandleChatPopup(); }
        /// <summary>🆕 2026-10-18（A985③）：自检用：跑一次联盟面板那一路的输入闸
        /// （= `Update` 里 `HandleAlliancePanel` 那一行）。</summary>
        public bool TickAllianceInputForTest() { return HandleAlliancePanel(); }
        /// <summary>自检用：跑一次设置面板 / 设置钮那一路的输入闸（含三根滑块）。</summary>
        public bool TickSettingsInputForTest() { return HandleSettings(); }
        /// <summary>自检用：跑一次回放条那一路的输入闸。</summary>
        public bool TickReplayBarForTest() { return HandleReplayBar(); }
        /// <summary>自检用：跑一次换牌面板那一路的输入闸。</summary>
        public bool TickMulliganInputForTest() { return HandleMulligan(); }
        /// <summary>自检用：跑一次选牌面板那一路的输入闸。</summary>
        public bool TickChooseInputForTest() { return HandleChoose(); }
        /// <summary>自检用：跑一次 HUD 那几颗「点了立刻做一件事」的小钮（进攻卡 / 重置镜头 / 墓地）
        /// —— **顺序与 `Update` 里那三行逐字一致**（前一颗没接住才轮到下一颗）。</summary>
        public bool TickHudButtonsForTest()
        {
            if (HandleOffensiveButton()) return true;
            if (HandleCameraResetButton()) return true;
            return HandleBattleLog();
        }
        /// <summary>自检用：跑一次**玩家回合**那条输入链（= `Update` 里调的那一个 `DrivePlayerTurn`）。
        /// ⚠️ 它只做「玩家回合的输入」，不推时钟、不跑 AI。</summary>
        public void DrivePlayerTurnForTest() { DrivePlayerTurn(); }

        /// <summary>🆕 2026-10-18（审查 K8）：自检用：跑一次**对手回合**那条链（= `Update` 里那个 `DriveAiTurn`）。
        /// ⚠️ 先把 `_aiTimer` 清零 —— 那条链头两句是节流（`_aiTimer -= Time.deltaTime; if (> 0) return;`），
        ///    批处理里 `Time.deltaTime` 是上一帧的数、**推不动它**，不清零会「调了等于没调」（假绿）。</summary>
        public void DriveAiTurnForTest() { _aiTimer = 0f; DriveAiTurn(); }

        /// <summary>自检用：`Update` 里 `_multiCards` / `_cardDisplay` 那两段的门（绕过窗口显隐的中间态）。</summary>
        public bool MultiCardsVisibleForTest { get { return _multiCards != null && _multiCards.Visible; } }

        /// <summary>自检用：本机那一方（`_me`）。摆探针单位 / 判目标侧都要它。</summary>
        public int MySideForTest { get { return _me; } }

        /// <summary>自检用：HUD 上某颗用 `ImageQuad` 字段存着的钮的世界坐标（喂给上面那套输入口用 ——
        /// 批处理里 `WorldPointer()` 是个死点，必须由自检钉死）。
        /// `which` = `"settings"` / `"cemetery"` / `"chat"` / `"offensive"` / 🆕 `"cameraReset"`；找不到返回 `Vector3.zero`
        /// （断言侧会表现为「没命中」⇒ 红，**不会静默通过**）。
        /// <para>🆕 **2026-10-18（A964 前置）**：补上 `"cameraReset"` 档 —— 它是**全 HUD 唯一出厂就 `SetActive(false)`**
        /// 的那颗钮（`BuildHudExtras` 建完立刻关），所以探测它**必须**走本表拿坐标，不能靠「可见时再取」。
        /// ⚠️ `"settings"` 那颗在 <see cref="HandleSettings"/> 里**没有 `activeSelf` 守卫**（本函数也没加）——
        /// 生产不可达（它出厂就亮着、没有关它的路径）⇒ **隐患、不是活缺陷**；A964 的探针会把它翻出来，
        /// ⛔ 别顺手给它补守卫（那会改行为，断言里也没有这一条）。</para></summary>
        public Vector3 HudButtonWorldPosForTest(string which)
        {
            ImageQuad q = null;
            switch (which)
            {
                case "settings":    q = _settingsBtn;    break;
                case "cemetery":    q = _cemeteryBtn;    break;
                case "chat":        q = _chatBtn;        break;
                case "offensive":   q = _offensiveBtn;   break;
                case "cameraReset": q = _cameraResetBtn; break;
            }
            return q != null ? q.transform.position : Vector3.zero;
        }

        /// <summary>自检用：那颗钮现在显示着吗（`activeSelf`）。键表同 <see cref="HudButtonWorldPosForTest"/>。</summary>
        public bool HudButtonActiveForTest(string which)
        {
            ImageQuad q = null;
            switch (which)
            {
                case "settings":    q = _settingsBtn;    break;
                case "cemetery":    q = _cemeteryBtn;    break;
                case "chat":        q = _chatBtn;        break;
                case "offensive":   q = _offensiveBtn;   break;
                case "cameraReset": q = _cameraResetBtn; break;
            }
            return q != null && q.gameObject.activeSelf;
        }

        /// <summary>自检用：把键表里某一颗钮显/隐（🆕 **2026-10-18（A964 前置）**）。
        /// 🔴 **只 `SetActive`、不起 tween** —— ⛔ 别改走 <see cref="ToggleCameraResetButton"/>：
        /// 那条是**产品语义**（显的那一下必须弹一次 `DOPunchScale`，还会 `DOTween.Kill`），
        /// 自检里借它摆状态会平白污染 `CameraResetPunchCount` 那个计数（A423 段正拿它做判别式）。
        /// 未知键 / 那一位是 null ⇒ **什么都不做**（返回 false），⛔ 不静默造一颗钮出来。</summary>
        public bool SetHudButtonActiveForTest(string which, bool on)
        {
            ImageQuad q = null;
            switch (which)
            {
                case "settings":    q = _settingsBtn;    break;
                case "cemetery":    q = _cemeteryBtn;    break;
                case "chat":        q = _chatBtn;        break;
                case "offensive":   q = _offensiveBtn;   break;
                case "cameraReset": q = _cameraResetBtn; break;
            }
            if (q == null) return false;
            q.gameObject.SetActive(on);
            return true;
        }

        /// <summary>点到哪个槽位了（从最前面往后测，压住的也能选中）</summary>
        static int HitSlot(Dictionary<int, CardView> views, Vector3 world)
        {
            int best = -1;
            float bestZ = float.MaxValue;
            foreach (var kv in views)
            {
                var v = kv.Value;
                if (v == null || !v.Contains(world)) continue;
                float z = v.transform.position.z;
                if (z < bestZ) { bestZ = z; best = kv.Key; }
            }
            return best;
        }

        // ---- 给批处理自检用的显式接口（不进 play 模式也能走完整流程）----
        //
        // ⚠️ 这几个都走**和真实鼠标同一条路**（开选择器 → 选打法 → 点目标），
        //    不是绕过交互直接调引擎 —— 那样自检就验不到交互本身了。

        /// <summary>点自己的单位 → 弹出选择器。返回「真弹出来了」没有</summary>
        public bool SimulateOpenCommand(int slot) { OpenCommand(slot); return _selectedSlot == slot; }

        /// <summary>自检用：在棋盘上**轻点**一个单位（`side` = `_me` 我方 / `1-_me` 对手）。
        /// 走的是 `Update` ② 段**同一条** `BoardPress`（不另写一份判据）。返回「大卡展示窗现在开着吗」。</summary>
        public bool SimulateTapUnit(int side, int slot)
        {
            _pressSlot = slot; _pressSide = side; _pressWorld = Vector3.zero;
            BoardPress(Vector3.zero, false);            // 原地松手 = 轻点
            return _cardDisplay != null && _cardDisplay.Visible;
        }

        /// <summary>自检用：在棋盘上**拖够再松手**（原版的选中/攻击手势）。返回「三选一弹出来了吗」。
        /// 同样走 `BoardPress` —— 这条是「拖拽才是选中」那条原版口径的守卫。</summary>
        public bool SimulateDragUnit(int side, int slot)
        {
            _pressSlot = slot; _pressSide = side; _pressWorld = Vector3.zero;
            var far = new Vector3(AttackSelector.DragThresholdWorld * 2f, 0f, 0f);
            BoardPress(far, true);                      // 还按着，但已经拖够
            BoardPress(far, false);                     // 松手
            return _selectedSlot == slot;
        }

        /// <summary>点选择器上的某个按钮</summary>
        public void SimulateCommand(AttackKind kind) { CommitCommand(kind); }

        /// <summary>点敌方目标 → 结算。返回引擎码</summary>
        public int SimulateResolve(int foeSlot) { return Resolve(_command, foeSlot); }

        /// <summary>选择器上有没有这一项（自检用来挑一个能点的）</summary>
        public bool HasCommand(AttackKind kind)
        {
            if (selector == null) return false;
            foreach (var o in selector.Options) if (o.Kind == kind && o.Enabled) return true;
            return false;
        }

        public bool SimulateDeselect() { ClearSelection(); return _selectedSlot < 0; }
        /// <summary>把指针挪到某个世界坐标（只喂给选择器，不做别的）</summary>
        /// <summary>自检用：把指针挪到某处。**和真实输入共用同一套逻辑**（选择器高亮 + 准星 + 面板）</summary>
        public void SimulatePointerAt(Vector3 world, bool down = false)
        {
            if (selector != null) selector.UpdatePointer(world);
            // 准星和面板跟着同一个指针走。批处理没有 `Update()`，这里补上 —— 但走的是**同一个**
            // `UpdateReticle` / `SetPointer`，不另写一份判据
            if (_selectedSlot >= 0 && _command != AttackKind.None)
            {
                UpdateReticle(world);
                if (skillPanel != null && skillPanel.Visible) skillPanel.SetPointer(world, down);
            }
        }

        /// <summary>自检用：准星现在开着吗（`reticle` 为空也算 false）</summary>
        public bool ReticleVisible { get { return reticle != null && reticle.Visible; } }
        public void SimulateEndTurn() { EndPlayerTurn(); }
        public int SelectedSlot { get { return _selectedSlot; } }
        public AttackKind Command { get { return _command; } }
        public AttackSelector Selector { get { return selector; } }
        public bool SelectorOpen { get { return selector != null && selector.Visible; } }
        public AttackKind HoveredCommand { get { return selector != null ? selector.Hovered : AttackKind.None; } }
        public string SelectorDescription { get { return selector != null ? selector.Describe() : "（没有选择器）"; } }
        public int HandCount { get { return _handViews.Count; } }
        /// <summary>我方/对手阵营（自检用）。</summary>
        public string MyFaction { get { return _myFaction; } }
        public string FoeFaction { get { return _foeFaction; } }
        /// <summary>本局我方用的**存档卡组**（null = 自动凑的）。</summary>
        public PlayerDeck MyDeckSource { get { return _myDeckSrc; } }
        /// <summary>开局那句「本局用的是哪副牌」的原文。空串 = 没什么要交代的。</summary>
        public string DeckNotice { get { return _deckNotice; } }
        /// <summary>自检用：本局 `Begin` 收到的那个 `deckNote`（**原样**，没包成句子）。
        /// 🔴 **钉 A387 就比它**（「回放局不许再借『联机局』那张标签」）：比 `DeckNotice` 强 ——
        /// 那句**只有在 `ResolveDeck` 没给出人话时才生成**（牌有名字时 `deckNote` 压根用不上），
        /// 拿成品句子比 = 同一份代码在不同录像上结论不同（**弱断言**）。
        /// `null` = 这一档明说「没有『卡组读不出来』这回事」（回放局就该是 null）。</summary>
        public string RawDeckNoteForTest { get { return _deckNote; } }
        /// <summary>提示行现在写着什么。⚠️ 它平时等于 <see cref="DeckNotice"/>（休息态），
        /// 悬停/选目标时会被临时提示盖住 —— 断言「玩家看得见那句」要挑对时机。</summary>
        public string HintText { get { return _hintLabel != null ? _hintLabel.Text : null; } }
        /// <summary>提示行这块字有多宽（世界单位，可见区宽 = `LayoutSpace.VisibleWidth`）。
        /// ⚠️ `Label` 是 **NoWrap** 的，太长不会折行、只会横着长到屏幕外去 ——
        /// 而卡组名是玩家自己起的、长度不可控，所以要有条断言挡着。</summary>
        public float HintWidth { get { return _hintLabel != null ? _hintLabel.WorldW : 0f; } }
        public IReadOnlyDictionary<int, CardView> MyUnits { get { return _myUnits; } }
        public IReadOnlyDictionary<int, CardView> FoeUnits { get { return _foeUnits; } }

        /// <summary>
        /// 自检用：放技能。走的是**和真实点击同一条路**：开选择器 → 选技能 → 点目标。
        /// 返回引擎的码。`targetSlot &lt; 0` = 只到「选目标」这一步就停（给截图留的）。
        /// </summary>
        public int SimulateUseAbility(int slot, int targetSlot = -1)
        {
            if (!SimulateOpenCommand(slot)) return RuleCodes.ErrNotUnit;

            var u = Ctx.Players[_me].Board[slot];
            if (u == null || !u.HasAbility) { ClearSelection(); return RuleCodes.ErrNoAbility; }
            if (!HasCommand(AttackKind.Ability)) { ClearSelection(); return RuleCodes.ErrNoAbility; }

            CommitCommand(AttackKind.Ability);
            if (_selectedSlot < 0) return RuleCodes.OK;   // 不用选目标的技能已经结算完了
            if (targetSlot < 0) return RuleCodes.OK;      // 停在「选目标」

            return Resolve(AttackKind.Ability, targetSlot);
        }

        /// <summary>自检用：取手牌第 idx 张的视图（拖拽用例要拿它的世界坐标）</summary>
        public CardView HandViewAt(int idx)
        {
            return (idx >= 0 && idx < _handViews.Count) ? _handViews[idx] : null;
        }

        /// <summary>这张视图在第几张手牌上（-1 = 不在手里）。**和上面那个私有的 `HandIndexOf` 是同一个**
        /// —— 公开出来只是给自检用（发牌入场的断言要算出它的落点）。</summary>
        public int HandIndex(CardView v) { return v == null ? -1 : _handViews.IndexOf(v); }

        /// <summary>我方牌堆中心的世界坐标（发牌的起点）。原版的牌堆锚点在 `MyDeckX01/Y01`</summary>
        public Vector3 DeckWorld { get { return LayoutSpace.ToWorld(MyDeckX01, MyDeckY01); } }

        /// <summary>自检用：把画面上的手牌名字列出来对账</summary>
        public string HandViewNames()
        {
            var names = new List<string>();
            foreach (var v in _handViews) names.Add(v == null ? "<null>" : $"{v.Data.id}({v.Data.cost})");
            return string.Join("/", names.ToArray());
        }

        /// <summary>自检用：把场上的单位名字列出来（按槽位）</summary>
        public string BoardViewNames(bool mine)
        {
            var views = mine ? _myUnits : _foeUnits;
            var names = new List<string>();
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                CardView v;
                if (views.TryGetValue(s, out v) && v != null) names.Add($"{s}:{v.Data.id}");
            }
            return string.Join(" ", names.ToArray());
        }

        /// <summary>自检用：把手牌第 idx 张直接打到 slot（**跳过鼠标拖拽，但不跳过录像**）。
        ///
        /// 🔴 **2026-09-27 修**：这里原来**直接**调 `RuleCore.PlayCard` —— 绕过的不是一个动画，
        ///    是**录像唯一的记账口**（`LocalAct`，见文件头那张「每一处会动引擎的地方都要记」的清单 ④）。
        ///    后果：自检、以及 `BattleAutoDrive`（构建后 `-wfdrive` 自动打一局）在**我的回合**出的每一张牌
        ///    **都不在录像里** ⇒ 放出来是另一局，而且**只有终局指纹才露馅**（分叉点在第一条这种动作上）。
        /// ⚠️ 它和 `SimulatePlayViaPanel` 的区别**不在录像**（两条都记）而在**面板**：
        ///    那个会先把该问的问完（`BeginPlay`），这个不问。
        /// ⚠️ 别改成「不记」：手改局面的靶场小节本来就不可回放，但**动作**必须记全 ——
        ///    有没有记全，正是回放对账要检出来的东西。
        /// 🔴 🆕 **2026-10-18（审查 K11/R8）：它【不过教程白名单闸门】、也【不推脚本指针】。**
        ///    两个调用点都是**非玩家路**（自检 · `BattleAutoDrive` 的 `-wfdrive`），产品里出牌一律走
        ///    `BeginPlay → DoPlay`（那一支过闸门 ② 与 `ContinueTutorialScript`）⇒ **不构成玩家越权**。
        ///    但它**确实是一条能改引擎状态、却绕过闸门的旁路** —— 写在这里，免得下个会话以为
        ///    「闸门覆盖了所有出牌路」。⚠️ 若将来给它接玩家输入，**必须先补这两件事**。</summary>
        public int SimulatePlay(int idx, int slot)
        {
            var act = new AiAction { Kind = AiActionKind.PlayCard, HandIdx = idx, Slot = slot };
            int code = LocalAct(act, () => RuleCore.PlayCard(Ctx, _me, idx, slot));
            if (code == RuleCodes.OK) RefreshAll();
            return code;
        }

        /// <summary>自检用：本机座位（`_me` 的只读出口 —— 自检要按「谁是真人的那一侧」摆夹具）。
        /// ⚠️ 与联机那条 `SetMySeat` 是**同一件事的两面**：驱动内部 100 处 `_me` 全是相对的，这个只给测试读。</summary>
        public int MySeat { get { return _me; } }

        /// <summary>自检用（§25）：HUD 那颗进攻卡钮现在**露着吗**（原版判据 = 选定卡 ≠ 空卡）。</summary>
        public bool OffensiveButtonVisible { get { return _offensiveBtn != null && _offensiveBtn.gameObject.activeSelf; } }

        /// <summary>自检用（§25）：把「进攻卡生效」那一步跑一遍（产品里由 `BeginBattleAfterSetup` 调）。</summary>
        public void SimulateApplyOffensiveEnv()
        {
            ApplyOffensiveEnvOnce();
            // 批处理**没有帧循环** ⇒ 补间要手动推到底，断言才读得到「应用之后」的状态。
            if (_envApplier != null) _envApplier.Advance(_envApplier.BlendDuration + 1f);
        }

        /// <summary>自检用：环境执行器（**可能为 null** —— 这一局没选进攻卡时根本不建）。</summary>
        public EnvironmentApplier EnvApplierForTest { get { return _envApplier; } }

        /// <summary>自检用：**直接起一次「AI 准星演出」**（原版 `BattleManager.EnemyTargetingAnim`）。
        /// 产品里它由 `DriveAiTurn` 起、由 `AdvanceTimeline` 泵推进；批处理**没有帧循环**
        /// ⇒ 自检走这两个入口（同 `SimulateAiTurn` 那条理由）。
        /// 返回 false = 这条动作本来就不该演（目标不在真人那一侧 / 没开 `animateFeel` / 取不到视图）。</summary>
        public bool SimulateStartEnemyTargetingAnim(AiAction act) { return StartAiTargetingAnim(act); }

        /// <summary>自检用：推进那个演出 `dt` 秒（产品里由 `AdvanceTimeline` 推）。</summary>
        public void SimulateTickAiTargetingAnim(float dt) { TickAiTargetingAnim(dt); }

        /// <summary>自检用：演出在演吗。</summary>
        public bool AiTargetingAnimActive { get { return _aiAnimAct != null; } }

        /// <summary>自检用：把对手那一步也走完（省得等延时）</summary>
        public void SimulateAiTurn()
        {
            SimpleAI.PlayTurn(Ctx);
            if (Ctx.IsOver) { RefreshAll(); return; }
            // ⚠️ **走同一个 `EndTurnAndAdvance`**（不是图省事：录像那条判据只有一处，
            //    见它的注释 —— 这里各写一份的话，自检里录的局回放不出来）
            EndTurnAndAdvance(1 - _me);
            RefreshAll();
        }

        // ==================================================================
        //  🆕 2026-10-15：「让位」预览的自检口 —— 补上之前**一次都没走过**的那条链
        // ==================================================================
        //
        // 🔴 **为什么必须补**：`TickShufflePreview` 落码（2026-10-01，判据 = 原版
        //    `MinionManager__ReassembleMinionsWhilePlayingUnit.c`）之后，全仓**只有 `Update` 一个调用点**
        //    （本文件 `:5145`），而**批处理自检没有帧循环** ⇒ `Update` 不跑 ⇒ 这条链**一次都没被自检走过**
        //    （2026-10-15 可玩性普查查出：`项目任务.md` 把它记成「已完成」，实际零覆盖）。
        //
        // 形状照本仓既有的 `SimulateAiTurn` / `SimulateStartEnemyTargetingAnim` 那一族：
        //    **产品怎么走，测试口就怎么调**（同一份实现、不另写判据）。
        //
        // ⚠️ 三个口共同的坑（写在各自的 doc 上，这里点一句）：`TickShufflePreview` 头一句就是
        //    `if (!animateFeel …) return;`，而 `animateFeel` **默认 `false`**（批处理自检为「当场精确的坐标」
        //    关掉的）⇒ **调用方必须先自己打开它**，否则这几个口**静默什么都不做**
        //    （同 `PlayDeathFeel` 那一节的坑：2026-09-25 那次正向断言静默通过、反例才红）。

        /// <summary>自检用：摆出「正拖着牌、瞄着某一格」的预览态。
        /// 产品里每帧由 `CardInteraction` 喂给 `OnDropPreview`（松手 / 取消 / 重开一局喂 `which = null`）。
        /// <paramref name="requestedSlot"/> &lt; 0 = 收工（等价于 `OnDropPreview(null, -1)`）。</summary>
        public void SimulateDropPreview(bool mine, int requestedSlot)
        {
            OnDropPreview(requestedSlot < 0 ? null : (mine ? playerBoard : enemyBoard), requestedSlot);
        }

        /// <summary>自检用：把「让位」预览**推进一步**（产品里 `Update` 每帧调一次 `Time.deltaTime`）。
        /// 🔴 **批处理没有帧循环 ⇒ `dt` 由调用方给**；而它是**指数趋近**
        /// （`k = 1 − e^(−18·dt)`，帧率无关）⇒ 给一个够大的 `dt`（如 `1f`）时 `k ≈ 1 − 1.5e-8`，
        /// **一步就基本到位**（残差 ≈ 1.5e-8 × 格距），不用像补间那样反复 `Advance`。
        /// ⚠️ 它是**纯位移**（内部直接 `Vector3.Lerp` 写 `localPosition`），**不经过 `CardTween`**
        /// ⇒ 不要（也没必要）用 `CardTween.Advance` 推它。</summary>
        public void SimulateTickShufflePreview(float dt) { TickShufflePreview(dt); }

        /// <summary>自检用：预览**收工** —— 产品里对应 `OnDropPreview(null, -1)` 那一条
        /// （松手取消 / 拖回手牌 ⇒ 被推开的单位补一次位移回真格位）。
        /// 返回「这次确实有视图被推开过」—— 拿它当**前提**，免得夹具没摆对时后面的断言**静默走空**。
        /// ⚠️ 走的是 `OnDropPreview` 本身（不直接调 `EndPreviewReturn`）：那样才会把
        ///    `_previewOwner` / `_previewRequested` 一起清成 −1，否则预览态会**留在原地**。</summary>
        public bool SimulateEndShufflePreview()
        {
            bool moved = _previewMoved.Count > 0;
            OnDropPreview(null, -1);
            return moved;
        }

        /// <summary>自检用：现在摆着「让位」预览态吗（= `_previewRequested ≥ 0`，`TickShufflePreview` 的第一道门）。</summary>
        public bool ShufflePreviewArmed { get { return _previewRequested >= 0; } }
    }
}
