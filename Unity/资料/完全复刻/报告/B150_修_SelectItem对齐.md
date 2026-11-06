# B150 修 · `Select Item` 左对齐差 28.17px（Menu 宿主最后一条产品侧失败）

> 日期：2026-11-06 · 执行代理 · 白名单：`Shell/TitleTab.cs`（未改）· `Battle/Label.cs`（**改的就是它**）· `Editor/MainMenuScene.cs`（未改）

## 一、根因（判据链完整，数字分毫不差）

### 1·1 现场两把尺子

| 量 | 值 | 含义 |
|---|---|---|
| `Label.WorldW × 108` | **165.22 px** | `AlignLeftOn` 摆位时用的宽（`_tmpW` 缓存） |
| TMP 实渲出来的宽 | **221.56 px** | 断言量到的那块 `textBounds` |
| 比值 | `165.22 / 221.56 = 0.74571` | |
| `basePx / maxPx`（原版 `auto[18~35]` 基准 **26.1**） | `26.1 / 35 = 0.74571` | **逐位相同** |
| 偏移 | `(221.56 − 165.22) / 2 = 28.17 px` | = 断言差的那个数 |

⇒ 对齐那一刻量到的是 **base 26.1px 那一档**的宽，实渲的是 **max 35px 那一档**的宽。两条都不是猜的：`26.1` 与 `35` 就是 `TitleTab.cs:256-258` 传给 `ProfilePage.Text` 的 `basePx` / `autoMaxPx`（原版 `menu_dump … "Player Profile Window"` 的 `Select Item` = `auto[18.0~35.0] 基准=26.1`）。

### 1·2 为什么「量到的是 base」

`Shell/PlayerProfileWindow.cs:502` —— `page.gameObject.SetActive(i == DefaultTabIndex);` ⇒ **建 `TitleTab` 时那一页是未激活的**（默认页是 Avatar）。于是走的是 Label 的「未激活档」：

1. `SetWrapWidth` → 欠下 `_pendWrapW`；
2. `SetAutoFitBox` → `_tmp.ForceMeshUpdate()` **在未激活时是空操作**（`IsActive()` 闸）；
3. `MenuDraw.AlignLeft` → `HasMeasuredWidth()` 为假 ⇒ 记 `_pendAlignX`（这一档本身是对的）；
4. 有人读 `WorldW` / 走到 `RefreshBounds` ⇒ `EnsureMeasured` → **`PushLayoutInactive()`** 把版面排出来 ⇒ `_tmpW = 165.22`；
5. `RefreshBounds` 尾句 `TryApplyPendingAlign()` 用 **165.22** 把左沿钉在 654.16 ⇒ 节点世界 x = `654.16 + 165.22/2 = 736.77`（日志就是这个数）；
6. 页面激活后 TMP 自己收敛到 **max**（实渲 221.56）⇒ 左沿变成 `736.77 − 110.78 = 625.99`（日志也是这个数），**而 `_tmpW` 没人再量、也没人再对齐**。

### 1·3 为什么未激活档只能量到 base（TMP 源码判据）

`PushLayoutInactive` 走 `TmpFont.GenerateLayout` → `TMP_Text.GetTextInfo(string)`（`TextMeshProUGUI.cs:574-591`）：`SetText + SetArraySizes + ComputeMarginSize + GenerateTextMesh`，**单趟**。

自适应要「长到装得下的最大号」靠的是 `GenerateTextMesh` 末尾那段二分：

```
fontSizeDelta = m_maxFontSize - m_minFontSize;
if (m_enableAutoSizing && fontSizeDelta > 0.051f && m_fontSize < m_fontSizeMax && …) { …; return; }
```
（`TextMeshProUGUI.cs:4487-4505`）

而 `m_maxFontSize / m_minFontSize` **只在 `OnPreRenderCanvas` 的收敛循环之前**被设成 `[fontSizeMax, fontSizeMin]`（`TextMeshProUGUI.cs:2499-2504`）——`GetTextInfo` **不经过那一步**。两个字段是 `protected float`、**类字段初值 0**（`TMP_Text.cs:521-522`）⇒ `fontSizeDelta = 0 − 0 = 0` ⇒ **增长分支永不触发** ⇒ 每一遍都停在 `m_fontSizeBase`（= 我们刚写进去的 base）⇒ 版面「漂亮、完整、量得到」，但**不是最后一版**。

原 `PushLayoutInactive` 的判据是「`textBounds` 是**测量值**就收工」—— 而二分第一遍本来就装得下、给得出完整 `textBounds` ⇒ 它在这儿**提前收工**。

## 二、改了哪个文件哪一行

**只改 `Assets/CardPresentation/Battle/Label.cs`**（`Shell/TitleTab.cs`、`Editor/MainMenuScene.cs` 一个字未动）。

