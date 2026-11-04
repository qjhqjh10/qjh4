# B140-WL 报告 — EventSystem 两件参数级修复（2026-11-04）

执行代理 `B140-WL`。判据 = `判据/B140_对账_G4_B139新账.md` 的 `B139-Y4` 行 · `方案_审计修复总方案_B138.md` §4·2 #3。
原版真值（现读 `d:/2/tools/assetripper_out/ExportedProject/Assets/Data/Scenes/Intro.unity`）：
`:5428 m_sendNavigationEvents: 0` · `:5429 m_DragThreshold: 5`（同一只 `EventSystem` 的两行）。

## 一、改了什么（逐处 · 改前 → 改后）

| # | 文件:行号 | 改前 | 改后 |
|---|---|---|---|
| 1 | `Battle/BattleEventSystem.cs:164`（③ 现建路） | `var es = …AddComponent<EventSystem>(); es.pixelDragThreshold = …;` | 同句追加 `es.sendNavigationEvents = false;`（**同一物理行**，行数零变化） |
| 2 | `Battle/BattleEventSystem.cs:133`（① 收养 `current`） | 只设 `pixelDragThreshold` | 追加 `cur.sendNavigationEvents = false;` |
| 3 | `Battle/BattleEventSystem.cs:153`（② 收养场景那只） | 只设 `pixelDragThreshold` | 追加 `pick.sendNavigationEvents = false;` |
| 4 | `Battle/BattleEventSystem.cs:119`（`Ensure()` 的 doc） | 只写「三条路都设 `pixelDragThreshold`」 | 补一句「三条路也都设 `sendNavigationEvents = false`」+ 判据 `:5428` + 为什么（我们的键盘导航走 `PointerLayer.KeyFrame`，两套同时开会一次按键走两步） |
| 5 | `Shell/PointerLayer.cs:98` | `public const float DragThreshold = 10f;` | `5f` |
| 6 | `Shell/PointerLayer.cs:22-23`（类头 ④） | 「越过 **10px**…原版 `m_DragThreshold = 10`，`EventSystem.cs:68`」 | 「越过 **5px**…原版串行值 `m_DragThreshold = 5`，`Intro.unity:5429`；⚠️ 2026-11-04 订正：10 是 **uGUI 默认值**」（保留更正痕迹，铁律 5） |
| 7 | `Shell/PointerLayer.cs:96-97`（常量 doc） | 「原版 …的**默认值**（`EventSystem.cs:68`）」 | 「原版 …的**序列化值 = 5**（`Intro.unity:5429`）」+ 同一句更正痕迹 |
| 8 | `Shell/PointerLayer.cs:436-438`（`Update` 的量纲副作用注） | 「`DragThreshold`（10px）」「10 设计 px 比 10×108 帧 px」 | 5px / 5 设计 px / 5×108（3 行就地改写，行数零变化） |
| 9 | `Shell/PointerLayer.cs:449`（`Update` ②） | 「照原版 EventSystem 的 10px 阈值」 | 「5px 阈值，`Intro.unity:5429`」 |
| 10 | `Editor/RewardsScene.cs:4354`（该组判据注） | 「拖拽阈值 = `m_DragThreshold = 10`（`EventSystem.cs:68`）」 | 「= 5（原版串行值 `Intro.unity:5429`；⚠️ 订正：10 是 uGUI 默认值）」 |
| 11 | `Editor/RewardsScene.cs:4394` | `dragP1 = …(1131f, 700f)`（按下 +6px） | `(1129f, 700f)`（按下 **+4px**） |
| 12 | `Editor/RewardsScene.cs:4395` | `dragP2 = …(1160f, 700f)`（+35px） | `(1131f, 700f)`（**+6px**） |
| 13 | `Editor/RewardsScene.cs:4401-4404`（文案） | 「挪 **6px** ⇒ 还没到 10」「挪过 10px ⇒ 拖拽（原版 = 10）」 | 「只挪 **4px** ⇒ 还没到 5」「挪到 **6px** ⇒ 已过 5、转成拖拽（原版 = 5，`Intro.unity:5429`）」 |

行数守恒：`git diff --numstat` 三个改动文件 = `4/4` · `5/5` · `7/7`（增删相等 ⇒ 零行数变化，账的行号落点不漂）。
行尾：二进制核过三个文件 + 新文件，**CRLF 0 / 纯 LF / 裸 CR 0**（未翻）。

## 二、原版值 vs 我们值

| 参数 | 原版 | 改前 | 改后 |
|---|---|---|---|
| `m_sendNavigationEvents` | **0**（`Intro.unity:5428`） | 默认 `true`（全仓 `grep sendNavigationEvents` = 0 命中） | `false`（三条路都设） |
| `m_DragThreshold`（外壳指针层） | **5**（`Intro.unity:5429`） | `10`（= uGUI 字段初值 `EventSystem.cs:68`） | `5` |
| `pixelDragThreshold`（战斗 EventSystem） | 5 | 已是 5（`B124-W10` 2026-11-03 落的，三路齐全） | 不动 |

## 三、断言（新建 `Assets/CardPresentation/Editor/B140_WL_Assert.cs` + `.meta`）

全局命名空间 · 类名 == 文件名 · `public static void Run()` · 零前提（自带临时 `EventSystem`，收尾 `DestroyImmediate` + `ResetForTest()`）。
**先断前提、再断结论**，没有一条 `if (x != null) Check(...)`。

