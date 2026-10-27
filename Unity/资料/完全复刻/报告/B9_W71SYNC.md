# `B9_W71SYNC` 报告（同步点执行清单 · 只读 · 2026-10-27）

> 交付物（主）：`资料/完全复刻/判据/同步点执行清单_第八十六会话.md`
> 本文件只作摘要。⛔ 本代理**没跑 Unity**、**没跑 `--fix`**、**没动 git**、**没改基线/指纹/账主表**。

---

## 一、结论

本会话「已接线但没跑过 Unity / 没重钉基线」的待验项，汇总成 **47 个可执行步**，按 5 段串行：
**Gate0（停写手）→ B 裸导入（shader 首编）→ C 账 → D 探针三颗指纹 → E 14 个宿主**，合计 **≈50–65 分钟**。

四条**顺序硬约束**（写错顺序会白跑或钉错基线）：

1. **裸导入（不带 `-executeMethod`）必须排在所有宿主之前** —— `WildcardShine.shader` 从未编译、两个新 `.cs` 无 `.meta`、Coffee dll / 3 个 `.anim` / 11 场 `Sprites/` 全是首导。
2. **`play` 指纹重钉必须排在 `W69ENG4` 回来并归因之后** —— 它改的是行为（Uprising / 死亡摘增益 / MissActions），会再动一次指纹。
3. **`--fix` 只能在没有 `.cs` 写手 + 补丁全合完时跑**，且它的**第一遍输出是修前快照**，必须复跑看真实漂移数。
4. **`--fix` 修不了那 118 条失败**（27 条「落点没有 `文件:行`」+ 91 条落点形态问题）⇒ 它们要**单独立账**，不能当「复刻欠账」。

**两张差异卡已逐张确认「改前错、改后对」**（实跑补证）：
`BL64` 改前 `playRc=18`（打不出去），改后 `rc=0` 且引擎日志显示按卡面真打了一记（`dmg=2`）；
`DA42` 改前打出当刻就 +3 任务点并造 1 张隐秘进牌库，改后按原版「附魔族只挂不跑」。⚠️ 但 `DA42` 的挂载仍缺模板（`A1a` 未做）⇒ **A1a 落地后要再钉一次 play + 一次池指纹**。

---

## 二、证据（文件:行号）

**本次现跑读数（只读）**

| 读数 | 值 | 命令 |
|---|---|---|
| 解析/池面 | `rc=0` · 全池 1121 · 解析差异 0 · 哨兵 0 失败 · 输出指纹 `c9a64798…` · 池 `c2ee753d…` | `bash 工具/ruleprobe.sh check` |
| 结算面 | `rc=1` · 现算 `e7f1de8f…` vs 基线 `0766fbe1…` · 差异卡 **2**（`BL64`/`DA42`）· 电池 586 逐字节一致 | `bash 工具/ruleprobe.sh play check` |
| 账闸 | **A 3,795 · 通过 2,907（告警 184）· 漂移 770 · 失败 118** | `check_ledger_points.py --all-A` |
| 另三闸 | 缺注解 79 · `A×缺口词` 候选 185（强 2）· 本地化 0 重复 | `verify_annot` / `check_a_vs_diff` / `check_loc_dupes` |
| 待合并补丁 | **0**（19:36 现读；19:30 前还有 `W61TARG`/`W63WC`/`W67CENTER`，期间已被逐个合走） | `find 账/_parts -maxdepth 1 -type f -name "*.tsv"` |

**判据出处**

- `调度台日志.md:5938`（「shader 一次都没编译过 ⇒ 同步点要验（含 AlwaysIncluded 包设置）」）· `:6157` · `:6354`（`.meta` 缺失）· `:6503`（W51SHINE2 的待同步点三项）。
- `报告/B9_W16SHINE_扫光统一.md:206-231`（shader 编译三处风险 + 该跑哪几个宿主）。
- `报告/B9_W43DA42_附魔全链.md:26-39`（`DA42` 逐列归因）· `报告/B9_R2DA42.md:46-47`（`A1a` 只能在同步点做）。
- `报告/B9_W27ENG_ta短路与主动能力已用.md:141-174`（`BL64` 归因 + `RuleEngineTest.cs:37253` 必须先改的那一行 —— **调度台已改**，现读 `u.ActiveAbilityUsed = true`）。
- `报告/B9_W24ASSERT_K04断言.md`（17 条从未跑）· `B9_W15CVIEW_CardView五条.md:117`（F 组 ~47 条）· `B9_W4NFG_NestedFadeGroup.md:26`（12+4）· `B9_W50ENGT_引擎断言.md:185-193`（E4 36 条）· `B9_W36SHINEA_扫光断言与宿主.md`（77 条）· `B9_W66MID_段位中段断言.md`（+6）。
- `交接_第八十五会话.md:157-176`（14 宿主的上一次合计，本清单拿它当**参考值**而非基线）。
- `工具/ruleprobe/PlayProbe.cs:3852`（`PinnedPlayMd5`）· `工具/ruleprobe/Program.cs:251`（`PinnedPoolMd5`）· `:350`（`PinnedParseMd5`）· `:285`（`PinnedBaselineMd5`）。

