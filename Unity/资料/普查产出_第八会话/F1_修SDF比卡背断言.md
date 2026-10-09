# F1 · 修 `BattleScene.cs` 那条「SDF 比卡背」断言（量纲混用）

> 执行代理 F1 · 2026-10-19 · 唯一改动文件 = `Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs`（纯 CRLF，改完仍是纯 CRLF）。
> ⛔ 没跑 Unity · ⛔ 没碰 git 写操作 · ⛔ 没改两张正本 · ⛔ 没动 `BattleDriver.cs`。
> 判据来源 = `普查产出_第八会话/X2_诊断SDF比卡背红.md`（只读诊断，我**现核过它的前提**，见 §④）。

---

## ① 改前 / 改后（整段）

**改前**（原 `:1305-1307`，现在对应 1305 + 1306-1307）：

```csharp
                    float rw = dsdf.WorldW / pile.WorldW, rh = dsdf.WorldH / pile.WorldH;
                    Check(Mathf.Abs(rw - 2.9212f / 2.1739f) < 0.02f && Mathf.Abs(rh - 3.8122f / 3.1364f) < 0.02f,
                          $"★ SDF 比卡背大 **1.34376 × 1.21548**（原版两个 sizeDelta 之比）—— 实得 {rw:F5} × {rh:F5}");
```

**改后**（新 `:1305-1323`，`BattleScene.cs` 现读）：

```csharp
                    float rw = dsdf.WorldW / pile.WorldW, rh = dsdf.WorldH / pile.WorldH;
                    // 🔴 **2026-10-19（F1 修红）**：这条原来**量纲混用** —— 分子 `dsdf` 是 `MakeDeckSdf` 建的，
                    //   用 `DeckSdfPx` ＋ 节点自己的比例 ⇒ 它那个 quad 里存的**就是框**（`_SDF` 那批两轴
                    //   padding 代数和 = 0）⇒ **框 == 画心**；而分母 `pile` 从 **B2** 起由 `FitDeckPile` 写的是
                    //   **画心**（`h = fh × (1020−padB−padT)/1020`）。⇒ 实得 = **SDF(框) ÷ 卡背(画心)**，
                    //   与「两个 `sizeDelta` 之比（框 ÷ 框）」**本来就差一个 `texRect/m_Rect`**。
                    //   根因：**卡背那批 sprite 的 padding ≠ 0（233/233），`_SDF` 那批 = 0**（`CardbackFace` 文件头）。
                    //   ⚠️ 期望值**随「这一局用哪张卡背」变** —— 四张阵营默认卡背的 `eh` 现算从 **1.21553**
                    //      （TL，`textureRect` 满高）漂到 **1.33324**（GOF）⇒ ⛔ **不能写成一个新常量**。
                    //   分母 707×1020 = 卡背那 233 张**恒定**的 `m_Rect`（`Cardbacks.json` 233/233 都是它）；
                    //   分子取**贴图自己**（我们的 PNG 就是 `textureRect` 裁片，同一张表逐张载的）
                    //   —— 独立于 `CardbackTable` / `CardbackFace.Fit`，⛔ 不问被测实现。
                    //   🧨 **改坏法**：把 `FitDeckPile` 里那个 `sh = (1020−padB−padT)/1020` 去掉（画心变回框）
                    //      ⇒ **本条必红**（正是 2026-10-19 这次红的成因）。
                    float kx = pile.Texture.width / 707f, ky = pile.Texture.height / 1020f;
                    float ew = (2.9212f / 2.1739f) / kx, eh = (3.8122f / 3.1364f) / ky;
                    Check(Mathf.Abs(rw - ew) < 0.02f && Mathf.Abs(rh - eh) < 0.02f,
                          $"★ SDF(**框**) 比卡背(**画心**) 大 **{ew:F5} × {eh:F5}**"
                        + $"（原版框之比 1.34376 × 1.21548 ÷ texRect/m_Rect）—— 实得 {rw:F5} × {rh:F5}");
```

