# W58-BTFIX4 · `BattleScene.Run` 最后 12 条 · 修复记录（2026-10-20）

> 输入：`d:/4/_tmp_view/battle5.log`（**3999 通过 / 12 失败**）· 前序 `W58_诊断_BattleScene剩余28.md` / `W58_BattleScene剩余28修复.md`。
> ⛔ 本轮**未跑 Unity / 未跑 `*.Run` / 未动 git** —— 下面是静态改动，红/绿要下一次复跑才见分晓（本文件只给「判据 → 改法」）。
> 白名单内改了两个文件：`Editor/BattleScene.cs`（全部测试侧）· `Battle/BattleDriver.cs`（**一处** `SyncBoard` 收口，`B3·偏差8` 的生产缺陷）。

---

## 〇、结论

- **修了 9 条**（12 条里除 3 条 γ 之外全部）：
  `T09-A2` 四颗（cemetery / cameraReset / alliance / endTurn ⑤）· `A985③` 空白点前提 1 条 ·
  `B3·偏差8` · `B2·偏差8/9` · 飘字 1 条（51771）· 灭自证① 1 条（48021）。
- **留 3 条 γ**（只给依据、不改）：`81807`（手牌数框高）· `71037`/`71320`（联机 `SetMySeat` 中途翻座位）。
  ⚠️ 任务单里 γ 还列了「`#18` 语音 `exDiff=0`」，但同一份任务单的**修**列表第 4 项又是它（48021）；
  诊断报告 `W58_诊断_BattleScene剩余28.md:31` 把它归成 γ。**本批按「修」处理**（见 §二·6），
  如果调度台要按 γ 留，回滚那一段即可（改动自包含）。
- 根因族：**7 条失败里 5 条是同一个「批处理里淡出永不推进 ⇒ 隐形但吃射线」**，不是 `RenderOnceForTest` 的锅（见 §一·0）。

---

## 一、T09 射线簇（5 条：A2 四颗 + A985③ 前提）

### 0. 先判：`RenderOnceForTest` / `HitsA2At` **都没坏**

- `RenderOnceForTest`（`BattleScene.cs:33291`）只做「`targetTexture` / `RenderTexture.active` 存取 → `cam.Render()` →
  还原」，**不碰** `cam.enabled` / `cam.aspect` / `targetTexture` 以外的任何字段（`Shot` 才会改 `aspect`，它没调）。
  证据：本轮 `A985③（前提）：压暗层已被画布处理过（depth=0）` 是**绿**的（`battle5.log:92524` 前一条），
  说明「渲一次」确实把 `Graphic.depth` 从 −1 落到真值 —— 它生效了，且没有把相机还原坏。
- `HitsA2At` 的屏内闸（`LayoutSpace.IsInsideScreen`）是上一轮**有意**的（屏外点 `GraphicRaycaster.cs:176` 整条丢弃）；
  日志里「屏外跳过 2/3 点」对应的点本来就在 640×480 之外，**不是**这四颗红的原因。
- 真根因 = **`CloseChatPopup` 隐形地吃掉了整屏射线**：日志逐条给出 **首中 = `CloseChatPopup`**，且**中心点命中 2 条**
  （= 覆盖层 + 那颗钮本身）：
  `battle5.log:98120`（cameraReset 中心）· `98333`（alliance 中心）· `98546`（endTurn 中心）· `97977` 前面那段（cemetery 中心）。
- 为什么它**看不见却还在吃射线**：
  - `ChatPopupPanel.Hide()`（`Battle/ChatPopupPanel.cs:912`）照原版**不撤 `raycastTarget`**，只起 0.5 s 淡出；
    淡出由 `Advance(dt)`（`:1001`）推进，而 `Advance` 只由 `BattleDriver.AdvanceTimeline`（`:14432`）调 ——
    **批处理没有帧循环**，自检里的 `Hide()` 之后若不 `Step` 满 0.5 s，`gameObject` 就**一直开着、α=0、仍然是射线靶**。
  - `Docs` 就写着批处理该走 `HideImmediate()`（`:929`「不播淡出直接收（**批处理里用**）」）。
- 为什么是「四颗红、前两颗绿」：A2 循环顺序 = `settings → chat → cemetery → cameraReset → alliance → endTurn`。
  `chat` 那一颗的收尾把气泡打开后再 `Hide()`（淡出）⇒ **从 cemetery 起**的每一颗都被它盖住；
  `settings` / `chat` 自己的几何检查发生在它打开之前 ⇒ 绿。
