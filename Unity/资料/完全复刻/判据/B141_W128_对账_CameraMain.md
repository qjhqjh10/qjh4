# B141 / W128 — `Camera.main` 逐处判定（R6 只读普查）

> 会话 128 · 2026-11-04 · 只读代理 R6 · 范围 `d:/4/Unity/MyGame/Assets/**/*.cs`（排除 `Library/`）
> ⛔ 本报告零代码改动。所有行号现读。统计法：按二进制读源，逐行找 `Camera.main` 字面（含注释与字符串），
> 再按「该行第一个 `//` 的位置」判它是活代码还是注释；字符串字面再按引号奇偶单独标出。

## 〇 一句话结论

- **现读 72 次出现 / 64 行 / 31 个文件**。不是委托里的「73 处 / 32 个文件」—— 那是 **B139-D4 施工前**的快照：
  `Shell/CollectionWindow.cs` 那一处活代码改掉后整行已不含该字面 ⇒ 文件 **32→31**。
- 其中 **活代码 12 次**：真正取相机 **4 次**（全部判对）· 自检前提比较 **2 次**（故意这么写）· 字符串文案 **6 次**（非消费者）。
- **现读「该改」= 0 处。** 委托里「其余 67 处未逐处判」**已过期** —— 同一天 `B139-D4` 已逐处判完并改了
  **26 处活语句（20 个文件）**，`B139-D1` 另改 1 处（`Editor/BattleScene.cs:25253`）。
  判定报告在 `资料/完全复刻/报告/B139_D4_CameraMain消费者.md`（150 行，含完整判定表）。
- 关键结论：**3D 档 4 处全部保留 `Camera.main`（正确）；HUD 档已全部改走 `LayoutSpace.Cam` / `ResolveHudCamera()`**，
  本轮逐处复核**没有一处需要再改**，也**没有一处 HUD 消费者被漏掉**。

---

## 一 判据（唯一几条，含本轮的独立实读）

| # | 判据 | 出处 |
| --- | --- | --- |
| 1 | **HUD 相机正规口 = `LayoutSpace.Cam`**（`Apply` 设：正交、中心原点、可见高 10） | `CardPresentation/Core/LayoutSpace.cs:46` · `:75` |
| 2 | HUD 兜底 = **`LayoutSpace.ResolveHudCamera()`**，三档「缓存 `Cam` → `UICamera` 标签 → `MainCamera` 标签」 | `LayoutSpace.cs:317-328`（本轮实读：`decomp_full/SceneCameraManager__GetUICamera.c` 先返回缓存、再逐个 `FindGameObjectWithTag`、全空才 `LogError`；`PopUpGameWindow__GetUICamera.c` 同样是「缓存→tagA→tagB→0」） |
| 3 | **3D 战场相机 = BoardCamera，tag = `MainCamera`；HUD 那台 tag = `UICamera`** | 本工程：`Editor/BattleScene.cs:47739`／`:47797`。原版：导出 YAML `Battle Arena 1.unity` 的 `m_Name: BoardCamera → m_TagString: MainCamera`、`m_Name: UI Camera → m_TagString: UICamera`（AssetRipper 工程，本轮实读） |
| 4 | 原版 `BattleManager.hudCamera` = `Camera &1462`（GO `&350` = UI Camera）· `boardCamera` = `Camera &1461`（GO `&348` = BoardCamera） | 场景 JSON `MonoBehaviour_4371.json`（hud 1462 / board 1461）+ `Camera_1461/1462.json` 的 `m_GameObject`（348 / 350）+ `dump.cs:30732-30733`（`hudCamera // 0xA0` / `boardCamera // 0xA8`） |
| 5 | **用途 → 相机**：UI/菜单/截图/UI 世界换算 → HUD；地面 `Physics.Raycast`、后期 Volume、音频听点、3D 战场件 → 3D | 逐处按调用语义判（见 §三），⛔ 不按目录猜 |

