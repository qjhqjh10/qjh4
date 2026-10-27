# A5 · `BattleScene` / 测试宿主断言跟改（池 1,129 · v35 · 白名单摘 5 条）

> 2026-10-27 · 第八十七会话 · 执行代理 `A5`（`.cs` 攻坚波）
> 白名单：`CardPresentation/Editor/BattleScene.cs` · `CardPresentation/Editor/RuleEngineTest_EngineE1.cs` ·
> `RuleEngine/Editor/RuleEngineTest.cs` · `RuleEngine/Editor/RuleEngineTest_EngineD4.cs`
> ⛔ 未碰 `CardPresentation/Battle/BattleDriver.cs`（另一代理）、未动 git、未跑 Unity、未重钉任何指纹。

---

## 〇 · 一句话

两件事都落地了：**池 1,128 → 1,129 / 卡表 v34 → v35 的断言全部跟改**（含 hash 重算与 3 处白名单相关断言），
**③ 那 5 张卡改成断「卡面 = 官方 `<descRef>` 正文」（= `<id>_new`）**、`AM72` 仍留白名单。
`typecheck` **编辑器程序集 0 错**（运行时程序集有 3 条**别人在飞的文件**的错，见 §四）。

---

## 一 · 每处怎么改的（行号 = **改后**现读）

### A · 池张数 / 版本（4 个文件）

| # | 文件:行 | 改前 | 改后 |
|---|---|---|---|
| 1 | `CardPresentation/Editor/BattleScene.cs:25303` | `Check(n == 1128, …｜2026-10-25：卡表 v30 新增 1 张隐藏附魔 Pray Enchantment`) | `Check(n == 1129, …｜2026-10-27：卡表 v35（加三列 `*TextReferenceId`）+ `A1a` 把 `DA42` 的附魔模板 `Reconnaissance Mission`（`e8cf1eb5…`）收进池（1,128 → 1,129；v30 那张仍在）`) |
| 2 | `CardPresentation/Editor/BattleScene.cs:25304` | `Check(h == 0x152CA48Fu, …)` | `Check(h == 0xE59A576Eu, …)`（⚠️ **同一个 `{ }` 里的 hash 断言也一定会红**，虽然简报只点了 `n == 1128`；见 §二·1） |
| 3 | `CardPresentation/Editor/RuleEngineTest_EngineE1.cs:80` | `Check(pool.Count, 1128, "…1128 张｜v30…")` | `Check(pool.Count, 1129, "…1129 张｜`A1a` 收 `DA42` 附魔模板（1128 → 1129）")`（上面补了 3 行归因注释） |
| 4 | `RuleEngine/Editor/RuleEngineTest.cs:22263` | `CheckTrue(pool.Count == 1128, …)` | `CheckTrue(pool.Count == 1129, …)`（上面补了 5 行归因注释） |
| 5 | `RuleEngine/Editor/RuleEngineTest_EngineD4.cs:101` | `Check(dto.version, 34, …)` | `Check(dto.version, 35, …)`（消息里补 v35 = 加三列 / 15·2·51 / ⚠️ 本版零引擎行为） |

**`0xE59A576E` 是怎么来的（可复查）**：`id + \x01 + desc + \x01 + descZh + \x01` 逐卡拼、按
`cards_engine.json` 的 `cards` 序做 FNV-1a 32。
✅ **可信度臂**：拿**改前**那份卡表（`git show HEAD:…/cards_engine.json`，1,128 张）用同一份脚本复算
= **`0x152CA48F`** —— 与旧期望**逐字相同** ⇒ 算法与旧期望同源；同一脚本对新池（1,129 张）= `0xE59A576E`。
增量只来自新卡自己（`desc`/`descZh` 均空 ⇒ 它只贡献 `id + 3 个分隔符`）。

### B · `RuleEngineTest_EngineD4.cs` 另外三处「整池计数」也必然红（同文件，一并跟改）

新卡（`e8cf1eb5…`）不是只有 `desc` 空：它**自己带 1 条 `abilityTargets`（`ta = [0, 20]` ⇒ 2 个 logic）
+ 2 行 `abilityTargetCriteria`（`targetsAffected` = 0 / 20）** ⇒ 本文件 §A/§B/§C 的整池字面量要跟着动：

