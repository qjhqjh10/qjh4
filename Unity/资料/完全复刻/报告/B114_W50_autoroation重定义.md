# B114_W50 —— `WFUIBorderHighlight.shader` 的 `redefinition of '_AUTOROTATION'`

- 写手：`W-50` · 日期：2026-11-01
- 对象文件：`Unity/MyGame/Assets/WarpforgeVFX/Shaders/WFUIBorderHighlight.shader`
- 诊断判据：`Unity/资料/完全复刻/判据/B114_R9_ShellScene六红诊断.md` §#1
- 报错原文：`redefinition of '_AUTOROTATION' at WFUIBorderHighlight.shader(202)`
  （日志 `_tmp_view/b114_unity_shellscene.log` 第 1003 / 181400 行附近）
- 备份：`_tmp_view/b114_w50/WFUIBorderHighlight.shader.bak`（改前 · 333 行）

---

## 一、成因（一句话）

`:117` 的 `[Toggle(_AUTOROTATION)]` 搭配 `:159` 的 `#pragma shader_feature_local _AUTOROTATION`，
**材质一开这个 Toggle，Unity 就 `#define _AUTOROTATION`**；而 `:202` 又声明了一个同名 `float`
⇒ 宏与变量重定义 ⇒ 整份编不过。

⚠️ **三颗同病**：`_AUTOROTATION` / `_FLIPDIRECTION` / `_DEBUG` 都是「`[Toggle]` + `shader_feature_local` + 同名 `float` 声明」。
**编译器每 pass 只报第一个同名标识符**，所以只报了 `_AUTOROTATION` —— 只修一颗必然下一轮再报另外两颗。

---

## 二、三个名字的全部出现点（改后现读，逐条给行号 + 用法）

### `_AUTOROTATION`

| 行 | 内容 | 性质 | 处置 |
|---|---|---|---|
| 13 | 文件头「原版属性表」注释 `_AUTOROTATION 0` | 注释（记录的**原版** `m_PropInfo`） | 保留 |
| 98/101/104 | 新增的「已知差异 ⑨」注释 | 注释 | 本次新增 |
| 125 | `[Toggle(_AUTOROTATION)] _AUTOROTATION ("AutoRotation", Float) = 0` | **Properties**（材质面板 + `SetFloat` 需要） | **保留** |
| 167 | `#pragma shader_feature_local _AUTOROTATION` | **关键字声明**（原版也是 local+feature） | **保留** |
| 211/214/215/217/218 | 本次新增的「⛔ 这里不能声明」注释 | 注释 | 本次新增 |
| **269** | `#if defined(_AUTOROTATION)` | **唯一代码点** —— 只判「有没有定义」，**不读值** | 保留 |

### `_FLIPDIRECTION`

| 行 | 内容 | 性质 | 处置 |
|---|---|---|---|
| 12 | 文件头「原版属性表」注释 `_FLIPDIRECTION 0` | 注释 | 保留 |
| 98 | 「已知差异 ⑨」注释 | 注释 | 本次新增 |
| 119 | `[Toggle(_FLIPDIRECTION)] _FLIPDIRECTION ("FlipDirection", Float) = 0` | **Properties** | **保留** |
| 168 | `#pragma shader_feature_local _FLIPDIRECTION` | **关键字声明** | **保留** |
| 211 | 「⛔ 这里不能声明」注释 | 注释 | 本次新增 |
| **319** | `#if defined(_FLIPDIRECTION)` | **唯一代码点** —— 只判定义、**不读值** | 保留 |

### `_DEBUG`

