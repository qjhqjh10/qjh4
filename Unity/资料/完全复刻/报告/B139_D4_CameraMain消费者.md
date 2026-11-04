# B139_D4 — `Camera.main` 消费者判定与改造（2026-11-04）

执行代理：`B139-D4` · 工作包 D4 · 30 个文件（⛔ 不碰 `Battle/BattleDriver.cs` / `Editor/BattleScene.cs`，包 D1 独占）

## 〇 一句话结论

相机 tag 已按原版对调（**BoardCamera = `MainCamera`** · HUD = `UICamera`）⇒ `Camera.main` 现在是 **3D 透视战场相机**。
本轮把 30 个文件里的 `Camera.main` 逐处判完：**HUD 档 26 处全部改成 `LayoutSpace.Cam`（或 `LayoutSpace.ResolveHudCamera()`）**，
**3D 档 2 处保留**，另有 1 处是**解析器自身的第三档**（原版就是 `MainCamera` 标签，必须留）。

| 指标 | 数量 |
|---|---|
| 判定的 `Camera.main` 出现处（30 文件内，含注释与日志文案） | **60** |
| 其中**活代码语句** | **29** |
| 改成 HUD（`LayoutSpace.Cam` / `ResolveHudCamera()`） | **26** |
| 保留 3D（`Camera.main` 正确） | **2**（`BattlePostFx` / `TapParticleController`） |
| 保留：解析器第三档（原版 `MainCamera` 档） | **1**（`LayoutSpace.ResolveHudCamera` 里那一句） |
| 纯注释 / 日志文案里提到（已按铁律 5 就地订正，非消费者） | **31** |

---

## 一 判据（唯一两条）

1. **HUD 相机的正规口** = `CardPresentation/Core/LayoutSpace.cs:46` `public static Camera Cam;`
   （`Apply` 里设；正交、中心在原点、可见高 10 单位，与原版 `UICamera` 同语义）。
2. **原版 UI 相机的解析序** = `decomp_full/SceneCameraManager__GetUICamera.c` + `decomp_full/PopUpGameWindow__GetUICamera.c:20-45`：
   **① 缓存字段 → ② `UICamera` 标签 → ③ `MainCamera` 标签 → ④ 空**（SceneCameraManager 那支还会 `LogError`）。
   ⇒ 本轮新增 **`LayoutSpace.ResolveHudCamera()`**（`:317`）照抄这三档，第 ① 档 = 静态 `Cam`。
   ⛔ 从此全仓 **不再把 `Camera.main` 当 HUD 相机**。

先例：`Battle/CombatAutoZoom.cs:756` `var ui = LayoutSpace.Cam; // 原版那台 UICamera`。

---

## 二 完整判定表（活代码，29 处）

