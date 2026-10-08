# 证据 · `IconSetup.Verify` 现跑（2026-10-18 · 第三会话）

> 日志 = `d:/4/_tmp_view/iconsetup_1020.log`（`-executeMethod IconSetup.Verify`，**不是** `Run` —— `Run` 会重建 sprite asset）。
> 起因：`A980` 说这个宿主**天然不在那 12 条自检里**，2026-09-21 之后**再没人跑过它**。
> 本文件只**如实抄读数**，判据与改法由写手/调度台定。

## 一、读数（逐条，`ICONSETUP …` 行）

| 条 | 结果 | 读数 / 原文 |
|---|---|---|
| ① sprite asset 在 `Resources` 下取得到 | OK | `Fonts/Warpforge Trait TextSprites` |
| ① 图集挂着 | OK | 1024×1024 |
| ① 字形 | OK | **96 条（原版 96）** |
| ① 材质 | OK | `TextMeshPro/Sprite` |
| **② 计划表里的 sprite 名全部查得到** | OK | 🔴 **1606/1606**（⚠️ **2026-10-18 订正：这是那次实跑的读数，已过期**；**现盘 = `2127/2127`** —— 判据 = 现跑 `python -I 工具/gen_icon_plan.py --write`，再数 `数据/游戏数据/card_icon_plan.json` 里 `"sprite"` 项。🔴 **随卡池 / 生成器走，要引用就现跑**） |
| ②b 运行时读计划表 | OK | 卡 **576** 张 / 记号 **1606** 处（`CardIcons`）⚠️ **订正：现盘 = 654 张 / 2127 处**（同一条命令） |
| **②c** 换出来了（DA44） | 🔴 **!!** | 实得：`Give +3 <link=melee><sprite name="Melee"></link>, +3 [Armor] or +3 Health to a friendly troop` |
| ②d 灵魂石：档位数字连图标一起换掉 | OK | `<link=spiritstone><sprite name="SpiritStone_1"></link>: Give +1 melee, +1 ranged and +1 Health to all your troops` |
| **②e** 裸关键词：图标插在前、**词留着** | 🔴 **!!** | 实得：`<link=remnant><link=remnant><sprite name="remnant"></link>残骸。</link>装甲 2。` |
| **②f** 方括号记号：**换掉**、词不留 | 🔴 **!!** | 实得：`Give +3 <link=melee><sprite name="Melee"></link>, +3 [Armor] or +3 Health to a friendly troop` |
| **②f2** 符号 token（`☀`）被吃掉 | 🔴 **!!** | 实得：`<link=faith><sprite name="faith"></link>：部署一个额外的战斗修女` |
| ②f2 圈码 token（`①`）同样被吃掉 | OK | `<link=spiritstone><sprite name="SpiritStone_1"></link>：给一个友方部队` |
| **②f3** `N Quest Point` 整串被吃掉 | 🔴 **!!** | 实得：`Gain <link=questpoints><sprite name="questPoints1"></link>` |
| ②g 换两遍和一遍结果相同（幂等） | OK | —— |
| ②h 裸关键词那条也幂等 | OK | —— |
| ②i 卡池里取得到 `UM84` 的 desc / descZh | OK | —— |
| ★ ②i `UM84 [desc]`：Oath 徽记 **1/1** 枚 | OK | `Friendly <link=oath><link=oath><sprite name="oath"></link>Oath abilities</link> apply an additional time.` |
| ★ ②i `UM84 [descZh]` | OK | `友方部队的<link=oath><link=oath><sprite name="oath"></link>誓言能力</link>额外结算 1 次。` |
| ★ ②i `UM84` 幂等 | OK | —— |
| ②i `UM89` | OK | —— |

⇒ **24 OK / 5 !!**（与上一会话记录一致：`②c/②e/②f/②f2/②f3`），**不是本批造成的**。

## 二、三条**没有查、但读数里直接看得见**的事实（交给写手/诊断，⛔ 别当结论用）

1. 🔴 **「计划表里 sprite 名查得到」的那三个数已经归一（2026-10-18 第三会话晚）**：
   · `工具/gen_icon_doc.py` 里写死的 `270/270` → **改成算出来的**（脚本本就「算出来、别手写」，这一句是漏网的）；
   · `资料/卡面图标_现状与缺口.md:40-41` 的 `1307/1307` / 「1369 处 / 572 张卡」→ 改成**现跑读数**；
   · 本文件那两格保留**那次实跑的读数**并就地标「已过期」。
   ⇒ **当前真值 = `2127/2127` · 654 张 / 2127 处**（判据 = `python -I 工具/gen_icon_plan.py --write` + `python -I 工具/gen_icon_doc.py`，**2026-10-18 实跑**）。
   ⚠️ 三个数当年打架**不是谁抄错**，是**同一个会变的量**被三次写死成常数 ⇒ 以后引用一律写「现跑 + 命令」，⛔ 别再抄常数。
   （`IconSetup.Verify` 那 12 条自检**不含本宿主**，所以它那份读数只能靠手动跑 —— 见本文件开头。）
2. 🔴 **`②e` 与 `★ ②i` 的实得原文里都出现了【两个直接相邻的 `<link=`】**（`<link=remnant><link=remnant>` · `<link=oath><link=oath>`）
   —— **和 `A969`（`Heavy Intercessor` 的 `<link=armour><link=armou…`）是同一个形状**。
   ⇒ **这两笔很可能是同一个根因**（某处把裸关键词**又包了一层 `<link>`**）。
   ⚠️ **本条只是「读数里看得见」，根因未查**（`A969` 正在被只读现核）。
3. `②c` 与 `②f` 的实得**逐字相同**（都是 DA44 那句），且都把 `[Armor]`/`+3 Health` **原样留着**没换
   ⇒ 两条断言**可能量的是同一处**（⛔ 别当成两个独立缺陷）。
