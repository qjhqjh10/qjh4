# B89 · 第 1 波 · 执行代理 `E7` —— AI 预测 / 教程脚本（账 L1025 · L93 · L96）

> 日期 2026-10-28 · 批次 = `B88_R9_主线68条分批.md` 的「### 批次 12」· 白名单 = `RuleEngine/Data/SimpleAI.cs` + `RuleEngine/Core/TutorialScript.cs`（**只动了这两个**）
> ⛔ 没跑 Unity / 没跑 ruleprobe / 没动 git / 没碰账主表。唯一跑过的命令 = `TMPDIR=/tmp/wf_B89E7 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。

---

## §1 逐条结论

| 账行 | 原版判据 | 改动（文件 : 符号名 @ 现读行号） | 状态建议 |
|---|---|---|---|
| **L1025** `EntityScript.EnemyReturnsAttack` | `decomp_full/EntityScript__EnemyReturnsAttack.c:1-82`（🥇 全文逐句）· 调用点 `AI__ScoreFromBuffedAttack.c:77` / `BattleManager__RecordAttackPendingDamage.c:84` / `CardHighlight__ToggleCombatPreviewHighlight.c:53` | `RuleEngine/Data/SimpleAI.cs:604`（在 `ScoreFromAttack` 内，声明在 **`:584`**）：`CounterAttack.NoCounter(…四道…)` → **`!CounterAttack.EnemyReturnsAttack(…五道…)`**。<br>零行数变化（2228 → 2228 行，diff 5/5）。 | **B**（见下「残差」；比改前收窄了一大截） |
| **L93** `AiScripted.ExecuteAction` | `decomp_full/AiScripted__ExecuteAction.c:55-1279`；补的两档实测 = `case 0x82`（`.c:133-180`）· `case 0x8c`（`.c:199-217`） | `RuleEngine/Core/TutorialScript.cs:908`（`ExecuteCore` 里两条新 `case`）· **`:1428` `RunShowManaAura`** · **`:1454` `RunResolveCard`** | **B**（结构已建，残差见下） |
| **L96** `AiScripted.GetActingCard` | `cpp2il dev AiScripted.cs:3008-3112` · `decomp_full/AiScripted__GetActingCard.c:1-90` · `decomp_full/AiScripted__GetChosenCard.c:17-43` | `RuleEngine/Core/TutorialScript.cs:1734`（`FindBoardUnit` 两处退回档）· **`:1778`（`FindHandIndex` 两处）** · **`:1931`（新增 `PickFirstMinion`）** | **A**（按调度台裁定照抄，见 L96 三、） |

### 一、L1025 —— `SimpleAI` 的反击预测改走预测版

**判据链（我自己回源核过，没有照抄账里的结论）**：
- `AI__ScoreFromAttack.c:30` —— 原版 `AI.ScoreFromAttack` 的**第一句**就是 `AI__ScoreFromBuffedAttack(param_1, param_3, param_4, …)`（整段转调，别的一概不做）。
- `AI__ScoreFromBuffedAttack.c:77` —— `cVar2 = EntityScript__EnemyReturnsAttack(param_2, param_3, 1, param_6, 0)`（`this` = 攻方、`attackedCard` = 目标、`isAttacker = 1`）。
- ⇒ **原版两条 AI 入口同源**：`ScoreFromAttack` 与 `ScoreFromBuffedAttack` 走的是**同一个**五道版的 `EnemyReturnsAttack`。
  🔴 **这推翻了原账那句「原版这两条入口本来就不一致（预测五道 / 实际四道），要不要一起接由调度台裁」** —— 那句话不成立，已在本批撤回（并在 `SimpleAI.cs` 的注释里就地写明）。
  「四道」只属于**实际结算**那条链（`BattleManager._ResolveAttack_d__438__MoveNext.c:871-895`），与 AI 估值无关。

**改动**（`SimpleAI.ScoreFromAttack(ctx, side, u, target, ranged)`）：
```
- bool noCounter = CounterAttack.NoCounter(ctx, side, u, foe, target, ranged, RuleCore.WouldKill(target, atk));
+ bool noCounter = !CounterAttack.EnemyReturnsAttack(ctx, side, u, foe, target, ranged);
```
`targetWillDie` 一并交方法内部按 `CombatDamage(攻方)` 算（原版 `.c:18-23` 的 `GetCombatDamage` 口径），⛔ 不再传 `RuleCore.WouldKill` 那个带 buff 的预测 —— 与 `AiScoring.cs:582`（`B8_P` 那次接线）**同一口径**。

**残差（所以建议仍是 B）**：
- ⓑ `CounterAttack.CombatDamage` 对 `dropPod(0xe6)` 返 `null`（原版读 `+0xe0` 的存档攻击力，我们没建那一格）。池内带 `droppod` 的卡 **0 张** ⇒ 今天恒不可达；方法已按「判不了」返 `null`，⛔ 没拿普通档冒充。
- ⓒ 残骸：`IsRemnant && !IsLeavingRemnant ⇒ 0` 已锚定（`CounterAttack.CurrentCombatAttack` / `CombatDamage`）。
- ⓐ **仍成立**（但不在本行落点内）：③ 战斗预览那条调用点 **根本没接上** `EnemyReturnsAttack`（见 §3 顺手发现 1）。

### 二、L93 —— `ExecuteAction` 补 `ShowManaAura(130)` / `ResolveCard(140)`

**原版逐句**（`AiScripted__ExecuteAction.c`）：
- `case 0x82 (130) ShowManaAura`（`.c:133-180`）：按 `data[0].actingUnitType(+0x14)` 的六档取列表 → `GetChosenCard(list, data[0].actingCard)` → 认得出才 `CardScript.ShowManaCostAura(card)`；认不出 ⇒ `goto LAB_180942dbe`（`FUN_1803f47a0()`，抛/不返回）。
  🔴 **`ShowManaCostAura` 本身是空桩**：`decomp_full/CardScript__ShowManaCostAura.c:3-12` 全文 = 一条 `CustomDebug.LogWarning("ShowManaCostAura not implemented")`，**不写任何状态**（账 928 行判 `D` 就是这个理由）⇒ 这一档**在引擎里不改局面**。
- `case 0x8c (140) ResolveCard`（`.c:199-217`）：取 `data[0].actingCard(+0x18)` 那张卡；**为空 ⇒ 整支跳过（什么都不做、也不抛）**；非空 ⇒ 对那张卡连发 `TriggerAbilities(card, 0)` + `TriggerAbilities(card, 500)`，形参 = `thisCard = cardPlayed = GetHero(mgr, 0)`（= **AI 那一侧的督军**）、`targetCard = null`、`isEnchantment = 1`、`onlyEnchantments = 0`。

**我们的实现**：
- `RunShowManaAura`：解析那张卡（`FindBoardUnit` → 不行再 `FindHandIndex`），认不出就 `Fail` 出声（原版那里是抛，我们不复制崩溃语义）；认得出就把「原版这一跳是空桩」`ctx.Log` 出来，**返回 false**（没改局面）。
- `RunResolveCard`：`d.acting` 为空 / `ourId` 空 ⇒ `Note` + `ctx.Log` + 返回 false（原版就是整支跳过）；认得出 ⇒ **转调 `Enchantments.TriggerEnchantment(ctx, 1, card, hero, null, false)`**（`Core/Enchantment.cs:1342`，`public static`）—— 它就是同一个形状「对同一张卡连发 `TriggerAbilities(card,0)` + `(card,500)`、`isEnchantment = 1`」（`RawCardScript__TriggerEnchantment.c:7-13`）。
- ⚠️ **残差（所以建议仍是 B）**：`Enchantments.TriggerEnchantment` 的 `trigger 0` 取数口是 `EffectText.Parse(card.Desc)`（**附魔那一族**的口径），而原版 `ResolveCard` 走的是通用 `cardAbilities[trigger == 0]` 的解析 ⇒ 对「正文就写在 `desc` 里」的卡等价，对其余不等价。**今天零可观测**（两档数据各 0 条）。
- 🔶 **没打 `🔶 自己设计`**：这不是「我们编的值」，是「借了同形入口 + 已知取数口差异」——按铁律 18 的三种不许打标情形②（语义一致、实现路径不同）不属该标，残差写在 doc + 账里。

### 三、L96 —— `GetActingCard` 补「认不出退回 `list[0]`」

**原版**：`GetChosenCard(list, chosenMinion)` 在 `chosenMinion == null` 或「一个都对不上」时 `iVar4 = 0;`（`AiScripted__GetChosenCard.c:38`）⇒ **返回 `list[0]`**；只有 `list.Count == 0` 才 `LogError` + 返回 null（`.c:18-21`）。

**改动**（`TutorialRules` 里三处）：
- `FindBoardUnit`（`:1734`）：`ourId` 空 ⇒ `PickFirstMinion(ctx, side, out slot)`；列表里找不到 ⇒ 同样 `PickFirstMinion`。
- `FindHandIndex`（`:1778`）：两个分支都改成 `ctx.Players[side].Hand.Count > 0 ? 0 : -1`。
- 新增 `PickFirstMinion`（`:1931`，**追加在 `TutorialRules` 类尾**）：那一侧按格号升序取第一个**非督军**在场单位（= 原版 `Get{Player,Enemy}MinionsAndRemnantInPlay()` 的那一份）。
- 四处实现点都写死注释：`// 🔴 原版此处为 X，照抄，⛔ 别修`（调度台 2026-10-28 裁定；先例 = `BroadcastCardDrawn` 漏 `!`）。

