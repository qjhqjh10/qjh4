# 交件 · 双语③ **波 0b3** —— `Core/Loc.cs` 的**最后一批「补与清」**

> 独占 **1 个文件**（`Core/Loc.cs`）· 未跑 Unity · 未碰 git（只 `git diff --numstat` 只读）。
> 秒级类型检查 `TMPDIR=/tmp/wf_0b3 bash d:/4/Unity/工具/typecheck.sh` = **运行时 0 / 编辑器 0**（改前改后各一次）。

## ① 结论

| 组 | 做了什么 | 条数 |
|---|---|---|
| **(a)** 联机页 8 颗写死英文钮 → 建键 | 8 条**自拟**键（原版两张表 0 命中） | +8 |
| **(b)** 图像页 / 音频页页标题 → 建键 | 2 条**原版键**（`Settings/{Graphics,Media}/Title`，查到就用它） | +2 |
| **(c)** 清死键 | `MenuDeck/Share/{ChatUnavailable,PlatformShare}` | −2 |
| **(d)** 清零调用点空键 | `MainMenu/Settings/ExitGame/{Confirm,Ok}` | −2 |
| **(e)** 订正值 | `MainMenu/Ranked/OfflineNote` 中英**两列**去掉已过期的「（将来做 P2P）」 | 1 |
| **(f)** 修踩 `Loc.HasCjk` 的英文列 | **8 条全修完**（6 全角空格 · 1 真汉字 · 1 既有） | 8 |

- `Loc.EntryCount`：**377 → 383**（现读 = 单行 `new Entry("…","…")` 解析 **382** + 1 条多行拼串的 `MainMenu/PurchasePremium/Description` —— 那种写法解析器吃不下、**手工核过它的 EN 是纯英文**）。
- `git diff --numstat -- Core/Loc.cs` = **`635 0`**（**纯增、零删**）。⚠️ 基线是 **`HEAD`（1115 行）**、比本件早两波 ⇒ 那 635 里 **569 行是波 0b2 的**，**本件净增 +66 行**（**1684 → 1750 行** · **198,958 B**；改前 1684 与简报吻合）。
- 行尾：**纯 LF**（`CRLF=0` / `LF=1750` / 无 BOM）—— 全程 `Edit`，无 `sed -i`、无整篇重写。
- ⛔ 没碰：调用点 · `Editor/*` · `Shell/*` · `Net/*` · `Deck/*` · `Battle/*` · 两张正本 · 任何**既有** `资料/*.md` · git。

## ② 新键表（10 条 · 键名 | EN | ZH | 来源 | 依据）

| 键名 | EN | ZH | 来源 | 依据 |
|---|---|---|---|---|
| `Settings/Online/RoleHost` | `Host` | 主机 | **自拟** | 调用点 `Shell/SettingsWindow.cs:2430`（EN 逐字） |
| `Settings/Online/RoleClient` | `Client` | 客机 | **自拟** | `:2431` |
| `Settings/Online/TestPublicIp` | `Test Public IP` | 测外网 | **自拟** | `:2470` |
| `Settings/Online/IpLabel` | `IP address` | IP 地址 | **自拟** | `:2531` |
| `Settings/Online/PasswordLabel` | `Password` | 密码 | **自拟** | `:2533` |
| `Settings/Online/Refresh` | `Refresh` | 刷新 | **自拟** | `:2558` |
| `Settings/Online/Save` | `Save` | 保存 | **自拟** | `:2562` |
| `Settings/Online/CheckConnection` | `Check Connection` | 检查连接 | **自拟** | `:2580` |
| `Settings/Graphics/Title` | `Graphics` | 图像 | **原版键**（EN 是我们挑的） | 表① `…_5215720994356428710` / `…_8895938149081907110` |
| `Settings/Media/Title` | `Audio` | 音频 | **原版键**（EN 是我们挑的） | 表① `…_-3908738614376169562` / `…_5584938879264653222` |

- (a) 的 **ZH 取词与表内既有文案对齐**（逐条现读核对）：`主机`=`…/HostReady` · `客机`=`…/St/ClientLobby` · `保存`=`…/St/ConnRefused` · `刷新`=`…/HowToConnect/VirtualNicYes` · `检查连接`=`…/StatusNoSession`。
- (b) **EN 列拿不到原版原文（如实记，不是猜）**：那 4 颗 TMP 的 `m_text` 就是**西班牙语**（`Gráficos` / `Multimedia` —— 本包 prefab 被本地化成西语，英文在**远端 I2 表**）⇒ EN 取**我们界面今天写死印的那串**（`SettingsWindow.cs:1626` `PageTitle(page,"Graphics")` / `:2349` `…"Audio"`）——**这条是我们挑的**；ZH 同样自拟（`资料/待办判据_卡面卡池与双语.md` §23）。
- 📌 **留给调度台的拍板项**：若要照原版那一族，`Settings/Media/Title` 应写 `Media`（依据 = 键名后缀 + GO 名 `Media Tab` + 西语译文 `Multimedia`）—— 那会**改英文档可见字样**（页签/页标题 `Audio`→`Media`），并让 `交件_换语言刷新链.md` §⑤ 那条「两语档下都仍是 `Graphics`/`Audio`」的反向断言失效 ⇒ 本件按「**英文档零变化**」取 `Audio`。

