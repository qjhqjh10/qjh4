# 交件 · 波 1b · P3b（社交 + 弹窗 + 商店：把 P3 剩下的簇全部落地）

> 白名单 7 文件（`Shell/{LiveOpsEventWindow,PracticeModePopup,DeckInfoPopup,DeckSelectionPopup,ShopWindow,ShopData,CollectionData}.cs`）。
> 秒级类型检查 `TMPDIR=/tmp/wf_p3b bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（改后跑两次，两次都干净）。
> `git diff --numstat`（本批三文件）= `CollectionData 20/5 · DeckInfoPopup 17/5 · DeckSelectionPopup 9/4 · LiveOpsEventWindow 37/14 · PracticeModePopup 20/8`；
> 七个文件行尾现核 **全部纯 LF**（`CRLF=0`）⇒ 没翻行尾。**只动这 5 个文件**（`ShopWindow.cs`/`ShopData.cs` 本轮**一个字没改**，
> 它们那 10/4 与 5/1 是**上一轮 P3 的**未提交改动）。

## ① 结论

**19 个调用点 / 19 条不同的键**（= 波 0b `P3` 那 18 条里的 **16 条** + P1 批的卡组串 **3 条**），一律**裸 `Loc.T(键)`**（⛔ 无 `HasEntry` 兜底、⛔ 无 `? :` 原串）。
其中三簇 = **联机匹配取消族 4 处**（②A #5–#8）· **模式名与三句整句 4 处**（#1–#4）· **卡组串三句 3 处**（#9–#11）（合计 **11**），
另 **8 处**（②B）= P3 清单 §③·A/·B/·D 与 §⑥·4 里同属本批、波 0b 已为它们建键的落点。
⛔ **只跳过 `MenuDeck/Share/{ChatUnavailable,PlatformShare}` 两处** —— 简报 ⑤ 明令本件不动（`A1040` 在查，见 ⑦·1）。

## ② 逐处表（行号 = **改后现读**）

**A · 三簇（本轮主体）**

| # | 文件:行号 | 原字串 | key | 依据 |
|---|---|---|---|---|
| 1 | `LiveOpsEventWindow.cs:247` | `遭遇战（Skirmish · 12 张）` / `经典（Classic · 30 张）` | `MenuDeck/GameMode/{Skirmish,Classic}` | `Loc.cs:1225-1226`；波 0b §② |
| 2 | `LiveOpsEventWindow.cs:286-291` | `这副预组是「…」的，不能用在…里 —— 换一副。` | `MenuDeck/Error/WrongGameMode`（`{1}`=模式名 · `{0}`=Tag） | `Loc.cs:1229-1230`；波 0b §③ 拆两条的其中一条 |
| 3 | `LiveOpsEventWindow.cs:297` | `还没有可用的卡组 —— 先点 \`Create deck\` 建一副…的。` | `MenuDeck/Error/NoDeckForMode`（`{0}`=模式名） | `Loc.cs:1233-1234` |
| 4 | `LiveOpsEventWindow.cs:305-308` | `「…」（卡组名）是「…」的卡组，不能用在…里 —— …` | `MenuDeck/Error/WrongGameModeDeck`（`{2}`=模式名 · `{1}`=Tag · `{0}`=卡组名） | `Loc.cs:1231-1232` |
| 5 | `LiveOpsEventWindow.cs:1015` | `已经取消这一局的联机匹配 …\n想再打一次：…` | `Settings/Online/MatchCancelled` | `Loc.cs:1222-1223` |
| 6 | `LiveOpsEventWindow.cs:1021` | `取消不了这一局：` + why | `Settings/Online/MatchCancelFailed`（`{0}`=why） | `Loc.cs:1224` |
| 7 | `PracticeModePopup.cs:1683` | 同 #5 | `Settings/Online/MatchCancelled` | 同上 |
| 8 | `PracticeModePopup.cs:1688` | 同 #6 | `Settings/Online/MatchCancelFailed` | 同上 |
| 9 | `CollectionData.cs:240` | `先粘贴卡组串` | `MenuDeck/Error/ImportEmpty` | `Loc.cs:1105`；**与 P1 `Deck/DeckRuntime.cs:3950` 同一键** |
| 10 | `CollectionData.cs:242` | `这不是一条合法的卡组串` | `MenuDeck/Error/ImportBadString` | `Loc.cs:1106`；同上（`DeckRuntime.cs:3951`） |
| 11 | `CollectionData.cs:251` | `卡组串读出来了，但**没写进存档**：` + 原因 | `MenuDeck/Error/ImportNotPersisted`（`{0}`=原因） | `Loc.cs:1110`；**对齐** `DeckRuntime.cs:3982`（见 ④） |

**B · 同批其余 6 处（波 0b 已建键、P3 清单点过名 ⇒ 一并落地，⛔ 不留空键）**

