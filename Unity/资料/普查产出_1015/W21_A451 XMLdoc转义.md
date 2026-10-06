# W21 · A451（XML doc 转义）· 块 X1 + X4（X2 未动，理由见 §3）

> 会话 2026-10-15 · 只改 `///` 行 · **没跑 Unity**（量尺 = `WF_DOC=1 bash 工具/typecheck.sh`，每路独立 `TMPDIR=/tmp/wf_w21`）·
> **没动 git** · **没改两张正本**。改前/改后都数过行尾（python **二进制**），**逐件 0 变化**。

## 一、摘要（6 行）

1. **我数到**：全仓 `///` 行里裸 `<`/`&` = **157 处 / 34 文件**（口径见 §2·口径）。分块：**X1 109**（`Battle/` 除 `Label.cs`，15 件）· `Label.cs` **6** · **X2 27**（`Shell/`，13 件）· **X3 6**（`CardPresentation/Editor/`，2 件）· **X4 4** · 范围外 `DeckRuntime` 3 / `ArenaBuilder` 2 · `WarpforgeVFX` 0（它那 4 条 CS1570 是**结构**问题，不是裸字符）。
   ⚠️ **两版账我都对不上**：1013 写 91（X1 69）· 1015 写 98（X1 68 · X2 20）——**我的 X1 = 109、X2 = 27**；只有 **X3 = 6 · X4 = 4 与两版都吻合**。我的口径与判据见 §2，**我用编译器验过它是够的**（改完我范围内 CS1570 = 0）。
2. **我改了**：**17 件 / 97 行 / 126 处转义**（**99×`<`→`&lt;`** · **27×`&`→`&amp;`**）+ **2 处结构性**（残缺块级标签）+ **2 处 cref**（见 §4·1）。
3. **量尺（`WF_DOC=1`，改前 → 改后，两次读数树里 error 都是 0/0）**：
   · 运行时「格式类」**581 → 400**；其中 **CS1570 265 → 84**
   · 编辑器「格式类」**40 → 31**；其中 **CS1570 23 → 14**
   · **我范围内（`Battle/` 除 `Label.cs` + `RuleEngineTest.cs`）CS1570 = 0**（裸字符扫描 + 块级平衡扫描 + 编译器，三方对得上）
   · CS1574 运行时 15 → 15（**先被我的转义顶出来 17，再被我修回 15**，见 §4·1）· CS1573 272 不变 · CS1587 28 不变 · CS1591 不变
4. **X2（`Shell/` 13 件 / 27 处）一处没动** —— 简报 ③ 明写 `Shell/*.cs` ⛔不许碰（§3·1 把矛盾与证据摆出来）。
5. **顺手发现两条「账记错了」**（§4·2）：`WarpforgeVFX/Runtime` 与 `CardPresentation/Deck/DeckRuntime.cs` **都不是「已归零」**，共 10 条 CS1570 还在，且**跟我这轮修的是同一族**（`<i>` / `<ConfirmDiscard>` 占位符）。
6. **一条经验**（建议写进下一份简报的「已知什么」）：**清完 CS1570 要再看一眼 CS1574** —— 有些 cref 是被「XML 坏掉」**掩盖**着的，XML 一修好它们就冒出来（本轮实测 `AttackSelector.cs` 两条）。

---

## 二、逐文件一张表（改动行 = 与「我开工前快照」逐行比出来的；行尾/行数逐件核过）

