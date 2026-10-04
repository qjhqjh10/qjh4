# A169 · `Battle/WfSlider.cs` 那四处合并成一次修（队列 / 端帽 / 手柄边长 / 手柄偏移）

> 🔴 **2026-10-07 就地更正（铁律 5）—— 本文附录里那条「手柄起点真值 = 轨道左 `+17`」【是算错的】，真值就是 `+12`；实现本来就对、一处没改。**
> 四条独立证据（Unity 自带 `DefaultControls.CreateSlider` 同形 · 原版运行时 dump 里滑区与轨道**左沿相同** · 四份 dump 逐行同形 · 编辑器态手柄左缘离轨道左端 0.8px）与全过程 → `资料/普查产出_1007/波8_手柄偏移_17与断言修正.md`（那份还修了同族两条真缺陷）。**⛔ 别照本文附录的 `+17` 改任何东西。**

2026-10-07 · 执行子代理（波 8 · 共用件族第一件，全权限）
· 改了 **4** 个文件（`Battle/WfSlider.cs` · `Battle/SettingsPanel.cs` · `Shell/SettingsWindow.cs` · `Editor/SettingsScene.cs`）
· **没跑 Unity / 没跑自检**（按简报，自检由主对话在同步点跑）；改完跑了两次**秒级类型检查** = `运行时错误数 0 / 编辑器错误数 0`
· 没动 git · 没改两张正本、没改任何 `资料/*.md` 正本 · `d:/2/` 只读

**判据来源**（全部当场实读，不引用别人转述）：
· `d:/4/Unity/资料/待办判据_1006.md` §A169（正本一行在 `d:/4/项目任务.md`，按 `A169` 搜到的 `:609` 那条）
· `d:/4/Unity/资料/普查产出_1006/A168_FPS滑块.md` 的 §顺手发现 ①②③④ + §步骤 0 的字段表
· **解包实读（本轮自己读的，`d:/2/新解包资源/assets_full/`）**：

| 读到的 | 出处（按 PathID 找得到） |
|---|---|
| 设置窗音频页那根 `Handle`：`m_SizeDelta = (46.811, 22.406)` · `m_AnchoredPosition.x = **11.9998779296875**` · 锚 (0,0)-(0,0) · pivot (.5,.5) | `bundle_menus_assets_all/RectTransform/RectTransform_-6029089631055872090.json` |
| 它的父 `Handle Slide Area`：`m_SizeDelta.x = **−10**` · `m_AnchoredPosition.x = **−4.9998779296875**` · 锚 (0,0)-(1,1) ⇒ **左右各让 5、居中** | 同目录 `RectTransform_452611598607941542.json` |
| 它俩的祖父 = 那根滑块：`m_SizeDelta = (0, **13**)`（高 13）、锚 (0,.5)-(1,.5)、`m_AnchoredPosition.y = −11.1` | 同目录 `RectTransform_-5607048967920844890.json` |
| 战斗三根 `Handle`（`_2597 / _2979 / _3354`）= **同** 46.811×22.406 / `aPos.x` 11.99988；各自的父（`_2596 / _2978 / _3351`）= 同 `sizeDelta.x −10` / `aPos.x −4.9998` | `bundle_scenes_scenes_battlearena1/RectTransform/*.json` |
| 四颗 Image（设置窗音频页 bg/fill + FPS 那根 bg/fill）`m_PixelsPerUnitMultiplier` **全是 2.0**（`m_Type = 1` Sliced、`m_Color` 全白、`m_RaycastTarget` 全 1） | `bundle_menus_assets_all/MonoBehaviour/{_-3335051800813797466, _-3703242373742624858, _-3786787164003336282, _-1239832872394260570}.json` |
| 战斗 bundle 里 `ppuMul == 2.0` 的 Image 共 **11 颗**，其中 `m_Sprite = -6332789735026905802`（Volume_bar_active）**×3** + `5759692323890646976`（Volume_bar_inactive）**×3** = 那三根的 Fill / Background ⇒ 战斗那三根**也是 2.0** | `bundle_scenes_scenes_battlearena1/MonoBehaviour/` 全扫 |
| 三张图的 `m_Border`：inactive **(184,0,184,0)** 400×31 · active **(30,0,30,0)** 64×31 · `Volume_button` 110×110 无 border | `sharedassets0/Sprite/{Volume_bar_inactive,Volume_bar_active,Volume_button}.json` |
| `Handle` 那颗 Image 的 `m_RaycastTarget = **1**`（⇒ 原版点得到手柄，哪怕它探出轨道） | 同上一行的 Image 清单（A168 §步骤 0 ④ 也逐颗列了） |

