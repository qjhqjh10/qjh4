# B117-WSMALL · 三条小活（`B117-09` / `B117-12` / `B117-15`）

> 执行代理 `B117-WSMALL` · 2026-11-02 · 三条判据均已给定，本次只落地、⛔ 未重新设计。
> ⛔ 未跑 Unity 批处理 · 未动 git · 未动 `d:/2` · 未动账主表。

## 一、三条各自改了什么

### 1️⃣ `B117-09` · `pool_diff4` 对 `UM46`/`UM87`/`UM48` 的期望本身错

**落点 = 产出 `pool_diff4` 的生成器 `Unity/工具/pool_effect_check.py`**（先 `grep -rn "pool_diff4"` 全仓：只有两个**消费方** `gen_card_vfx_verdict.py` / `gen_全卡核验_判定.py`，产出方就是它 —— 判据 `报告/B9_W41S3B_夹具第二批.md:55`「最终表 = `after/pool_diff4.tsv`」）。

| 位置 | 改动 |
|---|---|
| `pool_effect_check.py:1177` | `iter_rows` 的行字典加 `hasAA` / `hasAlt`（真值 `hasActiveAbility` / `hasAlternativeAbility`）|
| `pool_effect_check.py:762-781` | `judge()` 顶部加**「不适用」档**：`src == activeAbility ∧ hasActiveAbility == 0` ⇒ `("不适用", "不适用（hasActiveAbility=0）", "", 判据备注)`（`:774`）；`alternativeAbility` 同族（`:778`，闸 = `hasActiveAbility ∧ hasAlternativeAbility`）。**只认显式 `0`**，`None`/缺列不剔除（⛔ 不把「不知道」当「不适用」）|
| `pool_effect_check.py:22-32` | 文件头「判定（三档）」→「判定（四档）」，补 `不适用` 的语义 |
| `pool_effect_check.py:1377` | 统计行加 `不适用` 计数（⛔ 不许静默丢档）|

**判据（写进注释里）**：`CardScript__ResolveActiveAbilityPlayed.c:26-31` 在 `hasActiveAbility == 0` 时 `LogWarning` 后直接 `return`；我们同口径 = `Core/CardDefActiveAbility.cs:25-31`，执行层还有 `RuleCore.cs:15573` 的 `if (row == null || row.Gate != 1) continue;`。数据侧自洽：全池 `hasActiveAbility == 1` ⟺「有 `gate=1` 的 `activeAbilityLogic` 行」87⟺87（`CardDefActiveAbility.cs:37-39`）⇒ 用 `hasActiveAbility` 判与用 `gate` 判**等价**。

**影响面（现测，不是只改 3 行）**：池内 `hasActiveAbility=0` 的 `activeAbility` 行 = **52 行 / 45 张**（`UM46`/`UM87`/`UM48` 是判据点名的三张；另 42 张同根因）。改后统计 对 1,289→**1,254** · 未验到 413→**396** · 不适用 **52** —— 其中 **35 行原先被判「对」**（签名是从别的效果的噪声里撞上的），**17 行是「未验到」**。

### 2️⃣ `B117-12` · `DA46 The Rock` 的 `ca 360` 卡级派发层「出声不跑」

**落点 = `Unity/MyGame/Assets/RuleEngine/Core/CardAbilityDispatch_B116.cs:158`**（`NoteCardAbilityRowPending(...)` 那一行，行尾注释就地改写，**行数守恒 = 1 增 1 删**）。

补的注释：`ca 360`（`chooseOne`）的效果**已由正文层 `Choose one:` → `chooseone` op 承担**（`EffectText.cs:3958/5948` → 动词表 `chooseone` 1132；判据点名的例子 = `DA46 The Rock`），现在不是缺口；⛔ **将来谁给 360 补卡级执行器就会与正文双发**，补之前必须先让正文 `chooseone` op 让路/去重。

⚠️ **判据点名的是 DA46，但那不是池内唯一一行** —— 现读 `pool_diff4` 里 `chosenAbility 360` 共 **18 行 / 18 张卡**（`ASH71`/`UM109`–`UM118`/`SW48`/`TL20`/`DA46`/`Duelist's Hubris`/`Inscrutable Cunning`/`Hymn of Battle`/`Angel's Wrath`/`Catechism of Death`/`Exemplary Warrior`/`The Fang` …）。所以注释写的是**这一族**，⛔ 不是「只有 DA46 一张」。

### 3️⃣ `B117-15` · S3 生成器加「无效果树 ⇒ 免测」

**落点 = `Unity/工具/gen_全卡核验_判定.py`**（只改这一份；它比 `gen_card_vfx_verdict.py` 新且是 S3-G5 的正式件，`--check` 原本与盘上逐字节相同）。

| 位置 | 改动 |
|---|---|
| `:95` | `engine_cols(...)` 增参 `raw_by_id`（`cards_raw.json` 的 `hasActiveAbility`；`generate()` 本来就在用）|
| `:97-109` | 先把**原版不消费的行**剔出判定：`r[7]=="不适用"` **或**（`r[10]=="activeAbility"` ∧ `hasActiveAbility == 0`）⇒ 两种口径都吃（新夹具出「不适用」，旧夹具仍是「未验到」）；**只认显式 `0`** |
| `:111-113` | 剔完**一条不剩** ⇒ `免测`（全卡 `activeAbility` 都是死数据）|
| `:114-120` | 本来就没有效果树（`triggerAbilityIndices=[]` ∧ 骨架 `ability数=0`）⇒ **`免测`**，⛔ 不再标「未验到」|
| `:133` / `:152` / `:156` / `:160` | 有行被剔时，证据尾部追加「另 `activeAbility` 行 N 条 `hasActiveAbility`≠1 ⇒ 原版不消费（例外①）」（与同文件 ②③ 维的既有措辞一致）|
| `:303` | 调用点传 `raw_by_id` |