| # | 文件（相对 `MyGame/Assets/`） | 改动行 | `<`→`&lt;` | `&`→`&amp;` | 备注 |
|---|---|---|---|---|---|
| 1 | `CardPresentation/Battle/ScenarioBlendables.cs` | 39 | 39 | 15 | 最大一块（含 `<i>` 占位符 ×8 行） |
| 2 | `CardPresentation/Battle/BattleDriver.cs` | 20 | 20 | 6 | |
| 3 | `CardPresentation/Battle/AnimFXController.cs` | 8 | 7 | 6 | 含 `<i>` ×2 行 |
| 4 | `CardPresentation/Battle/EnvironmentApplier.cs` | 4 | 6 | 0 | 含 `<i>` ×3（同一行） |
| 5 | `CardPresentation/Battle/MulliganPanel.cs` | 4 | 5 | 0 | |
| 6 | `CardPresentation/Battle/BattleLogPanel.cs` | 4 | 6 | 0 | `<b>/<u>` 是真标签，**没动**；`<link…>` 才转义 |
| 7 | `CardPresentation/Battle/AttackSelector.cs` | 3 | 1 | 0 | **+2 处 cref**（§4·1） |
| 8 | `CardPresentation/Battle/BattleCameraSreenSize.cs` | 2 | 4 | 0 | `<< 0x20` |
| 9 | `CardPresentation/Battle/ArenaRuntimeLoader.cs` | 1 | 1 | 0 | `Warpforge_<场>` |
| 10 | `CardPresentation/Battle/CardDisplayWindow.cs` | 1 | 1 | 0 | |
| 11 | `CardPresentation/Battle/ChoosePanel.cs` | 1 | 1 | 0 | |
| 12 | `CardPresentation/Battle/EndPanel.cs` | 1 | 1 | 0 | |
| 13 | `CardPresentation/Battle/RemnantSfx.cs` | 1 | 1 | 0 | `Resources.Load<AudioClip>` |
| 14 | `CardPresentation/Battle/TargetReticle.cs` | 1 | 1 | 0 | |
| 15 | `CardPresentation/Battle/TouchInputManager.cs` | 1 | 1 | 0 | |
| 16 | `CardPresentation/Battle/ReplayStore.cs` | 1 | 0 | 0 | **结构性**：`:64` 开的 `<summary>` 没关 ⇒ `:66` 末补 `</summary>` |
| 17 | `RuleEngine/Editor/RuleEngineTest.cs`（= X4） | 5 | 4 | 0 | **结构性**：`:13678` 是裸 `///`（`:13686` 却有孤立 `</summary>`）⇒ 块首补成 `/// <summary>` |
| | **合计** | **97** | **99** | **27** | 另：2 处 cref · 2 处结构性 |

**口径（我扫描器的判据，两版账对不上就是差在这）**：只扫「行首（去空白后）是 `///`」的行；把 **`<` 后面跟的是【已知 XML doc 标签白名单】里的名字**（`summary/remarks/param/paramref/typeparam[ref]/returns/value/exception/example/code/c/list/item/para/see/seealso/b/i/em/...`）当标签**不动**，**其余一律算裸字符**；`&` 只认 `&lt; &gt; &amp; &quot; &apos; &#N;` 五种实体，其余算裸。
⚠️ **白名单里的名字也未必是标签** —— `modules.<i>.` 里的 `<i>` 是**下标占位符**，Roslyn 会照「我元素没关」报 CS1570（实测 4 个块）。⇒ **光靠字符扫描抓不到它，要配一条「块级标签平衡扫描」**（我两条都跑了，两条独立地给出同一份残余清单）。

---

## 三、没改的 / 判为不该改的（逐条 + 理由）

### 3·1 🔴 X2（`Shell/` 13 件 / 27 处 / 现存 CS1570 **65 条**）—— **简报自相矛盾，我按保守取**

- **①「范围」写**：`X1（Battle/）+ X2 + X4`；**③✅「能改」写**：`… + 账上点名的 X2/X4 目标文件`；
  **③⛔「不许碰」写**：`Battle/Label.cs · Editor/*.cs（别的写手在占）· Shell/*.cs`。
  ⇒ **同一个字段在 ③ 里既被允许又被禁止**。我按「**⛔ + 红线「不越白名单」+ 铁律 13·3（一个文件同一时刻只有一个写手）**」取，**一处没动**。