> ⚠️ 数据源小坑（本轮踩到，已核清）：`assets_full` 场景 JSON 里 `GameObject` 的 `m_Tag` 是**数字**，
> 本 build 的 **5 = MainCamera**（battlearena1 的 BoardCamera、level0/主菜单的 UI Camera 都是 5），
> **20003 = UICamera**（`ProjectSettings/TagManager.asset` 自定义表第 4 项）。
> ⛔ **别拿 Unity 内置顺序（4=MainCamera）去套这个数字** —— 判 tag 一律用导出 YAML 的 `m_TagString`。

---

## 二 现读数（72 次出现的形态分布）

| 形态 | 次数 | 说明 |
| --- | --- | --- |
| 活代码 — 真正取相机 | **4** | `BattleDriver:4886` · `BattlePostFx:253` · `TapParticleController:213` · `LayoutSpace:326`（解析器第三档） |
| 活代码 — 自检前提比较 | **2** | `DeckScene:1186` / `:1225`（故意同时断 `Camera.main` 与 `LayoutSpace.Cam` 两条链） |
| 活代码 — 字符串文案 | **6** | 日志/断言的文案里出现该词，**不是消费者** |
| 纯注释 | **60** | B139-D4 的判定说明 / 原版反编译引述 / 历史订正 |
| 合计 | **72**（64 行 / 31 文件） | 见 §三、§四 |

---

## 三 总表 A — 活代码 12 次（逐处判定）

| # | 文件:行 | 形态 | 用途 | 现用 | 该用 | 判定 | 一句话理由 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | `Battle/BattleDriver.cs:4886` | `var cam = Camera.main` | 场上没有 `AudioListener` 时补一台（听点） | `Camera.main` | **3D**（保持） | ✅ 已对 | 原版 listener 挂在 GO `&348` = BoardCamera（`AudioListener_1594.json` 的 `m_GameObject` = 348）；tag 对调后 `Camera.main` 正是那台 |
| 2 | `Battle/BattlePostFx.cs:253` | `return Camera.main` | 后期 Volume 栈 `UpdateVolumeStack` 的兜底相机 | `Camera.main` | **3D**（保持） | ✅ 已对 | 原版拍的是 `BattleManager.boardCamera`（`dump.cs:30733`）；且战斗场景里唯一 `renderType == Base` 的相机就是 BoardCamera（`BattleScene.cs:47848-47849` 把 board 设 Base、HUD 设 Overlay） |
| 3 | `Battle/TapParticleController.cs:213` | `var cam = Camera.main` | 点地面：`ScreenPointToRay` + `Physics.RaycastNonAlloc`（3D 掩码） | `Camera.main`（兜底 `boardCam`） | **3D**（保持） | ✅ 已对 | 原版读 `BattleManager.boardCamera`（`TapParticleController__Update.c` 里 `FUN_1805e38d0` = `BattleManager.Instance`，`+0xA8` = boardCamera）；结论 3D 正确。⚠️ 注释把出处写成「原版取 `Camera.main`」不准 → §六-1 |
| 4 | `Core/LayoutSpace.cs:326` | `var m = Camera.main` | `ResolveHudCamera` 的**第三档**（`MainCamera` 标签） | `Camera.main` | **HUD 兜底**（保持） | ✅ 已对 | 原版 `GetUICamera` 第三档就是 `MainCamera` 标签；单相机 UI 场景里那台就是 UI 相机。只在 `Cam` 与 `UICamera` 都取不到时才走 |
| 5 | `Editor/DeckScene.cs:1186` | `Camera.main == null && LayoutSpace.Cam == null` | 自检造「没有 HUD 相机」的局面 | 两者都比 | **不适用**（自检） | ✅ 已对 | 探针要把 `Shoot` 的两条输入一起断掉；只断一条验不到 |
| 6 | `Editor/DeckScene.cs:1225` | `Camera.main != null && LayoutSpace.Cam != null` | 自检收尾：把相机还回去 | 两者都比 | **不适用**（自检） | ✅ 已对 | 还原两样，别废掉后面的截图 |
| 7 | `Battle/TapParticleController.cs:220` | 字符串 | 日志文案 | — | — | ⛔ 非消费者 | `Debug.LogWarning` 的文案 |
| 8 | `Editor/B117_G10_02a_Assert.cs:193` | 字符串 | 断言文案（灭自证） | — | — | ⛔ 非消费者 | 「改回 `Camera.main.position` 就红」的说明 |
| 9 | `Editor/B139_S_CameraSerialized_Assert.cs:217` | 字符串 | 断言文案（tag 对调判据） | — | — | ⛔ 非消费者 | 「原版 MainCamera ⇒ Camera.main 指 3D 相机」 |
| 10 | `Editor/DeckScene.cs:1191` | 字符串 | 断言文案 | — | — | ⛔ 非消费者 | 前提说明 |
| 11 | `Editor/DeckScene.cs:1192` | 字符串 | 断言文案 | — | — | ⛔ 非消费者 | 失败排查提示 |
| 12 | `Editor/DeckScene.cs:1226` | 字符串 | 断言文案 | — | — | ⛔ 非消费者 | 收尾说明 |

