# A 表已收口 · 第十九会话

> 本文件是 **2026-10-11（第十九会话）** 的销账：从 `资料/任务_卡组卡牌战斗.md` 整行搬出，**原文一字未改**。
> 每条后面括号里写「谁做的 / 关键结论 / ⚠️ 还欠什么」—— 那几句是**新增的**，原文本身没动。
> 🔴 **红绿以收口那次实跑为准**（`RuleEngineTest.Run` / `BattleScene.Run`），见本文件末尾「收口实跑」一节。
> 📌 规矩（与 `资料/历史/A表已收口_*.md` 同族）：正本只放**待完成的账**；整条不再开着的搬来这里。

---

| 🆕 **`A1279`** | ⚠️ **`A1250` 让 `Company Champion` 形成【自链】+ 那一路的可达性没数全**（`G1` 报，⛔ 没改）—— ① `Slay: Deal 3 damage to the enemy with highest Attack` 打死人 ⇒ **再触发自己的 `Slay`**；⚠️ 原版同形、由 `EffectChain` 截断 ⇒ **不是环**，但**收口跑 `RuleEngineTest.Run` 时要留意有没有变慢**。② 🔴 **可达面比预想窄**：21 张 `Slay` 卡里「`Slay:` 之外还自带能打死单位的文字」的 **`0` 张**；能走的就是 `Company Champion` 那一张。⚠️ 另有「**`Slay` 被光环/天赋授予后再获摧毁能力**」那一路 —— **`G1` 没逐张数**。⇒ **要做**（铁律 11）。出处 → 同上 §5 / §8。 🔴 **2026-10-11 第十三会话·只读现核：判 (β)，且账上口径要订正两处** —— **① 21 张复现**（`keywords` 含 `slay` **9** + `desc` 含 `Slay:` **19** ⇒ 并集 **21**，与 `G1 §5` 吻合）。**② 本行「`Slay:` 之外还自带能打死单位文字 = 0 张、能走的只有 `Company Champion`」【低估】**：另有 **`EC28 Flawless Blade Champion`**（攻 6）正文 = `Slay: Give +1 [Might] to all friendly troops.` ＋ **`Ecstasy 3: Attack a random enemy.`** —— 后半句**在 `Slay:` 之外**，它 100% 能打死单位（`EffectText.cs:4817-4827` ④ 号正则 `^attacks?\s+(.+)$` ⇒ `forceattack` ⇒ `EffectResolver.cs:1595-1607 TryAttackOnce` ⇒ `RuleCore.DeclareAttack` 尾段 `:4020 TryCreditKill` 就算击杀）⇒ **它自己的 `Slay` 再响**（自链同 `Company Champion`）。⚠️ 严格说「造成**伤害/摧毁**」这个口径**确实还是 0**（`EC28` 走的是**攻击**口径）⇒ **本行该把口径拆成两条写**，⛔ 别只写一个「0 张」。**③ 本行「授予那一路没逐张数」今天粗算了**（卡面正则 + `subtype`/`faction`）：督军那一支**无可达对**（能授的 6 张里 3 张给督军，而全池 57 张 `Warlord` 里正文含伤害/摧毁的 3 张都不是 BL/GOF/GSC）；**可达到的至少 2 组** = `EC52 Pledge to the Dark Prince`（给一个**友方部队**）× {`EC19` `EC12` `EC33` `EC38`} 与 `GOF90 Beastboss on Squigosaur`（给**友方野兽**）× `GOF96 Gargantuan Squiggoth`（`Stomp. Rally: … Mob: Deal 1 damage to all enemies`）；落点 `Core/Aura.cs:749-766 GrantEmbeddedAura`。⚠️ **标记**：这是**卡面正则粗扫**、没逐张读正文、没逐跳核「那一下击杀会不会被 `TryCreditKill` 记上」。⇒ **本笔的真活 = 先改口径 + 再用【真卡夹具】补断言**（现有夹具照不到）。⚠️ 本行那句「收口时留意 `RuleEngineTest.Run` 有没有变慢」**判不了**（禁跑 Unity）。出处 → 同上。 |

