# W58 · 战斗线「场上四个数值 · 精确 thick 0.12 字形」

> 执行代理 `W58-THICK012`（2026-10-20）。判据输入 = 同目录 `W58_战斗线_场上词条判据.md` §2·1/§四 #5
> 与 `W58_战斗线_场上词条落地.md` §四 5（「字形表只有 0.05/0.097」）。
> ✅ 未跑 Unity、未动 git、未跑任何 `*.Run`、未截图（铁律 12）；只跑了生成器与秒级类型检查。
> ⛔ `Editor/BattleScene.cs` **一个字节没碰**（本批有别的代理在飞：`git diff --stat` = 973/36，几十个 hunk）
> —— 断言的可粘贴块见 §四。

---

## 一、结论（4 句）

1. **原版那 0.12 是【TMP 材质参数】**（`_OutlineWidth 0.12`），但**我们的管线不跑 TMP** —— 卡面数字是
   **CPU 预烘成一张字形表**（`Core/PragatiDigits.Data.cs`：每像素 2 字节 = 字面覆盖 / 描边覆盖）
   ⇒ 对我们来说 0.12 只能落成**第三档预烘字形** `thick3d`，加一个运行时材质参数**画不到**这些数字上。
2. **已加 `thick3d = 0.12` 一档并重生成字形表**：场上四个数值（`CardView.StatVariant` 的 board 分支）
   全部走 `Thick3D`；2D 卡面（手牌/详情/卡池）**不动**（护甲仍是 `thick 0.097`，其余 `thin 0.05`）。
3. 🔴 **顺手查出并修掉一条更严重的缺陷**：生成器把 SDF 的距离标尺写成了 `/ SS`（**量纲错 16 倍**）
   ⇒ 改前 `0.05 / 0.097 / 0.12` 三档的 **`face` 通道逐字节相同**，描边层则**饱和成一整块不透明矩形**
   （运行时 alpha = `max(face, outline)`：两档 **4340/4340 个像素都 `>0`**，thin 最低 74、thick 全 255）
   —— **每个数字背后都是一个黑方块**（`CardSprite` 预乘 alpha，照实画出来）。
   不修这一行，0.12 就是**假的一档**。
4. 修法 = 两行：`sc` 改回 `×(DIGIT_H/m_Height)×SS`；降采样 `LANCZOS → BOX`（消掉 LANCZOS 在字边
   ring 出来的孤立噪点）。修后三档 ink 单调递增：**thin 2752 / thick 3142 / thick3d 3254**。

---

## 二、0.12 的实现形态（材质参数 or 字形纹理 + 判据）

**原版 = TMP 材质参数**。逐字段实读
`d:/2/新解包资源/assets_full/bundle_fonts_assets_all/Material/Material_-1760077249967186717.json`
（`m_Name = "Pragati-Regular White_thick Offset for 3D"`）：

| 字段 | 值 |
|---|---|
| `_OutlineWidth` | **0.11999999731779099** |
| `_FaceDilate` | **0.11999999731779099** |
| `_ColorMask` | 15 |
| `_OutlineColor` | (0, 0, 0, **1.0**) |
| `_GradientScale` | 21.0 |
| `_ScaleRatioA` | 0.9523809552192688 |

场上四个数值的 TMP 共用这一份（判据：`W58_战斗线_场上词条判据.md` §2·1「数值节点 → 四份 TMP
`m_sharedMaterial` 全 = `Material_-1760077249967186717`」）。
对照 2D 卡面的两档（同一个包）：
`Material_-8115038251467144366` = `Atlas Material Thin Outline` **0.05 / FaceDilate 0.05**；
`Material_-455814643003338116` = `Atlas Material Thick Outline` **0.097 / FaceDilate 0.105**。

**我们 = 预烘字形纹理**。判据：
- `Core/PragatiDigits.cs:52`（Load）逐像素读 `face / outline` 两个字节；
- `CardView.cs` 的 `StatTexture` / `InfoTexture` 都是往 `Color32[]` 里 `PragatiDigits.Draw`，**没有 TMP**；
- 生成器 `工具/gen_pragati_digits.py` 的 `OUTLINES` 就是「烘进去的 `_OutlineWidth`」表。

⇒ 结论：**不是加一个运行时材质参数就行**；必须在字形表里多烘一档。

---

## 三、改法（文件 + 符号）

