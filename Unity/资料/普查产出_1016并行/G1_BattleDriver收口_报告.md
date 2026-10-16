# G1 · BattleDriver.cs 收口接线（收口波）

> 输入 = `资料/普查产出_1016并行/_收口台账_第四十二会话.md`（§二·2/2·2/2·6/2·7/2·8/2·12/2·13 的 `W*` 条目）
> ＋ 主对话中途**四条**追加指令（`G3` 的「逐字照抄 + 出声自检」· `G4` 的注释订正 · `FD` 的 `HudCanvas.IsRanked` 接线 · `FC` 的 `AttackSelector.SubCanvasName` 常量接线）。
> 白名单 = `Battle/BattleDriver.cs`（**只动了这一个 .cs**）+ 本报告。⛔ 没跑 Unity · 没动 git · 没改正本。
> ⚠️ **本文件里所有行号都只对「`G1` 收工那一刻」有效**（`BattleDriver.cs` 是共享文件，之后还会有人动）—— 引用前请按【方法名/常量名】现核，别照抄行号。

---

## ① 结论 —— 台账逐条

| 条目 | 结论 | 落点（现核行号） |
|---|---|---|
| **`W7`** `SkillPanel` 登记宿主 | ✅ **做了** | `:14507-14526`（`BuildHud()` 里、提示条那段之后；登记调用 `:14524`） |
| **`W8`** `:14430` 补 `Ensure` | ✅ **做了** | `:14479` |
| **`W12`** `BattleScene.cs:409` 的 `Build()` 顺序 | ✅ **按裁定不动**，代价如实记（见下） | —— |
| **`W26`**「其余阵营的召唤动画在远端包」 | ✅ **就地订正**（注释，保留更正痕） | `:12329-12343` |
| **`W30`** `AttackSelector` 登记宿主 | ✅ **做了**（**追加④ 起名字改引用常量**） | `:14620-14666`（登记调用 `:14657`） |
| **`W36`** 聊天钮 6 个的 `default` | ✅ **做了**（代价小 ⇒ 按 `G1` 简报「很小就改」办） | `:4188-4204`（调用 `:4202`） |
| **`W42`** `Unit Chat` / `ChatPopup` 登记宿主 | ✅ **两处都做了** | `:14557-14580`（Unit Chat，调用 `:14567`）· `:15294-15315`（ChatPopup，调用 `:15302`） |
| **`W43`** 五处 `EnsureSubCanvas` 缺 `Ensure` | ⚠️ **做了 3 处（不是 5 处）** —— **范围现核后缩了**，见下 | `:14479` · `:15423` · `:15475` |
| **`W50`** `AlliancePanelWindow` 登记宿主 | ✅ **做了** | `:15264-15290`（`Create(root, uguiHost: cv)` 在 `:15286`） |
| **`W55`** `grep -n "UseHost"` 不再是 0 命中 | ✅ **落实**（结果全文见 §④） | —— |
| 追加① `G3`：名字逐字照抄 + 出声自检 | ✅ **做了**（新增私有口 `EnsureRankedSubCanvas`，**5 个调用点全走它**） | `:14668-14704` |
| 追加② `G4`：语音条那段注释的错行号 + 错话 | ✅ **做了**（只改注释） | `:4228-4245` |
| 追加③ `FD`/`G1-5`：把 `HudCanvas.IsRanked` 接进读回自检 | ✅ **做了**（`FD` 备好口 · 本波接线 + 把那段 doc 改成过去时） | `:14668-14704`（`IsRanked` 调用在 `:14692`） |
| 追加④ `FC`/`G1-1`+`G1-2`：`AttackSelector.SubCanvasName` 常量就位 ⇒ 本波改回**引用常量** | ✅ **做了**（常量由 `FC` 补、本波接线 + 就地订正那段过期的注释） | `:14630-14666`（引用在 `:14655`） |

### `W43` 的**范围订正（现核）**：五处 → **三处**

台账说「五处 `EnsureSubCanvas` 之前都没有 `Ensure`」。**现核不成立**：那五处里**两处本来就有**：

