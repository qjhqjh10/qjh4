# 查证 · `P6d` 走线文案的协议兼容面（只读代理交件 · 第六会话）

> ⚠️ **原件说明**：本条由一个**只读代理**产出（其工具表无 Write）⇒ **它自己没能落盘**，
> 下面全文由**调度台**转存。**内容一字未改**（只补了标题与这一行说明）。
> 🔴 **行号声明（代理原话）**：以下全部是 **2026-10-08 它现读那一刻** 的读数
> （`NetSession.cs` 665 行 · `NetBattle.cs` 1147 行 · `NetRuntime.cs` 195 行 · `NetProtocol.cs` 421 行）。
> 同批 `P6a/P6b/P6c` 正在改别的行 ⇒ 交件后可能再漂。⛔ 它一个字没改。

---

## ① 结论（六件事，逐条一句）

1. **逐处定位**：7 处**全部属实**，但**不是同一个消息**——是 **`MsgBye.reason` ×5**（其中 1 处是 `Close()` 的兜底参数、另 1 处经 `Close()` 中转）+ **`MsgReject.reason` ×1** + **`NetRuntime:179` 走的是同一条 `Close→Bye` 路**（不是"第三条消息"）。⚠️ 简报把 `:461` 当 `ack`、把 `NetRuntime:179` 当独立消息，**都不准**。
2. **协议里【没有】现成的「枚举/键 → 文案」转换口** —— 🔴 **`NetProtocol.cs` 里 `Loc` 零命中**（`grep Loc Net/NetProtocol.cs` = 0）。但 🟢 **同一批代码里有一条【形状完全对口】的先例**：`NetPendingBattle.PlayMode`（`Net/NetBattle.cs:54-60` + `RuleEngine/Core/GameplayVariables.cs:55-79` 的 `PlayModeNames`）—— **线上一律发枚举名、收侧 `Parse` 回值、认不出就出声退回 `Classic`**，而且 `GameplayVariables.cs:59-60` 明写这个编码**换过一次、旧档照样读回来、不用改存档格式、也没 +版本号**。⇒ **有先例，照它做。**
3. **版本握手**：✅ **有** = `NetProtocol.Version = 2`（`NetProtocol.cs:198-202`）+ `MsgProof.protoVer`（`:50`，发出 `NetSession.cs:348`，校验 `NetSession.cs:362`）。**不等就硬拒**（Ack `ok=false` + 关台）。**旧客户端收到键标识**（**按代码判**）：**不崩、不被忽略、会把键名当正文印出来**（`NetSession.cs:477` 的 `ClampPeerText` 原样进 `StatusText` 与 `OnClosed`；7 条键最长 32 字 ≤ `MaxPeerTextChars=40` ⇒ **连截断都不会发生**）。**新客户端收到旧中文串**：`Loc.T(中文串)` **返回该中文串本身**（缺键回键名）+ 记一次 `MissingCount` + 一条 LogWarning（按键去重）⇒ **显示恰好正确**，但**污染缺键记账**。⚠️ `Loc.HasEntry(中文串) == false` —— **全仓没有反向查表**（见 ③）。
4. **录像**：🔴 **这 7 处【不进】录像**。`MsgAction` **确实**是"同一结构既上网又落盘"（`ReplayStore.cs:113` 的 `List<MsgAction>` = 线上 `NetKind.Action/Applied` 那份，`ReplayStore.cs:13` 逐字承认），但**这 7 个串在 `MsgBye`/`MsgReject`/`MsgAck` 上，不在 `MsgAction` 上**；`MsgAction`（`NetProtocol.cs:87-123`）**没有任何 `reason` 字段**，`RecRaw`（`BattleDriver.cs:520-526`）落的也只有 `kind/actor/marks/envSlot/envSO/defId` ⇒ **落盘里一个走线理由串都没有**。
5. **对端是谁**：**不是"发全部对端"** —— **5 处实质只走主机→客机**（`:434`/`:442`/`NetBattle:789`/`NetBattle:992`，加 `NetSession:376` 的 Ack），**只有 `:461` 是客机→主机**，`:567`/`NetRuntime:179` 两端都可能（看调用方）。⇒ **不存在"只有一侧会看到"的盲区，但存在"一侧永远看不到"的 5 处**。
6. **测试面**：**现读只有 3 条断言真读这类串**（`NetBattleTest.cs:994` · `NetSelfTest.cs:596` · `NetBattleTest.cs:652`），**改完最多红 3 条、且只有 1 条无条件红**（详表见 ②·断言）。**没有任何断言查过那 7 条 `Wire/*` 键**。

