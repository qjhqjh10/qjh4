# A80-1 · `Label.SetAutoFitBox` 的 `fontSize` 时序（A57③）—— 补一条**判别性**断言

日期：2026-10-06 · 白名单 2 个文件（只用了 1 个）· 未跑 Unity / 未动 git / 未改正本
代号：A80-1（只做 A80①；**A80② 不归我** —— `工具/_probe_deckinfo.py` 不在白名单，未碰）

---

## 一、结论

| # | 事 | 结果 |
|---|---|---|
| 1 | 新断言落地 | ✅ `Editor/BattleScene.cs:1986-2065`（**§4.6b**，接在既有 §4.6 之后、同一段作用域里）= **2 条前提 + 4 条 Check** |
| 2 | 正本写的「比 `FontPxNow`」那半 | ✅ 落地了（第 ① 条）—— **但它单独抓不到 A57③**（理由见 §一·1） |
| 3 | 「能真分辨两种状态」 | ✅ 由**第 ② 条**做到：**直读 TMP 的 `m_fontSizeBase`**（复用那条 vs 新建那条必须一致）。这才是把 A57③ 钉住的那颗牙 |
| 4 | 改 `Battle/Label.cs`？ | ⛔ **没改**。判据（A80② 的分工）= 「红了才动共用件」；现写法**是对的**（`:370-371` 先关自适应、再写 `fontSize`），新断言**应该**绿 ⇒ 共用件一个字没动 |
| 5 | 秒级类型检查 | ✅ 通过（最后一行） |

### §一·1 🔴 关键判断：**只比 `FontPxNow` 的写法，抓不到 A57③**（本件的核心发现）

正本给的判别性写法（`项目任务.md:474`）= 「复用 A → 再 B，比 `FontPxNow` 与新建只走 B 那份是否一致」。
我把 TMP 的自适应读透了以后，认为**它做不到**，依据是 TMP 源码里两件事：

- **自适应是二分**：缩的那支在 `TextMeshPro.cs:3073-3089`（`m_fontSize -= max((m_fontSize − m_minFontSize)/2, 0.05)`），
  涨的那支在 `:4136-4155`（`m_fontSize += max((m_maxFontSize − m_fontSize)/2, 0.05)`），
  直到 `m_maxFontSize − m_minFontSize ≤ 0.051` 为止（`:4139`）。
- **起点只影响路径、不影响终点**：`m_fontSizeBase` 唯一的下游用法就是**每次重排的起点**
  （`TextMeshPro.cs:2148-2149` `m_fontSize = Mathf.Clamp(m_fontSizeBase, m_fontSizeMin, m_fontSizeMax)`）
  ⇒ 起点是 1.758 还是 2.988，二分都收在**同一个**「装得下的最大号」上。

两条手工推演（文字都是 `221.6 × 29px` / `[10, 32]px` 这一档）都落在同一个终值；A57③ 改坏之后
**真正错位的只有 `m_fontSizeBase` 这个字段**（`Label.cs:363` 自己也写着「影响小，但字段是错的」）。
⇒ 本件把正本那条当**「历史不改变结果」的兜底**保留，另加**第 ② 条**当判别器。

### §一·2 第 ② 条为什么必须走反射

`m_fontSizeBase` 是 `protected`（`TMP_Text.cs:473`），TMP **没有公开口**：读 `fontSize` 拿到的是
**自适应结果**（`TMP_Text.cs:464-468` getter 返回 `m_fontSize`），不是 base。
公开口绕道也试过、**都不成立**（写下来免得下个人再试一遍）：

- **「空文本窥 base」不成立**：`m_characterCount == 0` 的提前返回（`TextMeshPro.cs:4163-4172`）
  在**涨的那一支之后**（`:4139-4155` 先跑）⇒ 空串会一路涨到 `m_fontSizeMax`，看不到 base。
- **「把区间收窄到 ≤ 0.051 让涨不动」** 能窥到、但要人为把 `maxPx − minPx` 压到 0.52px 以内，
  太脆、也不是任何真实调用点的形状 ⇒ 弃。

