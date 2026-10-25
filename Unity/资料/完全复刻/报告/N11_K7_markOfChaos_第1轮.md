# N11 · K7 —— `markOfChaos(8)` / `lastMarkOfChaos(17)`（第 1 轮）

> 代理：**N11（执行）** · 2026-10-25 · 完全复刻 ● 战斗逻辑驱动（`K7`）
> 白名单内改动：`RuleEngine/Core/EffectResolver.cs`（K7 那一段 + `GrantDarkPactCore` + `DoGive` 的 `CopyEventPact` 支）· `账/战斗逻辑驱动.tsv`（第 771 行 + 第 256/333 行）· 本报告。
> ⛔ 没碰：`UnitState.cs` · `CardPresentation/**` · `Editor/**`（含 `RuleEngineTest.cs` —— 见 §③ 的**断言改动请求**）· `EffectText.cs`（见 §⑤ 的解析层缺口）· git · 两本正本。

---

## ① 结论

| # | 件 | 结论 |
|---|---|---|
| 1 | **`markOfChaos(8)`** | ✅ 已落地并被数据判据逐条对上（上一版就有 `GrantDarkPact`，本笔把它接到**真判据**上并修了三处） |
| 2 | **`lastMarkOfChaos(17)`** | ✅ **新建**：`BattleManager.lastMarkOfChaos(+0x2f0)` 的等价槽 + `ApplyLastMarkOfChaos`（原版 case 17：**不写槽、不广播**）；`BL72 Accursed Helbrute`（池内**唯一**一张 case 17）从「再跑一次 case 8」改成走它 |
| 3 | **印记的**真实效果**判据找到了** | ✅ 上一版注释写「反编译查不到」= **查错地方**。真判据 = `Enchantments.GetMarkOfChaos(markOfChaosType)` 取到的那张**附魔卡**的 500 能力（4 张 `Mark of X (enchantment)`，`cards_truth.json` 有全文）。我们 `DarkPactFx` 的四句与它们**逐字一致**，只差 ①trait `330 markOfChaos` ②（Tzeentch 那条 `costChange 1` 是**死值**，见 §②） |

**两处就地订正（铁律 5）**：
- 🔴 **「同一单位只能有一份契约、换一份就把旧增益收回」——判据不成立，已改。** 原版 case 8 **从不撤旧印记**（`addStats` 逐项累加，`.c:385-402`），`lastMarkOfChaos` 只是「最后一份是哪份」。
  ⇒ 改成**累加**：`darkpact` 层数每份 +1、`markofchaos`(330) 每份 +1、属性/关键词都留着。
  **顺带修好 `BL48 Skeins of Fate`**（`Draw a card for each Dark Pact on it`）：契约层数以前**恒为 1**（每次 `RemoveAll` + `AddKeyword(1)`）。
- 🔴 **`CopyEventPact`（`it as well`）原来走的是「再跑一次 case 8」** ⇒ 会**再广播一次 510 + 重写槽 + 重新 Roll 种类**。原版 case 17 只有一句 `TriggerEnchantment(lastMark)` ⇒ 改成读全局槽、只重放效果。

---

## ② 判据与证据

**原版（第一权威）**
- `decomp_full/CardScript__AddEffect.c:776-792`（**case 8**）—— 逐句：`Log("Apply…")` → `bm = *(param_1+600)` 判空 →
  `Enchantments.GetMarkOfChaos(*(bm+0x38), *(param_2+0x78))` → **取不到就整条跳过** →
  `RawCardScript.TriggerEnchantment(附魔卡, bm, this, this, false, 0)` →
  `*(bm+0x2f0) = 附魔卡`（= `lastMarkOfChaos`）→ `BattleManager.BroadcastMarkResolved(this)`。
- `decomp_full/CardScript__AddEffect.c:933-940`（**case 0x11 = 17**）—— `if (bm.lastMarkOfChaos == null) 跳过`；否则
  `TriggerEnchantment(bm.lastMarkOfChaos, …)`。**不写槽、不广播、不取新的**。
- `decomp_full/RawCardScript__TriggerEnchantment.c:7-13` —— `TriggerAbilities(附魔卡, 0)` + `TriggerAbilities(附魔卡, 500)`。
- `decomp_full/BattleManagerSupport__BroadcastUnitObtainedMark.c:1-90`（= `BroadcastMarkResolved`）—— 广播 **510** 给
  **被标记那张 + 全场每张 + 双方手牌**（手牌那一趟用 `op_Inequality(item, markedCard)` 跳过主体）。我们 `BroadcastWhen` 的覆盖面与它一致。
