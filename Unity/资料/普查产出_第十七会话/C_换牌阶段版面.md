# C-换牌 · A1502 换牌阶段两处版面

> 执行代理 `C-换牌`，2026-10-19（第十七会话）。
> 判据优先级照简报：原版实拍 ⇒ 原版全树/dump ⇒ 解包字段；**全量反编译**是第一权威。
> 白名单内动手：`Battle/BattleDriver.cs`（**只动提示行 / 名牌那几段**）。`Battle/MulliganPanel.cs` **一个字没动**。
> ⚠️ **没跑 Unity**（简报禁）⇒ 本报告里没有「跑过自检」的结论；类型检查读数见末节。

---

## 结论（一句话）

- **① 中央提示文字压在换牌面板/卡上** —— **根因已查清**：那一条（`_hintLabel`）**钉在屏幕正中 (0.5, 0.507)**，
  而**屏幕正中一直站着我/敌两张督军卡**（换牌阶段手牌被提到顶端、棋盘正中只剩督军）⇒ 文字被卡从中间盖掉一截。
  **原版没有这一行**（我们自己的注释就写着）⇒ 已按「**换牌阶段整行不画**」处理（那一格让给原版自己的
  `MulliganText`）；**其余阶段仍会被督军卡盖住**，那是「这一行该摆哪儿」的位置问题、原版无判据，**未自行挑新位置**。
- **② 两处空黑框** —— **根因已查清且已修**：那两块是 **`Avatar Item Small/Image Container/Border` 那张
  `Player Profile Border` 图的不透明黑心**（图本身就长这样），而**压在它上面的头像立绘那一层
  `Image Container/Image` 我们根本没建**。原版出厂态那件是 `m_Enabled = 0` + `m_Sprite = null`，
  我们当年据此判「只摆框、不摆立绘」—— **那只说了出厂态**：运行期 `BattleHud.SetProfileInfo` →
  `SetProfileAvatar` 会填图并打开它，而且 **null 时回落默认头像 ⇒ 原版这一层永远不空**。
  已按原版字段把两侧立绘补出来（我 = 玩家选的头像；bot = 按本局种子确定性挑一张）。

---

## 根因 + 证据

### ① 中央提示文字被督军卡盖住

| 项 | 读数 / 出处 |
|---|---|
| 提示行的建立点 | `Battle/BattleDriver.cs:12268` —— `_hintLabel = Hud(root, "", 0.5f, 0.507f, 3, …)`。注释自己写着「放在**两行棋盘中间那条缝**里（玩家行上沿 0.483 / 对手行下沿 0.530）」 |
| 换牌阶段写了什么 | `:3184` `SetHint(_deckNotice)`（**盖掉** `:4411` 在 `OpenMulligan` 里写的那句「换牌中：点牌上的「换」…」）⇒ 屏幕上就是「本局用你编的《复仇者之刃》—— …」 |
| **实拍读数** | `d:/4/_tmp_view/ui_1019/17_battle.png`（2560×1440 ⇒ 设计像素 = 屏幕像素 ÷ 1.3333）—— 文字左半可见、**中段被督军卡盖死**；文字中心实测在画布 **(≈960, ≈532)** |
| 督军卡的位置 | 同一张实拍：两张督军卡（带底座）就在屏幕正中 x≈960–1050 一带；用户原版实拍 `资料/原版参照图/用户实拍_1017/战斗截图参考.png` 里那两张也同位置 ⇒ **正中一直是督军，不是「换牌阶段才有」** |
| 原版有没有这一行 | **没有**。`SetHint` 的 doc（`:8258`）逐字写着「⚠️ **原版没有这一行**（原版全卡种都能上场，没什么要交代的）—— 这是我们加的」 |
| 原版换牌阶段显示什么 | 原版自己的提示行 `MulliganText`（我们照原版摆在顶端：`MulliganPanel.cs:175` `PromptCx/PromptCy = 967, 106.5`，文案走词条 `Battle/Mulligan/Instructions`） |
| 同族先例 | `:13786` `_waitBanner.SetVisible(!InMulligan && …)` —— 本项目本来就按 `InMulligan` 关 HUD 件 |

⇒ **判定**：这是**我们自加件**（位置也是我们挑的）；它「压在卡上」不是渲染队列问题，是**位置与督军卡必然重叠**。
本轮按「**不与原版冲突的最小改法**」= 换牌阶段整行不画（那一格本来就是原版 `MulliganText` 的）。

