# W · 收尾小账五件（2026-10-18 第三会话）

五件全部落刀。⛔ 未跑 Unity · 未动 git · 未改正本 · 白名单外一个字节没碰。类型检查末次 **运行时 0 · 编辑器 0**。
📌 **路径订正**：简报里 `RuleEngine/…` 那三件**不在 `Assets/CardPresentation/` 下**，实际在 `Assets/RuleEngine/{Editor,Core}/`；`Battle/TutorialOverlay.cs` · `Shell/BoosterInfoPopup.cs` 才在 `CardPresentation/` 下。

---
### ① 两处：改前 / 改后（按内容定位）

**定位** = `RuleEngine/Editor/RuleEngineTest_BoardModel.cs` 的 `④b 两侧都满` 块末尾两条 `CheckCode`（紧跟 `int extra2Idx = ctx.Players[0].Hand.Count - 1;`，块注释是 `---- ④b 两侧都满 ⇒ 这才是唯一真的拒绝 ----`）。

```csharp
改前： CheckCode(RuleCore.CanPlayCard(ctx, 0, extra2Idx, 3), RuleCodes.ErrSlot,
                 "★ **两侧都满** ⇒ 打不出去（原版这一步靠调用方事先筛，我们返回 `ErrSlot`）");
       CheckCode(RuleCore.CanPlayCard(ctx, 0, extra2Idx, 5), RuleCodes.ErrSlot, "…请求右侧同理");
改后： 两条的码都换成 `RuleCodes.ErrNotEnoughRoom`（文案里的 `ErrSlot` 一并改掉）；
       上方另补 9 行留痕（拆成的三档分别是什么 · `RuleCore.cs:1366` 就是那一格 ·
       判据 = 原版 `BattleManager__CanPlayCard.c:104-120` 的 `NotEnoughRoom`）
```

🔎 **改前先现核过**（不是照简报抄）：`RuleEngine/Core/RuleCore.cs:1366` `if (!BoardSlots.HasRoomFor(ps, slot)) return RuleCodes.ErrNotEnoughRoom;` ⇒ 这两条**确实必红**。

---
### ② `Names` 补的两行 · `Describe` fallback 触发面的变化

补在 `Names` 表尾（`ErrNotWaystone` 之后），**只对齐新增这两行、没重排整张表**（免得 122 行无谓 diff）：

```csharp
{ ErrNoTargetAvailable, "这张战术卡没有可选的合法目标" },   // 18
{ ErrNotEnoughRoom,     "棋盘上已经放不下了" },             // 19
```
中文是**给人看的整句**，⛔ 不是出词条键（「`Describe` 改出键」是 `A985⑧` 第 ③ 步，本轮不做）。

**fallback（「未知错误码 N」）触发面 —— 核过，与简报的推断不同：**

| 时点 | 触发面 |
|---|---|
| 拆码**前** | 码只有 0–17，**全在 `Names`** ⇒ 对任何引擎能返回的码都**不可达** |
| 拆码**后、补这两行前** | 18/19 出现且**不在 `Names`** ⇒ **变成生产可达**（2 个码 × 全仓 20+ 处 `Describe` 调用点） |
| **本次补完** | 回到「不可达」 |

⇒ **净变化 = 0**（拆码曾把面撑大，这两行把它收回去）。⚠️ 码 **12 是空号**（`ErrUnimplemented` 从 12 挪到 13 给 `ErrPindown=11` 让位，`RuleCodes.cs:26-35`），**无人返回 12** ⇒ 「不可达」成立。

- 🔎 **`Describe(18/19)` 的真实读者是【诊断面】，不是玩家那一行**（与简报推断不同，**已按实读写进注释**）：`BattleDriver.cs:635`（回放被拒）/`:717`（联机重放被拒）· `Net/NetApply.cs:103` · `Net/NetBattle.cs:998-999/1036` · `EffectResolver.cs:1441-1442` · `TutorialScript.cs:917/945` · `Editor/RuleEngineTest.cs` 多处 `diag` · `Editor/BattleScene.cs:2998`。
- ⚠️ **玩家可见那一行不走 `Describe`**：`BattleDriver.cs:7004 SetHint(HintForCode(rc))`，而 `HintForCode`（`:7590`）= `TermKey(rc) != null ? Loc.T(键) : Describe(rc)`；18/19 **已有原版键**（`Terms` 那两行）⇒ 落 `Loc.T` 那一档 ⇒ 简报里「玩家会看到**未知错误码 19**」**不成立**。
- 🔴 **顺手发现（不在我白名单）**：那一档实际印的是 `Loc.T` 拿不到值时的**【键名本身】**（`Core/Loc.cs:902-917` 缺键 ⇒ `return key`），而 `Battle/Tips/*` 本地无 value ⇒ 玩家看到的是 **`Battle/Tips/NotEnoughRoom`** —— **比「未知错误码 19」更难懂**，建议单开待办。
- `BattleScene.cs:3035` 那条断言用的无键码是 `ErrSlot`（本来就在表里）⇒ **加这两行不影响它**。