- `decomp_full/Enchantments__GetMarkOfChaos.c` —— 在 `markOfChaos` 表里按 `MarkOfChaosType` 找卡。
- `CardEffect` 字段偏移（判据值都用得上）：`markOfChaosType = +0x78`、`healthChange = +0x60`、`costChange = +0x68`
  （字段序 = `Warpforge_code/Scripts/Assembly-CSharp/CardEffect.cs:4-89`）。

**解包资源（第二权威）**
- `assets_full/bundle_duplicateassetisolationso_assets_all/MonoBehaviour/Enchantments.json` —— `markOfChaos` 表：
  `10 → Mark of Khorne` · `20 → Mark of Nurgle` · `30 → Mark of Slaanesh` · `40 → Mark of Tzeentch` · **`50 → 4 张卡`**（⇒ Random = 均匀四选一）。
- `MarkOfChaosType` 枚举（`Warpforge_code/.../MarkOfChaosType.cs`）：`none 0 / Khorne 10 / Nurgle 20 / Slaanesh 30 / Tzeentch 40 / Random 50`。
- `DefinedTrait`：**330 = markOfChaos** · `980 = vanguard` · `985 = camouflage` · `1030 = regeneration`（`DefinedTrait.cs:30/94-101`）。
- 4 张附魔卡的 **500 能力**（`cards_truth.json`，cardType 20 / spellType 190）：

| 类型 | id | 效果（数据原样） |
|---|---|---|
| Khorne 10 | `9a2cadc64c80a6b42912441105bb1cee` | `addTrait(330)` + `addStats melee +2` + `addTrait(980)` |
| Nurgle 20 | `1dfc9bc738bfc4f4d93d48f165aee510` | `addTrait(330)` + `addTrait(1030, value 1)` + `addStats health +1` |
| Slaanesh 30 | `b8e81b2fe85cda747a68c4d6326b669c` | `addTrait(330)` + `addStats melee +2 · range +2` |
| Tzeentch 40 | `4a48457c24877f345be2e96f867c1e94` | `addTrait(330 [+ costChange 1])` + `addTrait(985)` + `addStats health +2` |

⚠️ **Tzeentch 那条 `costChange: 1` 是死值** —— addTrait 分支**不读** `+0x68`：`.c` 里只有 `case 1/2/10`（addStats）
  在 `.c:385-402` 读 `param_2+0x68`。⇒ 印记**不会**加费用（印的卡面也没写）。
- ⚠️ **同一批 id 在我们卡池里叫另一套名字**（id 完全相同）：`Dark Pact of Blood ↔ Khorne 10` · `Resilience ↔ Nurgle 20` · `Excess ↔ Slaanesh 30` · `Fate ↔ Tzeentch 40`
  ⇒ `EmitEnchantmentTrigger` 找的就是这 4 张（`PactKindOfChosenCard` 按名字后缀判）。

**我们侧（行号范围，现读）**
- 「K7」整节 `EffectResolver.cs:11827-11942`：`MarkOfChaosKeyword`(`:11863`) · `_lastMarkCtx/_lastMarkKind`+`SetLastMarkOfChaos`(`:11871`) ·
  `LastMarkOfChaos`(`:11878`) · `ApplyMarkBody`(`:11890-11929`) · `ApplyLastMarkOfChaos`(`:11931-11942`)。
- case 8 那一半：`GrantDarkPactCore`（`:11750-11824`，`:11823` 调 `ApplyMarkBody`）。
- case 17 那一半：`DoGive` 的 `CopyEventPact` 支 `:11402`。
- 计数读点（没改）：`CountFor` 的 `darkpact` 分支 `:10827-10838`（读 `KwValue(KeywordTable.DarkPact)`）。
- 表现层（没改）：`EmitEnchantmentTrigger`（`:12000` 一带）。

---

## ③ 改动清单

