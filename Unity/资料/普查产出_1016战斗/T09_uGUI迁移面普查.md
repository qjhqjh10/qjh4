# T09 · 战斗 HUD 整层换 uGUI —— 施工面普查（只读普查）

**日期** 2026-10-16 · **范围** `Unity/MyGame/Assets/` · **判据源** `资料/战斗复刻_正本.md` §2·1/2·4/2·6/3·1/3·1b/3·1c/3·1d
**⛔ 本报告只读产出，未改任何 `.cs`/`.unity`/`.json`。**
**⚠️ 读的时刻正在被改的文件**：`Battle/BattleDriver.cs`（mtime 2026-10-16 12:19:01）· `Editor/BattleScene.cs`（12:19:07）· `Core/CardFeel.cs`（12:16:21）—— 下面对这三份引的行号**以本次读到的内容为准**，别的写手可能已改。`Board/DeploySequence.cs`（09-20）· `Board/BoardLayout.cs`（06:54）· `Hand/CardInteraction.cs`（06:55）读到的可能是半成品。

---

## 一、数出来的量

| 件 / 口 | 文件（前几名） | 命中数 | 命令 |
|---|---|---|---|
| `ImageQuad` 总引用 | 105 个 `.cs` | **1424** | `grep -rn "ImageQuad" --include=*.cs Assets` |
| ├ `ImageQuad.Create`（**建件口**） | — | **218**（Battle 83 · Shell 73 · Editor 38 · Deck 19 · Core 5） | `grep -rn "ImageQuad\.Create"` |
| └ Battle 内 `Create` | `SettingsPanel 15` · `BattleDriver 10` · `WfSlider 7` · `WaitBanner 6` · `TutorialOverlay 6` · `MultiCardDisplay 6` · `MulliganPanel 5` · `CardChoicePanel 5` · `BattleLogPanel 5` · 其余 12 份各 1–3 | 83 | 同上 + `awk` 分组 |
| `Label.Create`（**建件口**） | — | **120**（Battle 57 · Editor 40 · Shell 16 · Deck 5 · Core 2） | `grep -rn "Label\.Create"` |
| └ Battle 内 `Create` | `SettingsPanel 13` · `Label.cs 8`(def) · `MulliganPanel 6` · `TutorialOverlay 5` · `MultiCardDisplay 5` · `BattleDriver 4` | 57 | 同上 |
| **HUD 建件助手**（BattleDriver） | 定义处：`HudAbs` `14253` · `HudAbsTex` `14261` · `Hud` `15573` · `HudImage` `15595` · `HudImageTex` `15670` | 调用点 **80**（`HudImageTex 26`·`Hud 23`·`HudImage 15`·`HudAbs 12`·`HudAbsTex 4`） | `grep -noE "\bHud[A-Za-z]*\(" BattleDriver.cs \| awk -F: '{print $2}' \| sort \| uniq -c` |
| 重锚注册表 | `BattleDriver.cs:15486-15489`（`_hudLabels`/`_hudSpots`/`_hudImages`/`_hudImageSpots`）· 消费点 `ReanchorHud` `:15731-15769`（12 个件被逐条拉回） | 4 个 List | 读文件 |

### `LayoutSpace` 换算口（**已剔除注释行**；`|` 左 = 全仓，右 = `Battle/`）

| 口 | 定义处 | 全仓 | Battle | 备注 |
|---|---|---|---|---|
| `FromPixel` | `LayoutSpace.cs:188` | **85** | **8** | 建件侧主口（用实测 `VisibleWidth`） |
| `ToPixel` | `:208` | **47** | **8** | x 写死 108（`PxX`） |
| `ToDesignPixel` | `:273` | **13** | **2** | 正本 §3·1 说的「唯一正确的逆」 |
| `PxX` | `:215` | **38** | **5** | |
| `PxY` | `:219` | **50** | **0** | y 那半恒真 |
| `PxPerWorldX` | `:252` | **6** | **0** | 长度（尺寸项）的斜率 |
| `ToWorld` | `:101` | **58** | **27** | |
| `ToNormalized` | `:109` | **22** | **13** | |
| `ScreenToWorld` | `:121` | **11** | **1** | |
| `RectCenter` | `:192` | **17** | **0** | |
| `Px` | `:185` | — | Battle **47** | |
| **局部 `px→世界` 助手**（`U(`/`At(`/`Pos(`/`Px(`/`W(`/`Spot(`/`WorldOfPx(`/`Content(`，**已剔除定义行**） | 每文件私有 | 全仓 **360** | Battle **251** | BattleDriver 单独 = **34** |

