# -*- coding: utf-8 -*-
"""B90-N5 账补丁生成器：社交_聊天 14 条 + 音频_语音 2 条 → 账/_parts/B90_N5.tsv
   用法：python -I -X utf8 _gen_B90_N5.py     （在 账/ 目录下跑）
   ⛔ 只读主表、只写 _parts 补丁；不动主表。
"""
import io, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)            # 账/
HEADER = "域\t原版类\t方法\t判据出处\t判据要点\t状态\t我们的落点\t差异\t等级\t轮次\t备注"

def load(fn):
    p = os.path.join(ROOT, fn)
    rows = []
    with io.open(p, "r", encoding="utf-8", newline="") as f:
        for line in f.read().split("\n"):
            if line.strip() == "":
                continue
            rows.append(line.split("\t"))
    return rows

def find(rows, cls, method):
    hits = [r for r in rows if len(r) >= 11 and r[1] == cls and r[2] == method]
    if len(hits) != 1:
        sys.exit("命中 %d 条：%s.%s" % (len(hits), cls, method))
    return hits[0]

SOC = load("社交_聊天.tsv")
AUD = load("音频_语音.tsv")

# ────────────────────────────────────────────────────────────── 本批改动的行（键 = 原版类 + 完整签名）
# 每项：(源, 类, 方法, {列号(1起): 新值})
E = []

# ═══════════════ 音频 2 条 ═══════════════
E.append((AUD, "UnitsVoiceLinesPanel", "HideChatBox(VoiceLinesOnSceneType type)", {
 6: "A",
 7: "Battle/UnitChatPanel.cs:1250（HideBox）· Battle/UnitChatPanel.cs:1233（HideBubble）（等价实现 = HideBox 按序号只取那一颗 + HideBubble 五步："
    "`AudioSource.Stop` / 清本句状态 / 关节点 / 清字 / `maxVisibleCharacters=0`）",
 8: "2026-10-28 B90-N5 现核：**② 「关即停音 + 清字」已补齐** ⇒ 升 A。逐句对 `UnitsVoiceLinesPanel__HideChatBox.c:9-25`："
    "① `AudioSource.Stop()`（`.c:11`，`HideBubble` 第一句）② `+0x40 = 0`（`.c:12` = `ResetState()` 里的 `isPlaying`）"
    "③ 波形件关（`.c:13-14` 的 `TouchInputManager.Toggle(touchInput,false)`；我们 `SetActive(false)` 连 `wave` 一起关，终态同）"
    "④ `SetActive(false)`（`.c:15`）⑤ **`SetText(string.Empty)` + `maxVisibleCharacters = 0`**（`.c:20-24`）——本轮补的就是这第 ⑤ 步里"
    "「清字」那一下（改前只置了 `maxVisibleCharacters = 0`，字串还留在 TMP 上）。"
    "🔴 **就地订正旧记录（铁律 5）**：本条原写「`HideBox`→`HideBubble` **完全不碰** `_player/_enemy/_radio.audio`」「能做的 `StopVoice()` 只有 "
    "`BattleDriver.Begin` 一个调用点」—— **两条都不成立**：2026-10-27 的 `B9` A-04 起 `HideBubble` 第一句就是 `b.audio.Stop()`（`HEAD` 里已如此），"
    "F8 那次大概读的是 `StopVoice`/`HideAll` 的**旧注释**而不是方法体（那两段注释本轮已同批订正）。"
    "⚠️ 顺序差异（如实记）：原版先 `SetActive(false)` 再清字（`.c:15`→`:20`），我们把清字放在 `SetActive(false)` **之前** —— 终态相同，"
    "且能避开「TMP 在未激活对象上量不出尺寸」那条坑（CLAUDE.md §三）。",
 9: "",
 11: "[B2AU 2026-10-27 第一批（A-04）] `Battle/UnitChatPanel.cs`：按颗关一颗（`HideBox(int)`）+ 五步照 `UnitsVoiceLinesPanel__HideChatBox.c:9-25`；"
     "`HideAll` 改走它。判据与逐条证据见 `报告/B9_B2AU_聊天面板音效.md` §②。"
     " ｜ 🔴 2026-10-28（`B90-N5`）：**② 已补齐 ⇒ 升 A**；旧备注里「需先裁定 `CloseChatBox`/`HideChatBox` 两条是否都停音 —— 判据缺」**已作废**："
     "两份 `.c` 都实读到了 `AudioSource.Stop`（`VoiceLineBox__HideChat.c:9` · `UnitsVoiceLinesPanel__HideChatBox.c:11`）。"
}))

E.append((AUD, "VoiceLinesPopupSelector", "SetTransformedWarlordNewChats(RawCardScript selectedWarlord, MatchData matchData, bool updateAvailable)", {
 6: "A",
 7: "Battle/ChatPopupPanel.cs:1463（SetTransformedWarlordNewChats）· Battle/ChatPopupPanel.cs:1478（ApplyChatLines）"
    "（等价实现 = ApplyChatLines：逐颗写字 + 只在越界档重算 `interactable`）",
 8: "2026-10-28 B90-N5 现核：**方法级逐句已复刻** ⇒ 升 A。对 `VoiceLinesPopupSelector__SetTransformedWarlordNewChats.c:22-84`："
    "① 督军 id 取 `chatMessagesReferenceId`、空则回落 `uniqueId`（`.c:23-29`）——我们直接收 id 串，由调用方解（等价）；"
    "② 那张 **2 元素** matchType 表 `{110 ClosedDeck, 120 ClosedDeckBot}`（`.c:32-38` 写死的 `0x6e`/`0x78`）落成 `TransformedWarlordTypes`，"
    "**与 `SetupChatOptions` 的 3 元素表故意不统一 = 原版如此**（`SetupChatOptionTypes`，⛔ 别统一）；"
    "③ 逐颗 `text = GetLocalizedChatMsg(warlord, i+5)`（`.c:56-84`，起点 5 与 `ChatMessageIndex` 同一处）；"
    "④ `interactable = IsAvailable(...)` **只在「下标越出 `ChatMessage` 枚举」那一支**真算（`.c:62-75`，与 `SetupChatOptions.c:87-111` 同形）；"
    "⑤ `updateAvailable == false` ⇒ **只换字不动 `interactable`**（`param_4` 只当闸）。另有空 id 守卫（出声、不换）。"
    "⚠️ **仍零生产调用者**：上游「督军换卡」（`CheckForHeroDeckTransform` + `BoardAnalysis.DeckMeetsCriteria`）整条未复刻 —— "
    "**那条缺口记在上游自己那一行**，不在本方法（本方法可直调、夹具已断）。"
    "📌 若调度台口径是「A 必须调用链可达」⇒ 本条回退 `C`，理由即此（B2 那轮判 C 的依据）；本条按「方法本身已复刻」判 A。",
 9: "",
 11: "[B2 2026-10-28] 本批零改动。缺的行为在**上游**（变身督军换 6 句），不在这个方法；在链做好前本条**不可能**转 A。"
     "⚠️ 路径订正：派活包写 Shell/ChatPopupPanel.cs ⇒ 实际是 Battle/ChatPopupPanel.cs。验收口 = 变身督军后那 6 句跟着换"
     " ｜ 2026-10-28（`B90-N5`）：现核方法体（`ApplyChatLines` + 2 元素表 + 越界档）**逐句已复刻** ⇒ 按「方法本身已复刻」升 A；"
     "上游缺口不改判、仍记在上游那一行。上面那句「不可能转 A」是 B2 的口径（A 需可达调用链），两种口径的取舍已写进差异列，等调度台一句话即可回退。"
}))

