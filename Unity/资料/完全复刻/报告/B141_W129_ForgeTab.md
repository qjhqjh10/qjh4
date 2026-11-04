# B141_W7 · W129 · 账 4-09 锻造阵营条内容高与底内边距

> 执行写手：`B141-W7`（第一百二十八会话 · 2026-11-04）· 批次 `B141` · 波次 `W129`
> 白名单内改的文件：`MyGame/Assets/CardPresentation/Shell/ForgeTab.cs`（改）·
> `MyGame/Assets/CardPresentation/Editor/B141_W7_Assert.cs`（新建）· 本报告（新建）。

---

## 一、结论（一句话）

锻造阵营条 `Army Content` 已按原版 prefab **内容高 126 · pad bottom 5** 复刻完成（改前 **125.1 · 0**），
条目中心因此**上移 2.5px**；横向内容宽与阵营条的可滚动极值**逐位未变**。
`ForgeTab.cs` **行数零变化**（1503 → 1503），账的既有 `文件:行号` 落点全部不漂移。
`typecheck` **0/0**（运行时 0 · 编辑器 0）。

---

## 二、判据：原版值 vs 改后值

| 项 | 原版值（判据） | 改前（我们） | 改后（我们） |
|---|---|---|---|
| `Army Content` 内容高 | **126**（`m_SizeDelta = (0,126)`） | 125.1（= 直接拿选择条 `_selR` 当容器） | **126** |
| 布局组底内边距 | **5**（`HorizontalLayoutGroup.m_Padding.m_Bottom`） | **0**（一处 pad 都没吃） | **5** |
| 内容顶边 | = 选择条顶边（`a=(.5,1)` · `pos=(0,−63)` = −126/2 · pivot 居中） | = 选择条顶边（巧合，因容器就是 `_selR`） | = 选择条顶边 |
| 条目中心相对内容中心 | **上移 2.5px**（在「126−5=121」那一格里 `MiddleCenter`） | 0（居中于 125.1） | **上移 2.5px** |
| 内容宽 | 1604.68（13×136.36 + 12×(−14)） | 1604.68 | 1604.68（未动） |
| 阵营条滚动极值 | ±265.16（= 内容两端 − 视口两端） | ±265.16 | ±265.16（未动） |

### 判据出处（两条独立读口，逐字现读）

1. 🥇 `d:/2/tools/assetripper_out/ExportedProject/Assets/Forge Tab.prefab` 的 `Army Content` 段：
   - `RectTransform`：`m_AnchorMin = m_AnchorMax = (0.5, 1)` · `m_Pivot = (0.5, 0.5)` ·
     `m_AnchoredPosition = (0, -63)` · **`m_SizeDelta = (0, 126)`**
   - `HorizontalLayoutGroup`：`m_Padding = {L 0, R 0, T 0, **B 5**}` · `m_ChildAlignment = 4`（MiddleCenter）·
     `m_Spacing = -14` · `ctrlW/ctrlH/expandW/expandH` 全 0
   - `ContentSizeFitter`：`m_HorizontalFit = 2`（PreferredSize）· `m_VerticalFit = 0`
2. 🥈 `python -I -X utf8 工具/menu_dump.py bundle_menus_assets_all "Forge Tab" --depth 8`：
   `Army Content  0.00×126.00  HorizontalLayoutGroup,ToggleGroup,ContentSizeFitter  spacing=-14.0 align=4 pad=0,0,0,5 ctrlW=0 ctrlH=0 …`
   （同一次输出里 `Forge Army Selector` = `1074.36 × 125.10` —— 两件**高度不同**，不是同一个数。）

---

## 三、🔴 订正（铁律 5）：审计写的「内容高 130」是**别页的值**

- `判据/B141_W129_对账_UI动画.md` 的 W8 包 与 `报告/B138_审计汇总_问题清单.md` 的 `4-09` 都写
  「原版 `内容高 130` · padB 5，我们 125.1 · 0」。**padB 5 对，130 不对。**
