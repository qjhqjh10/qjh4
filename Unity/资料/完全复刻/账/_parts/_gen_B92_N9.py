# -*- coding: utf-8 -*-
"""B92 波 4 · N9 的账补丁生成器（只写 `账/_parts/B92_N9.tsv`，⛔ 不碰主表）。

做法：按 (原版类, 方法) 从**主表那一行自己的字符串**取键（⛔ 不自己重写签名），
只改 `状态 / 我们的落点 / 差异 / 轮次 / 备注` 四列，其余原样抄回。
"""
import io, os, sys

LEDGER = r"d:/4/Unity/资料/完全复刻/账"
OUT = r"d:/4/Unity/资料/完全复刻/账/_parts/B92_N9.tsv"
COLS = ["域", "原版类", "方法", "判据出处", "判据要点", "状态", "我们的落点", "差异", "等级", "轮次", "备注"]

def read_raw(path):
    b = io.open(path, "rb").read()
    t = b.decode("utf-8-sig")
    if b.count(b"\r\n") and b.count(b"\r\n") == b.count(b"\n"):
        return t.split("\r\n")
    return t.split("\n")

# ---- 索引全 21 张主表：(原版类, 方法) -> [(文件, 行下标, 字段列表)] ----
idx = {}
files = [f for f in sorted(os.listdir(LEDGER)) if f.endswith(".tsv") and not f.startswith("_")]
for fn in files:
    lines = read_raw(os.path.join(LEDGER, fn))
    for i, l in enumerate(lines):
        if not l.strip():
            continue
        f = l.split("\t")
        if len(f) != 11:
            continue
        idx.setdefault((f[1].strip(), f[2].strip()), []).append((fn, i, f))

def row(cls, meth):
    hits = idx.get((cls, meth), [])
    if len(hits) != 1:
        print("!! 键命中 %d 处：%s · %s -> %s" % (len(hits), cls, meth, [(h[0], h[1] + 1) for h in hits]))
        sys.exit(2)
    fn, i, f = hits[0]
    return fn, i, list(f)

def setrow(cls, meth, status=None, land=None, diff_add=None, note_add=None, round_=None):
    fn, i, f = row(cls, meth)
    if status:
        f[5] = status
    if land:
        f[6] = land
    if diff_add:
        f[7] = (f[7] + " " if f[7].strip() else "") + "｜" + diff_add
    if note_add:
        f[10] = (f[10] + " " if f[10].strip() else "") + "｜" + note_add
    if round_:
        f[9] = round_
    return f, fn, i

out = []

# ==================================================================== 卡组编辑
f, fn, i = setrow(
    "CardDeck", "CardCount(bool countHero)", status="A",
    land="CardPresentation/Deck/DeckEditorState.cs:1506（DeckCardCount）· 消费者 = `DeckRuntime` 空态警告那条判据（原版 `DeckEditingPanel__Initialize` 同一句）",
    diff_add=("**2026-10-28 B92-N9 已补（A→齐）**：`DeckCardCount(bool)` 逐句照 `CardCount__CardCount.c` 全文 —— "
              "`列表数 + (countHero && 督军非空 ? 1 : 0)`；⚠️ **防御卡不加**（原版 `cardLibrary` 里没有它）。"
              "消费者已接：`DeckRuntime.RefreshDeckList` 的 `Empty Warning` 改用它（原版 `DeckEditingPanel__Initialize` = "
              "`emptyWarning.SetActive(CardCount(true) == 0)`）—— 改前我们按「可见行里有几张真卡」判，**只有督军的卡组会误报「拖牌进来」**。"
              "⛔ `DeckRules.CardCount(GameMode)` 是**张数上限**（30/40），同名不同义，别混。"),
    round_="B92_N9 2026-10-28",
    note_add="2026-10-28（`B92_N9`）：补 `countHero` 支 + 把 Empty Warning 的判据换成它；落点改指 `DeckDB` 那一层的 `DeckCardCount`（追加在 `DeckEditorState` 文件尾的第二个 partial 块，零行号漂移）。")
out.append((fn, i, f))

