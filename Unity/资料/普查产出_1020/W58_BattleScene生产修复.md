# W58 · `BattleScene` 生产修复（4 件）

> 执行代理 `W58-BTFIX-PROD`。⛔ 未跑 Unity / `*.Run` / player / git 写操作；只改生产代码（3 个白名单文件）。
> 输入判据：`资料/普查产出_1020/W58_诊断_BattleScene50条.md` §P0。
> 行尾核对（改前 / 改后）：`CameraIntro.cs` **LF** · `UnitChatPanel.cs` **LF** · `BattleDriver.cs` **CRLF** —— 均未翻。

---

## 结论

4 件全部落地，`typecheck` 0/0 绿。

| # | 件 | 文件 | 结果 |
| --- | --- | --- | --- |
| 1 | T12 开场运镜把批处理相机留在 t=0 | `Battle/CameraIntro.cs` | ✅ 批处理档起播**不写位姿**（相机留在本位），真机零差异 |
| 2 | T09-C3 气泡宽度 | `Battle/UnitChatPanel.cs` | ✅ `MakeBubble` 建件处补三条 `SetAspect`；`RefreshLayout` 早退**保留** |
| 3 | `OpenCommand` 裸下标崩溃 | `Battle/BattleDriver.cs` | ✅ `TryGetValue` + 空判 + 出声 + `return` |
| 4 | `TickIntroMonologue` 顺序 + 跨局气泡 | `BattleDriver.cs` / `UnitChatPanel.cs` | ✅ 收尾判定移到「气泡还在 ⇒ return」之后；`Begin` 清气泡并停语音 |

⛔ 未碰 `Editor/BattleScene.cs`、`RuleEngine/**`、正本。

---

## T12 批处理（`Battle/CameraIntro.cs:299`）

**判据/修法选择**：诊断给了两条，**两条都不直接可用**：

- **（诊断的）方案 A「批处理不自动进运镜」**：`StartCameraIntro` 直接 return。
  ⚠️ **它会弄红 4 条现在绿的 `T12` 断言** —— `Editor/BattleScene.cs:17981-18027` 那一组的
  `★ T12 接线 / 黑场同帧（两条）/ 重入` 靠的就是 **`Begin` 自动起播**：
  `drv.Begin(...)` 之后立刻读 `drv.CameraIntroForTest.IsPlaying`。停掉自动起播 ⇒ `null` ⇒ 新增 4 红
  （修 4 条又添 4 条，且测试侧不在本代理白名单里）。
- **（诊断的）方案 B「`Begin` 建场后立即 `Stop(resetHome:true)`」**：同理，`Begin` 后 `IsPlaying == false`
  ⇒ 同样打掉上面那 4 条。

**采用（第三档，语义等价 + 零新红）**：`CameraIntro.Play` 末尾

```csharp
if (!Application.isBatchMode) Sample(0f);
```

- **为什么对**：Unity 的 `Animation.Play()` 本身**不是同步落位** —— 要到下一次动画更新才求值；
  批处理的「下一次更新」= 自检显式 `Step` → `AdvanceTimeline` → `CameraIntro.Tick(dt)`（`BattleDriver.cs:14334`）。
  所以起播这一下**只置 `_t/_playing`**，位姿交给 `Tick` 写；没有帧循环就**没人写** ⇒ 相机停在本位
  `(0, 2.222, −13.572)`，而不是 clip 起点。
- **为什么能修那 4 条红**：本批红的三处（`4075/4089` 数值探针屏外 · `46978` 落点往返 0/18 ·
  `47041` 敌方行 −32.5%）都发生在**没有任何 `Step` 之前**（日志实测相机逐位 = clip t=0 ⇒ 一次 `Tick` 都没跑）。
  改后同一时刻相机在本位 ⇒ 与 HEAD 的量测前提一致。
- **为什么真机零差异**：非批处理档仍然 `Sample(0)`（`Update` 下一帧就继续 `Tick`）。
- **T12 自检逐条复核**（不改测试，只推演）：
  · `接线`：`IsPlaying && Elapsed < 1e-4` —— 起播状态照旧 ✅（位姿没写不影响）；
  · `黑场同帧`：`Step(0.625/1.0)` → `Tick` 正常推进 ✅；
  · `播完回本位`：`Tick(min(_t,len))` 采到终帧 = 本位 ✅；
  · `重入前提`：`Begin`（`Stop(resetHome:true)` 回本位、`Play` 不起样）+ `Step(0.625)` ⇒ 相机在 25% 行程处，
    离本位远 > 1 ✅（`battlearena1` 起点距本位 ≈14.8）；
  · 13 场位姿 / 灭自证 / 光晕那三组用**自建相机 + 显式 `Play` + `Pose*ForTest`（内部显式 `Sample`）** ⇒ 不受影响 ✅。

**顺带核 `FindBoardCamera`**：`Editor/BattleScene.cs:32395` 只取 `c.depth < 0` 的**第一台**；
`BoardCamera` 的 `depth = -1`（`Editor/BattleScene.cs:32374`），而 T12 的三台临时相机
（`:17941/18050/18076`）、A422/A463 rig 相机（`:22705/30910`）、HUD 相机（`:32410`）**都没设 `depth`**
（默认 0）⇒ **不会被抢走**；且 `:768` 还有一条 `driver.boardCam == FindBoardCamera()` 的断言盯着。
T12 每块结束都 `DestroyImmediate` 了临时相机（`:17978/18062-18063` 等），也不需要补。

