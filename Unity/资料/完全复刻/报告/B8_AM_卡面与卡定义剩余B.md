# B8_AM · `CardView` / `UnitState` / `CardDef` 剩下的 B 行（卡面与卡定义）

> 执行代理 **AM**（代号 `B8_AM`）· 2026-10-26
> 白名单 = `CardPresentation/Core/CardView.cs` · `RuleEngine/Core/UnitState.cs` · `RuleEngine/Core/CardDef.cs` · 账补丁 `账/_parts/B8_AM.tsv` · 本报告
> 红线执行：**没跑 Unity / 批处理**（只跑 `TMPDIR=/tmp/wf_b8am 工具/typecheck.sh`）· **没动 git** · **没改主账**（只写 `_parts/B8_AM.tsv`）· **没改正本** · **没越白名单** · **没派子代理**
> `typecheck`：**运行时 0 / 编辑器 0**（改完两处文档注释后各跑一次）
> 行尾：`CardDef.cs` **8615 行 / LF / 裸 CR 0**（改前 8615）· `UnitState.cs` **2474 行 / LF / 裸 CR 0**（改前 2474）⇒ **零行数变化**

---

## ① 结论

| 项 | 数 |
|---|---|
| 账切片现读（`ledger_slice --status B`） | `CardView` **119 行**（真正落点指到 `CardView.cs:<行号>` 的 **19** 行，其中 18 行 `B8_AA` 已逐条复核过）· `UnitState` **36 行** · `CardDef` **39 行** |
| **B → A** | **12** |
| **B → C**（查出真缺口） | **3** |
| 保持 **B**（本轮补证据 + 就地订正） | **5** |
| 改动的 `.cs` | **2 个文件、各 1 处文档注释、净 **+0** 行**（`UnitState.cs` / `CardDef.cs`） |
| 账补丁 | `账/_parts/B8_AM.tsv` **20 行**（11 列 · LF · 裸 CR 0 · 跨补丁重复键 0） |

**一句话**：`UnitState`/`CardDef` 那批 B 行的瓶颈**不是「没有实现」**，而是**「观测量没写进账」** ——
`RuleEngineTest.Run` 里早就有一大片现成断言（`Prayed` 6 条 · 残骸 5 条 · 标记光 4 条 · 替代能力 4 条 · 誓约跨回合 2 条），
`ruleprobe` 的 `traits cond=` 那一档又能把「trigger 存在性」逐卡读出来。本批做的是
**① 把这些既有读数接回账（12 条 A 的观测量全部来自现成断言 / 现成探针输出）· ② 把查出真缺口的三条降 C 并写清根因**。

🔴 **本轮最要紧的一条**：`:1068 TransformIntoRemnant` —— 原版翻面时**倒序遍历 `CurrentTraits` 逐条 `RemoveTraitSilently`（残骸一条 trait 都不剩）**，
而我们是**新造 `UnitState`**、构造器把卡面关键词全拷进来 ⇒ **残骸带着全部 trait**。
它与 `:1182 HasCurrentTrait` 那个「未来残骸只认 `buffType 90`」的支是**同一件事的两面**（两处自洽），一起降 C。
⚠️ 但**不能单独修**：`RuleCore.CanCollectWaystone` 现在读 `IsRemnant && Has(waystone)`，而原版翻面后 waystone 已被摘
⇒ 原版「点击残骸收集」**不可能**也走 `HasCurrentTrait(waystone)` ⇒ 修之前必须先定「收集读哪个口」（§⑦ R3）。

---

## ② 逐条去向

### ②·a 升 A（12）

