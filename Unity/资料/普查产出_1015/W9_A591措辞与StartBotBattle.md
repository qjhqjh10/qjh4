# W9 · A591 出声措辞收口 + `LiveOpsEventWindow.StartBotBattle` 改 `virtual`（2026-10-15）

> 执行写手 W9。**没跑 Unity**（本轮规矩：A 表清零前不中途跑自检）· **没动 git** · **没改正本**（`项目任务.md` / `CLAUDE.md`）· **只碰了简报点名的两个文件**（第三个白名单外的**一个字没动**）。
> 秒级类型检查：`TMPDIR=/tmp/wf_w9 bash d:/4/Unity/工具/typecheck.sh` → **运行时 0 错 · 编辑器 0 错**（改完最后一处又跑了一遍，共两遍）。
> 行尾（二进制读，改前 / 改后）：`ArenaBuilder.cs` **4533/4533 CRLF → 4567/4567 CRLF**（未翻）· `LiveOpsEventWindow.cs` **0 CRLF / 1143 LF → 0 / 1162**（未翻）。`git diff --numstat` = `39/5` 与 `21/2`，**无一份「数字 ≈ 文件行数」**。

## 一、摘要（6 行）

1. **A591 措辞收口**：`ApplyGroupNodes` 两处出声（`targets[]` / `adds[]`）的尾巴从「**很可能是上游闸门没建**」改成「**既有的、预期的 —— 不是新缺陷**」，并**把那条的实际签名写进出声**（`target BoardCamera` · 旁挂 pos `(100.000,2.222,-13.572)`）+ 判据（**A345-T-b「没对上 0 条 ×13」尚未达成**）+ 落痕出处；**计数器语义、控制流、`continue` 一字未动**。
2. 两处尾巴**抽成共用的一份**（`static string GroupNodeMissHelp()`）—— 照「两处写同一条规则 = 迟早不一致」办，**不是为了重构**（纯字符串方法，无行为）。
3. **出声多了一条反向判据**：「⚠️ 本条的名字或 pos 若不等于上面这条 ⇒ **那是新的**，按缺陷查」—— 防的是**镜面错误**（把将来真的新缺陷当成「既有的预期」挥手放过）。
4. `LiveOpsEventWindow.StartBotBattle`：`public` → **`public virtual`**（**纯形状改动**）。⛔ **没走**「加 `protected virtual GameMode PlayModeForWindow` 口」那条 —— **理由 = 在 §三·2 第 3 条**（会变成死钩子、或者会真回归）。
5. **额外一处（本件自己裁的、仅注释）**：该字段的 `/// <summary>` 原来写「（**自检断言直接读它**）」—— **今天不成立**（全仓 `.cs` 除本文件外**零命中**），就地更正（铁律 5）。⚠️ 若调度台认为越界，**这一处单独回退不牵动别处**。
6. 结果：**两处措辞 + 一个 `virtual` 关键字**，`ArenaBuilder.cs` `39/5`、`LiveOpsEventWindow.cs` `21/2`。⛔ 断言**一条没改、一条没加**（自检宿主不在白名单）。

---

## 二、改动清单

