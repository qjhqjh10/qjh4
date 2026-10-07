# V-BARE · 「裸数字」（图标被剥）全池普查

> 只读普查。**一个候选都没改**。临时脚本 / 裁块截图全在 `d:/tmp/wf_bare/`（不在仓库）。
> 扫描时点：2026-10-08 00:09–00:30。⚠️ 期间**卡池文件被另一个写手改过**（见 §7.1）。

---

## 1. 一句话结论

**全池 1126 张扫完。`desc` 里 `±N` 出现 397 处；其中「数字后面没有属性词、也没有方括号记号」（= 图标被剥掉）的有 20 处 / 19 张卡。
再加上两个没带正负号的同类（`Gain 1` / `gains 2`），是 22 处 / 21 张。
这 21 张全部逐张开成品卡图**读出了那一枚图标**（0 张判不了）：
18 张 = 粉圈白拳（近战 `Attack`）· 1 张 = 紫圈白枪（远程 `Ranged`）· 2 张 = 锯齿环形徽记（任务点 `Quest Point`）。**

**已修那 5 张之外，新清单 = 21 张。**（UM85 在本次扫描**进行到一半时**被另一个写手修掉了，见 §7.1 —— 所以它不在我这 21 张里，也不是漏扫。）

| 分组 | 张数 | 读出的图标 |
|---|---|---|
| `+N` 裸数字 → 粉圈白拳（**近战**） | **18** | 与 `Genestealer Familiar` 逐像素同形（NCC 0.57–0.98，对面那枚枪只 0.35–0.51） |
| `+N` 裸数字 → 紫圈白枪（**远程**） | **1** | `UM_Angels_of_Death`（NCC 枪 0.708 / 拳 0.428） |
| `Gain N` / `gains N` 裸数字 → 锯齿环形徽记（**任务点**） | **2** | 与 `March of Vengeance` 的 `Gain 2` 同形 |

---

## 2. 我的扫描口径（可复现）

### 2.1 数据源
`d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json`（**1126 张**，`count` 与 `cards[]` 长度一致）。
⚠️ 任务书里写的路径是 `d:/4/Unity/Assets/...`，**实际在 `d:/4/Unity/MyGame/Assets/...`**。

### 2.2 筛法（两遍，脚本 `d:/tmp/wf_bare/scan_cand2.py` 与 `scan_unsigned.py`）
1. **带符号**：正则 `([+-])\s?(\d+)` 抓**全部** 397 处；对每一处，取**紧跟其后的第一个 token**：
   - 是字母词且 ∈ 属性词表 ⇒ **不是候选**（图标没丢）；
   - 是 `[` ⇒ 记进**方括号族**（记号在，不算被剥）—— 54 处；
   - 是标点 / 词尾 / emoji ⇒ **候选**。
2. **不带符号**：正则 `\b(Gain|gain|Give|give|Heal|heal|have|has|gains|get|gets|receives?)\s+(\d+)\b` 抓 101 处，
   逐个看后一个 token 是否属性词 / 资源词 / 正常量词 ⇒ **捞到 2 处真裸数字**（`DA11 Slay: Gain 1` · `DA86 gains 2`）。
3. **反向兜底**：把所有 desc 里**每一个数字**的「前一个词 / 后一个词」都打印出来（117 组），逐组看有没有漏 ——
   剩下的全是 `Shuriken N` / `Vulnerable N` / `Tide N` / `N [Spirit Stone]` / `N ☀` / `Oath N:` / `Heal N` 这类**关键词带名**的，不是裸数字。

**属性词表**（我定的，见 §9 全文）：
`melee / ranged / attack / armour / armor / health / damage / wounds / cost / power / toughness / movement / move / speed / shield / energy / weapon / strength / fist / might`。

**排除项**：`Deal 3-5 damage` 这种**伤害区间**（`-` 是区间号不是减号），已在脚本里显式排掉。

### 2.3 怎么判那一枚图标（**照 `S9_GOF四张图标.md` §2 的姿势，并加了一道客观对拍**）
1. **裁块放大**：把 900×1200 卡图的**效果文字框**裁出来（`crop(0.19w, 0.55h, 0.83w, 0.90h)`）再放大 2×，逐张读。
2. **与两枚已知参照并排比**（同一裁法、同一放大倍率）：
   - **近战参照** = `Genestealer Cult/3部队/Warpforge_07_Genestealer-Familiar.png` 的 `Adjacent units have +1 ⟨粉圈白拳⟩`
     （本表早就写 `+1 Attack`）；
   - **远程参照** = `Tau/3部队/Warpforge_35_Cadre-Fireblade.png` 的 `troops have +2 ⟨紫圈白枪⟩`（本表写 `+2 Ranged Attack`）。