> ↳ E3 做**（2026-10-11）：口径按本行要求**拆成两条**钉住；新增 `TestA1279SlaySelfChain` + 3 个助手。⚠️ **口径③（授予那一路）只钉了存在性** —— 目标过滤放不放行那一跳没逐条核。⚠️ 本行那句「收口时留意 `RuleEngineTest.Run` 有没有变慢」→ 见文末收口实跑。

| 🆕 **`A1380`** | ⚠️ **`ruleprobe` 的 `check` 默认基线路径少两级**（第十四会话 `ruleprobe三笔` 写手顺手查出）—— `工具/ruleprobe/Program.cs:349` 的默认值会落到 `工具/ruleprobe/bin/基线/out_baseline.txt` ⇒ **不带参数跑报 `!! 没有基线 …`、`rc=2`**。⚠️ **是「响的」不是「静的」**（不会静默给假绿），且日常靠 `ruleprobe.sh` **显式传路径**躲过 ⇒ 当时判「超范围、没改」。⇒ **要做**（铁律 11）：把默认路径修对。出处 → `资料/普查产出_第十四会话/W_ruleprobe三笔.md` 顺手发现 1 |

> ↳ E1 做**（2026-10-11）：`Program.cs` 的 `DefaultBaseline()` 改成按名字往上找；**不带参直接跑通**（改前 `rc=2` → 改后 `解析差异 0 行`·`rc=0`），文件/目录缺失两条失败路各测一遍。

| 🆕 **`A1383`** | ⚠️ **`ruleprobe` 的 `filter` 模式对「没有 `choosecard` op 的卡」零输出**（第十四会话 `ruleprobe三笔` 写手顺手查出；⚠️ 第十三会话 `W9` 的「顺手发现 4」**可能已记过** ⇒ **收口时并账、⛔ 别记两份**）—— `filter "Primaris Intercessor"` **一行都不打**。⇒ **要做**（铁律 11）：查清是「本来就没 op」还是「过滤条件写窄了」。出处 → 同上 顺手发现 2 |

> ↳ E1 做**（2026-10-11）：两问都答了 —— ① 「本来就没 op」现在**出声**（`!! (UM14) 没有任何会问玩家的 op`）；② 「写窄了」也真存在（`chooseone`/`chooseeffect` 原来一条不打，ASH83/TL51 现在都出声）。主路径 586 张照旧。

| 🆕 **`A1424`** | ⚠️ **`CreatePool.cs:723-729` 记的「英文侧 128 处」复算不出来（算得 122）**（第十四会话 `A1405` 写手顺手查出）—— 写手那一趟复算器**故意简化了 `Singular` 与天赋名抽取**（因为两侧同用一个复算器 ⇒**差值 0 的结论不受影响**），所以 128 vs 122 的差**可能只是简化造成**、也可能是账上那个数过期。⇒ **要做**（铁律 11）：**用真代码重算一次那个数**，对不上就把账上那个数就地订正。⚠️ 中文侧写手复算 **132** 与账上「128 → 132」**逐字相同** ⇒ 复算器不是空转。出处 → `资料/普查产出_第十四会话/W_A1405相关卡文案.md` 顺手发现 5 |

> ↳ E2 做**（2026-10-11）：用**真编译的引擎代码**复算 ⇒ 英文 **125/64** · 中文 **132/67**，与 `RuleEngineTest` 钉的 4 个数逐字相同。**128 是过期、122 是简化假读数**（把 `A1078` 那半段停掉重编即退回 128/67）。另订正：+4 → **+7**，且「唯一一处可见差」不成立（逐张 dump 是 4 族 7 张）。

