// RuleEngineTest.cs — 规则引擎自检（纯逻辑，进 play 模式/图形设备都不需要）
//
// 语义基准是 `d:/warpforge/scripts/rule_core.gd` 的那份测试（99 个函数 / 320 条断言）。
// 这里挑**同一批语义**的用例，用同样的输入、对同样的期望值 ——
// 不一致就是移植错了，而不是「我的实现更合理」。
//
// 用法（菜单）：Tools > RuleEngine > 规则引擎自检
// 用法（CLI）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" \
//     -batchmode -quit -projectPath "D:\4\Unity\MyGame" -executeMethod RuleEngineTest.Run \
//     -logFile "d:/4/_tmp_view/ruleengine.log"
//   筛输出：grep "^RE " d:/4/_tmp_view/ruleengine.log
//   退出码：0 = 全过，1 = 有失败（批处理下会写进 Unity 的退出码）
using System;
using System.Collections.Generic;
using System.Text;
using RuleEngine;
using UnityEditor;
using UnityEngine;

public static partial class RuleEngineTest
{
    const string P = "RE ";

    static int _pass, _fail;
    static readonly List<string> _failures = new List<string>();

    // ==================================================================
    //  入口
    // ==================================================================

    [MenuItem("Tools/RuleEngine/规则引擎自检")]
    public static void Run()
    {
        _pass = 0; _fail = 0; _failures.Clear();

        Debug.Log(P + "=== 规则引擎自检 开始 ===");

        Section("棋盘一致性");
        TestBoardMatchesPresentation();

        Section("卡牌数据");
        TestKeywordParsing();
        TestCardDatabase();
        TestOriginalCardPool();


        Section("卡面逐张核对（字段 / 关键词修正）");
        TestCardFaceFixes();

        Section("卡牌身份（稳定 id）");
        TestCardIds();

        Section("选效果（chooseeffect）");
        TestChooseEffect();

        Section("A4 收尾·批 3（新机制）");
        TestA4Batch3();

        Section("A4 收尾·批 4（强行触发关键词 / 事件型手牌陷阱 / `a Stratagem` 词义）");
        TestA4Batch4();

        Section("A5 批 1（`Heal N` 无目标=自己 / 三族「已由别的层接手」）");
        TestA5Batch1();

        Section("战术卡文本（能解析 N/448）");
        TestTacticTextCoverage();

        Section("A4 解析层的宽度（付费前缀 / 小原子 / 设为 N 费）");
        TestA4PaidPrefixAndAtoms();

        Section("A4 收尾·批 1（受伤筛 / 静态降费 / 每单位星镖 / 正在祈祷 / 付费修饰 / 对称部署）");
        TestA4Batch1Atoms();

        Section("A4 收尾·批 2（强制攻击族 + 反向共用目标）");
        TestA4Batch2ForceAttack();

        Section("单位卡 desc 的效果文字（查证：接进 EffectText 能认多少 —— 只报数）");
        ReportUnitDescCoverage();

        Section("`When <事件>` 覆盖面（铺宽这条线的工作清单）");
        ReportWhenCoverage();

        Section("「相邻」对账（锚点定成了什么 / 光环族还没做 —— 只报数）");
        ReportAdjacentGap();

        Section("「相邻」：锚点、目标集、以及「锚点写反」的反例");
        TestAdjacent();

        Section("光环（A7）：锚点 / 筛选 / 载荷 / 时长 —— 解析层");
        TestAuraParse();

        Section("光环（A7）：结算 —— 增量维护、收回、叠加、回合限定");
        TestAuraSettle();

        Section("灵魂石能力（灵族 `N [Spirit Stone]: …`）：代价判据 + 部署时真的付石生效");
        TestSpiritStone();

        Section("誓约能力（`Oath N: …`，单位卡那一支）+ 三条修饰句（2026-09-16）");
        TestOathAbility();

        Section("2026-09-16 那批剩下那几张卡（残骸广播 / 宇宙巨蛇 / 诺恩降费 / 督军 0 费）");
        TestBatch0916();

        Section("条件里的缩写系动词（`it's a <兵种>`）—— 2026-09-16 修的那条静默失效（7 张卡）");
        TestConditionContractions();

        Section("`Accursed Helbrute`（`it as well` 载荷 + `this troop` 指代）—— 2026-09-16");
        TestAccursedHelbrute();

        Section("`Alpha Warrior`（`plus an additional N` 承前省略属性名）—— 2026-09-16");
        TestAlphaWarrior();

        Section("静默桩家族（`controlcount` / `energycheck` / `alreadyhas` / `A or B`）—— 2026-09-16");
        TestConditionKindFamily();

        Section("中文卡面对账抓到的两条（`Shrineworld` 数据 · `Prayer` 无逗号 if）—— 2026-09-16");
        TestZhCrosscheckFindings();

        Section("「会执行的那一层」有没有机制（按卡类型）—— 2026-09-16 · 只报数");
        ReportWillRunMechanism();

        Section("从句级尺子（吞句候选 —— 卡级两把尺子的盲区）—— 2026-09-19 · 只报数 + 新候选=0");
        ReportSwallowedClauses();

        Section("光环那一侧的机制账（`Aura.Recompose` 消费不消费得掉每一条载荷）—— 2026-09-19");
        ReportAuraMechanism();

        Section("收尾三件：`Beastboss` 引号里的动词 · 多段载荷丢段（改成看得见）· `Beast Snagga Nob` 叠份数 —— 2026-09-16");
        TestBeastbossAndPayloadSegments();

        Section("中文卡面对账里那几条零假阳性的判据（J1–J6）—— 2026-09-16");
        TestZhAgreement();

        Section("原来记成「终态」的 6 张查完 —— 4 张已修（另 2 张待裁决）—— 2026-09-16");
        TestFormerTerminalGaps();

        Section("玩家真的能选（选牌/三选一/选效果）：面板的答案被消费、没问的能看出来");
        TestPlayerChoice();

        Section("A6 族 A/B/C（嵌入正文 / 条件 / 载荷词）：静默错打与「能打但没用」");
        TestA6Batch1();

        Section("巧技 `Artifice`（每次打出战术时触发）");
        TestArtifice();

        Section("替代行动族 `Duty` / `Pray` / `Ferocity` / `Agenda`");
        TestAlternativeActions();

        Section("虫群 `Swarm`（打出在右侧同名部队旁边时合并）");
        TestSwarm();

        Section("突触 `Synapse`（被友方战术选中时对相邻单位重复效果）");
        TestSynapse();

        Section("起义 `Uprising`（之后每部署一个部队时触发）");
        TestUprising();

        Section("激励 `Stimulation`（被战术选中时、结算前触发）");
        TestStimulation();

        Section("潮涌 `Tide X`（打出时给 X 张临时复制）");
        TestTide();

        Section("残骸 `Remnant`（死亡时翻面、受伤害或回合结束被摧毁、可被翻回来）");
        TestRemnant();

        Section("传送 `Teleport`（当回合从牌库抽到即打出时触发）");
        TestTeleport();

        Section("伏击 `Ambush`（面朝下打出、挨伤害就作废、撑一轮才触发）");
        TestAmbush();

        Section("伴生 `Companion X`（打出时带出手里的伴生部队）");
        TestCompanion();

        Section("战术卡能打（解析 → 结算 → 弃牌堆）");
        TestTacticPlay();

        Section("防御卡（39 张：能打、进手牌、不参与换牌）");
        TestDefenceCards();
        TestGraveyardTakeOne();

        Section("卡实例身份（待办第 7 行 · 第 1 步：棋盘一侧先拿到实例）");
        TestCardInstanceStep1();

        Section("卡实例身份（第 7 行 · 第 2 步：三个区域换成实例，跨区域还是同一份）");
        TestCardInstanceStep2();
        TestCardInstanceNewVsMove();

        Section("🎯 卡实例身份（第 7 行 · 一号验收靶：Master of Manoeuvre + 同名另一份不许被误伤）");
        TestInstanceAcceptanceTarget();

        Section("阵营资源（信仰 / 灵魂石）");
        TestFactionResources();

        Section("`When <事件>` 铺宽（新接的几种事件真的会触发）");
        TestWhenEventsWidened();

        Section("卡组构筑");
        TestDeckRules();
        TestDeckValidation();
        TestDeckIntoBattle();
        TestDeckStoreRoundTrip();
        TestDeckLibrary();

        Section("开局");
        TestNewBattle();

        Section("开局换牌（Mulligan）");
        TestMulligan();

        Section("回合");
        TestBeginTurn();
        TestEnergyIsPerPlayer();

        Section("出牌");
        TestPlayCostRejected();
        TestPlaySlotRejected();
        TestPlayOccupiedRejected();
        TestPlayOk();

        Section("攻击");
        TestMeleeTrade();
        TestArmorMinimumOne();
        TestShieldBlocks();
        TestRangedEatsCounter();
        TestLongRangeNoCounter();

        Section("目标合法性");
        TestVanguardRestriction();
        TestStealthUntargetable();
        TestFlyingMeleeBlocked();

        Section("技能与触发");
        TestEffectSpecParsing();
        TestFlankInvulnVulnerable();
        TestAttackKeywords();
        TestBattleKeywords();
        TestRally();
        TestStrikeAndSlay();
        TestBacklash();
        TestPenitence();
        TestAbility();
        TestAbilityTargetRules();
        TestHealAndDraw();
        TestEffectChainGuard();
        TestStarterCardEffects();
        TestAiUsesAbility();

        Section("对手 AI（SimpleAI）：斩杀线 / 攻击目标评分");
        TestAiKillLine();

        Section("造牌（create）");
        TestCreate();

        Section("免费部署（deploy）");
        TestDeploy();

        Section("降费（lowercost）");
        TestLowerCost();

        Section("A5 批 3：`next` 一次性降费 · 括号选项表 · 每个单位各打一下 · `Other friendly X`");
        TestA5Batch3();

        Section("A5 批 4：天赋的裸名写法 · 开局上手（督军专有）");
        TestA5Batch4();

        Section("选牌（choosecard）");
        TestChooseCard();

        Section("回手/回牌库 + 指代抽牌（return / drawref / add）");
        TestReturnAndRefs();

        Section("常驻效果 / 手牌陷阱（回合起止触发）");
        TestPersistentEffects();

        Section("部署时触发 + 持续改费（常驻效果的另两个触发点）");
        TestDeployBuffAndCostMore();

        Section("单位卡 desc 的触发式效果（Rally/Strike/Slay/Backlash/Penitence）");
        TestUnitDescTriggers();

        Section("群体（Mob）与团（Regiment）：近战 / 远程各一条，都正反各钉一次");
        TestMobAndRegimentKeyword();

        Section("「关键词被触发」族的事件（`When … triggers <关键词>`）");
        TestKeywordTriggeredEvents();

        Section("阵营资源事件（信仰 / 灵魂石 / 任务点）真的发得出来");
        TestFactionResourceEvents();

        Section("只有单位卡才当事件监听者（`played` 是误报，不是漏做）");
        TestListenerCardType();

        Section("载荷里不认识的词必须报出来（不是静默忽略）");
        TestUnknownPayloadAttr();
        TestWeaponIsRanged();

        Section("「未实现关键词」名单不许误报（也不许把没做的登记成已做）");
        TestKeywordImplementedList();

        Section("不稳定 / 残忍（做成）与狂喜（推迟，卡点钉成断言）");
        TestUnstableEcstasyCruelty();

        Section("「条件换数值」：`…, or <另一个数> if <条件>`（Vindicator / Wulfen Pack Leader / Monster Hunters / Disruption Blades）");
        TestOrAltIf();

        Section("Maulerfiend 的「近战+远程翻倍」与 Runtherd 的「5 费及以下造兽」");
        TestDoubleAndCreateCost();

        Section("天赋（Talent）：回合开始生成同名战术卡，而且是**临时卡**");
        TestTalentKeyword();

        Section("事件层（When <事件>, …）");
        TestWhenEvents();

        Section("事件层收尾（降费真的落到费用上 / `damaged` 真的广播 / 递归守卫）");
        TestEventLayerTail();

        Section("临时卡（Ephemeral）+「移出游戏」区域");
        TestEphemeral();

        Section("伤害同时结算 / 死亡触发排在其后（规则书 :145 + :238）");
        TestSimultaneousDamage();

        Section("阵营机制（repeat / Oath / Codex）");
        TestFactionMechanics();

        Section("胜负与疲劳");
        TestWinnerByWarlord();
        TestDrawIsDraw();
        TestForfeit();
        TestFatigue();

        Section("确定性");
        TestDeterminism();
        TestSignalDeterminism();

        Section("端到端");
        TestFullGameWithRealCards();

        Section("两个阵营：极限战士 vs 兽人");
        TestFactionBattle();

        Section("对手 AI（照原版 `AI` 类重写）");
        TestAiOriginal();

        // ---- 汇总 ----
        int total = _pass + _fail;
        if (_fail == 0)
        {
            Debug.Log(P + $"=== 结束：{_pass}/{total} 全过 ✅ ===");
        }
        else
        {
            var sb = new StringBuilder();
            sb.Append(P).Append($"=== 结束：{_pass}/{total} 通过，**{_fail} 条失败** ❌ ===");
            foreach (var f in _failures) sb.Append("\n").Append(P).Append("   ✗ ").Append(f);
            Debug.LogError(sb.ToString());
        }

        if (Application.isBatchMode)
            EditorApplication.Exit(_fail == 0 ? 0 : 1);
    }

    // ==================================================================
    //  断言
    // ==================================================================

    static void Section(string title) { Debug.Log(P + $"--- {title} ---"); }

    static void Check<T>(T got, T want, string msg)
    {
        if (EqualityComparer<T>.Default.Equals(got, want))
        {
            _pass++;
            Debug.Log(P + $"   ✓ {msg}");
        }
        else
        {
            _fail++;
            string line = $"{msg} —— 期望 [{want}]，实得 [{got}]";
            _failures.Add(line);
            Debug.LogError(P + $"   ✗ {line}");
        }
    }

    /// <summary>
    /// **A4 第一批**（2026-09-13）：付费前缀的四种写法 + 三个小原子 + 「设为 N 费」。
    ///
    /// 这一批几乎全在**解析层的宽度**上（正则 / 词表），所以断言分三层，缺一不可：
    ///   ① **认得出**（`SegKind.Ok`）；
    ///   ② **切出来的东西长什么样**（动词 / 代价 / 货币 / 目标）—— 只钉 ① 会被「认出来了但切错了」骗过去
    ///      （第十六轮 `lowercost` 把 payload 切成 `f all vehicles` 就是这么漏的）；
    ///   ③ **真的改变了局面**（费用真的变了 / `DutyUsed` 真的复位）—— 前两层全绿而结算层没接，
    ///      本工程有过好几次先例。
    /// </summary>
    static void TestA4PaidPrefixAndAtoms()
    {
        // ---- ① 付费前缀：`(N)` 与**无冒号**那两种写法 ----
        //   卡图实据（2026-09-13 主对话亲读，铁律 7）：`Reclaim the Stars` 的 `❺` ·
        //   `Forewarned` 的 `❶` · `Will of Asuryan` 的 `❶` 都是**绿圈**；
        //   `Sacred Rose` 的 `6 ☀` 是**太阳（信仰）**。OCR 把它们抄成了 `(5)` / 裸数字 / `[icon]`。
        //
        //   🔴 **2026-09-14 更正（原来这里写「绿圈**能量**图标」—— 错了）**：
        //   主对话亲眼看图并排比对过（`Techmarine` 的深灰星芒 / `Fiery Conviction` 的金色太阳
        //   都不是它）⇒ **绿六边形 = 灵魂石**，是灵族的**阵营货币**，不是能量。
        //   这三张全在 `资料/灵魂石卡_逐张核.md` 的 28 张名单里。
        //   ⚠️ **所以下面这几条断言的语义要读准**：它们在钉「**解析器认得出这两种形状**」，
        //   **不是**在钉「这个数字是能量」。裸 `(N)` 无货币 ⇒ 解析层只能按能量兜底，
        //   **这正是那 28 张卡必须显式写成 `N [Spirit Stone]: …` 的原因**
        //   （数据侧已改完，见 `cardface_fixes.json` 的 `_2026-09-14_灵魂石` 说明段）。
        {
            var r1 = EffectText.ParseSegment("(5) Reduce their cost by 5");
            Check(r1.Kind, EffectText.SegKind.Ok, "`(5) Reduce their cost by 5` 认得出");
            if (r1.Ops != null && r1.Ops.Count > 0)
            {
                Check(r1.Ops[0].Verb, "lowercost", "动词 = lowercost");
                Check(r1.Ops[0].Cost, 5, "★ 代价 5（`(5)` 是绿圈图标被 OCR 抄成的括号数字）");
                Check(r1.Ops[0].Amount, 5, "降 5 费");
            }

            // 🔴 正确的写法才带得出货币 —— `Reclaim the Stars` 卡面那个绿圈是**灵魂石**
            var r1s = EffectText.ParseSegment("5 [Spirit Stone]: Reduce their cost by 5");
            Check(r1s.Kind, EffectText.SegKind.Ok, "`5 [Spirit Stone]: …` 认得出");
            if (r1s.Ops != null && r1s.Ops.Count > 0)
            {
                Check(r1s.Ops[0].Cost, 5, "★ 代价 5");
                Check(EffectText.CostKindOf(r1s.Ops[0].CostKind), "spirit",
                      "★ **货币 = 灵魂石** —— 写成裸 `(5)` 或 `5 [spirit]:` 都会退化成能量/失配（静默）");
            }

            var r2 = EffectText.ParseSegment("(1) Draw a card");
            Check(r2.Kind, EffectText.SegKind.Ok, "`(1) Draw a card` 认得出");
            if (r2.Ops != null && r2.Ops.Count > 0)
            {
                Check(r2.Ops[0].Verb, "draw", "动词 = draw");
                Check(r2.Ops[0].Cost, 1, "代价 1");
            }

            var r3 = EffectText.ParseSegment("6 ☀ Give them Flank");
            Check(r3.Kind, EffectText.SegKind.Ok, "`6 ☀ Give them Flank` 认得出（**卡面没有冒号**）");
            if (r3.Ops != null && r3.Ops.Count > 0)
            {
                Check(r3.Ops[0].Cost, 6, "代价 6");
                Check(r3.Ops[0].CostKind, "faith",
                      "★ 货币 = **信仰**（卡面是太阳图标 —— 修女会的 Faith，不是能量）");
            }

            var r4 = EffectText.ParseSegment("1 Also give it Shield");
            Check(r4.Kind, EffectText.SegKind.Ok,
                  "`1 Also give it Shield` 认得出（数字后面还跟着语气词 `Also`）");
            if (r4.Ops != null && r4.Ops.Count > 0) Check(r4.Ops[0].Cost, 1, "代价 1");

            var r4b = EffectText.ParseSegment("6 [icon] Give them Flank");
            Check(r4b.Kind, EffectText.SegKind.Ok, "`6 [icon] Give them Flank` 认得出（`[icon]` = 图标占位）");
            if (r4b.Ops != null && r4b.Ops.Count > 0)
                Check(r4b.Ops[0].CostKind, "faith",
                      "★ `[icon]` 判成**信仰** —— 实测那 3 处全是修女会卡（太阳图标），见 `CostKindOf`");

            // ⚠️ **反例**：没有「数字后面必须是动词」那条判据的话，正常句子会被当成付费前缀吃掉。
            var r5 = EffectText.ParseSegment("Draw 5 cards");
            Check(r5.Kind, EffectText.SegKind.Ok, "`Draw 5 cards` 照旧认得出");
            if (r5.Ops != null && r5.Ops.Count > 0)
            {
                Check(r5.Ops[0].Verb, "draw", "动词 = draw");
                Check(r5.Ops[0].Amount, 5, "抽 **5** 张");
                Check(r5.Ops[0].Cost, 0, "★ **没有代价** —— 被当成付费前缀的话这里会是 5、还只抽 1 张");
            }
        }

        // ---- ② `Give an additional +1 Health`（**没写目标**的 give）----
        //   出处：`Beacon of Faith` 的 `4: Give an additional +1 Health`。原来整句认不出，
        //   因为 `ReGive` 的第三种语序**要求代词**（`Give it/them X`）。
        {
            var r = EffectText.ParseSegment("4: Give an additional +1 Health");
            Check(r.Kind, EffectText.SegKind.Ok, "`4: Give an additional +1 Health` 认得出");
            if (r.Ops != null && r.Ops.Count > 0)
            {
                Check(r.Ops[0].Verb, "give", "动词 = give");
                Check(r.Ops[0].Cost, 4, "代价 4");
                CheckTrue(r.Ops[0].Target != null && r.Ops[0].Target.Subjectless,
                          "★ 目标标成 **`Subjectless`**（卡面没写给谁）—— 标错成「己方全体」的话，"
                          + "单位触发正文里那句 `Give +2 Health` 会给**全队**各加一份");
            }
        }

        // ---- ③ `Does nothing`（原版设计就是空效果）----
        {
            var r = EffectText.ParseSegment("Does nothing");
            Check(r.Kind, EffectText.SegKind.Ok, "`Does nothing` 认得出（`Improvised Barricade`）");
            if (r.Ops != null && r.Ops.Count > 0)
                Check(r.Ops[0].Verb, "noeffect", "动词 = noeffect");
        }

        // ---- ④ `Lower cost by 1 when <事件>`（事件触发式降费）----
        //   🔴 机制早就在（`CardDef.CostWhens` + `BroadcastCostWhen`），**卡住的是这句话本身**：
        //      它解析不出 ⇒ `IsFullyParsed` 为假 ⇒ **卡打不出去、进不了卡组、卡面还打 `*`**。
        {
            var r = EffectText.ParseSegment("Lower cost by 1 when an enemy dies");
            Check(r.Kind, EffectText.SegKind.Ok, "`Lower cost by 1 when an enemy dies` 认得出");
            if (r.Ops != null && r.Ops.Count > 0)
            {
                Check(r.Ops[0].Verb, "costwhen", "动词 = costwhen（标记 op）");
                Check(r.Ops[0].Amount, 1, "降 1 费");
            }
            var be = CardDatabase.Find(CardDatabase.Load(), "Burgeoning Empire");
            CheckTrue(be != null && be.CostWhens.Count > 0,
                      "真卡 `Burgeoning Empire` 的降费监听器被收下来了");
            CheckTrue(be != null && EffectText.IsFullyParsed(be.Desc),
                      "★ 而且它现在**整条 desc 解析得干净** —— 判据三处共用"
                      + "（能打 / 能进卡组 / 卡面打不打 `*`），解析不出来时这三件事**全都错**");
        }

        // ---- ⑤ 端到端：`Reload the Duty abilities of all your units` 真的复位 ----
        {
            var reload = Tactic("T_ReloadDuty", 3, "Reload the Duty abilities of all your units");
            var duty = new CardDef("FixtureDutyUnit", "FixtureDutyUnit", "unit", "", "common", "Test",
                                   2, 3, 5, 0, new[] { "Duty" }, subtype: "Infantry");
            var ctx = ProbeBattle(new[] { reload }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            var u = Place(ctx, 0, 0, duty, exhausted: true);
            u.DutyUsed = true;                       // 装成「本局已经用过职责」
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_ReloadDuty"), -1), RuleCodes.OK,
                      "打出装填那张");
            CheckTrue(!u.DutyUsed, "★ **职责真的被装填了**（`DutyUsed` true → false）—— "
                                 + "只钉「解析出来了」的话，这条会是 true");
        }

        // ---- ⑥ 端到端：`reduce its cost **to** 1`（**设为** 1 费，不是「降 1 费」）----
        //   真卡 `Lying in Wait`：`Return a friendly troop to your hand and reduce its cost to 1`。
        {
            var card = Tactic("T_LyingInWait", 2,
                              "Return a friendly troop to your hand and reduce its cost to 1");
            var expensive = new CardDef("FixtureExpensive", "FixtureExpensive", "unit", "",
                                        "common", "Test", 5, 2, 5, 0, null, subtype: "Infantry");
            var ctx = ProbeBattle(new[] { card }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            Place(ctx, 0, 0, expensive, exhausted: true);
            Check(RuleCore.CostOf(ctx, 0, expensive), 5, "回手之前它是 5 费");
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_LyingInWait"), 0), RuleCodes.OK,
                      "打出 `Lying in Wait`（指着我方那个单位）");
            CheckTrue(SlotOf(ctx, 0, "FixtureExpensive") == -1, "它回手了");
            // 第 7 行第 3 步：费用修正钉在**那一份**上 ⇒ 尺子要问**手里那一份**
            var expBack = HandInst(ctx, 0, "FixtureExpensive");
            CheckTrue(expBack != null, "它在手里（按实例取得到）");
            Check(RuleCore.CostOf(ctx, 0, expBack), 1,
                  "★ **费用被设成 1** —— 一张 5 费的牌要降 **4**，所以「设为 N」不能用「降 N」表达"
                  + "（写成「降 1 费」的话这里会实得 4）");
        }

        // ---- ⑦ 🆕 2026-09-16：**尾句 `and reload …` 被目标短语吞掉** ----
        //  卡面：`Give +1 Ranged Attack to your troops **and reload their Duty abilities**`
        //  （`Cadian Honour`）。改之前 `SplitAndTail` 的动词白名单里**没有 `reload`** ⇒ 不切尾句 ⇒
        //  整句判「认了」· `IsFullyParsed` 为真 · 卡面不打 `*`，而 **`reload` 从来不发生**。
        //  同族第 2 句 `Lead by Example`（`Give +2 Health to a friendly unit **and reload its Duty
        //  ability**.`）。全池含 `reload` 的卡**只有这 3 张**，第 3 张 `Press the Attack` 是句首写法。
        //  ⚠️ 断言**两层**：① 解析层真的多出一条 `reloadduty`；② **结算层**真的把 `DutyUsed` 复位
        //     —— 只钉解析的话，「切出来了但没人结算」照样绿。
        {
            var card = Tactic("T_CadianHonour", 2,
                              "Give +1 Ranged Attack to your troops and reload their Duty abilities");
            var ops = EffectText.Parse(card.Desc, out _, out _);
            bool hasReload = false;
            foreach (var o in ops) if (o.Verb == "reloadduty") hasReload = true;
            CheckTrue(ops.Count == 2 && hasReload,
                      "★ 尾句被切出来了：op 2 条、其中一条 `reloadduty`"
                      + "（改之前只有 1 条，目标引文是「your troops and reload their duty abilities」）");

            var duty = new CardDef("FixtureDutyTroop", "FixtureDutyTroop", "unit", "", "common", "Test",
                                   2, 3, 5, 1, new[] { "Duty" }, subtype: "Infantry");
            var ctx = ProbeBattle(new[] { card }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            var u = Place(ctx, 0, 0, duty, exhausted: true);
            u.DutyUsed = true;                       // 装成「本局已经用过职责」
            int ra = u.RangedAttack;
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_CadianHonour"), 0), RuleCodes.OK,
                      "打出 `Cadian Honour`（指着我方那个部队）");
            Check(u.RangedAttack, ra + 1, "★ +1 远程真的加上了");
            CheckTrue(!u.DutyUsed, "★ **职责也被装填了** —— 这半句就是这次修的：改之前它被吞进目标短语，"
                                 + "`DutyUsed` 会一直留在 true（而且没有任何报错）");
        }

        // ---- ⑧ 🆕 2026-09-16：**逗号枚举 `Give A, B and C` 被当成随机二选一** ----
        //  卡面：`Give +2 [attack], +2 [ranged] and Concussive to a friendly troop`（`Thunderous Charge`，
        //  中文「+2[攻击]、+2[远程] **和** 震荡」）。`TryEitherOr` 里那层「再按 `, ` 拆一次」的展开
        //  **漏了「整句必须真有 ` or `」这个前提** ⇒ 逗号枚举被拆成「三个选项」⇒ `chooseone` +
        //  `RandomPick` ⇒ 结算**只放一个** ⇒ **buff 只给一半、而且静默**（不打 `*`、日志看不出来）。
        //  全池 **13 张**同形（`Autarch` · `Thunderous Charge` · `Legendary Tenacity` · `Forward
        //  Deployment` · `Imagifier` · `Knights of Macragge` · `Daemonbreaker` · `Grey Hunter Pack
        //  Leader` · `Synaptic Imperative` · `Rites of Penance` · `Deathwing Assault` · `Deathwing
        //  Strikemaster` · `Hive Commander`），`descZh` 全是「、…和…」，**没有一张写「或」**。
        //  ⚠️ 断言**三层**：① 解析层不产 `chooseone`；② **结算层三段真的都落到单位身上**；
        //     ③ **反向**：真有 ` or ` 的那张（`Ancient Reliquary`）**必须还是随机三选一**
        //        —— 只做②的话，「顺手把 `or` 句也改坏」不会红。
        {
            var card = Tactic("T_ThunderousCharge", 1,
                              "Give +2 attack, +2 ranged and Concussive to a friendly troop");
            var ops = EffectText.Parse(card.Desc, out _, out _);
            bool anyChoose = false;
            foreach (var o in ops) if (o.Verb == "chooseone") anyChoose = true;
            CheckTrue(!anyChoose && ops.Count == 1,
                      "★ 逗号枚举**不是**选择：op 1 条、没有 `chooseone`"
                      + "（改之前是 `chooseone n=2`，随机只给一半）");

            var troop = new CardDef("FixtureBuffTroop", "FixtureBuffTroop", "unit", "", "common", "Test",
                                    2, 3, 5, 1, null, subtype: "Infantry");
            var ctx = ProbeBattle(new[] { card }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            var u = Place(ctx, 0, 0, troop, exhausted: true);
            int atk = u.Attack, ra = u.RangedAttack;
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_ThunderousCharge"), 0), RuleCodes.OK,
                      "打出它（指着我方那个部队）");
            Check(u.Attack, atk + 2, "★ +2 近战加上了");
            Check(u.RangedAttack, ra + 2,
                  "★ **+2 远程也加上了** —— 改之前随机只放一个选项，这一段多半不发生");
            // `concussive` 经 `GivePayload` 归一成 `concussion`（`GivePayload.cs:94`）
            CheckTrue(u.KwValue("concussion") > 0,
                      "★ **震荡也在** —— 三段是**并列**、不是三选一");

            // ③ 反向：真 ` or ` 的那张不许被这次修改波及（用**卡池里那张真卡**，不是造的句子）
            var rel = CardDatabase.Find(CardDatabase.Load(), "Ancient Reliquary");
            CheckTrue(rel != null, "卡池里有 `Ancient Reliquary`");
            if (rel != null)
            {
                bool relChoose = false;
                foreach (var o in EffectText.Parse(rel.Desc, out _, out _))
                    if (o.Verb == "chooseone") relChoose = true;
                CheckTrue(relChoose,
                          "★ 而 ` or ` 句**仍然是随机选择**（`Ancient Reliquary` 三选一那条路不受影响）");
            }
        }

        // ---- ⑨ 🆕 2026-09-16：**「半句被吞」那一批**（5 个根因，一次钉住）----
        //  逐张裁定与出处：`资料/普查产出_0916/吞句候选裁定_汇总.md` §二。
        //  这几条**每一条都曾经静默失效**（不打 `*`、报表不报、日志看不出来）——
        //  所以断言要盯**「那半句真的成了 op 吗」**，不是只盯「整句认了」。
        {
            // ① RC-B：`and <数值> to <目标>` 的第二段（`Particle Whip`）
            var pw = EffectText.Parse("Deal 8 damage to an enemy and 3 to its adjacent units", out _, out _);
            Check(pw.Count, 2, "★ `and 3 to its adjacent units` 切成第二条 op"
                             + "（原来整段进目标短语：数值被当成**目标数 ×3**、相邻单位跟着吃 8 点）");
            CheckTrue(pw.Count > 1 && pw[1].Amount == 3 && pw[1].Target != null && pw[1].Target.Adjacent,
                      "★ 第二条是「对**相邻**单位 3 点」（数值与相邻都落对）");

            // ② 「并且失去潜行」：`Blind all enemies, and they lose Stealth and Camouflage`（`Solar Pulse`）
            var sp = EffectText.Parse("Blind all enemies, and they lose Stealth and Camouflage", out _, out _);
            bool hasLose = false;
            foreach (var o in sp) if (o.Verb == "lose") hasLose = true;
            CheckTrue(hasLose, "★ `they lose Stealth and Camouflage` 成了一条 `lose` op"
                             + "（`TryBlind` 原来**绕过 `Finish`** ⇒ 尾句切得出来却没人解）");

            // ③ 目标列表：`Destroy a friendly troop and a random enemy troop`（`Summary Execution`）
            var se = EffectText.Parse("Destroy a friendly troop and a random enemy troop", out _, out _);
            Check(se.Count, 2, "★ 目标列表拆成两条（原来只解出「消灭一个己方部队」，**敌方那半整句没有**）");
            CheckTrue(se.Count > 1 && se[1].Target != null && se[1].Target.Side == "enemy",
                      "★ 第二条打的是**敌方**");

            // ④ `, plus <附加句>`（`Fenrisian Monstrosities`）
            var fm = EffectText.Parse(
                "Deal 3 damage to all units, plus 2 additional damage to each enemy with Hunt Mark",
                out _, out _);
            Check(fm.Count, 2, "★ `, plus` 附加句拆成第二条 op");
            CheckTrue(fm.Count > 1
                      && fm[0].Target != null && string.IsNullOrEmpty(fm[0].Target.KeywordFilter),
                      "★ 主伤害**不带** `Hunt Mark` 筛选 —— 原来那个筛选被套到主伤害上"
                      + "（「对全体 3 点」变成「只打带猎杀标记的 3 点」，而额外 2 点整段没有）");

            // ⑤ 图标词污染**嵌入关键词头**：`' [skull] Slay: Draw a Beast'`（`Ferocious Rage`）
            //    方括号在 `ParseSegment` 入口就被剥掉 ⇒ 头成了 `skull slay` ⇒ 整段嵌入效果静默丢。
            var fr = CardDatabase.Find(CardDatabase.Load(), "Ferocious Rage (Beastboss' Talent)");
            CheckTrue(fr != null, "卡池里有 `Ferocious Rage`");
            if (fr != null)
            {
                var fops = EffectText.Parse(fr.Desc, out _, out _);
                bool emb = false;
                foreach (var o in fops)
                {
                    if (string.IsNullOrEmpty(o.Payload)) continue;
                    var ps = GivePayload.Parse(o.Payload);
                    if (ps == null) continue;
                    foreach (var p in ps)
                        if (!string.IsNullOrEmpty(p.Embedded)
                            && p.Embedded.StartsWith("slay", System.StringComparison.Ordinal))
                            emb = true;
                }
                CheckTrue(emb, "★ `[skull] Slay: Draw a Beast` 的嵌入效果认出来了"
                             + "（头被 `[skull]` 污染成 `skull slay` ⇒ 原来整段静默丢）");
            }
        }

        // ---- ⑩ 🆕 2026-09-16：**`… in play and in hand` 的手牌那半**（`Avenging Zeal`）----
        //  根因**三层同时堵死**（`ParseTarget` 只抽它认识的维度、`in hand` 当噪声丢掉；
        //  补救通道 `SplitTargetList` 那道门要求后半句解得成目标；`ResolveTargets` 没有手牌池）
        //  ⇒ 原来**只加场上单位**，而且 `IsFullyParsed` 仍为真、卡面不打 `*`、日志不报。
        //  ⚠️ 断言要**两层**：① 登记进 `ctx.HandBuffs`；② **打出手里那张时真的兑现** ——
        //     只钉①的话，「登记了但没人兑现」照样绿。
        {
            var card = Tactic("T_AvengingZeal", 0,
                              "Give +2 health and +2 attack to all your units in play and in hand");
            var onBoard = new CardDef("FixtureOnBoard", "FixtureOnBoard", "unit", "", "common", "Test",
                                      0, 3, 5, 0, null, subtype: "Infantry");
            var inHand = new CardDef("FixtureInHand", "FixtureInHand", "unit", "", "common", "Test",
                                     0, 3, 5, 0, null, subtype: "Infantry");
            var ctx = ProbeBattle(new[] { card, inHand }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            var b = Place(ctx, 0, 0, onBoard, exhausted: true);
            int bh = b.Health, ba = b.Attack;
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_AvengingZeal"), -1), RuleCodes.OK,
                      "打出它");
            Check(b.Health, bh + 2, "场上的那个 +2 生命（这半原来就有）");
            Check(b.Attack, ba + 2, "场上的那个 +2 近战");
            CheckTrue(ctx.HandBuffs.Count > 0,
                      "★ **手牌那半登记了** —— 改之前这里是 0（只加场上、不报错、不打 `*`）");

            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "FixtureInHand"), 2), RuleCodes.OK,
                      "打出手里那张部队");
            var u = Board(ctx, 0, 2);
            CheckTrue(u != null, "它上场了" + LogTail(ctx));
            if (u != null)
            {
                Check(u.Health, 5 + 2, "★ **手牌那张打出时带上了 +2 生命**（兑现点 `RuleCore.ApplyHandBuffs`）");
                Check(u.Attack, 3 + 2, "★ +2 近战也带上了");
            }
        }
    }

    /// <summary>
    /// A4 收尾 · 批 2：**强制攻击族**（`Murderous Desires` · `Peerless Bladesmen` · `Let Loose`
    /// · `Damaged Hexmark` 的 `Artifice` 正文）+ 顺手抽出来的**反向共用目标**（`Da Irongob`）。
    /// </summary>
    static void TestA4Batch2ForceAttack()
    {
        // ---- ① `Make a damaged friendly unit attack by itself`（`Murderous Desires`）----
        {
            var r = EffectText.ParseSegment("Make a damaged friendly unit attack by itself");
            Check(r.Kind, EffectText.SegKind.Ok, "`Make … attack by itself` 认得出");
            if (r.Ops != null && r.Ops.Count > 0)
            {
                Check(r.Ops[0].Verb, "forceattack", "动词 = forceattack");
                CheckTrue(r.Ops[0].Target != null && r.Ops[0].Target.DamagedOnly,
                          "★ **攻击者**规格带 `DamagedOnly`（`a damaged friendly unit`）—— "
                          + "攻击者与被打的**是两栏**，混了就是「打自己人」那类错");
                CheckTrue(r.Ops[0].Target2 == null, "被打的那一栏是空的（`by itself` = 自动挑）");
            }

            var card = Tactic("T_MurderousDesires", 4, "Make a damaged friendly unit attack by itself");
            var ctx = ProbeBattle(new[] { card }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 3);
            ctx.Players[0].Energy = 12;
            var foe = Place(ctx, 1, 0, Unit("EBig", 1, 0, 20), exhausted: true);   // 攻击力 0 ⇒ 不反击
            var wounded = Place(ctx, 0, 0, Unit("FWounded", 1, 3, 5));             // 部署后**可行动**
            var healthy = Place(ctx, 0, 1, Unit("FHealthy", 1, 3, 5));
            wounded.Health = 3;                                                    // 只把第一个打成「已受伤」
            int foeBefore = foe.Health;

            // 反例先做：**满血的那个不该能被点**（`CanPlayTactic` 的 `IsLegalPick` 必须和结算读同一套筛）
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_MurderousDesires"), 1),
                      RuleCodes.ErrSlot,
                      "★ 反例：点**满血**的友方单位被拒 —— `IsLegalPick` 漏筛 `DamagedOnly` 的话这里会是 OK，"
                      + "然后结算时**空过**（拖拽高亮说能选、打出去没事发生）");

            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_MurderousDesires"), 0), RuleCodes.OK,
                      "点**受伤**的那个 ⇒ 打得出去");
            Check(foeBefore - foe.Health, 3,
                  "★ 真打出了 **3 点**（该单位自己的近战攻击力）—— 走的必须是 `RuleCore.DeclareAttack`"
                  + "那条唯一路径" + LogTail(ctx));
            CheckTrue(wounded.Exhausted,
                      "★ 攻击算「本回合已行动」⇒ 它**疲劳**了（走真实攻击路径才会这样）" + LogTail(ctx));
        }

        // ---- ② `Target friendly unit attacks the enemy with highest attack`（`Peerless Bladesmen`）----
        //  🔴 `with highest attack` 必须**抢在** `ReTargetWith` 前面认出来 ——
        //     不然它会被当成「带 `highest attack` 关键词」，筛完一个不剩、**空过**。
        {
            var r = EffectText.ParseSegment("Target friendly unit attacks the enemy with highest attack");
            Check(r.Kind, EffectText.SegKind.Ok, "`Target … attacks the enemy with highest attack` 认得出");
            if (r.Ops != null && r.Ops.Count > 0)
            {
                CheckTrue(r.Ops[0].Target2 != null && r.Ops[0].Target2.PickMost == "+attack",
                          "★ 被打的那一栏标成 **`PickMost = +attack`**（挑攻击力最高的）");
                CheckTrue(string.IsNullOrEmpty(r.Ops[0].Target2.KeywordFilter),
                          "★ 反例：**不许**把 `highest attack` 当成关键词塞进 `KeywordFilter`"
                          + "（塞了就会按 `Has(\"highest attack\")` 筛 ⇒ 一个都不剩）");
            }

            var card = Tactic("T_PeerlessBladesmen", 4,
                              "Target friendly unit attacks the enemy with highest attack");
            var ctx = ProbeBattle(new[] { card }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 3);
            ctx.Players[0].Energy = 12;
            var weak = Place(ctx, 1, 0, Unit("EWeak", 1, 0, 20), exhausted: true);
            var strong = Place(ctx, 1, 1, Unit("EStrong", 1, 5, 20), exhausted: true);
            Place(ctx, 0, 0, Unit("FAttacker", 1, 2, 5));
            int weakBefore = weak.Health, strongBefore = strong.Health;
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_PeerlessBladesmen"), 0), RuleCodes.OK,
                      "打出 `Peerless Bladesmen`（点我方那个）");
            Check(strongBefore - strong.Health, 2, "★ 打的是**攻击力最高**的那个（5 攻）" + LogTail(ctx));
            Check(weak.Health, weakBefore, "★ 反例：**0 攻**那个一点没挨" + LogTail(ctx));
        }

        // ---- ③ `Each friendly Beast gets +1 [fist] this turn and attacks a random enemy`（`Let Loose`）----
        //  两件事：① `gets` 这个动词（原来只认 `gains`）；② 尾句的攻击者要**从前半句继承**。
        {
            var r = EffectText.ParseSegment("Each friendly Beast gets +1 [fist] this turn");
            Check(r.Kind, EffectText.SegKind.Ok, "`Each friendly Beast **gets** +1 [fist]` 认得出（`gets` 也是「获得」）");
            if (r.Ops != null && r.Ops.Count > 0)
            {
                Check(r.Ops[0].Verb, "gain", "动词 = gain");
                CheckTrue(r.Ops[0].Target != null && r.Ops[0].Target.SubtypeFilter == "Beast",
                          "★ 兵种筛 = `Beast`");
                Check(r.Ops[0].Target.Count, 0,
                      "★ 张数 = **全部**（句首的 `Each ` 和 `all ` 同义）—— 落回 1 的话**只给一个单位加**");
            }

            const string letLoose = "Each friendly Beast gets +1 [fist] this turn and attacks a random enemy";
            var ops = EffectText.Parse(letLoose, out _, out _);
            CheckTrue(ops.Count == 2 && ops[1].Verb == "forceattack",
                      "★ 尾句切成了**第二条 op**（`SplitAndTail` 加了 `attack`）—— "
                      + "不切的话 `and attacks a random enemy` 会被当成**载荷的一部分**、而那半句永远不发生");
            CheckTrue(ops.Count == 2 && ops[1].Target != null && ops[1].Target.SubtypeFilter == "Beast",
                      "★ **攻击者继承前半句的目标**（`Each friendly Beast`）—— 不继承的话 `Target` 是空的，"
                      + "结算会退回「本卡自己」= 让**战术卡**去打人");

            var card = Tactic("T_LetLoose", 6, letLoose);
            var ctx = ProbeBattle(new[] { card }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 3);
            ctx.Players[0].Energy = 20;
            Place(ctx, 1, 0, Unit("EBig", 1, 0, 40), exhausted: true);
            Place(ctx, 0, 0, new CardDef("FBeast1", "FBeast1", "unit", "", null, "Test", 2, 2, 5, 0, null,
                                        subtype: "Beast"));
            Place(ctx, 0, 1, new CardDef("FBeast2", "FBeast2", "unit", "", null, "Test", 2, 2, 5, 0, null,
                                        subtype: "Beast"));
            Place(ctx, 0, 2, new CardDef("FInf", "FInf", "unit", "", null, "Test", 2, 2, 5, 0, null,
                                        subtype: "Infantry"));
            // ⚠️ 量的是**敌方全场总生命**（含督军）：卡面写 `a random enemy`，随机挑谁不写死
            //    （挑中督军也可能，而且督军会**反击**）。
            int before = SumHealth(ctx, 1);
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_LetLoose"), -1), RuleCodes.OK,
                      "打出 `Let Loose`");
            Check(before - SumHealth(ctx, 1), 6,
                  "★ **2 个兽各打一次、各 3 点**（先 +1 近战 → 攻击力 2+1=3，再各打一下）" + LogTail(ctx));
            Check(ctx.Players[0].Board[0].Attack, 3,
                  "★ 兽拿到了 +1 近战（`gets +1 [fist] this turn` 真生效）");
            CheckTrue(ctx.Players[0].Board[0].Exhausted && ctx.Players[0].Board[1].Exhausted,
                      "★ 两个兽都疲劳了（真打了 —— 走的是 `DeclareAttack` 那条路才会疲劳）");
            CheckTrue(!ctx.Players[0].Board[2].Exhausted,
                      "★ 反例：**不是 Beast** 的那个没动（兵种筛生效）");
        }

        // ---- ④ 裸形态 `Attack(s) <被打的>` —— 攻击者是**本卡自己**（`Damaged Hexmark` 的 `Artifice` 正文）----
        {
            var r = EffectText.ParseSegment("Attacks a random enemy with lowest Health");
            Check(r.Kind, EffectText.SegKind.Ok, "`Attacks a random enemy with lowest Health` 认得出（裸形态）");
            if (r.Ops != null && r.Ops.Count > 0)
            {
                Check(r.Ops[0].Verb, "forceattack", "动词 = forceattack");
                CheckTrue(r.Ops[0].Target == null,
                          "★ **攻击者是空的**（裸形态 = 本卡自己）—— 填成「任意单位」就会从全场挑一个去打");
                CheckTrue(r.Ops[0].Target2 != null && r.Ops[0].Target2.PickMost == "-health",
                          "★ 被打的那栏：挑**生命最低**的");
                CheckTrue(r.Ops[0].Target2.Random, "而且卡面写了 `random`");
            }
        }

        // ---- ⑤ 顺手：**反向共用目标**（`Da Irongob`）----
        //  `Your Warlord gains Concussive until your next turn **and heals 5**` ——
        //  尾句的 `heals 5` **没写目标**，主语沿用前半句的「你的督军」。
        //  ⚠️ 不继承的话 `Heal N` 会走它自己的默认（**治己方督军**）—— 这一张恰好同解，
        //     但 `All friendly units gain Armour 2 … and heal 2`（`Conviction of Faith`）就**只治督军一个**了。
        {
            const string ds = "Your Warlord gains Concussive until your next turn and heals 5";
            var ops = EffectText.Parse(ds, out var un, out var pa);
            CheckTrue(un.Count == 0 && pa.Count == 0, "`… gains Concussive … and heals 5` 整句解析干净");
            CheckTrue(ops.Count == 2 && ops[1].Verb == "heal" && ops[1].Target != null,
                      "★ 尾句 `heals 5` **继承了前半句的目标**（没继承的话它 `Target` 是空的 = 「半懂」）");

            var card = Tactic("T_DaIrongob", 3, ds);
            var ctx = ProbeBattle(new[] { card }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 3);
            ctx.Players[0].Energy = 10;
            var w = ctx.Players[0].Warlord;
            w.Health = w.MaxHealth - 8;
            int hp = w.Health;
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_DaIrongob"), -1), RuleCodes.OK,
                      "打出 `Da Irongob`");
            Check(w.Health, hp + 5, "★ 督军**治了 5**（尾句真的结算了）" + LogTail(ctx));
            // ⚠️ 关键词的**内部名是 `concussion`**（卡面写 `Concussive`）——
            //    出处：`CardDef.cs:1156` 的别名表 `new[] { "concussive", "concussion" }`。
            //    拿 `Has("concussive")` 去问会**恒为假**（本轮实测踩到）。
            CheckTrue(w.Has("concussion"), "★ 而且拿到了 `Concussive`（内部名 `concussion`）" + LogTail(ctx));
        }

        // ---- ⑥ 🆕 2026-09-14 **用户口径**：卡面没写打谁 ⇒ **优先挑「打得死的」** ----
        //  口径原文：「根据要求选择攻击对象，**不过没有这个说明**，那么就是**优先选择可以摧毁的单位**」。
        //  两半都要钉死：① 没写 ⇒ 打得死的优先（**哪怕它格位号更大**）；
        //              ② 写了（`with highest attack` 那种）⇒ **照卡面**，这条规则**不许插队**。
        //  ⚠️ 摆位**故意反着来**：先摆的那个（格位 0）**打不死**、打得死的在**格位 1** ——
        //     这样「按格位号试第一个打得动的」会打错人，**只有**优先挑打得死的才对。
        {
            const string byItself = "Make a friendly unit attack by itself";
            var r0 = EffectText.ParseSegment(byItself);
            Check(r0.Kind, EffectText.SegKind.Ok, "`Make a friendly unit attack by itself` 认得出");
            if (r0.Ops != null && r0.Ops.Count > 0)
                CheckTrue(r0.Ops[0].Target != null && r0.Ops[0].Target2 == null,
                          "★ 前提：这个形态**被打那栏是空的**（`Target2 == null`）—— "
                          + "变了的话下面测的就不是「没写打谁」这条路了");

            var card = Tactic("T_ForceKill", 4, byItself);
            var ctx = ProbeBattle(new[] { card }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 3);
            ctx.Players[0].Energy = 12;
            var tanky = Place(ctx, 1, 0, Unit("ETanky", 1, 0, 20));    // 攻 0 ⇒ 不反击
            var frail = Place(ctx, 1, 1, Unit("EFrail", 1, 0, 2));     // ★ 打得死，但在**大格位号**
            Place(ctx, 0, 0, Unit("FAttacker", 1, 3, 5));
            int tankyBefore = tanky.Health;
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_ForceKill"), 0), RuleCodes.OK,
                      "打出「让它自己去打一下」");
            CheckTrue(!frail.IsAlive,
                      "★ **优先打的是打得死的那个**（2 血、格位 **1**）—— 退回「按格位号试第一个打得动的」"
                      + "就会去打格位 0 的血厚怪，这条断言会红" + LogTail(ctx));
            Check(tanky.Health, tankyBefore,
                  "★ 反例：**打不死**的那个一点没挨" + LogTail(ctx));
            CheckTrue(ctx.Players[0].Board[0].Exhausted,
                      "★ 攻击者疲劳了（走的是 `DeclareAttack` 那条唯一路径才会这样）");
        }

        // ---- ⑥-b 反例：**一个都打不死** ⇒ 退回原顺序（按格位号试第一个打得动的）----
        {
            var card = Tactic("T_ForceKillB", 4, "Make a friendly unit attack by itself");
            var ctx = ProbeBattle(new[] { card }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 3);
            ctx.Players[0].Energy = 12;
            var a = Place(ctx, 1, 0, Unit("EThick0", 1, 0, 20));       // 都 20 血、攻击者只有 3 攻 ⇒ 都打不死
            var b = Place(ctx, 1, 1, Unit("EThick1", 1, 0, 20));
            Place(ctx, 0, 0, Unit("FAttacker", 1, 3, 5));
            int aBefore = a.Health, bBefore = b.Health;
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_ForceKillB"), 0), RuleCodes.OK,
                      "打出（场上没人打得死）");
            Check(aBefore - a.Health, 3,
                  "★ 退回**原顺序**：打的还是格位 0（加这条规则之前的行为，不许变）" + LogTail(ctx));
            Check(b.Health, bBefore, "★ 反例：格位 1 一点没挨");
        }

        // ---- ⑥-c 反例：**带 Shield 的判「打不死」** —— 盾把伤害全挡下，所以「血少」不等于「打得死」----
        //  这条钉的是「预测与结算**共用同一份伤害公式**」（`RuleCore.DamageAfterReduction`）——
        //  若预测自己抄一份、只比「`Health` ≤ 攻击力」，它会**先去打带盾那个**（它看着是 2 血、能打死），
        //  下面这条断言立刻红。
        //  ⚠️ 两个都是 2 血：格位 0 **带盾**（其实打不死）、格位 1 **不带**（真的打得死）。
        {
            var card = Tactic("T_ForceKillC", 4, "Make a friendly unit attack by itself");
            var ctx = ProbeBattle(new[] { card }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 3);
            ctx.Players[0].Energy = 12;
            var shielded = Place(ctx, 1, 0, Unit("EShield", 1, 0, 2, "Shield"));
            var soft = Place(ctx, 1, 1, Unit("ESoft", 1, 0, 2));
            Place(ctx, 0, 0, Unit("FAttacker", 1, 3, 5));
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_ForceKillC"), 0), RuleCodes.OK,
                      "打出（格位 0 = 2 血**带盾**，格位 1 = 2 血不带盾）");
            CheckTrue(!soft.IsAlive,
                      "★ 打的是**格位 1** 那个 —— 带盾那个虽然也是 2 血，但盾会全挡，**判它打不死**" + LogTail(ctx));
            Check(shielded.Health, 2,
                  "★ 反例：格位 0 那个**连盾都还在**（一点没挨）" + LogTail(ctx));
            CheckTrue(shielded.HasShield, "★ 而且它的 Shield **没被消费掉**（说明根本没人打它）");
        }

        // ---- ⑥-d 反例：卡面**写了要求**（`with highest attack`）⇒ 照卡面，这条规则**不许插队** ----
        {
            var card = Tactic("T_ForceKillD", 4,
                              "Target friendly unit attacks the enemy with highest attack");
            var ctx = ProbeBattle(new[] { card }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 3);
            ctx.Players[0].Energy = 12;
            var frailLowAtk = Place(ctx, 1, 0, Unit("EFrailLow", 1, 1, 2));   // 打得死，但攻击力**低**
            var thickTopAtk = Place(ctx, 1, 1, Unit("EThickTop", 1, 5, 20));  // 打不死，但**攻击力最高**
            Place(ctx, 0, 0, Unit("FAttacker", 1, 3, 9));
            int frailBefore = frailLowAtk.Health, thickBefore = thickTopAtk.Health;
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_ForceKillD"), 0), RuleCodes.OK,
                      "打出 `Peerless Bladesmen`（卡面写死了打谁）");
            Check(thickBefore - thickTopAtk.Health, 3,
                  "★ **照卡面**打「攻击力最高」那个（哪怕它打不死）" + LogTail(ctx));
            Check(frailLowAtk.Health, frailBefore,
                  "★ 反例：**打得死的那个不许被优先** —— 这条规则只在卡面没写打谁时生效" + LogTail(ctx));
        }
    }

    static void CheckCode(int got, int want, string msg)
    {
        Check(got, want, msg + $"（{RuleCodes.Describe(got)}）");
    }

    // ==================================================================
    //  A4 收尾 · 批 1：六条新机制
    //
    //  每一条都是「**解析层 + 局面真的变了 + 反例**」三层 —— 只钉「认出来了」的话，
    //  下面每一条都能在实现完全没接上时照旧变绿（这个工程吃过很多次）。
    // ==================================================================

    /// <summary>
    /// 批 1 的六条：付费前缀后剥语气词 · 「只要已受伤」/「只要正在祈祷」的目标筛 ·
    /// 静态条件降费 · 每单位按自己星镖开火 · 付费修饰型激活 · 对称部署。
    /// </summary>
    static void TestA4Batch1Atoms()
    {
        // ---- ① `Oath 4: **Also** destroy all damaged enemy troops` ----
        //  两个卡点叠在一句上：① 付费前缀剥完之后，句首那个 `Also` 没人管了（原来只剥整段开头一次）；
        //  ② `damaged` 这个词没有任何目标筛接（原来会被当成普通词，**满血的也一起毁**）。
        {
            var r = EffectText.ParseSegment("Oath 4: Also destroy all damaged enemy troops");
            Check(r.Kind, EffectText.SegKind.Ok, "`Oath 4: Also …` 认得出（付费前缀之后要再剥一次语气词）");
            if (r.Ops != null && r.Ops.Count > 0)
            {
                Check(r.Ops[0].Verb, "destroy", "动词 = destroy");
                Check(r.Ops[0].Cost, 4, "代价 4（`Oath 4` 是付费激活）");
                CheckTrue(r.Ops[0].Target != null && r.Ops[0].Target.DamagedOnly,
                          "★ 目标标成 **`DamagedOnly`** —— 不标的话 `destroy all` 会把**满血的也毁掉**");
            }

            // 反例（同一族、但不该带 damaged）：`Oath 2: Also destroy an enemy troop`
            var r2 = EffectText.ParseSegment("Oath 2: Also destroy an enemy troop");
            CheckTrue(r2.Kind == EffectText.SegKind.Ok && r2.Ops != null && r2.Ops.Count > 0
                      && r2.Ops[0].Target != null && !r2.Ops[0].Target.DamagedOnly,
                      "★ 反例：`destroy an enemy troop` **不许**被标成 `DamagedOnly`");

            // 端到端：敌方两个单位，只伤一个 ⇒ **只该死那一个**
            var oath = Tactic("T_OathOfTheThrone", 1, "Oath 4: Also destroy all damaged enemy troops");
            var ctx = ProbeBattle(new[] { oath }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 3);
            // ⚠️ 手动给够能量：这张卡要 **1 费 + 4 激活 = 5**，而 `ProbeBattle` 的牌库几乎是空的
            //    （`ToP1Turn(n)` 每多推一个回合就多挨一次疲劳，推太远督军会被疲劳打死）。
            ctx.Players[0].Energy = 8;
            Place(ctx, 1, 0, Unit("EHealthy", 1, 1, 5), exhausted: true);
            var hurt = Place(ctx, 1, 1, Unit("EHurt", 1, 1, 5), exhausted: true);
            hurt.Health = 3;                          // 只把第二个打成「已受伤」
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_OathOfTheThrone"), -1), RuleCodes.OK,
                      "打出 `Oath of the Throne`");
            CheckTrue(SlotOf(ctx, 1, "EHealthy") != -1,
                      "★ **满血的那个活着** —— 目标筛没接上的话它会被一起毁掉" + LogTail(ctx));
            CheckTrue(SlotOf(ctx, 1, "EHurt") == -1, "受伤的那个被毁掉了" + LogTail(ctx));
        }

        // ---- ② `This costs 1 less if you control a unit with Stealth`（**静态条件降费**）----
        //  和「事件触发式降费」不是一件事：这个是**常驻条件**（人走价回），那个是一次性登记。
        {
            var r = EffectText.ParseSegment("This costs 1 less if you control a unit with Stealth");
            Check(r.Kind, EffectText.SegKind.Ok, "`This costs N less if you control a unit with X` 认得出");
            if (r.Ops != null && r.Ops.Count > 0)
            {
                Check(r.Ops[0].Verb, "costifcontrol", "动词 = costifcontrol（标记 op）");
                Check(r.Ops[0].Payload, "stealth", "关键词 = stealth");
            }

            var fate = Tactic("T_FateInescapable", 2,
                              "This costs 1 less if you control a unit with Stealth. Give Vulnerable 2 to an enemy troop");
            var ctx = ProbeBattle(new[] { fate }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            Check(RuleCore.CostOf(ctx, 0, fate), 2, "场上**没有** Stealth 单位 ⇒ 原价 2 费");
            Place(ctx, 0, 0, Unit("FStealth", 1, 1, 3, "Stealth"), exhausted: true);
            Check(RuleCore.CostOf(ctx, 0, fate), 1,
                  "★ 场上有 Stealth 单位 ⇒ **1 费**（判据在 `RuleCore.CostOf`，每次现算）");
            // 反例：**对手**的 Stealth 不算数（卡面写的是 `you control`）
            ctx.Players[0].Board[0] = null;
            Place(ctx, 1, 0, Unit("EStealth", 1, 1, 3, "Stealth"), exhausted: true);
            Check(RuleCore.CostOf(ctx, 0, fate), 2,
                  "★ 反例：**对手**的 Stealth 单位不给降价（卡面是 `you control`）");

            // 🔴 端到端：打出它，日志里**不许**留「动作 … 本版还没实现」。
            //   2026-09-13 踩到：`costifcontrol` 只加了 `EffectText` 那半边、
            //   **忘了在 `EffectDispatch` 登记** ⇒ 解析干净、卡照样打得出去，
            //   但每打一次都写一行「本版还没实现」还被算进 `unresolved`。
            //   是**逐阵营覆盖率表**先报出来的（SaimHann 那栏多了「动词 costifcontrol」）——
            //   这条断言就是把它钉死，别让「加动词只加一半」再发生第二次。
            var ctx3 = ProbeBattle(new[] { fate }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx3, 3);
            ctx3.Players[0].Energy = 8;
            Place(ctx3, 1, 0, Unit("EFoe2", 1, 0, 9), exhausted: true);
            CheckCode(RuleCore.PlayTactic(ctx3, 0, HandIdx(ctx3, 0, "T_FateInescapable"), 0), RuleCodes.OK,
                      "打出 `Fate Inescapable`");
            CheckTrue(!Joined(ctx3).Contains("还没实现"),
                      "★ 结算表里登记过 `costifcontrol`（没登记的话这里会出现「动作 … 本版还没实现」）"
                      + LogTail(ctx3));
        }

        // ---- ③ `Each of your units deals damage equal to its Shuriken to a random enemy` ----
        //  🔴 数值取自**每一个单位自己** —— 不是求和、也不是施放者的值。
        {
            var r = EffectText.ParseSegment("Each of your units deals damage equal to its Shuriken to a random enemy");
            Check(r.Kind, EffectText.SegKind.Ok, "`Each of your units deals damage equal to its …` 认得出");
            if (r.Ops != null && r.Ops.Count > 0)
            {
                Check(r.Ops[0].Verb, "eachunitdeal", "动词 = eachunitdeal");
                Check(r.Ops[0].Payload, "shuriken", "数值来自 = shuriken");
            }

            var sudden = Tactic("T_SuddenAssault", 3,
                                "Each of your units deals damage equal to its Shuriken to a random enemy");
            var ctx = ProbeBattle(new[] { sudden }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 3);
            ctx.Players[0].Energy = 8;
            var foe = Place(ctx, 1, 0, Unit("EFoe2", 1, 0, 40), exhausted: true);
            Place(ctx, 0, 0, Unit("FSh2", 1, 1, 3, "Shuriken 2"), exhausted: true);
            Place(ctx, 0, 1, Unit("FSh3", 1, 1, 3, "Shuriken 3"), exhausted: true);
            Place(ctx, 0, 2, Unit("FNoSh", 1, 1, 3), exhausted: true);     // 没有星镖 ⇒ 跳过
            // ⚠️ 量的是**敌方全场总生命**，不是单个 `foe`：卡面写的是 `a random enemy`，
            //    而 `an enemy` 的目标池**含敌方督军**（`AddSide(..., troopOnly:false, ...)`）——
            //    钉死「一定打那个 troop」会假红（本轮踩到）。**总量对得上**才是要断言的东西。
            int before = SumHealth(ctx, 1);
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_SuddenAssault"), -1), RuleCodes.OK,
                      "打出 `Sudden Assault`");
            Check(before - SumHealth(ctx, 1), 5,
                  "★ 打掉 **2 + 3 = 5** —— 每个单位按**自己**的星镖值开火" + LogTail(ctx));
            // 反例：**没有星镖**的单位不许打 —— 它要是被算成「按 0 打」看不出来，
            // 但要是被算成「按施放者的值打」，它就白打一份。
            var ctx2 = ProbeBattle(new[] { sudden }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx2, 3);
            ctx2.Players[0].Energy = 8;
            Place(ctx2, 1, 0, Unit("EFoe2", 1, 0, 40), exhausted: true);
            Place(ctx2, 0, 0, Unit("FNoSh", 1, 1, 3), exhausted: true);    // 一个都没星镖
            int before2 = SumHealth(ctx2, 1);
            CheckCode(RuleCore.PlayTactic(ctx2, 0, HandIdx(ctx2, 0, "T_SuddenAssault"), -1), RuleCodes.OK,
                      "打出 `Sudden Assault`（我方一个单位都没星镖）");
            Check(SumHealth(ctx2, 1), before2,
                  "★ 反例：**没有星镖的单位不造成任何伤害**（不是「按 0 打」更不是「按别的值打」）"
                  + LogTail(ctx2));
        }

        // ---- ④ `Each friendly unit that is Praying heals 3` ----
        //  「正在祈祷」是**状态**（`UnitState.Prayed`，回合开始掉），由 `Pray` 替代行动置位。
        {
            var r = EffectText.ParseSegment("Each friendly unit that is Praying heals 3");
            Check(r.Kind, EffectText.SegKind.Ok, "`Each friendly unit that is Praying heals 3` 认得出");
            if (r.Ops != null && r.Ops.Count > 0)
            {
                Check(r.Ops[0].Verb, "heal", "动词 = heal（归一成 `heal 3 to all friendly units that are praying`）");
                CheckTrue(r.Ops[0].Target != null && r.Ops[0].Target.PrayedOnly,
                          "★ 目标标成 **`PrayedOnly`**");
            }
            // 条件那一半（同一族，`Sororitas Rhino`）：`if any friendly unit is Praying`
            Check(EffectCondition.Normalize("any friendly unit is Praying"), "anypraying",
                  "★ `if any friendly unit is Praying` 归一成 `anypraying`");

            var devout = Tactic("T_DevoutSerenity", 3,
                                "Give Shield to your units. Each friendly unit that is Praying heals 3");
            var prayer = new CardDef("FPrayer", "FPrayer", "unit", "Pray: Gain Shield",
                                     "common", "Test", 2, 1, 6, 0, new[] { "Pray" }, subtype: "Infantry");
            var ctx = ProbeBattle(new[] { devout }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 3);
            ctx.Players[0].Energy = 8;
            var a = Place(ctx, 0, 0, prayer);                 // 会祈祷
            var b = Place(ctx, 0, 1, prayer, exhausted: true); // 不会（下面不给它用 Pray）
            a.Health = 3; b.Health = 3;                        // 都打残，好看治疗
            CheckCode(RuleCore.UseAlternative(ctx, 0, 0, "pray"), RuleCodes.OK, "0 号格执行 `Pray`");
            CheckTrue(a.Prayed, "★ 用过 `Pray` 之后 `Prayed = true`（**状态**，不是事件）");
            CheckTrue(!b.Prayed, "没祈祷过的那张 `Prayed = false`");
            // ⚠️ 这张卡**要选一个目标格位**：`Give Shield to your units` 里的 `your units`
            //    解出来是 `Count = 1`（卡面没写 `all`）⇒ `CanPlayTactic` 要求给格位。这是既有口径。
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_DevoutSerenity"), 0), RuleCodes.OK,
                      "打出 `Devout Serenity`");
            Check(a.Health, 6, "★ 祈祷过的那个**治了 3**（3 → 6）" + LogTail(ctx));
            Check(b.Health, 3, "★ 反例：**没祈祷的那个一点没治** —— 目标筛没接上的话它会跟着一起治"
                             + LogTail(ctx));

            // 反例二：状态**按回合掉**（`rule_core.gd:1953`）—— 转一圈回到自己回合就不该再算了。
            // ⚠️ 要 **两次** `PassTurn`：`RefreshForNewTurn` 只刷**当前行动方**的单位，
            //    一次之后轮到对手，自己这边还没刷（这是既有口径，不是 bug）。
            PassTurn(ctx); PassTurn(ctx);
            CheckTrue(!a.Prayed, "★ 过了一个自己的回合开始 ⇒ `Prayed` 复位（不是「本局一直算」）");
        }

        // ---- ⑤ 付费**修饰型**激活：`6 [Energy]: Extend effect until your next turn` ----
        //  语义：付钱 → **撤销**基础效果 → 用 `this turn` → `until your next turn` **重结算**。
        //  不付钱则保持原样（`rule_core.gd:1799`「放弃（基础已结算）」）。
        {
            var r = EffectText.ParseSegment("6 [Energy]: Extend effect until your next turn");
            Check(r.Kind, EffectText.SegKind.Ok, "`6 [Energy]: Extend effect until your next turn` 认得出");
            if (r.Ops != null && r.Ops.Count > 0)
            {
                Check(r.Ops[0].Verb, "paidmod", "动词 = paidmod");
                Check(r.Ops[0].Payload, "extend", "修饰种类 = extend");
                Check(r.Ops[0].Cost, 6, "代价 6");
            }

            const string featDesc =
                "Give Shield to a friendly unit this turn. 6 [Energy]: Extend effect until your next turn";
            var feat = Tactic("T_MiraculousFeat", 3, featDesc);
            var ops = EffectText.Parse(featDesc, out _, out _);
            CheckTrue(ops.Count == 2 && ops[1].Verb == "paidmod"
                      && ops[1].BaseOps != null && ops[1].BaseOps.Count == 1,
                      "★ `Parse` 把**本卡在它之前**那条效果回填进了 `BaseOps`（逐句解析时看不到「之前」）");

            // 付了钱：增益是「到你下个回合」
            var ctx = ProbeBattle(new[] { feat }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 3);
            // ⚠️ 手动给够能量：要 **3 费 + 6 激活 = 9**。`ToP1Turn(8)` 那條路走不通 ——
            //    `ProbeBattle` 的牌库几乎是空的，推到第 8 个回合累積疲劳会把督军打死（本轮实测踩到）。
            ctx.Players[0].Energy = 20;
            var friend = Place(ctx, 0, 0, Unit("FFriend", 1, 1, 5));
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_MiraculousFeat"), 0), RuleCodes.OK,
                      "打出 `Miraculous Feat` 并付 6 点能量激活");
            CheckTrue(friend.TempBuffs.Count == 1 && friend.TempBuffs[0].UntilMyNextTurn,
                      "★ 增益是 **`until your next turn`**（付了钱才延长）" + LogTail(ctx));
            CheckTrue(friend.Has("shield"), "Shield 确实在（重结算真的加了回去）" + LogTail(ctx));

            // 没付钱（能量不够）：保持 `this turn` —— 那是**基础效果**，不该被撤销
            var ctx2 = ProbeBattle(new[] { feat }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx2, 2);
            ctx2.Players[0].Energy = 4;                // 付得起 3 费，付不起 6 激活
            var friend2 = Place(ctx2, 0, 0, Unit("FFriend", 1, 1, 5));
            CheckCode(RuleCore.PlayTactic(ctx2, 0, HandIdx(ctx2, 0, "T_MiraculousFeat"), 0), RuleCodes.OK,
                      "打出（能量不够，激活那半不生效）");
            CheckTrue(friend2.Has("shield"), "★ 反例：**没付钱也照旧有 Shield**（基础效果不是「付了才有」）"
                                           + LogTail(ctx2));
            CheckTrue(friend2.TempBuffs.Count == 1 && !friend2.TempBuffs[0].UntilMyNextTurn,
                      "★ 反例：没付钱就是 **`this turn`**，不许被延长" + LogTail(ctx2));
        }

        // ---- ⑥ `Each player deploys 3 troops from their deck`（**对称部署**）----
        {
            var r = EffectText.ParseSegment("Each player deploys 3 troops from their deck");
            Check(r.Kind, EffectText.SegKind.Ok, "`Each player deploys N … from their deck` 认得出");
            if (r.Ops != null && r.Ops.Count > 0)
            {
                Check(r.Ops[0].Verb, "deploy", "动词 = deploy");
                CheckTrue(r.Ops[0].EachPlayer, "★ 标成 **`EachPlayer`**（双方各来一次）");
                Check(r.Ops[0].Amount, 3, "张数 3");
                Check(r.Ops[0].DeployFrom, "deck", "从牌库");
            }

            var saga = Tactic("T_BirthOfaSaga", 3,
                              "Each player deploys 3 troops from their deck. Your troops deployed this way gain Flank and Armour 3 this turn");
            var ctx = ProbeBattle(new[] { saga }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 3);
            ctx.Players[0].Energy = 8;
            // ⚠️ 牌库里那几张**必须和自己督军同阵营**：`CreatePool.Resolve` 按 `casterFaction` 筛
            //    （`CreatePool.cs:124/131`），塞 `Test` 阵营的卡进去会**一张都筛不到**。
            for (int i = 0; i < 3; i++)
            {
                ctx.Players[0].Deck.Add(ctx.NewInstance(new CardDef("FMine" + i, "FMine" + i, "unit", "", null,
                                                    "Ultramarines", 1, 1, 3, 0, null, subtype: "Infantry")));
                ctx.Players[1].Deck.Add(ctx.NewInstance(new CardDef("ETheirs" + i, "ETheirs" + i, "unit", "", null,
                                                    "Goff", 1, 1, 3, 0, null, subtype: "Infantry")));
            }
            int mineBefore = CountName(ctx, 0, "FMine"), theirsBefore = CountName(ctx, 1, "ETheirs");
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_BirthOfaSaga"), -1), RuleCodes.OK,
                      "打出 `Birth of a Saga`");
            Check(CountName(ctx, 0, "FMine") - mineBefore, 3, "★ **我方**部署了 3 个" + LogTail(ctx));
            Check(CountName(ctx, 1, "ETheirs") - theirsBefore, 3,
                  "★ **对手也部署了 3 个**（卡面是 `Each player`）—— 只给自己部署的话这里会是 0" + LogTail(ctx));
            // 后半句 `Your troops deployed this way gain Flank and Armour 3 this turn`
            var u0 = FirstName(ctx, 0, "FMine");
            CheckTrue(u0 != null && u0.Has("flank"),
                      "★ 刚部署的那批拿到了 Flank（`deployed this way` 指的是**上一句刚部署的**）" + LogTail(ctx));
            var e0 = FirstName(ctx, 1, "ETheirs");
            CheckTrue(e0 != null && !e0.Has("flank"),
                      "★ 反例：**对手**那批**没有** Flank（卡面写的是 `**Your** troops`）" + LogTail(ctx));
        }
    }

    /// <summary>把战斗日志的尾巴拼进断言消息 —— 新加的用例红的时候能一眼看出**引擎当时说了什么**。</summary>
    static string LogTail(BattleContext ctx, int n = 6)
    {
        if (ctx == null || ctx.Events.Count == 0) return "";
        var sb = new StringBuilder("　⟪日志⟫ ");
        int from = System.Math.Max(0, ctx.Events.Count - n);
        for (int i = from; i < ctx.Events.Count; i++) sb.Append(ctx.Events[i]).Append(" ‖ ");
        return sb.ToString();
    }

    /// <summary>整条战斗日志拼成一串（给「不许出现某句话」这类断言用）。</summary>
    static string Joined(BattleContext ctx)
    {
        if (ctx == null || ctx.Events.Count == 0) return "";
        var sb = new StringBuilder();
        foreach (var e in ctx.Events) sb.Append(e).Append('\n');
        return sb.ToString();
    }

    /// <summary>名字**以 name 开头**的单位有几个（`FMine0/1/2` 这种带序号的夹具用）。
    /// ⚠️ 不能用全等 —— 写死全等的话 `FMine0` 匹配不上 `FMine`，断言会**假红**（本轮踩过）。</summary>
    static int CountName(BattleContext ctx, int p, string name)
    {
        int n = 0;
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            var u = ctx.Players[p].Board[s];
            if (u != null && u.Name != null && u.Name.StartsWith(name, System.StringComparison.Ordinal)) n++;
        }
        return n;
    }

    static UnitState FirstName(BattleContext ctx, int p, string name)
    {
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            var u = ctx.Players[p].Board[s];
            if (u != null && u.Name != null && u.Name.StartsWith(name, System.StringComparison.Ordinal)) return u;
        }
        return null;
    }

    /// <summary>某一方**全场总生命**（含督军）—— 「随机打一个敌人」这类断言的稳妥量法：
    /// 随机选谁不写死，但**总量必须对得上**。</summary>
    static int SumHealth(BattleContext ctx, int p)
    {
        int n = 0;
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            var u = ctx.Players[p].Board[s];
            if (u != null && u.IsAlive) n += u.Health;
        }
        return n;
    }

    static void CheckTrue(bool cond, string msg) { Check(cond, true, msg); }

    // ==================================================================
    //  夹具
    //
    //  刻意**走真实的 NewBattle 路径**（shuffle:false），而不是手动拼 BattleContext ——
    //  这样开局逻辑本身也在被验，夹具不会和实现悄悄漂移。
    // ==================================================================

    static CardDef Unit(string name, int cost, int atk, int hp, params string[] kws)
    {
        return new CardDef(name, name, "unit", "", null, "Test",
                           cost, atk, hp, 0, kws);
    }

    static CardDef Ranged(string name, int cost, int atk, int hp, int ranged, params string[] kws)
    {
        return new CardDef(name, name, "unit", "", null, "Test",
                           cost, atk, hp, ranged, kws);
    }

    static CardDef Hero(string name, int atk, int hp)
    {
        return new CardDef(name, name, "hero", "", null, "Test", 0, atk, hp, 0, null);
    }

    /// <summary>一张**战术卡**。`desc` 走 `Core/EffectText` 那套（原版卡面文本的语法）。</summary>
    static CardDef Tactic(string name, int cost, string desc)
    {
        return new CardDef(name, name, "tactic", desc, "common", "Test", cost, 0, 0, 0, null);
    }

    /// <summary>
    /// 组一副「起手就是 hand 的顺序」的牌库。
    /// 抽牌是 `pop_back`，所以要把 hand **反转**放 —— 和 rule_core 的 `_deck()` 一个道理。
    /// 垫牌必须放在**反转后的 hand 前面**（即牌库的开头），否则 pop_back 先抽到的是垫牌。
    /// </summary>
    static List<CardDef> Deck(params CardDef[] hand)
    {
        var d = new List<CardDef> { Hero("FixtureWarlord", 2, 30) };
        for (int i = 0; i < 20; i++) d.Add(Unit("filler" + i, 1, 0, 1));
        for (int i = hand.Length - 1; i >= 0; i--) d.Add(hand[i]);
        return d;
    }

    static BattleContext Battle(CardDef[] hand0, CardDef[] hand1)
    {
        return RuleCore.NewBattle(Deck(hand0), Deck(hand1), seed: 0, shuffle: false);
    }

    /// <summary>带**卡池**的夹具 —— 造牌（`create`）要从卡池里筛候选，不带池子就算不出来。</summary>
    static BattleContext BattlePool(CardDef[] hand0, CardDef[] hand1, IList<CardDef> pool,
                                    string warlordFaction = "Test", int seed = 0)
    {
        return RuleCore.NewBattle(DeckOf(warlordFaction, hand0), Deck(hand1),
                                  seed: seed, shuffle: false, cardPool: pool);
    }

    /// <summary>和 `Deck` 一样，但督军带阵营（造牌要按施放者阵营筛卡）。</summary>
    static List<CardDef> DeckOf(string warlordFaction, params CardDef[] hand)
    {
        var d = new List<CardDef> { HeroOf("FixtureWarlord", warlordFaction, 2, 30) };
        for (int i = 0; i < 20; i++) d.Add(Unit("filler" + i, 1, 0, 1));
        for (int i = hand.Length - 1; i >= 0; i--) d.Add(hand[i]);
        return d;
    }

    static CardDef HeroOf(string name, string faction, int atk, int hp)
    {
        // ⚠️ **`subtype` 必须给 `"Warlord"`**（2026-09-13 A4 批 2 补）——
        //    真卡的 56 个督军**全都**是 `subtype = "Warlord"`（实测 `cards_engine.json`）。
        //    不给的话这个督军会掉进「原版没给兵种」那一类，而**那类是保留下来的**
        //    （`ApplyTargetFilters` 的纪律：查不到兵种的卡不许悄悄排除）⇒
        //    卡面写 `each friendly Beast` 时**督军会被当成 Beast** 一起去打（夹具失真，不是引擎错）。
        return new CardDef(name, name, "hero", "", null, faction, 0, atk, hp, 0, null,
                           subtype: "Warlord");
    }

    /// <summary>
    /// **只有「督军 + 指定的那几张牌」的干净牌库** —— 给「这张牌后来怎么了」这类断言用。
    ///
    /// ⚠️ 为什么不直接用 `BattlePool`：那个会塞 **20 张 filler**。对「临时卡被扫走了几张」
    ///    这种**按卡对象计数**的断言，filler 里混一张同类卡就会把数搅乱；
    ///    而 `fixture` 卡若同时被塞进牌库，**牌库里那 20 份也会一起被扫**。
    ///    （本轮实测踩到：探针卡进了牌库 ⇒ 一次 `EndTurn` 扫出 2 份、手牌四张。）
    /// 顺序按 `DeckOf` 的约定：**督军在最前**，其余**倒序**（抽牌是 `pop_back`，所以最后入列的先抽到）。
    /// </summary>
    static BattleContext ProbeBattle(CardDef[] hand0, CardDef[] hand1)
    {
        var d0 = new List<CardDef> { HeroOf("FixtureWarlord", "Ultramarines", 2, 30) };
        for (int i = hand0.Length - 1; i >= 0; i--) d0.Add(hand0[i]);
        var d1 = new List<CardDef> { HeroOf("FixtureWarlord", "Goff", 2, 30) };
        for (int i = hand1.Length - 1; i >= 0; i--) d1.Add(hand1[i]);
        return RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: new List<CardDef>());
    }

    static void PassTurn(BattleContext ctx)
    {
        RuleCore.EndTurn(ctx);
        RuleCore.BeginTurn(ctx);
    }

    /// <summary>推进到「P1 的第 n 个回合」（和 rule_test.gd 的 `_to_p1_turn` 同构）</summary>
    static void ToP1Turn(BattleContext ctx, int n)
    {
        int target = 2 * n - 1;
        int guard = 0;
        while (ctx.Turn != target || ctx.Active != 0)
        {
            if (ctx.Turn == 0) RuleCore.BeginTurn(ctx);
            else PassTurn(ctx);
            if (++guard > 40) { CheckTrue(false, "回合推进超限，疑似死循环"); return; }
        }
    }

    static int HandIdx(BattleContext ctx, int p, string name)
    {
        var hand = ctx.Players[p].Hand;
        for (int i = 0; i < hand.Count; i++) if (hand[i].Card.Name == name) return i;   // 第 7 行第 2 步：手牌存实例
        return -1;
    }

    static UnitState Board(BattleContext ctx, int p, int slot) { return ctx.Players[p].Board[slot]; }

    /// <summary>把一个单位直接摆到场上（跳过部署流程，只为构造攻击场景）</summary>
    static UnitState Place(BattleContext ctx, int p, int slot, CardDef card, bool exhausted = false)
    {
        // 🔴 2026-09-18：夹具改成走**对局计数器**（`ctx.NewInstance`）—— 夹具的意思就是
        //    「这个单位已经按正常路径上了场」，所以它的实例也该从和真部署**同一个号段**发。
        //    （原来写 `new UnitState(card,false)` = **分离实例**（负数号），那会让下面这条不变量
        //     「真对局里场上的单位**没有**分离实例」在测试里**假红**。）
        var u = new UnitState(ctx.NewInstance(card), false) { Exhausted = exhausted };
        // 🆕 2026-09-16：夹具摆上去 = 「这一回合部署的」—— `RuleCore.PlayCard` / `DeployFree`
        // 两个真入口都写这个字段（誓约能力的「必须本回合部署」判据读它）。
        // ⚠️ 夹具本来**绕过**了那几个入口（见 `TestAuras` 的注释：光环要自己 `Recompose`）——
        //    这里补上，免得测试里的单位看起来像「上一回合就在场」。
        u.DeployedTurn = ctx.Turn;
        ctx.Players[p].Board[slot] = u;
        return u;
    }

    /// <summary>手牌里有没有这个**兵种**的卡（选牌自检用：挑了哪张是随机的，只能按兵种认）</summary>
    static bool HasSubtype(List<CardDef> hand, string subtype)
    {
        foreach (var c in hand) if (c.Subtype == subtype) return true;
        return false;
    }

    /// <summary>同上，但吃**实例手牌**（2026-09-18 第 7 行第 2 步：`Hand` 存的是 `CardInstance`）。</summary>
    static bool HasSubtype(List<CardInstance> hand, string subtype)
    {
        foreach (var h in hand) if (h.Card != null && h.Card.Subtype == subtype) return true;
        return false;
    }

    /// <summary>某个名字的单位在**哪一格**（找不到返回 -1）。选牌的部署走 `DeployFree`，
    /// 落点是**第一个空格**，所以不能写死槽号。</summary>
    static int SlotOf(BattleContext ctx, int p, string name)
    {
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            var u = ctx.Players[p].Board[s];
            if (u != null && u.Name == name) return s;
        }
        return -1;
    }

    // ==================================================================
    //  棋盘一致性
    // ==================================================================

    /// <summary>
    /// 规则引擎的棋盘和表现层的棋盘**必须是同一个**。
    ///
    /// 这条断言就是冲着「7 格 vs 9 格」那次事故加的：表现层自己拍了 7 格，
    /// 而规则引擎/规则书/原版资源三处都是 9 格 —— 两边各写各的，谁也不会报错，
    /// 直到玩家发现「第 8、9 格放不了牌」才暴露。
    /// 放在 Editor 测试里是因为只有这里能同时看见两个命名空间（Core/ 不依赖 Unity）。
    /// </summary>
    static void TestBoardMatchesPresentation()
    {
        Check(RuleEngine.BoardSpec.Size, CardPresentation.BoardLayout.SlotCount,
              "规则引擎棋盘格数 == 表现层格数");
        Check(RuleEngine.BoardSpec.WarlordSlot, CardPresentation.BoardLayout.WarlordSlot,
              "督军槽位号两边一致");
        Check(RuleEngine.BoardSpec.Size, 9, "棋盘 9 格（规则书 :30 / rule_core.gd:44 / MinionArea）");
        Check(RuleEngine.BoardSpec.WarlordSlot, 4, "督军在中位槽 4");
        Check(RuleEngine.BoardSpec.SlotsPerSide * 2 + 1, RuleEngine.BoardSpec.Size,
              "两侧各 4 格 + 督军 = 9");

        int deployable = 0;
        for (int s = 0; s < RuleEngine.BoardSpec.Size; s++)
            if (RuleEngine.BoardSpec.IsDeployable(s)) deployable++;
        Check(deployable, 8, "可部署格 8 个（规则书「两侧各 4 格共可部署 8 张部队卡」）");
        CheckTrue(!RuleEngine.BoardSpec.IsDeployable(RuleEngine.BoardSpec.WarlordSlot),
                  "督军格不可部署");
    }

    // ==================================================================
    //  卡牌数据
    // ==================================================================

    static void TestKeywordParsing()
    {
        // 卡面写法很脏（146 种不同串）—— 这几条是实测出来的真实形态
        var c = new CardDef("t", "T", "unit", "", null, null, 1, 1, 1, 0,
                            new[] { "Armour 1", "Long Range", "Can't Attack", "Blast 2", "Shield",
                                    "Blood Thirst", "Rally: Deal 3 damage to an enemy", "Armour 2." });
        Check(c.KwValue("armour"), 2, "同一张卡两处 Armour → 后者覆盖（Armour 2. 的句号不影响取数）");
        CheckTrue(c.Has("longrange"), "Long Range → longrange");
        CheckTrue(c.Has("cantattack"), "Can't Attack → cantattack");
        Check(c.KwValue("blast"), 2, "Blast 2 → blast 值 2");
        Check(c.KwValue("shield"), 1, "无数字的关键词「存在即真」取 1");
        CheckTrue(c.Has("bloodthirst"), "Blood Thirst → bloodthirst");
        CheckTrue(c.Has("rally"), "「Rally: 效果文字」只取冒号前的关键词部分");

        var bare = new CardDef("t2", "T2", "unit", "", null, null, 1, 1, 1, 0, new[] { "Armour" });
        Check(bare.KwValue("armour"), 1, "光写 Armour 不带数字 → 1（不是 0）");

        var none = new CardDef("t3", "T3", "unit", "", null, null, 1, 1, 1, 0, null);
        Check(none.Keywords.Count, 0, "keywords 为 null 不炸");
    }

    static void TestCardDatabase()
    {
        var pool = CardDatabase.Load();
        CheckTrue(pool.Count >= 1000, $"卡表加载：{pool.Count} 张（应 ≥ 1000）");

        // 🔴 关键词「认不出就被丢掉」必须**说出来**（红线：不许静默失败）。
        //    卡池现在是**干净的**（2026-09-15 清过 19 个词 / 25 张卡）⇒ 正常应当为空。
        //    **这一行红了 = 卡表里又混进了认不出的词**，怎么处置看两条：
        //      · 是**数据噪音**（天赋名/阵营名/兵种标签混进 keywords）→ 清 `cardface_fixes.json` 的 `_manual_keywords`
        //      · 是**真关键词**（卡面上确实有）→ 加进 `KeywordTable.Prefixes`，再给 `Implemented` 定去留
        //    分类依据：`资料/PnP卡图_逐张对账_0915.md` §六·五·A·⑥。
        CheckTrue(KeywordTable.Dropped.Count == 0,
                  KeywordTable.Dropped.Count == 0
                      ? "没有认不出的关键词（认不出就会被静默丢掉，见 `KeywordTable.Dropped`）"
                      : $"关键词被静默丢弃 {KeywordTable.Dropped.Count} 个：{string.Join(" / ", KeywordTable.Dropped.ToArray())}");

        int units = CardDatabase.Units(pool).Count;
        CheckTrue(units >= 500, $"单位卡 {units} 张（应 ≥ 500）");

        var autarch = CardDatabase.Find(pool, "Autarch");
        CheckTrue(autarch != null, "能找到 Autarch");
        if (autarch != null)
        {
            Check(autarch.Cost, 6, "Autarch 费用 6");
            Check(autarch.Attack, 5, "Autarch 近战 5");
            Check(autarch.Health, 6, "Autarch 生命 6");
            Check(autarch.RangedAttack, 6, "Autarch 远程 6（数据里 armor 字段实为远程，已迁移）");
        }

        var factions = CardDatabase.Factions(pool);
        CheckTrue(factions.Count >= 10, $"阵营 {factions.Count} 个（应 ≥ 10）");

        // 未实现关键词的量化 —— 不是让它静默失效，而是能一眼看到还差多少
        var unimplemented = RuleCore.UnimplementedKeywords(pool);
        // ⚠️ **别截断**（2026-09-13 改）。原来这里写的是 `GetRange(0, Math.Min(12, …))` + `"…"` ——
        //    于是日志里只有**字母序前 12 个**，剩下 4 个（`synapse` / `teleport` / `tide` / `uprising`）
        //    **谁都看不到**，想报个完整名单得回去改代码。这份名单是**工作清单**，就该一眼看全；
        //    而且它是 `SortedSet`（字母序），截断等于**把尾巴藏起来**。
        Debug.Log(P + $"   全卡池未实现关键词 {unimplemented.Count} 个："
                    + string.Join(" / ", unimplemented));
        CheckTrue(unimplemented.Count == 0,
                  "★ 未实现关键词应当**清零**（2026-09-14 起：`destroyer` / `ecstasy` / `sabotage` "
                  + "三个都做掉了）—— 非 0 就说明名单动了，回 `CardDef.Implemented` 对账；"
                  + "⚠️ **别为了让它变绿就随手登记**（没做的登记成已做，比名单多报一个更糟）");
    }

    /// <summary>
    /// **原版卡牌接进对战**（2026-09-12）—— 卡池 → 中文 → 组卡 这条链的数据断言。
    /// 「真的打进一局、卡面长什么样」在 `BattleScene.Run` 里验（那边有画面和截图）。
    /// </summary>
    static void TestOriginalCardPool()
    {
        var pool = CardDatabase.Load();

        // ① 中文并进卡表了没有（`工具/gen_cards_engine.py` 从 `数据/卡牌翻译/zh_cards.json` 并的）
        int zhName = 0, zhDesc = 0;
        foreach (var c in pool)
        {
            if (!string.IsNullOrEmpty(c.NameZh)) zhName++;
            if (!string.IsNullOrEmpty(c.DescZh)) zhDesc++;
        }
        CheckTrue(zhName >= 1100, $"有中文卡名 {zhName}/{pool.Count} 张（应 ≥ 1100）");
        CheckTrue(zhDesc >= 1100, $"有中文效果 {zhDesc}/{pool.Count} 张（应 ≥ 1100）");
        // 没翻译的那几张**不静默**：卡面回英文名，数量在这里报出来
        Debug.Log(P + $"   没有中文的 {pool.Count - zhName} 张（卡面回英文名，已知）");

        var desolation = CardDatabase.Find(pool, "Desolation Marine");
        CheckTrue(desolation != null, "能找到 Desolation Marine");
        if (desolation != null)
        {
            CheckTrue(!string.IsNullOrEmpty(desolation.NameZh),
                      $"中文名拿得到：「{desolation.NameZh}」");
            CheckTrue(desolation.FromOriginalPool, "标了「来自原版卡池」→ 卡面文字取它自己的效果原文");
        }
        // 我们自己设计的卡**不该**被标成原版卡（否则卡面会去显示风味文字）
        var ember = StarterCards.Ember()[0];
        CheckTrue(!ember.FromOriginalPool, "自设计卡没被标成原版卡（卡面仍走引擎关键词那套）");

        // ② 两个阵营的体量 —— 挑它们就是因为单位够多、够凑满 30 张
        int um = CardDatabase.OfFaction(pool, "Ultramarines").Count;
        int goff = CardDatabase.OfFaction(pool, "Goff").Count;
        CheckTrue(um >= 130, $"Ultramarines {um} 张（应 ≥ 130）");
        CheckTrue(goff >= 115, $"Goff {goff} 张（应 ≥ 115）");

        // ③ 阵营名走错池子要**报错**，不能静默替一套（原来 `Of()` 是「不是 Tide 就当 Ember」）
        Check(StarterCards.Of("Ultramarines").Count, 0,
              "StarterCards.Of(原版阵营) 返回空表 —— 不再静默给一套 Ember");
        Check(StarterCards.Of(StarterCards.EmberFaction).Count, 13, "StarterCards.Of(Ember) 照常 13 张");

        // ④ 两个阵营各凑一副，逐条查构成
        foreach (var fac in new[] { "Ultramarines", "Goff" })
        {
            var deck = DeckBuilder.StarterDeck(pool, fac, DeckBuilder.ClassicDeckSize, new System.Random(7));
            Check(deck.Count, DeckBuilder.ClassicDeckSize, $"{fac} 凑满 {DeckBuilder.ClassicDeckSize} 张");
            Check(deck[0].Type, "hero", $"{fac} 第 0 张是督军");
            CheckTrue(deck[0].FromOriginalPool, $"{fac} 的牌全来自原版卡池");

            int notUnit = 0, foreign = 0;
            foreach (var c in deck)
            {
                if (c.Type != "hero" && c.Type != "unit") notUnit++;
                if (c.Faction != fac) foreign++;
            }
            Check(notUnit, 0, $"{fac} 牌组里除督军外**全是单位卡**（战术/防御没混进来）");
            Check(foreign, 0, $"{fac} 牌组全同阵营");
            Debug.Log(P + $"   {fac} 曲线：" + string.Join(" ", DeckBuilder.CostCurve(deck)));
        }

        // ⑤ 稀有度必须以**卡面宝石扫描表**为准，不许退回 OCR 的 `card_stats.json.rarity`
        //
        //    ⚠️ 2026-09-12 抓到的真 bug：`工具/gen_cards_engine.py` 原来写成
        //    `if rarity not in RARITIES:` —— 只在 OCR 值是**非法串**（空串 / `defence`）时才查宝石表，
        //    于是 OCR 写错但「看起来合法」的值原样放行：**140 张被记成 `legendary` 的卡**一直没纠
        //    （改之前 legendary 341 张，宝石实测只有 197 张）。
        //    影响面不只是显示：`DeckRules.CopyLimit` 按稀有度定同名牌上限（传说 1 张、其余 2 张），
        //    所以这 140 张的组卡规则一直是错的。
        //
        //    下面四张是**对着卡面核过**的（底部那颗菱形宝石的颜色）：
        //      `D:/2/Warpforge部队卡片/Aeldari/3部队/Warpforge_08_Night-Spinner.png` → 浅蓝 = common
        //      `…/Aeldari/3部队/Warpforge_03_Shining-Spear.png`                      → 浅蓝 = common
        foreach (var (name, want) in new[]
                 {
                     ("Autarch", "common"),
                     ("Night Spinner", "common"),
                     ("Shining Spear", "common"),
                     ("Ursula Creed", "epic"),
                 })
        {
            var rc = CardDatabase.Find(pool, name);
            CheckTrue(rc != null, $"找得到 {name}");
            if (rc != null) Check(rc.Rarity, want, $"{name} 的稀有度 = {want}（卡面宝石实测）");
        }
        // **同名不同卡**（卡名当 id 的后果）：宝石表按卡名查，重名时后写的赢，
        // 所以这几张必须按「阵营 + 卡名」分开钉住。同样是对着卡面核的：
        //   `Chaos/3部队/Warpforge_44_Terminator-Champion.png`（Black Legion）→ 浅蓝 common
        //   `Chaos/3部队/Warpforge_46_Maulerfiend.png`（Black Legion）          → 紫   epic
        //   `Dark Angels/3部队/Warpforge_26_Bladeguard-Veteran.png`             → 绿   rare
        //   ⚠️ 宝石表里那行**批次 U（= Ultramarines）的 Bladeguard Veteran（common）在我们卡池里没有**
        //      —— `card_stats.json` 上游就缺这张（卡面图 `Ultramarines/3部队/Warpforge_34_…` 是有的），
        //      已记进 `资料/卡牌数据源对账_0912.md` 的「只有文档有」。所以这里只钉 DarkAngels 那张。
        foreach (var (faction, name, want) in new[]
                 {
                     ("BlackLegion", "Terminator Champion", "common"),
                     ("EmperorsChildren", "Terminator Champion", "epic"),
                     ("BlackLegion", "Maulerfiend", "epic"),
                     ("EmperorsChildren", "Maulerfiend", "common"),
                     ("DarkAngels", "Bladeguard Veteran", "rare"),
                 })
        {
            CardDef rc = null;
            foreach (var c in pool)
                if (c.Faction == faction && c.Name == name) { rc = c; break; }
            CheckTrue(rc != null, $"找得到 {faction} 的 {name}");
            if (rc != null) Check(rc.Rarity, want, $"{faction} 的 {name} 稀有度 = {want}（重名分开定档）");
        }
        int legend = 0;
        foreach (var c in pool) if (DeckRules.IsLegendary(c.Rarity)) legend++;
        CheckTrue(legend <= 230, $"传说卡 {legend} 张（应 ≤ 230 —— OCR 误判那版是 341）");

        // ⑥ 数值修正 —— OCR 把**紫圆（远程）**读错/漏读的那几张（`gen_cards_engine.py` 的 `STAT_FIXES`）。
        //    卡面四个圆的出处（原版 prefab 节点名 + 坐标，见 `Core/CardView.cs:121`）：
        //      **费用 = 右上蓝圆 · 近战 = 左下红圆 · 远程 = 左下偏右的紫圆 · 生命 = 右下绿**
        //      **护甲 = 右侧那枚盾牌**（不在任何一个圆里）
        //    ⚠️ 这几张是**抽 43 张开图**时抓到的，不是全量核对 —— 同类错还有多少没人量过。
        foreach (var (name, field, want) in new[]
                 {
                     ("Baneblade Tank", "ranged", 12),       // 图 Astra Militarum/3部队/Warpforge_43_Baneblade-Tank.png：紫圆 12、盾 2
                     ("Haarken Worldclaimer", "ranged", 2),  // 图 Chaos/1督军/Warpforge_3_Haarken-Worldclaimer.png
                     ("Lord Kaphrael", "ranged", 2),         // 图 Emperor_s Children/1督军/Warpforge_01_Lord-Kaphrael.png
                     ("Veldras the Sublime", "ranged", 1),   // 图 Emperor_s Children/3部队/Warpforge_24_Veldras-the-Sublime.png
                     ("Predator Annihilator", "ranged", 7),  // 图 Ultramarines/3部队/predator anihilator.png
                     ("Smothering Decree", "cost", 2),       // 图 Dark Angels/6秘密/IMG_3699.jpg：蓝圆 2
                 })
        {
            var sc = CardDatabase.Find(pool, name);
            CheckTrue(sc != null, $"找得到 {name}");
            if (sc == null) continue;
            int got = field == "ranged" ? sc.RangedAttack : sc.Cost;
            Check(got, want, $"{name} 的 {field} = {want}（卡面实测）");
        }

        // ⑦ 三份 0824 卡表**没收录**的那 9 张（DA 6秘密 / GSC 6破坏卡，手机翻拍没进 OCR 流水线）
        //    —— 稀有度只能对着卡面宝石定：秘密卡橙红=special、破坏卡浅蓝=common。
        foreach (var (name, want) in new[]
                 {
                     ("Convoke the Circle", "special"), ("None Must Know", "special"),
                     ("Obscure Ritual", "special"), ("Rites of Penance", "special"),
                     ("Smothering Decree", "special"),
                     ("Jammed Communications", "common"), ("Poisoned Supplies", "common"),
                     ("Improvised Barricade", "common"), ("Cult Propaganda", "common"),
                 })
        {
            var uc = CardDatabase.Find(pool, name);
            CheckTrue(uc != null, $"找得到 {name}（表外卡）");
            if (uc != null) Check(uc.Rarity, want, $"{name} 稀有度 = {want}（卡面宝石实测）");
        }
    }

    /// <summary>
    /// **战术卡文本解析的覆盖率** —— 这一轮的进度条（`资料/战术卡效果_移植方案.md`）。
    ///
    /// 448 张战术卡的效果文本是英文自然语言，解析器（`Core/EffectText.cs`）照
    /// `rule_core.gd:_resolve_text` 的 handler 顺序一条条试。每加一个 handler，
    /// **「完全解析 N/448」这个数就该往上走** —— 这就是「还差多少」的量化口径。
    ///
    /// ⚠️ 报的是**两个**口径，别混：
    ///   · **完全解析** —— 整条 desc 每一句都认了（这才叫「这张卡能打」）；
    ///   · **部分解析** —— 有话认了、有话没认（**半懂的卡比不懂更危险**，单独报）。
    /// </summary>
    static void TestTacticTextCoverage()
    {
        var pool = CardDatabase.Load();
        // 造牌那条要**真算一遍候选池**（`CreatePool`）—— 池子算不出来就算「没机制」。
        // 卡池只能由调用方给进来（`Core/` 不认识 UnityEngine，读不了 Resources）。
        var cov = EffectText.Coverage(pool, "tactic", pool);

        // ⚠️ 449 而不是 448：2026-09-12 捞回了 `Dark Pact of Fate` —— 它在源表里 `cost: null`
        //    （OCR 没读到），被 `hasStats=false` 挡在卡池外；四条独立证据确认它是真卡
        //    （同 subtype 的三兄弟都在池里且都是 1 费、有中文翻译、效果与规则书 :179 一致）。
        //    收录依据写在 `工具/gen_cards_engine.py` 的 `STATS_EXCEPTIONS` 里。**改这个数要同时改那里。**
        // ⚠️ 2026-09-15：449 → **450** —— PnP 逐张对账补回了黑军团的天赋卡 `Chosen of the Four`
        //    （阿巴顿的天赋，引擎里原来整张缺 ⇒ `RuleCore.SpawnTalents` 永远生成不出来）。
        //    见 `资料/PnP卡图_逐张对账_0915.md` §四·D。
        // ⚠️ 2026-09-15 同一天：450 → **445** —— 删掉 5 张**幽灵卡**（同一张卡在卡池里两行、
        //    一行 `tactic` 一行 `defence`、desc 一字不差）。判据：13 个阵营的 3 张防御卡一张不缺
        //    （与 PnP `5防御卡/` 逐张对上）、预组卡组只引用编号型 id ⇒ 留编号型那行。
        //    其中 4 张是删 tactic 行、1 张（`Rusted Vents`）是 **tactic → defence**（`GSC68`）。
        //    收录依据写在 `card_stats.json` 那 5 行的 `noise` / `noise_reason` 里。
        //    见 `资料/PnP卡图_逐张对账_0915.md` §六·3。**改这个数要同时改那里。**
        Check(cov.Cards, 445, "战术卡张数");
        // 分类必须**不重不漏**：每一句都恰好落进一个桶（抓计数 bug）
        Check(cov.SegKeyword + cov.SegOk + cov.SegPartial + cov.SegUnknown, cov.SegTotal,
              "分句分类总数 = 分句总数（不重不漏）");
        Debug.Log(P + "   " + cov.Summary());

        // 频次最高的「不认识的句子」= 下一个该实现的 handler（按量排，不按感觉排）
        var top = new List<KeyValuePair<string, int>>(cov.UnknownFreq);
        top.Sort((a, b) => b.Value.CompareTo(a.Value));
        Debug.Log(P + "   最常出现、还没实现的句子（TOP8）：");
        for (int i = 0; i < top.Count && i < 8; i++)
            Debug.Log(P + $"     ×{top[i].Value,-3} {top[i].Key}");

        var topP = new List<KeyValuePair<string, int>>(cov.PartialFreq);
        topP.Sort((a, b) => b.Value.CompareTo(a.Value));
        if (topP.Count > 0)
        {
            Debug.Log(P + "   句型认了、但目标/载荷词表里没有的（TOP5）：");
            for (int i = 0; i < topP.Count && i < 5; i++)
                Debug.Log(P + $"     ×{topP[i].Value,-3} {topP[i].Key}");
        }

        // **第二层：载荷有没有机制** —— 解析得出来但关键词没实现 = 「能打但没用」，是静默失效。
        // 按频次报出来，决定下一步补哪个关键词（`rule_core.gd:52` 的 `KW_IMPLEMENTED` 有 59 个，我们只有 13 个）。
        Debug.Log(P + $"   载荷有机制 {cov.FullAndMechanized}/{cov.Full}（在「完全解析」的卡里再过一层）");
        var topM = new List<KeyValuePair<string, int>>(cov.NoMechFreq);
        topM.Sort((a, b) => b.Value.CompareTo(a.Value));
        for (int i = 0; i < topM.Count && i < 8; i++)
            Debug.Log(P + $"     ×{topM[i].Value,-3} {topM[i].Key}");

        // ---- 「能打」的判据要不要再加一条「动词实现了」？先量出来再决定 ----
        // 现在 `IsFullyParsed` **只看解析**，被三处共用（`CanPlayTactic` / `DeckBuilder.TacticPlayable` /
        // 卡面打 `*`）。于是效果全靠没实现动词的那批卡：不打 `*`、收得进卡组、打得出去、**什么都不发生**。
        // 下面这个数就是「收紧判据会让多少张卡变成不能打」——**决策要数字，不要估计**。
        {
            int allVerbsOk = 0, anyVerbMissing = 0, allVerbsMissing = 0;
            var examples = new List<string>();
            var seen = new HashSet<string>();
            foreach (var c in pool)
            {
                if (c == null || c.Type != "tactic") continue;
                var ops = EffectText.Parse(c.Desc, out var un, out var pa);
                if (un.Count > 0 || pa.Count > 0) continue;         // 只看「完全解析」的
                bool any = false, all = true, missing = false;
                foreach (var op in ops)
                {
                    if (RuleCore.ImplementedEffectVerbs.Contains(op.Verb)) { all = false; any = true; }
                    else { missing = true; }
                }
                if (!missing) allVerbsOk++;
                else { anyVerbMissing++; if (!any) { allVerbsMissing++; if (seen.Add(c.Name)) examples.Add(c.Name); } }
            }
            Debug.Log(P + $"   「能打」判据实测：完全解析的 {cov.Full} 张里，"
                      + $"每条动词都实现了的 **{allVerbsOk}** 张、"
                      + $"有动词没实现的 {anyVerbMissing} 张（其中**整张卡全靠没实现的动词** {allVerbsMissing} 张）");
            if (examples.Count > 0)
                Debug.Log(P + "     整张卡都跑不起来的（严格口径下会变成不能打）："
                          + string.Join("、", examples.ToArray(), 0, System.Math.Min(12, examples.Count)));
            CheckTrue(allVerbsOk > 0, $"有条动词全实现的战术卡（{allVerbsOk} 张）");
        }

        // ---- 按**阵营**看覆盖率：做单个阵营时，这张表就是工作清单 ----
        ReportFactionCoverage(pool);

        // ---- 效果分类表：**这套分类本来只活在代码里**，没有一处写下来过 ----
        // 一张卡的效果 = (动词, 载荷, 目标, 条件, 时机) 的组合。这张表把 449 张按**动词**分堆，
        // 每堆再看载荷/目标/条件。用处有三个：
        //   ① 看「15 个动词」这套分法**对不对**（有些东西混进来了，见下面日志里的告警）
        //   ② 加新效果时知道该往哪一格里放（`资料/卡牌效果管线_计划与交接.md` §七）
        //   ③ 覆盖率的缺口按**格子**看，而不是按句子看
        DumpTaxonomy(pool, cov);

        // 全量落盘 —— TOP8 只够看个热闹，**排优先级要全量**（按频次降序）。
        // 落这里而不是 Assets/：是给人看的排查产物，不是资产。
        DumpUnparsed(cov);

        // 现阶段门槛：骨架已通（有卡能完全解析）、且一张卡都没有解析成空表也不报错。
        // 每补一个 handler 就把这个数往上抬（抬的时候顺手在提交信息里记一笔）。
        CheckTrue(cov.Full >= 120, $"完全解析 {cov.Full}/448（门槛 120，逐步抬高）");
        // 🆕 2026-09-16：**战术卡这一档的「不认识的句子」归零了**。
        // 这条原来是**正向哨兵**（`SegUnknown > 0` = 「还没做完，如实报出来」）——
        // 最后三句（`Cosmic Serpent` / `Spreading Corruption` 后半 / `Telephatic Domination`）
        // 接完之后它就是 0 了。现在按新事实改写成**回归哨兵**：
        // **只要它是不是 0，就说明新出现了一句不认识的**（而不是「还没做完」）。
        // ⚠️ 单位 / 督军 / 防御卡那三档在 `ReportUnitDescCoverage` 里各量各的，别混。
        Check(cov.SegUnknown, 0,
              "★ 战术卡「完全不认识的句子」= 0（2026-09-16 归零；>0 = 有回归，去 `_tmp_view/tactic_unparsed.txt` 看）");
    }

    // ==================================================================
    //  战术卡「能打」（引擎层：解析 → 结算 → 弃牌堆）
    // ==================================================================

    /// <summary>
    /// 战术卡的**结算链**：`EffectText.Parse` → `RuleCore.PlayTactic` → 状态真的变了 → 进弃牌堆。
    ///
    /// 覆盖三类：合成的卡（可控）、**真卡池里的原版卡**（真数据）、以及**解析不了的卡必须被拒绝**
    /// （不许「扣了费什么都不发生」—— 那是最难查的一类 bug）。
    /// </summary>
    /// <summary>
    /// **防御卡**（39 张）—— 2026-09-13 第三十三轮。
    ///
    /// 以前的状态是「引擎里除了组卡校验**没有任何地方认识 `defence`**」，`CanPlayTactic` 直接
    /// `return ErrUnimplemented`，`DeckBuilder` 也把它丢掉。这一轮把它接上了，判据是：
    /// **规则书 `:105` 说防御卡就是「后手可打出的特殊战术」**（`rule_core.gd:4696` 也写「防御卡=计策类」），
    /// 而且实测 **39/39 的 desc 都能完整解析**、动词全是已实现的那批 —— 所以**复用战术卡那一整条链**，
    /// **不另开一套机制**。这条测试就是钉「复用的是同一条链」。
    /// </summary>
    /// <summary>
    /// **`When &lt;事件&gt;` 铺宽**（2026-09-13 第三十三轮）—— 新接的几种事件**真能触发**。
    ///
    /// 判据不是「解析得出」（那只是注册成功），而是**局面真的变了**：
    /// 每条都钉「发生了 → 有效果」**和**「不该发生 → 没效果」两面。
    /// ⚠️ 这一节存在的理由：**「认得出」和「发得出」是两件事** ——
    ///    只认得出的话，监听器**永远收不到**，而卡面又不会打 `*`（因为解析是好的）
    ///    ⇒ 那是**对玩家说谎**（卡上写着会触发，实际永远不触发）。
    /// </summary>
    static void TestWhenEventsWidened()
    {
        var pool = CardDatabase.Load();
        var troop = CardDatabase.Find(pool, "Intercessor", "DarkAngels");      // 有 `When` 的监听器别选它
        CheckTrue(troop != null, "挑得到一张普通 troop 当尺子（`Intercessor`）");

        // ---- ① `When you play a troop` —— 自己打出会触发 ----
        {
            var watcher = new CardDef("W1", "W1", "unit", "When you play a troop, gain +1 Attack",
                                      "common", "Test", 1, 2, 9, 0, null, subtype: "Infantry");
            var played = Unit("Pawn", 1, 1, 1);          // 1 费，回合 1 打得起
            var ctx = Battle(new[] { played }, new[] { Unit("X", 1, 1, 5) });
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 0, watcher);
            CheckTrue(Board(ctx, 0, 0).Card.WhenTriggers.Count == 1,
                      "监听器收下来了（**认得出 ≠ 发得出** —— 这条先证明它注册了）");
            int atk = Board(ctx, 0, 0).Attack;
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "Pawn"), 1), RuleCodes.OK, "打出一张部队");
            Check(Board(ctx, 0, 0).Attack, atk + 1,
                  $"**打出单位 → `When you play a troop` 触发了**（攻 {atk} → {atk + 1}）");
        }

        // ---- ② 反例：**对手**打出单位**不该**触发（`you` 的极性）----
        //     ⚠️ 这是 2026-09-13 修掉的一条「打得比卡面宽」：`Clean` 会把 `you` 抹掉，
        //        修之前 `OwnerIs` 停在 -1（不限）⇒ 对手部署也触发。
        {
            var watcher = new CardDef("W2", "W2", "unit", "When you play a troop, gain +1 Attack",
                                      "common", "Test", 1, 2, 9, 0, null, subtype: "Infantry");
            var pawn = Unit("Pawn2", 1, 1, 1);
            var ctx = Battle(new[] { watcher }, new[] { pawn });
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 0, watcher);
            int atk = Board(ctx, 0, 0).Attack;
            PassTurn(ctx);                       // 轮到对手
            CheckCode(RuleCore.PlayCard(ctx, 1, HandIdx(ctx, 1, "Pawn2"), 1), RuleCodes.OK, "对手打出一张部队");
            Check(Board(ctx, 0, 0).Attack, atk,
                  "**对手**打出单位时**不**触发（`you` = 本方；修之前这里会 +1）");
        }

        // ---- ③ `When your opponent plays a Stratagem, …` ----
        //     ⚠️ 必须用**卡池里真的计策卡**：`stratagem` 这个词在兵种表里判的是
        //        `subtype == "Stratagem"`，我们自造的 `Tactic(...)` 没有 subtype ⇒ 永远匹配不上。
        {
            var realTac = null as CardDef;
            foreach (var c in pool)
                if (c.Type == "tactic" && c.Subtype == "Stratagem") { realTac = c; break; }
            CheckTrue(realTac != null, "卡池里挑得到一张真的「计策」（subtype=Stratagem）");
            if (realTac != null)
            {
                var watcher = new CardDef("W3", "W3", "unit",
                                          "When your opponent plays a Stratagem, gain +1 Attack",
                                          "common", "Test", 1, 2, 9, 0, null, subtype: "Infantry");
                var ctx = Battle(new[] { watcher }, new[] { realTac });
                ToP1Turn(ctx, 1);
                Place(ctx, 0, 0, watcher);
                int atk = Board(ctx, 0, 0).Attack;
                PassTurn(ctx);
                ctx.Players[1].Energy = 9;       // 白盒：保证打得起
                CheckCode(RuleCore.PlayTactic(ctx, 1, HandIdx(ctx, 1, realTac.Name), -1), RuleCodes.OK,
                          $"对手打出「{realTac.Name}」");
                Check(Board(ctx, 0, 0).Attack, atk + 1,
                      $"**对手打计策 → 触发**（攻 {atk} → {atk + 1}）");
            }
        }

        // ---- ④ `When you draw a card, …` ----
        {
            var watcher = new CardDef("W4", "W4", "unit", "When you draw a card, gain +1 Attack",
                                      "common", "Test", 1, 2, 9, 0, null, subtype: "Infantry");
            var ctx = Battle(new[] { watcher, Unit("F1", 1, 1, 1) }, new[] { Unit("X", 1, 1, 5) });
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 0, watcher);
            int atk = Board(ctx, 0, 0).Attack;
            RuleCore.Draw(ctx, 0);
            Check(Board(ctx, 0, 0).Attack, atk + 1, $"**抽牌 → 触发**（攻 {atk} → {atk + 1}）");
        }

        // ---- ⑤ `When Reanimated, …` + `reanimate`（最后一个没实现的动词）----
        {
            var back = new CardDef("W5", "W5", "unit", "When Reanimated, gain Fast",
                                   "common", "Test", 1, 3, 3, 0, null, subtype: "Infantry");
            var kill = Tactic("T_KillOwn", 0, "Deal 99 damage to a friendly unit");
            var bring = Tactic("T_Re", 0, "Reanimate a friendly Remnant");
            var ctx = Battle(new[] { kill, bring, back }, new[] { Unit("X", 1, 1, 5) });
            ToP1Turn(ctx, 1);
            // 先让它死一次（进墓地 = 我们的「残骸」）
            Place(ctx, 0, 1, back);
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_KillOwn"), 1), RuleCodes.OK,
                      "先把它打死（`Deal 99 damage to a friendly unit`）");
            CheckTrue(Board(ctx, 0, 1) == null, "它离开棋盘了");
            CheckTrue(ctx.DeadUnits.Count > 0, "墓地里记着它");

            // 🔴 2026-09-13 第三十四轮补的**反例旁观者** —— 上面那几条只量了「自己翻自己」，
            //    所以「**收成「任何单位被再造」**」这个 bug 一直没露头（实测确实漏了：
            //    `reanimated` 当初只设了 `Kind`、没设 `SelfOnly`）。
            //    ⚠️ **要摆在「打死」之后**：前面那句 `Deal 99 damage to a friendly unit` 是自动选目标的，
            //       先摆上来的话它可能替 W5 挨那一刀。
            var idle = new CardDef("W5b", "W5b", "unit", "When Reanimated, gain Fast",
                                   "common", "Test", 1, 3, 3, 0, null, subtype: "Infantry");
            var idleU = Place(ctx, 0, 0, idle);
            CheckTrue(!idleU.Has("fast"), "旁观那张：刚摆上去，还没有 Fast");

            var ops = EffectText.Parse(bring.Desc, out _, out _);
            CheckTrue(ops.Count > 0 && ops[0].Verb == "reanimate", "`Reanimate a friendly Remnant` → `reanimate`");
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Re"), -1), RuleCodes.OK,
                      "`Reanimate` 打得出去（**这是最后一个没实现的动词**）");
            var got = null as UnitState;
            for (int s = 0; s < BoardSpec.Size; s++)
                if (Board(ctx, 0, s) != null && Board(ctx, 0, s).Name == "W5") { got = Board(ctx, 0, s); break; }
            CheckTrue(got != null, "**它真的从墓地翻回场上了**");
            CheckTrue(got != null && got.Has("fast"),
                      "……而且 `When Reanimated, gain Fast` **触发了**（拿到 Fast）");
            CheckTrue(!ctx.DeadUnits.Exists(d => d.Card != null && d.Card.Name == "W5"),
                      "……翻回来之后**从墓地拿走**了（不然还能无限翻）");

            // 🔴 **反例**：翻回来的是 W5，旁观那张（W5b）**一次都不该响**。
            //    ⚠️ 尺子用 `WhenFired`（数日志里「这张卡的监听器响了」几行），
            //       **不用 `Has("fast")`** —— `gain Fast` 是**无目标**的正文，
            //       在 `DoGive` 里会落到**己方全体**（`EffectResolver.cs:1945`，照原版 `rule_core.gd:3137`），
            //       于是旁观那张**照样**会拿到 Fast，数值上分不出「是谁的监听器响的」。
            //       这个坑在自指那一格（⑥）也踩过一次，两处长得一模一样。
            Check(WhenFired(ctx, "W5"), 1, "★ **W5 自己的监听器响了恰一次**（它被翻回来）");
            Check(WhenFired(ctx, "W5b"), 0,
                  "★ **旁观那张一次都没响** —— 收成「任何单位被再造」的话这里会是 1"
                  + "（第三十四轮之前就是这个行为：派子代理逐条核 `When` 时才翻出来）");
        }

        // ---- ⑥ `When a friendly unit prays, …` ----
        //     ✅ **2026-09-13 A2 起这条不再靠近似**：`Pray` / `Duty` / `Ferocity` / `Agenda`
        //        四个替代行动**拆开**了（`RuleCore.UseAlternative`），`When … prays` 只认真·祈祷。
        //        （拆开之前它们是被自定的 `Ability:` 收成一条的，放 Duty 也会让 `prays` 响。）
        {
            var watcher = new CardDef("W6", "W6", "unit", "When a friendly unit prays, gain +2 Attack",
                                      "common", "Test", 1, 2, 9, 0, null, subtype: "Infantry");
            var prayer = new CardDef("W7", "W7", "unit", "Pray: Gain Shield",
                                     "common", "Test", 1, 1, 5, 0, new[] { "Pray" }, subtype: "Infantry");
            var ctx = Battle(new[] { watcher, prayer }, new[] { Unit("X", 1, 1, 5) });
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 0, watcher);
            var pr = Place(ctx, 0, 1, prayer);
            CheckTrue(prayer.TriggerOps("pray") != null, "`Pray:` 的正文收下来了（`RoutableTriggers` 里有它）");
            pr.Exhausted = false;                            // 祈祷是**缓慢**的：部署当回合不能动
            int atk = Board(ctx, 0, 0).Attack;
            CheckCode(RuleCore.UseAlternative(ctx, 0, 1, "pray"), RuleCodes.OK, "W7 祈祷");
            Check(Board(ctx, 0, 0).Attack, atk + 2,
                  $"**有人祈祷 → 触发**（攻 {atk} → {atk + 2}）");

            // 反例：**放职责**不该让 `prays` 响（拆开之前两个都会响）
            var dutyer = new CardDef("W7b", "W7b", "unit", "Duty: Gain Shield",
                                     "common", "Test", 1, 1, 5, 0, new[] { "Duty" }, subtype: "Infantry");
            var ctx2 = Battle(new[] { watcher, dutyer }, new[] { Unit("X", 1, 1, 5) });
            ToP1Turn(ctx2, 1);
            int atk0 = Place(ctx2, 0, 0, watcher).Attack;
            Place(ctx2, 0, 1, dutyer).Exhausted = false;
            CheckCode(RuleCore.UseAlternative(ctx2, 0, 1, "duty"), RuleCodes.OK, "W7b 用职责");
            Check(Board(ctx2, 0, 0).Attack, atk0,
                  "★ **放职责不该触发 `When a friendly unit prays`** —— "
                  + "收成一条的那个近似会让这里实得 +2（规则书 `:181` 的职责不是祈祷）");
        }

        // ---- ⑦ `When a friendly troop receives a Dark Pact, …` ----
        {
            var watcher = new CardDef("W8", "W8", "unit",
                                      "When a friendly troop receives a Dark Pact, gain +1 Attack",
                                      "common", "Test", 1, 2, 9, 0, null, subtype: "Infantry");
            var ctx = Battle(new[] { watcher, troop }, new[] { Unit("X", 1, 1, 5) });
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 0, watcher);
            Place(ctx, 0, 1, troop);
            int atk = Board(ctx, 0, 0).Attack;
            RuleCore.GrantDarkPact(ctx, 0, Board(ctx, 0, 1), "of blood", "自检");
            Check(Board(ctx, 0, 0).Attack, atk + 1,
                  $"**有人收到黑暗契约 → 触发**（攻 {atk} → {atk + 1}）");
        }
    }

    /// <summary>
    /// **两套阵营资源**（信仰 Faith / 灵魂石 Spirit Stone）—— 2026-09-13 第三十三轮。
    ///
    /// 数值口径是**用户 2026-09-12 亲口定的**（见 `PlayerState.Faith/SpiritStones` 的注释）：
    /// **两样都没有上限、没有初始值、没有每回合增长**；信仰**不是货币**（阈值用），
    /// 灵魂石**是**货币（`Spend all`）。⇒ 这几条断言的重点是「**没有上限**」和
    /// 「**付费扣的是对应资源、不是能量**」—— 后者原来是一条**真 bug**（结算层只扣能量）。
    /// </summary>
    static void TestFactionResources()
    {
        // ---- ① 解析：两种资源的写法（含**图标字形**，原来一律不认）----
        {
            var ops = EffectText.Parse("Gain 2 Spirit Stones", out _, out _);
            Check(ops.Count, 1, "`Gain 2 Spirit Stones` 解析出 1 条");
            Check(ops[0].Verb, "gainspirit",
                  "→ 动词 `gainspirit`（**不是**把 `2 spirit stones` 当关键词塞给单位）");
            Check(ops[0].Amount, 2, "数量 = 2");

            ops = EffectText.Parse("Gain 1 ☀", out _, out _);
            CheckTrue(ops.Count > 0, "`Gain 1 ☀`（**图标字形**）解析得出");
            if (ops.Count > 0) Check(ops[0].Verb, "gainfaith", "……而且认成 `gainfaith`");

            ops = EffectText.Parse("Gain 1 [Faith]", out _, out _);
            CheckTrue(ops.Count > 0, "`Gain 1 [Faith]`（方括号写法）解析得出");
            if (ops.Count > 0) Check(ops[0].Verb, "gainfaith", "……也认成 `gainfaith`");

            ops = EffectText.Parse("Gain +1☀", out _, out _);
            CheckTrue(ops.Count > 0, "`Gain +1☀`（带加号、无空格）解析得出");
            if (ops.Count > 0) Check(ops[0].Verb, "gainfaith", "……也认成 `gainfaith`");

            // 付费前缀 `4 ☀: …` —— 原来的正则只认单词，**☀ 根本认不出**（整句失配）
            ops = EffectText.Parse("4 ☀: Deal 2 additional damage", out _, out _);
            CheckTrue(ops.Count > 0 && ops[0].Cost == 4, "`4 ☀: …` 的付费前缀认得出（代价 4）");
            if (ops.Count > 0) Check(ops[0].CostKind, "faith", "……而且货币是**信仰**（☀ 就是信仰图标）");
            var pf = EffectText.Parse("8 [Faith]: Deploy an additional Battle Sister", out _, out _);
            CheckTrue(pf.Count > 0 && pf[0].Cost == 8 && pf[0].CostKind == "faith",
                      "`8 [Faith]: …` 同样 → 代价 8 信仰");
        }

        // ---- ② 结算：计数器真的涨，而且**没有上限** ----
        {
            var gain = Tactic("T_Faith", 0, "Gain 3 ☀");
            var ctx = Battle(new[] { gain, gain }, new[] { Unit("X", 1, 1, 5) });
            ToP1Turn(ctx, 1);
            Check(ctx.Players[0].Faith, 0, "开局信仰 0（**没有初始值** —— 用户口径）");
            RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Faith"), -1);
            Check(ctx.Players[0].Faith, 3, "打出后信仰 = 3");

            // 「没有上限」：直接推到远高于任何合理阈值，看它会不会被夹住
            ctx.Players[0].Faith = 99;              // 白盒：直接摆一个高位值
            RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Faith"), -1);
            Check(ctx.Players[0].Faith, 102, "信仰**没有上限**（99 + 3 = 102，没被 Min(上限,…) 夹住）");
        }

        // ---- ③ 付费：**扣的是对应资源，不是能量**（原 bug 就在这里）----
        {
            var pay = Tactic("T_PayFaith", 0, "3 [Faith]: Gain 1 Energy");
            var ctx = Battle(new[] { pay }, new[] { Unit("X", 1, 1, 5) });
            ToP1Turn(ctx, 1);
            var ps = ctx.Players[0];
            ps.Faith = 5;
            ps.MaxEnergy = 6;                       // 白盒：让 `Gain 1 Energy` 不会被上限吃掉
            int energyBefore = ps.Energy;
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_PayFaith"), -1), RuleCodes.OK,
                      "付得起 3 点信仰 → 打得出去");
            Check(ps.Faith, 2, "**信仰**扣了 3（5 → 2）");
            Check(ps.Energy, energyBefore + 1,
                  $"**能量没被当成信仰扣**（{energyBefore} → {ps.Energy}，只受了 `Gain 1 Energy` 的影响）");

            // 付不起 → 整段不激活，**而且一点信仰都不扣**
            var pay2 = Tactic("T_PayFaith2", 0, "9 [Faith]: Gain 1 Energy");
            var c2 = Battle(new[] { pay2 }, new[] { Unit("X", 1, 1, 5) });
            ToP1Turn(c2, 1);
            c2.Players[0].Faith = 4;
            RuleCore.PlayTactic(c2, 0, HandIdx(c2, 0, "T_PayFaith2"), -1);
            Check(c2.Players[0].Faith, 4, "付不起时**一点信仰都不扣**（整段不激活，照 `rule_core.gd:2529`）");
        }

        // ---- ④ 路标石阵亡 → 灵魂石 +1（规则书 :210/:225）----
        {
            var stone = Unit("Stone", 1, 1, 1, "Waystone");
            var kill = Tactic("T_Kill", 0, "Deal 5 damage to an enemy");
            var ctx = Battle(new[] { kill }, new[] { stone });
            ToP1Turn(ctx, 1);
            Place(ctx, 1, 0, stone);
            Check(ctx.Players[1].SpiritStones, 0, "打之前对方灵魂石 0");
            RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Kill"), 0);
            CheckTrue(Board(ctx, 1, 0) == null, "路标石单位被打死了（离开棋盘）");
            Check(ctx.Players[1].SpiritStones, 1,
                  "**路标石阵亡 → 它的控制者灵魂石 +1**（规则书 :225；⚠️ 我们简化成「一死就生成」，"
                  + "原版是「翻面 → 之后被摧毁才生成」两段式，见 `KeywordTable.Waystone`）");
        }

        // ---- ⑤ `… equal to your Faith`（数值取自资源）----
        {
            var refill = Tactic("T_RefillFaith", 0, "Refill Energy equal to your Faith");
            var ctx = Battle(new[] { refill }, new[] { Unit("X", 1, 1, 5) });
            ToP1Turn(ctx, 1);
            ctx.Players[0].MaxEnergy = 10;      // 白盒：回合 1 的上限只有 1，会被夹住看不出效果
            ctx.Players[0].Energy = 1;
            ctx.Players[0].Faith = 4;
            var ops = EffectText.Parse(refill.Desc, out _, out _);
            CheckTrue(ops.Count > 0 && ops[0].AmountRef == "faith",
                      "`… equal to your Faith` 记在 `AmountRef` 上");
            RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_RefillFaith"), -1);
            Check(ctx.Players[0].Energy, 5,
                  "`Refill Energy equal to your Faith` → 1 + 4 = 5（**不是回满** —— 原来没认这句，"
                  + "`Refill Energy` 会直接回满到上限 10）");
        }

        // ---- ⑥ `Spend all your Spirit Stones` + `For each one, …`（次数只算一处）----
        {
            var op = EffectText.Parse("Spend all your Spirit Stones", out _, out _);
            CheckTrue(op.Count > 0 && op[0].Verb == "spendspirit",
                      "`Spend all your Spirit Stones` → `spendspirit`");

            var host = CardDatabase.Find(CardDatabase.Load(), "Hosts of the Dead", "SaimHann");
            CheckTrue(host != null, "卡池里有 `Hosts of the Dead`"
                      + "（`… Spend all your Spirit Stones. For each one, deploy a Wraithguard`）");
            if (host != null)
            {
                var ctx = Battle(new[] { Tactic("T_X", 0, "Gain 1 Energy") }, new[] { Unit("X", 1, 1, 5) });
                ToP1Turn(ctx, 1);
                ctx.Players[0].SpiritStones = 3;
                ctx.LastSpentSpirit = 3;        // 白盒：`For each one` 读的就是它（由 `spendspirit` 写）
                var ops2 = EffectText.Parse("For each one, deploy a Wraithguard", out _, out _);
                CheckTrue(ops2.Count > 0 && ops2[0].CountScope == "spiritspent",
                          "`For each one, …` 的计数来源 = 「刚花掉的灵魂石」（scope=spiritspent）");
                if (ops2.Count > 0)
                    Check(RuleCore.CountForTest(ctx, 0, null, ops2[0]), 3,
                          "……数出来正好是刚花掉的 3 颗（**次数只在一处算**）");
            }
        }
    }

    /// <summary>墓地取走一张：**只取走一张**。
    ///
    /// 为什么值得单开一条：`BattleContext.TakeFromGraveyard` 的两个循环**都没有 `break`**
    /// （2026-09-18 修），而弃牌堆 / 死单位表里存的是**共享的 `CardDef` 对象** ——
    /// 同名两份 `ReferenceEquals` 都为真 ⇒ 取一张会把**同名副本全删掉**。
    /// 尺子就是「放两份、取一次、数剩几份」—— 不按名字取、不看日志。
    /// ⚠️ 这条**和卡实例身份无关**：它在今天就是错的（实例身份只是让它更精确）。
    /// </summary>
    static void TestGraveyardTakeOne()
    {
        var pool = CardDatabase.Load();
        var def = CardDatabase.Find(pool, "Firestrike Turrets", "Ultramarines");
        CheckTrue(def != null, "拿一张真卡当尺子：`Firestrike Turrets`");
        if (def == null) return;

        var ctx = Battle(new[] { def, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 5) });
        var d = ctx.Players[0].Discard;
        d.Add(ctx.NewInstance(def)); d.Add(ctx.NewInstance(def));   // 第 7 行第 2 步：弃牌堆存实例
        ctx.DeadUnits.Add(new DeadUnit { Card = def, Owner = 0, DeathTurn = 0 });
        ctx.DeadUnits.Add(new DeadUnit { Card = def, Owner = 0, DeathTurn = 0 });
        Check(d.Count, 2, "弃牌堆里放两份同名卡");
        Check(ctx.DeadUnits.Count, 2, "死单位表里放两份同名卡");

        ctx.TakeFromGraveyard(0, def);
        Check(d.Count, 1, "弃牌堆**只取走一张**（改之前是 0 —— 同名副本被一起删了）");
        Check(ctx.DeadUnits.Count, 1, "死单位表**也只取走一张**（同一处漏了 `break`）");
    }

    /// <summary>卡实例身份（待办第 7 行）**第 1 步**：棋盘那一侧先拿到实例。
    ///
    /// 这一步**不改任何行为**（`UnitState.Card` 只是从字段换成属性转发 `Instance.Card`），
    /// 所以尺子量的是**结构**、不是表现：
    ///   ① 真部署（督军 / `PlayCard` / `DeployFree`）上场的单位都带**对局发的**实例（正数号）；
    ///   ② 同一局里**没有两个单位共用一份实例**；
    ///   ③ 翻面成**残骸**、以及从残骸**翻回来**，都**沿用同一个实例**（这两处没换牌）；
    ///   ④ 没有 `BattleContext` 的场合发**负数**号 —— 与对局号段永不冲突；
    ///   ⑤ 同一个种子跑两局，发出去的实例**个数**一致（可复现）。
    /// ⚠️ 「**手牌的那一份上场后还是同一份**」要等**第 2 步**（换手牌类型）才成立 —— 那时再加。
    /// ⚠️ 这一条**不是**「效果对不对」的尺子：行为零变化是这一步的**目标**，
    ///    D 类那 92 张卡的语义断言在第 3 步加（一号靶 = `Master of Manoeuvre`）。
    /// </summary>
    static void TestCardInstanceStep1()
    {
        var plain = Unit("Plain", 1, 2, 3);

        // ---- ① 真部署上场的单位都带**对局发的**实例 ----
        {
            var ctx = Battle(new[] { plain }, new[] { Unit("X", 1, 1, 5) });
            ToP1Turn(ctx, 1);

            var wl = ctx.Players[0].Warlord;
            CheckTrue(wl != null && wl.Instance != null, "督军也带实例（`BuildPlayer` 走 `ctx.NewInstance`）");
            if (wl != null && wl.Instance != null)
                CheckTrue(wl.Instance.Id > 0, "……而且号是**对局计数器**发的（正数，不是 `Detached` 的负数）");

            int hi = HandIdx(ctx, 0, "Plain");
            CheckCode(RuleCore.PlayCard(ctx, 0, hi, 1), RuleCodes.OK, "打出一张部队上场");
            var unit = Board(ctx, 0, 1);
            CheckTrue(unit != null && unit.Instance != null, "打出去的部队带实例");
            if (unit != null && unit.Instance != null)
            {
                CheckTrue(ReferenceEquals(unit.Instance.Card, plain),
                          "实例指的正是那张卡（按**引用**比，不是按名字）");
                CheckTrue(unit.Instance.Id > 0, "……号也是对局发的（正数）");
                CheckTrue(!unit.Instance.IsDetached, "`IsDetached` 为假 —— 真对局里没有分离实例");
            }

            // ---- ② 同一局里没有一个号被两份共用（连督军一起数）----
            var seen = new HashSet<int>();
            int onBoard = 0, dup = 0;
            for (int p = 0; p < 2; p++)
                for (int s = 0; s < BoardSpec.Size; s++)
                {
                    var u = ctx.Players[p].Board[s];
                    if (u == null) continue;
                    onBoard++;
                    if (!seen.Add(u.Instance.Id)) dup++;
                }
            Check(onBoard, 3, "场上三个单位（两个督军 + 刚打出去的那张）");
            Check(seen.Count, onBoard, "★ **一个实例号只对应一个单位**（没有两份共用）");
            Check(dup, 0, "……重复计数为 0");
        }

        // ---- ③ 残骸 / 翻回来都沿用同一个实例 ----
        {
            var rem = new CardDef("InstRemUnit", "InstRemUnit", "unit", "",
                                  "common", "Test", 1, 2, 5, 0,
                                  new[] { KeywordTable.Remnant }, subtype: "Infantry");
            var kill = Tactic("T_RemKill", 0, "Deal 99 damage to a friendly unit");
            var raise = Tactic("T_Raise", 0, "Reanimate a friendly Remnant");

            var ctx = ProbeBattle(new[] { rem, kill, raise }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            var placed = Place(ctx, 0, 2, rem, exhausted: true);
            var inst = placed.Instance;
            CheckTrue(inst != null && inst.Id > 0, "夹具摆上去的单位也是**对局发的**实例（`Place` 已改成走 `ctx.NewInstance`）");

            RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_RemKill"), 2);
            var remn = Board(ctx, 0, 2);
            CheckTrue(remn != null && remn.IsRemnant, "先造一具残骸");
            CheckTrue(remn != null && ReferenceEquals(remn.Instance, inst),
                      "★ **翻面成残骸没有换牌** —— 还是原来那一份实例");
            Check(remn != null && remn.Instance != null ? remn.Instance.Id : -999,
                  inst != null ? inst.Id : -1, "……实例号也没变");

            RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Raise"), -1);
            var back = Board(ctx, 0, 2);
            CheckTrue(back != null && !back.IsRemnant, "再从残骸翻回来");
            CheckTrue(back != null && ReferenceEquals(back.Instance, inst),
                      "★ **翻回来也没有换牌** —— 同一个实例（`DoReanimate` 走 `rem.Instance`）");
        }

        // ---- ④ 没有对局的场合：分离实例发负数号，不与对局号段冲突 ----
        {
            var d1 = new UnitState(plain, false);
            var d2 = new UnitState(plain, false);
            CheckTrue(d1.Instance != null && d1.Instance.Id < 0,
                      "`new UnitState(卡, …)`（测试/演示那条路）= **分离实例**，发负数号");
            CheckTrue(d1.Instance.Id != d2.Instance.Id, "……两次分离实例的号也互不相同");
            CheckTrue(!ReferenceEquals(d1.Instance, d2.Instance), "……而且是两个不同的对象");
        }

        // ---- ⑤ 可复现：同一个种子跑两局，发出去的实例个数一致 ----
        {
            var a = Battle(new[] { plain, Unit("B", 1, 1, 1) }, new[] { Unit("X", 1, 1, 5) });
            var b = Battle(new[] { plain, Unit("B", 1, 1, 1) }, new[] { Unit("X", 1, 1, 5) });
            Check(a.InstanceCount, b.InstanceCount, "同种子两局发出去的实例**个数相同**（计数器挂在 ctx 上，不是静态的）");
            CheckTrue(a.InstanceCount > 0, $"……而且确实发出去了（{a.InstanceCount} 份）");
        }
    }

    /// <summary>
    /// **手牌里那一份**（按卡名找第一份）—— 第 7 行第 3 步之后，**算费用必须给它**：
    /// 「只降这一份」的修正挂在实例上，用卡模板查是**看不见**的（`CostOf(模板)` 会跳过按份的修正）。
    /// </summary>
    static CardInstance HandInst(BattleContext ctx, int p, string name)
    {
        foreach (var h in ctx.Players[p].Hand)
            if (h != null && h.Card != null && h.Card.Name == name) return h;
        return null;
    }

    /// <summary>区域里有没有**这一份**（按对象身份比 —— 同名两张也分得开）。</summary>
    static bool HasInst(List<CardInstance> zone, CardInstance inst)
    {
        foreach (var h in zone) if (object.ReferenceEquals(h, inst)) return true;
        return false;
    }

    /// <summary>
    /// 🎯 **一号验收靶**（待办第 7 行 · 用户 2026-09-18 点名的）+ 那一句「**另一份不许被误伤**」。
    ///
    /// 卡面：`DA41 Master of Manoeuvre` = `Ephemeral. Return a friendly Vehicle to your hand.
    /// **It** costs 4 less`（中文：「将 1 个我方载具移回你的手牌。该单位费用减少 4」）。
    ///
    /// 为什么它是「实例身份」的教科书例子（**两层**都要对）：
    ///   ① **`Return` 得把原来那一份**放回手里（不是新造一张）——
    ///      否则后面那句 `It costs 4 less` 指的就不是玩家看见的那张牌；
    ///   ② **`It` 必须只命中「那一份」** —— 手里拿着**两张同名载具**时，
    ///      按卡模板记的话**两张一起降价**（那正是第 7 行要修的老毛病，约 92 张卡受影响）。
    ///
    /// 尺子就是「**两张同名载具，只有回来的那张变便宜**」—— 不看日志、不按名字，全是对象身份。
    /// </summary>
    static void TestInstanceAcceptanceTarget()
    {
        var pool = CardDatabase.Load();
        var master = CardDatabase.Find(pool, "Master of Manoeuvre");
        var veh = CardDatabase.Find(pool, "Ravenwing Ancient", "DarkAngels");
        CheckTrue(master != null, "卡池里找得到 `Master of Manoeuvre`（一号靶）");
        CheckTrue(veh != null && veh.Subtype == "Vehicle", "靶子的道具：一张暗黑天使**载具**（`Ravenwing Ancient`）");
        if (master == null || veh == null) return;

        // 手里：**两张同名载具** + 这张天赋
        var ctx = BattlePool(new[] { veh, veh, master }, new[] { Unit("EInst", 1, 0, 9) }, pool, "DarkAngels", 11);
        ToP1Turn(ctx, 1);
        ctx.Players[0].Energy = 20;
        CheckTrue(HandInst(ctx, 0, "Master of Manoeuvre") != null,
                  "（前提）天赋还在手里 —— 回合数别拉太长，手牌上限会把最后抽到的丢掉");

        // ① 先把**一份**载具真打上场（走 `PlayCard` —— 手牌那一份原样上场）
        int vi = HandIdx(ctx, 0, "Ravenwing Ancient");
        CheckTrue(vi >= 0, "手牌里有载具");
        var deployed = ctx.Players[0].Hand[vi];
        CheckCode(RuleCore.PlayCard(ctx, 0, vi, 1), RuleCodes.OK, "把其中一份载具打上场");
        var onBoard = Board(ctx, 0, 1);
        CheckTrue(onBoard != null && ReferenceEquals(onBoard.Instance, deployed), "上场的就是那一份");

        // 手里**还剩一份同名载具** —— 它是「不该被误伤」的对照组
        var other = ctx.Players[0].Hand.Find(h => h != null && ReferenceEquals(h.Card, veh));
        CheckTrue(other != null && !ReferenceEquals(other, deployed), "手里还剩**另一份**同名载具（对照组）");
        int otherCostBefore = RuleCore.CostOf(ctx, 0, other);
        Check(otherCostBefore, veh.Cost, $"对照组现在按牌面价（{veh.Cost}）");

        // ② 打出天赋：把场上那个载具收回手，并让它便宜 4
        int mi = HandIdx(ctx, 0, "Master of Manoeuvre");
        CheckTrue(mi >= 0, "天赋在手里");
        CheckCode(RuleCore.PlayTactic(ctx, 0, mi, 1), RuleCodes.OK,
                  "打出 `Master of Manoeuvre`（`Return a friendly Vehicle to your hand. It costs 4 less`）");

        // ③ 回来的**就是上场的那一份**（用户拍的口径：回手 = 同一个实例）
        CheckTrue(Board(ctx, 0, 1) == null, "格位空了");
        CheckTrue(HasInst(ctx.Players[0].Hand, deployed),
                  "★ **回来的就是上场的那一份**（`Return` = 同一个 `CardInstance`）");

        // ④ 🎯 减费只落在**它**身上
        Check(RuleCore.CostOf(ctx, 0, deployed), System.Math.Max(0, veh.Cost - 4),
              $"★ 「It costs 4 less」落在**它**身上（{veh.Cost} → {System.Math.Max(0, veh.Cost - 4)}）");
        Check(RuleCore.CostOf(ctx, 0, other), otherCostBefore,
              "★★ **另一张同名载具一分钱没降** —— 这一条就是第 7 行要的那一刀"
              + "（改之前 `Key = 卡 id` 会把手里所有同名副本一起降价）");
    }

    /// <summary>卡实例身份（待办第 7 行）**第 2 步**：`Deck/Hand/Discard` 换成实例之后，
    /// **跨区域要还是同一份**。
    ///
    /// 第 2 步改的只是三个列表的元素类型，**行为一个字不该变**（2881 条老断言就是那条尺子）。
    /// 这一条量的是**目的**：那一份会不会在「手牌 → 场上 → 弃牌堆」「牌库 → 手牌」
    /// 「场上 → 手牌」这些跳跃里**被换成新对象**。判据一律 `ReferenceEquals`（对象身份），
    /// **不比 id、不比名字** —— 名字相同是两张牌，那正是这一行要解决的病。
    /// </summary>
    static void TestCardInstanceStep2()
    {
        var plain = Unit("InstPlain2", 1, 2, 3);
        var back = Tactic("T_InstReturn", 0, "Return a friendly troop to your hand");
        var kill = Tactic("T_InstKill", 0, "Deal 99 damage to a friendly unit");

        var ctx = Battle(new[] { plain, back, kill }, new[] { Unit("E2", 1, 1, 5) });
        ToP1Turn(ctx, 1);
        ctx.Players[0].Energy = 20;

        // ---- ① 手牌 → 场上：上场的就是手牌里的那一份 ----
        int hi = HandIdx(ctx, 0, "InstPlain2");
        CheckTrue(hi >= 0, "手牌里有那张部队");
        var inHand = ctx.Players[0].Hand[hi];
        CheckCode(RuleCore.PlayCard(ctx, 0, hi, 1), RuleCodes.OK, "把它打上场");
        var onBoard = Board(ctx, 0, 1);
        CheckTrue(onBoard != null && ReferenceEquals(onBoard.Instance, inHand),
                  "★ **上场的就是手牌里的那一份**（以前这里断链：`UnitState` 只收卡模板）");

        // ---- ② 场上 → 手牌（`Return`）：回到手里的还是那一份（用户 2026-09-18 拍的口径）----
        int ri = HandIdx(ctx, 0, "T_InstReturn");
        CheckCode(RuleCore.PlayTactic(ctx, 0, ri, 1), RuleCodes.OK, "回手那张部队");
        CheckTrue(Board(ctx, 0, 1) == null, "格位空了");
        CheckTrue(HasInst(ctx.Players[0].Hand, inHand),
                  "★ **回手 = 同一个实例**（默认保留实例态；不是新造一张）");

        // ---- ③ 场上 → 弃牌堆：单位死了，进弃牌堆的还是那一份 ----
        int h2 = HandIdx(ctx, 0, "InstPlain2");
        CheckTrue(h2 >= 0 && ReferenceEquals(ctx.Players[0].Hand[h2], inHand), "它还在手里（还是那一份）");
        CheckCode(RuleCore.PlayCard(ctx, 0, h2, 2), RuleCodes.OK, "再打上场");
        int ki = HandIdx(ctx, 0, "T_InstKill");
        CheckCode(RuleCore.PlayTactic(ctx, 0, ki, 2), RuleCodes.OK, "把它打死");
        CheckTrue(Board(ctx, 0, 2) == null, "格位空了");
        CheckTrue(HasInst(ctx.Players[0].Discard, inHand),
                  "★ **进弃牌堆的还是那一份**（`Discard.Add(u.Instance)`）");
        CheckTrue(ctx.DeadUnits.Count > 0 && ReferenceEquals(ctx.DeadUnits[ctx.DeadUnits.Count - 1].Card, plain),
                  "死单位表记的是这张卡（⚠️ 那张表仍按**卡模板**记 —— 见 `DeadUnit` 的注释）");

        // ---- ④ 牌库 → 手牌：抽出来的还是牌库里那一份 ----
        var deckTop = ctx.Players[0].Deck[ctx.Players[0].Deck.Count - 1];   // `Draw` 从末尾抽
        int before = ctx.Players[0].Hand.Count;
        RuleCore.Draw(ctx, 0);
        Check(ctx.Players[0].Hand.Count, before + 1, "抽了一张");
        CheckTrue(ReferenceEquals(ctx.Players[0].Hand[ctx.Players[0].Hand.Count - 1], deckTop),
                  "★ **抽出来的就是牌库里那一份**（不新发）");
    }

    /// <summary>卡实例身份**第 2 步 · 复制与变身**：
    /// 「新造一张」和「挪一份」必须分得开 —— 这是第 2 步最容易猜错的地方（计划文档 §六 点名的那个形状）。
    ///   · **潮涌复制** = 每张**新发一份**（复制品与原件从此是两个对象）；
    ///   · **变身** = **另发新实例**（用户 2026-09-18 拍的口径），旧那一份不再在手里。
    /// </summary>
    static void TestCardInstanceNewVsMove()
    {
        // ---- ① 潮涌复制：复制品与原件是**两个不同的实例** ----
        var tide = Unit("InstTide", 1, 1, 3, "Tide 2");
        var ctx = Battle(new[] { tide }, new[] { Unit("E3", 1, 1, 5) });
        ToP1Turn(ctx, 1);
        ctx.Players[0].Energy = 20;
        int ti = HandIdx(ctx, 0, "InstTide");
        CheckTrue(ti >= 0, "手牌里有潮涌卡");
        var origin = ctx.Players[0].Hand[ti];
        CheckCode(RuleCore.PlayCard(ctx, 0, ti, 1), RuleCodes.OK, "打出潮涌部队（会造 2 张复制）");
        var copies = new List<CardInstance>();
        foreach (var h in ctx.Players[0].Hand)
            if (h.Card != null && h.Card.Name == "InstTide") copies.Add(h);
        Check(copies.Count, 2, "手里多了 2 张复制");
        CheckTrue(copies.Count == 2 && !ReferenceEquals(copies[0], copies[1]), "★ 两张复制**不是同一个实例**");
        foreach (var c in copies)
            CheckTrue(!ReferenceEquals(c, origin),
                      "★ **复制品 ≠ 原件**（改之前它们是同一个 `CardDef` 对象，分不开）");

        // ---- ② 变身：另发新实例、旧那一份不再在手里 ----
        var pool = CardDatabase.Load();
        var s1 = Tactic("InstStrat1", 1, "Draw a card");
        var s2 = Tactic("InstStrat2", 1, "Draw a card");
        var hrolf = CardDatabase.Find(pool, "Hrolf the Ironhowl");
        if (hrolf == null) { CheckTrue(false, "卡池里找不到 `Hrolf the Ironhowl`"); return; }
        var ctx2 = BattlePool(new[] { s1, s2, hrolf }, new[] { Unit("E4", 1, 0, 9) }, pool, "SpaceWolves", 7);
        ToP1Turn(ctx2, 5);
        ctx2.Players[0].Energy = 20;
        var old1 = ctx2.Players[0].Hand[HandIdx(ctx2, 0, "InstStrat1")];
        var old2 = ctx2.Players[0].Hand[HandIdx(ctx2, 0, "InstStrat2")];
        ctx2.ResetChoices();
        CheckCode(RuleCore.PlayCard(ctx2, 0, HandIdx(ctx2, 0, "Hrolf the Ironhowl"), 0), RuleCodes.OK,
                  "部署 `Hrolf the Ironhowl`（Rally：手牌里的战略卡变身）");
        CheckTrue(!HasInst(ctx2.Players[0].Hand, old1) && !HasInst(ctx2.Players[0].Hand, old2),
                  "★ 变身之后**旧那一份不在手里了**（不是原地改模板）");
        int wolves = 0;
        foreach (var h in ctx2.Players[0].Hand)
            if (h.Card != null && (h.Card.Name == "Hunting Wolf" || h.Card.Name == "Fenrisian Wolf")) wolves++;
        Check(wolves, 2, "★ 两只狼 = **两张新实例**（用户拍板：变身另发新实例、状态不跟）");
    }

    static void TestDefenceCards()
    {
        var pool = CardDatabase.Load();

        // ---- ① 数据面：39 张、全解析得出 ----
        var all = new List<CardDef>();
        foreach (var c in pool) if (c != null && c.Type == "defence") all.Add(c);
        Check(all.Count, 39, "卡池里 39 张防御卡（13 阵营 × 3）");
        var cov = EffectText.Coverage(pool, "defence", pool);
        Check(cov.Full, all.Count, "**39/39 都能完整解析**（0 条不认识的句子）");
        CheckTrue(cov.FullAndMechanized >= 33,
                  $"其中**载荷有机制** {cov.FullAndMechanized}/39"
                  + "（余下的那几张要等阵营资源：灵魂石 / 信仰 / 再起）");

        // ---- ② 打得出：走的是战术卡同一条链（`CanPlayTactic` → `PlayTactic` → 弃牌堆）----
        var def = CardDatabase.Find(pool, "Firestrike Turrets", "Ultramarines");
        CheckTrue(def != null, "挑得到一张真防御卡当尺子：`Firestrike Turrets`（Ultramarines，`Deal 2 damage to an enemy`）");
        if (def == null) return;

        var ctx = Battle(new[] { def, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 5) });
        ToP1Turn(ctx, 1);
        Place(ctx, 1, 0, Unit("Victim", 1, 1, 5));
        int hi = HandIdx(ctx, 0, "Firestrike Turrets");
        CheckTrue(hi >= 0, "防御卡在手里");
        Check(RuleCore.CanPlayTactic(ctx, 0, hi, 0), RuleCodes.OK,
              "`CanPlayTactic` 放行 —— 改之前这里返回 `ErrUnimplemented`");
        Check(RuleCore.PlayTactic(ctx, 0, hi, 0), RuleCodes.OK, "防御卡真的打得出去");
        Check(Board(ctx, 1, 0).Health, 3, "敌方单位 5 → 3（吃了 2 点）—— 效果真的结算了");
        Check(ctx.Players[0].Discard.Count, 1, "防御卡进弃牌堆（和战术卡同一条处置）");

        // ---- ③ 进手牌、不进牌库 ----
        var dctx = RuleCore.NewBattle(new[] { def, Unit("Hero", 0, 1, 30) }, new[] { Unit("X", 1, 1, 5) },
                                      seed: 77, shuffle: false);
        // ⚠️ 上面那张 `Hero` 不是 `hero` 类型（`Unit` 造的）—— 只是为了不触发督军提取，
        //    所以这条只验「防御卡去哪了」，不验督军。
        CheckTrue(dctx.Players[0].Hand.Exists(x => x != null && x.Card.Type == "defence"),
                  "防御卡在**手牌**里");
        CheckTrue(!dctx.Players[0].Deck.Exists(x => x != null && x.Card.Type == "defence"),
                  "……不在**牌库**里（不参与洗牌，也不会被抽成第二张）");

        // ---- ④ 换牌不许把它换掉（原版是「抽完 → 换牌 → 再置入」，我们放在换牌前，所以必须挡一道）----
        var mctx = RuleCore.NewBattle(new[] { def, Unit("Hero2", 0, 1, 30), Unit("B", 1, 1, 1) },
                                      new[] { Unit("X", 1, 1, 5) },
                                      seed: 78, shuffle: false, openMulligan: true);
        int dIdx = -1;
        for (int i = 0; i < mctx.Players[0].Hand.Count; i++)
            if (mctx.Players[0].Hand[i].Card.Type == "defence") { dIdx = i; break; }
        CheckTrue(dIdx >= 0, "换牌阶段开始时防御卡在手里");
        if (dIdx >= 0)
        {
            int n = RuleCore.Mulligan(mctx, 0, new List<int> { dIdx });
            Check(n, 0, "**换牌换不掉防御卡**（返回 0 = 一张都没换成）");
            CheckTrue(mctx.Players[0].Hand.Exists(x => x != null && x.Card.Type == "defence"),
                      "……它还在手里");
        }
    }

    static void TestTacticPlay()
    {
        // ① 伤害类：打掉敌方单位 3 血
        {
            var tac = Tactic("T_Deal", 1, "Deal 3 damage to an enemy");
            var ctx = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 3, 3) });
            ToP1Turn(ctx, 1);
            Place(ctx, 1, 0, Unit("Victim", 1, 1, 5));

            Check(EffectText.PickSide(EffectText.Parse(tac.Desc, out _, out _)), "enemy",
                  "「Deal 3 damage to an enemy」要玩家选**敌方**目标");
            int hand0 = ctx.Players[0].Hand.Count;     // ⚠️ 别写死张数：回合开始还会抽 1 张
            int code = RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Deal"), 0);
            Check(code, RuleCodes.OK, "战术卡打出去了");
            Check(Board(ctx, 1, 0).Health, 2, "敌方单位 5 → 2（吃了 3 点）");
            Check(ctx.Players[0].Energy, 1, "扣了 1 能（回合 1 有 2 能）");
            Check(ctx.Players[0].Hand.Count, hand0 - 1, "手牌少了一张");
            Check(ctx.Players[0].Discard.Count, 1, "卡进了弃牌堆");
        }

        // ② 限时增益：`this turn` 的加攻，**回合结束要撤回去**
        {
            var tac = Tactic("T_Buff", 1, "Give +2 attack to a friendly unit this turn");
            var ctx = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 0, Unit("Mine", 1, 3, 5));

            var ops = EffectText.Parse(tac.Desc, out _, out _);
            Check(ops.Count, 1, "解析出 1 条效果");
            Check(ops[0].Duration, "turn", "时长 = 本回合");
            Check(EffectText.PickSide(ops), "own", "要选**己方**目标");

            RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Buff"), 0);
            Check(Board(ctx, 0, 0).Attack, 5, "3 攻 → 5 攻（+2）");
            Check(Board(ctx, 0, 0).TempBuffs.Count, 1, "登记了一条限时增益");

            RuleCore.EndTurn(ctx);
            Check(Board(ctx, 0, 0).Attack, 3, "回合结束后 +2 收回去了（3 攻）");
        }

        // ③ **解析不了的卡必须被拒绝**，不许扣费后什么都不发生
        {
            var tac = Tactic("T_Bad", 1, "Frobnicate the whatsit");
            var ctx = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            int e0 = ctx.Players[0].Energy;
            int h0 = ctx.Players[0].Hand.Count;
            int code = RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Bad"), -1);
            Check(code, RuleCodes.ErrUnimplemented, "认不出来的战术卡 → 拒绝");
            Check(ctx.Players[0].Energy, e0, "能量**一点没扣**");
            Check(ctx.Players[0].Hand.Count, h0, "牌还在手上");
        }

        // ④ 费用不够 → 拒绝（也不能扣）
        {
            var tac = Tactic("T_Pricey", 9, "Deal 1 damage to an enemy");
            var ctx = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            int e0 = ctx.Players[0].Energy;
            Check(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Pricey"), 0), RuleCodes.ErrCost,
                  "9 费的卡在 2 能回合打不出去");
            Check(ctx.Players[0].Energy, e0, "能量没动");
        }

        // ⑤ **真卡池里的原版卡** —— 挑一张「完全解析 + 只打敌方一个单位」的，真打一遍
        {
            var pool = CardDatabase.Load();
            CardDef pick = null;
            List<EffectOp> pickOps = null;
            foreach (var c in pool)
            {
                if (c == null || c.Type != "tactic") continue;
                var ops = EffectText.Parse(c.Desc, out var un, out var pa);
                if (un.Count > 0 || pa.Count > 0) continue;
                bool dealOnly = ops.Count > 0, hasDeal = false;
                foreach (var o in ops)
                    if (o.Verb != "deal") dealOnly = false; else hasDeal = true;
                if (!dealOnly || !hasDeal) continue;
                if (EffectText.PickSide(ops) != "enemy") continue;
                if (ops[0].Amount > 6) continue;                    // 别一下把测试单位打死（上限之外无所谓）
                pick = c; pickOps = ops; break;
            }
            CheckTrue(pick != null, "卡池里找得到「完全解析 + 只打一个敌方单位」的战术卡");
            if (pick != null)
            {
                var ctx = Battle(new[] { pick, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
                ToP1Turn(ctx, 1);
                ctx.Players[0].Energy = pick.Cost;                  // 让它一定付得起
                Place(ctx, 1, 0, Unit("Victim", 1, 1, 30));
                int code = RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, pick.Name), 0);
                Check(code, RuleCodes.OK, $"原版卡「{pick.Name}」打出去了");
                CheckTrue(Board(ctx, 1, 0).Health < 30,
                          $"原版卡「{pick.Name}」真打出了伤害（30 → {Board(ctx, 1, 0).Health}，"
                          + $"op = {pickOps[0]}）");
                Check(ctx.Players[0].Discard.Count, 1, "原版卡也进弃牌堆");
            }
        }

        // ⑥ 目标规则 —— 照原版 `_collect_tactic_targets` 的 pick 分支（`rule_core.gd:4030` 附近）
        {
            // `an enemy` **包含敌方督军**（原版的全体分支只判 `_effect_target_blocked`，不排督军）
            var tac = Tactic("T_Warlord", 1, "Deal 4 damage to an enemy");
            var ctx = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            var foeWarlord = ctx.Players[1].Warlord;
            int hp0 = foeWarlord.Health;
            Check(RuleCore.CanPlayCard(ctx, 0, HandIdx(ctx, 0, "T_Warlord"), BoardSpec.WarlordSlot),
                  RuleCodes.OK, "敌方督军格是合法的战术目标（`an enemy` 含督军）");
            RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Warlord"), BoardSpec.WarlordSlot);
            Check(foeWarlord.Health, hp0 - 4, $"敌方督军真的挨了 4 点（{hp0} → {foeWarlord.Health}）");
        }
        {
            // `troop` 类目标**不含督军** —— 规则书：效果目标为 troop 不能影响督军
            var tac = Tactic("T_Troop", 1, "Deal 4 damage to an enemy troop");
            var ctx = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            Check(RuleCore.CanPlayCard(ctx, 0, HandIdx(ctx, 0, "T_Troop"), BoardSpec.WarlordSlot),
                  RuleCodes.ErrSlot, "`enemy troop` 不能选敌方督军");
            Place(ctx, 1, 0, Unit("Grunt", 1, 1, 5));
            Check(RuleCore.CanPlayCard(ctx, 0, HandIdx(ctx, 0, "T_Troop"), 0),
                  RuleCodes.OK, "`enemy troop` 能选敌方部队");
        }
        {
            // **隐身的单位不能被敌方效果选中** —— 规则书 Stealth / Camouflage
            var tac = Tactic("T_Stealth", 1, "Deal 4 damage to an enemy troop");
            var ctx = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            Place(ctx, 1, 0, Unit("Ghost", 1, 1, 5, "Stealth"));
            Check(RuleCore.CanPlayCard(ctx, 0, HandIdx(ctx, 0, "T_Stealth"), 0),
                  RuleCodes.ErrSlot, "隐身单位不能被敌方战术选中");
        }
    }

    /// <summary>
    /// 2026-09-12 补的三个关键词（战术卡的高频载荷）。
    /// 出处：规则书 :98/:187/:190 与 `rule_core.gd` 的 `_damage_unit:4406` / 部署段 `:2248`。
    /// </summary>
    static void TestFlankInvulnVulnerable()
    {
        var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
        ToP1Turn(ctx, 1);

        // ① 侧翼 / 迅捷：**部署当回合不疲劳**
        //    （规则书 :98「部署当回合不能行动，除非注明，如迅捷/侧翼/狂暴」；:187 侧翼）
        Check(new UnitState(Unit("Plain", 1, 2, 3), false).Exhausted, true,
              "普通单位部署当回合是疲劳的");
        Check(new UnitState(Unit("Flank", 1, 2, 3, "Flank"), false).Exhausted, false,
              "带侧翼的单位部署当回合就能行动");
        Check(new UnitState(Unit("Fast", 1, 2, 3, "Fast"), false).Exhausted, false,
              "带迅捷的也一样（原版 `rule_core.gd:2248` 把两者写在一起）");

        // 走一遍**真部署**：打出去的那张带侧翼，落地就应该能动
        var flankCard = Unit("Flanker", 1, 2, 3, "Flank");
        var ctx2 = Battle(new[] { flankCard, Unit("B", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
        ToP1Turn(ctx2, 1);
        Check(RuleCore.PlayCard(ctx2, 0, HandIdx(ctx2, 0, "Flanker"), 0), RuleCodes.OK,
              "侧翼单位打出去了");
        Check(Board(ctx2, 0, 0).Exhausted, false, "真部署上去的侧翼单位不疲劳");

        // ② 无敌：**伤害完全挡下**（规则书 :190「无法被伤害或摧毁」；原版 `_damage_unit:4414` 返回 0）
        var inv = Place(ctx, 0, 0, Unit("Inv", 1, 1, 3, "Invulnerable"));
        Check(RuleCore.ApplyDamage(ctx, inv, 5, "自检"), 0, "无敌单位受到 0 点伤害");
        Check(inv.Health, 3, "血量一点没掉");

        // ③ 易伤 X：**多加 X 点**（⚠️ 名字容易看反 —— 是加伤，原版 `:4418`）
        var vul = Place(ctx, 0, 1, Unit("Vul", 1, 1, 9, "Vulnerable 2"));
        Check(RuleCore.ApplyDamage(ctx, vul, 3, "自检"), 5, "易伤 2 → 吃 3 点掉 5 点");

        // ④ 易伤与护甲的**先后**：先加伤、再减甲（原版 `_damage_unit` 的函数头写着这个顺序）
        var both = Place(ctx, 0, 2, Unit("Both", 1, 1, 9, "Vulnerable 2", "Armour 1"));
        Check(RuleCore.ApplyDamage(ctx, both, 3, "自检"), 4, "易伤2 + 护甲1 吃 3 点 → 掉 4 点（3+2−1）");
    }

    /// <summary>
    /// **攻击时机上的五个关键词**（2026-09-12 第二批）：星镖 / 爆裂 / 震荡 / 嗜血 / 标记光 / 伪装。
    /// 规则书 :170/:172/:173/:177/:192/:207；顺序照原版 `rule_core.gd` 的攻击段。
    /// </summary>
    static void TestAttackKeywords()
    {
        // ① 星镖 X：**攻击伤害之前**先对目标追加 X 点（规则书 :207）
        {
            var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 0, Unit("Shu", 1, 2, 5, "Shuriken 2"));
            var tgt = Place(ctx, 1, 0, Unit("T", 1, 0, 6));       // 0 攻 → 不反击，算式干净
            Check(RuleCore.DeclareAttack(ctx, 0, 0, 1, 0), RuleCodes.OK, "星镖单位可以攻击");
            Check(tgt.Health, 2, $"星镖2 + 2 攻 = 掉 4 点（6 → {tgt.Health}）");
        }
        // ①b 目标被星镖打死 → **跳过攻击伤害**（原版那支 `target_died`）
        {
            var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 0, Unit("Shu", 1, 2, 5, "Shuriken 5"));
            Place(ctx, 1, 0, Unit("T", 1, 0, 2));
            Check(RuleCore.DeclareAttack(ctx, 0, 0, 1, 0), RuleCodes.OK, "星镖单位攻击（星镖就能打死）");
            Check(Board(ctx, 1, 0), null, "目标死在星镖那一步（攻击伤害跳过，格位空了）");
        }
        // ② 爆裂 X：对目标**相邻的敌方部队**溅射 X 点（规则书 :170）
        {
            var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 0, Unit("Bla", 1, 1, 5, "Blast 2"));
            Place(ctx, 1, 0, Unit("T", 1, 0, 6));
            Place(ctx, 1, 1, Unit("Adj", 1, 0, 6));
            Check(RuleCore.DeclareAttack(ctx, 0, 0, 1, 0), RuleCodes.OK, "爆裂单位可以攻击");
            Check(Board(ctx, 1, 0).Health, 5, "主目标掉 1 点（1 攻）");
            Check(Board(ctx, 1, 1).Health, 4, "相邻的掉 2 点（Blast 2 溅射）");
        }
        // ③ 震荡：**被本单位攻击的单位获得眩晕**（规则书 :177）
        {
            var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 0, Unit("Con", 1, 1, 5, "Concussion"));
            var tgt = Place(ctx, 1, 0, Unit("T", 1, 0, 6));
            Check(tgt.IsStunned, false, "打之前没晕");
            RuleCore.DeclareAttack(ctx, 0, 0, 1, 0);
            Check(tgt.IsStunned, true, "挨了震荡单位一下就晕了");
        }
        // ④ 嗜血：一回合能攻击**两次**（规则书 :172）—— 关键在「达到配额上限才疲劳」
        {
            var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            var atk = Place(ctx, 0, 0, Unit("BT", 1, 1, 9, "Blood Thirst"));
            Place(ctx, 1, 0, Unit("T1", 1, 0, 9));
            Place(ctx, 1, 1, Unit("T2", 1, 0, 9));
            Check(RuleCore.DeclareAttack(ctx, 0, 0, 1, 0), RuleCodes.OK, "嗜血单位第 1 次攻击");
            Check(atk.Exhausted, false, "打完第 1 次**不疲劳**（配额还没满）");
            Check(RuleCore.DeclareAttack(ctx, 0, 0, 1, 1), RuleCodes.OK, "嗜血单位第 2 次攻击");
            Check(atk.Exhausted, true, "打完第 2 次就疲劳了");
            Check(RuleCore.DeclareAttack(ctx, 0, 0, 1, 0), RuleCodes.ErrExhausted, "第 3 次被拒");
        }
        // ④b 没有嗜血的单位打完一次就疲劳（旧行为没被改坏）
        {
            var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            var atk = Place(ctx, 0, 0, Unit("Plain", 1, 1, 9));
            Place(ctx, 1, 0, Unit("T1", 1, 0, 9));
            Check(RuleCore.DeclareAttack(ctx, 0, 0, 1, 0), RuleCodes.OK, "普通单位攻击");
            Check(atk.Exhausted, true, "普通单位打完就疲劳");
        }
        // ⑤ 标记光 X：**远程**伤害 +X，受远程伤害后标记光全部移除（规则书 :192）
        {
            var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 0, Ranged("Shooter", 1, 1, 9, 3));
            var tgt = Place(ctx, 1, 0, Unit("Marked", 1, 0, 9, "Markerlight 2"));
            Check(RuleCore.DeclareAttack(ctx, 0, 0, 1, 0, ranged: true), RuleCodes.OK, "远程攻击");
            Check(tgt.Health, 4, $"远程 3 攻 + 标记光 2 = 掉 5 点（9 → {tgt.Health}）");
            Check(tgt.Has("markerlight"), false, "挨过远程伤害后标记光移除");
        }
        // ⑥ 伪装：**攻击后失去**（规则书 :173「攻击前不能被敌方战术/效果选中」）
        {
            var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            var atk = Place(ctx, 0, 0, Unit("Cam", 1, 1, 9, "Camouflage"));
            Place(ctx, 1, 0, Unit("T", 1, 0, 9));
            Check(atk.Has("camouflage"), true, "开打前有伪装");
            RuleCore.DeclareAttack(ctx, 0, 0, 1, 0);
            Check(atk.Has("camouflage"), false, "攻击之后伪装没了");
        }
    }

    // ==================================================================
    //  关键词机制（2026-09-12 第三批：战场事件系）
    // ==================================================================

    /// <summary>
    /// 猎杀标记 / 黑暗契约 / 兽群 / 哨戒 / 狙击 / 再生 / 压制 / 失明。
    ///
    /// 出处逐条写在 `CardDef.Implemented` 的每一条 doc 里（规则书 :189/:179/:195/:205/:209/:201/:194/:166
    /// 对原版 `rule_core.gd:4562/:1663/:4172/:4280/:4312/:2025/:4209/:4212`）。
    /// **每条都要验「真的改变了局面」，不是「解析器认得这个词」** —— 那正是这一批要解决的问题。
    /// </summary>
    static void TestBattleKeywords()
    {
        // ① 猎杀标记：带标记的敌方部队被摧毁 → 敌方督军挨 X 伤、**击杀者的督军**回 X 血
        //    （规则书 :189；原版 `rule_core.gd:4562`）
        {
            var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 0, Unit("Hunter", 1, 3, 5));
            var prey = Place(ctx, 1, 0, Unit("Prey", 1, 0, 2, "Hunt Mark 2"));
            Check(prey.KwValue("huntmark"), 2, "猎物身上有 2 层标记");

            int foeW0 = ctx.Players[1].Warlord.Health;
            int myW0 = ctx.Players[0].Warlord.Health;
            ctx.Players[0].Warlord.Health = myW0 - 5;       // 先掉 5 血，才看得出「治回来了」
            myW0 = ctx.Players[0].Warlord.Health;

            RuleCore.DeclareAttack(ctx, 0, 0, 1, 0);

            Check(Board(ctx, 1, 0), null, "猎物被摧毁离场");
            Check(ctx.Players[1].Warlord.Health, foeW0 - 2, "敌方督军吃了 2 点（= 标记数）");
            Check(ctx.Players[0].Warlord.Health, myW0 + 2, "我方督军回了 2 点");
        }

        // ② 兽群：场上每有 1 个**友方部队** +1 近战 +1 远程（规则书 :195；原版 `:4172`）
        {
            var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            var beast = Place(ctx, 0, 0, Unit("Beast", 1, 2, 9, "Pack"));
            Check(RuleCore.FieldAttack(ctx, 0, beast, false), 3, "场上 1 个友方部队 → 近战 2+1=3");
            Place(ctx, 0, 1, Unit("Buddy", 1, 1, 5));
            Place(ctx, 0, 2, Unit("Buddy2", 1, 1, 5));
            Check(RuleCore.FieldAttack(ctx, 0, beast, false), 5, "3 个友方部队 → 2+3=5");
            Check(RuleCore.FieldAttack(ctx, 0, beast, true), 3, "远程也 +3（基础 0）");
            Check(ctx.Players[0].Warlord.KwValue("pack"), 0, "督军不参与计数（原版过滤条件）");
            // 督军自己带 Pack 时，数的还是**非督军**的友方部队
            Check(RuleCore.FieldAttack(ctx, 0, ctx.Players[0].Warlord, false),
                  ctx.Players[0].Warlord.Attack, "督军不带 Pack 时不加成");
        }

        // ③ 哨戒 X：被攻击时对**攻击者**先造成 X 伤害，「然后照常结算攻击」（规则书 :205；原版 `:4280`）
        {
            var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            var atk = Place(ctx, 0, 0, Unit("Raider", 1, 3, 10));
            var turret = Place(ctx, 1, 0, Unit("Turret", 1, 0, 9, "Sentry 4"));

            RuleCore.DeclareAttack(ctx, 0, 0, 1, 0);

            Check(atk.Health, 6, "攻击者先挨了 4 点哨戒伤害（10 → 6）");
            Check(turret.Health, 6, "但攻击**照常结算** —— 炮台照样吃 3 点（9 → 6）");
        }

        // ④ 狙击：**远程**攻击会摧毁目标 → **不承受反击**（规则书 :209；原版 `:4312`）
        //    ⚠️ 原版有一条修正：「此前『目标死则不反击』= 近战击杀也免反（规则偏差）+ Sniper 成死代码」
        //       —— 所以这条必须验**近战击杀仍然吃反击**，否则就把那个 bug 又做回来了
        {
            var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            var sniper = Place(ctx, 0, 0, Ranged("Sniper", 1, 0, 10, 5, "Sniper"));
            Place(ctx, 1, 0, Unit("Victim", 1, 9, 3));

            Check(RuleCore.DeclareAttack(ctx, 0, 0, 1, 0, ranged: true), RuleCodes.OK, "远程攻击打出去");
            Check(sniper.Health, 10, "Sniper 远程击杀 → **一点反击都没吃**（9 攻的反击被免了）");

            // 对照组：同样 5 伤打死，但**没有 Sniper** → 照样吃 9 点反击
            var ctx2 = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx2, 1);
            var plain = Place(ctx2, 0, 0, Ranged("Plain", 1, 0, 10, 5));
            Place(ctx2, 1, 0, Unit("Victim", 1, 9, 3));
            RuleCore.DeclareAttack(ctx2, 0, 0, 1, 0, ranged: true);
            Check(plain.Health, 1, "没有 Sniper 的远程攻击者照样吃满反击（10 → 1）");

            // 对照组 2：**近战击杀也不免反击**（原版修掉的那个偏差）
            var ctx3 = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx3, 1);
            var melee = Place(ctx3, 0, 0, Unit("Bruiser", 1, 8, 10));
            Place(ctx3, 1, 0, Unit("Victim", 1, 3, 3));
            RuleCore.DeclareAttack(ctx3, 0, 0, 1, 0);
            Check(melee.Health, 7, "近战击杀**仍然吃反击**（10 → 7）");
        }

        // ⑤ 再生 X：每回合结束时治疗 X（规则书 :201；原版 `:2025`）—— **双方单位都治**
        {
            var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            var mine = Place(ctx, 0, 0, Unit("Regen", 1, 1, 8, "Regeneration 2"));
            var theirs = Place(ctx, 1, 0, Unit("Regen2", 1, 1, 8, "Regeneration 3"));
            mine.Health = 4;
            theirs.Health = 2;

            RuleCore.EndTurn(ctx);

            Check(mine.Health, 6, "我方单位回合结束回 2（4 → 6）");
            Check(theirs.Health, 5, "**对方**单位也回（2 → 5）—— 规则书写的是 each turn");
            // 不能超过上限
            mine.Health = 8;
            RuleCore.BeginTurn(ctx);
            RuleCore.EndTurn(ctx);
            Check(mine.Health, 8, "回血不越过上限");
        }

        // ⑥ 压制：**只禁近战**（规则书 :194；原版 `:4209`），远程照常
        {
            var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 0, Ranged("Pinned", 1, 3, 9, 3, "Pindown"));
            Place(ctx, 1, 0, Unit("T", 1, 0, 9));
            CheckCode(RuleCore.DeclareAttack(ctx, 0, 0, 1, 0, ranged: false),
                      RuleCodes.ErrPindown, "被压制的单位不能近战");
            CheckCode(RuleCore.DeclareAttack(ctx, 0, 0, 1, 0, ranged: true),
                      RuleCodes.OK, "但远程照常打得出去");
        }

        // ⑦ 失明：**远程攻击力视为 0**（规则书 :166；原版 `:4212`），到下一回合结束恢复
        {
            // ⚠️ 用 `an enemy **troop**` 而不是 `an enemy`：后者按原版语义**包含督军**，
            //    「随机一个」在两个候选里掷哪一个是合法的 —— 那样测的就是掷骰不是失明了。
            //    这条测试只想要一个确定的靶子。
            var tac = Tactic("T_Blind", 1, "Blind a random enemy troop");
            var ctx2 = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx2, 1);
            var victim = Place(ctx2, 1, 0, Ranged("Shooter", 1, 2, 9, 4));
            Check(RuleCore.PlayTactic(ctx2, 0, HandIdx(ctx2, 0, "T_Blind"), 0), RuleCodes.OK,
                  "「Blind a random enemy troop」打出去了");
            CheckTrue(ReferenceEquals(victim, ctx2.LastTarget), "打中的就是那个部队（不是督军）");
            Check(victim.IsBlind, true, "目标失明了");
            Check(RuleCore.FieldAttack(ctx2, 1, victim, true), 0, "远程攻击力算成 0");
            Check(RuleCore.FieldAttack(ctx2, 1, victim, false), 2, "近战不受失明影响");

            // 到期点：**施放者的下个回合开始时**（和「直到你的下个回合」的限时增益同一个口径）
            Check(victim.BlindTurnEnd, ctx2.Turn + 2, "到期点 = 我的下个回合（当前 + 2：先过对手，再到我）");
            RuleCore.EndTurn(ctx2);                 // P1 的回合结束
            RuleCore.BeginTurn(ctx2);               // ← P2 的回合开始：**这时候必须还瞎着**
            Check(ctx2.Active, 1, "轮到 P2");
            Check(victim.IsBlind, true, "**对手的整个回合里一直失明**（这才是这张牌的用处）");
            RuleCore.EndTurn(ctx2);                 // P2 的回合结束
            RuleCore.BeginTurn(ctx2);               // ← 又是 P1 的回合：到期
            Check(victim.IsBlind, false, "回到施放者的下个回合时恢复");
            Check(RuleCore.FieldAttack(ctx2, 1, victim, true), 4, "远程攻击力回到 4");
        }

        // ⑧ 黑暗契约：四种契约各自的增益**真的加上去了**（规则书 :179；原版 `DARK_PACT_FX:1655`）
        {
            var ctx = Battle(new[] { Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            var u = Place(ctx, 0, 0, Unit("Chosen", 1, 2, 5));
            RuleCore.GrantDarkPact(ctx, 0, u, "excess", "测试");

            Check(u.Has(KeywordTable.DarkPact), true, "身上记着黑暗契约");
            Check(RuleCore.PactOf(u), "excess", "记的是「纵欲」那一份");
            Check(u.Attack, 4, "纵欲：+2 近战（2 → 4）");
            Check(u.RangedAttack, 2, "纵欲：+2 远程（0 → 2）");

            // 命运：+2 生命与伪装
            var v = Place(ctx, 0, 1, Unit("Chosen2", 1, 2, 5));
            RuleCore.GrantDarkPact(ctx, 0, v, "fate", "测试");
            Check(v.MaxHealth, 7, "命运：+2 生命上限（5 → 7）");
            Check(v.Has("camouflage"), true, "命运：还给伪装");

            // 再给一份 → **替换**（原版是赋值不是累加），前一份的增益要跟着走
            RuleCore.GrantDarkPact(ctx, 0, v, "resilience", "测试");
            Check(RuleCore.PactOf(v), "resilience", "契约被替换成「韧性」");
            Check(v.Has("camouflage"), false, "命运给的伪装跟着旧契约一起没了");
            Check(v.Attack, 2, "纵欲/命运本来就没动近战，2 保持不变");
            Check(v.Has("regeneration"), true, "韧性：给了再生");

            // `random` 走种子化随机 —— 同一局面必须永远选到同一个
            var w = Place(ctx, 0, 2, Unit("Chosen3", 1, 2, 5));
            RuleCore.GrantDarkPact(ctx, 0, w, "random", "测试");
            CheckTrue(RuleCore.PactOf(w) != null, "随机契约也落地了（种类：" + RuleCore.PactOf(w) + "）");
        }

        // ⑨ 三选一：`Choose one:` 解析出三个选项，结算时**挑一个**（原版 `_resolve_choose:1159`）
        {
            var tac = Tactic("T_Pick", 1, "Choose one: Draw a card; Heal 1 to your Warlord or Draw 2 cards");
            var ops = EffectText.Parse(tac.Desc, out var un, out var pa);
            Check(un.Count, 0, "三选一没有不认识的句子");
            Check(pa.Count, 0, "三选一没有半懂的句子");
            Check(ops.Count, 1, "产出一条 chooseone");
            Check(ops[0].Verb, "chooseone", "动词是 chooseone");
            Check(ops[0].Amount, 3, "拆出三个选项");
            Check(ops[0].Payload.Split('|').Length, 3, "Payload 里存着三段原文");

            var ctx = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            var wl = ctx.Players[0].Warlord;
            wl.Health = wl.MaxHealth - 5;
            int hp0 = wl.Health, hand0 = ctx.Players[0].Hand.Count;
            Check(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Pick"), -1), RuleCodes.OK,
                  "三选一打得出去");
            // 三个选项都会改变局面：抽牌 +1 / 督军回 1 / 抽 2 张 —— 至少有一个发生了
            bool changed = ctx.Players[0].Hand.Count != hand0 || wl.Health != hp0;
            CheckTrue(changed, "选中的那一项**真的结算了**（手牌或血量变了）");
        }

        // ⑩ 条件句：`targetsurvives` / `targethasarmour` / `controlcount` / `noeffect`
        //    —— 这四类原先一律「判不了」→ **整条效果不生效**（静默失效），现在要真的分岔
        {
            // `If the target survives, heal 1-5 to it`：打不死才治疗
            var tac = Tactic("T_Surv", 1, "Deal 2 damage to an enemy troop. If the target survives, heal 3 to it");
            var ctx = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            var tough = Place(ctx, 1, 0, Unit("Tough", 1, 0, 9));
            Check(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Surv"), 0), RuleCodes.OK, "带条件句的卡打得出去");
            Check(tough.Health, 9, "9 - 2 = 7，条件成立（没被打死）再回 3 → **封顶在上限 9**");

            // 同一个条件在「被打死了」时不成立 —— 用目标只有 2 血重现
            var ctx2 = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx2, 1);
            Place(ctx2, 1, 0, Unit("Frail", 1, 0, 2));
            RuleCore.PlayTactic(ctx2, 0, HandIdx(ctx2, 0, "T_Surv"), 0);
            Check(Board(ctx2, 1, 0), null, "被打死了就离场，治疗那句不生效（条件不成立）");
        }
        {
            // `If it has Armour, deal 8 damage instead` —— **替换**语义
            // ① 有护甲：走 instead 那支的 8 点，**原句 2 点不打**
            var tac = Tactic("T_Arm", 1, "Deal 2 damage to an enemy troop. If it has Armour, deal 8 damage instead");
            var ctx = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            var armored = Place(ctx, 1, 0, Unit("Ar", 1, 0, 20, "Armour 2"));
            Check(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Arm"), 0), RuleCodes.OK, "条件伤害卡打得出去");
            // 8 伤 - 护甲 2 = 6（2 点那句被替换掉了，所以不是 20-2-6=12）
            Check(armored.Health, 14, "有护甲 → 只走「instead」那支的 8 点（20 → 14）");

            // ② 没有护甲：原句 2 点照打，instead 那支不打
            var ctx3 = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx3, 1);
            var plain = Place(ctx3, 1, 0, Unit("Plain", 1, 0, 20));
            RuleCore.PlayTactic(ctx3, 0, HandIdx(ctx3, 0, "T_Arm"), 0);
            Check(plain.Health, 18, "没护甲 → 原句 2 点照打（20 → 18），8 点那句不打");
        }
        {
            // `If you don't control any, create an Intercessor in your hand`
            // —— 本版没有 `create` 造牌，但**条件必须判得出来**，否则整条卡静默失效。
            //    这里只验条件本身：空场时成立、有部队时不成立。
            var seg = EffectText.ParseSegment("If you don't control any, draw a card");
            Check(seg.Kind == EffectText.SegKind.Ok, true, "「you don't control any」现在解得开");
            Check(seg.Ops[0].ConditionKind, "controlcount", "条件类别 = controlcount");
        }

        // ⑪ `for each` 计数层（规则书 :233；原版 `_resolve_for_each:2524` + `_fe_count:1347`）
        //    **三种写法语义不同**，逐种验 —— 光看「解析得了」测不出数对不对
        {
            // ① 后置型 = 重复 N 遍：盘上 2 个友方部队 → 对敌方各打 1 点（打随机目标，共 2 次）
            var tac = Tactic("T_Each", 1, "Deal 1 damage to an enemy troop for each friendly unit");
            var ctx = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            var e1 = Place(ctx, 1, 0, Unit("E1", 1, 0, 9));
            var e2 = Place(ctx, 1, 1, Unit("E2", 1, 0, 9));
            Place(ctx, 0, 0, Unit("MineA", 1, 1, 5));
            Place(ctx, 0, 1, Unit("MineB", 1, 1, 5));
            // **数几个**先单独验一次。
            // 🔴 **2026-09-14 就地更正**：这里原来写的是
            //    `Check(…, "own|troop|all", "数的是「己方部队」（不含督军）")`，
            //    注释还写着「督军不算 —— 卡面写的是 `unit` 不是 `any`」—— **那句是错的**。
            //    规则书中文版 `:70-75`：**单位含督军，部队不含督军**；
            //    参考实现 `rule_core.gd` 的 `_fe_count` 写的是 `troop_only := s.contains("troop")`
            //    —— **只有 `troop` 这个词才排督军**。卡面写的是 `friendly **unit**` ⇒ 督军**要算**。
            //    （错因：解析层当时把 `unit` 归成了 `troop`，这条断言把错的行为钉住了。）
            var eachOps = EffectText.Parse(tac.Desc, out _, out _);
            Check(eachOps[0].CountScope, "board", "解析出盘面计数");
            Check(eachOps[0].CountRef, "own|unit|all",
                  "数的是「己方**单位**」（**含督军**）—— 卡面写的是 `unit`");

            Check(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Each"), 0), RuleCodes.OK, "带 for each 的卡打得出去");
            Check(e1.Health + e2.Health, 15,
                  "3 个己方**单位**（2 个部队 + 督军）→ 打 3 遍 1 伤（18 - 3 = 15）"
                  + "—— 修之前只数 2 个部队、少打 1 点");
        }
        {
            // ② **计数为 0 就是一次都不结算**（不是「退化成 1 次」）
            var tac = Tactic("T_Each2", 1, "Deal 1 damage to an enemy troop for each friendly unit");
            var ctx = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            // 把 P1 的督军血打到 0 是不可能的（那是败北），所以改用「敌方受损单位」这种天然为 0 的计数
            var tac2 = Tactic("T_Each3", 1, "Deal 1 damage to an enemy troop for each damaged enemy");
            var ctx2 = Battle(new[] { tac2, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx2, 1);
            var full = Place(ctx2, 1, 0, Unit("Full", 1, 0, 9));
            RuleCore.PlayTactic(ctx2, 0, HandIdx(ctx2, 0, "T_Each3"), 0);
            Check(full.Health, 9, "「每有一个**受损**敌人」而没人受损 → **一下都不打**（9 血没动）");

            // 同一张卡，先把目标打伤 → 计数 1 → 打 1 遍
            var ctx3 = Battle(new[] { tac2, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx3, 1);
            var hurt = Place(ctx3, 1, 0, Unit("Hurt", 1, 0, 9));
            hurt.Health = 5;
            RuleCore.PlayTactic(ctx3, 0, HandIdx(ctx3, 0, "T_Each3"), 0);
            Check(hurt.Health, 4, "有 1 个受损敌人 → 打 1 遍（5 → 4）");
        }
        {
            // ③ 增量型 = 「基础 + 计数 × 增量」，**不是**重复
            //    `Give +2 Melee Attack to a friendly troop, and an additional +2 Melee Attack
            //     for each Dark Pact on it` —— 一份契约时是 **+4**，不是 +2 再来两遍
            var tac = Tactic("T_Add", 1,
                "Give +2 Melee Attack to a friendly troop, and an additional +2 Melee Attack for each Dark Pact on it");
            var ops = EffectText.Parse(tac.Desc, out var un, out var pa);
            Check(un.Count, 0, "增量型没有不认识的句子");
            Check(pa.Count, 0, "增量型没有半懂的句子");
            Check(ops[0].PerCount, 2, "增量 = 2");
            Check(ops[0].CountScope, "darkpact", "计数对象 = 黑暗契约");
            Check(ops[0].CountRef, "it", "数的是**目标身上**的契约");

            var ctx = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            var plain = Place(ctx, 0, 0, Unit("Plain", 1, 2, 9));
            RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Add"), 0);
            Check(plain.Attack, 4, "没有契约 → 基础 +2（2 → 4），不是 +6");

            // 给它一份契约再打一次 → 2 + 2 + 2×1 = 6
            RuleCore.GrantDarkPact(ctx, 0, plain, "excess", "测试");
            int atkAfterPact = plain.Attack;      // 纵欲契约本身 +2 近战
            Check(atkAfterPact, 6, "纵欲契约自己 +2 近战（4 → 6）");
            int pc = RuleCore.CountForTest(ctx, 0, plain, ops[0]);
            Check(pc, 1, "**结算层**从目标身上数出 1 份契约（和解析层的 `it` 对得上）");
            Give(ctx, 0, tac);
            ctx.Players[0].Energy = 9;
            // 「a friendly troop」要选目标；契约会让它变成 6 + (2 + 2×1) = 10
            // ⚠️ 第二张要打**同一个目标**（契约在它身上才数得到 1）—— 传格位 0
            int pcode = RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Add"), 0);
            Check(pcode, RuleCodes.OK, "第二张也打出去了");
            Check(plain.Attack, 10, "有 1 份契约 → 基础 2 + 2×1 = +4（6 → 10）");
            Check(plain.KwValue(KeywordTable.DarkPact), 1, "契约还是 1 份（没被重复结算加爆）");
        }
        {
            // ④ `For each troop drawn` —— 数的是**同一张卡抽到的牌**
            var tac = Tactic("T_Drawn", 1, "Draw 2 cards. For each troop drawn, gain 1 Energy");
            var ctx = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            // 牌库里的垫牌是 `filler*`（都是 unit 类型）→ 抽 2 张就是 2 个部队
            int energy0 = ctx.Players[0].Energy;
            Check(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Drawn"), -1), RuleCodes.OK, "抽牌 + for each 的卡打得出去");
            Check(ctx.Players[0].Energy, energy0 - 1 + 2, "抽 2 张部队 → 回 2 能（打这张卡花了 1 能）");
        }
        {
            // ⑤ `For each one that dies` —— 数的是本回合阵亡数
            var tac = Tactic("T_Died", 1, "Deal 3 damage to an enemy troop for each one that dies");
            var ctx = Battle(new[] { tac, Unit("A", 1, 1, 1) }, new[] { Unit("X", 1, 1, 1) });
            ToP1Turn(ctx, 1);
            Place(ctx, 1, 0, Unit("Prey", 1, 0, 1));      // 1 血，3 攻一击必杀
            Check(ctx.DiedThisTurn, 0, "开局没人阵亡");

            Place(ctx, 0, 0, Unit("Killer", 1, 3, 5));
            int kcode = RuleCore.DeclareAttack(ctx, 0, 0, 1, 0);
            Check(kcode, RuleCodes.OK, "杀手打得出去");
            Check(Board(ctx, 1, 0), null, "猎物被摧毁、格位空了");
            Check(ctx.DiedThisTurn, 1, "打死了一个 → 本回合阵亡数 1");

            // 现在打这张卡：计数 1 → 打 1 遍 3 点
            Place(ctx, 1, 1, Unit("Next", 1, 0, 9));
            RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Died"), 1);
            Check(Board(ctx, 1, 1).Health, 6, "阵亡 1 个 → 打 1 遍 3 点（9 → 6）");
        }

        // ⑫ 卡池实测：真原版卡里这几类关键词确实存在（别只在合成卡上验）
        {
            var pool = CardDatabase.Load();
            int n = 0;
            foreach (var c in pool)
                if (c != null && c.Keywords.ContainsKey("huntmark")) n++;
            CheckTrue(n > 0, $"卡池里带猎杀标记的卡有 {n} 张");
        }
    }

    // ==================================================================
    //  造牌（`create`）
    // ==================================================================

    /// <summary>
    /// `Create …` —— 造牌。**规格书是规则书附录 B（生成卡的阵营指南）与附录 C（骰子查找表）**
    /// （`资料/规则书/Warpforge_Offline_Rulebook_1_5-3_中文翻译.md:254-320`）。
    ///
    /// 三层都要验，缺一层就会漏掉一类静默失效：
    ///   ① **解析** —— 张数 / 「造什么」原文 / **目的地**（送错人 = 另一张卡）；
    ///   ② **候选池** —— 按卡池实算，和附录 B 的「几种」、附录 C 的名单逐个对；
    ///   ③ **结算** —— 真打一张原版卡，看卡有没有真进手牌、池子对不对、同种子可复现。
    /// </summary>
    static void TestCreate()
    {
        var pool = CardDatabase.Load();

        // ---- ① 解析：三种写法（全卡池实测 28 个分句 / 20 张卡）----
        {
            var op = OneOp("Create a Termagant in your hand");
            Check(op.Verb, "create", "`Create …` → 动词 create");
            Check(op.Amount, 1, "`a Termagant` → 1 张");
            Check(op.Payload, "termagant", "「造什么」存进 Payload");
            Check(op.Dest, "hand", "`in your hand` → 自己手牌");

            op = OneOp("Create three Ultramarines Vehicles in your hand");
            Check(op.Amount, 3, "`three` → 3 张");
            Check(op.Payload, "ultramarines vehicles", "阵营词和兵种词一起留在 Payload 里");

            // 目的地**前置**的写法（`Drone Companion` 卡面就是这个语序）
            op = OneOp("Create in your hand a Gun Drone, Guardian Drone or Marker Drone");
            Check(op.Amount, 1, "目的地前置时数量照样剥得掉");
            Check(op.Dest, "hand", "目的地前置也认得出来");
            CheckTrue(op.Payload.Contains(" or "), "三选一的名单原样留着（结算时随机挑一个）");

            op = OneOp("Create a random Sabotage in the enemy hand");
            Check(op.Dest, "enemyhand", "`in the enemy hand` → **对手**手牌（送到自己手上就是另一张卡）");
            Check(op.Payload, "random sabotage", "`random` 留在原文里（选法由结算层掷）");

            op = OneOp("Create a copy of it at the top of your deck");
            Check(op.Dest, "decktop", "`at the top of your deck` → 牌库顶");
            Check(op.Payload, "copy of it", "`copy of it` 原样记下来（由结算层取上一条效果的目标）");

            op = OneOp("Create one Neophyte Hybrid in your hand for each enemy unit");
            Check(op.Amount, 1, "`one …` → 1 张");
            CheckTrue(!string.IsNullOrEmpty(op.CountRef), "`for each enemy unit` 被计数层剥进 CountRef");

            // ⚠️ **没写目的地必须判失败** —— 默认成手牌就是把牌送错人，最难查的一类
            var seg = EffectText.ParseSegment("Create a Termagant");
            Check(seg.Kind, EffectText.SegKind.Unknown, "没写目的地 → 判「不认识」，不猜成手牌");
        }

        // ---- ② 候选池：卡池实算 vs 规则书附录 B「几种」/ 附录 C 名单 ----
        // ⚠️ 一律走 **卡面原文 → 解析 → Payload → 算池子** 这条真路，
        //    不手写 payload —— 手写的那份和解析器产出的会悄悄不一样（第一版就是这么错的：
        //    手写成 `a gun drone, …`，而解析器早就把冠词当数量剥掉了）。
        {
            // 附录 B「装甲攻势：3 个极限战士载具（**18 种**，各 3 张，54-216 总计）」
            var r = PoolOf(pool, "Create three Ultramarines Vehicles in your hand", "Ultramarines");
            CheckTrue(r.Ok, "`three Ultramarines Vehicles` 的池子算得出来");
            Check(r.Cards.Count, 18, "Ultramarines 载具 18 张 == 附录 B 的「18 种」");
            CheckAll(r.Cards, c => c.Faction == "Ultramarines" && c.Subtype == "Vehicle",
                     "池子里每一张都是 Ultramarines 载具");

            // 附录 B「狂野宿主：2 个随机载具（**20 种**，各 2 张）」
            r = PoolOf(pool, "Create two random Saim-Hann Vehicles in your hand", "SaimHann");
            Check(r.Cards.Count, 20, "SaimHann 载具 20 张 == 附录 B 的「20 种」（`Saim-Hann` 的连字符要归一化掉）");

            // 附录 B「比你们快：3 个兽人载具（**14 种**）」—— 卡面写**种族名** `Ork`，卡池里叫 `Goff`
            r = PoolOf(pool, "Create three random Ork Vehicles in your hand", "Goff");
            Check(r.Cards.Count, 14, "`Ork` → `Goff`（别名表），14 张 == 附录 B 的「14 种」");

            // 附录 B「虫群大军：3 个虫群部队（10 种）」+ 附录 C 的 1d10 名单
            r = PoolOf(pool, "Create 3 random Leviathan troops with Swarm in your hand", "Leviathan");
            Check(r.Cards.Count, 11, "Leviathan 带虫群的**部队** 11 张（附录 B 写 10，差 1 —— 见下）");
            CheckTrue(!r.Cards.Exists(c => c.Name == "Swarming Masses"),
                      "`Swarming Masses` 是战术卡，不在「troops」池里");

            // 附录 B「锈蚀通风口：手牌生成随机带伏击部队」+ 附录 C 的 1d11 名单
            r = PoolOf(pool, "Create a random troop with Ambush in your hand", "Genestealers");
            Check(r.Cards.Count, 11, "基因窃取者带伏击的部队 11 张 == 附录 C「锈蚀通风口(1d11)」的 11 个名字");

            // `Combat Elixir` / `Sabotage` 在原版数据里是**兵种**（subtype）不是卡名 ——
            // 这一点以前在计划文档里记反了（记成「原版数据里没有这张卡」），见文末更正
            r = PoolOf(pool, "Create 3 random Combat Elixir in your hand", "EmperorsChildren");
            // ⚠️ 2026-09-13：这里原来写 5 张、注释是「附录 C 的 1d6 是 6 个名字，**差 1**」。
            //    那差的一张是 `Shivversplint` —— 它的 subtype 在 OCR 源表里被写成了 `Upgrade`。
            //    **卡面逐张核对**（见 `CARD_FACE_FIXES_SRC`）把它修成 `Combat Elixir` 之后，
            //    附录 C 的 6 个名字**全对上了**。见下面 `Shivversplint` 那条。
            Check(r.Cards.Count, 6, "战斗药剂 6 张 == 附录 C「战斗药剂(1d6)」的 6 个名字（2026-09-13 起全中）");
            r = PoolOf(pool, "Create a random Sabotage in the enemy hand", "Genestealers");
            // ⚠️ 2026-09-14 A4 批 4：这里原来钉 **2 张**（`Improvised Barricade` / `Poisoned Supplies`）——
            //    那**不是**真值，是**数据缺口**造成的：`Cult Propaganda` 的 subtype 被记成 `Spell`、
            //    `Jammed Communications` 记成 `Stratagem`，两张都进不了这个池子。
            //    **四张破坏卡的卡面橙字都是 `Sabotage`**（`d:/2/Warpforge部队卡片/Genestealer Cult/
            //    6破坏卡/`，2026-09-14 逐张开图读过）⇒ 修数据（`cardface_fixes.json` 的
            //    `_manual_subtype`）之后池子是 4 张。**这才是对的**（附录里破坏卡就是 4 张）。
            Check(r.Cards.Count, 4,
                  "破坏 4 张（`Improvised Barricade` / `Poisoned Supplies` / `Cult Propaganda` / "
                  + "`Jammed Communications`）—— 2026-09-14 修 subtype 之前只有 2 张");

            // 三选一名单：三个都要在原版数据里找得到
            r = PoolOf(pool, "Create in your hand a Gun Drone, Guardian Drone or Marker Drone", "TauEmpire");
            Check(r.Cards.Count, 3, "`Gun Drone / Guardian Drone / Marker Drone` 三张都在卡池里");

            // 具名卡：原版数据里撇号被剥掉了（`Abaddon's Chosen` → `Abaddons Chosen`）
            r = PoolOf(pool, "Create a copy of Ahnakh-Yth Shrine in your hand", "SaimHann");
            Check(r.Cards.Count, 1, "`Create a copy of <卡名>` → 池子里就那一张");
            r = PoolOf(pool, "For each troop drawn, create a copy of Abaddon's Chosen in your hand", "BlackLegion");
            CheckTrue(r.Ok, "卡面写 `Abaddon's Chosen`、数据里写 `Abaddons Chosen` —— 归一化后查得到");
            Check(r.Cards[0].Name, "Abaddons Chosen", "查到的就是数据里那张");

            // ⚠️ **查不到的卡必须如实报**，不许造效果（本工程红线）。
            //    而且要把「到底是没有，还是名字写岔了」分清楚 —— 2026-09-12 之前计划文档
            //    把下面这五种统统一句「原版数据里没有」带过，**其中三种是错的**。
            {
                // ① 名字写岔了（数据是 OCR + 手抄来的）：报出来，但**不拿近似的顶替**
                //    走 `PoolOf`（真解析）而不是手写 payload —— 手写会把冠词带进去，见 `PoolOf` 的注释
                var a = PoolOf(pool, "Create a Sergeant Taaman in your hand", "DarkAngels");
                CheckTrue(!a.Ok, "`Sergeant Taaman` 按**这个名字**查不到");
                CheckTrue(a.Why.Contains("Sergeant Naaman"),
                          "……但如实报出最接近的是 `Sergeant Naaman`（实体卡名 vs 数据名的抄写差）");

                // ①-b **差一个尾 `s` 的**（`Extermination Protocol` ↔ `Extermination Protocols`）——
                //     🔴 **2026-09-16 改判**：这一条原来钉的是「只报近似的、**不拿它顶替**」，
                //     现在**改成认了**。理由（铁律 7：**逐张开成品卡图核过**）：
                //       · `Awakened Obelisk`（`SAU65` · Defence）卡面印的是
                //         `… add **Extermination Protocol** to your hand` —— **单数**
                //         （`d:/2/Warpforge部队卡片/Necron/5防御卡/Warpforge_65_Awakened-Obelisk.png`）；
                //       · 而池里那张（`SAU45`）**自己那张 PnP 卡图印的也是单数**
                //         （`Necron/4计策/Warpforge_45_Extermination-Protocol.png`，`Deal 2 damage`）
                //         ⇒ 是**我们的卡名多抄了一个 `s`**，不是两张卡。
                //     ⇒ 判据：`CreatePool.FindByNameLoose`（**双向**剥尾 `s`）。
                //     ⚠️ 安全：全池**没有**两张卡只差一个尾 `s`（实测 0 组）⇒ 不会撞车。
                //     ⚠️ 与上面 `Sergeant Taaman` 那一类的**区别**：那个不是单复数
                //        ⇒ `FindByNameLoose` 碰不到它，**名字写岔了仍然只报不替**。
                var b = PoolOf(pool, "Create an Extermination Protocol in your hand", "Sautekh");
                CheckTrue(b.Ok && b.Cards.Count == 1
                          && b.Cards[0].Name.StartsWith("Extermination Protocol"),
                          "★ `Extermination Protocol`（单数）现在**认得出来**了 —— 池里那张写成复数，"
                          + "而**两张卡图印的都是单数**（`SAU65` 的引用 + `SAU45` 自己的卡面）"
                          + " ⇒ 是我们的卡名多抄了一个 `s`。实得 "
                          + (b.Ok ? b.Cards.Count + " 张「" + b.Cards[0].Name + "」" : "失败：" + b.Why));

                // ② 真的没有：连近似的都找不到（实体卡表 `卡牌信息权威表_0824.md` 里也没有）
                foreach (string missing in new[] { "Vindicare Assassin", "Vitric Consul" })
                {
                    var mr = PoolOf(pool, "Create a " + missing + " in your hand", "Ultramarines");
                    CheckTrue(!mr.Ok && mr.Why.Contains("原版数据里没有这张卡"),
                              $"`{missing}` 原版数据里没有 —— 如实报，不造效果");
                    CheckTrue(CreatePool.FindNearMiss(pool, missing) == null,
                              $"……`{missing}` 连近似的名字都没有（是真缺，不是抄错）");
                }
            }

            // 没有卡池 → 如实报，**不退化**成从牌库里抽
            var noPool = CreatePool.Resolve(null, "a termagant", "Leviathan");
            CheckTrue(!noPool.Ok && noPool.Why.Contains("卡池"), "不给卡池 → 明说「没有卡池」");
        }

        // ---- ③ 附录 C 的骰子表 vs 卡池实算 —— 逐个名字对，差一个都要报出来 ----
        CheckDiceTables(pool);

        // ---- ④ 结算：真打原版卡 ----
        {
            // `Mercurial Host`（帝皇之子 / 4 费）：`Create 3 random Combat Elixir in your hand`
            var mercurial = CardDatabase.Find(pool, "Mercurial Host");
            CheckTrue(mercurial != null, "卡池里有 `Mercurial Host`");

            var ctx = BattlePool(new[] { mercurial }, new[] { Unit("X", 1, 1, 5) }, pool,
                                 warlordFaction: "EmperorsChildren");
            ToP1Turn(ctx, 3);                       // 4 费，攒够能量
            int before = ctx.Players[0].Hand.Count;
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "Mercurial Host"), -1), RuleCodes.OK,
                      "`Mercurial Host` 打得出去");
            Check(ctx.Players[0].Hand.Count, before - 1 + 3, "手牌 −1（打出去的）+3（造出来的）");
            int elixirs = 0;
            foreach (var c in ctx.Players[0].Hand)
                if (c.Card.Subtype == "Combat Elixir" || c.Card.Subtype == "Elixir") elixirs++;
            Check(elixirs, 3, "造出来的 3 张都是战斗药剂");
            CheckTrue(ctx.Events.Exists(e => e.Contains("造了 3 张")), "日志里说了造了 3 张");

            // **同种子必须造出同样的牌**（对局可复现是硬要求）
            var ctx2 = BattlePool(new[] { mercurial }, new[] { Unit("X", 1, 1, 5) }, pool,
                                  warlordFaction: "EmperorsChildren");
            ToP1Turn(ctx2, 3);
            RuleCore.PlayTactic(ctx2, 0, HandIdx(ctx2, 0, "Mercurial Host"), -1);
            var a = ctx.Players[0].Hand; var b = ctx2.Players[0].Hand;
            bool same = a.Count == b.Count;
            for (int i = 0; same && i < a.Count; i++) same = a[i].Card.Name == b[i].Card.Name;
            CheckTrue(same, "同一个种子造出同样的牌（走 ctx.Rng，不是 UnityEngine.Random）");

            // `Create … in the enemy hand`：牌进的是**对手**手里
            // 用合成卡而不是 `Underground Network`：后者第二句（`For the rest of this battle,
            // Sabotage cards … cost 1 more`）还没实现，整张卡会被 `CanPlayTactic` 拒掉 —— 那条路另外验。
            var foeHand = Tactic("T_FoeCreate", 1, "Create a random Sabotage in the enemy hand");
            var ctx3 = BattlePool(new[] { foeHand }, new[] { Unit("X", 1, 1, 5) }, pool,
                                  warlordFaction: "Genestealers");
            ToP1Turn(ctx3, 1);                      // 1 费
            int foeBefore = ctx3.Players[1].Hand.Count;
            int ownBefore = ctx3.Players[0].Hand.Count;
            CheckCode(RuleCore.PlayTactic(ctx3, 0, HandIdx(ctx3, 0, "T_FoeCreate"), -1),
                      RuleCodes.OK, "`Create … in the enemy hand` 打得出去");
            Check(ctx3.Players[1].Hand.Count, foeBefore + 1, "造出来的破坏牌进了**对手**手牌");
            Check(ctx3.Players[0].Hand.Count, ownBefore - 1, "自己手上一张都没多（只少了打出去的那张）");
            Check(ctx3.Players[1].Hand[ctx3.Players[1].Hand.Count - 1].Card.Subtype, "Sabotage",
                  "送过去的那张确实是破坏");

            // `Create a copy of it at the top of your deck`：`it` = 上一条效果的目标
            var vengeful = CardDatabase.Find(pool, "Vengeful Brethren");
            var troop = Unit("CopyMe", 1, 1, 3);
            var ctx4 = BattlePool(new[] { vengeful, troop }, new[] { Unit("X", 1, 3, 3) }, pool,
                                  warlordFaction: "DarkAngels");
            ToP1Turn(ctx4, 1);                      // 1 费
            Place(ctx4, 0, 0, troop);
            int deckBefore = ctx4.Players[0].Deck.Count;
            CheckCode(RuleCore.PlayTactic(ctx4, 0, HandIdx(ctx4, 0, "Vengeful Brethren"), 0), RuleCodes.OK,
                      "`Vengeful Brethren` 打得出去（选了自己那个部队当目标）");
            Check(ctx4.Players[0].Deck.Count, deckBefore + 1, "复制出来的那张进了自己牌库");
            Check(ctx4.Players[0].Deck[ctx4.Players[0].Deck.Count - 1].Card.Name, "CopyMe",
                  "牌库**顶**（末尾，抽牌从末尾抽）那张就是被复制的那张卡");

            // **没有卡池的对局**：造牌必须如实报「没生效」，不许静默什么都不做
            var noPoolCtx = Battle(new[] { mercurial }, new[] { Unit("X", 1, 1, 5) });
            ToP1Turn(noPoolCtx, 3);
            int h = noPoolCtx.Players[0].Hand.Count;
            RuleCore.PlayTactic(noPoolCtx, 0, HandIdx(noPoolCtx, 0, "Mercurial Host"), -1);
            Check(noPoolCtx.Players[0].Hand.Count, h - 1, "没卡池 → 一张都没造出来");
            CheckTrue(noPoolCtx.Events.Exists(e => e.Contains("没有卡池")),
                      "没卡池 → 日志里明说原因（不静默失败）");
        }
    }

    /// <summary>
    /// **两个阵营打起来顺不顺**（用户 2026-09-12 指定：先做极限战士 + 兽人，然后检查对战）。
    ///
    /// 做法：**极限战士 vs 兽人**各凑一副真卡组，跑若干局完整对局（双方都走 `SimpleAI`，
    /// 而它 2026-09-12 起**会出战术卡了**），然后把整局里所有
    /// 「没生效 / 没实现 / 判不了 / 数不出来」的话**去重收上来**。
    ///
    /// 为什么不能只看「跑完了没崩」：**跑得完不等于跑得对**。
    /// 一张效果没结算的卡照样能让对局正常结束，只在日志里留一行 ——
    /// 这一节就是把那些行拎出来，让「不顺利」有个数字。
    /// </summary>
    static void TestFactionBattle()
    {
        var pool = CardDatabase.Load();
        if (pool.Count == 0) { CheckTrue(false, "卡表没加载上"); return; }

        const int Games = 6;
        int finished = 0, p1 = 0, p2 = 0, draw = 0, turns = 0, tacticsPlayed = 0;
        int eventsScanned = 0;
        int chooses = 0;                  // 选牌**成功**结算的次数（见下面扫日志那段）
        var warns = new Dictionary<string, int>();
        var warnExample = new Dictionary<string, string>();

        for (int g = 0; g < Games; g++)
        {
            // ⚠️ **先手方要换着来**：固定让极限战士先手的话，赢的那方是「先手」还是「阵营强」分不清
            //    （第一版 6:0，看不出名堂）。偶数局极限战士先手、奇数局兽人先手。
            bool umFirst = (g % 2 == 0);
            var dUM = DeckBuilder.StarterDeck(pool, "Ultramarines", DeckBuilder.ClassicDeckSize,
                                              new System.Random(100 + g), unitsOnly: false);
            var dGK = DeckBuilder.StarterDeck(pool, "Goff", DeckBuilder.ClassicDeckSize,
                                              new System.Random(200 + g), unitsOnly: false);
            var d0 = umFirst ? dUM : dGK;
            var d1 = umFirst ? dGK : dUM;
            // ⚠️ **必须传卡池** —— `create` / `deploy` 从全卡池筛候选；不传的话那一族会
            //    「如实报没有卡池然后什么都不做」（引擎的设计如此），这一节就测了个寂寞。
            var ctx = RuleCore.NewBattle(d0, d1, seed: 7000 + g, cardPool: pool);

            int guard = 0;
            while (!ctx.IsOver && guard++ < 300)
            {
                RuleCore.BeginTurn(ctx);
                PlayAiTurn(ctx, ref tacticsPlayed);
                if (ctx.IsOver) break;
                RuleCore.EndTurn(ctx);
            }
            if (ctx.IsOver) finished++;
            turns += ctx.Turn;
            // 按**阵营**记胜负（不是按座位）—— 座位是轮流先手的
            if (ctx.Winner == 3) draw++;
            else if (ctx.Winner != 0)
            {
                string winnerFac = (ctx.Winner - 1 == 0) == umFirst ? "Ultramarines" : "Goff";
                if (winnerFac == "Ultramarines") p1++; else p2++;
            }

            eventsScanned += ctx.Events.Count;
            foreach (string e in ctx.Events)
            {
                // 选牌**成功**结算的次数 —— 成功不打「没生效」，所以得单独数一条
                // （失败的那条走下面 `没生效` 的通道，两者合起来才是这一族的全貌）
                if (e.IndexOf("选了「", System.StringComparison.Ordinal) >= 0) chooses++;

                if (e.IndexOf("没生效", System.StringComparison.Ordinal) < 0
                    && e.IndexOf("没实现", System.StringComparison.Ordinal) < 0
                    && e.IndexOf("没结算", System.StringComparison.Ordinal) < 0
                    && e.IndexOf("判不了", System.StringComparison.Ordinal) < 0
                    && e.IndexOf("数不出来", System.StringComparison.Ordinal) < 0) continue;
                // 归一化：把「N 条」「具体卡名」摘掉，同类只留一条，好看清有几个**种类**
                string key = e;
                int w = key.IndexOf("有 ", System.StringComparison.Ordinal);
                if (w >= 0 && key.IndexOf(" 条效果本版没结算", System.StringComparison.Ordinal) > w)
                    key = key.Substring(0, w) + "有 N 条效果本版没结算：" + Tail(key);
                int n0;
                warns[key] = warns.TryGetValue(key, out n0) ? n0 + 1 : 1;
                if (!warnExample.ContainsKey(key)) warnExample[key] = e;
            }
        }

        Debug.Log(P + $"   两个阵营打了 {Games} 局：完成 {finished} · P1(极限战士) 胜 {p1} · "
                  + $"P2(兽人) 胜 {p2} · 平 {draw} · 平均 {turns / Games} 回合 · "
                  + $"**双方共打出战术卡 {tacticsPlayed} 张** · 其中选牌结算成功 {chooses} 次");

        Check(finished, Games, $"{Games} 局全部打出结果（没有卡死的）");
        CheckTrue(tacticsPlayed > 0, $"战术卡真的被打出来了（{tacticsPlayed} 张）—— AI 不再是只出单位卡");
        CheckTrue(tacticsPlayed >= Games, $"……而且每局平均不止一张（{tacticsPlayed}/{Games}）");

        // 选牌这一族**必须在实战里真跑到** —— 单元自检里跑通 ≠ 真打起来会走到
        // （第十六轮就是这么发现「战术卡用例全绿、真打起来却从来没碰过」的，见
        //  `资料/阵营推进_清单与交接.md` §六）。种子里程碑：极限战士在牌组里有 5 张选牌卡。
        CheckTrue(chooses > 0, $"选牌（`Choose a …`）在实战里真的结算成功过（{chooses} 次）");

        // 把「不顺利」按种类列出来
        var list = new List<KeyValuePair<string, int>>(warns);
        list.Sort((a, b) => b.Value.CompareTo(a.Value));
        Debug.Log(P + $"   对局里出现的「没结算/判不了」共 {list.Count} 种"
                  + $"（扫了 {eventsScanned} 条事件日志）");
        int shown = 0;
        foreach (var kv in list)
        {
            if (shown++ >= 12) { Debug.Log(P + $"     ……还有 {list.Count - 12} 种"); break; }
            Debug.Log(P + $"     ×{kv.Value,-3} {kv.Key}");
        }

        // ⚠️ **「动词没实现」必须是 0** —— 四个动词现在都实现了。
        //    这一条是**回退报警**：以后谁加了新动词却忘了登记，这里立刻红。
        int unimplemented = 0;
        foreach (var kv in list)
            if (kv.Key.IndexOf("的动作「", System.StringComparison.Ordinal) >= 0) unimplemented += kv.Value;
        Check(unimplemented, 0, "整局里**没有**「动词本版没实现」—— 登记的动词表是全的");
    }

    /// <summary>取一段日志里 `：` 之后的内容（归一化警告用的）</summary>
    static string Tail(string e)
    {
        int c = e.IndexOf('：');
        return c >= 0 && c + 1 < e.Length ? e.Substring(c + 1) : "";
    }

    /// <summary>跑完一方的 AI 回合：出牌（含战术卡）→ 攻击。判据全走 `SimpleAI` / 引擎。</summary>
    static void PlayAiTurn(BattleContext ctx, ref int tacticsPlayed)
    {
        int p = ctx.Active;
        for (int guard = 0; guard < 30 && !ctx.IsOver; guard++)
        {
            int ci, sl;
            if (!SimpleAI.NextPlay(ctx, out ci, out sl)) break;
            var card = ctx.Players[p].Hand[ci].Card;
            if (RuleCore.PlayCard(ctx, p, ci, sl) != RuleCodes.OK) break;
            if (!card.IsUnit) tacticsPlayed++;
        }

        for (int guard = 0; guard < 40 && !ctx.IsOver; guard++)
        {
            int a, tp, ts;
            bool ranged;
            if (!SimpleAI.NextAttack(ctx, out a, out tp, out ts, out ranged)) break;
            if (RuleCore.DeclareAttack(ctx, p, a, tp, ts, ranged) != RuleCodes.OK) break;
        }
    }

    /// <summary>
    /// **阵营机制与最后一个动词**：`repeat`（重放）+ `Oath N:`（付费触发）+ `Codex:`（能量为 0 时触发）。
    ///
    /// 三个都有**规则书明文**，不是从卡面猜的：
    ///   · `repeat`  —— `rule_core.gd:2542` → `_resolve_repeat`「把本句之前的效果再来一遍」
    ///   · `Oath X`  —— 规则书 :194「部署时支付 X 能量以触发效果」
    ///   · `Codex`   —— 规则书 :175「你的能量为 0 时触发效果」
    /// </summary>
    static void TestFactionMechanics()
    {
        // ---- 付费激活前缀的**三种写法**（2026-09-13 第三十二轮扩宽正则）----
        // 出处：`rule_core.gd:1549` 的 `\[?[Ee]nergy\]?` —— **方括号可选**。
        // ⚠️ 我们原来把它抄成必选，于是无括号那两张卡整句判不认识（查 47 条时定位出来的）。
        // ⚠️ **断言要钉「解析出来的长什么样」**，不能只钉「认识不认识」——
        //    第十六轮的教训：`lowercost` 的正则把 payload 切成了 `f all vehicles…`，
        //    **而解析判定照样是 Ok**。只断「认不认识」抓不住这类静默错解析。
        {
            var pool = CardDatabase.Load();
            foreach (var (desc, wantCost, wantKind, wantVerb, wantAmount) in new[]
                     {
                         ("Gain 3 [Energy]. 12 [Energy]: Draw 3 cards", 12, "energy", "draw", 3),
                         ("Give +2 Armor to a friendly unit this turn. 5 Energy: Draw a card",
                          5, "energy", "draw", 1),
                         // ⚠️ 动词是 **`drawtype`** 不是 `draw`：`Draw a troop` 是**定向翻找**
                         //    （从牌库翻到匹配的那张），和 `Draw a card`（抽顶上那张）不是一回事。
                         //    这条有断言盯着（`Payload` 要是兵种词）—— 别把它"修"成 draw。
                         ("Draw a troop. 2 : Draw an additional troop", 2, "", "drawtype", 1),
                     })
            {
                var ops = EffectText.Parse(desc, out _, out _);
                CheckTrue(ops != null && ops.Count > 0, $"`{desc}` 解析得出");
                if (ops == null || ops.Count == 0) continue;
                // 正文那条 = 最后一条（前缀是在 Dispatch 之前剥掉的，代价落在**这一段**的所有 op 上）
                var paid = ops[ops.Count - 1];
                Check(paid.Cost, wantCost, $"★ 代价 = {wantCost}（`{desc}`）");
                Check(paid.CostKind ?? "", wantKind,
                      $"★ 货币 = `{(string.IsNullOrEmpty(wantKind) ? "(无货币词 → 按能量)" : wantKind)}`"
                      + "（括号可选那条坑就在这儿：抄成必选的话这整句根本不认识）");
                // ⚠️ **钉「切出来的正文长什么样」，不只钉「认不认识」** —— 第十六轮的教训：
                //    `lowercost` 的正则把 payload 切成 `f all vehicles…` 而判定照样 Ok。
                Check(paid.Verb, wantVerb, $"★ 正文动词 = `{wantVerb}`（切对了，没把前缀吃进正文）");
                Check(paid.Amount, wantAmount, $"★ 正文数量 = {wantAmount}（`additional` 不该改数量）");
                _ = pool;
            }
        }

        // ---- `repeat`：**重放的是「本句之前」的效果** ----
        {
            var tac = Tactic("T_Repeat", 1, "Deal 2 damage to an enemy. Repeat this effect");
            var ctx = BattlePool(new[] { tac }, new[] { Unit("X", 1, 2, 5) }, CardDatabase.Load());
            ToP1Turn(ctx, 1);
            Place(ctx, 1, 0, Unit("Victim", 1, 1, 9));
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Repeat"), 0), RuleCodes.OK,
                      "`Deal 2 damage. Repeat this effect` 打得出去");
            Check(Board(ctx, 1, 0).Health, 5, "9 血挨了**两遍** 2 点（9 → 5）：repeat 真的重放了前面那条");
            CheckTrue(ctx.Events.Exists(e => e.Contains("重复一遍")), "日志里说了在重复");

            // `repeat` 前面没有效果 → **如实报**，不许静默什么都不做
            var lone = Tactic("T_RepeatLone", 1, "Repeat this effect");
            var c2 = BattlePool(new[] { lone }, new[] { Unit("X", 1, 1, 5) }, CardDatabase.Load());
            ToP1Turn(c2, 1);
            var ops2 = EffectText.Parse("Repeat this effect", out _, out _);
            CheckTrue(ops2.Count == 1 && ops2[0].RepeatOps != null && ops2[0].RepeatOps.Count == 0,
                      "孤零零一句 `Repeat this effect` → RepeatOps 是空表（不是 null）");
        }

        // ---- `Oath N:`：**付得起才触发**，付不起整条不生效 ----
        {
            var tac = Tactic("T_Oath", 1, "Draw a card. Oath 3: Draw 2 cards");
            var pool = CardDatabase.Load();

            // 第 1 回合 2 能：付 1 打牌 → 剩 1 < 3 → Oath 不触发
            var ctx = BattlePool(new[] { tac }, new[] { Unit("X", 1, 1, 9) }, pool);
            ToP1Turn(ctx, 1);
            int h0 = ctx.Players[0].Hand.Count;
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Oath"), -1), RuleCodes.OK, "Oath 卡打得出去");
            Check(ctx.Players[0].Hand.Count, h0 - 1 + 1, "能量不够付 Oath → 只结算基础的那 1 张");

            // 第 3 回合 4 能：付 1 打牌 → 剩 3 ≥ 3 → Oath 触发
            var ctx2 = BattlePool(new[] { tac }, new[] { Unit("X", 1, 1, 9) }, pool);
            ToP1Turn(ctx2, 3);
            int h1 = ctx2.Players[0].Hand.Count;
            int e1 = ctx2.Players[0].Energy;
            CheckCode(RuleCore.PlayTactic(ctx2, 0, HandIdx(ctx2, 0, "T_Oath"), -1), RuleCodes.OK, "打得出去");
            Check(ctx2.Players[0].Hand.Count, h1 - 1 + 3, "付得起 → 1 张基础 + 2 张 Oath");
            Check(ctx2.Players[0].Energy, e1 - 1 - 3, $"付了卡费 1 + Oath 3（{e1} → {e1 - 4}）");
        }

        // ---- `Codex:`：**能量为 0 时才触发** ----
        {
            var tac = Tactic("T_Codex", 2, "Draw a card. Codex: Draw 2 cards");
            var pool = CardDatabase.Load();

            // 第 1 回合 2 能：正好花光 → Codex 触发
            var ctx = BattlePool(new[] { tac }, new[] { Unit("X", 1, 1, 9) }, pool);
            ToP1Turn(ctx, 1);
            int h0 = ctx.Players[0].Hand.Count;
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Codex"), -1), RuleCodes.OK, "Codex 卡打得出去");
            Check(ctx.Players[0].Energy, 0, "正好花光能量");
            Check(ctx.Players[0].Hand.Count, h0 - 1 + 3, "能量为 0 → Codex 触发（1 + 2 张）");

            // 第 3 回合 4 能：花 2 还剩 2 → Codex 不触发
            var ctx2 = BattlePool(new[] { tac }, new[] { Unit("X", 1, 1, 9) }, pool);
            ToP1Turn(ctx2, 3);
            int h1 = ctx2.Players[0].Hand.Count;
            CheckCode(RuleCore.PlayTactic(ctx2, 0, HandIdx(ctx2, 0, "T_Codex"), -1), RuleCodes.OK, "打得出去");
            Check(ctx2.Players[0].Energy, 2, "还剩 2 能");
            Check(ctx2.Players[0].Hand.Count, h1 - 1 + 1, "能量不为 0 → Codex **不**触发（只有 1 张）");
        }

        // ---- 真卡面：这三个机制在两个阵营的战术卡里确实用到了 ----
        {
            var pool = CardDatabase.Load();
            int oath = 0, codex = 0, rep = 0;
            foreach (var c in pool)
            {
                if (c == null || c.Type != "tactic") continue;
                var ops = EffectText.Parse(c.Desc, out _, out _);
                foreach (var op in ops)
                {
                    if (op.Verb == "repeat") rep++;
                    if (op.Cost > 0 && op.CostKind == "oath") oath++;
                    if (op.ConditionKind == EffectCondition.EnergyZero) codex++;
                }
            }
            Debug.Log(P + $"   阵营机制实测：Oath {oath} 条 · Codex {codex} 条 · repeat {rep} 条");
            CheckTrue(oath > 0, $"卡池里真的有 `Oath N:` 的战术卡（{oath} 条）");
            CheckTrue(codex > 0, $"卡池里真的有 `Codex:` 的战术卡（{codex} 条）");
            CheckTrue(rep > 0, $"卡池里真的有 `repeat` 的战术卡（{rep} 条）");
        }
    }

    /// <summary>
    /// **A5 批 4 的督军专有那一半**（2026-09-14）—— 两类，都是**开局长效**而不是「一条 op」：
    ///   · **天赋的裸名写法**：卡面印的是 `[Talent 图标] Talent: &lt;名&gt;`，而**数据管线把图标和
    ///     `Talent:` 前缀一起剥掉了**（照成品卡图逐张核过，铁律 7）⇒ 只看文本认不出来。
    ///     判据两道闸：① 这一段像名字（无冒号/无数字/1~4 个词/首词不是动词）
    ///     ② 本卡**确实有** `Talent` 关键词，或者这一项与整条 desc 一字不差。
    ///   · **开局上手**：`Start the game with &lt;卡名&gt; in hand.`
    ///
    /// ⚠️ **反面才是重点**：`Psychophage` 带 `Talent` 关键词、正文却是一句
    ///    `Rally: Gain +2 Attack, +2 Ranged Attack and +1 Health …` —— 没有闸的话
    ///    这句话会被当成天赋名**静默吃掉**（卡面看着有 Rally、实际一个字都不结算）。
    /// </summary>
    static void TestA5Batch4()
    {
        var pool = CardDatabase.Load();

        // ---- 裸名天赋：三种来路各钉一张 ----
        var aunva = CreatePool.FindByName(pool, "Aun'Va");
        CheckTrue(aunva != null, "卡池里有 `Aun'Va`");
        if (aunva != null)
            Check(aunva.TalentName, "Ethereal Supreme",
                  "★ 裸名天赋之一：名字**就是整条 desc**（卡面 = `[Talent] Talent: Ethereal Supreme`）");

        var azrael = CreatePool.FindByName(pool, "Azrael");
        CheckTrue(azrael != null, "卡池里有 `Azrael`");
        if (azrael != null)
            Check(azrael.TalentName, "Supreme Grand Master",
                  "★ 裸名天赋之二：名字在 desc 的**第二段**（第一段是 Agenda 的正文）");

        var abaddon = CreatePool.FindByName(pool, "Abaddon the Despoiler");
        CheckTrue(abaddon != null, "卡池里有 `Abaddon the Despoiler`");
        if (abaddon != null)
            Check(abaddon.TalentName, "Chosen of the Four",
                  "★ 裸名天赋之三：名字**只在 `keywords` 数组里**，"
                  + "而且它连 `Talent` 关键词都没有 —— 靠「这一项与整条 desc 一字不差」那道闸认出来");

        var preacher = CreatePool.FindByName(pool, "Preacher");
        CheckTrue(preacher != null, "卡池里有 `Preacher`");
        if (preacher != null)
            Check(preacher.TalentName, "Hymn of Battle",
                  "★ 裸名天赋之四：名字在 `keywords` 里，靠「本卡有 `Talent` 关键词」那道闸认出来");

        // ⚠️ **反面**：带 `Talent` 关键词、正文却是普通句子 —— 不许被当成名字
        var phage = CreatePool.FindByName(pool, "Psychophage");
        CheckTrue(phage != null, "卡池里有 `Psychophage`");
        if (phage != null)
            CheckTrue(phage.TalentName == null,
                      "★ 反例：`Psychophage` 的 `Talent` 后面**没有名字** —— "
                      + "它那句 `Rally: Gain +2 Attack…` **没有被误当成天赋名**"
                      + $"（实得 {phage.TalentName ?? "null"}）");

        // ---- 开局上手：解析 + 开局真发到手 ----
        var logan = CreatePool.FindByName(pool, "Logan Grimnar");
        CheckTrue(logan != null && logan.StartWithInHand.Contains("Tyrnak and Fenrir"),
                  "★ `Logan Grimnar` → `StartWithInHand` 里有 `Tyrnak and Fenrir`");
        var hexcorn = CreatePool.FindByName(pool, "Sylar Hexcorn");
        CheckTrue(hexcorn != null && hexcorn.StartWithInHand.Contains("Abaddon's Chosen"),
                  "★ `Sylar Hexcorn` → `Abaddon's Chosen`"
                  + "（卡池里那张写作 `Abaddons Chosen`、**没有撇号** —— 靠 `CreatePool.Norm` 才配得上）");
        {
            var tyrnak = CreatePool.FindByName(pool, "Tyrnak and Fenrir");
            CheckTrue(logan != null && tyrnak != null, "两张卡都在池子里");
            var deck0 = new List<CardDef> { logan, tyrnak };          // 督军在最前，另一张进牌库
            var deck1 = new List<CardDef> { HeroOf("FixtureWarlord", "Goff", 2, 30) };
            var ctx = RuleCore.NewBattle(deck0, deck1, seed: 0, shuffle: false, cardPool: pool);
            CheckTrue(HasRef(ctx.Players[0].Hand, tyrnak),
                      "★ 开局那一刻它**已经在手牌里**了");
            CheckTrue(!HasRef(ctx.Players[0].Deck, tyrnak),
                      "★ ……而且**不在牌库里**了：是同一张卡**从牌库拿过来**的，不是凭空多一张");
        }
        // ---- 静态改战斗规则（5 句里的 4 句；第 5 句是 op，见本节末尾）----
        // 先钉**真卡识别得到**，再用**干净的夹具**验行为（真卡身上还有别的关键词，
        // 拿它们做行为夹具会把两件事搅在一起 —— 例：`Vargard Obyron` 自己要是带 Vanguard，
        // 「打督军」这一步会先被先锋规则挡掉，量到的就不是替身了）。
        {
            var scarab = CreatePool.FindByName(pool, "Scarab Swarm");
            CheckTrue(scarab != null && scarab.MeleeEqualsHealth,
                      "★ `Scarab Swarm` 认得出「近战 = 生命」");
            var obyon = CreatePool.FindByName(pool, "Vargard Obyron");
            CheckTrue(obyon != null && obyon.Bodyguard, "★ `Vargard Obyron` 认得出「替身」");
            var wraith = CreatePool.FindByName(pool, "Canoptek Wraith");
            CheckTrue(wraith != null && wraith.IgnoresVanguard, "★ `Canoptek Wraith` 认得出「无视先锋」");
            var patriarch = CreatePool.FindByName(pool, "Patriarch");
            CheckTrue(patriarch != null && patriarch.CostPerEnemyHandCard == 1,
                      "★ `Patriarch` 认得出「对手每张手牌便宜 1 费」");
        }

        // ① `This troop's Melee is always equal to its Health` —— **现算**，且**只换基础值**
        {
            var f = new CardDef("FixtureMeleeHealth", "FixtureMeleeHealth", "unit",
                                "This troop's Melee is always equal to its Health",
                                "common", "Test", 1, 2, 5, 3, null, subtype: "Infantry");
            CheckTrue(f.MeleeEqualsHealth, "夹具卡识得出这条规则");
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            var u = Place(ctx, 0, 0, f, exhausted: false);
            Check(RuleCore.FieldAttack(ctx, 0, u, false), 5, "★ 近战 = 生命 5（卡面写的是 2）");
            u.Health = 3;
            Check(RuleCore.FieldAttack(ctx, 0, u, false), 3, "★ 生命掉到 3 ⇒ 近战跟着变 3（**每次现算**）");
            Check(RuleCore.FieldAttack(ctx, 0, u, true), 3,
                  "……**远程不受影响**（卡面写的是 `Melee`，远程照旧用卡面的 3）");
        }

        // ② `Any attack against your Warlord targets this troop instead.` —— 替身
        {
            var guard = new CardDef("FixtureBodyguard", "FixtureBodyguard", "unit",
                                    "Any attack against your Warlord targets this troop instead.",
                                    "common", "Test", 1, 1, 9, 0, null, subtype: "Infantry");
            CheckTrue(guard.Bodyguard, "夹具卡识得出「替身」");
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 4);
            Place(ctx, 0, 0, Unit("FAtk", 1, 3, 9), exhausted: false);
            var body = Place(ctx, 1, 1, guard, exhausted: true);
            var lord = ctx.Players[1].Warlord;
            int bodyHp = body.Health, lordHp = lord.Health;
            CheckCode(RuleCore.DeclareAttack(ctx, 0, 0, 1, BoardSpec.WarlordSlot), RuleCodes.OK,
                      "声明攻击**打督军格**");
            CheckTrue(body.Health < bodyHp,
                      $"★ 伤害落到**替身**身上了（{bodyHp} → {body.Health}）");
            Check(lord.Health, lordHp, "★ ……而**督军一点没掉** —— 这就是「替身」的意思" + LogTail(ctx));
        }

        // ③ `This troop can ignore enemy units with Vanguard when attacking` —— 无视先锋
        {
            var f = new CardDef("FixtureIgnoresVanguard", "FixtureIgnoresVanguard", "unit",
                                "This troop can ignore enemy units with Vanguard when attacking",
                                "common", "Test", 1, 1, 9, 0, null, subtype: "Infantry");
            CheckTrue(f.IgnoresVanguard, "夹具卡识得出「无视先锋」");
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 4);
            Place(ctx, 0, 0, Unit("FPlain", 1, 1, 9), exhausted: false);
            Place(ctx, 0, 1, f, exhausted: false);
            Place(ctx, 1, 0, Unit("FVan", 1, 1, 9, "Vanguard"), exhausted: true);
            Place(ctx, 1, 1, Unit("FOther", 1, 1, 9), exhausted: true);
            CheckCode(RuleCore.IsValidTarget(ctx, 0, 0, 1, 1, false), RuleCodes.ErrTarget,
                      "普通攻击者打**非先锋**目标：被先锋规则挡住（反面，先证尺子有效）");
            CheckCode(RuleCore.IsValidTarget(ctx, 0, 1, 1, 1, false), RuleCodes.OK,
                      "★ 带这条规则的单位**可以**打非先锋目标（同一条判据，只差一个标记）");
            CheckCode(RuleCore.IsValidTarget(ctx, 0, 1, 1, 0, false), RuleCodes.OK,
                      "……打先锋目标当然也可以（没被反过来限制）");
        }

        // ④ `Costs 1 less for each card in enemy hand` —— **现算**，不登记 `CostMods`
        {
            var f = new CardDef("FixtureEnemyHandCost", "FixtureEnemyHandCost", "unit",
                                "Costs 1 less for each card in enemy hand",
                                "common", "Test", 5, 1, 5, 0, null, subtype: "Infantry");
            Check(f.CostPerEnemyHandCard, 1, "夹具卡识得出「对手每张手牌便宜 1 费」");
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 3);
            int c0 = RuleCore.CostOf(ctx, 0, f);
            Give(ctx, 1, Unit("Fx1", 1, 0, 1));
            Give(ctx, 1, Unit("Fx2", 1, 0, 1));
            Check(RuleCore.CostOf(ctx, 0, f), c0 - 2,
                  $"★ 对手手牌 +2 ⇒ 它便宜 2（{c0} → {c0 - 2}）");
            Check(ctx.CostMods.Count, 0,
                  "★ ……而且**一条 `CostMod` 都没登记** —— 对手手牌一直在变，"
                  + "登记成快照当场就错了（这条判据必须每次现算）");
            ctx.Players[1].Hand.Clear();
            Check(RuleCore.CostOf(ctx, 0, f), f.Cost,
                  $"……对手手牌清空后回到卡面原价 {f.Cost}（**现算**的第二个证据 —— "
                  + "登记成快照的话这里不会回去）");
        }

        // ⑤ `This troop attacks it`（`Hunta Rig` 的 Rally 尾句）—— 这是**一条 op**，不是静态规则
        {
            var r = EffectText.ParseSegment("This troop attacks it");
            Check(r.Kind, EffectText.SegKind.Ok, "`This troop attacks it` 认得出");
            Check(r.Ops[0].Verb, "forceattack", "动词 = forceattack");
            CheckTrue(r.Ops[0].Target == null && r.Ops[0].Target2 != null,
                      "攻击者是**本卡自己**（`Target` 留空）—— 打谁在 `Target2`");
        }

        // ---- 全会话最后扫出来的三条漏网（都不在原来那 5+5 的清单上）----
        {
            // ① `deal N damage` **没写目标**时接不了 ` and <动词>` 尾巴 —— `Nephilim Jetfighter` 的条件句
            var r = EffectText.ParseSegment("deal 5 damage and gain 1 Armour");
            Check(r.Kind, EffectText.SegKind.Ok, "`deal 5 damage and gain 1 Armour` 认得出");
            Check(r.Ops.Count, 2, "切成**两条**（deal + gain）—— 修之前整句失配（`ReDeal` 锚了 `$`）");
            Check(r.Ops[0].Verb, "deal", "① deal");
            Check(r.Ops[0].Amount, 5, "……打 5 点");
            CheckTrue(r.Ops[0].Target != null && r.Ops[0].Target.Auto,
                      "……**没写目标** ⇒ 走「自动选敌方最弱单位」那一支（**不是**「不知道打谁」）");
            // 🆕 2026-09-18：那一支现在**真的**按「生命最低」挑了（原版 `BattleManager.GetLowestHealthUnit`）。
            //    改之前 `Auto` 只是个「不问玩家」的标记，实际落到 `ResolveTargets` 的退化支 = **槽号最小**。
            CheckTrue(r.Ops[0].Target != null && r.Ops[0].Target.PickMost == "-health",
                      "……挑法是**生命最低**（原版 `TargetsAffected.lowestHealth = 240`），不是槽号最小");
            Check(r.Ops[1].Verb, "gain", "② gain（原来被切掉的那条尾巴）");

            var ri = EffectText.ParseSegment("If it has Flying, deal 5 damage and gain 1 Armour instead");
            Check(ri.Kind, EffectText.SegKind.Ok, "带 `If … instead` 的整句也认得出");
            int insteadCount = 0;
            foreach (var o in ri.Ops) if (o.Instead) insteadCount++;
            Check(insteadCount, ri.Ops.Count,
                  "★ 两条 op **都**带 `Instead` —— 卡面写的是「**替换**」前面的 3 点，不是「追加」");

            // ② `[Faith Icon]`（`Paragon Warsuit`）—— 方括号在 `ParseSegment` 开头就被剥掉了，
            //    所以「货币词」那一支必须**直接认 `faith icon` 这两个词**
            var rf = EffectText.ParseSegment("6 [Faith Icon]: Gain Vanguard");
            Check(rf.Kind, EffectText.SegKind.Ok, "`6 [Faith Icon]: Gain Vanguard` 认得出");
            Check(rf.Ops[0].Cost, 6, "★ 付费 6");
            Check(rf.Ops[0].CostKind, "faith",
                  "★ 货币 = **faith**（修之前整条正则失配 ⇒ 前缀没剥、付费也没记上，"
                  + "句子退化成一条「凭空给 Vanguard」的假解析）");

            // ③ `Stratagems in your hand become A or B`（`Hrolf the Ironhowl`）
            var rb = EffectText.ParseSegment("Stratagems in your hand become a Hunting Wolf or Fenrisian Wolf");
            Check(rb.Kind, EffectText.SegKind.Ok, "`Stratagems … become A or B` 认得出");
            Check(rb.Ops[0].Verb, "become",
                  "★ 动词 = `become`（**不是** `gain`）—— 修之前它被 give 系 handler 从中间截走，"
                  + "产出一条**载荷是两个卡名的假 `gain`**（半懂、卡面打 `*`，但什么都不会做）");
            Check(rb.Ops[0].Payload, "hunting wolf|fenrisian wolf", "两个候选按 ` or ` 切开");

            // 真卡那句**句首还带一个 `⚡` 图标**（卡面把触发关键词印成图标）——
            // 不先剥掉的话 `^([a-z]+)\s*:` 那条触发前缀匹配看不到 `rally`，
            // 整句又会被 give 系 handler 从中间截走（这就是它一直报「半懂」的真因）。
            var rg = EffectText.ParseSegment("⚡ Rally: Stratagems in your hand become a Hunting Wolf or Fenrisian Wolf");
            Check(rg.Kind, EffectText.SegKind.Ok, "★ 句首带 `⚡` 的整句也认得出");
            Check(rg.Ops[0].Verb, "become", "……动词仍是 `become`（触发前缀剥干净了）");

            // ④ 收尾扫到的**同类静默失效**：`This card costs 1 less for each <X>` 的「谁」= **本卡自己**
            //    （`Unholy Smite` / `Fenrisian Wolfpack`）。
            var rt = EffectText.ParseSegment("This card costs 1 less for each Dark Pact on friendly units");
            Check(rt.Kind, EffectText.SegKind.Ok, "`This card costs 1 less for each …` 认得出");
            Check(rt.Ops[0].Verb, "lowercost", "动词 = lowercost");
            Check(rt.Ops[0].Payload, "this card",
                  "载荷 = `this card`（和 `a random X` 一样，是**语义**不是噪声 —— 下游按「本卡自己」处理）");

            var sm = Tactic("T_ThisCard", 3, "This card costs 1 less for each Dark Pact on friendly units");
            var ctxS = ProbeBattle(new[] { sm }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctxS, 3);
            ctxS.Players[0].Energy = 20;
            CheckCode(RuleCore.PlayTactic(ctxS, 0, HandIdx(ctxS, 0, "T_ThisCard"), -1), RuleCodes.OK,
                      "打得出去（0 个 Dark Pact ⇒ 不降价，但也不该报错）");
            bool complained = false;
            foreach (string e in ctxS.Events)
                if (e != null && e.Contains("降费找不到对象")) complained = true;
            CheckTrue(!complained,
                      "★ 结算时**不再**报「降费找不到对象」—— 修之前 `this card` 会一路落到 "
                      + "`FindByName(\"this card\")` 查不到、**静默没生效**" + LogTail(ctxS));

            // ⑤ 用户 2026-09-14 指正后照三层权威核出来的一条：**「单位」与「部队」是两个词**。
            //    规则书中文版 `:70-75`：**单位含督军与衍生物，部队不含督军**；
            //    作用于「部队」的效果**不能**影响督军，作用于「单位」的**可以**。
            //    参考实现 `rule_core.gd` 的 `_fe_count` 也是 `troop_only := s.contains("troop")`。
            //    ⚠️ 解析层原来把 `for each friendly unit` 归成 `troop` ⇒ **少数一个督军**、**不报错**。
            //    实测带 `for each` 的卡里 **14 张**用的是 `unit`、6 张用 `troop`。
            {
                var rUT = EffectText.ParseSegment("Deal 1 damage to a random enemy for each friendly unit in play");
                Check(rUT.Kind, EffectText.SegKind.Ok, "`for each friendly unit in play`（`Shock Troops`）认得出");
                CheckTrue((rUT.Ops[0].CountRef ?? "").Contains("|unit|"),
                          $"★ 计数口径 = **unit**（含督军）—— 实际 `{rUT.Ops[0].CountRef}`");
                CheckTrue(!(rUT.Ops[0].CountRef ?? "").Contains("|troop|"),
                          "★ ……**不是** troop（旧写法写成 troop，静默少算一个督军）");

                // 同一张卡里两个词并存的真实例子 —— 正是「要分清」的理由
                List<string> u2, p2;
                var ops2 = EffectText.Parse(
                    "Deal 1 damage to an enemy for each friendly unit. "
                    + "If target dies, give +1 Health to all friendly troops in hand", out u2, out p2);
                CheckTrue(ops2.Count > 0, "`Drag it Down` 整条 desc 解得出");
                CheckTrue(ops2.Count > 0 && (ops2[0].CountRef ?? "").Contains("|unit|"),
                          $"★ 它前半句是 `unit`（含督军）—— 实际 `{(ops2.Count > 0 ? ops2[0].CountRef : "—")}`");

                var r3 = EffectText.ParseSegment("Deal 1 damage to a random enemy for each friendly troop");
                CheckTrue((r3.Ops[0].CountRef ?? "").Contains("|troop|"),
                          $"★ 写 `troop` 的才是 troop（**排督军**）—— 实际 `{r3.Ops[0].CountRef}`");
            }
        }
    }

    /// <summary>
    /// **A5 批 3 的第 2~5 条**（第 1、6 条在 `TestLowerCost` 里）—— 四条都是「解析对了 ≠ 机制在跑」
    /// 那一类，所以每条都**解析 + 结算各钉一次**。
    ///
    /// 这一节的价值在**每条的反面**：
    ///   · 第 2 条不实现 `next` ⇒ 不是不生效，而是**降多了**（本回合所有符合条件的牌都便宜）；
    ///   · 第 5 条不排自己 ⇒ `Deffkopta` 自己也会打一下（它是 Deffkopta）。
    /// 两种错都**不报错**，只能靠断言抓。
    /// </summary>
    static void TestA5Batch3()
    {
        // ---- 第 2 条：`next <X> … costs N less / 0`（**8 句一族**，见 `EffectOp.NextOnly`）----
        // 三个子问题，缺一个都会静默：
        //   ① 时长写在 `costs` **前面**（`Your next Stratagem **this turn** costs 0`）——
        //      旧代码把 `this turn` 当成主语的一部分 ⇒ `MatchesKind("next troop this turn")` 查不到
        //      ⇒ **这一族 8 句里有 5 句一条都没生效**（2026-09-14 实测）。
        //   ② `next` 是**一次性** —— 不实现就变成「本回合所有符合条件的牌都便宜」。
        //   ③ `costs 0` 是「**变成 0 费**」，而旧判据 `CostSetTo > 0` 认不出 0。
        {
            var op = OneOp("Your next Stratagem this turn costs 0");
            Check(op.Verb, "lowercost", "`costs 0` 那句 → lowercost");
            Check(op.Payload, "stratagem", "载荷**只剩兵种词**（`this turn` 与 `next` 都被剥掉）");
            Check(op.Duration, "turn", "时长写在 `costs` **前面**也要认出来");
            CheckTrue(op.NextOnly, "`next` ⇒ `NextOnly`（用完即销）");
            Check(op.CostSetTo, 0, "`costs 0` ⇒ **变成 0 费**（不是「降 0 费」）");

            op = OneOp("Your next troop this turn costs 2 less");
            Check(op.Payload, "troop", "`costs N less` 那句的载荷同样只剩兵种词");
            Check(op.Duration, "turn", "……时长也认出来了");
            CheckTrue(op.NextOnly, "……也是用完即销");
            Check(op.CostSetTo, -1, "`costs N less` **不是**「设为 N 费」（`CostSetTo` 保持哨兵 -1）");

            op = OneOp("The next Vehicle you play this turn costs 2 less");
            Check(op.Payload, "vehicle", "`The next Vehicle **you play** this turn` 的载荷 = `vehicle`");
            CheckTrue(op.NextOnly, "……同样是用完即销");

            // 反向：**不带 `next` 的不能被标成一次性**
            op = OneOp("Your troops cost 1 less this turn");
            CheckTrue(!op.NextOnly, "`Your troops cost 1 less` **不是**一次性（它管本回合全部）");
            Check(op.Payload, "troops", "……载荷照旧");
            op = OneOp("Your next Vehicle costs 2 less");
            CheckTrue(op.NextOnly, "`Your next Vehicle costs 2 less`（没写时长）也是一次性");
        }
        // ---- 第 2 条的**结算**：一次性修正被「打出的那一张」烧掉，别人不受影响 ----
        // ⚠️ 上面那组只证明**解析**对。「`next` 到底有没有被消费」只能在这里量 ——
        //    调用的位置也很要命：必须在**付费之后**（在 `CostOf` 那种查询里撤 = 看一眼就烧没了）。
        {
            var pool = CardDatabase.Load();
            var a = Tactic("T_OnceA", 0, "Draw a card");
            var b = Tactic("T_OnceB", 0, "Draw a card");
            var ctx = BattlePool(new[] { a, b }, new[] { Unit("X", 1, 1, 5) }, pool,
                                 warlordFaction: "Ultramarines");
            ToP1Turn(ctx, 3);
            ctx.Players[0].Energy = 20;
            ctx.CostMods.Add(new CostMod { Player = 0, Key = a.Id, Delta = -2, Once = true });

            Check(RuleCore.CostOf(ctx, 0, a), System.Math.Max(0, a.Cost - 2), "一次性修正先让 A 便宜 2");
            // ⚠️ **查询不许烧掉它** —— 这一条钉的就是「撤消费的位置」
            for (int i = 0; i < 3; i++) RuleCore.CostOf(ctx, 0, a);
            Check(ctx.CostMods.Count, 1, "★ 反复 `CostOf`（查询）**不会**烧掉一次性修正");

            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_OnceA"), -1), RuleCodes.OK, "打出 A");
            Check(ctx.CostMods.Count, 0, "★ A **打完**之后那条一次性修正才被撤掉");
            Check(RuleCore.CostOf(ctx, 0, b), b.Cost,
                  "B 还是原价 —— 一次性**没有蔓延成「本回合全部」**" + LogTail(ctx));
        }

        // ---- 第 3 条：`Choose and gain a bonus (A, B or C)`（`Carnifex`，全池唯一）----
        // 卡面把选项写在**括号里**、用 `,` 和 ` or ` 分隔；`TryChooseOne` 只认 `Choose one: A; B or C`。
        // ⇒ 归一成冒号写法（**不另写一套「选项表」文法**，三选一的判据继续只在一处）。
        {
            var r = EffectText.ParseSegment("Choose and gain a bonus (+2 Melee, +2 Ranged or Armour 1)");
            Check(r.Kind, EffectText.SegKind.Ok, "整句认得出（归一之后走已有的三选一）");
            Check(r.Ops.Count, 1, "归一出**一条** op");
            Check(r.Ops[0].Verb, "chooseone", "动词 = chooseone（不是新动词）");
            Check(r.Ops[0].Amount, 3, "★ 括号里是**三个**选项（`,` 与 ` or ` 各切一次）");
            Check(r.Ops[0].Payload, "gain +2 melee|gain +2 ranged|gain armour 1",
                  "三个选项各自归一成 `gain <载荷>`");
        }

        // ---- 第 4 条：`Each of your units deals N[-M] damage to <目标>`（`Sergeant Gadriel`）----
        // 主语是**己方场上全体单位**，数值写在卡面上（不是「按各自的关键词值」那支）。
        {
            var r = EffectText.ParseSegment("Each of your units deals 1-2 damage to a random enemy");
            Check(r.Kind, EffectText.SegKind.Ok, "`deals 1-2 damage` 那句认得出");
            Check(r.Ops[0].Verb, "eachunitdeal", "动词仍是 eachunitdeal（和关键词型同一个）");
            Check(r.Ops[0].Amount, 1, "下界 1");
            Check(r.Ops[0].AmountMax, 2, "上界 2");
            CheckTrue(r.Ops[0].Subject == null || r.Ops[0].Subject.IsEmpty,
                      "主语筛选为空 = **己方全体**（`Each of your units`）");

            var g = Tactic("T_Gadriel", 3, "Each of your units deals 1-2 damage to a random enemy");
            var ctx = ProbeBattle(new[] { g }, new[] { Unit("EFoe", 1, 0, 40) });
            ToP1Turn(ctx, 3);
            ctx.Players[0].Energy = 8;
            Place(ctx, 0, 0, Unit("FA", 1, 1, 3), exhausted: true);
            Place(ctx, 0, 1, Unit("FB", 1, 1, 3), exhausted: true);
            int before = SumHealth(ctx, 1);
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Gadriel"), -1), RuleCodes.OK,
                      "打出 `Each of your units deals 1-2 damage …`");
            int dealt = before - SumHealth(ctx, 1);
            // ⚠️ **督军也算「your units」** —— 实测开火者是 **3** 个（督军 + 我摆的 2 个）。
            //    ✅ **这是照规则书的正确行为**，不是引擎的将就：规则书中文版 `:70-75`
            //    「**单位（Units）**：任何有攻击与生命值的卡（**含督军与衍生物**）·
            //     **部队（Troops）**：仅部队卡，**不含督军**」——
            //    卡面写的是 `each of your **units**`，所以督军**该**开火。
            //    （同一段规则书还写着：作用于「部队」的效果不能影响督军，作用于「单位」的可以。）
            //    ⇒ 断言按**开火者个数**写成区间，别写死「2 个单位」。
            CheckTrue(dealt >= 3 && dealt <= 6,
                      $"★ 3 个开火者**各打 1-2** ⇒ 合计落在 [3,6]（**不是**固定 1、也不是每人都打同一个数）"
                      + $"—— 实际 {dealt}" + LogTail(ctx));
        }

        // ---- 第 5 条：`Other friendly <筛选> deal N damage to <目标>`（`Deffkopta`）----
        // 🔴 **两个坑都在这一条上**：① `Other` = 主语里要**排掉施放者自己**
        //    （`Deffkopta` 自己就是 Deffkopta）；② `friendly Deffkopta` 是**卡名**不是兵种
        //    （卡池里没有这个 subtype、原版也没有这个 trait），而且卡名**必须全等** ——
        //    `Mega Blasta Deffkopta` 名字里也有 `Deffkopta`。
        {
            var r = EffectText.ParseSegment("Other friendly Deffkopta deal 2 damage to a random enemy");
            Check(r.Kind, EffectText.SegKind.Ok, "`Other friendly …` 那句认得出");
            Check(r.Ops[0].Verb, "eachunitdeal", "动词 = eachunitdeal");
            Check(r.Ops[0].Amount, 2, "打 2 点（定值）");
            CheckTrue(r.Ops[0].OtherThanSelf, "★ `Other` ⇒ 要排掉施放者自己");
            CheckTrue(r.Ops[0].Subject != null && r.Ops[0].Subject.Name == CreatePool.Norm("Deffkopta"),
                      "★ 主语 = **卡名全等** `Deffkopta`（不是兵种词、也不是「包含」）");
            CheckTrue(r.Ops[0].Subject.KindWord == null, "……所以 `KindWord` 是空的（别两个都填）");

            var pool = CardDatabase.Load();
            var deff = CardDatabase.Find(pool, "Deffkopta", "Goff");
            CheckTrue(deff != null, "卡池里找得到真卡 `Deffkopta`（Goff）");
            if (deff != null)
            {
                var decoy = Unit("FNotDeffkopta", 1, 0, 3);          // 名字不含 Deffkopta ⇒ 不该打
                var ctx = ProbeBattle(new[] { deff }, new[] { Unit("EFoe", 1, 0, 60) });
                ToP1Turn(ctx, 4);
                ctx.Players[0].Energy = 20;
                Place(ctx, 0, 0, deff, exhausted: true);          // **别的**那只 Deffkopta
                Place(ctx, 0, 1, decoy, exhausted: true);         // 不是 Deffkopta
                int before = SumHealth(ctx, 1);
                CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "Deffkopta"), 2), RuleCodes.OK,
                          "部署**第二只** Deffkopta（触发 Rally）");
                int dealt = before - SumHealth(ctx, 1);
                Check(dealt, 2,
                      $"★ **只有「别的」那一只开火**（2 点）：施放者自己不算（`Other`）、"
                      + $"不是 Deffkopta 的那只也不算 —— 实际 {dealt} 点"
                      + "（漏了 `OtherThanSelf` 这里会是 4）" + LogTail(ctx));
            }
        }
    }

    /// <summary>
    /// `lowercost` —— **降费**（`Lower the cost of X by N` / `Lower its cost by N` /
    /// `They cost N less` / `Your troops cost N less`）。
    ///
    /// ⚠️ 这一段**存在的理由就是「payload 切错了也照样算成功」**：第一版把四条写法塞进一条大正则，
    /// 其中 `of?` 被当成「`of` 里的 f 可选」，于是 `Lower the cost of all Vehicles…` 的 payload
    /// 变成了 `f all vehicles…`（少了 `o`、多了 `f`），**而解析判定仍然是 Ok**。
    /// 断言**切出来的 payload 长什么样**才抓得住这种错 —— 只断言「认不认识」抓不住。
    /// </summary>
    static void TestLowerCost()
    {
        {
            var op = OneOp("Lower the cost of all Vehicles in your hand and deck by 1");
            Check(op.Verb, "lowercost", "`Lower the cost of …` → 动词 lowercost");
            Check(op.Payload, "all vehicles in your hand and deck", "payload 是**完整的**「谁」（不是切残的半截）");
            Check(op.Amount, 1, "降 1 费");

            op = OneOp("Lower the cost of all Beasts in your hand by 1");
            Check(op.Payload, "all beasts in your hand", "`in your hand` 的位置词也留在 payload 里");
            CheckTrue(op.Payload.StartsWith("all "), "**不能**丢掉开头的 `all`（丢了就筛错）");

            op = OneOp("Lower the cost of Tyrnak and Fenrir by 1");
            Check(op.Payload, "tyrnak and fenrir", "具名卡原样留着（两个名字用 and 连）");

            op = OneOp("Lower its cost by 2");
            Check(op.Amount, 2, "`Lower its cost by 2` → 2 费");
            Check(op.Payload, "(指代上一张)", "`its` 指代上一张（不是当卡名去查）");

            op = OneOp("They cost 1 less");
            Check(op.Payload, "(指代上一张)", "`They cost 1 less` 同样指代上一张");
            Check(op.Duration, "", "没写时长 = 永久");

            op = OneOp("Your troops cost 1 less this turn");
            Check(op.Amount, 1, "主语型 → 1 费");
            Check(op.Payload, "troops", "主语进 payload");
            Check(op.Duration, "turn", "`this turn` → 本回合（到期要撤）");

            op = OneOp("Your Drones cost 1 less for the rest of this battle");
            Check(op.Duration, "", "⚠️ `for the rest of this battle` 是**永久**，不能当成「本回合」");
        }

        // ---- 🆕 2026-09-14 A5 批 3 第 6 条：`a random <兵种词>` = **随机挑一张** ----
        // 四张卡：`Sergeant Telion`（Codex）· `Mekboy Gazmek`（Mob）· `Battlewagon`（Mob）·
        // `Living Icon`（Strike，正文嵌在 `Your Warlord gains "…"` 里）。
        // 🔴 改之前：冠词 `a` 与 `random` **原样留在 payload 里** ⇒ 结算层的
        //    `IsKindWord("a random infantry")` 只剥首词、拿 `random infantry` 去查 ⇒ 查不到 ⇒
        //    **这四张在实战里一条都没生效**（2026-09-14 基线自检的实战日志报 CODEX ×3 / MOB ×1）。
        // ⚠️ **光剥冠词还不够** —— 那会变成「手牌里所有 Infantry 一起降」。所以断言**两件一起钉**：
        //    `Payload` 干净 **和** `PickOne` 为真。只钉前者会漏掉后一半（静默降错一批牌）。
        {
            var op = OneOp("Lower the cost of a random Infantry in your hand by 1");
            Check(op.Verb, "lowercost", "`a random Infantry` 那句仍是 lowercost");
            Check(op.Payload, "infantry in your hand", "payload 里的 `a random` 被剥掉（只剩兵种词 + 位置词）");
            CheckTrue(op.PickOne, "`a random …` ⇒ `PickOne` 为真（**随机挑一张**，不是全部）");
            Check(op.Amount, 1, "降 1 费");

            op = OneOp("Lower the cost of a random Vehicle in your hand by 3");
            Check(op.Payload, "vehicle in your hand", "`Vehicle` 那句同理");
            CheckTrue(op.PickOne, "……`PickOne` 也为真");

            // 短位置词 `in hand`（`Living Icon` 的写法，没有 `your`）
            op = OneOp("Lower the cost of a random troop in hand by 1");
            Check(op.Payload, "troop in hand", "`in hand`（短写法）也要剥成「兵种词 + 位置词」");
            CheckTrue(op.PickOne, "……`PickOne` 也为真");

            // ⚠️ 反向：**没写 `random` 的不能被误标成随机挑一张**
            op = OneOp("Lower the cost of all Vehicles in your hand and deck by 1");
            CheckTrue(!op.PickOne, "`all Vehicles` **不是** PickOne（它是全部一起降）");
            op = OneOp("Lower the cost of Beasts in your hand by 1");
            CheckTrue(!op.PickOne, "裸兵种词也不是 PickOne");
        }

        // ---- 🆕 结算：`PickOne` 真的只降**一张** ----
        // 只钉解析层不够 —— 「没生效」和「降错一批」是两种不同的错，这条钉的是后者。
        {
            var pool = CardDatabase.Load();
            var lower = Tactic("T_LowerPick", 1, "Lower the cost of a random Infantry in your hand by 2");
            var infs = new List<CardDef>();
            foreach (var c in pool)
                if (c.Faction == "Ultramarines" && c.IsUnit && c.Subtype == "Infantry") infs.Add(c);
            CheckTrue(infs.Count >= 3, $"挑得到 3 张以上步兵当尺子（实际 {infs.Count}）");

            var hand = infs.GetRange(0, 3);
            var ctx = BattlePool(new[] { lower, hand[0], hand[1], hand[2] },
                                 new[] { Unit("X", 1, 1, 5) }, pool, warlordFaction: "Ultramarines");
            ToP1Turn(ctx, 1);
            // 第 7 行第 3 步：费用修正钉在**那一份**上 ⇒ 尺子也拿实例
            var insts = new CardInstance[3];
            for (int i = 0; i < 3; i++) insts[i] = ctx.Players[0].Hand.Find(h => ReferenceEquals(h.Card, hand[i]));
            CheckTrue(insts[0] != null && insts[1] != null && insts[2] != null, "三张步兵都在手里（按实例取得到）");
            var before = new int[3];
            for (int i = 0; i < 3; i++) before[i] = RuleCore.CostOf(ctx, 0, insts[i]);
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_LowerPick"), -1), RuleCodes.OK,
                      "`a random Infantry` 打得出去");
            int lowered = 0;
            for (int i = 0; i < 3; i++)
                if (RuleCore.CostOf(ctx, 0, insts[i]) < before[i]) lowered++;
            Check(lowered, 1,
                  $"手牌里**恰好一张**步兵被降费（0 = 没生效、3 = 降成「全部」）—— 实际降了 {lowered} 张");
        }

        // ---- 🆕 2026-09-14 A5 批 3 第 1 条：`Backlash: Returns to your hand and costs 2 more this turn` ----
        // 全池**只 1 处**（`Makari the Grot`，Goff）。三件事一起验：
        //   ① 尾句 ` and costs …` 终于切得开（要把 `cost` / `costs` 加进 `IsVerbWord`）
        //   ② 反噬触发时单位**已经离场** ⇒ 回手得走**弃牌堆**那条路
        //   ③ 回手之后**它自己**贵 2 费，而且只是**本回合**
        // ⚠️ **②才是本节的重点**：`ResolveTargets` 的 `Subjectless` 分支判 `source.IsAlive`，
        //    死人**不满足** ⇒ 一路落到「己方全体」兜底 ⇒ **把全场单位一起收进手牌**，
        //    而日志上只看到「把 A、B、C 放回手牌」。所以下面**故意多放一个旁观单位**当探测器。
        {
            var op = OneOp("costs 2 more this turn");
            Check(op.Verb, "costmore", "`costs 2 more this turn` → 动词 costmore");
            Check(op.Amount, 2, "加 2 费");
            Check(op.Duration, "turn", "`this turn` → 本回合（到期要撤）");
            Check(op.Payload, EffectText.SelfCostMoreMarker, "载荷是「这张卡自己」的哨兵值");
            // ⚠️ 上面那句 `OneOp` 自带「`SegKind.Ok`」断言 —— 它同时钉住了
            //    「自加价**不需要**目标」（不然 `Finish` 判半懂 ⇒ 卡面打 `*`、进不了卡组）。

            var two = EffectText.ParseSegment("Returns to your hand and costs 2 more this turn");
            Check(two.Kind, EffectText.SegKind.Ok, "整句认了");
            Check(two.Ops.Count, 2, "切成**两条**（return + costmore）—— "
                  + "修之前 `cost` 不在 `IsVerbWord` 里，尾句切不开、整句不认");
        }
        {
            var foe = new CardDef("FixtureMakariFoe", "FixtureMakariFoe", "unit", "", "common", "Test",
                                  1, 3, 9, 0, null, subtype: "Infantry");
            var bystander = new CardDef("FixtureBystander", "FixtureBystander", "unit", "", "common", "Test",
                                        1, 1, 5, 0, null, subtype: "Infantry");
            var makari = new CardDef("FixtureMakari", "FixtureMakari", "unit",
                                     "Backlash: Returns to your hand and costs 2 more this turn",
                                     "common", "Test", 2, 1, 1, 0, null, subtype: "Infantry");
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 4);
            Place(ctx, 0, 0, foe, exhausted: false);            // P1 的攻击者（3 攻，打得死 1 血的 Makari）
            var m = Place(ctx, 1, 0, makari, exhausted: true);
            var by = Place(ctx, 1, 1, bystander, exhausted: true);

            int costBefore = RuleCore.CostOf(ctx, 1, makari);
            CheckCode(RuleCore.DeclareAttack(ctx, 0, 0, 1, 0), RuleCodes.OK, "P1 攻打 Makari（1 血，必死）");

            CheckTrue(m == null || !m.IsAlive, "Makari 死了 —— 反噬的触发条件（`Backlash` = 单位死亡时触发）");
            CheckTrue(by != null && by.IsAlive && ctx.Players[1].Board[1] == by,
                      "★ **旁观单位还在场上**：修之前 `Subjectless` 会因为施放者已死而落到「己方全体」兜底，"
                      + "把他也一起收回手牌（而且日志上看不出错）");
            CheckTrue(HasRef(ctx.Players[1].Hand, makari),
                      "Makari 的卡**从弃牌堆回到了手牌**（触发反噬时它已经不在棋盘上，场上捞不到）");
            // 第 7 行第 3 步：加价钉在**回手的那一份**上（`DoCostMore` 认「这张卡自己」）
            var makariBack = HandInst(ctx, 1, "FixtureMakari");
            CheckTrue(makariBack != null, "Makari 那一份在手牌里（按实例取得到）");
            Check(RuleCore.CostOf(ctx, 1, makariBack), costBefore + 2,
                  $"回手后**它自己**贵 2 费（{costBefore} → {costBefore + 2}）");

            PassTurn(ctx); PassTurn(ctx);                       // 推进到下一轮
            Check(RuleCore.CostOf(ctx, 1, makari), costBefore,
                  "★ 过了这个回合就恢复原价（`this turn` 到期撤掉，不是永久加价）");
        }

        // ---- 结算：打折真的生效，而且**只**打在该打的那类牌上 ----
        {
            var pool = CardDatabase.Load();
            var lower = Tactic("T_Lower", 1, "Lower the cost of all Vehicles in your hand by 2");
            var veh = null as CardDef;
            var inf = null as CardDef;
            foreach (var c in pool)
            {
                if (c.Faction != "Ultramarines") continue;
                if (veh == null && c.IsUnit && c.Subtype == "Vehicle") veh = c;
                if (inf == null && c.IsUnit && c.Subtype == "Infantry") inf = c;
            }
            CheckTrue(veh != null && inf != null, "挑得到一张载具和一张步兵当尺子");

            var ctx = BattlePool(new[] { lower, veh, inf }, new[] { Unit("X", 1, 1, 5) }, pool,
                                 warlordFaction: "Ultramarines");
            ToP1Turn(ctx, 1);                       // 1 费，打得起
            var vehInst = HandInst(ctx, 0, veh.Name);      // 第 7 行第 3 步：尺子拿实例
            var infInst = HandInst(ctx, 0, inf.Name);
            CheckTrue(vehInst != null && infInst != null, "两张尺子都在手里（按实例取得到）");
            int vehBefore = RuleCore.CostOf(ctx, 0, vehInst);
            int infBefore = RuleCore.CostOf(ctx, 0, infInst);
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Lower"), -1), RuleCodes.OK,
                      "`Lower the cost of all Vehicles in your hand by 2` 打得出去");
            Check(RuleCore.CostOf(ctx, 0, vehInst), vehBefore - 2, $"载具真的便宜了 2（{vehBefore} → {vehBefore - 2}）");
            Check(RuleCore.CostOf(ctx, 0, infInst), infBefore, "步兵**没有**被误伤（只降载具）");
            Check(RuleCore.CostOf(ctx, 1, veh), vehBefore, "对手那边不受影响");
            // 卡面印的费用**不能**被改（`CardDef` 是共享对象，改它会污染整个卡池）
            Check(veh.Cost, vehBefore, "`CardDef.Cost` 本身没被动过（费用修正挂在 ctx 上）");
        }

        // ---- 永久 vs 本回合：`this turn` 的到期要撤掉 ----
        {
            var pool = CardDatabase.Load();
            var lower = Tactic("T_LowerTurn", 1, "Your troops cost 1 less this turn");
            var inf = null as CardDef;
            foreach (var c in pool)
                if (c.Faction == "Ultramarines" && c.IsUnit && c.Subtype == "Infantry") { inf = c; break; }

            var ctx = BattlePool(new[] { lower, inf }, new[] { Unit("X", 1, 1, 5) }, pool,
                                 warlordFaction: "Ultramarines");
            ToP1Turn(ctx, 1);
            var infT = HandInst(ctx, 0, inf.Name);        // 第 7 行第 3 步：尺子拿实例
            CheckTrue(infT != null, "那张步兵在手里");
            int before = RuleCore.CostOf(ctx, 0, infT);
            RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_LowerTurn"), -1);
            Check(RuleCore.CostOf(ctx, 0, infT), System.Math.Max(0, before - 1), "本回合内确实便宜了 1");
            PassTurn(ctx);                          // 换边 → 下一个回合开始，限时修正到期
            Check(RuleCore.CostOf(ctx, 0, inf), before, "回合结束后恢复原价（`this turn` 到期撤掉）");
        }

        // ---- 🆕 2026-09-13 第三十三轮：**同名卡不再一起降价**（发稳定 id 修掉的那条）----
        // 原版有 5 组跨阵营同名卡（`Bladeguard Veteran` = DarkAngels / Ultramarines …）。
        // 改之前 `CostMod.Key` 存的是**归一化卡名**，所以给一张降费会**连另一阵营那张一起降** ——
        // 而且**不报错**。现在 `Key` 是 `CardDef.Id`，两张各自独立。这条钉住它，别再退回卡名。
        {
            var pool = CardDatabase.Load();
            var da = CardDatabase.Find(pool, "Bladeguard Veteran", "DarkAngels");
            var um = CardDatabase.Find(pool, "Bladeguard Veteran", "Ultramarines");
            CheckTrue(da != null && um != null && da.Id != um.Id,
                      $"同名卡两张都在池子里、且 **id 不同**：{da?.Id}（DarkAngels） / {um?.Id}（Ultramarines）");

            var ctxTwin = BattlePool(new CardDef[0], new[] { Unit("X", 1, 1, 5) }, pool,
                                     warlordFaction: "Ultramarines");
            ToP1Turn(ctxTwin, 1);
            ctxTwin.CostMods.Add(new CostMod { Player = 0, Key = da.Id, Delta = -2 });
            Check(RuleCore.CostOf(ctxTwin, 0, da), System.Math.Max(0, da.Cost - 2),
                  $"按 id 登记降费：**这一张**真的降了 2（{da.Cost} → {System.Math.Max(0, da.Cost - 2)}）");
            Check(RuleCore.CostOf(ctxTwin, 0, um), um.Cost,
                  $"……**另一阵营的同名卡没被误伤**（还是 {um.Cost}）"
                  + "—— 按卡名匹配的时代这里会一起降，而且不报错");
        }
    }

    /// <summary>
    /// 选牌（`Choose a &lt;筛选&gt; [from/in &lt;来源&gt;] [and &lt;动词&gt;]`）——
    /// 权威源 `rule_core.gd:1157 _resolve_choose` + `:925` 候选匹配 + `:1041 _chosen_apply`。
    /// 数据与出处见 `资料/选牌Choose_数据与设计.md`。三层都要验：
    ///
    ///   ① **解析**：实测那 30 条句的「来源 / 筛选 / 动作」逐条钉死。
    ///      ⚠️ 这一族的剥壳**很容易切残而照样算成功**（`lowercost` 就踩过：payload 被切成
    ///      `f all vehicles…` 而判定照样 Ok）—— 所以断言的是**切出来长什么样**，不是「认不认识」。
    ///   ② **负面**：`Choose an effect …`（Leviathan 2 张）必须判**不认识**。
    ///      原版在这里会默认成 `to_hand`（`rule_core.gd:1193`），那是**静默的错误语义**。
    ///   ③ **结算**：候选域的几个来源各跑一遍，重点是**跨句指代** ——
    ///      这一族的动作常写在**下一句**里（`Choose a … . It costs 2 less`），
    ///      靠的正是选牌时把选中的卡写进引用位。
    /// </summary>
    static void TestChooseCard()
    {
        // ---- ① 解析：30 条实测句逐条钉死 ----
        // 格式：句子 | 来源 | 筛选 | 动作 | 死亡窗口 | 复制张数
        var rows = new[]
        {
            "Choose a 2-cost Leviathan troop and deploy it|pool|2-cost leviathan troop|deploy||0",
            "Choose a Dark Angels Secret and add it to your deck|pool|dark angels secret|todeck||0",
            "Choose a Drone and add it to your hand|pool|drone|hand||0",
            "Choose a Genomic Enhancement and put it in your hand|pool|genomic enhancement|hand||0",
            "Choose a Rune and put it in your hand|pool|rune|hand||0",
            "Choose a Sabotage and add it to the enemy hand|pool|sabotage|enemyhand||0",
            "Choose a Sabotage card and add it to your opponent's hand|pool|sabotage card|enemyhand||0",
            "Choose a Sautekh Stratagem and put it in your hand|pool|sautekh stratagem|hand||0",
            "Choose a Stratagem from your deck and draw it|deck|stratagem|draw||0",
            "Choose a card from your deck and draw it|deck|card|draw||0",
            "Choose a card from your deck and put it at the top of your deck|deck|card|decktop||0",
            "Choose a card in your hand and return it to your deck|hand|card|return||0",
            // ⚠️ 这一条**动作是空的**（卡面就到这里，后一句 `When played, gain 3` 是另一件事）。
            //    原版会把它默认成 `to_hand` —— 那是错的（挑的是**对手手里**的牌）
            "Choose a card in your opponent's hand|enemyhand|card|||0",
            "Choose a friendly Infantry that died this game and return it to your deck|dead|friendly infantry|return|all|0",
            "Choose a friendly troop that died since your last turn and deploy it|dead|friendly troop|deploy|since_last_turn|0",
            "Choose a friendly troop that died this battle and deploy it|dead|friendly troop|deploy|all|0",
            "Choose a friendly troop that died this game and put it in your hand|dead|friendly troop|hand|all|0",
            "Choose a non-Legendary Genestealer Cults troop and add it to your hand|pool|non-legendary genestealer cults troop|hand||0",
            "Choose a non-Legendary Ultramarines card and create two copies in your hand|pool|non-legendary ultramarines card|copies||2",
            // ⚠️ 这三条的**动作在下一句**（`Draw it and create a copy …` / `Lower its cost by 2`），
            //    所以 ChooseAct **必须是空串** —— 判成 `hand` 之类的默认值就是静默错语义
            "Choose a troop from your deck|deck|troop|||0",
            "Choose a troop in your deck|deck|troop|||0",
            "Choose a troop in your hand|hand|troop|||0",
            "Choose a troop from your deck and draw it|deck|troop|draw||0",
            "Choose an Astra Militarum Stratagem and add it to your hand|pool|astra militarum stratagem|hand||0",
            "Choose an Astra Militarum Vehicle and put it in your hand|pool|astra militarum vehicle|hand||0",
            "Choose an Invocation and put it in your hand|pool|invocation|hand||0",
            "Choose an Overlord Power and put it in your hand|pool|overlord power|hand||0",
            "Choose an Ultramarines Psychic Power and put it in your hand|pool|ultramarines psychic power|hand||0",
        };
        foreach (string row in rows)
        {
            var f = row.Split('|');
            var op = OneOp(f[0]);
            Check(op.Verb, "choosecard", $"「{f[0]}」→ 动词 choosecard");
            Check(op.ChooseSrc, f[1], $"「{f[0]}」来源");
            Check(op.ChooseWhat, f[2], $"「{f[0]}」筛选字数切出来的**原文**");
            Check(op.ChooseAct, f[3], $"「{f[0]}」动作");
            Check(op.ChooseDeadScope, f[4], $"「{f[0]}」死亡窗口");
            Check(op.ChooseCopies, int.Parse(f[5]), $"「{f[0]}」复制张数");
        }

        // ---- ② 选「效果」**不是选牌** —— 必须走 `chooseeffect`，不许被本 handler 顺手吃掉 ----
        //  🔴 **2026-09-14 T3 更正**：这一段原来钉的是「必须判**不认识**」
        //     （理由：候选效果池不在任何文本里，三处零命中）。**用户当天把池子给了** ⇒
        //     机制落在 `EffectText.TryChooseEffect` + `RuleCore.ChooseEffectPools`。
        //     ⚠️ 本 handler 里那行 `what == "effect" ⇒ return false` **仍然不许删** ——
        //        它正是「让给那个 handler」的那一步；删了这两句会被当成**选牌**处理。
        //     详细断言（含结算层）在 `TestChooseEffect`。
        foreach (string s in new[] { "Choose an effect and give it to a friendly troop",
                                     "Choose an effect and give it to all troops in your hand" })
        {
            var seg = EffectText.ParseSegment(s);
            Check(seg.Kind, EffectText.SegKind.Ok, $"「{s}」选效果认得出来（2026-09-14 T3）");
            CheckTrue(seg.Ops != null && seg.Ops.Count == 1 && seg.Ops[0].Verb == "chooseeffect",
                      $"★ 「{s}」走的是 **`chooseeffect`**，**不是** `choosecard`"
                      + "（两者都以 `choose` 开头，靠 `TryChooseCard` 里 `what == \"effect\"` 那行分家）");
        }

        // ---- ③a 结算：`pool` 来源 —— 凭空造一张符合筛选的进手牌 ----
        {
            var pool = CardDatabase.Load();
            var t = Tactic("T_ChooseDrone", 1, "Choose a Drone and add it to your hand");
            var ctx = BattlePool(new[] { t }, new[] { Unit("E", 1, 1, 5) }, pool);
            ToP1Turn(ctx, 1);
            int deckBefore = ctx.Players[0].Deck.Count;
            int handBefore = ctx.Players[0].Hand.Count;
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_ChooseDrone"), -1),
                      RuleCodes.OK, "`Choose a Drone and add it to your hand` 打得出去");
            // 打出的战术卡离手、挑来的那张进手 —— 张数持平
            Check(ctx.Players[0].Hand.Count, handBefore, "战术卡离手、挑来的进手（张数持平）");
            CheckTrue(HasSubtype(ctx.Players[0].Hand, "Drone"),
                      "手牌里多了一张 **Drone 兵种**的卡（不是随便一张）");
            Check(ctx.Players[0].Deck.Count, deckBefore,
                  "牌库张数没变（`pool` 来源是**凭空造**，不动牌库）");
        }

        // ---- ③b 结算：`dead` 来源 —— 把阵亡的部队捞回场上 ----
        {
            var pool = CardDatabase.Load();
            // ⚠️ **阵亡者必须取自真实卡池**：选牌的筛选是拿「全卡池」当判据集的
            //    （`CreatePool.FilterChoose` 复用 `CreatePool.Resolve` 的兵种/阵营判定），
            //    夹具里 `new` 出来的假卡**不在池子里，筛不出来** —— 这是判据只有一份的代价。
            CardDef fallen = null;
            foreach (var c in pool)
            {
                if (c.Faction != "Ultramarines" || !c.IsUnit || c.Subtype != "Infantry") continue;
                if (c.Health > 3) continue;
                fallen = c; break;
            }
            CheckTrue(fallen != null, "挑得到一个真实部队当阵亡者");

            var t = Tactic("T_ChooseDead", 1, "Choose a friendly troop that died since your last turn and deploy it");
            var ctx = BattlePool(new[] { t }, new[] { Unit("Killer", 1, 9, 9) }, pool);
            ToP1Turn(ctx, 3);

            Place(ctx, 0, 1, fallen);                              // 自己的小单位
            Place(ctx, 1, 1, Unit("FixtureBrute", 1, 9, 40));      // 对面的 9/9（血厚，别被反杀）
            CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 1), RuleCodes.OK,
                      "自己的部队撞上去（会被反击打死）");
            Check(Board(ctx, 0, 1), null, "它**真的阵亡**了、格位空出来");
            Check(ctx.DeadUnits.Count, 1, "阵亡登记表里记着 1 条");
            Check(ctx.DeadUnits[0].Owner, 0, "记的是它的主人");
            Check(ctx.DeadUnits[0].Card.Name, fallen.Name, "记的是它那张卡");

            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_ChooseDead"), -1), RuleCodes.OK,
                      "`Choose a friendly troop that died … and deploy it` 打得出去");
            CheckTrue(SlotOf(ctx, 0, fallen.Name) >= 0,
                      $"「{fallen.Name}」被选出来并**重新放到场上**了（槽 {SlotOf(ctx, 0, fallen.Name)}）");
            Check(ctx.DeadUnits.Count, 0,
                  "取走后登记表**空了**（`TakeFromGraveyard` 要同时清 `Discard` 和 `DeadUnits`）");
        }

        // ---- ③c 结算：**跨句指代** —— 动作写在下一句，靠引用位接通 ----
        // 这条是本轮真正的机制：`Choose a Drone and add it to your hand. It costs 2 less`
        // 的 `It` 指的是**刚挑中那张**，走 `ctx.LastCreated` 的 `(指代上一张)` 那条老路。
        {
            var pool = CardDatabase.Load();
            var t = Tactic("T_ChooseCheap", 1,
                           "Choose a Drone and add it to your hand. It costs 2 less");
            var ctx = BattlePool(new[] { t }, new[] { Unit("E", 1, 1, 5) }, pool);
            ToP1Turn(ctx, 1);
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_ChooseCheap"), -1),
                      RuleCodes.OK, "带后续句的选牌卡打得出去");
            // 第 7 行第 3 步：后续句的减费钉在**刚挑中那一份**上 ⇒ 尺子拿实例
            CardInstance got = null;
            foreach (var h in ctx.Players[0].Hand) if (h.Card.Subtype == "Drone") { got = h; break; }
            CheckTrue(got != null, "挑到的是 Drone");
            if (got != null)
                Check(RuleCore.CostOf(ctx, 0, got), System.Math.Max(0, got.Card.Cost - 2),
                      $"后续句 `It costs 2 less` **作用在刚挑中那张上**（{got.Card.Cost} → {got.Card.Cost - 2}）");
        }

        // ---- ③d `since your last turn` 的**窗口**：上个回合之前死的不算 ----
        {
            var pool = CardDatabase.Load();
            // ⚠️ 用**真实卡池**里的部队当「很久以前死的」那条 —— 用假卡的话，
            //    它本来就会被兵种筛选（拿全池当判据集）筛掉，**这条断言就空转了**。
            CardDef ancient = null;
            foreach (var c in pool)
                if (c.Faction == "Ultramarines" && c.IsUnit && c.Subtype == "Infantry") { ancient = c; break; }
            CheckTrue(ancient != null, "挑得到一个真实部队当「很久以前死的」那条");

            var t = Tactic("T_ChooseDeadWin", 1, "Choose a friendly troop that died since your last turn and deploy it");
            var ctx = BattlePool(new[] { t }, new[] { Unit("E", 1, 1, 5) }, pool);
            ToP1Turn(ctx, 3);
            // 手工登记一条「很久以前死的」—— 死亡回合早于本方最近一次回合开始
            ctx.DeadUnits.Add(new DeadUnit
            {
                Card = ancient,
                Owner = 0,
                DeathTurn = ctx.Players[0].LastTurnStartMark - 1,
            });
            Check(ctx.DeadUnits.Count, 1, "窗口外那条先放着");
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_ChooseDeadWin"), -1),
                      RuleCodes.OK, "窗口外没候选时**卡照样能打**（这是规则书允许的空候选）");
            Check(ctx.DeadUnits.Count, 1,
                  "那条**没被取走**（它在窗口外 = 不是合法候选，不许凑合拿走）");
            Check(SlotOf(ctx, 0, ancient.Name), -1, "它**没有**被放到场上");
        }
    }

    /// <summary>
    /// **常驻效果 / 手牌陷阱（回合起止触发）** —— 规则书英文版 `:39-41`「Persistent Effects」
    /// （中文版 `:32`）：写「For the rest of this battle」的卡**被弃置后依然生效**，
    /// 要单独放一摞备查 —— 也就是**卡本身就是效果来源**。
    ///
    /// 三层都验：
    ///   ① **解析** —— 3 条实测句逐条钉；另外两条**必须判不认识**（见下）；
    ///   ② **登记** —— 打出去进 `ctx.PersistentEffects`，且**只在打出者自己的回合**触发；
    ///   ③ **手牌陷阱** —— 躺在持有者手牌里，持有者回合结束才咬人；而且**不进自动牌组**。
    ///
    /// ⚠️ 两条**故意判不认识**的，是本轮「宁可报、也不装成已生效」的两处：
    ///   · `For the rest of this battle, give Shield to all Drones you deploy` ——
    ///     它是**部署时触发**，不是回合起止；注册一条永远不会被消费的效果 = 骗人
    ///   · `When you play a Stratagem, your Warlord takes 1 damage` ——
    ///     条件从句**不许当主语**，不然会变成「任何时候都掉血」的静默错语义
    /// </summary>
    static void TestPersistentEffects()
    {
        // ---- ① 解析 ----
        {
            var op = OneOp("For the rest of this battle, at the start of your turn, deploy a Shock Trooper");
            Check(op.Verb, "persist", "`For the rest of …` → 动词 persist（登记常驻效果）");
            Check(op.AtTurnPhase, "turn_start", "触发时机 = 回合开始");
            CheckTrue(op.AtTurnOps != null && op.AtTurnOps.Count == 1 && op.AtTurnOps[0].Verb == "deploy",
                      "正文解析出来了（1 条 deploy），**不是**只记了原文");
            // ⚠️ `Payload` 是**小写**的（`Dispatch` 拿到的就是 `low`）—— 和 `ChooseWhat` 一个口径。
            //    日志要打原样的话用 `op.Source`（那是**原文分句**，没改过大小写）。
            Check(op.Payload, "deploy a shock trooper", "正文原文留着（小写，日志要打人话时用 op.Source）");

            op = OneOp("For the rest of the match, at the end of your turn, draw a card");
            Check(op.AtTurnPhase, "turn_end", "`this battle` 与 `the match` 都认");

            var wr = EffectText.ParseSegment(
                "Your Warlord gains: \"At the start of your turn, create a random Combat Elixir in your hand\"");
            CheckTrue(wr.Ops != null && wr.Ops.Count == 1, "`Your Warlord gains: \"…\"` 解析得出 1 条");
            Check(wr.Ops[0].Verb, "persist", "它也是**常驻效果**（原版挂到督军身上，我们让那一方注册）");
            Check(wr.Ops[0].AtTurnPhase, "turn_start", "内层时机 = 回合开始");

            var trap = OneOp("At the end of your turn, your troops take 1 damage");
            Check(trap.Verb, "atturn", "`At the end of your turn, …` → 动词 atturn（手牌陷阱）");
            Check(trap.AtTurnPhase, "turn_end", "触发时机 = 回合结束");
            CheckTrue(trap.AtTurnOps != null && trap.AtTurnOps.Count == 1
                      && trap.AtTurnOps[0].Verb == "deal" && trap.AtTurnOps[0].Amount == 1,
                      "正文 = 打 1 点伤害");
            CheckTrue(trap.AtTurnOps[0].Target != null && trap.AtTurnOps[0].Target.Side == "own",
                      "打的是**自己**那边");
            Check(trap.AtTurnOps[0].Target.Count, 0,
                  "**全体**（`your troops **take**` 是复数动词 —— 单复数从动词看，不靠猜名词）");
        }
        // ⚠️ 上一条 `When you play a Stratagem, …` 必须**判不认识**：
        //    不挡的话 `(.+?)` 会把「什么时候」的从句吃成目标，然后在**任何**时候都结算。
        {
            Check(EffectText.ParseSegment("When you play a Stratagem, your Warlord takes 1 damage").Kind,
                  EffectText.SegKind.Unknown,
                  "带条件从句的 `X takes N damage` **判不认识**（不许把条件当主语）");
            // 但「主语写全」的那种要认 —— 单数动词 = 只打一个
            var one = OneOp("your Warlord takes 2 damage");
            Check(one.Amount, 2, "`your Warlord takes 2 damage` → 2 点");
            Check(one.Target.Count, 1, "单数动词 `takes` → **只打一个**");
        }
        // ---- ③ 同族但**别的触发点**的写法（2026-09-13 第三十轮做掉）----
        // 原来这三条**必须判不认识**（`EffectTargetSpec.Deployed` 还没有、`costmore` 也没实现）。
        // 现在真做了，断言改成**钉「解析出来长什么样」** —— 不只钉「认不认识」：
        // 第十六轮踩过，只钉认不认识会让静默错解析（payload 被切错）照样通过。
        {
            var op = OneOp("For the rest of this battle, give Shield to all Drones you deploy");
            Check(op.Verb, "persist", "`… give Shield to all Drones you deploy` → 登记常驻效果");
            Check(op.AtTurnPhase, "deploy", "触发点是**部署**（不是回合起止）");
            CheckTrue(op.Filter != null && op.Filter.KindWord == "drone", "筛的是 **Drone**");
            CheckTrue(op.AtTurnOps != null && op.AtTurnOps.Count == 1, "正文解析出 1 条");
            Check(op.AtTurnOps[0].Verb, "give", "正文动词 = give");
            Check(op.AtTurnOps[0].Payload, "shield", "载荷切出来是 **shield**（不是空、也不是带前缀的）");
            CheckTrue(op.AtTurnOps[0].Target != null && op.AtTurnOps[0].Target.Deployed,
                      "目标是「**刚部署的那个**」");

            var arm = OneOp("For the rest of the match, give Armour 1 to Vehicles you put in play");
            Check(arm.AtTurnPhase, "deploy", "`you put in play` 也是部署时");
            CheckTrue(arm.Filter != null && arm.Filter.KindWord == "vehicle", "筛的是 **Vehicle**");
            Check(arm.AtTurnOps[0].Payload, "armour 1", "载荷 = armour 1（**数值没被吃掉**）");

            var cm = OneOp("For the rest of this battle, Sabotage cards in the enemy hand cost 1 more");
            Check(cm.Verb, "costmore", "`… Sabotage cards in the enemy hand cost 1 more` → 持续改费");
            Check(cm.Amount, 1, "加 1 费");
            CheckTrue(cm.Target != null && cm.Target.Side == "enemy", "加的是**对手**手里那批");
            Check(cm.Target.Kind, "sabotage", "筛的是 sabotage");

            // ⚠️ **认不出的一律不许硬认** —— 这两条不能因为「看起来像」就被收下：
            //    · 条件从句当主语（老红线）
            //    · 兵种词表里没有的词
            Check(EffectText.ParseSegment("For the rest of this battle, give Shield to all Wizards you deploy").Kind,
                  EffectText.SegKind.Unknown, "兵种词表里没有的词 → 判不认识（不猜）");
            Check(EffectText.ParseSegment("For the rest of this battle, at the start of your turn, do the thing").Kind,
                  EffectText.SegKind.Unknown, "正文认不出的常驻效果 → 整句判不认识（绝不注册一条不会被消费的）");
        }

        // ---- ② 登记 + **只在打出者自己的回合**触发 ----
        {
            var pool = CardDatabase.Load();
            var t = Tactic("T_Persist", 1,
                           "For the rest of this battle, at the start of your turn, deploy a Shock Trooper");
            var ctx = BattlePool(new[] { t }, new[] { Unit("E", 1, 1, 5) }, pool,
                                 warlordFaction: "AstraMilitarum");
            ToP1Turn(ctx, 1);
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Persist"), -1), RuleCodes.OK,
                      "`For the rest of this battle, at the start of your turn, deploy a Shock Trooper` 打得出去");
            Check(ctx.PersistentEffects.Count, 1, "登记了 1 条常驻效果");
            Check(ctx.PersistentEffects[0].Owner, 0, "记的是**打出的那一方**");
            Check(ctx.PersistentEffects[0].Phase, "turn_start", "触发时机 = 回合开始");
            CheckTrue(ctx.PersistentEffects[0].Source != null
                      && ctx.PersistentEffects[0].Source.Name == "T_Persist",
                      "记下了来源卡（规则书要求这类卡留档备查）");

            PassTurn(ctx);                       // → P2 的回合开始
            Check(SlotOf(ctx, 0, "Shock Trooper"), -1,
                  "**对手**的回合开始**不触发** —— 常驻效果只属于打出它的那一方");
            PassTurn(ctx);                       // → P1 的回合开始
            CheckTrue(SlotOf(ctx, 0, "Shock Trooper") >= 0,
                      $"自己的回合开始**真的部署了** Shock Trooper（槽 {SlotOf(ctx, 0, "Shock Trooper")}）");
        }

        // ---- ③ 手牌陷阱：持有者回合结束才咬人，而且不进自动牌组 ----
        {
            var pool = CardDatabase.Load();
            CardDef trap = null;
            foreach (var c in pool) if (c.Name == "Poisoned Supplies") { trap = c; break; }
            CheckTrue(trap != null, "卡池里找得到 `Poisoned Supplies`");
            CheckTrue(EffectText.IsHandTrap(trap.Desc), "它被判成**手牌陷阱**");
            CheckTrue(!DeckBuilder.TacticPlayable(trap),
                      "**不进自动牌组** —— 陷阱卡是塞给对手的，自己牌组里放一张只会每回合坑自己");

            var ctx = BattlePool(new[] { Unit("F1", 1, 1, 1) }, new[] { Unit("E", 1, 1, 5) }, pool);
            ToP1Turn(ctx, 3);
            var victim = Place(ctx, 0, 1, Unit("FixtureTrapVictim", 1, 1, 9));
            Give(ctx, 0, trap);

            // 回合记账：`ToP1Turn` 之后**当前行动方是 P1**，所以第一次 `EndTurn` 结束的**就是 P1 的回合**。
            // 顺序：P1 回合末（**咬**）→ P2 回合末（不咬）→ P1 回合末（**咬**）
            RuleCore.EndTurn(ctx);               // P1 的回合结束（陷阱持有者）
            Check(victim.Health, 8, "**P1 手里**有陷阱 → P1 的回合结束，自己的部队掉 1 血");
            ctx.ClearSignals();
            RuleCore.BeginTurn(ctx);             // → P2 的回合

            RuleCore.EndTurn(ctx);               // P2 的回合结束 —— 陷阱不在 P2 手里
            Check(victim.Health, 8, "**P2 的回合结束不掉血**（陷阱在谁手里，谁的回合结束才生效）");

            RuleCore.BeginTurn(ctx);             // → P1 的回合
            RuleCore.EndTurn(ctx);               // P1 的回合结束：**再咬一次**
            Check(victim.Health, 7, "P1 的下一个回合结束**又咬一次**（常驻在手里，每回合都咬）");
        }
    }

    /// <summary>从卡池里按名字取一张（找不到返回 null）</summary>
    static CardDef PoolCard(IList<CardDef> pool, string name)
    {
        foreach (var c in pool) if (c.Name == name) return c;
        return null;
    }

    /// <summary>
    /// **单位卡 `desc` 里的触发式效果**（`Rally:` / `Strike:` / `Slay:` / `Backlash:` / `Penitence:`）
    /// —— 2026-09-13 第三十一轮。
    ///
    /// **修之前是什么样**：触发式正文只走 <see cref="EffectSpec"/> 那个**封闭文法**
    /// （只有 Damage/Heal/Draw），而原版卡面写的是 `Rally: Stun an enemy` ——
    /// 解析失败 ⇒ 卡面标 `*` ⇒ **打起来静默不动**。
    /// 实测 **89 张卡**在 `keywords` 和 `desc` 两处都登记了触发、正文却没人认。
    ///
    /// ⚠️ 断言**两层都要**：
    ///   ① 正文被解析出来了（`TriggerOps` 非空、动词/载荷切得对）
    ///   ② **它真的改变了局面** —— 只钉① 会漏掉「解析出来了但结算层没分支」，
    ///      那正是本工程反复强调的那类**静默失效**。
    /// </summary>
    static void TestUnitDescTriggers()
    {
        var pool = CardDatabase.Load();

        // ---- ① `Rally: Stun an enemy`（Howling Banshee，Aeldari）----
        {
            var banshee = PoolCard(pool, "Howling Banshee");
            CheckTrue(banshee != null, "卡池里有 `Howling Banshee`");
            if (banshee != null)
            {
                CheckTrue(banshee.TriggerOps(KeywordTable.Rally) != null,
                          "它的 `Rally:` 正文**被解析出来了**（原来走封闭文法，`Stun an enemy` 认不出）");
                Check(banshee.TriggerText(KeywordTable.Rally), "Stun an enemy", "正文原文留着（日志要用）");
                Check(banshee.TriggerOps(KeywordTable.Rally)[0].Verb, "stun", "解析成 `stun` 动词");

                var ctx = BattlePool(new[] { banshee }, new[] { Unit("EFoe", 1, 1, 9) }, pool,
                                     warlordFaction: "SaimHann");
                ToP1Turn(ctx, 4);
                var foe = Place(ctx, 1, 1, Unit("FixtureStunTarget", 1, 1, 9));
                CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "Howling Banshee"), 1), RuleCodes.OK,
                          "从手牌部署 `Howling Banshee`");
                CheckTrue(foe.IsStunned,
                          "**部署触发真的生效了**：对面那个单位被 Stun（只钉「解析出来了」抓不到这条）");
            }
        }

        // ---- ② `Strike: Return this troop to your hand`（Warp Spider）----
        // ⚠️ 这条专门盯**代词**：`this troop` 在解析器里归 `prev`（那是给战术卡的
        //    `Choose … Draw it` 用的），触发式正文里没有「上一句」——
        //    靠 `ResolveOps` 把**触发者自己**种进 `LastTarget`。不种的话它会去回手
        //    **上一张被指过的牌**，而场面上看不出哪里不对（静默错打）。
        {
            var spider = PoolCard(pool, "Warp Spider");
            CheckTrue(spider != null, "卡池里有 `Warp Spider`");
            if (spider != null)
            {
                var ctx = BattlePool(new[] { Unit("P1Filler", 1, 1, 1) }, new[] { Unit("EFoe", 1, 1, 9) }, pool,
                                     warlordFaction: "SaimHann");
                ToP1Turn(ctx, 4);
                // 直接摆上场（不走部署）—— 这一节要验的是 `Strike`，不是 `Rally`；
                // 而且刚部署的单位是疲劳的，打不了。
                var mine = Place(ctx, 0, 1, spider, exhausted: false);
                var foe = Place(ctx, 1, 1, Unit("FixtureStrikeTarget", 1, 1, 9));
                int handBefore = ctx.Players[0].Hand.Count;

                CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 1), RuleCodes.OK, "用 Warp Spider 攻击");
                CheckTrue(mine.Health > 0, "攻击者活下来了（`Strike` 的前提是本单位存活）");
                Check(SlotOf(ctx, 0, "Warp Spider"), -1,
                      "`Strike:` 触发 → **它自己回手了**（代词 `this troop` 认对了）");
                Check(ctx.Players[0].Hand.Count, handBefore + 1, "手牌 +1");
                Debug.Log(P + $"   ② 打了一下之后：场上 {SlotOf(ctx, 0, "Warp Spider")}、"
                          + $"手牌 {handBefore} → {ctx.Players[0].Hand.Count}");
            }
        }

        // ---- ③ 反向：触发时机**我们没有**的，仍然不许收（收了就是「注册一条没人消费的效果」）----
        {
            var talent = PoolCard(pool, "Howling Banshee Exarch");
            CheckTrue(talent != null, "卡池里有 `Howling Banshee Exarch`（它的 desc 里是 `Strike:`）");
            // 找一张只带 `Talent:` 的督军卡
            CardDef wl = null;
            foreach (var c in pool)
                if (c.Type == "hero" && c.Desc != null && c.Desc.Contains("Talent:")
                    && !c.Desc.Contains("Rally:") && !c.Desc.Contains("Strike:")) { wl = c; break; }
            CheckTrue(wl != null, $"找得到只带 `Talent:` 的督军卡（`{wl?.Name}`）");
            if (wl != null)
            {
                CheckTrue(wl.TriggerOps(KeywordTable.Rally) == null
                          && wl.TriggerOps(KeywordTable.Strike) == null,
                          "`Talent:` **不收** —— 本版没有那个触发时机，收了就是骗玩家");
            }
        }

        // ---- ④ 触发式正文里**没写主语**的 `gain`：落在**触发者自己**身上，不是全队 ----
        //   🔴 2026-09-13 A3 收窄的那条近似。卡面写 `Strike: Gain +2 Attack`
        //      （`Sisters Repentia` 的 `Penitence:` 也是这个形状）说的是**这张卡自己**；
        //      落成「己方全体」会**给全队各加一份**，而且看不出来 —— 没人会去数队友的数值。
        //   ⚠️ 反例（队友不动）才钉得住：只钉「触发者 +2」的话，「给全队都 +2」照样绿。
        {
            var striker = new CardDef("FixtureSelfGain", "FixtureSelfGain", "unit",
                                      "Strike: Gain +2 Attack", "common", "Test", 2, 2, 9, 0, null,
                                      subtype: "Infantry");
            var buddy = new CardDef("FixtureBuddy", "FixtureBuddy", "unit", "", "common", "Test",
                                    1, 1, 9, 0, null, subtype: "Infantry");
            var ctx = ProbeBattle(new[] { striker, buddy }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            var me = Place(ctx, 0, 0, striker, exhausted: false);
            var mate = Place(ctx, 0, 1, buddy, exhausted: true);
            Place(ctx, 1, 2, Unit("FixtureStrikePrey", 1, 0, 9), exhausted: true);
            CheckCode(RuleCore.DeclareAttack(ctx, 0, 0, 1, 2), RuleCodes.OK, "攻击一下（触发 `Strike`）");
            Check(me.Attack, 4, "★ **触发者自己** +2 攻（2 → 4）");
            Check(mate.Attack, 1,
                  "★ **队友一点都没变** —— 落回「己方全体」那条老近似时这里会实得 3");
        }
    }

    ///
    /// <summary>
    /// **部署时触发**（`… give Shield to all Drones you deploy`）与**持续改费**
    /// （`… Sabotage cards in the enemy hand cost 1 more`）—— 2026-09-13 第三十轮。
    ///
    /// **为什么这两件事放在一个方法里**：它们共用同一份**筛选条件**（<see cref="CardCriteria"/>，
    /// 我们的 `TargetCriteria`）。原版也是同一份 —— 部署那条路走 `OtherUnitSummoned` 事件、
    /// 改费那条路走 `HandEffect`，但两边都用 `TargetCriteria` 描述「作用在哪种卡上」。
    ///
    /// ⚠️ **断言必须钉「真的改变了局面」**，不能只钉「登记了一条效果」：
    ///    上一轮（常驻效果）的核心教训就是「注册一条永远不会被消费的效果 = 骗玩家」。
    ///    这里每条都验「该生效的生效了、**不该生效的没有**」——后者才是筛错了会露馅的地方。
    /// </summary>
    static void TestDeployBuffAndCostMore()
    {
        // ---- ① 部署时给：只给**筛中的那个兵种**，别的兵种一张都不给 ----
        {
            var pool = CardDatabase.Load();
            var t = Tactic("T_DeployBuff", 1,
                           "For the rest of this battle, give Shield to all Drones you deploy");
            var drone = new CardDef("FixtureDrone", "FixtureDrone", "unit", "", "common", "Test",
                                    1, 1, 3, 0, null, subtype: "Drone");
            var tank = new CardDef("FixtureTank", "FixtureTank", "unit", "", "common", "Test",
                                   1, 1, 3, 0, null, subtype: "Vehicle");
            var ctx = BattlePool(new[] { t, tank, drone }, new[] { Unit("E", 1, 1, 9) }, pool,
                                 warlordFaction: "TauEmpire");
            ToP1Turn(ctx, 3);

            // ⚠️ **先摆一个「早就在场上」的 Drone**（走 `Place` = 跳过部署流程）。
            //    没有它，这个用例钉不住最关键的那条：卡面写的是「你**部署**的」，
            //    不是「你场上**所有**的」—— 实现要是退回成「查场上所有 Drone」，
            //    下面那条 `droneEarly` 的断言就会亮。
            var early = Place(ctx, 0, 4, new CardDef("FixtureDroneEarly", "FixtureDroneEarly",
                                                    "unit", "", "common", "Test", 1, 1, 3, 0, null,
                                                    subtype: "Drone"));

            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_DeployBuff"), -1), RuleCodes.OK,
                      "`… give Shield to all Drones you deploy` 打得出去");
            Check(ctx.PersistentEffects.Count, 1, "登记了 1 条常驻效果");
            Check(ctx.PersistentEffects[0].Trigger, "deploy", "它是**部署时**触发，不是回合起止");
            CheckTrue(ctx.PersistentEffects[0].Criteria != null
                      && ctx.PersistentEffects[0].Criteria.KindWord == "drone",
                      "筛选条件记下来了（drone）");
            CheckTrue(!early.HasShield,
                      "**登记这条效果不会顺手给场上已有的 Drone 补上**（只对以后部署的生效）");

            // ⚠️ 筛错的话**这条会先炸** —— 部署一张 Vehicle 就不该给护盾
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "FixtureTank"), 1), RuleCodes.OK,
                      "部署一张 Vehicle");
            var tankU = Board(ctx, 0, 1);
            CheckTrue(tankU != null && !tankU.HasShield,
                      "部署的是 **Vehicle** → **不给**护盾（筛的是 Drone；筛错这里就会亮）");

            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "FixtureDrone"), 2), RuleCodes.OK,
                      "部署一张 Drone");
            var droneU = Board(ctx, 0, 2);
            CheckTrue(droneU != null && droneU.HasShield,
                      "部署的是 **Drone** → 常驻效果**真的把护盾加上了**");

            // ⚠️ **不能「一次生效就永远全体生效」**：护盾只给刚部署的那一个，
            //    不许回头把场上**早就躺着**的同类单位也补一遍（那是「打得比卡面宽」）
            CheckTrue(!early.HasShield,
                      "效果生效了也**没顺手给先前那个 Drone 补上**（`you deploy` ≠ `on your board`）");
            CheckTrue(tankU != null && !tankU.HasShield,
                      "后部署的 Drone 生效了，**也没顺手把先前的 Vehicle 补上**");
        }

        // ---- ② 持续改费：加在**对手手里**那类牌上，自己的不加、别的牌不加 ----
        {
            var pool = CardDatabase.Load();
            var t = Tactic("T_CostMore", 1,
                           "For the rest of this battle, Sabotage cards in the enemy hand cost 1 more");
            var sab = new CardDef("FixtureSabotage", "FixtureSabotage", "tactic", "Does nothing",
                                  "common", "Test", 3, 0, 0, 0, null, subtype: "Sabotage");
            var plain = Tactic("FixturePlain", 3, "Does nothing");
            // P1 手里也放一张破坏卡 —— 用来验「**自己手里的不加价**」
            var sab2 = new CardDef("FixtureSabotage2", "FixtureSabotage2", "tactic", "Does nothing",
                                   "common", "Test", 3, 0, 0, 0, null, subtype: "Sabotage");
            var ctx = BattlePool(new[] { t, sab2 }, new[] { sab, plain }, pool,
                                 warlordFaction: "Genestealers");
            ToP1Turn(ctx, 3);

            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_CostMore"), -1), RuleCodes.OK,
                      "`… Sabotage cards in the enemy hand cost 1 more` 打得出去");
            Check(ctx.CostMods.Count, 1, "挂上了 1 条费用修正");
            Check(ctx.CostMods[0].HandOf, 1,
                  "作用在 **P2 的手牌**上（`the enemy hand` —— 极性反了这条会亮）");

            Check(RuleCore.CostOf(ctx, 1, sab), 4, "对手手里的破坏卡 3 → **4**（真的加价了）");
            Check(RuleCore.CostOf(ctx, 1, plain), 3, "对手手里**别的**牌不加价");
            Check(RuleCore.CostOf(ctx, 0, sab2), 3, "**自己手里**的破坏卡不加价");
            Check(RuleCore.CostOf(ctx, 0, t), 1, "自己手里别的牌不加价");
        }

        // ---- ③ 实战检查：**整局里**这个机制真的会触发吗 ----
        // 出处：`资料/阵营推进_清单与交接.md` §六 ——「新做一批卡之后，照着这个测试的思路加一局
        // 实战检查，它是**跑得完 ≠ 跑得对**的那道关」。第十六轮就是靠它抓到
        // 「自动凑的牌组一张战术卡都没有」「AI 只出单位卡」这两个引擎自检抓不到的问题。
        //
        // ⚠️ **引擎是种子化的，同一副牌 + 同一个种子永远同一局** —— 所以这一节**不会 flaky**，
        //    断言可以写死「至少触发过 N 次」。
        //
        // ⚠️ 自动凑的牌组不一定含这几张卡，所以**硬塞**：用 TauEmpire 的牌组改几张。
        //    （7 张 `Drone` 全是 TauEmpire 的 —— `CreatePool` 的对账里写着「钛无人机 7=7」。）
        {
            var pool = CardDatabase.Load();
            CardDef tacticCard = null;
            var drones = new List<CardDef>();
            foreach (var c in pool)
            {
                if (c.Name == "Experimental Drone") tacticCard = c;
                if (c.Faction == "TauEmpire" && c.Subtype == "Drone" && c.IsUnit) drones.Add(c);
            }
            CheckTrue(tacticCard != null && drones.Count > 0,
                      $"卡池里有 `Experimental Drone` 和 TauEmpire 的 Drone（找到 {drones.Count} 张 Drone）");
            if (tacticCard != null && drones.Count > 0)
            {
                int registered = 0, fired = 0, granted = 0;
                for (int g = 0; g < 4; g++)
                {
                    // ⚠️ **牌组里全是 Drone** —— 第一版用 `StarterDeck` 自动凑，结果 4 局里只登记了 1 次，
                    //    而且登记之后部署的那 2 个单位**不是 Drone**（所以本来就不该触发）。
                    //    那样断言就被「部署的不是靶子」搅浑了，看不出机制到底有没有问题。
                    //    换成全 Drone 牌组 ⇒ **登记之后任何一次部署都是 Drone**，断言才有意义。
                    var d0 = new List<CardDef>();
                    CardDef hero = null;
                    foreach (var c in pool)
                        if (c.Faction == "TauEmpire" && c.Type == "hero") { hero = c; break; }
                    CheckTrue(hero != null, "TauEmpire 有督军卡");
                    if (hero == null) break;
                    d0.Add(hero);
                    for (int i = 0; i < 22; i++) d0.Add(drones[i % drones.Count]);
                    d0.Add(tacticCard);      // 抽牌是 `pop_back` ⇒ 放末尾 = 最先抽到

                    var d1 = DeckBuilder.StarterDeck(pool, "Goff", DeckBuilder.ClassicDeckSize,
                                                     new System.Random(400 + g), unitsOnly: false);
                    // ⚠️ `shuffle: false`：这一节要的是「一定跑到」，顺序必须自己说了算
                    var ctx = RuleCore.NewBattle(d0, d1, seed: 9000 + g, shuffle: false, cardPool: pool);

                    int guard = 0, tac = 0;
                    while (!ctx.IsOver && guard++ < 300)
                    {
                        RuleCore.BeginTurn(ctx);
                        PlayAiTurn(ctx, ref tac);
                        if (ctx.IsOver) break;
                        RuleCore.EndTurn(ctx);
                    }
                    foreach (string e in ctx.Events)
                    {
                        if (e.IndexOf("登记了**常驻效果**", System.StringComparison.Ordinal) >= 0
                            && e.IndexOf("部署", System.StringComparison.Ordinal) >= 0) registered++;
                        if (e.IndexOf("部署触发段：", System.StringComparison.Ordinal) >= 0) fired++;
                        if (e.IndexOf("盯上", System.StringComparison.Ordinal) >= 0) granted++;
                    }
                }
                Debug.Log(P + $"   实战：4 局里 `Experimental Drone` 被登记 {registered} 次 · "
                          + $"部署触发段跑了 {fired} 次 · 筛中 Drone {granted} 次");
                CheckTrue(registered > 0, "整局里**真的打出并登记了**这条常驻效果（AI 会打它）");
                CheckTrue(fired > 0, "整局里**部署触发段真的跑过**（不是「登记了但从不消费」）");
                CheckTrue(granted > 0, "整局里**真的筛中了 Drone**（筛选条件在实战数据上成立）");
            }
        }
    }

    /// <summary>按**引用**找在不在（手牌里两张同名卡是两张牌）</summary>
    static bool HasRef(List<CardDef> list, CardDef card)
    {
        foreach (var c in list) if (object.ReferenceEquals(c, card)) return true;
        return false;
    }

    /// <summary>同上，但吃**实例区域**（2026-09-18 第 7 行第 2 步）——
    /// 按**卡模板**比：问的是「这个模板的那一份在不在这个区域里」。</summary>
    static bool HasRef(List<CardInstance> zone, CardDef card)
    {
        foreach (var h in zone) if (h != null && object.ReferenceEquals(h.Card, card)) return true;
        return false;
    }

    /// <summary>
    /// **往手牌里塞一张新的**（自检夹具用）—— 2026-09-18 第 7 行第 2 步之后，手牌存的是
    /// `CardInstance`，夹具不能再直接 `Hand.Add(cardDef)`。走**对局计数器**发一份，和真路径同源。
    /// 返回塞进去的那一份（要按实例断言时用它）。
    /// </summary>
    static CardInstance Give(BattleContext ctx, int p, CardDef card)
    {
        var inst = ctx.NewInstance(card);
        ctx.Players[p].Hand.Add(inst);
        return inst;
    }

    /// <summary>牌库的**顺序**（名字连起来）—— 用来验「洗没洗牌」</summary>
    static string DeckOrder(BattleContext ctx, int p)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var c in ctx.Players[p].Deck) sb.Append(c.Card.Name).Append('|');
        return sb.ToString();
    }

    /// <summary>日志里有没有出现过某句话（负向断言用：老 bug 会留特征串）</summary>
    static bool HasLog(BattleContext ctx, string fragment)
    {
        foreach (string e in ctx.Events)
            if (e.IndexOf(fragment, System.StringComparison.Ordinal) >= 0) return true;
        return false;
    }

    /// <summary>
    /// **回手 / 回牌库（`return`）· `Draw it`（`drawref`）· `add X to your hand`（`create`）**
    /// —— 2026-09-13「按阵营逐个推进」的第一批（Sautekh / DarkAngels / TauEmpire）。
    ///
    /// 三件事都是从覆盖率那一栏里挖出来的，其中**一条是修静默错解析**：
    ///   · `Draw it` 以前掉进 `drawtype`，被当成「抽一种**叫 `it` 的兵种**」——
    ///     `MatchesKind("it")` 恒 false，翻遍牌库一张都找不到、只写一行日志就当无事发生，
    ///     **而覆盖率的「完全解析」和「载荷有机制」两栏都算它通过**。这一条最要紧。
    ///   · `return` 的语义规则书英文版 `:455-457` 写死了（回牌库**要洗**，除非卡面写明「顶」）。
    ///   · `add X to your hand` 卡在 `ReCreate` 只认 `create` 上（`If target dies, …` 的正文）。
    /// </summary>
    static void TestReturnAndRefs()
    {
        // ---- ① 解析：`return` ----
        {
            var op = OneOp("Return a friendly troop to your hand");
            Check(op.Verb, "return", "`Return … to your hand` → 动词 return");
            Check(op.Dest, "hand", "目的地 = 手牌");
            Check(op.Payload, "a friendly troop", "「谁」原样留着");
            CheckTrue(op.Target != null && op.Target.Side == "own",
                      "目标在自己那侧（表现层要靠它决定高亮哪边）");

            op = OneOp("Return a friendly Vehicle to your hand");
            Check(op.Dest, "hand", "`Return a friendly Vehicle to your hand` → 手牌");
            Check(op.Payload, "a friendly vehicle", "「谁」是小写原文");

            op = OneOp("Return a friendly troop and a random enemy troop to the top of their deck");
            Check(op.Verb, "return", "两个目标的写法也认");
            Check(op.Dest, "decktop", "`to the top of their deck` → **牌库顶**（不是洗入）");
            Check(op.Payload, "a friendly troop and a random enemy troop", "**两个**目标都留在 Payload 里");
            CheckTrue(op.Target == null,
                      "两目标时 `Target` 留空 —— 只填第一个会让表现层以为「只有一个目标」");

            // ⚠️ 目的地不纯 → **如实判不认识**。按「包含」匹配会把后半句静默吞掉
            // ⚠️ **2026-09-13 A4：这条断言反过来了**（原来是「判不认识」，理由写的是
            //    「那半句没做，不许吞掉装作做了」）。现在那半句**做了** ——
            //    `reduce its cost **to** N` 有了自己的原子（`EffectOp.CostSetTo`，见 ⑥ 那一段），
            //    而且 `TryReturn` 会把 `and <动词>` 的尾巴**切给 `Finish` 递归解**（和 give/draw/lowercost 同一套）。
            //    ⇒ 现在它必须是 `Ok`，而且**切成两条 op**（return + lowercost），不是「吞掉」。
            var rr = EffectText.ParseSegment("Return a friendly troop to your hand and reduce its cost to 1");
            Check(rr.Kind, EffectText.SegKind.Ok,
                  "`… to your hand **and reduce its cost to 1**` 现在**认得出**（A4 做了那半句）");
            Check(rr.Ops.Count, 2, "★ 而且切成**两条 op** —— 只回一条就是「把后半句吞掉了」");
            if (rr.Ops.Count == 2)
            {
                Check(rr.Ops[0].Verb, "return", "① return");
                Check(rr.Ops[1].Verb, "lowercost", "② lowercost");
                Check(rr.Ops[1].CostSetTo, 1, "★ ② 是**设为** 1 费（不是「降 1 费」）");
            }
        }

        // ---- ② 解析：`Draw it` 是 `drawref`，**不是** `drawtype("it")` ----
        {
            var r = EffectText.ParseSegment("Draw it and create a copy of it in your hand");
            CheckTrue(r.Ops != null && r.Ops.Count >= 1, "`Draw it and create a copy …` 拆得出效果");
            Check(r.Ops[0].Verb, "drawref", "`Draw it` → 动词 drawref（指代刚选中的那张）");
            foreach (var o in r.Ops)
                CheckTrue(o.Verb != "drawtype" || o.Payload != "it",
                          "**绝不能**再被当成 `drawtype` 且 payload = `it`（旧 bug 的特征）");

            var r2 = EffectText.ParseSegment("Draw 3 cards and lower their cost by 3");
            Check(r2.Ops.Count, 2, "`Draw 3 cards and lower their cost by 3` 拆成 2 条（抽牌 + 降费）");
            Check(r2.Ops[0].Verb, "draw", "第 1 条是抽牌");
            Check(r2.Ops[1].Verb, "lowercost", "第 2 条是降费（`and lower …` 现在切得开了）");
            Check(r2.Ops[1].Payload, "(指代上一张)", "降的是「刚抽到的那批」");
        }

        // ---- ③ 解析：`If target dies, add X to your hand` ----
        {
            var op = OneOp("If target dies, add Extermination Protocol to your hand");
            Check(op.Verb, "create", "`add X to your hand` → 动词 create（`add` 和 `create` 是同一件事）");
            Check(op.Dest, "hand", "目的地 = 手牌（`to` 那族写法）");
            Check(op.Payload, "extermination protocol", "造的是具名卡");
            Check(op.ConditionKind, "targetdies", "条件认出来了（认不出来会静默当成立）");
        }

        // ---- ④ 结算：`return` 真的把单位挪下手牌，而且**后续句的降费作用在它身上** ----
        {
            var pool = CardDatabase.Load();
            CardDef veh = null;
            foreach (var c in pool)
                if (c.Faction == "Ultramarines" && c.Subtype == "Vehicle") { veh = c; break; }
            CheckTrue(veh != null, "挑得到一张真实载具当尺子");

            var t = Tactic("T_Return", 1, "Return a friendly Vehicle to your hand. It costs 4 less");
            var ctx = BattlePool(new[] { t }, new[] { Unit("E", 1, 1, 5) }, pool);
            ToP1Turn(ctx, 3);
            var vehU = Place(ctx, 0, 1, veh);
            int before = RuleCore.CostOf(ctx, 0, vehU.Instance);   // 第 7 行第 3 步：尺子拿那一份实例

            // ⚠️ 要传**目标格位**：`return` 是「要选目标」的卡（`PickTarget` 拿得到 spec），
            //    `CanPlayTactic` 对这类卡要求 `targetSlot` 合法且那一格有人。载具放在槽 1。
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Return"), 1), RuleCodes.OK,
                      "`Return a friendly Vehicle to your hand` 打得出去");
            Check(Board(ctx, 0, 1), null, "单位**离开了格位**");
            CheckTrue(HasRef(ctx.Players[0].Hand, veh), "它**进了手牌**");
            CheckTrue(!HasRef(ctx.Players[0].Discard, veh),
                      "回手的那张**不进弃牌堆** —— 回手不是阵亡（弃牌堆里那 1 张是打出去的战术卡自己）");
            Check(ctx.DeadUnits.Count, 0, "**不进阵亡登记表** —— 它没死");
            CheckTrue(ReferenceEquals(vehU.Instance, HandInst(ctx, 0, veh.Name)),
                      "★ 回手的就是上场的那一份（实例身份）");
            Check(RuleCore.CostOf(ctx, 0, vehU.Instance), System.Math.Max(0, before - 4),
                  $"后续句 `It costs 4 less` 作用在**刚回手那张**上（{before} → {before - 4}）");
        }

        // ---- ⑤ 结算：`Draw it` 抽出的是**刚选中的那张**（不是空过） ----
        {
            var pool = CardDatabase.Load();
            var t = Tactic("T_DrawIt", 1, "Choose a troop from your deck. Draw it");
            var ctx = BattlePool(new[] { t }, new[] { Unit("E", 1, 1, 5) }, pool);
            ToP1Turn(ctx, 1);
            int deckBefore = ctx.Players[0].Deck.Count;
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_DrawIt"), -1), RuleCodes.OK,
                      "`Choose a troop from your deck. Draw it` 打得出去");
            Check(ctx.Players[0].Deck.Count, deckBefore - 1, "牌库**真的少了一张**（不是空过）");
            CheckTrue(!HasLog(ctx, "没找到「it」"),
                      "日志里**没有**「翻遍牌库也没找到 it」—— 那是旧 bug 的特征串");
        }

        // ---- ⑥ 规则书 `:477`：**牌库来源的 choose 要洗牌**（未选中的候选归还并洗） ----
        {
            var pool = CardDatabase.Load();
            var t = Tactic("T_ChooseShuffle", 1, "Choose a troop from your deck");
            var ctx = BattlePool(new[] { t }, new[] { Unit("E", 1, 1, 5) }, pool);
            ToP1Turn(ctx, 1);
            int n0 = ctx.Players[0].Deck.Count;
            string order0 = DeckOrder(ctx, 0);

            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_ChooseShuffle"), -1), RuleCodes.OK,
                      "`Choose a troop from your deck`（动作在下一句）打得出去");
            Check(ctx.Players[0].Deck.Count, n0,
                  "这条只挑不动，牌库**张数没变**");
            CheckTrue(DeckOrder(ctx, 0) != order0,
                      "但牌库**顺序变了 = 洗过牌**（规则书英文版 :477）");
        }
    }

    /// <summary>
    /// **卡面逐张核对**（2026-09-13）修掉的两列 —— `subtype`（字段）与 `keywords`。
    ///
    /// 为什么单开一条：这两列错了**不会报错**，只会**悄悄筛错卡** ——
    /// `Hunting Wolf` 卡面印的是「兽」，OCR 源表写成了 `Troop`，
    /// 于是 `抽/造一张兽` **永远抽不到它**，而卡看着是能用的。
    ///
    /// 数据是怎么修的：29 个子代理**逐张看 1118 张卡图**独立抄录（`Unity/资料/卡表核对_卡图提取/`），
    /// 算出修正条目（`Unity/工具/gen_cardface_fixes.py` → `数据/游戏数据/cardface_fixes.json`），
    /// 由 `工具/gen_cards_engine.py` 盖到卡表上。对账见 `资料/卡表逐张核对_与对账.md`。
    ///
    /// ⚠️ **这里钉的是「修好了」，不是「曾经的缺口」** —— 缺口见下面几条注释。
    /// </summary>
    /// <summary>
    /// 卡牌身份 —— **稳定 id**（2026-09-13 第三十三轮）。
    ///
    /// 为什么单开一节：卡表**一直拿卡名当身份**（`CardDatabase.cs` 里那句
    /// 「卡名当 id —— 真出重名再加 faction 前缀」，而重名早就出了）。后果三处全是静默的：
    ///   ① **费用修正按卡名匹配**（`CostMod.Key`）→ 同名卡一起降价
    ///   ② 复制品与原件是**同一个 `CardDef` 对象**，分不出「哪一张」
    ///   ③ 卡组存档存**卡名**，跨阵营重名时解析到错的那张（第三十二轮真踩到过）
    /// 所以这节的判据不是「id 字段有值」，而是「**同名卡分得开、按 id 反查回得来**」。
    /// </summary>
    static void TestCardIds()
    {
        var pool = CardDatabase.Load();

        // ---- ① 不变量：每张卡都有 id，且两两不同 ----
        // 判据只有一处（`CardDatabase.CheckIds`）—— 自检不重写一遍同样的逻辑。
        string why = CardDatabase.CheckIds(pool);
        // ⚠️ 张数**动态取**（原来写死 `1130 张`，卡池一动这句就变成误导 —— 2026-09-16 改成动态）。
        CheckTrue(why.Length == 0, $"每张卡都有唯一 id（{pool.Count} 张）"
                  + (why.Length > 0 ? "：" + why : ""));

        // ---- ② **跨阵营同名卡必须分得开**（发稳定 id 要解决的第一件事）----
        // 池子里 4 组同名卡，逐一钉住。右边的 id 是生成器从 `card_ids.json` 挑的
        // （挑法见 `gen_cards_engine.py` 的 `pick_id`：**按阵营前缀挑，挑不出就自造**）。
        // ⚠️ **2026-09-16 这里从 5 组变成 4 组**：原来第 5 组是 `Aggressor`
        //    （`DarkAngels` 的 `DA12` vs `SpaceWolves` 的自造 id `SW_Aggressor`）——
        //    那个 SW 行查明是**重复行**，已从 `card_stats.json` 删掉。判据（三条独立证据）：
        //      · 它与 `DA12` 那条**除 `faction`/`factionId` 外逐字段相同**
        //      · 它自己的 `subtitle` 写 `Dark Angels`、`ocrSrc` 指向
        //        `Dark Angels\Core\Warpforge_12_Aggressor.png`、语音是 `VO_DA_Aggressor`
        //      · 原版 bundle 里 SW 阵营**没有**独立的 Aggressor ——
        //        `bundle_spacemarinesspacewolvescardassets_assets_all/` 里只有
        //        `SM_SpaceWolves_inf_Blackmane Aggressor`（= 另一张卡 `SW35`），
        //        而 DA 那边有 `DarkAngels_inf_Aggressor`。
        //    ⇒ 同名跨阵营的**自造 id** 那条路现在没有样本了；以后再出现自造 id 要另立断言。
        var pairs = new[]
        {
            new[] { "Terminator Champion", "BlackLegion",     "EC33" },
            new[] { "Maulerfiend",         "BlackLegion",     "EC39" },
            new[] { "Bladeguard Veteran",  "DarkAngels",      "UM34" },
            new[] { "Terminator",          "EmperorsChildren","UM82" },
        };
        foreach (var p in pairs)
        {
            var a = CardDatabase.Find(pool, p[0], p[1]);
            var b = CardDatabase.FindById(pool, p[2]);
            CheckTrue(a != null, $"「{p[0]}」在 {p[1]} 里找得到");
            CheckTrue(a != null && b != null && a.Id != b.Id,
                      $"「{p[0]}」两张同名卡 **id 不同**：{a?.Id ?? "?"}（{p[1]}） vs {p[2]}");
            CheckTrue(b != null && CardDatabase.FindById(pool, b.Id) == b,
                      $"……按 id `{p[2]}` 反查**回到同一张**（{b?.Faction}）");
        }
        // ⚠️ **2026-09-16 改写了这一段**：原来拿 `Aggressor`/`SpaceWolves` 当**自造 id** 的样本
        //    （`SW_Aggressor`）。那个 SW 行查明是 `DA12` 的重复行、已从数据里删掉
        //    ⇒ 自造 id 现在**只出现在「原版 id 表里查不到」的卡上**，不再与同名跨阵营绑定。
        //    ⇒ 改成**覆盖全部自造 id**（判据：id 里带 `_`，见 `gen_cards_engine.py` 的 `pick_id`）——
        //      比钉死一张卡稳，也不怕以后再增删。
        int selfMade = 0, reverseFail = 0;
        foreach (var c in pool)
        {
            if (string.IsNullOrEmpty(c.Id) || c.Id.IndexOf('_') < 0) continue;
            selfMade++;
            if (CardDatabase.FindById(pool, c.Id) != c) reverseFail++;
        }
        CheckTrue(selfMade > 0, $"池子里有自造 id 的卡（{selfMade} 张）");
        CheckTrue(reverseFail == 0, $"自造 id 也能按 id 反查回同一张（{selfMade} 张，失败 {reverseFail} 张）");

        // ---- ③ id 的两种形态：原版 `AM12` / 自造 `AM_Some_Card` ----
        // **自造的必须数得出来**（不许静默）—— 它等于「这张卡原版 id 表里没有」。
        int orig = 0, made = 0, weird = 0;
        foreach (var c in pool)
        {
            var id = c.Id;
            if (string.IsNullOrEmpty(id)) { weird++; continue; }
            if (id.IndexOf('_') >= 0) made++; else orig++;
        }
        Check(weird, 0, "没有「既不是原版形态、也不是自造形态」的 id");
        Check(orig + made, pool.Count, "两种形态加起来 = 卡池张数");
        CheckTrue(orig > 900, $"原版 id 覆盖 {orig} 张（余 {made} 张是自造的）");

        // ---- ④ 索引与逐个找**结论一致**（`IdIndex` 是给循环用的，别和 `FindById` 分家）----
        var idx = CardDatabase.IdIndex(pool);
        Check(idx.Count, pool.Count, "`IdIndex` 建出来的条目数 = 卡池张数（没有 id 撞车）");
        var probe = pool[pool.Count / 2];
        CheckTrue(idx.TryGetValue(probe.Id, out var hit) && hit == probe,
                  $"`IdIndex[\"{probe.Id}\"]` 与 `FindById` 拿到同一张（{probe.Name}）");
    }

    static void TestCardFaceFixes()
    {
        var pool = CardDatabase.Load();

        // ---- ① 不变量：**没有任何单位卡的 subtype 是「卡的类型」** ----
        // 这正是当初串列的形态（`Heavy Intercessor` 被写成 `Unit`、`Daemonette` 被写成 `Troop`）。
        // 钉成不变量之后，将来谁再把类型填进字段列，这条会当场炸。
        var typey = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
                    { "unit", "troop", "soldier", "card", "upgrade" };
        var bad = new List<string>();
        foreach (var c in pool)
            if (c.IsUnit && !string.IsNullOrEmpty(c.Subtype) && typey.Contains(c.Subtype))
                bad.Add(c.Name + "=" + c.Subtype);
        Check(bad.Count, 0, "**没有单位卡的 subtype 是「卡的类型」**"
              + (bad.Count > 0 ? "：" + string.Join("、", bad.ToArray(), 0, System.Math.Min(6, bad.Count)) : ""));

        // ---- ② 逐张钉：这些是卡图上白纸黑字印着的 ----
        Check(SubOf(pool, "Heavy Intercessor"), "Infantry",
              "`Heavy Intercessor` 卡面印 `Infantry`（OCR 源表里曾是 `Unit`）");
        Check(SubOf(pool, "Hunting Wolf"), "Beast",
              "`Hunting Wolf` 卡面印 `Beast`（曾是 `Troop`）—— 它得能被「兽」筛到");
        Check(SubOf(pool, "Fenrisian Wolf"), "Beast", "`Fenrisian Wolf` 同样是兽");
        Check(SubOf(pool, "Daemonette"), "Daemon",
              "`Daemonette` 卡面印 `Daemon`（曾是 `Troop`）—— 恶魔不是兵");
        Check(SubOf(pool, "Shivversplint"), "Combat Elixir",
              "`Shivversplint` 卡面印 `Combat Elixir`（曾是 `Upgrade`）");
        Check(SubOf(pool, "Grim Effigy"), "Defence",
              "`Grim Effigy` 卡面印 `Defence`（曾是 `Spell`）");

        // ---- ③ 字段真的**筛得对**（不只看字段值，要看筛选结果）----
        var beasts = CreatePool.Resolve(pool, "random beast", "SpaceWolves");
        CheckTrue(HasCard(beasts.Cards, "Hunting Wolf") && HasCard(beasts.Cards, "Fenrisian Wolf"),
                  "`Create a random Beast` 现在**真能筛到**这两头狼（修之前筛不到）");

        // ---- ④ 关键词的**数值**：卡面 `Armour 1. Blast 2` ----
        // 修之前 keywords 是 `['Armour','Blast']` —— **数字没了**，`KwValue` 取 0、护甲不生效。
        CheckTrue(HasKwWithValue(pool, "War Walker", "armour", 1),
                  "`War Walker` 的 `Armour` **带上了数值 1**（修之前是光秃秃的 `Armour`）");
        Check(KwValueOf(pool, "War Walker", "armour"), 1,
              "……而且 `UnitState.KwValue(\"armour\")` 真的读得到 1（护甲从「不生效」变成「生效」）");
        CheckTrue(HasKwWithValue(pool, "Vyper", "shuriken", 3), "`Vyper` 的 `Shuriken 3`");
        CheckTrue(HasKwWithValue(pool, "Night Spinner", "blast", 3),
                  "`Night Spinner` 补上了**整个缺失**的 `Blast 3`");

        // ---- ⑤ 反例：**效果里「给别人加」的不许写进这张卡的关键词** ----
        // `Baneblade Tank` 卡面是 `Armour 2. Adjacent units have Armour 1` ——
        // 第二个 Armour 1 是**给邻居的**。算修正表时专门排掉了，这里钉住它没被误写进来。
        var bane = new UnitState(FindCard(pool, "Baneblade Tank"), false);
        Check(bane.KwValue("armour"), 2,
              "`Baneblade Tank` 自己的护甲是 **2** —— 不是邻居那 1 点（那是效果给的）");
    }

    /// <summary>
    /// 🔴 **2026-09-16：缩写系动词 `it's` / `they're` 让「兵种条件」整条静默失效。**
    ///
    /// **病根**（三层，缺一层都修不好）：
    ///   ① `EffectText.ClauseFullWord` / `IsClauseWord` 的正则原来只认**分开写**的
    ///      `it is` / `they are`，**缩写 `it's a Daemon` 一条都匹配不上**；
    ///   ② ⇒ 掉到 `Normalize` 末条 `c.StartsWith("it's a ") → "istype"`；
    ///   ③ ⇒ 结算层 `case "istype": return false`，而 `ConditionHolds` 的契约里
    ///      **返回 false = 「判不了」**（不是「不成立」）⇒ **整条效果不生效**。
    ///
    /// **首例 = 用户 2026-09-16 点名的那张**：帝皇之子督军天赋 `Excessive Vigour` ——
    /// `Give a Dark Pact of Excess to a friendly troop. **If it's a Daemon, give it +2 Health as well**`
    /// （中文「给予一个友方部队纵欲黑暗契约。**若其为恶魔**，另给予 +2 生命。」）。
    /// 前半句照常结算，后半句**永远不发生**。全池同形 **8 处**（`it's a Vehicle` ×3 ·
    /// `it's a troop` ×2 · `it's a Daemon` · `it's a Beast` · `it's an Infantry`）、
    /// 7 张卡（`Excessive Vigour` · `Thermal Weaponry` · `Darkshroud` · `Gnarled and Rugged` ·
    /// `Scrag 'Em` · `Technological Supremacy` · `Thundering Rampage`）。
    ///
    /// ⚠️ **断言四层** —— 只钉第①层的话，「归一化对了但没人结算」照样绿（这个工程吃过）：
    ///   ① 解析层：缩写归成**真会判的** `targethaskw`，不再是 `istype`；
    ///   ② **反向**：不认识的词**仍然**落 `istype`（该判不了就判不了，别顺手改成「成立」）；
    ///   ③ 结算层：真打一次真卡，**恶魔真拿到 +2 生命、步兵一点都拿不到**；
    ///   ④ 全池：没有任何一张卡的条件还会归出 `istype`。
    /// </summary>
    static void TestConditionContractions()
    {
        // ---- ① 解析层 ----
        Check(EffectCondition.Normalize("it's a daemon"), "targethaskw",
              "★ `it's a daemon` 归成 `targethaskw` —— 改之前是 `istype`（判不了）");
        Check(EffectCondition.Normalize("it's a vehicle"), "targethaskw", "……`it's a vehicle` 同");
        Check(EffectCondition.Normalize("it's an infantry"), "targethaskw", "……`it's an infantry`（冠词 `an`）同");
        Check(EffectCondition.Normalize("they're battlesuits"), "targethaskw",
              "……`they're battlesuits`（缩写 + 复数）同");
        Check(EffectCondition.Normalize("it is a daemon"), "targethaskw",
              "……分开写的 `it is a daemon` —— **这条本来就没坏**，钉住它别被顺手改坏");

        // ---- ② 反向：不认识的兵种词**必须**还是「判不了」 ----
        // 判据：`ClauseKeyword` 只认「**整词**命中关键词表或兵种表」的词，`Grot` 两边都不是。
        // 这条要是变成 `targethaskw`，等于把「不认识的兵种」当成**恒成立** —— 那是静默打错。
        Check(EffectCondition.Normalize("it's a grot"), "istype",
              "★ 反向：`it's a grot`（我们不认识的兵种词）**仍然归 `istype`** = 判不了");

        // ---- ③ 结算层：真打一次**真卡面原文** ----
        var pool = CardDatabase.Load();
        // ⚠️ 卡**名**带括号后缀（`id` 是 `EC_Excessive_Vigour_Daemon_Prince_s_Talent`）——
        //    `FindCard` 比的是 `Name`，写成 `Excessive Vigour` 会找不到。
        var real = FindCard(pool, "Excessive Vigour (Daemon Prince's Talent)");
        CheckTrue(real != null, "卡池里找得到 `Excessive Vigour (Daemon Prince's Talent)`（帝皇之子督军天赋）");
        if (real != null)
        {
            // ⚠️ `desc` 取**真卡面原文**，不在这儿手写一句「差不多」的英文 ——
            //    这次改的是**正则**，测的就必须是真句子，否则测的是我编的句子。
            int onDaemon = ExcessVigourHealthGain(real, "Daemon");
            int onInfantry = ExcessVigourHealthGain(real, "Infantry");
            Check(onDaemon, 2,
                  "★ 目标是**恶魔**时真多了 +2 生命 —— 改之前是 0（`istype` 判不了 ⇒ 这半句从不存在）");
            Check(onInfantry, 0,
                  "★ 反向：目标是**步兵**时一点生命都不加（条件该**不成立**，不是判不了）");
        }

        // ---- ④ 全池：没有一张卡还在用「结算层判不了」的条件种类 ----
        // ⚠️ **这一步是尺子本身**：解析覆盖率报表原来只看「条件种类是不是空」，
        //    看不见「这个种类在结算层是死桩」—— `istype` 就是这么绿着过了好几轮的。
        //    现在判据只有一个来源：`RuleCore.UnjudgeableConditions`（声明在 `EffectResolver.cs`）。
        //    **加条件种类时那张表与 `ConditionHolds` 一起改，这条断言负责对账。**
        {
            int hit = 0;
            var where = new List<string>();
            foreach (var c in pool)
            {
                if (c == null || string.IsNullOrEmpty(c.Desc)) continue;
                var ops = EffectText.Parse(c.Desc, out _, out _);
                foreach (var o in ops)
                {
                    // ⚠️ **没有条件的 op 直接跳过** —— `CanJudgeCondition("")` 是 false（空种类 =
                    //    压根没归一出来），不排掉的话**每一条无条件的效果都会被算成「判不了」**
                    //    （第一版就是这么错的：报出 891 张，全是假警报）。
                    if (string.IsNullOrEmpty(o.Condition)) continue;
                    // ⚠️ 读的是 `ConditionKind`（**归一化后的名字**）—— `ConditionHolds` 就是 switch 它。
                    //    `Condition` 是**原文**（`「it's a Daemon」`），只用来在失败信息里显示。
                    if (!RuleCore.CanJudgeCondition(o.ConditionKind))
                    {
                        hit++;
                        where.Add(c.Name + "「" + o.Condition + "」→ " + o.ConditionKind);
                        break;
                    }
                }
            }
            Check(hit, 0, "★ 全池没有一张卡在用「结算层判不了」的条件种类"
                        + (where.Count > 0 ? " —— 还有：" + string.Join(" / ", where) : ""));
        }
    }

    /// <summary>
    /// 真打一次 `Excessive Vigour`，返回目标**生命上限**的增量。
    /// 目标兵种由 `subtype` 指定 —— 卡面那句 `If it's a Daemon` 判的就是它。
    /// </summary>
    static int ExcessVigourHealthGain(CardDef real, string subtype)
    {
        var card = Tactic("T_ExcessiveVigour", 0, real.Desc);
        var ctx = ProbeBattle(new[] { card }, new[] { Unit("EFoe", 1, 0, 9) });
        ToP1Turn(ctx, 2);
        var target = Place(ctx, 0, 0, new CardDef("FixTgt", "FixTgt", "unit", "", null, "Test",
                                                  2, 2, 3, 0, null, subtype: subtype));
        int before = target.MaxHealth;
        CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_ExcessiveVigour"), 0), RuleCodes.OK,
                  "打出 `Excessive Vigour`（指着我方那个部队）");
        return target.MaxHealth - before;
    }

    /// <summary>
    /// 🔴 **2026-09-16：`Accursed Helbrute` 的 `it as well` 那半句从来没发生过。**
    ///
    /// 卡面（`BL74` · unit · 8 费 9/7/9 · BlackLegion）：
    /// `When a friendly troop receives a ✦Dark Pact, **this troop gains it as well**`
    ///
    /// **两处根因**（① 是看得见的症状，② 是修完 ① 才会暴露的静默错打）：
    ///   ① `GivePayload.ParseInto` 五条判据（能量 / 嵌入 / 多属性拆分 / `GiveKw` / `ReAttr`）
    ///      **一条都不命中 `it as well`** ⇒ `Parse` 返回 null ⇒ `DoGive` 报
    ///      「载荷本版不认识」并**整条 op 空过**；
    ///   ② `EffectText.ParseTarget` 把 `this troop` 交给 `IsPronoun` ⇒ `Side/Kind = prev`，
    ///      而事件路的 `prev` 被种子成**事件主语** ⇒ 修好 ① 之后契约会加给
    ///      **刚收到契约的那个队友**，不是印刷这张卡的它自己。
    ///
    /// ⚠️ **断言三层**（工程惯例；只钉第①层的话「认出来了但加错人」照样绿）：
    ///   ① 解析层：载荷认出 `CopyEventPact`、目标判成 `Subjectless`；
    ///   ② **反向**：`that troop` **仍然是 `prev`** —— 别把「上一条效果的目标」一起改掉；
    ///   ③ 结算层：真广播一次，**它也拿到了、而且拿的是同一份契约**（不是随机另抽一种）。
    /// </summary>
    static void TestAccursedHelbrute()
    {
        const string desc =
            "When a friendly troop receives a Dark Pact, this troop gains it as well";

        // ---- ① 解析层 ----
        {
            var ops = EffectText.Parse(desc, out _, out _);
            bool copy = false;
            var seen = new List<string>();
            foreach (var o in ops)
            {
                if (!string.IsNullOrEmpty(o.Payload))
                {
                    var ps = GivePayload.Parse(o.Payload);
                    if (ps != null) foreach (var p in ps) if (p.CopyEventPact) copy = true;
                }
                seen.Add((o.Verb ?? "?") + "「" + (o.Payload ?? "") + "」→ "
                         + (o.Target == null
                            ? "(无目标)"
                            : "raw「" + o.Target.Raw + "」side=" + o.Target.Side + " kind=" + o.Target.Kind
                              + " count=" + o.Target.Count + " auto=" + o.Target.Auto
                              + " subj=" + o.Target.Subjectless));
            }
            CheckTrue(copy, "★ 载荷 `it as well` 认出来了（改之前五条判据一条都不命中 ⇒ 整条 op 空过）");

            // ⚠️ **别拿 `EffectText.Parse(整条 desc)` 的结果去验 `this troop` 的指代** ——
            //    那是**裸解析**，`When <事件>, <正文>` 的切分发生在 `CardDef.AddWhenTrigger`，
            //    裸解析下整条事件从句会**漏进目标短语**（实测 raw「when a friendly troop receives
            //    a dark pact, this troop」）。引擎真正跑的是**事件层**那条路。
            //    所以这里**直接钉 `ParseTarget` 本身**（那是这次改的那个函数）。
            var direct = EffectText.ParseTarget("this troop");
            CheckTrue(direct != null && direct.Subjectless,
                      "★ `ParseTarget(\"this troop\")` = `Subjectless`（有施放者就是它自己），不再是 `prev`");
            // ② 那句反向的姊妹断言在下面（`that troop` 仍然 `prev`）。
            var bodyOps = EffectText.Parse("this troop gains it as well", out _, out _);
            string bodyDump = "";
            foreach (var o in bodyOps)
                bodyDump += (o.Verb ?? "?") + "→"
                          + (o.Target == null ? "(无目标)"
                             : "raw「" + o.Target.Raw + "」side=" + o.Target.Side
                               + " count=" + o.Target.Count + " subj=" + o.Target.Subjectless) + " ‖ ";
            // 这一条**只报数不断言**：事件层正文的解析形状（拿来跟上面那条对照）。
            CheckTrue(true, "（诊断）事件正文 `this troop gains it as well` 解析成：" + bodyDump);
            // 裸解析那条路也顺带留个证据，方便下一个会话看懂上面为什么不用它
            CheckTrue(seen.Count > 0, "（诊断）整条 desc 的裸解析：" + string.Join(" ‖ ", seen));
        }

        // ---- ② 反向：`that troop` 不许被一起改掉 ----
        {
            var keep = EffectText.ParseTarget("that troop");
            CheckTrue(keep != null && keep.Side == "prev" && !keep.Subjectless,
                      "★ 反向：`that troop` **仍然是 `prev`**（它真的是「上一条效果的目标」）");
        }

        // ---- ③ 结算层：真广播一次 ----
        {
            var brute = new CardDef("BL74F", "BL74F", "unit", desc, "common", "Test",
                                    8, 9, 7, 9, null, subtype: "Infantry");
            var ally = Unit("Ally", 1, 2, 5);
            var ctx = Battle(new[] { brute, ally }, new[] { Unit("X", 1, 1, 5) });
            ToP1Turn(ctx, 1);
            var b = Place(ctx, 0, 0, brute);
            var a = Place(ctx, 0, 1, ally);
            RuleCore.GrantDarkPact(ctx, 0, a, "excess", "自检");
            CheckTrue(b.Has(KeywordTable.DarkPact),
                      "★ 队友收到契约 ⇒ **它也拿到了**（改之前这半句从不发生）");
            Check(RuleCore.PactOf(b), "excess",
                  "★ 而且是**同一份**（纵欲）—— 不是随机另抽一种（卡面写的是「同样」）");
            // 🔴 自递归护栏：它自己收到契约时，监听器会「再给自己一份」⇒ 再广播 ⟳ 撞 `MaxEffectChain`。
            //    这里钉住「自己那一份不会把它顶掉」（种类仍是纵欲，链条没有反复重放）。
            Check(RuleCore.PactOf(a), "excess", "……队友那份没被顶掉");
        }
    }

    /// <summary>
    /// 🔴 **2026-09-16：`Alpha Warrior` 的 `plus an additional 1` 那半句没有生效。**
    ///
    /// 卡面（`TL75` · tactic · 2 费 · Leviathan）：
    /// `Ephemeral. Your Warlord gains Flying and +2 Melee Attack this turn,
    ///  **plus an additional 1 for each friendly troop**`
    ///
    /// **两条挡路**（缺一条都解释不了现状）：
    ///   ① 前半句没写 ` to `（`Your Warlord gains …` 的**目标就是主语**）
    ///      ⇒ `SplitPlusClause` 那条路的 `ContainsTo(headPlus)` 为假，整条 `, plus` 路进不去；
    ///   ② 附加句 **`an additional 1` 连属性名都省了**（继承前面那个 `+2 Melee Attack`）
    ///      ⇒ `GivePayload` 认不出，`BuildPlusGive` 拼出来也解不了。
    ///   ⇒ 现状：`, plus an additional 1` **留在载荷里**（探针实测载荷成了
    ///     `flying and +2 melee attack , plus an additional 1`），那 +N **整段没有**。
    ///
    /// ⚠️ **断言三层**：
    ///   ① 解析层：载荷里不再残留 `plus`、且真的多出一条动词 op；
    ///   ② **反向**：另外 3 张 `, plus` 卡（走的是老路）**不许被改坏**；
    ///   ③ 结算层：督军真的拿到「+2 基础 ＋ 每个友方部队 +1」。
    /// </summary>
    static void TestAlphaWarrior()
    {
        const string desc =
            "Ephemeral. Your Warlord gains Flying and +2 Melee Attack this turn, "
            + "plus an additional 1 for each friendly troop";

        // ---- ① 解析层 ----
        {
            var ops = EffectText.Parse(desc, out _, out _);
            bool leftover = false;
            int verbs = 0;
            foreach (var o in ops)
            {
                if (!string.IsNullOrEmpty(o.Payload)
                    && o.Payload.ToLowerInvariant().Contains("plus")) leftover = true;
                if (o.Verb == "give" || o.Verb == "gain") verbs++;
            }
            CheckTrue(!leftover, "★ 载荷里不再残留 `, plus an additional 1`（改之前整段被吞进载荷）");
            CheckTrue(verbs >= 2, "★ 拆出了**两条**动词 op（前半句 + `additional 1` 那半句）");
        }

        // ---- ② 反向：另外 3 张 `, plus` 卡不许被改坏 ----
        //    它们前半句都写了 ` to ` ⇒ 走的是原来那条老路，本次改动不该碰到它们。
        {
            var pool = CardDatabase.Load();
            foreach (var nm in new[] { "Grizzled Skarboy", "Fenrisian Monstrosities", "Relentless Fusillade" })
            {
                var c = FindCard(pool, nm);
                CheckTrue(c != null, "卡池里找得到 `" + nm + "`");
                if (c == null) continue;
                var fs = EffectText.Parse(c.Desc, out _, out _);
                bool stranded = false;
                foreach (var o in fs)
                    if (!string.IsNullOrEmpty(o.Payload)
                        && o.Payload.ToLowerInvariant().Contains("plus")) stranded = true;
                CheckTrue(!stranded, "★ 反向：`" + nm + "` 的 `, plus` 仍然被拆开（载荷里没残留）");
            }
        }

        // ---- ③ 结算层 ----
        {
            var aw = Tactic("T_AlphaWarrior", 2, desc);
            var ctx = Battle(new[] { aw }, new[] { Unit("X", 1, 1, 9) });
            ToP1Turn(ctx, 6);
            Place(ctx, 0, 0, Unit("A1", 1, 1, 5));
            Place(ctx, 0, 1, Unit("A2", 1, 1, 5));
            Place(ctx, 0, 2, Unit("A3", 1, 1, 5));
            var wl = ctx.Players[0].Warlord;
            int before = wl.Attack;
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_AlphaWarrior"), -1), RuleCodes.OK,
                      "打出 `Alpha Warrior`");
            Check(wl.Attack, before + 5,
                  $"★ 督军拿到 +2 基础**再加** 3 个友方部队各 +1（{before} → 期望 {before + 5}）");
        }
    }

    /// <summary>
    /// 🔴 **2026-09-16：静默桩普查找出的「家族」**（正本 `_tmp_view/stub_audit_0916.md`）。
    ///
    /// `istype` **不是孤例** —— 同一把尺子（「解析层**认得出**、结算层**判不了**」）扫出
    /// **6 个成员**（条件 5 + 载荷 1）。本方法钉住其中四个已修的 ＋ **尺子本身**：
    ///
    /// | 种类 | 原来怎么坏 | 影响 |
    /// |---|---|---|
    /// | `controlcount` | 只实现了「控制 **0 个部队**吗」，且带数字一律判不了 | **5 张判错 + 1 张判不了**，走**正常分支**（连「判不了」都不报）——全池最静的一档 |
    /// | `energycheck` | 恒 `return false` | 1 张，而且是**单位卡**（`BattleDriver` 只给战术卡打 `*`）⇒ 报表/卡面**双侧看不见** |
    /// | `alreadyhas` | `Hunt Mark` 的规范名是 `huntmark`（无空格），`Contains` **永远假** | 1 张（`SW55`）「改为摧毁」从来不发生 |
    /// | `targethaskw` 的 `A or B` | `IsClauseWord` **只取第一个词** | 1 张（`TAU48`）后半静默丢，报表还显示「完全解析」 |
    ///
    /// ⚠️ 断言**两层**：① 每个种类现在**判得了**（`CanJudgeCondition`）；
    ///    ② `controlcount` **真打一次**——只钉「种类名字对不对」的话，
    ///       「判得了但判反了」照样绿（这次修的正是判据本身）。
    ///    「全池不再出现判不了的种类」那条尺子在 `TestConditionContractions` 的 ④。
    /// </summary>
    static void TestConditionKindFamily()
    {
        // ---- ① 四个种类的「判得了」标记 ----
        Check(EffectCondition.Normalize("if you control a vehicle"), "controlcount",
              "`If you control a Vehicle` 仍然归 `controlcount`");
        CheckTrue(RuleCore.CanJudgeCondition("controlcount"),
                  "★ `controlcount` 现在**判得了**（原来只实现「0 个部队」，5 张判错）");
        CheckTrue(RuleCore.CanJudgeCondition("energycheck"),
                  "★ `energycheck` 现在**判得了**（原来恒 `return false`，1 张单位卡静默）");
        CheckTrue(RuleCore.CanJudgeCondition("targethaskw"), "`targethaskw` 判得了");
        CheckTrue(!RuleCore.CanJudgeCondition("istype"),
                  "★ 反向：`istype` **仍然标着判不了** —— 它是兜底，不许被顺手标成判得了");
        CheckTrue(!RuleCore.CanJudgeCondition(null) && !RuleCore.CanJudgeCondition(""),
                  "★ 反向：空种类（= 解析层压根没归一出来）仍然算判不了");

        // ---- ② `A or B` 两个词都要认出来 ----
        {
            var two = EffectCondition.ClauseKeywords("it's a vehicle or battlesuit");
            CheckTrue(two != null && two.Length == 2,
                      "★ `If it's a Vehicle **or** Battlesuit` 拆出**两个**词"
                      + "（改之前 `IsClauseWord` 只取第一个 ⇒ 只判载具，`TAU48` 后半静默丢）"
                      + " —— 实得：" + (two == null ? "null" : two.Length + " 个【" + string.Join("|", two) + "】"));
            var one = EffectCondition.ClauseKeywords("it has stealth");
            CheckTrue(one != null && one.Length == 1, "……单个词那条**没被改坏**（`If it has Stealth`）");
            // 有一个词不认识 ⇒ **整条判不了**（别静默当成成立 —— 那是本工程的红线）
            CheckTrue(EffectCondition.ClauseKeywords("it's a vehicle or grot") == null,
                      "★ 反向：`… a Vehicle **or Grot**`（有个词不认识）**整条返回 null** = 判不了");
        }

        // ---- ③ `alreadyhas`：多词关键词要认得出来 ----
        //    判据在 `EffectResolver.ExtractKeywordFromCondition`（私有）——
        //    这里钉的是**归一化后的种类**，结算行为由上游 ① 那条 `CanJudgeCondition` 兜住。
        Check(EffectCondition.Normalize("if it already had hunt mark"), "alreadyhas",
              "★ `If it already had Hunt Mark` 归 `alreadyhas`（`SW55` 的那半句）");

        // ---- ④ `controlcount` 真打一次 ----
        //    卡面写 `If you control a Beast` —— 用**真卡面的条件原文**，效果那半句是夹具
        //    （真卡 `GOF78` 的效应是 `gain +1 Attack`，要 `Rally`/`Tide` 才触发，夹具太重）。
        {
            int Delta(bool withBeast)
            {
                var card = Tactic("T_CtrlBeast", 1,
                    "Give +1 Attack to a friendly troop. If you control a Beast, draw a card");
                var ctx = Battle(new[] { card }, new[] { Unit("X", 1, 1, 9) });
                ToP1Turn(ctx, 5);
                Place(ctx, 0, 0, Unit("Tgt", 1, 1, 5));
                if (withBeast)
                    Place(ctx, 0, 1, new CardDef("Bst", "Bst", "unit", "", null, "Test",
                                                 1, 1, 5, 0, null, subtype: "Beast"));
                int h0 = ctx.Players[0].Hand.Count;
                RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_CtrlBeast"), 0);
                return ctx.Players[0].Hand.Count - h0;      // 打出去的卡 −1、抽到的 +1
            }
            int withBeast = Delta(true), withoutBeast = Delta(false);
            Check(withBeast - withoutBeast, 1,
                  "★ `If you control a Beast`：场上**有**野兽 ⇒ 那半句执行（抽 1 张）；"
                  + "**没有** ⇒ 不执行。改之前判的是「你场上有没有**部队**」——"
                  + "有部队判**不成立**（该触发时不触发）、空场判成立（不该触发时触发），正好反着"
                  + $" —— 实得 有/无 = {withBeast}/{withoutBeast}");
        }

        // ---- ⑤ 尺子自己抓出来的另外两张：`target is a <兵种>` ----
        //    `Vex Machinator`（`BL48`）：`Deal 5 damage to an enemy. If target is a Vehicle, destroy it`
        //    `Armoury of Excess`（`EC62`）：`… If target is a friendly troop, … If target is an enemy, …`
        //    这三句原来**一个字都归一不出来**（`ConditionKind` 是空的）⇒ 判不了、同样静默 ——
        //    它们**不是**这次普查一开始就点名的，是**这把尺子自己抓出来的**（说明尺子有用）。
        {
            Check(EffectCondition.Normalize("target is a vehicle"), "targethaskw",
                  "★ `If target is a Vehicle` 归 `targethaskw`（改之前是空 —— `ReClauseSubject` 不认裸 `target`）");
            Check(EffectCondition.Normalize("target is a friendly troop"), "targethaskw",
                  "★ `If target is a friendly troop` 同（**阵营前缀**那一支）");
            Check(EffectCondition.Normalize("target is an enemy"), "targethaskw",
                  "★ `If target is an enemy` 同（**裸** `enemy`，只判阵营、不筛兵种）");
        }
    }

    /// <summary>
    /// 🔴 **2026-09-16：「中文卡面 × 英文解析」全池对账**查出的两条**静默玩法错**
    /// （正本 `_tmp_view/zh_crosscheck_0916.md` · 脚本 `工具/zh_crosscheck.py`）。
    ///
    /// 为什么中文能当第二把尺子：它**不是权威**（`zh_cards.json` 是**我们自己译的**，
    /// 见那份报告第 1 节 —— 官方中文本地从来不存在），但能抓「**翻译时读懂了、解析器没读懂**」
    /// 这一类 —— 它跟英文原文**互相独立**。这次一把尺子抓到 2 条真错（另有 12 条假阳性）。
    ///
    /// **① `Shrineworld`（`SOR62`）—— 数据侧**：
    ///   卡面（成品图逐字核过 `d:/2/Warpforge部队卡片/Sorotitas/4计策/Warpforge_62_Shrineworld.png`）印
    ///   `Draw 3 cards. For each Troop drawn, gain 1 ☀`；而 `数据/游戏数据/cardface_fixes.json`
    ///   的 `desc` 列**把段首写成了 `Destroy an enemy unit.`** —— 那一列的用途只是「补回被 OCR
    ///   丢掉的图标」，不该改句子，而它的**覆盖优先级高于** `card_stats.json`（后者是对的）。
    ///   ⇒ 这张 6 费卡**打出去消灭一个敌方单位**，探针还报「认了 · 0 · 0」。
    ///
    /// **② `Prayer`（`SOR48`）—— 解析侧**：
    ///   卡面（逐字核过 `Sorotitas/4计策/Warpforge_48_Prayer.png`）印
    ///   `Give ⊙Shield to a friendly unit. If the unit has ▲Pray gain 1 ☀` —— **`Pray` 和 `gain`
    ///   之间真的没有逗号**。而 `TryIf` 原来那句 `comma &lt; 4 → return false` 直接判「不是 if 句」
    ///   ⇒ 整段落到别的 handler 上、**条件被静默丢掉**，每次白给 1 点信仰。
    ///   ⚠️ 这不能靠改数据（`desc` 是卡面的转录，加个逗号就是伪造卡面）⇒ 只能改解析器，
    ///   新增 `TryIfNoComma`（按词边界试切，条件那半必须归得出来、正文那半必须解得出来）。
    /// </summary>
    static void TestZhCrosscheckFindings()
    {
        var pool = CardDatabase.Load();

        // ---- ① `Shrineworld`：不许再解出 `destroy` ----
        {
            var sw = FindCard(pool, "Shrineworld");
            CheckTrue(sw != null, "卡池里找得到 `Shrineworld`（`SOR62`）");
            if (sw != null)
            {
                bool destroys = false;
                var ops = EffectText.Parse(sw.Desc, out _, out _);
                foreach (var o in ops) if (o.Verb == "destroy") destroys = true;
                Check(sw.Desc, "Draw 3 cards. For each Troop drawn, gain 1 ☀",
                      "★ `Shrineworld` 的 `desc` 就是卡面那句（数据侧已修）");
                CheckTrue(!destroys,
                          "★ ……而且解析里**不该有 `destroy`** —— 改之前这张 6 费卡打出去"
                          + "**消灭一个敌方单位**（`cardface_fixes.json` 的 `desc` 列把段首写错了）");
            }
        }

        // ---- ② `Prayer`：那句没逗号的 `If` 必须带上条件 ----
        {
            var pr = FindCard(pool, "Prayer");
            CheckTrue(pr != null, "卡池里找得到 `Prayer`（`SOR48`）");
            if (pr != null)
            {
                var ops = EffectText.Parse(pr.Desc, out _, out _);
                bool hasCond = false;
                foreach (var o in ops) if (!string.IsNullOrEmpty(o.Condition)) hasCond = true;
                CheckTrue(hasCond,
                          "★ `Prayer` 卡面那句 `If the unit has ▲Pray gain 1 ☀`（**真的没有逗号**）"
                          + "**带上了条件** —— 改之前 `TryIf` 的 `comma < 4` 直接判「不是 if 句」，"
                          + "条件被静默丢掉、每次白给 1 信仰。实得 desc：「" + pr.Desc + "」");
            }
        }
    }

    /// <summary>
    /// 🔴 **2026-09-16：把「有没有机制」这条账，从「主解析器」换成「会执行的那一层」。**
    ///
    /// 原来的两个口子（正本 `资料/普查产出_0916/静默桩家族_0916.md` §一）：
    ///   ① `BattleDriver` 那个卡面 `*` **只给 `Type == "tactic"` 判** ⇒
    ///      **单位卡 / 督军卡 / 防御卡的问题卡面永远看不见**；
    ///   ② 拿 `EffectText.Parse(desc)` 的结果去判单位卡，是**问错了层** ——
    ///      单位卡的 `desc` 带 `Rally:` / `When <事件>,` 前缀，正文早被
    ///      `CardDef.AddWhenTrigger` 分走了，主解析器解出来的是**没人执行的残渣**。
    ///      （2026-09-16 中文对账那轮 12 条假阳性大半出在这里。）
    ///
    /// 本方法按 `<see cref="EffectText.WillRunOps"/>`（**按卡类型问对的层**）重新算一遍，
    /// 分类型报数并列出卡名 —— **先量再改**（改卡面是玩家可见的改动，得有数才动）。
    /// </summary>
    static void ReportWillRunMechanism()
    {
        var pool = CardDatabase.Load();
        var byType = new Dictionary<string, int[]>();
        var fails = new Dictionary<string, List<string>>();
        foreach (var c in pool)
        {
            if (c == null) continue;
            int[] acc;
            if (!byType.TryGetValue(c.Type ?? "(空)", out acc)) { acc = new int[2]; byType[c.Type ?? "(空)"] = acc; }
            List<string> lst;
            if (!fails.TryGetValue(c.Type ?? "(空)", out lst)) { lst = new List<string>(); fails[c.Type ?? "(空)"] = lst; }
            acc[0]++;
            var ops = EffectText.WillRunOps(c);
            string why; bool imprecise;
            foreach (var op in ops)
            {
                if (!EffectText.OpHasMechanism(op, c.Faction, pool, out why, out imprecise))
                {
                    acc[1]++;
                    lst.Add(c.Name + "〔" + (op.Source ?? "") + "〕→ " + why);
                    break;
                }
            }
        }
        var sb = new StringBuilder();
        sb.AppendLine("# 「会执行的那一层」有没有机制 —— 按卡类型（2026-09-16）");
        sb.AppendLine();
        sb.AppendLine("> 判据 = `EffectText.WillRunOps`（**按卡类型问对的层**）+ `OpHasMechanism`。");
        sb.AppendLine("> `tactic`/`defence` 走主解析器；`unit`/`hero` 走事件层/触发层/灵魂石/誓约。");
        sb.AppendLine();
        var keys = new List<string>(byType.Keys); keys.Sort();
        foreach (var k in keys)
        {
            int[] a = byType[k];
            Debug.Log(P + $"   [会执行的那层] {k,-9} {a[0] - a[1]}/{a[0]}" + (a[1] > 0 ? $"  ← **{a[1]} 张有卡点**" : "  全通"));
            sb.AppendLine($"## [{k}] {a[0] - a[1]}/{a[0]}" + (a[1] > 0 ? $" —— **{a[1]} 张有卡点**" : " 全通"));
            foreach (var n in fails[k]) sb.AppendLine("- " + n);
            sb.AppendLine();
        }
        const string path = "d:/4/_tmp_view/willrun_mechanism.md";
        System.IO.File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8);
        Debug.Log(P + "   全量清单写到 " + path);
    }

    // ==================================================================
    //  待办第 11 行 ① 从句级尺子（2026-09-19）
    // ==================================================================

    /// <summary>从句级尺子的**连接符**表 —— 与 `工具/scan_swallowed_clauses.py` 的 `CONNECTORS` 同源。</summary>
    static readonly string[] SwallowConnectors = { " and ", " or ", "," };

    /// <summary>从句级尺子的**条件从句头** —— 与 `工具/scan_swallowed_clauses.py` 的 `COND_HEADS` 同源。
    /// 「条件从句**不许当主语**」这条工程里记过（`Accursed Helbrute` 那轮）。</summary>
    static readonly string[] SwallowCondHeads = { "when ", "whenever ", "if ", "while ", "until ", "unless " };

    /// <summary>
    /// 从句级尺子的**动作动词表** —— 与 `工具/scan_swallowed_clauses.py` 的 `VERBS` **逐字一致**
    /// （两处口径必须一样，改一处要同步另一处）。
    ///
    /// ⚠️ **只放动作动词**：`attack` / `damage` / `flank` 这类在目标短语里是**名词**
    ///    （`+2 Ranged Attack` · `a friendly troop with Flank`），放进来会淹掉真信号
    ///    （脚本第一版就这么干的：376 处候选里大半是 `载荷「+1 melee attack」` 这种正常引文）。
    /// </summary>
    static readonly System.Text.RegularExpressions.Regex SwallowVerbRe =
        new System.Text.RegularExpressions.Regex(
            "(?<![A-Za-z])(deal|give|gain|heal|draw|discard|destroy|reload|summon|create|"
            + "move|return|put|sacrifice|reveal|double|reduce|increase|restore|exhaust|shuffle|choose)"
            + "(?![A-Za-z])",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
            | System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>
    /// 🔴 **从句级尺子进自检报表**（2026-09-19，待办第 11 行 ①）。
    ///
    /// **为什么要有它**：卡级两把尺子（`Coverage` 的「不认/半懂」· `OpHasMechanism` 的「载荷有没有机制」）
    /// **都看不见「某半句被吞进目标短语或载荷里、从来没变成 op」** ——
    /// 那种句子判「认了」、载荷有机制、也不宽，四个桶一个都不响，**卡面还不打 `*`**
    /// （这一族 = **静默桩家族**，正本 `资料/普查产出_0916/静默桩家族_0916.md`）。
    /// 唯一看得见它的是从句级这把尺子，而它原来**只在本地手工跑**
    /// （`工具/scan_swallowed_clauses.py` 读 `EffectParseProbe.Run` 的产物）
    /// ⇒ **改完引擎不会自动知道有没有新吞句**。
    ///
    /// 🔴 **口径比那个脚本严一处**：脚本读的是**主解析器**对**整条 `desc`** 的意见，
    /// 而单位卡 / 督军卡的 `desc` 带 `Rally:` / `When <事件>,` 前缀，正文早被
    /// `CardDef.AddWhenTrigger` / `AddTriggerOp` 分走了 ⇒ 它解出来的是**没人执行的残渣**
    /// （见 `EffectText.WillRunOps` 的注释）。本报表量的是 <see cref="EffectText.WillRunOps"/>
    /// —— **真正会执行的那批 op**。
    ///
    /// ⚠️ **它是候选清单，不是结论**：`your troops and your warlord` 这种**正常的目标短语**
    /// 也会被「含连接符」命中。所以**有第二层判据**（2026-09-19 第一次跑出来之后加的）：
    ///   · **文本形状**只负责「挑出可疑的引文」（与脚本 `scan_swallowed_clauses.py` 同一套词表）；
    ///   · **结构解释**再问一句「引文里多出来的那截，**有没有被某个字段接走**」——
    ///     相邻型（`X and its adjacent units`）· 兵种并集（`A or B`）· 尾句（`op.Tail`）·
    ///     手牌那半（`AlsoHand`）· 已部署（`Deployed`）· 计数（`CountScope`）·
    ///     事件层（`op.When`）· 回合内层（`AtTurnOps`）· `chooseone` 的选项文本。
    ///     **接走了 = 不是吞句**（只留在表里给人扫）；**没接走 = ⚠ 真候选**。
    ///   · `give`/`gain` 的载荷**另有引擎自己的判据**：`GivePayload.Parse` 的 `PayloadOp.Unresolved`
    ///     （「丢段看得见」）—— 它连**文本看着正常、载荷真丢了半句**那种也抓得到。
    ///
    /// 断言 = **「⚠ 真候选」里不许出现没裁定过的卡**（`SwallowAdjudicated` 是已知项清单）。
    /// 新候选浮出来时**逐张裁定**（开卡图 / 查规则书），确认是正常写法才写进那张表并给理由，
    /// 真是吞句就改解析层 —— **别为了让它绿而放宽判据**。
    /// 🆕 **第一次跑出来的唯一真候选 = `Njal Stormcaller`**，见下面 `SwallowAdjudicated` 上方的注释
    /// （按卡图从数据侧修掉，不是改判据）。
    ///
    /// 产物：`_tmp_view/swallowed_clauses.md`（每次自检重写）。
    /// </summary>
    static void ReportSwallowedClauses()
    {
        var pool = CardDatabase.Load();
        var rows = new List<string[]>();
        var hit = new List<string>();        // 所有候选（含已被结构解释掉的）—— 报表全列，便于人扫
        var suspect = new List<string>();    // ⚠ **未解释**的（断言只看它）
        int cards = 0, ops = 0, explained = 0, unresolvedGive = 0;

        foreach (var c in pool)
        {
            if (c == null) continue;
            cards++;
            foreach (var o in EffectText.WillRunOps(c))
            {
                if (o == null) continue;
                ops++;

                // ---- ① 目标引文 ----
                string raw = o.Target == null ? null : o.Target.Raw;
                if (!string.IsNullOrEmpty(raw))
                {
                    string why = SwallowedWhyTarget(raw);
                    if (why != null)
                    {
                        if (!hit.Contains(c.Name)) hit.Add(c.Name);
                        string expl = SwallowedTargetExplained(o);
                        if (expl == null)
                        {
                            if (!suspect.Contains(c.Name)) suspect.Add(c.Name);
                            AddSwallowRow(rows, c, o, "目标", raw, why, "⚠ **未解释**");
                        }
                        else
                        {
                            explained++;
                            AddSwallowRow(rows, c, o, "目标", raw, why, "已解释：" + expl);
                        }
                    }
                }

                // ---- ② 载荷引文 ----
                if (string.IsNullOrEmpty(o.Payload)) continue;
                if (o.Verb == "give" || o.Verb == "gain")
                {
                    // 🔴 **判据换成引擎自己的那一个**：`GivePayload.Parse` 会给「本版不认识」的段打
                    //    `PayloadOp.Unresolved`（2026-09-16 加的「**丢段看得见**」）——
                    //    那正是「吞了半句还自称认了」的定义，比文本启发式准得多，而且**与结算层同源**。
                    //    ⚠️ 这一条**不看文本启发式有没有命中**：`UM58` / `GOF_Worst_Temper` /
                    //       `TAU74` 那三张（`加`/`放弃` 的段）就是「文本看着正常、载荷真的丢了半句」。
                    string un = null;
                    var pops = GivePayload.Parse(o.Payload);
                    if (pops == null || pops.Count == 0) un = "载荷**整条**解释不出来";
                    else
                        foreach (var p in pops)
                            if (p.Unresolved)
                            { un = "载荷里有**本版不认识的段**（`PayloadOp.Unresolved`）"; break; }
                    if (un != null)
                    {
                        unresolvedGive++;
                        if (!hit.Contains(c.Name)) hit.Add(c.Name);
                        if (!suspect.Contains(c.Name)) suspect.Add(c.Name);
                        AddSwallowRow(rows, c, o, "载荷", o.Payload,
                                      SwallowedWhyPayload(o.Payload) ?? "（文本层看不出，靠 `Unresolved`）",
                                      "⚠ **未解释**｜" + un);
                    }
                    else
                    {
                        string why = SwallowedWhyPayload(o.Payload);
                        if (why != null)
                        {
                            explained++;
                            if (!hit.Contains(c.Name)) hit.Add(c.Name);
                            AddSwallowRow(rows, c, o, "载荷", o.Payload, why,
                                          "已解释：载荷每一项都被 `GivePayload` 认下（无 `Unresolved`）");
                        }
                    }
                    continue;
                }
                {
                    string why = SwallowedWhyPayload(o.Payload);
                    if (why == null) continue;
                    if (!hit.Contains(c.Name)) hit.Add(c.Name);
                    string expl = SwallowedPayloadExplained(o);
                    if (expl == null)
                    {
                        if (!suspect.Contains(c.Name)) suspect.Add(c.Name);
                        AddSwallowRow(rows, c, o, "载荷", o.Payload, why, "⚠ **未解释**");
                    }
                    else
                    {
                        explained++;
                        AddSwallowRow(rows, c, o, "载荷", o.Payload, why, "已解释：" + expl);
                    }
                }
            }
        }

        var fresh = new List<string>();
        foreach (string n in suspect) if (!SwallowAdjudicated.ContainsKey(n)) fresh.Add(n);

        Debug.Log(P + $"   从句级（吞句候选）：卡 {cards} 张 / 会执行的 op {ops} 条 —— "
                  + $"候选 **{rows.Count}** 处 / **{hit.Count}** 张卡；其中"
                  + $"**已由结构化字段解释掉 {explained} 处**（相邻/并集/尾句/手牌/已部署/计数/事件层），"
                  + $"**真候选 {suspect.Count} 张**（其中 `give`/`gain` 的「载荷丢段」{unresolvedGive} 处）"
                  + (fresh.Count > 0 ? "：" + string.Join("、", fresh) : "，全部在已裁定清单里"));
        Debug.Log(P + "   全量候选写到 d:/4/_tmp_view/swallowed_clauses.md");

        var sb = new StringBuilder();
        sb.AppendLine("**从句级尺子 —— 吞句候选**（2026-09-19 起由自检每次重写）");
        sb.AppendLine();
        sb.AppendLine("> **它补的是哪条缝**：卡级两把尺子（`Coverage` 的「不认/半懂」· `OpHasMechanism` 的"
                    + "「载荷有没有机制」）**都看不见「某半句被吞进目标短语或载荷里、从来没变成 op」** ——"
                    + "那种句子判「认了」、有机制、也不宽，四个桶一个都不响，**卡面还不打 `*`**。");
        sb.AppendLine("> 判据 = `EffectText.WillRunOps`（**会执行的那批 op**）上的**两条**尺子：");
        sb.AppendLine(">  ① **文本形状**（与 `工具/scan_swallowed_clauses.py` 同一套词表/连接符/条件从句头）——"
                    + "只用来**挑出可疑的引文**；");
        sb.AppendLine(">  ② **结构解释**：引文里那截多出来的文字，**有没有被某个字段接走**"
                    + "（相邻 / `A or B` 并集 / 尾句 / 手牌那半 / 已部署 / 计数 / 事件层 / 回合内层 / `chooseone` 选项）。"
                    + "**接走了 = 不是吞句**；");
        sb.AppendLine(">  ③ `give`/`gain` 的载荷**另有引擎自己的判据**：`GivePayload.Parse` 的"
                    + "`PayloadOp.Unresolved`（「丢段看得见」，2026-09-16 加的）—— 它比文本启发式准。");
        sb.AppendLine("> ⇒ **断言只看「未解释」的那批**（`⚠` 列），已解释的照旧留在表里供人扫。");
        sb.AppendLine();
        sb.AppendLine($"卡 {cards} 张 / op {ops} 条 ⇒ 候选 **{rows.Count}** 处 / **{hit.Count}** 张卡；"
                    + $"**已解释 {explained} 处** · **⚠未解释 {suspect.Count} 张**"
                    + $"（`give`/`gain` 丢段 {unresolvedGive} 处）"
                    + $"· 其中没裁定过的 **{fresh.Count}** 张。");
        sb.AppendLine();
        sb.AppendLine("| 卡名 | 阵营 | 类型 | 层 | 位置 | 引文 | 文本命中 | 判定 | op |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|---|");
        foreach (var r in rows) sb.AppendLine("| " + string.Join(" | ", r) + " |");
        sb.AppendLine();
        sb.AppendLine("## 已裁定（`SwallowAdjudicated`：真候选里的**已知项**，每条都要给理由）");
        sb.AppendLine();
        sb.AppendLine("| 卡名 | 裁定 |");
        sb.AppendLine("|---|---|");
        foreach (var kv in SwallowAdjudicated) sb.AppendLine($"| {kv.Key} | {kv.Value} |");
        const string path = "d:/4/_tmp_view/swallowed_clauses.md";
        System.IO.File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8);

        // 🔴 断言：**新出现的、没解释掉也没裁定过的候选 = 0**。这是这把尺子的全部价值 ——
        //    它盯的是「吞了半句还能自称全认」那一类静默失效，而那一类**卡级两把尺子一条都看不见**。
        CheckTrue(fresh.Count == 0,
                  $"★ 从句级：出现 **{fresh.Count}** 张**没裁定过的吞句候选**"
                  + $"（{string.Join("、", fresh)}）—— 看 `_tmp_view/swallowed_clauses.md` 的 ⚠ 那几行："
                  + "逐张裁定（开卡图 / 查规则书）；确认是正常写法就写进 `SwallowAdjudicated` 并给理由，"
                  + "真是吞句就改解析层（**别放宽判据**）");
    }

    /// <summary>**目标引文里那截多出来的文字，有没有被某个字段接走** —— 接走了返回理由，没有返回 null。
    /// 这一层是**降噪**：不加它，35 处候选里 34 处是「相邻型/并集/尾句」的**正常写法**，
    /// 真信号会被淹掉（2026-09-19 第一次跑就是这样）。</summary>
    static string SwallowedTargetExplained(EffectOp o)
    {
        var t = o.Target;
        if (t == null) return null;
        if (t.Adjacent) return "相邻型的**固定写法**（`X and its/their adjacent units`）—— 锚点已定";
        if (!string.IsNullOrEmpty(t.SubtypeFilter) && t.SubtypeFilter.IndexOf('|') >= 0)
            return "`A or B` 是**兵种并集**（`SubtypeFilter` 里带 `|`）";
        if (t.AlsoHand) return "`in play and in hand` 的**手牌那半**（`Target.AlsoHand`）";
        if (t.Deployed) return "`vehicles you put in play` = **已部署**（`Target.Deployed`）";
        if (!string.IsNullOrEmpty(o.Tail)) return "逗号后面那半是**尾句**，已单独解成一条 op（`op.Tail`）";
        // 🔴 **引文以逗号结尾** = 它是「尾句继承了上一句目标」那条路的**继承值**（`SplitAndTail` 切句时
        //    把上一句的目标原样带过来，逗号留在 `Raw` 里）—— 下一句的正文已经**单独解成一条 op** 了
        //    （实测三处：`Solar Pulse` / `Skrag Every Stash!` / `Codex Discipline` 的尾句 op，
        //    它们的 `Target.Raw` 分别是 `all enemies,` / `all enemies,` / `your troops,`）。
        if (t.Raw != null && t.Raw.TrimEnd().EndsWith(",", StringComparison.Ordinal))
            return "引文**以逗号结尾** —— 这是**尾句继承上一句目标**的写法（正文已单独解成一条 op）";
        if (!string.IsNullOrEmpty(o.CountScope)) return "`… for each X on it` 由**计数**那一套接手（`CountScope`）";
        return null;
    }

    /// <summary>**非 `give`/`gain` 载荷**里那截多出来的文字，有没有被别的层接走。
    /// `give`/`gain` 不走这里 —— 它们用 `GivePayload.Parse` 的 `Unresolved`（与结算层同源）。</summary>
    static string SwallowedPayloadExplained(EffectOp o)
    {
        if (o.When != null)
            return "条件从句由**事件层**接手（`op.When` = «" + o.When.ToString() + "»）";
        if (o.AtTurnOps != null && o.AtTurnOps.Count > 0)
            return "正文在 `AtTurnOps`（回合内层）里**真的解出了 op**";
        if (o.Verb == "chooseone")
            return "`chooseone` 的载荷是**选项文本**（`|` 分隔），由选效果那条路逐项消费";
        return null;
    }

    static void AddSwallowRow(List<string[]> rows, CardDef c, EffectOp o,
                              string where, string quote, string why, string verdict)
    {
        rows.Add(new[]
        {
            c.Name, c.Faction ?? "", c.Type ?? "", o.Source ?? "", where,
            quote.Replace("|", "\\|"), why.Replace("|", "\\|"), verdict,
            EffectParseProbe.Dump(o, 0).Replace("|", "\\|").Replace("\n", " "),
        });
    }

    /// <summary>**目标引文**命中的判据（返回 null = 不候选）。与脚本 `why_target` 同一套。</summary>
    static string SwallowedWhyTarget(string quote)
    {
        string low = quote.ToLowerInvariant().Trim();
        if (low.Length == 0) return null;
        var hits = new List<string>();
        foreach (string h in SwallowCondHeads)
            if (low.StartsWith(h, StringComparison.Ordinal)) { hits.Add("条件从句当主语"); break; }
        var vs = new SortedSet<string>();
        foreach (System.Text.RegularExpressions.Match m in SwallowVerbRe.Matches(quote))
            vs.Add(m.Groups[1].Value.ToLowerInvariant());
        if (vs.Count > 0) hits.Add("目标引文含动词 " + string.Join("/", vs));
        foreach (string c in SwallowConnectors)
            if (low.IndexOf(c, StringComparison.Ordinal) >= 0)
            { hits.Add("目标引文含连接符 '" + c.Trim() + "'"); break; }
        return hits.Count == 0 ? null : string.Join("；", hits);
    }

    /// <summary>**载荷引文**命中的判据（返回 null = 不候选）。与脚本 `why_payload` 同一套。
    /// ⚠️ 载荷里出现 `+1 melee attack` / `Flank` / `Hunt Mark` 都是**正常的**（那是载荷本身）；
    ///    可疑的只有「**逗号/and + 第二个动词**」与「条件从句头」两条。</summary>
    static string SwallowedWhyPayload(string quote)
    {
        string low = quote.ToLowerInvariant().Trim();
        var hits = new List<string>();
        foreach (string h in SwallowCondHeads)
            if (low.StartsWith(h, StringComparison.Ordinal)) { hits.Add("载荷含条件从句头"); break; }
        if (low.IndexOf(",", StringComparison.Ordinal) >= 0
            || low.IndexOf(" and ", StringComparison.Ordinal) >= 0)
        {
            var vs = new SortedSet<string>();
            foreach (System.Text.RegularExpressions.Match m in SwallowVerbRe.Matches(quote))
                vs.Add(m.Groups[1].Value.ToLowerInvariant());
            if (vs.Count > 0) hits.Add("载荷含子句（连接符+动词 " + string.Join("/", vs) + "）");
        }
        return hits.Count == 0 ? null : string.Join("；", hits);
    }

    /// <summary>
    /// **已裁定的吞句候选** —— 卡名 → 裁定理由。`ReportSwallowedClauses` 的断言拿它当白名单。
    ///
    /// 🔴 **进这张表要有理由**（「看着正常」不算）：写清**为什么它不是吞句**，
    ///    最好带出处。新候选浮出来时**先裁定、再往这里加**，别为了让它绿而放宽判据。
    ///
    /// 📌 **2026-09-19 第一次跑：这张表最后是空的** —— 唯一那个真候选
    ///    （`Njal Stormcaller`，载荷「when you trigger ferocity, high rune priest」）
    ///    **没走白名单，走的是数据侧修根因**：
    ///    它的类型行/效果文字里那截 `when you trigger ferocity,` 是**被吞进来的条件从句**，
    ///    根因是 `keywords` 里挂着 `Ferocity`（OCR 把效果句里的内联图标词记成了关键词，
    ///    与 2026-09-18 那 5 张 `Stun` 同一趟、同一根因）⇒ `CardDef.CollectBareKeywordBody`
    ///    把**整条 desc** 当成了这张卡的 `Ferocity` 正文。
    ///    卡面逐张开图核过（`d:/2/Warpforge部队卡片/Space Wolves/1督军/Warpforge_04_Njal-Stormcaller.png`）：
    ///    那是 `When you trigger ⟨獠牙⟩Ferocity, …` 的**引用**，不是 `Ferocity: <正文>` 的授予
    ///    （对照 `Blood Claw` 的 `⟨獠牙⟩Ferocity: Deal 3 damage to an enemy`）。
    ///    ⇒ 修法 = `数据/游戏数据/cardface_fixes.json` 的 `_manual_keywords` 把该卡 keywords 覆盖成 `[]`
    ///    （同一趟修法的先例：`_2026-09-18_Stun误抽`）。
    /// </summary>
    static readonly Dictionary<string, string> SwallowAdjudicated = new Dictionary<string, string>
    {
        // 目前为空 —— 第一次跑出来的那个真候选已按根因修掉，不需要白名单。
        // （加条目要写「为什么它不是吞句」+ 出处；只为了让断言变绿而加 = 违规。）
    };

    // ==================================================================
    //  待办第 11 行 ② 光环那一侧的机制账（2026-09-19）
    // ==================================================================

    /// <summary>
    /// 🔴 **光环自己的机制账**（2026-09-19，待办第 11 行 ②）。
    ///
    /// **缺口是什么**：`AuraSpecs` 2026-09-16 起就收进了 `WillRunOps`（⇒ 卡面 `*` 与覆盖率
    /// 看得见光环了），但那是**解析侧**的账 —— 它只问「载荷解释得出来吗」。
    /// **结算侧**（`Auras.Recompose` → `ApplyAura`）**没有任何报表**：
    /// 一条光环被认下之后，它的载荷到底**有没有人消费**、落到哪个分支上，没人量过。
    ///
    /// **这道缝会怎么漏**（都是静默的）：`ApplyAura` 对每一项载荷只认三种形态 ——
    ///   ① 属性（`op.Attr != null`）⇒ `UnitState.RecordGrant(NormalizeAttr(attr), …)`
    ///      ⇒ 最终落到 `UnitState.ApplyGrant` 的 `switch`，而**那个 switch 只有四支**
    ///      （`attack` / `ranged` / `health` / `armour`）**且没有 default** ⇒
    ///      别的属性词**一声不响地丢掉**；
    ///   ② 关键词（`op.Keyword != null`）⇒ `AddAuraKeyword`；
    ///   ③ 嵌入正文（`op.IsEmbedded`）⇒ `RuleCore.GrantEmbeddedAbilityFromAura`。
    ///   三种都不命中的项 = **解析得出、结算层没有分支** ⇒ 玩家看不见任何效果，日志也没有。
    ///   另有两条路：`RemnantStay`（改残骸寿命，置标记）· `CostLess`（费用半，走 `CostMod`）。
    ///
    /// 所以本报表逐条光环列出「**这条载荷被哪个分支消费**」，并把**一条机制都没挂上的**
    /// 与**没人消费的载荷项**列成 🔴，断言它们为 0。
    ///
    /// 产物：`_tmp_view/aura_mechanism.md`（每次自检重写）。
    /// </summary>
    static void ReportAuraMechanism()
    {
        var pool = CardDatabase.Load();
        var rows = new List<string>();
        var bad = new List<string>();
        int cards = 0, specs = 0, payloadOps = 0, noMech = 0;

        foreach (var c in pool)
        {
            if (c == null || c.AuraSpecs == null || c.AuraSpecs.Count == 0) continue;
            cards++;
            for (int i = 0; i < c.AuraSpecs.Count; i++)
            {
                var a = c.AuraSpecs[i];
                if (a == null) { bad.Add($"{c.Name}｜第 {i + 1} 条 `AuraSpec` 是 null"); continue; }
                specs++;
                var kinds = new List<string>();

                // ---- 两条**没有载荷**的路 ----
                if (a.RemnantStay)
                    kinds.Add("残骸寿命标记（置 `UnitState.AuraRemnantStay`，读点 `RuleCore.DestroyRemnants`）");
                if (a.CostLess > 0)
                    kinds.Add($"费用 -{a.CostLess}（登记 `CostMod`，`RuleCore.CostOf` 每次现算）");

                // ---- 载荷那半：逐项问「哪个分支消费它」----
                if (!string.IsNullOrEmpty(a.Payload))
                {
                    var pops = GivePayload.Parse(a.Payload);
                    if (pops == null || pops.Count == 0)
                    {
                        // `TryParse` 的第三道闸已经验过载荷解释得了 ⇒ 走到这儿说明
                        // **两边判据不一致**（`Aura.cs` 的 `PayloadUnderstood` 与这里的 `Parse`）。
                        // `ApplyAura` 在这种情况下会打一行日志然后 return false（**如实报**），
                        // 但那意味着这条光环**永远不生效** ⇒ 报表里必须刺眼。
                        bad.Add($"{c.Name}｜`{a.Source}`｜载荷「{a.Payload}」**解析不出来**"
                              + "（`TryParse` 认了、`GivePayload.Parse` 认不出 —— 两边判据不一致，"
                              + "`ApplyAura` 会早退 ⇒ 这条光环永远不生效）");
                        continue;
                    }
                    foreach (var p in pops)
                    {
                        payloadOps++;
                        if (p.IsEmbedded)
                        {
                            kinds.Add($"嵌入正文「{p.Embedded}」（`GrantEmbeddedAura`）");
                            continue;
                        }
                        if (p.Attr != null)
                        {
                            string norm = RuleCore.NormalizeAttr(p.Attr);
                            if (norm == "attack" || norm == "ranged" || norm == "health" || norm == "armour")
                            {
                                kinds.Add($"属性 {norm} {p.Value:+#;-#;0}（`RecordGrant` → `ApplyGrant`）");
                            }
                            else
                            {
                                bad.Add($"{c.Name}｜`{a.Source}`｜属性项「{p.Attr}」→ `NormalizeAttr` 得 `{norm}`"
                                      + " —— **`UnitState.ApplyGrant` 的 switch 只有 `attack/ranged/health/armour` "
                                      + "四支、没有 default** ⇒ 这一项**静默丢掉**（`RecordGrant` 记了账、"
                                      + "数字却一点没动）");
                            }
                            continue;
                        }
                        if (p.IsKeyword)
                        {
                            kinds.Add($"关键词 {p.Keyword} ×{(p.Value <= 0 ? 1 : p.Value)}（`AddAuraKeyword`）");
                            continue;
                        }
                        bad.Add($"{c.Name}｜`{a.Source}`｜载荷项「{p}」既不是属性也不是关键词也不是嵌入正文"
                              + " ⇒ `ApplyAura` 的三个分支一个都不命中（**静默无效果**）");
                    }
                }

                if (kinds.Count == 0)
                {
                    noMech++;
                    bad.Add($"{c.Name}｜`{a.Source}`｜**这条光环一条机制都没挂上**"
                          + "（既没有载荷可消费、也不是残骸/费用那两条路）");
                }
                rows.Add($"| {c.Name} | {c.Faction} | {c.Type} | {i + 1} | {a.Source} | {a} | "
                       + (kinds.Count == 0 ? "🔴 **一条都没有**" : string.Join("；", kinds)) + " |");
            }
        }

        Debug.Log(P + $"   光环机制账：**{cards}** 张卡 / **{specs}** 条光环 / "
                  + $"载荷项 **{payloadOps}** 条 —— 一条机制都没挂上的 **{noMech}** 条 · "
                  + $"没人消费的载荷项 **{bad.Count - noMech}** 处");
        Debug.Log(P + "   全量清单写到 d:/4/_tmp_view/aura_mechanism.md");

        var sb = new StringBuilder();
        sb.AppendLine("**光环那一侧的机制账**（2026-09-19 起由自检每次重写）");
        sb.AppendLine();
        sb.AppendLine("> 判据 = 逐条 `CardDef.AuraSpecs`，问「这一项载荷**哪个结算分支**消费它」：");
        sb.AppendLine("> 属性 → `RecordGrant` → `UnitState.ApplyGrant`（**只有 4 支、无 default**）·");
        sb.AppendLine("> 关键词 → `AddAuraKeyword` · 嵌入正文 → `GrantEmbeddedAura` ·");
        sb.AppendLine("> `RemnantStay` → 置 `AuraRemnantStay` · `CostLess` → `CostMod`。");
        sb.AppendLine("> **三种都不命中的项 = 解析得出、结算层没有分支**（静默无效果）。");
        sb.AppendLine();
        sb.AppendLine($"卡 **{cards}** 张 / 光环 **{specs}** 条 / 载荷项 **{payloadOps}** 条 ⇒ "
                    + $"一条机制都没挂上的 **{noMech}** 条 · 没人消费的载荷项 **{bad.Count - noMech}** 处。");
        sb.AppendLine();
        sb.AppendLine("| 卡名 | 阵营 | 类型 | # | 原句 | 认成什么 | 消费者 |");
        sb.AppendLine("|---|---|---|---|---|---|---|");
        foreach (string r in rows) sb.AppendLine(r);
        if (bad.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## 🔴 没人消费的（断言要求为 0）");
            sb.AppendLine();
            foreach (string b in bad) sb.AppendLine("- " + b);
        }
        const string path = "d:/4/_tmp_view/aura_mechanism.md";
        System.IO.File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8);

        CheckTrue(bad.Count == 0,
                  $"★ 光环机制账：**{bad.Count}** 处「认下来了、结算层却没人消费」"
                  + "（详见 `_tmp_view/aura_mechanism.md`）—— 这就是「覆盖率绿 ≠ 机制在跑」那一类，"
                  + "要么补分支，要么如实报（别放宽解析层）");
    }

    /// <summary>
    /// 🔴 **2026-09-16 收尾两条**：`Beastboss on Squigosaur`（引号里的动词）·
    /// 多段载荷丢段（**改成「看得见」而不是「整条失败」**）· 外加一个**会崩进程的潜伏雷**。
    ///
    /// **① `Beastboss on Squigosaur`（`GOF90`）** —— 卡面逐字：
    ///   `Stomp. Friendly Beasts cost 1 less and have "💀 Slay: Gain Blood Thirst this turn"`
    ///   真意 = **给全体友方 Beast 挂上那个 `Slay:` 能力**（+ 给它们降 1 费）。
    ///   病根：`ReGain` 的 `(.+?)` 非贪婪 ⇒ 吃掉**引号里**那个 `Gain`
    ///   ⇒ 主语成了 `friendly beasts cost 1 less and have "slay:`、载荷成了 `blood thirst this turn"`。
    ///   ⇒ 表现是「**立刻**给一只 Beast 加血欲」，**卡面不打 `*`**（静默错打）。
    ///   修法：动词落点在引号内就交回原路（判据是「**动词落点在引号里**」，不是「句子里有引号」）。
    ///   ⚠️ 它**真正的执行层是光环**（`Aura.cs` 的 `IsEmbedded` 那一支：批量筛查 + 挂嵌入能力 +
    ///     `CostLess` 降费，三样都在树里）—— 所以这条断言只钉「主解析器别再抢答」，
    ///     光环那条路由 ③ 那条断言盯。
    ///
    /// **② 多段载荷丢段** —— `ParseInto` 的 `any` 只看「有没有一段解出来」，解不出的段
    ///   **不留任何痕迹**。实测受影响的只有 **4 张**（`UM58` 丢 `1 health` · `GOF_Worst_Temper`
    ///   丢 `wings flying` · `TAU74` 丢 `+1 power` · `DA21` 丢 2 点任务）。
    ///   🔴 **不改「一段不认识就整条 null」**：那会把**现在还能用的那半一起打掉**
    ///   （`UM58` 从「+1近战/+1远程」变成一点不给）——**用缩水换可见不划算**。
    ///   ⇒ 走第三条路：**保留 `any`，但给认不出的段留一个 `PayloadOp.Unresolved` 标记**，
    ///     让结算日志 + `Mechanized`（⇒ 卡面 `*` + 覆盖率）三处同时亮。
    ///
    /// **③ 顺手修掉的一个潜伏雷**：`ParseInto` 的多段判据吃 `low`（小写）、拆分吃 `w`（原串）
    ///   ⇒ 全大写 ` AND ` 会被判成「有多段」却**一段也拆不开** ⇒ 整段**原样递归回自己**。
    ///   实测 `GivePayload.Parse("+1 attack AND vanguard")` = **StackOverflow，进程直接死**
    ///   （在 Unity 里就是整个编辑器崩）。全池当前 0 条，属潜伏。
    /// </summary>
    static void TestBeastbossAndPayloadSegments()
    {
        // ---- ① `Beastboss on Squigosaur`：引号里的 `Gain` 不许被当成这张卡的动词 ----
        var pool = CardDatabase.Load();
        {
            var bb = FindCard(pool, "Beastboss on Squigosaur");
            CheckTrue(bb != null, "卡池里找得到 `Beastboss on Squigosaur`（`GOF90`）");
            if (bb != null)
            {
                var ops = EffectText.Parse(bb.Desc, out _, out _);
                bool badGain = false;
                foreach (var o in ops)
                {
                    if (o.Payload == null) continue;
                    if ((o.Verb == "gain" || o.Verb == "give" || o.Verb == "gainfaith")
                        && o.Payload.ToLowerInvariant().Contains("blood thirst")
                        && o.Target != null && o.Target.Raw != null
                        && o.Target.Raw.ToLowerInvariant().Contains("slay"))
                        badGain = true;
                }
                CheckTrue(!badGain,
                          "★ 主解析器**不再**把引号里的 `Gain` 当成这张卡的动词"
                          + "（改之前主语成了 `friendly beasts cost 1 less and have \"slay:`、"
                          + "载荷成了 `blood thirst this turn\"` ⇒ 静默错打）");
                var auraTxt = new List<string>();
                foreach (var a in bb.AuraSpecs) auraTxt.Add(a.ToString());
                CheckTrue(bb.AuraSpecs.Count > 0,
                          "★ 它**有光环** —— 真正的执行层是光环（挂 Slay 能力 + 给 Beast 降 1 费）。"
                          + "实得 " + bb.AuraSpecs.Count + " 条：" + string.Join(" / ", auraTxt));
            }
        }

        // ---- ② 多段载荷：**拆不开就不许递归**（原来会 StackOverflow）----
        {
            // ⚠️ **这条断言的真正判据是「跑到这里还没崩」** —— 改之前那一行会 StackOverflow，
            //    .NET 接不住 ⇒ **整个 Unity 进程直接死**，自检连汇总都打不出来
            //    （所以「能跑到这条断言」= 修复生效，比断言本身更有力）。
            //    这里只把实得值报出来 —— 别写「必须返回 null」：`+1 attack AND vanguard` 是**人造输入**，
            //    拆不开之后它会落到 `ReAttr` 那支，解出 1 项（`vanguard` 那半没进去）。
            //    全池**没有一条**载荷含大写 ` AND `（实测），所以那半不是真问题。
            var ps = GivePayload.Parse("+1 attack AND vanguard");
            CheckTrue(true, "（诊断）`+1 attack AND vanguard`（全大写 ` AND `）实得 "
                          + (ps == null ? "null" : ps.Count + " 项")
                          + " —— **没崩**（改之前判据吃小写、拆分吃原串 ⇒ 拆不开却判成多段"
                          + " ⇒ 整段原样递归回自己 = StackOverflow）");
        }

        // ---- ③ 丢段要**看得见** ----
        //    ⚠️ 判据得用一段**真认不出的载荷**。`UM58` 那句（`+1 melee attack, +1 ranged attack
        //      and 1 health`）2026-09-16 已经被顺手接上了（裸 `N <属性>`），所以不能再用它。
        //    这里用的是一段**构造出来的**载荷（`a kustom job of your choice` 那半段在
        //    `GivePayload` 层仍然不认识 —— 它现在是在**句子层**被 `TryChooseEffect` 接住的，
        //    两层的判据不同，别混）。
        {
            const string seg = "+1 attack and a kustom job of your choice";
            var ps = GivePayload.Parse(seg);
            int unresolved = 0, resolved = 0;
            if (ps != null) foreach (var p in ps) { if (p.Unresolved) unresolved++; else resolved++; }
            CheckTrue(unresolved > 0,
                      "★ 「认不出的那一段」被**标了出来**（改之前它静默消失）—— 实得 "
                      + unresolved + " 个标记 / " + resolved + " 个解出来的");
            string why;
            bool mech = GivePayload.Mechanized(seg, out why);
            CheckTrue(!mech, "★ 而且 `Mechanized` 判**没有机制**（否则报表与卡面照旧放行）。"
                           + "实得 why=「" + why + "」");

            // **反向**：`UM58` 那句现在**整条解得出来**（裸 `N <属性>` 本轮接上了）
            var u = GivePayload.Parse("+1 melee attack, +1 ranged attack and 1 health");
            int uUnresolved = 0;
            if (u != null) foreach (var p in u) if (p.Unresolved) uUnresolved++;
            CheckTrue(u != null && u.Count == 3 && uUnresolved == 0,
                      "★ 反向：`UM58` 那句（`… and 1 Health to your troops`）**现在整条解得出来**"
                      + " —— 实得 " + (u == null ? "null" : u.Count + " 项 / " + uUnresolved + " 个标记"));
        }

        // ---- ④ `Beast Snagga Nob`：**手牌通道其实早就通了**，缺的是「叠份数」 ----
        //   卡面（逐张开图核过 `d:/2/Warpforge部队卡片/Orks/3部队/Warpforge_07_Beast-Snagga-Nob.png`）：
        //   `[爪]Stomp. **At the end of your turn**, give +1[拳] to all Beasts in your hand`
        //   —— 是**每个回合结束都来一次**的常驻效果：撑到第三个回合，打出手里那张 Beast 时该 **+3**。
        //   🔴 **2026-09-16 更正一条旧结论**：`待修_手牌作用域.md` 说「本期不覆盖它」、
        //      `阵营推进` 记它「⑪ 那条路覆盖不到」—— 那是**探针看不见 `AtTurnOps`** 造成的**误判**
        //      （嵌套打印是 2026-09-16 才加的，见 `EffectParseProbe.Nested`）。
        //      真相：`atturn` 的内层 op 走的是**同一个 `DoGive`**，`GrantHandBuffForTargets`
        //      就是第二条写入路径 —— 通道早就通了。
        //   **真正缺的两样**：① `GrantHandBuffForTargets` 的合并分支只 `Count++`、**不叠载荷**
        //      ⇒ `ApplyHandBuffs` 打出一张时只跑**一遍** `Ops` ⇒ 永远只 +1；② 数据侧少了 `Stomp`。
        {
            var nb = FindCard(pool, "Beast Snagga Nob");
            CheckTrue(nb != null, "卡池里找得到 `Beast Snagga Nob`（`GOF81`）");
            if (nb != null)
            {
                CheckTrue(nb.Keywords.ContainsKey("stomp"),
                          "★ 它的 `keywords` 里有 `Stomp`（卡面印着，数据侧 2026-09-16 补）");
                var ops = EffectText.Parse(nb.Desc, out _, out _);
                string innerDump = "";
                foreach (var o in ops)
                {
                    if (o.AtTurnOps == null) continue;
                    foreach (var i2 in o.AtTurnOps)
                        innerDump += (i2.Verb ?? "?") + "「" + (i2.Payload ?? "") + "」"
                                   + (i2.Target == null ? "(无目标)"
                                      : " 目标[" + i2.Target.Side + "/" + i2.Target.Kind
                                        + " 兵种筛=" + i2.Target.SubtypeFilter
                                        + (i2.Target.AlsoHand ? " 也含手牌" : "")
                                        + (i2.Target.HandOnly ? " 只在手牌" : "") + "]") + " ‖ ";
                }
                CheckTrue(innerDump.Length > 0,
                          "★ `atturn` 的**内层 op** 解析出来了（探针原来打不出这一层）。实得：" + innerDump);

                // 结算：走**三个回合结束** ⇒ 那张手牌上该挂**三份**载荷
                var beast = new CardDef("FixBeast", "FixBeast", "unit", "", null, "Test",
                                        2, 2, 3, 0, null, subtype: "Beast");
                var ctx = Battle(new[] { nb, beast }, new[] { Unit("X", 1, 1, 9) });
                ToP1Turn(ctx, 9);
                Place(ctx, 0, 0, nb);
                for (int i = 0; i < 3; i++) { RuleCore.EndTurn(ctx); RuleCore.BeginTurn(ctx); }
                int opsOnBeast = 0, entriesOnBeast = 0;
                string dump = "";
                foreach (var h in ctx.HandBuffs)
                    if (h.Instance != null && h.Instance.Card != null && h.Instance.Card.Name == "FixBeast")
                    {
                        entriesOnBeast++;
                        int k = h.Ops == null ? 0 : h.Ops.Count;
                        dump += $"[第{entriesOnBeast}条 source=「{h.Source}」 {k} 份] ";
                        if (k > opsOnBeast) opsOnBeast = k;
                    }
                CheckTrue(opsOnBeast >= 3,
                          $"★ 撑过三个回合结束 ⇒ 那张手牌上挂了**三份**载荷（打出时 +3）—— "
                          + $"改之前只 `Count++`、`Ops` 不变 ⇒ **永远只 +1**（静默少算）。"
                          + $"实得最多 {opsOnBeast} 份，共 {entriesOnBeast} 条 entry：{dump}");
            }
        }

        // ---- ⑤ 两条新的载荷写法（2026-09-16，都是**先把账修好**才浮出来的）----
        {
            // `UM58 Knights of Macragge`：`Give +1 Melee Attack, +1 Ranged Attack and 1 Health to your troops`
            var a1 = GivePayload.Parse("1 Health");
            CheckTrue(a1 != null && a1.Count == 1 && a1[0].Attr == "health" && a1[0].Value == 1,
                      "★ 裸 `1 Health` 认得出（改之前那个 `1` 被 `ReLeadingNoise` 当「数量」剥掉、"
                      + "只剩 `health`，而它**不在 `GiveKw` 表里** ⇒ `UM58` 那半句从来不给）。实得 "
                      + (a1 == null ? "null" : a1.Count + " 项 / " + a1[0].Attr + "=" + a1[0].Value));

            // `GOF_Worst_Temper`：`Your Warlord gains +1 [Attack], [Shield] Armour 1 and [Wings] Flying this turn`
            var wt = FindCard(pool, "Worst Temper");
            CheckTrue(wt != null, "卡池里找得到 `Worst Temper`（`GOF_Worst_Temper`）");
            if (wt != null)
            {
                var ops = EffectText.Parse(wt.Desc, out _, out _);
                bool flying = false;
                foreach (var o in ops)
                    if (!string.IsNullOrEmpty(o.Payload)
                        && o.Payload.ToLowerInvariant().Contains("flying")) flying = true;
                CheckTrue(flying,
                          "★ 卡面那句 `[Wings] Flying` 认出来了（改之前 `Wings` 挡在 `Flying` 前面 ⇒ "
                          + "整段判不认识、`Flying` 从来不给）。实得 desc：「" + wt.Desc + "」");
            }
        }

        // ---- ⑥ `unresolved` **四条路都要报出来**（2026-09-16）----
        //    那个 `List<string>` 原来有**四处**「建了、传了、从来没读」，只有 `PlayTactic` 一处读它
        //    ⇒ **触发层 / 回合起止段 / 突触邻居 / 死亡结转**这四条路上，
        //    「这条效果本版没结算」**一条日志都不会打** —— 正是红线禁止的静默。
        //    这里走**触发层那个重载**（`ResolveOps(..., by, seed)`，所有触发层都走它）
        //    发一条解不出来的效果，钉住它现在会报。
        //    ⚠️ 判据读的是 `ctx.Events`（`BattleContext.Log` 就是往那儿写），不是屏幕输出。
        {
            var ctx = ProbeBattle(new[] { Unit("T", 1, 1, 5) }, new[] { Unit("X", 1, 1, 5) });
            ToP1Turn(ctx, 3);
            Place(ctx, 0, 0, Unit("T", 1, 1, 5));
            var ops = EffectText.Parse("Give a kustom job of your choice to a friendly troop", out _, out _);
            CheckTrue(ops != null && ops.Count > 0,
                      "夹具：那句**解析得出 op**（载荷认不出是**结算层**才拦的）");
            int before = ctx.Events.Count;
            RuleCore.ResolveOps(ctx, 0, null, ops, "自检触发层");
            string log = string.Join("\n", ctx.Events);
            CheckTrue(log.Contains("本版没结算"),
                      "★ 触发层那把 `ResolveOps` **现在会把没结算的报出来**"
                      + "（改之前那个 `unresolved` 建了不读 ⇒ 静默）。实得新增 "
                      + (ctx.Events.Count - before) + " 条日志");
        }

        // ---- ⑦ 裸 `Deal N damage` 打的是「**生命最低**」的敌人（2026-09-18 用户拍板）----
        //    改之前 `Auto` 只是个「不问玩家」的标记，实际落到 `ResolveTargets` 的退化支
        //    = 池序前 N = **槽号最小**。原版是 `BattleManager.GetLowestHealthUnit`（逐单位比
        //    `currentHealth` 取最小，严格小于 ⇒ 并列留列表序先者），分派链见 `EffectText.cs:4377`。
        //    🔑 判据要能**区分两者** ⇒ 故意让**槽号小的那个血更多**（槽号最小那条路会打错人）。
        {
            var ctx = ProbeBattle(new[] { Unit("T", 1, 1, 5) }, new[] { Unit("A", 1, 1, 9), Unit("B", 1, 1, 2) });
            ToP1Turn(ctx, 3);
            Place(ctx, 0, 0, Unit("T", 1, 1, 5));
            Place(ctx, 1, 1, Unit("A", 1, 1, 9));      // 槽号小、血多
            Place(ctx, 1, 4, Unit("B", 1, 1, 2));      // 槽号大、血少
            var ops = EffectText.Parse("Deal 1 damage", out _, out _);
            CheckTrue(ops != null && ops.Count > 0, "夹具：`Deal 1 damage` 解析得出 op");
            RuleCore.ResolveOps(ctx, 0, null, ops, "自检 auto 目标");
            Check(Board(ctx, 1, 4).Health, 1, "★ 打的是**生命最低**的 B（槽号大、血少）");
            Check(Board(ctx, 1, 1).Health, 9, "……槽号最小但血多的 A **没挨打**（改之前这里会掉血 ⇒ 判据能区分）");
        }

        // 平手时取**槽号在前**的那个（原版是严格小于 + 列表序，同一条规则，不是随手定的）
        {
            var ctx = ProbeBattle(new[] { Unit("T", 1, 1, 5) }, new[] { Unit("C", 1, 1, 3), Unit("D", 1, 1, 3) });
            ToP1Turn(ctx, 3);
            Place(ctx, 0, 0, Unit("T", 1, 1, 5));
            Place(ctx, 1, 2, Unit("C", 1, 1, 3));      // 槽号在前
            Place(ctx, 1, 6, Unit("D", 1, 1, 3));      // 一模一样，槽号在后
            var ops = EffectText.Parse("Deal 1 damage", out _, out _);
            RuleCore.ResolveOps(ctx, 0, null, ops, "自检 auto 目标平手");
            Check(Board(ctx, 1, 2).Health, 2, "★ 生命并列 ⇒ 取**槽号在前**的 C（原版严格小于 ⇒ 留列表序先者）");
            Check(Board(ctx, 1, 6).Health, 3, "……槽号在后的 D 没挨打");
        }
    }

    /// <summary>归一：只留字母/数字/汉字、转小写 —— **只用来判「两段文字是不是同一段」**，不参与语义。</summary>
    static string ZhNorm(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new StringBuilder();
        foreach (char ch in s) if (char.IsLetterOrDigit(ch)) sb.Append(char.ToLowerInvariant(ch));
        return sb.ToString();
    }

    /// <summary>
    /// 🔴 **2026-09-16：把「中文卡面对账」里那几条**零假阳性**的判据做进自检。**
    ///
    /// **背景**：全池中文对账跑了三轮，**A 类 15~17 条逐条核完、全是假阳性** ——
    /// 那套启发式**不能**直接进自检（噪声比信号多：脚本按旧格式读、读不到嵌套 op 与新增字段；
    /// 「每个」⇒`计数=` 那条判据**本身就是错的**。正本 `_tmp_view/zh_crosscheck_0916.md`）。
    /// 但里面筛出了几条**结构性**的小判据（实测零假阳性），各抓一种错 —— 够当护栏。
    ///
    /// ⚠️ **`descZh` 是我们自己译的、不是权威**（官方中文本地从来没有过）⇒
    ///    这几条判据抓的是「**两套独立文字对不上**」，**不是**「中文一定对」。
    ///    早期正是这条思路抓到 `Shrineworld`（6 费卡打出去消灭一个敌方单位）。
    /// </summary>
    static void TestZhAgreement()
    {
        var pool = CardDatabase.Load();

        // ---- J1 同名跨阵营：英文各不相同、中文却一模一样（= 中文串了）----
        //    实测抓到 `Terminator Champion`（`BL44` 的 `Desc` 是 `Armour 1`，
        //    中文却是 `EC33` 的「残忍…」⇒ **卡面承诺了一个它没有的能力**）。
        {
            var byName = new Dictionary<string, List<CardDef>>();
            foreach (var c in pool)
            {
                if (c == null || string.IsNullOrEmpty(c.Name)) continue;
                List<CardDef> g;
                if (!byName.TryGetValue(c.Name, out g)) { g = new List<CardDef>(); byName[c.Name] = g; }
                g.Add(c);
            }
            var hits = new List<string>();
            foreach (var kv in byName)
            {
                if (kv.Value.Count < 2) continue;
                var eng = new HashSet<string>();
                foreach (var c in kv.Value) eng.Add(ZhNorm(c.Desc));
                if (eng.Count < 2) continue;                 // 英文本来就一样 ⇒ 不是这一类
                string zh = null; bool same = true;
                foreach (var c in kv.Value)
                {
                    string z = ZhNorm(c.DescZh);
                    if (z.Length == 0) { same = false; break; }     // 中文缺 ⇒ 另一回事，不算串
                    if (zh == null) zh = z; else if (zh != z) { same = false; break; }
                }
                if (same) hits.Add(kv.Key + "（" + kv.Value.Count + " 张同名）");
            }
            Check(hits.Count, 0, "★ J1 同名跨阵营的卡，中文没有串（串了 = 卡面承诺一个它没有的能力）"
                + (hits.Count > 0 ? " —— 还有：" + string.Join(" / ", hits) : ""));
        }

        // ---- J2 中文写了某种效果、英文里**连对应的词都没有**（**窄表**）----
        //    ⚠️ 「生命 / 攻击 / 近战 / 远程」**故意不在表里** —— 那几个是**措辞差异**
        //       （英文写 `Heal N` / 图标被剥成 `+1`），实测 14 + 17 条假阳性。
        //    ⚠️ **反方向一律不查**（英文有、中文没有）：heal/damage/destroy 三个方向
        //       实测 74 / 7 / 5 条假阳性。
        {
            string[] zhWords = { "抽", "伤害", "造成", "部署", "治疗", "摧毁", "消灭", "眩晕", "弃", "复制",
                                 "护甲", "装甲", "侧翼", "费用", "手牌" };
            var hits = new List<string>();
            foreach (var c in pool)
            {
                if (c == null || string.IsNullOrEmpty(c.Desc) || string.IsNullOrEmpty(c.DescZh)) continue;
                bool zhHit = false;
                foreach (var w in zhWords) if (c.DescZh.Contains(w)) { zhHit = true; break; }
                if (!zhHit) continue;
                if (System.Text.RegularExpressions.Regex.IsMatch(c.Desc,
                        @"\b(draw|damage|deploy|heal|restore|destroy|stun|discard|cop(y|ies)|armou?r|flank|cost|hand)",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase)) continue;
                hits.Add(c.Name + "：「" + c.DescZh + "」/ 英文「" + c.Desc + "」");
            }
            Check(hits.Count, 0, "★ J2 中文写了某种效果、英文里连那个词都没有"
                + (hits.Count > 0 ? " —— 还有：" + string.Join(" ／ ", hits) : ""));
        }

        // ---- J3 英文 ` or `、中文没有「或」 ⇒ 那是**条件换数值**，op 树上必须有 `AltCondition` ----
        //   实测命中 3/3 全对（`Disruption Blades` / `Wulfen Pack Leader` / `Vindicator`）。
        //   ⚠️ 排除 `10 or less Health` 那种 **区间**写法。
        {
            var hits = new List<string>();
            foreach (var c in pool)
            {
                if (c == null || string.IsNullOrEmpty(c.Desc) || string.IsNullOrEmpty(c.DescZh)) continue;
                if (!System.Text.RegularExpressions.Regex.IsMatch(c.Desc, @"\bor\b",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase)) continue;
                if (System.Text.RegularExpressions.Regex.IsMatch(c.Desc, @"\bor\s+(less|more|fewer)\b",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase)) continue;
                if (c.DescZh.Contains("或")) continue;
                bool alt = false;
                foreach (var o in Walk(EffectText.Parse(c.Desc, out _, out _)))
                    if (o.AltAmount != 0 || !string.IsNullOrEmpty(o.AltCondition)) { alt = true; break; }
                if (!alt) hits.Add(c.Name);
            }
            Check(hits.Count, 0, "★ J3 英文 ` or `、中文无「或」（= 条件换数值）的卡，op 树上都有 `AltCondition`"
                + (hits.Count > 0 ? " —— 还有：" + string.Join(" / ", hits) : ""));
        }

        // ---- J4 中文有「若/如果」而整棵 op 树**没有任何条件载体**（护栏，本池 0 命中）----
        //   逮的正是 `Prayer` 那一类（中文「若该单位具有祈祷」曾被静默丢掉）。
        //   ⚠️ 豁免：卡级别层已经收下的（`WhenTriggers` / `TriggerTexts` / 光环）——
        //      那些句子的条件在**别的层**里，不在主解析器的 op 树上。
        {
            var hits = new List<string>();
            foreach (var c in pool)
            {
                if (c == null || string.IsNullOrEmpty(c.Desc) || string.IsNullOrEmpty(c.DescZh)) continue;
                if (!c.DescZh.Contains("若") && !c.DescZh.Contains("如果")) continue;
                if (c.WhenTriggers.Count > 0 || c.TriggerTexts.Count > 0 || c.AuraSpecs.Count > 0) continue;
                bool carrier = false;
                foreach (var o in Walk(EffectText.Parse(c.Desc, out _, out _)))
                {
                    if (!string.IsNullOrEmpty(o.Condition) || !string.IsNullOrEmpty(o.ConditionKind)
                        || o.When != null || !string.IsNullOrEmpty(o.AltCondition)
                        || o.DeathWatchTarget != null) { carrier = true; break; }
                    // ⚠️ **费用条件的载体不在「条件」那几个字段上**：`This costs 1 less if you control
                    //    a unit with Stealth`（`Fate Inescapable`）走的是**降费那条路**
                    //    （`costwhen` / `costifcontrol` 两个**标记 op**，真活在 `RuleCore.CostOf`）。
                    //    不豁免的话这一条是**假阳性** —— 实测就它一张。
                    if (o.Verb == "costwhen" || o.Verb == "costifcontrol") { carrier = true; break; }
                }
                if (!carrier) hits.Add(c.Name + "：「" + c.DescZh + "」/ 英文「" + c.Desc + "」");
            }
            Check(hits.Count, 0, "★ J4 中文写着「若/如果」的卡，op 树上都有条件载体"
                + (hits.Count > 0 ? " —— 还有：" + string.Join(" ／ ", hits) : ""));
        }

        // ---- J5 中文有「抽」而 op 树里没有 draw（护栏，本池 0 命中）----
        {
            var hits = new List<string>();
            foreach (var c in pool)
            {
                if (c == null || string.IsNullOrEmpty(c.Desc) || string.IsNullOrEmpty(c.DescZh)) continue;
                if (!c.DescZh.Contains("抽")) continue;
                if (System.Text.RegularExpressions.Regex.IsMatch(c.Desc, @"\bdraw",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase)) continue;   // 英文有 draw ⇒ 另一回事
                bool draw = false;
                foreach (var o in Walk(EffectText.Parse(c.Desc, out _, out _)))
                    if (o.Verb == "draw" || o.Verb == "drawtype" || o.Verb == "drawref") { draw = true; break; }
                if (!draw) hits.Add(c.Name + "：「" + c.DescZh + "」");
            }
            Check(hits.Count, 0, "★ J5 中文写着「抽」的卡，op 树里都有 draw"
                + (hits.Count > 0 ? " —— 还有：" + string.Join(" / ", hits) : ""));
        }

        // ---- J6 卡面写了 `Codex:` 的**单位/督军**，`keywords` 里就必须有 `Codex` ----
        //   🔴 没有的话 `RuleCore.CheckCodex` 的门 `u.Has(KeywordTable.Codex)` **永远不成立**
        //      ⇒ 那张卡的 Codex 能力**一次都不会发动**，而且**不报错**。
        //      实测 2026-09-16 补了 **11 张**（10 单位 + 1 督军）。
        //   ⚠️ **只查 `unit`/`hero`** —— 战术卡里的 `Codex:` 是**付费前缀**（`N Energy:` 那一类），
        //      不是要携带的关键词（实测 3 张：`Master Tactician` / `Assault Doctrine` / `Devastator Doctrine`）。
        {
            var hits = new List<string>();
            foreach (var c in pool)
            {
                if (c == null || string.IsNullOrEmpty(c.Desc)) continue;
                if (c.Type != "unit" && c.Type != "hero") continue;
                if (!System.Text.RegularExpressions.Regex.IsMatch(c.Desc, @"\bcodex\s*:",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase)) continue;
                if (c.Keywords != null && c.Keywords.ContainsKey("codex")) continue;
                hits.Add(c.Name + "（" + c.Id + "）");
            }
            Check(hits.Count, 0, "★ J6 卡面写了 `Codex:` 的单位/督军，`keywords` 里都有 `Codex`"
                + (hits.Count > 0 ? " —— 还有：" + string.Join(" / ", hits) : ""));
        }
    }

    /// <summary>
    /// 🔴 **2026-09-16：原来记成「终态」的 6 张，逐张开图查完 —— **6 张全都能修、0 张真终态**。**
    ///
    /// 这一节钉住其中 **4 张证据确凿的**；另 2 张（`Mekaniak` 的 Kustom Job 池 · `Ork Spanner` 的
    /// `a random bonus`）的池子是**结构推断**、不是文本直证 ⇒ **不猜**，留给用户裁决（见文档 §四）。
    ///
    /// · **`Awakened Obelisk`（`SAU65`）** —— 卡面 `add **Extermination Protocol** to your hand`（**单数**），
    ///   而池里那张（`SAU45`）叫 `Extermination Protocols`（**复数**）⇒ `FindByName` 全等失配
    ///   ⇒ 那半句造不出牌。修法：`CreatePool.FindByNameLoose`（**双向**单复数容错）。
    /// · **`Phobos Lieutenant`** —— `Create a random Ultramarines **card** in hand`：`card` 是**通称**、
    ///   不是兵种词 ⇒ `MatchKindWord` 认不出 ⇒ 落到「具名查」报「不是卡名也不是兵种词」。
    ///   修法：剥尾部 ` card(s)` + 「只剩阵营词 ⇒ 取该阵营全部卡」（判据照抄 `FilterChoose` 那一支）。
    /// · **`Neurotyrant`（`TL82`）** —— `create an **Ephemeral** copy of **it**`：`ephemeral` 前缀与
    ///   那个 `it` 两样都挡着。修法：`CreatePoolResult.MarkEphemeral` + `BattleContext.EventCard`
    ///   （参考实现 `rule_core.gd:722/:825-827`）。
    /// · **`Inner Circle Companion`（`DA21`）** —— `Gain Vanguard and **2 Quest Points**`：
    ///   一条 `gain` 里**混了两种接受者**（给单位 Vanguard + 给**玩家** 2 任务点）。
    ///   修法：`PayloadOp.Resource` + `DoGive` 在**目标循环之前**单独结算一次。
    /// </summary>
    static void TestFormerTerminalGaps()
    {
        var pool = CardDatabase.Load();

        // ---- ① `Awakened Obelisk`：单复数要认得出来 ----
        {
            var cr = CreatePool.Resolve(pool, "extermination protocol", "Sautekh");
            CheckTrue(cr.Ok && cr.Cards.Count == 1
                      && cr.Cards[0].Name.StartsWith("Extermination Protocol"),
                      "★ 造牌池认得出**单数**的 `Extermination Protocol`（池里那张是**复数**）"
                      + " —— 实得 " + (cr.Ok ? cr.Cards.Count + " 张「" + cr.Cards[0].Name + "」"
                                             : "失败：" + cr.Why));
        }

        // ---- ② `Phobos Lieutenant`：`a random Ultramarines card` ----
        {
            var cr = CreatePool.Resolve(pool, "random ultramarines card", "Ultramarines");
            bool allUM = cr.Cards.Count > 0;
            foreach (var c in cr.Cards) if (c.Faction != "Ultramarines") allUM = false;
            CheckTrue(cr.Ok && allUM,
                      "★ `Create a random Ultramarines **card** in hand` 筛得出池子、且**都是极限战士**"
                      + "（`card` 是通称、不是兵种词，原来整条造不出）—— 实得 "
                      + (cr.Ok ? cr.Cards.Count + " 张" : "失败：" + cr.Why));
        }

        // ---- ③ `Neurotyrant`：`ephemeral copy of it` ----
        {
            var cr = CreatePool.Resolve(pool, "ephemeral copy of it", "Leviathan");
            CheckTrue(cr.CopyOfPrev && cr.MarkEphemeral,
                      "★ `ephemeral copy of it` 认得出：既是「复制上一条的目标」、又带 **Ephemeral 标记**"
                      + "（原来 `ephemeral copy of it` 整段落进具名查 ⇒ 造不出来）—— 实得 CopyOfPrev="
                      + cr.CopyOfPrev + " MarkEphemeral=" + cr.MarkEphemeral);
        }

        // ---- ④ `Inner Circle Companion`：混在 `gain` 里的**玩家资源** ----
        {
            var ps = GivePayload.Parse("vanguard and 2 quest points");
            int res = 0; string resName = null; int resVal = 0;
            if (ps != null) foreach (var p in ps)
                if (!string.IsNullOrEmpty(p.Resource)) { res++; resName = p.Resource; resVal = p.Value; }
            CheckTrue(res == 1 && resName == "quest" && resVal == 2,
                      "★ `vanguard and 2 quest points` 里那半段认出来了（是给**玩家**的任务点，不是给单位的）"
                      + " —— 实得 " + res + " 段 / " + resName + "=" + resVal);
        }

        // ---- ⑤ `Mekaniak` 的 `Kustom Job` —— **用户 2026-09-16 拍板**按同组那 3 张定 ----
        //   依据是**卡面编号结构**（`Mekboy Gazmek` 25 + `Mekaniak` 25B + `25C/D/E`，
        //   与 `Titus` 00 + `Exemplary Warrior` 00a + `00b/c/d` 同形）。**不是文本直证**。
        {
            var mk = FindCard(pool, "Mekaniak");
            CheckTrue(mk != null, "卡池里找得到 `Mekaniak`（`GOF_Mekaniak`）");
            var opts = RuleCore.ChooseEffectOptions("Mekaniak");
            CheckTrue(opts != null && opts.Length == 3,
                      "★ `Mekaniak` 的选效果池 = **3 张**（`More Dakka` / `Ramshackle` / `Wreckin' Ball`）"
                      + " —— 实得 " + (opts == null ? "null" : opts.Length + " 张"));
            if (mk != null)
            {
                var ops = EffectText.Parse(mk.Desc, out _, out _);
                bool ce = false;
                foreach (var o in ops) if (o.Verb == "chooseeffect") ce = true;
                CheckTrue(ce, "★ 卡面那句解得出 `chooseeffect`（原来「Kustom Job」整段判不认识、卡废掉）"
                            + " —— 实得 desc：「" + mk.Desc + "」");
            }
        }

        // ---- ⑥ `Ork Spanner` 的 `a random bonus` —— **用户 2026-09-16 拍板**按 `Carnifex` 卡面的定义 ----
        {
            var sp = FindCard(pool, "Ork Spanner");
            CheckTrue(sp != null, "卡池里找得到 `Ork Spanner`（`GOF14`）");
            if (sp != null)
            {
                var ops = EffectText.Parse(sp.Desc, out _, out _);
                bool co = false;
                foreach (var o in ops)
                    if (o.Verb == "chooseone" && o.RandomPick && o.Payload != null
                        && o.Payload.Contains("melee") && o.Payload.Contains("ranged")
                        && o.Payload.Contains("armour")) co = true;
                CheckTrue(co, "★ `Give a random bonus to a friendly Vehicle` 解成「三项**随机取一**」"
                            + "（`+2 melee` / `+2 ranged attack` / `+1 armour`，定义抄自 `Carnifex` 卡面）"
                            + " —— 实得 desc：「" + sp.Desc + "」");
            }
        }
    }

    /// <summary>按卡名取 subtype（找不到返回 `(找不到)`，让断言当场显示出来）</summary>
    static string SubOf(IReadOnlyList<CardDef> pool, string name)
    {
        var c = FindCard(pool, name);
        return c == null ? "(找不到该卡)" : c.Subtype;
    }

    static CardDef FindCard(IReadOnlyList<CardDef> pool, string name)
    {
        foreach (var c in pool) if (c != null && c.Name == name) return c;
        return null;
    }

    /// <summary>
    /// 这张卡的关键词 `<词>` **带没带上数值** —— 走引擎真正读的那条路
    /// （`CardDef.Keywords` 是**解析过**的：`"Armour 1"` → 键 `armour`、值 `1`）。
    /// ⚠️ 别去比对原始字符串 —— 卡表里存的是 `"Armour 1"`，引擎读的是 `Keywords["armour"]`，
    ///    两处对不上的时候只有后者算数。
    /// </summary>
    static bool HasKwWithValue(IReadOnlyList<CardDef> pool, string name, string kw, int val)
    {
        return KwValueOf(pool, name, kw) == val;
    }

    /// <summary>走**引擎真正用的那条路**读数值关键词 —— 不是字符串比对，是 `UnitState.KwValue`</summary>
    static int KwValueOf(IReadOnlyList<CardDef> pool, string name, string kw)
    {
        var c = FindCard(pool, name);
        return c == null ? -1 : new UnitState(c, false).KwValue(kw);
    }

    /// <summary>
    /// `Deploy …` —— **免费把单位放进场上**（原版 `rule_core.gd:2970` / `_deploy_unit:3871`）。
    ///
    /// 和造牌同一套候选池（`CreatePool`），规格书同样是附录 B/C。三层都要验：
    ///   ① 解析（张数 / 从哪儿 / 费用区间 / `and` 尾句）；
    ///   ② 候选池和附录 B 的「几种」、附录 C 的名单对上；
    ///   ③ 结算（真打一张原版卡，单位**真的到了场上**、槽位对、疲劳对、满场时不硬塞）。
    /// </summary>
    static void TestDeploy()
    {
        var pool = CardDatabase.Load();

        // ---- ① 解析 ----
        {
            var op = OneOp("Deploy a Battle Sister");
            Check(op.Verb, "deploy", "`Deploy …` → 动词 deploy");
            Check(op.Amount, 1, "`a` → 1 个");
            Check(op.Payload, "battle sister", "部署什么存进 Payload");
            Check(op.DeployFrom, "pool", "默认从**卡池**找");

            // ⚠️ 这句**产出两条** op（部署 + 尾句 give），所以不能用 `OneOp`（它要求恰好 1 条）
            {
                var rr = EffectText.ParseSegment("Deploy three Tempestus Scion and give them Vanguard");
                Check(rr.Kind, EffectText.SegKind.Ok, "`Deploy … and give them Vanguard` 整句解析干净");
                Check(rr.Ops.Count, 2, "拆出 2 条：部署 + 尾句（尾句**不丢**）");
                Check(rr.Ops[0].Verb, "deploy", "第 1 条是部署");
                Check(rr.Ops[0].Amount, 3, "`three` → 3 个");
                Check(rr.Ops[1].Verb, "give", "第 2 条是尾句的 give");
                Check(rr.Ops[1].Target.Side, "prev", "尾句的 `them` 指**刚部署的那批**（不是「己方全体」）");
                Check(rr.Ops[1].Target.Count, 0, "`them` 是复数 → 一批（`it` 才是 1 个）");
            }

            op = OneOp("Deploy 4 random troops from your deck");
            Check(op.DeployFrom, "deck", "`from your deck` → 从牌库");
            CheckTrue(op.Random, "`random` 记下来了");
            Check(op.Payload, "troops", "`from your deck` 从 Payload 里剥掉");

            op = OneOp("Deploy up to 5 friendly Infantry troops that died this game");
            Check(op.DeployFrom, "graveyard", "`that died this game` → 从弃牌堆");
            CheckTrue(op.UpTo, "`up to` 记下来了（至多 N，不够就不凑）");
            Check(op.Amount, 5, "`up to 5` → 5");

            op = OneOp("Deploy 8 random Ork Infantry that cost 4 or less");
            Check(op.CostMax, 4, "`that cost 4 or less` → 上界 4");
            Check(op.CostMin, 0, "……下界不限");
            Check(op.Payload, "ork infantry", "费用从句从 Payload 里剥掉");

            op = OneOp("Deploy an Ultramarines troop that costs 6 or more");
            Check(op.CostMin, 6, "`that costs 6 or more` → 下界 6");
            Check(op.CostMax, 0, "……上界不限");

            op = OneOp("Deploy 4 random 2-cost Leviathan troops");
            Check(op.CostMin, 4 > 0 ? 2 : 2, "`2-cost` → 下界 2");
            Check(op.CostMax, 2, "`2-cost` → **上界也是 2**（恰好 2 费，不是「≤2」——见实证）");
            Check(op.Payload, "leviathan troops", "`2-cost` 从 Payload 里剥掉");
        }

        // ---- ② 候选池：卡池实算 vs 附录 B/C ----
        {
            // 附录 B「装甲攻势…」那类已在 `TestCreate` 验过，这里只验 deploy 独有的三类
            // 附录 B「次元裂隙：3 个随机 2 费部队（**6 种**，各 3 张，上限 36）」
            var r = PoolOf(pool, "Deploy 3 random 2-cost Sautekh troops",
                           "Sautekh", unitsOnly: true, costMin: 2, costMax: 2);
            Check(r.Cards.Count, 6, "Sautekh 恰好 2 费的部队 6 张 == 附录 B 的「6 种」");
            CheckAll(r.Cards, c => c.IsUnit && c.Cost == 2, "池子里每一个都是 2 费单位");

            // 附录 B「不屈远征：1 个 6 费+ 极限战士部队（**25 种**，各 1 张）」
            r = PoolOf(pool, "Deploy an Ultramarines troop that costs 6 or more",
                       "Ultramarines", unitsOnly: true, costMin: 6);
            Check(r.Cards.Count, 25, "Ultramarines 6 费及以上的部队 25 张 == 附录 B 的「25 种」");

            // 附录 B「火箭入侵：至多 8 个 4 费及以下步兵（24 种）」
            // ⚠️ 2026-09-13：这里原来写 23、注释「附录 B 写 24，**差 1**」。差的那张是
            //    **`Banner Nob`**（Goff，4 费）—— 它在 OCR 源表里 subtype 是 `unit`，
            //    **卡面印的是 `Infantry`**（卡面逐张核对发现，见 `CARD_FACE_FIXES_SRC`）。
            //    修掉之后与附录 B 的 24 **完全吻合**。
            r = PoolOf(pool, "Deploy 8 random Ork Infantry that cost 4 or less",
                       "Goff", unitsOnly: true, costMax: 4);
            Check(r.Cards.Count, 24, "Goff 4 费及以下步兵 24 张 == 附录 B 的「24 种」（2026-09-13 起全中）");
            CheckAll(r.Cards, c => c.Faction == "Goff" && c.Subtype == "Infantry" && c.Cost <= 4,
                     "……而且每一张都是高夫步兵且 ≤4 费");

            // `Genestealer Cults` → `Genestealers` 别名
            r = PoolOf(pool, "Deploy 2 random 2-cost Genestealer Cults troops",
                       "Genestealers", unitsOnly: true, costMin: 2, costMax: 2);
            Check(r.Cards.Count, 5, "`Genestealer Cults` 认得出（别名表），恰好 2 费 5 张 == 附录 C 的 1d5");

            // `friendly Infantry troops` —— **两个词都是兵种词**，`Infantry` 才是筛选条件。
            // ⚠️ 旧写法只看最后 1–2 个词当兵种词，于是 `Infantry` 被当成**阵营名**（对不上 →
            //    落回施放者阵营），兵种筛选**静默丢掉**。断言**筛出来的池子**，别断言日志措辞。
            r = PoolOf(pool, "Deploy 8 random Ork Infantry that cost 4 or less", "Goff", unitsOnly: true);
            CheckAll(r.Cards, c => c.Subtype == "Infantry",
                     "`Infantry troops`/`Ork Infantry` 里的 `Infantry` 真的当兵种筛了");

            // 部署只要**单位**：战术卡不该进池子
            r = PoolOf(pool, "Deploy a Sabotage", "Genestealers", unitsOnly: true);
            CheckTrue(!r.Ok, "「部署一张破坏（战术卡）」被拒 —— 部署要的必须是单位卡");
        }

        // ---- ③ 结算 ----
        {
            // `Deploy a Battle Sister` 出自哪张卡都行，用合成卡把变量压到最少
            var tac = Tactic("T_Deploy", 1, "Deploy a Battle Sister");
            var ctx = BattlePool(new[] { tac }, new[] { Unit("X", 1, 1, 5) }, pool,
                                 warlordFaction: "Sororitas");
            ToP1Turn(ctx, 1);
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Deploy"), -1), RuleCodes.OK,
                      "`Deploy a Battle Sister` 打得出去");
            var b = Board(ctx, 0, 0);
            CheckTrue(b != null && b.Name == "Battle Sister", "单位**真的到了场上**（槽 0）");
            CheckTrue(b.Exhausted, "部署当回合**疲劳**（Battle Sister 没有迅捷/侧翼）");
            CheckTrue(b.Card != null && b.Card.Faction == "Sororitas", "来的是卡池里那张真卡");

            // 张数：`Deploy two Storm Guardian`
            var tac2 = Tactic("T_Deploy2", 1, "Deploy two Storm Guardian");
            var ctx2 = BattlePool(new[] { tac2 }, new[] { Unit("X", 1, 1, 5) }, pool,
                                  warlordFaction: "SaimHann");
            ToP1Turn(ctx2, 1);
            RuleCore.PlayTactic(ctx2, 0, HandIdx(ctx2, 0, "T_Deploy2"), -1);
            int onBoard = 0;
            for (int s = 0; s < RuleEngine.BoardSpec.Size; s++)
                if (Board(ctx2, 0, s) != null && Board(ctx2, 0, s).Name == "Storm Guardian") onBoard++;
            Check(onBoard, 2, "`two` → 场上真的多了 2 个");

            // `and give them Vanguard` 的尾句必须一起结算（丢了就是静默失效）
            var tac3 = Tactic("T_Deploy3", 1, "Deploy a Battle Sister and give it Flank");
            var ctx3 = BattlePool(new[] { tac3 }, new[] { Unit("X", 1, 1, 5) }, pool,
                                  warlordFaction: "Sororitas");
            ToP1Turn(ctx3, 1);
            RuleCore.PlayTactic(ctx3, 0, HandIdx(ctx3, 0, "T_Deploy3"), -1);
            var b3 = Board(ctx3, 0, 0);
            CheckTrue(b3 != null && b3.Has("flank"), "`and give it Flank` 的尾句结算了（侧翼挂上了）");

            // `from your deck`：牌库里的那张要**移走**，不能同时留在牌库。
            // ⚠️ 夹具注意：`DeckOf` 把 hand 数组**倒着**放到牌库末尾，起手 3 张从末尾抽 ——
            //    所以要让 `deckSister` 留在牌库，就得给它前面垫 3 张（第一版没垫，它被起手抽进手牌了，
            //    于是「从牌库部署」当然找不到它，测试红而代码是对的）。
            var tac4 = Tactic("T_Deploy4", 1, "Deploy a Battle Sister from your deck");
            var deckSister = CardDatabase.Find(pool, "Battle Sister");
            // 起手 3 张 + 第一个回合开始再抽 1 张 = **抽走 4 张**；`DeckOf` 从 hand[0] 起按顺序被抽，
            // 所以 `deckSister` 要放在 hand[4] 之后才留得住（第一版放在 hand[3]，正好被第 4 抽抽走）
            var ctx4 = BattlePool(new[] { tac4, Unit("pad1", 1, 0, 1), Unit("pad2", 1, 0, 1),
                                          Unit("pad3", 1, 0, 1), Unit("pad4", 1, 0, 1), deckSister },
                                  new[] { Unit("X", 1, 1, 5) }, pool, warlordFaction: "Sororitas");
            ToP1Turn(ctx4, 1);
            int before = ctx4.Players[0].Deck.Count;
            CheckTrue(HandIdx(ctx4, 0, "Battle Sister") < 0, "夹具：那张 Battle Sister 现在在**牌库**里");
            RuleCore.PlayTactic(ctx4, 0, HandIdx(ctx4, 0, "T_Deploy4"), -1);
            Check(ctx4.Players[0].Deck.Count, before - 1, "从牌库部署后，那张牌**离开了牌库**");
            CheckTrue(HandIdx(ctx4, 0, "Battle Sister") < 0, "……也没跑到手牌里去");

            // **满场**：部署不下就不硬塞（`_deploy_unit` 的语义），而且要说出来
            var tac5 = Tactic("T_Deploy5", 1, "Deploy two Storm Guardian");
            var ctx5 = BattlePool(new[] { tac5 }, new[] { Unit("X", 1, 1, 5) }, pool,
                                  warlordFaction: "SaimHann");
            ToP1Turn(ctx5, 1);
            for (int s = 0; s < RuleEngine.BoardSpec.Size; s++)
                if (s != RuleEngine.BoardSpec.WarlordSlot) Place(ctx5, 0, s, Unit("Blocker" + s, 1, 1, 1));
            RuleCore.PlayTactic(ctx5, 0, HandIdx(ctx5, 0, "T_Deploy5"), -1);
            CheckTrue(ctx5.Events.Exists(e => e.Contains("没空格")), "满场 → 日志里说「没空格了」");
            int blockers = 0;
            for (int s = 0; s < RuleEngine.BoardSpec.Size; s++)
                if (Board(ctx5, 0, s) != null && Board(ctx5, 0, s).Name.StartsWith("Blocker")) blockers++;
            Check(blockers, 8, "满场时**没有挤掉任何一个**原有的单位");

            // 同种子可复现
            var tac6 = Tactic("T_Deploy6", 1, "Deploy 2 random 2-cost Sautekh troops");
            var cA = BattlePool(new[] { tac6 }, new[] { Unit("X", 1, 1, 5) }, pool, warlordFaction: "Sautekh");
            var cB = BattlePool(new[] { tac6 }, new[] { Unit("X", 1, 1, 5) }, pool, warlordFaction: "Sautekh");
            ToP1Turn(cA, 1); ToP1Turn(cB, 1);
            RuleCore.PlayTactic(cA, 0, HandIdx(cA, 0, "T_Deploy6"), -1);
            RuleCore.PlayTactic(cB, 0, HandIdx(cB, 0, "T_Deploy6"), -1);
            bool same = true;
            for (int s = 0; s < RuleEngine.BoardSpec.Size; s++)
            {
                var x = Board(cA, 0, s); var y = Board(cB, 0, s);
                if ((x == null) != (y == null)) { same = false; break; }
                if (x != null && x.Name != y.Name) { same = false; break; }
                // 池子是 6 张、只抽 2 张 —— 必须**不重复**
            }
            CheckTrue(same, "同一个种子部署出同样的单位");
            var names = new List<string>();
            for (int s = 0; s < RuleEngine.BoardSpec.Size; s++)
                if (Board(cA, 0, s) != null && Board(cA, 0, s).Name != "FixtureWarlord")
                    names.Add(Board(cA, 0, s).Name);
            Check(names.Count, 2, "`Deploy 2 random …` 部署了 2 个");
            CheckTrue(names[0] != names[1], "一次效果里**不重复**（池子 6 张只抽 2 张）");

            // **不触发 Rally**：规则书 :200 写的是「**从手牌**部署后触发」，
            // 原版也只在 play_card 那条路上触发（`rule_core.gd:2314`）—— 免费部署不触发。
            var rally = new CardDef("RallyGirl", "RallyGirl", "unit",
                                    "", null, "Sororitas", 1, 1, 3, 0,
                                    new[] { "Rally: Draw a card" });
            var tac7 = Tactic("T_Deploy7", 1, "Deploy a Battle Sister");
            var ctx7 = BattlePool(new[] { tac7 }, new[] { Unit("X", 1, 1, 5) }, pool,
                                  warlordFaction: "Sororitas");
            ToP1Turn(ctx7, 1);
            // 把卡池换成带 Rally 的那张，验证「部署了但它不触发」
            var poolR = new List<CardDef>(pool); poolR.Add(rally);
            ctx7.CardPool = poolR;
            int handBefore = ctx7.Players[0].Hand.Count;
            RuleCore.PlayTactic(ctx7, 0, HandIdx(ctx7, 0, "T_Deploy7"), -1);
            Check(ctx7.Players[0].Hand.Count, handBefore - 1,
                  "免费部署**不触发 Rally**（手牌没多 —— 规则书 :200「从手牌部署后」）");
        }

        // ---- ⑩ 🆕 2026-09-14 T2：**裸 `+N` 判错 10 处**（卡面是「远程」、我们当「近战」）----
        //  根因：载荷里数字后面跟的是**图标**（紫枪 = 远程攻击），OCR 把图标丢了 ⇒
        //  `GivePayload.cs:261-271` 那条裸 `+N` 兜底**一律判近战**（它唯一的依据是
        //  `March of Vengeance` 一张卡，那张的图标确实是红拳）。
        //  ⇒ 修的是**卡表数据**（`cardface_fixes.json` 的 `desc` 列），**不是**那条兜底 ——
        //    正则拿不到「卡面第几个图标是什么」这个信息。
        //  ⚠️ 断言分两层：① 十张卡的载荷**逐个**拆出来必须含 `ranged`（数据层，覆盖全部十张）；
        //                ② 挑两张**真打一局**量单位身上的数（结算层）—— 光钉数据的话，
        //                   「改对了数据但结算没接上」照样绿。
        {
            var p2 = CardDatabase.Load();

            // ① 数据层：`{卡名, 期望的 Attr 序列}`（顺序也钉 —— 防止两个 `+1` 换了位置）
            var want = new[]
            {
                new[] { "Cadian Honour",        "ranged" },
                new[] { "Lord Commander",       "attack", "ranged" },
                new[] { "Lord Exultant",        "attack", "ranged" },
                new[] { "Slaanesh Sorcerer",    "attack", "ranged" },
                new[] { "Terrifying Crescendo", "ranged" },
                new[] { "Neophyte Leader",      "attack", "ranged" },
                new[] { "Exhortation of Rage",  "ranged" },
                new[] { "Paragon of Ultramar",  "attack", "ranged" },
                new[] { "Primarch of the XIII", "attack", "ranged" },
                new[] { "Tactical Doctrine",    "ranged" },
            };
            int bad = 0;
            foreach (var w in want)
            {
                var cd = CreatePool.FindByName(p2, w[0]);
                if (cd == null) { bad++; CheckTrue(false, $"卡池里找不到 `{w[0]}`"); continue; }
                var ops = EffectText.Parse(cd.Desc, out _, out _);
                var got = new List<string>();
                foreach (var op in ops)
                {
                    var ps = GivePayload.Parse(op.Payload);
                    if (ps == null) continue;
                    foreach (var q in ps) if (!q.IsKeyword && q.Attr != "energy") got.Add(q.Attr);
                }
                bool ok = got.Count == w.Length - 1;
                if (ok) for (int i = 0; i < got.Count; i++) if (got[i] != w[i + 1]) ok = false;
                if (!ok) bad++;
                CheckTrue(ok, $"★ `{w[0]}` 的载荷属性应为 [{string.Join(", ", w, 1, w.Length - 1)}]，"
                              + $"实得 [{string.Join(", ", got)}]"
                              + " —— 判错的话那个「远程 +N」会静默加到**近战**上");
            }
            Check(bad, 0, "十张「裸 `+N` 实为远程」的卡**全部**修好（2026-09-14 T2）");

            // ② 结算层：**远程涨了、近战没涨**（`Exhortation of Rage` 卡面是 `+1 🔫`）
            {
                var rage = CreatePool.FindByName(p2, "Exhortation of Rage");
                var ctx = BattlePool(new[] { rage }, new[] { Unit("X", 1, 1, 5) }, p2,
                                     warlordFaction: "Ultramarines");
                ToP1Turn(ctx, 1);
                ctx.Players[0].Energy = 12;
                var mine = Place(ctx, 0, 0, Ranged("MyTroop", 1, 1, 5, 0));   // 近战 1 / 远程 0
                // ⚠️ 第 4 个参数是**玩家点的那一格**（`-1` = 不点）。这里**必须能传 `-1`** ——
                //    `to your troops` 是**复数 ⇒ 全体**（见下面 ⑪），`Count == 0` 就不该要玩家点。
                CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "Exhortation of Rage"), -1),
                          RuleCodes.OK, "打出真卡 `Exhortation of Rage`");
                Check(mine.RangedAttack, 1, "★ 它的**远程** +1（卡面 `+1 🔫`）" + LogTail(ctx));
                Check(mine.Attack, 1, "★ 反例：**近战**一点没动（判错时这里会是 2、远程还是 0）" + LogTail(ctx));
            }

            // ③ 反例：**判近战判对了的那 14 处，不许被改坏**（`Get'em ladz!` 卡面 `+2` 是红拳）
            //   🆕 顺带把「复数 ⇒ 全体」钉在**结算**上：场上摆**两个**己方单位，两个都要涨。
            {
                var ladz = CreatePool.FindByName(p2, "Get'em ladz!");
                CheckTrue(ladz != null, "卡池里有 `Get'em ladz!`");
                if (ladz != null)
                {
                    var ctx = BattlePool(new[] { ladz }, new[] { Unit("X", 1, 1, 5) }, p2,
                                         warlordFaction: "Goff");
                    ToP1Turn(ctx, 1);
                    ctx.Players[0].Energy = 12;
                    var a = Place(ctx, 0, 0, Ranged("MyTroopA", 1, 1, 5, 0));   // 近战 1 / 远程 0
                    var b = Place(ctx, 0, 1, Ranged("MyTroopB", 1, 1, 5, 0));
                    CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "Get'em ladz!"), -1),
                              RuleCodes.OK, "打出真卡 `Get'em ladz!`（`to your units` ⇒ 不要玩家点目标）");
                    Check(a.Attack, 3, "★ **近战 +2**（这张的裸 `+N` 判近战本来就是对的）" + LogTail(ctx));
                    Check(a.RangedAttack, 0, "★ 反例：**远程**一点没动" + LogTail(ctx));
                    Check(b.Attack, 3,
                          "★ **第二个单位也 +2** —— `to your units` 是**复数 ⇒ 全体**"
                          + "（`rule_core.gd:3966-3980`）。只给一个加的话这条会红" + LogTail(ctx));
                }
            }
        }

        // ---- ⑪ 🆕 2026-09-14 T2b：**复数的己方部队 = 全体**（原版 `rule_core.gd:3966-3980`）----
        //  原来只认 `all` / `each` 这两个**词** ⇒ `Give +1 to your troops` 落回 `Count = 1`：
        //  **只给一个单位加**，而且还要玩家点一个（`Count == 1` 正是 `PickTarget` 判「要不要点」的依据）。
        //  实测全卡池这种句子 **44 句**（普查脚本按「give 类动词 + to your/friendly troops|units」筛出来的）。
        //  ⚠️ 判据是**名词的单复数**、不是 `all` 这个词。
        {
            // ① 复数 ⇒ `Count = 0`
            foreach (var s in new[]
                     {
                         "Give +1 to your troops",
                         "Give +2 Health to your units",
                         "Heal 3 to your units",
                         "Give +1 Attack to your Infantry troops",
                     })
            {
                var sp = EffectText.ParseTarget(s);
                CheckTrue(sp != null && sp.Count == 0,
                          $"★ 「{s}」的目标是**全体**（`Count == 0`）—— 落回 1 就是「只给一个加」");
            }

            // ② 反例：**单数**仍然是「挑一个」
            foreach (var s in new[] { "a friendly troop", "a friendly unit", "an enemy troop" })
            {
                var sp = EffectText.ParseTarget(s);
                CheckTrue(sp != null && sp.Count == 1, $"★ 反例：「{s}」是**单数** ⇒ 仍然挑一个");
            }

            // ③ 反例：**相邻**短语的 `Count` 不许被动 —— 它管的是「锚点选几个」，
            //    改成 0 会让 `Cleansing Flames` 那族**不再要玩家选目标**、锚点落空。
            //    （「那一圈全要」是另一个标记 `AdjacentAll`，见 `ParseTarget` 里那段注释。）
            {
                var sp = EffectText.ParseTarget("an enemy and its adjacent units");
                CheckTrue(sp != null && sp.Adjacent && sp.Count == 1,
                          "★ 反例：`an enemy and its adjacent units` 的 `Count` **仍是 1**"
                          + "（锚点要玩家点；那一圈全要由 `AdjacentAll` 表达）");
            }

            // ④ 「*你的*部队」在**单位卡正文**里也一样（`Nexos` 的 `Artifice: Your units gain +1 Attack`）
            {
                var sp = EffectText.ParseTarget("your units");
                CheckTrue(sp != null && sp.Count == 0 && sp.Side == "own",
                          "★ `your units` ⇒ 己方**全体**");
            }
        }

        // ---- ⑫ 🆕 2026-09-14：**督军卡没有费用六边形** ⇒ 那 5 张的费用必须是 0 ----
        //  踩过：5 张 Saim-Hann 督军右上角的**阵营徽记**（深绿圆盘里的蛇形 / S 剑纹）
        //  被 OCR 读成了数字 **5**。**主对话逐张开图复核 5/5**（证据在
        //  `Unity/工具/gen_cards_engine.py` 的 `STAT_FIXES` 里，每条都带卡图路径）。
        //  ⚠️ **影响面小但不是零**：`CreatePool` 会按 `Cost` 筛卡（`Core/CreatePool.cs:133/:134/:301`）。
        //  ⚠️ **别把这条读成「督军费用已经全核过」** —— 另有 **9 张**督军 cost 非 0
        //     （2×4 / 3×3 / 1×2），**一张都没开图核过**。要核就照这个法子：看右上角有没有蓝色费用六边形。
        {
            foreach (var n in new[] { "Jain Zar", "Anvirr Keltoc", "Eliac Zephyrblade",
                                      "Medreyal Ghaelyn", "Lhykhis" })
            {
                var c = CreatePool.FindByName(pool, n);
                CheckTrue(c != null && c.Cost == 0,
                          $"★ 督军 `{n}` 的费用是 **0**（卡片右上角是**阵营徽记**、根本没有费用六边形）");
            }
            int nonZero = 0;
            foreach (var c in pool) if (c != null && c.Type == "hero" && c.Cost != 0) nonZero++;
            CheckTrue(nonZero <= 9,
                      $"★ 督军里 cost 非 0 的应 ≤ **9** 张（实得 {nonZero}）—— "
                      + "那 9 张是**待核**的，别当成已核（数字降下来是好事，涨上去就是有人往督军身上写费用）");
        }
    }

    /// <summary>
    /// 🆕 2026-09-14 T3：**「选一个效果」**（`chooseeffect`）—— 三张卡共用一套机制：
    ///   · `Exemplary Warrior`（UM 天赋，2 费）—— 池子里是**三张 0 费卡**：`Righteous Fury` /
    ///     `Master of Arms` / `Paragon of Ultramar`（**用户 2026-09-14 给的卡图位置**，卡面已亲读）
    ///   · `Hyper-adaptation` / `Infinite Biomorphologies`（Leviathan 各 2 费）—— 池子是
    ///     **三项固定载荷**（`+1 Armour` / `+2 Melee Attack` / `+2 Ranged Attack`，**用户 2026-09-13 给的**），
    ///     两张**共用同一份**，差别只在作用域（场上一个友方部队 / 手牌里全部部队）。
    ///
    /// ⚠️ **挑法是我们的**：UI 做出来之前用 `ctx.Rng` 等概率取 1（与 `choosecard` 同一口径、
    ///    同一局可复现）。原版是**玩家从 3 项里选 1** —— 「选效果」面板是独立一件，
    ///    见 `资料/选牌Choose_数据与设计.md` §四之二。所以下面的断言**必须对三种选法都成立**
    ///    （不能钉「一定选中某项」，那会随随机序列变化）。
    /// ✅ **但「治多少」已经能钉死了** —— 用户 **2026-09-14** 裁过（见 ⑤ 那条注释）。
    /// </summary>
    static void TestChooseEffect()
    {
        var pool = CardDatabase.Load();

        // ---- ① 解析层：三种写法 ----
        {
            var ops = EffectText.Parse("Your Warlord heals 1 and chooses an effect", out var un, out _);
            CheckTrue(un.Count == 0, "`Your Warlord heals 1 and chooses an effect` 解析干净");
            Check(ops.Count, 2, "★ 切成**两条 op**：先治疗、再选效果");
            if (ops.Count == 2)
            {
                Check(ops[0].Verb, "heal", "第一条是 `heal`（`Exemplary Warrior` 自己的「heals 1」）");
                Check(ops[0].Amount, 1, "治 1 点");
                CheckTrue(ops[0].Target != null && ops[0].Target.Kind == "warlord",
                          "★ 目标是**督军**（`Your Warlord heals 1` 是**反语序**，主语写在动词前面）");
                Check(ops[1].Verb, "chooseeffect", "第二条是 `chooseeffect`");
                Check(ops[1].Payload, "self", "作用域 = `self`（条目是整张卡，各自带自己的目标）");
            }
        }
        {
            var ops = EffectText.Parse("Choose an effect and give it to a friendly troop", out var un, out _);
            CheckTrue(un.Count == 0, "`Choose an effect and give it to a friendly troop` 解析干净");
            Check(ops.Count, 1, "一条 op");
            if (ops.Count == 1)
            {
                Check(ops[0].Verb, "chooseeffect", "动词 = chooseeffect");
                Check(ops[0].Payload, "give", "作用域 = `give`（条目是载荷，给挑中的那个单位）");
                CheckTrue(ops[0].Target != null && ops[0].Target.Count == 1,
                          "★ 目标是「**一个**友方部队」（要点一个）");
            }
        }
        {
            var ops = EffectText.Parse("Choose an effect and give it to all troops in your hand", out var un, out _);
            CheckTrue(un.Count == 0, "`… to all troops in your hand` 解析干净");
            Check(ops.Count, 1, "一条 op");
            if (ops.Count == 1)
            {
                Check(ops[0].Verb, "chooseeffect", "动词 = chooseeffect");
                Check(ops[0].Payload, "hand",
                      "★ 作用域 = `hand`（给**手牌**，不是场上的目标 —— 交给 `ParseTarget` 会解出错的池子）");
            }
        }

        // ---- ② 反例：不许抢走别人的句子 ----
        //  ②-a `Choose a troop from your deck and draw it` 仍归 `choosecard`
        {
            var ops = EffectText.Parse("Choose a troop from your deck and draw it", out var un, out _);
            CheckTrue(un.Count == 0 && ops.Count > 0, "`Choose a troop …` 仍解析干净");
            CheckTrue(ops.Count == 0 || ops[0].Verb != "chooseeffect",
                      "★ 反例：**选牌**的句子没被 `chooseeffect` 抢走（两者都以 `choose` 开头）");
        }
        //  ②-b 条件从句**不许当主语** —— 新的「反语序治疗」正则只认 `(your|the) warlord`，
        //      放开成一般的「名词短语 + heals N」会把这些吃成「任何时候都治疗」的静默错语义。
        foreach (var s in new[] { "When another troop dies, heals 2",
                                  "When a friendly unit obtains [Shield], it heals 2" })
        {
            var ops = EffectText.Parse(s, out var un, out _);
            CheckTrue(ops.Count == 0 || un.Count > 0 || ops[0].Verb != "heal" || ops[0].Target == null
                      || ops[0].Target.Kind == "any",
                      "★ 反例：「" + s + "」**不许**被反语序治疗吃掉（条件从句当主语 = 静默错语义）");
        }

        // ---- ③ 池子表：三张卡都在，且**两张 Leviathan 引用同一份**（不抄两份）----
        {
            var f = typeof(RuleCore).GetField("ChooseEffectPools",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            CheckTrue(f != null, "`RuleCore.ChooseEffectPools` 存在");
            if (f != null)
            {
                var tbl = f.GetValue(null) as System.Collections.IDictionary;
                // ⚠️ 2026-09-16：3 → **4**（新增 `Mekaniak`）。**用 `>=`** —— 以后再登记新池子
                //    不该把这条断言再改一次（它要钉的是「三张老卡都还在、且 Leviathan 那两张共用一份」）。
                CheckTrue(tbl != null && tbl.Count >= 4, "★ 池子表 ≥4 个键（每张卡各一个注册）");
                if (tbl != null)
                {
                    CheckTrue(tbl.Contains("Exemplary Warrior"), "`Exemplary Warrior` 登记了");
                    CheckTrue(tbl.Contains("Hyper-adaptation"), "`Hyper-adaptation` 登记了");
                    CheckTrue(tbl.Contains("Infinite Biomorphologies"), "`Infinite Biomorphologies` 登记了");
                    CheckTrue(tbl.Contains("Mekaniak"), "`Mekaniak` 登记了（2026-09-16 用户裁定）");
                    if (tbl.Contains("Hyper-adaptation") && tbl.Contains("Infinite Biomorphologies"))
                        CheckTrue(ReferenceEquals(tbl["Hyper-adaptation"], tbl["Infinite Biomorphologies"]),
                                  "★ 两张 Leviathan 卡**引用同一份池子**（抄成两份迟早不一致）");
                }
            }
        }

        // ---- ④ 结算层 `Hyper-adaptation`：**三种选法各验一次** ----
        //  做法：把三项的效果签名列出来 —— `+1 甲` / `+2 近战` / `+2 远程`，
        //  断言**恰好命中其中一种**（其余两项不许动）。这样不用知道随机选中了哪个，
        //  但任何一项写错（数值、属性、作用对象）都会当场红。
        {
            var card = CreatePool.FindByName(pool, "Hyper-adaptation");
            CheckTrue(card != null, "卡池里有 `Hyper-adaptation`");
            if (card != null)
            {
                var ctx = BattlePool(new[] { card }, new[] { Unit("X", 1, 1, 5) }, pool,
                                     warlordFaction: "Leviathan");
                ToP1Turn(ctx, 1);
                ctx.Players[0].Energy = 12;
                var picked = Place(ctx, 0, 0, Ranged("PickMe", 1, 2, 9, 3));     // 近 2 / 远 3 / 甲 0
                var other = Place(ctx, 0, 1, Ranged("NotMe", 1, 2, 9, 3));
                int a0 = picked.Armor, m0 = picked.Attack, r0 = picked.RangedAttack;
                int oa0 = other.Armor, om0 = other.Attack, or0 = other.RangedAttack;

                CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "Hyper-adaptation"), 0),
                          RuleCodes.OK, "打出真卡 `Hyper-adaptation`（点 0 号那个）");

                bool armour = picked.Armor == a0 + 1 && picked.Attack == m0 && picked.RangedAttack == r0;
                bool melee  = picked.Armor == a0     && picked.Attack == m0 + 2 && picked.RangedAttack == r0;
                bool ranged = picked.Armor == a0     && picked.Attack == m0     && picked.RangedAttack == r0 + 2;
                CheckTrue(armour || melee || ranged,
                          "★ 选中的那个**恰好吃到三项之一**（`+1 甲` / `+2 近战` / `+2 远程`）—— "
                          + $"实得 甲{a0}→{picked.Armor} 近战{m0}→{picked.Attack} 远程{r0}→{picked.RangedAttack}"
                          + LogTail(ctx));
                CheckTrue(other.Armor == oa0 && other.Attack == om0 && other.RangedAttack == or0,
                          "★ 反例：**没被点到的那个一点没变**（`to a friendly troop` 是挑一个）"
                          + LogTail(ctx));
            }
        }

        // ---- ⑤ 结算层 `Exemplary Warrior`：**督军治疗** + 三种选项（**三种各跑一遍**）----
        //  ⚠️ 三条选项的效果签名（都是照卡面读的，见 `资料/阵营推进_清单与交接.md` §一之三）：
        //     `Righteous Fury`   督军 +1 近战（**到你的下回合**，无 Blast）+ **它自己印的「治 1」**
        //     `Master of Arms`   督军 +1 近战 **且拿到 Blast**（本回合）
        //     `Paragon of Ultramar`  **你的部队**（不含督军）+1 近战 +1 远程
        //  ✅ **「heals 1」两张卡都印着**（`Exemplary Warrior` 自己 + `Righteous Fury` 也写）——
        //     **用户 2026-09-14 已裁决：各算各的**（「先天赋卡恢复 1 点，然后选择效果再恢复一点」）
        //     ⇒ 选中 `Righteous Fury` 时**治 2 点**，选中另两项**治 1 点**。**照字面来。**
        //     🔴 **这条裁决已钉进断言**（下面那张表里的 `Heal` 列）—— 改坏了会红。
        //     （另一种读法「本体那句就是在说这个选项的治疗」会拿掉 `Righteous Fury` 卡面上那句
        //       `Heals 1` 的作用 ⇒ 违反本工程「印了却不发生 = 红线」，已被否。）
        //  ⚠️ **挑法**：面板做出来之前是 `ctx.Rng` 等概率取 1（这一段原来是「三种里命中一种就算过」
        //     —— 那样**随机序列不覆盖哪一支，哪一支就没被验**）。现在**三种各跑一遍**、答案由
        //     `ctx.ChoosePicks` 直接给（**表现层面板走的就是这条队列**）⇒ 三条分支都真的跑到了。
        {
            var card = CreatePool.FindByName(pool, "Exemplary Warrior");
            CheckTrue(card != null, "卡池里有 `Exemplary Warrior`");
            if (card != null)
            {
                // 下标 = `EffectResolver.ChooseEffectPools` 里那个数组的次序（**别照卡面顺序猜**）
                var opts = new[]
                {
                    // 池子次序 / 名字               督军攻  Blast  部队攻  部队远  督军回血
                    new { Name = "Righteous Fury",      WAtk = 1, Blast = false, TAtk = 0, TRng = 0, Heal = 2 },
                    new { Name = "Master of Arms",      WAtk = 1, Blast = true,  TAtk = 0, TRng = 0, Heal = 1 },
                    new { Name = "Paragon of Ultramar", WAtk = 0, Blast = false, TAtk = 1, TRng = 1, Heal = 1 },
                };
                for (int k = 0; k < opts.Length; k++)
                {
                    var o = opts[k];
                    var ctx = BattlePool(new[] { card }, new[] { Unit("X", 1, 1, 5) }, pool,
                                         warlordFaction: "Ultramarines");
                    ToP1Turn(ctx, 1);
                    ctx.Players[0].Energy = 12;
                    ctx.ResetChoices();
                    ctx.ChoosePicks.Enqueue(k);        // ← 把答案先填好（= 玩家在面板上点了第 k+1 项）
                    var w = ctx.Players[0].Warlord;
                    w.Health = w.MaxHealth - 8;
                    var troop = Place(ctx, 0, 0, Ranged("MyTroop", 1, 2, 9, 3));   // 近 2 / 远 3
                    int wHp = w.Health, wAtk = w.Attack;
                    int tAtk = troop.Attack, tRng = troop.RangedAttack;

                    CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "Exemplary Warrior"), -1),
                              RuleCodes.OK, "打出真卡 `Exemplary Warrior`（面板选 **" + o.Name + "**）");
                    Check(ctx.ChooseAnswered, 1,
                          "★ 这次是**按面板答案**挑的，不是引擎替玩家挑（`ChooseAnswered`）");

                    string got = "实得 督军血 " + wHp + "→" + w.Health + " 攻 " + wAtk + "→" + w.Attack
                               + " blast=" + w.Has("blast")
                               + " / 部队 攻 " + tAtk + "→" + troop.Attack
                               + " 远 " + tRng + "→" + troop.RangedAttack;
                    Check(w.Attack - wAtk, o.WAtk, "★ 「" + o.Name + "」对**督军攻击**的影响 —— " + got);
                    Check(w.Has("blast"), o.Blast, "★ 「" + o.Name + "」给不给**督军 Blast** —— " + got);
                    Check(troop.Attack - tAtk, o.TAtk, "★ 「" + o.Name + "」对**部队近战**的影响 —— " + got);
                    Check(troop.RangedAttack - tRng, o.TRng, "★ 「" + o.Name + "」对**部队远程**的影响 —— " + got);
                    Check(w.Health - wHp, o.Heal,
                          "★ **治多少 = 用户 2026-09-14 的裁决**（本体 1 + 选中项自己印的那句）："
                          + "选 `" + o.Name + "` ⇒ " + o.Heal + " 点 —— " + got + LogTail(ctx));
                }
            }
        }

        // ---- ⑥ `Infinite Biomorphologies`（给**手牌**）—— 🆕 **2026-09-16 做出来了** ----
        //  🔴 这一节**原来是反例**（「如实报这一版没做，不静默」），当时作者留了一句话：
        //     「这条断言红了 = 有人把手牌加成做出来了 ⇒ 回来更新本节与那份文档」——
        //     现在正好是那一刻：`BattleContext.HandBuffs` + `GrantHandBuff` +
        //     `RuleCore.ApplyHandBuffs` 接上了（**回来更新了本节**；
        //     `资料/选牌Choose_数据与设计.md` §六 同步更新）。
        //  ⚠️ 做法**不是**引入卡实例身份：这张卡给的是「手牌里**所有**部队」，
        //     按「卡 + 份数」记账与实例身份**语义等价**（见 `BattleContext.HandBuff`）。
        //     结算级的断言（额度真的兑现、场上没被误加）在 `TestBatch0916` 的 ⑧。
        {
            var card = CreatePool.FindByName(pool, "Infinite Biomorphologies");
            CheckTrue(card != null, "卡池里有 `Infinite Biomorphologies`");
            if (card != null)
            {
                var troop = new CardDef("T_Hand2", "T_Hand2", "unit", "", "common", "Test",
                                        1, 2, 5, 0, null, subtype: "Infantry");
                var ctx = BattlePool(new[] { card, troop }, new[] { Unit("X", 1, 1, 5) }, pool,
                                     warlordFaction: "Leviathan");
                ToP1Turn(ctx, 1);
                ctx.Players[0].Energy = 12;
                CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "Infinite Biomorphologies"), -1),
                          RuleCodes.OK, "打出 `Infinite Biomorphologies`");
                CheckTrue(ctx.HandBuffs.Count > 0,
                          "★ **手牌上的额度记上了**（2026-09-16 之前这里是「这一版没做」）"
                          + LogTail(ctx));
                CheckTrue(ctx.HandBuffs.Count >= 1, "……而且是**一份一条**（第 7 行第 3 步：按实例记，不再按卡 + 份数）");
                {
                    var seenHb = new HashSet<CardInstance>();
                    int dupHb = 0;
                    foreach (var h in ctx.HandBuffs) if (h.Instance == null || !seenHb.Add(h.Instance)) dupHb++;
                    Check(dupHb, 0, "……没有一条重复挂在同一份上、也没有挂空的");
                }
            }
        }
    }

    /// <summary>
    /// 🆕 2026-09-14 A4 批 3 第 ① 条：**`Double the Melee Attack and Health of a friendly troop`**
    /// （`Possession`，Black Legion，8 费）。
    ///
    /// ⚠️ 语义里**两处是我们的取舍**（写在 `EffectResolver.DoDouble` 的注释里）：
    ///   ① 按**当前显示值** ×2（**不清**身上的增益 —— 原版那两个 handler 会先清）；
    ///   ② 生命**当前值连上限一起** ×2（依据是本工程既有的生命口径 `rule_core.gd:3317`，
    ///      **不是**从这张卡查到的）。
    /// **下面这些断言钉的就是这两条取舍** —— 哪天把原版口径查实了，这里会红，回来改。
    /// </summary>
    static void TestA4Batch3()
    {
        // ---- ① 解析层 ----
        {
            var r = EffectText.ParseSegment("Double the Melee Attack and Health of a friendly troop");
            Check(r.Kind, EffectText.SegKind.Ok, "`Double the Melee Attack and Health of …` 认得出");
            if (r.Ops != null && r.Ops.Count > 0)
            {
                Check(r.Ops[0].Verb, "double", "动词 = double");
                CheckTrue(r.Ops[0].Target != null && r.Ops[0].Target.Kind == "troop"
                          && r.Ops[0].Target.Count == 1,
                          "★ 目标是**一个己方部队**（要点一个）");
            }
        }
        //  ✅ **2026-09-15 翻面**（原来是「`Maulerfiend` 那句**不许**被当成 `double`」）。
        //  当年挡它的两条理由**现在都不成立**：
        //    ① 「按近战+生命翻会静默算错」→ 属性组合改由 **`Payload` 承载**
        //       （`melee,health` vs `melee,ranged`），算错的前提没了；
        //    ② 「它属未实现的 `ecstasy`」→ `ecstasy` 2026-09-14 做掉了。
        //  ⇒ **该认下来**。真打法在 `TestDoubleAndCreateCost`。
        {
            var ops = EffectText.Parse("Ecstasy 5: Double this troop's [Melee] and [Ranged].", out _, out _);
            EffectOp d = null;
            foreach (var o in ops) if (o.Verb == "double") d = o;
            CheckTrue(d != null,
                      "★ `Maulerfiend` 的 `Double this troop's [Melee] and [Ranged]` **认得出**了");
            if (d != null)
            {
                Check(d.Payload, "melee,ranged", "★ 翻的是**近战+远程**（不是近战+生命）");
                CheckTrue(d.Target != null && d.Target.Subjectless,
                          "★ 目标是**本部队自己**（`this troop's` ⇒ 走 `Subjectless`）");
            }
            // 反例：**别的属性组合一律不认**（不猜）—— 这条安全阀留着
            Check(EffectText.ParseSegment("Double this troop's Attack and Health").Kind,
                  EffectText.SegKind.Unknown,
                  "★ 反例：`Double this troop's Attack and Health`（不是那两种组合）**仍然不认**");
        }

        // ---- ② 结算层：真卡 `Possession`（Black Legion，8 费）----
        {
            var pool = CardDatabase.Load();
            var card = CreatePool.FindByName(pool, "Possession");
            CheckTrue(card != null, "卡池里有 `Possession`");
            if (card != null)
            {
                var ctx = BattlePool(new[] { card }, new[] { Unit("X", 1, 1, 5) }, pool,
                                     warlordFaction: "BlackLegion");
                ToP1Turn(ctx, 1);
                ctx.Players[0].Energy = 20;
                var mine = Place(ctx, 0, 0, Unit("MyTroop", 1, 3, 5));   // 攻 3 / 命 5/5
                mine.Health = 3;                                        // 打成**受伤**的 3/5
                var foe = Place(ctx, 1, 0, Unit("Enemy", 1, 2, 4));
                CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "Possession"), 0), RuleCodes.OK,
                          "打出真卡 `Possession`（点 0 号那个）");
                Check(mine.Attack, 6, "★ **近战攻击 ×2**（3 → 6）" + LogTail(ctx));
                Check(mine.MaxHealth, 10, "★ **生命上限 ×2**（5 → 10）" + LogTail(ctx));
                Check(mine.Health, 6,
                      "★ **当前生命也 ×2**（3 → 6）—— 注意这**不是**「治满」（治满会是 10），"
                      + "也不是「不动」（不动会是 3）" + LogTail(ctx));
                Check(foe.Attack, 2, "★ 反例：敌方单位一点没变（目标是**己方**部队）");
                Check(foe.MaxHealth, 4, "★ 反例：敌方单位的生命上限也没变");
            }
        }

        // ---- ③ `The next time it uses Ferocity this turn, it stays in play`（`Bjorn's Shrine`）----
        //  🔴 **出处**：反编译 `CardScript__UsedActiveAbility.c:52-64` —— `has(ferocity)` → **广播** →
        //     `has(dontReturnFerocity)`（`DefinedTrait:131 = 1270`）**或** `EnoughPendingDamageToDie`
        //     → **才**不回牌库；否则 `AddRecallToDeck`。`:75-88` 用完 `SendRemoveEffect(..., 1)` ⇒ **一次性**。
        //  ⚠️ 那是**两个各自独立**的短路 —— 「留在场上」只是其中一个，另一个是「本来就要死」，
        //     我们这边对应的是原来那个 `u.IsAlive && Board[slot] == u`。别把两者写成一个。
        {
            // 解析层
            var ops = EffectText.Parse(
                "Give +2 Health to a friendly unit. The next time it uses Ferocity this turn, it stays in play",
                out var un, out _);
            CheckTrue(un.Count == 0, "`Bjorn's Shrine` 整卡解析干净");
            Check(ops.Count, 2, "切成两条 op");
            if (ops.Count == 2)
            {
                Check(ops[1].Verb, "ferocitystay", "第二条动词 = ferocitystay");
                CheckTrue(ops[1].Target != null && ops[1].Target.Side == "prev",
                          "★ 主语 `it` 指**上一句那个友方单位**（不指代的话会标错人）");
            }
            // ★ 反例：`Bjorn the Fell-Handed`（SW42）写的是
            //   `When a friendly unit uses Ferocity, it stays in play` —— **常驻版**
            //   （无 `next time`、无 `this turn`），**另一张卡另一条语义**，归事件层。
            //   判据收宽了就会把它也按「一次性」处理。
            {
                var ops2 = EffectText.Parse("When a friendly unit uses Ferocity, it stays in play", out _, out _);
                bool got = false;
                foreach (var o in ops2) if (o.Verb == "ferocitystay") got = true;
                CheckTrue(!got, "★ 反例：`Bjorn the Fell-Handed` 的**常驻版**没被 `ferocitystay` 收走");
            }

            // 🆕 2026-09-16：常驻版的**正文那半句**现在收得下了（`SW42 Bjorn the Fell-Handed` 落地）。
            //   整句 `When <事件>, <正文>` 走**事件层**（`CardDef.AddWhenTrigger`），它只解析**正文**；
            //   而报表那关（`CardDef.HandledByOtherLayer`）要求**两边都成立** ⇒ 正文收不下来的话，
            //   那张卡会一直挂在「单位卡 ① 栏」（实测就是这样，见 `_tmp_view/unit_desc_unparsed.txt` 的历史）。
            {
                var r = EffectText.ParseSegment("it stays in play");
                Check(r.Kind, EffectText.SegKind.Ok, "常驻版的正文 `it stays in play` 认得了");
                Check(r.Ops[0].Verb, "ferocitystay", "……动词同样是 `ferocitystay`");
                Check(r.Ops[0].Payload, "always",
                      "★ 但带 `always` 标记（结算层据此换措辞；语义是**常驻**、不是「下一次」）");
            }

            // 结算层：**打了标记的留场、没打的照常洗回牌库**
            var pool = CardDatabase.Load();
            var card = CreatePool.FindByName(pool, "Bjorn's Shrine");
            CheckTrue(card != null, "卡池里有 `Bjorn's Shrine`");
            if (card != null)
            {
                var ctx = BattlePool(new[] { card }, new[] { Unit("X", 1, 1, 5) }, pool,
                                     warlordFaction: "SpaceWolves");
                ToP1Turn(ctx, 1);
                ctx.Players[0].Energy = 12;
                var fero = new CardDef("FFero", "FFero", "unit", "Ferocity: Gain +2 Attack",
                                       "common", "Test", 1, 1, 9, 0, new[] { "Ferocity" },
                                       subtype: "Infantry");
                var marked = Place(ctx, 0, 0, fero);
                Place(ctx, 0, 1, fero);                       // 同一张卡的另一个单位（不标记）

                CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "Bjorn's Shrine"), 0), RuleCodes.OK,
                          "打出真卡 `Bjorn's Shrine`（点 0 号那个）");
                CheckTrue(marked.FerocityStay, "★ 被点中的那个**拿到了「本回合下一次」标记**");

                CheckCode(RuleCore.UseAlternative(ctx, 0, 0, "ferocity"), RuleCodes.OK, "它用狂暴");
                CheckTrue(Board(ctx, 0, 0) != null, "★ **留在场上**（没洗回牌库）" + LogTail(ctx));
                CheckTrue(!marked.FerocityStay,
                          "★ 而且**标记用掉了** —— 「**下一次**」（`SendRemoveEffect(...,1)` 的对应物）；"
                          + "不消费的话它会「本回合每次狂暴都留场」");

                CheckCode(RuleCore.UseAlternative(ctx, 0, 1, "ferocity"), RuleCodes.OK,
                          "另一个（**没标记**的）用狂暴");
                CheckTrue(Board(ctx, 0, 1) == null,
                          "★ 反例：没标记的那个**照常洗回牌库**（规则书 :186）" + LogTail(ctx));
            }
        }

        // ---- ④ `For the rest of this battle, when a friendly troop uses Ferocity,
        //        deal 2 damage to the enemy Warlord`（`Raid Tactics`）----
        //  🔴 **出处**：事件名在 `rule_core.gd:3493`（`["ally_ferocity","you",
        //     r"a friendly (?:unit|troop) (?:triggers?|uses?) (?:a )?ferocity"]`，广播 `:2382`）；
        //     原版广播器 `BattleManagerSupport__BroadcastUnitFerocity.c`（**完整体**）。
        //  ⚠️ **容器形状是本工程自定的**：原版也是「**牌**在听」，而这张卡的监听者是
        //     **一张已经进弃牌堆的战术卡** —— 那一端在原版**同样找不到对应**
        //     （`BroadcastTacticPlayed` 也带 `IsInPlay` 检查）。如实标着，别当成查到了。
        {
            var ops = EffectText.Parse(
                "For the rest of this battle, when a friendly troop uses Ferocity, "
                + "deal 2 damage to the enemy Warlord", out var un, out _);
            CheckTrue(un.Count == 0, "`Raid Tactics` 解析干净");
            Check(ops.Count, 1, "一条 op");
            if (ops.Count == 1)
            {
                Check(ops[0].Verb, "persist", "动词 = persist（常驻）");
                CheckTrue(ops[0].When != null && ops[0].When.Kind == WhenEventKind.Triggers("ferocity"),
                          "★ 事件判据 = **`triggers:ferocity`**（事件短语交给 `WhenEvents.Parse` 复用，没另写一份）");
                CheckTrue(ops[0].AtTurnOps != null && ops[0].AtTurnOps.Count == 1
                          && ops[0].AtTurnOps[0].Verb == "deal",
                          "★ 正文 `deal 2 damage to the enemy Warlord` 也解析出来了");
                CheckTrue(ops[0].When == null || ops[0].When.Kind != "deploy",
                          "★ 反例：**没被当成部署型**（`Trigger` 该是 `when` 不是 `deploy`）");
            }

            var pool = CardDatabase.Load();
            var card = CreatePool.FindByName(pool, "Raid Tactics");
            CheckTrue(card != null, "卡池里有 `Raid Tactics`");
            if (card != null)
            {
                var ctx = BattlePool(new[] { card }, new[] { Unit("X", 1, 1, 5) }, pool,
                                     warlordFaction: "SpaceWolves");
                ToP1Turn(ctx, 1);
                ctx.Players[0].Energy = 12;
                var fero = new CardDef("FFero2", "FFero2", "unit", "Ferocity: Gain +2 Attack",
                                       "common", "Test", 1, 1, 9, 0, new[] { "Ferocity" },
                                       subtype: "Infantry");
                var foeW = ctx.Players[1].Warlord;
                int hp0 = foeW.Health;

                // ★ **反例先做**：还没登记常驻效果时，狂暴不该伤到敌方督军 ——
                //   钉住「这 2 点伤害是**常驻效果**给的，不是狂暴自带的」。
                Place(ctx, 0, 0, fero);
                CheckCode(RuleCore.UseAlternative(ctx, 0, 0, "ferocity"), RuleCodes.OK,
                          "先让一个友方部队用狂暴（`Raid Tactics` 还没登记）");
                Check(foeW.Health, hp0,
                      "★ 反例：**没登记时敌方督军一点没掉**" + LogTail(ctx));

                CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "Raid Tactics"), -1), RuleCodes.OK,
                          "打出真卡 `Raid Tactics`（这条不需要点目标）");
                CheckTrue(ctx.PersistentEffects.Exists(pe => pe.Trigger == "when"),
                          "★ 登记成了**事件型常驻效果**（`Trigger == \"when\"`，不是 `turn` / `deploy`）");

                Place(ctx, 0, 1, fero);
                CheckCode(RuleCore.UseAlternative(ctx, 0, 1, "ferocity"), RuleCodes.OK, "再让一个用狂暴");
                Check(hp0 - foeW.Health, 2,
                      "★ **敌方督军掉了 2 点** —— 「当友方部队用狂暴时」这条常驻效果**真的响了**"
                      + LogTail(ctx));
            }
        }
    }

    /// <summary>
    /// **A4 批 4**（2026-09-14）—— 战术卡最后几条里的两条，外加**顺手修掉的一类静默失效**
    /// （`… and trigger their &lt;X&gt; abilities` 那半句原来被吞进目标短语里）。
    ///
    /// 规格书 `资料/战术卡剩余7条_语义查证.md` §六～§八。
    /// ⚠️ 本批**改掉了 §七 的两条结论**（「你」是谁 · 原版有没有对应）——
    /// 依据是**卡图**（`Genestealer Cult/6破坏卡/IMG_3817.jpg` 的橙字 `Sabotage`）与
    /// `BattleManagerSupport__BroadcastCardPlayed`（**会遍历双方手牌**）。
    /// </summary>
    static void TestA4Batch4()
    {
        // ============================================================
        //  ① 解析：`Trigger the <关键词> ability/abilities/effect(s) of <目标>`
        // ============================================================
        {
            var r = EffectText.ParseSegment(
                "Trigger the Codex ability of a friendly unit and choose a Codicil and put it in your hand");
            Check(r.Kind, EffectText.SegKind.Ok, "`Author of the Codex` 那一句认得出");
            Check(r.Ops.Count, 2, "切成两条 op（强行触发 + 选牌尾句）");
            if (r.Ops.Count == 2)
            {
                Check(r.Ops[0].Verb, "triggerability", "第 1 条动词 = triggerability");
                Check(r.Ops[0].Payload, "codex", "★ 关键词 = `codex`");
                CheckTrue(r.Ops[0].Target != null && r.Ops[0].Target.Side == "own"
                          && r.Ops[0].Target.Kind == "unit" && r.Ops[0].Target.Count == 1,
                          "★ 目标是**一个己方单位**（要点一个）");
                Check(r.Ops[1].Verb, "choosecard", "第 2 条 = 选牌（**不是** `chooseeffect`）");
                Check(r.Ops[1].ChooseAct, "hand", "★ 去处 = 进手牌");
                CheckTrue((r.Ops[1].ChooseWhat ?? "").Contains("codicil"),
                          "★ 筛选词是 `Codicil`（候选池按 `subtype` 筛，3 张）");
            }
        }
        //  同一族的另一种写法：**不带尾句**（`Duty's End` 的反噬正文，原来整句不认）
        {
            var ops = EffectText.Parse("trigger the codex ability of all friendly units", out var un, out _);
            CheckTrue(un.Count == 0, "★ `Duty's End` 的反噬正文**解析干净了**（原来整句不认识）");
            Check(ops.Count, 1, "一条 op");
            if (ops.Count == 1)
            {
                Check(ops[0].Verb, "triggerability", "动词 = triggerability");
                Check(ops[0].Payload, "codex", "关键词 = codex");
                CheckTrue(ops[0].Target != null && ops[0].Target.Count == 0,
                          "★ 复数的 `all friendly units` = **全体**（`Count=0`，不要求玩家点）");
            }
        }
        //  尾句形态 `… and trigger their <X> abilities`（实测 5 张）——
        //  🔴 **原来那半句被吞进目标短语里**（`目标[your troops and trigger their teleport abilities]`），
        //     整句**判「认了」、卡面不打 `*`，而那半句永远不发生**。
        {
            var ops = EffectText.Parse("Heal 2 to all your units and trigger their Regiment abilities",
                                       out var un, out _);
            CheckTrue(un.Count == 0, "整句解析干净");
            Check(ops.Count, 2, "★ 切成两条 op —— 原来只有 1 条（`trigger their …` 被吞进目标里）");
            if (ops.Count == 2)
            {
                Check(ops[0].Verb, "heal", "第 1 条 = heal");
                CheckTrue(ops[0].Target != null && ops[0].Target.Kind == "unit",
                          "★ 第 1 条的目标**干净了**（不含 `and trigger …`）");
                Check(ops[1].Verb, "triggerability", "第 2 条 = triggerability");
                Check(ops[1].Payload, "regiment", "关键词 = regiment");
                CheckTrue(ops[1].Target != null && ops[1].Target.Kind == "unit",
                          "★ 第 2 条**继承了前半句的目标**（`Finish` 的反向共用目标）");
            }
        }
        //  一句话点名**两个**关键词（`Master Lazarus` 的 `Agenda:` 正文）
        {
            var ops = EffectText.Parse(
                "Agenda: Trigger the Teleport and Slay effects of a friendly unit and gain 1 Quest Point",
                out var un, out _);
            CheckTrue(un.Count == 0, "`Master Lazarus` 那句解析干净");
            Check(ops.Count, 2, "切成两条 op");
            if (ops.Count == 2)
            {
                Check(ops[0].Payload, "teleport,slay", "★ 两个关键词都在（逗号分隔，结算层逐个触发）");
                Check(ops[1].Verb, "gainquest",
                      "★ 尾句 `and gain 1 Quest Point` **没被吞掉**（原来它被当成载荷的一部分）");
            }
        }
        //  🔴 **2026-09-16 更正：这一条原来是「仍然不认识」的反例，它的前提已被推翻。**
        //     原写法是 `CheckTrue(un.Count == 1, "…仍然不认（卡池里 \`N [Spirit Stone]:\` 一张都没有
        //     ⇒ 认了也是永不生效的动词）")` —— **两半前提都不成立**：
        //       ① 卡池实测 **28 张**带 `N [Spirit Stone]:` 前缀（`CardDef.SpiritOps` 真的在收集）；
        //       ② 结算层现在**有**消费点（`DoTriggerAbility` 的 `SpiritStone` 分支，**触发不付费**，
        //          证据见 `BattleContext.SuppressCostDepth`）。
        //     ⇒ 按本工程那条纪律「红的断言不是删掉，是回来更新它」，改成**正面断言**。
        //     结算级的验证在 `TestBatch0916` 的 ② （真卡 `Cosmic Serpent` 触发 `Wraithblade` 的能力）。
        {
            var ops = EffectText.Parse("Trigger the abilities requiring Spirit Stones of all your troops.",
                                       out var un, out _);
            Check(un.Count, 0, "★ `Trigger the abilities requiring Spirit Stones …` **现在认了**"
                             + "（2026-09-16 之前判「不认识」）");
            Check(ops.Count, 1, "切成 1 条 op");
            if (ops.Count == 1)
            {
                Check(ops[0].Verb, "triggerability",
                      "动词 = `triggerability`（和 `Trigger the Codex ability of …` 同一条路）");
                Check(ops[0].Payload, "spiritstone", "载荷 = 规范关键词 `spiritstone`");
                CheckTrue(ops[0].Target != null && ops[0].Target.Side == "own",
                          "目标 = 己方（卡面 `all your troops`）");
            }
        }

        // ---- ② `SplitAndTail` 往后扫：载荷里自带 ` and ` 时不再吞掉后半句 ----
        {
            // 反例（非回归）：`an enemy and its adjacent units` —— 那是**目标的延续**，不切
            var ops = EffectText.Parse("Deal 3 damage to an enemy and its adjacent units", out _, out _);
            Check(ops.Count, 1, "★ 反例：`and its adjacent units` **不切**（不是动词开头）");
            // 该修好的那句（`Battle Sister`）：`heal 1` 原来被吞进载荷、**静默不发生**
            var ops2 = EffectText.Parse("Pray: Gain +1 Attack and +1 Ranged Attack and heal 1", out _, out _);
            Check(ops2.Count, 2, "★ `Battle Sister` 切成 2 条 —— 原来 `heal 1` 被塞进载荷里");
            if (ops2.Count == 2) Check(ops2[1].Verb, "heal", "第 2 条 = heal");
        }

        // ============================================================
        //  ③ 语义：`a Stratagem` = **战术大类**（`type ∈ {tactic, defence}`）
        // ============================================================
        //  🔴 我们原来把它当 `subtype == "Stratagem"`（**只有 6 张**）。权威实现
        //     `d:/warpforge/scripts/rule_core.gd` **三处一致**（`:950` / `:3852` / `:4695`）：
        //     `stratagem` = `tactic` 或 `defence`。规则书 `:72`（战术=非单位卡）· `:105` 同。
        {
            var pool = CardDatabase.Load();
            var jammed = CreatePool.FindByName(pool, "Jammed Communications");
            var acolyte = CreatePool.FindByName(pool, "Acolyte Leader");
            CheckTrue(jammed != null && acolyte != null,
                      "卡池里有 `Jammed Communications` 与 `Acolyte Leader`");
            if (jammed != null && acolyte != null)
            {
                CheckTrue(CreatePool.MatchesKind(jammed, "stratagem"),
                          "★ 战术卡算 `Stratagem`（判据 = `type ∈ {tactic, defence}`）");
                CheckTrue(!CreatePool.MatchesKind(acolyte, "stratagem"),
                          "★ 反例：**单位卡不算** Stratagem");
            }
        }

        // ============================================================
        //  ④ 结算：`Author of the Codex` —— 强行触发 codex 正文（**能量不为 0**）
        // ============================================================
        {
            var pool = CardDatabase.Load();
            var card = CreatePool.FindByName(pool, "Author of the Codex");
            var outrider = CreatePool.FindByName(pool, "Outrider");     // `Codex: Gain +1 Attack and +1 Ranged Attack`
            CheckTrue(card != null && outrider != null, "卡池里有 `Author of the Codex` 与 `Outrider`");
            if (card != null && outrider != null)
            {
                CheckTrue(outrider.TriggerOps(KeywordTable.Codex) != null,
                          "★ `Outrider` 的 `Codex:` 正文**收得下来了**"
                          + "（`codex` 进了 `RoutableTriggers`；不进的话这里恒为 null）");

                var ctx = BattlePool(new[] { card }, new[] { Unit("X", 1, 1, 9) }, pool,
                                     warlordFaction: "Ultramarines");
                // ⚠️ **只能用第 1 回合 + 手动加能量**：`Author of the Codex` 的卡面带 `Ephemeral.`
                //    ⇒ 推进到第 3 回合它**已经被 `SweepEphemeral` 扫走了**（实测：`HandIdx` 返回 -1）。
                //    手动加能量是为了让「付完 2 费还剩 >0」—— 那正是 Codex 平时**不**触发的条件。
                ToP1Turn(ctx, 1);
                ctx.Players[0].Energy = 20;
                var u = Place(ctx, 0, 0, outrider);
                int a0 = u.Attack;
                CheckTrue(ctx.Players[0].Energy > 0,
                          "能量不为 0 —— 这正是 Codex **平时不触发**的条件");
                CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "Author of the Codex"), 0),
                          RuleCodes.OK, "打出真卡 `Author of the Codex`");
                Check(u.Attack, a0 + 1,
                      "★ **强行触发了它的 Codex 正文**（攻 +1）—— 能量不为 0 也照触发"
                      + LogTail(ctx));
                CheckTrue(HasSubtype(ctx.Players[0].Hand, "Codicil"),
                          "★ 而且**拿到了一张 Codicil**（`choose a Codicil and put it in your hand`）");
            }
        }

        // ============================================================
        //  ④-bis 结算：**典籍（`Codex`）的自动触发点**（2026-09-14 A5）
        // ============================================================
        //  🔴 改之前：`Codex:` 的正文**只有** `Author of the Codex` 那一类「强行触发」会消费，
        //     **没有任何自动触发点** ⇒ 20 张带 `Codex` 的卡（10 张写前缀 + 10 张裸写）的正文
        //     **一条都不会自己发生**，而且报表上看不出来（解析得了、载荷也有机制）。
        //  语义照原版参考实现 `d:/warpforge/scripts/rule_core.gd:2397 _check_codex`（两条都照抄）：
        //     ① 能量**恰好为 0**；② 扫格位、**一次只触发第一个**带 `codex` 的单位（那边是 `break`）。
        {
            // ---- ① 裸写那一族：正文收得到，而且**挂着 `EnergyZero`**（两条路共用一份判据）----
            var pool = CardDatabase.Load();
            var bareCodex = new[] {
                "Epistolary Librarian", "Inceptor Sergeant", "Primaris Chaplain",
                "Primaris Judiciar", "Primaris Techmarine", "Sergeant Telion",
                "Sergeant Allectius", "Redemptor Dreadnought", "Stormtalon",
                "Predator Annihilator" };
            int got = 0; string miss = "";
            foreach (string n in bareCodex)
            {
                var c = CreatePool.FindByName(pool, n);
                if (c == null) { miss += n + "(卡池里没有) "; continue; }
                var cops = c.TriggerOps(KeywordTable.Codex);
                if (cops == null) { miss += n + " "; continue; }
                bool cond = true;
                foreach (var o in cops) if (o.ConditionKind != EffectCondition.EnergyZero) cond = false;
                if (!cond) { miss += n + "(没挂 EnergyZero) "; continue; }
                got++;
            }
            Check(got, bareCodex.Length,
                  "★ 裸写 `Codex` 正文的 10 张**全收得到、且挂着 `EnergyZero`**"
                  + "（`codex` 不在 `BodyKeywords` 里 ⇒ 恒为 null；裸写不挂条件 ⇒ 随时触发）"
                  + (miss.Length > 0 ? "　缺：" + miss : ""));

            // ⚠️ **反例：方括号写法 `[Codex]` 不能被当「裸写」**
            //    `Death from Above` 的 desc 是 `Deal 4 damage. [Codex] Deal 1 additional damage.` ——
            //    那半句是**主效果的附加条件**（正常 desc 解析就带着 `EnergyZero` 条件结算），
            //    **不是**一张独立的 Codex 能力卡。守卫不认方括号 ⇒ 会把整条 desc（含 `Deal 4 damage`）
            //    当成 Codex 正文收进去（实测撞到，2026-09-14）。
            var dfa = CreatePool.FindByName(pool, "Death from Above");
            CheckTrue(dfa != null, "卡池里有 `Death from Above`");
            if (dfa != null)
                CheckTrue(dfa.TriggerOps(KeywordTable.Codex) == null,
                          "★ `[Codex] …` 那种写法**不算裸写正文**（守卫认方括号 = 认前缀）");

            // ---- ② 自动触发：**能量恰好打光** ⇒ 触发 ----
            var cx = new CardDef("FixtureCodex", "FixtureCodex", "unit",
                                 "Codex: Gain +1 Attack", "common", "Test",
                                 1, 2, 9, 0, new[] { "Codex" });
            {
                var ctx = ProbeBattle(new[] { cx }, new[] { Unit("EFoe", 1, 0, 30) });
                ToP1Turn(ctx, 1);
                ctx.Players[0].Energy = 1;                 // 付完这 1 费**恰好为 0**
                CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "FixtureCodex"), 0),
                          RuleCodes.OK, "打出夹具单位（1 费，能量恰好打光）");
                var u = Board(ctx, 0, 0);
                CheckTrue(u != null, "夹具单位在场上");
                if (u != null)
                    Check(u.Attack, 3, "★ 能量**恰好为 0** ⇒ 自动触发了 Codex 正文（攻 2→3）"
                          + LogTail(ctx));
            }

            // ---- ③ 反例：付完**还有富余** ⇒ 不触发 ----
            {
                var ctx = ProbeBattle(new[] { cx }, new[] { Unit("EFoe", 1, 0, 30) });
                ToP1Turn(ctx, 1);
                ctx.Players[0].Energy = 4;                 // 付完 1 费还剩 3
                CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "FixtureCodex"), 0),
                          RuleCodes.OK, "打出夹具单位（能量有富余）");
                var u = Board(ctx, 0, 0);
                if (u != null)
                    Check(u.Attack, 2, "★ 反例：能量不为 0 ⇒ Codex **不**自动触发（攻仍为 2）"
                          + LogTail(ctx));
            }

            // ---- ④ **一次只触发第一个**（原版那条 `break`，别改成「全体各来一次」）----
            {
                var ca = new CardDef("FixtureCodexA", "FixtureCodexA", "unit",
                                     "Codex: Gain +1 Attack", "common", "Test",
                                     1, 2, 9, 0, new[] { "Codex" });
                var cb = new CardDef("FixtureCodexB", "FixtureCodexB", "unit",
                                     "Codex: Gain +1 Attack", "common", "Test",
                                     1, 2, 9, 0, new[] { "Codex" });
                var sp = Tactic("FixtureSpend2", 2, "Deal 1 damage to a random enemy");
                var ctx = ProbeBattle(new[] { sp }, new[] { Unit("EFoe", 1, 0, 30) });
                ToP1Turn(ctx, 1);
                Place(ctx, 0, 0, ca);
                Place(ctx, 0, 1, cb);
                Place(ctx, 1, 1, Unit("EFoe", 1, 0, 30), exhausted: true);
                ctx.Players[0].Energy = 2;                 // 打出这张战术卡之后归零
                CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "FixtureSpend2"), 0),
                          RuleCodes.OK, "打出一张把能量打光的战术卡");
                Check(Board(ctx, 0, 0).Attack, 3, "★ 0 号格那个**触发了**");
                Check(Board(ctx, 0, 1).Attack, 2,
                      "★ 1 号格那个**不触发**（原版 `break`：一次只触发一个）" + LogTail(ctx));
            }
        }

        // ============================================================
        //  ④-ter 结算：**棋盘单位自己的回合起止效果**（2026-09-14 A5 批 2）
        // ============================================================
        //  🔴 改之前 `ResolveAtTurn` 只有两个触发源（当前行动方**手牌**里的陷阱卡 + 已登记的
        //     **常驻效果**）—— 参考实现 `rule_core.gd:397-442 _at_turn_effects` 的**触发源②**
        //     「**双方棋盘 at-turn 单位**」整层没做 ⇒ `Chronomancer` / `Grot Orderly` /
        //     `Beast Snagga Nob` / `Aquilon Servo-Sentry` 这些**单位自己的**回合起止正文
        //     **一条都不会发生**，报表上还看不出来（desc 解析得出来、载荷也有机制）。
        //  两条语义照 `rule_core.gd:344` / `:441`：`each`（`each|every turn`）= **双方回合**都触发，
        //  `you` = **只在控制者自己**的回合触发；后缀式 `… at the start|end of your turn` 也认。
        {
            // ---- ① 夹具：`you` 视角 —— 自己回合结束触发、**对手回合结束不触发** ----
            var cu = new CardDef("FixtureAtTurnYou", "FixtureAtTurnYou", "unit",
                                 "At the end of your turn, Gain +1 Attack", "common", "Test",
                                 1, 2, 9, 0, null);
            {
                var ctx = ProbeBattle(new[] { Unit("EFillerA", 1, 0, 1) },
                                      new[] { Unit("EFoe", 1, 0, 30) });
                ToP1Turn(ctx, 1);
                var u = Place(ctx, 0, 0, cu);
                RuleCore.EndTurn(ctx);
                Check(u.Attack, 3, "★ 单位自己的 `At the end of your turn, …` **真的触发了**"
                      + "（改前没有任何消费点）" + LogTail(ctx));
                RuleCore.BeginTurn(ctx);                 // 轮到对手
                RuleCore.EndTurn(ctx);                   // 对手的回合结束
                Check(u.Attack, 3, "★ 反例：`your turn` 视角 —— **对手回合结束不触发**"
                      + LogTail(ctx));
            }

            // ---- ② 夹具：`each` 视角 —— **双方回合都触发** ----
            {
                var ce = new CardDef("FixtureAtTurnEach", "FixtureAtTurnEach", "unit",
                                     "At the end of each turn, Gain +1 Attack", "common", "Test",
                                     1, 2, 9, 0, null);
                var ctx = ProbeBattle(new[] { Unit("EFillerB", 1, 0, 1) },
                                      new[] { Unit("EFoe", 1, 0, 30) });
                ToP1Turn(ctx, 1);
                var ue = Place(ctx, 0, 1, ce);
                RuleCore.EndTurn(ctx);
                Check(ue.Attack, 3, "★ `each turn`：自己回合结束触发（2→3）");
                RuleCore.BeginTurn(ctx);
                RuleCore.EndTurn(ctx);
                Check(ue.Attack, 4, "★ `each turn`：**对手**回合结束**也**触发（3→4）"
                      + LogTail(ctx));
            }

            // ---- ③ 夹具：**后缀式** `Takes 1 damage at the start of your turn` ----
            //     （真卡是 `Concealed Explosives`；参考实现的 `re_suf` 认这一式）
            {
                var cs = new CardDef("FixtureAtTurnSuffix", "FixtureAtTurnSuffix", "unit",
                                     "Takes 1 damage at the start of your turn", "common", "Test",
                                     1, 2, 9, 0, null);
                var ctx = ProbeBattle(new[] { Unit("EFillerC", 1, 0, 1) },
                                      new[] { Unit("EFoe", 1, 0, 30) });
                ToP1Turn(ctx, 1);
                var us = Place(ctx, 0, 2, cs);
                int h0 = us.Health;
                RuleCore.EndTurn(ctx);
                RuleCore.BeginTurn(ctx);                 // 轮到对手 ⇒ `you` 视角不该触发
                Check(us.Health, h0, "★ 后缀式 + `you` 视角：**对手**回合开始不触发");
                RuleCore.EndTurn(ctx);
                RuleCore.BeginTurn(ctx);                 // 又轮到自己
                Check(us.Health, h0 - 1, "★ 自己回合开始 ⇒ **受 1 点伤害**（后缀式认出来了）"
                      + LogTail(ctx));
            }

            // ---- ④ 真卡：两张 `each turn` + 两张 `your turn` + 一张后缀式 ----
            {
                var pool = CardDatabase.Load();
                var cases = new[] {
                    new[] { "Chronomancer",         "turn_end",   "you",  "pre" },
                    new[] { "Aquilon Servo-Sentry", "turn_end",   "each", "pre" },
                    new[] { "Unleashed TramplaSquig","turn_start","each", "pre" },
                    new[] { "Concealed Explosives", "turn_start", "you",  "suf" },
                };
                int ok = 0; string bad = "";
                foreach (var cs2 in cases)
                {
                    var c = CreatePool.FindByName(pool, cs2[0]);
                    if (c == null) { bad += cs2[0] + "(卡池里没有) "; continue; }
                    bool hit = false;
                    foreach (var cl in EffectText.AtTurnClauses(c.Desc))
                        if (cl.Phase == cs2[1] && cl.View == cs2[2] && cl.Form == cs2[3]) hit = true;
                    if (hit) ok++; else bad += cs2[0] + " ";
                }
                Check(ok, cases.Length,
                      "★ 4 张真卡的回合起止从句都认得出（含 `each` 视角与**后缀式**）"
                      + (bad.Length > 0 ? "　缺：" + bad : ""));

                // 主语省略的 `Takes 1 damage`（`Concealed Explosives`）现在也收得到 ——
                // 口径与 `Heal N` 没写目标 = 自愈 一致（`EffectTargetSpec.Subjectless`）
                var ce = CreatePool.FindByName(pool, "Concealed Explosives");
                if (ce != null)
                    CheckTrue(EffectText.IsFullyParsed(ce.Desc),
                              "★ `Concealed Explosives` 整条 desc **完全解析**"
                              + "（主语省略的 `Takes 1 damage` 原来判不认识 ⇒ 那句**永远不生效**）");
            }
        }

        // ============================================================
        //  ④-quater 结算：`After receiving a Dark Pact, …`（**没有 `When` 前缀**）
        // ============================================================
        //  2026-09-14 A5 批 2。真卡 3 张（`Chaos Legionary` / `Meltagun Legionary` /
        //  `Aspiring Champion`）卡面把事件写成**介词短语**，`AddWhenTrigger` 只认 `When ` 开头
        //  ⇒ 它们一条监听器都没注册、效果**永远不发生**。
        //  语义 = **这张卡自己**收到黑暗契约（`SelfOnly`，和 `When deployed, …` 同一条路）。
        {
            var pool = CardDatabase.Load();

            // ---- ① 三张真卡都挂上了事件层 ----
            var dpNames = new[] { "Chaos Legionary", "Meltagun Legionary", "Aspiring Champion" };
            int dpOk = 0; string dpBad = "";
            foreach (string n in dpNames)
            {
                var c = CreatePool.FindByName(pool, n);
                if (c == null) { dpBad += n + "(卡池里没有) "; continue; }
                bool hit = false;
                foreach (string seg in EffectText.Split(c.Desc))
                    if (CardDef.HandledByOtherLayer(c, seg) == "事件层（WhenTriggers）") hit = true;
                if (hit) dpOk++; else dpBad += n + " ";
            }
            Check(dpOk, dpNames.Length,
                  "★ `After receiving a Dark Pact, …` 的三张真卡**都挂上了事件层**"
                  + "（认不出就一条监听器都没有 ⇒ 效果永不发生）"
                  + (dpBad.Length > 0 ? "　缺：" + dpBad : ""));

            // ---- ② 结算：自己拿到契约 ⇒ 真触发 ----
            //   夹具把「拿契约」与「收到契约后」写在同一张卡上（真卡里 `Chaos Sergeant` 那种组合）：
            //   打出时 `Rally: Gain a Dark Pact` 给自己发一张 ⇒ 事件广播 ⇒ 第二句触发。
            {
                var dp = new CardDef("FixtureDarkPact", "FixtureDarkPact", "unit",
                                     "Rally: Gain a Dark Pact. "
                                     + "After receiving a Dark Pact, Stun a random enemy",
                                     "common", "Test", 1, 2, 9, 0, new[] { "Rally" });
                var ctx = Battle(new[] { dp }, new[] { Unit("EFillerDP", 1, 0, 1) });
                ToP1Turn(ctx, 1);
                var e = Place(ctx, 1, 1, Unit("EFoeDP", 1, 0, 30), exhausted: true);
                CheckTrue(dp.WhenTriggers.Count >= 1,
                          "★ `After receiving a Dark Pact, …` **注册成监听器**了");
                CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "FixtureDarkPact"), 0),
                          RuleCodes.OK, "打出夹具（`Rally: Gain a Dark Pact`）");
                // `Stun a random enemy` 在**所有**敌人里随机（可能落到督军身上）⇒ 按「有没有人被晕」判
                bool anyStunned = e.IsStunned;
                for (int s = 0; s < BoardSpec.Size && !anyStunned; s++)
                {
                    var w = Board(ctx, 1, s);
                    if (w != null && w.IsStunned) anyStunned = true;
                }
                CheckTrue(anyStunned,
                          "★ 自己收到黑暗契约 ⇒ **里面那句真的触发了**（对面有单位被眩晕）"
                          + LogTail(ctx));
            }

            // ---- ③ **缺逗号**的 `When <事件> <正文>`（`Eliminator Sergeant`）----
            //    ⚠️ 全池只有这 1 条；切点由两个现成解析器判（见 `TryParseWhenSentence`）。
            var elim = CreatePool.FindByName(pool, "Eliminator Sergeant");
            CheckTrue(elim != null, "卡池里有 `Eliminator Sergeant`");
            if (elim != null)
            {
                CheckTrue(elim.WhenTriggers.Count >= 1,
                          "★ `When you deploy an Eliminator give it Stealth`（**缺逗号**）"
                          + "也注册成监听器了 —— 切点靠「事件短语与正文两边都解析得出」判出来");
                // ⚠️ **尺子要用 `HandledByOtherLayer`，不是 `IsFullyParsed`**：
                //    后者只问 `EffectText` 自己认不认，而**事件层本来就在它外面**
                //    （这正是 A5 批 1 加 `HandledByOtherLayer` 的原因）。
                Check(CardDef.HandledByOtherLayer(elim, elim.Desc), "事件层（WhenTriggers）",
                      "★ 而且覆盖率报告里这一句算「**事件层接手了**」（不再挂在 ① 栏）");
            }

            // ---- ④ `Whenever …` ≡ `When …`（`Neurotyrant`）----
            var neuro = CreatePool.FindByName(pool, "Neurotyrant");
            CheckTrue(neuro != null, "卡池里有 `Neurotyrant`");
            if (neuro != null)
                CheckTrue(neuro.WhenTriggers.Count >= 1,
                          "★ `Whenever you play a non-Ephemeral Stratagem, …` 注册成监听器了");

            // ---- ⑤ 结算：`non-Ephemeral` 限定**真的生效** ----
            //    `Ephemeral` 战术卡**不算**（判据照 `rule_core.gd:2170`：读卡面 keyword）
            {
                var watcher = new CardDef("FixtureNonEph", "FixtureNonEph", "unit",
                                          "When you play a non-Ephemeral Stratagem, gain +1 Attack",
                                          "common", "Test", 1, 2, 9, 0, null);
                var plain = Tactic("T_PlainSpellNE", 0, "Draw a card");
                var eph = new CardDef("T_EphSpellNE", "T_EphSpellNE", "tactic", "Draw a card",
                                      "common", "Test", 0, 0, 0, 0, new[] { "Ephemeral" });
                var ctx = Battle(new[] { watcher, plain, eph }, new[] { Unit("EFoeNE", 1, 0, 30) });
                ToP1Turn(ctx, 1);
                var w = Place(ctx, 0, 0, watcher);
                int a0 = w.Attack;
                CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_EphSpellNE"), -1),
                          RuleCodes.OK, "打出一张 **Ephemeral** 战术卡");
                Check(w.Attack, a0, "★ 反例：`Ephemeral` 战术卡**不**触发（`non-Ephemeral` 限定）"
                      + LogTail(ctx));
                CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_PlainSpellNE"), -1),
                          RuleCodes.OK, "打出一张普通战术卡");
                Check(w.Attack, a0 + 1, "★ 普通战术卡**触发**（`Whenever you play a Stratagem`）"
                      + LogTail(ctx));
            }

            // ---- ⑥ `Heal N and gain …` 的**尾句共目标**（2026-09-14 A5 批 2）----            //    `give` / `gain` / `lose` 本来就是同一套载荷机制，原来只有 `give` 能接目标
            //    ⇒ 这两句整句判「半懂」、卡面打 `*`，而它们该是「治自己 + 给自己加关键词」。
            {
                var ops = EffectText.Parse("Heal 5 and gain Vanguard until your next turn",
                                           out _, out var partial2);
                Check(partial2.Count, 0,
                      "★ `Heal 5 and gain Vanguard until your next turn`（`Captain Sicarius` 的 Codex 句）"
                      + "**不再半懂**");
                Check(ops.Count, 2, "两条 op（heal + gain）");
                if (ops.Count == 2)
                    CheckTrue(ops[0].Target != null,
                              "★ `heal` **接到了尾句的目标**（尾巴是 `gain` 也认了）");
            }
        }

        // ============================================================
        //  ④-quinquies 结算：**「被这一下打到的那个」**（`… attacked [by this unit]`，A5 批 3）
        // ============================================================
        //  全池 **6 张、一个关键词都没有**：`Venomthrope` · `Blastmaster Noise Marine` ·
        //  `Sonic Blaster Noise Marine` · `Stikkbomb Boy` · `Snakebite Grot` · `Arjac Rockfist`。
        //  原版触发点 `AbilityTrigger.UnitAttack = 50`（和 `Slay`/`Strike` 同一个函数）——
        //  引擎原来**没有任何消费点** ⇒ 这 6 张的正文一条都不会发生，而报表上看不出来。
        {
            // ---- ① 解析：标记 + **筛选条件不许被后缀吃掉** ----
            var pa = EffectText.Parse("Destroy any troop attacked by this unit", out _, out _);
            Check(pa.Count, 1, "`Destroy any troop attacked by this unit` 解析出 1 条");
            if (pa.Count == 1)
                CheckTrue(pa[0].Target != null && pa[0].Target.AttackedBySelf,
                          "★ 目标带着 `AttackedBySelf` 标记（锚在**被打者**身上，不是「让玩家点一个」）");
            var pb = EffectText.Parse("Destroy any enemy troop with Armour attacked by this unit",
                                      out _, out _);
            Check(pb.Count, 1, "带 `with Armour` 的那句也解析出 1 条");
            if (pb.Count == 1)
                Check(pb[0].Target.KeywordFilter, "armour",
                      "★ `with Armour` 这个**筛选条件没被后缀吃掉**");

            // ---- ② 结算：被打的那个**被摧毁**（血厚也不是被伤害打死的）----
            {
                var killer = new CardDef("FixtureAttacker", "FixtureAttacker", "unit",
                                         "Destroy any troop attacked by this unit",
                                         "common", "Test", 1, 2, 9, 0, null);
                var ctx = Battle(new[] { Unit("AF", 1, 1, 1) }, new[] { Unit("XF", 1, 1, 1) });
                ToP1Turn(ctx, 1);
                Place(ctx, 0, 0, killer);
                Place(ctx, 1, 0, Unit("Big", 1, 0, 30));       // 30 血，只能靠正文摧毁
                CheckCode(RuleCore.DeclareAttack(ctx, 0, 0, 1, 0), RuleCodes.OK, "夹具发起攻击");
                CheckTrue(Board(ctx, 1, 0) == null,
                          "★ 被这一下打到的那个**被摧毁了**（30 血不是被那 2 点伤害打死的）"
                          + LogTail(ctx));
            }

            // ---- ③ 反例：`with Armour` —— **没护甲的活下来**（筛选真在判，不是「见谁打谁」）----
            {
                var killer2 = new CardDef("FixtureAttackerArm", "FixtureAttackerArm", "unit",
                                          "Destroy any enemy troop with Armour attacked by this unit",
                                          "common", "Test", 1, 2, 9, 0, null);
                var ctx = Battle(new[] { Unit("AF2", 1, 1, 1) }, new[] { Unit("XF2", 1, 1, 1) });
                ToP1Turn(ctx, 1);
                Place(ctx, 0, 0, killer2);
                Place(ctx, 1, 0, Unit("NoArm", 1, 0, 30));                    // 无护甲
                CheckCode(RuleCore.DeclareAttack(ctx, 0, 0, 1, 0), RuleCodes.OK, "攻击**无护甲**目标");
                CheckTrue(Board(ctx, 1, 0) != null,
                          "★ 反例：**没有 Armour 的活下来了**（拿掉筛选就是「打得比卡面宽」）" + LogTail(ctx));
            }
            {
                var killer3 = new CardDef("FixtureAttackerArm2", "FixtureAttackerArm2", "unit",
                                          "Destroy any enemy troop with Armour attacked by this unit",
                                          "common", "Test", 1, 2, 9, 0, null);
                var ctx = Battle(new[] { Unit("AF3", 1, 1, 1) }, new[] { Unit("XF3", 1, 1, 1) });
                ToP1Turn(ctx, 1);
                Place(ctx, 0, 0, killer3);
                Place(ctx, 1, 0, Unit("Arm", 1, 0, 30, "Armour 2"));          // 有护甲
                CheckCode(RuleCore.DeclareAttack(ctx, 0, 0, 1, 0), RuleCodes.OK, "攻击**有护甲**目标");
                CheckTrue(Board(ctx, 1, 0) == null,
                          "★ 有 `Armour` 的那个**被摧毁了**（同一个判据的另一边）" + LogTail(ctx));
            }

            // ---- ④ `Stun … attacked and give them …` —— **尾巴不许被目标短语吞掉** ----
            {
                var ps = EffectText.Parse(
                    "Stun enemy troops attacked and give them -1 [armor] and -1 [attack]",
                    out _, out _);
                Check(ps.Count, 2,
                      "★ `Sonic Blaster` 那句解析出 **2 条**（stun + give）—— "
                      + "原来整条尾巴被吞进目标短语、`give` 那半句**静默不发生**");
                if (ps.Count == 2)
                {
                    Check(ps[0].Verb, "stun", "第 1 条 = stun");
                    Check(ps[1].Verb, "give", "第 2 条 = give（`them` 指被打的那个）");
                }
            }

            // ---- ⑤ 六张真卡**都收到了正文** ----
            {
                var pool = CardDatabase.Load();
                var names = new[] { "Venomthrope", "Blastmaster Noise Marine",
                                    "Sonic Blaster Noise Marine", "Stikkbomb Boy",
                                    "Snakebite Grot", "Arjac Rockfist" };
                int okA = 0; string badA = "";
                foreach (string n in names)
                {
                    var c = CreatePool.FindByName(pool, n);
                    if (c == null) { badA += n + "(卡池里没有) "; continue; }
                    if (c.AttackedOps != null) okA++; else badA += n + " ";
                }
                Check(okA, names.Length,
                      "★ `… attacked [by this unit]` 的 **6 张真卡都收到正文了**"
                      + "（原来 `AttackedOps` 恒为 null ⇒ 效果永不发生）"
                      + (badA.Length > 0 ? "　缺：" + badA : ""));
            }
        }

        // ============================================================
        //  ④-sexies 批 3 的四处载荷缺口（2026-09-14 A5 批 3）
        // ============================================================
        //  每一条都是「句子解析不出来 ⇒ 那个效果**永远不发生**」，而机制本身早就有：
        //   · `Returns to your hand`（**没写主语** = 本卡自己回手）
        //   · `Create a random Ultramarines card **in hand**`（省略 `your` 的目的地写法）
        //   · `Draw the **next** Stratagem **in your deck**`（`next` 被当成类型词）
        //   · `[Talent]: Catechism of Death`（方括号写法，`Talent:` 本来已实现）
        {
            // ---- ① 没写主语的 `return to <目的地>` ----
            {
                var rops = EffectText.Parse("return to your hand", out _, out _);
                Check(rops.Count, 1, "`return to your hand` 解析出 1 条");
                if (rops.Count == 1)
                    CheckTrue(rops[0].Target != null && rops[0].Target.Subjectless,
                              "★ 目标是 `Subjectless`（有施放者 = 它自己回手）");

                // 真卡结算：`Grot Orderly` 的 `At the start of your turn, return to your hand`
                var pool = CardDatabase.Load();
                var grot = CreatePool.FindByName(pool, "Grot Orderly");
                CheckTrue(grot != null, "卡池里有 `Grot Orderly`");
                CheckTrue(grot != null && EffectText.IsFullyParsed(grot.Desc),
                          "★ `Grot Orderly` 整条 desc **完全解析**（原来尾句认不出）");
                if (grot != null)
                {
                    var ctx = Battle(new[] { Unit("GO", 1, 1, 1) }, new[] { Unit("XO", 1, 1, 1) });
                    ToP1Turn(ctx, 1);
                    Place(ctx, 0, 0, grot);
                    RuleCore.EndTurn(ctx);
                    RuleCore.BeginTurn(ctx);            // 对手回合
                    CheckTrue(Board(ctx, 0, 0) != null, "反例：**对手**回合开始不触发（`you` 视角）");
                    RuleCore.EndTurn(ctx);
                    RuleCore.BeginTurn(ctx);            // 回到自己回合
                    CheckTrue(Board(ctx, 0, 0) == null,
                              "★ 自己回合开始 ⇒ **回手了**（格位空了）" + LogTail(ctx));
                    CheckTrue(HandIdx(ctx, 0, "Grot Orderly") >= 0, "★ 而且**回到手牌里**了");
                }
            }

            // ---- ② `create … in hand`（省略 `your`）----
            {
                var cops = EffectText.Parse("Create a random Ultramarines card in hand", out _, out _);
                Check(cops.Count, 1, "`Create a random Ultramarines card in hand` 解析出 1 条");
                if (cops.Count == 1)
                {
                    Check(cops[0].Verb, "create", "动词 = create");
                    Check(cops[0].Dest, "hand", "★ 目的地 = 手牌（少了 `in hand` 这条整句不认识）");
                }
            }

            // ---- ③ `Draw the next <类型> in your deck` ----
            {
                var dops = EffectText.Parse("Draw the next Stratagem in your deck", out _, out _);
                Check(dops.Count, 1, "`Draw the next Stratagem in your deck` 解析出 1 条");
                if (dops.Count == 1)
                {
                    Check(dops[0].Verb, "drawtype", "动词 = drawtype（定向翻找）");
                    Check(dops[0].Payload, "stratagem", "★ 类型词 = `stratagem`（`next` 不再被当成类型词）");
                }
            }

            // ---- ④ `[Talent]: <名>` 方括号写法 ----
            {
                var pool2 = CardDatabase.Load();
                var cassius = CreatePool.FindByName(pool2, "Chaplain Cassius");
                CheckTrue(cassius != null, "卡池里有 `Chaplain Cassius`");
                if (cassius != null)
                    Check(CardDef.HandledByOtherLayer(cassius, "[Talent]: Catechism of Death"),
                          "天赋（TalentName）",
                          "★ `[Talent]: …` 方括号写法算「天赋层接手了」（`ExtractTalent` 归一了方括号）");
            }

            // ---- ⑤ 数据修正：`Lord Exultant` 的 `Stimulation`（卡面亲读，铁律 7）----
            //    卡面印的是 `[图标] Stimulation: Give +1 [拳] and +1 [枪] to all friendly troops`，
            //    我们的卡表把 `Stimulation` **整个丢了**（keywords 为空）⇒ 正文没人认领、机制也不跑
            //    （`RuleCore` 的 Stimulation 触发要求**单位带这个关键词**）。
            //    修法走 `cardface_fixes.json` 的 `_manual_keywords`（手工覆盖列）。
            {
                var pool3 = CardDatabase.Load();
                var le = CreatePool.FindByName(pool3, "Lord Exultant");
                CheckTrue(le != null, "卡池里有 `Lord Exultant`");
                if (le != null)
                {
                    CheckTrue(le.Has(KeywordTable.Stimulation),
                              "★ 带 `Stimulation` 关键词（卡面印着，我们原来丢了）");
                    CheckTrue(le.TriggerOps(KeywordTable.Stimulation) != null,
                              "★ 正文也收得到（裸写 → `CollectBareKeywordBody`）");

                    // 结算：被战术选中 ⇒ 触发（`Stimulation` = 被战术选中时、**结算前**）
                    var buff = Tactic("T_FixtureStim", 0, "Give +1 Health to a friendly troop");
                    var ctx = Battle(new[] { buff }, new[] { Unit("XS", 1, 0, 9) });
                    ToP1Turn(ctx, 1);
                    Place(ctx, 0, 0, le);
                    var mate = Place(ctx, 0, 1, Unit("Mate", 1, 2, 9));
                    int m0 = mate.Attack;
                    CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_FixtureStim"), 0),
                              RuleCodes.OK, "打出一张**选中它**的战术卡");
                    Check(mate.Attack, m0 + 1,
                          "★ 被战术选中 ⇒ `Stimulation` 正文触发（己方部队 +1 攻）" + LogTail(ctx));
                }
            }

            // ---- ⑥ `it heals N` —— **代词当主语**（`Apothecary`，2026-09-14 A5 批 3）----
            //    `When a friendly unit obtains [Shield], **it** heals 2` ——
            //    「it」= **拿到盾的那个**（事件主语由 `BroadcastWhen` 从 `seed` 种进 `LastTargets`）。
            //    只放行**代词**：条件从句当主语那条坑（`When another troop dies, heals 2`）靠这条边界挡住。
            {
                var hops = EffectText.Parse("it heals 2", out _, out _);
                Check(hops.Count, 1, "`it heals 2` 解析出 1 条");
                if (hops.Count == 1)
                {
                    Check(hops[0].Verb, "heal", "动词 = heal");
                    Check(hops[0].Amount, 2, "点数 = 2");
                    CheckTrue(hops[0].Target != null && hops[0].Target.Side == "prev",
                              "★ 主语是代词 ⇒ 目标走 `prev`（= 事件主语）");
                }
                // ⚠️ 反例：**一般名词短语不许当主语**（那是本工程记过的坑）
                //    判据 = **一条 op 都不产出、而且如实报出来**（不认识或半懂都行，反正不许「认了」）
                var bad = EffectText.Parse("another troop heals 2", out var badUnparsed,
                                           out var badPartial);
                CheckTrue(bad.Count == 0 && (badUnparsed.Count > 0 || badPartial.Count > 0),
                          "★ 反例：`another troop heals 2` **仍判不认识**（不放行一般名词短语）");

                // 真卡结算：`Apothecary` 的 `When a friendly unit obtains [Shield], it heals 2`
                var poolA = CardDatabase.Load();
                var apo = CreatePool.FindByName(poolA, "Apothecary");
                CheckTrue(apo != null, "卡池里有 `Apothecary`");
                if (apo != null)
                    CheckTrue(apo.WhenTriggers.Count >= 1,
                              "★ 那条监听器注册上了（正文原来认不出 ⇒ 一条都没注册）");
            }

            // ---- ⑦ **跨句触发正文**（2026-09-14 A5 批 3）----
            //    参考实现的分段规则是「前缀 `:` 后到下一前缀为止，段内句号不断段」
            //    （`rule_core.gd:291`）⇒ 正文可以跨句。实测 42 张卡受影响、其中 18 条尾句解析得出。
            //    ⚠️ **但不能一直收到 desc 结尾**：那 42 张里有 12 张的尾句是**另一件事**
            //    （`Talent:` / `When` / 回合起止 / 付费激活前缀），照抄会双重触发或乱扣费。
            {
                var poolX = CardDatabase.Load();

                var ch = CreatePool.FindByName(poolX, "Crimson Hunter");
                CheckTrue(ch != null, "卡池里有 `Crimson Hunter`");
                if (ch != null)
                {
                    var r = ch.TriggerOps(KeywordTable.Rally);
                    CheckTrue(r != null && r.Count >= 2,
                              "★ `Crimson Hunter` 的 `Rally:` 正文**收进了尾句**"
                              + "（`If it has Flying, deal 6 damage instead`）—— 原来整句丢掉");
                }

                var grot2 = CreatePool.FindByName(poolX, "Grot Orderly");
                if (grot2 != null)
                {
                    string bt = grot2.TriggerText(KeywordTable.Rally) ?? "";
                    CheckTrue(bt.Length > 0 && bt.ToLowerInvariant().IndexOf("at the start") < 0,
                              "★ 反例：`Grot Orderly` 的 `Rally:` 正文**停在回合起止句之前**"
                              + "（不停就是**双重触发**：Rally 一次、回合段又一次）");
                }

                var cc = CreatePool.FindByName(poolX, "Chapter Champion");
                if (cc != null)
                {
                    string ct = cc.TriggerText(KeywordTable.Codex) ?? "";
                    CheckTrue(ct.Length > 0 && ct.ToLowerInvariant().IndexOf("oath") < 0,
                              "★ 反例：`Chapter Champion` 的 `Codex:` 正文**停在 `Oath 2:` 之前**"
                              + "（不停就会在触发 Codex 时**乱扣 2 费**）");
                }

                var br = CreatePool.FindByName(poolX, "Blackmane Reiver");
                if (br != null)
                {
                    var r4 = br.TriggerOps(KeywordTable.Ferocity);
                    CheckTrue(r4 != null && r4.Count >= 2,
                              "★ `Blackmane Reiver` 的 `Ferocity:` 正文收进了 `Give it +1 Attack, …`");
                }
            }
        }

        // ============================================================
        //  ⑤ 结算：`Jammed Communications` —— **事件型手牌陷阱**
        // ============================================================
        //  🔴 「你」= **持有者**：它是**破坏卡**（卡面橙字 `Sabotage`，规则书 `:204`
        //     「创造 1 张破坏卡放入**对手**手牌」）⇒ 躺在**对手**手里、害的是**持有者**。
        //     与同族 `Poisoned Supplies`（`At the end of your turn, your troops take 1 damage`）
        //     已经实现的口径一致。
        {
            var pool = CardDatabase.Load();
            var trap = CreatePool.FindByName(pool, "Jammed Communications");
            CheckTrue(trap != null, "卡池里有 `Jammed Communications`");
            if (trap != null)
            {
                CheckTrue(EffectText.IsHandTrap(trap),
                          "★ 它被判成**手牌陷阱**（`When you play a Stratagem, …`）");
                CheckTrue(trap.HandTrapWhens.Count > 0,
                          "★ 而且收成了**手牌陷阱监听器**（`HandTrapWhens`）");
                CheckTrue(!DeckBuilder.TacticPlayable(trap),
                          "★ 它**不进自动牌组**（陷阱是塞给对手的）");
                //  反例：**单位卡**的 `When …` **不算手牌陷阱**（那是事件层的地盘，别抢）
                var nob = CreatePool.FindByName(pool, "Ork Nob");
                CheckTrue(nob != null && !EffectText.IsHandTrap(nob),
                          "★ 反例：单位卡 `Ork Nob` 的 `When you play a troop, …` **不是**手牌陷阱");

                var noop = Tactic("T_Noop", 1, "Refill 1 Energy");
                var beast = Unit("T_Beast", 1, 1, 3);
                var ctx = BattlePool(new[] { trap, noop, noop, beast }, new[] { Unit("X", 1, 1, 9) },
                                     pool, warlordFaction: "Ultramarines");
                ToP1Turn(ctx, 5);
                var w = ctx.Players[0].Warlord;
                int hp0 = w.Health;

                // (a) 打出一张**部队** → 陷阱**不该响**（卡面写的是 Stratagem）
                CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "T_Beast"), 0), RuleCodes.OK,
                          "打出一个部队（**不是**战术）");
                Check(w.Health, hp0,
                      "★ 反例：**打部队不触发** —— `a Stratagem` 这个筛选是真的"
                      + "（原来它是 `subtype == \"Stratagem\"`，**谁打都不响**）");

                // (b) 打出一张**战术** → 持有者督军掉 1
                CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Noop"), -1), RuleCodes.OK,
                          "打出一张战术");
                Check(w.Health, hp0 - 1, "★ **持有者的督军掉了 1 点**" + LogTail(ctx));

                // (c) 再来一次 → **再掉 1**（卡面没写「只一次」）
                CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Noop"), -1), RuleCodes.OK,
                          "再打一张战术");
                Check(w.Health, hp0 - 2, "★ **每次打战术都掉 1**（不是一次性）" + LogTail(ctx));

                // (d) 打**陷阱自己** = 丢弃它 —— **不掉血**（它离手之后才广播，听不到自己那一次）
                CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "Jammed Communications"), -1),
                          RuleCodes.OK, "把手牌陷阱**打出去**（= 丢弃它）");
                Check(w.Health, hp0 - 2, "★ 打出去**不掉血**（打陷阱只是解掉它）");
                CheckTrue(HandIdx(ctx, 0, "Jammed Communications") < 0, "★ 它确实**离开手牌**了");
            }
        }
    }

    /// <summary>
    /// **A5 批 1**（2026-09-14）—— 单位卡 `desc` 缺口里**真缺的两件**里的第一件，
    /// 外加「**一直在跑、只是没被数出来**」的三族。
    ///
    /// 🔴 **口径在这轮变了**（`资料/单位卡desc与光环_批次划分.md` §一⑤ 记着）：
    ///    `unit_desc_unparsed.txt` ① 栏里的 **`When &lt;事件&gt;, …`（≈21 次）·
    ///    `Talent: &lt;名&gt;`（35 次）· `Companion N: &lt;卡名&gt;`（8 次）三族
    ///    机制一直在跑** —— `EffectText` 认不出的是**那句壳**，不是效果。
    ///    实据：`when_unparsed.md` 认不出的事件短语 **0 种**、带 `When` 的卡 78 张点亮 73 张、
    ///    自检 `TestWhenEvents` 有**结算级**断言（友方 troop 死 → 监听器真的改攻）；
    ///    而且卡面的 `*` **只打在战术卡上**（`BattleDriver.cs:2222` 明写 `c.Type == "tactic"`），
    ///    所以那三族**不会**骗玩家。
    /// </summary>
    static void TestA5Batch1()
    {
        // ============================================================
        //  ① 真缺的那一件：`Heal N` 没写目标 = **治疗自己**
        // ============================================================
        //  实测（逐句探针）：`Heals 3` / `Strike: Heals 4` / `Codex: Heal 1` 原来**全是半懂**
        //  （`heal n=N 目标[（没写）]`）⇒ 卡面打 `*`、掉出「完全解析」，而机制本来就有（自愈）。
        {
            var r = EffectText.ParseSegment("Strike: Heals 4");
            Check(r.Kind, EffectText.SegKind.Ok, "★ `Strike: Heals 4` 认了（原来判半懂）");
            if (r.Ops != null && r.Ops.Count > 0)
            {
                Check(r.Ops[0].Verb, "heal", "动词 = heal");
                Check(r.Ops[0].Amount, 4, "治 4");
                CheckTrue(r.Ops[0].Target != null && r.Ops[0].Target.Subjectless,
                          "★ 没写目标 → 走 `Subjectless`（**有施放者就是它自己**）");
            }
            var r2 = EffectText.ParseSegment("Heals 3");
            Check(r2.Kind, EffectText.SegKind.Ok, "★ 裸 `Heals 3` 同样认了（`Bladeguard Veteran` 那一族）");
            var r3 = EffectText.ParseSegment("Codex: Heal 1");
            Check(r3.Kind, EffectText.SegKind.Ok, "★ `Codex: Heal 1` 也认了（条件是 `energyzero`，不受影响）");

            // ⚠️ **非回归**：有尾句时必须仍然让 `Finish` 去继承**尾句的目标** ——
            //    `Heal 5 and give Camouflage to a friendly unit`（`Evasive Manoeuvre`）
            //    治的是**那个友方单位**、不是自己。`Subjectless` 默认只在**本句没有尾句**时套。
            var ops = EffectText.Parse("Heal 5 and give Camouflage to a friendly unit", out var un, out _);
            CheckTrue(un.Count == 0, "`Evasive Manoeuvre` 那句解析干净");
            CheckTrue(ops.Count > 0 && ops[0].Target != null && !ops[0].Target.Subjectless
                      && ops[0].Target.Kind == "unit" && ops[0].Target.Count == 1,
                      "★ 非回归：目标是**那个友方单位**（不是「自己」）"
                      + "；⚠️ `SplitAndTail` 给 `tail` 的初值是**空串不是 null**，判据别写错");
        }

        // ============================================================
        //  ② 结算：`Haruspex`（Leviathan，8/8）的 `Strike: Heals 4`
        // ============================================================
        {
            var pool = CardDatabase.Load();
            var haru = CreatePool.FindByName(pool, "Haruspex");
            CheckTrue(haru != null, "卡池里有 `Haruspex`（`Strike: Heals 4`）");
            if (haru != null)
            {
                var ctx = BattlePool(new[] { haru }, new[] { Unit("Foe", 1, 1, 9) }, pool,
                                     warlordFaction: "Leviathan");
                ToP1Turn(ctx, 1);
                var u = Place(ctx, 0, 0, haru);
                u.Health = 3;                                // 先打伤，好观察治疗
                // ⚠️ 敌人攻击力给 **0**：给 1 的话它会**反击掉 1 点**，治疗是从**反击之后**那个数
                //    起的（实测：3 → 反击 1 → 2 → `Strike` 治 4 → 6，不是 7）。
                Place(ctx, 1, 0, Unit("Foe", 1, 0, 9));
                CheckCode(RuleCore.DeclareAttack(ctx, 0, 0, 1, 0), RuleCodes.OK, "它攻击");
                Check(u.Health, 7, "★ `Strike: Heals 4` **治的是它自己**（3 → 7）" + LogTail(ctx));
            }
        }

        // ============================================================
        //  ③ 「已在跑却没被数出来」的三族：`CardDef.HandledByOtherLayer`
        // ============================================================
        {
            var pool = CardDatabase.Load();
            var casti = CreatePool.FindByName(pool, "Castigator");          // 事件层
            var chrono = CreatePool.FindByName(pool, "Chronomancer");       // 天赋
            var cold = CreatePool.FindByName(pool, "Coldstar Battlesuit");  // 伴生
            CheckTrue(casti != null && chrono != null && cold != null,
                      "卡池里有 `Castigator` / `Chronomancer` / `Coldstar Battlesuit`");
            if (casti != null && chrono != null && cold != null)
            {
                CheckTrue(CardDef.HandledByOtherLayer(
                              casti, "When a friendly unit Prays, deal 2 damage to a random enemy") != null,
                          "★ `When <事件>, …` 判**已由事件层接手**（≈21 次）");
                CheckTrue(CardDef.HandledByOtherLayer(chrono, "Talent: Reanimate") != null,
                          "★ `Talent: <名>` 判**已由天赋接手**（35 次）");
                CheckTrue(CardDef.HandledByOtherLayer(cold, "Companion 2: Marker Drone") != null,
                          "★ `Companion N: <卡名>` 判**已由伴生接手**（8 次）");

                // 🆕 2026-09-18：**改名会把 `cardface_fixes.json` 的键孤立** ⇒ 那两条修正静默失效。
                //    实测这两张死灵督军：`Diviner`→`Orikan the Diviner` 改名后，
                //    `desc` 里那行 `Talent: Master Chronomancer`（**卡面明写有**，铁律 7 开图核过）整行没了
                //    —— **当时没有任何断言看得见它**，是 `Dropped` 那条红顺着查才揪出来的。补两条盯住。
                var orikan = CreatePool.FindByName(pool, "Orikan the Diviner");
                var imotekh = CreatePool.FindByName(pool, "Imotekh the Stormlord");
                CheckTrue(orikan != null && orikan.TalentName == "Master Chronomancer",
                          "★ `Orikan the Diviner` 的天赋认得出来（应为 `Master Chronomancer`，"
                          + "卡面 `Necron/1督军/Warpforge_5_Orikan-the-Diviner.png` 明写）—— 实得 "
                          + (orikan == null ? "(找不到这张卡)" : "「" + (orikan.TalentName ?? "(空)") + "」"));
                CheckTrue(imotekh != null && imotekh.TalentName == "Lord of the Storm",
                          "★ `Imotekh the Stormlord` 的天赋认得出来（应为 `Lord of the Storm`）—— 实得 "
                          + (imotekh == null ? "(找不到这张卡)" : "「" + (imotekh.TalentName ?? "(空)") + "」"));
                // 反例：天赋名**不该同时留在 keywords 里**（那会混进「认不出的关键词」= `Dropped` 红）
                CheckTrue(imotekh == null || imotekh.Keywords == null
                          || !imotekh.Keywords.ContainsKey("lord of the storm"),
                          "★ ……而且那个天赋名**没有**混进 `keywords`（混进去就是 `Dropped` 那条红）");

                // 反例 ①：**真没机制的句子不许被放行**（否则报表会把缺口藏起来）
                CheckTrue(CardDef.HandledByOtherLayer(
                              null, "Take control of an enemy troop this turn and give it Fast") == null,
                          "★ 反例：`Telephatic Domination` 那句**不算被接手**（它确实没机制）");
                // 反例 ②：事件那一族**只对单位卡成立** —— 非单位卡的同形状句子是**手牌陷阱**
                //   （走 `EffectText.SplitHandTrapWhen` 那条路），不能靠这一条蒙混过去
                CheckTrue(CardDef.HandledByOtherLayer(
                              CreatePool.FindByName(pool, "Jammed Communications"),
                              "When you play a Stratagem, your Warlord takes 1 damage") == null,
                          "★ 反例：非单位卡的 `When …` **不算「事件层接手」**（那是手牌陷阱那条路）");
                // 反例 ③：事件短语认不出的句子照样不算（`HandledByOtherLayer` 转调的是同一个判据）
                CheckTrue(CardDef.HandledByOtherLayer(casti, "When you shuffle your deck twice, heal 2") == null,
                          "★ 反例：事件短语认不出 ⇒ 不算接手（宁可报出来）");
            }
        }
    }

    /// <summary>
    /// **按阵营的战术卡覆盖率** —— 做某个阵营时，这就是工作清单。
    ///
    /// 为什么要单开一层：总数（324/449）看不出「这个阵营还差什么」。
    /// 按阵营切之后，每个阵营缺的**句子**能列出来，那是可以直接开工的清单。
    /// </summary>
    static void ReportFactionCoverage(List<CardDef> pool)
    {
        var facs = CardDatabase.Factions(pool);
        var sb = new StringBuilder();
        sb.AppendLine("# 按阵营的战术卡覆盖率（自动生成，别手改）");
        sb.AppendLine();
        sb.AppendLine("由 `RuleEngineTest.ReportFactionCoverage` 每次跑自检时重写。");
        sb.AppendLine();
        sb.AppendLine("| 阵营 | 战术卡 | 完全解析 | 载荷有机制 | 打不出去的句子（去重） |");
        sb.AppendLine("|---|---|---|---|---|");

        foreach (string fac in facs)
        {
            var mine = new List<CardDef>();
            foreach (var c in pool) if (c != null && c.Faction == fac) mine.Add(c);

            int tac = 0, full = 0, mech = 0;
            var blockers = new Dictionary<string, int>();
            foreach (var c in mine)
            {
                if (c.Type != "tactic") continue;
                tac++;
                // 🔴 **2026-09-16 修：判据不再自己写一份。**
                //   原来这里自己判 `un.Count == 0 && pa.Count == 0`，**漏了 `EffectText.Coverage`
                //   里那两条豁免** —— ① 手牌陷阱（`IsHandTrap`）直接算全过；② `HandledByOtherLayer`
                //   把「别的层已经接手的句子」从「不认识」里剔掉。
                //   ⇒ **同一张卡在两张报表里结论不同**：实测 `Genestealers` 那栏
                //   「战术卡 34 · 完全解析 **33**」，而全局表头写 **445/445** —— 差的就是
                //   `Poisoned Supplies` / `Cult Propaganda` 那几张**手牌陷阱战术卡**。
                //   ⚠️ 这正是本工程记过的「**报表虚低**」：下一个人照着做会去重做已经做完的事
                //      （同 `单位卡desc与光环_批次划分.md` §一⑤ 那个坑）。
                //   ⇒ 现在**单卡跑一遍 `Coverage`**，和全局那份**同源**。
                var cov1 = EffectText.Coverage(new[] { c }, "tactic", pool);
                bool ok = cov1.Full >= 1;
                if (ok) full++;
                else
                {
                    var un1 = new List<string>(); var pa1 = new List<string>();
                    EffectText.Parse(c.Desc, out un1, out pa1);
                    foreach (string u in un1) Bump(blockers, u);
                    foreach (string u2 in pa1) Bump(blockers, u2);
                }
                if (cov1.FullAndMechanized >= 1) mech++;
                else if (ok)
                {
                    // 能解析但没机制 ⇒ 把 `OpHasMechanism` 的**理由**打出来
                    //（逐 op 问，和上面那条**同一条判据**）
                    foreach (var op in EffectText.Parse(c.Desc, out _, out _))
                    {
                        string why; bool imprecise;
                        if (EffectText.OpHasMechanism(op, c.Faction, pool, out why, out imprecise)) continue;
                        Bump(blockers, (imprecise ? "兵种过滤 " : "") + why);
                    }
                }
            }

            var top = new List<KeyValuePair<string, int>>(blockers);
            top.Sort((x, y) => y.Value.CompareTo(x.Value));
            var list = new List<string>();
            for (int i = 0; i < top.Count && i < 14; i++) list.Add(top[i].Key + "×" + top[i].Value);

            sb.AppendLine($"| {fac} | {tac} | {full} | {mech} | {string.Join("<br>", list.ToArray())} |");
            Debug.Log(P + $"   [{fac,-17}] 战术卡 {tac,3} · 完全解析 {full,3} · 载荷有机制 {mech,3} · 卡点 {top.Count}");
        }

        const string path = "d:/4/_tmp_view/tactic_by_faction.md";
        System.IO.File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8);
        Debug.Log(P + "   按阵营的清单写到 " + path);
    }

    static void Bump(Dictionary<string, int> d, string k)
    {
        int n;
        d[k] = d.TryGetValue(k, out n) ? n + 1 : 1;
    }

    /// <summary>
    /// **效果分类表** —— 449 张战术卡按「动词 → 载荷类别 → 目标」分堆，落盘到
    /// `_tmp_view/tactic_taxonomy.md`，并在日志里报每一堆的量。
    ///
    /// 为什么要它有：这套分类一直**只活在 `Dispatch` 的 handler 顺序里**，没有一处写下来。
    /// 于是「加新效果该放哪一格」只能靠读代码推。写成表之后，缺格、混格、以及
    /// 「哪些格子已经满了、哪些还是空的」一眼能看出来。
    /// </summary>
    /// <summary>
    /// **单位卡的 `desc` 里那些效果文字，拿 `EffectText` 解析能认多少** —— 2026-09-13 查证。
    ///
    /// **为什么要先查这个**：`CardDef` 只解析 `keywords` 里带 `:` 的那几条（走 `EffectSpec` 的
    /// **封闭文法**，只有 Damage/Heal/Draw），**`Desc` 从来没进过 `EffectText`** ——
    /// 所以那批「卡面写着效果、打起来完全不生效」的单位卡是**静默的**。
    /// 交接文档要求「动手前先花半轮查证 desc 该不该由 `EffectText` 解析」，这一节就是那个数。
    ///
    /// ⚠️ **只报数、不断言** —— 它回答的是「值不值得接」，不是「接得对不对」。
    ///    落盘到 `_tmp_view/unit_desc_unparsed.txt`（和战术卡那份分开）。
    /// </summary>
    /// <summary>
    /// **`When &lt;事件&gt;` 的覆盖面 + 认不出的短语全量清单**（2026-09-13 第三十三轮加）。
    ///
    /// 为什么要有它：铺宽这条线（`资料/事件层_数据与设计.md` §三）一直只有**文档里的两句数**
    /// （「82 张里 31 张点亮」「37 种事件短语认不出」），**没有可复核的清单** ——
    /// 每开一个新会话都要重新数一遍，而且数出来的口径还各不相同（「张」和「种」混着说）。
    /// ⇒ 做成和 `tactic_unparsed.txt` 一样的产物：**每次自检重写一份**，按频次排。
    /// </summary>
    static void ReportWhenCoverage()
    {
        // ⚠️ 先清空再 Load —— `UnknownPhrases` 是 `Parse` 里写的静态桶，
        //    不清的话会把上一次（甚至上一个测试用例）的残留一起报出来。
        WhenEvents.UnknownPhrases.Clear();
        var pool = CardDatabase.Load();      // 加载即重建监听器，顺带把认不出的短语写进桶

        int withWhen = 0, lit = 0, listeners = 0, costWhens = 0;
        var freq = new Dictionary<string, int>();
        foreach (var c in pool)
        {
            if (c == null) continue;
            if (c.WhenTriggers.Count > 0) { lit++; listeners += c.WhenTriggers.Count; }
            if (c.CostWhens.Count > 0) costWhens++;

            // 🔴 **非单位卡不参与下面这段**（2026-09-13 候选 E）——
            //    它的 `When …` **不是事件监听器**（`BroadcastWhen` 只扫棋盘上的单位，
            //    判据见 `CardDef.CanListenForEvents`），卡面那句走的是别的路。
            //    把它算进来会让「带 `When` 的卡」和「认不出的短语」**两个数都虚高** ——
            //    实测就是 `played`（`Reconnaissance Mission`，DarkAngels **战术卡**）。
            //    ⚠️ 所以**必须和 `CardDef.AddWhenTrigger` 用同一个判据**，否则两边口径又会分家。
            if (!c.CanListenForEvents) continue;

            // 「带 `When …` 的卡」按**卡面文字**数（desc + keywords 两个来源都扫，同 `CollectWhenTriggers`）
            bool has = false;
            foreach (string seg in SegsOf(c))
            {
                if (!seg.TrimStart().StartsWith("When ", StringComparison.OrdinalIgnoreCase)) continue;
                has = true;
                int comma = seg.IndexOf(',');
                if (comma <= 5) continue;
                string phrase = seg.Substring(5, comma - 5).Trim().ToLowerInvariant();
                if (WhenEvents.Parse(phrase) == null)
                {
                    int n; freq.TryGetValue(phrase, out n); freq[phrase] = n + 1;
                }
            }
            if (has) withWhen++;
        }

        Debug.Log(P + $"   `When <事件>`：带它的卡 **{withWhen}** 张 · 真的点亮 **{lit}** 张"
                  + $"（共 {listeners} 条监听器）· 事件触发式降费 **{costWhens}** 张");
        Debug.Log(P + $"   认不出的**事件短语** {freq.Count} 种");

        var top = new List<KeyValuePair<string, int>>(freq);
        top.Sort((a, b) => b.Value.CompareTo(a.Value));
        for (int i = 0; i < top.Count && i < 10; i++)
            Debug.Log(P + $"     ×{top[i].Value,-3} {top[i].Key}");

        var sb = new StringBuilder();
        sb.AppendLine("**`When <事件>` 认不出的短语**（2026-09-13 第三十三轮起由自检重写）");
        sb.AppendLine();
        sb.AppendLine($"带 `When` 的卡 {withWhen} 张 · 点亮 {lit} 张 · 监听器 {listeners} 条 · "
                    + $"事件触发式降费 {costWhens} 张 · 认不出的事件短语 {freq.Count} 种");
        sb.AppendLine();
        sb.AppendLine("| 次数 | 事件短语（原文） |");
        sb.AppendLine("|---|---|");
        foreach (var kv in top) sb.AppendLine($"| {kv.Value} | {kv.Key} |");
        const string path = "d:/4/_tmp_view/when_unparsed.md";
        System.IO.File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8);
        Debug.Log(P + "   全量清单写到 " + path);
    }

    /// <summary>一张卡的全部卡面文字分句（`desc` + `keywords` 两个来源，和 `CollectWhenTriggers` 同口径）</summary>
    static IEnumerable<string> SegsOf(CardDef c)
    {
        foreach (string seg in EffectText.Split(c.Desc ?? "")) yield return seg;
        foreach (var kw in c.Keywords)
            foreach (string seg in EffectText.Split(kw.Key ?? "")) yield return seg;
    }

    ///
    /// <summary>
    /// **「相邻」对账表**（2026-09-13 候选 F **实现之后**；在此之前这里是个缺口探针）。
    ///
    /// **之前**：`EffectText` **认得** `adjacent` 写法（`spec.Adjacent = true`，`EffectText.cs`），
    /// 但**全仓没有消费者** —— 卡面写着「相邻」的 36 张打的**不是卡面写的目标集**，
    /// 而且**卡面不打 `*`**（正文解析是成功的）⇒ 标准的静默失效。两个方向都会错：
    ///   · `Deal 3 damage to an enemy and its adjacent units` → 当成「一个敌方单位」⇒ **打少了**
    ///   · `Adjacent units have +1 Attack` → 当成「己方全体」⇒ **打多了**
    ///
    /// **现在**：锚点（`AdjacentAnchor`，照原版 `TargetsAffected.cs:17-22` 的枚举）在解析期定好、
    /// 结算期由 `EffectResolver.ResolveTargets` 消费（相邻格判据唯一收在 `BoardSpec.AdjacentSlots`）。
    /// 这条自检随之改成**对账** —— 逐张列出锚点定成了什么，并把两类**挑出来报**：
    ///   ① **锚点认不出**（`AdjacentFailed`）—— 整句降级成「半懂」、卡面照旧打 `*`。这是**故意的**
    ///      （「宁可认不出，别静默错打」）。张数应该很小；涨了要看是不是新卡进来了。
    ///   ② **光环族**（`Adjacent units have X`，**10 张**）—— ✅ **2026-09-14 A7 收工**：
    ///      解析与数据模型走 `Core/Aura.cs` 的 `Auras.TryParse` → `CardDef.AuraSpecs`
    ///      （**不是 op** —— 光环没有触发时机，是状态的生命周期，见 `CardDef.AuraSpecs` 的说明）。
    ///      现在这里报的是「**认下几张 / 哪张故意没收**」，**不再是「本轮不做」**。
    ///      ⚠️ **结算还没接**（A7 第 3 步）—— 在此之前光环**不会生效**，这是如实的。
    ///   ③ **整句本来就认不出**（`When …` 从句之类，由别的层消费）
    ///
    /// 产物：`_tmp_view/adjacent_report.md`（每次自检重写）。
    /// </summary>
    static void ReportAdjacentGap()
    {
        var pool = CardDatabase.Load();
        var detail = new List<string>();       // 逐卡：卡名 / 阵营 / 类型 / 锚点 / 备注
        var debug = new List<string>();        // 逐句原始解析（排查用：这句到底解成了什么）
        var unknown = new List<string>();      // ① 锚点认不出（会打 `*`）
        var auraOk = new List<string>();       // ② 光环族：**已被光环层（AuraSpecs）认下来**
        var auraFail = new List<string>();     // ② 光环族：形状对、但光环层**故意没收**的
        var deferred = new List<string>();     // ③ 整句本来就认不出（等 A3 的 `When` 短语）
        int withAnchor = 0;

        foreach (var c in pool)
        {
            if (c == null) continue;
            bool says = false, cardAura = false, cardUnknown = false;
            var anchors = new List<string>();

            // ⚠️ 两种上下文**都要量**，因为**引擎自己就是这么走的**：
            //    · 战术卡 → `EffectText.Parse(card.Desc)`（整条）
            //    · 单位卡的触发正文（`Strike: …` / `Rally: …`）→ `CardDef.AddTriggerOp` 把冒号后那段
            //      **单独**送进 `Parse(body)`（`CardDef.cs:251`）
            //    · 手牌陷阱 / 回合起止 → `ResolveAtTurn` 也是**单独**送正文（`EffectText.SplitAtTurn`）
            //    而「相邻」的锚点判据要看**上一句点过谁** —— 逐句拆开会把那个上下文丢掉，
            //    量出来的锚点是错的（本轮先按句量，量错过一次）。所以两种都量、都报。
            foreach (var text in TextsOf(c))
            {
                if (string.IsNullOrEmpty(text)) continue;
                bool any = false;
                foreach (string seg in EffectText.Split(text))
                {
                    if (seg.IndexOf("adjacent", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    any = true;
                    if (Auras.LooksLikeAura(seg)) cardAura = true;
                }
                if (!any) continue;
                says = true;

                foreach (string probe in Probes2(text))
                {
                    if (probe.IndexOf("adjacent", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    var ops = EffectText.Parse(probe, out var un, out var pa);
                    if (ops == null) continue;
                    foreach (var o in Walk(ops))
                    {
                        string ts = o.Target == null ? "（无目标）"
                                  : $"{o.Target.Side}/{o.Target.Kind}「{o.Target.Raw}」"
                                    + (o.Target.Adjacent ? $"〔相邻→{AnchorName(o.Target)}〕" : "");
                        debug.Add($"| {c.Name} | {o.Verb} {ts} "
                                + $"| {(un.Count + pa.Count == 0 ? "干净" : "**半懂/不认**")} | {probe} |");
                        var t = o.Target;
                        if (t == null || !t.Adjacent) continue;
                        if (t.AdjacentFailed) { cardUnknown = true; continue; }
                        string a = $"{o.Verb} → `{t.Anchor}`"
                                 + (t.AnchorInSet ? "（锚点也在目标里）" : "")
                                 + (t.AdjacentAll ? "（一圈全要）" : "");
                        if (!anchors.Contains(a)) anchors.Add(a);
                    }
                }
            }
            if (!says) continue;

            string note;
            if (anchors.Count > 0)
            {
                note = "✅ " + string.Join("；", anchors);
                withAnchor++;
            }
            else if (cardUnknown)
            {
                note = "🔴 锚点**认不出** ⇒ 整句已降级成半懂、卡面打 `*`";
                unknown.Add(c.Name);
            }
            else if (cardAura)
            {
                // 🆕 2026-09-14 A7：光环层**已经接上**（`CardDef.AuraSpecs` ← `Auras.TryParse`）。
                //    这里分两类报，因为它们的**后续动作不一样**：
                //      · 认下来了 → 只等 A7 第 3 步把结算接上（不用再查解析）
                //      · 形状对、但没收 → **是故意的**（见下面那条断言），别当成漏做
                if (c.AuraSpecs.Count > 0)
                {
                    var said2 = new List<string>();
                    foreach (var au in c.AuraSpecs) said2.Add(au.ToString());
                    note = "✅ **光环层认下来了**：" + string.Join("；", said2);
                    auraOk.Add(c.Name);
                }
                else
                {
                    note = "🔴 **光环句没被光环层认下来** —— 形状对、载荷判不出来"
                         + "（卡面裸 `+N`、图标被数据管线剥掉了，见断言里的说明）";
                    auraFail.Add(c.Name);
                }
            }
            else
            {
                // 例句：`When an enemy gets Hunt Mark, deal 1-2 damage to it and its adjacent units`
                // （`Venerable Dreadnought`）· `When this unit attacks an enemy with Hunt Mark,
                // deal 3 damage to adjacent enemies`（`Long Fang`）。
                //
                // ⚠️ **2026-09-13 A3 更新措辞**：这两张的 `When` 短语**都已经接上了**
                //    （`an enemy gets Hunt Mark` 是第三十四轮、`attacks an enemy with Hunt Mark` 是 A3），
                //    监听器都注册上了 —— 它们在这里出现，是因为**这把尺子量的是另一件事**：
                //    「把**整条 desc 原文**当效果文本解析」认不出 `When …` 从句
                //    （事件是**事件层**消费的，见 `WhenEvent.cs`）。
                //    ⇒ **这不是缺口**，是两把尺子的差别；真正该盯的是自检里那句
                //    「带 `When <事件>` 的卡 78 张 · 真的点亮 N 张」。
                // 🆕 2026-09-14 A7：这里原来只可能是 `When …` 从句，措辞就写死了。
                //    Zahndrekh 的 `Adjacent Remnants do not disappear at the end of your turn`
                //    现在也落进来（旧判据把它算成「光环族」，新判据要求句子里有 `have` ⇒ 它不是）。
                //    ⇒ **措辞按「是不是事件层那一族」分叉**，别让一句讲 `When` 的话套在它头上。
                note = c.WhenTriggers.Count > 0
                     ? "本句在**效果文本层**认不出（`When …` 从句由**事件层**消费 —— "
                       + "两张都已接线，见「点亮」那一行；这不是「相邻」的账）"
                     : "本句在**两把尺子**（效果文本层 / 光环层）下都认不出 —— "
                       + "⚠️ **不是漏做**：它是**另一个常驻 handler**（改残骸寿命，"
                       + "和其余 29 张不是同一条），A7 第 5 步单列，见 "
                       + "`资料/单位卡desc与光环_批次划分.md` §6.3";
                deferred.Add(c.Name);
            }
            detail.Add($"| {c.Name} | {c.Faction} | {c.Type} | {note} |");
        }

        Debug.Log(P + $"   「相邻」对账：卡面写了 `adjacent` 的 **{detail.Count}** 张 —— "
                  + $"已接锚点 **{withAnchor}** 张 · 锚点认不出 **{unknown.Count}** 张 · "
                  + $"光环层认下 **{auraOk.Count}** 张 · 光环层没收 **{auraFail.Count}** 张 · "
                  + $"整句本来就认不出 **{deferred.Count}** 张");
        if (unknown.Count > 0)
            Debug.Log(P + "   锚点认不出的：" + string.Join("、", unknown));
        if (deferred.Count > 0)
            Debug.Log(P + "   效果文本层认不出的（`When …` 从句由**事件层**消费，不是「相邻」的账）："
                      + string.Join("、", deferred));
        Debug.Log(P + "   全量对账表写到 d:/4/_tmp_view/adjacent_report.md");

        var sb = new StringBuilder();
        sb.AppendLine("**「相邻」逐卡对账表**（2026-09-13 候选 F 实现后，由自检每次重写）");
        sb.AppendLine();
        sb.AppendLine($"卡面写了 `adjacent` 的卡 **{detail.Count}** 张 · 已接锚点 **{withAnchor}** 张 · "
                    + $"锚点认不出 **{unknown.Count}** 张 · 光环层认下 **{auraOk.Count}** 张 · "
                    + $"光环层没收 **{auraFail.Count}** 张 · 整句本来就认不出 **{deferred.Count}** 张");
        sb.AppendLine();
        sb.AppendLine("锚点取值来自原版 `TargetsAffected.cs:17-22`（`Self=100` / `PreviousTarget=105` / "
                    + "`FriendlyWarlord=113` …）；「相邻」= 锚点所在那一方棋盘行内的左右紧邻格");
        sb.AppendLine("（`BoardSpec.AdjacentSlots`，原版 `BattleManager.GetAdjacentUnits`），"
                    + "相邻单位**过同一套筛选**（原版 `CardScript.TargetedSpellPlayed` 逐个调 `criteria.Matches`）。");
        sb.AppendLine();
        sb.AppendLine("| 卡名 | 阵营 | 类型 | 锚点 / 备注 |");
        sb.AppendLine("|---|---|---|---|");
        foreach (string d in detail) sb.AppendLine(d);
        sb.AppendLine();
        sb.AppendLine("## 逐句原始解析（排查用 —— 锚点为什么是那个值）");
        sb.AppendLine();
        sb.AppendLine("| 卡名 | 送进解析的句子 | 解析 | 解出来的 ops |");
        sb.AppendLine("|---|---|---|---|");
        foreach (string d in debug) sb.AppendLine(d);
        const string path = "d:/4/_tmp_view/adjacent_report.md";
        System.IO.File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8);
        // ⚠️ 旧名字（`adjacent_unimplemented.txt`）是**缺口探针**时代的产物：那时这份表的作用是
        //    「记下哪些卡还没接」。现在接到了，名字里的 `unimplemented` 只会误导下一个会话 ——
        //    顺手把旧文件删掉，别让两份说法并存（CLAUDE.md 铁律 5）。
        const string oldPath = "d:/4/_tmp_view/adjacent_unimplemented.txt";
        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);

        // ① **不许静默**：句子解析得出来、却带着一个**没定锚点**的「相邻」目标 ⇒ 那正是本轮要消灭的
        //    那类静默失效（卡面不打 `*`、打的人却不是卡面写的那些）。实测现在是 0 张。
        CheckTrue(unknown.Count == 0,
                  $"★ 「相邻」：还有 **{unknown.Count}** 张卡**解析得出来、锚点却没定**"
                  + $"（{string.Join("、", unknown)}）—— 看 `_tmp_view/adjacent_report.md`："
                  + "要么补判据，要么确认它整句本来就认不出（那种会打 `*`，是**如实**）");
        // ② 光环层**已经接上**（2026-09-14 A7 收工）—— 这条断言现在是**真账**，不是「还没做」。
        //    ✅ **10 张全认下了**：`Nemesor Zahndrekh`（没有 `have` 那句）在第 3 步收进了光环层
        //    （`AuraSpec.RemnantStay`）；`Genestealer Familiar` 原来是「卡面裸 `+N` ⇒ 故意不收」，
        //    2026-09-14 **从数据侧修掉**（`cardface_fixes.json` 的 `desc` 列补 `[Melee]`，卡图亲读）
        //    ⇒ `auraFail` 现在是 **0**。
        //    ⚠️ `Aura.cs` 的 `BareSignedAmbiguous` 那道闸**留着**：将来再出现裸 `+N` 时仍然不收
        //    （那时属性在文本层确实判不出来 —— 宁可认不出，也别静默错一张）。
        CheckTrue(auraOk.Count == 10,
                  $"★ 「相邻」光环族：光环层应当认下 **10** 张（`adjacent` 形的全认下）"
                  + $"—— 实得 {auraOk.Count} 张");
        CheckTrue(auraFail.Count == 0,
                  $"★ 「相邻」光环族：**没有**认不下的了"
                  + $"—— 实得 {auraFail.Count} 张：{string.Join("、", auraFail)}");
    }

    /// <summary>
    /// **替代行动族**：`Duty` / `Pray` / `Ferocity` / `Agenda`（2026-09-13 A2）。
    ///
    /// 规则书 `:150`「职责、狂暴、祈祷等关键词是部分单位可执行的**替代攻击行动**，
    /// 受与攻击相同的限制条件约束」+ 每个关键词各自那一行（`:165` / `:181` / `:186` / `:198`）。
    ///
    /// | 关键词 | 差别 | 出处 |
    /// |---|---|---|
    /// | `ferocity` 狂暴 | **快速**：部署当回合就能用；用完**洗回牌库** | `:186` · `:98` |
    /// | `pray` 祈祷 | **缓慢**：部署当回合不能用 | `:198` · `:98` |
    /// | `duty` 职责 | **本局一次**（可被装填）；**无法攻击时也能激活** | `:181` · `:152` |
    /// | `agenda` 议程 | 「以触发效果代替攻击」 | `:165` |
    ///
    /// 这四条以前是用我们自定的 `Ability:` **收成一条**的（会互相冒充广播），现在**逐关键词**。
    /// </summary>
    static void TestAlternativeActions()
    {
        // ---- ① 职责：能用、花掉行动、**本局只一次** ----
        {
            var duty = new CardDef("FixtureDuty", "FixtureDuty", "unit", "Duty: Gain +2 Attack",
                                   "common", "Test", 1, 1, 9, 0, new[] { "Duty" }, subtype: "Infantry");
            var ctx = Battle(new[] { duty }, new[] { Unit("X", 1, 1, 5) });
            ToP1Turn(ctx, 1);
            var u = Place(ctx, 0, 0, duty);
            u.Exhausted = false;                       // 部署当回合不能动（规则书 :98）
            CheckTrue(duty.TriggerOps("duty") != null, "`Duty:` 的正文收下来了");
            Check(RuleCore.AvailableAlternative(ctx, 0, 0), "duty", "这一格给出来的是**职责**");

            CheckCode(RuleCore.UseAlternative(ctx, 0, 0, "duty"), RuleCodes.OK, "用一次职责");
            Check(u.Attack, 3, "★ 职责的正文生效了（1 → 3 攻）");
            CheckTrue(u.Exhausted, "花掉了本回合的行动");
            CheckCode(RuleCore.UseAlternative(ctx, 0, 0, "duty"), RuleCodes.ErrExhausted,
                      "同一回合不能再用（`:150` 与攻击同限制）");

            PassTurn(ctx); PassTurn(ctx);              // 回到**我的下一回合**（`PassTurn` 只换一次边）
            CheckCode(RuleCore.UseAlternative(ctx, 0, 0, "duty"), RuleCodes.ErrDutyUsed,
                      "★ **下一回合也不能再用** —— 职责是**一次性**的（`:181`）"
                      + "（用 `Exhausted` 判会在这里放行，那正是它和别的替代行动的区别）");
        }

        // ---- ② 狂暴：**部署当回合就能用**（快的），用完**洗回牌库** ----
        {
            var fero = new CardDef("FixtureFero", "FixtureFero", "unit", "Ferocity: Gain +2 Attack",
                                   "common", "Test", 1, 1, 9, 0, new[] { "Ferocity" }, subtype: "Infantry");
            var pray0 = new CardDef("FixturePray0", "FixturePray0", "unit", "Pray: Gain +1 Health",
                                    "common", "Test", 1, 1, 9, 0, new[] { "Pray" }, subtype: "Infantry");
            var ctx = Battle(new[] { fero }, new[] { Unit("X", 1, 1, 5) });
            ToP1Turn(ctx, 1);
            int deckBefore = ctx.Players[0].Deck.Count;
            // ⚠️ **部署当回合疲不疲劳是 `UnitState` 构造函数定的事**，而 `Place` 会**显式覆盖**它
            //    （那样方便造场景，但会把这套规则绕过去）⇒ 这一条要**直接问构造函数**。
            CheckTrue(!new UnitState(fero, false).Exhausted,
                      "★ **狂暴的单位部署当回合不疲劳**（规则书 :98 点名了它）");
            CheckTrue(new UnitState(pray0, false).Exhausted,
                      "★ **祈祷的单位部署当回合是疲劳的** —— 和狂暴正好相反（`:98`/`:198`）");
            var u = Place(ctx, 0, 0, fero);            // ⚠️ 这里不动 `Exhausted`，下面几行照实际值走
            CheckCode(RuleCore.UseAlternative(ctx, 0, 0, "ferocity"), RuleCodes.OK, "用一次狂暴");
            Check(SlotOf(ctx, 0, "FixtureFero"), -1, "★ 狂暴结算完 **洗回牌库了**（`:186`「然后洗回牌库」）");
            Check(ctx.Players[0].Deck.Count, deckBefore + 1, "牌库 +1（是**牌库**，不是弃牌堆）");
            Check(ctx.Players[0].Discard.Count, 0, "弃牌堆没多东西 —— 洗回牌库不等于被弃掉");
        }

        // ---- ③ 祈祷：**慢的**，部署当回合不能动 ----
        {
            var pray = new CardDef("FixturePray", "FixturePray", "unit", "Pray: Gain +1 Health",
                                   "common", "Test", 1, 1, 9, 0, new[] { "Pray" }, subtype: "Infantry");
            var ctx = Battle(new[] { pray }, new[] { Unit("X", 1, 1, 5) });
            ToP1Turn(ctx, 1);
            var u = Place(ctx, 0, 0, pray, exhausted: true);   // 「刚部署」的状态（见 ② 的构造函数断言）
            CheckCode(RuleCore.UseAlternative(ctx, 0, 0, "pray"), RuleCodes.ErrExhausted,
                      "★ 部署当回合的祈祷**用不了**（慢的）—— "
                      + "和狂暴那条形成对照（`UnitState` 构造里只豁免了 fast/flank/ferocity）");
            u.Exhausted = false;
            CheckCode(RuleCore.UseAlternative(ctx, 0, 0, "pray"), RuleCodes.OK, "下回合能用");
            Check(u.Health, 10, "祈祷的正文生效了（9 → 10 血）");
        }

        // ---- ④ 议程：每回合都能用（**不是一次性的**，和职责区别就在这）----
        {
            var ag = new CardDef("FixtureAgenda", "FixtureAgenda", "unit", "Agenda: Gain +1 Attack",
                                 "common", "Test", 1, 1, 9, 0, new[] { "Agenda" }, subtype: "Infantry");
            var ctx = Battle(new[] { ag }, new[] { Unit("X", 1, 1, 5) });
            ToP1Turn(ctx, 1);
            var u = Place(ctx, 0, 0, ag);
            u.Exhausted = false;
            CheckCode(RuleCore.UseAlternative(ctx, 0, 0, "agenda"), RuleCodes.OK, "用一次议程");
            Check(u.Attack, 2, "1 → 2 攻");
            PassTurn(ctx); PassTurn(ctx);              // 回到**我的下一回合**
            CheckCode(RuleCore.UseAlternative(ctx, 0, 0, "agenda"), RuleCodes.OK,
                      "★ **下一回合还能用** —— 议程不是一次性（用 `DutyUsed` 那种判法会在这里错误地拦下）");
            Check(u.Attack, 3, "2 → 3 攻");
        }

        // ---- ⑤ 反例：**没有正文**的关键词不给按钮（`Solitaire` 那种只印了词的卡）----
        {
            var bare = new CardDef("FixtureBareKw", "FixtureBareKw", "unit", "Pray",
                                   "common", "Test", 1, 1, 9, 0, new[] { "Pray" }, subtype: "Infantry");
            var ctx = Battle(new[] { bare }, new[] { Unit("X", 1, 1, 5) });
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 0, bare).Exhausted = false;
            CheckCode(RuleCore.UseAlternative(ctx, 0, 0, "pray"), RuleCodes.ErrNoAction,
                      "★ 卡上只有裸关键词、没有正文 ⇒ **明说不支持** —— "
                      + "放行的话就是一个「点了什么都不发生」的按钮（红线）");
        }
    }

    /// <summary>
    /// **伴生 `Companion X`**（2026-09-13 A2）—— 规则书 `:176`「**从手牌打出时，可打出至多 X 张
    /// 其伴生部队**」。
    ///
    /// 三条：① 手里有伴生部队 → 打出来**免费带出**（最多 X 张）· ② 手里有 3 张、X = 2 → **只带 2 张** ·
    /// ③ 手里没有 → **一张都不带**（而且日志要说清，不静默）。
    /// ⚠️ 原版是「**可**打出」（玩家选），我们**自动带满** —— 这条测试同时把这个近似钉在案上。
    /// </summary>
    static void TestCompanion()
    {
        var drone = Unit("FixtureDrone", 1, 1, 3);
        var host = new CardDef("FixtureHost", "FixtureHost", "unit",
                               "Companion 2: FixtureDrone",
                               "common", "Test", 3, 2, 5, 0,
                               new[] { "Companion 2: FixtureDrone" }, subtype: "Battlesuit");

        // ---- ① / ② 手里 3 张 → 带 2 张 ----
        {
            var ctx = ProbeBattle(new[] { host, drone, drone, drone }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 6);
            Check(host.CompanionName, "FixtureDrone", "伴生名字读得出来（`Companion 2: FixtureDrone`）");
            Check(host.KwValue(KeywordTable.Companion), 2, "数量读得出来（= 2）");
            int hand0 = ctx.Players[0].Hand.Count;
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "FixtureHost"), 0), RuleCodes.OK,
                      "打出带伴生的单位");
            int dronesOnBoard = 0;
            for (int s = 0; s < BoardSpec.Size; s++)
                if (Board(ctx, 0, s) != null && Board(ctx, 0, s).Name == "FixtureDrone") dronesOnBoard++;
            Check(dronesOnBoard, 2, "★ **带出 2 张伴生部队**（正好是 X）");
            Check(ctx.Players[0].Hand.Count, hand0 - 3,
                  "★ 手牌少了 3 张（打出宿主 1 + 带出的 2）—— 第 3 张 Drone **留在手里**");
        }

        // ---- ③ 手里没有 → 一张都不带 ----
        {
            var ctx = ProbeBattle(new[] { host }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 4);      // ⚠️ 别推太远：垫牌抽空后督军会吃疲劳（实测踩过）
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "FixtureHost"), 0), RuleCodes.OK,
                      "打出带伴生的单位（手里没有伴生部队）");
            int dronesOnBoard = 0;
            for (int s = 0; s < BoardSpec.Size; s++)
                if (Board(ctx, 0, s) != null && Board(ctx, 0, s).Name == "FixtureDrone") dronesOnBoard++;
            Check(dronesOnBoard, 0, "★ **一张都没带出来**（不凭空生成 —— 卡面说的是「**从手牌**打出」）");
        }
    }

    /// <summary>
    /// **伏击 `Ambush`**（2026-09-13 A2）—— 规则书 `:166`「**面朝下打出**；下次回合前
    /// **若被伤害：翻开无效果**；**若未被伤害：翻开并触发效果**」。
    ///
    /// 两条出口**各钉一次**（少一条就有一半永远测不到）：
    ///   ① 挨到伤害 → 翻开、**效果作废** · ② 撑到控制者下个回合 → 翻开、**触发**。
    /// </summary>
    static void TestAmbush()
    {
        var amb = new CardDef("FixtureAmbush", "FixtureAmbush", "unit",
                              "Ambush: Gain +3 Attack",
                              "common", "Test", 1, 1, 9, 0, new[] { "Ambush" }, subtype: "Infantry");
        var amb2 = new CardDef("FixtureAmbush2", "FixtureAmbush2", "unit",
                               "Ambush: Gain +3 Attack",
                               "common", "Test", 1, 1, 9, 0, new[] { "Ambush" }, subtype: "Infantry");
        var poke = Tactic("T_AmbPoke", 0, "Deal 1 damage to a friendly unit");

        // ---- ① 挨到伤害 → 翻开，**效果作废** ----
        {
            var ctx = ProbeBattle(new[] { amb, poke }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 4);
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "FixtureAmbush"), 2), RuleCodes.OK,
                      "面朝下打出");
            var u = Board(ctx, 0, 2);
            CheckTrue(u != null && u.FaceDown, "★ 打出来是**面朝下**的");
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_AmbPoke"), 2), RuleCodes.OK,
                      "在它翻开前打它 1 点");
            CheckTrue(!u.FaceDown, "★ **挨到伤害 → 翻开了**");
            Check(u.Attack, 1, "★ 攻击力还是 1 —— **那次的伏击效果没有了**（+3 没给）");
        }

        // ---- ② 撑到控制者下个回合 → 翻开并**触发** ----
        {
            var ctx = ProbeBattle(new[] { amb2 }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 4);
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "FixtureAmbush2"), 2), RuleCodes.OK,
                      "面朝下打出");
            var u = Board(ctx, 0, 2);
            CheckTrue(u.FaceDown, "先确认它是面朝下的");
            PassTurn(ctx); PassTurn(ctx);            // 一圈：对手回合 → 我的回合开始
            CheckTrue(!u.FaceDown, "★ **撑过一轮 ⇒ 翻开**");
            Check(u.Attack, 4, "★ 而且**伏击效果触发了**（1 + 3 = 4）—— "
                             + "和①对照：挨过打的那张只有 1");
        }
    }

    /// <summary>
    /// **传送 `Teleport`**（2026-09-13 A2）—— 规则书 `:219`「**当回合从牌库抽到即打出**时触发能力」；
    /// 问题机制那一节 `:234` 说得更死：「**仅当回合从牌库抽到时触发**」。
    ///
    /// 三条：① **当回合抽到的** → 打出来会触发 · ② **本来就在手里的** → **不触发** ·
    /// ③ 当回合抽到、但**下回合才打** → **不触发**（`BeginTurn` 清零）。
    /// </summary>
    static void TestTeleport()
    {
        var tele = new CardDef("FixtureTele", "FixtureTele", "unit",
                               "Teleport: Gain +3 Attack",
                               "common", "Test", 1, 1, 5, 0,
                               new[] { "Teleport" }, subtype: "Infantry");
        var t1 = new CardDef("FixtureTeleA", "FixtureTeleA", "unit", "Teleport: Gain +3 Attack",
                             "common", "Test", 1, 1, 5, 0, new[] { "Teleport" }, subtype: "Infantry");
        var t2 = new CardDef("FixtureTeleB", "FixtureTeleB", "unit", "Teleport: Gain +3 Attack",
                             "common", "Test", 1, 1, 5, 0, new[] { "Teleport" }, subtype: "Infantry");

        // ---- ① 当回合抽到 → 打出来触发 ----
        {
            var ctx = ProbeBattle(new CardDef[0], new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            ctx.Players[0].Deck.Add(ctx.NewInstance(t1));                 // 放到牌库**末尾**（`Draw` 从末尾抽）
            RuleCore.Draw(ctx, 0);
            CheckTrue(HandIdx(ctx, 0, "FixtureTeleA") >= 0, "抽到手里了");
            CheckTrue(t1.TriggerOps("teleport") != null, "`Teleport:` 的正文收下来了");
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "FixtureTeleA"), 2), RuleCodes.OK,
                      "把它打出来");
            Check(Board(ctx, 0, 2).Attack, 4,
                  "★ 1 + 3 —— **当回合抽到的那张，打出来触发了传送**");
        }

        // ---- ② 反例：**本来就在手里**的 → 不触发 ----
        {
            var ctx = ProbeBattle(new[] { tele }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 2);                            // `ProbeBattle` 的起手就在手里，不是这回合抽的
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "FixtureTele"), 2), RuleCodes.OK,
                      "打出一张**起手就在手里**的传送单位");
            Check(Board(ctx, 0, 2).Attack, 1,
                  "★ **不触发**（还是 1 攻）—— 判据是「**这回合从牌库抽到的**」，"
                  + "不打这个标记的话每张传送单位落地都会白拿一次效果");
        }

        // ---- ③ 反例：当回合抽到、**下回合才打** → 不触发 ----
        {
            var ctx = ProbeBattle(new CardDef[0], new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            ctx.Players[0].Deck.Add(ctx.NewInstance(t2));
            RuleCore.Draw(ctx, 0);
            PassTurn(ctx); PassTurn(ctx);                // 走一圈回到我的回合（`BeginTurn` 会清标记）
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "FixtureTeleB"), 2), RuleCodes.OK,
                      "隔了一个回合才把它打出来");
            Check(Board(ctx, 0, 2).Attack, 1,
                  "★ **不触发** —— 规则书写死了「**当回合**从牌库抽到」");
        }
    }

    /// <summary>
    /// **残骸 `Remnant`**（2026-09-13 A2）—— 规则书 `:203`「本部队死亡时**翻面**表示残骸；
    /// 残骸**受伤害或控制者回合结束时被摧毁**」。
    ///
    /// 四条：① 死亡 → **翻面留在格位上**（不进弃牌堆）· ② 挨一下就碎 → **这时才进弃牌堆** ·
    /// ③ 回合结束**自动摧毁** · ④ `reanimate` **从场上的残骸翻回来**（`When Reanimated` 要响）。
    /// </summary>
    static void TestRemnant()
    {
        var rem = new CardDef("FixtureRemnant", "FixtureRemnant", "unit", "",
                              "common", "Test", 1, 2, 5, 0,
                              new[] { KeywordTable.Remnant }, subtype: "Infantry");
        var kill = Tactic("T_RemKill", 0, "Deal 99 damage to a friendly unit");
        var raise = Tactic("T_Raise", 0, "Reanimate a friendly Remnant");

        // ---- ① 死亡 → 翻面（留在格位上、不进弃牌堆）----
        {
            var ctx = ProbeBattle(new[] { rem, kill }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            Place(ctx, 0, 2, rem, exhausted: true);
            int disc0 = ctx.Players[0].Discard.Count;
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_RemKill"), 2), RuleCodes.OK,
                      "把带残骸的单位打死");
            var u = Board(ctx, 0, 2);
            CheckTrue(u != null, "★ **格位上还有东西** —— 翻面成残骸了（`Board[2]` 不是空的）");
            CheckTrue(u != null && u.IsRemnant, "★ 它被标成了**残骸**");
            Check(u != null ? u.Health : -1, 1, "★ 残骸只有 **1 点生命**（挨任何一下就没）");
            Check(u != null ? u.Attack : -1, 0, "残骸攻 0（它是一张背面朝上的牌，没有能力）");
            Check(ctx.Players[0].Discard.Count, disc0 + 1,
                  "弃牌堆只多了**那张战术卡**（残骸还压在场上，没进弃牌堆）");
        }

        // ---- ② 残骸挨伤害 → 被摧毁（进弃牌堆）----
        {
            var poke = Tactic("T_RemPoke", 0, "Deal 1 damage to a friendly unit");
            var ctx = ProbeBattle(new[] { rem, kill, poke }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            Place(ctx, 0, 2, rem, exhausted: true);
            RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_RemKill"), 2);
            CheckTrue(Board(ctx, 0, 2) != null && Board(ctx, 0, 2).IsRemnant, "先造一具残骸");
            int disc1 = ctx.Players[0].Discard.Count;
            // 「受伤害」——**1 点就够**（残骸只有 1 血）。用自己那张伤害战术打它，省掉换回合
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_RemPoke"), 2), RuleCodes.OK,
                      "对残骸打 1 点伤害");
            CheckTrue(Board(ctx, 0, 2) == null, "★ 残骸挨一下就**被摧毁**（格位空了）");
            Check(ctx.Players[0].Discard.Count, disc1 + 2,
                  "★ **这时候那张卡才进弃牌堆**（+1 是残骸那张、+1 是刚打的战术卡）");
        }

        // ---- ③ 回合结束 → 自动摧毁 ----
        {
            var ctx = ProbeBattle(new[] { rem, kill }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            Place(ctx, 0, 2, rem, exhausted: true);
            RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_RemKill"), 2);
            CheckTrue(Board(ctx, 0, 2) != null && Board(ctx, 0, 2).IsRemnant, "先确认残骸在场上");
            PassTurn(ctx); PassTurn(ctx);              // 走完一整轮，回到我方回合结束那一刻
            CheckTrue(Board(ctx, 0, 2) == null,
                      "★ **控制者回合结束 ⇒ 残骸被摧毁**（规则书 `:203`）—— "
                      + "不摧毁的话它会永远占着那一格");
        }

        // ---- ④ `reanimate` 从**场上的残骸**翻回来 ----
        {
            var ctx = ProbeBattle(new[] { rem, kill, raise }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            Place(ctx, 0, 2, rem, exhausted: true);
            RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_RemKill"), 2);
            CheckTrue(Board(ctx, 0, 2) != null && Board(ctx, 0, 2).IsRemnant, "先造一具残骸");
            var back = Place(ctx, 0, 0, Unit("Watcher", 1, 0, 9), exhausted: true);   // 占位无所谓
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Raise"), -1), RuleCodes.OK,
                      "打出 `Reanimate a friendly Remnant`");
            var u = Board(ctx, 0, 2);
            CheckTrue(u != null && !u.IsRemnant,
                      "★ **它从残骸翻回来了**（不再是残骸）");
            Check(u != null ? u.Health : -1, 5, "翻回来的是**完整的单位**（5 血，不是 1）");
            Check(u != null ? u.Attack : -1, 2, "攻也回来了（2）");
            CheckTrue(back != null && back.Health == 9, "塔子单位没被牵连");
        }
    }

    /// <summary>
    /// **潮涌 `Tide X`**（2026-09-13 A2）—— 规则书 `:220`「从手牌打出时：本回合可打出 X 张额外复制；
    /// 费用与首张相同」。
    ///
    /// 三条：① X 张复制**进了手牌** · ② 它们**是临时卡**（回合结束消失 —— 规则书 `:229`
    /// 把潮涌复制与天赋/伴生并列）· ③ 复制品的**费用跟首张一致**（首张被打折过时也要一致）。
    /// </summary>
    static void TestTide()
    {
        // ---- ① / ② ----
        {
            var tide = new CardDef("FixtureTide", "FixtureTide", "unit", "",
                                   "common", "Test", 2, 2, 3, 0,
                                   new[] { "Tide 2" }, subtype: "Infantry");
            var ctx = ProbeBattle(new[] { tide }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 4);
            int hand0 = ctx.Players[0].Hand.Count;
            CheckTrue(tide.Has(KeywordTable.Tide), "`Tide` 关键词认得出");
            Check(tide.KwValue(KeywordTable.Tide), 2, "`Tide 2` 的值读得到（= 2）");
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "FixtureTide"), 2), RuleCodes.OK,
                      "打出一张 `Tide 2` 的部队");
            Check(ctx.Players[0].Hand.Count, hand0 + 1,
                  "★ **手里多了 2 张复制**（打掉 1 张、又回来 2 张 ⇒ 净 +1）");
            int markedCopies = 0;
            foreach (var h in ctx.Players[0].Hand) if (ctx.MarkedCount(h) > 0) markedCopies++;
            Check(markedCopies, 2,
                  "★ 那 2 张**标成了临时卡**（规则书 `:229` —— 不标就会**赖在手里不走**）");

            // 回合结束 → 临时卡从「手牌」消失（规则书 :229「未打出即消失」）
            int inHand = 0;
            foreach (var c in ctx.Players[0].Hand) if (c.Card == tide) inHand++;
            Check(inHand, 2, "回合结束之前它们还在手里");
            PassTurn(ctx);
            int after = 0;
            foreach (var c in ctx.Players[0].Hand) if (c.Card == tide) after++;
            Check(after, 0, "★ **回合结束 ⇒ 复制品从手里消失**（进「移出游戏」区，不是弃牌堆）");
        }

        // ---- ③ 费用与首张一致（首张被打折时）----
        {
            var tide = new CardDef("FixtureTide2", "FixtureTide2", "unit", "",
                                   "common", "Test", 3, 2, 3, 0,
                                   new[] { "Tide 1" }, subtype: "Infantry");
            var ctx = ProbeBattle(new[] { tide }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 4);      // ⚠️ 别推太远：20 张垫牌会抽空、督军吃疲劳（实测踩过）
            // 先给这个 id 挂一条 -1 的折扣（模拟「首张被打折」）
            ctx.CostMods.Add(new CostMod { Player = 0, Key = tide.Id, Delta = -1 });
            int paid = RuleCore.CostOf(ctx, 0, tide);
            Check(paid, 2, "首张实付 2（牌面 3 − 1）");
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "FixtureTide2"), 2), RuleCodes.OK,
                      "打出首张");
            Check(RuleCore.CostOf(ctx, 0, tide), 2,
                  "★ **复制品的费用和首张一样**（还是 2）—— "
                  + "不补那条 `CostMod` 的话这里会回到 3（牌面价）");
        }
    }

    /// <summary>
    /// **激励 `Stimulation`**（2026-09-13 A2）—— 规则书 `:212`「**被战术选中时、结算前**：触发能力」。
    ///
    /// 🔴 **「结算前」这三个字是可以验的**：让激励的正文给单位加血，再用一张会造成致命伤的战术打它 ——
    /// **先加血就活、后加血就死**。所以这一条同时钉住「触发了」和「在结算之前」。
    /// ⚠️ 卡面**没写「友方」**（和突触 `:217` 的「被**友方**战术选中时」不同）⇒ 谁的战术都算，照字面来。
    /// </summary>
    static void TestStimulation()
    {
        var stim = new CardDef("FixtureStim", "FixtureStim", "unit",
                               "Stimulation: Gain +5 Health",
                               "common", "Test", 1, 0, 5, 0,
                               new[] { "Stimulation" }, subtype: "Infantry");
        var heal = Tactic("T_StimHit", 0, "Deal 6 damage to a friendly unit");

        // ---- ① 被**友方**战术选中 → 触发，而且**在结算之前** ----
        {
            var ctx = ProbeBattle(new[] { stim, heal }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            var u = Place(ctx, 0, 2, stim, exhausted: true);
            Place(ctx, 0, 1, Unit("Bystander", 1, 0, 5), exhausted: true);
            CheckTrue(stim.TriggerOps("stimulation") != null, "`Stimulation:` 的正文收下来了");
            Check(u.Health, 5, "先 5 血");

            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_StimHit"), 2), RuleCodes.OK,
                      "用一张**会致命**的战术（6 伤）打它");
            CheckTrue(u.IsAlive, "★ 它**还活着** —— 激励在**结算之前**先给了 +5 血（5 → 10 → 挨 6 剩 4）");
            Check(u.Health, 4, "★ 血量正好是 5 + 5 - 6 = 4（**先加血后挨打**才算得出来）");
        }

        // ---- ② 反例：**没有**激励的单位被同一张战术打 → 直接死 ----
        {
            var plain = Unit("FixturePlain", 1, 0, 5);
            var ctx = ProbeBattle(new[] { plain, heal }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            var u = Place(ctx, 0, 2, plain, exhausted: true);
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_StimHit"), 2), RuleCodes.OK,
                      "同样的战术打一个**没有激励**的单位");
            CheckTrue(!u.IsAlive, "★ 没有激励 ⇒ 5 血挨 6 伤**就死了**（这条证明上面那个活下来是激励干的）");
        }
    }

    /// <summary>
    /// **起义 `Uprising`**（2026-09-13 A2）—— 规则书 `:222`「本单位**之后**部署的部队，
    /// 在其部署当回合触发能力」；英文原版 `:377`「Trigger an ability **each time a troop is
    /// deployed after this one**」。
    ///
    /// 三条：① **之后**部署一个部队 → 响 · ② 它**自己**落地那次**不响**（「之后」）·
    /// ③ 一个回合里部署两次 → **响两次**（`each time`，不是一次性的）。
    /// </summary>
    static void TestUprising()
    {
        // ⚠️ 正文用「对一个随机敌人造成 1 点伤害」而不是 `Gain +1 Attack` ——
        //    后者**没写目标 = 己方全体**（本工程的既定口径），一次触发会同时改好几个单位的数值，
        //    根本分不清「谁响了、响了几次」。伤害则是**可数的**：敌方总血量掉几点就是响了几次。
        const string body = "Uprising: Deal 1 damage to a random enemy";
        var up = new CardDef("FixtureUprising", "FixtureUprising", "unit", body,
                             "common", "Test", 1, 1, 9, 0, new[] { "Uprising" }, subtype: "Infantry");
        var up2 = new CardDef("FixtureUprising2", "FixtureUprising2", "unit", body,
                              "common", "Test", 1, 1, 9, 0, new[] { "Uprising" }, subtype: "Infantry");
        var troop = Unit("FixtureTroop", 1, 0, 5);

        int FoeHp(BattleContext c)
        {
            int sum = 0;
            for (int s = 0; s < BoardSpec.Size; s++)
                if (c.Players[1].Board[s] != null) sum += c.Players[1].Board[s].Health;
            // ⚠️ **别再单独加一次 `Players[1].Warlord.Health`** —— 督军就在 `Board[4]` 上，
            //    而且是**同一个对象**（`PlayerState.Warlord` 的注释写着「便于直接取用，别写成两份」）。
            //    第一版就是这么双计的，于是每个数都恰好翻倍。
            return sum;
        }

        // ---- ① 之后部署 → 响；② 自己落地那次 → 不响 ----
        {
            var ctx = ProbeBattle(new[] { up, up2, troop, troop }, new[] { Unit("EFoe", 1, 0, 30) });
            ToP1Turn(ctx, 4);
            Place(ctx, 0, 0, up, exhausted: true);
            CheckTrue(up.TriggerOps("uprising") != null, "`Uprising:` 的正文收下来了");

            int hp0 = FoeHp(ctx);
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "FixtureUprising2"), 1), RuleCodes.OK,
                      "把另一张带起义的部队部署下去");
            Check(hp0 - FoeHp(ctx), 1,
                  "★ 部署一个部队 → **先在场的那张响了、刚落地那张没响**（合计 1 点伤害）—— "
                  + "两张都响的话这里是 2（判据是「**之后**部署的」，不是「每次有人落地」）");

            int hp1 = FoeHp(ctx);
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "FixtureTroop"), 2), RuleCodes.OK,
                      "再部署一个部队（在它**之后**）");
            Check(hp1 - FoeHp(ctx), 2,
                  "★ 两张带起义的**各响一次**（合计 2 点）");
        }

        // ---- ③ 同一回合部署两次 → 响两次（`each time`）----
        {
            var ctx = ProbeBattle(new[] { up, troop, troop }, new[] { Unit("EFoe", 1, 0, 30) });
            ToP1Turn(ctx, 4);
            Place(ctx, 0, 0, up, exhausted: true);
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "FixtureTroop"), 1), RuleCodes.OK,
                      "部署第一个");
            int hp1 = FoeHp(ctx);
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "FixtureTroop"), 2), RuleCodes.OK,
                      "同一回合再部署一个");
            Check(hp1 - FoeHp(ctx), 1,
                  "★ **又响了一次**（做成「每回合一次」的话这里是 0）—— 规则书 `:222` 的 `each time`");
        }
    }

    /// <summary>
    /// **突触 `Synapse`**（2026-09-13 A2）—— 规则书 `:217`「被**友方战术**选中时：
    /// 对**相邻**部队/单位**重复效果**（依战术而定）」。
    ///
    /// 语义照原版反编译 `CardScript__TargetedSpellPlayed.c:55-75`（五条，见调用点注释）。
    /// 三条必须各钉一次：① 相邻的**重复到了** · ② **隔一格的不重复** ·
    /// ③ **敌方战术**选中它**不触发**（原版判的是「施放者与目标同一方」）。
    /// </summary>
    static void TestSynapse()
    {
        CardDef SynapseGuy(string n, int hp = 20)
        {
            return new CardDef(n, n, "unit", "", "common", "Test", 1, 0, hp, 0,
                               new[] { KeywordTable.Synapse }, subtype: "Infantry");
        }

        // ---- ① 友方战术选中带突触的单位 → 相邻的**也吃一次效果** ----
        {
            var syn = SynapseGuy("FixtureSyn");
            var heal = Tactic("T_SynHeal", 0, "Heal 3 to a friendly unit");
            var ctx = ProbeBattle(new[] { syn, heal }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            var mid = Place(ctx, 0, 2, syn, exhausted: true);        // 锚点
            var left = Place(ctx, 0, 1, Unit("FriendL", 1, 0, 10), exhausted: true);
            var right = Place(ctx, 0, 3, Unit("FriendR", 1, 0, 10), exhausted: true);
            var far = Place(ctx, 0, 5, Unit("FriendFar", 1, 0, 10), exhausted: true);
            mid.Health = 10; left.Health = 4; right.Health = 4; far.Health = 4;

            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_SynHeal"), 2), RuleCodes.OK,
                      "对带突触的单位用一张**友方**战术");
            Check(mid.Health, 13, "锚点自己被治（4 → 13 是 10 + 3）");
            Check(left.Health, 7, "★ **左边相邻的也吃了一次**（4 → 7）");
            Check(right.Health, 7, "★ **右边相邻的也吃了一次**（4 → 7）");
            Check(far.Health, 4, "★ 隔了一格的那个**没吃**（相邻 ≠ 全场）");
        }

        // ---- ② 反例：**敌方**战术选中它 → **不触发**（原版判「施放者与目标同一方」）----
        {
            var syn = SynapseGuy("FixtureSyn2");
            var foeHit = Tactic("T_FoeSyn", 0, "Deal 1 damage to an enemy troop");
            var ctx = ProbeBattle(new[] { Unit("P1Dummy", 1, 0, 5) },
                                  new[] { foeHit, Unit("FoeNbr", 1, 0, 20) });
            ToP1Turn(ctx, 2);
            var mid = Place(ctx, 0, 2, syn, exhausted: true);
            var left = Place(ctx, 0, 1, Unit("FriendL2", 1, 0, 20), exhausted: true);
            PassTurn(ctx);
            CheckCode(RuleCore.PlayTactic(ctx, 1, HandIdx(ctx, 1, "T_FoeSyn"), 2), RuleCodes.OK,
                      "**对手**用一张战术选中这个带突触的单位");
            Check(mid.Health, 19, "锚点挨了 1 点");
            Check(left.Health, 20,
                  "★ **相邻的没被重复**（还是 20）—— 判据是「**友方**战术」，"
                  + "不判方向的话这里会实得 19（对面一张战术把我们两个人都打了）");
        }
    }

    /// <summary>
    /// **虫群 `Swarm`**（2026-09-13 A2）—— 规则书 `:216`「打出在同名部队**左侧**时：合并
    /// （置于其下，**攻击生命相加**）」。
    ///
    /// 语义照原版反编译 `CardScript__ResolveCardPlayed.c`：只看**右边的紧邻格** + **卡名全等**。
    /// 所以三条都要钉：合并 ✅ · 名字不同不合并 · **同名在左边也不合并**（只看右边）。
    /// </summary>
    static void TestSwarm()
    {
        CardDef SwarmGuy(string n, int atk, int hp)
        {
            return new CardDef(n, n, "unit", "", "common", "Test", 1, atk, hp, 0,
                               new[] { KeywordTable.Swarm }, subtype: "Infantry");
        }

        // ---- ① 合并：打出在**右侧同名部队**的左边 ----
        {
            var a = SwarmGuy("SwarmGuy", 2, 3);
            var b = SwarmGuy("SwarmGuy", 2, 3);
            var ctx = ProbeBattle(new[] { b }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            Place(ctx, 0, 2, a, exhausted: true);            // 场上已有同名（右边那格）
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "SwarmGuy"), 1), RuleCodes.OK,
                      "把同名的虫群部队打到它**左边**");
            Check(SlotOf(ctx, 0, "SwarmGuy"), 2, "★ 合并之后只剩**右边那一格**有单位");
            var host = Board(ctx, 0, 2);
            Check(host.Attack, 4, "★ **攻击相加**（2 + 2）");
            Check(host.Health, 6, "★ **生命相加**（3 + 3）");
            Check(host.SwarmUnder.Count, 1, "★ 新来的那张**压在下面**（`SwarmUnder`）");

            // 宿主死掉 → 压着的也一起进弃牌堆
            var kill = Tactic("T_KillSwarm", 0, "Deal 99 damage to a friendly unit");
            Give(ctx, 0, kill);
            int disc0 = ctx.Players[0].Discard.Count;
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_KillSwarm"), 2), RuleCodes.OK,
                      "把合并后的宿主打死");
            Check(SlotOf(ctx, 0, "SwarmGuy"), -1, "宿主确实死了");
            // 弃牌堆 +2（宿主自己那张 + 压着的 1 张；战术卡本身也进弃牌堆 ⇒ 实际 +3）
            Check(ctx.Players[0].Discard.Count, disc0 + 3,
                  "★ 宿主阵亡 ⇒ **下面压着的那张一起进弃牌堆**"
                  + "（只进 2 的话，合并进去的牌就永远消失了）");
        }

        // ---- ② 反例：**名字不同**不合并 ----
        {
            var a = SwarmGuy("SwarmGuyA", 2, 3);
            var b = new CardDef("SwarmGuyB", "SwarmGuyB", "unit", "", "common", "Test", 1, 2, 3, 0,
                                new[] { KeywordTable.Swarm }, subtype: "Infantry");
            var ctx = ProbeBattle(new[] { a }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            Place(ctx, 0, 2, b, exhausted: true);
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "SwarmGuyA"), 1), RuleCodes.OK,
                      "打一张**不同名**的到它左边");
            CheckTrue(Board(ctx, 0, 1) != null && Board(ctx, 0, 2) != null,
                      "★ 名字不同 ⇒ **不合并**（两张都还在）—— 不判名字的话这里会只剩一张");
        }

        // ---- ③ 反例：**同名在左边**不合并（卡面写的是「打出在同名部队**左侧**」）----
        {
            var a = SwarmGuy("SwarmGuyL", 2, 3);
            var b = SwarmGuy("SwarmGuyL", 2, 3);
            var ctx = ProbeBattle(new[] { b }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            Place(ctx, 0, 1, a, exhausted: true);
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "SwarmGuyL"), 2), RuleCodes.OK,
                      "把同名的打到它**右边**");
            CheckTrue(Board(ctx, 0, 1) != null && Board(ctx, 0, 2) != null,
                      "★ 只看**右边的紧邻格**（原版 `GetAdjacentUnitRight`）—— "
                      + "改成「全盘找同名」的话这条会实得只剩一张");
        }

        // ---- ④ `When a friendly unit triggers Swarm, …` 真的响 ----
        {
            var watcher = new CardDef("FixtureSwarmWatch", "FixtureSwarmWatch", "unit",
                                      "When a friendly unit triggers Swarm, gain +2 Attack",
                                      "common", "Test", 1, 1, 9, 0, null, subtype: "Infantry");
            var a = SwarmGuy("SwarmGuyW", 2, 3);
            var b = SwarmGuy("SwarmGuyW", 2, 3);
            var ctx = ProbeBattle(new[] { b }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            Place(ctx, 0, 0, watcher, exhausted: true);
            Place(ctx, 0, 2, a, exhausted: true);
            Check(Board(ctx, 0, 0).Attack, 1, "监听者一开始 1 攻");
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "SwarmGuyW"), 1), RuleCodes.OK,
                      "触发一次虫群合并");
            Check(Board(ctx, 0, 0).Attack, 3,
                  "★ **`When a friendly unit triggers Swarm` 响了**（1 → 3 攻）—— "
                  + "`swarm` 登记进 `Implemented` 之后这条短语自动点亮，但**广播得有人发**");
        }
    }

    /// <summary>
    /// **巧技 `Artifice`**（2026-09-13 A2）—— 规则书 `:168`「**每次打出战术时**触发额外效果」。
    ///
    /// 卡面两种写法都有（实测 15 张：`Artifice: …` 6 张、**正文裸写** 9 张），
    /// 所以这条测试**两种都要钉**，外加两条反例（不是自己打的不响 / 歧义就不收）。
    /// </summary>
    static void TestArtifice()
    {
        // ---- ① 有前缀：`Artifice: Gain +1 Attack` ----
        {
            var art = new CardDef("FixtureArt", "FixtureArt", "unit",
                                  "Artifice: Gain +1 Attack",
                                  "common", "Test", 1, 2, 9, 0, new[] { "Artifice" }, subtype: "Infantry");
            var spell = Tactic("T_ArtSpell", 0, "Deal 1 damage to an enemy");
            var ctx = ProbeBattle(new[] { art, spell }, new[] { Unit("EFoe", 1, 0, 30) });
            ToP1Turn(ctx, 2);
            var a = Place(ctx, 0, 0, art, exhausted: true);
            Place(ctx, 1, 1, Unit("EFoe", 1, 0, 30), exhausted: true);

            CheckTrue(art.TriggerOps("artifice") != null, "`Artifice:` 的正文被收下来了");
            Check(a.Attack, 2, "打出战术之前 2 攻");
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_ArtSpell"), 1), RuleCodes.OK,
                      "打出一张战术卡");
            Check(a.Attack, 3, "★ **打出战术 → 巧技触发**（2 → 3 攻）—— 不读 `Artifice` 的话这里是 2");
        }

        // ---- ② 反例：**对手**打出战术，我的巧技**不该**响（收听者是「**你**那一排」）----
        {
            var art = new CardDef("FixtureArt2", "FixtureArt2", "unit",
                                  "Artifice: Gain +1 Attack",
                                  "common", "Test", 1, 2, 9, 0, new[] { "Artifice" }, subtype: "Infantry");
            var foeSpell = Tactic("T_FoeSpell", 0, "Deal 1 damage to an enemy");
            var ctx = ProbeBattle(new[] { art, Unit("P1Dummy", 1, 0, 5) }, new[] { foeSpell });
            ToP1Turn(ctx, 2);
            var a = Place(ctx, 0, 0, art, exhausted: true);
            Place(ctx, 0, 1, Unit("P1Dummy", 1, 0, 5), exhausted: true);   // 给对手一个合法目标
            Place(ctx, 1, 1, Unit("EFoe", 1, 0, 30), exhausted: true);
            PassTurn(ctx);                                    // 换对手行动

            CheckCode(RuleCore.PlayTactic(ctx, 1, HandIdx(ctx, 1, "T_FoeSpell"), 1), RuleCodes.OK,
                      "**对手**打出一张战术卡");
            Check(a.Attack, 2, "★ **对手打出的战术不触发我的巧技**（还是 2 攻）—— "
                             + "不按「谁打出的」筛的话这条会实得 3");
        }

        // ---- ③ 裸写正文：没有 `Artifice:` 前缀，**整条 desc 就是正文** ----
        //   实测 200 张卡是这样（`Dark Apostle` / `Blood Claw` / `Hybrid Metamorph`…）——
        //   而 `AddTriggerOp` 要求冒号 ⇒ 它们的效果**一条都收不到**、静默不发生。
        {
            var bare = new CardDef("FixtureArtBare", "FixtureArtBare", "unit",
                                   "Deal 2 damage to a random enemy",
                                   "common", "Test", 1, 2, 9, 0, new[] { "Artifice" }, subtype: "Infantry");
            CheckTrue(bare.TriggerOps("artifice") != null,
                      "★ **没有前缀时整条 desc 就是正文**（`CollectBareKeywordBody`）—— "
                      + "不收的话这一族 200 张卡的效果**静默不发生**");
            var spell = Tactic("T_BareSpell", 0, "Deal 1 damage to an enemy");
            var ctx = ProbeBattle(new[] { bare, spell }, new[] { Unit("EFoe", 1, 0, 30) });
            ToP1Turn(ctx, 2);
            Place(ctx, 0, 0, bare, exhausted: true);
            var foe = Place(ctx, 1, 1, Unit("EFoe", 1, 0, 30), exhausted: true);
            int wpBefore = ctx.Players[1].Warlord.Health;
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_BareSpell"), 1), RuleCodes.OK,
                      "再打一张战术卡");
            // 「a random enemy」的池子里**含督军** ⇒ 只能量两侧合计（1 点来自战术、2 点来自巧技）
            int dealt = (30 - foe.Health) + (wpBefore - ctx.Players[1].Warlord.Health);
            Check(dealt, 3, "★ **战术 1 伤 + 巧技那条裸写正文 2 伤**都落到了敌方"
                          + "（收不到正文的话这里只有 1）");
        }

        // ---- ④ 反例：**两个**带正文的关键词、又没写前缀 ⇒ 歧义 ⇒ **不收**（宁可不动）----
        {
            var amb = new CardDef("FixtureAmbig", "FixtureAmbig", "unit",
                                  "Gain +1 Attack",
                                  "common", "Test", 1, 2, 9, 0,
                                  new[] { "Rally", "Ferocity" }, subtype: "Infantry");
            CheckTrue(amb.TriggerOps("rally") == null && amb.TriggerOps("ferocity") == null,
                      "★ 两个带正文的关键词、又没写 `X:` 前缀 ⇒ **歧义，不认**"
                      + "（`Morkai Eliminator` 那种；猜错就是在**错的时机**放效果）");
        }
    }

    // 🗑️ 2026-09-14 A7 删掉：这里原来有个 `IsAuraSentence`（判「`adjacent … have/has …`」）。
    //    它是**光环判据的第二份**（而且比生产那份窄：只认 `adjacent` 打头的），
    //    光环层接上之后报表直接转调 `Auras.LooksLikeAura`（形状）/ `CardDef.AuraSpecs`（认没认下）——
    //    留着一份「看着像判据」的死代码，下一个人迟早会照着它改（工程红线：判据只此一处）。

    /// <summary>
    /// **光环解析层的验收**（2026-09-14 A7 第 2 步）。
    ///
    /// 量的是 `Auras.TryParse` → `CardDef.AuraSpecs` 这一段的**形状**：锚点、筛选、载荷、时长。
    /// **结算不在这一轮**（A7 第 3 步）—— 所以这里**没有**「打出去之后谁涨了多少」那类断言。
    ///
    /// 挑的卡覆盖**每一个筛选维度**，每种各一张（全池 30 张里各维度的代表）：
    ///   · 相邻型 + 裸关键词载荷（`Baneblade Tank`）
    ///   · 相邻型 + **兵种词**（`Honoured Ethereal` 的 `adjacent **troops**` ⇒ 排督军）
    ///   · 己方全体 + **排除自己**（`Company Ancient`）
    ///   · **两个兵种词**（`Devilfish` 的 `Infantry **and** Drones` ⇒ 并集）
    ///   · **关键词筛**（`Winged Autarch` 的 `Flying units`）
    ///   · **卡名筛**（`Alluress` 的 `Daemonette` —— 它是一张卡的卡名，不是兵种）
    ///   · **敌方**（`Bringer of decay` 的 `Enemies have Vulnerable 1`，**主语是空的**）
    ///   · **时长限定**（`Fyrri Askar` 的 `Invulnerable during your turn`）
    ///   · **费用 + 属性合体**（`Winged Daemon Prince`）
    ///   · **反例**：裸 `+N` 的两张**故意不收**（`Genestealer Familiar` / `Cadre Fireblade`）
    /// </summary>
    static void TestAuraParse()
    {
        var pool = CardDatabase.Load();

        AuraSpec a;
        // ① 相邻型：不筛兵种、载荷是个关键词带值
        CheckTrue(Auras.TryParse("Adjacent units have Armour 1", out a), "★ 认得出 `Adjacent units have Armour 1`");
        CheckTrue(a.Adjacent && !a.Enemy && !a.ExcludeSelf, "★ 相邻型 · 不打敌方 · 不排除自己");
        CheckTrue(a.Filter != null && a.Filter.IsEmpty, "★ 筛选条件为空（`units` **不筛** —— 含督军）");
        CheckTrue(a.Payload == "Armour 1" && a.CostLess == 0, "★ 载荷 `Armour 1`、没有费用那半");

        // ② `adjacent troops` —— `troops` 是**真筛选**（排督军），不是噪声词
        CheckTrue(Auras.TryParse("Adjacent troops have Vanguard", out a), "★ 认得出 `Adjacent troops have Vanguard`");
        CheckTrue(a.Filter != null && a.Filter.KindWord == "troops",
                  "★ `troops` 进筛选词 —— 🔴 它是这一段里**唯一**能把督军排掉的判据"
                  + "（`units` 则一律不筛；拿 `unit` 当筛选词会**静静筛掉督军**）");

        // ③ 己方全体 + 排除自己
        CheckTrue(Auras.TryParse("Your other units have +2 Ranged Attack", out a), "★ 认得出 `Your other units …`");
        CheckTrue(!a.Adjacent && !a.Enemy && a.ExcludeSelf, "★ 己方 · `other` ⇒ **排除自己**");

        // ④ **两个兵种词是并集**（`Infantry and Drones`）—— `ParseTarget` 只留第一个，会静默少筛一类
        var dv = PoolCard(pool, "Devilfish");
        CheckTrue(dv != null && dv.AuraSpecs.Count == 1, "★ `Devilfish` 收下来 1 条光环");
        if (dv != null && dv.AuraSpecs.Count == 1)
        {
            var f = dv.AuraSpecs[0].Filter;
            CheckTrue(f != null && f.KindWord == "infantry"
                      && f.KindAnyOf != null && f.KindAnyOf.Count == 1 && f.KindAnyOf[0] == "drones",
                      "★ `Infantry **and** Drones` ⇒ `KindWord=infantry` + `KindAnyOf=[drones]`（**并集**）");
        }

        // ⑤ 关键词筛（`Flying units`）· ⑥ 卡名筛（`Daemonette`）· ⑦ 敌方（主语为空）
        CheckTrue(Auras.TryParse("Your other Flying units have +2 Melee and +2 Ranged Attack", out a)
                  && a.Filter != null && a.Filter.Keyword == "flying",
                  "★ `Flying units` ⇒ 关键词筛（不是兵种词）");
        var al = PoolCard(pool, "Alluress");
        CheckTrue(al != null && al.AuraSpecs.Count == 1
                  && al.AuraSpecs[0].Filter != null && al.AuraSpecs[0].Filter.Name != null,
                  "★ `Friendly Daemonette have Flank` ⇒ **卡名筛**"
                  + "（`Daemonette` 是卡名 `EC7`，**不是**兵种词 —— 判据转调 `EffectText.SubjectOf`）");
        CheckTrue(Auras.TryParse("Enemies have Vulnerable 1", out a) && a.Enemy
                  && a.Filter != null && a.Filter.IsEmpty,
                  "★ `Enemies have Vulnerable 1` —— **主语是空的**也能认（第一版正则要求至少有主语，"
                  + "把这一张整张漏掉了）");

        // ⑧ 时长限定 + 费用那半
        CheckTrue(Auras.TryParse("Friendly units with Pack have Invulnerable during your turn", out a)
                  && a.Duration == "duringyourturn"
                  && a.Filter != null && a.Filter.Keyword == "pack"
                  && a.Payload == "Invulnerable",
                  "★ `during your turn` 摘进 `Duration`、**没被当成载荷的一部分丢掉**（丢了 = 静默变成永久）");
        CheckTrue(Auras.TryParse("Other friendly Daemons cost 2 less and have +2 [attack]", out a)
                  && a.CostLess == 2 && a.ExcludeSelf,
                  "★ `cost 2 less **and** have +2 [attack]` ⇒ 费用那半进 `CostLess`、"
                  + "主语**不能**把 `costs 2 less and` 一起吃进去（那样筛选条件就错了）");

        // ⑨ 那两张「卡面裸 `+N`」的（`Genestealer Familiar` / `Cadre Fireblade`）——
        //    **2026-09-14 已从数据侧修掉**：`cardface_fixes.json` 的 `desc` 列补成**规范写法**
        //    `+1 Attack` / `+2 Ranged Attack`（照 `_manual_desc_note` 立的规矩 ——
        //    「写入的是我们能解析的规范写法，**不是卡面图标的字形**」）。
        //    **两张卡图逐张亲读过**：前者粉圈白拳 = 近战、后者紫圈枪 = 远程。
        //    ⚠️ **`BareSignedAmbiguous` 那道闸留着** —— 它防的是**将来**再出现裸 `+N`
        //    （那时属性在文本层确实判不出来，宁可认不出，也别猜成错的那一张）。
        var gf = PoolCard(pool, "Genestealer Familiar");
        var cf = PoolCard(pool, "Cadre Fireblade");
        CheckTrue(gf != null && gf.AuraSpecs.Count == 1,
                  "★ `Genestealer Familiar` 的 `Adjacent units have +1 Attack` 收得下");
        CheckTrue(cf != null && cf.AuraSpecs.Count == 1,
                  "★ `Cadre Fireblade` 的 `Your other Infantry and Battlesuit troops have +2 Ranged Attack` 收得下");
        // 属性的**方向**也要钉：这两张写法一模一样，在卡面上却**不是同一个属性**
        //  ⚠️ 比的是 **`NormalizeAttr` 之后**的值 —— 载荷里的原始 token 是 `melee`
        //     （`GivePayload` 那条正则认得它），落到字段上前**必须**归一成 `attack`。
        {
            var gops = gf == null || gf.AuraSpecs.Count == 0 ? null : GivePayload.Parse(gf.AuraSpecs[0].Payload);
            var cops = cf == null || cf.AuraSpecs.Count == 0 ? null : GivePayload.Parse(cf.AuraSpecs[0].Payload);
            CheckTrue(gops != null && gops.Count > 0
                      && RuleCore.NormalizeAttr(gops[0].Attr) == "attack",
                      "★ 前者是**近战**（卡面粉拳图标）");
            CheckTrue(cops != null && cops.Count > 0
                      && RuleCore.NormalizeAttr(cops[0].Attr) == "ranged",
                      "★ 后者是**远程**（卡面紫枪图标）—— 兜底成同一条规则就会**静默错一张**");
        }
        CheckTrue(!Auras.TryParse("Adjacent units have +1", out a) && !Auras.TryParse("have +2", out a),
                  "★ **裸 `+N` 本身仍然不收**（闸留着防将来 —— 文本层判不出属性）");

        // ⑩ 不该被吃掉的：条件从句（它们**不是**光环）
        CheckTrue(!Auras.LooksLikeAura("If it has Flying, deal 6 damage instead"),
                  "★ `If it has Flying, …` **不是**光环（`^` 锚定 + 锚点词白名单挡住了）");
        CheckTrue(!Auras.LooksLikeAura("If the target has Armour, deal 8 damage instead"),
                  "★ `If the target has Armour, …` 同上");
        // ⚠️ **2026-09-14 第 3 步起这条反过来了**：`Has Flying during your turn` 原来是「不做」那一族
        //    （裸关键词 + 时长限定），现在**收进光环层**当**自指型**做（`AuraSpec.Self`）——
        //    它和其余 29 张是同一件事：**只在拥有者回合有效**的持续加成。
        //    ⇒ 卡面上那 3 张督军的「自己回合里会飞」靠它；**裸 `Flying` 已从这三张的 `keywords` 里去掉**
        //      （卡面写的是带限定的那句，见 `cardface_fixes.json` 的 `_manual_keywords`）。
        CheckTrue(Auras.LooksLikeAura("Has Flying during your turn"),
                  "★ `Has Flying during your turn` **是**光环（自指型，督军 3 张）");
        AuraSpec selfAura;
        CheckTrue(Auras.TryParse("Has Flying during your turn", out selfAura)
                  && selfAura.Self && selfAura.Duration == "duringyourturn" && selfAura.Payload == "Flying",
                  "★ 它解成「**只作用于自己** + `during your turn` + `Flying`」");
        CheckTrue(Auras.TryParse("Adjacent Remnants do not disappear at the end of your turn", out selfAura)
                  && selfAura.RemnantStay && selfAura.Adjacent,
                  "★ `Nemesor Zahndrekh` 那句解成 `RemnantStay`（**改残骸寿命**，不是属性也不是关键词）");
    }

    /// <summary>
    /// **光环结算的验收**（2026-09-14 A7 第 3 步）。
    ///
    /// 形状是「**整份摘掉再重加**」（照原版 `CardScript__UpdateWhileInPlay`，设计稿 §8.2/§8.4）：
    /// 棋盘一变就 `Auras.Recompose` 一次。这里钉四件事，缺一条就有盲区：
    ///   · **钩子真的挂了**（走真实部署路径 `DeployFree`，不是手工摆完再手动重算）
    ///   · **筛选维度算对了**（相邻 / 排除自己 / `troops` 排督军 / 关键词 / 敌方）
    ///   · **收回**：来源离场，加成**一分不多一分不少**地退回去
    ///     （🔴 这条最关键 —— 盲减会把单位**自己印的**护甲一起扣掉）
    ///   · **可叠加**（用户 2026-09-14 拍板：`Armour 2` 旁边再来 `Armour 1` ⇒ 3）
    ///
    /// ⚠️ **手工 `Place` 之后必须自己调一次 `Auras.Recompose`** —— `Place` 是测试夹具，
    ///    它**绕过**了 `RuleCore` 的那几个部署入口（钩子挂在那里）。第 ① 条专门走真钩子。
    /// </summary>
    /// <summary>
    /// **2026-09-16 那一批「剩下那几张卡」的结算级断言** —— 逐条对应 `资料/战术卡剩余7条_语义查证.md` §十。
    ///
    /// 为什么必须单开一节：这一批的每一条都是「**解析得出 ≠ 机制在跑**」的高发区 ——
    /// 覆盖率报表只证明「句子认了」，而这个工程为「报表绿着但机制没跑」付过好几次学费
    /// （见 `阵营推进_清单与交接.md` 里那条跨轮教训）。这里逐条量**棋盘状态真的变了**。
    ///
    /// 覆盖：① 残骸广播（`SAU61`）② 宇宙巨蛇触发灵魂石能力且**不付石**（`ASH52`）
    /// ③ 诺恩使节的 `every time` 降费（`TL83`，数据侧）④ 督军费用全 0（9 张开图核过的收账）。
    /// </summary>
    static void TestBatch0916()
    {
        var pool = CardDatabase.Load();

        // ---------- ① `SAU61 Undying Legions`：友方部队**变成残骸**时获得护盾 ----------
        {
            var ul = CreatePool.FindByName(pool, "Undying Legions");
            var nec = CreatePool.FindByName(pool, "Necron Warrior");     // 卡面 `Remnant. Vanguard`
            CheckTrue(ul != null && nec != null, "卡池里有 `Undying Legions` 与 `Necron Warrior`");
            if (ul != null && nec != null)
            {
                var kill = Tactic("T_KillOwn2", 0, "Deal 99 damage to a friendly unit");
                var ctx = BattlePool(new[] { ul, kill }, new[] { Unit("X", 1, 1, 5) }, pool,
                                     warlordFaction: "Sautekh");
                ToP1Turn(ctx, 1);
                ctx.Players[0].Energy = 12;
                CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "Undying Legions"), -1),
                          RuleCodes.OK, "打出真卡 `Undying Legions`（整句现在解析得下）");

                var u = Place(ctx, 0, 0, nec, exhausted: true);
                CheckTrue(!u.IsRemnant, "（前提）它是活着的");
                CheckTrue(!u.Has("shield"), "（前提）它身上没有 Shield");

                CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_KillOwn2"), 0),
                          RuleCodes.OK, "把它打死（点 0 号格）");

                var rem = Board(ctx, 0, 0);
                CheckTrue(rem != null && rem.IsRemnant, "★ 它翻面成残骸（留在格位上，不是进弃牌堆）");
                CheckTrue(rem != null && rem.Has("shield"),
                          "★ **常驻效果真的响了** —— 残骸拿到 Shield"
                          + "（卡面 `when a friendly troop becomes a Remnant it gains Shield`）");
            }
        }

        // ---------- ② `ASH52 Cosmic Serpent`：触发所有部队的灵魂石能力，**不付石** ----------
        {
            var cs = CreatePool.FindByName(pool, "Cosmic Serpent");
            var wb = CreatePool.FindByName(pool, "Wraithblade");         // `1 [Spirit Stone]: Gain Armour 2`
            CheckTrue(cs != null && wb != null, "卡池里有 `Cosmic Serpent` 与 `Wraithblade`");
            if (cs != null && wb != null)
            {
                var ctx = BattlePool(new[] { cs }, new[] { Unit("X", 1, 1, 5) }, pool,
                                     warlordFaction: "SaimHann");
                ToP1Turn(ctx, 1);
                ctx.Players[0].Energy = 12;
                ctx.Players[0].SpiritStones = 0;      // ★ 故意**一颗石头都不给**
                var w = Place(ctx, 0, 0, wb, exhausted: true);
                Check(w.Armor, 0, "（前提）Wraithblade 护甲 0");
                Check(w.Card.SpiritCost, 1, "（前提）它的灵魂石能力要 1 颗石头");

                CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "Cosmic Serpent"), -1),
                          RuleCodes.OK, "打出真卡 `Cosmic Serpent`");
                Check(w.Armor, 2,
                      "★ 它的灵魂石能力**被触发了**（护甲 0 → 2）—— 注意此时**没有石头**");
                Check(ctx.Players[0].SpiritStones, 0,
                      "★ **一颗石头都没花**（触发 ≠ 玩家主动激活；证据见 `BattleContext.SuppressCostDepth`）");
            }
        }

        // ---------- ③ `TL83 Norn Emissary`：`every time …` 也能收成事件型降费 ----------
        {
            var norn = CreatePool.FindByName(pool, "Norn Emissary");
            CheckTrue(norn != null, "卡池里有 `Norn Emissary`");
            if (norn != null)
            {
                Check(norn.CostWhens.Count, 1,
                      "★ `Lower cost by 2 **every time** a friendly unit triggers Synapse` 收下了"
                      + "（原来两处正则只认 `when` ⇒ 一条都收不到、卡面也不打 `*`）");
                if (norn.CostWhens.Count > 0)
                {
                    Check(norn.CostWhens[0].Delta, 2, "……降 2 费（存的是正数，登记时取负）");
                    var k = norn.CostWhens[0].Ev != null ? norn.CostWhens[0].Ev.Kind : null;
                    CheckTrue(k != null && k.Contains("synapse"),
                              $"……事件是「触发突触」那条（kind = {k}）");
                }
            }
        }

        // ---------- ④ 督军卡费用**全 0**（9 张 2026-09-16 逐张开 PnP 成品卡图核过）----------
        {
            var bad = new List<string>();
            int heroes = 0;
            foreach (var c in pool)
            {
                if (c.Type != "hero") continue;
                heroes++;
                if (c.Cost != 0) bad.Add(c.Name + "=" + c.Cost);
            }
            CheckTrue(heroes >= 56, $"卡池里督军卡有 {heroes} 张（≥56）");
            Check(bad.Count, 0,
                  "★ 督军卡**全部 0 费**（卡面是阵营徽记、没有费用六边形；"
                  + "这 9 张逐张开图核过，证据见 `gen_cards_engine.py` 的 `STAT_FIXES`）"
                  + (bad.Count > 0 ? " —— 非 0 的：" + string.Join(" / ", bad) : ""));
        }

        // ---------- ⑤ 「再触发一次」：Mob（真卡 `GOF_Big_Choppa_Nob`）----------
        {
            var nob = CreatePool.FindByName(pool, "Big Choppa Nob");
            var mobber = new CardDef("T_Mobber", "T_Mobber", "unit", "Mob: Gain +1 Attack",
                                     "common", "Test", 1, 2, 20, 0, new[] { "Mob" },
                                     subtype: "Infantry");
            CheckTrue(nob != null, "卡池里有 `Big Choppa Nob`");
            if (nob != null)
            {
                var ctx = BattlePool(new[] { mobber }, new[] { Unit("EFoe", 1, 0, 20) }, pool,
                                     warlordFaction: "Goff");
                ToP1Turn(ctx, 1);
                Place(ctx, 0, 0, nob, exhausted: true);           // 监听者：在场上才收得到广播
                var atk = Place(ctx, 0, 1, mobber);               // ⚠️ 攻击者**不能是 Exhausted**
                Place(ctx, 1, 5, Unit("EFoe2", 1, 0, 20), exhausted: true);
                Check(atk.Attack, 2, "（前提）它的近战是 2");

                CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 5, false), RuleCodes.OK,
                          "近战打一下（会触发 Mob）");
                Check(atk.Attack, 4,
                      "★ Mob **触发了两次**（2 → 4）—— 基础 1 次 + `it triggers an additional time` 1 次"
                      + "（额度由 `GOF_Big_Choppa_Nob` 在广播里挂上，`RuleCore.TakeExtraTrigger` 就地消费）");
                CheckTrue(atk.ExtraTriggers.Count == 0,
                          "★ ……而且额度**用掉就摘**（留着会让下一次攻击白捡一次）");
            }
        }

        // ---------- ⑥ 「效果生效两次」：Synapse（真卡 `TL30 Broodlord`）----------
        {
            var lord = CreatePool.FindByName(pool, "Broodlord");
            var heal = Tactic("T_SynHeal2", 0, "Heal 3 to a friendly unit");
            CheckTrue(lord != null, "卡池里有 `Broodlord`");
            if (lord != null)
            {
                var ctx = BattlePool(new[] { heal }, new[] { Unit("EFoe", 1, 0, 9) }, pool,
                                     warlordFaction: "Tyranid");
                ToP1Turn(ctx, 2);
                var mid = Place(ctx, 0, 2, lord, exhausted: true);
                var left = Place(ctx, 0, 1, Unit("FriendL3", 1, 0, 20), exhausted: true);
                // ⚠️ 锚点只能往**掉过血**的方向调：`Heal` 会把生命**抬到上限为止**
                //    （设成 10 而它的上限是 6 ⇒ 治好仍是 6，量不出「治了几次」）
                mid.Health = 2; left.Health = 4;
                CheckTrue(mid.Has(KeywordTable.Synapse),
                          "（前提）`Broodlord` 自己带突触（`this unit` 指的就是它）");

                CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_SynHeal2"), 2), RuleCodes.OK,
                          "对带突触的它用一张**友方**战术");
                Check(mid.Health, 5, "锚点自己被治一次（2 → 5；它的上限是 6，所以只能这么量）");
                Check(left.Health, 10,
                      "★ 相邻那个**吃了两次**（4 → 10 = 3 点 × 2）——"
                      + "`it applies the effect twice` 真的让突触的重复跑了两趟");
            }
        }

        // ---------- ⑦ 「本回合内死了就转给另一个」：真卡 `BL77 Spreading Corruption` ----------
        {
            var sc = CreatePool.FindByName(pool, "Spreading Corruption");
            var killFoe = Tactic("T_KillFoe", 0, "Deal 99 damage to an enemy troop");
            CheckTrue(sc != null, "卡池里有 `Spreading Corruption`");
            if (sc != null)
            {
                var ctx = BattlePool(new[] { sc, killFoe }, new[] { Unit("EFoe", 1, 0, 9) }, pool,
                                     warlordFaction: "Genestealers");
                ToP1Turn(ctx, 1);
                ctx.Players[0].Energy = 12;
                var victim = Place(ctx, 1, 5, Unit("EVictim", 1, 0, 5), exhausted: true);
                var other = Place(ctx, 1, 6, Unit("EOther", 1, 0, 5), exhausted: true);

                CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "Spreading Corruption"), 5),
                          RuleCodes.OK, "打出真卡 `Spreading Corruption`（点 5 号格）");
                CheckTrue(victim.Has("vulnerable"), "★ 目标拿到 `Vulnerable 4`");
                CheckTrue(!other.Has("vulnerable"), "（前提）另一个还没有");
                CheckTrue(ctx.DeathWatches.Count > 0,
                          "★ 监听登记上了（`If it dies this turn, …` —— 修之前这句是**静默不发生**）");

                CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_KillFoe"), 5),
                          RuleCodes.OK, "把它打死（同一个回合内）");
                CheckTrue(other.Has("vulnerable"),
                          "★ 效果**真的转给了另一个敌方部队**（`Vulnerable 4` 落到它身上）");
                Check(ctx.DeathWatches.Count, 0, "★ ……而且**只转一次**（监听用完就摘，不递归）");
            }
        }

        // ---------- ⑧ 「给手牌里的所有部队」：真卡 `TL53 Infinite Biomorphologies` ----------
        {
            var ib = CreatePool.FindByName(pool, "Infinite Biomorphologies");
            var troop = new CardDef("T_HandTroop", "T_HandTroop", "unit", "", "common", "Test",
                                    1, 2, 5, 0, null, subtype: "Infantry");
            CheckTrue(ib != null, "卡池里有 `Infinite Biomorphologies`");
            if (ib != null)
            {
                var ctx = BattlePool(new[] { ib, troop }, new[] { Unit("EFoe", 1, 0, 9) }, pool,
                                     warlordFaction: "Tyranid");
                ToP1Turn(ctx, 1);
                ctx.Players[0].Energy = 12;
                var onBoard = Place(ctx, 0, 0, Unit("OnBoard", 1, 3, 5), exhausted: true);
                int atk0 = onBoard.Attack, hp0 = onBoard.Health;

                CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "Infinite Biomorphologies"), -1),
                          RuleCodes.OK, "打出真卡 `Infinite Biomorphologies`");
                CheckTrue(ctx.HandBuffs.Count > 0,
                          "★ 额度记在**手牌那张部队卡**上（不是「己方全体」那条兜底路）");
                CheckTrue(onBoard.Attack == atk0 && onBoard.Health == hp0,
                          "★ 场上那个**一点没变** —— 修之前它会走 `(未写目标：己方全体)` 加到**场上**");

                int hi = HandIdx(ctx, 0, "T_HandTroop");
                // ⚠️ 手上**不止这一张部队卡**（牌库里的 `filler` 单位也会被抽上来）——
                //    额度是**每一张各记一份**，所以打出一张之后表里还剩别的，不能断言「清空」。
                int entries = ctx.HandBuffs.Count;
                int mine = 0;
                foreach (var h in ctx.HandBuffs)
                    if (h.Instance != null && h.Instance.Card != null && h.Instance.Card.Name == "T_HandTroop") mine++;
                CheckTrue(mine >= 1, "（前提）那张牌在手牌上有额度");
                CheckCode(RuleCore.PlayCard(ctx, 0, hi, 1), RuleCodes.OK, "把手牌里那张部队打出去");
                int mineAfter = 0;
                foreach (var h in ctx.HandBuffs)
                    if (h.Instance != null && h.Instance.Card != null && h.Instance.Card.Name == "T_HandTroop") mineAfter++;
                Check(mineAfter, 0, "★ 打出一份就兑现一份（那张牌的额度用完就摘）");
                bool said = false;
                foreach (string e in ctx.Events)
                    if (e != null && e.Contains("手牌加成")) { said = true; break; }
                CheckTrue(said, "★ 事件流里说清了这一份是**打出时兑现**的（不是静默加上的）");
            }
        }

        // ---------- ⑨ 「本回合抢过来」：真卡 `GSC_Telephatic_Domination` ----------
        {
            var td = CreatePool.FindByName(pool, "Telephatic Domination");
            CheckTrue(td != null, "卡池里有 `Telephatic Domination`");
            if (td != null)
            {
                var ctx = BattlePool(new[] { td }, new[] { Unit("EFoe", 1, 3, 5) }, pool,
                                     warlordFaction: "Genestealers");
                ToP1Turn(ctx, 1);
                ctx.Players[0].Energy = 12;
                var foe = Place(ctx, 1, 5, Unit("EVictim2", 1, 3, 5), exhausted: true);

                CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "Telephatic Domination"), 5),
                          RuleCodes.OK, "打出真卡（抢对面 5 号格那个）");
                CheckTrue(SlotOf(ctx, 0, "EVictim2") >= 0,
                          "★ 它**到了你的棋盘上**（归属 = 待在谁的 `Board[]` 里）");
                CheckTrue(SlotOf(ctx, 1, "EVictim2") < 0, "★ 对面那边没有了");
                CheckTrue(!foe.Exhausted,
                          "★ `and give it Fast` ⇒ **现在就能动**（`Exhausted` 被显式清掉 ——"
                          + "`AddKeyword(\"fast\")` 只写关键词、不会自己解锁）");

                // ⚠️ 「还回去之后置 Exhausted」那一条**不能在 `PassTurn` 之后量**：
                //    归还发生在 `EndTurn` 里，紧接着 `BeginTurn` 的 `RefreshForNewTurn` 会把它清掉。
                //    ⇒ 直接调 `EndTurn`、量那一刻（这才是归还真正生效的时点）。
                RuleCore.EndTurn(ctx);
                CheckTrue(SlotOf(ctx, 1, "EVictim2") >= 0,
                          "★ `EndTurn` 把它**归还**给原主（卡面 `this turn`）");
                CheckTrue(SlotOf(ctx, 0, "EVictim2") < 0, "……你这边也没留下");
                CheckTrue(foe.Exhausted,
                          "……还回去之后置「已行动」状态（我们挑的，见 `RuleCore.EndTurn` 那段注释）");
            }
        }

        // ---------- ⑩ `Kustom Job`：**终态** —— 查不到就是查不到，但**不许静默** ----------
        {
            var mek = CreatePool.FindByName(pool, "Mekaniak");
            var veh = new CardDef("T_Veh", "T_Veh", "unit", "", "common", "Test", 1, 2, 5, 0,
                                  null, subtype: "Vehicle");
            CheckTrue(mek != null, "卡池里有 `Mekaniak`");
            if (mek != null)
            {
                var ctx = BattlePool(new[] { mek }, new[] { Unit("EFoe", 1, 0, 9) }, pool,
                                     warlordFaction: "Goff");
                ToP1Turn(ctx, 1);
                ctx.Players[0].Energy = 12;
                var v = Place(ctx, 0, 0, veh, exhausted: true);
                int atk0 = v.Attack, hp0 = v.Health;

                CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "Mekaniak"), 0),
                          RuleCodes.OK, "打出真卡 `Mekaniak`（要选一个友方载具）");
                bool saidUnknown = false;
                foreach (string e in ctx.Events)
                    if (e != null && e.Contains("kustom job")) { saidUnknown = true; break; }
                // 🔴 **2026-09-16 改判**：这一条原来钉的是「**不静默**：事件流里明说载荷不认识
                //    ⇒ 三层零命中、**终态**、不猜一种效果顶上去」。现在**反过来** ——
                //    `Kustom Job` 的池子由**用户 2026-09-16 拍板**定为同组那三张
                //    （结构推断，见 `EffectResolver.ChooseEffectPools` 的注释），
                //    所以事件流里**不该**再出现「`a kustom job…` 不认识」。
                CheckTrue(!saidUnknown,
                          "★ `Mekaniak` **不再报「认不出」**（`Kustom Job` 的池子已按用户裁定登记）"
                          + " —— 原来这条钉的是「三层零命中 ⇒ 终态」");
            }
        }
    }

    /// <summary>
    /// **誓约能力**（卡面 `Oath N: 正文`）+ **三条修饰句** —— 🆕 2026-09-16。
    ///
    /// **为什么单开一节**：单位卡的 `Oath N:` 正文**从来没接进引擎**（`oath` 既不在
    /// `RoutableTriggers`、也不在 `BodyKeywords` ⇒ `TriggerOps("oath")` 恒为 null），
    /// 那 12 张 Ultramarines 单位的能力**既不报错、卡面也不打 `*`** —— 标准的静默失败
    /// （`CardDef.OathOps` 的注释里有完整来龙去脉）。三条「改规则」的句子
    /// （`UM84` / `UM89` / `UM_Vico_Therbeus`）则一直挂在「完全不认识」那一栏。
    ///
    /// 分四层钉（照 `TestSpiritStone` 的规矩：光「解析得出」不算数）：
    ///   ① 三条修饰句**认得出**，而且**只认那三种写法**
    ///   ② 正文**收得下**（`OathOps` / `OathCost`）
    ///   ③ **真的付费、真的生效**，且**不占单位那次行动**
    ///   ④ 三条规则各自真的改行为：多结算一遍 / 上限 3 / 跨回合可激活；付不起**不消耗次数**
    ///
    /// 语义与出处：`CardDef.OathDouble` 那一整段注释（`DefinedTrait` 0x4fc/0x4fd/0x4fe +
    /// `CardScript__CanUseOathAbility.c` / `CardScript__ResolveActiveAbilityPlayed.c`）。
    /// </summary>
    static void TestOathAbility()
    {
        var noKws = (string[])null;

        // ---------- ① 三条修饰句：认得出 ----------
        var cDouble = new CardDef("T_OD", "T_OD", "unit",
            "Friendly [Oath] abilities apply an additional time.",
            "common", "Test", 1, 2, 2, 0, noKws, subtype: "Infantry");
        CheckTrue(cDouble.OathDouble,
                  "`Friendly [Oath] abilities apply an additional time` → `OathDouble`（卡面印的是图标 ⇒ 带方括号）");
        var cTriple = new CardDef("T_OT", "T_OT", "unit",
            "Oath abilities of friendly troops can be activated up to 3 times each turn. Oath 1: Deal 1 damage",
            "common", "Test", 1, 2, 2, 0, noKws, subtype: "Infantry");
        CheckTrue(cTriple.OathTripleActivation, "……`up to 3 times each turn` → `OathTripleActivation`");
        var cAllTurns = new CardDef("T_OA", "T_OA", "unit",
            "Oath abilities of friendly troops may be activated on later turns. Oath 1: Gain Camouflage",
            "common", "Test", 1, 2, 2, 0, noKws, subtype: "Infantry");
        CheckTrue(cAllTurns.OathInAllTurns, "……`may be activated on later turns` → `OathInAllTurns`");
        CheckTrue(!cTriple.OathDouble && !cAllTurns.OathDouble,
                  "反例：三种开关**各归各的**（没有互相串）");
        var cBare = new CardDef("T_OB", "T_OB", "unit", "Oath abilities are great",
                                "common", "Test", 1, 2, 2, 0, noKws, subtype: "Infantry");
        CheckTrue(!cBare.OathDouble && !cBare.OathTripleActivation && !cBare.OathInAllTurns,
                  "反例：只提到 `Oath` 的句子**一个开关都不给**（不猜）");

        // ---------- ② 正文收得下 ----------
        var oath1 = new CardDef("T_O1", "T_O1", "unit", "Oath 1: Gain Armour 1",
                                "common", "Test", 1, 2, 3, 0, noKws, subtype: "Infantry");
        Check(oath1.OathOps.Count, 1, "`Oath 1: Gain Armour 1` 的正文收下 1 条");
        Check(oath1.OathCost, 1, "……代价 = 1（`Oath N:` 里的 N）");
        var oath3 = new CardDef("T_O3", "T_O3", "unit", "Oath 3: Gain +1 Ranged and Flank",
                                "common", "Test", 1, 2, 3, 0, noKws, subtype: "Infantry");
        Check(oath3.OathCost, 3, "……N 大的卡也对（`Oath 3` → 3）");

        // ---------- ③ 真的付费、真的生效、不占行动 ----------
        {
            var ctx = Battle(new[] { oath1 }, new[] { Unit("X", 1, 1, 5) });
            ToP1Turn(ctx, 1);
            ctx.Players[0].Energy = 5;
            // ⚠️ `exhausted: true` —— 誓约**不占单位那次行动**：一个已经行动过的单位也照样能激活
            var u = Place(ctx, 0, 0, oath1, exhausted: true);
            Check(u.Armor, 0, "打之前：护甲 0（前提）");

            CheckCode(RuleCore.UseOathAbility(ctx, 0, 0), RuleCodes.OK, "激活 `Oath 1:` 能力");
            Check(ctx.Players[0].Energy, 4, "★ **真的付了 1 费**（5 → 4）");
            Check(u.Armor, 1, "★ **真的生效**（护甲 0 → 1）");
            CheckTrue(u.Exhausted, "……而且**没有**把它翻成「未行动」（誓约不吃那次行动）");
            Check(u.OathUsesThisTurn, 1, "……激活次数记了 1");

            // ---------- ④ 每回合一次 ----------
            CheckCode(RuleCore.UseOathAbility(ctx, 0, 0), RuleCodes.ErrExhausted,
                      "同一回合再激活一次 → **拒绝**（默认上限 1）");
            Check(ctx.Players[0].Energy, 4, "……被拒时**没有偷偷扣费**");

            // ---------- ⑤ 付不起 ⇒ 不生效、且不消耗次数 ----------
            ctx.Players[0].Energy = 0;
            u.OathUsesThisTurn = 0;                     // 手工放行次数闸，专量付费那一层
            CheckCode(RuleCore.UseOathAbility(ctx, 0, 0), RuleCodes.ErrUnimplemented,
                      "能量为 0 时激活 → **不生效**（如实报 `ErrUnimplemented`，不是静默成功）");
            Check(u.OathUsesThisTurn, 0, "……★ 而且**不消耗这次激活**（没钱 ≠ 用掉了）");
            Check(u.Armor, 1, "……护甲没变（正文没跑）");
        }

        // ---------- ⑥ `oathDouble`：**多结算一遍**（次数 = 同方场上带该开关的牌数）----------
        {
            var ctx = Battle(new[] { oath1, cDouble }, new[] { Unit("X", 1, 1, 5) });
            ToP1Turn(ctx, 1);
            ctx.Players[0].Energy = 5;
            var u = Place(ctx, 0, 0, oath1, exhausted: true);
            Place(ctx, 0, 1, cDouble, exhausted: true);   // 旁观的那张 = 开关的来源
            Check(RuleCore.OathExtraReplays(ctx, 0), 1, "同方场上带 `oathDouble` 的牌数 = 1");

            CheckCode(RuleCore.UseOathAbility(ctx, 0, 0), RuleCodes.OK, "有 `oathDouble` 时激活");
            Check(u.Armor, 2, "★ **正文跑了 2 遍**（护甲 0 → 2：基础 1 次 + `oathDouble` 再 1 次）");
            Check(ctx.Players[0].Energy, 4, "……**只付了一费**（多跑的是结算，不是再买一次）");
        }

        // ---------- ⑦ `oathTripleActivation`：上限 1 → 3 ----------
        {
            var ctx = Battle(new[] { oath1, cTriple }, new[] { Unit("X", 1, 1, 5) });
            ToP1Turn(ctx, 1);
            ctx.Players[0].Energy = 9;
            var u = Place(ctx, 0, 0, oath1, exhausted: true);
            Place(ctx, 0, 1, cTriple, exhausted: true);
            Check(RuleCore.OathActivationCap(ctx, 0), 3, "同方有 `oathTripleActivation` ⇒ 上限 3");

            CheckCode(RuleCore.UseOathAbility(ctx, 0, 0), RuleCodes.OK, "第 1 次激活");
            CheckCode(RuleCore.UseOathAbility(ctx, 0, 0), RuleCodes.OK, "第 2 次激活");
            CheckCode(RuleCore.UseOathAbility(ctx, 0, 0), RuleCodes.OK, "第 3 次激活");
            Check(u.Armor, 3, "……三次都生效（护甲 0 → 3）");
            CheckCode(RuleCore.UseOathAbility(ctx, 0, 0), RuleCodes.ErrExhausted, "第 4 次 → 拒绝");
        }

        // ---------- ⑧ `oathInAllTurns`：下一回合还能激活 ----------
        {
            // 先在**没有**开关的对局里量出「下一回合会被拒」这个基线
            var ctxA = Battle(new[] { oath1 }, new[] { Unit("X", 1, 1, 5) });
            ToP1Turn(ctxA, 1);
            ctxA.Players[0].Energy = 9;
            var ua = Place(ctxA, 0, 0, oath1, exhausted: true);
            ToP1Turn(ctxA, 2);                          // 过一回合
            CheckTrue(ua.DeployedTurn != ctxA.Turn, "（前提）它不再「本回合部署」");
            CheckCode(RuleCore.UseOathAbility(ctxA, 0, 0), RuleCodes.ErrExhausted,
                      "基线：**没有** `oathInAllTurns` ⇒ 过了部署回合就激活不了");

            var ctxB = Battle(new[] { oath1, cAllTurns }, new[] { Unit("X", 1, 1, 5) });
            ToP1Turn(ctxB, 1);
            ctxB.Players[0].Energy = 9;
            var ub = Place(ctxB, 0, 0, oath1, exhausted: true);
            Place(ctxB, 0, 1, cAllTurns, exhausted: true);
            ToP1Turn(ctxB, 2);
            CheckCode(RuleCore.UseOathAbility(ctxB, 0, 0), RuleCodes.OK,
                      "★ 有 `oathInAllTurns` ⇒ **后续回合照样能激活**");
            Check(ub.Armor, 1, "……而且生效了");
        }

        // ---------- ⑨ 战术卡的 `Oath N:` 那条路**没有被动过** ----------
        {
            // 单位卡这一轮接的是**新路**（`OathOps`）；战术卡的 `Oath N:` 早就能打
            // （付费前缀走 `PlayTactic` 那一支）—— 这里钉一下，别哪天被这条改动带坏了。
            var tac = Tactic("T_OathTac", 2, "Oath 1: Deal 2 damage to an enemy troop");
            var ctx = Battle(new[] { tac }, new[] { Unit("X", 1, 1, 9) });
            ToP1Turn(ctx, 1);
            ctx.Players[0].Energy = 9;
            // ⚠️ 这张战术**要选目标**（`Deal 2 damage to an enemy troop`）⇒ 棋盘上得有敌方单位，
            //    而且 `PlayTactic` 的第三个参数要**指名槽号**（传 -1 会返回 `ErrTarget`）。
            var foe = Place(ctx, 1, 5, Unit("EFoeOath", 1, 1, 9), exhausted: true);
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_OathTac"), 5), RuleCodes.OK,
                      "战术卡的 `Oath N:` 照旧打得出去");
            Check(foe.Health, 7, "……而且誓约那半句**真的结算了**（9 → 7）");
            Check(ctx.Players[0].Energy, 6, "……照旧付的是 (2 卡费 + 1 誓约费)");
        }
    }

    /// <summary>
    /// **灵魂石能力**（卡面 `N [Spirit Stone]: …`）—— 灵族的**阵营货币**，2026-09-14。
    ///
    /// **为什么单开一节**：这一族原来是**没有任何触发点**的 —— 句子「解析得出来、载荷也有机制」，
    /// 却**永远不会发生**，而且**报表看不见它们**（判据是「解析得出 + 有机制 + **没有触发点**」，
    /// 见 `资料/单位卡desc与光环_批次划分.md` §一⑦）。和 A7 的光环是同一个坑。
    /// ⇒ 按本工程的老规矩分三层钉：① 代价判据读得对 ② **真的付了石** ③ **真的生效 / 监听器真的响**。
    /// 前两层全绿而第三层没接，这个工程有过好几次先例。
    ///
    /// **语义与出处**：`CardDef.SpiritOps` 与 `EffectResolver.ResolveSpiritAbility` 的注释。
    /// ⚠️ **别和 `useWaystone`（`BattleActionType = 76`）混了** —— 那个是「**收集**」石头
    /// （点场上已翻面的灵族残骸），**本版没做**，见 `资料/查证_useWaystone_语义.md`。
    /// 逐卡清单：`资料/灵魂石卡_逐张核.md`。
    /// </summary>
    static void TestSpiritStone()
    {
        var pool = CardDatabase.Load();
        var byName = new Dictionary<string, CardDef>();
        foreach (var c in pool) byName[c.Name] = c;

        // ---- ① 数据侧：`N [Spirit Stone]: …` 的 N 必须被读成**代价**，货币必须是 **spirit** ----
        //   🔴 这条同时钉住「前缀写法没写错」：写成 `1 [spirit]:`（光秃的 `spirit` **不在**
        //      `RePaid` 的货币词表里）或裸 `(1)` 时，前缀会**整条失配** —— 轻则判「不认」，
        //      重则被后一条正则吃成**载荷**（`2 [spirit]: Repeat this effect` → `载荷「2 spirit:」`，
        //      而且还报「认了」）。那种情况下这条断言会在付出代价那一行红。
        {
            // 逐张钉（全池实测 **28 张**，逐卡证据见 `资料/灵魂石卡_逐张核.md`）
            var want = new Dictionary<string, int> {
                { "Storm of Silence", 1 }, { "Witchfire", 1 }, { "Wrath of Khaine", 2 },
                { "Shining Spear", 1 }, { "Hornet", 3 }, { "Warlock", 1 },
                { "Swooping Hawk", 1 }, { "Wraithblade", 1 }, { "Wraithguard", 1 },
                { "Spiritseer", 2 }, { "Vengeful Wraithblade", 2 }, { "Hemlock Wraithfighter", 3 },
                { "Wraithlord", 3 }, { "Avatar of Khaine", 2 }, { "Wraithknight", 3 },
                { "Death Spinner Warp Spider", 1 }, { "Farseer", 1 }, { "Farseer Skyrunner", 2 },
                { "Autarch", 1 }, { "Ahnakh-Yth Shrine", 1 }, { "Will of Asuryan", 1 },
                { "Webway Gate", 1 }, { "Cosmic Serpent", 2 }, { "Devoted of Khaine", 1 },
                { "Eldritch Storm", 2 }, { "Reclaim the Stars", 5 }, { "Wailing Doom", 2 },
                { "Forewarned", 1 },
            };
            int n = 0;
            foreach (var kv in want)
            {
                CardDef c;
                if (!byName.TryGetValue(kv.Key, out c))
                {
                    CheckTrue(false, "卡池里找不到「" + kv.Key + "」（数据被改名/删掉了？）");
                    continue;
                }
                Check(c.SpiritCost, kv.Value, "★「" + kv.Key + "」的灵魂石代价 = " + kv.Value);
                if (c.SpiritOps.Count > 0)
                    Check(EffectText.CostKindOf(c.SpiritOps[0].CostKind), "spirit",
                          "★「" + kv.Key + "」的货币判成 **spirit**（不是能量 —— 判错会静默多扣能量）");
                n++;
            }
            Check(n, 28, "★ 全池 **28 张**灵魂石卡（清单：资料/灵魂石卡_逐张核.md）");

            // 反例：`Path of the Seer` 的 `… Draw a card. Gain 1 Spirit Stone` 是**获得**、
            //   不是费用前缀；它被当成费用的话这张卡会**反过来扣掉**一颗石（静默）。
            CardDef seer;
            if (byName.TryGetValue("Path of the Seer", out seer))
                Check(seer.SpiritCost, 0,
                      "★ 反例：`Path of the Seer` 的 `Gain 1 Spirit Stone` 是**获得**、SpiritCost 必须是 0");
        }

        // ---- ② 结算侧：**部署那一刻**真的付石、真的生效 ----
        {
            var ctx = Battle(new[] { PoolCard(pool, "Wraithblade") }, new CardDef[0]);
            ToP1Turn(ctx, 3);
            ctx.Players[0].SpiritStones = 1;
            int idx = HandIdx(ctx, 0, "Wraithblade");
            CheckTrue(idx >= 0, "前提：`Wraithblade` 在手里");
            CheckCode(RuleCore.PlayCard(ctx, 0, idx, 2), RuleCodes.OK, "打出 `Wraithblade`（`1 [Spirit Stone]: Gain Armour 2`）");
            Check(ctx.Players[0].SpiritStones, 0,
                  "★ **灵魂石真的扣了 1 颗**（没扣 = 这句话根本没被消费）");
            Check(ctx.Players[0].Board[2].Armor, 2, "★ **`Gain Armour 2` 真的生效了**（付石之后才加）");
        }

        // ---- ②-bis **付不起就不生效，而且要如实报**（不许「假装加上了」）----
        {
            var ctx = Battle(new[] { PoolCard(pool, "Wraithblade") }, new CardDef[0]);
            ToP1Turn(ctx, 3);
            ctx.Players[0].SpiritStones = 0;
            int idx = HandIdx(ctx, 0, "Wraithblade");
            CheckCode(RuleCore.PlayCard(ctx, 0, idx, 2), RuleCodes.OK, "打出 `Wraithblade`（但**一颗石都没有**）");
            Check(ctx.Players[0].SpiritStones, 0, "★ 没石可扣，余额仍是 0（没被扣成负数）");
            Check(ctx.Players[0].Board[2].Armor, 0,
                  "★ **付不起 ⇒ 不加护甲** —— 白给的话这张卡的代价形同虚设");
        }

        // ---- ③ **监听器真的响**：`Bright Lance Vyper` = `When you trigger a Spirit Stone ability, …` ----
        //   🔴 这条是这族最要紧的一条断言：卡池里**唯一**一个「花石」的监听者。
        //      广播点在全仓**只有一处**（`EffectResolver` 付费段 `kind == "spirit"` 那一行）。
        //      做 A3 时它「点亮了但不会响」（池子里 0 张卡带前缀）—— 现在必须真的响。
        {
            var ctx = Battle(new[] { PoolCard(pool, "Wraithblade") }, new CardDef[0]);
            ToP1Turn(ctx, 3);
            var vyper = Place(ctx, 0, 2, PoolCard(pool, "Bright Lance Vyper"));
            int r0 = vyper.RangedAttack;
            ctx.Players[0].SpiritStones = 1;
            int idx = HandIdx(ctx, 0, "Wraithblade");
            CheckCode(RuleCore.PlayCard(ctx, 0, idx, 6), RuleCodes.OK, "打出 `Wraithblade`（触发一次灵魂石能力）");
            CheckTrue(vyper.Has("sniper"),
                      "★ **监听器真的响**：`Bright Lance Vyper` 拿到 `Sniper`"
                      + "（不响 = 广播点没接上，或那句 `When …` 的正文没被消费）");
            Check(vyper.RangedAttack, r0 + 1, "★ 并且 +1 远程（`gain Sniper and +1 Ranged Attack` 的两半都要落）");
        }
    }

    /// <summary>
    /// **玩家真的能选**（选牌 / 三选一 / 选效果）—— 2026-09-14。
    ///
    /// **为什么单开一节**：这三个点原来**全是 `ctx.Rng` 等概率自动挑**（`DoChooseCard` 的注释
    /// 自己写着「表现层的选牌 UI 是另一件」）。全池 **62 张**卡会走到那里 —— 普查见
    /// `资料/选牌_受影响卡普查.md`；表现层那个面板见 `CardPresentation/Battle/ChoosePanel.cs`。
    ///
    /// 这一节钉三层，缺一不可：
    ///   ① 判据列得出「本该问玩家」的那一处（`PlayerChooseOps`）；
    ///   ② 🔴 **面板给的答案真的决定了选哪张**（不是又被 `ctx.Rng` 盖过去）；
    ///   ③ 面板**没给**答案时照旧自己挑（不卡住），且 `ChooseSites &gt; ChooseAnswered` 能暴露出来。
    ///
    /// ⚠️ ② 的判据**绕开了「看日志」那条脆路** —— 直接读引擎自己写下的 `ctx.LastChosenCard`。
    /// </summary>
    static void TestPlayerChoice()
    {
        var pool = CardDatabase.Load();

        // ---- ① 判据：列得出 ask 点，且不该有的卡列不出 ----
        {
            var asks = RuleCore.PlayerChooseOps(PoolCard(pool, "Rapid Deployment"));
            Check(asks.Count, 1, "★ `Rapid Deployment`（`Choose a troop in your hand …`）有 **1** 个 ask 点");
            if (asks.Count > 0) Check(asks[0].Verb, "choosecard", "★ 它是 `choosecard`");
            Check(RuleCore.PlayerChooseOps(PoolCard(pool, "Wraithblade")).Count, 0,
                  "★ 不选牌的卡 ask 点 = **0**（面板不会为它弹出来）");
        }

        // ---- ② 🔴 面板给的答案真的决定了选哪张 ----
        //   判据用 `ctx.LastChosenCard`（`DoChooseCard` 自己写下的那个**对象**）。
        //   ⚠️ **必须「两次不同下标 → 不同卡」**：同种子、同局面下，若引擎忽略下标、
        //      仍然走 `ctx.Rng`，两次会选中**同一张** —— 这条断言就是钉这个。
        {
            int n0, a0, n1, a1; string w0, w1;
            var c0 = RunChoosePick(pool, 0, out n0, out a0, out w0);
            var c1 = RunChoosePick(pool, int.MaxValue, out n1, out a1, out w1);   // 最后一个候选
            CheckTrue(n0 >= 2 && n0 == n1, "前提：两次跑的候选数一样（同种子同局面）—— " + w0 + " / " + w1);
            CheckTrue(c0 != null && c1 != null, "前提：两次都真的选出了牌");
            Check(a0, 1, "★ 面板的答案**被消费了**（`ChooseAnswered` = 1）");
            if (c0 != null && c1 != null)
                CheckTrue(!ReferenceEquals(c0, c1),
                          "★ **不同下标选出了不同的卡** —— 引擎真的听了面板的"
                          + "（忽略下标、仍用 `ctx.Rng` 的话两次会是**同一张**）");
        }

        // ---- ③ 面板没给答案 ⇒ 照旧自己挑（不卡住），而且**能看出有几处没问** ----
        {
            int n, a; string w;
            var c = RunChoosePick(pool, -1, out n, out a, out w);
            CheckTrue(c != null, "★ 没填队列时引擎照旧自己挑（**不会卡住、不会漏结算**）—— " + w);
            Check(a, 0, "★ 而且 `ChooseAnswered` = 0 ⇒ `ChooseSites > ChooseAnswered` 能把「没问」暴露出来");
        }

        // ---- ④ 🆕 2026-09-14：**另外三族也列得出 ask 点** ----
        //   `chooseone`（7 张）· `chooseeffect`（3 张）· `become`（`Hrolf the Ironhowl`，1 张）。
        //   引擎的 `TakePick` 一直支持它们（有面板答案就用），**缺的是「表现层有没有问」**
        //   —— 判据就是从这儿开始的（`BattleDriver.ShowAsk` 读的就是它）。
        {
            var one = RuleCore.PlayerChooseOps(PoolCard(pool, "The Fang"));
            Check(one.Count, 1, "★ `The Fang`（`Choose one: …`）有 1 个 ask 点");
            if (one.Count > 0)
            {
                Check(one[0].Verb, "chooseone", "★ 它是 `chooseone`");
                var opts = RuleCore.ChooseOneOptions(one[0]);
                Check(opts.Length, 3, "★ 解出 **3** 个选项（`A; B or C`）");
                Check(opts[0], "Deploy a Grey Hunter",
                      "★ 选项文字保留**卡面原文大小写**（不是 `deploy a grey hunter`）"
                      + " —— 面板要把选项**画成一张卡**，卡面上印的就是它");
            }

            var ef = RuleCore.PlayerChooseOps(PoolCard(pool, "Exemplary Warrior"));
            Check(ef.Count, 1, "★ `Exemplary Warrior`（`… chooses an effect`）有 1 个 ask 点");
            if (ef.Count > 0) Check(ef[0].Verb, "chooseeffect", "★ 它是 `chooseeffect`");

            // 🔴 **2026-09-14 用户裁决**：`Hrolf the Ironhowl` 的 `… become a Hunting Wolf or
            //   Fenrisian Wolf` 卡面**没有 `choose` 字样** ⇒ **引擎随机挑、不开面板**。
            //   （规则书英文版 `:475`：**只有写了 `choose` 才轮到玩家**。）
            //   ⇒ 这条从「有 1 个 ask 点」**翻面**成「**0 个**」。
            var bc = RuleCore.PlayerChooseOps(PoolCard(pool, "Hrolf the Ironhowl"));
            Check(bc.Count, 0,
                  "★ `Hrolf the Ironhowl`（`… become A or B`）**不再有 ask 点** —— 卡面没有 `choose`，"
                  + "按用户口径该**随机**、不该问玩家（原来这里写的是「有 1 个 ask 点」，已推翻）");
            // 但那条 `become` op **还在**（机制照跑，只是挑法改成随机）
            var hrolfOps = EffectText.Parse(PoolCard(pool, "Hrolf the Ironhowl").Desc, out _, out _);
            var becomeOp = hrolfOps.Find(o => o.Verb == "become");
            CheckTrue(becomeOp != null, "★ `become` 这条 op **还在**（改的是「谁挑」，不是「做不做」）");
            if (becomeOp != null)
                Check(becomeOp.Amount, 2, "★ 两个变身候选（Hunting Wolf / Fenrisian Wolf）");

            // 反例：`become` 的**别的用法**不许被当成 ask 点
            //   （`Humanity's Shield` 的 `Your Warlord becomes Invulnerable` —— 单名，不是二选一）
            Check(RuleCore.PlayerChooseOps(PoolCard(pool, "Humanity's Shield")).Count, 0,
                  "★ 反例：`becomes Invulnerable`（不是 `A or B`）**不产生 ask 点**");
        }

        // ---- ⑤ 🔴 三选一的答案真的决定了选哪一项（**看局面，不看日志**）----
        //   `The Fang` 三项 = `Deploy a Grey Hunter` / `Heal 4 to a friendly unit` / `Draw 2 cards`
        //   ⇒ 选第 1 项场上多一个单位、选第 3 项手上多两张。
        //   ⚠️ 引擎若忽略队列、仍旧走 `ctx.Rng`，两种答案会得到**同一个局面** —— 这条就是钉这个。
        {
            int b0, h0, b2, h2, ans0, ans2; bool ok0, ok2; string d0, d2;
            ok0 = RunChooseOne(pool, "The Fang", 0, out b0, out h0, out ans0, out d0);
            ok2 = RunChooseOne(pool, "The Fang", 2, out b2, out h2, out ans2, out d2);
            CheckTrue(ok0 && ok2, "前提：两次都打出去了 —— " + d0 + " / " + d2);
            Check(ans0, 1, "★ 第 1 次：面板的答案**被消费了**（`ChooseAnswered` = 1）");
            CheckTrue(b0 > b2, "★ 选第 1 项（`Deploy a Grey Hunter`）**场上多一个单位** —— " + d0 + " / " + d2);
            CheckTrue(h2 > h0, "★ 选第 3 项（`Draw 2 cards`）**手上多两张** —— " + d0 + " / " + d2);
        }

        // ---- ⑥ 🔴 变身：**引擎随机挑，不问玩家**（用户 2026-09-14 裁决）----
        //   钉三件事：① 每个种子都变成候选之一、**全手牌统一**（不是每张各掷一次）；
        //             ② **不同种子真的会变出不同的狼**（固定一只 = 根本没在随机）；
        //             ③ **面板队列完全没被碰过**（`ChooseSites` = 0 ⇒ 没问玩家）。
        {
            var seen = new HashSet<string>();
            bool allFine = true, anyAsked = false;
            string detail = "";
            for (int seed = 0; seed < 8; seed++)
            {
                int asked = 0;
                string n = RunBecome(pool, seed, out asked, out string d);
                if (n != "Hunting Wolf" && n != "Fenrisian Wolf") { allFine = false; detail += d + " ‖ "; }
                else seen.Add(n);
                if (asked != 0) anyAsked = true;
            }
            CheckTrue(allFine,
                      "★ 8 个种子都变出了候选之一、且**全手牌统一变成同一只** —— " + detail);
            CheckTrue(seen.Count == 2,
                      $"★ **不同种子变出不同的狼**（8 个种子里出现过 {seen.Count} 种）—— "
                      + "固定一只就说明它根本没在随机");
            CheckTrue(!anyAsked,
                      "★ **一次都没问玩家**（`ChooseSites` 恒为 0）—— 卡面没有 `choose` 字样");
        }
    }

    /// <summary>
    /// 跑一次 `Rapid Deployment`，返回**引擎实际选中的那张卡**（没选成返回 null）。
    /// `pick &lt; 0` = **不填队列**（模拟「面板没问」那条路）。
    /// 判据 `ctx.LastChosenCard` 是 `DoChooseCard` 自己写的，**不是我们从日志里猜的**。
    /// </summary>
    static CardDef RunChoosePick(IList<CardDef> pool, int pick, out int candCount, out int answered,
                               out string diag)
    {
        candCount = 0; answered = 0; diag = "?";
        // 最小夹具（和 `ProbeBattle` 同形，只是**带上真卡池**）：
        //   ⚠️ 手牌里必须**有别的卡**当候选 —— `Rapid Deployment` 自己会在结算前离手，
        //      只放它一张的话候选恒为 0。
        // ⚠️ **顺序**按 `DeckOf` 的约定：督军最前、其余**倒序**（抽牌是 `pop_back`，最后入列的先抽到）
        //    ⇒ 要用的那张牌必须放**最后**，否则抽不到手里（实测踩到：手牌 3 张里没有它）。
        var d0 = new List<CardDef> {
            HeroOf("FixtureWarlord", "SaimHann", 2, 30),
            Unit("PickFodder1", 1, 0, 1), Unit("PickFodder2", 1, 0, 1), Unit("PickFodder3", 1, 0, 1),
            PoolCard(pool, "Rapid Deployment"),
        };
        var d1 = new List<CardDef> { HeroOf("FixtureWarlord", "Goff", 2, 30), Unit("PickFoe", 1, 0, 9) };
        var ctx = RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: pool);
        ToP1Turn(ctx, 3);
        int idx = HandIdx(ctx, 0, "Rapid Deployment");
        if (idx < 0)
        {
            var names = new List<string>();
            foreach (var c in ctx.Players[0].Hand) names.Add(c.Card.Name);
            diag = "牌不在手里（手牌 " + names.Count + " 张：" + string.Join("/", names.ToArray()) + "）";
            return null;
        }
        var asks = RuleCore.PlayerChooseOps(ctx.Players[0].Hand[idx].Card);
        if (asks.Count == 0) { diag = "没有 ask 点"; return null; }

        // 面板在**弹出来的那一刻**现取候选（和驱动层 `ShowAsk` 同一条路）
        string srcName, detail, why;
        var cands = RuleCore.ChooseCardCandidates(ctx, 0, asks[0], out srcName, out detail, out why);
        diag = $"候选 {cands.Count}（源={srcName}，why={why ?? "无"}）";
        if (why != null || cands == null || cands.Count == 0) return null;
        candCount = cands.Count;

        ctx.ResetChoices();
        if (pick >= 0) ctx.ChoosePicks.Enqueue(Mathf.Min(pick, candCount - 1));
        int code = RuleCore.PlayTactic(ctx, 0, idx, -1);
        if (code != RuleCodes.OK) { diag += $"，打出被拒（{RuleCodes.Describe(code)}）"; return null; }
        answered = ctx.ChooseAnswered;
        diag += "，打出成功";
        return ctx.LastChosenCard != null ? ctx.LastChosenCard.Card : null;   // 第 7 行第 3 步：槽里是实例
    }

    /// <summary>
    /// **A6 族 A / B / C**（2026-09-14）—— 这一批全是「解析得出、有机制、却**永远不会发生**」
    /// 或者「**打错人**」，报表上分别写成 ③ 栏和②栏。逐句探针的原始输出在 `_tmp_view/probe_out.txt`。
    ///
    /// | 族 | 是什么 | 原来怎么坏 |
    /// |---|---|---|
    /// | A | 载荷里嵌的**一整段效果文字**（`Give … "Strike: Draw a card"`）**10 张** | 运行时读的是**封闭文法** `EffectSpec`（只认 `Damage/Heal/Draw`）⇒ 一条都放不出来 |
    /// | — | 引号里的 ` and ` / ` to ` | `SplitAndTail` / `ReGive` **不看引号** ⇒ 载荷被切在引号中间、目标变成一段垃圾（**静默错打**） |
    /// | B | `EffectCondition` 认不出的 **6 条条件** | 一律返回「判不了」⇒ 整条效果不结算 |
    /// | C | 载荷词表缺的词（`blind` / `(1)`） | `blind` 原版 `GIVE_KW` 里就没有；`(1)` 是**图标被 OCR 丢掉**，数据侧修 |
    /// </summary>
    static void TestA6Batch1()
    {
        var pool = CardDatabase.Load();

        // ---- ① 族 A 判据：嵌入正文走**真解析器**（和运行时同源）----
        foreach (var payload in new[] {
            "\"slay: heal 2 and gain a dark pact of excess\"",
            "\"strike: lower the cost of a random troop in hand by 1\"",
            "\"💀 backlash: return to your hand\"",
            "flank and 'strike: draw a card'",
            "\"penitence: deal 1 damage to two random enemy troops\"",
            "\"blast 2 and slay: deploy a black legionary\"",
        })
        {
            string why;
            CheckTrue(GivePayload.Mechanized(payload, out why),
                      "★ 载荷「" + payload + "」**有机制**了（原来走 `EffectSpec` 封闭文法一律判没有）"
                      + " —— " + (why ?? "无"));
        }
        {
            string why;
            CheckTrue(!GivePayload.Mechanized("\"slay: 这一段是中文\"", out why),
                      "★ 反例：**真的**解析不出来的嵌入正文仍判「没有机制」，不许一律放行 —— " + why);
        }

        // ---- ② 族 A：引号里那一整段是**原子**（里面没有语法成分）----
        {
            var ops = EffectText.Parse("Give \"💀 Backlash: Return to your hand\" to a friendly troop",
                                       out _, out _);
            Check(ops.Count, 1, "★ `Graceful Avoidance` 只有 **1** 条 op（没在引号里的 ` to ` 处切开）");
            if (ops.Count == 1)
            {
                Check(ops[0].Payload, "\"💀 backlash: return to your hand\"",
                      "★ 载荷 = **整段**（引号包着的那一整段）");
                CheckTrue(ops[0].Target != null && ops[0].Target.Kind == "troop"
                          && ops[0].Target.Side == "own",
                          "★ 目标 = **一个友方部队**（原来被切成 `your hand\" to a friendly troop`）");
            }

            var ops2 = EffectText.Parse(
                "Give \"Slay: Heal 2 and gain a Dark Pact of Excess\" to a friendly troop", out _, out _);
            Check(ops2.Count, 1,
                  "★ `Pledge to the Dark Prince` 只有 1 条 op（没在引号里的 ` and gain ` 处切尾句）");
            if (ops2.Count == 1)
                CheckTrue(ops2[0].Target != null && ops2[0].Target.Kind == "troop",
                          "★ ……而且目标是**一个友方部队**（原来退化成「未写目标 ⇒ 己方全体」）");

            var ops3 = EffectText.Parse("Give to a friendly troop Flank and 'Strike: Draw a card'",
                                        out _, out _);
            Check(ops3.Count, 1, "★ `Enhanced Aggression` 1 条 op");
            if (ops3.Count == 1)
                CheckTrue(ops3[0].Target != null && ops3[0].Target.Kind == "troop",
                          "★ ② 语序的目标解出来了（原来窄写法挑最左的 ` to ` ⇒ 目标只剩 `a`、整个丢掉）");
        }

        // ---- ③ 族 A 端到端：真打一局，挂上去的正文**真的在单位身上** ----
        {
            var ga = CardDatabase.Find(pool, "Graceful Avoidance");
            CheckTrue(ga != null, "卡池里有 `Graceful Avoidance`");
            if (ga != null)
            {
                var ctx = BattlePool(new[] { ga }, new[] { Unit("X", 1, 1, 5) }, pool,
                                     warlordFaction: "SaimHann");
                ToP1Turn(ctx, 1);
                ctx.Players[0].Energy = 12;
                ctx.ResetChoices();
                var u = Place(ctx, 0, 0, Unit("MyTroop", 1, 2, 9));
                CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "Graceful Avoidance"), 0),
                          RuleCodes.OK,
                          "打出 `Graceful Avoidance`（**要点一个友方部队** —— 目标格必须传，不能给 -1）");
                CheckTrue(u.Has("backlash"), "★ 目标单位拿到了 `backlash` 关键词");
                CheckTrue(u.FxOps("backlash") != null && u.FxOps("backlash").Count > 0,
                          "★ ……而且**挂上去的正文真的在它身上**（`FxOps` 非空）——"
                          + " 原来这里恒为 null（封闭文法解不了 `return to your hand`），"
                          + "表现是「关键词给了、效果永远不发生」");
                CheckTrue(u.HasFx("backlash"), "★ `HasFx` 也认这条（触发点靠它判断有没有正文）");
            }
        }

        // ---- ④ 族 B：四条「判不了」的条件 ----
        {
            Check(EffectCondition.Normalize("it has Stealth"), "targethaskw",
                  "★ `If it has Stealth`（`Flickerjump`）→ `targethaskw`");
            Check(EffectCondition.Normalize("it is [Destroyer]"), "targethaskw",
                  "★ `If it is [Destroyer]`（`Hardwired Destruction`）→ `targethaskw`（方括号是图标标记）");
            Check(EffectCondition.Normalize("a friendly troop died this turn"), "deaths",
                  "★ `If a friendly troop died this turn`（`Vengeful Surge`）→ `deaths`");
            Check(EffectCondition.Normalize("it has 0 Ranged Attack"), "rangedzero",
                  "★ `if it has 0 Ranged Attack`（`Terrifying Crescendo`）→ `rangedzero`");
            Check(EffectCondition.Normalize("your Warlord has 10 or less Health"), "warlordlowhp",
                  "★ `If your Warlord has 10 or less Health`（`At All Costs`）→ `warlordlowhp`");
            // 反例：别把后面才判的那几种吃掉
            Check(EffectCondition.Normalize("it is damaged"), "damaged",
                  "★ 反例：`If it is damaged` 仍是 `damaged`（没被 `targethaskw` 抢走）");
        }

        // ---- ④b 族 B 端到端：`Terrifying Crescendo` 真的会摧毁 ----
        //   `Give -3 Ranged Attack to an enemy troop. Then, if it has 0 Ranged Attack, destroy it`
        {
            var tc = CardDatabase.Find(pool, "Terrifying Crescendo");
            CheckTrue(tc != null, "卡池里有 `Terrifying Crescendo`");
            if (tc != null)
            {
                var ctx = BattlePool(new[] { tc }, new[] { Unit("X", 1, 1, 5) }, pool,
                                     warlordFaction: "EmperorsChildren");
                ToP1Turn(ctx, 1);
                ctx.Players[0].Energy = 12;
                ctx.ResetChoices();
                var foe = Place(ctx, 1, 0, Ranged("FoeR3", 1, 2, 9, 3));   // 远 3 ⇒ 减 3 之后正好 0
                CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "Terrifying Crescendo"), 0),
                          RuleCodes.OK, "打出 `Terrifying Crescendo`（点敌方那一格）");
                CheckTrue(foe == null || !foe.IsAlive,
                          "★ 远程被减到 0 之后**真的被摧毁了** —— 条件这条原来判「判不了」⇒ 整句的后半永远不发生");
            }
        }

        // ---- ⑤ 族 C：`blind` 走载荷也给得上了 ----
        {
            string why;
            CheckTrue(GivePayload.Mechanized("blind", out why),
                      "★ 载荷 `blind` 有机制了（原版 `GIVE_KW` 里没有这一条 —— 照卡面补的）");
            var fb = CardDatabase.Find(pool, "Fenrisian Blizzard");
            CheckTrue(fb != null, "卡池里有 `Fenrisian Blizzard`");
            if (fb != null)
            {
                var ctx = BattlePool(new[] { fb }, new[] { Unit("X", 1, 1, 5) }, pool,
                                     warlordFaction: "SpaceWolves");
                ToP1Turn(ctx, 1);
                ctx.Players[0].Energy = 12;
                ctx.ResetChoices();
                var foe = Place(ctx, 1, 0, Unit("FoeBig", 1, 2, 9));
                CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "Fenrisian Blizzard"), -1),
                          RuleCodes.OK, "打出 `Fenrisian Blizzard`");
                CheckTrue(foe.IsBlind, "★ 敌方单位**真的失明了**（`give them Blind` 原来静默不发生）");
            }
        }

        // ---- ⑥ 族 C：`(1)` 是**图标被 OCR 丢掉**，走数据侧修掉 ----
        {
            var ms = CardDatabase.Find(pool, "Martial Superiority");
            CheckTrue(ms != null, "卡池里有 `Martial Superiority`");
            if (ms != null)
            {
                Check(ms.Desc, "Heal 2 to a friendly unit.\nGain 1 Quest Point",
                      "★ desc 已按卡面修回（`Gain (1)` = 卡面那个**锯齿圆环徽记** = 任务点；"
                      + "判据 = 卡图上那个徽记与 `Intercessor`（`Dark Angels/3部队/Warpforge_07_Intercessor.png`）"
                      + "逐像素同形 + 我们自己的中文表；**不是光看形状认的**）"
                      + "  ［2026-09-18 措辞更正：原写「官方中文」—— 实测原版客户端**没有中文表**，那份中文是我们自己译的］");
                var ctx = BattlePool(new[] { ms }, new[] { Unit("X", 1, 1, 5) }, pool,
                                     warlordFaction: "DarkAngels");
                ToP1Turn(ctx, 1);
                ctx.Players[0].Energy = 12;
                ctx.ResetChoices();
                var u = Place(ctx, 0, 0, Unit("MyTroop", 1, 2, 3));
                u.Health = 1;
                int before = ctx.Players[0].QuestPoints;
                CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "Martial Superiority"), 0),
                          RuleCodes.OK, "打出 `Martial Superiority`（点我方那一格）");
                Check(u.Health, 3, "★ 治疗生效（1 → 3）");
                Check(ctx.Players[0].QuestPoints, before + 1, "★ **任务点 +1**（原来 `(1)` 谁都认不出）");
            }
        }
    }

    /// <summary>
    /// 打一张「三选一」的卡、指定**面板选第几项**，回传打完之后的局面。
    /// `pick &lt; 0` = 不填队列（模拟面板没问）。
    /// ⚠️ 判据用**局面**（场上单位数 / 手牌数），**不是日志**（和 `TestPlayerChoice` ② 同一条纪律：
    ///    日志那条路脆，而且改一句文案就断）。
    /// </summary>
    static bool RunChooseOne(IList<CardDef> pool, string cardName, int pick,
                             out int board, out int hand, out int answered, out string diag)
    {
        board = 0; hand = 0; answered = 0; diag = "?";
        var h0 = new[] { Unit("OneFodder1", 1, 0, 1), Unit("OneFodder2", 1, 0, 1),
                         PoolCard(pool, cardName) };
        var ctx = BattlePool(h0, new[] { Unit("OneFoe", 1, 0, 9) }, pool, "SpaceWolves");
        ToP1Turn(ctx, 5);
        ctx.Players[0].Energy = 20;
        int idx = HandIdx(ctx, 0, cardName);
        if (idx < 0) { diag = "牌不在手里"; return false; }

        var asks = RuleCore.PlayerChooseOps(ctx.Players[0].Hand[idx].Card);
        if (asks.Count == 0) { diag = "没有 ask 点"; return false; }

        ctx.ResetChoices();
        if (pick >= 0) ctx.ChoosePicks.Enqueue(pick);       // 面板在弹出那一刻填的就是这个
        int code = RuleCore.PlayTactic(ctx, 0, idx, -1);
        if (code != RuleCodes.OK) { diag = $"打出被拒（{RuleCodes.Describe(code)}）"; return false; }

        var ps = ctx.Players[0];
        for (int i = 0; i < ps.Board.Length; i++)
            if (i != BoardSpec.WarlordSlot && ps.Board[i] != null) board++;   // 督军不算（它一直在）
        hand = ps.Hand.Count;
        answered = ctx.ChooseAnswered;
        diag = $"场上 {board} / 手牌 {hand} / 面板答了 {answered}";
        return true;
    }

    /// <summary>
    /// 部署 `Hrolf the Ironhowl`（`Rally: Stratagems in your hand become A or B`），
    /// 指定**面板选第几项**，回传**变身之后手牌里那只狼的名字**（没变成返回空串）。
    /// `pick &lt; 0` = 不填队列（模拟面板没问）。
    /// ⚠️ 判据找的是**名字 = 两只狼之一**的手牌 —— 战略卡变身之后**已经不是战略卡**了
    ///    （变成 unit），所以不能按 `MatchesKind` 回头找。
    /// </summary>
    static string RunBecome(IList<CardDef> pool, int seed, out int asked, out string diag)
    {
        asked = 0;
        diag = "?";
        // 手牌里得有**战略卡**（`CreatePool.MatchesKind(c,"stratagem")` 认 `type ∈ {tactic,defence}`）
        var h0 = new[] { Tactic("BecomeStrat1", 1, "Draw a card"), Tactic("BecomeStrat2", 1, "Draw a card"),
                         PoolCard(pool, "Hrolf the Ironhowl") };
        var ctx = BattlePool(h0, new[] { Unit("BecomeFoe", 1, 0, 9) }, pool, "SpaceWolves", seed);
        ToP1Turn(ctx, 5);
        ctx.Players[0].Energy = 20;
        int idx = HandIdx(ctx, 0, "Hrolf the Ironhowl");
        if (idx < 0) { diag = "Hrolf 不在手里"; return ""; }

        ctx.ResetChoices();
        // ⚠️ **故意不填 `ChoosePicks`** —— 用户裁决卡面没有 `choose`，本来就不该问玩家。
        //    填了反而会掩盖「改回问玩家」这种回归。
        int code = RuleCore.PlayCard(ctx, 0, idx, 0);       // 单位走 `PlayCard`，`Rally` 在部署时触发
        if (code != RuleCodes.OK) { diag = $"部署被拒（{RuleCodes.Describe(code)}）"; return ""; }
        asked = ctx.ChooseSites;

        // 手牌里那批战略卡变成了哪一只；**混着两只**说明「每张各掷一次」（那是另一种读法，不是我们要的）
        string got = "";
        bool mixed = false;
        foreach (var c in ctx.Players[0].Hand)
        {
            if (c == null) continue;
            if (c.Card.Name != "Hunting Wolf" && c.Card.Name != "Fenrisian Wolf") continue;
            if (got.Length == 0) got = c.Card.Name;
            else if (got != c.Card.Name) mixed = true;
        }
        var hn = new List<string>();
        foreach (var c in ctx.Players[0].Hand) hn.Add(c != null ? c.Card.Name : "null");
        diag = $"变身 {got}（混合={mixed}；问了玩家 {ctx.ChooseSites} 次）"
             + $"；手牌 = {string.Join("/", hn.ToArray())}";
        return mixed ? "MIXED" : got;
    }

    static void TestAuraSettle()
    {
        var pool = CardDatabase.Load();
        var baneblade = PoolCard(pool, "Baneblade Tank");      // `Armour 2. Adjacent units have Armour 1`

        // ---- ① 钩子：走**真实部署路径**（`DeployFree`）那一刻就要生效 ----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 0, Unit("Filler0", 1, 1, 5));
            Place(ctx, 0, 1, Unit("Filler1", 1, 1, 5));
            Place(ctx, 0, 3, Unit("Filler3", 1, 1, 5));
            int slot;
            CheckTrue(RuleCore.DeployFree(ctx, 0, baneblade, out slot) && slot == 2,
                      "★ `Baneblade Tank` 落在 2 号格（前提：0/1/3 已被占、4 是督军）");
            Check(ctx.Players[0].Board[1].Armor, 1, "★ 1 号格邻居 +1 护甲（**部署那一刻**就算出来）");
            Check(ctx.Players[0].Board[3].Armor, 1, "★ 3 号格邻居 +1 护甲");
            Check(ctx.Players[0].Board[0].Armor, 0, "★ 不相邻的 0 号格**不沾光**");
            Check(ctx.Players[0].Board[2].Armor, 2,
                  "★ **自己不吃自己的光环** —— 它自己印的 `Armour 2` 原样（相邻不含本格）");

            // ---- 收回：来源**死了**（走真实的伤害入口 → 离场那一段）----
            // ⚠️ 不能用 `ApplyDamage` —— 那个**只扣血**，离场在下一段（实测踩到过：
            //    单位还留在棋盘上，这条断言就变成了「测一件没发生的事」）。
            RuleCore.HurtForTest(ctx, ctx.Players[0].Board[2], 99, "夹具");
            CheckTrue(ctx.Players[0].Board[2] == null, "★ `Baneblade Tank` 已经离场（前提）");
            Check(ctx.Players[0].Board[1].Armor, 0,
                  "★ **来源离场 ⇒ 加成收回** —— 邻居回到 0（不是 1，也没被扣成负数）");
            Check(ctx.Players[0].Board[3].Armor, 0, "★ 同上，3 号格");
        }

        // ---- ② 可叠加（用户拍板「可以叠加」）+ 只减自己那一份 ----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            var guard = Place(ctx, 0, 2, Unit("ArmourGuy", 1, 1, 5, "Armour 1"));   // 自己印着 1 点护甲
            Place(ctx, 0, 1, baneblade);                          // 左邻：+1
            Place(ctx, 0, 3, PoolCard(pool, "Honour Guard"));     // 右邻：`Adjacent units have Armour 1`，再 +1
            Check(guard.Armor, 1, "前提：它自己印着 1 点护甲（`Armour 1` 关键词）");
            Auras.Recompose(ctx);
            Check(guard.Armor, 3, "★ 自己 1 + 左邻光环 1 + 右邻光环 1 = **3**（光环可叠加）");

            // ⚠️ 收回时必须**只减光环那一份** —— 用 `RemoveAll` 那种「整个摘掉」会把自己的 1 也抹了
            ctx.Players[0].Board[1] = null;
            ctx.Players[0].Board[3] = null;
            Auras.Recompose(ctx);
            Check(guard.Armor, 1,
                  "★ 两个光环来源都走了 ⇒ 回到**它自己的 1**（🔴 盲减/`RemoveAll` 会在这里露馅）");
        }

        // ---- ③ `Your other units …`：**排除自己**，但**督军要算**（规则书 `:70-75` 的「单位」）----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            var stalker = Place(ctx, 0, 5, PoolCard(pool, "Triarch Stalker"));  // `Your other units have +2 Ranged Attack`
            var mate = Place(ctx, 0, 6, Ranged("Mate", 1, 2, 5, 3));
            int stalkerBase = stalker.RangedAttack, warlordBase = ctx.Players[0].Warlord.RangedAttack;
            Auras.Recompose(ctx);
            Check(mate.RangedAttack, 5, "★ 另一个单位 +2 远程");
            Check(stalker.RangedAttack, stalkerBase, "★ **自己不吃**（卡面 `Your **other** units`）");
            Check(ctx.Players[0].Warlord.RangedAttack, warlordBase + 2,
                  "★ **督军也吃得到**（卡面写 `units` —— 规则书 `:70-75`：「单位」含督军）");
        }

        // ---- ④ `Adjacent **troops** have Vanguard`：`troops` 是**真筛选**，督军吃不到 ----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            var troop = Place(ctx, 0, 2, Unit("SomeTroop", 1, 2, 5));
            Place(ctx, 0, 3, PoolCard(pool, "Honoured Ethereal"));   // `Shield. Adjacent troops have Vanguard.`
            Auras.Recompose(ctx);
            CheckTrue(troop.Has("vanguard"), "★ 相邻的**部队**得到 `Vanguard`");
            CheckTrue(!ctx.Players[0].Warlord.Has("vanguard"),
                      "★ 相邻的**督军吃不到** —— 卡面写的是 `troops` 不是 `units`"
                      + "（🔴 这条就是「单位 ≠ 部队」那条口径在光环上的落地）");
        }

        // ---- ④b `+1 Melee and +1 Ranged Attack` 的**两半都要落**（`Company Ancient`）----
        //   🔴 **2026-09-14 实做时踩到**：`GivePayload` 那条正则**认得 `melee`**，
        //      但它的 `switch` 不映射 ⇒ `PayloadOp.Attr` 留着 `"melee"`，而 `UnitState.ApplyGrant`
        //      的 switch 里**没有** `melee` ⇒ **属性静默丢失**（解析成功、结算时无操作）。
        //      光环那条路一开始绕过了 `NormalizeAttr`，于是只有远程那半加上了。
        //      ⇒ 这条断言就是钉「**两半都在**」，缺一半会在近战那一行红。
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            var mate = Place(ctx, 0, 2, Ranged("MeleeMate", 1, 2, 5, 3));
            int a0 = mate.Attack, r0 = mate.RangedAttack;
            Place(ctx, 0, 3, PoolCard(pool, "Company Ancient"));
            Auras.Recompose(ctx);
            Check(mate.Attack, a0 + 1,
                  "★ `+1 Melee` 那半**真的加上了**（属性词 `melee` 必须映射成 `attack`）");
            Check(mate.RangedAttack, r0 + 1, "★ `+1 Ranged Attack` 那半也加上了");
        }

        // ---- ⑤ 关键词型光环 + 回合限定 ----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            var mate = Place(ctx, 0, 2, Unit("PackMate", 1, 2, 5));
            Place(ctx, 0, 3, PoolCard(pool, "Wolf Guard Battle Leader"));   // `Adjacent units have Pack.`
            Auras.Recompose(ctx);
            CheckTrue(mate.Has("pack"), "★ 相邻单位得到 `Pack` 关键词（关键词型光环）");

            // `Friendly units with Pack have Invulnerable **during your turn**`（`Fyrri Askar`）
            Place(ctx, 0, 0, PoolCard(pool, "Fyrri Askar"));
            Auras.Recompose(ctx);
            CheckTrue(mate.Has("invulnerable"), "★ 自己的回合：带 `Pack` 的友方单位得到 `Invulnerable`");
            // 换边 —— ⚠️ 用 `PassTurn`（= `EndTurn` + `BeginTurn`）：光环重算的钩子在 `BeginTurn`，
            //    只调 `EndTurn` 的话那一刻还没到（`EndTurn` 只负责换 `ctx.Active`）。
            PassTurn(ctx);
            CheckTrue(!mate.Has("invulnerable"),
                      "★ 到了**对方回合**：`during your turn` 那条**灭掉**（钩子挂在 `BeginTurn`）");
        }

        // ---- ⑥ 自指型：`Has Flying during your turn`（督军 `Commander O'Maisos`）----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            var hero = Place(ctx, 0, 2, PoolCard(pool, "Commander O'Maisos"));
            Auras.Recompose(ctx);
            CheckTrue(hero.Has("flying"), "★ 自己的回合：它**自己有** `Flying`");
            PassTurn(ctx);
            CheckTrue(!hero.Has("flying"), "★ 对方回合：`during your turn` ⇒ 灭掉");
        }

        // ---- ⑦ `Nemesor Zahndrekh`：**相邻残骸不被回合结束摧毁** ----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 3, PoolCard(pool, "Nemesor Zahndrekh"));
            var near = Place(ctx, 0, 2, Unit("RemNear", 1, 1, 5));
            var far = Place(ctx, 0, 6, Unit("RemFar", 1, 1, 5));
            near.IsRemnant = true;
            far.IsRemnant = true;
            Auras.Recompose(ctx);
            CheckTrue(near.AuraRemnantStay, "★ 相邻的残骸被罩住");
            CheckTrue(!far.AuraRemnantStay, "★ 不相邻的不罩");
            RuleCore.EndTurn(ctx);              // 回合结束扫的是**这一方的**残骸
            CheckTrue(ctx.Players[0].Board[2] != null && ctx.Players[0].Board[2].IsRemnant,
                      "★ 被罩住的残骸**留场**");
            CheckTrue(ctx.Players[0].Board[6] == null,
                      "★ 没被罩住的残骸**照常被摧毁**（反例 —— 只有反例能抓住「全都留场」那种写错）");
        }

        // ---- ⑧ Stealth 的「**一回合到期**」（规则书 `:211`：「一回合内**或**本单位攻击前」）----
        {
            var ctx = Battle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 1);
            var sneak = Place(ctx, 0, 2, Unit("Sneaky", 1, 2, 5, "Stealth"));
            CheckTrue(sneak.Has("stealth"), "前提：带着 `Stealth`");
            PassTurn(ctx);
            CheckTrue(sneak.Has("stealth"),
                      "★ **对方回合里仍然隐身** —— 那正是它有用的时候（扫的是**当前行动方**的单位）");
            PassTurn(ctx);
            CheckTrue(!sneak.Has("stealth"),
                      "★ **回到自己回合** ⇒ 一回合到期、失效"
                      + "（🔴 不补这条的话，不攻击的潜行单位会**永久隐身**）");
        }
    }

    /// <summary>
    /// **「相邻」的端到端验收**（2026-09-13 候选 F）。
    ///
    /// 这块以前**整个是空的**：`EffectTargetSpec.Adjacent` 认得写法却**没有消费者**，
    /// 于是卡面写「相邻」的 36 张打的**不是卡面写的目标集**，而**卡面不打 `*`**（正文解析是成功的）
    /// —— 标准的静默失效。现在锚点在解析期定（`AdjacentAnchor`，照原版 `TargetsAffected.cs:17-22`）、
    /// 结算期由 `EffectResolver.ResolveTargets` 消费；「谁算相邻」唯一收在 `BoardSpec.AdjacentSlots`。
    ///
    /// 三条**必须各钉一次**（缺一条就有盲区）：
    ///   · 锚点 = **被打目标**（`Cleansing Flames`，真实卡）
    ///   · 锚点 = **自己**（`Strike: Heal N to adjacent units`，夹具）
    ///   · **反例**：锚点是目标时，**施放者自己那一排的邻居不该挨打** —— 锚点写反只有反例抓得到
    /// </summary>
    static void TestAdjacent()
    {
        // ---- 0) 「谁算相邻」只有一处：`BoardSpec.AdjacentSlots`（原来 `RuleCore` 里内联写了两遍）----
        {
            var slots = new List<int>();
            BoardSpec.AdjacentSlots(0, slots);
            Check(string.Join(",", slots), "1", "0 号格（左边界）的相邻格只有右边一个");
            BoardSpec.AdjacentSlots(4, slots);
            Check(string.Join(",", slots), "3,5", "督军格（4）的相邻格 = 3 / 5（**不排除督军格**）");
            BoardSpec.AdjacentSlots(8, slots);
            Check(string.Join(",", slots), "7", "8 号格（右边界）的相邻格只有左边一个");
            BoardSpec.AdjacentSlots(99, slots);
            Check(slots.Count, 0, "非法格位返回空表（不抛、也不返回自己）");
        }

        // ---- ① 锚点 = **被打目标**：`Deal 3 damage to an enemy and its adjacent units` ----
        //   用**真实卡** `Cleansing Flames`（Sororitas）：卡面原文就是这一句。
        {
            var pool = CardDatabase.Load();
            var flames = PoolCard(pool, "Cleansing Flames");
            CheckTrue(flames != null, "卡池里有 `Cleansing Flames`");
            if (flames != null)
            {
                var ctx = BattlePool(new[] { flames }, new CardDef[0], pool);
                ToP1Turn(ctx, 8);

                var e1 = Place(ctx, 1, 1, Unit("EFar", 1, 0, 20), exhausted: true);
                var e2 = Place(ctx, 1, 2, Unit("EMid", 1, 0, 20), exhausted: true);   // ← 锚点
                var e3 = Place(ctx, 1, 3, Unit("ENear", 1, 0, 20), exhausted: true);
                var e5 = Place(ctx, 1, 5, Unit("EFar2", 1, 0, 20), exhausted: true);
                // **反例用的**：自己那一排、和锚点同号的格子
                var m1 = Place(ctx, 0, 1, Unit("MyL", 1, 0, 20), exhausted: true);
                var m3 = Place(ctx, 0, 3, Unit("MyR", 1, 0, 20), exhausted: true);

                int hi = HandIdx(ctx, 0, "Cleansing Flames");
                CheckTrue(hi >= 0, "`Cleansing Flames` 在手里");
                CheckCode(RuleCore.PlayTactic(ctx, 0, hi, 2), RuleCodes.OK,
                          "对 2 号格那个敌方部队打出「相邻」战术卡");

                Check(e2.Health, 17, "★ **锚点自己也吃 3 点**（卡面写的是 `an enemy **and its** adjacent units`）");
                Check(e1.Health, 17, "★ 左边**相邻**的那个也吃 3 点");
                Check(e3.Health, 17, "★ 右边**相邻**的那个也吃 3 点");
                Check(e5.Health, 20, "★ 隔了两格的不吃（**相邻 ≠ 全场**）");
                Check(m1.Health, 20, "★ **反例**：施放者自己那一排的邻居**不该挨打**"
                                   + "（把锚点写反成 `Self` 只有这条抓得到）");
                Check(m3.Health, 20, "★ 反例（另一侧）同理");
            }
        }

        // ---- ② 锚点 = **自己**：`Strike: Heal 3 to adjacent units`（真卡是 `Chaplain Letharius`，
        //      但它是**没有触发前缀**的单位正文（引擎还没跑那条路，见 A5），所以用夹具把这个语义钉住）----
        {
            var healer = new CardDef("FuHealer", "FuHealer", "unit",
                                     "Strike: Heal 3 to adjacent units", "common", "Test",
                                     1, 1, 6, 0, new[] { "Strike" });
            var ctx = ProbeBattle(new[] { healer }, new[] { Unit("EFoe", 1, 0, 30) });
            ToP1Turn(ctx, 2);

            var h = Place(ctx, 0, 2, healer);
            var l = Place(ctx, 0, 1, Unit("FriendL", 1, 1, 6));
            var r = Place(ctx, 0, 3, Unit("FriendR", 1, 1, 6));
            var f = Place(ctx, 0, 5, Unit("FriendFar", 1, 1, 6));
            l.Health = 2; r.Health = 2; f.Health = 2;
            var foe = Place(ctx, 1, 2, Unit("EFoe", 1, 0, 30));

            CheckCode(RuleCore.DeclareAttack(ctx, 0, 2, 1, 2), RuleCodes.OK,
                      "用带 `Strike:` 的治疗者打一下（触发它自己的正文）");
            Check(l.Health, 5, "★ 左边**相邻**的队友被治了 3（锚点 = **施放者自己**）");
            Check(r.Health, 5, "★ 右边**相邻**的队友被治了 3");
            Check(f.Health, 2, "★ 隔了两格的队友**没被治**（相邻 ≠ 己方全场 —— 这正是原来那个「打多了」）");
            Check(h.Health, 6, "★ **施放者自己不在目标集里**（卡面是 `adjacent units`，"
                             + "要含自己得写 `this troop and its adjacent units`）");
            Check(foe.Health, 29, "★ 敌方只有**那次攻击**的 1 点伤害，治疗一滴都没沾到它"
                               + "（相邻判据是**同一方那一排**的左右格）");
        }

        // ---- ③ 锚点认不出 ⇒ **空过并如实报**，绝不退回「整个目标池」 ----
        //   ⚠️ 现在**没有一张真卡**走到这一支（36 张的锚点全都定得下来，见对账表）——
        //   所以这里**直接构造**一个没定锚点的规格来钉住结算层的行为，别让它在没人看的时候退化。
        //   （`When <事件>, … adjacent …` 那张（`Long Fang`）等 `When` 短语落地后才会产生 op，
        //     那时 `EffectText` 的判据 ⑤ 会把它标成认不出 —— 见 `FillAdjacentAnchors`。）
        {
            var ctx = ProbeBattle(new CardDef[0], new CardDef[0]);
            ToP1Turn(ctx, 2);
            Place(ctx, 1, 1, Unit("EFoe1", 1, 0, 9), exhausted: true);
            Place(ctx, 1, 2, Unit("EFoe2", 1, 0, 9), exhausted: true);
            Place(ctx, 1, 3, Unit("EFoe3", 1, 0, 9), exhausted: true);

            var broke = new EffectTargetSpec
            {
                Raw = "(锚点认不出)", Side = "enemy", Kind = "any", Count = 1,
                Adjacent = true, Anchor = AdjacentAnchor.Unset,
            };
            var got = RuleCore.ResolveTargets(ctx, 0, broke, null, null);
            Check(got.Count, 0, "★ 锚点认不出 ⇒ **空过**（退回「整个目标池」的话这里会是 1"
                              + " —— 那正是「打得比卡面宽」，是这一轮要消灭的东西）");

            const string when = "When an enemy attacks, deal 2 damage to adjacent enemies";
            CheckTrue(!EffectText.IsFullyParsed(when),
                      "★ `When … adjacent …` **不算解析干净**（认不出的锚点不许冒充成功）");
        }

        // ---- ④ 单数 vs 复数：`an adjacent troop` 只挑一个，`adjacent troops` 一圈全要 ----
        //   ⚠️ 正文挂在 `atturn` 的 `AtTurnOps` 里（`At the end of your turn, …`）——
        //      要看那一层的 op，用 `Walk`，别拿最外层那个（它自己没有目标）。
        {
            var one = EffectText.Parse("At the end of your turn, give Stealth to an adjacent troop",
                                       out _, out _);
            EffectTargetSpec spec1 = null;
            foreach (var o in Walk(one))
                if (o.Target != null && o.Target.Adjacent) spec1 = o.Target;
            CheckTrue(spec1 != null && spec1.Anchor == AdjacentAnchor.Self,
                      "★ `an adjacent troop` → 锚点 = 自己（单数，`Stealth Drone`）");
            CheckTrue(spec1 != null && !spec1.AdjacentAll,
                      "★ **单数** ⇒ 不标「一圈全要」（挑一个，按槽号小的优先，不掷骰）");

            var many = EffectText.Parse("Strike: Give Invulnerable to adjacent troops this turn",
                                        out _, out _);
            EffectTargetSpec spec2 = null;
            foreach (var o in Walk(many))
                if (o.Target != null && o.Target.Adjacent) spec2 = o.Target;
            CheckTrue(spec2 != null && spec2.AdjacentAll,
                      "★ `adjacent troops`（复数） ⇒ **一圈全要**（`Nuadhu Fireheart`）");
        }

        // ---- ⑤ 光环族**不走「一次性给己方全体」那条路**，由**光环层**认领 ----
        //   2026-09-14 A7 第 2 步：解析 + 数据模型已接（`CardDef.AuraSpecs`）；
        //   **结算仍是 A7 第 3 步** —— 在它落地之前光环**不会生效**（如实的，不是静默失败）。
        //   ⚠️ 这里钉的是**两件相反的事**，缺一条就有盲区：
        //     · `EffectText` 那把尺子**照样**判它「解析不出来」—— 这是**对的**：
        //       光环句**不该**产生一条一次性 op（产生了就会在打出这张牌时被结算一次，
        //       那是把常驻当成一次性的，见 `CardDef.AuraSpecs` 的说明）。
        //     · 同时它**必须**被光环层收下来 —— 否则就是「谁都没管」的静默缺口。
        {
            CheckTrue(!EffectText.IsFullyParsed("Adjacent units have Armour 1"),
                      "★ 光环句**不产生一次性 op**（`EffectText` 那把尺子判它解析不出来是**对的**）—— "
                      + "绝不能被当成「给己方全体加」");
            // ⚠️ 2026-09-13 更正：交接文档原来写这 10 张「现在走普通一次性路径 ⇒ 给己方全体加」——
            //    **不成立**。实测这三张真实卡的 `desc` 都**不算解析干净**（光环句根本没被认领），
            //    所以它们对谁都没生效，不是「打多了」。按铁律 5 就地改了文档。
            var pool = CardDatabase.Load();
            foreach (string n in new[] { "Makari the Grot", "Baneblade Tank", "Honour Guard" })
            {
                var c = PoolCard(pool, n);
                CheckTrue(c != null && !EffectText.IsFullyParsed(c.Desc),
                          $"★ `{n}` 的光环句**不在 `EffectText` 那条路上**（它不产生 op，如实）");
                CheckTrue(c != null && c.AuraSpecs.Count > 0,
                          $"★ ……但它**必须**被**光环层**收下来（`AuraSpecs`）—— "
                          + "两把尺子都不认 = 「谁都没管」的静默缺口");
            }
        }

        // ---- ⑥ 真实卡的锚点（解析级对账 —— 上表 `adjacent_report.md` 的抽样复核）----
        {
            var pool = CardDatabase.Load();
            var chaplain = PoolCard(pool, "Chaplain Letharius");
            CheckTrue(chaplain != null, "卡池里有 `Chaplain Letharius`");
            if (chaplain != null)
            {
                var ops = EffectText.Parse(chaplain.Desc, out _, out _);
                var t = ops != null && ops.Count > 0 ? ops[0].Target : null;
                CheckTrue(t != null && t.Adjacent && t.Anchor == AdjacentAnchor.Self
                          && !t.AnchorInSet && t.AdjacentAll,
                          "★ `Heal 1 to adjacent units` → 锚点 = 自己、**不含自己**、一圈全要");
            }
        }
    }

    /// <summary>锚点 → 人看的短名（对账表用）</summary>
    static string AnchorName(EffectTargetSpec t)
    {
        if (t.AdjacentFailed) return "认不出";
        return t.Anchor == AdjacentAnchor.Unset ? "待定" : t.Anchor.ToString();
    }

    /// <summary>整段 + 「`前缀:` 后面那段」（触发式正文挂在冒号后）。去重，顺序稳定。</summary>
    static IEnumerable<string> Probes(string seg)
    {
        yield return seg;
        int c = seg.IndexOf(':');
        if (c > 0 && c + 1 < seg.Length)
        {
            string body = seg.Substring(c + 1).Trim();
            if (body.Length > 0 && body != seg) yield return body;
        }
    }

    /// <summary>一条文本的**两种解析上下文**：① 整条（战术卡那条路）② 每个分句冒号后的正文
    /// （单位卡触发正文那条路，见 `CardDef.AddTriggerOp`）。去重，顺序稳定。</summary>
    static IEnumerable<string> Probes2(string text)
    {
        yield return text;
        foreach (string seg in EffectText.Split(text))
        {
            string body = null;
            int c = seg.IndexOf(':');
            if (c > 0 && c + 1 < seg.Length) body = seg.Substring(c + 1).Trim();
            if (!string.IsNullOrEmpty(body) && body != text) yield return body;
        }
    }

    /// <summary>一张卡的**可解析文本**：`desc` 与每个关键词段落（`keywords` 里也可能挂正文）。
    /// ⚠️ 和 `SegsOf` 的分工：`SegsOf` 是**逐句摊平**的（量「哪句话提到某个词」），
    /// 这里是**整条文本**（量「按上下文解析成什么」）—— 「相邻」的锚点判据要用**上一句点过谁**，
    /// 摊平就丢了那个上下文（本轮先按句量，量出来的锚点是错的）。</summary>
    static IEnumerable<string> TextsOf(CardDef c)
    {
        if (!string.IsNullOrEmpty(c.Desc)) yield return c.Desc;
        foreach (var kw in c.Keywords)
            if (!string.IsNullOrEmpty(kw.Key)) yield return kw.Key;
    }

    /// <summary>op 列表 + 挂在它们身上的子 op（`AtTurnOps`，`At the start|end of your turn, …` 的正文）
    /// —— 摊平，顺序稳定。
    /// ⚠️ **不展开 `RepeatOps`**：那存的是**前面 op 的引用**（同一批对象），再走一遍会重复计数。</summary>
    static IEnumerable<EffectOp> Walk(IEnumerable<EffectOp> ops)
    {
        foreach (var o in ops)
        {
            if (o == null) continue;
            yield return o;
            if (o.AtTurnOps != null)
                foreach (var inner in Walk(o.AtTurnOps)) yield return inner;
        }
    }

    static void ReportUnitDescCoverage()
    {
        var pool = CardDatabase.Load();
        var sb = new StringBuilder();
        sb.AppendLine("**单位卡 / 督军卡的 desc 拿 EffectText 解析** —— 未覆盖清单（2026-09-13 查证）");
        // 🔴 **2026-09-16 更正（这份报表的口径）**：这里量的**不是单位卡自己的账**。
        //    单位卡 / 督军卡的 `desc` 带 `Rally:` / `When <事件>,` / `Strike:` 这些**前缀**，
        //    正文早被 `CardDef.AddWhenTrigger` / `AddTriggerOp` 分走了 ⇒
        //    主解析器解出来的东西是**没人执行的残渣**。
        //    ⇒ 这张表**只能当「这句话主解析器认不认识」看**，**不能当「这张卡能不能跑」看**。
        //    **单位卡真正的账**在 `ReportWillRunMechanism`（判据 `EffectText.WillRunOps`，
        //    按卡类型问对的层）。两张表**口径不同、不许互相印证**。
        sb.AppendLine("> ⚠️ **口径提醒（2026-09-16）**：这份量的是**主解析器**的意见，");
        sb.AppendLine("> 而单位卡/督军卡实际走**事件层/触发层** ⇒ 这张表**不是它们自己的账**。");
        sb.AppendLine("> 真正那份看 `_tmp_view/willrun_mechanism.md`（`ReportWillRunMechanism`）。");
        sb.AppendLine();
        // ---- 防御卡（39 张）也量一遍（2026-09-13 第三十三轮加）----
        // 为什么加：防御卡一直是「引擎不认识 `defence`、`ErrUnimplemented`」的状态，
        // 所以**从来没人量过它的效果文字能解析多少**。要动手做它，先得知道覆盖率。
        foreach (string t in new[] { "unit", "hero", "defence" })
        {
            var cov = EffectText.Coverage(pool, t, pool);
            if (cov.Cards == 0) continue;
            Debug.Log(P + $"   [{t}] " + cov.Summary());
            sb.AppendLine($"## [{t}] " + cov.Summary());
            sb.AppendLine();
            Append(sb, $"① 完全不认识的句子 —— [{t}]", cov.UnknownFreq);
            Append(sb, $"② 半懂（句型认了、词表里没有）—— [{t}]", cov.PartialFreq);
            sb.AppendLine($"### 完全解析不了的卡 —— [{t}]");
            sb.AppendLine("   " + string.Join("、", cov.NoneCards));
            sb.AppendLine();

            var top = new List<KeyValuePair<string, int>>(cov.UnknownFreq);
            top.Sort((a, b) => b.Value.CompareTo(a.Value));
            for (int i = 0; i < top.Count && i < 8; i++)
                Debug.Log(P + $"     ×{top[i].Value,-3} {top[i].Key}");
        }
        const string path = "d:/4/_tmp_view/unit_desc_unparsed.txt";
        System.IO.File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8);
        Debug.Log(P + "   全量清单写到 " + path);

        // ---- 第二层：**收到了正文的触发**（2026-09-13 新开的那条路）----
        // ⚠️ 上面那个 `Coverage` 量的是「把整条 desc 当战术卡解析」，**带 `Rally:` 前缀**，
        //    所以它认不出 —— 那条路和这次做的**不是同一件事**。真正要盯的是这个数：
        //    「有几张卡的触发**真的收到了正文**」（`CardDef.TriggerOps`）。
        int cardsWithOps = 0, opsCount = 0;
        var byKw = new Dictionary<string, int>();
        var sample = new List<string>();
        foreach (var c in pool)
        {
            int n = 0;
            foreach (var kv in c.TriggerTexts)
            {
                n++; opsCount++; Bump(byKw, kv.Key);
                if (sample.Count < 8) sample.Add($"{c.Name} [{kv.Key}] {kv.Value}");
            }
            if (n > 0) cardsWithOps++;
        }
        Debug.Log(P + $"   ★ 触发式效果**收到了正文**的卡：{cardsWithOps} 张 / 共 {opsCount} 条");
        var topK = new List<KeyValuePair<string, int>>(byKw);
        topK.Sort((a, b) => b.Value.CompareTo(a.Value));
        Debug.Log(P + "     按触发点：" + string.Join(" · ",
                  topK.ConvertAll(k => k.Key + "×" + k.Value).ToArray()));
        foreach (string s in sample) Debug.Log(P + "     · " + s);
    }

    ///
    /// <summary>
    /// **事件层**（`When &lt;事件&gt;, &lt;正文&gt;`）—— 2026-09-13 第三十二轮。
    ///
    /// 这一层分**上下两截**，两截都要钉：
    ///   ① **认事件**（`Core/WhenEvent.cs` 解析卡面）—— 认不出就返回 null，**绝不降级**
    ///   ② **发事件**（`EffectResolver.BroadcastWhen` + 四处广播点）—— 没人广播的话
    ///      监听器一辈子不响，**而卡面照样不打 `*`**（静默失效）
    /// 只钉 ① 会漏掉 ② —— 那正是本工程最怕的形状。所以这里**每条都钉到「局面真的变了」**。
    ///
    /// ⚠️ **极性**是这一层最容易反的地方（`When an enemy dies` vs `When a friendly troop dies`
    /// 事件种类完全一样，差别只在归属）—— 所以每条都**正反各钉一次**：
    /// 该触发的变了、**不该触发的没变**。第三十轮的教训：只钉「该生效的生效了」钉不住筛错。
    /// </summary>
    static void TestWhenEvents()
    {
        // ---- ① 解析器本身：**极性不许在解析期被吃掉** ----
        {
            var e1 = WhenEvents.Parse("an enemy dies");
            CheckTrue(e1 != null, "`an enemy dies` 认得出（第一版「先剥谁再判事」会把它剥成裸动词 `dies`）");
            if (e1 != null)
            {
                Check(e1.Kind, WhenEventKind.Die, "事件种类 = die");
                Check(e1.OwnerIs, WhenEvent.RelEnemy,
                      "**极性保留下来了**（`enemy` 被剥掉的话这里会是 -1 = 不限，那就是静默放宽）");
            }

            var e2 = WhenEvents.Parse("a friendly troop dies");
            CheckTrue(e2 != null, "`a friendly troop dies` 认得出");
            if (e2 != null)
            {
                Check(e2.Kind, WhenEventKind.Die, "事件种类 = die");
                Check(e2.OwnerIs, WhenEvent.RelFriendly, "极性 = 友方");
                CheckTrue(e2.Criteria != null && e2.Criteria.KindWord == "troop",
                          "兵种筛选 `troop` 记下来了");
            }

            var e3 = WhenEvents.Parse("you deploy a Vehicle");
            CheckTrue(e3 != null, "`you deploy a Vehicle` 认得出");
            if (e3 != null)
            {
                Check(e3.Kind, WhenEventKind.Deploy, "事件种类 = deploy");
                CheckTrue(e3.Criteria != null && e3.Criteria.KindWord == "vehicle", "兵种筛选 `vehicle`");
            }

            // ⚠️ **认不出的事件必须返回 null** —— 降级成「任意事件」= 那件事一发生就乱触发
            // ⚠️ 2026-09-13 第三十三轮：这里原来拿 `you collect a Spirit Stone` 当反例 ——
            //    **现在它认得了**（阵营资源那两条事件接上了），所以换一个**真的**认不出的短语。
            CheckTrue(WhenEvents.Parse("you shuffle your deck twice") == null,
                      "认不出的事件短语返回 null（**不许降级成「任意事件」**）");

            // ⚠️ 2026-09-13 第三十四轮：**这条断言反过来了**，旧的那句是
            //    「`When deployed` 也返回 null —— 宁可收不到，不许乱触发」。
            //    当时只有「收成**任何单位**部署」这一条路，收下来就是「打得比卡面宽」。
            //    现在有了自指（`SelfOnly`）就**不用二选一**了：收下来，但要求
            //    「发生事件的那个单位**就是**监听者自己」。
            var eSelf = WhenEvents.Parse("deployed");
            CheckTrue(eSelf != null, "`When deployed` **现在收得下来了**（第三十四轮加了自指）");
            if (eSelf != null)
            {
                Check(eSelf.Kind, WhenEventKind.Deploy, "事件种类 = deploy");
                CheckTrue(eSelf.SelfOnly,
                          "★ **而且是按「自指」收的** —— 不是「任何单位部署」（收错就是静默放宽）");
                Check(eSelf.OwnerIs, -1,
                      "自指不靠极性表达 —— 极性那一维留给 `friendly` / `enemy` 那种写法");
            }

            // ---- 阵营资源两条事件（2026-09-13 第三十三轮）----
            // 出处：卡面 `When you collect a Spirit Stone, …`（4 张灵族单位）·
            //       `When you gain Faith, deal 4 damage to the enemy warlord`（`Paragon Warsuit`）。
            var eSp = WhenEvents.Parse("you collect a Spirit Stone");
            CheckTrue(eSp != null, "`you collect a Spirit Stone` **认得出**了（原来落在认不出那一栏）");
            if (eSp != null) Check(eSp.Kind, WhenEventKind.GainSpirit, "事件种类 = gainspirit");
            var eFa = WhenEvents.Parse("you gain Faith");
            CheckTrue(eFa != null, "`you gain Faith` **认得出**了");
            if (eFa != null) Check(eFa.Kind, WhenEventKind.GainFaith, "事件种类 = gainfaith");
        }

        // ---- ② 单位卡上的监听器：**事件发生 → 局面真的变了**，且**极性正确** ----
        {
            var pool = CardDatabase.Load();
            // 监听器本体：友方 troop 死 → 自己 +3 攻（钉「真的改了数值」，不只看日志）
            var watcher = new CardDef("FixtureWatcher", "FixtureWatcher", "unit",
                                      "When a friendly troop dies, gain +3 Attack",
                                      "common", "Test", 1, 2, 9, 0, null, subtype: "Infantry");
            // 炮灰 1 血，一击就死
            var fodder = new CardDef("FixtureFodder", "FixtureFodder", "unit", "", "common", "Test",
                                     1, 1, 1, 0, null, subtype: "Infantry");

            CheckTrue(watcher.WhenTriggers.Count == 1,
                      "卡表加载时**监听器被收下来了**（`CardDef.CollectWhenTriggers`）");

            // ---- ②-a **友方** troop 死 —— 该触发 ----
            {
                var foe = new CardDef("FixtureFoe", "FixtureFoe", "unit", "", "common", "Test",
                                      1, 1, 1, 0, null, subtype: "Infantry");
                var ctx = BattlePool(new[] { watcher, foe }, new[] { Unit("EFoe", 1, 1, 1) },
                                     pool, warlordFaction: "Ultramarines");
                ToP1Turn(ctx, 4);
                var w = Place(ctx, 0, 0, watcher, exhausted: true);
                Place(ctx, 0, 1, foe, exhausted: true);
                Place(ctx, 1, 1, Unit("FixtureEnemyKiller", 1, 1, 1), exhausted: true);

                // ⚠️ **不能拿自己人当靶子** —— `IsValidTarget` 禁止（实测返回 `ErrTarget`）。
                //    改用「**敌方单位来打死我方的炮灰**」—— 这才是「友方 troop 死」的真实来源。
                //    ⚠️ 还得**换到对手的回合**（`ErrNotTurn = 4`）：监听器是**被动的**，
                //       它该在「事件发生」时触发，**不管现在是谁的回合** —— 这正是要验的。
                PassTurn(ctx);
                Check(ctx.Active, 1, "换到对手的回合");
                CheckCode(RuleCore.DeclareAttack(ctx, 1, 1, 0, 1), RuleCodes.OK, "对手打死我方炮灰");
                Check(SlotOf(ctx, 0, "FixtureFoe"), -1, "**友方**炮灰确实死了");
                Check(w.Attack, 5,
                      "★ **友方 troop 死 → 监听器真的结算了**（攻 2 → **5**）—— "
                      + "而且是在**对手的回合**里触发的（被动监听不该挑回合）");
            }

            // ---- ②-b **敌方**单位死 —— 监听的是「**友方** troop 死」，所以**不该**触发 ----
            {
                var own = new CardDef("FixtureOwn", "FixtureOwn", "unit", "", "common", "Test",
                                      1, 1, 1, 0, null, subtype: "Infantry");
                var ctx = BattlePool(new[] { watcher, own },
                                     new[] { Unit("EFoe", 1, 1, 9), Unit("FixtureEnemyFodder", 1, 1, 1) },
                                     pool, warlordFaction: "Ultramarines");
                ToP1Turn(ctx, 4);
                var w = Place(ctx, 0, 0, watcher, exhausted: true);
                Place(ctx, 0, 1, own, exhausted: true);          // 我方这张**不能死**（否则监听器该响）
                // ⚠️ **攻击方 9 血、目标 0 攻**：目标是 0 攻所以**不反击**，
                //    而攻击方就算被打也死不了 —— **这一格必须保证「只有敌方那一张死」**，
                //    否则「我方的攻击方吃反击死了」会自己触发监听器，
                //    这条断言就会以「实得 5」失败，而**失败的其实是测试自己**（实测踩到）。
                Place(ctx, 0, 2, Unit("FixtureOurKiller", 1, 9, 9), exhausted: false);
                Place(ctx, 1, 1, Unit("FixtureEnemyFodder", 1, 0, 1), exhausted: true);

                CheckCode(RuleCore.DeclareAttack(ctx, 0, 2, 1, 1), RuleCodes.OK, "我方单位打死敌方炮灰");
                Check(SlotOf(ctx, 1, "FixtureEnemyFodder"), -1, "**敌方**炮灰确实死了");
                Check(SlotOf(ctx, 0, "FixtureOwn"), 1, "我方那张还活着 —— 这次死的是**敌方**的");
                Check(SlotOf(ctx, 0, "FixtureOurKiller"), 2, "我方攻击方也活着（没吃反击 —— 目标 0 攻）");
                Check(w.Attack, 2,
                      "★ **敌方**单位死 → 监听「**友方** troop 死」的那张**不动**"
                      + "（极性反了这条就亮：会变成 5）");
            }
        }

        // ---- ③ 部署事件 + 兵种筛选：**筛错的话先炸** ----
        {
            var pool = CardDatabase.Load();
            var listener = new CardDef("FixtureDeployWatcher", "FixtureDeployWatcher", "unit",
                                       "When you deploy a Vehicle, give it +2 Attack",
                                       "common", "Test", 1, 1, 9, 0, null, subtype: "Infantry");
            var tank = new CardDef("FixtureTank2", "FixtureTank2", "unit", "", "common", "Test",
                                   1, 2, 5, 0, null, subtype: "Vehicle");
            var foot = new CardDef("FixtureFoot", "FixtureFoot", "unit", "", "common", "Test",
                                   1, 2, 5, 0, null, subtype: "Infantry");
            var ctx = BattlePool(new[] { listener, tank, foot }, new[] { Unit("EFoe", 1, 1, 9) },
                                 pool, warlordFaction: "Ultramarines");
            ToP1Turn(ctx, 4);
            Place(ctx, 0, 0, listener, exhausted: true);

            // 先部署**步兵** —— 筛的是 Vehicle，不该触发
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "FixtureFoot"), 1), RuleCodes.OK,
                      "部署一个步兵");
            var footU = Board(ctx, 0, 1);
            CheckTrue(footU != null && footU.Attack == 2,
                      "★ 部署**步兵** → 不给加成（筛错兵种这条先亮）");

            // 再部署**载具** —— 该触发
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "FixtureTank2"), 2), RuleCodes.OK,
                      "部署一个载具");
            var tankU = Board(ctx, 0, 2);
            CheckTrue(tankU != null && tankU.Attack == 4,
                      "★ 部署**载具** → 监听器把它 +2 攻（2 → 4）");
            CheckTrue(footU != null && footU.Attack == 2,
                      "先部署的那个步兵**没被回溯补上**（`deploy` 是那一刻的事）");
        }
    }

    ///
    /// <summary>
    /// **事件层的收尾两件 + 递归守卫** —— 2026-09-13 第三十四轮。
    ///
    /// 这两件在第三十二轮就把**数据与解析**写好了，但**没人消费**，形状和本工程踩过的那几次一模一样：
    ///   · `CardDef.CostWhens` 收下了 3 张卡，`EffectResolver` 里**一个调用点都没有** ⇒ 费用从来不降
    ///   · `WhenEventKind.Damaged` 定义了，**一个广播点都没有** ⇒ 2 张卡的监听器一辈子不响
    /// 两边的卡面都**照旧不打 `*`**（正文解析得好好的）—— 正是本工程最怕的那种静默失效。
    /// ⇒ 所以这里每条都钉到「**局面真的变了**」（费用数字 / 攻击数字），**不只看日志**。
    ///
    /// ⚠️ **极性照旧正反各钉一次**（和 <see cref="TestWhenEvents"/> 同一条理由）：
    ///    「该触发的触发了」钉不住筛错 —— 得再钉一条「**不该触发的没动**」。
    ///
    /// ⚠️ 第三件是**递归**：`damaged` 广播接在 `ApplyDamage` 里，而监听器的效果可能**再造成伤害**
    ///    ⇒ 又走一遍 `ApplyDamage` ⇒ 又广播。这条环**天生存在**（规则书 `:237`「同时触发」那节就是讲它的），
    ///    靠 `ctx.EffectChain`（上限 `MaxEffectChain = 8`）截断 ——
    ///    所以必须有一条用例证明「**真的会停，而且链会归零**」，不能只靠读代码相信它。
    /// </summary>
    static void TestEventLayerTail()
    {
        // ---- ① 事件触发式降费：**费用真的降了** ----
        {
            var disc = new CardDef("FixtureCostWhen", "FixtureCostWhen", "tactic",
                                   "Draw a card. Lower cost by 1 when an enemy dies",
                                   "common", "Test", 5, 0, 0, 0, null);
            var fodder = new CardDef("FixtureCostFodder", "FixtureCostFodder", "unit", "",
                                     "common", "Test", 1, 0, 1, 0, null, subtype: "Infantry");
            var ctx = ProbeBattle(new[] { disc }, new[] { fodder });

            Check(disc.CostWhens.Count, 1,
                  "卡表加载时**降费监听被收下来了**（`CardDef.CostWhens`）");

            ToP1Turn(ctx, 2);
            CheckTrue(HandIdx(ctx, 0, "FixtureCostWhen") >= 0, "那张降费卡在 P0 手里");
            var discInst = HandInst(ctx, 0, "FixtureCostWhen");    // 第 7 行第 3 步：降费钉在这一份上
            CheckTrue(discInst != null, "那张降费卡在 P0 手里（按实例取得到）");
            Check(RuleCore.CostOf(ctx, 0, discInst), 5, "动手之前：印的费用 5");

            Place(ctx, 0, 0, Unit("FixtureCostKiller", 1, 5, 5), exhausted: false);
            Place(ctx, 1, 1, fodder, exhausted: true);
            CheckCode(RuleCore.DeclareAttack(ctx, 0, 0, 1, 1), RuleCodes.OK, "打死敌方炮灰");
            Check(SlotOf(ctx, 1, "FixtureCostFodder"), -1, "**敌方**单位确实死了");

            Check(ctx.CostMods.Count, 1, "`ctx.CostMods` 上挂上了 1 条修正（结构上真的登记了）");
            Check(RuleCore.CostOf(ctx, 0, discInst), 4,
                  "★ **事件触发式降费真的生效了**（5 → 4）—— 接 `ctx.CostMods` 之前这条实得 5");
        }

        // ---- ② 反例：**友方**单位死 → 监听「**敌方**死」的那张**不该降** ----
        {
            var disc = new CardDef("FixtureCostWhenEnemy", "FixtureCostWhenEnemy", "tactic",
                                   "Draw a card. Lower cost by 1 when an enemy dies",
                                   "common", "Test", 5, 0, 0, 0, null);
            var ownFodder = new CardDef("FixtureOwnFodder", "FixtureOwnFodder", "unit", "",
                                        "common", "Test", 1, 0, 1, 0, null, subtype: "Infantry");
            var ctx = ProbeBattle(new[] { disc }, new[] { ownFodder });
            ToP1Turn(ctx, 2);
            Place(ctx, 0, 1, ownFodder, exhausted: true);
            Place(ctx, 1, 1, Unit("FixtureEnemyKiller2", 1, 5, 5), exhausted: true);

            PassTurn(ctx);
            Check(ctx.Active, 1, "换到对手的回合");
            CheckCode(RuleCore.DeclareAttack(ctx, 1, 1, 0, 1), RuleCodes.OK, "对手打死我方炮灰");
            Check(SlotOf(ctx, 0, "FixtureOwnFodder"), -1, "**友方**单位确实死了");

            Check(ctx.CostMods.Count, 0, "**一条修正都没挂**（极性反了这条会亮）");
            Check(RuleCore.CostOf(ctx, 0, disc), 5,
                  "★ **友方**死 → 监听「**敌方**死」的那张**不动**（极性反了会变成 4）");
        }

        // ---- ③ `damaged` 广播：**受伤事件真的发出来了** ----
        {
            var watcher = new CardDef("FixtureHurtWatcher", "FixtureHurtWatcher", "unit",
                                      "When a friendly troop receives damage, gain +2 Attack",
                                      "common", "Test", 1, 2, 20, 0, null, subtype: "Infantry");
            var own = new CardDef("FixtureHurtTarget", "FixtureHurtTarget", "unit", "",
                                  "common", "Test", 1, 0, 20, 0, null, subtype: "Infantry");
            var ctx = ProbeBattle(new[] { watcher, own }, new[] { Unit("EFoe", 1, 1, 9) });

            Check(watcher.WhenTriggers.Count, 1, "受伤监听器被收下来了");

            ToP1Turn(ctx, 2);
            var w = Place(ctx, 0, 0, watcher, exhausted: true);
            Place(ctx, 0, 1, own, exhausted: true);
            Place(ctx, 1, 1, Unit("FixtureHurtAttacker", 1, 3, 9), exhausted: true);
            Check(w.Attack, 2, "动手之前：监听者 2 攻");

            PassTurn(ctx);
            Check(ctx.Active, 1, "换到对手的回合");
            CheckCode(RuleCore.DeclareAttack(ctx, 1, 1, 0, 1), RuleCodes.OK,
                      "对手打我方那个 20 血单位");
            Check(SlotOf(ctx, 0, "FixtureHurtTarget"), 1, "挨打的那个还活着（没被这一下打死）");
            Check(w.Attack, 4,
                  "★ **damaged 事件真的广播出来了**（监听者 2 → 4 攻）—— 接上广播之前实得 2");
        }

        // ---- ④ 反例：**敌方**单位受伤 → 监听「**友方**受伤」的那张**不该动** ----
        {
            var watcher = new CardDef("FixtureHurtWatcher2", "FixtureHurtWatcher2", "unit",
                                      "When a friendly troop receives damage, gain +2 Attack",
                                      "common", "Test", 1, 2, 20, 0, null, subtype: "Infantry");
            var ctx = ProbeBattle(new[] { watcher }, new[] { Unit("EFoe", 1, 1, 9) });
            ToP1Turn(ctx, 2);
            var w = Place(ctx, 0, 0, watcher, exhausted: true);
            // ⚠️ 攻击方 9 血、目标 **0 攻**：目标是 0 攻所以不反击，
            //    于是**只有敌方那一个受伤** —— 这一格必须保证这一点，
            //    否则「我方攻击方吃反击」也会广播 `damaged`，这条断言就验不到极性了
            Place(ctx, 0, 1, Unit("FixtureOurHurtKiller", 1, 3, 9), exhausted: false);
            Place(ctx, 1, 1, Unit("FixtureEnemyHurtTarget", 1, 0, 20), exhausted: true);

            CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 1), RuleCodes.OK, "我方打敌方单位");
            Check(SlotOf(ctx, 0, "FixtureOurHurtKiller"), 1, "我方攻击方还活着（没吃反击 —— 目标 0 攻）");
            Check(w.Attack, 2,
                  "★ **敌方**受伤 → 监听「**友方**受伤」的那张**不动**（极性反了会变成 4）");
        }

        // ---- ⑤ 递归守卫：监听器自己再造成伤害 → **必须停得住，而且链要归零** ----
        {
            // 同一个句式两边各挂一张：一边挨打 → 打对面 → 对面挨打 → 打回来 → …
            // ⚠️ **两个单位各 50 血**：这条环是被**链上限**截断的，不是被「有人死了」截断的 ——
            //    血少的话环会因为目标死亡自然停，那就验不到上限了。
            const string echo = "When a friendly troop receives damage, deal 1 damage to a friendly unit";
            var echoA = new CardDef("FixtureEchoA", "FixtureEchoA", "unit", echo,
                                    "common", "Test", 1, 0, 50, 0, null, subtype: "Infantry");
            var extra = new CardDef("FixtureEchoExtra", "FixtureEchoExtra", "unit", "",
                                    "common", "Test", 1, 0, 50, 0, null, subtype: "Infantry");
            var ctx = ProbeBattle(new[] { echoA, extra }, new[] { Unit("EFoe", 1, 1, 9) });

            Check(echoA.WhenTriggers.Count, 1,
                  "★ 自触发的监听器被收下来了 —— **这条是下面几条的前提**："
                  + "正文解析不出来的话，环根本不会建立，那几条会**假通过**");

            ToP1Turn(ctx, 2);
            var a = Place(ctx, 0, 0, echoA, exhausted: true);
            var x = Place(ctx, 0, 1, extra, exhausted: true);
            Place(ctx, 1, 2, Unit("FixtureEchoKiller", 1, 3, 9), exhausted: true);

            PassTurn(ctx);
            Check(ctx.Active, 1, "换到对手的回合");
            // ⚠️ 这一句**必须能返回** —— 不返回就是死循环（下面几条都到不了）
            CheckCode(RuleCore.DeclareAttack(ctx, 1, 2, 0, 0), RuleCodes.OK, "对手打 echoA 一下");

            int lost = (50 - a.Health) + (50 - x.Health);
            CheckTrue(lost >= 3,
                      $"★ **链真的往下跑了几层**（我方两个单位共掉 {lost} 血，攻击本身只贡献 1）");
            CheckTrue(a.Health > 0 && x.Health > 0,
                      "链是被**上限**截断的，不是被「有人死了」截断的（两个都还活着）");
            Check(ctx.EffectChain, 0,
                  "★ **效果链归零了** —— 每一层的 `--` 都执行到了（漏掉会残留或变负）");
        }

        // ---- ⑥ 自指：`When deployed` **只认自己的部署**（2026-09-13 第三十四轮）----
        {
            // 两张**同句式**的卡：B 直接摆到场上（**没走过部署广播**），A 从手牌真打出去。
            // 只有 A 该响 —— 这一格同时钉住「该响的响」和「**旁边的没被误触发**」，
            // 后者才是自指真正的价值所在。
            //
            // ⚠️ **尺子是「监听器响了几次」，不是「数值变了多少」** —— 这是踩了两回才定下来的：
            //    ① 第一版用 `gain +2 Attack` 量，旁观那张也涨了攻，看着像自指失效；
            //       查下去是**既有的、标注过的**行为：`DoGive` 里「无目标的 `gain` 落到**己方全体**」
            //       （`EffectResolver.cs:1945`，注明照 `rule_core.gd:3137`、原版自标为近似）——
            //       `gain` 这类正文**分不清「谁的监听器响了」**，量到的是「有没有效果洒过来」。
            //    ② 第二版改用 `draw a card` 数手牌，结果监听器**根本没注册**（那个写法解析不出，
            //       `AddWhenTrigger` 要求正文解析成功才收）⇒ 断言**假通过**。
            //    ⇒ 现在两件事都钉：**先钉「注册上了」**（否则下面全是假通过），
            //      再用**日志**量响了几次（`BroadcastWhen` 每次都写一行，与目标语义无关）。
            var selfA = new CardDef("FixtureSelfA", "FixtureSelfA", "unit",
                                    "When deployed, gain +1 Attack",
                                    "common", "Test", 1, 1, 5, 0, null, subtype: "Infantry");
            var selfB = new CardDef("FixtureSelfB", "FixtureSelfB", "unit",
                                    "When deployed, gain +1 Attack",
                                    "common", "Test", 1, 1, 5, 0, null, subtype: "Infantry");
            var ctx = ProbeBattle(new[] { selfA }, new[] { Unit("EFoe", 1, 1, 9) });
            ToP1Turn(ctx, 2);

            Check(selfA.WhenTriggers.Count, 1,
                  "★ A 的自指监听器**注册上了**（没注册的话下面两条会**假通过**）");
            Check(selfB.WhenTriggers.Count, 1, "★ B 的也注册上了");

            Place(ctx, 0, 0, selfB, exhausted: true);
            CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "FixtureSelfA"), 1), RuleCodes.OK,
                      "从手牌真打出 A");
            CheckTrue(Board(ctx, 0, 1) != null, "A 上场了（槽 1）");

            Check(WhenFired(ctx, "FixtureSelfA"), 1,
                  "★ **A 的监听器响了恰一次**（它自己的部署）");
            Check(WhenFired(ctx, "FixtureSelfB"), 0,
                  "★ **B 一次都没响** —— 收集成「任何单位部署」的话这里会是 1"
                  + "（那正是「打得比卡面宽」，而卡面不打 `*`，看不出来）");
        }

        // ---- ⑦ 筛选条件的**运行时关键词**：`When an enemy with Hunt Mark dies` ----
        //    ⚠️ 这一格挡的是**两层**静默失效（2026-09-13 第三十四轮，派子代理逐条核 `When` 时查出来的）：
        //      ① 判据只收 `CardDef`（**卡面印的**），而猎杀标记是效果在**运行时** `AddKeyword` 加的
        //         ⇒ 永远判不中；
        //      ② 就算换成 `UnitState`，判据那侧存的还是**卡面原话** `"hunt mark"`（带空格），
        //         卡表那侧存的是规范键 `"huntmark"` ⇒ 还是永远不相等。
        //    两张**已经点亮**的卡（`Stormwolf` / `Wolf Priest`）就是这么「点亮了却一次都不响」的。
        {
            var hunter = new CardDef("FixtureHuntWatch", "FixtureHuntWatch", "unit",
                                     "When an enemy troop with Hunt Mark dies, gain +1 Attack",
                                     "common", "Test", 1, 2, 9, 0, null, subtype: "Infantry");
            var prey = new CardDef("FixturePrey", "FixturePrey", "unit", "",
                                   "common", "Test", 1, 0, 1, 0, null, subtype: "Infantry");
            var ctx = ProbeBattle(new[] { hunter }, new[] { prey });
            ToP1Turn(ctx, 2);
            var w = Place(ctx, 0, 0, hunter, exhausted: true);
            Check(w.Card.WhenTriggers.Count, 1,
                  "监听器注册上了（`… enemy troop with Hunt Mark …` 解析得出，后面才有得判）");

            // 敌方那只**在场上**被打上猎杀标记 —— ⚠️ 它的**卡面没有印这个词**，
            // 是运行时加上去的（效果 `give Hunt Mark to an enemy troop` 就是这么干的）
            var p1 = Place(ctx, 1, 1, prey, exhausted: true);
            p1.AddKeyword("huntmark", 1);
            Place(ctx, 0, 1, Unit("FixtureHuntKiller", 1, 5, 5), exhausted: false);

            int before = w.Attack;
            CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 1), RuleCodes.OK,
                      "打死那个带猎杀标记的敌方部队");
            Check(SlotOf(ctx, 1, "FixturePrey"), -1, "它确实死了");
            Check(w.Attack, before + 1,
                  "★ **带「运行时关键词」的筛选条件真的判中了** —— 卡面没印 `Hunt Mark`，"
                  + $"只按卡面比的话这条会实得 {before}");

            // ---- 反例：同样打死一只敌方部队，但它**没有**猎杀标记 ⇒ **不该响** ----
            var ctx2 = ProbeBattle(new[] { hunter }, new[] { prey });
            ToP1Turn(ctx2, 2);
            var w2 = Place(ctx2, 0, 0, hunter, exhausted: true);
            Place(ctx2, 1, 1, prey, exhausted: true);
            Place(ctx2, 0, 1, Unit("FixtureHuntKiller2", 1, 5, 5), exhausted: false);
            CheckCode(RuleCore.DeclareAttack(ctx2, 0, 1, 1, 1), RuleCodes.OK,
                      "打死一只**没有**猎杀标记的敌方部队");
            Check(w2.Attack, 2,
                  "★ **没标记的就不响** —— 筛选条件被忽略的话这条会实得 3");
        }

        // ---- ⑧ 「关键词被**给予 / 失去**」族：解析层（2026-09-13 第三十四轮）----
        //    这一族和「关键词被**触发**」那族（swarm/synapse/mob/ferocity）**不是一回事**：
        //    那族要等机制本身做完，这族只要**授予/移除这个动作发生**就成立 —— 而动作的发生点
        //    引擎里**早就有**，只是原来没人广播。所以这族每条都只差「词表 + 一行广播」。
        {
            var pHm = WhenEvents.Parse("an enemy gets Hunt Mark");
            CheckTrue(pHm != null, "`an enemy gets Hunt Mark` 认得出");
            if (pHm != null)
            {
                Check(pHm.Kind, WhenEventKind.GetsHuntMark, "事件种类 = gethuntmark");
                Check(pHm.OwnerIs, WhenEvent.RelEnemy, "★ 极性 = **敌方**（给错就是整档反着触发）");
            }

            var pSh = WhenEvents.Parse("you gain [Shield]");
            CheckTrue(pSh != null, "`you gain [Shield]` 认得出（**方括号要先被去掉**）");
            if (pSh != null)
            {
                Check(pSh.Kind, WhenEventKind.GetsShield, "事件种类 = getsshield");
                Check(pSh.OwnerIs, WhenEvent.RelFriendly, "极性 = 本方（`you`）");
            }
            var pSh2 = WhenEvents.Parse("a friendly unit obtains [Shield]");
            CheckTrue(pSh2 != null, "`a friendly unit obtains [Shield]` 认得出（同族的另一种写法）");
            if (pSh2 != null) Check(pSh2.Kind, WhenEventKind.GetsShield, "事件种类 = getsshield");

            var pSt = WhenEvents.Parse("an enemy receives a Stun");
            CheckTrue(pSt != null, "`an enemy receives a Stun` 认得出");
            if (pSt != null)
            {
                Check(pSt.Kind, WhenEventKind.GetsStun, "事件种类 = getsstun");
                Check(pSt.OwnerIs, WhenEvent.RelEnemy, "极性 = 敌方");
            }

            var pLs = WhenEvents.Parse("a friendly unit loses Stealth");
            CheckTrue(pLs != null, "`a friendly unit loses Stealth` 认得出");
            if (pLs != null) Check(pLs.Kind, WhenEventKind.LosesStealth, "事件种类 = losestealth");

            // ⚠️ **反例**：别顺手把「关键词被**触发**」那族也收进来 ——
            //    只有**真有触发时机**的词才该收（收了 = 注册一条没人消费的监听器，红线）。
            //    ⚠️ 2026-09-14：`destroyer` **已经实现**了（目标选择限制），但它
            //    **没有任何触发点**（不会有人广播「某单位触发了毁灭者」）⇒ 这条**仍然必须认不出**。
            //    判据不是 `Implemented`，是 `KeywordTable.HasTriggerMoment`。
            CheckTrue(WhenEvents.Parse("a friendly unit triggers destroyer") == null,
                      "★ `triggers destroyer` **仍然认不出** —— `destroyer` 虽已实现，"
                      + "但它是**目标选择限制**、没有任何触发广播点；收下来就是一条**永远不响**的监听器");
            CheckTrue(WhenEvents.Parse("this unit triggers synapse") != null,
                      "★ `this unit triggers synapse` **认得出**了（`synapse` A2 已实现，广播真的会发）");
        }

        // ---- ⑨ 四条广播**真的发出来了**（尺子用 `WhenFired`：数「这张卡的监听器响了几次」）----
        {
            // ① 失去潜行 —— 发生点：`DeclareAttack` 里攻击之后的 `RemoveKeyword(Stealth)`
            {
                var watch = new CardDef("FixtureStealthWatch", "FixtureStealthWatch", "unit",
                                        "When a friendly unit loses Stealth, gain +1 Attack",
                                        "common", "Test", 1, 2, 9, 0, null, subtype: "Infantry");
                var sneaky = new CardDef("FixtureSneaky", "FixtureSneaky", "unit", "",
                                         "common", "Test", 1, 3, 9, 0, new[] { "Stealth" },
                                         subtype: "Infantry");
                var ctx = ProbeBattle(new[] { watch, sneaky }, new[] { Unit("EFoe", 1, 0, 9) });
                ToP1Turn(ctx, 2);
                Place(ctx, 0, 0, watch, exhausted: true);
                var sk = Place(ctx, 0, 1, sneaky, exhausted: false);
                Place(ctx, 1, 1, Unit("FixtureStealthTarget", 1, 0, 9), exhausted: true);
                CheckTrue(sk.Has("stealth"), "它一开始是会潜行的");
                CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 1), RuleCodes.OK, "潜行单位出手");
                CheckTrue(!sk.Has("stealth"), "出手之后潜行没了");
                Check(WhenFired(ctx, "FixtureStealthWatch"), 1,
                      "★ **`loses stealth` 广播出来了** —— 摘掉潜行的那一处原来没广播");
            }

            // ② 被眩晕 —— 发生点：`DoStun` 的 `IsStunned = true`
            {
                var watch = new CardDef("FixtureStunWatch", "FixtureStunWatch", "unit",
                                        "When an enemy receives a Stun, gain +1 Attack",
                                        "common", "Test", 1, 2, 9, 0, null, subtype: "Infantry");
                var stunTac = Tactic("T_Stun", 0, "Stun an enemy");
                var ctx = ProbeBattle(new[] { watch, stunTac }, new[] { Unit("EFoe", 1, 1, 9) });
                ToP1Turn(ctx, 2);
                Place(ctx, 0, 0, watch, exhausted: true);
                var victim = Place(ctx, 1, 1, Unit("FixtureStunVictim", 1, 0, 9), exhausted: true);
                CheckTrue(!victim.IsStunned, "动手之前没被晕");
                CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Stun"), 1), RuleCodes.OK,
                          "打出眩晕");
                CheckTrue(victim.IsStunned, "它被晕了");
                Check(WhenFired(ctx, "FixtureStunWatch"), 1, "★ **`receives stun` 广播出来了**");
            }

            // ③ 被给予猎杀标记 —— 发生点：`ApplyOneGain` 的 `AddKeyword("huntmark")`
            {
                var watch = new CardDef("FixtureHmWatch", "FixtureHmWatch", "unit",
                                        "When an enemy gets Hunt Mark, gain +1 Attack",
                                        "common", "Test", 1, 2, 9, 0, null, subtype: "Infantry");
                var hmTac = Tactic("T_HM", 0, "Give Hunt Mark to an enemy troop");
                var ctx = ProbeBattle(new[] { watch, hmTac }, new[] { Unit("EFoe", 1, 1, 9) });
                ToP1Turn(ctx, 2);
                Place(ctx, 0, 0, watch, exhausted: true);
                var prey = Place(ctx, 1, 1, Unit("FixtureHmPrey", 1, 0, 9), exhausted: true);
                CheckTrue(!prey.Has("huntmark"), "动手之前它没有标记");
                CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_HM"), 1), RuleCodes.OK,
                          "给敌方上猎杀标记");
                CheckTrue(prey.Has("huntmark"), "标记确实上去了");
                Check(WhenFired(ctx, "FixtureHmWatch"), 1,
                      "★ **`gets hunt mark` 广播出来了** —— 授予点原来没广播");
            }

            // ④ 获得护盾 —— 发生点同上（`AddKeyword("shield")`）
            {
                var watch = new CardDef("FixtureShieldWatch", "FixtureShieldWatch", "unit",
                                        "When you gain [Shield], gain +1 Attack",
                                        "common", "Test", 1, 2, 9, 0, null, subtype: "Infantry");
                var shTac = Tactic("T_Shield", 0, "Give Shield to a friendly unit");
                var ctx = ProbeBattle(new[] { watch, shTac }, new[] { Unit("EFoe", 1, 1, 9) });
                ToP1Turn(ctx, 2);
                var w = Place(ctx, 0, 0, watch, exhausted: true);
                CheckTrue(!w.HasShield, "动手之前它没有盾");
                // ⚠️ 目标槽**必须填 0**（= watch 所在的格）—— 这是「给**友方**」的战术卡，
                //    `PlayTactic` 会把 `targetSlot` 解释成**本方**的格位，填 1 会得到
                //    `ErrSlot`（槽 1 是空的）。实测踩过：三条断言一起红。
                CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Shield"), 0), RuleCodes.OK,
                          "给友方上盾");
                CheckTrue(w.HasShield, "盾确实上去了");
                Check(WhenFired(ctx, "FixtureShieldWatch"), 1,
                      "★ **`gain [Shield]` 广播出来了**");
            }
        }

        // ---- ⑩ 造「隐秘 / 破坏」与「再造残骸」：解析层（2026-09-13 第三十四轮）----
        {
            var pSec = WhenEvents.Parse("you create a Secret");
            CheckTrue(pSec != null, "`you create a Secret` 认得出");
            if (pSec != null)
            {
                Check(pSec.Kind, WhenEventKind.CreatesSecret, "事件种类 = createsecret");
                Check(pSec.OwnerIs, WhenEvent.RelFriendly, "极性 = 本方");
            }
            var pSab = WhenEvents.Parse("you create a Sabotage");
            CheckTrue(pSab != null, "`you create a Sabotage` 认得出");
            if (pSab != null) Check(pSab.Kind, WhenEventKind.CreatesSabotage, "事件种类 = createsabotage");

            // ⚠️ **2026-09-13 A3：这条断言反过来了**。第三十四轮这里是「故意仍判认不出」——
            //    因为当时 `Parse` 的签名只回**一条**事件，而这句话要两条，
            //    只接半边就是「漏触发且不报错」（卡面照旧不打 `*`）。
            //    现在 <see cref="WhenEvents.ParseAll"/> 支持**多条**，于是改钉：
            //    ① 拆得成**两条**（顺序照卡面：create → play）；
            //    ② 两半**各自**是对的（种类 + 宾语筛选），不是「两条都收到同一条事件」。
            var pOr = WhenEvents.ParseAll("you create or play a secret");
            Check(pOr.Count, 2, "★ `create **or play** a secret` 拆成**两条**事件（A3 起收得下了）");
            if (pOr.Count == 2)
            {
                Check(pOr[0].Kind, WhenEventKind.CreatesSecret, "① = `createsecret`（照卡面先说 create）");
                Check(pOr[1].Kind, WhenEventKind.Play, "② = `play`");
                CheckTrue(pOr[1].Criteria != null && pOr[1].Criteria.KindWord == "secret",
                          "★ ② 的宾语筛选 = 兵种/牌类词 `secret`（`subtype = \"Secret\"`）—— "
                          + "不筛的话「对手打出任意一张战术卡」都会触发");
                Check(pOr[1].OwnerIs, WhenEvent.RelFriendly, "极性 = 本方（`you`）");
            }
            // ⚠️ **反例**：只认得出**一半**时**整条不收**（返回空表）——
            //    这就是原来那条顾虑的正确落点：宁可整条认不出（卡面打 `*`），也别只接半边。
            //    `play a secret` 认得出，`frobnicate a secret` 认不出 ⇒ 整条作废。
            Check(WhenEvents.ParseAll("you frobnicate or play a secret").Count, 0,
                  "★ 只认得出**一半**（`or` 的左边认不出）⇒ **整条不收**，不是「接能认的那半」");

            // `When you reanimate a Remnant`（`Diviner`）—— 和下面那条**只差一个字母、语义相反**
            var pRe = WhenEvents.Parse("you reanimate a Remnant");
            CheckTrue(pRe != null, "`you reanimate a Remnant` 认得出");
            if (pRe != null)
            {
                Check(pRe.Kind, WhenEventKind.Reanimated, "事件种类复用 = reanimated（**同一处广播**）");
                CheckTrue(!pRe.SelfOnly,
                          "★ **它没有 `SelfOnly`** —— 卡面说的是「**你这一方**做了再造」，"
                          + "而监听者（督军 `Diviner`）**自己并没有被再造**。带上自指就永远不响");
                Check(pRe.OwnerIs, WhenEvent.RelFriendly, "极性 = 本方");
            }
            var pRe2 = WhenEvents.Parse("reanimated");
            CheckTrue(pRe2 != null && pRe2.SelfOnly,
                      "★ 对照：省主语的 `When Reanimated` **必须**带自指（两条只差一个字母，别合并）");
        }

        // ---- ⑪ 再造残骸的广播真的发出来（端到端，且**监听者不是被再造的那个**）----
        {
            var watch = new CardDef("FixtureRemnantWatch", "FixtureRemnantWatch", "unit",
                                    "When you reanimate a Remnant, gain +1 Attack",
                                    "common", "Test", 1, 2, 9, 0, null, subtype: "Infantry");
            var kill = Tactic("T_KillR", 0, "Deal 99 damage to a friendly unit");
            var bring = Tactic("T_ReR", 0, "Reanimate a friendly Remnant");
            var fodder = new CardDef("FixtureRemnantFodder", "FixtureRemnantFodder", "unit", "",
                                     "common", "Test", 1, 1, 1, 0, null, subtype: "Infantry");
            var ctx = ProbeBattle(new[] { watch, kill, bring }, new[] { Unit("EFoe", 1, 1, 9) });
            ToP1Turn(ctx, 2);
            Place(ctx, 0, 0, watch, exhausted: true);
            Place(ctx, 0, 1, fodder, exhausted: true);       // 待会儿打死它 = 墓地里那张「残骸」
            Check(WhenFired(ctx, "FixtureRemnantWatch"), 0, "动手之前它没响过");
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_KillR"), 1), RuleCodes.OK,
                      "先打死一个友方单位（进墓地 = 残骸）");
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_ReR"), -1), RuleCodes.OK,
                      "再造一个残骸");
            Check(WhenFired(ctx, "FixtureRemnantWatch"), 1,
                  "★ **`you reanimate a remnant` 广播出来了** —— 注意**监听者自己没被再造**，"
                  + "这正是这条不能带自指的原因（带上就永远不响）");
        }

        // ---- ⑫ 造「破坏」的广播真的发出来（端到端）----
        //   `Create a random Sabotage in the enemy hand` 是**真卡面写法**（Genestealers 一族 10 张在用）。
        //   ⚠️ 必须用 `BattlePool`（**带真卡池**）而不是 `ProbeBattle` —— 后者的池是**空的**，
        //      造牌从空池里挑不出东西，`DoCreate` 会直接判失败。
        {
            var watch = new CardDef("FixtureSabWatch", "FixtureSabWatch", "unit",
                                    "When you create a Sabotage, gain +1 Attack",
                                    "common", "Test", 1, 2, 9, 0, null, subtype: "Infantry");
            var mk = Tactic("T_Sab", 0, "Create a random Sabotage in the enemy hand");
            var ctx = BattlePool(new[] { watch, mk }, new[] { Unit("EFoe", 1, 1, 9) },
                                 CardDatabase.Load(), warlordFaction: "Genestealers");
            ToP1Turn(ctx, 2);
            Place(ctx, 0, 0, watch, exhausted: true);
            Check(WhenFired(ctx, "FixtureSabWatch"), 0, "动手之前它没响过");
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Sab"), -1), RuleCodes.OK,
                      "造一张破坏（进**敌方**手牌）");
            Check(WhenFired(ctx, "FixtureSabWatch"), 1,
                  "★ **`create a sabotage` 广播出来了** —— 注意事件归属传的是**造牌方**，"
                  + "不是「牌落到谁手里」（破坏恰恰是进**敌方**手牌的，传错就是对方的监听器响）");
        }

        // ==================================================================
        //  A3（2026-09-13）：`When` 短语剩下的 4 条
        // ==================================================================
        //   ① `this unit attacks an enemy with Hunt Mark`（`Long Fang`）
        //   ② `this unit kills an enemy`（`Sisters Repentia`）
        //   ③ `you create **or play** a secret`（`Ravenwing Ballistus Dreadnought`，见上面那条断言）
        //   ④ `you trigger a Spirit Stone ability`（`Bright Lance Vyper`）
        // 四条原本都落在「认不出的事件短语」那一栏（`_tmp_view/when_unparsed.md`）。
        // 这一节钉**两层**：解析层（事件长什么样）+ 端到端（局面真的变了 + **反例**）。

        // ---- ⑬ 解析层 ----
        {
            var atk = WhenEvents.Parse("this unit attacks an enemy with Hunt Mark");
            CheckTrue(atk != null,
                      "★ `this unit attacks an enemy with Hunt Mark` 认得出（A3 之前落在「认不出」）");
            if (atk != null)
            {
                Check(atk.Kind, WhenEventKind.Attack, "事件种类 = attack");
                CheckTrue(atk.ActorSelf,
                          "★ 自指记在 **`ActorSelf`**（这件事是监听者**干的**）—— 不是 `SelfOnly`"
                          + "（那条要求「被打的**是**监听者自己」，在这里永远为假，而且**静默**）");
                Check(atk.TargetOwnerIs, WhenEvent.RelEnemy, "宾语的归属 = 敌方");
                CheckTrue(atk.TargetCriteria != null && atk.TargetCriteria.Keyword == "hunt mark",
                          "★ 宾语的筛选 = 关键词 `hunt mark`（存卡面原话，比对时归一成 `huntmark`）");
            }

            var kill = WhenEvents.Parse("this unit kills an enemy");
            CheckTrue(kill != null, "★ `this unit kills an enemy` 认得出");
            if (kill != null)
            {
                Check(kill.Kind, WhenEventKind.Kills,
                      "事件种类 = kills —— **和 `attack`（打了）、`die`（死了）都是两回事**");
                CheckTrue(kill.ActorSelf, "自指同上：凶手必须**是它自己**");
                Check(kill.OwnerIs, WhenEvent.RelEnemy, "极性 = 敌方（被杀的必须是敌方的）");
            }

            var spAa = WhenEvents.Parse("you trigger a Spirit Stone ability");
            CheckTrue(spAa != null, "★ `you trigger a Spirit Stone ability` 认得出");
            if (spAa != null)
            {
                Check(spAa.Kind, WhenEventKind.SpiritAbility, "事件种类 = spiritability");
                Check(spAa.OwnerIs, WhenEvent.RelFriendly, "极性 = 本方（卡面写 `you`）");
            }
        }

        // ---- ⑬·二 **真卡**（不是夹具）：四张卡的监听器都注册上了，而且锚点是对的 ----
        //   夹具证明的是**机制**，这里证明**卡面文字**也对得上 —— 两件事会各自单独坏
        //   （夹具全绿、真卡一条都没接上，正是本工程最怕的那种「测试骗人」）。
        {
            var pool = CardDatabase.Load();

            var lf = CardDatabase.Find(pool, "Long Fang");
            CheckTrue(lf != null && lf.WhenTriggers.Count == 1,
                      "★ 真卡 `Long Fang`：监听器注册上了（`When this unit attacks an enemy with Hunt Mark, …`）");
            var lfAnchor = AdjacentAnchor.Unset;
            if (lf != null && lf.WhenTriggers.Count == 1)
                foreach (var o in lf.WhenTriggers[0].Ops)
                    if (o.Target != null && o.Target.Adjacent) lfAnchor = o.Target.Anchor;
            Check(lfAnchor, AdjacentAnchor.TargetIfMeetsCriteria,
                  "★ 真卡的相邻锚点 = **事件宾语**（`TargetIfMeetsCriteria`）—— 落成 `Self` 就是"
                  + "打在长牙**自己那一排**上（静默错打）；落成 `AdjacentFailed` 则整句降级");

            var sr = CardDatabase.Find(pool, "Sisters Repentia");
            CheckTrue(sr != null && sr.WhenTriggers.Count == 1,
                      "★ 真卡 `Sisters Repentia`：监听器注册上了（`When this unit kills an enemy, gain 1 ☀`）");

            var rb = CardDatabase.Find(pool, "Ravenwing Ballistus Dreadnought");
            CheckTrue(rb != null && rb.WhenTriggers.Count == 2,
                      "★ 真卡 `Ravenwing Ballistus Dreadnought`：**两条**监听器（create + play）——"
                      + " 只接半边时这里会是 1，而那正是「漏触发且不报错」");

            var bl = CardDatabase.Find(pool, "Bright Lance Vyper");
            CheckTrue(bl != null && bl.WhenTriggers.Count == 1,
                      "★ 真卡 `Bright Lance Vyper`：监听器注册上了（`you trigger a Spirit Stone ability`）");
            if (bl != null && bl.WhenTriggers.Count == 1 && bl.WhenTriggers[0].Ops.Count > 0)
                CheckTrue(bl.WhenTriggers[0].Ops[0].Payload.ToLowerInvariant().Contains("sniper"),
                          "★ 正文是**卡图核出来的那份**（`gain Sniper and +1 Ranged Attack`）—— "
                          + "卡表里原来写 `gain +1 Attack`，那是 OCR 把**图标**猜成了单词，"
                          + "见 `数据/游戏数据/cardface_fixes.json` 的 `_manual_desc_note`");
        }

        // ---- ⑭ 端到端：`Long Fang` 形状 —— **打的敌人带标记**才响，且锚点 = **被打的那个** ----
        //   这一格同时钉三个静默错打的入口，每一个**都能单独把这条断言打红**：
        //     ① 不筛宾语（打得比卡面宽）⇒ 打没标记的敌人也响；
        //     ② 锚点退化成「自己」（`FillAdjacentAnchors` 的 ④）⇒ 打在**自己**的邻居上；
        //     ③ 锚点退化成「整个目标池」（A1 明令禁止的兜底）⇒ 隔一格的那个也挨。
        {
            var longFang = new CardDef("FixtureLongFang", "FixtureLongFang", "unit",
                "When this unit attacks an enemy with Hunt Mark, deal 3 damage to adjacent enemies",
                "common", "Test", 4, 1, 9, 0, null, subtype: "Infantry");
            var marked = new CardDef("FixtureMarkedPrey", "FixtureMarkedPrey", "unit", "",
                                     "common", "Test", 1, 0, 9, 0, null, subtype: "Infantry");

            // ---- ⑭-a 打**带**猎杀标记的敌人 ⇒ 它的相邻敌人挨 3 ----
            {
                var ctx = ProbeBattle(new[] { longFang }, new[] { marked });
                ToP1Turn(ctx, 2);
                var lf = Place(ctx, 0, 0, longFang, exhausted: false);
                var own = Place(ctx, 0, 1, Unit("FixtureOwnNeighbour", 1, 0, 9), exhausted: true);
                // 打 2 号格 ⇒ 相邻格是 {1, 3}（同一方行内左右紧邻，`BoardSpec.AdjacentSlots`）
                var tgt = Place(ctx, 1, 2, marked, exhausted: true);
                tgt.AddKeyword("huntmark", 1);
                var neigh = Place(ctx, 1, 1, Unit("FixtureEnemyNeighbour", 1, 0, 9), exhausted: true);
                // ⚠️ 「不相邻」的那只需要隔**两格**：2 号的相邻是 {1,3} —— 放在 0 号才是隔一格的
                //    （第一版放在 3 号，那**正是**相邻格，于是断言红了一次而**实现是对的**）。
                var far = Place(ctx, 1, 0, Unit("FixtureEnemyFar", 1, 0, 9), exhausted: true);

                CheckCode(RuleCore.DeclareAttack(ctx, 0, 0, 1, 2), RuleCodes.OK,
                          "长牙攻击那个带猎杀标记的敌人");
                Check(neigh.Health, 6,
                      "★ **被打者的相邻敌人挨了 3** —— 锚点是**被打的那个**，不是长牙自己"
                      + "（那种情况下它自己这边全是友军、一个敌方邻居都挑不出来 ⇒ 实得 9）");
                Check(lf.Health, 9, "长牙自己没挨（锚点错成「自己」时这一条会红）");
                Check(own.Health, 9, "自己这边的邻居也没挨（同上）");
                Check(far.Health, 9, "★ 隔一格的那个**不挨** —— 退化成「整个目标池」时这一条会红");
            }

            // ---- ⑭-b 反例：打**没标记**的敌人 ⇒ 一次都不响 ----
            {
                var ctx = ProbeBattle(new[] { longFang }, new[] { marked });
                ToP1Turn(ctx, 2);
                Place(ctx, 0, 0, longFang, exhausted: false);
                Place(ctx, 1, 2, marked, exhausted: true);            // **不加** huntmark
                var neigh = Place(ctx, 1, 1, Unit("FixtureEnemyNeighbour2", 1, 0, 9), exhausted: true);
                CheckCode(RuleCore.DeclareAttack(ctx, 0, 0, 1, 2), RuleCodes.OK, "打一个没有标记的敌人");
                Check(neigh.Health, 9,
                      "★ **没标记就不响** —— 宾语筛选被忽略的话这里会实得 6");
            }

            // ---- ⑭-c 反例：**别人**打那个带标记的敌人 ⇒ 长牙不响（`ActorSelf`）----
            {
                var ctx = ProbeBattle(new[] { longFang }, new[] { marked });
                ToP1Turn(ctx, 2);
                Place(ctx, 0, 0, longFang, exhausted: true);
                Place(ctx, 0, 1, Unit("FixtureOtherAttacker", 1, 1, 9), exhausted: false);
                var tgt = Place(ctx, 1, 2, marked, exhausted: true);
                tgt.AddKeyword("huntmark", 1);
                var neigh = Place(ctx, 1, 1, Unit("FixtureEnemyNeighbour3", 1, 0, 9), exhausted: true);
                CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 2), RuleCodes.OK,
                          "**另一个**友方单位打那个带标记的敌人");
                Check(neigh.Health, 9,
                      "★ **卡面写的是 `this unit`** ⇒ 别人打的**不算** —— 只收「任何友方攻击」时这里会实得 6");
            }
        }

        // ---- ⑮ 端到端：**`the target of the attack` 指被打的那个**（`Valtus` 形状）----
        //   🔴 这一格修的是**一条真的静默错打**（A3 顺手查出来的）：
        //      这句宾语短语**不在 `IsPronoun` 那一族**里，于是 `ParseTarget` 把它当成了
        //      「一个任意单位」（`Side = any` / `Kind = any`）⇒ 从**全场**挑一个 ——
        //      实测挑中的是**自己这边最左边那个**。而句子解析得干干净净、**卡面不打 `*`**。
        //      现在它有了自己的指代（事件宾语，`ctx.EventTarget`，原版 `TargetsAffected.target = 30`）。
        {
            var valtus = new CardDef("FixtureValtus", "FixtureValtus", "unit",
                "When a friendly unit attacks, deal 3 damage to the target of the attack",
                "common", "Test", 9, 0, 9, 0, null, subtype: "Infantry");
            var prey = new CardDef("FixtureValtusPrey", "FixtureValtusPrey", "unit", "",
                                   "common", "Test", 1, 0, 9, 0, null, subtype: "Infantry");
            var ctx = ProbeBattle(new[] { valtus }, new[] { prey });
            ToP1Turn(ctx, 2);
            var v = Place(ctx, 0, 0, valtus, exhausted: true);
            var atk = Place(ctx, 0, 1, Unit("FixtureValtusAttacker", 1, 1, 9), exhausted: false);
            var tgt = Place(ctx, 1, 2, prey, exhausted: true);   // 0 攻 ⇒ 不反击，账好算
            Check(v.Card.WhenTriggers.Count, 1, "监听器注册上了");

            CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 2), RuleCodes.OK, "友方单位攻击");
            Check(tgt.Health, 5,
                  "★ **被打的那个**挨 1（攻击）+ 3（监听器的效果）—— 实得 9 说明效果空过、"
                  + "实得 8 说明只算了攻击");
            Check(atk.Health, 9,
                  "★ **攻击者自己没挨那 3 点** —— 修之前 `the target of the attack` 被当成「任意一个单位」，"
                  + "挑中的就是自己这边的单位");
            Check(v.Health, 9, "监听者也没挨");
        }

        // ---- ⑯ 端到端：`Sisters Repentia` 形状（**击杀**，不是「攻击」）----
        {
            var repentia = new CardDef("FixtureRepentia", "FixtureRepentia", "unit",
                "When this unit kills an enemy, gain 1 ☀",
                "common", "Test", 3, 3, 9, 0, null, subtype: "Infantry");
            var small = new CardDef("FixtureSmallPrey", "FixtureSmallPrey", "unit", "",
                                    "common", "Test", 1, 0, 2, 0, null, subtype: "Infantry");
            var tough = new CardDef("FixtureToughPrey", "FixtureToughPrey", "unit", "",
                                    "common", "Test", 1, 0, 9, 0, null, subtype: "Infantry");

            // ---- ⑯-a 打死一个 ⇒ 信仰 +1 ----
            {
                var ctx = ProbeBattle(new[] { repentia }, new[] { small });
                ToP1Turn(ctx, 2);
                Place(ctx, 0, 0, repentia, exhausted: false);
                Place(ctx, 1, 2, small, exhausted: true);
                Check(ctx.Players[0].Faith, 0, "动手之前信仰是 0");
                CheckCode(RuleCore.DeclareAttack(ctx, 0, 0, 1, 2), RuleCodes.OK, "一击打死敌方单位");
                Check(SlotOf(ctx, 1, "FixtureSmallPrey"), -1, "它确实死了");
                Check(ctx.Players[0].Faith, 1,
                      "★ **击杀广播发出来了、正文也结算了**（信仰 0 → 1）—— 只钉「认得出」的话，"
                      + "广播没接上时这一条照样是 0");
            }

            // ---- ⑯-b 反例：打了但**没打死** ⇒ 不该响 ----
            {
                var ctx = ProbeBattle(new[] { repentia }, new[] { tough });
                ToP1Turn(ctx, 2);
                Place(ctx, 0, 0, repentia, exhausted: false);
                Place(ctx, 1, 2, tough, exhausted: true);          // 9 血，3 攻一下打不死
                CheckCode(RuleCore.DeclareAttack(ctx, 0, 0, 1, 2), RuleCodes.OK, "打一下但打不死");
                CheckTrue(Board(ctx, 1, 2) != null && Board(ctx, 1, 2).IsAlive, "它活着");
                Check(ctx.Players[0].Faith, 0,
                      "★ **打不死就不算击杀** —— 把 `kills` 退化成 `attack` 时这一条会红");
            }

            // ---- ⑯-c 反例：**别人**打死的不算（`ActorSelf`）----
            {
                var ctx = ProbeBattle(new[] { repentia }, new[] { small });
                ToP1Turn(ctx, 2);
                Place(ctx, 0, 0, repentia, exhausted: true);
                Place(ctx, 0, 1, Unit("FixtureOtherKiller", 1, 5, 5), exhausted: false);
                Place(ctx, 1, 2, small, exhausted: true);
                CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 2), RuleCodes.OK,
                          "**另一个**友方单位把它打死");
                Check(SlotOf(ctx, 1, "FixtureSmallPrey"), -1, "它确实死了");
                Check(ctx.Players[0].Faith, 0,
                      "★ **卡面写的是 `this unit`** ⇒ 别人杀的不算 —— 按「任何单位死」收时这一条会红");
            }
        }
    }

    /// <summary>
    /// 数「**这张卡的监听器响过几次**」—— 数 `ctx.Events` 里 <see cref="RuleCore.BroadcastWhen"/>
    /// 写的那行（`—— 事件「x」触发：「名字」的监听器 → …`）。
    ///
    /// 为什么不用「数值变了多少」当尺子：`gain` 这类**无目标的正文**在 `DoGive` 里会落到**己方全体**
    /// （`EffectResolver.cs:1945`，照 `rule_core.gd:3137`、原版自标为近似）——
    /// 于是「谁响的」和「谁被加了」是两件事，数值差分不出来（实测踩过）。
    /// 日志量的是**触发次数**本身，和目标语义无关。
    /// </summary>
    static int WhenFired(BattleContext ctx, string cardName)
    {
        int n = 0;
        string needle = "「" + cardName + "」的监听器";
        foreach (var line in ctx.Events)
            if (!string.IsNullOrEmpty(line) && line.Contains(needle)) n++;
        return n;
    }

    ///
    /// <summary>
    /// **「未实现关键词」名单不许误报** —— 2026-09-13 第三十四轮。
    ///
    /// 派子代理逐条核了那 23 个「未实现」关键词的代码，查出 **`codex` / `oath` / `stun`** 三个
    /// **其实早就有机制**，只是没登记进 `KeywordTable.Implemented` ——
    /// 结果名单多报 3 个，**卡面还会白打 `*`**（玩家看到的和实际能力对不上）。
    ///
    /// 这条把**两半都钉住**：已登记的必须真的认，**真没做的必须仍然被报出来** ——
    /// 只钉前一半的话，把名单压小最省事的办法就是「什么都登记进去」，那是更糟的错。
    /// </summary>
    static void TestKeywordImplementedList()
    {
        var un = RuleCore.UnimplementedKeywords(CardDatabase.Load());

        // ① 这几个**不许**再出现在名单上（机制一直在，只是漏登记 / 后来做掉了）
        //    ⚠️ **2026-09-13 A2 起加人**：`artifice` / `duty` / `pray` / `ferocity` / `agenda`
        //    是那一轮真做掉的（做掉之后必须从名单上下来，否则卡面白打 `*`）。
        foreach (var kw in new[] { "codex", "oath", "stun", "mob", "regiment",
                                   "artifice", "duty", "pray", "ferocity", "agenda", "swarm", "synapse" })
            CheckTrue(!un.Contains(kw),
                      $"★ `{kw}` **不在「未实现」名单上** —— 机制一直在（或已做掉），"
                      + "漏登记会让名单误报、卡面白打 `*`");

        // ② **2026-09-14：这三个已经真做掉了**（用户点名要求）—— 必须从名单上下来，
        //    否则卡面白打 `*`、报表也一直误报。判据 = 代码在那个时机真的读了它：
        //      · `destroyer` → `RuleCore.IsValidTarget`（与 Vanguard 同构的目标硬约束）
        //      · `ecstasy`   → `RuleCore.Hurt`（生命降至 X 或以下未死，一辈子一次）
        //      · `sabotage`  → 机制一直在跑（造牌 / 手牌陷阱），只是判据走 `subtype` 那一列
        foreach (var kw in new[] { "destroyer", "ecstasy", "sabotage" })
            CheckTrue(!un.Contains(kw),
                      $"★ `{kw}` **不在「未实现」名单上** —— 它 2026-09-14 真做掉了，"
                      + "还挂在名单上会让卡面白打 `*`、诊断单误报");
    }

    ///
    /// <summary>
    /// **群体（Mob）与团（Regiment）** —— 2026-09-13 第三十四轮。**成对的两条**：
    /// 规则书 `:193`「友方部队执行**近战**攻击后触发效果」 / `:202`「友方单位执行**远程**攻击时触发效果」。
    ///
    /// 实测卡面两条同形，都是 `Xxx: &lt;效果&gt;` 挂在**自己**身上
    /// （`Skarboy Nob` = `Mob: Gain +1 Attack` · `Kasrkin` = `Regiment: Deal 1 damage to a random enemy…`），
    /// 所以语义 = **这张卡出手打人之后做一件事**。它们和 <see cref="KeywordTable.Strike"/> 同形，
    /// **唯一的差别就是近战 / 远程** —— 所以每一条都必须**正反各钉一次**：
    ///   近战那条：近战 → 该触发；**远程 → 不该触发**。
    ///   远程那条：远程 → 该触发；**近战 → 不该触发**。
    /// 漏掉任何一个反例，「把 `!ranged` 写成 `ranged`」这类错就抓不到。
    /// </summary>
    static void TestMobAndRegimentKeyword()
    {
        var mobber = new CardDef("FixtureMobber", "FixtureMobber", "unit",
                                 "Mob: Gain +1 Attack",
                                 "common", "Test", 1, 2, 9, 0, null, subtype: "Infantry");
        CheckTrue(mobber.TriggerOps("mob") != null,
                  "★ `Mob: Gain +1 Attack` 的**正文被收下来了** —— "
                  + "`RoutableTriggers` 没加它的话这里就是 null（正文收了没人消费 = 静默失效）");

        // ---- ① 近战攻击 → **该**触发 ----
        {
            var ctx = ProbeBattle(new[] { mobber }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            var m = Place(ctx, 0, 0, mobber, exhausted: false);
            Place(ctx, 1, 1, Unit("FixtureMobTarget", 1, 0, 9), exhausted: true);
            Check(m.Attack, 2, "动手之前 2 攻");
            CheckCode(RuleCore.DeclareAttack(ctx, 0, 0, 1, 1), RuleCodes.OK, "**近战**打一下");
            Check(m.Attack, 3, "★ **近战攻击 → `Mob:` 触发**（2 → 3 攻）");
        }

        // ---- ② 远程攻击 → mob **不该**触发（这是 Mob 和 Strike 唯一的差别）----
        {
            var shooter = new CardDef("FixtureMobShooter", "FixtureMobShooter", "unit",
                                      "Mob: Gain +1 Attack",
                                      "common", "Test", 1, 2, 9, 3, null, subtype: "Infantry");
            var ctx = ProbeBattle(new[] { shooter }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            var s = Place(ctx, 0, 0, shooter, exhausted: false);
            Place(ctx, 1, 1, Unit("FixtureMobTarget2", 1, 0, 9), exhausted: true);
            CheckCode(RuleCore.DeclareAttack(ctx, 0, 0, 1, 1, ranged: true), RuleCodes.OK,
                      "**远程**打一下");
            Check(s.Attack, 2,
                  "★ **远程攻击 → `Mob:` 不触发**（还是 2 攻）—— 漏掉 `!ranged` 这条会实得 3");
        }

        // ---- ③ 团（Regiment）：和 Mob **成对**，差别只在远程 ----
        var trooper = new CardDef("FixtureTrooper", "FixtureTrooper", "unit",
                                  "Regiment: Gain +1 Attack",
                                  "common", "Test", 1, 2, 9, 3, null, subtype: "Infantry");
        CheckTrue(trooper.TriggerOps("regiment") != null,
                  "★ `Regiment: Gain +1 Attack` 的**正文被收下来了**");

        // ③-a 远程攻击 → **该**触发
        {
            var ctx = ProbeBattle(new[] { trooper }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            var t = Place(ctx, 0, 0, trooper, exhausted: false);
            Place(ctx, 1, 1, Unit("FixtureRegTarget", 1, 0, 9), exhausted: true);
            Check(t.Attack, 2, "动手之前 2 攻");
            CheckCode(RuleCore.DeclareAttack(ctx, 0, 0, 1, 1, ranged: true), RuleCodes.OK,
                      "**远程**打一下");
            Check(t.Attack, 3, "★ **远程攻击 → `Regiment:` 触发**（2 → 3 攻）");
        }

        // ③-b 近战攻击 → regiment **不该**触发
        {
            var meleeMan = new CardDef("FixtureRegMelee", "FixtureRegMelee", "unit",
                                       "Regiment: Gain +1 Attack",
                                       "common", "Test", 1, 2, 9, 0, null, subtype: "Infantry");
            var ctx = ProbeBattle(new[] { meleeMan }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            var t = Place(ctx, 0, 0, meleeMan, exhausted: false);
            Place(ctx, 1, 1, Unit("FixtureRegTarget2", 1, 0, 9), exhausted: true);
            CheckCode(RuleCore.DeclareAttack(ctx, 0, 0, 1, 1), RuleCodes.OK, "**近战**打一下");
            Check(t.Attack, 2,
                  "★ **近战攻击 → `Regiment:` 不触发**（还是 2 攻）—— 把 `ranged` 写反了这条会亮");
        }
    }

    ///
    /// <summary>
    /// **「关键词被触发」族的事件** —— 2026-09-13 候选 E。
    ///
    /// 卡面写的是 `When a friendly unit triggers Swarm, …` / `When this unit triggers Synapse, …` /
    /// `When a friendly troop uses Ferocity, …` / `When you trigger Ferocity, …` /
    /// `When a friendly troop triggers Duty, …` / `When a friendly unit triggers Mob, …`
    /// —— 这一族在候选 E 之前**一条都认不出**（自检的 `_tmp_view/when_unparsed.md` 里占 14 条中的 8 条）。
    ///
    /// 它由**一条通用规则**收下（`WhenEvents.TryParseKeywordTrigger`）：`&lt;谁&gt; triggers/uses &lt;关键词&gt;`，
    /// 而**不是**一个关键词写一条分支 —— 卡面换哪个词都是同一个形状。
    ///
    /// 这条 **四个方向都要钉**（少钉一个就会留下一个静默失效）：
    ///   ① **已实现的关键词 → 认得出**（`mob`）；
    ///   ② **没实现的关键词 → 仍然认不出** 🔴 —— 这是本族最容易犯的错：
    ///      认出来了、监听器也注册了，可那件事**永远发不出来**，于是卡面不打 `*`、
    ///      玩家却看不到任何效果。**「认不出」比「注册一条永远不响的」安全**。
    ///   ③ **`this unit triggers X` 是自指** —— 别人触发时**不该**叫醒它；
    ///   ④ **端到端真响一次**（正例），外加**敌方触发时不响**（反例）——
    ///      只钉正例的话，`RelFriendly` 那半写反了抓不到。
    /// </summary>
    static void TestKeywordTriggeredEvents()
    {
        // ---- ① 已实现的关键词 → 认得出 ----
        var ok = WhenEvents.Parse("a friendly unit triggers Mob");
        CheckTrue(ok != null && ok.Kind == WhenEventKind.Triggers("mob"),
                  "★ `a friendly unit triggers Mob` **认得出**了，"
                  + "`Kind` = `triggers:mob`（`mob` 第三十四轮已实现）");
        CheckTrue(ok.OwnerIs == WhenEvent.RelFriendly,
                  "★ `friendly` 那半段**收下了**（`OwnerIs = RelFriendly`）—— "
                  + "漏了这个，敌方单位触发 Mob 时它也会响（打得比卡面宽）");

        // ---- ② 没有**触发时机**的关键词 → **仍然认不出**（本族最容易犯的错）----
        //    ⚠️ 判据 **不是**「有没有实现」（`KeywordTable.Implemented`），是
        //    `KeywordTable.HasTriggerMoment` —— 2026-09-14 做掉 `destroyer` / `sabotage`
        //    时才分出来的两个概念。
        //    `destroyer` 是**目标选择限制**：没有任何地方会广播「某单位触发了毁灭者」
        //    ⇒ 认出来 = 注册一条**永远不响**的监听器（卡面不打 `*`、实际什么都不发生）。
        foreach (var kw in new[] { "destroyer" })
            CheckTrue(WhenEvents.Parse($"a friendly unit triggers {kw}") == null,
                      $"★ `{kw}` **没有触发时机** ⇒ `a friendly unit triggers {kw}` 必须**仍然认不出**。"
                      + "认出来了就会注册一条**永远不响**的监听器：卡面不打 `*`、实际却什么都不发生");
        // ✅ 反过来（2026-09-14）：`ecstasy` **已实现且真有触发点**（`RuleCore.Hurt` 里
        //    `FireTriggerOnBoard(…, Ecstasy)`）⇒ **必须认得出**。
        CheckTrue(WhenEvents.Parse("a friendly unit triggers ecstasy") != null,
                  "★ `ecstasy` 2026-09-14 做掉了（时机在 `RuleCore.Hurt`）⇒ "
                  + "`a friendly unit triggers ecstasy` **必须认得出**");
        // ✅ 反过来：`ferocity` / `duty` / `pray` / `agenda` / **`swarm`** / **`synapse`**
        //    已经实现 ⇒ **必须认得出**（否则那几张卡的监听器白丢）。
        //    ⚠️ `pray` 走的是**另一条短语**（`… prays`，kind=`Prays`），
        //    它在解析器里排在「关键词被触发」那一族**前面** —— 两个 kind 都算认得出。
        foreach (var kw in new[] { "ferocity", "duty", "agenda", "swarm", "synapse" })
        {
            var ev = WhenEvents.Parse($"a friendly unit uses {kw}");
            CheckTrue(ev != null && ev.Kind == WhenEventKind.Triggers(kw),
                      $"★ `a friendly unit uses {kw}` **认得出**了（`{kw}` A2 已实现）");
        }
        {
            var ev = WhenEvents.Parse("a friendly unit prays");
            CheckTrue(ev != null && ev.Kind == WhenEvent.Prays,
                      "★ `a friendly unit prays` **认得出**（kind = `prays`）");
            var ev2 = WhenEvents.Parse("a friendly troop uses Ferocity");
            CheckTrue(ev2 != null && ev2.Kind == WhenEventKind.Triggers("ferocity"),
                      "★ 卡面真写法 `When a friendly troop uses Ferocity`（`Raid Tactics`）也认得出");
        }

        // ---- ③ `this unit triggers X` 是**自指** ----
        var selfEv = WhenEvents.Parse("this unit triggers Mob");
        CheckTrue(selfEv != null && selfEv.SelfOnly,
                  "★ `this unit triggers Mob` 认成**自指**（`SelfOnly`）—— "
                  + "不设的话**任何友方单位**触发 Mob 都会把它叫醒");

        // ---- ④ 端到端：正例 + 反例 ----
        // 监听者：`When a friendly unit triggers Mob, gain +2 Attack`（正文走战术卡那套解析器）
        //
        // ⚠️ **2026-09-13 A3：这一格的两个数改了，理由不是实现退步**。
        //    写这条测试时（第三十四轮）两者都是 5，因为当时
        //    「`gain` 没写目标 → 落到**己方全体**」那条近似还在（`rule_core.gd:3167` 自标「既有近似」）——
        //    于是触发者那条 `Mob: Gain +1 Attack` 会给**全队** +1，监听器那条 `gain +2 Attack`
        //    也给**全队** +2。A3 把这条近似收窄成「**有施放者就落在施放者自己身上**」
        //    （`EffectTargetSpec.Subjectless`，按卡面：`Gain +1 Attack` 说的是这张卡自己）⇒
        //      · 触发者 = 2 + 1（自己那条）= **3**
        //      · 监听者 = 2 + 2（自己那条）= **4**
        //    **关键判据反而更干净了**：那 **+2 只可能来自监听器**（触发者那条只给自己加），
        //    广播没接的话监听者会停在 **2**。
        var watcher = new CardDef("FixtureKwListen", "FixtureKwListen", "unit",
                                  "When a friendly unit triggers Mob, gain +2 Attack",
                                  "common", "Test", 1, 2, 9, 0, null, subtype: "Infantry");
        CheckTrue(watcher.WhenTriggers.Count > 0,
                  "★ 监听器**注册上了**（事件短语 + 正文都解析成功才有这一条）");

        var mobber = new CardDef("FixtureKwMobber", "FixtureKwMobber", "unit",
                                 "Mob: Gain +1 Attack",
                                 "common", "Test", 1, 2, 9, 0, null, subtype: "Infantry");

        // ④-a **正例**：己方近战攻击 → 触发者的 Mob 响 → 监听者跟着响
        {
            var ctx = ProbeBattle(new[] { mobber, watcher }, new[] { Unit("EFoe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            var m = Place(ctx, 0, 0, mobber, exhausted: false);
            var w = Place(ctx, 0, 1, watcher, exhausted: true);
            Place(ctx, 1, 1, Unit("EFoe2", 1, 0, 9), exhausted: true);

            Check(m.Attack, 2, "出手前：触发者 2 攻");
            Check(w.Attack, 2, "出手前：监听者 2 攻");
            CheckCode(RuleCore.DeclareAttack(ctx, 0, 0, 1, 1), RuleCodes.OK, "己方**近战**打一下");
            Check(m.Attack, 3,
                  "触发者 3 攻 = 2 + 1（**只有自己那条 `Mob:`**；A3 起不再给全队加）");
            Check(w.Attack, 4,
                  "★ **`When a friendly unit triggers Mob` 真的响了** —— 监听者 2 + 2（自己那条）；"
                  + "**广播没接的话它会停在 2**");
        }

        // ④-b **反例**：**敌方**触发 Mob → `a friendly unit triggers Mob` **不该**响
        //     ⚠️ 观察量取**敌方那张触发者**的攻击力：极性写反的话，监听器会把 +2 种到
        //        **敌方**身上（2 + 1 + 2 = 5）；极性对了就是 2 + 1 = 3。
        {
            var ctx = ProbeBattle(new[] { watcher }, new[] { mobber });
            ToP1Turn(ctx, 2);
            var w = Place(ctx, 0, 0, watcher, exhausted: true);
            Place(ctx, 0, 1, Unit("EFoe3", 1, 0, 9), exhausted: true);   // 我方的靶子
            var em = Place(ctx, 1, 1, mobber, exhausted: false);
            PassTurn(ctx);                                              // 轮到**敌方**

            CheckCode(RuleCore.DeclareAttack(ctx, 1, 1, 0, 1), RuleCodes.OK, "**敌方**近战打一下");
            Check(em.Attack, 3,
                  "★ **敌方触发 Mob → 我方监听器不动** —— 敌方那张只有自己那份（2 → 3）；"
                  + "`friendly` 那半段被丢掉的话，它会变成 **5**");
            Check(w.Attack, 2, "我方监听者始终 2 攻（没被敌方的事件带着走）");
        }
    }

    ///
    /// <summary>
    /// **只有单位卡才当事件监听者** —— 2026-09-13 候选 E。
    ///
    /// `EffectResolver.BroadcastWhen` 收集监听者时**只扫棋盘上的单位**（`ctx.Players[p].Board[s]`），
    /// 别处一概不看 ⇒ **非单位卡（战术卡 / 防御卡）的 `WhenTriggers` 是一条永远没人消费的监听器**。
    /// 它还有第二重害处：把短语写进 <see cref="WhenEvents.UnknownPhrases"/> 报告桶，
    /// 于是自检的「认不出的短语」清单里**多出一条根本不该存在的项** ——
    /// 实测就是 `played`（`Reconnaissance Mission` 战术卡，那句 `When played, …` **本来就会被结算**，
    /// 走的是 `EffectText` 的 `Re gain`，主语根本不参与）。
    ///
    /// 这条钉**两半**（少一半都会退回误报）：
    ///   ① **不变量**：卡池里任何非单位卡的 `WhenTriggers` 必须是 0 ——
    ///      有人绕过 `CardDef.CanListenForEvents` 就会亮；
    ///   ② **报告桶**：`played` 不许再出现 —— 它重新冒出来说明
    ///      `ReportWhenCoverage` 那边**又按卡面文字不分卡类地数**了。
    ///      ⚠️ 两处必须**同一个判据**，各写一份迟早分家（本工程的老毛病）。
    /// </summary>
    static void TestListenerCardType()
    {
        WhenEvents.UnknownPhrases.Clear();
        var pool = CardDatabase.Load();

        // ---- ① 不变量：非单位卡不许有监听器 ----
        int bad = 0; string firstBad = null;
        foreach (var c in pool)
        {
            if (c == null || c.CanListenForEvents) continue;
            if (c.WhenTriggers.Count > 0)
            {
                bad++;
                if (firstBad == null) firstBad = c.Name;
            }
        }
        Check(bad, 0,
              $"★ **非单位卡的监听器数必须是 0** —— 实得 {bad} 张"
              + (firstBad != null ? $"（例：`{firstBad}`）" : "")
              + "。非 0 = `AddWhenTrigger` 的卡类判据被绕过了，那些监听器**永远不会被消费**"
              + "（`BroadcastWhen` 只扫棋盘上的单位）");

        // ---- ② 误报不许再进报告桶 ----
        CheckTrue(!WhenEvents.UnknownPhrases.Contains("played"),
                  "★ `played` **不在「认不出的短语」里** —— 它是**误报**，不是漏做："
                  + "`Reconnaissance Mission`（战术卡）那句 `When played, gain 3 Quest Points` "
                  + "**本来就会被结算**（`EffectText` 按 `.` 切句 → `Re gain` 认得，主语不参与）。"
                  + "⚠️ **别给它加解析分支** —— 加了就是给一张永远不在棋盘上的卡注册监听器，"
                  + "卡面还不打 `*`（红线）。它重新冒出来 = 报告那边又按卡面文字不分卡类地数了");

        // ---- ③ 反面护栏：**单位卡该收的照收**（别为了修 ② 把 ① 做成「都不收」）----
        var u = new CardDef("FixtureListenUnit", "FixtureListenUnit", "unit",
                            "When a friendly troop dies, gain +1 Attack",
                            "common", "Test", 1, 2, 9, 0, null, subtype: "Infantry");
        Check(u.WhenTriggers.Count, 1, "★ **单位卡**的监听器照旧收得到");

        var t = new CardDef("FixtureListenTactic", "FixtureListenTactic", "tactic",
                            "When a friendly troop dies, gain +1 Attack",
                            "common", "Test", 1, 0, 0, 0, null);
        Check(t.WhenTriggers.Count, 0,
              "★ **战术卡**写同一条卡面文字 → **不注册**（它不会出现在棋盘上）");
    }

    ///
    /// <summary>
    /// **载荷里不认识的词，必须报出来 —— 不许静默什么都不做** —— 2026-09-13 候选 E。
    ///
    /// 🔴 **2026-09-14（A7）改过这条测试的样本词，理由是它本来就在注释里**
    ///    —— 这条原先拿 `Gain +1 [weapon]` 当「不认识的属性词」的样本，
    ///    而本注释的上一版**已经写着**「`Banner Nob` 的卡面逐字核过是 +1 ⟨紫枪⟩ = 远程」。
    ///    也就是说：**词义早就查清了，当时只是选择「如实报、不实现」**。
    ///    A7 要做的 `Banner Nob` 光环句恰好带这个词（`+1 [attack] and +1 [weapon]`），
    ///    ⇒ 这轮把它**实现掉**（`GivePayload.ReAttr` 加 `weapon` → `ranged`），
    ///    样本词换成**仍未实现**的 `[power]`（`Hidden Hunters` 的真实卡面词），
    ///    并**新增**一条钉「`weapon` 现在真的生效了、而且加在远程上」的断言。
    ///    ⚠️ 这就是本工程那条纪律的实例：**红的断言不是删掉，是回来更新它**。
    ///
    /// **`weapon` = 远程**的实据（铁律 7，4 张卡**逐张亲读**，图标一律**紫圈枪**）：
    ///    · `Orks/3部队/Warpforge_16_Banner-Nob.png` —— `+1 ⟨拳⟩ and +1 ⟨枪⟩`
    ///    · `Sorotitas/4计策/Warpforge_44_Beacon-of-Faith.png` —— `+1 ⟨拳⟩ and +1 ⟨枪⟩`
    ///    · `Emperor_s Children/3部队/Warpforge_29_Malgarash-the-Adamant.png` —— `+1 ⟨枪⟩`
    ///    · `Emperor_s Children/3部队/Warpforge_33_Terminator-Champion.png` —— `+1 ⟨拳⟩ and +1 ⟨枪⟩`
    ///
    /// **仍然没实现的**（这 3 个词还在卡池里，`ReAttr` 白名单匹配不上 ⇒ 会走「本版不认识」）：
    ///    `Hidden Hunters` 的 `[power]`（卡面是拳，= 近战）·
    ///    `Avenging Zeal` 的 `[health icon]`（= 生命，卡面本来就是纯文字）/ `[attack icon]`（= 近战）。
    ///
    /// 🔴 **写这条时先入为主地猜错了，把过程记在这儿**：
    ///    一开始以为它们是在 `ApplyOneGain` 的属性 `switch` 里**静默 no-op** 的
    ///    （那个 `switch` 当时确实没有 `default`），于是去补了 `default` 并写了这条测试 ——
    ///    结果**测试红了**：事件流里根本没有那句话。追下去才发现
    ///    `GivePayload.Parse` 的属性词是**白名单**（`ReAttr`），`weapon` 压根匹配不上 ⇒
    ///    **在更早的地方就断了**，走的是 `DoGive` 的「载荷…本版不认识 —— 这条没生效」，
    ///    而且卡名会被记进 `unresolved`。⇒ **它们本来就被如实报出来了。**
    ///    （`default` 那条护栏留着，但它是护栏，不是修好的 bug —— 注释里写清楚了。）
    ///
    /// 这条测的就是**那个真正的保证**，两头都钉：
    ///   ① **值不许乱变**（不认识的词既不生效、也不许猜成 attack/ranged 加上去）；
    ///   ② **事件流里必须有一句话说明它没生效**（没有这句 = 静默失效）。
    /// </summary>
    static void TestUnknownPayloadAttr()
    {
        var bad = Tactic("T_BadAttr", 0, "Gain +1 [power]");
        var ctx = Battle(new[] { bad, bad }, new[] { Unit("X", 1, 1, 5) });
        ToP1Turn(ctx, 1);

        var u = Place(ctx, 0, 0, Ranged("FixtureAttrUnit", 1, 1, 5, 3), exhausted: true);
        Check(u.RangedAttack, 3, "打之前：远程 3（前提）");

        ctx.Events.Clear();
        int code = RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_BadAttr"), -1);

        Check(u.RangedAttack, 3,
              "★ 不认识的属性词**没有生效** —— 远程仍是 3（也别猜成 attack 乱加）");
        Check(u.Attack, 1, "★ 近战也没被动过");

        bool said = false;
        foreach (string e in ctx.Events)
            if (e != null && (e.Contains("不认识") || e.Contains("没生效"))) { said = true; break; }

        // 诊断：把「提到这张卡 / 提到达不到」的那几行拼出来，方便下次一眼看懂它走了哪条路
        var hint = new List<string>();
        foreach (string e in ctx.Events)
            if (e != null && (e.Contains("T_BadAttr") || e.Contains("power")
                              || e.Contains("载荷") || e.Contains("解析")))
                hint.Add(e);
        CheckTrue(said,
                  $"★ **引擎如实说了**（事件流里得有「不认识 / 没生效」字样）—— code={code}；"
                  + $"相关事件：{(hint.Count == 0 ? "(一条都没有)" : string.Join(" ‖ ", hint))}");
    }

    ///
    /// <summary>
    /// **`[weapon]` = 远程**，而且**只加远程**（2026-09-14 A7 新增）。
    ///
    /// 为什么单独一条：这个 token 在卡池里出现 **4 次**（`Banner Nob` · `Beacon of Faith` ·
    /// `Malgarash the Adamant` · `Terminator Champion`），而它长得**像近战词**
    /// （`weapon` 字面是「武器」，`[attack]` 才是拳图标）—— 归错了就是
    /// 「两个属性一起加在近战上」，**数值看着对得上、远程静默少加**，
    /// 而那正是本工程反复点名的那类静默失效。
    ///
    /// 两头都钉：**该动的动了**（远程 +1）· **不该动的没动**（近战保持原样）。
    /// 出处见 <see cref="TestUnknownPayloadAttr"/> 的注释（4 张卡图逐张亲读）。
    /// </summary>
    static void TestWeaponIsRanged()
    {
        var t = Tactic("T_Weapon", 0, "Gain +1 [weapon]");
        var ctx = Battle(new[] { t, t }, new[] { Unit("X", 1, 1, 5) });
        ToP1Turn(ctx, 1);

        var u = Place(ctx, 0, 0, Ranged("FixtureWeaponUnit", 1, 1, 5, 3), exhausted: true);
        Check(u.RangedAttack, 3, "打之前：远程 3（前提）");

        RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_Weapon"), -1);

        Check(u.RangedAttack, 4, "★ `+1 [weapon]` 加在**远程**上（4 张卡图核过：紫圈枪）");
        Check(u.Attack, 1, "★ **近战没被动过** —— 归成 attack 就会在这里红");
    }

    ///
    /// <summary>
    /// **阵营资源事件真的发得出来** —— 2026-09-13 候选 E 修的一个**静默失效**。
    ///
    /// `WhenEvent.Parse` 早就认得出 `When you gain Faith, …` / `When you collect a Spirit Stone, …`，
    /// 卡面也不打 `*`（正文是好的）—— 但 `BroadcastWhen` **从来没被这三种事件调用过**
    /// （`EffectResolver.DoFactionResource` 里只往 `ctx.Signals` 塞了一条**给表现层**的记录）。
    /// ⇒ `Paragon Warsuit` 与 4 张灵族单位注册的是**永远不会响的监听器**。
    ///
    /// 这条钉**三层**（少一层就会退回静默）：
    ///   ① **解析**：`When you gain [honour], …` 认得出，且 `Kind` 是**任务点**那一支
    ///      （`[honour]` 查出就是任务点，**不是**新资源 —— 证据见 `WhenEvent.GainQuest` 的注释）；
    ///   ② **端到端**：真的加了信仰 → 监听器**真的响**（督军掉 4 血）；
    ///   ③ **反例**：加的是**别的**资源（任务点）时，`gain Faith` 的监听器**不该**响 ——
    ///      三种资源共用同一个 `DoFactionResource`，串台的写法（拿 `kind` 判错）只钉正例抓不到。
    /// </summary>
    static void TestFactionResourceEvents()
    {
        // ---- ① 解析 ----
        var q = WhenEvents.Parse("you gain [honour]");
        CheckTrue(q != null && q.Kind == WhenEventKind.GainQuest,
                  "★ `When you gain [honour], …` 认得出，且归到**任务点**（`gainquest`）—— "
                  + "`[honour]` 不是新资源，就是任务点（中文译文写死的那句）");
        CheckTrue(WhenEvents.Parse("you gain Faith") != null,
                  "`you gain Faith` 认得出（这条一直认得出，缺的是**广播**）");
        CheckTrue(WhenEvents.Parse("you collect a Spirit Stone") != null,
                  "`you collect a Spirit Stone` 认得出");

        // ---- ② 端到端：加信仰 → 监听器响 ----
        var faithful = new CardDef("FixtureFaithListen", "FixtureFaithListen", "unit",
                                   "When you gain Faith, deal 4 damage to the enemy warlord",
                                   "common", "Test", 1, 2, 9, 0, null, subtype: "Infantry");
        CheckTrue(faithful.WhenTriggers.Count > 0,
                  "`When you gain Faith, …` 的监听器**注册上了**");
        {
            var gain = Tactic("T_FixtureFaith", 0, "Gain 3 ☀");
            var ctx = Battle(new[] { gain, gain }, new[] { Unit("X", 1, 1, 5) });
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 0, faithful, exhausted: true);

            int wlBefore = ctx.Players[1].Warlord.Health;
            RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_FixtureFaith"), -1);
            Check(ctx.Players[0].Faith, 3, "信仰 +3（前提）");
            Check(ctx.Players[1].Warlord.Health, wlBefore - 4,
                  "★ **监听器真的响了**（敌方督军 -4）—— "
                  + "`DoFactionResource` 里那条 `BroadcastWhen` 没加的话，督军血**一点不掉**，"
                  + "而且**不报任何错**（这正是它静默了一个轮次的原因）");
        }

        // ---- ③ 反例：加**任务点**时，`gain Faith` 的监听器**不该**响 ----
        //    三种资源共用同一个 `DoFactionResource`，`kind` 判错就会串台。
        {
            var gainQ = Tactic("T_FixtureQuest", 0, "Gain 2 任务点");
            var ctx = Battle(new[] { gainQ, gainQ }, new[] { Unit("X", 1, 1, 5) });
            ToP1Turn(ctx, 1);
            Place(ctx, 0, 0, faithful, exhausted: true);

            int wlBefore = ctx.Players[1].Warlord.Health;
            RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_FixtureQuest"), -1);
            Check(ctx.Players[0].Faith, 0,
                  "★ 加的是**任务点**，信仰**没动**（前提）—— "
                  + "这条同时钉住「任务点没有被误记成信仰」");
            Check(ctx.Players[1].Warlord.Health, wlBefore,
                  "★ **`gain Faith` 的监听器不响** —— 三种资源串台的话这里会掉 4 血");
        }
    }

    ///
    /// <summary>
    /// **天赋（Talent）** —— 2026-09-13 第三十四轮。规则书 `:218`「**回合开始时在手牌中生成临时战术**」。
    ///
    /// 它和别的关键词**最不一样**：效果**不是卡面正文**，而是「**按名字去卡池查一张卡**」。
    /// 实测 **80 个天赋名里 72 个查得到同名卡**、而且**全是 `tactic`**
    /// （`Witchfire` / `Rapid Deployment` / `Flickerjump` …）—— 所以这一条能一次点亮 **91 张卡**。
    ///
    /// 两层都要钉：
    ///   ① **生成**：回合开始 → 手里多出那张同名战术卡；
    ///   ② **临时**：**没打出去**的话回合结束要**移出游戏** ——
    ///      ⚠️ 多数天赋卡面**并没有印 `Ephemeral`**（`Witchfire` 卡面是 `Deal 1-3 damage. …`），
    ///      靠的是 `MarkEphemeral`；漏了这步它会**赖在手里不走**，而画面看起来完全正常。
    /// </summary>
    static void TestTalentKeyword()
    {
        var tal = new CardDef("FixtureTal", "FixtureTal", "unit", "Talent: Witchfire",
                              "common", "Test", 1, 2, 9, 0, null, subtype: "Infantry");
        CheckTrue(tal.TalentName == "Witchfire",
                  "★ 天赋名解析出来了（`Talent: Witchfire`）—— 它是**按名字去卡池查**的钥匙");

        // ⚠️ 名字要切在 `.` / `,` —— `Mekboy Gazmek` 的卡面是 `Talent: Mekaniak. Mob: …`，
        //    不切的话会把后面那段关键词一起当成名字（查不到卡，而且不报错）。
        var twoParts = new CardDef("FixtureTal2", "FixtureTal2", "unit",
                                   "Talent: Legendary Commandant. Duty: Deploy a Shock Trooper",
                                   "common", "Test", 1, 2, 9, 0, null, subtype: "Infantry");
        CheckTrue(twoParts.TalentName == "Legendary Commandant",
                  "★ 名字切在 `.` 为止（后面还接着别的关键词也不上当）");

        var ctx = BattlePool(new[] { tal }, new[] { Unit("EFoe", 1, 1, 9) },
                             CardDatabase.Load(), warlordFaction: "Ultramarines");
        ToP1Turn(ctx, 2);
        Place(ctx, 0, 0, tal, exhausted: true);
        CheckTrue(HandIdx(ctx, 0, "Witchfire") < 0,
                  "上了场但**还没到回合开始** —— 手里还没有那张天赋卡（反例）");

        PassTurn(ctx);      // P0 结束 → P1 开始
        PassTurn(ctx);      // P1 结束 → **P0 开始**（这一下该生成）
        CheckTrue(HandIdx(ctx, 0, "Witchfire") >= 0,
                  "★ **回合开始 → 天赋把同名战术卡塞进了手牌**（卡池里查得到 `Witchfire`）");

        // 不打出去 → 回合结束该被移走
        PassTurn(ctx);      // P0 结束（清扫临时卡）→ P1 开始
        CheckTrue(HandIdx(ctx, 0, "Witchfire") < 0,
                  "★ **回合结束还没打 → 移出游戏** —— 它是**临时卡**（规则书 `:229`）。"
                  + "漏了 `MarkEphemeral` 这条会实得「还在手里」，而画面看不出来");

        // ---- 反例：天赋名在卡池里查不到 → 什么都不生成（且要打日志，不许静默）----
        var bogus = new CardDef("FixtureTalBogus", "FixtureTalBogus", "unit",
                                "Talent: No Such Talent At All",
                                "common", "Test", 1, 2, 9, 0, null, subtype: "Infantry");
        var ctx2 = BattlePool(new[] { bogus }, new[] { Unit("EFoe", 1, 1, 9) },
                              CardDatabase.Load(), warlordFaction: "Ultramarines");
        ToP1Turn(ctx2, 2);
        Place(ctx2, 0, 0, bogus, exhausted: true);
        int before = ctx2.Players[0].Hand.Count;
        PassTurn(ctx2);
        PassTurn(ctx2);
        Check(ctx2.Players[0].Hand.Count, before + 1,
              "★ **查不到同名卡 → 一张都不生成**：手牌只多了**回合开始那次正常抽牌**（+1）。"
              + "天赋若乱生成，这里会实得 +2");
    }
    ///
    ///   ① `Unstable`（规则书 `:221`）—— 本单位死亡时**对随机单位（含双方）造成 1-3 伤害**。
    ///      ⚠️ 它**没有卡面正文**（卡面只写裸关键词），效果完全由规则定义。
    ///   ② `Ecstasy X`（`:182`）—— ⏸ **没做**：阈值 X 拿不到、正文也收不下来，两条卡点钉在块 ② 里。
    ///   ③ `Cruelty`（`:178`）—— **你的回合**敌方挨打未死 → **己方**带该词的牌触发。
    ///      🔴 触发方是挨打方的**对面**，和 `Penitence`（挨打那个自己触发）是**两条**。
    ///
    /// ①③ 都**正反各钉一次** —— 「该触发的触发了」钉不住筛错，那是本工程的老教训。
    /// </summary>
    ///
    /// <summary>
    /// **「条件换数值」**（`…, or &lt;另一个数&gt; if &lt;条件&gt;`）—— 2026-09-15，用户指正后加。
    ///
    /// 🔴 这一族**不是二选一**（英文那个 `or` 极易读错，中文一看就清楚）：
    /// 「若…则**把那一个数换掉**」。四条逐字（含中文）见 `EffectOp.AltAmount`：
    ///   `Vindicator` · `Wulfen Pack Leader` · `Monster Hunters` · `Disruption Blades`。
    ///
    /// 判据落点：解析 `EffectText.TryOrAltIf`（两种形态）· 结算 `EffectResolver.AltHolds` +
    /// 三个消费点（`DoDeal` 换伤害 · `DoDeployOnce` 换数量 · `DoGive` 换载荷）。
    ///
    /// ⚠️ **每条都跑真局面**（只看血量 / 场上数 / 攻击值，不看日志）；
    ///    **每条都跑正反两遍** —— 只测「条件成立」那一遍的话，
    ///    「两个数一起加上去」这种错会**假通过**。
    /// </summary>
    static void TestOrAltIf()
    {
        var pool = CardDatabase.Load();

        // ---- ① `Deal 3 damage to an enemy, or 6 if it has Hunt Mark`（`Vindicator`，**真卡** + `Rally`）----
        {
            string d1, d2;
            int with = RunVindicator(pool, true, out d1);
            int without = RunVindicator(pool, false, out d2);
            Check(with, 6, "★ 目标**带**猎杀标记 ⇒ 造成 **6** 点伤害 —— " + d1);
            Check(without, 3, "★ 目标**不带**猎杀标记 ⇒ 造成 **3** 点 —— " + d2);
        }

        // ---- ② `Deal 3 …, or 5 if you control no other troops`（`Wulfen Pack Leader` 那句，用等效夹具跑）----
        //   ⚠️ 用夹具不用真卡：`Wulfen Pack Leader` 的正文**没有 `Rally:` 前缀**
        //      （卡面裸写），走的是「带正文关键词」那条路 —— 那与**本条判据无关**，
        //      混进来只会让这条断言在别的东西坏掉时一起红。
        {
            string d1, d2;
            int alone = RunOrAltDeal(pool, "Deal 3 damage to a random enemy troop, or 5 if you control no other troops",
                                     0, out d1);
            int withMate = RunOrAltDeal(pool, "Deal 3 damage to a random enemy troop, or 5 if you control no other troops",
                                        1, out d2);
            Check(alone, 5, "★ **没有其他部队** ⇒ 造成 **5** 点 —— " + d1);
            Check(withMate, 3, "★ 场上有别的部队 ⇒ 造成 **3** 点 —— " + d2);
        }

        // ---- ③ `Deploy a Beast Snagga Boy, or 3 if your opponent controls a troop with 5 or more Health`
        //        （`Monster Hunters`，**真卡**；这里换的是**数量**）----
        {
            string d1, d2;
            int big = RunMonsterHunters(pool, 6, out d1);
            int small = RunMonsterHunters(pool, 4, out d2);
            Check(big, 3, "★ 对手有 **≥5 生命**的部队 ⇒ 部署 **3** 个猎兽小子 —— " + d1);
            Check(small, 1, "★ 对手只有 4 生命的部队 ⇒ 只部署 **1** 个 —— " + d2);
        }

        // ---- ④ `Give +1 [Attack] to your units this turn, or +3 [Attack] if they are Destroyer`
        //        （`Disruption Blades`；这里换的是**载荷里的数**）----
        {
            string d;
            var got = RunOrAltGive(pool, out d);
            Check(got[0], 3, "★ 带**毁灭者**的那个单位拿到 **+3** 攻击 —— " + d);
            Check(got[1], 1, "★ 不带的那一个只拿到 **+1** —— " + d);
        }
    }

    /// <summary>跑一次 `Vindicator`（真卡，Rally 在部署时触发），返回挨打那个敌人**掉了多少血**。</summary>
    static int RunVindicator(IList<CardDef> pool, bool huntMark, out string diag)
    {
        diag = "?";
        var vind = PoolCard(pool, "Vindicator");
        var foeCard = huntMark ? Unit("VindTarget", 1, 0, 30, "hunt mark") : Unit("VindTarget", 1, 0, 30);
        var ctx = BattlePool(new[] { vind }, new CardDef[0], pool, "SpaceWolves");
        ToP1Turn(ctx, 5);
        ctx.Players[0].Energy = 20;
        var target = Place(ctx, 1, 0, foeCard, exhausted: true);
        int hp0 = target.Health;
        int idx = HandIdx(ctx, 0, "Vindicator");
        if (idx < 0) { diag = "`Vindicator` 不在手里"; return -1; }
        ctx.ResetChoices();
        int code = RuleCore.PlayCard(ctx, 0, idx, SimpleAI.FirstFreeSlot(ctx.Players[0]));
        diag = $"部署 {RuleCodes.Describe(code)}；血 {hp0} → {target.Health}";
        return hp0 - target.Health;
    }

    /// <summary>一句 `Deal N …, or M if you control no other troops` 的夹具，返回敌人掉的血。
    /// `mates` = 自己场上**先摆几个**部队。</summary>
    static int RunOrAltDeal(IList<CardDef> pool, string desc, int mates, out string diag)
    {
        diag = "?";
        var t = Tactic("T_OrAltDeal", 0, desc);
        var foeCard = Unit("OrAltFoe", 1, 0, 30);
        var ctx = BattlePool(new[] { t }, new CardDef[0], pool, "Test");
        ToP1Turn(ctx, 3);
        ctx.Players[0].Energy = 20;
        for (int i = 0; i < mates; i++)
            Place(ctx, 0, i, Unit("OrAltMate" + i, 1, 1, 5), exhausted: true);
        var target = Place(ctx, 1, 0, foeCard, exhausted: true);
        int hp0 = EnemyTroopHp(ctx, 1);
        int idx = HandIdx(ctx, 0, "T_OrAltDeal");
        if (idx < 0) { diag = "夹具不在手里"; return -1; }
        // 🔴 **目标格必须传 `-1`**：这张卡的 `Target.Random == true` ⇒ `PickTarget` 返回 null
        //    ⇒ **本来就不该给格位**。传了 `0` 的话引擎会把它当成「玩家选中的那个目标」
        //    （`chosen` 优先于 `Side`）—— 结果**打到自己人身上**，而且卡返回 OK、不报错。
        //    实测踩过：`mates=0` 时 P0 的 0 号格是空的（`chosen` 为 null）⇒ 侥幸通过，
        //    `mates=1` 时才露出来。**测试夹具的坑，写在这儿免得下一个人再踩一次。**
        int code = RuleCore.PlayTactic(ctx, 0, idx, -1);
        int hp1 = EnemyTroopHp(ctx, 1);
        // ⚠️ **量「敌方非督军单位的总生命掉了多少」**，不要盯着某一个 `UnitState` ——
        //    `Place` 塞进去的那个引用可能**被后面的流程换掉**（实测踩过：单看它一直是 30，
        //    日志却说「造成了 3 点伤害」）。总生命是**结算结果**，与实现细节无关。
        diag = $"打出 {RuleCodes.Describe(code)}；目标血 {target.Health}；"
             + $"敌方总血 {hp0} → {hp1}；敌方场上[{BoardDump(ctx, 1)}]（自己场上另有 {mates} 个部队）"
             + "｜日志尾：" + TailEvents(ctx, 4);
        return hp0 - hp1;
    }

    /// <summary>某方场上**非督军**单位的总生命（`DoDeal` 这类「打了多少」的稳健量尺）</summary>
    static int EnemyTroopHp(BattleContext ctx, int p)
    {
        int n = 0;
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            var u = ctx.Players[p].Board[s];
            if (u != null && !u.IsWarlord) n += u.Health;
        }
        return n;
    }

    /// <summary>某方棋盘上「卡名(血)」的串，失败时一眼看出打到了谁</summary>
    static string BoardDump(BattleContext ctx, int p)
    {
        var sb = new System.Text.StringBuilder();
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            var u = ctx.Players[p].Board[s];
            if (u == null) continue;
            if (sb.Length > 0) sb.Append(" ");
            sb.Append(s).Append(":").Append(u.Name).Append("(").Append(u.Health).Append(")");
        }
        return sb.ToString();
    }

    /// <summary>战斗日志的最后 <paramref name="n"/> 条 —— 断言输出里带上它，
    /// 失败时不用再跑一遍去猜「为什么什么都没发生」。</summary>
    static string TailEvents(BattleContext ctx, int n)
    {
        var e = ctx.Events;
        int from = e.Count - n; if (from < 0) from = 0;
        var sb = new System.Text.StringBuilder();
        for (int i = from; i < e.Count; i++) { if (sb.Length > 0) sb.Append(" ‖ "); sb.Append(e[i]); }
        return sb.ToString();
    }

    /// <summary>跑一次 `Monster Hunters`（真卡），返回**自己场上多了几个单位**。
    /// `enemyHp` = 对手摆的那个部队的生命值（≥5 才该变 3 个）。</summary>
    static int RunMonsterHunters(IList<CardDef> pool, int enemyHp, out string diag)
    {
        diag = "?";
        var mh = PoolCard(pool, "Monster Hunters");
        var foeCard = Unit("MHFoe", 1, 0, enemyHp);
        var ctx = BattlePool(new[] { mh }, new CardDef[0], pool, "Goff");
        ToP1Turn(ctx, 8);
        ctx.Players[0].Energy = 20;
        Place(ctx, 1, 0, foeCard, exhausted: true);
        int before = OwnUnitCount(ctx, 0);
        int idx = HandIdx(ctx, 0, "Monster Hunters");
        if (idx < 0) { diag = "`Monster Hunters` 不在手里"; return -1; }
        int code = RuleCore.PlayTactic(ctx, 0, idx, -1);
        int after = OwnUnitCount(ctx, 0);
        diag = $"打出 {RuleCodes.Describe(code)}；自己场上 {before} → {after}（对手那个 {enemyHp} 血）";
        return after - before;
    }

    /// <summary>跑一次 `Give +1 …, or +3 … if they are destroyer` 的夹具，
    /// 返回 **[毁灭者那一个的攻击增量, 普通那一个的攻击增量]**。</summary>
    static int[] RunOrAltGive(IList<CardDef> pool, out string diag)
    {
        diag = "?";
        var t = Tactic("T_OrAltGive", 0,
                       "Give +1 attack to your units this turn, or +3 attack if they are destroyer");
        var ctx = BattlePool(new[] { t }, new CardDef[0], pool, "Sautekh");
        ToP1Turn(ctx, 3);
        ctx.Players[0].Energy = 20;
        var des = Place(ctx, 0, 0, Unit("OrAltDestroyer", 1, 2, 9, "destroyer"), exhausted: true);
        var plain = Place(ctx, 0, 1, Unit("OrAltPlain", 1, 2, 9), exhausted: true);
        Place(ctx, 1, 0, Unit("OrAltGiveFoe", 1, 0, 30), exhausted: true);
        int a0 = des.Attack, b0 = plain.Attack;
        int idx = HandIdx(ctx, 0, "T_OrAltGive");
        if (idx < 0) { diag = "夹具不在手里"; return new int[2]; }
        int code = RuleCore.PlayTactic(ctx, 0, idx, -1);
        diag = $"打出 {RuleCodes.Describe(code)}；毁灭者 {a0}→{des.Attack}，普通 {b0}→{plain.Attack}";
        return new[] { des.Attack - a0, plain.Attack - b0 };
    }

    /// <summary>某方场上**非督军**单位数（`Monster Hunters` 那条数部署了几个）</summary>
    static int OwnUnitCount(BattleContext ctx, int p)
    {
        int n = 0;
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            var u = ctx.Players[p].Board[s];
            if (u != null && !u.IsWarlord && u.IsAlive) n++;
        }
        return n;
    }

    ///
    /// <summary>
    /// **`Maulerfiend` 的近战+远程翻倍** + **`Runtherd` 的「5 费及以下造兽」** —— 2026-09-15，用户点名后补。
    ///
    /// 两张卡的**中文**（判据）：
    ///   · `Maulerfiend`：狂喜 5：**使本部队的近战和远程翻倍**。
    ///     （卡面 `Emperor_s Children/3部队/Warpforge_39_Maulerfiend.png`：8 费 · 近战 9 · 远程 5 · 生命 9；
    ///      正文那两个图标是**拳**与**枪**，**没有生命图标** ✓）
    ///   · `Runtherd`：群体：在你的手牌中生成一个**费用 5 或以下**的随机兽人野兽。
    /// </summary>
    static void TestDoubleAndCreateCost()
    {
        // ---- ① `Maulerfiend`：生命**不许**跟着翻（这正是当年整句不认的原因）----
        {
            var mf = new CardDef("FixtureMauler", "FixtureMauler", "unit",
                                 "Ecstasy 5: Double this troop's [Melee] and [Ranged]",
                                 "common", "Test", 8, 3, 4, 2, null, subtype: "Vehicle");
            var hit = Tactic("T_HitSelf", 0, "Deal 1 damage to a friendly unit");
            var ctx = ProbeBattle(new[] { hit }, new[] { Unit("MFFoe", 1, 0, 30) });
            ToP1Turn(ctx, 3);
            var u = Place(ctx, 0, 0, mf, exhausted: true);
            Check(u.Attack, 3, "前提：近战 3");
            Check(u.RangedAttack, 2, "前提：远程 2");

            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_HitSelf"), 0), RuleCodes.OK,
                      "打自己 1 点（生命 4 → 3，越过狂喜 5，触发正文）");
            Check(u.Attack, 6, "★ **近战翻倍**（3 → 6）");
            Check(u.RangedAttack, 4, "★ **远程也翻倍**（2 → 4）");
            CheckTrue(u.Health == 3 && u.MaxHealth == 4,
                      $"★ **生命没被翻倍**（应为 3/4，实得 {u.Health}/{u.MaxHealth}）—— "
                      + "照「近战+生命」套的话会变成 6/8，那正是当年把它整句挡掉的原因");
        }

        // ---- ② `Runtherd`：`create` 那条路要把**费用区间**抽出来并传给池子 ----
        {
            var pool = CardDatabase.Load();
            var runtherd = PoolCard(pool, "Runtherd");
            CheckTrue(runtherd != null, "卡池里有 `Runtherd`");
            if (runtherd != null)
            {
                var ops = EffectText.Parse(runtherd.Desc, out _, out _);
                EffectOp cr = null;
                foreach (var o in ops) if (o.Verb == "create") cr = o;
                CheckTrue(cr != null, "★ 解出一条 `create`");
                if (cr != null)
                {
                    Check(cr.CostMax, 5, "★ 费用上界抽出来了（**5 费及以下**）");
                    Check(cr.CostMin, 0, "★ 下界 = 0 —— 「5 点及以下」就是 **0~5**（用户 2026-09-15 原话）");
                    Check(cr.Payload, "random ork beast",
                          "★ 载荷里**不再夹着** `that costs 5 or less`（夹着的话下游会拿它当兵种词）");
                }
            }

            // 池子真的收口了：候选里**一张超 5 费的都没有**，而且全是野兽
            var r = CreatePool.Resolve(pool, "random ork beast", "Goff", costMin: 0, costMax: 5);
            CheckTrue(r != null && r.Ok, "★ 按「兽人野兽 + ≤5 费」筛得出候选 —— " + (r != null ? r.Detail : ""));
            if (r != null && r.Ok)
            {
                int over = 0, notBeast = 0;
                foreach (var c in r.Cards)
                {
                    if (c.Cost > 5) over++;
                    if (!CreatePool.MatchesKind(c, "beast")) notBeast++;
                }
                Check(over, 0, $"★ 候选（{r.Cards.Count} 张）里**一张超过 5 费的都没有**");
                Check(notBeast, 0, "★ 候选全是**野兽**");
            }
        }
    }

    static void TestUnstableEcstasyCruelty()
    {
        // ---- ① 不稳定：死亡时随机自爆 1-3 ----
        {
            var bomb = new CardDef("FixtureBomb", "FixtureBomb", "unit", "", "common", "Test",
                                   1, 1, 1, 0, new[] { "Unstable" }, subtype: "Infantry");
            CheckTrue(bomb.Has("unstable"), "★ `Unstable` 关键词认得出（`Prefixes` 里有它）");

            var kill = Tactic("T_KillBomb", 0, "Deal 99 damage to a friendly unit");
            var ctx = ProbeBattle(new[] { bomb, kill }, new[] { Unit("EFoe", 1, 0, 30) });
            ToP1Turn(ctx, 2);
            Place(ctx, 0, 1, bomb, exhausted: true);
            Place(ctx, 1, 1, Unit("FixtureBystander", 1, 0, 30), exhausted: true);

            // 场上活着的候选 = 敌方那个 + 双方督军 —— **随机**挑一个，所以断言只能量总量。
            int before = ctx.Players[0].Warlord.Health + ctx.Players[1].Warlord.Health
                       + ctx.Players[1].Board[1].Health;
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_KillBomb"), 1), RuleCodes.OK,
                      "把带 `Unstable` 的那个打死");
            CheckTrue(SlotOf(ctx, 0, "FixtureBomb") == -1, "它确实死了");
            int after = ctx.Players[0].Warlord.Health + ctx.Players[1].Warlord.Health
                      + ctx.Players[1].Board[1].Health;
            int blast = before - after;
            CheckTrue(blast >= 1 && blast <= 3,
                      $"★ **死亡自爆造成了 1-3 点随机伤害**（实测 {blast} 点）—— "
                      + "不读 `Unstable` 的话这里是 0");
        }

        // ---- ② 狂喜 X：✅ **2026-09-14 做掉**（用户点名要求）----
        //   这一格原来钉的是「**做不了**」的两条卡点（它们自己写着「红了就说明卡点解了」）。
        //   两条**都解了**，所以**换成正向的机制断言**：
        //     · 卡点 ①（`Ecstasy 2:` 的正文收不下来）→ 现在收得到（`AddTriggerOp` 认数值后缀）
        //     · 卡点 ②（阈值 X 没来源）→ 现在从**卡面正文**取（`CardDef.EcstasyX`）
        //   触发判据照参考实现 `rule_core.gd:4438-4449`：**首次越线、一辈子一次**。
        {
            var ecs = new CardDef("FixtureEcstasy", "FixtureEcstasy", "unit",
                                  "Ecstasy 2: Gain +1 Attack",
                                  "common", "Test", 1, 1, 3, 0, null, subtype: "Infantry");
            CheckTrue(ecs.TriggerOps("ecstasy") != null,
                      "★ **卡点 ① 解了**：`Ecstasy 2:` 的正文**收下来了** —— `AddTriggerOp` "
                      + "现在认「名字 + 可选数字后缀」（原来整词相等比对，`\"ecstasy 2\"` 对不上）");
            Check(ecs.EcstasyX, 2,
                  "★ **卡点 ② 解了**：阈值 X 从**卡面正文**取到 2 —— `keywords` 里是裸 `Ecstasy`"
                  + "（只读卡表会静默当成 1），正文正则才是最终真相");
            CheckTrue(!RuleCore.UnimplementedKeywords(CardDatabase.Load()).Contains("ecstasy"),
                      "★ 所以 `ecstasy` **从「未实现」名单上下来了**");

            // 真打一局：打 1 点 → 生命 3→2，**越过阈值 2** ⇒ 触发一次（1 → 2 攻）
            var hit = Tactic("T_EcsHit", 0, "Deal 1 damage to an enemy");
            var ctx = ProbeBattle(new[] { hit, hit }, new[] { Unit("EcsFoe", 1, 0, 9) });
            ToP1Turn(ctx, 2);
            var u = Place(ctx, 1, 0, ecs, exhausted: true);
            Check(u.Attack, 1, "动手之前 1 攻");
            Check(u.Health, 3, "动手之前 3 血");

            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_EcsHit"), 0), RuleCodes.OK,
                      "打第 1 点伤害（瞄准狂喜单位所在的 0 号格）");
            Check(u.Health, 2, "★ 生命降到 **2**（= 阈值 X）");
            Check(u.Attack, 2, "★ **生命降至 X 未死亡 ⇒ 触发 `Ecstasy 2:` 的正文**（1 → 2 攻）");

            // 反例：**再挨一下不再触发** —— 参考实现用 `_ecstasy_fired` 置位，是「一辈子一次」
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_EcsHit"), 0), RuleCodes.OK,
                      "再打第 2 点伤害");
            Check(u.Health, 1, "生命降到 1（仍 ≤ 阈值 2）");
            Check(u.Attack, 2,
                  "★ **第二次挨打不再触发**（还是 2 攻）—— 不置 `EcstasyFired` 的话这条会实得 3");
        }

        // ---- ③ 残忍：己方回合、**敌方**挨打未死 → **己方**带该词的牌触发 ----
        {
            var cruel = new CardDef("FixtureCruel", "FixtureCruel", "unit",
                                    "Cruelty: Gain +1 Attack",
                                    "common", "Test", 1, 2, 9, 0, null, subtype: "Infantry");
            var ownTank = new CardDef("FixtureCruelTank", "FixtureCruelTank", "unit", "",
                                      "common", "Test", 1, 0, 30, 0, null, subtype: "Infantry");
            var hitOwn = Tactic("T_HitOwn", 0, "Deal 1 damage to a friendly unit");
            var ctx = ProbeBattle(new[] { cruel, ownTank, hitOwn },
                                  new[] { Unit("EFoe", 1, 0, 30) });
            ToP1Turn(ctx, 2);
            var c = Place(ctx, 0, 0, cruel, exhausted: true);
            Place(ctx, 0, 1, Unit("FixtureCruelKiller", 1, 3, 9), exhausted: false);
            Place(ctx, 0, 2, ownTank, exhausted: true);
            Place(ctx, 1, 1, Unit("FixtureCruelPrey", 1, 0, 30), exhausted: true);
            Check(c.Attack, 2, "动手之前 2 攻");

            // ① 打**敌方**（没打死）→ 该触发
            CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 1), RuleCodes.OK, "我方打敌方一下");
            Check(SlotOf(ctx, 1, "FixtureCruelPrey"), 1, "对面那个还活着（没被打死）");
            Check(c.Attack, 3, "★ **敌方挨打未死 → 己方 `Cruelty:` 触发**（2 → 3 攻）");

            // ② 反例：**自己人**挨打 → **不该**再触发（规则书写的是「**敌方**单位受伤害」）
            //    ⚠️ 走**伤害效果**打自己人，不走攻击 —— 攻击**根本选不中自己人**
            //    （`IsValidTarget` 直接 `ErrSelf`）。第一版就是拿同一个攻击者再打一次，
            //    结果先撞上「本回合已行动」（错误码 6），**没走到极性那一步**（实测踩过）。
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "T_HitOwn"), 2), RuleCodes.OK,
                      "用伤害效果打自己人一下");
            Check(c.Attack, 3,
                  "★ **自己人挨打 → `Cruelty` 不触发**（还是 3 攻）—— "
                  + "不判「挨打的是不是敌方」的话这条会实得 4");
        }
    }

    ///
    /// <summary>
    /// **临时卡 `Ephemeral` + 「移出游戏」区域** —— 2026-09-13 第三十二轮。
    ///
    /// 规则依据（`资料/规则书/…_中文翻译.md`）：
    ///   · `:183` 临时（Ephemeral）| **回合结束时若在手牌则移除**
    ///   · `:229` 天赋/伴生/潮涌复制 均临时，回合结束**未打出即消失** ——
    ///           **从游戏中移除（非弃置）**
    ///
    /// 分四层钉（用户特别要求「不能只在真实运行时才发现问题」，所以每层都要有）：
    ///   ① **机制** —— 回合结束真的移走了，而且**进了 `Removed`、没进 `Discard`**
    ///   ② **反例** —— 不该动的**没动**（非临时卡 / 打出去的 / 对手手里的）
    ///   ③ **实例级** —— 被标记的复制移走时**原件不受影响**（这一层最容易做错，见下）
    ///   ④ **整局不变量** —— 手牌里的牌**不许同时出现在 `Removed`**（钉住「该移的没移」）
    /// </summary>
    static void TestEphemeral()
    {
        // ---- ① 机制：回合结束 → 从手牌移除 → 进 `Removed`、**不进 `Discard`** ----
        {
            var eph = new CardDef("FixtureEph", "FixtureEph", "tactic", "Does nothing",
                                  "common", "Test", 1, 0, 0, 0, new[] { "Ephemeral" });
            var plain = Tactic("FixturePlainTac", 1, "Does nothing");
            var ctx = ProbeBattle(new[] { eph, plain }, new[] { Unit("EFoe", 1, 1, 9) });
            ToP1Turn(ctx, 1);
            Check(HandIdx(ctx, 0, "FixtureEph"), 0, "临时卡在 P1 手里");
            Check(HandIdx(ctx, 0, "FixturePlainTac"), 1, "普通战术卡也在手里");

            int removedBefore = ctx.Removed.Count;
            int discardedBefore = ctx.Players[0].Discard.Count;
            RuleCore.EndTurn(ctx);

            Check(HandIdx(ctx, 0, "FixtureEph"), -1,
                  "★ 回合结束 → **临时卡从手牌移除了**（规则书 :183）");
            Check(ctx.Removed.Count, removedBefore + 1,
                  "★ 它进了 **`Removed`（移出游戏）** 这个区域");
            Check(ctx.Removed[ctx.Removed.Count - 1].Card.Name, "FixtureEph", "记的是那一张");
            Check(ctx.Removed[ctx.Removed.Count - 1].Owner, 0, "记了主人");
            Check(ctx.Removed[ctx.Removed.Count - 1].Turn, 1, "记了第几回合移出的");
            Check(ctx.Players[0].Discard.Count, discardedBefore,
                  "★ **没进弃牌堆** —— 规则书 :229「从游戏中移除（**非弃置**）」"
                  + "（进了弃牌堆的话，「从弃牌堆拿一张」那类效果就能把它捞回来，那是错的）");
            CheckTrue(HandIdx(ctx, 0, "FixturePlainTac") >= 0,
                      "★ **非临时卡不动**（筛错的话这条会亮）");
        }

        // ---- ② 反例：**打出去的**临时卡不该被移除（`未打出即消失`） ----
        {
            var eph = new CardDef("FixtureEphTac", "FixtureEphTac", "tactic", "Draw 1 card",
                                  "common", "Test", 1, 0, 0, 0, new[] { "Ephemeral" });
            // ⚠️ 牌库里要**垫一张**给 `Draw 1 card` 抽（否则它去抽空牌库、督军吃疲劳，
            //    虽然结论一样，但日志会变得难读）
            var ctx = ProbeBattle(new[] { eph, Unit("FixtureDrawFodder", 1, 0, 1) },
                                  new[] { Unit("EFoe", 1, 1, 9) });
            ToP1Turn(ctx, 1);
            CheckCode(RuleCore.PlayTactic(ctx, 0, HandIdx(ctx, 0, "FixtureEphTac"), -1), RuleCodes.OK,
                      "把这张临时战术卡**打出去**");
            int discardedBefore = ctx.Players[0].Discard.Count;
            Check(discardedBefore, 1, "打出去的战术卡正常进弃牌堆");

            RuleCore.EndTurn(ctx);
            Check(ctx.Removed.Count, 0,
                  "★ **打出去了的**临时卡**不该**被移出游戏 —— 规则书 :229 的限定词是"
                  + "「回合结束**未打出**即消失」（不看这个限定词的话这条会亮）");
            Check(ctx.Players[0].Discard.Count, discardedBefore, "它照旧待在弃牌堆里");
        }

        // ---- ③ 实例级：**被标记的复制**移走 ⇒ **原件不受影响** ----
        //     🔴 2026-09-18 第 7 行第 3 步之后，这一段量的是**真正的实例身份**：
        //        两张同名卡是**两个对象**，标记打在**其中一份**上，
        //        「谁该走」是**直接问出来**的 —— 改之前按卡模板记份数，
        //        同名两张在对象层面**分不开**（这一节原来的注释就写着「没有实例身份」）。
        {
            var copyCard = new CardDef("FixtureNoKw", "FixtureNoKw", "tactic", "Does nothing",
                                       "common", "Test", 1, 0, 0, 0, null);   // **不带 Ephemeral 关键词**
            var ctx = ProbeBattle(new[] { copyCard, copyCard }, new[] { Unit("EFoe", 1, 1, 9) });
            ToP1Turn(ctx, 1);
            Check(ctx.Players[0].Hand.Count, 2, "手里两张**同名**卡");
            var first = ctx.Players[0].Hand[0];
            var second = ctx.Players[0].Hand[1];
            CheckTrue(!ReferenceEquals(first, second),
                      "★ 同名两张是**两个实例**（改之前它们是同一个 `CardDef` 对象）");
            CheckTrue(!ctx.IsEphemeral(first), "默认**不是**临时卡（它没带那个关键词）");

            ctx.MarkEphemeral(first);               // 只把「其中一份」标成临时
            CheckTrue(ctx.IsEphemeral(first), "标了之后判据认它是临时的");
            CheckTrue(!ctx.IsEphemeral(second),
                      "★ **另一份不受影响**（改之前按卡模板记，两张一起判真 —— 那正是老 bug）");

            RuleCore.EndTurn(ctx);
            Check(ctx.Players[0].Hand.Count, 1, "★ 标记了一份 ⇒ **只移走一份**");
            Check(ctx.Removed.Count, 1, "`Removed` 里一张");
            CheckTrue(ctx.Removed.Count == 1 && ReferenceEquals(ctx.Removed[0].Instance, first),
                      "★ **移走的正是被标记的那一份**（不是靠遍历顺序猜的）");
            CheckTrue(ctx.Removed.Count == 1 && !ctx.Removed[0].Instance.EphemeralMarked,
                      "★ **标记被销掉了** —— 不销的话「牌已经不在了但标记还在」，"
                      + "下次造同样的卡会多出一份本不该存在的临时身份（**静默**，两轮之后才看得出来）");

            // 再验一次：**另一局**里不标任何东西，这张卡就该老老实实待着。
            var ctx2 = ProbeBattle(new[] { copyCard }, new[] { Unit("EFoe", 1, 1, 9) });
            ToP1Turn(ctx2, 1);
            RuleCore.EndTurn(ctx2);
            Check(ctx2.Players[0].Hand.Count, 1,
                  "★ 没标过 ⇒ 它就是**普通卡**，回合结束不动它");
            Check(ctx2.Removed.Count, 0, "`Removed` 空");
        }

        // ---- ④ 整局不变量：**手里有的牌不许同时出现在 `Removed`** ----
        //     直接钉「该移的没移」这一类 —— 只扫一遍手牌、某张漏了，这条就会亮。
        //     ⚠️ 这不等于「全局守恒」：`create`/`deploy` 是**凭空造牌**（`PickN` 从卡池复制），
        //        所以「卡总数」本来就会变。真正的守恒要另开一轮（见文档 §五 的说明）。
        {
            var pool = CardDatabase.Load();
            int checked_ = 0, violations = 0;
            for (int g = 0; g < 3; g++)
            {
                var d0 = DeckBuilder.StarterDeck(pool, "Ultramarines", DeckBuilder.ClassicDeckSize,
                                                 new System.Random(500 + g), unitsOnly: false);
                var d1 = DeckBuilder.StarterDeck(pool, "Goff", DeckBuilder.ClassicDeckSize,
                                                 new System.Random(600 + g), unitsOnly: false);
                var ctx = RuleCore.NewBattle(d0, d1, seed: 5100 + g, cardPool: pool);
                int guard = 0;
                while (!ctx.IsOver && guard++ < 300)
                {
                    RuleCore.BeginTurn(ctx);
                    PlayAiTurn(ctx, ref _tacCount);
                    if (ctx.IsOver) break;
                    RuleCore.EndTurn(ctx);

                    // 每一回合都查一遍不变量（`EndTurn` 之后 `Removed` 刚更新过）
                    for (int p = 0; p < 2; p++)
                        foreach (var c in ctx.Players[p].Hand)
                        {
                            checked_++;
                            bool inRemoved = false;
                            foreach (var r in ctx.Removed)
                                if (ReferenceEquals(r.Card, c)) { inRemoved = true; break; }
                            if (inRemoved) violations++;
                        }
                }
            }
            CheckTrue(checked_ > 0, $"整局不变量查了 {checked_} 次手牌");
            Check(violations, 0,
                  "★ **没有任何牌同时出现在手牌和「移出游戏」里** —— "
                  + "该移的漏移了的话这条会亮");
            Debug.Log(P + $"   ④ 3 局共查 {checked_} 张手牌，越界 {violations} 张");
        }
    }

    ///
    /// <summary>
    /// **伤害同时结算 / 死亡触发排在其后** —— 2026-09-13 第三十二轮（规则书审计第①条）。
    ///
    /// 规则书依据：
    ///   · `:145`「伤害按声明的攻击类型**同时结算**」——
    ///     例子：「兽人小子造成 3 点伤害，**同时**受到 1 点反击。初生者（0 生命）进入弃牌堆；
    ///     兽人小子生命 3→2」⇒ **被打死的那个照样反击**
    ///   · `:238`「序列：攻击 → **双方结算伤害** → 生命归 0 方触发效果 → 摧毁方触发效果」
    ///
    /// 🔴 **修之前错在哪**：`Hurt` 一边扣血一边当场放死亡触发 ⇒ 目标一死，
    ///    它的 Backlash 就**比反击先放**。而被攻击方的 Backlash 若把攻击者打死，
    ///    **反击整下被跳过**（`Hurt` 见已经死了就 `return 0`）—— 每一局带 Backlash/Penitence
    ///    的攻击都算错。
    ///
    /// ⚠️ 这一节的价值全在**第二段那个反例**上：只验「Backlash 会触发」的话，
    ///    改动前后都是绿的（它本来就触发，只是**时机**不对）。要钉住「顺序」，
    ///    必须造一个「Backlash 能打死攻击者」的局面 —— 那样修之前反击会被吞掉。
    /// </summary>
    static void TestSimultaneousDamage()
    {
        // ---- ① 规则书 :145 的例子：**被打死的照样反击** ----
        {
            var big = new CardDef("FixtureBig", "FixtureBig", "unit", "", "common", "Test",
                                  1, 3, 5, 0, null, subtype: "Infantry");   // 3 攻，能一击打死 1 血的
            var small = new CardDef("FixtureSmall", "FixtureSmall", "unit", "", "common", "Test",
                                    1, 1, 1, 0, null, subtype: "Infantry"); // 1 血、1 攻 —— 正是那个「初生者」
            var ctx = ProbeBattle(new[] { big }, new[] { Unit("EFoe", 1, 1, 9) });
            ToP1Turn(ctx, 4);
            var a = Place(ctx, 0, 0, big, exhausted: false);
            var b = Place(ctx, 1, 1, small, exhausted: true);

            CheckCode(RuleCore.DeclareAttack(ctx, 0, 0, 1, 1), RuleCodes.OK, "3 攻打 1 血");
            Check(SlotOf(ctx, 1, "FixtureSmall"), -1, "被打的进了弃牌堆（`:145` 那句）");
            Check(a.Health, 4,
                  "★ 攻击者挨了 **1 点反击**（5 → 4）—— 规则书 `:145` 的例子就是「被打死的照样反击」");
        }

        // ---- ② **反例：Backlash 不许抢在反击前面**（这一条才是本节的重点） ----
        //     ⚠️ **必须让「反击」和「Backlash」都真的生效**，否则断言量不到东西：
        //        · 攻击者要**活得下来反击那一下、但活不过接下来的 Backlash**
        //        · Backlash 的正文要用**引擎真的结算得了**的句子
        //          （⚠️ 别用 `Deal 3 damage to the attacker` ——「the attacker」不是解析器认识的
        //           目标词，那条会「没有合法目标，空过」。实测卡池里**没有任何卡**这么写，
        //           所以这不是解析器的缺口，是本用例自己的夹具写错了。）
        //     局面：攻击者 4 血 1 攻 · 被攻击者 1 血 1 攻 + `Backlash: Deal 4 damage to all enemies`
        //       · **修之前**：目标一死 → Backlash **当场**放（攻击者 4→0，死）→
        //         轮到反击时 `Hurt` 见攻击者已经死了就 `return 0` ⇒ **反击整下被吞**（日志里没有那条）
        //       · **修之后**：先反击（4→3），再 Backlash（3→-1）—— **两次都在日志里**
        {
            var tough = new CardDef("FixtureTough", "FixtureTough", "unit", "", "common", "Test",
                                    1, 1, 4, 0, null, subtype: "Infantry");   // 4 血 1 攻
            var vengeful = new CardDef("FixtureVengeful", "FixtureVengeful", "unit",
                                       "Backlash: Deal 4 damage to all enemies",
                                       "common", "Test", 1, 1, 1, 0, null, subtype: "Infantry");
            var ctx = ProbeBattle(new[] { tough },
                                  new[] { Unit("EFoe", 1, 1, 9), vengeful });
            ToP1Turn(ctx, 4);
            var a = Place(ctx, 0, 0, tough, exhausted: false);
            Place(ctx, 1, 1, vengeful, exhausted: true);

            CheckCode(RuleCore.DeclareAttack(ctx, 0, 0, 1, 1), RuleCodes.OK, "1 攻打 1 血");

            int counter = 0, backlash = 0, counterAt = -1, backlashAt = -1;
            for (int i = 0; i < ctx.Events.Count; i++)
            {
                string e = ctx.Events[i];
                if (e == null) continue;
                if (e.Contains("反击")) { counter++; if (counterAt < 0) counterAt = i; }
                if (e.Contains("BACKLASH")) { backlash++; if (backlashAt < 0) backlashAt = i; }
            }
            Check(counter, 1,
                  "★ **反击打出来了，而且正好一次** —— 修之前这条是 0：目标一死，Backlash 当场把攻击者打死"
                  + "（4→0），轮到反击时见人已经没了就整下跳过（`Hurt` 开头那个 `!u.IsAlive → return 0`）");
            CheckTrue(backlash > 0, "Backlash 也放了（两边都该发生）");
            CheckTrue(counterAt >= 0 && backlashAt >= 0 && counterAt < backlashAt,
                      "★ **顺序**：反击**在** Backlash **之前** —— 规则书 `:238`"
                      + "「攻击 → 双方结算伤害 → 生命归 0 方触发效果」（顺序反了这条会亮）");
            Check(a.Health, -1, "攻击者两下都吃了：4 → 反击后 3 → Backlash 后 −1（两次伤害都到位）");
            Check(SlotOf(ctx, 0, "FixtureTough"), -1, "攻击者最终也倒了");
        }

        // ---- ③ 反例：**不该同时的别同时**（把批用过头了会变成「每次攻击都延后所有人」）----
        //     攻击者一击打死 1 血目标、自己没那么脆 ⇒ 攻击者**不该**掉血。
        {
            var strong = new CardDef("FixtureStrong", "FixtureStrong", "unit", "", "common", "Test",
                                     1, 5, 9, 0, null, subtype: "Infantry");
            var zeroAtk = new CardDef("FixtureZeroAtk", "FixtureZeroAtk", "unit", "", "common", "Test",
                                      1, 0, 1, 0, null, subtype: "Infantry");
            var ctx = ProbeBattle(new[] { strong }, new[] { Unit("EFoe", 1, 1, 9) });
            ToP1Turn(ctx, 4);
            var s = Place(ctx, 0, 0, strong, exhausted: false);
            Place(ctx, 1, 1, zeroAtk, exhausted: true);

            CheckCode(RuleCore.DeclareAttack(ctx, 0, 0, 1, 1), RuleCodes.OK, "5 攻打 0 攻的 1 血");
            Check(SlotOf(ctx, 1, "FixtureZeroAtk"), -1, "目标死了");
            Check(s.Health, 9, "★ 攻击者**一点没掉血**（目标 0 攻 ⇒ 没有反击）—— "
                  + "批用过头的话这里会变成「延后处理导致重复扣血」");
        }
    }

    /// <summary>按**卡对象**比 `RemovedCard` 的辅助类（留着给别的查询用）</summary>
    class RemovedCardComparer : IEqualityComparer<RemovedCard>
    {
        public bool Equals(RemovedCard a, RemovedCard b)
        {
            return a != null && b != null && ReferenceEquals(a.Card, b.Card);
        }
        public int GetHashCode(RemovedCard o)
        {
            return o == null || o.Card == null ? 0 : o.Card.GetHashCode();
        }
    }

    static int _tacCount;

    static void DumpTaxonomy(List<CardDef> pool, EffectText.TextCoverage cov)
    {
        // 动词 → （载荷子类 → 张数）
        var byVerb = new Dictionary<string, Dictionary<string, int>>();
        var verbCards = new Dictionary<string, List<string>>();
        var targetKinds = new Dictionary<string, int>();
        var condKinds = new Dictionary<string, int>();
        int cardsWithOps = 0;

        foreach (var c in pool)
        {
            if (c == null || c.Type != "tactic") continue;
            var ops = EffectText.Parse(c.Desc, out _, out _);
            if (ops.Count == 0) continue;
            cardsWithOps++;

            var seenVerbs = new HashSet<string>();
            foreach (var op in ops)
            {
                if (!byVerb.ContainsKey(op.Verb)) { byVerb[op.Verb] = new Dictionary<string, int>(); verbCards[op.Verb] = new List<string>(); }
                string sub = PayloadClassOf(op);
                int n0;
                byVerb[op.Verb][sub] = byVerb[op.Verb].TryGetValue(sub, out n0) ? n0 + 1 : 1;
                if (seenVerbs.Add(op.Verb) && verbCards[op.Verb].Count < 4) verbCards[op.Verb].Add(c.Name);

                if (op.Target != null)
                {
                    string tk = op.Target.Side + "/" + op.Target.Kind
                              + (op.Target.Count == 0 ? "(all)" : "(n" + op.Target.Count + ")")
                              + (op.Target.SubtypeFilter != null ? "[sub:" + op.Target.SubtypeFilter + "]" : "");
                    int n1; targetKinds[tk] = targetKinds.TryGetValue(tk, out n1) ? n1 + 1 : 1;
                }
                if (!string.IsNullOrEmpty(op.ConditionKind))
                {
                    int n2; condKinds[op.ConditionKind] = condKinds.TryGetValue(op.ConditionKind, out n2) ? n2 + 1 : 1;
                }
                else if (!string.IsNullOrEmpty(op.Condition))
                {
                    int n3; string k = "(判不了) " + op.Condition;
                    condKinds[k] = condKinds.TryGetValue(k, out n3) ? n3 + 1 : 1;
                }
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine("# 战术卡效果分类表（自动生成，别手改）");
        sb.AppendLine();
        sb.AppendLine("由 `RuleEngineTest.DumpTaxonomy` 每次跑自检时重写。");
        sb.AppendLine($"战术卡 {cov.Cards} 张 · 解析出效果的 {cardsWithOps} 张 · " + cov.Summary());
        sb.AppendLine();
        sb.AppendLine("## 一、按**动词**分堆（这是结算层的分派单位）");
        sb.AppendLine();
        sb.AppendLine("| 动词 | 结算层实现了吗 | op 数 | 载荷子类 | 例 |");
        sb.AppendLine("|---|---|---|---|---|");

        var verbOrder = new List<string>(byVerb.Keys);
        verbOrder.Sort((x, y) => Total(byVerb[y]) - Total(byVerb[x]));
        foreach (string v in verbOrder)
        {
            var subs = new List<string>();
            var subOrder = new List<string>(byVerb[v].Keys);
            subOrder.Sort((x, y) => byVerb[v][y] - byVerb[v][x]);
            foreach (string sh in subOrder) subs.Add(sh + "×" + byVerb[v][sh]);
            sb.AppendLine($"| `{v}` | {(RuleCore.ImplementedEffectVerbs.Contains(v) ? "✅" : "❌ **没实现**")} "
                        + $"| {Total(byVerb[v])} | {string.Join(" · ", subs.ToArray())} | {string.Join("、", verbCards[v].ToArray())} |");
        }

        sb.AppendLine();
        sb.AppendLine("## 二、按**目标**分堆（`EffectTargetSpec` 的组合）");
        sb.AppendLine();
        var tOrder = new List<string>(targetKinds.Keys);
        tOrder.Sort((x, y) => targetKinds[y] - targetKinds[x]);
        foreach (string t in tOrder) sb.AppendLine($"- `{t}` ×{targetKinds[t]}");

        sb.AppendLine();
        sb.AppendLine("## 三、按**条件**分堆（`EffectCondition`）");
        sb.AppendLine();
        if (condKinds.Count == 0) sb.AppendLine("- （没有带条件的 op）");
        var cOrder = new List<string>(condKinds.Keys);
        cOrder.Sort((x, y) => condKinds[y] - condKinds[x]);
        foreach (string c in cOrder) sb.AppendLine($"- `{c}` ×{condKinds[c]}");

        const string path = "d:/4/_tmp_view/tactic_taxonomy.md";
        System.IO.File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8);

        Debug.Log(P + $"   效果分类表写到 {path}（{byVerb.Count} 个动词 / {targetKinds.Count} 类目标 / {condKinds.Count} 类条件）");
        foreach (string v in verbOrder)
            Debug.Log(P + $"     {v,-12} op {Total(byVerb[v]),-4} "
                      + (RuleCore.ImplementedEffectVerbs.Contains(v) ? "" : "← **没实现**"));
    }

    static int Total(Dictionary<string, int> d) { int n = 0; foreach (var kv in d) n += kv.Value; return n; }

    /// <summary>一个 op 的**载荷子类** —— 分类表的第二层。`give` 那一栏最需要它
    /// （属性增减益 / 关键词授予 / 嵌入效果 是三条完全不同的结算路）。</summary>
    static string PayloadClassOf(EffectOp op)
    {
        if (op.Verb == "give" || op.Verb == "gain" || op.Verb == "lose")
        {
            var ps = GivePayload.Parse(op.Payload);
            if (ps == null) return "(载荷认不出)";
            var parts = new List<string>();
            foreach (var p in ps)
                parts.Add(p.IsEmbedded ? "嵌入效果" : (p.IsKeyword ? "关键词" : "属性增减益"));
            return string.Join("+", parts.ToArray());
        }
        if (op.Verb == "deal" || op.Verb == "heal") return op.AmountMax > 0 ? "区间值" : "定值";
        if (op.Verb == "create") return op.Dest;
        if (op.Verb == "deploy") return op.DeployFrom + (op.CostMin > 0 || op.CostMax > 0 ? "+费用区间" : "");
        if (op.Verb == "chooseone") return "选项数 " + op.Amount;
        if (op.Verb == "repeat") return string.IsNullOrEmpty(op.Payload) ? "无条件" : "有条件";
        if (op.Verb == "stun" || op.Verb == "blind") return "硬控";
        return "";
    }

    /// <summary>附录 C 的骰子表 vs 卡池实算的池子。**差集两个方向都要报** ——
    /// 「书上有、卡池没有」是原版数据缺口，「卡池有、书上没列」是名字对不上或我们的筛子开宽了。</summary>
    static void CheckDiceTables(IReadOnlyList<CardDef> pool)
    {
        var cases = new[]
        {
            // 表名,              卡面原文（走真解析）,                                    施放者阵营,          附录 B 的「几种」
            new[] { "战斗药剂(1d6)",   "Create 3 random Combat Elixir in your hand",        "EmperorsChildren", "6" },
            new[] { "钛无人机(1d7)",   "Create 3 random Drones in your hand",               "TauEmpire",        "7" },
            new[] { "虫群大军(1d10)",  "Create 3 random Leviathan troops with Swarm in your hand", "Leviathan", "10" },
            new[] { "锈蚀通风口(1d11)", "Create a random troop with Ambush in your hand",    "Genestealers",     "11" },
            new[] { "狂野宿主(1d20)",  "Create two random Saim-Hann Vehicles in your hand", "SaimHann",         "20" },
            // ---- deploy 那几条（`Deploy` 和 `Create` 共用同一套候选池）----
            new[] { "次元裂隙(1d6)",   "Deploy 3 random 2-cost Sautekh troops",              "Sautekh",          "6" },
            new[] { "纵欲狂欢(1d5)",   "Deploy three random Emperor's Children Daemons that cost 5 or less",
                                                                                            "EmperorsChildren", "5" },
            new[] { "空中播种(1d5)",   "Deploy 4 random 2-cost Leviathan troops",             "Leviathan",        "5" },
        };

        foreach (var cs in cases)
        {
            string tableName = cs[0];
            string[] book = null;
            foreach (var t in CreatePool.DiceTables) if (t[0] == tableName) book = t;
            CheckTrue(book != null, $"附录 C 里有「{tableName}」这张表");

            var got = PoolOf(pool, cs[1], cs[2]);
            var gotNames = new HashSet<string>();
            foreach (var c in got.Cards) gotNames.Add(CreatePool.Norm(c.Name));
            var bookNames = new HashSet<string>();
            for (int i = 1; i < book.Length; i++) bookNames.Add(CreatePool.Norm(book[i]));

            var onlyBook = new List<string>();
            var onlyBookRaw = new List<string>();
            for (int i = 1; i < book.Length; i++)
                if (!gotNames.Contains(CreatePool.Norm(book[i]))) { onlyBook.Add(CreatePool.Norm(book[i])); onlyBookRaw.Add(book[i]); }
            var onlyData = new List<string>();
            foreach (var n in gotNames) if (!bookNames.Contains(n)) onlyData.Add(n);
            onlyBook.Sort(); onlyData.Sort();

            // 附录 B 的「N 种」是第三个尺子：卡池实算的池子该和它一样大
            // 「书上有、卡池对不上」的，顺带报出卡池里**最接近**的那个名字（多半是换个写法）
            var nearMiss = new List<string>();
            foreach (var n in onlyBookRaw)
            {
                string near = CreatePool.FindNearMiss(pool, n);
                nearMiss.Add(n + "→" + (near != null ? near : "(真没有)"));
            }

            Debug.Log(P + $"   附录C「{tableName}」：卡池算得 {gotNames.Count} 张、"
                      + $"书里列 {bookNames.Count} 个名字、附录B 写「{cs[3]} 种」"
                      + (nearMiss.Count > 0 ? $"；**书里有、卡池对不上**：{string.Join("、", nearMiss)}" : "")
                      + (onlyData.Count > 0 ? $"；**卡池有、书里没列**：{string.Join("、", onlyData)}" : ""));

            CheckTrue(gotNames.Count > 0, $"「{tableName}」的池子在卡池里算得出来");
        }

        // 上面那几处已经查出实打实的差：**如实钉在这儿**，别让它悄悄漂走。
        // 「书上有、卡池没有」= 原版数据缺口；「卡池有、书上没列」= 我们的筛子可能开宽了。
        //
        // ⚠️ 2026-09-13 更正：这一处**原来的差已经修掉了** —— 卡面逐张核对发现
        //    `Shivversplint` 的 subtype 在 OCR 源表里是 `Upgrade`，**卡面印的是 `Combat Elixir`**
        //    （见 `CARD_FACE_FIXES_SRC`）。现在附录 C 的 1d6 六个名字全在池里，
        //    所以这条断言从「钉住缺口」改成「钉住已修」。
        var elixir = CreatePool.Resolve(pool, "random combat elixir", "EmperorsChildren");
        CheckTrue(HasCard(elixir.Cards, "Shivversplint"),
                  "`Shivversplint` **在战斗药剂池里**（2026-09-13 起：subtype 已按卡面改成 `Combat Elixir`）");
        CheckTrue(HasCard(pool, "Shivversplint"), "……而且它本来就在卡池里");

        var swarm = CreatePool.Resolve(pool, "random leviathan troops with swarm", "Leviathan");
        CheckTrue(HasCard(swarm.Cards, "Tyranid Prime"),
                  "`Tyranid Prime` 带虫群、是利维坦部队，但附录 C 的 1d10 名单里没有它（卡池 11 vs 书上 10）");

        // 附录 C 的 1d11 名单里写着 `Acolyte Hybrid`，而卡池里那一格是 `Aberrant`。
        // 查清楚了才敢下结论：**`Acolyte Hybrid` 这张卡在卡池里**，只是**身上没有伏击关键词** ——
        // 所以它进不了「带伏击部队」的池子。这是**数据差异**（名字对不上 / 关键词挂在哪张卡上），
        // 不是我们筛错了。**不做别名映射**（没有第二条证据说这两张是同一张），如实钉在这儿。
        var acolyte = CreatePool.FindByName(pool, "Acolyte Hybrid");
        CheckTrue(acolyte != null, "`Acolyte Hybrid` 这张卡本身在卡池里");
        CheckTrue(!acolyte.Has("ambush"), "……但它身上**没有伏击关键词**，所以不在「带伏击部队」池里");
        CheckTrue(!HasCard(PoolOf(pool, "Create a random troop with Ambush in your hand", "Genestealers").Cards,
                           "Acolyte Hybrid"),
                  "……于是它确实没进那个池子（不是筛子漏了它）");
    }

    /// <summary>
    /// **卡面原文 → 解析 → `Payload` → 候选池**。走真路，不手写 payload ——
    /// 手写的那份和解析器产出的会悄悄不一样（第一版就是这么错的：手写成 `a gun drone, …`，
    /// 而解析器早把冠词当数量剥掉了，于是测试红、代码却是对的）。
    /// </summary>
    static CreatePoolResult PoolOf(IReadOnlyList<CardDef> pool, string cardText, string faction,
                                   bool unitsOnly = false, int costMin = 0, int costMax = 0)
    {
        // `create` 和 `deploy` 共用同一套候选池 —— 两边的卡面文本都从这儿进
        EffectOp found = null;
        foreach (string seg in EffectText.Split(cardText))
        {
            var r = EffectText.ParseSegment(seg);
            if (r.Ops == null) continue;
            foreach (var op in r.Ops)
                if (op.Verb == "create" || op.Verb == "deploy") found = op;
        }
        CheckTrue(found != null, $"「{cardText}」里解析得出 create/deploy");
        // 卡面自带的费用区间**也要带上**（别只信调用方传的）
        if (found.CostMin > costMin) costMin = found.CostMin;
        if (found.CostMax > costMax) costMax = found.CostMax;
        return CreatePool.Resolve(pool, found.Payload, faction, unitsOnly, costMin, costMax);
    }

    static bool HasCard(IEnumerable<CardDef> list, string name)
    {
        string want = CreatePool.Norm(name);
        foreach (var c in list) if (c != null && CreatePool.Norm(c.Name) == want) return true;
        return false;
    }

    static void CheckAll(List<CardDef> list, Func<CardDef, bool> ok, string msg)
    {
        foreach (var c in list)
            if (!ok(c)) { CheckTrue(false, msg + $"（不满足的是 {c.Name}）"); return; }
        CheckTrue(true, msg);
    }

    /// <summary>解析一句话，要求恰好产出 1 条 op</summary>
    static EffectOp OneOp(string seg)
    {
        var r = EffectText.ParseSegment(seg);
        Check(r.Kind, EffectText.SegKind.Ok, $"「{seg}」解析成功");
        Check(r.Ops == null ? 0 : r.Ops.Count, 1, $"「{seg}」解析出 1 条效果");
        return r.Ops[0];
    }

    /// <summary>
    /// 把**全部**未解析/半懂/缺机制的句子按频次降序落盘 → `d:/4/_tmp_view/tactic_unparsed.txt`。
    ///
    /// 为什么要落盘：日志里只印 TOP8，而「下一个该实现哪个 handler」必须**按量排**。
    /// 落 `_tmp_view/` 而不是 `Assets/`：这是排查产物，不是资产。
    /// </summary>
    static void DumpUnparsed(EffectText.TextCoverage cov)
    {
        var sb = new StringBuilder();
        sb.AppendLine("战术卡文本解析 —— 未覆盖清单（按频次降序）");
        sb.AppendLine(cov.Summary());
        sb.AppendLine();

        Append(sb, "① 完全不认识的句子（还没有 handler 认领）", cov.UnknownFreq);
        Append(sb, "② 句型认了、但目标/载荷词表里没有（半懂 —— 比不懂更危险）", cov.PartialFreq);
        Append(sb, "③ 解析得了、但载荷的关键词没有机制（能打但没用）", cov.NoMechFreq);
        // 2026-09-12：原版 `card_stats.json` 里的 `subtype`（兵种）接进来之后，
        // `a friendly Vehicle` 这类**真能筛了**（`EffectTargetSpec.SubtypeFilter` → `ResolveTargets`），
        // 所以这一栏只剩「原版数据里也没有对应兵种」的少数卡（过去是 12 张，现在 1 张）。
        Append(sb, "④ 会生效、但**打得比卡面宽**（兵种词在 `subtype` 里找不到对应值，只能按整个目标池打）",
               cov.ImpreciseFreq);

        sb.AppendLine();
        sb.AppendLine("⑤ 完全解析不了的卡（卡面该打 `*`）：");
        sb.AppendLine("   " + string.Join("、", cov.NoneCards));

        const string path = "d:/4/_tmp_view/tactic_unparsed.txt";
        System.IO.File.WriteAllText(path, sb.ToString(), System.Text.Encoding.UTF8);
        Debug.Log(P + "   全量清单写到 " + path
                  + $"（不认 {cov.UnknownFreq.Count} 种 / 半懂 {cov.PartialFreq.Count} 种 / 缺机制 {cov.NoMechFreq.Count} 种）");
    }

    static void Append(StringBuilder sb, string title, Dictionary<string, int> freq)
    {
        var list = new List<KeyValuePair<string, int>>(freq);
        list.Sort((a, b) => b.Value.CompareTo(a.Value));
        int total = 0;
        foreach (var kv in list) total += kv.Value;
        sb.AppendLine("──── " + title + $"（{list.Count} 种 / {total} 次）");
        foreach (var kv in list) sb.AppendLine($"  ×{kv.Value,-4} {kv.Key}");
        sb.AppendLine();
    }

    /// <summary>
    /// **存档卡组 → 对战**（`DeckBuilder.FromDeck`）。
    /// 卡组编辑器存的是 id；这条链就是把它展开成引擎要的卡表。
    /// </summary>
    static void TestDeckIntoBattle()
    {
        var pool = CardDatabase.Load();
        var um = CardDatabase.OfFaction(pool, "Ultramarines");

        CardDef hero = null, defence = null;
        var unitIds = new List<string>();
        foreach (var c in um)
        {
            if (hero == null && c.Type == "hero") { hero = c; continue; }
            if (defence == null && c.Type == "defence") { defence = c; continue; }
            if (c.Type == "unit" && unitIds.Count < DeckRules.ClassicCards) unitIds.Add(c.Id);
        }
        CheckTrue(hero != null && defence != null && unitIds.Count == DeckRules.ClassicCards,
                  $"选料齐了：督军 {hero?.Name}、防御卡 {defence?.Name}、单位 {unitIds.Count} 张");

        var deck = new PlayerDeck("自检套", hero.Id, defence.Id, unitIds);
        // ⚠️ **解析卡组引用只有一处**（`CardDatabase.DeckLookup`，2026-09-13 第三十三轮）：
        //    先按**稳定 id**，再退回「卡名 + 阵营」。2026-09-13 那次的坑是：卡组里存的是卡名，
        //    而原版有跨阵营同名卡（`Terminator` / `Bladeguard Veteran` …），只按名字查会撞上
        //    **另一个阵营**那张 ⇒ 判 `WrongFaction`。
        Check(DeckRules.Validate(deck, CardDatabase.DeckLookup(pool, "Ultramarines")), DeckError.None,
              "这副卡组是合法的（`DeckRules.Validate` 说了算）");

        var skipped = new List<string>();
        var cards = DeckBuilder.FromDeck(pool, deck, skipped, "Ultramarines");
        // 2026-09-13 第三十三轮：防御卡**不再被丢掉**，它和督军一样进牌表（独立的一格）
        Check(cards.Count, 2 + DeckRules.ClassicCards,
              $"展开成 {cards.Count} 张（1 督军 + 1 防御卡 + {DeckRules.ClassicCards} 单位）");
        Check(cards[0].Type, "hero", "第 0 张是督军（`RuleCore.BuildPlayer` 认这个约定）");
        Check(skipped.Count, 0, "没有卡被丢掉（防御卡现在**进得去**了）");
        Debug.Log(P + "   引擎还不支持、被丢掉的：" + string.Join("、", skipped));

        var ctx = RuleCore.NewBattle(cards, cards, seed: 4242);
        Check(ctx.Players[0].Warlord.Name, hero.Name, "开出来的局，督军就是卡组里那个");
        // ---- ✅ 防御卡：进**手牌**、不进牌库（规则书 `:105`/`:121`）----
        var dfcInHand = ctx.Players[0].Hand.Find(x => x != null && x.Card.Type == "defence");
        CheckTrue(dfcInHand != null && dfcInHand.Card.Id == defence.Id,
                  $"防御卡**开局就在手牌里**：「{dfcInHand?.Card.Name}」（不是抽来的，所以不参与洗牌）");
        CheckTrue(!ctx.Players[0].Deck.Exists(x => x.Card.Type == "defence"),
                  "……而且**不在牌库里**（不会出现「第二张防御卡」）");
        Check(ctx.Players[0].Deck.Count + ctx.Players[0].Hand.Count, DeckRules.ClassicCards + 1,
              $"抽牌堆 + 手牌 = {DeckRules.ClassicCards} + 1 张防御卡（卡一张没少）");

        // ---- 🆕 战术卡（2026-09-12 起收）：**能解析干净的收下**、解析不了的照样丢并记下来 ----
        // 判据只有一处：`DeckBuilder.TacticPlayable` → `EffectText.IsFullyParsed`
        // （和 `CanPlayTactic` 是同一份，不会出现「牌组收了、出牌又被拒」）
        var mixed = new List<string>(unitIds);
        int kept = 0, droppedTactic = 0;
        foreach (var c in um)
        {
            if (c.Type != "tactic") continue;
            bool ok = DeckBuilder.TacticPlayable(c);
            if (ok && kept < 3) { mixed.Add(c.Id); kept++; }
            else if (!ok && droppedTactic < 2) { mixed.Add(c.Id); droppedTactic++; }
        }
        CheckTrue(kept > 0, $"Ultramarines 里找得到能解析的战术卡（{kept} 张）");
        var deck2 = new PlayerDeck("自检套·混战术", hero.Id, defence.Id, mixed);
        var skipped2 = new List<string>();
        var cards2 = DeckBuilder.FromDeck(pool, deck2, skipped2);
        Check(cards2.Count, 2 + unitIds.Count + kept, $"能解析的战术卡收下了（+{kept} 张）");
        Check(skipped2.Count, droppedTactic,
              $"解析不了的战术 {droppedTactic} 张 → 记进 skipped（防御卡**不再**被丢）");
        int tacticsIn = 0;
        foreach (var c in cards2) if (c.Type == "tactic") tacticsIn++;
        Check(tacticsIn, kept, "收下的确实都是战术卡");
    }

    // ==================================================================
    //  开局
    // ==================================================================

    static void TestNewBattle()
    {
        var ctx = Battle(new[] { Unit("A", 1, 1, 1), Unit("B", 1, 1, 1), Unit("C", 1, 1, 1) },
                         new[] { Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1) });

        Check(ctx.Players[0].Hand.Count, 3, "起手 3 张（先手）");
        Check(ctx.Players[1].Hand.Count, 3, "起手 3 张（后手）");
        Check(ctx.Players[0].Warlord.Health, 30, "督军 30 血");
        Check(ctx.Players[0].Warlord.Attack, 2, "督军 2 攻");
        Check(ctx.Players[0].Energy, 0, "开局能量 0");
        Check(ctx.Active, 0, "先手 = 玩家 1");
        Check(ctx.Turn, 0, "还没开始算回合");

        CheckTrue(ctx.Players[0].Board[BoardSpec.WarlordSlot] == ctx.Players[0].Warlord,
                  "督军就在槽 4，且和 Warlord 是同一个对象");
        Check(Board(ctx, 0, 0), null, "格 0 空");
        Check(Board(ctx, 0, BoardSpec.WarlordSlot).Name, "FixtureWarlord",
              "督军卡被从牌组提出来当了督军（不在牌库里）");
        Check(ctx.Players[0].Deck.Count, 20, "牌库 = 24 - 督军 1 - 起手 3 = 20");

        // 手牌顺序：Deck() 反转摆放 → 抽到的顺序就是传入顺序
        Check(ctx.Players[0].Hand[0].Card.Name, "A", "起手顺序 = 传入顺序（反转摆放生效）");
        Check(ctx.Players[0].Hand[2].Card.Name, "C", "第 3 张也对得上");
    }

    // ==================================================================
    //  开局换牌（Mulligan）
    //
    //  原版：`_SetupMulliganPhase` → `MulliganManager.ActivateMulligan` →（玩家选完）
    //        `_FinishMulliganFirstPhase` → `_FinishMulliganFinalPhase` → `ShuffleDeck`
    //        → `PlayerHand.CompleteMulliganPhase` → `StartBattlePhase`。
    //  规则：规则书 :46「**可弃回任意起手牌后重洗补抽**」。
    // ==================================================================

    /// <summary>某一方手牌的名字（按顺序）—— 换牌的确定性断言要逐张比</summary>
    static string[] HandNames(BattleContext ctx, int p)
    {
        var names = new List<string>();
        foreach (var c in ctx.Players[p].Hand) names.Add(c.Card.Name);
        return names.ToArray();
    }

    /// <summary>「手牌 + 牌库 + 弃牌堆」全部卡名的**多重集合签名** —— 换牌前后必须一模一样
    /// （凭空多一张或少一张，是这一块最容易出的静默错）</summary>
    static string CardsSignature(BattleContext ctx, int p)
    {
        var names = new List<string>();
        var ps = ctx.Players[p];
        foreach (var c in ps.Hand) names.Add(c.Card.Name);
        foreach (var c in ps.Deck) names.Add(c.Card.Name);
        foreach (var c in ps.Discard) names.Add(c.Card.Name);
        names.Sort();
        return string.Join(",", names.ToArray());
    }

    static void TestMulligan()
    {
        var d0 = new[] { Unit("A", 1, 1, 1), Unit("B", 1, 1, 1), Unit("C", 1, 1, 1) };
        var d1 = new[] { Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1) };

        var ctx = RuleCore.NewBattle(Deck(d0), Deck(d1), seed: 7, shuffle: false, openMulligan: true);
        CheckTrue(ctx.MulliganOpen, "`openMulligan: true` → 开局进换牌阶段");
        Check(ctx.Players[0].Hand.Count, 3, "换牌前：起手 3 张");
        Check(ctx.Players[0].Deck.Count, 20, "换牌前：牌库 20 张");

        string before = CardsSignature(ctx, 0);
        int hand1 = ctx.Players[0].Hand.Count, deck1 = ctx.Players[0].Deck.Count;

        int n = RuleCore.Mulligan(ctx, 0, new List<int> { 1, 2 });
        Check(n, 2, "换掉 2 张");
        Check(ctx.Players[0].Hand.Count, hand1, "**补抽了**：手牌张数不变（3 → 3）");
        Check(ctx.Players[0].Deck.Count, deck1, "牌库张数也不变（弃回 2 + 抽回 2）");
        Check(CardsSignature(ctx, 0), before, "**牌一张不多一张不少**（手牌+牌库+弃牌堆的签名不变）");

        // 越界/重复的下标：忽略，不报错也不重复删
        int m = RuleCore.Mulligan(ctx, 0, new List<int> { 0, 0, 99, -1 });
        Check(m, 1, "重复下标只算一次、越界下标被忽略（换掉 {0,0,99,-1} → 实际 1 张）");
        Check(CardsSignature(ctx, 0), before, "……牌还是不多不少");

        // 空列表 = 不换（原版「什么都不选直接继续」就是这个）
        Check(RuleCore.Mulligan(ctx, 0, new List<int>()), 0, "空列表 → 换 0 张（合法，不是错误）");

        // ⚠️ **不在换牌阶段就换不了** —— 而且必须**报出来**（返回 -1），不能静默成功
        RuleCore.EndMulligan(ctx);
        CheckTrue(!ctx.MulliganOpen, "`EndMulligan` 之后阶段关闭");
        string sig2 = CardsSignature(ctx, 0);
        int after = RuleCore.Mulligan(ctx, 0, new List<int> { 0 });
        Check(after, -1, "对局开始后再换 → 返回 **-1**（不是 0，也不是换成功）");
        Check(CardsSignature(ctx, 0), sig2, "……而且手牌一张没动");

        // 确定性：同样的种子 + 同样的换法 → 同样的结果
        var c1 = RuleCore.NewBattle(Deck(d0), Deck(d1), seed: 7, shuffle: false, openMulligan: true);
        var c2 = RuleCore.NewBattle(Deck(d0), Deck(d1), seed: 7, shuffle: false, openMulligan: true);
        RuleCore.Mulligan(c1, 0, new List<int> { 0 });
        RuleCore.Mulligan(c2, 0, new List<int> { 0 });
        Check(string.Join("/", HandNames(c1, 0)), string.Join("/", HandNames(c2, 0)),
              "同种子同换法 → 同一副手牌（换牌走的是 `ctx.Rng`，对局可复现）");
        // 默认不开：老调用方（规则自检里那一大堆）行为不变
        var plain = RuleCore.NewBattle(Deck(d0), Deck(d1), seed: 7, shuffle: false);
        CheckTrue(!plain.MulliganOpen, "不传 `openMulligan` → **不进换牌阶段**（老用例不受影响）");
    }

    // ==================================================================
    //  回合
    // ==================================================================

    static void TestBeginTurn()
    {
        var ctx = Battle(new[] { Unit("A", 1, 1, 1), Unit("B", 1, 1, 1), Unit("C", 1, 1, 1) },
                         new[] { Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1) });

        RuleCore.BeginTurn(ctx);
        Check(ctx.Turn, 1, "回合 1");
        Check(ctx.Players[0].Energy, 2, "回合 1 能量 2（初始 1 + 每回合 +1）");
        Check(ctx.Players[0].Hand.Count, 4, "抽 1 张");
        Check(ctx.Players[0].TurnCount, 1, "P1 自己的回合计数 = 1");

        // 解疲劳只作用于**当前行动方**
        Place(ctx, 0, 1, Unit("Mine", 1, 1, 1), exhausted: true);
        Place(ctx, 1, 1, Unit("Theirs", 1, 1, 1), exhausted: true);

        PassTurn(ctx);            // → P2 第 1 回合
        Check(Board(ctx, 1, 1).Exhausted, false, "P2 的回合开始时，P2 的单位解疲劳");
        Check(Board(ctx, 0, 1).Exhausted, true, "P1 的单位在 P2 的回合里不解疲劳");

        PassTurn(ctx);            // → P1 第 2 回合
        Check(Board(ctx, 0, 1).Exhausted, false, "轮到 P1 时己方单位解疲劳");
        Check(ctx.Players[0].Energy, 3, "P1 第 2 回合能量 3（自己的回合数 + 1）");
    }

    static void TestEnergyIsPerPlayer()
    {
        var ctx = Battle(new[] { Unit("A", 1, 1, 1), Unit("B", 1, 1, 1), Unit("C", 1, 1, 1) },
                         new[] { Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1) });

        RuleCore.BeginTurn(ctx);   // P1 第 1 回合
        Check(ctx.Players[0].Energy, 2, "P1 第 1 回合 = 2 能");
        PassTurn(ctx);             // P2 第 1 回合
        Check(ctx.Players[1].Energy, 2, "P2 第 1 回合也是 2 能（**不是 3**）");
        Check(ctx.Players[1].TurnCount, 1, "P2 自己的回合计数 = 1");
        // ⚠️ 这条最容易写错：按**全局回合数**算的话，后手首回合会变成 2 能、先手第 2 回合变成 3 能，
        //    整局能量曲线偏高。rule_core.gd 记着这条修正。
    }

    // ==================================================================
    //  出牌
    // ==================================================================

    static void TestPlayCostRejected()
    {
        var ctx = Battle(new[] { Unit("Cheap", 2, 1, 1), Unit("Pricey", 5, 5, 5), Unit("Free", 0, 1, 1) },
                         new[] { Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1) });
        ToP1Turn(ctx, 1);
        Check(ctx.Players[0].Energy, 2, "手上有 2 能");

        int handBefore = ctx.Players[0].Hand.Count;
        int code = RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "Pricey"), 1);
        CheckCode(code, RuleCodes.ErrCost, "5 费卡在 2 能时被拒绝");
        Check(ctx.Players[0].Energy, 2, "拒绝后能量不变");
        Check(ctx.Players[0].Hand.Count, handBefore, "拒绝后手牌不变");
        Check(Board(ctx, 0, 1), null, "拒绝后格位仍空");
    }

    static void TestPlaySlotRejected()
    {
        var ctx = Battle(new[] { Unit("Cheap", 2, 1, 1), Unit("Free", 0, 1, 1), Unit("Free2", 0, 1, 1) },
                         new[] { Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1) });
        ToP1Turn(ctx, 1);

        CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "Cheap"), BoardSpec.WarlordSlot),
                  RuleCodes.ErrSlot, "督军格不可部署");
        Check(ctx.Players[0].Energy, 2, "非法格不扣费");
        Check(ctx.Players[0].Hand.Count, 4, "非法格不弃牌");

        CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "Cheap"), -1),
                  RuleCodes.ErrSlot, "格位 -1 被拒绝");
        CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "Cheap"), BoardSpec.Size),
                  RuleCodes.ErrSlot, "格位 9（越界）被拒绝");
        Check(ctx.Players[0].Energy, 2, "越界也不扣费");
    }

    static void TestPlayOccupiedRejected()
    {
        var ctx = Battle(new[] { Unit("Free", 0, 1, 1), Unit("Free2", 0, 1, 1), Unit("Free3", 0, 1, 1) },
                         new[] { Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1) });
        ToP1Turn(ctx, 1);

        CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "Free"), 1), RuleCodes.OK, "格 1 部署");
        CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "Free2"), 1),
                  RuleCodes.ErrSlot, "被占格拒绝（0 费卡，排除了费用干扰）");

        // 全 9 格只有督军格不能放，其余 8 格都能放
        Check(BoardSpec.SlotsPerSide * 2, 8, "两侧共 8 个可部署格");
    }

    static void TestPlayOk()
    {
        var ctx = Battle(new[] { Unit("Grunt", 2, 3, 4), Unit("F", 1, 1, 1), Unit("G", 1, 1, 1) },
                         new[] { Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1) });
        ToP1Turn(ctx, 2);

        Check(ctx.Players[0].Energy, 3, "P1 第 2 回合 3 能");
        int handBefore = ctx.Players[0].Hand.Count;
        int idx = HandIdx(ctx, 0, "Grunt");
        CheckCode(RuleCore.PlayCard(ctx, 0, idx, 1), RuleCodes.OK, "2 费单位在 3 能时可部署");
        Check(ctx.Players[0].Energy, 1, "能量 3-2=1");

        var u = Board(ctx, 0, 1);
        CheckTrue(u != null, "格 1 有单位了");
        Check(u.Name, "Grunt", "上去的是 Grunt");
        Check(u.Health, 4, "满血上场（不是卡面血量以外的值）");
        Check(u.Exhausted, true, "部署当回合不可行动");
        Check(ctx.Players[0].Hand.Count, handBefore - 1, "手牌少了一张");

        // CanPlayCard 和 PlayCard 必须是同一份判据（表现层拖拽时要实时问它）
        CheckCode(RuleCore.CanPlayCard(ctx, 0, HandIdx(ctx, 0, "F"), 1), RuleCodes.ErrSlot,
                  "CanPlayCard 和 PlayCard 判据一致（被占格）");
        ctx.Players[0].Energy = 0;
        CheckCode(RuleCore.CanPlayCard(ctx, 0, HandIdx(ctx, 0, "F"), 2), RuleCodes.ErrCost,
                  "能量归零后 1 费卡判为能量不足");

        // 非本方回合
        CheckCode(RuleCore.PlayCard(ctx, 1, 0, 1), RuleCodes.ErrNotTurn, "非本方回合不能出牌");

        // 手牌索引越界
        CheckCode(RuleCore.PlayCard(ctx, 0, 99, 2), RuleCodes.ErrBadHand, "手牌索引越界");
    }

    // ==================================================================
    //  攻击
    // ==================================================================

    /// <summary>造一个「P1 有 A、P2 有 B，且轮到 P1」的场景</summary>
    static BattleContext Duel(CardDef a, CardDef b, out UnitState ua, out UnitState ub,
                              int aSlot = 1, int bSlot = 1)
    {
        var ctx = Battle(new[] { a, Unit("F1", 1, 1, 1), Unit("F2", 1, 1, 1) },
                         new[] { b, Unit("G1", 1, 1, 1), Unit("G2", 1, 1, 1) });
        ToP1Turn(ctx, 3);
        ua = Place(ctx, 0, aSlot, a);
        ub = Place(ctx, 1, bSlot, b);
        return ctx;
    }

    static void TestMeleeTrade()
    {
        UnitState a, b;
        var ctx = Duel(Unit("A", 1, 1, 1), Unit("B", 1, 1, 1), out a, out b);

        CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 1), RuleCodes.OK, "1v1 近战可结算");
        Check(Board(ctx, 0, 1), null, "攻击者被反击同归于尽");
        Check(Board(ctx, 1, 1), null, "目标死亡");
        Check(ctx.Players[0].Discard.Count, 1, "攻击者进弃牌堆");
        Check(ctx.Players[1].Discard.Count, 1, "目标进弃牌堆");
    }

    static void TestArmorMinimumOne()
    {
        UnitState a, b;
        // 3 攻打 1 甲 5 血 → 3-1 = 2 伤 → 剩 3
        var ctx = Duel(Unit("A", 1, 3, 5), Unit("B", 1, 5, 5, "Armour 1"), out a, out b);
        Check(b.Armor, 1, "Armour 1 解析成护甲 1");

        CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 1), RuleCodes.OK, "近战结算");
        Check(Board(ctx, 1, 1).Health, 3, "3 攻 vs 1 甲 → 2 伤害（5→3）");
        // 反击：B 5 攻打 A 无甲 → 5 伤，A 5 血死
        Check(Board(ctx, 0, 1), null, "反击 5 伤杀死 5 血攻击者");

        // 最低 1：1 攻打 3 甲 → 仍然造成 1
        UnitState c, d;
        var ctx2 = Duel(Unit("C", 1, 1, 5), Unit("D", 1, 5, 5, "Armour 3"), out c, out d);
        Check(d.Armor, 3, "Armour 3 → 护甲 3");
        RuleCore.DeclareAttack(ctx2, 0, 1, 1, 1);
        Check(Board(ctx2, 1, 1).Health, 4, "1 攻打 3 甲 → 减免到最低 **1**（不是 0）");
    }

    static void TestShieldBlocks()
    {
        UnitState a, b;
        // 攻击者做成 20 血，好让它挨得住反击 —— 不然第一刀之后它自己就死了，验不了第二刀
        var ctx = Duel(Unit("A", 1, 3, 20), Unit("B", 1, 5, 5, "Shield"), out a, out b);
        Check(b.HasShield, true, "Shield 关键词 → HasShield");

        RuleCore.DeclareAttack(ctx, 0, 1, 1, 1);
        Check(Board(ctx, 1, 1).Health, 5, "Shield 挡下全部伤害（血没动）");
        Check(Board(ctx, 1, 1).HasShield, false, "Shield 用掉了");
        Check(Board(ctx, 0, 1).Health, 15, "攻击者照样吃了 B 的 5 点反击");

        // 第二次就不再挡了
        // ⚠️ 要连**攻击配额**一起重置（`RefreshForNewTurn` 干的就是这件事）——
        //    只把 `Exhausted` 置 false 的话，新的配额检查（`AttacksThisTurn >= 1`）会把这一刀挡掉
        Board(ctx, 0, 1).RefreshForNewTurn();
        RuleCore.DeclareAttack(ctx, 0, 1, 1, 1);
        Check(Board(ctx, 1, 1).Health, 2, "Shield 只挡一次，第二次照常吃 3 伤（5→2）");
    }

    static void TestRangedEatsCounter()
    {
        UnitState a, b;
        // 远程 2 攻、2 血；目标 1 攻 5 血（远程没打死 → 吃反击）
        var ctx = Duel(Ranged("Sniper", 2, 0, 2, 2), Unit("Rat", 1, 1, 5), out a, out b);
        Check(a.RangedAttack, 2, "远程攻击力 2 进了单位状态");

        CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 1, ranged: true), RuleCodes.OK, "远程攻击");
        Check(Board(ctx, 1, 1).Health, 3, "远程 2 攻打 5 血目标 → 剩 3");
        Check(Board(ctx, 0, 1).Health, 1, "**远程未击杀 → 照样吃目标反击 1 伤**（2→1）");
        // ⚠️⚠️ 这条最容易写反。直觉是「远程不受反击」，rule_core.gd 有明确修正记录：
        //     「2026-08-21 修正：此前远程完全不吃反击，Long Range/Sniper 成死代码」
    }

    static void TestLongRangeNoCounter()
    {
        UnitState a, b;
        var ctx = Duel(Ranged("LR", 2, 0, 2, 2, "Long Range"), Unit("Rat", 1, 1, 5), out a, out b);
        CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 1, ranged: true), RuleCodes.OK, "Long Range 远程攻击");
        Check(Board(ctx, 0, 1).Health, 2, "Long Range 免反击（血没动）");

        // 近战攻击者不带 Long Range 时就照常吃反击
        UnitState c, d;
        var ctx2 = Duel(Unit("Melee", 1, 2, 2, "Long Range"), Unit("Rat2", 1, 1, 5), out c, out d);
        RuleCore.DeclareAttack(ctx2, 0, 1, 1, 1, ranged: false);
        Check(Board(ctx2, 0, 1).Health, 1, "Long Range 只在**远程**攻击时免反击，近战照吃");
    }

    // ==================================================================
    //  目标合法性
    // ==================================================================

    static void TestVanguardRestriction()
    {
        var ctx = Battle(new[] { Unit("A", 1, 5, 5), Unit("F1", 1, 1, 1), Unit("F2", 1, 1, 1) },
                         new[] { Unit("Van", 1, 1, 5, "Vanguard"), Unit("Plain", 1, 1, 5), Unit("G2", 1, 1, 1) });
        ToP1Turn(ctx, 3);
        Place(ctx, 0, 1, Unit("A", 1, 5, 5));
        Place(ctx, 1, 1, Unit("Van", 1, 1, 5, "Vanguard"));
        Place(ctx, 1, 2, Unit("Plain", 1, 1, 5));

        CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 2), RuleCodes.ErrTarget,
                  "敌方有 Vanguard 时，不能打普通单位");
        CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 1), RuleCodes.OK,
                  "敌方有 Vanguard 时，可以打 Vanguard");

        // Vanguard 死了之后限制解除
        var ctx2 = Battle(new[] { Unit("A", 1, 5, 5), Unit("F1", 1, 1, 1), Unit("F2", 1, 1, 1) },
                          new[] { Unit("Van", 1, 1, 1, "Vanguard"), Unit("Plain", 1, 1, 5), Unit("G2", 1, 1, 1) });
        ToP1Turn(ctx2, 3);
        Place(ctx2, 0, 1, Unit("A", 1, 5, 5));
        Place(ctx2, 1, 1, Unit("Van", 1, 1, 1, "Vanguard"));
        Place(ctx2, 1, 2, Unit("Plain", 1, 1, 5));
        RuleCore.DeclareAttack(ctx2, 0, 1, 1, 1);      // 秒掉 Vanguard
        Check(Board(ctx2, 1, 1), null, "Vanguard 被秒");
        Board(ctx2, 0, 1).RefreshForNewTurn();     // 连攻击配额一起重置（见 TestShieldBlocks 那条注释）
        CheckCode(RuleCore.DeclareAttack(ctx2, 0, 1, 1, 2), RuleCodes.OK,
                  "Vanguard 没了之后就能打普通单位");
    }

    static void TestStealthUntargetable()
    {
        var ctx = Battle(new[] { Unit("A", 1, 5, 5), Unit("F1", 1, 1, 1), Unit("F2", 1, 1, 1) },
                         new[] { Unit("Sneak", 1, 1, 5, "Stealth"), Unit("Plain", 1, 1, 5), Unit("G2", 1, 1, 1) });
        ToP1Turn(ctx, 3);
        Place(ctx, 0, 1, Unit("A", 1, 5, 5));
        Place(ctx, 1, 1, Unit("Sneak", 1, 1, 5, "Stealth"));
        Place(ctx, 1, 2, Unit("Plain", 1, 1, 5));

        CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 1), RuleCodes.ErrTarget, "隐身单位不可被攻击");
        CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 2), RuleCodes.OK, "打旁边的普通单位没问题");

        // 隐身单位自己一攻击就现身
        Place(ctx, 0, 3, Unit("Prey", 1, 1, 9));
        PassTurn(ctx);                    // 轮到 P2（顺便把 P2 的单位解疲劳）
        CheckCode(RuleCore.DeclareAttack(ctx, 1, 1, 0, 3), RuleCodes.OK, "隐身单位自己可以攻击");
        Check(Board(ctx, 1, 1).Has("stealth"), false, "隐身单位攻击后现身（失去 Stealth）");
    }

    static void TestFlyingMeleeBlocked()
    {
        var ctx = Battle(new[] { Unit("Ground", 1, 5, 5), Unit("F1", 1, 1, 1), Unit("F2", 1, 1, 1) },
                         new[] { Unit("Bird", 1, 1, 5, "Flying"), Unit("Plain", 1, 1, 5), Unit("G2", 1, 1, 1) });
        ToP1Turn(ctx, 3);
        Place(ctx, 0, 1, Unit("Ground", 1, 5, 5));
        Place(ctx, 1, 1, Unit("Bird", 1, 1, 5, "Flying"));
        Place(ctx, 1, 2, Unit("Plain", 1, 1, 5));

        CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 1, ranged: false), RuleCodes.ErrTarget,
                  "地面单位**近战**打不到飞行单位");
        // 远程可以
        var ctxR = Battle(new[] { Ranged("Archer", 1, 0, 5, 3), Unit("F1", 1, 1, 1), Unit("F2", 1, 1, 1) },
                          new[] { Unit("Bird", 1, 1, 5, "Flying"), Unit("Plain", 1, 1, 5), Unit("G2", 1, 1, 1) });
        ToP1Turn(ctxR, 3);
        Place(ctxR, 0, 1, Ranged("Archer", 1, 0, 5, 3));
        Place(ctxR, 1, 1, Unit("Bird", 1, 1, 5, "Flying"));
        CheckCode(RuleCore.DeclareAttack(ctxR, 0, 1, 1, 1, ranged: true), RuleCodes.OK,
                  "远程可以打飞行单位");

        // 同为飞行可以近战互相打
        var ctxF = Battle(new[] { Unit("Bird2", 1, 3, 5, "Flying"), Unit("F1", 1, 1, 1), Unit("F2", 1, 1, 1) },
                          new[] { Unit("Bird", 1, 1, 5, "Flying"), Unit("Plain", 1, 1, 5), Unit("G2", 1, 1, 1) });
        ToP1Turn(ctxF, 3);
        Place(ctxF, 0, 1, Unit("Bird2", 1, 3, 5, "Flying"));
        Place(ctxF, 1, 1, Unit("Bird", 1, 1, 5, "Flying"));
        CheckCode(RuleCore.DeclareAttack(ctxF, 0, 1, 1, 1, ranged: false), RuleCodes.OK,
                  "同为飞行的单位可以近战互相攻击");
        // ⚠️ Flying 检查的是**目标**（飞行单位不能被近战打到），不是攻击者。
        //     rule_core.gd 修正过方向：「此前禁止飞行单位近战打地面、却允许地面近战打飞行」—— 正好反了
    }

    // ==================================================================
    //  技能与触发（2026-09-12）
    //
    //  这块以前**根本没有** —— 「部队卡发动技能」「触发效果」两类特效接不上，
    //  不是特效的问题，是引擎里没有这两种事件。这里逐条验：
    //    · 效果文法解析（解析不出来必须是 null，不许静默吞掉）
    //    · 五个触发类关键词的**时机**（照规则书 :161 那张 61 关键词表）
    //    · 主动技能的行动开销、不吃反击、和攻击目标规则的关系
    //    · 事件流本身（谁、在第几格、什么关键词）—— 表现层就靠它
    //    · 效果链的递归截断（反噬/忏悔互相触发天生是个环）
    // ==================================================================

    static bool HasSignal(BattleContext ctx, EvtKind kind, string keyword = null)
    {
        return FindSignal(ctx, kind, keyword) != null;
    }

    static BattleEvent FindSignal(BattleContext ctx, EvtKind kind, string keyword = null)
    {
        foreach (var e in ctx.Signals)
            if (e.Kind == kind && (keyword == null || e.Keyword == keyword)) return e;
        return null;
    }

    static void TestEffectSpecParsing()
    {
        var d = EffectSpec.Parse("Damage 2 EnemyUnit");
        CheckTrue(d != null, "「Damage 2 EnemyUnit」解析成功");
        if (d != null)
        {
            Check(d.Verb, "damage", "动词 = damage");
            Check(d.Amount, 2, "数值 = 2");
            Check(d.Target, EffectTargets.EnemyUnit, "目标 = enemyunit");
        }

        Check(EffectSpec.Parse("Heal 3 OwnWarlord").Target, EffectTargets.OwnWarlord, "Heal → OwnWarlord");
        Check(EffectSpec.Parse("Draw 2").Verb, "draw", "Draw 2 → draw（不用写目标）");
        Check(EffectSpec.Parse("Damage EnemyWarlord").Amount, 1, "省略数值 = 1");
        Check(EffectSpec.Parse("damage 1 enemyunit").Target, EffectTargets.EnemyUnit,
              "大小写不敏感");
        Check(EffectSpec.Parse("Damage 2 EnemyUnit").Short(), "DMG2 UNIT", "卡面小字 DMG2 UNIT");

        // ⚠️ 解析不出来一律 null —— **不给默认值、不猜**。
        //    「Damage 2」这种缺目标的，猜一个就等于替玩家做决定，而这工程最忌讳静默（见文件头）。
        CheckTrue(EffectSpec.Parse("Deal 3 damage to an enemy") == null,
                  "原版那种自然语言 → null（**故意的**，卡面会标 *）");
        CheckTrue(EffectSpec.Parse("Damage 2") == null, "Damage 没写目标 → null（不猜打谁）");
        CheckTrue(EffectSpec.Parse("Damage 2 SomeGuy") == null, "不认识的目标 → null");
        CheckTrue(EffectSpec.Parse("Frobnicate 1 Self") == null, "不认识的动词 → null");
        CheckTrue(EffectSpec.Parse("Damage -1 Self") == null, "负数值 → null");
        CheckTrue(EffectSpec.Parse("") == null, "空串 → null");
        CheckTrue(EffectSpec.Parse(null) == null, "null → null");
    }

    /// <summary>Rally（集结）：「从手牌部署后触发效果」—— 规则书 :200</summary>
    static void TestRally()
    {
        var rallier = Unit("Rallier", 1, 1, 3, "Rally: Damage 2 EnemyWarlord");
        var ctx = Battle(new[] { rallier, Unit("F1", 1, 1, 1), Unit("F2", 1, 1, 1) },
                         new[] { Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1) });
        ToP1Turn(ctx, 2);

        int foeHp = ctx.Players[1].Warlord.Health;
        ctx.ClearSignals();
        CheckCode(RuleCore.PlayCard(ctx, 0, HandIdx(ctx, 0, "Rallier"), 1), RuleCodes.OK,
                  "Rallier 部署成功");

        Check(ctx.Players[1].Warlord.Health, foeHp - 2, "部署时 Rally 就打出去了（敌方督军 -2）");
        // 事件流：**Play → Deploy → Trigger → Hit**。**挨伤害也要发** ——
        // 表现层光看「血少了」分不出是挨刀、被技能打、还是疲劳。
        // ⚠️ 2026-09-13 起第一条是 `Play`（「打出了这张牌」）—— 加它是为了让**战术卡也有事件**
        //    （以前战术卡打出去一个事件都没有，战斗日志和表现层都看不见它），
        //    单位卡于是变成 Play + Deploy 两条。
        Check(ctx.Signals.Count, 4, "发了四条事件（Play + Deploy + Trigger + Hit）");
        Check(ctx.Signals[0].Kind, EvtKind.Play, "第 1 条是 Play（打出了这张牌）");
        Check(ctx.Signals[0].CardId, "Rallier", "Play 带着卡名");
        Check(ctx.Signals[1].Kind, EvtKind.Deploy, "第 2 条是 Deploy");
        Check(ctx.Signals[1].Slot, 1, "Deploy 带着格位");
        Check(ctx.Signals[1].CardId, "Rallier", "Deploy 带着卡名");
        Check(ctx.Signals[2].Kind, EvtKind.Trigger, "第 3 条是 Trigger");
        Check(ctx.Signals[2].Keyword, KeywordTable.Rally, "Trigger 带着关键词 rally");
        Check(ctx.Signals[2].Slot, 1, "Trigger 带着格位（表现层照它播特效）");
        Check(ctx.Signals[2].CardId, "Rallier", "Trigger 带着卡名");
        Check(ctx.Signals[3].Kind, EvtKind.Hit, "第 4 条是 Hit");
        Check(ctx.Signals[3].Amount, 2, "Hit 带着实际伤害值");
        Check(ctx.Signals[3].Player, 1, "Hit 的归属方是**挨打那边**（敌方督军）");

        // **留档日志**（`Ctx.ActionLog`）也在收：存的是同一份事件流，但**不会被搬走** ——
        // 而且把「出单位卡连着发的 Play + Deploy」**合并成一条**（不然每张单位卡都重复一行）。
        Check(ctx.ActionLog.Count, 3, $"留档日志把 Play+Deploy 合并了（{ctx.ActionLog.Count} 条：Play / Trigger / Hit）");
        Check(ctx.ActionLog[0].Kind, EvtKind.Play, "留档里第 1 条是 Play（不是 Deploy）");

        // 没写 `Rally:` 效果的卡不会「触发了个寂寞」
        var plain = Unit("Plain", 1, 1, 3, KeywordTable.Rally);
        var ctx2 = Battle(new[] { plain, Unit("F1", 1, 1, 1), Unit("F2", 1, 1, 1) },
                          new[] { Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1) });
        ToP1Turn(ctx2, 2);
        ctx2.ClearSignals();
        RuleCore.PlayCard(ctx2, 0, HandIdx(ctx2, 0, "Plain"), 1);
        CheckTrue(!HasSignal(ctx2, EvtKind.Trigger), "光有 rally 关键词、没写效果 → 不触发（也不发事件）");
    }

    /// <summary>Strike（猛击）攻击后触发 / Slay（斩杀）摧毁单位后触发 —— 规则书 :208 / :214</summary>
    static void TestStrikeAndSlay()
    {
        // ---- Strike：打没打死都算 ----
        UnitState a, b;
        var ctx = Duel(Unit("Striker", 2, 3, 5, "Strike: Damage 1 EnemyWarlord"),
                       Unit("Dummy", 1, 0, 9), out a, out b);
        ctx.ClearSignals();
        int foe = ctx.Players[1].Warlord.Health;
        CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 1), RuleCodes.OK, "砍一刀（没砍死）");
        Check(Board(ctx, 1, 1).Health, 6, "目标 9 → 6");
        Check(ctx.Players[1].Warlord.Health, foe - 1, "Strike 在攻击后触发（敌方督军 -1）");
        var strike = FindSignal(ctx, EvtKind.Trigger, KeywordTable.Strike);
        CheckTrue(strike != null, "事件流里有 Strike");
        CheckTrue(strike != null && strike.Slot == 1, "Strike 事件带着攻击者的格位");

        // ---- Slay：攻击并**摧毁单位**后触发 ----
        var slayer = Unit("Slayer", 4, 5, 6, "Slay: Damage 2 EnemyWarlord");
        var ctx2 = Duel(slayer, Unit("Victim", 1, 0, 2), out a, out b);
        ctx2.ClearSignals();
        int foe2 = ctx2.Players[1].Warlord.Health;
        CheckCode(RuleCore.DeclareAttack(ctx2, 0, 1, 1, 1), RuleCodes.OK, "斩杀那一刀");
        Check(Board(ctx2, 1, 1), null, "目标被摧毁");
        Check(ctx2.Players[1].Warlord.Health, foe2 - 2, "Slay 触发（敌方督军 -2）");
        CheckTrue(HasSignal(ctx2, EvtKind.Trigger, KeywordTable.Slay), "事件流里有 Slay");

        // ---- 没打死 → 不触发 ----
        var ctx3 = Duel(slayer, Unit("Tank", 1, 0, 9), out a, out b);
        ctx3.ClearSignals();
        int foe3 = ctx3.Players[1].Warlord.Health;
        CheckCode(RuleCore.DeclareAttack(ctx3, 0, 1, 1, 1), RuleCodes.OK, "砍一刀（砍不死）");
        Check(ctx3.Players[1].Warlord.Health, foe3, "没摧毁单位 → Slay 不触发");
        CheckTrue(!HasSignal(ctx3, EvtKind.Trigger, KeywordTable.Slay), "事件流里也没有 Slay");

        // ---- 攻击者自己死了 → 两个都不触发（原文都写着「本单位存活时」）----
        var ctx4 = Duel(slayer, Unit("Brute", 1, 9, 3), out a, out b);
        ctx4.ClearSignals();
        int foe4 = ctx4.Players[1].Warlord.Health;
        CheckCode(RuleCore.DeclareAttack(ctx4, 0, 1, 1, 1), RuleCodes.OK, "同归于尽的一刀");
        Check(Board(ctx4, 0, 1), null, "攻击者阵亡");
        Check(ctx4.Players[1].Warlord.Health, foe4, "攻击者没活下来 → Slay 不触发");
    }

    /// <summary>Backlash（反噬）：「单位死亡时触发效果」—— 规则书 :169</summary>
    static void TestBacklash()
    {
        UnitState a, b;
        var ghost = Unit("Ghost", 2, 0, 3, "Backlash: Damage 2 EnemyWarlord");
        var ctx = Duel(Unit("Killer", 1, 5, 5), ghost, out a, out b);
        ctx.ClearSignals();

        int p1hp = ctx.Players[0].Warlord.Health;     // Ghost 在 P2 场上，反噬打的是 P1 的督军
        CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 1), RuleCodes.OK, "打死反噬单位");
        Check(Board(ctx, 1, 1), null, "Ghost 阵亡");
        Check(ctx.Players[0].Warlord.Health, p1hp - 2, "Backlash 在阵亡时触发，打到对方督军");

        var death = FindSignal(ctx, EvtKind.Death);
        var back = FindSignal(ctx, EvtKind.Trigger, KeywordTable.Backlash);
        CheckTrue(death != null && death.Player == 1 && death.Slot == 1,
                  "Death 事件带着**原格位**（格位马上就空了，不带上就没法播特效）");
        CheckTrue(back != null && back.Player == 1 && back.Slot == 1,
                  "Backlash 事件也带着原格位（单位已经不在棋盘上了）");

        // 先发 Death 再结算反噬 —— 顺序反了的话表现层会在「人已经没了」之后才收到阵亡
        int di = ctx.Signals.IndexOf(death), bi = ctx.Signals.IndexOf(back);
        CheckTrue(di >= 0 && bi > di, "Death 排在 Backlash 前面");
    }

    /// <summary>Penitence（忏悔）：「受到伤害但未死亡时触发效果」—— 规则书 :196</summary>
    static void TestPenitence()
    {
        UnitState a, b;
        var penitent = Unit("Penitent", 2, 0, 5, "Penitence: Damage 1 EnemyWarlord");

        var ctx = Duel(Unit("Poker", 1, 2, 5), penitent, out a, out b);
        ctx.ClearSignals();
        int p1hp = ctx.Players[0].Warlord.Health;
        CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, 1), RuleCodes.OK, "打一下但没打死");
        CheckTrue(Board(ctx, 1, 1) != null, "Penitent 还活着");
        Check(ctx.Players[0].Warlord.Health, p1hp - 1, "受伤未死 → Penitence 触发");

        // 打死了就不触发 —— 「未死亡」是这条的关键字，那种情况归 Backlash 管
        var ctx2 = Duel(Unit("Poker", 1, 9, 5), penitent, out a, out b);
        ctx2.ClearSignals();
        int p1b = ctx2.Players[0].Warlord.Health;
        CheckCode(RuleCore.DeclareAttack(ctx2, 0, 1, 1, 1), RuleCodes.OK, "一击打死");
        Check(Board(ctx2, 1, 1), null, "Penitent 阵亡");
        Check(ctx2.Players[0].Warlord.Health, p1b, "死了就不走 Penitence（那条只管「未死亡」）");
        CheckTrue(!HasSignal(ctx2, EvtKind.Trigger, KeywordTable.Penitence), "事件流里没有 Penitence");

        // Shield 全挡 = 没受伤 → 也不触发
        var shielded = Unit("Shielded", 2, 0, 5, KeywordTable.Shield, "Penitence: Damage 1 EnemyWarlord");
        var ctx3 = Duel(Unit("Poker", 1, 2, 5), shielded, out a, out b);
        ctx3.ClearSignals();
        int p1c = ctx3.Players[0].Warlord.Health;
        CheckCode(RuleCore.DeclareAttack(ctx3, 0, 1, 1, 1), RuleCodes.OK, "打在盾上");
        Check(ctx3.Players[1].Board[1].Health, 5, "Shield 挡住了，一滴血没掉");
        Check(ctx3.Players[0].Warlord.Health, p1c, "没真受伤 → Penitence 不触发");
    }

    /// <summary>主动技能：花掉一次行动、**不吃反击**</summary>
    static void TestAbility()
    {
        UnitState a, b;
        var caster = Unit("Caster", 3, 2, 5, "Ability: Damage 3 EnemyUnit");
        var ctx = Duel(caster, Unit("Prey", 1, 4, 6), out a, out b);
        ctx.ClearSignals();
        int energy = ctx.Players[0].Energy;

        CheckCode(RuleCore.CanUseAbility(ctx, 0, 1, 1), RuleCodes.OK, "有技能、没疲劳、目标合法 → 可以放");
        CheckCode(RuleCore.UseAbility(ctx, 0, 1, 1), RuleCodes.OK, "放技能成功");

        Check(Board(ctx, 1, 1).Health, 3, "Prey 6 → 3（技能 3 点）");
        CheckTrue(Board(ctx, 0, 1).Exhausted, "放技能花掉了这个单位的行动");
        Check(Board(ctx, 0, 1).Health, 5, "**技能不吃反击** —— 施法者一滴血没掉（Prey 4 攻）");
        Check(ctx.Players[0].Energy, energy, "放技能不花能量");

        CheckTrue(HasSignal(ctx, EvtKind.Ability), "事件流里有 Ability —— 这一类以前压根没有");
        var ab = FindSignal(ctx, EvtKind.Ability);
        Check(ab.Slot, 1, "Ability 事件带着施法者的格位");
        Check(ab.CardId, "Caster", "Ability 事件带着卡名");
        Check(ab.Keyword, KeywordTable.Ability, "Ability 事件带着关键词");
        Check(ab.Amount, 3, "Ability 事件带着数值（卡面/日志用）");

        CheckCode(RuleCore.CanUseAbility(ctx, 0, 1, 1), RuleCodes.ErrExhausted, "一回合只能行动一次");
        CheckCode(RuleCore.CanUseAbility(ctx, 1, 1, 1), RuleCodes.ErrNotTurn, "非本方回合不能放技能");

        // 没有技能的单位
        var ctx2 = Duel(Unit("Mundane", 1, 1, 1), Unit("Dummy", 1, 0, 1), out a, out b);
        CheckCode(RuleCore.CanUseAbility(ctx2, 0, 1), RuleCodes.ErrNoAbility, "卡上没写 Ability → ErrNoAbility");
        CheckCode(RuleCore.UseAbility(ctx2, 0, 1), RuleCodes.ErrNoAbility, "UseAbility 也拒绝");
    }

    /// <summary>
    /// 技能**不是攻击** —— 攻击目标那三条限制里只有「潜行」管得着它。
    /// 三条的原文见 `RuleCore.CanUseAbility` 的注释（规则书 :223 / :188 / :211）。
    /// </summary>
    static void TestAbilityTargetRules()
    {
        UnitState a, b;
        var caster = Unit("Caster", 3, 2, 5, "Ability: Damage 3 EnemyUnit");
        var ctx = Duel(caster, Unit("Guard", 1, 1, 9, KeywordTable.Vanguard), out a, out b);

        CheckCode(RuleCore.CanUseAbility(ctx, 0, 1, 1), RuleCodes.OK,
                  "先锋不挡技能（原文是「不能被选为**攻击目标**」）");

        // 飞行同理 —— 原文是「只能被…以**近战攻击**选中」
        Place(ctx, 1, 2, Unit("Bird", 1, 1, 5, KeywordTable.Flying));
        CheckCode(RuleCore.CanUseAbility(ctx, 0, 1, 2), RuleCodes.OK, "飞行也不挡技能");

        // 潜行是「不能被**任何方式**选中」—— 这条管得着
        Place(ctx, 1, 3, Unit("Lurker", 1, 1, 5, KeywordTable.Stealth));
        CheckCode(RuleCore.CanUseAbility(ctx, 0, 1, 3), RuleCodes.ErrTarget, "潜行挡技能");

        // 目标合法性：空格 / 督军 / 越界
        CheckCode(RuleCore.CanUseAbility(ctx, 0, 1, 8), RuleCodes.ErrTarget, "空格 → 目标非法");
        CheckCode(RuleCore.CanUseAbility(ctx, 0, 1, BoardSpec.WarlordSlot), RuleCodes.ErrTarget,
                  "选不中督军 —— 要打督军的技能，目标栏会直接写 EnemyWarlord");

        // 攻击者自己不存在 / 格位越界
        CheckCode(RuleCore.CanUseAbility(ctx, 0, 0), RuleCodes.ErrNotUnit, "空格上放技能");
        CheckCode(RuleCore.CanUseAbility(ctx, 0, 99), RuleCodes.ErrNotUnit, "越界格位");

        // ⚠️「能不能开始放」和「这个目标行不行」是两件事。
        //    要选目标的技能，表现层得先**进入选目标状态**（那一步还没选呢）——
        //    拿要求目标的判据去问只会得到 ErrTarget，于是永远进不了那个状态（踩过）。
        CheckCode(RuleCore.CanStartAbility(ctx, 0, 1), RuleCodes.OK, "CanStartAbility 不判目标");
        CheckCode(RuleCore.CanUseAbility(ctx, 0, 1), RuleCodes.ErrTarget,
                  "CanUseAbility 不带目标 → 「还没选目标」");
    }

    static void TestHealAndDraw()
    {
        UnitState a, b;
        var medic = Unit("Medic", 2, 1, 4, "Ability: Heal 3 OwnWarlord");

        var ctx = Duel(medic, Unit("Dummy", 1, 0, 1), out a, out b);
        ctx.Players[0].Warlord.Health = 20;
        CheckCode(RuleCore.CanUseAbility(ctx, 0, 1), RuleCodes.OK, "治疗技能不用选目标");
        CheckCode(RuleCore.UseAbility(ctx, 0, 1), RuleCodes.OK, "放出来");
        Check(ctx.Players[0].Warlord.Health, 23, "督军 20 → 23");

        var ctx2 = Duel(medic, Unit("Dummy", 1, 0, 1), out a, out b);
        ctx2.Players[0].Warlord.Health = 29;
        CheckCode(RuleCore.UseAbility(ctx2, 0, 1), RuleCodes.OK, "血快满了也能放");
        Check(ctx2.Players[0].Warlord.Health, 30, "治疗封顶在最大生命 30（不是 32）");

        var scribe = Unit("Scribe", 2, 1, 4, "Ability: Draw 2");
        var ctx3 = Duel(scribe, Unit("Dummy", 1, 0, 1), out a, out b);
        int hand = ctx3.Players[0].Hand.Count;
        CheckCode(RuleCore.UseAbility(ctx3, 0, 1), RuleCodes.OK, "抽牌技能");
        Check(ctx3.Players[0].Hand.Count, hand + 2, "Draw 2 真抽了两张（不是只抽一张）");
    }

    /// <summary>
    /// 效果链的递归截断。
    /// 「受伤 → 打回去 → 那边也受伤 → 再打回来」天生是个环（规则书 :237 就在讲同时触发），
    /// 靠 `BattleContext.MaxEffectChain` 截断，不能指望「保证不会发生」。
    /// </summary>
    static void TestEffectChainGuard()
    {
        UnitState a, b;
        var p1 = Unit("PingPong", 1, 0, 30, "Penitence: Damage 1 EnemyUnit");
        var p2 = Unit("PingPong", 1, 0, 30, "Penitence: Damage 1 EnemyUnit");

        // Poker 放在槽 3，让「敌方最左的部队」落在 PingPong 上，环才转得起来
        var ctx = Duel(Unit("Poker", 1, 3, 9), p2, out a, out b, aSlot: 3, bSlot: 2);
        Place(ctx, 0, 1, p1);
        ctx.ClearSignals();

        CheckCode(RuleCore.DeclareAttack(ctx, 0, 3, 1, 2), RuleCodes.OK, "点着这条链");

        int triggers = 0;
        foreach (var e in ctx.Signals) if (e.Kind == EvtKind.Trigger) triggers++;

        CheckTrue(triggers > 0, $"链真的转起来了（{triggers} 次触发）");
        CheckTrue(triggers <= BattleContext.MaxEffectChain,
                  $"被深度上限截断，没有无限递归（{triggers} ≤ {BattleContext.MaxEffectChain}）");
        Check(ctx.EffectChain, 0, "结算完深度回到 0（不然下一刀就少一层可用额度）");
        CheckTrue(Board(ctx, 0, 1).IsAlive && Board(ctx, 1, 2).IsAlive,
                  "两个乒乓单位都还活着（这条链是「互相点」，不是同归于尽）");
    }

    /// <summary>自己那套卡：效果文字**全部**解析得出来，六类关键词都有人用</summary>
    static void TestStarterCardEffects()
    {
        var all = StarterCards.Both();

        var unparsed = RuleCore.UnparsedEffects(all);
        Check(unparsed.Count, 0, "自己那套卡的效果文字全部解析得出来"
              + (unparsed.Count > 0 ? "（坏在这些：" + string.Join(" / ", unparsed) + "）" : ""));

        int withEffect = 0, withAbility = 0;
        foreach (var c in all)
        {
            if (c.Effects.Count > 0) withEffect++;
            if (c.HasAbility) withAbility++;
        }
        CheckTrue(withEffect >= 5, $"带触发效果的卡 {withEffect} 张（应 ≥ 5）");
        CheckTrue(withAbility >= 3, $"带主动技能的卡 {withAbility} 张（应 ≥ 3）");

        // 六类关键词**每一类都要有卡在用** —— 有一类没人用，它等于没做
        string[] need =
        {
            KeywordTable.Rally, KeywordTable.Strike, KeywordTable.Slay,
            KeywordTable.Backlash, KeywordTable.Penitence, KeywordTable.Ability,
        };
        foreach (var kw in need)
        {
            int n = 0;
            foreach (var c in all)
                if (kw == KeywordTable.Ability ? c.HasAbility : c.Effect(kw) != null) n++;
            CheckTrue(n > 0, $"「{kw}」至少有一张卡在用");
        }
        Debug.Log(P + $"   带效果的卡：触发 {withEffect} 张 / 技能 {withAbility} 张（共 {all.Count} 张）");
    }

    /// <summary>AI 会不会用技能 —— 不会用的话对局里永远看不到那类特效</summary>
    static void TestAiUsesAbility()
    {
        int s, t;

        // ① 治疗类：督军掉血就放，满血不浪费
        var guard = new CardDef("Reef Guard", "Reef Guard", "unit", "", null, "Test", 2, 0, 7, 0,
                                new[] { KeywordTable.Vanguard, "Ability: Heal 2 OwnWarlord" });
        var ctx = ToP2TurnWith(guard);
        CheckTrue(!SimpleAI.NextAbility(ctx, out s, out t), "督军满血 → 不浪费行动去治疗");

        ctx.Players[1].Warlord.Health -= 3;
        CheckTrue(SimpleAI.NextAbility(ctx, out s, out t), "督军掉血 → AI 会放治疗技能");
        Check(s, 1, "选中了场上的 Reef Guard");
        CheckCode(RuleCore.UseAbility(ctx, 1, s, t), RuleCodes.OK, "AI 挑的这一手真放得出来");

        // ② 伤害类：技能伤害 ≥ 攻击力就放 —— 技能不吃反击，没道理去平A
        var brute = new CardDef("Brute", "Brute", "unit", "", null, "Test", 3, 2, 5, 0,
                                new[] { "Ability: Damage 2 EnemyUnit" });
        var ctx2 = ToP2TurnWith(brute);
        Place(ctx2, 0, 1, Unit("Prey", 1, 0, 5));
        CheckTrue(SimpleAI.NextAbility(ctx2, out s, out t), "2 攻打 2 伤技能 → 放技能");
        Check(t, 1, "目标是对面那个单位");
        CheckCode(RuleCore.UseAbility(ctx2, 1, s, t), RuleCodes.OK, "放得出来");

        // ③ 攻击力更高 → 平A更划算
        var big = new CardDef("Big", "Big", "unit", "", null, "Test", 3, 9, 5, 0,
                              new[] { "Ability: Damage 2 EnemyUnit" });
        var ctx3 = ToP2TurnWith(big);
        Place(ctx3, 0, 1, Unit("Prey", 1, 0, 5));
        CheckTrue(!SimpleAI.NextAbility(ctx3, out s, out t), "9 攻打 2 伤技能 → 平A划算，不放技能");

        // ④ 对面场上没部队 → 伤害技能空过，不放
        var ctx4 = ToP2TurnWith(brute);
        CheckTrue(!SimpleAI.NextAbility(ctx4, out s, out t), "对面没有部队可打 → 不放（不白扔一次行动）");
    }

    /// <summary>造一个「轮到 P2 的第 1 回合，P2 槽 1 站着 card」的场景</summary>
    static BattleContext ToP2TurnWith(CardDef card)
    {
        var ctx = Battle(new[] { Unit("F1", 1, 1, 1), Unit("F2", 1, 1, 1), Unit("F3", 1, 1, 1) },
                         new[] { card, Unit("G1", 1, 1, 1), Unit("G2", 1, 1, 1) });
        ToP1Turn(ctx, 2);
        RuleCore.EndTurn(ctx);
        RuleCore.BeginTurn(ctx);          // 轮到 P2
        Place(ctx, 1, 1, card);
        return ctx;
    }

    /// <summary>
    /// **对手 AI 照原版重写**（2026-09-17）—— 把从反编译里读出来的规则逐条钉住。
    /// 正本：`资料/AI_原版反编译_0917.md`（公式 · 常量 · 数据类 · **死代码清单**都在那儿）。
    /// </summary>
    static void TestAiOriginal()
    {
        // ① 换牌规则（原版 `AI.GetAiMulliganCards`）：`manaCost > 4` 换掉、其余保留
        {
            var ctx = RuleCore.NewBattle(
                Deck(Unit("a", 1, 1, 1)),
                Deck(Unit("Cheap", 1, 1, 1), Unit("Dear", 6, 6, 6), Unit("Mid", 4, 2, 2)),
                seed: 0, shuffle: false, openMulligan: true);
            var idx = SimpleAI.AiMulliganIndices(ctx, 1);
            Check(idx.Count, 1, "★ 只换「费用 > 4」的那张（原版 `manaCost > 4`）");
            Check(idx[0], 1, "★ 换的正是手牌里那张 6 费（下标 1）");
        }

        // ② 卡价值 = 费用 × 2 + 1（原版 `GetCardReferenceValueInPlay`）—— 所有分曲线的尺子
        Check(SimpleAI.RefValue(Unit("X", 3, 1, 1)), 7f, "★ 卡价值 = 费用 × 2 + 1");

        // ③ 打督军：够斩杀 = 压倒性分；非致命 = 伤害 × 1.8（剩余血 ≤ 15）
        {
            var ctx = ToP2TurnWith(Unit("Brute", 3, 3, 5));
            var w = ctx.Players[0].Warlord;
            w.Health = 3;
            CheckTrue(SimpleAI.ScoreWarlordDamage(w, 3) >= 1000000f,
                      "★ 这一下打死督军 → 压倒性分（原版 1e6）");
            w.Health = 10;
            float got = SimpleAI.ScoreWarlordDamage(w, 3);
            CheckTrue(Math.Abs(got - 3f * 1.8f) < 0.001f,
                      $"★ 非致命打督军 = 伤害 × 1.8（剩余血 ≤ 15），实得 {got}");
        }

        // ④ 合击斩杀上界：各单位**取自己最高的那一刀**、再相加（原版 `CombinedAttackCanKillWarlord`）
        {
            var ctx = ToP2TurnWith(Unit("A", 2, 2, 2));
            ctx.Players[1].Warlord.Exhausted = true;      // ⚠️ 督军也算攻击者 —— 先疲劳掉再量（踩过）
            Place(ctx, 1, 2, Unit("B", 2, 2, 2));
            ctx.Players[0].Warlord.Health = 4;
            Check(SimpleAI.DamageToFoeWarlord(ctx), 4, "★ 合击上界 = 2 + 2（两支部队各自的刀相加）");
            CheckTrue(SimpleAI.CanLethal(ctx), "★ 攒够 4 点 → 判得出斩杀线");

            ctx.Players[0].Warlord.Health = 5;
            CheckTrue(!SimpleAI.CanLethal(ctx), "★ 差 1 点就不算斩杀（边界卡死）");
        }

        // ⑤ 选择：平手保留**先出现的**（原版严格 `>`）；分 ≤ 0 就宁可收手（`endTurn` 基准 0）
        {
            var ctx = ToP2TurnWith(Unit("A", 2, 2, 2));
            var l = new List<AiAction> {
                new AiAction { Kind = AiActionKind.PlayCard, HandIdx = 0, Score = 5f },
                new AiAction { Kind = AiActionKind.PlayCard, HandIdx = 1, Score = 5f },
                new AiAction { Kind = AiActionKind.EndTurn },
            };
            Check(SimpleAI.SelectBestAction(ctx, l).HandIdx, 0, "★ 平手时保留**先出现**的那条（严格 `>`）");

            var l2 = new List<AiAction> {
                new AiAction { Kind = AiActionKind.PlayCard, HandIdx = 0, Score = -1f },
                new AiAction { Kind = AiActionKind.EndTurn },
            };
            CheckTrue(SimpleAI.SelectBestAction(ctx, l2).Kind == AiActionKind.EndTurn,
                      "★ 分 ≤ 0 就不做 —— 原版的「留牌」就写在这条基准上");
        }

        // ⑥ 难度旋钮（原版 `TweakAvailableActions`）：困难档一条都不砍；很简单档一定砍最低分的
        {
            Check(SimpleAI.KnobsOf(AiDifficulty.Hard).MaxSkips, 0, "★ 困难档不砍任何动作");
            CheckTrue(SimpleAI.KnobsOf(AiDifficulty.SuperEasy).MaxSkips > 0,
                      "★ 很简单档会砍掉最低分的那些动作");

            // 同一 seed 的两个局面 ⇒ 砍掉的条数一致（骰子走 `ctx.AiRng`，一局必须可复现）
            var c1 = ToP2TurnWith(Unit("A", 2, 2, 2));
            var c2 = ToP2TurnWith(Unit("A", 2, 2, 2));
            var l1 = SimpleAI.EnumerateActions(c1);
            foreach (var a in l1) a.Score = SimpleAI.ScoreAction(c1, a);
            var l2 = SimpleAI.EnumerateActions(c2);
            foreach (var a in l2) a.Score = SimpleAI.ScoreAction(c2, a);
            int n1 = l1.Count;
            Check(n1, l2.Count, "同 seed 的两个局面，动作表一样大");
            SimpleAI.TweakAvailableActions(c1, l1, AiDifficulty.SuperEasy);
            SimpleAI.TweakAvailableActions(c2, l2, AiDifficulty.SuperEasy);
            Check(l1.Count, l2.Count, "★ 同 seed ⇒ 砍掉的条数一致（可复现）");
            CheckTrue(l1.Count < n1, "★ 很简单档确实砍掉了动作（不是空转）");
        }

        // ⑦ 动作表覆盖：**替代行动**进表 —— 老实现里 AI 一条都不走（这些卡在它手上等于死的）
        {
            var dut = new CardDef("DutyMan", "DutyMan", "unit", "Duty: Deal 3 damage to an enemy troop",
                                  null, "Test", 2, 2, 3, 0, new[] { KeywordTable.Duty });
            var ctx = ToP2TurnWith(dut);
            Place(ctx, 0, 1, Unit("Prey", 1, 1, 3));
            bool hasDuty = false, hasAttack = false;
            foreach (var a in SimpleAI.EnumerateActions(ctx))
            {
                if (a.AltKeyword == KeywordTable.Duty) hasDuty = true;
                if (a.Kind == AiActionKind.AttackMelee) hasAttack = true;
            }
            CheckTrue(hasDuty, "★ `duty` 替代行动进了动作表（原来 AI 完全不碰这一族）");
            CheckTrue(hasAttack, "近战攻击也在表里（近战/远程各一条）");
        }
    }

    /// <summary>
    /// `SimpleAI` 的**斩杀线**（2026-09-17）：这一回合总伤害够打死对面督军时，
    /// 必须**直接打脸**，不许去清部队。
    ///
    /// 🔴 这是一条**回归测试**，钉的是一个真会送掉胜局的 bug：原来评分里
    ///    「打死一个部队」= **1000** 分、「打督军脸」= **400** 分 ⇒
    ///    对面督军剩 5 血、我这三个 3/3 一刀就能砍死他的时候，
    ///    AI **会先去清那个 9 血诱饵**。修法见 `SimpleAI.CanLethal` / `NextAttack` 的注释。
    /// </summary>
    static void TestAiKillLine()
    {
        int a, tp, ts; bool ranged;

        // ---- ① 正面：3 个 3/3 够打死 5 血督军 ⇒ 这一刀必须打脸 ----
        var ctx = ToP2TurnWith(Unit("Mine0", 1, 3, 3));
        Place(ctx, 1, 0, Unit("Mine1", 1, 3, 3));
        Place(ctx, 1, 6, Unit("Mine2", 1, 3, 3));
        ctx.Players[0].Warlord.Health = 5;              // 对面督军剩 5
        Place(ctx, 0, 0, Unit("Prey", 1, 1, 9));        // 一个**打不死**的诱饵（9 血）
        // ⚠️ **督军自己也是攻击者**（它就在槽 `BoardSpec.WarlordSlot`，`NextAttack` 一直把它算在内）
        //    ⇒ 是 3×3 + 2 = **11**，不是 9。这条断言顺带把「督军算一个攻击者」钉下来
        //    —— 第一版这里写的是 9，被这条断言当场打了回来。
        Check(SimpleAI.DamageToFoeWarlord(ctx), 11,
              "★ 这一回合能对督军打出的总伤害 = 3×3 + **督军自身 2 攻**" + LogTail(ctx));
        CheckTrue(SimpleAI.CanLethal(ctx), "★ 判得出斩杀线（11 ≥ 5）");
        CheckTrue(SimpleAI.NextAttack(ctx, out a, out tp, out ts, out ranged), "AI 挑得出一刀");
        Check(tp, 0, "★ 这一刀打的是对面那一方");
        Check(ts, BoardSpec.WarlordSlot,
              "★ 目标是**督军格**，不是那个 9 血诱饵（旧评分里「清部队 1000 > 打脸 400」会去清诱饵）");
        CheckCode(RuleCore.DeclareAttack(ctx, 1, a, tp, ts, ranged), RuleCodes.OK,
                  "AI 挑的这一手引擎认（别让它挑一手打不出的刀）");

        // ---- ② 反例：差 1 点就不算斩杀（9 < 10）----
        var ctx2 = ToP2TurnWith(Unit("Mine0", 1, 3, 3));
        Place(ctx2, 1, 0, Unit("Mine1", 1, 3, 3));
        Place(ctx2, 1, 6, Unit("Mine2", 1, 3, 3));
        ctx2.Players[0].Warlord.Health = 12;
        CheckTrue(!SimpleAI.CanLethal(ctx2), "★ 差 1 点（11 < 12）就不算斩杀 —— 别把「差不多」当斩杀");
        ctx2.Players[0].Warlord.Health = 11;
        CheckTrue(SimpleAI.CanLethal(ctx2), "★ 刚好够（11 = 11）就算 —— **边界要卡死**");

        // ---- ③ 一个单位**只算一刀**：近战 2 / 远程 5 只贡献 5 ----
        // ⚠️ 把督军先疲劳掉，这条才量得准（不排掉的话它是 5 + 2 = 7，看不出是不是两路相加 ——
        //    第一版就是这么被打了回来的）
        var ctx3 = ToP2TurnWith(Ranged("Shooter", 2, 2, 3, 5));
        ctx3.Players[1].Board[BoardSpec.WarlordSlot].Exhausted = true;
        Check(SimpleAI.DamageToFoeWarlord(ctx3), 5,
              "★ 近战 2 / 远程 5 的单位只算**高的那一路**（两路相加的话这里是 7）");

        // ---- ④ 疲劳的单位打不了 ⇒ 不算进斩杀线 ----
        var ctx4 = ToP2TurnWith(Unit("Tired", 1, 4, 4));
        ctx4.Players[1].Board[BoardSpec.WarlordSlot].Exhausted = true;   // 同样先排掉督军
        Check(SimpleAI.DamageToFoeWarlord(ctx4), 4, "★ 对照组：没疲劳时它算 4 点");
        ctx4.Players[1].Board[1].Exhausted = true;
        Check(SimpleAI.DamageToFoeWarlord(ctx4), 0, "★ 疲劳的单位不算进斩杀线");
    }

    /// <summary>
    /// 事件流的**确定性**：同种子必须逐条一致。
    /// 事件流是表现层放特效的依据 —— 它要是不可复现，特效的 bug 就没法查。
    /// </summary>
    static void TestSignalDeterminism()
    {
        var a = RunScriptedGame(20260911);
        var b = RunScriptedGame(20260911);
        Check(a.Count, b.Count, $"同种子 → 事件条数一致（{a.Count} 条）");

        bool same = a.Count == b.Count;
        for (int i = 0; same && i < a.Count; i++) same = a[i] == b[i];
        CheckTrue(same, "同种子 → 事件流逐条一致");

        var c = RunScriptedGame(777);
        bool differs = c.Count != a.Count;
        for (int i = 0; !differs && i < c.Count; i++) differs = c[i] != a[i];
        CheckTrue(differs, "换种子 → 事件流不一样（证明随机真的在起作用）");

        // 这两类事件**必须真的出现在对局里** —— 不然「接上了」只是纸上谈兵。
        // 跑满 40 回合（牌库抽空就靠疲劳收场），30 张的牌库足够把带效果的卡都摸到
        bool sawAbility = false, sawTrigger = false;
        foreach (var line in a)
        {
            if (line.Contains("Ability")) sawAbility = true;
            if (line.Contains("Trigger")) sawTrigger = true;
        }
        CheckTrue(sawAbility, $"这段脚本对局里出现了 Ability 事件（部队卡发动技能）");
        CheckTrue(sawTrigger, $"这段脚本对局里出现了 Trigger 事件（触发效果）");

        // 事件种类分布 —— 「怎么一条 Ability 都没有」这种问题一眼能看见
        var kinds = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var line in a)
        {
            var sp = line.Split(' ');
            if (sp.Length < 2) continue;
            int n;
            kinds.TryGetValue(sp[1], out n);
            kinds[sp[1]] = n + 1;
        }
        var parts = new List<string>();
        foreach (var kv in kinds) parts.Add($"{kv.Key}×{kv.Value}");
        Debug.Log(P + $"   事件流 {a.Count} 条：" + string.Join("  ", parts));
    }

    /// <summary>
    /// 跑一段固定脚本的对局，把事件流原样收下来做字符串（好比较）。
    ///
    /// ⚠️ **督军的血被调大了** —— 这一段的目的不是「打完一局分出胜负」
    ///    （那是 <see cref="TestFullGameWithRealCards"/> 的事），而是**跑够回合数**，
    ///    让 30 张牌库里那些带效果的卡都上过场。真实长度的对局十几回合就结束了，
    ///    那点回合数里摸不摸得到 Ironclad / Reef Guard 全看运气，断言会飘。
    /// </summary>
    static List<string> RunScriptedGame(int seed, int turns = 40)
    {
        var ctx = RuleCore.NewBattle(
            DeckBuilder.StarterDeck(StarterCards.Ember(), StarterCards.EmberFaction, 30, new System.Random(1)),
            DeckBuilder.StarterDeck(StarterCards.Tide(), StarterCards.TideFaction, 30, new System.Random(2)),
            seed);

        for (int i = 0; i < 2; i++)
        {
            ctx.Players[i].Warlord.MaxHealth = 999;
            ctx.Players[i].Warlord.Health = 999;
        }

        for (int i = 0; i < turns && !ctx.IsOver; i++)
        {
            RuleCore.BeginTurn(ctx);
            SimpleAI.PlayTurn(ctx);
            if (ctx.IsOver) break;
            RuleCore.EndTurn(ctx);
        }

        var log = new List<string>();
        foreach (var e in ctx.Signals) log.Add(e.ToString());
        return log;
    }

    // ==================================================================
    //  胜负与疲劳
    // ==================================================================

    static void TestWinnerByWarlord()
    {
        var ctx = Battle(new[] { Unit("A", 1, 40, 40), Unit("F1", 1, 1, 1), Unit("F2", 1, 1, 1) },
                         new[] { Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1) });
        ToP1Turn(ctx, 3);
        Place(ctx, 0, 1, Unit("A", 1, 40, 40));

        Check(RuleCore.CheckWinner(ctx), 0, "开局进行中");
        CheckCode(RuleCore.DeclareAttack(ctx, 0, 1, 1, BoardSpec.WarlordSlot), RuleCodes.OK,
                  "可以攻击敌方督军");
        Check(ctx.Winner, 1, "督军被打死 → P1 胜");
        Check(RuleCore.CheckWinner(ctx), 1, "重复调用 CheckWinner 结果稳定");

        // 不能打自己的督军
        var ctx2 = Battle(new[] { Unit("A", 1, 1, 1), Unit("F1", 1, 1, 1), Unit("F2", 1, 1, 1) },
                          new[] { Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1) });
        ToP1Turn(ctx2, 3);
        Place(ctx2, 0, 1, Unit("A", 1, 1, 1));
        CheckCode(RuleCore.DeclareAttack(ctx2, 0, 1, 0, BoardSpec.WarlordSlot), RuleCodes.ErrSelf,
                  "不能攻击自己场上的督军");
    }

    /// <summary>
    /// 投降（原版 `BattleResult.Forfeit`）。**这条以前完全没有** —— 用户 2026-09-12 点名要还原。
    /// 判据有三条：① 立刻判对方胜（**不看血量**）② 记下是谁投的 ③ 判过了不再改。
    /// </summary>
    static void TestForfeit()
    {
        var ctx = Battle(new[] { Unit("A", 1, 40, 40), Unit("F1", 1, 1, 1), Unit("F2", 1, 1, 1) },
                         new[] { Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1) });
        ToP1Turn(ctx, 3);
        Check(ctx.Winner, 0, "开局进行中（谁都没倒）");
        Check(ctx.ForfeitedBy, -1, "还没人投降");

        // P0 投降 → P1（2 号）胜
        Check(RuleCore.Forfeit(ctx, 0), 2, "P0 投降 → P1 胜（**不看督军血量**）");
        Check(ctx.ForfeitedBy, 0, "记下是谁投的");
        CheckTrue(ctx.Players[0].Warlord.Health > 0, "投的时候督军还活着（不是被打死的）");
        CheckTrue(ctx.Events[ctx.Events.Count - 1].Contains("投降"), "事件日志里写明了投降");

        // 已经结束了：第二次投降（连同对方的）都不作数
        Check(RuleCore.Forfeit(ctx, 1), 2, "判过之后再投降不改结果");
        Check(ctx.ForfeitedBy, 0, "也不改「谁投的」");

        // 另一条路：P1 投降 → P0 胜
        var ctx2 = Battle(new[] { Unit("A", 1, 1, 1), Unit("F1", 1, 1, 1), Unit("F2", 1, 1, 1) },
                          new[] { Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1) });
        Check(RuleCore.Forfeit(ctx2, 1), 1, "P1 投降 → P0 胜");
        Check(ctx2.IsOver, true, "投降之后 `IsOver` 为真");

        // 越界/无效参数：什么都不做（**不能静默判错**）
        var ctx3 = Battle(new[] { Unit("A", 1, 1, 1), Unit("F1", 1, 1, 1), Unit("F2", 1, 1, 1) },
                          new[] { Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1) });
        Check(RuleCore.Forfeit(ctx3, 7), 0, "非法玩家号 → 返回 0、不改状态");
        Check(ctx3.Winner, 0, "…结果也还是「进行中」");
    }

    static void TestDrawIsDraw()
    {
        var ctx = Battle(new[] { Unit("A", 1, 1, 1), Unit("F1", 1, 1, 1), Unit("F2", 1, 1, 1) },
                         new[] { Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1) });
        ctx.Players[0].Warlord.Health = 0;
        ctx.Players[1].Warlord.Health = 0;
        Check(RuleCore.CheckWinner(ctx), 3, "双方督军同时倒下 → 平局");
        Check(ctx.Winner, 3, "ctx.Winner 也记成 3");
    }

    static void TestFatigue()
    {
        var ctx = Battle(new[] { Unit("A", 1, 1, 1), Unit("F1", 1, 1, 1), Unit("F2", 1, 1, 1) },
                         new[] { Unit("X", 1, 1, 1), Unit("Y", 1, 1, 1), Unit("Z", 1, 1, 1) });
        ctx.Players[0].Deck.Clear();

        Check(ctx.Players[0].Warlord.Health, 30, "抽空之前督军 30 血");
        RuleCore.Draw(ctx, 0);
        Check(ctx.Players[0].Fatigue, 1, "第 1 次抽空 → 疲劳 1");
        Check(ctx.Players[0].Warlord.Health, 29, "督军挨 1 伤（30→29）");
        RuleCore.Draw(ctx, 0);
        Check(ctx.Players[0].Fatigue, 2, "第 2 次 → 疲劳 2");
        Check(ctx.Players[0].Warlord.Health, 27, "督军再挨 2 伤（29→27）");

        // 疲劳能打死督军 —— 每次抽牌后都必须复查胜负
        ctx.Players[0].Warlord.Health = 1;
        RuleCore.Draw(ctx, 0);
        CheckTrue(ctx.Players[0].Warlord.Health <= 0, "疲劳把督军打死了");
        Check(ctx.Winner, 2, "督军被疲劳打死 → 对方胜（Draw 内部复查了胜负）");
    }

    // ==================================================================
    //  确定性
    // ==================================================================

    static void TestDeterminism()
    {
        var pool = CardDatabase.Load();
        if (pool.Count == 0) { CheckTrue(false, "卡表没加载上，确定性测试跳过"); return; }

        var factions = CardDatabase.Factions(pool);
        string f = factions[0];

        var a = RuleCore.NewBattle(DeckBuilder.StarterDeck(pool, f, 30, new System.Random(7)),
                                   DeckBuilder.StarterDeck(pool, f, 30, new System.Random(9)),
                                   seed: 20260911);
        var b = RuleCore.NewBattle(DeckBuilder.StarterDeck(pool, f, 30, new System.Random(7)),
                                   DeckBuilder.StarterDeck(pool, f, 30, new System.Random(9)),
                                   seed: 20260911);

        Check(a.Players[0].Hand.Count, b.Players[0].Hand.Count, "同种子 → 手牌张数一致");
        Check(a.Players[0].Deck.Count, b.Players[0].Deck.Count, "同种子 → 牌库张数一致");

        bool sameHand = true, sameDeck = true;
        for (int i = 0; i < a.Players[0].Hand.Count; i++)
            if (a.Players[0].Hand[i].Card.Name != b.Players[0].Hand[i].Card.Name) { sameHand = false; break; }
        for (int i = 0; i < a.Players[0].Deck.Count; i++)
            if (a.Players[0].Deck[i].Card.Name != b.Players[0].Deck[i].Card.Name) { sameDeck = false; break; }

        CheckTrue(sameHand, "同种子 → 起手逐张一致（顺序也一致）");
        CheckTrue(sameDeck, "同种子 → 洗牌结果逐张一致");

        // 换个种子应该不一样（防「压根没洗牌」这种假通过）
        var c = RuleCore.NewBattle(DeckBuilder.StarterDeck(pool, f, 30, new System.Random(7)),
                                   DeckBuilder.StarterDeck(pool, f, 30, new System.Random(9)),
                                   seed: 99999);
        bool differs = false;
        for (int i = 0; i < a.Players[0].Deck.Count; i++)
            if (a.Players[0].Deck[i].Card.Name != c.Players[0].Deck[i].Card.Name) { differs = true; break; }
        CheckTrue(differs, "换种子 → 洗牌结果不同（证明真的在洗）");
    }

    // ==================================================================
    //  端到端：用真卡表跑完一局
    // ==================================================================

    static int FirstFreeSlot(PlayerState p)
    {
        for (int s = 0; s < BoardSpec.Size; s++)
            if (BoardSpec.IsDeployable(s) && p.Board[s] == null) return s;
        return -1;
    }

    /// <summary>
    /// 贪心出牌 + 全量攻击。够验证「规则能跑完一局分出胜负」，不是 AI 设计。
    /// 打不死就靠疲劳分出结果 —— 那也顺带验了疲劳。
    /// </summary>
    static void PlayGreedyTurn(BattleContext ctx)
    {
        int p = ctx.Active;
        var ps = ctx.Players[p];

        // 出牌：反复挑「能打得起的最贵的一张」放到第一个空格
        for (int guard = 0; guard < 40 && !ctx.IsOver; guard++)
        {
            int best = -1, bestCost = -1;
            for (int i = 0; i < ps.Hand.Count; i++)
            {
                var card = ps.Hand[i].Card;
                if (!card.IsUnit || card.Cost > ps.Energy) continue;
                if (card.Cost > bestCost) { bestCost = card.Cost; best = i; }
            }
            if (best < 0) break;
            int slot = FirstFreeSlot(ps);
            if (slot < 0) break;
            if (RuleCore.PlayCard(ctx, p, best, slot) != RuleCodes.OK) break;
        }

        // 攻击：每个己方单位打一次，目标按槽位顺序取第一个合法的
        for (int s = 0; s < BoardSpec.Size && !ctx.IsOver; s++)
        {
            var u = ps.Board[s];
            if (u == null || u.Exhausted) continue;
            if (u.IsWarlord && u.Attack <= 0) continue;
            for (int ts = 0; ts < BoardSpec.Size; ts++)
            {
                if (ctx.Players[1 - p].Board[ts] == null) continue;
                if (RuleCore.DeclareAttack(ctx, p, s, 1 - p, ts, ranged: false) == RuleCodes.OK) break;
            }
        }
    }

    static void TestFullGameWithRealCards()
    {
        var pool = CardDatabase.Load();
        if (pool.Count == 0) { CheckTrue(false, "卡表没加载上，端到端测试跳过"); return; }

        var factions = CardDatabase.Factions(pool);
        string f0 = factions[0];
        string f1 = factions.Count > 1 ? factions[1] : factions[0];

        var deck0 = DeckBuilder.StarterDeck(pool, f0, DeckBuilder.ClassicDeckSize, new System.Random(11));
        var deck1 = DeckBuilder.StarterDeck(pool, f1, DeckBuilder.ClassicDeckSize, new System.Random(22));

        Check(deck0.Count, DeckBuilder.ClassicDeckSize, $"P1 牌组凑满 {DeckBuilder.ClassicDeckSize} 张");
        Check(deck0[0].Type, "hero", "牌组第 0 张是督军");

        var curve = DeckBuilder.CostCurve(deck0);
        Debug.Log(P + "   P1 法术力曲线：" + string.Join(" ", curve));

        var ctx = RuleCore.NewBattle(deck0, deck1, seed: 20260911);
        Check(ctx.Players[0].Hand.Count, RuleCore.StartHand, "起手 3 张");

        int turnLimit = 200;
        while (!ctx.IsOver && ctx.Turn < turnLimit)
        {
            RuleCore.BeginTurn(ctx);
            PlayGreedyTurn(ctx);
            if (ctx.IsOver) break;
            RuleCore.EndTurn(ctx);
        }

        CheckTrue(ctx.IsOver, $"打完一局出结果了（共 {ctx.Turn} 回合，上限 {turnLimit}）");
        CheckTrue(ctx.Winner >= 1 && ctx.Winner <= 3, $"赢家 = {ctx.Winner}（1/2 胜，3 平局）");

        string who = ctx.Winner == 3 ? "平局" : $"P{ctx.Winner} 胜";
        Debug.Log(P + $"   对局结果：{who}，{ctx.Turn} 回合；"
                    + $"P1 督军剩 {Math.Max(0, ctx.Players[0].Warlord.Health)}，"
                    + $"P2 督军剩 {Math.Max(0, ctx.Players[1].Warlord.Health)}");
        Debug.Log(P + $"   末 6 条事件：");
        int from = Math.Max(0, ctx.Events.Count - 6);
        for (int i = from; i < ctx.Events.Count; i++) Debug.Log(P + "     · " + ctx.Events[i]);

        // 一局跑完账面要自洽
        CheckTrue(ctx.Players[0].Board[BoardSpec.WarlordSlot] != null, "督军始终留在槽 4 上");
        CheckTrue(ctx.Players[1].Board[BoardSpec.WarlordSlot] != null, "对方督军也在槽 4 上");
    }
}