---

## ② 逐处表

### 2·A 7 处走线点（现读行号 | 消息/字段 | 现在发什么 | 改成什么键 | 方向）

| `文件:现读行号` | 消息 / 字段 | 现在发什么 | 建议键标识 | 方向 |
|---|---|---|---|---|
| `Net/NetSession.cs:434` | `MsgBye.reason`（`case NetKind.Reconnect`，**主机**收到坏 token） | 「这把钥匙对不上这一局」 | `Settings/Online/Wire/BadKey` | **主机→客机** |
| `Net/NetSession.cs:442` | `MsgBye.reason`（同一 case，主机没挂 `ResumeProvider`） | 「主机这边没有这一局的记录」 | `…/NoRecord` | **主机→客机** |
| `Net/NetSession.cs:461` | `Close(true,"重连包解不出来")` ⇒ 经 `:567` 拼进 **`MsgBye.reason`**（在 `case NetKind.Resume` = **客机**收到） | 「重连包解不出来」 | `…/BadResume` | 🔴 **客机→主机**（唯一反向） |
| `Net/NetSession.cs:567` | `MsgBye.reason` = `reason ?? "对面结束了这一局"`（`Close(say,reason)` 的**兜底**） | 兜底串「对面结束了这一局」**+ 调用方传的任意串**（3 个生产调用点，见 2·C） | `…/PeerDone`（**只兜底那一份**） | **两端都发**（看调用方） |
| `Net/NetBattle.cs:789` | `_s.Close(true,…)` ⇒ `MsgBye.reason`（`ReconnectCountdownExpired` 的 `else` 支） | 「这一局的联机房间已经散了」 | `…/RoomGone` | **主机→客机**（判据：`:692 iAmDisconnectedEnd = _s.Role == NetRole.Client`，进 `else` ⇒ 本机是主机） |
| `Net/NetBattle.cs:992` | **`MsgReject.reason`**（`case NetKind.Action`，`:969 if (!IsHost) return`） | 「不是你的回合」 | `…/NotYourTurn` | **主机→客机** |
| `Net/NetRuntime.cs:179` | `Session.Close(true,"主机重开了")` ⇒ `MsgBye.reason`（`Reset()`） | 「主机重开了」 | `…/HostRestarted` | **两端都可能有**：`Reset()` 被 `Shell/SettingsWindow.cs:2843`（【保存】= 主机）与 `:2878`（【检查连接】= 客机）调用 |

**7 条键的字符长度**（对 `MaxPeerTextChars=40` 的意义）：`Settings/Online/Wire/` 前缀 21 字 + 后缀 ⇒ **27 / 29 / 29 / 30 / 32 / 34**，**全部 ≤ 40** ⇒ `ClampPeerText` **一次都不会截断**。✅

### 2·B 🔴 7 处之外，还有 3 条**同样过 TCP** 的「我们的中文串」（简报漏了）

