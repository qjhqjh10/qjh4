# S9 · GOF 四张「图标被吃掉」的 desc 补回

**结论：4 张全改 · 生成器重跑后 `desc` 真的变了（且只有这 4 处变）· 张数不变 · `descZh` 没动 · 类型检查 0/0 · 既有断言一条都不受影响（唯一那条断言在结算层，结果值不变）。**
顺手查出**第 5 张同类（`UM85 Catechism of Death`，两处裸 `+2` 卡面都是粉拳）—— 按红线只报不改**。

---

## 1. 逐张卡面实据

**判据姿势**：不是只开四张图看一眼，而是把四张的图标**裁块（各 90×90、以 `+N` 右侧那枚为中心）×4 放大**，
与**两枚已知参照**放进同一张对照图（`d:/tmp/wf_s9/grid_icons.png`，上排 A/B/C、下排 D/E/F）并排比：

| 位 | 卡 | 卡图（`d:/2/Warpforge部队卡片/…`） | 卡面印的原文 | `+N` 后面那枚 | 我凭什么这么读 |
|---|---|---|---|---|---|
| A | `GOF50 Tide of Muscle` | `Orks/4计策/Warpforge_50_Tide-of-Muscle.png` | `Draw two troops and give them +1 ⟨图标⟩` | **粉圈白拳** | 与 E 逐像素同形同色（白圈 + 深红底 + 粉色握拳）；与 F 的紫底白枪**明显不同**（底色、字形都不同） |
| B | `GOF53 Get'em ladz!` | `Orks/4计策/Warpforge_53_Getem-ladz.png` | `Give +2 ⟨图标⟩ to your units this turn` | **粉圈白拳** | 同上 |
| C | `GOF_Da_Red_Waaagh` | `Orks/2天赋/Warpforge_01B_Da-Red-Waaagh.png` | `Ephemeral / Draw a troop and give it +1 ⟨图标⟩` | **粉圈白拳** | 同上 |
| D | `GOF_Krumpaklaw` | `Orks/3部队/Warpforge_07A_Krumpaklaw.png` | `Unstable. / Mob: Gain +2 ⟨图标⟩ and ⟨盾⟩ Armour 1 / Vehicle` | **粉圈白拳**（另一枚是**银盾** = 真护甲） | 同上；那半句本来就写对了（`Armour 1`），只补拳那一枚 |
| E | `Genestealer Familiar`（**参照 · 近战**） | `Genestealer Cult/3部队/Warpforge_07_Genestealer-Familiar.png` | `Adjacent units have +1 ⟨图标⟩.` | 粉圈白拳 | **本表早就写过 `+1 Attack`** ⇒ 「粉拳 → Attack」这一步本工程早已落地，这就是同一个写法（`_manual_desc_note` 的 `Genestealer Familiar` 条） |
| F | `Cadre Fireblade`（**参照 · 远程**） | `Tau/3部队/Warpforge_35_Cadre-Fireblade.png` | `… troops have +2 ⟨图标⟩` | **紫圈白枪** | 本表写的是 `+2 Ranged Attack` |

**旁证（数值，防「看着像」）**：六枚图标各自**内盘 r=13 的均值 RGB**（把白圈排除在外）——
A `[106,85,72]` · B `[151,120,119]` · C `[115,87,86]` · D `[168,137,136]` · E `[144,114,113]` → **R 最大、B≈G（粉红系）**；
F `[197,182,203]` → **B 最大（蓝紫系）**。四张与 E 同色系、与 F 不同色系。

⇒ **四张一律按近战写 `Attack`**（写法照本表 `_manual_desc_note` 那条约定与 `_2026-09-16_属性图标_第二批`：
「写入的是我们能解析的规范写法」= 卡面那一枚**画的是图标**、正文写**裸词 `Attack`**，不是卡面字形）。

---

## 2. 改了什么（逐条）

只改一个文件：`d:/4/Unity/数据/游戏数据/cardface_fixes.json` → **`desc` 列新增 4 条**
（这条列由 `gen_cards_engine.py:818-820` 覆盖进卡池；键名照同表先例用**裸卡名** —— 这 4 个名字在
`card_stats.json` 里**各只出现一次**（Goff），不存在跨阵营同名，已实测）。

| 键 | `desc` 改前 | `desc` 改后 |
|---|---|---|
| `Tide of Muscle` | `Draw two troops and give them +1` | `Draw two troops and give them +1 Attack` |
| `Get'em ladz!` | `Give +2 to your units this turn` | `Give +2 Attack to your units this turn` |
| `Da Red Waaagh` | `Draw a troop and give it +1` | `Draw a troop and give it +1 Attack` |
| `Krumpaklaw` | `Mob: Gain +2 and Armour 1` | `Mob: Gain +2 Attack and Armour 1` |

