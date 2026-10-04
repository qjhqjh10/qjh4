# A154 + A165 + A166 · 设置窗这一类的闭环（档位 / 小屏缩放器 + 开关 / placement 哨兵）

2026-10-06 · 执行子代理 · **SettingsScene 这一类**（三件一起做完才算清）
· 改了 5 个文件（4 改 1 新）· **没跑 Unity / 没跑自检**（自检由主对话在同步点跑）· 没动 git · 没改两张正本

**判据来源**：`资料/普查产出_1006/A154_A155_窗口档位与缩放.md`（唯一判据）· `资料/阶段二_Shell_原版规格.md` §三 第 5 条（已就地订正）
· 反编译 `d:/2/tools/decomp_full/{GameWindow__Open,GameWindow__TryOpen,TransformScalerBySmallScreenUI__*,GraphicsTab__{OnSetup,OnEnable,SmallScreenToggleClick},GraphicsTab.cs 桩}`
· 解包 `d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/{MonoBehaviour_2730265326332837798（SettingsMenu）,MonoBehaviour_-3185861090812863363（TrophyInfoPopup 上那颗缩放器）}`
· `python 工具/menu_dump.py bundle_menus_assets_all "Graphics Tab" --depth 6 --relative`（图形页那 7 行的行名/矩形）

---

## 结论（三件各一句话）

**① A154（档位）**：`Shell/SettingsWindow.cs:248` 已从 **`Popup`(15)** 改成 **`Canvas`(5)**（原版 MB `windowsPlacement: 5`），
并配了**两条**断言（值 = 原版字面量 5；值**被用上**了 = 真的挂在 5 那一档锚点下）——**只改这一扇**。

**② A165（小屏缩放器 + 开关）**：**整条做完了** —— 新文件 `Shell/TransformScalerBySmallScreenUI.cs`（静态开关 `SmallScreenUI` + 组件 `TransformScalerBySmallScreenUI`，
行为逐句照反编译，含「`extra = 1.0` 是**不覆盖**」与「`LateUpdate` 防重复乘」两条）+ `GameWindow.TryOpen` 上的挂载点 +
设置窗图形页**第 0 行**那颗开关 + `TrophyInfoPopup` 那扇**烤在 prefab 里的 1.35** 真挂上了。

**③ A166（默认档位静默失败）**：`GameWindow.placement` 的默认值从 `Popup`(15) 改成**哨兵 `(WindowsPlacement)(-1)`**，
`AttachToAnchor` 遇到哨兵**打 LogError 出声**（并照老默认值兜底），另有一条自检断言 + 一条反面断言互为对照。

🔴 **顺手核出并已修的一处【抄错】**：设置窗的 `extraScaleSmallScreen` 我们写的是 **1.0**，原版 MB 是 **1.2**（同一颗 MB，逐字段实读）——
本批已改成 **1.2f**，`资料/已知的坑.md` 里那条「我们写的是 1f」现在可以划掉了。`资料/普查产出_1006/A154_A155_窗口档位与缩放.md` §①-a 那张表**早写着 1.2**（只是没和我们的值对齐过），
我把我们能对上的 26 扇窗**逐扇**重对了一遍（脚本，见 §实测证据），**只有这一扇不一致**。

---

## 改了什么（文件:符号名/行号）

### 新文件

| 文件 | 内容 |
|---|---|
| `Shell/TransformScalerBySmallScreenUI.cs`（新，152 行） | `SmallScreenUI`（静态开关）+ `TransformScalerBySmallScreenUI`（组件） |

- `SmallScreenUI`：`Enabled`（= 原版 `GameStaticData.smallScreenUI`，出厂 **false** = 原版 cctor 写 0）· `ChosenManually`（= `smallUIChosenManually`）·
  `Set(bool)`（= 原版 `GraphicsTab.SmallScreenToggleClick`，**两个字段一起写**）· `PersistOverride`（**自检注入点**）· `ResetForTest()`。