| `文件:现读行号` | 消息/字段 | 现在发什么 | 说明 |
|---|---|---|---|
| `Net/NetSession.cs:376` | `MsgAck.reason` = `why ?? ""` | 「对面发来的握手包解不出来」/「两边版本不一样（对面协议 v{0}，本机 v{1}）—— 要用同一份构建」/「密码不对」 | **主机→客机**；`:360-363` 三个取值来源 |
| `Net/NetBattle.cs:998` | `MsgReject.reason` = **`RuleCodes.Describe(code)`** | `RuleCodes.cs:68-98` 那张 `Names` 表的**中文整句**（16 条） | **主机→客机**；🔴 `RuleCodes.cs:95` 逐字写「⛔ 下面是**给人看的中文整句**，**不是** I2 词条键 ——『`Describe` 改出键』是 `A985⑧` 第 ③ 步，本轮不做」⇒ **这一处今天改不动** |
| `Battle/BattleDriver.cs:6158` | `s.Close(true,"对面离开了这一局")` ⇒ `MsgBye.reason`（`LeaveNetRoom()`） | 「对面离开了这一局」 | **两端**（`OnDestroy`/结算/`NetSelfTest:590` 同一口） |

### 2·C `NetSession.Close(reason)` 的 3 个生产调用点（都经 `:567` 上网）

| 调用点 | 传的串 | 方向 |
|---|---|---|
| `Battle/BattleDriver.cs:6158` | 「对面离开了这一局」 | 两端 |
| `Net/NetRuntime.cs:179` | 「主机重开了」 | 两端 |
| `Net/NetBattle.cs:789` | 「这一局的联机房间已经散了」 | 主机→客机 |

⚠️ `NetSession.cs:563` 那句 `Debug.LogWarning` 里**也**有「对面结束了这一局」——那是**仅日志**，不上网。

### 2·D 断言表（现读行号 + 断什么 + 改后红不红）

| 断言 | 断什么 | 改后 |
|---|---|---|
| `Editor/NetBattleTest.cs:994` | `hsC.StatusText.Contains("这一局的联机房间已经散了")`（**子串**，读的是 `:789` 传给 `Close` 的那串经 `SetState(Closed,reason)` 落成的本机状态字） | 🔴 **中文档绿 / 英文档红**（改 `Loc.T` 后 EN = "This match's online room is gone"）⇒ **单语档断言**，与 `A1031⑦(a)` 同族 |
| `Editor/NetSelfTest.cs:596` (M⑳) | `n2[0].Contains("自检：对面离开了这一局")`（**子串**；那个串是**自检自己**在 `:590` 传给 `cli.Close(true,…)` 的，经 `:567` 原样上网 → 对面 `HandleLobbyPeerClosed` → 弹窗正文） | ⚠️ **绿 ⇔ 收侧仍原样回显"不认识的串"**。若 P6d 把收侧改成"只认键、不认识的落一句本地固定话" ⇒ **红** |
| `Editor/NetBattleTest.cs:652` | `n9c[0].Contains("离开")`（**子串**；同理，串来自 `:645 cs9.Close(true,"对面离开了这一局")`） | ⚠️ **同上条件**（本行只断"离开"二字，比 M⑳ 略宽） |
| `Editor/NetBattleTest.cs:655` | `hb9.LastSay.Contains("结束")` → 断本地固定前缀「联机对局结束：」 | ✅ 绿 |
| `Editor/NetBattleTest.cs:1193 / 1195` | `Contains("弃权")` → 本地固定话 | ✅ 绿 |
| `Editor/NetSelfTest.cs:173` (E④) | `host.LastError.Contains("版本")` → 断 `NetSession.cs:363` 那句（**Ack 族，不在 7 条内**） | ✅ 绿（除非 Ack 族也一并改） |
| `Editor/NetSelfTest.cs:1162-1304`（Q①–Q⑭，A961 钳位那一族） | 灌 `200×'A' + "TAIL-MARK"`，断**长度 ≤ 41** 与**不含 TAIL** | ✅ 绿（**与 7 个字面量无关**）—— ⚠️ 但若收侧加 `Loc.T(x)`，**每条对端串都会 `MissingCount++`**；已核：**没有 net 自检断 `MissingCount`**（消费点只有 `Editor/BattleScene.cs:3042` / `DeckScene.cs:5696` / `SettingsScene.cs:688`，都在别的域） |
| 全仓 | 断 `Settings/Online/Wire/*` 这 7 条键**存在** | 🔴 **一条都没有**（`grep -rn "Online/Wire" Editor/*.cs` = 0） |

