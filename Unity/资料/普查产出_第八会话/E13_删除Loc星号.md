# E13 · 删掉 `Core/Loc.cs` 里那 27 条键的 `**`（`A1091` 施工单）

> 白名单内**实际只写了 1 个文件**：`MyGame/Assets/CardPresentation/Core/Loc.cs`
> （`git diff --numstat` = **+76 / −59**，改动全部落在 **52 条物理行**上，其余 1962 行一个字没动）。
> ⛔ 没跑 Unity（红线）· ✅ 秒级类型检查跑了 1 次（`TMPDIR=/tmp/wf_e13`，**0 / 0**）·
> ⛔ 没碰 git 的写操作（只 `diff --numstat` / `status` 只读）。
> 行尾：改前 `CRLF 0 / LF 2014` → 改后 `CRLF 0 / LF 2014`（**未翻；总行数一行未增未减**）。
> 施工单来源：`资料/普查产出_第八会话/E7_弹窗星号与toast.md` §②·C（本件照它逐条落，未自拟）。

---

## ① 结论

| 项 | 结论 |
|---|---|
| **27 条键 / 146 处 `**`** | ✅ **全部删净**，与 E7 施工单**逐条吻合**（27 条 · 26 条两列都有 + 1 条只有 ZH · `**` 共 146 处）。**我现读出来的也是 27 / 146 ⇒ 没有「停手」的条件。** |
| **删的到底是哪些字符** | **只有 `**` 本身**（146 处 = 292 字节）。独立复核见 ②·3：把**两份文件各自**的所有 `**` 剥掉之后，**字节完全相同** ⇒ 这一次的唯一变化就是「删 `**`」，**没有任何别的字符被顺手动过**。 |
| **没动什么** | 键名（429 条键，**名字与顺序逐条相同**）· `new Entry(中文, 英文)` 形参顺序与结构 · **注释里的 `**`（强调符号）一处未动**（改前 2084 处 → 改后 1938 处，**差额恰好 146**，剩下的全是注释）· ⛔ 没加 `<b>` · ⛔ 没发明 markdown→TMP 转换口 · ⛔ 没改任何一个别的字。 |
| **行尾** | 纯 LF 保住了（`CRLF = 0`）。 |
| **编不编得过** | 秒级类型检查 **0 / 0**。 |
| **做法** | `D:/4/_tmp_e13_edit.py`（**工程目录之外**）· **二进制读**（`io.open(p,'rb')`）· **按 key 名定位**那一行 `new Entry(`（⛔ 脚本里**一个行号都没写死**）· **只在 `new Entry(...)` 的字符串实参里**删 · 先把新内容算进变量、校验通过再 `write` · ⛔ 没用 `sed -i` · 改前留了一份副本 `D:/4/_tmp_e13_Loc.cs.bak`。 |

---

## ② 证据

### ②·1 计数与施工单逐条一致（改前实测，非照抄）

扫描器：**按 token 切**（跳过注释与字符串转义），逐个 `new Entry(...)` 取它的字符串实参，
只在**实参里**数 `**`；再用「实参里有没有汉字」分 ZH / EN 列（`E7` 用的是同一套口径）。

**实测：429 个 `Entry` 中，实参里带 `**` 的 = 27 个 · `**` 合计 = 146 处。**
下面这张表的每一格都与 `E7` §②·C 那张表**逐字相同**（含它的 `2+2` / `4+6` / `8+8` 写法）：