| # | 文件 · 锚点（按内容，⛔ 别按行号） | 改前 | 改后 | 依据 |
| --- | --- | --- | --- | --- |
| 1 | `MyGame/Assets/WarpforgeArena1/Editor/ArenaBuilder.cs` · `ApplyGroupNodes` ② **`targets[]`** 支：`LastGroupNodeMissedWhat.Add($"target `{t.name}`（旁挂 pos {Vec3Str(t.pos)}）…找不到" + …)` | 尾巴 = `"（**很可能是上游闸门没建** —— 见同一场日志里「内容：…另跳过无贴图 N · 原版关着 M · renderMode=None K 个」那行；闸门 = 本文件 `BuildContent` 的四道）"` | 尾巴 = `GroupNodeMissHelp()`（内容见 #3） | `资料/战场场景线_交接.md` §五 末的 **A591 落痕**（2026-10-15）· `资料/普查产出_1015/W-doc2_文档订正.md` §二 A591 行 |
| 2 | 同文件 · 同方法 ③ **`adds[]`** 支（**同一句话的副本**） | 同上（逐字相同） | 同上 | 铁律 5「`grep` 那句话的关键词，把同一句话被复制到别处的地方一起改」（**历史：这句话本来就抄了两份** —— `普查产出_1011/FX2_A343闸门修复.md:16` 记着当时也是「两处 + 一份副本」一起补的） |
| 3 | 同文件 · 新增 `static string GroupNodeMissHelp()`（插在 `ApplyGroupNodes` 之后、`ResolveGroupParent` 之前） | 无（两处各写一份字面量） | 一份共用措辞 + 一段带出处的 `///` 说明（为什么收口、判据是什么、今天有没有断言读它、那个猜测的现状） | 「两处写同一条规则 = 迟早不一致」（`CLAUDE.md` §三）· A591 落痕「**那条出声自己的措辞会误导**…**建议连同 A591 一起把措辞收口**」 |
| 4 | `MyGame/Assets/CardPresentation/Shell/LiveOpsEventWindow.cs` · `StartBotBattle` 签名 + 它上面的 `/// <summary>` | `public void StartBotBattle()`，注释只有两行「打 bot 那一支…」 | **`public virtual void StartBotBattle()`**；注释补三段：① 为什么改虚（原来是**间接**钩子）② ⛔ 为什么不走 `PlayModeForWindow` 那条（死钩子 / 会真回归）③ ⚠️ 联机那一支不走这里 | `资料/普查产出_1015/W4_A383模式号.md` **§五·1**（它把理由写全了）· 同文件 §四·3（「`LiveOpsEventWindow` 里没有『本窗模式号』这个显式声明」） |
| 5 | `MyGame/Assets/WarpforgeArena1/Editor/ArenaBuilder.cs` · `LastGroupNode*` 五个字段的 `/// <summary>` **第一句**（**仅注释**） | `/// <summary>上一次 `ApplyGroupNodes` 的结果（自检断言直接读它）。` | 就地更正（带日期与判据）：**今天没有任何断言读它们** —— 现读全仓 `.cs`（`grep -rn "LastGroupNode" --include=*.cs`）**除本文件外零命中** ⇒ 自检不会因「没对上」变红、也不会替我们盯住它 | 铁律 5（就地改掉与事实不符的记录）+ A591 落痕「⚠️ **今天没有任何断言在读它**」；**留着会与我在 #3 里写的那句打架**（同一文件两份相反说法 = 铁律 5 点名的正是这种） |

**新出声的完整样子**（`13/13` 场那一条，逐字；下一轮可直接拿它 `grep`）：

```
target `BoardCamera`（旁挂 pos (100.000,2.222,-13.572)）在我们建出来的树里找不到（🔴 **既有的、预期的 —— 不是新缺陷**：13/13 场逐场都是同一条 `target BoardCamera` · 旁挂 pos **(100.000,2.222,-13.572)**；判据 = **A345-T-b 的「没对上 0 条 ×13」**（`资料/普查产出_1013/WA345Ta_战场全树生成侧.md` §八）—— 🔴 **尚未达成：今天每场 1 条** ⇒ 按【预期红】处置；落痕 = `资料/战场场景线_交接.md` §五 末。⚠️ **本条的名字或 pos 若不等于上面这条 ⇒ 那是新的**，按缺陷查。· 还要查什么（上游闸门那个猜测，未证实）：同一场日志里「内容：…另跳过无贴图 N · 原版关着 M · renderMode=None K 个」那行；闸门 = 本文件 `BuildContent` 的四道）
```

（⛔ 只换文字，`LastGroupNodeMissed++` / `LastGroupNodeMissedWhat.Add(...)` / `continue` 的**条数与次序**逐条不变。）

---

## 三、「改完行为逐位不变」的论证

### 3·1 `StartBotBattle` 那半（**必须写的**）