- **量法 / 容差 0.02 / 期望值算式**一律照 `X2 §④` 的推荐写法，一个符号没改。
- 改动只有两处①期望值改**现算**；②文案从「（原版两个 `sizeDelta` 之比）」改成「**SDF(框) 比卡背(画心)**」＋把算式写进文案。
- 断言**数量不变**（仍是同一处 `Check`），所以 `BattleScene.Run` 的**断言合计应与基线一致**（2402 那条口径下不会多也不会少）。
- ⛔ **没动**同一块里的其它四条断言（`_sdf` 贴图名 / 原版 shader / z 层次 / 牌堆 SDF 存在），整段其它行逐字未变（见 §④ 的行号核对）。

---

## ② 期望值现算的推导（我**现核**的，不是转述 X2）

**代数**：`FitDeckPile`（`BattleDriver.cs:13041-13053`）写进 `pile` 两个 quad 字段的是**画心**：
`sh = (1020 − padB − padT)/1020`（`:13049` 注释）、`sw = (707 − padL − padR)/707`。
`MakeDeckSdf`（`:13060-13078`）给 SDF 那颗的是**框**：`worldHeight = Px(DeckSdfPx)` ＋ `SetAspect(2.9212/3.8122)`。

⇒ `rw = 框W / 画心W = (原版框之比) ÷ sw`、`rh = (原版框之比) ÷ sh`。

**关键：`sw/sh` 就是「我们那张 PNG 的宽高 ÷ 707/1020」** —— 因为 `Resources/Art/cardbacks/<名>.png`
本来就是原版 `textureRect` 的**裁片**（`Core/CardbackFace.cs:18-19` 明写），而 `Cardbacks.json` 里
`padL/padR/padB/padT` 就是 `m_Rect − textureRect`（同文件 `:39-40`）。

**现读实测**（本会话亲读，两处独立）：
`Resources/Art/cardbacks/Cardback_BL_Campaign_Free.png` 的 PNG 头 = **697 × 968**；
`Cardbacks.json` 那条 = `rectW 707 · rectH 1020 · padL 0 · padR 10.0761 · padB 21.0761 · padT 31.0761`
⇒ 理论裁片 `696.9239 × 967.8478` ⇒ **PNG 取整只差 0.076 / 0.152 px**（比值上 ≤ 1.6e-4）。

| 阵营默认卡背 | 我们 PNG（现读） | `kx` | `ky` | `ew` | `eh` |
|---|---|---|---|---|---|
| `TL_Campaign_Free`（Leviathan） | 707×1020 | 1.000000 | 1.000000 | 1.34376 | **1.21547** |
| **`BL_Campaign_Free`（本局 `battle.log` 用的就是它）** | **697×968** | 0.985856 | 0.949020 | **1.36304** | **1.28076** |
| `UM_Campaign_Free` | 707×940 | 1.000000 | 0.921569 | 1.34376 | 1.31891 |
| `GOF_Campaign_Free` | 646×930 | 0.913720 | 0.911765 | 1.47065 | 1.33310 |

（四张都取自 `Cardbacks.json` 的 `defaults`，与 `X2 §④` 那张表同源；我这边 `eh` 与 X2 差在第 4 位，
因为 X2 用的是 `textureRect` 小数、我用的是**PNG 取整后的整数** —— 断言也走整数这一路。）

**这个红被判定的两道余量（现算）**：

| | 旧常量差 | 新期望差 |
|---|---|---|
| x：`1.36319` | `|1.36319 − 1.34376| = 0.01943`（**擦着 0.02 过**） | `|1.36319 − 1.36304| = **0.00015**` |
| y：`1.28100` | `|1.28100 − 1.21547| = 0.06553`（**就是它顶红的**） | `|1.28100 − 1.28076| = **0.00024**` |

⇒ 新期望的两轴余量都是量的 **1/80 以下**，**0.02 绰绰有余；⛔ 没有放宽容差**（仍是 `0.02f`，逐字未动）。
`X2` 提的「PNG 取整 ≤1e-3 ＋ `DeckSdfPx` 0.12% ≈ 1.5e-3」与实测同量级。

