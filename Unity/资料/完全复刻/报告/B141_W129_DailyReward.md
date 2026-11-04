# B141 · W129 · W6 —— `DailyRewardPopup` 两条 UI 参数

> 执行写手 `B141-W6` · 2026-11-04 · 批次 `B141` · 会话 第一百二十八。
> 输入 = `判据/B141_W129_对账_UI动画.md` 的 **W7 包**（条目 `4-08` / `4-16`）· `报告/B138_审计汇总_问题清单.md` §4。
> 改动文件 = `Shell/DailyRewardPopup.cs`（+ 新建 `Editor/B141_W6_Assert.cs`）。⛔ 未动宿主 `Editor/RewardsScene.cs` / 账 / 活状态表 / `d:/2`；未跑 Unity；未动 git。

---

## 结论一览

| 条目 | 判定 | 原版值 | 我们（修前） | 我们（修后） |
|---|---|---|---|---|
| **4-16** 阵营条视口 `RectMask2D` | ✅ **已修** | `m_Padding = (0,0,0,0)` · `m_Softness = (42,0)` | `(0,0,0,0)` · `(0,0)` | `(0,0,0,0)` · **`(42,0)`** |
| **4-08** 进度图 / 计数 y 区间 | ⛔ **判据本身错（本项目未改）** | `Image` y **358.76..446.76** · `Counter` 同 | `358.76..446.76` **已与原文一致** | 不动（照审计改反而制造 3.29px 偏离） |

---

## 一、4-16 · `Army Selector/Viewport` 的 `RectMask2D`（真改的那条）

### 原版判据（2026-11-04 实读）

`d:/2/tools/assetripper_out/ExportedProject/Assets/Daily Reward Popup.prefab` 里共 **2 颗** `RectMask2D`
（`m_Script fileID -146154839`），按父链区分：

| 节点（父链） | `m_Padding` | `m_Softness` |
|---|---|---|
| `Viewport / Army Selector / Daily Reward Popup` | `(0,0,0,0)` | **`(42,0)`** |
| `Viewport / Rewards Scroll View / Tracks / Daily Reward Popup` | `(0,0,0,0)` | `(0,0)` |

⇒ **只有阵营条这颗是 `(42,0)`**（左右各 42px 线性渐隐、上下硬边）；奖格轨那颗本来就是 `(0,0)`。
我们原来两处**都**写死 0 —— 阵营条那条是从奖格轨照抄过来的。

### 改法（`Shell/DailyRewardPopup.cs`）

- `BuildArmySelector`（现读 `:770-772`）把 4 行内联（`AddComponent<ViewportClip>` + `padding=zero` + `softness=zero` + `CaptureNow`）
  收口成一句：`var vc = ApplyArmyViewportClip(vpNode.gameObject);`
- 类尾新增两个原版值常量 + **唯一写口**：
  - `public const int ArmyViewportSoftX = 42, ArmyViewportSoftY = 0;`
  - `public static readonly Vector4 ArmyViewportPad = Vector4.zero;`
  - `public static ViewportClip ApplyArmyViewportClip(GameObject node)`（内含 `CaptureNow()`）
- 副作用核对：`CaptureNow()` 仍在**写矩形之后、建子件之前**（调用点紧跟 `MenuDraw.Node(sel, "Viewport", …)`）⇒ 基准矩形不变。
- 渲染侧不需要改：`ViewportClip.Resolve(parent, …)` 沿父链取节点状态，`MenuDraw` 的软边那一支会吃 `SoftnessPx`。

### ⚠️ 宿主断言

`Editor/RewardsScene.cs` 里**没有**钉 `DailyRewardPopup ▸ Army Selector/Viewport` 的 softness
（现读：`:5065-5090` 钉的是锻造页奖格轨 · `:13157-13237` 钉的是战役页阵营条）⇒ **本改动不需要同改宿主**。
（同文件 `:3940-3964` / `:5507` 的 `(42,0)` 那几条是 **ForgeTab** 的阵营条，与本条同源、不冲突。）

---

## 二、4-08 · 进度图 y —— ⛔ 判据本身错，本项目**未改**

### 审计的说法（`报告/B131_审计_UI版面与字体.md:250`，`B138` §4 转抄）

> `Personal Progression/Image`（锚 .5·size 68×88·**pos 0**；PP 高 100 居中于 812.1）⇒ 中心 **406.05** ⇒ y **362.05..450.05**