# ═══════════════ 社交 14 条 ═══════════════
E.append((SOC, "AllianceView", "Draw(Group currentAlliance)", {
 6: "A",
 7: "Shell/AllianceMemberTab.cs:1354（Build）· Shell/AllianceMemberTab.cs:1665（Apply）"
    "（等价实现 = Build 的整条渲染 + EditChrome.Apply 的显隐梯子）",
 8: "2026-10-28 B90-N5 现核：**渲染侧逐条已复刻** ⇒ 升 A。Build（`:1354`）建齐：盟名 / 两处评级（`RateEmpty` = prefab 自带 `'-------'`）/ "
    "简介 / 成员列（`SocialData.Members`，按 `Rating` 降序）/ 徽章两层 / `regionText`（act T 与 act F **两棵各一份、运行期都亮**，判据 "
    "`out_cpp2il_dev_ilspy/AllianceView.cs:459-460` 那句无条件的 `SetActive(true)`）；编辑态四件 + `regionText` 的显隐梯子**唯一一份**在 "
    "`EditChrome.Apply`（`:1665`）。"
    "**①②③④ 不是「没做」而是「没有数据源」**（每一项都有独立的上游行，⛔ 不在本行重复算）："
    "① 徽标 logo 只在 `badge.featuredTrophy != null` 时画（原版同一支，本地恒 null ⇒ 走同一条不画的路）；"
    "② 两处评级数值 = 服务端 profile（`eloRating@0x38` / `allianceRating@0x7C`）＋ LiveOps 活动图标 key=6（13 个活动 SO 都没有）；"
    "③ `Group.region` / `Group.privacy` / 简介 = 服务器字段（`Group` 本体由 `AlliancesManager.GetGroupById` 取，账 142/143 判 D）；"
    "④ 成员列读 `SocialData.Members` —— **有写入方就能画**（行模板已建，账 30/31 已 A）。"
    "⚠️ 与 `Initialize(string groupId)` 的分界（免得看成一件事）：**那条 = 「去把 Group 取回来」（服务器），本条 = 「拿到 Group 之后怎么画」**。",
 9: "",
 11: "2026-10-28 E22b：⑤ 完整落地（含 regionText）｜①②③④ 判据空/远端/数据恒空 ⇒ 仍 B"
     " ｜ 2026-10-28（`B90-N5`）：现核渲染侧已齐（Build + EditChrome），①②③④ 全部是**数据源在服务器**⇒ 升 A；"
     "数据侧缺口各自记在 `AlliancesManager.GetGroupById`（D）/ `Initialize(string)`（B）/ LiveOps 图标那几行，不重复算。"
}))

E.append((SOC, "AllianceView", "OnEditCancel()", {
 6: "A",
 7: "Shell/AllianceMemberTab.cs:1665（Apply）· Shell/AllianceMemberTab.cs:1464（Cancel button 的 onClick）",
 8: "2026-10-28 B90-N5 现核：`Cancel button` 的 onClick = `chrome.Apply(false, true)`，与 "
    "`AllianceView.OnEditCancel`（`out_cpp2il_dev_ilspy/AllianceView.cs:521-538`）逐句对上 —— 关 `LanguagesDropdown` / `Privacy Dropdown` / "
    "`Confirm` / `Cancel`，重开 `Edit` 与 `regionText`。⇒ 升 A。"
    "两处 `interactable`（`cancelButton` / `saveButton` = false）**不可观测**：原版在同一个方法里紧接着就把这两颗 `SetActive(false)`"
    "（`:528-532`），而进编辑态的 `OnToggleEditClicked` 第一、二句又**无条件**置 `true`（`:490-491`）⇒ 那一格没有任何可观测状态，"
    "不是「漏做」。`descriptionText.interactable = false`（`.c:523`）我们天然成立 —— 那一格是 `Label`、永远不可编辑（= 退出编辑态该有的样子）。"
    "**仍如实留白**：描述框「进编辑态可编辑」那一半属 `OnToggleEditClicked`（账 100，仍 B）。",
 9: "",
 11: "2026-10-28 E22b：`regionText` 那一格已补（E22b 建的节点）⇒ 只剩两条 `interactable`（要动 `v.Hit`/`SocialView.Text` 那一族）⇒ B"
     " ｜ 2026-10-28（`B90-N5`）：两条 `interactable` 落在**同方法内就被 `SetActive(false)`** 的钮上 ⇒ 不可观测；描述格天然「不可编辑」"
     "⇒ 升 A。"
}))

