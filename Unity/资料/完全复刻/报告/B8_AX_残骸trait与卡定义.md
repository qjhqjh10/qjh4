# B8_AX · 残骸不继承 trait（成对口径）· `CardDef` / `UnitState` 剩余 B 行

> 执行代理 **AX**（代号 `B8_AX`）· 2026-10-26
> 白名单 = `RuleEngine/Core/CardDef.cs` · `RuleEngine/Core/UnitState.cs` · 账补丁 `账/_parts/B8_AX.tsv` · 本报告
> 红线执行：**没跑 Unity / 批处理**（只跑 `TMPDIR=/tmp/wf_b8ax 工具/typecheck.sh`）· **没动 git** · **没改主账**（只写 `_parts/B8_AX.tsv`）· **没改正本** · **没越白名单** · **没派子代理**
> `typecheck`：**运行时 0 / 编辑器 0**
> 行尾：`CardDef.cs` **8,639 行 / LF / 裸 CR 0**（改前 8,615，**+24**）· `UnitState.cs` **2,549 行 / LF / 裸 CR 0**（改前 2,474，**+75**）

---

## ① 结论

| 项 | 数 |
|---|---|
| 账切片现读（`ledger_slice --file <X> --status B --limit 0`） | `UnitState` **30 行** · `CardDef` **36 行** |
| **B → A** | **3**（`IsWarlordEquivalent` · `SetBaseCardTraits` · `ClearEndOfTurnEffects`） |
| 已有的 A 行**就地订正**（同一个名字两套口径） | **1**（`SupportMethods.IsAeldariRemnant`） |
| 保持 **B**（本轮补证据 / 就地订正） | **6**（`GetExtraAbilityTraits` · `GetTraitForActiveAbilityRawCard` · `GetTraitFromAbilityTrigger` · `GetTraitEffect` · `GetTraitEffects` · `RemoveTraitSilently`） |
| 保持 **C**（判据补齐 + 原语已备） | **3**（`TransformIntoRemnant` · `HasCurrentTrait` · `TryUsingWaystone`） |
| 改动的 `.cs` | **2 个文件、净 +99 行**（`CardDef.cs` +24 · `UnitState.cs` +75） |

### 🔴 任务 1 的**成对结论**（这是本轮最要紧的一条）

**原版「点击残骸收集灵魂石」到底读哪个字段 —— 读清了，而且和 trait 无关：**

| 环节 | 原版 | 判据 |
|---|---|---|
| ① **哪张残骸能收** | `SupportMethods.IsAeldariRemnant(card)` = `rawCard.cardArmy(+0x2c) == 30 (SaimHann)` ∧ `isRemnant(+0x65)` —— **一个字都不读 trait** | `decomp_full/SupportMethods__IsAeldariRemnant.c:1-14`（🥇 14 行全文） |
| ② **此刻能不能收** | `BattleManager.CanUseWaystone(bm, card, isPlayerAsking, showTips)` 五道闸：`card.isPlayer(+0x40)==isPlayerAsking` ∧ 该方允许行动 ∧ `UIstate(+0x290)==1` ∧ `cardState(+0x228)==2` ∧ 教程闸 | `decomp_full/BattleManager__CanUseWaystone.c:20-53`（🥇） |
| ③ **点了之后** | `TryUsingWaystone` → `useWaystone`(76) 入队 → `ResolveUseWaystone` 协程 | `decomp_full/BattleManager__TryUsingWaystone.c` 全文 |

`+0x2c = cardArmy` 由**两处独立读数**坐实（同一个偏移、两个不同阵营值）：
`IsAeldariRemnant` 问 `0x1e(30) = SaimHann`；`SupportMethods__ShouldRemnantDestroyOnTurnEnd.c:17` 问 `0x28(40) = Sautekh`；
且 `ObtainableItem.cs` 的字段序（`cardRarity` → `cardArmy`）与 `EntityScript__get_cardRarity.c`（`rawCard +0x28`）自洽。

