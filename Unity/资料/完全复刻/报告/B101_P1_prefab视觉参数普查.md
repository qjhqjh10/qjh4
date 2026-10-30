# B101_P1 —— prefab 视觉参数 vs 我们实现（普查）

> 只读普查代理 `B101_P1`，2026-10-30。**未改任何 `.cs` / 账 / 正本 / Resources**；未跑 Unity / ruleprobe / git。
> 产物：本文 + `判据/B101_P1_prefab视觉参数.tsv`（逐条表，`\t` 分隔，供下一波工具化）。

---

## §一 方法（怎么解 json、怎么定位我们的文件）

### 1·1 数据形状（现读，⛔ 不是假设）

`D:/2/新解包资源/assets_full/bundle_menus_assets_all/` 下**没有 `Image/` `Text/` 目录**（`ls` 现读 22 个类型目录）。
uGUI 的 `Image` / `Text` / `TMP` **全部躺在 `MonoBehaviour/` 里**，靠字段名区分：

| 类型 | 判别键（现读） | 数量 |
|---|---|---|
| `Image` / `RawImage` | 有 `m_Sprite` + `m_Color` + `m_RaycastTarget` | 11,880（含 Text） |
| uGUI `Text` | 有 `m_FontData` + `m_Text` | 5 |
| TMP | 有 `m_fontAsset` + `m_text` | 4,894 |
| 自定义脚本 | 有 `m_Script`，类名要反查 | 其余 |

- `GameObject/<名>.json`（同名的多实例写成 `<名>_<pathID>.json`）· `RectTransform/RectTransform_<pid>.json` · `Transform/…`。
- 类名反查：`m_Script.m_PathID` → `bundle_Waprforge_monoscripts/MonoScript/MonoScript_<pid>.json` 的 **`m_ClassName`**（722 条，命中率 100%）。
- 🔴 **`m_Children` / `m_Father` 全是本包内引用**（现数 15,266 条子引用，`m_FileID != 0` = **0**）⇒ 不需要跨包拼树。
- 子树根判据 = `m_Father.m_PathID == 0` ⇒ 本包 **313 棵树**（实测 308 棵能走通，5 棵无 transform 组件）。

### 1·2 四类「非默认值」怎么判

| 类 | 判据 |
|---|---|
| `m_LocalScale` | `x/y/z` 任一 ≠ 1（EPS 1e-4，⛔ 不用 `==`） |
| `m_LocalRotation` | 四元数 → 欧拉；`|x|,|y|,|z|` 任一 ≥ 0.01° |
| `m_Color` | `Image`/`Text`/`TMP` 的 RGB ≠ 白 或 α ≠ 1 |
| `m_IsActive` | `false` |

### 1·3 复现脚本（两支，本会话写在 `d:/4/_tmp_view/b101/`，⛔ 未入仓）

`scan.py <tree|delta> <bundleDir> <PrefabName>` —— 打单棵子树（`tree` 打全树、`delta` 只打非默认）。
`sweep.py <bundleDir> <out.tsv>` —— 遍历全部根，聚合 `prefab | nodePath | param | values | instances`。

`sweep.py` 核心 = 对每个 `m_Father == 0` 的 RT 跑一遍 DFS，对每个节点取 `deltas()`（四类判据），
按 `(prefabName, nodePath, param)` 聚合成 `set(values)` + 计数。**同名多实例逐个算**（值相同才合并成一行）。

> 全量 sweep 结果：**308 棵 prefab / 6,114 行**（`m_IsActive` 2,519 · `Image.m_Color` 1,938 · `m_LocalScale` 1,452 · `m_LocalRotation` 205）。
> 那张 6,114 行的原始表只在工作目录（`_tmp_view/b101/sweep_menus.tsv`）；本报告 §二 只收**能落到我们代码**的那些。
> ⚠️ 工作目录会被清 ⇒ 下一波要全量请按上面两支脚本重跑（7 秒）。

### 1·4 怎么定位我们的落点（**这一步决定结论可不可信**）

