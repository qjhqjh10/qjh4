# A1808 · 两张画风 logo + 异画风格档位（第二十八会话 · 2026-10-14）

> 账原文（`资料/普查产出_第二十七会话/新发现_待立账.md` §二·己）：**「两张 Art Style Logo：第三只包已拿到
> （我们全库都没有）⇒ 补进 `Resources/` 并接筛选面板那个槽」**。
> 本报告 = **做了什么 / 判据 / 顺手查出来的真偏离 / 没做的**。判据一律取**原版字节或反编译方法体**，
> ⛔ 不用我们自己的常量互相证明。

---

## 一、判据链（**三路互证**，全部 2026-10-14 现读）

| # | 判据 | 读到什么 |
|---|---|---|
| ① | **SO 序列化体**：`d:/2/unity_run_ref/Warpforge_Data/sharedassets0.assets`（内置文件、**无 type tree** ⇒ 按原始字节解） | `m_Name`(len=24) → `count = 2` → 每条 `[style:int32][AssetGUID]`：**`style 0 (Default)` → `0195addac64424715b03a2310dfbbfe9`** · **`style 5 (HammerAndBolter)` → `cd516d1bf3fd249f3ad4776db89e70f7`** |
| ② | **远端 catalog**（旧 profile 缓存 · `工具/_guid_resolve_probe.py`，把它的 `CAT` 指到旧 profile 那份） | 两个 GUID 都解到**同一个束** `alternateartstyles_assets_all_9db3b696…bundle`；束内 `m_Container`：内部键 `'0'` → **`WarpForgeLogo Style`**（Texture2D + Sprite）、`'c'` → **`Cell Shaded Alternative Art Style`**（各 **512×128**、`ppu=100`） |
| ③ | **图的内容**（导出后亲看） | `Cell Shaded…` 印的是 **`CEL SHADED`**；`WarpForgeLogo Style` 印的是 **`WARPFORGE THE CARD GAME`** 徽标 |

⇒ **权威映射：`Default` = Warpforge 徽标 · `HammerAndBolter` = CEL SHADED**。
（②的两端我也**独立复算过一遍**：自己 base64 解 `m_KeyDataString`/`m_EntryDataString`/`m_BucketDataString`
得到 `internalId '0'` / `'c'`，与工具输出逐字相同 —— 不是拿工具的输出再证明工具。）

### 🔴 顺带查出来的真偏离（**另立账 `A1830`**）

| 判据 | 结论 |
|---|---|
| `AlternateArtSyle.cs`（签名桩） | **只有两个成员**：`Default = 0` / `HammerAndBolter = 5` |
| `AlternateArtStyleIconsSO__GetAlternateArtStyleOnly.c` + 它的 lambda（`d:/2/tools/decomp_full/`） | 方法体 = `styleIcons.FindAll(x => x.style != 0)` ⇒ **轮播里只列非 `Default` 的档** |
| `cards_1195_2022时代.json` 里的 `AlternateArtCard` SO | 导出的两件（`AA_HB_DarkAngels_war_Azrael` / `AA_HB_BlackLegion_war_Abaddon…`）**`style` 都是 5** |
| 那张卡的 `cardImage` GUID `50de37c7…` 现解 | **就是 `DarkAngels_AA_warlord_Azrael_v2`**（= 我们原来标 `v2` 的那张图） |
| 另 6 张的图片名 | `AA_HB_…` 前缀 或 `…_AA_HB` 后缀（同一个 HB 家族，另有 `Avatar_*_AA_HB` / `Cardback_*_AA_HB` / `UI_Deck_Warlord_*_AA_HB` 共用） |

⇒ **我们原来那「2 档风格」（`AA_HB` 6 张 + `v2` 1 张）是按【图片文件名】拆出来的，原版只有 1 档**；
7 张卡**全是 `HammerAndBolter`**，轮播里也就只有那一档。

---

## 二、改了什么