| # | 文件:行号 | 原字串 | key | 依据 |
|---|---|---|---|---|
| 12 | `LiveOpsEventWindow.cs:514` | `这套卡组还没有战将 —— 去卡组编辑里选一个再来。` | `MenuDeck/HUD/NoWarlordText` | 简报 ③·1 末句；`Loc.cs:1246-1247` |
| 13 | `LiveOpsEventWindow.cs:944` | `这套卡组还没有选战将，开不了局。` | `MenuDeck/Error/CantStartNoWarlord` | **简报 ③·1 裁定**（见 ③） |
| 14 | `PracticeModePopup.cs:1622` | 同 #13 | `MenuDeck/Error/CantStartNoWarlord` | 同上 |
| 15 | `PracticeModePopup.cs:1297` | `（未选阵营）`（`SelectedArmyName()` 兜底，**会画上屏**） | `MenuDeck/HUD/NoArmySelected` | P3  §③·D 首条；`Loc.cs:1249` |
| 16 | `DeckInfoPopup.cs:1178` | `This deck can't be imported.`（**写死英文**） | `MenuDeck/CantImportDeck` | P3 §⑥·4；`Loc.cs:1250`（键名 = 原版 `0x42CEBF0`） |
| 17 | `DeckInfoPopup.cs:1277` | `这套卡组里有隐藏卡，开不了练习赛。` | `MenuDeck/Error/HiddenCards` | P3 §③·A·6；`Loc.cs:1235-1236` |
| 18 | `DeckSelectionPopup.cs:695` | `预组卡组的数据读不到（…）⇒ …`（两句拼接） | `MenuDeck/Error/PrebuiltMissing` | P3 §③·A·9；`Loc.cs:1241-1242` |
| 19 | `DeckSelectionPopup.cs:697` | `这一页一副可用的都没有（…）` | `MenuDeck/Error/NoUsablePrebuilt` | P3 §③·A·9；`Loc.cs:1243-1244` |

**中文档零变化** = #1–#10、#12、#15–#19（ZH 列与改前写死串**逐字相同**，逐条核过）；
**有意变化 2 处** = #11（见 ④）· #13/#14（见 ③）。

## ③ 整句那两处改回了什么（简报 ③·1）

`LiveOpsEventWindow.cs:944` 与 `PracticeModePopup.cs:1622` —— P3 复用了**短键** `MenuDeck/Error/NoWarlord`
（ZH「还没有选战将」），弹窗正文从「这套卡组还没有选战将，**开不了局。**」缩成 5 个字。
⇒ 现按 `A1034` 裁定改指**整句键** `MenuDeck/Error/CantStartNoWarlord`（`Loc.cs:1251-1252`，ZH 与改前原串**逐字相同**）。
🔴 **短键 `MenuDeck/Error/NoWarlord` 仍留给那 4 处「未选战将」栏位**（`DeckInfoPopup.cs:836` · `PracticeModePopup.cs:565/1468/1485`，上一轮已改，本轮未动）。
同族的 `LiveOpsEventWindow.cs:514`（`NoWarlordText`，整句）按简报 ③·1 末句一并处理 —— 这条**不是**用短键，是它自己那条整句键。

## ④ 卡组串三句对齐结果（简报 ③·2）

三句现走 `MenuDeck/Error/{ImportEmpty,ImportBadString,ImportNotPersisted}`，**与 P1 同一批键**（P1 现读：`DeckRuntime.cs:3950/3951/3982`）。
- #9/#10：ZH 与改前**逐字相同** ⇒ 中文档零变化。
- 🔴 #11（`CollectionData.cs:251`）**有意对齐到 `DeckRuntime` 那一版**：加「导入失败：」前缀、分隔符 `：`→`——`、
  尾加「（重启就没了）」⇒ 中文档下这句**比改前长**（这正是裁定的目的：两处逐字一致、**不是**保持原样）。
  用 `.Replace("{0}", SaveFailReason())`（⛔ 非 `string.Format`；先例 `Battle/HUD/CreatedBy`，见 `Loc.cs:1107`）。
- ⚠️ **顺手改了同一行的日志**：`Debug.LogWarning(… + why + "（重启就没了）")` 里那个尾巴现在**是重复的**
  （`why` 里已经有）⇒ 去掉重复尾巴，免得一条日志念两遍。**它只是开发者日志（②，不上屏）**，全仓 `grep 重启就没了` 无断言依赖（现核）。

## ⑤ `Skirmish` / `Classic` 的现读判定（简报 ④）

🔴 **`LiveOpsEventWindow.cs:946`**（`StartMatch` 里）`string modeStr = DeckGameMode == 13 ? "Skirmish" : "Classic";`
= **网络协议串、⛔ 不翻**（现读坐实）：它喂 `NetMatchmaking.TryStart(pd, modeStr, …)`，
落 `NetMatchmaking._myMode`，在 `NetMatchmaking.cs:533-536` 与对面的 `_foeMode` **逐字比对**
（不一致就 `LogError` 拒绝开局），字段声明在 `NetProtocol.cs:61/69`（注释就写着 `"Classic" / "Skirmish"`）⇒ **改一个字就配对不上**。
✅ **该翻的模式名在别处** = `DeckGameModeName`（`LiveOpsEventWindow.cs:243-248`）与内嵌 Tag —— 本轮已走 `MenuDeck/GameMode/*`（②表 #1/#2/#4）。
📌 另：`PracticeModePopup` 那边过网用的是 `PlayModeNames.Name(PlayMode)`（`"OfflinePractice"` 等），**没有** `Skirmish/Classic` 字面量（现核 grep 0 命中）。