f, fn, i = setrow(
    "CardDeck", "Has(RawCardScript card)", status="A",
    land="CardPresentation/Deck/DeckEditorState.cs:1517（DeckHas）· 消费者 = `DeckRuntime.TickCardWindowAltArt`（关详情窗那一帧对账）· 换条口 = `DeckEditorState.ReplaceDeckEntry`",
    diff_add=("**2026-10-28 B92-N9 已补（A→齐）**：原版 `CardDeck__Has.c` 全文三跳照抄 —— "
              "① **督军为空 ⇒ 恒 false**（连普通卡都不比，`.c:29-32` 就这一支；原版自己的形状，⛔ 别「修好」）"
              "② 督军同卡键 ⇒ true ③ `CountOf > 0`（同卡键 = `GetOriginalId`，异画副本算同一张）。"
              "消费者 = 详情窗换异画那一拍（原版只有两处：`DeckEditingWindow.cs:217` / `:918` = "
              "`if (EditingDeck.Has(card)) { RemoveCard(card); TryAddCard(altArt); }`）—— "
              "`CardDetailPopup` 没暴露关窗回调，我们按 `CurrentState` **由开到关那一帧**跑同一段（`TickCardWindowAltArt`）。"
              "⚠️ `RuleEngine/Core/DeckRules.cs` 的 `CountOf` 本批**未动**（`RuleEngine/**` 另有写手）⇒ 语义落在 `CardPresentation/Deck` 这一层。"),
    round_="B92_N9 2026-10-28",
    note_add="2026-10-28（`B92_N9`）：补督军那一支 + 接上唯一消费者（换异画）；落点从 `RuleEngine/Core/DeckRules.cs` 的 `CountOf` 改指 `DeckHas`（`DeckEditorState` 文件尾 partial）。")
out.append((fn, i, f))

# ==================================================================== 战役
f, fn, i = setrow(
    "RankedDeckSelector", "Initialize(IPlayEvent rankedEvent)", status="A",
    land="Shell/LiveOpsEventWindow.cs:549（Open）· 取槽 / 记账 / 落盘 = `EventDeckIndex` / `RememberEventDeck` / `LoadEventDeckSlots` / `SaveEventDeckSlots`（同一文件类尾那一段，散文不给行号）",
    diff_add=("**2026-10-28 B92-N9 已补跨进程持久化**（B91-L6 判定的那一项）：`SaveKeys.OursPrefix + \"EventDeckSlot\"`"
              "（`_ours_` 前缀 ⇒ `SaveKeys.IsKnown` 直接放行，**不用改键表**）—— `Open()` 读一次（`LoadEventDeckSlots`）、"
              "`RememberEventDeck` 每写一次就落一份（`SaveEventDeckSlots`；`JsonUtility` 不能序列化 Dictionary ⇒ 落成行数组）。"
              "⇒ 关掉游戏再进，每个活动仍记着自己那副（= 原版 `IPlayEvent.GetDeck/SetDeck` 的可观测结果）。"
              "🔶 键名是我们自定的：原版那一格住在**活动存档**（`EventSaveGames`）里，我们没有活动存档 ⇒ 用独立 `_ours_` key。"
              "⚠️ 可观测差异（如实记）：活动存档被清时我们这份**不会跟着清**；读档失败只出声、退回「回落全局当前」的旧行为。"),
    round_="B92_N9 2026-10-28",
    note_add="2026-10-28（`B92_N9`）：跨进程持久化补完（`_ours_EventDeckSlot`）⇒ 转 A。")
out.append((fn, i, f))

f, fn, i = setrow(
    "RankedDeckSelector", "OnSelectDeckButtonClick()", status="A",
    land="Shell/LiveOpsEventWindow.cs:1887（OpenDeckSelection）· 模式闸 = `DeckSelectionPopup.ModePasses`（判据见该段注释）",
    diff_add=("**2026-10-28 B92-N9 已补齐 `currentDeck` 那一格**：`DeckSelectionPopup.Create` 新增第 5 个实参 "
              "`currentDeckMode`（= 原版 `DeckSelectionContext.Deck.GameMode`），由 `LiveOpsEventWindow.CurrentDeckModeForSelection()` 喂"
              "（预组那副用它自己的 `gameMode`）。模式闸改照 `DeckSelectionPopup___TryOpen_b__10_0.c` 全文的**二选一**"
              "（有当前卡组就**只**比它的模式，否则比 `context.GameMode`）—— ⛔ 不是「或」；改前我们只传窗口模式 ⇒ "
              "当前那副卡组的模式与窗口模式不同时会列错一批。`allowEdit` 那条按 B91-L6 已删（`DeckSelectionContext.cs:88` 第 3 实参是 `overrideDeckList=null`，原版没有那个参数）。"),
    round_="B92_N9 2026-10-28",
    note_add="2026-10-28（`B92_N9`）：补 `currentDeck`（模式闸二选一）⇒ 转 A。")
out.append((fn, i, f))

