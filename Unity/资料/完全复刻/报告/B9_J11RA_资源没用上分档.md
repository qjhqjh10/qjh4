# B9 · 资源账「没用上」17,692 个对象 —— 五档分档（只读取证 · J11RA · 2026-10-26）

> **输入** = `资料/完全复刻/资源/R0_资源覆盖账.tsv`（247,612 行 · 91 个序列化文件）
> **「没用上」的定义（本报告沿用账的口径）** = `处置` 列 = **`要做：原版可达却没用上`（17,591）** + **`疑似死资源：原版不可达，先核再定`（101）** = **17,692**。
> 它同时等于 `我们有没有用` ∈ {从没看过 10,866 · 读过（文档）5,603 · 读过（索引）1,223}。
> **产出**：① 逐条分档表 `资料/完全复刻/判据/B9_资源没用上_分档.tsv`（17,692 行 · 7 列：bundle / 类型 / 对象名 / 档 / 理由 / 证据 / 建议）
> ② 本报告。⛔ 本代理**未改**任何 `.cs` / 资产 / 主账表 / 正本，未跑 Unity，未动 git。

---

## §〇 一句话结论

**这 17,692 个「没用上」里，真正「原版用得到而我们没有」的只有 225 个（1.3%）、而且其中 173 个是「类名没进账」的待判清单；
78.4%（13,877）是【账的匹配口径看不见它们】，不是我们没用。**
根因是账的「我们有没有用」判据只有三档 —— ① 名字 == `MyGame/Assets` 里某个**文件名**（U1）② 名字以整串出现在我们的**数据表**里（U2）③ 只出现在**资料文档**里（U3）。
这套判据对下面三类东西**结构上不可能判对**：

| 结构性盲区 | 影响 |
|---|---|
| **① `.cs` 根本不在 U1 的扩展名白名单里** | 所有「我们用自己的代码实现、节点名/类名写成字符串字面量或标识符」的东西，全部落到「从没看过 / 只读过文档」。实测：**MonoScript 4,884 行全部只能靠 U2/U3 命中**，哪怕 `ChatPanel.cs` / `BattleHud.cs` / `DragAndDropItem.cs` 就在我们工程里 |
| **② 预制体树内节点不是文件** | GameObject 12,276 行（69.4%）永远不可能被 U1 命中；而「这个节点我们建了没有」要看它**所属的预制体有没有重建**，账里没有这一层信息 |
| **③ 切图 / 图集 / 加前缀的同源对象** | 例：卡框 `Orc Frame`（88 个同名不同 PathID）、图集容器 28 个、`trait_icon_uprising`（我们用的是 `Atlas_trait_icon_uprising`） |

## §〇·一 五档计数（逐条见 TSV）

| 类型 | 行数 | **A 缺口** | B 死资源 | **C 账要修** | D 不复刻对象 | E 判不了 |
|---|---:|---:|---:|---:|---:|---:|
| GameObject | 12,276 | 33 | 0 | **11,886** | 295 | 62 |
| MonoScript | 4,884 | 173 | 0 | **1,755** | 2,498 | 458 |
| MonoBehaviour | 157 | 16 | 0 | 86 | 51 | 4 |
| Sprite | 104 | 3 | 0 | 94 | 7 | 0 |
| Texture2D | 91 | 0 | 0 | 0 | 91 | 0 |
| SpriteAtlas | 28 | 0 | 0 | 28 | 0 | 0 |
| AssetBundle | 82 | 0 | 0 | 0 | 82 | 0 |
| ComputeShader | 24 | 0 | 0 | 0 | 24 | 0 |
| AudioMixer\*（3 类） | 26 | 0 | 0 | **26** | 0 | 0 |
| AudioClip | 7 | 0 | **7** | 0 | 0 | 0 |
| TextAsset / LightingSettings / Texture3D / Cubemap / SVC / VFXAsset / LightProbes / Material | 13 | 0 | 0 | 2 | 11 | 0 |
| **合计** | **17,692** | **225（1.3%）** | **7（0.04%）** | **13,877（78.4%）** | **3,059（17.3%）** | **524（3.0%）** |

**分档规则（可复核 · 逐条理由与证据都写在 TSV 里）**