| 键 | 改后行号（现读） | 计数 |
|---|---|---|
| `MenuDeck/Error/EffectOnlyCard` | 1021 | ZH 2 + EN 2 = 4 |
| `MainMenu/PurchasePremium/Description` | 1157 | **ZH 2 + EN 0** = 2 |
| `MenuDeck/Error/ImportNotPersisted` | 1244 | ZH 2 + EN 2 = 4 |
| `Settings/General/RedeemCodeUnavailable` | 1269 | ZH 2 + EN 2 = 4 |
| `Settings/Online/PublicAddress/Mismatch` | 1285 | ZH 4 + EN 6 = 10 |
| `Settings/Online/PublicAddress/BothOk` | 1287 | ZH 2 + EN 2 = 4 |
| `Settings/Online/HowToConnect/PublicDirect` | 1310 | ZH 6 + EN 6 = 12 |
| `Settings/Online/HowToConnect/DontUseTestSite` | 1312 | ZH 8 + EN 8 = 16 |
| `Settings/Online/HowToConnect/PublicV6No` | 1319 | ZH 6 + EN 6 = 12 |
| `Settings/Online/HowToConnect/UpnpNote` | 1325 | ZH 2 + EN 2 = 4 |
| `Settings/Online/MatchCancelled` | 1389 | ZH 2 + EN 2 = 4 |
| `MenuDeck/Error/WrongGameModeDeck` | 1398 | ZH 2 + EN 2 = 4 |
| `MenuDeck/Error/NoUsablePrebuilt` | 1417 | ZH 2 + EN 2 = 4 |
| `Settings/Online/Lobby/PlayedVsBot` | 1626 | ZH 2 + EN 2 = 4 |
| `Settings/Online/Lobby/StartAfterCancel` | 1630 | ZH 2 + EN 2 = 4 |
| `Settings/Online/Lobby/MissedCancel` | 1632 | ZH 2 + EN 2 = 4 |
| `Settings/Online/Lobby/PeerCancelled` | 1634 | ZH 2 + EN 2 = 4 |
| `Settings/Online/Lobby/ModeMismatch` | 1636 | ZH 2 + EN 2 = 4 |
| `Settings/Online/Cancel/WhyStarted` | 1647 | ZH 2 + EN 2 = 4 |
| `Settings/Online/Echo/NoEcho` | 1656 | ZH 2 + EN 4 = 6 |
| `Settings/Online/Upnp/NoResponse` | 1672 | ZH 2 + EN 2 = 4 |
| `Settings/Online/Upnp/NoService` | 1674 | ZH 2 + EN 2 = 4 |
| `Settings/Online/Upnp/PortTaken` | 1676 | ZH 2 + EN 2 = 4 |
| `Settings/Online/Upnp/Rejected` | 1680 | ZH 2 + EN 2 = 4 |
| `Settings/Online/Upnp/Cgnat` | 1682 | ZH 4 + EN 4 = 8 |
| `Settings/Online/Upnp/Ok` | 1684 | ZH 2 + EN 2 = 4 |
| `Settings/General/LangHasNoTable` | 1750 | ZH 2 + EN 2 = 4 |
| **合计** | | **146** |

**73 个强调对**（146 ÷ 2）· **每个实参里的 `**` 个数都是偶数**（无落单星号，故「配对」这个概念在这些串上是良定义的）。

### ②·2 简报要求的那四处验证 —— 逐条读数

| # | 检查 | 读数 | 判定 |
|---|---|---|---|
| ① | **行尾**：`b.count(b'\r\n')` vs `b.count(b'\n')` | 改前 **`0 / 2014`** → 改后 **`0 / 2014`** | ✅ 纯 LF，**且行数一行未变** |
| ② | `git diff --numstat` | 改前（别的写手的基线）`+23 / −6` → 改后 **`+76 / −59`**（**我这一笔 = +53 / −53**） | ✅ **很小**（52 条物理行，占 2014 行的 2.6%）；⛔ **不是整篇重写** |
| ③ | `**` 计数 | **总处数** 改前 **2084** → 改后 **1938**（**Δ = 146** ✅ 精确）<br>**`new Entry(` 行里的 `**`**：见下面「⚠️ 与简报预期不符」 | ✅ 处数精确 == 146 |
| ④ | **27 条键仍在表里、键名一个没变** | `Entry` 总数 **429 → 429**；把两份文件的键名按序抽出来逐条比 ⇒ **`identical order+names: True`**；无重复键 | ✅ 键名/顺序/条目结构全未动（`Loc.HasEntry` 语义不受影响） |

