# W_战斗措辞 —— `A971` + 三处过期注释（铁律 5）

写手：执行代理（战斗侧 · 纯注释/措辞）。**一行行为都没改**（`git diff` 逐行核过：全在 `//` / `///` / 一条日志串里）。
白名单三件：`Battle/BattleDriver.cs` · `Battle/UnitChatPanel.cs` · `Battle/TutorialOverlay.cs`（⛔ 别的文件一个没碰）。

---

## ① `A971` —— 逐处（内容定位 · 旧文 · 新文）

统一口径：**「聊天三档」→「聊天五档」**（枚举值本身一个字没动）。

| # | 内容定位（按内容找，不看行号） | 旧文 | 新文 |
|---|---|---|---|
| 1 | `A940` 尾账那一节的分节横幅 | `**动作音效 + 聊天三档 + 「等提示」**` | `**动作音效 + 聊天五档 + 「等提示」**` |
| 2 | `PlayTutorialSound` doc 末段 | `⚠️ **聊天那三档不走这里**` | `⚠️ **聊天那五档不走这里**` |
| 3 | `TutorialSpeakingCard` doc 首句 | `聊天那三档要用它的立绘/卡名。` | `聊天那五档要用它的立绘/卡名。` |
| 4 | `SpeakTutorialChat` doc 抬头 | `🆕 **聊天三档**（`PlayerChat 50` / … / `RadioMessage 90`）` | `🆕 **聊天五档**（…同 5 档，一字未动…）`。⚠️ 紧跟的「**× 三颗气泡**」**保留** —— 那是**气泡数**（`PlayerChatDisplay/EnemyChatDisplay/RadioChat`），本来就对 |
| 5 | **出声文案**（运行时日志串）—— 见下节专述 | `Debug.LogWarning("[Tutorial] 聊天三档要 `UnitChatPanel`，…")` | `…聊天五档要 `UnitChatPanel`，…`（**只改「三→五」两字**） |
| 6 | 战后脚本 13 条那一大段注释 | `（聊天三档 + 音效）` | `（聊天五档 + 音效）` |
| 7 | `_postStep` 推进处注释 | `它自己会放音效、演聊天三档；` | `它自己会放音效、演聊天五档；` |
| 8 | `ApplyTutorialActionView` ⓪ 段注释 | `⚠️ 聊天那三档在 `PlayTutorialSound` 里被跳过` | `⚠️ 聊天那五档在 …` |
| 9 | `ApplyTutorialActionView` ②′ 段抬头 | `// ---- ②′ 🆕 …（`A940` 尾账 · 聊天三档）----` | `… · 聊天五档）----` |
| 10 | 🆕 `Battle/UnitChatPanel.cs` `_radio` 字段 doc | `🆕 2026-10-18（`A940` 尾账 · 聊天三档）：**第三颗气泡…**` | `… 聊天五档）：**第三颗气泡…**` |

全仓 `聊天三档` / `聊天那三档` 现已 **0 处**（`grep -rn "聊天三档\|聊天那三档" Battle/` = 空）。

### 第 5 处（出声文案）：**改了**
- 先按要求 `grep` 全仓：`聊天三档要` 只命中**源码本身** + `资料/普查产出_1018第三会话/RECON_外壳族.md:101`（账，不是断言）。
- 再核「有没有测试在**抓日志流**时按这句话过滤」：本仓抓日志一律走 `Application.logMessageReceived`
  （`BattleScene.cs` / `NetBattleTest.cs` / `SettingsScene.cs` … 二十余处），我 grep 了它们的**过滤词** ——
  **没有任何一处**按 `[Tutorial]` / `聊天` / `没显示` / `UnitChatPanel` 过滤（`grep -rn '\[Tutorial\]' Editor/` = 空）。
- ⇒ 改它**不会红任何断言**；但它**会改运行时输出**，按约定**单列**：新串 =
  `[Tutorial] 聊天五档要 `UnitChatPanel`，可它是 null ⇒ 这一句**没显示**（不静默）；…`。

---

## ② 那 6 处「零接线」——逐处「为什么不该改」

⛔ 全部**未改**（除 ① 那一处，见 ③）。前 5 处**不在我的白名单**（`Editor/**`、`Net/**`）。