· uGUI 语义（本机自带包 `D:/Unity/Hub/Editor/6000.3.23f1/Editor/Data/Resources/PackageManager/BuiltInPackages/com.unity.ugui`）：
`Slider.UpdateVisuals` 只写 `m_HandleRect.anchorMin/anchorMax`（**不动 `anchoredPosition`** ⇒ 那个 11.99988 是**常驻偏移**）；
`UpdateDrag` 拿 `m_HandleContainerRect`（= `Handle Slide Area`）当点击矩形；`Image.GenerateSlicedSprite` → `GetAdjustedBorders(border / multipliedPixelsPerUnit)`。

---

## 一、结论

**做完了** —— 简报那四条全部落实 + 两个调用点同步 + 补了断言；类型检查绿。

**但比简报多了一个必传形参 `capScale`**，理由写清楚（请调度台裁）：

* 简报要的三条 `queue` / `handlePx` / `handleOffset` **一条不少、语义照写**；
* 第 ④ 条（端帽）真正要画对，需要**两个**因子：`÷ m_PixelsPerUnitMultiplier 2`（简报写的）× **该实例的设计 px→画布 px 倍数**。
  战斗那条父链无缩放（倍数 = 1），主菜单设置窗根那层 0.9 是**烘进矩形**的（`SettingsWindow.Screen()`）⇒ 端帽应当是
  **82.8 / 13.5 画布 px**（A168 报告 §顺手发现 ② 自己给的就是这两个数，A168 那条自检期望也是 82.8）。
  那个倍数**在本件内部拿不到**：`trackH` 两边不同源（战斗 12 / 设置窗 13）、任务书又要求 `handlePx` 按"框的高"走
  （FPS 那根是 35.406）⇒ 从任何入参反推都是"拿一个值当全部情况"（铁律 5·c）⇒ 只能显式传。
  ⇒ 本件签名 = 简报三条 + `capScale`（**四条都必传、都没有默认值** —— 呼应验收标准 ⑥「不许默默用默认值顶替」）。

🔴 另**新查出一条真偏离**（简报里没有、本件**只报没改**，见 §附录·1）：原版手柄的起点还要再加 **5**
（`Handle Slide Area` 左右各让 5）⇒ 真值 = 轨道左 **+17** + 值 × 滑区宽；我们（含 A168 那根 FPS 滑块）= **+12**。

---

## 二、证据

### 2.1 原版真值 → 改前 → 改后（逐条）

| # | 项 | 原版真值（出处见文件头） | 改前（A169 之前） | 改后 |
|---|---|---|---|---|
| ① | 渲染队列 | 逐宿主不同（uGUI 里靠层级序，本就是"照场景给"的东西） | **本件内部硬编码 3000**（那是**战斗**面板的档）⇒ 设置窗里被压暗层 3130 / 面板 3131 / 填色 3132 / 内容 3133 盖住 | 调用方必传：**设置窗音频页 = 3133 / 3134 / 3135**（`QContent/QText/QOverlay`，与 A168 那根 FPS 滑块同档）；**战斗 = 3000 / 3001 / 3002**（`SettingsPanel.QPanel`） |
| ② | 九宫格端帽 | `m_Border(184 / 30) ÷ m_PixelsPerUnitMultiplier(2)` = **92 / 15** 设计 px | 184 / 30 —— **贴图 px 原样**当画出来的端帽 ⇒ 宽 2.2 倍、中段短一半 | 92·`capScale` / 15·`capScale` ⇒ 战斗 **92 / 15**、设置窗 **82.8 / 13.5**（画布 px） |
| ③ | 手柄中心 | 轨道左 + `m_AnchoredPosition.x`（**11.9998779296875**；`Slider` 不动它） | **一次都没加** ⇒ 手柄整体偏左 12 设计 px | 加 **10.799892**（= 11.99988 × 0.9，设置窗）/ **11.99988**（战斗） |
| ④ | 手柄实画边长 | 逐实例：框 46.811×22.406（音频九根）/ 46.811×**35.406**（FPS 那根）+ 110×110 方图 `preserveAspect` ⇒ 取**短边** | 硬编码 **22.406**（只对音频九根） | 调用方必传：设置窗 **22.406 × 0.9 = 20.1654**、战斗 **22.406** |