**就地订正了 4 处过期文案**（铁律 5）：`ExecuteAction` 的大段 doc（`:879`）、`RunAttack` doc（`:1016`）、`RunPlayCard` 内注（`:986`）、`Fail()` 那条警告文案（`:1159`）—— 它们原来都写着「⛔ 绝不退回 `list[0]`」，现在按原版照抄了，不改就是两份说法打架。

---

## §2 验证（我跑不了，留给调度台）

### L1025
```
bash d:/4/Unity/工具/ruleprobe.sh ai ASH35
```
- **改前读数**（账里记的）：`② 反击/存活 = 0.1（counter=0 · 存活奖励=0.1）` = 走到「有反击」那一支。
- **改后期望**：若夹具那个目标被 **星镖 8** 打死（`DamageKillsTarget` 只减护甲：`Max(1, 8 − 护甲) ≥ 生命`）⇒ 读数应变成 **`② 反击 = 0（豁免）`**，总分 **−0.1**。
- ⚠️ **若读数一字未变**，**不能**判「改动没生效」——先看夹具目标的 `生命 / 护甲`：目标是 `生命 > 8 − 护甲` 时星镖打不死，豁免本来就不该触发。此时把夹具目标换成血量 ≤ `8 − 护甲` 的单位再跑一次（这一步才真正区分「接线了」和「没接线」）。
- ⛔ **`play all dump` 不能当读数口**（结构上看不见 AI；逐字节不变是正常的，既不能当「没生效」也不能当「改对了」的证据）。
- 另：`bash d:/4/Unity/工具/ruleprobe.sh play check` 的指纹**应当完全不变**（AI 打分不进引擎结算）。