| # | 行 | 改前 | 改后 |
|---|---|---|---|
| 6 | `:193` | `Check(atcRows, 1619, …)` | `Check(atcRows, 1621, …)` |
| 7 | `:265` | `Check(atcCards, 1009, …)` | `Check(atcCards, 1010, …)`（「其余 119 张」不变：1,129 − 1,010 = 119） |
| 8 | `:272` | `Check(atcTa0, 286, …)` | `Check(atcTa0, 287, …)` |
| 9 | `:363` | `Check(cards, 1128, …)` | `Check(cards, 1129, …)` |

✅ **复算方式（可复查，写在 `:184-190` 的注释里）**：拿 `cards_engine.json` 按 §A4/§B 同一套判据现数 ——
**改前池逐格复现旧值**（`1619 / 1009 / 286 / 13 / 117 / 60 / 391 / 556 / 336 / 729 / 28 / 51 / 50 / 163 / 79 / 10 / 21 / 2 / 193 / 132 / 1128`），
同一脚本对新池只在这 3 格 + `cards` 上变。
⚠️ **没有变的**（已核，别手贱去改）：`aatRows 193` · `aatCards 132` · `atcRefs 13` · `atcKeyword 117` ·
`atcTraits 60` · `atcMaxT 391` · `atcFriendly 556` · `atcHero 336` · `atcMinions 729` · `atcExclTrait 28` ·
`atcChosenArmy 51` · `atcArmyFilter 50` · `atcRemnants 163` · `atcChooseRes 79` · `atcSpellType 10` · `atcMana 21` · `atcRarity 2`
（新卡那两行 criteria 除 `targetsAffected` 外**全是 0/空**）。

### C · 白名单摘 5 条 → 3 处「依赖旧版正文」的断言（`BattleScene.cs`）

| # | 行 | 改了什么 |
|---|---|---|
| 10 | `:25119-25123`（②·F 头注） | 「白名单（非缺陷，6 个）= …」→ 「键 = `Card_Description/` + (`descRef` ? 它 : 自己 id)（照原版那个三元）」+ 「白名单 **2026-10-27 起只剩 1 个 = `AM72`**」+ 摘除那 5 个的归因（§12·94） |
| 11 | `:25209-25286`（原 **③** 整段） | 原来 6 个手写子块（`ASH48`/`SAU58`/`SAU15`/`TAU65`/`AM72`/`SOR25`）断「**仍印我们的旧正文**」→ 改成 **5 张的循环**（`ASH48`/`SAU58`/`SAU15`/`TAU65`/`SOR25`）+ **`AM72` 子块原样保留**。新断法逐张 3 条：① `!IsOfficialFaceTextWhitelisted(pid)`（**已摘**）② `A1945DescKeyOf(pid) != "Card_Description/"+pid`（数据前提：`descRef` 非空）③ 卡面正文**含 `CardIcons.Rewrite(pid, field, 官方 <descRef> 原文)`**（中/英各一条）。判别式 = 「`want` 与**旧键**（`<id>`）的渲染结果**不相等**」⇒ 生产若仍按卡 id 取键，`want` 不会出现在正文里 ⇒ 红。 |
| 12 | `:25325` / `:25365`（**⑤ / ⑥** 的 `want`） | `Loc.OfficialEnglish("Card_Description/" + c.Id)` / `Loc.OfficialCurrent(同)` → **`…(A1945DescKeyOf(c.Id))`**（与 ③、与生产**同一口径**）；并各加一条硬前提 `Check(refKeyMissing == 0, …)`（`:25346` / `:25394`）：**`descRef` 非空却查不到 `<ref>` 键 ⇒ 红**，⛔ 不许静默 `continue` 掉（那只会让覆盖率悄悄变好看） |
| 13 | `:25151`（**①** 的 `enRows`） | 整行换卡：`TL24 / "dies, deploy another one"` → **`GOF48 / "random Ork troops that cost 4"`**。理由（§12·94）：`TL24` 我们自己的 `Desc` **也写新版**（`Flying. When an enemy attacks, deploy a Spore Mine`）⇒ 它当不了「官方 vs 我们」判别式样本。`GOF48`：官方 `…random Ork **troops** that cost 4…` vs 我们 `…random Ork **Infantry**…` ⇒ 判别式与断言落在同一处差值上，且该卡 `descRef` 为空（键恒 = 卡 id）⇒ **换键前后都成立** |
| 14 | `:49088+`（类尾，**零漂移**） | 新增 `A1945RawRefRow` / `A1945RawRefFile`（`[System.Serializable]`）+ `A1945DescRefMap()` / `A1945DescRefOf()` / `A1945DescKeyOf()`：`descRef` 从**卡表原文**（`Resources/cards_engine.json`，v35 已有三列）读，⛔ 不经 `CardDef` ⇒ ① 期望值不从被测实现里取 ② `CardDef` 三属性落地前也能跑。读失败**不当成功缓存**（下次重试） |

