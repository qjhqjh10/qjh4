# B141-W129 · 4-21 手牌悬停 z 提前量 —— 报告

> 执行写手：`B141-W24`（会话第一百二十八 · 批次 B141）
> 条目：`判据/B141_W129_对账_UI动画.md` **W16 包** · `报告/B138_审计汇总_问题清单.md` §4 的 **4-21**
> 日期：2026-11-04

## 一、结论（一句话）

手牌悬停那张的 z 提前量从 **`0.5`（我们的世界单位）** 改成 **`0.00137361`（我们的世界单位）** = **0.14835 px**，
换算链是：原版 **0.01 原版世界单位** × **14.835 px/原版单位** ÷ **108 px/我们的单位**。
两处常量（`HandLayout.frontZ` / `CardInteraction.frontZ`）同步，两处都补了 `[System.NonSerialized]`
（否则场景里序列化的旧 `0.5` 会盖掉初始化器 ⇒ 改小了也**真机不生效**）。

**审计表那句「大 6.9 倍」不成立**（口径错，见 §三）；按同一坐标帧算，改前比原版大 **364 倍**。

## 二、判据（全部现读，逐条给坐标）

| # | 判据 | 出处 / 读法 |
| --- | --- | --- |
| ① | 原版悬停把卡往相机方向挪 **0.01** | `d:/2/tools/decomp_full/CardScript__ShowCardInHand.c:104`：`local_40 = *(float *)(lVar7 + 8) - DAT_1834b2dbc;`（`get_position()` 的 `+8` = **z**）→ 紧接着 `Transform__set_position` ⇒ 等价于 `position = (x, y, **z − 0.01**)` |
| ② | 那个 `DAT_1834b2dbc` 真是 `0.01f` | `0x1834B2DBC`，`python -I -X utf8 工具/read_literal.py "D:/2/unity_run_ref/GameAssembly.dll" 0x1834b2dbc` ⇒ **4B float = 0.01 ✅**（8B double = 5.37e-17 ⚠️ ⇒ 不是 double） |
| ③ | 它是**原版世界单位**（不是 px、不是我们的单位） | 同一地址在 `CardScript__SetCardMovement.c:41-45` 当 `d2 < 0.01` 的**近阈**，而 `B120-W1` 已核过那里的 `d2` 是**世界坐标平方和**、必须乘「桥²」（`CardInteraction.HandSnapNearD2 = 0.01f × 桥 × 桥`）⇒ 同一座桥，同一量纲 |
| ④ | 桥 = **14.835 / 108** | `HandLayout.OriginalPxPerUnit = 14.835f`（原版 UI 相机 fov40 / 平面 100）+ `LayoutSpace.DesignHeight = 10 单位 = 1080 px`（⇒ 108 px/单位）· `HandLayout.OurUnitsPerWorldUnit = 14.835/108 = 0.1373611`（判据文档见该常量的 doc，含「7.28 倍不是错」的推导） |

## 三、换算过程（为什么 **不是** 0.5 → 0.01）

两边**不是同一个坐标帧**：

| | x/y 的单位 | z 的单位 | px / 单位 |
| --- | --- | --- | --- |
| 原版 | 手牌父级 `HandArea` 带 `localScale 108`、Canvas 又乘 1/14.835 ⇒ 合计 **7.28** | **世界单位** | **14.835** |
| 我们 | 世界单位（正交相机，可见高 10 单位 = 1080 px） | 世界单位 | **108** |

⇒ 原版那句 `z − 0.01` 的 `0.01` 是 **0.01 × 14.835 = 0.14835 px**；
换到我们的单位 = **0.14835 / 108 = 0.00137361**。

- 改前 `0.5` 我们的单位 = **54 px** ⇒ 比原版 **大 364 倍**（54 ÷ 0.14835 = 364.0）。
- 审计表写的「大 **6.9 倍**」= 直接 `0.5 ÷ 0.01` 的商（≈ 50）再叠了 `zOrderStep` 的折算，
  **把「我们的单位」和「原版世界单位」当成同一个帧** ⇒ **⛔ 别按那个数改**（若照它去改会得到 0.0725，仍然大 48 倍）。
- 缩略：`0.5 → 0.00137361`，比值 **364.0**。

## 四、改动清单