| 🆕 **`A1466`** | 🔴🔴 **`ruleprobe.sh` 会拿【旧 dll】接着跑 ⇒ 探针读数的「0 差异」可能是假绿**（第十四会话 `A1447/A1448` 写手实测 · ⛔ 只报未改）—— 它**只判「dll 在不在」**（`工具/ruleprobe.sh:20-24`），而**构建会被别人半成品的源码打断**（本笔实测一次 `CS1525`，来源 = 另一个写手当时正在写的 `工具/ruleprobe/WillRun.cs`）⇒ 构建失败时它**照样用上一次的 dll 跑**，给出「**解析差异 0 行**」这种**看起来全绿**的读数。🔴 **为什么这条最要紧**：`ruleprobe` 是**本会话所有引擎改动的验收工具**（`A1369`/`A1435`/`A1439`/`A1447`/`A1452` … 全靠它）⇒ **仪器会静默报绿 ⇒ 那些读数都要打问号**。⇒ **要做**（铁律 11）：脚本要**判构建是否成功**（拿 `dotnet build` 的退出码），失败就**出声并停**，⛔ 不许「dll 在就接着跑」。⚠️ 落在 `工具/ruleprobe/**` —— **现在有写手（`A1459`）** ⇒ **等让路**。出处 → 同上 顺手发现 6 |

> ↳ E1 做**（2026-10-11）：`ruleprobe.sh` 改判 `dotnet build` 退出码，非 0 ⇒ 报错误落点 + `exit 1`。**改前**：构建被打断仍拿旧 dll 跑 ⇒ `解析差异 0 行`·`rc=0`（假绿）；**改后**当场停下。🔑 **当天真兑现一次**：19:06 另一个写手的 `CreatePool.cs` 引用未写出的符号，新脚本当场停住。

| 🆕 **`A1470`** | ⚠️ **`CreatePool.cs:837` / `:879` 那两份天赋段剥离，与 `HandledByOtherLayer` 是【同一条规则的两次落笔】**（第十四会话 `A1449/A1450` 写手顺手查出 · ⛔ 只报未改）—— ⇒ **两处写同一条规则 = 迟早不一致**（本仓铁律）。✅ **本笔没引入第三份**（新加的 `BareBodyText()` **直接转调** `HandledByOtherLayer`，**一行新词表都没写**）。⇒ **要做**（铁律 11）：把 `CreatePool.cs` 那两处**收口到同一处**。出处 → 同上 顺手发现 3 |

> ↳ E2 做**（2026-10-11）：`StripTalentSegments` 改成**一次逐段扫**，删掉 `StripBareTalentSegment`，新增唯一判据 `TalentCutAt(seg, talentName)`。**全池 1126 张 × 英/中两趟逐张 dump 与改前逐字节相同**；⚠️ 唯一（故意的）行为差：`Talent:` 后接换行的写法从前 hits=0 → 现 1，而**真池这种文本 0 处**。

| 🆕 **`A1477`** | 🔴 **探针新缝：`ruleprobe check` 的 dump 【不打 `Target`】**（第十四会话 `A1467` 写手查出）—— `A1467` 改的**正是「目标解析」**（`T=[enemies]…n=1` → `T=[all enemies]…n=0`），而 **`check` 的 dump 里没有 `Target` 列 ⇒ 它一个字看不见**（`check` 只报了别人那 1 行）。🔴 **后果**：凡改「目标怎么解」的笔，`check` **给不出任何读数** ⇒ 那类改动会**静默过关**。⇒ **要做**（铁律 11）：给 `check` 的 dump **补 `Target.*` 列**（`A1467` 写手只能**自建**一份带 `Target.*` 的签名 dump 才拿到读数）。⚠️ 与 `A1459`（`willrun` 补「无头」档）、`A1466`（`ruleprobe.sh` 拿旧 dll 跑）**同一条链** —— **「探针看不见哪一类改动」这件事本身还有缝**。⚠️ 落在 `工具/ruleprobe/**`。出处 → `资料/普查产出_第十四会话/W_A1467ReLose.md` §③ |

> ↳ E1 做**（2026-10-11）：`Scan.cs` 每条 op 末尾追加目标块 `/{T:…}`（＋`/{DW:…}`），`Program.cs` 加 `TargetCols`/`TargetText`/`TargetBlock` + 哨兵④。**改前**去掉 `AM48` 的 `All` ⇒ A/B `diff 0 行`（瞎）；**改后** `diff 1 行`·`rc=1`。基线重生成 **922 行纯追加 · 反例 0**。⚠️ **同族第三条缝仍在**（`Tail` 与 `ChooseDeadScope` 打了、`Scan` 不打）⇒ 已另立新账。