**③ 新段落的判别力自检**：这 5 张的官方 `_new` 都是「**旧正文 + 白名单注释里写着「官方缺」的那半句**」
（`ASH48 + ((Spirit_x,2)) ((Repeat_this_effect))` · `SAU58 + ((Draw_a_troop))` · `SAU15 + [[rally]]…[[stealth]]` ·
`TAU65 + [[strike]]: ((Heal_1))` · `SOR25 + ((Melee…+1)), ((Ranged…+1))`）⇒ `_new` 渲染结果必然**严格更长**，
所以「正文含 `_new`」这一条本身就排除了「仍印旧键」那种实现。

---

## 二 · 我**多做**的两件（简报没点，但不做就是留红）

1. **④ 的 hash 断言**（`BattleScene.cs:25304`）：它和 `n == 1128` 在同一个 `{ }` 里、同样整池遍历
   ⇒ 池 +1 张必红。已按上面 §一·A 的办法重算成 `0xE59A576E`，并把复算方式写进断言消息。
2. **`RuleEngineTest_EngineD4.cs` 的 `atcRows/atcCards/atcTa0/cards`**：同上（新卡自带 2 个 logic）。
   ⚠️ 这四处**必须**在「池 +1」这一批里一起改，否则 `RuleEngineTest.Run` 会在同步点点名它们。

---

## 三 · 我没敢改 / 改了也会红的（⛔ 不在我白名单）

1. 🔴 **`CardPresentation/Battle/BattleDriver.cs`（另一代理在改）** —— 我 ③/⑤/⑥ 的断言**以它为前提**：
   - `OfficialFaceTextWhitelist` 必须**真的只剩 `AM72`**（`A1945Demoted` 那 5 条里第一条断言就查这个）；
   - `FaceTextFull` 必须**把键换成 `Card_Description/<descRef>`**（§12·94 施工规格里的「消费端 4 处」）。
   ⚠️ **只删白名单条目、不换键** ⇒ 那 5 张会退成「印官方**旧**正文」（丢掉 `Repeat this effect` / `Draw a troop` /
   Rally 句 / `Strike: Heals 1` / `+1,+1`），我的 ③ **会红并且报得对** —— 那不是误报，是「摘了创可贴但没做根因」。
2. `RuleEngine/Data/CardDatabase.cs` **`dto.version < 34` 守卫（现读 `:1420`）与 DTO 三列**：v35 只落了数据列，
   `CardDatabase` / `CardDef` 都还没有 `cardDescriptionTextReferenceId`。我这边**不需要**它（读原文），
   但**批次 B** 欠着；`dto.version < 35` 的出声也该在那批加。
3. 🔴 **`RuleEngine/Editor/RuleEngineTest_G12.cs` 会红 2 条（不是我的文件）**：
   - `:57` `Check(pool.Count, 1128, …)` ⇒ 应 1129；
   - `:104` `Check(cardsEmpty, 996, …)` ⇒ 应 **997**（1,129 − 132；新卡没有 `activeAbilityLogic` 行）。
   （`:93` 那句「1128/1128」只是消息文案，断言是 `notVersioned == 0` ⇒ 仍绿。）
   ⇒ **建议**：谁拿到 `RuleEngineTest_G12.cs` 的文件所有权就顺手改这两处（判据同上）。
4. `RuleEngine/Editor/RuleEngineTest_EngineA3.cs:491` 消息里「现读 1128 张」是**文案**（断言是形状判据）⇒ 只是过期措辞。
5. `CardPresentation/Battle/BattleDriver.cs:27930` 注释「`spellType` 列（1128/1128 有值）」等**运行时文件里的 1128 死数**：
   只影响可读性，⛔ 我没动（那是别人的文件）。调度台日志 §13·5 记的「文档里 1128 死数 9 处」同族。

---

## 四 · 顺手发现（只报不改）

1. 🔴 **`typecheck` 的运行时程序集现在有 3 条别人的错**（2026-10-27 本次现读）：
   `Shell/FastModeBaseEvent.cs(304,33) CS1503 float→int` · `Shell/ChatTab.cs(156,28)/(176,28) CS1061
   SocialData.ChatMessage 没有 Channel`。**同一时刻编辑器程序集 0 错**、我四个文件全在编辑器程序集 ⇒ 与我无关
   （按铁律 13·3：错误全集中在**不是我负责的文件**上 ⇒ 不是我的问题，隔一会儿重跑）。收工时最后一次重跑见 §五。