---

## 三、没查清的部分（如实记）

1. **本会话新增断言的确切条数没查到** —— 只能给估算（`CardFaceAssertE5` 64 → ~127 · `BattleScene` +19 · `DeckScene` +40 · `ShellScene` +7 · `MainMenuScene` +4~6 · `CollectionScene` +36 · `RewardsScene` +15 · `RuleEngineTest` +36）。**只有跑一次才知道真值**；清单里已经把「别拿旧合计当基线」写成红线。
2. **`W63WC` / `W65ABIL` / `W68ENG3` / `W69ENG4` / `W70VOPT` 在 19:55 仍无回执** —— 它们的落点会不会再改 `play` 指纹、再加断言，我判不了。清单的 Gate0 P1 把「全部回执」写成硬前置。
3. **`WildcardShine.shader` 能不能编译没验**（禁跑 Unity）。只做了两件静态核对：`Shader "CardPresentation/WildcardShine"`（`:59`，与断言里的 `Shader.Find` 串一致）· 花括号 13/13 配平。语法错只能等阶段 B。
4. **首趟导入的耗时是估计**（2–6 min），未实测。
5. **`W59ARENA` 那 2 场建场要不要在本轮做** —— 判据齐、但会改 `ArenaPrefabs/*.prefab` 与 `Scenes/*.unity`，且用户对战场线说的是「以后再做，到时你询问我」⇒ 我在清单 §9 标成「需调度台自决或问用户」，**没有替它拍板**。

---

## 四、清单条数

**47 步** = 5（Gate0 前置）+ 4（准备）+ 1（裸导入）+ 6（账）+ 6（探针/指纹）+ **14（Unity 宿主）** + 7（鉴别力验证，随宿主做）+ 4（收尾）。
另附 **9 条「明确不在本轮做」**（每条给了「为什么」与「判据/何时做」）。

---

## 五、顺手发现（请调度台分流；⛔ 我一条都没动手）

1. **`W22GLOW` 的断言写了不跑**：`Shell/UIBorderGlow.cs:422 SelfCheckViewUv()` **全仓 0 调用点**（`RewardsScene` 只断「组件挂着」）⇒ 那条「前提硬检查 + 结论」永远不执行。
2. **`W61TARG` 的断言写了不跑**：`Core/Loc.cs:3814 CheckTargetedText()` **全仓 0 调用点**（`BattleScene.cs` 里没有）⇒ `Card_Targeted/*` 31 条键今天**零覆盖**；片段在 `报告/B9_W61TARG_卡牌目标文本.md` §4·4，可直接粘。
3. **`账/_parts/` 顶层还堆着 12 个 `_gen_*.py`**（是各批的补丁生成器，不是补丁）。它们不会被 `merge_ledger_parts.py` 吃进去，但会让「看目录判还有未合并补丁」的人误判一次；顶层 `.tsv` 现在是 0。
4. **三个新文件缺 `.meta`**（`WildcardShine.shader` / `CardDisplayLevelUp.cs` / `LoadableRef.cs`）。其中 `CardDisplayLevelUp` 是 **MonoBehaviour**（`Effects/CardDisplayLevelUp.cs:81`）⇒ 首趟导入后才会拿到脚本资产；若有人在此之前手造 GUID，会与该组件在场景里的引用对不上。
5. **`play DA42` 现读日志里有一句「附魔没挂上」**（模板不在池）⇒ 重钉 `play` 时**必须在常量注释里写清「A1a 落地后还会再变一次」**，否则下个会话会把它当成新回归。
6. **`check_ledger_points` 的 118 条失败里 27 条是「落点里没有 `文件:行`」** —— `--fix` 按定义修不了（它只改行号）。这批要单独立账为「落点形状」，否则会长期占着「失败」数、把真回归埋掉。