---
### ③ `EffectText.cs` 那处：判成哪种 · 改前 / 改后

**定位** = `RuleEngine/Core/EffectText.cs` 的 `PickTarget` 里、`if (t.Kind == "warlord") continue;` 上方那句（全文件 `ErrSlot` **只此一处**，grep 坐实）。

**判成「没有合法目标」= `ErrNoTargetAvailable`，⛔ 不是「满了」**，按上下文判的三条依据：① 该句点名的 API 是 **`CanPlayTactic`**（不是 `CanPlayCard`）；② `CanPlayTactic` 里 `spec != null` 的两处出口（`EffectResolver.cs:908` `targetSlot < 0 || 越界` · `:910` 那一格没有合法候选）**都**返回 `ErrNoTargetAvailable`，**没有第三个出口**；③ `ErrNotEnoughRoom` 全仓只出自 `RuleCore.CanPlayCard` 的 `!BoardSlots.HasRoomFor`（**单位卡**那条，`RuleCore.cs:1366`），与战术卡无关。

⇒ 只有一种可能，所以**没有**写「两种都可能」。改前 `…⇒ **整张卡打不出去**（`ErrSlot`）。` → 改后同上改成 `` `ErrNoTargetAvailable` `` + 5 行留痕（上面三条判据）。

---
### ④ 删的四个口 · grep 证据

**删前自查 grep**（`--include=*.cs`，全 `CardPresentation`）：四个名字的全部命中只在 ① `TutorialOverlay.cs` 自己的定义/doc；② `BattleDriver.cs:7746/7749/11372` · `BattleScene.cs:14780/14978` 的**注释** ⇒ **零代码调用**（改前逐处读过：那两段断言读的已是 `driver.Settings.*`）。

删的四个（`CardPresentation/Battle/TutorialOverlay.cs`，连各自 doc 一起删）：`SkipPanel` 字段 `:130`（doc `:122-129`）· `SkipVisible` 属性 `:214`（doc `:208-213`）· `SkipWorldPos` 属性 `:222`（doc `:220-221`）· `ClickSkipAt` 方法 `:526-529`（doc `:517-525`）。
每处**换成 2–4 行留痕注释**（「原来有什么 · A991 那轮为什么留 · 现在为什么能删 · 真身在哪」），另就地订正 **3 处**因删而失真的注释：文件头 `:112-116`（原写「本件只剩那几个只读转发口」）· `Build` 里 `:344-345`（原写「+ 三个转发口」）· 上述四处 doc。
⛔ **`minTimeBeforeSkip` 那道闸一个字没动**（`MinTimeBeforeSkip` / `SetSkip` / `SkipAllowed` / `TrySkip` / `SkipClickedByPlayer` 全在；`SkipAllowed` 的 doc 照旧标着「本工程自留、原版没有」）。

**改后注释里还剩 12 处引用，⛔ 一处都不是代码**：`BattleDriver.cs` 3 处（`:7746/:7749/:11372`）· `Editor/BattleScene.cs` 2 处（`:14780`/`:14978`）—— **这 5 处不在我白名单、未动**；`TutorialOverlay.cs` 自己 7 处（全是本次新写的留痕 + 文件头那两句）。
⚠️ `BattleScene.cs:14978` 那句「`ClickSkipAt` / `SkipWorldPos` 只是**转发**」现已不成立（口没了）—— 只报不动。

---
### ⑤ 注释：改前 / 改后

**定位** = `CardPresentation/Shell/BoosterInfoPopup.cs` · `BuildTooltipHit` 的 doc 里判据 **①** 那两行。**代码一行未改**（`TooltipR ± 15f` 本来就是外扩口径）。