2. **账的漂移面 = 0**（好消息，可省一次核对）：现读 `账/*.tsv` 里 **落点列**写着 `BattleScene.cs` 的只有 **2 行、
   且都不是 A 档**；`RuleEngineTest.cs` / `_EngineD4` / `_EngineE1` **各 0 行** ⇒ 我这批改动（净行数：`BattleScene.cs` +18 · `RuleEngineTest.cs` +6 · `_EngineE1.cs` +3 · `_EngineD4.cs` +13）
   **不会产生任何 A 档落点漂移**。（只读跑 `check_ledger_points --all-A`：现读合计 A 4303 · 通过 3695 · 漂移 593 · 失败 15
   —— 那 593/15 来自**别的代理**正在飞的 `.cs`，与这四份无关。）
3. `RuleEngineTest_EngineD4.cs` 文件头的「⚠️ 本文件还没接线」**已过期**：`RuleEngineTest.cs` 现读 `:1084` 就有
   `Step(TestEngineD4);` ⇒ 已就地订正（铁律 5）。同处「期望值一律写死字面量（1127 / 193 / 1616 / 132 / 1008 …）」也换代成现读值。
4. `RuleEngineTest.cs:1083` 注释「`abilityTargetCriteria` 1616 行」也过期（现读 **1621**）⇒ 已就地订正。
   （⚠️ 提醒 `D4` 正文里 `Check(..., "…1616 个 logic…")` 那一类是**消息文案**，真正的字面量是 `atcRows`，已改到 1621。）
5. 📌 **`TL24` 的旧中文列是朝鲜语**（§12·94 记的那条）现读仍然成立（`Card_Description/TL24` 的 `zh` 列 =
   `[[flying]]. 아군 $$TL37$$이 죽을 때 …`）。它现在**不再被任何断言采样**（① 那行已换成 `GOF48`）⇒ 这条数据缺陷
   **没有任何自检在盯**；要么补一条（`_new` 的 zh 列是干净中文 ⇒ 换键后玩家看到的是对的），要么记账。
6. 📌 ⑤/⑥ 的**空正文档**我没扩：`FaceTextFull` 在「中文档但这张卡 `descZh` 空」时会取**官方英文列**
   （`Loc.OfficialEnglish`），而 ⑥ 对这类卡是 `continue`（既有行为）⇒ 那一档今天**没有断言**。我 ③ 的 zh 支
   跟着生产加了同一道闸（`CardText.Zh && !string.IsNullOrEmpty(pc.DescZh)`），⛔ 没顺手扩 ⑥ 的口径（会改既有覆盖形状，留给判据侧）。

---

## 五 · 验收

- `TMPDIR=/tmp/wf_a5 bash d:/4/Unity/工具/typecheck.sh` —— **编辑器程序集 0 错**（我四个文件全在编辑器程序集，
  改前改后都是 0）。
- ⚠️ **运行时程序集在本次收工的最后几次重跑里一直有 1–3 条别人的错**（`Shell/FastModeBaseEvent.cs` CS1503 ·
  `Shell/ChatTab.cs` CS1061 ×2 · `Shell/ScoringEvent.cs` CS1529「using 必须在其他元素之前」= 典型的**正在写的文件**）。
  我**重试了 4 次**（间隔 40–60 s），错误**每次换一个文件**、全部落在我白名单外 ⇒ 按铁律 13·3 判定为「别人在飞」，
  **不是我这批的问题**；运行 `typecheck` 时也没有一条点名我这四个文件。
- 行尾：四个文件**逐文件核过** —— `BattleScene.cs` CRLF **49,115** / LF 49,115 · `RuleEngineTest.cs` CRLF **38,825** / LF 38,825 ·
  `RuleEngineTest_EngineE1.cs` 纯 LF **485** · `RuleEngineTest_EngineD4.cs` 纯 LF **556** ⇒ **裸 CR 0 · 混行尾 0**
  （⛔ 没用 `sed -i`；`.cs` 全走 `Edit` / python `wb`）。
- `git diff --numstat`：`BattleScene.cs` 130/112 · `_EngineE1.cs` 4/1 · `RuleEngineTest.cs` 8/2 · `_EngineD4.cs` 27/14。
- 未跑 Unity（`BattleScene.Run` 由调度台统一跑）；未重钉 `Pinned*Md5`；未动 git / 账主表 / 两张正本。
