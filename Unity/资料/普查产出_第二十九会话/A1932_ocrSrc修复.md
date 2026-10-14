# `A1932` —— `card_stats.json` 的 `ocrSrc` 列修好了（1119 条全改完，0 条修不了）

> 账 `A1932`（在 `资料/任务_卡组卡牌战斗.md:601`）：PnP 目录早已重构成
> `<阵营>/<1督军|2天赋|3部队|4计策|5防御卡>/`，而 `ocrSrc` 还写着旧子目录 ⇒ 任何「照 `ocrSrc` 去开卡图核对」的流程整片扑空。
> **本轮：改 1119 条 · 剩 0 条修不了 · 改完 1121/1121 条 `ocrSrc` 都真的打得开。**
> 修复脚本 = `工具/_fix_ocrsrc_1014.py`（干跑 / `--apply` / `--check` 三种用法，可复跑）。

---

## 一、改了 1119 条 / 剩 0 条修不了

- **改 1119 条**（表里非空 `ocrSrc` 共 1121 条，其中 2 条本来就打得开 ⇒ 一个字节没动）。
- **修不了 0 条** —— 1119 条的旧文件名，**每一条都在现目录树里唯一命中**（见 §二的三条独立旁证）。
- **另有 92 条 `ocrSrc` 本来就是空的** —— ⛔ 不是「失效」，不在本账范围，**我一条都没填**（见 §六·1）。
- **1 条点名**：`Poisoned Supplies` 解析出来了，但落点是 `Genestealer Cult/卡背和部队卡PDF（用不着）/Core/`，
  **不在五位标准子目录里**（见 §三）。它是全树唯一一份该图、卡面亲读确系那张卡 ⇒ 照实指过去，另点名。

---

## 二、映射规则

### 2.1 用哪个字段把卡对到文件

**用旧 `ocrSrc` 的「文件名」（basename），⛔ 不用卡名。**

- ⛔ 卡名会 miss：`Fire prism`（`name` 小写 p，`ocrName` 才是 `Fire Prism`）、`Iron Priest` 有一行 `name` 带前导空格。
  按 `name` 精确查会漏，按 `name` 模糊查会**静默配错**（红线）。
- 做法：`os.walk('d:/2/Warpforge部队卡片')` 建两个索引 —— ① 原名精确 ② `NFC` 归一 + 小写 + `_`→`-` + 空格→`-`。
- **实测：1121/1121 走的是①精确匹配，②一次都没用上。** 基名在现树里 **恰好 1 个**文件
  （全树 1450 个文件 / 1449 个不同名字；唯一重名 = `Normal Conditions.png`（在 `Emperor_s Children/进攻卡/` 与
  `Ultramarines/进攻卡/` 各一份），**它不对应任何 `ocrSrc`** ⇒ 不构成歧义）。
- **大小写 / 空格 / `_`↔`-` 的差异：0 处**（旧基名与盘上基名逐字节相同）。

### 2.2 三条独立旁证（都不是「文件存在」这种自证）

| # | 判据 | 结果 |
|---|---|---|
| ① | **阵营段**：新路径第一级目录 vs 卡的 `faction` 字段（13 条对照，§六·5） | **1119/1119 全对，0 不符** |
| ② | **类型段**：`unit`→`3部队` · `hero`→`1督军` · `defence`→`5防御卡` | **583/583 · 55/55 · 35/35 全对**；unit/hero **一条都没落进别的目录** |
| ③ | **文件名尾巴**：`Warpforge_NN_<卡名>.png` 里的 `<卡名>` vs `name`/`ocrName`（归一掉撇号/叹号/连字符后比） | **可比对 1033 条，对不上 0 条**（另 88 条文件名没有 `NN_卡名` 这种尾巴：12 条 `Zrzut ekranu…`（波兰语截图名）· 8 条 `IMG_xxxx.jpg` · 22 条编号带字母（`Warpforge_00a_…`）/带 `UPDATED_` 前缀 · 46 条干脆就是裸卡名如 `assault doctrine.png` ⇒ 这 88 条走的是「基名精确命中」+②③旁证） |
| ④ | **人工亲读卡面**：6 张拼成一张对照图逐张看（含 3 条反常 + `Autarch` + `Fire Prism` + `Iron Priest`） | 6/6 都是同一张卡（`Autarch` 那张的图名是 `Zrzut ekranu 2026-04-16 o 18.59.12.png`，卡面印 `Autarch / Waystone / Give +1 …`，与表里那条逐字对上） |