| # | 文件:行 | 要 3D 还是 HUD | 判定 | 判据一句话 |
|---|---|---|---|---|
| 1 | `Battle/BattlePostFx.cs:253` | **3D** | **保留** `Camera.main` | 原版句是 `camera.UpdateVolumeStack()`，目标是 `BattleManager.boardCamera`；tag 对调后 `Camera.main` 正是那台（原版 GO `&348`） |
| 2 | `Battle/TapParticleController.cs:213` | **3D** | **保留** `Camera.main` | 原版 `TapParticleController__Update.c:140-144` 取 `Camera.main` 做地面 `Physics.RaycastNonAlloc`（3D 掩码）；兜底由 `BattleDriver.cam`（HUD）改成 `BattleDriver.boardCam`（3D） |
| 3 | `Battle/TargetReticle.cs:519` | HUD | 改 `LayoutSpace.Cam` + 出声 | 原版 `hudCamera`（`+0xA0`）= 场景里的 `UI Camera`；`WorldToScreenPoint`/`ScreenPointToRay` 同一台往返 |
| 4 | `Core/DraggableController.cs:190` | HUD | 改 `LayoutSpace.Cam` | 原版 `Draggable.OnDrag` 读 `pressEventCamera`（uGUI 画布相机）；本仓无 EventSystem ⇒ 等价物 = HUD 相机 |
| 5 | `Core/LayoutSpace.cs:157` | HUD | 改 `ResolveHudCamera()` | `ScreenToWorld` 的兜底；原版 `SceneCameraManager.GetUICamera` 三档（`Cam`→`UICamera`→`MainCamera`） |
| 6 | `Core/LayoutSpace.cs:326` | HUD（解析器第三档） | **保留** `Camera.main` | 这就是原版第三档（`MainCamera` 标签）；只在 `Cam` 与 `UICamera` 都取不到时才走 |
| 7 | `Deck/DeckRuntime.cs:1279` | HUD | 改 `ResolveHudCamera()` | 卡组窗所有屏幕↔世界换算用 UI 相机（`Apply` 设的 `Cam` 优先） |
| 8 | `Editor/B107_W15_Assert.cs:86` | HUD | 改 `LayoutSpace.Cam` | 自检要喂**同一台**相机给 `Label.LinkAt` 与生产（HUD） |
| 9 | `Editor/B117_WCARDTIER_Assert.cs:187` | HUD | 改判据 `LayoutSpace.Cam == null` | 判「宿主有没有相机」应看权威档，不看 `Camera.main` |
| 10 | `Editor/CollectionScene.cs:13727` | HUD | 改 `LayoutSpace.Cam` | UI-21 量测：层沿视线深度，正交相机 |
| 11 | `Editor/CollectionScene.cs:13735` | HUD | 改 `LayoutSpace.Cam` | 同上（欧氏距离那一份） |
| 12 | `Editor/DeckScene.cs:1186` | HUD（A1007 探针） | 判据改成 `Camera.main == null && LayoutSpace.Cam == null` | 探针要造「没有 HUD 相机」；`Shoot` 现在读 `Cam` ⇒ 要清两样 |
| 13 | `Editor/DeckScene.cs:1225` | HUD（A1007 探针收尾） | 断 `Camera.main != null && LayoutSpace.Cam != null` | 还原两样，别废掉后面的截图 |
| 14 | `Editor/EndPanelProbe.cs:107` | HUD | 改 `LayoutSpace.Cam` | 探针建场时自己 `Apply` 过 |
| 15 | `Editor/MenuCheck.cs:401` | HUD | 改 `LayoutSpace.Cam` | 整屏截图要拍宿主那台 UI 相机（原 `Camera.main` 会把战场相机渲进图） |
| 16 | `Editor/ShellScene.cs:1045` | HUD | 改 `LayoutSpace.Cam` | 壳宿主 `Build()` 里 Apply 过的那台 |
| 17 | `Editor/WindowShots.cs:480` | HUD | 改 `LayoutSpace.Cam` | 窗图渲染 |
| 18 | `Editor/WindowShots.cs:762` | HUD | 改 `LayoutSpace.Cam` | 模糊快照底的源相机 |
| 19 | `Shell/GachaEventWindow.cs:1357` | HUD | 改 `LayoutSpace.Cam` + 出声 | 原版 `GachaCratePanel.Awake` ④ `videoPlayer.targetCamera = Camera.main`（菜单里那台 = UI 相机）；`Cam` 为空时 `Debug.LogWarning`（同一物理行，零行号变化） |
| 20 | `Shell/InboxWindow.cs:1388` | HUD | 改 `LayoutSpace.Cam` + 出声 | 原版 `TextLinkProcessing.TryProcessText` 传 UI 相机 |
| 21 | `Shell/InboxWindow.cs:1445` | HUD | 改 `LayoutSpace.Cam` | 原版 `IsVisibleFrom(content, Camera.main)`（菜单里 = UI 相机） |
| 22 | `Shell/MainMenuRuntime.cs:456` | HUD | 改 `ResolveHudCamera()` + 出声 | 主菜单场景自己的相机要重新 `Apply`（A1494：`Cam` 会指向已销毁的壳相机） |
| 23 | `Shell/PopUpGameWindow.cs:558` | HUD | 改 `LayoutSpace.Cam` | 原版 `PopUpGameWindow.GetUICamera()`（`UICamera`→`MainCamera`） |
| 24 | `Shell/ShellRuntime.cs:725` | HUD | 改 `ResolveHudCamera()` | 壳那台 UI 相机（单相机场景 = `MainCamera` 档） |
| 25 | `Shell/ShellRuntime.cs:1170` | HUD | 改 `ResolveHudCamera()` + 出声 | 切主菜单前关掉**壳自己那台** UI 相机；取不到时 `Debug.LogWarning`（否则两台相机同时渲，画面会重） |
| 26 | `Shell/UIBakeScreen.cs:150` | HUD | 改 `LayoutSpace.Cam` | `cameraToBake` = 背景模糊快照源（UI 相机） |
| 27 | `Shell/UIBakeScreen.cs:234` | HUD | 改 `ResolveHudCamera()` | 壳相机被销毁后取「当前那台」 |
| 28 | `Shell/UIBorderGlow.cs:364` | HUD | 改 `ResolveHudCamera()` | 原版 `CalculateViewPosition.c:24-29` 懒取 `Camera.main`（菜单里 = UI 相机） |
| 29 | `WarpforgeVFX/Runtime/UIViewportUvWriter.cs:141` | HUD | 改 `LayoutSpace.ResolveHudCamera()` | 原版 `ModifyMesh` 的 uv0.zw 视口坐标，相机同 `CalculateViewPosition` |

