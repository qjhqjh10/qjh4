# `W16` —— 对账口仪器两处（`A1877`）＋ 旧 id 残留（`A1777`）＋ `willrun` 那条红（`A1879`）

> 写手 `W16`，2026-10-14 第二十八会话。只碰 `工具/ruleprobe/**` · `工具/ruleprobe.sh` · `工具/*.py`（除简报禁列那几支）。判据工具 = `工具/ruleprobe.sh`（纯 dotnet）。
> ⛔ 没跑 Unity / 没跑任何 `*.Run`（用户口径）；⛔ 没动 git；⛔ 没改任何正本。

---

## A1877 ① `AuraSpecs` / `keywords` 层 —— 加了 `layers <out.jsonl>`

### 选了什么形状 · 为什么
**新子命令 `layers <out.jsonl>`**（一行一卡的 JSON）。简报允许另一种形状（`scan` 多打几列），选它的四条理由（原文在 `工具/ruleprobe/Layers.cs` 类注）：
① **粒度不同** —— `scan` 一行一卡、列里放 **op**；光环/关键词是**卡级**的（一卡 N 条），塞进 op 流会变成伪 op，把哨兵 ⑧ 的 `phantom` 从 6 条推到几十条 ⇒ **把有声读数变成噪声**；
② **分隔符不安全** —— `scan` 以 `|` 分列，而 `Payload`/`Source` 是**卡面原文**（可含 `|`/`;`/`/`）；
③ 消费方是对账脚本（Python），JSON 对象**零歧义**；④ **零回归面** —— 不动 `scan`/`check`/基线/三个指纹 ⇒ 别的写手的仪器不被打断。

### 实跑输出（现读，2026-10-14 14:11）
```
$ bash d:/4/Unity/工具/ruleprobe.sh layers D:/4/_tmp_view/w16_layers.jsonl
wrote D:/4/_tmp_view/w16_layers.jsonl cards=1126 auras=40 willrunOps=1331 rawKeywords=1038   （rc=0）
{"id":"AM72","name":"Vitus Gryf",…,"kw":["Flying","Strike"],"kwKeys":{"flying":1,"strike":1},
 "auras":[{"Head":"friendly","Payload":"Flank","CostLess":"0","ExcludeSelf":"0","Filter":"troops + 带 flying",
           "Source":"Friendly troops with Flying have Flank"}], …}
```
带光环的卡 **40 张 / 40 条**（`auras` 全空会出声；列名清单腐烂由 `Layers.AuraColsDrift()` 守卫生效）。

### 重跑对账会多判出多少条（只读复算，`A1759_逐条对账.tsv` × 本 dump）
- `trig=WhileInPlay`（含 `WhileInPlayVariable`）族 = **40 条**步骤，判定 `✅6 / ⚠️1 / ❓3 / 🔴30`；🔴 里 `我们缺 26`。
- **那 30 条里 28 条的卡在【光环层】里有对照物**，载荷逐条对得上：`TAU19` 原版 `buff{addTrait vanguard}` ↔ 我们光环 `Vanguard` · `AM72` `addTrait flank` ↔ `Flank` · `SAU36` `addTraitRemnant notDestroyRemnant` ↔ `Adjacent Remnants do not disappear…` · `UM14` `armour+1` ↔ `Armour 1` · `BL18` `addTrait vulnerable` ↔ `Vulnerable 1` · `EC27` `ranged-2` ↔ `-2 [Ranged]` …
  ⇒ **28/30 是口径缺口（判错了层），不是实现缺口**。剩 2 条待逐条看。另：`addTrait` 一族 🔴 **42 条**里 **10 条**的卡在光环层有东西。

---

## A1877 ② `scan` 打 `EffectOp.AmountMax` / `Tail`

### 改了什么
`Program.TargetBlock` 里、`/{T2:…}` 之后 `/{SRC:…}` **之前**，各追加一个块（**只在非默认值时打**，形状照 `/{DW:}`/`/{T2:}`）：`/{AM:n}`（`AmountMax > 0`）· `/{TAIL:…}`（`Tail` 非空）。
`/{SRC:…}` 仍排最末尾（哨兵 ⑧ 靠它收尾）；`Sentinel.Legacy()` 按 `/{T:` 切 ⇒ **尾部七列位置一个都不动**。自证 =「把这两个块删掉 ⇒ 与旧行逐字节相同」（实测，见下「基线」那节）。