E.append((SOC, "AllianceView", "OnToggleEditClicked()", {
 6: "B",
 7: "Shell/AllianceMemberTab.cs:1665（Apply）· Shell/AllianceMemberTab.cs:1449（Edit button 的 onClick）",
 8: "**已落地**：`Edit button` 的 onClick = `chrome.Apply(true, true)` —— 开两个下拉 + `Confirm`/`Cancel`、关 `Edit` 与 `regionText`"
    "（判据 `AllianceView.cs:483-519`）。**仍缺两条，都不是本方法能自决的**："
    "① `descriptionText.interactable = !alliance.blockDescriptionChange && !me.blockDescriptionChange`（`.c:502-510`）"
    "—— 🔴 **本轮核到「这一支要不要真可编辑」的答案：要**。`descriptionText` 在原版是 **`EverguildInputField`（TMP_InputField）**、"
    "prefab 里**一直都在**（§C 表 `Description input text` 那一行），`OnToggleEditClicked` 只切它的 `interactable` ⇒ "
    "**进编辑态 = 真能打字**；我们那一格是 `Label`、**本仓没有可编辑输入框**（同一缺口也压着 `CreateAllianceMenu` 的两组输入框）"
    "⇒ 要补先得有输入系统（不是我漏了 `interactable` 这一句）；"
    "② `privacyDropdown.value = (int)alliance.privacy` / `languageDropdown.value = IndexOf(GroupLanguages, alliance.region)`（`.c:512-518`）"
    "—— **机制已就绪**（2026-10-28 `B90-N5` 建的 `AllianceDropdownBox.SetValue` + 展开面，见账 101），"
    "**差的只是这两个值的来源**：`Group.privacy` / `Group.region` 是**服务器字段**（`Group` 本体由 `AlliancesManager.GetGroupById` 取，账 142/143 判 D）。"
    "⇒ 仍 B（缺口已从「没建展开面」缩小到「没有初值数据」+「没有输入框」）。"
    "⚠️ `interactable = true` 那两颗鈕**天然满足**（我们的钮激活即可点，没有「先置 false」的来源）⇒ 不算缺口。",
 9: "P1",
 11: "2026-10-28 E22b：只剩 `interactable` 与两个下拉的 `.value` ⇒ B"
     " ｜ 2026-10-28（`B90-N5`）：`interactable=true` 那两条天然满足、已从缺口里划掉；剩下的「描述可编辑」与「下拉 .value」"
     "分别卡在**没有输入框**与**没有展开面**（两个都是别的方法的欠账）⇒ 维持 B。"
}))

E.append((SOC, "AllianceView", "Start()", {
 6: "A",
 7: "Shell/AllianceMemberTab.cs:1582（Dropdown）· Shell/AllianceMemberTab.cs:2220（Create）· Shell/SocialData.cs:477（LanguageOptionLabels）"
    "（等价实现 = 选项表 → `Loc.T` 映射 → 交给 `AllianceDropdownBox`）",
 8: "**2026-10-28 `B90-N5` 三度现核 ⇒ 升 A**（三遍各补一段，逐条闭合）："
    "① 两张选项表 = `GroupLanguages` 14 值（逐字照 `GameStaticData.cs:2476-2507`）/ `GroupPrivacyNames`。"
    "② **选项的「字」现在走词条**（本轮补的那一条）：原版 `Start` = "
    "`ClearOptions()` + `AddOptions(GroupLanguages.Select(lang => new OptionData("
    "`GetTermTranslation(\"SocialMenu/Alliances/Language/\" + lang)`)))`、隐私同理 "
    "`GetTranslation(\"SocialMenu/Alliances/Privacy/\" + name)`（`Enum.GetNames(typeof(GroupPrivacy))` = `{Open, Private}`）"
    "（判据 = `out_cpp2il_dev_ilspy/AllianceView.cs:104-175`）；"
    "本轮现查官方 13 语表 `数据/游戏数据/官方本地化_13lang.json`：**16/16 条键全在**（14 语言 + 2 隐私，缺 0）⇒ "
    "落成 `SocialData.LanguageOptionLabels()` / `PrivacyOptionLabels()`（`:477`/`:480`，只在**喂盒子那一刻**映射），"
    "⛔ **没改** `GroupLanguages`/`GroupPrivacyNames` 本身（它们同时是 `IndexOf(…, region)` 的输入）；缺键支路 = 退回英文原名 + 出声。"
    "③ 展开面 ✅（`AllianceDropdownBox`，判据 普查 `:284-300`）。"
    "④ `Start` 注册的四个监听（`toggleEdit`/`save`/`cancel`/`memberList`）✅ 有真接线。"
    "⑤ `regionText`（`extra_info`）✅ 两棵树都建了。"
    "⑥ 两处 `value` 的**初值** = `Group.privacy` / `Group.region`（服务器字段；`Group` 由 `GetGroupById` 取 = 账 142/143 判 **D**）⇒ "
    "**永久零数据源**（按三档尺子第 ② 档：上游是 D ⇒ 允许 A，写「永久不可达 + 方法体已逐句复刻」），盒子本身有 `SetValue` 可接真值。",
 9: "",
 11: "2026-10-28 E22b：只剩「展开面不建」⇒ B ｜ 第一遍：展开面已建 ⇒ 曾升 A ｜ 第二遍：发现选项字没走词条 ⇒ 退回 B"
     " ｜ **第三遍：16/16 键全在、已按原版 `Loc.T` 映射 ⇒ 升 A**（①–⑥ 见差异列）。"
}))