### ② 两处空黑框 = 头像立绘那一层没建

**（a）那两块到底是什么 —— 像素量测（`17_battle.png`）**

| 量 | 左上（敌） | 左下（我） |
|---|---|---|
| **连续纯黑 `(0,0,0)` 横run** | row 99：x 22..131 ⇒ **110 屏幕 px = 82.5 设计 px** | row 1290–1320：x 20..129 ⇒ **110 屏幕 px = 82.5 设计 px** |
| **连续纯黑 `(0,0,0)` 纵run** | col 78：y 35..157 ⇒ **123 屏幕 px = 92.3 设计 px** | col 80：y 1283..1406 ⇒ **124 屏幕 px = 93.0 设计 px** |
| 黑心中心（画布） | (57.4, 72.0) | (55.9, 1008.0) |
| 代码里那一件的中心 | `(58.451, 74.080)` | `(57.151, 1009.533)` |

- **那张图自己就是黑的**：`Resources/Art/ui/Player_Profile_Border.png`（256×286，与 `Art/原版/去重资源/` 那份逐字节同形）
  —— **精确 `(0,0,0,255)` 像素 35,457 个**，bbox x15..246 / y14..274，**最宽连续 run = 191/256 = 74.6%**。
  反算：110 屏幕 px ÷ 0.746 ÷ 1.3333 = **110.6 设计 px** ≈ 代码里那个 `110.90930610790465`（差 0.3%）
  ⇒ **我们渲出来的就是 `Border` 那一格，位置与尺寸都对着**（中心实测与代码差 1.0–1.5 设计 px）。
- ⚠️ **顺带订正一处自己走过的弯路**（留痕，别重犯）：一开始拿 `Player_Profile_Border` 的 **bbox**
  （237/256 = 92.6%）当黑心占比，算出「实绘 89.1×98.8 ⇒ 整条 HUD 的 px→世界换算里含 1/1.25」——
  **那是错的**。bbox 把边缘的暗金属一起算进去了；**该用的是「最宽连续 run」191/256**。
  改用 run 之后 110.6 ≈ 110.909，**没有那个 0.8**（`ImageQuad.RebuildMesh` 就是 `WorldH` 直给，无名目系数）。

**（b）我们为什么没建那一层**

- `bundle_scenes_scenes_battlearena1` 直读（我方 / 敌方两套）：
  - `Image Container/Image`（我 `RectTransform_2835` / MB `4621`；敌 `RT_3488` / MB `5067`）：
    **`m_Enabled = 0`** · **`m_Sprite = {m_FileID:0, m_PathID:0}`（null）** · `m_PreserveAspect = 1` · `m_Color` 白
  - 它的 RT：`anchor (0,0)-(1,1)`、`sizeDelta (0,0)`、`m_AnchoredPosition (0, 2.70)`、**`m_LocalScale = 2.0`**
  - 同级的 `Border`（MB `4606` / `5134`）：`m_Enabled = 1` · `m_Sprite = Player Profile Border` · `m_LocalScale = 1.25`
- `Battle/BattleDriver.cs:12480-12483`（改前的原文）逐字写着
  「⚠️ 原版场景态里 `avatarImage`（头像立绘）是 **m_Enabled=0** —— 所以**只摆框、不摆立绘**」⇒ 那两块黑框就是这么来的。

**（c）运行期那一层会被填起来 —— 全量反编译逐条（`d:/2/tools/decomp_full/`）**

| 判据文件 | 说了什么 |
|---|---|
| `BattleHud__SetProfileInfo.c` | `matchData+0x40` → `PlayerAvatarDataManager.GetItemWithId` → **`PlayerProfileUIController.SetProfileAvatar`**（另 `+0x48` → `SetProfileAvatarBorder`，`+0x50` → 称号） |
| `PlayerProfileUIController__SetProfileAvatar.c` | 传进来是 null ⇒ **回落 `PlayerAvatarDataManager.get_DefaultAvatarItem()`**，再 null 才 return ⇒ **原版这一格永远有图** |
| `AvatarDisplay__ChangeAvatar.c` | `avatarImage.sprite = CosmeticItem.get_Image(item)`；紧接着 `Behaviour.set_enabled(avatarImage, sprite != null)` ⇒ **有图就开**（`m_Enabled=0` 只是出厂态） |
| `MatchData__SetBotDeckRandomCosmetics.c` | **单机对局的 bot 也有头像**：`AIBotsConfig.GetRandomAvatar` 写进 `matchData+0x40`（同族还有 `+0x50` 称号 / `+0x58` 卡背） |
| 实拍旁证 | `资料/原版参照图/用户实拍_1017/战斗截图参考.png` —— 两侧都画着头像（左上一张络腮胡、左下一张） |