| 🆕 **`A1479`** | ⚠️ **`RuleEngineTest.cs:12149` 那句「报 `586/586` 全通」是【2026-10-07 的历史叙述】**（第十四会话 `A1440` 写手指出）—— 而 `A1440` 之后**那句话会误导**（老形状正是这条要治的病）。⇒ **要做**（铁律 5）：**就地补一句**写清「那是旧口径的历史叙述、新读法见 `ReportWillRunMechanism` 的 doc」。⚠️ 落点 `RuleEngine/Editor/RuleEngineTest.cs`。出处 → 同上 顺手发现 4 |

> ↳ E3 做**（2026-10-11）：就地在 `RuleEngineTest.cs` 那句后补写清「那是旧口径的历史叙述、新读法见 `ReportWillRunMechanism`」。纯注释，无行为断言。

| 🆕 **`A1540`** | 🔴 **对战开局：【替换的手卡】都堆在一起**（**2026-10-11 用户真机点名** · 主对话立账）—— 用户原话：「**对战开局，替换手卡都堆在一起**」。⚠️ **与已有账不是同一条**：`A1489①` 是**场上卡的文字重叠**、`A1502①` 是**屏幕中央提示行压面板**，都不是「换掉的那几张手卡挤在同一处」。**还没查清**：是哪一层（换牌面板的卡位布局？被换下的卡的去向？）、原版那几张该摆在哪。**判据从哪来**：`工具/menu_dump.py` 读原版换牌面板那棵树 + `d:/2/tools/decomp_full/` 的换牌/`Mulligan` 那一族。🆕 **2026-10-11 只读诊断代理 `D1` 挖到根因（⛔ 未改）—— 归属 (乙) 位置算错、不是缺层**：链条 ＝ `Battle.unity:2057` 我方手牌 `animateRelayout: 1` ⇒ `HandLayout.Place`（`HandLayout.cs:589-595`）**只开一条 0.2s 补间、不当场落位** ⇒ **同帧**随后 `CardFeel.DealIn`（`CardFeel.cs:1138-1153`）读 `to = tr.position`（补间**还没跑过一帧**）＝ **(0,0,0)** ⇒ 把牌挪到牌堆、再补间回**那个原点**（调用顺序 `BattleDriver.cs:11320` → `:11348-11352`）⇒ **起手牌停在父级原点**（`Battle` 的世界位 (0,0,0) ＝ 画布正中 960,540）；`MulliganPanel.cs:388-391` **同帧**按 `c.transform.position` 建「换」钮 ⇒ **钮全落到牌堆**。**量化对上**：牌堆点 `MyDeckX01/Y01`（`BattleDriver.cs:1336`）＝ (1730,965)、钮应在 (1730,≈1044)，而两轮**真实玩家实拍**量到「换」钮 ≈ **(1723,1039)**、手牌该在的那一排（**y≈920**）**是空的**（`_tmp_view/ui_1019/17_battle.png` · `_tmp_view/player/auto/auto_01_01_接上.png`）。**会自愈**：换牌结束后指针一碰牌就 `Relayout`。**原版判据**：实况 dump `panel_0914b/p3_mulligan_tree.tsv` 里 `Mulligan` 子树**没有卡节点** ⇒ 起手牌**留在手牌层级**（＝原版摆在手牌位）；`MulliganManager__SetupMulliganButton.c:26-32` 也读卡**当时**的 position（与我们同法，差的是**时机/状态**）。**修法要点**：让 `SetCards` 那一趟**先按当场位姿落位**（`animateRelayout` 临时关掉，或给 `Place` 加 `commitNow`）。🔴 **注意**：批处理里 `animateRelayout` **出厂 `false`** ⇒ **自检结构上复现不了**这条 ⇒ 夹具要**显式置 1** 才挡得住。⚠️ **未查到**：`MulliganAnchor` 上那份 `CardsHorizontalLayout`（MB 4881）**谁在读**（decomp 零命中）⇒「原版是否在换牌阶段换布局参数」**判不了**。📌 全文 → `资料/普查产出_第十八会话/D1_开局换牌与进攻卡.md`。⇒ **要做**（铁律 11）。📌 **用户 2026-10-11 真机实测**，优先级 = **插队**。 |