| 原行号 | 画布 | 原状 |
|---|---|---|
| `:14430` | `AboveShader Canvas` | ❌ **没有** `Ensure` ⇒ 本波补（→ `:14479`） |
| `:14624` | `LoadingMenu Canvas` | ✅ **本来就有**（`Ensure` 在同文件 `:14785`，`BuildLoadingCover` 开头）；本波**没动** |
| `:15193` | `TooltipManager Canvas` | ❌ **没有** ⇒ 本波补（→ `:15423`） |
| `:15240` | `AboveShader Canvas`（加时 splash） | ❌ **没有** ⇒ 本波补（→ `:15475`） |
| `:15487` | `UI Error Message Controller Canvas` | ✅ **本来就有**（`Ensure` 紧邻上一行 `:15704`）；本波**没动** |

⇒ **真缺的那三处已全部补齐**；台账那一格的「五处」是**旧口径**（另两处是后来补进去的，登记时没跟上）。

### `W12` 的代价（按裁定**保留现状**，如实记）

`Editor/BattleScene.cs:409` 的 `driver.skillPanel.Build()` **保留**（它排在 `driver.Begin(...)`（`:410`）**之前**）⇒ 那一刻 `BuildHud` 还没跑、宿主还没登记 ⇒ **那次建的是世界档**；随后 `BuildHud` 里登记宿主（`:14507`）时 `SkillPanel._built == true` ⇒ 走 `SkillPanel.UseHost` 的 `Teardown(); Build();` 分支**再建一次画布档**。

- **代价（台账原话，如实转抄）**：`W12` 那条写的是「世界档会先建一次、登记时再重建一次（**多一条 `HudCanvas.Group` 僵尸 z 账**）」。
- ⚠️ **本波对这句的旁证（只读，未跑）**：`HudCanvas.Group` 是**按容器**的 z 账（`_zs` / `_kids` 两个 `List`，`HudCanvas.cs:377-395`），而 `SkillPanel.Teardown()`（`SkillPanel.cs:351`）只销毁自己的节点、**没有**任何「从 `Group` 出表」的回调（`HudCanvas.cs` 里 `grep "Remove("` ⇒ **0 命中**）⇒ 被销毁的那一批在 `_zs`/`_kids` 里**留位**。⇒ **方向成立**；但「具体错位多少」**本波没量**（没跑），见 §⑤。
- ⛔ 没有把登记挪到 `Build()` 之前 —— 那要改 `BattleScene.cs`（不是我的白名单）。

### `W36` 的处置依据（为什么这条算「很小」）

- **判据已查清**（本波**自己**读了反编译，不是只抄 `A8` 的结论）：`d:/2/tools/decomp_full/RawCardScript__GetChatSound.c` 的 `switch(param_2)`，`case 0..10` 各取一个字段，**取不到 ⇒ 落 `*(param_1 + 0x188)` = `defaultChatSound`**；那一段（`:94-98`）写在 `if (lVar4 != 0)` **之内**、`case` 标签**之外** ⇒ **11 个枚举值共用同一条回落**。本件用的是枚举 **5…10** ⇒ **这 6 个本来也各有那一跳**。
- **改动 = 一个数组元素**：`new[] { VoiceLines.ForChatButton[idx] }` → `new[] { VoiceLines.ForChatButton[idx], "default" }`。
- **不是「改 `TryPick` 的语义」**：`TryPick` 本来就是「按 `order` 顺序、第一个命中赢」（`VoiceLines.cs:541-564`），与 `ForCantDo`/`ForHurry`/`ForConcede`/`ForIntro`/`ForMirror` 五张表**逐字同形**。
- **对既有断言的影响 = 0（逐个核过）**：全仓 `ForChatButton` 的消费点只有两处 —— 本行，与 `Editor/BattleScene.cs:12315`（`if (pdrv.ChatClickAt(w)) { spoke++; evs.Append(VoiceLines.ForChatButton[i])…}`）。后者那个 `evs` **只进失败文案**，`spoke` 的计数只看 `ChatClickAt` 的返回值；加第二跳**只可能让 `spoke` 变大、不可能变小** ⇒ `spoke == 6` 不会因此变红。