🔴 **⚠️ ③ 有一条与简报的预期不符，如实报（我没有硬凑）**：

- 简报写「`grep -c '\*\*' Loc.cs` 的新读数**应当 = 旧读数 − 146**」。**实测不成立**：
  `grep -c` 数的是**行**不是**处** —— 改前 **812** 行含 `**` → 改后 **760** 行（**Δ = 52，不是 146**）。
  原因：那 27 条词条的值**跨 52 条物理行**，每行被删掉的处数各不相同（1~2 处），**处数合计才是 146**。
  ⇒ **正确的等价检查是「处数」**：`grep -o '\*\*' | wc -l` = **2084 → 1938（Δ 146）** ✅。
  （简报里那个「812 是含注释的数」的提醒是对的；只是「−146」那半句把**处**当成了**行**。）
- 简报写「单独再数一次『`new Entry` 行里的 `**`』应当 == 0」。**这条也不能按字面做**：
  `grep 'new Entry(' | grep -c '\*\*'` 改后仍是 **43** —— 因为**同一行的行尾注释里本来就有 `**`**
  （如 `// TMP 原文；中文**自拟**`、`// TMP 原文（**带冒号**）`），**那些是一个字都不许动的**。
  ⇒ **按简报的【真实意图】做的检查是**：**落在 `new Entry(...)` 实参（字符串字面量）里的 `**` == 0** ✅
  （重跑扫描器：`entries with ** in args: 0`，全文件实参内 `**` 合计 **0**）。
  **旁证**：处数差额恰好 == 146 ⇒ **删掉的 146 处全部来自实参，注释里的 1938 处一处未动。**

### ②·3 独立复核 —— 「只删了 `**`」不靠自我报告

**做法**：把**改前（副本）**与**改后**两份文件**各自**的所有 `**` 全部剥掉，再比字节。

```
backup **  : 2084      new **  : 1938      delta: 146
after stripping ALL ** from both, files identical: True
backup bytes - new bytes = 292            (= 146 x 2)
```

⇒ **两份文件在「去掉 `**` 之后」逐字节相同** ⇒ 这次改动的**全部内容**就是「删掉 146 个 `**`」，
**没有多删一个字、没有一个字符被替换成别的、行尾/行数未变**。（`Loc.cs` 全文件 232650 → 232358 字节。）

**另一条**：` ** `（空格 + 一对星号 + 空格）这种写法在这些串里 **0 处**
⇒ 删完**不会留下双空格**；且实测「双空格个数」在每一条实参里改前改后**完全相同**。

---

## ③ 改动清单（逐条「改前 / 改后」，只列有变化的那 27 条）

> 格式：`...上下文[**原文**]上下文...  ->  ...上下文[原文]上下文...`
> 🔴 **`[` `]` 是本报告加的分隔记号，不在文件里**；`...` 是省略的未变上下文。
> `ZH#n` / `EN#n` = 该条键的**第 n 个强调对**（`ZH` = 含汉字的那个实参，`EN` = 另一个）。
> 73 对全部列出（= 146 处 `**`，与 ②·1 的合计对齐）。

### MenuDeck/Error/EffectOnlyCard  (removed=4)
  ZH#0  ..."这张是[**效果生成的卡**]（药剂/破坏/秘仪），不能放...  ->  ..."这张是[效果生成的卡]（药剂/破坏/秘仪），不能放...
  EN#0  ..."This is an [**effect-generated card**] (Elixir / Sab...  ->  ..."This is an [effect-generated card] (Elixir / Sab...

### MainMenu/PurchasePremium/Description  (removed=2)
  ZH#0  ...你有多个高级战役，这份奖励会[**全部**]给你！\n"...  ->  ...你有多个高级战役，这份奖励会[全部]给你！\n"...

