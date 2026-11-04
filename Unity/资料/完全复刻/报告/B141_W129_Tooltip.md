# B141 · W129 · `4-14` Tooltip 面板几何 + `4-22` 正文字号 —— 执行报告

> 执行代理 **B141-W19** · 2026-11-04 · 第一百二十八会话 · 波次 `W129`。
> 施工单 = `判据/B141_W129_对账_UI动画.md` 的 **W10 包** + `报告/B138_审计汇总_问题清单.md` §4 `4-14` / `4-22`。
> ⛔ 未跑 Unity / 未动 git / 未改 `d:/2` / 未改账主表 / 未动任何白名单外文件。

---

## 一、结论

两条都**改完了**，而且比施工单写的多拿到一条判据（原版**上下内边距 10** 与**面板高下限 110** 是能直接读出来的，不用「反推」）。

| 项 | 原版（`BasicToolTip.prefab`） | 改前 | 改后 |
|---|---|---|---|
| 面板宽 | 根 `m_SizeDelta.x = 500`（布局组不控宽 ⇒ **宽恒 500**） | `MinW 120` / `MaxW 640`（自定） | **`MinW 500`**（长句仍留 `MaxW 640` 生长余量，见 §五·2） |
| 正文宽 | `Text (TMP)` `m_SizeDelta.x = 468.90002` | 由 `PadX 20` 推 ⇒ 随文案变 | **反推 468.9** ✅（`PadX 15.55`） |
| 左右内边距 | (500 − 468.9) / 2 = **15.55**（原版无 pad 常量，由两个矩形定） | **20** | **15.55** ✅ |
| 上下内边距 | `VerticalLayoutGroup.m_Padding` = **Top 10 / Bottom 10** | **14** | **10** ✅ |
| 面板高下限 | 正文自定义 fitter `heightMin 90` + 上下各 10 ⇒ **110** | 无（单行只有 `30 + 28 = 58`） | **`MinH 110`** ✅ |
| 正文字号 | `m_fontSize = 28` × Pragati 大写高比 **0.656** | `0.72 × 28 / 108`（em 读回 **30.73 px**，大 9.7%） | **`0.656 × 28 / 108`**（em 读回 **28.00 px**） ✅ |

落点（**现读**行号）：`Unity/MyGame/Assets/CardPresentation/Core/Tooltip.cs:121-129`（常量）· `:608`（高度公式）。
断言 = **新建** `Unity/MyGame/Assets/CardPresentation/Editor/B141_W19_Assert.cs`（27 条检查，见 §四）。

---

## 二、原版判据（全部本地现读，⛔ 不是照截图量的）

文件：`d:/2/tools/assetripper_out/ExportedProject/Assets/BasicToolTip.prefab`

1. **根 = 500 × 100**
   `:100-119`（`--- !u!224 &224884547391421755`，`m_GameObject: 1709254077376921` = `m_Name: BasicToolTip`）：
   ```
   m_Father: {fileID: 0}
   m_AnchorMin: {x: 0.5, y: 0.5}   m_AnchorMax: {x: 0.5, y: 0.5}
   m_SizeDelta: {x: 500, y: 100}
   ```

2. **面板本体 = `Generic Popup Background`（根的唯一子件），宽 500、高由子件撑**
   `:22-40`（`--- !u!1 &1730242686860875`）挂着 **五件**：
   * `RectTransform 224587765826080250`（`:156-176`）`m_SizeDelta = (500, 0)`、锚点居中 ⇒ **宽恰 500**
   * `Image`（`m_Type: 1` Sliced，`m_PixelsPerUnitMultiplier: 3`）⇒ 可见面板底图就在这一颗上
   * **`VerticalLayoutGroup`**（`:216-240`）：`m_Padding = {L0, R0, T10, B10}`（`:228-232`）·
     `m_ChildControlWidth: 0`（`:237`）⇒ **不控子件宽度**（所以正文那 468.9 是自己定的）·
     `m_ChildForceExpand{Width,Height}: 1`
   * `ContentSizeFitter`（`:241-255`）`m_VerticalFit: 1`（MinSize）⇒ **高度 = 子件高 + 上下 pad**