---

## ③ 证据（逐条 `文件:行号`）

**问题 2：有没有现成的"发 ID / 枚举、收侧取词"口？**
- ❌ `NetProtocol.cs` 全文 **`Loc` 零命中**；也没有任何"码 → 文案""键 → 文案"的表。
- ✅ **先例（形状完全对口）**：`Net/NetBattle.cs:54-60` `NetPendingBattle.PlayMode { get { return PlayModeNames.Parse(Mode); } }` + `RuleEngine/Core/GameplayVariables.cs:62-79` `PlayModeNames.Name/Parse`。
  - `:70-79 Parse`：`Enum.TryParse` + `IsDefined`，**认不出 ⇒ `Debug.LogWarning` + 退回 `Classic`**（不是静默）。
  - `:57-60` 逐字：「字段还是那个 `Mode` 字符串，只是**编码换了**：老编码只有 `"Classic"`/`"Skirmish"`… ⇒ 旧录像/旧开局包照样读得回来」。
  - `:30-31`：「枚举顺序/名字不许改：它是**跨场景 · 过网 · 落盘**共用的字面量口径」。
  - 🔴 **关键**：这次编码变更 **没有 +`NetProtocol.Version`**（`Version` 至今 = `2`，`NetProtocol.cs:202`；`PlayModeNames` 那笔是 `2026-10-15 A383`，晚于 `Version 1→2` 的 `2026-10-01`）⇒ **仓库里已有一条"改线上字符串编码但不动版本号、靠收侧出声兜底"的先例**。
- ✅ **另一条（反向）先例**：`NetProtocol.cs:154-156` `MsgMatchCancel.reason` 的 doc 逐字：「`reason` 只进日志（**给对面看的那句话固定，不渲染对面传来的任意文本**）」，实现见 `Net/NetMatchmaking.cs:508`（`reason` 只进 `Debug.Log`）与 `:509-510`（上屏的是**本地固定句**）。⛔ **这是"干脆不渲染对端文本"那条路，与 P6d 要的"取词"是相反的选择** —— 别把它当成"已经这么做了"。

**问题 3：版本握手 / 兼容**
- `NetProtocol.cs:198-202`：`/// 协议版本。🔴 改消息格式就必须 +1 …` · `public const int Version = 2;`（注释记 `2026-10-01：1 → 2`，**理由是引擎语义变了、不是消息格式变了** —— 这条先例支持"语义变了也该 +1"）。
- 发出：`NetSession.cs:346-352`（`protoVer = NetProtocol.Version`）。
- 校验：`NetSession.cs:362-363` `else if (m.protoVer != NetProtocol.Version) why = $"两边版本不一样（对面协议 v{m.protoVer}，本机 v{NetProtocol.Version}）—— 要用同一份构建";` ⇒ `:376` 回 `Ack{ok=false, reason=why}` + `:380-382 SetState(Closed,"拒绝了这次连接："+why)` + `_t.Close()` + `OnClosed(why)`。**硬拒，不静默兼容。**
- 旧客户端收键标识的**真实路径**：`NetSession.cs:471-482`（`case NetKind.Bye`）→ `:477-478 ClampPeerText(m.reason)` → `:479 SetState(Closed, why)` → `:481 OnClosed(why)` → `NetBattle.cs:372 HandlePeerClosed(why)` → `:381 body = why` → `:385-389 SayPeerGone("联机对局结束："+body… )`；大厅那一半 = `NetMatchmaking.cs:205-216`。**⇒ 印出键名，不崩、不忽略。**
- `MsgReject` 旧客户端路径：`NetBattle.cs:1047 Abort($"主机拒绝了我的动作：{ClampPeerText(m.reason)}")` → `:232 NetSay` + `:236 Notice`。**⇒ 同样是印键名。**
- 本地已有相关键：`Core/Loc.cs:1427 Settings/Online/St/VersionMismatch`（本机显示那一族的 ZH 逐字同 `:363`）—— 说明"本机显示"与"走线那句"是**两族**（`St/*` vs `Wire/*`，`:1645-1647` 逐字说明）。

