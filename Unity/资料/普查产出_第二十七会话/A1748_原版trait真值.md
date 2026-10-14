# `A1748` 那 20 张卡 —— 原版 `CardTraits` 真值

> 数据源 = `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`（原版 `RawCardScript` 序列化字段，1195 张）
> 枚举源 = `d:/2/Warpforge_code/Scripts/Assembly-CSharp/DefinedTrait.cs`
> 我们那一侧 = `RuleEngine/Resources/cards_engine.json`（1126 张，只读对照）
> 生成脚本 = `d:/4/Unity/工具/gen_trait_dict.py`（本表由它现算，见 §3）

## 1. 结论

| # | 结论 |
|---|---|
| ① | 20 张里 **18 张在真值里查得到，且每一张的 `CardTraits` 都【确实挂着】那条替代行动 trait**（`pray`=1185 / `duty`=1220 / `agenda`=1235）⇒ `A1748` 的治本方向被真值**直接证实**：我们那 18 张 `keywords` 缺词是**数据侧漏抄**，不是「原版也没挂」。 |
| ② | 🔴 **剩下 2 张（`Blackmane Reiver` / `Fenrisian Drop Pod`）真值里【整张卡都没有】** ——它们要的 `ferocity` 在这份 2025-07 快照的 **650 张 `CardTraits` 里出现 0 次**（`DefinedTrait.ferocity = 1265` 定义了但快照里没人用）⇒ 这两张**本地拿不到原版判据**。 |
| ③ | 🔴 **本轮的 id 口径订正（重要）**：`W1_A1745落地.md` 里那 20 个 id 是**换真值之前的旧命名空间**（该报告写于第二十五会话，换 id 在第二十六会话）⇒ **别照它的 id 去卡池里找**。本报告**按卡名 join**，并逐张核对旧 id→新 id（见 §2 的「现卡池 id」列）。 |
| ④ | 四条替代行动的枚举真值：`pray`=**1185** · `duty`=**1220** · `agenda`=**1235** · `ferocity`=**1265**（`DefinedTrait.cs`）。真值里这三条的 `_traitValue` **一律 0**。 |
| ⑤ | **全量口径（真值当判据，不止这 20 张）**：真值里挂替代行动 trait 的卡共 **57 张**，其中 **37 张**我们 `keywords` **已经**有那个词、**18 张缺**、**2 张卡池里根本没有这张卡**。⇒ A1748 点名的那 18 张 **= 全量口径下的这 18 张**（**无一漏网、无一多算**）。 |
| ⑥ | ⚠️ 真值里这 18 张**多数还同时挂着别的 trait**（`flying` / `vanguard` / `camouflage` / `talent` …）—— 见 §2「原版 CardTraits」列。那些**我们 `keywords` 里基本都已经有**（§2 只报替代行动那一条的差异）。 |

## 2. 逐卡表（`A1748` 20 张）

> 「现卡池 id」= 按卡名在我们 1126 张里现查到的 id（**与 W1 报告的旧 id 不同是正常的**，见结论③）。