E.append((SOC, "BattleAlliancePanel",
         "Open(string playerName, string playerTitle, int playerScore, PlayerAvatar avatar, CosmeticItemAvatarBorder avatarBorder, string allianceName, int allianceScore, GroupBadge allianceBadge)", {
 6: "B",
 7: "Shell/AlliancePanelWindow.cs:860（Open）· Shell/AlliancePanelWindow.cs:1064（Apply）",
 8: "① 已落地（E9）：出场动画 `alpha 0→1` / `scale ×0.75→1`（0.35s，三常量已读）。"
    "③ 已落地（B88-B7）：`Alliance Badge Drawer` 的 `Frame`+`Badge` 两层 + `Open(...,badgeId,badgeTier)` 重载。"
    "④ **本轮已补（2026-10-28 B90-N5）**：`Open`/`Apply` 新增 `playerScore` / `allianceScore`，按 "
    "`AllianceRatingDisplay__Initialize` 的**本地分支**写进 `Individual rating value` —— 无活跃 LiveOps 活动 ⇒ `text = score.ToString()`"
    "（`.c:60-66` 的 `LAB_1807ac910`；联盟那格 `param_5 = 0` 直接跳同一支，`.c:69`）；`-1` = 我们不知道 ⇒ 保持 prefab 空态串 + 出声"
    "（`0` 是合法值，⛔ 别拿 0 当哨兵）。"
    "**仍缺**：② 头像立绘 —— 要 `Open` 的 `PlayerAvatar`/`AvatarBorder` 实参 + `AvatarDisplay.ChangeAvatar` 那一跳；头像图本地齐"
    "（`Resources/Art/avatars/` 944 张），⚠️ 但 `ChangeAvatar(NULL)` 是**不画**（`AvatarDisplay__ChangeAvatar.c:34` 的 `op_Equality(param_2,0)` "
    "把整段跳掉）⇒「没数据就画 DefaultAvatar」是**错的口径**，⛔ 别照 `PlayerProfileUIController` 那条回落当本扇依据；"
    "⑤ 联盟分支单机恒不触发（`BattleDriver` 传 `Open(name,\"\",\"\")`）。② 与「喂实参」两处都在 `Battle/BattleDriver.cs`（本批白名单外）⇒ 仍 B。",
 9: "P1",
 11: "2026-10-28 E22 复核未动（②④⑤ 全在 `Battle/BattleDriver.cs`，白名单外）⇒ B"
     " ｜ 2026-10-28（`B90-N5`）：④ 的**接收侧**已做完（Open/Apply 收 score 并渲染，判据是 `AllianceRatingDisplay__Initialize` 的离线支），"
     "现在 ④ 只差 `BattleDriver` 把那两个实参传进来；② 仍要 BattleDriver 侧一起做 ⇒ 维持 B。"
}))

E.append((SOC, "ChatGlobalManager", "BlockPlayer(string playerId)", {
 6: "A",
 7: "Shell/ChatCache.cs:105（BlockPlayer）（等价实现 = ChatCache.BlockPlayer：查重 → 追加 → 整份名单落盘）",
 8: "2026-10-28 B90-N5 现核 + 新建等价物 ⇒ 升 A。逐句对 `decomp_full/ChatGlobalManager__BlockPlayer.c:27-77`："
    "① `.c:28-33` 已在名单 ⇒ 直接 return（我们 `Contains` 早退）② `.c:34-54` 追加一条 ③ `.c:55-67` 组 "
    "`{PlayerDataKey.BlockedPlayers → JSON(整份名单)}` ④ `.c:69` `PlayerDataManager.UpdateOnlineUserData`（**云端**）⇒ 我们走**本地中央存档** "
    "`SaveCenter.Set(SaveKeys.BlockedPlayers, …)`（同一个 29 号 key；写失败出声）⑤ `.c:72` `GameAnalytics.SendBlockUser` = **P3 遥测** ⇒ 不复刻（已在方法注释里逐条记）。"
    "存储形状差异：原版 payload 是**裸 JSON 数组**，`SaveCenter` 的既有约定要求 payload 是 JSON **对象** ⇒ 外面裹一层 `{\"playerIds\":[...]}`（读回语义一致）。"
    "多一处 `🔶 自己设计`：空 / null playerId 的守卫（出声 + 不加）—— 原版会往名单里塞一个 null 条目。",
 9: "",
 11: "断点：本方法体内无直接后端调用；转发：ChatGlobalManager.cs:1686 `list.AddWithResize((object)playerId);`；该类后端入口 = ChatGlobalManager.cs:1100 "
     "`Photon.Chat.AuthenticationValues authenticationValues = new Photon.Chat.AuthenticationValu` ｜ 2026-10-28 `B89-E25b` 现核：原判 D 是判错"
     "（纯本地缓存，Q4 §四·二），改 C；缺一跳见差异列。原『保持 C 等用户裁定』G2 残留已删。（⚠️ 本行取代 `B89_E25.tsv` 同键行，合并顺序 E25→E25b）"
     " ｜ 2026-10-28（`B90-N5`）：**已新建 `Shell/ChatCache.cs` 落地**（黑名单 + 持久化）⇒ 升 A；`GameAnalytics.SendBlockUser` 属 P3 遥测、不复刻，已在方法注释记明。"
}))

E.append((SOC, "ChatGlobalManager", "CheckBlockedList(string playerId)", {
 6: "A",
 7: "Shell/ChatCache.cs:84（CheckBlockedList）（等价实现 = 一行 `_blocked.Contains(playerId)`，名单懒加载自 `SaveKeys.BlockedPlayers`）",
 8: "2026-10-28 B90-N5 现核 + 新建等价物 ⇒ 升 A。判据 `decomp_full/ChatGlobalManager__CheckBlockedList.c:9-13` —— 整个方法体就是 "
    "`blockedPlayers.Contains(playerId)`（原版列表为 null 时抛 NRE；我们列表恒非 null）。名单本体已建（见 `BlockPlayer` 那一行）。",
 9: "",
 11: "若恢复聊天，这条是本地可做项（PlayerData 名单）｜断点：本方法体内无直接后端调用；转发：ChatGlobalManager.cs:1668 `NullReferenceException ex = new NullReferenceException();`；"
     "该类后端入口 = ChatGlobalManager.cs:1100 `Photon.Chat.AuthenticationValues authenticationValues = new Photon.Chat.AuthenticationValu` ｜ "
     "2026-10-28 `B89-E25b` 现核：原判 D 是判错（纯本地缓存，Q4 §四·二），改 C；缺一跳见差异列。原『保持 C 等用户裁定』G2 残留已删。"
     "（⚠️ 本行取代 `B89_E25.tsv` 同键行，合并顺序 E25→E25b） ｜ 2026-10-28（`B90-N5`）：名单已建 ⇒ 本方法一行 `Contains` 落地 ⇒ 升 A。"
}))

