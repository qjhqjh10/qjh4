# W61 战斗线 · BattleScene 47 红只读诊断

> 只读代理 K（2026-10-21 第六十一会话）。未改任何 `.cs` / 资产 / 正本，未跑 Unity / 测试 / git。
> 本代理无写盘能力，**正文由主对话原样落盘**。
> 本轮：`=== 合计：3946 通过 / 47 失败 ===`（基线 4102/1）。

---

## 1. 一句话结论

**47 红 = 5 组，唯一根因组是 B 的 `BattleHudLayer`**（43 条测试侧查找没跟上 + 1 条生产侧层序被挤）；其余 3 条分别是 H 的新断言暴露的存量「教程藏敌督军跨局复用」、J 的 VFX 断言绕过 `RangedVfxFallback`、以及基线已知的手牌数框高 γ。**本批回归 = 组 1（测试侧）与组 2（生产侧各 1 行）。**

---

## 2. 分组表

| 组 | 条数 | 根因（`文件:行号` + 机制） | 分类 | 最小修法 | 是否本批 |
|---|---|---|---|---|---|
| G1 | **43** | `BattleDriver.cs:19399-19412` 新建 `BattleHudLayer`，`Hud()`/`HudImageTex()`/`AddEnergyLight()` 的裸 Transform 父件改挂该层（`:22188-22190` / `:22330-22332` / `:22394-22397`）；测试侧仍用**非递归**的 `HudCanvas.RootRect.Find(name)`（`BattleScene.cs:613` `HudPxOf`、`:652` `CheckHudBox`、`:18375-18376` A1302、`:18936-18937` QP 夹具、`:19032` no-box 循环、`:19846` `Rt`）⇒ 件现在是 RootRect 的孙辈 ⇒ 全部 null。子项：HudPxOf 8 · CheckHudBox 前提 19 · A1302 4 · A1502②/Rt 12 | **α**（测试侧；生产侧按设计正确，`HudCanvas.FillParent` 给层 = 画布整矩形，子件锚点/几何一个数没变） | 测试侧：查找口统一加回退 `RootRect.Find("BattleHudLayer/" + name)`（建议抽 `FindHudNode(name)`）；QP 夹具的 `SetActive` 查找也要一起改 | **是（B 的 HUD 层）** |
| G2 | **1** | `BattleDriver.cs:19412` `HudCanvas.Add(_hudLayer, 1000f)` vs `:21441` `ZFiller = 5f`；`HudCanvas.cs:397-406` `Group.Add` 按 z **降序**插兄弟序 ⇒ 层占子件序 0，两枚黑边件被挤到 1/2（uGUI 序 0 = 最底 ⇒ 黑边现在画在**整层 HUD 之上**，与原版 `m_Children[0]/[1]` 相反） | **γ**（本批回归，生产侧层序） | 生产侧一行：`HudCanvas.Add(_hudLayer, 4.9f)`（>0.6 且 <5；其余根画布直子 z 都 ≤0.3 或为负） | **是（B）** |
| G3 | **1** | `BattleDriver.cs:3208-3209` `BeginTutorial` 把敌方督军视图 `SetActive(false)`；`Begin()`（`:3593`）只清两张身份表，**不清也不复位** `_myUnits/_foeUnits` 里的旧 CardView；`SyncBoard` 首次同步按格号**认领旧视图**（`:18198-18206`）⇒ 复用同一张 inactive 的敌督军；`CardHit`（`:23454-23457`）对 `!activeInHierarchy` 直接 false ⇒ 敌督军恒 -1 | **β**（存量生产缺陷，H 的新断言暴露；旧 `Physics2D` 同样查不到未激活对象） | 生产侧：`Begin()` 清身份表后把 `_myUnits/_foeUnits` 里仍存活的视图 `SetActive(true)`（或清表重建）；加「教程局→普通局，敌督军 active」回归断言 | 断言是本批 H 加的；行为是旧账 |
| G4 | **1** | `VfxMap.cs:991-1033` `ChainDuration` 查不到时**按契约返回 0**；ASH77 的 `BulletImpact_DeathSpinner_spider` 不在 `SharedRangedVfxDuration`（`:1047-1063`），日志 2035 有「没有登记 AnimInfo 时长」。生产路径 `BattleDriver.cs:16280` 把 0→**-1**，再由 `EventTiming.RangedVfxFallback`（0.75）兜底；断言 `BattleScene.cs:1022-1031` 却直接拿 0 比 0.75 | **α**（测试侧；绕过生产兜底口） | 测试侧一行：`float raw = VfxMap.ChainDuration(...); float dASH = raw > 0f ? raw : EventTiming.RangedVfxFallback;`（与生产同口径） | **是（J 的新断言）** |
| G5 | **1** | 已知 γ：手牌数字框高（框 234×64；实测高 75.81 > 64） | **γ**（基线既有） | 不在本批范围；单独开账 | 否 |

合计 43 + 1 + 1 + 1 + 1 = **47**。

---

## 3. 逐组机制