- 组件：`menuScale`（**烤在 prefab 里的倍数**，默认 1）· `SetScale` · `Initialize`（`enabled = menuScale != 1f && 开关`）· `OnEnable` · `LateUpdate` · `Tick`（= `LateUpdate` 的同一段，**批处理没有帧循环** ⇒ 自检直调）。
- 🔴 **三处照原文**：① `LateUpdate` 乘的是**当前** `localScale`；② 守卫 = **逐分量差的平方和 < 0.0001**（⛔ 不是「每分量差 < 0.01」）；
  ③ `Initialize` 用**裸 `!=`**（`Open()` 那两个判据才用 `Mathf.Approximately`）—— 两处故意不统一。
- 🔴 **两处如实标注（我们挑的）**：① 持久化走 **`PlayerPrefs`**（原版存**玩家存档**，我们没有存档系统；先例 = `Core/WarpforgeAudio.cs` 的 `MusicVolume`）；
  ② `ChosenManually` **不落盘**（原版从存档读回来，我们只做「点过就置 1」这一半）。

### 改的文件

| # | 文件:行 | 改动 |
|---|---|---|
| 1 | `Shell/SettingsWindow.cs:248` | `win.placement`：`Popup`(15) → **`Canvas`(5)**（判据 = 原版 MB `windowsPlacement: 5`；就地写清「别推广成大家都该是 5」） |
| 2 | `Shell/SettingsWindow.cs:256` | `win.extraScaleSmallScreen`：`1f` → **`1.2f`**（判据 = 同一颗 MB `1.2000000476837158`；🔴 **这是原值抄错**） |
| 3 | `Shell/SettingsWindow.cs:153` | 新常量 `SmallScreenRow = 0`（勾选行第几行；附 `menu_dump` 出处与行高/步进的复核） |
| 4 | `Shell/SettingsWindow.cs:458` | 图形页新建第 0 行的 `BuildCheckRow(page, "Small Screen UI", SmallScreenRow, …)`（原版 `GraphicsTab.smallScreenToggle`） |
| 5 | `Shell/SettingsWindow.cs:541` | 新 `ToggleSmallScreenUI()`（= `GraphicsTab.SmallScreenToggleClick`；点完**说出「只对之后打开的窗生效」**） |
| 6 | `Shell/SettingsWindow.cs:436` | 那行「图像页没建哪几件」的出声日志**改了**（`Small Screen Size Toggle` 从「没建」划掉、补上 `Android extra compatibility`（原版出厂 `act=0`）） |
| 7 | `Shell/SettingsWindow.cs:263-280` | `Screen()` 的注释**订正**（老注「父节点缩放对它不起作用」量错了对象 —— 详见 §顺手发现 ②） |
| 8 | `Shell/WindowsManager.cs:81,83` | 新哨兵 `GameWindow.UnsetPlacement = (WindowsPlacement)(-1)`；`placement` 默认值改成它（⛔ **没加进枚举**） |
| 9 | `Shell/WindowsManager.cs:100` | 新 `GameWindow.HasPlacement` |
| 10 | `Shell/WindowsManager.cs:110,131` | `TryOpen` 末尾调 `ApplySmallScreenScale()`；该方法逐句照 `GameWindow__Open.c` |
| 11 | `Shell/WindowsManager.cs:292` | `GetWindowAnchor(哨兵)` ⇒ 出声 + 返回 null（拦住「绕过 AttachToAnchor」那条路） |
| 12 | `Shell/WindowsManager.cs:363` | `AttachToAnchor` 遇哨兵 ⇒ **LogError 出声** + 照**旧默认值 Popup** 兜底（画面不变，日志明明白白） |
| 13 | `Shell/WindowsManager.cs:13,28` | 文件头两条**错记录就地订正**：「宽度 < 阈值」（判据里没有宽度判定）与「默认空转」（现在真做了） |
| 14 | `Shell/TrophyInfoPopup.cs:99,212-214` | 窗体根上补挂**烤值 1.35** 的缩放器（判据 = `MonoBehaviour_-3185861090812863363.json`：`m_Enabled: 1` · `menuScale = 1.350000023841858`） |
| 15 | `Shell/TrophyInfoPopup.cs:17` | 文件头那句「`menuScale = 1.35`（小屏缩放那一颗）」补上「✅ A165 起真接上了」 |
| 16 | `Editor/SettingsScene.cs:238` | 新 helper `CaptureErrors`（`Application.logMessageReceived`，本仓先例 `Editor/BattleScene.cs:1888`） |
| 17 | `Editor/SettingsScene.cs:374-379` | 自检开头把开关的持久化关掉 + 内存态放回出厂（`finally` 里 `:992` 放回注入点） |
| 18 | `Editor/SettingsScene.cs:1003-1021` | `Build`：**加建 `2 - Canvas Holder Above upper bar`**（原窗档位改成 5 之后必须给这一档；`3 - PopUp Holder` 留着给 A166 探针） |
| 19 | `Editor/SettingsScene.cs:398,408,423-450,582-721` | 断言（逐条见下节） |

