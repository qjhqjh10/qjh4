# B117-WPOPUP · 菜单/收藏详情窗的卡面档位（执行代理交付 · 2026-11-02）

> 任务 = 把 `Shell/CardDetailPopup.cs` 建的卡改成原版那一档。
> **结论一句话**：原版菜单详情窗是**第 4 个档位**，`CardUse.ZoomWindow` 只对上 2/6 个值 ⇒ **必须新增 `CardUse.MenuDetail`**。
> ⛔ `Core/CardView.cs` 归 `B117-WCARD` 独占 ⇒ 本代理**一行源码都没写**，只交**可粘贴补丁 + 调用链自证**（见 §四、§五）。
> 本会话：没跑 Unity 批处理、没动 git、没改 `d:/2`、没改任何 `.cs`。只跑了一次 `typecheck`（0 / 0，见 §七）。

---

## 一、判定：**新增 `CardUse.MenuDetail`**，⛔ 不能用 `CardUse.ZoomWindow`

### 1·1 逐值对照（原版 prefab 字面量 vs 我们三档现值）

| 项（原版节点） | **原版菜单详情窗** | `Standard`（我们现值） | `ZoomWindow`（我们现值） | 逐值相同？ |
| --- | ---: | ---: | ---: | --- |
| 卡名 `NameTextUnit` 上界 `m_fontSizeMax` | **16** | 19（`NameUnitBandMax` 1.595） | **19**（同上 1.595） | ❌ |
| 效果 `DescTextUnit` 上界 | **16** | 24（`DescUnitBandMax` 1.560） | 24（同 1.560） | ❌ |
| 兵种 `MinionOrWarlord/RaceText` | **16** | 18（`RaceEmOfCard` 0.18） | 18（同） | ❌ |
| 阵营 `ArmyTextUnit` | **16** | 15（`ArmyEmOfCard` 0.15） | 15（同） | ❌ |
| 卡名名义 `m_fontSize` | 16 | 19（`TitleEmOfCard` 0.04275） | **16**（`TitleEmOfCardZoom` 0.0360） | ✅ |
| 阴影 alpha | **0.8** | 0.604 | **0.8** | ✅ |

⇒ **`ZoomWindow` 只对上「名义卡名 16」+「阴影 0.8」两项**，另外四项全等于 `Standard`（`DescUnitBandMaxForUse` / `ArmyFontSizeForUse` / `RaceFontSizeForUse` / `NameUnitBandMaxForUse` 里 `ZoomWindow` 都没有分支）。
所以「最小改 = 加 `use: CardUse.ZoomWindow`」**只能修一半**，而且会把卡名**上界**留在 19（`FitToBox` 装得下就一路涨到 `bandMax`，见 §三·4）⇒ 卡名照样渲染成 19。**不可接受**。

### 1·2 为什么不能把 `ZoomWindow` 扩成菜单值：**两棵树的上界真的不同**

| | 原版 `m_fontSize / m_fontSizeMax` | 出处 |
| --- | --- | --- |
| 战斗放大窗（`Card Display Window/Card Display/Cards`） | **16 / 19** | `资料/普查产出_1015战斗/C1_逐格对账_卡面放大窗与卡面渲染.md` 表三·1（13 arena × 8 实例，`fs=16` 恰好 65 个 = 每场 5 张） |
| 菜单详情窗（`Card Displayer Menu For Menu/Card Display/Cards/CardUI…`） | **16 / 16** | 本会话亲解 YAML（§二） |

⇒ **同一个 `ZoomWindow` 档位不能同时满足 `max=19` 和 `max=16`**。这正是任务书 §一·1 说的「若两者需要不同值 ⇒ 改为新增一档」。
另外战斗放大窗的**效果 / 兵种 / 阵营三行本来就没动**（走标准档），菜单详情窗要降到 16 ⇒ 也不共用。

---

## 二、一手读数（我自己复核的，逐张 5 份全同值）

来源：`d:/2/tools/assetripper_out/ExportedProject/Assets/Data/Scenes/MainMenu Warpforge.unity`
路径：`Card Displayer Menu For Menu / Card Display / Cards / CardUI … CardUI (4)`（**5 张**，逐张同值；复算脚本走的是「按祖先链归属」而不是树序，避开 `m_Children` 序的坑）