### 反证（两条独立源）

**① prefab 逐字段实读**（`Daily Reward Popup Entry.prefab` + `Daily Reward Popup.prefab`，**5 个实例同值**）：

| 节点 | `anchor` | `anchoredPosition` | `sizeDelta` | `pivot` | ⇒ y（y 向下 · 相对 Entry 顶） |
|---|---|---|---|---|---|
| `Personal Progression` | y `0.5`（竖向居中） | `(0, 0)` | `(0, 100)` | `(0.5, 0.5)` | 356.05 .. 456.05（中心 **406.05**） |
| `Image` | `(0.5, 0.5)` | `(-0.000024, **+3.2939**)` | `68 × 88` | `(0.5, 0.5)` | **358.756 .. 446.756**（中心 **402.756**） |
| `Counter` | `(0.5, 0.5)` | `(0, 0)`（**相对 `Image`**） | `85 × 88` | `(0.5, 0.5)` | 同 `Image` |

⇒ 审计的 **“pos 0” 是错的**：`Image` 有 `anchoredPosition.y = +3.2939`，所以它的中心**比 PP 中心低 3.2939**，
不是同心。审计直接把 PP 的中心 406.05 当成了 Image 的中心。

**② 工程自己的尺子现读**（`python 工具/menu_dump.py bundle_menus_assets_all "Daily Reward Popup Entry" --depth 5 --relative --no-sprite`）：

```
1    Personal Progression    0.0   356.0   315.0   456.0   315.03  100.00
2      Image               123.5   358.8   191.5   446.8    68.00   88.00
3        Counter           115.0   358.8   200.0   446.8    85.00   88.00
```

与我们的常量**逐值吻合（≤0.01 px）**：`D_ProgMark (123.51, 358.76, 191.51, 446.76)` · `D_ProgCount (115.01, 358.76, 200.01, 446.76)`。

### 处置

- **不改数值**。若照审计把两个 y 各 `+3.29`（→ 362.05..450.05），反而**制造**一处 3.29px 的原版偏离。
- 代码里就地留更正痕迹（铁律 5）：`D_ProgMark` / `D_ProgCount` 两行行尾加注释，写明反证与「判据本身错」。
- 同族判例：CLAUDE.md「判据与 `.c` 冲突时，以 `.c` 为准、宁可拒做」（`B9_W69ENG4`，2026-10-27）。
- 📌 **给调度台**：`报告/B138_审计汇总_问题清单.md:189` 的 `4-08` 那一行、以及 `B131_审计_UI版面与字体.md:250`
  建议就地改写成「⛔判据本身错：漏掉 `Image.anchoredPosition.y = +3.2939`；我们现值已与原版一致（358.76..446.76）」。
  （本写手按红线**未改**任何报告/账主表。）

---

## 三、改动清单

| 文件 | 改动 | 行数变化 |
|---|---|---|
| `Unity/MyGame/Assets/CardPresentation/Shell/DailyRewardPopup.cs` | ① `BuildArmySelector` 收口到 `ApplyArmyViewportClip`；② 类尾新增常量 + 写口；③ `D_ProgMark`/`D_ProgCount` 行尾更正注释 | **−1 行 + 27 行**（现读 1418 行；`git diff --numstat` = `32 / 6`，含两条行尾注释的重写） |
| `Unity/MyGame/Assets/CardPresentation/Editor/B141_W6_Assert.cs` | **新建**（229 行） | +229 |

⚠️ 本文件没有被 `账/*.tsv` 按 `文件:行号` 引用（已 `grep` 全 `账/` = 0 命中）⇒ 行号漂移不影响账闸。

---

## 四、断言（`Editor/B141_W6_Assert.cs`，共 **30 条** + 3 条条件前提）

### A 段 · 4-08 进度图 y（16 条）

| # | 断的是什么 |
|---|---|
| A0 ×4 | 前提：PP 中心 `406.05` · PP 高 `100` · Image 高 `88` · Counter 高 `88` |
| A1 / A2 | `D_ProgMark` 上沿 `358.76` · 下沿 `446.76`（**字面量**） |
| A3 | `D_ProgMark` 中心 `402.76` |
| A4 ×2 | `D_ProgCount` 上/下沿同值 |
| A5 | **灭自证（结构）**：`PP.CY − Mark.CY == 3.2939`（Image 有 `anchoredPosition.y`） |
| A6 | **灭自证**：上面那个偏移必须 `> 1px`（挡「两边改成同心」） |
| A7 | Counter 与 Image 同心 |
| A8 ×2 / A9 ×2 | x 区间未被动（123.51/191.51 · 115.01/200.01） |

