# B89-E22b · `Editor/MainMenuScene.cs` 两条「成对改动」+ 账的行号/符号复核

> 会话：第八十九会话 第 1 波 · 代理 **E22**（接调度台追加授权）· 2026-10-28
> 授权范围：`Editor/MainMenuScene.cs`（新增）＋ 上一批那 5 个 `Shell/*.cs`
> 账补丁 = `资料/完全复刻/账/_parts/B89_E22b.tsv`（11 列表头 · **5 行** · 无 tab/换行/裸 CR；key 与主表逐字比对 **0 缺失**）
> **`typecheck` = 运行时 0 / 编辑器 0**（`TMPDIR=/tmp/wf_B89E22b`）· ⛔ 没跑 Unity · 没动 git

---

## 一、改动清单（3 个文件）

| 文件 | 改了什么 | 行数变化 |
|---|---|---|
| `Editor/MainMenuScene.cs` | ① `:14480` 那条「`extra_info` 不建」断言**翻案**；② `:10759` / `:10955` / `:10963` 三条「`Name` = 夹具串」的断言改成 `ProfileData.PlayerName`；③ 顺带把 `:14469` 那句「`extra_info` 灭」的注释就地订正 | **+41 / −9** |
| `Shell/AllianceMemberTab.cs` | `GeoT.ExtraInfo` 由 `null` 改成 **`1080.54,209.81→1534.23,269.81`**（`extra_info` = 原版 `regionText` **真的建出来**，否则第 ① 条断言无处可断） | 净 −4（三条注释改写成两条 + 一行赋值） |
| `Shell/LeaderboardRow.cs` | 新增 `EffectiveName(d)`（`IsSelf ? ProfileData.PlayerName : d.Name`），`Name` 那一格与 `OnRowClicked` 的窗键/`ViewedPlayer` **同源** | **+22 / −4** |

行尾：三个文件**全是 LF、零裸 CR**（二进制核过）⇒ 没有整篇翻行尾。

---

## 二、① `:14480`：`extra_info`「不建」→「建了 + 开着」

### 改之前（原文，逐字）

```csharp
                            CheckTrue(FindChild(gd, "extra_info") == null,
                                      "★ `extra_info`（`English / Private` 那行）**不建** —— 它在 act T 那份里是 "
                                      + "**act F**（普查 `:280`），只在未入盟支那一棵上亮（普查 `:187`）"
                                      + "⇒ 建在这棵树上 = **取错实例**（改之前这里画的正是它）");
```

### 为什么错（判据）

- 「act T 那份里它是 act F」说的是 **prefab 的序列化出厂态**，不是运行期；
- 原版 `AllianceView.Draw` 里 **`regionText.gameObject.SetActive(true)` 是无条件的**
  （`out_cpp2il_dev_ilspy/AllianceView.cs:459-460`；`regionText.text = text2` 在 `:382`），
  进编辑态（`OnToggleEditClicked`）才关、退编辑态（`OnEditCancel`）又开；
- ⇒ **运行期在两棵树上都亮**。旧断言编码的是「我们本地没有那份判据」时的妥协，**前提已被推翻**。

### 改之后（现行代码，`Editor/MainMenuScene.cs:14488-14511`）

1. `CheckTrue(xinfoT != null && xinfoT.GetComponent<Label>() != null, "★ `extra_info`（= 原版 `regionText`）**建了**（act T 普查 `:280`，`1080.54,209.81→1534.23,269.81`）")`
2. `CheckTrue(xinfoT.gameObject.activeSelf, "★ **开着**（原版 `Draw` 那条 `regionText.SetActive(true)` 是无条件的）")` —— 带**改坏法**说明
3. **右缘**：`CheckNear(zx2, 1534.23f, 1f, …)`（量 **TMP 渲出来那块字的右缘**，走 `TmpRenderedRect`）
4. **纵向中心**：`CheckNear(MenuCheck.DesignPx(xinfoT).y, 239.81f, 1f, …)`（= `209.81,269.81` 的中线）
5. **文字**：`CheckText(TextOf(xinfoT), "English / Private", …)`

⚠️ **量法不是 `CheckAtWorld`**：这颗是 **`Right` 对齐**的（`MenuDraw.AlignRight` 会把节点挪到右边）⇒ 拿节点中心断会
「恒等自证」甚至假红 —— 与 act F 那一棵的断言（`:14557` 起）**同一条口径**（那条当年就是因为这个被 `A666` 订正过）。
⇒ 我这里**照它抄的量法**，只是把期望值换成 act T 的 `1534.23 / 239.81`。

### 配套的代码改动（同批，`Shell/AllianceMemberTab.cs:1320`）