---

## ② 改动清单

**唯一改动文件**：`d:/4/Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs`

| 行号（现核） | 一句话 |
|---|---|
| `:4188-4204` | `W36`：给 `ChatPopup` 6 个钮补原版那一跳 `defaultChatSound`（`ForChatButton[idx]` → `{ ForChatButton[idx], "default" }`）＋ 判据注释 |
| `:4228-4245` | `追加②`：`Unit Chat` 那段抬头注释**就地订正**（错话「后缀对应是我们定的」＋错行号 `§三·〇 :127` ⇒ 真落点 `:112`），保留更正痕 |
| `:12329-12343` | `W26`：「其余阵营的召唤动画**在远端包**」就地订正（同句在 `VfxMap.cs` 已改，这是第二份） |
| `:14473-14479` | `W8`/`W43`：`AboveShader Canvas` 的 `EnsureSubCanvas` 之前补 `HudCanvas.Ensure(LayoutSpace.Cam)` |
| `:14507-14526` | `W7`/`W55`：`SkillPanel.UseHost(EnsureRankedSubCanvas(SkillPanel.SubCanvasName, OverHand, 0))` |
| `:14557-14580` | `W42`/`W43`：`Unit Chat` 登记宿主（`Ensure` + `EnsureRankedSubCanvas` + `Create(root, cv)`）＋ 拿不到画布时**出声、本面板不建** |
| `:14620-14666` | `W30`：`AttackSelector.UseHost(EnsureRankedSubCanvas(AttackSelector.SubCanvasName, OverHand, 0))`（`selector == null` 也出声）。🔴 **追加④ 起名字改成【引用常量】、不再写字面量**，并把那段说自己「将来该补常量」「`AttackSelector.cs` 已过期」的注释**就地订正成过去时** |
| `:14668-14704` | `追加①+③`：私有口 `EnsureRankedSubCanvas(name, layer, order)` —— 建/取受排次的子画布，**当场验两条**：① `HudCanvas.IsRanked(name)`（不在表里 ⇒ `LogWarning`，`:14692`）② 建出来的就是这个名字（读不回 ⇒ `LogWarning`）。doc 里「**得让 `HudCanvas` 开一个 `IsRanked`**」已改成过去时 |
| `:15264-15290` | `W50`/`W43`：`AlliancePanelWindow.Create(root, uguiHost: cv)` ＋ `cv == null` 时**出声、本扇窗不建**（不退回世界档） |
| `:15294-15315` | `W42`/`W43`：`ChatPopup` 登记宿主（同 `Unit Chat` 的写法） |
| `:15423` | `W43`：`TooltipManager Canvas` 的 `EnsureSubCanvas` 之前补 `Ensure` |
| `:15475` | `W43`：加时 splash 的 `AboveShader Canvas` 之前补 `Ensure` |

### `git diff --numstat` + 行尾核对（**已做**）

```
$ git diff --numstat Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs
243     7       Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs
```

- ✅ **行尾没翻**：改前 `HEAD` = **17269 行 / CRLF 17269 / LF 17269（纯 CRLF）**；改后 = **17505 行 / CRLF 17505 / LF 17505（纯 CRLF）**。
- ✅ **判据**：`+243/−7`（净 +236）**远小于**文件行数 17505 ⇒ **不是整篇重写**；`CRLF == LF` 恒成立 ⇒ 无混行尾。
- ⚠️ **一处与简报警告不符（如实记）**：`G1` 简报写「本文件应是 **LF**」——**实测是纯 CRLF**（`file -b -` 与逐字节计数两法一致）。本波**按 CRLF 维持**（既没翻也没刻意改），与 `git diff --numstat` 的读数一致。

### `typecheck`（`TMPDIR=/tmp/wf_g1 bash 工具/typecheck.sh`）