### MenuDeck/Error/ImportNotPersisted  (removed=4)
  ZH#0  ...导入失败：卡组串读出来了，但[**没写进存档**]——{0}（重启就没了）"...  ->  ...导入失败：卡组串读出来了，但[没写进存档]——{0}（重启就没了）"...
  EN#0  ...was read, but [**was not written to the save**] — {0} (it is ...  ->  ...was read, but [was not written to the save] — {0} (it is ...

### Settings/General/RedeemCodeUnavailable  (removed=4)
  ZH#0  ...这个项目没有那台服务器 ⇒ [**这里兑不了**]。"...  ->  ...这个项目没有那台服务器 ⇒ [这里兑不了]。"...
  EN#0  ...such server ⇒ [**redeeming does not work here**]."...  ->  ...such server ⇒ [redeeming does not work here]."...

### Settings/Online/PublicAddress/Mismatch  (removed=10)
  ZH#0  ..."⚠️ [**两个不一样**] ⇒ 上面那个 IPv6 是...  ->  ..."⚠️ [两个不一样] ⇒ 上面那个 IPv6 是...
  ZH#1  ...）：\n　 外面看得到它，但[**别人连不到你这台机器**] ⇒ IPv6 直连这条路走...  ->  ...）：\n　 外面看得到它，但[别人连不到你这台机器] ⇒ IPv6 直连这条路走...
  EN#0  ..."⚠️ [**The two differ**] ⇒ that IPv6 a...  ->  ..."⚠️ [The two differ] ⇒ that IPv6 a...
  EN#1  ... above is the [**[router's]**] (it is doing ...  ->  ... above is the [[router's]] (it is doing ...
  EN#2  ...n see it, but [**nobody can reach this machine**] ⇒ direct IPv6...  ->  ...n see it, but [nobody can reach this machine] ⇒ direct IPv6...

### Settings/Online/PublicAddress/BothOk  (removed=4)
  ZH#0  ...IPv6 直连」这条路可行（[**要求对面也有**]）。"...  ->  ...IPv6 直连」这条路可行（[要求对面也有]）。"...
  EN#0  ...v6 is viable ([**the other side needs one too**])."...  ->  ...v6 is viable ([the other side needs one too])."...

### Settings/Online/HowToConnect/PublicDirect  (removed=12)
  ZH#0  ...直连 ⇒ 主机点【保存】时会[**自动向路由器要一个端口**]（UPnP）；\n　成没成会...  ->  ...直连 ⇒ 主机点【保存】时会[自动向路由器要一个端口]（UPnP）；\n　成没成会...
  ZH#1  ...成没成会弹一条告诉你 —— [**没成**]就是路由器不支持 / 关着 ...  ->  ...成没成会弹一条告诉你 —— [没成]就是路由器不支持 / 关着 ...
  ZH#2  ...到主机这台机器。\n　（主机[**自己**]有公网 IPv6 的话填 I...  ->  ...到主机这台机器。\n　（主机[自己]有公网 IPv6 的话填 I...
  EN#0  ...ses [Save] it [**asks the router for a port automatically**] (UPnP);\n  yo...  ->  ...ses [Save] it [asks the router for a port automatically] (UPnP);\n  yo...
  EN#1  ... either way — [**no port**] means the rou...  ->  ... either way — [no port] means the rou...
  EN#2  ... (If the host [**itself**] has a public ...  ->  ... (If the host [itself] has a public ...

