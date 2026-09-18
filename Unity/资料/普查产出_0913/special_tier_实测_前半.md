# `special` 稀有度 → 卡框 tier 实测 —— 前半（2026-09-13 子代理普查产出）

> 任务来源：`资料/可并行任务清单.md` 第 ⑭ 条；对应**悬案 #4**。**后半见同目录 `special_tier_实测_后半.md`**。
> 分片：39 张 special 防御卡按卡名字母序**前 20 张**（`Alien Idol` … `Holy Fire`）。
> ⚠️ `_合并总表.md` 里 `special` 实为 **45 行** = 39 张防御卡（13 阵营 × 3）+ **6 张 EC 药剂/Combat Elixir**
> （`Antrak Silk` / `Heliotrophos` / `Quail` / `Shivversplint` / `Skorflense` / `Xylocil`）。**本文件只覆盖防御卡。**

## 🔴 两半合起来的总结论

| | tier1 | tier2 | tier3 | **tier4** |
|---|---|---|---|---|
| 前半 20 张 | 12 | 8 | 0 | **0** |
| 后半 19 张 | 12 | 7 | 0 | **0** |
| **合计 39 张** | **24** | **15** | 0 | **0** |

⇒ **没有一张 `special` 卡用 tier4。**

### 要改的地方（精确到行）

- ~~`d:/4/Unity/工具/import_original_art.py` **第 82 行**：`RARITY_TIER['special'] = 4`~~
  ⇒ 🔴 **2026-09-18 实测：那句是死代码**（全仓无引用、导出脚本对每个阵营无条件导全部 4 档，
  改它不改变任何产物）—— **该表已删**。「两处」「同一张表」的说法作废。
- `d:/4/Unity/MyGame/Assets/CardPresentation/Core/CardArt.cs` 的 `TierOf(rarity, faction)` ← **生效路径只此一处**

⚠️ **但不是「把 4 改成 1」** —— 39 张里 24 张是 tier1、**15 张是 tier2**，
所以「一个稀有度对应一个档」这个模型本身不成立（见下）。
✅ **2026-09-18 用户拍板：照 PnP 实测「按阵营」给档，已落地**（判 tier2 的 5 阵营 =
`AstraMilitarum`·`DarkAngels`·`Genestealers`·`Goff`·`Sororitas`，其余 → tier1）。
落地细节与两条作废说法见 `资料/卡表核对_卡图提取/_裁定_special卡框.md` §三。

### 为什么「改成 1」也不行 —— 三条独立旁证

1. **框档不跟稀有度**：39 张同为 `special`，实测 tier1/tier2 并存。
2. **同阵营内按「批次」分档**：Aeldari `4计策` 的 `Graceful Avoidance`(common) / `Whirling Death`(rare) /
   `Craftworld Convergence`(epic) / `Dance of Death`(legendary) 分属**两组几何**
   （bbox `104–797×93–1146` vs `130–771×142–1114`），**rare 同时出现在两组**。
3. **`CardTier` 与 `CardRarity` 本来就是两个枚举**（`CardTier{Tier1..4}` / `CardRarity{…,Special=5}`），
   `CardFramesSO.GetClanFrame(army, **cardTier**, …)` 收的是 `CardTier`，
   而 `CardServices.GetCardTier / CheckForCardUpgrade / CardTierConfig` 表明它来自**卡牌升级档**。
   ⇒ 推测跟原作 **per-card `cardTier`** 走。

✅ **稀有度本身没错**：20 张底部中央菱形宝石实测 hue 15–31（橙红 = special），与 `_裁定_稀有度.md` 的
`special hue≈15` 一致 —— **是框档在变，不是稀有度错了**。

## 判据（三条独立证据，四路同向才算高把握）

> 原始框：`d:/4/Unity/素材/Warpforge原版/卡框/40k_Cardframe(s)_stratagem_<阵营>_tier1..4.png`
> （104 个 = 13 阵营 × 4 档 × 2 类）。**防御卡走 `stratagem` 序列。**

**S｜轮廓特征（对缩放/位置免疫，最可靠）**
用卡图 **alpha 通道**求「基座矩形」：左右竖边取行中位数、上下横边取「行宽 ≥0.85×基座宽」的首末行；
再算四个**越出基座矩形量 ÷ 基座宽** = `top / bot / left / right`。同法算 4 档框，比欧氏距离。
⚠️ **同阵营 4 档若 `top/bot/left/right` 相同**（SaimHann t1=t2=t3、DarkAngels t1=t2=t3、EC t1=t2），
**此路只能排除 t4**，必须靠 D 或视觉。