E.append((SOC, "ChatGlobalManager", "CheckIfOlderLogin(PrivateMessageData messageData)", {
 6: "A",
 7: "Shell/ChatCache.cs:306（CheckIfOlderLogin）· Shell/ChatCache.cs:314（CheckIfOlderLoginParsed）"
    "（等价实现 = `parsed < LoginTimeMs`；`LoginTimeMs` = 本进程首次触到本类时的本机 UTC 毫秒）",
 8: "2026-10-28 B90-N5 现核 + 新建等价物 ⇒ 升 A。判据 `decomp_full/ChatGlobalManager__CheckIfOlderLogin.c:31-59`："
    "`.c:33` `long.TryParse(messageData.parameter1, out v)`（失败时 `v` 保持 **0**）· `.c:58` `return v < playerData.serverTimeOnLogin`。"
    "⚠️ **形参收窄（如实记）**：原版只用到 `messageData.parameter1` 这一个字段（`PrivateMessageData` 的其余字段全属私信链、判 D）⇒ 我们收 "
    "「服务器时刻毫秒」的 `long` 版 + 保留一个「先 `TryParse` 再比」的 `Parsed` 版（`TryParse` 是原版方法体第一句，不单独开一口就没法逐条对）。"
    "⚠️ **照抄原版的行为**：`TryParse` 失败 ⇒ `v = 0` ⇒ 结果 **true**（0 比任何真实时刻都小）——⛔ 别「修」成 false。"
    "🔶 自己设计（非原版）：`LoginTimeMs` 的**取值来源** —— 原版取服务器在登录结果里给的 `serverTimeOnLogin`"
    "（`PlayerDataManager.cs:11753-11759`），我们无服务器 ⇒ 取**本机时钟**（懒初始化一次）；语义仍是「本次会话开始时刻」。"
    "⚠️ 调用链：原版的调用点在被反编译器丢掉的 `ReceiveMessageWithSameId` 里（`.c`/ILSpy 全树只有定义、没有调用点）⇒ 本地 0 调用者，属私信链（那一行判 D）。",
 9: "",
 11: "断点：本方法体内无直接后端调用；转发：ChatGlobalManager.cs:2546 `bool flag = long.TryParse(messageData.parameter1, out var _);`；"
     "该类后端入口 = ChatGlobalManager.cs:1100 `Photon.Chat.AuthenticationValues authenticationValues = new Photon.Chat.AuthenticationValu` ｜ "
     "2026-10-28 `B89-E25b` 现核：原判 D 是判错（纯本地缓存，Q4 §四·二），改 C；缺一跳见差异列。原『保持 C 等用户裁定』G2 残留已删。"
     "（⚠️ 本行取代 `B89_E25.tsv` 同键行，合并顺序 E25→E25b） ｜ 2026-10-28（`B90-N5`）：已落地（含 `TryParse` 那一跳与 `-1`/0 语义）⇒ 升 A。"
}))

E.append((SOC, "ChatGlobalManager", "IsMessageRepeated(string channelId, ChatMessageData message)", {
 6: "A",
 7: "Shell/ChatCache.cs:192（IsMessageRepeated）（等价实现 = 取该频道切片末条，五个字段逐个比）",
 8: "2026-10-28 B90-N5 现核 + 新建等价物 ⇒ 升 A。判据 `decomp_full/ChatGlobalManager__IsMessageRepeated.c:15-44`："
    "`.c:15-17` 取 `GetCachedChatMessages(channelId).LastOrDefault()`（空 ⇒ false）· `.c:23-31` 逐字段 `op_Inequality`："
    "`data(+0x50)` → `msg(+0x58)` → `playerId(+0x20)` → `playerName(+0x28)`（任一不等 ⇒ false）· `.c:33-38` `type(+0x18)` 相等 ⇒ **true**。"
    "我们的字段映射：`LocData`/`Text`/`PlayerId`/`Sender`/`Type`（映射表写在 `Shell/ChatCache.cs` 文件头）。"
    "⚠️ **只比末条、不做全表查找** —— 全表去重是表现侧的 `ChatContentView.CheckForMessage`（另一行），⛔ 别顺手改。"
    "⚠️ ILSpy 那份不可用：它把「与末条同名字段比」读成 `if (data == (string)0)`（第二个实参整个丢了）⇒ 以 `.c` 为准。",
 9: "",
 11: "断点：本方法体内无直接后端调用；转发：ChatGlobalManager.cs:1610 `List<ChatMessageData> cachedChatMessages = GetCachedChatMessages(channelId);`；"
     "该类后端入口 = ChatGlobalManager.cs:1100 `Photon.Chat.AuthenticationValues authenticationValues = new Photon.Chat.AuthenticationValu` ｜ "
     "2026-10-28 `B89-E25b` 现核：原判 D 是判错（纯本地缓存，Q4 §四·二），改 C；缺一跳见差异列。原『保持 C 等用户裁定』G2 残留已删。"
     "（⚠️ 本行取代 `B89_E25.tsv` 同键行，合并顺序 E25→E25b） ｜ 2026-10-28（`B90-N5`）：已落地（五字段逐个比）⇒ 升 A。"
}))

