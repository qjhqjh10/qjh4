# B117-WMASKCLIP — 主界面奖励活动卡 `Dynamic Content` 的 `RectMask2D` 裁切

> 任务 `B117-R59`。执行代理 `B117-WMASKCLIP`，2026-11-02。
> 承载件：`Shell/RankedArmyBonusCard.cs`（**不是** `MainMenuRuntime.cs` —— 见 §1 的 grep 结论）。
> 本文件只写证据与做法；状态见 `活状态表.md`。

## 0. 结论

原版 `Reward Event Container 1x1` 的 `Dynamic Content` 节点上有一颗 `RectMask2D`，我们没实现。
已用本工程既有的 `ViewportClip`（`RectMask2D` 的等效物）挂上去，参数逐字取原版：
`m_Padding {x:0, y:-15, z:0, w:0}` · `m_Softness {0,0}`。
效果 = 三颗 `Simple Army Icon`（总宽 `3×254.90341 + 2×20 = 804.71023`）居中摆在 **529.8** 宽的视口里时，
**第 1 / 3 颗各被裁掉 137.4551 px**（原版就是这个行为）。

## 1. 判据（全部现读）

| 事实 | 出处 |
|---|---|
| `Dynamic Content` 带 `RectMask2D` | `判据/B90_C2b_远端prefab几何.md:207`（该行组件列逐字有 `RectMask2D`） |
| 遮罩参数 `m_Padding {x:0,y:-15,z:0,w:0}` · `m_Softness {x:0,y:0}` | `d:/2/tools/assetripper_out/ExportedProject/Assets/Reward Event Container 1x1.prefab`：MB `&114571718729274231` → `m_GameObject: 1709867586078768`（该 GO 的 `m_Name` = `Dynamic Content`，RT `224436073260064126`） |
| `Content` 的 `HorizontalLayoutGroup` spacing 20 · align 4（MiddleCenter） | `判据/B90_C2b_远端prefab几何.md:210` |
| `Simple Army Icon.sizeDelta.x = 254.90341` · 三层矩形 `2.54,11.16→532.34,291.99` | 同上 `:207/:208/:210` |
| 溢出量推导 | 视口宽 `532.34−2.54 = 529.8`；居中摆 ⇒ 两侧各溢出 `(804.71023−529.8)/2 = 137.4551` |

⚠️ 顺带核到：**同一份 prefab 的根节点（`Reward Event Container 1x1`）上也有一颗 `RectMask2D`**（MB `&114977153420339851`，
`m_Padding {0,0,0,0}`）；E-1 表 `:192` 的根行组件列里也写了它。我们**没有**复刻那颗 —— 见 §6「顺手发现」。

## 2. 改了什么（`文件:行号`）

**宿主 grep 结论**：`Dynamic Content` 不在 `Shell/MainMenuRuntime.cs` 里建 —— 它由
`Shell/RankedArmyBonusCard.cs` 的 `Build()` 建（`MainMenuRuntime` 只是 `Create` + `ApplyPxRect` + `Initialize` 的调用方，
`:5197 PlaceRankedArmyBonusCard`）。⇒ 按简报「对应宿主」应落在 `RankedArmyBonusCard.cs`（简报已授权）。

| 位置 | 改动 |
|---|---|
| `Shell/RankedArmyBonusCard.cs:240` | `var scroll = MenuDraw.Node(t, "Dynamic Content", …)` → `_clip = ViewportClip.Hang(t, "Dynamic Content", contentAbs, MaskPadding, Vector2Int.zero);`（下一行 `var view = MenuDraw.Node(_clip.transform, …)`） |
| `Shell/RankedArmyBonusCard.cs:273` | 在既有行上追加 `if (_clip != null) _clip.SetBaseRect(contentAbs);`（**代码写在行内 `//` 之前**，⛔ 没有被注释吞掉） |
| `Shell/RankedArmyBonusCard.cs:369-389`（类尾） | 新增 `ViewportClip _clip;` 与 `static readonly Vector4 MaskPadding = new Vector4(0f, -15f, 0f, 0f);`（带判据注释） |
| `Editor/B117_WMASKCLIP_Assert.cs`（新建） | 断言宿主，`Run()` 入口 |

**行数守恒**：`Clean()` 仍在 **:291**、`Initialize()` 仍在 **:254**（账 `个人资料_成就_排行.tsv:197/:198` 的两条 A 落点）。
新增成员全部在**类尾**（ContentAbsNow 之后）。`check_ledger_points.py` 现跑该域：`A=230 通过=230 漂移=0 失败=0`。

