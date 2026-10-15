# R8 · `card_stats.json` 两列审计（`armor` / `art`）

> 只读审计 · 未改任何文件 · 未跑 Unity · 未动 git。
> 判据 = **PnP 成品卡图**（`d:/2/Warpforge部队卡片/…`，铁律 7）+ 池侧 `cards_engine.json`（`armour` 列）+ 解包原件（`d:/2/新解包资源/assets_full/bundle_*cardassets*_assets_all/Texture2D/`）。
> 被审文件：`d:/4/Unity/数据/游戏数据/card_stats.json`（1,172,159 B · **CRLF** · 1213 条记录）。
> 行号一律指**该文件**（记录起始 `{` 行 / 具体字段行，两种都标了）。

---

## 一、`armor` 列 —— **结论：该列 6 个非空值全部错，值都等于卡面「紫圈（远程）」**

### 1. 全表盘点
- `armor` 为 `null`：**1207 / 1213**。非空：**6 行**，取值分布 `2×3 · 4×2 · 6×1`。
- `type=="hero"`（= 督军，`subtype` 全是 `Warlord`）：**58 行**（含 4 行残渣，见 §三·顺手发现 3）；池侧（`cards_engine.json`）督军 **56**。
- 督军里 `armor>0` 的：**只有 2 行**（`Commissar Elan`、`Acolyte Iconward`），值都是 `2`。
  ⇒ 上一趟代理说的「`armour>0` 的 62 张全是部队」**不是这份文件**（`d:/4/Unity/数据/游戏数据/card_stats.json` 的 `armor>0` 只有 6 行，其中 2 行是督军）；那个 62 是池侧 `cards_engine.json` 的 `armour==1` 计数（该列 >0 共 81 行，确实全是 `type=="unit"`）。

### 2. 逐张（**6/6 都开过 PnP 成品卡图**）
| # | card_stats 行 | 卡名 | faction | `armor` / `ranged_attack` | PnP 卡面我看到的（红剑圈 / 紫枪圈 / 盾 / 绿框） | 判定 | 出处 |
|---|---|---|---|---|---|---|---|
| 1 | 记录 `:5280` · `armor` `:5322` · `ranged_attack` `:5321` | Commissar Elan | AstraMilitarum | `2` / `null` | **2 / 2 / 无盾 / 30** | **错**：值=紫圈；护甲应 `null`（池 `armour`=0），`ranged` 应 `2` | `Astra Militarum/1督军/Warpforge_01_Commissar-Denkler.png` |
| 2 | `:15136` · `:15182` · `:15181` | Acolyte Iconward | Genestealers | `2` / `null` | **2 / 2 / 无盾 / 35** | **错**，同上 | `Genestealer Cult/1督军/Warpforge_01_Iconward-Malak-Vorenth.png` |
| 3 | `:19829` · `:19847` · `:19846` | Veteran Flyboy | Goff | `4` / `null` | **7 / 4 / 无盾 / 6** | **错**：值=紫圈；护甲应 `null`（池 0），`ranged` 应 `4` | `Orks/3部队/Warpforge_31_Veteran-Stormboy.png` |
| 4 | `:22369` · `:22392` · `:22391` | Simulacrum Celestian | Sororitas | `4` / **`4`** | **5 / 4 / 无盾 / 6** | **错**：护甲应 `null`（池 0）；`armor` 与 `ranged_attack` **同一个数写了两遍** | `Sorotitas/3部队/Warpforge_30_Simulacrum-Celestian.png` |
| 5 | `:31374` · `:31394` · `:31393` | 2nd Company Terminator | Ultramarines | `6` / `null` | **5 / 6 / 盾 1 / 6** | **错**：值=紫圈；护甲真值 **1**（池 `armour`=1），`ranged` 应 `6` | `Ultramarines/3部队/Warpforge_19_1st-Company-Terminator.png` |
| 6 | `:31584` · `:31604` · `:31603` | Bladeguard Lieutenant | Ultramarines | `2` / `null` | **6 / 2 / 盾 1 / 6** | **错**：值=紫圈；护甲真值 **1**（池 `armour`=1），`ranged` 应 `2` | `Ultramarines/3部队/Warpforge_34_Bladeguard-Veteran.png` |

**结论：6 张里 6 张判为错，0 张判不了。** 规律：`card_stats.armor` = 卡面**紫圈（远程）**的值（6/6 命中），**与「护甲」无关**。