E.append((SOC, "ChatGlobalManager", "TryCacheMessage(string channelId, ChatMessageData message)", {
 6: "A",
 5: "屏蔽名单外的消息进缓存并超 100 裁一条（**50 是初始容量、不是裁剪阈值**）",
 7: "Shell/ChatCache.cs:218（TryCacheMessage）（等价实现 = 黑名单过滤 → 追加 → 该频道超 100 条丢最旧一条）",
 8: "2026-10-28 B90-N5 现核 + 新建等价物 ⇒ 升 A。判据 `decomp_full/ChatGlobalManager__TryCacheMessage.c:19-63`："
    "`.c:19` 取该频道缓存 · `.c:20-32` `message == null` ⇒ 抛；**发送者在黑名单里（`.c:27-28` 读 `+0x20` = `playerId`）⇒ 一条都不加、原样返回** · "
    "`.c:33-52` 追加 · `.c:53-57` 该频道条数 **> 100** ⇒ `RemoveAt(0)`。"
    "🔴 **就地订正旧记录（铁律 5）**：本条判据要点原写「裁剪到 **50**」—— **50 是 `GetCachedChatMessages` 里 `new List<>(50)` 的初始容量**，"
    "裁剪阈值是 **100**（`.c:53` 与 `0x65` = 101 比 ⇒ 上限 100；ILSpy 同一处写 `_size > 100`）。本轮已把判据要点改过来。"
    "⚠️ 我们多一步：把 `message.Channel` 置成 `channelId`（原版的频道是字典 key、消息不带频道；我们单表模型靠这个字段做切片，见 `ChatCache.cs` 文件头）。",
 9: "",
 11: "断点：本方法体内无直接后端调用；转发：ChatGlobalManager.cs:2825 `List<ChatMessageData> cachedChatMessages = GetCachedChatMessages(channelId);`；"
     "该类后端入口 = ChatGlobalManager.cs:1100 `Photon.Chat.AuthenticationValues authenticationValues = new Photon.Chat.AuthenticationValu` ｜ "
     "2026-10-28 `B89-E25b` 现核：原判 D 是判错（纯本地缓存，Q4 §四·二），改 C；缺一跳见差异列。原『保持 C 等用户裁定』G2 残留已删。"
     "（⚠️ 本行取代 `B89_E25.tsv` 同键行，合并顺序 E25→E25b） ｜ 2026-10-28（`B90-N5`）：已落地（含 100 条裁剪与黑名单过滤）⇒ 升 A；"
     "判据要点里的「50」已订正为初始容量。"
}))

E.append((SOC, "ChatGlobalManager", "TryCacheMessages(string channelId, List<ChatMessageData> messages)", {
 6: "A",
 7: "Shell/ChatCache.cs:237（TryCacheMessages）（等价实现 = 清空该频道 → 逐条走 TryCacheMessage → 返回切片）",
 8: "2026-10-28 B90-N5 现核 + 新建等价物 ⇒ 升 A。判据 `decomp_full/ChatGlobalManager__TryCacheMessages.c:25-52`："
    "`.c:25-32` `list.Clear()`（`_version++` / `_size=0` / `Array.Clear`）· `.c:33-48` `messages == null` ⇒ 抛，否则**逐条走 `TryCacheMessage`**"
    "（⇒ 黑名单过滤与 100 条裁剪**再算一遍**，黑名单里的人即使在这批里也进不去）· `.c:49-51` 枚举器 `Dispose()` + 返回 `GetCachedChatMessages`。"
    "⚠️ `.c:49` 那条 `NetworkingPeer__OnMessage` 是**枚举器 `Dispose()`**（ICF 折叠把名字换成了别的函数），**不是**网络调用 —— 判据说「方法体 0 处后端调用」仍然成立。",
 9: "",
 11: "断点：本方法体内无直接后端调用；转发：ChatGlobalManager.cs:2858 `List<ChatMessageData> cachedChatMessages = GetCachedChatMessages(channelId);`；"
     "该类后端入口 = ChatGlobalManager.cs:1100 `Photon.Chat.AuthenticationValues authenticationValues = new Photon.Chat.AuthenticationValu` ｜ "
     "2026-10-28 `B89-E25b` 现核：原判 D 是判错（纯本地缓存，Q4 §四·二），改 C；缺一跳见差异列。原『保持 C 等用户裁定』G2 残留已删。"
     "（⚠️ 本行取代 `B89_E25.tsv` 同键行，合并顺序 E25→E25b） ｜ 2026-10-28（`B90-N5`）：已落地（清空 + 逐条 + 返回）⇒ 升 A。"
}))

# ── ③ 三条阻塞（不改判，只补证据 / 修落点行号）
E.append((SOC, "AllianceView", "Initialize(string groupId)", {
 6: "A",
 7: "Shell/AllianceSearchTab.cs:362（HandleDisplayAlliance）（等价实现 = 点亮详情树 + 填盟名/两处评级/region/简介/成员列；"
    "原版 `AllianceView.Initialize`）",
 8: "**2026-10-28 `B90-N5` 现核 ⇒ 升 A。** 依据两条："
    "① **几何逐颗核过**（`判据/B90_C2b_远端prefab几何.md` §C 的 14 个字段 × 2 个实例，与我们的 `GeoT`/`GeoF` 逐值对账："
    "两边的 GeneralDetails 根 = `331.17,188.83→1920.00,1080.05`，§C 的相对矩形 + 该原点与我们的绝对值**逐颗对上**，"
    "差只出现在 `Config fields` 三个 LayoutGroup 孩子上（2.5–4.6 px，两份 dump 的布局趟不同）；"
    "14 个字段全部有落点：`icon`=BadgeDrawer（Frame+Badge）· `nameText` · `ratingText`/`draftRatingText`（两处 `AllianceRatingDisplay`，"
    "`Secondary Icon` 两份都是 **act F** ⇒ 不建，判据 普查 `:273`/`:277`）· `regionText`=extra_info · `descriptionText` · "
    "两个 dropdown（**已可展开**，见账 101）· 三个钮 · `memberList` · `memberOptionsPopup`（账 99）。"
    "② **零数据源但上游是 D 档**：形参已由 `Group` 换成**盟名字符串**；徽标/评级/region/简介/成员列**全部**靠 `Group`，"
    "而 `Group` 本体只能由 `AlliancesController.GetGroupById` 取（**服务器**）= 账 142/143 判 **D** ⇒ "
    "**永久零数据源**（不是「还没做」）。空态 = 原版没有该数据时的样子（`RateEmpty` / `Members: --/20` / `English / Private`），已在 `Say` 里逐句出声。"
    "🔴 新的尺子（调度台 2026-10-28 三档）：零调用者 + 上游**在 D（离线不可达）** ⇒ 允许 A，差异列写死「永久不可达 + 方法体已逐句复刻」。",
 9: "",
 11: "2026-10-28 E22 复核未动：整条依赖服务器 Group ⇒ 仍 B ｜ 2026-10-28（`B90-N5`）：落点行号 `:309` 已漂（现读 **362**）⇒ 就地订正；"
     "同轮按新尺子（上游 D 档）**升 A** —— 几何 14 个字段逐颗核过（§C 表），数据侧永久不可达（`GetGroupById` = D）。"
}))

