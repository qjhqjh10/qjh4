# E20 · 处置 `Editor/DeckScene.cs` 里会因 `A1012` 后半【变红】的那几条断言

> 执行代理 E20 · 2026-10-19 · 白名单内**实际写了 1 个文件**：`Editor/DeckScene.cs`（`git diff --numstat` = **+85 / −16**）。
> ⛔ 没跑 Unity（红线）· ⛔ 没碰 git 的写操作（只 `diff --numstat` / `status` 只读）· ⛔ 没改两张正本 · ⛔ 没碰 `Shell/**` · `Deck/**` · `Core/**`。
> ✅ 秒级类型检查（`TMPDIR=/tmp/wf_e20 bash d:/4/Unity/工具/typecheck.sh`）：**跑了两次** ——
> 第一次「运行时 **0** / 编辑器 **8**」（8 条**全在 `Editor/ShopScene.cs`**，是**别的写手**当时在写的半成品，
> ⛔ 我没去碰它）；隔 100 秒重跑 ⇒ **「运行时 0 / 编辑器 0」**（证明那 8 条与本笔无关，已自愈）。
> 行尾：`DeckScene.cs` 纯 **CRLF**（改前 `b'\r\n'=6037` / `b'\n'=6037` → 改后 **`6106 / 6106`**，**未翻**；全程用 Edit 工具，⛔ 没用 `sed -i`）。

---

## ① 结论

**6 条必红 + 4 处注解已废 —— 全部处置完；另捞出 1 条「被误判成必红、其实是绿的」并就地纠正（见 §④·A）。**

1. **必红那 5 条 `*Shown` 断言**（原来断「画出来的就是那个键」）⇒ 改成 \*\*「拿的是哪个键」归 `*Key` + 「画出来的是词条」用 `Loc.T(键)` + **灭自证「印的不是键名本身」**\*\*，形状照**本文件自己的两个既有先例**抄（`A891` 那节 `:2498-2502` 的 `Loc.HasEntry(键)` + `Loc.T(键)` 一对；`G9` 那节 `:3700-3712` 的两语档真字面量）。
2. **必红那 1 条 `Terms` 往返**（`:3796-3798`）⇒ 那一段是**自证结构**（拿我们自己的表证明我们自己的函数），按调度台口径**重写成「结构上不可能同时满足」**：往 `Terms` 塞一条 `MenuDeck/Error/5`，**选出来的键不动、字却取到了** —— 同一个实现不可能既「选键看 `Terms`」又「选键不看 `Terms`」。⛔ **没**给 `MenuDeckErrorKey` 加回 `Terms` 那一跳（那等于把静默失败搬回来）。
3. **`:3788-3789` 那条「前提」**（`Terms.Count == 0`）⇒ **措辞重写、保留**（它现在说的是「两处表」这件真事实，不再是「下面显示的是键」）；另**新补两条真前提**：兜底键在 `Loc` 表里 / 数字键今天不在 `Loc` 表里。
4. 🔴 **纠正 E14 §④(c) 与调度台清单里的一条**：**`:3799`（原行号）「`Term` 取到了塞进去的假词条」其实是【绿的】、不是必红** —— 详见 §④·A。
5. 🔴 **一处覆盖面缺口（如实报，非本笔能补）**：`MenuDeckErrorKey` 的**正半支**（「数字键在表里 ⇒ 就用它」）**今天构造不出来**（`Loc` 无写入口、`MenuDeck/Error/{2,4,5}` 不在表里）⇒ 我用一条**真前提断言**把**切换时机**钉住：哪天补上那三条词条，那条前提**自动红**、提醒把 ③-a 的期望值一起切（两态写法，照 `Editor/BattleScene.cs` `A1018①` 那节的 `costInTable` 先例）。
6. **产品代码一行没动**；`Shell/PopUpGameWindow.cs` · `Deck/DeckRuntime.cs` · `Core/Loc.cs` **一个字节没碰**。