---

## 四 总表 B — 纯注释 60 次（按文件；同时是分文件小结）

| 文件 | `Camera.main` 出现行（次数） | 活代码 | 该改 | 备注（注释判读） |
| --- | --- | --- | --- | --- |
| `Battle/BattleDriver.cs` | 4886（注释 2 次）· 9823（3 次） | 1 | 0 | 4886 说明准确；⚠️ **9823 的「原版 ApplyEnvironment 用 Camera.main」与二进制不符** → §六-2 |
| `Battle/BattlePostFx.cs` | 130 · 242 · 253（注释 1 次） | 1 | 0 | 3D 档说明准确 |
| `Battle/EnvironmentApplier.cs` | 12 | 0 | 0 | 订正句「原版=battleCenterAnchor + identity」**与二进制一致**（§六-2） |
| `Battle/TapParticleController.cs` | 19 · 212 · 220（文案） | 1 | 0 | ⚠️ 212 引用「原版取 Camera.main」不准（19 行头部是同源概括）→ §六-1；结论（3D）仍对 |
| `Battle/TargetReticle.cs` | 500 · 519 | 0 | 0 | ✅ 与 decomp 一致：`+0xA0` hudCamera 做 WorldToScreenPoint、`+0xA8` boardCamera 做 ScreenPointToRay |
| `Core/DraggableController.cs` | 190 | 0 | 0 | uGUI `pressEventCamera` 的等价物 = HUD 相机，成立 |
| `Core/EnvBlendables.cs` | 13 | 0 | 0 | 与 `EnvironmentApplier` 同一条订正，准确 |
| `Core/LayoutSpace.cs` | 156 · 307 | 1 | 0 | 三档自述准确 |
| `Deck/DeckRuntime.cs` | 1279 | 0 | 0 | HUD 档，准确 |
| `Editor/B107_W15_Assert.cs` | 8 · 86 | 0 | 0 | `TextLinkProcessing__TryProcessText.c:34` 实读确为 `Camera.main` ✅ |
| `Editor/B109_E16_AspectClamp_Assert.cs` | 251 | 0 | 0 | 「临时相机不 tag MainCamera」说明仍成立 |
| `Editor/B117_G10_02a_Assert.cs` | 31 · 193（文案） | 0 | 0 | 灭自证文案 |
| `Editor/B117_WCARDTIER_Assert.cs` | 187 | 0 | 0 | 判「有没有 HUD 相机」看 `LayoutSpace.Cam`，正确 |
| `Editor/B139_S_CameraSerialized_Assert.cs` | 170 · 217（文案） | 0 | 0 | ✅ 与本轮实读的导出 YAML `m_TagString: MainCamera` 一致 |
| `Editor/B97_W2_Assert.cs` | 9 | 0 | 0 | 原版 VideoPlayer 行为描述 |
| `Editor/BattleScene.cs` | 25253（2 次）· 47739 | 0 | 0 | 25253 是 D1 改过的 Shot；47739 是 tag=MainCamera，与导出 YAML 一致 |
| `Editor/CollectionScene.cs` | 13727 · 13735 | 0 | 0 | HUD 量测，正确 |
| `Editor/DeckScene.cs` | 1171 · 1184 · 1191/1192/1226（文案） | 2 | 0 | 探针说明与实现一致 |
| `Editor/EndPanelProbe.cs` | 107 | 0 | 0 | HUD，正确 |
| `Editor/MainMenuScene.cs` | 8552 | 0 | 0 | 同步 `Shoot` 判据 |
| `Editor/MenuCheck.cs` | 401 | 0 | 0 | 整屏截图走 HUD，正确 |
| `Editor/ShellScene.cs` | 1045 | 0 | 0 | 同上 |
| `Editor/WindowShots.cs` | 480 · 762 | 0 | 0 | 拍窗/烘模糊源走 HUD，正确 |
| `Shell/GachaEventWindow.cs` | 1346 · 1357 | 0 | 0 | ✅ `GachaCratePanel__Awake.c:25-27` 实读确为 `set_targetCamera(Camera.main)` |
| `Shell/InboxWindow.cs` | 1369 · 1388 · 1424 · 1445 | 0 | 0 | ✅ 同上（TryProcessText 实读） |
| `Shell/MainMenuRuntime.cs` | 456 | 0 | 0 | 主菜单重设 `Apply(ResolveHudCamera())`，正确 |
| `Shell/PopUpGameWindow.cs` | 550 · 558 | 0 | 0 | ✅ `GetUICamera` 三档实读 |
| `Shell/ShellRuntime.cs` | 725 · 1170 | 0 | 0 | 壳 UI 相机，正确 |
| `Shell/UIBakeScreen.cs` | 77 · 150 · 234 | 0 | 0 | 模糊快照源 = UI 相机，正确 |
| `Shell/UIBorderGlow.cs` | 314 · 357 · 364（2 次） | 0 | 0 | ✅ `UIBorderGlow__CalculateViewPosition.c:26` 实读确为 `Camera.main` |
| `WarpforgeVFX/Runtime/UIViewportUvWriter.cs` | 17 · 59 · 141（2 次） | 0 | 0 | 同上（UI uv0.zw 视口坐标，HUD 相机正确） |

