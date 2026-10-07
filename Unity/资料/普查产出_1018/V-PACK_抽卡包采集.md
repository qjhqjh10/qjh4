# V-PACK · 抽卡包「包名=包内某卡」规律核 + 原版 id→卡名 采集

> 只读采集。⛔ 没跑生成器 · 没跑 Unity · 没动 git · **一个 id 都没回填** · 只写了两个文件（本报告 + `V-PACK_原版id到卡名.tsv`）。
> 落笔 2026-10-18。判据全在本地：`d:/2/新解包资源/assets_full/` · `d:/2/Warpforge_code/Scripts/Assembly-CSharp/`（类桩）· `d:/4/Unity/`。
> 上游判据正本：`资料/普查产出_1018/V-ID_卡id命名空间复核.md`（整篇读过，其 §8-4 就是本轮的题目）。

---

## 1. 一句话结论

**「包名 = 包内某一张卡」这条规律，在【包名能被本地认出来的那一档】上【成立且很硬】；在【认不出来的那一档】上【判不了】—— 而认不出来的那档占 75%。所以它【不能】把采集面从 17 条扩到 ~880 条；实采 = 48 条（33 条新 + 17 条旧，其中 2 条重叠），覆盖我们卡池的 ~4.3%。**

**逐包给结论（882 个包，三类，加总 = 882）：**

| 类 | 包数 | 结论 |
|---|---|---|
| **A · 名字认得出 + 该包 3 张卡的原版稀有度都查得到** | **42** | **规律成立**：33 个包能唯一解出一个 (id, 卡名) 对子；9 个包因同稀有度平手判不了 |
| **B · 名字认得出，但 id 侧稀有度查不全/没有** | **176** | **判不了**（缺 id 侧判据，不是「不成立」） |
| **C · 包名在任何本地名字源里都找不到** | **664** | **判不了**（见 §3-③，这批名字在 `d:/2`+`d:/4` 全仓只出现在抽卡包里） |

**关键读数（分母都是「名字认得出」的那 215 个包）：**

| 检验 | 读数 | 若「包名 = 包内某张卡」应当是 |
|---|---|---|
| **甲 · 阵营**：我们池给这个名字的 `faction` == 包的 `packArmy` | **212 / 215 = 98.6%** | 100%（例外 3 条疑似同名跨阵营，见 §3-②） |
| **乙 · 稀有度**：我们池给这个名字的 `rarity - 2`（下限 0）== 包的 `packTier` | **190 / 215 = 88.4%** | 100% |
| **丙 · 「包名那张卡就是包里唯一那张最高稀有度的卡」** | **37 / 42 = 88.1%** | 100% |
| 丙的**基线**（随便挑包里一张卡，它是最高稀有度的概率） | **39.9%** | —— |
| **丁 · 结构性**：包里 3 张卡的最高稀有度 == `packTier + 2` | **306 / 312 = 98.1%**（另 6 条只低不高） | 100% |
| **戊 · 包级直接真值**（`V-ID` 那 3 条） | **3 / 3 命中**（逐条复现） | 3/3 |
| **己 · 与 `card_ids.json` 交叉核** | **0 / 48 一致 = 100% 错配** | 48/48 —— 与 `V-ID` 的 0/17 同向、同样彻底 |

⇒ **甲、乙、丙、丁四条是【互相独立】的尺子**（甲用阵营、乙/丙用稀有度、丁只看原版两侧），**四条同向**，所以「包名是包里某张卡的名字」这一档**置信度高**。
⇒ **但 丙 那条「featured = 最高稀有度」的加强版【被证伪】**：按它去逐包钉，会有 **81 个 id 被钉到 2 个以上互相打架的包名上**（§5-①）。所以 **不能用它扩采集面**，只剩「用名字自己的稀有度去包内对号入座」这一条可用的路（§5-②）。

---

## 2. 抽卡包资产的形状

**位置**：`d:/2/新解包资源/assets_full/bundle_draftpacks_assets_all/MonoBehaviour/*.json` —— **882 份**，文件名 = 包名 + `.json`（含 `'` 开头的，如 `'Eadbanger.json`；26 组重名的会带 `_<pid>` 后缀，如 `Assault Powerhouse_8060185568102621531.json`）。

**类**：`PrebuiltPack : IdentifiableSO` —— 类桩在 **`d:/2/Warpforge_code/Scripts/Assembly-CSharp/PrebuiltPack.cs`**，枚举在 **`…/PackTier.cs`**。

