# 查证：EC 四张「裸写效果句」单位卡的触发点

> 2026-09-14。**只读查证**，未改任何已有文件 / 引擎代码。四张卡 `keywords` 均为 `[]`、`desc` 均无 `X:` 前缀
> ⇒ 引擎不知道何时触发，效果**从不发生**（且覆盖率报表看不见）。
> 🔴 **原假设「`Lord Exultant` / `Tormentor Obsessionist` 是部署时」被推翻** —— 它们不是没触发点，
> 而是**触发词前缀被存到了 `desc` 之外的字段**（见下表 D/E 列）。四个触发点全部查到，无「查不到」。

证据路径缩写：`AT`=`d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityTrigger.cs` ·
`DC`=`d:/2/Warpforge_tools/data/decomp_il2cpp_0827/`（`decomp_out/` `decomp_out2/`）·
`RB`=`d:/4/Unity/资料/规则书/Warpforge_Offline_Rulebook_1_5-3_中文翻译.md` ·
`RAW`=`d:/4/Unity/数据/游戏数据/card_stats_raw.json` ·
`IMG`=`d:/2/Warpforge部队卡片/Emperor_s Children/3部队/`（900×1200 PnP 成品卡图，铁律 7）

| 卡名 ‖ 句子 ‖ 触发点是什么 ‖ 证据 ‖ 置信度 ‖ 建议挂在引擎哪一处 |
|---|---|
| `Blastmaster Noise Marine` ‖ `Destroy any enemy troop with Armour attacked by this unit` ‖ **本单位攻击结算后**，对它打过的**每一个**目标各判一次；目标须带 Armour。⇒ 原版 `AbilityTrigger.UnitAttack = 50`。**不是**关键词、**不是**部署（`RAW` 里 `subtitle: null`、`keywords: []`） ‖ ①`AT:14` `UnitAttack = 50`；②`DC/decomp_out/CardScript__ResolveUnitAttacked.c:30` —— `OnTrigger(…, 0x32 /*=50*/, …)`；③`DC/decomp_out/BattleManagerSupport__BroadcastUnitAttacked.c:24-68` —— 逐支广播给「目标 → 攻击者 → 场上每一张 → 双方手牌」，**逐支都带 `targetCard`**；④`DC/decomp_out2/BattleManager._ResolveAttack_d__438__MoveNext.c:991` —— 广播点在 `_ResolveAttack` 协程内、`FinishAttackActionWithoutMoving` 之后；⑤`RB:161-225` 无任何「attacked」关键词 ⇒ 排除关键词触发；⑥`IMG/Warpforge_19_Blastmaster-Noise-Marine.png` 效果行**无触发词前缀**（句中的 `[盾牌]Armour` 是内联词标，不是前缀） ‖ **高** ‖ `RuleCore.DeclareAttack`（`RuleCore.cs:996`）里**伤害之后**那段触发块 `RuleCore.cs:1242-1287`（`Slay` `:1258` / `Strike` `:1268` / `Mob` `:1280` / `Regiment` `:1286`）—— 原版这四个和 `UnitAttack` **都长在同一个 `ResolveUnitAttacked` 里**（本轮新证：它的 `param_4` 就是 `AttackTypes`）。⚠️ **别挂** `RuleCore.cs:1064` 那个 `WhenEventKind.Attack` 广播：那条在伤害**之前** |
| `Sonic Blaster Noise Marine` ‖ `Stun enemy troops attacked and give them -1 [armor] and -1 [attack]` ‖ 同上：**本单位攻击结算后**，对它打过的每一个目标：眩晕 + 攻/甲各 -1。同样 `UnitAttack = 50` ‖ ①②③④同上（同一族）；⑤`RB:177`「**震荡（Concussion）**｜被本单位攻击的单位获得眩晕（Stun）」—— 规则书对 Concussion 的定义与本卡**第一小句逐字同义**，是独立的第二来源；⑥`RAW` 该卡 `keywords: []`（**没有** `Concussive`）、`subtitle: null`；⑦`IMG/Warpforge_17_…png` 首字符是 **Stun 图标**，但它紧跟的词就是句首的 `Stun` ⇒ **是句中该词的内联词标，不是触发词前缀** ‖ **高** ‖ 同上（`RuleCore.cs:1242-1288`）。载荷 `stun` + `give -1 attack` / `give -1 armour` 引擎已有 |
| `Lord Exultant` ‖ `Give +1 Attack and +1 Ranged Attack to all friendly troops` ‖ **激励（Stimulation）：本卡被战术选中时、结算前触发**。**不是部署** ‖ ①`IMG/Warpforge_35_Lord-Exultant.png` 效果行前缀 = 激励图标 + 英文词 **`Stimulation:`**（卡面上图标旁就印着关键词，照 `资料/关键词图标/关键词与图标_对照表.md:133` = 激励）；②`RAW` → `"subtitle": "Stimulation"`（**结构化字段直接坐实**）；③`RB:212`「**激励（Stimulation）**｜被战术选中时、结算前：触发能力」；④`AT:94` `Stimulation = 710`；⑤`DC/decomp_out/BattleManager__AddTriggerCardPlayedByStimulation.c` · `…__ResolveTriggerCardPlayedByStimulation.c` · `BattleActionType.cs:94 triggerCardPlayedByStimulation = 91` ‖ **高** ‖ **机制已有、只差登记**：触发点 `EffectResolver.cs:882`（`FireTriggerAt(ctx, st, KeywordTable.Stimulation, …)`，在 `ResolveOps` **之前** = 规则书的「结算前」）。把 `stimulation` 加进这张卡的 `keywords`，`CardDef.cs:265 CollectBareKeywordBody` 就会把整条 `desc` 收成它的正文（`Stimulation` 已在 `BodyKeywords` `CardDef.cs:172`） |
| `Tormentor Obsessionist` ‖ `Give +2 [Attack] to all friendly troops` ‖ **狂喜 3（Ecstasy 3）：本单位生命降到 3 或以下且未死亡时触发**。**不是部署** ‖ ①`IMG/Warpforge_23_Tormentor-Obsessionist.png` 效果行前缀 = 狂喜图标 + **`Ecstasy 3:`**（图标对照 `…与图标_对照表.md:95` = 狂喜，**带数值**）；②`RAW` → `"subtitle": "Ecstasy 3:"`（**阈值 X=3 就在这儿**）；③`RB:182`「**狂喜 X（Ecstasy X）**｜本单位生命降至 X 或以下未死亡时触发效果」；④`AT:95` `Ecstasy = 715`；⑤`DC/decomp_out/CardScript__ResolveDamageDealt.c:149` `ShouldTriggerEcastasy(this, …, 生命差)` → `:153` `OnTrigger(…, 0x2cb /*=715*/, …)`，**紧接其后** `:163` 才发 `0x3c`（=60 `UnitDamaged`）⇒ 时机在**伤害已结算之后** ‖ **高** ‖ **引擎还没做**（如实记着：`CardDef.cs:934`、`:1105-1107`）。时机点 = `RuleCore.Hurt` 里已经在放 `Cruelty` 的那一处（`RuleCore.cs:1427`）。要做三件：①`ecstasy` 进 `RoutableTriggers`(`CardDef.cs:139`) 与 `BodyKeywords`(`:172`)；②`AddTriggerOp`(`CardDef.cs:400`) 现在**整词相等**，认不了 `Ecstasy 3:`，要改成认 `关键词 N:` 并取 N；③阈值从 `RAW` 的 `subtitle` 取（**不是**从 `cards_engine.json` 的 `keywords`） |