**（d）几何：原版序列化字段推导（两套都直读）**

| 层 | 原版字段 | 推导出的实绘（画布 px） |
|---|---|---|
| `Border`（已在画） | 容器 155.64×99.125（= 父 155.64×**136.493** 减 `sizeDelta.y −37.367`）· 自己 `scale 1.25` · `PA=1` · 图 256×286 | 155.64×99.125 →×1.25→ 194.55×123.906 →内接→ **110.909 × 123.906** |
| `Image`（本轮补） | 与容器同框 · 自己 `scale 2.0` · `PA=1` · 图 512×512 | 155.64×99.125 →×2→ 311.28×198.25 →内接→ **198.250 × 198.250** |
| 中心差（`Image` − `Border`） | `m_AnchoredPosition`：我 Border(-1.00,-2.00) / Image(0,2.70)；敌 Border(0,-2.30) / Image(0,2.70) | 容器局部 **(+1.0, +14.6125)**（我）· **(0, +14.9125)**（敌）；父链 `PlayerName`/`EnemyName` 的 `m_LocalScale = 0.8` ⇒ 画布 **(+0.8, +11.690)**（我）· **(0, +11.930)**（敌） |

⇒ 立绘实绘高 = **Border 的 1.6 倍**（198.25 / 123.906），**这正是下面两个常量写成
`99.1251923339398 * 2` 而不是「Border ×2」的原因**。

---

## 改动清单（全部在 `Battle/BattleDriver.cs`）

| 行 | 改了什么 | 判据出处 |
|---|---|---|
| `:8280` | `SetHint` 开头加 `if (InMulligan) { _hintLabel.SetText(""); return; }` + 一段注释 | 见 ①（原版无此件；原版换牌阶段的提示行是 `MulliganText`；同族先例 `_waitBanner`） |
| `:12357` | 新增字段 `ImageQuad _hudAvatarMe, _hudAvatarFoe;` + 三个「已画的是哪一张」的 int 缓存 | 与 `Shell/TopBar` 的 `RefreshTopAvatarIfChanged` 同一手法 |
| `:12386-12397` | `HudAbs` 拆成 `HudAbs`（走 `CardArt.Ui`）+ **新 `HudAbsTex`（直接吃 `Texture2D`）**，换算只有一处 | 头像图在 `Art/avatars/`，不在 `Art/ui/`（`Core/CardArt.cs:601 Cosmetics`） |
| `:12400-12457` | 新增那一整段 doc 注释（判据 / 几何 / 画谁的头像）+ 常量 `HudAvatarSize / W / H / Z` | 上面 (c)(d) 两节 |
| `:12459-12500` | 新增 `AvatarTex()` · `BotAvatarIndex()` · `RefreshHudAvatars()` · 两个只读口 `HudAvatarMeArt / HudAvatarFoeArt` | 同上 |
| `:12623-12636` | `BuildHudExtras` 里建两层立绘（`AvatarImage_Me` / `AvatarImage_Foe`）+ 记下标 | 上面 (d) 的实绘尺寸与中心 |
| `:12669-12678` | **`ChatButton` 的 z：`HudImageZ`（0.3）→ `HudImageZ - 0.065f`（0.235）** | 原版同级兄弟序 `PlayerInfo.m_Children = [PlayerName(头像挂在它里面), Milestones, ChatButton]`（`runtime_ui_dump_drive_0912.tsv:118,130,133`，深度优先序 = 兄弟序）⇒ UGUI 后者在上 |
| `:13819` | `UpdateHud` 里调一次 `RefreshHudAvatars()` | 与 `:13813` 那两行名牌文字同一段 |