**D｜基座对齐后 RGB NCC**
按基座矩形把 tier 框缩放到卡图并平移对齐，在框体 `alpha>200` 区域比亮度 NCC；
**每档各自做局部 `(S, ox, oy)` 最优搜索**（公平），取最高分为该档。

**视觉兜底（各阵营 4 档的判别性装饰，逐阵营不同）**

| 阵营 | t1 → t2 → t3 → t4 |
|---|---|
| GSC | 素 → **顶黄黑警示电缆＋右上金灯＋左上铁钩红布幡** → 顶中骷髅 → 两侧人形 |
| AstraMilitarum | 素 → **左上圆格栅＋铁链＋底角弹药包＋底中翼饰** → 顶骷髅＋两侧刀＋封蜡纸 → 两侧金像 |
| Ultramarines | 素 → **左上单尖塔** → 双尖塔＋两侧兜帽骑士像 → 金蓝＋尖拱＋两侧金翼人像 |
| Sororitas | 素 → **暗红镶边＋银卷草＋隔板百合花饰** → 金饰＋烛台＋骷髅徽/骷髅列 → 天使＋两侧修女像 |
| SaimHann | 素 → **加青玉宝石** → 更多宝石＋白纹 → 尖拱＋红饰＋羽饰 |
| Orks | 素 → **左轨链甲带＋右轨吊线/工具** → 顶中尖刺项圈 → 左上大尖刺肩甲＋中颚齿 |
| DarkAngels | 素（已有尖拱）→ **金翼＋铁链＋两侧三联骷髅壁龛** → 金饰＋圣像＋绷带缠绕 → 两侧兜帽守望者像 |
| EC | 素（紫＋银刺，底部椭圆舱）→ **加金边＋金铆钉＋左上金球** → 金龙头＋两侧弯刀 → 宝石＋锁链＋紫布＋红甲 |

## 一、实测表（前半 20 张）

| 卡名 | 阵营 | 卡面框档实测 | 判据 | 把握 |
|---|---|---|---|---|
| Alien Idol | GenestealerCult | **tier2** | S：card`[.071 .033 .039 .040]` vs t2`[.074 .032 .039 .041]` 距 0.003，t1 距 0.060（决定性）；D：t2 .618 > t1 .546；视觉：顶部黄黑警示电缆＋右上金灯＋左上铁钩红布幡（t1 三者皆无） | 高 |
| ~~Antrak Silk~~ | EmperorsChildren | —（**非防御卡**，属另 6 张药剂） | — | — |
| Armoury of Excess | EmperorsChildren | **tier1** | D：t1 .537 > t2 .481（差 .055）；S：t1=t2 距 .003 打平、t4 .027 排除；视觉：全框无金边/金铆钉/左上金球/金龙头/两侧弯刀 → 非 t2/t3/t4 | 中高 |
| Aspect Shrine | Aeldari(SaimHann) | **tier1** | D：t1 .829 ≫ t2 .724（差 .105，20 张里最干净）；视觉：侧柱/顶中/隔板/底角**全无青玉宝石**（t2 起有） | 高 |
| Awakened Obelisk | Necron(Sautekh) | **tier1** | S：`[.038 .043 .034 .031]` vs t1 距 .001（t2 .003 / t3 .015 / t4 .045）；D：t1 .365 > t2 .308；视觉：顶边平直无抬升块、无顶中绿球、无两侧绿球/金舱 | 中高 |
| Decadent Throne | EmperorsChildren | **tier1** | D：t1 .555 > t2 .500；S/几何与 `Armoury of Excess` **完全同签名**；视觉同 EC t1（无金） | 中高 |
| Defence Batteries | AstraMilitarum | **tier2** | 视觉（决定性）：**左上圆形格栅＋垂落铁链＋底角弹药包＋底中翼饰＋隔板金属片** = AM t2 特有；D：t2 .540 ≫ t1 .399 | 高 |
| Defensive Turrets | DarkAngels | **tier2** | D：t2 .576 > t1 .454（差 .122）；视觉：左上金翼＋铁链、两侧**三联骷髅壁龛**（t1 无）、底中翼饰 | 高 |
| Digestion Pool | Tyranid(Leviathan) | **tier1** | D：t1 .687 ≫ t2 .521（差 .166）；S：t1 距 .002（t2 .017） | 高 |
| Divination Menhir | Necron(Sautekh) | **tier1** | S：t1 距 .001；D：t1 .386 > t2 .316；与 `Awakened Obelisk` 同签名 | 中高 |
| Extractor Rig | Orks | **tier2** | D：t2 .671 > t1 .578（差 .093）；视觉：左轨链甲带＋右轨吊线/红布＋隔板木条工具 = Goff t2 | 中高 |
| Fenrisian Runestones | SpaceWolves | **tier1** | D：t1 .629 ≫ t2 .494（差 .134）；S：t1 距 .004 < t2 .005、t3 .045/t4 .085 | 高 |
| Firestrike Turrets | Ultramarines | **tier1** | S：t1 距 .013（t2 .020 / t3 .078 / t4 .079）；视觉：顶左**无哥特尖塔**、无两侧骑士像、无金蓝；D 与 t2 仅差 .015（弱） | 中 |
| Grim Effigy | DarkAngels | **tier2** | D：t2 .640 > t1 .510（差 .130）；与 `Defensive Turrets` 同签名/同几何 | 高 |
| Gretchin Tower | Orks | **tier2** | D：t2 .707 > t1 .611；视觉同 `Extractor Rig` | 高 |
| Guidance of the Saints | Sororitas | **tier2** | S：t2 距 .002 vs t1 .030（决定性）；D：t2 .588 > t1 .506；视觉：**暗红镶边＋银卷草＋隔板百合花饰** | 高 |
| Halls of Legend | SpaceWolves | **tier1** | D：t1 .543 ≫ t2 .407（差 .136）；S：t1 距 .004 | 高 |
| Hellfire Pit | BlackLegion | **tier1** | S：t1 距 .002 vs t2 .021；D：t1 .585 > t2 .459 | 高 |
| Hellfire Torch | BlackLegion | **tier1** | D：t1 .560 > t2 .440；与 `Hellfire Pit` 同签名 | 高 |
| Hive Factory | Ultramarines | **tier1** | S：t1 距 .013 最佳；与 `Firestrike Turrets` 同签名/同几何；D 与 t2 仅差 .011（弱） | 中 |
| Holy Fire | Sororitas | **tier2** | S：t2 距 .002 vs t1 .030；D：t2 .503 > t1 .429；与 `Guidance of the Saints` 同签名 | 中高 |