### Settings/Online/HowToConnect/DontUseTestSite  (removed=16)
  ZH#0  ..."⚠️ [**别拿「IPv6 测试网站」当判据**]：那里显示的是【**外网看到...  ->  ..."⚠️ [别拿「IPv6 测试网站」当判据]：那里显示的是【**外网看到...
  ZH#1  ...」当判据**：那里显示的是【[**外网看到的**]地址】，\n　它有可能是**...  ->  ...」当判据**：那里显示的是【[外网看到的]地址】，\n　它有可能是**...
  ZH#2  ...**地址】，\n　它有可能是[**路由器的**]（有些路由器在做 IPv6 ...  ->  ...**地址】，\n　它有可能是[路由器的]（有些路由器在做 IPv6 ...
  ZH#3  ...AT）⇒ 外面看得到，\n　[**但别人连不到你这台机器**]。本机到底能不能被连上，看下...  ->  ...AT）⇒ 外面看得到，\n　[但别人连不到你这台机器]。本机到底能不能被连上，看下...
  EN#0  ..."⚠️ [**Do not use an \"IPv6 test site\" as the criterion**]: it shows the...  ->  ..."⚠️ [Do not use an \"IPv6 test site\" as the criterion]: it shows the...
  EN#1  ... address the [[**outside world sees**]],\n  which ma...  ->  ... address the [[outside world sees]],\n  which ma...
  EN#2  ...ch may be the [**router's**] (some routers...  ->  ...ch may be the [router's] (some routers...
  EN#3  ...m outside,\n  [**yet nobody can reach this machine**]. Whether this...  ->  ...m outside,\n  [yet nobody can reach this machine]. Whether this...

### Settings/Online/HowToConnect/PublicV6No  (removed=12)
  ZH#0  ..."· 公网 IPv6：[**没有**] ⇒ 本机网卡上没有全局 I...  ->  ..."· 公网 IPv6：[没有] ⇒ 本机网卡上没有全局 I...
  ZH#1  ...「测试网站看得到 IPv6」[**不矛盾**] —— 那个多半是路由器的）...  ->  ...「测试网站看得到 IPv6」[不矛盾] —— 那个多半是路由器的）...
  ZH#2  ...\n  ⇒ 第 ③ 条只能靠[**端口映射**]，或者走 ① ②"...  ->  ...\n  ⇒ 第 ③ 条只能靠[端口映射]，或者走 ① ②"...
  EN#0  ... Public IPv6: [**none**] ⇒ this machin...  ->  ... Public IPv6: [none] ⇒ this machin...
  EN#1  ...(⚠️ this does [**not contradict**] \"a test site...  ->  ...(⚠️ this does [not contradict] \"a test site...
  EN#2  ...te ③ only via [**port forwarding**], or take ① ②"...  ->  ...te ③ only via [port forwarding], or take ① ②"...

### Settings/Online/HowToConnect/UpnpNote  (removed=4)
  ZH#0  ...机点【保存】时自动试 —— [**成没成都会弹一条说出来**]"...  ->  ...机点【保存】时自动试 —— [成没成都会弹一条说出来]"...
  EN#0  ...sses [Save] — [**a pop-up tells you either way**]"...  ->  ...sses [Save] — [a pop-up tells you either way]"...

### Settings/Online/MatchCancelled  (removed=4)
  ZH#0  ...匹配 —— 对面会收到通知，[**双方都没有开局**]。\n想再打一次：两边各自重...  ->  ...匹配 —— 对面会收到通知，[双方都没有开局]。\n想再打一次：两边各自重...
  EN#0  ... a notice and [**neither side has started**].\nTo try agai...  ->  ... a notice and [neither side has started].\nTo try agai...

### MenuDeck/Error/WrongGameModeDeck  (removed=4)
  ZH#0  ...ck` 建一副新的（照原版：[**模式在建组那一刻定，之后改不了**]）。"...  ->  ...ck` 建一副新的（照原版：[模式在建组那一刻定，之后改不了]）。"...
  EN#0  ...the original: [**the mode is fixed the moment the deck is built, it cannot be changed later**])."...  ->  ...the original: [the mode is fixed the moment the deck is built, it cannot be changed later])."...