- 跑法：**每改一处跑一次**（全程 12 次）。**最终读数 `运行时错误数: 0` / `编辑器错误数: 0`**。
- ⚠️ **中途一次「编辑器错误数: 1」**：`Editor/BattleScene.cs(21146,33) CS0136`（`probe964` 重复局部）—— **全部落在 `G2` 的文件上**（本波一行都没碰它），**隔一次复跑即消失**。符合简报预告的「源码互相可见」。另有 `A3`/`A12` 报过的同族现象，未重复计。

---

## ③ 接线清单（**白名单外**该改的 —— 只报不改）

| # | 落点 | 该改什么 | 为什么 |
|---|---|---|---|
| **G1-1** | ~~`Battle/AttackSelector.cs:388`~~ | ✅ **已办**（**`FC` 做的**，落点 `AttackSelector.cs:400`：`public const string SubCanvasName = "Drag Attack Selector Canvas";`，**27 字符与本波核过的表内字面量逐字节相同**）⇒ 本波已把 `BattleDriver.cs:14655` 改成 **引用常量**（⛔ 不再写字面量） | 「子画布名两处各写一遍」**从此消灭**（唯一来源 = `AttackSelector`）；`SubCanvasRank` 那张表按 `G3` 的口径**仍用字面量**，两边的一致性由 `EnsureRankedSubCanvas` 的 `IsRanked` **运行时核** |
| **G1-2** | ~~`Battle/AttackSelector.cs:390-393`（`_host` 的 doc）~~ | ✅ **已办**（**`FC` 做的**）⇒ `AttackSelector` **现在不需要再动** | 那两句过期 doc（「我们**不许**给它起一块自己的画布」＋「并进哪一块待主对话裁」）已订正 |
| **G1-3** | `Core/VoiceLines.cs:364-366`（`ForChatButton` 的 doc） | 那句「…消费点 `BattleDriver.cs` 用的是 `ForChatButton[idx]` **单元素**）⇒ **这里放不下**；要补得先改那个消费点」—— **消费点本波已改**（`BattleDriver.cs:4202`）⇒ 后半句「还没接」过期 | 与 `G1-1` 同族：文档说「没接」，代码已经接了 |
| **G1-4** | `Battle/UnitChatPanel.cs:29` | 🔴 **同一句错话的第五份**：`// 🔴 **查不到的（所以是我们挑的）**：原版**什么时机播哪一条**没有任何代码或数据（对账表 §三·〇 :127）。` —— ① 「查不到」已被推翻（`GetChatSound` 的 `switch` 钉死枚举→字段）；② 行号同样错（真落点 `:112`） | `铁律 5` 要我 grep 同一句被复制到别处的地方一起改；**这份不在本波白名单**（`B4` 那句「只碰我自己那份」的边界） |
| **G1-5** | ~~`Battle/HudCanvas.cs`（新增公共口 `IsRanked`）~~ | ✅ **已办**（**修波 `FD` 做的**，落点 `HudCanvas.cs:581`，签名逐字相同）⇒ 本波已把它**接进** `EnsureRankedSubCanvas`（`:14688`），读回自检**从「恒真」变成真检查** | 原先那道自检只能验「`EnsureSubCanvas` 建出来的名字 = 传进去的串」（恒真），**验不了「在不在 `SubCanvasRank` 表里」** ⇒ 这条已消灭 |
| **G1-6** | `BattleDriver.cs` 里**另外 2 个** `HudCanvas.EnsureSubCanvas` 调用点（`:14785` 一带 `LoadingMenu Canvas` · `:15704` 一带 `UI Error Message Controller Canvas`） | 可选：也改走 `EnsureRankedSubCanvas`（**提示**：`LoadingMenu` 在 `Blocking` 层、**本就不在 `SubCanvasRank` 表里**，改了会因为 `SubCanvas` 读得回来而**照样静默通过** —— 所以对它**没有收益**；`UI Error Message Controller Canvas` 在表里（`PopUps/1`），改了有收益） | 本波只动了台账点名的落点，没顺手扩大范围（`铁律 12` 的「别越界」） |
| **G1-7** | `Editor/BattleScene.cs:12315` | 可选：`evs.Append(VoiceLines.ForChatButton[i])` 现在**可能印错那个 token**（第二跳命中时实际 ev = `"default"`，文案仍印主 token） | 它**只进失败文案**、不影响判绿红；要精确得用 `TryPick` 的 `out ev` 重载（`VoiceLines.cs:532`） |

