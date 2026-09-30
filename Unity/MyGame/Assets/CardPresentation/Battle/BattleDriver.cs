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
using RuleEngine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CardPresentation
{
    public class BattleDriver : MonoBehaviour, INetBattleHost
    {
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
        ///    · 计数器在（`RuleEngine/Core/PlayerState.cs:48` 信仰 / `:49` 灵魂石），写读口都全；
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
        /// <summary>联机局：**牌堆顺序由主机下发**（`RuleCore.NewBattle` 按座位顺序抽随机数洗牌
        /// ⇒ 两端镜像跑会洗出不同的牌堆，见 `NetBattle` 文件头）。</summary>
        bool _shuffleDecks = true;
        /// <summary>联机局：**对面换牌不跑 AI** —— 由主机定序后下发（`RuleCore.Mulligan` 会掷 `ctx.Rng`）。</summary>
        bool _noAiMulligan = false;

        public void AttachNet(NetBattle nb) { _net = nb; }

        /// <summary>这一帧该不该由**本机的 AI** 去驱动对面 —— **联机局永远不该**
        /// （对面那一侧是网络的活：`NetTick()` 把对面对作落地）。
        /// 🔴 单独做成一个判据（而不是把 `_net == null` 散在 `Update` 里）：这样自检能**直接问它**，
        ///    盯的是「做判断的那个人」，不是「我另写一份判断」。</summary>
        public bool AiShouldDriveOpponent { get { return _net == null; } }

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

            RefreshAll(); UpdateHud(); ReportUnaskedChoices();
            if (NetApply.EndsTurn(m) || NetApply.IsMulliganDone(m)) NetAfterTurnStart();
            return code;
        }

        /// <summary>`INetBattleHost`：对面投降 ⇒ 本机判胜（`Ctx.ForfeitedBy` 记成对面）。</summary>
        public void NetRemoteResign()
        {
            if (Ctx == null || Ctx.IsOver) return;
            RecRaw(RecKindForfeit, 1 - _me);          // 🆕 录像：投降也是一条要重放的动作
            RuleCore.Forfeit(Ctx, 1 - _me);
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
        //    ⚠️ 还有一处**故意不记**：`AutoEndTurnIfStuck` 走的是**同一条** `EndTurn` 路 ⇒ 记在 ⑥ 那一处就够。
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
        ///    （`TakePick` 队列空时会多抽一次 `ctx.Rng`，见 `EffectResolver.cs:1998`）。</summary>
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

        /// <summary>开局：把「重建这一局要的全部东西」记下来（种子 / 模式 / 双方卡组 / 战场 / 名字 / 先手）。
        /// ⚠️ 收的 `d0`/`d1` 是**座位 0 / 座位 1** 的卡组（`Begin` 那两个参数就是这个口径）。</summary>
        void RecBegin(PlayerDeck d0, PlayerDeck d1, string f0, string f1, int seed, string mode, string arena)
        {
            if (!RecordReplays) { _rec = null; return; }
            LastReplayFile = null;
            _rec = new ReplayRecord
            {
                savedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                mySeat = _me,
                myHero = "", foeHero = "",                 // 结算时补（此刻还没抽出督军）
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

        /// <summary>记一条**非 `AiAction`** 的（换牌 / 投降）。</summary>
        void RecRaw(int kind, int actor, int[] marks = null)
        {
            if (_rec == null) return;
            _rec.actions.Add(new MsgAction { seq = _rec.actions.Count, kind = kind, actor = actor, marks = marks });
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
                    + $"{rec.actions.Count} 条动作 · 本机座位 {pb.MySeat}");

            bool keepRecording = RecordReplays;
            RecordReplays = false;                 // 🔴 **放的时候别录**（否则会把回放自己录成新的一局）
            try
            {
                BeginFromPendingCore(pb, attachNet: false);
                for (int i = 0; i < rec.actions.Count; i++)
                {
                    var m = rec.actions[i];
                    if (m == null) continue;
                    if (m.kind == RecKindForfeit) { RuleCore.Forfeit(Ctx, m.actor); continue; }
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
        /// 重放的是**权威动作流里的每一条**（对面那条翻座位、本机那条不翻）。</summary>
        public void NetReplay(MsgStart start, List<MsgAction> actions)
        {
            bool host = _net != null && _net.IsHost;
            var pb = NetPendingBattle.FromReplay(start, host);
            Debug.Log($"[Net] 重连重放：重建这一局（种子 {pb.Seed}）并重放 {actions.Count} 条动作");
            BeginFromPendingCore(pb, attachNet: false);      // 重建（`Net` 保持挂着）
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
        /// **必须留着** —— `Restart()` 要照原样再来一局，不记的话「按 R 再来一局」就变成自动凑的牌了
        /// （和 `_myFaction` 一个道理：那是「这一局才有」之外的状态，跨局要显式带过去）。</summary>
        PlayerDeck _myDeckSrc, _foeDeckSrc;
        /// <summary>开局那句「本局用的是哪副牌 / 多少张没上场」。提示行空着时显示它（见 `SetHint`）。
        /// 空串 = 没什么要交代的。**这是我们加的** —— 原版没有这一行（原版全卡种都能上场）。</summary>
        string _deckNotice = "";
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
        /// <summary>splash 的淡入/停留/淡出计时（秒）。<b>负数 = 没在播</b>。</summary>
        float _overtimeAnimT = -1f;
        /// <summary>「已经播过一次」——原版 `IsOvertime` 置 true 后不再判，我们也不重播</summary>
        bool _overtimeFired;
        /// <summary>敌方能量数字（原版 `EnemyMana/ManaText`）</summary>
        Label _foeEnergyLabel;
        EndPanel _endPanel;
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
        /// <summary>这局里**敌方督军降到过的最低生命** —— 结算的骷髅数由它算（规则书:36）。
        /// 生命只会往下走（治疗会回，但「首次得到」不回退），所以取最小值就够，不用记历史。</summary>
        int _foeWarlordMinHp = int.MaxValue;
        /// <summary>敌方视角的同一件事：**我方督军降到过的最低生命**。🆕 2026-09-27 加 ——
        /// 只给「对局历史」那条记录算**对面拿了几颗骷髅**用（`Shell/BattleLogData.cs` 的 `EnemySkulls`）；
        /// HUD 上照旧只看 `_foeWarlordMinHp` 那一份（那个 `x N` 显示的是**我们**的里程碑）。</summary>
        int _myWarlordMinHp = int.MaxValue;
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
        /// <summary>卡背：`Cardback` 的 `sizeDelta` 2.1739 × 3.1364，父节点 scale 100 → 217×314 px</summary>
        const float DeckCardPx = 314f;

        /// <summary>🆕 2026-09-26：牌堆那层 **SDF** 的高度（px）。
        ///
        /// 🔴 逐值出处 = 原版预制体 `Cardback Container` 下**两个兄弟节点自己的 sizeDelta**
        /// （`bundle_battleprefabs_vfxandmisc_assets_all/GameObject/`，2026-09-26 实读）：
        ///   · `Cardback`              = **2.1739 × 3.1364** ⇒ @容器 scale 100 = 217.39 × 313.64 px（= 上面那个 314）
        ///   · `Cardback Shadow SDF`   = **2.9212 × 3.8122** ⇒ @100 = **292.12 × 381.22 px**
        ///   · 两个都是 `anchoredPos (0,0)` / `pivot (.5,.5)` ⇒ **同心**，SDF 比卡背大 **1.34376 / 1.21548 倍**
        /// ⚠️ 与「收藏窗卡背格」那处的**倍数不同**（那边 337.5/250 = 1.35、550.8/405 = 1.36）——
        ///    两处各自的 rect 不一样，**别拿一个值当全部**（铁律 5·c）。
        /// 本常量按同一比例从 `DeckCardPx` 推：`314 × 3.8122 / 3.1364 = 381.66`。
        /// 📌 旁证：`资料/战斗UI_原版对账表.md:90` 记的「原版 292×381」与上面逐值吻合。</summary>
        const float DeckSdfPx = DeckCardPx * (3.8122f / 3.1364f);
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

        /// <summary>「能量累积」那盏灯亮不亮 —— **原版判据 = `0 < GameplayVariablesData.manaAccumulation`**
        /// （`BattleManager__SetupBoardPhase.c:181/196` 调 `PlayerManager.SetAccumulationMana(0 < *(int*)(vars+0x34))`；
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
            // 碰撞模块的卡上下文 —— 同样从 `_animfxCtx` 转发（`targetIsWarlord` 用格位判）。
            // ⚠️ **它的另一个钩子 `ColliderLookup` 还没接**（`BattleCollider id → Transform`）。
            //    🔴 更正（2026-09-18）：我一度在这里写「`*FromCamera` 原版怎么定的查不到」—— **是错的**。
            //    查全了：7 个都是**场景里手摆的固定 Transform**（`BattleParticleColliderManager` 的
            //    7 个 `[SerializeField] Transform` 字段，反编译 `GetColliderTransform.c` 逐值对上
            //    `+0x20…+0x50`），坐标已从 `07_场景/battlearena1/` 读出；
            //    而且 **`PlayerWarlordFromCamera` 与 `PlayerWarlord` 坐标完全相同** —— 它不是按相机算的。
            //    差的是**换算**：那是原版 arena 的世界系，要接到我们棋盘得走「格位节距 149.3 px」那座桥，
            //    而且我们战场目前只摆了烘平的背景图 ⇒ 这 7 个碰撞体的对应物要先建出来。
            //    数据与出处见 `资料/AnimFX_实现与接线.md` §八之补。
            //    在那之前让它走 `DroppedPlanes` 计数 + 一次性警告（不静默）。
            WarpforgeVFX.WFModuleCollisions.ContextResolver = m =>
            {
                if (m == null) return;
                m.actingCard = _animfxCtx.actingCard;
                m.targetCard = _animfxCtx.targetCard;
                m.actingIsPlayer = _animfxCtx.actingIsPlayer;
                m.targetIsPlayer = _animfxCtx.targetIsPlayer;
                m.targetIsWarlord = _animfxLastEvent.TargetSlot == RuleEngine.BoardSpec.WarlordSlot;
            };
            // 两条**兵线中心** —— `ScaleByTarget.ChangeShapeAngle` 要它（原版读的是
            // `BattleParticleColliderManager` 的 `playerMinionCollider` / `enemyMinionCollider`）。
            // 🔑 **不另摆空物体**：兵线的定义本来就在 `BoardLayout` 上，`SlotPosition(督军槽)` 就是那个点。
            //   两行中心线相距 0.2241 归一化 × `LayoutSpace.DesignHeight`(10) = **2.241 我们世界单位**
            //   = 原版屏上那 **242 px**（出处 `资料/AnimFX_实现与接线.md` §11.6 d)）。
            WarpforgeVFX.WFModuleScaleByTarget.MinionLines = (out Vector3 pLine, out Vector3 eLine) =>
            {
                pLine = eLine = Vector3.zero;
                if (playerBoard == null || enemyBoard == null) return false;
                pLine = playerBoard.SlotPosition(BoardLayout.WarlordSlot);
                eLine = enemyBoard.SlotPosition(BoardLayout.WarlordSlot);
                return true;
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

        /// <summary>
        /// **按 Play 时的开局**：读卡组编辑器里当前选中的那套 → 开一局。
        ///
        /// 抽成独立方法是为了**自检能走同一条路** —— 批处理下 `AddComponent` 不触发 `Start`，
        /// 不这样的话「卡组库 → 对局」这段连接就永远没被验过（而那正是这一段的意义）。
        /// </summary>
        /// <summary>
        /// 联机局的开局：**照主机那份参数开**（`资料/联机P2P_设计与交接.md` §六 N3）。
        /// 🔴 **两端跑的是同一套绝对座位编号**（主机 = 0 / 客机 = 1）：
        ///   · `Begin(myDeck:, foeDeck:)` 那两个参数**指的是座位 0 / 座位 1 的牌**（不是「我 / 对面」）；
        ///   · 本机是几号由 `SetMySeat` 定 —— 视图那一侧靠 `_me` 自己翻（驱动里 100 处 `_me` 全是相对的）；
        ///   · 先手由主机定（`ForceFirstSeat`，绝对座位），不是本地投硬币。
        /// ⚠️ **第一版是「两端都把自己当 0 号位」（镜像）—— 那条路走不通**：自检跑到第 40 步分叉，
        ///    根因是 `Unstable`（随机自爆）的候选单位表按座位顺序拼、镜像后同一个随机下标选中不同的单位。
        ///    教训全文 → `NetProtocol.Fingerprint` 的注释。
        /// </summary>
        public void BeginFromPendingCore(NetPendingBattle pb, bool attachNet)
        {
            if (pb == null) { Debug.LogError("[Net] `BeginFromPendingCore(null)` —— 不开局"); return; }
            SetMySeat(pb.MySeat);
            _shuffleDecks = !pb.NoShuffle;
            ForceFirstSeat = pb.FirstSeat;
            _noAiMulligan = true;                    // 联机：对面换牌不跑 AI（由主机定序，见 `OnMulliganDone`）
            var vars = GameplayVariables.For(pb.ModeStr == "Skirmish" ? GameMode.Skirmish : GameMode.Classic);
            Debug.Log($"[Net] 联机开局：种子 {pb.Seed} · 模式 {pb.ModeStr} · **本机座位 {pb.MySeat}** · "
                    + $"先手座位 {pb.FirstSeat}（{(pb.FirstSeat == pb.MySeat ? "我" : "对面")}）· 战场 {pb.Arena}");
            Begin(myFaction: pb.Seat0Faction, foeFaction: pb.Seat1Faction, seed: pb.Seed,
                  myDeck: pb.Seat0Deck, foeDeck: pb.Seat1Deck, deckNote: "联机局", vars: vars);
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
        /// </summary>
        void OnDestroy()
        {
            // 🆕 2026-09-27：**摘掉录像那个引擎钩子**（静态回调 —— 不摘的话，下一个场景里
            //    `SimpleAI.Executed` 还指着已销毁的这个 driver）。⚠️ `ctx != Ctx` 那道闸能兜住，
            //    但静态回调该摘就得摘。
            SimpleAI.Executed = null;
            if (_net == null) return;
            if (NetRuntime.Instance != null) NetRuntime.Instance.LobbyHandled = true;
            NetMatchmaking.Reset();
            Debug.Log("[Net] 离开战场：大厅消息处理权已还给 `NetRuntime`，联机匹配状态已清");
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
            // 🆕 2026-09-26（N3）：**联机局** —— 主机算好的那份开局参数在等着（`NetBattle` 放进去的）
            //    ⇒ 整条走它：种子/两副牌/谁先手/战场全是主机定的，本地一样都不许自己算。
            //    判据 → `资料/联机P2P_设计与交接.md` §六 N3/N4。
            var pbNet = NetPendingBattle.Take();
            if (pbNet != null) { BeginFromPendingCore(pbNet, attachNet: true); return; }

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
            Debug.Log($"[Battle] 本局模式：{vars.deckSize} 张（{(vars.IsSkirmish ? "遭遇 Skirmish" : "经典 Classic")}）"
                    + $"· 卡组 {(saved != null ? "「" + saved.Name + "」" : "（自动凑）")}");
            // 🆕 2026-09-26：**本局的种子必须每局都不一样** —— 否则「投硬币决定先后手」是假的：
            //   原来这条没传 `seed` ⇒ 用的是 `Begin` 的**默认常量** `20260911` ⇒ **每一局的硬币都落在同一面**
            //   （玩家永远同一边；实测自检里就是「P1 恒先手」）。原版那枚硬币是**每局现抽**的
            //   （`SearchOpponentManager.StartBattle` 抽完写进 `MatchData.playerGoesFirstRandomInt`，
            //    PvP 里再经 Photon 同步 ⇒ 两端同一枚）。
            //   ⚠️ **对局仍可复现**（工程红线）：种子**打进日志**，照它重开就是同一局。
            //   ⚠️ 自检可以钉住它（`ForceSeed`）—— 见那个字段的注释。
            int seed = ForceSeed ?? unchecked((int)(System.DateTime.Now.Ticks & 0x7FFFFFFF));
            Debug.Log($"[Battle] 本局种子 {seed}（记下来就能复现这一局 —— **谁先手由它决定**）");
            Begin(seed: seed, myDeck: saved, deckNote: note, vars: vars);
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
            note = lib.LastError;      // 「还没编过」不算失败：那时 LastError 是 null，库也是空的
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
        /// <param name="deckNote">卡组**读不出来**时的人话（`PickSavedDeck` 的 note）。null = 没这回事。</param>
        public void Begin(string myFaction = null, string foeFaction = null, int seed = 20260911,
                          PlayerDeck myDeck = null, PlayerDeck foeDeck = null, string deckNote = null,
                          GameplayVariables vars = null)
        {
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

            // 🆕 2026-09-26：**牌数与模式对不上就出声**（不许静默失败）。
            //   会撞上的场景：一副 12 张的遭遇牌被当成经典开（牌库两回合抽干、看起来像 bug）。
            //   ✅ 2026-09-26 起**两条路都能带模式**了：预组副（`PrebuiltDecks.ToPlayerDeck` 抄了 `gameMode`）
            //      与玩家自建副（`PlayerDeck.GameMode` + 落盘）—— **判据只有一个：这副牌自己**。
            //      所以这条守卫现在是**真的异常**（不是「那条路还没做」）。
            if (myDeck != null && myDeck.CardIds != null && myDeck.CardIds.Count != _vars.deckSize)
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
            var myCards = ResolveDeck(PoolFor(pool, _myFaction), _myFaction, myDeck, seed + 1, "我", out myNotice);
            var foeCards = ResolveDeck(PoolFor(pool, _foeFaction), _foeFaction, foeDeck, seed + 2, "对手", out _);
            // ⚠️ 2026-09-12：`StarterDeck` 现在**会混进能打的战术卡**（原来那开关没实现，自动凑的牌
            //    一张战术都没有，实战里永远看不到战术）。要退回「只有单位卡」就把 `ResolveDeck`
            //    里那一处传 `unitsOnly: true`。

            // 提示行只说**我方**那副 —— 对手那副是自动凑的，不用跟玩家交代
            _deckNotice = myNotice;
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
            int firstSeat = ForceFirstSeat ?? FirstSeatForSeed(seed);
            Ctx = RuleCore.NewBattle(myCards, foeCards, seed, shuffle: _shuffleDecks, cardPool: pool,
                                     openMulligan: mulliganEnabled, vars: _vars, firstSeat: firstSeat);
            Debug.Log($"[Battle] 谁先手：{Ctx.Players[firstSeat].Name}（**投硬币**决定的 —— 用户 2026-09-26 拍板："
                    + "一律投硬币，等价于「所有督军的 `initiative` 相同」；原版那条顺序见 `资料/加时与冲突模式_原版规格.md` §2.8）");

            // 🆕 2026-09-27：**开局就把录像的头记下来**（种子 / 模式 / 双方卡组 / 先手）——
            // 必须在 `NewBattle` 之后（先手是它定的）。⛔ 别挪到 `Begin` 开头：那时 `Ctx.FirstSeat` 还没有。
            RecBegin(myDeck, foeDeck, myFaction, foeFaction, seed,
                     _vars != null && _vars.IsSkirmish ? "Skirmish" : "Classic",
                     UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            // 🆕 2026-09-27（录像）：**AI 那半挂到引擎边界上**（`SimpleAI.Executed`）——
            //   AI 的动作有两个入口（产品的 `NextAction`+`ExecuteAction`、自检/兼容层的 `PlayTurn`），
            //   挂在调用点会漏掉一半。⚠️ 只记「对面那一侧」的动作（`ctx.Active != _me`），
            //   免得哪个自检替玩家走路时把玩家的动作也记成对面的。
            SimpleAI.Executed = OnAiExecuted;

            BuildHud();

            // 落点合法性**由这里说了算** —— 表现层只问这一个委托
            interaction.CanDropAtSlot = (slot, card) =>
            {
                if (Ctx == null || Ctx.IsOver) return false;
                if (Ctx.Active != _me) return false;                 // 对手回合不能出牌
                int idx = HandIndexOf(card);
                if (idx < 0) return false;
                return RuleCore.CanPlayCard(Ctx, _me, idx, slot) == RuleCodes.OK;
            };
            // 战术卡能落到**敌方半场**（`Deal 3 damage to an enemy` 打的就是敌方单位）——
            // 不接这个引用的话，敌方目标的战术卡拖过去一律弹回来
            interaction.foeBoard = enemyBoard;
            // ⚠️ 先 `-=` 再 `+=`：`Begin()` 会被调多次（重开一局），不清的话每开一局就多挂一份，
            //    落位回调会跑 N 遍（第二遍起 `HandIndexOf` 找不到牌、还会报错刷屏）。
            interaction.OnDeployed -= OnCardDeployed;
            interaction.OnDeployed += OnCardDeployed;
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
            ResetClock();                  // 第 1 回合的表也得上（原版 `ClockManager.StartTimer`）
            RefreshAll();
            UpdateHud();
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
        /// ⚠️ 只能在这里调 `ClickedThisFrame()` —— 它是 latch（按一次只算一次），
        ///    在 Update 里先调一次，回合那段就再也收不到点击了。所以：
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
                if (ClickedThisFrame() && !captured) SettingsClickAt(WorldPointer());
                return true;
            }
            if (_settingsBtn == null) return false;
            if (!_settingsBtn.Contains(WorldPointer())) return false;
            if (ClickedThisFrame()) _settingsPanel.Show();
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
            if (_settingsPanel.HitDifficulty(w)) { CycleAiDifficulty(); return true; }
            if (_settingsPanel.HitClose(w)) { _settingsPanel.Hide(); return true; }
            return true;      // 点面板别处：吃掉（不穿透到棋盘），但不做事
        }

        /// <summary>🆕 2026-09-29（§25）：HUD 那颗**进攻卡（环境）**钮 —— 点它**只弹展示窗**。
        /// 原版两个入口（`BattleHud.OffensiveButtonClicked` / `DisplayOffensiveCards`）转的是**同一个**
        /// `BattleManager.DisplayOffensiveCard()` → `CardDisplayWindow.ShowCard(..., showOptions:false, ...)`：
        /// 里面只有卡面/相关卡/文本，**不改任何战场状态、也不发网络包**。
        /// 🔴 **别把它做成「点了就换环境」** —— 那会变成我们的设计（判据 → 判据文件 §三 第 25 条）。
        /// ⚠️ 展示窗要的是 `CardDef`，而**进攻卡不在我们的卡池里**（费用/效果文字在远端 CCD）⇒
        ///    这一步**如实出声**并停在这儿，「把进攻卡喂进展示窗」记成待办（不静默）。</summary>
        bool HandleOffensiveButton()
        {
            if (_offensiveBtn == null || !_offensiveBtn.gameObject.activeSelf) return false;
            if (!_offensiveBtn.Contains(WorldPointer())) return false;
            if (!ClickedThisFrame()) return false;
            Debug.LogWarning("[Battle] 点了进攻卡钮：原版这一步只弹**展示窗**（`DisplayOffensiveCard`，"
                           + "`showOptions:false`，**不换环境**）—— 但进攻卡不在卡池里（名称/费用/效果文字在远端 CCD）"
                           + " ⇒ 展示窗这一步**还没接**（如实出声）。"
                           + $"这一局选的环境 = `{Ctx.OffensiveEnvSO}`");
            return true;
        }

        /// <summary>`ChatPopup` 的点击。规矩和设置面板一样：
        /// **开着 ⇒ 无条件接管（模态）**；关着 ⇒ **只在指针落在 `ChatButton` 上**才接管。
        /// （`ClickedThisFrame()` 是 latch，只能在这里耗一次 —— 见 `HandleSettings` 上面那段注释。）</summary>
        bool HandleChatPopup()
        {
            if (_chatPopup != null && _chatPopup.Visible)
            {
                if (ClickedThisFrame()) ChatClickAt(WorldPointer());
                return true;
            }
            if (_chatBtn == null || !_chatBtn.Contains(WorldPointer())) return false;
            if (!ClickedThisFrame()) return false;
            if (_chatCooldown > 0f)
            {
                // 原版冷却中按钮 `interactable = false`（按不动）—— **说出来**，别静默吞掉
                Debug.Log($"[Battle] `ChatButton` 冷却中（还剩 {_chatCooldown:F1}s）—— 原版也是按不动");
                return true;
            }
            _chatPopup.Show();
            return true;
        }

        /// <summary>一次点击落在 `ChatPopup` 上 —— **真实输入与自检走同一条判定**
        /// （自检拿钮的世界坐标喂进来，不直接调 `SpeakChat`）。
        /// 返回「这一下被面板吃掉了没有」。</summary>
        public bool ChatClickAt(Vector3 w)
        {
            if (_chatPopup == null || !_chatPopup.Visible) return false;
            int hit = _chatPopup.SetPointer(w, true);
            if (hit < 0) return true;         // 面板外 ⇒ 面板自己关掉了（原版那条全屏关闭区）
            if (SpeakChat(hit)) _chatCooldown = ChatPopupPanel.Cooldown;   // 原版说完进 4 秒冷却
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

            _unitChat.Speak(_me, w.Card.Id, "Chat" + idx, ArtKey(w.Card), w.Card.NameZh, text, clip, file);
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
                            ArtKey(card), card.NameZh, text, clip, file);
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
            _unitChat.Speak(who == _me ? 0 : 1, card.Id, "Concede", ArtKey(card), card.NameZh, text, clip, file);
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
            _unitChat.Speak(0, card.Id, "CantDo", ArtKey(card), card.NameZh, text, clip, file);
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
            _unitChat.Speak(0, card.Id, "Hurry", ArtKey(card), card.NameZh, text, clip, file);
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

        /// <summary>回放条的一次点击。返回 true = 这一帧到此为止（和设置面板同一个形状）。</summary>
        bool HandleReplayBar()
        {
            if (_replayBar == null || !ClickedThisFrame()) return false;
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
                if (ClickedThisFrame()) { _logPanel.Hide(); HideLogCard(); }
                return true;
            }
            if (_cemeteryBtn == null) return false;
            if (!_cemeteryBtn.Contains(WorldPointer())) return false;
            if (ClickedThisFrame()) ShowBattleLog();
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
        /// `BattleManager.<MulliganCountdown>` 从 `globalVars + 0x28` 取（`_MulliganCountdown_d__347__MoveNext.c:38`），
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
        ///   · **`< 10` 秒**才把剩余秒数写到「完成换牌」那颗钮上（`MulliganManager.SetMulliganTimer`）
        ///   · **`< 1` 秒**自动完成 —— 原版调 `MulliganManager.ProcessMulliganDone()`，**等价于玩家点完成**
        /// ⚠️ 格式串**已查到**（2026-09-17）：`SetMulliganTimer` = `String.Concat("0:0", 秒数)`
        /// ⇒ 最后十秒按钮上写 **`0:09`…`0:01`**（前缀写死 `0:0`，这也正是阈值取 `<10` 的原因）。
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
            if (ClickedThisFrame()) _mulligan.HandleClick(WorldPointer());
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
                if (_mulligan != null) _mulligan.SetDoneText("等待对手…");
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
            ResetClock();
            RefreshAll();
            // 🆕 2026-09-18：**开局独白**（原版 `BattleManager.StartBattlePhase` 末了起的
            // `ShowHeroesIntroMessage` 那条协程）。放在 `BeginTurn` 之后 —— 原版就是在这个位置。
            StartIntroMonologue();
            // 🆕 2026-09-29（§25）：进攻卡**每场只生效一次**，就在这儿（原版
            // `_ApplyOffensiveAndDefensiveEffects` 的唯一调用点 = `FinishMulliganFinalPhase`，
            // **不在回合结算里** —— 别写成「每回合」）。
            ApplyOffensiveEnvOnce();
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
            _offensivePhaseDone = true;                  // 一局只来一次
            if (!offensivePhaseEnabled) return false;    // 自检关掉它（见那个字段的注释）
            if (!OffensiveCards.Available) return false;

            // ⚠️ **联机局先跳过并出声**：原版是两台各弹各的（没有同步包），我们这条同步链**还没做**。
            if (_net != null)
            {
                Debug.LogWarning("[Battle] 联机局：**进攻卡那一段还没接同步**（原版两台各弹各的面板）"
                               + " ⇒ 这一局整个跳过，环境按默认。待办 → `项目任务.md` §三 第 25 条");
                return false;
            }

            // ① **AI 那一侧先定**（原版 `AI.GetAiEnvEffectCard`：均匀随机，可能抽到空卡）
            if (Ctx.FirstSeat != _me) PickOffensiveForAi(Ctx.FirstSeat);
            if (Ctx.SecondSeat != _me) PickDefensiveForAi(Ctx.SecondSeat);

            // ② 本机那一侧：开面板
            if (Ctx.FirstSeat == _me)
            {
                var list = OffensiveCards.Choices(_myFaction);
                if (list.Count < 2) return false;         // 原版：列表 <2 张时**不弹菜单**，直接取 list[0]
                var views = new List<CardView>();
                for (int i = 0; i < list.Count; i++)
                    views.Add(CardView.Create(_choosePanel.transform, OffensiveCardData(list[i], _myFaction),
                                              "Offensive_" + i));
                _choosePanel.OnDone = OnOffensiveDone;
                _choosePanel.Open(views, "选择进攻卡");   // ⚠️ 文案是**我们的**：原版词条在远端本地化表
                SetHint("选择进攻卡（先手）—— 选完点「继续」");
                return true;
            }

            // ③ 本机是**后手**：原版这里弹的是「选防御卡」（3 张、无空卡）。
            Debug.LogWarning("[Battle] 本机是先手？不是 —— 「选防御卡」那半边**还没做**"
                           + "（数据侧只普查了进攻卡；原版那条链见 `资料/加时与冲突模式_原版规格.md` §2.7c）"
                           + " ⇒ 这一局跳过，不弹面板（如实出声，不静默）");
            return false;
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
            var list = OffensiveCards.Choices(_myFaction);
            int i = (picked != null && picked.Count > 0) ? picked[0] : 0;   // 原版：没选就按空卡（下标 0）
            if (i < 0 || i >= list.Count) i = 0;
            var c = list[i];
            RuleCore.ChooseOffensiveCard(Ctx, _me, c.idx, EnvSOFor(c, _myFaction));
            _choosePanel.Close();
            BeginBattleAfterSetup();
            SetHint("");
        }

        /// <summary>AI 那一侧的进攻卡：**均匀随机**（原版 `AI.GetAiEnvEffectCard`，`Random` 那一路可能抽到空卡）。</summary>
        void PickOffensiveForAi(int seat)
        {
            var list = OffensiveCards.Choices(FactionOf(seat));
            if (list.Count == 0) return;
            int i = Ctx.Rng.Next(list.Count);
            var c = list[i];
            RuleCore.ChooseOffensiveCard(Ctx, seat, c.idx, EnvSOFor(c, FactionOf(seat)));
        }

        /// <summary>AI 那一侧的防御卡（原版：**随机取一张**；空列表就写空串）。
        /// ⚠️ 我们**没有防御卡那三张**的数据（只普查了进攻卡）⇒ 记一个「未选」并出声。</summary>
        void PickDefensiveForAi(int seat)
        {
            Debug.Log($"[Battle] AI（P{seat + 1}）是后手 ⇒ 防御卡那一段**我们还没数据**（原版是随机取一张）。"
                    + "如实记「未选」，不影响进攻卡那一半。");
            RuleCore.ChooseDefensiveCard(Ctx, seat, -1);
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

        /// <summary>把选定的进攻卡**生效一次**（原版 `_ApplyOffensiveAndDefensiveEffects`：
        /// reveal 那张卡 → 2 s → 送去坟场（1 s）→ `ApplyEnvEffect` → 非空卡再等 4 s）。
        /// ⚠️ **战场那一半（换雾/环境光/环境 prefab）归【战场场景线】**（`项目任务.md` §三 第 30 条）
        /// —— 这里只把「该切到哪条环境 SO」算出来交给它，自己**不碰战场**。 </summary>
        void ApplyOffensiveEnvOnce()
        {
            if (!Ctx.OffensiveChosen) return;
            string so = Ctx.OffensiveEnvSO;
            // 🆕 那颗钮的显隐（原版 `d__337:146-155`：判据 = 选定卡 id ≠ 空卡 id ⇒ 才 `SetActive(true)`）
            bool useCard = Ctx.OffensiveSlotIdx >= 0;
            if (_offensiveBtn != null) _offensiveBtn.gameObject.SetActive(useCard);
            if (!useCard)
            {
                Debug.Log("[Battle] 进攻卡：这一局选的是**不使用进攻卡**（`Normal Conditions` 那一张）"
                        + " ⇒ HUD 那颗钮**不出现**（原版 `isEmptyOffensiveCard` 那条判据），环境也不动");
                return;
            }
            // 🆕 2026-09-30：**战场侧接上了**（§三 第 30 条 · 4 环境）。
            // 判据 = 原版 `ApplyEnvironment(so, instant:false)`：同一条不重播 · 补间雾与环境光混合 ·
            //        `scenarioObjects` 有值就 `Instantiate(prefab, Camera.main.position/rotation)`。
            // ⚠️ 仍缺的那一半（**如实**）：原版 prefab 上那族 `IScenarioEnvironmentBlendeable`
            //    （按 filter 分组的淡入淡出）**我们一个都没复刻** ⇒ 现在只能整份 prefab 一起上。
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
            _unitChat.Speak(side, w.Card.Id, "Intro", ArtKey(w.Card), w.Card.NameZh, text, clip, file);
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
        // 全池 **62 张**卡会走到那里（普查：`资料/选牌_受影响卡普查.md`）。
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

        CardView _pendingView;                                   // 面板问完之后要打出的那张牌
        int _pendingIdx = -1, _pendingSlot = -1;
        /// <summary>
        /// 🆕 第 7 行第 4 步：正在被问的**那一份**（面板可能开着好几帧，手牌中途会变 ——
        /// 只握下标的话，变一次就指到**另一张**上了）。
        /// </summary>
        CardInstance _pendingInst;
        List<EffectOp> _pendingAsks = new List<EffectOp>();      // 这张卡里要问玩家的那几处（按结算顺序）
        int _pendingAsk;
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

        /// <summary>面板开着时**吃掉这一帧的输入**（和换牌那条同一个规矩）</summary>
        bool HandleChoose()
        {
            if (_choosePanel == null || !_choosePanel.Visible) return false;
            if (ClickedThisFrame()) _choosePanel.HandleClick(WorldPointer());
            return true;
        }

        /// <summary>玩家要出一张牌 —— **先把该问的问完**，再真的打出去。</summary>
        void BeginPlay(CardView card, int idx, int slot)
        {
            var asks = RuleCore.PlayerChooseOps(Ctx.Players[_me].Hand[idx].Card);   // 第 7 行第 2 步：手牌存实例
            if (asks == null || asks.Count == 0) { DoPlay(card, idx, slot); return; }

            _pendingView = card; _pendingIdx = idx; _pendingSlot = slot;
            _pendingInst = idx >= 0 && idx < Ctx.Players[_me].Hand.Count ? Ctx.Players[_me].Hand[idx] : null;
            _pendingAsks = asks; _pendingAsk = 0;
            Ctx.ResetChoices();
            ShowAsk();
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
        ///     **`List<RawCardScript>`** —— 选效果那些选项在原版里**就是卡**。
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
                if (RuleCore.ChooseEffectIsHand(op))
                {
                    // `Infinite Biomorphologies` 的「给手牌里的全部部队」**这一版没做**
                    // ⇒ 开了面板也没用（引擎那边照样如实报）。见 `DoChooseEffect` ②。
                    Ctx.Log("（选牌面板：这一处是「**给手牌里的全部部队**」—— 这一版没做，不问了）");
                }
                else
                {
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
            _choosePanel.Open(_chooseViews, title);
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

            var v = _pendingView; int s = _pendingSlot;
            // 🔴 第 7 行第 4 步：真打出去之前**按那一份重算下标** ——
            //    面板开着的这段时间里手牌可能变过（抽牌/弃牌/效果改手牌），沿用旧下标会打错牌。
            int i = (_pendingInst != null) ? Ctx.Players[_me].Hand.IndexOf(_pendingInst) : _pendingIdx;
            if (i < 0) i = _pendingIdx;
            _pendingView = null; _pendingIdx = -1; _pendingSlot = -1; _pendingInst = null;
            _pendingAsks = new List<EffectOp>(); _pendingAsk = 0;
            ClearChooseViews();
            DoPlay(v, i, s);
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
        /// <summary>自检用：称号文字**量出来**的实际字号（画布 px）。原版 `m_fontSize` = 30.55。</summary>
        public float TitleFontPxNow(bool mine)
        {
            var l = mine ? _titleMe : _titleFoe;
            return l != null ? l.FontPxNow : 0f;
        }
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

        /// <summary>
        /// 把引擎**留档的**战斗日志（`Ctx.ActionLog`）翻成人话喂给面板 —— **新的在前**
        /// （原版就是从最新一条往下排）。卡名走 `Zh` 翻中文，查不到就原样显示英文（不静默丢）。
        ///
        /// ⚠️ 目标卡名必须**读事件里记下来的那个**（`BattleEvent.TargetCardId`）——
        ///    留档以后再回看时，那个格位早就换人了，去棋盘上查会查到错误的对象。
        /// </summary>
        void RefreshBattleLog()
        {
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

        /// <summary>卡名 → 中文名。卡池里没有就**原样返回英文**（不静默丢成空串）</summary>
        string Zh(string name)
        {
            if (string.IsNullOrEmpty(name) || _pool == null) return name ?? "";
            var d = CardDatabase.Find(_pool, name);
            return (d != null && !string.IsNullOrEmpty(d.NameZh)) ? d.NameZh : name;
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
        /// ⚠️ 卡池里找不到就**出声、不弹**（不静默、也不弹一张空卡 —— 空卡会走 `CardView` 的「空卡位」分支）。</summary>
        bool ShowLogCard(string key)
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
            _logCard.SetPose(_logPanel.HoverCardLocalPos(BattleLogPanel.ZHoverCard), 0f, scale);
            _logCardKey = key;
            return true;
        }

        void HideLogCard()
        {
            if (_logCard != null) _logCard.gameObject.SetActive(false);
            _logCardKey = null;
        }

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
            RuleCore.Forfeit(Ctx, _me);
            // 🆕 2026-09-26（N4）：联机局要把「我投降了」发对面（对面收到后 `Forfeit(ctx, 对面)`）
            if (_net != null) _net.OnLocalResign();
            SpeakConcede(_me);        // 认输也有台词（原版 `concede` 那一族）
            RefreshAll();
            UpdateHud();
        }

        /// <summary>
        /// 重开一局（结算面板上那句「按 R 再来一局」就是它）。
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
                Debug.LogWarning("[Net] 联机局收到「按 R 再来一局」—— **拒绝**（两端会打岔）；"
                               + "要重开得两边都退回菜单再连一次");
                return;
            }
            if (_endPanel != null) _endPanel.Hide();     // 上一局的结算面板先收掉（HUD 复用，不清会叠着）
            Begin(_myFaction, _foeFaction, _seed + 1, _myDeckSrc, _foeDeckSrc, vars: _vars);
        }

        /// <summary>🆕 2026-09-26：**本局参数**（经典 / 遭遇…）。逐字段见 <see cref="GameplayVariables"/>。
        /// 由 `Begin` 写入、`Restart` 原样带过去；`Ctx.Vars` 就是它。</summary>
        GameplayVariables _vars = GameplayVariables.Classic;
        /// <summary>本局参数（自检/HUD 读用）。</summary>
        public GameplayVariables Vars { get { return _vars; } }

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
        /// ⚠️ 语义：`ForceFirstSeat = 0` ⇒ **我方（座位 0）先手**。</summary>
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
                //（`Terminator` / `Bladeguard Veteran` …），只按名字查会撞上**另一个阵营**那张，
                //    于是 `Validate` 判 `WrongFaction`、**一副合法卡组被打回自动凑**（静默降级）。
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
                    Debug.LogError($"[Battle] {who}的卡组「{saved.Name}」不合法（{DeckRules.Describe(err)}）");
                    notice = $"你的卡组「{Short(saved.Name, 14)}」不合法（{DeckRules.Describe(err)}）"
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

        /// <summary>手牌被**轻点**了（按下→松开几乎没动）。原版这个动作就是开关卡牌展示窗。</summary>
        void OnCardTapped(CardView card)
        {
            if (_cardDisplay == null || card == null) return;
            // 同一帧里刚被「点遮罩空白」关掉 ⇒ 这一下**不再开**：两个来源读的是同一次鼠标
            //（`CardInteraction` 自己读、不经过这里），不掐的话表现就是「点了没反应 / 一闪」。
            if (SameFrame(_cardWinClosedFrame)) return;
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
        /// 返回 true = 这一下被窗吃掉了（别往下走）。四处落点照原版 `CardDisplayWindow`：
        /// ① **语音钮**（`voiceOverButton`）② **眼睛钮**（`showCardTextButton`）③ **卡格** ⇒ 换位
        /// （点前台那张 = 原版闸② 「什么都不做」，但**这一下也要吃掉** —— 原版那张卡自己的
        /// `UI Collider` 会把点击挡住）④ **遮罩空白 ⇒ 关窗**（原版 `BackgroundCloseButton` /
        /// `OnBackgroundClick`；🆕 **2026-09-29 接上**，原来记的是「我们没接」）。</summary>
        public bool HandleDisplayWindowClick(Vector3 wp)
        {
            if (_cardDisplay == null || !_cardDisplay.Visible) return false;
            if (_cardDisplay.HitVoice(wp))
            {
                if (!_cardDisplay.PlayVoice())
                    Debug.Log("[Battle] 「放大窗·语音」这张卡没有单位语音 —— **没播**（不静默失败）");
                return true;
            }
            if (_cardDisplay.HitEye(wp)) { _cardDisplay.ToggleLore(); return true; }
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
            // 战术卡：**不落格位** —— 它打出去就没了（效果已经结算完），视图直接销毁。
            // 单位卡才走下面「从手牌变成场上单位」那条路。
            bool tactic = !card.Data.isUnit;
            // 🆕 2026-09-26（N4）：联机局里这条动作要能发对面 ⇒ 走 `LocalAct`（单机下与老代码一字不差）
            var playAct = new AiAction { Kind = AiActionKind.PlayCard, HandIdx = idx, Slot = slot };
            int code = LocalAct(playAct, () => RuleCore.PlayCard(Ctx, _me, idx, slot));
            if (code != RuleCodes.OK)
            {
                Debug.LogError($"[Battle] 引擎拒绝了这次落位（{RuleCodes.Describe(code)}）—— "
                             + "校验委托和实际出牌用的不是同一份判据");
                return;
            }
            _cardsPlayedThisTurn++;             // 本回合已出牌数（原版 `CardsPlayedInTurn1..3` 那三枚灯）

            _handViews.Remove(card);
            if (tactic)
            {
                Kill(card.gameObject);          // 批处理下 Destroy 不生效，`Kill` 会走 DestroyImmediate
                RefreshAll();
                UpdateHud();
                ReportUnaskedChoices();
                AutoEndTurnIfStuck();
                return;
            }

            // 这张卡从手牌变成场上单位：视图也搬过去，别重建（重建会丢落位动画）
            card.SetData(ToCardData(_ctx_CurrentUnit(slot), _myFaction));
            // 🔴 **换展示场景**：出牌是「搬视图」不是「重建视图」⇒ 出生时是**手牌那一套**
            //    （卡框/费用/宝石/卡名/效果文字）。场上按原版只有立绘+数值+徽标，这一步少不了 ——
            //    漏了的话「自己打出去的兵在场上仍带着卡框」而督军是对的（督军出生就在场上）。
            card.SetFace(CardFace.Board);
            _myUnits[slot] = card;
            card.transform.SetParent(boardRoot, true);

            // 登场特效**不在这儿播** —— 引擎在 `PlayCard` 里已经发了一条 Deploy 事件，
            // 下面这次 `RefreshAll()` 会把它翻译成特效（表现层只有那一个出口）。
            // 这样 AI 出的牌也带着卡名，两边走的是同一条路。
            RefreshAll();
            UpdateHud();
            ReportUnaskedChoices();
            AutoEndTurnIfStuck();
        }

        /// <summary>
        /// 结算完如实报「有几次选择是**引擎替玩家挑的**」（`ChooseSites &gt; ChooseAnswered`）。
        /// 🔴 **这件事不报错** —— 不说的话玩家会以为那个面板把该问的都问了（本工程的静默失败红线）。
        /// ✅ 2026-09-14：四族（`choosecard` / `chooseone` / `chooseeffect` / `become`）**都有面板了**，
        ///    剩下会漏的还是「**ask 点不在被问的那张卡 desc 里**」的那些
        ///    （事件层的监听正文、`When …` 之类 —— 见 `EffectResolver.PlayerChooseOps` 的注释）。
        /// </summary>
        void ReportUnaskedChoices()
        {
            if (Ctx == null) return;
            int missed = Ctx.ChooseSites - Ctx.ChooseAnswered;
            if (missed <= 0) return;
            Ctx.Log($"⚠️ 这次结算里有 **{missed} 处选择是引擎替你挑的**"
                  + $"（本该问 {Ctx.ChooseSites} 处、面板问了 {Ctx.ChooseAnswered} 处）");
        }

        UnitState _ctx_CurrentUnit(int slot)
        {
            return Ctx.Players[_me].Board[slot];
        }

        // ==================================================================
        //  每帧
        // ==================================================================

        void Update()
        {
            if (Ctx == null) return;
            NetTick();          // 🆕 2026-09-26（N4）：联机局收包 + 落地对面的动作（自检里显式调 `NetTick`）

            // 🆕 2026-09-20 加时：引擎一旦把 `IsOvertime` 置真就播一次。
            // 原版那道 `if (!IsOvertime)` 闸决定了**只播一次**（`BattleManager._NextTurn`）。
            if (Ctx.IsOvertime && !_overtimeFired) ShowOvertime();
            TickOvertime(Time.deltaTime);   // 批处理下 deltaTime = 0 ⇒ 自检直接调 `TickOvertime`

            // 🆕 2026-09-20 悬停信息层（原版 `EverguildTooltipTrigger` 挂在卡面数值容器与 HUD 计数上）
            TickTooltip();

            // 🆕 2026-09-29 战斗日志：悬停行内卡名 ⇒ 弹一张卡（原版 `CemeteryManager.CheckCardLink`）
            TickLogCard(WorldPointer());

            // 多张展示窗开着 ⇒ **先吃掉点击**（原版 `UIMultiCardDisplay`：`Continue` 与背景都能关）。
            // ⚠️ 排在回放条**之前** —— 窗开着的时候它就是最上面那一层。
            if (_multiCards != null && _multiCards.Visible)
            {
                bool tapped = ClickedThisFrame();
                Vector3 wp = WorldPointer();
                bool onDeck = HitMyDeckPile(wp);
                if (tapped) _multiCards.Hide();
                UpdateHud();
                if (tapped && onDeck) ShowMyDeck(true);   // 点牌堆本身 = 关掉（别立刻又开一次）
                return;
            }

            // 🆕 2026-09-29：放大窗 —— **指针移开就关**（原版 `CardCollider.OnPointerExit` →
            //    `CardScript.OnTouchExit`，唯一守卫是 `displayingCardFlag`；原来我们走的是「再点一下关」）。
            //    判据只此一处，见 `TickCardWinPointerExit`。
            if (TickCardWinPointerExit(WorldPointer())) return;

            // 放大窗开着时：**这一下点击先交给窗**（卡格换位 / 语音钮 / 眼睛钮 —— 判据只此一处）
            if (_cardDisplay != null && _cardDisplay.Visible && ClickedThisFrame()
                && HandleDisplayWindowClick(WorldPointer()))
                return;

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
                UpdateHud();
                // 结算面板上写着「按 R 再来一局」—— 那句话原来**没有任何代码接**
                //（2026-09-12 发现的：面板承诺了一件事，什么都没发生）。补上。
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
            if (HandleBattleLog()) { UpdateHud(); return; }

            // 暂停时：面板照常能开（上面两条），但时钟与两个回合的驱动都停
            if (!_replayPaused)
            {
                TickClock(Time.deltaTime);

                if (Ctx.Active == _me) DrivePlayerTurn();
                // 🔴 **联机局：对面那一侧**绝不能**跑 AI** —— 那是网络的活（`NetTick()` 把对面对作落地）。
                //    不拦这一条的话，AI 会和网络**同时**给对面出招 ⇒ 两边立刻打岔。
                else if (AiShouldDriveOpponent) DriveAiTurn();
            }

            UpdateHud();
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

            // ① **选择器开着**：只做两件事 —— 喂指针（压着谁就放大 1.3 倍 + 亮黄圈）、点一下定下来
            if (selector != null && selector.Visible)
            {
                selector.UpdatePointer(world);
                if (ClickedThisFrame())
                {
                    var hot = selector.Hovered;
                    if (hot != AttackKind.None) CommitCommand(hot);
                    else if (!selector.ContainsBar(world)) ClearSelection();   // 点槽外 = 取消
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
            if (BoardPress(world, PointerDown())) return;

            // ③ 正在选目标 → **每帧**把准星挪到指针压着的那个合法目标上（原版「我现在指着谁」）
            //    放在 `ClickedThisFrame` 之前 —— 它是持续反馈，不是只在点击那一下更新
            if (_selectedSlot >= 0 && _command != AttackKind.None)
            {
                UpdateReticle(world);
                // 技能卡面板：指针按在面板上会铺蓝色那层（原版 `LightPressed`）
                if (skillPanel != null && skillPanel.Visible) skillPanel.SetPointer(world, PointerDown());
            }

            if (!ClickedThisFrame()) return;

            // ③′ 点**我方牌堆** → 把牌库摊开看（原版这个窗由 `BattleManager.ResolveAction` 打开 ——
            //     环境卡 / 战绩卡组，我们还没有那两条流程 ⇒ **入口接在牌堆上，这是我们挑的**）
            if (HitMyDeckPile(world)) { ShowMyDeck(true); return; }

            // ③ 结束回合按钮（有原版按钮底图就按图判，没有就按文字判）
            bool onEndTurn = _endTurnBg != null ? _endTurnBg.Contains(world)
                                                : (_endTurnLabel != null && _endTurnLabel.Contains(world));
            if (onEndTurn)
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

            // ⑤ 点棋盘上的单位 → **先记下来**（松手照上面 ② 分流：轻点开大卡窗 / 拖够弹选择器）
            int pick = HitSlot(_myUnits, world);
            if (pick >= 0) { _pressSlot = pick; _pressSide = _me; _pressWorld = world; return; }
            //    对手单位：原版棋盘段**没有敌我判断**（唯一的 `isPlayer` 守卫在**手牌段**）⇒ 也能开窗；
            //    但拖不动（`OnTouchDrag` 的 `isPlayer` 闸）⇒ 只走轻点那条（② 里按 `side` 判）
            int foePick = HitSlot(_foeUnits, world);
            if (foePick >= 0) { _pressSlot = foePick; _pressSide = 1 - _me; _pressWorld = world; }
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

        static bool PointerDown()
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
                var wsAct = new AiAction { Kind = AiActionKind.CollectWaystone, Slot = slot };
                int rc = LocalAct(wsAct, () => RuleCore.CollectWaystone(Ctx, _me, slot));
                if (rc != RuleCodes.OK) SetHint(RuleCodes.Describe(rc));
                return;
            }

            if (u.Exhausted) { SetHint(CardText.Phrase("THIS UNIT ALREADY ACTED")); return; }
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
        /// </summary>
        string AltActionOf(UnitState u)
        {
            if (u == null || u.Card == null) return null;
            foreach (string k in RuleCore.AlternativeActions)
                if (u.Has(k) && u.Card.TriggerOps(k) != null) return k;
            return null;
        }

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
            _command = kind;
            if (selector != null) selector.Hide();

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
        ///   打不打得死 = `RuleCore.WouldKill` + **`RuleCore.FieldAttack`** —— 后者正是真打出去时
        ///   `DeclareAttack` 用的那一份攻击力（`RuleCore.cs:1482`）⇒ **预览与实际不可能分叉**。
        /// ⚠️ **主动技能那一路我们还没接** —— 🔴 **2026-09-29 已查实原版是算的**（不是「不显示」）：
        ///   原版 `CardHighlight.ToggleCombatPreviewHighlight` 的**伤害是一个 List<int>**（外加一张并行的
        ///   `List<DamageType>`），技能走 `formUnityAbility` 那支 → `EntityScript.GetActiveAbilityDamage()`
        ///   （遍历 ability 里 trigger 10/15/12、effectId 0x1e 的 `+0x14` 累加），**和我们这条 `FieldAttack` 是两回事**。
        ///   ⚠️ 而且它**逐条扣护甲**（`Max(1, dmg − armour)` **每条各扣一次**），还会追加
        ///   `CurrentShuriken` / `CurrentMarkerlight` 两条独立条目 ⇒ 多条小伤害的边界上与我们**必然不同**。
        ///   ⇒ **我们这条用的是引擎自己的预测**（`RuleCore.WouldKill` ↔ `ApplyDamage` 共用
        ///   `DamageAfterReduction` 那一份公式）—— 好处是**预览与实际不可能分叉**，
        ///   代价是与原版在「多条目」那几种情形下不一致。**技能那一路记成待办**（判据全文 →
        ///   `资料/待办判据_战场与战斗视图.md` §8b 的 Q1 那节），不是不做。</summary>
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
            if (Ctx != null && _selectedSlot >= 0 && _command != AttackKind.Ability)
            {
                var u = Ctx.Players[side].Board[slot];
                var atk = Ctx.Players[_me].Board[_selectedSlot];
                if (u != null && atk != null)
                    willDie = RuleCore.WouldKill(u, RuleCore.FieldAttack(Ctx, _me, atk, _command == AttackKind.Ranged));
            }
            view.SetWillDie(willDie);
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

        /// <summary>打出去（攻击或放技能）。`targetSlot` &lt; 0 = 不需要选目标的技能。返回引擎码。</summary>
        int Resolve(AttackKind kind, int targetSlot)
        {
            int slot = _selectedSlot;
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

            if (code != RuleCodes.OK) Debug.Log($"[Battle] 这一手打不出去：{RuleCodes.Describe(code)}");

            // 技能**正在结算** → 面板铺白那层（原版 `ShowActingLight()`），
            // 紧接着 `ClearSelection` 会带着这层白淡出，不是「啪」地消失
            if (kind == AttackKind.Ability && skillPanel != null) skillPanel.SetActing();

            ClearSelection();
            RefreshAll();
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
            _aiTimer = aiStepDelay;
            _aiSteps = 0;                 // 对手的新回合 → 步数清零
            _aiRejected.Clear();          // 同上：排除名单也只在本回合内有效
            RefreshAll();
            if (_net != null) _net.OnLocalAction(endAct);
            RecAct(endAct, _me, endPicks, endPickIds);      // 🆕 录像：玩家这条结束回合
            NetAfterTurnStart();          // 🆕 每回合开始对一次状态指纹（联机才有）
        }

        /// <summary>没牌可出、也没技能可放、也没人能攻击了 → 别让玩家干等，自动结束回合</summary>
        void AutoEndTurnIfStuck()
        {
            if (Ctx.IsOver || Ctx.Active != _me) return;
            // ⚠️ 用 `HasAnyAction`（**不掷骰、不吃难度旋钮**）—— 难度那套会「随机砍掉最低分的动作」，
            //    拿它判「玩家卡住了」会**误判**（玩家明明还能出牌，却被砍成只剩 endTurn）。
            if (SimpleAI.HasAnyAction(Ctx)) return;
            Debug.Log("[Battle] 没牌可出、没技能可放也没人能打 —— 自动结束回合");
            EndPlayerTurn();
        }

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
        /// 共同守卫是**受击方是真人**（`attacker.isPlayer==0 && IsAgainstHuman()`）。</summary>
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

            UpdateHud();     // 批处理里没有 Update() 循环，HUD 得在这里刷，不然截图上是旧值
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

            // 🆕 2026-09-18：**开局独白也走这个泵**。
            // 🔴 **不能挂在 `Update` 里** —— 批处理**没有帧循环**，`Update` 根本不跑，
            //    而自检是靠 `BattleScene.Step → AdvanceTimeline` 推的（`Step` 的注释里写着这个坑）。
            //    挂在 `Update` 里的话，真包能跑、**自检永远推不动**，那就是个只在一种环境下活的实现。
            // ⚠️ 必须排在 `_unitChat.Advance` **之后** —— 它判「上一条播完没有」靠的就是气泡的最新状态。
            TickIntroMonologue();

            // 🆕 2026-09-18：`ChatPopup` 的淡入淡出 + `ChatButton` 的 4 秒冷却。
            //    🔴 **和独白同一条理由挂在这里**：批处理没有帧循环，`Update` 不跑，
            //       挂在 `Update` 里就成了「真包能跑、自检永远推不动」的实现。
            if (_chatPopup != null) _chatPopup.Advance(dt);
            if (_chatCooldown > 0f) _chatCooldown = Mathf.Max(0f, _chatCooldown - dt);

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
                case EvtKind.Death:   evt = VfxMap.Death; break;
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
            Vector3 at = layout.SlotPosition(e.Slot);

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
                LastPop = CardFeel.PopNumber(transform, at + new Vector3(0f, CardFeel.ToOurs(0.35f), -0.4f),
                                             e.Amount, e.Amount < 0);
        }

        /// <summary>最近一次飘出来的数值（自检断言用 —— 飘字 1.83 s 后自己销毁，截图上看不出它来过）</summary>
        public Label LastPop { get; private set; }

        /// <summary>阵亡消散。
        /// ⚠️ **必须把视图从 `_myUnits/_foeUnits` 里摘掉** —— 不然 `SyncBoard` 发现引擎里那一格空了，
        ///    当场就把视图 `Kill` 了，消散一帧都看不见（批处理里更是直接 `DestroyImmediate`）。</summary>
        void PlayDeathFeel(BattleEvent e)
        {
            var views = e.Player == _me ? _myUnits : _foeUnits;
            CardView v;
            if (!views.TryGetValue(e.Slot, out v) || v == null) return;

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
            if (Ctx != null && BoardSpec.IsValid(e.Slot)
                && Ctx.Players[e.Player].Board[e.Slot] != null)
                return;

            views.Remove(e.Slot);
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
            CardView v;
            if (!views.TryGetValue(e.Slot, out v) || v == null) return;
            views.Remove(e.Slot);

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

        void SyncBoard(int owner, Dictionary<int, CardView> views, bool mine)
        {
            var board = Ctx.Players[owner].Board;
            var layout = mine ? playerBoard : enemyBoard;
            // ⚠️ 原来这里是「是不是 Tide，不是就是 Ember」的二选一 —— 一接原版阵营就会
            //    全部掉进 Ember 那个色。改成查表（`FactionColor`）。
            var frame = FactionColor(owner == _me ? _myFaction : _foeFaction);

            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = board[s];
                CardView v;
                bool hasView = views.TryGetValue(s, out v) && v != null;

                if (u == null)
                {
                    if (hasView)
                    {
                        // 🆕 2026-09-26：这一格空了 ⇒ 「未行动」绿光也要灭。
                        //   ⚠️ **必须在这里关**：下面 `DeathPendingFor` 那条会 `continue`（阵亡事件还排在时间线上、
                        //      视图要再站 0.85 s）—— 不关的话那张正在消散的卡会**一直亮着绿光**。
                        v.SetCanAct(false);
                        // ⚠️ **这一格刚空、但阵亡事件还排在时间线上时，先别销毁。**
                        //    `PlaySignals` 只**排期**、不当场播（见 `EventTiming`）—— 引擎里人已经死了，
                        //    而画面上那张卡还要再站 0.85 s 才轮到「阵亡」那一刻。
                        //    第一版就是在这里立刻销毁的：`DyingCount` 恒为 0、消散一帧都没演出来，
                        //    而**截图上看不出**（只看到「人没了」）。
                        if (animateFeel && DeathPendingFor(owner, s)) continue;

                        Kill(v.gameObject); views.Remove(s);
                    }
                    continue;
                }

                if (!hasView)
                {
                    var data = ToCardData(u, owner == _me ? _myFaction : _foeFaction);
                    data.frame = u.IsWarlord ? new Color(0.95f, 0.82f, 0.35f) : frame;   // 督军描金
                    // 🔴 **场上用 `CardFace.Board`**：原版在 inPlay 类状态把**整张 `2DCard` 关掉**
                    //    （`Card2DController.Toggle(false)`），只留 `Board Elements` 那棵子树 ——
                    //    插图/卡框/费用/稀有度宝石/卡名/阵营行/兵种行/效果底板/效果文字**一起没有**，
                    //    只剩攻/血/甲 + 7 槽关键词徽标。见 `CardFace` 的注释与出处。
                    v = CardView.Create(boardRoot, data, $"{(mine ? "My" : "Foe")}Unit_{s}_{u.Name}",
                                        CardFace.Board);
                    views[s] = v;

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
                else
                {
                    v.SetData(ToCardData(u, owner == _me ? _myFaction : _foeFaction));  // 掉血/疲劳要反映到卡面
                }

                if (use3DBoard)
                {
                    // 🔴 **真 3D 落点**（2026-09-20）。逐值来自原版 `MinionManager`：站在地面上、
                    //    y=0、卡根旋转 identity、缩放 = `desiredScale`（玩家 0.36 / 敌 0.69）。
                    //    判据 = `ArenaSlots`（唯一出处）。
                    //    ⚠️ **屏幕位置和原来那套正交坐标几乎重合**（两行 65.4%/42.7% vs 原版实测
                    //    65.6%/43.1%）⇒ **命中判定仍然按屏幕空间走，不用改**。
                    bool foe = !mine;
                    float sc = u.IsWarlord ? ArenaSlots.HeroScale(foe) : ArenaSlots.CardScale(foe);
                    v.SetLayer(ArenaSlots.ArenaLayer);            // 换 layer：改由透视相机画
                    v.SetPose(ArenaSlots.RootPosition(s, foe, sc), 0f, sc);
                }
                else
                {
                    // 没有 3D 战场（退回烘好的背景图）时**保持原样** —— 那一层没有别的相机，
                    // 这时候把卡挂到 `ArenaLayer` 会**两台相机都不画**（静默消失）。
                    v.SetPose(layout.SlotPosition(s), 0f, layout.placedScale * LayoutSpace.Scale);
                }
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
                    match = CardView.Create(transform, ToCardData(card, _myFaction), "Hand_" + card.Name);
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

        void SyncFoeHand()
        {
            if (foeHand == null) return;
            var h = Ctx.Players[1 - _me].Hand;
            _foeDealt.Clear();

            // 卡背视图按需增减（手牌只会一张张长；打到上限就停）
            while (_foeHandViews.Count < h.Count)
            {
                var q = ImageQuad.Create(foeHand.transform, CardArt.CardBack(_foeFaction), Vector3.zero,
                                         FoeCardWorldH, new Vector2(0.5f, 0.5f),
                                         "FoeHand_" + _foeHandViews.Count);
                if (q == null) break;                       // 没卡背图就整个不建（别静默建一半）
                // 画成**卡本身那个矩形**（卡背图自己的宽高比和 2DCard 不一样）
                q.SetAspect(CardView.Width / CardView.Height);
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
                tr.localPosition = foeHand.SlotPosition(i, n);
                tr.localRotation = Quaternion.Euler(0f, 0f, foeHand.RotationAt(i, n));
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
        ///   （`DescZh` 有就用中文，没有就英文 `Desc`）—— 原版卡面上印的就是这段字。
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

            bool zh = !string.IsNullOrEmpty(c.DescZh);
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
            string seg = CardText.KeywordSegment(c.Keywords, zh, body);
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
            //    重开一局（「按 R 再来一局」）会再走一遍 `Begin()` → `BuildHud()`，
            //    再建一份的话屏幕上会**叠两层 HUD**，而且**旧的那个结算面板成了孤儿**——
            //    `_endPanel` 已经指向新建的那个，旧面板再也没人 `Hide()`，就一直挂在画面上
            //    （2026-09-12 接「再来一局」时截图抓到的：新一局已经开打，上一局的
            //     「对局结束 / 三个骷髅 / 2/3 / 按 R 再来一局」还压在战场上）。
            if (_hudBuilt) return;
            _hudBuilt = true;

            var hudGo = new GameObject("HudRoot");
            hudGo.transform.SetParent(transform, false);
            hudRoot = hudGo.transform;
            var root = hudRoot;      // 下面所有 HUD 件都挂这个根（原来直接挂 `transform`）
            CardArt.Load();

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
            _enemyText = Hud(root, "", 0.125099f, 0.940833f, 3, dim, new Vector2(0.5f, 0.5f), "EnemyPlateText");
            _myPlate = HudImage(root, "UI_Player_Frame", -0.005938f, 0.060602f,
                                new Vector2(0f, 0.5f), 126.3f / 108f, "PlayerPlate");
            _myText = Hud(root, "", 0.123568f, 0.074583f, 3, dim, new Vector2(0.5f, 0.5f), "PlayerPlateText");

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
            _skullScore = Hud(root, "", SkullScoreX01, SkullScoreY01, 3, Color.white,
                              new Vector2(0f, 0.5f), "MatchSkullsScore");

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
            _myFaithText = Hud(root, "0", MyFaithX01, MyFaithY01, 4, new Color(1f, 1f, 1f),
                               new Vector2(0.5f, 0.5f), "PlayerFaithText");
            _foeFaithText = Hud(root, "0", FoeFaithX01, FoeFaithY01, 4, new Color(1f, 1f, 1f),
                                new Vector2(0.5f, 0.5f), "EnemyFaithText");

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
            _myStoneText = Hud(root, "0", MyStoneX01, MyStoneY01, 4, new Color(1f, 1f, 1f),
                               new Vector2(0.5f, 0.5f), "PlayerSpiritStoneText");
            _foeStoneText = Hud(root, "0", FoeStoneX01, FoeStoneY01, 4, new Color(1f, 1f, 1f),
                                new Vector2(0.5f, 0.5f), "EnemySpiritStoneText");
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
            _energyLabel = Hud(root, "", MyEnergyX01, MyEnergyY01, 4, gold, new Vector2(0.5f, 0.5f), "EnergyLabel");
            // 敌方水晶：原版有（`EnemyMana`，也带 `ManaText`），**我们原来一颗都没画** ——
            // 于是玩家看不到对手还剩多少能量，只能靠猜。
            _foeEnergyGem = HudImage(root, "40k_battle_energy_full", FoeEnergyX01, FoeEnergyY01,
                                     new Vector2(0.5f, 0.5f), 0.72f, "FoeEnergyGem");
            _foeEnergyGemEmpty = HudImage(root, "40k_battle_energy_empty", FoeEnergyX01, FoeEnergyY01,
                                          new Vector2(0.5f, 0.5f), 0.72f, "FoeEnergyGemEmpty");
            _foeEnergyLabel = Hud(root, "", FoeEnergyX01, FoeEnergyY01, 4, gold,
                                  new Vector2(0.5f, 0.5f), "FoeEnergyLabel");
            // ⚠️ x01 从 0.017 挪到 0.075：手牌数底板有 **259 px 宽**，还摆在屏幕左缘的话整块板会有一半在屏幕外
            //    （第一版就是这样，截图里只看得见板子右半边）。原版那个计数在 `HandArea`（手牌区）里、不在屏幕角上，
            //    挪进来既让板进画面、也更接近原版的位置。
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
            _endTurnLabel = Hud(root, CardText.Phrase("END TURN"), EndTurnX01, EndTurnY01, 3,
                                new Color(1f, 1f, 1f), new Vector2(0.5f, 0.5f), "EndTurnButton");
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
            //   `Cardback Container`（scale 100）→ `Cardback`(2.17×3.14×100 = **217×314 px**)
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

            _pileLabel = Hud(root, "", mySizeX, mySizeY, 3, dim,
                             new Vector2(0.5f, 0.5f), "MyPileLabel");
            _foePileLabel = Hud(root, "", foeSizeX, foeSizeY, 3, dim,
                                new Vector2(0.5f, 0.5f), "FoePileLabel");

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
            // 🔴 **2026-09-24 改：z 从 `HudDecorZ`（最远那层）提到名牌**前面****
            //    原版同级顺序（直读 `RectTransform_3189.json` 的 `m_Children`）=
            //      `[NameBackground(3293), TitleBackground(3560), Avatar Item Small(2709), PlayerNameText(3016)]`
            //    —— UGUI **后出现的兄弟画在上面** ⇒ 底条在名牌**之上**、头像块又在底条之上。
            //    实拍（`桌面/战斗截图参考.png`）也是这个样：称号那根条压在名牌下半截上。
            //    ⛔ 旧注释写的「它就是名牌的底、要压在名牌下面」**没有出处**，已按同级顺序推翻。
            //    （原来那次的真实事故是「和名牌同 z ⇒ 谁压谁不确定」，不是「原版在下面」。）
            _titleBgMe = HudAbs(root, "UI_PlayerFrame_TitleBackground", 54.2f, 1028.5f, 311f, 42f,
                                 "TitleBackground_Me", HudImageZ - 0.02f);
            _titleBgFoe = HudAbs(root, "UI_PlayerFrame_TitleBackground", 54.5f, 92.8f, 311f, 42f,
                                 "TitleBackground_Foe", HudImageZ - 0.02f);

            // ---- 称号文字（原版 GO 名也叫 `EnemyTitle`，两侧同参数）----
            // 出处 `子代理读报_back左区_0827.md:48`（我）`:71`（敌）+ 直读 `MonoBehaviour_3797/3887.json`：
            //   我 x[110.0,344.2] y[1021.0,1067.1]（框 234.2×46.0）· 敌 x[110.3,344.5] y[85.3,131.3]
            //   TMP：出厂 `m_text` 就是占位串 `"Title Text"` · fs **30.55**（autosize 2→35）·
            //        H 居中 · V Midline · `m_fontColor` = (1.0, 0.6306, 0.4198) ≈ **#FFA16B 橙**
            // 🔴 **显隐判据**（`PlayerProfileUIController__SetProfileTitle.c`，机器码级）：
            //    `SetActive(titleGO, !string.IsNullOrEmpty(title))` ⇒ **没有称号 ⇒ 连底条一起关**。
            //    实况 dump 印证（`runtime_ui_dump_drive_0912.tsv`）：`TitleBackground` 的
            //    `activeSelf = False`（原版关服、玩家没有称号）⇒ 我们默认也**不显示**。
            // ⚠️ 所以这一件**不是「画一行字上去」**（§13-E 第 7 条当时是这么理解的）——
            //    原版无资料时**整块都不出现**。单机没有玩家资料 ⇒ 默认关；`SetTitle` 留好了接线点。
            var titleOrange = new Color(1.0f, 0.6306f, 0.4198f);
            _titleMe = Hud(root, "", 227.1f / 1920f, 1f - 1044.05f / 1080f, 3, titleOrange,
                           new Vector2(0.5f, 0.5f), "TitleText_Me");
            _titleFoe = Hud(root, "", 227.4f / 1920f, 1f - 108.3f / 1080f, 3, titleOrange,
                            new Vector2(0.5f, 0.5f), "TitleText_Foe");
            if (_titleMe != null) _titleMe.SetGlyphHeight(TitleFontPx / 108f);
            if (_titleFoe != null) _titleFoe.SetGlyphHeight(TitleFontPx / 108f);
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
            // ⚠️ 原版场景态里 `avatarImage`（头像立绘）是 **m_Enabled=0** —— 所以**只摆框、不摆立绘**
            //    （那本来由 `ItemDrawer` 按玩家资料运行时灌，单机没有资料）。这一件因此和名牌自带的
            //    盾形**几乎重合**（同一个位置、同一个造型）—— 但它是**独立节点**，原版有、我们原来没有。
            HudAbs(root, "Player_Profile_Border", 1.71f, 947.61f, 110.88f, 123.88f,
                   "AvatarItemSmall_Me", HudImageZ - 0.05f);
            HudAbs(root, "Player_Profile_Border", 3.01f, 12.11f, 110.88f, 123.88f,
                   "AvatarItemSmall_Foe", HudImageZ - 0.05f);

            // ---- 三个边角按钮（图都在；`m_OnClick` 原版也是空的）----
            // `ChatButton`（玩家名牌下）：64.44×61.85 @x[50.9,115.4] y[880.2,942.0]；
            //   图 `40k_UI_bt_voicelines` 128×128 → 实绘 0.50×（`子代理读报_back左区_0827.md:59`）。
            //   ⚠️ 名字叫 Chat，但这**一个对象上挂了两个组件**（2026-09-18 更正）：
            //      ① `Button`(MB 5291) 的 `m_OnClick → BattleManager.ClickChat` ⇒ **开 `ChatPopup` 面板**（我们接了）
            //      ② `PlayerStateToggle`(MB 4089) 的 `selectedBool='EnableWarlordVOs'` ⇒ 敌语音开关（**没接，待实况确认**）
            //      旧报告那句「与 6 个聊天钮完全无关，勿混」**已被推翻** —— 见 `资料/语音线_原版规格与ASR管道.md` §1.7.0。
            _chatBtn = HudAbs(root, "40k_UI_bt_voicelines", 50.9f, 880.2f, 64.44f, 61.85f, "ChatButton");
            // `CenterCameraButton` 64.44×61.85 @x[17.9,82.4] y[568.2,630.0]；图 237×237 → 0.27×（`:94`）
            HudAbs(root, "40k_UI_bt_center_camera", 17.9f, 568.2f, 64.44f, 61.85f, "CenterCameraButton");
            // `OffensiveButton` 109.01×106.94 @x[0,109] y[446.9,553.8]；图 128×124 → 0.85×（`:95`）
            // 🆕 2026-09-29（§25）：**那颗钮的显隐是有判据的**（原来我们画上去就一直亮着）——
            //   原版 `BattleHud.Initialize` 里先 `SetActive(false)`，之后**只有** `_ApplyOffensiveAndDefensiveEffects`
            //   会把它打开（判据 = 选定的那张卡 id **≠ 空卡 id**，`d__337:146-155`）；
            //   `isEmptyOffensiveCard` 也不「恒真」，它就是「id 等于本阵营空卡的 id」。
            //   ⇒ 我们照做：建完先关着，`ApplyOffensiveEnvOnce` 里按同一条判据开。
            _offensiveBtn = HudAbs(root, "40k_battle_icon_environmental", 0f, 446.9f, 109.01f, 106.94f, "OffensiveButton");
            if (_offensiveBtn != null) _offensiveBtn.gameObject.SetActive(false);

            // `ChatPopup` 面板本身（原版在 `FrontCanvas/Safe area/Unit Chat` 下，默认 `m_IsActive = false`）
            //   版面与逐节点坐标见 `资料/语音线_原版规格与ASR管道.md` §1.7.1；实现见 `Battle/ChatPopupPanel.cs`
            _chatPopup = ChatPopupPanel.Create(root);

            // ---- 任务点数字 `QPText '0/3'`：fs 40.5、**Bold**、白、H 居中 / V Capline ----
            // 出处：`子代理读报_back右区_0827.md:133`（敌 x[1840.9,1889.1] y[176.1,221.3]）
            //      与 `:154`（我 x[1841.8,1889.9] y[621.4,666.6]）。
            // ⚠️ 中心正好等于**任务点 holder 的中心**（我 (1865.9,644.2) / 敌 (1865.5,198.9)）
            //    —— 所以它画在那颗任务点图标上，不是另起一块。
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
            _qpTextMe = Hud(root, "0/3", 1865.85f / 1920f, 1f - 644.0f / 1080f, 4, white,
                            new Vector2(0.5f, 0.5f), "QPText_Me");
            _qpTextFoe = Hud(root, "0/3", 1865.0f / 1920f, 1f - 198.7f / 1080f, 4, white,
                             new Vector2(0.5f, 0.5f), "QPText_Foe");
            _qpTextMe.gameObject.SetActive(meQp);
            _qpTextFoe.gameObject.SetActive(foeQp);

            // ---- `Energy Accumulation`（能量累积那盏灯）：77.8×80.1 ----
            // 出处：`子代理读报_back右区_0827.md:142`（敌 x[1746.7,1824.4] y[247.9,328.0]，102×102 → 0.763×）
            // 与我方那个是**同一个相对位置**（holder 中心的 (-81.3, +0.5)，见 `:141`/`:162`）。
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
            HudAbs(root, accumArt, 1746.7f, 247.9f, 77.8f, 80.1f, "EnergyAccumulation_Foe");
            HudAbs(root, accumArt, 1746.7f, 515.6f, 77.8f, 80.1f, "EnergyAccumulation_Me");

            // ---- 加时标记 `OvertimeIndicator`：68.6×71.0，图 `40k_icon_overtime`（preserveAspect=1）----
            // 出处：`子代理读报_back右区_0827.md:171`（x[1718.9,1787.5] y[341.5,412.5]，
            //       174×180 → 0.394×）。它在能量 holder 内、时钟左边。
            // ⚠️ **默认关着**：原版也只在加时里出现（`OvertimeUi.DisplayOvertime`：淡入 1s/停 1s/淡出 1s）。
            //    🆕 2026-09-20：**加时机制已经接上了**（引擎 `ctx.IsOvertime`，判据见 `BattleContext.IsOvertime`）
            //    —— 它现在会在对局里真的亮起来，不再是「摆着好看」。
            _overtime = HudAbs(root, "40k_icon_overtime", 1718.9f, 341.5f, 68.62f, 70.99f, "OvertimeIndicator");
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

            _overtimeSplashRoot = new GameObject("OvertimeSplashText");
            _overtimeSplashRoot.transform.SetParent(root, false);
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
            _overtimeSplashText = Label.Create(rt, "OVERTIME!", new Vector3(0f, 0f, zText), 1,
                                               Color.white, new Vector2(0.5f, 0.5f), "Text");
            if (_overtimeSplashText != null)
            {
                // 🔴 **不能照抄原版那个 `m_fontSize = 80`** —— 那是**原版自己画布**的单位。
                //    （2026-09-20 踩过：写成 `SetCapHeight(80 * WorldCapPerFontSize)` 之后
                //     「OVERTIME!」一个字母占了大半屏 ≈335 px。）跨工程能对齐的只有**渲染出来的高度**：
                //    原版 fontSize 80 ⇒ 大写高 ≈ 0.72 em = **57.6 px**（`TmpFont` 里那条
                //    「拉丁大写高约 0.72 em」），本工程 108 px = 1 世界单位 ⇒ **0.5333 世界单位**。
                const float origFontSize = 80f;         // `MonoBehaviour_3973.json` 的 m_fontSize
                const float latinCapEm    = 0.72f;      // TMP 注释里那条：拉丁大写 ≈ 0.72 em
                _overtimeSplashText.SetCapHeight(origFontSize * latinCapEm / 108f);
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

        /// <summary>`hurry` 语音本回合说过没有（原版 `ClockManager` 的 `latch_0xb8`）——
        /// 自检靠它验「过 35 秒只播一次、跨回合复位」。</summary>
        public bool HurrySaidThisTurn { get { return _hurrySaidThisTurn; } }

        /// <summary>敌方能量数字（`2/2` 这种）—— 那颗水晶原来**根本没画**</summary>
        public string FoeEnergyText { get { return _foeEnergyLabel != null ? _foeEnergyLabel.Text : null; } }
        /// <summary>🆕 2026-09-27：**我方**能量数字 —— `FoeEnergyText` 的对称件。
        /// 联机客机视角那一段靠「两侧文字跟着 `_me` 换」来验 `UpdateHud` 认的是哪一方
        /// （`UpdateHud` 里 `me = Ctx.Players[_me]`）。</summary>
        public string MyEnergyText { get { return _energyLabel != null ? _energyLabel.Text : null; } }
        /// <summary>🆕 2026-09-27：我方牌堆/弃牌那行字（`DECK n  DISC m`）—— 同上，给客机视角那段用。</summary>
        public string MyPileText { get { return _pileLabel != null ? _pileLabel.Text : null; } }
        /// <summary>🆕 2026-09-27：敌方牌堆/弃牌那行字。</summary>
        public string FoePileText { get { return _foePileLabel != null ? _foePileLabel.Text : null; } }
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

        Label Hud(Transform root, string text, float x01, float y01, int scale,
                  Color c, Vector2 anchor, string name)
        {
            var l = Label.Create(root, text, LayoutSpace.ToWorld(x01, y01), scale, c, anchor, name);
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

        /// <summary>称号那行字的字号（原版 `m_fontSize` = **30.55 画布像素**；见 `MonoBehaviour_3797.json`）。
        /// ⚠️ 别拿它去喂 `Label.SetFontSize`（那会大 2.7 倍）—— 走 `SetGlyphHeight(px/108)`。</summary>
        public const float TitleFontPx = 30.55f;

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
            _handLabel.SetText(CardText.Phrase("HAND") + " " + me.Hand.Count);
            PlaceHandPlate();                       // 底板跟着标签走（原版：文字居中压在板上）
            // 🔴 **2026-09-28 用户拍板：名牌那格印【名字】** —— 原版那个节点就叫 `EnemyNameText`
            //    （`BattleDriver.cs:4782` 的出处），我们原来印「阵营 + HP n」是因为**没有名字数据源**。
            //    现在：我方 = `ProfileData.PlayerName`（默认「玩家123」，档案窗可改）；
            //         敌方 = 联机局的对端名（`NetMatchmaking.FoeName`），**单机局留空不编**（同 `EnemyName` 的口径）。
            //    ⚠️ **名字不随座位变**（我就是我、对手就是对手）⇒ 它不再能判「翻座位」，
            //       `BattleScene` 那两条断言已改成别的判据（座位方向本来就有能量/牌堆两条更硬的）。
            _myText.SetText(ProfileData.PlayerName + "   " + CardText.Faction(_myFaction));
            _enemyText.SetText((_net != null && !string.IsNullOrEmpty(NetMatchmaking.FoeName)
                                ? NetMatchmaking.FoeName + "   " : "") + CardText.Faction(_foeFaction));
            // 记「降到过的最低生命」（骷髅头判据用它）—— 两边各记一份（我方那份只给对局历史用，见字段注释）
            if (foe.Warlord.Health < _foeWarlordMinHp) _foeWarlordMinHp = foe.Warlord.Health;
            if (me.Warlord.Health < _myWarlordMinHp) _myWarlordMinHp = me.Warlord.Health;
            // 名牌上的里程碑：原版是 `MatchSkulls Score` = `x N`，N 由 `BattleScoreUiManager.UpdateMilestonesCount`
            // 写。⚠️ **原版那个方法体被剥空了**（`d:/2/Warpforge_code/.../BattleScoreUiManager.cs` 只有字段），
            //    「x3」到底是「已达成数」还是「总数」**在原版数据里证不出来** —— 我们按「已达成数」算，
            //    判据和结算面板**共用同一份**（`DeckRules.SkullsFor`，规则书:36 的三个血量阈值）。
            if (_skullScore != null) _skullScore.SetText("x" + DeckRules.SkullsFor(_foeWarlordMinHp));
            // 本回合已出牌数：出几张亮几枚（原版只有三枚节点，第四张不显示）
            if (_playedPips != null)
                for (int i = 0; i < _playedPips.Length; i++)
                    if (_playedPips[i] != null) _playedPips[i].gameObject.SetActive(i < _cardsPlayedThisTurn);
            _pileLabel.SetText(CardText.Phrase("DECK") + " " + me.Deck.Count + "  " +
                               CardText.Phrase("DISC") + " " + me.Discard.Count);
            _foePileLabel.SetText(CardText.Phrase("DECK") + " " + foe.Deck.Count + "  " +
                                  CardText.Phrase("DISC") + " " + foe.Discard.Count);

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
                if (_endPanel != null && !_endPanel.Visible)
                {
                    _endPanel.Show(Ctx.Winner, _me,
                                   _foeWarlordMinHp == int.MaxValue ? 30 : _foeWarlordMinHp, Ctx.Turn,
                                   Ctx.ForfeitedBy);
                    // 🆕 2026-09-23：**打完一局 → 任务进度动**（原版也是这条链：对局回来 `MissionChallengeProgress` 累加）。
                    // 战果**由引擎记**（`BattleContext.DamageToEnemy` / `TroopsPlayed`），这里只消费。
                    // 判据「赢没赢」与上面那行文字**同源**（`Ctx.Winner == _me + 1`），不另写一套。
                    DailyData.OnBattleEnd(Ctx.Winner == _me + 1, Ctx.DamageToEnemy[_me], Ctx.TroopsPlayed[_me]);
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
        /// 骷髅 = `DeckRules.SkullsFor(对方督军降到过的最低生命)` —— **连 `int.MaxValue → 30` 那个兜底
        /// 都照抄 `EndPanel.Show` 的写法**（面板显示几颗，记录里就是几颗；`SkullsFor(30)` 与
        /// `SkullsFor(int.MaxValue)` 同值 0，抄它是为了两处**字面**也一致）。
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
                OwnSkulls = DeckRules.SkullsFor(_foeWarlordMinHp == int.MaxValue ? 30 : _foeWarlordMinHp),
                EnemySkulls = DeckRules.SkullsFor(_myWarlordMinHp == int.MaxValue ? 30 : _myWarlordMinHp),
                OwnScore = "", EnemyScore = "",
                // 模式：原版 `matchType` → 本地化键那条映射**本地查不到**（`BattleLogData.Mode` 的注释）
                // ⇒ 存的就是这两个词，它们也是本工程**唯一**的模式字符串口径（`NetMatchmaking` 的 `ModeStr` 同款）。
                Mode = Ctx.Vars.IsSkirmish ? "Skirmish" : "Classic",
                Pinned = false,
                // 回放没做（§三 第 18 条 第 6 件）⇒ 没有编号可比。行上那颗 `ReplayButton` 本来就会如实出声。
                RecordingIndex = -1,
            });
        }

        /// <summary>督军名的**显示名**（有中文就用中文 —— 与卡面 / 单位发言同一条规矩，`Zh()` 那一族）。
        /// 卡池里查不到就原样回英文，**不静默丢成空串**。</summary>
        static string HeroDisplayName(PlayerState p)
        {
            var c = (p != null && p.Warlord != null) ? p.Warlord.Card : null;
            if (c == null) return "";
            return string.IsNullOrEmpty(c.NameZh) ? c.Name : c.NameZh;
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
            if (HitTip(_skullIcon, wp, TipText.Skulls)) return true;
            if (HitTip(_myEnergyPlate, wp, TipText.Energy) || HitTip(_foeEnergyPlate, wp, TipText.Energy)) return true;
            if (HitTip(_myQuestIcon, wp, TipText.QuestPoints) || HitTip(_foeQuestIcon, wp, TipText.QuestPoints)) return true;
            if (HitTip(_myFaithIcon, wp, TipText.Faith) || HitTip(_foeFaithIcon, wp, TipText.Faith)) return true;
            if (HitTip(_myStoneIcon, wp, TipText.SpiritStone) || HitTip(_foeStoneIcon, wp, TipText.SpiritStone)) return true;

            Tooltip.Hide();
            return false;
        }

        /// <summary>
        /// 🆕 2026-09-21：卡面**关键词**的 tooltip（悬停那枚图标 / 那个词时弹）。
        ///
        /// **原版这条链**（全量反编译，见 `资料/tooltip_原版规格与实现.md` §五）：
        /// 关键词段整项套 `<link=<DefinedTrait枚举名>>`（`GameStaticData__TraitNameToString.c:84-109`）
        /// → `TextTooltipController` 每帧 `TMP_TextUtilities.FindIntersectingLink` 命中
        /// → `EverguildTraitTooltipItem`（比基础版多 **图标 + 标题**）。
        /// 我们这条：`CardText.KeywordSegment` 套 `<link=规范键>`（卡面组装那一侧）→
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
            Vector2 sp = PointerScreen();
            return LayoutSpace.ScreenToWorld(sp, cam);
        }

        static Vector2 PointerScreen()
        {
            if (Mouse.current != null) return Mouse.current.position.ReadValue();
            if (Touchscreen.current != null) return Touchscreen.current.primaryTouch.position.ReadValue();
            return Vector2.zero;
        }

        bool _clickLatch;

        bool ClickedThisFrame()
        {
            // 轮询按下（批处理里没有输入事件，轮询才验得了）；用 latch 防止按住触发多次
            bool down = PointerHeld();
            if (!down) { _clickLatch = false; return false; }
            if (_clickLatch) return false;
            _clickLatch = true;
            // 🆕 **真实点击记录**（用户 2026-09-24；与 `PointerLayer` / `DeckRuntime` 那两条同源）。
            //    ⚠️ 战场里**没有单一命中表**（卡 / 手牌 / HUD / 面板各判各的）⇒ 这一条主要靠
            //    「这一帧的日志」说话；另附世界坐标，方便对到是哪张卡/哪块地盘。
            if (ClickLog.Enabled)
            {
                var wp = WorldPointer();
                ClickLog.Begin(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, "BattleDriver",
                               LayoutSpace.ToPixel(wp));
                ClickLog.Hit("世界坐标 (" + wp.x.ToString("F2") + ", " + wp.y.ToString("F2") + ", "
                             + wp.z.ToString("F2") + ")"
                             + " —— 战场没有统一的命中表，**实际吃到的是哪一件看下面这帧的日志**");
            }
            return true;
        }

        /// <summary>指针**按着**（不带 latch）。滑块拖动要用它 —— 拖动是持续状态，不是一次点击。</summary>
        static bool PointerHeld()
        {
            return Mouse.current != null && Mouse.current.leftButton.isPressed
                || Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed;
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
        ///    有没有记全，正是回放对账要检出来的东西。</summary>
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
    }
}
