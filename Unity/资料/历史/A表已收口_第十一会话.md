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

---

### 5. `A1211` —— 两处注释 + 一份报告里的认错件要订正 —— ✅ **2026-10-10 已修**（**①②都收了；① 是 `P-A` 顺手做的、② 调度台做**）

**原文（一字未改）**：

> ⚠️ **两处注释 + 一份报告里的认错件要订正**（`R4` 顺手查出，铁律 5）—— ① 🔴 `Shell/EnergySinglePlayerOnlyEventWindow.cs` 里**两处**写着「ToS 的 URL 是**原版运行时拼的**」—— **与事实不符**（它是**静态字段、恒 `null`**，见 `A1209`）；`Shell/SettingsWindow.cs` 的 `Sp*` 段**同源**、**一并核**。② `P8_A1190.md` §6·2 把那个 `0.8696` 记到 `GenericScoreBarLine` 头上 —— **实际长在 `Skull` 上**（而 `Score` 是 `Skull` 的子件）⇒ **就地订正**。⇒ **要做**（铁律 5）

#### ① —— 🔴 **这条账的【前提】本身是错的（记错了文件）**，真实落点已由 `P-A` 修完

- **实据**：`SpFaqUrl` / `SpTermsUrlNote` / `SpTermsNoUrl` **三个符号全在 `Shell/SettingsWindow.cs` 里**（现读 `:1105-1107` / `:1115-1123` / `:2696-2703`）；
  **`Shell/EnergySinglePlayerOnlyEventWindow.cs` 里一处都没有** —— `git log -S"ToS" -- <该文件>` **零提交**（该文件从没有过这个词），`grep -n "ToS\|Terms\|URL\|Url"` **零命中**。
- **错因** = **记对了【符号】、写错了【文件】**（`Sp*` 那一族确实是那一栏外链钮的常量，但它长在 `SettingsWindow` 类里）。
- ✅ **两处已订正**：`P-A` 做 `A1209` 时就地改了（含 `SpTermsUrlNote` 那个**英文串**），并在报告里列出「就地更正 6 处旧说法」↔ 本账的落点重合。
- ✅ **`R4` 报告已就地订正**（`资料/普查产出_第十会话/R4_布局刻度族查实.md` §七·1，保留订正痕 + 记下错因）。
- ⚠️ **同一条顺手记下的反例**：本条自己就是一例「**按文件名找归属、没按助手名找**」——
  同一族错在第十会话 `A1212` 追那 30 行时也犯过一次（`PageTitle`/`ActionButton` 被记成「没找到归属件」，其实它们是 `SettingsWindow` 类里的私有助手）。
- ⚠️ **另有一处【真正同源、仍然成立】的、本条没点到**：`SettingsWindow.cs:856` 与 `:2363` 写「原版 **Twitch** 那条外链是运行时用玩家存档拼出来的」——
  **那两处判据是齐的**（`AccountTab__OnSetup.c` 末尾那条 5 段 `System.String.Concat`）⇒ **不是 `A1209` 那一档、不适用本条订正**，⛔ **别顺手去改它**（已写进 `R4` 那节的订正块）。

#### ② —— ✅ **已就地订正**（`资料/普查产出_第十会话/P8_A1190.md` §6·2）

那个 **`0.8695654273033142` 长在 `Skull` 上**，⛔ **不是** `GenericScoreBarLine`（后者自身 `localScale = 1`）；
**树的形状是「`Score` 是 `Skull` 的子件」**。现读链：`Score` ← **`Skull`(`0.86956543`)** ← `Score Line Level k`(1) ← `Score Levels`(1) ← `Scoring Bar Event Score Info`(`1.32`) ⇒ 积 **`1.1478264`**（本件写的「1.148」是截断、值对）。
**错因** = 只看 `⇲ls` 那一列的**名字**、没解 `m_Father` 父链。
🔑 **连带一条更值钱的**（`R4` 顺手记下、归 `A1197`）：**我们摆 `Score` 时把它当成了【行节点的直接子件】** ⇒ **树形本身就与原版差一层**。

---

### 6. `A1223` —— `Slay`/`Kills` 可能是同一族 —— ✅ **2026-10-10 查实：【不成立】**（`R-N` 只读查证）

**原文（一字未改）**：

> 🔴 **`Slay`/`Kills` 可能是 `A1176` 的【同一族】**（`P-D` 做 `A1176` 时顺手查出，⛔ 没改 · **置信度中下**）—— `RuleCore.cs:3754` 写的是 `bool killed = !target.IsWarlord && !target.IsAlive;`（**用的是 `IsAlive`、不是 `targetDied`**）⇒ **翻面成残骸的目标会被当成「被摧毁」** ⇒ 触发 `Slay`/`Kills` 那一族关键词。原版线索：`AddTriggerSlay` **全库只有一个调用点**（`AbilityLogic__PlayAbility.c:1903`），而 `ResolveTriggerSlay.c:128` 判的是 **`IsInPlayOrDying()`（凶手还活着）**、**不判目标死活**。⇒ **先做只读查证、再决定改不改**（铁律 11）。出处 → `资料/普查产出_第十一会话/PD_targetDied残骸档.md` §8·①。

**🔴 裁决：`P-D` 担心的那条【不成立】** —— **原版对「刚翻面」的目标【照样】触发 `Slay`**，与我们一致。

