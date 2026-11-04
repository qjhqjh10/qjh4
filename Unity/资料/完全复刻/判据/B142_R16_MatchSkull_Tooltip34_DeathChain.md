# B142_R16 · 三件判据调查：匹配骷髅三档 / Tooltip 34 / 阵亡链

> 只读代理 · 2026-11-04 · 波次 `B142`
> **未改任何源码 / 未跑 Unity / 未动 git / 未改活状态表**，只写本文件。
> 判据源次序：`d:/2/tools/decomp_full/*.c`（Ghidra，主力）→ `out_cpp2il_dev_ilspy/*.cs` → `out_il2csharp` → `assets_full/*.json` → `assetripper_out/ExportedProject`。
> 「素材」= `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1/`（13 场同一份 BattleHud，本报告用 arena1 为样本，其余 12 场同名同构）。

---

## 〇 · 三句话结论（先看这个）

| 编号 | 结论 | 一句话 |
|---|---|---|
| `B142-MATCHSKULL` | **⛔ 判据本身错**（若账里写的是「建 3 档里程碑图标」） | 原版 `Match Skull Mark` **从不被实例化**（`SetMilestones` 0 调用点、`BattleScoreIcon.Config/Check` 0 调用点），装它的容器 `Match Skulls` 还 `m_IsActive=false`。**可见行为 = 没有那 3 个图标**；活着的是 HUD 那条 `Milestones`（骷髅图 + `xN`），**我们已复刻** |
| `B142-TOOLTIP-AUTOSIZE-34` | **前提半错** | 「短 tooltip 渲 fontSizeMax」**成立**（fitter 量的是 max 号下的高，自指闭环）；但 **34 只在 `GameStaticData.smallScreenUI == true` 时**（默认 = Steam Deck / 玩家手开 Small UI）。**桌面档是 28**。照做只需**门控**，桌面一个像素都不变 |
| `B142-DEATH-CHAIN` | **前提半错** | 阵亡**不走** `SendToCemetery` / `DissolveWholeCard` / `DissolveMinionCard` / `FadeCardItems`（那四条是**手牌/回手/撤场**）。阵亡链 = `backlash(0x0d)` → `minionDeath(0x04)` → `CardScript.UnitDeath` 协程（抖 → 站 0.5 s → 死亡爆散件 + 关 3D 体 → 0.2 s → `GoToCemetery`）。`0x2d` 是**另一条**（残骸转生 / 回合末清残骸 / 灵魂石） |

---

## 一 · `B142-MATCHSKULL` —— 3 档阈值语义与几何

### 1·1 阈值语义（原版）

| 项 | 原版行为 | 出处（`d:/2/tools/decomp_full/`） |
|---|---|---|
| 阈值表来源 | `MatchData.GetMilestones()`：开关量是 **`playMode`**（`MatchData + 0x18`），**不是** `matchType` | `MatchData__GetMilestones.c:20` |
| 有 3 档的 9 个模式 | `Classic(0)` `Dungeon(3)` `OfflinePractice(6)` `ClosedDeck(7)` `Replay(10)` `RankedFriendly(11)` `OwnDeckTraining(12)` `Skirmish(13)` `Battle4Warpforge(14)` ⇒ 返回 **`[20, 10, 0]`**（降序） | 同文件 `:47`（0x14=20）`:62`（10）`:73`（0）；case 表 `:20-40` |
| 空表的 6 个模式 | `Duel(1)` `PracticeLodge(2)` `Tutorial(4)` `CutScene(5)` `Campaign(8)` `TutorialReplay(9)` ⇒ `Array.Empty` | 同文件 `:34-37` |
| 其余（≥15） | 原版**抛 `NotImplementedException`** | 同文件 `:38-46` |
| 阈值对象 | `HealthThresholdData { int healthThreshold(+0x10, get); bool AlreadyAccomplished(+0x14, private set) }` | `HealthThresholdData.cs:6-31` |
| 单条判定 | `CheckThreshold(life) = !AlreadyAccomplished && life <= healthThreshold` ⇒ 置 true 并返回 true | `HealthThresholdData__CheckThreshold.c:11-17` |
| 达成扫描 | 注册在 `EnemyHealthChangeSignal` 上：**逐条**（不是短路）`!done && signal.CurrentHealth <= threshold` ⇒ `done=true` 并 `UpdateMilestonesCount(下标)` | `BattleScoreManager__CheckThresholds.c:22-26` |
| 我方骷髅数 | `GetSkullCount()` = `Count(done)` | `BattleScoreManager.cs:64-88` |
| 对方骷髅数 | `GetEnemySkullCount(ownLife)` = 「阈值 ≥ 我方当前生命」的条数（`if (threshold < ownLife) 不计数`） | `BattleScoreManager__GetEnemySkullCount.c:25-31` |
| HUD 文本 | `UpdateMilestonesCount(i)` 写 **`x{i+1}`**；初始化传 `-1` ⇒ 显示 **`x0`** | `BattleScoreUiManager__UpdateMilestonesCount.c:13-16`；调用点 `BattleScoreManager__Initialize.c:54-60` |
| HUD 显隐 | `targetThresholds.Length > 0` ⇒ 才 `milestoneCountIcon.enabled = milestoneCountText.enabled = 真`（关的是**组件**，节点仍在） | `BattleScoreUiManager__Initialize.c:9-13` |