| 行 | 内容 | 性质 | 处置 |
|---|---|---|---|
| 11 | 文件头「原版属性表」注释 `_DEBUG 1` | 注释 | 保留 |
| 23 | 文件头 VS-B 说明里提到 `_DEBUG` | 注释 | 保留 |
| 98 | 「已知差异 ⑨」注释 | 注释 | 本次新增 |
| 116 | `[Toggle(_DEBUG)] _DEBUG ("Debug", Float) = 1` | **Properties** | **保留** |
| 166 | `#pragma shader_feature_local _DEBUG` | **关键字声明** | **保留** |
| 211 | 「⛔ 这里不能声明」注释 | 注释 | 本次新增 |
| **262** | `#if defined(_DEBUG)` | **唯一代码点** —— 只判定义、**不读值** | 保留 |

---

## 三、自证：这三个名字在 shader 里【除 `#if defined` 与 `[Toggle]` 之外没有任何读值点】

判据方式 = 对整份文件 `grep -n "_AUTOROTATION\|_FLIPDIRECTION\|_DEBUG"`，把结果按「注释 / Properties / pragma / 代码」分类（上面三张表）。

**剔除注释、`Properties` 声明、`#pragma` 之后，真正的代码点只有三处，全是 `#if defined(...)`：**

```
262:                #if defined(_DEBUG)
269:                    #if defined(_AUTOROTATION)
319:                #if defined(_FLIPDIRECTION)
```

- `#if defined(X)` 只问「宏在不在」，**不求值** ⇒ 不需要 `float X;` 这个 uniform。
- 文件里**没有**任何 `if (_DEBUG)` / `_AUTOROTATION *` / `_FLIPDIRECTION ?` / `lerp(..., _DEBUG)` 一类的**读值**写法
  —— 上面 `grep` 是全量输出，没有遗漏。
- `_Rotation` / `_AutoRotationSpeed` **不是**关键字名（`Properties:124/126` 是普通 Float，没有 `[Toggle]`、
  也没有 `#pragma`）⇒ **必须照旧声明**，本次保留（`float _Rotation, _AutoRotationSpeed, ...`，`:210`）。

**旁证（谁在开关键字）**：`Shell/MenuFxMaterials.cs` 里 **真的** 会 `EnableKeyword`
（`:269` `_AUTOROTATION` · `:270` `_FLIPDIRECTION` · `:446`/`:448`/`:505`/`:506` 按状态开、`:262-265`/`:438-442`/`:499-502` 配合 `SetFloat`）
⇒ 这条重定义**在真机上必然触发**，不是只在某种罕见配置下才会中。

**为什么删 uniform 不会破坏 `SetFloat` / `EnableKeyword`**：
- `SetFloat("_AUTOROTATION", v)` 写的是**材质属性存储**，只要 `Properties` 里还有这一条就照旧能写。
- `EnableKeyword("_AUTOROTATION")` 认的是 `#pragma shader_feature_local` 的关键字表，与 uniform 声明无关。
- ⇒ 删的只是「shader 代码能不能读到值」，而**它本来就不读**。

---

## 四、改了哪几行

### 改动 1（核心修复）：`:202` → `:210`，删掉三个 uniform 名字

改前（1 行）：
```hlsl
            float _Rotation, _AUTOROTATION, _AutoRotationSpeed, _FLIPDIRECTION, _DEBUG, _QueueOffset, _QueueControl;
```

改后：
```hlsl
            float _Rotation, _AutoRotationSpeed, _QueueOffset, _QueueControl;
```
（下面紧跟 11 行说明注释，写明「⛔ 这里不能声明这三个」+ 报错原文 + 同族先例）

- 删除的名字：`_AUTOROTATION` · `_FLIPDIRECTION` · `_DEBUG`（三颗全删）
- 保留的名字：`_Rotation` · `_AutoRotationSpeed`（VS 真读值）· `_QueueOffset` · `_QueueControl`（原版两个 HideInInspector）

### 改动 2：文件头「已知差异」节新增 **⑨**（`:98-105`）

补一条与本波同源的已知差异，内容 = 这三个名字不声明为 uniform 的原因、报错原文、
同族先例（`WFEclipseTau.shader:96-101` · `WFParticlePremultiplyGreyscale.shader:109-111`），
并点明「编译器每 pass 只报第一个 ⇒ 三颗同病」。

