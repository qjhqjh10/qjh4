# H16 · 全仓 XML doc 转义清理（A442）（写手 · 2026-10-12）

> 一句话：**白名单范围（`CardPresentation/Shell/*.cs`，除 9 个黑名单件）** 的 XML doc 转义**已全部清干净** ——
> **33 个文件 / 174 条 CS1570 → 0**（运行时程序集，`/doc` 编译实证）。另修 3 个文件的**结构性**未闭合标签。
> 常规类型检查 **0 / 0**（带 `/doc` 与不带 `/doc` 各跑过）。
> ⚠️ **Shell/ 之外**（54 文件 / 492 条）、**9 个黑名单 Shell 件**（42 条）、**Editor 程序集**（15 文件 / 82 条）
> 都**不在本件白名单** ⇒ 全表在 §四，**只报不改**。

---

## 一、结论

| 范围 | 改前 CS1570 | 改后 | 结论 |
|---|---|---|---|
| **`CardPresentation/Shell/*.cs`（非黑名单，33 件）** | **174** | **0** | ✅ **全清**（本件的范围） |
| `Shell/*.cs` 黑名单 9 件（归别的写手） | 42 | 42 | ⛔ 不碰（§四·2） |
| `Shell/` 合计 | 216 | 42 | —— |
| 运行时程序集全量 | 708 | 534 | 差 = 174 ✅ |
| Editor 程序集 | 82 | 82 | ⛔ 白名单外（§四·3） |

**根因（与 H13 §八·1 同一族）**：XML doc 注释里**贴代码/路径片段，`<` 与 `&` 没转义**。
XML 解析器把它们当**标记开始**，于是一个裸 `<` 会把后面整段吞成元素名，
**连带产出「summary 需要结束标记」「结束标记 X 与开始标记 Y 不匹配」等一串诊断** ——
所以**一个根因 = 十几条警告**（最多的 `BoosterInfoPopup.cs:485` 一行产 **14 条**）。

---

## 二、改动清单（按「类」给，行号 = **本件改完那一刻现读**）

### 2·1 「裸字符 → 实体」共 **81 处**（60×`<`→`&lt;` · 21×`&`→`&amp;`），落在 30 个文件

