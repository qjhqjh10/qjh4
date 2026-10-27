# B9_W57BDR4 —— 敌方打出卡的展示位 + 技能面板喂参 + 三处过期 doc

> 代理：**B9_W57BDR4** · 2026-10-27（`B9` 波 · W57 批次）
> 判据（未重查，只读「改动请求」②④）：`资料/完全复刻/报告/B9_W44HUD_战斗界面剩余C.md` §六·②/④ · §一·5、§二·⑦。
> 文件所有权：`CardPresentation/Battle/BattleDriver.cs` · `RuleEngine/Core/UnitState.cs`（仅注释一处）。
> ⛔ 未动：`*.unity` / `ProjectSettings` / `RuleEngine/**` 的其它文件 / 别的账补丁。

---

## 一 结论

### ① 敌方打出卡的展示终点 —— 已从「屏幕正中」改成原版落点

`DisplayEnemyCardInCenter`（原版 `CardScript._DisplayEnemyCardInCenter`）的 `DOMove` 终点，原来是
`LayoutSpace.ToWorld(0.5f, 0.5f)` = **屏幕正中 (960,540)**；现在是
`enemyHand` 锚 `(960,50)` + `VarsGlobal.PlayedCardDisplayOffsetPx(offensive: false, SmallScreenUI.Enabled, 1080f)`：

| 档 | X | Y | 落点（设计像素，左上原点 / y↓） |
|---|---|---|---|
| **1080p 桌面档** | `960 + (−26 × 14.835) = 960 − 385.71 = 574.29` | `50 + (24 × 14.835) = 50 + 356.04 = 406.04` | **(574, 406)** |
| **小屏档**（`SmallScreenUI.Enabled`） | `960 + (+25 × 14.835) = 960 + 370.88 = 1330.88` | 同上 `406.04` | **(1331, 406)** |

- **X 符号两档相反**是原版两个字段（`+0x54 = −26` 桌面 / `+0x58 = +25` 小屏），⛔ 不是笔误。
- 换算**只有一份**：新增 `EnemyCardPlayedDisplayPointWorld()` 调 `VarsGlobal.PlayedCardDisplayOffsetPx`，
  调用点不再出现 `×14.835`。`PlayedCardDisplayOffsetPx` 由此从「零消费者」变成**唯一入口**。
- 可视变化：卡停在**偏左、垂直偏上**（不再盖在屏幕正中 / 手牌上方）。
- 已知边界：`screenHeightPx` 传 1080（非 1080p 档的落点未算，与 `RevealEndPx` 同口径）。

### ② 技能面板「可选目标」两格 —— 已喂真值（但**可见行为今天仍不变**，原因见下）

`ShowSkillPanel` 原来传 `maxTargets = 0` / `firstTargetName = null`（写死）⇒ 单目标 `(卡名)` 那支**结构上**走不到。
现在传 `SkillPanelMaxTargets(u)` / `SkillPanelSingleTargetName(targets)`。

🔴 **如实说：整段后缀今天仍然拼不出来** —— 挡在它前面的**另一道闸**是
`targetsAny = !ShouldShowAbilityTargetsCounter(u.Card)`，现实现恒 `true`（= 原版
`ActiveSkillDisplay.cs:160` 那道 `avail.targetsAffected != any` 闸，等价实现见 `BattleDriver.ShouldShowAbilityTargetsCounter`）。
而那道闸关着是**对的**：现扫 1,351 张 SO，`activeAbility/alternativeAbility.targetsAvailableCounter`
**全是** `targetsAffected = any` / `maxTargets = 0` ⇒ **原版在这份数据上也从不拼后缀**。
⇒ 本波做的是「把**值**接对」（原来的 `0, null` 是第二道**不该有**的闸）；**看得见的 `(卡名)` 要等
ability 级 `targetsAvailableCounter` 落进 `CardDef` 且数据非 `any`**。