3. 🆕 **归一化互相关（NCC）模板匹配**（脚本 `d:/tmp/wf_bare/run_match.py`）—— 把两枚参照图标**切成模板**，
   在每张候选卡的文字框里滑窗找最相似的位置：
   - 模板盒（900×1200 原图坐标）：近战 `(636,803)-(692,859)` · 远程 `(612,894)-(680,962)` · 任务点 `(462,891)-(518,947)`；
   - **自检**：近战模板去匹近战参照卡 = **1.000**（匹远程参照卡只有 **0.506**）；
     远程模板去匹远程参照卡 = **1.000**（匹近战参照卡只有 **0.446**）⇒ **两个模板能分开**；
   - 多尺度 0.7–1.25×（因为卡面图标随文字大小变），粗到细搜索。
4. **旁证（不是权威）**：`descZh` / 官方中文串表 `d:/warpforge/data/i18n/zh_CN.csv`。
   ⚠️ 实测**它自己也常常是裸的**、而且**同一个英文串在不同条目里译得不一样**（详见 §3 与 §7.5）⇒ 只用它当旁证。

### 2.4 成本
扫全池（1126 张、两遍正则 + 反向兜底）约 1 分钟；逐张裁块 22 张约 1 分钟；模板匹配 22 张 × 2–3 个模板约 6 分钟。
**没有跑 Unity、没有跑生成器、没有动 git。**

---

## 3. 候选清单（21 张 · 逐张）

> 「NCC」= 归一化互相关最高分（拳 / 枪）。自检基准：近战参照卡 **1.000 / 0.506**，远程参照卡 **0.446 / 1.000**。
> 行尾的「旁证」列只作参考，**不是判据**。

### 3.A 粉圈白拳 = **近战 `Attack`**（18 张）

