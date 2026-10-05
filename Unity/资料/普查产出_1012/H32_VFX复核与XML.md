# H32 · VFX/Arena1 块的复核（事故后）+ XML doc 转义补齐（A451 残块）

> 写手代理 · 2026-10-12。判据与做法沿用 H16（§三）与 H21（§2 / §七）。
>
> 一句话：**8 个件复核完毕 —— 7 个 VFX/Runtime 件确实是「== HEAD」，覆盖前那批转义**（H27 做的）**8 处全丢**；
> 已**逐字节复原**（照 H27 的改动表重做，**7/7 件与 H27 的意图逐字节相同**）。
> `ArenaByArmy.cs` 保留的就是 H27 已改的那 2 行 ⇒ **未动**。
> 本块 **CS1570 15 条 → 0**（8 处根因），常规类型检查 **0 / 0**。
> ⚠️ 同两个件另余 **4 条 `CS1573`**（缺 `<param>`）—— **不是 CS1570、不在 A451 口径内，本件没碰**（§六·7）。
> 🔴 **事故根因已查明**（见 §六·1）：不是「内容算错」，是 H27 的脚本**写盘循环用了一个过期的 `lines` 变量**。

---

## 一、复核表（动手前那一刻的状态）

| # | 文件 | 结论 | 证据 |
|---|---|---|---|
| 1 | `WarpforgeVFX/Runtime/ArenaOriginalMaterial.cs` | **== HEAD（逐字节）** | `git hash-object --path=<f> <f>` == `git rev-parse HEAD:<f>`；`git status --porcelain` 无该件 |
| 2 | `WarpforgeVFX/Runtime/ArenaParticleKeywords.cs` | **== HEAD** | 同上 |
| 3 | `WarpforgeVFX/Runtime/WFModuleChangeMaterial.cs` | **== HEAD** | 同上（CRLF 件） |
| 4 | `WarpforgeVFX/Runtime/WFModuleTransformModifier.cs` | **== HEAD** | 同上（CRLF 件） |
| 5 | `WarpforgeVFX/Runtime/WFModuleTween.cs` | **== HEAD** | 同上（CRLF 件） |
| 6 | `WarpforgeVFX/Runtime/WarpforgeEffectBinder.cs` | **== HEAD** | 同上（CRLF 件） |
| 7 | `WarpforgeVFX/Runtime/WarpforgeShaderMap.cs` | **== HEAD** | 同上（CRLF 件） |
| 8 | `WarpforgeArena1/Runtime/ArenaByArmy.cs` | **≠ HEAD，但内容干净**（H27 已改的 2 行） | `git diff` **只有 2 行**、全是 `///` 转义；`HEAD + H27 那 2 条 == 现值`（逐字节） |
| — | `WarpforgeArena1/Runtime/VFXWhiteboard.cs` | **== HEAD**（未被覆盖事故波及） | mtime 09-11，未在 H27 的 11 件里；0 条 CS1570 ⇒ **未动** |

* 🔑 **「== HEAD」是硬判据**：`git config core.autocrlf = false` 且**无 `.gitattributes`** ⇒ blob 哈希**逐字节可比**，
  不存在「行尾归一化让它看着像 HEAD」的假象。
* ✅ **无一件「≠ HEAD 且内容可疑」** ⇒ **没有触发停手条件**。
* 🔴 **丢失确认**：H27（做本块的写手）的工作目录**还在**（`/tmp/wf_h27/`，2026-10-12 23:36~23:42），
  其 `fix27.py` 的 `EDITS` 表 = **16 处 / 11 个文件**，其中**属于本块的就是这 8 个件 / 10 处**。
  逐件核过：**7 个 VFX 件的 8 处全丢**（回退到 HEAD 丢的），**`ArenaByArmy.cs` 的 2 处在**。
  ⇒ 「回退到 HEAD 会丢掉那部分工作」**属实**，且**丢的正好是这 8 处、没有别的**（H27 的 EDITS 里本块再没别的条目）。

---

## 二、转义清单（文件:行 · 条数 · 根因）

