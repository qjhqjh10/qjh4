# `B9_W64DESC` —— `cardDescriptionTextReferenceId` 判据与施工规格（报告）

> 查判据代理 `W64DESC` · 2026-10-27 · **只读**（未改 `.cs` / `.py` / `.json` / 账 / 正本 / 资产；未跑 Unity、未动 git）
> 产出正本：`资料/完全复刻/判据/卡面描述引用id_判据.md`（§一…§六，含逐卡新旧中英对照）
> 复现脚本：`d:/4/_tmp_view/w64/desc/e{1..6}_*.py`（只读）

## 结论

1. **「10 张印旧版正文」成立，但真实面更大**：15 张池卡的 `Card_Description/<id>` 与 `<id>_new` **中英两档 15/15 实质不同**；5 张（`ASH48`/`SAU58`/`SAU15`/`TAU65`/`SOR25`）只是**被 `OfficialFaceTextWhitelist` 遮住**（它们根本不读官方表）。
2. **白名单不是免疫机制，是根因的创可贴**：那 5 张的 `<id>_new` 官方正文**恰好就是注释里写着「官方缺」的那半句** ⇒ 该字段是根因。6 条白名单里**只有 `AM72` 仍必需**（`AM72` 是 `Vitus Gryf`、`descRef` 为空、无 `_new` 键）。**摘不摘这 5 条 = 调度台裁定**（会改 5 张卡显示措辞 + 打红一整片断言）。
3. **名字兜底：池内 0 次触发，卡名不会印错**。15 张的 `Card_Name/<ref>` 在**全部 13 个语言列都不存在**，`Card_Name/<自己 id>` **全部存在**；全池 1,128 张里「主名字键缺失」只 1 张（`9218576f… Pray Enchantment`，`descRef` 为空）。但**原版那条 `Card_Name/<descRef>` 回退我们没实现**（今天 0 对象）。
4. **白名单能被绕过，且今天就绕过了**：`BattleDriver.cs:28023-28027` 的「其余 10 种语言」分支**不查白名单** ⇒ 德语/法语… 档下 5 张白名单卡印官方**旧**正文（已用德语列实证）。这不是缺陷（原版亦然），但**决定了 `Lang13Current` 那一路也必须换 ref 键**。
5. **旁证**：这 15 张我们自己的 `CardDef.Desc` **15/15 已经是新版语义**（`cards_engine.json` 逐张现读）⇒ 是**取键落后于自己的卡数据**，不是数据错。

## 证据（文件:行号）

| 事 | 出处 |
|---|---|
| 字段声明 `+0x250` | `d:/2/tools/il2cpp_out/dump.cs:23330` |
| 正文取键（**无**「ref 缺键退回自己 id」这一支） | `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/RawCardScript.cs:2317-2350` |
| 名字回退（**主名字键取不到才走**） | 同文件 `:2135-2156`（`Card_Name/<ref>`）· `:2211-2240`（`Card_FullName/<ref>`）· 主键拼法 `:2171-2188` |
| 现状 4 个取键点（恒用卡 id） | `CardPresentation/Battle/BattleDriver.cs:28013` / `:28015` / `:28017` / **`:28025`（10 语言那一路，最易漏）** |
| 白名单 6 条 | `BattleDriver.cs:27975-27976` |
| 白名单只挡中/英 | `BattleDriver.cs:28010-28019` vs `:28023-28027` |
| 单一路径（全仓只这一处读 `Card_Description/`） | `grep -rn 'Card_Description' --include=*.cs` ⇒ 只有 `BattleDriver` 4 处 + `Editor/BattleScene.cs` 断言 |
| 生成器字段表 | `工具/gen_cards_engine.py:3428`（`A1799_FIELDS`）· 两条主循环 `:4900-4903` / `:4264-4265` · `version` `:5572` |
| DTO / 构造 / 透传 | `RuleEngine/Data/CardDatabase.cs:33`（`CardDto`）· `:1554`（透传位）· `RuleEngine/Core/CardDef.cs:990-1005`（末尾可选形参惯例） |
| 池指纹三颗 | `工具/ruleprobe/Program.cs:251`（`PinnedPoolMd5 = c2ee753d…`）· `:350` · `:285` · `工具/ruleprobe/PlayProbe.cs:3852` |
| **会被打红的 3 处断言** | `Editor/BattleScene.cs:25106`（TL24 断「旧版片段」）· `:25336-25339`（⑤ 拿旧键算 `want`）· `:25365-25368`（⑥ 同上） |

## 没查清的部分