### 1·2 图标（`Match Skull Mark`）本来怎么用

| 环节 | 原版代码 | 出处 |
|---|---|---|
| 实例化 | `BattleScoreUiManager.SetMilestones(milestones)`：遍历，`Instantiate(milestonePrefab, milestonesMountPoint)` | `BattleScoreUiManager__SetMilestones.c:74` |
| 初始态 | `checkMarkIcon.enabled = false`（`:84`）· `targetPointsText.enabled = true`（`:89`） | 同上 |
| 填字 | `targetPointsText.text = threshold.ToString()` ⇒ `"20"` / `"10"` / `"0"`（**不是** `xN`） | 同上 `:96-101` |
| 收集 | 加进 `milestonesIcons` 列表 | 同上 `:126` |
| 勾选切换 | `BattleScoreIcon.Check(state)`：`targetPointsText.enabled = !state`、`checkMarkIcon.enabled = state` | `BattleScoreIcon.cs:20-25` |
| 配置入口 | `BattleScoreIcon.Config(data)`：关勾、开字、填阈值 | `BattleScoreIcon.cs:11-18` |

### 1·3 ⛔ 关键裁定：这条链在原版是【死】的

三条**互相独立**的证据（任何一条都足以判死）：

1. **`SetMilestones` 全库 0 调用点。** 它对 `*.c` 全量 grep 只命中它自己；`assetripper_out/ExportedProject` 的 1314 prefab + 全场景 grep `SetMilestones` **0 命中**（排除 UnityEvent 的 `m_MethodName` 这条路）。
2. **`BattleScoreIcon.Config` / `.Check` 全库 0 调用点**；`BattleScoreUiManager.Initialize(HealthThresholdData[])`（public）也是 **0 调用点**（`BattleScoreManager.Initialize` 是**内联**照抄它的三句，并不调它）。
3. **容器 `Match Skulls` 自己 `m_IsActive = false`**：`bundle_scenes_scenes_battlearena1/GameObject/Match Skulls.json`（GO 169，rect 2836，父 = `BattleSettingsPanel`）；运行期 dump 同样是 `activeSelf=False activeInHierarchy=False`（`资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_Battle_Arena_1.tsv`）。

⚠️ 唯一引用 `Match Skull Mark` 的地方就是 `BattleScoreUiManager.milestonePrefab` 那一个字段（`Battle Arena 1.unity:239621`，guid `6bb3f58d26b79cd4f87a5395e1ae24b2`），**场景里没有任何 prefab 实例**。

⇒ **可见行为 = 那 3 个图标一个都不出现**；设置面板里连 caption 也看不见（整块 `Match Skulls` 关着）。**真正活着的是 HUD 那条 `Milestones`**。

### 1·4 几何 / 锚点表（现读 arena1；13 场同名同值）

HUD 侧（`BackCanvas` → `LeftArea` → `PlayerInfo` → `Milestones`）：

| 节点 | 父 | anchorMin/Max | anchoredPos | sizeDelta | pivot | 备注 |
|---|---|---|---|---|---|---|
| `PlayerInfo` | `LeftArea` | (0,0)/(0,0) | (31.7, 28.0) | 260 × 75 | (0,0) | 左下角起算 |
| `Milestones` | `PlayerInfo` | (.5,.5)/(.5,.5) | (83.263, 55.1) | 173.885 × 38 | (.5,.5) | 容器 |
| `MatchSkulls Icon` | `Milestones` | (.5,.5)/(.5,.5) | (−51.579, 2.8) | 65.39 × 54.1375 | (.5,.5) | Image = `40k_battle_Win Skull`，白色 |
| `MatchSkulls Score` | `Milestones` | (.5,.5)/(.5,.5) | (27.7, −0.34) | 94.461 × 47.311 | (.5,.5) | TMP `m_fontSize 35` / auto 1 / max 35；模板文本 `x3` |