| 字段（逐包实测） | 值 / 读数 |
|---|---|
| `m_Name` | 与 `packName` **基本同值**（个别处差一个字符，用 `packName` 为准） |
| **`packName`** | 包名（**本轮的 X**）。856 个互不相同；882 个包里 26 组重名 |
| `packId` | 包 id，形如 `UM_PointBlankShot` / `GOFF_BombSquig` / `DA_1stCompany` ⇒ **由包名去空格得来，不携带额外信息** |
| `nameRefId` | **882 个全为空串** ⇒ `GetLocalizedName()` 必落 `packName` 兜底 |
| **`packArmy`** | `CardArmy` 枚举数字。**13 个值，与卡 id 前缀 100% 自洽**：10=UM(131) · 20=GOFF+ORK(92) · 30=SH+ASH(73) · 40=SAU(60) · 50=BL(66) · 60=LEV(62) · 70=TAU(63) · 80=SOR(58) · 90=GSC(61) · 100=AM(65) · 110=DA(62) · 120=EC(42) · 130=SW(47) |
| **`packTier`** | `PackTier{Common=0, Epic=1, Legendary=2}`。分布 1:398 · 0:243 · 2:241。**与稀有度强相关，见 §4** |
| `packEnabled` | 1:872 · 0:10（10 个包原版自己关掉了） |
| **`cardIds`** | `List<string>`，**882 个包恒为 3 个**，全部是**原版命名空间**的 id（`UM34`/`GOF23`/`EC11`…）。**904 个互不相同的 id**（2646 个槽位，平均每个 id 出现 2.9 次） |

**没有踩到「没有 type tree」那类坑** —— 这批是完整字段的 MonoBehaviour JSON（`m_Script` 指向 `PrebuiltPack`），直接 `json.load` 即可，**不需要 `peek_name()` 手工解**。

**`packArmy` ↔ id 前缀的细节**（两条容易看错的）：
- `packArmy=20` 同时收 `GOF*`(62 包) 与 `ORK*`(30 包) —— **同一支军队两套前缀**；
- `packArmy=30` 同时收 `SH*`(60 包) 与 `ASH*`(13 包) —— 同理。
- 每个包内 3 个 id **同前缀**（= 同一阵营），无例外。

**⚠️ 一条结构性事实（很重要，支撑 §3-③ 的判断）**：**每个阵营里「互不相同的包名数」≈「该阵营出现在包里的 id 数」**：

| army | 包数 | distinct 包名 | distinct id | 包名−id |
|---|---|---|---|---|
| 10 UM | 131 | 105 | 104 | +1 |
| 20 Orks | 92 | 92 | 95 | −3 |
| 30 Aeldari | 73 | 73 | 75 | −2 |
| 40 Sautekh | 60 | 60 | 65 | −5 |
| 50 BL | 66 | 66 | 65 | +1 |
| 60 Leviathan | 62 | 62 | 65 | −3 |
| 70 TAU | 63 | 63 | 65 | −2 |
| 80 SOR | 58 | 58 | 65 | −7 |
| 90 GSC | 61 | 61 | 65 | −4 |
| 100 AM | 65 | 65 | 65 | 0 |
| 110 DA | 62 | 62 | 65 | −3 |
| 120 EC | 42 | 42 | 55 | −13 |
| 130 SW | 47 | 47 | 55 | −8 |

⇒ **「一个包名 ↔ 一个 id」，包差不多是「一张卡一个包」**。（EC/SW 差得最多 —— 它们是最后上线的两个阵营。）

---

## 3. 逐包结果

### ① 三类划分（加总 882）

见 §1 那张表：**A=42 · B=176 · C=664**。

**A 类 42 个包逐条列**（`…` 后是 `原始稀有度(卡) ｜ tier ｜ 名字在我们池里的稀有度`；★ = 唯一解出 id）：