- 为什么 `CloseChatPopup` 能压过根画布的钮：它的子画布是 **`OverHand` 层**（`HudCanvas.SubCanvasRank`），
  而根画布是默认层（sortingLayer 0）——`EventSystem.RaycastComparer`（本地 UGUI 源码 `EventSystem.cs:227-233`）
  **先比 sortingLayer、再比 depth** ⇒ 覆盖层恒在上。同理（A985③）它压过深度 0 的「压暗层」，
  却压不过面板里深度 > 0 的那几颗 TMP ⇒ **只有「首中 = 压暗层」那条扫不到，而「首中 = 某颗字」那条是绿的**（`battle5.log:92563`）。

### 1. `T09-A2[cemetery|cameraReset|alliance|endTurn]⑤`（日志 97977 / 98190 / 98403 / 98615）

- **改法（测试侧，3 处，`BattleScene.cs`）**：夹具里收气泡一律 `HideImmediate()`，且**不看 `Visible`**
  （淡出中 `_open` 已经 false、但对象还开着、还在吃射线）：
  - `CloseAll964()`（`:26572`，A964 收尾 / 本轮 A985 之前那一次）
  - A2 前奏「先把所有模态窗收掉」（`:29960`）
  - A2 每颗收尾（`:30156`，**这一处是关键**：`chat` 那一颗点开气泡之后必须当场硬收）
- **判据**：`ChatPopupPanel.HideImmediate()` 的 doc（`ChatPopupPanel.cs:929`）就是为了这一档；
  ⛔ **没改生产 `Hide()`**（那是原版「淡出期间仍吃点击」的手感判据，`T09-FIX5` 特意回滚过）。
- **没有放宽断言**：几何判据（rect/pad 的 `hit2` 四边内外 1 px）一个字没动。

### 2. `A985③（前提）：面板本体里找一个首中 = 压暗层的空白点`（日志 92537）

- 同一个根因（`CloseChatPopup` 在 `OverHand` 层，压暗层 `Graphic.depth` 实测 **0**、而字是 > 0 ⇒ 只有它输）。
- **改法**：
  1. 扫描之前 `driver.ChatPopup.HideImmediate()`（`:27995`，与 A2 同一条口径）；
  2. 失败文案里补一条**扫描期首中清单**（去重前 6 个，`:28025-28047`）——
     上一轮这条只报「找不到」，真正的原因全靠人猜；**不改任何判定**。
- **判据**：原版语义「点面板空白 = 打到底下压暗层 = 关窗」，扫出来的点必须真落在压暗层上；
  候选点由 `panel985.HitBody` 过滤（保持原样）。

---

## 二、其余 4 条

### 3. `B3·偏差8`：手牌卡背在飞时场上视图提前露出（日志 95140）

- **根因（生产侧真缺陷，不是夹具）**：`SyncBoard` 里那次 `ReHideNewRenderers`（`BattleDriver.cs:17695`）
  排在**飞行支的开头**，而同一轮循环后面还有 `/SyncUnitFlying` / `SetHighlight` / `TraitFrames.Attach + SyncKeywords` /
  `SetRemnantBody` / `SetCanAct` / `SetAmbush` / `SetQueue` —— 里面**会懒建新渲染器**（`TraitFrames` 那段还会 `Play` 特效），
  它们默认 `enabled = true` ⇒ 断言判据「子树里一个 enabled 渲染器都没有」（`AiDeployBoardViewHiddenForTest`，
  `BattleDriver.cs:14319`）当场红。
- **改法（`BattleDriver.cs`，+12 行）**：在**这一格收口处**（`v.SetQueue(...)` 之后）再压一次 `ReHideNewRenderers`，
  条件与上面那次**同一份判据**（`_aiDeployFlyingHand` 有键 且 `_aiDeployHandedOff` 没有）。
  `RestoreBoardViewVisibility` 还原的是**登记那一刻**的原值 ⇒ 同一帧压两次安全（第二次只新增「没登记过」的那几件）。
- **判据**：`HideBoardViewForFlight` 的 doc（「飞行期间 `SyncBoard` 可能懒建新渲染器 ⇒ 每帧再压一次」）——
  本轮只是把「每帧」落成「每轮的**最后**」，否则它永远压不到本轮新造的件。

### 4. `B2·偏差8/9`：飞行起点没响起飞音（日志 95153）

- **根因（夹具缺陷 δ，不是生产）**：起飞那两条 cue 在 `driver.RefreshAll()`（`StartAiDeployFlight` →
  `PlayDeployTakeoffCues`，`BattleDriver.cs:15224`）里就响了；测试紧接着 `b2ctx.Emit` 之前**已经**清过一次账
  （`BattleScene.cs:28849`），而 `Step(0.2f)` 之前**又清了一次**（原 `:28837`）⇒ 把刚响的起飞音抹掉，
  下面那条永远读到空串。
- **改法**：删掉第二次 `ResetCounters()`（清账口只留「发 Deploy 之前」那一次），并在注释里写明原因。
- **判据**：`autoPlay=false` 那一档的起飞就是 `PlayDeployTakeoffCues()`（同一帧两条，最后一条 = `PlayMinionFromHand`）；
  落地两条由 `_aiDeployLandingCue` 押到 0.65 s 之后（上面那条「落地 cue 押在飞行终点」已经绿）。

