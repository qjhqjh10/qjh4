# W_卡面残余 · 执行写手报告（`UM_Death_from_Above` 补词 · `DA38 [honour]` 留痕 · `IconSetup ②d` 守卫补牙）

> 2026-10-20（卡面数据第 4 批 · 残余三件）。**只改了**：`数据/游戏数据/cardface_fixes.json` · `工具/gen_icon_plan.py` ·
> `Editor/IconSetup.cs` + 生成器产物（`cards_engine.json` · **两份** `card_icon_plan.json`）。
> **没跑 Unity · 没动 git · 没改两张正本 · 没碰 `Core/**` / `Battle/**` / `RuleEngine/**` · ⛔ 没猜。**
> 备份 / 离线复刻脚本 / 裁图 → `_b_wc_scratch/`（`bk_*` · `verify_2d.py` · `zoom_codex.png` · `zoom_last.png`，gitignore 已覆盖）。

## ① `UM_Death_from_Above`：卡图读数 · 守卫复核 · 改前/改后 · 解析等价性

**卡图亲读**（本批作者自己开的图）`d:/2/Warpforge部队卡片/Ultramarines/4计策/death.png`（900×1200）：
效果区 = `Deal 4 damage.` ／ `⟨蓝圆盘+白星芒+小Ω⟩Codex: Deal 1 additional damage`。裁块 ×3 放大核两处：
· 徽记**紧贴** `Codex:`、中间是**一个空格量级**的空隙 ⇒ 写 `[Codex] Codex:`（token 换图标 + 空格 + 词）与卡面同形，和 `[Shield] Shield` / `[Dark Pact] Dark Pact` 那一族一致。
· ⚠️ **卡面末句 `additional damage` 后没有句号**（第一行 `Deal 4 damage.` 有）—— **没动**，理由见「顺手发现 2」。

**另一条判据（本批新查到、比卡图更硬）**：我们自己的中文表 `d:/warpforge/data/i18n/zh_CN.csv:1029-1030` 的**英文键**
= `"Deal 4 damage.\n[Codex] Deal 1 additional damage."`，与我们的 `desc` **逐字节相同**（含那个句号、`[Codex]` 后无词）；
同行**中文列**已是「造成 4 点伤害。\n**典籍：**额外造成 1 点伤害。」⇒ **中文一直有词、英文一直没有**，本批补的就是这个不对称。

**守卫复核结论**（只读看，⛔ 没改 `RuleEngineTest.cs`）：`RuleEngineTest.cs:10057-10063` 的反例断言在、**不会红**：
· **读代码**：守卫 `CardDef.CollectBareKeywordBody:465` 的①支走 `EffectText.NormalizeIconPrefix`（`EffectText.cs:943-950`，
  正则 `^\[\s*(codex|…)\s*\]\s*`）⇒ 改后该分句归一成 `codex: Codex: …` ⇒ `IndexOf(':')=5>0` 且 `IsBodyKeyword("codex")`
  （`KeywordTable.Codex` **在** `BodyKeywords` 里，`CardDef.cs:278-295`）⇒ **仍提前 return**。
  ⚠️ 顺带更正那条测试注释的口径：`codex` 现在**在** `BodyKeywords` 里，守卫生效靠**①支**、不是「codex 不在表里 ⇒ 恒 null」（两版都 return）。
· **实跑**（离线探针，复刻断言那条 ctor + `keywords=["Codex"]` + type=`tactic`；源码 `d:/tmp/wf_wc_probe/Program.cs`）：

```text
改前 desc=[Deal 4 damage.\n[Codex] Deal 1 additional damage.]          → TriggerOps(codex) = null
改后 desc=[Deal 4 damage.\n[Codex] Codex: Deal 1 additional damage.]  → TriggerOps(codex) = null
对照 desc=[When you gain a Quest Point, deal 1 damage to a random…]    → TriggerOps(codex) = 1 条 cond=[your energy is 0]
```

对照那行（`= 1 条`）证明探针**看得见非 null** ⇒ 断言不会红。**改前 / 改后**（`cardface_fixes.json` 的 `desc` 列**新增键** `"Death from Above"`；`descZh` 没动）：

| | `desc` |
|---|---|
| 改前 | `Deal 4 damage.\n[Codex] Deal 1 additional damage.` |
| 改后 | `Deal 4 damage.\n**[Codex] Codex:** Deal 1 additional damage.` |