| # | 卡 id | 卡名 · 阵营 | `desc` 原文（粗体 = 裸的那一处） | 卡图（`d:/2/Warpforge部队卡片/…`） | 卡面那枚图标 | 我判它是 | 凭什么 | NCC 拳/枪 | 旁证 | 置信 |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `ASH79` | Howling Banshee Exarch · Aeldari | `Waystone. Camouflage. Strike: Give `**`+1`**` to your units` | `Aeldari/3部队/Warpforge_06_Howling-Banshee-Exarch.png` | 粉圈白拳 | 近战 | 与参照 A 同形；NCC 差 0.49 | **0.931 / 0.441** | 中文「+1 **近战攻击**」✅ | 高 |
| 2 | `DA42` | Deathwing Knight Master · DarkAngels | `Armour 2. Teleport: Deal 3 damage to all enemies and give `**`+2`**` to all friendly units.` | `Dark Angels/3部队/Warpforge_42_Deathwing-Knight-Master.png` | 粉圈白拳 | 近战 | 同上 | **0.899 / 0.438** | 中文「+2 攻击」✅ | 高 |
| 3 | `DA55` | Sacred Standard · DarkAngels | `Give `**`+1`**` and Blast 1 to all friendly units this turn` | `Dark Angels/4计策/Warpforge_55_Sacred-Standard.png` | 粉圈白拳（旁边那枚紫爆裂 = `Blast`） | 近战 | 同上 | **0.783 / 0.421** | 中文「+1 攻击」✅ | 高 |
| 4 | `EC1` | Lord Kaphrael · EmperorsChildren | `Stimulation: Give `**`+1`**` to a random friendly troop. Talent: Euphoric Strike` | `Emperor_s Children/1督军/Warpforge_01_Lord-Kaphrael.png` | 粉圈白拳 | 近战 | 同上 | **0.965 / 0.434** | 中文也是裸的 | 高 |
| 5 | `EC16` | Slaanesh's Spawn · EmperorsChildren | `Cruelty: Gain `**`+1`**` and Flank` | `Emperor_s Children/3部队/Warpforge_16_Slaaneshs-Spawn.png` | 粉圈白拳（旁边橙双箭头 = `Flank`） | 近战 | 同上 | **0.845 / 0.452** | 中文也是裸的 ⚠️ 见 §7.5 | 高 |
| 6 | `EC58` | Exquisite Swordsmanship · EmperorsChildren | `Your Warlord gains `**`+1`**` this turn. Deal damage to all enemies equal to your Warlord's Melee` | `Emperor_s Children/4计策/Warpforge_58_Exquisite-Swordsmanship.png` | 粉圈白拳 | 近战 | 同上 | **0.898 / 0.513** | 中文也是裸的 | 高 |
| 7 | `EC77` | Xylocil · EmperorsChildren | `Give `**`+2`**` to a friendly troop` | `Emperor_s Children/6药剂/Warpforge_77_Xylocil.png` | 粉圈白拳 | 近战 | 同上 | **0.915 / 0.513** | 中文也是裸的 | 高 |
| 8 | `GOF99` | Cyber‑augmentation · Goff | `Give `**`+1`**` and Flank to a random troop in your hand` | `Orks/4计策/Warpforge_25_Cyber‑augmentation.png` | 粉圈白拳（旁边橙双箭头 = `Flank`） | 近战 | 同上 | **0.924 / 0.405** | ⚠️ 中文串表写「+1 攻击**和 +1 生命值**，以及侧翼」—— 英文里没有生命值，**中文串表多写了** | 高 |
| 9 | `GOF_Da_Bigger_Dey_Iz_Mozrog_s_Talent` | Da Bigger Dey Iz... (Mozrog's Talent) · Goff | `Ephemeral. Deal 1 damage to an enemy. If target survives, give `**`+1`**` and Stomp to your Warlord this turn` | `Orks/2天赋/Warpforge_02B_Da-Bigger-Dey-Iz.png` | 粉圈白拳（旁边徽章 = `Stomp`） | 近战 | 同上 | **0.857 / 0.440** | ⚠️ 中文串表写「+1 攻击、**+1 生命值**和践踏」—— 同样多写 | 高 |
| 10 | `GOF_Waaagh_Energy_Weirdboy_Talent` | Waaagh! Energy (Weirdboy Talent) · Goff | `Ephemeral. Deal 1 damage to all friendly units and give them `**`+2`**` this turn.` | `Orks/2天赋/Warpforge_17B_Waaagh-Energy.png` | 粉圈白拳 | 近战 | 同上 | **0.915 / 0.442** | ⚠️ 中文串表写「+2 攻击**和 +2 生命值**」—— 同样多写 | 高 |
| 11 | `GSC22` | Acolyte Leader · Genestealers | `When your opponent plays a Stratagem, give `**`+1`**` and +1 Health to your troops` | `Genestealer Cult/3部队/Warpforge_22_Acolyte-Leader.png` | 粉圈白拳（后半句 `+1 Health` 卡面写的是裸词） | 近战 | 同上；同阵营参照卡的兄弟卡 | **0.952 / 0.353** | 中文也是裸的 | 高 |
| 12 | `SOR11` | Crusader · Sororitas | `3: Gain `**`+1`**` and Armour 2` | `Sorotitas/3部队/Warpforge_11_Crusader.png` | 粉圈白拳（旁边银盾 = `Armour`；`3` 后面那枚见 §7.2） | 近战 | 同上 | **0.978 / 0.395** | 中文「+1 **近战攻击**」✅ | 高 |
| 13 | `SW26` | Thunderwolf Cavalry Pack Leader · SpaceWolves | `When an enemy gets a Hunt Mark, give `**`+1`**` to your units this turn. Rally: Give Hunt Mark to an enemy troop` | `Space Wolves/3部队/Warpforge_26_Thunderwolf-Cavalry-Pack-Leader.png` | 粉圈白拳 | 近战 | 同上 | **0.672 / 0.474** | 中文也是裸的 | 高（目视很清） |
| 14 | `TAU72` | Krootox Rampager · TauEmpire | `Flank. Rally: Gain `**`+3`**` this turn` | `Tau/3部队/Warpforge_08_Krootox-Rampager.png` | 粉圈白拳 | 近战 | 同上 | **0.952 / 0.492** | 中文也是裸的 | 高 |
| 15 | `TAU75` | Thundering Rampage · TauEmpire | `Give `**`+3`**` to a friendly unit this turn. If it's an Infantry, give it Flank as well` | `Tau/4计策/Warpforge_11_Thundering-Rampage.png` | 粉圈白拳（旁边橙双箭头 = `Flank`） | 近战 | 同上 | **0.933 / 0.391** | 中文也是裸的 | 高 |
| 16 | `TL86` | Bestial Rage · Leviathan | `Give `**`+4`**` to all friendly troops` | `Tyranid/4计策/Warpforge_13_Bestial-Rage.png` | 粉圈白拳 | 近战 | 同上（NCC 偏低是**这张字号大、图标尺寸与模板不同尺度**，目视很清楚） | **0.571 / 0.440** | 中文也是裸的 | 高（目视） |
| 17 | `UM_Angel_s_Wrath` | Angel's Wrath · Ultramarines | `Ephemeral. Give `**`+1`**` to a friendly unit until your next turn. Oath 2: Give it Armour 2 this turn` | `Ultramarines/2天赋/Warpforge_06B_Angels-Wrath.png` | 粉圈白拳 | 近战 | 同上 | **0.682 / 0.459** | 中文也是裸的 | 高（目视很清） |
| 18 | `UM_Assault_Doctrine` | Assault Doctrine · Ultramarines | `Give `**`+3`**` to a friendly unit. Codex: Give it Armour 1 and heal 2 to it` | `Ultramarines/4计策/assault doctrine.png` | 粉圈白拳 | 近战 | 同上 | **0.946 / 0.387** | 中文也是裸的 | 高 |

### 3.B 紫圈白枪 = **远程 `Ranged`**（1 张）

| # | 卡 id | 卡名 · 阵营 | `desc` 原文 | 卡图 | 卡面那枚图标 | 我判它是 | 凭什么 | NCC 拳/枪 | 旁证 | 置信 |
|---|---|---|---|---|---|---|---|---|---|---|
| 19 | `UM_Angels_of_Death` | Angels of Death · Ultramarines | `Deploy 3 Primaris Intercessor and give them Flank. Codex: Give `**`+1`**` to your Primaris Intercessor` | `Ultramarines/4计策/angel.png` ⚠️ **文件名不带卡名**，见 §3.C | **紫圈白枪** | **远程** | 与参照 B（`Cadre Fireblade`）逐像素同形；NCC **枪 0.708 > 拳 0.428**（唯一一张枪胜出的） | **0.428 / 0.708** | 中文也是裸的 | 高 |

> 🔴 **这张是本次最要紧的一条**：它是**唯一被判成远程**的候选 ——
> 若照「默认近战」的惯例去补，会**静默地把远程加在近战上**（正是 `CLAUDE.md` 铁律 7 记的那个坑，S9 §7 也是同一种错）。

### 3.C 锯齿环形徽记 = **任务点 `Quest Point`**（2 张）

| # | 卡 id | 卡名 · 阵营 | `desc` 原文 | 卡图 | 卡面那枚图标 | 我判它是 | 凭什么 | 置信 |
|---|---|---|---|---|---|---|---|---|
| 20 | `DA86` | Hunters of Heretics · DarkAngels | `Give Vulnerable 4 and "Backlash: Your opponent gains `**`2`**`" to an enemy troop` | `Dark Angels/4计策/Warpforge_12_Hunters-of-Heretics.png` | **锯齿环形徽记**（银刺 + 墨绿圆 + 数字嵌在里面） | 对手获得 **2 点任务点** | 与 `March of Vengeance` 的 `Gain ⟨锯齿环 2⟩` **同形同色**；NCC **任务点 0.846 > 拳 0.545 > 枪 0.411** | 高 |
| 21 | `DA11` | Sergeant Naaman · DarkAngels | `Slay: Gain `**`1`**` | ⚠️ **卡图找不到**（见下） | （替身卡）`Dark Angels/3部队/Warpforge_11_Sergeant-Taaman.png` 的 `Slay: Gain ⟨锯齿环 1⟩` | **获得 1 点任务点** | ① 官方中文串表 `zh_CN.csv:2273` 把这个**一字不差的英文串**译作「斩杀：获得 **1 点任务**」；② 我们表里这张卡的 `ocrSrc` 就指着上面那张 Taaman 卡，而那张卡面画的是**同款锯齿环**（我裁块 8× 放大亲读） | 中（**旁证链**，不是直接读到自己那张卡的图） |

> ⚠️ `DA11` 的**名字有疑**：我们池里叫 `Sergeant Naaman`，`ocrSrc` 却是 `…Sergeant-Taaman.png`，
> 而**卡面标题印的是 `Sergeant Taaman`**。`zh_CN.csv` 里 **两个名字都存在**
> （`Sergeant Naaman` 见 2227/3104/4293/5487；`Sergeant Taaman` 只在 `The Rock` 的正文里，825 行）。
> ⇒ **「Naaman 与 Taaman 是不是同一张卡」我没有查清**（见 §8）。
> 但**不论是不是同一张，结论都是任务点** —— 前者靠官方中文串、后者靠卡面。
>
> ⚠️ **`DA86` 的中文（我们自己的）写错了**：`descZh` = 「你的对手获得 **2 个骷髅头**」——
> 卡面画的是锯齿环，**不是骷髅**。（这条也解释了「descZh 不能当权威」。）

---

## 4. 判不了的

**0 张「完全判不了」。** 只有一条**判据强度打折**：

| 卡 | 情况 | 还差什么 |
|---|---|---|
| `DA11 Sergeant Naaman` | 卡面图标**没有直接读到**（PnP 目录里没有 `Naaman` 的卡图），靠**两条旁证**推的 | 若能确认「Naaman = Taaman 是同一张卡」，这条就升成**高**。查法：`d:/2/Warpforge备份/` 或原版 `draftpacks` 里按 `Sergeant Naaman` / `Naaman` 全文搜一遍卡图与卡组数据（**我这轮只搜了 `d:/2/Warpforge部队卡片/` 与 `zh_CN.csv`**） |

另外**顺带**记两条「这两张卡我判得了，但卡图文件名对不上卡名」：
- `UM_Angels_of_Death` → 文件是 `Ultramarines/4计策/angel.png`（`card_stats.json` 的 `ocrSrc` 写的是
  `Ultramarines/Core/angel.png`，`Core/` 这个子目录**现在已经不存在了**，文件被挪到 `4计策/`）。
- `UM85 Catechism of Death` → 正常命名，无问题。

---

## 5. 已修那 5 张的复核（有没有把它们漏掉 / 重复算）

| 卡 | 当前 `desc`（我扫时的读数） | 我的扫描有没有再报它 |
|---|---|---|
| `GOF50 Tide of Muscle` | `Draw two troops and give them +1 **Attack**` | ❌ 没报（`+1` 后面已是属性词）✅ |
| `GOF53 Get'em ladz!` | `Give +2 **Attack** to your units this turn` | ❌ 没报 ✅ |
| `GOF_Da_Red_Waaagh` | `Draw a troop and give it +1 **Attack**` | ❌ 没报 ✅ |
| `GOF_Krumpaklaw` | `Mob: Gain +2 **Attack** and Armour 1` | ❌ 没报 ✅ |
| `UM85 Catechism of Death` | `Ephemeral. Give +2 **Attack** to a friendly troop, or to your Warlord this turn. Oath 3: Give it an additional +2 **Attack**` | ❌ 没报 —— **但它是在我这次扫描「扫到一半时」被另一个写手改掉的**，见 §7.1 |

⇒ **我的 21 张清单与那 5 张零交集，没有重复算，也没有漏掉。**

---

## 6. 历史文档里已有的同类结论（别重复劳动）

| 文档 | 已有的结论 | 与本轮的关系 |
|---|---|---|
| `资料/卡表核对_卡图提取/_裁定_图标丢失.md`（2026-09-13 第三十三轮） | **`Gain N` 那 54 张**（卡面图标被 OCR 丢掉、`Gain N` 裸数字被当「+1 能量」静默算错）。给了**图标含义表**：锯齿环 = **任务点** / ☀ = 信仰 / 蓝石 = 灵魂石 / 粉拳 = 近战 / 紫枪 = 远程 / 银盾 = 护甲 / 圆能量 = 能量。**并留了 3 条「没查到 / 待复核」**：`Aggressor`(SW) 找不到卡图 · **`Sergeant Naaman` 找不到卡图** · `Nephilim Jetfighter` 待复核 | ✅ 本轮**沿用它的图标含义表**；其中 **`Sergeant Naaman` 这一条本轮有进展**（我把它的图标判成任务点，证据见 §3.C）—— **建议调度台据此更新那一行** |
| `资料/普查产出_1018/S9_GOF四张图标.md` §7 | 第 5 张 `UM85`（两处 `+2` 卡面都是粉拳）、`EC37` 是当年举错的 | ✅ 本轮复核：`UM85` **已被改**（§7.1）；`EC37` 我在扫描里**单独排掉**（它 `desc` 里本来就有 emoji 记号 `-1 🗡 and -1 🔫`，不是裸数字） |
| `资料/PnP卡图_逐张对账_0915.md:560` | 已订正过「卡面也裸」那句 | 本轮结论与订正后的口径一致 |
| `资料/卡表核对_卡图提取/_还原效果文字.md` 等一族 | 我只做了**文件名级**的检索（有没有同名主题），**没有逐篇读**——若要收口，建议派只读代理专门过一遍这一族，看有没有第三批同类 | ⚠️ 见 §8.3 |

---

## 7. 顺手发现（**都没改**）

1. 🔴 **卡池文件在我扫描期间被另一个写手改过。**
   `d:/4/Unity/数据/游戏数据/cardface_fixes.json` mtime = **00:14:30**，
   `d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json` mtime = **00:14:40**。
   **改动内容 = `UM85` 的两处 `+2` → `+2 Attack`**（S9 §7 那条「按红线只报不改」的欠账，看来被调度台落了）。
   ⇒ 影响：我第一遍扫（00:1x）**报了** `UM85`，最后一遍扫（00:2x）**不再报它**。
   ⚠️ **本报告 §1/§3 的数字以「最后一遍扫」为准**，`UM85` **不算**在我这 21 张里。

2. 🔴 **另一族「图标被剥」：`N ⟨资源图标⟩:` 的**发动费用**（10 张）** —— 和属性图标是**两回事**，
   但同属「卡面印着图标、文本里没有」。卡面实据（我抽了 4 张亲读）：
   | 卡 | 我们 `desc` | 卡面实况 |
   |---|---|---|
   | `SOR24 Seraphim` | `3: Gain +1 Ranged and +1 Health` | `3 ⟨金十字日轮 = 信仰⟩ : Gain +1 ⟨紫枪⟩ and +1 Health` |
   | `SOR23 Retributor` | `1 : Deal 3 damage to a random enemy` | `1 ⟨金十字日轮⟩ : Deal 3 damage to a random enemy` |
   | `SOR11 Crusader` | `3: Gain +1 and Armour 2` | `3 ⟨金十字日轮⟩ : Gain +1 ⟨粉拳⟩ and ⟨银盾⟩ Armour 2` |
   | `UM80 Devastator Marine` | `4: Deal 5 damage to an enemy troop` | `⟨紫爆裂⟩ Blast 2. ⟨蓝盔⟩ Oath 4: Deal 5 damage to an enemy troop` |

   **其余同族（未逐张核卡图，只由文本形态判出）：** `SOR72`(`6:`) · `SOR28`(`2 :`) · `SOR43`(`2 :`) · `SOR49`(`2 :`) · `UM81`(`3:`) · `UM_Scout_Sniper`(`1:`)。共 **10 张**。
   ⚠️ **两处存疑**：① 同一个「金日轮」徽记，`SOR24/23/11` 那三处按修女会该是**信仰 ☀**，
   而 `SOR66 Daemonbreaker` 的**同一枚**我们却写成 `[Energy]` —— **两者必有一处标错**，我没查清（§8.4）；
   ② `UM80` 的卡面是 `**Oath** 4:`，我们 desc 把 `Oath` 也丢了（另一半 `Blast 2.` 也不在 desc 里）。

3. **我们 `desc` 里有 1 处**裸露的占位符**：`UM_Phobos_Lieutenant` = `… Oath 1: Gain **[eye icon]** Stealth. …`
   —— `[eye icon]` 是**英文的说明文字**，不是我们的方括号记号体系。
   ⚠️ 这会让卡面/解析把这几个字母**当记号原样印出来**（我没有跑 Unity 去验它最终长什么样）。

4. **方括号族 54 处 / 44 张**（`±N` 后面**紧跟着**一个方括号记号）。绝大多数是规范的
   `[Attack]`(23) / `[attack]`(13) / `[Ranged]`(5) / `[ranged]`(3)。
   ⚠️ **但其中有 9 处用的是【卡面字形名】而不是属性名**：
   `[Might]`×3（`EC28` · `SOR55`×2）· `[fist]`×3（`GSC51` · `SW63` · `GOF_Ferocious_Rage_Beastboss_Talent`）·
   `[Strength]`（`GSC43`）· `[strength]`（`UM87`）· `[weapon]`（`GOF16`）。
   🔴 **这与铁律 7 记的坑同族**（`[weapon]` 在 `+N Attack … +N Armour` 那个固定搭配里其实指**远程**；
   真护甲一律写裸词 `Armour 1`）。**是否同一类缺陷、要不要一起收口，我没有查**（§8.5）。

5. 🔴 **`descZh` / 官方中文串表 `zh_CN.csv` 对这批卡【自己也是裸的】，而且自相矛盾。** 三条实测：
   - **自己也裸（逐处数过）**：这 21 张里，中文**在裸那处点了属性的只有 8 张**
     （`ASH79`「+1 **近战攻击**」· `DA42`「+2 **攻击**」· `DA55`「+1 **攻击**」· `GOF99`「+1 **攻击**」·
     `GOF_Da_Bigger`「+1 **攻击**」· `GOF_Waaagh_Energy`「+2 **攻击**」· `SOR11`「+1 **近战攻击**」·
     `DA11`「1 点**任务**」）；
     **其余 13 张的中文也是光秃秃的 `+N`**（`EC1` · `EC16` · `EC58` · `EC77` · `GSC22` · `SW26` ·
     `TAU72` · `TAU75` · `TL86` · `UM_Angel_s_Wrath` · `UM_Angels_of_Death` · `UM_Assault_Doctrine` · `DA86`）。
   - **同一个英文串，中文三条译法**：`Gain +1 and Flank` 在串表里有三种中文 ——
     `①：获得 +1 **远程攻击**和侧翼`（这一条说**远程**！）· `残忍：获得 +1 和侧翼`（裸）· `誓言 2：获得 +1 和侧翼`（裸）。
     ⚠️ 其中「说远程」那条的英文原文是 `**(1)** Gain +1 and Flank`（**我们池里没有这张**，我们只有 EC16 的
     `Cruelty: Gain +1 and Flank`）⇒ **它可能压根不是同一张卡**，所以我**不主张**它错、只主张
     **「同一族的英文串在不同条目里译得不一样」= 串表不能独立定图标**。
     （EC16 那处我自己**读了卡面**：粉拳，NCC 0.845 / 0.452，见 §3.A #5 —— 那才是判据。）
   - **中文串表会多写**：`GOF99` / `GOF_Da_Bigger` / `GOF_Waaagh_Energy` 三条，英文里只有 1 个加成，
     中文串表写成「**+1 攻击和 +1 生命值**」「**+2 攻击和 +2 生命值**」—— **多出的生命值英文里没有**。
   ⇒ 🔴 **结论：`descZh` 对「图标被剥」这一族【不能当权威，连强旁证都算不上】**。
   （与 CLAUDE.md 那句「看中文 = 看玩家实际看到的东西」**不冲突** —— 那句管的是**措辞歧义**（`or` 的两种含义），
   不管**图标被剥**；这两件事要分开。）

6. **`card_icon_plan.json` 我按只读查了一眼**：本批 21 张里只有 `GOF_Krumpaklaw`（已修那张）在表内，
   **本轮没有新增任何方括号记号/符号** ⇒ 该表**不需要重跑**。

---

## 8. 没查清的部分（**如实记，没猜**）

1. **`Sergeant Naaman` 与 `Sergeant Taaman` 是不是同一张卡** —— 没查清。两条线索互相矛盾：
   我们池里的 `DA11` 叫 `Naaman` 但 `ocrSrc` 指着 `Taaman` 的图；
   `zh_CN.csv` 两个名字**都在**（`Naaman` 是卡名 + Talent + Companion ×2，`Taaman` 只出现在 `The Rock` 正文）。
   **我没搜的地方**：`d:/2/Warpforge备份/**`、`d:/4/Unity/素材/Warpforge原版/卡牌/卡组数据/**`、原版 `draftpacks` 包。

2. **有没有「连数字一起被剥掉」的卡**（卡面印着 `⟨图标⟩ 2`，我们 desc 里**那半句整个不见了**）——
   **本轮没有扫这一类**。我扫的只是「数字还在、图标没了」。
   要收口得拿**卡面文字**和我们 `desc` 做**整句对拍**（不是找数字），那是另一件活。

3. **`资料/卡表核对_卡图提取/` 那一族（`_合并总表.md` / `_还原效果文字.md` / `_裁定_*.md` 共 12 篇）
   我只读了 `_裁定_图标丢失.md` 一篇**，其余只做了**文件名级**检索。是否有第三批同类结论**没查清**。

4. **`SOR66 Daemonbreaker` 的 `[Energy]` 与 `SOR24/23/11` 那枚「金日轮」是不是同一个图标** ——
   我目视觉得**同形**（棕金十字/星形底座 + 中心金色日轮），但**没有做 NCC 对拍**。
   若同形，则**两者必有一处标错**（修女会该是 ☀ 信仰）。

5. **方括号族里那 9 处字形名记号（`[Might]` `[fist]` `[Strength]` `[strength]` `[weapon]`）
   在我们的解析层认不认、算成什么** —— **没查**（本轮不碰 `.cs`、不跑探针）。

6. **本报告所有「卡面读出来的属性」都是【读图】得到的，没有跑 Unity 验收**
   （红线：不许跑）。按 S9 的先例，正式验收要在同步点由调度台跑 **`RuleEngineTest.Run`**。

---

## 9. 我搜过的目录与关键词

**读过的数据源（只读）**
- `d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json`（1126 张，全量）
- `d:/4/Unity/数据/游戏数据/card_stats.json`（查 `ocrSrc` / `face` / `name`，全量索引）
- `d:/warpforge/data/i18n/zh_CN.csv`（**6370 行**，旁证）
- `d:/2/Warpforge部队卡片/**`（**1375 个 PNG**，全量建了「规范化卡名 → 路径」索引）
- `d:/4/Unity/资料/普查产出_1018/S9_GOF四张图标.md`（全文）
- `d:/4/Unity/资料/卡表核对_卡图提取/_裁定_图标丢失.md`（全文）

**关键词 / 正则（都跑过，附命中数）**
| 搜法 | 命中 |
|---|---|
| `([+-])\s?(\d+)` over 全部 `desc` | **397** 处 |
| 其中「后一个 token 是字母词且 ∈ 属性词表」 | 排除 |
| 其中「后一个 token 是 `[`」 | **54** 处（方括号族） |
| 其中「后一个 token 是标点 / 词尾 / emoji / `to` `and` `this`」 | **22** 处 / **21** 张 |
| `\b(Gain\|gain\|Give\|give\|Heal\|heal\|have\|has\|gains\|get\|gets\|receives?)\s+(\d+)\b` | **101** 处，其中真裸数字 **2** 处 |
| 「所有数字的前后一个词」全量打印、逐组人看 | **117** 组，新增候选 **0** |
| `\b\d+\s*:`（发动费用族） | **50** 处，去重后真发动费用 **10** 张 |
| 文件名 `find -iname "*Angels*of*Death*"` over `d:/2` | 命中解包美术 `bundle_spacemarinesultramarinescardassets_assets_all/Texture2D/SM_UM_strat_Angels of Death.png`（**立绘，不是卡面**）+ PnP 里 `Ultramarines/4计策/angel.png` |
| 文件名 `find -iname "*Hunter*"` / `"*Naaman*"` / `"*Taaman*"` over `d:/2/Warpforge部队卡片` | 见 §3.C |
| `grep "Slay: Gain" / "获得 1 点任务" / "Naaman" / "Taaman"` in `zh_CN.csv` | 见 §3.C、§7.5 |
| 全池「裸数字」复核用的卡图目录 | `d:/2/Warpforge部队卡片/{Aeldari, Astra Militarum, Chaos, Dark Angels, Emperor_s Children, Genestealer Cult, Necron, Orks, Sorotitas, Space Wolves, Tau, Tyranid, Ultramarines}/…`（**13 个阵营目录全在**） |

**没搜的地方（如实记）**：`d:/2/Warpforge备份/**` · `d:/2/Warpforge_tools/data/ui_extract/**` ·
`d:/4/Unity/素材/**` · `d:/4/Unity/资料/卡表核对_卡图提取/` 里除 `_裁定_图标丢失.md` 外的 11 篇 ·
`d:/2/tools/decomp_full/**`（本轮是**读卡图**的活，没去碰反编译）。

**本轮产出的临时物（在仓库外，可随时删）**
`d:/tmp/wf_bare/{scan_words,scan_cand,scan_cand2,scan_unsigned,find_imgs,mkcrops,lookup_csv,final_table,matcher,run_match}.py` ·
`d:/tmp/wf_bare/crops/*.png` · `d:/tmp/wf_bare/tmpl/*.png` · `d:/tmp/wf_bare/matchout/tmpl_match.json`。