命令：`python -I` 遍历 `--include=*.cs`、跳过 `lstrip().startswith('//')` 的行后正则计数（脚本临时、未落盘）。

🔴 **正本 §3·1 那个「共 86 个 · `BattleDriver.cs` 28」—— 复不出来，⛔ 别再引。**
- `BattleDriver.cs` 的 **28 可精确复现**，但它量的是**局部 `Px(` 一个助手**（非注释 29 次 − 1 个定义 = 28），**不是** `FromPixel` 那一族（BattleDriver 里 `FromPixel` 只有 4 处）。
- 全仓 **85**（非注释 `FromPixel`）≈ 86，但那是**另一把尺**。
- ⇒ 原句是**两种口径混着写的一个数**（85 来自 `FromPixel`、28 来自局部 `Px(`）。**引用时改用本表。**

### 命中那条链（当前真身）

| 入口 | 位置（现核） | 现在判什么 | 换 uGUI 后该由谁判 |
|---|---|---|---|
| `ImageQuad.Contains(Vector3)` | `ImageQuad.cs:528` | 世界点 → 本件局部，比**画出来那个矩形** | uGUI `Graphic` 自己的 `rect`（+`m_RaycastPadding`） |
| `Label.Contains(Vector3)` | `Label.cs:185` | 同上（点阵/世界网格文字） | 同上（`raycastTarget`） |
| `HitPaddedRect(q, world, rectW,rectH, padL,padB,padR,padT)` | `BattleDriver.cs:3711` | 世界→局部 px（`ppu = 1/Px(1f)` = 108）→ 转发 `MenuDraw.PaddedHitRect` | `GraphicRaycaster.cs:327`（rect ∧ `raycastPadding`） |
| 5 个钮口：`CameraResetButtonHit:3596` · `SettingsBtnHit:3747` · `ChatBtnHit:3764` · `CemeteryBtnHit:3780` · `AllianceButtonHit:3841` | 同上 | 各自 `rect+pad`（原版值逐条写在常量里） | `Button.onClick` |
| `HitMyDeckPile(Vector3)` | `:7876`（`static`） | 走 `ToDesignPixel` 后比归一化锚点算的 230² 方框 | `PlayerDeck` 的 `NonDrawingGraphic`（正本 §2·4:102-103） |
| `HitEndTurn(Vector3)` | `:7895` | 走 `HitPaddedRect`（171.082×129.132） | `Clock/TurnBtn` 的 `Image` |
| `HitSlot` / `HitUnitSlotForTest` | `:7843` / `:7944` | 棋盘槽位世界坐标 | 原版 = `MinionManager.GetClosestAvailableSlot` 比**屏幕距离**（正本 §2·6:146-147） |
| 面板族：`SettingsPanel.Contains:1713`·`HitResign:1719`·`HitClose:1741`·`HitBlank:1785`·`CardChoicePanel.HitEye:341`·`MulliganPanel.HitEye:764`·`MultiCardDisplay.HitContinue:332`·`WfSlider.Contains:550`·`CardView.Contains:4359`·`ChatPopupPanel.Contains:381`·`SkillPanel.Contains:338` … | — | 各判各的 | 逐条换成 uGUI 件 |
| 手牌：`CardInteraction.HitTest` | `Hand/CardInteraction.cs:626`（调 `CardView.Contains` `:633`） | 遍历所有手牌、取 z 最小者 | 原版 = `Physics2D.GetRayIntersectionNonAlloc`（正本 §2·4:99-101）⇒ **留世界空间 + `Collider2D`** |
| 计数 | `public bool Contains\|Hit*` **定义 25 个**（Battle+Core+Hand+Board）· `.Contains(` **调用点 Battle 42 / Core 16 / Hand 2 / Board 0** | | `grep -rnE "public (static )?bool (Contains\|Hit[A-Za-z]*)\("` |
| `HitPaddedRect` 调用点 | 5 · `MenuDraw.PaddedHitRect` 调用点 16 | | |