**期望值随卡背漂动的证据**：四张默认卡背的 `eh` = `1.21547 / 1.28076 / 1.31891 / 1.33310`
（跨 0.118，是容差 0.02 的 **5.9 倍**）⇒ 换一个阵营打，旧口径必红、且新口径必须现算。这条**否掉了「换一个新常量」**的改法。

---

## ③ 改坏法（怎么一眼证伪）

**改坏法 A（写在注释里那条）**：把 `BattleDriver.FitDeckPile` 的 `:13049` `q.SetWorldHeight(Px(h))` 里的 `h`
换成 `fh`（= 去掉 `sh = (1020−padB−padT)/1020` 那一段，画心变回框）⇒ `pile.WorldH` 变大 ≈ 5.4%
⇒ `rh` 从 1.28100 掉回 ≈ 1.21547 ⇒ `|rh − eh| ≈ 0.065` **必红**（且 x 侧 `rw` 也会掉到 ≈ 1.34376 ⇒ `|rw−ew| ≈ 0.019`，
**x 那条可能仍在 0.02 内** —— 所以判红只能靠 y，这正是**旧断言当初红的方式**，可作反查）。
**恢复** = 把 `sh` 乘回去。

**改坏法 B（证「现算那一项真的在起作用」）**：把 `kx/ky` 改成恒 `1f`
⇒ 期望退回旧常量 ⇒ 本局**必红**（`eh` 差 0.0653）。这条比 A 更直接地钉住「换卡背会漂」这件事。

**改坏法 C（证容差没被偷偷放宽）**：把 `kx/ky` 里那个 `707f/1020f` 换成任意别的底数（例 `700f/1000f`）
⇒ `ew/eh` 偏移 >> 0.02 ⇒ 必红。**这条同时说明：容差确实仍是 0.02，没被我调过。**

⛔ 以上三条都**没跑**（我是执行代理，不跑 Unity）—— 它们写在这里是给收口那次复跑/后续复查用的**可证伪清单**。

---

## ④ 现核 + 读数（我**没有**盲信 X2）

### 4·1 「诊断的前提不成立就停手」—— 我逐条现核过，**四条全部成立**

| X2 的前提 | 我现核的办法 | 结果 |
|---|---|---|
| `ImageQuad.Texture` 已公开、能取 `width/height` | 读 `Battle/ImageQuad.cs:138`（`public Texture Texture`）；`:124` 建时 `_tex = tex` | ✅ `Texture`（基类）自带 `width/height`；`:1297` 那句 `pile.Texture == null` 早就在用 |
| `pile.Texture` 就是**卡背裁片 PNG**（不是 `m_Rect` 全图） | `CardArt.CardBack`（`Core/CardArt.cs:323-338`）→ `Cosmetic`（`:642-646`）= `Resources.Load("Art/cardbacks/<名>")`；再量 PNG 头 | ✅ `BL_Campaign_Free` PNG = **697×968**，与 `textureRect` 696.92×967.85 只差取整 |
| 那批 PNG 不会被导入器改尺寸 | 读 `Resources/Art/cardbacks/Cardback_BL_Campaign_Free.png.meta` | ✅ `maxTextureSize: 2048`，源图 697×968 **不触发缩放** |
| 卡背 233 张的 `m_Rect` 恒 707×1020（所以底数能写死） | 读 `Resources/Cardbacks.json` 的 `items`（233 条）＋ 抽查 2 条 | ✅ 头两条 `rectW 707 · rectH 1020`；X2 §⑤ 已给「233/233」的普查 |
| 「分母 = 画心」 | 读 `BattleDriver.cs:13041-13053`（`SetWorldHeight(Px(h))`，`h = fh × sh`） | ✅ 与 X2 §① 逐字一致 |
| 「分子 = 框」 | 读 `BattleDriver.cs:13060-13078`（`DeckSdfPx` ＋ `SetAspect(2.9212/3.8122)`）＋ `:1241/1254/1265` | ✅ `_SDF` padding 代数和 = 0（`CardbackFace.cs:55-59` 也是同一结论） |