3. **正文 `Text (TMP)`**
   `:406-424`（`m_GameObject: 1703437598541787` = `:94` 的 `m_Name: Text (TMP)`）：
   ```
   m_AnchorMin/Max: {0, 1}          // 左上锚
   m_AnchoredPosition: {x: 250, y: -55}
   m_SizeDelta: {x: 468.90002, y: 0}
   ```
   `250 − 468.9/2 = 15.55` ⇒ **左右各 15.55**（与「(500 − 468.9)/2」同一个数，两条独立算出来吻合）。
   TMP 本体（`:445-519`）：`m_fontSize: 28`（`:481`）· `m_enableAutoSizing: 1` · `m_fontSizeMin: 10` ·
   `m_fontSizeMax: 28` · `m_HorizontalAlignment: 2`(Center) · `m_VerticalAlignment: 512`(Middle) ·
   `m_fontAsset` = `2548b731…`（= Pragati；文件头 `A1024③` 那条已钉）。

4. **高下限 / 上限 = 正文 fitter 给的，不是根上那个 100**
   正文那颗自定义 fitter（`:537-544`）：`m_VerticalFit: 2`（PreferredSize）· `clampHeight: 1` ·
   `heightMin: 90` · `heightMax: 200` ⇒
   **面板高 = clamp(正文 prefer, 90, 200) + 10 + 10 ∈ [110, 220]**。
   🔴 **根上的 100 是模板值**（布局组不控宽、高度由 fitter 撑）⇒ **运行时面板从不等于 100**，
   ⛔ 别把「500×100」照抄成运行期尺寸。这一条已写进 `Tooltip.cs:128` 的注释和断言 `A6`。

5. **字号比值 0.656 从哪来**（`4-22`）：`Core/TmpFont.cs` 的 `CapPerFontSize(Pragati)` / `EmPerFontSize(拉丁)`
   实测 `0.0656 / 0.1`（`Label.CapOverGlyph` 的 doc：`0.656（Pragati）/ 0.685（Asar）`）⇒
   `BodyCapWorld = 0.656 × 28 / 108` ⇒ `FontSizeToPxNow` 读回 **em = 28 px**（= 原版 `m_fontSize`）。
   改前的 `0.72` 是 `capLine/pointSize` 的 **em 口径推算**（非实测）⇒ 读回 30.73 px，**大 9.7%**。

---

## 三、改动清单

### 3·1 `CardPresentation/Core/Tooltip.cs`（**行数零变化**：1353 → 1353 行）

| 位置 | 改前 | 改后 |
|---|---|---|
| `:52-53`（文件头 ②） | 「内边距与最小宽高**是我们挑的**」 | 「内边距 / 宽下限 / 高下限**已按原版矩形反推**；仍自设计的只剩 `MaxW` / `LineH`」 |
| `:121-122` | `const float PxPerUnit` | **`public const`**（自检要拿它折 px；行尾注释说明用途） |
| `:123-126` | `0.72f * 28f / PxPerUnit` | **`0.656f * 28f / PxPerUnit`** + 订正 doc（判据、旧值错因、CJK 面注意）+ **`public`** |
| `:128-129` | `PadX 20 / PadY 14 / MinW 120 / MaxW 640 / LineH 30`，注释只有散文「⚠️ 我们挑的」 | **`PadX 15.55 / PadY 10 / MinW 500 / MaxW 640 / MinH 110 / LineH 30`**；注释写明每一项的原版出处（含行号）、**根 100 是模板值**、以及 **`🔶 自己设计` 标记**（`MaxW` / `LineH` 两项 + 为什么必须自定 + 谁定的 + 日期） |
| `:608` | `_h = lines * LineH + PadY * 2f;` | `_h = Mathf.Max(MinH, lines * LineH + PadY * 2f);` |