### 输入派发（换 uGUI 时被替掉的那一层）

- `PollInputEdges()` `BattleDriver.cs:16341` + 5 个字段（`_downEdge`/`_upEdge`/`_held`/`_pressCaptured`/`_swallowNextRelease`，`:16311-16334`）· `Update()` 第一句 `:8124`。
- 模态链 = `Update()` 里 **15 处 `if (HandleXxx()) return;`**（`:8174-8260`）；`HandleXxx` 定义 **13 个**（`:3331…:10150`）。
- 两条沿消费点：`ReleasedThisFrame()` 约 30 处 / `ClickedThisFrame()` 全文件 **4 处**（`:16356-16361` 注释自陈，`Editor/BattleScene.cs` 有 `A462` 源级断言钉这个数）。

### 落地需要、现在**没有**的

| 项 | 现状 | 判据 |
|---|---|---|
| `Canvas` 组件（`!u!223`） | **全仓 21 个 `.unity` 一个都没有**（6 场景目录 + 1 `SampleScene` + 14 `WarpforgeArena1/Scenes`） | `grep -c "^--- !u!223"` 逐份 = 0（`find Assets -name "*.unity" \| wc -l` = 21） |
| `CanvasScaler` / `GraphicRaycaster` / `EventSystem` / `StandaloneInputModule` | **任何 `.unity` 都没有**（命中的「Canvas」全是 **GameObject 名**：`Shell.unity:1651` / `CollectionCheck.unity:1399131` 的 `2 - Canvas Holder Above upper bar`） | `grep -rl "…" --include=*.unity Assets` = 空 |
| `EventSystem` | 代码里 `using UnityEngine.EventSystems` 有 5 份（`Core/CardDraggingController.cs:14` 等），**没有实例** | `Battle/CombatCameraZoom.cs:85-87` 自陈「本仓没有 UGUI `EventSystem`/`EventSystemController`」；`Core/DraggableController.cs:84-92` 同 |
| `com.unity.ugui` | ✅ **有**（`Packages/manifest.json:11` `2.0.0`；`Library/PackageCache/com.unity.ugui@27635d171b1a`） | |
| `com.unity.modules.physics2d` | 🔴 **没有** —— manifest / `packages-lock.json` / `PackageCache` 三处皆无 | `ls -d Library/PackageCache/com.unity.modules.physics2d` → No such file；`Editor/DOTweenSetup.cs:6` 自陈「本工程 manifest 是瘦的，没装 `com.unity.modules.physics2d`」 |
| `com.unity.modules.physics`（3D） | ✅ 有（`packages-lock.json:98`）—— 原版挡板那条路要用（正本 §2·8:169，`BoxCollider` layer 11） | |
| uGUI → 世界 的桥 | ✅ **已有**：`Core/UguiRect.cs`（锚点五元组 → 绝对 px，文件头两套独立实现）→ `LayoutSpace.RectCenter` | |

---

## 二、必须同批动的清单