### 4·2 类型检查（`TMPDIR=/tmp/wf_f1`，独立）

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

⚠️ **第一次跑是红的（4 个 `CS0103`：`rw` / `rh` 找不到）—— 是我自己的错**：第一版 Edit 把
`float rw = dsdf.WorldW / pile.WorldW, rh = ...;` 那一行**一起吃掉、忘了写回去**（`old_string` 里带了它、
`new_string` 里没有）。**同一分钟内补齐后重跑 = 0/0**。
记录这条是因为它是**本仓的已知形状**：改断言时「顺手」带走一行声明，**只有秒级类型检查能挡**
（`CS0103` 在这条断言里不会静默 —— 若它编译得过，那才会变成「绿得莫名其妙」）。
同批**没有**任何错误落在别人正在改的文件上（F1 这一趟独立 `TMPDIR`、未受干扰）。

### 4·3 `git diff --numstat` ＋ 行尾

```
416     17      Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs
```

- ⚠️ 这是**整篇 vs 索引**的数（该文件**在我动手之前就已经是 `M`**，含 B2/别的会话的在飞改动）
  ⇒ **不能读成「我改了 416 行」**。
- **我这一处 hunk 单独量**：`git diff -U0` 取含 `sizeDelta 之比` 的那个 hunk = `@@ -1306,2 +1306,18 @@` ⇒ **+18 / −2**
  （`float rw …` 那行与前后文逐字相同，git 把它当上下文、没算进去）。
- **行尾**：改前 `CRLF 20317 / LF 20317`（= 纯 CRLF，自己复量过）→ 改后 `CRLF 20333 / LF 20333`
  ⇒ **仍是纯 CRLF、一个字节没翻**（两数相等即「所有 `\n` 都是 `\r\n` 的一半」）。
  ⛔ 全程**没用 `sed -i`**、没整篇重写（走 Edit 工具，只替换那一段）。
- 文件大小 `1 703 881 B → 1 705 772 B`（+1891，即新增的注释/代码字节）。

### 4·4 改完的行号自检

现读 `:1303-1324`：`:1303` `if (pile != null …)` · `:1304` `{` · `:1305` `float rw…` ·
`:1306-1318` 注释 · `:1319` `kx/ky` · `:1320` `ew/eh` · `:1321-1323` `Check` · `:1324` 下一条断言原样接上。
**下面那四条断言（`_sdf` 名 / 原版 shader / z / SDF 存在）行号与内容都没变。**

---

## ⑤ 没查清的部分 / 边界 / 需要哪几条自检覆盖

### 5·1 已知边界（**不是**没查，是这条路今天不走但**该记**）

- 🔴 **`CardArt.CardBack` 有一条降级路**：`Cosmetic(默认卡背名)` 取不到图时，它会退回
  `Resources/Art/cards/back_<阵营>.png`（`Core/CardArt.cs:337`，本会话现读**那 4 张确实在盘上**：
  `back_ember / back_goff / back_tide / back_ultramarines`），并 `Debug.LogWarning`。
  **那 4 张不在 `Cardbacks.json` 里 ⇒ `CardbackFace.Fit` 走兜底分支（`rw/rh = 贴图自己`、`padding = 0`）
  ⇒ `sh = 1` ⇒ 那个 quad 里存的又变回「框」**。此时 `kx/ky` 若照旧反推，会得到**偏小的期望值 ⇒ 假红**。
  **触发条件**：`Art/cardbacks/` 那批图**丢失**（⚠️ 本仓已知风险：`Resources/Art/**` 在 `.gitignore` 里，
  素材腿一跑、git **一声不响**）。**今天不触发**（233 张都在盘上、本局走的就是它）。
  ⛔ 我**没有**为此改断言（照 `X2 §④` 的写法，⛔ 不为一条今天到不了的路加耦合）。
  **要收口的话有现成办法**：`CardbackFace.TryRect(pile.Texture.name, …)` 返回 `false` 时把 `kx/ky` 当 `1f`
  —— 一行条件、且仍是**数据查表**不是问被测实现。**留给主对话裁**。