| 文件 | 行 | 改了什么（原文 → 转义后） | 变了几处 |
|---|---|---|---|
| `BlinkGraphic.cs` | 152 | `` GetComponent<Graphic>() `` → `GetComponent&lt;Graphic>()` | 1 |
| `BoosterInfoPopup.cs` | 485 | `` `Tooltip < Booster pack guarantee Slider < Text < window < Booster Info Popup` `` → 四处 `&lt;` | 4 |
| `CollectionWindow.cs` | 143/267/473/675/2367/2553 | `1575 < 视口` · `CollectionDisplay<T>` · `CollectionFilterController<T>` · `Count <= 0`（×2）· `CollectionFilterController<T>` | 12 |
| `DailyData.cs` | 195/216/621/727/740/780/1004 | `<= *(int*)` · `&&`（×4）· `index < 它` · `INotificationProvider<MissionsBadge>` · `i < 它` · `&&`（×2）· `Array__Empty<T>()` | 11 |
| `DailyStreakPopup.cs` | 169 | `5 < 当前天` | 1 |
| `DeckInfoPopup.cs` | 395/398/412 | `1 < 卡组数` · `卡组数 < 上限` · `iVar1 < *(int *)` | 3 |
| `DeckSelectionPopup.cs` | 158 | `Action<CardDeck>` | 1 |
| `ForgeTab.cs` | 53 | `WindowTabBase<MainMenuRewardsWindow>` | 3 |
| `ItemDrawer.cs` | 1140 | `Count <= i` | 1 |
| `LeaderboardRow.cs` | 55 | `` `Avatar_<阵营>_<单位>` `` | 4 |
| `LeaderboardWindow.cs` | 390/392 | `List<CardArmy>` · `"<前缀>" + army` | 2 |
| `LiveOpsEventWindow.cs` | 610 | `RankedV3ArmyState & 4 /*Featured*/` | 1 |
| `MainMenuRuntime.cs` | 685/1003 | `BiDirectionalDictionary<ComponentReference<GameWindow>, GameWindow>` · `QAvatarFrame < QContent` | 2 |
| `MenuScroll.cs` | 201 | `` `dy<0` `` | 1 |
| `MissionRerollPopup.cs` | 421/422/443/445/448 | `GetOpenWindow<带 Missions 页的那扇窗>()` · `ChangeTab<MissionsTab>(win)` · `GetOpenWindow<带页签的窗>()` · `List<GameWindow>` · `GetOpenWindow<T>()` | 5 |
| `MissionsTab.cs` | 36/1209 | `WindowTabBase<MainMenuRewardsWindow>` · `Shader.Find(<那个串>)` | 3 |
| `OfferContainer.cs` | 716/812/982/1320 | `Assembly-CSharp/<类>.cs` · `PoolIndex < 0` · `FilledPool < 0` · `target=<外部链接>` | 8 |
| `PointerLayer.cs` | 771/1042 | `sqrMagnitude < dz²` · `dt<=0` | 3 |
| `PopUpGameWindow.cs` | 163 | `MenuDeck/Error/<1..5>` | 1 |
| `PracticeModePopup.cs` | 613 | `state < 2` · `state == 0 && isPlayerDeck` | 3 |
| `ProfileData.cs` | 77/231 | `` 去掉 `ACH<n> ` 前缀 `` · `army <= 0` | 6 |
| `ProfileTab.cs` | 89 | `L_Art(2) < L_Frame(3)` | 1 |
| `PromptPopup.cs` | 302/542/949 | `AddComponent<WindowButton>()` · `<常态图>_hover` · `state != 4 && !softDisabled` | 4 |
| `RankedTab.cs` | 82/483 | `L_Art(2) < L_Frame(3)` · `` `<Initialize>b__10_0` `` | 2 |
| `RewardWindow.cs` | 82/1339 | `IsPremiumLocked && 有高级档奖励` · `<>c__<Open>b__20_1` | 6 |
| `RewardsWindow.cs` | 69/266/269 | `WindowTabBase<MainMenuRewardsWindow>` · `INotificationProvider<MissionsBadge>` · `idx == 0 && DailyData.RewardsHasBadge` | 6 |
| `SettingsWindow.cs` | 1041/1861 | `852.26 < 视口底 954.00` · `!isMobilePlatform && … && …`（两对） | 5 |
| `ShellRuntime.cs` | 305 | `` `<原版名> outer` / `<原版名> inner` `` | 2 |
| `SocialData.cs` | 106 | `<= 0 就用原版 _DefaultItemSize` | 3 |
| `TrophyInfoPopup.cs` | 172 | `!progressBar.IsFilled && !Data.DontShowProgress` | 3 |

### 2·2 「结构性」修补 6 处（**残缺的块级标签**，不是字符问题）

| # | 文件:行 | 改了什么 | 判据 |
|---|---|---|---|
| 1 | `BattleLogTab.cs:54` | 末尾补 `</summary>` | `:45` 开的 `<summary>` 一路到 `:55` 的 `const float RowH…` 都没关 |
| 2 | `SkirmishEventWindow.cs:43` | `<summary>… 居中叠。 */` → `…居中叠。</summary>` | 写了 `*/` 但 XML doc 的闭合是 `</summary>`；且 `<summary>` 与 `>` 之间有空格 ⇒ 「此位置不允许使用空格」 |
| 3 | `SocialWindow.cs:349 / 360` | 各补一个 `</para>`（放在已有的 `…等价。</para>` / `…不自己拍。</para>` 之后） | 那一块 **`<para>` 开 10 / 关 8** ⇒ 缺 2 |
| 4 | `SocialWindow.cs:367-368` | 拆成两行（`…批次里。</para>` + `/// </summary>`） | 同一行挨着写 `</para></summary>` 会被当「结束标记 summary 与开始标记 para 不匹配」 |
| 5 | `CollectionWindow.cs:1654` | **删掉**多余的 `/// </summary>` | 那一块 `summary` **开 1 关 2** —— `:1647` 已经关过一次了 |
| 6 | `LeaderboardWindow.cs:399` · `RewardWindow.cs:1236` · `RewardWindow.cs:1276` · `ItemDrawer.cs:633` | 各补一个 `</summary>`（见下 §三·2） | 这 4 处 `<summary>` **开 1 关 0** |
| 7 | `ItemDrawer.cs:331` | `` （`</remarks>` 里那条…） `` → `` （`&lt;/remarks>` 里那条…） `` | 那是在**引用**一个闭合标签的字面量，不是真标记 |