| # | 账行 | 原版方法 | 判据（🥇 = `decomp_full/*.c`） | 我们的落点 | 观测量（两侧） |
|---|---|---|---|---|---|
| 1 | 战斗逻辑驱动:78 | `AbilityLogic.MatchingTrigger` | `AbilityLogic__MatchingTrigger.c:1-31` | `CardDef.cs:2462-2466` | `traits cond=241,242` ⇒ 我们 **29 / 26**；`cards_truth` 的 `abilities[].trigger` 600/605 ⇒ **29 / 26** |
| 2 | :830 | `CardScript.DoesAbilityRequireLoading` | `CardScript__DoesAbilityRequireLoading.c:1-41` | `ConditionEval.cs:461-480`（`case 280`） | `traits cond=280` = 真 0 / 假 1127 / 判不了 1；`activeNeedsLoading` 1128 张一律 0 |
| 3 | :831 | `…RequireLoadingAndIsNormalSiege` | `…AndIsNormalSiege.c:1-56` | 同上 | 同上（`loadingAbilityLogic` 列缺，见 §⑤-1） |
| 4 | :906 | `CardScript.IsTheSameTurnPlayed` | `.c:1-37` + 写点 `ResolveCardPlayed.c:55` | `UnitState.cs:771` + `RuleCore.cs:3948/4770` | `RuleEngineTest:17487/17489` 誓约跨回合 ⇒ `ErrExhausted`；对照组 `:17496` ⇒ `OK` |
| 5 | :1021 | `CardScript.SetFutureRemnant` | `.c:1-8`（= `set_isRemnant`） | `UnitState.cs:410` + `RuleCore.cs:9567` | `RuleEngineTest:4073/4075/4077`（残骸 / 1 血 / 攻 0） |
| 6 | :1025 | `CardScript.SetLeavingRemnant` | `.c:1-8` + 清零点 `TransformFromRemnant.c:13` | `UnitState.cs:425` + `CounterAttack.cs:85/133` | `RuleEngineTest:4189 / :4953`（翻回来不再是残骸） |
| 7 | :1027 | `CardScript.SetPray` | `.c:1-8`（= `set_isPraying`）+ 调用点 `ResolveTriggerPray.c` | `UnitState.cs:351` + `EffectResolver.cs:14762` + `RuleCore.cs:2147/3059/8624` | `RuleEngineTest:2182/2183/2188/2189/2199/2202` 六条 |
| 8 | :1029 | `CardScript.SetRemoveMarkerlight` | `.c:1-83` | `UnitState.cs:2096` + `RuleCore.cs:6991`（置位）/ `2347-2360`（回合末摘） | `RuleEngineTest:6495/6498/6505/6509` 四条 |
| 9 | :1117 | `CardScript.UseSurvivor` | `.c:1-90` | `UnitState.cs:573-582` + `RuleCore.cs:9360` | `RuleEngineTest:33610-33613 / 34414 / 34695`（夹具；池内 0 张 survivor） |
| 10 | :1174 | `EntityScript.GetCurrentTraitValue` | `.c:1-36` | `CardDef.cs:6458-6463`（`GetDefaultTraitValue`） | `traits BL27` = **1** vs `cardTraits._traitValue` **1**；`traits SW21` = **0/0** vs **0/0** |
| 11 | :1175 | `…GetCurrentTraitValueWithModifiers` | `.c:1-71`（收尾 `:68-71` 夹 0） | `UnitState.cs:1074-1078`（`KwValue`）+ `:2468-2472`（`RawKw`） | `traits BL27` 1=1 · `traits SW21` 1 vs 0（已知兜底差异）· `layers ASH34` `{armour:2, talent:1}` |
| 12 | :1370 | `RawCardScript.GetActiveAbility` | `.c:1-19` | `CardDef.cs:282-303`（`UsesAlternativeAbility`） | `RuleEngineTest:27323-27352` `TestA20AlternativeAbilityBranch` 两个方向各一条 |

### ②·b 降 C（3，查出真缺口）

| 账行 | 方法 | 缺口 | 等级 |
|---|---|---|---|
| 战斗逻辑驱动:1067 | `CardScript.TransformFromRemnant` | 原版**原地翻位**（`*(u16*)(+0x65)=0` 一次清两个标志位）；我们**换对象** ⇒ 净结果同，但**原版属性由谁恢复**在方法体里读不到（落在 `BattleCardUI.ChangeCardToRemnant`，未读） | P2 |
| :1068 | `CardScript.TransformIntoRemnant` | 🔴 **缺「倒序逐条 `RemoveTraitSilently` 摘光 trait」这一步** ⇒ 我们的残骸带着卡面全部 trait | P1 |
| :1182 | `EntityScript.HasCurrentTrait` | 缺「未来残骸只认 `buffType 90`」那一支；根因同上（残骸没摘 trait）。池内 `buffType==90` **2 张**（`SAU36` + `eab54e4c…`）⇒ 原版那一支可达 | P1 |

### ②·c 保持 B（5，本轮补证据 / 就地订正）