**⇒ 修法（成对，缺一半就断链）**

1. **残骸不继承 trait**：`RuleCore.CleanupDeaths` 造出残骸 `UnitState` 之后、写进 `Board[slot]` 之前，调 `rem.ClearAllKeywords()`（本轮已在 `UnitState` 类尾备好）。
2. **收集判定换成原版字段**：`RuleCore.CanCollectWaystone` 那句 `!u.IsRemnant || !u.Has(KeywordTable.Waystone)` 换成 `!u.IsAeldariRemnant`（本轮已在 `UnitState` 类尾备好 `IsAeldariRemnant`）。
3. **（连带）一处夹具**：`RuleEngineTest.cs:4062` 的收集夹具阵营写死 `"Test"` ⇒ 换成阵营判定后它会红，必须改成 `SaimHann`。

**为什么这两步不会改坏东西（零回归证明）**：池内两条口径**同集合** ——
`cards_engine.json` 1,128 张现扫：带 `Waystone` 的 **25 张全在 `SaimHann`**；带 `Remnant` 的 **26 张全在 `Sautekh`**；两者交集 **0**。
（同一条事实在 `cards_truth.json` 1,351 张上：`waystone(1140)` 27 张全 army 30；`remnant(1050)` 28 张全 army 40。）

**⛔ 本轮没接线、只备原语的原因**：两处落点都在 `RuleEngine/Core/RuleCore.cs`，
而调度台开局表（`调度台日志.md:4263`）把它判给执行代理 **B** 独占 ⇒ 按 CLAUDE.md §13·3「一个文件同一时刻只有一个写手」**不越界**。
精确补丁见 §六 R1，是**两句调用 + 一行断言夹具改动**。

---

## ② 逐条证据

### ②·a 升 A（3）

| # | 账行 | 原版方法 | 判据（🥇 `decomp_full/*.c`） | 我们的落点 | 观测量（两侧） |
|---|---|---|---|---|---|
| 1 | 其他_基础设施:258 | `GameStaticData.IsWarlordEquivalent` | `GameStaticData__IsWarlordEquivalent.c:1-11` + `CardScript__IsWarlordEquivalent.c:8` | `CardDef.cs:6552`（新增谓词） | `cardType==10` ∩ 池 = **56** vs `type=="hero"` = **56** |
| 2 | 战斗逻辑驱动:1014 | `CardScript.SetBaseCardTraits` | `CardScript__SetBaseCardTraits.c:1-120` | `UnitState.cs:881-940`（构造） | 卡面 trait 表：**1,128/1,128 条数一致**、0 张不匹配；名集 1,123/1,128（剩 5 张是 `rage↔penitence` 同义归一） |
| 3 | 战斗逻辑驱动:812 | `CardScript.ClearEndOfTurnEffects` | `CardScript__ClearEndOfTurnEffects.c:1-36` | `UnitState.cs:1646-1685`（RevertBuffs） | 「本回合 +2 攻」：3→5→（EndTurn 后）3；断言 `RuleEngineTest.cs:5460-5465`，灭自证判别式 `:5852/5859` |

### ②·b 保持 B（6，本轮把判据读全，下一路可直接照做）