> 列 = 根因行（**注意行号会漂**：诊断常报在**后面 1~4 行**的闭合标记上）。
> 「条」= 该根因产出的 CS1570 诊断数（**条数 ≠ 处数**，见 §四）。

| 文件:行 | 改了什么 | 根因 | 条 |
|---|---|---|---|
| `WarpforgeVFX/Runtime/ArenaOriginalMaterial.cs:337` | `` `_SrcBlend==1 && _DstBlend==0` `` → `&amp;&amp;`（**2 个字符**） | 裸 `&` | 2 |
| `WarpforgeVFX/Runtime/ArenaOriginalMaterial.cs:393` | 同上（2 个字符） | 裸 `&` | 2 |
| `WarpforgeVFX/Runtime/ArenaParticleKeywords.cs:35` | 行尾**补** `</summary>` | **结构**：`<summary>` 开 1 关 0（块 25~35） | 1 |
| `WarpforgeVFX/Runtime/WFModuleChangeMaterial.cs:154` | `` `@asset:Material:<名字>` `` → `&lt;`（1 个字符） | 裸 `<` | 2 |
| `WarpforgeVFX/Runtime/WFModuleTransformModifier.cs:522` | `` `unParentAtStart && parentAtExit` `` → `&amp;&amp;`（2 个字符） | 裸 `&` | 2 |
| `WarpforgeVFX/Runtime/WFModuleTween.cs:69` | `` `@asset:MonoBehaviour:<名字>` `` → `&lt;`（1 个字符） | 裸 `<` | 2 |
| `WarpforgeVFX/Runtime/WarpforgeEffectBinder.cs:31` | `` `emissionOn && hadEmissionKeyword` `` → `&amp;&amp;`（2 个字符） | 裸 `&` | 2 |
| `WarpforgeVFX/Runtime/WarpforgeShaderMap.cs:359` | `` **`<noninit>`** `` → `&lt;`（1 个字符） | 裸 `<` | 2 |
| `WarpforgeArena1/Runtime/ArenaByArmy.cs:112` | `` `Battle_<场>` `` → `&lt;` | 裸 `<` | **0（H27 已改完 ⇒ 本件未动）** |
| `WarpforgeArena1/Runtime/ArenaByArmy.cs:113` | `` `Resources/ArenaPrefabs/<场>.prefab` `` → `&lt;` | 裸 `<` | **0（同上）** |
| **合计（本件实改 8 处）** | **11 个裸字符**（3×`<` · 8×`&`）+ **1 处结构** | **8 处根因 / 7 个文件** | **15 条** |

* ⚠️ **`WFModuleChangeMaterial` / `WFModuleTween` / `WarpforgeShaderMap` 那三行各有 1 个裸 `<` 却产 2 条诊断**
  （「结束标记 summary 与开始标记 X 不匹配」+「元素 summary 需要结束标记」）—— 这就是「条 ≠ 处」的来源。
* ⚠️ **`ArenaParticleKeywords.cs` 一个裸字符都没有**（字符扫描零命中），它只在**块级平衡扫描**里现形 ——
  **两把尺子缺一不可**（H16 §3·2）。

---

## 三、怎么验的