* 🔶 标记串固定为 **`🔶 自己设计`**（铁律 18），就地写在 `:128` 那条**已有注释**上 ⇒ **零行数变化**（铁律 18·b）。
* 改前 `grep -n "🔶" Core/Tooltip.cs` = **0 命中**（`4-14` 的原症状），改后 = **1 处**，断言 `A11` 盯住它。
* 另打两条 **`✅ 已复刻（2026-11-04 · B141-W19 · <判据>）`**（铁律 20）：`:125`（`4-22`，判据 `BasicToolTip.prefab:481`）·
  `:128`（`4-14`，判据 `:100-119` / `:228` / `:406-424` / `:537-544`）—— 一段一条，**仍零行数变化**。

### 3·2 `CardPresentation/Editor/B141_W19_Assert.cs`（**新建**，类名 == 文件名、全局命名空间）

27 条检查，分三段（详见 §四）。`Run()` 无参数、自己带 `Pass` / `Fail` 计数。
**接线（本代理不动宿主）**：
```
B141_W19_Assert.Run();  pass += B141_W19_Assert.Pass;  fail += B141_W19_Assert.Fail;
```
推荐宿主 = `Editor/BattleScene.cs`（tooltip 在那里是 uGUI 档、已有 tooltip 一组用例）或 `Editor/ShellScene.cs`（9 s，最便宜）。

---

## 四、断言清单（27 条）

| 组 | 条 | 盯什么 |
|---|---|---|
| **A（原版值）** | A1 | `MinW == 500`（原版根宽） |
| | A2 ★ | 面板宽 − 2×`PadX` = **468.9**（原版正文宽） |
| | A3 | `PadX == 15.55` |
| | A4 | `PadY == 10`（原版 `VerticalLayoutGroup.m_Padding`） |
| | A5 ★ | `MinH == 110`（原版 `heightMin 90` + 上下各 10） |
| | A6 | 高下限**不是**根上那个 100（防照抄模板值） |
| | A7 ★ | 比值 = **0.656** |
| | A8/A9 ★ | 前提（字体资产量得到）+ **按资产实测链路读回 em = 28 px**（⛔ 不是把常量再乘一遍） |
| | A10/A11 ★ | 前提（读得到源码）+ **`🔶 自己设计` 标记真的在** |
| **B（灭自证）** | B1 | 旧 `PadX 20` ⇒ 正文宽只有 **460**（差 8.9 px）⇒ 改回去 A2 必红 |
| | B2–B4 | `PadX` 不是 20 · `PadY` 不是 14 · `MinW` 不是 120 |
| | B5 ★ | 比值**不是 0.72 那一档**（改回 0.72 ⇒ B5 + A7 + A9 一起红） |
| | B6 | `MaxW > MinW`（生长余量还在，长句不会画到底图外） |
| **C（实例级）** | C0a/C0b | 前提：没被抑制 / **真建出了一只 tooltip**（建不出 ⇒ 本行先红，⛔ 不静默跳过） |
| | C1 ★ | 短文案面板**宽 500**、C2 ★ **高 110**（⇒ 常量真进了布局） |
| | C3/C4 ★ | 正文喂进去了 + **标签上真挂着 0.656×28 那个大写高**（`DebugDump` 的 `cap=`） |
| | C5a/C5b/C5 ★/C6 ★ | 长文案（`TipText.Health`）：面板宽 = `Clamp(正文宽+2×15.55, 500, 640)`、**面板宽 ≥ 正文宽 + 2×pad**（没截字）；并把**实测正文宽**打进日志（见 §五·2） |

🧨 **灭自证的形状**：A2（原版值）与 B1（旧值推出的 460）**结构上不可能同时满足**；A9 的期望值从**字体资产实测**反算，
不是把 `0.656` 再乘一遍；C 段读的是**运行时**的 `PanelSizePx` / `cap=`，改常量不改实现的话过不去。

---

## 五、没做的 / 需要调度台决定的（⛔ 只报，不改）

