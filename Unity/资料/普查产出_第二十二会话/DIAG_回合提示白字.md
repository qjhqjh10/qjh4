# DIAG 「第 N 回合 / 你的回合」那行白字

截图坐标：`d:/4/_tmp_view/s22_11_play.png`，白字横贯中部、约距顶 240 px（≈0.78 处）。

## 一、结论（3–5 行）

1. **那行白字是我们自己画的** —— `BattleDriver._turnLabel`（建 `Battle/BattleDriver.cs:12170`、文本每帧写 `:14186`），原版 HUD 里没有这一行。本仓自己早就写了这句：`BattleDriver.cs:12168`「⚠️ **这一行本身是我们自加的**（原版没有 `TurnLabel`）」。
2. 它由**两半**拼成：`第 N 回合` + `你的回合`。**`第 N 回合` 这半原版没有**（键 `Battle/Phrase/TurnLabel` 是我们自建、值也是自拟，`Core/Loc.cs:2268`）⇒ **属「自己设计」，按用户口径该去掉**。
3. **`你的回合` 这半原版有** —— 词条 `Battle/HUD/YourTurn`（代码字面量 `0x4288a48`），`BattleManager.<NextTurn>` 在**我方回合开始时**调 `BattleTipController.ShowHeadsUpMessage`。但原版是**短暂横幅**（1.5 s），不是常驻行。
4. ⇒ 处置方向：**删掉前段「第 N 回合」，并把后半句从「常驻行」改成原版的「短暂横幅」**（位置/尺寸/底图/字号都取得到真值，见 §三）。铁律 11：全部照做，只有先后。

## 二、我们侧

| 项 | 值 / 出处 |
|---|---|
| 对象 | `Label _turnLabel`（声明 `Battle/BattleDriver.cs:1006`；GO 名 `TurnLabel`），建在 `BattleDriver.cs:12170` |
| 位置 | `Hud(root,"",0.5f,0.810f,4, …, new Vector2(0.5f,1f))` ⇒ 锚 (0.5,1)、距顶 **205 px**、水平居中。第 4 参 `4` 是 `Label` **缩放档**（签名 `:13973`），不是字号 |
| 文案 | `BattleDriver.cs:14186` = `CardText.TurnLabel(Ctx.Turn) + "   " + CardText.Phrase(who)`；`who` 定在 `:14180-14181`（`GAME OVER` / `YOUR TURN` / `ENEMY TURN`） |
| 前段来源 | `Core/CardText.cs:576-579` → 键 `Battle/Phrase/TurnLabel`（`Core/Loc.cs:2268`：ZH `第 {0} 回合` / EN `TURN {0}`，**两列都自拟、键也是自建**） |
| 后段来源 | `Core/CardText.cs:557-563` + `PhraseTerms:498-500` → `Battle/HUD/YourTurn` / `Battle/HUD/EnemyTurn` |
| 显隐 | `BattleDriver.cs:14184-14185`：`showTurn = !InMulligan` ⇒ **整个回合常驻**（不是短暂提示） |
| 中英两档 | 中文档 `第 3 回合   你的回合`；英文档 `TURN 3   YOUR TURN`（`G2b` 2026-10-18 刚修过「英文档仍印中文」那处 `CardText.cs:566-574`） |
| 注释出处 | 位置那条注释自己写着「我们自加的」`BattleDriver.cs:12165-12169`；换牌期不显示 `:14182-14183` |
| ⛔ 别混 | 屏幕**正中**（0.507）那行更淡的中文是**另一件** `_hintLabel`（`BattleDriver.cs:12598`，`SetHint` 那套），与本条无关 |

## 三、原版侧