| 件 | 为什么不能单独换 | 只换一半会怎样 |
|---|---|---|
| **①建件侧（尺寸斜率）× ②读回侧（命中斜率）** | 正本 §3·1b/§3·1d：位置与尺寸**不同源**；建件走 `FromPixel`（实测宽）、读回走 `ToPixel`/`PxX`（写死 108）。**唯一正确的逆 = `ToDesignPixel`/`PxPerWorldX`**（`LayoutSpace.cs:252/273`） | 正本 §3·1d:345-348 逐句：**只缩尺寸不改读回 ⇒ 命中框比画的【小一圈】**；**只改读回不缩尺寸 ⇒ 位置对了、元素互相压住** |
| `ReanchorHud` + 4 个注册表 + 12 个 `RefreshLayout` 的件 | `BattleDriver.cs:15731-15769` 把 `_hudLabels`/`_hudImages`/`FitDeckPile`×2/`PlaceDeckLights`/`PlaceHandPlate`/`EnsureMarkers`×2/`backdrop`/`selector`/`skillPanel`/`_replayBar`/`_unitChat` **逐条拉回锚点**。换 uGUI 后锚点由 `RectTransform` 自己管 | 一半跟画布走、一半跟世界走 ⇒ 换分辨率时两套坐标系互相打架（且**只在非 16:9 现形**，见 §五风险） |
| 5 个 `*Hit` 口 + `HitPaddedRect` + `MenuDraw.PaddedHitRect` | uGUI 自带 rect∧`raycastPadding` —— 与手搓版**同一件事两份实现**（`BattleDriver.cs:3707` 注释自己写着「只此一份，⛔ 别抄第二遍」） | 两套并存 ⇒ 同一按被派发**两次**（uGUI `onClick` + 我们 `ReleasedThisFrame`），或吞沿逻辑（`_swallowNextRelease` `:16334`）失效 ⇒ 闪一下/看不见地触发 |
| `PollInputEdges` 两条沿 + 15 处模态 `return` 链 | 原版是 **1 根 `UIstate` int（15 档）+ 8 个 `GraphicRaycaster`**（正本 §2·6:140-142 / §2·4:104-105）；我们是一条 `if…return` 链 + 两条手搓沿 | 沿退一半：`ClickedThisFrame` 仅剩 4 处、`ReleasedThisFrame` 约 30 处（`:16353-16361`）⇒ 改到抬起那 12 处若漏一条，**点击记录与触发都少一大半而画面正常**（`LogClickBeat` `:16373` 注释即此） |
| HUD 相机（正交）→ `Canvas` 的 `ScreenSpaceCamera` 宿主 | 原版 `Canvas.m_Camera` = `hudCamera`（**透视**，正本 §2·3:84）；我们 HUD 是**正交**（`LayoutSpace.Apply:49-58`），战场 3D 是另一台透视（`Editor/BattleScene.cs:23049` / `:23077-23078`） | `Canvas` 挂错相机 ⇒ 整个 HUD 缩放/位置全错，且与 `LayoutSpace` 全局映射冲突（正本 §3·1c 那次回滚就是这条路） |
| `Label`/`ImageQuad` 的排序（`SetRenderQueue`）与 uGUI 的兄弟序/`sortingOrder` | 我们靠显式渲染队列分层（CLAUDE.md §三「分层要用渲染队列、不能用 z」）；uGUI 靠层级序 + `Canvas.sortingOrder` | 混用一层 ⇒ 面板被底板盖住（本工程 2026-09-20 踩过一次：卡组侧栏） |
| 断言宿主 | `BattleScene.cs` 有 **2620 个 `Check(`**（其中 458 行含 `Hit` / `Contains` / `FromPixel` / `ToPixel` / `ToDesignPixel`），且 `cam.aspect` 钉死在 `DesignAspect`（**现核 `Editor/BattleScene.cs:23119`**；正本 §3·2 引的是旧行号 `:22701`，文件已长） | 换一半 ⇒ 断言环境比生产窄（正本 §3·2），**全绿 ≠ 画面在 4:3 对** |

---

## 三、不该换的（留世界空间 / 留 3D）