### G1（43 条）：父链 `RootRect → BattleHudLayer → 件`，测试仍只找直接子件
- 生产建层：`BattleDriver.cs:19399` `new GameObject("BattleHudLayer")` → `SetParent(cvRoot)` → `HudCanvas.FillParent(_hudLayer)`（锚点 0..1、pivot 中心、sizeDelta 0、anchoredPosition 0）→ `HudCanvas.Add(_hudLayer, 1000f)`。
- 三族建件口改父：`Hud()`（`:22188-22190`）、`HudImageTex()`（`:22330-22332`）、`AddEnergyLight()`（`:22394-22397`）。`MakeAnchored(parent=layer)` 只 `SetParent(parent,false)`，几何仍按画布归一化锚点写 ⇒ **尺寸/位置零变化**。
- 测试读口：`RootRect.Find(name)` 是 `Transform.Find`，**不递归**（`BattleScene.cs:644`、`:19833` 自注）⇒ 件从「RootRect 直接子件」变成「BattleHudLayer 直接子件」后全部 null：
  - `HudPxOf`（`:611-615`）→ `(-1,-1)`：8 条底板/名牌红。
  - `CheckHudBox`（`:648-659`）前提 19 条红并 `return` ⇒ 每颗后面 9 条被静默跳过（≈171 条）。
  - A1302 `rtOff/rtCam = RootRect.Find(...)`（`:18375-18376`）为 null ⇒ `HudCanvas.SizePx(null)=(0,0)`，而 driver 自检口 `HudExtraDrawnPx` 仍对 ⇒ 4 条「0.000 / 两条读法不一致」全是查找口断的。
  - `Rt()`（`:19844-19847`）为 null ⇒ 12 条 A1502②/Rt 红。
- 判定依据：`ApplyMulliganHudVisibility` 用 `CanvasGroup.alpha`，**不 SetActive** ⇒ 节点 `activeInHierarchy` 仍 true；日志 57898 打印出 `父 BattleHud Canvas` 证明层存在。生产几何若错，driver 自检口也会错，但它们是绿的。
- 结论：**生产侧正确、测试侧查找过期**（α）。

### G2（1 条）：子件序 0/1 被层挤到 1/2
- 黑边件建在根画布下、`ZFiller=5f`（`:21437-21446`），原意「z 比所有 HUD 件大 ⇒ 子件序 0/1」。
- `Group.Add`：`while (i < _zs.Count && _zs[i] >= z) i++; rt.SetSiblingIndex(i);`（`HudCanvas.cs:397-406`）⇒ 层 z=1000 插到 0，两枚 z=5 的黑边件落到 1/2。uGUI 下标小 = 先画 = 在底下（`HudCanvas.cs:373-378`）⇒ 黑边现在画在整层 HUD 之上。
- 修法：层 z 取 `(0.6, 5)` 之间（建议 **4.9**）。

### G3（1 条）：教程藏起来的敌督军视图跨 `Begin` 被复用
- `BeginTutorial`（`:3191`）先 `Begin` 再 `foeW.gameObject.SetActive(false)`（`:3208-3209`）；下一次 `Begin`（`:3341`）在 `:3593` 只 `_myUnitViewByUnit.Clear()`，`_myUnits/_foeUnits` **不清**；`SyncBoard` 首次同步走退路 `views.TryGetValue(s, out byId) && !ViewOwnedByLiveUnit(...)`（`:18198-18206`）⇒ 复用同一张 inactive 的敌督军。
- `CardHit` 的 `!activeInHierarchy` 守卫（`:23454-23457`）⇒ 恒 -1。日志无 collider 缺失/禁用证据，排除 collider 问题。
- 修法：`Begin()` 清身份表后复位存活视图的激活态（或清表重建）+ 回归断言。

### G4（1 条）：VFX 断言绕过了生产兜底
- `ChainDuration` 文档写明「返回 0 = 调用方按不等处理」（`VfxMap.cs:986-990`）。ASH77 的 `AnimTimes` 只有 `UseSpiritStone` 三条；其远程件不在 `SharedRangedVfxDuration`（`:1047-1063`）⇒ 0，日志 2035 出声。
- 生产：`RangedVfxDurationBefore` 0→-1（`BattleDriver.cs:16280`）→ `EventTiming.RangedDamageLag(-1)` 用 `RangedVfxFallback=0.75`（`EventTiming.cs:254-264`）⇒ 实际 0.95 s 结算。
- 断言直接拿 0 比 0.75 ⇒ 红。测试侧补兜底即可。

### G5（1 条）：已知 γ
- `_handLabel.WorldW/WorldH`（`BattleDriver.cs:12784-12785`），框 234×64（`:1609`/`:19803`）；实测高 75.8061。基线唯一红，本批未动。

---

## 4. 建议修复顺序

1. **G1（43 条，测试侧）**：抽 `FindHudNode(name)`（先 `RootRect.Find(name)`，再 `RootRect.Find("BattleHudLayer/" + name)`），替换 `BattleScene.cs:613 / 652 / 18375-18376 / 18936-18937 / 19032 / 19846`。零产品风险；会连带恢复被挡掉的 ≈171 条下游断言。
2. **G2（1 条，生产侧一行）**：`BattleDriver.cs:19412` `1000f` → `4.9f`。
3. **G4（1 条，测试侧一行）**：`BattleScene.cs:1022` 起加 `raw > 0 ? raw : EventTiming.RangedVfxFallback`。
4. **G3（1 条，生产侧小修）**：`Begin()` 清身份表后复位存活视图激活态；加回归断言。
5. **G5**：已知 γ，本批不碰，单独开账。

---

## 5. 没查清的部分

- **ASH77 的真实 `AnimInfo` 到底 0.75 还是 1.0**：`W61_战斗线_演出链第1批.md:48/96` 写 0.75「恰好等于它自己的 AnimInfo」；但 `Unity/数据/游戏数据/card_anim_map.json:198-207` 实读该件为 `timeAtStartPos 0.75 + timeMoving 0.25 + timeAtEndPos 0 + vfxDelay 0`，而 `AnimInfo__GetAnimDuration.c` 四字段全加 ⇒ 应为 **1.0**。否则会补出一个错值。
- **G3 的 inactive 是「排除法 + 调用链」结论**：日志没有直接打印 `activeSelf`；建议修完加显式探针留证。
- **E 的 CardView 条带/描边改动**与 47 条无交集证据。
- 本报告为静态读码 + 日志对齐，未跑 Unity/自检，修法未实测。