---

## C3 气泡宽度（`Battle/UnitChatPanel.cs:532-534`）

`MakeBubble` 建完三片后补一次（与 `Place()` 同源常量、幂等）：

```csharp
if (b.bg != null) b.bg.SetAspect(BubbleW / BubbleH);
if (b.portrait != null) b.portrait.SetAspect(PortraitW / PortraitH);
if (b.wave != null) b.wave.SetAspect(WaveW / WaveH);
```

- **根因**：`RefreshLayout()` 画布支早退（`:719`），而唯一那三句 `SetAspect` 在 `Place()`（`:742-747`）
  ⇒ uGUI 档宽度停在建件时的**贴图比例**：`236.50 × 2.7357 = 646.996` vs 原版 `648.774`。
- 改后：`236.50 × (648.77/236.50) = 648.77`、高 `236.50` ⇒ `C3③` 的 `±0.02` 判据过
  （差 0.004 / 0.005）；`C3①/②/④` 动的是锚点/`anchoredPosition`，不受影响。
- 顺带修正了同族的两片：**portrait**（原贴图比例 vs `144.46`）与 **wave**（`708/96` vs `387.95`）——
  两者在 `Place` 里本来就是这么写的，本改动只是让「建完即可读」这一档与 `Place` 对齐。
- ⛔ `RefreshLayout` 的 early-return **保留**：`C3⑤/⑥`（画布档 `RefreshLayout` 幂等空转 / 不写几何）要它成立。

---

## OpenCommand 健壮性（`BattleDriver.cs:10262-10272`）

```csharp
CardView openView;
if (!_myUnits.TryGetValue(slot, out openView) || openView == null)
{
    Debug.LogWarning($"[Battle] `OpenCommand({slot})`：这一格**还没有视图** ...");
    return;                       // 选择态不变
}
_selectedSlot = slot;
openView.SetHighlight(...);
```

- 视图不在是**设计内**状态：`Deploy` 排在 `max(_clock, _timelineTailAt)`（队尾），
  `SyncBoard` 被 `IsPendingDeploy` 闸着不建件。
- ⛔ 不抛、⛔ 不静默：出声 + 直接返回；**不写 `_selectedSlot`** —— 否则下一次点击会被 ④ 支当成「选目标」。
- 同族裸下标顺带核完：只剩 `:9587`（建视图时的赋值，正常）与 `:12314`（`ContainsKey` 之后取值，已安全）。
- ⚠️ **测试侧仍会红一条**（夹具）：`SimulateOpenCommand` 现在**返回 false 而不是崩溃**；
  真正变绿要靠诊断 §A 的夹具修法（先推到队尾 / 先断视图存在）——不在本代理白名单内。

---

## Intro 顺序（`BattleDriver.cs:6350` + `:3350`）

1. `TickIntroMonologue`：把 `if (_introStage > 1) { _introStage = -1; return; }` **移到**
   `if (_unitChat.IsSpeaking || _unitChat.ShownSide != -1) return;` **之后**。
   理由 = 原版是一条协程（`yield return` 等本句播完、循环走完才结束）；旧顺序让「两条都播完」
   在第二句还在说时就成立。
2. `Begin`（`:3350`，紧跟 `_introStage = -1` 那一族「本局账不跨局」）：
   `_unitChat.HideAll()` + **新加的** `_unitChat.StopVoice()`。
   - `HideAll` 按既有口径**不碰音频**（`CloseTutorialChatBoxes` 的注释写着原版 `CloseChatBox` 是否停音没核到）
     ⇒ 没有改它的语义，另开 `StopVoice`（`UnitChatPanel.cs:923`），**只给 `Begin` 用**。
   - 两者都缺一不可：气泡不清 ⇒ 新局第一句被「气泡还在 ⇒ 等」闸死；音频不停 ⇒ `IsSpeaking` 同样闸死。

---

## typecheck

```
TMPDIR=/tmp/wf_btprod bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 --- 运行时错误数: 0
--- 编辑器程序集 --- 编辑器错误数: 0
```

行尾复核（`io.open(...,'rb')` 数字节）：`BattleDriver.cs` CRLF 22846 / LF 22846（全 CRLF）；
`UnitChatPanel.cs` / `CameraIntro.cs` 全 LF。`git diff --numstat`：
`BattleDriver.cs 375/66` · `UnitChatPanel.cs 171/7`（含本批别人已写的部分），**没有整篇翻行尾**。

---

## 没做 / 交给别的线

- `Editor/BattleScene.cs` 的夹具（`DrainQueue`、`SimulateOpenCommand` 前先推队尾、G/H 组的 `seen` 条件、
  `T12` 若改成显式自起播）**未动** —— 不在白名单。
- 诊断 §A 的 B 组（`SetMySeat` 中途翻座位）、D/E/F/H/L/M/N/O/P/Q/R 组：均非本代理范围。
- ⚠️ **留给审查的一条**：`CameraIntro` 在批处理档「起播不写位姿」依赖「没人 `Step` 就有没人动相机」。
  若将来某个自检节在 `Begin` 之后先 `Step` 再量 `boardCam`，相机会停在 clip 中段（不是本位）。
  彻底版是给 driver 加 `SettleCameraIntroForTest()` 并在建场 `Begin` 后立刻调（诊断方案 2 的后半），
  但那要动 `Editor/BattleScene.cs` ⇒ 本轮做不到。