另在 `_manual_desc_note` 里加**两条批注**（同表体例：批注只进 `_manual_desc_note`、不进 `desc`）：
`_2026-10-18_GOF四张图标被吃掉`（证据链 + 为什么这样写）与 `_2026-10-18_GOF四张图标被吃掉_证据`（裁块读数 + 探针对照）。

**没碰**：`descZh`（本来就是「+1 攻击 / +2 攻击」，与改后英文一致）· 任何其它卡 · `prebuilt_decks_full.json`（红线）· 任何 `.cs`。

---

## 3. 重跑生成器的读数

命令：`"D:/2/Warpforge_tools/py312/python.exe" -X utf8 d:/4/Unity/工具/gen_cards_engine.py`
（**没跑 Unity**；先把改前的产物拷了一份到 `d:/tmp/wf_s9/cards_engine.before.json` 做逐字段对拍）

| 项 | 读数 |
|---|---|
| 生成器自检（「表里有、却一张卡都没匹配上」的孤儿守卫） | **`卡面修正表：所有键都匹配上了 ✅`** ⇒ 我加的 4 条键**都真的生效了**（不是挂着的死键） |
| 卡池张数 | **1126 → 1126**（`count` 字段与 `cards[]` 长度都是 1126，**没变**） |
| **逐字段对拍（before ↔ after，1126 张全量）** | **只有 4 处不同，全是 `desc`，正好是这 4 张**：<br>`GOF50` `… +1` → `… +1 Attack`<br>`GOF53` `Give +2 to your units…` → `Give +2 Attack to your units…`<br>`GOF_Da_Red_Waaagh` `… +1` → `… +1 Attack`<br>`GOF_Krumpaklaw` `Mob: Gain +2 and Armour 1` → `Mob: Gain +2 Attack and Armour 1` |
| 卡 id | 用原版 **995** 张 · 自造 **131** 张（共 1126）—— 与改前一致 |
| `descZh` 有没有被带坏 | **没有**：4 处差异里**一处 `descZh` 都没有**；四张的中文仍是「抽两个部队并给予它们 +1 攻击。」等（与改后英文同义） |
| 产物 | `Assets/RuleEngine/Resources/cards_engine.json`（375 KB，由脚本写出，**手工没碰**） |

**额外旁证（离线探针，不跑 Unity）**：`工具/ruleprobe` 的 `seg` 逐条对照 + `check` 全池对拍 ——

* `Draw two troops and give them +1` → `give/pool///0/+1`；改成 `… +1 Attack` → `give/pool///0/+1 attack`。
  两版**都不报 `unparsed` / `partial`**，op 条数、动词、目标都不变。
* `check` 全池：**解析差异 4 行 · 改名 0 行 · 新增 0 · 消失 0**，那 4 行就是上面这 4 张（逐行看过，见 §7 第 1 条）。

---

## 4. 会不会影响既有断言

**grep 全工程 `.cs`（`--include=*.cs`，四种拼写都搜过：卡名 + `GOF50`/`GOF53`/`GOF_Da_Red_Waaagh`/`GOF_Krumpaklaw`）——
命中 14 处，其中【断言】只有 1 处，其余 13 处全是注释。**

