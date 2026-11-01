# `B115_W22` · `B-313`（最有界的一块）—— `WildcardShine` 三族 **ColorFilter 0..8 九档**补齐

> 写手 `B115_W22` · 2026-11-01。
> ⛔ 没跑 Unity · ⛔ 没动 git / 账 / 正本 / 其它 `.shader` / `.cs`。
> 只改 **1 个文件**：`Unity/MyGame/Assets/CardPresentation/Resources/Shaders/WildcardShine.shader`
> （行尾 **LF**：`CRLF 0 · 裸CR 0`；**455 行 / 33,542 B → 621 行 / 47,034 B**）＋本报告。

---

## 〇 一句话 ＋ 两条必须先说的**判据订正**

1. **三族 ColorFilter 的 0..8 九档全部实现**（0 = 不染；1/2/3/4/5/6/7/8 逐档带判据行号）。
2. 🔴 **订正一：`_ColorFilter` 原来【一档都没有】** —— 简报写「我们现在只做了 0 与 6 两档」，
   那对 `_TransitionColorFilter` / `_EdgeColorFilter` 成立（它们各有 `(filter > 5.5) ? … : s` 一行），
   但**基础 `_ColorFilter` 在本文件里连名字都没有**（`grep -n "_ColorFilter" ` 改前只有注释命中）。
   之所以一直看不出来：`filter = 0` 时它就是**恒等**，而现役材质是 `new Material(sh)`（没人 `SetFloat` 过它）⇒ 永远取默认 0。
   **本波把它连同 `_ColorValue` / `_ColorIntensity` / `_ColorGlow` 一起补上了**（原版 4 个效果档都带它）。
3. 🔴 **订正二：「三者算式互不相同」只对了一半** —— 逐段比过编译产物：
   · **(B) 过渡族** 与 **(A) 基础族** 确实**不是**一个算式（例 4 直接 `= C.rgb`、例 2 = `p + C.rgb`）；
   · 但 **(A) 基础族** 与 **(C) 边缘族** 的**公式逐条相同**（见 §三 的 diff 与逐条判读），
     真正的差异只有「颜色参数」与「输入已经是 (A)+intensity 之后的值」。
   ⇒ 我**仍然写成三个函数**（`B-313`/`CLAUDE.md` 的「⛔ 别合并」照办），但把这条事实写进注释，免得以后有人以为 (C) 抄错了。

---

## 一 判据（原版产物 · 现读）

| 项 | 值 |
|---|---|
| 原版 shader | `d:/2/tools/ruri_out_1020_rootshaders/ExportedProject/Assets/Shader/Hidden_UI_Default (UIEffect).shader`（114 行） |
| 片元产物 | 同目录 `Sub0_Pass0_Fragment_b13..b32.hlsl`（20 份） |
| **本波用到的三份** | `b14`（534 行 · `md5 faca99bc6bcf6b8eae0c1aa91fe7582d`）· `b16`（747 行 · `3574c6457988f837ada876b192314ae6`）· `b17`（872 行 · `49131c4b531d632e7488843a0c50d949`）＋旁证 `b15`（735 行，过渡族与 b16 同） |
| 每份的变体归属 | 第 2 行 `// Keywords:`（b14 `TONE_GRAYSCALE` · b16 `TRANSITION_SHINY` · b17 `EDGE_SHINY`） |

**三族各在哪（cbuffer 现读）**

| 族 | 参数 | 出现在哪些变体 |
|---|---|---|
| **(A) `_ColorFilter`** | `_ColorFilter`(int, b14 `:11` c15.y) · `_ColorValue`(`:14` c16) · `_ColorIntensity`(`:15` c17) · `_ColorGlow`(`:16` c17.y) | **b14 `:311-513`** · b15 `:522-711` · b16 `:523-725` · b17 `:444-637`（4 个效果档**都有**；b13 基础档**没有**） |
| **(B) `_TransitionColorFilter`** | `_TransitionColorFilter`(b16 `:24` c21) · `_TransitionColor`(`:28` c22) · `_TransitionColorGlow`(`:29` c23) | **b15 `:176-306`** · **b16 `:175-305`**（两档归一化后**逐行相同**，见 §三） |
| **(C) `_EdgeColorFilter`** | `_EdgeColor`(b17 `:38` c30) · `_EdgeColorGlow`(`:39` c31) | **b17 `:638-829`**（输入是 (A) 算完的 `_747.._750`，见 `b17:631-637`） |

