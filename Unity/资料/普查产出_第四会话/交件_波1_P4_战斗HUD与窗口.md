# 交件 · 双语③ 波 1 · **P4「战斗 HUD / 各窗口」**

> 白名单（10 件，全部只读+改过 3 处）：`Unity/MyGame/Assets/CardPresentation/Battle/{BattleDriver,EndPanel,ChatPopupPanel,SettingsPanel,CardDisplayWindow,MultiCardDisplay,TutorialOverlay}.cs`
> · `…/Shell/{CampaignTab,ReferralPopupWindow,RankedRewardEventWindow}.cs`。判据 = `资料/普查产出_第四会话/施工单_双语③逐处换key.md` + 附件 `_附_BattleDeck.md` / `_附_Shell.md` + **现读**。

## ① 结论

- 🔴 **只改了 3 处，另外 42 处一字未动**。**根因不在本笔**：波 0 补的是施工单 §⑤ 那 16 条「**代码引用过、表里没有**」的键；
  **两份附件 §二那张「① 新键」表一条都没进表**。P4 现读共 **45 处**玩家可见文案，其中**只有 3 处的键在 `Loc` 表里**
  （`Battle/Tips/Continue` · `Settings/Graphics/AutoZoom` · `Battle/Mulligan/WaitEnemy`）⇒ 这 3 处已接；
  其余 42 处的键**一条都不在表里** ⇒ 按本笔红线（⛔ 不许碰 `Core/Loc.cs`、⛔ 缺键自己不加）**只能报告、不能落**
  （硬接 = `Loc.T` 印**键名**，那是本工程点名的真缺陷）。**P1/P2/P3 大概率同一症状**（它们的键也没进波 0）。