### 实跑输出（现读）
```
$ bash d:/4/Unity/工具/ruleprobe.sh scan D:/4/_tmp_view/w16_scan_new.txt   → wrote … lines=1121
/{AM: 出现 30 行 · /{TAIL: 出现 88 行
deal/pool///3//0/0//////{T:Raw=an enemy|…|PickMost=-}/{TAIL:stun it}/{SRC:Deal 3 damage to an enemy and Stun it};
stun/pool///1//0/0//////{T:Raw=it|…}/{SRC:stun it};
```
⇒ 病灶那两句现在都看得见：`Amount` 之外有了 `AmountMax`，「**…and stun it**」那半句以 `/{TAIL:…}` 出现。

### 区间伤害现在怎么比
`A1759` 的 TSV 里**原版取值含区间（`\d+-\d+`）的步骤 = 647 条，其中 🔴 104 条** —— 那 104 条此前**只比得上下界**；现在我们的 op 带 `/{AM:…}`（现读 30 条），上界可比。

### 🔴 基线：**没有重生成、三个指纹没有重钉 —— 这是有意的**（`_tmp_view/w16_attr.py` 逐行归因）
`saved 基线 vs 新 scan`：**919 行逐字节相同 · A 类 115 行**（只多了我这两个块，删掉即逐字节相同）·
**B 类 86 行**（`unparsed`/`partial`/`ops` **真变了**，例 `AM1` 的 `Talent: Lord Commander` 从「不解析」变成一条 `create` op）。
B 类 = **另一个写手在改 `RuleEngine/Core/EffectText.cs` + `CardDef.cs`（文件 mtime 14:04，本件一个字节没碰 `RuleEngine/**`）**。
⇒ 按 `PinnedParseMd5` 的纪律「**重钉前必须先确认这批变化是对的**」，此刻重钉会**把那 86 行未经确认的漂移盖成绿**（正是哨兵 ⑦ 要抓的事）⇒ **停手**。
⇒ **下一步（建议由主对话在同步点、B 类被确认之后一次做完）**：① `ruleprobe.sh scan 工具/ruleprobe/基线/out_baseline.txt` → ② 手改 `PinnedBaselineMd5` / `PinnedParseMd5` → ③ `check` 应为「解析差异 0 行 · 哨兵 0 条失败 · rc=0」。这段已写进 `Scan.cs` 文件头，下一个会话看得到。

---

## A1777 旧 id 残留

### 功能性命中 **34 → 17**，其中**可动的 0 条**（重跑 `_scan_stale_cardids.py` 现读）
口径 = **census §一 那栏「真残留·功能性」（= `verdict=STALE & barelit=1`）**，**34 条全部落在 `工具/**/*.py`**
（§二A 表里那几行 `.cs` 按分类器落在 `comment`，不在 34 里；脚本打印表里另有一个 `STALE-bare` 列（`where=='bare'`）恒 0 —— **两个不是一回事**）。

| 文件 | 改前 | 改后 | 说明 |
|---|---|---|---|
| `_fix_a6_descZh.py` | 7 | **0** | 26 个 id 全部按【集合级】判据换成同卡的现 id |
| `_fix_a7_talent.py` | 2 | **0** | 5 处（含注释里的 `TAU1`×2） |
| `_verify_fixes.py` | 2 | **0** | 18 处 |
| `import_original_art.py` | 6 | **0** | `ALT_ART` 表 6 个键 —— ⚠️ 这是**真功能缺陷**：输出文件名是 `alt_<id>.png`，旧 id 会把**异画命名到别的卡**上 |
| `gen_trait_dict.py` | 16 | 16 | ⛔ **已知假阳性，没动**（它按**卡名** join，旧 id 只是「A1 报告那个命名空间」的出处注记，文件头自己写着） |
| `gen_cards_engine.py` | 1 | 1 | ⛔ **`W15` 的文件**（`:557` 注释里的 `heroId:"AM3"` 举例）。census 写的是 `:542`，**行号已挪** |

