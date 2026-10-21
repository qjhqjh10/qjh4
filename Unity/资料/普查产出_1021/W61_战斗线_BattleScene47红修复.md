# W61 战斗线 · `BattleScene` 47 红修复（执行代理 L）

> 2026-10-21 第六十一会话。按只读诊断 `W61_战斗线_BattleScene47红诊断.md` 逐组修 **G1 / G2 / G4 / G3**；
> **G5 未碰**（已知 γ）。只改了 `Editor/BattleScene.cs` 与 `Battle/BattleDriver.cs` 两个文件。
> **未跑 Unity / 未跑自检 / 未动 git**；只跑了秒级类型检查（`TMPDIR=/tmp/wf_l 工具/typecheck.sh` ⇒ 运行时 0 错 / 编辑器 0 错）。

---

## 1. 结论一句话

**G1（43 条）= 测试侧抽 `FindHudNode(name)`（先根画布直接子件、再 `BattleHudLayer/`）替换 6 处非递归查找；
G2（1 条）= `HudCanvas.Add(_hudLayer, 1000f)` → `4.9f`（两枚黑边件回到子件序 0/1）；
G4（1 条）= ASH77 那条断言改走与生产同一个兜底（`raw > 0 ? raw : EventTiming.RangedVfxFallback`，期望值仍是 0.75）；
G3（1 条）= `Begin()` 清身份表后把 `_myUnits/_foeUnits` 里仍存活的视图 `SetActive(true)` + 4 条回归断言。**
47 红里应只剩 **G5（手牌数框高 γ，1 条）**。

---

## 2. 改动清单

### G1（测试侧 · 43 条）— `Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs`

| 位置（改后行号） | 改动 | 为什么 |
|---|---|---|
| `:620-634`（新增 `FindHudNode`） | `Transform FindHudNode(string name)`：`HudCanvas.RootRect.Find(name)` → 找不到再 `RootRect.Find("BattleHudLayer/" + name)` | `BattleHudLayer` 一建，`Hud()`/`HudImageTex()`/`AddEnergyLight()` 的件变成根画布的**孙辈**；`Transform.Find` 不递归 ⇒ 六处查找口全 null。**两档都留**：黑边件 / 回放条 / 等待横幅不进那一层 |
| `:616` `HudPxOf` | `RootRect.Find(name)` → `FindHudNode(name)` | 8 条底板/名牌尺寸红 |
| `:680` `CheckHudBox` | 同上 | 19 条前提红（并让它后面 ≈171 条静默跳过） |
| `:18413-18414` `A1302` `rtOff/rtCam` | 同上（`OffensiveButton` / `CenterCameraButton`） | 4 条「0.000 / 两条读法不一致」 |
| `:18975-18976` QP 夹具 `QPText_Me/Foe` | 同上（`SetActive` 夹具的查找口一起改） | 诊断点名的「夹具也要一起改」 |
| `:19072` no-box 循环 | 同上（`TurnLabel`/`TurnClock`/`ResultLabel`） | 3 条前提 + 3 条正文 |
| `:19888` `A1502②` 的 `Rt()` | 同上 | 12 条 `A1502②/Rt` |

顺手把 4 处因此**过期**的 doc 就地订正（铁律 5，均带 `2026-10-21（G1）就地订正` 字样）：
`HudPxOf` 的 doc（`:606-610`）· `CheckHudBox` 的 doc（`:670-674`）· `Rt` 的 doc（`:19875-19877`）· `IsDrawn` 的「节点不在」文案（`:19885`）。

⛔ **没动的两处 `RootRect.Find`**（都不是 G1 的病灶）：
- `:19205` `FindPiece`（两枚 `Aspect Ratio Filler`）—— 它们**不进 HUD 层**（`BuildOneAspectFiller` 直接挂根画布），且 G2 修完正是要它保持在根画布直下、子件序 0/1；
- `:19003` `hudStaleHint`（见 §4 顺手发现 ①）。

### G2（生产侧一行 · 1 条）— `Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs`

| 位置（改后行号） | 改动 | 为什么 |
|---|---|---|
| `:19460`（块 `:19441-19461`） | `HudCanvas.Add(_hudLayer, 1000f)` → **`4.9f`** | `Group.Add` 按 z **降序**插兄弟序 ⇒ z=1000 的层占了子件序 0，把 `ZFiller = 5f` 的两枚黑边件挤到 1/2 ⇒ 黑边画在**整层 HUD 之上**（与「原版 `m_Children[0]/[1]` 在最底」相反）。`4.9` 同时满足：**> 0.6（底板）/ 0（字）**、**< 5（黑边件仍在层之下）** |