---

## ④ 判据与出处

### `W55` 的收尾 `grep`（**要求逐字贴**）

```
$ grep -n "UseHost" Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs
14510:            //   🔴 **登记之前这扇面板 100% 跑世界档**（`grep -n "UseHost"` 此前只有下面 `Tooltip` 那一处）
14524:                if (skillCv != null) skillPanel.UseHost(skillCv);
14653:                if (selectorCv != null) selector.UseHost(selectorCv);
14655:                                  + "（非 16:9 下会与已迁到画布的同族面板分家；`UseHost(null)` 自己也出声）");
15408:            if (tipCv != null) Tooltip.UseHost(tipCv);
```

⇒ **不再是「只有 `Tooltip.UseHost` 一处」**：`skillPanel` / `selector` 两处**真调用**已落地（另三处是注释）。
⚠️ 另外三扇（`Unit Chat` / `ChatPopup` / `Alliance Panel`）**走 `Create(parent, uguiHost)`、不调 `UseHost`** ⇒ 它们不出现在这个 `grep` 里，**不是漏了**。

### 5 块子画布的名字 —— **逐字节核过**（脚本产出，`G3` 追加要求）

| 本波登记点 | 名字来源 | 值 | 在 `SubCanvasRank` 表里？ |
|---|---|---|---|
| `:14522` `SkillPanel` | `SkillPanel.SubCanvasName`（`SkillPanel.cs:291`） | `"ActiveSkillDesc Canvas"`（22） | ✅ |
| `:14567` `Unit Chat` | `UnitChatPanel.SubCanvasName`（`UnitChatPanel.cs:116`） | `"Unit Chat Canvas"`（16） | ✅ |
| `:15302` `ChatPopup` | `ChatPopupPanel.SubCanvasName`（`ChatPopupPanel.cs:157`） | `"ChatPopup Canvas"`（16） | ✅ |
| `:15277` `Alliance Panel` | `AlliancePanelWindow.SubCanvasName`（`AlliancePanelWindow.cs:132`） | `"Alliance Panel Canvas"`（21） | ✅ |
| `:14655` `Drag Attack Selector` | `AttackSelector.SubCanvasName`（`AttackSelector.cs:400`，**追加④ 起**） | `"Drag Attack Selector Canvas"`（27） | ✅ |

⇒ 🔴 **5/5 全部走【面板自己的公开常量】**（本波最后一次改动就是把第 5 条从字面量换成常量）—— 「子画布名在别处再写一遍」这条**在整个 `BattleDriver` 里已经不存在**。

🔴 **这道「在不在表里」的检查现在是【代码自动核】的**（追加③）：`EnsureRankedSubCanvas`（`BattleDriver.cs:14668-14704`）第一步就是
`if (!HudCanvas.IsRanked(name)) Debug.LogWarning(…)`（`:14692`），口在 `HudCanvas.cs:581`
（`return !string.IsNullOrEmpty(name) && System.Array.IndexOf(SubCanvasRank, name) >= 0;`）
⇒ 将来任一登记点把名字改歪，**跑起来会响**，不再是「静默排在末尾、16:9 下看不出来」。
ℹ️ 主对话告知：这个口现在**有两个消费者** —— 本波这里，以及 `FA` 在 `BattleScene.cs` 的「`②-7` 灭自证」断言里**直接拿 `HudCanvas.IsRanked` 当判据**。
⚠️ **一处如实记**：`IsRanked` 为假**不等于「登记错了」** —— 表里本来就没有的子画布（`LoadingMenu` / `TooltipManager` 都在 `Blocking` 层、**本就不该进表**，见 `IsRanked` 的 doc）也会报一条；本口今天只被那 **5 个都在表里的登记点**调用，所以这条 warning 一旦出现就是真问题。
⚠️ **本波没能「跑」验这件事**：`IsRanked` 是**编译期查表**（`Array.IndexOf` 对 `static readonly string[]`），本波只做了**静态核对**（上表 5/5）+ `typecheck`；**运行时会不会真的不报**，要等主对话跑自检才看得到。