- **A**：原版有引用（PPtr / AssetReference / SO 字段）或属于原版可达的树，而我们的工程与数据里**任何形式都找不到等价物**（名字、`Atlas_` 前缀、数据表、代码标识符四路都试过）。
- **B**：名字自带 `OLD` / `REJECTED` / `TBC` / `Backup` / `Extra`（原版自己废弃或未采用的一份），且没有活的根引用它。
- **C**：① 名字在我们 `.cs`/`.json`/`数据/` 里能找到（含归一化后含 index 后缀、`Atlas_` 前缀两种变体）② 或所属预制体/场景我们已重建（战场 13 场 · 主菜单 · 各窗）③ 或已有等价物（自建 AudioMixer + BattleMusic 三档 · 图集切片 · AnimFX 表驱动）。
- **D**：引擎内置 / URP / TMP / 第三方 SDK / 编辑器工具 / 平台层（PlayFab · Steam · Photon · IAP）/ bundle 头对象。
- **E**：四路都没命中、也判不出所属预制体；写清「还差什么」。

---

## §一 先抽样校准（219 条，按类型分层）

- **怎么抽**：`random.seed=20261026`，按类型分层（GameObject 120 · MonoScript 40 · MonoBehaviour 15 · Sprite 8 · Texture2D 8 · SpriteAtlas 5 · ComputeShader 4 · AudioMixer 4 · AudioClip 3 · AssetBundle 3 · 其余 1~2），共 **219 条**；每条都自动带出四路证据（`.cs` 归一化命中 / `.json`+`数据/` 命中 / `素材/`+`ui_extract/` 命中 / 小整数 PathID / OLD-REJECTED 词）。
- **抽样结果（按最终规则）**：**C 154 · D 51 · A 8 · E 3 · B 3**（C 70.3% · D 23.3% · A 3.7%）——与外推到的全量比例（C 78.4% · D 17.3% · A 1.3%）同向，A 略高是因为抽样里 Sprite 只取到 8 条、其中就含 3 条 tier 背景。
- **抽样里被规则判错、我人工改判的**（说明规则并非无脑匹配）：
  - `Splat`, `Crack`, `Plane Cracks`, `Tendril 4 impact below`, `Swipe`, `Sparks Outer`, `Jaw` —— 初判「没命中」→ 查 `数据/游戏数据/animfx_components.json` 与 `数据/历史_Godot快照/粒子/` 发现**全部有** ⇒ 改判 C（特效是表驱动，名字不在 .cs 而在数据表）。
  - `trait_icon_uprising` —— 初判缺口 → 发现工程里有 `Assets/WarpforgeVFX/Textures/Atlas_trait_icon_uprising_sprite.png` ⇒ C。
  - `BW_Trim1/2/3`, `Skull_jaw`, `Skulltop` —— 骷髅粒子图标，`Assets/*.cs` 有 ⇒ C。
  - `CursorsByArmySO`, `ForgeIconsByArmySO` —— 四路 0 命中，且原版确有 army cursors / forge icons 包 ⇒ **A**。
  - `Campaign Node Data` ×N —— 初判无命中 → `Shell/CampaignData.cs` 文件头写明「47 个 SO 逐字段抄进表」⇒ C。
  - `Premium Campaign GSC/SW/…` —— SO 存在、我们只抄到 UM ⇒ **A**（见 §三·4）。
  - `WF_Special offer_Bck_tier1/2/3` —— 四路 0 命中，但 `r0_edges` 显示被 4 个 Premium Item Offer SO 引用 ⇒ **A**（见 §三·3）。

---

## §二 逐条证据：三条口径缺陷（都能指着行号复核）

### 2.1 `.cs` 不在 U1 的扩展名白名单里 —— 单一规则造成最大一块误判

生成脚本 `r0_report.py`（路径见 `资源/R0_未使用清单.md` 头部：`D:/4/_tmp_view/wfres/`）第 163–170 行：

```python
ours_assets, _ = scan_tree(r'D:/4/Unity/MyGame/Assets',
    ('.png', '.asset', '.prefab', '.mat', '.shader', '.json', '.txt', '.unity',
     '.fbx', '.obj', '.ogg', '.wav', '.mp4', '.anim', '.controller', ...))
```