**tier1（12 张）**：Armoury of Excess · Aspect Shrine · Awakened Obelisk · Decadent Throne · Digestion Pool ·
Divination Menhir · Fenrisian Runestones · Firestrike Turrets · Halls of Legend · Hellfire Pit ·
Hellfire Torch · Hive Factory
**tier2（8 张）**：Alien Idol · Defence Batteries · Defensive Turrets · Extractor Rig · Grim Effigy ·
Gretchin Tower · Guidance of the Saints · Holy Fire

**判不出 0 张**；**中把握 2 张**（`Firestrike Turrets` / `Hive Factory`，D 与 t2 只差 .011–.015，
靠 S 与 «UM t1 无左上尖塔» 的目视定案）。

## 二、可信度证据

**8 组同阵营双卡**（EC、DA、Orks、SW、Sautekh、UM、Sororitas、BlackLegion）
**签名逐位相同、判档 8/8 一致** ⇒ 不是噪声，**卡框在同阵营内是稳定的**。

## 三、⚠️ 踩过的坑（后人别重犯）

1. **900×1200 的 PnP 卡来自至少两个渲染批次** —— 卡框连**绘制尺寸**都不同
   （alpha 轮廓高 **903–1093 px，差 20%**）⇒ **任何不做缩放归一的像素比对都会失败**。
2. **IoU 不能用** —— t4 的轮廓是 t1 的**超集**，IoU 会**系统性偏向 t4**。
3. **`S` 签名只在「同阵营 t1/t2/t3 互不相同」时能单独定案**
   （GSC、Sororitas、AstraMilitarum 是这种）；
   若 t1=t2=t3 相同（EC、DA、Orks、SW、Sautekh、UM、BlackLegion、Leviathan、SaimHann、Tau 多是这种），
   必须靠 **D 的 NCC 差值（>0.05 才可信）+ 上面那张阵营视觉清单**。

## 四、相关文件

`资料/卡表核对_卡图提取/_合并总表.md` · `_裁定_稀有度.md` ·
`工具/import_original_art.py` · `MyGame/Assets/CardPresentation/Core/CardArt.cs` ·
`素材/Warpforge原版/卡框/` · `MyGame/Assets/CardPresentation/Resources/Art/cards/frame_<阵营>_strat_tier1..4.png` ·
对照图 `资料/留档_排查证据/卡面组装_0912/tier_map.png`