> ↳ 主对话做**（2026-10-11）：修法比账上那条「要点」更彻底 —— ①`HandLayout` 每趟 `Refresh` 记下**算出来的真位姿**（新增 `TryLastPose`）②发牌前 `CommitDealtPoses()` **当场落实**（含掐掉 `Place` 刚开的重排补间）③发牌补间改用新增的 `CardFeel.DealInPose`，**落在真位姿上** —— 实拍量到旧写法收尾时牌停在 **226 设计 px**（手牌档应 165）⇒ 光补落点不够。④顺带治第二个症状：「换」钮落在牌堆（`RefreshAll()` 跑在 `OpenMulligan()` 之前）⇒ 发牌入场**推后到面板建好之后**。✅ 夹具按本行要求**显式置 `animateRelayout=1` + `animateFeel=1`** 才跑得到那条路（批处理出厂 false ⇒ 结构上复现不了）。⚠️ 本行末尾那条「`MulliganAnchor` 的 MB 4881 谁在读 ⇒ 判不了」**仍未解**（属判据不足，不是尾巴）。

| 🆕 **`A1541`** | 🔴 **【进攻卡】一直显示、不能关闭**（**2026-10-11 用户真机点名** · 主对话立账）—— 用户原话：「**进攻卡一直显示，不能关闭**」。⚠️ **与 `A1487` 是同一族但【不是同一件】**：`A1487②` 说的是「**选择防御卡**」面板疑似每回合重弹（且已注明「也可能是 `-wfdrive` 的时序假象」）；本条是**进攻卡**那一支**关不掉**。**还没查清**：原版进攻卡**显示多久 / 靠什么关**（`选择/确认` 钮？倒计时？进下一阶段自动关？）、我们是**没接关闭回调**还是**回调接了但没生效**。**判据从哪来**：`d:/2/tools/decomp_full/` 的进攻卡面板那一族 + `资料/加时与冲突模式_原版规格.md`（`§25` 环境效果卡流程）。🆕 **2026-10-11 只读诊断代理 `D1` 挖到根因（⛔ 未改）—— 归属 (丙) 没有关闭路径；精确说：面板关得掉、【面板里那几张卡】没有**：`BeginOffensivePhaseIfAny` 把 4 张进攻卡建成 `_choosePanel` 的子件、**存在局部变量 `views`**（`BattleDriver.cs:4532`），而 `OnOffensiveDone` 只 `Close()` ＋ 开打（`:4603-4614`）**一句 kill/hide 都没有**；`CardChoicePanel.Close()` / `SetVisible(false)`（`CardChoicePanel.cs:201-218`）只 KILL 钮与字、只 `SetActive(false)` **面板自己的件**，**碰不到调用方建的卡视图** ⇒ 面板关掉后 **4 张大卡面永远留在屏幕正中**、不属于任何面板 ⇒ **点它、点什么都关不掉**。**对照组**：**选牌那条路有** `ClearChooseViews()`（`:5719` / `:5731-5735`）—— **只有进攻/防御两支漏了**。**原版判据判死**：`ChooseCardMenu__ProcessChooseDone.c` 先 `ShowChooseCardElements(false)`（含 `ShowChooseCards(false)` **逐张 `SetActive(false)`**）**再遍历 anchor 子节点 `Object.Destroy`** ⇒ **原版「隐藏 ＋ 销毁」两件都做，我们两件都没做**。**旁证**：真实玩家那轮 `player.log:205` 有 `[OffensiveCards]`（**只有面板那支会走**）却**一条**进攻卡后续日志都没有；`click_log.txt` 的 #24/#27/#28 三下**都落在「继续」钮命中框内**（`D1` 按 `CardChoicePanel.cs:333-339` 的框算过、三点全在里面）且**逐条记「这一下一条日志都没有」**。⚠️ **仍没坐实的一格**：用户那屏到底是「面板没关」还是「**面板关了卡还在**」—— 两种都能解释那三条点击；次要疑点 ＝ 点击边沿可能被上层窗口吃掉（`BattleDriver.cs:7280-7302` 的次序 ＋ `ReleasedThisFrame()` 用掉就没了）⇒ **要真机复现一次**。🔴 **与 `A1487②` 的关系**：`D1` **没有**把两条并起来，但指出**它们是同一个缺陷的两支**（**防御那支 `:4553-4556` 同样一句没清**）⇒ **`A1487②` 很可能就是它**。📌 全文 → `资料/普查产出_第十八会话/D1_开局换牌与进攻卡.md`。⇒ **要做**（铁律 11）。📌 **用户真机实测**，优先级 = **插队**。 |