**解析等价性（全池 + 逐字段两路）**：· 全池 = `ruleprobe scan` 的 `UM_Death_from_Above` 行
（`基线/out_baseline.txt:1035` ↔ 现在 `scan_now.txt:1035`）= `UM_Death_from_Above|Death from Above|||deal/pool///4/;deal/pool///1/;`
—— **逐字节相同**。· 逐字段 = `ruleprobe card "Death from Above"` 改前/改后 op 两行**逐字段相同**（`deal 4`/`deal 1`、
`target` 同串、`cost=0`、`unparsed=`/`partial=` 均空）。· 计划表 = `UM_Death_from_Above` 条目**逐字节未变**
（`[Codex]` 仍在文本里 ⇒ 不产生死条目、也不会给字面 `Codex:` 再画一枚 ⇒ **不画两遍**）。

## ② `DA38`：留痕写在哪 · 写了什么（⛔ 没改任何值）

1. **生成表** `工具/gen_icon_plan.py` 的逐卡条目 `("DA38","[honour]")`（注释块 `:220-231`、条目本体 `:232`）——
   上方注释 + `why` 末尾各加一句「⛔ **别删：`[honour]` 是个词、不是图标名、靠本逐卡条目才对上**（删条目 / 改 token
   ⇒ 徽记整枚不画）」；判据出处照旧写全 = 还原表 `Unforgiven Redemptor`：`When you gain Quest Point, deal 2 damage`。
   该 `why` 随生成器进产物（`card_icon_plan.json` 的 DA38 `[honour]` 条目 `why`；**`sprite` 一字未动**）。
2. **数据侧** `cardface_fixes.json` 新增顶层说明键 **`_2026-10-20_DA38honour_别删`**（`_` 开头 ⇒ `gen_cardface_fixes.py:178-180`
   原样保留、`gen_cards_engine.py` 只读四列 ⇒ **不影响产物**）。它还**点掉一个真实的误删诱因**：`gen_icon_plan.py` 的 `DA8`
   那条 `why` 写着「2026-09-15 已把卡表这段文字改成 `[Quest Point]`」—— **那句说的是 `DA8`/`DA15`（key = `Apothecary` /
   `Company Veteran`）**，`DA38` **从来不是** `[Quest Point]`（`[honour]` 至今留在 `card_stats.json` 的原始行里）⛔ 别据此把 DA38 也改掉。
   改前=改后：`desc` = `Armour 1. When you gain [honour], deal 2 damage to a random enemy`；`descZh`/`keywords`/计划表 `sprite` 全未动。

## ③ `IconSetup` ②d 守卫：改前（没牙）· 改后 · 🧨 改坏法 · 离线自证

**改前**（`IconSetup.cs:297-299`）`… && ash.IndexOf("1 <sprite", Ordinal) < 0` —— 意图 =「档位数字已烘在图里
（`SpiritStone_1`）、不该再印那个 `1`」。自 `A969` 给图标包上 `<link=…>` 后，**坏结果**是
`1 <link=spiritstone><sprite name="SpiritStone_1">…`、**好结果**是 `…</link>: Give …` ⇒ `"1 <sprite"` **两态都匹配不到** ⇒ 恒真 = **没牙**。

**改后**（照 `②i` 那一族写法，**不绑死标签层数与顺序**）：(a) `CountOf(ash,"<sprite name=\"SpiritStone_1\">") == 1`；
(b) `ash.IndexOf("[Spirit Stone]") < 0`；🔑 (c) **冒号前不许再留数字** —— `StripTags(ash)` 后取 `:` 之前那一段判有没有 `0-9`。

**🧨 改坏法**（必须让它红）：把 `Core/CardIcons.cs:275` 那条 `Regex.Replace(s, @"[0-9]+\s+" + Escape(it.token), tag)` 的 **`[0-9]+\s+` 去掉**（只换 token、不吃数字；改完**第二条**正则 `[0-9]+<token>` 也匹配不上 —— `1` 与 `[` 之间那个空格没了）。

**离线自证**（`_b_wc_scratch/verify_2d.py`：复刻那一支的替换逻辑、两态各跑一遍；`python -I -X utf8`）：

```text
实得(好) = <link=spiritstone><sprite name="SpiritStone_1"></link>: Give +1 melee, +1 ranged and +1 Health to all your troops
实得(坏) = 1 <link=spiritstone><sprite name="SpiritStone_1"></link>: Give +1 melee, +1 ranged and +1 Health to all your troops
对拍：好态 == 现成日志 d:/4/_tmp_view/iconsetup_1020b.log:457 打印的实得串   → True（复刻没走样）
两态可分辨（好 != 坏）                                                    → True
改前 `"1 <sprite" not in s` ：好 True / 坏 True   → 没牙（两态都绿）❌
改后 (a)+(b)+(c)            ：好 True / 坏 False  → 有牙 ✅
剥标签(好)=`: Give +1 melee, …`    剥标签(坏)=`1 : Give +1 melee, …`
```