- **支持「别动」的现场证据**（mtime）：`Shell/MenuDraw.cs` **17:52** · `Shell/DailyData.cs` **17:34** · `Shell/WindowsManager.cs` **17:34** 近一小时内都被写过；而同目录还有别的写手在（`AllianceMemberTab/FriendsTab/SocialWindow/CampaignRewardWindow/CollectionData/...` 全在 `git status` 的 M 列里）。
- **代价我承担说明**：X2 **没做完**（不是「不需要做」）。**调度台要么另派一个写手，要么自己跑一行命令**（脚本在 `/d/tmp/wf_w21/fix451.py`，`--list` 是干跑、`--apply` 才写，自带字节增量 / 行尾 / 行数断言）：

```bash
S="d:/4/Unity/MyGame/Assets/CardPresentation/Shell"
D:/2/Warpforge_tools/py312/python.exe /d/tmp/wf_w21/fix451.py --apply \
 "$S/BaseOfferPopup.cs" "$S/ChatPanel.cs" "$S/CollectionData.cs" "$S/DailyData.cs" \
 "$S/DailyStreakPopup.cs" "$S/InboxWindow.cs" "$S/MainMenuRuntime.cs" "$S/MenuDraw.cs" \
 "$S/MenuScroll.cs" "$S/MissionsTab.cs" "$S/ReferralPopupWindow.cs" "$S/RewardWindow.cs" "$S/WindowsManager.cs"
```

- **逐件（我现跑，干跑=不改）**：`RewardWindow.cs` 4 处/3 行 · `WindowsManager.cs` 5/4 · `MenuDraw.cs` 4/3 · `InboxWindow.cs` 2/2 · `MissionsTab.cs` 2/1 · `DailyData.cs` 2/1 · `ReferralPopupWindow.cs` 2/2 · `BaseOfferPopup.cs` 1 · `ChatPanel.cs` 1 · `CollectionData.cs` 1 · `DailyStreakPopup.cs` 1 · `MainMenuRuntime.cs` 1 · `MenuScroll.cs` 1。
- ⚠️ 顺带更正：**1015 现核给 X2 记的「20 处」**与我扫的 **27 处**不一致（大概是逐行数 vs 逐字符数）；文件表基本一致。

### 3·2 `Battle/Label.cs`（6 处 / CS1570 **9 条**，含一个不平衡块）
- **简报点名「别的写手正占着」**（mtime **18:12**，全仓最新之一）⇒ 没动。**但它的清单留在这儿**，接手的人照着改即可：
  `:193` `<link=…>` · `:424` `&&`×4 · `:921` `<sprite name="…">`（这一行的 `<sprite>` **开 1 关 0** ⇒ 补 `</sprite>` 或把 `<` 转义，二选一，看你是想「引用一个标签字面量」（转义，我推荐）还是「真用 sprite 元素」）。

### 3·3 X3 = `CardPresentation/Editor/{BattleScene,MainMenuScene}.cs`（6 处 / CS1570 **10 条**）
- **简报说「X3 已收口」/「`Editor/*.cs` 别的写手在占」⇒ 没动。**
- 🔴 **但实测【没收口】**：`Editor/BattleScene.cs` **8 条** + `Editor/MainMenuScene.cs` **2 条** 还在（`BattleScene.cs` mtime 18:11、`MainMenuScene.cs` 18:10，都在被写）。**请调度台核一下「已收口」这条**——见 §4·2。

### 3·4 `WarpforgeArena1/Editor/ArenaBuilder.cs`（2 处 / CS1570 4 条）
- **它不在 X1..X4 任何一块里**（1013/1015 两版账的四个块都没点名它）⇒ **没动**，请调度台定归谁。

### 3·5 「扫到了但**判为不该改**」的两类（⛔ 一处没动，这是刻意的）
1. **真标签**：`<summary>` `</summary>` `<para>` `<b>` `<u>` `</u></b>` `<see cref="…"/>` `<c>` …… **转义了它们，doc 就废了**。
   ⚠️ 本仓有 590 条 `///` 行**已经在用** `&lt;`/`&amp;`（先例 → `BattleCameraSreenSize.cs:141` · `BattleDriver.cs:2462` 等），**写法照它们**。
