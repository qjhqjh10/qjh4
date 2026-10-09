# A 表已收口 · 第十一会话（2026-10-10）

> 🔴 **本文件的用法**：`项目任务.md` §三第 29 条 **§29·b** 是**唯一待办正本**；一条账**做完**就把它**整行原文**搬到这里（+ ✅ 注记），
> 正本里那行**删掉**。这样正本永远只等于「**还开着的**」。
> ⚠️ **本文件不是待办** —— 是**收口留痕**（「当时怎么销的」）。判据原文在各自那一行 / 各线报告里。

---

## 一、本次会话销掉的（逐行原文 + ✅ 注记）

### 1. `RankedRewardEventWindow.BuildCard` 那颗 `Debug.Log` 文案过期 —— ✅ **2026-10-10 已修**（**调度台自己动手**）

**原文（`项目任务.md` §29·b 第二节「没有独立编号的零碎」那一行，一字未改）**：

> 🔴 **`RankedRewardEventWindow.BuildCard` 那颗 `Debug.Log` 文案是过期的** —— 它印「后者那张图本仓没有 ⇒ 两张都只建节点、不画」，而它上面 `if (bgTex != null) MenuDraw.Rect(...)` **现在真会把 `Background` 画出来** ⇒ **这句话今天在骗人**（连带：那个 `if` 只看 `_warnedCardIcon`、**不看 `bgTex`**）

**✅ 收口内容**（`CardPresentation/Shell/RankedRewardEventWindow.cs`，**LF**）：

| 面 | 改前 | 改后 |
|---|---|---|
| **出声文案** | 印「`Army Icon` 与 `Background`…两张都**只建节点、不画**」 | 说 **`Army Icon` 只建节点不画**（原版由 `ArmyIconsSO.GetArmyIcon` 运行期灌、本地没那张表），**`Background` 那一半按实际走的那一支说** |
| **那道闸** | `if (!_warnedCardIcon)` —— 只看「报过没有」、**不看 `bgTex`** | 同上（保留一次性），但**文案里带上 `bgTex != null ? … : …`** ⇒ 两者一致、不再自相矛盾 |
| **代码注释** | 无 | 加了 **2026-10-10 订正痕**（铁律 5）：写明原句哪里错、为什么错、依据是本文件 `ArtCardBg` 那条 doc |

- **判据**：`ArtCardBg`（阵营卡底图）**本仓有** ⇒ `:565` 那颗 `if (bgTex != null) MenuDraw.Rect(go, bgTex, bgR, "Background", QCardBg)` **真会画**；只有取不到时才退化成 `MenuDraw.Node(go, "Background", bgR)` 空节点。
  而同文件 `:553-556` 的 doc **早就写过这条订正**，只有 `Debug.Log` 那句没跟着改 ⇒ 本次是**把落下的那一半补上**（铁律 5「把同一句话被复制到别处的地方一起改」）。