# ==================================================================== 社交
f, fn, i = setrow(
    "AlliancesController", "GetCachedGroupById(string id)", status="A",
    land="CardPresentation/Shell/SocialData.cs:586（GroupById）· 字段 = `AllianceListing.Id`（同文件第二个 partial 块，零行号漂移）",
    diff_add=("**2026-10-28 B92-N9 已落地**：`AllianceListing.Id` + `SocialData.GroupById(id)` = `_openAlliances.Find(g => g.Id == id)` —— "
              "判据 `AlliancesController__GetCachedGroupById.c` 全文（造闭包 → `groupListCache.Find(谓词)`，**纯本地缓存读、零后端**；找不到 = `List.Find` 的 null）。"
              "⚠️ **零调用者**（A 尺子①）：上游 `AlliancesManager.GetGroupById(id)` 那条同步转发在本仓没有对位（我们按名字取，见 `AllianceSearchTab.HandleDisplayAlliance` 那条出声）；"
              "且 `Id` 的写入方是**服务端 `Group.id`**（账 142/143 判 D）⇒ 缓存里恒 null、`GroupById` 恒返回 null = **原版离线时的形状**（⛔ 没拿 `Name` 冒充 id）。"),
    round_="B92_N9 2026-10-28",
    note_add="2026-10-28（`B92_N9`）：按 B92-N4 的施工单补齐（字段 + Find）⇒ 残差归零（上游零调用者，A 尺子①）；`Id` 无写入方如实记。")
out.append((fn, i, f))

f, fn, i = setrow(
    "MessageContainer.<>c__DisplayClass23_0", "_003CAnimateIn_003Eb__0()", status="A",
    land="CardPresentation/Shell/InboxWindow.cs:1414（PlaySlottingIfVisible）",
    diff_add=("**2026-10-28 B92-N9 现核：已落地（C→A）**。逐句对过 `out_il2csharp/.../MessageContainer.cs:147-154` + `decomp_full/...___AnimateIn_b__0.c`："
              "`if (IsVisibleFrom(content.RectTransform, Camera.main)) Play2D(slottingSound, FX)` —— "
              "我们的 `PlaySlottingIfVisible`：每行一颗底图 `RectTransform` 过 `RendererExtensions.IsVisibleFrom`，真可见才 `WindowSounds.PlayFx`；"
              "时机 = `AppendInterval((i+1)×delay)` 之后那一拍（`_animSoundPlayed` 闸，**每行一次**）；不可见 ⇒ 静默（原版那一支什么都不做）。"
              "🔴 cue = `CardClick 2`（`WindowSounds.BoosterCardClickCue`）：prefab `slottingSound` 的 PPtr pid `3481876391266756950` "
              "就是 `CardInBoosterPack.clickSound`（`Shell/WindowSounds.cs:19` 那张表已登记）⇒ **不是我们挑的音**。"
              "⚠️ `maxSounds = 6` 原版全类只有声明处、0 处读取（死字段）⇒ 不实现（照抄「它没被读」）。"),
    round_="B92_N9 2026-10-28",
    note_add="2026-10-28（`B92_N9`）：现核落地（`PlaySlottingIfVisible`，B92_N6 那一批落的）⇒ C→A；cue 的 pid 与 `CardInBoosterPack.clickSound` 同源。")
out.append((fn, i, f))

# ==================================================================== 个人资料/成就/排行（只追加现核结论，状态不动）
f, fn, i = setrow("AvatarBorderController", "Initialize()",
    land="Shell/AvatarTab.cs:253（Build）· 边框那一层走 `AvatarTab.BorderArtName`（散文不给行号；画的是**已装备那条**）",
    note_add="2026-10-28（`B92_N9`）：现核保持 B —— 与 `B92-N4b` 的调度台裁定一致（`goldenAvatarBorder` 是否 = 4 条活动边框之一未证 + 该 PPtr 在本地读不到）；⛔ 判据闭合前不落地「恒画金框」。")
out.append((fn, i, f))

f, fn, i = setrow("AvatarBorderController", "Initialize(PlayerInfo playerInfo)",
    land="Shell/ProfileTab.cs:608（BuildPlayerInfo）· 边框那一层同 `AvatarTab.BorderArtName`（散文不给行号）",
    note_add="2026-10-28（`B92_N9`）：同无参版（`playerInfo` 是死形参、三条未证项同一条）⇒ 保持 B。")
out.append((fn, i, f))