E.append((SOC, "AlliancesManager", "GetCurrentTrophies()", {
 6: "D",
 7: "Shell/SocialData.cs（数据只有 `AllianceTrophies` 一个 **int 计数**，没有「已获得奖杯」集合）",
 8: "🔴 **2026-10-28 调度台裁定（本条按「铁律 11 例外①：原版没有 / 判据是空的」记，**状态沿用当时的 B、不改成 A**）**："
    "本地**不存在**「已获得奖杯」这个数据（C2 也核过：定义表 `AllianceTrophyModel.Defs` 本地同样是空的）⇒ "
    "「补一个返回集合、内容永远空」的方法 = **形状对、内容永远空，把缺口换了个形状藏起来，不是复刻行为** ⇒ **不实现**。"
    "（⛔ 不以「影响小」为理由 —— 理由是**数据源在服务器、本地不存在**。）"
    "技术上：`AlliancesManager` 方法体无后端调用（原判 D 判错）；**仍缺（形状不符）**：原版返回的是**集合** `IEnumerable<AllianceTrophy>`"
    "（`decomp_full/AlliancesManager__GetCurrentTrophies.c` 返回 `trophyController+0x38`），我们本地只有**计数** `SocialData.AllianceTrophies`（int，"
    "`SocialData.cs:30`）＋ 一张**定义表** `AllianceTrophyModel.Defs`（**全量定义**、不是「已获得」，且默认空、要 `SetDefsForTest` 注入）"
    "⇒ 补一个返回 `int` 的同名方法是**形状不对**（那是自证，不是复刻）。要升 A 得先有「已获得奖杯」的数据源（原版在服务器）。"
    "2026-10-28（B90-N5）复核：结论不变，**不做**。",
 9: "",
 11: "2026-10-28 E22 复核：不做（返回集合 vs 我们只有计数；补 int 版 = 自证）⇒ B ｜ 2026-10-28（`B90-N5`）三次复核不变"
     " ｜ 🔴 **2026-10-28 调度台裁定：按「铁律 11 例外①」判 **D**（不是 A）：本地不存在「已获得奖杯」数据（`AllianceTrophyModel.Defs` 本地也空），"
     "返回空集合 = 形状对内容永远空 = 把缺口换个形状藏起来 ⇒ 不实现；⛔ 不以「影响小」为由。理由全文见差异列。"
}))

E.append((SOC, "CreateAllianceMenu", "Initialize()", {
 6: "A",
 7: "Shell/AllianceSearchTab.cs:288（ShowCreate）（等价实现 = Create 版面：两组「标题+输入框」+ 两个可展开下拉 + "
    "`Create Alliance Text` + `Price Display Button>Generic UI Button>Price Display{icon,text}`；原版 `CreateAllianceMenu.Initialize`）",
 8: "**2026-10-28 `B90-N5` 现核 ⇒ 升 A。** 三条逐条闭合："
    "① **选项表 + 展开面都有了**（`GroupLanguages` / `GroupPrivacyNames` + `AllianceDropdownBox`，见账 101）。"
    "② 🔴 **`1000` 不再是「我们写死的」**：`判据/B90_C2b_远端prefab几何.md` §D 现读 —— `Price Display`（RT `-1193917768153177949`）"
    "下面 `text` 那格 **baked 文本就是 `\"1000\"`**（`sizeDelta (0,-13.596)`、锚 `(0.0378,0.1085)→(0.9543,0.8765)`）"
    "⇒ **它是 prefab 里烘死的样例字**，我们的显示值与原版**逐字相同**；"
    "`AlliancesConfig.createAlliancePrice` 的**运行时覆盖**那一跳本地无该配置资产（**判据空**）⇒ 原版没有该资产时也保持 prefab 值 = 我们的行为。"
    "③ `TryCreateAlliance`（点下去要服务器）**是另一个方法**（账 359 判 **D**）⇒ 不是本方法的欠账。"
    "版面其余各件与 §D 表逐值对上：`Name input title`/`Name Input`、`Desc input title`/`Desc Input`（都是 `40K_dropdown_bg` 九宫 23,20,23,20）、"
    "`Create Alliance Text`、`Generic UI Button`（`40K_button` 489×107 九宫 234,46,234,46）、两个下拉（`40K_dropdown_field_closed`）。",
 9: "",
 11: "2026-10-28 E22 复核未动：② 判据空 / ③ 要服务器 ⇒ B ｜ 2026-10-28（`B90-N5`）：① 展开面**已建**；② 拿到实据（`1000` = prefab 烘死字，"
     "§D）；③ 属账 359（D）⇒ 本方法无欠账 ⇒ **升 A**。"
}))

