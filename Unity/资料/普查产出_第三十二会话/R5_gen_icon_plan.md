# R5 —— `工具/gen_icon_plan.py` 收编「官方记号那一代」文本（2026-10-15）

### 改了什么

`Unity/工具/gen_icon_plan.py`（**本次唯一改的源码**；3442 → 3508 行）。共 6 处：

1. **根因①/②（生产侧）** —— 旧 `:2800-2801`
   `<旧逐字>` = `        for field in ("desc", "descZh"):` / `            text = c.get(field) or ""` / `            toks = list(TOK.finditer(text))`
   `<新逐字>` = 上面第 2、3 行之间插 6 行（`:2824-2831`）：
   ```python
   # ── 🆕 2026-10-15：**官方记号那一代 ⇒ 整字段跳过**（判据见 `has_official_marks` 上面那一段）──
   #   `CardIcons.cs:227` 在 `HasOfficialMarks` 就分流到 `RewriteMarks`、**不查本表**；
   #   不跳的话 `TOK` 会把 `[[waystone]]` 读成 token `waystone`（位置 1 匹配）⇒ 整族炸。
   if has_official_marks(text):
       stat["跳过的字段（官方记号）"] += 1
       continue
   ```
2. **新增模块级判据函数** `:2737-2739`（插在 `_a1703_lock_check` 之前）：
   `def has_official_marks(text):` → `return ("[[" in text) or ("((" in text) or ("$$" in text)`
   （= `Core/CardIcons.cs:448-453` 的 python 对家；**全脚本只此一份**，下面 4 处都用它）
3. **`_a1703_lock_check`** `:2757` / `:2765`：两个循环各插一行
   `        if has_official_marks(texts.get(c_, {}).get(f_)):` / `            continue`（**载体不适用**）
4. **`FACE_ICON_COUNT`** `:3128-3147`：`_face_na = []` + `:3133` 起 `if has_official_marks(_txt):` → 打 `⚪ …**跳过**…` + `continue`；循环后加一行汇总
5. **`A1703` 位置锁段** `:3396-3398`：新增 `_a1703_lock_na = [...]`（**只用来打印**，判据仍是 ③ 里那一份）+ `:3402-3405` 打 `⚪`
6. **`DARKPACT_FACE_COUNT`** `:3290` / `:3302-3327`：`_dp_na` 加入；**按字段**判 `_e_na` / `_z_na`，
   `_ok = ((_e_na or _ge == _we) and (_z_na or _gz == _wz))`；灭自证的分母换成 `_dp_ctrl_rows`

行尾：改前 crlf=**0** lf=**3442** → 改后 crlf=**0** lf=**3508**（**纯 LF 保持、没被翻**；二进制读 `io.open(p,'rb')`）

`git diff --numstat`：
```
95      6315    Unity/MyGame/Assets/CardPresentation/Resources/card_icon_plan.json
78      11      Unity/工具/gen_icon_plan.py
95      6315    Unity/数据/游戏数据/card_icon_plan.json
```
（脚本 78/11 —— 不是整篇重写；两份 json 的 6315 删除是**相对盘上旧产物**，见下面「卡面有没有被改坏」第 1 条）

### 跑生成器

命令：`cd D:\4\Unity && python -I -X utf8 工具/gen_icon_plan.py --write`
**rc = 0**（改前 **rc=1、两份 json 一个字节都不写**）。**连跑两次 byte-identical（幂等）**。

关键读数（照抄改前 / 改后）：
- `没认出来`: **952 → 0**（⚠️ brief 里写的 948 是更早的读数，现池实测是 952）
- `锚点自动对齐`: **3 → 0**（那 3 条本来就是 `[[waystone]]` 误读出来的假条目）
- `按卡裸 token`: 296 → **280**
- `有记号的卡`: 940 → **874**
- `跳过的字段（官方记号）`: **558**（新增计数；= 全池带官方记号的字段数）
- 未变：`同字段内 token 互相包含` 0 · `同一个 token 两条来源 sprite 不一致` 0 · `sprite 名盘上没有` 无
- ✅ 五条灭自证控制臂**照旧全部变红**：FACE `13/13` · ASH75 变红 · DARKPACT `37/37` · A1703 五条（`0 条控制臂没变红`）

产物（**都落盘了**）：
- `Unity/数据/游戏数据/card_icon_plan.json` —— md5 **`379d8935329f358d57d8062bb6a234f1`** · 877,202 B · crlf0/lf23055
- `Unity/MyGame/Assets/CardPresentation/Resources/card_icon_plan.json` —— md5 **`19e31e99b8b747a89c4bb752fde2756d`** · 877,138 B · crlf0/lf23055
- 两份的 **`cards` 数组逐字节相同**（规范化后 md5 `cf8ed882de48a98ebed3a58a065abe40`）⇒ `IconSetup` ⑥a 不受影响

### 卡面有没有被改坏

**结论：没有。** ⛔ 没跑 Unity。

1. 🔴 **先纠一件事**：盘上旧产物**不是现池重跑得出来的**（它写于 `descZh` 还在旧世代时）⇒ 直接拿它对账会把
   「**池漂移**」混进来。所以我**先在证据级造了一份「同池改前 baseline」**：把原脚本复制到 `_tmp_view`、
   只改 3 处（`ROOT` 硬编码为 `d:/4/Unity` / 两个 OUT 路径改到 `_tmp_view` / `if WRITE and bad:` → `if WRITE and False:`）
   跑出 `_tmp_view/plan_BEFORE_probe.json`。**它的 stdout 与改前 baseline 逐行相同**（只多 3 行落盘提示）⇒ 可信。
