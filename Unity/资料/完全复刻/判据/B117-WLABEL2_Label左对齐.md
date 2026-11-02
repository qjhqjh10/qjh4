# B117-29 · uGUI 标签「左对齐 + 量不到尺寸」时左移半个框宽 —— 落地报告（`B117-WLABEL2`，2026-11-02）

> 结论：**修好了，只动 7 处，另 3 处按判据不接。** `typecheck 0 / 0`；新增断言 **25 条**（`Editor/B117_WLABEL2_Assert.cs`，未接宿主）。

---

## 一、根因（复述判据，⛔ 未重新查）

- `TmpFont.NewTextUgui` 给 TMP 子节点的枢轴是出厂 `(0.5, 0.5)`，`Label.anchor` 只在 `RefreshBounds` 的算式里被用到。
- `Label.RefreshBounds`（`Battle/Label.cs:1677`）只在**量到** `textBounds` 时才把子节点摆到 `-anchor.x*W - b.min.x`；
  空文本 / 未激活（`GetTextBounds` 只统计 `isVisible` 的字 ⇒ 空集哨兵 ±4.29e9）时它在 `:1663` 早退。
- ⇒ 子节点停在 `(0,0)`，矩形按「枢轴 + 对齐」几何排：**枢轴居中 ⇒ 矩形左沿 = −框宽/2**，而左锚（`anchor.x = 0`）
  的语义是「文字块左缘贴节点 x」⇒ **整块从框左沿外半个框宽起笔**（真机实证 = `Battle/UnitChatPanel.cs:711` 那一串注）。
- ⛔ `Label.cs:204` 的 `rt.pivot` 是 **Label 节点自己**的枢轴，该节点 `sizeDelta = Vector2.zero`（`:205`），
  全仓没有任何代码读它的 pivot ⇒ 改那里**一处也修不好**（本波没动那一行）。

---

## 二、新入口

**`Label.AlignTmpPivotToAnchor()`** —— `Unity/MyGame/Assets/CardPresentation/Battle/Label.cs` **类尾**
（原 `2759` 行 → 现 `2795` 行，**+36 行全部追加在类尾**，之前所有行号零漂移）。

```csharp
public void AlignTmpPivotToAnchor()
{
    if (_tmp == null) { NoteDotBackendLacks("左缘枢轴", …); return; }   // 点阵后端出声，不静默
    var rt = _tmp.rectTransform;
    if (rt != null) rt.pivot = new Vector2(anchor.x, anchor.y);
    if (anchor.x <= 0f) SetAlignLeft();                                  // 复用「逐行左对齐」的唯一实现
    else if (anchor.x >= 1f) _tmp.alignment = TextAlignmentOptions.Right;
}
```

三件事，缺一不可：

1. **枢轴 = `anchor`**（只动 TMP **子节点**，不碰 Label 节点）。
2. **横向对齐必须同侧** —— 只改枢轴不改对齐，量不到尺寸时文字在矩形内**居中**，仍偏半个**字**宽；
   只改对齐不改枢轴，矩形还在框心，偏半个**框**宽。两个一起才是「文字起点 = 框左沿」。
3. **量得到的路径逐位不变**（已在纸面推过，见 §五 的断言 B1/B2/B3）：
   `textBounds` 是相对枢轴的，`localPosition = -anchor.x*W - b.min.x` ⇒ `localPosition + b.min.x ≡ -anchor.x*W`，
   **与枢轴、与对齐方式都无关**。
   - 例：框宽 200 / 字宽 50，`anchor.x = 0`：
     · 旧（pivot 0.5 + Center）：rect `[-100,100]`、`b.min.x = -25` ⇒ `localPosition = 25` ⇒ 词块左沿 `= 0`；
     · 新（pivot 0 + Left）：rect `[0,200]`、`b.min.x = 0` ⇒ `localPosition = 0` ⇒ 词块左沿 `= 0`。**同一处。**
4. 居中锚（`anchor.x = 0.5`）是**零变化**：pivot 本来就 0.5、对齐本来 Center ⇒ 入口对该档什么都不写
   （⛔ 不是「无条件钉 anchor + 无条件 SetAlignLeft」—— 那会把居中站推歪，断言 `C1/C3` 咬这一条）。

---

## 三、逐站点（7 处接线 · 全部「同一物理行追加」= **行数守恒**）