- **行尾**：全程 CRLF（改前 14,599 行 / 14,756 行，`\r\n == \n == \r` 复验通过）；`git diff --numstat` = **+167 / −2**（无整篇翻转）。
- **没能改的**：`MulliganPanel.cs` 一个字没动（本来就不需要）。
- 两次落盘脚本留在 `_tmp_view/_edit_c.py` / `_edit_c2.py` / `_edit_c3.py`（可复现）。

### 两个立绘的坐标常量（可原样复核）

```
我  AvatarImage_Me : x -41.17400062251361  y  898.7175677448397   w/h 198.2503846678796   z 0.245
敌  AvatarImage_Foe: x -40.67419593501402  y  -36.97501556814864  w/h 198.2503846678796   z 0.245
（x/y 是「中心 − 半宽/半高」；`HudAbsTex` 只拿 `x + w/2`、`y + h/2` 当中心 ⇒ 与 `Border` 那两行同一个口径）
中心：我 (57.95119171142578, 997.84276007877941) · 敌 (58.45099639892577, 62.15017676579116)
```

---

## 状态 → 参数 表（铁律 5·c：一个值 ≠ 全部情况）

### ① 提示行

| 状态 | 显示 | 判据 / 出处 |
|---|---|---|
| `Ctx.MulliganOpen == true`（换牌中，含联机「已提交、等主机定序」） | **整行不画** | 本轮改的 `:8280`；这一格由原版 `MulliganText` 承担 |
| 换牌结束、正常对局 | 画 `_deckNotice`（本局用哪副牌 / N 张没上场） | `:4444`（单机）· `:294`（联机） |
| 事件提示（「换掉了 N 张」等） | 画那句 | `:4411` / `:4426` / `:8370` 一带 |
| 结算 | `SetText("")`（收干净，别从结算面板底下透出来） | `:13907`（原有） |
| ⚠️ **换牌结束之后仍会被督军卡盖住** | （未改） | 正中恒有督军卡；原版无此件 ⇒ 无判据 |

### ② 头像立绘

| 状态 | 画什么 | 出处 |
|---|---|---|
| 我方（`ProfileData.Avatars` 有清单） | `ProfileData.Avatars[ProfileData.AvatarIndex].Art` | 原版对应 `matchData+0x40`（`BattleHud__SetProfileInfo.c`） |
| 我方（清单读不到 / 图取不到） | **不画那一层**（只剩 `Border` 的黑框）+ `Debug.LogWarning`（**不静默**） | 本轮 `AvatarTex()` |
| 对方（单机 bot） | **本局种子确定性挑一张**（`BotAvatarIndex()`） | 原版是 `AIBotsConfig.GetRandomAvatar`，**允许清单在远端 LiveOps 配置里** ⇒ 见下节 |
| 对方（联机） | 同上（**未接**对联机对端的真头像） | 见下节「没查清」 |
| 玩家在档案窗改了头像 / 按 R 重开换了种子 | 下一帧 `RefreshHudAvatars()` 换贴图 | 本轮 |

### ③ 名牌那一族的 z 阶梯（本轮之后的现状；**小 z = 更靠前**）

| z | 件 | 原版同级序（`PlayerInfo.m_Children`，前 = 下） |
|---|---|---|
| 0.30 | `_myPlate` / `_enemyPlate`（`UI_Player_Frame`） | `NameBackground` |
| 0.28 | `TitleBackground_Me/Foe` | `TitleBackground` |
| **0.245** | **`AvatarImage_Me/Foe`（本轮补）** | `Avatar Item Small/Image Container/Image`（排在 `Border` 之后） |
| 0.25 | `AvatarItemSmall_Me/Foe`（`Border`）· `MatchSkullsIcon` | `Avatar Item Small/Image Container/Border` · `Milestones` |
| 0（文字） | `PlayerNameText` / `EnemyNameText` | `PlayerNameText`（最后一个） |
| **0.235** | **`ChatButton`（本轮改）** | `ChatButton` |

⚠️ 两处与「原版同级序」仍有出入、**都没动**（各有理由）：`Border` 与 `Milestones` 同 z（0.25，原来的写法）；
立绘（0.245）比 `Milestones`（0.25）更靠前，而原版骷髅在立绘之上 —— 但**两者框不重叠**
（立绘 x ≤ 157.1 / 骷髅 x ≥ 166），无可见差。

---

## 没查清的部分（⛔ 不填空）