⛔ **不能靠 grep 数值**：本工程大量采用「**把倍率乘进矩形**」的口径（先例 `Shell/AvatarTab.cs:207` 的
`ScaleImage=2.0` 那段 doc、`Shell/BoosterInfoPopup.cs` 的 `* iconScl`）—— 烘进矩形之后，
**原版的 `1.25` / `0.87626` 在代码里一个字符都不会出现**，grep 必然漏判成「缺失」。

本节每条结论按下面三条之一取证：
- **A（✅ 已对上）**：能指名到「同一个值 / 同一条换算」的常量或算式（给 `文件:行`）；
- **B（❌ 缺失）**：先在 prefab 上**按锚点 + pivot 算出该节点的**布局矩形**，再乘 `m_LocalScale` 得**视觉矩形**，
  然后把我们代码里那个矩形与之逐数比对（差额 > 1 px 才算缺失）—— §二 里带「实算」两字的都是这么来的；
- **C（？ 判不准）**：我们**根本没建这个节点**，或建的是同族另一个件。

---

## §二 逐条表

> `原版值` 列的「视觉」= 布局矩形 × `m_LocalScale`（绕 pivot，本批全部 `pivot=(0.5,0.5)`，即绕中心）。
> 行内坐标一律 **行/父级局部、左上原点**；`r.x1/r.y1` = 宿主矩形左上角。

### 2·1 `Avatar Item Small` 一族（**本批唯一确认的成片缺口**）

prefab 事实（`bundle_menus_assets_all`，**7 个主实例 + 2 个 `_Ref` 变体，9/9 同值**）：

| 节点（相对 `Image Container`） | 锚点 / 偏移 | 布局框 | `m_LocalScale` | **视觉框** |
|---|---|---|---|---|
| `Highlight` | stretch `(0,0)-(1,1)` · `apos(1.669,2.600)` · `size(3.338,1.855)` | 宿主 +3.34 宽 / +1.86 高 | **1.9** | 布局 × 1.9 |
| `Border` | stretch `(0,-0.1)-(1,0.9)` · `size(0,0)` | 与容器同宽、高 = 容器高 | **1.25** | 布局 × 1.25 |
| `Image` | stretch `(0,0)-(1,1)` · `apos.y=2.700` | 与容器同框、上移 2.7 | **2.0**（z = 1） | 布局 × 2.0 |

**以 `Alliance Member Entry`（行 750×100）为例实算**（容器 = `Avatar Item Small` 内缩 y 37.368 ⇒ 98.877×65.694，
中心 `(r.x1+100, r.y1+44.716)`）：

| 节点 | 布局框（实算） | ×scale 后视觉框 | 我们的落点 | 判档 |
|---|---|---|---|---|
| `Highlight` | `x[50.57,152.78] y[8.34,79.24]`（102.21×70.90） | `x[4.58,198.78] y[−23.57,111.15]`（194.20×134.71） | `Shell/AllianceMemberTab.cs:1907-1908` 画 **102.2×70.9** | **❌** |
| `Border` | `x[50.57,149.44] y[18.44,84.13]`（98.88×65.69） | `x[38.20,161.80] y[10.23,92.35]`（123.60×82.12） | `Shell/AllianceMemberTab.cs:1911-1912` 画 **98.9×65.7** | **❌** |
| `Image` | `x[50.57,149.44] y[9.17,74.86]`（98.88×65.69） | `x[1.13,198.88] y[−23.68,107.71]`（197.75×131.39） | `Shell/AllianceMemberTab.cs:1910` 画 **98.9×65.7** | **❌** |

⇒ 三个倍率全丢，立绘只有原版的 **50%**（与大抽屉 `AvatarTab` 同族的 E18 修法只覆盖了三页，**没覆盖成员行**）。

同 prefab 的另外 5 个宿主（逐条见 TSV）：