| # | 文件:行 | 标签 | 原版语义 | 该行改动 |
|---|---|---|---|---|
| 1 | `Battle/SettingsPanel.cs:1173` | `settings_autozoom_label`（`_azLabel`） | Left/Middle | `if (_azLabel != null) { SetAlignLeft(); AlignTmpPivotToAnchor(); }` |
| 2 | `Battle/SettingsPanel.cs:1550` | `settings_lang_caption`（`_langCap`） | Left/Middle | 同上（`_langCap`） |
| 3 | `Battle/SettingsPanel.cs:1571` | `settings_lang_label`（`_langSelText`） | Left/Middle | 同上（`_langSelText`） |
| 4 | `Battle/SettingsPanel.cs:1807` | 语言下拉 `Item Label`（`lb`） | Left/Middle | 同上（`lb`） |
| 5 | `Battle/SettingsPanel.cs:2177` | `settings_mute_label`（`_muteLabel`） | Left/Middle | 同上（`_muteLabel`） |
| 6 | `Battle/SettingsPanel.cs:2304` | `settings_slider_label_<i>`（`l`，三根滑块说明） | Left/**Bottom**（纵向走 `SetVAlign`，不归 TMP 对齐管） | 同上（`l`） |
| 7 | `Shell/AlliancePanelWindow.cs:1312` | `Txt()` 的画布档（7 颗字共用这一条路） | Left/Middle | 追加 `if (_ugui) lb.AlignTmpPivotToAnchor();`（**只在画布档**） |

- 这 7 处的 `anchor` 全是 `new Vector2(0f, 0.5f)`（`grep` 现读：`SettingsPanel.cs` 里即此 6 行）。
- 调用位置：`SettingsPanel` 的 6 处都排在 `ApplyLangFont` / `SetAutoFitBox` **之前**
  （`SetAlignLeft` 的 doc 要求「定版面之前调」，且不会给 `AlignAfterLayoutCount` 记账）。
  `AlliancePanelWindow` 那一处只能排在 `SetAutoFitBox` 之后（它在 `Txt()` 的末尾）——
  与那里**原有**的 `lb.SetAlignLeft()` 同拍，**没有新增「过晚调用」语义**，只是计数器多记 1（只影响该窗自己，见 §五 D）。
- **行数守恒已验证**：`SettingsPanel.cs` **2906 行 → 2906 行**、`AlliancePanelWindow.cs` **1494 → 1494**
  （`io.open(...,'rb')` 现读；行尾也没翻：两个文件都是纯 LF）。

### 为什么跳过那 3 处「先建在 0、后摆」

| 站点 | 形状 | 不接的理由 |
|---|---|---|
| `Battle/ChatPopupPanel.cs:471` | 建在 `(0,0)`（`anchor=(0,0.5)`），随后由调用方按弹窗位置摆 | 节点坐标**建完就改** ⇒ 加了会把既有摆位挪走（`anchor` 只是「块内对齐」的参照，不是最终位置） |
| `Battle/UnitChatPanel.cs:706` | 同上；且 `:711` 已有 2026-11-02 的**手写修法**（把 TMP 子节点 pivot 钉成 `(0,0.5)`）+ `:1097` 先 `SetAlignLeft()` 再 `SetText()` | **已经在做同一件事**，重复接会二次改 pivot / 二次记账 |
| `Battle/BattleLogPanel.cs:923` | 建在 `(0,0)` 后摆 | 同第一条 |

⇒ 这三处的**行为完全没动**（源码级 0 调用点，断言 `D3/D4/D5` 咬这一条）。

---

## 四、验证

- `TMPDIR=/tmp/wf_wlabel2 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
  - ⚠️ **第一遍跑出 5 条错，全部在 `Shell/RankedArmyBonusCard.cs:240/273`（`_clip` / `MaskPadding` 未定义）
    + 1 条 `CS0006`（旧 `WFCheck.dll`）** —— 那是**别人的在飞文件**（`CLAUDE.md` 那条「错误全集中在不是你负责的文件上 ⇒ 不是你的问题」）。
    复跑一遍即 `0 / 0`，**我没有动那个文件**。
- ⛔ 未跑 Unity 批处理（红线）；新断言**未接宿主**（红线：不许改 `Editor/*Scene.cs`），接法写在文件头。
- `git diff --numstat` 对过一眼：`Label.cs` **+36 / −0**（正是类尾那一段）；
  `SettingsPanel.cs` / `AlliancePanelWindow.cs` 的 numstat **不可用**（工作树本来就有大量他人未提交改动，`CLAUDE.md` 那条），
  改用「行数 + 行尾计数」核对（见 §三 末）。

---

## 五、断言（`Assets/CardPresentation/Editor/B117_WLABEL2_Assert.cs`，**25 条**）

| 组 | 条数 | 断什么 | 改哪一处会让它红 |
|---|---|---|---|
| A（空文本 + 左锚） | 10 | A0 前提建起来了 · A1/A1b 出厂 pivot=0.5 · A2 框宽=200 · **A4 改造前左沿 = −框宽/2** · A5/A5b 改造后 pivot = anchor · **A6 改造后左沿 = 0** · A7 对齐 = Left | 入口空实现 ⇒ A6 红；只改枢轴不改对齐 ⇒ A6 偏半个字宽；只改对齐 ⇒ A6 偏半个框宽；删掉 `SetAlignLeft` 那行 ⇒ A7 红 |
| B（有文本） | 5 | B01 前提 · B0 两件都量到了 · B1/B2 新旧写法的字墨左沿都 = 0 · B3 两者差 = 0 | 删掉 `RefreshBounds` 的 `-b.min.x` ⇒ B1/B2 同时红 |
| C（居中锚） | 5 | C01 前提 · C0/C3 对齐仍是 Center · C1/C2 枢轴仍是 0.5 | 入口改成「无条件钉 anchor / 无条件 SetAlignLeft」⇒ C1/C3 红 |
| D（接线面） | 5 | 源码里 `.AlignTmpPivotToAnchor(` 的调用点数：`SettingsPanel` = 6 · `AlliancePanelWindow` = 1 · 那 3 处 = 0 | 接到那 3 处 ⇒ D3/D4/D5 红 |

**灭自证的关键是 A4 + A6 的「两态」**：A4 先证明「不接就是 −框宽/2」（缺陷本体可复现），A6 再证明「接了就是 0」。
只写 A6 一条没有鉴别力（谁把框宽写成 0 也能过）。

**边界（如实登记）**：`D*` 是**源码级**接线检查，只能证明「没接到那 3 处」，**证明不了**「那 3 处的摆位真的没变」
（后者要跑各自宿主看坐标）。点阵后端那一支本件**没有夹具**（它只出声、不摆位，那族归 `SettingsScene` 的 A491）。

---

## 六、可能被打破的地方（我判断的风险，按可能性排序）

1. **`AlliancePanelWindow.Txt()` 的画布档 7 颗字**（`Individual rating` 两颗走 `HorizontalLayoutGroup`）：
   它们的 `Txt()` 现在会把 TMP 子节点 pivot 钉成 `(0,0.5)`。
   - **量得到**时按 §二·3 逐位不变；
   - **量不到**时（窗未激活时 `Build`）它们从「框心起笔」变成「框左沿起笔」—— 那正是本次要修的方向，
     但如果原版这 7 颗**本来就是居中**（`Txt` 的注释写的是 `Left/Middle`、`:1312` 也一直在调 `SetAlignLeft`），
     那么这是**修对了**，不是打破。
   - ⚠️ 唯一要真机看的：`_ratingValues` 那两颗在**布局组跑完**之后的实际落点（`:817` 另外还把 **Label 节点**的 pivot
     钉成 `(0,0.5)`，那是给 `HorizontalLayoutGroup` 用的，**与本次改的 TMP 子节点不是同一层**，没有冲突）。
2. **`SettingsPanel` 的 6 颗**：`SetAlignLeft()` 是新加的（之前是 TMP 出厂 `Center`）。
   单行文字下「逐行对齐」看不出来（`SetAlignLeft` 的 doc 写明「只有真折行时才看得见」），
   所以**画面上应当只有本次要修的那半个框宽**。⚠️ 但 `_azLabel` / `_muteLabel` / `_langCap` 都开了 `SetAutoFitBox`（折行 = 1/0 不一），
   若某档真的折了行，短行会从「居中」变成「贴左」—— 那是**判据要求的那一档**（原版 `m_HorizontalAlignment = 1`），不是回归。
3. **`AlignAfterLayoutCount` 计数**：`AlliancePanelWindow` 那 7 颗字现在每次 `Txt()` 会多记 1
   （入口内部又调了一次 `SetAlignLeft`）。`Editor/SettingsScene.cs:3500-3519` 的 A546② 断言用的是**局部 label 的增量**
   （`c0` / `c1` 差值），**不受全局累计影响** ⇒ 判断是安全的；⛔ 但**别**把它改成「全局累计 == 某个数」那种写法。
4. **`SetVAlign(Bottom)` 那三颗滑块标签**：TMP 对齐由 `SetAlignLeft` 设成 `Left`（V 分量 = `Middle`），
   与出厂 `Center`（V 也是 `Middle`）**纵向同档**；纵向档位走 `Label._vTier` + `VOffsetWorldNow`，**不读 TMP 的 V 分量**
   ⇒ 纵向落点不受影响。
5. **未覆盖**：世界档（`Label.Create`，非 uGUI）**没接**这个入口 —— 那批标签不进 `SetAutoFitBox`、`sizeDelta` 恒 0，
   量不到尺寸时 `rect` 宽为 0，改 pivot 零收益。若将来有世界档标签也要框宽，需要单独一轮判据。

---

## 七、给调度台的两条

1. 新断言**未接宿主**（红线）。建议接进 `Editor/ShellScene.cs`（`AlliancePanelWindow` 同宿主）或 `Editor/SettingsScene.cs`，
   两者都会建 canvas；接法写在 `B117_WLABEL2_Assert.cs` 文件头（同一物理行、放在该行 `//` 之前）。
2. 账：本波动了 `Battle/Label.cs`（**类尾 +36 行，之前的行号零漂移**）· `Battle/SettingsPanel.cs`（**行数不变**）·
   `Shell/AlliancePanelWindow.cs`（**行数不变**） ⇒ **理论上前面的 A 行落点一条都不该漂**，但请照常跑一次
   `check_ledger_points` 复核（我不动主表）。