| # | 位置（改后行号） | 改动 |
|---|---|---|
| ① | `Label.cs:730-735`（`PushLayoutInactive` 的循环体） | 新增收敛判据：`float f0 = _tmp.fontSize` → 排一遍 → **`ok && Mathf.Abs(fontSize − f0) < 5e-3` 才算收敛**（原来 `ok` 就 `return true`） |
| ② | `Label.cs:727` | 进循环前调 `SyncAutoSizeBoundsForInactive();` —— 把 TMP 的二分区间补成 `[fontSizeMin, fontSizeMax]` |
| ③ | `Label.cs:3017-3065`（**类尾新增**） | `SyncAutoSizeBoundsForInactive()`（`:3035`）+ `FindAutoSizeFlds()`（`:3057`，反射 `m_maxFontSize`/`m_minFontSize`，同 `BaseFld` 的写法）+ 两个静态 `FieldInfo`（`:3027`）+ 一条「取不到字段」的出声 |
| ④ | `Label.cs:699-700`、`712-716` | 就地订正两段 doc（原文写「有真 `textBounds` 就说明收敛了」，那正是这条 bug 的错误前提） |

### 为什么是这个修法（而不是改期望 / 改 TitleTab）

- `654.16` 有原版判据（`menu_dump` 的 `Select Item` = `654.16,147.51 → 1166.61,210.70`，`Left/Middle`，折行=1）⇒ **期望值一个字没动**。
- 症状在 `TitleTab`，病灶在**共用件的「未激活测量」**：任何未激活 + 自适应的标签都会「量到 base、渲成 max」。改 `TitleTab` 只是绕开（而且绕不干净：对齐发生在 `_pendAlignX` 兑现那一刻，那时宽度已经是错的了）。
- 修法 = 让「未激活量到的宽」**等于「激活后实渲的宽」**（这正是 TMP 自己 `OnPreRenderCanvas` 会做的事，我们只是把同一套二分补上）。收敛判据用「字号不再变」，与 TMP 内部的 `fontSizeDelta ≤ 0.051` 停在同一个不动点 ⇒ 同一标签两条路给出**同一个字号**。

### 行数

- 目标区（`PushLayoutInactive` 及其 doc）**净 0 行**：doc 6→5（−1）、循环 +1。
- 新增代码**全部追加在类尾**（`Label.cs` 的最后那个 `}` 之前，单类文件已核）⇒ **`728` 以下的行号一个没动**。
  ✅ 已逐行复核账引用的 12 个落点（`Label.cs:263 · 367 · 1108 · 1117 · 1293 · 2460 · 2474 · 2495 · 2577 · 2593 · 2626 · 2696`）内容与行号**全部未变**。
- 文件 3017 → **3077** 行（+60，全是类尾新增 + 两处「同一物理行追加」）；行尾 **CRLF 3077 / LF 3077 / 裸 CR 0**。

## 三、typecheck

```
--- 运行时程序集 ---  运行时错误数: 0
--- 编辑器程序集 ---  编辑器错误数: 0
```
（`bash Unity/工具/typecheck.sh`）

## 四、未验的部分（如实）

- **没跑 Unity**（按简报）⇒ 红→绿**没有实跑确认**。因果链是按 TMP 源码 + 三个数字的恒等式钉的（`165.22/221.56 = 26.1/35` 精确到小数点后 5 位、`(221.56−165.22)/2 = 28.17` 精确），但**必须在下一次 Unity 窗口用 `MainMenuScene.Run` 复验**。
- 复验时看两点：① 那条断言转绿；② 诊断行里的 `Label.WorldW×108` 应该从 **165.22** 变成 **≈221.56**（两把尺子一致才是真修好）。

## 五、⚠️ 波及面（**第二轮已收窄**，见 §七）

第一版是**对所有「未激活 + autoFit」标签**放开收敛 ⇒ 带出 `BattleScene` 3 条新红；第二轮已把收敛**收窄到「落过 `Align*On` 的标签」**（判据见 §七）。
现状：
- **未激活 + autoFit + 落过对齐**：量到**收敛后的字号**（这正是 `Select Item` 那一族，也是唯一会「拿量到的宽去摆位」的一族）。
- **未激活 + autoFit + 没落过对齐**：**一个字不动**（与改前逐位相同）—— `WorldW` 只是诊断/断言读数，按 base 量是既有行为。
- 已激活的路径**一个字节没动**（`PushLayoutInactive` 只在 `!_tmp.isActiveAndEnabled` 时被调）。

## 六、顺手发现