| 宿主（实例） | 判定 | 落点 / 理由 |
|---|---|---|
| `Player Profile Window ▸ … ▸ Avatar Menu Item` | ✅ | `Shell/AvatarTab.cs:321/334/335`（`ScaleAboutCenter` + `ScaleImage=2.0/ScaleBorder=1.25/ScaleHighlight=1.9`，定义在 `:207`） |
| `Player Profile Window ▸ … ▸ Avatar Item Small_Ref`（Avatar 页的格子） | ✅ | `Shell/AvatarTab.cs:429/444/448` |
| `Profile Tab / Ranking Tab` 两页的头像格 | ✅ | `Shell/ProfileTab.cs:622-629` · `Shell/RankedTab.cs:285-292` |
| 战斗名牌 `EnemyInfo/…/Avatar Item Small` | ✅ | `Battle/BattleDriver.cs:29990`（Border ×1.25）+ `HudAvatarSize`（`:29949` 那一段，Image ×2 × 父链 0.8）；判据在 `:29725-29745` |
| **战斗 `Alliance Panel ▸ Avatar Item Small ▸ Image`** | **❌** | `Shell/AlliancePanelWindow.cs:284`（`AvatarImageR` = **未缩放**布局框 `10.0,63.6→180.0,182.8`）+ `:1114` 用它建 quad |

`AlliancePanelWindow` 的 `Border` 那一格是 **✅**：`:276` 的 `AvatarBorderR = (−11.283,60.573)-(201.246,209.590)` = **212.5×149.0**，
正是 arena1 那份容器 `170.02×119.21` 的 **×1.25**（实算吻合）。
⚠️ 但同一处的注释（`:278-284`）把 `Image` 的 ×2 说成「`EverguildButtonMaterialModifier` 的**运行时**视觉缩放」——
**这句不成立，两条证据**：
① ×2 是 `m_LocalScale` **序列化在 prefab 里**的（`RectTransform` 字段直读，9/9 实例同值，不需要运行期）；
② 那三颗节点**确实都挂着** `EverguildButtonMaterialModifier`（`Highlight`/`Border`/`Image` 各一），
   但该类**全文不碰缩放** —— `d:/2/tools/decomp_full/EverguildButtonMaterialModifier__*.c` 里
   `localScale` / `m_LocalScale` / `set_localScale` **0 命中**（它只有 `GetModifiedMaterial` / `ChangeCanvasColor` /
   `ToogleGreyScale` / `SetMaterialDirty` + 一个 ctor）⇒ **它不产生任何尺寸变化**。
   ⚠️ 同名 `.c` 里有一份**文件名与内容不符**（`EverguildButtonMaterialModifier__.ctor.c` 的内容是
   `OnClickDestroy___ctor`）—— 那一条不构成反证（见 CLAUDE.md §三「文件名与内容不符」那一族）。
⇒ 同一处「Border 按 ×1.25、Image 不按 ×2」**不是口径差异，是漏了**。

### 2·2 `Deck Editing Menu ▸ Sidebar/Footer/Done/Done Highlight` —— **旋转 90° 丢了**

| 节点 | 原版值 | 我们的落点 | 判档 |
|---|---|---|---|
| `Done Highlight` | `m_SizeDelta = 81.36×223.22` · **`m_LocalRotation.z = 90.000°`** · pivot(0.5,0.5) ⇒ **视觉 223.22×81.36（横条）**；子件 `Done Enabled` 带 **−90°** 把内容转正 | `Deck/DeckRuntime.cs:697` `DoneHlW=81.4, DoneHlH=223.2`（`:2477` 建 quad）—— **画成了竖条 81.4×223.2** | **❌** |
| 同上 `Image.m_Color` | `(1, 0.5672, 0.1098, 1)`（橙金） | `:2477` 走 `FX_Square_UI_SDF`，未见同值 tint（`QDoneHl=3001` 那一层） | ？ |