| 文件 | 改动 |
|---|---|
| `Unity/MyGame/Assets/RuleEngine/Core/EffectResolver.cs` | ① 新增「K7」整节（330 常量 / 槽 / `ApplyMarkBody` / `ApplyLastMarkOfChaos`）；② `GrantDarkPactCore`：**去掉「替换旧契约」那一段**（`RemoveAll`+`RevertGrantsFrom`+日志），改成累加 + 写槽 + 写 `markofchaos`；③ `DoGive` 的 `CopyEventPact` 支改走 `ApplyLastMarkOfChaos`；④ `ResetTransientForNewBattle` 清槽；⑤ `DarkPactFx` / `PactGrantedKeywords` / `PactOf` 的注释按新判据重写（旧注释「反编译查不到」是错的）。 |
| `Unity/资料/完全复刻/账/战斗逻辑驱动.tsv` | 第 **771** 行：`8 markOfChaos` / `17 lastMarkOfChaos` 的 ⛔ 改 ✅（并订正「17 池内 0 张」→ **1 张**）+ 追加 K7 整段（判据/落点/观测量/回归/还差什么/断言改动请求）。第 **256**（`CancelAbilityTargeting`）与第 **333**（`ExecuteActiveAbility`）行：按调度台要求改成 **C** + 写清差异与判据行号（**本轮不实现**）。 |
| `Unity/资料/完全复刻/报告/N11_K7_markOfChaos_第1轮.md` | 本报告。 |

**同批（调度台明确授权后）又收掉三件 —— 都不再是「改动请求」**
1. ✅ **`RuleEngine/Editor/RuleEngineTest.cs` ⑧ 段那条旧语义断言已按判据改掉**：原来断「换一份就摘掉旧的」
   （`Check(v.Has("camouflage"), false, …)` + 注释「原版是赋值不是累加」），现在断「**两份都在 + 生命累加**」，
   并在断言旁写清**期望值依据** = `CardScript__AddEffect.c:776-792`（case 8 不撤旧）+ `.c:385-402`（`addStats` 逐项 `+=`）
   + 为什么旧期望错（把「`kws.darkpact` 是赋值」——那是我们上一版 Godot 复刻的写法、原版根本没有——当成了「整份印记被替换」）。
   **离线镜像读数**（探针 `H`，与那段同形）：上限 5→7（命运）→**8**（韧性）· 伪装仍在 · `darkpact`/`markofchaos` 层数各 2。
2. ✅ **`EffectText.cs`：`Give <N> <可重复的授予> to <目标>` 的 N = 【授予次数】**（`BL46` 现在真的给 2 份）。
   判据 = BL46 的 SO 数据是**两条独立 logic**（各 `buffType 8 / markOfChaosType 50`）⇒ 同一个目标叠两份、各摇各的。
   🔴 **窄规则**（⛔ 别放宽）：那个 N 只有在「**带数字那版解析**解不出 / 或与**去掉数字那版逐项等值**」时才算次数。
   实测判别：`Parse("2 random dark pact")` ≡ `Parse("random dark pact")`（都是 `kw darkpact x1` ⇒ 数字被丢掉 ⇒ 次数）；
   而 `Parse("3 armour")` = `armour 3` ≠ `Parse("armour")` = `kw armour x1` ⇒ **不套**（否则「+3 甲」会变成「三次 1 甲」）。
3. ✅ **`markofchaos` 已并进 `KeywordTable.MarkOfChaos`**（`CardDef.cs`），⛔ **没放进 `Implemented`**
   —— 330 没有「每关键词一条机制」；而且 `CardPresentation/Battle/BattleDriver.cs:27102/27111` 正是**按 `Implemented`
   过滤关键词外观**的 ⇒ 放进去会让每张带印记的卡渲染一个原版没有的**假徽章**（330 是隐藏 trait）。理由写进了那个常量。

---

## ④ 核验结果