🔴 **一处前提订正（铁律 5，针对 `G3` 那条提醒）**：`G3` 说「⚠️ `AttackSelector.cs` 里没有 `SubCanvasName` 常量（只有 `:388 FrameName = "Drag Attack Selector"`，**不带 `" Canvas"`**）⇒ **照它拼会拼出对不上的一串**」——
**后半句不成立**（本波逐字节核过）：`AttackSelector.FrameName`（`:388`）＋ `" Canvas"` = **`"Drag Attack Selector Canvas"`，27 字符与 `HudCanvas.cs:548` 那条全等**（`FC` 补的常量 doc 自己也写着这句：「它的值与 `FrameName + " Canvas"` **逐字节相同**（27 字符，`G1` 现核过全等）」）。
⇒ 拼法**不会**出错，风险只在「两处都得改」。**当时的处置**：本波按 `G3` 的要求改成字面量照抄（好处：与 `HudCanvas.cs:548` 一眼可对）；**追加④ 之后**：`FC` 补了常量 ⇒ 本波改成**引用常量**（比字面量更好，见上表）。`G3` 那条提醒的**其余部分（②③④）全部照办**。

### `W26` 的判据

- 同一句错话**已在 `Core/VfxMap.cs` 先改**：`SummonCircleOf` 的 doc（`VfxMap.cs:232-250`，含「这句没有判据」的更正痕）＋ `SummonCircleByFaction` 表尾（`:280-282`）⇒ `BattleDriver.cs` 这份是**第二份**。
- **本波现读的三条实据**（转抄 `A1731`，并在 `VfxMap.cs` 原文核过）：① `assets_full` 的 `*ByArmy*` 只有 `RangedAttackParticlesByArmy` / `CardFramesByArmy` 两条（无 summon 支）；② `decomp_full` 按 `SummonCircle`/`summonParticles`/`SummonAnim` **0 命中**；③ 本地唯一的引用证据是逐卡的 `cardAbilities[].abilityLogic[].cardAnimations[]` ⇒ 那 4 个候选是「某些卡的技能动画」。
- ⚠️ **本波只订正那句话、没改行为**：`SummonCircleOf` 没配到仍回 `null`（并在 `VfxMap` 里出声），登场这一拍照旧留白。

### `追加②`（`G4` 那条）的判据 —— **已在源文件核过**

- `资料/战斗UI_原版对账表.md:112` = 上表「**单位语音条**」那一行（原文含「⚠️ **两处是我们挑的**：① 事件→台词后缀的对应；…」）。
- 同文件 `:114-128` = `G4` 插进去的**更正块**（含「本条在台账里被引作 `§三·〇 :127` —— **`:127` 是「回放条」那条的判据指引行**，本句的实际落点是 `:112`」）⇒ **与 `G4` 的说法逐字一致** ✓。
- **本波只改注释、零代码改动**（`:4228-4245` 全在 `//` 里）。

### `W7`/`W30`/`W42`/`W50` 的宿主判据

全部取 `普查产出_1016战斗判据/T09c_面板到原版宿主映射.md` §一 那一行（经 `G3` 的表核过）：`Drag Attack Selector`(0/14) · `ActiveSkillDesc`(1/14) · `Unit Chat`(2/14) · `ChatPopup`(4/7，在 `Unit Chat` 内部) · `Alliance Panel`(13/14)；层一律 `OverHand` / order 0。

---

## ⑤ 没查清 / 没做的 —— ⛔ 不猜