⚠️ **(B) 没有 `_TransitionColorIntensity`、(C) 没有 `_EdgeColorIntensity`** —— 只有 `_ColorIntensity` 一条（grep 现读：b15/b16/b17 里 `_TransitionColorIntensity` / `_EdgeColorIntensity` 均 0 命中）。
所以「强度插值」只属于 (A)；过渡/边缘族只有 **glow 只影响 α** 这一条（本文件原来就实现了）。

---

## 二 逐档对照表（档位 → 原版 hlsl 行 → 我们的实现）

> 我们的行号 = 当前文件。函数体：`wfColorFilterBase`(`:367`) · `wfColorFilterTransition`(`:389`) · `wfColorFilterEdge`(`:409`)；
> 共用辅助：`wfColorLuma`(`:332`) · `wfRgb2Hsv`(`:339`) · `wfHueK`(`:358`)；(A) 的 intensity/glow 一跳 = `wfBaseColorFilter`(`:434`)。

### (A) 基础 `_ColorFilter` —— 判据 `b14.hlsl:316-481`（b17 `:643-829` 同形）

| 档 | 原版行 | 算式（`p` = 预乘 rgb · `a` = α · `C` = `_ColorValue` · `cw = C.a`） | 我们 |
|---|---|---|---|
| **0** | `b14:503`（`_282 = int(_194) > 0` 为假 ⇒ 用未滤波值） | 恒等 | `:371` `if (f < 0.5) return float4(p, a);` |
| **1** | `:316-330` | `p·C.rgb·cw`；α = `a·cw` | `:372` |
| **2** | `:337-347` | `p + a·C.rgb·cw`；α 不变 | `:373` |
| **3** | `:354-364` | `p − a·C.rgb·cw`；α 不变 | `:374` |
| **4** | `:371-385` | `a·C.rgb·cw`；α = `a·cw` | `:375` |
| **5** | `:392-412` | `0.5·a·C.rgb·cw·(luma(p)+1)`；α 不变 | `:376` |
| **6** | `:415-421` | `p·(C.rgb·cw + 1)`；α 不变 | `:377` |
| **7** | `:422-462`（HSV）+ `:478-481`（选择） | HSV(p) → `h'=|h|+C.x` · `s'=s+C.y` · `v'=v+C.z` → `(1+s'·k)·v'`，再 `·a·cw`；α = `a·cw` | `:378-384` |
| **8** | `:464-481` | `(C.y·1.5 + ((p−0.5)·(C.x+1) + 0.5))·a·cw`；α = `a·cw` | `:386` |
| **post** | `:503-513` | `filter>0` 时：`rgb = lerp(原值, 滤波值, _ColorIntensity)`；`α = lerp(α, α滤波, I)·(1 − _ColorGlow·I)` | `wfBaseColorFilter` `:434-442`；调用点 `:459`（TONE 之后）· `:517`（过渡之后）· `:556`（边缘色之前） |

### (B) 过渡 `_TransitionColorFilter` —— 判据 `b16.hlsl:179-306`（＝ b15 `:176-306`）

| 档 | 原版行 | 算式（**不乘 `C.a`、也不乘 α**） | 我们 |
|---|---|---|---|
| **0** | `b16:287`（`_811/_813/_815` 全假 ⇒ `_407`） | 恒等（原版另有 `_479 = filter>0` 的闸） | `:393` `if (f < 0.5) return p;` |
| **1** | `:179-187` | `p·C.rgb` | `:394` |
| **2** | `:193-198` | `p + C.rgb`（`mad(_403,_394,C.x)`，掩码为 1 时 `_403 = p`） | `:395` |
| **3** | `:204-209` | `p − C.rgb` | `:396` |
| **4** | `:215-220` | **`C.rgb`**（纯色，丢掉图） | `:397` |
| **5** | `:226-238` | `0.5·C.rgb·(luma(p)+1)` | `:398` |
| **6** | `:244-249` | `p·(C.rgb + 1)` ← **与 (A) 的例 6 差一个 `C.a`** | `:399` |
| **7** | `:250-285`（HSV）+ `:287`（选择） | HSV 同上，**不带 `·a·cw`** | `:400-405` |
| **8** | `:287-289` | `C.y·1.5 + ((p−0.5)·(C.x+1) + 0.5)` | `:407` |
| **post** | `:307-314` | 混合：`s + mask2·(inner·α − s)`，`inner = C.a·(tc − s) + s`；α 只在 `_TransitionColorGlow` 时变 | **原有代码已实现**（`:493-505` 一带），本波只把 `tc` 换成函数结果（`:492`） |