> ↳ 主对话做**（2026-10-11）：①两处 `views` 局部变量收进字段 `_envPickViews` ②`OnOffensiveDone`/`OnDefensiveDone` 各加 `ClearEnvPickViews()`③`BeginOffensivePhaseIfAny` 开头再清一次（防跨局残留）④新增按树计的读数口 `PanelCardViewsAlive()`。✅ `BattleScene.Run` 新增 5 条断言**全过**（面板开 4 张卡 → 关闭 → 存活的 0 张、含 inactive 也 0）。🔴 **同时按用户 2026-10-11 拍板**：进攻卡那一半**暂时关掉**（新增 `offensiveCardEnabled=false`，**代码一行没删**、`A1541` 那两条断言仍开着守住备份）；**防御卡照旧弹**。⚠️ 本行那句「要真机复现一次」**没做**（判据不足，不是尾巴）。


---

## 收口实跑（**红绿以此为准**）

本文件里那些「已改完」的断言，都在**同一天的收口那趟**实跑过 —— 读数如下（⚠️ 这一批**只跑了覆盖到的两条**，不是全套 12 条）：

| 宿主 | 读数 | 日志 |
|---|---|---|
| **`RuleEngineTest.Run`** | **4725 / 4725 全过 ✅** | `d:/4/_tmp_view/run19_ruleengine.log`（`:74009`） |
| **`BattleScene.Run`** | **2749 通过 / 0 失败 ✅** | `d:/4/_tmp_view/run19_battlescene2.log`（`:75120`） |
| 秒级类型检查（两个程序集） | **0 / 0 错** | —— |

**为什么只跑这两条**（铁律 12 的「按覆盖面跑」）：
- `RuleEngineTest.Run` 覆盖 `CreatePool` 与 `RuleEngineTest` 自己的改动（引擎侧）；
- `BattleScene.Run` 覆盖 `HandLayout` / `CardFeel` / `BattleDriver` 的改动（对战表现侧）。
- ⚠️ `工具/ruleprobe/**` 那几笔**不在任何 Unity 自检里** —— 它们**由写手自己跑端到端**验证（改前/改后读数见各自报告）。

**基准对照**（两边都只多不少，说明新增断言全绿、无回归）：
- `RuleEngineTest`：上一版基线 **4698/4698** → 现 **4725/4725**；
- `BattleScene`：上一版基线 **2735/0** → 现 **2749/0**（新增 14 条 = `A1540` 三条 + `A1541` 五条 + 进攻/防御分流六条）。

---

## 第二批（同一天 · 引擎那一批写手交件之后）
| 🆕 **`A1437`** | 🔴 **全池 1126 张里 `keywords` 含 `ability` 的 = 0 张 ⇒ 「主动技能」那条路对任何卡都走不到**（第十四会话 `探针willrun` 写手顺手查出）—— `CardDef.Ability`（`:135`）**恒 `null`** ⇒ `RuleCore.UseAbility`（`:6289`）/ `CanUseAbility`（`:6272`）**走不到**。⚠️ **与 `A1436` 是同一件事的两面**（一个从「卡的 `desc` 侧」看、一个从「引擎的注册侧」看）。⇒ **要做**（铁律 11）：先查证「**原版哪些卡真的有 ability**」（判据 = PnP 卡图 ＋ 反编译），再定是我们漏建还是本来就没有。出处 → 同上 顺手发现 3 |