### 3. 与池侧 `cards_engine.json` 的 `armour` 列对照
| 卡 | card_stats.armor | 池 `armour` | 卡面盾 |
|---|---|---|---|
| Commissar Elan / Acolyte Iconward / Veteran Flyboy / Simulacrum Celestian | 2 / 2 / 4 / 4 | **0 / 0 / 0 / 0** | 无盾 ✅ 与池一致 |
| 2nd Company Terminator / Bladeguard Lieutenant | 6 / 2 | **1 / 1** | 盾 1 ✅ 与池一致 |
⇒ **6/6 与池不一致；池侧 6/6 与卡面一致。**

### 4. 反向证据（说明这一列不是「护甲」）
- 全表带 `Armour N` 关键词的行 **29 条**，其中 **28 条 `armor` 是 `null`**；唯一一条有值的（`Bladeguard Lieutenant`）**值还是错的**。
- 6 行里有 **5 行根本没有 `Armour` 关键词**。
- 全仓 **没有任何代码读 `card_stats.json` 的 `armor` 字段**（9 个读该文件的 py 里，`grep -c armor` = `gen_cards_engine.py` 2 处**全是注释**、`gen_cardface_fixes.py` 1、`gen_icon_plan.py` 5 处都是关键词文本/图标别名；其余 6 个 = 0）⇒ 这一列是**死数据**，错着也不影响产物 —— 但**读它的人会被误导**。

### 5. 督军「不印银盾」——抽样（**不是全量**）
另开 **12 张**督军成品卡图（Lord Solar Leontus · Ghazghkull Thraka · Morvenn Vahl · Marneus Calgar · Ramatekh the Cruel · Shadowsun · Logan Grimnar · Abaddon · Azrael · Lucius · Jain Zar · Swarmlord）：
底部一排四个位置全是 **红剑圈 · 紫枪圈 · 宝石菱形 · 绿框生命**，**12/12 无银盾**（连上 §一·2 的 2 张 = **14/14 无银盾**）。
旁证（独立来源）：池里 **56 个督军的 `armour` 全是 0**。
⇒ 「督军卡面不印银盾」这个**规律方向成立**（14 张抽样 + 56 张池值），但**我没有逐张开满 58 张**。

### 6. ⚠️ 这一条**已知**，且引擎层**已经补掉了**
`d:/4/Unity/工具/gen_cards_engine.py:396-397` 白纸黑字写着：
> 「这 4 张的共同根因：**紫圈（远程）被 OCR 读成了 `armor`，`ranged_attack` 留空** …… 判据是 `card_stats.json` 里 `ranged_attack=null 且 armor=N`」，
并**逐张列了 4 张的卡图与我读到的红/紫/绿值**（与上表 1~4 逐一吻合）。

`STAT_FIXES` 里已给 6 张都补了 `ranged`：`:407 Commissar Elan` · `:408 Acolyte Iconward` · `:439 Veteran Flyboy` · `:278 2nd Company Terminator` · `:317 Bladeguard Lieutenant` · `Simulacrum Celestian` 是**直接改写源表**（该表 `:409` 注释记着这件事）。
⇒ **缺陷本身是既知、且已在引擎侧绕开**；但 **① 源表 `card_stats.json` 的 `armor` 6 行仍是错的、② 5 行的 `ranged_attack` 仍是 `null`（只在下游补）**。谁直接读源表就会拿到错的三个数。

### 7. ⚠️ 改法叮嘱（本报告**只报不改**）
`card_stats.json` 是 **CRLF**（36003 CRLF / 36003 LF）、**手工缩进**（记录 2 空格 / 字段 3 空格 / `rarity_gem` 内嵌套 4 空格）。
⇒ **不能用 `json.dumps(..., indent=1)` 整篇重写**（会把缩进、空行、行尾全翻掉，`git diff --numstat` 会变成整篇）。要改就走 **Edit 逐行**或 **python `wb` 保持 CRLF**。

---

## 二、`art` 列 —— **结论：真错配 4 条（含已知 1 条）；另有 21 条 art 为空**

### 1. 全表盘点
- `art` 有值 **1192**；空串 **15** + `null` **6** = **21 条为空**。
- 按 `.../art/<阵营目录>/...` 的**目录段**与记录自身 `faction` 比：**不一致 14 条**
  = **10 条 noise 占位行**（`Normal` / `Normal Conditions`，`noise:true` 且 `hasStats:false`，**共用** UM / Sororitas 的环境占位图 ⇒ 不是错配，是设计）
  + **4 条真错配**。
- **`art` 文件名在解包资源里 0 条缺失**：对 13 个 `bundle_*cardassets*_assets_all/Texture2D/` 建索引（**1413 个名字**）后逐条比对，`1213 − 21 = 1192` 条全部命中 ⇒ **不存在「指到不存在的文件」这一类**。
- 13 个 cardassets 包之间**文件名 0 重名** ⇒ 「目录段」这条判据可靠（重名只出现在 cardassets 包与 UI/字体包之间，与卡面立绘无关）。