**连带改的一处（必须，否则另一条宿主会红）**：`Contains` 的命中区 —— 手柄中心带上那 12 之后，**值接近 1 时手柄中心落到轨道外**
（战斗：轨道半宽 2.5976 世界单位，手柄中心 2.6161）。原版那边**点得到**（`Handle` 那颗 Image `m_RaycastTarget = 1`，射线冒泡到 `Slider`），
所以 `Contains` 现在按"轨道 ∪ 手柄"判。⛔ 不改这一处，`Editor/BattleScene.cs:5438` 那条「指针落在手柄上 ⇒ 命中判定打得中」会红 ——
而它**原来只靠这个 bug 才绿**（详见 §附录·5）。

### 2.2 改了哪几行

| 文件 | 行 | 改动 |
|---|---|---|
| `Battle/WfSlider.cs` | `:61-101` | 常量组：新增 `HandleFrameW/HandleFrameH`(46.811/22.406) · `HandleOffsetPx`(**11.99988**) · `BarBorderPx/FillBorderPx`(184/30) · `PpuMul`(**2**)；`HandlePx` 保留（= `HandleFrameH`，只当"音频九根"那个值）；`QSlider` 从 `const` 改 `public const` 并**不再被内部使用**（改成调用方必传，注释写明"别退回默认"） |
| `WfSlider.cs` | `:126` | 新字段 `_handleH, _handleOffsetU`（逐实例，世界单位） |
| `WfSlider.cs` | `:165-188` | `Create` 签名加 **`int queue, float handlePx, float handleOffset, float capScale`（都必传）**；三条越界告警（`queue < 1` / `handlePx <= 0` / `capScale <= 0` = 不许静默顶替） |
| `WfSlider.cs` | `:221-243` | 三层：bg = `MenuDraw.Nine(..., queue, borderOutPx: 184/2·capScale)`；fill = `queue+1, borderOutPx: 30/2·capScale`；handle = `ImageQuad.Create(..., s._handleH, ...)` + `SetRenderQueue(queue+2)` |
| `WfSlider.cs` | `:267` | `Layout()`：`x = -_w*0.5f + _handleOffsetU + t*(_w - TravelRightPx/U)`（**加了中间那一项**） |
| `WfSlider.cs` | `:272-288` | `Contains()`：纵向用 `_handleH`；横向 = 轨道那一段 **∪ 手柄自己那块**（探出去的部分） |
| `Battle/SettingsPanel.cs` | `:230-248` | 三个调用点必传：`queue: QPanel, handlePx: WfSlider.HandlePx, handleOffset: WfSlider.HandleOffsetPx, capScale: 1f`（+ 注释：本面板父链无缩放，⛔ 别乘 0.9） |
| `Shell/SettingsWindow.cs` | `:1065-1079` | 音频页那处调用点必传：`queue: QContent, handlePx: WfSlider.HandlePx * RootScale, handleOffset: WfSlider.HandleOffsetPx * RootScale, capScale: RootScale`（+ 注释；**这一处以外这个文件一个字没动**） |
| `Editor/SettingsScene.cs` | `:1169-1279` | 新增一节断言（**纯新增**，`git diff --numstat` = `114 / 0`）—— 见 2.3 |

### 2.3 新断言（音频页；`Editor/SettingsScene.cs:1169-1279`，逐字）

判据基座：**基准取本窗真实的那几层**（把窗里所有 quad 扫一遍，**排除滑块自己那棵树**），
`winChromeQ` = 3130/3131/3132 那三档的最高者、`winContentQ` = **内容**档 3133。
⛔ 不拿 `SettingsWindow.QContent` 当期望值 —— 那是被测实参（同 A125① 的口径）。