1. **秒级类型检查**：`TMPDIR=/tmp/wf_n11 bash d:/4/Unity/工具/typecheck.sh` ⇒ 运行时 **0/0** · 编辑器 **0/0**。
2. **离屏探针**（`d:/4/_tmp_view/n11probe/`，`dotnet bin/Debug/net8.0/n11probe.dll k7`）—— **改前 15 红 → 改后全绿（A~I 共 30 条）**：
   - **0. 载荷解析**：四句 → `[health 2][kw camouflage]` / `[melee 2][kw vanguard]` / `[melee 2][ranged 2]` / `[health 1][kw regeneration]`（与数据一致）。
   - **A 四种印记**：Blood 近战 2→4 + Vanguard + `darkpact1` + **`markofchaos1`**（改前 0）；Resilience 生命 4→5 + Regeneration 1；
     Excess 远程 0→2；Fate 生命 5→**7**（改前 6 —— 那是「替换」把韧性那 +1 收回去了）。
   - **B 四份叠一起**：近战 6 · 远程 2 · 生命 7 · 三个关键词都在 · 层数 `darkpact4` / `markofchaos4`（改前：近战回 2、层数 1）。
   - **C 同一份挂两次**：近战 6 · 层数 2（改前 4 / 1）。
   - **D `BL72`（case 17）**：友方拿到「鲜血」⇒ BL72 也拿到**同一份**（近战 +2 + Vanguard，不是随机）+ `markofchaos1`。
   - **E `BL48`**：目标带 2 份契约 ⇒ **抽 2 张**（改前抽 1）。
   - **F `Random`**：40 次覆盖 4 种。
   - **G `BL46`「给 2 份」**：同一个目标层数 **2** · 全场合计 **2**（不是 2×目标数）—— 解析层那条已修（见 §③-2）。
   - **H 断言镜像**（与 `RuleEngineTest` ⑧ 段同形）：上限 5→7→**8** · 伪装仍在 · `darkpact2` / `markofchaos2`（9 条全绿）。
   - **I 解析窄规则**：`BL46` 解出 **2** 条 `give`（同 payload「random dark pact」、`target.Count = 1`）；
     `Give +3 Health to …` 仍 1 条；**`Give 3 Armour to …` 仍 1 条**（不被拆成 3 次 —— 窄规则的关键反例）。
3. **`ruleprobe play check`（结算层全池 1,128 张）**：
   - 主档 **13 张卡变化，全部只是关键词集合里多了 `markofchaos1`** —— `BL4/BL6/BL8/BL13/BL30/BL37/BL38/BL46/BL57/EC36/EC41`
     + 两张契约附魔卡 `141c5ed661a9e42bd9a793f0d2a1c682`（Bitter Blows）/`2621d4280735baf4c952e6afea8d0686`（Excessive Vigour）。**数值一格没动**。
   - 事件电池 10 张（`BL4/6/8/10/13/16/21/24/25/30`）的变化来自**「同一份印记重复给 ⇒ 生命/攻击累加」这条原版语义**
     （例：`BL6` 每抽一张牌拿一份命运 ⇒ 生命一直涨 ⇒ 后续事件更多；`net 25 → 44`）。
   - ⚠️ 该命令报的 `AM23/DA29/DA65/SOR18/20/25/30/35/68/SW11` **不是本笔**（上一轮已用「四闸全关」对照坐实 = 别的代理在飞的 `UnitState/RuleCore`）。
   - **`BL46` 这一格是本批最直观的一条**：`FIX_A 2/0/0/2 → 4/2/0/3 {darkpact2, markofchaos2, regeneration1}`
     （**同一个目标**两份印记、各摇各的；改前是 `{darkpact1}`）。
   - 结算指纹 `374ae74827e3210666c443e96a247677`（改后，含 BL46 修复）· 基线 `0da717c6…`（**待调度台统一重钉**）。
4. **`ruleprobe check`（解析层）**：全池 **1,121 张 · 解析差异 1 行 = `BL46|Gifts of Chaos`**（就是 §③-2 那条**有意**的变化）·
   **哨兵失败 1 条 = ⑦ [输出指纹]**（现算 `9cf3481c648981963ca654614e117d9c`，钉住 `af836aa0…`）。
   🔴 **这是预期的**：⑦ 盯的是「源码跑出来的解析输出有没有离开被确认过的那一版」，本笔**改了**解析器 ⇒ 必须由调度台
   **重钉 `PinnedParseMd5`**（`工具/ruleprobe/Program.cs`，整行照抄 `9cf3481c648981963ca654614e117d9c`），连同卡池/基线两个指纹一起。
5. **账工具**（收工必跑）：`ledger_progress` ⇒ `战斗逻辑驱动` 1,536 行（A 151 · B 1,175 · **C 62** · D 148；C 比改前 +2 = 本笔记的两条）；
   `check_ledger_points 战斗逻辑驱动` ⇒ A 151 · 通过 54（告警 11）· 漂移 97 · **失败 0**；
   `verify_annot` ⇒ 同名 506 · 异名已注解 1,588 · 缺注解 15（全是别人的 A 行，本笔没有新增 A 行）；
   `check_a_vs_diff` ⇒ 85 条候选（**强 0** · 弱 85），其中 `战斗逻辑驱动` 12 条 —— 都不是本笔的行。