---

## ② 三堆分类（**逐条列全**，⛔ 一条不落 —— 行号 = **本笔改前**的现读行号）

### 堆 A —— 🔴 **会红（必红，5 条）**

| # | 原行号 | 断的是什么 | `(B)` 之后**会变成** | 处置 |
|---|---|---|---|---|
| A1 | `:3836-3837` | `Check(pop.MessageShown, "MenuDeck/Error/InvalidDeck", …「画出来的就是那个键」)` | 「卡组不合法」/`Invalid deck` | ③ 段重写（见 §③·C） |
| A2 | `:3838-3839` | `Check(pop.PrimaryShown, "MainMenu/General/Discard", …)` | 「丢弃」/`Discard` | ③ 段重写 |
| A3 | `:3840` | `Check(pop.SecondaryShown, "MainMenu/General/Cancel", …)` | 「取消」/`Cancel` | ③ 段重写 |
| A4 | `:4118` | `Check(one.PrimaryShown, "MainMenu/General/Cancel", "★ 那一颗的字 = 给的那个键")` | 「取消」/`Cancel` | ④ 段重写 |
| A5 | `:3797-3798` | `Check(MenuDeckErrorKey(TooFewCards), "MenuDeck/Error/5", …)` | 兜底键 `MenuDeck/Error/InvalidDeck` | ① 段重写 |

### 堆 B —— ⚪ **仍绿，但注解/理由已废（4 处）**

| # | 原行号 | 原来写什么 | 为什么废 | 处置 |
|---|---|---|---|---|
| B1 | `:3775` | 注释「① 文案：原版号 → 术语键 → **显示的是键**」 | `Term()` 已转 `Loc` ⇒ 印的是**词条** | 就地订正 + 留痕 |
| B2 | `:3788-3789` | `CheckTrue(Terms.Count == 0, "（前提）I2 词条表**本地是空的** …… 下面「显示的是键」才有意义")` | 推理已不成立（它钉的是**我们的实现细节**，⛔ 不是原版判据） | 措辞重写、断言**保留**（`Terms` 确实仍空）；**另补两条真前提** |
| B3 | `:3790-3791` | `Check(MenuDeckErrorKey(TooFewCards), "MenuDeck/Error/InvalidDeck", "★ ……表空 ⇒ 落兜底键")` | **结论仍对、理由变了** —— 不是「`Terms` 空」而是「**`Loc` 没有数字键**」 | 理由重写 + 把真前提**显式断出来** |
| B4 | `:3795` | 注释「🔴 把『将来填表』那条路也钉住：…」 | 那条「将来填表」的路已**改看 `Loc`**（往 `Terms` 填不再影响选键） | 整段重写（见 §③·B） |
| — | `:3835` | `Check(pop.MessageKey, "MenuDeck/Error/InvalidDeck", "★ 正文键 = 兜底键（表空）")` | 断言仍绿（断的是**键**）；错的是尾注「（表空）」 | 尾注改成「判据 = 数字键不在 `Loc` 表里」 |

> （B4 与 §③·B 是同一处的两种说法；把它记在「注解废」堆里是因为**那两句注解**才是废的。）

### 堆 C —— ✅ **完全不受影响（绿、注解也对；⛔ 一个字没改）**