| 节点（在 `Card UI/2DCard/Front/Card Info/Name and description/` 下） | `m_fontSize` | `m_fontSizeMax` | `m_IsActive` |
| --- | ---: | ---: | ---: |
| `MinionOrWarlord Container/NameTextUnit` | **16** | **16** | 1 |
| `MinionOrWarlord Container/DescTextUnit` | **16** | **16** | 1 |
| `MinionOrWarlord Container/RaceText` | **16** | **16** | 1 |
| `MinionOrWarlord Container/ArmyTextUnit` | **16** | **16** | 0 |
| `MinionOrWarlord Container/NameTextUnit No description` | **19** | **19** | 0 |
| `MinionOrWarlord Container/Army No Desciption` | **15** | **15** | 0 |
| `Tactic Container/NameTextTactic` | 21.5 | 24 | 1 |
| `Tactic Container/DescTextTactic` | 19 | 19 | 1 |
| `Tactic Container/ArmyTextTactc` | 14 | 15 | 0 |
| `Tactic Container/RaceText` | 18 | 18 | 0 |
| `…Big` 全族（`NameTextUnit Big` / `DescTextUnit Big` / `RaceText Big` / …） | 21.15 / 26 / 22 … | 24 / 26 / 22 | **全 0** |

**阴影**：`d:/2/tools/decomp_full/CardDisplayWindow__InitializeCardForDisplay.c:46-51` 对每张卡**无条件**调 `ChangeShadowColor(…, (0,0,0,0.8))`；菜单与战斗是同一个类同一段代码（`CardDisplayWindow__InitializeCards.c:8` → `InitializeCardForDisplay`），**没有分支**。

### 2·1 对既有文档的两处订正（铁律 5，就地记在这里）

1. **`B117-GC`（GC-3）没读到「无描述」那两颗**，容易被误当成「跟着一起降到 16」。
   实际：`NameTextUnit No description` = **19/19**、`Army No Desciption` = **15/15** ⇒ **不跟着降**（与我们现在的 `noDescName ? NameUnitBandMax : …` 恰好一致 ⇒ **`CardView.cs:4987` 那一行不用改**）。
2. `Core/CardView.cs:6030` 的 doc 写「`CardUI (4)/…/NameTextUnit` 的 TMP：`m_fontSize=16` · `min=1` · `max=19`」——**那是【战斗 arena】那一棵**，没错；但⛔ 别把它当菜单详情窗的读数，**菜单这一棵是 16/16**。已在新档的 doc 里写清两棵树的差别。
3. `B117-GC` 写的「5 张，与 `Core/CardFan` 的五槽同源」**成立**（`m_Name: CardUI` 5 命中：无后缀 / (1) / (2) / (3) / (4)）——但常规的「沿 `m_Children` 往下走」只会走到 2 张，**得按祖先链反查**才拿全 5 张（本报告就是按祖先链取的）。

---

## 三、六个值的落地换算（新档 `MenuDetail`）

我们内部的三个单位与它们的换算关系（**都不是新造的口径，全部沿用标准档那一格反解**）：

| 量 | 我们单位 | 换算 | 新档值 |
| --- | --- | --- | --- |
| 卡名上界 | `NameUnitBandMax` = 1.595 = **19 pt** | `× 16/19` | **1.3431579** |
| 效果上界 | `DescUnitBandMax` = 1.560 = **24 pt** | `× 16/24` | **1.040** |
| 阵营 em | `ArmyEmOfCard` = 0.15/3.3313 = **15 pt** | 直接写 16 pt | **0.1600 / 3.3313** |
| 兵种 em | `RaceEmOfCard` = 0.18/3.3313 = **18 pt** | 直接写 16 pt | **0.1600 / 3.3313** |
| 卡名名义 | `TitleEmOfCard` = 0.04275 = 19 pt（已含 ×0.75 实拍系数） | 用现成的 `TitleEmOfCardZoom`（= 16 pt 同一套系数） | **0.0360** |
| 阴影 alpha | `ShadowAlpha` | **0.8**（同 `ZoomWindow`） | **0.8** |