### 2.3 旧子目录 → 新子目录 对照表（1119 条全量，按条数）

| 旧子目录 | 新子目录 | 条数 |
|---|---|---|
| `Core` | `3部队` | 453 |
| `Core` | `4计策` | 266 |
| `Core` | `2天赋` | 73 |
| `Core` | `1督军` | 38 |
| `Core` | `5防御卡` | 38 |
| `Beast Snaggas` | `3部队` | 20 |
| `The Avenging Son` | `3部队` | 19 |
| `Beast Snaggas` | `4计策` | 10 |
| `The Avenging Son` | `4计策` | 10 |
| `2nd Company` / `Dark Zealots` / `Engines of Rebellion` / `Kroot Hunting Pack` / `Letharius Strike Force` / `Ramatekh` / `Sacred Rose` / `Storm of Silence` / `Tempestus` / `Vanderghast Swarm` / `Vengeful Brethern` / `Whispering Web` | `3部队` | 各 7 |
| `Core` | `6药剂` | 6 |
| `Gitstompas` | `3部队` | 6 |
| `The Avenging Son` | `2天赋` | 6 |
| `Beast Snaggas` | `2天赋` | 5 |
| `Core` | `6秘密` | 5 |
| `2nd Company` | `2天赋` | 4 |
| `Gitstompas` | `4计策` | 4 |
| 上面 12 个「各 7」的旧名 | `4计策` | 各 3 |
| `2nd Company` → `4计策` · `Core` → `6破坏卡` | —— | 各 3 |
| `Beast Snaggas` → `1督军` · `The Avenging Son` → `1督军` · `Dark Zealots` → `2天赋` · `Vanderghast Swarm` → `2天赋` · `Whispering Web` → `2天赋` | —— | 各 2 |
| `2nd Company` / `Dark Zealots` / `Engines of Rebellion` / `Gitstompas` / `Kroot Hunting Pack` / `Letharius Strike Force` / `Ramatekh` / `Sacred Rose` / `Storm of Silence` / `Tempestus` / `Vanderghast Swarm` / `Vengeful Brethern` / `Whispering Web` | `1督军` | 各 1 |
| `Engines of Rebellion` / `Gitstompas` / `Kroot Hunting Pack` / `Letharius Strike Force` / `Ramatekh` / `Sacred Rose` / `Storm of Silence` / `Tempestus` / `Vengeful Brethern` | `2天赋` | 各 1 |
| `Core` | `Core`（＝ §三·A `Poisoned Supplies`，落点非标准） | 1 |
| — | — | **合计 1119** |

> 逐条全量（69 行 `旧→新`）可由 `git diff -U0 -- 数据/游戏数据/card_stats.json` 现算，
> 脚本里也把这张表打出来了（`_fix_ocrsrc_1014.py` 干跑的输出里 `旧子目录：{…}` / `新子目录：{…}` 两行）。
>
> **一句话**：旧子目录名（`Core` / `Beast Snaggas` / `The Avenging Son` / `2nd Company` / `Whispering Web` … **16 个**）
> 与**卡的类型**完全无关，新目录才是按类型分的 —— 所以「只换目录段」这一步**必须靠盘上真实路径**，
> ⛔ 不能按 `type` 反推（`tactic` 会同时落在 `4计策`/`2天赋`/`5防御卡`/`6药剂`/`6秘密`/`6破坏卡` 六处）。

### 2.4 值写成什么样

- **保留每条原有的盘符 / 分隔符写法**，只换 `Warpforge部队卡片` 之后那一段，
  并**用 `os.walk` 出来的真实相对路径**（不是拼的）。
  改后分布（与改前完全一致）：`d:/2/…\…` **1085** 条 · `d:\2\…\…` **34** 条 · `d:/2/…/…` **2** 条。
- **落盘方式 = 原文字面量替换**（`"旧值"` → `"新值"`），⛔ **不是整篇 `json.dumps`**。
  原因：本文件有 **23 行** `"desc"` 的缩进是手工多一个空格（`:8040` · `:8466` · `:8497` · `:8586` · `:8650` · `:8965` ·
  `:8991` · `:9043` … 全篇是 3 空格、这 23 行是 4 空格），整篇重序列化会把这些抹平（实测往返**短 23 字节**）
  ⇒ 那样 diff 就不止 `ocrSrc` 了。