> 合计：31 个文件；活代码 6（真取相机 4 + 自检 2）；**该改 0**。

---

## 五 ✅ B139-D4 / B139-D1 已改样本（共 27 处活语句）

`B139-D4`（20 个文件，26 处活语句）：`TargetReticle:519` · `DraggableController:190` · `LayoutSpace:157` ·
`DeckRuntime:1279` · `B107_W15_Assert:86` · `B117_WCARDTIER_Assert:187` · `CollectionScene:13727/13735` ·
`EndPanelProbe:107` · `MenuCheck:401` · `ShellScene:1045` · `WindowShots:480/762` · `GachaEventWindow:1357` ·
`InboxWindow:1388/1445` · `MainMenuRuntime:456` · `PopUpGameWindow:558` · `ShellRuntime:725/1170` ·
`UIBakeScreen:150/234` · `UIBorderGlow:364` · `UIViewportUvWriter:141` · `DeckScene:1186/1225`（后两处是自检探针）。
另新增解析器 `LayoutSpace.ResolveHudCamera()`（`:317`）。
`B139-D1` 另 1 处：`Editor/BattleScene.cs:25253`（悬停 tooltip 截图改 `LayoutSpace.Cam`）。
**保留 3D 的 2 处**（`BattlePostFx:253` / `TapParticleController:213`）+ 解析器第三档 1 处（`LayoutSpace:326`）在判定表里都标为「保留正确」。