```text
# ① 本件专属 TMPDIR（⛔ 不与别人抢 /tmp/wfcheck/WFCheck.dll；⚠️ 必须传 Windows 形式路径，H21 §八·1）
export TMPDIR="C:/Users/qjh36/AppData/Local/Temp/wf_h32"
cd /d/4/Unity/MyGame

# ② 改前基线：`/doc` 编译（带 -utf8output 才有中文）
WF_DOC=1 bash d:/4/Unity/工具/typecheck.sh        # ← A453 那条可选档，本件用它生成 doc rsp
dotnet "C:/Program Files/dotnet/sdk/8.0.425/Roslyn/bincore/csc.dll" @…/wf_h32/wf_csc_doc.rsp -utf8output

# ③ 改后同一条命令 → 逐文件对比
  本块 7 件：4+1+2+2+2+2+2 = 15 条 CS1570  →  0  ✅
  ⚠️ **这一版读数是在「树里还有 11 条别人的 error」时取的** —— 见下面 ③b，这一版只当参考。

# ③b 🔴 **重测（唯一可比的那一版）**：同一棵 **0 error** 的树上的 apples-to-apples
  🔴 **坑**：`/doc` 的格式类警告**在有 error 时会缩水**（csc 编不过就不跑完整的 doc 校验）——
     实测同一批文件：树里有 error 时全程序集 CS1570/格式类只有 239 条，**0 error 时 531 条**。
     ⇒ **不同 error 状态的两次读数不可比**（这条是 H16/H21 也没写下的）。本件因此重测了一次：
      ① 把「改前快照」的 7 件拷到临时目录（不动仓库）
      ② 复制一份 doc rsp，把那 7 个源路径**换成副本路径**，其余一行不改
      ③ 两份 rsp 各编一次（都 0 error）
  结果：**CS1570 全程序集 241 → 226，差 = 15 = 本件的全部** ✅
        且**除本块这 7 件外，没有任何文件变化** ✅（CS1573/1574/1587/1572 四族逐条未变）
        ⚠️ 本块那两件「还剩 2 条」是 **CS1573**，不是 CS1570（见 §六·7）

# ④ 自证「改动行全是注释行」
git diff -U0 -- Unity/MyGame/Assets/WarpforgeVFX/ Unity/MyGame/Assets/WarpforgeArena1/Runtime/ > mydiff2.txt
  新增 10 / 删除 10 行；**非 `///` 的改动行 = 0**  ✅
  （那 10 行里 8 行是本件、2 行是 H27 早先对 `ArenaByArmy.cs` 做的，本件未动它）

# ⑤ 行尾（python 二进制数，改前 → 改后）
  7 件**行尾类型与 CR/LF 计数一个都没变**（2 件 LF · 5 件 CRLF）✅
  写盘脚本自带两条守卫：① 不许有 `\r` 落在【行内】 ② CR/LF 计数只允许按「插/删行数」变（本件插 0 删 0）

# ⑥ 块级标签平衡扫描（summary/remarks/para/code/example/list/item，逐连续 `///` 块）
  改前：零报 —— 除了 ArenaParticleKeywords.cs 25~35（summary 开 1 关 0）
  改后：**零报** ✅

# ⑦ 🔑 复原自证（本件最硬的一条）
  把 /tmp/wf_h27/fix27.py 的 EDITS 表抽出来，对【改前快照】做同一次替换，与【现值】逐字节比：
    7 个 VFX 件  → **逐字节相同** ✅
    ArenaByArmy  → 拿 `git show HEAD:<f>` 做同一次替换，**逐字节相同** ✅
  ⇒ 本件落盘的 = H27 的意图，**一字不多、一字不少**。

# ⑧ 常规类型检查（不带 /doc，= 工具/typecheck.sh 原样）
  改后（与 H27/别的写手那几条在飞改动一起测）：运行时 0 / 编辑器 0 ✅
  ⚠️ 中途两次跑出过 1 条 `Shell/ShopWindow.cs(782) CS0117: ShopData 未包含 GrantsOf` ——
     那是**别的写手正在写的活**（`Shell/**` 不在本件白名单，本件一行没碰），后来他们写完就没了。
```

* ⛔ **没跑 Unity** · ⛔ **没动 git**（只读 `git status` / `git diff` / `git show` / `git hash-object`）·
  ⛔ 没改两张正本 · ⛔ 没越白名单（⛔ 一行没碰 `Shell/` `Editor/` `Battle/` `Core|Deck|RuleEngine|Net/`）·
  ⛔ 没改 `工具/typecheck.sh` · ✅ **本件零自检**（按类型：纯 `///` 注释；用户口径「A 表清完再跑」）。

---

## 四、根因块计数（**条数 ≠ 处数**）