- 替换前**核对每个旧值在原文里出现的次数**（表里几条就应当出现几次）。实测：1121 条非空里有 **2 个值各出现 2 次**
  （`Imotekh the Stormlord` 那两行 · `Iron Priest` 那两行）⇒ 两条都替换成同一个新值，正确。
  重复的**值**只有 2 个（`Imotekh-the-Stormlord` · `Iron-Priest`），其余 1117 个值各出现 1 次。

---

## 三、修不了的逐条

**修不了 = 0 条。** 下面两类**不是**「修不了」，是**点名 + 只报不改**：

### A. ⚠️ 解析出来了、但落点不在五位标准子目录（1 条，**已改**）

| 卡 | 旧 `ocrSrc` | 新 `ocrSrc` | 备注 |
|---|---|---|---|
| `Poisoned Supplies`（Genestealer Cults / Sabotage） | `d:/2/Warpforge部队卡片\Genestealer Cult\Core\Warpforge_15_Poisoned-Supplies.png` | `d:/2/Warpforge部队卡片\Genestealer Cult\卡背和部队卡PDF（用不着）\Core\Warpforge_15_Poisoned-Supplies.png` | 该子目录里有 `…png的说明.txt`：「打出部队 Biophagus 后，在对手的手牌里生成一张卡牌 Poisonesd——就是这个 `Warpforge_15_Poisoned-Supplies.png`。**这个卡牌不需要**，已经有了相同的卡牌了。」**全树唯一一份**该图；我亲读卡面：`Poisoned Supplies / Genestealer Cults / At the end of your turn, your troops take 1 damage / Sabotage` ⇒ **确系那张卡**。同阵营 `5防御卡/` 里的是 `Pilfered Supplies`（另一个名字）⇒ 不是它 |

我搜过的范围：`d:/2/Warpforge部队卡片/` **全树 `os.walk`**（1450 个文件），按基名精确 + 归一化两种查法，
关键词 `poisoned` / `Warpforge_15_Poisoned-Supplies` —— 只命中上面那一处（外加同名的 `.txt`）。

### B. `ocrSrc` 本来就空（92 条）—— **不在本账范围，一条没填**

逐条看过名字，分三类：

1. **17 条**：**同名的图确实存在**，但都在 `<阵营>/进攻卡/` 里（**战场环境 / 进攻卡**，不是部队卡）——
   `Normal Conditions`（×9）· `Default Conditions` · `Webway Rift` · `Aural Hijack` · `Empyric Rift` ·
   `Stimm-Vents Leak` · `Everstorm` · `First Light` · `Full moon`。
   ⚠️ 注意 `Normal Conditions.png` **全树只有两份**（`Emperor_s Children/进攻卡/` 与 `Ultramarines/进攻卡/`），
   而表里有 **9 条**这个环境的记录（每个阵营一条）⇒ **它们本来就不是「一卡一图」的关系**。
   ⛔ **没填**：`进攻卡` 与「部队卡」是**两类资源**，账只说修「失效的 1119 条」；这是**新账**（见 §六·1）。
2. **4 条是 `defence` 型、且有 `face` 指向 PnP 图**：`Hellfire Torch` · `Hellfire Pit` · `Rusted Vent` · `Awakened Obelisk`
   —— 它们与表里**另外三行**（`Helfire Torch` / `Helfire Pit` / `Awakening Obelisk`，名字拼错、**带 `ocrSrc`**）是**重复行**（见 §六·2）。
3. **其余 71 条**：本地确实没有对应图（`Bladeguard Lieutenant` · `Lieutenant with Combi-Weapon` · `Planetary invasion` ·
   `Righteous Gaze` · `Commissar Elan` · `Predator Destructor` · `Orbital Bombardment` · `Solar Storm` · `Acid Rain` …）。

---

## 四、抽验 5 条

从 1121 条里**随机抽 5 条**（`random.seed(20261014)`），逐条：改后的 `ocrSrc` → 开图 → 是不是同一张卡。

