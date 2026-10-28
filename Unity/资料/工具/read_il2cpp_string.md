# `工具/read_il2cpp_string.py` —— 解 `DAT_1842xxxxx` 里的字符串（2026-10-28 · B93_E4）

> 一句话：**反编译 `.c` 里的 `DAT_1842xxxxx` 不是「读不出来的垃圾」，是 il2cpp 元数据槽**，
> 用本工具三步就能解出**原版真正传给那个方法的字符串**。
> 本文件是它**唯一**的用法说明（`资料/命令速查.md` 里不再写第二份）。

## 一、它解决什么

`d:/2/tools/decomp_full/*.c` 里有两族长得一样的 `DAT_`，**读法完全不同**（B92_N4 §5 点出）：

| 形状 | 所在段 | 是什么 | 谁读得了 |
|---|---|---|---|
| `DAT_1834xxxxx` | `.rdata` / `.text` | **裸数值常量**（float / double / int） | `工具/read_literal.py` |
| `DAT_1842xxxxx` | `.data` | 🔴 **il2cpp metadata usage 槽**：8 字节 = `0xA0000000 \| 编码下标` | **本工具** |

⚠️ 拿 `read_literal.py` 读第二族会得到 `-1.08e-19` 这种数 —— 那**不是**「原版这里是个奇怪的浮点」，
是「这个地址上根本不是浮点」。`read_literal.py` 的输出里那句「两个宽度都不像数值字面量 ⇒ 它多半不是数值」
说的就是这一族。

## 二、怎么用

```bash
python -I -X utf8 d:/4/Unity/工具/read_il2cpp_string.py DAT_1842baac0 0x184271c70
```

```
= GameAssembly: D:/2/unity_run_ref/GameAssembly.dll（ImageBase=0x180000000，7 段）
= metadata    : .../il2cpp_data/Metadata/global-metadata.dat（version=31，string literal 26514 条 …）

  0x1842baac0    .data    槽值=0x00000000a0000055 type=5(StringLiteral) idx=85 entry=42 ⇒ '\n'
```

三个开关：

| 开关 | 作用 |
|---|---|
| `--selftest` | **先跑这个**：5 条已知真值 + 2 条「错解必然解错」的反例。全绿才说明本 build 的规律没变 |
| `--search TEXT` | **反查**：表里哪些字面量含 `TEXT`，以及 `.data` 里哪些**槽 VA** 指向它（`entry` → 槽） |
| `--dll PATH` / `--md PATH` / `--limit N` | 换文件 / 限制 `--search` 的条数（默认 30） |

- **不带参数 ⇒ 只打印用法、退出码 2**（本工具**纯只读，一个字节都不写**，也不碰 `D:/2` 以外的任何东西）。
- `-h/--help` ⇒ 打印用法、退出码 0。
- 退出码：`0` 全解出 · `1` 有解不出的（不在已映射段 / 类型不是 5 / 表越界）· `2` 用法错或文件打不开。
- 默认路径：`D:/2/unity_run_ref/GameAssembly.dll` +
  `D:/2/unity_run_ref/Warpforge_Data/il2cpp_data/Metadata/global-metadata.dat`（**只读**，⛔ 不往里写）。

## 三、解码链（三步，每步都能自己核）

1. **VA → 槽值**：`RVA = VA − 0x180000000` → 按 PE 节表换成文件偏移 → 读 **8 字节**。
2. **槽值 → 类型 + 下标**：取**低 32 位**，`type = v >> 29`、`idx = v & 0x1FFFFFFF`。
   `type == 5` = **StringLiteral**（1=TypeInfo · 2=Il2CppType · 3=MethodDef · 4=FieldInfo · 6=MethodRef）。
3. **下标 → 表下标：`entry = idx >> 1`**（🔴 本 build 实测规律，见下），再到
   `global-metadata.dat` 的 string literal 表取字符串：条目 = `{ uint32 length; uint32 dataIndex }`，
   字符串 = `data[stringLiteralDataOffset + dataIndex : +length]`（**不是 NUL 结尾**）。

### 🔴 `entry = idx >> 1` 是**实测规律**，不是推的

同一批槽按两种解各解一遍，拿 **ILSpy 反编译体里的字面量集合**当判据
（`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/<类>.cs` 的同一方法体，C# 转义反转义后比集合）：

| 解法 | 命中字面量 | 命中方法 |
|---|---|---|
| `entry = idx`（不移位） | **2** 条 | 2 / 6767 |
| `entry = idx >> 1`（移位） | **3271** 条 | **1500 / 6767** |

且不移位解出来的是**别的函数**的字符串（`'Fulah'` / `'Cheerful'` —— 表里真有这些词，所以
「解出来是个像样的英文」**不等于**解对了）。这就是 `--selftest` 里那两条反例的作用。