> ✅ 复核结论：26 + 1 处改法**全部与用途相符**；未发现「本该要 3D 却被改成 HUD」的站点（唯一涉及战场的
> `TargetReticle` 已按原版 `hudCamera`（`+0xA0`）判定为 HUD）。

---

## 六 顺手发现（⛔ 未改，交调度台分流）

1. ⚠️ **`TapParticleController` 的判据引用不准（结论不变）**：`TapParticleController.cs:212` 写「原版
   `.c:140-144` 取的就是 `Camera.main`」（同文件 `:19` 头部是同一句的概括）。本轮实读 `decomp_full/TapParticleController__Update.c`：
   该处先取 `FUN_1805e38d0`（= `BattleManager.Instance`，同族调用点直接把它喂给 `BattleManager__GetHero`），
   再读 `+0xA8`（`dump.cs:30733` = `boardCamera`）做 `ScreenPointToRay` ⇒ **原版用的是 `boardCamera`，不是 `Camera.main`**。
   ✅ 结论（要 3D）不变，只需把注释出处订正（铁律 5）。建议并给 W132 写手一行小改。
2. ⚠️ **两处注释互相矛盾（`BattleDriver.cs:9823` vs `EnvironmentApplier.cs:12` / `EnvBlendables.cs:13`）**：
   前者说原版 `ApplyEnvironment` 用 `Camera.main.position/rotation`；后者说那是错的、原版是
   `battleCenterAnchor.position` + `Quaternion.identity`。本轮实读二进制**支持后者**：
   `ScenarioEnvironmentConditionSO__EnableEnvironment.c:90-106` 从 `Transform.get_position` 取位置（`+0x1D0`）+
   静态四元数，而 `dump.cs:30781` `battleCenterAnchor // 0x1D0` ⇒ **`BattleDriver.cs:9823` 那句「原版自己的写法」应订正**。
3. ℹ️ `BattlePostFx.ResolveBoardCamera()`（`:245-253`）用「`renderType == Base` 只有一台」挑相机。现读正确
   （`BattleScene.cs:47848-47849` 明确 board=Base、HUD=Overlay），但这是**靠唯一性**的隐式前提：
   若将来出现第二台 Base 相机（编辑器探针/预览），会**静默**拿错。建议补一条出声 + 断言（归 W132）。
4. ℹ️ **`m_Tag` 数字映射坑**：`assets_full` 的 `GameObject.m_Tag` 在本 build 是 **5 = MainCamera**、**20003 = UICamera**，
   ⛔ 与 Unity 内置顺序（4=MainCamera）不同。判 tag 必须用导出 YAML 的 `m_TagString`，别用数字套。
5. ℹ️ `FindFirstObjectByType<Camera>()` 一族（`WarpforgeArena1/Editor/ArenaBuilder.cs:1231,2013,2047,2088`、
   `CardPresentation/Editor/ArtBaker.cs:100`）目前**成立**（独立战场场景只有 BoardCamera 一台），
   但同样靠「唯一性」；若往那些场景加 HUD 相机就会静默拿错（同类风险，非 `Camera.main`，供 W132 参考）。

---

## 七 ❓判不出 / 需注意

- ❓「73 处 / 32 文件」这个**施工前**数字无法从当前工作树逐字复现（工作树是施工后状态，且 B139-D4 的报告只给了
  「30 个文件 / 60 次 / 29 活语句」的**包内**口径）。本轮只对**现读**负责：72 次 / 64 行 / 31 文件。
  若要闭合旧口径，建议按 `git show 512d3f86` 的 diff 逐行核（本轮未做，因为不属于「现读判定」）。
- ❓ `Library/PackageCache/**` 里有大量第三方 `Camera.main`（URP/InputSystem 示例与编辑器工具），
  **不属于我们的代码**，本报告未判；它们不参与我们的 tag 语义。
- （无其它判不出的站点：4 处真取相机与 2 处自检比较都已逐条落到具体用途与二进制/场景判据。）
