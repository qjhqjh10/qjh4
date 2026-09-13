# `special` 稀有度 → 卡框 tier 实测 —— 后半（2026-09-13 子代理普查产出）

> 任务来源：`资料/可并行任务清单.md` 第 ⑭ 条；对应**悬案 #4**（原版 SO 只 4 档卡框，我们暂按 tier4）。
> 分片：39 张 special 防御卡按卡名字母序**后 19 张**（#21 `Inspiring Legacy` … #39 `Webway Entrance`）。

## 🔴 一、三条会改变问法的前提修正

### 1. **悬案 #4 的问法本身可能不成立** —— `CardTier` 与 `CardRarity` 是**两个独立枚举**

- `d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardTier.cs`：`CardTier{Tier1..Tier4}`
- `d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardRarity.cs`：`CardRarity{None, Common, Rare, Epic, Legendary, Special=5}`
- `CardFramesSO.GetClanFrame(army, **cardTier**, cardTypeOptions)` 收的是 **CardTier**；
  而 `CardServices.GetCardTier / CheckForCardUpgrade / CardTierConfig` 表明它来自**卡牌升级档**，**不是稀有度**。

⇒ 「`special` 映射到哪一档」这个问法本身可能就问错了。**见下面 §三④ 的实测反证。**

### 2. `去重资源/` 里那 5 张 `*_cardframe_rarity_*.png` **不是卡面框**

它们是 **84×76 的「牌表小图标」**。
**卡面框只有 tier1–4**：`d:/4/Unity/素材/Warpforge原版/卡框/` —— **104 张** = 13 阵营 × {troop, stratagem} × 4 档。
（另有 `CardPresentation/Art/原版/去重资源/{1..5}_40k_cardframe_rarity_*.png`）

### 3. 「39 张」的准确出处

`d:/4/Unity/数据/游戏数据/defensive_cards_39.json` —— 13 阵营 × 3 张**防御卡**（另有 6 张 EC 药剂单列）。
⚠️ `_合并总表.md` 里 `rarity=special` 实为 **45 行**（39 防御 + 6 药剂）。

## 二、独立判据

**视觉（逐档复杂度阶梯；跨阵营美术不同，但阶梯一致）**

| 档 | 特征 |
|---|---|
| **tier1** | 最要素框。素拱/素帐顶，**无哥特尖塔、无雕像、无侧柱夹钳铆钉** |
| **tier2** | 加**顶边/左上的哥特尖塔或旗幡**；侧柱中腰加**夹钳+铜铆钉**（Tau）或**圆通风口+吊链、侧柱弹药包、中横绿带、底部翼形饰**（AM） |
| **tier3** | 再加**更高的暗色尖塔（带红窗）、侧柱骨状雕饰与雕像**；仍无大面积金 |
| **tier4** | 最繁复 + **大面积金/青铜饰带**（柱身、底横幅）+ 蓝金花纹旗 |

⚠️ 例外：SW 的 tier1 本身就带一条细黄铜线 ⇒「金 = tier4」只在「**大面积**金饰带」意义上成立。

**定量（可复现，实际用它判的 19 张）**：
把候选框按「卡本体在行/列中线的外沿」缩放+平移对齐 → **只在框 `alpha≥250` 且卡外圈带内**
（排除画窗与文本框）算 `|RGB|` 平均绝对差（MAD）。
判据：同阵营同族**正确档 MAD 9–22**，**错误档 20–60**；**跨阵营/跨族 ≥40**（所以先锁阵营与 troop/stratagem 族）。

反向验证：Tau 的传说/史诗/稀有/普通**全部 tier1**（MAD 16/15/18/13）；Aeldari 中期批次为 tier2。

## 三、实测表（后半 19 张）