**问题 4：录像**
- `Battle/ReplayStore.cs:112-113`：`/// 动作流…` `public List<MsgAction> actions` = **线上那一个结构**；`:13` 逐字「**一条动作 = 一个 `MsgAction`**，这正是联机重连重放用的那个结构」= 🟢 **同一结构既上网又落盘（`A985⑬` 那条判据由此成立）**。
- 但 `Net/NetProtocol.cs:87-123 MsgAction` **字段清单里没有 `reason`**（`seq/actor/kind/handId/handIdx/slot/targetP/targetSlot/ranged/altKeyword/picks/pickIds/marks/preApplied/envSlot/envSO/defId`）。
- `Battle/BattleDriver.cs:520-526 RecRaw` 落的 `MsgAction` 只填 `seq/kind/actor/marks/envSlot/envSO/defId`；`:539-553 RecFinish` 只补 `myHero/foeHero/result/finalHash` —— **没有理由串**。`RecKindForfeit = 200`（`:366`）只是**一个 int kind**。
- `ReplayStore.cs:51-132 ReplayRecord` 全文读毕：**无 `reason` 字段**（`result`/`finalHash`/`start`/`actions`/`trace`/`traceLogTail`/`traceState`/`tutorialStage`）。
- 旁证：`资料/普查产出_第四会话/现核_引擎与战斗族.md:13` 记 `A985⑬`**仍开着**（"录像头尾各一条只落盘记录" 还没做）；`:36` 逐字：「**`RecRaw` 与 `MsgAction` 是同一结构**…**别指望在 `MsgAction` 上加字段还能不上网**」。
- ✅ **结论**：**这 7 处不进录像**（半条都不进）。⚠️ 唯一沾边的是 `ReplayRecord.traceState`（`BattleDriver.StateBrief` 的局面速写，含卡名等中文），但那是**别族的串**、且已核不含这 7 条。

**问题 5：方向**（判据行已在 2·A 表内注出）
- `:434`/`:442` 在 `case NetKind.Reconnect: // 主机收到`（`NetSession.cs:429`）⇒ **只有主机发**。
- `:461` 在 `case NetKind.Resume: // 客机收到：全量重放`（`:458`）⇒ **只有客机发**。
- `NetBattle.cs:992` 在 `case NetKind.Action: // 只有主机会收到`（`:967`）+ `:969 if (!IsHost) return` ⇒ **只有主机发**。
- `NetBattle.cs:789` 在 `!iAmDisconnectedEnd` 支（`:748`，`iAmDisconnectedEnd = _s.Role == NetRole.Client`，`:692`）⇒ **只有主机发**。
- `:567` / `NetRuntime:179`：**两端都能发**（`Close` 是公开 API；`Reset()` 被主客两条钮调用）。

**问题 6**：见 2·D 表。

---

## ④ 兼容面裁定建议（三条各一句）

1. **旧客户端**：🔴 **建议【不 +版本号】，照 `PlayModeNames` 那条先例走** —— 收侧一律 `Loc.HasEntry(x) ? Loc.T(x) : x`（**不是** `Loc.T(x)` 裸调，见 ⑤·2 与下方）：
   - 旧收新键 ⇒ **印键名（27~32 字，不被钳）**、不崩、不忽略 —— 可接受但难看；
   - 新收旧中文串 ⇒ `HasEntry=false` ⇒ **原样回显中文**，**显示恰好正确**（这是白赚的兼容）。
   - ⛔ **若改成"不认识的串一律落固定本地句"** ⇒ 新收旧会**丢掉对面那句话**，且 `NetSelfTest:596` / `NetBattleTest:652` 会红。
   - ⛔ **若 +`Version`(→3)** ⇒ 新旧**互相连不上**（`:362` 硬拒）—— 比"印键名"更干净但**会切断跨版本联机**；`NetProtocol.cs:199-201` 那条"语义变了也 +1"的先例**支持**这么做。**两条路都自洽，我判不了该选哪条 ⇒ 请主对话拍**（但我倾向不 +，因为收侧已有兜底先例）。