- 中心是对的（我们的中心 `(107.9, 1045.1)` vs 原版 `Done` 中心 `(107.25, 1045.6)`）⇒ **只差 w/h 互换**。
- 改法：`DoneHlW = 223.2f, DoneHlH = 81.4f`，并保持中心不动 ⇒ `DoneHlX = 107.9 − 111.6 = −3.7f`、`DoneHlY = 1045.1 − 40.7 = 1004.4f`。
  （本工程惯例是「倍率/旋转乘进矩形」，先例 `AvatarTab.ScaleAboutCenter`；⛔ 不要改成 `localEulerAngles`。）
- ⚠️ `:946` 那句注释「原版它**纵跨**到卡组列表区」在 90° 旋转下**不成立**（纵跨 223 的是**旋转前**的字段），一并订正。

### 2·3 `Deck Editing Menu ▸ Content Area/Header/Filler` —— 我们没建

| 节点 | 原版值 | 我们的落点 | 判档 |
|---|---|---|---|
| `Filler` | `443.256×85`（= 页头全高）· `Image` **无 sprite** + `m_Color (0,0,0,1)` · 另挂 **`Gradient2`**：`_gradientType=0` · `_modifyVertices=1` · `_blendMode=2` · 颜色键 2 条（黑→黑）· **α 键 3 条**（t=0 α1 → t=0.8646 α1 → t=1 α0） ⇒ **「黑带、末尾 13% 渐隐」** | `Deck/DeckRuntime.cs` 全文 **`Filler` 0 命中**（`grep` 现读）⇒ 页头这段黑渐变带**没建** | ？ |

判 ？ 而不是 ❌：**是否可见取决于它压着谁**（页头里它是 HLG 的弹性占位）。要不要补，先要在真机上量「页头那一段现在偏亮还是本来就被别的东西盖住」——
本代理**不跑 Unity**，不下这个结论。判据（值 + 结构）已经齐了，照上表复刻即可。

### 2·4 已对上（✅）—— 列出来是为了让下一波**不要重查**