2. **`&` 已经成实体**：`&lt;` `&gt;` `&amp;` `&quot;` `&apos;` `&#…;` —— 再转义会变成 `&amp;lt;`（**渲染出 `&lt;` 而不是 `<`**）。

### 3·6 ⛔ 我**故意没碰**的非 `///` 行
`//` 注释 / 字符串字面量 / 代码里的 `a < b`：**XML doc 根本不解析它们**，动了就是改代码。实测同族里就有这种「看着像」的（`ScenarioBlendables.cs:33/1753/1868/1955` · `AnimFXController.cs:17/96/128/136/138` 都是 `//`，**原样留着**）。

---

## 四、顺手发现（⛔ 只报不改——都不在我白名单里；但按铁律 5/5·b 请落盘）

### 4·1 🔴 我的转义**顶出来两条被掩盖的 CS1574**（已修，因为「让 doc 警告归零」）
- `CardPresentation/Battle/AttackSelector.cs:311` / `:318`：`<see cref="IconName(AttackKind)"/>` ——
  **它写在嵌套类型 `Option` 里**，简单名 `IconName` 先绑到 `Option.IconName`（**同名字段** `:320`），带参数表绑不上 ⇒ `CS1574 未能解析的 cref`。
  **改前它不报**，因为那一整块 XML 是坏的（`AttackType_button_<X>` 那个裸 `<`）⇒ Roslyn 根本不解析 cref。
  **改法**：`IconName(AttackKind)` → **`AttackSelector.IconName(AttackKind)`**（限定到外层类型）⇒ 实测 **CS1574 回到 15（= 改前）**。
- **可复用的教训**：**清完 CS1570 必须再看一眼 CS1574/CS1587** —— 否则「坏掉的那些块」里藏着的 cref 债会一次性冒出来，而这次是**我改了才看见**。
- ⚠️ **同类嫌疑（我没动，范围外）**：`Shell/ItemDrawer.cs:130/187` · `Core/Badges.cs:249` · `RuleEngine/Core/WhenEvent.cs:131` · `BattleContext.cs:670/685/987/1027` · `CardDef.cs:1996/2057` · `EffectText.cs:122/283/4101` —— 全是 CS1574，**它们潜伏多久了「要先把所在块的 XML 修好才知道」**。

### 4·2 🔴 两版账把「已归零」记错了：`WarpforgeVFX/Runtime` 与 `Deck/DeckRuntime.cs` **还开着 10 条**
- 1013 `A表现核_块5.md:273` 白纸黑字：**「⛔ 已归零、别再列（现读确认 0 命中）：`WarpforgeVFX/Runtime/*` · … · `CardPresentation/{Core,Deck,Net}`」**。
- **实测（2026-10-15，改前改后都一样）**：
  · `WarpforgeVFX/Runtime/WFModuleScreenShake.cs` **2 条**（`:221`）+ `WFSceneModuleScreenShake.cs` **2 条**（`:72`）
  · `CardPresentation/Deck/DeckRuntime.cs` **6 条**（`:2343/2344/2360/2370/2371`）
- **根因 = 跟我这轮修的是同一族**（块级平衡扫描实证）：
  · 两个 VFX 文件：**`<i>` 下标占位符**（`WFModuleScreenShake.cs:216` · `WFSceneModuleScreenShake.cs:69`）—— **正是我在 X1 里修的 `<i>` 那一族**。
  · `DeckRuntime.cs`：**`<ConfirmDiscard>` / `<TrySaveDeck>` 占位符**（`:2328` · `:2356`）—— 就是块5 判成「**合法散文**、扫描器误判」的那个例子。
    🔴 **块5 判错了**：编译器明写 `结束标记"summary"与开始标记"ConfirmDiscard"不匹配` —— **它是真缺陷**。块5 因此**整档丢了这 6 条**（它还诚实标了「那一档的数字不可信，我不给」）。