2. **录像**：✅ **无需任何兼容动作** —— 7 处**一个字节都不进** `ReplayRecord`，`ReplayRecord.version`（`ReplayStore.cs:59`）= 2 也**不用动**。
3. **断言**：🔴 **必须同批改 2 条、同批核 1 条**：
   - `Editor/NetBattleTest.cs:994` **必改**（子串 → `Loc.T("Settings/Online/Wire/RoomGone")`，否则英文档红）；
   - `Editor/NetSelfTest.cs:596`（M⑳）**必改或必保**：它的"绿"绑在"收侧原样回显不认识的串"上 —— 若 P6d 保留兜底回显就**不用改**，若去掉就必须改。
   - `Editor/NetBattleTest.cs:652` 同 M⑳ 的条件。
   - ⚠️ **建议加一条新断言**：断"收侧印的是**词条值**、不是键名"（现在**零条**断言查过 `Wire/*`）。

---

## ⑤ 没查清的（逐条：卡在哪 / 还差什么）

1. **原版到底发的是什么** —— 🔴 **我在本地查不到**。我**只在** `d:/4/Unity/MyGame/Assets/`（`grep -rn "MsgBye|MsgReject|MsgAck|reason" --include=*.cs`）与 `d:/4/Unity/资料/`（`grep A1036|A1038|P6d|走线|reason`）搜过，**没有搜 `d:/2/tools/decomp_full/`**（简报的"原版是服务端发键、客户端取词"我**只有简报这一个来源**，本件**未独立复核**）。卡点 = 本件任务范围=协议兼容面，原版那条链在另一个盘。
2. **收侧取词的落点没裁定**（设计缺口，不是查不到）：`NetSession.Close(bool say, string reason)` 的 `reason` 是 **双重载荷** —— 既上 wire（`:567`）又进本机 `StatusText`（`:568 SetState(Off, reason ?? "未连接")`）。改法必须把这两个用途**拆成两个参数**；拆完之后"本机状态字走哪一份"没人定。
3. **`NetMatchmaking` 那 40 字提示行预算会不会被顶爆**：`NetMatchmaking.cs:226-233` 逐字算过「`what` = 「对面离开了：」+ 对方报的理由 = 14 字 ⇒ 14+24 = **38** ≤ 40（`Shell/SearchingMatchPopup.HintLineMaxChars = 40`）」。🔴 **这是按"收侧看到的是中文串"算的**：若**取词发生在拼句之后**，`what` = 6 + 29（键长）= 35 ⇒ 35+24 = **59 > 40** ⇒ **每次都 `LogWarning`**；若**取词发生在拼句之前**（先 `Loc.T` 再拼），= 6 + 8（「对面结束了这一局」）= 14 ⇒ 38 ≤ 40 ✅。**两种落点给出 38 / 59，取哪种 = 没裁定**，我只报两面。
4. **`MsgAck.reason`（`NetSession.cs:376`）与 `RuleCodes.Describe`（`NetBattle.cs:998`）这两条走线串要不要同批改** —— 简报没列，**我判不了**；后者还被 `RuleCodes.cs:117-123` 明确"第 ③ 步、本轮不做"卡着。
5. **`NetSession.cs:434`/`:442` 是"主机收到 reconnect 之后的拒绝"，这两条会不会也在**客机自己那条 `Send(NetKind.Reconnect)`（`:419`）之后**触发** —— 时序我读了代码但**没跑过**，不写成事实。

---

## ⑥ 顺手发现的