---

## 三、怎么做的（可复现）

### 3·1 怎么把范围查全（**这一步是本件的地基**）

`工具/typecheck.sh` 抓不到 CS1570，两层原因（H13 §4·1 已查明）：**它只 `grep "error CS"`，且生成的 rsp 不传 `/doc:`**。
本件的做法是**复制一份 rsp 加 `-doc:`**（⛔ 没改 `工具/typecheck.sh`）：

```bash
# ① 用现成脚本生成 rsp，但落到一个【本件专属 TMPDIR】
cd /d/4/Unity/MyGame
TMPDIR=/tmp/wf_h16 D:/2/Warpforge_tools/py312/python.exe d:/4/Unity/工具/gen_csc_rsp.py
#    ⚠️ gen_csc_rsp.py 会把 TMPDIR 解析成 Windows 绝对路径 ⇒ -out: 那两行要改成 …/wf_h16/wfcheck/…
#       （且【不能用默认 TMPDIR】—— 会和别人正在跑的 typecheck 抢同一份 WFCheck.dll）

# ② 只加一行 -doc:，输出到另两个文件（不覆盖常规那份）
#    运行时：wf_csc_doc.rsp        编辑器：wf_csc_editor_doc.rsp（后者要 -r: 到刚编出的 WFCheck.dll）

# ③ 编译（两次都跑过；退出码 0）
dotnet "C:/Program Files/dotnet/sdk/8.0.425/Roslyn/bincore/csc.dll" @…/wf_csc_doc.rsp -utf8output
```

**范围（改前）**：运行时程序集 **708 条 CS1570 / 93 个文件**；其中 `Shell/` **216 条 / 39 文件**。
按派单白名单切：**174 条 / 33 文件 = 可改**，**42 条 / 9 文件 = 黑名单**。

**一条副作用诊断要说明**（**别误当成「我改坏了」**）：`/doc` 编译下的 CS1570 **条数 ≠ 缺陷处数**。
一个裸 `<` 会产出 **8~14 条**诊断（「应为标识符」/「此位置不允许使用空格」/「需要 `>` 来结束标记 X」/
「结束标记 summary 与开始标记 Y 不匹配」/「元素 summary 需要结束标记」…）。
**本条是官方文档就有的歧义**，我不改写；**本件一律以「根因处数」记账**。

### 3·2 逐处怎么定位到「根因那一行」

CS1570 的**行号会漂**（诊断常报在**后面 20 行**的 `</summary>` 上，见 H13 §4·2 那条）⇒ 逐处现读**整块**。
两个互补手段：

* **字符扫描**：只扫 `///` 行，找「`&` 后面不是合法实体」与「`<` 后面不是**已知 XML 标签名**」的字符。
  🔴 **「已知标签名」白名单是必须的** —— 我第一版没加，**把 `<summary>` / `</summary>` 自己给转义了**，
  干跑（`--apply` 前的 DRY-RUN）当场发现、修正后**转义数从 101 掉到 60**（差的 41 个全是标记）。
  ⇒ 别对 `///` 行做无差别 `sed s/<`。
  🔴 **扫描脚本的已知「假阳性」**（`<paramref>` **不在**我脚本的标签白名单里，所以它会把合法标签也报出来）：
  `CollectionData.cs:125` · `CollectionWindow.cs:433/1360/1938/1939/2014` · `DeckSelectionPopup.cs:243` ·
  `LiveOpsEventWindow.cs:243/260` · `MainMenuRuntime.cs:1170` —— **共 11 处，全是 `<paramref name="…"/>`，一律不用改**
  （编译器实证：这 11 行**产 0 条 CS1570**）。下一笔复用脚本时**把 `paramref` 补进白名单**。