| 文件 | 行 | 改动 | 行数变化 |
| --- | --- | --- | --- |
| `Unity/MyGame/Assets/CardPresentation/Hand/HandLayout.cs` | `:284-285` | `[Tooltip]` 补单位说明 + `[System.NonSerialized]`；`frontZ = 0.5f` → `= 0.01f * OurUnitsPerWorldUnit`；`✅ 已复刻` 标记与整条换算写在同一物理行的行尾注释里 | **0**（2 行 → 2 行，`:285` 仍是原行号） |
| `Unity/MyGame/Assets/CardPresentation/Hand/CardInteraction.cs` | `:42-43` | 同上同步（判据同一条）；注释写明「本字段**零读取方**」 | **0**（`:43` 仍是原行号；该文件另有 15 行是**别人此前未提交的改动**，不是本笔） |
| `Unity/MyGame/Assets/CardPresentation/Editor/B141_W24_Assert.cs` | 新建（244 行） | 断言宿主 A/B/C 三组，共 **17 条** | 新文件 |

- `HandLayout.cs` 的 `git diff --numstat` = **2 / 2**（只有本笔）；行尾仍是 **LF**（未翻）。
- `CardInteraction.cs` 行尾仍是 **CRLF**（2865/2865，未翻）；`git diff --numstat` = 17 / 2（含他人 15 行）。
- 新文件行尾 **LF**（与同目录 `B117_G10_06_Assert.cs` / `B140_WO_Assert.cs` 一致）。
- ⛔ 本笔**未动** `账/` · `活状态表.md` · `文件所有权_当前.md` · `CLAUDE.md`（白名单外）——
  **4-21 的核销与所有权表登记由调度台做**。核销时可引用的独立判据 =
  `Editor/B141_W24_Assert.cs` 的 A1/B1（字面量独立重算，⛔ 不读被测字段）。

## 五、验证

1. **typecheck**：`cd /d/4/Unity && TMPDIR=/tmp/wf_b141_w24 bash 工具/typecheck.sh`
   ⇒ **运行时错误数 0 / 编辑器错误数 0**（最终一跑，18:44）；并已确认 `wf_csc_editor.rsp` 里**含** `B141_W24_Assert.cs`（不是「没编到」的假 0）。
   ⚠️ 中间两跑出现过红，**全部落在别人的在飞文件上**（按 CLAUDE.md 的并发写手口径，不属本笔）：
   ① `Shell/MissionsTab.cs` 一度未闭合 / 未声明 `InfoHitPad`（W14 的 4-15 在写）；
   ② `Editor/B141_W25_Assert.cs` 引用 `TextInputField.AreaNodeName`（W25 的 4-23 在写，成员还没落盘）。
   两个文件静默 45 s 后复跑 ⇒ **0 / 0**，本包的三个文件（`HandLayout.cs` / `CardInteraction.cs` / `B141_W24_Assert.cs`）**自始至终 0 错**。
2. **断言（未跑 Unity，⛔ 子代理不许 `-executeMethod`）**：`B141_W24_Assert.Run()` 三组 17 条，零前提、自带临时根、收尾 `DestroyImmediate`。
   - **A 组 · 常量（7 条）**：A0 前提（拿得到 `HandLayout`）、A0b 前提（桥 = 14.835/108）、
     **A1 值 == 0.01×14.835/108（±1e-7）**、**A2 灭自证（≠ 旧值 0.5）**、**A3 灭自证（≠ 漏过桥的 0.01）**、
     **A4 物理量 0.14835 px（区间 0.14–0.16）**、A5 `[NonSerialized]` 必须在（否则场景旧值静默覆盖）。
   - **B 组 · 实际施加（7 条）**：5 张卡、关补间；B1 悬停那张的 **Δz == 0.00137361（期望值由字面量独立重算、不读 `frontZ`）**；
     B2 方向（z 变小）；B3 Δz×108 ≈ 0.14835 px；**B4 其余 4 张的 z 逐张不变**；
     **B5 灭自证：把 `frontZ` 临时改回 0.5，输出必须跟着变（差 ≈ −0.4986）** —— 证明 z 由该字段驱动、不是公式里另写死的常量。
   - **C 组 · 第二处常量（3 条）**：C1 `CardInteraction.frontZ` 与之一致；C2 它也必须 `[NonSerialized]`。
   - **灭自证总览**：把 `frontZ` 改回 `0.5` ⇒ A1/A2/A4/B1/B3 同时红；写成 `0.01`（漏过桥）⇒ A1/A3/B1 红；
     删掉 `- frontZ` 那句 ⇒ B1/B5 红；删掉 `[System.NonSerialized]` ⇒ A5（或 C2）红。