设置面板侧（`FrontCanvas` → `BattleSettingsPanel`，**整块 `m_IsActive=false`**）：

| 节点 | 父 | anchorMin/Max | anchoredPos | sizeDelta | pivot | 备注 |
|---|---|---|---|---|---|---|
| `BattleSettingsPanel` | `Safe area FrontCanvas` | (.5,.5)/(.5,.5) | (0, −29.317) | 743.202 × 758.635 | (.5,.5) | 运行期 dump：`False/False` |
| `Match Skulls`（容器，GO 169） | 同上 | (.5,0)/(.5,0) | (−0.4, −20.0) | 743.2 × 148.764 | (.5,1) | **`m_IsActive=false`** |
| `Match Skulls Text` | `Match Skulls` | (.5,0)/(.5,0) | (0.0028, 114.0) | 686.8 × 50 | (.5,0) | TMP `31.59` px · Center/Middle · 白 α=0.631 · 文本 `Reduce your oponents' health to win more skulls:`（**原版拼写就是 `oponents`**） |
| `Match Skulls`（挂点，GO 929） | `Match Skulls` | (.5,.5)/(.5,.5) | (0.00045, −13.224) | 686.8 × 75.212 | (.5,.5) | 挂 **`HorizontalLayoutGroup`**：`childAlignment 4`(MiddleCenter) · `spacing 0` · `childForceExpand W/H=1` · `childControl W/H=0` |

`Match Skull Mark` prefab（`assetripper_out/ExportedProject/Assets/Match Skull Mark.prefab`，源包 `battlesharedresources`）：

| 子件 | rect（sizeDelta） | anchoredPos | 图 / 颜色 | 行号 |
|---|---|---|---|---|
| 根（`BattleScoreIcon` 所在） | 220.1087 × 71.5221 | (−23.974, −0.761) | anchor (0,0)、pivot (.5,.5) | `:114-115`；组件 `:137-140` |
| `background` | 247.2161 × 66.8974 | (0, 1.5513) | `40k_bt_underbutton_0`（9 宫格）· 色 **(0.3632, 1, 0.5798)** 绿 | `:158-159, 182, 189` |
| `targetText` | 89.464 × 66.219 | (26.3, 0) | TMP `m_fontSize 76` / auto **0** / max 72 · `HAlign Center` · `VAlign 4096`(=Geometry) | `:216-217, 274` |
| `checkmark` | 59.756 × 62.439 | (34.2, 0) | `40k_tick_0` · `PreserveAspect 1` | `:335-336, 366` |
| `badge` | 67.0158 × 69.6988 | (−62.1, −1.74) | `40K_missions_icon_Daily skulls` | `:393-394, 424` |

> 层级顺序（`m_Children`）：`background` → `targetText` → `checkmark` → `badge`。
> 若真被实例化到挂点：HLG 横向排 3 个 220.1 宽 = 660.3 ≤ 686.8，`MiddleCenter` 居中，视觉上就是「[徽章][20/勾] [徽章][10/勾] [徽章][0/勾]」。

### 1·5 我们该在哪建、建什么、数据从哪来

| 项 | 我们的现状（现读） | 判定 |
|---|---|---|
| 阈值表 `{20,10,0}` | `RuleEngine/Core/DeckRules.cs` 的 `SkullThresholds`；`SkullsFor(hp)` | ✅ 已有 |
| 模式门控（哪 9 档有、哪 6 档没有） | `CardPresentation/Battle/BattleDriver.cs:40866-40905`（`MilestonesExist(ctx)`，逐模式列出，含「≥15 原版抛异常、我们不抛」的说明） | ✅ 已有 |
| HUD 骷髅图 + `xN` | `BattleDriver.cs:2594-2600`（`SkullIconX01/Y01/Px`、`SkullScoreX01/Y01`）+ `_foeSkullCount`（事件驱动、单调不回退，`:2400-2429`） | ✅ 已有 |
| 结算面板骷髅 | `Battle/EndPanel.cs:221`（`_skulls = new HudPiece[DeckRules.SkullThresholds.Length]`） | ✅ 已有 |
| 3 个 `Match Skull Mark` | —— | **⛔ 不建**：原版从不实例化；我们若建就得有人**调用**它，画面反而比原版多 3 个图标（方向错） |
| 设置面板那句 caption | 没建 | **也不建**：整块 `m_IsActive=false`，原版从来不可见 |