| 件 | 判据 |
|---|---|
| **竞技场 3D 层**（`Arena3D` / `ArenaRuntimeLoader` 的 13 件 prefab、29 网格 + 34 粒子） | 由**透视** `BoardCamera` 画：`c.depth = -1`（`Editor/BattleScene.cs:23077`）· `c.cullingMask = 1 << ArenaLayer`（`:23078`），宿主函数 `BuildBoardCamera` `:23049`。原版对应物 = `Scenario/*`（正本 §四 行 39/40），**原版也不在 `Canvas` 上**（正本 §2·1:35-37 只有 19 个 Canvas，全是 HUD 那族） |
| **原版挡板/棋盘边界** | 原版 = `CardBoardLimits/CardLowBoardLimit` 的 **`BoxCollider`**（layer 11，正本 §2·8:169）⇒ **物理世界**，不是 uGUI；我们还没有（账 `A2101`） |
| **手牌 / 场上卡 / 卡面**（`CardView`、`2DCard`） | 原版卡片命中走 **`Physics2D.GetRayIntersectionNonAlloc`**（正本 §2·4:99-101 `GetTargetInPos.c:104`），而非 `GraphicRaycaster` ⇒ **卡是世界空间带 `Collider2D` 的物件**；旁证 `CardCollider2D__SetColliderNotInHand.c`（`Battle/BattleDriver.cs:4539` 引用）。⇒ 换 uGUI 的是**外框/HUD**，卡体**保持世界空间**（要换成原版那条得先补 `physics2d` 模块） |
| **粒子 / 特效**（`Scenario/Particles`、`VfxMap.Draw`） | 世界空间 + 与卡严丝合缝对齐，`LayoutSpace.cs:6-9` 文件头明写「卡要跟特效（世界空间的粒子）严丝合缝」——那是当年**不用 CanvasScaler 的理由**，对特效仍然成立 |
| **准星 `NoCanvas2D/Attack Target Reticle`** | 名字自带 `NoCanvas2D`（正本 §四 行 37）；且它的**双平面求交要两台相机**（`BattleDriver.cs:16130-16131`） |

> 边界一句话：**换 uGUI 的范围 = 原版 `BattleHud/Canvas` 下那 19 个 Canvas 的内容（HUD/弹窗/卡面窗/门）**；
> **`Scenario/*` + `BattleBoardElements/*` + 卡体 = 留在世界空间**。

---

## 四、分步建议（只说先后）

| 步 | 做什么 | 做完能验证什么 |
|---|---|---|
| **0** | 补 `com.unity.modules.physics2d` 进 `manifest.json`（**纯前置、零行为改动**） | `Library/PackageCache/com.unity.modules.physics2d` 出现 · `Physics2D.` 能编过（原版那条命中链的**物理前提**） |
| **1** | 把「唯一一份逆」铺到战场侧剩下的读回点：`HitPaddedRect:3711` 的 `ppu` 与 `HudAbsTex:14261` 的 `/108f` **成对**改（正本 §3·1d:340-343 那张两行表） | 正本 §3·1d:349 给的尺子：`世界半宽 × 斜率` 与 `rectW/2` 之差 ≤ 容差，在 **16:9 与 4:3 两档都成立**（4:3 才是尺子） |
| **2** | 立层级：`BattleHud/Canvas`（`ScreenSpaceCamera` → hudCamera）+ `CanvasScaler(1920×1080/Expand)` + `GraphicRaycaster` + `EventSystem` —— **先只建、不迁件** | 树里字段逐条 = 正本 §2·1 那张 6 行表；`!u!223` 从 0 变 1 |
| **3** | 迁**最纯的一族**：`HudAbs`/`HudAbsTex` 后面挂的**静态件**（名牌 / 能量 / 时钟 / 牌堆底板 / 六枚 Glow） | 4:3 与 16:9 两档截图并排；`ReanchorHud` 对这族**可以开始不生效**而不出偏差 |
| **4** | 迁按钮族：`HitPaddedRect` 的 5 个调用点 + 13 个 `HandleXxx` → 退掉 `PollInputEdges` 两条沿 | 断言转红点集中在 `A462` 那批（`ClickedThisFrame` 只 4 处 / `ReleasedThisFrame` 约 30 处）⇒ 一次全改完才算这一步完 |
| **5** | 迁手牌 / 棋盘：`CardInteraction.HitTest:626` | 用**指针**比而不是用**卡**比（正本 §4·补 行「交互件+输入链」：最值得改的就是这条） |
| **6** | 拆 `ReanchorHud` + 4 个注册表 + 12 个 `RefreshLayout` | 换分辨率**不再**需要 `_hudWidth` 闸（`BattleDriver.cs:15736` 附近） |
| **7** | 收尾：`LayoutSpace` 那族读回口逐个退场（`ToPixel`/`PxX` 全仓 47/38 处） | 全仓 `FromPixel` 与 `ToDesignPixel` **成对归零**（改完再统一验收） |