| 卡名 | 阵营 | 卡面框档实测 | 判据 | 把握 |
|---|---|---|---|---|
| Inspiring Legacy | Aeldari / SaimHann | **tier1** | MAD 9.6，次优 37（margin 27） | 高 |
| Light Cover (`cover.png`) | Ultramarines | tier1（tier2 在噪声内） | MAD 30.1 vs 33.4（margin 3.3）；画面为最简素框 | **低** |
| Munitorum Supplies | Astra Militarum | **tier2** | MAD 13.0 vs 19；目视见 tier2 独有通风口+吊链、侧柱弹药包、中横绿带、底部翼形饰 | **高（目视确认）** |
| Orbital Defences | Space Wolves | **tier1** | MAD 21.9 vs 29/32；目视 = SW 素框（帐顶+两侧毛皮流苏+细黄铜线，无尖顶） | **高（目视确认）** |
| Pilfered Supplies | Genestealer Cult | **tier2** | MAD 12.9 vs 18.1 | 中 |
| Plasma Generator | Dark Angels | **tier2** | MAD 9.3 vs 19.8（margin 10.5） | 高 |
| Protective Bio-structure | Tyranid | **tier1** | MAD 13.6 vs 25 | 高 |
| Resurrection Vault | Necron | **tier1** | MAD 11.6 vs 22.4 | 高 |
| Rusted Vent | Genestealer Cult | **tier2** | MAD 12.8 vs 18.1 | 中 |
| Sacred Altar | Sororitas | **tier2** | MAD 29.5 vs 34.2；目视 = tier2 的红侧板+拱顶小花饰+柱身卷草纹 | 中（目视确认档位，绝对拟合差） |
| Throne of the Heretic | Chaos / BlackLegion | **tier1** | MAD 11.8 vs 24.6（margin 12.8） | 高 |
| Tidewall Droneport | Tau | **tier1** | MAD 11.2 vs 30/50（margin 19） | 高 |
| Tidewall Gunrig | Tau | **tier1** | MAD 12.2 vs 30 | 高 |
| Timely Reinforcements | Astra Militarum | **tier2** | MAD 12.9 vs 19 | 中 |
| Toxic Bonfire | Orks | **tier2** | MAD 12.3 vs 22.7（margin 10.4） | 高 |
| Toxic Vapours | Tyranid | **tier1** | MAD 12.7 vs 24.9 | 高 |
| Vial Tanks | Emperor's Children | **tier1** | MAD 15.3 vs 19.4（margin 4.1） | 中 |
| Watch Tower | Tau | **tier1** | MAD 11.2 vs 30/50；目视 = 无侧夹钳/铜铆钉/底部尖刺 | **高（目视确认）** |
| Webway Entrance | Aeldari | **tier1** | MAD 9.9，次优 37（margin 27） | 高 |

**分布：tier1 = 12 张 · tier2 = 7 张 · tier3 = 0 · tier4 = 0。**
⇒ **没有一张 `special` 卡用 tier4**，即我们当前的「**暂按 tier4**」**与原版卡面不符**。

**判不出 0 张**；但 1 张低把握（`Light Cover`，tier1 与 tier2 只差 3.3，且 `cover.png` 命名异常）、
4 张中把握（`Pilfered Supplies` / `Rusted Vent` / `Timely Reinforcements` / `Vial Tanks`，margin 4–7）。

## 四、四条硬结论

1. **框档不跟稀有度** —— 39 张同为 `special`，实测却 tier1/tier2 并存。
2. **同阵营内还会按「批次」分档** —— Aeldari 4计策 10–12 号 = tier2，45–54 号 = tier1，
   而两批里 common/rare/epic/legendary **混编**。
3. `special` 在成品卡面上的唯一表达是**底部橙色菱形宝石**（橙红 hue≈15，见 `_裁定_稀有度.md`），
   **不靠框档**。
4. ⇒ **建议把悬案 #4 改判**：不是「`special` 映射到哪一档」，而是
   「**原版不存在 `rarity→tier` 映射；卡面框档是独立轴（升级档 / 印刷批次）**」。

## 五、落地提醒

`分层卡演示_0827/README.md` 里 **`GameData.force_frame_tier4=true` 把全卡框强行拉到 tier4**，
与本实测（`special` 实为 tier1/tier2）冲突。**若目标是复刻卡面观感，`special` 至少不该默认 tier4。**

**视觉已确认的 4 张**（可供人工复核）：`Watch Tower`(t1) · `Munitorum Supplies`(t2) ·
`Sacred Altar`(t2) · `Orbital Defences`(t1)。