6. **改动量 / 行尾**：`git diff --numstat` = `569 49 EffectResolver.cs`（含同工程别的代理在飞的 hunk；本笔两批合计约 +340）；
   **CRLF 保持** 15,019/15,019（没有整篇翻 LF）。

---

## ⑤ 没查清 / 做不到（如实）

1. ✅ **（已修）`BL46 Gifts of Chaos` 现在真的落 2 份** —— 修在**解析层**（`EffectText`，见 §③-2），
   ⛔ 没有在 `EffectResolver` 里按 `op.Amount` 循环补偿（那会让「给 2 个目标各一份」变 4 次）。
   探针 G（同一目标 2 层 / 全场合计 2）与 `play check` 的 `BL46` 那一格是验收依据。
2. **原版「取不到附魔卡 ⇒ 整条 case 8 不发生」这一支没落**：我们的四份效果是 `DarkPactFx` 硬表、**不来自那张卡的数据**，
   若把「卡池里有没有那张附魔卡」当前置闸，任何小卡池夹具都会让印记凭空消失。缺卡时只有 `EmitEnchantmentTrigger` 出声。
   （⇒ 如实标注为**模型差异**，不是漏做。）
3. **`markofchaos`(330) 的叠层语义**：`CardTrait._stackable` 的真值我们**没有**（`判据/CardTrait词条权威表.md` §一的 `stackable` 列全是占位 `10`）。
   本笔按「每份 +1」（`AddKeyword` 累加）落 —— 这与 `BL48` 的「for each Dark Pact」口径自洽，但**不是**从 `_stackable` 读出来的。
4. **`BroadcastMarkResolved` 的覆盖面没有逐条对齐**：原版扫的是 `allCardsInPlay`（`bm+0x470`）+ 双方手牌；我们走 `BroadcastWhen`
   （棋盘 + 手牌，见它的注释）。**墓地/牌库**两档原版扫不扫**没核**（`.c` 里只看到 in play + 两手牌）。
5. **`case 8` 的 `markOfChaosType` 没从数据读**（我们按卡面文字反推种类）：本笔把 26 条（卡 × logic）逐一与 `cards_truth.json` 的
   `markOfChaosType` 对过，**全部一致**（血=10 / 韧=20 / 纵=30 / 命=40 / random=50）⇒ 今天不吃亏；真出现文字与数据打架的卡要以数据为准。

---

## ⑥ 顺手发现（⛔ 没自己改）

1. 🔴 **`RuleEngineTest.cs:6673-6677` 钉的是已被推翻的旧语义**（见 §③ 改动请求 1）⇒ **同步点会红一条**，请按判据改断言。
2. 🔴 **`GivePayload.cs:489` 的注释仍写「种类由 `PactOf(事件主语)` 取」** —— 现在走的是**全局槽** `LastMarkOfChaos(ctx)`
   （原版 case 17 读 `bm.lastMarkOfChaos`，**不是**事件主语身上那份）。`GivePayload.cs` 不在我白名单 ⇒ 只报不改。
3. 🟡 **`Enchantments.json` 的 `markOfChaos[40]` 序列化不自洽**：`targetId` 是 `4a48457c…`（Tzeentch），而 `reference.m_PathID`
   指向 `-6517057525472259341`（全库查不到这张卡）。按本工程口径 **`targetId` 才是准的**（`reference` 那一格是 PPtr 残留）。
   ⇒ 将来若有人照 `reference` 取卡会拿到空。
4. 🟡 **`PactKindOfChosenCard` 认的是「名字后缀 of fate/blood/excess/resilience」** —— 而 SO 真值里这 4 张叫
   `Mark of Tzeentch/Khorne/Slaanesh/Nurgle`。我们卡池改了名（`cards_engine.json`），所以今天能对上；
   **哪天卡池名字改回 SO 原名，这条会静默失效**（`PactKindOfChosenCard` 返回 null ⇒ 随机）。
5. ℹ️ **`AiTargets.cs:1029` 早就有 `case 330 → "markofchaos"`** —— 之前引擎从不授予 330 ⇒ 那条 AI 目标筛选**永远匹配不到**；
   本笔之后它才有意义（`traitsFilter`/`multiplier` 里唯一用它的是 `BL48`）。