### L93 / L96
- 这两档**没有离线读数口**（`ruleprobe tutorial g3` 只覆盖 `IsReferenceCardElement` / `IsChatKind` / `IsWaitingChangeAttack` 三条纯谓词）。
- 唯一能给出的硬验证 = **`bash 工具/typecheck.sh` 0/0**（我已跑）+ **教程自检宿主**（`BattleScene.Run`，含 `⑯-④`/`⑯-④′` 一族；⚠️ 会重写 `Battle.unity`，跑前留副本）。
- 语义验证只能靠**合成夹具**：一条 `ResolveCard` 动作（`data[0].acting.ourId` = 一张有 `desc` 正文的卡）+ 断言「`ctx.Log` 里出现 `trigger 0` / `trigger 500` 两跳」。

---

## §3 顺手发现（⛔ 我没改，只报）

1. 🔴 **账 L1025 的 R17 / R21 备注「③ 战斗预览那条调用点已接（`BattleDriver.cs:34111`）」与现读不符。**
   `CardPresentation/Battle/BattleDriver.cs` 里 **`EnemyReturnsAttack` 0 命中**；`BattleBoardPreview`（现读 `:34357`，不是 34111）的 `isAttacker` 支只有
   `one.Add(otherU.IsRemnant ? 0 : RuleCore.FieldAttack(Ctx, otherSide, otherU, ranged));` —— **一个豁免闸都没有**，而该处自己的 doc（`:34347-34350`）还写着「ⓐ … **恒按「有反击」**」。
   ⇒ 原版 `CardHighlight__ToggleCombatPreviewHighlight.c:53` 那条调用点**其实没接**；账里那句「2/3 已接」应改成「② 已接（两条 AI 入口）· ① 无对应物 · ③ 未接」。**本行仍 B** 的一大半原因就在这。