| 包名 | cardIds（稀有度） | tier | 名字稀有度 | 解出的 id |
|---|---|---|---|---|
| 'Ardshell Gurk | GOF33(1),GOF16(1),**GOF23(4)** | 2 | legendary(4) | ★ GOF23 |
| Angels of Death | UM6(1),UM64(3),**UM13(3)** | 1 | epic(3) | 平手(2) |
| Avenging Zeal | UM15(1),UM23(1),**UM40(4)** | 2 | legendary(4) | ★ UM40 |
| Banner Nob | GOF5(1),GOF43(1),**GOF11(2)** | 0 | rare(2) | ★ GOF11 |
| Battlefield Supremacy | **UM62(4)**,UM44(2),UM59(1) | 2 | legendary(4) | ★ UM62 |
| Big Choppa Nob | GOF14(1),GOF67(2),**GOF68(4)** | 2 | legendary(4) | ★ GOF68 |
| Bomb Squig | **GOF3(3)**,GOF36(1),GOF61(2) | 1 | epic(3) | ★ GOF3 |
| Boss Nob | GOF73(2),GOF9(2),GOF67(2) | 0 | rare(2) | 平手(3) |
| Captain Sicarius | UM21(2),UM25(1),**UM29(4)** | 2 | legendary(4) | ★ UM29 |
| Da Green Horde | GOF4(2),**GOF37(3)**,GOF39(1) | 1 | epic(3) | ★ GOF37 |
| Da Irongob | GOF36(1),GOF42(1),GOF24(2) | 0 | **common(1)** | 无候选（稀有度打架） |
| Da Old Ways | GOF79(2),GOF101(2),GOF88(2) | 0 | rare(2) | 平手(3) |
| Dead Choppy | **GOF72(3)**,GOF10(1),GOF61(2) | 1 | epic(3) | ★ GOF72 |
| Deff Dread | GOF5(1),GOF43(1),**GOF22(2)** | 0 | rare(2) | ★ GOF22 |
| Deffkopta | **GOF7(3)**,GOF38(2),GOF65(2) | 1 | **rare(2)** | ★ GOF38（**中**：稀有度与 tier 打架；靠消元） |
| Grizzled Skarboy | GOF8(1),GOF66(1),**GOF74(2)** | 0 | rare(2) | ★ GOF74 |
| Honour Guard | UM4(1),**UM14(2)**,UM23(1) | 0 | rare(2) | ★ UM14 ✔真值 |
| Humanity's Shield | **UM60(4)**,UM61(1),UM8(2) | 2 | legendary(4) | ★ UM60 |
| Killa Kan | GOF35(2),GOF36(1),**GOF17(3)** | 1 | epic(3) | ★ GOF17 |
| Krump da Gitz | GOF71(1),GOF4(2),GOF70(3) | 1 | **common(1)** | ★ GOF71（**中**） |
| Krumpaklaw | GOF66(1),GOF67(2),**GOF70(3)** | 1 | epic(3) | ★ GOF70 |
| Mega Blasta Deffkopta | GOF72(3),GOF56(2),GOF69(1) | 1 | **common(1)** | ★ GOF69（**中**） |
| Meganob Ugrak | GOF8(1),GOF45(1),**GOF19(3)** | 1 | epic(3) | ★ GOF19 |
| Monster Hunters | GOF79(2),GOF105(2),GOF96(1) | 0 | rare(2) | 平手(2) |
| No Mukkin' About | GOF1(1),**GOF35(2)**,GOF40(2) | 0 | rare(2) | 平手(2) |
| Oath of Moment | **UM73(2)**,UM56(1),UM57(1) | 0 | rare(2) | ★ UM73 |
| Ork Nob | GOF55(1),GOF35(2),**GOF57(4)** | 2 | legendary(4) | ★ GOF57 |
| Painboy Sniklaw | GOF14(1),**GOF18(2)**,GOF26(1) | 0 | rare(2) | ★ GOF18 |
| Point-Blank Shot | UM41(1),UM33(1),**UM34(2)** | 0 | rare(2) | ★ UM34 ✔真值 |
| Predator Annihilator | UM70(1),UM75(1),**UM72(2)** | 0 | rare(2) | ★ UM72 |
| Primaris Chaplain | UM35(3),UM13(3),UM18(3) | 1 | epic(3) | 平手(3)（真值 UM13 在其中） |
| Prophet of the Waaagh! | **GOF34(4)**,GOF8(1),GOF47(3) | 2 | legendary(4) | ★ GOF34 |
| Rok Invasion | GOF7(3),GOF40(2),**GOF48(4)** | 2 | legendary(4) | ★ GOF48 |
| Sergeant Telion | UM10(1),**UM20(4)**,UM22(2) | 2 | legendary(4) | ★ UM20 |
| Spear of Macragge | UM44(2),**UM12(4)**,UM30(2) | 2 | legendary(4) | ★ UM12 |
| Stikkbomb Boy | **GOF65(2)**,GOF74(2),GOF69(1) | 0 | rare(2) | ★ GOF65（靠消元） |
| Stomp 'Em | GOF73(2),GOF68(4),GOF22(2) | 2 | **rare(2)** | ★ GOF73（**中**，靠消元） |
| Stormboyz Strike | GOF32(2),GOF33(1),GOF46(2) | 0 | rare(2) | 平手(2) |
| Tactical Insight | **UM32(3)**,UM31(2),UM42(1) | 1 | epic(3) | ★ UM32 |
| The Chapter's Due | UM61(1),UM56(1),UM58(1) | 0 | **common(1)** | 无候选 |
| Valtus | UM84(2),UM81(1),**UM83(3)** | 1 | epic(3) | ★ UM83 |
| Will of Gork | GOF31(2),GOF15(2),**GOF47(3)** | 1 | epic(3) | ★ GOF47 |