- ⇒ **建议**：这 3 件 + 3·2/3·3/3·4 一起，开一个「A451 补账」小块（同一套脚本 1 分钟跑完）。

### 4·3 量尺本身的一条使用注意（这次实测到了）
- `WF_DOC=1` 跑第 1 次时报 **`编辑器错误数: 2`、同时 `CS1591 共 0 条`** —— **这是「编译器没真跑起来」的指纹**（`CS1591` 不可能真为 0）；隔一次复跑又是 `4`，再跑就 `0/0` 了。
  根因：**别的写手正在写 `Editor/*.cs`**（简报里那条警告是真的；`Editor/{BattleScene,MainMenuScene,SettingsScene,DeckScene,ShopScene,ShellScene,CollectionScene,RewardsScene}.cs` 的 mtime 全在 18:07–18:12）。
  ⇒ **报数时必须连着「错误数」和「CS1591 条数」一起看**，两个都正常那个数才算数。

---

## 五、怎么验的（可复现）

```bash
# ① 量尺（每路独立 TMPDIR；⚠️ 别用默认 /tmp，会和别人的 wfcheck/WFCheck.dll 抢）
cd /d/4/Unity/MyGame && WF_DOC=1 TMPDIR=/tmp/wf_w21 bash d:/4/Unity/工具/typecheck.sh
# ② rsp 复制一份、在 -out: 前插一行 -doc:（= typecheck.sh 里 mkdoc 干的事），拿去全量打 CS1570
#    （typecheck.sh 只 head -20 ⇒ 数不到总数、也看不到逐文件分布）
dotnet "C:/Program Files/dotnet/sdk/8.0.425/Roslyn/bincore/csc.dll" "@/tmp/wf_w21/wf_csc_doc.rsp" -utf8output
# ③ 两个独立扫描（都在 /d/tmp/wf_w21/，只读）
D:/2/Warpforge_tools/py312/python.exe scan2.py  <根>   # 裸字符（按 doc 标签白名单）
D:/2/Warpforge_tools/py312/python.exe balance.py <根>   # 块级标签平衡
# ④ 改（逐行锚点 + 断言：字节增量 = 3×#< + 4×#&、CRLF/LF 计数不变、行数不变）
D:/2/Warpforge_tools/py312/python.exe fix451.py --list|--apply <files...>
# ⑤ 自证「改动行全是 /// 行」：逐件比「开工前快照」与现在
D:/2/Warpforge_tools/py312/python.exe final_check.py    # ⇒ 17 件 / 97 行 / 非注释行改动 0 件
```

- **三方交叉**：改完后「裸字符扫描 = 0」+「块级平衡扫描 = 0」（我范围内）+「编译器 CS1570 = 0」——**三条独立路径给出同一结论**。
- **只读了没改**：`Shell/` 13 件 · `Label.cs` · `Editor/` 2 件 · `ArenaBuilder.cs` · `Deck/` · `WarpforgeVFX/` —— 我对它们**只做了扫描**（`fix451.py --list` 是干跑，不写盘）。
- 开工前快照在 `/d/tmp/wf_w21/orig/`（17 件）——**要回退**：`cp /d/tmp/wf_w21/orig/<name>.cs <原路径>`（逐件、不影响别人的改动）。

## 六、还欠什么

1. **X2（`Shell/` 13 件 / 27 处）** —— 因为 ③ 的黑名单没动（§3·1），**要么另派、要么按那行命令跑**。
2. **`Label.cs`（6 处）· X3 的 `Editor/` 2 件（6 处）· `ArenaBuilder.cs`（2 处）** —— 都不在我白名单，清单在 §3·2 / §3·3 / §3·4。
3. **§4·2 那 3 件（`WarpforgeVFX` ×2 + `DeckRuntime`）** —— 账上写「已归零」，实测还开着 10 条 CS1570，**请调度台订正 + 决定归谁**。
4. **全仓 CS1570 还剩 98 条**（运行时 84 + 编辑器 14），**一条都不在我范围内**。