| # | 原行号 | 断什么 | 为什么不受影响 |
|---|---|---|---|
| C1 | `:3792` | `MenuDeckErrorKey(None) == ""` | 断的是 `None` 那一支（`err==0 ⇒ 空串`），与表无关 |
| C2 | `:3793-3794` | `Term("MenuDeck/Error/5") == "MenuDeck/Error/5"` | `Terms` 与 `Loc` **两处都没有**这条键 ⇒ 两版实现都返回键名（语义**恰好**重合） |
| C3 | **`:3799`** | `Term("MenuDeck/Error/5") == "（自检塞的假词条）"` | 🔴 **仍绿** —— `Term()` 是**先查 `Terms`**、`Terms` 里刚被塞过 ⇒ 取到的就是那条假词条。**E14 §④(c) 判它「必红」是错的**（见 §④·A） |
| C4 | `:3800-3801` | `Terms.Remove(...)` + `Terms.Count == 0`（收尾） | 断的是我们自己的收尾，两版实现下都成立 |
| C5 | `:4119` | `one.SecondaryShown == null \|\| == ""` | 1 按钮版没有第二颗 ⇒ `_secondaryLb` 是 null，与 `Term` 无关 |
| C6 | `:3831` | `CheckTrue(!pop.closeOnEsc, …)` 等同段其它字段断言 | 全在窗元数据上，不碰文本 |
| C7 | `:3906-3915` | `MessageText` 那颗 `Label` —— **只读 `RenderQueue`**、⛔ 不断它的文本 | 断的是层带号，不是文案 |
| C8 | `:3921-3934` | 压暗层 `ShadeHitNode` / `WasAbsorb` / `absorbOnly` | 与文案无关 |
| C9 | `:4568` | `popAsk.MessageKey == DeckRuntime.DiscardChangesKey` | 断的是**键** |
| C10 | `:4571` / `:4573` | `popAsk.PrimaryKey` / `SecondaryKey` | 断的是**键**（这一组**本来就是**调度台要我照抄的形状 ⇒ 见 §③·C 的注释「与 `:4571` 那组同形」） |
| C11 | `:4651` / `:4657` | `pop502.MessageKey` / `_rt.ModalPopup.MessageKey` | 断的是**键** |
| C12 | `:4456` / `:4467`（A415 那节） | `_rt.ModalPopupOpen` + 拿窗句柄 | 不断文本 |
| C13 | `:4697` | `Tooltip.ShownBody == TipText.Cost` | **另一个 `Shown`**（tooltip），与 `PopUpGameWindow` 无关 |

> 🔎 **「第 7 条」的排查办法与结论**：我把 `DeckScene.cs` 里**每一条**读
> `MessageShown` / `PrimaryShown` / `SecondaryShown` / `MessageKey` / `PrimaryKey` / `SecondaryKey` / `Terms` 的断言**逐条过了一遍**
> （`grep -n` 三遍：`Shown` / `(Message|Primary|Secondary)(Key|Shown)` / `PopUpGameWindow|\.Terms`），
> 又把**读弹窗渲染文本的所有旁路**也扫了（`FindDeep(pop.transform, "MessageText")` · `UiLabelText` · `UiLastSay` · `ModalPopup`），
> ⇒ **没有第 7 条**。唯一的「多出来的一条」是 C3（**E14 多算了一条红的**）。

---

## ③ 每一处：改前 / 改后 / 断什么 / 期望值怎么来 / 改坏法

> 全部在 `D:/4/Unity/MyGame/Assets/CardPresentation/Editor/DeckScene.cs`。**改后行号 = 现读**。

### A. `:3775`（+3 行）—— 段头注释订正

- **改前**：`// ---- ① 文案：原版号 → 术语键 → **显示的是键**（纯函数先钉住，不依赖窗口）----`
- **改后**：`…… **显示的是词条** ……` + 3 行 `🔴 2026-10-19（A1012）就地订正（铁律 5）`（照录旧句 + 写清判据 = `Term()` 已转 `Loc`）。
- **断什么**：无（纯注释）。**改坏法**：不改 ⇒ 下一轮把「印键名」当成本节的设计意图。

### B. `:3791-3835`（现读）—— ① 段的 `Terms` 往返，**整段重写**