| # | 卡 | 新 `ocrSrc` | 打得开 | 是同一张卡吗 |
|---|---|---|---|---|
| 1 | `Exemplary Warrior`（Ultramarines） | `d:/2/Warpforge部队卡片\Ultramarines\2天赋\Warpforge_00a_Exemplary-Warrior-2.png` | ✅ 1,079,510 B | ✅ 卡面印 `Exemplary Warrior / Ultramarines / Ephemeral. Your Warlord heals 1 and chooses an effect` |
| 2 | `Veteran Noise Marine`（Emperor's Children） | `d:/2/Warpforge部队卡片\Emperor_s Children\3部队\Warpforge_25_Veteran-Noise-Marine.png` | ✅ 892,528 B | ✅ 卡面印 `Veteran Noise Marine / Blast 3 / Infantry` |
| 3 | `Ravenwing Ancient`（Dark Angels） | `d:/2/Warpforge部队卡片\Dark Angels\3部队\Warpforge_29_Ravenwing-Ancient.png` | ✅ 855,679 B | ✅ 卡面印 `Ravenwing Ancient / Dark Angels / Flank. Slay: Give +1 … / Vehicle` |
| 4 | `Chaplain Cassius`（Ultramarines） | `d:/2/Warpforge部队卡片\Ultramarines\3部队\Warpforge_20_Chaplain-Cassius.png` | ✅ 895,693 B | ✅ 卡面印 `Chaplain Cassius / Friendly Oath abilities apply an additional time / Infantry` |
| 5 | `Exhortation of Rage`（Ultramarines） | `d:/2/Warpforge部队卡片\Ultramarines\2天赋\Warpforge_02_Exhortation-of-Rage-2.png` | ✅ 1,125,529 B | ✅ 卡面印 `Exhortation of Rage / Ultramarines / Ephemeral. Give +1 … to your troops` |

（5 张**并排渲成一张图亲读**确认过，不是只看文件名。另**全量复检**：`--check` ⇒ **非空 1121 条，可打开 1121，打不开 0**。）

---

## 五、改动读数

| 项 | 改前 | 改后 |
|---|---|---|
| md5 | `d2a5a351c57b8804e97fae002ed12b5a` | `76e61d72a0cc8bb5391ce8976a2c2628` |
| size | 1,171,057 B | 1,172,147 B（+1090 = 1119 条新子目录名比旧的长出来的字节） |
| 行尾 | CRLF 36003 / LF 36003（**纯 CRLF**） | CRLF 36003 / LF 36003 —— **行数没变、行尾没翻** |
| 条目 | 1213 条（非空 `ocrSrc` 1121） | 1213 条（非空 `ocrSrc` 1121） |
| `git diff --numstat` | —— | **1119 / 1119**（一行一条，无别行） |
| 被改行里不含 `"ocrSrc"` 的 | —— | **0 行**（`git diff -U0 \| grep -v ocrSrc \| wc -l` = 0） |
| 非 `ocrSrc` 字段被动过 | —— | **0 处**（脚本落盘前逐条逐字段比对，有一处就 abort） |

### 落盘前的三道死检查（都在脚本里，不通过就不写）

1. JSON 还能解、**条数不变**（1213）；
2. **除 `ocrSrc` 外逐条逐字段比对**，差异 0 处；
3. **行尾没翻**（`out.count(b'\r\n') == out.count(b'\n') == 36003`）。

### 下游能不能用（实测，不是推断）

- `工具/gen_cardface_fixes.py::load_src_index`（`:146`）**只取 `os.path.basename(s.replace('\\','/'))`**
  ⇒ 本次改动对它**零影响**。实测：改前 / 改后**基名索引键集合完全相同（1117 键）**、
  **逐条记录剔除 `ocrSrc` 后内容差异 0 处**；非空条数 1121 = 1121。
- `工具/gen_cards_engine.py` **明确丢掉 `ocrSrc`**（`:482` 注释：`art`/`voice`/`ocrSrc`/`face`/… 全部丢掉）。
- 另两处 `grep` 命中（`MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs:8457` · `数据/游戏数据/cardface_fixes.json`）
  都是**注释 / 文档串**，不是路径读取。⇒ **除本次修复脚本外，全仓没有别的地方读 `ocrSrc` 的目录段。**

### 复跑办法