| 账行 | 方法 | 本轮读到的（判据已齐，差的是「照搬」或「没有运行时可观察量」） |
|---|---|---|
| :1380 | `RawCardScript.GetExtraAbilityTraits` | 🥇 120 行全文 + 两张表（25 对 `(trigger,trait)` / 六档阵营）+ 唯一调用者 `RawCardScript.CheckCurrentTraits` **全树 0 调用点** ⇒ **死代码**。`D` 不允许（调度台裁定 1 原话「死代码本身不足以判 D」）。语义已解：`CheckCurrentTraits` 会把派生 trait **追加进 SO 的 `CardTraits`**。 |
| :1129 | `CardTraitCollection.GetTraitForActiveAbilityRawCard` | 🥇 26 行六档逐值解出（10→1275 `oath` · 80→1185 `pray` · 100→1220 `duty` · 110→1235 `agenda` · 130→1265 `ferocity` · 其余→400 `activeAbility`）；池内 **77 张** `hasActiveAbility` 卡里 **76 张**本来就带那条 trait，**唯一反例 `SOR66 Armorium Cherub`**（活路 = 表现层图标/动画）。 |
| :1130 | `CardTraitCollection.GetTraitFromAbilityTrigger` | 🥇 58 行：按**名字**折（0→10 · 80→120 · 81→1010 三处硬编码，其余 `Enum.TryParse<DefinedTrait>(lowerFirst(name))`）。逐项对表：`AbilityTrigger` 97 项里 **29 项**同名命中 + 3 硬编码 = **32/97 有映射，65 项回 0**。 |
| :1126 / :1128 | `GetTraitEffect` / `GetTraitEffects` | 🥇 70 行 / 29 行逐句：同一张全局词条表的「单条 / 整列」两档（`trait` → 效果列表，再按 `entry + 0x28 == triggerType` 取一条）。结构差 = 原版「一 trait 一份机制共享全池」vs 我们「一卡一份正文」。 |
| :979 | `CardScript.RemoveTraitSilently` | 已备原语 `ClearAllKeywords`（`UnitState.cs:2539-2547`），只差 `RuleCore` 一句接线。 |

### ②·c 保持 C（3，判据补齐）

| 账行 | 方法 | 现在卡在哪（都写进了账的「备注」） |
|---|---|---|
| :1068 | `CardScript.TransformIntoRemnant` | 缺第 ③ 步（倒序逐条 `RemoveTraitSilently`）；原语已在，接线在 `RuleCore.cs`（B 独占）。 |
| :1182 | `EntityScript.HasCurrentTrait` | 与 :1068 同一根因 + 「印刷那份 / 授予那份」双簿（大改，会动全部 `Has` 读点语义）。`buffType 90` 池内 2 张 ⇒ 那一支可达。 |
| :692 | `BattleManager.TryUsingWaystone` | ③ UIstate 闸已由 `B8_A` 落地；④ 那半的口径本轮订正为「阵营 + isRemnant」（原写法是替代品）。 |

---

## ③ 改了哪些文件、哪些行

| 文件 | 位置 | 改动 | 行数变化 |
|---|---|---|---|
| `RuleEngine/Core/CardDef.cs` | **`:6529-6552`**（`CardDef` 类尾，紧跟 `IsKeyword` 之后） | 新增 `public bool IsWarlordEquivalent { get { return Type == "hero"; } }` + 判据 doc（10 行 doc + 1 行代码 + 空行，共 +24） | **+24**（8,615 → 8,639） |
| `RuleEngine/Core/UnitState.cs` | **`:2474-2508`** | 新增 `public bool IsAeldariRemnant`（原版 `SupportMethods.IsAeldariRemnant` 的引擎侧等价物）+ 判据 doc | 合计 **+75** |
| `RuleEngine/Core/UnitState.cs` | **`:2510-2547`** | 新增 `public void ClearAllKeywords()`（原版 `TransformIntoRemnant` 第 ③ 步的等价位）+ 判据 doc | （含在上面 +75 里） |