## ③ 删掉的键（4 条 · 零调用点的 grep 证据）

核法（**只读**）：脚本遍历 `Unity/MyGame/Assets/**/*.cs`（含 `Editor/`）逐行逐字搜键名，输出 `文件:行号`。

| 删的键 | 证据（**行号 = 改前**） | 理由 |
|---|---|---|
| `MenuDeck/Share/ChatUnavailable` | 全仓命中 **1 处 = 本文件自己的定义**（旧 `Loc.cs:1237`） | `A1040` 分享收口后成死键（`交件_分享卡组收口.md` §⑦·1） |
| `MenuDeck/Share/PlatformShare` | 同上（旧 `:1239`） | 死键，且**中英兜底都是错的**（写「原版是平台分享」，判据 `decomp_full/DeckInfoPopup__ShareDeck.c` = **写剪贴板**） |
| `MainMenu/Settings/ExitGame/Confirm` | **1 处 = 定义**（旧 `:983`） | `A1026` 三处调用点已全改指 `Demo/MainMenu/ExitGame`（波 1b §③） |
| `MainMenu/Settings/ExitGame/Ok` | 同上（旧 `:984`） | 改指 `Demo/MainMenu/ExitButton`；且与那两条**值逐字相同** = 同值空键 |

- 改动方式：**只删键行**，原地留一条**更正痕迹**注释（哪天删的/为什么/怎么核的），并把 `⑫·C P3` 节的条数
  **`18 条` → `16 条`**（清单与数字只留一处 · 铁律 6）。删后复验：键**定义**行 `grep -c` = **0**；解析器 `dup keys: []`。

## ④ (e)(f) 逐条改了什么（改前 → 改后）

**(e) `MainMenu/Ranked/OfflineNote`**（只改**值**，⛔ 调用点一个字没动；中文档**有意**变一点）
- ZH：`…本地版没有联机（将来做 P2P），所以…` → `…本地版没有联机，所以…`
- EN：`…no online play (P2P later), so…` → `…no online play, so…`

**(f) 8 条英文列**（判据 = `Loc.HasCjk` 的区间，见本文件 **`:1686`**：含 `0x3000–0x303F`）

| # | 键 | 改前（EN） | 改后 |
|---|---|---|---|
| 1 | `Settings/Online/PublicAddress/Mismatch` | `\n　 the outside…` | `\n  the outside…` |
| 2 | `Settings/Online/ClickAgain` | `"　—— tap again…"` | `" —— tap again…"` |
| 3 | `Settings/Online/VirtualNic` | `\n　 If the other…` | `\n  If the other…` |
| 4 | `…/HowToConnect/VirtualLan` | `both sides\n　(Tailscale…` | `both sides\n  (Tailscale…` |
| 5 | `…/HowToConnect/PublicDirect` | 3 处 `\n　` | 3 处 `\n  ` |
| 6 | `…/HowToConnect/DontUseTestSite` | 3 处 `\n　` | 3 处 `\n  ` |
| 7 | `MenuDeck/Error/PrebuiltMissing` | `…run \`python 工具/gen_prebuilt_decks.py\` to regenerate it` | `…run the prebuilt-deck generator (\`python gen_prebuilt_decks.py\`, in the project's tools folder) to regenerate it` |
| 8 | `Battle/Log/TurnPrefix`（**既有**） | EN `Round {0}　{1}` | EN `Round {0} {1}` |

- 1–6 = **全角空格 U+3000 → 半角 1:1**（等价、安全）；**ZH 列一个字没动**（中文本来就该有全角）。
- 7 = 路径**没删**，只把中文目录名改写成英文描述（`工具`→`the project's tools folder`），脚本名照旧。
- 8 = 🔴 **只读判完 ⇒ 结论「可改、且今天零行为影响」**：① 它**真会**被判红（`0x3000` 在区间内，脚本实测）；
  ② 全仓 `*.cs` grep `Battle/Log/TurnPrefix` ⇒ **0 个调用点**（只有定义行）⇒ 改 EN 不动任何界面。