# ────────────────────────────────────────────────────────────── 裁定 1 补丁（调度台 2026-10-28）
# 「A 档不要求可达调用链，但必须写死三件」+ 2026-10-28 晚的**最终三档尺子**：
#   ① 零调用者 + 上游**在 B/C** ⇒ A，写「上游是 X、账在 Y」
#   ② 零调用者 + 上游**是 D（离线不可达）** ⇒ A，写「**永久不可达**」+「方法体已逐句复刻」
#   ③ 逐句对过 `.c`（⛔ 不许拿「我们写了同名方法」当已复刻）
EXTRA = {
 ("VoiceLinesPopupSelector", "SetTransformedWarlordNewChats(RawCardScript selectedWarlord, MatchData matchData, bool updateAvailable)"):
   "｜🔴 **裁定 1（调度台 2026-10-28，三档尺子第 ① 档：上游在 C）**："
   "① **零调用者**；上游 = **「督军换卡」那一步**（`BattleManager.CheckForHeroDeckTransform` + `BoardAnalysis.DeckMeetsCriteria`，"
   "两者都没建 ⇒ 换卡永不发生 ⇒ 本方法永不触发）。"
   "② 该上游**另有账在追** = `账/战斗逻辑驱动.tsv:248`（**C**）；另 `BoardAnalysis.DeckMeetsCriteria` 在 `:671` 判 D。⛔ 不是「两边都不记」。"
   "③ 本条是**逐句读 `.c`**（`SetTransformedWarlordNewChats.c:22-84` 五行结构逐个对）+ ILSpy 交叉核。",

 ("ChatGlobalManager", "BlockPlayer(string playerId)"):
   "｜🔴 **裁定 1（2026-10-28）**：① **已不是零调用者** —— 本轮把 `Shell/ChatPanel.cs:563`"
   "（`OnOption` 里 `Block player` 的确认框 onConfirm）接到 `ChatCache.BlockPlayer`（原版同形：`ChatPlayerOptionsPanel.Block()` 的 "
   "onConfirm = `instance.BlockPlayer(messageData.playerId)`，ILSpy `:245`）；再往上游 = 选项面板那颗钮（账 329 = A）。"
   "② 上游账 = 账 329（**A**）+ 本行 ⇒ **无 B/C/D 欠账**（这条链已通到底）。③ 逐句读 `.c`（`BlockPlayer.c:27-77`）。",

 ("ChatGlobalManager", "CheckBlockedList(string playerId)"):
   "｜🔴 **裁定 1（2026-10-28）**：① 调用者 = `ChatCache.TryCacheMessage`（本批代码；**原版同形** —— `.c:27-28` 那句屏蔽过滤就是"
   "内联调 `CheckBlockedList`）；再往上 = `TryCacheMessage` 那一行的上游（见该行）。"
   "② 上游账 = 本补丁 `TryCacheMessage` 那一行引的 D 行 ⇒ **无 B/C 欠账**。③ 逐句读 `.c`（`CheckBlockedList.c:9-13` 整个方法体一句 `Contains`）。",

 ("ChatGlobalManager", "CheckIfOlderLogin(PrivateMessageData messageData)"):
   "｜🔴 **裁定 1（调度台 2026-10-28，三档尺子第 ② 档：上游是 D）**："
   "① **零本地调用者**；上游 = `ChatGlobalManager.ReceiveMessageWithSameId`（重复登录检查那一跳，走 Photon 私信）。"
   "② 该上游 = 本表 `ChatGlobalManager.ReceiveMessageWithSameId` 判 **D（离线不可达）** ⇒ **永久零调用者**"
   "（不是「还没做」；且它的调用点在全量反编译里被丢了 —— `.c`/ILSpy 全树只有定义）。③ **方法体已逐句复刻**：`CheckIfOlderLogin.c:31-59`"
   "（`TryParse` → `ObscuredLong.op_Implicit` → `<`），含「解析失败 ⇒ `v=0` ⇒ true」那条照抄。",

 ("ChatGlobalManager", "IsMessageRepeated(string channelId, ChatMessageData message)"):
   "｜🔴 **裁定 1（2026-10-28，三档尺子第 ② 档：上游是 D）**："
   "① **零本地调用者**；上游 = `ChatGlobalManager.SendChatMessage(string, string)`（发之前挡重复那一步）。"
   "② 该上游 = 本表 `SendChatMessage(string chatMessage, string channelId)` 判 **D（`PublishMessage` 走 Photon）⇒ 永久零调用者**。"
   "③ **方法体已逐句复刻**：`IsMessageRepeated.c:15-44`（末条 + 五字段逐个比），ILSpy 那份把比较的第二实参丢了、**没拿它当判据**。",

 ("ChatGlobalManager", "TryCacheMessage(string channelId, ChatMessageData message)"):
   "｜🔴 **裁定 1（2026-10-28，三档尺子第 ② 档：上游是 D）**："
   "① 调用者 = `ChatCache.TryCacheMessages`（本批代码）；**真正的生产上游** = `ChatGlobalManager.OnGetMessages` / "
   "`ProcessReceivedChatMessage`（收获消息 → 入缓存那一跳）。"
   "② 该上游 = 本表 `OnGetMessages` / `ProcessReceivedChatMessage` 判 **D（Photon 收包链、离线不可达）⇒ 永久零生产调用者**。"
   "③ **方法体已逐句复刻**：`TryCacheMessage.c:19-63`（黑名单过滤 → 追加 → 超 100 裁一条）。",

 ("ChatGlobalManager", "TryCacheMessages(string channelId, List<ChatMessageData> messages)"):
   "｜🔴 **裁定 1（2026-10-28，三档尺子第 ② 档：上游是 D）**："
   "① **零本地调用者**；上游 = `ChatGlobalManager.DownloadChatHistory`（整批拉历史 → 整批替换）与 `ChatPanel.SyncMessageHistory`。"
   "② 该两处 = 本表 `DownloadChatHistory` / `ChatPanel.SyncMessageHistory` 判 **D ⇒ 永久零调用者**。"
   "③ **方法体已逐句复刻**：`TryCacheMessages.c:25-52`；`.c:49` 那条 `NetworkingPeer__OnMessage` 已查明是枚举器 `Dispose()`。",
}

# ────────────────────────────────────────────────────────────── 组装
used = set()
out = [HEADER]
for src, cls, method, upd in E:
    row = list(find(src, cls, method))
    for col, val in upd.items():
        row[col - 1] = val
    key = (cls, method)
    if key in EXTRA:                 # 裁定 1 的三条 → 追加到差异列
        row[7] = row[7] + EXTRA[key]
        used.add(key)
    if len(row) != 11:
        sys.exit("列数不对：%s.%s = %d" % (cls, method, len(row)))
    row[9] = "B90"          # 第 10 列 = 轮次
    out.append("\t".join(row))
missing = set(EXTRA) - used
if missing:
    sys.exit("EXTRA 有键没被吃到：" + repr(sorted(missing)))

p = os.path.join(HERE, "B90_N5.tsv")
with io.open(p, "w", encoding="utf-8", newline="") as f:
    f.write("\n".join(out) + "\n")
print("写出 %s ：%d 行（含表头）" % (p, len(out)))
for l in out[1:]:
    f = l.split("\t")
    print("  %-4s | %-24s | %s" % (f[5], f[1], f[2][:70]))