**没有 `.cs`。** 而 U1 的定义是「名字 == 某个文件的**文件名**」（`R0_未使用清单.md` §0.4）。
⇒ 只要一个原版对象的名字在我们这里**只以 C# 类名或字符串字面量存在**，U1/U2/U3 三档全部落空。

**这是同一族 bug 的第三次**：R0 文档自己记了两次订正 —— 扩展名表漏 `.ogg/.wav`（486 个 clip 误判）、漏 `.anim`（24 条战场 clip 误判，见 `资源/AnimationClip_24条对账.md`）。**`.cs` 是同一个位置上的第三处漏项，还没补。**

**实测影响**（本报告算出来的，可复跑）：
- `MonoScript` 4,884 行：只有 **1,623 行**能靠「该类已在代码账里」判 C，**186 行（107 个类名）**能靠「`MyGame/Assets` 里有同名文件」（如 `ChatPanel.cs`）；剩下 **约 3,100 行**只能落到 A/D/E —— 而它们**并不是资源问题**，是「这个类我们有没有等价行为」的问题。
- `GameObject` 9,655 行（占 GO 的 78.6%）：名字**直接命中我们 `.cs` 的归一化文本**（例：`Cards in deck` / `Card Cost` 在 `Core/CostCurveDrawer.cs`、`Deck/DeckRuntime.cs`；`Timer Text` 在 `Battle/Label.cs`；`Collected Badge` / `Premium Highlight` 在 `Shell/ItemDrawer.cs`）。

### 2.2 预制体树内节点不是文件 —— GO 这一大类必须换判据

- 12,276 个 GO 里有 **5,282 个**是 13 个战场场景的树内节点，**5,227 个的根是 `BattlePrefab`**（另有 40 个根 `Scenario`、10 个根 `Battle Arena Black Legion Baked 2`）。战场是 `ArenaBuilder` 按 manifest 重建的，**节点名本来就不该出现在文件系统里** ⇒ 这一整块（30% 的 17,692）在任何「名字==文件名」的判据下都必然判「没用上」。
- 真正有用的切法 = **按所属预制体根聚合**（本报告已做：`own_*.tsv`，用 `Transform/RectTransform.m_Father` 回溯到根）。结果：
  - `bundle_menus_assets_all` 308 个根 → **255 个根的类名命中我们 `.cs` + 285 个根命中我们的数据表 ⇒ 只有 4 个根两路都不中**（3 个是 `General Basic Offer Popup/Container Variant Premium_Booster_avatar_cardback` 变体、1 个 `GenericDropdown`）。
  - `bundle_battleprefabs_vfxandmisc` 292 个根 → 147 命中 `.cs`、**286 命中数据表** ⇒ 特效是 `animfx_components.json` / `effect_index.json` 表驱动，账看不见。
  ⇒ **「预制体级」的结论是：UI 窗与特效几乎全有等价物；账给出的「逐个节点没用上」没有信息量。**

### 2.3 同源对象的不同写法（图集切片 / `Atlas_` 前缀 / 同名多实例）

| 现象 | 例 | 处理 |
|---|---|---|
| 我们用的是**图集里的那一份**，账里报的是**另一份独立 Sprite** | `trait_icon_uprising`（`battleprefabs` 里有一份，`40ktraiticonatlas` 里也有一份 `Atlas_trait_icon_uprising`） | C（工程内 `Atlas_…_sprite.png` 在） |
| 原版**同包同名、PathID 不同** | `Orc Frame` 88 个（11 个卡包 × 8 档） | C（我们按档位自绘卡框 `GetClanFrame`） |
| 图集容器对象本身 | `Deck Selection Atlas <阵营>` ×12、`Missions Atlas`、`0_RewardsAtlas`… 共 28 | C（我们按 `slices/*.png` 用单图） |
| 音频靠**自建 Mixer** 而不是原版 Mixer | `Music` / `Voices` / `BattleGroupHigh/Medium/Low` / `BattleMusicHigh/Medium/Low` 26 个 | C（`Resources/Audio/Main Mixer.mixer` + `Battle/BattleMusic.cs` 的**三条强度分轨**） |

---

## §三 A 档（225 个）—— 按类型 + 最可能被玩家看到的前 10