| 文件 | 改动 |
|---|---|
| 🆕 `工具/import_altstyle_logos.py` | 新导入器（**可复跑**）：从远端缓存那只 69,579 B 的包导出两张 512×128 logo；`--install` 装进 `Resources/Art/ui_menu/`，名字 = **原版资产名、空格换下划线**（`Cell_Shaded_Alternative_Art_Style` / `WarpForgeLogo_Style`，合 `CardArt.MenuUi` 的命名约定）。⚠️ 包**无 type tree** ⇒ 按裸类号 `28/213` 筛（**只按类型名筛会导出 0 张** —— 今天实测踩过） |
| `Resources/Art/ui_menu/*.png`（2 张，新） | 图本体。⚠️ 该目录在 `.gitignore` 里（原版美术一律本地重出）⇒ **clone 后要自己跑一次导入器** |
| `Shell/CollectionWindow.cs` | ① 7 张卡的 token 全改成**原版枚举名** `HammerAndBolter`（新增 `StyleHammerAndBolter` / `StyleDefault` 两个常量）② 新增 `StyleLogoName(style)` + `StyleLogo(style)`（映射 + 取图）③ **`Art Style Logo` 那一格改成画图**（按原版 `styleImage`），载不到才**退回画名并出声** ④ 4 处「远端 CCD / 服务端 / 2 种风格」的旧话就地订正（铁律 5） |
| `Shell/CardDetailPopup.cs` | 2 处旧话就地订正（铁律 5）：那条「风格图标 SO 在服务端」**不成立**；并把「那一格原版是 `Image`」与**还差什么**写清（见 §四） |
| `Editor/CollectionScene.cs` | 断言跟着改：① 那一格改断**画出了图 + 图是不是那一张 + 画出来是不是 512×128**（量法走 `QuadRectOf` = 独立于实现）② 风格数 **2 → 1**、异画读数 **6 → 7**（三处）③ **换风格那一段重写**：从「点右箭切到 `v2`、只剩 1 张、回绕」改成「**只有 1 档、两颗箭钮是死键**」④ 删掉「那行字渲染宽度 ≤ 512」那一条（它量的是**已不存在的文字**）；`A1173` 形参契约探针**保留**（它自己建 Label） |
| `资料/索引与盘点/解包资源使用地图.md` | `:1187` 那条「**`AlternateArtStyleIconsSO` 全库未提取 = 数据边界**」**划掉**，换成判据链（铁律 5） |

**验证**：秒级类型检查 `0 / 0`（中途被 `CS0165` 抓过一次 —— `gotQ && …` 短路让编译器证不出 `out` 参数已赋值，
已加初值；这正是那条检查存在的意义）。真机验收（进游戏看那一格）与 `CollectionScene.Run` **留到本会话收口一起跑**。

---

## 三、没做 / 没查清（如实）

1. 🔴 **卡详情窗 `Alternate Art Panel` 的 `Current Style` 那一格仍是文字**（原版喂 `Image`：
   `AlternateArtPanel__SetStyle.c` 里 `+0x30 styleImageContainer` 的 `set_enabled` + `set_sprite(该风格图标)`）
   —— **图已在工程、映射也有了，还差原版那颗 `styleImageContainer` 的矩形**（本地未核）⇒ 已立账 **`A1837`**，
   ⛔ 别照着我们现在那行文字的框硬套。
2. ⚠️ **原版那颗 `Image.m_PreserveAspect` 的值没查**（场景里 `m_Sprite = 0`、运行时才喂图）。
   我们传 `keepAspect: true`，但那一格与图**同比例**（512×128 = 4.0）⇒ **画出来逐位相同**，这一档不影响结果。
3. ⚠️ **`.png` 文件名为什么两套写法**（`AA_HB_…` 前缀 vs `…_AA_…_vN` 后缀）没查 —— 那是原版美术自己的命名。
4. ⚠️ 导出时 **Sprite 与 Texture2D 会各写一遍同一个目标文件**（同名同像素）⇒ 无害，但工具输出里会看到 4 行。
5. ⚠️ **原版轮播的「档数」我们只做了「1 档」这一档的行为**（箭钮是死键）—— 若将来远端还有别的档，
   结构不用改（列表建模保留），但**死键那两条断言要跟着改**（已在注释里标明）。

---

## 四、出处

- 包：`d:/2/远端缓存_旧profile_1013/Unity缓存_按哈希/7416b641…/9db3b696…/__data`（69,579 B，**本机唯一一份**，⛔ 别删）
- SO 本体：`d:/2/unity_run_ref/Warpforge_Data/sharedassets0.assets`（内置文件；`globalgamemanagers.assets` 里另有两条 MonoScript 注册）
- 反编译：`d:/2/tools/decomp_full/AlternateArtStyleIconsSO__GetAlternateArtStyleOnly.c` ·
  `AlternateArtStylesUtilities__GetStyleIcon.c` · `AlternateArtPanel__SetStyle.c`
- 签名桩：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/{AlternateArtSyle,AlternateArtCard,AlternateArtStyleItem,AlternateArtStyleIconsSO,AlternateArtCardCollectionTab}.cs`
- 卡 SO 导出：`d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`
- 远端 catalog：`d:/2/远端缓存_旧profile_1013/Everguild/com.unity.addressables/catalog_main.json`
- 上一轮账：`资料/普查产出_第二十七会话/远端缓存_残余深挖.md` §Q①（它把这张图记成「第三只包」）