1. **bot 头像的「允许清单」查不到。** `AIBotsConfig.GetRandomAvatar` 从
   `ConfigManager.GetConfig<AIBotsConfig>()`（LiveOps 配置，**远端**）取一份 `List`，再拿它过滤本地头像池。
   那份清单**本地没有**（91 个 bundle 里没有它；客户端连不上后端）⇒ 我们只能「从本地 469 张里挑」，
   **这是我们挑的**，不是原版行为。**还差**：远端 `AIBotsConfig`。
2. **联机局对手的头像没接。** 原版走 `matchData+0x40`（对端档案）；我们的联机握手目前不带头像 id。
   要接就得动 `Net/`（不在白名单）⇒ **停手记在这里**。**还差**：一条 wire 字段 + 两侧读写。
3. **头像「边框」（`matchData+0x48` → `SetProfileAvatarBorder`）没做。** 原版边框也是装饰品，
   而 `Resources/profile_cosmetics.json` 里**只有 avatars / titles / achievements 三类、没有边框**
   （生成器 `工具/gen_profile_cosmetics.py` 没抽它）。⚠️ 我们画的 `Player Profile Border` = **出厂默认那一张**，
   这一点与原版「没选边框时」一致，但**选过边框的玩家**在原版里不是这张。**还差**：边框那批 SO。
4. **原版「换牌阶段」的实拍没有**（用户那张 `战斗截图参考.png` 是战斗中的）。
   所以「换牌阶段正中也是那两张督军卡」是从「我们这张实拍 + 用户那张实拍里督军都在正中」推的，
   **没跑到实况**（本轮不跑 Unity、也没开原版游戏）。**还差**：原版换牌阶段的实拍 / 实况。
5. **`LeftArea` 那条父链的 `0.8` 我们整条都没乘。** 实测：我们渲出的 `Border` 宽 **110.6 设计 px**，
   而原版（把 `EnemyName`/`PlayerName` 的 `m_LocalScale = 0.8` 算进去）应是 **88.73**（= 110.909 × 0.8）
   ⇒ **整块 LeftArea HUD 大 25%**。这正是 `A1254` 记在名牌上的那一条（`NameBackground` 真高 101.04 不是 126.3），
   **本轮新证据说明同一个 0.8 也压在 `AvatarItemSmall` 上**。⛔ 本轮**没动**它（改动面大、且 `Border`/铭牌都在
   `A1254` 名下）⇒ **将来修 A1254 时，本件两个立绘要跟着一起乘 0.8**（已在代码注释里写死这句）。
   ⚠️ 立绘的**相对**关系（= 框的 1.6 倍、中心 +0.8/−11.69）**与这个 0.8 无关**，所以先补不算白补。
6. **立绘中心的 ±3 px 不确定度**：偏移我按「容器局部 × 父链 0.8」取 `(+0.8, −11.690)`；
   若父链那一级最终被证明**不**该乘（= 代码现状的口径），则应是 `(+1.0, −14.613)`（差 ≤3 px）。
   两侧都远小于贴图那圈透明边距，**肉眼判不出**；等 A1254 那笔一起定。

---

## 我建议加的断言（逐条；**本轮没写** —— `Editor/` 是主对话独占）

> 宿主建议：`Editor/BattleScene.cs`（它已经有 HUD 那一族的口径）。以下都能用现成的只读口 `HudAvatarMeArt / HudAvatarFoeArt`。

1. **两侧立绘都建出来了、而且都有图**（灭「只剩一块黑」）：
   `HudAvatarMeArt != null && HudAvatarFoeArt != null`，且**两张都不是空串**。
   ⚠️ 这条要**结构上不可能同时满足**的写法才挡得住「两边一起改回黑框」——例如再断一句
   `HudAvatarMeArt.StartsWith("Avatar_") && HudAvatarFoeArt.StartsWith("Avatar_")`（图名规约是 `Avatar_<阵营>_<名>`）。
2. **尺寸 = 框的 1.6 倍**（⛔ **别钉绝对值** —— 绝对值会跟着 A1254 那个 0.8 走）：
   `|AvatarImage_Me.WorldH − 1.6 × AvatarItemSmall_Me.WorldH| < 1e-3`，敌侧同式。
   （1.6 的来历：`198.25 / 123.906`，两边都是原版 PA 内接后的实绘值。）