---

## 五、没查清 / 判不了的 + 我搜过哪些词

1. 🔴 **「共 86 个 · `BattleDriver.cs` 28」复不出来** —— 见 §一 末尾（`BattleDriver` 28 可精确复现，但它量的是**局部 `Px(` 一个助手**；全仓 85 是**另一把尺**）。**⛔ 别引原句，改引本表。**
2. **uGUI 换上去之后「谁压谁」判不了**：正本 §2·4:104-105 只给了 8 个 `GraphicRaycaster` 的**宿主名**（根 Canvas / FrontCanvas / BackCanvas / BattleDoors/Canvas / Drag Attack Selector / PopupAnchor / CemeteryLogPanel / BattleSettingsPanel），**没给各自 `sortingOrder`**。我们这边同样没定（`BattleDriver.cs:3942` 明写「命中优先级不动 —— 原版靠 `GraphicRaycaster` 排序，谁在上没读，**保持我们现有算法**」）。
3. **原版 `UIstate` 15 档 → 哪个 handler 的逐档映射没展开**（正本 §2·6:140-142 只给了「哪 5 档不处理输入」）⇒ 我们那 15 处 `if (Handle…)` 换成什么顺序，**本报告没定**。
4. **`BattleScene.cs` 2620 个 `Check(` 里哪些会因为换层而红，没逐条判**（该文件读的时刻正在被改）。
5. **`AboveShader/TipText`（正本 §4·补 行 19/26 记的「真缺 8」）等未建件与本次迁移的先后关系** —— 没查。
6. **`HandLimitArea` 用途** 正本 §4·B 段仍写「用途未知」（虽然同段又说已查实是 1×1 空锚点）—— 两种说法并存，本次未裁。
7. **我们 `Board/` 那一侧（`BoardLayout`/`ArenaSlots`）的命中口** —— `Board/` 里 `.Contains(` = **0**、`RectCenter` = **0**；但 `Board/BoardLayout.cs` 读的时刻是**半成品**（mtime 06:54），**没下结论**。

**搜过的词**：`ImageQuad` `ImageQuad.Create` `Label` `Label.Create` `FromPixel` `ToPixel` `ToDesignPixel` `PxX` `PxY` `PxPerWorldX` `ToWorld` `ToNormalized` `ScreenToWorld` `RectCenter` `Px(` `HudAbs` `HudAbsTex` `HudImage` `HudImageTex` `Hud(` `ReanchorHud` `PollInputEdges` `ReleasedThisFrame` `ClickedThisFrame` `Contains(` `HitPaddedRect` `PaddedHitRect` `Handle` `Hit` `Canvas` `CanvasScaler` `GraphicRaycaster` `EventSystem` `StandaloneInputModule` `EventTriggerType` `Physics2D` `Collider2D` `BoxCollider` `108f` `!u!223` `!u!114` `!u!115` `com.unity.modules.physics2d` `com.unity.ugui` `using UnityEngine.UI` `using UnityEngine.EventSystems`。

---

## 这一步最大的风险

**「只改半边」在 16:9 下逐位等价、全绿，坏只在别的宽高比上现形。**
正本 §3·1c 记的是**同一个坑已经踩过两次**（当天回滚），而根因是 **`FromPixel`/`ToPixel` 只在 16:9 互为逆**；§3·1d:345-348 明说**只换一侧比不换更坏**（小一圈 / 互相压住）。
**放大器**：正本 §3·2 —— 自检全程把 `cam.aspect` 钉在 `DesignAspect`（现核 `Editor/BattleScene.cs:23119`；正本引的 `:22701` 是旧行号），**生产跑的是 4:3 / 16:10 / 21:9**。
⇒ 第二风险紧随其后：`HitPaddedRect` 与 uGUI 自带的 rect∧`raycastPadding` **双轨并存**时，「同一按触发两次」的窗口是**静默的**（画面正常、只是点一下做了两件事）。