⚠️ **换 build 先跑 `--selftest`**：它红了 ⇒ 规律变了（或 metadata 版本变了），
⛔ **别硬套、别把解出来的东西当判据**，先回头重新做上面那张对照表。

## 四、已解出的实例（三重旁证）

### 4.1 `SceneInitializer.GetUnityFormatStacktrace(Exception)` —— B92_N4 §5 那 5 个（**已解出，不再是缺口**）

| 令牌 | 解出 | 在方法里干什么（`.c` 用法） | 旁证 |
|---|---|---|---|
| `DAT_1842baac0` | `"\n"` | `Stack.Spl<…>` → `System_String__Split(…, DAT_1842baac0, …)` 逐行切 | ILSpy `stackTrace.Split("\n")` |
| `DAT_184271c70` | `"Assets\"` | `Contains` + `IndexOf` → 从「Assets\」处截断 | ILSpy `.Contains("Assets\\")` |
| `DAT_1842c7cf8` | `":"` | `Split` → `{3}:{4}` 两段 = 文件名 : 行号 | ILSpy `text4.Split(":")` |
| `DAT_184238728` | `" in "` | `Split` → `{0}` = 「 in 」之前的路径 | ILSpy `.Split(" in ")` |
| `DAT_184268c80` | `'{0} in <a href="{1}" line="{2}">{3}:{4}</a>'` | `String.Format(template, object[5])` | 5 个占位符 ↔ `.c` 里 `object[5]` 的五次赋值 |

**怎么知道它是对的**：① 五条都与 **ILSpy 同一方法体**里的字面量**逐字相同**（同族方法）；
② 模板里的 `{3}:{4}` 正好对应 `":"` 那一步切出来的两段、`{0} in …` 正好对应 `" in "` 那一步 ——
**语义自洽**；③ 全库对照实验 3271 条命中 / 1500 个方法（§三）。

### 4.2 第三重旁证：解出的字符串命中**官方本地化表**

拿 `d:/4/Unity/数据/游戏数据/官方本地化_en+zh-CN.json`（10,854 个键）比，例如：

```
AccountTab__InactivateAccount.c  0x1842c1618 -> 'MainMenu/Settings/ButtonLabel/DeleteAccount'   zh=删除账户
AccountTab__InactivateAccount.c  0x1842c0018 -> 'MainMenu/Notification/DeleteAccountConfirm'     zh=你即将删除你的游戏账户…
AccountTab__Register.c           0x18428cd08 -> 'PlayfabErrors/InvalidPassword'                 zh=无效的密码
```

**它为什么能当证据**：这些键是本地化表的**键名**，而它们出现在**同名方法**（`AccountTab.InactivateAccount` /
`AccountTab.Register` 的反编译体）里 —— 键名与方法语义一一对得上，且键在本地化表里存在。

### 4.3 规模（`--search` 反查顺手量出来）

`GameAssembly.dll` 的 `.data` 里有 **26,507 个 type=5 槽**，覆盖 **26,507 个不同字面量**
（表共 26,514 条）⇒ 也就是说：**凡是这类「原版方法里有个字符串我不知道」的行，本地都解得开**，
不需要外网、不需要跑游戏。

## 五、还解不出什么（如实写）

1. **`type != 5` 的槽本工具不碰** —— TypeInfo(1) / Il2CppType(2) / MethodDef(3) / FieldInfo(4) / MethodRef(6)：
   它们指向的是**类型/方法/字段的定义表**，要解名字得再解 `typeDefinitions` / `methodDefs` 那几张表
   （索引是否也要移位**尚未考证**）。本工具会如实打「⛔ 不是字符串字面量（type=3 MethodDef）」并计入退出码 1。
2. **`entry = idx >> 1` 的机制未考证** —— 只知道「本 build 实测如此」且有 3271 条命中支撑。
   ⛔ 别把它写成「il2cpp 就是这样」；换 build 必须重跑 `--selftest`。
3. `DAT_18453b635` 这种 **`.data` 里的普通标志位**（`if (DAT_18453b635 == '\0')`）不是 metadata 槽，
   本工具会按「类型不是 5」报出来 —— 那是正常现象，不是工具坏了。

## 六、复跑（别人验收用）

```bash
python -I -X utf8 d:/4/Unity/工具/read_il2cpp_string.py --selftest
#   → PASS 7 / FAIL 0

python -I -X utf8 d:/4/Unity/工具/read_il2cpp_string.py DAT_184238728 DAT_184268c80
#   → ' in ' / '{0} in <a href="{1}" line="{2}">{3}:{4}</a>'

python -I -X utf8 d:/4/Unity/工具/read_il2cpp_string.py --search 'line="{2}"' --limit 3
#   → entry=26003 … 槽(VA)=0x184268c80      ← 反查能自己找回同一个槽，闭环
```