| 口径 | 数 |
|---|---|
| **`CS1570` 条数**（改前，本块 7 件） | **15**（改后 **0**） |
| **根因处数** | **8**（7 处裸字符行 + 1 处结构） |
| 其中**裸字符**个数 | **11**（3×`<` · 8×`&`） |
| 落到几个文件 | **7**（`ArenaByArmy.cs` 0 —— 已被 H27 改完） |
| 平均 | 1.875 条/处（H16 那块有过「1 行产 14 条」，所以**按条估工程量会偏大**） |
| ⚠️ **同两件另余 `CS1573`** | **4 条**（**另一族**：缺 `<param>` 标记；**不在 A451 的 CS1570 口径内**，见 §六·7） |

---

## 五、没查清 / 只能记账的

1. 🟡 **「覆盖前 H27 是否已经写盘到本块这 7 件」——只能间接证，不能直证**。
   现在能确证的是：① 7 件 == HEAD；② `ArenaByArmy.cs` 留着 H27 的 2 行（**说明 fix27.py 确实 `--apply` 过**）；
   ③ H27 的 `EDITS` 列了本块 10 处。⇒ **合理结论是那 8 处曾写盘、被回退抹掉**，
   但**我没有当时的工作区快照**（git 索引/HEAD 里没有那一版、`git status` 也不显示）。
   **不影响结果**：本件已把该做的 8 处做完，且与 H27 的意图逐字节相同。
2. 🟡 **`ArenaParticleKeywords.cs` 那条 `<summary>` 是「作者本来就漏了」还是「改一半」**：无从判断
   （只补闭合、**原文一字未动**，与 H16 §2·2 的裁法一致）。
3. ⚪ 本件**没有**别的留白 —— 本块 H27 的扫描（`/tmp/wf_h27/scan.txt` / `scan2.txt` / `block.py`）
   与我这两把尺子**逐条一致**（同样的文件、同样的行、同样的内容），**没有第三条线索被漏掉**。

---

## 六、顺手发现（⛔ 只报不改）

1. 🔴🔴 **事故根因已查明：`/tmp/wf_h27/fix27.py` 的写盘循环用了一个【过期的循环变量】。**
   它的结构是「先一个循环**逐文件算** `lines` → 再另一个循环**逐文件写** `b'\n'.join(lines)`」，
   而写的那一段**没有重新算 `lines`** ⇒ 写的永远是**第一个循环留下的最后一个文件**（= `byfile` 的末件
   `WarpforgeArena1/Runtime/ArenaByArmy.cs`）的行。
   **证据**：① 该脚本 `EDITS` 表的文件集合**恰好 11 个**（= 您说的「11 个 .cs」）；
   ② 写相位 `new_b = b'\n'.join(lines)` 里 `lines` 是外层残留变量（`p`/`old_b` 都在内层重算了，只有它没有）；
   ③ `ArenaByArmy.cs` 是字典末键 ⇒ **它自己写自己 = 内容正确**，这正好解释「为什么只有它保留自身内容」。
   ⇒ **11 件里 3 个 Shell 件（`AllianceMemberTab` / `CampaignTab` / `ShopData`）也各自被写成了 ArenaByArmy 的内容**，
   后来由各自写手重写/回退。（本件不碰它们，仅报。）
2. 🔴 **H27 对那 3 个 Shell 件的 6 处改动，现在【都不在树上】**（本件实测，只读）：
   `AllianceMemberTab.cs:213`（裸 `<`）· 同文件 `:568`（补 `</para>`）· `CampaignTab.cs:39`（裸 `<`）·
   `ShopData.cs:41 / :249 / :251`（裸 `&`×3 + 裸 `<`）——
   六条的 `new` 串在当前文件里**一次都搜不到**、`old` 串**原样在**。
   这与 `/doc` 基线吻合（那 3 件当时仍产 `AllianceMemberTab` 4 · `CampaignTab` 2 · `ShopData` 8 条 CS1570；
   ⚠️ 读数取自别位写手**正在改**的那一刻，是快照）。**那 3 件不在本件白名单 ⇒ 一行没碰**，
   但它们的写手**可能不知道 H27 已经算好了这 6 处**（连 old→new 串都是现成的）—— 建议转给他们。