| 账行 | 方法 | 卡在哪 |
|---|---|---|
| :961 | `CardScript.RemoveAttackThisTurn` | **真缺口、方向待定**：原版在强制攻击**入队那一刻** `--`（守卫 `>0`）并记 `wasFirst`；我们**无 -- 入口**且强制攻击也走 `RuleCore.DeclareAttack`（`AttacksThisTurn++`）⇒ 净效果与原版两条可能读法**都不同**。到底哪种取决于 `_ResolveForceAttack_d__504`（122 行协程，**未读全**） |
| :979 | `RemoveTraitSilently` | 🔴 就地订正：`+0x130` 是 **`CurrentShowingTraits`**（不是「第二份 buffed 列表」）⇒ 原版只删「运行时 trait 表 + 图标表」、**不碰 activeEffects**；我们的**合并字典**结构上表达不了「摘卡面那份、保留授予那份」 |
| :1408 | `RawCardScript.HasTriggerOnceAbility` | 池内 `trigger==200` 的卡 **0 张** ⇒ 两侧恒 false，**无可见行为可钉**（只有数据侧计数） |
| :1167 | `EntityScript.GetActiveAbilityCard` | 可达性已核（`buffType 40 replaceAbility` **0 条** ⇒ 原版恒回自己的 rawCard）；**读数口缺**（`ruleprobe` 无该模式） |
| 其他_基础设施:258 | `GameStaticData.IsWarlordEquivalent` | 数据侧已逐张证等价（`cardType==10` 池内 56 / `type=="hero"` 56，且那 10 张池外卡是天然反例）；**缺具名谓词**，加它会挪 `CardDef.cs` 的下游落点 |

---

## ③ 改了哪些文件、哪些行（净 **+0** 行）

| 文件 | 位置 | 改动 | 行数变化 |
|---|---|---|---|
| `RuleEngine/Core/UnitState.cs` | **`:571`**（`UseSurvivor` 的 doc 末段，紧挨方法声明 `:573`） | 就地并入 `🔶 自己设计（非原版）` 标记：函数体第一句 `if (sv < 1) return 0;` 是**我们加的护栏**（原版无早退，`CurrentSurvivor==0` 时会当场判死）；写清「这一格本池不可达 / 我们选择不动作 / 谁定的」 | **+0**（2474 → 2474） |
| `RuleEngine/Core/CardDef.cs` | **`:6021`**（`Keywords` 属性的 doc） | 就地并入 `🔶 自己设计（非原版）` 标记：零值词兜底成 **1** 是我们的选择（原版 `_traitValue` 是 0），并写清「只有『有没有』那一侧能与原版逐值对齐」 | **+0**（8615 → 8615） |

> 两处都是**就地改写已有注释行**（`18·b`：零行数变化），改完全文件行数逐字节核过；`CardDef.cs` 的账落点（最远 `:6518`）与 `UnitState.cs` 的（`:2468`）**一格都没位移**。

---

## ④ 断言草稿（`Editor/BattleScene.cs` / `Editor/RuleEngineTest*.cs` 都不在我白名单，只写草稿）

### ④·a `:1068` 残骸不继承 trait（修完才写；这是本轮最该钉的一条）

```csharp
// 判据 = decomp_full/CardScript__TransformIntoRemnant.c:52-70（倒序遍历 CurrentTraits 逐条 RemoveTraitSilently）
// 夹具：一张带 ferocity 的部队（例 AM72/UM14 那一族）被打死
CheckTrue(live.Has(KeywordTable.Ferocity), "（前提）翻面前它带着 ferocity");
CheckTrue(rem != null && rem.IsRemnant, "（前提）它翻成残骸了");
CheckTrue(!rem.Has(KeywordTable.Ferocity),
          "★ 残骸【不继承】卡面 trait（原版翻面时逐条 RemoveTraitSilently 摘光）"
          + "｜🧨 不摘 ⇒ 本行实得 True（= 当前实现）");
// 灭自证（结构上互不代偿）：上面两条是「翻面前 true / 翻面后 false」，
//   「一律 false」的实现会让前提那条红；「一律 true」会让 ★ 那条红。
```

### ④·b `:961` 强制攻击不吃配额（**方向定下来之后**才写）

```csharp
int q0 = atk.AttacksThisTurn;
RuleCore.DeclareAttack(ctx, 0, slot, 1, tgt, forced: true);
// 若裁定「原版净 0」：
Check(atk.AttacksThisTurn, q0, "★ 强制攻击【不消耗】攻击配额（原版 AddForceAttack 入队时先 -- 一次）");
// 🧨 把 DoForceAttack 的 refund 去掉 ⇒ 本行实得 q0+1
```