> ⚠️ **CardDef 的插入点是「类尾」但 `CardDef` 类并不在文件尾** —— 文件里 `CardDef`(48) → `KeywordTable`(6545) → `TraitReachability`(8540)。
> 第一版误插在 `TraitReachability` 里，`typecheck` 立刻报 `CS0708`（静态类里不能有实例成员）⇒ 已改到 `CardDef` 类真正的尾（`:6529`）。
> **下游落点实测零位移**：全账 A 行引用 `CardDef.cs` 的最大行号是 `:6519`（我插在 6529 之后）；`UnitState.cs` 最大是 `:2468`（我插在 2474 之后）。
> 实跑 `check_ledger_points --all-A`：**A 3,175 · 通过 2,986 · 漂移 189 · 失败 0** —— 我这两个文件相关的 **35 条引用全部 OK**（那 189 条漂移全在别人正在改的 `NetBattle.cs` / `CardView.cs` / `MainMenuRuntime.cs` / `ItemDrawer.cs` 一族）。
> 唯一的**旧指针**：`战斗逻辑驱动.tsv:1121` 的副引用 `CardDef.cs:7864-7867` **改前就已经偏**（`Waystone,` 当时在 `:7859`），本轮 +24 之后在 `:7883-7886` 一带 ⇒ 归那一行的所有者重钉，我没动（它是 B 行的副引用，闸不查）。

**零行为变化的三条读数**（本批只加未接线的成员 + doc）：
- `bash 工具/ruleprobe.sh check` → `解析差异 1 行`（`f8260229254a…｜Ethereal Supreme`）+ `哨兵合计：失败 1 条`（`PinnedParseMd5`），**与 B8_AM 开工时报的同一个状态**；
- `bash 工具/ruleprobe.sh play check` → 现算指纹 **`e11aa1c263e933dbdd67718133c8e5b2`**（**与 B8_AM 报的值逐字节相同**）· 基线 `a93ffb8f344517a9ac611ef06de17a3a`；
- `typecheck` 运行时 0 / 编辑器 0。

账补丁自查（离线复刻两个闸的判法 + 直接拿闸跑补丁）：
`check_ledger_points --all-A B8_AX.tsv` → **A=4 · 通过=4 · 漂移=0 · 失败=0（告警 3 条，都是 getter/散文括号）**；
`verify_annot` 复刻 → 4/4 PASS；`check_a_vs_diff` 复刻 → 4/4 clean；`check_patch_conflicts` → **无跨补丁重复键**；**11 字段 13/13 · LF · 裸 CR 0**。

---

## ④ 没查清的部分（如实记）

1. **`TransformIntoRemnant` 里那两段 5-int 写入**（`+0xa4..0xb4` 与 `+0x68..+0x78`，值来自 `GameStaticData + 0x298`）——
   `+0x298 = remnantMaxHealth` 是账里旧结论，但 `ObscuredInt__op_Implicit` 的反编译把 4 字节返回值读成了 5 个 word（栈上的噪声），
   **所以「残骸的护甲/攻击型也被覆盖成 0」这一条我没坐实**。今天不可观察（残骸的护甲在伤害那一跳被 `!u.IsRemnant` 整段跳过）。
   反证方向：`ResetToValuesInHand` 用**同一个形状**从 `rawCard + 0x9c` 抄 5 个 int 到 `+0xa4..0xb4`，
   而 `RawCardScript` 的字段序（`cardType +0x94` / `keyword +0x98` / `maxHealth +0x9c` / `armour +0xa0` / `meleeAttack +0xa4` / `rangeAttack +0xa8`）
   与这个偏移完全对得上 ⇒ `+0xa4..0xb4` 那一块**是「从 SO 镜像过来的 5 个属性」**。
2. **`buffType 90`（`AddTraitRemnant`）在翻面后的可见性**：`HasCurrentTrait` 的残骸支只认它，
   但「谁在什么时候给残骸挂 90」我没查（池内 2 张：`SAU36` + `eab54e4c…`）⇒ `:1182` 的 ② 那一半留着。
3. **`ClearAllKeywords` 与 `RemoveTraitSilently` 的一处结构差**：我们的 `RemoveAll` 会连带 `RevertGrantsOf`（把关键词带出来的属性增益一起撤），
   原版 `RemoveTraitSilently` **不碰 `activeEffects`**（`buffType 3` 授予的属性仍在）。翻面那一刻的**新对象**上这本账是空的 ⇒ 零差；
   但可达性（「翻面那一刻身上带 `buffType 3` 授予的 trait 的残骸有几张」）**我没算**。