**「没有别处依赖 1000」已核**（`grep -n "1000f"` 全 `CardPresentation/`）：`BattleDriver.cs` 里只有这一处；
其余 `1000f` 全是无关的毫秒/像素/名字里的数（`CollectionWindow` 计时、`WaitBanner.ShadeRaycastPadBottomPx`、
`CardView` 的 key 取整等）。根画布直子件里 z 的**全集**也核过：`5`（两枚黑边件）· `4.9`（本层）·
`0.3`（`ReplayBar.Z`）· `-0.45`（`WaitBanner.Z`）；其余件全在各子画布内（子画布由 `ApplySubCanvasRank`
按名字排到**末尾**，与本层 z 无关）⇒ **相对次序与加这一层之前一致**，唯一变化 = 黑边件回到层之下。

### G4（测试侧一行 · 1 条）— `BattleScene.cs`

| 位置（改后行号） | 改动 | 为什么 |
|---|---|---|
| `:1058-1059` | `float rawASH = VfxMap.ChainDuration(...); float dASH = rawASH > 0f ? rawASH : EventTiming.RangedVfxFallback;` | `ChainDuration` 契约「查不到 ⇒ 0」；生产在 `BattleDriver.RangedVfxDurationBefore` 把 0 → −1、再由 `EventTiming.RangedDamageLag` 用 `RangedVfxFallback = 0.75` 兜底 ⇒ 原来断言拿 **0** 比 0.75 是**绕过生产兜底口** |
| `:1065` | 断言消息补一句 `ChainDuration 原始返回 {rawASH:F2}` | 让「为什么是 0.75」在日志里自证 |

⛔ **期望值仍是 `0.75`、没改成 1.0** —— ASH77 的真值（0.75 还是 1.0）按诊断 §5 **未裁定**，本批只修「绕过兜底」。

### G3（生产侧小修 + 回归断言 · 1 条）— `BattleDriver.cs` + `BattleScene.cs`

| 位置（改后行号） | 改动 | 为什么 |
|---|---|---|
| `BattleDriver.cs:1010-1029` | 新增 `static int ReviveBoardViews(Dictionary<int, CardView>)` | 把「格号→视图」表里**还活着、但被关掉**的视图拨回 `SetActive(true)`；**拨完仍在 `activeInHierarchy` 之外 ⇒ `LogWarning`（不许静默失败）**；已销毁条目跳过（`SyncBoard` 自己换） |
| `BattleDriver.cs:3616-3634`（`Begin()` 里 `_myUnitViewByUnit.Clear()` 之后） | 调它 + 一张一条 `Debug.Log`（拨了几张） | 教程局 `BeginTutorial` 把敌督军 `SetActive(false)`（两拍先藏），而 `TickTutorialView` 第一句 `if (Ctx.Tutorial == null) return;` ⇒ 教程局没跑完两拍 / 下一局一开（`Begin` 还会把 `_tutBeatDone` 拨回 false）⇒ 视图留在 inactive；`_myUnits/_foeUnits` **跨局复用** ⇒ 新一局 `SyncBoard` 认领同一张 inactive 视图 ⇒ 不画、`CardHit` 恒 −1 |
| `BattleScene.cs:27055-27088` | 新增 G3 回归断言块（4 条） | 见 §3 |

**安全性两条已核**：
- ⛔ **不动复用机制**（不 Kill、不清表）—— 残骸体「同一张卡翻个面」靠它；
- 全仓 grep `\.gameObject.SetActive(false)`：**对场上 `CardView` 根节点**的只有教程两处
  （`BattleDriver` 的 `BeginTutorial` / `TickTutorialView`）；飞行那一档用的是「关渲染器」
  （`_aiDeployHiddenRenderers`），`CardView` 内部的 `Show(MeshRenderer…)` / `_stats[s]` 关的都是**子层**
  ⇒ 本复位**不会把该藏的拨亮**。

---

## 3. 断言清单

### 3·1 G3 新增（`BattleScene.cs:27055-27088`，放在 ⑯-⑥ 收尾 `BeginTutorial(0)` 之前）

判据链（缺任一步这条就没有鉴别力）：

1. **（前提）** `driver.BeginTutorial(0)` 之后，敌督军视图 `!activeSelf` —— 证明「教程确实藏了它」；
2. **（前提）** 紧接着 `driver.Begin(A, B, 20261021)`（普通局）后 `BoardViewAt(WarlordSlot, false)` **`==` 同一张视图** —— 证明验的确实是「跨局复用」那条路（不是重建）；
3. **🧨 回归主条**：该视图 `activeInHierarchy == true`。消息里写死「**修复前此条必红**」及机制（只清身份表 ⇒ 认领 inactive 视图 ⇒ 不画 + `CardHit` 恒 false）；
4. **行为那一半**：`HitSlotForTest(1 - MySide, PointerAtWorldForTest(view.transform.position)) == WarlordSlot`
   —— 只点亮节点但命中没恢复 ⇒ 红。