2. **同池 before/after**（`plan_BEFORE_probe.json` ↔ 新产物）：
   - 条目数（item）**3420 → 2449**；卡数 **940 → 874**（66 张卡整个消失）
   - **消失 971 条 · 新增 0 条 · 值改变 0 条**
   - 触及 **507 个 `(卡, 字段)`，全部是 `descZh`**；逐条核过：**这 507 个字段的当前原文全都带 `[[`/`((`/`$$`**
   - 🔴 **反向核对（写脚本从卡池验，没手抄）：971 条里「字段不带官方记号」的 = 0 条** ⇒ **无退步**
   - 66 张消失的卡：改前**只有 `descZh` 一个字段有条目**，且该字段 100% 带官方记号（逐张核过）
   - 971 条里只有 **19 条带真 sprite**（其余是 `""` 的缺口条目）。那 19 条的来源全是
     「**裸 token 规则在 `((Stun_an_enemy))` 这种官方记号【里面】匹配到英文词**」——本来就不该有
     （`24fcaf…|stun` `ASH15|stun` `GSC13|sabotage` `UM16|blind` `ASH80|SpiritStone_1` …）
   - 消失 token：**59 个不同 token**。前 20 = `[rally]`73 · `[talent]`65 · `[flying]`63 · `[armour]`60 ·
     `[ephemeral]`59 · `[flank]`55 · `[stealth]`38 · `[vanguard]`33 · `[blast]`29 · `[strike]`26 ·
     `[remnant]`25 · `[duty]`23 · `[codex]`21 · `[camouflage]`21 · `[shield]`20 · `[shuriken]`17 ·
     `[waystone]`17 · `[slay]`15 · `[pray]`15 · `[artifice]`14
   - **用到的 sprite 名集合 69 → 69，一个都没少** ⇒ `IconSetup` ⑤b（计划表→盘）不受影响
3. **判据我自己又读了一遍**（不是转述）：`Core/CardIcons.cs:227`
   `if (HasOfficialMarks(text)) return RewriteMarks(text, field == DescZhField);` · `:448-453` 判的就是
   `[[` / `((` / `$$` ⇒ 那 971 条**运行时一条也读不到**。
4. 盘上旧产物 ↔ 新产物之间另有 **6 个不带官方记号的 `descZh` 字段**有差异
   （`ASH34|天赋：` · `GSC45`/`GSC56`/`GSC65|破坏→Sabotage` · `SOR71|集结：→集结` · `UM23|典籍：`）——
   **不是本次改动造成的**：同池 before/after 对这 6 个字段**逐条相同**，是 `cards_engine.json` 自己改了。

### 没查清 / 没做的

- 🔴 **`IconSetup.Verify` ④a（`ZhGaps`）在现池上【本来就红】**。我把 `ZhGaps` 的谓词照 `.cs` 重实现，
  用三份计划表各跑一遍：盘上旧产物 **0 条**（当时绿）· 改前同池 **752 条** · 改后 **769 条**（**+17**）。
  `KnownZhGaps` 现在是**空表**、`KnownZhGapsCount = 0` ⇒ 那 752 条「新缺口」早就把 ④a 判红，
  根因是 **`descZh` 换代之后那 558 个字段的计划条目变空**（`ZhGaps` 只数非空 sprite）——**与本次改动无关**。
  新增的 17 条是**同一类假阳性**（中文侧由 `((Stun_an_enemy))` 这类官方模板画、本表看不见）：
  `ASH15|stun` `SAU17|stun` `DA35|stun` `GSC13|sabotage` `UM16|blind` `ASH80|SpiritStone_1` …
  **修它要改 `.cs`（`IconSetup.cs` 的 `ZhGaps` 或它的判据），在本次白名单外 ⇒ 没动。**
- `gen_icon_doc.py` 的产物 `资料/卡面图标_对照与缺口.md` 现在相对新计划表**是旧的**（它的输出路径不在白名单 ⇒ 没跑）。
- 盘上旧产物与新产物之间那 6315 行删除里，**混着「池漂移」**（`descZh` 换代 + 6 个字段的措辞改动）
  与「本次改动」两部分 ⇒ 若主对话要一份**只含本次改动**的对账，用 `_tmp_view/plan_BEFORE_probe.json` 当分母。
- `_tmp_view/` 下留了本次的中间件（**不在仓库里**）：`gen_icon_plan.py.bak_1015` · 两份 json 的 `.bak_1015` ·
  `probe_orig_1015.py`（同池 baseline 那支）· `plan_BEFORE_probe.json` · `gip_before.txt` / `gip_after_write.txt`。

### 顺手发现（由主对话分流）

1. `A1945` 的「可删前置条件②」写的是「`gen_icon_plan.py` 整族改造完（它现在一跑就 `rc=1`、不落盘）」
   —— **这一次它满足了**（rc=0、能落盘）。⚠️ 但条件①（`desc` 也换成官方 EN 列）**仍不满足** ⇒ `A1945` 整体仍不能推进。
   （我不改两张正本，交主对话判。）
2. `工具/_oneoff_1015b/land_1032.py:267` 的 `A2035` 结论是「`card_icon_plan.json` **不该重出**」。
   本次**重出了**。若那句话是说「**画面**一个像素都不会变」——仍然成立；若想说「**盘上那份内容是对的、别动**」——
   则不成立（盘上那份是**旧世代 `descZh`** 生成的，现池重跑不出来）。
3. `IconSetup.cs:524` 有一条 `Check(nCards > 600, …)` —— 新产物 874 张，仍然过。
4. 我**没有**动 `CLAUDE.md` / `项目任务.md` / `任务_卡组卡牌战斗.md`，也**没动 git**、没跑 Unity、没碰任何 `.cs`。