- `maxTargets` 的取值口径：原版读 ability 级 `targetsAvailableCounter.maxTargets`（`ActiveAbility+0x40`）——
  我们 `CardDef` **没落这一格**；本面板只由 `Ability:` 文法（`EffectSpec`）驱动，而那条文法**每条只取一个格位**
  ⇒ 需要点目标（`EnemyUnit` / `FriendlyUnit`）= **1**，不需要点目标（self / warlord / draw）= **0**。
  代码里按 CLAUDE.md 铁律 18 打了 **`🔶 自己设计（非原版）`**（理由：原版那一格拿不到 + 数据全 0）。
- `firstTargetName`：**恰好一个**合法目标时取它的卡名（`CardDisplayName` 口，与技能标题同一个口），
  无目标 / 多目标 ⇒ `null`（原版 `:172` 只在 `maxTargets == 1` 时读 `targets[0]`，此时 `Count` 结构上 ≤ 1 ⇒ 等价）。
  候选口与点亮/准星**同一个** `LegalTargetCode`（判据只此一处），并额外要求那张卡有视图（与 `HighlightTargetsCore` 计数口径一致）。
- ⛔ 费用格**没动**（要引擎先有 `currentAbilityCost`，不在本波）。

### ③ 三处过期 doc 已就地订正（**零行数变化**）

| # | 位置 | 原文（过期） | 现文 |
|---|---|---|---|
| a | `RuleEngine/Core/UnitState.cs:754-757` | 「本字段今天**只服务表现层** … **那处应改读 `ActiveAbilityUsed`**（本条只记账）」 | 「✅ 2026-10-27（`B9_W49BDR3`）**已改完**：`BattleDriver.cs:27866` 已改读 `!u.ActiveAbilityUsed` ⇒ 本字段今天**没有任何表现层/规则消费者**（剩读点只有引擎写入、清零与自检）」 |
| b | 账 `战斗逻辑驱动.tsv:300`（`DisplayRevealedCard`）差异列 | 「② 那个『目标旋转』原版**没解出来** ⇒ 我们恒 identity」 | 「② 目标旋转 **2026-10-24 已解出 = 零向量**（`…_d__264__MoveNext.c:41-44` 的 `DORotate` 目标读 `Vector3` 静态块 `+0x0` = `zeroVector`）⇒ 我们恒 identity = **终态等价**」 |
| c | `BattleDriver.cs:10002`（**顺带：同一条错话的第三处副本**） | `// 原版那个目标旋转**没解出来**（见 ① ）` | `// 原版目标旋转 = zeroVector（…）⇒ 我们 identity = 终态等价` |

---

## 二 证据（文件:行号）

| 断言 | 出处 |
|---|---|
| 原版展示位公式（`enemyHand + right×Xoffset − up×Yoffset`；桌面 X `−26` / 小屏 X `+25`；Y `24`） | `decomp_full/CardScript__GetEnemyCardPlayedDisplayPosition.c:60-75` |
| 展示位 state0 用它做 `DOMove(…, 0.15)` | `decomp_full/CardScript._DisplayEnemyCardInCenter_d__263__MoveNext.c:28-46` |
| `enemyHand` 屏幕锚点 **= (960,50)@1080p**（`UpperAnchor/EnemyArea/HandArea`） | `资料/完全复刻/判据/enemyHand屏幕锚点_判定.md` §一·1 · §一·2 累加表 · §一·3 |
| 六个偏移常量的原版字段号与值（`+0x50/+0x54/+0x58`、`+0x5c/+0x60/+0x64`） | `RuleEngine/Core/GameplayVariables.cs:606-624` |
| px 换算 `14.835 px/世界单位 @1080p` + `y 向下为正`（与 `LayoutSpace.FromPixel` 同约定） | `RuleEngine/Core/GameplayVariables.cs:663-693` |
| 原版技能面板三档：`avail.targetsAffected != any` 闸 / `maxTargets==1 && Count>0` 单目标支 / `desc + " " + 后缀` | `cpp2il/out_cpp2il_dev_ilspy/ActiveSkillDisplay.cs:160` · `:172` · `:188` |
| `GetTargetAvailableText` = `"(" + 名字 + ")"`；`GetTargetsAvailableText` = 词条 `{0}` 替换 | `cpp2il/out_cpp2il_dev_ilspy/SupportMethods.cs:1478-1496` |
| 我们后缀的**唯一写口**（`targetsAny` 语义是反的） | `CardPresentation/Battle/SkillPanel.cs:2085-2110` |
| 原版 `avail` 数据现实：1,351 张全 `targetsAffected = 0` / `maxTargets = 0` | `资料/普查产出_1018/allcards_1350/cards_raw.json`（本笔现扫 `activeAbility` / `alternativeAbility` 两支，各 1,351 行） |
| 本面板只由 `Ability:` 文法驱动（池内 0 张） | `RuleEngine/Core/EffectSpec.cs:47-48`（原文记 v28，本笔现扫 v34 同一读数）+ `cards_engine.json` 现扫（`desc` 含 `Ability:` = 0 / 1,128） |
| Duty 徽标现读 `+0x4C` 那一格（`ActiveAbilityUsed`） | `CardPresentation/Battle/BattleDriver.cs:27852-27866` |
| 「目标旋转 = 零」的 2026-10-24 订正链 | `CardPresentation/Battle/BattleDriver.cs:9793-9796` |