🧨 **怎么红**：照审计把两个 y 各 `+3.29` ⇒ A1/A2 红（字面量不符）**且** A5/A6 红（偏移变 0）。两条不共因。

### B 段 · 4-16 视口 softness（14 条）

| # | 断的是什么 |
|---|---|
| B0 ×3 | 前提：常量 = 原版 `(42,0)` / pad 全 0 |
| B1 | 前提：写口真挂上了 `ViewportClip` |
| B2 | **★** `softness == 字面量 (42,0)`（⛔ 不拿常量比 ⇒ 「常量与实现一起改回 0」也拦得住） |
| B3 | **★** `padding == Vector4.zero` |
| B4 ×2 | `SoftnessPx` = (42, 0)（`Mathf.Max(0,·)` 那一支没夹掉、y 不是 42） |
| B5 | **灭自证**：`softness.x > 0` |
| B6 | 前提：`CaptureNow()` 生效（`HasBaseRect`） |
| B7 / B8 / B9 / B10 | **源码闸**（非注释部分）：写口调用恰 1 处 · 旧的内联挂载 0 处 · `vc.softness = Vector2Int.zero` 恰 **1** 处（只剩奖格轨那颗）· softness 常量声明恰 1 处 |

🧨 **怎么红**：把 `ApplyArmyViewportClip` 改回 `Vector2Int.zero` ⇒ B2/B5/B9 红；把奖格轨也改成 `(42,0)` ⇒ B9 变 0 处 ⇒ 红。

### 接线（⛔ 本代理按红线未动宿主）

`Editor/RewardsScene.cs` 的聚合行（**同一物理行、该行第一个 `//` 之前**）追加：

```csharp
B141W6_Assert.Run(); _pass += B141W6_Assert.Pass; _fail += B141W6_Assert.Fail;
```

（计数器变量名以宿主当时那一对为准。本断言**不建窗**、不需要 Transform 夹具，BatchMode 可跑。
⚠️ B 段会多记 **一次** `ViewportClip.LiveDerivations++`（`CaptureNow()` 内部读一次 `ClipPx` → 「没记过 ⇒ 实时反推」支）；
全仓没有「绝对值 == 0」的断言依赖它 —— `Editor/ShellScene.cs:6115-6119` 那条是**差分**（自己当场取 `ld0` 再比），先后无关。）

---

## 五、验证

- `cd /d/4/Unity && TMPDIR=/tmp/wf_b141_w6 bash 工具/typecheck.sh`
  - 第一次跑：运行时 `0` / 编辑器 `6` —— **全部落在 `Editor/ArtBaker.cs`（不是本写手负责的文件，18:17:52 刚被他人写入）**，按 CLAUDE.md「错误全集中在你不是负责的文件上 ⇒ 不是你的问题」判为并发写手的假错。
  - 隔一轮复跑：**运行时 `0` / 编辑器 `0`** ✅
- 未跑 Unity 自检（红线）：断言文件的**接入与实跑**留给调度台在同步点执行。

---

## 六、顺手发现（⛔ 未改，只报）

1. **`4-08` 的错误已经扩散两处**：`B131_审计_UI版面与字体.md:250` 原句 + `B138_审计汇总_问题清单.md:189` 转抄，
   两处都写着「原版 362.05..450.05」。建议调度台一并就地订正（铁律 5）。
2. **同一个按钮类错在这个仓里已出现第二次**：`Personal Progression` 那颗 `Image` 的 `anchoredPosition`
   与「PP 中心」混用 —— 凡「子件居中于父」的推断，都必须**先读子件自己的 `anchoredPosition`**，
   ⛔ 别拿父的 rect 当子件的 rect。这与 CLAUDE.md「拉伸锚点的节点不能量 sizeDelta」同族。
3. **`4-16` 的另外三处（`RankedTab.cs:583` · `DraftModeWindow.cs:1956` · `EnergySinglePlayerOnlyEventWindow.cs:386`）
   不在本写手白名单内**，仍是 `(0,0)` —— 由 W12 包处理（判据里已分配）。