### MenuDeck/Error/NoUsablePrebuilt  (removed=4)
  ZH#0  ..."这一页一副可用的都没有（[**拼不齐的按原版口径整副不显示**]）"...  ->  ..."这一页一副可用的都没有（[拼不齐的按原版口径整副不显示]）"...
  EN#0  ...on this page ([**as in the original, decks that cannot be completed are not shown at all**])"...  ->  ...on this page ([as in the original, decks that cannot be completed are not shown at all])"...

### Settings/Online/Lobby/PlayedVsBot  (removed=4)
  ZH#0  ..."这一局[**打的是电脑，不是联机**]。\n原因：{0}。\n你在...  ->  ..."这一局[打的是电脑，不是联机]。\n原因：{0}。\n你在...
  EN#0  ..."[**This match is against the AI, not online.**]\nReason: {0}....  ->  ..."[This match is against the AI, not online.]\nReason: {0}....

### Settings/Online/Lobby/StartAfterCancel  (removed=4)
  ZH#0  ...取消之后开局了 —— 这一局[**没有进**]。\n对面那边会停在等待界面...  ->  ...取消之后开局了 —— 这一局[没有进]。\n对面那边会停在等待界面...
  EN#0  ...u cancelled — [**this one did not go through**].\nThe other s...  ->  ...u cancelled — [this one did not go through].\nThe other s...

### Settings/Online/Lobby/MissedCancel  (removed=4)
  ZH#0  ...之后才点了取消 —— 这一局[**照旧开始**]。\n对面那边会看到「已经开...  ->  ...之后才点了取消 —— 这一局[照旧开始]。\n对面那边会看到「已经开...
  EN#0  ...you started — [**this match starts anyway**].\nThey will s...  ->  ...you started — [this match starts anyway].\nThey will s...

### Settings/Online/Lobby/PeerCancelled  (removed=4)
  ZH#0  ...面取消了这一局的匹配 —— [**双方都没有开局**]，退回大厅。\n可以各自重新...  ->  ...面取消了这一局的匹配 —— [双方都没有开局]，退回大厅。\n可以各自重新...
  EN#0  ... this match — [**neither side started**], back to the ...  ->  ... this match — [neither side started], back to the ...

### Settings/Online/Lobby/ModeMismatch  (removed=4)
  ZH#0  ...局没有开成 —— 请两位换成[**同一个模式**]，再各自点一次 `Battl...  ->  ...局没有开成 —— 请两位换成[同一个模式]，再各自点一次 `Battl...
  EN#0  ...— please pick [**the same mode**] and each pres...  ->  ...— please pick [the same mode] and each pres...

### Settings/Online/Cancel/WhyStarted  (removed=4)
  ZH#0  ..."这一局[**已经开局了**]（开局包已经发出/收到）——...  ->  ..."这一局[已经开局了]（开局包已经发出/收到）——...
  EN#0  ..."[**This match has already started**] (the start pa...  ->  ..."[This match has already started] (the start pa...

### Settings/Online/Echo/NoEcho  (removed=6)
  ZH#0  ..."两个方向都没探到 —— [**可能是回显站被网络挡了**]（不是「你没有公网地址」）。...  ->  ..."两个方向都没探到 —— [可能是回显站被网络挡了]（不是「你没有公网地址」）。...
  EN#0  ... a response — [**the echo sites may be blocked by your network**] (this does **...  ->  ... a response — [the echo sites may be blocked by your network] (this does **...
  EN#1  ...** (this does [**not**] mean \"you ha...  ->  ...** (this does [not] mean \"you ha...

### Settings/Online/Upnp/NoResponse  (removed=4)
  ZH#0  ...进来：① 去路由器管理页把 [**UPnP 打开**] 再点一次【保存】；② 或者...  ->  ...进来：① 去路由器管理页把 [UPnP 打开] 再点一次【保存】；② 或者...
  EN#0  ...nnect: ① turn [**UPnP on**] in the router...  ->  ...nnect: ① turn [UPnP on] in the router...