**判据（`R-N` 新查的，全部在 `d:/2/tools/decomp_full/`）**：
- `Slay(120)` **全库只有两个发射点**（多行感知扫了 **111 个** `RawCardScript__OnTrigger` 调用点）：
  **A = `CardScript__ResolveDeadCard.c:259`**（死亡结算，**`P-D` 那份漏了这一处**）· **B = `CardScript__TriggerSlay.c:16`** ← `ResolveTriggerSlay.c:226` ← `AddTriggerSlay` ← `AbilityLogic__PlayAbility.c:1903` = `case AbilityEffect.triggerSlay(467)` = **「强行触发某单位的 Slay」**（卡面 `Master Lazarus`）⇒ **`P-D` 那条线索不是普通击杀的路**。
- **普通击杀走 A**，闸（`:242-256`）= 解析出那张 **== `action.actingCard`（凶手）** ∧ 凶手 `cardState != 5` ∧ 死者 `cardType != 10`（不是督军）∧ **死者 `deathType ∈ {combatDefender(15), ability(20)}`** ∧ 凶手属当前回合方 ∧ **死者 `isRemnant(0x65) == 0`**。
- 🔑 **`0x65` = `isRemnant`**（`EntityScript__set_isRemnant.c:5` / `TransformIntoRemnant.c:29` / `TransformFromRemnant.c:10`）；它在 `CheckIfDead.c:123` 决定「真死 vs 翻面」。而 **`TriggerUnitBacklashActions` 先于 `AddTransformIntoRemnant` 调用** ⇒ **死亡结算那一刻死者 `isRemnant` 还是 `0`** ⇒ 闸真 ⇒ **原版照样触发** ⇒ **与我们一致**。
- 旁证：`local_28` = `UnitDeathType` 实证（`TriggerOnMinionDeath.c:97` → `OnMinionDeath.c:19-27` 把 `10/0x14/0x28` 映成 `175/180/170`）；`param_1` = 死亡广播列表里那一张（`BroadcastDeadUnit` 遍历 `bm+0x470`）⇒ 四块（`DeadHero` / 死亡触发 / `Slay` / `Requiem`）在这一读法下**全部自洽**。

**🔴 但同一条判据带出两条【真差异】**（都要做，铁律 11）⇒ 已另立 **`A1249`**（我们**多**算了一档）与 **`A1250`**（我们**少**算了一档）。

**⚠️ `R-N` 顺手订正一处行号**：`IsRemnant` 的**唯一写点现在是 `RuleCore.cs:4551`**（`P-D` 报的 `4465` 已因本轮改动挪位）。
**⚠️ 没查清**：字段偏移**没拿 `dump.cs`/IL 核过**（`R-N` 用字段声明序 + 状态机拷贝基址 `+0x20` + `TriggerUnitBacklashActions` 的构造三段推的）· `bm + 0x470` 是什么列表没查 · 差异② 的可达性没数 · 25 处 `!IsAlive` 的同族性没逐条核 · **两条都没做实况验证**。

---

### 7. `A1154` —— 替身（Bodyguard）原版是【一条 ability】 —— ✅ **2026-10-10 已修**（**执行代理 P-K 交件**）

**原文（一字未改）**：

> ⚠️ **替身（Bodyguard）原版是【一条 ability】**（`BattleManager__AddRedirectedAttack.c`，唯一调用点 `AbilityLogic__PlayAbility.c:2730`），**不是**「攻击声明时扫场上」—— 我们那一支仍是 **gd 旁证口径**（`B1` 已在注释里如实标「旁证一致、**不是查实**」）。⇒ **要做**（铁律 11）。

**结论（`P-K` 逐跳现读）**：原版那条链的形状确实是 **`AbilityTrigger.AboutToAttack(280)` 的一条 ability** ——
`_ResolveAttack` → `CheckUnitsCancellingAttack` → `HasCancelAttackAbility` → `CanTriggerAbility` → `CancelAttack`/`ClearPendingDamage` → `ResolveCancelAttack` → `OnTrigger(0x118)` → `AbilityLogic.PlayAbility` 的 `case 0xec` → `AddRedirectedAttack` → `BattleActionType.redirectedAttack(74)`。
🔑 **但净效果与我们那支「就地换目标」在结果上等价**（同一次伤害 / 同一次反击 / 同一条攻击事件）⇒ **正解不是换一套行为**，而是**按判据补齐四道闸 + 两条不变式、收成一个具名口**。

**三处硬判据（现读，不是转述）**：
1. 🔴 **反汇编钉死一个歧义**（`.c` 里 Ghidra 把 `this` 省掉了）：`AddRedirectedAttack` 头两道闸判的是 **`actingCard`（打人那张牌）**、不是新目标 —— `disasm_va.py 0x180953D70`：`180953DE2 mov rcx,rbx`（`rbx = param_2`）在分支之前就装好，`180953DED call EnoughPendingDamageToDie`。
2. **`+=0x120` 是什么**：`il2cpp_out/dump.cs` 的 `EntityScript.currentAttackType // 0x120` ⇒ `PlayAbility.c:2731` 那句 = **重定向沿用【攻方当时那一档打法】**（不是替身的打法）。同理 `BattleManager.unitsInPlay // 0x470`（扫的是**全场**）· `CardAbility.targetCriteria // 0x30`（**筛被打者**的 criteria）。
3. **两条短路**（各配了断言）：`AllowResolveAttack.c:134-136` 对 `redirectedAttack(0x4a)` **直接 `return 1`**（⇒ 不复检目标合法性，**带 `Stealth` 的替身照样挨打**）；`:528-533` 的 `if (actionType != 0x4a)`（⇒ 重定向过的那一记**不会被再截一次**）。
- 顺带：从 `all_strings.txt`（`69591552`/`69591808`）拿到两条日志原文，并把 `AddScriptedAttack` 里那道 trait `100` 认成 `DefinedTrait.stun`（与 `DefinedTrait.cs:10` 对上）。