**给活状态表的处置建议**：若表里存在「建 3 档里程碑图标 / Match Skull Mark」这一行 ⇒ 判 **`⛔ 判据本身错`**，就地改写成「原版该 prefab 是死件（`SetMilestones` 0 调用点 + 容器 `m_IsActive=false`），不需要建；已完成的是 HUD `Milestones`」。不要新开一条「补建」的活。

### 1·6 还差什么

- **没跑实况**：`m_IsActive=false` 是**序列化值**；我没有进原版游戏确认「设置面板打开时这块会不会被某段代码/动画打开」。本机反编译里没有找到置 true 的路径，但「没找到」≠「不存在」（铁律 3）。要彻底钉死需要一次实况（打开战斗设置面板截图）——按 2026-11-02 用户口径，战场实拍已一次性做完，不必再进原版，故本条**记为未验证**。
- 若将来有人复刻「原版自己的死代码」：本条给的全套几何/语义都在，可直接照 §1·4 建，但**必须在账里写明「原版不显示」**，否则会被当成缺功能。

---

## 二 · `B142-TOOLTIP-AUTOSIZE-34` —— 34 px 覆写到底成不成立

### 2·1 判据表

| 项 | 原版值 / 行为 | 出处 |
|---|---|---|
| `EverguildTextController` 字段 | `maxFontSize(+0x20)` · `minFontSize(+0x24)` | `EverguildTextController.cs:5-9` |
| Awake 门控 | `if (!Application.isPlaying \|\| !GameStaticData.smallScreenUI) return;` ⇒ **整条覆写只在 smallScreenUI 下生效** | `EverguildTextController__Awake.c:17-20`（`+0xb8` 里 `+0x11c` 那一字节）；`EverguildTextController.cs:11-17` |
| 覆写本体 | `max>0`：`!autoSizing ? fontSize = max : fontSizeMax = max`；`min>0`：`fontSizeMin = min` | 同上 `.c:21-38` |
| `BasicToolTip ▸ Text (TMP)` | `m_fontSize 28` · `m_fontSizeBase 28` · `m_enableAutoSizing 1` · `min 10` · **`max 28`** | `BasicToolTip.prefab:481-486` |
| 同节点第三颗组件 | `EverguildTextController { maxFontSize: 34, minFontSize: 10 }` | `BasicToolTip.prefab:554-558` |
| 同节点 fitter | `ContentSizeFitterMinMax`：`HorizontalFit 0`(Unconstrained) · `VerticalFit 2`(PreferredSize) · `clampHeight 1` · `heightMin 90` · `heightMax 200` · `clampWidth 0` | `BasicToolTip.prefab:537-544` |
| 正文框 | `sizeDelta (468.90002, 0)` · `TextWrappingMode 1` · `overflowMode 0` | `BasicToolTip.prefab:424/497/499` |
| `smallScreenUI` 出厂值 | `false`（`GameStaticData` 静态构造） | `GameStaticData.cs:2357` |
| `smallScreenUI` 自动来源 | 未手选时 = `GameStaticData.DefaultTouchInput` = **`SteamUtils.IsSteamRunningOnSteamDeck()`**；手选时读存档 | `PlayerDataManager.cs:10362-10370`；`GameStaticData.cs:497-499` |
| 手选写入点 | 设置窗「图像」页那颗开关：同时写 `smallScreenUI` 与 `smallUIChosenManually` | `GraphicsTab__SmallScreenToggleClick.c:12-13` |
| 另一个同款 prefab | `Battle Tooltip Variant`：`m_fontSize 34` / `base 28` / `auto 1` / **`max 34`** + ETC 34 | `Battle Tooltip Variant.prefab:481-486, 557-558` |
| 菜单里真正在用的变体 | `Updated Tooltip Variant`：`m_fontSize 30` / `base 28` / `max 30` + ETC **34**（19 处引用） | `Updated Tooltip Variant.prefab:544-549, 620-621` |

引用面（现读 `assetripper_out`，全 prefab + scene）：

| prefab | guid | 被引用文件数 |
|---|---|---|
| `BasicToolTip` | `92122eeb…` | **45**（含 13 个战场场景 + 主菜单） |
| `Updated Tooltip Variant` | `bcb14978…` | 19 |
| `Trait Tooltip` | `08c1f7da…` | 14 |
| `Battle Tooltip Variant` | `9d482503…` | **0**（只有它自己的 `.meta`）⇒ 本 build 里是死件 |

### 2·2 结论：「短 tooltip 实际渲 34 px」——**只在小屏档成立**