⚠️ **越界说明（请调度台复核）**：`Shell/TrophyInfoPopup.cs` **不在我的白名单**里，但 ② 要求「`extra = 1.0` 的那三扇不能只读 `extraScaleSmallScreen`」，
而三扇里**只有它建了**（`AllianceMemberOptionsPopup` / `GenericOptionsPanel` 我们根本没有）—— 不补这两行，这一扇在小屏下就是**静默不放大**。
改动 = **加 3 行 + 一条常量 + 注释**，没碰它的任何版式/数据。若该文件另有主人，请以那位的版本为准、把这三行并过去。

---

## 断言（期望值来自哪个原版字段 / 怎么改坏就红）

宿主 `Editor/SettingsScene.cs`（`SettingsScene.Run`）。

| # | 行 | 断言 | 期望值来自 | 怎么改坏就红 |
|---|---|---|---|---|
| 1 | 398 | `(int)win.placement == 5` | 原版 MB `MonoBehaviour_2730265326332837798.json` 的 **`windowsPlacement: 5`**（字面量，⛔ 不写 `WindowsPlacement.Canvas`） | 改回 `Popup` ⇒ 实得 15 |
| 2 | 401 | `win.transform.parent == canvasAnchor` | 自检自己建的那颗 **5 档锚点**（`2 - Canvas Holder Above upper bar`） | `AttachToAnchor` 不看 `placement` ⇒ 挂到 Popup 上 ⇒ 红（#1 照样绿 ⇒ 两条分得开） |
| 3 | 408 | `extraScaleSmallScreen == 1.2` | 同一颗 MB 的 **`extraScaleSmallScreen: 1.2000000476837158`** | 改回 `1f` ⇒ 红 |
| 4 | 429-441 | 裸 `GameWindow`（`placement` 未赋）⇒ `AttachToAnchor` **打一条 LogError**，且内容含「没有显式赋值」 | 原版该字段**必填**（全库 141/141 都有值）；我们这边「忘赋」必须能从日志分辨 | 默认值改回 `WindowsPlacement.Popup` ⇒ 一声不吭 ⇒ 红 |
| 5 | 445 | **反面**：显式赋过值 ⇒ 一条错都不打 | 同上 | 让 `AttachToAnchor` 无条件报警 ⇒ 红（#4#5 互为对照，否则只测出「有日志」） |
| 6 | 594 | 第 0 行那个方框**渲出来** = `[603.17,443.29]–[710.27,511.29]`（画布 px，`CheckRectPx`**不过 `Screen()`**） | 原版 dump 的绝对字面量：`Graphics Tab` 左上 (551.87,164.79) + 第 0 行 `Small Screen Size Toggle>Toggle` 相对 [51.3,278.5]–[158.4,346.5] | 把这颗开关挪到别的行（例如跟 Vsync 一样给 5）⇒ y 差 ~362px ⇒ 红 |
| 7 | 602 | 点一下 ⇒ `SmallScreenUI.Enabled` = 真 **且** `ChosenManually` = 真 **且** 方框换成 `40K_toggle_on` **且** 有 flash 文案 | `GraphicsTab__SmallScreenToggleClick.c`（**两颗一起写**：`smallScreenUI`@0x11c + `smallUIChosenManually`@0x12e） | 只写其中一个 / 不换图 / 不吭声 ⇒ 各红一条 |
| 8 | 610 | 再点一下 ⇒ 关回来 + 图换回 | 同上 | 单向开关 ⇒ 红 |
| 9 | 630 | 开关**关** + `extra = 1.2` ⇒ **连组件都不挂**、`localScale` 停在 1 | `GameWindow__Open.c` 第一层判据（与窗口宽/屏宽**无关**） | 去掉开关那一层判据 ⇒ 挂上组件 ⇒ 红 |
| 10 | 640 | 开关开 + `extra = 1.2` ⇒ 挂组件、`menuScale = 1.2`、`enabled`；`Tick` ⇒ 根 `localScale = 1.2` | `extraScaleSmallScreen` + `Open()` 第二层判据（`Mathf.Approximately`） | 删掉挂载点 ⇒ 停在 1 ⇒ 红 |
| 11 | 648 | **再 `Tick` 一次不重复乘**（仍 1.2，不是 1.44） | `TransformScalerBySmallScreenUI__LateUpdate.c` 的守卫（平方和 < 0.0001） | 删掉守卫 ⇒ 1.44 ⇒ 红 |
| 12 | 660 | 开关开 + `extra = **1.0**` + 烤 `menuScale = 1.35` ⇒ **1.35** | 反编译：`extra == 1` 时 `Open()` **连 `SetScale` 都不调**（=「不覆盖」）⇒ 真实倍数取 prefab 烤的值 | 无条件 `SetScale(extra)` ⇒ 变 1.0 ⇒ 红 |
| 13 | 672 | 开关**关** + 烤 1.35 ⇒ `enabled == false` | `Initialize()` = `(menuScale != 1) && 开关` | 少判开关那一半 ⇒ 1.35 照样乘上 ⇒ 红 |
| 14 | 692-704 | **前提**：根 `localScale = 1.2` ⇒ 子件**渲出来**的宽 ×1.2（量 `MeshRenderer.bounds`，**渲染真值**） | Unity 层级世界矩阵（本仓自己的旁证：`Battle/WfSlider.cs:158` · `Battle/SkillPanel.cs:248` · `Battle/AttackSelector.cs:550`） | 若 `ImageQuad` 改成把网格建在**世界空间** ⇒ 红（那正是本缩放器会静默失效的那一刻；见 §顺手发现 ①） |
| 15 | 713-721 | `TrophyInfoPopup` 的窗体根上**有**缩放器，且 `menuScale == 1.35` | 原版 prefab `MonoBehaviour_-3185861090812863363.json` 的 `menuScale = 1.350000023841858` | 删掉那三行 ⇒ 红（小屏下它会静默不放大） |