| 条 | 断什么 | 灭自证 |
|---|---|---|
| A | `PointerLayer.DragThreshold == 5f` | 改回 `10f` ⇒ 红 |
| A1 | `DragThreshold < 10f` | 改回 uGUI 默认（≥10）⇒ A/A1 双红 |
| C1 / C2 | 4px 仍在阈值内 / 6px 已越阈值（= `RewardsScene` 那两条样本的判别力） | 阈值被设成 ≥6 或 ≤4 ⇒ 对应条红 |
| B0 | **硬前提**：裸 `EventSystem` 出厂 `sendNavigationEvents == true`（uGUI `EventSystem.cs:56`） | 前提不成立 ⇒ 报红，不许静默跳过 |
| B0b | **硬前提**：`Ensure()` 现建出那只 | 拿不到 ⇒ 红 |
| B | ③ 现建路 `sendNavigationEvents == false` | 删掉该句 ⇒ 停在 true ⇒ 红 |
| B1 | 同一只的 `pixelDragThreshold` 仍 = 5（与 `B124_W10` 同源，顺带守一眼） | —— |
| D0 / D0b | **硬前提**：外部那只此刻是 `true` 且就是 `EventSystem.current` | 前提不成立 ⇒ 红（不静默） |
| D | ① 收养路把它从 `true` 改成 `false` | 删掉 ①/② 那两句 ⇒ 仍旧 true ⇒ 红（**Play 模式走的就是这条**） |

合计 **11 条 Check**，其中 **7 条灭自证/硬前提**（A1 · C1 · C2 · B0 · B0b · D0 · D0b）。

## 四、typecheck（`TMPDIR=/tmp/wf_b140wl bash 工具/typecheck.sh`）

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
（首跑时编辑器侧有 4 条 `CS1061`，全部落在 `Assets/RuleEngine/Editor/B140_WA_Assert.cs` —— 别的写手当时正在写（mtime 早 28 秒）；等他写完复跑 ⇒ 0/0，与本包无关。）

## 五、没做的 / 需要调度台接手的

1. **`PointerLayer.cs` 里没有 `EventSystem` 可补** —— 派活单假设「PointerLayer 里创建 EventSystem 的那一处」，现读全仓 `AddComponent<EventSystem>` **只有 1 个生产点** = `Battle/BattleEventSystem.cs:162`（另一处在 `Editor/B124_W10_Assert.cs:98`，是夹具）；`Shell/` 全目录 `grep EventSystem` 无组件创建（`PointerLayer` 是自建轮询输入，**不用 UGUI EventSystem**）。⇒ **战斗与外壳共用同一只 EventSystem**，所以覆盖 `Ensure()` 的三条路 = 两条路都覆盖；断言 D 就断在「收养路」（Play 模式 `current` 已存在时走的那条）。
2. **账：`sendNavigationEvents` 同键已存在** —— `账/战斗界面_HUD.tsv:828`（`EventSystem.set_sendNavigationEvents(bool)`，状态 `C`，备注原文写着「转交 Battle/PointerLayer 所属包：在 `AddComponent<EventSystem>()` 之后加 `es.sendNavigationEvents = false`」）。按派活口径**未新增**，请调度台把该行 C→A、落点补 `Battle/BattleEventSystem.cs:133/153/164`、差异改「已一致」、备注加「2026-11-04 B140-WL 落地 · 断言 `Editor/B140_WL_Assert.cs` B/D」。
3. **账：新键** = `账/_parts/B140_WL.tsv`（`菜单/外壳 · EventSystem · set_pixelDragThreshold(int)`，A 档，落点 `Shell/PointerLayer.cs:1446（MoveTo）`，11 列 · 裸 tab）。⛔ 未动账主表。
4. **全仓同类过期文案（不在我的白名单内，未改，请派给对应写手）**：
   - `Shell/MainMenuRuntime.cs:2657`「`PointerLayer` 那条 10px 阈值照旧」
   - `Shell/SettingsWindow.cs:5181`「只看 10px 阈值」
   - `Deck/DeckRuntime.cs:1409`「按下越过 10px 判成拖拽」
   - `Editor/ShellScene.cs:10740`「⑤ 越过 10px ⇒ 进入拖动」（断言**文案**；样本离阈值很远，逻辑不受影响）
   以上都是**文案/注释**，不是逻辑缺陷（逻辑读的是常量）⇒ 改阈值后它们说的是旧值，建议同批扫掉。
5. **未跑 Unity**（红线）：`RewardsScene.Run` 与两个 EventSystem 断言都只做了静态核对 + typecheck；首次宿主运行时看行首 `B140WL ✓ / B140WL ✗`。
6. ⚠️ 如实记：`Editor/RewardsScene.cs:4394-4395` 的样本从「+6 / +35 设计 px」改成「+4 / +6 设计 px」（阈值 5 下才有判别力）。这里 `k == 1`（`MenuDraw.PointerFramePxFromDesign` 的 doc 自陈「今天全部调用点走的都是这一档」）⇒ 设计距 = 指针帧距，4/6 精确；`DragTo` 是线性、无死区，6px 仍会改 `Offset`（原来 35px 能过 ⇒ 该方向不在夹边）。