- ✅ **秒级类型检查**：`TMPDIR=/tmp/wf_p4 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 错 / 编辑器 0 错**（改动后跑的）。
- ✅ **三个键都现读核过在表**（⛔ 不是猜）：`Loc.cs:663 / :675 / :951`。**英文档取到的值与改前逐字相同** ⇒ 英文档零变化。
- ⛔ 没跑 Unity · 没写断言 · 没碰 `Core/Loc.cs` · 没碰任何 `Editor/*.cs` · 没碰文档正本 · 没碰 git。

## ② 逐处改动表（4 个 hunk / 3 处字串）

| # | 文件:行号（改后） | 原字串 | 改成 | key | 键出处 |
|---|---|---|---|---|---|
| 1 | `Battle/TutorialOverlay.cs:429`（+ 新 const `:196-201`） | `withContinue ? "Continue" : ""` | `withContinue ? Loc.T(ContinueTerm) : ""` | `Battle/Tips/Continue` | `Loc.cs:675`（表里早有）；EN 逐字 = 原版 `ContinueText` 那颗 TMP；ZH「继续」= `zh_CN.csv:84` |
| 2 | `Battle/SettingsPanel.cs:610-625`（原 :612） | `Label.Create(transform, AutoZoomLabelEn, …)` + `SetCapHeight(U(AzFontPx*0.72f))` | `Loc.T(AutoZoomTermKey)` + `ApplyLangFont(_azLabel, azText, AzFontPx)` | `Settings/Graphics/AutoZoom` | 键由本文件 `:254 AutoZoomTermKey` 记着（原版 `battlearena1/MonoBehaviour_5032.json` 的 `mTerm`）；值 = 波 0 建的 `Loc.cs:951` |
| 3 | `Battle/SettingsPanel.cs:793-802`（`RefreshTexts()` 末尾） | （新增） | `_azLabel` 也按 `Loc.T(AutoZoomTermKey)` 重设 + `ApplyLangFont` | 同上 | 同上。⚠️ 不加这块 ⇒ 窗内换语言后那一行要等下次开窗才对（本面板**自己就有**语言下拉） |
| 4 | `Battle/BattleDriver.cs:3983`（原 :3979） | `SetDoneText("等待对手…")` | `SetDoneText(Loc.T("Battle/Mulligan/WaitEnemy") + "…")` | `Battle/Mulligan/WaitEnemy` | `Loc.cs:663`（原版 TMP 原文 `Waiting for enemy`）|

- **#2 比「直接换」多做了一步**（字号）：原版 fs42 那条 `×0.72` 只对**拉丁**成立，中文照它算**小 28%**；
  `ApplyLangFont` → `Label.SetScriptHeight` → `Loc.HasCjk`（本工程那条**唯一**判据）。EN 档取值与原来**逐字相同**（`0.72/108`）。
- **#4 附件【没列】**（附件只列了 `:3981` 的 `SetHint`）—— 属同文件同族、且用的是**已在表**的键，按「本批 = 把本文件玩家可见中文接进 `Loc`」办了。
  ⛔ 若调度台认为越界：**删这一行**即可（`git diff` 里它是独立 hunk，回退零代价）。

## ③ 没改的 42 处及其理由

**A. 键不在表里 + 原版【也没有】这条词条（38 处，全部要新增「自拟」键 → 键表见 ⑤）**
现读逐处：`BattleDriver.cs` `:280 · :720 · :2687 · :3970 · :3981 · :3999 · :4086 · :4087 · :4107 · :4108 · :5232(×2) · :5646(×2) · :5652 · :5654 · :5656 · :5659 · :5660 · :5663 · :5666 · :5668 · :5670(×2) · :5672 · :5678 · :6133 · :6540` ·
`EndPanel.cs:342(×3) · :345 · :346` · `SettingsPanel.cs:479("Settings") · :569("AI Difficulty")` · `ChatPopupPanel.cs:76(×6)` ·
`CardDisplayWindow.cs:201` · `MultiCardDisplay.cs:152` · `CampaignTab.cs:742("Points: ")`。

**三条另有 in-code 硬判据、⛔ 别按附件硬来：**
1. 🔴 **战斗日志整簇 11 句（`BattleDriver.cs:5646-5678`）** —— 同文件 `:5575-5639`（`G5`）**已经把 `Battle/Cemetery/*` 那 19 条活键
   + `CemeteryManager.GetActionText` 里 21 个 `DAT_` 逐条解出来了**，结论有两条：① 原版记「谁做了什么**动作**」，我们记**动作的后果**
   ⇒ **12 条模板只有 6 条能与原版键对上**；② 结论行**字面写着**「它们没有原版 `mTerm`，接的时候要和这次重构一起决定键名，**⛔ 别先自造一批键**」。
   ⇒ 与附件 B §五「键名先自拟」**打架**，按代码那份（有反编译证据）**不动**（也正好满足本笔「别只改一半」）。
2. **`CardDisplayWindow.cs:196-201`** —— 同段 in-code 判据写明「原版子树里没有这个节点、`Battle/` 93 条字面量里也没有键 ⇒
   铁律 11 例外① ⇒ **保留中文、不改**」（附件把它列成 ①，与代码打架，我按代码）。
3. **`SettingsPanel.cs:475-478`** —— 否定证据齐全（`BattleSettingsPanel` 子树**没有一个标题节点**）⇒「原版确实没有这一格」；
   `:562-568` 的 AI 难度那行也是**我们加的**。⚠️ 且这两处今天是**英文**（不是中文）⇒ 严格说不在本批题面（要不要让英文也跟语言走 = **调度台裁**）。

**B. 不算玩家可见文案（4 处）**：`Shell/ReferralPopupWindow.cs:227-243` 的 12 个 `Txt*` 与 `Shell/RankedRewardEventWindow.cs:195-199` 的 5 个 `Txt*`
= **原版 prefab 的俄语/西语/英文样例串**（§③ 那一类），运行期被真值/空串覆盖（`:567` / `:448-455`）。
⚠️ Referral 那一族的注释里**留着原版 mTerm**（`MenuShop/referral/{mainTitle,inputLabel,title,description,referrerLabel,counter}`）⇒ 真要接得先建那一族键（值在远端）。
⛔ 这两个文件我**逐行扫过：玩家可见文案 0 处**（中文只在 `Debug.Log` 与节点名里）—— 与附件 Shell §一「其余 63 个文件 0 处」一致。

## ④ 需要哪个断言宿主 + 哪一条要改

宿主 = **`d:/4/Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs`**（本批唯一相关宿主，**不在我白名单**）。
- 🔴 **必须改的一条（我这一改它就红）**：`:8652-8655` 现断 `sp.AutoZoomLabelText == SettingsPanel.AutoZoomLabelEn && == "Auto zoom"`
  ⇒ 中文档那一行现在是「自动缩放」，**必红**。改法照施工单 §⑧·A：`Loc.RestoreForTest(Chinese/English)` 两档各断
  `== Loc.T(SettingsPanel.AutoZoomTermKey)`，**加** `zh != en && 都非空` 与 `!Loc.HasCjk(en)`（灭自证 C1），跑完放回原语档。
- 🆕 **建议补一条（今天的断言接不住 #1）**：`TutorialOverlay.TipContinueText` 现成的断言只断「`== 空串`」（`:15084`，61 条全是 `withContinue=false`）
  ⇒ **把它改回写死 `"Continue"` 也照样全绿**（弱断言）。要加一条「两语档下各自等于 `Loc.T("Battle/Tips/Continue")`」，
  可先 `SetTip(true, "x", withContinue:true, …)` 造出那一态再读。
- 🆕 另加「本批的键都在表里」逐键 `Loc.HasEntry`（P4 现成只有 **3** 条）。

## ⑤ 没做到 / 拿不准的 —— **38 条待新增键（键名 + ZH 我备好了；EN 全部自拟）**

**核过的否定证据**：`d:/2/tools/il2cpp_out/stringliteral.json`（26,507 条）里 559 条词条形状串，我按前缀逐族核过
（`Battle/`93 · `MainMenu/`86 · `Settings/`10 · `Tips/`8 · `MenuShop/`13 …）：**`Battle/{Log,Hint,Chat,MultiCard,CardWindow,ChooseCard/Offensive,ChooseCard/Defensive}` 整族不存在**，
`Battle/BattleEnd/` 只有 `{DamageDone,Tactics,TroopsDead}`（**是另一族**，见 `EndPanel.cs:321-341`），`Battle/Settings/` 只有 `Exit`
⇒ 这 38 条**没有一条能照原版键**，只能自拟。⚠️ **缺键是我照本笔红线的处置，不是「查不到」**。

- **`BattleDriver.cs` 提示行 13 条**：`:280`→`Battle/Hint/MulliganDone`（换牌完成，开打）· `:720`→`Battle/Hint/Reconnected`（已重连并追平）·
  `:2687`→`Battle/Hint/DeckLoadFailed`（卡组存档读不出来（{0}）—— 本局自动凑了一副）· `:3970`→`Battle/Hint/MulliganPick` ·
  `:3981`→`Battle/Hint/MulliganSent` · `:3999`→`Battle/Hint/ReplacedCount`（换掉了 {0} 张）· `:4086/:4087`→`Battle/ChooseCard/Offensive`（选择进攻卡 / ＋「（先手）—— 选完点「继续」」）·
  `:4107/:4108`→`Battle/ChooseCard/Defensive` · `:5232`→`Battle/Hint/ChooseCard` / `Battle/Hint/ChooseOption` · `:6133`→`Battle/Hint/NoRestartOnline`
- **`BattleDriver.cs` 其它 4 条**：`:6540`→`Battle/MultiCard/Title`（你的牌库）· `:5646`→`Battle/Log/SideMe`+`SideFoe`（我方/敌方）·
  `:5678`→`Battle/Log/TurnPrefix`（回合 {0}　{1}，**含全角空格**）· `:5670`→`Battle/Log/EffectFallback`（效果）
- **战斗日志 11 句模板**（`:5652 …`）：建议键 `Battle/Log/{Play,Deploy,Attack,Melee,Ranged,Heal,Damage,Death,Return,Ability,Trigger,Bare}` —— ⚠️ **但见 ③·A·1：代码里那份判据说「别先自造一批键」⇒ 这 11 条我建议【先不加】**，等重构那一轮定。
- **`EndPanel.cs` 5 条**：`:342`→`Battle/BattleEnd/{ForfeitMe,ForfeitFoe}`（我方/对方投降）· `Rounds`（{0} 回合　　）·
  `:345`→`Battle/BattleEnd/RoundsMinFoeHealth` · `:346`→`Battle/BattleEnd/RoundsOnly`
- **`ChatPopupPanel.cs` 6 条**：`Battle/Chat/{Greet,Threat,WellPlayed,Taunt,Sorry,Oops}` —— 文件头 `:23-32` 写明原版标签来自**远端 I2**、
  序列化兜底 6 个**全是 `Greetings`**、本工程**没有任何一处能印出真标签** ⇒ 只有自拟一条路。
- **`SettingsPanel.cs` 2 条**：`:479`→`Battle/Settings/Title`（原版**没有**这格，见 ③·A·3）· `:569`→`Battle/Settings/AiDifficulty`（我们加的）
- **单条**：`CardDisplayWindow.cs:201`→`Battle/CardWindow/TapToClose`（⚠️ 代码判过「保留中文」，见 ③·A·2）·
  `MultiCardDisplay.cs:152`→`Battle/MultiCard/TapToClose`（原版这格**零词条**，代码自己标了「已知缺口」）· `CampaignTab.cs:742`→`MainMenu/Campaign/Points`（`Points: {0}`）
- ⚠️ **另一条路（若调度台更想先要覆盖率）**：这 38 处也可以先按施工单 §⑧·E2 的写法落 `Loc.HasEntry(k) ? Loc.T(k) : <原串>` —— 今天零回归、键一进表就生效。
  **我没这么做**，因为：本笔写着「缺键 ⇒ 写进报告」，且 §⑧·B2 那条设计好的断言（「本批字面量不再出现在本批文件里」）要求最终**不留原串** ⇒ 兜底分支迟早要删，等于多造一轮清理。**要不要走这条，请调度台裁。**

## ⑥ 顺手发现的（⛔ 一处都没顺手改）

1. **附件 ① 表漏了 2 处 `SetHint`**：`BattleDriver.cs:4087`「选择进攻卡（先手）—— 选完点「继续」」与 `:4108`「选择防御卡（后手）—— 选完点「继续」」
   （附件只列了 `:4086`/`:4107` 的 `Open(...,"选择进攻卡")`）；同族还有 `:3979` 的 `SetDoneText`（**我已接**）。⇒ 附件 B 的「BattleDriver 27 处」实际 **≥ 29 处**。
2. 🔴 **`Battle/ChoosePanel.cs:127-135` doc 与代码不一致**：doc 写链是「本表 → **`Loc` 表** → 调用方兜底」，**代码里没有查 `Loc` 的那一跳**
   （只 `Terms.TryGetValue("Battle/ChooseCard/Instructions-"+uniqueId)` → 无后缀那条 → `fallback`）⇒ 「两处写同一条规则」那族。那个文件不在我白名单，**只报不改**。
3. **附件 B §五 的「疑似 / 没逐条对过」已过期**：`BattleDriver.cs:5575-5639`（`G5`）里已经逐条对过（19 条活键 + 21 个 `DAT_` 全解出来了）。
4. **6 颗聊天钮的真键没查到**：`stringliteral.json` 里有 `Chat_Messages/warcry-` 与 `Chat_Messages/warcry-default` 两条，
   但按 `Chat_Messages` 搜 `decomp_full/` 与 `assets_full/` **零命中** ⇒ 那两条**不是**这 6 颗的标签来源（与 `ChatPopupPanel` 文件头「来源在远端」一致）。**如实记「没查到」**。
5. **`Shell/` 三件里两件是 0 处**：`ReferralPopupWindow.cs` / `RankedRewardEventWindow.cs` 逐行扫过，玩家可见文案 **0**（见 ③·B）；
   只有 `CampaignTab.cs:742` 一处真 ①。⇒ 施工单 §⑦ P4 那格写的「38 处」现读对不上（白名单 10 件的真值 = **45 处**）。
6. ⚠️ `Battle/Mulligan/WaitEnemy` 现在**两处消费**（`Battle/WaitBanner.cs:280` + 我刚接的 `BattleDriver.cs:3983`）—— 同一条键两处用是**对的**（同义），
   但那颗钮上原版印的是不是这句**没查到**（原版那一态的字也来自远端 I2）⇒ 我按「同义复用」办，**如实标「没核」**。