### ④·c `CardView` 那批（本批**未动**，仍按 `B8_AA` §③ 的 11 条草稿走）
`B8_AA` 已补 11 条 `Check(...)` 到 `BattleScene.cs` 并给了行号位移表；本轮`CardView.cs` **零改动**，
`B8_N` 报的那 52 条 `CardView.cs` 漂移**不会因本批增加**。

---

## ⑤ 没查清的部分（如实记）

1. **`:831` 的 `loadingAbilityLogic(+0x30)`** —— 我们**全程没采这一列**（`cards_engine.json` 里没有对应键）⇒ 「普通围城 / 装填围城」两档**判不开**。
   今天不可观测（`needsLoading` 两侧恒 0 ⇒ 两档都是 false），但**这是真缺口**，已在账的「差异」列写明、并判 A 的前提是「可观察面等价」。
   补法：生成器加一列 `activeLoadingAbilityLogic`（布尔「非 null」即可）→ `CardDef` 加一格 → `ConditionEval` case 280 分支。
2. **`:961` 的方向** —— `BattleManager._ResolveForceAttack_d__504`（122 行协程）**没读全**，它决 ++ 不 ++；不读全就无法判「我们的强制攻击到底多消耗了还是少消耗了」。
3. **`:1067` 的属性恢复** —— `BattleCardUI.ChangeCardToRemnant` 的第三参那一侧是「换立绘」还是「连数据一起换」**没读**（原版 `.c:13` 只传 `ui + 0 + param_2`）。
4. **`:1068` 与「点击残骸收集」的成对口径** —— 见 §①。
5. **`:1175` 的 `armour` 归属** —— `cards_truth.json` 1,351 张里 `_trait==1020` 只有 **5** 张，而 `armour > 0` **81** 张 ⇒ 对那 **76** 张「armour 是字段不是 trait」。这与 `B8_Q` 的结论一致，但**「那 5 张同时有字段与 trait 的卡，两者数值是否总是相等」没逐张核**（Q 核的是「81/81 字段与卡面 `Armour N` 相等」）。
6. **`:78` 的「同一 trigger 多条 ability」那一层** —— 我们把 triggers 去重成集合 ⇒ 表达不了「这条 trigger 有 3 条 ability、要按 priority 依次跑」。今天原版三个消费者都不需要它，但 `GetAbilitiesForTriggerRawcard`（账 `:1369`，仍 B）要的就是**那个列表**。

---

## ⑥ 顺手发现（比多升一条 A 更有价值的几条）

1. 🔴 **`CardScript.TransformIntoRemnant` 会「摘光残骸的 trait」，我们没有** —— 见 §①。这条同时解释了 `EntityScript.HasCurrentTrait` 里那个看着很怪的「未来残骸只认 `buffType 90`」分支（两处自洽）。**它不是「原版有 bug」，是我们少了一步。**
2. 🔴 **`:979 RemoveTraitSilently` 的第二张表叫 `CurrentShowingTraits`（`+0x130`），不是「buffed 列表」** —— 账里原来没写清；这直接决定「原版摘 trait 时**不碰** `activeEffects`」这条机制差异。
3. 🟢 **`ruleprobe traits cond=NNN` 是一条被低估的读数口** —— 它跑**产品代码** `ConditionEval.MeetsCard`，可以把「trigger 存在性」「siege 门」这类**看似没有可观察量**的行一次读出两侧数值（本批 3 条 A 靠它）。
   同样 `traits q=<trait>` / `traits <id>` 直接给 `CardTraits` / `GetDefaultTraitValue` / `KeywordId` 逐卡读数。
   **建议**：以后判「引擎行没有可观察量」之前，先 `bash 工具/ruleprobe.sh traits cond=<号>` 试一次。