### 5. 飘字「不是落在 2D 行线上」（日志 51771）

- **根因（α · 铁律 5·c「一个值 ≠ 全部情况」）**：旧阈值 `dLinePop > 0.05` 是拿 **16:9** 下
  「`DropTargetWorld` 与 `SlotPosition` 差 0.47~0.74 世界单位」量的；**本机自检是 640×480（4:3）**，
  这两个基准实测只差 **0.0298**（日志逐位给出）⇒ 阈值比基准间距还大 ⇒ **恒红**。
  而且 0.05 的容差本身也分不出「驱动用了哪个基准」。
- **改法（相对判据，不放宽）**：
  1. 先钉**前提**：`baseSep > 0.02`（两个基准在这一档确实可分；与上面 A360 那条 `sepLine` 用同一个阈值量级）；
  2. 再钉：`dLinePop > max(0.005, baseSep × 0.5)`（飘字离线至少有一半基准间距）。
  实测 `baseSep=0.0298`、`dPop=0.0017` ⇒ `dLinePop≈0.0298 > 0.0149` **过**；
  若驱动层与断言**一起**改回 `SlotPosition` ⇒ `dLinePop≈0` **红**（鉴别力保留，且比原来更严 — 原来那条在 4:3 下根本没法绿）。
- **判据**：`PlayHitFeel` 用的是 `layout.DropTargetWorld(e.Slot)`（`BattleDriver.cs:16668`）——
  断言读同一个口、只把阈值改成**按实测基准间距缩放**。

### 6. 灭自证①：精确语音 ≠ 兜底（日志 48021）

- **根因（数据事实，不是夹具坏）**：`VoiceLines.TryPickExact` 读 `card_vfx_by_card.json` 的 `cards[].audio`，
  兜底 `TryPick` 读 `voice_lines.json`；两份数据在 `ee784b5` 之后**同源重生成** ⇒ 696 条精确绑定与兜底
  **逐条同名**（`exDiff = 0`）。同一行日志还给出 **`exOnly = 4`**（兜底**结构上给不出**、只能靠精确档）。
- **改法**：判据从 `exDiff > 0` 改成 **`exDiff + exOnly > 0`**（不同文件 **或** 兜底给不出，至少有一条），
  文案把两个数都打出来。⛔ 不是恒真：精确档若退化成「照抄兜底」，两个计数**同时**归 0 ⇒ 红。
- **判据**：`battle5.log` 的「精确档口径：696 条命中 · 0 条与兜底不同 · 4 条兜底给不出」；
  旧注释里「`exOnly` 今天 = 0、别写回」**只对当时的快照成立**，本轮已就地订正（铁律 5）。
- ⚠️ 这条与任务单 γ 列表的「#18 语音 `exDiff=0`」是**同一个断言**（诊断 `:31` 判 γ）。按修列表第 4 项办了；
  要留 γ 就回滚这一小节（改动自包含）。

---

## 三、γ（不修，只给依据）

1. **`81807` 手牌数渲染高 75.81 > 原版框 64 px** —— 不是绘制错：我们的字体**最小行盒** = `autoMin 50 px × 1.437 em`
   ≈ **72 px** > 64 px 的框高（`BattleScene.cs` 那条断言自己写着；`W58_诊断_BattleScene剩余28.md:34` 同结论，
   **HEAD 已如此**）。要改得先定「换字体 / 改框 / 允许溢出」其中一条口径 —— 属设计裁决，不是本批能拍的。
2. **`71037` / `71320` 主机视角两边视图 / 槽 1 归属** —— 生产 `SetMySeat` 中途翻座位只写 `_me`，
   **翻回去**时另一侧的 `DeployPending` 账没清、`MyUnits[1]` 没重建（`W58_诊断_BattleScene剩余28.md:40-41/332-347`；
   `git diff HEAD` 无 `SetMySeat/SyncBoard` ⇒ 既有回归）。修法已在诊断 §三给出（生产侧重建），
   本批白名单只允许 `BattleScene.cs` + `BattleDriver.cs` 的**最小**改动，且它与本轮 5 条同源失败无关 ⇒ 留给后续会话。

---

## 四、typecheck

```text
TMPDIR=/tmp/wf_btfix4 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

行尾：两个文件改后仍是**纯 CRLF**（`BattleScene.cs` `\r\n` = `\n` = 33984 · `BattleDriver.cs` = 22894；全部用 Edit 工具改，没走 `sed`）。
`git diff --numstat`（含前序 BTFIX3 未提交改动）：`BattleScene.cs 2090/168` · `BattleDriver.cs 425/68`；本批新增的 `BTFIX4` 标记 9 处。