```csharp
            ExtraInfo = new PxRect(1080.54f, 209.81f, 1534.23f, 269.81f), // :280
```

- 原来这里是 `ExtraInfo = null`（注释写「act T 里它是 F」）。改完 `Build` 里那段 `if (g.ExtraInfo.HasValue)` 就会
  在 **已入盟那一棵**也建出 `extra_info`，且**排在 5 个可编辑件之前**（原版 `m_Children` 顺序 =
  `[extra_info, LanguagesDropdown, Privacy Dropdown, Edit, Confirm, Cancel]`，普查 `:280-315`）✓。
- 出厂显隐由 `EditChrome.Apply(false, …)` 管 ⇒ `regionText` **开**、其余四件关 —— 与 `AllianceView.Draw` 末尾一致。

---

## 三、② `:10748`：`IsSelf = true` 夹具 vs `OwnPlayer` 取名

### 冲突是什么

- 夹具（`:10746`）：`new LeaderboardRowData { Rank = 7, Name = "Test Commander", …, IsSelf = true }`
- 原版：`nameText.text = data.OwnPlayer ? PlayerDataManager.playerName : data.Name`
  （`out_cpp2il_dev_ilspy/UIRankingRow.cs:21-49`）
- 旧断言（`:10759`）：`CheckText(TextOf(FindChild(row, "Name")), "Test Commander", "行内 `Name`")`
  ⇒ 这条**把「名字恒印 `data.Name`」钉死了**，正是账 90 剩下那一半做不了的原因。

### 我的判（怎么清）

**实现按原版走，断言跟着改**（而不是「实现迁就断言」）：

1. `Shell/LeaderboardRow.cs:340` 新增：

```csharp
        public static string EffectiveName(LeaderboardRowData d)
        {
            if (!d.IsSelf) return d.Name;
            string mine = ProfileData.PlayerName;
            return string.IsNullOrEmpty(mine) ? d.Name : mine;
        }
```

   · 左支用 **`ProfileData.PlayerName`** —— 全工程**唯一一份**（`Shell/ProfileData.cs:239`，原版是 `PlayerDataManager.playerName`；
     档案窗改名窗写的就是它）。
   · `Name` 那一格（`:246`）与 `OnRowClicked`（`:321-330`，窗键 + `ViewedPlayer`）**都走它** —— 两处同源。
     ⚠️ 原版点击**不吃名字**（`PlayerUIRankingRow.CheckProfile` 用 `playerId`），我们本地**没有 id 这一格** ⇒
     名字是唯一可用的键；两处不同源会出现「显示我的名字、点开却是 `data.Name` 的档案」（**静态错**，本工程红线）。

2. 三条断言（`Editor/MainMenuScene.cs`）：

| 行 | 改前 | 改后 |
|---|---|---|
| `:10764` | `CheckText(…, "Test Commander", "行内 `Name`")` | `CheckText(…, ProfileData.PlayerName, "行内 `Name`（`IsSelf = true` ⇒ **自己的名字**，不是夹具的 `data.Name` = `Test Commander`）")` |
| `:10957` | `pw.ViewedPlayer == "Test Commander"` | `pw.ViewedPlayer == ProfileData.PlayerName` |
| `:10965` | `… == "「Test Commander」的档案"` | `… == "「" + ProfileData.PlayerName + "」的档案"` |

   ⚠️ **灭自证**：这三条的期望值来自**数据源**（`ProfileData.PlayerName`），而夹具那一行的 `data.Name` 是
   **另一个串**（`Test Commander`）⇒ 有人把 `EffectiveName` 改回 `d.Name`，三条**立刻红**（不是「两边一起改回去」也能绿的那种）。

3. 顺带确认**没有别的断言依赖旧口径**：全 `Editor/` 只有 `:10759`（现 `:10764`）这一处读行内 `Name` 文本；
   滚动夹具（`:10993` 的 `many`，`IsSelf = i == 1`）只断条数/位置，不吃名字 ⇒ 不受影响。

---

## 四、调度台问的第 3 件：**漂的是行号，不是符号**（已核，结论：✅ 全是行号）

方法：把**所有** `*.tsv` 里落点含 `Shell/AllianceMemberTab.cs` 的行抽出来（**27 行**），
再把落点里**反引号包住的每个标识符**拿去该文件里做整词搜索（`\b<id>\b`）。

- **缺失标识符 = 0** ⇒ 每一条落点提到的符号**都还在文件里**，`check_ledger_points --all-A --fix` 能按符号名重定位。
- 同一份检查对另外四个文件也跑了：`LeaderboardRow.cs`（8 行）· `AllianceMemberOptionsPopup.cs`（5 行）·
  `SocialData.cs`（12 行）· `AlliancesTab.cs`（0 行）—— **缺失也都是 0**。