| prefab | 节点 / 参数 | 原版值 | 我们的落点 |
|---|---|---|---|
| `Alliance Member Entry` | `background` / `background/Image` / `connection status` 色 | `(1,.3596,0,1)` / `(.4811,.1822,0,1)` / `(0,1,.0736,1)` | `Shell/AllianceMemberTab.cs:1887-1890`、`:1914`（另有 `IsPlayer` 覆盖色 `:1886`） |
| `PlayerRankingRow` / `AllianceRankingRow (Skulls) Variant` | `Background` / `BackgroundHighlight` 色 α0.1647 | `(.8302,.1919,.4280,.1647)` / `(.9779,1,0,.1647)` | `Shell/LeaderboardRow.cs:113-116`（`BgNormalTint` / `BgHighlightTint`） |
| `PlayerRankingRow…` | `Name Holder/Guild Name`、`Ratings/*/Secondary Icon` `IsActive=0` | 出厂关 | **不是缺陷**：`UIRankingRow.Config` 无条件写三颗字后 `ToggleComponents(true)`（`E9` 已核）；`Secondary Icon` 原版永不点亮 ⇒ 不建（`Shell/AllianceMemberTab.cs:1651`、`Editor/MainMenuScene.cs:18510-18513`） |
| `Chat` / `ChatPanel` | `CloseBackground` α.5176 · `ChatBackground` α.8667 · `Orange Tab Toggle/button_bg` `(1,.4275,0)` · `Player Options Panel` `(.3113,.2012,.2012)` | 同左 | `Shell/ChatPanel.cs:79`、`:225`、`:456`、`:339` |
| `Chat/Tab Buttons/Orange Tab Toggle` `IsActive=0` | 出厂关 | **不是缺陷**：`Shell/ChatPanel.cs:16` 记明它是**运行期模板**（`tabButtonPrefab` 自己） |
| `CardUI` / `Card Drawer` / `Card Alternate Art Drawer` | `Textbackgrounds/TextBackground {Big,Small} UI` 色 `(0,0,0,.6471)`、容器 `IsActive=0` | 同左 | `Core/CardView.cs:4476`（`TextBgAlpha`）、`:8111`、`:5337`（态 3/4/7/8 不建） |
| 同上 | 所有 `*Text*` 节点 **`m_LocalScale = (0.01,0.01,0.01)`**（**738 行**，全库最大一类） | 原版用「缩到 1%」当**隐藏**手法 | **等价**：我们用 `Show()/alpha`（`Core/CardView.cs:2386`）⇒ 视觉等价，⛔ 不按 0.01 复刻（那会在画面上留 1% 的斑点） |
| 同上 | `Cost Container Big` / `Cardback Container` / `Created By Text` / `Card Ready for level up` `IsActive=0` | 出厂关 | `Core/CardView.cs:4765`（节点序表）、BattleDriver `CreatedByText` 显隐链 `:9841-9849` |
| 同上 | `New Card Badge` `IsActive=0` + `Image.m_Color=(.5472,.0876,.0542,1)` | 同左 | `Core/CardView.cs:15276-15310`；`Editor/ShopScene.cs:2988`（一进来就 `SetActive(false)`） |
| `Deck Editing Menu` | `Deck Information cost drawer` **×1.48** + `IsActive=0` · 底板色 `(.1226×3,1)` | 同左 | `Deck/DeckRuntime.cs:542-555`（`CurveScale=1.48`）、`:633`、`:618` |
| `Deck info Popup` / `Practice Mode Menu` / Draft 两窗 | 同一颗 `Deck Information cost drawer` 的 **1.8 / 1.2 / 1.1326** | 三套不同值（**铁律 5·c 的活标本**） | `Core/CostCurveDrawer.cs:5-6`（1.8 / 1.2）；`Shell/DraftModeWindow.cs:649-662`（1.1326 **已烘进矩形**） |
| `Deck Editing Menu` | `Window Options/Deck Name/Image` `(0.0627,0,0,1)` · `WIldcard Counter/Background` `(0.4623×3,1)` · 三颗 `Highlight` 红 | 同左 | `Deck/DeckRuntime.cs:1907-1939`（含 `InputFieldBackground` 同值）、`:1696`、`:1860-1900`（`RefreshHeader` 给红 tint） |
| `Base Game Mode Container 1x1` | `TextDarkening` α.6117647 · `Border` 灰 .33962 · `Clock Icon` `(.7373×3)` | 同左 | `Shell/MainMenuRuntime.cs:2912`、`:3013`、`:3030` |
| `Missions Tab` | `Daily/Special Missions` **×1.15** | 同左 | `Shell/MissionsTab.cs:23`、`:104-127`（整条 VLG 换算链都在） |
| `Rewards Base Submenu Variant ▸ Forge Tab` | `Column Right` **(−1.04,1.04,1.04)** · `Candle` 0.9615 · `Column Left` 1.04 | 同左 | `Shell/ForgeTab.cs:187` |
| `Booster Pack Open Window` | `Booster Animation Parent` **×0.87626** | 同左 | `Editor/ShopScene.cs:2834`、`:2902-2903` |
| `Alliance Event Score Info` | `Progress Bar` / `Chest` / `Skull` **×0.869565**（= 1/1.15） | 同左 | `Shell/EnergySinglePlayerOnlyEventWindow.cs:157`（`SkullFontScale`） |
| `Alliance Event Score Panel` | **×0.95581** | 同左 | `Editor/DraftAssertD3.cs:1369-1371` |
| `Campaign Tab` | `Point Icon` **×2** + 子件 ×1.2 · `Campaign Glow` ×1.5 | 同左 | `Shell/CampaignTab.cs:397`、`Editor/RewardsScene.cs:2472` |
| `Rewards Base Submenu Variant ▸ Campaign Tab` | `Tutorial Message` `(0.5,1,1)` + 子件 `(2,1,1)` | 造出来也只有这一处 | **不建**：`ToggleChooseArmyText` 全库 0 调用者（`Editor/RewardsScene.cs:5736-5737`） |
| `Draft` 两窗 | `CardUI-Warlord` **×75.6675** | 同左 | `Shell/DraftModeWindow.cs:644`（视觉 192.48×254.89 已烘进 `DeckWarlordCardR`） |
| 同上 | `Cardback` z=+1.60° / `Cardback Front` z=−3.43° | 同左 | `Shell/DraftModeWindow.cs:641-643` + `:3079` |
| `Generic Multi Card Display` | `CardUI Reference` **×223.14** | 同左 | `Battle/MultiCardDisplay.cs:61`、`:271`；`Shell/DraftModeWindow.cs` `PrevCardW/H` |
| `Social Submenu Variant` | `Cancel button/Image` `(−1,1,1)` + **rotZ 180°** | 镜像钮 | 未见对应（`Cancel` 是通用件） | ？ |