**改前（原 `:3788-3801`）**：
```csharp
CheckTrue(PopUpGameWindow.Terms.Count == 0, "（前提）I2 词条表**本地是空的** …… 下面「显示的是键」才有意义");
Check(DeckRuntime.MenuDeckErrorKey(DeckError.TooFewCards), "MenuDeck/Error/InvalidDeck", "★ ……表空 ⇒ 落兜底键");
Check(DeckRuntime.MenuDeckErrorKey(DeckError.None), "", "★ ……`None` ⇒ 空串");
Check(PopUpGameWindow.Term("MenuDeck/Error/5"), "MenuDeck/Error/5", "★ ……`Term` 查不到就返回键本身");
PopUpGameWindow.Terms["MenuDeck/Error/5"] = "（自检塞的假词条）";
Check(DeckRuntime.MenuDeckErrorKey(DeckError.TooFewCards), "MenuDeck/Error/5", "★ ……表里真有 ⇒ 键就用它");
Check(PopUpGameWindow.Term("MenuDeck/Error/5"), "（自检塞的假词条）", "★ ……而且 `Term` 取到了那条文字");
PopUpGameWindow.Terms.Remove("MenuDeck/Error/5");
Check(PopUpGameWindow.Terms.Count, 0, "（收尾）……");
```

**改后（现读 `:3804-3835`）** —— 五件事，**逐条**：

| 位置 | 断什么 | 期望值怎么来 | 🧨 改坏法 |
|---|---|---|---|
| `:3804-3806` | `Loc.HasEntry(DeckRuntime.MenuDeckErrorKeyFallback)` —— **兜底键在 `Loc` 表里** | 键来自 `DeckRuntime.MenuDeckErrorKeyFallback`（原版 `DAT_1842cf5f0` 地址表读数），**存不存在**由 `Loc` 表说话 | 兜底键从表里删掉 ⇒ 红（那时那扇窗印的会是键名） |
| `:3807-3813` | `!Loc.HasEntry(numKey5)`（`numKey5` = `string.Format(MenuDeckErrorKeyFmt, MenuDeckErrorNumber(TooFewCards))` = `MenuDeck/Error/5`）—— **真前提** | **原版键**由 `DeckRuntime` 的两条常量拼出（`Fmt` + `Number`，各有反编译出处）；**它在不在表里**由 `Loc` 现查 | 🔴 **两态切换点**：哪天补上这三条数字键 ⇒ 这条**自动红**，那时把 ③-a 的期望值一起改成 `numKey5`（注释里写死了这句提醒） |
| `:3814-3816` | `PopUpGameWindow.Terms.Count == 0`（**保留**，措辞重写） | 我们的实现事实（`Terms` 出厂空）；现在说的是「**两处**表」这件真事实 | —— |
| `:3817-3820` | `MenuDeckErrorKey(TooFewCards) == DeckRuntime.MenuDeckErrorKeyFallback` | 期望值 = **原版兜底键常量**（⛔ 不是从实现反推） | 判据改回 `Terms.ContainsKey`（恒空表）⇒ **③-c 之一红** |
| `:3822-3824` | `Term("MenuDeck/Error/5") == "MenuDeck/Error/5"` | 原版语义「两处都没有 ⇒ 给键本身」（`Core/Loc.cs:1826`） | 让它编一句人话 ⇒ 红 |
| **`:3826-3829`（灭自证之一）** | 塞 `Terms["MenuDeck/Error/5"] = "（自检塞的假词条）"` 之后，**选出来的键** `==` 兜底键 | 同上（判据 = `Loc.HasEntry`，`Terms` **不参与选键**） | 判据改回 `Terms.ContainsKey` ⇒ 这时会返回 `MenuDeck/Error/5` ⇒ **红** |
| **`:3830-3833`（灭自证之二）** | **同一时刻** `Term("MenuDeck/Error/5") == "（自检塞的假词条）"` | `Terms` **优先于** `Loc`（`Shell/PopUpGameWindow.cs` 的 `Term` 现读语义） | 删掉 `Term` 里 `Terms` 那一跳 ⇒ **红** |
| `:3834-3835` | 收尾：`Terms.Remove` + `Count == 0` | —— | —— |