| W1的旧id | 卡名 | 现卡池 id | 我们的 keywords | 原版 CardTraits(name/_trait/_traitValue) | 差异 |
|---|---|---|---|---|---|
| `AM32` | Chimera | `AM28` | `[]` 空 | duty(`_trait`=1220, val=0) | 🔴 我们 `keywords` **缺** `duty` |
| `AM39` | Valkyrie | `AM34` | `Flying` | flying(`_trait`=390, val=0) · duty(`_trait`=1220, val=0) | 🔴 我们 `keywords` **缺** `duty` |
| `DA18` | Ravenwing Speeder | `DA14` | `Flying` | flying(`_trait`=390, val=0) · agenda(`_trait`=1235, val=0) | 🔴 我们 `keywords` **缺** `agenda` |
| `DA30` | Ravenwing Talonmaster | `DA27` | `Flying` | flying(`_trait`=390, val=0) · agenda(`_trait`=1235, val=0) | 🔴 我们 `keywords` **缺** `agenda` |
| `SOR1` | Erika Luminas | `SOR1` | `[]` 空 | talent(`_trait`=845, val=0) · pray(`_trait`=1185, val=0) | 🔴 我们 `keywords` **缺** `pray` |
| `SOR5` | Morvenn Vahl | `SOR3` | `[]` 空 | talent(`_trait`=845, val=0) · pray(`_trait`=1185, val=0) | 🔴 我们 `keywords` **缺** `pray` |
| `SOR9` | Battle Sister | `SOR5` | `[]` 空 | pray(`_trait`=1185, val=0) | 🔴 我们 `keywords` **缺** `pray` |
| `SOR10` | Battle Sister Gunner | `SOR6` | `[]` 空 | pray(`_trait`=1185, val=0) | 🔴 我们 `keywords` **缺** `pray` |
| `SOR15` | Celestian | `SOR15` | `Vanguard` | vanguard(`_trait`=980, val=0) · pray(`_trait`=1185, val=0) | 🔴 我们 `keywords` **缺** `pray` |
| `SOR21` | Hospitaller | `SOR20` | `[]` 空 | pray(`_trait`=1185, val=0) | 🔴 我们 `keywords` **缺** `pray` |
| `SOR26` | Aestred Thurga | `SOR26` | `[]` 空 | pray(`_trait`=1185, val=0) | 🔴 我们 `keywords` **缺** `pray` |
| `SOR29` | Imagifier | `SOR25` | `[]` 空 | pray(`_trait`=1185, val=0) | 🔴 我们 `keywords` **缺** `pray` |
| `SOR31` | Celestian Sacresant | `SOR30` | `Vanguard.`, `Armour 1` | vanguard(`_trait`=980, val=0) · pray(`_trait`=1185, val=0) | 🔴 我们 `keywords` **缺** `pray` |
| `SOR33` | Ephrael Stern | `SOR29` | `Camouflage` | pray(`_trait`=1185, val=0) · camouflage(`_trait`=985, val=0) | 🔴 我们 `keywords` **缺** `pray` |
| `SOR36` | Celestian Superior | `SOR34` | `[]` 空 | pray(`_trait`=1185, val=0) | 🔴 我们 `keywords` **缺** `pray` |
| `SOR39` | Immolator | `SOR35` | `Armour 1`, `Blast 2` | blast(`_trait`=350, val=2) · pray(`_trait`=1185, val=0) | 🔴 我们 `keywords` **缺** `pray` |
| `SOR71` | Zephyrim Superior | `SOR69` | `Flying`, `Flank` | pray(`_trait`=1185, val=0) · flying(`_trait`=390, val=0) · flank(`_trait`=460, val=0) | 🔴 我们 `keywords` **缺** `pray` |
| `SOR_Celestian_Sacresant_Aveline` | Celestian Sacresant Aveline | `SOR65` | `[]` 空 | talent(`_trait`=845, val=0) · pray(`_trait`=1185, val=0) | 🔴 我们 `keywords` **缺** `pray` |
| `SW13` | Blackmane Reiver | `SW13` | `Camouflage` | —— | ⚠️ 真值里**查不到这张卡**（快照 2025-07 之后才新增） |
| `SW31` | Fenrisian Drop Pod | `SW31` | `Flying`, `Armour 1` | —— | ⚠️ 真值里**查不到这张卡**（快照 2025-07 之后才新增） |

## 3. 复算命令（一行）

```bash
python -I -X utf8 d:/4/Unity/工具/gen_trait_dict.py    # 重出本报告 + 词典 TSV，两处表都由它现算
```

## 4. 自检（脚本顺带打的）

| 项 | 读数 |
|---|---|
| 真值总张数 | 1195 |
| `CardTraits` 非空的张数 | **650**（与 `卡牌id_换真值_落地记录.md` §五·4 记的 650 一致 ✅） |
| 不同 `(name, _trait)` 组合 | **46** |
| `_trait` 数字不在 `DefinedTrait` 里的 | **0** ✅ |
| `name` 不在 `DefinedTrait` 枚举名里的 | **0** ✅ |
| 同名不同号（`name`→多个 `_trait`） | **0** ✅ 一对一 |
| 同号不同名（`_trait`→多个 `name`） | **0** ✅ 一对一 |
| `_traitValue` 取过非 0 的 trait | `armour`(_trait=1020)=[1] · `blast`(_trait=350)=[1, 2, 3, 4, 5] · `companion`(_trait=1180)=[1, 2] · `ecstasy`(_trait=1250)=[1, 2, 3, 5, 15] · `regeneration`(_trait=1030)=[1, 2, 3] · `shuriken`(_trait=1120)=[1, 2, 3, 4, 8] · `tide`(_trait=910)=[1, 2, 3, 4] |
| 20 张里真值查不到 | **2** 张：`Blackmane Reiver`(SW13), `Fenrisian Drop Pod`(SW31) |

## 5. 全量口径（真值判据 · 不止 A1748 那 20 张）