3. **中心 = 框中心 + 偏移**（容差 0.05 px）：
   我 `(Δx, Δy) = (+0.8, −11.690)` · 敌 `(0, −11.930)`（画布 px）。
4. **队列（灭自证）**：`avatarZ < frameZ && avatarZ < chatZ` —— 一行同时钉住「立绘在框之上」和
   「`ChatButton` 在立绘之上」。**反了就是一块黑**（`Player Profile Border` 的中心是不透明黑）。
5. **换牌阶段提示行必须是空的**：`InMulligan == true` 时 `HintText == ""`；
   紧接着把 `MulliganOpen` 落掉（`RuleCore.EndMulligan`）再断 `HintText != ""`（**恢复**那一半也要断，否则「永远空着」也能过）。
6. **bot 头像跟种子走**：同一 `seed` 调两次 `Begin` ⇒ `HudAvatarFoeArt` 相同；换一个 seed ⇒
   **不是恒等于同一张**（防「写死一张」当完成）。
7. **玩家改头像 ⇒ 战斗里跟着换**：改 `ProfileData.AvatarIndex` → 调 `RefreshHudAvatars()`（或跑一帧 `UpdateHud`）→
   `HudAvatarMeArt` 变成新那张。
8. **不许动引擎随机流**：断言 `Ctx.Rng` 在同 seed 下与「补这张头像之前」产生的动作序列逐字相同
   （即：换头像不该让对局走向变）。判据 = 现成的终局局面哈希那条自证法。

---

## 顺手发现（看见了但**没改**）

1. **`LeftArea` 整条父链的 0.8 没乘**（见「没查清」5）——**面积比 A1254 记的更大**：
   不止名牌，`AvatarItemSmall`/`TitleBackground`/`ChatButton`/骷髅那一族全在这条链上。
   本轮**新证据**：`AvatarItemSmall` 实渲 110.6 设计 px，应为 88.73。
2. **换牌阶段顶端那句「选择要在首轮替换的牌」压在起手牌上**（同一张实拍）：
   `MulliganPanel` 的 `_prompt` 在 `PromptCy = 106.5`，而起手牌这时就摆在屏幕顶端 ⇒ 两者叠在一起。
   ⚠️ 这一件**可能与原版一致**（原版 `MulliganText` 的中心也是 (967,106.5)、且原版这一阶段本来就把牌摆在
   上半屏）⇒ 我没有判据说它错，**只记不改**。
3. **名牌第二行的内容是「阵营名」、原版是「称号」**：我们 `_myText = 玩家名 + "   " + CardText.Faction(...)`
   （`:13813`），原版是 `PlayerNameText`（名字）+ `TitleBackground/EnemyTitle`（称号，橙色）。
   实拍对照：原版「Miketueres / 天空战士」「Kirio Asuna / 冷漠的哨兵」是**两行**，我们挤成一行。
   （这一条已在 `:13646` 那一段注释里有据地记过「没有名字数据源」的历史，**本轮不动**。）
4. **`Border` 与 `Milestones` 同 z（都是 `HudImageZ - 0.05f`）** —— 本仓自己的规矩是「同 z 谁压谁不确定」。
   本轮因为立绘要插在 `Border` 之上才注意到；两者框不重叠，先不动。
5. **`_hintLabel` 在**其它**阶段也被督军卡盖着**（见 ①的最后一格）：这一行要么换位置、要么常态隐藏，
   但**原版没有这一件、没有判据** ⇒ 我没自行挑新位置。**建议主对话/用户拍一次「它到底该在哪儿」。**

---

## 类型检查读数

```
TMPDIR=/tmp/wf_c bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---  运行时错误数: 0
--- 编辑器程序集 ---  编辑器错误数: 0
```
（跑过两次：第一次在全部改动之后，第二次在 `_edit_c3.py` 订正尺寸常量之后 —— 两次都是 0 / 0。）

**没跑任何 Unity 自检宿主**（简报禁止；`BattleScene.Run` 由主对话在同步点跑）。
⇒ 本轮的改动**没有经过任何断言**，两处版面的最终验收需要：① 主对话在同步点跑 `BattleScene.Run`；
② 真机看一眼（换牌阶段与战斗阶段各一张截图）——我自己没法跑 Unity。