- **阵营 / 兵种的 `bandMax`**（`ArmyBandMaxForUse` / `RaceBandMaxForUse`）原版 16pt ⇒ **1.60**。⚠️ 这两行今天**没有框高**（`Fill` 的 `maxHeight` 传 0）⇒ 只是文档值、不跑 `FitToBox`，但一起改对（「给框高那天不用再查」）。
- **战术卡三行不动**：菜单树里 `NameTextTactic` 21.5/**24** · `DescTextTactic` 19/19 · `ArmyTextTactc` 14/**15**，与标准档**逐值相同** ⇒ 新档不给 tactic 分支。
- **`DescTacticBandMax` / `NameTacticBandMax` 也不动**。
- ⚠️ **实拍系数的适用性**：`NameUnitBandMax`(1.595) 与 `DescUnitBandMax`(1.560) 里的 `×0.75 / ×0.78 / ×1.1193 / ×0.8333` 是 `B117_F14` / `B117_WCARD` **按标准档（19/24pt）的发行版实拍**标定的；菜单详情窗这一档**没有单独的实拍读数** ⇒ 本补丁按**同一比例**折。若日后量到详情窗实拍偏大/偏小，改这两个比例即可（见 §七）。

---

## 四、补丁（**给 `B117-WCARD` / 调度台合并** · 锚点全部是**当前文本**）

> 快照：`Core/CardView.cs` **md5 `e36d265d…` · mtime `2026-11-02 12:29:16`**。
> ⚠️ 那个文件此刻有写手在改（我 20 分钟内两次读到不同的 Booster 值）⇒ **按引号里的原文定位，别按行号**。
> ✅ 除 `P1` 外全是**同一物理行改写**（行数不变，不制造账漂移）；`P1` 一个块 **+13 行**（🔴 2026-11-02 `WMENUDETAIL` 落地时就地订正：原文写「+7」是错的，doc 块自身 12 行注释 + 1 行枚举 = 13）。

### P1 · `CardUse` 枚举（原 `:258` 之后，`Booster = 2,` 与 `}` 之间）

**old**（原 `:257-258`，两行）：
```
        /// ⇒ 有效字号本来就一样。判据全文 → `资料/完全复刻/报告/B106_W9.md`。</summary>
        Booster = 2,
```
**new**：
```
        /// ⇒ 有效字号本来就一样。判据全文 → `资料/完全复刻/报告/B106_W9.md`。</summary>
        Booster = 2,
        /// <summary>🆕 2026-11-02（`B117-WPOPUP`）**菜单 / 收藏的卡片详情窗**
        /// （`Card Displayer Menu For Menu / Card Display / Cards / CardUI …`，宿主 `Shell/CardDetailPopup.cs`）。
        /// 判据（一手，YAML 亲解 `ExportedProject/Assets/Data/Scenes/MainMenu Warpforge.unity`，5 张卡逐张同值）：
        /// 单位卡四行**全是 16 / max 16** —— `NameTextUnit` · `DescTextUnit` · `MinionOrWarlord Container/RaceText` ·
        /// `ArmyTextUnit`（对照标准档 `19/19` · `23.55/24` · `18/18` · `15/15`）；战术卡三行与标准档**同值**
        /// （`NameTextTactic` 21.5/**24** · `DescTextTactic` 19/19 · `ArmyTextTactc` 14/**15**）⇒ 本档不给 tactic 分支。
        /// ⚠️ **「无描述」那两颗不跟着降**：`NameTextUnit No description` = **19/19**、`Army No Desciption` = **15/15**
        /// （⇒ `NameUnitBandMaxForUse` 的 `noDescName` 那一支**保持** `NameUnitBandMax`；阵营行走 `ArmyFontSizeForUse(unit, noDesc)`）。
        /// 阴影 = `CardDisplayWindow__InitializeCardForDisplay.c:46-51` 运行时**无条件**写 `(0,0,0,0.8)`（同放大窗）。
        /// 🔴 **与 `ZoomWindow` 不是同一档**：那份原版是 `16 / max 19`（13 arena × 5 张现读），本档是 **16 / max 16**；
        /// 而且放大窗的效果 / 兵种 / 阵营三行**走标准档**、本档要降到 16。⇒ 必须另立一档，⛔ 别合并。
        /// 判据全文 → `资料/完全复刻/判据/B117-WPOPUP_菜单详情窗卡档.md`。</summary>
        MenuDetail = 3,
```

### P2 · 卡名名义字号（原 `:6061`，`NameFontSize`）
**old**：
```
                    Height * (_use == CardUse.ZoomWindow ? TitleEmOfCardZoom : TitleEmOfCard));
```
**new**：
```
                    Height * ((_use == CardUse.ZoomWindow || _use == CardUse.MenuDetail) ? TitleEmOfCardZoom : TitleEmOfCard));
```

### P3 · 阴影 alpha（原 `:6397`）
**old**：
```
        float ShadowAlpha { get { return _use == CardUse.ZoomWindow ? 0.8f : 0.604f; } }
```
**new**：
```
        float ShadowAlpha { get { return (_use == CardUse.ZoomWindow || _use == CardUse.MenuDetail) ? 0.8f : 0.604f; } }
```

### P4 · 卡名 autosize 上界（原 `:15564`）
**old**：
```
        float NameUnitBandMaxForUse { get { return _use == CardUse.Booster ? 1.679f : NameUnitBandMax; } }
```
**new**：
```
        float NameUnitBandMaxForUse { get { return _use == CardUse.Booster ? 1.679f : (_use == CardUse.MenuDetail ? NameUnitBandMax * 16f / 19f : NameUnitBandMax); } }
```
> `NameUnitBandMax`(1.595) 是**标准档 19pt** 折出来的 ⇒ `× 16/19` 就是原版菜单那格的 16pt。⚠️ 只有标准 / 放大 / 菜单三档共用这一组实拍系数；`Booster` 系数不同（1.679）⇒ 保持原样。

### P5 · 效果 autosize 上界（原 `:15568`）
**old**：
```
        float DescUnitBandMaxForUse { get { return _use == CardUse.Booster ? 1.235f : DescUnitBandMax; } }
```
**new**：
```
        float DescUnitBandMaxForUse { get { return _use == CardUse.Booster ? 1.235f : (_use == CardUse.MenuDetail ? DescUnitBandMax * 16f / 24f : DescUnitBandMax); } }
```

### P6 · 阵营 bandMax（原 `:15576`，文档值）
**old**：
```
        float ArmyBandMaxForUse(bool unit) { return (_use == CardUse.Booster && unit) ? 1.70f : ArmyBandMax; }
```
**new**：
```
        float ArmyBandMaxForUse(bool unit) { return (_use == CardUse.Booster && unit) ? 1.70f : ((_use == CardUse.MenuDetail && unit) ? 1.60f : ArmyBandMax); }
```

### P7 · 兵种 bandMax（原 `:15582`，文档值）
**old**：
```
        float RaceBandMaxForUse(bool unit) { return (_use == CardUse.Booster && unit) ? 1.70f : RaceBandMax; }
```
**new**：
```
        float RaceBandMaxForUse(bool unit) { return (_use == CardUse.Booster && unit) ? 1.70f : ((_use == CardUse.MenuDetail && unit) ? 1.60f : RaceBandMax); }
```

### P8 · 阵营行字号（原 `:15594` 签名 + `:15597` 体，两行都改）
**old `:15594`**：
```
        float ArmyFontSizeForUse(bool unit)
```
**new**：
```
        float ArmyFontSizeForUse(bool unit, bool noDesc = false)
```
**old `:15597`**：
```
                Height * ((_use == CardUse.Booster && unit) ? ArmyEmOfCardBooster : ArmyEmOfCard));
```
**new**：
```
                Height * ((_use == CardUse.Booster && unit) ? ArmyEmOfCardBooster : ((_use == CardUse.MenuDetail && unit) ? (noDesc ? 0.1500f : 0.1600f) / 3.3313f : ArmyEmOfCard)));
```
> `noDesc` 的判据 = 原版 `Army No Desciption` **15/15**（无描述的单位卡阵营行不跟着降到 16）。默认 `false` ⇒ `ArmyFontSizeForTest(bool)` 等既有调用零影响。

### P9 · 兵种行字号（原 `:15605`）
**old**：
```
                Height * ((_use == CardUse.Booster && unit) ? RaceEmOfCardBooster : RaceEmOfCard));
```
**new**：
```
                Height * ((_use == CardUse.Booster && unit) ? RaceEmOfCardBooster : ((_use == CardUse.MenuDetail && unit) ? 0.1600f / 3.3313f : RaceEmOfCard)));
```
> 兵种行**只有一个** `MinionOrWarlord Container/RaceText` 节点（16/16），战术那颗是 `Tactic Container/RaceText`(18/18)，我们按 `unit` 分档 ⇒ 不需要 `noDesc`。

### P10 · 阵营行调用点（原 `:5098`，`BuildTextLayers` 里）
**old**：
```
            _army = Fill(_army, "army", CardText.Faction(d.faction), armyAt, nameW, ArmyFontSizeForUse(unit), InkArmy, false,
```
**new**：
```
            _army = Fill(_army, "army", CardText.Faction(d.faction), armyAt, nameW, ArmyFontSizeForUse(unit, noDescName), InkArmy, false,
```
> `noDescName` 就在同一方法里（原 `:4956`）⇒ 直接可用。

### P11 · 「这一张算大图档」的判据（原 `:2237`，`IsBigCardView`）
**old**：
```
            if (use == CardUse.ZoomWindow) return true;
```
**new**：
```
            if (use == CardUse.ZoomWindow || use == CardUse.MenuDetail) return true;
```
> 语义上是必需的（显式档位 = 详情窗大图档）；**行为上今天两条路都通**（`CardDetailPopup` 那 5 张叫 `CardUI (n)` 且祖上有 `Card Display`，走第二条判据也会返回 true）⇒ 这一条是**防以后改名/挪树时静默失效**，不是修 bug。
> ⚠️ 同一段 doc（原 `:2226-2228`）写着「菜单那扇详情窗**不传 `use`**（走默认 `Standard`），只能靠这条认」—— 合并后**这句过期**，请顺手就地改成「菜单那扇传 `CardUse.MenuDetail`」。

### P12 · 两处 doc 顺手订正（可选，同一物理行改写）
- 原 `:6031-6032`（`TitleEmOfCardZoom` 的 doc）：在「`m_fontSize=16` · `min=1` · `max=19`」后补一句
  `（这是【战斗 arena】那一棵；菜单详情窗那一棵是 16/**16**，见 CardUse.MenuDetail）`。
- 原 `:2183`：`🔴 **只有战斗放大窗要传 `CardUse.ZoomWindow`**` → 补「菜单/收藏详情窗传 `CardUse.MenuDetail`」。

---

## 五、`Shell/CardDetailPopup.cs` 的改法（**两处，其中一处是给断言留读口**）

### P13 · 档位常量 —— **必须追加在类尾**（`CardDetailPopup.cs` 被账按行号引用 **177 + 69 处**，插在常量区会让整片下游漂移）

**锚点 = 文件末尾**（现读 `:3589-3594`）：
```
            foreach (var key in result.Keys) wholeList.Remove(key); // 原版第二段：把这批从源表移除
            return result;
        }
    }
}
```
**改成**（在「方法收尾的那个右花括号」与「类收尾的右花括号」之间插入）：
```
            foreach (var key in result.Keys) wholeList.Remove(key); // 原版第二段：把这批从源表移除
            return result;
        }

        /// <summary>🔴 **本窗建卡用的卡面档位**（判据 → `资料/完全复刻/判据/B117-WPOPUP_菜单详情窗卡档.md`）。
        /// 原版 `Card Displayer Menu For Menu` 那 5 张 = 单位卡四行全 `16/16` + 阴影 `0.8`，
        /// 与战斗放大窗（`16/max 19`）**不是同一档** ⇒ 单独一档 `CardUse.MenuDetail`。
        /// ⚠️ 别改回 `Standard`（那是 19 / 23.55 / 18 / 15 + 0.604 ⇒ 卡名大 18.75%、效果大 ~47%）。
        /// 📌 追加在**类尾**（⛔ 别插进常量区）：本文件被账按行号引用 177 + 69 处。</summary>
        public const CardUse PopupCardUse = CardUse.MenuDetail;
    }
}
```
> 🔴 **2026-11-02 `WMENUDETAIL` 落地时就地订正：这句不成立** —— `CardDetailPopup.cs` **最末那对 `}` 关的是 `CardSell`**（`:3440`），`CardDetailPopup` 的真类尾在 **`:3411`**。照「文件末尾」锚点追加会把 `PopupCardUse` 落进 `CardSell` ⇒ `CardDetailPopup.PopupCardUse` 不存在、断言编不过。实际落法是插在真类尾之后 ⇒ 该文件 **+7 行**（账里 8 条引用落在 `:3411` 之后的要 +7）。
```csharp
        /// <summary>🔴 **本窗建卡用的卡面档位**（判据 → `资料/完全复刻/判据/B117-WPOPUP_菜单详情窗卡档.md`）。
        /// 原版 `Card Displayer Menu For Menu` 那 5 张 = 单位卡四行全 `16/16` + 阴影 `0.8`，
        /// 与战斗放大窗（`16/max 19`）**不是同一档** ⇒ 单独一档 `CardUse.MenuDetail`。
        /// ⚠️ 别改回 `Standard`（那是 19 / 23.55 / 18 / 15 + 0.604，卡名大 18.75%、效果大 ~47%）。</summary>
        public const CardUse PopupCardUse = CardUse.MenuDetail;
```

### P14 · 建卡那一行（原 `:1203`）
**old**：
```
                var v = CardView.Create(node, cd, SlotName(i),
                                        artOverdraw: i == 0,
                                        useOverDraw: def != null ? def.UseOverDraw : true);
```
**new**（⚠️ 把 `use:` 写进**同一物理行**，**行数零变化** —— 原 `:1203` 下面还有 2000+ 行、账有落点）：
```
                var v = CardView.Create(node, cd, SlotName(i), use: PopupCardUse,
                                        artOverdraw: i == 0,
                                        useOverDraw: def != null ? def.UseOverDraw : true);
```

---

## 六、断言文件全文（新建 `Assets/CardPresentation/Editor/B117_WPOPUP_Assert.cs`）

> 必须**等 P1..P11 合并后**才能落盘（它引用 `CardUse.MenuDetail`）⇒ 所以放在补丁里，本代理没建。
> 宿主：建议挂 `Editor/CollectionScene.cs`（与 `B117F14_Assert` 同一宿主 —— 卡面管线在批处理里已验证能建出卡）。
> 判绿红：`grep "B117WPOPUP"` 的合计行；逐条按**行首** `B117WPOPUP ✓` / `B117WPOPUP ✗` 数（⛔ 别 `grep -c ✗`）。

```csharp
// B117_WPOPUP_Assert.cs — 执行代理 `B117-WPOPUP`（菜单 / 收藏详情窗的卡面档位）的断言宿主
//
// ============================ 判据（一手，YAML 亲解）============================
//   `d:/2/tools/assetripper_out/ExportedProject/Assets/Data/Scenes/MainMenu Warpforge.unity`
//   → `Card Displayer Menu For Menu / Card Display / Cards / CardUI … CardUI (4)`（5 张，逐张同值）：
//     单位卡四行 `NameTextUnit` / `DescTextUnit` / `MinionOrWarlord Container/RaceText` / `ArmyTextUnit`
//       全是 **m_fontSize = m_fontSizeMax = 16**（对照标准档 19/19 · 23.55/24 · 18/18 · 15/15）
//     无描述两颗 `NameTextUnit No description` = **19/19** · `Army No Desciption` = **15/15**（不跟着降）
//     战术卡三行 = 标准档同值（NameTextTactic 21.5/**24** · DescTextTactic 19/19 · ArmyTextTactc 14/**15**）
//     阴影 = `CardDisplayWindow__InitializeCardForDisplay.c:46-51` 无条件 `(0,0,0,0.8)`
//   战斗放大窗那一棵（**不同树**）= `16 / max 19`（`资料/普查产出_1015战斗/C1_逐格对账…` 表三·1）
//
// ============================ 为什么这不是「我们自己的常量」============================
//   期望值一律写成**原版 prefab 的 pt 字面量**（16 / 19 / 24 / 0.8）。
//   我们的实现单位（band 1.595、em 0.16/3.3313）通过**标准档那一格**折算回 pt 再比：
//     pt = 本档 bandMax ÷ (标准档 bandMax ÷ 标准档 pt)
//   ⇒ 若把菜单档改回标准档，得到 19（≠16）⇒ 红；若把标准档一并改小，折算会跟着变 ⇒ 也红（不是自证）。
//
// ============================ 断言一览（21 条 Check 调用）============================
//   接线 1 · 前提 4 · 菜单档 7 · 战术不动 2 · 灭自证 2 · 战斗放大窗回归 5 = 21
//   （正文编号 ①–⑯ 只覆盖「被测项」；接线 + 4 条前提不计编号）
//
// 用法（⚠️ 要主对话跑 —— 子代理不许 -executeMethod）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod CollectionScene.Run -logFile -
using CardPresentation;
using RuleEngine;
using UnityEngine;

public static class B117_WPOPUP_Assert
{
    const string P = "B117WPOPUP ";

    static int _pass, _fail;
    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(P + "✓ " + msg); }
        else { _fail++; Debug.LogError(P + "✗ " + msg); }
    }

    static void CheckNear(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol,
              msg + "（期望 " + want.ToString("F5") + " ± " + tol.ToString("F5") + "；实得 " + got.ToString("F5") + "）");
    }

    // ---- 原版 prefab 的字面量（⛔ 别从实现常量反推）----
    const float PtUnitRow      = 16f;    // 菜单详情窗：NameTextUnit / DescTextUnit / RaceText / ArmyTextUnit 的 m_fontSizeMax
    const float PtStdName      = 19f;    // 标准档 CardUI：NameTextUnit 的 m_fontSizeMax
    const float PtStdDesc      = 24f;    // 标准档 CardUI：DescTextUnit 的 m_fontSizeMax
    const float PtZoomNameMax  = 19f;    // 战斗放大窗：NameTextUnit 的 m_fontSizeMax（16/max 19）
    const float AlphaMenu      = 0.8f;   // InitializeCardForDisplay 无条件写的 (0,0,0,0.8)
    const float AlphaStd       = 0.604f; // 标准档材质默认

    public static void Run()
    {
        _pass = 0; _fail = 0;
        GameObject root = null;
        try
        {
            // ---- 0. 接线：宿主真的传了这一档 ----
            Check(CardDetailPopup.PopupCardUse == CardUse.MenuDetail,
                  "接线：`CardDetailPopup` 建卡走 `CardUse.MenuDetail`（实得 " + CardDetailPopup.PopupCardUse + "）");

            // ---- 1. 前提（硬检查，⛔ 不用 `if (前提) Check(...)` 那种会静默跳过的写法）----
            Check(TmpFont.Available, "前提：字体资产取得到（取不到时 title/keywords 根本不建，下面全空转）");
            if (!TmpFont.Available) return;

            var pool = CardDatabase.Load();
            CardDef def = null;
            for (int i = 0; i < pool.Count; i++) if (pool[i].Name == "Uriel Ventris") { def = pool[i]; break; }
            Check(def != null, "前提：卡池里有 `Uriel Ventris`（B117_F14 用的同一张真卡）");
            if (def == null) return;

            root = new GameObject("b117wpopup_probe");
            var disp = new GameObject("Card Display");              // 原版那层祖先（`IsBigCardView` 的第二条判据靠它）
            disp.transform.SetParent(root.transform, false);

            var cd = BattleDriver.ToCardData(def, def.Faction);
            var std  = CardView.Create(disp.transform, cd, "CardUI");                              // 对照：标准档
            var menu = CardView.Create(disp.transform, cd, "CardUI", use: CardUse.MenuDetail);      // 被测
            var zoom = CardView.Create(disp.transform, cd, "CardUI", use: CardUse.ZoomWindow);      // 回归：战斗放大窗
            Check(std != null && menu != null && zoom != null, "前提：三张探针卡都建出来了");
            if (std == null || menu == null || zoom == null) return;
            Check(menu.Use == CardUse.MenuDetail,
                  "前提：MenuDetail 探针没被 `IsBoosterCardView` 改写（实得 " + menu.Use + "）");

            float stdNameBand = CardView.BandMaxForTest("NameUnit");   // 标准档 19pt 折我们单位（= 1.595）
            float stdDescBand = CardView.BandMaxForTest("DescUnit");   // 标准档 24pt 折我们单位（= 1.560）

            // ---- 2. 菜单详情档：六个值（期望值 = 原版 prefab 字面量）----
            CheckNear(menu.NameFontSize / std.NameFontSize, PtUnitRow / PtStdName, 1e-4f,
                      "① 详情窗卡名**名义**字号 = 标准档 × 16/19（旧实现 = 1.0 ⇒ 红）");
            CheckNear(menu.NameBandMaxForTest / (stdNameBand / PtStdName), PtUnitRow, 1e-3f,
                      "② 详情窗卡名 **autosize 上界 = 16 pt**（原版 `NameTextUnit` 16/16；旧实现 19 ⇒ 红）");
            CheckNear(menu.DescBandMaxForTest / (stdDescBand / PtStdDesc), PtUnitRow, 1e-3f,
                      "③ 详情窗效果 **autosize 上界 = 16 pt**（原版 `DescTextUnit` 16/16；旧 24 ⇒ 红）");
            CheckNear(menu.ArmyFontSizeForTest(true), 1.6f, 1e-4f,
                      "④ 详情窗阵营行字号 = 16 pt（原版 `ArmyTextUnit` 16/16；旧 15 ⇒ 红）");
            CheckNear(menu.RaceFontSizeForTest(true), 1.6f, 1e-4f,
                      "⑤ 详情窗兵种行字号 = 16 pt（原版 `RaceText` 16/16；旧 18 ⇒ 红）");
            Check(menu.CardShadowLayerBuilt,
                  "⑥ 前提：MenuDetail 探针的阴影层建出来了（shader/SDF 取不到时这条红，红过才谈下面的 α）");
            CheckNear(menu.CardShadowAlphaForTest, AlphaMenu, 1e-3f,
                      "⑦ 详情窗阴影 alpha = 0.8（旧 0.604 ⇒ 红）");

            // ---- 3. 战术卡三行**不跟着降**（原版与标准档同值）----
            CheckNear(menu.ArmyFontSizeForTest(false), 1.5f, 1e-4f,
                      "⑧ 战术 `ArmyTextTactc` 仍是 max 15（原版 14/15 ⇒ 不动）");
            CheckNear(menu.RaceFontSizeForTest(false), 1.8f, 1e-4f,
                      "⑨ 战术 `RaceText` 仍是 18（原版 18/18 ⇒ 不动）");

            // ---- 4. 灭自证：同一组读口在标准档上必须给出**旧值** ----
            CheckNear(std.NameBandMaxForTest / (stdNameBand / PtStdName), PtStdName, 1e-3f,
                      "⑩ 灭自证 · 标准档卡名上界 = 19（证明读口能分档，不是恒返回 16）");
            CheckNear(std.CardShadowAlphaForTest, AlphaStd, 1e-3f,
                      "⑪ 灭自证 · 标准档阴影 = 0.604（不是恒返回 0.8）");

            // ---- 5. 回归：战斗放大窗那一条没被改坏 ----
            CheckNear(zoom.NameFontSize / std.NameFontSize, PtUnitRow / PtStdName, 1e-4f,
                      "⑫ 战斗放大窗卡名名义字号仍是 16/19 比");
            CheckNear(zoom.NameBandMaxForTest / (stdNameBand / PtStdName), PtZoomNameMax, 1e-3f,
                      "⑬ 战斗放大窗卡名上界仍是 **19**（那一棵原版 max=19，⛔ 别被菜单档带到 16）");
            CheckNear(zoom.CardShadowAlphaForTest, AlphaMenu, 1e-3f,
                      "⑭ 战斗放大窗阴影仍 0.8");
            CheckNear(zoom.ArmyFontSizeForTest(true), 1.5f, 1e-4f,
                      "⑮ 战斗放大窗阵营行仍 15 pt（放大窗不降）");
            CheckNear(zoom.DescBandMaxForTest, stdDescBand, 1e-4f,
                      "⑯ 战斗放大窗效果上界仍 = 标准档（放大窗不动效果行）");
        }
        finally
        {
            if (root != null) Object.DestroyImmediate(root);
            Debug.Log(P + "合计：通过 " + _pass + " · 失败 " + _fail);
        }
    }
}
```

---

## 七、合并注意 / 验证状态

1. **`typecheck`**：本代理开工后跑过一次 → **运行时 0 / 编辑器 0**（那是**没打补丁的现状**；我一行源码都没写，所以没有「我改坏」的可能）。
   命令：`TMPDIR=/tmp/wf_wpopup bash d:/4/Unity/工具/typecheck.sh`。
2. **`CardView.cs` 正在被 `B117-WCARD` 改**（我 20 分钟内两次读到不同的 Booster 值：1.500→1.679、1.482→1.235）⇒ **合并前先按引号原文重定位**；若有冲突，以「保持 `Booster` 现值 + 追加 `MenuDetail` 分支」为准。
3. **行数漂移**：除 P1（**实际 +13 行**）外全是同物理行改写。P1 之后 `CardView.cs` 下游行号整体 **+13**（265 条引用全部落在 `:259` 之后） ⇒ 合并后要跑一次
   `python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py --all-A --fix`，**且必须在「没有 `.cs` 写手 + 补丁已合并完」时**做
   （`--fix` 的第一遍输出是修前快照，要复跑一次看真实数字）。
   不想漂移的替代写法：把 `MenuDetail = 3,` 塞进 `Booster = 2,` 那一行（`Booster = 2, MenuDetail = 3,`），doc 压进上面那条 `///` 行 —— **行数零变化**，但可读性差。**本报告推荐 P1 原样（+7 行）**，理由：这个档位的判据相当长，压进一行会被后来人当噪声删掉。
4. **`Shell/CardDetailPopup.cs` 是我这条线唯一该动的源码**，但**它依赖 `CardUse.MenuDetail` 先存在** ⇒ 本代理按红线**没写**。合并顺序：`CardView.cs`（P1..P12）→ `CardDetailPopup.cs`（P13/P14）→ 断言文件 → `typecheck`。
5. **断言条数**：`Check` 调用 **21 条**（接线 1 · 前提 4 · 菜单档 7 · 战术不动 2 · 灭自证 2 · 放大窗回归 5 = 21）；其中被测项按 ①–⑯ 编号（战术那两条占 ⑧⑨、放大窗五条占 ⑫–⑯）。

---

## 八、还差什么（⛔ 别当成「已完全对齐」）

1. **菜单详情窗这一档没有发行版实拍读数**：`NameUnitBandMax` / `DescUnitBandMax` 里的实拍系数（×0.75 / ×0.78 / ×1.1193 / ×0.8333）是**按标准档 19/24pt 的实拍**标定的，本补丁按**同一比例**折到 16pt。若日后用户拿详情窗截图并排比发现偏大/偏小，只需改 P4/P5 那两个比例（不用重查 prefab）。
2. **`ArmyTextUnit` / `ArmyTextTactc` 在原版 prefab 里 `m_IsActive = 0`**（我们照用户实拍仍在印，见 `CardView.BuildTextLayers` 那段长注释）⇒ 阵营行的 16 是「prefab 字段值」，**没有实拍二次确认**。
3. **战斗放大窗的 `DescTextUnit` 没被核**：战斗那棵树本地导出工程里没有（13 arena 是 bundle）⇒ 本补丁**不动**放大窗的三行，只保证「没被菜单档带坏」。
4. **`NameTextUnit No description`（19/19）我们这一侧走的是「名义 16 + autosize 涨到 19」**，与原版「名义 19 + max 19」在**装不下时**的收口点可能差一档（原版从 19 往下降，我们从 19 的 max 往下降 ⇒ 结果同；但若某张卡名在 19 装不下，两边收缩路径一致）。**没有实拍核过**，属于低风险未覆盖面。
5. **没跑 Unity / 没跑真机**（红线）：断言文件本身**从未执行过**（它引用还不存在的枚举）；合并后第一次跑要通过 `CollectionScene.Run` 印出 `B117WPOPUP 合计` 才算数。
