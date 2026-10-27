# B9_W21SKILL — 技能描述取键（`Card_Abilities/*`）三处偏离修正

> 派活包说「上一路 `B9_W13CDW` 已把判据查清、只差动手」—— **成立**，本笔只动手，**没有重查判据**（只复核了结论要引用的那几行出处）。
> 白名单内唯一代码文件：`Unity/MyGame/Assets/CardPresentation/Battle/SkillPanel.cs`。⛔ 没碰任何别的 `.cs` / 场景 / 账主表。

---

## 一、结论

1. **三处取键偏离全部改完**：
   ① 前缀 `Card_Description/` → **`Card_Abilities/`**（两处调用点 + 三处头注）；
   ② **`RemovingSiege` 那一支已按原版实现**（键 = `Card_Abilities/<id>`，**后缀 = 空串**；值里带 `[[reload]]` 时按原版**顺序**剥 4 个变体）；
   ③ **裸键档改成先查 `Card_Abilities/<id>`**（原版 `Desc()`），**缺键才回** `CardText.EffectSentence`（我们改前一直在用的模板拼句）。
2. **顺带补齐了原版 `SupportMethods.ModifyLocalization` 那一层**（三条路最后都过它）：官方值过 `CardIcons.RewriteMarks`。
   **这条不是可选项** —— 现读 120 条 `Card_Abilities/*` 值里 **102 条**带 `[[…]]` / `((…))` / `$$…$$`，不过这一层就是把记号原样印到技能面板上。
   同时把**字体闸**接上（中文档但 `CardText.Zh == false` ⇒ 取官方英文列，同 `BattleDriver.FaceTextFull` 那条口径）。
3. **行为影响已量化（⛔ 别当成「没生效」）**：
   - **会变**：官方表 120 条 `Card_Abilities/*` 里 **86 条能由卡 id 拼出**，而 `hasActiveAbility == 1` 的卡 **87 张**、
     这 86 条**全部**落在其中（`hit ⊂ hids`，交集 86）⇒ 技能面板只对这批卡开 ⇒ **86 张卡的描述文本与改前不同**（正是要修的偏差）。
   - **不会变**：现 build 官方表 10,862 键里 **`-load` / `-effect` 各 0 条、`Card_Abilities/GenericSiegeText` 0 条**
     ⇒ 那三档今天仍落裸描述 —— **这是正确的**（按原版取键，只是数据里没有那几条）。`[[reload]]` 在 `Card_Abilities/*` 值里**现读 0 条** ⇒ 剥离支今天空转。
4. **只剩数据侧一半**：原版 id 那一半是 `(activeAbilityTextReferenceId ?? uniqueId)`，**我们没那一列**
   ⇒ 34 条键拼不出；今天的可见实例 = `SOR66`（Armorium Cherub，`hasActiveAbility==1` 但没有 `Card_Abilities/SOR66` 键）仍显示模板句。
   ⇒ 账**状态留 `C`**、**等级 `P1`**（补数据列后可转 `A`）。