## 3. 用的是哪套裁切设施、为什么

- **`Shell/ViewportClip`**（= 原版 `RectMask2D` 的等效物，全壳 54 处生产站点在用）：
  `ViewportClip.Hang(parent, name, rect, pad, softness)` 建节点并挂组件；
  `MenuDraw.Rect/Nine/Hit` 在 `clip == null` 时**沿父链解析**最近的 `ViewportClip`
  （`ViewportClip.Resolve` ②支）⇒ 图标那块 `Image` 自动吃到裁切，**不需要给每个图标传 `clip` 实参**。
- 为什么不用 `MenuScroll.ClipNode`：那张卡**没有滚动实现**（原版 `UI_InfiniteScroll` 我们只暴露了
  `AutoScrollEnabled` 这一位，见 `RankedArmyBonusCard.cs` 文件头「没做到的」）⇒ 不需要滚动区那一套。
  `ClipNode` 是 `MenuScroll` 的读框入口，不是遮罩本体；遮罩本体就是 `ViewportClip`。
- 🔴 **`BaseRect` 必须在 `Initialize` 里重设**（本件最容易错的一处）：
  `Build()` 那一刻卡根还在屏幕中心（原版 prefab 的 `anchoredPosition (0,−43.8868)` 是相对窗口），
  宿主随后才 `ApplyPxRect` 把它挪到 first-fit 第 5 列；而图标是按 `ContentAbsNow()`（**摆放后**）烘焙的。
  `MenuDraw.ApplyPxRect` 的自动记框只作用于**它自己那个节点**（宿主只对卡根调它），所以这里必须显式一句。
  ⛔ 漏了这句的后果：遮罩框留在屏心 ⇒ 三枚图标整块在框外 ⇒ `MenuDraw.Rect` 返 null ⇒ **图标静默消失**。

## 4. 断言（`Editor/B117_WMASKCLIP_Assert.cs`）

47 个 `Check/CheckNear` 调用点（两处 3 次循环展开，运行时约 53 条）。分组：

- **前提**（5）：卡建得出 · `Content` 找得到 · `ArmyImageCount==3` · `Dynamic Content` 找得到 · 上面挂着 `ViewportClip`。
- **① 视口 = 原版**（5）：`Content` 529.8 × 280.83；中线 = 卡实测中心 + 原版相对矩形；且在屏外列（x>1920）。
- **② 遮罩框**（16）：`ClipPx` 非 null · 宽高 = 529.8/280.83 · 中线 = `Content` 中线 ·
  `m_Padding` 四分量逐字 `(0,−15,0,0)` · `m_Softness==(0,0)` · `RenderClip = Clip − pad`（只下沿 +15）。
- **③ 全在遮罩内**（6）：每颗图标都有 `Image` quad；每颗 quad 落在 `RenderClip` 内。
- **④ 🧨 灭自证**（8）：图标[0] 整块左沿 = 视口左沿 − 137.455；图标[2] 整块右沿 = 视口右沿 + 137.455；
  两者**画出来**的宽 = `254.90341 − 137.455 = 117.448`、左/右沿 = 视口边；中间那颗不裁（宽 254.90341）。
  ⇒ 去掉遮罩 ⇒ quad 回全幅、越出视口 ⇒ 这一组与 ③ 同时红。**结构性判别式**。
- **⑤ 裁切不改布局**（5）：相邻间距仍 = `254.90341+20`、同一行、中间那颗居中。
- **⑥ 🧨 判别式**（8）：整卡再挪 `(137, 61)` 重建 ⇒ 遮罩框**跟着挪**（漏写 `SetBaseRect` 必红）、
  且两侧仍被裁到 117.448。

⚠️ **接线状态**：本代理写权限不含 `Editor/*Scene.cs` ⇒ `Run()` **目前没有宿主调用**。
由调度台在 `Editor/MainMenuScene.cs`（或 `ShellScene.cs`）加一行 `B117_WMASKCLIP_Assert.Run();` 即可。
命令见文件头。判绿红看 `^B117WMASKCLIP` 的合计行，⛔ 别用 `grep -c ✗`。

## 5. 验证结果