**判据**：`判据/B117_逐卡核验_E_Leviathan_Tau_SW_GS.md` §五·3（34 张纯关键词卡 `triggerAbilityIndices=[]`，在 S3 恒「未验到」是**结构性**的）+ §五·4（6 张督军 `activeAbility` 是原版死数据）· `RuleCore.cs:15573` 的 `gate = 0` 判据。

## 二、生成器差异面

### `pool_effect_check.py`（无 `--check/--dry` 开关，用 `--out` 到临时路径 = 等价预览，⛔ 未覆写盘上产物）

```
python -I -X utf8 Unity/工具/pool_effect_check.py \
  --cache d:/4/Unity/_tmp_view/w41s3b/after --out /tmp/wsmall/pool_diff4_after.tsv
```

- **改前**（同一份缓存重跑，与盘上 `pool_diff4.tsv` 逐字节只差 2 行，见下）：`对=1289 · 错=0 · 未验到=413`
- **改后**：`对=1254 · 错=0 · 未验到=396 · 不适用=52`，行数不变（1,701 数据行）
- 逐条差异 = 52 行（每行「期望」→`不适用（hasActiveAbility=0）`、「实得」清空、「判定」→`不适用`、「备注」写判据）
- 三张点名卡的现读数（新表 `:268/:315/:372`）：
  `UM46 Marneus Calgar / UM87 Roboute Guilliman / UM48 Varro Tigurius` · `chosenAbility 10 · 不适用（hasActiveAbility=0）`
- ⚠️ **顺手发现（未改）**：同一份缓存+现数据重跑会多出 **2 行** —— `e8cf1eb55ca41534abe38efc90b302e9 Reconnaissance Mission`（`enchantment`）。根因：`cards_engine.json` 现读 **v47 / count 1129**，而 10-27 那次跑的是 **1128**，`全卡核验总表.tsv` 骨架也还是 1128。⇒ 盘上 `pool_diff4.tsv` 相对现数据是**过期一版**的，**本次有意不覆写**（覆写会把 2 行漂移带进来，且 `调度台日志:12243` 等文档引用的 1,289/411 会一起变）—— 要落地请**调度台**在同步点一次做完。

### `gen_全卡核验_判定.py` `--check`（读**盘上未变的** `pool_diff4.tsv`）

- 改前：`逐字节相同（1128 行数据）rc=0`
- 改后：`差异行数：130（盘上 1129 行 / 生成 1129 行）rc=1`
- 130 行的构成：
  - **判定变化 94 行**：`→免测 85` · `→对 9` · `→未验到 0`
  - **只改证据文字 36 行**（本来就是「对」，只是「N行全对」里的 N 变小 + 追加了剔除说明）
- `→免测 85` 里包含 **E 组那 34 张**（`GSC26/GSC19/GSC36 · SW24/SW31/SW17 · TAU27/11/17/26/4/8/14/23/5/22/16/31/30 · TL38/67/7/2/13/60/66/65/1/61/4/5/12/10/21`），另外 51 张同根因（A/B/C 组的纯关键词卡）。
- `→对 9` = `ASH1 · GSC1 · GSC2 · SAU66 · SW1 · SW3 · TL34 · UM46 · UM48` —— 它们**唯一**的「未验到」行就是那条 `activeAbility` 死数据，剔掉后剩下的行全对。
- 分布：盘上 `对707 / 未验到421` → 生成 `对716 / 未验到327 / 免测85`（合计 1,128 不变）。
- ⚠️ **本次未 `--write`**（任务只要求 `--check`）；盘上那份表被多份文档引用，落盘应由调度台在同步点决定。

## 三、typecheck

```
TMPDIR=/tmp/wf_wsmall bash d:/4/Unity/工具/typecheck.sh
→ 运行时错误数: 0 · 编辑器错误数: 0
```

## 四、没做成 / 判据不足 / 需要调度台接手的

1. **`pool_diff4.tsv` 未覆写**（理由见 §二）。落地命令：
   `python -I -X utf8 d:/4/Unity/工具/pool_effect_check.py --cache d:/4/Unity/_tmp_view/w41s3b/after --out d:/4/Unity/_tmp_view/w41s3b/after/pool_diff4.tsv`
   —— 会同时带进 `Reconnaissance Mission` 那 2 行（数据 v47 vs 骨架 1128 的既有漂移）。
2. **`全卡核验_判定.tsv` 未 `--write`**（同上）。要落就 `--write`，差异面 = §二 那 130 行。
3. **同产物的另一份生成器 `Unity/工具/gen_card_vfx_verdict.py` 未改**（不在本代理白名单）：它读同一份 `pool_diff4.tsv`、写同一个 `全卡核验_判定.tsv`，但**不认识「不适用」档**（`n_bad = n - n_ok` ⇒ 会把 52 行当「未验到」，那 45 张卡被判 `未验到`）。今天不影响（`pool_diff4` 没覆写），但**一旦 §四·1 落地，谁跑它就会把表改回错的**。⇒ 建议调度台给它补同一条剔除规则，或直接把它标成「已被 `gen_全卡核验_判定.py` 取代，⛔ 别跑」。
4. **没查到的**：无（三条判据都足以落地）。
