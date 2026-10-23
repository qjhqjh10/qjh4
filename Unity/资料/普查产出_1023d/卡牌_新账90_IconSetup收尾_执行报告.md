# 卡牌线 · `新账-90`（`IconSetup.Run` 既有红）收尾 —— 执行报告（2026-10-23）

> 执行代理（卡牌线 `新账-90`）。**没跑 Unity / player / `-executeMethod`**（只有主对话能跑）。
> 跑过的检查只有三个：`TMPDIR=/tmp/wf_card bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（末次）；
> `python 工具/gen_cards_engine.py --check` ⇒ **一致 ✅**；`python 工具/gen_icon_plan.py --check` ⇒ **两份产物都一致 ✅**。

---

## 〇、一句话结论

1. 🔴 **接手时已经不是「5 条红」了**：`D1`（`资料/普查产出_1023c/卡牌线_三条收尾_执行报告.md`）先修掉 4 条；
   最后一条 `ASH83|waystone` 在 **21:29:17–21:29:28** 由数据侧补上（`cardface_fixes.json` 的 `descZh | Autarch` 前面加 `路标石。`，
   随后重跑两个生成器）—— **`_tmp_view/t68_sync/r14_IconSetup.log`（21:30:xx，现读最新一份）末尾是
   `ICONSETUP 全部通过`**（`:4508`），④a「新缺口 **0** 条」、④b「控制集 **707** 条 · 抓到 **707/707**」。
2. 本代理**没有重复修一遍**，而是：① **逐条复核「已经绿了」是不是真的**（用 Python 复刻 `ZhGaps` / `RevertControlFor`：
   **缺口 0 · 控制集 707 · 抽掉 707 · 抓到 707**，与 `r14` 逐字吻合）；② 补 **三条修复各自的「改回去 ⇒ 红」判别式**
   （`IconSetup.Verify` 新增 **⑫a~⑫d**）；③ 给 21:29 那笔**没留下报告**的数据改动补**留痕**（`cardface_fixes.json` 新增说明键）。
3. ⛔ **没动** `CalibScale`（仍 **0.842**，`新账-89` 未裁）· ⛔ **没动** ⑨ / ⑪。
4. 🔴 **一条判据分歧要调度台知道**：**`路标石` 不是官方中译** —— 官方 `Card_Trait/waystone` 中译 = **灵魂石**
   （`数据/游戏数据/官方本地化_en+zh-CN.json`）。本项目**故意**把「trait Waystone」与「Spirit Stone 资源」分成两个词
   （trait = **路标石**、资源 = **灵魂石**），理由与判据已落进 `cardface_fixes.json` 的新说明键（见 §五·1）。

---

## 一、逐条（改前红 → 改后 / 改在哪 / 验证读数）

| # | 条目 | 改前（`r12_IconSetup.log`） | 改后（`r14_IconSetup.log`） | 改在哪 | 本代理复核 |
|---|---|---|---|---|---|
| 1 | `②i UM100` 中文 Oath | `0/1`（`:3863`） | **`1/1`** | `IconSetup.cs`：`CountSprite` / `IndexOfSprite`（**短名 + `Atlas_trait_icon_*` 两档都数**） | 逐行核过：中文那侧走 `RewriteMarkTraits` ⇒ 全名，英文走计划表 ⇒ 短名；补 **⑫a** 钉死「哪一档用在哪一侧」 |
| 2 | `②i UM106` 中文 Oath | `0/2`（`:3919`） | **`2/2`** | 同上 | 同上 |
| 3 | `②i UM93` 中文 Oath | `0/2`（`:3975`） | **`2/2`** | 同上 | 同上 |
| 4 | `④a f104d4c2…\|faith` | 新缺口 1 条 | **0 条**；计划表 `descZh` 多一条 `4 能量 → faith` | `工具/gen_icon_plan.py`：`FAITH_COST = [0-9]\s*(?:Energy\|能量)(?=\s*[：:])` + `faith_cost_tokens()` | 复核：运行时那一枚**真的是 `RewriteFaithCost` 画的**（计划表那条**恒空转**）；`--check` 两份产物一致；补 **⑫d** 端到端 |
| 5 | `④b` 灭自证 | `705/719`（漏 14） | **`707/707`** | `IconSetup.cs`：`RevertControlFor` 加 `MentionsDarkPactCard` 跳过（与 `ZhGaps` 同作用域） | **逐条核了那 14 条**（§二）：全部属于「黑暗契约」那一族；今天「不跳过」口径的控制集 = **721**，减这 14 = **707** ⇒ 与 `r14` 逐字吻合 |
| 6 | `④a ASH83\|waystone` | 真缺口 1 条（`descZh` 里 Waystone / Spirit Stone **两个词都没有**） | **0 条** | `数据/游戏数据/cardface_fixes.json` 的 `descZh \| Autarch` 前缀补 **`路标石。`**（现读 = `路标石。①：给予你的所有部队 +1 [攻击]、+1 [远程]和 +1 生命`） | 复核：`Rewrite("ASH83","descZh", 真值)` 画得出 `waystone`；**修之前的原值画不出**（两半一起钉在 ⑫c 里）；补说明键留痕 |

> ⚠️ 「改后」一列**不是我跑出来的** —— 是 `r14` 那一次 Unity 批处理的读数。本代理只做**静态复核 + 断言**（Unity 只有主对话能跑）。

---

## 二、`④b` 那 14 条，逐条核（`r12` 只打出前 8 条，这里给全 14 条）

判据：`ZhGaps` 对「黑暗契约」那一族**整卡 `continue`**（`RuleEngine.CreatePool.MentionsDarkPact`）；
`RevertControlFor` 原来**没跳** ⇒ 这些 (卡,sprite) 对进了控制集、却**永远不可能**被抓到。

```
9a2cadc64c80a6b42912441105bb1cee|Melee        9a2cadc64c80a6b42912441105bb1cee|vanguard
BL8|markOfSlaanesh    BL12|Ranged    BL12|markOfChaos    BL16|markOfChaos
BL31|markOfChaos      BL37|markOfChaos                 BL38|markOfChaos
BL43|Melee            BL43|markOfChaos                 BL57|markOfChaos
b8e81b2fe85cda747a68c4d6326b669c|Melee           b8e81b2fe85cda747a68c4d6326b669c|Ranged
```

核法：按 `MentionsDarkPact`（`Subtype == "Dark Pact"` ∨ 英文名/正文/天赋名含 `Dark Pact` ∨ 中文名/正文含 `黑暗契约`
∨ 抹下划线后含 `markofchaos/khorne/tzeentch/nurgle/slaanesh`）逐张判 —— **14/14 都是那一族**（族内共 41 张）。
`r12` 漏掉的 8 条（日志截断前 8 条）**全部在这 14 条里**，没有第 15 条。

---

## 三、断言清单（新加 `IconSetup.Verify` ⑫a~⑫d，含灭自证）

| 断言 | 钉什么 | 🧨 改回去 ⇒ 怎么红 | 为什么不是自证 |
|---|---|---|---|
| **⑫a-1** | `CountSprite` / `IndexOfSprite` **两档名字都数、不按前缀误吃** | 把 `CountSprite` 只留短名那一项 ⇒ `全名=1` 那半变 0 ⇒ 红 | 纯函数负例（`oathless` 必须 = 0），不读被测数据 |
| **⑫a-2** | `②i` 那三张：英文侧**只该有短名**（`短 5 / 全 0`）、中文侧**只该有全名**（`短 0 / 全 5`），且两侧枚数相等 | 只数一档 ⇒ 另一侧恒 0（`r12` 实测就是 `0/1`·`0/2`·`0/2`）；把中文侧改成写短名 ⇒ 本条红 | 两句话**不可能同时**靠「同一个口径」满足 —— 它与 ②i 用的是同一个 `Rewrite` 输出，但**数的档不同** |
| **⑫b** | **计划表里每一条 token 都还得在今天的卡池原文里**（现读 `2360` 条） | 改 `cardface_fixes.json` 的 `descZh` 却**不重跑** `gen_cards_engine.py` + `gen_icon_plan.py` ⇒「找不到」> 0 ⇒ 红 | 灭自证 = 同一谓词喂「**只塞一条假 token**」的合成计划表 ⇒ 必须**正好 1 条**（恒绿 ⇒ 0 条 ⇒ 红；恒红 ⇒ 真表那半先红） |
| **⑫c** | `ASH83` 的 `waystone` **来自中文正文那一句**（正文 + 计划表 + 替换链三处都得对） | 删掉 `descZh \| Autarch` 的 `路标石。` ⇒ 重跑生成器后 ④a 红；**没重跑生成器**时由本条红（计划表那一枚变空转） | 两半：今天必须 ≥1 枚 / **喂回修之前的原值必须 0 枚** ⇒ 谓词恒真、恒假都过不了 |
| **⑫d** | `f104…` 的 `faith` **来自运行时的「数字 + 能量 + 冒号」句法** | 生成器不再镜像那条句法 ⇒ ④a 红；运行时句法被改坏 ⇒ 本条红（今天 1 枚、去掉 `能量` 必须 0 枚） | 同上：两半结构上不可能同时满足 |

📌 「**抽掉计划表里那一枚 ⇒ ④a 必须认出来**」那一半**已经由 ④b 覆盖**（`faith` / `waystone` **都在它的控制集里**，已现核），
所以 ⑫ 只补**它看不见的那一格**（计划表 ↔ 原文）与**运行时文本**那一段 —— 不重复、也不放宽。

---

## 四、主对话要跑什么

```bash
unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod IconSetup.Run -logFile -
```

**期望**：

* **rc = 0**，末行 `ICONSETUP 全部通过`（不是 `有 N 条没过`）。
* 新增四条 `★ ⑫…【灭自证】` 行，期望读数：
  * `⑫a`：`短名=1 / 全名=1 / oathless=0 / 全名的下标=0`；`英文侧 短 5 / 全 0`、`中文侧 短 0 / 全 5`
  * `⑫b`：`扫了 2360 条 · 找不到 0 条 · 计划表里有而卡池里没有的卡 0 张` · 合成表 `正好 1 条`
  * `⑫c`：今天 `1` 枚 · 修之前原值 `0` 枚
  * `⑫d`：今天 `1` 枚 · 去掉 `能量` `0` 枚
* 旧读数不应变：`⑨` **1.670**（`CalibScale` 0.842 没动）· `②i` `1/1`·`2/2`·`2/2` · `④a` **新缺口 0** · `④b` **707/707** · `⑥a` 两份计划表逐字节相同。
* ⚠️ **跑之前确认计划表与 `cards_engine.json` 是同一批数据**（⑫b 会**故意**在「数据改了、计划表没重跑」时红）——
  今天这两者是同步的（`gen_cards_engine.py --check` / `gen_icon_plan.py --check` 都一致 ✅）。

---

## 五、没查清 / 判不准 / 顺手发现

1. 🔴 **官方中译 vs 我们的词（要调度台知会）**：官方 `Card_Trait/waystone` = **灵魂石**；而**资源 Spirit Stone** 在官方与我们的
   中文里也是 **灵魂石**。本项目**故意**分开：trait 用 **路标石**（`资料/关键词图标/_规则书关键词表.md` + `Core/CardIcons.cs` /
   `Core/Loc.cs` / `Resources/trait_tips.json` 同词），资源用 **灵魂石**。若哪天要把 trait 改成权威官方的「灵魂石」，
   `card_icon_plan` 里必须**同时**加一条 `灵魂石 → waystone`，否则中文卡面那枚图标会**静默消失**（④a 能抓到）。
   ⚠️ 这是**全项目口径**，不是本笔能裁的 —— 本笔只按简报用 `路标石。`，并把理由落盘。
2. **21:29 那笔 `ASH83` 数据改动没有执行报告**（`普查产出_1023c` / `普查产出_1023d` 里都搜不到）。
   本报告 §一#6 + `cardface_fixes.json` 的新说明键 `_2026-10-23_新账90_ASH83_waystone` 把痕迹补上了；
   说明键以 `_` 开头 ⇒ `gen_cardface_fixes.py` 会原样保留（它「顶层 `_` 开头一律带走」）。
3. **`ASH83` 的中文措辞仍是旧译**（`给予你的所有部队` vs 官方 `给所有你的军队`）—— 不在本笔范围，未动。
4. `⑤c` 那 4 张「盘上有图、正文里也出现过那个词、而计划表一条都没用」（`markOfKhorne` / `markOfNurgle` /
   `markOfTzeentch` / `relentless`）**仍开着** —— 前轮已定为**人判项**（不是硬错；`relentless` 那枚画的是 `talent`），
   本笔不碰。
5. `_规则书关键词表.md` 里仍是旧中文词（护甲 / 潜行 / 爆裂 …）—— `D1` 已记「将来把 498 张无官方记号的中文换成官方词时，
   `zh_en` 要同时加 7 条别名」，本笔不动。
6. ⛔ **没跑过真机/Unity**：本报告所有「改后」读数都来自 `r14_IconSetup.log`（主对话那次批处理），
   本代理的断言**只做了静态复核与编译验证**（typecheck 0/0）。