（B 类 176 + C 类 664 **不逐条列**，理由见本节 ②③；要复算跑 `§9` 里那两段脚本即可。）

### ② 不命中的有没有别的规律？—— **有的，而且是三条**

1. **乙/丙 那 12%~16% 的偏差不是「随机噪声」，而是集中在「名字稀有度 < tier+2」上**（对照表见 §4 的 3×4 列联表）。可能的解释有三，**我不裁**：① 我们池的 `rarity` 对那几张卡就是错的（`rarity` 来源是 `card_stats.json` + 宝石像素取色，本身有已知补丁史，见 `工具/gen_cards_engine.py:115-175`）；② 那张卡的**名字在另一阵营也有一张同名卡**；③ **featured 卡本来就不是包里最高稀有度那张**（丙 只剩 88.1% 而非 100%，支持 ③ 有份）。
2. **甲乙丙都在说的是「这个包名是一张真卡的名字」**，而 **C 类 664 个包名不是**（见 ③）。这两件事**必须分开说**，混起来就会得出「规则成立 98%」这种假读数。
3. **阵营那一把尺子只有 3 条反例**：`Inescapable Retribution`（SOR 包，我们池说 Sautekh）· `Perfect Hosts`（BL 包，我们池说 Genestealers）· `Rapid Deployment`（TAU 包，我们池说 Ultramarines）。**三条都像「同名卡跨阵营 + 我们池只登记了其中一张」**，**标疑似**，没有逐条查证。

### ③ C 类那 664 个包名是什么？—— **查不到，且证据两边打架，我不下结论**

**支持「它们也是卡名」的**：① 每阵营 distinct 包名 ≈ distinct id（§2 末表）；② 包名读起来全是 40k 味（UM：`Air Strike`/`Banner of Command`/`Auspex Incursion`；Orks：`'Eadbanger`/`Blazt`/`Waaagh Parade`；SW：`Canid Predators`/`The Fell-Handed`/`Ironhowl`）。

**反对「它们也是卡名」的（更硬）**：
- **卡图资产的名字里没有它们**。13 个 `<阵营>cardassets_assets_all/Texture2D/` 一共 **1194 个卡图名**（形如 `SM_UM_strat_Point-Blank Shot` / `Ork_BeastSnagga_inf_Grot`），**它们与我们的卡池名（1126 条）逐阵营重合 80%~97%** ⇒ **这两份是同一套卡名**，且**卡图这份应当是「每张卡都有图」**。
- 而 **UM 的 105 个 distinct 包名里，只有 15 个落在这 138 个 UM 卡图名（或卡池名）里**；`EC`(42) 与 `SW`(47) 两个阵营是 **0 / 42、0 / 47**。
- 这 664 个名字在 **`d:/2` + `d:/4` 全仓**只出现在抽卡包 JSON 里（搜法见 §9）。
⇒ 若 C 类也是卡名，那么这 13 个阵营就得再有 ~640 张「有包、有 id、却没卡图」的卡 —— 而卡图包里显然不是这样。**所以我说「判不了」，并把 C 类排除在采集面之外。**

> ⚠️ 这里有一条**我明确没有查清**、但决定性的疑点：**抽卡包这一族资产可能是【过期快照】**（包名是旧卡名，id 是现 id）—— 那么「一包一卡 + 旧名」能同时解释 ③ 的 664 与 ② 的 15/105。**我没有找到能证/否它的判据**（原版改名的记录本地没有）。**标疑似，置信度低。**

---

## 4. 卡名匹配口径（我用了什么归一化 / 拿谁当卡名）

**归一化**：`lower()` 后删掉所有非 `[a-z0-9]` 的字符（空格、`'`、`-`、`.` 全去掉）。
效果：`'Eadbanger` / `.75 Caliber` / `Dakka Diz'ipline` / `Ork_Goff` 里的撇号都能吃下。**没有做任何词干化/别名替换**（试过子串匹配：856 个包名里只有 44 个与某个卡名互为子串，且全是单复数/前缀那类平凡情形，未见系统变换）。

**「卡名」的四个本地来源，逐个说清可靠性**：