1. **白名单 5 条摘不摘**（§3·2 A/B 方案）——需调度台裁；我没定。
2. **§4·5 那 3 处断言的新措辞**只给了方向；`TL24` 不能再当「官方 vs 我们」的判别式样本（两边都写 `When an enemy attacks`），要另选卡或改判别式。
3. **`Card_Targeted` 接线**：`Loc.TargetedText` API 早建好（`Core/Loc.cs:3769-3800`）但**0 调用者**；「提示语放哪个控件」没查（不在本任务范围）。
4. **`effectText` 链的引擎侧对应物**（`CardEffect`）没查（`W55ALTREF` 已记同一缺口，本次复核仍成立）。
5. 账 `账/战斗逻辑驱动.tsv:1232`（`GetLocalizedCardDesc`）落点现指 `Loc.cs:3421（T）`，改完应改指 `FaceTextFull`；**本次不动账**，行号要现读。

## 该怎么改（施工规格）

- **生成器**：`A1799_FIELDS` 表尾追加 `("cardDescriptionTextReferenceId", "cardDescriptionTextReferenceId", "", "<说明>")` ⇒ 两条主循环自动带上；`version` 34→35。
- **DTO/引擎**：`CardDto` 加 `public string cardDescriptionTextReferenceId;`（**不加 `[Obsolete]`**）；`CardDef` **类尾**加属性 + **末尾**可选形参；`CardDatabase.Parse` 透传；加 `dto.version < 35` 出声。
- **消费端 4 处**：统一走 `OfficialDescKey(id, descRef) => string.IsNullOrEmpty(descRef) ? id : descRef`（照 `RawCardScript.cs:2337-2346` 三元，逐字）；**含 `:28025` 那条 10 语言路**。⛔ 不加「ref 缺键退回自己 id」。
- **断言同批改 3 处**（§4·5），并补一条**灭自证**：用**旧键**算的正文必须 ≠ 实得 body（防「把 helper 改回 `c.Id`」静默变绿）。
- **重生成只能在调度台无写手窗口**：`PinnedPoolMd5` **必重钉**；`PinnedParseMd5` / `PinnedBaselineMd5` / `PinnedPlayMd5` **应不变**（变了就停下查）。

## 顺手发现

1. **`TL24` 的旧中文列是朝鲜语**：`아군 $$TL37$$이 죽을 때, 또 다른 하나를 배치합니다` ⇒ 今天中文档 `TL24` 卡面**中朝混排**。改 `_new` 会一并治好（⛔ 别去改官方表那一格）。
2. **`UM72` 旧版官方正文多印一条能力**（`[[armour]] 1.`），而我们的 `Desc` 没有 ⇒ 今天卡面既错数字又多一条本不存在的效果。
3. **`W55ALTREF` §六·4 的「`AM72` 疑似应为 `UM72`」已否掉**：`AM72` = `Vitus Gryf`（在池、`descRef` 空、`Card_Description/AM72` 是 `[[slay]]`）—— 与 `UM72`（`Predator Annihilator`，`descRef = UM72_new`）是两张卡两条理由。
4. **`SOR24` 的 SO `cardName` = `Simulacrum Imperialis`，官方 `Card_Name/SOR24` = `Simulacrum Celestian`**；我们显示官方那个，而 PnP 目录里确有 `Warpforge_30_Simulacrum-Celestian.png` ⇒ **我们是对的**，只为消歧。
5. **账 `战斗逻辑驱动.tsv:1221/1232/1235/1241`** 这四行（`GetCardNameTerm` / `GetLocalizedCardDesc` / `GetLocalizedCardNameFull` / `GetLocalizedTargetted`）的落点都在 `Loc.cs:3421（T）`，属「等价实现但落点不准」，改完本批后建议一起校准。
6. **`A1799_FIELDS` 的值源不是 `cards_truth.json`**（那份没有这一列），是 `load_truth_cards()` 读 `cards_raw.json` 并展平 `full_raw` ⇒ 加列**不用动 `gen_cards_truth.py`**（差点走错）。

## 交接给下一个代理

- 本件已**全部做完**（10 张逐卡中英对照、名字兜底、白名单机制、施工规格、同批清单、还差什么 都落盘）。
- 未做（按简报属调度台/写手）：① 白名单 5 条 A/B 裁定；② 生成器/DTO/消费端/断言的实际改动；③ 重生成与重钉指纹。
- 下一手开工只需读：`资料/完全复刻/判据/卡面描述引用id_判据.md` §四（施工规格）+ §4·5（会红的 3 处断言）+ §4·6（指纹窗口）。