```csharp
int winChromeQ = -1, winContentQ = -1;
var allQ = root.GetComponentsInChildren<ImageQuad>(true);
for (int k = 0; k < allQ.Length; k++)
{
    var q = allQ[k];
    if (q == null) continue;
    bool mine = false;                       // ⛔ 把滑块自己那棵树排除（轨道就落在 3133）
    for (var t = q.transform; t != null && t != root; t = t.parent)
        if (t.name.StartsWith("slider_")) { mine = true; break; }
    if (mine) continue;
    int rq = q.RenderQueue;
    if (rq >= 3130 && rq <= 3132 && rq > winChromeQ) winChromeQ = rq;   // 压暗/面板/填色
    if (rq == 3133 && rq > winContentQ) winContentQ = rq;              // 内容
}
CheckTrue(winChromeQ > 0, "（前提）本窗量得到压暗 / 面板 / 填色那三档…");
CheckTrue(winContentQ > 0, "（前提）本窗量得到**内容**档…");
for (int i = 0; i < 3; i++)
{
    … var bgL = FindChild(rowN2, "slider_bg"); var flL = …("slider_fill"); var hdL = …("slider_handle");
    int qBg = …(slider_bg 子 quad 的队列)…, qFl = …, qHd = hdq.RenderQueue;
    CheckTrue(qBg > winChromeQ,  $"{who} 的**轨道队列 {qBg} > 本窗压暗/面板/填色最高档 {winChromeQ}**…");
    CheckTrue(qBg >= winContentQ, $"{who} 的**轨道队列 {qBg} ≥ 本窗内容档 {winContentQ}**…");
    CheckTrue(qFl > qBg,   $"{who} 的**填条队列 {qFl} > 轨道 {qBg}**（同队列时填条会被轨道盖住）");
    CheckTrue(qHd > qFl,   $"{who} 的**手柄队列 {qHd} > 填条 {qFl}**（手柄要在最上层）");

    // 端帽：取该层里窄于 200px 的那几块子 quad 的宽（九宫格左右端帽），量法同 A168 那条
    CheckNear(capBg, 82.8f, 2f,  "轨道九宫格的**端帽**宽 = 原版 `m_Border 184 ÷ ppuMul 2` = 92 设计 px × 0.9 ⇒ **82.8**");
    s.SetValue(1f, false);                      // ⚠️ fire:false —— 自检不许改总线/存档
    CheckNear(capFl, 13.5f, 1.5f, "填条的**端帽**宽 = 原版 `30 ÷ ppuMul 2` = 15 设计 px × 0.9 ⇒ **13.5**");

    // 手柄：实画边长 + 值 0 时的中心 + 行程
    CheckNearPx(hdq.WorldW * 108f, 20.17f, "手柄的**实画宽** = 原版 22.406 × 0.9");
    CheckNearPx(hdq.WorldH * 108f, 20.17f, "手柄的**实画高** = 原版 22.406 × 0.9（方图 ⇒ 宽高相等）");
    float hx0 = (s.HandleWorldPos.x - s.LeftWorld.x) * 108f;   // 值 1 那一帧
    s.SetValue(0f, false);
    float hxAt0 = (s.HandleWorldPos.x - s.LeftWorld.x) * 108f;
    s.SetValue(handleV0, false);                                // 还原（不 fire）
    CheckNear(hxAt0, 10.8f, 0.6f,  "值 0 时手柄中心 = 轨道左端 + 原版 `m_AnchoredPosition.x` 12 × 0.9 ⇒ **+10.8**");
    CheckNear(hx0 - hxAt0, 606.78f, 2f, "手柄的**行程**（值 0 → 值 1）= 原版滑区 (684.195 − 10) × 0.9 ⇒ **606.78**");
}
```

**期望值怎么来的（全部是原版数，不是我们的常量）**：
* 82.8 = `184 ÷ 2 × 0.9`（border 与 ppuMul 都实读，0.9 = `SettingsWindow.RootScale` = 原版根 `m_LocalScale`）；
* 13.5 = `30 ÷ 2 × 0.9`；
* 20.17 = `22.406 × 0.9`（22.406 = 原版 `Handle.m_SizeDelta.y`，实读；110×110 方图 + preserveAspect ⇒ 取短边）；
* +10.8 = `11.99988 × 0.9`（原版 `m_AnchoredPosition.x` 字面量）；
* 606.78 = `(684.195 − 10) × 0.9`（原版 `Handle Slide Area` 宽 = 组宽 684.195 + `m_SizeDelta.x(−10)`）。
  ⚠️ **我们这条路实得 605.77**（少 1.00 画布 px）：本件的 `TravelRightPx = 10` 是**画布 px**，而原版那个 10 是**设计 px**（⇒ 设置窗应为 9）。
  容差 2 盖住它，并在注释里写清；这条钉的是"行程 = 整根滑区"（改成 0 / 半根就红）。**同族问题记在 §附录·3。**