5. **类型检查**：`TMPDIR=/tmp/wf_w21 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（最后一跑）。
   中途有一跑出现过 **2 条 `Shell/UIBorderGlow.cs` 的 `CS0535`**（`IMeshModifier.ModifyMesh`）—— 那个文件不在本代理白名单里，是别人在飞的文件；下一跑即消失。
6. **账闸**：`check_ledger_points.py`（只读）全表扫过一遍，**`SkillPanel.cs` 零漂移/零失败**
   （唯一一条命中 `SkillPanel` 的是 `BattleDriver.cs:15584（ShowSkillPanel）` 的漂移，属 W19 的在飞文件）。
   账补丁 `B9_W21SKILL.tsv` 的 `merge_ledger_parts.py --dry --only B9_W21SKILL` ⇒ **更新 1 · 新增 0**。

---

## 二、证据（文件:行号）

### 2·1 判据（派活包给的，本笔复核过的就这几行）

| 事 | 出处 |
| --- | --- |
| 键 = `Card_Abilities/` + (`activeAbilityTextReferenceId` ?? `uniqueId`) + suffix | `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/RawCardScript.cs:2385-2396`（`:2396` 那一行就是 `"Card_Abilities/" + text + suffix`）；`decomp_full/RawCardScript__GetLocalizedAbilityDesc.c:19-27` |
| `RemovingSiege` 同一拼法（**后缀 = 空串**） | `RawCardScript.cs:2408`（`suffix = ""` 的默认值）+ `:2411`（键拼法） |
| 后缀常量就是那个空串 | `decomp_full/CardScript__GetCurrentLocalizedAbilityDescription.c:32`（RemovingSiege 调用传 `DAT_1842b80e0`）与 `:67`（**裸键**调用传**同一个** `DAT_1842b80e0`）⇒ 两处后缀相同 ⇒ 空串 |
| 4 个 `[[reload]]` 变体（替换值全是空串，**顺序**照抄） | `RawCardScript.cs:2420`（`Contains`）→ `:2427` `[[reload]]: ` → `:2430` `[[reload]]:` → `:2433` `[[reload]] : ` → `:2436` `[[reload]] :` |
| 三条路最后都过 `ModifyLocalization` | `RawCardScript.cs:2405`（普通）、`:2445`（RemovingSiege） |
| 原版缺键那一支是 `throw`（ILSpy 误译成 `return (string)(object)new NullReferenceException();`） | `RawCardScript.cs:2443` |
| 改前的错前缀 | `SkillPanel.cs:2142` / `:2156`（改前 = `"Card_Description/" + cardId + "-load"` / `-effect`）；头注 `:2124` 也写着同一个错前缀 |
| 记号渲染/字体闸的既有先例（本笔照抄的口径） | `BattleDriver.cs:17746-17750`（`OfficialCurrent` → `HasOfficialMarks` ? `RewriteMarks(v, zhCol)`）· `BattleDriver.cs:27991-28006`（中文字体缺失 ⇒ `Loc.OfficialEnglish`） |

### 2·2 数据侧统计（本笔用 `python -I` 现读，⛔ 不是抄上一路报告）

数据源：`MyGame/Assets/CardPresentation/Resources/official_loc_13lang.json`（**10,862** 键）· `…/Resources/official_names.json`（bulk，**3,263** 条）·
`MyGame/Assets/RuleEngine/Resources/cards_engine.json`（**1,128** 张）。

| 量 | 值 |
| --- | --- |
| `Card_Abilities/*` 键 | **120** |
| 其中 `Effect<名>` 前缀（非卡 id 形状） | **30** |
| 其中**拼不出来**的（非卡 id） | **34**（= 30 条 `Effect<名>` + 4 条：`SOR30_new` · `SOR25_new` · `SaviorProtocolsEffect` · `ExperimentalDronesEffect`） |
| **能**由卡 id 拼出 | **86** |
| `Card_Abilities/…-load` / `…-effect` | **0 / 0** |
| `Card_Abilities/GenericSiegeText` | **不存在** |
| 值里带记号（`[[` / `((` / `$$`） | **102 / 120** |
| 值里带 `[[reload]]` | **0** |
| bulk（`official_names.json`）里的 `Card_Abilities/*` | **0** ⇒ `Loc.T` 只能走**官方全表**那一档（`Table`/bulk 都没有；`Loc.cs:3477-3497`） |
| `hasActiveAbility == 1` 的卡 | **87** |
| 上面 86 条键 ∩ 那 87 张 | **86**（`hit ⊂ hids`，**没有一条键落到「无主动能力」的卡上**） |
| 差的那 1 张 | **`SOR66`**（`Armorium Cherub`）—— 有主动能力、但官方表里没有 `Card_Abilities/SOR66` |

> 🔑 `hit ⊂ hids` 这一格是**这次改动正确的旁证**：官方表里那 86 条「按卡 id 拼」的能力文本键，
> **无一例外**落在这批有主动能力的卡上 ⇒ 原版这条链喂的就是这批卡（不是我们套了一个碰巧对得上的形状）。

---

## 三、没查清的部分

1. **`activeAbilityTextReferenceId`（原版 `RawCardScript +0x248`）我们没有这一列**（`cards_engine.json` 无此键），
   ⇒ 键的「id 那一半」缺一半、34 条键取不到；今天可见 = `SOR66`。
   **要补只能走数据侧**（来源 = 1,351 个卡 SO 的该字段），⛔ 表现层猜不出来。**本波不做**（简报明写）。
2. **原版 `((…))` 短路分支的语义没查清**：`activeAbilityTextReferenceId.Contains("((")` 时原版**原样返回该字段**
   （`RawCardScript.cs:2384-2389`）。那串是「内嵌富文本」还是「指向别的卡的引用」，**没有第二个证据** ⇒ 记「没查清」。
   今天不影响（我们没该字段），补数据列时**必须一起裁**。
3. **原版缺键那一支到底会怎样**：ILSpy 给的是 `throw`（`:2443`），照抄 = 一条必崩的路径。我们退回裸描述/模板句，
   已在代码里标 `🔶 自己设计（非原版）` + 「调度台 2026-10-27 裁定」。**原版真会走到那里没有第二个证据**（不算查清）。
4. **`DoesAbilityRequireLoading` 的循环边界**（上一路 `B9_W13CDW` §三·4 记的）：原版把 `HasRemovedTrait(reload)` 放在
   `activeEffects(+0x108)` 循环体内，我们无条件 `&&` ⇒ 「列表空 ∧ 已移除 reload」这一格两者不同。
   **不在本笔白名单**（写点在 `BattleDriver.cs`，W19 在飞），且 `ActiveAbilityNeedsLoading` 现读 1,351 张**全 0** ⇒ 今天不可观测。**照实记，未改**。
5. **`Loc.T` 缺键会出声 + `MissingCount++`**：本笔沿用了这个既有判据（缺键 ⇒ 出声，符合「⛔ 不许静默」）。
   今天最多只有 `SOR66` 一张会缺（86/87 有键）⇒ **不会刷屏**；若将来池子/键表变动，这里会按卡数出声（去重表按 key 去重）。
6. **一个潜在过度取键的风险（没法证伪，如实记）**：我们的键恒是 `Card_Abilities/<卡 id>`。
   若某张卡原版的 `activeAbilityTextReferenceId` 是**别的 id**（那 34 条之一），**而表里同时也有 `Card_Abilities/<卡 id>`**，
   那我们就会取到卡 id 那条（原版取的是 reference 那条）—— 今天**无法判定有没有这种卡**：
   要么拿到 SO 的 `activeAbilityTextReferenceId` 逐卡对（= 补 §三·1 那列时才做得动），要么永远不知道。
   ⇒ 补那列时**顺带把这一格判掉**（判法：`textReferenceId` 非空且 ≠ 卡 id 的卡，检查它的 `Card_Abilities/<卡 id>` 是否存在）。

---

## 四、改动清单

### 4·1 `Unity/MyGame/Assets/CardPresentation/Battle/SkillPanel.cs`（唯一改的代码文件）

| 行（改后） | 改动 | 行数影响 |
|---| --- | --- |
| `:2119` · `:2124` · `:2126-2127` · `:2131` | 头注订正（`Card_Description/` → `Card_Abilities/`；「RemovingSiege 后缀没解出来」→「= 空串、已解出」） | **零行数变化**（改前改后 `:2142` / `:2156` 仍是那两条键行 —— 账里有 `SkillPanel.cs:2142` / `:2156` 的文字引用） |
| `:2129` | 字段 `_warnedRemovingSiege` → **`_warnedLoadKeyMiss`**（那一支已不出声，本位只服务 `-load` 档），**同行加一句改名说明** | 零行数变化 |
| `:2136` | `bare = AbilityBareText(ability, cardId)`（裸键先查官方表） | 同行替换 |
| `:2142-2144` | `k1 = "Card_Abilities/" + cardId + "-load"`；取值改 `AbilityTerm(k1)`（含记号渲染） | 同行替换 |
| `:2146-2147` | `kG` 取值改 `AbilityTerm(kG)` | 同行替换 |
| `:2156-2158` | `k2 = "Card_Abilities/" + cardId + "-effect"`；`AbilityTerm(k2)` | 同行替换 |
| `:2161-2162` | RemovingSiege 支 → `return AbilityRemovingSiege(bare, cardId);`（⛔ 删掉原来那段「后缀没解出来 ⇒ 不退裸键」的出声） | 12 行 → 3 行 |
| `:2130-2132` · `:2165` | doc 订正（裸键两档写明；`ResetAbilityDescWarnForTest` 的说明改准） | 零行数变化 |
| `:2168-2244`（类尾新增） | 六个私有口 + 一段判据头注：`AbilityBareText`(`:2187`) · `AbilityRemovingSiege`(`:2202`) · `AbilityTerm`(`:2214`) · `AbilityRaw`(`:2223`) · `AbilityMarked`(`:2239`) | **全部追加在类尾**（`ResetAbilityDescWarnForTest` 之后、类闭合之前）⇒ 既有 `文件:行号` 落点（最大一条是 `:2142`）**零漂移** |

### 4·2 账

| 文件 | 内容 |
| --- | --- |
| `Unity/资料/完全复刻/账/_parts/B9_W21SKILL.tsv` | **1 行**（11 列 · 带表头 · `方法` = `GetCurrentLocalizedAbilityDescription()` 完整签名）。`状态` 留 **`C`**、`等级` **`P1`**；`我们的落点` 改成**方法级 + 行号**（`SkillPanel.cs:2133（AbilityDescription）`）；`差异`/`备注`/`轮次` 写全本笔结论与仍差的一半 |
| `Unity/资料/完全复刻/账/_parts/_gen_B9_W21SKILL.py` | 生成上面那份补丁的脚本（按 `(原版类, 方法)` 从主表取行、只覆盖 5 列；只读主表、只写 `_parts/`，可复跑） |

自检：`python -I -X utf8 d:/4/Unity/工具/merge_ledger_parts.py --dry --only B9_W21SKILL` ⇒ **合计：更新 1 · 新增 0 · 无变化 0（dry-run，未写盘）**。

### 4·3 ⛔ 明确没动

- `Battle/BattleDriver.cs`（W19）· `Battle/CardDisplayWindow.cs`（S4 地盘）· `Core/CardFeel.cs` / `Editor/BattleScene.cs`（W11）·
  `Core/CardView.cs` / `Editor/CardFaceAssertE5.cs`（W15）· `RuleEngine/Core/{EffectResolver,UnitState}.cs`（S0）· 其余派活包里点名的在飞文件。
- 账主表 · `CLAUDE.md` · `项目任务.md` · 任务线总表 · 别人的报告 · 任何基线文件。
- 没跑任何 Unity、没动 git（红线 1/2）。

---

## 五、顺手发现

1. **`Card_Abilities/*` 这一族可能不止一条语义 —— 「34 条拼不出来」里有一部分也许根本不是缺口（⚠️ 只证到 1 条）**：
   ① **能力描述** = `Card_Abilities/<卡 id>`（86 条，本笔这条链）；
   ② **效果标题** = `Card_Abilities/<effectText>`：**至少** `Card_Abilities/EffectDisplayCardsAddToDeck` 是这一条链在用
   （`RuleEngine/Core/EffectResolver.cs:7762` 的 `string key = "Card_Abilities/" + title;` + 断言
   `RuleEngine/Editor/RuleEngineTest_EntryPump.cs:364-366`）。
   ⇒ 那条**已在「拼不出来的 34 条」名单里**，但它**不是**能力描述缺口。
   ⚠️ **其余 29 条 `Effect<名>` 属于哪条链，本笔没查** —— ⛔ 别把「34 条全是缺口」当成定论；
   补数据列时要逐条判「这条键是能力描述还是效果标题」，别一刀切。
2. **`SOR66` 的 `refNameId` 是空串**（`cards_engine.json`），而它是 `hasActiveAbility==1` 的唯一一张没有 `Card_Abilities/<id>` 键的卡
   ⇒ 它是**补数据列后的第一个验证样本**（补完应能看到它取到官方能力文本）。**建议**：补列时优先用它验收。
3. **`Loc.Table` / bulk 里一条 `Card_Abilities/*` 都没有**（现读：`official_names.json` 3,263 条 0 命中；`Loc.cs` 里也没有这一族的手写键）
   ⇒ 这一族**只有官方全表一个源**。以后若有人「在手写表里补一条」来救某张卡，那会变成**自造判据**，⛔ 别这么做。
4. **`CardIcons.HasRenderedMarkup`（`<sprite …>` / `<link=…>`）在 `Card_Abilities/*` 值里也有**：例如 `Card_Abilities/AM10`
   的英文/中文列都直接带 `<nobr>+2<link=ranged><sprite name=Atlas_trait_icon_Ranged></nobr></link>`。
   这类值 `HasOfficialMarks` 为假 ⇒ `AbilityMarked` 原样返回 —— 对 TMP 是**成品标签**（能直接渲染），
   ✅ 与卡面那条链（`CardIcons.Rewrite` 里对 `HasRenderedMarkup` 的分流）一致，⛔ 不必再包一层。
5. **`_warnedRemovingSiege` 这个名字在仓里只此一处**（4 个引用全在 `SkillPanel.cs`），改名零外部影响 —— 已顺手改掉，并留了「原名」说明。

---

## 六、交接给下一个代理（若需要继续）

- **本件已做完**，无半成品。下一步的两条都在**数据侧 / 别人白名单**：
  ① **补 `activeAbilityTextReferenceId` 列**（`RuleEngine/Core/CardDef.cs` + `CardDatabase` 落列，来源 = 1,351 个卡 SO）
     ⇒ 补完把本账 `C` 转 `A`，并用 `SOR66` 做第一个样本；
  ② `DoesAbilityRequireLoading` 的循环边界（`BattleDriver.cs`，见 §三·4），需与该文件当时的写手错开。
- **重跑命令**：`TMPDIR=/tmp/wf_w21 bash d:/4/Unity/工具/typecheck.sh`（必须 0/0）·
  `python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py`（读 `SkillPanel` 那几行应零漂移）。