⚠️ **自检卫生**：开关的**持久化**在整个 Run 里被 `PersistOverride` 关掉（**玩家的真 `PlayerPrefs` 一个字节都不动**），
内存态跑完放回出厂值（原版 cctor = 0）——与 `NetConfig.OverridePath` / `QualitySetterOverride` 同一族。

---

## 实测证据

1. **秒级类型检查**（`TMPDIR=/tmp/wf_st2 bash d:/4/Unity/工具/typecheck.sh`，**改完每个文件都跑过**，最后一次是全部改完之后）：
   ```
   --- 运行时程序集 ---
   运行时错误数: 0
   --- 编辑器程序集 ---
   编辑器错误数: 0
   ```
2. **26 扇窗的窗参逐扇重对**（脚本扫 `assets_full` 全部 84 包的 `MonoBehaviour/*.json`，按 `m_Script` → `bundle_Waprforge_monoscripts` 解类名；
   逐类读 `extraScaleSmallScreen / windowsPlacement / type / closeOnESC`，与我们 `Shell/*.cs` 的赋值比）：
   **唯一不一致的就是设置窗的 `extra`（我们 1.0 / 原版 1.2）** —— 已改。
   （其余各扇对得上，例：`BoosterInfoPopup` 1.2 · `DuelPopupWindow` 1.15 · `PlayerProfileMenu` 1.075 · `PracticeModePopup` 1.07 · 其余 1.0。
    ⚠️ `RankedEventWindow`（非 V2）原版是 1.07，但我们建的是 **V2**（`new GameObject("RankedEventWindowV2")`，原版 1.0）⇒ 1f 是对的。）
