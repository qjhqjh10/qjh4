# `special` 稀有度 → 卡框档位（tier1–4）· 逐张裁定（2026-09-14）

> **任务**：悬案 #4 —— `special` → 卡框 tier 映射从没实测过，代码里暂按 `tier4`。逐张看 39 张卡图量出实际框档。
> **一句话结论**：**卡图上根本没有「帧档」这个信息** —— 同一阵营的卡面框在 common/rare/epic/legendary/special
> 之间**逐像素相同**（实测 `mean|d| = 0.0`）。卡面上 `special` 的唯一表达是**底部菱形宝石的橙红色**。
> 39 张的「tier1 / tier2」是**按阵营（＝印刷批次）分的常数**（8 阵营 t1 = 24 张，5 阵营 t2 = 15 张），
> **不含任何逐卡/逐稀有度的信息**。⇒ **没有一张 `special` 卡用 tier4**，我们现行的 `special = 4` 是错的。

> ⚠️ **本文件不是首次实测**。2026-09-13 已有两份产出，各覆盖 20 / 19 张，且用**两套独立方法**互验：
> - `资料/普查产出_0913/special_tier_实测_前半.md`
> - `资料/普查产出_0913/special_tier_实测_后半.md`
>
> 本文件**不重抄**它们的判据与数字表，只做三件事：① 独立定「尺子」并**证伪「4 档框 = 4 档稀有度」这个前提**；
> ② 给 39 张补两列**可复现的客观量**（宝石 hue / 卡图外接框），把它们的「tier」结论**归因**清楚；
> ③ 记下新发现的坑。第二节的 tier 列是**引用** 0913 两份文件（两法一致），不是本次重测。

---

## 一、尺子

### 1.1 ❌ 任务假设的尺子（4 档框 ↔ 4 档稀有度）**在卡图上不成立**

「挑 common/rare/epic/legendary 各几张，看 4 档框子长什么样」—— 这件事**做不出来，因为卡图上只有一种框**。

| 证据 | 做法 | 结果 |
|---|---|---|
| **A｜同组四档逐像素相同** | 取 Ultramarines `4计策` 里同一渲染组的 4 张：`Warpforge_31_Dutys-End`(common) · `Warpforge_30_Champions-of-Humanity`(rare) · `Warpforge_35_No-Mercy`(epic) · `Warpforge_37_Indomitus-Crusade`(legendary)，按 alpha 外接框裁齐后比**上/左/右三条边框带** | `mean|d| = 0.0 / 0.0 / 0.0`（四张两两之间） |
| **B｜含 special 的第二组** | `Warpforge_49_Sheltered-Location`(rare) · `Warpforge_52_Humanitys-Shield`(legendary) · `5防御卡/cover.png`(special) · `Warpforge_67_Hive-Factory`(special) · `firestrike.png`(special) | 同样 `mean|d| = 0.0`；**只有最下面那条带**（宝石）差 `3.4–5.1` |

⇒ **5 档稀有度里，框子一模一样；变的是底部宝石。** 所以「看卡图定帧档」这个动作本身没有尺子可用。