* **块级标签平衡扫描**：每个连续 `///` 块里数 `<summary>/<remarks>/<para>` 的**开/关**，
  不等的就逐行读 —— **§2·2 那 6 处全是这么找出来的**（字符扫描一个都抓不到）。

改法：**逐文件、逐行**（python **二进制**读、只改 `///` 后的那段文本、**原样写回**），
⛔ 没用 `sed -i`、⛔ 没用一把梭重写整文件、⛔ 没动任何非 `///` 行。
⚠️ 顺带修的一处**不是转义**的：`SkirmishEventWindow.cs:43` 的 `<summary … />` 里 `<summary>` 后**多一个空格**。

### 3·3 行尾（python **二进制**数，改前 / 改后）

**33 个目标件 + 3 个结构件全部是纯 LF** —— 改后逐件复核 **CRLF 0 个**（脚本里还加了断言：一旦出现 `\r` 就停手）。
`Shell/*.cs` 这一族**本来就是 LF**（与 H13 记的 `AllianceMemberTab.cs 0/1406` 一致），
全程**只走 Edit 工具 + python 二进制写**，⛔ 没有 `sed -i`、⛔ 没有 python 文本模式 `open(...,'w')`。

---

## 四、CS1570 全表（**改后**，全仓 / 全程序集）

> 列 = 文件 · 条数 · 触发块起始行（同一诊断族的行号差距 ≤40 的并为一块）。
> 行号 = **改后那一刻**；本件之后若有人改这些文件，**行号会漂**（认代码锚点，别认行号）。

### 4·1 运行时程序集 · `Shell/` 之外 —— **54 文件 / 492 条**（**本件范围外**）