> 🔑 **为什么这算「结构上不可能同时满足」**：灭自证之一要求「选键**不**看 `Terms`」、灭自证之二要求「取字**看** `Terms`」——
> 两条分别盯**两个不同的口**，而 `DeckScene` 这一段是**唯一**能同时构造「`Terms` 里有一条、`Loc` 里没有」这个状态的地方。
> 原来的写法（`MenuDeckErrorKey == "MenuDeck/Error/5"`）则是「**两边一起**改回看 `Terms` ⇒ 两条全绿」的**自证**。
> ⚠️ 与铁律「灭自证」那条同族：这条管的是**改哪两处会一起变绿**。

### C. `:3869-3901`（现读）—— ② 开窗那一组的三条 `*Shown`

**改前（原 `:3835-3840`）**：4 条（`MessageKey` 1 条 + `MessageShown`/`PrimaryShown`/`SecondaryShown` 3 条），后 3 条断的是**键名**。

**改后（现读 `:3869-3901`）**：

| 位置 | 断什么 | 期望值怎么来 |
|---|---|---|
| `:3869-3870` | `pop.MessageKey == "MenuDeck/Error/InvalidDeck"`（**保留**，尾注改成「判据 = 数字键不在 `Loc` 表里」） | **原版字面量**（同上） |
| `:3879` | `string wantMsg = Loc.T("MenuDeck/Error/InvalidDeck");` | **从 `Loc` 表读**（⛔ 不从实现反推、⛔ 不写死中英字面量 —— 语档无关） |
| `:3880-3882` | `pop.MessageShown == wantMsg` —— **画出来的是那条键的词条** | 同上 |
| `:3883-3885` | **灭自证**：`!string.IsNullOrEmpty(pop.MessageShown) && pop.MessageShown != pop.MessageKey` | `pop.MessageKey`（同窗真值）；**本缺陷要防的那一半** |
| `:3887-3889` | `pop.PrimaryKey == "MainMenu/General/Discard"` —— **左钮拿的键**（原来这半句挂在 `*Shown` 上 ⇒ 归位到 `*Key`） | 原版字面量（`LiveButtons[0]` = `ButtonLeft`，与 `:4640` 那组同形） |
| `:3890-3891` | `pop.PrimaryShown == Loc.T("MainMenu/General/Discard")` | `Loc` 表 |
| `:3892-3893` | 灭自证：`PrimaryShown` 非空且 `!= PrimaryKey` | 同上 |
| `:3895-3897` | `pop.SecondaryKey == "MainMenu/General/Cancel"` | 原版字面量 |
| `:3898-3899` | `pop.SecondaryShown == Loc.T("MainMenu/General/Cancel")` | `Loc` 表 |
| `:3900-3901` | 灭自证：`SecondaryShown` 非空且 `!= SecondaryKey` | 同上 |

- 🧨 **改坏法**：把 `Shell/PopUpGameWindow.cs` 的 `Term()` 末句改回 `return key ?? ""` ⇒ **灭自证三条 + `Loc.T` 三条全红**；
  把 `Loc` 表里那三条键的值改坏（例如「丢弃」→「扔掉」）⇒ **`Loc.T` 三条红**（先例理由见本文件 `:5671-5677` 那段）；
  `Loc.HasEntry` 那层由 ① 段两条真前提单独钉住。
- ⚠️ **`!string.IsNullOrEmpty` 那半是必要的**（照本文件 `:5110-5113` 的写法）：少了它，`MessageShown == null` 会让 `!=` 那条**假绿**。

### D. `:4179-4188`（现读）—— 1 按钮版那颗钮

**改前（原 `:4118`）**：`Check(one.PrimaryShown, "MainMenu/General/Cancel", "★ 那一颗的字 = 给的那个键");`
**改后**：
- `:4181` `string wantOne = Loc.T("MainMenu/General/Cancel");`
- `:4182-4183` `one.PrimaryKey == "MainMenu/General/Cancel"`（**拿的键**）
- `:4184-4185` `one.PrimaryShown == wantOne`（**画出来的词条**）
- `:4186-4187` 灭自证：非空且 `!= one.PrimaryKey`
- `:4188`（原 `:4119`）`one.SecondaryShown == null || == ""` **原样保留**（堆 C5）