4. 🟡 **`:1408 HasTriggerOnceAbility` / `:1445 UpdateTriggerOnce` / `:809 CheckReplacedAbility` / `:979` 的 `survivorSpent` 静默摘 —— 池内全是 0 张可达**（`trigger 200` = 0 张 · `buffType 40` = 0 条 · `survivor` 关键词 0 张）。这不是「我们没做」，是**原版数据事实**；按铁律 11 例外① 处理，**不欠账**。
5. 🔴 **`RuleCode` 侧的 `Prayed` 清零点是我们补的，`RuleCore.cs:2121-2147` 那一段还没有 `🔶 自己设计` 标记** —— 原版 `set_isPraying(…,0)` 全树 0 命中（清零点可能在 724 条无方法体清单里）。`RuleCore.cs` **不在我白名单**，故只报不改（§⑦ R5）。
6. 🟡 **`GameStaticData.IsWarlordEquivalent` 的数据侧等价已证**：`cards_truth` 的 `cardType==10` 66 张里 56 张在池内、**56/56** 的 `cards_engine.type == "hero"`；另 10 张是**池外 GUID 卡** ⇒ 「池内 ⟺ hero」逐张成立。缺的只是一个具名谓词。
7. 🟡 **账里两条旧结论被本轮推翻/收紧**：`:961` 的备注写「池内 1 张带 cantattack」—— 那是**别的机制**，不能当本行的观测量（已在补丁里订正）；`:1027` 的差异写「我们同样只置真、从不复位」—— **已过期**（我们现在有 4 处清零）。

---

## ⑦ 给调度台的请求（都不在本代理白名单）

| # | 文件 | 需要什么 | 为什么 |
|---|---|---|---|
| **R1** | `RuleEngine/Editor/RuleEngineTest*.cs` | 按 §④·a 补「残骸不继承 trait」2 条断言（**先修 R3 再写**） | `:1068` 修补的验收口 |
| **R2** | `RuleEngine/Core/EffectResolver.cs` `DoForceAttack` | 补「强制攻击退回配额」那一步（照 `BattleManager__AddForceAttack.c:36-41`）。⚠️ **先把 `_ResolveForceAttack_d__504` 读完**，否则方向会做反 | `:961` 的真缺口 |
| **R3** | 调度台裁定 + `RuleEngine/Core/RuleCore.cs` | **成对定**：① 残骸翻面时摘光 trait（`CleanupDeaths` 翻面支 + `UnitState` 需要一个 `ClearAllKeywords()` 之类的口）② `CanCollectWaystone` 改读**卡面** trait（`Card.Keywords`/`HasDefaultTrait`）而不是运行时 `Has` | 不先定 ②，做 ① 会把路标石收集改坏 |
| **R4** | `RuleEngine/Core/CardDef.cs` 类尾 | 加 `public bool IsWarlordEquivalent { get { return Type == "hero"; } }` + 判据注释 | `其他_基础设施:258` 升 A 的最后一格。⚠️ **排在「没有 `.cs` 写手」的窗口**（`CardDef.cs` 的账落点最远 `:6518`，类尾插入会让 `:7864` 那条陈旧落点漂移） |
| **R5** | `RuleEngine/Core/RuleCore.cs:2121-2147` | 给「我们补的 `Prayed` 清零」加 `🔶 自己设计（非原版）` 标记 | 铁律 18；那段现在读起来像原版 |
| **R6** | `工具/ruleprobe/` | 加一个 `ruleprobe branch <id>`（读 `UsesAlternativeAbility` / `HasActiveAbility` / needsLoading 三格） | `:1370` 的探针侧读数（今天只能靠 `RuleEngineTest` 断言） |
| **R7** | 调度台 | **重跑单**：`RuleEngineTest.Run`（本批 12 条 A 里有 10 条的观测量在它里面）· 合并补丁 `merge_ledger_parts --only B8_AM`（⛔ 不带参） | —— |

### 复跑时要知道的两条现状（**都不是本代理造成的**）

- `ruleprobe check`：**rc=1**，`解析差异 1 行`（`f8260229254a…｜Ethereal Supreme`）+ `哨兵合计：失败 1 条`（`PinnedParseMd5` 期望 `18439d965d14288fe28cfbe5986dc445`）。
  我开工时（07:39）就是这个状态；本代理**只改了文档注释**，`解析差异` 与它无关。
- `ruleprobe play check`：**rc=1**，结算指纹现算 = **`e11aa1c263e933dbdd67718133c8e5b2`** · 基线 = `a93ffb8f344517a9ac611ef06de17a3a`（**不同**）。
  `B8_Q` 开工时报的是 `350c16b2…` ⇒ 此后又有别的代理改了引擎（部署 / 光环重算那几条路）。**请调度台统一重钉。**
- `check_patch_conflicts.py`：**无跨补丁重复键**（现读 5 份补丁）。
- 本代理补丁自查（离线复刻两个闸的判法）：**11 字段 20/20 · A 档缺口词命中 0 · 缺注解 0 · LF 20 行 / CRLF 0 / 裸 CR 0**。