### 2·5 判不准（？）—— 明确写清「查到哪、差什么」

| 项 | 现状 |
|---|---|
| `Deck Editing Menu/Footer/Done/Done Highlight` 的 `Image.m_Color (1,.5672,.1098)` | 结构已知（§2·2），但 `DeckRuntime` 那一路是 `FX_Square_UI_SDF` 的材质色 / `UIEffectTogglerImage` 淡入淡出两段合起来的，**静态读不出最终 tint** ⇒ 要真机看，或读 `:8910` 那一段 `UIEffect` 逐字段表 |
| `Player Profile Window ▸ Profile Tab/Ranking/*/Content/DivisionImage` **×1.25 / ×1.2（z=1.08）** | 我们**画了段位图**，但尺寸用 `cardW × DivArtSideRatio`（`Shell/ProfileTab.cs:1048`，注释写明「实拍反算、**我们挑的**」）⇒ 与 prefab 的「布局框 ×1.25」**不是同一路**。判据齐（`m_LocalScale` + `Content` 的 VLG），但**要动就得连 VLG 槽高一起算**，本代理不跑 Unity、不核版面 ⇒ 记 ？+施工线索 |
| `Player Profile Window ▸ MainRating/Mission Milestones Progress/steps/RankedSealStep` **×1.3**（5 颗） | 模式卡那一排印玺我们已建（`Shell/MainMenuRuntime.cs:BuildRankedSealRow`，StepW=100 / pitch 103.2），但那是**另一个宿主**（`Base Game Mode Container` 那一族里 `RankedSealStep` 是 ×1）。Profile 页那一排是否建档 / 是否 ×1.3 **未核** |
| `Player Profile Window ▸ …/FactionScoreSmall*/Alliance Rating Display` **×0.5037**（8 颗） | `Shell/FactionScore.cs` 的口径是「出厂矩形逐实例覆盖」（`SmallSize` / `BigSize` / 宿主 `Bind`），**倍率没在代码里出现**；是否已烘进 `SmallSize` **未逐数核**（`B94_B17` 报告 §五有那张表，本代理没读） |
| `Avatar Item Small ▸ Highlight` 的 `m_Enabled` | `AllianceMemberTab` 那一格我们**恒建**；原版 `Alliance Panel` 那份 `Highlight` 是 `m_Enabled = 0`（`Shell/AlliancePanelWindow.cs:660` 有记）。成员行那一份的 `m_Enabled` **本代理没单独核**（`deltas()` 只收四个类，不读 `m_Enabled`） |
| `Card Drawer` / `Card Alternate Art Drawer` 的**每实例装饰旋转**（z = −1.98 ~ 9.94°，19 个实例各不相同；`Booster pack stars` 还带 **y=180°** 的镜像） | 这批值是**摆位**（每个实例手工转一点），不是组件参数。哪些宿主（商店/Draft/奖励）该复刻**未逐个核**——`Editor/ShopScene.cs` 只对 `Booster Animation Parent` 那颗有记录 |
| `Player Profile Window ▸ …/Edit Name Button` **scale (0,0,0)** | 原版用「缩到 0」当隐藏；我们是否也关着**未核**（`deltas()` 收了，但没找到对应件名） |
| `Card Drawer` / `CardUI` 根 **scale (0,0,0) / 151.3 / 150**（188 + 3 行） | 这是**模板**（`Instantiate` 的源），我们的卡面由 `CardView` 自己那套管；**不需要复刻**，但本代理没有逐条证明每一处都是模板 |