3. **未接线**：`Editor/BattleScene.cs` 不在本包白名单内 ⇒ 宿主接线留给调度台。零前提，建议接在**方法尾聚合行、`合计` 之前**：
   `B141_W24_Assert.Run(); pass += B141_W24_Assert.Pass; fail += B141_W24_Assert.Fail;`
   （同一物理行追加 ⇒ 行数零变化。）本文件头也写了独立跑法：`-executeMethod B141_W24_Assert.Run`。

## 六、⚠️ 必须让下一个会话知道的四件事

1. **当前相机是正交**（`LayoutSpace.Apply`：`cam.orthographic = true`、z = −20）⇒ **z 今日完全不参与投影**，
   所以这 0.5 → 0.0014 的改动在**今天的画面里零像素差**。它不是「修好一个看得见的 bug」，是**参数口径对齐**；
   真正变成可见量是在 `A2098`（换回原版透视相机）之后 —— 那时旧 `0.5` 会是 20 单位景深上的 **2.5% 缩放跳变**，
   新值只有 **0.0069%**。**⛔ 别用「改完看不出区别」判定本笔没生效**（读数口 = 断言 A1 / B1）。
2. **层序（谁压谁）与 z 无关** —— 由 `HandLayout.queueTop` 那套 render queue 决定（`HandLayout.cs:806-832` 的注释 + `SetQueue`），
   z 只影响几何纵深。所以把 z 改小**不会**让悬停那张被邻牌压住（悬停那张恒拿 `queueTop`）。
   本笔 ⛔ 没动 `queueTop` / `zOrderStep`（`zOrderStep = 0.08` 是已打 `🔶 自己设计` 的量，属另一格）。
3. **`[System.NonSerialized]` 是必须的，不是可选的**：`Scenes/Battle.unity:640`（`CardInteraction`）、
   `:1307` / `:2544`（`HandLayout` 我方/敌方）、`CardBase.unity:8150` / `:22445` **五处序列化着旧 `frontZ: 0.5`**。
   只改初始化器而不排除序列化 ⇒ **真机仍跑 0.5**（静默；`hoverLift` 2026-11-02 正是踩了这个坑）。
   断言 A5 / C2 就是钉这件事的。⚠️ 那些 YAML 里的 `frontZ: 0.5` **本笔没有删**（改场景要跑 Unity）——
   它们现在被 `[NonSerialized]` **忽略**、无害；等下一次 `BattleScene.BuildAndSaveScene` / `CardBaseDemo.Run`
   重存场景时 Unity 会自动把它们丢掉（届时 `git diff` 会看到这几行消失，**那是预期的**）。
4. **`CardInteraction.frontZ` 现读零读取方**（全仓 `.frontZ` 只命中 `HandLayout` 与两处编辑器断言
   `B117F11_Assert.cs:128` / `B117_G10_06_Assert.cs:119`）。悬停 z 实际由 `HandLayout.Refresh` 施加。
   本笔按审计要求**同步了值**，但 ⛔ 别再在 `CardInteraction` 里加一次 z 抬升（会叠加两次）。

## 七、顺手发现（只报，未改）

| # | 发现 | 位置 | 影响 |
| --- | --- | --- | --- |
| 1 | 那两处编辑器断言**读的是 `layout.frontZ`**（不是硬编码），所以它们会自动跟随新值，**不需要改**；但也意味着它们对本笔**不构成独立判据**（拿被测字段当期望值 = 同义反复）。独立判据由本包的 `B141_W24_Assert` A1/B1 提供。 | `B117F11_Assert.cs:128` · `B117_G10_06_Assert.cs:119` | 无回归风险；知识性 |
| 2 | `zOrderStep = 0.08`（我们的单位 = **8.64 px**）同样是「我们的单位」的量，原版 `useZOrder` 的加量是 **0.001 原版世界单位 ≈ 0.015 px** —— 但那一格**已经**在 `HandLayout.cs:299` 打了 `🔶 自己设计` 并写明「原版靠 canvas 排序、我们没有等价物」，**不属本笔**。 | `HandLayout.cs:287-299` | 已记账，⛔ 别顺手改 |
| 3 | `dragLiftZ = 0.6`（`CardInteraction.cs:40`，拖拽抬升）与 `frontZ` 同族（同样是「我们的单位」的 z 偏置），但**判据不同**（拖拽来自原版 `StartMovingFromHand` / `SetCardMovement`，本包未查）。若后续核出原版值，应按同一座桥换算。 | `CardInteraction.cs:40` + `:940`/`:986`/`:2835` | 待查，未改 |