**改动**（白名单两个文件）：`Core/RuleCore.cs` **150/30** —— 新增具名口 **`TryRedirectAttackToBodyguard`**（判别式**只此一处**，照 `StunStillOnBoard` 先例）+ **补攻方那两道闸** + **补 `jam` 例外**；`DeclareAttack` 里那 23 行内联换成一行调用。
`Editor/RuleEngineTest.cs` **136/0** —— 替身格从 **4 条扩到 25 条**（新增 ②·a~②·e 共 21 条），含 **灭自证条 ②·d**（「只把伤害挪过去」/「只另排一条攻击」两种错路都过不了）与 **反面 ②·e**（打普通部队时谁都不许动目标 ⇒ 挡「无差别都改到替身身上」）。
🔑 **没有新增「会改引擎状态的动作路径」** ⇒ 两个记账口 / 录像 / 联机**都不受影响**（重定向是 `DeclareAttack` 内部的确定性函数）。

- **可达性**：全池 1126 张里**只有 1 张**走这条 —— `Vargard Obyron`（`SAU42` · Sautekh · 9 费 10/9/远程 5 · `Armour 2`）。
- **验证**：类型检查 **4 次**（最后一次在全改完之后）⇒ **运行时 0 / 编辑器 0**。行尾 `RuleCore.cs` **LF 6615** · `RuleEngineTest.cs` **CRLF 23700/23700**（**都没被翻**）。⛔ 没跑 Unity ⇒ **收口要跑 `RuleEngineTest.Run` 一条**。
- **⚠️ 没查清**（都如实写进代码注释，⛔ 没拿猜测填空）：① 同方 **2 个以上替身**时原版落在哪一个（原版收的是 `List`、对每个命中单位各排一条；我们**保持取槽号最小** = 旁证口径）；② `unitsInPlay` 含不含督军（我们跳过督军那一格仍是**旁证**）；③ `Vargard` 那张卡的 **ability 资产在远端 CCD** ⇒ 那几条 criteria 的**具体取值是按卡面那句话推的**（代码那一半查实）；④ 我们的 `EnoughPendingDamageToDie(攻方)` 那一半**表示不出来**（没有 `pendingDamage` 队列）—— 已论证在本路径上等价于 `IsAlive`。
- 🆕 **它顺手带出的三条已另立账**：`A1256`（强制攻击被替身截 —— **真偏离**）· `A1257`（`CardDef` 的 `Bodyguard` 注释读点过期）· `A1258`（原版「取消攻击」是两条独立通道、替身只占第 2 条）。

---

## 已做完的账（2026-10-11 收尾归档 · 整行原文 · 一字未改）

> 🔴 **归档纪律**：**这些行的实现 + 断言都已在 2026-10-11 收口那趟全套 12 条里跑绿**
> （**20,121 条断言 / 0 失败**）⇒ 从 §29·b 整行搬出。**判据 / 别推翻的结论都在行内**，
> 要查某条当时怎么收口的 ⇒ **按 A 编号在本文件里找**。