---

## §三 「❌ 缺失」施工清单（按文件分组）

### 3·1 `Shell/AllianceMemberTab.cs` —— 成员行头像三层（3 条）

落点 `:1906-1912`。**布局矩形一个像素都不动**，只把三层各自绕**自己布局框的中心**放大
（本工程唯一实现 = `Shell/AvatarTab.ScaleAboutCenter`，`Shell/AvatarTab.cs:214`；倍率常量 `Shell/AvatarTab.cs:207`）：

| 现（`:1907` / `:1910` / `:1911`） | 改成 |
|---|---|
| `Highlight` = `(av.x1, av.y1−3.53, av.x2+3.34, av.y2−36.37)` | `ScaleAboutCenter(该矩形, 1.9f)` |
| `Image` = `(av.x1, av.y1−2.70, av.x2, av.y1+62.99)` | `ScaleAboutCenter(该矩形, 2.0f)` |
| `Border` = `(av.x1, av.y1+6.57, av.x2, av.y1+72.26)` | `ScaleAboutCenter(该矩形, 1.25f)` |

- **画序**必须保持 `Highlight(2) → Border → Image`（立绘压框）——`Player_Profile_Border` 中心是不透明黑（`AvatarTab.cs` 那段 `W18` 注释），放大后两者重叠更多，画反了会整块黑。
- 断言可**照抄**已有那份：`Editor/AvatarScaleAssertE18.cs`（它已经量「可见内容宽/框宽 = 1.006±0.02」「顶沿戳出框顶」），把宿主从 Profile 换成 Alliance 成员行即可。
- ⚠️ `E18` 留的那条账（放大后相邻格子的实心内容会重叠、uGUI 按兄弟序画而我们靠到相机距离）**在这一处同样成立**，一并转给负责队列档的人。

### 3·2 `Shell/AlliancePanelWindow.cs` —— 战斗侧联盟面板的立绘（1 条）

`:1114` 建的 `Image` 用的是 `AvatarImageR`（`:284`，**未缩放**布局框 `10.0,63.6→180.0,182.8`）。
按 prefab：`Image` 视觉 = 布局 × **2.0** 绕中心 ⇒ 该实例（容器 `170.02×119.21`、容器中心 `(95.01,123.2)`、`Image` 上移 2.7）
实算 = **340.05×238.43** ⇒ `x[−75.01,265.01] y[1.29,239.72]` —— 这与注释里自己引的 dump「视觉框=×2 → 340.05×238.43」**逐数吻合**。
⇒ 改：把 `AvatarImageR` 换成 `ScaleAboutCenter(布局框, 2.0f)`（或直接写上面那组实算值），
并**删掉** `:278-284` 里「那是 `EverguildButtonMaterialModifier` 的运行时缩放」那句（错因：把 prefab 序列化字段当成了运行期行为）。
⚠️ 改完立绘会**戳出**头像框（原版就是这样，与用户 2026-10-24 说的「头像应该更大、甚至超出框」一致）；
队列 `QAvatar+1 > QAvatar` 已经保证立绘压框（`:1114` vs `:666`）⇒ 画序不用动。

### 3·3 `Deck/DeckRuntime.cs` —— Done 外发光的 90°（1 条）

`：697` 的 `DoneHlW/DoneHlH` 是**旋转前**的字段值。原版 `m_LocalRotation.z = 90°` ⇒ 视觉 w/h 互换。
改：`DoneHlW = 223.2f; DoneHlH = 81.4f; DoneHlX = −3.7f; DoneHlY = 1004.4f;`（保中心 `(107.9,1045.1)` 不动）。
顺手订正 `:946` 那条「纵跨」注释。