### 3.1 按类型

| 类型 | 行数 | 是什么 |
|---|---:|---|
| GameObject | 33 | **主菜单环境光效 21** · **开场动画 & 首屏 UI 12** |
| Sprite | 3 | **`WF_Special offer_Bck_tier1/2/3`** |
| MonoBehaviour | 16 | **15 个 `Premium Campaign *`（13 阵营 + Store + UM）** · `CursorsByArmySO` · `ForgeIconsByArmySO` |
| MonoScript | 173 | **原版游戏类，账里没有以它为主体的行、我们 `.cs` 里也没有这个名字** —— 是**待逐类判**的清单，不是「确认缺失」（见 3.3） |

### 3.2 前 10（按「玩家能看见」排序，逐条给判据）

1. **开场动画 / 首屏（`Intro UI` 10 + `Boot` + `Intro Videoplayer`，level0）**
   原版在哪用：`level0` 的 `Boot → Intro UI`（`Loading text` / `Progress text` / `Safe area` / 四向 `Smooth background fade` / `FadeBackground` / `PopupHolder`）＋ `Intro Videoplayer` 播开场视频。
   我们没有会怎样：**进游戏直接到主菜单**，没有「logo→加载进度→开场片」这一段；项目目标里写的「开场动画 → 主界面」这一层是空的。
   证据：`own_level0.tsv` 根 = `Intro UI`/`Boot`/`Intro Videoplayer`；`find MyGame/Assets -iname *.mp4` = 0；`Assets` 里没有 boot 场景（只有 `Battle/CardBase/CollectionCheck/DeckEditor/MainMenu/Shell/SampleScene`）。
2. **主菜单环境装饰与光效（21 个节点，`scenes_mainmenuwarpforge` 的 `Game UI` 根下）**
   原版在哪用：主菜单壳上的 `Swirl`×4 · `Glow Explosion Up/R/Down` · `Side Border Left/Right` · `Ray Right` · `Sparks R/RL` · `EffectElement`×5 · `Cog` · `Craft` · `Level Up Effect Down` · `Animated Loading Image` — 菜单边框的流动光效/升级光效。
   我们没有会怎样：主菜单是**静帧**，边框不发光、不流动，升级也没有那一下。
   证据：`MainMenuRuntime.cs` / `MainMenuScene.cs` / `ShellScene.cs` 里 `ParticleSystem` 计数 **0**（对比 `ForgeTab.cs` / `RankedDivisionChangeWindow.cs` 等都有）。
   ⚠️ 同根下还有 **57 个非光效的 UI 节点**（`Player Name` / `Upper bar` / `Navigation Panel` / `Buttons Container` / `Resources Bar` / `Select Art Button Left/Right`…）—— 这些我判 **E**：同族节点有命中、只有它们没有，多半是我们命名不同，**要按窗逐条核**，不算缺口。
3. **`WF_Special offer_Bck_tier1/2/3`（Premium Item Offer 弹窗的按档背景）**
   原版在哪用：被 **4 个** SO 引用 —— `Expansion UM1` / `Expansion Orks1` / `Release Space Wolves` / `Release Emperors Children` 的 `Premium Item Offer`（`bundle_cosmeticsso`，`r0_edges.tsv` 16 条入边）。
   我们没有会怎样：礼包/扩展包弹窗缺按档（tier1/2/3）底图，只剩 `WF_Special_offer_Value` 那条徽章条。
   证据：工程内 `find -iname "*Special?offer*"` 只有 `WF_Special_offer_Value.png`（3 份副本），**没有 `_Bck_tier*`**。
4. **`Premium Campaign *` 15 个 SO（13 阵营 + Store + UM）**
   原版在哪用：战役页的 premium 轨道定义（`bundle_menus_assets_all`）。
   我们没有会怎样：`Shell/CampaignData.cs` 只把 **UM 一套** 抄进来（文件头自己写着），其余阵营的 premium 战役**没有数据**。
   ⚠️ 顺手订正一句文档：`CampaignData.cs` 头写的「本地只有 UM 这一套」指的是**节点表**；**premium 战役定义在本地有 15 份 SO**，两者别混。