**改前**：① 全包 **11880** 个带 `m_RaycastPadding` 的件里 **11672** 个是 (0,0,0,0)，非零的几乎全是负值（…；**唯一一个正的**在一件 246.8,84.4,338.6,132.4 的怪件上）；

**改后**（**我独立现扫**的数，⛔ 不是抄简报）—— `d:/2/新解包资源/assets_full/**` 全扫（`grep -rl "m_RaycastPadding"` ⇒ **22289 个文件**，每件恰 1 个，再逐件解四分量）：
- `(0,0,0,0)` **21845** 个 · **非零 444** 个（**28 组**不同取值）；非零里绝大多数确实是负值（−8/−10/−11/−12/−15/−20/−23.13/−25/−30/−40…），但**含正分量的有 5 组 / 68 处**（⛔ 不是「唯一一个正的」）：`(10,10,10,10)`×52 · `(0,1000,0,0)`×13 · `(246.8,84.44,338.6,132.38)`×1 · `(42,25,46.5,27.5)`×1 · `(0.03,−27.62,−59.06,0)`×1。
- **`Dark Shade` 那条现核到文件级**：`assets_full/bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4415.json` 的 `m_RaycastPadding = (0,1000,0,0)`，其 `m_GameObject.m_PathID = 990` ⇔ `GameObject/Dark Shade_990.json`（与简报的例子一致）。
- **结论不变**：定符号靠同段 **②**（`Handle` 4.141×50.597 + padding −25 那条算术反证），① 只是旁证；且订正后 ① **更自洽**——那 52 个**全正**的 `(10,10,10,10)` 按「正 = 内缩」读才讲得通，与「负 = 外扩」互为镜像。
- **错因**照写：把**更早一版范围更窄**的扫描结果贴了「全包」标签（同族教训：先查有没有口径不同的旧数）。

---
### 类型检查 · 行尾 · 没查清

- **类型检查**：`TMPDIR=/tmp/wf_ec bash d:/4/Unity/工具/typecheck.sh` ⇒ 末次 **运行时 0 · 编辑器 0**（期间没有别人的半成品报错混进来）。
- **行尾**（改后二进制现数）：五件**全是纯 LF**（crlf = 0、bare LF = 行数），与改前一致 ⇒ **一个字节没翻**（全程 Edit 工具，⛔ 未用 `sed -i`）。`git diff --numstat`（增/删，**⚠️ 这是相对 HEAD 的累计量、含本会话前面几批在同一文件上的未提交改动，不全是我这五笔**）：`RuleEngineTest_BoardModel 13/3` · `RuleCodes 122/0` · `EffectText 9/2` · `TutorialOverlay 102/87` · `BoosterInfoPopup 18/2`。**我自己那几笔的量**：`RuleEngineTest_BoardModel` = 2 行替换 + 9 行注释（13/3 ≈ 全是我）· `RuleCodes` = **13 行**（2 表项 + 11 行注释；那 122 里其余 ~108 行是**上一批拆码**留下的）· `EffectText` = 1 行换成 10 行 · `TutorialOverlay` = 删 4 个口 + 写留痕（102/87）· `BoosterInfoPopup` = 纯注释。⇒ 量级与改动相称，**没有整篇重写**。
- ⛔ **没跑 Unity** ⇒ ① 那两条断言、④ 的删口**只是静态核对过**，未经实跑验证。⚠️ 若 `RuleEngineTest.Run` 红在 ① 那一块，第一嫌疑是「`HasRoomFor` 那一格返回的还不是 19」（我只现读了 `RuleCore.cs:1366` 那一行，没实跑）。
- **没查清 / 只报不动（都不在我白名单）**：① 上文那条「`Battle/Tips/*` 印键名」；② `BattleDriver.cs:7567` 的 doc 仍写「`TermKey` 只映射了**两条**」—— 现读 `Terms` 已是 **4 条**（多了 18/19），**过时**；③ `Editor/BattleScene.cs:3031` 注释「原版那一档还盖着 `NotEnoughRoom`」**拆码后不成立**；④ 同文件 `:14978` 说那两口「只是转发」**已过时**；⑤ 未核 `Describe` fallback 对**哪些**码还可达（靠实跑枚举才准；我只静态核过「0–11 / 13–19 全有名字，`Names` 现 19 条」）。