| 场景 | `m_fontSizeMax` | 短文案实渲 | 判定 |
|---|---|---|---|
| 桌面 PC（`smallScreenUI=false`，出厂默认） | **28**（ETC 在 Awake 第一句就 return） | **28 px** | 🟠 **「渲 34」不成立** |
| Steam Deck / 玩家手开 Small UI | **34**（ETC 把 `fontSizeMax` 改写成 34） | **34 px** | ✅ 成立 |
| 战斗里若用 `Battle Tooltip Variant` | 34（序列化就是 34，与 ETC 无关） | 34 px | ⚠️ 但该 prefab **0 引用** |
| 菜单里的 `Updated Tooltip Variant` | 30（小屏 34） | 30 / 34 px | 与 BasicToolTip 同一套机制 |

⇒ 之前 `报告/B142_E25.md` §四·1 写的「短路 tooltip 在原版**多半**渲 34 px」**结论方向不对**：它只读到 `.c` 里 `if (maxFontSize > 0)` 那两句，漏了上面那层 `smallScreenUI` 门控（`.c:17-20`）。**桌面档位是 28。**

### 2·3 为什么「短文案恒等于 `fontSizeMax`」—— fitter 自指 + 框高来源（这次钉死）

1. **TMP 的「首选尺寸」是在 `fontSizeMax` 下量的**：
   `TMP_Text.GetPreferredWidth/Height()` 第一句就是
   `float fontSize = m_enableAutoSizing ? m_fontSizeMax : m_fontSize;`
   （`Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/TMP/TMP_Text.cs:3684 / 3711 / 3730 / 3758 / 3799`）
   ⇒ **ContentSizeFitter 拿到的高，是「按 max 号排出来」的高**，不是按当前收敛值。
2. **框高来源**：`ContentSizeFitterMinMax` 继承 `ContentSizeFitter`，`SetLayoutVertical()` 先跑基类
   （`VerticalFit = PreferredSize` ⇒ `sizeDelta.y = preferredHeight`），再 `ClampSize()` 把 y 夹进
   `[heightMin 90, heightMax 200]`（`ContentSizeFitterMinMax.cs:40-57`，字段定义 `:8-18`）。
3. **闭环**：框高 ≈ 「@max 的自然高」⇒ TMP 的自适应（`TextMeshPro.cs:2148` 起、增长判据 `:4139`）
   每次都「正好装得下」⇒ `m_fontSize` 停在 `m_fontSizeMax`。
4. **唯一会缩的一档**：若「@max 的自然高 > 200」⇒ 框被夹到 200 ⇒ 这时才真的二分下缩，缩到装进 200 为止（地板 `fontSizeMin = 10`）。
5. 宽度不参与压缩：`HorizontalFit = 0`（不控宽），框宽恒 468.9，`TextWrappingMode 1` 自己折行 ⇒ 只影响行数（从而影响高）。

⇒ 一句话：**框高是被 max 号撑出来的，所以字号恒等于 `fontSizeMax`，直到 200 px 上限被顶满才开始缩。**

### 2·4 要不要照做 / 最小落点

**要照做，但照的是「机制」不是「常数 34」。**（铁律 11「全部都要做」+ 18·0「默认复刻原版」）

| 落点 | 改什么 | 备注 |
|---|---|---|
| `CardPresentation/Core/Tooltip.cs:129`（常量行） | 追加一格 `BodyMaxPx = SmallScreenUI.Enabled ? 34f : BodyFontPx`（或等价三元） | 现有 `BodyFontPx = 28f` / `BodyMinPx = 10f` 保留不动 |
| `Tooltip.cs:603`（`SetScriptHeight`） | 小屏档传 `34`，桌面档仍 `28` | 原版 `m_fontSizeBase` 序列化是 **28**，但「@max 量框」⇒ 我们要让**框高按 max 量**，所以名义尺寸跟 max 走 |
| `Tooltip.cs:608`（`SetAutoFitBox` 与 `_h` 重算） | 第 4 实参 `maxPx` → `BodyMaxPx`；第 5 实参 `basePx` → 保持 `BodyFontPx`（28） | 原版 base=28（`prefab:482`）⇒ **别把 base 也写成 34** |
| `CardPresentation/Shell/TransformScalerBySmallScreenUI.cs` | **不用改** —— `CardPresentation.SmallScreenUI.Enabled` 就是原版那一格的等价物 | 现读已存在，默认 false、设置窗可切 |

**三个「为什么现在可以照做」**（逐条回 `B142_E25` §四·1 记的两个顾虑）：