> ↳ **判死 = 原版本身就没有**（2026-10-11 · 写手 `E8` · 三条判据全查实）：① 反编译 `d:/2/tools/decomp_full/` 里 `ability` **16 处全是** `Reliability`/`Reachability`/`Scrollability`，`Ability:` **零命中**；② 原始卡表 `card_stats.json`（1213 张）keyword/keywords 含 ability **0 张**；③ `cards_engine.json` 1126 张 `keywords` 含 ability **0 张**，`desc/descZh` 提到 ability 的 5 张**说的都是别的系统**（`Spirit Stone ability` / `Duty ability` / `Codex ability`）。⇒ 落**铁律 11 例外①**（与 `CardDef.cs:2570-2574` 自己的注释逐字一致）。⚠️ **残余**：「要不要删 `RuleCore.UseAbility` 那条走不到的路」是**引擎侧的独立小账**，⛔ 不在本条里。
| 🆕 **`A1562`** | 🔴 **`A1399` + `A1397①` 的收口落在 `WhenEvent.cs`（不在本批任何写手的白名单里 ⇒ 停手报回）**（2026-10-11 · 写手 `E7` 回机器码查实 · ⛔ 未改）—— 🔴 **账上「原版这一族自身不一致」是【错的】**：`.c` 里看着「`ReactToUnitUsedSacrifice` 只传 1 个实参」是 **Ghidra 掉了 `rdx`**，`.c` 里 6 处 VA 各有一条 `mov rdx, rsi`（`rsi` = 入口 `mov rsi, rdx` = 被献祭那张卡）⇒ **原版统一自排除**，靠两个 id 分工（`Mob=300` 给自己 / `OtherCardMob=645` 给别人）；同族 `TriggerOtherCard{Landing,Mob,Codex,SpiritStone,Requiem,Regiment}.c:14` 全是 `op_Inequality(param_2, param_1)`。**我们的真差异** = `WhenEvents.Matches` **没有这一关**（只有反向的 `SelfOnly`）。**修法** = `RuleEngine/Core/WhenEvent.cs` 的 `WhenEvents.Matches` 加「`triggers:*` 且非 `SelfOnly` 时不许 `ReferenceEquals(listenerUnit, subject)`」。📌 全文 → `资料/普查产出_第十九会话/E7_RuleCore四笔.md`。⇒ **要做**（铁律 11）。 |

> ↳ **守卫 + 断言都做完**（2026-10-11）。**守卫**（写手 `E9`，`WhenEvent.cs` 97/0）：`Matches` 加自排除 ＋ 新增 `KeywordTriggerIncludesSelf` 例外表。🔴 **它的前提被自己回机器码推翻**：账上原写「原版这一族自身不一致」→ `E7` 改成「统一自排除」→ **`E9` 数 `op_Inequality`(`0x182FFB920`) 的 `call` 条数**（`UsedSacrifice` 3 · `Mob` 1 · `Swarm` 1；`Ferocity`/`Duty`/`Synapse` **各 0**）⇒ **原版确实不一致、只是分法不同**；**blanket 版会改错 `Bjorn the Fell-Handed`**（靠「自己响」才留得下来）⇒ 落地 =「默认自排除 ＋ 例外表」。**断言**（写手 `E12`，`RuleEngineTest.cs` +257/0）：38 条 `Check` / 2 个方法，**6 组改坏真跑**验证过（M1 删守卫 ⇒ 只红 ②；M2 清空例外表 ⇒ 只红 ③；M3 去 `!SelfOnly` ⇒ 只红 ④；M4 删判空 ⇒ 只红 ⑤；M5/M6 ⇒ 各自那一组）。⚠️ **残余**：`E9` 原给的两条 `sacrifice` 判别式**实测不成立**（根因 = `FireTriggerAlways` 排在 `UseSurvivor` 之前 ⇒ 触发源那一刻已不在活体名单）⇒ 承重改用 `mob`，已另立 `A1576`。
| 🆕 **`A1564`** | 🔴 **引擎这一批改动的【断言全部欠着】—— 一条都没写**（2026-10-11 · 写手 `E4`/`E5`/`E7` 各自如实报回：`RuleEngineTest*.cs` 不在他们白名单）—— 覆盖 `A1469`（门① / 门①-bis，**真池抓不到、必须合成卡**：`keywords=[Rally]` + `desc="Ecstasy 2: …"` ⇒ `TriggerOps("rally")` 从 `null` → `[gain]`）· `EC28`（**真卡**：`TriggerText("slay")` 不再拖上 `Ecstasy 3:`、`TriggerOps("slay").Count` 1→2）· `A1471`（**真卡** `DA25.WhenTriggers[0].Ops` 多一条 `heal\|cond=targetsurvives`）· `A1258`（带 `abouttoattack` ⇒ 返 `OK` + 目标血不动 + `AttacksThisTurn` 不变 + 无 `EvtKind.Attack`；**且**不带 ⇒ 照常打；**且** `forced:true` ⇒ 不取消）· `A1397②`（手工卡同时带 `stealth`+`dropPod` + 挂 `triggers:Landing` 监听器 ⇒ 断「`landing` 事件下标 < `LosesStealth`」）· `A1399`（卡 A 带 `sacrifice` + 非 `SelfOnly` 监听器 ⇒ 断「B 会响」**且**「A 自己不响」）· `A1335③`（`Bastion 5` 挨 3 ⇒ `EvtKind.Hit` 的 `Amount == 0`）。**规格逐条写在各自报告的 §2/§4/§5 里**（`E4_CardDef三笔.md` §五 · `E5_EffectText三笔.md` · `E7_RuleCore四笔.md` §2）⇒ **派断言写手照抄即可，⛔ 别再来一轮设计**。⇒ **要做**（铁律 11）。 |

