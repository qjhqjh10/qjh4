using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RuleEngine;

/// <summary>
/// `play` —— **逐卡真打**：「这张卡打出去，局面的数字对不对」。
///
/// 🔴 **它修的是哪一类【结构性失明】**（台账 `A1431` · 出处
///   `资料/普查产出_第十四会话/逐卡效果检查_方法与边界.md` §1 / §2 / §4）：
///   在本子命令之前，本仓 **9 类**检查手段 —— `ruleprobe` 的 `check`/`scan`/`seg`/`card`/`b19test`/`jackal` ·
///   `RuleEngineTest.Run` 的四份 `_tmp_view` 报表 · `EffectText.Coverage` + `WillRunOps` + `UnimplementedKeywords` ·
///   `zh_crosscheck.py` · `check_pnp_cards.py` · `gen_icon_plan.py` —— **全部只回答
///   「这句英文今天解析成什么样」**，**没有一条回答「这张卡打出去数字对不对」**。
///   ⇒ 「**解析过、结算错**」这一整类（记账 7 个洞里有 4 个是它）**任何全池尺子都照不到**：
///     `check` 报「解析差异 0 行」、`OpHasMechanism` 全通、`Coverage` 四栏全 0。
///   ⚠️ 与 `WillRun.cs`（`A1432`）的分工：那条量「**卡面正文收到没有**」（注册层）；
///     本条量「**收到之后打出去，局面的数字动了没有**」（结算层）。**两条腿互相看不见对方的洞。**
///
/// **做法**（不是判结论，是**把数字摆出来**）：一局固定的、种子化的夹具对战 ——
///   两边各一位督军 + 各 3 张固定部队摆好场 ⇒ **把被测那张卡打进手牌并对准它打一发** ⇒
///   打之前/之后的**局面指纹**逐条对照（能/棋盘每格/手牌·牌库·弃牌堆/伤害累计）。
///
/// 🔴 **它【不】判「对错」** —— 判据在**卡面**（`desc` / `descZh` / PnP 成品卡图）上，
///   本工具**只把读数摆出来**（读数与卡面并排，判决归人 / 调度台）。
///   ⛔ **绝不用我们自己的实现反填期望值**（`逐卡效果检查_方法与边界.md` §四 那条纪律：
///    「一份夹具的判据必须写死在卡面上」，拿实现反填就是「拿我们的实现证明我们的实现」）。
///
/// ⛔ **本文件一行都不改引擎**（`MyGame/Assets/**` 不在本工具的白名单里）。
/// ⚠️ 它是【快速内环 / 旁证】：正式验收仍是 `RuleEngineTest.Run`；但它不跑 Unity（整卡 &lt; 1 秒）。
/// </summary>
class PlayProbe
{
    // ==================================================================
    //  夹具常量（**全池同一份**，所以读数能横向比）
    // ==================================================================

    /// <summary>夹具牌库里的垫牌张数 —— 只为让「牌库还剩几张」这一列有个非零起点。</summary>
    const int FillerCount = 30;

    /// <summary>夹具的起始能量 —— ⛔ 不按卡面费用给，免得「打不出去」里混进「能量不够」这一档噪声。
    /// 全池印得最高的费用现读 **14**（`cost` 列）⇒ 20 留了余量。</summary>
    const int FixtureEnergy = 20;

    /// <summary>夹具的能量上限 —— 🔴 **必须留余量**：`Refill all your Energy` 那一族
    /// （`AM46 High Command`）在「能=上限」时**本来就什么都没发生** ⇒ 拿它当缺陷就是误报。
    /// 30 &gt; 起始 20 ⇒ 灌满看得见。</summary>
    const int FixtureMaxEnergy = 30;

    /// <summary>摆场之后把**我方**三张部队各扣多少血 —— 🔴 不扣的话，全池 `Heal …` 那一族
    /// （`AM48 Regimental Doctrine` / `BL49 Delightful Agonies` / `DA5 Apothecary` …）
    /// **满血治疗 = 什么都不发生** ⇒ 全都会被误列成候选。扣 2 点既让治疗看得见、又不致死。</summary>
    const int FixtureChip = 2;

    /// <summary>夹具手牌张数 —— 3 张摆场部队 + 1 张被测卡。</summary>
    const int FixtureHand = 4;

    // ------------------------------------------------------------------
    //  🆕 `A1943`（2026-10-14）：**夹具的起始【派系资源】三件**
    // ------------------------------------------------------------------
    // 🔴 **这三个值是我们挑的、⛔ 不是原版的做法**（本仓铁律 3：资源里查不到的部分，
    //    要么写「查不到」，要么明确标成「这是我们挑的」—— ⛔ 绝不能把猜测写成「原版就是这样」）。
    //
    // **为什么要有它**：在这之前夹具**只给能量**（`SetEnergy`），`Faith` / `SpiritStones` /
    //   `QuestPoints` **从头到尾没人写过** ⇒ 恒为 `int` 默认 **0** ⇒ 全池
    //   `N [Spirit Stone]: …`（15 张）与 `N ☀: …`（6 张）那两族的**能力前缀付不起** ⇒
    //   它们在**主档那一拍就已经结算不了**（`play check` 的 ①-c 里那 21 张的根因）。
    //
    // **取值怎么来的**（都是**全池实测**的边界值 · 2026-10-14 现读
    //   `MyGame/Assets/RuleEngine/Resources/cards_engine.json`（1126 张）；⛔ 不是从原版抄的 ——
    //   原版判据见下）：
    //   · `FixtureFaith = 12` —— 全池**阈值**最大的 N = **12**（`SOR50` 的 `Gain 3 ☀. 12 ☀: Draw 3 cards`；
    //     全池 ☀ 阈值现读分布 = 1×11 · 2×5 · 3×4 · 4×7 · 5×1 · 6×6 · 7×1 · 8×4 · **12×1**）
    //     ⇒ 12 让**所有** ☀ 阈值都够得着。
    //   · `FixtureSpirit = 5` —— 全池**当代价**最大的 N = **5**（`ASH53` 的
    //     `Draw 5 cards. 5 [Spirit Stone]: Reduce their cost by 5`；全池**带 `N [Spirit Stone]`
    //     代价前缀的正好 28 张**，代价现读分布 = 1×15 · 2×8 · 3×4 · **5×1**；
    //     另有 9 张正文只是提到这个 token、不带代价前缀）⇒ 5 让**所有**代价都付得起。
    //     ⚠️ 与信仰不同：信仰是**阈值**（到了就触发）、灵魂石是**货币**（要**扣掉**）⇒
    //        这里给的是「够付最大的一笔」而不是「达到最高的阈值」。
    //   · `FixtureQuestpoints = 0` —— 🔴 **必须保持 0**：全池**没有任何一张卡**把任务点当代价
    //     （33 张全是 `Gain N Quest Points`），而引擎里它**只被** `EffectResolver.cs:7269`
    //     「每满 3 点加 1 张隐秘」消费 ⇒ **给一个非 0 值会凭空触发里程碑**
    //     （那是**夹具自己造出来的**变化，会被读成「这张卡的实得」）。**这一格是「原版就是 0」**
    //     （见下），所以它同时满足「照原版」与「别造噪声」两条。
    //
    // **原版真值（反编译方法体 · 2026-10-14 本会话逐条现核，出处 = `d:/2/tools/decomp_full/`）**：
    //   · `BattleManager__SetupInitialMana` 收尾那一调 = `PlayerManager__ResetMana(this, <能量>,
    //     *(督军 + 0x124), …, *(督军 + 0x128), 0)` ⇒ **灵魂石是唯一从督军身上取初值的派系资源**
    //     （`RawCardScript.cs:83-87` 那五个「修饰符」之一）。
    //   · `PlayerManager__ResetMana` 现读写到的字段 = `+0x28 / +0x30 / +0x38~+0x5c / +0x74~+0xac`，
    //     其中第 **5** 个形参写的就是 `+0x9c`~`+0xac`（`GetCurrentSpiritStone` 读的正是它）
    //     ⇒ 🔴 **`+0xb0`（信仰 = `GetCurrentFaith` 读的）与 `+0xc4`（任务点 = `GetCurrentQuestPoints`
    //     读的）它【一个都不写】**。
    //   · `PlayerManager___ctor` 把 `+0x38`~`+0xd8` 那一整片 `ObscuredInt`（每个 5 个 dword）
    //     全用 `op_Implicit(…, 0, 0)` 初始化 ⇒ **开局 Faith = 0 · QuestPoints = 0（灵魂石也是 0）**。
    //   ⇒ 三格里**只有灵魂石是「本该由督军给、而我们没接」的那个缺口**（→ 账 `A1966`），
    //     另两格**原版本来就是 0**。
    //   · 而「督军给几颗灵魂石」的**逐督军真值本地取不到**（⚠️ 这一条**是台账转述**、本会话**没复核**：
    //     台账 `A1966` 记「`d:/2` 全盘含 `assets_full` 二进制 grep `initialSpiritStoneModifier`
    //     **0 命中**、`d:/4` 数据表无此字段」，并附了复核口 —— 要动它先回那一行）
    //     ⇒ 夹具只能取一个**覆盖面够宽的常数**。
    //   🔴 **本质是一次【量具干预】，⛔ 不是「把原版值补回来」**：它让那两族**有活目标**、
    //      于是「付得起之后数字动没动」这件事才**照得到**。读数里它表现为
    //      **`Gain N Faith` 那类卡的起点从 0 变成 12**（`Gain N Spirit Stones` 同理）
    //      —— 判读时按「**夹具起点**」读，⛔ 别当原版。
    const int FixtureFaith = 12;
    const int FixtureSpirit = 5;
    const int FixtureQuestpoints = 0;

    // ==================================================================
    //  读数
    // ==================================================================

    /// <summary>一张卡的「真打读数」。⛔ 这里面**没有任何期望值** —— 只有实得。</summary>
    class Reading
    {
        public CardDef C;
        public string How = "";              // 怎么打的（`PlayCard` / `PlayTactic` / 不可打）
        public int Rc = -1;                  // `RuleCore.PlayCard` / `PlayTactic` 的返回码
        public string Note = "";             // 打不出去时的原因（人话）
        public bool Played;                  // `Rc == RuleCodes.OK`
        public List<string> Changes = new List<string>();   // 逐条变化（打前 → 打后）
        public int SelfPlace;                // 其中「这张卡自己落场」占了几条
        public int Paid;                     // 其中**恰好等于打出费用**那一条能量变动占了几条
        public int Mech;                     // 其中「出牌本身」的机械动作（手牌−1 / 进弃牌堆）占了几条
        public int Other;                    // 除上面三类之外的条数 ⇒ **这一格就是「有没有实得」**
        public int ExpectedPay = -1;         // 照卡面应付（卡费 ＋ 付费能力前缀）；-1 = 这一档不算（督军）
        public int Dmg;                      // `DamageToEnemy[0]` 的增量
        public string Crash;                 // 抛异常时的 `类型: 消息`（⛔ 不许静默）
        public bool FaceHasOps;              // 卡面（`desc` 或注册层）上有没有可执行正文
        public string Before = "", After = "";   // 局面指纹（两行）
        public bool HeroMode;                // 督军走「对照局」那条路（见 <see cref="MeasureHero"/>）
        /// <summary>🆕 `A1572`：这一张是**夹具补进手牌**的（不是走自然上手那条路）——
        /// 两种卡**天然**到不了本夹具那一步：临时卡（`ephemeral`）会被回合末的 `SweepEphemeral` 移出游戏、
        /// 防御卡（`defence`）由 `BuildPlayer` 分流给**后手**（而本夹具的 P0 是先手）。
        /// ⚠️ 它是个**读数标记**：这一格为真的行 = 「量具干预过」，判读时与自然上手那批**不等价**。</summary>
        public bool Injected;
        public List<string> ControlChanges = new List<string>();   // 督军专用：空督军对照局的变化
        public string Bucket = "";           // 扫描时算的「档」（见 <see cref="Bucket"/>）
        /// <summary>🆕 **`B8_AV`（2026-10-26）：棋盘上的 `PlayOrder`（进场序）读数** ——
        /// 形如 `P0[4]=0 P0[3]=1 P0[6]=2 P0[7]=3 P1[4]=4 …`，**按 `PlayOrder` 升序排**
        /// （`PlayOrder &lt; 0` 的排末尾 —— 与 `PlayerState.UnitsByPlayOrder` /
        /// `RuleCore.StableSortByPlayOrder` 同口径）⇒ **在这一列里从左到右的字面次序就是「谁先响」**。
        /// 为什么要它：`B8_X` 的两条账（`MinionManager.GetMinionList` / `BattleManager.SetUnitOrderInPlay`）
        /// 卡在「`PlayOrder` 不在观察量表里、三个探针都不打印它」 ⇒ 没有读数就不能升 A。
        /// ⚠️ 它**不进** `Fingerprint` / `FingerprintRow`（那两处动一下就要重钉 `PinnedPlayMd5`）；
        /// 只作为 dump 的**附加列**与单卡读数的一行。</summary>
        public string PlayOrder = "";
        /// <summary>🆕 `P3`（2026-10-26）：`preset pick=1` 时**实际点定的那个目标**的卡名
        /// （空串 = 这一张没走 pick / 目标池是空的 ⇒ 没点）。见 <see cref="PresetSpec.Pick"/>。
        /// ⚠️ 它**不进** `Fingerprint` / `FingerprintRow`（同 `PlayOrder`），只作 dump 的**附加列**。</summary>
        public string PickTarget = "";
        /// <summary>🆕 **2026-10-27（`B9_E5ENG`）**：终局棋盘每格的 `AttacksThisTurn` / `Exhausted`
        /// （形状见 <see cref="Snap.ActCnt"/> / <see cref="UnitNumCol"/>；`P0… ‖ P1…`）。
        /// 🔴 **进** `FingerprintRow`（= 进结算指纹，会改 `PinnedPlayMd5`）——
        /// 它们原来是**结构性盲区**：实测把强制攻击的入队预扣注掉，指纹逐字节不变。
        /// ⚠️ 与 `PlayOrder` / `PickTarget` **相反**：那两列刻意**不进**指纹，这两列**必须进**。</summary>
        public string AtkCount = "", Exh = "";
        public int EnergyDelta;              // 能量净变动（读数用）
        public int HandCostDelta;            // 手牌总价净变动（读数用）
        /// <summary>打出去之后**引擎自己的事件日志**末尾几行 —— 🔴 少了它，「什么都没发生」就只能靠猜；
        /// 有了它，引擎自己会说出「查不到 / 没有合法目标 / 这一项没生效」那类话（本仓红线「不许静默失败」）。</summary>
        public List<string> LogTail = new List<string>();
    }

    /// <summary>局面指纹 —— 逐格 / 逐区域。**只打实得数值，不打判据。**
    /// 🔴 手牌/弃牌堆存的是**逐张 id 清单**（⛔ 不是张数）—— 张数会把「打出去的那一张」与
    ///   「效果生成/抽到的那一张」**抵消成 0**，于是「`Create a X in your hand` 那条根本没跑」
    ///   看起来和「跑了」**一模一样**（张数都是 0 差）。同名重复没法靠张数分开，靠 id 清单能。</summary>
    class Snap
    {
        public readonly int[] Energy = new int[2];
        public readonly string[][] Cells = new string[2][];
        public readonly List<string>[] HandIds = { new List<string>(), new List<string>() };
        public readonly List<string>[] DeckIds = { new List<string>(), new List<string>() };
        public readonly List<string>[] DiscIds = { new List<string>(), new List<string>() };
        public readonly int[] HandCost = new int[2];
        /// <summary>🆕 **阵营资源三件**（信仰 / 灵魂石 / 任务点）——
        /// 不加的话 `Gain 5 Spirit Stones`（`ASH51 Infinity Circuit`）这类卡会**被误列成候选**
        /// （它明明改了资源，只是读数里没这一格）。</summary>
        public readonly int[] Faith = new int[2];
        public readonly int[] Spirit = new int[2];
        public readonly int[] Quest = new int[2];
        public readonly int[] Dmg = new int[2];
        public readonly int[] Troops = new int[2];
        /// <summary>🆕 **2026-10-27（`B9_E5ENG`）：棋盘每格的 `AttacksThisTurn` / `Exhausted` 读数。**
        /// 形状见 <see cref="UnitNumCol"/>（`P0[3]=1 P0[5]=2`；只收**有人那格 · 非 0 值** ⇒
        /// 空串 = 全员 0 / 全员未疲劳，这样「0→1」「1→2」都看得见而 dump 不被刷屏）。
        /// 🔴 **为什么必须加**：这两格**从来不在任何指纹里**（<see cref="Cell"/> 只打 攻/远/甲/血/关键词）
        /// ⇒ `play check` 对「强制攻击少扣/多扣一次行动额度」「疲劳位算错」这一整族**结构性失明**。
        /// **实测实证**（2026-10-27）：把 `RuleCore.DeclareAttackRefundingForced` 的**入队预扣**注掉再跑，
        /// 结算指纹 `517be27d…` **逐字节相同**、逐卡差异也相同 —— 那就是这个盲区的证明。</summary>
        public readonly string[] ActCnt = new string[2];
        public readonly string[] Exh = new string[2];
    }

    // ==================================================================
    //  入口
    // ==================================================================

    /// <summary>`play &lt;id|卡名&gt;` 单卡 · `play all [dump &lt;out.tsv&gt;] [pf &lt;out.txt&gt;]` 全池 ·
    /// 🆕 `play check [&lt;pf.txt&gt;]`（`A1431` 第二笔：**结算层对拍** —— 见 <see cref="Check"/>）。
    /// 🆕 `play &lt;id&gt; preset &lt;spec&gt;`（`A2009`：**夹具预置局面**，见 <see cref="PresetSpec"/>）——
    ///   ⛔ 不传 `preset` 时夹具**一个字节都不变**（全池指纹不受影响）。
    /// 返回**退出码**（判据见 <see cref="RunOne"/> 尾部 · <see cref="Sweep"/> 尾部 · <see cref="Check"/> 尾部）。</summary>
    public static int Run(List<CardDef> pool, string key, string dumpPath, string pfPath = null,
                          string basePath = null, string presetSpec = null)
    {
        if (pool == null || pool.Count == 0) { Console.Error.WriteLine("!! 卡池是空的"); return 2; }
        if (string.IsNullOrEmpty(key))
        {
            Console.Error.WriteLine("!! 用法：play <id|卡名> [preset <spec>] | play all [dump <out.tsv>] [pf <out.txt>]"
                                    + " | play check [<pf.txt>]"
                                    + "（`unit` 档的单卡输出**带事件电池**那一节，见 `A1431` 第三笔）");
            return 2;
        }
        PresetSpec preset = null;
        if (!string.IsNullOrEmpty(presetSpec))
        {
            try { preset = PresetSpec.Parse(presetSpec); }
            catch (Exception ex)
            {
                Console.Error.WriteLine("!! `preset` 写法不对：" + ex.Message);
                Console.Error.WriteLine("   用法：preset dead[=N][:Subtype][,enemyhand=N][,chipenemy=1][,sub=<卡id|卡名>][,legal=1][,pick=1][,tpick=1]"
                                        + " [,hand=<卡id|卡名|self>][,handn=N][,event=<gainfaith|gainquest|gainspirit|createssecret|createssabotage>][,amount=N][,legacy=1][,face=ambush]"
                                        + " —— 例 `preset dead=1:Infantry` / `preset enemyhand=2` / `preset chipenemy=1`"
                                        + " / `preset sub=BL1`（把夹具的 `FIX_A` 换成池内一张真卡，`B8_AH`）"
                                        + " / `preset hand=self,event=gainfaith`（**手牌态监听卡**，`B8_AV`）"
                                        + " / `preset face=ambush`（**面朝下 + 带 `ambush` 的出手**，`B9_A3`）");
                return 2;
            }
        }
        if (string.Equals(key, "all", StringComparison.OrdinalIgnoreCase)) return Sweep(pool, dumpPath, pfPath, preset);
        if (string.Equals(key, "check", StringComparison.OrdinalIgnoreCase)) return Check(pool, basePath);

        var hit = Find(pool, key);
        if (hit == null) return 2;
        // 🆕 `B8_AV`（2026-10-26）：**手牌态监听**那一档 —— 与「真打一发」是两条完全不同的路
        //   （这条**不把被测卡打出去**，它要那张卡**躺在手里**）。见 `PresetSpec` 的 `HandListenMode`。
        if (preset != null && preset.HandListenMode) return RunHandListen(pool, hit, preset);
        // 🆕 `B9_A3`（2026-10-26）：**面朝下 + 带 `ambush` 的出手**那一档（见 `PresetSpec.Face`）。
        if (preset != null && preset.FaceAmbushMode) return RunFaceAmbush(pool, hit, preset);
        var r = Measure(pool, hit, preset);
        Print(r);
        // 🆕 `A1431` 第三笔：**单卡也要能看事件电池**（否则「这一张到底有没有被验到」得整池跑一遍才知道）。
        EventReading er = null;
        // 🔴 🆕 `B9_W32S3`（2026-10-27）：这里原来**丢掉了 `preset`**（`MeasureEvents(pool, hit)`）——
        //   而 `play all` 那条路（`Sweep`）是**带着** `preset` 的。
        //   ⇒ 同一个 `preset` 在「单卡读数」和「全池读数」上给出**两套不同的电池读数**，
        //   而且**一声不响**（`play AM4 preset big=1` 的电池照样按默认夹具跑）。
        //   实测：`big=1` 加上之后单卡仍打 `softAtk=skip(would-kill)`，全池那一份才有靶子。
        //   ⛔ 不传 `preset` 时 `MeasureEvents(pool, hit, null)` 与改前**逐字节相同**。
        if (hit.Type == "unit") { er = MeasureEvents(pool, hit, preset); PrintEvents(hit, er); }
        // 🆕 `B9_W41S3B`（2026-10-27）：**督军档电池**（只在 `preset hero=1` 时；⛔ 不传时一行都跑不到）
        else if (hit.Type == "hero" && preset != null && preset.HeroBattery > 0)
        { er = MeasureHeroBattery(pool, hit, preset); PrintEvents(hit, er); }
        // 🔴 退出码只认**硬失败**（⛔ 不拿「候选」当失败 —— 那会把「红了不用管」训练成习惯）：
        //   · 崩了（引擎抛异常）= 缺陷，必须红；
        //   · `ErrUnimplemented` = 这张卡**根本打不出去**（`CanPlayTactic` 要求
        //     `IsFullyParsed`）⇒ 卡面上写着正文而它连打都打不出去 = 缺陷，必须红。
        //   🆕 **电池抛异常 / 对照局没起步**同样算硬失败（`A1431` 第三笔）—— 那两样意味着
        //     「这一张**没被验到**」，正是本笔要治的那件事，⛔ 不许静默放过。
        //   ⚠️ `ErrNoTargetAvailable`（`A985⑧` 拆出来的那一档）**不算** ——
        //     那是**夹具里没有合法目标**（本夹具只有 `Infantry` 一个兵种、没有隐秘/飞行/载具），
        //     是**量具的限制**，不是卡的缺陷。如实打在读数里，⛔ 不混进失败。
        //   ⚠️ **净差 0 条也不算**（电池抬不到那个事件 ≠ 卡有缺陷）—— 但它**会在上面出声**。
        if (r.Crash != null) return 1;
        if (r.Rc == RuleCodes.ErrUnimplemented) return 1;
        if (er != null && (er.Crash != null || er.Note.Length > 0)) return 1;
        return 0;
    }

    static CardDef Find(List<CardDef> pool, string key)
    {
        // ⚠️ **卡名不是唯一键**（全池 3 个重名）⇒ 先按 `id`（唯一），再按卡名**逐个列出**。
        foreach (var c in pool)
            if (string.Equals(c.Id, key, StringComparison.OrdinalIgnoreCase)) return c;
        var byName = pool.Where(c => string.Equals(c.Name, key, StringComparison.OrdinalIgnoreCase)).ToList();
        if (byName.Count == 1) return byName[0];
        if (byName.Count > 1)
        {
            Console.Error.WriteLine("!! 卡名 `" + key + "` 有 " + byName.Count + " 张同名卡（卡名不是唯一键）—— 请按 `id` 指：");
            foreach (var c in byName) Console.Error.WriteLine("     " + c.Id + "  " + c.Name + "  [" + c.Type + "]");
            return null;
        }
        Console.Error.WriteLine("!! 卡池里没有 `" + key + "`（既不是 id 也不是卡名）");
        return null;
    }

    // ==================================================================
    //  夹具
    // ==================================================================

    static CardDef HeroOf(string id, string fac) =>
        new CardDef(id, id, "hero", "", null, fac, 0, 2, 30, 0, null, subtype: "Warlord");

    static CardDef Fix(string id, int atk, int hp, int rng, string subtype = "Infantry") =>
        new CardDef(id, id, "unit", "", null, "Fixture", 1, atk, hp, rng, null, subtype: subtype);

    /// <summary>垫牌用到的兵种（**轮着来**）—— `Draw a Beast` / `The next Vehicle` / `Deploy 3 random Drones`
    /// 那一族要**牌库/手里真有那种兵种**才可能有效果，⛔ 全用 `Infantry` 会把它们整族误列成候选。</summary>
    static readonly string[] FillerSubtypes = { "Infantry", "Vehicle", "Beast", "Drone" };

    /// <summary>垫牌里每几张有一张是**战术卡** —— `Draw a Stratagem` / `Your next Stratagem costs 1 less`
    /// 那一族要有「牌库/手里真有战术卡」才可能有效果（⛔ 全给单位 ⇒ 整族误列成候选）。</summary>
    const int FillerTacticEvery = 5;

    /// <summary>牌库 = 督军 + 垫牌 + `hand`（**倒着加**：抽牌从尾部 pop ⇒ `hand[0]` 最先上手，顺序与原样一致）。
    /// 形状照 `BattleProbe.DeckOf`（那份与 `RuleEngineTest` 对齐过）。</summary>
    static List<CardDef> Deck(string fac, params CardDef[] hand)
    {
        var d = new List<CardDef> { HeroOf("FIXWarlord_" + fac, fac) };
        for (int i = 0; i < FillerCount; i++)
        {
            if (i % FillerTacticEvery == FillerTacticEvery - 1)
                d.Add(new CardDef("filler" + i, "filler" + i, "tactic", "", null, "Fixture", 1, 0, 0, 0, null));
            else
                d.Add(Fix("filler" + i, 0, 1, 0, FillerSubtypes[i % FillerSubtypes.Length]));
        }
        for (int i = hand.Length - 1; i >= 0; i--) d.Add(hand[i]);
        return d;
    }

    static int HandIdx(BattleContext ctx, int p, CardDef c)
    {
        var h = ctx.Players[p].Hand;
        for (int i = 0; i < h.Count; i++) if (h[i].Card == c) return i;
        return -1;
    }

    static void SetEnergy(BattleContext ctx, int p)
    {
        ctx.Players[p].Energy = FixtureEnergy;
        ctx.Players[p].MaxEnergy = FixtureMaxEnergy;
    }

    /// <summary>🆕 `A1943`：夹具的**起始派系资源**三件 —— 与 <see cref="SetEnergy"/> **同一个
    /// 调用点、同一个形状**（见 `BuildFixture` 与 `HeroPass`，共 **4 处**；⛔ 少跟一处就会
    /// 「有的档给了、有的档没给」，而那种漏**不会报错**，只会让读数**静默地一半一半**）。
    ///
    /// 🔴 **取值是「我们挑的」、⛔ 不是原版的做法** —— 逐条的来龙去脉与「原版真值」见
    /// <see cref="FixtureFaith"/> 上面那一整段注释（**先读它再改这三个数**）。
    ///
    /// ⚠️ **这不是「补回原版起始值」**：原版开局 Faith / QuestPoints 本来就是 0
    /// （`PlayerManager__.ctor.c` 十个 `ObscuredInt` 全 `op_Implicit(0)`）；
    /// 这里把三者一次写成常数 = **量具干预**（让 Sp/Faith 两族能力**付得起**，结算对不对才照得到）。
    /// ⇒ 读数里那三列（信仰/灵魂石/任务点）的**起点**变成这三个数，⛔ 别按原版读。</summary>
    static void SetFaction(BattleContext ctx, int p)
    {
        ctx.Players[p].Faith = FixtureFaith;
        ctx.Players[p].SpiritStones = FixtureSpirit;
        ctx.Players[p].QuestPoints = FixtureQuestpoints;
    }

    /// <summary>摆场：两边各 3 张固定部队，落在 3 / 6 / 7 三格（**贴着督军格 4、且有相邻对 6-7**）——
    /// 这样「相邻」族与「督军旁边的」族都有活目标。</summary>
    static readonly int[] DeploySlots = { 3, 6, 7 };

    /// <summary>🆕 `B9_W32S3`（2026-10-27）：`preset big=1` 那个「打得动的靶子」落在哪一格。
    /// 取 **8**：可部署格 = `0..3 / 5..8`（督军格 4），既有的三张占 3/6/7 ⇒ 8 空着、且仍与 7 相邻
    /// （「相邻」族在 `big` 档里也仍有一对相邻格）。⛔ 不与 `DeploySlots` 合并 —— 那条数组的
    /// 三个下标被 P0/P1 摆场共用，动它会改掉**默认夹具**。</summary>
    const int BigSlot = 8;

    // ==================================================================
    //  🆕 `A2009`：夹具「预置局面」（默认关闭 —— 只有 `play <id> preset …` 才生效）
    // ==================================================================
    //
    // 🔴 **它治的是哪一条**：`A2009` 的 9 个 `DoChooseCard` 分支里，`deploy`（`SOR42`）与
    //   `return`（`UM61`）的候选域是**墓地**（`EffectResolver.DeadCandidates` → `BattleContext.DeadUnits`），
    //   而默认夹具跑完摆场后**弃牌堆/墓地都是空的** ⇒ 候选恒空、那两支**结构上验不到**。
    //   `shuffle`（`ASH82`）要的是**对手手牌里有牌**：默认夹具里 P1 手牌恰好还剩垫牌，但那是
    //   **巧合**（P1 只打出 3 张、发 4 张）—— 显式预置才让这条读数**可复现**。
    //
    // ⚠️ **这是【量具干预】**（与 `FixtureFaith` 同一档）：预置出来的 `FIX_DEAD*` / `FIX_EH*`
    //   **不在卡池里**，也不代表原版开局有牌。它只让「候选非空之后，选牌→动作这一跳跑不跑得通」
    //   照得到；⛔ 别把读数当「原版开局墓地有 1 张」。
    //
    // **预置的形状照引擎自己的账**（`BattleContext.TakeFromGraveyard` 的注释：`Discard` 与
    //   `DeadUnits` **必须一起**）⇒ 一份实例进 `Discard`、一条 `DeadUnit{Owner,DeathTurn}` 进表。
    //   `DeathTurn = ctx.Turn` ⇒ `since_last_turn` 窗口（`DeathTurn >= LastTurnStartMark`）也满足。
    sealed class PresetSpec
    {
        public int Dead;                 // 往 P0 墓地预置几张「友方部队」
        public string DeadSubtype;       // null = Infantry（`SOR42` 只要求 troop；`UM61` 要求 Infantry）
        public int EnemyHand;            // 往 P1 手牌预置几张垫牌
        public int ChipEnemy;            // 把 P1 的**非督军**单位各扣 `FixtureChip` 血（1 = 开；`A2001` 的 UM2 要「受伤的敌人」）
        /// <summary>🆕 `B8_AH`（2026-10-26）：把夹具的 `FIX_A`（默认 `Infantry` 的合成部队）**换成池内一张真卡**。
        /// 为什么需要它：`FIX_A/B/C` 是 `Fix(...)` 现造的 `CardDef`（**不带 `keyword`**）⇒ 条件 275/276/277
        /// 会走 `Subtype` 退回支，**结构上测不到**「原版 `keyword(+0x98)` 透传」的效果。
        /// 换一张真卡（例 `BL1 Chaos Spawn`：`Subtype=Infantry` 而 `keyword=Daemon`）才让
        /// 「原版判据 vs 卡面橙字」的差在**打出路径**上看得见。⛔ 不传 = 夹具一个字节都不变。</summary>
        public string Substitute;

        /// <summary>🆕 `B8_AH`（2026-10-26）：`legal=1` —— 目标格**改由产品代码的合法性判定**挑
        /// （`RuleCore.IsLegalPlayTarget`，它就是「拖拽高亮 / 这一点能不能选」问的那一处），
        /// 并把**合法目标池**打进事件日志。
        /// 为什么需要它：默认的 `PickSlot` 只取「第一个非督军格」，**完全不看合法性**
        /// ⇒ 显式目标格会绕过 `TargetCondition` 的筛（实测：`sub=BL1` 时效果照样落在 BL1 身上）
        /// ⇒ 那是一个**量具盲区**，不是引擎读数。⛔ 不传 = 夹具与目标挑法都不变。</summary>
        public int Legal;

        /// <summary>🆕 `P3`（2026-10-26）：`pick=1` —— **单位卡也给它一个「玩家点定的目标」**。
        ///
        /// <para>🔴 它治的是哪一条：不带目标的单位卡 `Rally`（例如 `AM70 Scion Medic` 的
        /// `Rally: Heal 2 to a friendly unit`）在**默认夹具**里会把「友方单位」解析成
        /// **刚打出去的这张卡自己**（它落场后是棋盘扫描里第一个友方单位），而它通常是满血
        /// ⇒ 治疗恒 0、整族「看不到」。</para>
        ///
        /// <para>**判据（第一权威，现读）**：原版 `AbilityLogic.GetTargets` 里 `ta=30 (target)`
        /// 那一支（`AbilityLogic__GetTargets.c:192-207` = `iVar6 == 0x1e`）**只**把**调用方传进来的
        /// 那个目标**放进结果表，**传 null 就返回空表**（⛔ 不以「自己」兜底）。
        /// 而调用方传的就是 `abilityData.targetCard`（`AbilityLogic__CanPlayAbility.c:47-57` 读
        /// `+0x10/+0x18/+0x20` 三个字段）；`targeted == true` 的卡**没有目标时整段
        /// `ThisCardPlayed` 直接跳过**（`RawCardScript.OnCardPlayedWithTarget`：`if (!targeted || target != null)`
        /// 才 `TriggerAbilities`，否则只打一条 `OnCardPlayedWith null Target` 警告）。
        /// ⇒ 原版语义 = **这类卡必须带一个玩家选的目标**；夹具给一个合法目标才叫保真。</para>
        ///
        /// <para>**怎么挑**：走**产品代码**自己的目标池 `EffectResolver.PlayTargetPool`
        /// （= 拖拽高亮 / `IsLegalPlayTarget` 问的那一处，⛔ 不另立一套筛），
        /// 在池里**优先取已受伤的那个**（治疗只有在受伤目标上才看得见；对伤害类无害），
        /// 然后把它当 `RuleCore.PlayCard(..., chosen)` 的 `chosen` 传进去。</para>
        ///
        /// <para>⚠️ 这是**量具干预**（与 `preset dead=` / `hand=` 同一档）；⛔ 不传 `pick=1` 时
        /// 夹具一个字节都不变（全池默认读数不受影响 —— 本档只用于**另一次**全池扫描）。</para>
        /// 🆕 **2026-10-26（执行代理 `E9ER`）**：`tpick=1`（**独立旋钮，不复用 `pick=`**）—— 走
        /// `RuleCore.PlayTactic` 时优先把**敌方的第一个非督军单位**当 `chosen` 传进去（治「`Side == "any"`
        /// 的卡点对面却打到自己人同号格」那条缺口的**量具盲区**：`EffectText.PickTarget` 对 `Side=="any"`
        /// 返回 null ⇒ 默认这一路根本不传目标）。⛔ 不传 `tpick=1` 时这一路一个字节都不变。
        /// </summary>
        public int Pick;

        /// <summary>`tpick=1` —— 非单位卡的「点定目标」（见 <see cref="Pick"/> 上面那段；⛔ 与 `Pick` 分开）。</summary>
        public int TacticPick;

        // ==================================================================
        //  🆕 `B8_AV`（2026-10-26）：「手牌态监听卡 + 触发一个事件」夹具
        // ==================================================================
        //
        // 🔴 **它治的是哪一条**（`B8_AE` 报告 §④·1 原话）：
        //   「`play all dump`（1128 主档 ＋ 586 电池）改前/改后 **逐字节相同** ⇒ 现有夹具测不到这条
        //    （不是「没改」而是**量具盲区**）。原因：`play` 的夹具把被验的那张卡**先打到棋盘上**，
        //    另外 10 张监听卡根本不在场；而 11 张可达监听卡在夹具里**从来不会以「手牌态」遇到这 5 个事件**。」
        //   ⇒ 本夹具让「**某张卡以手牌态在场**」成为可指定的起始条件，再把事件**打进去**。
        //
        // **形状**：`hand=<卡id|卡名|self>` ＋ `event=<事件>`（⛔ 两个都得给 —— 只给一个就出声退出）。
        //   · `hand=self` = 就用 `play <id>` 指的那一张；写卡 id / 卡名 = 换成池内另一张。
        //   · 放进 P0 手牌走引擎自己的 **`NewInstance` → `Hand.Add` → `SetupCardInHand`**
        //     （与 `ApplyPreset` 的 `enemyhand` / `RuleCore.SetDefensiveCard` **同一个口**，
        //      ⛔ 不自己拼「半份实例」）。
        //   · `handn=N`（默认 1）= 放几张（同名多张时用来看「会不会连锁响」）。
        //   · `amount=N`（默认 1）= 事件给多少（`Gain 1 Faith` 的 1）。
        //   · `legacy=1` = **改前形状的复现**（见 `FireHandEvent`）：广播走**四跳**（默认参数），
        //     也就是 2026-10-26 那 9 个发射点加 `boardAndPersistentOnly: true` **之前**的行为。
        //     ⇒ 「改前 / 改后应当不同」的那一对读数就是**同一条命令加不加 `legacy=1`**。
        //
        // ⚠️ **这是量具干预**（与 `preset dead=` / `FixtureFaith` 同一档）：它**不**声称原版开局手里有这张牌；
        //   它只回答「这张卡躺在手里时，那 5 个广播**会不会把它叫醒**」。
        // ⛔ 本夹具**不改变**任何既有读数：`hand=` / `event=` 都不传时 `MeasurePlay` 那条路一个字节都不变。
        /// <summary>`hand &lt;卡id|卡名|self&gt;` —— 以**手牌态**放进 P0 手牌的那一张（见上面整段注释）。</summary>
        public string Hand;
        public int HandCount = 1;
        /// <summary>`event &lt;gainfaith|gainquest|gainspirit|createssecret|createssabotage&gt;` ——
        /// 要打进去的那个广播（取值就是 `WhenEventKind` 的常量串，⛔ 不另立一套名字）。</summary>
        public string Event;
        public int EventAmount = 1;
        /// <summary>`legacy=1` —— **改前形状**：广播不带 `boardAndPersistentOnly`（= 四跳，会把
        /// 手牌里那些监听者真的叫醒并 `ResolveOps`）。⛔ 这是**复现历史缺陷**用的，不是产品路径。</summary>
        public bool Legacy;

        // ==================================================================
        //  🆕 `B9_A3`（2026-10-26）：**面朝下的伏击单位出手**夹具
        // ==================================================================
        //
        // 🔴 它治的是哪一条（`A1` 本批的原话）：探针电池的 `selfChip`（`ApplyDamage(被测单位, 2)`）
        //   排在两发攻击**之前**，而引擎 `ApplyDamage` 一挨伤害就把 `FaceDown` 置假
        //   ⇒ 「面朝下的伏击单位出手」那一支**结构上到不了**；第二发又带 `forced:true`，本来也不走。
        //   ⇒ 没有这条夹具之前，「伏击翻开之后再补打这一记」在产品里**零可观察量**。
        //
        // **形状**：`play <卡id|卡名> preset face=ambush`（见 <see cref="RunFaceAmbush"/>）。
        // ⚠️ 这是量具干预（与 `preset dead=` / `hand=` 同一档）；⛔ 不传 `face=` 时一条都跑不到。
        /// <summary>`face=ambush` —— 把被测单位摆成「面朝下 + 带 `ambush`」再让它出手（只认这一个取值）。</summary>
        public string Face;

        public bool FaceAmbushMode { get { return Face != null; } }

        // ==================================================================
        //  🆕 `B9_W32S3`（2026-10-27）：**「打得动的靶子」夹具**（`big=1`）
        // ==================================================================
        //
        // 🔴 **它治的是哪一条**（`B9_W32S3` 现读）：`play all dump` 的 `steps` 列里，
        //   **28 行 / 22 张卡**的 `softAtk` 一律是 `skip(would-kill)` ⇒ 电池那一发
        //   「**非致命**攻击」结构上打不出去（见 `RunEventPass` 里 `softAtk` 那一段）。
        //
        //   **两条互相独立的挡法，都被同一个靶子解决**（判据 = 那一段的两个 `continue`）：
        //     ① `t.Health <= dmg` —— 夹具敌人全是 **6 血**；`BL31`(近战 9) / `SAU33`(近战 10) /
        //        `DA66`(远程 9) / `GOF17`(7/7) / `AM31`(6/6) 这一批**一发就把三张全打死**。
        //     ② `me.Health <= t.Attack && !me.Has("longrange")` —— 被测单位先被 `selfChip`
        //        削到 1 血（`AM4` 只有 2 血 ⇒ 削到 1），而三张夹具敌人的攻击是 2/3/1
        //        ⇒ **1 ≤ 2/3/1 恒真** ⇒ 一律跳过（怕它被反击打死）。
        //   ⇒ 一个 **0 攻 / 30 血**的靶子同时满足两条：血够厚（打得动打不死）、攻击为 0（不会被反击）。
        //
        // ⚠️ **这是量具干预**（与 `preset dead=` / `pick=` / `hand=` 同一档）：它**不**声称原版有这样一个单位；
        //   它只回答「给这一族一个**挨得住、也打不回来**的靶子，那一发非致命攻击会不会把事件叫醒」。
        // ⛔ **不传 `big=1` 时夹具一个字节都不变** —— 全池基准读数与 `play check` 的基线**完全不受影响**
        //   （与 `dead=` / `pick=` 不同：那两个进了第二/第三套扫描，`big` 目前**没有**进任何自动扫描，
        //    只在 `play <id> preset big=1` 这一条路上生效 ⇒ 零基线影响、零重钉）。
        /// <summary>`big=1` —— 往 P1 摆一个 **0 攻 / 30 血**的合成部队当「打得动的靶子」（见上面整段注释）。</summary>
        public int Big;

        // ==================================================================
        //  🆕 `B9_W41S3B`（2026-10-27）：**把被测单位自己打死**（`selfDies=1`）
        // ==================================================================
        //
        // 🔴 **它治的是哪一条**（`B9_W32S3` 分族表 §三·3.2 的 D 族 **81 Unstable**，17 行 / 17 张）：
        //   引擎的 `Unstable` 挂在「**本单位死亡**」那一拍上（`RuleCore.cs` 的
        //   `if (u.Has(KeywordTable.Unstable)) EnqueueAbility(… UnstableBlastDeferred …)`，
        //   位置在 `CleanupDeaths` 的死亡族里、Backlash(80) 之后）。
        //   而现有电池的 `friendDies` **只杀 P0 那颗路标石**（`FIX_WAY`），被测单位自己**从不死**
        //   ⇒ 「本单位死亡」这个事件**结构上造不出来** ⇒ 整族永远净差 0。
        //
        // ⚠️ **为什么不用 `ApplyDamage(999)`**（设计稿原话）：`RuleCore.ApplyDamage` **不处理死亡**
        //   （它只扣血/发事件；离场在 `Hurt` → `RemoveIfDead` → `CleanupDeaths`）
        //   ⇒ 999 之后棋盘上留下的是**负血活单位**、一条死亡触发都不响（这正是 `friendDies`
        //   当年踩过的坑，见那里的一整段注释）。所以这里走**引擎自己的伤害/死亡入口**
        //   `RuleCore.HurtWithSourceForTest`（= `Hurt` 的公开包装，`RuleCore.cs:9131`）。
        //
        // ⚠️ **这是量具干预**（与 `dead=` / `pick=` / `big=` 同一档）：它不声称原版有这样一个事件顺序；
        //   它只回答「让这一张**自己死一次**，挂在本单位死亡上的那一族会不会响」。
        // ⛔ **不传 `selfdies=1` 时 `steps` 与读数一个字节都不变** —— 这一步**不进**默认电池，
        //   所以全池 `play check` 的 `PinnedPlayMd5` **不受影响**（⛔ 别把它加成无条件的一步）。
        /// <summary>`selfdies=1` —— 电池末尾把**被测单位自己**打死（走 `Hurt`，治 `Unstable 81`）。</summary>
        public int SelfDies;

        // ==================================================================
        //  🆕 `B9_W41S3B`（2026-10-27）：**督军档的电池宿主**（`hero=1`）
        // ==================================================================
        //
        // 🔴 **它治的是哪一条**（分族表 §一 的 **A1 族 = 144 行 / 56 张**，本表最大的夹具形状缺口）：
        //   现有 `MeasureHero` 只跑「**在场 + 一个来回**」的对照局 ⇒ 督军那 144 行里
        //   「**主动能力**（trigger=None，61 行）」与「**Talent**（trigger=46，56 行）」两档
        //   **从来没有被主动用过**：
        //     · `activeAbility` 要有人调 `RuleCore.UseAbility`；
        //     · `Talent` 是**回合开始时造一张临时卡进手**（`RuleCore.SpawnTalents`），
        //       而夹具**从不把那张卡打出去** ⇒ 那张卡的效果一条都没结算过
        //       （实测 `play BL34`：手上确实有 `Chosen of the Four`，但「变化逐条」里只有手牌总价那一行噪声）。
        //   ⇒ 本档 = **一条新的电池宿主**：以被测卡为 **P0 督军**建场 → ① 打出手里那张天赋卡
        //   ② 用主动能力 → 再把**同一套电池步**跑一遍（与单位档**共用** `RunBatterySteps`）。
        //
        // ⚠️ **这不是 preset 能给单位档加的一步**（⛔ 别把督军塞进 `if (c.Type == "unit")` 那条）：
        //   督军既不能「打出」（`PlayCard` 只处理非 hero），也不在 `Sweep` 的 unit 电池里。
        // ⛔ **不传 `hero=1` 时一个字节都不变**：`MeasureHero` / `MeasurePlay` 的既有读数、
        //   `play check` 的两套指纹行**完全不经过这里**（本档只在 `Sweep` 末尾**追加** `H|` 行，
        //   而 `BothFingerprintRows`（`play check` 用的那一路）**不调**它）。
        /// <summary>`hero=1` —— 督军档跑**新的电池宿主**（被测卡当 P0 督军 → 打天赋 → 用主动能力 → 走电池步）。</summary>
        public int HeroBattery;

        // ==================================================================
        //  🆕 `B9_W41S3B`（2026-10-27）：**对照局要剥掉的关键词**（`strip=unstable`）
        // ==================================================================
        //
        // 🔴 **它治的是哪一条（本批第二个量具缺陷，比 `selfdies` 更根本）**：
        //   电池的对照局 = `Blank(card)`，它**逐字保留 `keywords`**（那是**故意的**：`Armour` /
        //   `Flank` / `Fast` 会改战斗算术，两局必须逐位相同，见 <see cref="Blank"/> 的 doc）。
        //   可是有一批效果**根本不是 `desc` 里的正文，而是【关键词本身】的硬编码分支** ——
        //   典型 = **`Unstable`**：`RuleCore.cs` 的死亡族里 `if (u.Has(KeywordTable.Unstable)) EnqueueAbility(…)`，
        //   `desc` 里那行 `Unstable.` 只是**印字**、`KeywordTable.EffectText` 对它返回 null。
        //   ⇒ 对照局**也带着 `unstable`** ⇒ 两局都自爆、自爆的那点伤害**相减抵消**
        //   ⇒ 这一族**结构上永远净差 0**（实测 `GOF10 Grot Tank`：电池局 49 条、对照局 49 条、净差 0）。
        //
        // **做法**：只在**显式传 `strip=`** 时，把列出的关键词从**对照局那张替身**上剥掉
        //   （⛔ 不动被测卡那一局；⛔ 缺省 null ⇒ `Blank` 一个字节都不变）。
        //   ⚠️ **只能剥「纯效果型」关键词**（`unstable` 这种：它只喂死亡触发、不参与任何算术）；
        //      `armour`/`flank`/`fast`/`shield`/`dodge` 一剥就会让两局**算术不同** ⇒ 净差变噪声。
        //   ⚠️ 这是**量具干预**，读数里必须能看见（`ApplyPreset` 那行日志会打出来）。
        /// <summary>`strip=unstable` —— 对照局替身要剥掉的关键词（逗号分隔；见上面整段注释）。</summary>
        public string StripKw;

        public bool HandListenMode { get { return Hand != null || Event != null; } }

        public bool Any { get { return Dead > 0 || EnemyHand > 0 || ChipEnemy > 0
                                    || !string.IsNullOrEmpty(Substitute) || Legal > 0
                                    || HandListenMode || Big > 0 || SelfDies > 0 || HeroBattery > 0
                                    || !string.IsNullOrEmpty(StripKw); } }

        /// <summary>`dead[=N][:Subtype][,enemyhand=N][,chipenemy=1]`。⛔ 认不出的 token 一律**抛错**（不静默忽略 ——
        /// 静默忽略会让你以为预置了、其实没有，正是本仓红线里「静默失败」那一类）。</summary>
        public static PresetSpec Parse(string s)
        {
            var ps = new PresetSpec();
            foreach (var raw in (s ?? "").Split(','))
            {
                string t = raw.Trim();
                if (t.Length == 0) continue;
                string head = t, rest = "";
                int eq = t.IndexOf('=');
                if (eq >= 0) { head = t.Substring(0, eq).Trim(); rest = t.Substring(eq + 1).Trim(); }
                else
                {
                    int c0 = t.IndexOf(':');
                    if (c0 >= 0) { head = t.Substring(0, c0).Trim(); rest = t.Substring(c0 + 1).Trim(); }
                }
                string sub = null, val = rest;
                int colon = rest.IndexOf(':');
                if (colon >= 0)
                {
                    val = rest.Substring(0, colon).Trim();
                    sub = rest.Substring(colon + 1).Trim();
                }
                switch (head.ToLowerInvariant())
                {
                    case "dead":
                        int dn;
                        if (string.IsNullOrEmpty(val)) { ps.Dead = 1; }
                        else if (int.TryParse(val, out dn)) { ps.Dead = dn; }
                        else { ps.Dead = 1; sub = sub ?? val; }   // `dead:Infantry` 那种写法
                        if (!string.IsNullOrEmpty(sub)) ps.DeadSubtype = sub;
                        break;
                    case "enemyhand":
                        ps.EnemyHand = string.IsNullOrEmpty(val) ? 1 : int.Parse(val);
                        break;
                    case "chipenemy":
                        ps.ChipEnemy = string.IsNullOrEmpty(val) ? 1 : int.Parse(val);
                        break;
                    // 🆕 `B9_W32S3`（2026-10-27）：`big=1` —— 往 P1 摆一个「打得动的靶子」
                    //   （0 攻 / 30 血）。见 `PresetSpec.Big` 那一整段注释（⛔ 不动默认夹具）。
                    case "big":
                        ps.Big = string.IsNullOrEmpty(val) ? 1 : int.Parse(val);
                        break;
                    // 🆕 `B9_W41S3B`（2026-10-27）：`selfdies=1` —— 电池末尾把**被测单位自己**打死
                    //   （治 `Unstable 81`；见 `PresetSpec.SelfDies` 的整段注释）。
                    case "selfdies":
                        ps.SelfDies = string.IsNullOrEmpty(val) ? 1 : int.Parse(val);
                        break;
                    // 🆕 `B9_W41S3B`（2026-10-27）：`hero=1` —— 督军档的**电池宿主**
                    //   （治 A1 族；见 `PresetSpec.HeroBattery` 的整段注释）。
                    case "hero":
                        ps.HeroBattery = string.IsNullOrEmpty(val) ? 1 : int.Parse(val);
                        break;
                    // 🆕 `B9_W41S3B`（2026-10-27）：`strip=unstable` —— 对照局替身要剥掉的关键词
                    //   （见 `PresetSpec.StripKw` 的整段注释；⛔ 只认「纯效果型」关键词）。
                    case "strip":
                        ps.StripKw = string.IsNullOrEmpty(rest) ? null : rest.Trim();
                        if (string.IsNullOrEmpty(ps.StripKw))
                            throw new ArgumentException("`strip=` 后面要写关键词（例：`strip=unstable`）");
                        break;
                    // 🆕 `B8_AH`（2026-10-26）：`sub=<卡id|卡名>` —— 把夹具的 `FIX_A` 换成池内一张**真卡**
                    //   （见 `PresetSpec.Substitute` 的 doc；⛔ 认不出就抛错，不静默忽略）。
                    case "sub":
                        ps.Substitute = string.IsNullOrEmpty(rest) ? null : rest;
                        if (string.IsNullOrEmpty(ps.Substitute))
                            throw new ArgumentException("`sub=` 后面要写卡 id 或卡名");
                        break;
                    // 🆕 `B8_AH`（2026-10-26）：`legal=1` —— 目标格改由**产品代码**的合法性判定挑
                    //   （`RuleCore.IsLegalPlayTarget`）并把合法池打进日志；见 `PresetSpec.Legal` 的 doc。
                    case "legal":
                        ps.Legal = string.IsNullOrEmpty(val) ? 1 : int.Parse(val);
                        break;
                    // 🆕 `P3`（2026-10-26）：`pick=1` —— 单位卡也给它一个「玩家点定的目标」
                    //   （见 `PresetSpec.Pick` 的 doc；判据 = 原版 `ta=30` 那一支与 `targeted` 两道闸）。
                    case "pick":
                        ps.Pick = string.IsNullOrEmpty(val) ? 1 : int.Parse(val);
                        break;
                    // 🆕 `E9ER`（2026-10-26）：`tpick=1` —— **非单位卡**（战术/防御）也给它一个「玩家点定的目标」。
                    //   ⛔ **故意不复用 `pick=`**：`pick=1` 已经被 `P3` 用于单位卡、且 `pool_effect_check`
                    //   的第二套夹具就靠它 ⇒ 扩义会**静默改掉那批既有读数**。新键 = 新读数、老键一字不动。
                    case "tpick":
                        ps.TacticPick = string.IsNullOrEmpty(val) ? 1 : int.Parse(val);
                        break;
                    // 🆕 `B8_AV`（2026-10-26）：手牌态监听卡 ＋ 事件（见 `HandListenMode` 上面那一整段注释）。
                    case "hand":
                        // ⚠️ 用 `rest`（不是 `val`）—— 卡名里可能带 `:`；这里整段就是那张卡的名字/id。
                        ps.Hand = string.IsNullOrEmpty(rest) ? null : rest;
                        if (string.IsNullOrEmpty(ps.Hand))
                            throw new ArgumentException("`hand=` 后面要写卡 id / 卡名 / `self`");
                        break;
                    case "handn":
                        ps.HandCount = string.IsNullOrEmpty(val) ? 1 : int.Parse(val);
                        break;
                    case "event":
                        ps.Event = string.IsNullOrEmpty(rest) ? null : rest;
                        if (string.IsNullOrEmpty(ps.Event))
                            throw new ArgumentException("`event=` 后面要写事件名（gainfaith / gainquest / gainspirit "
                                                      + "/ createssecret / createssabotage）");
                        break;
                    case "amount":
                        ps.EventAmount = string.IsNullOrEmpty(val) ? 1 : int.Parse(val);
                        break;
                    case "legacy":
                        ps.Legacy = (string.IsNullOrEmpty(val) || val != "0");
                        break;
                    // 🆕 `B9_A3`（2026-10-26）：`face=ambush` —— 面朝下 + 带 `ambush` 的出手夹具
                    //   （见 `PresetSpec.Face` 上面那一整段注释；⛔ 认不出就抛，不静默忽略）。
                    case "face":
                        ps.Face = string.IsNullOrEmpty(rest) ? null : rest.Trim();
                        if (string.IsNullOrEmpty(ps.Face)
                            || !string.Equals(ps.Face, "ambush", StringComparison.OrdinalIgnoreCase))
                            throw new ArgumentException("`face=` 今天只认 `ambush`（现读 `" + (ps.Face ?? "") + "`）");
                        break;
                    default:
                        throw new ArgumentException("不认识的预置项 `" + raw + "`（只认 dead / enemyhand / chipenemy / sub / legal"
                                                  + " / pick / tpick / big / selfdies / hero"
                                                  + " / hand / handn / event / amount / legacy / face）"
                                                  + "　⚠️ `preset` 是用**逗号**分项的 ⇒ 卡名里带逗号的两张"
                                                  + "（`Varius, Lord Kakophonist` / `Xarahan, Daemon Prince`）"
                                                  + "在 `hand=` / `sub=` 里会被切碎，请改用**卡 id**");
                }
            }
            if (ps.Dead < 0 || ps.EnemyHand < 0) throw new ArgumentException("张数不能为负");
            return ps;
        }
    }

    /// <summary>把预置局面写进夹具（**调用点在 `BuildFixture` 取快照之前** ⇒ 预置本身不出现在「变化逐条」里）。</summary>
    static void ApplyPreset(BattleContext ctx, PresetSpec ps)
    {
        if (ctx == null || ps == null || !ps.Any) return;
        for (int i = 0; i < ps.Dead; i++)
        {
            var def = Fix("FIX_DEAD" + i, 2, 3, 0, ps.DeadSubtype ?? "Infantry");
            var inst = ctx.NewInstance(def);
            ctx.Players[0].Discard.Add(inst);
            ctx.DeadUnits.Add(new DeadUnit { Card = def, Owner = 0, DeathTurn = ctx.Turn });
        }
        for (int i = 0; i < ps.EnemyHand; i++)
        {
            var def = Fix("FIX_EH" + i, 1, 2, 0, "Infantry");
            var inst = ctx.NewInstance(def);
            ctx.Players[1].Hand.Add(inst);
            RuleCore.SetupCardInHand(ctx, 1, inst);
        }
        // `A2001` 的 `UM2 Octavio Infiltrator`（`Rally: Give Blind to a **damaged** enemy`）：
        //   默认夹具里敌人**全是满血** ⇒ 没有合法目标。这里走引擎自己的 `ApplyDamage`（与 P0 那三张
        //   同一个口，⛔ 不手改 `Health`），只打**非督军**单位（督军 30 血，扣 2 不改变任何判据）。
        if (ps.ChipEnemy > 0)
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = ctx.Players[1].Board[s];
                if (u != null && !u.IsWarlord) RuleCore.ApplyDamage(ctx, u, FixtureChip, "夹具预置扣血（敌方）");
            }
        ctx.Log($"[夹具预置] 墓地 P0 +{ps.Dead} 张（{(ps.DeadSubtype ?? "Infantry")}）"
              + $" · 手牌 P1 +{ps.EnemyHand} 张 · 敌方扣血 {(ps.ChipEnemy > 0 ? "开" : "关")}"
              + $" · 靶子(0攻/30血) {(ps.Big > 0 ? "有" : "无")}"
              + $" · 电池末尾自毁 {(ps.SelfDies > 0 ? "开" : "关")}"
              + $" · 督军电池宿主 {(ps.HeroBattery > 0 ? "开" : "关")}"
              + (string.IsNullOrEmpty(ps.Substitute) ? "" : $" · `FIX_A` 换成真卡 `{ps.Substitute}`"));
    }

    /// <summary>建一局夹具对战。`warlordOverride` 非 null 时用它当 P0 督军（督军档的对照局用）。
    /// 🔴 `injected`（`A1572`）：被测卡**是**靠补手牌才进得了手牌的那种（= 量具干预过）——
    ///    两族：临时卡 / 防御卡，见下面那一段注释。
    /// 🆕 `A2009`：`preset` 非 null 时在建场末尾**预置墓地 / 敌方手牌**（见 <see cref="PresetSpec"/>）。
    /// 🆕 `B9_W41S3B`（2026-10-27）：`factionOverride` —— 夹具双方的阵营。
    ///   缺省 `null` ⇒ 逐字沿用旧规则（`target?.Faction ?? "P0"/"P1"`），⛔ 一个字节都不变。
    ///   为什么要有它：**督军档**（`target == null`）原来恒用 `"P0"`/`"P1"` 这种**池里不存在的阵营**，
    ///   而「造牌不写阵营 ⇒ 按施放者阵营」读的是 `ps.Warlord.Card.Faction`（见下面 `p0Fac` 那一段注释）
    ///   ⇒ 督军电池里「按阵营造牌 / 按阵营筛」那一族会**一张都筛不到**（假读数）。
    ///   ⚠️ 只在新宿主（`RunHeroBattery`）里传它；既有两条路的调用点**一个字都没改**。</summary>
    static BattleContext BuildFixture(List<CardDef> pool, CardDef target, CardDef warlordOverride,
                                      out int handIdx, out bool injected, PresetSpec preset = null,
                                      string factionOverride = null)
    {
        var vars = GameplayVariables.Classic.Clone();
        vars.startingHand = FixtureHand;      // ⚠️ 只改「发几张」—— 别的一律照 `Classic`
        injected = false;                     // 见函数头：只有真走补手牌那一步才置真

        // 兵种**故意分散**（Infantry / Vehicle / Beast）—— 卡面上 `a friendly Vehicle` /
        // `your Beasts` 那一族才找得到目标（⛔ 全用 `Infantry` 会把整族误列成「没目标」）。
        var a = Fix("FIX_A", 2, 4, 0, "Infantry");
        // 🆕 `B8_AH`（2026-10-26）：`preset sub=<卡id|卡名>` ⇒ 把 `FIX_A` 换成池内**真卡**。
        //   为什么：`Fix(...)` 现造的 `CardDef` **不带 `keyword`** ⇒ 条件 275/276/277 走 `Subtype` 退回支
        //   ⇒ 「原版 `keyword(+0x98)` 透传」在打出路径上**测不到**。换 `BL1 Chaos Spawn`
        //   （`Subtype=Infantry` 而 `keyword=Daemon`）就能看到条件 277 的候选集变化。
        //   ⚠️ 只用「卡 id / 卡名**精确**」匹配；认不出 ⇒ 抛（⛔ 不静默退回合成部队）。
        CardDef substituted = null;
        if (preset != null && !string.IsNullOrEmpty(preset.Substitute))
        {
            var real = pool.FirstOrDefault(c => string.Equals(c.Id, preset.Substitute, StringComparison.OrdinalIgnoreCase)
                                             || string.Equals(c.Name, preset.Substitute, StringComparison.OrdinalIgnoreCase));
            if (real == null)
                throw new ArgumentException("`sub=` 指定的卡在池内找不到（按 id / 卡名精确）：" + preset.Substitute);
            if (real.Type != "unit")
                throw new ArgumentException("`sub=` 只能换**部队** `type=unit`（夹具 `FIX_A` 的位置是部队）："
                                          + real.Id + " type=" + real.Type);
            a = real;
            substituted = real;
        }
        var b = Fix("FIX_B", 3, 5, 0, "Vehicle");
        var c = Fix("FIX_C", 1, 4, 0, "Beast");
        var e1 = Fix("FIX_E1", 2, 6, 0, "Infantry");
        var e2 = Fix("FIX_E2", 3, 6, 0, "Vehicle");
        var e3 = Fix("FIX_E3", 1, 6, 0, "Beast");

        // ⚠️ **督军卡不进牌库**（`BuildPlayer` 把第一张 `type == "hero"` 当督军）
        //    ⇒ 督军档走 `warlordOverride`，被测卡不参与发牌（见 `MeasureHero`）。
        var hand = target != null && target.Type != "hero"
            ? new[] { a, b, c, target }
            : new[] { a, b, c };
        // 🔴 **2026-10-19（`A2001` 残余 · `EC17` 夹具阵营盲区）：督军阵营 = 被测卡的真实阵营。**
        //   原版「造牌没写阵营 ⇒ 按**施放者阵营**」读的是 `ps.Warlord.Card.Faction`
        //   （`EffectResolver.cs` 的 `CreatePool` / `DoCreate` 三处：`:2456` `:2753` `:7740`）；
        //   夹具原来用 `"P0"` / `"P1"` 这种**池里不存在**的阵营 ⇒
        //   `EC17 Konstrictus Tormentor` 的 `At the end of your turn, create a random Combat Elixir`
        //   在 `EmperorsChildren` 的 6 张 Elixir 里**一张都筛不到**（E9 `§A2001` 第 230 行）。
        //   真实对局里督军与卡组**同阵营**（构筑规则）⇒ 用被测卡的阵营才是**保真的量具**。
        //   ⚠️ 这是**量具改变**、不是改引擎：`target == null`（对照局）/ 阵营为空时退回 `"P0"`/`"P1"`。
        //   ⚠️ 它会让「按阵营造牌」那一族**开始真的造出牌** ⇒ 全池电池读数会变（与 `A1965` 同批重钉）。
        string p0Fac = target != null && !string.IsNullOrEmpty(target.Faction) ? target.Faction : "P0";
        string p1Fac = target != null && !string.IsNullOrEmpty(target.Faction) ? target.Faction : "P1";
        // 🆕 `B9_W41S3B`：督军电池宿主显式给阵营（见函数头 `factionOverride` 那一段）。缺省 null ⇒ 逐字不变。
        if (!string.IsNullOrEmpty(factionOverride)) { p0Fac = factionOverride; p1Fac = factionOverride; }
        var d0 = Deck(p0Fac, hand);
        // 督军档的**对照局**：只把 P0 督军换成指定的那一张，别的逐字不变。
        if (warlordOverride != null) { d0.RemoveAt(0); d0.Insert(0, warlordOverride); }
        // 🆕 `B9_W32S3`（2026-10-27）：`preset big=1` ⇒ P1 多一张 **0 攻 / 30 血**的合成部队
        //   （见 `PresetSpec.Big` 的整段注释）。**只在这一档**加 —— 默认夹具一个字节都不变。
        bool bigOn = preset != null && preset.Big > 0;
        var eBig = bigOn ? Fix("FIX_BIG", 0, 30, 0, "Infantry") : null;
        var d1 = bigOn ? Deck(p1Fac, e1, e2, e3, eBig) : Deck(p1Fac, e1, e2, e3);

        var ctx = RuleCore.NewBattle(d0, d1, seed: 0, shuffle: false, cardPool: pool, vars: vars);

        SetEnergy(ctx, 0);
        SetFaction(ctx, 0);      // 🆕 `A1943`：起始派系资源（⛔ 与 `SetEnergy` 成对，别只跟一处）
        for (int i = 0; i < 3; i++)
        {
            int hi = HandIdx(ctx, 0, hand[i]);
            if (hi >= 0) RuleCore.PlayCard(ctx, 0, hi, DeploySlots[i]);
        }
        RuleCore.EndTurn(ctx); RuleCore.BeginTurn(ctx);      // → P1
        SetEnergy(ctx, 1);
        SetFaction(ctx, 1);      // 🆕 `A1943`
        RuleCore.PlayCard(ctx, 1, HandIdx(ctx, 1, e1), DeploySlots[0]);
        RuleCore.PlayCard(ctx, 1, HandIdx(ctx, 1, e2), DeploySlots[1]);
        RuleCore.PlayCard(ctx, 1, HandIdx(ctx, 1, e3), DeploySlots[2]);
        // 🆕 `B9_W32S3`：靶子落在**第 9 格（下标 8）** —— 与既有三张（3/6/7）不撞、仍在督军右侧序列里。
        if (bigOn) RuleCore.PlayCard(ctx, 1, HandIdx(ctx, 1, eBig), BigSlot);
        RuleCore.EndTurn(ctx); RuleCore.BeginTurn(ctx);      // → P0（被测卡这一手）

        SetEnergy(ctx, 0);
        SetFaction(ctx, 0);      // 🆕 `A1943` —— 🔴 这一处是**被测卡那一拍**看得见的那一次：
                                 //    被测卡打出去时用的就是这里给的三件（前面两处只服务摆场）
        // 🔴 **把我方三张部队各扣 2 血**（见 `FixtureChip`）—— 不扣的话满血治疗看不见。
        //    ⚠️ 走引擎自己的 `ApplyDamage`（⛔ 不直接改 `u.Health`）：它会走盾/闪避/触发那一套，
        //       与我们手改出来的状态**不同口径**。它引发的那些副作用都发生在**基线快照之前**。
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            var u = ctx.Players[0].Board[s];
            // 🆕 `B8_AH`：`sub=` 换进来的那张**不扣血** —— 「扣 2 血」是为了让「满血治疗」看得见，
            //   与预置换进来的真卡无关；而真卡可能只有 1 血（`BL1 Chaos Spawn` = 2/1）⇒ 扣了就死、
            //   条件 277 的候选集**当场看不见**（实测：不跳过时 `play` 直接 `ErrNoTargetAvailable`）。
            if (u != null && !u.IsWarlord && !ReferenceEquals(u.Card, substituted))
                RuleCore.ApplyDamage(ctx, u, FixtureChip, "夹具扣血");
        }
        // 🔴 **我方督军也扣 2 点** —— 不扣的话 `Heal 4 to your Warlord`（`SW60`）/`Heal N to your Warlord`
        //    那一族**满血治疗 = 什么都不发生**，会被整族误列成候选（与上面三张同一个道理）。
        RuleCore.ApplyDamage(ctx, ctx.Players[0].Warlord, FixtureChip, "夹具扣血");
        // 🆕 `A2009`：**预置局面**（默认 `preset == null` ⇒ 一个字节都不做）。
        //   ⚠️ 位置 = 取快照之前、`handIdx` 计算之前 —— 预置不改 P0 手牌 ⇒ 被测卡的下标不受影响。
        ApplyPreset(ctx, preset);
        handIdx = target == null ? -1 : HandIdx(ctx, 0, target);
        // 🔴 **被测卡没活到这一手时，按引擎自己的形状补进手牌**（`A1431` 实测：不补会漏掉 **137 张**）：
        //   · **临时卡（`ephemeral`，98 张 `tactic`）** —— `RuleCore.EndTurn:1340` 的 `SweepEphemeral`
        //     在回合结束时把本方手牌里的临时卡**弃掉** ⇒ 跨一个来回之后它**不在手里**
        //     （这是引擎**对的**行为，不是缺陷 —— 是夹具要照顾它）。
        //   · **防御卡（39 张）** —— `BuildPlayer` 把 `type == "defence"` 从牌库里**分流出去**
        //     （不进牌库、由 `GrantDefenceCard` 发给**后手**）⇒ 先手（P0）手里根本不会有它。
        //   ⇒ 形状照引擎自己的 `RuleCore.SetDefensiveCard`：`NewInstance` → `Hand.Add` → `SetupCardInHand`
        //     （⛔ 不自己拼一个「半份实例」，那会与引擎口径不同）。
        //   ⚠️ 这一段跑在 `MeasurePlay` 取**基线快照之前** ⇒ 补进来的那一张**在读数里看得见**。
        if (target != null && handIdx < 0)
        {
            var late = ctx.NewInstance(target);
            ctx.Players[0].Hand.Add(late);
            RuleCore.SetupCardInHand(ctx, 0, late);
            handIdx = HandIdx(ctx, 0, target);
            injected = true;
        }
        return ctx;
    }

    // ==================================================================
    //  单卡读数
    // ==================================================================

    static Reading Measure(List<CardDef> pool, CardDef card, PresetSpec preset = null)
    {
        var r = new Reading { C = card };
        try
        {
            r.FaceHasOps = FaceHasOps(card);
            r.Bucket = Bucket(card);
            if (card.Type == "hero") MeasureHero(pool, card, r, preset);
            else MeasurePlay(pool, card, r, preset);
        }
        catch (Exception ex)
        {
            // 🔴 引擎抛异常 = 缺陷。**出声**（⛔ 不许静默）—— 但⛔ 不在这里判因由。
            r.Crash = ex.GetType().Name + ": " + ex.Message;
            r.Note = "**真打时引擎抛异常**";
        }
        return r;
    }

    // ==================================================================
    //  🆕 `B8_AV`（2026-10-26）：**手牌态监听卡 + 触发一个事件**
    // ==================================================================
    //
    // 🔴 为什么单开这一档（`B8_AE` 报告 §④·1）：`play all dump` 对「手牌态监听卡」是**盲的** ——
    //   它把被验的那张卡**先打到棋盘上**，于是 11 张可达监听卡（`SOR38` / `DA5` / `DA12` / `DA6` /
    //   `ASH80` / `ASH28` / `ASH78` / `ASH25` / `GSC68` / `DA15` / `DA66`）在夹具里
    //   **从来不会以「手牌态」遇到那 5 个广播** ⇒ 5 条广播收窄成「只跳棋盘」前后 dump 逐字节相同。
    //
    // ⚠️ **本档不「真打一发」**：它摆好局面（含手牌里那张监听卡）→ 取快照 → **把事件打进去** → 再取快照 → 逐条比。
    //   ⇒ 读数回答的是「**这张卡躺在手里时，那个广播会不会把它叫醒并真的结算**」。
    //   ⛔ 它**不改** `Measure` / `Sweep` / `Check` 的任何一格（不传 `hand=`/`event=` 就一行都跑不到这里）。
    static int RunHandListen(List<CardDef> pool, CardDef card, PresetSpec ps)
    {
        if (string.IsNullOrEmpty(ps.Hand) || string.IsNullOrEmpty(ps.Event))
        {
            Console.Error.WriteLine("!! `hand=` 与 `event=` **必须同时给**（只给一个的话，本档要问的问题本身不成立）："
                                    + "现读 hand=`" + (ps.Hand ?? "") + "` event=`" + (ps.Event ?? "") + "`");
            Console.Error.WriteLine("   例：play SOR38 preset hand=self,event=gainfaith"
                                    + "     （再跑一遍加 `,legacy=1` 看**改前**会多出什么）");
            return 2;
        }
        // 手牌里要放哪一张：`self` = `play <id>` 指的那一张；否则按 id / 卡名在池内精确找。
        CardDef subject = card;
        if (!string.Equals(ps.Hand, "self", StringComparison.OrdinalIgnoreCase))
        {
            subject = Find(pool, ps.Hand);
            if (subject == null)
            {
                Console.Error.WriteLine("!! `hand=` 指的卡在池内找不到（按 id / 卡名精确）：" + ps.Hand);
                return 2;
            }
        }
        int n = ps.HandCount <= 0 ? 1 : ps.HandCount;

        // 建夹具：`target = null` ⇒ **不把任何卡打出去**（这就是「手牌态」那一档与前几档的分界）。
        int handIdx; bool injected;
        var ctx = BuildFixture(pool, null, null, out handIdx, out injected);
        var added = new List<CardInstance>();
        for (int i = 0; i < n; i++)
        {
            // ⚠️ 形状照引擎自己的「补进手牌」那一跳（`RuleCore.SetDefensiveCard` /
            //    `BuildFixture` 里 `injected` 那一段 / `ApplyPreset` 的 `enemyhand`）：
            //    `NewInstance` → `Hand.Add` → `SetupCardInHand`。⛔ 不自己拼半份实例。
            var inst = ctx.NewInstance(subject);
            ctx.Players[0].Hand.Add(inst);
            RuleCore.SetupCardInHand(ctx, 0, inst);
            added.Add(inst);
        }

        var before = Capture(ctx);
        string pathDesc, err = FireHandEvent(ctx, ps, out pathDesc);
        var after = Capture(ctx);
        var changes = new List<string>();
        Diff(before, after, changes);

        Console.WriteLine("=== play " + card.Id + " · " + card.Name + " [" + card.Type + "] ‖ 手牌态监听夹具（`B8_AV`）===");
        Console.WriteLine("  被测/监听卡 : " + subject.Id + " · " + subject.Name + " [" + subject.Type + "]"
                          + (ReferenceEquals(subject, card) ? "（= `play` 指的那一张）" : "（`hand=` 指定的另一张）"));
        Console.WriteLine("  卡面（英文）: " + subject.Desc);
        Console.WriteLine("  放了几张    : " + n + " 张（都走 `NewInstance` → `Hand.Add` → `SetupCardInHand`，**手牌态**）");
        Console.WriteLine("  事件        : " + ps.Event + " × " + ps.EventAmount
                          + (ps.Legacy ? "  ⚠️ **legacy=1 —— 改前形状**（广播四跳，会把手里那些监听者叫醒）"
                                       : "  （产品形状）"));
        Console.WriteLine("  走的哪条路  : " + pathDesc);
        Console.WriteLine();
        if (err != null)
        {
            Console.WriteLine("  🔴 " + err);
            return 2;
        }
        // 手牌里那张卡**还在不在手里**（这一条是本档的前提：不在手里就不是「手牌态」了）
        int stillIdx = HandIdx(ctx, 0, subject);
        Console.WriteLine("  ── 事件之后，那张卡还在手牌里吗 ──");
        Console.WriteLine("     " + (stillIdx >= 0
                          ? "在（手牌下标 " + stillIdx + " · 手牌增益条数 "
                            + (ctx.Players[0].Hand[stillIdx].HandBuffOps == null ? 0
                               : ctx.Players[0].Hand[stillIdx].HandBuffOps.Count) + "）"
                          : "** 不在了 ** ← 本档的前提被破坏（读数的判读要另说，⛔ 别当成「监听没响」）"));
        Console.WriteLine();
        Console.WriteLine("  ── 事件之前 ──");
        Console.WriteLine("     " + Fingerprint(before));
        Console.WriteLine("  ── 事件之后 ──");
        Console.WriteLine("     " + Fingerprint(after));
        Console.WriteLine();
        Console.WriteLine("  ── 变化逐条（" + changes.Count + " 条）──");
        if (changes.Count == 0) Console.WriteLine("     （局面一个字段都没动 —— 手牌里那张监听者**没被叫醒**）");
        foreach (var ch in changes) Console.WriteLine("     " + ch);
        Console.WriteLine();
        Console.WriteLine("  ── 引擎自己的事件日志（末尾 16 条）—— 🔴「没响」时先看这里它自己说了什么 ──");
        var tail = ctx.Events.Skip(Math.Max(0, ctx.Events.Count - 16)).ToList();
        if (tail.Count == 0) Console.WriteLine("     （一条都没有）");
        foreach (var l in tail) Console.WriteLine("     " + Clip(l, 150));
        Console.WriteLine();
        Console.WriteLine("  ── 怎么读这一档（⛔ 本工具不判对错）──");
        Console.WriteLine("    · 原版那 5 个广播器**只有棋盘一跳**（`B8_AE` §②·1 逐份 `grep -c GetCardsInHandRef` = 0）");
        Console.WriteLine("      ⇒ **产品形状下「手牌里那张监听者」不该被叫醒**，变化 0 条 = 与卡面/原版一致；");
        Console.WriteLine("    · 加 `,legacy=1` 再跑一次 = **改前形状**（四跳）⇒ 两次读数**应当不同**；");
        Console.WriteLine("      不同 = 本夹具**有鉴别力**（它照得到那条差异），且产品当前**没有**多跳。");
        Console.WriteLine("    ⚠️ 变化 0 条也可能是「这张卡在手牌里本来就不监听」（拿逐触发头判：`traits <id>`）");
        Console.WriteLine("      ⇒ ⛔ 别把「0 条」单独当结论，务必配一次 `legacy=1` 对照。");
        return 0;
    }

    // ==================================================================
    //  🆕 `B9_A3`（2026-10-26）：**面朝下的伏击单位出手 ⇒ 翻开 + 续攻**
    // ==================================================================
    //
    // 🔴 它治的是哪一条（`A1` 本批的原话）：探针电池的 `selfChip`（`ApplyDamage(被测单位, 2)`）
    //   排在两发攻击**之前**，而引擎 `ApplyDamage` 一挨伤害就把 `FaceDown` 置假
    //   ⇒ 「面朝下的伏击单位出手」那一支**结构上到不了**；第二发又带 `forced:true`，本来也不走。
    //   ⇒ 没有这条夹具之前，「伏击翻开之后再补打这一记」在产品里**零可观察量**。
    //
    // **形状**：`play <卡id|卡名> preset face=ambush`
    //   · 被测卡必须是 `type=unit`（督军没有「打出去」这个动作）；
    //   · 走引擎自己的 `PlayCard` 落到 `FreeSlot`，再**过一个完整来回**
    //     （清「部署当回合不能行动」：`Exhausted` / `AttacksThisTurn`）；
    //   · 然后把它摆成 **面朝下 + 带 `ambush`** —— 两个写入口都是引擎自己的
    //     （`FaceDown` 位 / `AddKeyword`，与 `PlayCard` 那条「面朝下下场」用的是同一对，
    //     ⛔ 不手改 `Board`）；
    //   · 最后 `RuleCore.DeclareAttack` 打一发，逐条比前后局面。
    //
    // ⚠️ **这是量具干预**（与 `preset dead=` / `hand=` 同一档）：它**不**声称「原版开局这个单位就面朝下」；
    //   它只回答「面朝下 + 带 `ambush` 的单位出手时，那一记**到底打不打出去**」。
    // ⛔ 不传 `face=` 时一条都跑不到这里 ⇒ 全池指纹不受影响。
    static int RunFaceAmbush(List<CardDef> pool, CardDef card, PresetSpec ps)
    {
        if (card.Type != "unit")
        {
            Console.Error.WriteLine("!! `face=ambush` 只对 `type=unit` 有意义（现读 `" + card.Id + "` type=" + card.Type + "）");
            return 2;
        }
        int handIdx; bool injected;
        var ctx = BuildFixture(pool, card, null, out handIdx, out injected, ps);
        if (handIdx < 0)
        {
            Console.Error.WriteLine("!! 夹具没接住：`" + card.Id + "` 打出时不在手牌里");
            return 2;
        }
        int slot;
        int rc = RuleCore.PlayCard(ctx, 0, handIdx, FreeSlot(ctx, 0), out slot);
        if (rc != RuleCodes.OK)
        {
            Console.Error.WriteLine("!! 被测卡打不出去 rc=" + CodeName(rc) + " ⇒ 本档无从起步");
            return 2;
        }
        // 过一个完整来回：清「部署当回合不能行动」（`Exhausted` / `AttacksThisTurn`）——
        // 否则下一发会被疲劳/配额挡住，读数就变成「被闸挡下」而不是「伏击那一支跑没跑」。
        RuleCore.EndTurn(ctx); RuleCore.BeginTurn(ctx);   // → P1
        RuleCore.EndTurn(ctx); RuleCore.BeginTurn(ctx);   // → P0
        SetEnergy(ctx, 0);
        SetFaction(ctx, 0);

        var atk = ctx.Players[0].Board[slot];
        if (atk == null)
        {
            Console.Error.WriteLine("!! 过完一个来回，被测单位已经不在 " + slot + " 格（被别的效果移除）");
            return 2;
        }
        // 摆成「面朝下 + 带 `ambush`」—— 两个写入口都是引擎自己的（同 `PlayCard` 的「面朝下下场」那一支）。
        bool hadAmbush = atk.Has(KeywordTable.Ambush);
        atk.FaceDown = true;
        if (!hadAmbush) atk.AddKeyword(KeywordTable.Ambush, 1);

        // 目标：优先**非督军**（打死督军会牵出 `IsOver` 那一串，读数不好读）；没有就退回任意一格。
        int tgtSlot = -1;
        for (int s = 0; s < BoardSpec.Size && tgtSlot < 0; s++)
        {
            var u = ctx.Players[1].Board[s];
            if (u != null && !u.IsWarlord) tgtSlot = s;
        }
        for (int s = 0; s < BoardSpec.Size && tgtSlot < 0; s++)
            if (ctx.Players[1].Board[s] != null) tgtSlot = s;
        if (tgtSlot < 0)
        {
            Console.Error.WriteLine("!! 敌方棋盘一个单位都没有（夹具坏了，不是被测卡的问题）");
            return 2;
        }
        var tgt = ctx.Players[1].Board[tgtSlot];
        int hpBefore = tgt.Health;
        string tgtCellBefore = Cell(tgt);   // ⚠️ 出手之后它可能已经离场（空串/`.`），要在出手前取

        // 打法：先近战，近战不可用（`ErrNoMeleeAttack` 那一族）再试远程 —— 读数里如实打出来。
        bool ranged = false;
        int pre = RuleCore.CanAttackNow(ctx, 0, slot, false);
        if (pre != RuleCodes.OK && RuleCore.CanAttackNow(ctx, 0, slot, true) == RuleCodes.OK) ranged = true;

        var before = Capture(ctx);
        string atkCellBefore = Cell(atk);
        // ⚠️ 这三个状态必须**在出手之前**取下来：`DeclareAttack` 会当场把 `FaceDown` 置假、
        //    摘掉 `ambush`、并把 `AttacksThisTurn` +1 / 配额用完时置 `Exhausted` ⇒
        //    在打印那一刻再读，读到的是**出手之后**的状态（第一版就是这么错的）。
        bool faceBefore = atk.FaceDown;
        bool ambBefore = atk.Has(KeywordTable.Ambush);
        bool exhBefore = atk.Exhausted;
        int atkCntBefore = atk.AttacksThisTurn;
        int rcAtk = RuleCore.DeclareAttack(ctx, 0, slot, 1, tgtSlot, ranged);
        var after = Capture(ctx);
        var changes = new List<string>();
        Diff(before, after, changes);

        Console.WriteLine("=== play " + card.Id + " · " + card.Name + " [" + card.Type + "] ‖ 面朝下伏击出手夹具（`B9_A3`）===");
        Console.WriteLine("  被测单位    : " + card.Id + " · " + card.Name + "（落第 " + slot + " 格）");
        Console.WriteLine("  敌目标      : 第 " + tgtSlot + " 格 " + tgtCellBefore + "（出手前）");
        Console.WriteLine("  摆好的状态  : FaceDown=" + (faceBefore ? "真" : "假")
                          + " · Has(ambush)=" + (ambBefore ? "真" : "假")
                          + "（摆之前" + (hadAmbush ? "**本来就有** ambush" : "**没有** ambush，本档补上") + "）"
                          + " · Exhausted=" + (exhBefore ? "真" : "假")
                          + " · AttacksThisTurn=" + atkCntBefore);
        Console.WriteLine("  出手        : `RuleCore.DeclareAttack`（" + (ranged ? "远程" : "近战") + "）rc=" + rcAtk
                          + "（" + CodeName(rcAtk) + "）"
                          + (pre != RuleCodes.OK ? "　⚠️ 近战前置检查 rc=" + pre + "（" + CodeName(pre) + "）" : ""));
        Console.WriteLine();
        Console.WriteLine("  ── 出手之前 ──");
        Console.WriteLine("     " + Fingerprint(before));
        Console.WriteLine("     " + atkCellBefore);
        Console.WriteLine("  ── 出手之后 ──");
        Console.WriteLine("     " + Fingerprint(after));
        Console.WriteLine();
        Console.WriteLine("  ── 变化逐条（" + changes.Count + " 条）──");
        if (changes.Count == 0) Console.WriteLine("     （局面一个字段都没动）");
        foreach (var ch in changes) Console.WriteLine("     " + ch);
        Console.WriteLine();
        Console.WriteLine("  ── 关键读数（这一档要回答的就是这两行）──");
        var atkAfter = ctx.Players[0].Board[slot];
        Console.WriteLine("    · 翻开 / 撤伏击：FaceDown=" + (atkAfter == null ? "（不在场）"
                          : (atkAfter.FaceDown ? "**仍然为真**" : "已置假"))
                          + " · Has(ambush)=" + (atkAfter == null ? "（不在场）"
                          : (atkAfter.Has(KeywordTable.Ambush) ? "**仍然有**" : "已摘掉")));
        Console.WriteLine("    · 敌目标血量：" + hpBefore + " → " + (ctx.Players[1].Board[tgtSlot] == null
                          ? "（已离场）" : ctx.Players[1].Board[tgtSlot].Health.ToString())
                          + " · 对敌伤害累计（`ctx.DamageToEnemy[1]`）：" + before.Dmg[1] + " → " + after.Dmg[1]);
        Console.WriteLine();
        Console.WriteLine("  ── 引擎自己的事件日志（末尾 16 条）—— 「一记都没打」时先看这里它自己说了什么 ──");
        var tail = ctx.Events.Skip(Math.Max(0, ctx.Events.Count - 16)).ToList();
        if (tail.Count == 0) Console.WriteLine("     （一条都没有）");
        foreach (var l in tail) Console.WriteLine("     " + Clip(l, 150));
        Console.WriteLine();
        Console.WriteLine("  ── 怎么读这一档（⛔ 本工具不判对错）──");
        Console.WriteLine("    · 原版 `_ResolveAttack…:288-305` 在「面朝下的攻方出手」那一支：先 `SetAmbush(false)`（翻开 + 触发 `Ambush:`），");
        Console.WriteLine("      随后 `:478` 压一条 `AddFollowResolutionAttackContinuationAction`（`BattleActionType.attack`）⇒ **这一记要补打**；");
        Console.WriteLine("    · ⇒ 读数为「揭开 + 撤 ambush」**且**「敌目标掉血 / 对敌伤害累计 > 0」= 与判据一致；");
        Console.WriteLine("      若只有前半句、伤害仍是 0 ⇒ **续攻那一半没落地**（`B9 A1` 修的就是这一条）。");
        Console.WriteLine("    ⚠️ 伤害为 0 也可能是「这一记被别的闸挡了」（看上面的 rc 与日志），⛔ 别单看伤害一个数。");
        return 0;
    }

    /// <summary>把 `event=` 指定的那个广播**打进去**。返回 `null` = 成功；否则返回错误文案（⛔ 不静默）。
    /// `pathDesc` 回填「走的哪条路」（读数里要看得见 —— 产品路径和「直调广播」是两件事）。
    ///
    /// <para>**三条路（都是照既有产品代码的形状走，⛔ 不另写一份判据）**：
    /// ① `legacy=0` 且 `event` ∈ {gainfaith, gainquest, gainspirit}
    ///    → **真产品路径**：构造一条 `EffectOp{Verb=…}` 交 `RuleCore.ResolveOps`
    ///    ⇒ 走 `DoFactionResource`（资源真的加、信号真的发、广播由**产品自己**按当前形状发）。</para>
    /// <para>② `legacy=1` 且同上三种 → **改前形状的复现**：先按产品同一句加资源，再
    ///    `RuleCore.BroadcastWhen(…)` **不带 `boardAndPersistentOnly`** ⇒ 四跳
    ///    （= 2026-10-26 那 9 个发射点加 flag 之前的行为）。⛔ 这一段**只服务复现历史缺陷**，
    ///    产品代码里⛔ 别照抄。</para>
    /// <para>③ `createssecret` / `createssabotage` → 只能**直调广播**（产品那两处发射点埋在
    ///    `DoCreate`/`CreateHandCard` 的造牌流程里、要真造出一张牌）⇒ 本档按**与发射点同一组实参**
    ///    发一次广播（`boardAndPersistentOnly` 跟 `legacy` 走），并在读数里**明说这是直调**。</para></summary>
    static string FireHandEvent(BattleContext ctx, PresetSpec ps, out string pathDesc)
    {
        string ev = (ps.Event ?? "").Trim().ToLowerInvariant();
        var wl = ctx.Players[0].Warlord;
        var ps0 = ctx.Players[0];
        using (RuleCore.BeginAction(ctx))     // 与 `RuleCore.ApplyDamage` / `BroadcastWhen` 同形：入口自带作用域（否则监听者停在队列里）
        {
            switch (ev)
            {
                case "gainfaith":
                case "gainquest":
                case "gainspirit":
                    if (!ps.Legacy)
                    {
                        var op = new EffectOp
                        {
                            Verb = ev,
                            Amount = ps.EventAmount,
                            Source = "夹具事件（手牌态监听 · `B8_AV`）",
                        };
                        RuleCore.ResolveOps(ctx, 0, null,
                            new List<EffectOp> { op }, "夹具事件（手牌态监听 · `B8_AV`）");
                        pathDesc = "**产品路径**：`RuleCore.ResolveOps` → `DoFactionResource`（广播由产品自己发）";
                        return null;
                    }
                    // 改前形状：加资源那一句与产品同形，然后发**四跳**广播。
                    if (ev == "gainfaith") ps0.Faith += ps.EventAmount;
                    else if (ev == "gainquest") ps0.QuestPoints += ps.EventAmount;
                    else ps0.SpiritStones += ps.EventAmount;
                    RuleCore.BroadcastWhen(ctx, WhenEventKindOf(ev), 0,
                        wl != null ? wl.Card : null, wl);      // ⛔ 不带 boardAndPersistentOnly = 改前的四跳
                    pathDesc = "⚠️ **改前形状复现**：先加资源，再 `BroadcastWhen(…)` **不带** `boardAndPersistentOnly`"
                             + "（= 四跳：事件型降费 / 手牌陷阱 / **手牌单位**）";
                    return null;
                case "createssecret":
                case "createssabotage":
                    // ⚠️ 直调广播（原因见上面 ③）。subject 传 null —— 产品发射点传的是**被造出来的那张牌**，
                    //   本档没有真造牌 ⇒ 传 null，并在这里如实说明。
                    RuleCore.BroadcastWhen(ctx, WhenEventKindOf(ev), 0, null, null,
                        boardAndPersistentOnly: !ps.Legacy);
                    pathDesc = "**直调广播**（`RuleCore.BroadcastWhen`，与发射点同一组实参但 `subject=null` —— "
                             + "本档没有真造出一张牌；`boardAndPersistentOnly=" + (!ps.Legacy ? "true" : "false") + "`）";
                    return null;
                default:
                    pathDesc = "（未执行）";
                    return "`event=` 不认识 `" + ps.Event + "`（只认 gainfaith / gainquest / gainspirit "
                         + "/ createssecret / createssabotage）";
            }
        }
    }

    /// <summary>`event=` 的串 → `WhenEventKind` 常量（⛔ 不另立一套名字：两边本来就是同一串）。</summary>
    static string WhenEventKindOf(string ev)
    {
        switch (ev)
        {
            case "gainfaith": return WhenEventKind.GainFaith;
            case "gainquest": return WhenEventKind.GainQuest;
            case "gainspirit": return WhenEventKind.GainSpirit;
            case "createssecret": return WhenEventKind.CreatesSecret;
            case "createssabotage": return WhenEventKind.CreatesSabotage;
            default: return ev;
        }
    }

    static void MeasurePlay(List<CardDef> pool, CardDef card, Reading r, PresetSpec preset = null)
    {
        int handIdx;
        bool injected;
        var ctx = BuildFixture(pool, card, null, out handIdx, out injected, preset);
        r.Injected = injected;
        if (handIdx < 0)
        {
            // 🔴 **夹具没接住要出声、而且要说清手里有什么** —— ⛔ 不静默（本仓红线）。
            r.Before = "手牌 = " + List(ctx.Players[0].Hand.Select(x => x.Card.Id).ToList())
                     + " · 牌库 " + ctx.Players[0].Deck.Count + " 张 ["
                     + string.Join(",", ctx.Players[0].Deck.Select(x => x.Card.Id)) + "]"
                     + " · 弃牌 = " + List(ctx.Players[0].Discard.Select(x => x.Card.Id).ToList())
                     + "\n     P0 棋盘 " + Board((from s in Enumerable.Range(0, BoardSpec.Size)
                                                select Cell(ctx.Players[0].Board[s])).ToArray());
            r.Note = "**夹具没接住**：被测卡 `" + card.Id + "` 打出去的时候不在手牌里（" + r.Before + "）";
            return;
        }

        var before = Capture(ctx);
        var inst = ctx.Players[0].Hand[handIdx];
        string playedEntry = HandEntry(inst);   // ⚠️ 必须在打出去【之前】取（打完就离开手牌了）
        // 🔴 **照卡面应付多少**（在打出去【之前】算）—— 用来把「付费那一条能量变动」从
        //    「实得变化」里摘出去。⛔ 别用「能量变了就算付费」那种近似：
        //    `Refill 2 Energy` 类的卡**效果本身就是改能量**，按近似会被整张漏掉。
        //    ⚠️ **应付 ≠ 卡的 `cost`**：卡面上 `N Energy:` 那种**付费能力前缀**是**另付**的
        //    （`EffectOp.Cost > 0 && !CostShared` 的那几条，实测 `SOR_Righteous_Repugnance` = 卡费 2 + 前缀 4 = 6）。
        //    ⚠️ 单位卡的 `2 ☀: …` 段**不在部署这一步付**（归 `UseAbility`）⇒ 单位卡不加前缀那部分。
        int expectedPay = RuleCore.CostOf(ctx, 0, inst);
        int instCost = expectedPay;      // 这张牌**自己**的价（= 手牌总价里该减掉的那一份）
        if (!card.IsUnit)
            foreach (var op in EffectText.Parse(card.Desc, out _, out _, card: card))
                if (op.Cost > 0 && !op.CostShared) expectedPay += op.Cost;
        r.ExpectedPay = expectedPay;

        if (card.IsUnit)
        {
            int slot = FreeSlot(ctx, 0);
            // 🆕 `P3`（2026-10-26）：`pick=1` ⇒ 给这张单位卡**一个玩家点定的目标**（见 `PresetSpec.Pick`）。
            //   判据：原版 `targeted == true` 的卡**没有目标时整段 `ThisCardPlayed` 直接跳过**
            //   （`RawCardScript.OnCardPlayedWithTarget`）；而 `ta=30` 那一支只认调用方传进来的目标
            //   （`AbilityLogic__GetTargets.c:192-207`）。⛔ 不传 `pick=1` ⇒ 走原来那一句，一个字节都不变。
            UnitState chosen = null;
            if (preset != null && preset.Pick > 0)
                chosen = PickPlayTarget(ctx, 0, card);
            if (chosen != null)
            {
                r.PickTarget = chosen.Name ?? "";
                r.How = "`RuleCore.PlayCard`（落第 " + slot + " 格 · `pick=1` 点定目标 = "
                      + chosen.Name + "）";
                r.Rc = RuleCore.PlayCard(ctx, 0, handIdx, slot, chosen);
            }
            else
            {
                if (preset != null && preset.Pick > 0)
                    r.How = "`RuleCore.PlayCard`（落第 " + slot + " 格 · `pick=1` **目标池为空 ⇒ 没点**）";
                else
                    r.How = "`RuleCore.PlayCard`（落第 " + slot + " 格）";
                r.Rc = RuleCore.PlayCard(ctx, 0, handIdx, slot);
            }
        }
        else
        {
            // 🔴🆕 **2026-10-26（执行代理 `B8_AV`）：必须带 `card:`** —— 引擎侧每一条结算路径都是
            //   `EffectText.Parse(card.Desc, out _, out _, card: card)`（`EffectResolver.cs:1947`/`:2041`/
            //   `:2434`/`:5098`），而这里原来**不传** ⇒ `AssignCardTargetFallback` 那道闸根本跑不到
            //   ⇒ 探针自己算出来的 `TargetCondition` 恒 null、`PickSlot` 也看不见它
            //   （⚠️ 这是 `A1911` 同族的**第 6 处**：`Program.LoadPool` 传不传 `targeted` 只决定
            //     `card.Targeted` 读得对不对，**这一处不传 `card:` 照样瞎** —— 两处都修才连得上）。
            var ops = EffectText.Parse(card.Desc, out _, out _, card: card);
            var spec = EffectText.PickTarget(ops);
            int tslot = spec == null ? -1 : PickSlot(ctx, 0, spec);
            // 🆕 `B8_AH`（2026-10-26）：`legal=1` ⇒ 目标格改由**产品代码**挑（`RuleCore.IsLegalPlayTarget`
            //   = 拖拽高亮 / 点选问的那一处），并把**合法目标池**打进事件日志。
            //   理由：默认 `PickSlot` 只看「第一个非督军格」、**完全不看合法性** ⇒ 显式目标格会绕过
            //   `op.TargetCondition` 的结构化条件筛（实测 `sub=BL1`：效果照样落在 BL1 身上）。
            //   ⚠️ 这是**量具修正**，⛔ 不传 `legal=1` 时一个字节都不变（全池指纹不受影响）。
            if (preset != null && preset.Legal > 0)
            {
                int side = spec == null ? 0 : (spec.Side == "enemy" ? 1 : 0);
                var legal = new List<int>();
                for (int i = 0; i < BoardSpec.Size; i++)
                {
                    var u = ctx.Players[side].Board[i];
                    if (u != null && RuleCore.IsLegalPlayTarget(ctx, 0, card, u)) legal.Add(i);
                }
                ctx.Log($"[合法目标池] `RuleCore.IsLegalPlayTarget` ⇒ **{legal.Count}** 格："
                      + (legal.Count > 0 ? string.Join(" · ", legal.Select(s => "格" + s + " " + Cell(ctx.Players[side].Board[s])))
                                         : "（空）"));
                // 顺手把这条 op 上挂的结构化条件打出来（`B8_AH`：条件 275/276/277 走的是它）——
                //   `null` = 这条 op **没有条件**。
                //   ✅ **2026-10-26（`B8_AV`）就地订正**：原来这里写「本探针的 `LoadPool` **没传 `targeted`**
                //   ⇒ 卡级 `cardtarget` 条件在 `play` 这条路上永远挂不上」—— **已经不成立了**：
                //   `Program.LoadPool` 从同一天起把 `targeted` 照 `CardDatabase.cs:1499` 透传
                //   ⇒ 卡级条件现在**会**挂上来（`Targeted == 1` 的那 246 张）。
                //   ⚠️ 读数会因此变（这正是那一笔的目的）；`targeted` 仍是 `-1` 的行 = 卡池缺这一列，会出声。
                for (int k = 0; k < ops.Count; k++)
                    ctx.Log($"[目标条件] op[{k}] `{ops[k].Verb}` TargetCondition = "
                          + (ops[k].TargetCondition == null
                                ? "null"
                                : "cond=" + ops[k].TargetCondition.Condition + " ex=" + ops[k].TargetCondition.Exclude
                                  + " role=`" + ops[k].TargetCondition.Role + "`")
                          + " · gap=`" + (ops[k].TargetConditionGap ?? "") + "`");
                tslot = legal.Count > 0 ? legal[0] : -1;
            }
            // 🆕 `E9ER`（2026-10-26）：`tpick=1` —— **非单位卡**也给一个「点定的目标」（⛔ 与 `pick=` 分开：
            //   那个已被 `P3` 的单位卡占用、且 `pool_effect_check` 第二套夹具靠它 ⇒ 扩义会静默改既有读数）。
            //   治的是账 `B9` 缺口 ② 的量具盲区：`Side == "any"` 的卡 `EffectText.PickTarget` 返回 null
            //   ⇒ `spec == null` ⇒ 这一路**根本不传目标**，而引擎改前的结算恒读「我方同号格」
            //   ⇒ 夹具无论怎么摆都看不出「打到自己人」。判据同上面单位卡那一支（原版 `action+0x20` 是
            //   **CardScript 引用**，侧跟目标走）。优先取**敌方**那个（那是唯一能看出差异的一侧）。
            UnitState picked = null;
            if (preset != null && preset.TacticPick > 0)
            {
                for (int i = 0; i < BoardSpec.Size && picked == null; i++)
                {
                    var eu = ctx.Players[1].Board[i];
                    if (eu != null && !eu.IsWarlord) { picked = eu; tslot = i; }
                }
                if (picked != null) r.PickTarget = picked.Name ?? "";
            }
            r.How = "`RuleCore.PlayTactic`"
                  + (spec == null ? "（不需选目标）" : "（目标侧=`" + spec.Side + "` 格=" + tslot + "）")
                  + (picked != null ? " · `tpick=1` 点定目标 = " + picked.Name : "");
            r.Rc = picked != null ? RuleCore.PlayTactic(ctx, 0, handIdx, tslot, picked)
                                  : RuleCore.PlayTactic(ctx, 0, handIdx, tslot);
        }

        var after = Capture(ctx);
        r.PlayOrder = PlayOrderCol(ctx);
        // 🆕 2026-10-27（`B9_E5ENG`）：计数类两格（进结算指纹 —— 见 `Reading.AtkCount` 的 doc）
        r.AtkCount = after.ActCnt[0] + " ‖ " + after.ActCnt[1];
        r.Exh = after.Exh[0] + " ‖ " + after.Exh[1];
        Diff(before, after, r.Changes);
        r.LogTail = ctx.Events.Skip(Math.Max(0, ctx.Events.Count - 14)).ToList();
        r.Before = Fingerprint(before);
        r.After = Fingerprint(after);
        r.Played = r.Rc == RuleCodes.OK;
        if (!r.Played) r.Note = "打不出去：" + CodeName(r.Rc) + "（" + CodeWhy(r.Rc) + "）";

        // 分类（**这几格就是「有没有实得」**）—— 前四类是**「打出去这个动作本身」**，不算效果：
        //   · 能量那一条 —— **恰好等于照卡面应付的**（卡费 ＋ 付费能力前缀）⇒ 只是付费；
        //   · 手牌「减掉被测那一张」与弃牌堆「加上被测那一张」（单位卡不进弃牌堆）；
        //   · 「自身落场」= 我方棋盘上「原本是空格、现在出现了这张卡的 id」的格数；
        //   · 其余一律算实得。
        int eDelta = after.Energy[0] - before.Energy[0];
        int netEnergy = eDelta + expectedPay;
        r.EnergyDelta = eDelta;
        r.HandCostDelta = after.HandCost[0] - before.HandCost[0];
        // 🔴 单位卡：这张卡**刚落地时「应该是」什么样** —— 拿引擎**自己**的初值算
        //    （`new UnitState(CardInstance.Detached(card), false)`，≡ `PlayCard` 里那一句）。
        //    ⚠️ **不能只按「那一格从空变成了这张卡」就一律算「自身落场」** —— 实测 `BL4 Chaos Sergeant`
        //       （卡面 `2/1/2`、`keywords=[rally1]`）落地时是 `4/3/0/2 {darkpact1, rally1}`：
        //       **Rally 的效果就发生在同一格里**，一律当「自身落场」会把**它整个吞掉**（误报成候选）。
        string naiveCell = card.IsUnit ? Cell(new UnitState(CardInstance.Detached(card), false)) : null;
        string handLine = "✋ 手牌 P0 -「" + playedEntry + "」×1";
        string discLine = "🗑 弃牌 P0 +「" + card.Id + "」×1";
        for (int i = 0; i < r.Changes.Count; i++)
        {
            string ch = r.Changes[i];
            if (ch.StartsWith("⚙ 能量 P0"))
            {
                if (netEnergy == 0) { r.Paid++; continue; }
                // 付费之外还有净变化 ⇒ 这一条**是效果**（如实把净额写出来）
                r.Changes[i] = ch + "   ⇒ **照卡面应付 " + expectedPay + " · 除付费外净 "
                             + (netEnergy > 0 ? "+" : "") + netEnergy + "**";
                r.Other++;
                continue;
            }
            if (ch == handLine || (!card.IsUnit && ch == discLine)) { r.Mech++; continue; }
            // 手牌总价：**出牌本身**就会把它减掉「这张牌自己的价」⇒ 差额才是效果（减费/加费那一族）。
            if (ch.StartsWith("💰 手牌总价 P0"))
            {
                int net = (after.HandCost[0] - before.HandCost[0]) + instCost;
                if (net == 0) { r.Mech++; continue; }
                r.Changes[i] = ch + "   ⇒ **除出牌本身外净 " + (net > 0 ? "+" : "") + net + "**";
                r.Other++;
                continue;
            }
            // 单位卡：`PlayCard` 顺手把 `TroopsPlayed[p]` +1（那一步是**出牌本身**，不是效果）
            if (card.IsUnit && ch.StartsWith("🎖 出兵数 P0")) { r.Mech++; continue; }
            if (card.IsUnit && ch.StartsWith("✚ 落场/生成 P0["))
            {
                int arrow = ch.IndexOf(" → ", StringComparison.Ordinal);
                string cellAfter = arrow < 0 ? "" : ch.Substring(arrow + 3);
                if (cellAfter == naiveCell) { r.SelfPlace++; continue; }     // 就是「印的那些数」⇒ 纯落场
                if (cellAfter.StartsWith(card.Id + " "))
                {
                    // 同一格里就带着变化（`Rally:` 那种自指效果）⇒ **是效果**，⛔ 不许被「落场」吞掉
                    r.Changes[i] = ch + "   ⇒ **落地时它自己身上与印的数不同**（印的：`" + naiveCell + "`）";
                    r.Other++;
                    continue;
                }
                r.Other++;      // 生成的是**别的**卡 ⇒ 效果
                continue;
            }
            if (card.IsUnit && ch.Contains("✚ 落场") && ch.Contains("P0[") && ch.Contains(card.Id + " ")) r.SelfPlace++;
            else r.Other++;
        }
        r.Dmg = after.Dmg[0] - before.Dmg[0];
    }

    /// <summary>督军档 —— ⚠️ **督军没有「打出去」这个动作**（`PlayCard`/`PlayTactic` 都只认手牌里的非督军卡）
    /// ⇒ 改测「**这位督军在场上、过完一个来回**局面的实得变化」，并跑一局**空督军对照**
    /// （同种子、同摆场，只把 P0 督军换成一个 `FIXWarlord`）把「与督军无关的变化」减掉。
    /// ⛔ 这不是「我们挑的做法」—— 是因为督军**确实**只能这样测；如实打在读数里。</summary>
    static void MeasureHero(List<CardDef> pool, CardDef card, Reading r, PresetSpec preset = null)
    {
        r.HeroMode = true;
        r.How = "督军**没有「打出去」这个动作**（`PlayCard`/`PlayTactic` 只处理非 hero）"
              + " ⇒ 本档测的是「**在场 + 一个来回**」的实得变化，并减掉一局【空督军对照】";

        var a = HeroPass(pool, card, preset);
        var b = HeroPass(pool, null, preset);
        r.ControlChanges = b.Changes;
        // 🔴 **净变化 = A − B**（两份逐条字符串相减）—— 两局只差 P0 督军那一张，
        //    所以这一栏才是**这位督军带来的**。⛔ 不拿 A 直接当读数（那里面大半是与督军无关的回合噪声）。
        r.Changes = a.Changes.Where(x => !b.Changes.Contains(x)).ToList();
        // 🔴 **静态那一半要单独算** —— 上面那份 diff 是「同一局内 before→after」，
        //    而**督军自己那一格**从头到尾都在场、数值没变过 ⇒ 它**永远不出现在 diff 里**。
        //    而「这位督军的攻/远/甲/血/关键词」恰恰是它最要紧的读数（例：`ASH2` 是 `2/2/0/40`，
        //    空白对照是 `2/0/0/30`）⇒ 直接比两趟**终局的督军格**。
        string wa = a.End.Cells[0][BoardSpec.WarlordSlot], wb = b.End.Cells[0][BoardSpec.WarlordSlot];
        if (wa != wb)
            r.Changes.Insert(0, "★ 督军格（本卡在场）" + wa + "  ‖ 对照局（空督军）" + wb);
        r.Before = "";
        r.After = Fingerprint(a.End);
        // 🆕 2026-10-27（`B9_E5ENG`）：督军档同样收计数两格（否则这一档仍是盲区）
        r.AtkCount = a.End.ActCnt[0] + " ‖ " + a.End.ActCnt[1];
        r.Exh = a.End.Exh[0] + " ‖ " + a.End.Exh[1];
        r.Played = true;
        r.Other = r.Changes.Count;      // 督军档不分类（付费/落场那套不适用），全部算「净变化」
    }

    /// <summary>跑一局：督军 = `card`（null ⇒ 空督军对照），过完 <c>P0 结束 → P1 开始+结束 → P0 开始</c>
    /// 一个来回，返回（变化逐条 · 终局快照）。</summary>
    static (List<string> Changes, Snap End) HeroPass(List<CardDef> pool, CardDef card, PresetSpec preset = null)
    {
        int handIdx;
        bool injected;      // 督军档不用它（这条路上根本没有「补手牌」这一步）
        var ctx = BuildFixture(pool, null, card ?? HeroOf("FIXWarlord_P0", "Fixture"), out handIdx, out injected, preset);
        var before = Capture(ctx);
        RuleCore.EndTurn(ctx); RuleCore.BeginTurn(ctx);      // → P1
        SetEnergy(ctx, 1);
        SetFaction(ctx, 1);      // 🆕 `A1943`（第 4 处 —— 督军档 `HeroPass` 那条路；
                                 //    ⚠️ 只补 `BuildFixture` 里那三处会漏掉它）
        RuleCore.EndTurn(ctx); RuleCore.BeginTurn(ctx);      // → P0
        var after = Capture(ctx);
        var ch = new List<string>();
        Diff(before, after, ch);
        return (ch, after);
    }

    // ==================================================================
    //  🆕 `B9_W41S3B`（2026-10-27）：**督军档的电池宿主**（`preset hero=1`）
    // ==================================================================
    //
    // 🔴 **它治的是哪一条**（分族表 §一 的 **A1 族 = 144 行 / 56 张**，本表最大的夹具形状缺口）：
    //   现有 `MeasureHero` 只跑「**在场 + 一个来回**」的对照局 ⇒ 督军那 144 行里
    //   「**主动能力**（trigger=None，61 行）」与「**Talent**（trigger=46，56 行）」两档
    //   **从来没有被主动用过**：
    //     · `activeAbility` 要有人调 `RuleCore.UseAbility`（现有夹具一次都没调过）；
    //     · `Talent` 是**回合开始时造一张临时卡进手**（`RuleCore.SpawnTalents` ⇒
    //       `AddCardToHandWithinLimit`），而夹具**从不把那张卡打出去**
    //       ⇒ 那张卡的效果一条都没结算过（实测 `play BL34`：「变化逐条」里只有手牌总价那一行噪声，
    //       而 `Chosen of the Four` **确实就在手上**）。
    //
    // **步骤**（都在引擎自己的公开面上，⛔ 不手改任何状态）：
    //   ① 打出手里那张**天赋卡**（`PlayCard`；失败时用 `<see cref="PickPlayTarget"/>` 补一个目标重试一次）
    //   ② 走**同一套电池步**（`RunBatterySteps` —— 与单位档共用的那一份）
    //   ③ 用**主动能力**（`UseAbility`）/ 替代行动（`AvailableAlternative`+`UseAlternative`）
    //   ⚠️ ③ 排在最后是**故意的**：`UseAbility` 会把督军置 `Exhausted`，
    //      而电池⑤那两发攻击里**只有第二发带 `forced:true`**（第一发会吃 `ErrExhausted`）
    //      ⇒ 先放技能会把「另一种攻击型」那一发挤掉（`Mob` / `Regiment` 就只剩一半）。
    //
    // **对照局** = **同一个夹具 + 同一张卡的【空正文替身】**（`Blank(hero)`，与单位档同一条契约：
    //   同 id / 同印数 / 同兵种 / 同关键词标记，只清正文），**而且不打天赋**（`useTalent: false`）。
    //   🔴 为什么不能用 `FIXWarlord_P0`（`HeroPass` 用的那个）：它是**另一张卡**（2/0/0/30、无兵种），
    //      与督军的攻/血/兵种全不同 ⇒ 两局的战斗算术天然不同 ⇒ 净差全是噪声。
    //   🔴 为什么对照局**不打天赋**：`SpawnTalents` 的真值表那一支**按卡 id 查**（`TalentCardData.Resolve`），
    //      空正文替身**同 id** ⇒ 它照样会在回合开始造出同一张天赋卡。若两局都把它打出去，
    //      那张卡的效果**逐条相消** ⇒ 56 行 Talent 仍然净差 0（本批实测：改前那一档就是这么哑的）。
    //      ⇒ 对照局里那张卡**留在手上**，A 局把它打出去 ⇒ 净差 = 「打这张天赋卡到底做了什么」。
    // ⚠️ 这是量具干预（与 `dead=` / `big=` 同一档）；只在 `preset hero=1` 时跑，
    //   `play check` 的两套指纹行**不经过这里**（`BothFingerprintRows` 不调它）。
    /// <summary>督军电池的**一趟**（被测卡当 P0 督军）。`useTalent: false` ⇒ 不打手里那张天赋卡
    /// （对照局用；见上面那两段判据）。`hero == null` ⇒ 空督军（只是兜底，正常宿主走 `Blank`）。</summary>
    static EventPass RunHeroBattery(List<CardDef> pool, CardDef hero, PresetSpec preset = null,
                                    bool useTalent = true)
    {
        var ep = new EventPass();
        var wl = hero ?? HeroOf("FIXWarlord_P0", "Fixture");
        int handIdx;
        bool injected;
        // 🆕 阵营显式给（`factionOverride`）：督军档 `target == null` ⇒ 否则双方阵营退成 "P0"/"P1"
        //   （池里不存在）⇒「按施放者阵营造牌」那一族一张都筛不到（假读数）。
        //   对照局**用同一个阵营**（它是夹具的起始条件，不是被测卡的一部分）。
        var ctx = BuildFixture(pool, null, wl, out handIdx, out injected, preset,
                               factionOverride: string.IsNullOrEmpty(wl.Faction) ? null : wl.Faction);
        const int slot = BoardSpec.WarlordSlot;
        ep.Played = true;          // 督军在场 = 起步成功（与 `MeasureHero` 同口径：没有「打出」这一步）
        ep.PlayRc = RuleCodes.OK;
        ep.Steps.Add("hero=" + wl.Id);

        var snaps = new List<Snap>();
        snaps.Add(Capture(ctx));   // ⓪ 之前

        // ---- ① 打出手里那张天赋卡（`Talent:` ⇒ `RuleCore.SpawnTalents` 造的临时卡）----
        if (hero != null && useTalent)
        {
            var cands = TalentCardData.Resolve(pool, hero.Id);      // 真值表（可能为 null）
            int ti = -1;
            var hand = ctx.Players[0].Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                var h = hand[i];
                if (h == null || h.Card == null) continue;
                bool match = cands != null && cands.Any(t => t != null && t.Id == h.Card.Id);
                // 兜底：回合开始造的那张临时卡（`SpawnTalents` 一律 `EphemeralMarked = true`）
                if (!match) match = h.EphemeralMarked;
                if (match) { ti = i; break; }
            }
            if (ti < 0) ep.Steps.Add("talent=none");
            else
            {
                var tname = ctx.Players[0].Hand[ti].Card.Id;
                int tslot;
                int rc = RuleCore.PlayCard(ctx, 0, ti, FreeSlot(ctx, 0), out tslot);
                // ⚠️ 带目标的战术卡在没有 `chosen` 时会 `ErrNoTargetAvailable`
                //    （原版 `targeted` 那一支：没有目标整段跳过）⇒ **补一个目标重试一次**。
                //    重试前手牌没被动过（失败的 `PlayCard` 不扣牌/不改局面）⇒ 下标仍然有效。
                if (rc != RuleCodes.OK)
                {
                    var chosen = PickPlayTarget(ctx, 0, ctx.Players[0].Hand[ti].Card);
                    if (chosen != null)
                    {
                        int rc2 = RuleCore.PlayCard(ctx, 0, ti, FreeSlot(ctx, 0), out tslot, chosen);
                        ep.Steps.Add("talent=" + rc + "→" + rc2 + "@" + tname + "/tgt" + SlotOf(ctx, 0, chosen));
                        rc = rc2;
                    }
                    else ep.Steps.Add("talent=" + rc + "@" + tname);
                }
                else ep.Steps.Add("talent=" + rc + "@" + tname + "/slot" + tslot);
            }
        }
        else ep.Steps.Add("talent=-");
        snaps.Add(Capture(ctx));   // ① 之后

        // ---- ② 同一套电池步（与单位档**共用** `RunBatterySteps`）----
        RunBatterySteps(ctx, slot, preset, ep, snaps);

        // ---- ③ 主动能力 / 替代行动（**最后**用；理由见上面整段注释）----
        int tgt = PickAnyEnemy(ctx, 0);
        if (ctx.Players[0].Board[slot] != null)
        {
            ep.Steps.Add("abil=" + RuleCore.UseAbility(ctx, 0, slot, tgt));
            var alt = RuleCore.AvailableAlternative(ctx, 0, slot);
            if (alt != null)
                ep.Steps.Add("alt(" + alt + ")=" + RuleCore.UseAlternative(ctx, 0, slot, alt, tgt));
        }
        snaps.Add(Capture(ctx));   // ③ 之后

        for (int i = 1; i < snaps.Count; i++) Diff(snaps[i - 1], snaps[i], ep.Changes);
        return ep;
    }

    /// <summary>督军电池的**读数**：净差 = 电池局 − 空督军对照局（形状与 `MeasureEvents` 一致，
    /// 但**不跑三个变体** —— 变体是「打出那一拍」的三族，督军没有「打出」；如实标着）。</summary>
    static EventReading MeasureHeroBattery(List<CardDef> pool, CardDef card, PresetSpec preset = null)
    {
        var r = new EventReading { C = card };
        try
        {
            r.Bucket = Bucket(card);
            var a = RunHeroBattery(pool, card, preset, useTalent: true);
            r.Steps = string.Join(",", a.Steps);
            r.PlayRc = a.PlayRc;
            r.Played = a.Played;
            r.Raw = a.Changes.Count;
            if (a.End != null) { r.AtkCount = a.End.ActCnt[0] + " ‖ " + a.End.ActCnt[1]; r.Exh = a.End.Exh[0] + " ‖ " + a.End.Exh[1]; }
            // 对照局 = 同卡的**空正文替身**（同印数/同关键词，只清正文 ⇒ 主动能力与天赋正文都没了），
            //   且**不打天赋**（判据见 `RunHeroBattery` 头注那两段）⇒ 净差 = 「主动能力 + 打天赋」这两件事。
            var b = RunHeroBattery(pool, Blank(card, preset == null ? null : preset.StripKw), preset,
                                   useTalent: false);
            r.CtrlPlayed = b.Played;
            r.Ctrl = b.Changes.Count;
            if (!b.Played)
            {
                r.Note = "**对照局没起步**（空督军 rc=" + CodeName(b.PlayRc) + "）⇒ 本行净差不可信";
                r.Net = a.Changes;
            }
            else
            {
                r.Net = a.Changes.Where(x => !b.Changes.Contains(x)).ToList();
                if (a.End != null && b.End != null) r.NetDmg = a.End.Dmg[0] - b.End.Dmg[0];
            }
        }
        catch (Exception ex)
        {
            r.Crash = ex.GetType().Name + ": " + ex.Message;
        }
        return r;
    }

    // ==================================================================
    //  🆕 `A1431` 第三笔：**事件电池** —— 治「档 B」那一档的【结构性失明】
    // ==================================================================
    //
    // 🔴 **它修的是哪一条**：`play all` / `play check` 的主档量的是「**把卡打出去那一拍**」
    //   局面的数字动没动。而 **档 B**（`Bucket` 的第 3 档）的定义就是
    //   「卡面正文**每一句都挂在别的时机的头上**」（`Strike:` / `Mob:` / `Regiment:` / `Slay:` /
    //   `Duty:` / `Pray:` / `Agenda:` / `Oath N:` / `Teleport:` / `Ambush:` / `Ecstasy N:` /
    //   `At the start of your turn …` …）⇒ **本夹具产生不了那个事件**，`other == 0` 是**预期**的。
    //   ⚠️ **于是那一档的指纹行是一个【常量】**（`other=0` · `changesFull` 空）：
    //   它的结算**今天坏掉、明天修回来**，主档的指纹**一个字符都不会变** ⇒
    //   `play check` 在那一档上是**看着绿、实际没验**（本仓红线「不许静默失败」）。
    //
    // **做法**：把卡打出去**之后**，再**替它走一段事件电池** —— ⛔ 不是我们手改状态，
    //   每一发都走**引擎自己的公开面**：
    //     ① 誓约（`UseOathAbility`）⇒ `Oath N:`。
    //        🔴 **必须排在回合电池之前、同一回合里用** —— `CanUseOathAbility` 有一条
    //        `!OathAllTurns && u.DeployedTurn != ctx.Turn ⇒ ErrExhausted` ⇒ 过了回合就再也用不了
    //        （实测：排在后面时电池自检的 `Oath` 那一对两条净差**都是 0**）；它**不查 `Exhausted`**。
    //     ② **两个来回**（`EndTurn` + `BeginTurn` ×2，P0→P1→P0）⇒ 驱动
    //        `At the start of your turn` / `At the end of your turn` 那一族；
    //        ⚠️ 顺带让 P0 的部队被**引擎自己的** `RefreshForNewTurn` 刷活
    //        （`Core/UnitState.cs:1190`，由 `RuleCore.cs:1160` 在 `BeginTurn` 里调）
    //        ⇒ 后面那两步才跑得动。⛔ 探针**不手改 `Exhausted` / `SummonSickness`**。
    //     ③ 抽一张（`Draw`）⇒ `When you draw a card, …`；
    //     ④ 主动用：`UseAbility`（自定的 `Ability:`）＋ `AvailableAlternative`+`UseAlternative`
    //        ⇒ `Duty:` / `Pray:` / `Ferocity:` / `Agenda:` / `Talent:` / `Artifice:`；
    //     ⑤ **攻击两发**（`DeclareAttack` ×2：一发引擎自己选的型 ＋ 一发**另一种**）⇒
    //        `Strike:` / `Mob:` / `Regiment:` / `Ferocity:` / `Cruelty:`。
    //        🔴 **为什么非两发**：`Mob` 只在**近战**后触发、`Regiment` 只在**远程**后触发
    //        （`RuleCore.cs:4312` / `:4334` 那两条 `if (!ranged…)` / `if (ranged…)`）⇒
    //        只打一发 ⇒ **挂另一个头的整族永远抬不起来**（实测：自检的 `Regiment` 那一对全红）。
    //        第二发走 `forced: true` = 引擎自己的「**不是玩家点出来的**那一档」
    //        （原版 `scriptedAttack(0x34)`，见 `CanAttackNow` 头注：它跳过回合/配额那一段）；
    //        ⛔ 探针**不改 `AttacksThisTurn`**。
    //     ⑥ 打死一个敌人 ⇒ `Slay:` / `When … dies` / 死亡触发那一族。
    //        🔴 **`A1965` 续工（2026-10-19）**：这一句原来是裸 `ApplyDamage(999)`，而它**不处理死亡**
    //        ⇒ 棋盘上留下负血活单位、**一条死亡触发都没响过**。现在改成**引擎攻击路**
    //        （先把目标削到 1 血 → 被测单位/己方部队 `DeclareAttack(forced:true)` 真打死，
    //         `steps` 里打 `kill=<rc>@<格>/<live|rem|gone>`；实在没人能打才退回旧口径并点名）。
    //
    //   🆕 🔴 **`A1965`（2026-10-19 第五十五会话 · 写手 `E8`）：上面那六步之外再加 6 步 ＋ 3 个夹具变体。**
    //      步骤全表见 `RunEventPass` 与 `A1965_电池步设计.md` §四；一句话：
    //      `selfChip`（被测单位挨一下）· `chip2`（P1 两格削到 1 血 ⇒ 两发攻击变致命）·
    //      `tactic`（P0 打一张带目标的战术）· `enemyTactic`（P1 回合里打一张战术）·
    //      `troop2`（P0 再部署一个带 `Waystone` 的部队）· `friendDies`（**让 P1 用攻击路**打死它、
    //      原地留残骸）· `collect`（收那颗路标石）。
    //      变体 = V1 能量=卡费（`Codex`）· V2 牌库抽到再打出（`Teleport`）· V3 空棋盘（条件 `Rally`）；
    //      **四遍的净差取并集**（同一个 `net` 列）。⇒ 原来「抬不到」那九族里的大多数现在抬得起来了。
    //      ⚠️ **三个变体把【打出那一拍】也纳入 diff**：`Codex`/`Teleport`/条件 `Rally` 全部发生在
    //        `PlayCard` 里，只从打出之后快照的话它们在电池里**永远看不见**（实测踩过）。
    //      ⚠️ **`selfChip` 封顶到 `Health-1`**（设计原文是硬打 2 点）：硬打会当场打死 1–2 血的被测卡，
    //        后面所有步骤都没有被测单位 ⇒ **覆盖面倒退**。1 血单位记 `selfChip=skip`。
    //      ⚠️ **`friendDies` 必须走攻击路**：`RuleCore.ApplyDamage` **不处理死亡**（它只扣血/发事件），
    //        999 点之后棋盘上留下的是 **`-993` 血的活单位** ⇒ 不会翻成残骸、`collect` 必报
    //        `ErrNotWaystone(17)`（本笔实测）。
    //   ⚠️ ④⑤ 互相**不抢**：一个单位至多只有一条主动能力（`AvailableAlternative` 的注释：
    //      全池没有任何一张同时带两个）⇒ 没有主动能力的卡在 ④ 一步都不消耗；
    //      有主动能力的卡在 ④ 用掉行动、⑤ 第一发会返回 `ErrExhausted`，第二发（`forced`）照打。
    //      **每一个返回码都照原样进 `steps` 列**（⛔ 不静默吞掉）。
    //   ⛔ **电池抬不到的**（今天**如实标着**，见 ⑥ 节逐条列出的那批）：**付费段**
    //      （`N ☀:` / `Ecstasy N:` 那一族要**主动付费**才进得来）、
    //      `Ambush:`（要在**面朝下**打出那一拍触发）、被动光环（`Your other … troops have +N`，
    //      它改的是**别人**、且**不在打出去那一拍**）。那几族**仍未被电池验到** ——
    //      `play check` 的 ①-c 会**逐条把它们的 id 列出来**，⛔ 不许只印一句「已知不适用」。
    //      ✅ **`A1965` 之后已从这一串里摘掉**：`Teleport`（V2 抽到再打出）、`Stimulation`（`tactic` 指到被测单位）、
    //      `Codex`（V1）、条件 `Rally`（V3）、`Uprising`/部署听众（`troop2`）、`Slay`（`chip2` ⇒ 攻击击杀）、
    //      友军阵亡（`friendDies`）、`OnCollect*`（`collect` —— ⚠️ 但引擎侧的广播缺口未修，见自检那一对的注释）。
    //
    // 🔴 **对照局（⛔ 不是可选项）**：电池抬起来的**事件本身**（两边抽牌、能量刷新、回合计数…）
    //   对**每一张卡**都会产生一串**与卡无关**的变化。不加对照，`net` 就是一锅噪声，
    //   「这一张有没有被验到」这个问题**没有可测量的答案**。
    //   ⇒ 对照 = **同一张卡、同 id、同印数、同兵种、同关键词标记，只把 `desc` 清空**的替身
    //     （见 <see cref="Blank"/>）：两局的**印数/关键词**逐位相同 ⇒ 战斗算术那部分噪声抵消，
    //     剩下的净差**只能来自卡面正文**。净差 = `A.Changes` 减 `B.Changes`（与 `MeasureHero`
    //     的「空督军对照」同一个形状）。
    //
    // ⚠️ **本档只覆盖 `type == "unit"`**（586 张）—— **档 B / 档 C 按定义就是 `unit`**
    //   （`Bucket`：`tactic`/`defence` 直接归 A 档；`hero` 归 H 档，另法测）。
    //   ⛔ 不是「省事」：`tactic`/`defence` 的正文**打出即结算**、主档已经看得到，
    //     而它们的「空正文替身」在 `PlayTactic` 那一道 `IsFullyParsed` 闸上**打不出去**
    //     ⇒ 对照局不成立（那会让净差把**整张卡**算成效果）。
    //
    // ⛔ **它【不】判对错**（判据仍在卡面上）· ⛔ **不改主档一个字** —— 主档的读数、
    //   主档的指纹行、`Sweep` 的候选分档**全部逐字不动**，本档只**在末尾追加**一批行。
    //   这是有意的：主档那 407 张候选**已经逐张对过卡面**，重算它们等于让那批判决作废。

    /// <summary>🔴 **`A1431` 第三笔：电池抬不起来的那些，按【根因】分一格**（⛔ 不是「总之没验到」）。
    ///
    /// 判据**全部转调引擎自己的公开访问器**（`CardDef.SpiritOps` / `FaithOps`），⛔ 不在探针里另写表：
    ///   · **灵魂石能力**（`N [Spirit Stone]: …`）—— 收在 `CardDef.SpiritOps`、代价在 `EffectOp.Cost`；
    ///     结算点 = **部署那一拍**（`RuleCore.PlayCard` → `EffectResolver.ResolveSpiritAbility`）。
    ///   · **信仰能力**（`N ☀: …`）—— 同形（`CardDef.FaithOps`），阈值那一拍发。
    ///   · **其余** —— 电池**抬不到那个事件**（`steps` 列里哪一步不是 `rc=0`，或那个事件本夹具没有）。
    ///
    /// 🔴 **`A1943`（2026-10-14）把前两格的根因治掉了 —— 这里记的是【治之前】的事，别照旧话再治一轮**：
    ///   · **治之前**：夹具**只给能量**（`SetEnergy`），`Faith` / `SpiritStones` 恒为 `int` 默认 **0**
    ///     ⇒ 那两族能力**前缀付不起** ⇒ **主档那一拍就已经结算不了**（实测：临时给 P0 灌 10 颗，
    ///     同一批卡里有 22 张当场变成有净差）⇒ 它们全落进这两格。
    ///   · **现在**：`BuildFixture` 与 `HeroPass` 里那 **4 处** `SetEnergy` 各配了一次 `SetFaction`
    ///     （Faith **12** / Spirit **5** / Quest **0**，取值依据见 `FixtureFaith` 那一段）⇒
    ///     🔴 **全池实测：原来那 21 张（15 灵魂石 + 6 信仰）全部转成 `net > 0`**
    ///     （`play check` 的 ①-c 净差 = 0 由 **175 降到 154**，**这两格一张都不剩**）。
    ///   ⇒ ⚠️ **这两格现在的含义只剩「这张卡有 Sp/Faith 能力」**（判据就是那两个字面量非空），
    ///     ⛔ **它【不再】蕴含「夹具付不起」** —— 真要判付不起，得拿 `EffectOp.Cost`（灵魂石）
    ///     或阈值（信仰）与夹具给的那两个常数**逐个比**，本函数**不替你做那一步**（那是另一轮的口径）。
    ///   ⇒ 留着这两格是为了**卡池或夹具再变时**仍能指名，⛔ 不是「它们还有货」。
    ///
    /// 🔴 **`A1999`（2026-10-19 落地）：第三格已改成【按触发头名分格】** —— 原来它把**所有**
    ///   「电池抬不到那个事件」的卡压成一格（`A1943` 之后那两格基本清空 ⇒ 这一格就变成唯一一格、
    ///   ①-c 那张表**彻底失去鉴别力**，见 `资料/普查产出_第三十会话/A1965_电池步设计.md` §七·1）。
    ///   现在按**引擎公开面**分：
    ///     · `TriggerTexts` 的键（`rally` / `strike` / `artifice` …，来自 `CardDef` 的触发登记）；
    ///     · `WhenTriggers[].Ev.Kind`（`deploy` / `die` / `damaged` …，事件层，前缀写成 `When:` 以区分）。
    ///   ⇒ 一格 = 一组头名（`触发头：rally+strike`）。**读法**：这一组头 = 那张卡**声明了**哪些时机，
    ///   而电池那一趟**这些时机一个都没抬起净差** —— ⛔ 它仍**不是**「卡有问题」的判据，
    ///   是「该往哪个时机补电池步」的**定位**。头名一个都没有的卡（正文不是 `Head: body` 形态）
    ///   单独一格，`.Count` 就是那张卡。
    ///   ⚠️ **本改动只动控制台分组的文案/粒度**，⛔ 不动任何指纹行（`E|` 那些行不经过这里）⇒
    ///   **不需要重钉 `PinnedPlayMd5`**（现核 `EcCause` 的三个调用点：`:1932` / `:2185` / `:2959`，
    ///   全在 `Console.WriteLine` 的参数里）。
    /// </summary>
    static string EcCause(CardDef c)
    {
        if (c == null) return "?";
        // ⚠️ 下面两个串**只报告「有没有这类能力」**，⛔ 不声称根因 —— 见函数头 `A1943` 那一段。
        if (c.SpiritOps != null && c.SpiritOps.Count > 0) return "有灵魂石能力（判据 = `SpiritOps` 非空）";
        if (c.FaithOps != null && c.FaithOps.Count > 0) return "有信仰能力（判据 = `FaithOps` 非空）";
        // 🔴 `A1999`：按**触发头名**分格（判据全部取自引擎公开面，⛔ 不在探针里另写一张表）。
        var heads = new List<string>();
        if (c.TriggerTexts != null)
            foreach (var kv in c.TriggerTexts)
                if (!string.IsNullOrEmpty(kv.Key)) heads.Add(kv.Key);
        if (c.WhenTriggers != null)
            foreach (var w in c.WhenTriggers)
                if (w != null && w.Ev != null && !string.IsNullOrEmpty(w.Ev.Kind))
                    heads.Add("When:" + w.Ev.Kind);
        if (heads.Count == 0) return "无触发头（正文没有 `Head: body` 段 / 只有部署拍）";
        heads.Sort(StringComparer.Ordinal);
        var uniq = new List<string>(heads.Count);
        foreach (var h in heads)
            if (uniq.Count == 0 || uniq[uniq.Count - 1] != h) uniq.Add(h);
        return "触发头：" + string.Join("+", uniq);
    }

    /// <summary>`A1431` 第三笔：事件电池走完一趟的产物。</summary>
    class EventPass
    {
        public readonly List<string> Changes = new List<string>();
        public readonly List<string> Steps = new List<string>();
        public int PlayRc = -1;
        public bool Played;
        public Snap End;
    }

    /// <summary>`A1431` 第三笔：**一张卡的事件电池读数**。
    /// 🔴 `Net` 是**净差**（电池局 − 空正文对照局）—— 它才是「这张卡的正文在别的时机上跑没跑」。</summary>
    class EventReading
    {
        public CardDef C;
        public string Bucket = "";
        public string Steps = "";
        public int PlayRc = -1;
        public bool Played;
        public bool CtrlPlayed;          // 对照局起步了吗（没起步 ⇒ 净差不可信，如实标出来）
        public int Raw;                  // 电池局自己的变化条数（含与卡无关的事件噪声）
        public int Ctrl;                 // 对照局的变化条数
        /// <summary>🆕 这一张**在主档里是不是「候选」**（卡面有正文 · 打出去了 · 实得变化 0 条）。
        /// 🔴 **必须由 `Sweep` 从主档那一份 `Reading` 传进来** —— 覆盖面那一节要的是
        ///   「**被主档看着、却看不见**的那一批」到底被电池救回来多少，
        ///   ⛔ 不是「整个 `unit` 档」的条数（那里面一半是**卡面根本没正文**的卡，
        ///   它们 `other=0` 与「失明」无关 —— 混进去会把覆盖面**灌水**）。</summary>
        public bool Cand;
        public List<string> Net = new List<string>();
        public int NetDmg;               // 净伤害（两边 `DamageToEnemy[0]` 之差）
        public string Crash;
        public string Note = "";
        /// <summary>🆕 **2026-10-27（`B9_E5ENG`）**：电池局**终局**棋盘的 `AttacksThisTurn` / `Exhausted`
        /// （`P0… ‖ P1…`，形状见 `UnitNumCol`）。🔴 **进** `FingerprintERow`（= 进电池指纹）。
        /// ⚠️ 它**不是**「电池净差」的一部分（那一条走 `Net`）—— 它只回答「终局那两个位是什么」，
        /// 用来让「行动计数 / 疲劳位」这一族在电池档也照得到（原来两档都全盲）。</summary>
        public string AtkCount = "", Exh = "";
    }

    /// <summary>空正文替身 —— **同 id / 同卡名 / 同类型 / 同印数 / 同兵种 / 同关键词标记，
    /// 只把 `desc`（与 `descZh`）清空**。
    ///
    /// 🔴 **为什么是「只清正文」而不是「造一个白单位」**（实测踩过）：电池里有**攻击**那一发，
    ///   而攻防算术吃的是**印的数**（攻/远/甲/血/兵种）与**关键词标记**（`Armour 1` / `Flank` …）
    ///   ⇒ 拿一个 `Fix(…, 0, 1, 0)` 当对照，两局的战斗结果**天然不同** ⇒ 整锅净差全是噪声。
    ///   只清正文 ⇒ 那些部分**逐位相同**，净差**只剩正文**那一份。
    /// ⚠️ 关键词**标记**照留 —— 两族理由，都实测过：
    ///   · 它的正文在 `desc` 里（`KeywordTable.EffectText` 对 `"Rally"` 这种**没有** `':'` 的
    ///     一律返回 null ⇒ 关键词只登记**标记**，正文全靠 `desc`）⇒ 清 `desc` 就清掉了正文；
    ///   · **`Fast` / `Flank` 会改「部署当回合能不能动」那一档**（`HasDeployExemption`）——
    ///     对照局丢了它就会在与被测卡**结构不同**的状态下跑电池（那一步的返回码都不同），
    ///     净差立刻变成噪声。
    /// ⚠️ 还原写法时用 `NumericKeywords` 判「原来印没印数字」—— 规范化字典只存值，
    ///   分不出 `Armour 1`（印了）与 `Armour`（没印），而那一分影响卡面角标那一层。</summary>
    static CardDef Blank(CardDef c, string stripKw = null)
    {
        // 🆕 `B9_W41S3B`（2026-10-27）：`stripKw`（逗号分隔）—— **只从对照局替身上剥掉**这些关键词。
        //   判据 / 为什么必须有它 → `PresetSpec.StripKw` 的整段注释（`Unstable` 那类
        //   「效果在关键词的硬编码分支里、不在 desc 里」的族，否则两局一起自爆、净差恒 0）。
        //   ⛔ `stripKw == null`（缺省）⇒ 与改前**逐字相同**。
        HashSet<string> strip = null;
        if (!string.IsNullOrEmpty(stripKw))
        {
            strip = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in stripKw.Split(',')) if (t.Trim().Length > 0) strip.Add(t.Trim());
        }
        var kw = new List<string>();
        if (c.Keywords != null)
            foreach (var kv in c.Keywords)
            {
                if (strip != null && strip.Contains(kv.Key)) continue;
                bool printed = c.NumericKeywords != null && c.NumericKeywords.Contains(kv.Key);
                kw.Add(printed ? kv.Key + " " + kv.Value : kv.Key);
            }
        return new CardDef(c.Id, c.Name, c.Type, "", c.Rarity, c.Faction,
                           c.Cost, c.Attack, c.Health, c.RangedAttack, kw,
                           subtype: c.Subtype,
                           // 🔴 🆕 **2026-10-14（账 `A1942`）：卡表后加的那三列必须【原样传下去】。**
                           //   本函数造的替身与源卡**必须在除 `desc`/`descZh` 之外**逐格相同（见上面的
                           //   `<summary>`：`同 id / 同卡名 / 同类型 / 同印数 / 同兵种 / 同关键词标记`）——
                           //   而 `CardDef` 这三个可选参数（`Core/CardDef.cs:215-216`）**默认全是 `-1`**，
                           //   原来只传到 `subtype` 为止 ⇒ 替身恒 `-1`（=「真值里查不到这张卡」）。
                           //   ⇒ 形状与 `A1911`（`Program.LoadPool` 同一个坑位）**一模一样**：
                           //     探针现造的 `CardDef` **静默丢列**，而 `-1` 长得和「没数据」一样。
                           //   实测（`play all pf` · 2026-10-14）：改前替身 `inv/show/init` **全 `-1` ×
                           //     1126**，源卡分布 `init 20×955 · -1×171` / `inv 10×98 · 5×31 · 0×826 · -1×171` /
                           //     `show 5×862 · 0×93 · -1×171` ⇒ **955 张**三列全被抹掉（余 171 张源卡本就是 `-1`）。
                           //   ⛔ **不许**借 `Program.LoadPool` 里那个 `I()` 缺列返回 `0` 的口径：这三列的
                           //     `0` 都是**合法真值**（`InInventory` / `Hide` / `Initiative.Undefined`）。
                           //   消费点（引擎侧）= `DeckRules.CannotAddToDeck`（能不能进卡组）·
                           //     `FirstSeatRules.ByInitiative`（谁先手）—— `play` 这条路今天**一个都不走**
                           //     ⇒ 本次**零读数影响**（改前/改后 `play all pf` 指纹逐字节相同，见下）；
                           //     补它是为了**将来拿探针量这两件事**时口径是对的。
                           inventoryOptions: c.InventoryOptions,
                           showInCollectionOptions: c.ShowInCollectionOptions,
                           initiative: c.Initiative,
                           // 🆕 **2026-10-26（执行代理 `B8_AV`）**：`targeted` 也照传（替身契约同上：
                           //   「除 `desc`/`descZh` 之外逐格相同」）。`Program.LoadPool` 从同一天起也传它
                           //   ⇒ 源卡有真值、替身若仍落 `-1`，两道闸（`AssignCardTargetFallback` 的
                           //   `Targeted != 1`）在两局里就会**不一致**，净差立刻混进噪声。
                           targeted: c.Targeted,
                           // 🔴 🆕 **2026-10-19（`A1952` · `W_引擎线_波4.md` §二·4）**：这两个字段原来也**没传**
                           //   ⇒ 替身与源卡不是「除 `desc` 外逐格相同」。`FromOriginalPool` 决定
                           //   `EffectSpec` 那一条分支（`CardDef.cs:240`），`GoesSecondId` 只在
                           //   `CreatePool.ResolveGoesSecond`（装池）里消费（替身不进池）。
                           //   ⚠️ 本笔按账文**只补这两个**；`armour:` 仍不传（波 4 §四·3 已核：
                           //     护甲走 `kw` 那一份，`armour:` 形参只喂对账用的 `ArmourFromPool`）。
                           fromOriginalPool: c.FromOriginalPool,
                           goesSecondId: c.GoesSecondId,
                           // 🆕 **2026-10-19（第五十五会话 `E8` · `A1959` 收口）**：`choicesDefinition` 三列
                           //   **同样照传** —— 替身的契约是「除 `desc`/`descZh` 之外逐格相同」，这三列
                           //   现在也参与 `EffectText.Parse(…, card:)` 的注入判据（`A1960`）。
                           //   ⚠️ 替身的 `desc` 已清空 ⇒ `HasUnspecifiedDarkPactGrant` 恒假 ⇒ **注入恒不发生**
                           //     ⇒ 对两局净差**零影响**（不是行为改动；补它是为了契约成立、将来别被静默改坏）。
                           choicesTargetsAffected: c.ChoicesTargetsAffected,
                           choicesMaxTargets: c.ChoicesMaxTargets,
                           choicesSpecificCardIds: c.ChoicesSpecificCardIds);
    }

    /// <summary>敌人那侧**有单位的**一格（先非督军格；都没有才退督军格）。⛔ 只决定「打哪一格」，
    /// 不是判据 —— 打不打得动由引擎的返回码说了算，如实进 `steps`。</summary>
    static int PickAnyEnemy(BattleContext ctx, int p)
    {
        var b = ctx.Players[1 - p].Board;
        for (int i = 0; i < BoardSpec.Size; i++)
            if (i != BoardSpec.WarlordSlot && b[i] != null) return i;
        return BoardSpec.WarlordSlot;
    }

    /// <summary>走一趟事件电池（<see cref="RunEventPass"/> 的正文与取舍见本区块头注）。</summary>
    static EventPass RunEventPass(List<CardDef> pool, CardDef card, PresetSpec preset = null)
    {
        return RunEventPass(pool, card, PassVariant.Main, preset);
    }

    /// <summary>`A1965`：带**夹具变体**的一趟电池（三个变体见 <see cref="PassVariant"/>）。
    /// ⚠️ 主档（`Main`）的步骤序列 = 原六步 ＋ 新增 6 步（`selfChip`/`chip2`/`tactic`/`troop2`/
    ///   `friendDies`/`collect`）＋ `enemyTactic`；三个变体**只改夹具的起始状态**，步骤一字不差
    ///   ⇒ 变体与主档的读数可以直接并进同一个 `net`（设计 §四）。
    /// 🆕 `P3`（2026-10-26）：`preset` 非 null 时**同一套预置**也进电池夹具 —— 否则「另一次全池扫描
    ///   带预设」这条路上，主档有预置、电池没有 ⇒ 两半读数不同口径（而且不会报错）。</summary>
    static EventPass RunEventPass(List<CardDef> pool, CardDef card, PassVariant variant,
                                  PresetSpec preset = null)
    {
        var ep = new EventPass();
        int handIdx; bool injected;
        var ctx = BuildFixture(pool, card, null, out handIdx, out injected, preset);

        // ---- 🆕 `A1965` 变体准备（⛔ 全部在「打出被测卡」之前做完）----
        switch (variant)
        {
            case PassVariant.LowEnergy:
                // V1：能量 = 卡费 ⇒ 打出之后 == 0（`Codex 145` 的唯一闸；设计 §三 · Codex 那一行）。
                ctx.Players[0].Energy = Math.Max(0, card.Cost);
                break;
            case PassVariant.FromDeck:
                // V2：被测卡放**牌库顶**、本回合抽到再打出（`Teleport 690` 要求 `DrawnThisTurn`）。
                //   `Deck` 的**末尾** = 牌库顶（`RuleCore.MoveDeckTopToHand` 取 `Count-1`，判据在那边）。
                if (handIdx >= 0)
                {
                    var inst = ctx.Players[0].Hand[handIdx];
                    ctx.Players[0].Hand.RemoveAt(handIdx);
                    ctx.Players[0].Deck.Add(inst);
                    RuleCore.Draw(ctx, 0);        // 它会置 `DrawnThisTurn` ＋ `SetupCardInHand`
                    handIdx = HandIdx(ctx, 0, card);
                    injected = true;              // 量具干预：不是「自然上手」那一档（与 `A1572` 同义）
                }
                break;
            case PassVariant.Solo:
                // V3：先把 P0 三张夹具部队打死 ⇒ 打出时棋盘只剩被测卡
                //   （条件 `Rally`：`If you control no other troops`）。走引擎 `ApplyDamage`
                //   （⛔ 不手改 `Board` —— 那会绕开护盾/触发/死亡那套，与引擎不同口径）。
                for (int s = 0; s < BoardSpec.Size; s++)
                {
                    var u0 = ctx.Players[0].Board[s];
                    if (u0 != null && !u0.IsWarlord) RuleCore.ApplyDamage(ctx, u0, 999, "夹具事件电池");
                }
                break;
        }

        if (handIdx < 0) { ep.Steps.Add("fixture=miss"); return ep; }

        // 🔴 🆕 **`A1965`：三个变体必须把【打出那一拍】也纳入 diff** —— 它们要治的事件
        //   （`Codex 145` / `Teleport 690` / 条件 `Rally`）**全部发生在 `PlayCard` 里面**
        //   （`RuleCore.cs:2567-2571` / `:2706` / 召唤触发那一带）⇒ 只从打出之后开始快照的话，
        //   这些事件**在电池里永远看不见**（本笔实测：`UM15` 的 V1 `energyAfter=0`、`fxCodex=1`
        //   却 `V1:net=0` —— 因为治疗发生在 `s0` 之前）。
        //   主档（`Main`）**保持原样**（打出那一拍由主档 `Measure` 覆盖，电池只看之后那一段）。
        Snap sPre = variant != PassVariant.Main ? Capture(ctx) : null;

        // ---- ⓪ 出牌（与 `MeasurePlay` 同一条路、同一个落点选择）----
        int slot = -1;
        // 🆕 `P3`（2026-10-26）：`pick=1` ⇒ 与主档**同一个**目标挑法（`PickPlayTarget`），
        //   否则「主档带目标、电池不带」两条读数不同口径（⛔ 不许这样，且它不会报错）。
        UnitState picked = (preset != null && preset.Pick > 0) ? PickPlayTarget(ctx, 0, card) : null;
        ep.PlayRc = picked != null
            ? RuleCore.PlayCard(ctx, 0, handIdx, FreeSlot(ctx, 0), out slot, picked)
            : RuleCore.PlayCard(ctx, 0, handIdx, FreeSlot(ctx, 0), out slot);
        ep.Played = ep.PlayRc == RuleCodes.OK;
        ep.Steps.Add("play=" + ep.PlayRc);
        if (!ep.Played) return ep;              // 打不出去 ⇒ 电池无从起步（主档已经报了这条）
        // V1：`Codex` 只在**打出那一拍**读「能量 == 0」⇒ 打完立刻把夹具能量还回去。
        //   ⛔ 不还的话，后面 `tactic`（0 费）之后的 `troop2`（1 费）会 `ErrCost` ——
        //   读数会变成「夹具没钱」而不是「这张卡在别的时机有没有实得」。
        if (variant == PassVariant.LowEnergy) SetEnergy(ctx, 0);

        // 🔴 逐段累积差异（⛔ 不是「末态减初态」）：中间态被改回去的（打伤又治好）那一种
        //    在「末态减初态」里**会自己抵消掉** ⇒ 那一段就白测了。与主档 `Diff(before,after)` 同形。
        //    ⚠️ 快照点全部在 `RunBatterySteps` 里（本方法只负责 diff 顺序 = 快照顺序）。
        // ---- ⓪ 之后**全部步骤**抽成 `RunBatterySteps`（🆕 `B9_W41S3B`）----
        //   🔴 单位档与督军档（`RunHeroBattery`）**共用同一份步骤序列** —— ⛔ 别各写一份
        //     （两份迟早不一致，而它们的净差要能横向比）。步骤顺序、快照顺序**逐字未动**。
        var snaps = new List<Snap>();
        if (sPre != null) snaps.Add(sPre);      // 变体：把「打出那一拍」也纳入（见上面 `sPre` 的说明）
        RunBatterySteps(ctx, slot, preset, ep, snaps);

        // 🔴 **逐段 diff，顺序 = 上面捕获的顺序**（⛔ 别只留首尾：中途被改回去的那一段会自己抵消）。
        for (int i = 1; i < snaps.Count; i++) Diff(snaps[i - 1], snaps[i], ep.Changes);
        return ep;
    }

    /// <summary>🆕 `B9_W41S3B`（2026-10-27）：**电池的步骤序列**（⓪ 出牌之后的一切）。
    /// 从 `RunEventPass` **逐字抽出**（顺序 / 快照点 / `steps` 文案一个字都没改），
    /// 为的是让**督军档**（<see cref="RunHeroBattery"/>）能复用同一份步骤 —— ⛔ 别复制第二份。
    /// 参数：`slot` = 被测单位那一格（单位档 = 落入的格；督军档 = `BoardSpec.WarlordSlot`）。
    /// `snaps` 由调用方建好（变体要先把「打出那一拍」那个 `sPre` 塞进去），本方法**按序追加**。
    /// `preset` 只用于 `selfdies=1` 那一步（⛔ 缺省 null ⇒ 与改前逐字相同）。</summary>
    static void RunBatterySteps(BattleContext ctx, int slot, PresetSpec preset, EventPass ep, List<Snap> snaps)
    {
        var s0 = Capture(ctx);

        // ---- ① 誓约（`Oath N:`）—— 🔴 **必须赶在回合电池【之前】、同一回合里用** ----
        //   判据 = `RuleCore.CanUseOathAbility`：`if (!OathAllTurns(...) && u.DeployedTurn != ctx.Turn)
        //   return ErrExhausted;` ⇒ **只有上场的那个回合**能用（实测：放在回合电池之后 ⇒
        //   电池自检的 `Oath` 那一对两条净差都是 0、`steps` 里是 `oath=6`）。
        //   ⚠️ 它**不查 `Exhausted`**（同那个方法：它查的是 `OathUsesThisTurn` 上限与 `DeployedTurn`）
        //   ⇒ 部署当回合就能用，⛔ 不需要（也不许）手改疲劳位。
        if (slot >= 0 && ctx.Players[0].Board[slot] != null)
            ep.Steps.Add("oath=" + RuleCore.UseOathAbility(ctx, 0, slot));
        var s1 = Capture(ctx);

        // ================================================================
        //  🆕 `A1965` 新增 6 步（治设计 §一「够不到的事件」那九族）
        // ================================================================

        // ---- ② `selfChip`：被测单位自己挨 2 点 ----
        //   治 `Strike: Heals N` / `UnitDamaged` / `When this unit is damaged, …` 那一族。
        //   🔴 设计 §七·3 点名的老坑：`BuildFixture` 只扣了 a/b/c 与督军，**从没扣过被测卡自己**
        //      （`FixtureChip` 这个常量本来就是为了「满血治疗看不见」设的）。
        //   ⚠️ 照设计原样打 **2 点**，但**封顶到 `Health-1`**（`A1965` 本笔的取舍，⛔ 不是原版值）：
        //      全池 unit 里有大量 1–2 血的卡，硬打 2 点会**当场打死被测卡** ⇒ 后面 6 步 ＋ 原来的
        //      ③④⑤⑥ 全部没有被测单位、净差塌成 0 —— 那是**覆盖面倒退**（比不补这步更糟）。
        //      封顶之后 `Heal` / `UnitDamaged` 两族的意图不变（掉血 ⇒ 治疗看得见），
        //      而 1 血单位**跳过**（打它必死，`selfChip=skip` 如实打出来）。
        if (slot >= 0 && ctx.Players[0].Board[slot] != null)
        {
            int hp0 = ctx.Players[0].Board[slot].Health;
            if (hp0 <= 1) ep.Steps.Add("selfChip=skip");
            else ep.Steps.Add("selfChip=" + RuleCore.ApplyDamage(ctx, ctx.Players[0].Board[slot],
                                                                Math.Min(2, hp0 - 1), "夹具事件电池"));
        }
        else ep.Steps.Add("selfChip=-");
        var sc1 = Capture(ctx);

        // ---- 🆕 2026-10-19（`A2001` 残余 · `SW22`）：**一发非致命攻击** ----
        //   `Strike: If the target is a troop and **survives**, …`（`SW22 Blackmane Inceptor`）要的是
        //   「目标挨了这一下**还活着**」；而下面 `chip2` 会把两个敌人削到 1 血、⑤ 的那两发按设计
        //   都是**致命一击** ⇒ 「survives」**结构上不可能为真**（判据 → `E8b_报告.md` §四）。
        //   ⇒ 在这里（`chip2` **之前**、敌人还满血）补一发：只打**打得动、但打不死**的那一格。
        //   判据 = 引擎自己的 `FieldAttack` vs 目标 `Health`（⛔ 不手改任何单位）。
        //   打不死才打；一发就会打死 / 攻击力为 0 ⇒ 如实打 `softAtk=skip(…原因)`，让「没验到」看得见。
        //   ⚠️ `forced: true` = 引擎自己的「不是玩家点出来的」那一档（原版 `scriptedAttack(0x34)`，
        //      跳过回合/配额），与 ⑤ 那两发**同一档**。
        //   ⚠️ 战后状态一并打出来（`same` = 目标还活着 ⇒ 这一发达成了设计目的）。
        var ss1 = sc1;
        if (slot >= 0 && ctx.Players[0].Board[slot] != null)
        {
            var me = ctx.Players[0].Board[slot];
            bool softRanged = RuleCore.CurrentAttackIsRanged(ctx, 0, slot);
            int dmg = RuleCore.FieldAttack(ctx, 0, me, softRanged);
            int softTgt = -1;
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var t = ctx.Players[1].Board[s];
                if (t == null || t.IsWarlord || t.Health <= dmg) continue;
                // ⚠️ **别让被测单位被反击打死**：`DeclareAttack` 的 `counterAtk = target.Attack`
                //    （`RuleCore.cs` 的「反击值现在就取」那段；远程照样吃反击，只有 `Long Range` 免）。
                //    本步的目的是「给 `survives` 一族一发非致命攻击」，⛔ 不是「顺手打死被测单位」——
                //    实测（`EC17`）：`selfChip` 之后它只剩 1 血，这一发会**当场被反击打死** ⇒
                //    回合末的 `At the end of your turn, …` 再也验不到（这是本步引入的回归，已挡住）。
                if (me.Health <= t.Attack && !me.Has("longrange")) continue;
                softTgt = s; break;
            }
            if (softTgt < 0)
                ep.Steps.Add("softAtk=skip" + (dmg <= 0 ? "(atk0)" : "(would-kill)"));
            else
            {
                var softVictim = ctx.Players[1].Board[softTgt];
                int rc = RuleCore.DeclareAttack(ctx, 0, slot, 1, softTgt, softRanged, forced: true);
                ep.Steps.Add("softAtk=" + rc + "@" + softTgt
                           + "/" + (softVictim != null && softVictim.Health > 0 && !softVictim.IsRemnant
                                    ? "survived" : Aftermath(ctx, 1, softVictim)));
            }
            ss1 = Capture(ctx);
        }
        else ep.Steps.Add("softAtk=-");

        // ---- ②b `chip2`：P1 最低两格部队各削到 1 血 ----
        //   让后面两发攻击**变得致命** ⇒ `Slay 120` 才会响（`TryCreditKill` 只在攻击路/效果路记凶手，
        //   而第 ⑥ 步那记 999 伤害**不给凶手** ⇒ 结构上换不来 Slay，见设计 §三·Slay）。
        ep.Steps.Add("chip2=" + ChipTo(ctx, 1, 2, 1));
        var sc2 = Capture(ctx);

        // ---- ③ `tactic`：P0 打一张**带目标**的战术，目标指向被测格 ----
        //   治 `Artifice 560`（只要「打出一张战术」）＋ `Stimulation 710`（要「带目标的战术**指到被测单位**」）。
        //   ⚠️ 夹具自己造的战术卡（`FixtureTactic`，0 费）走引擎 `PlayCard` → `PlayTactic`
        //      ⇒ 与玩家点出去的是同一条路。
        int tacSlot;
        ep.Steps.Add("tactic=" + PlayFixtureTactic(ctx, 0, slot, out tacSlot) + "@" + tacSlot);
        var sc3 = Capture(ctx);

        // ---- ④ `troop2`：P0 再部署一个部队（带 `Waystone`，留给 ⑤⑥）----
        //   治 `Uprising 680`（同回合再部署）＋ `When you deploy a X, …` 那一族；
        //   它同时是 ⑤ 要打死的那个「友军」、⑥ 要收的那颗路标石的来源。
        int waySlot;
        int wayRc = DeployFixture(ctx, 0, FixtureWaystoneTroop(), out waySlot);
        // ⚠️ `@格号` 是**诊断信息**（`A1965`）：部署失败时格号是 `-1`，下游两步会据此退化 ——
        //   不打出来的话「troop2=0 却没落场」在 `steps` 里看起来像成功了。
        ep.Steps.Add("troop2=" + wayRc + "@" + waySlot);
        // ⑤ 打的就是这一格；它没落场 ⇒ 那两步如实返回 `-`（⛔ 不假装测过）。
        int wreckSlot = waySlot;
        var sc4 = Capture(ctx);

        // ---- ② 两个来回：P0 → P1 → P0（驱动「回合开始 / 回合结束」那一族）----
        //   🆕 `A1965` 把这两拍**摊开写**了：P1 那一拍里要插两步（`enemyTactic` / `friendDies`）。
        ep.Steps.Add("end0=" + RuleCore.EndTurn(ctx));
        RuleCore.BeginTurn(ctx);                       // → P1 的回合
        // 🆕 `enemyTactic`：**在 P1 的回合里让对手打一张战术** ——
        //   治 `When your opponent plays a Stratagem, …` 那一族。
        {
            int eSlot;
            ep.Steps.Add("enemyTactic=" + PlayFixtureTactic(ctx, 1, -1, out eSlot) + "@" + eSlot);
        }
        var se0 = Capture(ctx);

        // ---- ⑤ `friendDies`：让 **P1 打 P0 那个路标石部队**（1 血 / 0 攻），治「友军阵亡」族 ----
        //   🔴 **为什么走「敌人打」而不是 `ApplyDamage`**（本笔踩过、实测）：
        //      `RuleCore.ApplyDamage` **不处理死亡** —— 它只扣血/发事件，真正离场在 `Hurt` →
        //      `RemoveIfDead` → `CleanupDeaths` 那一条上。探针打 999 点之后棋盘上留下的是
        //      **`-993` 血的活单位**（`steps` 里 `friendDies=999@1/live+way`）⇒ 翻不成残骸、
        //      `collect` 必报 `ErrNotWaystone(17)`。攻击路是引擎自己的死亡入口
        //      ⇒ 残骸由 `CleanupDeaths` 的 `Waystone` 分支生成（判据见那里的 `SupportMethods__HasToTransformIntoRemnant`）。
        //   ⚠️ `forced: true` = 引擎自己的「不是玩家点出来的」那一档（与下面两发攻击同一个口）。
        int friendRc = -1, friendAtk = -1;
        if (wreckSlot >= 0)
        {
            friendAtk = PickAttackerOnSide(ctx, 1);
            if (friendAtk >= 0)
                // ⚠️ 第 4 个形参是 **`tgtP`**（目标归属方）—— P1 打 P0 ⇒ **0**。
                //    本笔第一版误传成 `1`（= 打自己那一侧的空格）⇒ `ErrNotUnit(5)`，实测踩过。
                friendRc = RuleCore.DeclareAttack(ctx, 1, friendAtk, 0, wreckSlot, ranged: false, forced: true);
        }
        ep.Steps.Add("friendDies=" + friendRc + "@" + wreckSlot + "/atk@" + friendAtk);
        var sd1 = Capture(ctx);

        ep.Steps.Add("end1=" + RuleCore.EndTurn(ctx));
        RuleCore.BeginTurn(ctx);                       // → P0 的回合
        var s2 = Capture(ctx);

        // ---- ⑥ `collect`：收那颗路标石（治 `OnCollect*` 610/665/695）----
        //   `CollectWaystone` 只认 `IsRemnant && Has(Waystone)`（`RuleCore.CanCollectWaystone`）；
        //   不成立时把它的返回码**原样**打出来（⛔ 不静默）。
        //   ⚠️ 后面 `/rem:标志` 是**诊断**：`rem` = 残骸、`live` = 还是活单位、`-` = 那格空的；
        //      `+way` = 身上还带 `waystone`。没有它，`collect=17`（`ErrNotWaystone`）分不出
        //      「没翻面」和「翻面后丢了关键词」两种因（`A1965` 施工时踩过）。
        string remFlag = "-";
        if (wreckSlot >= 0 && ctx.Players[0].Board[wreckSlot] != null)
        {
            var occ = ctx.Players[0].Board[wreckSlot];
            remFlag = (occ.IsRemnant ? "rem" : "live") + (occ.Has(KeywordTable.Waystone) ? "+way" : "");
        }
        ep.Steps.Add("collect=" + (wreckSlot >= 0 && ctx.Players[0].Board[wreckSlot] != null
                                   ? RuleCore.CollectWaystone(ctx, 0, wreckSlot).ToString() : "-")
                     + "@" + wreckSlot + "/" + remFlag);
        var sd2 = Capture(ctx);

        // ---- ③ 抽一张 ----
        RuleCore.Draw(ctx, 0);
        ep.Steps.Add("draw=1");
        var s3 = Capture(ctx);

        // ---- ④ 主动用（`Duty:` / `Pray:` / `Ferocity:` / `Agenda:` / `Talent:` / `Artifice:`）----
        int tgt = PickAnyEnemy(ctx, 0);
        if (slot >= 0 && ctx.Players[0].Board[slot] != null)
        {
            ep.Steps.Add("abil=" + RuleCore.UseAbility(ctx, 0, slot, tgt));
            var alt = RuleCore.AvailableAlternative(ctx, 0, slot);
            if (alt != null)
                ep.Steps.Add("alt(" + alt + ")=" + RuleCore.UseAlternative(ctx, 0, slot, alt, tgt));
        }
        var s4 = Capture(ctx);

        // ---- ⑤ 攻击（`Strike:` / `Mob:` / `Regiment:` / `Cruelty:`）----
        //   🔴 **打两发**（一发近战 ＋ 一发远程）—— 判据在引擎里是**分成两条**的：
        //     `RuleCore.cs:4312` `if (!ranged && …) FireTriggerAt(… Mob …)` ·
        //     `:4334` `if (ranged && …) FireTriggerAt(… Regiment …)`
        //     ⇒ **只打一发**时，挂另一个头的整族**在电池里永远抬不起来**
        //     （实测：自检的 `Regiment` 那一对两条净差**都是 0**，红的）。
        //   第一发走引擎自己的自然选择（`CurrentAttackIsRanged`）；第二发是**另一种**，
        //   走 `DeclareAttack(..., forced: true)` = 引擎自己的「**不是玩家点出来的**那一档」
        //   （原版 `scriptedAttack(0x34)`，见 `CanAttackNow` 头注：它跳过回合/配额那一段）——
        //   ⛔ 探针**不手改 `Exhausted` / `AttacksThisTurn`**。两发的返回码**都进 `steps`**。
        if (slot >= 0 && ctx.Players[0].Board[slot] != null)
        {
            bool first = RuleCore.CurrentAttackIsRanged(ctx, 0, slot);
            ep.Steps.Add((first ? "atkR" : "atkM") + "="
                         + RuleCore.DeclareAttack(ctx, 0, slot, 1, PickAnyEnemy(ctx, 0), first));
        }
        var s5 = Capture(ctx);
        if (slot >= 0 && ctx.Players[0].Board[slot] != null)
        {
            // ⚠️ 第二步那两格叫 `atkM2` / `atkR2`（**第二发**）—— ⛔ 别写成 `atkM!` / `atkR!`，
            //    那会和 `!=` 撞脸，读成「atkR 不等于 0」。
            bool second = !RuleCore.CurrentAttackIsRanged(ctx, 0, slot);
            ep.Steps.Add((second ? "atkR2" : "atkM2") + "="
                         + RuleCore.DeclareAttack(ctx, 0, slot, 1, PickAnyEnemy(ctx, 0), second, forced: true));
        }
        var s6 = Capture(ctx);

        // ---- ⑥ 打死一个敌人（`Slay:` / `When … dies` / 死亡触发那一族）----
        //   🆕 `A1965`：`chip2` 把 P1 两格削到 1 血 ⇒ 上面两发攻击**会自己打死人** ⇒
        //   这一族现在由**攻击路**（`TryCreditKill`，`RuleCore.cs:4704`）兑现，而不是靠下面这记 999。
        //
        //   🔴 🆕 **`A1965` 续工（2026-10-19 · 调度台裁定）：这一句原来是一记裸 `ApplyDamage(999)` ——
        //   而 `RuleCore.ApplyDamage` **不处理死亡**（只扣血/发事件，`Hurt` → `RemoveIfDead` → `CleanupDeaths`
        //   才离场）⇒ 棋盘上留下的是**负血活单位**，**一条死亡触发都没响过**
        //   （本笔实测：`kill(dmg)=999` 之后那一格是 `-993` 血、`friendDies=999@1/live+way`）。
        //   ⇒ 改成与 `friendDies` **同一口径的引擎攻击路**：
        //     ① 先把目标削到 **1 血**（`ApplyDamage` 只削不死，正好当这一步的「准备」）；
        //     ② 让**被测单位自己**去打（`forced: true` = 引擎自己的「不是玩家点出来的」那一档，
        //        跳过回合/配额 ⇒ `Slay` 记得到凶手）；它打不了（0 攻 / 已死）就退到**己方一个夹具部队**；
        //     ③ 两次都没打死 / P0 压根没有能攻击的单位 ⇒ 退回旧口径，`steps` 里**点名** `dmg(deadless)`。
        //   ⚠️ `steps` 形如 `kill=<rc>@<格><M|R>/<战后状态>`，状态三档：`live` / `rem`（翻成残骸）/ `gone`。
        //      ⛔ 不许只打 rc —— 「打出去了」和「人真的没了」是两件事（这正是本笔要治的那种静默）。
        int kv = PickKillTarget(ctx, 1);
        var victim = kv >= 0 ? ctx.Players[1].Board[kv] : null;
        string killStep = "-";
        if (victim != null)
        {
            // ① 削到 1 血（不致死；只有 `Hurt` 那条路才离场）
            if (victim.Health > 1) RuleCore.ApplyDamage(ctx, victim, victim.Health - 1, "夹具事件电池");
            // ② 候选击杀者：被测单位优先（Slay 的凶手），其次己方攻击力最高的部队
            var killers = new List<int>();
            bool kRanged = false;
            int k0 = PickKiller(ctx, 0, slot, out kRanged);
            if (k0 >= 0) killers.Add(k0);
            int k1 = PickAttackerOnSide(ctx, 0);
            if (k1 >= 0 && !killers.Contains(k1)) killers.Add(k1);
            var parts = new List<string>();
            foreach (int k in killers)
            {
                var u = ctx.Players[0].Board[k];
                if (u == null) continue;
                bool ranged = u.RangedAttack > u.Attack;    // 哪种更高就用哪种打
                int rc = RuleCore.DeclareAttack(ctx, 0, k, 1, kv, ranged, forced: true);
                string after = Aftermath(ctx, 1, victim);
                parts.Add(rc + "@" + k + (ranged ? "R" : "M") + "/" + after);
                if (after != "live") break;                 // 真的死了/翻面了 ⇒ 不再补刀
            }
            if (parts.Count == 0)
            {
                // ③ 没有任何能攻击的己方单位：退回旧口径，但**如实点名**（这一档抬不到死亡族）
                int rc = RuleCore.ApplyDamage(ctx, victim, 999, "夹具事件电池");
                parts.Add(rc + "/dmg(deadless)/" + Aftermath(ctx, 1, victim));
            }
            killStep = string.Join(";", parts) + "@tgt" + kv;
        }
        ep.Steps.Add("kill=" + killStep);
        var s7 = Capture(ctx);

        // ---- ⑦ 🆕 `B9_W41S3B`（2026-10-27）：`preset selfdies=1` ⇒ **把被测单位自己打死** ----
        //   治 **81 `Unstable`**（17 行 / 17 张）：引擎那一支挂在「**本单位死亡**」那一拍上
        //   （`RuleCore.cs` 的 `if (u.Has(KeywordTable.Unstable)) EnqueueAbility(… UnstableBlastDeferred …)`，
        //   位置在 `CleanupDeaths` 的死亡族里、Backlash(80) 之后），而本电池**从不杀被测单位**
        //   （`friendDies` 只杀 P0 那颗路标石）⇒ 这个事件**结构上造不出来**。
        //   🔴 **必须走引擎的死亡入口**：`RuleCore.ApplyDamage` **不处理死亡**（只扣血/发事件），
        //     999 之后棋盘上留下的是**负血活单位**、一条死亡触发都不响
        //     （`friendDies` 那一整段注释就是踩这个坑写的）。⇒ 用 `HurtWithSourceForTest`（= `Hurt` 的公开包装）。
        //   ⚠️ 被测单位**已经死了**（被 `selfChip` / 反击 / `Solo` 变体打死）⇒ 如实打 `selfDies=skip`，
        //     ⛔ 不假装测过。⛔ 不传 `selfdies=1` 时这一步**一行都不执行**（`steps` 一个字节都不变）。
        var s8 = s7;
        if (preset != null && preset.SelfDies > 0)
        {
            var me = (slot >= 0 && BoardSpec.IsValid(slot)) ? ctx.Players[0].Board[slot] : null;
            if (me == null)
                ep.Steps.Add("selfDies=skip(dead)");
            else
            {
                // 🔶 自己设计（非原版）：「打 999 点把它打死」是**量具**挑的数值（原版没有这个事件），
                //    判据 = 只要必死即可；选 999 与电池别处（`friendDies`/`kill`）同一口径 · 调度台 2026-10-27 裁定
                int rc = RuleCore.HurtWithSourceForTest(ctx, me, 999, "夹具事件电池（自毁）");
                ep.Steps.Add("selfDies=" + rc + "/" + Aftermath(ctx, 0, me));
                // ⚠️ `Aftermath` 只在棋盘上取那一格 ⇒ 死了就报 `gone`/`rem`，活着报 `live`。
            }
            s8 = Capture(ctx);
        }

        snaps.AddRange(new[] { s0, s1, sc1, ss1, sc2, sc3, sc4, se0, sd1, s2, sd2, s3, s4, s5, s6, s7, s8 });
        ep.End = s8;
    }

    // ==================================================================
    //  🆕 `A1965`（2026-10-19 第五十五会话 · 写手 `E8`）：电池新增 6 步 ＋ 三个夹具变体
    // ==================================================================
    //
    // 🔴 施工图 = `资料/普查产出_第三十会话/A1965_电池步设计.md` §四/§五（⛔ 别在这里另立一套口径）。
    //   病灶：原来的电池只有 ⓪出牌 ①誓约 ②两个来回 ③抽一张 ④主动用 ⑤两发攻击 ⑥打死一个
    //   ⇒ `Artifice` / `Stimulation` / `Uprising` / `OtherUnitDeath` / `OnCollect*` / `Slay` /
    //     `Codex` / `Teleport` / 条件 `Rally` 这九族**结构上够不到**（`play check` ①-c 里那一批）。
    //   本笔补：**主电池 6 步**（`selfChip` / `chip2` / `tactic` / `troop2` / `friendDies` / `collect`）
    //   ＋ `enemyTactic`（在两个来回里让 P1 打一张战术）＋ **三个夹具变体**
    //   （V1 能量=卡费 ⇒ Codex · V2 牌库抽到再打出 ⇒ Teleport · V3 棋盘只留被测卡 ⇒ 条件 Rally）。
    //
    // 🔴 **本笔会改全池 `E|` 段每一行**（设计 §四·风险 1）⇒ 基线/指纹**必须重生成并重钉**
    //   —— 按既定口径**本波不做**（留到引擎线收口统一做）⇒ `play check` 现红是**预期的**。
    //
    // ⚠️ **两局（电池局 / 空正文对照局）跑的是同一套步骤** ⇒ 夹具自己造出来的噪声（多打一张战术、
    //   多部署一个部队、路标石残骸…）在净差里**相减抵消**。唯一抵消不掉的是 **RNG 分叉**
    //   （两局手牌张数错开 ⇒ 同一新步骤在两局里随机选择不同）—— 设计 §五·1 的判据
    //   `net > 0 ⇒ raw != ctrl` 就是兜它；本笔**逐变体**都按同一条形状判（见 `MergeVariant`）。

    /// <summary>🆕 `A1965`：事件电池的**四遍夹具**。`Main` = 原样；三个变体各治一族够不到的事件。</summary>
    enum PassVariant
    {
        /// <summary>原夹具（主档）。</summary>
        Main,
        /// <summary>V1：P0 能量 = 卡费 ⇒ 打出后 == 0（`Codex 145` 的闸，设计 §三）。</summary>
        LowEnergy,
        /// <summary>V2：被测卡放牌库顶、本回合抽到再打出（`Teleport 690` 要求 `DrawnThisTurn`）。</summary>
        FromDeck,
        /// <summary>V3：先把 P0 三张夹具部队打死 ⇒ 打出时棋盘只剩被测卡（条件 `Rally`）。</summary>
        Solo,
    }

    /// <summary>🆕 `A1965`：往 `p` 的手牌里注入一张**夹具专用**卡（走引擎自己的
    /// `NewInstance` + `SetupCardInHand`，与 `BuildFixture` 补被测卡那一段**同形**）。返回手牌下标。</summary>
    static int InjectToHand(BattleContext ctx, int p, CardDef card)
    {
        var inst = ctx.NewInstance(card);
        ctx.Players[p].Hand.Add(inst);
        RuleCore.SetupCardInHand(ctx, p, inst);
        return HandIdx(ctx, p, card);
    }

    /// <summary>🆕 `A1965`：夹具专用战术卡（`tactic` / `enemyTactic` 两步用）。
    /// 正文 = `Give +1 [Melee] to a friendly troop` ⇒ **带目标、指向自己一侧的部队**：
    /// `Artifice 560` 只要「打出一张战术」，`Stimulation 710` 要「带目标的战术**指到被测单位**」。
    /// 费用 0 ⇒ 两步都不会因为夹具没钱而打不出去（那会把「事件没到」和「打不起」混成一根红）。</summary>
    static CardDef FixtureTactic() =>
        new CardDef("FIX_TAC", "FIX_TAC", "tactic", "Give +1 [Melee] to a friendly troop",
                    "common", "Fixture", 0, 0, 0, 0, null);

    /// <summary>🆕 `A1965`：夹具专用「路标石」部队（`troop2` 部署的就是它）。
    /// `Waystone` ⇒ 死亡时原地留残骸（`RuleCore.CleanupDeaths` 的 `Waystone` 分支），
    /// `collect` 那一步才有东西可收（`CollectWaystone` 只认 `IsRemnant && Has(Waystone)`）。
    /// ⚠️ **1 血 / 0 攻**是**为这一步挑的**（⛔ 不是原版值）：1 血 ⇒ P1 任何一击都能打死它、
    ///   0 攻 ⇒ **不反击**（否则打死它的那个 P1 夹具单位会被换掉 1 点血，给净差添噪声）。</summary>
    static CardDef FixtureWaystoneTroop() =>
        new CardDef("FIX_WAY", "FIX_WAY", "unit", "", "common", "SaimHann", 1, 0, 1, 0,
                    new[] { "Waystone" }, subtype: "Troop");

    /// <summary>该侧一个**真能当目标**的格（先要 `prefer`；没有就退到第一个非督军格）。
    /// ⚠️ 这只是**夹具怎么点**，不是判据 —— 选错时引擎会返回错误码、如实进 `steps`。</summary>
    static int PickOwnSlot(BattleContext ctx, int p, int prefer)
    {
        var b = ctx.Players[p].Board;
        if (BoardSpec.IsValid(prefer) && prefer != BoardSpec.WarlordSlot && b[prefer] != null) return prefer;
        for (int i = 0; i < BoardSpec.Size; i++)
            if (i != BoardSpec.WarlordSlot && b[i] != null) return i;
        return BoardSpec.WarlordSlot;
    }

    /// <summary>某个单位在 `p` 那一侧棋盘上的格号（找不到 = `-1`）。</summary>
    static int SlotOf(BattleContext ctx, int p, UnitState u)
    {
        var b = ctx.Players[p].Board;
        for (int i = 0; i < BoardSpec.Size; i++) if (ReferenceEquals(b[i], u)) return i;
        return -1;
    }

    /// <summary>🆕 `A1965`：该侧一个**近战攻击力最高**的部队格（同攻取格号小者；`-1` = 没有）。
    /// 用来让 P1 打死那个 1 血的路标石部队（`friendDies` 一步）—— 排序**定死**、不随机。</summary>
    static int PickAttackerOnSide(BattleContext ctx, int p)
    {
        var b = ctx.Players[p].Board;
        int best = -1;
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            var u = b[s];
            if (u == null || u.IsWarlord || u.IsRemnant || u.Attack <= 0) continue;
            if (best < 0 || u.Attack > b[best].Attack) best = s;
        }
        return best;
    }

    /// <summary>🆕 `A1965` 续工：⑥ 的击杀目标 —— 先挑**非督军**的敌人（督军打不死、只会判胜负），
    /// 没有才退到督军格；`-1` = 敌方棋盘是空的。</summary>
    static int PickKillTarget(BattleContext ctx, int p)
    {
        var b = ctx.Players[p].Board;
        for (int s = 0; s < BoardSpec.Size; s++)
            if (s != BoardSpec.WarlordSlot && b[s] != null && !b[s].IsRemnant) return s;
        for (int s = 0; s < BoardSpec.Size; s++)
            if (b[s] != null) return s;
        return -1;
    }

    /// <summary>🆕 `A1965` 续工：⑥ 的击杀者 —— 优先**被测单位自己**（`Slay` 的凶手才记得到它），
    /// 打不了（0 攻 / 不在了）就退到己方攻击力最高的那个部队。
    /// `out ranged` = 该用远程打（远程值高于近战值时）。</summary>
    static int PickKiller(BattleContext ctx, int p, int measuredSlot, out bool ranged)
    {
        ranged = false;
        var b = ctx.Players[p].Board;
        if (BoardSpec.IsValid(measuredSlot) && b[measuredSlot] != null && !b[measuredSlot].IsWarlord)
        {
            var m = b[measuredSlot];
            if (Math.Max(m.Attack, m.RangedAttack) > 0)
            {
                ranged = m.RangedAttack > m.Attack;
                return measuredSlot;
            }
        }
        int best = -1, bestV = 0;
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            var u = b[s];
            if (u == null || u.IsWarlord || u.IsRemnant) continue;
            int v = Math.Max(u.Attack, u.RangedAttack);
            if (v <= 0 || v <= bestV) continue;
            best = s; bestV = v;
        }
        if (best >= 0) ranged = b[best].RangedAttack > b[best].Attack;
        return best;
    }

    /// <summary>🆕 `A1965` 续工：一个单位被这一下之后**还在不在场** ——
    /// `live`（原实例还占着格）/ `rem`（翻成残骸：新实例、同一 `CardInstance`）/ `gone`（真离场）。
    /// ⚠️ 必须按**实例**找，⛔ 不能按格号：连续棋盘会在死亡时整体内移。</summary>
    static string Aftermath(BattleContext ctx, int p, UnitState u)
    {
        var b = ctx.Players[p].Board;
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            if (b[s] == null) continue;
            if (ReferenceEquals(b[s], u)) return "live";
            if (b[s].Instance == u.Instance && b[s].IsRemnant) return "rem";
        }
        return "gone";
    }

    /// <summary>🆕 `A1965` 的 `chip2`：把 `p` 一侧**血最低的 `take` 个单位**各削到 `toHp` 血。
    /// 走引擎自己的 `ApplyDamage`（⛔ 不手改 `Health`）—— 与 `BuildFixture` 的 `FixtureChip` 同一条取舍。
    /// 排序 = (当前血量, 格号)：**定死**，⛔ 不随机（本仓「对局必须可复现」）。</summary>
    static string ChipTo(BattleContext ctx, int p, int take, int toHp)
    {
        var b = ctx.Players[p].Board;
        var pick = new List<UnitState>();
        for (int s = 0; s < BoardSpec.Size; s++)
            if (b[s] != null && !b[s].IsWarlord && !b[s].IsRemnant && b[s].Health > toHp) pick.Add(b[s]);
        pick = pick.OrderBy(u => u.Health).ThenBy(u => SlotOf(ctx, p, u)).Take(take).ToList();
        if (pick.Count == 0) return "-";
        var rcs = new List<string>();
        foreach (var u in pick)
            rcs.Add(RuleCore.ApplyDamage(ctx, u, u.Health - toHp, "夹具事件电池").ToString());
        return string.Join(",", rcs);
    }

    /// <summary>🆕 `A1965` 的 `tactic` / `enemyTactic`：给 `p` 打一张夹具战术（目标指向该侧 `prefer` 格）。
    /// `out targetSlot` = 实际传给引擎的目标格（`-1` = 没打成 / 注入失败）—— `steps` 里要看得见它，
    /// 否则 `tactic=18`（`ErrNoTargetAvailable`）分不出「没给格」和「那格不合法」。</summary>
    static int PlayFixtureTactic(BattleContext ctx, int p, int prefer, out int targetSlot)
    {
        targetSlot = -1;
        int idx = InjectToHand(ctx, p, FixtureTactic());
        if (idx < 0) return -1;
        targetSlot = PickOwnSlot(ctx, p, prefer);
        return RuleCore.PlayCard(ctx, p, idx, targetSlot);
    }

    /// <summary>🆕 `A1965` 的 `troop2`：给 `p` 部署一张夹具部队，报出**真正落下的格**（`-1` = 没落场）。</summary>
    static int DeployFixture(BattleContext ctx, int p, CardDef card, out int landed)
    {
        landed = -1;
        int idx = InjectToHand(ctx, p, card);
        if (idx < 0) return -1;
        int rc = RuleCore.PlayCard(ctx, p, idx, FreeSlot(ctx, p), out landed);
        if (rc != RuleCodes.OK) landed = -1;
        return rc;
    }

    /// <summary>`A1431` 第三笔：一张卡的**事件电池读数**（含对照局的净差）。
    /// 🆕 `A1965`：除主档那一遍之外，再跑**三个夹具变体**（<see cref="PassVariant"/>），
    ///   四遍的净差**取并集**（同一个 `Net` 列，⛔ 不开四列）—— 见 <see cref="MergeVariant"/>。</summary>
    static EventReading MeasureEvents(List<CardDef> pool, CardDef card, PresetSpec preset = null)
    {
        var r = new EventReading { C = card };
        try
        {
            r.Bucket = Bucket(card);
            var a = RunEventPass(pool, card, preset);
            r.Steps = string.Join(",", a.Steps);
            r.PlayRc = a.PlayRc;
            r.Played = a.Played;
            r.Raw = a.Changes.Count;
            // 🆕 2026-10-27（`B9_E5ENG`）：计数两格（放进电池指纹；早退那几条也带上）
            if (a.End != null) { r.AtkCount = a.End.ActCnt[0] + " ‖ " + a.End.ActCnt[1]; r.Exh = a.End.Exh[0] + " ‖ " + a.End.Exh[1]; }
            if (!a.Played) { r.Note = "电池没起步（出牌 rc=" + CodeName(a.PlayRc) + "）"; return r; }
            var b = RunEventPass(pool, Blank(card, preset == null ? null : preset.StripKw), preset);
            r.CtrlPlayed = b.Played;
            r.Ctrl = b.Changes.Count;
            if (!b.Played)
            {
                // 🔴 **对照局没起步 ⇒ 净差不可信** —— 出声，⛔ 不拿「电池局减 0 条」当净差。
                r.Note = "**对照局没起步**（空正文替身 rc=" + CodeName(b.PlayRc) + "）⇒ 本行净差不可信";
                r.Net = a.Changes;
            }
            else
            {
                r.Net = a.Changes.Where(x => !b.Changes.Contains(x)).ToList();
                if (a.End != null && b.End != null) r.NetDmg = a.End.Dmg[0] - b.End.Dmg[0];
                // 🆕 `A1965`：三个夹具变体各跑一遍（a/b 两局同步骤 ⇒ 噪声相减抵消）。
                MergeVariant(pool, card, PassVariant.LowEnergy, "V1", r, preset);
                MergeVariant(pool, card, PassVariant.FromDeck,  "V2", r, preset);
                MergeVariant(pool, card, PassVariant.Solo,      "V3", r, preset);
            }
        }
        catch (Exception ex)
        {
            r.Crash = ex.GetType().Name + ": " + ex.Message;
        }
        return r;
    }

    /// <summary>🆕 `A1965`：跑**一个夹具变体**，把它的**新增净差**并进 `r.Net`（同一个 `net` 列）。
    ///
    /// 🔴 **防洗绿的两条（设计 §五·1 的形状，本笔改成【逐变体】各判一次）**：
    ///   ① **两局（电池局 / 空正文对照局）的变化【逐条相同】⇒ 这一遍跳过** ——
    ///      没有可归因的净差；同时这也是「两局跑了不同事件序列」（RNG 分叉）的挡板。
    ///      ⚠️ 设计原文那条判据是 `net > 0 ⇒ raw != ctrl`，而 `raw`/`ctrl` 两列
    ///      **只统计主档那一遍**（`PfECols` 未改）⇒ **有了变体之后不能拿它当全局判据**；
    ///      变体这一侧的等价物就是这里的 `SequenceEqual` 短路。
    ///   ② 变体自己没起步（出牌失败 / 对照局没起步）⇒ **整遍跳过**并在 `steps` 里点名，
    ///      ⛔ 不当作「净差 0」（否则「没跑」与「跑了没效果」在读数上分不开）。
    ///
    /// ⚠️ 只有**真带来新条目**时才把这个变体的 `steps` 追加进 `r.Steps`（前缀 = `V1:`/`V2:`/`V3:`）
    ///   —— 这样「主档那一遍没变」的行**一个字节都不变**，变体只在它**真救回一张卡**时才留下痕迹。
    /// ⚠️ `NetDmg` 仍只取主档那一遍（四个变体的伤害基数不同，混在一起不可比）。</summary>
    static void MergeVariant(List<CardDef> pool, CardDef card, PassVariant v, string tag, EventReading r,
                             PresetSpec preset = null)
    {
        var a = RunEventPass(pool, card, v, preset);
        // 🔴 **不许静默**：变体没起步 ⇒ `steps` 里**点名**（否则「这一遍没救回任何东西」
        //   和「这一遍压根没跑」在读数上长得一模一样 —— 正是本笔要治的那一类）。
        if (!a.Played) { r.Steps += " | " + tag + ":miss(rc=" + a.PlayRc + ")"; return; }
        var b = RunEventPass(pool, Blank(card, preset == null ? null : preset.StripKw), v, preset);
        if (!b.Played) { r.Steps += " | " + tag + ":ctrlMiss(rc=" + b.PlayRc + ")"; return; }
        if (a.Changes.Count == b.Changes.Count && a.Changes.SequenceEqual(b.Changes))
            return;                                  // 两局逐条相同 ⇒ 没有可归因的净差（也挡 RNG 分叉）
        var net = a.Changes.Where(x => !b.Changes.Contains(x)).ToList();
        bool added = false;
        foreach (var x in net) if (!r.Net.Contains(x)) { r.Net.Add(x); added = true; }
        if (added) r.Steps += " | " + tag + ":" + string.Join(",", a.Steps);
    }

    // ==================================================================
    //  局面指纹 / 差异
    // ==================================================================

    static Snap Capture(BattleContext ctx)
    {
        var s = new Snap();
        for (int p = 0; p < 2; p++)
        {
            var ps = ctx.Players[p];
            s.Energy[p] = ps.Energy;
            s.Cells[p] = new string[BoardSpec.Size];
            for (int i = 0; i < BoardSpec.Size; i++) s.Cells[p][i] = Cell(ps.Board[i]);
            foreach (var c in ps.Hand) s.HandIds[p].Add(HandEntry(c));
            foreach (var c in ps.Deck) s.DeckIds[p].Add(c.Card.Id);
            foreach (var c in ps.Discard) s.DiscIds[p].Add(c.Card.Id);
            s.HandIds[p].Sort(StringComparer.Ordinal);
            s.DeckIds[p].Sort(StringComparer.Ordinal);
            s.DiscIds[p].Sort(StringComparer.Ordinal);
            // 🆕 **手牌总价**（逐张走引擎的 `CostOf`）—— 用来让「手牌减费 / 下一张减费」那一族
            //    （`lowercost` / `next …`）**在读数里看得见**：它们**不改棋盘**，只改价格，
            //    光靠棋盘+区域三样是**照不到**的（记着的已知盲区）。
            int sum = 0;
            foreach (var c in ps.Hand) sum += RuleCore.CostOf(ctx, p, c);
            s.HandCost[p] = sum;
            s.Faith[p] = ps.Faith;
            s.Spirit[p] = ps.SpiritStones;
            s.Quest[p] = ps.QuestPoints;
            s.Dmg[p] = ctx.DamageToEnemy[p];
            s.Troops[p] = ctx.TroopsPlayed[p];
            // 🆕 2026-10-27（`B9_E5ENG`）：计数类两格（见 `Snap.ActCnt` / `Snap.Exh` 的 doc）
            s.ActCnt[p] = UnitNumCol(ctx, p, false);
            s.Exh[p] = UnitNumCol(ctx, p, true);
        }
        return s;
    }

    /// <summary>一格 = `id 攻/远/甲/血 {关键词…}`（空格 = `.`）。
    /// ⚠️ 督军那格打 `WARLORD` 而**不打它的 id** —— 那是为了让**对照局**（督军档）的两份指纹可比
    /// （id 不同 ≠ 局面不同）；督军自己的数值与关键词照打。</summary>
    static string Cell(UnitState u)
    {
        if (u == null) return ".";
        var sb = new StringBuilder();
        sb.Append(u.IsWarlord ? "WARLORD" : u.Card.Id);
        sb.Append(' ').Append(u.Attack).Append('/').Append(u.RangedAttack).Append('/')
          .Append(u.Armor).Append('/').Append(u.Health);
        var kw = u.Keywords;
        if (kw != null && kw.Count > 0)
        {
            sb.Append(" {");
            bool first = true;
            foreach (var k in kw.OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                if (!first) sb.Append(", ");
                first = false;
                sb.Append(k.Key);
                if (k.Value != 0) sb.Append(k.Value);
            }
            sb.Append('}');
        }
        return sb.ToString();
    }

    /// <summary>🆕 **`B8_AV`（2026-10-26）：把棋盘上的 `PlayOrder`（进场序）摊成一列。**
    ///
    /// <para>**形状**：`P0[4]=0 P0[3]=1 P0[6]=2 P0[7]=3 P1[4]=4 …`，**按 `PlayOrder` 升序** ——
    /// ⇒ 这一列字面上的**从左到右就是「谁先响」**（原版 `allCardsInPlay` 的遍历序，
    /// 判据 → `UnitState.PlayOrder` 的 doc）。每格打的是 **`P{方}[{格}]={号}`**（号 = 原始
    /// `PlayOrder`；`-1` = 从没取过号，按 `PlayerState.UnitsByPlayOrder` 的口径**排末尾**）。</para>
    ///
    /// ⚠️ **督军也在里面**：`RuleCore.BuildPlayer`（`RuleCore.cs:649`）给督军发了第一个号，
    /// 而 `UnitsByPlayOrder()` **刻意不含督军**（它仿的是原版 `GetMinionList`）——本列**照棋盘逐格收**，
    /// 所以别拿它去和 `UnitsByPlayOrder().Count` 比张数。
    /// ⚠️ 本函数**只读**、`ctx == null`（督军档 / 没起步）时返回空串（⛔ 不编一个假次序出来）。</summary>
    static string PlayOrderCol(BattleContext ctx)
    {
        if (ctx == null) return "";
        var rows = new List<int[]>();     // {排序键, 方, 格, 原始号}
        for (int p = 0; p < 2; p++)
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                var u = ctx.Players[p].Board[s];
                if (u == null) continue;
                rows.Add(new[] { u.PlayOrder < 0 ? int.MaxValue : u.PlayOrder, p, s, u.PlayOrder });
            }
        rows.Sort((a, b) =>
        {
            if (a[0] != b[0]) return a[0].CompareTo(b[0]);
            if (a[1] != b[1]) return a[1].CompareTo(b[1]);
            return a[2].CompareTo(b[2]);
        });
        var sb = new StringBuilder();
        foreach (var r in rows)
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.Append('P').Append(r[1]).Append('[').Append(r[2]).Append("]=").Append(r[3]);
        }
        return sb.ToString();
    }

    static string Fingerprint(Snap s)
    {
        return "P0 能=" + s.Energy[0] + " 手=" + List(s.HandIds[0]) + " 手牌总价=" + s.HandCost[0]
             + " 库=" + s.DeckIds[0].Count
             + " 弃=" + List(s.DiscIds[0]) + " 伤害=" + s.Dmg[0] + " 出兵=" + s.Troops[0]
             + " 信仰=" + s.Faith[0] + " 灵魂石=" + s.Spirit[0] + " 任务点=" + s.Quest[0]
             + "\n     P0 棋盘 " + Board(s.Cells[0])
             + "\n     P1 棋盘 " + Board(s.Cells[1])
             + "   （P1 能=" + s.Energy[1] + " 手=" + List(s.HandIds[1]) + " 库=" + s.DeckIds[1].Count
             + " 弃=" + List(s.DiscIds[1]) + " 伤害=" + s.Dmg[1] + "）"
             // 🆕 2026-10-27（`B9_E5ENG`）：计数类两格（`AttacksThisTurn` / `Exhausted`）—— 原来**全盲**
             + "\n     行动计数 " + s.ActCnt[0] + " ‖ " + s.ActCnt[1]
             + "   疲劳位 " + s.Exh[0] + " ‖ " + s.Exh[1];
    }

    static string List(List<string> l) { return l.Count == 0 ? "(空)" : "[" + string.Join(",", l) + "]"; }

    /// <summary>🆕 **2026-10-27（`B9_E5ENG`）**：棋盘上每格的 `AttacksThisTurn`（`exhausted:false`）
    /// 或 `Exhausted`（`exhausted:true`）摊成一串，供 <see cref="Snap"/> 收进指纹。
    /// 形状 = `P0[3]=1 P0[5]=2`（**只收有人那格 · 只打非 0 值** ⇒ 空串 = 全员 0 / 全员未疲劳；
    /// 这样 dump 不会被每个单位刷一格、而「0→1」「1→2」照样看得见）；
    /// 顺序 = **格号升序**（确定性，不依赖任何遍历序）。
    /// ⚠️ `ctx == null` ⇒ 空串（⛔ 不编一个假读数出来，同 `PlayOrderCol`）。</summary>
    static string UnitNumCol(BattleContext ctx, int p, bool exhausted)
    {
        if (ctx == null) return "";
        var sb = new StringBuilder();
        for (int s = 0; s < BoardSpec.Size; s++)
        {
            var u = ctx.Players[p].Board[s];
            if (u == null) continue;
            int v = exhausted ? (u.Exhausted ? 1 : 0) : u.AttacksThisTurn;
            if (v == 0) continue;                 // 0 = 默认值 ⇒ 不打（空列 = 全员 0 / 未疲劳）
            if (sb.Length > 0) sb.Append(' ');
            sb.Append('P').Append(p).Append('[').Append(s).Append("]=").Append(v);
        }
        return sb.ToString();
    }

    /// <summary>手牌里一张牌的读数键 —— 🔴 **带上它的手牌增益条数**：
    /// 手牌加成（`Give +1 [Attack] to a random troop in your hand`，`GOF100`）**不改卡的定义、也不改棋盘**，
    /// 不加这一格就**整族照不到**（会被误列成候选）。`*N` = 这张牌身上挂了几条 `HandEffect`。</summary>
    static string HandEntry(CardInstance c)
    {
        int n = c.HandBuffOps == null ? 0 : c.HandBuffOps.Count;
        return n == 0 ? c.Card.Id : c.Card.Id + "*" + n;
    }

    static string Board(string[] cells)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < cells.Length; i++) sb.Append('[').Append(cells[i]).Append(']');
        return sb.ToString();
    }

    /// <summary>逐字段比。**只报实得差异**，⛔ 不在这里判对错。
    /// 区域（手牌/牌库/弃牌堆）按 **id 多重集**比 —— 见 <see cref="Snap"/> 的注释。</summary>
    static void Diff(Snap a, Snap b, List<string> into)
    {
        for (int p = 0; p < 2; p++)
            if (a.Energy[p] != b.Energy[p])
                into.Add("⚙ 能量 P" + p + " " + a.Energy[p] + " → " + b.Energy[p]
                         + "（" + (b.Energy[p] - a.Energy[p]) + "）");
        for (int p = 0; p < 2; p++)
            for (int i = 0; i < BoardSpec.Size; i++)
                if (!string.Equals(a.Cells[p][i], b.Cells[p][i], StringComparison.Ordinal))
                {
                    string tag = b.Cells[p][i] == "." ? "☠ 离场"
                               : a.Cells[p][i] == "." ? "✚ 落场/生成"
                               : "✎ 改动";
                    into.Add(tag + " P" + p + "[" + i + "] " + a.Cells[p][i] + " → " + b.Cells[p][i]);
                }
        for (int p = 0; p < 2; p++)
        {
            DiffIds("✋ 手牌 P" + p, a.HandIds[p], b.HandIds[p], into);
            DiffIds("🂠 牌库 P" + p, a.DeckIds[p], b.DeckIds[p], into);
            DiffIds("🗑 弃牌 P" + p, a.DiscIds[p], b.DiscIds[p], into);
            if (a.HandCost[p] != b.HandCost[p])
                into.Add("💰 手牌总价 P" + p + " " + a.HandCost[p] + " → " + b.HandCost[p]
                         + "（" + (b.HandCost[p] - a.HandCost[p]) + "）");
            if (a.Faith[p] != b.Faith[p]) into.Add("🔮 信仰 P" + p + " " + a.Faith[p] + " → " + b.Faith[p]);
            if (a.Spirit[p] != b.Spirit[p]) into.Add("🔮 灵魂石 P" + p + " " + a.Spirit[p] + " → " + b.Spirit[p]);
            if (a.Quest[p] != b.Quest[p]) into.Add("🔮 任务点 P" + p + " " + a.Quest[p] + " → " + b.Quest[p]);
            if (a.Dmg[p] != b.Dmg[p]) into.Add("☠ 伤害累计 P" + p + " " + a.Dmg[p] + " → " + b.Dmg[p]);
            if (a.Troops[p] != b.Troops[p]) into.Add("🎖 出兵数 P" + p + " " + a.Troops[p] + " → " + b.Troops[p]);
        }
    }

    /// <summary>两个 id 清单（排序过）的多重集差，按 id 出「加了/减了各几张」。</summary>
    static void DiffIds(string label, List<string> a, List<string> b, List<string> into)
    {
        var keys = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var k in a) keys.Add(k);
        foreach (var k in b) keys.Add(k);
        foreach (var k in keys)
        {
            int da = a.Count(x => x == k), db = b.Count(x => x == k);
            if (da == db) continue;
            into.Add(label + " " + (db > da ? "+" : "-") + "「" + k + "」×" + Math.Abs(db - da));
        }
    }

    static int FreeSlot(BattleContext ctx, int p)
    {
        // 先挑贴着督军 / 有相邻的那几格（「相邻」族要有活目标），再退到其余格。
        foreach (int s in new[] { 3, 5, 2, 6, 1, 7, 0, 8 })
            if (ctx.Players[p].Board[s] == null) return s;
        return 3;
    }

    /// <summary>🆕 `P3`（2026-10-26）：`preset pick=1` 时给**单位卡**挑一个「玩家点定的目标」。
    ///
    /// <para>**池子 = 产品代码自己的那个**（`RuleCore.PlayTargetPool` —— 拖拽高亮 / `IsLegalPlayTarget`
    /// 问的就是它，⛔ 不另立一套筛）。这里**不传「落位后预览」**：那样池子里会多出**刚打出去的这张卡
    /// 自己**，而 `ta=30` 的语义是「调用方传进来的目标」（⛔ 不是自己），预览那一个不该被选。</para>
    ///
    /// <para>**怎么挑**：池里**优先取已受伤的**（`Health &lt; MaxHealth`）。理由 —— 治疗类
    /// （`Heal N to a friendly unit`）只有在受伤目标上才看得见；对伤害/加成一类**无害**
    /// （效果一样会发生、一样可读）。都不受伤就取第一个。</para>
    ///
    /// <para>⚠️ 池子空 ⇒ 返回 `null`（= 这张卡没有可选目标，走原路径，⛔ 不替它发明一个）。</para></summary>
    static UnitState PickPlayTarget(BattleContext ctx, int p, CardDef card)
    {
        var pool = RuleCore.PlayTargetPool(ctx, p, card);
        if (pool == null || pool.Count == 0) return null;
        UnitState first = null, damaged = null;
        foreach (var u in pool)
        {
            if (u == null || !u.IsAlive) continue;
            if (first == null) first = u;
            if (damaged == null && u.Health < u.MaxHealth) damaged = u;
        }
        return damaged ?? first;
    }

    /// <summary>「要选目标」的战术卡：挑一个**该侧真有单位**的格（先非督军格，没有才用督军格 4）。
    /// ⚠️ 这只是**夹具怎么点**，不是判据 —— 选错时 `CanPlayTactic` 会返回 `ErrNoTargetAvailable`，如实打出来。</summary>
    static int PickSlot(BattleContext ctx, int p, EffectTargetSpec spec)
    {
        int side = spec.Side == "enemy" ? 1 - p : p;
        var b = ctx.Players[side].Board;
        for (int i = 0; i < BoardSpec.Size; i++)
            if (i != BoardSpec.WarlordSlot && b[i] != null) return i;
        return BoardSpec.WarlordSlot;
    }

    static bool FaceHasOps(CardDef c)
    {
        var won = EffectText.WillRunOps(c);
        if (won != null && won.Count > 0) return true;
        var ops = EffectText.Parse(c.Desc, out _, out _);
        return ops != null && ops.Count > 0;
    }

    // ==================================================================
    //  档 —— 「**在本夹具里它应不应该有实得变化**」
    // ==================================================================
    //
    // 🔴 **为什么必须有这一层**：`other == 0` 这个判据**本身不区分**
    //   「该发生而没发生」（= `A1431` 那一类）与「**本夹具产生不了那个事件**」。
    //   实测反例（不加这一层时误列进候选）：`AM14 Autocannon Squad` 的 `Regiment: Gain +1 [Ranged]`
    //   —— `Regiment`（团）**不是部署时触发**，它是**攻击时**触发
    //   （`RuleCore.cs:4027-4031`：`团（Regiment）：和 Mob 成对，差别只在远程`；
    //    消费点 = `FireTriggerAt(ctx, attacker, KeywordTable.Regiment, p, atkSlot)`）⇒
    //   本夹具只「打出一发」、不让单位攻击 ⇒ **它不该有变化**，报成候选就是**误报**。
    //   ⇒ 判据全部**转调引擎的公开面**（`EffectText.Split` / `StripLeadingIcons` /
    //     `CardDef.RoutableTriggers` / `CardDef.HandledByOtherLayer`），⛔ 探针里不另写关键词表。
    //
    // 档（打印顺序 = 可疑度从高到低）：
    //   · **A 打出即结算** —— `tactic` / `defence`。判据：这两类的 `WillRunOps` **就是**
    //     `Parse(Desc)`（见 `willrun` README「四条别推翻的取舍」1）⇒ 打出**就是**触发，
    //     没有任何「等事件」可以解释 → **本档的候选最可疑**。
    //   · **C 部署时该发生** —— `unit`，卡面里存在**不带触发头**的正文段（`Head:` 缺失，或无头的
    //     部署时正文），或头是 `rally`（`Rally:` = 部署时触发）⇒ 出牌那一拍就该有变化。
    //   · **B 等事件 / 等主动用** —— `unit`，每一段正文都挂在一个**别的时机**的头上
    //     （`Strike`/`Regiment`/`Mob`/`Slay`/`Duty`/`Agenda`/`Oath`/`Teleport`/`Ambush`/`Codex`/
    //     `Ecstasy`/`When …` …）⇒ **本夹具产生不了那个事件**，`other == 0` 是**预期的**、
    //     ⛔ 不是缺陷（要测它得另写「打一发 / 让它死 / 让它抽到」那种专属夹具）。
    //   · **H 督军档** —— 另法测（见 <see cref="MeasureHero"/>）。
    //   ⚠️ **付费能力段（`N <货币>:` / `Oath N:`）一律不算「该发生」** —— 它是**主动用**。
    //      判据 = **引擎自己那次 `ParseSegment`**（见 `IsPaidSegment` / ⑥ 自检，`A1599`）。
    //
    // `oldPaidRule` 🆕 `A1599`：**只给自检用** —— `true` = 用**旧那条半截正则**
    //   （`WillRun.IsPaidHead`，只认 `N <货币>:`）重算一遍，好让「旧口径必红」是**机器量出来的**
    //   而不是靠嘴说。⛔ **生产路径一律取缺省 `false`**（判据 = 引擎自己那次 `ParseSegment`）。
    static string Bucket(CardDef c, bool oldPaidRule = false)
    {
        if (c.Type == "hero") return "H 督军档";
        if (c.Type == "tactic" || c.Type == "defence") return "A 打出即结算";
        bool unguarded = false;
        foreach (var raw in EffectText.Split(c.Desc))
        {
            if (string.IsNullOrEmpty(raw)) continue;
            string s = EffectText.StripLeadingIcons(raw.Trim());
            if (string.IsNullOrEmpty(s)) continue;
            int col = s.IndexOf(':');
            string head = col <= 0 ? null : s.Substring(0, col).Trim().ToLowerInvariant();
            // ⚠️ **不是可执行正文段的直接跳过**（`Waystone.` / `Stealth.` / `Armour 1.` 这种裸关键词句）
            //    —— 不加这一道，`ASH77` 的 `Waystone. Stealth. …` 会被判成
            //    「无头 + 该发生」（那两个词根本没有 `:`，`HeadOfBody` 当然查不到），**整整一族误报**。
            string probe = head == null ? s : s.Substring(col + 1).Trim();
            if (string.IsNullOrEmpty(probe)) continue;
            var ops = EffectText.Parse(probe, out _, out _);
            if (ops == null || ops.Count == 0) continue;

            if (head == null)
            {
                // 🔴 **无头正文不能一律当「部署时该发生」** —— 卡面常常把触发头**印成图标**
                //    （`Agenda:` / `Duty:` / `Artifice:` …），而我们的 `desc` 里那个头**没了**。
                //    实测反例：`DA18 Inner Circle Companion` 的 `Gain Vanguard and 2 Quest Points`
                //    （`keywords=[stealth1, agenda1]`）· `BL25 Dark Apostle` 的
                //    `Give a random Dark Pact to each friendly troop`（`artifice1`）·
                //    `AM23 Bombast Field Gun`（`duty1`/`longrange1`）—— 三张都**不是**部署时该发生的。
                //    ⇒ 判据**转调引擎自己的答案**：这段正文落在 `TriggerTexts` 的哪个头下面
                //      （`CollectBareKeywordBody` 收进去的那一条，`key` 就是那个头）。
                string h = WillRun.HeadOfBody(c, s);
                if (h != null && h != "rally") continue;                       // 已认领在别的时机下
                if (CardDef.HandledByOtherLayer(c, raw) != null) continue;     // `When <事件>, …` 那一族
                unguarded = true;                                             // 没人认领 / 认领在 rally 下
                continue;
            }
            if (head == "rally") { unguarded = true; continue; } // `Rally:` = 部署时触发
            if (Array.IndexOf(CardDef.RoutableTriggers, head) >= 0) continue;   // 别的时机
            // 🔴 **`A1599`（2026-10-12）**：付费前缀的判据**转调引擎自己那一次 `ParseSegment`**
            //    （<see cref="IsPaidSegment"/>，与 `WillRun.AddFaceBody` / `WillRun.Classify` ② 同一处）。
            //    改前用的是 `WillRun.IsPaidHead` —— 那条正则只认 `N <货币>:` 那**半截**，
            //    **漏掉引擎同样认的 `Oath N:`**（`EffectText.ReOathPaid`）⇒ 那些段落到「认不出的头」
            //    那一支 ⇒ **整张卡被归进 C 档**（`oldPaidRule` 那一支就是那时的行为，自检里并排量出来）。
            //    ⛔ **别把它读成「誓约能力没收下来」** —— 它们收得到（`CardDef.OathOps`）；
            //       这一处只决定**档位**（C 还是 B），与「收到没有」是两条腿。
            if (oldPaidRule ? WillRun.IsPaidHead(head) : IsPaidSegment(s)) continue;  // 付费能力 = 主动用
            if (CardDef.HandledByOtherLayer(c, raw) != null) continue;          // 别的层接手
            unguarded = true;   // 认不出的头 ⇒ ⛔ 不替它豁免（宁可列进可疑档，不静默放过）
        }
        return unguarded ? "C 部署时该发生" : "B 等事件/等主动用";
    }

    /// <summary>🆕 **`A1599`：这一段是不是「引擎认的付费前缀段」**（`N &lt;货币&gt;:` / `Oath N:` …）。
    ///
    /// 🔴 **判据 = 引擎自己那一次 `ParseSegment`**（`SegResult.PaidCost &gt; 0`），
    ///    ⛔ 不是探针自己写一条头正则 —— 付费用**两条**语法（`EffectText.RePaid` / `ReOathPaid`），
    ///    任何一条手写正则迟早漏掉其中一条（`WillRun.IsPaidHead` 就漏了 `Oath N:`，实测）。
    ///    ⚠️ 传的是**整段原文**（含 `Head:`），与 `WillRun.AddFaceBody` 里那一处逐字同形。
    /// </summary>
    static bool IsPaidSegment(string seg)
    {
        var segr = EffectText.ParseSegment(seg);
        return segr != null && segr.PaidCost > 0;
    }

    // ==================================================================
    //  `A1572`：本夹具【结构上不适用】的两族 —— 可执行的过滤口径
    // ==================================================================
    //
    // 🔴 **为什么要有这一层**：`other == 0` 这个判据问的是「打出去之后局面动没动」，
    //   而**有两族卡在本夹具里压根到不了「打出去」那一步**（不是卡的问题、也不是引擎的问题，
    //   是**夹具的形状**决定的）⇒ 把它们与自然上手那批**混在一起读**就会误报一整族。
    //
    // 两条口径（判据**全部转调引擎的公开面**，⛔ 探针里不另写一份表）：
    //   ① **临时卡（`ephemeral`）** —— 判据 = `CardDef.Has("ephemeral")`（与
    //      `BattleContext.IsEphemeral` 的前半同一处语义，见 `RuleCore.cs:1171`）。
    //      **为什么**：`RuleCore.EndTurn`（`RuleCore.cs:1341`）会调 `SweepEphemeral(ctx, ctx.Active)`
    //      —— 回合结束时把**该方手牌**里的临时卡**移出游戏**（规则书 `:229`「非弃置」）。
    //      本夹具是「**先摆牌库、过两个回合、再对准它打一发**」⇒ 对被测的那张卡来说，
    //      它在**回合 0 结束时就已经不在手牌里了**（实测：98 张全部落在这一档）。
    //      ⇒ 只有「夹具按引擎自己的形状**补手牌**」这条路能接住它。
    //   ② **防御卡（`type == "defence"`）** —— 判据 = `CardDef.Type == "defence"`。
    //      **为什么**：`RuleCore.BuildPlayer`（`RuleCore.cs:506/553`）把 `defence` 从**牌库**里
    //      **分流**出去（`defenceCardOut`），发牌由调用方按**座位**做 —— 只发给**后手**
    //      （`RuleCore.GrantDefenceCard` / `SetDefensiveCard`，见 `RuleCore.cs:448-473` 的说明）。
    //      本夹具的 P0 是**先手** ⇒ 手里永远不会有它（实测：39 张全部落在这一档）。
    //      ⚠️ **补进来之后还有一层不等价**：它被测量时站在**先手**这一侧，
    //         而原版里它只会出现在**后手**手里 ⇒ 这一批的读数「**座位与原版不同**」。
    //
    // 🔴 **这两条口径的用法 = 【标注 ＋ 单列】，⛔ 不是【整族从候选里删掉】** ——
    //    实测（2026-10-11，池 1126 张 · 411 张候选）落在候选里的只有 **3 张临时卡 ＋ 4 张防御卡**，
    //    而逐条看引擎日志，那 7 张**各有别的原因**（3 张是**真缺陷**：`Deploy <二词具名>` 被当兵种筛
    //    ⇒ 一张都筛不到；4 张是**量具盲区**：牌库顶换序 / 牌库减费不在指纹里 / 要「残骸」而夹具没有）。
    //    ⇒ **整族删除会把那 3 张真缺陷一起盖掉**（本仓红线「不许静默失败」＋ 铁律 11「全部都要做」）。
    static string NotApplicable(CardDef c)
    {
        var why = new List<string>();
        if (c.Has("ephemeral")) why.Add("临时卡（`ephemeral`）");
        if (c.Type == "defence") why.Add("防御卡（`defence`，原版只发给后手）");
        return why.Count == 0 ? "" : string.Join(" ＋ ", why);
    }

    /// <summary>`fixtureFit` 列的值（TSV 用，纯 ASCII 好 grep）：空 = 适用 ·
    /// `ephemeral` / `defence` / `ephemeral+defence` = 本夹具对它结构上不适用（见 <see cref="NotApplicable"/>）。</summary>
    static string FitTag(CardDef c)
    {
        var t = new List<string>();
        if (c.Has("ephemeral")) t.Add("ephemeral");
        if (c.Type == "defence") t.Add("defence");
        return string.Join("+", t);
    }

    /// <summary>从引擎自己的事件日志里挑出**最能解释「为什么没变化」**的那一行
    /// （优先「**没生效**」/「空过」/「没有合法目标」/「打不了」—— 引擎自己会说话，⛔ 不用我们猜）。</summary>
    static string WhyLine(Reading r)
    {
        string[] keys = { "没生效", "空过", "没有合法目标", "打不了", "造不出" };
        foreach (var k in keys)
            foreach (var l in r.LogTail)
                if (l.Contains(k)) return l;
        return r.LogTail.Count > 0 ? r.LogTail[r.LogTail.Count - 1] : "（引擎日志里没有说明）";
    }

    // ==================================================================
    //  打印
    // ==================================================================

    static void Print(Reading r)
    {
        var c = r.C;
        Console.WriteLine("=== play " + c.Id + " · " + c.Name + " [" + c.Type + "] ===");
        Console.WriteLine("  卡面（英文）: " + c.Desc);
        Console.WriteLine("  卡面（中文）: " + (string.IsNullOrEmpty(c.DescZh) ? "（这张卡没有中文）" : c.DescZh));
        Console.WriteLine("  印的数值    : cost=" + c.Cost + " attack=" + c.Attack + " ranged=" + c.RangedAttack
                          + " health=" + c.Health + " subtype=" + (c.Subtype ?? "-")
                          + " faction=" + (c.Faction ?? "-") + " rarity=" + (c.Rarity ?? "-"));
        Console.WriteLine("  关键词      : " + (c.Keywords == null || c.Keywords.Count == 0 ? "（无）"
                          : string.Join(", ", c.Keywords.Select(k => k.Key + (k.Value != 0 ? k.Value.ToString() : "")))));
        Console.WriteLine("  怎么打的    : " + r.How);
        Console.WriteLine("  档          : " + r.Bucket + "（见 `play all` 的分档说明；"
                          + "决定 `other=0` 时**该不该当缺陷看**）");
        // 🆕 `A1572`：这一张在本夹具里**结构上适不适用** —— 两族（临时卡 / 防御卡）天然到不了本夹具那一步
        string na = NotApplicable(c);
        Console.WriteLine("  夹具适用性  : " + (na.Length == 0 ? "适用"
                          : "**结构上不适用** —— " + na + "（见 `play all` 的 ③ 节；"
                            + "`other=0` 不能单独当缺陷）"));
        Console.WriteLine();
        if (r.Crash != null)
        {
            Console.WriteLine("  🔴 **真打时抛异常**：" + r.Crash);
            Console.WriteLine("     （这是引擎侧缺陷 —— ⛔ 本工具只报，不改；判决归调度台/引擎侧）");
            return;
        }
        Console.WriteLine("  ── 打出去之前 ──");
        Console.WriteLine(r.Before.Length > 0 ? "     " + r.Before : "     （督军档：没有「打出去」这一步，见上）");
        Console.WriteLine("  ── 打出去之后 ──");
        Console.WriteLine("     " + r.After);
        // 🆕 `B8_AV`（2026-10-26）：进场序读数 —— `P{方}[{格}]={PlayOrder}`，**按号升序**
        //   ⇒ 从左到右就是原版 `allCardsInPlay` 的遍历序（谁先响）。见 `PlayOrderCol`。
        if (r.PlayOrder.Length > 0)
            Console.WriteLine("  进场序(PlayOrder): " + r.PlayOrder
                              + "\n     ⚠️ 按号升序；`-1` = 从没取过号（排末尾）。督军也在里面"
                              + "（`RuleCore.BuildPlayer` 给它发了第一个号），⛔ 别拿它跟 `UnitsByPlayOrder()` 比张数。");
        Console.WriteLine();
        Console.WriteLine("  ── 变化逐条（" + r.Changes.Count + " 条"
                          + (r.HeroMode ? "（= 上面那一趟 **减掉** 下面的空督军对照）"
                                        : "：付费 " + r.Paid + " · 出牌本身 " + r.Mech
                                          + " · 自身落场 " + r.SelfPlace + " · 其它 " + r.Other)
                          + "）──");
        if (r.Changes.Count == 0) Console.WriteLine("     （局面一个字段都没动）");
        foreach (var ch in r.Changes) Console.WriteLine("     " + ch);
        if (r.HeroMode)
        {
            Console.WriteLine("  ── 【空督军】对照局的变化（" + r.ControlChanges.Count + " 条，"
                              + "⛔ 与上面那一栏相减才是这位督军的贡献）──");
            foreach (var ch in r.ControlChanges) Console.WriteLine("     " + ch);
        }
        Console.WriteLine();
        Console.WriteLine("  ── 引擎自己的事件日志（末尾 " + r.LogTail.Count + " 条）—— 🔴「什么都没发生」时先看这里 ──");
        if (r.LogTail.Count == 0) Console.WriteLine("     （一条都没有）");
        foreach (var l in r.LogTail) Console.WriteLine("     " + Clip(l, 150));
        Console.WriteLine();
        Console.WriteLine("  ── 判据（⛔ 判据在**卡面**上，本工具只摆读数、不判对错）──");
        Console.WriteLine("    卡面有没有可执行正文（`desc` 或注册层）: " + (r.FaceHasOps ? "**有**" : "没有"));
        Console.WriteLine("    打出去了吗: " + (r.Played ? "是（rc=0 OK）" : "**否** —— " + r.Note)
                          + (r.Played && r.ExpectedPay >= 0 ? "   （照卡面应付 **" + r.ExpectedPay + "** 能）" : ""));
        // 🆕 `A1572`：这一张是**补进手牌**才测到的 ⇒ 读数带「量具干预」标记（判读时要看得见）
        if (r.Injected)
            Console.WriteLine("    ⚠️ 这一张是**夹具补进手牌**的（不是自然上手）—— 见 `play all` 的 ③ 节："
                              + "临时卡会被回合末的 `SweepEphemeral` 移出游戏、防御卡本来就只发给后手。");
        Console.WriteLine("    实得变化（**除付费 · 出牌本身 · 自身落场**）: " + r.Other + " 条"
                          + (r.Played && r.FaceHasOps && r.Other == 0
                             ? "   ← 🔴 **卡面写着正文、打出去了、局面一个数字都没动** = `A1431` 那一类【结算层失明】的第一候选"
                             : ""));
        Console.WriteLine("    对敌造成的伤害累计: " + r.Dmg
                          + (r.Dmg != 0 ? "（照卡面数）" : ""));
        Console.WriteLine("  ⚠️ 「实得变化 0 条」**不等于缺陷**（可能夹具里没有合法目标 / 效果只在别的时机发生）"
                          + " —— 判决要看卡面，本工具不代替判决。");
    }

    /// <summary>🆕 `A1431` 第三笔：**事件电池**的单卡读数（`play &lt;id&gt;` 打在主档读数之后）。
    /// ⛔ 与 <see cref="Print"/> 一样**只摆读数、不判对错**。</summary>
    static void PrintEvents(CardDef c, EventReading r)
    {
        Console.WriteLine();
        Console.WriteLine("=== play " + c.Id + " · 事件电池（`A1431` 第三笔 · 打出去**之后**那一段）===");
        Console.WriteLine("  电池的步骤/返回码 : " + r.Steps);
        Console.WriteLine("  档                : " + r.Bucket
                          + "   · 主档候选（卡面有正文 · 打出去 · 主档实得 0 条）: " + (r.Cand ? "**是**" : "否"));
        Console.WriteLine("  对照局（空正文替身）起步了吗 : " + (r.CtrlPlayed ? "是" : "**否** —— 净差不可信"));
        Console.WriteLine("  电池局自己的变化条数 = " + r.Raw + " · 对照局 = " + r.Ctrl
                          + " ⇒ **净差 = " + r.Net.Count + " 条**（净伤害 " + r.NetDmg + "）");
        if (r.Crash != null) Console.WriteLine("  🔴 **电池抛异常**：" + r.Crash);
        if (r.Note.Length > 0) Console.WriteLine("  🔴 " + r.Note);
        if (r.Net.Count == 0)
            Console.WriteLine("  ⛔ **净差 0 条 = 电池没抬起这一张的正文** ⇒ 它**仍然没被「事件的结算」验到**。"
                              + "根因 = **" + EcCause(c) + "**（见 `play all` 的 ⑥ 节）。");
        else
            foreach (var ch in r.Net) Console.WriteLine("     " + ch);
    }

    // ==================================================================
    //  全池 sweep
    // ==================================================================
    /// <summary>全池逐卡真打，出一份 TSV + 一段摘要。
    /// ⚠️ 这是 `A1431` 那句「**全池尺子对『解析过、结算错』这一类结构性失明**」的正面回答：
    ///   本仓第一次有一条全池尺子**能看见「打出去之后局面动了没有」**。
    /// ⛔ 输出的是**候选**，判决归调度台/引擎侧（与 `willrun` 同一条纪律）。</summary>
    static int Sweep(List<CardDef> pool, string dumpPath, string pfPath = null, PresetSpec preset = null)
    {
        var sb = new StringBuilder();
        sb.Append("id\tname\ttype\tbucket\twarlord\tplayRc\trcName\tplayed\tfaceOps\tselfPlace\tmech\tother\t"
                  + "expectedPay\tenergyDelta\thandCostDelta\tdmg\tchanges\tcrash\tnote\t"
                  // 🆕 `A1572` 两列（**只加在末尾** —— 前面的列一个字没动）
                  + "fixtureFit\tinjected"
                  // 🆕 `B8_AV`（2026-10-26）：棋盘上的 `PlayOrder`（进场序）—— 同样**只加在末尾**。
                  //   形如 `P0[4]=0 P0[3]=1 …`（按号升序 ⇒ 字面次序 = 谁先响），见 `PlayOrderCol`。
                  //   ⚠️ 它**不进**结算指纹（`FingerprintRow`）⇒ 加这一列**不需要**重钉 `PinnedPlayMd5`。
                  + "\tplayOrder"
                  // 🆕 `P3`（2026-10-26）：`preset pick=1` 实际点定的那个目标（空 = 没走 pick / 目标池空）。
                  //   同样**只加在末尾**、同样**不进**结算指纹。
                  + "\tpickTarget"
                  // 🆕 2026-10-27（`B9_E5ENG`）：计数类两列 —— **只加在末尾**（前面的列一个字没动）。
                  //   形如 `P0[3]=1 P0[5]=2 ‖ P1[3]=1`（`UnitNumCol`；空 = 全员 0 / 未疲劳）。🔴 与 `playOrder` / `pickTarget`
                  //   **相反**：这两列**进**结算指纹（`FingerprintRow`）⇒ 加它们**一定要重钉** `PinnedPlayMd5`。
                  + "\tatkCount\texhausted\n");
        var pfRows = new List<string>();      // 🆕 `A1431` 第二笔：**结算指纹**（逐卡一行，见 `FingerprintRow`）
        var ecRows = new List<string>();      // 🆕 `A1431` 第三笔：**事件电池**（`E|` 前缀那一套，见 `FingerprintERow`）
        var ecReads = new List<EventReading>();   // 电池档的读数（末尾那两节要按档统计）
        // 🆕 `B9_W41S3B`（2026-10-27）：**督军档电池**（`H|` 前缀；只 `preset hero=1` 时有内容）
        var heroRows = new List<string>();
        var heroReads = new List<EventReading>();

        int played = 0, cand = 0, crash = 0, unimpl = 0, cantPlay = 0, heroCnt = 0;
        int naTotal = 0, naInjected = 0;                 // `A1572`：结构上不适用的两族 / 其中靠补手牌的
        var buckets = new Dictionary<string, int>();
        var cands = new List<Reading>();
        var crashes = new List<Reading>();
        var naCands = new List<Reading>();               // 两族里**落进候选**的那些（要单列，⛔ 不静默）

        foreach (var c in pool)
        {
            var r = Measure(pool, c, preset);
            r.Bucket = Bucket(c);
            if (c.Type == "hero") heroCnt++;
            if (r.Crash != null) { crash++; crashes.Add(r); }
            else if (r.Played) played++;
            else
            {
                cantPlay++;
                var k = CodeName(r.Rc);
                buckets[k] = (buckets.TryGetValue(k, out var n) ? n : 0) + 1;
                if (r.Rc == RuleCodes.ErrUnimplemented) unimpl++;
            }
            if (r.Played && r.FaceHasOps && r.Other == 0 && c.Type != "hero")
            {
                cand++; cands.Add(r);
                // `A1572`：落在候选里的那两族**单列**（见 `NotApplicable` 的那段长注释）
                if (FitTag(c).Length > 0) naCands.Add(r);
            }
            if (FitTag(c).Length > 0)
            {
                naTotal++;
                if (r.Injected) naInjected++;
            }

            pfRows.Add(FingerprintRow(c, r));      // 🆕 `A1431` 第二笔

            // 🆕 `A1431` 第三笔：**事件电池**（只对 `unit`，见本区块头注）
            if (c.Type == "unit")
            {
                var er = MeasureEvents(pool, c, preset);
                er.Cand = r.Played && r.FaceHasOps && r.Other == 0;   // 见 `EventReading.Cand`
                ecRows.Add(FingerprintERow(c, er));
                ecReads.Add(er);
            }
            // 🆕 `B9_W41S3B`（2026-10-27）：**督军档电池**（只在 `preset hero=1` 时跑）
            //   ⛔ 不是 `E|` 行、也不进 `ecReads`（覆盖面那一节的分母是 unit 档，混进来会灌水）。
            //   ⚠️ 它**不进** `play check` 的基线（`BothFingerprintRows` 那条路不调它）。
            else if (c.Type == "hero" && preset != null && preset.HeroBattery > 0)
            {
                var hr = MeasureHeroBattery(pool, c, preset);
                heroRows.Add(FingerprintERow(c, hr, HeroPrefix));
                heroReads.Add(hr);
            }

            sb.Append(c.Id).Append('\t').Append(c.Name).Append('\t').Append(c.Type)
              .Append('\t').Append(r.Bucket)
              .Append('\t').Append(r.HeroMode ? "1" : "0")
              .Append('\t').Append(r.Rc)
              .Append('\t').Append(r.HeroMode ? "hero" : CodeName(r.Rc))
              .Append('\t').Append(r.Played ? "1" : "0")
              .Append('\t').Append(r.FaceHasOps ? "1" : "0")
              .Append('\t').Append(r.SelfPlace)
              .Append('\t').Append(r.Mech)
              .Append('\t').Append(r.Other)
              .Append('\t').Append(r.ExpectedPay)
              .Append('\t').Append(r.EnergyDelta)
              .Append('\t').Append(r.HandCostDelta)
              .Append('\t').Append(r.Dmg)
              .Append('\t').Append(r.Changes.Count)
              .Append('\t').Append(r.Crash == null ? "" : r.Crash.Replace('\t', ' ').Replace('\n', ' '))
              .Append('\t').Append(r.Note.Replace('\t', ' ').Replace('\n', ' '))
              // 🆕 `A1572`：本夹具对它**结构上不适用**吗（空 = 适用；`ephemeral` / `defence` / 两者）
              //    + 这一张是不是**补进手牌**才测到的（`1` = 量具干预过）。⛔ 只加列、不改前面的列。
              .Append('\t').Append(FitTag(c))
              .Append('\t').Append(r.Injected ? "1" : "0")
              // 🆕 `B8_AV`：`PlayOrder` 列（见 `PlayOrderCol`；空 = 这一档没有 ctx / 督军档）
              .Append('\t').Append(r.PlayOrder)
              // 🆕 `P3`：`pickTarget` 列（`preset pick=1` 实际点定的目标；空 = 没走 pick / 目标池空）
              .Append('\t').Append(r.PickTarget.Replace('\t', ' ').Replace('\n', ' '))
              // 🆕 2026-10-27（`B9_E5ENG`）：计数两列（末尾；见 `Snap.ActCnt` / `UnitNumCol`）
              .Append('\t').Append(r.AtkCount)
              .Append('\t').Append(r.Exh)
              .Append('\n');
        }

        Console.WriteLine("=== play all · 逐卡真打（不跑 Unity）· 池 " + pool.Count + " 张 ===");
        // 🆕 `P3`（2026-10-26）：带 `preset` 的全池扫描 = **另一次**夹具，⛔ 不是上面那套基准读数。
        //   ⛔ 不许静默（否则你会以为全池读数是在同一套夹具上跑的）。
        if (preset != null && preset.Any)
            Console.WriteLine("  🔴 本次是 **带 `preset` 的另一次全池扫描**（`dead=" + preset.Dead
                              + "` · `pick=" + preset.Pick + "` · `enemyhand=" + preset.EnemyHand
                              + "` · `chipenemy=" + preset.ChipEnemy + "`）—— ⛔ 它不是基准读数，"
                              + "别拿它去对 `play check` 的基线。");
        Console.WriteLine("  夹具（全池同一份，所以读数能横向比）：");
        Console.WriteLine("    · 双方督军各 1（P0 的督军 = 夹具 `FIXWarlord`，**除非被测卡本身是 hero**）");
        Console.WriteLine("    · 双方各 3 张固定部队：我方 `FIX_A` 步 2/4 · `FIX_B` 载具 3/5 · `FIX_C` 兽 1/4，"
                          + "敌方 `FIX_E1/E2/E3` 步/载具/兽 各 6 血");
        Console.WriteLine("      落在 **3 / 6 / 7** 三格（贴着督军格 4、且有相邻对 6-7）；"
                          + "兵种**故意分散**（步/载具/兽）⇒「friendly Vehicle」「your Beasts」那族找得到目标");
        Console.WriteLine("    · 手牌 " + FixtureHand + " 张 = 3 张摆场部队 + **被测那一张**；"
                          + "能 " + FixtureEnergy + "/" + FixtureMaxEnergy
                          + "（上限比起始高 ⇒ `Refill all your Energy` 看得见）；`seed=0`、不洗牌、换牌关");
        Console.WriteLine("    · 摆完场后**我方三张各扣 " + FixtureChip + " 血**（走引擎 `ApplyDamage`）"
                          + " ⇒ `Heal …` 那一族才看得见（满血治疗 = 什么都没发生）");
        Console.WriteLine("  量的是什么：把被测卡**真打一发**，比打前/打后的**局面指纹**"
                          + "（能 · 棋盘每格 攻/远/甲/血+关键词 · 手牌/牌库/弃牌堆**逐张 id** · 手牌总价 · 伤害累计 · 出兵数）。");
        Console.WriteLine("  ⛔ 本工具**不判对错** —— 判据在**卡面**上（`desc`/`descZh`/PnP 卡图），"
                          + "这里摆的是**实得数字**。");
        Console.WriteLine();
        Console.WriteLine("  --- ① 总账 ---");
        Console.WriteLine("    真打成功（rc=0）        " + played + " 张");
        Console.WriteLine("    打不出去                " + cantPlay + " 张"
                          + (cantPlay > 0 ? "（分因：" + string.Join(" · ",
                              buckets.OrderByDescending(kv => kv.Value).Select(kv => kv.Key + " " + kv.Value)) + "）" : ""));
        Console.WriteLine("      ⚠️ 其中 `ErrUnimplemented` **" + unimpl + " 张** = 卡面上写着正文、"
                          + "而 `CanPlayTactic` 就把它挡回去了（`A1431` 的硬失败档）");
        Console.WriteLine("    引擎抛异常              " + crash + " 张" + (crash > 0 ? "  ← 🔴 **必须查**" : ""));
        Console.WriteLine("    督军档（另法测）        " + heroCnt + " 张");
        Console.WriteLine();
        Console.WriteLine("  --- ② 候选（卡面写着正文 · 打得出去 · 局面一个数字都没动 = " + cand + " 张），**按可疑度分档** ---");
        Console.WriteLine("     🔴 **档 A（最可疑，先看它）** —— `tactic`/`defence`：打出**就是**触发，"
                          + "没有「等事件」可以解释");
        Console.WriteLine("     ⚠️ 档 C —— `unit`：卡面里有**不带触发头**的正文（或 `Rally:`）⇒ 部署那一拍就该有变化");
        Console.WriteLine("     ⚪ 档 B —— `unit`：每一句都挂在**别的时机**的头上（`Strike`/`Regiment`/`Duty`/`When …` …）"
                          + "⇒ 本夹具**产生不了那个事件**，`other=0` 是**预期的**，⛔ 不是缺陷");
        Console.WriteLine();
        foreach (var bucket in new[] { "A 打出即结算", "C 部署时该发生", "B 等事件/等主动用" })
        {
            var g = cands.Where(r => r.Bucket == bucket).ToList();
            if (g.Count == 0) continue;
            Console.WriteLine("     ── " + bucket + " · " + g.Count + " 张"
                              + (bucket.StartsWith("A") ? "  ← 🔴 **这一档最可疑**" : "") + " ──");
            foreach (var r in g.Take(bucket.StartsWith("A") ? 120 : 60))
                Console.WriteLine("       " + Pad(r.C.Id, 24) + Pad(r.C.Name, 30) + "[" + r.C.Type + "]  rc="
                                  + CodeName(r.Rc) + "  `" + Clip(r.C.Desc, 66) + "`");
            if (g.Count > (bucket.StartsWith("A") ? 120 : 60))
                Console.WriteLine("       …（还有 " + (g.Count - (bucket.StartsWith("A") ? 120 : 60)) + " 张，看 TSV）");
        }
        Console.WriteLine("     ⛔ **候选 ≠ 缺陷清单** —— 里面还有一类**量具本身的盲区**（本夹具测不到）："
                          + "① 需要专属事件的（档 B 已分开）；② 只改**后续回合**的（`For the rest of this battle, at the start of your turn …`）；"
                          + "③ 需要**特定兵种/关键词**才成立的（本夹具只有 步/载具/兽 三兵种、`Infantry` 无关键词）；"
                          + "④ 需要**上一句承接**的（`A1428` 跨句承接那族）。⇒ **逐张对着卡面看**，判决归调度台/引擎侧。");

        // ------------------------------------------------------------------
        //  `A1572`：本夹具**结构上不适用**的两族 —— 可执行的过滤口径 ＋ 它们里落进候选的逐条
        // ------------------------------------------------------------------
        Console.WriteLine();
        Console.WriteLine("  --- ③ 本夹具【结构上不适用】的两族（`A1572`：**量具口径**，⛔ 不是引擎缺陷）---");
        Console.WriteLine("     口径 ① 临时卡（`ephemeral`，判据 `CardDef.Has(\"ephemeral\")`）："
                          + "`RuleCore.EndTurn` 的 `SweepEphemeral`（`RuleCore.cs:1341`）会在回合末把"
                          + "**该方手牌**里的临时卡**移出游戏**（规则书 `:229`「非弃置」）"
                          + "⇒ 任何「先摆牌库、等它自然上手」的夹具对它天然无效。");
        Console.WriteLine("     口径 ② 防御卡（`type == \"defence\"`，判据 `CardDef.Type`）："
                          + "`RuleCore.BuildPlayer`（`RuleCore.cs:506/553`）把 `defence` 从**牌库**分流出去、"
                          + "只发给**后手** ⇒ 本夹具的 P0（先手）手里永远不会有它。");
        Console.WriteLine("     ⇒ 读法：这两族的行，`other == 0` **不能单独当缺陷**；"
                          + "本夹具靠【按引擎形状补手牌】接住它们（`injected=1` 那一列就是这么来的）。");
        Console.WriteLine("     🔴 **⛔ 但这两族【不整族删除】**（`A1572` 的账文原写「必须从候选里排除」——"
                          + "实测那句**与本夹具的实际形状不符**）：落在候选里的只有下面这 "
                          + naCands.Count + " 张，而它们**各有别的原因**（多为量具盲区，也有**真缺陷**）"
                          + "⇒ 整族删除会连真缺陷一起盖掉（本仓红线「不许静默失败」＋ 铁律 11）。");
        Console.WriteLine("     对照口径（实测）：两族共 " + naTotal + " 张 · 其中 **" + naInjected
                          + " 张**是靠补手牌才测到的（余下 " + (naTotal - naInjected)
                          + " 张是自然上手/落场，不是这一层的问题）");
        Console.WriteLine("     口径自证：两族的行与 `injected=1` 的行**互为同一集合**（今天实测 "
                          + naTotal + " = " + naInjected + "）—— 哪天不再相等，说明引擎的补牌/分流"
                          + "改了形状（该回来重读这一节，⛔ 不是自动豁免）。");
        if (naCands.Count == 0)
            Console.WriteLine("     ✅ 两族**没有一张**落进候选 —— 本夹具对它们已经够用（⛔ 不是「免检」："
                              + "它们仍然只是**结构上不适用**的那一层提示）。");
        else
        {
            Console.WriteLine("     ── 两族里**落进候选**的 " + naCands.Count
                              + " 张（逐条列出 · 附引擎自己那句最能解释「为什么没变化」的话）──");
            foreach (var r in naCands)
                Console.WriteLine("       " + Pad(r.C.Id, 24) + Pad(r.C.Name, 30) + "[" + FitTag(r.C) + "]  "
                                  + "`" + Clip(r.C.Desc, 54) + "`\n"
                                  + "          ⇒ " + Clip(WhyLine(r), 150));
        }
        Console.WriteLine("     ⚠️ 这一节是**提示**、不是判决：真要销掉某一张，判据仍在**卡面**上"
                          + "（`desc`/`descZh`/PnP 成品卡图），⛔ 别拿本节当「已解释、可跳过」。");

        if (crash > 0)
        {
            Console.WriteLine();
            Console.WriteLine("  --- ④ 🔴 引擎抛异常（" + crash + " 张）---");
            foreach (var r in crashes.Take(60))
                Console.WriteLine("       " + r.C.Id + "  " + r.C.Name + "  [" + r.C.Type + "]  " + r.Crash);
        }

        if (!string.IsNullOrEmpty(dumpPath))
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(dumpPath));
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(dumpPath, sb.ToString());
            Console.WriteLine();
            Console.WriteLine("  wrote " + dumpPath + " lines=" + (pool.Count + 1));
        }

        // ------------------------------------------------------------------
        //  🆕 `A1431` 第二笔：**结算指纹** —— 全池读数的**规范化摘要**（`play check` 对拍的那一份）
        // ------------------------------------------------------------------
        Console.WriteLine();
        Console.WriteLine("  --- ⑤ 结算指纹（`A1431` 第二笔 · `play check` 拿它对拍）---");
        Console.WriteLine("     主档（打出去那一拍） " + FingerprintMd5(pfRows) + "   （" + pfRows.Count + " 张）");
        Console.WriteLine("     事件电池（`E|` 那一套）" + FingerprintMd5(ecRows) + "   （" + ecRows.Count + " 张 unit）");
        Console.WriteLine("     合起来（= 基线文件的非 `#` 行 · 哨兵钉的就是这一串） "
                          + FingerprintMd5(pfRows.Concat(ecRows)) + "   （" + (pfRows.Count + ecRows.Count) + " 行）");
        Console.WriteLine("     含什么：`id|type|warlord|playRc|played|faceOps|自落场|机械|实得|应付|能量Δ|手牌价Δ|伤害|"
                          + "变化条数|crash|档|**变化逐条全文**`；电池档见 " + string.Join("/", PfECols) + "。");
        Console.WriteLine("     ⛔ 不含什么：**卡名**（改名的产物，本仓已改过 6 张）· `note`（口径散文，电池档除「对照局没起步」外）·"
                          + "引擎日志（措辞会变）—— 这三样进来会**天天红**，把「红了不用管」训练成习惯。");
        if (!string.IsNullOrEmpty(pfPath))
        {
            var pdir = Path.GetDirectoryName(Path.GetFullPath(pfPath));
            if (!string.IsNullOrEmpty(pdir)) Directory.CreateDirectory(pdir);
            File.WriteAllText(pfPath, FingerprintFileText(pfRows, ecRows, heroRows));
            Console.WriteLine("     wrote " + pfPath + " lines="
                              + (pfRows.Count + ecRows.Count + heroRows.Count + 1));
        }
        else
        {
            Console.WriteLine("     ⚠️ 没写指纹文件 —— 要留一份「被确认的状态」⇒ `play all pf <out.txt>`"
                              + "（写完之后**手改** `PlayProbe.PinnedPlayMd5` = 上面那串，见 `play check`）。");
        }

        // ------------------------------------------------------------------
        //  🆕 `A1431` 第三笔：**事件电池**的覆盖面 —— 「档 B」那一档今天到底被验到多少
        // ------------------------------------------------------------------
        Console.WriteLine();
        Console.WriteLine("  --- ⑥ 🔴 事件电池的覆盖面（`A1431` 第三笔 · 治「档 B」的结构性失明）---");
        Console.WriteLine("     量的是什么：打出去**之后**再走一段事件电池（两个来回 / 抽一张 / 主动用 / 攻击一发 / 打死一个），"
                          + "读数 = **电池局 − 空正文对照局**（见 `EventReading.Net`）。");
        Console.WriteLine("     ⛔ 它**不改**上面主档的任何一格；只把新读数**追加**在指纹文件的第二段。");
        foreach (var bucket in new[] { "B 等事件/等主动用", "C 部署时该发生" })
        {
            // ⚠️ 分母 = **候选**（`Cand`：卡面有正文 · 打出去了 · 主档实得 0 条）——
            //    那一批才是「主档看着它、却看不见」的卡；⛔ 拿整个 unit 档当分母是灌水。
            var g = ecReads.Where(r => r.Bucket == bucket && r.Cand).ToList();
            if (g.Count == 0) continue;
            var cov = g.Where(r => r.Net.Count > 0).ToList();
            Console.WriteLine("     ── " + bucket + " · **候选 " + g.Count + " 张**"
                              + "（该档 unit 全档 " + ecReads.Count(r => r.Bucket == bucket) + " 张）· "
                              + "**电池净差 > 0 的 " + cov.Count + " 张**"
                              + (bucket.StartsWith("B") ? "（=「档 B」今天被事件验到的张数）" : "") + " ──");
            var zero = g.Where(r => r.Net.Count == 0).ToList();
            if (zero.Count > 0)
            {
                Console.WriteLine("        🔴 净差 = 0 的 " + zero.Count + " 张 —— 主档那一拍看不见、电池这一趟也没抬起来"
                                  + " ⇒ **这一档里它们仍然没被「事件的结算」验过**（⛔ 不是「没有差异」，"
                                  + "是**没验**；逐张对着卡面看）");
                // 🔴 **按根因分**（判据 = 引擎自己的公开访问器，见 `EcCause`）——
                //    ⛔ 不写「总之没验到」那种没信息量的话。
                //    ⚠️ `A1943`（2026-10-14）之后前两格**只表示「有 Sp/Faith 能力」**（夹具已给资源：
                //    Faith 12 / Spirit 5）⇒ 它们**不再声称根因**；真判根因要逐条比代价/阈值（见 `EcCause` 头注）。
                foreach (var grp in zero.GroupBy(r => EcCause(r.C)).OrderByDescending(x => x.Count()))
                    Console.WriteLine("          · " + Pad(grp.Key, 30) + " = " + grp.Count() + " 张");
                foreach (var r in zero.Take(60))
                    Console.WriteLine("          " + Pad(r.C.Id, 24) + Pad(r.C.Name, 30)
                                      + " steps=" + Clip(r.Steps, 90));
                if (zero.Count > 60) Console.WriteLine("          …（还有 " + (zero.Count - 60) + " 张，看指纹文件）");
            }
        }
        var ecBad = ecReads.Where(r => r.Note.Length > 0 || r.Crash != null).ToList();
        Console.WriteLine("     对照局没起步 / 电池抛异常的行 = **" + ecBad.Count + " 张**"
                          + (ecBad.Count > 0 ? "  ← 🔴 出声（逐条）" : "（✅ 没有）"));
        foreach (var r in ecBad.Take(30))
            Console.WriteLine("       " + Pad(r.C.Id, 24) + " " + Clip(r.Crash ?? r.Note, 120));

        Console.WriteLine();
        Console.WriteLine("  判据：**抛异常 = 0 张** ⇒ rc=0；否则 rc=1。");
        // 🆕 `B9_W41S3B`（2026-10-27）：**督军档电池**的覆盖面（只 `preset hero=1` 时有内容；⛔ 不许静默）
        if (preset != null && preset.HeroBattery > 0)
        {
            Console.WriteLine();
            Console.WriteLine("  --- ⑦ 🆕 督军档电池（`preset hero=1`：被测卡当 P0 督军 → 打天赋 → 电池步 → 主动能力）---");
            Console.WriteLine("     督军 **" + heroReads.Count + " 张** · **净差 > 0 的 "
                              + heroReads.Count(r => r.Net.Count > 0) + " 张** · 抛异常 "
                              + heroReads.Count(r => r.Crash != null) + " 张 · 对照局没起步 "
                              + heroReads.Count(r => r.Note.Length > 0) + " 张");
            foreach (var r in heroReads.Where(r => r.Net.Count == 0).Take(60))
                Console.WriteLine("       净差 0：" + Pad(r.C.Id, 10) + Pad(r.C.Name, 28)
                                  + " steps=" + Clip(r.Steps, 110));
            foreach (var r in heroReads.Where(r => r.Crash != null || r.Note.Length > 0).Take(20))
                Console.WriteLine("       🔴 " + Pad(r.C.Id, 10) + Clip(r.Crash ?? r.Note, 130));
        }
        Console.WriteLine("  ⚠️「候选」与「打不出去」**都不计入 rc** —— 它们要**对着卡面**逐张判，"
                          + "本工具不代替判决（⛔ 也不许静默：两栏都在上面逐条打了出来）。");

        // ------------------------------------------------------------------
        //  🆕 `A1599`：`Bucket` 的【付费前缀判据】自检（**计入 rc**）
        // ------------------------------------------------------------------
        Console.WriteLine();
        int bucketFail = BucketSelfCheck(pool);
        Console.WriteLine("  ⇒ 判据自检（`A1599`）" + (bucketFail == 0 ? "全过 ✅" : "红 " + bucketFail + " 条 🔴")
                          + " —— 它也计入 rc。");

        // ------------------------------------------------------------------
        //  🆕 `A1431` 第三笔：**事件电池**的自检（**计入 rc**）—— 灭自证
        // ------------------------------------------------------------------
        Console.WriteLine();
        int ebFail = EventBatterySelfCheck(pool);
        Console.WriteLine("  ⇒ 判据自检（`A1431` 第三笔 · 事件电池）"
                          + (ebFail == 0 ? "全过 ✅" : "红 " + ebFail + " 条 🔴") + " —— 它也计入 rc。");

        // 🔴 **电池档的「出声」也计入 rc** —— 一行「对照局没起步 / 电池抛异常」= **那一张没验到**，
        //    正是本笔要治的那件事（看着绿、实际没验）⇒ 一律红，⛔ 不许只印一行字。
        if (ecBad.Count > 0) ebFail++;

        return (crash > 0 || bucketFail > 0 || ebFail > 0) ? 1 : 0;
    }

    // ==================================================================
    //  🆕 `A1599`：`Bucket` 付费前缀判据的自检
    // ==================================================================

    /// <summary>🆕 **`A1599`**：`Bucket` 的付费前缀判据自检（返回**红的条数**）。
    ///
    /// 🔴 **为什么非要有它**：`Bucket` 原来用 `WillRun.IsPaidHead`（正则 `^\d+\s*[\[\(]?\s*[a-z☀]`）
    ///   当「这段是不是付费能力」的判据，而**引擎认的付费前缀有两条**（`EffectText.RePaid` /
    ///   `ReOathPaid`）⇒ 那条正则**漏掉 `Oath N:`**（`oath 2` 以字母开头，第一位 `\d` 就不匹配）
    ///   ⇒ 这些段落进「认不出的头」那一支 ⇒ **整张卡被归进 C 档**（=「部署那一拍就该有变化」）。
    ///   ⚠️ **它不是「誓约能力没收下来」** —— 那 28 张（池侧现读）的 `Oath N:` 收得到
    ///   （`CardDef.OathOps`）；这一条只治**档位**（C 还是 B），⛔ 别混成那 18/28 张的假警报。
    ///
    /// **断言 A —— 旧口径必红的那一条**
    ///   夹具 `Oath 2: …`（**一个字都不改**，只让头是引擎认的 `Oath N:`）：
    ///   新口径 ⇒ **B**（付费能力 = 主动用，本夹具产生不了）；旧口径 ⇒ **C**（认不出这个头）。
    ///   ⇒ **旧口径下这条断言结构上不可能绿。**
    /// **断言 B —— 反面对照（覆盖面没丢）**
    ///   `2 ☀: …` / `2 [Spirit Stone]: …`：**新旧两个口径都必须给 B** ——
    ///   挡住「把付费判定整个删掉」那种改法（那种改法会让 A 变绿、B 变红）。
    /// **断言 C —— 池侧（钉子）**
    ///   ① 池里**存在**「引擎认付费、旧正则认不出」的段（= 这条断言今天不是空话）；
    ///   ② **新旧口径给出不同档位的卡 ≥ 1**（= 改动真的改了读数；⛔ 一旦有人把它改回去，这里立刻 0 条 ⇒ 红）；
    ///   ③ 每一张「档位变了」的卡，**都**含至少一条这样的段（= 变化**由这一处引起**，不是别的原因顺带红）。
    ///
    /// ⚠️ 夹具卡**在内存里现造**（`new CardDef(...)`，⛔ 不进卡池、⛔ 不碰 `MyGame/Assets/**`）。
    /// </summary>
    static int BucketSelfCheck(List<CardDef> pool)
    {
        Console.WriteLine("  --- ⑦ 🔴 `Bucket` 付费前缀判据自检（`A1599`：判据 = **引擎自己那次 `ParseSegment`**）---");
        int fail = 0;
        Func<string, string, CardDef> fx = (id, desc) =>
            new CardDef(id, id, "unit", desc, "common", "Fixture", 1, 1, 1, 0, null, subtype: "Troop");

        // ---- 断言 A：`Oath N:` —— 引擎认、旧正则认不出 ⇒ 新 B / 旧 C ----
        foreach (var desc in new[] { "Oath 2: Deal 3 damage to an enemy troop",
                                     "Oath 1: Give Armour 1 to a friendly troop" })
        {
            var f = fx("FX-P1", desc);
            string nw = Bucket(f, false), od = Bucket(f, true);
            int paid = IsPaidSegment(desc) ? 1 : 0;
            bool oldSays = WillRun.IsPaidHead(desc.Substring(0, desc.IndexOf(':')).Trim().ToLowerInvariant());
            bool ok = nw == "B 等事件/等主动用" && od == "C 部署时该发生";
            Console.WriteLine("    " + (ok ? "✅" : "🔴") + " A 「" + desc + "」");
            Console.WriteLine("        新口径（引擎 `ParseSegment`）⇒ **" + nw + "**  ·  旧口径（`IsPaidHead`）⇒ **" + od + "**");
            Console.WriteLine("        引擎说的：`ParseSegment.PaidCost` = " + paid + "（>0 = 付费前缀段）"
                              + "  ·  `WillRun.IsPaidHead(\"" + desc.Substring(0, desc.IndexOf(':')).Trim()
                              + "\")` = " + (oldSays ? "true" : "**false**（← 那条正则漏掉了它）"));
            if (!ok)
            {
                fail++;
                Console.WriteLine("        🔴 期望 **新 = B / 旧 = C** —— 旧口径下这里只能给 C（元凶 = 那条半截正则）；"
                                  + "若新口径也给 C，说明 `Bucket` 又拿形状正则当付费判据了。");
            }
        }

        // ---- 断言 B：反面 —— 老的两条写法【两个口径都必须给 B】（⛔ 覆盖面不许丢）----
        foreach (var desc in new[] { "2 ☀: Deal 3 damage to an enemy troop",
                                     "2 [Spirit Stone]: Deal 3 damage to an enemy troop" })
        {
            var f = fx("FX-P2", desc);
            string nw = Bucket(f, false), od = Bucket(f, true);
            bool ok = nw == "B 等事件/等主动用" && od == "B 等事件/等主动用";
            Console.WriteLine("    " + (ok ? "✅" : "🔴") + " B 「" + desc + "」⇒ 新 **" + nw + "** / 旧 **" + od
                              + "**（**两个口径都必须是 B**）");
            if (!ok) fail++;
        }

        // ---- 断言 C：池侧 —— ① 这类段真的存在 ② 档位真的变了 ③ 变化由它引起 ----
        var changed = new List<string>();
        int segFailingOld = 0;
        foreach (var c in pool)
        {
            if (c == null) continue;
            bool hasSuchSeg = false;
            foreach (var raw in EffectText.Split(c.Desc))
            {
                if (string.IsNullOrEmpty(raw)) continue;
                string s = EffectText.StripLeadingIcons(raw.Trim());
                if (string.IsNullOrEmpty(s)) continue;
                int col = s.IndexOf(':');
                if (col <= 0) continue;
                string head = s.Substring(0, col).Trim().ToLowerInvariant();
                if (IsPaidSegment(s) && !WillRun.IsPaidHead(head)) { hasSuchSeg = true; break; }
            }
            if (hasSuchSeg) segFailingOld++;
            string nw = Bucket(c, false), od = Bucket(c, true);
            if (nw != od)
                changed.Add(c.Id + " [" + c.Type + "] " + od + " → " + nw + (hasSuchSeg ? "" : "  ← 🔴 无此类段"));
        }
        bool okC = segFailingOld > 0 && changed.Count > 0 && !changed.Any(x => x.Contains("← 🔴 无此类段"));
        Console.WriteLine("    " + (okC ? "✅" : "🔴") + " C 池侧（" + pool.Count + " 张）：");
        Console.WriteLine("        ① 含「引擎认付费、`IsPaidHead` 认不出」的段的卡 = **" + segFailingOld
                          + " 张**（必须 > 0：否则本断言是空话）");
        Console.WriteLine("        ② 新旧口径**档位不同**的卡 = **" + changed.Count
                          + " 张**（必须 > 0：⛔ 一旦有人把判据改回那条半截正则，这里就是 0 ⇒ 红）");
        Console.WriteLine("        ③ 每一张都**含**这样的段（否则「档位变了」另有原因，要回来重读）");
        foreach (var x in changed.Take(40)) Console.WriteLine("           · " + x);
        if (changed.Count > 40) Console.WriteLine("           …（还有 " + (changed.Count - 40) + " 张）");
        if (!okC)
        {
            fail++;
            Console.WriteLine("        🔴 期望三条同时成立（见上）。最要紧的一条：⚠️ **别把 `Oath N:` 那批读成"
                              + "「誓约能力没收下来」** —— 它们收得到（`CardDef.OathOps`），这里只关**档位**。");
        }
        Console.WriteLine("    ⇒ 自检 **" + (fail == 0 ? "全过 ✅" : "红 " + fail + " 条 🔴") + "**");
        return fail;
    }

    // ==================================================================
    //  🆕 `A1431` 第三笔：**事件电池**的自检（灭自证）
    // ==================================================================

    /// <summary>🆕 **`A1431` 第三笔**：事件电池的判据自检（返回**红的条数**）。
    ///
    /// 🔴 **它在防的那件事**：「电池看着跑过了、其实**抬不起那个事件**」—— 那正是本笔要治的病
    ///   （一个**看着绿、实际没验**的档）。只断「净差 > 0」**挡不住**它：
    ///   电池里那两个来回**本来就会**产生一串与卡无关的变化（两边抽牌 / 能量刷新 / 回合计数），
    ///   只要对照局算错一点，「净差 > 0」就能**靠噪声**成立。
    ///
    /// **断言 = 结构上不可能靠噪声满足的那一条**：每一对比的是**同一张夹具卡、只把一个数字改掉**
    ///   （`5 → 9`）。若电池**真抬起了**那个事件 ⇒ 两条的净差**必然不同**（伤害/加值不一样了）；
    ///   若电池**压根没抬起**它 ⇒ 两条都是**空的净差**、必然**相同** ⇒ 红。
    ///   ⚠️ 噪声对两条**是一样的**（同种子、同夹具、同事件序列）⇒ 它**帮不了**这一条断言。
    ///
    /// **覆盖面**：每一对都指名一个**事件**（`Strike` / `Regiment` / `Duty` / `Pray` / `Oath` /
    ///   回合开始 / 回合结束 / `Slay`）—— 这些正是「档 B」的成员挂着的那些头。
    ///   ⇒ 哪一条红了，就等于说「**这个事件电池没抬起来**」，档 B 里挂那个头的卡**仍然没被验到**。
    ///   🆕 **`A1965`（2026-10-19）**：新 6 步（`selfChip` / `tactic` / `enemyTactic` / `troop2` /
    ///   `friendDies` / `collect`）与三个夹具变体（V1 Codex / V2 Teleport / V3 条件 Rally）**各补一对**
    ///   —— 设计 §五·3 要求「扩到每一个新步骤」：少一对 = 那一步坏掉之后电池只会**静默抬不起来**。
    ///
    /// ⚠️ 夹具卡**在内存里现造**（`new CardDef(...)`，⛔ 不进卡池、⛔ 不碰 `MyGame/Assets/**`）。
    /// </summary>
    static int EventBatterySelfCheck(List<CardDef> pool)
    {
        Console.WriteLine("  --- ⑧ 🔴 事件电池判据自检（`A1431` 第三笔 · **灭自证**：同卡改一个数，净差必须不同）---");
        int fail = 0;
        // 夹具单位：`3/2/20`（近战 3 · 远程 2 · **血 20**），费用 1（能落场、能打两下）。
        // 🔴 **血必须够厚**（20）—— 实测踩过：血 4 的夹具在**第一发（近战）**就被反击打死
        //    ⇒ 第二发（远程）根本没单位可打 ⇒ `Regiment:` 那一对两条净差**都是 0**（假红）。
        Func<string, string, string[], CardDef> fx = (id, desc, kw) =>
            new CardDef(id, id, "unit", desc, "common", "Fixture", 1, 3, 20, 2, kw, subtype: "Troop");

        // 每一对 =（这一对在验哪个事件 · 真的正文 · 只把一个数改掉的正文 · 关键词）
        var pairs = new[]
        {
            new object[] { "Strike（攻击那一发）",   "Strike: Deal 5 damage to an enemy troop",                 "Strike: Deal 9 damage to an enemy troop",                 new string[0] },
            new object[] { "Regiment（攻击那一发）", "Regiment: Gain +2 [Ranged]",                              "Regiment: Gain +7 [Ranged]",                              new string[0] },
            new object[] { "Mob（攻击那一发）",      "Mob: Gain +1 [Melee]",                                    "Mob: Gain +6 [Melee]",                                    new string[0] },
            new object[] { "Duty（主动用）",         "Duty: Deal 3 damage to an enemy troop",                   "Duty: Deal 8 damage to an enemy troop",                   new[] { "Duty" } },
            new object[] { "Pray（主动用）",         "Pray: Deal 3 damage to an enemy troop",                   "Pray: Deal 8 damage to an enemy troop",                   new[] { "Pray" } },
            new object[] { "Oath（誓约）",           "Oath 2: Deal 3 damage to an enemy troop",                 "Oath 2: Deal 7 damage to an enemy troop",                 new[] { "Oath" } },
            new object[] { "回合开始",               "At the start of your turn, deal 3 damage to an enemy troop", "At the start of your turn, deal 8 damage to an enemy troop", new string[0] },
            new object[] { "回合结束",               "At the end of your turn, deal 3 damage to a random enemy",  "At the end of your turn, deal 8 damage to a random enemy",  new string[0] },
            // ============================================================
            //  🆕 `A1965`（2026-10-19）：**新 6 步 ＋ enemyTactic ＋ 三变体**各一对。
            //   🔴 设计 §五·3 要求「扩到每一个新步骤」—— 缺一条 = 那个新步骤**没被灭自证**，
            //      它坏掉之后电池只会**静默变成抬不起来**（正是本笔要治的那一类）。
            //   ⚠️ 两个数必须真的不同（`3 → 8`），且**只有这一个数**变 —— 否则「净差不同」可能来自别处。
            // ============================================================
            new object[] { "受伤（selfChip）",       "When this unit is damaged, deal 3 damage to an enemy troop",           "When this unit is damaged, deal 8 damage to an enemy troop",           new string[0] },
            new object[] { "Artifice（tactic）",     "Artifice: Deal 3 damage to an enemy troop",                            "Artifice: Deal 8 damage to an enemy troop",                            new[] { "Artifice" } },
            new object[] { "对手打战术（enemyTactic）", "When your opponent plays a Stratagem, deal 3 damage to an enemy troop", "When your opponent plays a Stratagem, deal 8 damage to an enemy troop", new string[0] },
            new object[] { "Uprising（troop2）",     "Uprising: Deal 3 damage to an enemy troop",                            "Uprising: Deal 8 damage to an enemy troop",                            new[] { "Uprising" } },
            new object[] { "友军阵亡（friendDies）",  "When a friendly troop dies, deal 3 damage to an enemy troop",          "When a friendly troop dies, deal 8 damage to an enemy troop",          new string[0] },
            // 🆕 `A1965` 续工：第 ⑥ 步改成**真击杀路**之后补的这一对 —— 验「**敌方**阵亡」那一族。
            new object[] { "敌方阵亡（⑥ 真击杀）",   "When an enemy dies, deal 3 damage to an enemy troop",                   "When an enemy dies, deal 8 damage to an enemy troop",                   new string[0] },
            // ⚠️ **`收路标石` 这一对是看门狗 —— 它盯过一条真引擎缺陷，现已修好（2026-10-19 同日）。**
            //   本会话实测（第五十五会话 `E8`）：`collect` 那一步 **rc=0、残骸已形成**
            //   （`steps` 里 `collect=0@1/rem+way`），但两条正文的净差**都是 0**。
            //   根因当时是：`RuleCore.CollectWaystone` 只往 `ctx.Signals` 塞两条表现层事件、
            //   **没有 `BroadcastWhen(WhenEventKind.GainSpirit, …)`** ⇒「`When you collect a Spirit Stone, …`」
            //   这条监听器**从来不会响**（与 `EffectResolver.DoGain` 那段「Signals 不能顶替广播」同一个坑）。
            //   ✅ **引擎线同日已补上**（现读 `RuleEngine/Core/RuleCore.cs:8492` 就是那一行）；
            //   本对随即转绿（实测 12 条 vs 12 条、伤害值 3 vs 8）⇒ **留着它当回归看门狗**：
            //   谁把那一行拿掉，这一对会立刻红。
            new object[] { "收路标石（collect）",     "When you collect a Spirit Stone, deal 3 damage to an enemy troop",     "When you collect a Spirit Stone, deal 8 damage to an enemy troop",     new string[0] },
            new object[] { "Slay（攻击击杀）",        "Slay: Deal 3 damage to an enemy troop",                                "Slay: Deal 8 damage to an enemy troop",                                new[] { "Slay" } },
            // ============================================================
            //  🆕 `A1965` 三个**夹具变体**各一对（V1 Codex / V2 Teleport / V3 条件 Rally）。
            //   ⚠️ 这三条**不是**新步骤，是**同一套步骤换起始状态** —— 验的是「装具换一档，读数必须跟着变」。
            // ============================================================
            new object[] { "V1 Codex（能量=卡费）",   "Codex: Deal 3 damage to an enemy troop",                               "Codex: Deal 8 damage to an enemy troop",                               new[] { "Codex" } },
            new object[] { "V2 Teleport（牌库抽到）", "Teleport: Deal 3 damage to an enemy troop",                            "Teleport: Deal 8 damage to an enemy troop",                            new[] { "Teleport" } },
            new object[] { "V3 条件 Rally（空棋盘）", "Rally: If you control no other troops, deal 3 damage to an enemy troop", "Rally: If you control no other troops, deal 8 damage to an enemy troop", new string[0] },
        };

        foreach (var p in pairs)
        {
            string what = (string)p[0], a = (string)p[1], b = (string)p[2];
            var kw = (string[])p[3];
            var ra = MeasureEvents(pool, fx("FX-EBA", a, kw));
            var rb = MeasureEvents(pool, fx("FX-EBA", b, kw));
            string na = Norm(ra), nb = Norm(rb);
            bool diff = na != nb;
            Console.WriteLine("    " + (diff ? "✅" : "🔴") + " " + Pad(what, 22)
                              + " 净差 " + ra.Net.Count + " 条 vs " + rb.Net.Count + " 条"
                              + (ra.Net.Count == 0 && rb.Net.Count == 0 ? "（**两条都是空的**）" : ""));
            Console.WriteLine("        真：`" + Clip(a, 70) + "`  ⇒ " + Clip(na, 150));
            Console.WriteLine("        改：`" + Clip(b, 70) + "`  ⇒ " + Clip(nb, 150));
            Console.WriteLine("        steps " + Clip(ra.Steps, 220)
                              + (ra.Note.Length > 0 ? "   ⚠️ " + ra.Note : ""));
            if (!diff)
            {
                fail++;
                Console.WriteLine("        🔴 期望两条净差**不同** —— 相同 = **电池没抬起这个事件**"
                                  + "（要么 `steps` 里那一步返回码不是 0，要么那个头根本没被 `WillRun` 认领）"
                                  + " ⇒ 「档 B」里挂这个头的卡**仍然没被验到**。");
            }
        }
        Console.WriteLine("    ⇒ 自检 **" + (fail == 0 ? "全过 ✅" : "红 " + fail + " 条 🔴")
                          + "**（红的每一条 = 那个事件电池没抬起来）");
        return fail;
    }

    /// <summary>归一化净差（给自检**比两条**用）：只留**条数**与**排序后的逐条原文**，
    /// ⛔ 不带卡 id 之类「本来就不同」的东西 —— 否则这一条断言会**恒真**（自证）。</summary>
    static string Norm(EventReading r)
    {
        if (r.Crash != null) return "crash:" + r.Crash;
        return r.Net.Count + "|" + string.Join(";", r.Net.OrderBy(x => x, StringComparer.Ordinal));
    }

    // ==================================================================
    //  🆕 `A1431` 第二笔：结算层对拍（`play check`）
    // ==================================================================
    //
    // 🔴 **修的是哪一条**：`A1431` 的账文 = 「全池尺子对『**解析过、结算错**』结构性失明」。
    //   第一笔（`E8`，2026-10-11）把 `play` 立了起来 —— 它**能看见**「打出去之后局面动了没有」，
    //   但它**只摆读数、永不红**（`rc` 只认「崩了 / `ErrUnimplemented`」）。
    //   ⇒ 到今天仍有两条**结构性**的洞：
    //     ① **读数不可比**：「这张卡昨天实得 3 条变化、今天实得 0 条」**没有任何东西记得住** ——
    //        结算层的**静默漂移**（`A1388`/`A1407`/`A1426`/`A1428` 那一族**修完又坏回去**）
    //        谁都不会知道；411 张候选逐张对着卡面看**是人的活**，机器这一侧**没有留痕**。
    //     ② **同一个洞的第二层**：`check` 那边「**真回归 ＋ 重生成基线 ⇒ 静默变绿**」，
    //        在**结算层**一模一样地成立（`play` 若也拿一份「基线」对拍，那份基线同样**一条命令就能重写**）。
    //
    // ⇒ 做法（`A1382` 那个形状，一层一层搬下来）：
    //   · **逐卡结算指纹**（<see cref="FingerprintRow"/>）：把 `play all` 的读数压成一行**只含数值**的摘要
    //     （含**变化逐条全文** —— 那是「数字动成什么」），全池一份 ⇒ md5。
    //   · `play check [<pf.txt>]`：① 与**基线文件**逐卡比（出「**哪张卡、哪一格**变了」）；
    //     ② **哨兵**：基线文件的内容 md5 必须等于**源码里的 `PinnedPlayMd5`**
    //     ⇒ `play all pf` 重生成基线**抹不掉它**（重钉只能**手改源码** = 显式确认）。
    //   🆕 **`A1431` 第三笔**：对拍现在是**两套**（①-a 主档 · ①-b **事件电池**，见上面那一整块），
    //     外加 **①-c「电池覆盖了谁」** —— 那一节把「**净差 = 0**」的候选**逐条列出并分根因**，
    //     ⛔ 它治的就是本笔的起因：**一个看着绿、实际没验的档**（主档的 `other=0` 原来在
    //     「档 B」上是**常量**，它坏掉也不会有任何一行变红）。
    //
    // ⚠️ 与 `check` 的**已知差别（如实标着）**：结算读数**依赖卡池内容**（改一张卡的 `desc`
    //   就会改它的读数）⇒ 卡池一改，本条**就会红**（⛔ **不像**哨兵 ⑤ 那样「此时不判」——
    //   那正是 `A1382` 假阴的来源）。**这是有意的**：红了先跑 `play check` 看逐卡差异（全池 ≈5 秒）。
    // ⛔ **它仍不判对错**（判据在**卡面**上）—— 它判的是「**结算结果有没有离开被确认的那一版**」。
    // ⚠️ 它**不进任何 Unity 自检**（不跑 Unity、≈5 秒），也**不该进**（判据在卡面上，机器判不了对错）。
    // ==================================================================

    /// <summary>🆕 钉住的**结算指纹**（= 基线文件里那些**非 `#` 行**的规范化 md5，见 <see cref="FingerprintMd5"/>）。
    ///
    /// **为什么必须有它**（`A1431` 第二笔的②）：`play check` 的判据之一是「与**基线文件**比」，
    /// 而基线文件**同一个工具就能重写**（`play all pf <out>`）⇒ 光靠它 = **自证**。
    /// ⇒ 参照物取**源码里的常量** ⇒ 重生成基线**抹不掉**（与 `Program.PinnedParseMd5` 同一个形状）。
    ///
    /// 🔴 **重钉（⛔ 没有「一条命令重钉」）**：`play check` 会把该抄的**整行**打出来，**手改这一个常量**。
    ///   **重钉前必须先确认这批变化是对的**（对着卡面 / 反编译 / 真机逐张看过）——
    ///   为了「把红抹平」而重钉 = **正是它在抓的那件事**。
    ///
    /// ⚠️ **它现在钉的值** → **见下面那一行常量**（⛔ 别在这里再抄一份 —— 两处迟早不一致）。
    /// 🔴 **2026-10-13 就地订正（铁律 5）**：本段原来写「它现在钉的值（原样交代）：`7175ec95…`
    ///   = 2026-10-12（第二十一会话 · `E5`）当次现读 …… 那时 **411 张**候选还没逐张对过卡面」——
    ///   **那一句已经不成立了**（常量早被第二十三会话重钉过，`411` 现读也是 **407**）。
    ///   错因 = 重钉时**只改了常量、没回头改这段说明**（同一个坑：重写正本最容易漏「夹在里面的那半句」）。
    ///   ⇒ 现在的规矩：**值只写在常量那一行**，本段只交代「为什么必须有它」与「重钉纪律」；
    ///   每次重钉的**归因**写在常量上方的注释块里（那里是**历史**，允许留着）。
    /// ⚠️ **它钉的永远是【当时那一版】，⛔ 不是「当时就对」** —— 逐卡候选要对着卡面判，机器判不了。
    ///   ⛔ **不许**为了让它变绿而重跑 `play all pf` —— 那只把洞开回来。
    /// </summary>
    // 🔴 **2026-10-12 第二十三会话：有意重钉** —— 依据是逐卡差异**逐条归因完毕**，⛔ 不是把红抹平。
    //   ① `GSC_Jammed_Communications` 的 `faceOps 0 → 1` = **本会话 `W1`（`A1686`）** 给前缀形 `When <事件>, …`
    //      产出的 `whenclause` 标记 op（**该卡因此从「空表」变「收到一条」**，正是那笔要的）——
    //      已由 `RuleEngineTest.Run` **5246 / 5246 全过**确认（日志 `_tmp_view/s23_ruleengine2.log`）。
    //   ② `SOR47` / `SOR54` / `SOR_Daemonbreaker` 的 `other` / `energyDelta` / `changesFull` = **上一会话 `A1605`**
    //      （信仰前缀 8 种写法统一成 `☀`；那三张原来的 `[Energy]` 被认成能量 ⇒ 现在按信仰计）—— 已确认的改动。
    //   ③ `TAU15` + 16 张 `UM*` 的 `bucket C → B` = **上一会话 `A1599`（`W7`）** 改 `IsPaidSegment` 的
    //      直接后果（它自己的报告就写着「16 张档位真变了」）。
    //   ⚠️ **以上 21 张与本会话的 `W2` 结算改动无关** —— 它自己用 8 张逐张真打 A/B 证明过（逐字相同）。
    //   ⚠️ 重钉只能**手改源码**（⛔ 没有自动口子），判据与纪律 → `README.md` 的「改探针输出格式三步」。
    // 🔴 **2026-10-13 第二十四会话：有意重钉（`A1431` 第三笔 · 事件电池）** ——
    //   依据是 **主档逐行【逐字节未变】**，⛔ 不是把红抹平。**机器验过的证据**：
    //     · 重钉前 `git status`（本仓纪律，见 CLAUDE.md 铁律 13·6）：`ruleprobe/PlayProbe.cs`（**我**在改）·
    //       `ruleprobe/WillRun.cs`（**另一路写手 `A1694` 在飞**）· `RuleEngine/Core/CardDef.cs`（**第三路**）。
    //       后两份现读都是**纯文档/自检**改动（`git diff` 逐条看过：`CardDef.cs` 只动 XML doc
    //       里的三个计数；`WillRun.cs` 加的是 `⑨ Source` 自检与 dump 的一列）⇒ 不改结算语义。
    //     · **主档 md5 重钉后仍是 `83655a105f8eeb8111eab9f0fa479fc7`** = 与上一版**逐字符相同**
    //       （旧基线 1126 行 vs 新基线 1126 行，集合差 **0 / 0**）⇒ 这两路的在飞改动**没有被烘进来**。
    //     · 新增的只有 **586 行 `E|` 行**（`type=unit` 的事件电池）⇒ `git diff --numstat` =
    //       **592 / 0**（592 = 586 行 ＋ 6 行 `#` 注释/分段头）—— **纯追加，一行都没改**。
    //   ⚠️ 前面那份「`7175ec95…` / 第二十三会话重钉」的归因记录**保留在 git 历史里**，⛔ 不在这里抄第二份。
    //   ⚠️ 它钉的仍是**当时那一版**、⛔ **不是「当时就对」**：主档那 407 张候选**还没逐张对过卡面**；
    //     而电池那 586 行里有 **189 张候选的净差 = 0**（**没被验到**，逐条列在 `play check` 的 ①-c）。
    //
    // 🔴 **2026-10-14：有意重钉（`A1943` · 夹具的起始【派系资源】）** ——
    //   改的是**量具**，⛔ 不是把红抹平。**理由**：`BuildFixture` 原来**只给能量**（`SetEnergy`），
    //   `Faith` / `SpiritStones` / `QuestPoints` **从头到尾没人写过** ⇒ 恒为 `int` 默认 **0** ⇒
    //   全池 `N [Spirit Stone]: …`（15 张）与 `N ☀: …`（6 张）那两族的**能力前缀付不起**
    //   ⇒ 它们在**主档那一拍就已经结算不了**（`play check` ①-c 里那 21 张的根因）。
    //   ⇒ 现在 `BuildFixture` / `HeroPass` 的 **4 处 `SetEnergy` 各配了一次 `SetFaction`**
    //      （Faith **12** / Spirit **5** / Quest **0**）。⚠️ **取值是【我们挑的】、⛔ 不是原版的做法**
    //      （原版开局 Faith / QuestPoints **本来就是 0**，三格里只有灵魂石是真缺口）——
    //      逐条依据与那句免责 → `FixtureFaith` 上面那一整段注释。
    //
    //   **这一版钉住的读数**（2026-10-14 现跑）：
    //     · `play check` **rc=0**（①-a / ①-b 逐卡全等 · ② 哨兵过）；
    //     · **①-c 净差 = 0 由 175 降到 154**（分母 382 → 356 · 净差 > 0 由 207 → 202）——
    //       原来那 21 张（15 灵魂石 + 6 信仰）**全部转成 `net > 0`**，根因分组里那两格**一张都不剩**。
    //
    //   **机器验过的证据（夹具改动那一份，是【隔离】量出来的）**：
    //     · 改之前先把现算读数固化了一份（同一棵树 · `play all pf`）⇒ 改前 / 改后**逐行比**：
    //       变化 **89 行** · **新增 0 · 消失 0**；89 行**全部**落在 **`ASH*`（灵族 · 灵魂石）与
    //       `SOR*`（修女会 · 信仰）**两个阵营 ⇒ **其余阵营一行都没动**（= 只由夹具资源引起，无连带）。
    //     · 主档 md5 `888869e68be7df5c347854b8ed3b6de0` → **`fc6da398465b770c0fb21e347f2195a1`**；
    //       电池 md5 `08a1d44b5639f71785ad29db9bb2f229` → **`a601642ba4b942d4cfef78f0273573fe`**。
    //     · 重钉前 `git status -sb`（本仓纪律 · 铁律 13·6）：只有 `ruleprobe/PlayProbe.cs`（**我**在改）·
    //       两张**文档**正本 · 以及**另一路写手在飞**的 `CardPresentation/Editor/BattleScene.cs`。
    //       🔴 最后那份**进不了本探针的编译**：`ruleprobe.csproj` 关掉了 `EnableDefaultCompileItems`，
    //       只显式列 `工具/ruleprobe/*.cs` ＋ `MyGame/Assets/RuleEngine/Core/*.cs`
    //       ⇒ `CardPresentation/` 下的改动**改不成这个 md5**（现核 csproj 的 `<Compile Include>` 清单）。
    //
    //   🔴 **这一版【另外】吞掉了一批【预先就存在】的漂移 —— 如实记在这里，⛔ 别读成「本账治好的」**：
    //     重钉前 `play check` **就已经是 rc=1**：**①-a 773 变 · 增 193 · 消 193** ·
    //     **①-b 345 变 · 增 179 · 消 179**（那是 `A1943` 之前就在的、**与本次夹具改动无关**）。
    //     台账（`资料/任务_卡组卡牌战斗.md` 的 `A1943` 行）把它归给 **`A1758`（卡牌 id 整族换原版真值）
    //     ＋ 第二十五~二十九会话的引擎改动**；🔴 **本账【没有】逐条归因过那 1118 行**（只隔离出上面那 89 行）。
    //     ⇒ 「整体重生成 ＋ 重钉」是**台账明写的顺序**，但它**同时把这批一起钉住了**。
    //   🔴 **剩下那 154 张也不是本账治好的**：根因是「**电池抬不到那个事件**」，属 **`A1965`**
    //     （事件电池那一轮）—— ⛔ 本账不治，⛔ 也别把这次重钉读成「它已经验过了」。
    //
    // 🔴 **重钉（⛔ 没有「一条命令重钉」）**：`play check` 会把该抄的**整行**打出来，**手改这一个常量**。
    //   **重钉前必须先确认这批变化是对的**（对着卡面 / 反编译 / 真机逐张看过）——
    //   为了「把红抹平」而重钉 = **正是它在抓的那件事**。
    // 🔴 **2026-10-19（`A2137`）：有意重钉** —— K06（`3251df0`）重生成基线时**只重钉了 `Program.cs`
    //   那三条指纹**（`PinnedPoolMd5`/`PinnedBaselineMd5`/`PinnedParseMd5`），**漏了这里**
    //   ⇒ ② 哨兵从那天起一直红（基线 `2754c07f…` ≠ 钉住 `76ced8a6…`）。本轮把缺口补上。
    //   ⚠️ **不是为抹红而重钉**：97 个变化行（主档 83 ＋ 电池 14）**已逐项定性**，
    //   分桶与逐条证据 → `资料/普查产出_1018/DIAG_A2137_playcheck.md` ＋ `W_A2137_基线重钉.md`：
    //     · **β 真行为缺陷 = 0 条**。硬证据：用**当前代码 ＋ K06 卡池**跑 `play all pf`，
    //       合集 md5 与旧基线 `2754c07f…` **逐字节一致** ⇒ 同输入零回归。
    //     · **α 基线过期 16 行**（本波 `W_卡牌线_数据侧2` 的 12 张 `keywords` 回卡面：主档 10 个新增
    //       红点 ＋ 电池 6 行；`TL74`/`UM87` 与 γ 血量**同行叠加**）。判据 = PnP 卡图 ＋
    //       `_还原效果文字.md` 转录 ＋ SO `CardTraits` 三票同向。
    //     · **γ 数据漂移 63 行**（`4995622` 的 SO 数值真值覆盖 56 处：49 health ＋ 6 cost ＋ 1 attack，
    //       另电池 2 行；`579bcad` W28 官方改名连带：4 个督军「手牌总价」行 ＋ `E|GOF92`/`E|GOF94`）。
    //     · **δ 工具前提 1 行**（`GOF48` 随机部署：`CreatePool.SortByName` 原来按**卡名**排序，
    //       W28 改名 ⇒ 同一下标抽到别的卡）。本轮**已修**：排序键 `Name → Id`（`CreatePool.cs`）。
    //       修完的**全池连带**一并吸收 —— 另 9 张主档（`ASH44/EC48/GOF107/GSC52/SAU50/TL53/TL72/
    //       UM118/UM75`）＋ 电池 4 行（`E|DA36/EC28/EC31/GOF84`），全部是「按池随机」的卡换了确定性结果。
    //       ⚠️ 这是**有意**的：以后改卡名不再移动随机候选池。
    //     · **另 4 行**来自同期**已关账**的 `W_DarkPact_无费用.md`（`RuleCodes.ErrNotPlayable=20`：
    //       4 张 `Dark Pact of *` 的 `cost = -1` ⇒ `playRc 0 → 20`、`played 1 → 0`）。
    //       那是**另一笔已定的修正**，重生成时按盘上代码一并烘入 —— 复核本基线前先确认它仍在工作树里。
    //   本版读数：**1713 行**（主档 1127 ＋ 电池 586）· 规范 md5 **`07aba22133b70af4fe609d37b00a88bf`**；
    //   同一条 `play all pf` 连跑两次**逐字节一致**（`_tmp_view/a2137/` 留证）· `play check` rc=0。
    // 🔴 **2026-10-19（第五十六会话 · `A1973` 收口重钉）** —— 与 `Program.PinnedPoolMd5` 同一批、同一份归因：
    //    主档 15 张 = `A1571` autoPlay（`AM58` `BL55` `SW57`）· `A2142` Talent 桥（`GOF77` `GOF87` `SW1`）·
    //    `A2143` 敌方手牌（`GOF93`，E12）· `A2001`/E9 数值来源（`GSC35` `GSC59` `SOR14`）·
    //    choose 注入（`BL38`）· 卡面数据（`SW38`）· E8/E9 同批引擎改动（`TL37` `TL49` `TL8`，
    //    E8b `§5·2` 逐张列过）；**电池 586 行全变** = `A1965` 新增 6 步（E8，格式扩展）。
    //    🔴 **本会话另补两个探针盲区**（同批重钉，⛔ 不是行为漂移）：
    //      · `SW22`：电池新增 `softAtk`（`chip2` 之前、敌人满血时的一发**非致命**攻击，
    //        且挡住「被反击打死」）⇒ `Strike … survives` 那一族第一次验得到（`SW22` 净差 0 → 3）；
    //      · `EC17`：夹具督军阵营 `P0/P1` → **被测卡的真实阵营**（原版「按施放者阵营造牌」读
    //        `Warlord.Card.Faction`）⇒ 按阵营造牌那一族第一次造得出牌（`EC17` 净差 0 → 1）。
    //    重生成后 `play check` 现读「当次现算 == 基线文件」、①-a/①-b 差异 0。
    // 🔴 **2026-10-20（第五十七会话 · 工具线 `PLAY-1`）有意重钉** —— 与 `Program.PinnedPoolMd5` 同一批；
    //   主档 6 张 ＋ 电池 3 张，逐条归因见 `资料/普查产出_1019/W57_引擎线_T07工具收口.md` §四：
    //     · `SOR44` / `f104d4c2`（Righteous Repugnance）= `8f77ff7a` 的 `CostKindOf`/`MentionsFaith`
    //       把 `5/4 ☀:` 的费用种类从 energy 修成 faith（信仰前缀不再扣能量；旧读数 = 旧 bug）；
    //     · `DA42` / `DA38` = `8f77ff7a` 的任务点阈值开始扣余数（3 点到阈值立即扣掉 ⇒ 末态 0→0）；
    //     · `DA44` = 同提交的派生 RNG 流（`QuestInsertSeed`/`QuestAux.InsertRng`）⇒ filler 序号位移、
    //       语义未变（只是 RNG 流位置变了）；
    //     · `ASH34`（主档＋电池）= 本会话 W57 的 `Talent:` 解析修复（不再白送 `Wrath of Khaine`，
    //       天赋由 `RuleCore.SpawnTalents` 回合开始造）；
    //     · `ASH28`（电池）= 本会话 W57 修掉「收获灵魂石 → 卡级广播 → 自己再触发自己」的无限递归
    //       （旧 `5 → 22` 是被 `MaxEffectChain=8` 截断的错值，现 `5 → 8` 才对）。
    //   ⛔ **没有一条是「今天改坏了」**：前 5 张是 `8f77ff7a` 之后基线过期没重钉，后 2 张是 W57 的修复。
    //   重生成 `play all pf` 连跑两次逐字节一致（718,793 B / md5 `9024dbb2…`；新增 0 / 消失 0），
    //   重生成后 `play check` 现读 ①-a/①-b 差异 0 · ② 哨兵过。
    // 🔴 **2026-10-21（第六十一会话 · 卡牌线执行代理 `W61`）有意重钉** —— **逐类归因过，⛔ 不是「红了就重钉」**。
    //   ⚠️ 本笔的**归因方法**值得留：把 `5f211be6` 那一版 `RuleEngine/Core/*.cs` 原样编进一份**影子探针**
    //     （`dotnet build` 在临时目录，不动仓库），拿它跑 `play check` ⇒ **与旧基线 `08d51f6d…` 逐卡全等**
    //     （①-a/①-b 差异 0 · ② 哨兵过）⇒ **旧基线 = 旧引擎的状态**，之后每一处漂移都能二分到具体改动。
    //   · **1051 张主档 `changesFull` ＋ 586 张电池的最大头 = 夹具不再「假进加时」**（一个根因）：
    //     旧实现按「后手方 `MaxEnergy >= 10`」判加时（用户 2026-09-17 口径），而夹具 `SetEnergy`
    //     把 `MaxEnergy` 写成 **30** ⇒ **第 2 回合就进加时、每回合多抽 1 张**（旧读数里那张多出来的
    //     垫牌、`filler` 序号偏 1、`手牌总价` 起点差 1 全是它）。`W58-TURN-A` 按原版
    //     `BattleManager._NextTurn_d__395__MoveNext.c:191-207` 改回**全局回合数**
    //     （`ctx.Turn >= overtimeTurn`，见 `W58_回合流程_回合开始守卫.md:179`）⇒ 夹具不再假进加时。
    //     **二分实证**：影子探针**只改这一行** ⇒ 主档差异 1051 → **8** 张、电池差异 586 → **245** 张。
    //   · **主档余下 8 张**（逐张对上文档，读数逐字比过）：
    //     `ASH66` / `BL30` / `TAU34` = 「`When you deploy …` 主体不给自己」修复
    //       （`W58_战斗结算_其余广播顺序.md:25,197-199`；`TAU34` 落地血 9 → 7、`ASH66` 不再自加 `shuriken1`）；
    //     `DA61` = `Gain N Energy` 改「抬上限、不补当前」（`W58_回合流程_加时能量手牌上限.md:94`
    //       的新读数 `能量 P0 20 → 17（-3，只有卡费）` **逐字一致**）；
    //     `SW53` = 猎杀标记删 `killer` 闸（`W58_审查修复_B1.md:15`；新增 P0 督军 -1 / P1 督军 +1 两行）；
    //     `Helspear Assault` / `'Uge Choppa` / `Ferocious Rage` = `GivePayload` D1 修复
    //       （`W58_战斗结算_slay修复.md:13,25-27`；`Blast 2`/`+1 fist`/`+2 attack` 三段改前**静默丢掉**、
    //       改后回来了 —— 这三张卡**改前是「打出去什么都不发生」**）。
    //   · **电池余下 245 张**：**244 张的差异只落在 `V3` 变体**（`PassVariant.Solo` = 先把三张夹具部队
    //     打死再打被测卡），而变化的列全是 `kill`/`friendDies`/`collect`/`atkM`/`atkR2`
    //     —— 正是 `W58` **死亡链重做**的作用域（`SLAYFIX` 改按死亡入队 FIFO · `INTERLEAVE` 逐死者交错 ·
    //     哨戒击杀记 Slay · 猎杀标记删 killer 闸；见 `W58_战斗结算_死亡触发顺序D4D5.md` /
    //     `W58_战斗结算_反噬Slay交错.md` / `W58_审查修复_B1.md`）。**没有一条落在主档那一拍**。
    //   ⛔ **没有一条是「为了把红抹平」**：影子探针（旧引擎）＋ 只回滚那一行加时判据 ⇒ 主档只剩上面 8 张，
    //     8 张全部能在 W58/W59 的报告里按卡名找到「改前 → 改后」的原文读数。
    //   重生成 `play all pf` 连跑两次逐字节一致（**755,372 B**；新增 0 / 消失 0），行数 **1713 不变**
    //   （主档 1127 ＋ 电池 586）；重生成后 `play check` **rc=0**（①-a/①-b 差异 0 · ② 哨兵过）。
    // 🔴 **2026-10-25（第七十五会话 · 引擎线执行代理 `A` · 本笔 `A1`/`A2`）有意重钉** —— **4 行，逐张对过卡面**：
    //   本批**只改了一处引擎代码**（`EffectResolver.DoForceAttack`：`forceattack` 的 `targetCriteria`
    //   按 `chosenAbility` 归侧 —— 见那个函数 ① 上面那段判据），全池**只有 `EC46` 走那条新分支**
    //   （全池仅它同时点名攻击者与受害者）⇒ 它从「筛错池、4→0 空过」回到基线那一版（**这一行本来就该是它**，
    //   所以它**没有**出现在下面 4 行里）。剩下 4 行**不是本批引入的** —— 它们是 2026-10-24 `W2`/`W25`
    //   把 v25 的 `abilityTargetCriteria`（原版 `TargetCriteria` 其余 36 格）接进目标池之后，
    //   **探针第一次看得见筛子**造成的正确收敛（旧基线是「不筛」那一版录的）：
    //     · `AM41`（主档）`Give Flank to all your Infantry troops` —— criteria `friendlyUnits/kw=5(Infantry)`
    //       ⇒ 只该给 `FIX_A`（Infantry）；旧基线给 3 个（含载具 / 兽）⇒ 旧值是**宽了**。
    //     · `E|EC23` `…to another random friendly troop` —— `includeSelf=0` + `maxTargets=1`
    //       ⇒ 只给 1 个「别人」；旧基线给了 3 个（含自己）。
    //     · `E|UM13` `…to your Infantry troops` —— `kw=5` ⇒ 只给 `FIX_A`；旧基线给 3 个。
    //     · `E|UM22` `…to your other troops` —— `includeSelf=0` ⇒ 自己不再吃那一口（下游战斗读数连带变）。
    //   判据出处：原版字段 → `d:/2/Warpforge_code/Scripts/Assembly-CSharp/TargetCriteria.cs`
    //   ＋ `Keyword.cs`（`Infantry = 5`）＋ `TargetSelectMethod.cs`（`highestAttack = 1`）；
    //   卡面 → `cards_engine.json` 的 `desc`/`descZh`。**⛔ 没有一条是「为了让 check 变绿」**：
    //   本笔只动引擎那一处（typecheck 0 error），4 行的期望值全部来自**卡面文字**而不是我们的实现。
    //   新增 0 / 消失 0 · 行数 **1713 不变**（主档 1127 ＋ 电池 586）· 重生成后 `play check` rc=0。
    //  🔴 **2026-10-25（执行代理 `E6` · 引擎线「13 行接闸」）有意重钉** —— 主对话裁定允许重钉这一颗。
    //    归因（**不是把红抹平**）：本批修好了一处**主语解析缺陷** —— `WhenEvents.Subjects.Parse` 原把
    //    `warlord` 当**卡名**（`CreatePool.IsKindWord("warlord")` 为假 ⇒ 落到 `c.Name = "warlord"`），
    //    而没有任何一张卡叫 `warlord` ⇒ `BL8 Noise Marine` 的
    //    `When your Warlord receives damage, gain a ❄ Dark Pact of Excess` **监听器一次都不响**（静默）。
    //    修法 = 与 `CardCriteria.FromTarget` / `CardCriteria.WarlordOnly` 早就采用的口径统一。
    //    ⇒ 现算里 `BL8` 真的能拿到 `{darkpact1}`（主档 `selfPlace 1→0 / other 0→1`、电池 `net 0→14`），
    //      **与卡面文字逐字一致**；逐卡 diff 只有 `BL8` 与 `E|BL8` 两行（1713 行里 2 行）。
    //    ⛔ **另外三颗指纹原样不动**：`PinnedPoolMd5`（卡池没改）· `PinnedParseMd5`（`check` 现跑
    //      解析差异仍 0 行、输出指纹 ✓）· `PinnedBaselineMd5`（`基线/out_baseline.txt` 没重生成）。
    //    判据全文 → `资料/普查产出_1025/E6_十三行接闸.md` §五·2 / §六。
    public const string PinnedPlayMd5 = "e7f1de8f96f9e7a255ae72534cf263de";   // 🆕 2026-10-27（第八十六会话 · 重同步点）差异卡 2 张：BL64（ta 短路闸：原 playRc=18 打不出去 → 现在能打）· DA42（A7 结算层闸：不再「打出当刻 +3」）。⚠️ DA42 的完整链等 A1a（模板入池 1,128→1,129）会再钉一次 play + 一次池指纹，那时不是新回归。上一版 = 0766fbe1…  // 🔴 2026-10-27 重钉（第八十五会话同步点 · 两笔探针口径改动）：① 新增 `atkCount`/`exhausted` 两列（原先对「计数类偏离」**全盲**，`E5ENG` 用 ON/OFF 对照实验实测坐实）；② `Program.LoadPool` 在逐卡循环里给 `Core/ForcedAttackRefund.Install` 灌 `chosenAbility`（原先探针里那份 `Data/` partial 走 `Resources.Load`、恒 null ⇒ **一律退还** ⇒ 计数必然回 0，是**量具失真**）。②的影响面**恰好 1 张**：`GOF93`（235 不退还族）`atkCount` 由空变 `P0[2]=1`，其余 1127 张逐列全等、电池 586 张逐字节一致。⚠️ **`atkCount` 只是「退款 / 行动额度记账」的哨兵，⛔ 不是「攻击发生没发生」的读数**。上一版 = 79057e33… ｜ 🔴 2026-10-27 重钉（第八十五会话 · `E1RNG` P-04 随机源）：结算随机改成**原版形状** = 开局预烧 100 个整数 + 游标按下标取（`MatchData__GenerateRandomNumbers.c:1-40` / `MatchData__GetRandomInt.c:8-40`），洗牌也从 Fisher–Yates 改成**原版「按名次挑选」**（`SupportMethods__GetRandomOrder.c:1-98`）⇒ 全池 `ctx.Rng.Next` 清零，结算路径只剩烧表那一行。差异面 = **主档 100 张 / 电池 76 张**，逐族归因与完整 id 清单见 `资料/完全复刻/报告/B9_E1RNG_随机源P04.md` §5.2/§5.3（`dmg` 变值 15 张，其余是「随机挑到哪一张」变了）。上一版 = 1b40f42a… ｜ 🔴 2026-10-26 重钉（第八十四会话同步点）：本会话引擎改动 = BL47 解析修正（`EffectText` 加法型反向正则）· 附魔摘除改排队摘（`481`）· `pendingHealing` 治疗收尾清（含满血/溢疗支）· 残骸原地翻回（`DoReanimate`）· P-06 接 `Aura.RefreshInPlayNumericAndKeywords`（`BL18`/`UM18` 各 +4）· P-05 残骸 `Has` 闸（`SW25`）⇒ 主档 1 张（`BL47`）· 电池 3 张（`BL18`/`SW25`/`UM18`）；每条已由各写手做 A/B 隔离归因。上一版 = af8e8178… ｜ 🔴 2026-10-26 重钉（第八十三会话同步点）：伏击/landing **续打一记**（`A1`/`A3` 查实原版两支都压一条 `BattleActionType=1` 续攻、出队后再进 `_ResolveAttack` ⇒ 12 张 `ambush` 卡从「**零次攻击**」变成「一次」）。电池段 **5 张变**（GSC11/GSC15/GSC25/GSC69/GSC7），`A3` 做过 A/B 实证：**不是卡行为变了**，是控制臂 `Blank(card)` 保留了 `ambush` 关键词、现在也面朝下，与真卡逐字相同被净差减掉（插桩证明两版 a/b 条数完全相同 45/43，只 `b` 里一行内容变了）。主档 1128 张**逐字节一致**。上一版 = 6e888bbe… ｜ 🔴 2026-10-26 重钉（第八十二会话同步点）：本会话引擎改动（残骸清 trait / 灵魂石按阵营 / 相邻目标 / 5 条广播收窄 / 强制攻击退还 / 探针夹具阵营改 SaimHann） ｜ 🔴 2026-10-26 重钉（第八十一会话 · 调度台收尾同步点）：**G14 的「死后相邻单位」效果修好** —— 那 5 张卡原本「卡面写着、我们不打」，现在照卡面生效 ⇒ 主档 **5 张变**（ASH54 dmg 8→12 · ASH73 伤害累计 8→13 且督军 28→26 · SAU56 dmg 0→3 · Particle Whip · None Must Know 眩晕督军 1），**事件电池 586 张逐字节一致**（零外溢）。逐条核对过「都是这 5 张、无新回归」。⚠️ 探针对**归一化内容**算的（文件原始 md5 另算，别混）。上一版 = 39c4f77e…


    /// <summary>`play check` 的缺省基线 —— **按名字往上找**（形状照 `Program.CheckMain.DefaultBaseline`，
    /// ⛔ 别拿「程序集目录往上 N 级」写死：换 build 配置 / 换 TFM 就漂）。</summary>
    static string DefaultPlayBaseline()
    {
        var d = new DirectoryInfo(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
        DirectoryInfo dirHit = null;
        while (d != null)
        {
            var f = Path.Combine(d.FullName, "基线", "out_play_baseline.txt");
            if (File.Exists(f)) return f;
            if (dirHit == null && Directory.Exists(Path.Combine(d.FullName, "基线"))) dirHit = d;
            d = d.Parent;
        }
        return dirHit != null
            ? Path.Combine(dirHit.FullName, "基线", "out_play_baseline.txt")
            : "基线/out_play_baseline.txt";
    }

    /// <summary>指纹行的**列名**（与 <see cref="FingerprintRow"/> **同序**，逐字对应 ⇒ 差异能指名到列）。</summary>
    static readonly string[] PfCols = new[]
    {
        "id", "type", "warlord", "playRc", "played", "faceOps", "selfPlace", "mech", "other",
        "expectedPay", "energyDelta", "handCostDelta", "dmg", "changes", "crash", "bucket",
        "fixtureFit", "injected", "changesFull",
        // 🆕 2026-10-27（`B9_E5ENG`）：计数类两列（**只加在末尾**）。
        //   🔴 它们**进**指纹（不是 `PlayOrder` 那种附加列）—— 那正是本笔的目的。
        "atkCount", "exhausted",
    };

    /// <summary>一张卡的**结算指纹行**（`A1431` 第二笔）。列见 <see cref="PfCols"/>。
    /// 🔴 **⛔ 不含**：卡名（改名的产物）· `note`（口径散文）· 引擎日志（措辞会变）——
    ///   那三样进来会**天天红**，把「红了不用管」训练成习惯（`Sentinel` 头注那条教训）。
    /// 🔴 **含**最后一列 `changesFull` = **变化逐条全文**（「数字动成什么」）——
    ///   少了它，「条数一样而内容变了」就照不到（同族坑：只比张数会被抵消，见 <see cref="Snap"/>）。</summary>
    static string FingerprintRow(CardDef c, Reading r)
    {
        return string.Join("\t", new[]
        {
            c.Id, c.Type, r.HeroMode ? "1" : "0", r.Rc.ToString(),
            r.Played ? "1" : "0", r.FaceHasOps ? "1" : "0",
            r.SelfPlace.ToString(), r.Mech.ToString(), r.Other.ToString(), r.ExpectedPay.ToString(),
            r.EnergyDelta.ToString(), r.HandCostDelta.ToString(), r.Dmg.ToString(),
            r.Changes.Count.ToString(), One(r.Crash), r.Bucket,
            One(FitTag(c)), r.Injected ? "1" : "0",
            One(string.Join(";", r.Changes)),
            // 🆕 2026-10-27（`B9_E5ENG`）：计数两列（末尾；见 `PfCols`）
            One(r.AtkCount), One(r.Exh),
        });
    }

    /// <summary>`TAB`/换行 一律换成空格 —— 指纹文件是**逐行**读的，一格里混进换行会把行读断（静默错位）。</summary>
    static string One(string s) { return s == null ? "" : s.Replace('\t', ' ').Replace('\n', ' ').Replace('\r', ' '); }

    // ------------------------------------------------------------------
    //  🆕 `A1431` 第三笔：**事件电池**的指纹行
    // ------------------------------------------------------------------
    //
    // 🔴 **它为什么必须【另起一套行】、⛔ 不许并进主档**：
    //   · 主档那一批（407 张候选）**已经逐张对过卡面**；把它重算一遍 = 让那批判决全部作废
    //     （`A1431` 账文明写着「那会改动全池所有读数 ⇒ 得单独一轮」）。
    //   · 两档量的是**两件事**（打出去那一拍 / 打出去之后的事件），混在一格里
    //     「哪一档红了」就分不出来了。
    //   ⇒ 本档的行**第一列带前缀 `E|`**（见 <see cref="ECPrefix"/>），同一个文件、两套行。

    /// <summary>事件电池行的**第一列前缀** —— 同一个基线文件里区分两套行的**唯一**判据。</summary>
    public const string ECPrefix = "E|";

    /// <summary>🆕 `B9_W41S3B`（2026-10-27）：**督军档电池**行的前缀。
    /// 形状与 `E|` 行**逐列相同**（同一份 <see cref="PfECols"/>），只是服务的那一族不同
    /// （`E|` 只覆盖 `type=unit`，督军档走 <see cref="MeasureHeroBattery"/>）。
    /// ⚠️ **只在 `preset hero=1` 时写出**（`Sweep` 末尾那一段）⇒ `play check` 的基线里**没有它**。
    /// ⛔ 别把它当第二份 `E|`：两者**不能混读**（夹具不是同一套）。</summary>
    public const string HeroPrefix = "H|";

    /// <summary>这一行是不是事件电池行（判据 = 第一列以 <see cref="ECPrefix"/> 开头）。</summary>
    static bool IsEcRow(string row) { return row != null && row.StartsWith(ECPrefix, StringComparison.Ordinal); }

    /// <summary>事件电池行的**卡 id**（去掉前缀）；非电池行返回 null。</summary>
    static string EcIdOf(string row)
    {
        if (!IsEcRow(row)) return null;
        int p = row.IndexOf('\t');
        return row.Substring(ECPrefix.Length, (p < 0 ? row.Length : p) - ECPrefix.Length);
    }

    /// <summary>事件电池行的**列名**（与 <see cref="FingerprintERow"/> **同序**，逐字对应 ⇒ 差异能指名到列）。</summary>
    static readonly string[] PfECols = new[]
    {
        "id", "type", "bucket", "cand", "playRc", "played", "ctrlPlayed", "steps", "raw", "ctrl",
        "net", "netDmg", "note", "netFull",
        // 🆕 2026-10-27（`B9_E5ENG`）：计数两列（**只加在末尾**；取值 = 电池局**终局**那两格，
        //   见 `EventReading.AtkCount`）。电池档原来对「行动计数 / 疲劳位」同样全盲。
        "atkCount", "exhausted",
    };

    /// <summary>一张卡的**事件电池指纹行**（`A1431` 第三笔）。列见 <see cref="PfECols"/>。
    /// 🔴 **含**最后一列 `netFull` = 净差**逐条全文**（缺了它「条数一样而内容变了」照不到）。
    /// ⛔ **不含**引擎日志（措辞会变 ⇒ 会天天红）。⚠️ `note` 是**口径散文**，但它今天只有
    /// 「对照局没起步」那一种取值（= 出声标记），留着它比丢了强 —— 一旦出现，就是要人看的。
    /// 🆕 `B9_W41S3B`（2026-10-27）：`prefix` —— 默认 `E|`（单位电池）；督军档电池传 `H|`
    ///   （`HeroPrefix`）。⛔ **列一个都不许改**（两边共用同一份 `PfECols` 读法）。</summary>
    static string FingerprintERow(CardDef c, EventReading r, string prefix = null)
    {
        return string.Join("\t", new[]
        {
            (prefix ?? ECPrefix) + c.Id, c.Type, r.Bucket, r.Cand ? "1" : "0", r.PlayRc.ToString(),
            r.Played ? "1" : "0", r.CtrlPlayed ? "1" : "0", One(r.Steps),
            r.Raw.ToString(), r.Ctrl.ToString(), r.Net.Count.ToString(),
            r.NetDmg.ToString(), One(r.Note),
            One(string.Join(";", r.Net)),
            // 🆕 2026-10-27（`B9_E5ENG`）：计数两列（末尾；见 `PfECols`）
            One(r.AtkCount), One(r.Exh),
        });
    }

    /// <summary>指纹行里**按列名取值**（⭐ 别按下标 —— 列一多，下标就会错位；
    /// 列名找不到 / 行不是那套列 ⇒ 返回 `""`，⛔ 不猜）。</summary>
    static string ColOf(string row, string[] cols, string name)
    {
        int c = Array.IndexOf(cols, name);
        if (c < 0) return "";
        var f = row.Split('\t');
        return c < f.Length ? f[c] : "";
    }

    /// <summary>跑一遍全池，出**两套指纹行**：主档 ＋ 事件电池（`play check` 用）。
    /// 🔴 **必须写在同一个循环里** —— 电池那一档的 `cand` 列要**主档那一份 `Reading`**
    ///   （见 <see cref="EventReading.Cand"/>）；分两个循环各算一遍不但慢一倍，
    ///   两套行还会与同一份读数**对不上**（一个写盘、一个在 `Check` 里现算 ⇒ 天天红）。
    /// ⚠️ 与 `Sweep` 的循环**同形**（那一份要不要 TSV/摘要，所以没合并）—— 改一处要改两处。</summary>
    static void BothFingerprintRows(List<CardDef> pool, out List<string> main, out List<string> ec)
    {
        main = new List<string>();
        ec = new List<string>();
        foreach (var c in pool)
        {
            var r = Measure(pool, c);
            r.Bucket = Bucket(c);
            main.Add(FingerprintRow(c, r));
            if (c != null && c.Type == "unit")
            {
                var er = MeasureEvents(pool, c);
                er.Cand = r.Played && r.FaceHasOps && r.Other == 0;
                ec.Add(FingerprintERow(c, er));
            }
        }
        main.Sort(StringComparer.Ordinal);
        ec.Sort(StringComparer.Ordinal);
    }

    /// <summary>跑一遍全池，出**指纹行**（按 `id` 排序 —— ⛔ 别靠卡池顺序：`Dictionary`/池序都不是判据）。</summary>
    static List<string> FingerprintRows(List<CardDef> pool)
    {
        var rows = new List<string>();
        foreach (var c in pool)
        {
            var r = Measure(pool, c);
            r.Bucket = Bucket(c);
            rows.Add(FingerprintRow(c, r));
        }
        rows.Sort(StringComparer.Ordinal);
        return rows;
    }

    /// <summary>一组指纹行的 md5（规范化：**丢掉空行、按整行字节序排序、`\n` 连接**，UTF-8）。
    /// ⚠️ 排序在**函数内部**做 ⇒ 调用方给什么顺序都一样（⛔ 别指望调用方排好）。</summary>
    static string FingerprintMd5(IEnumerable<string> rows)
    {
        var sorted = rows.Where(x => !string.IsNullOrEmpty(x)).OrderBy(x => x, StringComparer.Ordinal).ToList();
        var sb = new StringBuilder();
        foreach (var r in sorted) sb.Append(r).Append('\n');
        return Md5Utf8(sb.ToString());
    }

    /// <summary>写盘用的文本（头几行是 `#` 注释 —— 读的时候**整行丢掉**，⛔ 不参与 md5）。
    /// 🆕 `A1431` 第三笔：**两套行**（主档 ＋ 事件电池 `E|` 前缀那一套）写进同一个文件。</summary>
    static string FingerprintFileText(IEnumerable<string> rows, IEnumerable<string> ecRows = null,
                                      IEnumerable<string> heroRows = null)
    {
        var sb = new StringBuilder();
        sb.Append("# ruleprobe `play` 结算指纹（`A1431` 第二笔）—— 一行一张卡；`play all pf <out>` 生成，`play check` 拿它逐卡对拍。\n");
        sb.Append("# 列（TAB 分隔）：" + string.Join("/", PfCols) + "\n");
        sb.Append("# 🔴 本文件里非 `#` 行的 md5 必须等于源码常量 `PlayProbe.PinnedPlayMd5` —— 重生成它抹不掉那条哨兵。\n");
        sb.Append("# ⛔ 本文件是【被人确认过的状态】的快照；为了把红抹平而重跑 `play all pf` = 把洞开回来。\n");
        sb.Append("#\n# 🆕 `A1431` 第三笔：下面第二段是【事件电池】的行（第一列带 `E|` 前缀，只覆盖 type=unit）。\n");
        sb.Append("# 列（TAB 分隔）：" + string.Join("/", PfECols) + "\n");
        sb.Append("# 它量的是「打出去**之后**，替它把别的时机的事件走一遍，局面动没动」（治「档 B」那一档的失明）。\n");
        sb.Append("# --- 主档（打出去那一拍）---\n");
        foreach (var r in rows.Where(x => !string.IsNullOrEmpty(x)).OrderBy(x => x, StringComparer.Ordinal))
            sb.Append(r).Append('\n');
        if (ecRows != null)
        {
            sb.Append("# --- 事件电池（打出去之后那一段）---\n");
            foreach (var r in ecRows.Where(x => !string.IsNullOrEmpty(x)).OrderBy(x => x, StringComparer.Ordinal))
                sb.Append(r).Append('\n');
        }
        // 🆕 `B9_W41S3B`（2026-10-27）：**督军档电池**（只 `preset hero=1` 时有行；同一份列定义）。
        if (heroRows != null)
        {
            sb.Append("# --- 督军档电池（被测卡当 P0 督军 → 打天赋 → 走电池步 → 用主动能力）---\n");
            sb.Append("# 列（TAB 分隔）：" + string.Join("/", PfECols) + "\n");
            foreach (var r in heroRows.Where(x => !string.IsNullOrEmpty(x)).OrderBy(x => x, StringComparer.Ordinal))
                sb.Append(r).Append('\n');
        }
        return sb.ToString();
    }

    static List<string> LoadFingerprintRows(string path)
    {
        var rows = new List<string>();
        foreach (var l in File.ReadAllLines(path))
            if (l.Length != 0 && !l.StartsWith("#", StringComparison.Ordinal)) rows.Add(l);
        return rows;
    }

    static string Md5Utf8(string s)
    {
        using (var md5 = System.Security.Cryptography.MD5.Create())
        {
            var h = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(s));
            var sb = new StringBuilder(h.Length * 2);
            foreach (var x in h) sb.Append(x.ToString("x2"));
            return sb.ToString();
        }
    }

    /// <summary>`play check [&lt;pf.txt&gt;]` —— **结算层对拍**（`A1431` 第二笔）。
    /// 两件事，**两件都算进 rc**（⛔ 不许「文件变了但界面全绿」）：
    ///   ① **逐卡比**：当次现算的指纹 vs 基线文件（出「哪张卡、哪一列、从什么变成什么」）；
    ///   ② **哨兵**：基线文件内容的 md5 vs `PinnedPlayMd5`（源码常量 ⇒ 重生成基线抹不掉）。
    /// 退出码：`0` = 两条都过 · `1` = 有差异或哨兵红 · `2` = 基线缺失/读不到。
    /// ⛔ **它不判对错** —— 它判的是「结算结果有没有离开被确认的那一版」；
    ///   「离开」可能是**真回归**、也可能是**有意改动**，判决看卡面 / 反编译 / 真机。</summary>
    static int Check(List<CardDef> pool, string baselinePath)
    {
        bool wasDefault = string.IsNullOrEmpty(baselinePath);
        if (wasDefault) baselinePath = DefaultPlayBaseline();
        baselinePath = Path.GetFullPath(baselinePath);
        if (!File.Exists(baselinePath))
        {
            Console.Error.WriteLine("!! 没有结算指纹基线：" + baselinePath
                                    + (wasDefault ? "（缺省值 = 程序集目录往上找到的第一条 `基线/out_play_baseline.txt`）" : ""));
            Console.Error.WriteLine("   生成办法：`play all pf <out.txt>`，逐卡对着卡面确认之后，"
                                    + "把 `PlayProbe.PinnedPlayMd5` **手改**成打出来的那串。");
            return 2;
        }

        List<string> live, liveE;
        BothFingerprintRows(pool, out live, out liveE);   // 主档 ＋ 事件电池（一个循环里算，见该函数）
        var baseRowsRaw = LoadFingerprintRows(baselinePath);
        // 🔴 两套行**分家对拍**（判据 = 第一列的 `E|` 前缀，见 `IsEcRow`）——
        //    混成一份会把「主档少了一张」误报成「电池多了一张」。⛔ 不靠行序/行数。
        var baseRows = baseRowsRaw.Where(x => !IsEcRow(x)).ToList();
        var baseE = baseRowsRaw.Where(IsEcRow).ToList();
        string liveMd5 = FingerprintMd5(live.Concat(liveE)), baseMd5 = FingerprintMd5(baseRowsRaw);

        string IdOf(string row) { int p = row.IndexOf('\t'); return p < 0 ? row : row.Substring(0, p); }
        var baseMap = new Dictionary<string, string>();
        foreach (var r in baseRows) baseMap[IdOf(r)] = r;
        var changed = new List<string[]>();
        int added = 0;
        var addedIds = new List<string>();
        foreach (var r in live)
        {
            var id = IdOf(r);
            if (!baseMap.TryGetValue(id, out var b)) { added++; if (addedIds.Count < 20) addedIds.Add(id); continue; }
            if (!string.Equals(b, r, StringComparison.Ordinal)) changed.Add(new[] { id, b, r });
        }
        int removed = 0;
        var removedIds = new List<string>();
        var liveIds = new HashSet<string>(live.Select(IdOf), StringComparer.Ordinal);
        foreach (var kv in baseMap)
            if (!liveIds.Contains(kv.Key)) { removed++; if (removedIds.Count < 20) removedIds.Add(kv.Key); }

        // ---- 🆕 事件电池那一套（同一套对拍口径，只是换了一份行）----
        var ecBaseMap = new Dictionary<string, string>();
        foreach (var r in baseE) ecBaseMap[IdOf(r)] = r;
        var ecChanged = new List<string[]>();
        int ecAdded = 0, ecRemoved = 0;
        var ecAddedIds = new List<string>();
        foreach (var r in liveE)
        {
            var id = IdOf(r);
            if (!ecBaseMap.TryGetValue(id, out var b)) { ecAdded++; if (ecAddedIds.Count < 20) ecAddedIds.Add(id); continue; }
            if (!string.Equals(b, r, StringComparison.Ordinal)) ecChanged.Add(new[] { id, b, r });
        }
        var liveEIds = new HashSet<string>(liveE.Select(IdOf), StringComparer.Ordinal);
        foreach (var kv in ecBaseMap) if (!liveEIds.Contains(kv.Key)) ecRemoved++;

        Console.WriteLine("=== play check · 结算层对拍（不跑 Unity）· 池 " + pool.Count + " 张 ===");
        Console.WriteLine("  基线 " + Path.GetFileName(baselinePath) + "（主档 " + baseRows.Count
                          + " 行 ＋ 事件电池 " + baseE.Count + " 行）· 当次现算 主档 " + live.Count
                          + " ＋ 电池 " + liveE.Count);
        Console.WriteLine("  结算指纹：当次现算 = **" + liveMd5 + "** · 基线文件 = " + baseMd5
                          + (liveMd5 == baseMd5 ? "（一致）" : "（**不同**）"));
        Console.WriteLine();
        Console.WriteLine("  --- ①-a 逐卡差异（主档 · 打出去那一拍）---");
        if (changed.Count == 0 && added == 0 && removed == 0)
            Console.WriteLine("     ✅ 没有一张卡与基线不同（" + live.Count + " 张逐字节一致）");
        else
        {
            Console.WriteLine("     🔴 **" + changed.Count + " 张卡变了** · 新增 " + added + " · 消失 " + removed
                              + (added > 0 ? "（新增：" + string.Join(" · ", addedIds) + "）" : "")
                              + (removed > 0 ? "（消失：" + string.Join(" · ", removedIds) + "）" : ""));
            Console.WriteLine("     逐张（最多列 60 张；`changesFull` 截 160 字）：");
            foreach (var t in changed.Take(60))
            {
                var a = t[1].Split('\t'); var b = t[2].Split('\t');
                Console.WriteLine("       " + Pad(t[0], 24) + "（" + Math.Max(a.Length, b.Length) + " 列对拍）");
                int n = Math.Max(a.Length, b.Length);
                int shown = 0;
                for (int k = 0; k < n; k++)
                {
                    string av = k < a.Length ? a[k] : "";
                    string bv = k < b.Length ? b[k] : "";
                    if (string.Equals(av, bv, StringComparison.Ordinal)) continue;
                    if (PfCols.Length == n) Console.Write("         · " + Pad(PfCols[k], 14));
                    else Console.Write("         · 第 " + k + " 列            ");
                    Console.WriteLine(Clip(av, 160) + "  →  " + Clip(bv, 160));
                    if (++shown >= 8) { Console.WriteLine("         …（这一张还有差异列，⛔ 已截断：看 `play <" + t[0] + ">`）"); break; }
                }
            }
            if (changed.Count > 60) Console.WriteLine("       …（还有 " + (changed.Count - 60) + " 张）");
        }

        // ------------------------------------------------------------------
        //  🆕 `A1431` 第三笔：事件电池那一套的对拍 ＋ **覆盖面**
        // ------------------------------------------------------------------
        Console.WriteLine();
        Console.WriteLine("  --- ①-b 逐卡差异（**事件电池** · 打出去**之后**那一段）---");
        if (ecChanged.Count == 0 && ecAdded == 0 && ecRemoved == 0)
            Console.WriteLine("     ✅ 没有一张卡与基线不同（" + liveE.Count + " 张逐字节一致）");
        else
        {
            Console.WriteLine("     🔴 **" + ecChanged.Count + " 张卡变了** · 新增 " + ecAdded + " · 消失 " + ecRemoved
                              + (ecAdded > 0 ? "（新增：" + string.Join(" · ", ecAddedIds) + "）" : ""));
            foreach (var t in ecChanged.Take(60))
            {
                var a = t[1].Split('\t'); var b = t[2].Split('\t');
                Console.WriteLine("       " + Pad(t[0].Substring(ECPrefix.Length), 24)
                                  + "（" + Math.Max(a.Length, b.Length) + " 列对拍）");
                int n = Math.Max(a.Length, b.Length);
                int shown = 0;
                for (int k = 0; k < n; k++)
                {
                    string av = k < a.Length ? a[k] : "";
                    string bv = k < b.Length ? b[k] : "";
                    if (string.Equals(av, bv, StringComparison.Ordinal)) continue;
                    if (PfECols.Length == n) Console.Write("         · " + Pad(PfECols[k], 14));
                    else Console.Write("         · 第 " + k + " 列            ");
                    Console.WriteLine(Clip(av, 160) + "  →  " + Clip(bv, 160));
                    if (++shown >= 8) break;
                }
            }
            if (ecChanged.Count > 60) Console.WriteLine("       …（还有 " + (ecChanged.Count - 60) + " 张）");
        }

        // ---- 🔴 覆盖面：**这一档到底被验到了多少**（⛔ 不许只印一句「已知不适用」就完事）----
        //   ⚠️ 统计的口径 = **主档里的候选**（`cand=1`：卡面有正文 · 打出去了 · 主档实得 0 条）
        //      —— 那一批才是「主档看着它、却看不见」的卡。⛔ 别拿整个 `unit` 档当分母：
        //      里面一半是**卡面根本没正文**的卡，它们 `other=0` 与失明无关，混进去就是灌水。
        var candsE = liveE.Where(r => ColOf(r, PfECols, "cand") == "1").ToList();
        int ecNetPos = candsE.Count(r => ColOf(r, PfECols, "net") != "0" && ColOf(r, PfECols, "net") != "");
        Console.WriteLine();
        Console.WriteLine("  --- ①-c 🔴 事件电池覆盖了谁（`A1431` 第三笔 · **这一档真被验到了几张**）---");
        Console.WriteLine("     分母 = 主档里的**候选**（卡面有正文 · 打出去了 · 主档实得 0 条）= **"
                          + candsE.Count + " 张**（电池行总数 " + liveE.Count + " 张 = `unit` 全档）");
        Console.WriteLine("     其中**电池净差 > 0 的 = " + ecNetPos + " 张**（= 它的正文**真的被那个事件抬起来过**）· "
                          + "净差 = 0 的 = **" + (candsE.Count - ecNetPos) + " 张**");
        Console.WriteLine("     ⚠️ 净差 = 0 的那几张，**主档那一拍看不见、电池这一趟也没抬起来**"
                          + " ⇒ 它们**仍然只被「读数的存在」看着、没被「事件的结算」验过**（逐条列在下面）。");
        var zeroIds = candsE.Where(r => ColOf(r, PfECols, "net") == "0")
                            .Select(r => IdOf(r).Substring(ECPrefix.Length)).ToList();
        for (int i = 0; i < zeroIds.Count; i += 8)
            Console.WriteLine("       " + string.Join(" · ", zeroIds.Skip(i).Take(8)));
        if (zeroIds.Count == 0) Console.WriteLine("       ✅ 一张都没有 —— 主档那份候选**全部**被电池抬起来过");
        if (zeroIds.Count > 0)
        {
            // 按**根因**分（判据 = 引擎自己的公开访问器，见 `EcCause`）——
            // 卡池里按 id 找回来；找不到的（池被换过）单独记一格，⛔ 不当成「没原因」。
            var byId = new Dictionary<string, CardDef>(StringComparer.Ordinal);
            foreach (var c in pool) if (c != null && c.Id != null) byId[c.Id] = c;
            var groups = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (var id in zeroIds)
            {
                string k = byId.TryGetValue(id, out var cc) ? EcCause(cc) : "卡池里查不到这个 id";
                if (!groups.TryGetValue(k, out var l)) groups[k] = l = new List<string>();
                l.Add(id);
            }
            Console.WriteLine("       ⛔ **按根因分**（判据 = 引擎自己的 `CardDef.SpiritOps` / `FaithOps`；"
                              + "⚠️ 后两格 `A1943` 之后**只表示「有这类能力」**，⛔ 不再等于「夹具付不起」）：");
            foreach (var kv in groups.OrderByDescending(x => x.Value.Count))
                Console.WriteLine("         · " + Pad(kv.Key, 30) + " = " + kv.Value.Count + " 张");
            Console.WriteLine("       🔴 `A1943`（2026-10-14）**已经把夹具的起始派系资源补齐**"
                              + "（`BuildFixture` / `HeroPass` 的 4 处 `SetEnergy` 各配一次 `SetFaction`："
                              + "Faith " + FixtureFaith + " / Spirit " + FixtureSpirit + " / Quest " + FixtureQuestpoints + "）");
            Console.WriteLine("         ⇒ 原来那 21 张「有 Sp/Faith 能力」的全部转成 `net > 0`"
                              + "（**①-c 净差 = 0 由 175 降到 154**）⇒ 🔴 **今天这两格应为空**；"
                              + "若又冒出几格，先比 `EffectOp.Cost` / 阈值与上面三个常数（见 `EcCause`），⛔ 别再照旧话治一遍。");
        }
        int ecNote = liveE.Count(r => ColOf(r, PfECols, "note").Length > 0);
        Console.WriteLine("     行内出声（`note` 非空 = 对照局没起步） = **" + ecNote + " 张**"
                          + (ecNote > 0 ? "  ← 🔴 这几张的净差**不可信**" : "（✅ 没有）"));

        Console.WriteLine();
        Console.WriteLine("  --- ② 哨兵：基线文件的内容 vs 源码常量 ---");
        int sentFail = 0;
        if (baseMd5 != PinnedPlayMd5)
        {
            sentFail = 1;
            Console.WriteLine("     ✗ 🔴 **基线文件的内容 ≠ 被确认过的那一版**（`A1431` 第二笔的②）。");
            Console.WriteLine("        基线文件 md5=" + baseMd5 + " · 钉住=" + PinnedPlayMd5);
            Console.WriteLine("        ⚠️ 这一条判的是**基线文件本身**（它与当次现算是否一致见 ①）——"
                              + "「重生成基线把一次真回归抹平」正是它抓的那个动作。");
            Console.WriteLine("        ⇒ 判：`git diff` 那个基线文件 + 看 ① 的逐卡差异：");
            Console.WriteLine("           · **真回归**（某张卡该结算而没结算 / 数字变了）⇒ 去修引擎，⛔ 别改常量；");
            Console.WriteLine("           · **有意且已验过**（逐张对过卡面 / 反编译 / 真机）⇒ **手改**这一行：");
            Console.WriteLine("               public const string PinnedPlayMd5 = \"" + baseMd5 + "\";");
        }
        else
            Console.WriteLine("     ✓ 基线文件的内容与钉住的一致（" + baseMd5.Substring(0, 8) + "…）"
                              + " —— 重生成基线抹不掉这一条");

        Console.WriteLine();
        Console.WriteLine("  判据：①-a 主档逐卡全等 且 ①-b 电池逐卡全等 且 ② 哨兵过 ⇒ rc=0；否则 rc=1。");
        Console.WriteLine("  🔴 `A1431` 第三笔（2026-10-13）**就地订正**：本行原来写着「`档 B` 那 274 张"
                          + "「等事件」的卡在这里**照样看不见**」—— **现在不成立了**："
                          + "事件电池（①-b）就是替那一档把**别的时机**的事件走一遍（见本文件 `RunEventPass`）。");
        Console.WriteLine("  ⚠️ **仍如实标着的边界**：①-a 只看得见「打出去那一拍」；**电池只覆盖 `type=unit`**"
                          + "（`tactic`/`defence` 的正文打出即结算，主档已经看得到）；"
                          + "电池抬不到的那个事件（若 `steps` 里对应那一步不是 `rc=0`）**那一张就没被验到** ——"
                          + "逐张的 `steps` 列就在基线文件里，⛔ 别只看「绿」就以为全档都验过了。");
        bool ecClean = ecChanged.Count == 0 && ecAdded == 0 && ecRemoved == 0;
        return (changed.Count == 0 && added == 0 && removed == 0 && ecClean && sentFail == 0) ? 0 : 1;
    }

    // ==================================================================
    //  小工具
    // ==================================================================

    /// <summary>返回码 → 名字（照 `RuleCodes` 逐条抄）。⛔ 别只打数字 —— 读数要能一眼读。</summary>
    static string CodeName(int rc)
    {
        switch (rc)
        {
            case 0: return "OK";
            case 1: return "ErrBadHand";
            case 2: return "ErrCost";
            case 3: return "ErrSlot";
            case 4: return "ErrNotTurn";
            case 5: return "ErrNotUnit";
            case 6: return "ErrExhausted";
            case 7: return "ErrNoAttack";
            case 8: return "ErrSelf";
            case 9: return "ErrStunned";
            case 10: return "ErrTarget";
            case 11: return "ErrPindown";
            case 13: return "ErrUnimplemented";
            case 14: return "ErrNoAbility";
            case 15: return "ErrDutyUsed";
            case 16: return "ErrNoAction";
            case 17: return "ErrNotWaystone";
            case 18: return "ErrNoTargetAvailable";
            case 19: return "ErrNotEnoughRoom";
            case -1: return "未打";
            default: return "rc" + rc;
        }
    }

    static string CodeWhy(int rc)
    {
        switch (rc)
        {
            case 2: return "能量不足";
            case 3: return "格位非法/越界";
            case 4: return "非本方回合";
            case 13: return "`IsFullyParsed(desc)` 为假且不是手牌陷阱 ⇒ 本版打不出去";
            case 18: return "**夹具里没有合法目标**（量具限制，多半不是卡的缺陷）";
            case 19: return "棋盘满了";
            default: return "见 `RuleCodes.cs`";
        }
    }

    static string Pad(string s, int w)
    {
        int vis = 0;
        foreach (char ch in s ?? "") vis += ch > 0x2000 ? 2 : 1;
        var b = new StringBuilder(s ?? "");
        while (vis < w) { b.Append(' '); vis++; }
        return b.ToString();
    }

    static string Clip(string s, int n)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Length <= n ? s : s.Substring(0, n) + "…";
    }
}