⇒ 用反射直读那个字段。**工程内已有先例**：`Assets/RuleEngine/Editor/RuleEngineTest.cs:7823`（读 `RuleCore` 的非公开静态字段）。
顺带核过：本工程**只有一个 TMP 源**（`Packages/manifest.json:11` 只有 `com.unity.ugui: 2.0.0`，
全盘 `find -name TMP_Text.cs` 只命中 `Library/PackageCache/com.unity.ugui@27635d171b1a/...`）⇒ 字段名不会有两份。

### §一·3 ⚠️ 未跑 Unity（红线 1）

「这一条现在是绿的」是**按源码推的、不是实跑量到的**。同步点跑 `BattleScene.Run` 才有结论。
**如果它在同步点红了**，第一嫌疑不是「断言写错」，按这个顺序查：

1. 反射拿不到字段（前提断言会**先**红 —— 那说明跑的 TMP 不是这份源码）；
2. **复用那条的 `_tmp.fontSize = cur` 撞上 setter 的 `m_fontSize == value` 早退**（`TMP_Text.cs:467` 的第一段守卫）
   ⇒ base 刷不到。我按「`cur` 不在 1/20 量化格上、而自适应终值一定在格上」推**不会撞**（见「怎么改坏」第 1 条的算式），
   但这是**推的**：真撞上了要把探针的 `bPx` 换一个不在网格上的值再试。
3. `GetComponentInChildren<TMP_Text>` 拿不到组件（前提断言会先红）。

---

## 二、证据（`文件:行号`）

**判据（正本 + 我们自己的）**

- `d:/4/项目任务.md:474` —— A80 那一行（判别性写法的原文 + 「现在没有任何断言会红」的判定）
- `Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs:1927-1985` —— 既有 §4.6：读的是 `probe.FontSizeMax/FontSizeMin`，
  而那两个数是 `Label.SetAutoFitBox` **自己写进去的**（`cur × (maxPx|minPx)/nomPx`）⇒ 自证，两种时序逐位同结果
- `Unity/MyGame/Assets/CardPresentation/Battle/Label.cs:355`（`float cur = TmpFontSize();`）· `:370-374`（**A57③ 的那两步**）
  · `:472`（`SetTextTmp` 里另有一处 `_tmp.fontSize = …`）· `:363-369`（作者自己标了「影响小，但字段是错的」＋残留洞）
- `Unity/资料/已知的坑.md:547-579` —— 棘轮那一族；`:578-579` 的判据（带自适应的文字**必须**同时断「字号落在原版 auto 区间内」）

**TMP 源码**（`Unity/MyGame/Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/TMP/`）

| 事实 | 出处 |
|---|---|
| `fontSize` 的 setter **只在 `!m_enableAutoSizing` 时**才回写 `m_fontSizeBase`；且 `m_fontSize == value` 直接早退 | `TMP_Text.cs:464-468` |
| `m_fontSizeBase` 是 `protected`（无公开口） | `TMP_Text.cs:473` |
| 每次重排**起点** = `Clamp(m_fontSizeBase, m_fontSizeMin, m_fontSizeMax)` | `TextMeshPro.cs:2147-2152` |
| 缩（二分下半支） | `TextMeshPro.cs:3073-3089` |
| 涨（二分上半支，收在 `maxFontSize − minFontSize ≤ 0.051`） | `TextMeshPro.cs:4136-4155` |
| 空文本的提前返回在「涨」那一支**之后**（⇒ 用空串窥 base 不成立） | `TextMeshPro.cs:4158-4173` |
| `ComputeMarginSize` 只在 `OnEnable`/`GetTextInfo`/`OnValidate`/`OnRectTransformDimensionsChange` 里跑 | `TextMeshPro.cs:2015-2032` · `:2069` |

**别的**