| 处 | 是什么 | 判 |
|---|---|---|
| `RuleEngineTest.cs:9228-9249`（`Get'em ladz!` 那一块） | **唯一的断言宿主**：真打这张卡，`a.Attack == 3` · `a.RangedAttack == 0` · 第二个单位也 `Attack == 3` | ✅ **不受影响、仍绿**。理由：改前走 `GivePayload.cs:563-584` 的**裸 `+N` 兜底**（`Attr = "attack"`），改后走 `:551-559` 的 `ReAttr` → `NormAttr("attack") = "attack"`（`:257` 的 `default`），结算层再过 `EffectResolver.cs:5686` 的 `NormalizeAttr`（`"attack"` 原样返回）⇒ **两条路的 `Attr` 逐字相同**；目标 `your units`（复数 ⇒ 全体，`ReAttr` 不在那一步）与 `this turn` 的时长也**都没动**（`ruleprobe seg` 实拍两版的 op 完全同形）。⚠️ 只有那句**注释**「这张的裸 `+N` 判近战本来就是对的」措辞变旧（那半句现在不裸了），**注释不是断言**，且说的事（它是近战）仍然成立 —— 在 `.cs` 白名单外，**没动**，留给调度台裁。 |
| `RuleEngineTest.cs:14056` | 注：真卡池那四张（`GOF50`/`TAU54`/`GOF_Da_Red_Waaagh`/`UM23`）**全是 `drawtype`**，靠的是代词槽**兜底** | ✅ 不受影响：`drawtype` 那一段的句子**一个字没改**（`ruleprobe seg` 实拍 `drawtype … 2/troops` 原样）。**注**：该注释里抄的旧原文（`Draw two troops and give them +1`）随之变旧，**是注释、在 `.cs` 门外**。 |
| `RuleEngineTest.cs:2068` | `CheckTrue(cov.Full >= 120, …)` —— 覆盖率**下限** | ✅ 不受影响：两版**解析完整度相同**（前后都不报 unparsed/partial）。它是个下限、不是精确值。 |
| `RuleEngineTest.cs:11214-11219` | `auraOk.Count == 10` / `auraFail.Count == 0`（光环族计数） | ✅ 不受影响：这四张**都不是光环**。`Auras.LooksLikeAura`（`Aura.cs:199-205`）的四条形状**条条要求 `have`/`has`**（`ReAura:167-169` · `ReCostCombo:158-160` 也带 `have`；另两条是 `flying during your turn` / `Remnants do not disappear`）—— 四张的 desc 里**没有 `have`/`has`**，改前改后都没有。 |
| `Aura.cs:199-306`、`RuleEngineTest.cs:12164-12191` | `BareSignedAmbiguous` 那道闸 + 它那两条断言（`Genestealer Familiar` / `Cadre Fireblade`） | ✅ 不受影响：断言用**字符串**（`"Adjacent units have +1"`）与**另外两张卡**；我改的 4 张不是那两张。 |
| `BattleContext.cs:854,863` · `EffectResolver.cs:60,1655-1656,3422,3426` · `EffectText.cs:756,5502-5503,5574` | 全是**注释/文档**，讲 `them`/`it` 的先行词与 `Draw two troops` 的数量词 | ✅ 无断言。⚠️ 其中几条**抄了改前的原句**（`entity:… give them +1`），现已是旧措辞 —— **都在 `.cs` 里、不在白名单**，没动，报给调度台。 |

**跑过的**：`TMPDIR=/tmp/wf_s9 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 · 编辑器 0**。
**没跑的**：Unity 那 11 条（红线：只有调度台能跑）。**本批的断言宿主是 `RuleEngineTest.Run`** —— 建议在同步点跑它。

---

## 5. 那句「卡面也裸」的订正痕迹

`d:/4/Unity/资料/PnP卡图_逐张对账_0915.md:560`（**只动了这一行**，diff 1/1）：

* **原文**：`| \`UM85\` · \`GOF_Da_Red_Waaagh\` · \`GOF50\` · \`EC37\` | 不动（照旧） | 英文**本来就是裸 \`+N\`**、卡面也裸 —— 要不要写属性名是**我们的选择**，不是「抄漏」 |`
* **现文**：结论格改成「🔴 **2026-10-18 更正：「卡面也裸」是错的** ⇒ 这一格拆成三档」，判据格写全了
  「原来写 X · 实际是 Y · 错因是 Z」：`GOF50`/`GOF_Da_Red_Waaagh` 的 `+N` 后面**印着粉圈白拳**（给了卡图路径）⇒
  2026-10-18 已补 `+1 Attack`；`EC37` **本来就不是裸 `+N`**（`desc` 里是 `-1 🗡 and -1 🔫`）⇒ 当年拿它当同类是举错了；
  `UM85` 同类、**未改**（指向本文件 §7）。
* **错因**（照实写进那一格了）：当年只拿「中英属性序列」那条正则当判据（**正是该文件上一节自己在批判的「正则误报」**），
  **没开图看那一枚到底是什么图标**（铁律 7）。
* 行结构核对：那一行**未转义的竖线 = 4 条**（3 列），与表头一致；文件行尾**仍是 LF**（`CRLF 0 · LF 836`）。

---

## 6. 行尾核对

| 文件 | `git -C d:/4 diff --numstat` | 字节级行尾（二进制读） |
|---|---|---|
| `Unity/数据/游戏数据/cardface_fixes.json` | **8 / 2** | **CRLF 1453 · LF 1453 · 孤立 LF 0** ⇒ 全篇仍是纯 CRLF ✅（改前 1447/1447；+5 条新行 + 每条 `\r\n` 计数一致） |
| `Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json` | **1 / 1** | 单行 JSON，脚本写出（生成器口径不变） |
| `Unity/资料/PnP卡图_逐张对账_0915.md` | **1 / 1** | **CRLF 0 · LF 836** ⇒ 仍是纯 LF ✅ |