---

## 三 没查清的部分

1. **非 1080p 档的像素落点未算** —— `PlayedCardDisplayOffsetPx` 的 `screenHeightPx` 传死 1080（与 `RevealEndPx` 同一口径）；
   4:3 档下 (960,50) 这个锚点本身也会变（锚点判定文档只给 1080p 值）。
2. **`maxTargets` 的原版真值来源没落** —— ability 级 `targetsAvailableCounter`（`ActiveAbility+0x40`）在 `CardDef` 里没有对应字段；
   我用「`EffectSpec` 文法结构」代替（`🔶`）。将来 `CardDef` 落这一格时，**只改 `SkillPanelMaxTargets` 一处**即可，⛔ 别在调用点再判一次。
3. **单目标支在真实数据上跑不到** —— 原版那份数据（全 `any`）根本不开闸，我们的 `Ability:` 卡池内 0 张 ⇒ 这条支
   只能靠合成夹具验；本笔**没有**为它加断言（`BattleScene.cs` 不在本笔文件白名单）。
4. **`targetsAny` 的命名与语义相反**（true = **不**拼后缀）—— 两处长注解释过，但很容易读反；见 §五·3。

---

## 四 改动清单

| # | 文件 | 行 | 改了什么 | 行数变化 |
|---|---|---|---|---|
| 1 | `CardPresentation/Battle/BattleDriver.cs` | `:25911` | `centerWorld = LayoutSpace.ToWorld(0.5f,0.5f)` → `EnemyCardPlayedDisplayPointWorld()`（+ 尾注写死两档落点） | **0**（原地一行） |
| 2 | 同上 | `:25904` | 注释「打出的牌在**屏幕正中**展示」→「在展示位（enemyHand 锚 + 偏移，**不是屏幕正中**）」 | **0** |
| 3 | 同上 | `:15606` | `skillPanel.Show(…, targets, 0, null, …)` → `…, targets, SkillPanelMaxTargets(u), SkillPanelSingleTargetName(targets), …`（+ 尾注说明为何 / ⛔费用格没动） | **0**（原地一行） |
| 4 | 同上 | `:10002` | 「目标旋转**没解出来**」→「= `zeroVector`，identity = 终态等价」（同一条错话的第三处副本） | **0** |
| 5 | 同上 | 类尾（`:35534` 之后） | 新增 3 个成员 + 2 个锚点常量：`EnemyHandScreenAnchorPxX/Y` · `EnemyCardPlayedDisplayPointWorld()` · `SkillPanelMaxTargets(UnitState)` · `SkillPanelSingleTargetName(int)`（`BattleDriver` 非 partial ⇒ **一律追加类尾**） | **+66**（全在类尾，已有行号一个不动） |
| 6 | `RuleEngine/Core/UnitState.cs` | `:754-757` | 「只服务表现层 / 应改读 `ActiveAbilityUsed`」→「已改完 / 今天没有任何生产消费者」 | **0**（原地四行） |
| 7 | `资料/完全复刻/报告/B9_W57BDR4_敌方展示位与技能面板.md` | — | 本报告（新建） | — |
| 8 | `资料/完全复刻/账/_parts/B9_W57BDR4.tsv` | — | **表头 + 1 行**（`战斗逻辑驱动.tsv:300` 的整行副本；改「差异/轮次/备注」三列 + 落点列三个**已漂的内嵌行号**，见 §五·6；11 列） | — |