## ⑥ 仍缺的键（只报告，⛔ 没加）

**本批 0 条**（19 条键改前逐条 `grep Core/Loc.cs` 确认在表）。本批**新引入的缺口 = 0**。⚠️ 但**波 0b 为 P3 建的 `MenuDeck/Share/{ChatUnavailable,PlatformShare}` 两条今天仍无消费点** ⇒ 是 `A1040` 结案前的**挂起键**（不是空键该删）。

## ⑦ 没做到 / 拿不准

1. **分享那两处未动**（简报 ⑤ 明令）：`DeckInfoPopup.cs:1382-1383`（`ShareDeck()` 弹窗正文）与 `Deck/DeckRuntime.ShareDeckString()` ⇒ 那两条键仍未用。**等 `A1040` 结论。**
2. **`DeckInfoPopup.cs:1277` 的 `hiddenWhy` 未翻**（P3 §③·D 判 ②、波 0b 同意未建键）⇒ **英文档下这条弹窗是「英文正文 + 中文诊断」的混合体**。如实记，不是静默；要治得先建键 + 裁它算不算上屏。
3. **`DeckInfoPopup.cs:1178` 的阵营名括注 `"（" + wl.Name + "）"` 没走词条** ⇒ 英文档下会是「英文正文 + 全角括号」。原版那条是远端 I2 键（`DAT_1842cebf0`）**带格式化实参**，我们取不到它的模板 ⇒ 这一处**没查到**该怎么拼（搜过：`Loc.cs` 无键、`assets_full` 侧无该 GO 词条表 —— 那批在远端 CCD）。
4. **P3 遗留的两处「可能溢出」仍未量**（本波不许跑 Unity）：`未选战将`→`还没有选战将`（`DeckInfoPopup.cs:836` / `PracticeModePopup.cs:565`，4→6 字）· `没有可选的卡组`→`没有符合当前筛选的卡组`（`DeckSelectionPopup.cs:703`，7→11 字）。`DeckInfoPopup` 那两处**无 `SetAutoFitBox`**。**建议随宿主看一张渲染图。**
5. **本批一次 Unity 自检都没跑**（排期：波 2 统一加）⇒ 19 处的运行期效果**一次都没验过**；没动 `Editor/`、没动 `Core/Loc.cs`、没碰 git。

## ⑧ 需要哪个断言宿主 + 顺手发现（⛔ 一处都没顺手改）

**断言宿主（本波按排期不写断言）**
- `Editor/ShellScene.cs` ← #1–#8、#12–#14（`LiveOpsEventWindow`/`PracticeModePopup` **两扇都挂壳锚点**）。
- `Editor/CollectionScene.cs` ← #9–#11、#18、#19（该宿主 `:6742` 已在调 `ImportDeck`；`DeckSelectionPopup` 空态也在它那侧）。
- `Editor/DeckScene.cs` 或 `ShellScene` ← #16、#17（`DeckInfoPopup`）。
- 现成模板：`Editor/ShopScene.cs:1502`（`CheckTrue(Loc.HasEntry(键))` + 字 = `Loc.T(键)`）。
- 三条必备：**①逐键 `HasEntry`** · **②本批字面量不再出现在本批源码里** · **③两语档各断一次**（`zh != en` 且英文无汉字）。
  ⚠️ #11 那条**不能**断「等于旧串」—— 它**有意变了**；要断的是「等于 `Loc.T("MenuDeck/Error/ImportNotPersisted")` 且含 `DeckRuntime` 那一版的形状」。

**顺手发现**
1. 🔴 **`Editor/CollectionScene.cs:2938/2944` 与 `:6768` 的断言拿【写死中文】当期望值**（`CheckText(imp.ErrorText, "先粘贴卡组串", …)` / `why != "先粘贴卡组串"`）。
   本批改后**中文档下仍然全绿**（ZH 列逐字相同，包括 `:6768` 那条 —— 新串与两个短键都不等）⇒ **今天不会红**。
   ⚠️ 但它们是**单语档断言**：切英文档就会红。要覆盖两语档 ⇒ 期望值应改成 `Loc.T("MenuDeck/Error/ImportEmpty")` 一类（那是 `Editor/` 的活，不在本批白名单）。
2. ⚠️ **`LiveOpsEventWindow.cs:1011` / `PracticeModePopup.cs:1679` 的 `"对局发起方点了取消"`** 是喂 `NetMatchmaking.Cancel` 的**内部原因串**（只进日志/诊断）⇒ 判 ②、未翻。将来别把它当 ① 翻掉。
3. ⚠️ **P3 §⑥ 那 5 条顺手发现仍全部开着**（`Net/` 整片 10 处写死 `"知道了"`/整句中文 · `Script` 里的协议串 · `ShellScene.cs:1138` 的样例正文 · `DeckInfoPopup` 那条英文已由本轮 #16 解决）—— 见上一轮交件，本件未重复列。