1. 🔴 **一处，如实记（**本波**的取舍）**：**我一开始把「拿不到子画布」写成了「出声 + 退回世界空间档」**（理由是「整扇要么全在画布、要么全在世界，不是半迁移态」）—— 这与 `W50` 的「**别退回世界档**」**相反**。**改成「出声 + 本件不建」** 后与同批三处**逐字同口径**：`MulliganPanel.Create`（`MulliganPanel.cs:603-611`）· `CardDisplayWindow.Create`（`CardDisplayWindow.cs:306-313`）· `MultiCardDisplay.Create`（`MultiCardDisplay.cs:208-215`）——**三处都是 `DestroyImmediate(节点) + LogError + 本件不建`**。本波这四处**还没 `new` 出节点 ⇒ 少一步销毁**，其余相同。
   - ⚠️ **代价如实记**：`cv == null` 时 `_unitChat` / `_chatPopup` / `_alliancePanel` 会**留 `null`（整件缺席）**，`SkillPanel`/`AttackSelector` 会**留在世界档**（它们是**已存在的场景组件**，「不建」这个选项不存在 —— 它们的世界档 `Build()` 在本波之前就发生了）。
   - 🔴 **这条只在病态档可达**：`EnsureSubCanvas` 回 `null` 的**唯一**条件是 `HudCanvas.RootRect == null`（根 canvas 没立起来）；本波已在每个登记点**先** `Ensure(LayoutSpace.Cam)` ⇒ 只有「`cam` 为空 / `LayoutSpace.Cam` 为空」才会走到那里（那一档整个 uGUI HUD 层都是坏的）。**本波没跑，所以「实际会不会触发」没验。**
2. ⚠️ **`W12` 的「僵尸 z 账」具体错位量**：只做到「读代码 + 旁证」（`Group` 无出表回调、`Teardown` 无注销）⇒ **方向成立**；**具体影响（会不会让某个同容器件的兄弟序偏 1）没量**（要跑才量得到）。
3. ⚠️ **`W7`/`W30` 的「重建一次」没有实测**：`SkillPanel.UseHost` 排在 `Build()` 之后会走 `Teardown(); Build();` —— 那是 **`SkillPanel` 自己的 doc 说的**（`SkillPanel.cs:342-345`），本波**没跑**去确认它真的重建成功（贴图/字体缺失时可能出空件）。
4. ⚠️ **`W36` 的第二跳没有一个「卡上真的只有 `default`、没有 `greet`」的实例**：本波只核了**反编译那条回落链**与 `TryPick` 的顺序语义；**没查**现池里到底有几张卡会靠这一跳救回来（要查 `voice_lines.json` 的 `ev="default"` 与 `ForChatButton` 6 个 token 的分布）。⇒ **行为对**、**命中率未知**。
5. ⚠️ **`ChatPopup` 的层序（4/7「在 `Unit Chat` 内部」）在我们这边是【两块平级子画布】**：`G3` 的表把 `"ChatPopup Canvas"` 紧跟 `"Unit Chat Canvas"` 排 ⇒ 渲染次序对得上；但我们**没有**原版那种父子嵌套（`ChatPopup` 是 `Unit Chat` 的 `m_Children[4]`）⇒ **`_unitChat` 上如果有整块 `SetActive(false)` 之类的操作，原版会连 `ChatPopup` 一起关、我们不会**。**本波没查** `UnitChatPanel` 有没有这种整块开关（留下这条给主对话判）。
6. ⛔ **没做**（白名单外，见 §③）：`AttackSelector.cs` 的 `SubCanvasName` 常量与过期 doc · `VoiceLines.ForChatButton` 的过期 doc · `UnitChatPanel.cs:29` 的错话 · `BattleScene.cs` 的文案/断言（`W11`/`W13`/`W32`/`W34`/`W45`/`W52`/`W57` 等 —— **全部是 `G2` 的活**，本波一行没碰）。

---

## ⑥ 顺手发现（**不属于本件，只报不改**）