5. **`CursorsByArmySO`（按阵营的鼠标指针表）**
   原版：战场/界面按阵营换鼠标（`bundle_armycursors`）。我们：`.cs`/`数据` **0 命中** ⇒ 所有阵营都是系统默认指针。
6. **`ForgeIconsByArmySO`（锻造厂按阵营图标表）**
   原版：锻造页按阵营换图标；我们 0 命中 ⇒ 锻造页图标不随阵营变。
7. **成就（`AchievementsList` + 同伴）** —— 原版主菜单成就列表/条目；我们 `.cs` 里只有 `AchievementsMenu` 字样（`MainMenuScene.cs`/`ShellScene.cs` 里出现），需核是否只是按钮名 ⇒ **待判**（P2 社交域）。
8. **金币商店页（`GoldShopTab`）** —— 商店的「金币」页签；我们 `GoldShop` 0 命中（有 `ShopScene`/`ItemShopTab` 同名类在账里，但那是 item 商店）⇒ 需核页签数是否缺一页。
9. **商店数据模型（`ShopItemDefinition` / `ShopItemCollection` / `SpecialShopItemCollection` / `CrateContainerItem` / `CurrencyItem` / `PriceAndRarity`）** —— 原版商店货架的 item 定义与价格/稀有度绑定；我们商店是自建数据（`资料/阶段二_商店_原版规格.md`），**字段覆盖面未逐条比对** ⇒ 待判。
10. **排位数据（`RankedV2Data` / `RankedV3Data` / `RankedEventData` / `RankedEventSaveGame` / `RankedSharedData`）** —— 原版排位档位/赛季/奖励数据；我们 `RankedDivision`/`RankedV2` 出现在 `.cs`（UI 侧），**数据侧未确认** ⇒ 待判。

> 另记 3 个「小但确定」的：`SceneTransitionList`（转场 clip 表，0 命中，需核 24 条 clip 是否覆盖）、`TraitsCombinationsList`（词条组合表，0 命中）、`Combo`（连击规则 SO，0 命中）。

### 3.3 A 档 MonoScript 173 行（166 个类名）—— 请当【待判清单】用，别当「确认缺失」

规则只说明三件事：① `Warpforge_code/Scripts/Assembly-CSharp/<名>.cs` 存在（是原版游戏程序集）② `资料/完全复刻/账/*.tsv` 里**没有以它为主体**的行 ③ 我们的 `.cs` 里没有这个名字。
**它不等于「行为没复刻」** —— 例：`VATGenerator/VATRecorder/VATSample`（VAT 顶点动画）我们有自己的 `Battle/WarpforgeVatDriver.cs`；`CardStack2D` 有 `Core/CardFan.cs`；`CardExpanding3D` 有 `Battle/Card3DAnimationController.cs`。
**精度自估**：我抽 15 个手核，4 个其实已在账里以「参数类型」出现或已实现（`CurrencyItem`/`CampaignNodeInfo`/`DailyStore`/`TraitCombination`）⇒ **精确率约 70–80%**，真实缺口规模应在 **120–140 个类**量级。
成色较硬的一批（建议优先逐类核）：`AchievementsList` · `GoldShopTab` · `CardbackFilterController` · `ShopItemDefinition/Collection` · `RankedV2Data/V3Data/RankedEventSaveGame` · `ToggleDifficultySetting` · `PickupItem/PickupItemSyncer/SpawnEffects` · `ScoreBySkullsFunction/ScoreByWinFunction` · `MilestoneReward/ChildEventData/EventAdmissionPriceData` · `WildcardConfig/Provider` · `TimedDraftData/UnrankedFastModeEvent/UnlimitedMatchesEventData` · `UIMobileTextSizeManager`。

---

## §四 没查清（E 524 行 · 3.0%）—— 还差什么

| 组 | 行数 | 差什么 |
|---|---:|---|
| MonoScript 不在 stub 树 | 458 | 类名在 `Warpforge_code/Scripts/**` 反查不到（嵌套/内部类或用 mangled 名），要靠 `dump.cs` 的**带命名空间全名**再核一遍才能判 D/A |
| 树内节点（GO） | 62 | **57 个**是主菜单 `Game UI` 根下的非光效 UI 节点（`Player Name` / `Upper bar` / `Navigation Panel` / `Text Background`×4 / `Selected highlight`×4 …，同族有命中、只有它们没有 ⇒ 多半命名不同）＋ 5 个 liveops 变体节点（`Transition Effect` / `GenericDropdown` / `avatarBG`×2 / `Toast`），要按窗逐条核 |
| 配置 SO | 4 | `1. Welcome To Warpforge`（首教程步）· `SceneTransitionList` · `TraitsCombinationsList` · `Combo` |