4. **`SOR66 Armorium Cherub` 该怎么处置**（`pray` 只在原版的 `GetTraitForActiveAbilityRawCard` 推导里出现）——
   今天我们的引擎不推导、`pray` 也就没行为差异；但**表现层的 `ActivateIconAnim` / `ActivateTriggerTraitAnim` 是活路** ⇒ 图标可能少一条。要调度台裁「补推导」还是「记成卡表偏离」。
5. **`GetExtraAbilityTraits` 派生集合的 508 张 / 514 条**是我**按方法体推**的（`abilities[].trigger` + `hasActiveAbility` + 六档表），
   **没有反编译级第二来源**（本 build 里 `CheckCurrentTraits` 0 调用点，跑不出实况）⇒ 当「照搬后的验收尺子」用，不当判据。

---

## ⑤ 顺手发现（比多升一条 A 更值钱）

1. 🔴 **原版「残骸是 Aeldari 阵营 + isRemnant」这件事，与 trait 完全无关** —— 本轮把这条链的三跳都读了（见 §①）。
   连带结论：`RuleCore.CanCollectWaystone` 的那句 `Has(Waystone)` **本来就是替代品**；它在池内碰巧同集合，
   一旦残骸真的不继承 trait 就会**恒假** ⇒ 「灵族收灵魂石」整条路会断（用户可见）。**两步必须同批**。
2. 🔴 **`RawCardScript.CheckCurrentTraits` 会往【卡 SO】里追加 trait**（`CardTraits(+0x230)` 是 `List.Add` 改的共同资产），
   而它**全树 0 调用点** ⇒ 这是「原版作者写了、出货版没用上」的一条。配套的 `GetExtraAbilityTraits` 也是 0 调用点。
   ⚠️ **但它解释了卡表里一个统计现象**：`hasActiveAbility==1` 的 77 张里有 76 张的卡 SO **本来就带**那条阵营 trait
   —— 说明作者是「手工写进 SO」，而不是靠这条运行时推导。
3. 🔴 **`SOR66 Armorium Cherub` 是那 1 张反例**（Sororitas 的主动能力兵却没 `pray` trait）⇒ 池内唯一一处「推导会给、卡面没有」。
4. 🟡 **`CardDef` 类不在文件尾**（改后现读：`CardDef` 48–6554 / `KeywordTable` 6569–8562 / `TraitReachability` 8564–8637 + 命名空间尾 8639）。
   「追加到 `CardDef.cs` 类尾」这句话在**文件尾**和**类尾**之间会有 2,000 多行的差别，而且**插错类编不过（`CS0708`）** ⇒ 下一个接手的别照「文件尾」理解。
5. 🟡 **`rage` 与 `penitence` 是同一个 trait 名的两套写法**：卡 SO（`cards_truth`）写 `rage`，
   我们的解析层规范化成 `penitence`（`KeywordTable` 的 `Prefixes`）。逐张比 trait 名集时必须先归这一条，
   否则会得到 **5 张假不一致**（SOR8/SOR21/SOR10/SOR32/SOR27）。同族还有 `frenzied → Destroyer`（12 张）。
6. 🟡 **收灵魂石那条断言夹具的阵营是 `"Test"`**（`RuleEngineTest.cs:4062` + `Unit()` 助手 `:2392-2396`）——
   凡「按阵营判」的原版口径落到测试上，夹具都得显式给阵营，否则**改口径 = 把断言改红**（而且红的原因看着像实现坏了）。
7. 🟡 **`:1380` 不能判 `D`**：调度台裁定 1 的原话是「`D` 只留给『原版没有 / 用户拍板 / 服务端』；**『死代码』本身不足以判 D**」——
   所以这个「原版有实现、我们没照搬、且 0 调用点」的行，只能停在 B/C，**只能照搬**。

---