### 方法学：**集合级判据真的救了一次**（简报警告的那条）
`_fix_a6_descZh.py` 里有一对 **id 互换**：`UM85`（当时 = *Catechism of Death*）与 `UM_Tactical_Doctrine`（= *Tactical Doctrine*）**换真值后互相调了个**。按「逐 token 映射」会把两条都指错，是 `--filecheck` 的 exp/cur 多重集差逼出来的（末态已按卡名 + descZh 双核）。

### `--filecheck` 末态
`_fix_a6_descZh.py` · `_fix_a7_talent.py` · `_verify_fixes.py` **干净**。`import_original_art.py` 剩 `多 {UM_Terminator:1}` —— 是注释里**点名了一张【当前有效】的卡**（顺带点名 `EC_Terminator` 以消歧，因为新池有两张 `Terminator`；`exp` 靠 `migration_map` 对**同名撞车**会挑错那张，正是 census 自记的误报源①）⇒ **不是残留**。

### 顺带把 `工具/ruleprobe/*.cs` 的旧 id 也清了（简报点名了这一支，`--filecheck` 也报）
`FaithProbe.cs`（实战夹具 `Scenario(pool,"SOR27",…)` 等 8 组）· `WillRun.cs`（12 组）· `PlayProbe.cs`（18 组）· `NonKwProbe.cs` · `Program.cs` · `Scan.cs` —— **末态全部干净**
（`Program.cs` / `Scan.cs` 剩的 `多` 是 2026-10-14「就地换卡」的**有意更正痕迹**，铁律 5 的产物，保留）。每个 id 都用「旧池卡名 == 新池卡名」核过（55 处 + 48 处，零不一致）。
> 🔴 **其中一条是真功能缺陷（不是文案）**：`NonKwProbe.cs:403` 原来 `c.Id == "GOF85"`，而 `GOF85` 现在是 `Snakebite Nob` ⇒ **(b) 真路径那一半【静默跳过】**（只打一行「卡池里没有」）。
> 改成 `GOF86` 后实跑：`(b) 真路径：GOF86 Unleashed TramplaSquig（卡面 At the start of each turn attack a random non Flying enemy）` ⇒ 8 次抽样全跑 ✅。

### 我改不了、交主对话的（逐条 `文件:行号`）—— `Assets/**` 的**文本级**（census §二B）
`Assets/**/*.cs` 的**功能性命中 = 0**（无裸 id 键），但 **29 处断言文案 / 注释点名了错的卡**（行号已现核；左=现指、右=该写）：
`RuleEngineTest.cs:4410` `Dynamic Offensive` 写 `TAU54`（现 `Razorshark Strike`）→ `TAU47` · `:4411` `Crisis Battlesuit` `TAU15` → `TAU11` ·
`:9003` `Shrineworld` `SOR62` → `SOR49` · `:9020` `Prayer` `SOR48` → `SOR39` · `:10395` `Beastboss on Squigosaur` `GOF90` → `GOF91` ·
`:10477` `Beast Snagga Nob` `GOF81` → `GOF82` · `:10935` `Ork Spanner` `GOF14` → `GOF_Ork_Spanner` · `:19041` `Chaos Sergeant` `BL13` → `BL4` ·
`:19053` `Dark Pact of Fate` `BL16` · `:20841` `DS8 Support Turret` `TAU18` → `TAU13` · `:21552` `Flawless Blade Champion` `EC28` → `EC12`。
⚠️ `RuleEngineTest.cs` 是 `W15` 的文件（⛔ 没碰）；`IconSetup.cs:1125` / `CardFaceProbe.cs:90` 属 `CardPresentation/**`（⛔ 也没碰）。

### 两条 census 更正（我改不了那份文件 ⇒ 落在这里）
1. 🔴 **「一次性脚本，再跑会把描述写错卡」不成立**：`_fix_a6_descZh.py` / `_fix_a7_talent.py` 都有一道闸（`cs.get(cid) is None` ⇒ 收进 `bad`；收尾 `if bad: … sys.exit(1)`、**不写盘**）
   ⇒ 实况是「**旧 id ⇒ 报错停手**」（外加一道「现值对不上」的闸），**不会静默写错卡**（但**会**卡住重跑）。`import_original_art.py` 才是**真会写错**的那一支（已修）。
2. `gen_trait_dict.py` 现在是 **16 条**（census 写 12）；`gen_cards_engine.py` 的命中在 **`:557`**（census 写 `:542`）。

---