### (C) 边缘 `_EdgeColorFilter` —— 判据 `b17.hlsl:643-829`

| 档 | 原版行 | 算式（与 (A) **同形**，只是 `C = _EdgeColor`） | 我们 |
|---|---|---|---|
| **0** | `:805-808`（`_1519.._1521` 全假 ⇒ `_747`） | 恒等 | `:413` `if (f < 0.5) return float4(p, a);` |
| **1** | `:643-657` | `p·C.rgb·cw`；α = `a·cw` | `:414` |
| **2** | `:664-674` | `p + a·C.rgb·cw` | `:415` |
| **3** | `:681-691` | `p − a·C.rgb·cw` | `:416` |
| **4** | `:698-712` | `a·C.rgb·cw`；α = `a·cw` | `:417` |
| **5** | `:719-739` | `0.5·a·C.rgb·cw·(luma(p)+1)` | `:418` |
| **6** | `:742-748`（`_1452 = _747 · mad(_1442,_1448,1.0)`，`_1448 = C.w`） | `p·(C.rgb·cw + 1)` ← **乘 `C.a`**（≠ (B) 的例 6） | `:419` |
| **7** | `:749-790`（HSV）+ `:805`（选择） | 同 (A) 例 7 | `:420-426` |
| **8** | `:794-804` | 同 (A) 例 8 | `:428` |
| **post** | `:830-831`（`_911 = (1−_EdgeColorGlow)·α滤波`）+ `:846`（`(filter>0) ? _911 : _907`） | glow 只影响 α、且只在 `filter>0` 时生效 | `:563-565` |

**例 7 的 HSV 细节**（三族共用，判据 `b14:659-684` / `b16:749-779` / `b17:1456-1481`，三处逐句相同）：

```text
h/s/v 由 p 提取（h = (mid−lo)/(6·chroma+1e−5) + (r≥max? offB : offA) · s = chroma/(v+1e−5) · v = max）
h' = |h| + C.x   s' = s + C.y   v' = v + C.z
k(n) = clamp(|frac(h'+n)·6 − 3| − 1, 0, 1) − 1      n = 1 / 2/3 / 1/3
rgb  = (1 + s'·k) · v'
```
⚠️ **h 不是 0..1 量纲**（就是原版 `frac·6−3` 那一套），⛔ 别自己归一 —— 注释里也写死了。

---

## 三 「三族算式互不相同」的**证明**（附一条订正）

### 3·1 (B) 与 (A) 不同 —— 三条硬差异（逐例可查）

| 例 | (A) 基础 `b14` | (B) 过渡 `b16` | 差在哪 |
|---|---|---|---|
| 1 | `:322-325` `p·C.rgb·C.a`，**α 也乘 C.a** | `:181-186` `p·C.rgb`，α 不动 | 乘不乘 `C.a` |
| 2 | `:343-345` `p + a·C.rgb·C.a` | `:195-197` `p + C.rgb` | 同上（少 `a·C.a`） |
| 4 | `:377-380` `a·C.rgb·C.a` | `:217-219` **`C.rgb`** | 差 α 因子 |
| 6 | `:419-421` `p·(C.rgb·C.a + 1)` | `:247-249` `p·(C.rgb + 1)` | **`C.a`**（这条就是 `B115_W12` §六·8 记的那处，也是本波最容易写错的一处） |

### 3·2 (B) 在 b15 / b16 里是**同一份体**（不是「两档各写一遍」）

```text
归一化（把 `_NNN` 全换成 `_N`）后 diff：
  b15:176-306  vs  b16:175-305   →  131 行 vs 131 行 · diff = 0
```
⇒ 两个效果档（溶解 / 扫光）用的是**逐行相同的**过渡色族；我按一份写、两档共用（我们本来就是 `#if WF_DISSOLVE || WF_SHINY` 共用一块）。

### 3·3 (A) 与 (C)：**公式相同**（这条是订正，不是「互不相同」）

同样的归一化 diff，`b14:311-513` vs `b17:444-637` 有 **37 行差异**，但**逐条判读全是「等价重排」**：