| # | 文件:行（现核） | 上下文 | 为什么**正确、不该改** |
|---|---|---|---|
| ① | `Battle/BattleDriver.cs:5858,5860`（`LeaveNetRoom` doc） | 见 ③ | ⚠️ **这处是【现在时】、且今天不成立** ⇒ 与其余 5 处不同，**已按 ③ 订正**（留在引号里的旧文才带「零接线」二字） |
| ② | `Editor/NetBattleTest.cs:570` | `// 原来 NetSession.OnClosed / OnPeerLost 在**生产侧零接线**…` | **过去时**（「原来」）+ 正在解释**这条新测试为什么值得存在**；改了就没了「从无到有」的对照 |
| ③ | `Editor/NetBattleTest.cs:628` | 断言文案 `★（对面掉线）…（原来零接线 ⇒ 一条都没有；实得 N 条）` | **断言文案里的历史**；「原来 ⇒ 一条都没有」是**这条断言的意义**。改文案 = 改测试表意 |
| ④ | `Editor/NetBattleTest.cs:650` | 同上，对面**离开**那一条 | 同上 |
| ⑤ | `Editor/NetSelfTest.cs:537` | `"M⑥ ★ 大厅阶段对面掉线要弹一条（原来零接线 ⇒ 一条都没有）"` | 同上（**大厅层**那一支） |
| ⑥ | `Net/NetBattle.cs:244` | `// 账 A880：…原来**全仓零接线**（只有定义 + 三处 Invoke）` | **过去时**（「原来」）—— 它正是**B13/A880 这笔账的账头**，改掉就把「这笔账在还什么」抹了 |

---

## ③ 「零接线」那一处（`Battle/BattleDriver.cs` `LeaveNetRoom` doc）：改前 / 改后

**改前**（现在时、今天不成立）：
> 🔴 **如实记一条还没接的**（不在本件白名单，归联机线）：对面收到 `bye` 之后 `NetSession` 会 `SetState(Closed, why)` 再回调 `OnClosed` —— 而 **`OnClosed` 全仓零接线**（只有定义与三处 `Invoke`，生产侧没人订阅）⇒ 对面那台**收得到、玩家看不到**。同一族的还有 `OnPeerLost`（掉线那条）也零接线。⛔ 别在这里自己发明 UI —— 原版那半边的判据（Photon 的 `OnLeftRoom` / `OnPlayerLeftRoom`）**没查到**，先记账。

**改后**（如实 + 更正痕迹 + 出处）：
> 🔴 **2026-10-18 就地订正（铁律 5）**：本段原来写着「……`OnClosed` 全仓零接线……`OnPeerLost` 也零接线」—— **这两句今天不成立**：那两条**早就接上了** ——
> `Net/NetBattle.cs:279-280`（`WireSession()` 里 `_s.OnPeerLost += HandlePeerLost` / `_s.OnClosed += HandlePeerClosed`）、大厅那层另有 `Net/NetMatchmaking.cs:134`
> ⇒ 对面那台**收得到、也看得到**（`Editor/NetBattleTest.cs` 的 M⑨ 那两条断言测的就是「弹了一条给人的提示」）。
> ⚠️ 顺带：原来那句「原版那半边**没查到**」**也已过期** —— 判据见 `Net/NetBattle.cs` 那一节头部（`…SetOpponentDisconnected.c` → `…__ShowDisconnectionPopUp.c` → `WindowsManager.ShowPopUp`，文案键 `Battle/HUD/WaitOpponentConnectionMsg`；原版**没有**「对面主动离开」的专属窗，两条共用一条链）。⛔ 但「弹出什么」那一层仍是**我们挑的**措辞。

- 行号：账上记 `:5417-5421`、现核读 `:5857-5861`（我改的正是这一处，逐字比对内容确认同一处，**未**误伤其余 5 处）。

---

## ④ 两处错注释：改前 / 改后

### ④a `Battle/BattleDriver.cs` · `HandleTutorialSkip` doc（`SkipTutorial` 那一颗钮）
**改前**（两版说法，两版都不成立）：先写「⚠️ 没查清：**场景节点**在 `Bottom buttons/SkipTutorial Button [22,1375]`（**HUD 根下**），而**处理函数名**却是 `BattleSettingsWindow__…` ⇒ 两处对不上」；后（`Z7`）改写成「那**是两颗不同的钮**：设置窗里一颗 + HUD 底部一颗，**后者的处理器活在场景的序列化 `onClick` 里**」。

**改后**（逐条现核 · 铁律 2）：
- 🔴 **两版都不成立**。
- **全库只有一颗钮**：13 个战场场景各只有一份 `SkipTutorial Button`（判据 `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena*/GameObject/SkipTutorial Button.json`，**13/13**）。
- **它在设置窗里，不在 HUD 根下**：按 `m_Father` 逐跳解父链（arena1）= `SkipTutorial Button` → `Bottom buttons` → **`BattleSettingsPanel`** → `Safe area FrontCanvas` → `FrontCanvas` → `Canvas`（起点 `…battlearena1/RectTransform/RectTransform_3271.json`；文字版 `资料/说明书/01_战斗_对战/2D层_battlearena1全树.md:718-722`，它在 `BattleSettingsPanel`（`:673`）之下、**不是** `Tutorial`（`:733`）之下）。
- **序列化 `onClick` 是空表**：那钮的 uGUI `Button`（`MonoBehaviour_4945.json`）`m_OnClick.m_PersistentCalls.m_Calls` = `[]`（**13/13 同**；我只实读了 arena1 的 `MonoBehaviour_4945.json`，13/13 那句照账）。
- ⇒ 处理器**只有代码接的那一处**（`SkipTutorialButtonOnClick`，判据 `d:/2/tools/decomp_full/BattleSettingsWindow__SkipTutorialButtonOnClick.c`）⇒ 场景节点与函数名**本来就是同一颗钮**，「对不上」是**我们自己看错**。
- 并如实标了**我们这边与它不同**：跳过钮建在 `TutorialOverlay` 自己那棵子树上、我们的 `SettingsPanel` 里没有这一颗（⛔ 别读成「原版 HUD 一颗 + 设置窗一颗」）。