| 源 | 位置 | 条数 | 用它做什么 |
|---|---|---|---|
| **① 我们卡池** `cards_engine.json` | `MyGame/Assets/RuleEngine/Resources/` | 1126 | **主源**：给名字的 `faction` 与 `rarity`（甲乙两把尺子的分母） |
| **② 卡图资产名** | `assets_full/bundle_<阵营>cardassets_assets_all/Texture2D/*.png`（去掉 `40k_Cardframe*`） | 1194 | 只用来**扩名字集合**（当「认不认得出来」的判据）。名字切法：`<前缀>_<类别>_<卡名>`，**卡名里没有下划线** ⇒ 取 `split('_')[-1]` 即可（1194 条无一条失败） |
| ③ `card_ids.json` 的 value | `数据/游戏数据/`（PnP 编号体系，见 `V-ID` §3） | 996 | 只做交叉核（§5-③） |
| ④ PnP 卡图文件名 / 其它我们的表 | `d:/2/Warpforge部队卡片/*/*/Warpforge_*_*.png` 等 | —— | 并入 ① 的并集做「认不认得出来」判据，**不单独采信** |

⚠️ **① 与 ② 逐阵营重合 80%~97%**（UM 138/134/132 · GOF 123/119/119 · EC 74/70/66 …）⇒ **它们不是两个独立来源，是同一套卡名的两个副本**。所以「包名认得出」这件事**只有一把尺子**，不能算两票。

⚠️ **改名史**：本工程确有改卡名的历史（`zh_CN.csv` 精确查、PnP 19 条回滚等），**但它们都在我们自己的表里**，与「原版包名对不上现卡名」不是同一件事。**这一轮没有用任何别名表**。

---

## 5. 提取方法、读到的两条路、以及交叉核读数

### ① 路 A（按 `packTier` 钉）—— **实测证伪，不能用**

规则：「featured = 包里唯一那张稀有度 == `packTier+2` 的卡」。

- 覆盖：312 个包（= 4 个有 DT 掉落表的阵营：UM/GOF/EC/SW）。**306/312 的包最高稀有度正好 = `tier+2`**（另 6 条只低不高）⇒ 结构上很有说服力。
- 钉出 **264 个包 / 152 个 id**。
- **🔴 但 81 个 id 被钉到 ≥2 个互相打架的包名上**，例如：
  - `UM35` ← `Adepts of the Codex` [UM3(2),**UM35(3)**,UM16(2)] 与 `Codex Enforcers` [UM4(1),**UM35(3)**,UM23(1)] 两个 tier=1 的包**都只有 UM35 是稀有度 3**；
  - `EC39` ← `Agonising Resurrection` / `Death Defied`；`UM99` ← `Arts of Obscuration`×2 / `Obscured Destruction`×2 …
- 冲突不是数据错（已逐条查过：**329 个 id 的 DT 稀有度零冲突、60 个 id 出现在多张 DT 表里但稀有度一致**）⇒ **是规则错**：featured 卡不必是包里最高稀有度那张。
- ⇒ **路 A 的 264 条一条都不进 TSV。**

### ② 路 B（按「名字自己的稀有度」钉）—— **0 冲突，采它**

规则：包名 N 在我们池里稀有度已知 = R，且包内 3 个 id 的原版稀有度都查得到 ⇒ featured = 包里**唯一那张稀有度 == R 的卡**。

- 钉出 **30 条直接唯一，0 冲突**（每个 id 只对应一个包名）。
- 再加一轮**消元**（「一个 id 只能是一张卡、只可能被一个包命名」⇒ 已被别的包认领的 id 从候选里划掉）：**+3 条 ⇒ 33 条，仍 0 冲突**。这 3 条是 `GOF38`/`GOF65`/`GOF73`（TSV「来源」列标了 `(+消元)`），**且与包序无关**（它们的另一个候选都被「直接唯一」的包先认领了）。
- **在 3 条已知真值上的表现**：`UM34` = Point-Blank Shot ✔、`UM14` = Honour Guard ✔、`UM13` = Primaris Chaplain **判不了**（那个包 3 张卡稀有度全 3，平手）。**2/3 复现，第 3 条是平手而非判错。**
- 33 条 = **30 条直接唯一 + 3 条靠消元**。其中 **29 条**满足 `R == packTier+2`（两条独立估计互相印证）⇒ **置信度「高」**；**4 条** `R < tier+2`（`Deffkopta`/`Krump da Gitz`/`Mega Blasta Deffkopta`/`Stomp 'Em`）⇒ 只有一条来源，**置信度「中」**。

**为什么只能到 33 条 —— 天花板是两重叠加的**：① **DT 掉落表只有 4/13 个阵营**（UM/GOF/EC/SW，共 312 包），id 侧稀有度只对这 4 个阵营可得；② 这 312 包里**名字认得出的只有 42 个**。42 → 30（唯一解）→ 33（消元）。**这就是「扩到 ~880」不成立的地方。**

### ③ 与 `card_ids.json` 的交叉核 —— **0 / 48，与 `V-ID` 同向**