```bash
python -I -X utf8 d:/4/Unity/工具/_fix_ocrsrc_1014.py            # 干跑（只打印计划，不落盘）
python -I -X utf8 d:/4/Unity/工具/_fix_ocrsrc_1014.py --apply    # 真改
python -I -X utf8 d:/4/Unity/工具/_fix_ocrsrc_1014.py --check    # 体检：非空 ocrSrc 是否都打得开（坏一条退出码 1）
```

**幂等**：改完之后再跑干跑，输出「要改 **0** 条」（脚本对「已经打得开的那一条」直接跳过，一个字节不碰）。

---

## 六、顺手发现（只报，⛔ 我一个都没顺手改）

1. 🔴 **92 条 `ocrSrc` 为空，其中 17 条的图就在 `<阵营>/进攻卡/` 里**（`Normal Conditions` ×9 等战场环境图）。
   **这是新账**：`ocrSrc` 这一列要不要把「进攻卡 / 战场环境」也纳进来（还是另开一列），得主对话定口径
   （我按「账只说修失效的 1119 条」**一条没填**）。另 4 条是 `defence` 型且有 `face`（见下条），71 条本地无图。
2. ⚠️ **同表有重复行 / 拼错的名字**：
   - `Hellfire Torch`(type=`defence`, ocrSrc 空, 有 `face`) **与** `Helfire Torch`(sic, type=`tactic`, 有 ocrSrc) —— 同一张卡两行；
   - `Hellfire Pit` / `Helfire Pit`(sic) · `Awakened Obelisk` / `Awakening Obelisk`(sic) 同样成对；
   - `Rusted Vent` / `Protective Bio-structure` 也各有一对（一行有 `face` 无 `ocrSrc`、一行相反）。
   - 另有 `Iron Priest` 两行（一行 `name` 带**前导空格** `' Iron Priest'`）、`Imotekh` 两行（`name` 写成 `'HB'` 与 `'stormlord'`，两行指向**同一张** PnP 图）。
   - 全表重名 6 组：`normal-conditions`×9 · `normal`×3 · `terminator-champion`×2 · `maulerfiend`×2 · `terminator`×2 · `fire-warrior-marksman`×2。
3. ⚠️ **3 条 `type` 与卡面 / PnP 目录不符**：`Helfire Pit` · `Helfire Torch` · `Awakening Obelisk` 的 `type` 写 `tactic`，
   但**卡面印的是 Defence**、PnP 也放在 `5防御卡/`（我亲读三张卡面确认）。
4. ℹ️ **`ocrSrc` 这一列现在混着三种写法**（`d:/2/…\…` 1085 · `d:\2\…\…` 34 · `d:/2/…/…` 2）。
   本次**只改失效的**，所以没统一；要统一是另一件事。
5. ℹ️ **`faction` 字段（内部 id 风格）与 PnP 目录名（阵营名）的对照表不在数据里** —— 本次是现推的 13 条：
   `Ultramarines`→`Ultramarines` · `Goff`→`Orks` · `SaimHann`→`Aeldari` · `BlackLegion`→`Chaos` ·
   `Genestealers`→`Genestealer Cult` · `DarkAngels`→`Dark Angels` · `Sautekh`→`Necron` · `TauEmpire`→`Tau` ·
   `Leviathan`→`Tyranid` · `AstraMilitarum`→`Astra Militarum` · `Sororitas`→`Sorotitas`（**PnP 目录拼错的是 `Sorotitas`**）·
   `EmperorsChildren`→`Emperor_s Children` · `SpaceWolves`→`Space Wolves`。
   ⇒ 将来再有工具要「按阵营找 PnP 目录」，**这张表要有出处**（本轮 1119/1119 全对，可作判据）。
6. ℹ️ `card_stats.json` **不是 `json.dumps` 的干净产物**：23 行 `"desc"` 缩进多一个空格（见 §2.4）。
   任何「读进来再整篇写回」的脚本都会**静默改掉这 23 行**。

---

## 附：本轮没做的事（⛔ 越界项，一个没碰）

- 没跑 Unity · 没动 `git`（只读 `git diff` / `git status` / `git show`）· 没改 `CLAUDE.md` / `项目任务.md` / `资料/任务_卡组卡牌战斗.md`。
- **只改 `ocrSrc` 一列**，`card_stats.json` 的别的字段一个没动（脚本硬拦）；**别的数据文件一个没动**
  （`cards_engine.json` / `prebuilt_decks.json` / `cardface_fixes.json` 全部未写）。