| 文件（绝对路径） | 符号 / 改动 |
|---|---|
| `d:/4/Unity/工具/gen_pragati_digits.py` | `OUTLINES` 追加 **`"thick3d": 0.12`**（只加在末尾；行号 = 表内 y，插中间会让已有两档坐标平移）· CS 模板新增 **`OutlineWidth(string variant)`**（把烘的时候用的数写进生成物，供自检读，**不自证**）· 新增 **`--check`**（内存产物与盘上逐字节比、零写盘、不一致退 1）· 🔴 **修 `sc`**：`/ SS` → `* SS`（量纲）· 降采样 `LANCZOS` → `BOX` |
| `d:/4/Unity/MyGame/Assets/CardPresentation/Core/PragatiDigits.cs` | 新增 `enum Variant { Thin, Thick, Thick3D }` · `Tag(Variant)` · `OutlineWidth(Variant)` · `HasVariant(Variant)` · `InkPixels(char, Variant)` · `Measure/Draw` 的 `Variant` 重载（旧的 `bool thick` 重载保留、转发）· `Find(char, Variant)`。`Glyph.variant` 注释补 `thick3d` |
| `d:/4/Unity/MyGame/Assets/CardPresentation/Core/CardView.cs` | `StatThick(int,bool)` → **`StatVariant(int,bool)`**（`board ⇒ Thick3D`；2D 仍是护甲 `Thick`、其余 `Thin`）· `DrawStatAt(..., PragatiDigits.Variant, ...)` · `InfoTexture` 四处 + `StatTexture` 一处调用点 · 新增公开读口 **`BoardStatVariantTag(int)`** · 三处注释就地订正（不再写「本批不改生成物」） |
| `d:/4/Unity/MyGame/Assets/CardPresentation/Core/PragatiDigits.Data.cs` | **生成物**，已重跑生成器（`SheetW 230 / SheetH 68→102`、`Rows 20→30`、新增 `thick3d` 10 行 + `OutlineWidth`）。`.meta` **已存在**（`PragatiDigits.Data.cs.meta`，59 B）⇒ 没有缺 meta 的新资产 |

**2D 卡面为什么没变**：`StatVariant(s, board=false)` 仍返回 `Thick`（仅护甲）/ `Thin`（其余）；
`InfoTexture` 里 2D 那条路（`statsOnly=false`）拿到的档与改前逐字节相同。

---

## 四、断言（`Editor/BattleScene.cs` 在飞 ⇒ 只给可粘贴块）

**插入位置（符号）**：`⑤·b` 块（`sd = CardData.Simple("StatProbe", …)`）里，
紧跟在 `Check(sv.StatShown(CardView.StatMelee) == 3 && … "…（isUnit=… · Pragati=… · face=…）");`
**之后**、`Check(sv.StatFlash(CardView.StatHealth) == Color.white, "刚建出来没有闪现色");` **之前**。
`Check` / `sv` 都已在作用域里，不用另建对象。

```csharp
                // 🆕 2026-10-20（W58 · THICK012）：**场上四个数值的描边 = 原版 0.12**。
                //  判据：四份 3D TMP 的 `m_sharedMaterial` 都是
                //  `Pragati-Regular White_thick Offset for 3D`（`Material_-1760077249967186717`：
                //  `_OutlineWidth 0.12` / `_FaceDilate 0.12`）。2D 卡面是另外两档（thin 0.05 / thick 0.097）。
                //  ⚠️ 我们走**预烘字形**：0.12 烘在 `Core/PragatiDigits.Data.cs` 里
                //  （生成器 `工具/gen_pragati_digits.py` 的 `OUTLINES`），不是运行时材质参数。
                //  灭自证（三条结构上互不代偿）：
                //    ① 把 `CardView.StatVariant` 的 board 分支改回 `Thick`(0.097) ⇒ 第二条必红；
                //    ② 把生成器 `OUTLINES` 的 thick3d 改回 0.097 再重跑 ⇒ 第一条必红；
                //    ③ 两处一起改回去 ⇒ 第三条「thick3d 的墨迹必须比 thick 多」必红（那是烘出来的像素，不是常量）。
                Check(Mathf.Abs(PragatiDigits.OutlineWidth(PragatiDigits.Variant.Thick3D) - 0.12f) < 0.0005f,
                      "★ 场上数值的字形档 thick3d 描边 = **0.12**（原版 `White_thick Offset for 3D` 的 `_OutlineWidth`）"
                    + $" —— 实测 {PragatiDigits.OutlineWidth(PragatiDigits.Variant.Thick3D):F4}（2D 的 thin/thick = "
                    + $"{PragatiDigits.OutlineWidth(PragatiDigits.Variant.Thin):F4} / "
                    + $"{PragatiDigits.OutlineWidth(PragatiDigits.Variant.Thick):F4}）");
                string statTagBad = "";
                for (int si = CardView.StatMelee; si <= CardView.StatHealth; si++)
                    if (CardView.BoardStatVariantTag(si) != PragatiDigits.Tag(PragatiDigits.Variant.Thick3D))
                        statTagBad += $" [stat{si}:{CardView.BoardStatVariantTag(si)}]";
                Check(statTagBad.Length == 0,
                      "★ 场上四个数值**全部**走 thick3d（0.12），不是 2D 那两档"
                    + $" —— 不在 0.12 档上的格：{(statTagBad.Length == 0 ? "无" : statTagBad)}");
                Check(PragatiDigits.HasVariant(PragatiDigits.Variant.Thick3D)
                      && PragatiDigits.InkPixels('0', PragatiDigits.Variant.Thick3D)
                         > PragatiDigits.InkPixels('0', PragatiDigits.Variant.Thick),
                      "★ 字形表里**真烘出了** thick3d 一档，而且比 thick 粗（不是只改了常量）"
                    + $" —— ink('0'): thin={PragatiDigits.InkPixels('0', PragatiDigits.Variant.Thin)} / "
                    + $"thick={PragatiDigits.InkPixels('0', PragatiDigits.Variant.Thick)} / "
                    + $"thick3d={PragatiDigits.InkPixels('0', PragatiDigits.Variant.Thick3D)}");
```