### 2.4 逐条"怎么改坏就红"

| 断言 | 怎么改坏就红 |
|---|---|
| 轨道队列 > 棚三档 | 退回 `WfSlider` 内部硬编码 3000 ⇒ `3000 > 3131/3132` 假 ⇒ 红（**这就是本件修的那个真缺陷**） |
| 轨道队列 ≥ 内容档 | 把队列压到内容层之下（如 `queue: QPanel`）⇒ 红 |
| 填条 > 轨道 / 手柄 > 填条 | 三层都用同一个 `queue`（= 退回"靠 z 排"）⇒ 红（同队列时填条会被轨道盖住，见 `CLAUDE.md` §三） |
| 端帽 82.8 | 端帽传 184（A169 前的做法）⇒ 184 ⇒ 红；只除 2 没乘 `capScale` ⇒ 92 ⇒ 红；把 `capScale` 传 1 ⇒ 92 ⇒ 红 |
| 端帽 13.5 | 传 30 ⇒ 30 ⇒ 红；只除 2 ⇒ 15 ⇒ 红 |
| 手柄 20.17 | 传**裸** 22.406（不过 `RootScale`）⇒ 22.406 ⇒ 红（比原版大 11%） |
| 手柄中心 +10.8 | `Layout()` 里删掉 `_handleOffsetU`（= A169 前的做法）⇒ 0 ⇒ 红 |
| 行程 606.78±2 | `TravelRightPx/U` 项被删 ⇒ 行程变 615.77 ⇒ 红；写成半根 ⇒ 红 |
| 三条"前提" | 窗口少了压暗/面板/内容档 ⇒ 红（提醒"下面几条等于没验"） |

### 2.5 静默失败（验收标准 ⑥）怎么落

* 三条**必传**（无默认值）⇒ 调用方漏给 = **编译不过**，不可能默默用默认顶上；
* 仍然给了运行时兜底告警（越界才响）：`queue < 1`（会落到 3000 以下 = 被自家底板/压暗层盖住）、`handlePx <= 0`（手柄画不出来）、`capScale <= 0`（端帽画成 0）—— 每条都报出**哪个滑块 + 该传什么**。

### 2.6 与另外几个宿主的相容性（静态核过，没跑 Unity）

* `Editor/BattleScene.cs:5438 / 5441`（战斗侧原来那两条）：`Contains(HandleWorldPos)` 现在成立（见 2.1 末 + §附录·5）；
  `5443-5446`（拖到最左 = 0 / 最右 = 1）、`5456`（中点 = 0.5）、`5462`（面板外接不住）**都不受影响**
  （映射公式没动：值域仍锚在轨道左沿、分母仍是 `轨道宽 − 10`，只是手柄位置右移了 12 设计 px）。
* `Editor/SettingsScene.cs` 音频页**原有**那几条：轨道宽（`LeftWorld/RightWorld`）、两层高（`TrackWorldH` / `slider_fill` 并集高）、
  滑块中心（`WorldPos`）、音频总线往返 —— **一条都不动**（端帽只在九宫格内部把中段/端帽的比例改了，**外框矩形不变**）。
* 战斗侧的层序变化（填条/手柄 3000 → 3001/3002）见 §三·1（没跑实机的那一条）。

---

## 三、没查清的部分

1. **战斗侧三层抬档之后，与"同时开着"的其它 3000 档窗口的相对层序没跑实机**。事实层面：本件把战斗那三根从
   "三层都在 3000 + 靠 z" 改成 "**3000 / 3001 / 3002**"，而战斗现场同一档（3000）的还有 `WaitBanner.BattleQChrome`、
   `CardDisplayWindow.QChrome`、棋盘上的卡（`BattleScene.cs:1592` 那句"卡的 3000"）。**理论上**：设置面板开着时指针被它吃掉
   （`BattleDriver.cs:1964-1969`），别的窗口开不出来；但"先开别的窗口、再开设置面板"时那三层的相对先后**没验证**。
   **还差**：真 Play / 实拍对照（`资料/真Play待验清单.md` 的 B/C 类）。⛔ 没有据此改任何判断。