## ⑥ 给调度台的请求

### 🔴 R1（P1 · 转派 `RuleCore.cs` 的写手）· 三件同批，缺一件就断链

```csharp
// ── ① RuleCore.cs 的翻面支（现 :9565-9580）：造出 rem 之后、写进 Board[slot] 之前加一句 ──
var rem = new UnitState(u.Instance, false) { IsRemnant = true, … };
rem.ClearAllKeywords();          // ★ 新增：原版 TransformIntoRemnant.c:54-75 的第 ③ 步（倒序逐条 RemoveTraitSilently）
ps.Board[slot] = rem;

// ── ② RuleCore.cs:12028（CanCollectWaystone 里那一句）──
-   if (!u.IsRemnant || !u.Has(KeywordTable.Waystone)) return RuleCodes.ErrNotWaystone;
+   // 原版 SupportMethods.IsAeldariRemnant：rawCard.cardArmy(+0x2c)==30(SaimHann) ∧ isRemnant(+0x65)，不读 trait
+   if (!u.IsAeldariRemnant) return RuleCodes.ErrNotWaystone;

// ── ③ Editor/RuleEngineTest.cs:4062（收集夹具的阵营；否则 ② 一落地 :4089 那条 `RuleCodes.OK` 会红）──
-   var stone = Unit("Stone", 1, 1, 1, "Waystone");
+   var stone = new CardDef("Stone", "Stone", "unit", "", null, "SaimHann", 1, 1, 1, 0, new[] { "Waystone" });
```
> 补丁①的两条断言（草稿，`B8_AM` §④·a 已备，我复核过判据行号无误）：
> 翻面前 `live.Has(ferocity)` 真 → 翻面后 `rem.Has(ferocity)` **假**；灭自证 = 「一律 false」会让前提那条红。
> 补丁①②都会改引擎 ⇒ **`play check` 指纹应由调度台统一重钉**（现算 `e11aa1c2…`，基线 `a93ffb8f…`）。

### R2（判据已齐、只差照搬）· `CardDef` / `UnitState` 谁接手都行
- `RawCardScript.GetExtraAbilityTraits`（`:1380`）：25 对 `(trigger,trait)` 表已在 §②·b，语义 =「有该 trigger 就加该 trait」+「`hasActiveAbility` 再加阵营档」。**照搬后仍 0 可达**（唯一调用者 0 调用点）。
- `CardTraitCollection` 的四条（`:1126` / `:1128` / `:1129` / `:1130`）：判据已逐句读全、表已解出，**只缺一个运行时可观察量**（唯一消费者是表现层图标/动画）⇒ 要么给 `ruleprobe` 加一档，要么由表现层线出读数。

### R3（拍板）· `SOR66 Armorium Cherub`
原版 `GetTraitForActiveAbilityRawCard`（活路 = 图标/动画）会给它补 `pray(1185)`，卡表里没有。
要么补「按阵营推」这条路（会同时动表现层图标），要么把它记成卡表偏离（同 `cardface_fixes.json` 的 `_manual_*` 那一族）。

### R4（收口）· 「阵营 vs 关键词」三条口径收成一条
现在同时存在：`BattleDriver.TapBoardCard`（阵营·`SpiritStoneFaction`）· `RuleCore.CanCollectWaystone`（关键词）· **`UnitState.IsAeldariRemnant`（阵营·本轮新增）**。
同族还有 `ConditionEval.IsNecronRemnant`（阵营）与 `RuleCore.DestroyRemnants`（关键词）。建议 R1 落地时一并收口到 `UnitState` 那两个谓词。

### R5（重跑单）
`RuleEngineTest.Run`（本批 3 条 A 里有 2 条的观测量在它里面：`:5460-5465` / `:5852/5859`）·
合并补丁 `merge_ledger_parts` **务必带 `--only B8_AX`**（⛔ 不带参什么都不做 / 带了别的批会盖别人的手工修订）。
