# CardTierConfig · 服务端数据取证（第 3 轮 · 2026-10-26）

> 起因：E7（批次 3）把 `卡牌面_渲染.tsv` 的 **6 条 `CardTierConfig` C 行**挂起，报「数据在原版服务端」并给三个选项。
> 调度台按铁律 2/5 先自己把「本地到底有没有」查穿（⛔ 不拿「搜不到」当结论），结论如下。
> **结论一句话**：**算式本地完整可读、`RoundPrecision` 构造函数里写死 = 5f；缺的只是 4 个标量 + 一张 `tiersByRarity` 表，而那张表用户 2026-09-22 已经裁过用自拟梯度。**

---

## 一、类与取数路径（判据：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/`）

| 项 | 判据 |
|---|---|
| 类 | `Everguild.LiveOps.Config/CardTierConfig.cs`（`IConfigData`） |
| Provider | `CardTierConfigProvider.cs`（全文 5 行）= `ConfigProvider<CardTierConfig>`，**自己不存任何默认值** |
| **配置键** | `ConfigKey => "CardTierConfig"`（`Get` 里那行 `Cpp2ILHelpers.NoteDecompilerIssue("Unmanaged memory load")` 下面就是它）⇒ **远端 LiveOps 配置**，不是 Addressables 资源 |
| 字段 | `List<CardTierByRarity> tiersByRarity` · `float Y_Max` · `float X_Inflection` · `float Offset` · `float Base` · `float RoundPrecision` |
| **构造函数里唯一的写死值** | `RoundPrecision = 5f`（其余 float 全是默认 0，`tiersByRarity = new List<...>()` 空表） |

`CardTierInfo` 每格 = `CardTier` + `Price{LoadableReference<Currency> Currency, int Amount}` + `RequiredCopies` + `PointReward` + `XPReward`。

## 二、`GetXPRequired(level)` 算式（**本地可读，逐句**）

cmath 后的纯算式（`num4` 之后那个 `Method not found @18048B040` = `Mathf.Exp`：后面紧接 `+1f` 再 `1f/`，就是标准 logistic）：

```
num4  = Base * ((level - X_Inflection) / (X_Inflection / 5f))
num5  = Exp(num4) + 1
num6  = 1 / num5
num7  = Y_Max - Offset
num10 = ((1 - num6) * num7) / RoundPrecision
num12 = round-half-away-from-zero(num10)          // 那两段 if(num10<0) 是 Math.Round 的手工内联
return num12 * RoundPrecision + Offset
```

`GetAccumulatedXPForLevel(level)` = `Σ_{i=1..level} GetXPRequired(i)`；`GetLevelForXP(xp)` = 反解（上限保护 `num > 9999` 时 `Debug.LogWarning("XP exceeds defined curve range.")`）。
⚠️ 三处 `NoteDecompilerIssue("Method not found …")` 都是**内联的数学内建**（`Exp` / `Round`），**不是缺失的业务逻辑** ⇒ 别按「缺口」处理。

## 三、本地到底有没有那份数据（四条独立取证，全部为负）

| # | 查了什么 | 结果 |
|---|---|---|
| ① | `d:/2/新解包资源/assets_full/` 全量搜 `tiersByRarity`（E7 已查） | **0 命中** |
| ② | 🆕 **`D:/2/关服前存档/Warpforge/com.unity.addressables/catalog_main.json`**（当前 build 的 Addressables 目录，1.72 MB 明文）搜 `Tier` / `tiersByRarity` / `CardTierConfig` | **全部 0 命中**（只有 2 处小写 `liveops`）⇒ 它**不是**可寻址资源 |
| ③ | 🆕 **关服前存档的 3 份 `__data` 包**（含 1,351 张卡 SO 那份，10.7 MB / 532 KB / 70 KB）搜 `Tier` | **3/3 全 0** |
| ④ | 🆕 **客户端日志** `D:/2/关服前存档/Warpforge/Player.log`（18.6 MB）+ `Player-prev.log`（6.2 MB）搜 `CardTierConfig` | **0 命中**（跑过的客户端没把远端配置打出来） |

另：`资料/卡牌重复与升级_原版规格.md:93` 也写着「本地无副本」。
⇒ **四条独立取证一致：这份数据本地确实拿不到**（不是「没搜到」）。

## 四、🔴 用户已经裁过一半：`tiersByRarity` 那张表

`Unity/MyGame/Assets/CardPresentation/Shell/CardDetailPopup.cs:40`：

> 「**升级表数值是我们编的**（原版在服务端 `CardTierConfig`）—— 照实拍那个数据点」

`:62` 再写：「…`CardTierConfig`；照实拍那个数据点做的梯度，**用户 2026-09-22 已裁决照办**」

⇒ **`tiersByRarity`（升级费用 / 需要副本数 / 点数奖励 / XP 奖励）已经按用户 2026-09-22 的裁决走「自拟梯度」**，这条**不用再问**。

## 五、⏳ 仍需用户裁定的那一小块：**玩家 XP 等级曲线的 4 个标量**

`Y_Max` / `X_Inflection` / `Offset` / `Base`（`RoundPrecision` 已写死 5f）—— 这是 `GetLevelForXP` / `GetAccumulatedXPForLevel` 用的**玩家等级曲线**，
和上面那张升级表**不是同一件事**，**不在 2026-09-22 那条裁决的射程内**（那条只讲「升级表数值」）。

三个选项（给出我的建议）：

| 选项 | 说明 | 建议 |
|---|---|---|
| **(a) 判 D** | 理由写「数据在远端 liveops、本机四条取证全负」+ 时间限定 | ❌ 不推荐：这是一条**玩家可见**的等级曲线，判 D 会把机制删掉 |
| **(b) 自拟曲线 + 注明非原版** | 照 `GetXPRequired` 的真算式实现，4 个标量挑一组让曲线形状合理（如 `Y_Max=100, X_Inflection=20, Base=-0.15, Offset=0`），代码注释写明「数值自拟、算式照原版」 | ✅ **推荐** —— 与 2026-09-22 对升级表的裁决同一口径；算式是原版的，只有 4 个数是自拟 |
| (c) 继续等远端 | 指 `d:/2` 里还没翻过的其它快照 | 已翻穿（见 §三），不建议 |

## 六、给账的落法（用户选了之后照填）

- 选 (b)：6 行里 `GetXPRequired` / `GetAccumulatedXPForLevel` / `GetLevelForXP` / `GetTierInfo` / `GetNextTierInfo` / `GetTotalPlayerXPFromInventory` 按「算式照原版、标量自拟」落 **A**，
  `差异` 列写死「**4 个标量自拟（原版在远端 `CardTierConfig`，见判据文件）· 算式逐句照原版**」；
  `tiersByRarity` 那半写「用户 2026-09-22 裁决自拟梯度」。
- 选 (a)：6 行判 **D**，理由必须带上面四条取证 + 时间限定（「远端数据若出现需重跑」）。