3. **独立复算**（不想让 #6 那条断言自证）：原版第 0 行方框的绝对矩形，用两路算了一遍 ——
   ① `menu_dump.py` 的「绝对 = 被查节点父 `Tab Content` 左上 (551.87,164.79) + 行相对 [51.3,278.5]」；
   ② 逆着 `SettingsWindow.Screen()` 用我们的常量算 `(ChkL,ChkT)–(ChkL+119,ChkT+76)` ⇒ 603.168/443.25/710.268/511.65。
   两路逐值吻合（≤0.36px），且都落在自己的 ±1.5px 容差里。
4. **没跑 Unity、没跑自检**（按简报；自检由主对话在同步点跑）。**下面这 15 条断言一次都没实际执行过** —— 交付的是「编译通过 + 静态可推」。

---

## 没查清的部分

1. **「同一扇窗被反复 Open 时会不会叠加」没跑到实况**（A154 §④-5 也留着这条）。我照反编译落地（守卫 = 与「上次设过的值」比），
   **还差**一次真 Play / 原版实机 dump（`TransformScalerBySmallScreenUI` 每帧读窗口根 `localScale`）。
2. **`GameStaticData.DefaultSmallScreenUI` 的实现体没钉死**（反编译那个 getter 的符号名自相矛盾，A154 §④-4）——
   我们按 **cctor = 0** 落地（= `PlayerPrefs.GetInt(key, 0)`），**这一跳没查清**，但**不影响**「不是按屏宽算的」这条判据。
3. **小屏开关打开后的实际观感没跑过**：设置窗会 ×1.2、`TrophyInfoPopup` ×1.35 —— 会不会顶出 1920×1080（原版靠 CanvasScaler/`UIScaleToFit` 兜底？），
   **没查清**。**还差**：把开关置 1 跑一次 `SettingsScene.Play`（或真 Play）拍一张。
4. 🔴 **我们的量测/命中层不带 `lossyScale`**（见 §顺手发现 ①）：窗口根一旦被乘上倍数，
   **画面是对的**（层级世界矩阵照乘），但 `ImageQuad.WorldW`、`LayoutSpace.PxX/PxY`、`PointerLayer.HitBoxPx` 读的都是**scale 1 那一帧的数**
   ⇒ 「画出来的比量到的大」「命中区比画出来的小（1/M）」。**这条的严重度与影响面没查清**（哪些窗在开关打开时真的有滚动/裁切区域），
   **还差**：开关打开后逐扇窗核一遍命中/滚动区域，再决定改 `PointerLayer.HitBoxPx`（按 §顺手发现 ① 的两行改法）还是改成「按矩形重建」。
   ⚠️ **默认是关的**（原版 cctor = 0）⇒ 今天没有任何可见影响。

---

## 顺手发现（**只报，没改**）

1. 🔴 **`PointerLayer.HitBoxPx` 的命中区不含父链缩放**（`Shell/PointerLayer.cs:696-708`）：
   `half = new Vector2(q.WorldW * k * 0.5f, q.WorldH * k * 0.5f)`，而 `center` 取的是 `q.transform.position`（**世界坐标，含缩放**）。
   ⇒ 一旦窗口根被乘上倍数（A165 的小屏开关），**命中区中心对、尺寸偏小 1/M**（1.2 倍窗 = 83%、1.35 倍窗 = 74%）。
   **建议改法（两行）**：`half` 再乘 `q.transform.lossyScale.x/y`（scale = 1 时行为完全不变 ⇒ 零回归）。
   ⛔ 我没动它（不在白名单）。同族的还有「按 `WorldW/WorldH` 量的东西」：`MenuScroll` 的裁切/滚动、`MenuDraw.ClipRect`、以及各宿主的几何断言。