| 文件 | 条 | 块起始行 |
|---|---|---|
| `RuleEngine/Core/EffectText.cs` | 85 | 192~193, 558, 1290~1291, 2350~2351, 2421~2422, 2471~2472, 2557~2558, 2604~2605, 2825~2826, 3622~3623, 3773~3774, 3815~3845, 3948~3949, 4087~4088, 4181~4187, 4528~4529, 4711~4712, 4920~4921, 5160~5161, 5294~5318, 5401~5402, 5641~5642, 6042~6052, 6250~6265, 7333~7354, 7401~7402 |
| `CardPresentation/Battle/ScenarioBlendables.cs` | 60 | 244~245, 387~388, 448, 498~522, 589~591, 640~641, 692~693, 749~845, 908~919, 1155~1165, 1367~1368, 1756~1757, 1824~1825, 2173~2174 |
| `CardPresentation/Battle/BattleDriver.cs` | 46 | 864~888, 1160~1161, 2728~2741, 3606~3607, 5365~5366, 5546, 8580~8582, 8703~8712 |
| `CardPresentation/Deck/DeckRuntime.cs` | 30 | 181~210, 1021~1022, 1220, 2264~2272, 2442~2443, 2722~2723, 3481~3482, 4000~4008, 4158, 4207~4208 |
| `CardPresentation/Core/CardArt.cs` | 23 | 156~157, 322~359, 432~442, 600~601, 657~672, 713~714 |
| `CardPresentation/Core/CardView.cs` | 19 | 64~65, 742~745, 2342~2343, 2851~2859, 3265~3292, 4655~4656 |
| `CardPresentation/Core/CardIcons.cs` | 16 | 111~130 |
| `RuleEngine/Core/RuleCore.cs` | 13 | 2766, 3157~3158, 3259, 3436~3437 |
| `CardPresentation/Battle/AnimFXController.cs` | 12 | 147~165, 222~244 |
| `RuleEngine/Data/SimpleAI.cs` | 11 | 193~203, 989~992 |
| `RuleEngine/Core/Aura.cs` | 10 | 147~167 |
| `CardPresentation/Core/VoiceLines.cs` | 10 | 125~149, 203~204, 258~259 |
| `CardPresentation/Battle/EnvironmentApplier.cs` | 9 | 82~83, 497~498, 553~554, 691~692 |
| `CardPresentation/Battle/MulliganPanel.cs` | 9 | 64~73, 433~434 |
| `CardPresentation/Battle/Label.cs` | 9 | 155~159, 335, 683~684 |
| `CardPresentation/Core/CardText.cs` | 9 | 230~243, 401 |
| `RuleEngine/Core/EffectResolver.cs` | 7 | 3714, 4065, 5607~5608, 5827~5828 |
| `RuleEngine/Core/BattleContext.cs` | 7 | 48~49, 202~203, 646~647 |
| `CardPresentation/Battle/BattleLogPanel.cs` | 7 | 299~301, 685~691 |
| `CardPresentation/Core/Tooltip.cs` | 6 | 92~93, 428~431 |
| `CardPresentation/Core/CardFeel.cs` | 6 | 67, 143~144 |
| `RuleEngine/Core/CardDef.cs` | 6 | 386~387, 1218~1219, 2483 |
| `CardPresentation/Battle/CardDisplayWindow.cs` | 5 | 298~303 |
| `CardPresentation/Core/TmpFont.cs` | 5 | 179~188 |
| `CardPresentation/Core/Badges.cs` | 5 | 172~173, 299 |
| `CardPresentation/Core/EnvironmentConditions.cs` | 4 | 88~94 |
| `WarpforgeVFX/Runtime/ArenaOriginalMaterial.cs` | 4 | 337, 393 |
| `RuleEngine/Core/CreatePool.cs` | 4 | 260~268 |
| `CardPresentation/Core/RelatedCards.cs` | 4 | 30~31 |
| `WarpforgeArena1/Runtime/ArenaByArmy.cs` | 3 | 115~116 |
| `CardPresentation/Core/PlayerBoot.cs` | 3 | 184~185 |
| `CardPresentation/Battle/EndPanel.cs` | 3 | 98 |
| `RuleEngine/Core/WhenEvent.cs` | 3 | 293~294 |
| `CardPresentation/Core/VfxMap.cs` | 3 | 29~43 |
| `CardPresentation/Battle/RemnantSfx.cs` | 2 | 30~31 |
| `CardPresentation/Battle/AttackSelector.cs` | 2 | 226~227 |
| `RuleEngine/Core/GivePayload.cs` | 2 | 62~63 |
| `CardPresentation/Core/TraitParticles.cs` | 2 | 35~36 |
| `WarpforgeVFX/Runtime/WFModuleTween.cs` | 2 | 69~70 |
| `WarpforgeVFX/Runtime/WFModuleTransformModifier.cs` | 2 | 522 |
| `CardPresentation/Battle/ArenaRuntimeLoader.cs` | 2 | 22~23 |
| `RuleEngine/Core/PlayerState.cs` | 2 | 94 |
| `WarpforgeVFX/Runtime/WarpforgeEffectBinder.cs` | 2 | 31 |
| `WarpforgeVFX/Runtime/WFModuleChangeMaterial.cs` | 2 | 154~155 |
| `WarpforgeVFX/Runtime/WarpforgeShaderMap.cs` | 2 | 363~364 |
| `CardPresentation/Deck/DeckEditorState.cs` | 2 | 37 |
| `CardPresentation/Core/WarpforgeAudio.cs` | 2 | 22~23 |
| `CardPresentation/Battle/ChoosePanel.cs` | 2 | 99~100 |
| `CardPresentation/Battle/TargetReticle.cs` | 2 | 176 |
| `CardPresentation/Net/NetTransport.cs` | 2 | 269 |
| `CardPresentation/Battle/ReplayStore.cs` | 1 | 67 |
| `WarpforgeVFX/Runtime/ArenaParticleKeywords.cs` | 1 | 36 |
| `CardPresentation/Core/BattleAutoDrive.cs` | 1 | 493 |
| `RuleEngine/Core/UnitState.cs` | 1 | 90 |

### 4·2 `Shell/*.cs` 黑名单 9 件 —— **42 条**（⛔ 归别的写手，本件不碰）

| 文件 | 条 | 块起始行 |
|---|---|---|
| `Shell/AllianceMemberTab.cs` | 4 | 216~217, 578~582 |
| `Shell/CampaignRewardWindow.cs` | 2 | 227~228 |
| `Shell/CampaignTab.cs` | 2 | 39~40 |
| `Shell/MenuDraw.cs` | 15 | 367, 527~545, 586~587, 1926~1940, 2168~2181 |
| `Shell/ShopData.cs` | 8 | 41, 213~220 |
| `Shell/WindowsManager.cs` | 11 | 479, 596~644, 856~857 |