1. 「要复刻跨帧布局回环」——**不必**。我们用的是显式 `SetAutoFitBox`，它的语义已经等价于「恒 max，超 200 才缩」；原版那条回环在**可见结果**上就是这两条。
2. 「会把普通文案 28.00 基线整体抬到 34」——**不会**。门控在 `SmallScreenUI.Enabled`，桌面档默认 false ⇒ **一个像素都不变**，`B141_W19` / `B142_E6` 那些以 28 为基准的断言不受影响。
3. 「牵动多条断言」——只会牵动**小屏档**的新断言（可加一条：`Set(SmallScreenUI)=true` 时 `em ≈ 34`，`false` 时 `em ≈ 28`）。

### 2·5 还差什么

- **没跑 Unity**：`em` 的真实收敛值（28 / 34）、以及小屏档下框高是否真的变高，**都没有实测**。本节的「恒 max」是从 **TMP 源码 `GetPreferredHeight` 用 `m_fontSizeMax`** 这一条推出来的（比 `B142_E25` 的「推演」硬，但仍不是实拍）。
- 未核对 `Tooltip.cs` 里 `BodyFontPx` 在被 `SetAutoFitBox` 消费之外还有没有别的用途（若还有「按 28 算行数」一类的地方，小屏档下可能要一起跟）。**建议写手动手前 grep 一次 `BodyFontPx`。**

---

## 三 · `B142-DEATH-CHAIN` —— 阵亡表现链

### 3·1 完整调用链（`.c` 文件:行）

| # | 环节 | 谁调谁 | 干什么 | 出处 |
|---|---|---|---|---|
| ① | 伤害结算 → `CheckIfDead` | `CardScript._ReceiveDamage_d__381__MoveNext` → `CardScript.CheckIfDead` | HP ≤ 0：**残骸支** ⇒ `TriggerUnitBacklashActions` + `AddTransformIntoRemnant`(0x48)；**普通支** ⇒ `cardState = waitingToDie(5)`、关 `canAttackAnim`、`TriggerUnitBacklashActions` | `CardScript__CheckIfDead.c:124-143` |
| ② | `TriggerUnitBacklashActions` | 由 ① 调 | **无条件**入队 `BattleAction 0x0d (backlash)`，priority = `globalVars+0xc0`；若带 `backlash(120)` 特性，先跑 `ThisUnitDeath(0x50)` 触发 | `CardScript__TriggerUnitBacklashActions.c:45`（`local_308[0] = 0xd`）`:55`（trigger 0x50）`:181`（入队） |
| ③ | `ResolveAction` case **0x0d** → `ResolveBacklash` 协程 | `BattleManager.ResolveAction` | 条件 `state==5 或 isRemnant 或 HasEnoughPendingDamage…` ⇒ `ResolveDeadCard(action)`（死亡触发：`TriggerOnMinionDeath` / `RemoveExtrinsicEffectsFrom` / requiem / sacrifice / 督军死亡）→ `TriggerUnitDeathActions(...)` → `AddReassembleMinionsOrder` | `BattleManager._ResolveBacklash_d__451__MoveNext.c:30`（条件）`:94`（TriggerUnitDeathActions）；`CardScript__ResolveDeadCard.c:236-291` |
| ④ | `TriggerUnitDeathActions` | 由 ③ 调 | 非督军 ⇒ 入队 `BattleAction 0x04 (minionDeath)`；督军只打日志 | `CardScript__TriggerUnitDeathActions.c:13-17`（督军分支）`:45`（`local_208[0] = 4`）`:123`（入队） |
| ⑤ | `ResolveAction` case **0x04** → `ResolveMinionDeath` 协程 | `BattleManager.ResolveAction` | 要求 `state==5`：`MinionManager.RemoveMinion`（离场 + 补位）→ 从在场表移除 → `AddReassembleMinionsOrder` → `ChallengeLogMgr.TroopKilled` → **`StartCoroutine(CardScript.UnitDeath)`** → 完事双方 `PlayerHand.UpdateCardEffects` → `FinishResolvingAction` | `BattleManager._ResolveMinionDeath_d__454__MoveNext.c:56`（RemoveMinion）`:61`（Reassemble）`:112`（TroopKilled）`:119`（StartCoroutine）`:145-149` |
| ⑥ | `CardScript.UnitDeath` 协程 | 由 ⑤ 起 | 见 §3·2 分拍表 | `CardScript._UnitDeath_d__446__MoveNext.c` |
| ⑦ | `GoToCemetery` | ⑥ 的 state 2 | `state = inCemetery(6)`；单位 ⇒ `ResetToValuesInHand`；带 `ephemeral(5)` ⇒ **只 `SetActive(false)`**；否则 `BattleManager.AddToCemetery` → `CemeteryManager.AddCardToCemetery` | `CardScript__GoToCemetery.c:8,10-12,14-17,24`；`BattleManager__AddToCemetery.c:6` |