---

## 五、净增净减

| | 行数 |
|---|---|
| 改前 | **333** |
| 改后 | **352** |
| 净 | **+19 行，全部是注释**（改动 1 的说明 11 行 + 改动 2 的 ⑨ 8 行） |

**代码净变化 = −3 个标识符**（删掉 `_AUTOROTATION` / `_FLIPDIRECTION` / `_DEBUG` 三个 uniform 名字），
**没有增删任何一条语句 / 算式 / 关键字 / 属性**。

- 行尾：改前 `CRLF=0 / LF=333`，改后 `CRLF=0 / LF=352` ⇒ **行尾未被翻**（该文件本来就是纯 LF）。
- 该文件是 `.shader`，**不被 `账/*.tsv` 按行号引用**（`grep WFUIBorderHighlight 账/` = 0 命中）⇒ 无账漂移风险。

---

## 六、复跑结果（本机已跑）

### ① 秒级类型检查
```
TMPDIR=/tmp/wf_w50 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
⇒ **0/0**。⚠️ 但 `typecheck` **不覆盖 shader**（它只编 C#），所以这一条只能证明「没碰到 C#」。

### ② 现有断言不会被这次改动打红（逐条核过）
- `B114_W31_Assert.cs:209-214` · `B114_W36_Assert.cs:218-221` 只 `Count()` **`#pragma` 行**
  （`#pragma shader_feature_local _DEBUG` == 1 等）—— 这三条 pragma **原样保留**，计数不变。
- `B110_W5` 的 `B7b`/`B12` 判的是「没有 `#pragma multi_compile_local _DEBUG`」—— 本次**没有**引入该行。
- `B114_W36_Assert.cs:208` 的 `Idx(src, "o.clipRect = float4(v.vertex.xy * 2.0")` 是**相对顺序**检查，
  本次只在它**之前**删了 3 个标识符 + 加了注释，**相对顺序不变**。

### ③ 🔴 **待复跑 `B110W5`**（我这一侧跑不了 Unity）
本波**没有跑 Unity**，所以「shader 真能编过」这一条**尚未实证**。
⇒ **下一轮 Unity 窗口请跑 `B110W5`**，判据 = 日志里**不再出现** `redefinition of '_AUTOROTATION'`（以及那两颗同病的）。
- 若 `B110W5` 仍报重定义：**同名但换了名字**（说明还有第四颗同类，见 §七）。
- 若 `B110W5` 绿了：这条真回归收口。

---

## 七、没查清的部分

1. **只在本机静态判定「不会有读值点」，没有编译验证**。
   `#if defined` 不求值是 HLSL 语义常识，但**「编得过」这件事只有 Unity 能证明** ⇒ 见 §六·③。
2. **是否还有第四颗同类，我只扫了这一份文件 + 全仓 `_AUTOROTATION`/`_FLIPDIRECTION`/`_DEBUG`**。
   全仓同族（`[Toggle]` / `shader_feature` 名字同时被声明成同名变量）**没有做全仓普查**
   —— 本波简报只授权这一个文件，另一份 `WFEclipseTau` / `WFParticlePremultiplyGreyscale` 已各自修过。
   如需普查，建议下一波单独派（判据写法：对每个 `.shader`，取 `[Toggle(NAME)]` 的名字集合与
   `#pragma shader_feature* NAME` 的名字集合，再与 `float NAME` / `half NAME` 的声明集合求交集，非空即同病）。
3. **`MenuFxMaterials.cs:262-270 / 438-448 / 499-506` 的 `SetFloat` 调用在新 shader 下是否仍被原版语义需要**：
   我确认了它们不会报错（写材质属性存储即可），但**没有**逐个核「原版这个材质到底把这三颗当不当材质属性用」。
   —— 简报判据（`B114_R9` §#1）说「Properties 里那三条 `[Toggle]` 保留」，我照做了；这个保留本身是对的。