| 差异形态 | 例子 | 判读 |
|---|---|---|
| 常量折进 mad | b14 `_725 = _172−0.5; … mad(_725,_733,0.5)` ↔ b17 `mad(_354,_345,−0.5)` ⇒ `mad(p,k,−(0.5k))` | 编译器把 `(p−0.5)·k` 折成 `p·k − 0.5k` —— **同一个值** |
| 半系数位置 | b14 `_636 = _633·0.5` 在 `_718 = _715·_651` **之前**；b17 `_1515 = _1512·_1448` **之后** | 只差乘法次序（结合律，浮点上 ±1 LSB） |
| FMA 合并 | b14 `_660 = g−b; mad(_659,_660,_174)` ↔ b17 `mad(_659, mad(g,b,−b), …)` | 同一式子 |
| 布尔换位 | b14 `((!(_A‖_B))‖_C) ? x : y` ↔ b17 `(_C ‖ (!(_A‖_B))) ? x : y` | 同一真值表 |
| 临时量 | b14 先算 `_283.._286 = 滤波值 − 原值` 再 `mad`；b17 内联 | 同一式子（(A) 的 post 那一跳） |

⇒ **结论**：`_EdgeColorFilter` 与 `_ColorFilter` 是**同一族公式**（差颜色参数 + 差的输入）。
我照纪律写成两个函数（`wfColorFilterBase` `:367` / `wfColorFilterEdge` `:409`），并在两处注释里写明「相同是事实、改一处要连带核另一处」。

---

## 四 接线：三族各插在哪（判据 = 原版块的位置）

| 效果档 | 原版顺序 | 我们插在哪 |
|---|---|---|
| `TONE_GRAYSCALE`（b14） | Tone（`_155→_172`）→ **(A)**（`:311`）→ 末尾 | Tone 之后：`:458-461` |
| `TRANSITION_DISSOLVE`（b15） | 过渡混合（`_509.._512`）→ **溶解乘 `_448`**（`:513-516`）→ **(A)**（`:522`） | 过渡块末尾：`:516-518`（溶解/扫光共用那一块） |
| `TRANSITION_SHINY`（b16） | 过渡混合（`_510.._513`）→ **(A)**（`:523`） | 同上（`:516-518`） |
| `EDGE_SHINY`（b17） | **(A)**（`:444`）→ **(C)**（`:638`）→ 扫边光混合（`:846-850`） | 边缘色之前：`:555-558`（(A)）→ `:562`（(C)）→ 原有混合式不变 |
| **基础档**（b13） | **没有** (A) | 我们也没有（`WF_*` 一个都不定义 ⇒ 三个 `#if` 全不进）—— 结构自检实测 `baseFilterCalls=0` ✅ |

`(B)` 的调用点 = 原过渡色那行：`:492`（原来是 `(_TransitionColorFilter > 5.5) ? (s * (_TransitionColor.rgb + 1.0)) : s`）。

🔴 **`_ColorFilter` 的 `filter==0` 闸写在了 `wfBaseColorFilter`（`:436`）里**（原版 `_282 = int(_194) > 0`）——
⚠️ 这一闸**同时管 `_ColorGlow`**（原版 `_323 = _282 ? _298 : _274`）：glow 只在 `filter>0` 时生效，⛔ 别把它挪到闸外。

---

## 五 今天观感**不变**（现役实例逐条核过）

材质是 `Effects/UIEffectTogglerCoffeeEffects.cs` 的 `WildcardShineLayer.Attach` 里 **`new Material(sh)`** 造的（不是抄原版材质），
之后只 `SetFloat` 了 `_Transition*` 那一串（`_TransitionColorFilter = p.colorFilter`），**没人写过 `_ColorFilter` / `_ColorValue` / `_ColorIntensity` / `_ColorGlow`**。

| 参数 | 现值来源 | 结果 |
|---|---|---|
| `_ColorFilter` | 我们的 Property 默认 **0** | `wfBaseColorFilter` 直接返回原值 ⇒ **(A) 整段跳过**（与改前「没有 (A)」逐项相同） |
| `_ColorIntensity` / `_ColorGlow` | 默认 **1 / 0** | 即使 `filter≠0` 也是恒等（`lerp` 系数 1、glow 0） |
| `_TransitionColorFilter` | `= p.colorFilter`（5 个实例全是 **6**） | 新算式 `p·(C.rgb+1)` **与旧式 `s·(C.rgb+1)` 逐字相同** ⇒ 扫光颜色不变 |
| `_EdgeColorFilter` | 默认 **0** | 恒等；`ea` 的闸与改前相同（filter>0 才乘 `1−glow`） |

⇒ **现役 5 个 Shiny 实例的输出逐位不变**；`B99W2_Assert` 那批（扫光 rate 0→1）不受影响（除非 shader 编不过 —— 见 §七）。