### Settings/Online/Upnp/NoService  (removed=4)
  ZH#0  ..."路由器回应了，但它[**没有提供端口映射服务**]（不是常见的家用路由器固件）...  ->  ..."路由器回应了，但它[没有提供端口映射服务]（不是常见的家用路由器固件）...
  EN#0  ...wered, but it [**does not offer a port-mapping service**] (not a typica...  ->  ...wered, but it [does not offer a port-mapping service] (not a typica...

### Settings/Online/Upnp/PortTaken  (removed=4)
  ZH#0  ..."路由器说 [**{0} 这个端口上已经有别的映射了**] ⇒ 换一个端口再来（或者去...  ->  ..."路由器说 [{0} 这个端口上已经有别的映射了] ⇒ 换一个端口再来（或者去...
  EN#0  ...e router says [**port {0} already has another mapping**] ⇒ try a diffe...  ->  ...e router says [port {0} already has another mapping] ⇒ try a diffe...

### Settings/Online/Upnp/Rejected  (removed=4)
  ZH#0  ..."路由器[**拒绝了**]端口映射请求（错误码 {0}...  ->  ..."路由器[拒绝了]端口映射请求（错误码 {0}...
  EN#0  ..."The router [**refused**] the port-mapp...  ->  ..."The router [refused] the port-mapp...

### Settings/Online/Upnp/Cgnat  (removed=8)
  ZH#0  ..."✅ 端口映射要到了，[**但你这台大概率在「大内网」(CGNAT) 里**] ——\n路由器自己的外网地...  ->  ..."✅ 端口映射要到了，[但你这台大概率在「大内网」(CGNAT) 里] ——\n路由器自己的外网地...
  ZH#1  ...器自己的外网地址是 {0}（[**私网段**]）⇒ 外面照样连不进来。\n...  ->  ...器自己的外网地址是 {0}（[私网段]）⇒ 外面照样连不进来。\n...
  EN#0  ... was granted, [**but this machine is very likely behind a carrier-grade NAT (CGNAT)**] —\nthe router...  ->  ... was granted, [but this machine is very likely behind a carrier-grade NAT (CGNAT)] —\nthe router...
  EN#1  ...dress is {0} ([**a private range**]) ⇒ outside co...  ->  ...dress is {0} ([a private range]) ⇒ outside co...

### Settings/Online/Upnp/Ok  (removed=4)
  ZH#0  ...口（TCP）{1} —— 把[**外网地址 + 端口**]给朋友就能连进来。"...  ->  ...口（TCP）{1} —— 把[外网地址 + 端口]给朋友就能连进来。"...
  EN#0  ...1} — give the [**public address + port**] to a friend a...  ->  ...1} — give the [public address + port] to a friend a...

### Settings/General/LangHasNoTable  (removed=4)
  ZH#0  ...没有这一套文案 ⇒ 界面文字[**回退英文**]"...  ->  ...没有这一套文案 ⇒ 界面文字[回退英文]"...
  EN#0  ...⇒ the UI text [**falls back to English**]"...  ->  ...⇒ the UI text [falls back to English]"...

---

## ④ 没查清的部分

1. ⚠️ **删掉之后「强调」就没了** —— 那 27 条词条在屏幕上将变成**和正文完全一样的字**。
   这是**已经裁过的口径**（E7 ②·C：⛔ 不加 `<b>`、⛔ 不做 markdown→TMP 转换口），
   所以**本件没做任何替代**；但它**不是原版的做法**（原版压根没有这些串）。
   ⇒ **仍然没查清的是**：原版这几处提示**到底靠什么强调**（本地 `assets_full` 全树 0 命中 `**`、
   `stringliteral.json` 里 9 条 `**` 全是引擎/日志串、原版那条链 `PopUpGameWindow__SetText` 逐句读下来
   只有「查词条 + 原文赋给 TMP」，**没有任何富文本处理**）。
   ⛔ **没拿猜测填空**：本地的原版资源推不出「原版怎么强调」，要判只能进实况（`真Play待验清单` 那一类）。