**触发（第一权威 = 反编译方法体）** `d:/2/tools/decomp_full/BattleManager._NextTurn_d__395__MoveNext.c`
- `:358` `if (*(char *)(param_1 + 0x28) == '\0') { …敌方回合那一大块… }`，`:444 else { …}`，其中 `0x28` = `isPlayer`（同文件 `:355-357` 用它给两侧 `DeckManager.SetYourTurn`）⇒ **只有我方回合进 else**。
- `:474` `SoundManager.Play2D(SoundAssetCollection.get_StartTurn)`；`:479-481` `LocalizationManager.GetTranslation("Battle/HUD/YourTurn")`；`:483` `BattleTipController.ShowHeadsUpMessage(tip, text, opaqueBackground: **1**, bgColorRed: 0)`；`:485` `yield return new WaitForSeconds(**1.5**)`。
- `:462-470` **教程局且第 1 回合 ⇒ 整段跳过**（日志串 `Skip display turn in first tutorial turn`，`d:/2/tools/all_strings.txt:4574`）。
- 常量实读（`pefile` 读 `d:/2/unity_run_ref/GameAssembly.dll`，ImageBase 0x180000000）：`0x1834b3090` = **1.5**；交叉验证 `0x1834b3180` = **0.6**、`0x1834b2bb8` = **1.0** —— 后两个正是 `tipsBackground` 的 60% / 不透明两档 alpha，与下面场景里的 `0.6` 吻合 ⇒ 映射可信。
  ⇒ **回合开始那句 heads-up 走的是「不透明」档**（第 3 实参传 1），画面上的绿条是**满不透明**，不是场景里序列化的 0.6。

**载体与外形**（场景 JSON + 2D 全树 + 逐节点读报）
- 类 `BattleTipController`：字段 `startText`（TMP）/ `tipsGameObject` / `tipsBackground`（Image）/ `voiceLinesController`（stub `d:/2/Warpforge_code/Scripts/Assembly-CSharp/BattleTipController.cs`）；实例 = `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4760.json`（`startText` pid **3813** / `tipsGameObject` **692** / `tipsBackground` **5136**）。
- 节点：`Safe area FrontCanvas / AboveShader / TipText`，**出厂 `activeSelf = False`**（`d:/4/Unity/资料/说明书/01_战斗_对战/2D层_battlearena2全树.md:1041-1044`；arena1 那份全树这一支没导全）。
- 位置尺寸（raw RT 实读 = `d:/4/Unity/资料/战斗规格/战斗重建_0827/子代理读报_front交互层_0827.md:198-201`）：
  - `TipText` GO 692 / RT 2672：anchor **(0.5,0.5)**、pos **(1.1, 91.8)**、size **(2450.1 × 99.7)**；底图 sprite `40k_bt_underbutton`、color(0.3686, 0.8941, 0.5874, **0.6**)
  - `└ Text` GO 417 / RT 2937 / **MB 3813**：anchor (0.282,0)-(0.719,1)；TMP **`fs = 60`**（base 36、auto 4–60）、**白色**、`H=2`(Center)、`V=512`(Middle)、font `3485036404935369831`（CaslonAntique SDF）
  - 绝对矩形 **x[427.6, 1497.0]（水平中心 962.3 = 屏心）· y[398.4, 498.1]（距顶；中心 448.25 = 屏心【上】91.8 px）**
- 停留时长：`BattleTipController._DisplayTip_d__14__MoveNext.c` 用 `GlobalVars` 的一个 float（`+0x1c`）当等待，**隐藏是瞬时 `SetActive(false)`、没有淡出**。
- 近亲 `YourTurnUI`（stub：`bg` Image + `text` TMP + `timeToFadeStep`/`timeToWaitStep`，带 DOFade 淡入淡出）——**全库找不到任何调用点**（`decomp_full` 里 `DisplaySign` 只出现在它自己那 3 个文件）⇒ 判为**未被使用的旧件**。

**「回合 N」到底有没有 —— 搜过哪些包与词（零命中）**
- `assets_full` 全树 `grep '"Battle/HUD/*"'` ⇒ 序列化里**只有 5 条**（`AffectedBy` / `WaitOpponentConnectionMsg` / `PleaseWaitConnection` / `MatchAlreadyFinished` / `LostConnectionMsg`）；`EndTurn`/`YourTurn`/`EnemyTurn` 都只在**代码字面量**里。
- `bundle_scenes_scenes_battlearena1` 整包 `grep '"Battle/…"'` 全量去重 ⇒ **唯一带 `Turn` 的是 `Battle/Mulligan/secondTurn`**。
- 全库 strings（`d:/2/tools/all_strings.txt`）与官方中文表（`d:/4/Unity/数据/本地化/i18n/zh_CN.csv`）里唯一的 `Turn %d` 是 `zh_CN.csv:163` 的 `Skulls %d : %d    Turn %d    Warlord HP %d : %d`（开发/统计串，不是 HUD 件）。`BattleHud` 的字段表里只有 `energyAndTurnHolder`（内部是 `Clock/TurnBtn/TurnText = 'END TURN'`）与 `SetTurnPlayedCards`（`CardsPlayedInTurn1..3` 小图标）—— **没有任何显示回合数字的件**。
- ⚠️ `Battle/HUD/EnemyTurn` 在原版是**「结束回合」那颗钮上的字**（`ClockManager.SetEndTurnText`，键 `0x4288768`），**不是**回合横幅 ⇒ 我们把它当「回合行后半句」用是**第二次挪用**。