---

## 六 闸读数 / 结构自检 / 行尾（改完现读）

```bash
python -I -X utf8 d:/4/Unity/工具/check_shader_keyword_collision.py --quiet --no-warn
# 合计：文件 65 · 关键字 150 · 变量声明 3,797 · 真冲突 0  ⇒ PASS（rc=0）
```

- **真冲突 0**（本轮新增 4 条 Property：`_ColorFilter` / `_ColorValue` / `_ColorIntensity` / `_ColorGlow` —— **都不是关键字名**，⛔ 与 `[Toggle(NAME)]` 无交集）。
- 变量声明 3,797（原来 3,755；+42 = 本文件新增的 4 条 uniform + 一批函数内局部量，闸把它们都算「声明」）。
- **括号/预处理配对**（7 种关键字组合各跑一遍，`#define` 也展开）：

```text
TONE       braces 17/17 parens 132/132 brackets 0/0  OK   (A)调用1 (B)定义  (C)定义
DISSOLVE   braces 17/17 parens 145/145 brackets 0/0  OK   (A)调用1 (B)定义+调用 (C)定义
SHINY      braces 17/17 parens 145/145 brackets 0/0  OK   (A)调用1 (B)定义+调用 (C)定义
EDGE       braces 17/17 parens 189/189 brackets 0/0  OK   (A)调用1 (B)定义  (C)定义+调用
BASE(none) braces 16/16 parens 128/128 brackets 0/0  OK   (A)调用0 ← 与 b13 一致
SHINY+CLIP braces 17/17 parens 163/163 brackets 8/8  OK
DISSOLVE+AC braces 17/17 parens 146/146 brackets 0/0 OK
# if / #endif = 13 / 13
```
- 行尾：`LF · 621 行 · CRLF 0 · 裸CR 0 · 47,034 B`（用 **Edit 工具**改，⛔ 没用 `sed -i` / python 整篇写）。

---

## 七 🔴 必须靠 Unity 编译验证的清单（`typecheck` 看不见 shader）

| # | 要验什么 | 为什么必须验 | 怎么验 / 判失败长什么样 |
|---|---|---|---|
| 1 | 🔴🔴 **整份 shader 编得过（20 档变体）** | 本波新增 3 个带 **8 路 if/else** 的函数（含 HSV 分支） | Console 无 `Shader error`。若报 **instruction count / temp register 超限** ⇒ 看第 2 条 |
| 2 | 🔴 **`#pragma target 3.0` 这一步** | 本文件原来是**默认 2.5**；2.5 在 CG 侧**没有动态分支** ⇒ 三族 8 档会被**全部拍平**，指令数成倍 | 已提到 3.0（`WFUIFxShine.shader` 也是 3.0）。若 3.0 仍超限 ⇒ 依次试 4.0 → 把 (A)/(C) 的 1..8 用 `#if` 静态剪掉（本项目其它 shader 用过这种兼容档写法）。⚠️ 别再退回 2.5 |
| 3 | **基础档（b13）不受影响** | 新函数在**所有**变体里都会被编译（即使不调用） | 随便一个不开任何效果关键字的材质 ⇒ Console 无错、画面 = 原图 |
| 4 | **扫光仍照旧（视觉回归）** | `_TransitionColorFilter=6` 的新算式与旧式**逐字相同**，理应零差异 | 收藏窗 `CardsTab` 的扫光 + `B99W2_Assert` 那 7 条（rate 0→1、计数条不亮） |
| 5 | **`_ColorFilter` 默认 0 真的生效**（即不会被 Unity 当成「缺失属性」乱填） | 我们把它写进了 `Properties`（原版没有），`new Material(sh)` 会取默认 0 | 运行时 `mat.GetFloat("_ColorFilter")` 现读 = 0；或临时置 6 看颜色是否变成 `p·(C·C.a+1)` |
| 6 | **`_ColorValue` 默认 (1,1,1,1) 无害** | filter=0 时整段不用它 | 同上 |
| 7 | 变体数 / 编译时间 | 仍是 20 档（本波没动关键字） | Shader Inspector 变体数与改前一致 |

**静态已自检**：7 种组合括号全配平 · 每档 `(A)` 调用 1 次（基础档 0 次） · `(B)/(C)` 各自有定义与唯一调用 · `#if/#endif` 13/13 · 行尾 LF。

---

## 八 顺手发现（⛔ 只报，没越界改）