---

## 五、生成物数字

**改前（`git show HEAD` 的 `PragatiDigits.Data.cs`）**：

| 项 | 值 |
|---|---|
| 文件 | `Core/PragatiDigits.Data.cs` · **45,934 B** · md5 **`2035d6bac87a39ec0e92cc01dc8106e1`** |
| 表尺寸 / 行数 | `SheetW 230 × SheetH 68` · **20 行**（thin/thick 各 10） |
| 中间产物 | `_tmp_view/pragati_digits/{png,json,txt}` **改前不存在** |
| 解出来的 `0` 字格（15×28） | thin / thick 的 `face` **逐字节相同**（10 个数字全部如此）；`0` 的 `outline > 128` 像素 = **420 / 420**；运行时 alpha = `max(face, outline)` 的**零透明像素 = 4340/4340**（thin 最低 74、thick 全 255）⇒ 每个数字背后都是一整块不透明矩形 |
| ink 像素（10 个数字合计） | thin **4340** · thick **4340**（= 完全没区别） |

**改后**：

| 项 | 值 |
|---|---|
| `Core/PragatiDigits.Data.cs` | **68,980 B** · md5 **`c9024f1214eb9d9c5176e346f7cfb7f6`** · `SheetW 230 × SheetH 102` · **30 行** · 含 `OutlineWidth`：thin 0.05 / thick 0.097 / thick3d 0.12 |
| `_tmp_view/pragati_digits/pragati_digits.png` | **11,998 B** · md5 `028bb965f65cbb57e178418f22186865` · (230, 102) |
| `_tmp_view/pragati_digits/pragati_digits.json` | **4,253 B** · md5 `d4ea840176f722f107348d25e852160f` |
| `_tmp_view/pragati_digits/pragati_digits.txt` | **1,147 B** · md5 `98ba86b8c374b26adc39c3b7109dc3eb` |
| ink 像素（`alpha>0`，10 个数字合计） | thin **2752** · thick **3142** · **thick3d 3254**（单调递增 ⇒ 0.12 真的比 0.097 粗） |
| `--check` | 四份**逐字节一致**，`--check 通过（幂等）`，退出码 **0**（连跑两次）。⚠️ PNG 那一份比的是**字节**，换 PIL/zlib 版本可能误报；`Data.cs` / `json` / `txt` 三份是纯文本，跨机器稳定 |

`git diff --numstat`（行尾未翻：`CardView.cs` 是纯 CRLF 8366/8366，其余纯 LF）：
`gen_pragati_digits.py 81/25` · `PragatiDigits.cs 71/8` · `CardView.cs 336/28` · `PragatiDigits.Data.cs 30/3`。

---

## 六、没查清的部分（⛔ 没猜）

1. **`_FaceDilate 0.12` 没有建模**。原版 3D 材质同时有 `_OutlineWidth 0.12` 与 `_FaceDilate 0.12`；
   我们的生成器**一直只烘 `_OutlineWidth`**（thin/thick 两档也一样：2D thick 的 FaceDilate 是 0.105，
   生成器里只用了 0.097）。`_FaceDilate` 会让**字面本身**变粗，要不要补烘、补多少，**没有本批判据**。
2. **字形格紧贴墨迹 ⇒ 向外的描边被裁掉一圈**。取样用的是 TMP atlas 的 `m_GlyphRect`（= 墨迹矩形），
   SDF 的 padding 没进来；0.12 折合约 **1.1 个目标像素**，最外一圈会缺。
   要补得给 `Glyph` 加 `pad` 并在 `Blit` 里左移 tile、同时改 ink 居中；**本批没做**（会动既有排布）。
3. **`_ScaleRatioA 0.9524` 没乘**。TMP 真算式是 `_OutlineWidth × _ScaleRatioA`；
   生成器沿用既有口径 `_OutlineWidth × _GradientScale`（差 **4.8%** ≈ 0.05 目标像素）。
   三档都按同一口径，rank 不变；要逐像素对齐得连 thin/thick 一起改。
4. **没有实机/并排验收**（红线：不跑 Unity、不截图）。断言的「粗一档」是像素统计，**不是肉眼看像不像**。
5. 🔴 **本次修 `sc` 会改变所有数字的外观**（去掉黑方块、边缘变利），**超出「只加 0.12」的范围** ——
   但不修则 0.12 是空的。建议调度台找一次真机/并排比确认（`_tmp_view/thick012/fixed_box_x5.png`
   是 5× 放大示意，仅用于本次自证，不入库）。

---

## 七、typecheck

```
TMPDIR=/tmp/wf_thick012 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