### ④b `Battle/TutorialOverlay.cs` · 类 doc
**改前**：`🔑 **为什么合成一个文件/一件**：原版那六层**本来就挂在同一个 `Tutorial` 根下**（见文件头那棵树）…`

**改后**：原句作废 + 更正痕迹。现读事实（`bundle_scenes_scenes_battlearena1` 的 `RectTransform/` 按 `m_Father` 解父链 ＋ 全树 md）：
- **只有 4 层**在 `Tutorial`（pid 3013）之下：`InitTutorialTip` / `TutorialArrows` / `TutorialTip` / `TutorialPointerCombat`。
- 另三层**都不在这棵根下**：`TutorialObjs` ⇒ `Card Display/Card Display Window`（全树 `:578`）；`Tutorial highlight` ⇒ **`Canvas` 的直接子物体**（与 `FrontCanvas` 平级，全树 `:799`）；`SkipTutorial Button` ⇒ **`BattleSettingsPanel/Bottom buttons`**（全树 `:673`/`:718-722`）。
- ⇒ 合并成一件的理由是**共享同一套显隐驱动**，**不是**「同一个根」。
- ⛔ **文件头那棵「树图」一字未动**（按要求：那一段是对的）。

---

## ⑤ 类型检查 / 行尾

- `TMPDIR=/tmp/wf_battle bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时错误数: 0 · 编辑器错误数: 0**（无「别人半成品」伪错）。
- `git diff --numstat Battle/` ⇒ `BattleDriver.cs 43/24` · `TutorialOverlay.cs 13/3` · `UnitChatPanel.cs 1/1`（**都不是整篇重写**）；
  行尾复核：`BattleDriver.cs` CRLF **13023/13023**（未翻）· `UnitChatPanel.cs` / `TutorialOverlay.cs` 纯 LF（未翻）。改动**全部在注释里**。
- 未跑 Unity 批处理（禁止）；未动 git；未改两张正本；未越白名单。

---

## ⑥ 没查清 / 顺手发现（**没动**，交调度台分流）

1. 🔴 **真差异（行为，不在本件范围）**：**原版那颗「Skip tutorial」在设置窗里**（`BattleSettingsPanel/Bottom buttons`，该面板场景里 `(inactive)`），
   而我们把它建在 **`TutorialOverlay` 的 HUD 子树上**（`TutorialOverlay.cs` `_skipGo.transform.SetParent(_root,…)`，`_root = transform`），
   且我们的 `SettingsPanel.cs` **没有**这一颗（它只 grep 到 `Battle/Settings/SkipTutorial` 这条**词条**与 `Resign`）。
   ⇒ **落点与原版不同**（铁律 11：该记该做，⛔ 不是「影响小不做」）。**本件只改注释，没动行为**。
   相关一条**已存疑的空缺**：`TutorialOverlay.cs:143-145` `SkipAt01` 的注释仍写「原版那个 `[22,1375]` 的**父链偏移没能标定**」——
   父链**现在标定了**（`BattleSettingsPanel/Bottom buttons`），但那两个坐标（1370/1375 > 1080）疑似「VLG 布局跑之前的模板位」（CLAUDE.md 那条），
   **最终屏幕偏移我这次没证出来** ⇒ 只报不改（且它属「摆位是我们挑的」那一族）。
2. ⚠️ **文件头那棵树是「挑出来的摘录」、不是完整层级**：它把 `- Bottom buttons … → SkipTutorial Button …` 与 `- Tutorial` 并列写，
   **省略了中间的 `BattleSettingsPanel`**（真实父链见 ④b）。按要求**未动**；若要动，建议只加一行父链备注。
3. ⚠️ 「13/13 同」这句我**只实读了 arena1 一个场景**的三份 json（`GameObject/SkipTutorial Button.json` · `RectTransform_3271.json` · `MonoBehaviour_4945.json`）＋ 13 个包的**文件计数**（每包恰 1 份）—— **逐场 m_OnClick 逐份读是照账**，若要写死进正文请再核一遍 13 份。
4. ✅ 未发现其他「注释与行为不符**且**修它要动行为」的地方（若发现会只报不改）。