**明确没做的**：① 没有逐条核 166 个 A 类名的「行为是否另有等价物」；② 没有核 15 个 `Premium Campaign` SO 的**字段内容**（只确认对象存在且我们没抄）；③ 没核 `SceneTransitionList` / `TraitsCombinationsList` 的**内容差集**；④ 没把 `资料/` 文档正文（U3）那一档的 5,603 行重新按 `.cs` 归一化重扫（只按现成账判）。

---

## §五 改动清单（本代理产出 / 未动）

- 新增：`资料/完全复刻/判据/B9_资源没用上_分档.tsv`（17,692 行 · 5.2 MB）
- 新增：`资料/完全复刻/报告/B9_J11RA_资源没用上分档.md`（本文件）
- **未改**：任何 `.cs` / 资产 / 主账表 / 正本 / `.tsv` 主表；未跑 Unity；未动 git。
- 中间件（临时，可删）：`d:/4/_tmp_view/j11ra/` —— `own.py`（按 `m_Father` 回溯所属预制体根）· `own_*.tsv`（13 战场 + menus + battleprefabs + mainmenu + level0 + resources）· `match_csjson.py`/`cs_json_match.tsv`（归一化 `.cs`/`.json` 命中）· `ms_class.tsv`（类 → 程序集/是否在账）· `classify5.py`（最终五档规则）· `sample200.tsv`（219 条抽样）。

---

## §六 顺手发现（不在我的分档口径内，但会误导后续）

1. **「疑似死资源」101 行里，真死资源 = 0**：82 行是 **bundle 头对象**（`AssetBundle` 类型，BFS 永远不可达）、14 行是 URP 后处理 Volume 组件（`Bloom`/`Tonemapping`/… 运行时才挂）、5 行是第三方脚本。⇒ 账里「疑似死资源」这一档目前**没有识别能力**，别拿它当 B 档依据。
2. **全表「原版不可达」934 行也不是死资源证据**：457 Mesh + 171 Material + 84 AssetBundle + 56 MonoBehaviour + 50 Texture2D + 35 Shader + 26 AnimationClip + 25 AudioClip … ⇒ 这是**引用解析缺口**（Mesh/Material 的 PPtr 没接上），不是「原版没用」。R2 已另案处理 905 个 Mesh 的归属；**Material/Texture2D 那两百多个没有对应的复核轮**。
3. **`.cs` 扩展名漏项是第三例**（前两例 `.ogg/.wav`、`.anim` 已在 R0 文档里订正过）。补上后本报告里的 C 档绝大部分会自动归位，**建议 R0 的「没用上」这一列在补 `.cs` 后重算一次**，否则下一次审计还会在这 17,692 上再花一轮。
4. **`CampaignData.cs` 文件头「本地只有 UM 这一套」有歧义**：节点表确实只有 UM（47 个 `Campaign Node Data`），但 **premium 战役定义本地有 15 份 SO**（13 阵营 + Store + UM）。两条别互相引用。
5. **同名不同对象的坑又出现一次**：`trait_icon_uprising` 有 3 个对象（`battleprefabs` 独立 Sprite / `40ktraiticonatlas` 图集内 Sprite / `Warpforge Trait TextSprites` 里的引用）；**只按名字判会得出相反结论**（先判缺口、实际在用）。
6. **主菜单里 `Game UI` 根下的 21 个纯光效节点说明**：原版主菜单的「框」是**用粒子/流光拼的动画框**，我们主菜单是静态图。若验收时觉得「主菜单不够活」，根因在这里，不在贴图。
7. **`globalgamemanagers.assets` 里的 4,251 个 MonoScript 全是「构造性可达」**（内部文件的所有对象都被当根）⇒ 这一档的「原版可达」**没有信息量**，不能拿来当「原版确实用它」的判据。