f, fn, i = setrow("CosmeticItemAvatarBorder", "HandleOnCollect(int rewardQuantity)",
    diff_add=("**2026-10-28 B92-N9 现核补一条硬判据**：`Resources/profile_cosmetics.json` 的 5 条边框里 **4 条 `aa=1`（`autoAssign` = 拿到即自动装备）**，"
              "且这 4 条**每条都带父活动 `pe`**（`ECReleaseScoring` / `ExpansionOrks1Scoring` / `ExpansionUM1Scoring` / `SWReleaseScoring`）；"
              "`Default` 是 `aa=0`。⇒ 原版「领到就自动装备」这一条**只对 aa=1 那 4 条成立**，而本机 4 个父活动都不在期 ⇒ **一条都没「获得」** "
              "⇒ 离线原版此刻的装备态就是 `Default`（= 我们现状，**不是**我们在偷懒）。残差仍只有**上游「获得」动作**（活动发奖链，服务器驱动）。"),
    note_add="2026-10-28（`B92_N9`）：补 `autoAssign` 那 4 条 + 父活动的读数 ⇒ 残差收敛为「上游获得事件」；到账时 1 行 `SetEquippedBorderById`。")
out.append((fn, i, f))

f, fn, i = setrow("RankedArmyBonusContainer", "Clean()",
    diff_add=("**2026-10-28 B92-N9 现核（仍 C）**：b 档要建 `Reward Event Container 1x1` 这张卡，而它的内容 = liveop 的**阵营加成数据**（服务端）；"
              "本机没有在期活动 ⇒ 建出来只能是**空卡**，而账里明令 ⛔ 别做成空卡留场（原版 `evt == null ⇒ Destroy(this)` 正说明「没数据就不该有这张卡」）"
              "⇒ 仍阻塞在数据（不是文件归属）。施工两步（建 `Dynamic Content ▸ Viewport ▸ Content` + `Clean()` 清子件/清列表）已在备注里。"),
    note_add="2026-10-28（`B92_N9`）：保持 C —— b 档的前提（有本地 liveop 加成数据）不成立；建成空卡反而与原版相反。")
out.append((fn, i, f))

f, fn, i = setrow("RankedArmyBonusContainer", "OnInitialize(LiveOpsEvent evt)",
    diff_add=("**2026-10-28 B92-N9 现核（仍 B）**：① `evt == null ⇒ Destroy(this)` 与我们「不建卡」天然一致；② 类型闸那一支同样 Destroy；"
              "③ 过了闸要按 `evt` 的每个阵营加成 `Instantiate(armyPrefab, armyContentAnchor)` —— 这一步的数据源是 liveop（服务端）⇒ "
              "b 档（本地模拟）必须先有**本地的阵营加成清单**，本机没有 ⇒ 保持 B（与上一行同一笔账）。"),
    note_add="2026-10-28（`B92_N9`）：保持 B —— 阻塞点仍是 liveop 加成数据（b 档需先定本地模拟的数据来源）。")
out.append((fn, i, f))

f, fn, i = setrow("RankedEventDivisionChangeWindowNewSystem",
    "ChangeParticleEffectAndSpriteRank(Image targetImage, ParticleSystem targetParticleSystem, Sprite rankSprite)",
    diff_add=("**2026-10-28 B92-N9 现核：方法体四步已全部解出**（`decomp_full/RankedEventDivisionChangeWindowNewSystem__ChangeParticleEffectAndSpriteRank.c` 全文）："
              "① `targetImage.sprite = rankIcon.sprite`（**不是**传进来的 `rankSprite`）② `targetImage.gameObject.SetActive(rankSprite != null)` "
              "③ `particleSystem.textureSheetAnimation.SetSprite(0, rankIcon.sprite)` ④ `particleSystem.GetComponent<Renderer>().enabled = (rankSprite != null)`。"
              "①② 与 sprite 那一半已在（`BuildDivisionImage`）；③④ 需要**真 `ParticleSystem`**（`Particles Rank Up` 那 5 个系统），"
              "而菜单层没有粒子宿主（`Shell/RankedDivisionChangeWindow.cs` 只按 prefab 名建了容器节点 + `NoteNoArt`）⇒ 归**特效线**，本条保持 B。"),
    note_add="2026-10-28（`B92_N9`）：四步判据写全（可直接照做）；仍归特效线 ⇒ 保持 B。")
out.append((fn, i, f))

f, fn, i = setrow("RankedV2Event", "Expire(Action onComplete)",
    diff_add=("**2026-10-28 B92-N9 现核（仍 B）**：b 档（本地模拟赛季切换）落地前必须先定**本地模拟的赛季数据从哪来** —— "
              "`Expire` 的接线是 `GetLeaderboard(…, 1000)` → callback → `OpenSeasonChangePopup`（上一赛季/新赛季段位与榜都来自那次拉榜），"
              "而拉榜 = P3 服务器；我们只有判定半 `ShouldShowSeasonChange`。⇒ 残差 = 赛季数据源（属 18·0 允许自己设计的服务器门类，"
              "但要先由调度台给一个「本地赛季号/段位从哪来」的口径，⛔ 不许随手编一个）。"),
    note_add="2026-10-28（`B92_N9`）：保持 B —— 需要一个「本地赛季数据源」的口径才谈得上 b 档。")