⚠️ `IconSetup.Verify` **不在那 12 条自检里、本批也没跑**（红线：不跑 Unity）⇒ 上面是**静态等价物** + 离线复刻，真值以调度台手动跑 `-executeMethod IconSetup.Verify` 为准。

## 产物前后差异 · 行尾 · 类型检查 · 没查清

**生成器（固定顺序）**：`gen_cards_engine.py` → `gen_cardface_fixes.py` → `gen_icon_plan.py --write`；
再连跑一次 `--write` ⇒ 两份产物 **md5 逐字节相同 = 幂等**。差异：
· `cards_engine.json` **1126 张 / 只 1 处字段**（`UM_Death_from_Above.desc`）；`count`/`keywords`/`subtype`/数值/`descZh` **零变化**。
· `card_icon_plan.json`（两份 `cards` 逐字相同）**654 张 / 2127 处（未变）**；仅 `DA38` 那条 `why` 文案变（`sprite` 未动）；
  **死条目 26（没新增）**，`UM_Death_from_Above [Codex]` **不在**死条目里。
· `cardface_fixes.json`：`desc` 242 → **243**（+1 新键）· 顶层 18 → **19** 键（+`_2026-10-20_DA38honour_别删`）；其余键**零变化**。

**行尾（`b'\r\n'` / `b'\n'`）—— 没一处翻**（全程 Edit 工具 + python `wb`，**没跑过 `sed -i`**）：
`cardface_fixes.json` **1652/1652 → 1654/1654（CRLF）** · `gen_icon_plan.py` **0/993 → 0/1007（LF）** ·
`IconSetup.cs` **0/544 → 0/565（LF）** · 两份 `card_icon_plan.json` **0/20360（LF，未变）** · `cards_engine.json` 单行。
**类型检查**：`TMPDIR=/tmp/wf_cf bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（改完 `.cs` 立刻跑、收尾又跑一次）。

### 顺手发现（**没动**，交调度台分流）
1. 🔴 **那个词在原版数据里本来就没有 —— 是显示层的活，不是 OCR 丢的。** `zh_CN.csv:1029` 的英文键就是
   `[Codex] Deal 1 additional damage.`（无词）。按 `CardIcons.cs` 里已实读的口径（`GameStaticData__TraitNameToString.c:75-109`），
   原版把占位符展开成 `<link=…><nobr>{图}{词}</nobr></link>` ⇒ **词是渲染出来的**。我们这条链方括号 token 走「整串换掉」
   ⇒ **真·完全复刻**该在显示层把 `[Codex]` 展成「图标 + `Codex:`」（= `Core/CardIcons.cs`，**不在本批白名单**）。本批按房内族约定
   在**数据侧**补词（与 `[Shield] Shield` 一族、以及我们自己的中文 `典籍：` 同形）；**这一族要不要整体挪到显示层 = 跨文件的账**，建议进 A 表。
2. ⚠️ **卡面末句无句号、我们有**：`death.png` 印的是 `additional damage`，而 `desc` 与 `zh_CN.csv:1029` 的英文键**都有**句号
   ⇒ 是 **PnP 印刷省略**、不是我们多写。**没动**（铁律 10 第 3 条：PnP 是印刷品；且 `desc` 是引擎输入）；要按 PnP 删是**一行回滚**。
3. ⚠️ **`ruleprobe check` 现报「全池 1120 张 · 解析差异 103 行」**（`scan` 出 1120 行 / 卡池 1126 张，差 6 张是 `scan` 本来就跳的类别；
   改前=改后行数一致）—— **与本批无关**（本批那张卡的行逐字节相同）。根因：`Core/**` 另有**未提交**的解析器改动，基线
   `基线/out_baseline.txt` 停在它们之前 ⇒ 谁收口谁确认后重建基线（`scan`），否则这个数会一直是红的噪声。
4. ⚠️ **`DA38` 的 `desc`（含 `[honour]`）在 `card_stats.json` 里是裸的、没进 `cardface_fixes.json`** —— 没有 `desc` 覆盖层兜着；
   哪天有人为别的事给 DA38 加一条 `desc` 覆盖，就会顺手抹掉这个 token（② 那两条留痕正是冲它去的）。
5. 计划表里 `UM_Phobos_Lieutenant [Talent]` 等 **26 条死条目**仍在（本批没新增、也没清）—— 既有账，另议。