| 检验 | 读数 |
|---|---|
| 48 条 `(原版id, 卡名)` 真值里，`card_ids.json[id]` 与卡名一致的 | **0 / 48 = 0%** |
| 反向：`card_ids.json` 给这个**卡名**发的 id（去重后）与真值 id 相同的 | **0 / 48 = 0%** |
| 例 | `GOF1`：真名 `Grot`，我们表写 `Grukk Face-Rippa`（督军）；而我们表把 `Grot` 发给了 `GOF4`——**`GOF4` 的真名是 `Slugga Boy`** |

⇒ **`V-ID` 的 0/17 推广到 0/48，方向完全一致**：两套命名空间整体错位，不是「差一个常数」（`V-ID` §6 已排除偏移）。**本轮只统计，一个 id 都没改。**

---

## 6. 别的采集路（逐条：在哪 · 能采多少 · 值不值得做）

| # | 路 | 在哪 | 能采多少 | 评 |
|---|---|---|---|---|
| 1 | **DT 掉落表 × 教程关（现有的 17 条）** | `bundle_cosmeticsso_assets_all/MonoBehaviour/DT *.json` 的 `obtainableItems[].targetId` + `…{genericObjectReference,reference}`(pid) × `bundle_tutorialso_assets_all/…/Warpforge_TutorialStage{1..6}.json` 的 `turnScriptedData[].scriptedActions[].actionType`（**字面就是卡名**，形如 `PlayCard (Bladeguard Lieutenant)`）与 `.scriptedActionData[].actingUnit`(pid) | **17**（教程 107 行 PlayCard / 75 个互异 pid ∩ DT 329 个 pid） | ✅ **已采**。**天花板卡在 pid 桥**：教程只给了 75 个 pid 的名字，DT 只有 329 个 pid→id。想扩就得找**第三个同时给「pid + 名字」的表** —— 本轮**没找到**（见下 5、7） |
| 2 | **抽卡包**（本轮） | 见 §2 | **+33**（新） | ✅ **已采**。天花板见 §5-②|
| 3 | **`bundle_uiwarlords_assets_all/Texture2D/`** | 65 张督军像，名如 `UI_Deck_Warlord_Uriel Ventris.png` | **65 个「督军名」**，但**没有 id** | ⚠️ **只能当名字表**。要和 `warlord_ids.json` 的 57 个 key（= 236 副预组牌 `heroId` 去重，`V-ID` 已证 key 侧是原版命名空间）配对，就得解一个**多对多指派**（同阵营 5 个 id ↔ 5 个名字）—— **是 CSP，不是采集**。**建议做**，但要做好「先定规矩再解」 |
| 4 | **预组牌 SO 的 `deckHero.targetId` + SO 资产名尾段** | `bundle_prebuiltdecks_assets_all/MonoBehaviour/*.json`（236 份）。SO 名如 `Ultramarines_Deck0_Tutorial6_Uriel`，同文件 `deckHero.targetId="UM47"` | **~63 条督军**（名字只是「含这个词」的近似：`Uriel` 而非 `Uriel Ventris`） | ⚠️ **近似真值**。`V-ID` §8-5 已记「`deckName` 不是 heroId 的函数，不能拿来推 id」，但 **SO 资产名这条路它没否** —— 值得单独核一遍 |
| 5 | **`decklists.json` 的 `heroPortrait`** | `d:/4/Unity/数据/游戏数据/decklists.json` | **0（不能用）** | ⛔ **顺手发现**：`AM1/AM2/AM3/AM66` **四个不同的督军 id 全指向同一张 `UI_Deck_Warlord_Ursula Creed.png`** ⇒ 它是**按阵营**填的，不是按督军。**别拿它当 id→名 的来源** |
| 6 | **成就 `ACH*` / 战役节点 `Campaign Node Data`** | `bundle_staticgeneralassets_assets_all/MonoBehaviour/ACH*.json` · `bundle_menus_assets_all/…/Campaign Node Data_*.json` | **0** | ⛔ ACH 的 `targetId` **全是空串**（只有 10 个奖励槽的壳）；战役节点的 `targetId` 指向的是 **DT 表 / Booster Pack**，不是卡。**不是采集路** |
| 7 | **卡图资产名（1194 条）· 卡动画 SO 名 · AudioClip 名** | `bundle_<阵营>cardassets_assets_all/Texture2D/*` · `bundle_<阵营>cardanims_assets_all/MonoBehaviour/*`（`m_Name`，形如 `UM_InspiredRetribution` / `Goff_Da_Irongob` / `Krump Da Gitz effect`） · 同包 `AudioClip/` | **只有名字，没有 id** | ⚠️ **是这一轮最大的副产品**：① 卡图名给了 **1194 条卡名 + `inf/veh/strat/offensive/defence/war/dae` 类别**（我们以前只用过一部分）；② 卡动画 SO 名把**卡名与「技能名」混在一起**，可用它把 `banner`/技能名从卡名里剔出去。**都和 id 无关**，但能让「认不认得出包名」的判据更准 |
| 8 | **`allcards_assets_all.bundle`（真正的卡定义表）** | **远端 CCD**。已核实：`C:/Users/qjh36/AppData/LocalLow/Everguild/Warpforge/com.unity.addressables/catalog_main.json` 里它的内部 id 是 `https://d03469d2-….client-api.unity3dusercontent.com/.../allcards_assets_all.bundle`，**不是本地路径** | **全量** | 🔴 **这才是唯一能一次性解掉整条账的路**。本地 catalog 的 10490 个 internal id **全是 bundle 路径**，不含任何卡名/卡 id。**建议：作为「等哪天能拿到远端包」的挂起项** |
| 9 | 老 Godot 仓 `d:/warpforge/` · `d:/2/Warpforge_tools/data/card_ids/` | —— | —— | ⛔ **顺手发现**：`d:/2/Warpforge_tools/data/card_ids/{card_ids,decklists,prebuilt_decks_full,warlord_ids,deck_btn_map}.json` **与我们 `d:/4/Unity/数据/游戏数据/` 下的同名文件是同一批**（`card_ids.json` 亦为 996 条 PnP 编号）⇒ **不是新来源**，别重复采。`deck_btn_map.json` 是「卡组选择按钮 sprite 名 → 卡名」，**没有 id** |