- `Core/TmpFont.cs:208-214`（`SetWrapWidth` 先把 `sizeDelta` 写成 `(w, 0)` 再 `GetTextInfo`）· `:147-176`（`NewText` 把 TMP 建在**子** GO 上 ⇒ `GetComponentInChildren` 找得到）
- `Assets/RuleEngine/Editor/RuleEngineTest.cs:7823`（读非公开字段的先例）· `Packages/manifest.json:11`（唯一的 TMP 包）
- 样板：`Editor/ShellScene.cs:745-810`（R7 的前提断言写法）· `Editor/RewardsScene.cs:641-651`（同一份谓词算期望）

---

## 三、改动清单（改前 → 改后）

**只动一个文件：`Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs`**
（`git diff --numstat` = `131 1`，其中我这块 **+80 行 / −0 行**；剩下的是工作树里**本来就有的** A96 那段，见「四·顺手」）

### 改前
§4.6 在 `:1985` 收尾（`capProbe` 那一段），`:1986` 是外层 `}`，`:1988` 直接是 `driver.SimulateAiTurn();`。

### 改后：`§4.6b`（`:1986-2065`）原样

```
① 前提：反射拿得到 m_fontSizeBase（`TMP_Text.cs:473`）        —— Check
② 前提：两条探针都走 TMP 后端（点阵没有自适应）                —— Check
   两条探针（摆 y=99 画面外，量完 DestroyImmediate —— 同 §4.6）：
     AutoFitReuseProbe：18px → 30.6px，两步都给同一对 (框 221.6×29px, min/max 10/32)
     AutoFitFreshProbe：只有 30.6px 那一步
③ ★  复用 vs 新建的 FontPxNow 一致（容差 0.05px）             —— 正本要的那条（「历史不改变结果」的兜底）
④ ★  复用那条的 FontPxNow 落在原版区间 [10, 32]px 内          —— `已知的坑.md:578` 那条纪律
⑤ ★★ 两者 m_fontSizeBase 一致（容差 1e-3 fontSize 单位）      —— **判别 A57③ 的那条**
⑥ ★  …而且 base 就是第二次要的 30.6px（`FontSizeToPx` 换算）  —— 把它钉在**原版字段值**上，不是自证
```

- 三个数全是**外部事实**：`18 / 30.6 / 32 / 10` 里，`30.6` = 原版 `Timer Text` 的 `m_fontSize`、`10 / 32` = 它的 `m_fontSizeMin/Max`
  （与 §4.6 同源），框 `221.6 × 29px` 同上；`18` 只是「**另一个**名义字号」（取一个 ≠ 30.6 的即可，与 §4.6 的 min 档同数，纯属顺手）。
- **没碰** `Battle/Label.cs`、没碰 §4.6 既有四条、没碰 `~:5203` 起 A96 那一段 WfSlider 断言（读到了 = 最新版，原样保留）。

---

## 四、没查清的部分 + 顺手发现的东西

### 没查清

1. **①那条（正本写法）到底是不是绿的 —— 推的，没实跑。** 两条探针的**拟合输入逐项相同**
   （文字、框、`min/max`、base 都相同 ⇒ 同一个二分序列），所以我判绿。真红了的排查顺序见 §一·3。
2. **批处理下 `OnRectTransformDimensionsChange` 到底跑不跑 —— 没查出确证。**
   这决定「`SetWrapWidth` 里按 `(w, 0)` 量出来的 margin 会不会被后续 `sizeDelta.y = worldH` 刷新」
   （`TmpFont.cs:208-214` 先写 `(w, 0)` 再 `GetTextInfo`；刷新只可能来自 `TextMeshPro.cs:2069`）。
   ⇒ 它只影响**绝对拟合值**，不影响本断言的**比较**：两条探针的尺寸变更序列相同
   （不跑 ⇒ 都用 `(w, 0)` 那份；跑 ⇒ 都在最后一次 `(w, h)` 上）⇒ margin 状态一致。
   ⚠️ 但这条**值得记**：如果它不跑，那所有 autofit 文字的**纵向**判据都在用「高=0」的 margin —— 与
   「模式卡标题能量到 36.34px 这种中间档」这一实测现象对不上，**说明它大概是跑的**，我没定论。