| 🆕 **`A1178`** | ⚠️ **能量窗那两颗的框宽是「我们摆的」，原版是「跟着文字走」**（`W2` 做 `A1126`·A1 时查出，⛔ 没改）—— 原版 `EnergySinglePlayerOnlyEventWindow` 的 `Victories title` / `Timer` 两颗是 `m_SizeDelta.x = 0` + **`ContentSizeFitterMinMax(h:PreferredSize)`**（`menu_dump` 工具标 `⚙CSF` = 算不出）⇒ **原版运行时框宽 = 文字 preferred width、永不横向缩**；我们写死 **254.112 / 289.72**（`vtR`/`txR`）⇒ 若我们更窄，**开自适应会让字比原版小一点**（本次已按原版 `auto[18~53.5]`/`auto[10~38]` 接线，但框宽这一层没复刻）。🔴 **原版 preferred 量不出**（要 TMP 字体度量）⇒ **要做**（铁律 11）；差多少 / 怎么复刻 CSF 那一档，**待查**。出处 → `资料/普查产出_第十会话/W2_A1126A1.md` 裁·2。 ✅ **2026-10-10（第十会话 · `R4`）已查实 —— 结论与原来的担心【相反】**：① 原版那族的**字体**是 `Pragati-Regular SDF`（`pointSize 95`、**无 kerning/ligature**）⇒ **preferred width 算得出**（公式 = `Σ advance × (fontSize/pointSize)`，出处 `TMP_Text.cs:3684-3717`，用 `m_fontSizeMax` 算）：`'Victories: '@53.5 = 176.98` · `'Termina en: 23d 5h'@38 = 248.44` · `'751'@77 = 96.81`。② 🔴 **我们比原版【更宽】、不是更窄**：`254.112 vs 176.98`（**+44%**）· `289.72 vs 248.44`（**+17%**）⇒ **开自适应不会缩字**（`P8`/`P2` 担心的那一档不成立）；残差只在「运行期文案更长」时（原版撑框、我们缩字）。③ **裁定：要复刻 CSF 那一档**（铁律 11）—— 我们框架**没有** CSF 机制（`MenuDraw.TextCore` 收死 `PxRect`、`Label.SetAutoFitBox` 只写 `sizeDelta.x`）⇒ 代价 = 建树期用 `TMP_Text.GetPreferredValues` 量一次写回，且**换语言后原版会自动变、我们不会**（要么只给固定文案、要么补重量路）。⚠️ **没查清**：我们菜单 TMP 到底用哪个字体资产渲染（`Resources/Fonts/` 只有 NotoSerifCJK + Trait）—— 若是它，复刻该量**我方**宽度、⛔ 不是抄那两个数。出处 → `资料/普查产出_第十会话/R4_布局刻度族查实.md` §A1178。 |
| 🆕 **`A1197`** | 🔴 **两扇事件窗【内部有缩放子树】：我们把 `localScale` 烘进了【框】、却没烘进【字号】**（`P8` 做 `A1190` 时顺手查出，⛔ **没改**）—— 现读 `⇲ls=`：能源窗 `Score` 的祖先刻度 = **1.148**（1.32×0.8696）、`Collect/Button Text` = **1.32**（`SkirmishEventWindow` 那两颗**没有**）。我们**框取视觉值**（`Energy…:126` 那条 `79.84 = 60.482×1.32` 的注释就是），**字号却传原版字段原值**（48 / 55）⇒ **原版那一颗实绘 ≈55.1 画布 px、我们 48 ⇒ 相对框小 ≈13%**（③ ≈24%）。⇒ **要做**（铁律 11）。🔴 **但先要查实**（`P8` 自己标了三条不确定）：① 不能跑 Unity 并排比；② **没有反编译证据**说「原版 autosize 是在**本地单位**里二分的」（那是推论的前提）；③ 这条链**跨两笔账**（**框**归 `A1178`、**字号**无人认领）⇒ **先查实再决定改框还是改字号**。出处 → `资料/普查产出_第十会话/P8_A1190.md` §5·2。 ✅ **2026-10-10（第十会话 · `R4`）已查实 —— 裁定 = 【真偏离】，而且修法已定**：① **刻度链**（`⇲ls` + 直读 `m_LocalScale`）：`Score` ← **`Skull`(`0.86956543`)** ← `Score Bar Line Level k`(1) ← `Score Levels`(1) ← **`Scoring Bar Event Score Info`(1.32)** ← … ⇒ 祖先积 **1.147826**；`Collect/Button Text` ← `Generic Simplified UI Button` ← **同一颗 1.32**。🔴 **订正 `P8` 报告 §6·2**：那个 `0.8696` 长在 **`Skull`** 上（⛔ 不是 `GenericScoreBarLine`），**而 `Score` 是 `Skull` 的子件**；来历 = prefab 里**序列化写死**的 `m_LocalScale`（那两个组件都不管 localScale）。⚠️ **同一子件在 Skirmish / Two-Sides 窗里是 `1.2563`**（实例覆盖、**逐窗不同**）⇒ **修的时候逐窗取值**。② 🔴 **「原版 autosize 在【本地单位】里二分」有硬判据**（不是推论）：`com.unity.ugui@…/Runtime/TMP/TextMeshProUGUI.cs:2342` `m_marginWidth = m_rectTransform.rect.width - m_margin.x - m_margin.z`（`rect` 是**局部量**）+ 二分两端 `:3424-3428`/`:4488-4502` 都改 `m_fontSize` 与局部 `marginWidth` 比。（⚠️ `decomp_full` **只覆盖 Assembly-CSharp**、**没有 TMP 的 `.c`** ⇒ 这类问题要读**工程自带的包源码**。）③ **裁定**：原版屏上字号 = 本地 `48×1.148 = 55.1` / `55×1.32 = 72.6`，我们传 48 / 55 ⇒ **相对框小 −12.9% / −24.2%** ⇒ **真偏离**。**修法 = 把祖先刻度乘进 `fontPx`**（与 `SettingsWindow` 的 `fs * RootScale` **同一条规矩**）；⚠️ **框不要再乘**（`ScoreW = 133.32` 已经烘过）。出处 → 同上 §A1197。 |
| 🆕 **`A1200`** | ⚠️ **`AspectRatioFilter` 那一族（6 颗 `… Button Text`）：我们传【字段值】，而原版**跑起来**用的是【ARF 之后】的值**（`P7` 做 `A1194` 时查出，⛔ 只报不改）—— 逐颗差：`Redeem` / `Close Game` **90 vs 53.30**（差 36.7）· `Twitch` 50.06 vs 52.78 · `LoginWindow Login` 52.37 vs 55.09 · `Discord` / `IG` 33.55 / 13.55 vs 19.73。🔴 **但【没查清】**那 6 颗的 ARF **到底开没开**（`menu_dump` 只印 `⛔MULTI-COMP(6组件·1禁用)`、**不说是哪个组件禁用**）⇒ **要一条探针才定得下来，⛔ 别拿今天的读数当结论**。影响面（只描述）：这几颗文案短、折行 0、**框高今天不夹字号** ⇒ 画面上看不出差。⇒ **要做**（铁律 11）。出处 → `资料/普查产出_第十会话/P7_A1189A1194A1196.md` §2·②。 ✅ **2026-10-10（第十会话 · `R3`）已查实 —— `ARF` 是【开】的 ⇒ 真偏离、要做**：逐组件现读 `m_Enabled`：那棵树里 **36 个 `Button Text` 全部**挂 `AspectRatioFitter`，**36/36 `m_Enabled = 1` · `m_AspectMode = 1`（宽控高）· `m_AspectRatio = 5.140573`** ⇒ `P7` 说的「ARF 后」**就是运行时真值**；工具那句「6 组件·1 禁用」**禁的不是 ARF**。旁证：`menu_dump` 开布局会打 `⚙ARF 宽控高(5.14057)`。🔴 **要改的是 12 颗里的【9 颗】**（`P7` 点名的 6 颗只是子集）：Redeem/Exit Game **300×90 → 274×53.303**（高 +36.70、宽 +26）· Unregistered Login 50.58→53.303 · Twitch 50.06→52.777 · Logout 50.58→53.303 · LoginWindow Login 52.36→55.087 · Discord 33.55→**19.733** · IG/FB/Twitter 13.55→19.733 · Youtube 18.55→19.733；**Register / Delete / Switch 三颗本来就对**（prefab 字段已烘成 ARF 值）。⚠️ **宽与高要一起改**（ARF 的高由宽推出来）；改完 `AcSocTxtH` 五颗全同（数组可消）。⚠️ **没查清**：`P7` 那句「框高今天不夹字号 ⇒ 看不出差」**`R3` 一次没核**（要跑 Unity）；Redeem/Close 的**宽**（300 vs 274）对 ARF 的连锁没算。出处 → 同上 §三。 |
| 🆕 **`A1204`** | ⚠️ **原版 `Tab Buttons` 父级那层 `Mask Tabs buttons` 我们仍未实现**（`P10` 落地 `A1186` 时记下，⛔ 没做）—— 原版那层是 `RectMask2D`（与本仓已在用的**视口裁切**同族）。⚠️ `P10` 说回到 5 键后 `surplus = +5.99`、**不再溢出** ⇒ 这层现在**只影响很小的出入眉**；但**原版有它** ⇒ **要做**（铁律 11）。⇒ **先补判据**：确认那层在原版的**实际裁剪矩形**、以及我们的 tab 组与它是否真有差（差多少、在哪些档）。出处 → `资料/普查产出_第十会话/P10_Online改入口.md` §5·③。 |
| 🆕 **`A1205`** | 🔴 **五个包装层【连 autosize 形参都没有】**（`R5` 普查包装层菜单族时查出）—— `MainMenuRuntime.Text` · `DeckInfoPopup.Txt` · `ImportDeckPopup.Txt` · `PracticeModePopup.Txt` · `MissionsTab.Txt1` ⇒ 本表里属于它们的 **28 处「该接」不是「忘了传」，是「没地方传」**（**与 `A1173` 对 `MainMenuSubmenuWindow.Text` 的诊断同形**）。⇒ **要做**（铁律 11）：**要接先动签名**（加四个可选形参，**全缺省 ⇒ 旧调用点逐位不变**，形状照 `A1173`/`A1181` 已落地的两件抄）。⚠️ 注意 `MissionsTab.Txt1` 曾是**「不换行」**那一路（原版 `m_TextWrappingMode = 0`）⇒ 接线时**别把折行打开**。出处 → `资料/普查产出_第十会话/R5_包装层菜单族.md` §4·4。 |
| 🆕 **`A1206`** | ⚠️ **`CampaignTab` 两处把 `max` 抄成了标称**（`R5` 现核查出，⛔ 没改）—— `:209 _title` 传 **31.75**，原版是 **`auto[25~35]`**；`:237 _points` 传 **34.8**，原版是 **`auto[18~40]`**（两处的 `min`/`base` 已对，**只错 `max`**）。⚠️ **`A274` 只修了 `panTitle` 那一颗**（同族漏了两颗）⇒ **真偏离**，⇒ **要做**（铁律 11）。出处 → 同上 §2·⚠️。 |
| 🆕 **`A1209`** | 🔴 **支持页 `Terms of Service` 我们停在出厂值 `1`、原版是【恒关】**（`R4` 查实 · **硬判据、不是推断**）—— `SupportTab__OnSetup.c` 末尾 `SetActive(TOS, *(*(DAT_18427be00+0xb8)+0x70) != 0)`；那个静态字段 = **`GameStaticData.TermsOfServiceUrl`**（`dump.cs:119411` 字段表 `0x70`；这也解释了为什么 privacy / FAQ / contact 三条是 `const` 内联字面量、**只有 ToS 要走静态字段**），而 **`GameStaticData__.cctor.c:147` 显式把它写成 `0`**（全仓 `grep` 逐条核过，写这个偏移的**只有这一处**）⇒ **原版运行时恒 `SetActive(false)`**（`OnSetup` 会被基类调）。⇒ **我们停在 `1` = 真偏离** ⇒ **要做**（铁律 11）：**关掉这一颗**（⚠️ **只关这一颗** —— `Privacy Policy` **不在**这个判据里）。出处 → `资料/普查产出_第十会话/R4_布局刻度族查实.md` §A1203。 |
| 🆕 **`A1210`** | ⚠️ **`Quality Value` 要照原版改 + `GenButton` 的 `Button Text` 要内缩**（`R4` 查实）—— ① 🔴 原版那颗 = **`Quality DropDown > Label`**（`TMP_Dropdown.m_CaptionText`）：**`18.0` · 基准 `14` · `auto[18~40]` · 折行 `0` · 色 `(0.67,0.67,0.67,1)`（灰）· `Left/Middle` · 框 `380.67×46.4` 设计 px**；我们 = `40`（= 本工程常数 `FontLabel`，注释自认「判据未定」）/ **白** / `340.66` ⇒ **要做**（铁律 11）。② `GenButton` 的 `Button Text`：原版**内缩**到 `274×53.3`，我们**摊满**整颗钮 `300×90` ⇒ **画面无差**（文案居中 + 装得下），按铁律 11 仍**要做**、**排在后面**。出处 → 同上 §A1193·②③。 |
| 🆕 **`A1216`** | ⚠️ **`ARF`（`AspectRatioFitter`）那一族要【按类】普查，⛔ 不止 `A1200` 那 12 颗**（`R3` 顺手查出，⛔ 没改）—— 那棵树里 **36 颗 `Button Text` 全挂 ARF**（`Steam Button` / `Support` 那几颗也在内）；⚠️ **抽屉的 `Price Display` 自己也挂 ARF**（`m_Enabled=1` · 宽控高 · ratio **`4.88480281829834`**；`Converted` 那颗 `sizeDelta.y = 93.5475`（= `456.96/4.8848`，**已烘好**）、`Ephemeral` 那颗 `= 0`（**靠 ARF 撑**））⇒ **`A1200` 只解决 12 颗那一小撮，这一族要按类盘一遍**（凡挂 ARF 的：**框宽宽高都要用 ARF 后的值**）。⇒ **要做**（铁律 11）。出处 → `资料/普查产出_第十会话/R3_三笔查实.md` §四·①②。 |
| 🆕 **`A1219`** | 🔴 **系统疑点：我们的 `wrapPx` 用的是【整格钮宽】，而原版那颗 TMP 是钮底下【更窄的子件】**（`P12` 顺手查出，⛔ 只报不改）—— 实测两例：价签那颗原版 TMP 的 `m_SizeDelta` = **`92`**（而钮是 `232`）· `Button Text` = **`213`**（而页签是 `260`）⇒ **我们的价签 / 页签字可能【渲得比原版大】**（折行宽给宽了 ⇒ autosize 少缩、甚至不缩）。⇒ **要派只读代理逐颗实读那几颗原版 TMP 的 `m_SizeDelta` 再定账**；⚠️ `P12` 本轮**没改实现、也没把期望值换成 `92`/`213`** —— 那会让**今天的绿基线立刻红**（**这是对的做法**：先查实再改）。⚠️ **这是一族**（凡「`wrapPx` = 整格钮宽」的站都要核）。出处 → `资料/普查产出_第十会话/P12_A1192余下.md`「两处必须报的发现·2」。<br>✅ **2026-10-10（`R-M` 只读普查）—— 本账【证实】，而且规模比预判大：不是 2 例、是 `5` 站**（`m_SizeDelta` 亲读，不是 dump 布局值）：**① `BoosterInfoPopup` 价签** 原版 `92.22 × 43.56` / 我们传 `232.17`（`PriceR.W`）= **2.52×**（`BoosterInfoPopup.cs:603-604`，`PriceR` `:150`）· **② `DailyRewardPopup` 价签** `92.22 × 33.06` / `174.05`（`PremPrice.W`）= **1.89×**（`DailyRewardPopup.cs:411-412`，`PremPrice` `:74`）· **③ `ShopWindow` 商店格价签**（= 块 6 #19）`92.22 × 33.06` / `176.22`（`CellPrice.W`）= **1.91×**（`ShopWindow.cs:780-781`，`CellPrice` `:312`）· 🔴 **④ `ItemDrawer.TextCentered` 抽屉价签** `108.97 × 93.55` / `row.W` = 原版 `Price Display` 条宽 **`640.8`** = **`5.88×`** ← **全表最离谱**（`ItemDrawer.cs:1021-1022`）· **⑤ `DeckSelectionPopup` 页签** `213.00 × 0`（`A[0.5,0-0.5,1]`）/ `260`（`TabW`）= **1.22×**（`DeckSelectionPopup.cs:572,583`，`TabW` `:120`）。⚠️ **我们自己的注释 `Shell/ItemDrawer.cs:1017-1019` 早就写着「原版没有一条固定的框宽」** —— 与这一族的成因**是同一件事**，两处说法现在合上了。⇒ **要做**（铁律 11）。 |
| 🆕 **`A1249`** | 🔴 **我们【多】算一档：打掉一个【本身已是残骸】的格子，我们也算击杀**（`R-N` 查 `A1223` 时带出，⛔ 没改）—— 原版 `CardScript__ResolveDeadCard.c:256` 有一道 **`isRemnant(0x65) == 0`** 的闸挡掉它，我们没有。⚠️ **真可达**（残骸 `Health = 1`）。🔑 **改法极小**：`RuleCore.cs:3754` 加 `&& !target.IsRemnant`（⛔ **别新造判据**；`IsRemnant` 的**唯一写点现在是 `RuleCore.cs:4551`** —— ⚠️ **`P-D` 报的 `4465` 已因本轮改动挪位**）。⇒ **要做**（铁律 11）。出处 → 同上 ①。 |
| 🆕 **`A1250`** | 🔴 **我们【少】算一档：用能力/效果摧毁【不算击杀】**（`R-N` 查 `A1223` 时带出，⛔ 没改）—— 原版那道闸允许 **`deathType = ability(20)`**（还有 `combatDefender(15)`）⇒ **效果击杀也算**；而我们的 `killed` **只在 `DeclareAttack` 里算**（全仓唯一发射口 `RuleCore.cs:3757`）⇒ **效果击杀一次都不触发 `Slay`/`Kills`**。⇒ **要做**（铁律 11），🔴 **形状要调度台定**：等于把「**击杀成立**」提升成一个**共用判定口**（⛔ **`Slay`/`Kills` 别写成两份** —— 本仓「两处写同一条规则 = 迟早不一致」）。⚠️ **该档可达性没数**（要逐张读 **21 张** `Slay` 卡）。出处 → 同上 ①。 |
| 🆕 **`A1256`** | 🔴 **真偏离：「强制攻击」在我们这边【会被替身截】、原版【不会】**（`P-K` 做 `A1154` 时顺手查出，⛔ 没改）—— `Core/EffectResolver.cs:1586` 调的是 `RuleCore.DeclareAttack`；而原版 `forceAttack(0x2a)` 由 **`_ResolveForceAttack_d__504`** 结算，**全库只有 `_ResolveAttack` 调 `CheckUnitsCancellingAttack`**（`P-K` `grep -rln` 只命中它自己 + 定义文件）⇒ 原版那条路**根本不经过替身那道闸**。⇒ **要做**（铁律 11）；🔴 **修法要动 `EffectResolver.cs`**（`P-K` 白名单外、⛔ 它没动）：给 `DeclareAttack` 加 `forced` 形参、由那个调用点传。⚠️ 可达性没数。出处 → `资料/普查产出_第十一会话/PK_A1154替身.md`「顺手发现」①。 |
| 🆕 **`A1257`** | ⚠️ **`Core/CardDef.cs:701-705` 的 `Bodyguard` 注释【读点已过期】**（`P-K` 报，⛔ 没改）—— 它写着「读点 = `RuleCore.DeclareAttack`」+「**旁证、不是判据**」；而读点已搬到新的具名口 **`RuleCore.TryRedirectAttackToBodyguard`**，且**判据那一半已经查实**（只剩 `P-K` ①②两格是旁证：多替身落点 / `unitsInPlay` 含不含督军）。⇒ **要做**（铁律 5：改为与事实一致）。出处 → 同上「顺手发现」②。 |
| 🆕 **`A1267`** | 🔴 **`TcpTransport.Fail()` 是【静默】的 —— 「为什么掉线」永远不进日志**（`F5` 查 X2 时撞上，⛔ 没改 · **`X2` 就卡在这**）—— `Net/NetTransport.cs:337-342` 只写 `_lastError`、**一行不打**，而 `_lastError` 全仓唯一消费点是 `NetSession.Send` 的前后对比。🔴 **生产侧也该有**：玩家看到的状态字 `St/PeerLostInBattle` **不含原因**、`LastError` **没有任何界面读它** ⇒ **违反「不许静默失败」**。⇒ **要做**（铁律 11）。出处 → `资料/普查产出_第十一会话/F5_NetBattle夹具.md` §7·1。<br>✅ **2026-10-10（`G2` 已做完）**：**实现形状 = `Fail` 【入队】一条话 → `Pump()`（主线程唯一出口）出队打印** —— ⛔ **不能在 `Fail` 里直接 `Debug.Log`**：`Fail` 的调用点**有一半在读线程上**（6 处 `ReadLoop` 里 + 1 处 `Send`），而 `NetTransport.cs` 文件头那条规矩是「后台线程只碰 socket 与并发队列、**绝不碰 Unity API**」；形状照抄已有的 `_inbox`，出队循环放在 `Pump` 的 `into == null` 早退**之前**。🔴 **一处口径取舍（请转告用户）**：**没有**把原因拼进**玩家界面** —— 判据 = 玩家那侧原是**固定词条**（`Battle/HUD/WaitOpponentConnectionMsg`，13 个战场各一份）⇒ 属**铁律 11 第一类「原版本身就没有」**。`_lastError` 语义**一字未动**。⚠️ **`G2` 顺带订正一处判据（结论对、判据错 —— 主对话也抄进过正本，已一并改）**：`F5` 说「`_lastError` **全仓唯一消费点**是 `NetSession.Send`」——**实际不止**：现核 `StartHost:132` / `InternalConnect:185` / 重连失败 `:320` **也读它**；**结论不变**（掉线那条路上它确实没人看）。⚠️ **没查清**：那行「生产侧会不会真打出来」有半个前提**没验**（「会话刚 `Close`（`Off`）之后那条迟到的话还打不打」只能在**真 Play** 看）。 |
| 🆕 **`A1268`** | 🔴 **`TcpTransport.Setup` 【不停旧读线程】⇒ 旧读线程迟到的 `Fail` 会把【活着的新连接】标成掉线**（`F5` 顺手查出，⛔ 没改）—— ⚠️ `F5` 说**本次只能排除「落在两次 `Send` 之前」**，`1–5 ms` 那个窗口**排除不掉**；但它**解释不了丢帧** ⇒ **应独立立账、不并进 `NetBattleTest` 那三条红**。⇒ **要做**（铁律 11）。出处 → 同上 §7·2。 |
| 🆕 **`A1269`** | ⚠️ **`NetSession.Pump()` 的掉线检测闸【不挡 `Off`】**（`F5` 顺手查出，⛔ 没改）—— `Net/NetSession.cs:205` **只挡 `Closed` / `WaitingReconnect`、不挡 `Off`** ⇒ `Close()` 之后**再被 `Pump` 一次**会**再报一次 `OnPeerLost`**、状态从 `Off` **倒回 `WaitingReconnect`**。⚠️ 这正是 `F5` **不把客机也推起来**的理由（推了会**假红**：`n9c.Length` 从 1 变 2）。⇒ **要做**（铁律 11）。出处 → 同上 §7·3 ＋ §2·②。 |
| 🆕 **`A1271`** | 🔴 **骷髅卡奖励格【也错了】，`F3` 没动**（它明说「都是**要做**、不是不做」）—— 我们传的 `rw = 109.25 × 47.433` 设计（`footer` `325 × 75.84`），而**页内实例与独立 prefab 两处的 `Rewards` 都是 `(325, 77.643)`、两格各 `162.5 × 77.643`** ⇒ **整条不对**（且走的还是「**竖向**」档）。⚠️ **停手原因**：牵到骷髅卡 `footer`（原版 `325 × 181.86`）+ `counter` / `TimerHolder` **整条链** ⇒ **是白名单外的另一站、要单独量**。🔴 **另**：**骷髅卡 `count 1` 没有任何断言覆盖**（**静默站**）。⇒ **要做**（铁律 11）。出处 → `资料/普查产出_第十一会话/F3_MissionsTab框.md` §6·1。<br>✅ **2026-10-10（`G4` 已做完，**而且把本账的【判据源】改了判**）**：**这不是「改一格」，是「换预制体那一份」** —— `F3` 量的数**在【页内实例】上全对**，但它**只量到「格」、没走完「卡」**。本页画的是**页内那一份全尺寸的 `Daily Skulls Mission Container`（`347.64 × 555` 设计）**；我们此前建模的是**独立预制体 `Daily Skulls Mission Container Small`（`336 × 277.5`）**，而后者**在这个位置根本画不出来**：`Special Missions` 的 HLG `ctrlW/H = 1` ⇒ 交叉轴 `requiredSpace = Clamp(innerSize 556.223, minH 555, prefH 555) = 555`（所有任务卡的 `LayoutElement` 都是 `minW 347.64 · minH 555`）⇒ **它自己写的 277.5 永远被覆盖**。**四条互相独立**的证据：① 上面那条 HLG Clamp（uGUI 源码在工程 `Library/PackageCache` 里逐句可查）② `menu_dump.py bundle_menus_assets_all "Missions Tab"` 实算页内那份 = **`347.64 × 555`**（与①逐位同）③ `UseSmallContainer` **只挂在 `MissionEvent` 上**，而 `FlexibleLayoutSizeOption` **全库唯一消费者 = `FlexibleGridLayout`**（存的是**网格格位**、不是像素）⇒「Small」属**网格页**，**不是** `Special Missions` 那一格 ④ 🔑 **用户那张原版实拍**：卡的高宽比 ≈ **`1.615`** ↔ `555/347.64 = 1.596`（差 **1.2%**）vs `277.5/336 = 1.211`（差 **33%**）；且卡里有**大骷髅立绘 + `x0` + 5 格里程碑 + 奖励格 + 收集钮 + 一行时间**、**时钟行前面【没有】小时钟图标** —— 与全尺寸那一份**逐项吻合**。**落地**：卡框 → `347.64 × 555` · 新增 `body` 层 · `footer` → `(325, 181.86)` · `Rewards` → `(325, 77.643)` · **删掉只属 Small 的 `footer/counter/icons` 整条** · `Timer` 改走 `Txt`（`fs 38`、**不再画时钟图标**）· **新增 ≈17 条断言**（含那个**静默站 `count 1`** 的 5 条 + 两条**结构性关系式**）· 改写 5 处既有断言。⚠️ **就地订正 `F3` 两条**：① 「两格各 `162.5`」是 **prefab 作者预览** —— **格数是数据驱动的**（`MissionRewardsDisplay` 先 `DestroyAllChildren` 再按 `AvailableRewards()` 逐格 `Instantiate`），我们这份奖励**只有 1 份** ⇒ **1 格吃满 325**（实拍上奖励块**居中**）② `F3` 说的「独立 prefab」与我们抄的那一份**不是同一份**（`Daily Skulls Mission Container` 是 `325×77.643`，`… Small` 是 `109.25×47.433`）。⇒ 余下的四条已另立 `A1289`–`A1292` + `A1288`。出处 → `资料/普查产出_第十一会话/G4_骷髅卡奖励格.md`。 |
| 🆕 **`A1287`** | ⚠️ **`Editor/MainMenuScene.cs` 两笔：① 注释【取错了节点】② 一条验收要收紧**（`H1` 报，⛔ **不在它白名单、只报不改**）—— **① 就地订正（铁律 5）**：`:2845-2852` 写「页签 = `260`（`Generic Tab UI Button` 那一格 …… `Button Text` 是它的子件）」⇒ **拿父件的宽当子件的框**（**真值 `213`**）；⚠️ **现状不会红**（那是**上界**守卫 `mw <= boxW+1.5`，字变小照样 ≤ `261.5`），**但失去鉴别力**（回归到 `260` 也绿）。**② 收紧**：把那条的 `boxW` 从 `260f` 改成 `213f`，并**补三条**（框宽 `sizeDelta.x × 108 == 213`（±0.1，读写口同 `:1223` 那条先例）· `LabelRenderedPx(lb).x <= 213f + 1.5f` · **灭自证**：同时断**底板两颗仍是原版 `260 × 67.6421`** ⇒ 堵死「顺手把 `TabW` 也改 213」这条**假修法**）。**改坏法**：`DeckSelectionPopup.cs:606` 第 8 实参改回 `r.W` ⇒ 前两条都红。⇒ **要做**（铁律 11 + 5）。出处 → 同上「该断什么」A/B。 |
