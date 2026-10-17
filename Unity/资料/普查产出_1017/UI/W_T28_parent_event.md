# W_T28 — 活动承载窗 `ParentEventWindow`（界面线 T28）

> 会话日期：2026-10-17 · 执行写手 · 白名单：`Shell/ParentEventWindow.cs`（新建）· `Shell/WindowsManager.cs` · `Editor/MainMenuScene.cs`
> 本会话**不跑自检、不起玩家**；只跑 `bash d:/4/Unity/工具/typecheck.sh`。

---

## 一、结论（一句话）

**建出来了**：`Shell/ParentEventWindow.cs`（958 行，类名 == 文件名）+ `WindowsManager.PrefabRefParentEvent` /
`WindowsManager.OpenParentEvent(...)` + `Editor/MainMenuScene.cs` 新增 `T28` 断言一节（207 行、**72 处 `Check` 调用**；
`CheckFontWindow` 内部再各带两条）。
窗参 / 页签树 / 两页内容 / 数据缺口全部照 prefab 与反编译现读；`typecheck = 运行时 0 / 编辑器 0`。

---

## 二、改了什么

| 文件 | 改动 |
| --- | --- |
| `Unity/MyGame/Assets/CardPresentation/Shell/ParentEventWindow.cs` | **新建**（958 行）。窗根 / 压暗层 / `Content Area`+`Background`（纯代码渐变）/ `Tab Buttons`（底板 + Shadow + 母版 + 2 颗运行期页签）/ `Tabs`（两页）/ `Shadow (1)`（出厂关着）；两页的建树；`SwitchPage`；数据缺口与取图出声；自检读口。 |
| `Unity/MyGame/Assets/CardPresentation/Shell/WindowsManager.cs` | 加 `PrefabRefParentEvent = "Parent Event Window"`；加 `OpenParentEvent(Page page = ChooseSide)`（照 `OpenItemDisplay` / `OpenWhereToGetCards` 的形状，走 `OpenByRef`）。顺手把 `OpenWhereToGetCards` 里「承载窗 `Parent Event Window` 我们没建」那句就地订正（铁律 5）。 |
| `Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs` | 在 `T27` 那节之后、`=== 合计 ===` 之前插入 `T28` 一节（窗参 / 外壳 / 页签树 / 页签文字 / 两页切换 / 第 1 页内容 / 第 2 页内容 / 数据缺口 / 点压暗层关窗）。 |

⛔ 没碰：`WhereToGetCardsPopup.cs` · `MenuScroll.cs` · `Core/**` · `Battle/**` · `Deck/**` · 其它 Editor 宿主 · `项目任务.md` · `CLAUDE.md` · `资料/任务线/**` · `d:/2/**` · git。

---

## 三、两页是什么（`tabs` 的 2 个 PPtr，逐 MB 反解）

| # | PPtr | MonoScript 类名 | 页节点名 | 内容 |
| --- | --- | --- | --- | --- |
| 0 | `5874186985141415430` | `TwoSidesEvent_ChooseSideTab` | `Select Team Tab` | 两张队伍卡（A 红 / B 蓝，`UICornersGradient` 四角色逐条照抄）+ 标题底/`BATTLE FOR WARPFORGE` + `VS_icon` + 说明（含 `<color=#E27E1B>`）+ 返回钮 + Scan Lines / Glow |
| 1 | `-7528133727782064634` | `TwoSidesEvent_OnGoingTab` | `Event On Going Tab` | 红底/噪点/vignette + 表头（`Game mode` + 返回钮）+ 两张队伍卡（`#squidmar`）+ `Reward Display`（竖向进度条 + **5 档记分** `1256` + Collect + `Skulls: 125`）+ `Scoring slider` + `Your Team` 两行 + `Blue Team` 两行 + `TimerDisplay` |

页签按钮 = `Menu Navigation Panel Button` 模板的等价物：`Highlight`（红 `40k_main_bt_selected_BW`，九宫角块 = 30 ÷ 0.92 = **32.6087px**）·
`Icon`（**运行期图标留空**，见数据缺口）· `Label`（`40k_main_bt_nametag` 155×37.86）· `Badge Highlight`（`40K_notification_number` 35×35 · tint 0.7358）。
母版照 `TabButtons.Initialize` 第一句 **`SetActive(false)`**。
`Tabs` 下另建了原版模板页 **`Sample Tab`**（= `TabbedWindowComponents.tabPrefab`，`Header` + `Separator Line`，**出厂关着**）——
本窗两页是预置的（Two Sides 那份），模板只建不用。

---

## 四、判据对照（逐值）