1. **今天没有任何覆写**：全仓现读只有两个 `StartBotBattle` **声明** —— `Shell/LiveOpsEventWindow.cs`（本件改的那个）与 `Shell/PracticeModePopup.cs`（`public class PracticeModePopup : GameWindow`，**与 `LiveOpsEventWindow` 无继承关系**，是另一个类自己的同名方法）。两扇子类（`Shell/SkirmishEventWindow.cs` · `Shell/RankedEventWindow.cs`）里 `StartBotBattle` 只出现在**注释**里。
2. **调用点只有一处，还在同一个类里**：`LiveOpsEventWindow.cs` 的 `_search.OnSearchDone = () => { OnSearchFinished(); StartBotBattle(); };`。C# 里类内调虚方法也走虚分派，但**因为没有覆写**，落到的仍是**同一个方法体** ⇒ 逐位不变。（装配上：全仓没有 `GetMethod("StartBotBattle")` 这类**反射/字符串查找**，`grep -rn "StartBotBattle" --include=*.cs` 的命中全是声明 / 调用 / 注释。）
3. **没有隐藏/遮蔽冲突**：两个子类都没写 `new void StartBotBattle()` ⇒ 不会冒出 `CS0114`（"隐藏继承成员"）那类**语义会变**的警告；类型检查实跑也印证了（0 错 0 警）。
4. **没动的东西**：方法体**一个字没改**（`CollectionData.DeckAt` / `PrebuiltDecks.PendingSource` / `StartedBattle = true` / `BattleSceneFor(faction)` / `Debug.Log` / `Application.isBatchMode` 早退 / `SceneManager.LoadScene(scene)` 的**次序与条件全部照旧**）。`virtual` 只往 vtable 加一个槽 —— **今天的行为差异 = 0**。
5. **⚠️ 这半唯一的"行为"是被有意留白的**：**改成虚以后，将来任何子类覆写并自己写模式号，那一刻行为当然会变** —— 那正是这件活的目的（W4 §五·1）。今天**没有**这样的覆写 ⇒ 本条不构成"今天变了"。

### 3·2 `A591` 措辞那半

- **变了的只有字符串内容**（这正是要求）。计数器 `LastGroupNodeCreated/Moved/AnimAdded/Missed` 的**自增时机、条数、次序**与 `MissedWhat` 的**条数**（每处仍恰好 `Add` 一次）逐条不变；`continue` 仍在该 `continue` 的地方。
- **没有任何东西在解析这段文字**：现读全仓 `.cs`，`LastGroupNode*` 五个名字**除 `ArenaBuilder.cs` 自己外零命中** ⇒ 没有断言 / 没有下游消费者会因文案变化而红或绿（这也正是 A591 落痕点名的那件事）。
- **`missed` 条数也不受影响**：本件改的是**已经 `++` 之后**那一句 `Add` 的文本，判「对上 / 没对上」的 `FindBuilt(...) == null` 条件一字未动。

---

## 四、没做完 / 做不了的

1. ⛔ **断言一条都没补**（自检宿主 `Editor/BattleScene.cs` / `Editor/ShellScene.cs` **不在白名单**）：
   - A591 这条仍然**没有任何断言在读**（本件只让**人和日志**看得懂，没让自检盯住它）—— W4 §四·1 那批断言缺口原样还在。
   - 「子类覆写 `StartBotBattle` 能在切场景前写模式号」这一格**今天无法断言**（没有覆写者）；将来谁真去覆写，**得同时补一条「覆写生效」+ 一条「灭自证」**（例：让覆写设一个哨兵模式号 ⇒ 断言切场景前 `pending` 是它，而不是「与 `DeckGameMode` 同值」—— 不然「两边一起改回去」照样绿）。
2. ⚠️ **文档侧还留着一句"现在时"的旧引文（白名单外，本件没动）**：`资料/战场场景线_交接.md` §五 末的 A591 落痕里那句
   「⚠️ **那条出声自己的措辞会误导**：**它写**「**很可能是上游闸门没建**」（`ArenaBuilder.ApplyGroupNodes`）」——
   本件改完之后，**代码里已经不再这么写了** ⇒ 那句话成了「记录已经过期」。**建议**（属 `资料/`，请调度台分流）：在那条落痕尾加一行
   「✅ **2026-10-15 已收口**（W9）：`ArenaBuilder.GroupNodeMissHelp()` 改说『既有的、预期的』并写进签名 pos `(100.000,2.222,-13.572)`」。
   ⚠️ 另外两条**历史报告**（`普查产出_1011/D4_A191没对上诊断.md:270` · `普查产出_1011/FX2_A343闸门修复.md:16` · `资料/历史/A表已收口_1011.md:144`）记的是**当时**的建议与做法，**按铁律 5「成立与否」判 = 当时成立**，**不必改**（本件也不在那些文件的范围里）。