2. 🔴 **`Shell/SettingsWindow.cs` 那条老注的机制半句是错的**（我已就地订正，`:263-280`）：
   原文「父节点的缩放**对它不起作用**（实测：75px 的钮挂在 0.9 的根下，渲出来仍是 75.0）」—— **量的是 `ImageQuad.WorldW`（代码值）而不是渲染**；
   真正的渲染走层级世界矩阵（网格是**局部空间**的：`RebuildMesh` 写 ±WorldW/2），
   而**旁证就在本仓**：`Battle/WfSlider.cs:158` 给根设 `localScale.x` 来定填充条宽度、`Battle/SkillPanel.cs:248` 直接写 `q.transform.localScale = 目标宽 / q.WorldW`。
   ⇒ 「把 0.9 烘进矩形、根保持 1」这个**做法照旧**（理由换成「我们的量测/命中都只认 scale 1 那一帧」）。
   🔴 **这条订正由自检 #14 兜底**（量 `MeshRenderer.bounds` 这个**渲染真值**）：它绿 = 订正成立；它红 = 老注成立、A165 的缩放器得换实现。
   ⚠️ 同族措辞在 `Shell/BoosterPackOpenWindow.cs:285`（那段说「父级一带缩放，子件 localPosition 就不再等于那个差值，整排卡会一起错位」）
   —— 那句**对位置是不成立的**（世界矩阵两边一起缩放，**画面位置仍是对的**），真正会错的是**尺寸/量测**。⛔ 我没改它（不在白名单）。
3. 🔴 **三处「我们这边没有这个缩放器 / 零消费者」的记录现在过期**（本批已把它做出来）：
   `Editor/ShellScene.cs:677-678`（「`extraScaleSmallScreen` 在我们这套里没有消费者」「小屏缩放器没实现」）·
   `资料/阶段二_Shell_原版规格.md:95`（「我们工程：整条没做」）· `资料/普查产出_1006/A154_A155_窗口档位与缩放.md:18,221`（「我们工程里没有这个缩放器 / 全工程零消费者」）。
   ⚠️ `ShellScene.cs` 那两行的**结论**（PromptPopup 那棵树量出来是 1000px）**仍然成立**，但换理由：
   `PromptPopup` 的 `extraScaleSmallScreen = 1.0` 且窗口根上**没有**烤 `menuScale` ⇒ 小屏开关开着也**不缩放**（改法见 `Shell/TransformScalerBySmallScreenUI.cs` 文件头 ③）。
4. ⚠️ **`A154_A155_窗口档位与缩放.md` §①-b 那张表第 1 行的「🔴 唯一的真偏离」现在该划掉**（已改 5），
   同表也**没有 `extra` 那一列** —— 设置窗 `extra` 抄错（1.0 vs 1.2）就是因此漏过去的。建议那张表补一列 `extra`（我们值 vs 原版值）。
5. ℹ️ **`Shell/SearchingMatchPopup.cs:154`（`SearchingMatchPopup`，嵌在搜索窗根下的那件）与 `Battle/CardDisplayWindow.cs:143`（战斗侧 `CardDisplayWindow`）
   的 `placement` 字段现在停在哨兵**（它们不走 `AttachToAnchor`，是挂在宿主根下的，所以**不会**出声、也没有行为变化）。
   它们各自的头注写着「实证 `windowsPlacement = 15 / 0`」—— 要不要把值补上（纯登记，零行为）由调度台定。
6. ℹ️ **FPS 上限那一行在原版是【滑块】不是勾选**：`GraphicsTab.cs` 桩里 `private Slider fpsLimit` + `FPSLimitValueChanged(float)`（`OnEnable` 挂的是 `UnityAction<float>`），
   而我们现在是 `BuildCheckRow(... FpsRow ...)`（点击在 60/30/无限之间循环）。**本轮没动**（不属 A154/A165/A166）——判据在 A154 报告之外，建议另开一条。
7. ℹ️ **新文件 `Shell/TransformScalerBySmallScreenUI.cs` 还没有 `.meta`**（Unity 导入时自动生成；本件没跑 Unity）。