1. **简报里的「可疑点」已经过期**：`Shell/TitleTab.cs:256-258` **已经**传了 `autoMaxPx: 35f, basePx: 26.1f`，与 `Shell/AvatarTab.cs:308-310` 逐位相同 ⇒ 不需要补参数。这条不是本 bug 的成因（成因此见 §1）。
2. `Label.EnsureMeasured`（`:694`）的**已激活**分支仍然只跑一遍 `TmpFont.GenerateLayout` —— 今天安全（激活对象上 TMP 自己已经收敛过，单跑一遍是在同一字号上重排），但语义上仍是「单趟」。若将来出现「激活了但还没渲过」的测量路径，会复发同一类 bug。本波**没动它**（不在本 bug 的因果链上，改它属于另一个 blast radius）。
3. `Battle/Label.cs` 工作树里本来就有别人未提交的一处改动（`RefreshBounds` 的「两轴都 ≈0」零宽闸，2026-11-05 `B150`）—— 本波**没碰它**，`git diff` 里能看到，合并时别当成我的改动。
4. `PushLayoutInactive` 原 doc 写「每次 early-return 都会把 `m_fontSize` 挪一步」——**这句在未激活档不成立**（区间是 0，压根不会 early-return 到增长分支）。已就地订正（铁律 5）。

## 七、第二轮：为什么第一版会让「自适应看起来不压」（3 条新红的成因与处置）

### 7·1 现场

`d:/4/_tmp_view/b154_BattleScene.log`：

| 断言 | 改前 | 第一版之后 |
|---|---|---|
| `B97W41` A7「长数字实收字号 < 40」 | 36.00 ✓ | **40.00 ✗** |
| `B97W41` A9「短 ≥ 长」 | 36 / 36 ✓ | 36 / **40** ✗ |
| `B98A2` D6「五位数 < 40」 | 36.00 ✓ | **40.00 ✗** |
| A5/A6（宽/高不溢出）、A8（≤ 40）、A1–A4（三档字段） | ✓ | ✓（未变） |

### 7·2 成因（就是你猜的那条，已证实）

- 这三颗 `Quantity` 标签来自 `EndPanel.Create(null)`（`B97W41_Assert.cs:80`）⇒ **建在面板里、未激活** ⇒ 走的是 `PushLayoutInactive` 那条单趟路。
- **改前**：`m_maxFontSize/m_minFontSize` 都是 0 ⇒ 增长分支永不触发 ⇒ 量到/读到 **base 36**（A7 的「< 40」、A9 的「短 ≥ 长」**正是靠这个 base 读数成立的** —— A9 的注释自己就写着「本档实测两者都 = base 36」）。
- **第一版之后**：我把区间补成 `[18,40]` ⇒ 增长分支被打开 ⇒ 收敛到 **40 = max** ⇒ 三条红。
- 顺带一条反证：这不代表「产品侧真的在压」——A11 的注释早就记着「把这颗 TMP 的 `fontSize` 钉到 40，`textBounds` 一点没变（8.7×4.7，与 36 那一档相同）」，也就是说这一档的「压」在本环境里从来没被真正验证过。

### 7·3 处置（**不回退 `Select Item`，也不动那三条断言**）

把「未激活档的自适应收敛」**收窄到「这一族标签量到的宽会被拿去摆位」**：

| 改动 | 位置 | 内容 |
|---|---|---|
| 新字段 `_alignEver` | `Label.cs:3030-3033`（类尾） | 本标签**被 `AlignLeftOn`/`AlignRightOn` 落过**（一旦为真不复位） |
| 打标 | `Label.cs:1756`（`AlignLeftOn`）/ `:1780`（`AlignRightOn`） | `_alignEver = true; RefreshBounds();`（**同一物理行改写**，零行数变化；⚠️ **打标必须在 `RefreshBounds` 之前** —— 那一句自己就可能触发未激活档的测量） |
| 闸门 | `Label.cs:3046` | `if (!_alignEver) return false;` —— 没落过对齐的标签**一个字都不动** |

为什么这样就够：`AlignLeftOn`/`AlignRightOn` 是**唯一**会「读 `WorldW` 再把节点挪到边上」的口 ⇒ 只有这一族的测量宽度会变成**画面上的位置误差**；其余标签的 `WorldW` 只是诊断/断言读数，按 base 量是**改前**的既有行为，没有理由在这一波里顺带改。
`Select Item` 这条链上 `MenuDraw.AlignLeft` 在 `TitleTab.Build` 里就跑过（`_alignEver` 在 `PushLayoutInactive` **之前**就已经是 true）⇒ 收敛照旧生效，§1 的修法**不受影响**。

### 7·4 复核

- `typecheck`：运行时 **0** / 编辑器 **0**。
- 行数：`Label.cs` 3017 → **3077**；`728` 以下**行号未变**，账引用 12 个落点（`:263 · 367 · 1108 · 1117 · 1293 · 2460 · 2474 · 2495 · 2577 · 2593 · 2626 · 2696`）**内容与行号逐条复核未变**。
- ⚠️ **仍需跑一次 `BattleScene.Run` + `MainMenuScene.Run`**：`BattleScene` 那 3 条应回到改前的 36 读数（绿），`Menu` 的 `Select Item` 应保持绿。