---

## 7. 产出的 TSV

- **文件**：`资料/普查产出_1018/V-PACK_原版id到卡名.tsv`（**TSV · 纯 LF · `#` 开头 5 行表头说明**）
- **行数**：**48 张对子**（33 条抽卡包新采 + 17 条 DT×教程旧采，其中 `UM14`/`UM34` 两条同时被两路命中 ⇒ 48 = 33+17−2 重叠）
- **列**：`原版id` · `卡名(原版给的)` · `我们池里对上的卡` · `我们表给这个卡名的 id` · `来源(资产/字段)` · `置信度`
- **置信度**：**高 44 条 · 中 4 条**（4 条「中」= 我们池给的名字稀有度与 `packTier+2` 打架的那 4 个包）
- **覆盖**：按 id 数 48 / 1126 ≈ **4.3%**；按「原版侧出现在抽卡包里的 id」48 / 904 ≈ **5.3%**。
- 🔴 **这不是正式数据、别直接引用、更别拿去回填 `card_ids.json`** —— 它只是「采集面有多大」的读数，**中间隔着 §5-② 那条单腿推理**。回填的正确前置条件仍是「拿到全量真值表」（`V-ID` §8-1）。

**张数指针**：本表张数只在 TSV 头部与本节各写一次，**别在别处抄第二份**（铁律 6）。

---

## 8. 没查清的部分（⛔ 不拿猜测填空）