1. **中文档字号仍是「按拉丁大写口径」下发的（≈ 小 20–28%）** —— 这是本格的**同族**问题，但**判据属 4-01**（站点→面选择表），
   `4-22` 的施工单也只点名比值。现状：`Tooltip` 用 `_body.SetCapHeight(BodyCap)`（**拉丁大写**口径），
   而正文喂中文时 `Label.SyncFace` 会换到 CJK 面（大写比 **0.0779**、汉字墨高比 **0.0853**）⇒
   按现读常数推，中文正文 em ≈ **20–22 px**（原版 em 28 px）。**正解 = `Label.SetScriptHeight(text, 28f, pxPerUnit)`**
   （按语种在 `SetGlyphHeight` / `SetCapHeight` 之间选 —— 全工程唯一一份判据就在那个方法里），
   即把 `:265` 那一句改成在 `Layout` 里按文案调。⚠️ **本代理未做**（属 4-01 面选择；且要改的是布局路径不是常量），
   **建议并入 4-01 那条一起派**。
2. **我们的折行宽度还是按「字符数」(`MaxCharsPerLine = 22` 单位)，不是原版那 468.9 px** ——
   原版是 TMP 在 468.9 宽的框里自动换行；我们是 `Wrap()` 手折 + `Label` 不裁切。
   ⇒ 面板宽（500）与正文宽（468.9）现在**参数对齐了**，但**同一句话折出来的行**可能不同。
   断言 `C5` 会把「长文案实测正文宽」打进日志（原版 468.9 ⇒ 差值多少一目了然），
   **实测值本批拿不到（不跑 Unity）**，拿到的那个数才是「要不要改 `MaxCharsPerLine`」的判据。
3. **原版正文 fitter 还有 `heightMax: 200`（⇒ 面板上限 220），我们没落** ——
   我们只加了**下限** `MinH 110`：上限会让更长文案被压（原版是让正文自己 overflow / 缩字号），而我们的 `Label` 既不缩也不裁
   ⇒ 加个上限会把字画到底图外。⚠️ 要保持原版就得先有「正文超高怎么办」的落地（原版是 TMP 的 `m_overflowMode`/自适应），**属另一件活**。
4. **`LineH 30` 是折中值**（已标 🔶）：原版两个面的行盒差 9.5 px（Pragati 26.5 / CJK 36），
   而我们的 `Label` **按语种换面**、却只有一个 `LineH` ⇒ 单一常数必然两头不准。要修得先定「行高跟面走」，属结构活。
5. **同名 4 个变体参数各不相同**，本批只钉 **`BasicToolTip`**（= 我们基础版式的判据源；文件头 `:12-15` 已列）：
   `Battle Tooltip Variant`（fs34 / 正文宽 333 / pad 上下 0）· `Updated Tooltip Variant`（fs30 / 根 413.27×146.06 / pad 左右 20）·
   `Trait Tooltip`（fs32+30，我们已有独立的 `LayoutTrait` 一档，参数另钉在 `:158-165`）⇒ ⛔ 别拿别的那份来对基础版式。
6. **可见变化提醒**：单行 tooltip 面板高 **58 → 110 px**（宽也变成恒 500）⇒ 真机上会明显「更厚」。
   这是**照原版**（原版单行也是 110），但属于肉眼可见改动 ⇒ 建议进一次真 Play 时顺手看一眼。

---

## 六、验证读数

```
$ cd /d/4/Unity && TMPDIR=/tmp/wf_b141_w19 bash 工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

* **行数变化**：`Core/Tooltip.cs` **1353 → 1353 行**（`git diff --numstat` = `10 10`，**零漂移**，账闸不需要重钉）；
  新增 `Editor/B141_W19_Assert.cs`（新文件，不在账的落点里）。
* **行尾**：两个文件都是 **纯 LF**（`b.count(b'\r\n') == 0`，`b.count(b'\r') - b.count(b'\r\n') == 0`）。
* **未跑**：Unity 宿主（断言尚未接线）、真机。⇒ `C` 段那 10 条**还没被执行过**（接线由调度台在同步点做）。