**值得注意的发现**

1. 🔴 **`card_stats_raw.json` 的 `subtitle` 就是被丢掉的那半**：它带着 `Ecstasy 3:` / `Stimulation` / `Waystone.` / `Tide 1.`，而 `cards_engine.json` **压根没有这个字段**。四张里两张靠它直接定案 —— 建议先把它补进卡表，再谈挂触发点。
2. ⚠️ **卡面图标不能当触发线索用**：`Sonic Blaster` 句首的 Stun 图标，和 `keywords` 里登记的关键词**不是一回事**（同族 `Snakebite Grot` 的 `keywords` 里才有 `Stun`）。照着图标挂关键词会错。
3. 「被这套打过的单位如何」这一族**不止 2 张**：`cards_engine.json` 里 `desc` 含 `attacked` 的共 **6 张** —— 另加 `Stikkbomb Boy`、`Snakebite Grot`、`Arjac Rockfist`、`Venomthrope`（横跨 EC / Orks / Space Wolves / Tyranid）。第 1-2 行的修法**一次修好 6 张**。
4. ⚠️ **唯一没坐实的**：`UnitAttack` 广播相对**主伤害**的先后（`_ResolveAttack` 协程状态 `0x15`/`0x16` 分支跳出；且 `ResolveUnitAttacked` 内 `Slay`/`Strike`/`Mob` 的先后反编译也看不到 —— 我们 `RuleCore.cs:1272-1274` 早已如实标着）。取「伤害后」与原版队友一致；**要坐实只能跑实况**（铁律 4，本轮未跑 Unity）。
5. `subtitle` **不是纯关键词字段**：同族另 3 张 SaimHann 里 `Autarch` = `Waystone.`（是关键词），但 `Wraithblade` = `Saim-Hann`、`Wraithlord` = `Vehicle`（是阵营/兵种行）⇒ 用它之前要先判「这一条是不是关键词」。