- **验证**：`TMPDIR=/tmp/wf_main bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时错误 0 / 编辑器错误 0**；
  `git diff --numstat` = `12 增 / 3 删`（**没翻行尾**）。

---

### 2. `A1191` —— `WindowHeader` 那一处「同形状再来一份」 —— ✅ **2026-10-10 裁定：框定不成立、无账可做**（**调度台自己动手**）

**原文（`项目任务.md` §29·b 第一节那一行，一字未改）**：

> ⚠️ **`WindowHeader` 那一处「同形状再来一份」**（`P1` 做 `A1173` 时顺手查出，⛔ 没改）—— `Shell/MenuWindowBase.cs:777` 的 `WindowHeader` 用 **`MenuDraw.Text(…, TitleFontPx, s.QTitle)` 不传 `wrapPx`**，随后**手工** `title.SetAutoFitBox(fitW, fitH, …)`（`:787` / `:792`）⇒ 这**正是 `A1173` 刚消灭掉的那种「两处写同一条规则」**。⚠️ 修它要么动 `MenuDraw.cs`（**共用件**）、要么改 `WindowHeader` 自己走本窗漏斗 ⇒ **做法/判据待定**。⇒ **要做**（铁律 11）。出处 → `资料/普查产出_第十会话/P1_A1173.md` §7·①。

**✅ 裁定（判据 = 现读源码 + 与 `A1195` 同一族的先例）**：**不是「两处写同一条规则」，是 `TextCore` 的【能力缺口】** ⇒ ⛔ **不该把形状统一**。

| 事实 | 现读判据 |
|---|---|
| `MenuDraw.Text` **十个形参里没有 `charSpacing`** | `Shell/MenuDraw.cs:1809-1812` 签名 —— `(parent, r, text, color, name, fontPx, q, wrapPx, autoMinPx, autoMaxPx, autoBasePx, clip, clipSoftness)` |
| 自适应被藏在 `wrapPx > 0f` 那道闸里 | `Shell/MenuDraw.cs` 的 `TextCore`：`if (wrapPx > 0f) { SetWrapWidth(…); if (autoMinPx > 0f && fontPx > autoMinPx) SetAutoFitBox(…); }` |
| 本处要的是**三档次序**（带字距） | `MenuWindowBase.TitleFit` 的 doc：`SpacingOnly`（**完全不要**自适应）· `FitBeforeSpacing`（自适应→字距）· `FitAfterSpacing`（字距→自适应）⇒ `TextCore` **一档都表达不出** |
| 「手工 `SetAutoFitBox`」是**既有且普遍**的写法，不是孤例 | 现读 `grep -rn "SetAutoFitBox("` ⇒ `Deck/DeckRuntime.cs` 5 处 · `Battle/UnitChatPanel.cs:344` · `Editor/ChatBoxProbe.cs:40/55` · `Editor/BattleScene.cs` 十几处 · … |
| 而**规则本身只有一处**（没有第二份） | 就是 `Battle/Label.cs` 的 `Label.SetAutoFitBox`；各站的 `min/max/base` 是**各自 prefab 的值**，不是规则的副本 |

⇒ **与 `A1195` 同一族的裁定**（两个漏斗契约语义不同 ⇒ **写清差异、⛔ 不统一形状**；统一 = 丢掉原版真有的组合）。
**本件的收口 = 把这条差异写进 `Shell/MenuWindowBase.cs:776` 之前那段注释**（含「真要合并得先给 `MenuDraw.Text` 加『字距 + 次序』两维、那是共用件的改动」这一条留痕），⛔ **不是**改形状。

- **验证**：`TMPDIR=/tmp/wf_main bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
- ⚠️ **残留（如实登记，⛔ 不属本账）**：若将来真要给 `MenuDraw.Text` 加那两维，**那是一次共用件改造**，判据要另立（且必须先想清「`TextCore` 还是 `TextBox` 的内层」这一层影响面）。

---

### 3. `A1206` —— `CampaignTab` 两处把 `max` 抄成了标称 —— ✅ **2026-10-10 已修**（**执行代理 P-B 交件，调度台复核**）

**原文（`项目任务.md` §29·b 第一节那一行，一字未改）**：

> ⚠️ **`CampaignTab` 两处把 `max` 抄成了标称**（`R5` 现核查出，⛔ 没改）—— `:209 _title` 传 **31.75**，原版是 **`auto[25~35]`**；`:237 _points` 传 **34.8**，原版是 **`auto[18~40]`**（两处的 `min`/`base` 已对，**只错 `max`**）。⚠️ **`A274` 只修了 `panTitle` 那一颗**（同族漏了两颗）⇒ **真偏离**，⇒ **要做**（铁律 11）。出处 → 同上 §2·⚠️。

**✅ 收口内容**（`CardPresentation/Shell/CampaignTab.cs`，**LF**）：

| # | 落点 | 改前 → 改后 | 判据 |
|---|---|---|---|
| 1 | `:211` `_title` | `autoMaxPx: 31.75f` → **`35f`** | 原版 `Campaign Header/Title` = 字号 31.75 · 基准 36.0 · **`auto[25.0~35.0]`** · 折行 0 |
| 2 | `:244` `_points` | `autoMaxPx: 34.8f` → **`40f`** | 原版 `Campaign Header/Points` = 字号 34.8 · 基准 36.0 · **`auto[18.0~40.0]`** · 折行 0 |