3. ⚠️ **`LiveOpsEventWindow` 里"本窗模式号"仍然没有显式声明**（W4 §四·3 那条原样）：两扇子类继续在 `OnSearchFinished` 里各设一次。本件把「**以后可以覆写 `StartBotBattle`**」这条路铺好了，但**没有**、也不能替它们改（那两个文件不在白名单）。
4. ⚠️ 没跑真 Play、没跑任何 Unity 自检（本轮规矩）—— 文案与 `virtual` 都只能靠**类型检查 + 静态论证**背书；「13 场重建日志读起来不再像 13 个新缺陷」这一步要等下一次**真跑 `ArenaBuilder.BuildArenaPrefabs`** 才算验收。

---

## 五、顺手发现（⛔ 只报不改）

1. 🟡 **同族还有一处「把猜测写成结论」的出声**：`ArenaBuilder.ResolveGroupParent` 的 `why` 里仍写
   「（**父可能是被上游闸门挡掉的** —— 见同一场日志里「内容：…另跳过…」那行；⚠️ 旁挂里被闸门挡掉的父会进 `nodes[]`…）」（现读 `ArenaBuilder.cs` 在 `ResolveGroupParent` 的 `why = …` 那两行）。
   **今天不触发**（13/13 的 `missed = 1` 是 `targets[]` 那条，不是"父找不到"），而且它**不属于 A591 的判据面**（A591 说的是 `target BoardCamera` 那条）⇒ **本件没动**（⛔ 不自己发明口径）。若调度台要一起收口，它和 #3 是同一个形状（`GroupNodeMissHelp()` 旁边再加一份共用尾巴）。
2. 📌 **一条容易误会的邻近注释，我核过、它没坏**：`Editor/BattleScene.cs` 里那条「🧨 **改坏法**：把 `ArenaBuilder.ApplyGroupNodes` 里补 `Animation` 那两句去掉…⇒ 下面这条红」的断言，读的是**结果**（`bl.myAnimation != null`），**不是** `LastGroupNodeAnimAdded` 这个计数器 ⇒ 与我写在字段注释里的「今天没有任何断言读这五个名字」**不矛盾**（那条断言仍然有效、仍然挡得住那个改坏法）。**别把两件事读成一件事**。
3. 📌 **`ApplyGroupNodes` 的两个"没对上"支今天都只走 `targets[]` 那一条**（`adds[]` 支 13 场全 0）—— 所以两个分支共用尾巴时，`adds[]` 那句「13/13 场逐场都是同一条 `target BoardCamera`」在**它自己触发时看着会有点隔**；靠末尾那句判别式（「名字或 pos 若不等于上面这条 ⇒ 那是新的」）兜住。若将来 `adds[]` 真出一批新的，再考虑分家（**今天不拆**：拆了就又变成两份）。
4. 📌 **W4 §四·2 / §五·2 / §五·3 那三条我已核、不重复报**（`LiveOpsEventWindow.cs:902` 的模式串闸 · `BattleDriver.cs:2106` 的警告措辞 · 「12 张的牌从练习窗开出去」那条缝）——它们**都在本件白名单外**，属调度台裁定。

---

## 六、本件跑过什么

| 检查 | 结果 |
| --- | --- |
| 秒级类型检查（改完最后一处又跑一遍） | `TMPDIR=/tmp/wf_w9 bash d:/4/Unity/工具/typecheck.sh` → **运行时 0 错 · 编辑器 0 错**（两遍） |
| 行尾（二进制读，改前 + 改后各一次） | `ArenaBuilder.cs` **4533/4533 CRLF → 4567/4567 CRLF**（`loneLF = 0`，**未翻**）· `LiveOpsEventWindow.cs` **0 CRLF / 1143 LF → 0 / 1162**（**未翻**） |
| `git diff --numstat` | `ArenaBuilder.cs` **39 / 5** · `LiveOpsEventWindow.cs` **21 / 2**（都不是「数字 ≈ 文件行数」） |
| 关键事实的现读复核 | `grep -rn "LastGroupNode" --include=*.cs`（**除 ArenaBuilder.cs 外零命中**）· `grep -rn "很可能是上游闸门没建"`（**`.cs` 里只剩我新写的那段说明性引文**）· `grep -rn "StartBotBattle"`（**只有一处声明 + 一处调用 + 注释**）· 依赖关系现读（`PracticeModePopup : GameWindow`，与 `LiveOpsEventWindow` 无继承关系） |
| Unity 自检 | **一条都没跑**（本轮规矩：A 表清零前不中途跑） |
| git | **只读**（`git diff` / `git diff --numstat`）；**没 commit / checkout / stash / reset** |