| 项 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_wmaskclip bash Unity/工具/typecheck.sh` | **运行时 0 / 编辑器 0**（第一次跑撞上别的代理正在写 `Battle/BattleSfx.cs` ⇒ 2 条 `CS0103 _lastAt` 假错；60 秒内该文件改完、复跑归零） |
| `check_aggregate_lines.py`（两个改动文件） | **PASS**（聚合行 0 · 违规 0） |
| `check_ledger_points.py`（全表） | `个人资料_成就_排行.tsv`：**A=230 通过=230 漂移=0 失败=0**（本件未引入漂移） |
| `git diff --numstat` 对一眼 | `RankedArmyBonusCard.cs 54/4` —— 其中约 35 行是**上一批 `B117-WMENUICON` 未提交的 `ContentAbsNow()`**（HEAD 里还是 `var contentAbs = _contentAbs;`），本件自身 ≈ +19/−4 且全在类尾/行内 |
| 行尾 | 改动文件与新建文件均 **纯 LF**（与同目录 `.cs` 一致；逐字节数过） |

⛔ 未跑 Unity 批处理（红线），所以 **`Run()` 的绿红还没实测** —— 结论是「判据 + 代码路径」级，不是「跑过」级。

## 6. 顺手发现（本代理未做，交调度台判）

1. 🟠 **卡根上的那颗 `RectMask2D` 我们也没实现**（原版 `m_Padding {0,0,0,0}`，MB `&114977153420339851`；
   `判据/B90_C2b_远端prefab几何.md:192` 根行组件列有它）。
   今天**视觉可见的差别只有一处**：`Feature Badge` 的矩形是 `−4.95,−4.02 → 185.90,186.44`（比卡根大 4~5px）
   ⇒ 原版会被裁掉左上那几条像素，我们没裁。`Background Image` / `Foreground Image` 超出更多
   （`−106.66,−167.25 → 641.66,581.07`），但我们这两件**只建了节点、原版也只在运行时喂图**，今天无 quad ⇒ 无差别。
   做法（若采纳）= 在 `Build()` 的卡根上 `ViewportClip.Hang(t, RootName, CardRel, Vector4.zero, Vector2Int.zero)`；
   它的 `BaseRect` 会由宿主那一次 `MenuDraw.ApplyPxRect(card.transform, …)` **自动**更新（组件就在那个节点上）。
   ⚠️ 未做原因：会改变 `Feature Badge` 的可见几何（宽 190.85 → 185.95），属于**真机才看得出的视觉变化**，
   且可能有别的断言按 190.85 在量 ⇒ 不在本件授权范围，先记账。
2. ℹ️ `判据/B117-WMENUICON_主界面图标.md:92-95` 那条「🟠 `Dynamic Content` 的 `RectMask2D` 我们没实现」
   本轮已解决 —— 建议调度台把那条就地改成「✅ 2026-11-02 `B117-WMASKCLIP` 已复刻（`ViewportClip` + padding (0,−15,0,0)）」。
   （判据文件由调度台改；本代理只在本报告里记。）

## 7. 真机才看得出的风险

1. **今天看不见**：这张卡在 first-fit 第 5 列（x 2425…2960），整张在 1920 屏外 ⇒ 裁切效果**在现界面里无从肉眼验收**。
   要肉眼验，得把 `affectedArmies` 加到 3 个以上并让卡进第 1~4 列，或临时改宿主列号。
2. **遮罩框的帧依赖 `Initialize` 那一次 `SetBaseRect`**：任何「摆放后不调 `Initialize` 就重建图标」的新路径
   都会让框停在旧帧。今天宿主只有 `PlaceRankedArmyBonusCard` 一条路（`Create → ApplyPxRect → Initialize`），
   且滚动重建走 `_placeAllCards` 整卡重建 ⇒ 安全；**将来若加「只换图标不重建卡」的路径，必须一并 `SetBaseRect`**。
3. **`MenuDraw.Rect` 在「整块在框外 ⇒ 返回 null」**：若将来活动阵营多到某颗图标完全落在视口外，
   那颗图标会**没有 quad**（= 原版被 `RectMask2D` 完全裁掉），`SimpleArmyImage` 那颗 Image 也随之不存在 ——
   与「取不到图 ⇒ 关掉 Image」的现有告警路径**不同源**，日志上看不出区别。今天 3 颗时不会发生。
4. **padding 的符号**按本工程约定（正 = 缩小、负 = 扩大）取 −15 = 下边外扩；原版 `Vector4` 字段序
   X=Left / Y=Bottom / Z=Right / W=Top 已按 `ViewportClip.cs` 文件头核过。若真机看到下边多留 15px 背景，
   说明符号理解反了 —— 改 `MaskPadding.y` 的符号即可（只有这一处）。