- **判据复核**：`P-B` **没照抄 `R5`**，自己跑了一遍 `python -I d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Rewards Base Submenu Variant" --depth 8` 实读，与 `R5_包装层菜单族.md` §3 注③ / §6 隐患 2 **逐位一致**。`min`/`base` 一格没动。
- **验证**：`TMPDIR=/tmp/wf_pb bash d:/4/Unity/工具/typecheck.sh` ⇒ 运行时 0 / 编辑器 0；`git diff --numstat` = `12 增 / 4 删`（没翻行尾）。
- 🔑 **配套补的断言（调度台自己加的，`Editor/RewardsScene.cs`，2026-10-10）**：`P-B` 顺手查出**这两颗的 autosize 窗口一直零断言覆盖**（本文件里与它们有关的只有 `A303②` 的「折行 = 0」与「左边缘 = 480.69」⇒ 四格 `min`/`max`/`base`/折行 里**三格没人兜着**）⇒ 在 `:6813` 之后补了两条 `CheckFontWindow(camHeader, "Title", 25f, 35f, …)` / `(…, "Points", 18f, 40f, …)`。
  ⚠️ **如实登记两处缺口**：① **`base` 那一格 `CheckFontWindow` 断不了**（它只收 `wantMin/max`）⇒ 别拿它冒充断全了；② 起找的父节点**必须是 `Campaign Header`**（助手内部是 `FindChild` 按名递归 + **单参** `GetComponentInChildren<Label>()`，整棵树里叫 `Title` 的不止一颗 ⇒ 从 `camView` 起找会串味）。
  **改坏法**（写进断言文案）：把那两行的第 4 个实参改回标称 `31.75f` / `34.8f` ⇒ 上界读成 31.75 / 34.8 ≠ 35 / 40 ⇒ 这两条红。
  **验证**：`TMPDIR=/tmp/wf_main … typecheck.sh` ⇒ 0 / 0；`git diff --numstat` = `17 增 / 0 删`。
  ⚠️ **断言【没跑过】**（铁律 12：待办没做完不跑自检）⇒ 收口那趟要跑 `RewardsScene.Run`（它属 `Shell`/`Rewards` 那一族）。
- ⚠️ **一处日期口径留痕**：`P-B` 报本机系统日期是 **2026-10-10**，而同文件邻近注释块（`A305①`/`A303②`）与本批 `普查产出_1011/` 都写 **2026-10-11**。它**跟着同文件既有口径用了 2026-10-11**（没去改别人的日期）。**两个日期谁对、要不要统一 —— 留着，⛔ 别再各写各的。**

---

### 4. `A1176` —— 震荡那一格缺「那张牌还在不在棋盘上」 —— ✅ **2026-10-10 已修**（**执行代理 P-D 交件**）

**原文（`项目任务.md` §29·b 第一节那一行 —— ⛔ 已在正本里删掉该行；下面是它的要点，全文见 `git log`）**：

> ⚠️ **震荡那一格缺「那张牌还在不在棋盘上」** —— `A1166` 已查实原版那道闸 = **`CardScript.IsInPlay()`**；**但震荡那一格只有 `target.IsAlive`、没有「还在棋盘上」**⇒ 若某个死亡触发在伤害后、震荡前把目标弹回手牌，原版判假、我们仍会晕它。 —— ✅ **2026-10-10（`R3`）已查实，🔴 裁定【要补】，但方向与当初的框定【相反】**：③ **它查到一条【真的、可达的】差异 —— 我们【少晕】**：`CheckIfDead.c:110-147` 有一条**不置 5** 的支路 `HasToTransformIntoRemnant`（= `remnant(0x41a)` ∨ `waystone(0x474)`）⇒ 与 stun **同队列**、stun **入队更早** ⇒ FIFO **先晕、后翻面** ⇒ **原版会晕**。🔴 **正解 = 让判别式对齐原版** —— ⛔ **不是**加「还在棋盘上」（方向反了）。⚠️ **改前必须核** `targetDied` 另三个用户（Stomp / Sniper / Markerlight）。

**🔴 收口时把简报的前提【推翻了】（理由成立）** —— `P-D` 逐条现读 `d:/2/tools/decomp_full/`，核出 `targetDied` 一共 **5 处用途 + 3 处写入点**，**原版那 5 处没有一处用「会不会翻面」**：