1. ✅ **`Battle/HudCanvas.cs` 那条「自查法」的反例已被 `FC` 消掉**（本波原先是报了这个反例）：它写「本表每一项都必须是**某扇面板 `SubCanvasName` 的字面量**（`grep -rn "SubCanvasName = " Battle/ Shell/`）」—— 本波初查时 `"Drag Attack Selector Canvas"`（表里 `:548`）**没有**对应的常量（`AttackSelector.cs` 里 `grep -c SubCanvasName` = **0**）⇒ 照那条自查法会把它判成「写坏了」。**`FC` 本波已补上常量**（`AttackSelector.cs:400`）⇒ **这条自查法现在对全部 16 项都成立**，⛔ 不用再列例外。
2. ⚠️ **`G3` 表里的行号引用会漂**：`G3` 的报告与主对话转述都写 `HudCanvas.cs:544`，**现核那一条在 `:548`**（表头 doc 改过、行号整体 +4）。同理它给 `W7` 的行号是 `:14434`、现核 `:14430`（本波改前）。⇒ 与本线已知的坑（「引本文件行号一律先现核」）同族，**不是错、只是会过期**。
3. ✅ **主对话托核的那一条（`HudCanvas.cs` 的 `PairCanvas` 举例）`FD` 已经自己办掉了，不用再动**：`HudCanvas.cs:485` 那句「各自在外层写了一个幂等包装：`PairCanvas` / `ReuseOrMakeCanvas`」**下面是 `FD` 当场加的更正段**（`:486-488`：「上面点名的那几个包装**本波已全删**（四处，直接调本口）⇒ 如今只剩方法名、体内就是一句本口」）—— 本波现读核实 ✅。（`HudCanvas.cs` 不归本波，**只读**。）
4. ⚠️ **本报告自身的行号也只对「本波收工那一刻」有效** —— `BattleDriver.cs` 是**共享文件**，`G1` 之后还会有人动（本波自己就在 `:14664` 一带插了新方法，把它后面的锚点整体推后）。**引用前请按【名字/方法名】现核**，不要照抄行号：`grep -n "EnsureRankedSubCanvas(" BattleDriver.cs` 一步即可定位全部 5 个登记点；`grep -n "UseHost" BattleDriver.cs` 定位登记口。
5. 🔴 **同一句错话的副本共 **5** 处在 `.cs` 里**（本波 grep 到，全部带错行号 `:127` 或带「查不到」）：① 本文件 `:4238`（**本波已改**）② `Battle/UnitChatPanel.cs:29` ③ `Core/VoiceLines.cs:18-20`（**已有更正痕**，只留引文）④ `Core/VoiceLines.cs:113`（**已有更正痕**）⑤ `资料/战斗UI_原版对账表.md`（**`G4` 已加更正块**）。⇒ 只剩 **②** 没改（§③ `G1-4`）。
6. ⚠️ **`BattleScene.cs` 里那条聊天钮断言（`:12315`）现在是「弱断言」**：第二条路（`evs`）在第二跳命中时会**印错 token**；`spoke == 6` 仍然有效，但**「点得动」这件事从此不再等价于「那一条真的存在」**（原来 `TryPick` 失败会 `return false` ⇒ `spoke` 掉；现在有兜底 ⇒ 更难红）。**建议**（不属本件）：要么用 `out ev` 断言 ev 属于 {主 token, `default`}，要么别拿 `spoke` 当「这条语音存在」的证据。

---

## 可直接粘进总表的一行（≤3 行）

```
[G1] BattleDriver.cs 收口接线 ✅ —— W7/W8/W12/W26/W30/W36/W42/W50/W55 全办（W43 现核由「五处」修正为「三处真缺、两处本来就有」，3/3 已补）；
追加四条也办（G3：5 块子画布名逐字节核过；G4：语音条那段注释订正 §三·〇 :127→:112；FD：接上 HudCanvas.IsRanked，读回自检由「恒真」变真检查；
FC：改用 AttackSelector.SubCanvasName 常量 ⇒ 5/5 登记点全走面板常量，子画布名不再有第二个抄写点）。
改 1 文件 +243/−7（17269→17505 行 · 纯 CRLF 未翻）· typecheck 0/0（每处跑一次，共 14 次）· 未跑 Unity · 未动 git。
```