2. 🔴 **原账「原版这两条入口本来就不一致（预测五道 / 实际四道）」是错的** —— 见 §1 一、的判据链（`AI__ScoreFromAttack.c:30` 整段转调）。已在 `SimpleAI.cs` 注释与账补丁里撤回。
3. ⚠️ **`Enchantments.TriggerEnchantment`（`Core/Enchantment.cs:1342`）是本批新增的跨文件依赖** —— 该文件本轮由 `E4` 独占。我只**调用**没改；若 `E4` 改了它的签名，同步点那次 typecheck 会立刻红。
4. ⚠️ **`TutorialScript.cs` 里 `TutorialRules` 的三条谓词与 `TutorialScript` 的六档实现分属两个类**，`IsNoOpScriptKind` 的清单现在与 `ExecuteCore` 的 `case` 表**不再一一对应**（`ShowManaAura` / `ResolveCard` 两边都有）—— 已在 doc 里注明这是有意的（对齐「原版本来没有改局面分支」的语义），⛔ 别当成 bug 去删。

---

## §4 没查清

1. **`RunResolveCard` 的 `trigger 0` 取数口**：通用 `cardAbilities[trigger == 0]` 在我们引擎里**没有公开入口**（`_triggerOps` 是按行为关键词索引的，不是按 trigger id；按 trigger id 派发那条链在 `Core/ActionPump.cs`，本轮由 `E1` 独占）。今天借 `Enchantments.TriggerEnchantment` 的 `desc` 解析顶上，差异已写进账。**要真解决得给 `CardDef` 加一个「按 `AbilityTrigger` 数值取 op」的公开口**——不在本代理白名单。
2. **原版 `EnemyReturnsAttack` 的 `param_3`（`isAttacker`）为假的那些分支**：我们 `CounterAttack.EnemyReturnsAttack` 的签名**没有**这个参数，后三道（sniper/longRange/shuriken）恒被求值。三个调用点里 ②（AI 两条）恒传 `1`、① 恒传 `1`，只有 ③ 传的是实参 —— 而 ③ 现在**根本没接**（§3-1）⇒ 今天**零可观测差异**，没改。要不要补这个形参，等 ③ 接线时一起定。
3. **`tutorial_stages.json` 6 关里 `ShowManaAura` / `ResolveCard` 各 0 条**这个数我是照账里那句「数据 0 条」用的，**没有自己重数**（数据文件不在我的白名单里读也没用，因为改动与它无关）。若调度台要一个「确数」，得现数一遍。

---

## §5 交接

**动过的文件（只这两个）**：
- `MyGame/Assets/RuleEngine/Data/SimpleAI.cs` —— **2228 → 2228 行（零行数变化）**，diff 5/5。行尾 LF 未变（HEAD = LF 2228）。
- `MyGame/Assets/RuleEngine/Core/TutorialScript.cs` —— **1843 → 1945 行（+102）**，diff 129/29。行尾 LF 未变（HEAD = LF 1843，当前 LF 1945，CRLF 0、裸 CR 0）。

**⚠️ `TutorialScript.cs` 的账锚点漂移（非均匀，`--fix` 前请按这张表核）**：

| 旧行号区间 | 漂移 |
|---|---|
| ≤ 879 | **0** |
| 880 – 1016 | **+1** |
| 1017 – 1182 | **+2** |
| 1183 – 1402 | **+4** |
| **≥ 1403**（整个 `TutorialRules` 类） | **+76** |

- 已实测反例：`CanSkipCurrentAction` 1791 → **1867**、`IsReferenceCardElement` 1752 → **1828**、`IsWaitingChangeAttack` 1828 → **1904**（三条都 +76 ✓）。
- `RuleEngine/Data/SimpleAI.cs` **零漂移**（`ScoreFromAttack` 仍在 **`:584`**）。

**新成员落在哪**：
- `TutorialScript` 类尾（原 `:1403` 的 `}` 之前）：`RunShowManaAura`（`:1428`）、`RunResolveCard`（`:1454`）。
- `TutorialRules` 类尾（原 `:1841` 的 `}` 之前、末条锚点 `:1828` 之后）：`PickFirstMinion`（`:1931`）⇒ **不引起任何漂移**。

**下一步建议**：
1. 同步点跑 `bash 工具/typecheck.sh`（我这次 0/0）+ 若要跑教程自检 ⇒ `BattleScene.Run`（先留 `Battle.unity` 副本）。
2. 跑 §2 的 `ruleprobe ai ASH35`；读数若不变，**先看夹具目标的血/甲**再下结论。
3. `--fix` 时按 §5 的漂移表核一遍；顺手把 L1025 备注里「③ 已接」那句改掉（§3-1）。
4. L93 那条「其余 13 档逐档对 `.c`」仍是未完成项（本批只补了 `ShowManaAura` / `ResolveCard` 两档）。