- 🔴 **`ImageQuad.WorldW/H` 不是渲染测量**（`= _worldH * _aspect`，`ImageQuad.cs:107-108`）——
  本断言比的是「我们塞进去的几何」，这份明账**本轮没动**（`B2 §4·3-4` / `B6 §5·1-1` 同口径）。
- ⚠️ **`X2 §②` 末尾那条顺带订正我没动**：`BattleDriver.cs:1247/13029` 注释里「@容器 scale 100 ⇒ 217.39×313.64 px」
  在这层 prefab 里不成立（那三节点 `m_LocalScale` 全是 1）。**它属于 `BattleDriver.cs` = 我的黑名单**，
  且与本红无关 ⇒ 只在这里提一句，**由主对话决定派给谁**。
- ⚠️ **`X2 §④` 的「可选第二条断言」（让 `BattleDriver` 把框也留口、另断一条「框 ÷ 框」= 1.34376 × 1.21548）
  —— 本件按简报要求【没做】**：那要动 `BattleDriver.cs`，不在我白名单。它的好处（红的时候一眼看出坏的是哪一半、
  且能顺带覆盖 5·1 那条降级路）仍然成立，**建议主对话单独派一件 / 或者就挂账**。
- ⚠️ **「画心 = 按 padding 内缩那一块」仍是推的**（`Sprites.DataUtility.GetOuterUV/GetPadding` 是 native）；
  `X2 §③` 列的三条独立证据 + `CardbackFace.cs:30-44` 都指向它。**若真 Play 哪天推翻**，
  本断言要**连同 `CardbackFace` 的第二段一起退**（那时旧常量会重新成立）—— 这条**不在我这件里**，仅留指路。

### 5·2 没查清的

1. **本局的阵营为什么是 BlackLegion**：我只从 `battle.log:3958` 现读到「SDF 贴图 = `Cardback_BL_Campaign_Free_sdf`」，
   没去追 `BattleScene.Run` 建的那一局 `_myFaction` 是从哪来的（与本红无关，`kx/ky` 是现算的、无影响）。
2. **`kx/ky` 与 `CardbackFace` 表里 `sw/sh` 的差** = **≤1.6e-4**（PNG 取整），我**只量了这一张**（BL）；
   其余 232 张没逐张比 —— 但 `eh/ew` 的余量是 ~2e-4 级 vs 容差 0.02，**留了两个数量级**，不构成风险。

### 5·3 需要哪几条自检覆盖（**我没有跑**，按铁律 12 留给主对话）

- 🔴 **必跑 `BattleScene.Run`**：本件是它的**断言宿主**（`Editor/BattleScene.cs` = 该条唯一的宿主）
  ⇒ 这一条就能验。期望：**断言合计回到基线、那条红消失**（`X2` 记的口径是 `2428 通过 / 1 失败` ⇒ 应为 `2429 / 0`，
  ⚠️ 但**断言合计数以主对话手上那份基线为准**，我这边只能给「少一条红」这个判据）。
- ⚠️ **不必跑 `CollectionScene.Run` / `DeckScene.Run`**：本件**只动了一条断言**，
  没碰 `Core/`（`CardbackFace`/`CardbackTable` 一个字没动），那两条的 `1466/1466`、`1374/1374` 不可能受影响。
  （`X2 §⑥` 也确认那两条全绿、本红是**唯一一处没同步**的。）
- ⚠️ **`RuleEngineTest.Run` 那 2 条红与本件无关**（近战/远程翻倍，`X2 §⑥` 已记另一路在改）。
- 📌 **纯断言改动**，不碰引擎/共用件 ⇒ **不构成跨面**，⛔ 不必上全套 12 条。