- 三条都留了更正痕迹注释；**ZH 列一律不动**。复验：脚本按**同一区间**重扫全表 ⇒ **`EN with cjk: 0`**（含新加 10 条）。

## ⑤ 查不到 / 自拟的清单（含**搜过的词**）

- **(a) 8 颗钮 = 原版没有 ⇒ 键名/EN/ZH 全自拟**（EN 逐字抄调用点，⛔ 没编）。
  **表①** `d:/2/新解包资源/assets_full/**/MonoBehaviour/*.json`：按 `"mTerm": "<字面量>"` 全树扫
  `Host` · `Client` · `Test Public IP` · `IP address` · `Password` · `Refresh` · `Save` · `Check Connection`
  ⇒ **0 命中**；再按前缀扫 `Settings/(Online|Audio|Network)[^"]*` ⇒ **0 命中**；另把
  `bundle_menus_assets_all` **全包 308 条 `mTerm` 逐条列出**核过（确实没有这一族）。
  **表②** `d:/2/tools/il2cpp_out/stringliteral.json`（**26,507** 条）逐词扫同一批 ⇒ **0 命中**。
  **正对照（证明扫描有效、不是无效否定）**：同法扫 `Demo/MainMenu`=**7** · `MainMenu/Settings`=**4**
  · `Battle/HUD`=**8** · `MenuDeck/`=**33** · 前缀 `Settings/`=**10**。
- **(b) 两条 = 原版键查到了**（非自拟）：键名用原版 `mTerm`；**只有 EN 列拿不到**（西班牙语问题，见 §②）。
  同族旁证（表②）：`Settings/Graphics/`（`0x4244CC8`）· `Settings/Media/WindowMode/{Fullscreen,Windowed}`
  （`0x4244DC8` / `0x4244EC8`）。
- ⛔ **没拿 `Settings/Online/Title` 顶替**那 8 颗钮（那是页标题、已被 `PageTitle` 消费 —— 简报点名禁止）。

## ⑥ 没做到的

1. **(b) 的 EN 拿不到「原版逐字英文」** —— 本地 prefab 是西语、英文在**远端 I2 表**（判据在远端）。本件取
   「我们界面今天印的那串」并如实标注；要改成 `Media` 需调度台拍板（§② 末）。
2. **两处引用这 8 颗钮的正文，本件没跟着改**（有意，见 §⑦·1）。
3. **没跑任何 Unity 自检**（⛔ 红线）。`Core/Loc.cs` 是共用件 ⇒ **跑哪几条由调度台在同步点按铁律 12 定**；
   本件只保证「秒级类型检查 0 错 + 表结构自洽（无重键 / 无 EN 含 CJK）」。

## ⑦ 顺手发现（⛔ 除第 3 条已如实记录外，一处都没顺手改）

1. 🔴 **接线那一波要一起改的两处「引用正文」**（本件故意没改 —— **今天改了会先失配**，因为按钮**还没接线、仍是英文**）：
   - `Settings/Online/StatusNoSession` ZH：`（会话还没建 —— 点一下 Host 的保存，或 Client 的检查连接）`
     ⇒ 接线后应跟随新 ZH 变成 `点一下主机的保存，或客机的检查连接`。
   - `Settings/Online/HowToConnect/DontUseTestSite` ZH：`或者点【Test Public IP】把两者摆在一起对照。`
     ⇒ 接线后应变成 `点【测外网】`（它的 **EN 列本来就是 `[Test Public IP]`，不用改**）。
2. ℹ️ **`…StatusNoSession` 的 ZH 今天已与界面不一致**（它说「保存 / 检查连接」，按钮却印 `Save` / `Check Connection`）
   —— 不是本件造成的（波 0b 按「调用点原话」抄的值），接线后自然对齐。
3. ⚠️ **本件改了 (e) 的 ZH** —— ⛔ 不是「悄悄改中文档」，是**有意订正**（波 1b §⑥·2 点名要求 · 铁律 5）；
   除这一处外，**全表 ZH 一个字没动**（(b) 的两条是**新增**）。
4. ℹ️ **`Settings/Media/WindowMode` 是原版真键、表里还没有**（`SettingsWindow.cs:2412` 自己出声说「没建」）
   ⇒ 那一件将来要做时**键名直接用原版那条**（表① `…_7322287963451522982.json`；两个选项值的字面量见
   表② `0x4244DC8` / `0x4244EC8`）。
5. ℹ️ **`Loc.HasCjk` 目前只被 `Editor/DeckScene.cs`（`:4952/:4959/:4976/:5012`，断 `detail`）用**，
   **没有一条断言遍历全表断 `!HasCjk(en)`** —— P6 §⑤ 那条「灭自证 C1」还只是**建议**。本件按那条口径把
   8 条**先修干净**，它将来一跑就是零红。