- 漂移量（`Shell/AllianceMemberTab.cs`）：`var cf = Node(content,"Config fields",…)` **1398 → 1408**；
  `OpenMemberOptions` **1839 → 1917**；`Dropdown` 1586 → 1585；`YellowBtn` 1605 → 1604。
  其余四个文件本轮**仍是零漂移**（`LeaderboardRow.cs:137` 仍是 `Build`、`:208` 未动；
  popup 的 `458`=`Apply` / `539`=`ShowConfirmation`；`SocialData.cs` 只在类尾追加）。

### ⚠️ 两条**与漂移无关、但是真陈旧**的落点（早于我这两批，`--fix` 也修不好，需人工看一眼）

以下三行落点指向的行**早就不是那个符号**了（本轮实测）：

| 台账 | key | 落点 | 现在那一行是什么 |
|---|---|---|---|
| `社交_聊天.tsv:115` | `AlliancesController.GetCachedGroupById` | `SocialData.cs:83` | `public int Privacy;` |
| `社交_聊天.tsv:124` | `AlliancesController.SetInvitationsCached` | `SocialData.cs:64` | `public string BadgeId; public int BadgeTier;`（B88-B7 加的） |
| `社交_聊天.tsv:290/292` | `ChatMessageData.GetChatText` / `ToString` | `SocialData.cs:102` | `public string Role;` |

⇒ 这四条（115 / 124 / 290 / 292）是**符号级**错位，`--fix` 只能按名字重定位、定位不到就会报「名字已消失」。
🔴 **我顺手全仓查了那三个名字**（`grep -rn "\bGetCachedGroupById\b|\bSetInvitationsCached\b|\bGetChatText\b" --include=*.cs Shell/`）
⇒ **0 命中**（连别的文件里也没有）⇒ 它们不是「被改名的落点」，而是**从来没按这几个名字实现过**
（等价物大概是内联在 `AlliancesTab` / `ChatPanel` 里的某段）。⇒ 要**人工重新指定落点**（或按「无方法落点」改等级），
`--fix` 帮不上；本轮不在我的 14 条里，**没动**，只报给你。

---

## 五、账的结果（这 5 行补丁）

| 账 | 原版方法 | 状态 | 一句话 |
|---|---|---|---|
| **90** | `AllianceUIRankingRow.Config` | **B → A** | ③ `OwnPlayer` 取名落地 + 与点击口同源 ⇒ 本条**无已知缺口** |
| **93** | `AllianceView.Draw` | B（不变） | ⑤ 完整（含 `regionText`）；①②③④ 判据空/远端/数据恒空 |
| **97 / 100** | `OnEditCancel` / `OnToggleEditClicked` | B（不变） | `regionText` 那一格已补 ⇒ 只剩 `interactable`（我们的钮是裸命中区、描述格是 `Label`）与两个假下拉的 `.value` |
| **101** | `AllianceView.Start` | B（不变） | 只剩「下拉展开面」——原版那份 `Template` 是 **Unity 内置模板**（§B·13 不建） |

**合并顺序**：`E21 → E22 → E22b`（字典序 `B89_E21 < B89_E22 < B89_E22b`，一次 `--only B89_` 的结果就是对的）。

---

## 六、⚠️ 留给真机看的一眼（不是缺陷，是**没验过**）

`ProfileData.PlayerName` 的**默认值**是本地化占位名（`Loc.T("MainMenu/Profile/DefaultPlayerName")`，
中文档可能是**汉字**），而排行榜 `Name` 那一格的字体特性是 `TmpFont.Face.LatinCardBody`。
断言读的是 TMP 的**字符串**（不受字体影响），所以**自检会绿**；但**画面上汉字能不能渲**本轮没验（没跑 Unity）。
⇒ 真机看排行榜「自己那一行」或档案窗改名后回来看一眼；若出现豆腐块，那属于「字体特性选档」问题（`TmpFont` 那一族），
与本条的取名逻辑无关。

---

## 七、我没碰的东西

⛔ 没跑 Unity · 没跑 ruleprobe · 没动 git · 没改账主表 / 正本 / 调度台日志 ·
没碰 `Battle/BattleDriver.cs`、`RuleEngine/**`、`Core/**`、`Net/**`、`Shell/**` 的其它文件、`Editor/*Check.cs` 与其它 Editor 宿主。
**改过的只有**：`Editor/MainMenuScene.cs` · `Shell/AllianceMemberTab.cs` · `Shell/LeaderboardRow.cs`
（外加产物 `_parts/B89_E22b.tsv` 与本报告）。