`cardface_fixes.json` 用 **Edit 工具**改（没用 `sed -i`、没用 python 文本写），改完 JSON 仍可 `json.loads`。
生成器重跑**只多改了 1 个文件**（`cards_engine.json`），工作区其余改动全是别的写手的（`git status` 已核）。

---

## 7. 顺手发现（**都没改**）

1. 🔴 **第 5 张同类：`UM85 Catechism of Death`（Ultramarines）—— 两处裸 `+N`，卡面两处都是粉拳。**
   * 我们池里 `desc` = `Ephemeral. Give +2 to a friendly troop, or to your Warlord this turn. Oath 3: Give it an additional +2`
     （**两个 `+2` 都是裸的**）；`descZh` = 「…+2 近战攻击，…誓言 3：额外给予它 +2 近战攻击。」（中文早就写对了）。
   * 亲读卡图：`d:/2/Warpforge部队卡片/Ultramarines/2天赋/Warpforge_21_Catechism-of-Death.png` ——
     `Give +2 ⟨粉圈白拳⟩` 与 `Give it an additional +2 ⟨粉圈白拳⟩`（同一枚粉拳，与 §1 那四张同形）。
   * ⇒ **同类缺陷、按红线没改**，交调度台裁。（顺带：那张卡面上 `Oath 3:` 前面还印着一枚**徽记图标**，
     我们 `desc` 里只写 `Oath 3:` 没写记号 —— 属另一类「图标没带走」，一并报。）
2. `PnP卡图_逐张对账_0915.md:560` 那格里的 **`EC37` 是举错的**（它 `desc` 里是 emoji 记号 `-1 🗡 and -1 🔫`，
   本来就不是裸 `+N`）—— 已写进订正格，**没有另外改别的行**。
3. **`.cs` 里有 5 处注释抄着改前的旧原句**（`EffectResolver.cs:3422` · `EffectText.cs:5502-5503`/`5574` ·
   `RuleEngineTest.cs:14056` · 以及 `RuleEngineTest.cs:9228/9243` 那句「裸 `+N`」）——
   **在 `.cs` 白名单外，一字未动**，报给调度台（措辞变旧、不是缺陷）。
4. **`card_icon_plan.json` 不需要重跑**：已核（`d:/4/Unity/数据/游戏数据/card_icon_plan.json`），
   这 4 张里只有 `GOF_Krumpaklaw` 在表内，且只有 `Mob:` 那一条记号；本批**没有新增任何方括号记号/符号** ⇒ 图标计划无变化。
5. 卡面渲染层对**裸词 `Attack` 无特殊处理**（`Core/CardIcons.cs` 只管 `[方括号]` 记号与符号，`Core/CardText.cs` 无该词）⇒
   改后卡面会把 `+1 Attack` **按文字印出来**。这与本表既有约定一致（`_manual_desc_note` 明写规范写法就是 `Gain +1 Attack`，
   池内 100+ 张已这么写，参照卡 `Genestealer Familiar` 就是 `… have +1 Attack.`）—— **不是我引入的新形态**。

---

## 8. 没查清的部分

1. **「卡面按文字印出来」这件事我没有实机验收**（不能跑 Unity，也不能出图）——
   结论是从「池内 100+ 张早就这么写 + 参照卡 `Genestealer Familiar` 同写法」推的，**没有并排渲过一张**。
   真要看，得在同步点用 `CardBaseDemo.Run` / `CardFaceProbe` 抽这 4 张渲一次（铁律 10 第 6 条那条）。
2. `UM85` 我**没有**核它的其余部分（比如 `Oath 3:` 前面那枚徽记到底是什么、要不要写记号）——只核了那两个 `+2`。
3. 我**没有**全池扫「还有没有别的裸 `+N`」——本轮范围就是这 4 张 + 顺手核到的一张。
   若要收口，建议按 `_合并总表.md` 的独立抄录扫一遍「`±N` 后面没跟属性词」的全池清单（那是可派的活）。
4. **Unity 那 11 条自检一条都没跑**（红线）——本节所有「不受影响」是**读码 + 离线探针**推的，
   不是跑出来的。**正式验收 = 调度台在同步点跑 `RuleEngineTest.Run`**（本批的断言宿主）。