## A1879 `willrun` 那条红

### 结论：**探针/期望值过期，不是实现缺陷**（判据 = 现读卡池 + 卡面 `desc`）
- `WillRun.SelfCheck` 断言 B 的哨兵卡写的是 **`SOR28` / `SOR36`**，而换真值那趟（`A1758`）之后：**`SOR28` = `Zephyrim`（`Flying. Flank` —— 整张卡【没有付费段】）** ⇒ 实得通道栏只能是空 ⇒ **那条红**；
  而 `SOR36` 现在是 `Exorcist`（`8 ☀: Deal 5 damage to all enemies`）—— 它**恰好也有信仰付费段** ⇒ 那一格是**假绿**（换了张卡照样过）。
- 原版那两个实例的现 id：**`SOR22 Dominion Superior`** · **`SOR34 Celestian Superior`** —— 两句 `desc` 与本节要的形状**逐字相同**。

### 我做了什么
① `WillRun.cs` 哨兵列表 `{"SOR28","SOR36"}` → **`{"SOR22","SOR34"}`**，并在 `SelfCheck` 的 doc 里按铁律 5 写下「⚠️ 2026-10-14 更正：原来写 X，实际是 Y，错因 Z」（含 `SOR36` 那条**假绿**的成因）；
② 同文件 13 处 + `FaithProbe.cs` 2 处同族注记一并换掉；
③ 🔴 **报告那句话已按实况改写** —— 末尾原来只有一句**条件式**（「判据：…= 0 条红 ⇒ rc=0」）却长得像结论（`A1759` 就是照它写成「⑦ = 0 条红」的）。现在**先打这一趟的六个数**，再另起一行写判据。

### 跑完的实况读数（现读）
```
实况读数（本趟现算）：② 0 张 · 段级 收漏 0 / 付费段 0 · ⑤「无消费点」 0 条 ·
  ⑦ 判据自检（A1593）红 0 条 · ⑧ 头→通道键近似自检（A1598②）红 0 条 · ⑨ 收集层 Source 自检（A1694）红 1 条
```
⇒ **⑦ 1 → 0**（本件要的那条红清了）。`rc` 仍 `1`，**是 ⑨**，**不是本件**：逐卡定位 = `ASH34 Avatar of Khaine` 的 `CardDef.SpiritOps` **1 → 2 条**（28 张/28 条 → 28 张/29 条），而 `ASH34` 正是那 86 行 B 类解析差异里的一张 ⇒ 来源是 `EffectText.cs`/`CardDef.cs` 在 **14:04** 的改动（**`W15`**）。等它确认后重跑即可，⛔ 不建议由本件去动 `RuleEngine/**`。

---

## 没做 / 没查清 / 顺手发现
- **没做**：基线重生成 + 三个指纹重钉（理由见 `A1877②` 那节，**有意停手**）。`import_original_art.py` 那 1 处 `多`（有意点名）。`gen_trait_dict.py` 16 + `gen_cards_engine.py` 1（前者假阳性、后者非我文件）。
- **没查清**：`WhileInPlay` 族 30 条 🔴 里**剩 2 条**卡在光环层没对照物（要逐条看是否真缺）；`addTrait` 🔴 42 条里 32 条的卡光环层什么都没有 —— **这批是真是假要另跑一次带新口的对账**才判得了（本件只给「有多少条会变」，⛔ 没替它下结论）。
- **顺手发现**：① `NonKwProbe.cs` 的 (b) 真路径**静默跳过**（已修）· ② `_fix_a6_descZh.py` 的 `UM85`/`UM_Tactical_Doctrine` 那对 **id 互换**（已修）· ③ `import_original_art.py` 的异画命名会挂错卡（已修）·
  ④ 🔴 `Program.LoadPool` 造卡时**没有**传 `inventoryOptions`/`initiative`/`showInCollectionOptions`（`W15` 新加的 3 个可选参数 ⇒ 探针侧全是默认 `-1`）——**探针与引擎的第二个口径差**（同 `A1339` 那一族）。
  它**不影响本件任何一个读数**（`scan`/`layers` 都不读那三个字段），但**将来谁拿探针去量「先手 / 能否进卡组」就会拿到 `-1`** ⇒ 记在这里，⛔ 我没改（改 `Program.LoadPool` 会牵动别人的读数）。