---

## ④ 没查清 / 做不了的部分

### A. 🔴 **纠正两处「必红」的误判**（本笔现读核出来的）

1. **`:3799`（E14 §④(c) 判「必红」）—— 实际是【绿的】**。
   理由：`Shell/PopUpGameWindow.cs` 的 `Term()` 是
   `if (… Terms.TryGetValue(key, out t) …) return t; return Loc.T(key);` —— **`Terms` 先查**；
   而上一句（`:3796` 原行号）刚刚 `Terms["MenuDeck/Error/5"] = "（自检塞的假词条）"` ⇒ **取到的就是那条假词条**。
   E14 那句「`Term` 转 `Loc`，`Loc` 表没有 ⇒ 返回键名本身 ⇒ 红」**漏了 `Terms` 优先这一跳**（它自己在同一段的建议里写着「`Terms` 优先我已经保留了」）。
   ⇒ 调度台清单里的「必红的 6 条」应为 **5 条**（我按 5 条处置）。
2. **`:3790-3791`（E14 §④(e) 判「仍绿」）—— 判对了**，但 E14 只写了「措辞宜改」；我已按**两态前提**把它做实（见 §③·B 第 2、4 行、堆 B3）。

### B. ⚠️ **覆盖面缺口（本笔构造不出来，⛔ 不是「不做」）**

`MenuDeckErrorKey` 的**正半支**（「数字键**在**表里 ⇒ 就用它」）**今天无法构造**：
- `Loc` 是**私有字段 + 无写入口**（`Core/Loc.cs`，且它是**黑名单**，⛔ 我不许碰）；
- `MenuDeck/Error/{2,4,5}` 这三条数字键**不在** `Loc` 表里（E14 §② 已逐条核过，我复核：`grep "MenuDeck/Error"` 只有具名族 + `SaveFailed` + `Import*` + 整句族）。
- ⇒ 我用 `!Loc.HasEntry(numKey5)` 这条**真前提**当「切换时机」的哨兵（补上词条当天它自动红、提醒改期望值），
  **不是**把期望值写成「实测值」。**这一格仍欠一条正面覆盖**，须等「补数字键」那笔账落地（E14 §④·C 第 1 条）。
  ⛔ **没为了让它好看去建键**（`Core/Loc.cs` 不在白名单，且编文案是红线）。

### C. ⛔ **没做的事**（越界即违红线）

- ⛔ **没跑 Unity** ⇒ 本报告**没有一条断言被真跑过**，A/B/C 三堆的分类与红绿全是**静态推断**（判据 = E14 改的那两行 + `Term`/`Loc.T`/`Loc.HasEntry` 的现读语义）。
- ⛔ **没改产品代码**：`Shell/PopUpGameWindow.cs` · `Deck/DeckRuntime.cs` · `Core/Loc.cs` · `Editor/ShopScene.cs` 一个字节没动。
- ⛔ **没去修** `Editor/ShopScene.cs` 那 8 条 `CS0165`（那不是我的白名单文件；隔 100 秒重跑已自愈 ⇒ 是并发写手的半成品）。

### D. 📋 交给调度台的账

1. **本笔之后 `DeckScene.Run` 里那一族应当全绿**，但**只有真跑才算数**（主对话收口时跑）。
2. **`MenuDeck/Error/{2,4,5}` 三条数字键的缺口**照旧挂着（E14 §④·C 第 1 条）——补上之后
   **本笔新加的 `!Loc.HasEntry(numKey5)` 那条会红**，那是**设计好的提醒**、⛔ 别当成缺陷。