`UnitDeath` 分拍（⑥）：

| 拍 | 干什么 | 出处 |
|---|---|---|
| 0-a | 校验：对象活着 + `state == 5`，否则 `LogWarning` 直接返回 | `:60-97`（`op_Equality` / `activeSelf` / `0x228 != 5`） |
| 0-b | `RawCardScript.GetDeathSound()` → `BattleManager.PlaySoundClip` | `:97` |
| 0-c | `DOShakePosition`（**不 await**）：时长 督军 = `globalVars+0x34`（0.5）· 小兵 = `+0x38`（0.2）· vibrato 10 | `:114`（`uVar15` 在 `:100-109` 分档） |
| 1 | `WaitForSeconds(globalVars+0x34)` = **0.5**（**不分档**） | `:122` |
| 2 | 生成死亡爆散体：`RawCardScript` 的 deathVfx AssetReference（`+0xd8` → `AssetReferenceTyped.Load`），取不到就用 `card+0x1d8` 的 prefab；位置 = 卡位、旋转取 `cardUI+0x180`、scale 取它的 `lossyScale`；材质写随机 float + 卡面纹理 + 按 `CardTier` 索引的第二张纹理 → `cardUI+0x158.SetActive(false)` → `cardUI+0x2e0` vtable `+0x1b8` | `:233` `:262` `:333` |
| 2' | 残骸支：`BattleCardUI.ShowOriginalRemnantCard` | `:341-352` |
| 3 | `SoundManager.Play3D`（`card+0x1d0` 的 clip） | `:368` |
| 4 | `StopAnimsInPlay` → `WaitForSeconds(globalVars+0x8c)` = **0.2** | `:374` `:379` |
| 5 | `GoToCemetery()` | `:152` |

### 3·2 ⛔ 判据纠偏：四条「看着像阵亡」的其实不是

| 方法 | 真正的调用者（全量 grep） | 用途 |
|---|---|---|
| `CardScript.SendToCemetery` | `PlayerHand._AddDrawnCardToHand` / `_AddCardNotDrawnToHand` / `_AddCardNotDrawnToHandAtIndex` 三个协程 + `BattleManager._ResolveCopyUnitInCemetery` | **手牌/牌库卡的销毁**（`StopAnimsInPlay` → `DissolveWholeCard(globalVars+0xb0)` → `GoToCemetery`，`CardScript__SendToCemetery.c:8-13`） |
| `CardScript.DissolveWholeCard` | `DestroyCard` 协程 · `SendToCemetery` · `MoveIntoFieldFromHand` · `HideHeroAtStart` · `TutorialSetup` · `TimedCampaignSetup` · `_ResolvePlayCardFromHand` | 卡「消融」（`DOScale × globalVars+0x40` → `StopShowingCodex` → `StopReflected` → `ToggleHighlight(false)` → `FadeCardItems(0,t)`），`CardScript__DissolveWholeCard.c:9-33` |
| `CardScript.DissolveMinionCard` | 只有 `MoveBackToHand` / `OutFromPlayingAreaAnim` | **回手 / 撤场**（不是死亡） |
| `BattleCardUI.FadeCardItems(newAlpha, time)` | `DissolveWholeCard` / `UnDissolveWholeCard` / `UndissolveToDeck` / `PlayerHand._FinishMulligan` | 就是 `BasicCardUI.FadeCard`（淡 `fadeGroup`），`BattleCardUI.cs:1491-1494` |

⇒ **单位阵亡不使用溶解**。阵亡的四拍是「抖 → 站 0.5 s → 死亡爆散件 + 关 3D 体 → 0.2 s → 进墓地」。

> 另：`BattleManager.DestroyUnit` 入队的 **0x2d** 走的是另一条 —— `ResolveAction.c:4015-4061` 的 `case 0x2d` → `BattleManager.ResolveDestroyUnit`，它要求 `state==waitingToDie(5)`；若 `HasToTransformIntoRemnant` 则 `TriggerUnitBacklashActions` + 入队 **0x48（转残骸）**，否则只 `LogWarning` + `FinishResolvingAction`。`DestroyUnit` 的调用者只有 `CardScript.ResolveTransient`、回合末清残骸、灵魂石收集三族 ⇒ **它不是普通阵亡的入口**。

### 3·3 我们对应到哪 / 缺什么