（`CardDetailPopup.cs` / `ViewportClip.cs` / `ShopWindow.cs` 改前改后**都是 0 条**，列出来只为对照白名单。）

### 4·3 Editor 程序集 —— **15 文件 / 82 条**（⛔ 本件白名单外）

| 文件 | 条 | 文件 | 条 |
|---|---|---|---|
| `WarpforgeArena1/Editor/ArenaBuilder.cs` | 24 | `WarpforgeArena1/Editor/EffectCompare.cs` | 3 |
| `CardPresentation/Editor/MainMenuScene.cs` | 10 | `CardPresentation/Editor/IconSizeProbe.cs` | 3 |
| `RuleEngine/Editor/RuleEngineTest.cs` | 9 | `CardPresentation/Editor/SettingsScene.cs` | 2 |
| `CardPresentation/Editor/BattleScene.cs` | 8 | `CardPresentation/Editor/PlayerBuild.cs` | 2 |
| `WarpforgeArena1/Editor/EffectSweepBatch.cs` | 4 | `CardPresentation/Editor/DeckScene.cs` | 2 |
| `WarpforgeArena1/Editor/EffectIso.cs` | 4 | `CardPresentation/Editor/CardFaceProbe.cs` | 2 |
| `CardPresentation/Editor/RewardsScene.cs` | 4 | `CardPresentation/Editor/AudioSetup.cs` | 2 |
| `WarpforgeArena1/Editor/MeshVertexColors.cs` | 3 | | |

### 4·4 已清表（✅ 33 件 / 174 条 → 0）

`BattleLogTab` · `BlinkGraphic` · `BoosterInfoPopup` · `CollectionWindow` · `DailyData` · `DailyStreakPopup` ·
`DeckInfoPopup` · `DeckSelectionPopup` · `ForgeTab` · `ItemDrawer` · `LeaderboardRow` · `LeaderboardWindow` ·
`LiveOpsEventWindow` · `MainMenuRuntime` · `MenuScroll` · `MissionRerollPopup` · `MissionsTab` · `OfferContainer` ·
`PointerLayer` · `PopUpGameWindow` · `PracticeModePopup` · `ProfileData` · `ProfileTab` · `PromptPopup` ·
`RankedTab` · `RewardWindow` · `RewardsWindow` · `SettingsWindow` · `ShellRuntime` · `SkirmishEventWindow` ·
`SocialData` · `SocialWindow` · `TrophyInfoPopup`（均 `Shell/` 下）

---

## 五、怎么验的（原文）

```text
# ① 改后 · 运行时程序集 `/doc` 编译（退出码 0）
dotnet …/csc.dll @C:/…/wf_h16/wf_csc_doc.rsp -utf8output   → /tmp/wf_h16/after2_runtime.txt
  error CS                    : 0
  warning CS1570 全量         : 708 -> 534      （差 174 = 本件的全部）
  warning CS1570 · Shell 白名单: 174 -> 0        ✅
  warning CS1570 · Shell 黑名单:  42 -> 42       ⛔ 不碰

# ② 改后 · 常规类型检查（**不带** /doc，= 工具/typecheck.sh 的原始跑法）
TMPDIR=/tmp/wf_h16 bash d:/4/Unity/工具/typecheck.sh
  运行时错误数: 0
  编辑器错误数: 0

# ③ 改后 · Editor 程序集 `/doc`
dotnet …/csc.dll @C:/…/wf_h16/wf_csc_editor_doc.rsp -utf8output
  error CS : 0     warning CS1570 : 82（与改前**逐文件同数** ⇒ 本件没碰 Editor/*.cs）

# ④ 自证「改动行全是注释行」
git diff -U0 -- Unity/MyGame/Assets/CardPresentation/Shell/ > …/mydiff.txt
  ⇒ 含 `&lt;`/`&amp;` 的新增行 **79 条**，其中**非 `///` 注释行 = 0**  ✅
  （⚠️ 工作区里另有**别的写手未提交的改动**：我把 `-` 行里含 `<`/`&` 的 128 行逐条核过，
  4 条非注释行**全部**是别人的代码改动 —— 最明显的一条 = `MissionsTab.cs` 的
  `if (lb != null && (autoMaxPx > 0f || autoBasePx > 0f))`，**HEAD 里不存在**，是别人写的。）