out.append((fn, i, f))

# ==================================================================== 社交（状态不动）
f, fn, i = setrow("AllianceView", "OnToggleEditClicked()",
    land="Shell/AllianceMemberTab.cs:1665（EditChrome.Apply）· 接线 = `Edit` 那颗钮的 onClick（散文不给行号）",
    note_add="2026-10-28（`B92_N9`）：现核保持 B —— 残差两项（描述可编辑 / 两个下拉的 `.value`）**都卡在服务端字段**（`description` / `blockDescriptionChange` / `privacy` / `region`）：输入框现在有了（`Shell/TextInputField.cs`），但**没有可编辑的数据**，换了也只是一格空输入框。")
out.append((fn, i, f))

f, fn, i = setrow("BattleAlliancePanel",
    "Open(string playerName, string playerTitle, int playerScore, PlayerAvatar avatar, CosmeticItemAvatarBorder avatarBorder, string allianceName, int allianceScore, GroupBadge allianceBadge)",
    land="Shell/AlliancePanelWindow.cs:870（Open）· 填充 = `Apply`（散文不给行号）",
    note_add="2026-10-28（`B92_N9`）：现核保持 B —— ②（头像立绘）与④（喂实参）都在 `Battle/BattleDriver.cs`，本波由 N10 在改（白名单外）。")
out.append((fn, i, f))

f, fn, i = setrow("ChatManager", "GetChatSoundAsset(ChatMessage chatMsgIndex, bool isPlayer)",
    note_add="2026-10-28（`B92_N9`）：现核保持 B —— 两条偏差的真身是 `CardPresentation/Core/VoiceLines.cs`（本波白名单外）；静音支今天不可达（168/168 + 29/29 全有 soundGuid）。")
out.append((fn, i, f))

# ==================================================================== 教程
f, fn, i = setrow("TutorialTipScript", "ClickContinue()",
    note_add=("2026-10-28（`B92_N9`）：现核保持 B —— 剩 `DismissTutorialTip(force:true)` + `ContinueTutorialScript()` 与输入路由都在 `Battle/BattleDriver.cs`（N10 在改）。"
              "⚠️ 调度台点名的 `PositionWillBeAtLeft` 过宽接受（我们 = 「左半场」）不在本文件，在 `RuleEngine/Core/TutorialScript.cs`（`a.TargetSlot < BoardSpec.WarlordSlot`）"
              "⇒ 本批 `RuleEngine/**` 全禁，**未改**；原版判据已解出（报告 `报告/B92_N9.md` §5，可直接照做）。"))
out.append((fn, i, f))

# ==================================================================== 锻造
f, fn, i = setrow("BoosterPackVisualItem", "Initialize(string localizedArmyText, BoosterPackOpenWindow boosterPackOpenWindow)",
    land="Shell/BoosterPackOpenWindow.cs:433（Build）· ⛔ **这不是等价物**：原版 `BoosterPackVisualItem` 是一件独立 3D prefab（Mesh + 世界空间 TMP + 相机抖），我们**没有**对应物；本行只指「拆包声」那一跳的落点",
    diff_add=("**2026-10-28 B92-N9 现核（仍 B）**：缺口两条未变 —— ① **3D 世界物件 + 可抖相机**（`Renderer` 网格 + 世界空间 `TextMeshPro`×2 + `CAMERA_SHAKE` 预设），"
              "本工程菜单层是「世界空间 quad + 渲染队列」、**没有可抖相机**（现有等价物 = 抖卡容器，见本文件头那一节）；"
              "② 包面图 **11 张待导**（16 源 / 5 已导，归调度台的 `工具/import_original_art.py`）。⇒ 仍 B。"
              "⛔ 不拿 quad 冒充（账已写死）。拆包声那一跳已接、并如实标注为「我们挑的落点」（原版在 `Initialize` 末段）。"),
    note_add="2026-10-28（`B92_N9`）：保持 B —— 3D 物件 + 包面图两条外部依赖未变。")
out.append((fn, i, f))

# ---- 写盘：UTF-8 with BOM + CRLF ----
lines = ["\t".join(COLS)]
for fn, i, f in out:
    lines.append("\t".join(f))
data = ("\ufeff" + "\r\n".join(lines) + "\r\n").encode("utf-8")
io.open(OUT, "wb").write(data)
print("wrote %s · %d rows" % (OUT, len(out)))