### 3·4 （候选，需真机定档）`Deck/DeckRuntime.cs` —— 页头 `Filler` 黑渐变带（1 条）

若真机确认页头那一段偏亮 ⇒ 按 §2·3 建：`443.256×85 @ (73.8,0)` · 无图纯黑 · α 键 3 条（`1 → 1@0.8646 → 0`）。

---

## §四 覆盖边界（扫了哪些、哪些没扫、为什么）

**扫了**（全量机扫，不是抽样）：
- `bundle_menus_assets_all` **全部 313 棵根（308 棵走通）· 16,768 GameObject · 16,510 RectTransform** 的四类参数，共 **6,114 行**。
- 逐条到代码核对的是**能落到我们窗口**的那些（§二），重点八族：
  `Avatar Item Small` · `Alliance Member Entry` · 排行榜三行族 · `Chat/ChatPanel` · `CardUI/Card Drawer/Card Alternate Art Drawer` · `Deck Editing Menu` · `Base Game Mode Container` · `Player Profile Window`。

**没扫 / 只扫了清单**：
1. **其它 14 个 bundle**（`bundle_scenes_scenes_*` 13 个战场 + `bundle_generalsgamewindows` / `bundle_menusharedresources` / `bundle_mainmenualwaysloaded` …）。
   战场场景里的 **battle HUD** 参数的权威源是场景，不在 `bundle_menus` ⇒ 本报告**不含战斗 HUD 的场景级参数**（那属于战斗线/战场线的账）。
   ⚠️ 例外：`CardUI` / `Card Drawer` 这两族**在 menus 包里也有**（那是它们的 prefab 母本），已扫。
2. **`m_Enabled`**（组件级关断，例如 `Alliance Panel` 的 `Highlight`）—— 不在四类清单里，**只顺手记了两条**（§2·5）。它与 `m_IsActive=0` 同族且同样「方法级看不见」，**建议下一波并入普查**（判据现成：`MonoBehaviour.m_Enabled`）。
3. **`CanvasGroup.m_Alpha`** —— 脚本里收了（非 1 才报），但本包 **155 个 CanvasGroup 全是 α=1**（0 行命中）⇒ 无发现。
4. **`m_Material` / `m_Sprite` / `m_Type` / `m_PixelsPerUnitMultiplier`** 等——属另一类（§一 只做四类），不在本批。
5. **`.prefab` 之外的运行时赋值**（`m_Transition` 的 `m_SpriteState`、`UIEffectTogglerImage` 的目标色）——静态字段里读不到最终值，已在 §2·5 记「判不准」。

**可信度**：§二 里带「实算」的结论全部是「锚点 + pivot 推出布局框 → 乘 `m_LocalScale` → 与我们常量逐数比」，
差额给到小数点后两位；**没有一条是基于「数值在代码里搜不到」下的结论**（§1·4 那条反模式）。

---

## §五 没查清

1. §2·5 那 7 项（Done Highlight 的最终 tint · Profile 的 `DivisionImage` ×1.25/×1.2 · `RankedSealStep` ×1.3 · `Alliance Rating Display` ×0.5037 · 成员行 `Highlight` 的 `m_Enabled` · 每实例装饰旋转 · `Edit Name Button` scale 0）。
2. **`Avatar Item Small` 的 `Highlight` 与 `Border` 谁的 `m_Enabled` 是 0**：prefab 里 `Alliance Panel` 那份记 `m_Enabled=0`（`Shell/AlliancePanelWindow.cs:660`），
   但**成员行那份是否也关着**没有核 ⇒ 若也关着，§3·1 的 `Highlight` 那一条应当降级成「先确认是不是该建」。
3. **放大后的队列档冲突**（E18 留的那条账）：本报告只指出「成员行也要放大」，**没有**评估相邻行重叠时谁压谁 —— 那是队列方案的活。
4. 313 棵根里 **5 棵走不通**（无 transform 组件 / 解析异常），未逐棵查明是哪 5 个。