3. **`_iconBoost` 恒 1**（`CardIcons.FontScaleFor` 现在恒返回 1）—— 本条断言不依赖它；哪天它不恒 1 了，
   两条探针仍同时乘同一个系数 ⇒ 比较部分不受影响。

### 顺手发现（不在本件范围，按规矩只报不改）

1. 🔴 **「空文本窥 base」这条路是死的**（`TextMeshPro.cs:4139` 的涨支对空串也跑、`:4163` 的空串返回在它后面）
   —— 上面已写；留给下一个想用公开口观测 `m_fontSizeBase` 的人，省一轮。
2. **工作树里 `BattleScene.cs` 还带着 A96 那段的未提交改动**（`~:5203` 起：轨道高 12.00 的更正 + 3 条断言）。
   我那一块与它**文件不相交**（一个在 §4.6b、一个在 §14c），原样保留了。
3. `Label.SetAutoFitBox` 的**残留洞**（作者自己写的：`fontSize` setter 的 `m_fontSize == value` 早退
   ⇒ 第二次若 TMP 正好停在 `cur`，base 刷不到）—— 本件**没有**为它写断言（它与 A57③ 是两件事，
   且「故意拨一个别的值再拨回来」会造成一次多余重排，作者已判「不做」）。本断言的探针**不会**撞上它（§一·3 第 2 条）。

---

## 五、怎么改坏这条断言就会红（三条，证明不是自证）

| # | 改坏哪里 | 哪条红 | 数 |
|---|---|---|---|
| 1 | `Battle/Label.cs:370` 的 `_tmp.enableAutoSizing = false;` **删掉**（或把 `:371` 的 `_tmp.fontSize = cur;` 挪到 `:374` 的 `enableAutoSizing = true;` 之后） | **⑤**（★★） | 复用那条的 base 停在第一次那档 **18.0px**，新建的是 **30.6px** ⇒ 差 ≈ **12.6px**（≈1.23 fontSize 单位） |
| 2 | `Battle/Label.cs:355` 的 `float cur = TmpFontSize();` 换回 `float cur = _tmp.fontSize;`（2026-09-25 之前的**棘轮**写法） | **⑤**（★★★★） | 复用那条第二轮拿到的是**上一轮的自适应结果** ⇒ base 与新建那条分叉 |
| 3 | `Battle/Label.cs:371` 的 `_tmp.fontSize = cur;` **整行删掉** | **⑤** | 第二次调用时 base 永不更新 ⇒ 复用那条停在第一次的 **18.0px**，新建那条由 `SetTextTmp`（`:472`，那时自适应还是关的）写成 **30.6px** ⇒ 分叉 |

- ⚠️ 三条都落在 **⑤** 上 —— 这不是巧合：**A57③ 改坏之后可见的错位只有 `m_fontSizeBase`**（§一·1）。
  反过来说，**① 那条要红得换另一族缺陷**：把 `:355` 的 `cur = TmpFontSize();` 换成 `_tmp.fontSize` **并且**
  把 `:372` 的 `fontSizeMax` 改回 `cur`（= A50① 之前的老写法）⇒ **① 与 §4.6 既有第 2 条一起红**。
- **为什么这不是自证**：期望的**不是**「我们写进去的那个数」，而是**另一条 label 的内部状态**
  （⑤ 是「两个 label 的 base 相等」，⑥ 才是钉原版字段值 30.6px 的那半）——
  改坏实现 ⇒ 两条 label 分叉 ⇒ 红；两条 label 都不变 ⇒ 绿。
- ⚠️ **第 1、2、3 条改坏时，§4.6 既有那四条（读 `FontSizeMin/Max` 的）仍然全绿** ——
  这正是 A80① 说的「现在没有任何断言会红」，也就是本件要堵的那个洞。

---

类型检查：运行时 0 / 编辑器 0