## 四、改动清单（逐条）

| # | 文件:行号 | 改什么 | 风险 / 会不会动断言 |
|---|---|---|---|
| 1 | `Battle/BattleDriver.cs:14186` | **删前段**：只写归属那半（或整个改成 §2 的短暂横幅） | 会动断言：`Editor/BattleScene.cs:3633-3634` 断**英文档前段 `StartsWith("TURN ")`** ⇒ 必红，要同批改。`:3619-3646` 断 `EndsWith("你的回合"/"ENEMY TURN")` 那条**不受影响** |
| 2 | 新增「回合横幅」件（照原版名 `TipText`）+ `BattleDriver.cs:12170`/`14186` | 位置 `(0.5,0.5)+(1.1, +91.8 px)`、size **2450.1×99.7**、底图 `40k_bt_underbutton`、**alpha = 1.0**（opaque 档）、文本 = `Loc.T("Battle/HUD/YourTurn")`、TMP **fs 60**、白、H/V 居中；显示 **1.5 s** 后隐藏 | 低（新件）。断言要配两条：① **不是我的回合 ⇒ `activeSelf == False`** ② **换牌阶段 ⇒ `False`**（先例 `Editor/BattleScene.cs:13576`；⛔ 判「没显示」必须断 `activeSelf`，`FindChild` 会含 inactive） |
| 3 | `Battle/BattleDriver.cs:14193` 一带 | 原版 heads-up 之后还有 `StartTurn` 音效（`:474`）—— 我们那条有没有接，属同族、一并核 | 低 |
| 4 | `Core/Loc.cs:2268`（`Battle/Phrase/TurnLabel`） | 删前段后**这条键没有消费点** ⇒ 一并删（⛔ 别留死条目，同族先例 = `CardText.cs:517-519` 那 4 条） | 低；`Editor/CardBaseDemo.cs:1323,1351` 两条断言依赖 `TurnLabel(3)` ⇒ 会红，要同批改 |
| 5 | ⛔ 不动 | `_hintLabel`（`BattleDriver.cs:12598`，屏幕正中 0.507）是**另一件**，用户在等它摆哪（`项目任务.md` §29·c），**别顺手改** | — |

## 五、没查清的部分（⛔ 不许猜）

1. `BattleTipController._DisplayTip` 读的 `GlobalVars + 0x1c` 到底是 `tipDuration` 还是 `waitForHumanMatchMaking`（`VarsGlobal` stub 里这两个 float 同在那一带，**没逐字段核偏移**）⇒ **只影响「横幅停留多久」的复刻值**，不影响方向。
2. `VarsGlobal` desktop 资产（`bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4371.json` 的 `desktopVariables`，pid −3698593662854848861）里那两个 float 的**实际值没读**。
3. `YourTurnUI`（带淡入淡出的那个）**本地找不到任何调用点、也没找到它的 prefab** ⇒ 「原版是否在别处用它」**判不了**。
4. 我方回合**结束**时原版屏幕上还有没有另一句 heads-up（如 `ENEMY TURN`）—— 本地**没找到**（`Battle/HUD/EnemyTurn` 只被那颗钮读）。
5. 那句 heads-up 的**淡入淡出**：`DisplayTip` 看不出补间（疑似瞬时显隐），但 `VarsGlobal` 那一带还有 `timeToScaleIntoMoving` 之类 ⇒ **没钉死**；没跑到实况（本轮纯静态）。