2. **手柄的纵向位置**仍是文件头写着的"我们挑的（竖直居中）" —— 本件**没动**。原版 RT 实读 `m_AnchoredPosition.y = 6.103515625e-05`
   （锚 y 在 FPS 那根是 0→1、音频九根是 0→0），**看不出该居中还是贴底**（判据本来就不足，A168 也标了"编辑器残留"）。**还差**：
   跑原版实况量一帧（或按 VA 反汇编 `Slider.UpdateVisuals` 那一段确认 anchors 的 y 分支）。
3. **端帽的"像不像"没并排渲过**（本批没跑 Unity，按简报）。量的是几何（82.8 / 13.5），与 A168 那根一样；
   真验收要 `CardFaceProbe` 那种并排（本件不涉及卡面，用 `SettingsScene` 的截图里那三根即可）。
4. **§附录·1 那 +5 的偏离**：判据是清楚的（三条 RT 字段 + uGUI `UpdateVisuals`），但**本件按简报口径没有实现**（只做 +12）。
   要不要一起做、怎么做（含 A168 那根与它的三条期望值）**留给调度台裁**。

---

## 四、改动清单

| # | 文件（绝对路径） | 改了什么 |
|---|---|---|
| 1 | `d:/4/Unity/MyGame/Assets/CardPresentation/Battle/WfSlider.cs` | 文件头补 A169 四条 + 一条已知偏离；常量组重排（新增 `HandleFrameW/HandleFrameH` · `HandleOffsetPx` · `BarBorderPx/FillBorderPx` · `PpuMul`，`QSlider` 改 public 且不再内部使用）；`Create` 加 4 个**必传**形参 + 3 条越界告警；三层队列按 `queue/+1/+2`、端帽按 `÷PpuMul×capScale`；`Layout` 补 `_handleOffsetU`；`Contains` 让出手柄探出轨道那一段 |
| 2 | `d:/4/Unity/MyGame/Assets/CardPresentation/Battle/SettingsPanel.cs` | `:230-248` 三个调用点传 `queue: QPanel` / `handlePx: WfSlider.HandlePx` / `handleOffset: WfSlider.HandleOffsetPx` / `capScale: 1f` + 理由注释（**只动这一段**） |
| 3 | `d:/4/Unity/MyGame/Assets/CardPresentation/Shell/SettingsWindow.cs` | `:1065-1079` 音频页调用点传 `queue: QContent` / `handlePx: 22.406*RootScale` / `handleOffset: 11.99988*RootScale` / `capScale: RootScale` + 理由注释（**只动这一处**） |
| 4 | `d:/4/Unity/MyGame/Assets/CardPresentation/Editor/SettingsScene.cs` | `:1169-1279` **纯新增**一节断言（队列 3 条/根 + 端帽 2 条 + 手柄 3 条 + 前提 2 条 ≈ **30 条调用**）—— `git diff --numstat` = `114 / 0` |
| 5 | `d:/4/Unity/资料/普查产出_1007/波8_A169_WfSlider.md` | 本报告 |

**行尾**：四个文件 `HEAD` 都是纯 LF，改完仍是纯 LF（`b.count(b'\r\n') = 0`，LF 计数与文件行数一致 ⇒ 没被翻）。
**`git diff --numstat`**：`WfSlider.cs 131/22` · `SettingsPanel.cs 16/3` · `SettingsWindow.cs 14/0` · `SettingsScene.cs 114/0`
（都不是"整篇重写"的量级；别的文件在我这一轮里**一个字没碰**）。

---

## 附录 · 顺手发现（**只报，没改**）

1. 🔴 **手柄真值起点是"轨道左 + 5 + 12 = +17"，不是 +12**（**要做**，铁律 11 —— 本件按简报口径只做了 +12）。
   判据：`Handle Slide Area` 的 `m_SizeDelta.x = −10` + `m_AnchoredPosition.x = −4.9998779296875`（锚 (0,0)-(1,1) ⇒ **居中**）
   ⇒ 滑区左沿 = 轨道左 + **5**；uGUI `Slider.UpdateVisuals` 把手柄锚点写成 `lerp(滑区.min, 滑区.max, 值)` 且**保留** `anchoredPosition`(12)
   ⇒ 手柄中心 = 轨道左 **+17** + 值 × 滑区宽。
   **影响面**：`WfSlider`（音频九根）+ **A168 那根 FPS 滑块**（`SettingsWindow.PlaceFps` 也是 `+12`）—— 两处都少 5 设计 px
   （设置窗屏上 = 4.5 画布 px，约手柄宽的 14%）。**要一起做**：两处代码 + A168 那三条期望值（`853.37/1069.90/1286.43` 各 +4.5）
   + `Contains` 的让位区 + 本报告 2.3 里我那两条手柄断言。