真值里挂四条替代行动 trait 之一的卡共 **57 张**；按「id → 卡名」两级 join 到我们 1126 张后：
- ✅ 我们 `keywords` **已经有**那个词的：**37 张**
- 🔴 **缺词的：18 张**（= A1748 点名的那 18 张，逐一吻合）
- ⚠️ **我们卡池里没有这张卡**：**2 张** —— `AM3`(Commissar Denkler, 要 `duty`)；`AM66`(Tempestor Prime, 要 `duty`)
- ℹ️ 其中 **4 张**是「真值 id ≠ 我们 id、按卡名才 join 上」的（id 换真值时没覆盖到的形态）：`DA3`→`DA_Asmodai`(Asmodai, Master Interrogator)；`66486b3f61682ab42ba2ce0fa921002f`→`AM68`(Tempestus Scion)；`DA1`→`DA_Azrael`(Azrael, Grand Master)；`AM59`→`AM_Astropath`(Astropath)

缺词 18 张逐条：`AM28` Chimera(−duty)；`AM34` Valkyrie(−duty)；`DA14` Ravenwing Speeder(−agenda)；`DA27` Ravenwing Talonmaster(−agenda)；`SOR1` Erika Luminas(−pray)；`SOR15` Celestian(−pray)；`SOR20` Hospitaller(−pray)；`SOR25` Imagifier(−pray)；`SOR26` Aestred-Thurga(−pray)；`SOR29` Ephrael Stern(−pray)；`SOR3` Morvenn Vahl(−pray)；`SOR30` Celestian Sacresant(−pray)；`SOR34` Celestian Superior(−pray)；`SOR35` Immolator(−pray)；`SOR5` Battle Sister(−pray)；`SOR6` Battle Sister Gunner(−pray)；`SOR65` Celestian Sacresant Aveline(−pray)；`SOR69` Zephyrim Superior(−pray)

## 6. 还没查清的 / 还差什么

- 🔴 **`ferocity`（1265）0 判据** —— 这份 2025-07 快照里 **一次都没出现**（含 SW13/SW31 那两张）。原版给 `Blackmane Reiver` / `Fenrisian Drop Pod` 挂没挂 `ferocity`，**本地核不了**（要更晚的 `allcards` 快照）。
- ⚠️ **快照年代**：Unity **2022.3.38f1 / 缓存 2025-07-30**，比当前客户端老；**字段结构与 id 方案是原版真值，个别卡的值未必等于最终版**。真值 **1195 张** vs 我们 **1126 张**，**266 张卡没有真值**（`卡牌id_换真值_落地记录.md` §二已记）。
- ⚠️ **「原版靠什么判这条行动发得出来」本轮没复核**：W1 报告 §2 查实原版走的是`CardAbility.trigger` 枚举（`CardScript__TriggeredPray.c`），**trait 是另一条线**。**「ability 触发器」与「`CardTraits`」谁是闸、两者是否恒同步，本轮没查** ——本报告只给「原版挂了什么 trait」，**不等于**「原版能不能发」。
- ⚠️ **`AM3 Commissar Denkler` / `AM66 Tempestor Prime`**：真值里挂了 `duty`、`showInCollectionOptions=5`、`emptyCard=0`，但**我们 1126 张里按 id 和卡名都查不到**。**为什么缺没查**（是没做、还是被筛掉了）。
- 🔴 **`ShowTraits` 与 `CardTraits` 不是一回事**：真值里与 `CardTraits` 平行还有一列 `ShowTraits`，而 **`A1748` 这 18 张的 `ShowTraits` 全部为【空】**（`CardTraits` 却都非空）⇒ **「空 `ShowTraits`」≠「没有 trait」**，判「原版挂了什么」必须读 **`CardTraits`**。两列各自的用途**没查**。

## 7. 顺手发现的东西（只报，未改任何东西）

- 🔴 **`W1_A1745落地.md` 的 id 是旧命名空间**（结论③）—— 该报告 §1/§3/§4 里所有 id 都按旧口径读，**下个会话照它去卡池里找会找错卡**。建议由主对话在正本里就地订正。
- 🔴 **`armour` trait ≠ 护甲值**：`CardTraits` 里 `name=="armour"`(1020) **全快照只有 2 条**（`BL27` / `TL70`），而 `armour` **数值字段 >0 的卡有 62 张** ⇒ 护甲走 `armour` 字段，不走 trait。
- ⚠️ **`keyword`（int）字段不是 trait 号**：取值只有 `0/5/10/15/25/30/35/40`；**548 张非 0 里 526 张的数字不在自己的 `CardTraits` 里** ⇒ 它**不是** trait 指针，**语义没查**。
- 📋 **`CardTraits` 这一层只用掉了很小一块**：650 张里 **46 种**不同的 `(name,_trait)` 组合（`DefinedTrait` 定义了 ~140 个）⇒ 其余 ~94 个枚举值在这份快照里**没人用**。
- 📋 **本轮只对账了「四条替代行动」这一个族**；`CardTraits` 与全池 `keywords` 的**逐卡全量对账（其余 42 种 trait）没做** —— 那才可能捞出另一批同样缺词的卡。