**校验（全部跑过）**

- `TMPDIR=/tmp/wf_w57 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
- `bash 工具/ruleprobe.sh check` ⇒ **rc=0**，输出指纹 **`c9a64798…` 不变**（"当次现算的解析输出与被确认的那一版一致"）。
- `bash 工具/ruleprobe.sh play check` ⇒ **rc=1（预期）**，仍是 **`BL64` + `DA42` 两张**，**没有第三张**。
- 行尾/行数（二进制现读）：`BattleDriver.cs` 全 **CRLF**（35,534 → **35,600**，+66 全在类尾；CRLF 35,600 / LF 35,600）、`UnitState.cs` 全 **LF**（3,190 / 3,190）、裸 CR **0 / 0**。
- 账补丁：现读 11 列 × 2 行；`工具/check_patch_conflicts.py` ⇒ **没有跨补丁重复键（2 份补丁）**。

---

## 五 顺手发现（只报，未越界）

1. **`BattleDriver.cs:10002` 是同一条错话的第三处副本**（账里只点了 `tsv:300`、交接里点了 `UnitState`）。
   它在我的文件所有权内、且是纯注释 ⇒ **已就地订正（0 行数）**，与 `:9793-9796` 的 2026-10-24 订正一致。⛔ 不是越界改动。
2. **`VarsGlobal.PlayedCardDisplayPosition`（世界版）不该在这条链上被消费** ——
   它的偏移单位是**原版 UI 画布的世界单位**（1 单位 = 14.835 px），而我们 `LayoutSpace` 的 1 世界单位 = **108 px**
   ⇒ 直接用它会把偏移放大 **7.28 倍**（`−26` 世界单位 = `−2808 px`，跑到屏外）。
   所以「唯一入口」这一波只接了 **px 版**；世界版仍零消费者，将来谁要在世界空间里用它，必须先过 px 桥。
3. **`SkillPanel.Show` 的 `targetsAny` 命名与语义相反**（true = **不**拼后缀）。两处长注反复解释过，
   但每次读都要在脑子里翻一次；建议改名 `suppressTargetsSuffix`（纯改名，但动 `SkillPanel.cs`，不在本波白名单）。
4. **技能面板整条链今天在生产路径上不可达**：`ShowSkillPanel` 的闸是 `u.Ability != null`，而 `Card.Ability`
   来自 `Ability:` 文法（池内 **0 张**）⇒ 只有合成夹具走得到。这不是本波引入的，但 §一·② 的「可达」只能靠自检。
5. **断言缺口（不在我的白名单，故只报）**：`Editor/BattleScene.cs` **没有任何断言**钉「中央展示的终点」——
   现读只有 `:15390` 的「载体建出来了」。建议补两条结构断言（别比浮点）：
   ① `Vector2.Distance(落点, FromPixel(960,540)) > 100` 且 x 更靠左；
   ② **小屏档 X 反号**的判别式（`SmallScreenUI` 两态各取一次落点，`x(小屏) > 960 > x(桌面)`）。
   现有断言**不会**因为我这改动变红（`ToWorld(0.5f,0.5f)` 的其它 3 处都是 HUD 兜底/指针夹具，与本终点无关）。
6. **账 `战斗逻辑驱动.tsv:300` 的「我们的落点」列有三个内嵌行号早就漂了**（不是本波造成的，是本波现读核出）：
   `:9568 CardView.Create(...)` → 现读 **`:9998`**；`:9570-9572` 起点三句 → **`:9999-10003`**；
   `:9738 TickOffensiveReveal` → **`:10019`**（首格 `:9968 BeginOffensiveRevealOrApply` 现读仍对，未动）。
   已在补丁里按现读改正（改动范围写进了「备注」列）；若调度台不想要这一笔，回退补丁该行即可，**不影响**差异列那一条。