### 3·2 G4 改口径（`BattleScene.cs:1050-1068`）

- `dASH` 由「直接拿 `ChainDuration` 的返回值」改成「**与生产同口径**的兜底值」；**期望值 0.75 不变**；
- 三档判别式（`UM83` 1.0 / `ASH77` 0.75 / 1-shot 0.3）与「灭自证 `|dUM − dASH| > 0.2`」都保持原样。

---

## 4. 没查清 / 顺手发现

### ① 顺手发现（**未改，按简报只动那 6 处**）——`hudStaleHint` 的反向断言被这一层**削弱了**

`BattleScene.cs:19003` `var hudStaleHint = HudCanvas.RootRect.Find("HintLabel")` 接着
`Check(hudStaleHint == null, "自建的 `HintLabel` 不许再出现")`。

- **现状**：它只找**直接子件**。若有人把自建 `HintLabel` **通过 `Hud()` 加回来**，那颗会落进
  `BattleHudLayer` ⇒ 这条**仍然绿**（= 反向断言失去判别力）。这是 `BattleHudLayer` 引入后**新出现**的松口。
- **建议**（1 行、零风险、语义只会变强不会变弱）：把它也改成 `var hudStaleHint = FindHudNode("HintLabel");`。
- 我**没有自己改**：简报点名「替换这 6 处」，第 7 处属于改断言语义 ⇒ 交给调度台裁。

### ② `G5` 未碰（已知 γ）

手牌数框高（框 234×64、实测 75.81）—— 按公文与诊断 §2 留在账上，本批不动。

### ③ ASH77 的 `AnimInfo` 真值（0.75 vs 1.0）仍未裁定

诊断 §5 的两份证据打架（`W61_战斗线_演出链第1批.md:48/96` 写 0.75；`card_anim_map.json:198-207`
四字段相加 = 1.0）。本批**只修断言绕过兜底这一件事**，⛔ 没有把期望值改成 1.0 —— 这一格继续挂账。

### ④ G3 的 4 条新断言**没有在 Unity 里跑过**（铁律 12：本批不验收）

静态推理：`SyncBoard` 在身份表清空后走「按格号认领旧视图」退路（`ViewOwnedByLiveUnit` 对空表恒 false）
⇒ 前提 2「还是同一张」应成立。若实测前提 2 红 ⇒ 说明视图被 `DetachStaleView` 摘掉过
（那也说明修复点要往 `SyncBoard` 挪），请把它当**诊断输入**而不是「修坏了」。

### ⑤ 其他「同一层」的读口已巡过，无需改

- `HudCanvas.OrderZOf` 按**父节点分组**记账 ⇒ 件挂进层之后仍读得到自己那一档（`A1502②` 的 `QZ` 不受影响）；
- `BattleDriver.HudLabelOnCanvasForTest` 的父链判据是 `p == RootRect || p.parent == RootRect` ⇒ 字挂本层时走后者（层自己的 doc 已写明）；
- `ApplySubCanvasRank` 只断**相对次序**（`BattleScene.cs:27183-27195`）⇒ 根画布多一个子件不影响。

---

## 5. 主对话同步点要跑什么

**跑 `BattleScene.Run`（单条）** —— 本批只动了它一个宿主（`BattleDriver` 是它的被测件，其余宿主不 import）。

期望：

| 项 | 期望 |
|---|---|
| **失败数** | **1**（只剩 **G5**：手牌数框高 γ） |
| **通过数** | 比 3946 **明显上升**：G1 会恢复被 19 条前提挡住而**静默跳过**的那一批（≈110–170 条），外加本批 **+4** 条（G3 回归，第 4 条依赖前一条）；量级应回到基线 **≈4102** 那一档 |
| 重点看 | ① `Aspect Ratio Filler` 的「子件序 **0 / 1**」那条（G2）；② `A1502②` 12 条（G1 的 `Rt`）；③ 新出现的 `🧨 G3 回归` 4 条；④ 日志里有没有 `[Battle] 新一局：把上一局留下的 **N** 张…`（N=0 说明没走到 G3 场景，那条断言的前提本身会先红） |
| 若仍有红 | 先按上面的「组」对号 —— **别把它当新账**，直接回贴到这份报告上；`BattleScene.Run` 的已知间歇 = 退出码 139（判绿红看**断言合计**、不看退出码） |