| 用途 | 原版判据 | 与「会不会翻面」一致？ |
|---|---|---|
| **Stomp** `:3534` | `ShouldTriggerStompDamage.c:20-36`：`HasCurrentTrait(攻方,0x4d8)` ∧ **`*(int*)(目标+0x68) < 0`（裸血）** ∧ 相邻表>0 | ❌ 纯裸血 |
| **Sniper** `:3555` | `ActivatesNoReturnSniperAttack.c` → **`DamageKillsTarget`**（**预测**）+ `!IsProtectedFromDamageOrSurvivor` | ❌ |
| **Markerlight** `:3587` | `_ResolveAttack…:1254` **`EnoughPendingDamageToDie(目标)`** | ❌ |
| **`sniperKillEarly`** `:3408` | `EnoughPendingDamageToDie(攻方)` | ❌ |
| **星镖档跳主伤害** `:3463` | `_ResolveAttack…:616/:632` 的 2 字节双标志（`+0x139`/`+0x13a`） | ❌ |

⇒ **裁定：`targetDied` 的定义【一个字节都不动】**（照简报第一种形状会**一次性引入 3~4 处新偏离**）；**只有第 6 处用途（震荡那一格）的原版判据是 `IsInPlay()`**。

**实现**：新增**具名判定口** `RuleCore.StunStillOnBoard(ctx, tgtP, tgtSlot, target, targetDied)`（`RuleCore.cs:2892`），震荡那一格（老闸 `!targetDied && target.IsAlive`）改成调它。
- ① 没被打死 ⇒ 返回 `target`（**判据一个字节没改**，只是从调用点搬进来）；② 打死了 ⇒ **唯有** `Board[tgtSlot]` 是「`IsRemnant` ∧ **实例相等**」的才算还在场上，返回它。
- 🔑 **不构成第二份判别式**：它读的是**棋盘事实**，不用「会不会翻面」的预测；而 `IsRemnant = true` 全仓**只有 `RuleCore.cs:4465` 一个写点**（现核 `grep`）⇒「什么算翻面」这条规则仍**只有一处**。
- 🔴 **必须返回棋盘上占位的那一个**：`CleanupDeaths` 翻面时**新建** `rem`（`UnitState._keywords` 每实例一份）⇒ 挂旧 `target` = **静默丢掉**。
- ⛔ 注释里明确否掉两种写法：「加一条『还在棋盘上』」（方向反了）·「`Board[slot].IsAlive`」（会晕到搬进来的别人身上）。

**断言**（`RuleEngine/Editor/RuleEngineTest.cs` `TestAttackKeywords` ③ 之后；⚠️ **只写没跑**）：③′（`Remnant`：5 攻 Concussion 打死带残骸的单位 ⇒ 翻面**且**被晕）· ③″（**灭自证的另一半**：同一发打**不带**残骸的 ⇒ 格位空、整张敌方棋盘一个被晕的都没有）· ③‴（关键词换 `Waystone` ⇒ 同样翻面**且**被晕）。
🔑 **灭自证成立的理由**：③′/③″ **只差「会不会翻面」一个变量**而结果要求相反 ⇒ 改回旧写法红、放宽成「血≤0也晕」红、挂回旧对象 ③′ 红 —— **不可能一起改回去还全绿**。

**顺带就地更正**：`RuleCore.cs` 里 `A1166` 那块「已判等价」的结论（差的就是残骸那一档，错因 = 「血 ≤ 0」≠「这一批必死」）；并把它挂着的那条「**没查清**：`CheckIfDead` 与眩晕结算的先后」**改成已查清**。

- **验证**：`TMPDIR=/tmp/wf_pd … typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（跑两次）。`git diff --numstat`：`RuleCore.cs` **117/31** · `RuleEngineTest.cs` **67/0**（**行尾没被翻**）。
- 🔴 **断言【没跑过】** ⇒ **收口那趟必须跑 `RuleEngineTest.Run`**（动了 `RuleEngine/Core/` ⇒ **必跑这一条**）。
- 📌 **`P-D` 顺手订正了调度台的一处事实错**：简报写的自检文件路径 `CardPresentation/Editor/RuleEngineTest.cs`**不存在**，真身是 **`RuleEngine/Editor/RuleEngineTest.cs`**（**CRLF** · 23497/23497，现核）。⇒ **简报里的路径类事实要现核**。
- 🆕 **它顺手带出的三条已另立账**：`A1223`（`Slay`/`Kills`）· `A1224`（`EffectResolver.DoStun`）· `A1225`（`survivor(430)` 补记，归 `A1218②`）。