# ⑤ 行尾（python 二进制数）
33 个目标件 + 3 个结构件：改后 CRLF **0**（全 LF）✅

# ⑥ 块级标签平衡扫描（§3·2 那个脚本）
改前：SocialWindow 块 `<para>` 10 开 / 8 关 · CollectionWindow 块 `summary` 1 开 / 2 关 ·
      ItemDrawer 块 summary 1 开 / 0 关 · RewardWindow ×2 · LeaderboardWindow … 全部**报出来了**
改后：白名单内**零报**。
```

* ⛔ **没跑 Unity** · ⛔ 没动 git（只读了 `git diff` / `git show`）· ⛔ 没改两张正本 · ⛔ 没越白名单 ·
  ⛔ 没改 `工具/typecheck.sh` · ⛔ **本件零自检**（按类型；用户口径：A 表清完再跑）。

---

## 六、顺手发现（⛔ 只报不改）

1. 🔴 **「`/doc` 的 CS1570 条数」这一列不能当「缺陷处数」用** —— 一个根因产 8~14 条诊断（§3·1）。
   下一笔若照「条数」估工作量会**高估一个数量级**。本件一律**按根因块记账**。
2. 🔴 **`SocialWindow.cs:290-368` 那个块的 `<para>` 是【真嵌套了两层】**（开 10 / 关 8 ⇒ 补 2 个 `</para>` 才平衡）。
   ⚠️ 我是**按「补足到平衡」改的**（两个 `</para>` 各跟在一条已有的 `…。</para>` 之后）——
   **它未必是作者当年想要的那两层**；只保证 XML 合法 + 原文一字未动。
   要复原「作者本意的段落划分」得回头看那一坨的历史（`git log -L`）。**如实记账**。
3. 🟡 **`ItemDrawer.cs` 有两处「在正文里引用闭合标签字面量」**（`:331` 的 `</remarks>` 本件已转义；
   同文件 `:545-547` / `:601-607` 提到的 `</remarks>` 也属同族）—— 那一族的写法**只要引一次就可能踩**，
   建议以后正文里引用标签一律写成 `&lt;/remarks>`。
4. 🟡 **`SkirmishEventWindow.cs:43` 原来是 `<summary>… </summary>` 里写成了 `*/`** ——
   **看着像从块注释改过来的**（`/* … */` 改 XML doc 时只换了行首）。同族的「只换行首」改动值得全仓抽查一次。
5. ✅ **反证一条既有记账**：`H13 §八·1` 列的「已知起点」（`BoosterInfoPopup.cs:485` 13 条 ·
   `DailyData.cs:728/1004/195/216/621/740/780` · `DeckInfoPopup.cs:395/398/412`）**逐条核过、全部属实**
   （`BoosterInfoPopup.cs:485` 实测 **14 条**，H13 记 13 —— 差 1 条是因为它数的是那一刻的一版，
   **不影响结论**）。H13 说的「CS1570 在本工程是一大片」也**坐实**（运行时 93 文件 / 708 条）。

---

## 七、还欠什么

* ⛔ **`Shell/` 之外那 54 文件 / 492 条** 与 **Editor 程序集 15 文件 / 82 条**：**本件范围外，一条没动**。
  同一套判据、同一套脚本（§3·1 / §3·2）可直接复用；建议**按目录切块**另开（`RuleEngine/` 一批、
  `Battle/` 一批、`Core/` 一批），**每块一个写手**（铁律 13·3 第 1 条：一个文件只有一个写手）。
* ⛔ **9 个黑名单 Shell 件 42 条**：**归它们的写手**（本件不碰）。
* 📌 **建议给 `工具/typecheck.sh` 加一条【可选】的 `-doc:` 档**（H13 §八·1 也提过）——
  否则这一族**永远没有可观测点**，清完也会悄悄长回来。⛔ 本件**没改它**（不在白名单）。