| 原版拍 | 我们的对应物（现读） | 状态 |
|---|---|---|
| 三拍时刻表（0.5 / 0.7） | `Core/CardFeel.cs:2380-2420` 的 `DeathBeats{ShakeEnd, BodyAt=0.5, CemeteryAt=0.7}` / `DeathTotal` | ✅ 已有，且**抖动与 0.5 s 等待同帧起算**（W61 已订正） |
| 抖 → 爆散 → 关 3D 体 → 进墓地 | `BattleDriver.cs:25620-25670` `PlayDeathFeel`：`StopInPlayAnims` → `CardFeel.DeathExplosion`（含 `DeathTotal` 0.7s、`onDone` 销毁） | ✅ 已有 |
| 死亡音效 / 3D 音 | `CardFeel.DeathExplosion` + `BattleDriver.cs:21580`（`UnitDeath → Play3D` 在单位格位置播） | ✅ 已有 |
| 墓地落点 / 缩放 / 用时 | `CardFeel.cs:2387-2406`：`CemeteryCardX=-8.14` `CemeteryCardY=-0.2` `Scale=0.65` `GrowTime=0.3`（`OutCubic`） | ✅ 已有 |
| `ChallengeLogMgr.TroopKilled` | `TroopsKilledCount.OnTroopKilled` + `LiveOpsSignal`；但我们在**结算拍批量补发**（`BattleDriver.cs:38947-38957`，自标 `🔶 自己设计`） | 🟠 **数值一致、时序不同** |
| `backlash(0x0d)` 这一拍 | `RuleEngine/Core/ActionPump.cs:1148/1306/1333/1910`（`KeywordTable.Backlash` → `ThisUnitDeath` 80；`DyingThisAction` 写点 `RuleCore.cs:8938`） | ✅ 引擎有等价物（**未逐拍核对优先级 `globalVars+0xc0`**） |
| `ephemeral(5)` 支（只关对象、不进墓地） | 全仓 `IsEphemeral` 只用于**手牌**角标（`BattleDriver.cs:27568`）；`GoToCemetery` 那条「带 ephemeral 就不进墓地」的分支**没找到对应物** | ❓ **未确认有**（可能是缺口；也可能因为我们的墓地只是日志列表，不影响画面） |
| 残骸 `0x48` 支 | `BattleDriver.cs:25622-25624`（翻面成残骸 ⇒ 同一张卡继续站着） | ✅ 已有 |

**另外发现一处文档自相矛盾（铁律 5，建议就地改，本代理不改）**：
`Core/CardFeel.cs` 的 `DeathFxHold` doc 还写着「阵亡视图从引擎说它死了到被收掉一共 **0.9 s（小兵）/ 1.2 s（督军）**」——
而同文件 `DeathBeats` / `DeathTotal`（`:2412-2432`）与 `Core/EventTiming.cs:660-669` 都是 **0.7 s（不分档）**。
`.c` 判据站 0.7 这边（`_UnitDeath:114-126` 抖动**不等** + `:117-122` 0.5 + `:374-379` 0.2）⇒ **`DeathFxHold` 那句是旧口径，应删/改**。

### 3·4 还差什么

- `backlash` 的**优先级**（原版 `globalVars+0xc0`）与我们动作泵的排序是否一致 —— 属于引擎线，本次没展开。
- `ephemeral` 不进墓地那支**是否存在**：需要在 `RuleEngine` 侧确认「死亡落账」是否按特性分流。
- `ResolveDeadCard` 的死亡触发顺序（`TriggerOnMinionDeath` → `RemoveExtrinsicEffectsFrom` → requiem / sacrifice）与我们的实现是否逐条同序 —— 本次只定位了链，没有逐条对。
- vtable 槽位（`+0x1f8` / `+0x268` / `+0x1b8` / `+0x1c8`）**没有逐槽对表**（`dump.cs` 有 `Slot:` 注解可查，但对象布局要先解 `Il2CppClass` 头长度）⇒ 本报告只按**上下文语义**描述它们做了什么，没有点名方法。

---

## 四 · 本报告改动了什么 / 没改动什么

- ✅ 只新建本文件。
- ⛔ 没改源码、没改活状态表、没跑 Unity、没动 git。
- 建议调度台据此改三处账（**由调度台改，不是我改**）：
  1. `B142-MATCHSKULL` ⇒ 判 `⛔ 判据本身错`（原版该 prefab 是死件），HUD 那一半已复刻。
  2. `B142-TOOLTIP-AUTOSIZE-34` ⇒ 结论改成「**门控在 `smallScreenUI`，桌面 28 / 小屏 34**」，落点按 §2·4 三处。
  3. `B142-DEATH-CHAIN` ⇒ 判据纠偏（四条 dissolve 与阵亡无关）+ 顺手记 `CardFeel.DeathFxHold` 那句旧口径要改。