3. **`Deck/DeckRuntime.cs:3648-3653` 的 `DiscardChangesKey` 那一族**：`popAsk` / `pop502` 两组断言**只断 `*Key`**（堆 C9/C10/C11），
   ⇒ 「那扇窗的正文键 `MenuDeck/HUD/DiscardChanges` 屏上印的是**词条**（中文「丢弃未保存的改动？」）」**今天没有任何断言钉住**。
   ⚠️ 这是**同一族的第三个洞**（前两个已由本笔补上）。**本笔没补** —— 理由：那两处窗口的生命周期是「开→点→关」，
   要验渲染文本得在窗口活着时插断言，会给 A399/A502 两节的点击链加耦合 ⇒ **留给调度台决定要不要单开一笔**（如实报，不是不做）。

---

## ⑤ 顺手发现的东西（⛔ 一个都没改，只报）

1. **`Loc.T` 的「自证陷阱」在本文件里已经有两套写法并存**：
   - 主流（`A891` / `D2` / `D43` / `P4` / `G9`）：`Loc.HasEntry(键)` + `Check(实测, Loc.T(键))`；
   - 最强（本文件 `:5670-5677` 那一段自己写着理由）：**中英两列都写成字面量**（`sTerms`/`lZh`/`lEn`），
     并在注释里点名「本批其余每一条断言都是 `Check(渲染值, Loc.T(同一把键))` —— 两边都过 `Loc.T`
     ⇒ **把 `Core/Loc.cs` 里那条中文值改坏，全部断言照绿**」。
   ⇒ **本笔三处用的是主流写法**（§③·C/D），因为要照**同族最近**的写法、且两语档字面量写法需要切换语档（那会在 A364 那节引状态耦合）。
   ⚠️ **但这是已知的弱处**：`Loc.T` 三条挡不住「`Loc.cs` 里中文值被改坏」。**如实报**，不粉饰。
2. **`Shell/PopUpGameWindow.cs` 的 `Terms` 现在只剩「设计」意义**（两条消费路一条改看 `Loc`、一条是 `Term` 的优先跳）。
   `DeckScene.cs` 里**唯一**还读 `Terms` 的地方就是本笔重写的那一段（现读 `:3814-3835`）⇒ 那一层要不要留自检口，须调度台定（E14 §④·C 第 2 条同问）。
3. **`Editor/ShopScene.cs` 在 2026-10-19 这一波里出现过 8 条 `CS0165`（`gx1/gx2/gy1/gy2` · `ax1/ax2/ay1/ay2` 未赋值局部）**，
   100 秒后自愈 ⇒ **是并发写手的瞬时半成品**。⚠️ 记一笔：**下次看到这种「错误全在别人文件上」的读数，隔一会儿重跑就对了**（铁律 13·3 那条判据又一次兑现）。
4. **`Check<T>(T got, T want, string msg)` 是泛型**（`:83`）⇒ `Check(pop.MessageShown, Loc.T(键), …)` 这种「实测/期望同源」的写法**编得过**，
   所以「自证」在**编译期**没有任何拦阻 —— 只能靠人（`CLAUDE.md` 铁律 10 第 4 条 / 灭自证那条）。

---

## 需要哪几条自检覆盖（**本笔没跑，交给调度台排**）

| 条目 | 为什么 |
|---|---|
| **`DeckScene.Run`** | 🔴 **必跑**：本笔的改动全部在这一个宿主里（6 条红改完 + 4 处注解） |
| 其它 11 条 | ⚪ **不必**：本笔**只动了一个 `Editor/*.cs` 自检宿主**、⛔ 没碰共用件、⛔ 没碰引擎 ⇒ 铁律 12 判据 ①（只动一个宿主就只跑那一条） |

⚠️ 判绿红照旧看**断言合计**、⛔ 别按行首 `✗` 数（`✗` 会出现在断言**文案**里 —— 本笔新增的文案里就带 🧨/⛔ 这类描述旧行为的字）。