### 纯注释出现处（31 处，非消费者）

| 文件:行 | 判定 |
|---|---|
| `Battle/EnvironmentApplier.cs:12` · `Core/EnvBlendables.cs:13` | ⚠️ 2026-11-04 订正：原文写「`Instantiate(prefab, Camera.main.pos/rot)`」**是错的**，原版是 `battleCenterAnchor.position` + `Quaternion.identity`（与本文件 `:167` 一致） |
| `Editor/B139_S_CameraSerialized_Assert.cs:170,217` | **断言目标本身**（tag 对调判据：BoardCamera = `MainCamera`）⇒ 只读不动 |
| `Editor/B117_G10_02a_Assert.cs:31,193` | 「灭自证」文案：把落点改回 `Camera.main.position` ⇒ 红（不是消费者） |
| `Editor/B109_E16_AspectClamp_Assert.cs:251` | 说明「本文件的临时相机不 tag MainCamera，免得吃掉宿主的 `Camera.main`」—— 仍然成立 |
| `Editor/B97_W2_Assert.cs:9` | 原版 `VideoPlayer` 行为描述 |
| `Editor/MainMenuScene.cs:8552,8588,16901` | 已同步成 `LayoutSpace.Cam == null`（`Shoot` 的判据变了） |
| `Shell/CollectionWindow.cs:5460` | 已同步成 `LayoutSpace.Cam.enabled = false` |
| `Shell/GachaEventWindow.cs:1346` · `Shell/InboxWindow.cs:1369,1424` | 原版反编译原文引述（保留原样，旁边代码行已标注我们的等价物） |
| `Shell/UIBakeScreen.cs:77,232` · `Shell/UIBorderGlow.cs:314,357` · `WarpforgeVFX/Runtime/UIViewportUvWriter.cs:17,59,145` | 文档已同步成 `LayoutSpace` 三档 / 已标 2026-11-04 订正 |
| `Battle/BattlePostFx.cs:130,242` · `Battle/TapParticleController.cs:19,212,220` · `Core/LayoutSpace.cs:156,307` · `Shell/PopUpGameWindow.cs:549` | 已标注「3D 档保留 / HUD 档改」的理由（见上表） |

---

## 三 改了哪些（按文件）

**新增**
- `Core/LayoutSpace.cs` — 类尾新增 `public static Camera ResolveHudCamera()`（`LayoutSpace.cs:317`，原版 `GetUICamera` 三档）。

**生产代码（HUD 档改）**
- `Battle/TargetReticle.cs` · `Core/DraggableController.cs` · `Deck/DeckRuntime.cs`
- `Shell/ShellRuntime.cs`（×2）· `Shell/MainMenuRuntime.cs` · `Shell/PopUpGameWindow.cs`
- `Shell/InboxWindow.cs`（×2）· `Shell/GachaEventWindow.cs` · `Shell/UIBakeScreen.cs`（×2）· `Shell/UIBorderGlow.cs`
- `WarpforgeVFX/Runtime/UIViewportUvWriter.cs`

**3D 档（只改注释 + 一处兜底）**
- `Battle/BattlePostFx.cs:253` — 保留 `Camera.main`（BoardCamera）。
- `Battle/TapParticleController.cs:213` — 保留 `Camera.main`；兜底 `_driver.cam`（HUD）→ `_driver.boardCam`（3D）。

**自检宿主**
- `Editor/MenuCheck.cs`（`Shoot`）· `Editor/DeckScene.cs`（A1007 探针）· `Editor/ShellScene.cs` · `Editor/CollectionScene.cs`（×2）
- `Editor/EndPanelProbe.cs` · `Editor/WindowShots.cs`（×2）· `Editor/B107_W15_Assert.cs` · `Editor/B117_WCARDTIER_Assert.cs`