- 错因：`assets_full/bundle_menus_assets_all` 里 `m_Name = Army Content` 的 **GameObject 有 15 个**，
  它们的五元组至少三种：
  - **Forge 这一族 = 3 个**，全部 `a=(0.5,1)` · `pos=(0,−63)` · **`sz=(0,126)`**
    （`Army Content_4914115240102170269` / `_6906597080686486583` / `_6959420616810567423`；
    与 `Forge Tab.prefab` 逐字同值 ⇒ 这就是锻造页那一份）
  - **`sz=(0,130)` 的 6 个**是别的页/别的版式（其中 `(0.5,0.5)` · `pos=(6.1e-05,0)` · `sz=(0,130)`
    那一组正是本文件旧注释里抄的 `… 6.1e-05,0, 0,130`）—— 审计很可能取了这一组。
  - 另有 `sz=(0,0)` 的拉伸锚点若干（收藏/其它窗的 `Army Content`）。
- ⇒ 本件按 **126** 复刻，并在 `Shell/ForgeTab.cs` 的就地注释里把旧的「`… 0,130`」改成
  「`a=(.5,1)` · `p=(.5,.5)` · `pos=(0,−63)` · `sz=(0,126)`」。
- ⛔ **别按 130 改回去** —— 那会把别页的值搬到锻造页，且会让条目中心再多偏 2px。
- ⚠️ 两份审计文件的正文**不在本写手白名单**内，**未改**；请调度台就地订正（把 `130` 改成 `126`，
  并注明「130 = 别页同名实例」）。

---

## 四、改动（`ForgeTab.cs`，**行数零变化 1503 → 1503**）

| 位置（现读行号） | 改动 |
|---|---|
| `:141-143` | 常量行追加 `ArmyContentH = 126f, ArmyPadBottom = 5f`（同一行，⛔ 不新增行）+ 打 `✅ 已复刻（2026-11-04 · B141 · 判据 …）`；`<summary>` 第二行改成「内容高 126」 |
| `:480-485` | 内容盒 = `new PxRect(_selR.x1, _selR.y1, _selR.x2, _selR.y1 + ArmyContentH)` ⇒ `_armyContentR` 高 126 且**顶边与选择条顶边重合**；注释按原版真实五元组重写 |
| `:750-752` | 条目摆放改用「扣掉 pad bottom 5 之后」的那一格：`new PxRect(_armyContentR.x1, _armyContentR.y1, _armyContentR.x2, _armyContentR.y2 - ArmyPadBottom)` ⇒ `HorizontalChild` 在「高−5 = 121」里居中（条目上移 2.5px） |

**为什么没动共用件**：`Editor/UguiRect.cs`（`UguiLayout.HorizontalContentCentered` / `HorizontalChild`）
签名里没有上下 pad，审计也提过「可能要顺带加到共用件」。本件**没有**改它 —— 只在锻造页**本地**扣掉
pad（一行矩形构造），理由：① 该共用件还被 `CampaignTab` 用，且它的注释明确写「与 `HorizontalContentCentered`
用同一张表」；② 加参数会波及另一页与账里对它的引用；③ 本件的语义（`MiddleCenter` + 单边 pad）
用一个「有效矩形」就精确等价（UGUI `GetStartOffset` 的 cross-axis 档推导见下）。

> 等价性推导（供后来人核）：`HorizontalOrVerticalLayoutGroup` 的 cross 轴，`Middle` 档 ⇒
> `startOffset = padding.top + (rectH − padding.top − padding.bottom − childH) × 0.5`，
> 而 `HorizontalChild` 的 `top = container.CY − childH/2`。
> 让 `container = [rect.y1, rect.y2 − padBottom]` ⇒ `CY = rect.y1 + (rectH − padB)/2` ⇒
> `top = rect.y1 + (rectH − padB − childH)/2`，与 UGUI 逐项同值（`padTop = 0`）。✅

---

## 五、断言（新建 `Editor/B141_W7_Assert.cs` · 共 **16 条**，其中前提 7 条 / 结论 9 条）