1. 🔴 **C 类 664 个包名到底是不是卡名 —— 没查清**。两边证据打架（§3-③）。**「抽卡包是过期快照」这条解释我没有判据**。
2. 🔴 **「featured 到底是包里哪一张」没有通用判定法**。路 A 证伪（81 冲突）；路 B 只覆盖 42 个包。**12 个月 9 个平手包**判不了。
3. ⚠️ **EC / SW 两个阵营的包名与卡名 0 重合** —— 是「新阵营的包名另起了一套」还有别的缘故，**没查**（它们是 42/47 个包，占 882 的 10%）。
4. ⚠️ **甲乙两把尺子的分母只有 215**，其中 **UM 占 15、GOF 占 27**，另 11 个阵营**合起来只有 173 条**。所以「所有阵营都成立」这句话**没有逐阵营证据**，只能说「没有反例、量级一致」。
5. ⚠️ **`packTier` 的语义**：我实测出「`tier+2` = 包内最高稀有度」（306/312）与「= 包里名字那张卡的稀有度」（190/215），**但这两个说法哪个是原版的设计意图、还是都不是**，反编译里 **`PrebuiltPack` 只留了字段、方法体是空的**（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/PrebuiltPack.cs` 的 `GetCardLibrary/IsValid/GetLocalizedName` 全是空壳）⇒ **读不到**。
6. ⚠️ **`packEnabled=0` 的那 10 个包**是什么（下线的活动包？），**没查**。
7. ⚠️ **26 组重名包里，重名的两个 `cardIds` 逐字相同**（例：`Arts of Obscuration` 两份都是 `UM91,UM114,UM99`）⇒ 我判断是**重复资产**，**但没有查它们是不是不同 `packId`/不同来源**。

---

## 9. 我搜过的目录与关键词（🔴「全仓没有 X」必附）

**搜过的目录**（全部只读）：
`d:/2/新解包资源/assets_full/` —— **90 个 bundle 逐个列过**；逐字段读了 `bundle_draftpacks_assets_all/MonoBehaviour`（**882 份全读**）· `bundle_cosmeticsso_assets_all/MonoBehaviour`（DT 全族 33 份）· `bundle_duplicateassetisolationso_assets_all`（15 份）· `bundle_prebuiltdecks_assets_all`（236 份，字段级）· `bundle_tutorialso_assets_all` · `bundle_staticgeneralassets_assets_all/MonoBehaviour`（266 份，含 102 个 ACH）· `bundle_menus_assets_all`（Campaign Node Data）· `bundle_uiwarlords_assets_all/Texture2D`（65）· **13 个 `*cardassets_assets_all/Texture2D`（1194 张卡图名）** · **13 个 `*cardanims_assets_all/MonoBehaviour`（`m_Name` 全列）** · `bundle_cardanimsgeneral_assets_all`（571 条动画名） · `bundle_boosterpacks_assets_all`；
`d:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/`（84 个 bundle 全列）· `d:/2/Warpforge_code/Scripts/Assembly-CSharp/`（`PrebuiltPack.cs`/`PackTier.cs`/`PrebuiltPackUI.cs`/`BoosterPackData.cs`/`CardInBoosterPack.cs`/`DraftMode*` 等类桩）· `d:/2/tools/decomp_full/`（grep `draftpack`）· `d:/2/tools/all_strings.txt` · `d:/2/Warpforge_tools/data/`（含 `card_ids/`）· `d:/2/Warpforge部队卡片/`；
`C:/Users/qjh36/AppData/LocalLow/Everguild/Warpforge/`（`com.unity.addressables/catalog_main.json` 逐字段解过 · `Player.log` · `PlayerDecks39.dat` 只看了名字）· `C:/Users/qjh36/AppData/LocalLow/341aacf…`（308K，空）；
`d:/4/Unity/{MyGame/Assets/RuleEngine/Resources, 数据/游戏数据, 资料, 工具}`。

**搜过的关键词**：
`draftpack` · `PrebuiltPack` · `PackTier` · `packTier` · `packArmy` · `packName` · `nameRefId` · `cardIds` · `cardLibraryIds` · `deckHero` · `targetId` · `obtainableItems` · `cardRarity` · `heroPortrait` · `allcards` · `PlayCard (` · `UM34` · `UM23` · `GOF23` · `Point-Blank` · `Bladeguard` · `Avenging Vow` · `Apex Warrior` · `Atomantic Generators` · `Air Strike` · `Assault Powerhouse` · `1st Company` · `Krump da Gitz` · `Towa of Deztruction`。

**明确说「没有」的三条（附搜法）**：
1. **`allcards_assets_all.bundle` 本地没有**（复现 `V-ID` 的边界声明）：`ls d:/2/新解包资源/assets_full/`（90 项，无）· `ls d:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/`（84 项，无）· 在 `catalog_main.json` 里它是**唯一的 remote URL 形态**。
2. **本地没有任何资产同时带「卡 id」与「卡名」**：`grep -rl '"targetId"'` 全仓只有 6 个 bundle 有，**卡 id 形态的取值只出现在 DT 表（329 个）与预组牌 `deckHero`（63 个）**；`grep -rl 'cardIds\|cardLibraryIds\|deckHero'` 只有 `draftpacks`(882) 与 `prebuiltdecks`(236)。`d:/2/tools/all_strings.txt` 里 `UM34`/`GOF23`/`Point-Blank` **0 命中**。
3. **C 类 664 个包名在 `d:/2`+`d:/4` 全仓只出现在抽卡包 JSON 里**：搜法 = 对 `Avenging Vow`/`Apex Warrior`/`Atomantic Generators`/`Air Strike`/`Assault Powerhouse`/`1st Company` 逐个 `grep -ril <词> d:/2/新解包资源/assets_full/`，**每个都只命中它自己那份 `bundle_draftpacks_assets_all/MonoBehaviour/<名字>.json`**。

**复算脚本**（落在 `d:/tmp/wf_pack/`，**不在仓库**）：`truth.py`（17 条真值 × 包）· `main2.py`（最高稀有度 vs tier）· `main4.py`（阵营尺）· `main9.py`（路 A vs 路 B）· `extract2.py`（消元）· `mkcsv.py`（出 TSV）· `perarmy.py`·`artpool.py`（阵营名覆盖）。
