# X5 · `CollectionScene` 3 红修复（执行报告 · 2026-10-25）
判据 = `资料/普查产出_1025/Dg5_Collection3红诊断.md`。本轮**只改 1 个文件**。

## 1. 改了什么
`Unity/MyGame/Assets/CardPresentation/Editor/CollectionScene.cs`（纯 LF 未变；diff **+48 / −4**）
- **`:10564-10588`**：读 `ti = ttTmp.textInfo` **之前**补一次 `Label.ForceRelayout()`
  （`ttLb.GetComponentInChildren<Label>(true)` + 一条 `（前提·不静默）` 断言；拿不到 ⇒ 出声）。
  判据链（已写进注释）：未激活页建 ⇒ `Label.cs:641-649` 记待办 ⇒ 补做那一刀是 **单跑一遍 `GetTextInfo`**
  （`Core/TmpFont.cs:572-576`）⇒ 自适应档在 Phase II 之前 early-return（`TextMeshPro.cs:4138-4155`）⇒
  `characterCount = 0`；`ForceRelayout()` 已激活档 = `SetFontSize(现值)→ForceMeshUpdate()`（`Label.cs:924`）
  ⇒ `OnPreRenderObject` 的收敛循环（`TextMeshPro.cs:2171-2175`）。
- **`:10630-10652`**：前提拆成**各自独立** —— 新增 `characterCount > 0`、`nV > 0` 单独一条；
  空集时 `nBad==0` / `nBadBelow==0` 从「恒真假绿」改成 **「判不了」⇒ 出红**（log `:161624`/`:161638` 正是这两条）。
- ⛔ 未动 `Battle/Label.cs` · `Shell/TitleTab.cs` · `Shell/PlayerProfileWindow.cs` · `Shell/AvatarTab.cs`（理由 §3）。

## 2. 真机受不受影响 ⇒ **不受影响**
1. `TmpFont.GenerateLayout` = `GetTextInfo`，跑在 `TextRenderFlags.DontRender` ⇒ **Phase III 被跳过、从不产出可见网格**
   （`Core/TmpFont.cs:560-571`，文件自己写着「画面由 TMP 自己在对象激活后重排」）。
2. TMP 自己的激活路：`[ExecuteAlways]`（`TextMeshPro.cs:19`）⇒ 激活跑 `OnEnable`（`:630-668`）→ `SetAllDirty()`（`:665`）
   → `SetVerticesDirty` 登记 graphic rebuild（`:220-230`）/ `SetLayoutDirty` 立 `m_isLayoutDirty`（`:236-247`）
   → `TMP_UpdateManager` 挂 `Canvas.willRenderCanvases`（`TMP_UpdateManager.cs:52`）→ `DoRebuilds`（`:154-178`）
   → `Rebuild(CanvasUpdate.PreRender)` → `OnPreRenderObject()`（`TextMeshPro.cs:281-295`）
   → `while (m_IsAutoSizePointSizeSet == false) GenerateTextMesh();`（`:2129-2175`）= 完整自适应收敛。
   整条在**该帧绘制之前**跑完 ⇒ 真机不会出现「字不显示」。
3. 实证：`D:/4/_tmp_view/shots/2026-10-25_第七十五会话/17_achievements.png` —— 档案窗**非默认页** `AchievementsMenu`
   （与 `TitleDrawer_9` 同样是「未激活时建、点页签才激活」）真机上**字全在**；它**没有** `OnOpen` 覆写
   （`Shell/WindowTabBase.cs:17` 空实现）、也没有激活后重排调用 ⇒ 那段字只能来自第 2 条。
   ⇒ 本批 3 红 = **批处理无帧循环时「读得太早」**，不是渲染缺陷。

## 3. 为什么不在实现层修（②-3 结论）
- 在 `TitleTab`/`PlayerProfileWindow` 补重排 = **替 TMP 做它自己会做的事**，真机纯冗余；
- 在 `Label.cs` 改「已激活单跑一遍够用」这条口径（`Label.cs:691-695` P7）会牵动 12 个宿主，判据也不支持；
- ⛔ `AvatarTab` 的「建满 469 格」照的是原版（`AvatarTab__InitializeGameObjects.c:140-160`），**未动**；
- 遗留候选（**未证**）：469 颗标签同帧激活可能在真机造成一帧生成尖峰（卡顿，不是缺字）；日志无相关告警，本笔判不了。

## 4. 断言与灭自证
- 新增 2 条断言 ⇒ 总数 **2358 → 2360**（原读数 2355 / 2358 · 3 红）。
- 灭自证：删掉 `:10588` 那句 `ForceRelayout()` ⇒ `characterCount = 0` ⇒ 预计 **6 红**
  （`nChars`/`nV`/`nBand`/`minA` 4 条 + 空集两条「判不了」），**不是只红 1 条** ⇒ 修法载重、空集不再假绿。
- 不碰被测机制：渐隐带期望值仍是原版剖面 `255 × (855.46 − y) ÷ 50`，重排只把模型刷成当前真值。

## 5. typecheck
`TMPDIR=/tmp/wf_coll bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（改动后、注释订正后各一次）。

## 6. 没做的 / 还差什么
- **没跑 Unity**（主对话统一跑）：需复跑 `CollectionScene.Run`，验收口径 = **0 红 / 总数 2360**。
- 未逐帧验真机：「真机不受影响」止于第 3 条那档证据（静态链 + 真机截图），未跑 player 逐帧看。
- `Label.PendingWrapAppliedCount` 没用来分辨「OnEnable 没跑」vs「单跑一遍排空」—— 批处理下 `OnEnable` 本就不跑
  （`Label.cs:946-949`），这个计数读不出结论。