### 2. 4 条真错配（**逐条开 PnP 成品卡图 + 同阵营原件并排核过**）
| # | `art` 行 | 卡名 | faction | 现值（错） | **应改为** | 判据 |
|---|---|---|---|---|---|---|
| 1 | `:8031`（记录 `:8025`） | **Aggressor** | **DarkAngels** | `…/SpaceWolves_太空野狼/SM_SpaceWolves_inf_Aggressor.png` | `res://assets/cards/art/DarkAngels_暗黑天使/DarkAngels_inf_Aggressor.png` | PnP `Dark Angels/3部队/Warpforge_12_Aggressor.png` 画的是**绿甲** DA 星际战士（=DA 原件），现值那张是**蓝灰 SW**；两原件 md5 不同（`22c01a547e` vs `920b6f65fe`） |
| 2 | `:11307`（记录 `:11301`） | **Terminator** | **EmperorsChildren** | `…/Ultramarines_极限战士/SM_UM_inf_Terminator.png` | `res://assets/cards/art/EmperorsChildren_帝皇之子/CSM_EmperorsChildren_inf_Terminator.png` | PnP `Emperor_s Children/3部队/Warpforge_22_Terminator.png` 是**粉紫**混沌终结者；现值是蓝色忠诚派。**= R3 已知那例（行号 `11307` 精确吻合）** |
| 3 | `:6470`（记录 `:6464`） | **Terminator Champion** | **BlackLegion** | `…/EmperorsChildren_帝皇之子/CSM_EmperorsChildren_inf_Terminator Champion.png` | `res://assets/cards/art/BlackLegion_黑色军团/CSM_BlackLegion_inf_Terminator Champion.png` | PnP `Chaos/3部队/Warpforge_44_Terminator-Champion.png` 是**黑金** BL 终结者（**另一张卡**：atk7/hp8 vs EC 的 atk6/hp6） |
| 4 | `:7788`（记录 `:7782`） | **Maulerfiend** | **BlackLegion** | `…/EmperorsChildren_帝皇之子/CSM_EmperorsChildren_veh_Maulerfiend.png` | `res://assets/cards/art/BlackLegion_黑色军团/CSM_BlackLegion_veh_Maulerfiend.png` | PnP `Chaos/3部队/Warpforge_46_Maulerfiend.png` 是**暗红/铁锈**色；EC 那张是**亮粉** |

**三个「同一张 art 被两张不同阵营的卡共用」**（= 上表 2/3/4，机器可查、`grep` 一下就能复现）：
`SM_UM_inf_Terminator.png`（`:11301` EC + `:32822` UM）· `CSM_EmperorsChildren_inf_Terminator Champion.png`（`:6464` BL + `:11269` EC）· `CSM_EmperorsChildren_veh_Maulerfiend.png`（`:7782` BL + `:12533` EC）。
第 1 条（Aggressor）不属这一类 —— 它是**单侧占用了别阵营的图**（SW 自己那张 `Aggressor` 不在池里）。

### 3. 抽查：4 个「应改到」的原件**都在本地且可读**
```
OK  761,297 B  bundle_spacemarinesdarkangelscardassets_assets_all/Texture2D/DarkAngels_inf_Aggressor.png
OK  872,435 B  bundle_chaosspacemarinesemperorschildrencardassets_assets_all/Texture2D/CSM_EmperorsChildren_inf_Terminator.png
OK  946,702 B  bundle_chaosspacemarinesblacklegioncardassets_assets_all/Texture2D/CSM_BlackLegion_inf_Terminator Champion.png
OK 1,127,173 B bundle_chaosspacemarinesblacklegioncardassets_assets_all/Texture2D/CSM_BlackLegion_veh_Maulerfiend.png
```
⇒ 4 条都**有现成的正确原件**，不是「本地没有」。