1. 🔴 **注释里的行号已漂**（同文件内自相矛盾）：`Net/NetBattle.cs:627` 写 `:749`（现读 **`:789`**）、`:628` 写 `:952`（现读 **`:992`**）；`NetProtocol.cs:210` 写 `NetSession` 的 `:428`/`:336`/`:362`（现读 **`:477`**/**`:375`**/**`:401`**）。
2. 🔴 **两处口径打架**：`Net/NetBattle.cs:604`「⛔ **本批【没有】把这几句接上 `Loc.T`**」+ `:615`「⛔ 已由主对话裁定接受」 **vs** `Core/Loc.cs:1648-1650`「⚠️ 这 7 条会经过网络…**接线时两端要同批更新**…P6d 单开」。**同一件事两个说法，P6d 收口时两条都得订正。**
3. 🔴 **`NetRuntime.cs:179` 的措辞在客机那条路上是错的** —— `Reset()` 被 `SettingsWindow.cs:2878`（**客机**【检查连接】）调用，而它给主机捎的是「**主机**重开了」。原样保留会把一句语义颠倒的话送到对面（同 `NetBattle.cs:784-788` 那条 R7 中性化纪律）。
4. 🟢 **`Settings/Online/Wire/` 这 7 条键【已经建好且已在表】**（`Core/Loc.cs:1651-1657`，ZH 列 = 调用点原话逐字，EN 列自拟）—— `Loc.cs:1650` 逐字「本件**只建键、不接线**」⇒ **P6d 只剩"接线"这一半**，不用建键。
5. ⚠️ **两条键 ZH 同值**：`Battle/Tips/NotYourTurn`（`Loc.cs:964`「不是你的回合」）与 `Settings/Online/Wire/NotYourTurn`（`Loc.cs:1656` 同值）—— 若哪天做"中文反查键"会**撞车**（`Loc.cs:1644-1647` 已声明两族**语义不同、并存**）。
6. 📌 **`Loc` 无反向查表**（已核：`grep TryGetKey|KeyOfText|Reverse|ByText|KeyFor Core/Loc.cs` = 0 命中；`KeyOf` 是**语言枚举 → 键**，不是文案 → 键）⇒ "把旧中文串翻成键"这条路**实现不了**，只能靠 `T()` 的缺键回键名来白赚兼容（见 ④·1）。
7. 📌 **Ack 族有一条现成的本机键**：`Loc.cs:1427 Settings/Online/St/VersionMismatch` 与 `NetSession.cs:363` 的字面量 ZH 逐字相同 —— 那条是**本机显示**族，可作为"接线时 `St/*` 与 `Wire/*` 别混"的活样例。

---

## 📄 摘要（≤300 字 · 原文照录）

P6d 查证完毕（只读，零改动）。7 处属实但**不是 3 条消息**：`MsgBye.reason` 占 5 处（`NetSession:434/442/461/567` + `NetRuntime:179`，后者经 `Close→Bye`）、`MsgReject.reason` 1 处（`NetBattle:992`，现读行号，非简报的 789 之一）——`NetBattle:789` 实际是**第 7 处** `Close→Bye`。方向：**5 处只有主机→客机，仅 `:461` 客机→主机**。协议里**没有**现成的"码→文案"口，但有一条**形状完全对口**的先例：`NetPendingBattle.PlayMode` + `PlayModeNames.Parse`（**线上发枚举名、收侧 Parse、认不出出声退兜底**，且**换过编码、没 +版本号、旧档照样读**）。版本握手**有**（`Version=2`，不符硬拒）；**不 +版本号**时旧客户端收键标识**会把键名当正文印出来**（7 条 ≤32 字，不被钳），新客户端收旧中文串则由 `Loc.T` 缺键回键名⇒**中文原样正确**。**录像安全**：7 串不在 `MsgAction` 上，半条都不进 `ReplayRecord`。测试面：只有 3 条断言真读这类串，`NetBattleTest:994` 英文档**必红**，`NetSelfTest:596`/`NetBattleTest:652` 红不红取决于"收侧还回不回显不认识的串"。⚠️ 另发现 3 条**同样走线**的中文串（`NetSession:376` Ack · `NetBattle:998 RuleCodes.Describe` · `BattleDriver:6158`），简报未列。卡点两条：原版发什么（未搜 `d:/2`）、取词落点在拼句前还是后（40 字提示行预算给出 38 或 59）。