2. ⚠️ **那 27 条的英文列里有 26 条也是我们自己写的**（`E7` ②·B 已定：原版不带、也不转）。
   ⇒ 本件**只做了「去星号」**，**没有**去核这 27 条的**英文/中文文案本身是否忠实** ——
   那是另一笔账（其中 `Settings/Online/*` 那些「怎么联机」的长文**本来就是本项目自拟的帮助文案**，
   原版客户端里就不存在）。**本件不动它，如实标在这里。**
3. ⚠️ **`Loc.cs` 此刻仍有别的写手在动**（`git status`：本件开跑前它的 numstat 就已是 `+23 / −6`）。
   ⇒ 我按 **key 名**定位、**没有写死行号**，且改完立刻复核了「429 条键、名字与顺序逐条相同」⇒ 未撞车。
   但**行号一定会继续漂** —— ③ 那张表里的行号只能当**当时读数**。

---

## ⑤ 顺手发现的东西（**一处都没改**；`Loc.cs` 之外一个字没碰**）

1. 🔴 **`grep -c` 数行、`grep -o | wc -l` 数处 —— 这批活的口径必须用后者**（②·2 ③）。
   简报里「新读数 = 旧读数 − 146」这句建议以后改成「**处数**减 146」，否则下一个会话会以为**没做完**。
2. ⚠️ **同一行行尾注释里带 `**` 的行有 43 条**（`grep 'new Entry(' | grep -c '\*\*'`）——
   这是**本仓注释的一种既定写法**（`**自拟**` / `**带冒号**` / `**另一串**`）。
   ⇒ 以后写「`new Entry` 行里不许有 `**`」这类断言**会恒红**；**判据只能落在字符串实参上**（本件这条坑值得记）。
3. ⚠️ **`Core/Loc.cs` 里 `**` 总数 1938 处、`Entry` 只有 429 条** ⇒ **绝大部分 `**` 在注释里**。
   本文件的注释密度很高（表格每条都带出处注释），**任何「整篇扫 `**`」的批处理都要先排除注释**。
4. ⚠️ **`Settings/Online/Echo/NoEcho` 的 EN 里有一个 `**not**`**（`this does **not** mean …`）——
   它是**句中的否定词**被强调。删掉后语义不变（`this does not mean …`），但它说明：
   **这些星号不是「成对包一个短语」那么简单，是嵌在句法里的** ⇒ 若要「局部还原强调」，
   **不能用「找短语」的办法**，得逐条读句子。
5. ⚠️ **`Settings/Online/Upnp/Ok` 的串里本来就含一个字面 `+`**（`把**外网地址 + 端口**给朋友`）。
   ⇒ 删星号后那段是 `外网地址 + 端口` —— **那个 `+` 是文案自己的字符，不是被删出来的**。
   （提醒下一个读的人别把它当成「删星号留下的连接符」。）
6. ⚠️ **本件的临时件都在工程目录之外**（`D:/4/_tmp_e13_*.py` / `_tmp_e13_Loc.cs.bak` /
   `_tmp_e13_frag.txt` / `_tmp_e13_report.json` / `_tmp_e13_beforeafter.txt`），**没有一个进 `MyGame/`**。
   副本 `D:/4/_tmp_e13_Loc.cs.bak` 建议**留到本批收口之后**再删（那是「改前」唯一的一份快照）。
7. ⚠️ **本件对自检的影响面：0 条。** 只动了**词条值里的两个字符**，没有改任何 `.cs` 的行为、
   没有加/删/改键名 ⇒ `Loc.HasEntry` 的语义与全部宿主都不受影响。
   （`E7` 那张「需要哪几条自检覆盖」表里，第一笔本就写着**一条都不用跑** —— 本件即那一笔。）