1. 🔴 **`_ColorFilter` 原来完全没实现**（不只是缺 1..8）—— 见 §〇 订正一。建议把 `B115_W12` §六·4 那句「只做了 0 与 6 两档」**就地订正**（那句把三族当成同一状态，实际是「过渡/边缘 = 0+6，基础 = 0 档都没有」）。
2. 🔴 **`B-313` / 简报里「三族算式互不相同」要收窄成「(B) 与 (A)/(C) 不同；(A) 与 (C) 公式相同」** —— 见 §三·3。⛔ 但「别合并成一个函数」这条**仍然有效**（理由从「算式不同」换成「参数/输入不同 + 防止以后改一支串到另一支」）。
3. ⚠️ **文件所有权表没更新**：`资料/完全复刻/文件所有权_当前.md:33` 现读仍写着
   `| B115_W12 | B-274 WildcardShine 变体整批 | …/WildcardShine.shader | 在飞 |`，
   而 `B115_W12` 的报告已在 20:44 落盘、本波是调度台指派给我接手。
   ⇒ **请调度台把该行改成当前写手**（我按「调度台指派 = 所有权已转移」处理，未并发迹象：改前文件 mtime 20:42 < W12 报告 20:44）。
   ⚠️ 这也是 `CLAUDE.md` 那条「⛔ 别拿 `git status` 判文件归谁」的正面例子 —— 那个文件本来就一直是 `M`。
4. ⚪ **`_SamplingScale` 的默认值 1 仍没打 `🔶 自己设计` 标记**（`B115_W12` §六·11 说它是「我们挑的默认值」，但只写在报告里）。
   本波只给自己新增的编译档打了标记（`:193`），没动它（零行数改动本可以顺手做，但属另一条活）。建议另派一条把这类「只写在报告里、没进代码」的标记一次补齐。
5. ⚪ **原版 `_TextureSampleAdd` 仍未实现**（`b13.hlsl:35-38`；`W12` §六·3 已记）。与 ColorFilter 无关，不重复展开。
6. ⚠️ **`_TransitionColorGlow` 的语义是我们自己的近似**：原版 `_513 = α·(1 − mask2·glow·C.a)`（`b16:314`），我们写的是
   `aT = glow ? (1−C.a) : 1` 再 `lerp`。**glow 是整数 0/1 时逐项等价**（这点 `W12` 核过），但 glow 取 0.5 这类值时两者不同 —— 记一笔，不属本波范围。

---

## 九 复核材料（可复跑）

```bash
# ① 闸
python -I -X utf8 d:/4/Unity/工具/check_shader_keyword_collision.py --quiet --no-warn
# ② 行尾（必须二进制读）
python -I -X utf8 -c "import io;p=r'd:/4/Unity/MyGame/Assets/CardPresentation/Resources/Shaders/WildcardShine.shader';b=io.open(p,'rb').read();print(b.count(b'\r\n'),b.count(b'\n'),b.count(b'\r')-b.count(b'\r\n'),len(b))"
# 现读：0 621 0 47034
# ③ 原版三块的定位（可 grep 复核）
cd "d:/2/tools/ruri_out_1020_rootshaders/ExportedProject/Assets/Shader/Hidden_UI_Default (UIEffect)"
grep -n "uint(_ColorFilter)\|uint(_TransitionColorFilter)\|uint(_EdgeColorFilter)" Sub0_Pass0_Fragment_b1[3-7].hlsl
```
（②③ 之外，本波的两个结构/对照脚本写在 `%TEMP%/w22/`（**不进仓库**），逻辑见 §三、§六 贴出的全部输出。）

---

## 十 改动边界（自证没越界）

- 只碰 1 个 `.shader`（本件所有权）+ 本报告。⛔ 没动 git / 账 / 正本 / 其它 shader / 任何 `.cs`。
- **没动**已经工作的 5 路效果体（Tone 去色式 / 过渡混合式 / 边缘 12 向采样 / 扫边光 / 两个 UI 关键字）与 `b13` 基础档 ——
  本波在它们**之间**插了 (A) 的两跳、把 (B)/(C) 的两行 `? :` 换成函数调用，其余算式逐字未改。
- 账：`grep -rn "WildcardShine" 账/*.tsv` 现读 9 条，**没有一条**带 `WildcardShine.shader:<行号>` 形态（`grep -o "WildcardShine.shader:[0-9]*"` = 0 命中）⇒ **`+166 行` 不产生 `check_ledger_points` 漂移**。