3. 🟡 **`/tmp/wf_h27/recon/` 里躺着 3 个 Shell 件的重建副本**（`AllianceMemberTab.cs` 125 KB ·
   `CampaignTab.cs` 80 KB · `ShopData.cs` 23918 B），是 H27 从 `/tmp/wf_h16/mydiff.txt` 反推出来的
   「23:13 时刻的工作区版本」。**只报**：若那 3 件现在的版本不如 23:13 那版，这份是现成的参照。
4. 🟡 **同一批里 `CardPresentation/Editor/**` 与 `WarpforgeArena1/Editor/**` 共 11 个 `.cs` 的 mtime 全是
   `23:35:07`（同一亚秒时间戳）** —— 看着像一次批量写盘。**逐件核过：11 件内容各不相同、都是真件**
   （大小 8.9 KB ~ 679 KB），**不是**「同一份内容」那次覆盖 ⇒ **不疑**。（那是 A451 的 Editor 块。）
5. ✅ **反证一条：全仓 `.cs` 没有两个文件内容相同**（`sha256` 全表查重，0 组）
   ⇒ 那次覆盖**已经清干净了，没有残留件**。
6. ✅ **H16 §4·1 的记账逐条坐实**：`WarpforgeVFX/Runtime/*` = **15 条 / 7 件**、
   `WarpforgeArena1/Runtime/ArenaByArmy.cs` = **3 条 / 1 件** —— 本件实测**逐条吻合**
   （`ArenaByArmy` 那 3 条已由 H27 清掉，故本件基线是 0）。
7. 🟡 **本块 CS1570 已归零，但同两个件还剩 4 条 `CS1573`**（不是 `CS1570`，**本件没碰**）：
   `WFModuleTransformModifier.cs:132`（`TryGet(…)` 的 `<param name="p">` / `name="ctx"` 缺 2 个）·
   `WarpforgeEffectBinder.cs:209`（`Build(WFMatDef, bool)` 缺 `<param name="d">`）· 同文件 `:330`
   （`ApplyDerivedParticleDefaults(…)` 缺 `<param name="m">`）。
   **判据**：A451 的口径是 **CS1570（裸字符 / 结构）**，H16 §四 与 H21 §4·2 两张余量表数的**都是 CS1570**
   ⇒ `CS1573`（缺 `<param>`）**不在本块口径内**，本件**按不越界优先没碰**。
   ⚠️ 全程序集这一族 **246 条**（`CS1573`）—— 要不要专门开一刀，**请调度台裁**。
8. 🔴 **`/doc` 的格式类警告计数【在有 error 时会缩水】—— 两次读数因此不可比**（本件实测）：
   同一批文件，树里带 11 条 error 时全程序集只报 **239** 条格式类；树清成 0 error 后报 **531** 条
   （差的 292 条几乎全是 `CS1573` 那一族，**CS1570 也少了**）。⇒ 以后**报「清了多少条」必须写明当时树里有没有 error**，
   否则会得出「某人清完反而涨了」这种假象。本件 §三·③b 的重测就是为了消掉这个歧义。

---

## 七、还欠什么

* ✅ **本件范围（`WarpforgeVFX/Runtime/**` + `WarpforgeArena1/Runtime/**`）CS1570 已归零**，
  且与 H27 的意图逐字节相同 ⇒ **不需要再派第二个写手重做**。
* ⛔ **本件范围外，一条没动**：`Shell/` 剩余（含 §六·2 那 6 处 H27 算好的）· `Battle/` · `Core|Deck|Net|RuleEngine` 的余量 ·
  Editor 程序集 —— 切块见 H16 §四 / H21 §4·2。
* 📌 复现本件全部读数：脚本在 `C:/Users/qjh36/AppData/Local/Temp/wf_h32/`
  （`scan32.py` 字符扫描 · `blk32.py` 块级平衡 · `fix32.py` 带断言的 DRY-RUN/`--apply` ·
  `before/` 改前快照 · `base_runtime.txt` / `after_runtime.txt` / `mydiff2.txt`）—— **临时目录，不随仓库走**。
  ⚠️ **复用 `fix32.py` 前**：`KNOWN` 白名单**只能放真 XML doc 标签**（⛔ **别加 `link`**，H21 §八·2 踩过）。
