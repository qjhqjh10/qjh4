# B9_E3CMP · 战役脚本动作执行器（4 + 1 条方法 + 回合驱动器）

> 执行代理 `E3CMP` · 2026-10-27 · 工程 `d:/4/Unity/MyGame/Assets`
> 单件产出：**`CardPresentation/Battle/CampaignScriptRunner.cs`（新建，733 行）**
> `TMPDIR=/tmp/wf_E3CMP bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（跑了 3 次，最后一次在终稿之后）

---

## 一、结论

1. **原版这 5 条方法全部落在 `AiScripted` 一处**（不是 `BattleManager`）—— 它们和教程那 5 条是**同名重载**，
   区别只在读的指针：教程 `actionCounter @+0x10`、战役 **`actionCampaignCounter @+0x14`**
   （`d:/2/tools/il2cpp_out/dump.cs:24552-24553`）。我们的教程那一半早在
   `RuleEngine/Core/TutorialScript.cs`；**战役这一半之前完全不存在**（账里 5 行全是 `C`）。
2. **5 条方法已全部落地**，每条逐句对齐、出处写在代码里：`HasPendingActions` / `PlayNextScriptedAction` /
   `PlayScriptedTurn` / `GetCurrentWaitTime` / `ExecuteAction(…, ScriptedActionCampaignData)`。
   其中 `ExecuteAction` 的**战役那份在 `decomp_full` 里没有产物**（同名重载被教程那份覆盖）⇒
   判据 = **VA 反汇编 `0x1809407F0`–`0x180940DAF`** ＋ 🥈 ILSpy `AiScripted.cs:1915-2061`（两份逐句吻合）。
3. **顺手把「没有落点」的那台驱动器也建了**：`CampaignScriptTurnDriver`（原版
   `BattleManager.ExecuteCampaignScriptedTurn` 协程，账 `战斗逻辑驱动.tsv:305` 现在是「B / 无落点」）。
   ⚠️ 它的 `Begin/Pump/End` 三步口是**我们自己设计的形状**（原版是 `IEnumerator` + 两次 `yield`），已打 `🔶`。
4. 🔴 **数据源本地不存在 ⇒ 按验收口径 ③ 停手，⛔ 没有编任何战役脚本内容**。
   数据结构（`CampaignScriptAction` 等 4 个类）是**逐字段照 `dump.cs` 抄的**（不是我们发明的格式），
   但**没有一份真实数据**喂得进去 ⇒ 今天这套执行器**零可观测行为**。

---

## 二、证据（逐条 · `文件:行号`）

### 2·1 四条驱动方法（战役那半）

| 方法 | 🥇 反编译（本地有真体） | 🥈 ILSpy dev | 逐句语义 |
|---|---|---|---|
| `HasPendingActions(List<ScriptedActionCampaignData>)` | `decomp_full/AiScripted__HasPendingActions.c:6-11` | `AiScripted.cs:491-503` | 只有一句：`actionCampaignCounter(+0x14) < turnData.Count(+0x18)` |
| `PlayNextScriptedAction(BattleManager, List<…>)` | `decomp_full/AiScripted__PlayNextScriptedAction.c:11-21` | `AiScripted.cs:443-464` | **没有「到头」前闸**：直接 `list[指针]`（越界 ⇒ `get_Item` 抛）→ `ExecuteAction` → **指针 ++** → 返回 `指针 < Count` |
| `PlayScriptedTurn(BattleManager, List<…>)` | `decomp_full/AiScripted__PlayScriptedTurn.c:11-28` | `AiScripted.cs:262-289` | **有前闸**：`指针 ≥ Count ⇒ false`；否则同上 |
| `GetCurrentWaitTime(BattleManager, List<…>)` | `decomp_full/AiScripted__GetCurrentWaitTime.c:14-43` | `AiScripted.cs:3556-3572` | `指针 < Count` 且元素非空 ⇒ `GetSoundAsset(...).GetDuration()`（**取不到声音按 0**，`:30`）**＋ `waitTime @+0x28`**（`:37`）；其余 **0**（`:43`） |

🔑 **`GetCurrentWaitTime` 的形参 `battleManager` 原版根本不读**（ILSpy `:3556-3572` 全文没有它）；
它只是拿来取 `PlayerDataManager.singletonManager.bundlesManager`（反汇编 `0x18094095B`→`0x180940970` 的 `[singleton+0x58]`）。
我们把它收敛成 `ICampaignScriptHost.ResolveBundledSound`。

### 2·2 `ExecuteAction(BattleManager, ScriptedActionCampaignData)` —— 六支

判据：**VA 反汇编 `0x1809407F0`–`0x180940DAF`**（`工具/disasm_va.py` 现跑，函数以 `int3` 收尾于 `0x180940DB0`）
＋ ILSpy `AiScripted.cs:1915-2061`。逐支：

| 档 | 值 | 原版做什么 | 反汇编锚点 | ILSpy |
|---|---|---|---|---|
| `PlayerChat` | 50 | `voiceLines.DisplayChatBox(text, GetHero(true), sound.clip)` | `0x180940B6A` 起 | `:1984-1988` |
| `PlayerChatBig` | 55 | `raw = GetHero(true).rawCardData`；`radioSpeakerCard` 非空 ⇒ `allCardCollection.Find(x => x.uniqueId == 它)` 覆盖 `raw`；`sprite = raw.referenceCard?.fullHeroSpriteReference ?? (raw.fullHeroSpriteReference.RuntimeKeyIsValid() ? 它 : null)`；`DisplayBigChat(text, sprite)`（重载 B） | `0x180940BB0`–`0x180940D32` | `:1990-2041` |
| `AiChat` | 60 | text 非空 ⇒ `DisplayChatBox(text, GetHero(false), clip)`；**空** ⇒ `DisplayWarlordChatMessage(ChatMessage.Intro, isPlayer:false, ignoreUiState:true)` | `0x180940D37`–`0x180940DA3` | `:2034-2058` |
| `AiChatBig` | 65 | `msg = IsNullOrEmpty(text) ? ChatManager.GetChatMessage(Intro, false) : text`；`DisplayBigChat(msg, GetHero(false))`（重载 A） | `0x1809409BF`–`0x180940A3E` | `:1933-1946` |
| `RadioMessage` | 90 | `tip = new SmallTipData(text, smallTipParams, 鼠标点击位置, null)`；`card = allCardCollection.Find(uniqueId == radioSpeakerCard)`；`ShowRadioMessage(tip, card.cardSprite, sound.clip, card.cardType)` | `0x180940A3F`–`0x180940B4A` | `:1948-1974` |
| `LandWarlords` | 160 | `manager.LandWarlordsInCampaign()` | `0x180940B4F`–`0x180940B65` | `:1976-1979` |

（六个 `if` 的**行号现读**：`1933` / `1948` / `1976` / `1984` / `1990` / `2034`；
方法整体 = `AiScripted.cs:1915-2061`。）

**六支共用**：开头 `Log("Execute Campaign Scripted AI Action " + actionType)`（`0x1809408C8-0x180940907`，
`String.Concat` + `Enum.ToString`）→ `soundAsset = GetSoundAsset(...)`（`0x180940935-0x18094097A`）→
`text = GetLocalizedText()`（`0x18094097F-0x180940996`）；**末尾** `if (soundAsset != null) PlaySoundAsset(soundAsset)`
（`0x1809409FF-0x180940A0D`）。**其余所有档位什么都不做** —— ⛔ 别自己加行为。

🔴 **`RadioMessage` 取的是 `cardSprite`（卡面插图）、`PlayerChatBig` 取的是 `fullHeroSprite`（督军整身立绘）**
（反汇编 `0x180940B0C` 调 `RawCardScript.get_cardSprite` vs `0x180940D00` 那条走 `AssetReferenceTyped<Sprite>.Load`）
—— 这两格**在同一个函数里是不同的两个属性**，一开始我把它们都写成了 `fullHeroSprite`，读反汇编时自己发现并改回
（`ICampaignScriptHost.CardSprite` / `FullHeroSprite` 现在是两个口）。

### 2·3 数据结构（逐字段照 `dump.cs`，⛔ 不是我们发明的格式）

- `ScriptedActionCampaignData` `dump.cs:89410-89437`：`stageAction 0x10` · `soundAssetName 0x18` ·
  `textReference 0x20` · **`waitTime 0x28`** · `actionType 0x2C` · `smallTipParams 0x30` · `radioSpeakerCard 0x38`。
- `SmallTipData` `dump.cs:24731-24766`；构造体 🥇 `decomp_full/SmallTipData__.ctor.c`：
  `tipDuration` 初值 **6.0f（`0x40c00000`）**、随后被 `tipParams.tipDuration` 覆盖；`tipParams == null` ⇒ **抛**。
- `TutorialTipParams` `dump.cs:24712-24729`（**战役也用它**，名字里的 "Tutorial" 是原版类名）。
- `SoundAsset_OLD` `dump.cs:3220`；`GetDuration()` 🥇 `decomp_full/SoundAsset_OLD__GetDuration.c` = `clip != null ? clip.length : 0`。
- `GetSoundAsset` 🥇 `decomp_full/ScriptedActionCampaignData__GetSoundAsset.c`：名字空 ⇒ **null**；
  否则建对象，**`volume` 硬编码 `0x3f800000` = 1.0**（`:28`）。
- `GetBundledSound` 🥇 `decomp_full/ScriptedActionCampaignData__GetBundledSound.c` 语义 = **逐个已加载 AssetBundle 试
  `LoadAsset<AudioClip>(name)`，第一个非 null 的胜**；全都没有 ⇒ `clip = null`（对象照样建）。
- `GetLocalizedText` 🥇 `decomp_full/ScriptedActionCampaignData__GetLocalizedText.c` = `GetTranslation(前缀 + textReference)`。
  🔑 **前缀字面量实读 = `"Event_Description/"`**：反汇编 `0x180797A98` 的 `mov rcx,[rip+0x3b12541]` → 槽 `0x1842A9FE0`
  → `il2cpp_out/stringliteral.json` 里 `address = 0x42A9FE0` 那条。现核：`Resources/official_loc_13lang.json`
  里 `Event_Description/` 前缀共 **145** 条 ⇒ 这条路在本地接得上。

### 2·4 指针生命周期（两条容易写错的）

- `ResetActionCounter()` 🥇 `decomp_full/AiScripted__ResetActionCounter.c` = `*(longlong *)(this + 0x10) = 0`
  —— **一次写 8 字节**，把教程 `+0x10` 与战役 `+0x14` **一起**清零。
- `UpdateTurn(int)` 🥇 `decomp_full/AiScripted__UpdateTurn.c` = **只清 `+0x10`**（教程那个），
  **⛔ 战役指针不被回合推进清掉** —— 所以「换回合了战役指针还是旧值」是**原版行为**，不是 bug。

### 2·5 回合驱动器（顺手那座，账 `战斗逻辑驱动.tsv:305`）

- kickoff 🥈 `out_cpp2il_dev_ilspy/BattleManager.cs:35578-35592`；
  **真体** 🥇 `decomp_full/BattleManager._ExecuteCampaignScriptedTurn_d__598__MoveNext.c` 全文
  ＋ 🥈 同类 `BattleManager.cs:2703-2825`。
- 时序（逐句）：`coroutineCounter(+0x534)++` → Log → 列表 null/空 ⇒ LogWarning + **结束**（`:46-57`）
  → `duringScriptedActionsFlag(+0x268) = true`（`:63`）→ `ResetActionCounter()`（`:70`）
  → **循环**：Log("Resolve next action") + `yield 1.0s`（常量 `DAT_1834b2bb8` = `0x3f800000`）
  → **先** `GetCurrentWaitTime` **再** `PlayScriptedTurn`（`:75-87`，**这个次序是判据**）
  → `yield wait` → 循环 → 收尾 `flag=false` + `onComplete?.Invoke()`（`:122-136`）。
- 字段名：`aiScriptedmanager @+0x250` · `duringScriptedActionsFlag @+0x268` · `coroutineCounter @+0x534`
  （`dump.cs:30825/30829/30924`）。

---

## 三、没查清 / 缺数据（**数据源本地不存在 —— 按验收口径 ③ 停手**）

### 3·1 缺的是什么

战役脚本的四张表挂在 **`WarlordPathData`**（`dump.cs:89285-89289`）：
`preMulliganScriptedActions +0x38` · `postMulliganScriptedActions +0x40` · `victoryScriptedActions +0x48`（+ 属性 `:89297-89299`），
而 `WarlordPathData` 由 **`TimedCampaignObject : ScriptableObject`**（`dump.cs:2290`）持有
（`WarlordPath1/2/3` 三个槽 + `RaidBoss*` 一族，`:89170-89262`）。消费入口 =
`BattleManager.GetCampaignScriptedData(CampatinScriptedActionType)`（`dump.cs:32289`；枚举 `PreMulligan 0 / PostMulligan 10 / Victory 20`，`dump.cs:2289`）。

### 3·2 在哪找过（三条，全空）

| 找法 | 结果 |
|---|---|
| `d:/2/新解包资源/assets_full` 全树 grep `WarlordPath1\|preMulliganScriptedActions` | 7 命中，**全是教程**：`bundle_tutorialso_assets_all/MonoBehaviour/Warpforge_TutorialStage{1..6}.json` + `Scenary_Test.json`。教程 SO 那一格是 `TurnScriptedData`（`dump.cs:42064`），**不是**战役的四张表 |
| 同上 grep `TimedCampaign` | **1 命中** = `globalgamemanagers/MonoScript/MonoScript_4429.json`（**类注册**，不是实例） |
| `d:/2/关服前存档`（835 MB，含远端 Addressables 缓存 `Everguild_Warpforge_Unity_Cache`）二进制 grep `preMulliganScriptedActions` / `radioSpeakerCard` / `TimedCampaign` | **0 命中** |
| 我们自己的 `Unity/数据/游戏数据/*.json` grep `stageAction\|waitTime` | 0 命中（没有战役脚本表） |

⇒ **结论：`TimedCampaignObject` 一个实例都没有**（它是服务端 LiveOps 下发的资产）。
与 `资料/完全复刻/判据/战役开局_判据与落点.md` §一 #7 的结论一致（那份也写着 `WarlordPathData` 本地没有）。

### 3·3 还差什么（接线前必须先有的三件，⛔ 一件都不能自己编）

1. **数据**：一份 `TimedCampaignObject` → `WarlordPathData` → 四张 `List<ScriptedActionCampaignData>`。**本地没有**。
2. **战役局入口**：`MatchType.Campaign = 130`（`dump.cs:27382`）的**构造点**在已反编译的 ~29% 里 **0 命中**
   （`战役开局_判据与落点.md` 开头那段）。⇒ 就算有数据，也**没有「开一局战役」的那一跳**把驱动器接上去。
3. **宿主接线**：`ICampaignScriptHost` 的 15 个成员要落到 `BattleDriver` / `UnitChatPanel` /
   `BattleBigChat` / `CardArt` / `VoiceLines` 上。本波**故意没接** —— 理由同上（没数据 = 接了也零行为，
   而 `BattleDriver.cs` 是本波多代理共用的热文件，只该做「必须」的改动）。

### 3·4 仍然没查清的（如实记，没当成结论）

- **`GetLocalizedText` 缺键时原版返回什么** —— 原版那张 I2 表是远端下发的，`GetTranslation` 的缺键行为
  本机拿不到判据 ⇒ 我们选了「返回空串」（见代码里的 `🔶`，理由：另一条路 `Loc.T` 会把键名印到屏幕上）。
  ⚠️ 这条**会间接改变 `AiChatBig` 的分支**（文案空 ⇒ 走 `ChatManager.GetChatMessage(Intro,false)`）——
  等将来有真数据/真表时**要复核一次**。
- **`0x1805E3920`**（`RadioMessage` / `PlayerChatBig` 两处 `Find` 前的那一跳）：`工具/resolve_va.py` 解不出
  （「没有方法从这个地址开始」），ILSpy 也只留 `NoteDecompilerIssue("Method not found @1805E3920")`。
  按上下文（`(…)→ .allCardCollection`）判它 = **`PlayerDataManager.get_singletonManager`**，
  但这是**旁证不是判据**，如实标注。
- **`RawCardScript.cardSprite` 我们自己怎么取**：`CardArt.Portrait(card.Id)` 是**我们**的等价物
  （原版是一个序列化 `Sprite`）；离线核对不了「同一张卡两边是不是同一张图」。

---

## 四、改动清单

| 文件 | 动作 | 内容 |
|---|---|---|
| `Unity/MyGame/Assets/CardPresentation/Battle/CampaignScriptRunner.cs` | **新建**（733 行） | ① `CampaignTipParams` / `CampaignSmallTipData` / `CampaignSoundAsset` / `CampaignScriptAction`（DTO，逐字段照 `dump.cs`）② `ICampaignScriptHost`（15 个成员，逐条注明原版方法）③ `NullCampaignScriptHost`（出声不动作，`🔶`）④ `CampaignScriptRunner`（4 条方法 + `ExecuteAction` + `ResetActionCounter`）⑤ `CampaignScriptTurnDriver` + `CampaignScriptStep`（`🔶` 三步口） |
| 其余文件 | **一行未动** | 特别是 `BattleDriver.cs`：查完判定「没数据、没入口 ⇒ 接线零收益」，⛔ 没碰 |

- `.meta`：**没有手工建**（验收里允许）—— Unity 下次导入时自己生成。
- 行尾：**纯 LF / 无裸 CR**（现读 `CRLF 0 · LF 733 · bareCR 0`）；无既有文件被改写 ⇒ 不存在「翻行尾」风险。
- 新增成员全部在**新文件**里 ⇒ 不会造成任何账的 `文件:行号` 漂移。

### 4·1 账该往哪改（⛔ 主表不是我的写手，交调度台合并）

`资料/完全复刻/账/战斗逻辑驱动.tsv`：

| 行 | 方法 | 现状态 | 建议 |
|---|---|---|---|
| 94 | `ExecuteAction(BattleManager, ScriptedActionCampaignData)` | `C` | → **B**（六支已落 + 出声兜底；⚠️ **零数据 ⇒ 无法验**，G3 那条观测不到）· 落点 = `CampaignScriptRunner.cs` 的 `ExecuteAction` |
| 100 | `GetCurrentWaitTime(…, List<ScriptedActionCampaignData>)` | `C` | → **B** · 落点 = 同名方法 |
| 105 | `HasPendingActions(List<ScriptedActionCampaignData>)` | `C` | → **B** · 落点 = 同名方法 |
| 109 | `PlayNextScriptedAction(…, List<ScriptedActionCampaignData>)` | `C` | → **B** · 落点 = 同名方法 |
| 111 | `PlayScriptedTurn(…, List<ScriptedActionCampaignData>)` | `C` | → **B** · 落点 = 同名方法 |
| 305 | `BattleManager.ExecuteCampaignScriptedTurn` | `B`（无落点） | → **B** · **落点 = `CampaignScriptTurnDriver`**（`Begin/Pump/End`），备注里的「建议另开一条实现类待办」**已兑现** |

**为什么只能是 B 不能 A**：G3 观测量要求「拿真数据跑一遍对数值」，而**数据本地不存在**；
在没有数据的反例夹具之前，断言只能覆盖「指针 + 返回值」这种合成用例 —— 本波没建（`BattleContext` 无关，
是纯类，将来可以补一个纯 C# 夹具）。

---

## 五、顺手发现（⛔ 都没动手，交调度台分流）

1. 🔴 **`decomp_full/AiScripted__ExecuteAction.c` 落盘的是【教程】那份**（读 `+0x48 scriptedActionData` /
   字符串 `+0x10`），**战役那份没有产物**。账 `:94` 的判据栏引的是 ILSpy dev（正确），
   但**任何人**照着「同名 `.c` 存在」去读战役口径都会读成教程的体。
   ⇒ 建议把这些**同名重载**在文档里统一标成「⚠️ 只落下了 X 那份」（已知坑：账 `:112` 记过同一件事）。
2. 🆕 **第 N 例「文件名与内容不符」**：`decomp_full/AiScripted.__c__DisplayClass33_0__.ctor.c` 的内容其实是
   `<>f__AnonymousType31..ctor`（体里只有一句 `NetworkingPeer__OnMessage`）—— 与文件名无关。
   （同族已知 6 例，这是又一例。）
3. 🆕 **`工具/resolve_va.py` 对「属性 getter 的共享体」解不出**：`0x1805E3920` 报
   「没有方法从这个地址开始」，而 ILSpy 在那两处打了 `NoteDecompilerIssue("Method not found @1805E3920")`
   ⇒ **两份工具在同一处同时失效**，只能靠上下文反推。可写进 `资料/全量反编译_入口与用法.md`。
4. 🆕 **`0x1804F0EC0` 被 resolve 成 `UnityEngine.Rendering.MousePositionDebug.GetMouseClickPosition`** ——
   它是 ICF 折叠的共享体，**名字只能当旁证**（我按「进 `SmallTipData.tipPosition`、是个 `Vector2`」定的语义）。
   与 `资料/…«ICF 折叠»` 那条已知现象同族，但这是**「名字对不上语义」**（不是「名字缺」）的新一例。
5. 🆕 **`ResetActionCounter` 一次写 8 字节 ⇒ 同时清两个计数器**（教程 `+0x10` + 战役 `+0x14`）。
   我们 `RuleEngine/Core/TutorialScript.cs` 的 `ResetActionCounter()` 只清自己那一个 ——
   **单跑教程时无差**（战役指针本来没人用），但将来两边接上后，**只调教程那个不会清战役指针**。
   ⚠️ `RuleEngine/**` 归 `E1RNG`，本波没碰，只报。
6. 🆕 **`BattleManager.coroutineCounter @+0x534` 与那串日志是【教程/战役共用】的**：
   两座协程都 `++` 同一个计数器，且**日志字符串逐字相同**（`"Start ExecuteScriptedTurn. coroutineCounter "`）。
   ⇒ 将来打日志排查时，**没法从这行区分是教程回合还是战役回合**（原版自己就这样，⛔ 别「修好」它）。
7. 🆕 **`duringScriptedActionsFlag @+0x268` 是 `ObscuredBool`（8 字节）** —— 反汇编里那条
   `mov [lVar3+0x268], ...` + `mov [lVar3+0x270], ...` 是**同一个字段的两半**，
   别把它读成「两个 bool 字段」（我第一次读时就差点）。

---

# 第二批（2026-10-27 · 调度台追派）

## 一、账补丁（已交）

- **文件**：`d:/4/Unity/资料/完全复刻/账/_parts/B9_E3CMP.tsv`（6 行 + 表头，`状态` 列**原值未动**：5 个 `C` + 1 个 `B`）。
- **表头**：从主表 `战斗逻辑驱动.tsv` 第 1 行**逐字复制**（11 列：`域 原版类 方法 判据出处 判据要点 状态 我们的落点 差异 等级 轮次 备注`）。
- **按方法名找的 6 行**（⛔ 不是按行号）：`ExecuteAction(BattleManager, ScriptedActionCampaignData)` · `GetCurrentWaitTime(…, List<ScriptedActionCampaignData>)` · `HasPendingActions(List<ScriptedActionCampaignData>)` · `PlayNextScriptedAction(…, List<ScriptedActionCampaignData>)` · `PlayScriptedTurn(…, List<ScriptedActionCampaignData>)` · `ExecuteCampaignScriptedTurn(…)`。
- **改动的列**：`差异`（重写成「原版 X vs 我们 Y」）· `备注`（前缀 `[E3CMP 2026-10-27]`，并写清「数据源本地不存在（三条取证全空）⇒ 今天零可观测差异」这句时间限定）· `轮次` = `B9_E3CMP`。
- **`我们的落点` 列按你的指示【没动】** ⇒ ⚠️ 但它现在是**过期事实**（那 5 行还写着「查不到（战役未做）」，1 行写着「（无落点…未实现）」）。落点的**真值**我写进了 `备注`（`落点已建 = CampaignScriptRunner.cs 的 X`）。要不要改那一列、改了要不要一起跑 `check_ledger_points` —— **你裁**。
- **冲突检查**：`python -I -X utf8 d:/4/Unity/工具/check_patch_conflicts.py` ⇒ `✅ 没有跨补丁重复键（2 份补丁）`。

## 二、804 `MinionPlayedIntoField` —— 改了什么（`DeploySequence.cs`，本波我独占）

### 2·1 已改：**④ 末段等待 = `AttackStepTime`**（真差异，之前引擎早结算 0.1 s）

判据 🥇 `decomp_full/CardScript._MinionPlayedIntoField_d__314__MoveNext.c:186-205`（**state 3**）：
`LandOnGround()` 之后 `yield WaitForSeconds(CardScript.get_AttackStepTime())`（**:199 就是那一句**）。
而 `_ResolvePlayCardFromHand_d__447__MoveNext` 是**等整条协程跑完**才 `ResolveCardPlayed`
⇒ 原版「真出牌」= `0.3 + 0.1 + 0.05 + 0.2 + **0.1**` = **0.75 s**；我们原来是 **0.65**（漏了尾拍）。

改法（3 处，都在 `DeploySequence.cs`）：
1. 新增 `public const float LandTailTime = CardFeel.AttackStepTime;`（**0.1**；⛔ 不写第二份字面量 —— 值来自 `CardFeel.cs:523`，那文件归 `E2UDW`，**只读引用**）；
2. `TotalTime = HandoffTime + HoldTime + LandTime + LandTailTime` ⇒ **0.75**；
3. `Play(...)` 与 `PlayQuad(...)` 两处（含 `handoff = default` 的退化支）在落地补间之后各 `seq.AppendInterval(LandTailTime)` ⇒ `onDone` 落在 0.75 s。

**连带影响（都是「自动跟上」，不需要改别人的文件）**：
- `Editor/BattleScene.cs:22477` 那条断言比的是 `Dur(Play(...)) == TotalTime`（**两侧同源**）⇒ 数字自动一致，**不会红**；
  ⚠️ 但它的**文案**还写着「四段共 …」（`:6824`/`:6837`/`:22903`）⇒ 文案过期，**`Editor/BattleScene.cs` 不归我**，如实记。
- `CardInteraction.cs:1436` 的出牌阻塞窗口读 `DeploySequence.DurationFor(card)` ⇒ 自动变成 0.75（**这正是原版语义**：整条飞行 + 尾拍都算「在动作里」）。
- 其余提到 0.65 的地方**全是注释**（`BattleDriver.cs` 的 `:13462/:13468/:13527/:13562/:18151/:20942/:23788/:25138/:25749`）—— **没有任何一处把它当常量用**（现核：闸宽走 `DurationFor`）。文案归 `E4BDR`，我只报。

### 2·2 ① `orderInPlay`：**不改**（我们的等价物在引擎侧，视图侧不存第二份）

- 原版判据：`d__314:72` = `*(int *)(card + 0x44) = *(int *)(param_1 + 0x34)`；
  `EntityScript.orderInPlay`（**`+0x44`**，`dump.cs` 的 `private int <orderInPlay>k__BackingField; // 0x44`）
  = **调用方传进来的 `unitInPlayCounter`**（不是自增）。
- 我们的等价物 = 引擎 `UnitState.PlayOrder`（`BattleContext.TakePlayOrder()` 在部署那一刻发号；
  排序消费点 `RuleCore.StableSortByPlayOrder` / `EffectResolver`）。
- ⇒ `DeploySequence.cs` 文件头（现读第 52 行）**已经定过这条口径**：「视图侧不另存一份……⛔ 别为它加
  `CardView.inPlayOrder` 那种没人读的字段」。施工单 ① 想做的那件事**在我们的形状里不存在**，
  所以本条的正确处置是「核过、不落代码」——**不是漏做**。

### 2·3 ② `summonSickness` / `OnDisplaySummonMessage` / `summonAnimDelayed`：**待接线**（本波不落）

判据（`d__314` 三处，逐句）：
| 位置 | 原版做什么 | 字段（现读 `dump.cs`） |
|---|---|---|
| `:69-71`（state 0） | `keepStatus == false` ⇒ 置位 | `EntityScript.summonSickness @+0x58`（`private bool <summonSickness>k__BackingField`） |
| `:196-197`（state 3 前闸） | `shouldTriggerSummon == false` **或** `HasCurrentTrait(5)` **或** `HasCurrentTrait(0x4B0)` ⇒ **整段跳过** | `EntityScript.HasCurrentTrait(int)` |
| `:207-211` | 触发委托 | `CardScript.OnDisplaySummonMessage @+0x138`（`Action<CardScript>`） |
| `:212-247` | 委托之后：若 `rawCardData.summonAnimDelayed` 的 AssetReference 有效且 `Load()` 出对象 ⇒ `Instantiate(fx, card.transform.position, 静态旋转)` | `RawCardScript.summonAnimDelayed @+0xB0`（`AssetReferenceTyped<GameObject>`） |
| 收尾 | 无论上面走不走到，都 `yield WaitForSeconds(AttackStepTime)` | —— |

⚠️ 施工单把 ② 写成「`summonAnimDelayed` 与 `m_OnDisplaySummonMessage` 两个出口」——**字段名对**，但
**`m_OnDisplaySummonMessage` 的真名是 `OnDisplaySummonMessage`**（`CardScript @+0x138`），
且它**排在 summon FX 之前**、两者**同属 `shouldTriggerSummon` 那一支**（不是两个独立开关）。

**精确改法（给 `E4BDR` / 视图侧）**：这三个出口都在**视图层**，`DeploySequence` 只排补间、不知道卡上有哪些关。
建议在 `DeploySequence.Play/PlayQuad` 的 `Handoff` 结构里加一个可空 `Action OnLandedTail`（**默认 null ⇒ 行为不变**），
由驱动层在 0.65 s 那一刻执行「召唤消息 + FX」；`summonSickness` 是**状态位**不是演出，该在 `DoPlay` 之后按 `keepStatus` 置。

### 2·4 ③ `createdByText`：**不归本条**（与账 866 同一件）

`BasicCardUI.createdByText @+0xD8`（`public TextMeshProUGUI createdByText`）—— 它是「由谁生成」那一行，
归 `SetStateInHand`（账 866）那条线，本波**没碰**。

## 三、782 `HeroLandIntoField` —— 现状复核 + **`showStats` 判据已解出**

### 3·1 结论：**BattleDriver 侧已经落过了**，我不重复造

- 实现：`CardPresentation/Battle/BattleDriver.cs:33938-33990` 的 `HeroLandInto(CardView v, bool showStats, float waitMult)`；
  唯一调用点 `BattleDriver.cs:17697`（教程局第二拍：先 `SetActive(true)` 再调，与 `_TutorialStartSequence_d__592__MoveNext.c:80` 逐字一致）。
- 它用的两件 `CardFeel` 成员**都已存在**：`CardFeel.HeroLandBadgeFadeTime = 0.3f`（`CardFeel.cs:188`）·
  `CardFeel.ShakeCameraForLand(...)`（`CardFeel.cs:1566`）。
- 已有 5 条 `🔶 自己设计` 裁定（缩放不写 / alpha 不写 / showStats TODO / LandOnGround 只震镜 / 不调 ActivateTraits）——
  **其中 ① ② 两条我复核后认为成立**（原版 `DOScale` 的起止值同级 ⇒ 空操作；`DOFade(1)` 在 alpha 已 1 时也是空操作）
  ⇒ ⛔ **不推翻调度台 2026-10-26 的裁定**，`DeploySequence` 侧**不加第二份**（否则就是「两处写同一条规则」）。

### 3·2 `showStats` 那一半：**判据已逐句解出**（原「TODO / 判据未逐句解」可以销掉）

判据 🥇 `decomp_full/CardScript._HeroLandIntoField_d__318__MoveNext.c:87-338`（state 0：`:87` 是 `showStats == 0 ⇒ 直接等` 那一闸，`:97-338` 是 `showStats != 0` 那一支）。

**它到底做什么**：对 **4 个** `CardTextCountersController`（字段在 `BasicCardUI`，现读 `dump.cs`）——
`healthCountersController @+0xA0` · `meleeAttackCountersController @+0xB0` · `rangeAttackCountersController @+0xB8` ·
`armourCountersController @+0xC0` —— **逐个容器**（`CardTextCountersController.containers[] @+0x30`；
每项 = `container(GameObject) @+0x10` + `activationRules(int[]) @+0x18`）算一个 bool 然后
**`GameObject.SetActive(container, bool)`**（四个控制器分别在 `:98` armour / `:156` health / `:208` melee / `:264` range；每个的容器循环末尾各一处 `SetActive`：`:205` / `:261` / `:322` / `:425`）。

**判定口径**（与 `CardView.cs:514-533` 已经解过的那一套**逐字相同**，⛔ 别当新东西）：
`ActivationRules { Normal=0 · SmallScreen=5 · SimplifiedView=10 · NoSimplifiedView=15 }`
（`dump.cs` 的 `CardTextCountersController.CardTextCounterContainer.ActivationRules`）；
- 规则 `0` ⇒ **亮**；
- 规则 `5` ⇒ 看静态 **`GameStaticData.smallScreenUI`**（`+0x11c`）→ 真才亮（我们的等价物 = `CardPresentation.SmallScreenUI.Enabled`）；
- 规则 `10` ⇒ 看 `simplifiedView` → d__318 这一处**恒 false** ⇒ **灭**；
- 规则 `15` ⇒ `!simplifiedView` ⇒ **亮**；
- 一条都不命中 ⇒ **灭**。
（`Toggle(value, simplifiedView)` 也有同一套规则（`CardTextCountersController__Toggle.c`），d__318 这一处直接 `SetActive`、等价于 `simplifiedView = false`。）

**还差什么（这是唯一真正没解的部分）**：**四个控制器的 `containers[]` / `activationRules` 是 prefab 序列化值** ——
要接就得先把那四组容器从卡预制体里读出来（`CardView.cs:518` 已经读过**费用格**那一份：
`bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/MonoBehaviour_6320885271709195200.json`，
`Cost Container = [0,15]` / `Cost Container Big = [5,10]`）⇒ **同一份 prefab 里另外四个控制器的 `containers[]` 应该也在**，
本轮**没读**（不在我的白名单：那是 `Core/CardView.cs` 的地盘）。

**精确改法（给 `E2UDW` 的 `CardFeel`/`CardView` 侧）**：
1. 从上面那份 prefab 读 4 个控制器的 `containers[]`（容器 GO + `activationRules`），落一张表；
2. 判定用一个**共用**的 `ActivationRulesAllows(int[] rules, bool simplifiedView)`（⛔ 别在 `CardView` 与
   `CardFeel` 各写一份 —— 现在 `CardView.cs:514-533` 那份是费用格专用）；
3. `HeroLandInto` 收到 `showStats == true` 时对 4 个控制器逐容器 `SetActive`，`false` 时**不动**（原版 `false` 是「直接等」）。

## 四、顺手发现（第二批）

1. 🔴 **账会漂 4 行**：`账/战斗界面_HUD.tsv` 的 4 行（`:746`/`:769`/`:786`/`:788`）都指向
   `Board/DeploySequence.cs:224`（= `Play(CardView …, Handoff, …)` 那个重载）—— 本次改动把该方法挪到 **`:249`**。
   ⇒ 同步点时跑 `check_ledger_points --all-A --fix` 重定位（**只在没有 `.cs` 写手时跑**）。
2. **施工单 ② 的字段名有一处不准**：`m_OnDisplaySummonMessage` 真名 = `CardScript.OnDisplaySummonMessage @+0x138`；
   而 `summonAnimDelayed` 在 **`RawCardScript @+0xB0`**（不是 `CardScript`），两者**同属 `shouldTriggerSummon` 一支**。
3. **`EntityScript +0x58` 是 `summonSickness`、不是「召唤延迟动画」** —— 施工单把它描述成「summonAnimDelayed 开关」会误导：
   `keepStatus=false ⇒ summonSickness=true` 是**状态位**，不是在排演出。
4. **`0.65` 的过期文案散在 `BattleDriver.cs` 9 处**（上表）—— 全是注释、**无逻辑**，归 `E4BDR`。
5. **`Editor/BattleScene.cs` 三条断言的文案**写着「四段共 / 四段之和」（`:6824`/`:6837`/`:22903`），
   数值自动跟上、只文案过期 —— 归 `Editor/BattleScene.cs` 的写手。

---

# 第三批 · 教程线（2026-10-27）

> 本批只碰 **`RuleEngine/Core/TutorialScript.cs`**（调度台指派独占）。`RuleCore.cs` / `EffectResolver.cs` /
> `Aura.cs` / `ActionPump.cs` / `Enchantment.cs` / `CardDef.cs` / `BattleEvent.cs` / `BattleDriver.cs` /
> `Shell/**` **一行未动**。

## 一、`ResetActionCounter` —— 改成原版的「一次写 8 字节、两格一起清」

- **原版判据** 🥇 `decomp_full/AiScripted__ResetActionCounter.c`：`*(long long *)(param_1 + 0x10) = 0`
  —— **一次写 8 字节** ⇒ `actionCounter(+0x10)` 与 **`actionCampaignCounter(+0x14)`** 同时归零。
- **改法**（`TutorialScript.cs`，`:711` 一带）：
  ```csharp
  public void ResetActionCounter() { ActionCounter = 0; ResetCampaignActionCounter?.Invoke(); }
  public System.Action ResetCampaignActionCounter;   // 战役那一格（+0x14）的归零口
  ```
  为什么用 delegate 而不是引用 `CampaignScriptRunner`：原版是**同一个实例上的两个 int**，我们分在两层
  （教程在 `RuleEngine`、战役在 `CardPresentation`）⇒ 用这一个口把它们重新绑成「一次清两格」，
  **不引入 `RuleEngine → CardPresentation` 的反向依赖**（本批也不碰 `CampaignScriptRunner.cs`）。
- **今天的行为**：`ResetCampaignActionCounter` **没有订阅方**（没有战役局）⇒ 与「只清一格」**逐字等价**
  ⇒ 可观测差异 = 0（`ruleprobe play check` 现跑 **rc=0**、指纹仍是 `b671acdb…`）。
- ⚠️ 接线时的**一行**：拿到 `TutorialScript` 与 `CampaignScriptRunner` 的那一处写
  `tut.ResetCampaignActionCounter = () => runner.ActionCampaignCounter = 0;`（`ActionCampaignCounter` 是 public 属性）。

## 二、`ExecuteAction` 的 `ActiveAbility(40)` 桩 → **照原版接通**

- **判据**：🥈 ILSpy `AiScripted.cs:2422-2610`（acting 解析）+ `:2644` 起的 `IL_0f85`（target 解析）。
  🔴 **落盘那份 `.c` 的 `switch` 里没有 `case 0x28`**（只有 `0x1e/0x1f/0x23/0x24`）⇒ **不能拿 `.c` 当这一支的证据**，
  这一档在 `.c` 里走的是 if/else 链（`scriptedActionData.actionType > ChangeToMelee` 之后的第一支）。
- **原版逐句**：`acting.SetupActiveAbility()` → **`data[0].targetUnitType != None(0)` 才解析目标**
  （同一套六档：督军 / 玩家部队 30·31·32 / 敌方部队 / 两侧手牌，用 `GetChosenCard`）→ 入队 `playActiveAbility`。
- **我们的实现**（新增 `TutorialScript.RunActiveAbility`，`:1072`）：
  `FindBoardUnit(acting)` → 有目标就 `FindBoardUnit(target)` → `RuleCore.UseAbility(ctx, side, slot, targetSlot)`
  → 填 `ScriptedActionDone`（`Seat/Slot/TargetP/TargetSlot`）。
  两处**如实记的边界**：
  1. `SetupActiveAbility()` 那一跳我们引擎**没有对应物**（它只备演出/选目标形态；引擎那半边由
     `UseAbility` 里的 `RecomputeAttackTypeAfterActiveAbility` 覆盖）；
  2. **手牌目标不自己造**：`UseAbility` 第 4 个实参是**棋盘格号**、手牌没有格号 ⇒ 认不出就 `Fail` 出声，
     ⛔ 不猜一个格子塞进去。
- **可观测差异 = 0**：6 关数据里 `ActiveAbility` **0 条**（`tutorial_stages.json` 逐条 dump 过）⇒ 本条把
  「只会出声的桩」换成「照原版接通的链」，**今天跑不出任何不同**（`ruleprobe play check` rc=0，指纹未变）。

## 三、教程线其余在册条目 —— 逐条处置（**都没做，理由在下面**）

| 账行 | 方法 | 状态 | 处置 | 为什么 |
|---|---|---|---|---|
| `:89` | `CheckIfWaitingToClickCard` | C | **不做** | 谓词**无消费者**（原版唯一调用方在缺失体里）；台账备注写着「⛔ 现在别并存两个推进口（先裁谁是唯一推进口）」⇒ **要调度台先裁**，不是代码问题 |
| `:98` | `GetChosenCard` | C | **不做** | 台账写着「【需裁定】要复刻『认不出降级取 `list[0]`』⇒ 改两处 return」—— 这是**口径裁定**（我们现在的红线是「不退回 `list[0]`，出声」）⇒ 我不自己推翻红线 |
| `:96` | `GetActingCard` | B | **不做** | 卡在 `:98` 那条裁定上（备注原话：「仍 B：共享落点里含 `GetChosenCard` 的『认不出退回 `list[0]`』那一半」） |
| `:1366` | `TurnScriptedData.UpdateName` | B | **不做** | **判据缺**：`.c` 的拼串段失真（`num6 = default` 之类），两次复核仍读不全；且它只影响可读名、不进对局 |
| `:799` | `IsTheSameTurnPlayed` | B | **不做** | 已是等价实现，B 的原因是「落点只能指到字段/属性，拿不到方法级声明」（结构性），不是缺口 |

⇒ 教程线**在 `TutorialScript.cs` 里能做的只有上面两条**（`ResetActionCounter` · `ActiveAbility`）；
其余 5 条要么**等裁定**、要么**判据缺**、要么**不是缺口**。

## 四、账补丁（`账/_parts/B9_E3CMP2.tsv`，8 行）

`check_patch_conflicts.py` ⇒ `✅ 没有跨补丁重复键（4 份补丁）`。`状态` 列**一律沿用主表原值**。

| 块 | 行 | 改了什么 |
|---|---|---|
| A（6 行） | 战役那 6 条 | **按调度台裁定补 `我们的落点` 真值**（全部指到 `CardPresentation/Battle/CampaignScriptRunner.cs` 的对应方法 / `CampaignScriptTurnDriver`），其余列沿用主表现值 |
| B（1 行） | `ResetActionCounter()` | `落点` 补上「两格一起清」的口；`差异` 重写；`轮次` = `B9_E3CMP2`；备注写明「原差异栏『我们没有战役计数器 ⇒ 等价』已过期」+ 零可观测差异的时间限定 |
| C（1 行） | `ExecuteAction(BattleManager, ScriptedAction)` | `落点` 补 `RunActiveAbility`；`差异` 改成「ActiveAbility 已接通，仍缺 ShowManaAura/ResolveCard（数据 0 条）」；`轮次` = `B9_E3CMP2` |

## 五、验证

- `TMPDIR=/tmp/wf_E3CMP bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（改动后两次）。
- `bash d:/4/Unity/工具/ruleprobe.sh play check` ⇒ **rc=0**，哨兵「基线文件的内容与钉住的一致（`b671acdb…`）」通过
  ⇒ 本批两处改动（8 字节清零语义 · ActiveAbility 接通）**对 play 指纹零影响**（都是 0 数据 / 未接线的档）。
- ⚠️ **本批没有跑 Unity 自检**（按纪律，批处理不归我）。

## 六、🔴 待办（⛔ 这两个文件不归我，只记账）

**T1 · 文案过期（`CardPresentation/Battle/BattleDriver.cs`，归 `E4BDR`）**：9 处注释仍写「0.65 s」的出牌飞行，
现为 **0.75 s**（`DeploySequence.TotalTime` 已按原版 state 3 的尾拍修正）：
`:13462` · `:13468` · `:13527` · `:13562` · `:18151` · `:20942` · `:23788` · `:25138` · `:25749`。
**无逻辑影响**（闸宽走 `DeploySequence.DurationFor`，已自动跟上）。

**T2 · 断言文案过期（`CardPresentation/Editor/BattleScene.cs`）**：三条断言的消息里写「四段共 / 四段之和」，
现为**五段 / 0.75 s**（数值两侧同源、断言仍绿，只是话术过期）：
`:6824` · `:6837` · `:22903`。

**T3 · 账行号漂移（各批合并后统一跑一次）**：
- `账/战斗界面_HUD.tsv` `:746/:769/:786/:788` 指 `Board/DeploySequence.cs:224` ⇒ 现 **`:249`**（第二批改动）。
- `RuleEngine/Core/TutorialScript.cs` 本批 **净 +40 行**（`ResetActionCounter` +6 · `ActiveAbility` case −4 ·
  `RunActiveAbility` +38；`git diff --numstat` 现读 `47/7`、文件 1803 → 1843 行）⇒ 指向该文件 `≥ :711` 的账行全部下移；现读关键行：
  `ResetActionCounter :715` · `ExecuteAction :882` · `ExecuteCore :907` · `RunActiveAbility :1072` ·
  `FindBoardUnit :1658`。⇒ 同步点跑 `check_ledger_points --all-A --fix`（只在没有 `.cs` 写手时）。

---

# 第四批 · 战役线（2026-10-27）

> 本批只碰 **`CardPresentation/Battle/CampaignScriptRunner.cs`** 与 **`CardPresentation/Shell/CampaignData.cs`**
> （`CampaignTab.cs` / `CampaignRewardWindow.cs` **一行未动** —— 现读**没有任何未完成条目**落在它们上，见 §三）。

## 〇、任务线在哪（现读，别照「战役线」找）

`资料/任务线/` 现读 **14 个目录，没有「战役线」**；战役的未完成条目全在 **`界面线/任务总表.md`**：
2026-10-27 那批新账（3 条「商店/战役/活动基础设施」）+ `X2·#8 战役通关` + 「`CampaignData` 可重复领状态机自相矛盾」那条审计。

## 一、做了的两条（都在 `Shell/CampaignData.cs`）

### 1·1 `Premium Campaign` 13 个阵营的定义表（界面线 §一「15 个 SO 只抄了 UM 一套」）

- **判据（逐份 SO 实读）**：`d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/Premium Campaign <X>.json`
  —— 每份的 `id`（`premium_campaign_xx`）· `cardArmy`（10…130）· `itemIcon` · `localizationKey`（全是 `MenuShop/PremiumCampaign`）。
  本批**亲自复核了 4 份**：`UM(10)` / `GOF(20)` / `ASH(30)` / `SW(130)` / `GSC(90)`（`id` 与 `cardArmy` 逐字对上）。
- **🔴 就地订正任务表那个「15」**：是 **14 份** —— 13 个阵营的 `PremiumItem` + **1 份 `Premium Campaign Store`**
  （`eventId PremiumCampaignStore`，是 **LiveOps 商店定义、不是 `PremiumItem`**，归 `ShopData`）。
  判据 → `判据/T4_532与PremiumCampaign_判据.md` §「摘要」第 2 条（那里已经订正过一次；**任务表还没跟上**）。
- **落码**（`CampaignData.cs` 类尾）：`struct PremiumItem { Army, Id, SoName }` + `PremiumTable`（13 条）+
  `PremiumAll`（自检读口）+ `TryPremium(string army, out PremiumItem)` + `PremiumIcon(string army)`。
  **图标不另存一份**：那 13 张就是 `ArmyIcons` 那族的 `40k_DeckSelection_icon_Faction*`（逐条比对：GOF→`…FactionOrks`、
  GSC→`…Genestealers`、SW→`…SpaceWolves`）⇒ 走 `ArmyIcons.IconName`（**判据只留一处**）。
- ⚠️ **消费点不在我这**：`Shell/PurchasePremiumWindow.cs` 还没读这张表 ⇒ **待接线**（改法见 §四·1）。

### 1·2 战役进度**落盘**（`X2·#8 战役通关` 的「先补存档」那一半）

- **原来**：`_base` / `_prem` / `Selected` **纯内存**（静态构造里 `ResetForTest()`）⇒ 退出即丢，
  `Ach_16`（完成战役）只在「这一趟领完」的那一刻成立。
- **现在**：落 `SaveKeys.ActiveCampaign`（那个键 `SaveKeys.cs:68`/`:254` 早就登记着、**全仓零使用** —— 现读 `grep` 只命中声明）。
  payload = `SaveBlob { ver, selected, baseClaimed[], premClaimed[] }`。
- 三条口径：
  1. 🔴 **按节点 id 存、不按下标** —— 节点表将来插一格不会把旧档错位（认不出的 id 直接跳过）；
  2. 🔴 **读档只在「会写盘的那一趟」**（`SaveCenter.StoreOn`）⇒ 批处理自检 / 探针仍是**干净起手态**；
  3. 写点 = `Claim` 成功（两档各一次）+ `Select`（换阵营）。
- `ResetForTest()` **仍然只清内存、不碰盘**（自检语义不变）。

## 二、顺手接上的一行（批次 3 欠的那条）

`CampaignScriptTurnDriver` 新增一个可选构造：
```csharp
public CampaignScriptTurnDriver(RuleEngine.TutorialScript tutorial)
{
    if (tutorial != null) tutorial.ResetCampaignActionCounter = () => Runner.ActionCampaignCounter = 0;
}
```
+ 默认构造（`tutorial == null` ⇒ 不挂，行为与从前逐字等价）。
判据仍是 `decomp_full/AiScripted__ResetActionCounter.c` 的「一次写 8 字节、两格一起清」。
⚠️ 今天**还没有构造点**（没有战役局）⇒ 仍是零可观测差异；但接线这件**从此只差一个实参**。

## 三、**没做**的（逐条写明为什么 + 在哪）

| 条目（界面线 §一） | 为什么没做 | 精确改法（给对应写手） |
|---|---|---|
| `CampaignBadge` 缺 `army` 字段 | 在 **`Shell/BadgeNotifications.cs:75`**（`public sealed class CampaignBadge : BadgeNotification { }`）—— **不归我** | 加 `public int army;` + `CampaignBadge(CardArmy army = 0)`；`Register` 处按 `CampaignData.Selected` 的 `CardArmy` 传；取图走 `CampaignData.PremiumIcon` / `ArmyIcons.IconName(army)` |
| `ShopData._shopItems` 字典丢顺序 | 在 **`Shell/ShopData.cs`** —— 不归我 | 改成 `List<…>` 保序（原版 = `items` 按序在前、`offerLists` 按 `TargetId` 展开在后） |
| `Premium Campaign Store` 那 13 条商店项（price 30 / amount 1999 / sku `premium_campaign`） | 在 **`Shell/ShopData.cs`** —— 不归我 | 照 `判据/PremiumCampaign_15SO_补齐清单.tsv` 的「Store」那一行落 13 条 `items[]`（⚠️ 表里记着 `SW` 那条 `targetId` 被复制粘贴成 `premium_campaign_ec`、对象引用才是 SW —— 照**对象引用**落，并把这条 bug 注在注释里） |
| `CampaignData` 可重复领状态机自相矛盾 | 🔴 **要拍板**：`StateOf` 对 `IsRepeatable` 且基础档已领返回 `Repeatable(40)` ⇒ 界面画「可领」，而 `Claim` 的基础档分支 `if (_base[i]) return false` ⇒ 点下去领不到。**原版对 repeatable 节点允不允许重复领 / 重复领发不发奖励，本地只有 `IsRepeatable` 字段值、没有方法体证据**（审计原文：「属判据在远端（服务端）/ 待拍板，⛔ 别凭界面表现猜」） | 若拍「允许重复领」⇒ `Claim` 的 `if (_base[i])` 那条对 `Repeatable` 放行（并决定奖励发不发）；若拍「不允许」⇒ `Claimable` 对 `Repeatable` 返 false、界面改画「已领」 |
| 档案窗「当前战役」的 `ArmyName` 用本地真值 | 落点 **`MainMenuScene.cs:7028`** —— 不归我（主对话已裁决：一格填真值、另两格留空） | 见 `调度台日志` §10·9；`CampaignData.Selected` + `ArmyIcons` 已能给出真值 |
| `CampaignTab.cs` / `CampaignRewardWindow.cs` | **现读：0 命中** —— `界面线/任务总表.md` 里 `CampaignTab.cs` / `CampaignRewardWindow.cs` 各 0 次，连 `CampaignTab` 这个词都没有 ⇒ 这两个文件本批**没有可做的在册条目** | —— |

## 四、待接线（我这边没有构造点/没有消费方的地方）

1. **`PurchasePremiumWindow` 消费 `CampaignData.PremiumTable`**：按当前 `CampaignData.Selected` 取那一行，
   图标 `PremiumIcon(Selected)`、标题键 = `MenuShop/PremiumCampaign`（原版 `localizationKey`，13 份**全是这一个键**）。
2. **战役局构造点**：`new CampaignScriptTurnDriver(tutorialScript)` —— 传了那个实参，「两格一起清」才算真接上。

## 五、账补丁（`账/_parts/B9_E3CMP3.tsv`，6 行）

`check_patch_conflicts.py` ⇒ `✅ 没有跨补丁重复键（6 份补丁）`。`状态` 一律沿用主表（`D/D/A/A/A/A`）。

| 行 | 改了什么 |
|---|---|
| `CampaignHandler.SaveSelectedArmy` / `SetAndSaveArmy`（D） | `我们的落点` 原写「（仅内存）」**已过期** ⇒ 改为「CampaignData.Select（本地落盘 `SaveKeys.ActiveCampaign`，不是原版那个服务端存盘）」；**状态仍 D**（原版走 PlayFab） |
| `CampaignHandler.SetSelectedArmy`（A） | 备注补「`Select` 现在还会落盘」 |
| `CompleteCampaign.OnInitialize` / `OnTrigger` / `b__3_0`（A） | 备注补「进度已落盘 ⇒ 跨会话不再归零；`Ach_16` 的『只记一次』仍靠 `SetAchievementValueAtLeast`」 |

## 六、验证 & 漂移

- `TMPDIR=/tmp/wf_E3CMP bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（改完两次）。
- **没跑 `ruleprobe play check`**：本批只改 `Shell/**`，**未触及引擎**（按纪律「改了引擎行为才跑」）。
- ⚠️ **`CampaignData.cs` 行号漂移**（账里有 10+ 行按行号指它）—— 现读新锚点：
  `At :123` · `IsUnlocked :166` · `StateOf :178` · `Claim :199` · `AllNodesClaimed :234` ·
  `NotifyCampaignCompletionIfAllClaimed :248` · `HasContent :278` · `Background :288` ·
  `Selected :310` · `Select :311` · `RewardsOf :441` · 新增 `TryPremium :583` / `LoadFromSave :627` / `SaveToSave :641`。
- ⚠️ 另有 `其他_基础设施.tsv`（`GetPlayerCampaignHero` / `ProcessCampaignDataObject` / `ShowEarlyCampaignMode`）与
  `日常_奖励.tsv` / `泛型与启动链_补读.tsv` / `个人资料_成就_排行.tsv` 若干行按行号指 `CampaignData.cs` ⇒ 一并靠 `--fix`。