> ↳ **断言补齐**（2026-10-11 · 写手 `E10`，`RuleEngineTest.cs` **+521/−0**）：覆盖 `A1469`（合成夹具卡两例）· `EC28`（真卡断句 ＋ 灭自证）· `A1471`（真卡 `DA25`）· `A1258`（取消/沙包/`forced` 三组）· `A1397②`（stealth 到期次序 ＋ 反例）· `A1335③` · `A1428`（`instead` 三闸 ＋ 行为面）。**59 条断言 / 7 个方法，7 组改坏真跑逐条验过判别力**（M1~M7，每组报出「哪几条红、其余几条不动」）。🔴 **`E5` 说「`A1428` 行为面没做」—— `E10` 补上了**（夹具实测 1 vs 3）。

---

## 收口实跑 · 第二批（同一天 · 引擎那 5+3 个写手与两轮战场修复之后）

| 宿主 / 检查 | 读数 | 日志 |
|---|---|---|
| **`RuleEngineTest.Run`** | **4822 / 4822 全过 ✅** | `d:/4/_tmp_view/run19_ruleengine2.log`（`:75405`） |
| `BattleScene.Run`（本会话早先那一趟） | **2749 通过 / 0 失败 ✅** | `d:/4/_tmp_view/run19_battlescene2.log` |
| 秒级类型检查（默认档） | **0 / 0 错** | —— |
| 秒级类型检查（**`WF_DOC=1`** 档） | **0 错** · `CS1573` = **0** | `d:/4/_tmp_view/tc_doc.txt` |

**基线演进（只增不减 ⇒ 新断言全绿、无回归）**：
`RuleEngineTest` **4698 → 4725 → 4822** · `BattleScene` **2735 → 2749**。

**战场那两轮的实测（不是断言，是渲图读数）** —— 见 `资料/普查产出_第十九会话/F1_…md` / `F2_…md`：
`m_pivot` 接上 ⇒ 13 场 **11 场**亮度比向原版靠（arena3 1.057→1.035 · blacklegion 1.070→1.064）；
`m_VertexStreams` 接上 ⇒ sororitas 1.017→1.010 · spacewolves 1.005→1.002；`Barrels` 火框 **1.30× → 1.11×**（原版 83.8）。

⚠️ **仍未收口的**（**别当已做完**）：~~`A1557` 的第 ② 项（`m_ApplyActiveColorSpace`）~~（🔴 **2026-10-11 就地订正**：这一句写于 **20:23**，而写手 `F3` **20:27** 才落盘 ⇒ **② 当天就做完了**，产物自检 **161==161**）· `A1554`/`A1555` 的**生成器根治**（在 `d:/2`，等用户拍板）·
`A1576`（`sacrifice` 那两条判别式不成立的真因）· 战场「火为什么是一团糊」的只读调查（`R2`）。