| 项 | 值 | 出处 |
| --- | --- | --- |
| `type` | 0 Fullscreen | MB `MonoBehaviour_-1838579764277826042.json` |
| `windowsPlacement` | 15 Popup | 同上 |
| `closeOnESC` / `updateNavPanel` | 0 / 0 | 同上 |
| `extraScaleSmallScreen` | 1.0 | 同上 |
| `useDefaultCloseSoundIfNull` | 1 | 同上 |
| `openSound` / `closeSound` | 空 PPtr | 同上 |
| `tabs` | **2 个 PPtr**（R2 旧记「3 个」是错的） | 同上 |
| `backgroundCloseButton` | `2610068799221640710` | 同上 |
| `TabbedWindowComponents` | MB `-2754843456145227452`：`tabButtons=7645027671073194308` · `tabPrefab=1358808318654906692` · `tabHolder=6733376813114622276` | 现读 |
| `TabButtons` MB | `tabButtonPrefab=-5825802606871740092` · `options=[]` · `group=7535499631002485060`（`m_AllowSwitchOff=0`） | 现读 |
| `Content Area` | 167.2,70.9 → 1920,1080（1752.83×1009.06） | `D_ParentEventWindow_dump.txt` |
| `Background` | `Image.m_Sprite=0` + `UIGradient`：`m_color1=(0.2235294,0.0117647,0.0196078,1)` · `m_color2=(0.0470588,0,0.0156863,1)` · `m_angle=82` · `m_ignoreRatio=1` | MB `4376020486595051844` |
| `Tab Buttons` | 165×1009.06 · `40k_main_tab_background` · VLG spacing0 align1 pad(0,0,120,0) · `Gradient2` type1/Override/4 色键/2 alpha 键 | dump + MB `-4730011654172802748` / `-6105840469599813308` |
| `Shadow` | 47.64×1009.06 · `40k_main_tab_shadow` · (0,0,0,0.4745098) | dump + MB `-6636899021308591804` |
| `Shadow (1)` | 49.43×1009.06 · **出厂 INACT** | dump |
| 页签 | 165×180 · 第 1 颗 y 190.9（pad top 120）· 第 2 颗 y 370.9 | dump |
| `TabButtonLabel` | fs 25.65 · 基准 23 · auto[12~33] · 字距 −3.5 · 色 (0.9568628,0.8823530,0.6745098) · Center/Midline · NoWrap | TMP MB（4 份同值，如 `-1265065209958071996`） |
| 第 1 页标题 | `BATTLE FOR WARPFORGE` · fs72 · 基准36 · auto[18~72] · 字距 5 · 框 1337.67×100 | dump |
| 第 2 页表头 | `Game mode` · fs67.55 · 基准36 · auto[18~67.55] · 字距 5 | dump |
| 5 档记分 | 行 371.38×75.98 · `1256` fs48 · 箱子 `40k_Crate_Tier1_Iron`…`Tier5_Warp` | dump |
| Collect | `Collect` fs55 · 基准12 · auto[10~55] | dump |
| Total Victories | `125` fs48 | dump |

> ⚠️ **两个 prefab 的事实**（⛔ 别混成一份）：`Parent Event Window`（GO `-3290546486736551612`，组件
> `TabbedWindowComponents` + `BaseParentEventWindow`，`tabs=[]`、`placement=5`、`closeOnESC=1`、`updateNavPanel=1`）
> 提供**外壳树**；`Two Sides Event Window`（GO `-2798723332429074938`，组件 `TwoSidesEventWindow`）提供
> **窗参与 2 个 tab**。任务简报指定这种组合；原版运行期 `BaseParentEventWindow.SetupTabs` 才会往左栏
> `AddTabButton` —— 这是**我们挑的组合**，已在文件头如实写明。

---

## 五、数据缺口（如实留空 + 出声）

全部来自 **LiveOps 远端**，本地离线包体没有：

1. 两个 tab 的**标题词条 / 图标 sprite**（`LiveOpsAssetUtility.GetLabel` / `AddTabButton(label, sprite)` 实参）。
   判据：`官方本地化_en+zh-CN.json`（9,441 条）**0 命中** `Two Sides` / `Choose your side`。
   ⇒ 页签文字用**页节点名**兜底（`Select Team Tab` / `Event On Going Tab`）；**图标留空**（⛔ 不拿母版的 `40K_shop_bt_boosters` 冒充）。
2. 队伍名 / 队长 / 队伍图 / 分数 / 里程碑 / 奖励 / 倒计时截止时刻 / `GetTimeDescriptionTextKey` 的「Ends in:」。
   ⇒ 照 prefab 出厂原文画：`Red Team` / `Blue Team` / `#squidmar` / `1256` / `125` / `BATTLE FOR WARPFORGE` /
   `Choose your side. Fight for glory!` / `Progression` / `Collect` / `Skulls:` / `Game mode`；
   倒计时接 `TimerDisplay`，目标时刻 null ⇒ `--` + 出声一次。
3. **两张图本工程未导入**：`VS_icon`（369×381）· `40k_battle_Win_Skull_SDF`（128×128）
   ⇒ 对应件**不画 + 记进 `MissingArt` + 出声**（自检断言了这两条）。
4. `Crate Border Highlight`（描边框）未导入 ⇒ `Highlight Crate` **只建节点、不出图**（⛔ 不用实心色块顶）。

---

## 六、断言（`Editor/MainMenuScene.cs` · `T28` 节 · 72 处 `Check`）