**为什么会这样（出处）**：`CardRarity{None, Common=1, Rare=2, Epic=3, Legendary=4, Special=5}` 与
`CardTier{Tier1=0 … Tier4=3}` 是**两个独立枚举**
（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardRarity.cs` / `CardTier.cs`）。
宝石资产名 `{1..5}_40k_cardframe_rarity_{common,rare,epic,legendary,special}` 里那个 `{1..5}` = **`CardRarity` 的值**；
而卡框资产名 `40k_Cardframe(s)_{troop|stratagem}_<阵营>_tier{1..4}` 里的是 **`CardTier`**。
`CardFramesSO.GetClanFrame(army, **cardTier**, CardTypeOptions)` 收的是 `CardTier`，
`CardServices.GetCardTier / CheckForCardUpgrade / CardTierConfig{requiredCopies,cost,pointReward}`
表明它来自**卡牌升级档**（用副本升档），**不是稀有度**。

**全包复核（本次）**：`d:/2/新解包资源/assets_full/` 全量扫 `*ardframe*` = **493 个资产**
（`Texture2D/` 209 · `GameObject/` 153 · `Sprite/` 130 · `MonoBehaviour/` 1）；
其中带 tier 后缀的 **`tier1/tier2/tier3/tier4` 各 82 个，`tier5` = 0** —— **不存在第 5 档卡框**。
（卡框资产名里的 13 章令牌 × 2 类型 × 含 `_SDF_` 变体。）
另有 5 档稀有度宝石，`{1..5}_40k_cardframe_rarity_{common,rare,epic,legendary,**special**}.json` **五档齐全**
（PNG 只导出了 `rare`，其余是运行时 hue-shift —— 见 `解包资源使用地图.md` :689）。

### 1.2 ✅ 卡图上真正的「稀有度尺子」= 底部菱形宝石（5 色，各配 2 张例卡）

位置：卡面**底部中央**的菱形座。判色法：采样宝石区取 hue（同 `_裁定_稀有度.md` 的方法）。

| 稀有度 | 宝石色 | hue 区间 | 例卡 1 | 例卡 2 |
|---|---|---|---|---|
| common | 浅蓝（去饱和钢蓝） | ≈186 | `Ultramarines/4计策/Warpforge_09_Tactical-Doctrine-2.png` | `Aeldari/4计策/Warpforge_11_Graceful-Avoidance.png` |
| rare | 绿 | ≈122 | `Ultramarines/4计策/Warpforge_30_Champions-of-Humanity.png` | `Aeldari/4计策/Warpforge_12_Whirling-Death.png` |
| epic | 紫 | ≈279 | `Ultramarines/4计策/Warpforge_11_Rapid-Deployment-2.png` | `Aeldari/4计策/Warpforge_10_Craftworld-Convergence.png` |
| legendary | 金 | ≈42 | `Ultramarines/4计策/Warpforge_37_Indomitus-Crusade.png` | `Ultramarines/4计策/Warpforge_52_Humanitys-Shield.png` |
| **special** | **橙红** | **≈15** | `Ultramarines/5防御卡/cover.png` | `Ultramarines/5防御卡/Warpforge_67_Hive-Factory.png` |

> ✅ **本次 39 张 special 实测 hue = 15.3 – 16.0（中位 15.8）**，与 `_裁定_稀有度.md` 的
> `special hue≈15` 一致 ⇒ 39 张的**稀有度判得没错**，错的只是帧档。
> 路径前缀 `d:/2/Warpforge部队卡片/`。

### 1.3 原版 4 档框（`CardTier`）本身长什么样 —— 这次量的是**素材**，不是卡图

参照物：`d:/4/Unity/素材/Warpforge原版/卡框/` —— **104 张 = 13 阵营 × {troop, stratagem} × tier1–4**，1024² RGBA。
以 Ultramarines `stratagem` 序列为例（跨阵营美术不同，但**复杂度阶梯一致**）：

| 档 | 视觉特征 | 例（文件） | 例（文件 2） |
|---|---|---|---|
| **tier1** | 最要素框：细银双线矩形，**无尖塔、无雕像、无金** | `40k_Cardframe_stratagem_Ultramarines_tier1.png` | `40k_Cardframe_stratagem_GSC_tier1.png`（令牌是 `GSC` 不是 `Genestealers`） |
| **tier2** | 加上窗框**顶角哥特尖塔**＋底角哥特尖拱线脚；仍无雕像、无金 | `40k_Cardframe_stratagem_Ultramarines_tier2.png` | `40k_Cardframe_stratagem_Orks_tier2.png` |
| **tier3** | 再加**两侧铠甲人像**；画窗顶变圆拱；仍无大面积金 | `40k_Cardframe_stratagem_Ultramarines_tier3.png` | `40k_Cardframe_stratagem_Sautekh_tier3.png` |
| **tier4** | 最繁复＋**大面积金/青铜**：顶部金色卷草、两侧金翼人像、画窗变**尖拱** | `40k_Cardframe_stratagem_Ultramarines_tier4.png` | `40k_Cardframe_stratagem_Sororitas_tier4.png` |

**框族**：13 章只有 13 套名字，且**两套前缀/命名并存**（照抄，别当成"两套不同的框"）：
`40k_Cardframe_stratagem_<X>_tierN`（X = `BlackLegion / GSC / Leviathan / Orks / SaimHann / Sautekh / Sororitas / Tau / Ultramarines`）
与 `40k_Cardframes_stratagem_<X>_tierN`（X = `Astra Militarum / DarkAngels / Emperor Children / Space Wolves`，troop 那张写 `Troop`）。
令牌映射：`GSC→Genestealers · Orks→Goff · Emperor Children→EmperorsChildren · Astra Militarum→AstraMilitarum · Tau→TauEmpire`。
**防御卡走 `strategem` 序列**（照 0913 两文件的判据）。

> ⚠️ **本次抽样没见过任何一张 PnP 卡用 tier3/tier4**（39 张 special 全落在 t1/t2；0913 两套方法也从未判出 t3/t4）。
> 所以 **tier3/tier4 在 PnP 成品卡上到底有没有出现过 = 仍未证实**。

---

## 二、实测表（39 张 special 防御卡）

**列说明**
- **判定 tier**：**引用** `普查产出_0913/special_tier_实测_前半.md` / `_后半.md`（两套独立方法 **19/19 一致**）。
- **宝石 hue** / **卡图外接框**：**本次独立实测**（复现法：`PIL` 取 `alpha≥250` 外接框；宝石区取底部中央
  `x=中心±45, y=底上6–80`，取饱和度最高 30% 像素的平均 hue）。
- **把握**：照抄 0913 的把握度（`高 / 中高 / 中 / 低`）。

**⚠️ 关键读数：外接框（第 6 列）在同一阵营内三张完全相同、跨阵营各不相同** ⇒ 该量是**阵营级**属性。

| 卡名 | 阵营 | 卡图文件名 | 判定 tier | 宝石 hue | 卡图外接框 | 把握 |
|---|---|---|---|---|---|---|
| Inspiring Legacy | SaimHann | `Aeldari/5防御卡/Warpforge_65_Inspiring-Legacy.png` | **tier1** | 15.4 | 639×968 | 高 |
| Webway Entrance | SaimHann | `Aeldari/5防御卡/Warpforge_66_Webway-Entrance.png` | **tier1** | 15.4 | 639×968 | 高 |
| Aspect Shrine | SaimHann | `Aeldari/5防御卡/Warpforge_67_Aspect-Shrine.png` | **tier1** | 15.4 | 639×968 | 高 |
| Defence Batteries | AstraMilitarum | `Astra Militarum/5防御卡/Warpforge_64_Defence-Batteries.png` | **tier2** | 15.8 | 748×1054 | 高 |
| Timely Reinforcements | AstraMilitarum | `Astra Militarum/5防御卡/Warpforge_65_Timely-Reinforcements.png` | **tier2** | 15.8 | 748×1054 | 中 |
| Munitorum Supplies | AstraMilitarum | `Astra Militarum/5防御卡/Warpforge_66_Munitorum-Supplies.png` | **tier2** | 15.8 | 748×1054 | 高（目视） |
| Hellfire Torch | BlackLegion | `Chaos/5防御卡/Warpforge_69_Hellfire-Torch.png` | **tier1** | 15.3 | 668×982 | 高 |
| Throne of the Heretic | BlackLegion | `Chaos/5防御卡/Warpforge_70_Throne-of-the-Heretic.png` | **tier1** | 15.3 | 668×982 | 高 |
| Hellfire Pit | BlackLegion | `Chaos/5防御卡/Warpforge_71_Hellfire-Pit.png` | **tier1** | 15.3 | 668×982 | 高 |
| Defensive Turrets | DarkAngels | `Dark Angels/5防御卡/Warpforge_64_Defensive-Turrets.png` | **tier2** | 15.8 | 596×929 | 高 |
| Grim Effigy | DarkAngels | `Dark Angels/5防御卡/Warpforge_65_Grim-Effigy.png` | **tier2** | 15.8 | 596×929 | 高 |
| Plasma Generator | DarkAngels | `Dark Angels/5防御卡/Warpforge_66_Plasma-Generator.png` | **tier2** | 15.8 | 596×929 | 高 |
| Armoury of Excess | EmperorsChildren | `Emperor_s Children/5防御卡/Warpforge_62_Armoury-of-Excess.png` | **tier1** | 15.9 | 604×930 | 中高 |
| Decadent Throne | EmperorsChildren | `Emperor_s Children/5防御卡/Warpforge_63_Decadent-Throne.png` | **tier1** | 15.9 | 604×930 | 中高 |
| Vial Tanks | EmperorsChildren | `Emperor_s Children/5防御卡/Warpforge_64_Vial-Tanks.png` | **tier1** | 15.9 | 604×930 | 中 |
| Alien Idol | Genestealers | `Genestealer Cult/5防御卡/Warpforge_66_Alien-Idol.png` | **tier2** | 16.0 | 724×1089 | 高 |
| Pilfered Supplies | Genestealers | `Genestealer Cult/5防御卡/Warpforge_67_Pilfered-Supplies.png` | **tier2** | 16.0 | 724×1089 | 中 |
| Rusted Vent | Genestealers | `Genestealer Cult/5防御卡/Warpforge_68_Rusted-Vent.png` | **tier2** | 16.0 | 724×1089 | 中 |
| Awakened Obelisk | Sautekh | `Necron/5防御卡/Warpforge_65_Awakened-Obelisk.png` | **tier1** | 15.4 | 649×979 | 中高 |
| Divination Menhir | Sautekh | `Necron/5防御卡/Warpforge_66_Divination-Menhir.png` | **tier1** | 15.4 | 649×979 | 中高 |
| Resurrection Vault | Sautekh | `Necron/5防御卡/Warpforge_67_Resurrection-Vault.png` | **tier1** | 15.4 | 649×979 | 高 |
| Extractor Rig | Goff | `Orks/5防御卡/Warpforge_60_Extractor-Rig.png` | **tier2** | 16.0 | 615×925 | 中高 |
| Gretchin Tower | Goff | `Orks/5防御卡/Warpforge_61_Gretchin-Tower.png` | **tier2** | 16.0 | 615×925 | 高 |
| Toxic Bonfire | Goff | `Orks/5防御卡/Warpforge_62_Toxic-Bonfire.png` | **tier2** | 16.0 | 615×925 | 高 |
| Guidance of the Saints | Sororitas | `Sorotitas/5防御卡/Warpforge_63_Guidance-of-the-Saints.png` | **tier2** | 16.0 | 731×1088 | 高 |
| Holy Fire | Sororitas | `Sorotitas/5防御卡/Warpforge_64_Holy-Fire.png` | **tier2** | 16.0 | 731×1088 | 中高 |
| Sacred Altar | Sororitas | `Sorotitas/5防御卡/Warpforge_65_Sacred-Altar.png` | **tier2** | 16.0 | 731×1088 | 中（目视） |
| Fenrisian Runestones | SpaceWolves | `Space Wolves/5防御卡/Warpforge_66_Fenrisian-Runestones.png` | **tier1** | 15.9 | 589×901 | 高 |
| Orbital Defences | SpaceWolves | `Space Wolves/5防御卡/Warpforge_67_Orbital-Defences.png` | **tier1** | 15.9 | 589×901 | 高（目视） |
| Halls of Legend | SpaceWolves | `Space Wolves/5防御卡/Warpforge_68_Halls-of-Legend.png` | **tier1** | 15.9 | 589×901 | 高 |
| Tidewall Droneport | TauEmpire | `Tau/5防御卡/Warpforge_67_Tidewall-Droneport.png` | **tier1** | 15.4 | 643×969 | 高 |
| Tidewall Gunrig | TauEmpire | `Tau/5防御卡/Warpforge_68_Tidewall-Gunrig.png` | **tier1** | 15.4 | 643×969 | 高 |
| Watch Tower | TauEmpire | `Tau/5防御卡/Warpforge_69_Watch-Tower.png` | **tier1** | 15.4 | 643×969 | 高（目视） |
| Digestion Pool | Leviathan | `Tyranid/5防御卡/Warpforge_64_Digestion-Pool.png` | **tier1** | 15.4 | 629×956 | 高 |
| Protective Bio-structure | Leviathan | `Tyranid/5防御卡/Warpforge_65_Protective-Bio-structure.png` | **tier1** | 15.4 | 629×956 | 高 |
| Toxic Vapours | Leviathan | `Tyranid/5防御卡/Warpforge_66_Toxic-Vapours.png` | **tier1** | 15.4 | 629×956 | 高 |
| Hive Factory | Ultramarines | `Ultramarines/5防御卡/Warpforge_67_Hive-Factory.png` | **tier1** | 15.4 | 635×982 | 中 |
| Light Cover | Ultramarines | `Ultramarines/5防御卡/cover.png` | **tier1** | 15.4 | 635×982 | **低** |
| Firestrike Turrets | Ultramarines | `Ultramarines/5防御卡/firestrike.png` | **tier1** | 15.4 | 635×982 | 中 |

**合计：tier1 = 24 · tier2 = 15 · tier3 = 0 · tier4 = 0（判不出 0 张）**

### 2.1 🔴 这张表的真正形状：**tier 是按阵营的常数，不是逐卡属性**

13 阵营 × 3 张，**每阵营三张的 tier 完全一致**：

| 判 tier2（5 阵营 = 15 张） | AstraMilitarum · DarkAngels · Genestealers(=GSC) · Goff(=Orks) · Sororitas |
| 判 tier1（8 阵营 = 24 张） | SaimHann · BlackLegion · EmperorsChildren · Sautekh · SpaceWolves · TauEmpire · Leviathan · Ultramarines |

⇒ **`special` 这一档内部没有 tier 差异可言**；差异落在**阵营**上。所以「`special` → 哪一档」这个问法**本身就是错的**：
不存在随稀有度变化的映射，也不存在随卡变化的映射。

**旁证（本次）**：卡图外接框也是**阵营级常数**（上表第 6 列，同阵营三张完全相同）；
0913 两文件也独立记到同一现象 —— Aeldari 同阵营内 `4计策` 10–12 号（一批）与 45–64 号（另一批）
**两组几何**，而两批里 common/rare/epic/legendary **混编**，且 `rare` 在两组都出现。
**「两组几何」的边界 = 渲染批次边界，不是稀有度边界**（本次用 Aeldari 复算：组内 `mean|d| 10–16`、跨组 `25–35`）。

---

## 三、结论与落地

1. **`special` 不该映射到 tier4** —— 39 张里一张 t4 都没有。
2. **但也不该改成固定 tier1** —— 15 张是 t2（按阵营分）。
3. ⇒ 正确形状是**按阵营/批次**给档，而不是 `rarity→tier` 查表。
   ✅ **2026-09-18 用户拍板：照 PnP 实测「按阵营」给档。已落地**——
   `MyGame/Assets/CardPresentation/Core/CardArt.cs` 的 `TierOf(rarity, faction)`
   （`Frame()` 把 `faction` 传下去）：判 **tier2 的 5 个阵营** = `AstraMilitarum` · `DarkAngels` ·
   `Genestealers` · `Goff` · `Sororitas`，**其余阵营 → tier1**。
   自检断言加在 `CardPresentation/Editor/BattleScene.cs`（`special+Sororitas→tier2` ·
   `special+Sautekh→tier1` · **`special ≠ legendary`**）。
   - 🔴 **`工具/import_original_art.py` 的 `RARITY_TIER` 是死代码，2026-09-18 已删。**
     本节原来写「同一张表」暗示「两处要一起改」—— **那条是错的**：它全仓**没有任何引用**，
     导出脚本对每个阵营**无条件导全部 4 档**，改它不改变任何产物。**生效路径只有 `CardArt.TierOf` 一处。**
   - 🔴 **`分层卡演示_0827/README.md` 的 `GameData.force_frame_tier4=true` 与本工程无关**（2026-09-18 实测）：
     那是 `d:/warpforge/`（**另一套 Godot 原型**）里 `autoload/game_data.gd:133` 的开关，
     Unity 工程 `.cs/.py/.json/.gd/.tscn/.unity` **零命中** ⇒ **不冲突，不用管**。
     本条原来写「与本实测直接冲突」，是在**拿另一个工程的开关当本工程的约束**。
   - ⚠️ **文档自己留的那条保留意见照旧成立**：§2.1 的旁证说明「两组几何」的边界是
     **渲染批次边界、不是稀有度边界** ⇒ 本落地是**照实拍复刻**，不是「原版设计如此」。
     **11 张 `tactic` 的 `special`（6 张 EC 药剂 + 5 张 DA 秘密卡）从没单独量过**，
     按阵营规则吃到 **EmperorsChildren → tier1 / DarkAngels → tier2**。
4. **顺带查实的一处数据不一致（`special` 到底几张）**：
   - `MyGame/Assets/RuleEngine/Resources/cards_engine.json`：`rarity=="special"` = **53 张**
     （39 张 `type=defence` + 14 张 `type=tactic`）。
   - `数据/游戏数据/defensive_cards_39.json`：39 张防御卡 + 6 张 EC 药剂（单列）。
   - `普查产出_0913` 两文件写「`_合并总表.md` 里 special 实为 **45** 行 = 39 防御 + 6 药剂」。
   - **三者不一致**（39 / 45 / 53）。53 张里多出来的 14 张 tactic 含：
     6 张 EC 药剂（`EC72–77`）· 5 张 DarkAngels 秘密卡（`DA_Convoke_the_Circle` 等）·
     `GSC68 Rusted Vents` · **2 张重名重复**（`BL_Helfire_Pit` / `BL_Helfire_Torch`，拼写 `Helfire`
     与正牌 `BL71 Hellfire Pit` / `BL69 Hellfire Torch` 重复，`card_stats_report.md` 已记为 `ratio` 模糊匹配项）。
   ⇒ **`special` 到底该定义为哪 39 张，需与 `_合并总表.md` 对齐后再落地。**
   ✅ **2026-09-18 实测（引擎表 `version 6` / `count 1126`）：`special` = 50 张** = **39 defence + 11 tactic**
   （tactic 那 11 张 = 6 张 EC 药剂 `EC72–77` + 5 张 DA 秘密卡）。
   53 → 50 的差额**已查清**：`BL_Helfire_Pit` / `BL_Helfire_Torch` 两张重名重复**已不在引擎表**；
   `GSC68 Rusted Vents` 现在是 `type=defence`（已含在那 39 里）。
   ⚠️ 39 / 45 / 50 三个数**说的是不同口径**（「防御卡」「防御卡+药剂」「引擎表里 rarity=special 的全部」），
   不是互相打架 —— 引用时**写清是哪个口径**。

---

## 四、坑（后人别重犯）

1. **🔴 「4 档框 = 4 档稀有度」是错的** —— 卡图上层不出帧档。判稀有度只能看**底部菱形宝石**。
   `CardTier`（4 档·升级用）与 `CardRarity`（5 档·含 `Special=5`）是**两个枚举**。
   全包扫过：**只有 tier1–4 卡框，不存在 tier5**（`assets_full` 493 个 `*ardframe*`，tier 后缀各 82）。
2. **🔴 900×1200 的 PnP 卡来自多个渲染批次，且垂直位置随机** —— 同一张卡在两份文件里也能差 88 px
   （`cover.png` 卡体 y42–1027 vs `Warpforge_67_Hive-Factory.png` y130–1115，**内容尺寸完全相同**）。
   ⇒ **任何用绝对坐标切卡图的做法都会错**。必须先按 alpha 外接框定位。
   同阵营三张的外接框**完全相同**（阵营级常数），跨阵营从 589×901 到 748×1054 不等。
3. **「`{1..5}_40k_cardframe_rarity_*`」是宝石、不是卡面框** —— 它们是 84×76 的牌表小图标；
   卡面框只有 `tier1–4`。（0913 后半文件已记，此处复述以免再踩。）
4. **不要用 IoU 比轮廓** —— t4 的轮廓是 t1 的**超集**，IoU 会系统性偏向 t4。
5. **切片对齐时不要只取外圈带就宣称"像素相同"** —— 外接框归一后仍会带进画窗内的立绘内容差异
   （本次实测：左带取 16% 宽时组内 `mean|d| 10–16` 全是立绘差异；取 7.5% 宽才是纯框）。

## 五、出处

| 要什么 | 去哪 |
|---|---|
| `special` 39 张清单 | `数据/游戏数据/defensive_cards_39.json`（含 `dir_faction_map`） |
| 0913 已有实测（20+19 张、两法互验、把握度） | `资料/普查产出_0913/special_tier_实测_前半.md` / `_后半.md` |
| 稀有度判色法 / special hue≈15 | `资料/卡表核对_卡图提取/_裁定_稀有度.md` |
| 卡图根 | `d:/2/Warpforge部队卡片/<阵营目录>/5防御卡/` |
| 4 档框素材（104 张） | `d:/4/Unity/素材/Warpforge原版/卡框/` |
| 枚举与取框逻辑 | `d:/2/Warpforge_code/Scripts/Assembly-CSharp/{CardTier,CardRarity,CardFramesSO,CardServices,CardTierConfig}.cs` |
| 现行映射（待改） | `MyGame/Assets/CardPresentation/Core/CardArt.cs` 的 `TierOf()` · `工具/import_original_art.py` 的 `RARITY_TIER` |