**只改注释**
- `Battle/EnvironmentApplier.cs` · `Core/EnvBlendables.cs` · `Editor/MainMenuScene.cs` · `Shell/CollectionWindow.cs` · `Editor/BattlePostFx.cs` 头部两条

**未动（判据本体 / 无消费者）**
- `Editor/B139_S_CameraSerialized_Assert.cs`（tag 断言）· `Editor/B109_E16_AspectClamp_Assert.cs` · `Editor/B117_G10_02a_Assert.cs` · `Editor/B97_W2_Assert.cs`

---

## 四 需要 Unity 验的宿主

| 宿主 | 为什么需要真跑 |
|---|---|
| `DeckScene.Run` | **A1007 探针**改了造局面的方式（清 `LayoutSpace.Cam` + 摘标签）⇒ 必验「恰好 1 条 ✗ / 0 条 ✓ / 不落图」；另 `DeckScene` 全程截图走 `Shoot`（现在读 `Cam`） |
| `ShellScene.Run` | 相机三条断言（正交 / size 5 / 纯黑）与全程截图都改成 `LayoutSpace.Cam`；同时验 `ShellRuntime.Build` 的 `ResolveHudCamera()` |
| `MainMenuScene.Run` | `MainMenuRuntime.Build` 的 `Apply(ResolveHudCamera())`；页面切换后的截图 |
| `CollectionScene.Run` | `LayerDepth/LayerDist` 的相机；UI-21 量测断言 |
| `RewardsScene.Run` / `ShopScene.Run` / `SettingsScene.Run` | 三份都调 `MenuCheck.Shoot`（现在读 `LayoutSpace.Cam`）——三宿主均已 `LayoutSpace.Apply` 过，需真跑确认截图非空 |
| `EndPanelProbe.Run` | 四张骷髅图 |
| `WindowShots`（工具） | 31 张窗图 + 模糊底 |
| **真机**（`-wfscene Battle`） | `TapParticleController`（点地面 FX）· `TargetReticle`（准星弧线）· `InboxWindow`/`PopUpGameWindow` 正文链接 · `UIBorderGlow`（uv0.zw 视口坐标）· 开箱视频 |

---

## 五 验证结果

- `TMPDIR=/tmp/wf_d4 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
- `python -I -X utf8 工具/check_ledger_points.py --all-A` ⇒ **A 5942 条 · 通过 5942 · 漂移 0 · 失败 0**（与改前一致；告警 901 不变）。
- 账补丁：`资料/完全复刻/账/_parts/B139_D4.tsv`（11 列 · 25 行 · `(原版类,方法)` 与主表**零碰撞** · 落点逐行经 `check_ledger_points.looks_like_decl/decl_name_at` 复核）。
- 行尾：26 个被改文件逐字节核过（`DeckRuntime.cs` / `DeckScene.cs` 是 CRLF，其余 LF；**0 个混行尾、0 个裸 CR**）。

---

## 六 顺手发现（不在本包白名单，交调度台分流）

1. `Battle/TapParticleController.cs` 的相机兜底原来是 `BattleDriver.cam`（**HUD** 相机）—— 拿正交 HUD 相机做地面 `Physics.Raycast` 是错的。
   本包**已在白名单内就地改成 `BattleDriver.boardCam`**（3D 档），记在账补丁第 2 行。
2. `Battle/BattleDriver.cs:4886`（包 D1）`EnsureAudioListener` 保留 `Camera.main` = BoardCamera —— 与包 S 的 tag 对调一致，**已由 D1 自证**（该行注释写明了）。
3. `Editor/BattleScene.cs:25253`（包 D1）tooltip 截图已改成 `LayoutSpace.Cam` —— 与本包口径一致。

---

## 七 顺带核过（没改，说明理由）

- `CardPresentation/Editor/B109_E16_AspectClamp_Assert.cs:251` 的临时相机 **不 tag `MainCamera`**：那是刻意的（不吃掉宿主 `Camera.main`），本包不动。
- `CardPresentation/Editor/B139_S_CameraSerialized_Assert.cs` 的 `Camera.main` 全在**注释里**（断言文案），tag 判据本体（`BoardTag` / `HudTag`）不动。
- 所有调用 `MenuCheck.Shoot` 的 7 个宿主 + `WindowShots` 都**先** `LayoutSpace.Apply` 再截图（逐文件核过 `LayoutSpace.Apply` 命中数 ≥ 1），所以 `Shoot` 改读 `Cam` 不会让任何宿主退化成「没拍成」。