期望值全部独立取自 prefab 字面量 / 根 MB（⛔ 不调被测窗的常量）：

- 窗参 7 条（type / placement / closeOnESC / updateNavPanel / extraScale / useDefaultCloseSound / 两个空 PPtr）
- 外壳 5 条（`Content Area` 宽高 / `Background` 纯代码渐变 / 宽 / `Tab Buttons` 宽高 / 底板 sprite）
- 页签栏 6 条（`Gradient2` 两端顶点色 / `Shadow` 宽 + 色 / `Shadow (1)` 关着 + 宽 / 母版关着）
- 页签树 5 条（2 颗 / 宽 165 / 高 180 / 两颗的绝对矩形 / 选中态只亮当前那颗）
- 页签文字 5 条（文字兜底 / 窗口 [12~33] / 基准 23 / 字距 −3.5 / 色 0.957,0.882,0.675 / 名牌 155×37.86）
- 两页 5 条（2 页 / 两个节点名 / 默认第 1 页 / `SetActive` 语义 / 切到第 2 页）
- 第 1 页 9 条（标题文字 / 窗口 / 字距 / 框宽 / 两张队伍卡 / A 红四角色 / B 蓝四角色 / `Red Team` + fs82.1 / `Select` 色 / 说明原文 / 返回钮命中）
- 第 2 页 9 条（表头文字 + 窗口 + 范围 / `Progression` / `Collect` + fs55 / `125` / 5 档 `1256` + fs48 / `Blue Team` / `--` / 返回钮命中）
- 缺口 2 条（`DataMissing` + `DataNote` 点名 LiveOps / `MissingArt` 两条）
- 关窗 3 条（压暗层命中区存在 / 点它关窗 / cue = `ButtonClick Back`）

---

## 七、typecheck 原文

```text
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

（本会话中途编辑器程序集红过 17→36→20 条，**全部**在 `Editor/DeckScene.cs` / `Editor/RuleEngineTest.cs`
—— 是别的写手正在改的文件；我只报自己白名单内文件的结果，最后一次跑到 0/0。）

---

## 八、没查清 / 已知偏离（⛔ 别当「已复刻」）

1. **`Gradient2` 的方向没能逐句确证**：`UnityEngine.UI.Extensions.Gradient2__ModifyMesh.c` 的方法体被反编译器
   打散（大量 `CONCAT44`），`GetPositions` 也读不出干净的 x/y 选择。当前按 **uGUI 顶点 y 向上 ⇒ t=0 在下**
   取两端（下 = `(0,0,0,1)`、上 = `(0.283,0.283,0.283,0)`）。
   ⚠️ 而且四个色键里**中段白平台**（0.3353→0.7324）我们只有四角顶点色、**表达不了** ⇒ 这是**两档近似**。
2. **`UICornersGradient` 的 `SkewedImage.skewVector = 0.09` 与 `Mask` 的裁切没做**（我们这一层没有斜切/掩码原语）。
3. **`UIAnchorAvoidSafeArea` 没接**（`Background` / `General Red Background` 各一颗）—— 本壳没有逐节点的安全区组件；
   `General Red Background` 自己的 rect 在 dump 里是**退化值**（`634.3 → 474.6`，负高），所以那一族用**子件导出值**
   （红底 `93.6 → 1015.4`、vignette `-28.2 → 1082.3`）。
4. **页签图标 / 页签文字**：本地无 liveops label/sprite ⇒ 文字用页节点名、图标留空（见 §五）。
5. **入口没接**：原版由 `ParentEventHandler` / liveops 事件链打开；我们只建 + 注册（`WindowsManager.OpenParentEvent`），
   ⛔ 不伪造调用点（同 `T26/T27` 口径）。
6. **`WhereToGetCardsPopup` 那条入口没接**：简报里提到「若反编译里 tab 的 onClick 就是它」。现读两个 tab 的 MB 字段：
   `TwoSidesEvent_ChooseSideTab` = `backButton / titleText / teamContainers[2]`；`TwoSidesEvent_OnGoingTab` =
   `header / teamDrawers / timerDisplay / …` —— **两页都没有 `getCardsWindow`**（那是 `ExpansionPassEventTab` /
   `GachaEventTab` 的字段）⇒ 本窗**不接**那条入口，也没有可接的按钮。
7. **`WhereToGetCardsPopup.cs` 的文件头仍写着「我们没建 `Parent Event Window`」** —— 该文件不在本任务白名单，
   **没改**；`WindowsManager.cs` 里同一句已就地订正，建议后续把那份文件头也改掉（铁律 5）。
8. 页树与 prefab 的**深度**不是逐层一致的：`Bottom Text` / `Top` / `Reward Display` 这些容器保留了，
   但 `Background noise` / `Foreground Image` 是 `Background` 的**兄弟**（prefab 里是子件）——
   矩形一律是**屏幕绝对坐标**、视觉位置一致，**层级不同**（如实记下，不影响命中/渲染队列）。