### 4. 21 条 `art` 为空（**都在池里、不是 noise**）
行号 `:35421 → :35974`（连续尾块），全部 `cards_engine.json` 里**有这张卡**。举例：`Lord Commander` · `Dark Pact of Fate/Blood/Excess/Resilience` · `Master of Repentance` · **`Lord Kaphrael`（督军）** · `Veldras the Sublime` · `Armoury of Excess` · `Decadent Throne` · `Undying Legions` · `Moment of Grace` · `Exemplary Warrior` · **`Predator Annihilator`** · `Hellfire Torch/Pit` · `Rusted Vent` · `Awakened Obelisk` · `Protective Bio-structure` · **`Fire Warrior Marksman`** · `Chosen of the Four`。
抽查 **`Predator Annihilator` / `Fire Warrior Marksman` / `Veldras the Sublime` / `Lord Kaphrael` / `Hellfire Torch` / `Protective Bio-structure` / `Chosen of the Four` / `Moment of Grace` / `Exemplary Warrior`** 在 `assets_full` **全树**搜过（不只看 `Texture2D/`）：**只有语音 / 头像 / 草稿包 MonoBehaviour，没有卡面立绘原件** ⇒ 属**铁律 11 例外①「本地原件不存在」**，不是错配。⚠️ 我的检索是**按卡名**做的，若原版资产名与本池卡名差得远，这条会漏。

---

## 三、没查清 / 判不了的
1. **`art` 的「同阵营内指错卡」没查**（目录段对、但画的其实是同阵营另一张卡）。1213 行只能逐张并排，本轮不可行。
   只做了一条**弱判据**（art 文件名是否包含卡名/`ocrName`）：**6 行不包含**，且**全在同阵营内**、看着都是原版命名差异 ⇒ 不报为缺陷：
   `Sylar Hexscorn`→`…Sylar Hexcorn` · `Sergeant Taaman`→`…Sergeant Naaman` · `Simulacrum Celestian`→`…Simulacrum Imperialis` · `Sister Repentia`→`…Sisters Repentia` · `Telepathic Domination`→`…Telephatic Domination` · `Experimental Drones`→`…Experimental Drone`。
2. **督军 58 张卡面只开了 14 张** ⇒ 「督军不印银盾」是**抽样结论**，不是全量。（独立旁证另有一条：池里 56 督军 `armour` 全 0。）
3. **Aggressor 那条的「数字版实际用哪张」没核**。我判「应为 DA 原件」的依据是 PnP 卡面（绿甲 DA）+ 同阵营原件存在；按铁律 7 这够，但**没进原版游戏对过**。
4. **21 条空 `art` 是「本地没有」还是「检索词没对上」** —— 见 §二·4 末句，我按卡名搜的，**判不了**这一层。

---

## 四、顺手发现（不在本次两列范围内，但值得记账）
1. **那 6 行的 `ranged_attack` 丢了 5 个**：`:5280` / `:15136` / `:19829` / `:31374` / `:31584` 的 `ranged_attack` 都是 `null`（真值 2 / 2 / 4 / 6 / 2）。第 6 行 `Simulacrum Celestian` 例外 —— `armor` 与 `ranged_attack` **都是 4**（同一个数写了两遍）。
   ⇒ 源表这一处的修法是「值搬了家」：**紫圈的值被写进 `armor`，`ranged_attack` 留空**。
2. **这 6 行的 `ocrName` 与 `name` 全都不一致**，且 `ocrSrc` / `face` 都是空（`ocrSrc` 空的行全表 92 条，其中 `ocrName != name` 的**只有 6 条 —— 就是这 6 条中的 5 条 + `Sporecaster Biostructure`**）⇒ 它们**不是从 PnP OCR 那条路进来的**。PnP 印的名字分别是 Denkler / Iconward Malak Vorenth / Veteran Stormboy / Simulacrum Celestian / 1st Company Terminator / Bladeguard **Veteran**。
   ⚠️ **别照 PnP 改名** —— `工具/gen_cards_engine.py:84-87,183-184,295-305` 有一整套「改名前的名字会变死键」的教训（`1st Company Terminator` / `Simulacrum Imperialis` / `Bladeguard Veteran` 三条都已裁「不按 PnP 改名」）。
3. **`card_stats` 有 4 行督军的 `name` 是坏的**：`v2`（`:8002`，DarkAngels，`noise:true`）· `HB`（`:17626`）· `stormlord`（`:17656`）· `Diviner`（`:17734`）—— 后三行 `ocrName` 写着 `Imotekh the Stormlord` / `Orikan the Diviner`，而池里那两张督军卡的**名字就是** `Imotekh the Stormlord` / `Orikan the Diviner` ⇒ 这 4 行的 `name` 字段与 `ocrName` 脱节（`HB`/`stormlord` 两行 `noise` 还不一致：一行 `true` 一行 `null`）。
4. **`card_stats` 1213 条 vs 池 1127 张**：差出来的多是 `environment` / 占位行（`Normal Conditions` ×9、`Normal` ×3、`Rusted Vent` ×2 …）。
5. 文件形态：**CRLF**、手工 1/2/3 空格缩进、`rarity_gem` 里嵌套一层（4 空格）⇒ **⛔ 别 `json.dumps` 整篇重写**（见 §一·7）。