入口两条：`Run()`（自己建空场景 + 相机 + `WindowsManager.EnsureHost()` + `RewardsWindow.Create` + `OpenWindow`，跑完拆窗）
· `RunForge(RewardsWindow win)`（宿主已有窗时用；`null` 则自建）。

| # | 断言 | 灭自证（🧨 什么改动会让它红） |
|---|---|---|
| P1-P5 | 窗 / `ForgeTab` / 切到锻造页 / 三件节点 / 都是 `RectTransform` | 前提不成立时**显式红**（⛔ 不 `if` 跳过） |
| A1 | 选择条高 = 125.1 | 没有它，「内容高 126」与「拿选择条高当内容高」分不清 |
| **A2** | `Army Content` 内容高 = **126**（读 `sizeDelta`，容差 0.2） | 改回 `HorizontalContentCentered(_selR, …)`（旧写法 125.1）⇒ 红 |
| **A3** | 内容**顶边 = 选择条顶边**（容差 0.1） | 内容盒写成「居中于选择条」（`CY ± 63`）⇒ 顶边低 0.45px ⇒ 红 |
| P6 | 至少建出 1 条阵营格 | —— |
| P7 | 条目高 = 121.59 | 量纲前提 |
| **A4** | **pad bottom = 5**：`padB = 2 ×（内容中心 y − 条目中心 y）`，容差 0.3 | 去掉 `_armyContentR.y2 − ArmyPadBottom`（pad 回 0）⇒ Δ=0 ⇒ 红 |
| **A5** | 结构性：`Δ > 1.5px`（实测 ≈2.5） | 与 A4 互为判别式：「高度对但 pad=0」A2 绿、A4/A5 红；「pad 对但高 125.1」A4/A5 绿、A2 红 —— 两条路都堵死 |
| **A6** | （回归）内容宽 = 1604.68 | 改 pad 时误动横向 ⇒ 红 |
| P8 | `ForgeTab.ArmyScroll != null` | —— |
| **A7/A8** | （回归）滚动极值 = ±265.16 | 同上：可滚动区域几何必须**逐位不变**（`MenuScroll` 只吃 x） |

⚠️ **A4/A5 不读实现里的常量**（不读 `ArmyPadBottom`）—— 量的是**几何后果**，避免「拿我们的常量证明我们的常量」。
⚠️ 搜索 `Army Content` **必须从 `Forge Tab` 子树起** —— 战役页（`Campaign Tab`）里也有一个同名节点（本件已这样做）。

**怎么跑**（⛔ 执行代理不跑 Unity，以下留给调度台）：

```bash
unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod B141_W7_Assert.Run -logFile "d:/4/_tmp_view/b141_w7.log"
# 判绿 = 合计行 `✗ 0`（按行首 `[B141W7] ✗` 数，⛔ 别 grep -c ✗）
```

并进 `Editor/RewardsScene.cs`（**该宿主不在本件白名单，未接线**）：

```csharp
B141_W7_Assert.RunForge(win);
pass += B141_W7_Assert.Pass; fail += B141_W7_Assert.Fail;
```

---

## 六、验证

| 项 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_b141_w7 bash 工具/typecheck.sh` | **运行时 0 · 编辑器 0** |
| `ForgeTab.cs` 行数 | **1503 → 1503**（零漂移；`git diff --numstat` = 10/10，只有那三处就地改动） |
| 行尾 | 纯 LF，`bareCR 0`（未翻行尾） |
| Unity 自检 / 实拍 | **未跑**（执行代理红线）—— `B141_W7_Assert` 的 16 条待调度台跑 |

---

## 七、还差什么（交调度台）

1. **跑一次 `B141_W7_Assert.Run`**（或接进 `RewardsScene` 后随该宿主跑），确认 16 条全绿。
2. **就地订正两份审计**（不在本件白名单）：`判据/B141_W129_对账_UI动画.md` 的 W8 行 与
   `报告/B138_审计汇总_问题清单.md` 的 `4-09` 行，把「内容高 130」改成 **126**，并注明 130 = 别页同名实例。
3. **活状态表核销**：`4-09` 这一行由调度台改状态（本件不动账 / 不动活状态表）。