2. 🔴 **取值映射（指针 → 值）同样少了那 5，而且分母也差一点**：原版 `UpdateDrag` 用 `m_HandleContainerRect`（**滑区**）当 `clickRect`
   ⇒ 值的 0 在**滑区左沿**（轨道左 + 5）、分母 = 滑区宽 674.195 设计 px；我们锚在**轨道左沿**、分母 = "轨道宽 − 10"。
   ⇒ 平移差 5 设计 px + 设置窗那条路上分母差 1 画布 px（我们用 10，原版 9）。**要做**，与第 1 条同批（改 `SetFromPointer` 的起点/分母，
   并核 `BattleScene` 那三条（拖到最左/最右/中点）在新分母下仍绿 —— 静态看仍然绿：两端都被 `Clamp01` 夹住、中点差 0.5% 之内）。
3. 🔴 **`TravelRightPx = 10` 是画布 px，原版那 10 是设计 px**（同一个"本件拿不到实例 scale"的根因）⇒ 设置窗应为 9。
   本件只把**端帽**接上了 `capScale`，**没有**顺手改它（简报没提，改了会让 2.3 那条行程断言的实得值变，也会让第 2 条的分母两级一起动）。
   **要做**：把它并进 `capScale`（或把 `capScale` 正名为"本实例的 设计→画布 倍数"，让端帽与滑区让位都乘它），与第 1、2 条**同批**做。
4. ℹ️ **两处注释已过期，我改不到**（不在白名单）：
   · `Shell/SettingsWindow.cs:769-774`（A168 写的"为什么不复用 `Battle/WfSlider`"）现在说的是 A169 **改掉之前**的事实
     （"队列硬编码 `QSlider = 3000`" / "手柄边长硬编码 22.406" / "要合并的话 = 补 `queue / handlePx / handleOffset` 三个参数"）——
     A169 后**三条都变了**，而且参数是**四个**（多 `capScale`）。请调度台在后续碰这个文件时顺手订正（判据 = 本报告 §2.2）。
   · `Editor/BattleScene.cs:5391-5393` 与 `Editor/SettingsScene.cs:1147-1149` 说"`Battle/WfSlider.cs` 不在白名单 ⇒ 按层名取 `slider_fill`" ——
     那是 A130/A125② 当时的口径，**结论仍然成立**（本件**没**给 `Fill` 加访问器，故意不动那两个宿主）。若将来加访问器，这两处要一起改。
5. 🔴 **`Editor/BattleScene.cs:5438` 那条断言原来"只靠 bug 才绿"**：「指针落在音乐滑块的手柄上 ⇒ 命中判定打得中」。
   手柄少了 +12 时，值 ≈1 的手柄中心 = 轨道右端**内** 10 px（`|x| = w/2 − 10/108 < w/2`）⇒ 恰好命中；一旦补上 +12 就落到轨道外 ⇒ 若不改
   `Contains` 会**红**。⚠️ 而且它的有效性**取决于本机存着的音量**（`WarpforgeAudio.Music` 默认 1、会写 `PlayerPrefs`）——
   音量不是 ~1 时它本来就分辨不出（弱断言）。本件给 `Contains` 补了"手柄那一块"（= 与原版同构：九根 Handle Image `m_RaycastTarget = 1`），
   它现在**与值无关**都成立。**建议**（另开一条）：把那条断言写成"值 = 1 那一帧也打得中"（显式 `SetValue(1f, false)` 后再测），别靠机器上的存档值。
6. ℹ️ **`Create` 的 `trackW`/`trackH` 仍带默认值**（A96 的约定，战斗那条路用它），而新加的四样**必传** ——
   两种口径混在同一个签名里。不是缺陷（`trackW/TrackH` 的默认值就是战斗那份、有出处），但下次有人加参数时要知道这条分界。
