#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""gen_card_dossier.py —— **卡牌总索引**（用户 2026-11-02 点名；用途 = 以后做 **mod 卡牌** 时查
「某张卡对应什么代码 / 数值 / 特效 / 动画 / 语音」）。

跑法（🔴 写盘脚本必须有显式动作档，见 CLAUDE.md §三 2026-10-26 那条）：
    python -I -X utf8 d:/4/Unity/工具/gen_card_dossier.py --card ASH83          # 单卡全貌（人读，打印）
    python -I -X utf8 d:/4/Unity/工具/gen_card_dossier.py --all --dry           # 全池读数，**零写盘**
    python -I -X utf8 d:/4/Unity/工具/gen_card_dossier.py --all --check         # 与盘上 TSV 逐字节比（一致 rc=0 / 不一致 rc=1）
    python -I -X utf8 d:/4/Unity/工具/gen_card_dossier.py --all --write         # 写 判据/卡牌总索引.tsv（**唯一会写盘的档**）
    python -I -X utf8 d:/4/Unity/工具/gen_card_dossier.py --json --out <路径>    # 机器可读导出
    python -I -X utf8 d:/4/Unity/工具/gen_card_dossier.py --json                # 打印到 stdout（--out 缺省）
    python -I -X utf8 d:/4/Unity/工具/gen_card_dossier.py --kwindex --dry          # 关键词/效果词索引读数（零写盘）
    python -I -X utf8 d:/4/Unity/工具/gen_card_dossier.py --kwindex --check        # 与盘上 判据/关键词与效果词_代码落点索引.tsv 逐字节比
    python -I -X utf8 d:/4/Unity/工具/gen_card_dossier.py --kwindex --write        # 写 判据/关键词与效果词_代码落点索引.tsv
    ⛔ 一个档都不给（或不认识）= **只打印用法并退出 2**，不计算、不写盘。

🆕 **2026-11-03（`B121-WF`）加的两样**（用户 2026-11-03 原话：「或者卡牌总索引工具有说对应卡牌的
代码在什么地方……以后方便查询」）：
  ① `--kwindex` 档：产 **关键词/卡面动词/trigger → 实现落点** 索引（`判据/关键词与效果词_代码落点索引.tsv`）。
     数据源**只读源码**：`CardDef.cs` 的 `KeywordTable`（64 常量）· `EffectResolver.cs:EffectDispatch`（动词表）·
     `ActionPump.cs:TriggerNameOfKeyword`（关键词 ↔ 原版 trigger id）· `EffectText.cs`（动词的解析产出点）。
     🔴 落点行**每次现读**（符号锚 + 声明行），⛔ 不写死行号；查不到就写「查不到」，⛔ 不编。
  ② 卡牌总索引新增两列 `code_verbs` / `code_abil`（见 `COLS` 注释）：让每张卡能查到**自己的**效果处理入口，
     不再只有那个「全池同值」的通用管线（`code_entry` 仍是通用管线，但单元格首句已标明「全池同值」）。

产出的 TSV 以 `#` 开头的行是**元信息**（生成方式 / 数据源版本 / 复跑命令），消费方跳过 `#` 行后
第一行是列名行。列名**稳定**（mod 脚本按列名取），改列名要在本文件头写清并同步报告。

⚠️ 本脚本**只读**：不写任何数据源、不重算别人已经算过的判定。
   特效/动画/语音的「在工程 / 在消费表」**直接复用** `判据/全池事件覆盖_静态.tsv`（`B118-W13` 的产物），
   ⛔ 这里**不另算一套规则**。

数据源（逐列出处见 `--all` 产出的文件头与报告 §2）：
    MyGame/Assets/RuleEngine/Resources/cards_engine.json            身份 + trigger/trait 面 + animTimes（版本 / 张数**现读 `count`**，⛔ 别写死）
    MyGame/Assets/RuleEngine/Resources/cards_truth.json             效果树 abilities/activeAbility/alternativeAbility（1,351 张卡 SO）
    MyGame/Assets/CardPresentation/Resources/card_vfx_by_card.json  逐卡事件 → prefab/CardAnim（运行时那份）
    MyGame/Assets/CardPresentation/Resources/cardanim_clips.json    逐 CardAnim 的动画参数
    MyGame/Assets/CardPresentation/Resources/voice_lines.json       语音 cue 归属
    资料/完全复刻/判据/全池事件覆盖_静态.tsv                        「在工程 / 在消费表 / 参数判定」（只读复用）
    资料/完全复刻/判据/全卡核验_判定.tsv                            四维核验判定（引擎/特效/动画/语音）
    资料/完全复刻/判据/**                                          该卡有专项判据文档的，列文件名
    MyGame/Assets/RuleEngine/{Core,Data}/*.cs                       代码落点（入口现读行号）+ 逐卡专项特判

🔴 两种「口径」写死在这里，⛔ 别在别处再开一套：
  ① **统计口径**：`cards_raw.json` 每卡有两份同构技能树（解析视图 + `full_raw`）⇒ 递归扫会**翻倍**，
     本工具**不读**它。
  ② **张数口径 = `cards_engine.json` 的 `count`（现读，⛔ 别写死）**；`全卡核验总表.tsv` 的骨架张数
     是**历史快照**，可能比它少（例：差 `Reconnaissance Mission`，
     `e8cf1eb55ca41534abe38efc90b302e9`）。
     本工具**以 cards_engine 的 `count` 为准**，⛔ 不扩表、不给骨架补行。
"""

import io
import json
import os
import re
import sys
from collections import Counter, defaultdict

TOOL_DIR = os.path.dirname(os.path.abspath(__file__))
BASE = os.path.dirname(TOOL_DIR)              # D:/4/Unity
ROOT = os.path.dirname(BASE)                  # D:/4

P_ENGINE = os.path.join(BASE, "MyGame", "Assets", "RuleEngine", "Resources", "cards_engine.json")
P_TRUTH = os.path.join(BASE, "MyGame", "Assets", "RuleEngine", "Resources", "cards_truth.json")
P_VFX = os.path.join(BASE, "MyGame", "Assets", "CardPresentation", "Resources", "card_vfx_by_card.json")
P_CLIPS = os.path.join(BASE, "MyGame", "Assets", "CardPresentation", "Resources", "cardanim_clips.json")
P_VOICE = os.path.join(BASE, "MyGame", "Assets", "CardPresentation", "Resources", "voice_lines.json")
VO_DIR = os.path.join(BASE, "MyGame", "Assets", "CardPresentation", "Resources", "Art", "audio", "vo")

CRIT_DIR = os.path.join(BASE, "资料", "完全复刻", "判据")
P_EVT = os.path.join(CRIT_DIR, "全池事件覆盖_静态.tsv")
P_VERIFY = os.path.join(CRIT_DIR, "全卡核验_判定.tsv")
P_OUT = os.path.join(CRIT_DIR, "卡牌总索引.tsv")

SRC_DIRS = [os.path.join(BASE, "MyGame", "Assets", "RuleEngine", "Core"),
            os.path.join(BASE, "MyGame", "Assets", "RuleEngine", "Data")]

# 原版枚举（⛔ 不抄表，每次都从签名桩现读；读不到就退回裸数字并在输出里写「查不到」）
ENUM_SRC = os.path.join("D:/2", "Warpforge_code", "Scripts", "Assembly-CSharp")

# 代码落点：符号锚（行号**现读**，⛔ 不写死 —— 源码会动，写死就过期）
CODE_ENTRIES = [
    ("CardDef",                       "MyGame/Assets/RuleEngine/Core/CardDef.cs",
     r"public\s+partial\s+class\s+CardDef\b"),
    ("CardDef.AddTriggerOp",          "MyGame/Assets/RuleEngine/Core/CardDef.cs",
     r"\bvoid\s+AddTriggerOp\s*\("),
    ("EffectText",                    "MyGame/Assets/RuleEngine/Core/EffectText.cs",
     r"public\s+static\s+class\s+EffectText\b"),
    ("EffectText.Parse",              "MyGame/Assets/RuleEngine/Core/EffectText.cs",
     r"public\s+static\s+List<EffectOp>\s+Parse\s*\("),
    ("EffectResolver.EffectDispatch", "MyGame/Assets/RuleEngine/Core/EffectResolver.cs",
     r"Dictionary<string,\s*EffectHandler>\s+EffectDispatch\b"),
    ("VfxMap.ByCard",                 "MyGame/Assets/CardPresentation/Core/VfxMap.cs",
     r"Dictionary<string,\s*Dictionary<string,\s*List<CardPick>>>\s+ByCard\b"),
    ("CardAnimClips",                 "MyGame/Assets/CardPresentation/Battle/CardAnimClips.cs",
     r"public\s+static\s+class\s+CardAnimClips\b"),
    ("UnitTweenTable",                "MyGame/Assets/CardPresentation/Core/UnitTweenTable.cs",
     r"public\s+static\s+class\s+UnitTweenTable\b"),
    ("VoiceLines",                    "MyGame/Assets/CardPresentation/Core/VoiceLines.cs",
     r"public\s+static\s+class\s+VoiceLines\b"),
]

# 「批量数据表」阈值：一个文件里出现 ≥N 个**不同的池内卡 id** ⇒ 那是数据表（id 别名表之类），
# 不是「逐卡特判」⇒ 整表排掉并**明说排掉了谁**（铁律 2：别把「排掉了」写成「没有」）。
BULK_TABLE_MIN = 100

USAGE = """用法：python -I -X utf8 d:/4/Unity/工具/gen_card_dossier.py <档>
  --card <卡id|英文卡名>   单卡全貌（人读；分区 + 每段带 文件:行号 / 数据源）
  --all --dry              全池读数（多少张、每维覆盖多少、三态分布）· 零写盘
  --all --check            与盘上 `判据/卡牌总索引.tsv` 逐字节比（一致 rc=0 / 不一致 rc=1）· 零写盘
  --all --write            写 `判据/卡牌总索引.tsv`（**唯一会写盘的档**）
  --json [--out <路径>]    机器可读导出（缺 --out = 打印到 stdout）
  --kwindex --dry          关键词/效果词 → 实现落点 读数（零写盘）
  --kwindex --check        与盘上 `判据/关键词与效果词_代码落点索引.tsv` 逐字节比
  --kwindex --write        写 `判据/关键词与效果词_代码落点索引.tsv`
  ⛔ 裸跑 = 打印本用法并退出 2，不计算、不写盘。"""


# ======================================================================================
# 关键词 / 卡面动词 / trigger → 实现落点索引（`--kwindex` 档；2026-11-03 `B121-WF`）
#
# 为什么单开这一档：`判据/卡牌总索引.tsv` 的 `code_entry` 是**全池同值**的通用管线
# （1,129 张卡只有 1 个取值），而用户问的是「**某张卡 / 某个关键词的实现在哪个方法**」
# （原话见 `报告/B121_CMT_战斗代码注释与卡牌索引.md` §〇）。
#
# 数据源（**全部现读源码**，⛔ 不抄任何文档里的行号）：
#   · `RuleEngine/Core/CardDef.cs`          `KeywordTable` 的 64 个常量 + `Prefixes`（别名表）
#   · `RuleEngine/Core/EffectResolver.cs`   `EffectDispatch`（**卡面动词 → 结算函数**，只此一处）
#   · `RuleEngine/Core/EffectText.cs`       `Verb = "<x>"` 的**解析产出点**（一句话怎么变成 op）
#   · `RuleEngine/Core/ActionPump.cs`       `TriggerNameOfKeyword`（关键词 ↔ 原版 trigger 名/数值）
#
# 🔴 三条纪律（照 CLAUDE.md）：
#   ① **行号现读**（符号锚 = 声明行的正则），源码一动就对不上 ⇒ 本表重跑即刷新，⛔ 不写死行号；
#   ② **落点行必须像声明**（注释/空行/语句中间都不算）—— 生成时逐行自检，不合格**出声**；
#   ③ **查不到就写「查不到」**，⛔ 不编。`KW_LANDING` 里没有条目 / 正则匹配 0 处 ⇒ 该行标「查不到」。
# ======================================================================================

P_KWINDEX = os.path.join(CRIT_DIR, "关键词与效果词_代码落点索引.tsv")

KW_COLS = ["关键词/动词", "类别", "实现方法", "文件:行", "判据出处", "备注"]

# 关键词 → 实现落点（**人工裁定 + 符号锚**；2026-11-03 `B121-WF`）。
#   (常量名, 文件相对 D:/4/Unity, 声明行正则, 方法标签, 备注)
# ⚠️ 多个关键词共用一个方法（`IsValidTarget` / `CleanupDeaths` / `NoCounter`）是**正常的**：
#   那些是「关键词总闸」，备注里点名**是哪一条支路**。⛔ 别为了「一行一个不同方法」去编造落点。
KW_LANDING = [
    ("Vanguard", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"public static int IsValidTarget\(", "RuleCore.IsValidTarget",
     "攻击合法性总闸：`vanguard` = 前线单位，是「打不打得到」的判据之一（`targetIsDefender` 那一段）"),
    ("GiantKiller", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"public static int IsValidTarget\(", "RuleCore.IsValidTarget",
     "巨人杀手：带这个词的攻击者只能打督军/前线单位（拒码 `ErrAttackGiantKiller`）；池内 0 张"),
    ("Stealth", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"public static int IsValidTarget\(", "RuleCore.IsValidTarget",
     "隐身三支：①不可被攻击（本方法）②自己发起攻击时揭示（`DeclareAttackCore`）③回合开始到期（`BeginTurnTail`）"),
    ("SummonRift", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"public static int CanUseAbilityTarget\(", "RuleCore.CanUseAbilityTarget",
     "召唤裂隙：不可被交互（主动能力 + 攻击两条裸闸，两侧都不分敌我）；池内 0 张"),
    ("Flying", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"public static int IsValidTarget\(", "RuleCore.IsValidTarget",
     "飞行：近战打不到（**反向不成立**，飞行单位可以近战打地面）"),
    ("Armour", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"public static int DamageAfterReduction\(UnitState u, int dmg, out int vulnPart\)", "RuleCore.DamageAfterReduction",
     "护甲 = 每一条伤害 `Max(1, 伤害 − Armour)`；读的是 `UnitState.Armor`（构造里 `KwValue(Armour)` 那一格）。⚠️ 残骸不吃护甲"),
    ("Shield", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"static int ApplyDamageCore\(", "RuleCore.ApplyDamageCore",
     "护盾：挡下一次伤害后**摘关键词**（不是写布尔字段）；派生只读属性 = `UnitState.HasShield`"),
    ("Dodge", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"public static int DamageAfterReduction\(UnitState u, int dmg, out int vulnPart\)", "RuleCore.DamageAfterReduction",
     "闪避：与 `shield` **同一条分支**（挡下/消耗判据同形）；预览侧见 `WouldKillByEntries`"),
    ("Stun", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"public static void RecomputeActAndAttack\(", "RuleCore.RecomputeActAndAttack",
     "眩晕 = 不占行动配额（本方法读 `Stun`）；施加 `EffectResolver.DoStun` · 到期 `EndTurnCore` · 闸 `RuleCore.CheckStun`"),
    ("Blind", "MyGame/Assets/RuleEngine/Core/EffectResolver.cs",
     r"static bool DoBlind\(", "EffectResolver.DoBlind",
     "失明：施加点；读点 = `RuleCore.FieldAttack`（失明期间远程攻击力视为 0），到期在 `RuleCore.EndTurnCore`"),
    ("Survivor", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"static void CleanupDeaths\(", "RuleCore.CleanupDeaths",
     "幸存者 = 第二条命：致死时改为「消耗 survivor + 生命设成 Min(X, 最大生命)」并留场；消耗口 `UnitState.UseSurvivor`"),
    ("SurvivorSpent", "MyGame/Assets/RuleEngine/Core/UnitState.cs",
     r"public int UseSurvivor\(", "UnitState.UseSurvivor",
     "运行时授予的标记词（**不是卡面词**）：消耗幸存者时顺手挂上，供后续判据区分「已用过」"),
    ("Bastion", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"static int ApplyDamageCore\(", "RuleCore.ApplyDamageCore",
     "堡垒 = 额外血池：整份伤害先进堡垒、打穿了才把溢出打到生命（判据 `CardScript__RemoveBastionDamage.c`）"),
    ("Sacrifice", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"static void CleanupDeaths\(", "RuleCore.CleanupDeaths",
     "献祭：**被幸存者救回来**的那一瞬间多挂一条触发（与「真死 / 变残骸」两支互斥）；发射口在 `FireTriggerAlways(..., Sacrifice, ...)`"),
    ("DropPod", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"static bool OpenDropPod\(", "RuleCore.OpenDropPod",
     "空投舱：致死时先「开舱」（按存量吃伤害）+ 广播 Landing；预览侧 `WouldKillByEntries` 也要算它"),
    ("Poisoned", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"static int EndTurnCore\(", "RuleCore.EndTurnCore",
     "中毒：回合末按层扣血；另在 `ResolveDamageDealtHook` 里做「受伤后叠层」那一半。抗性 `Resistant` 会挡"),
    ("Resistant", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"static int EndTurnCore\(", "RuleCore.EndTurnCore",
     "抗性：中毒那一支的免疫闸（`UnitState.AddKeyword` 里还有「中毒/抗性互斥」的一跳）"),
    ("CantAttack", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"static bool AttackBannedByTraits\(", "RuleCore.AttackBannedByTraits",
     "不能攻击：与 `Noncombatant` / `Unstoppable` 同一个闸（这几条判据只此一处）"),
    ("LongRange", "MyGame/Assets/RuleEngine/Core/CounterAttack.cs",
     r"public static bool NoCounter\(", "CounterAttack.NoCounter",
     "远程：不挨反击（四条豁免的唯一判据 = `CounterAttack.NoCounter`）"),
    ("SneakAttack", "MyGame/Assets/RuleEngine/Core/CounterAttack.cs",
     r"public static bool NoCounter\(", "CounterAttack.NoCounter",
     "潜行攻击：与远程同族的反击豁免（卡面写法 `Sneak Attack`，别名表里两种写法都认）"),
    ("Terror", "MyGame/Assets/RuleEngine/Core/CounterAttack.cs",
     r"public static bool NoCounter\(", "CounterAttack.NoCounter",
     "恐惧：反击豁免第三道（隐藏 trait，官方 `Card_Trait/terror` 0 条⇒卡面不印；池内 0 张）"),
    ("Sniper", "MyGame/Assets/RuleEngine/Core/CounterAttack.cs",
     r"public static bool NoCounter\(", "CounterAttack.NoCounter",
     "狙击：目标会死时不挨反击；另 `RuleCore.DeclareAttackCore` 里有 `sniperKill` 那一支"),
    ("DarkPact", "MyGame/Assets/RuleEngine/Core/EffectResolver.cs",
     r"static void GrantDarkPactCore\(", "EffectResolver.GrantDarkPactCore",
     "黑暗契约 = **我们的层数代理**（`CountFor` 的 `darkpact` 分支读 `KwValue`）；与原版 `markofchaos` 每份同时 +1"),
    ("MarkOfChaos", "MyGame/Assets/RuleEngine/Core/EffectResolver.cs",
     r"static void ApplyMarkBody\(", "EffectResolver.ApplyMarkBody",
     "混沌印记 = 原版真表示（trait 330）：一份印记一层；只是计数/标记，⛔ 不进 `Implemented`（原版是隐藏 trait）。"
     "⚠️ 官方 `Card_Trait/markOfChaos` 的**显示名**是 `Dark Pact`（与本表 `DarkPact` 那一行不是同一个词）"),
    ("Rally", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"static int PlayCardCore\(", "RuleCore.PlayCardCore",
     "集结：打出这张卡时触发（`FireTriggerOnBoard(..., Rally, ...)`）；免费部署那条路在 `DeployFreeCore` 也有同一跳"),
    ("Strike", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"static int DeclareAttackExecute\(", "RuleCore.DeclareAttackExecute",
     "攻击（Strike）：近战攻击后触发；正文收口 `StrikeChosenOrNull`"),
    ("Slay", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"static bool TryCreditKillCore\(", "RuleCore.TryCreditKillCore",
     "消灭（Slay）：谁能拿击杀的那一处（`FireTriggerAt(..., Slay, ...)`）"),
    ("Backlash", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"static void CleanupDeaths\(", "RuleCore.CleanupDeaths",
     "反噬：本卡死亡时触发（`deathTriggersAllowed` 闸内）；skip 表里它同时算 `ThisUnitDeath`"),
    ("Penitence", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"static int HurtCore\(", "RuleCore.HurtCore",
     "忏悔：受伤后触发（同一处还有 `DoubleRage` 加倍那一支）"),
    ("DoubleRage", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"static int HurtCore\(", "RuleCore.HurtCore",
     "双倍狂怒：受伤结算里那一支；池内 0 张"),
    ("Mob", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"static void MobExtraTriggerDeferred\(", "RuleCore.MobExtraTriggerDeferred",
     "暴群：近战攻击后**延迟**多触发一次（`FireTriggerDeferred` 按关键词分流）"),
    ("Regiment", "MyGame/Assets/RuleEngine/Core/EffectResolver.cs",
    r"static void FireMobOrRegimentOnSameSide\(", "EffectResolver.FireMobOrRegimentOnSameSide",
     "团（Regiment）：攻击后对同侧同关键词的其它单位触发（与 `Mob` 同一处按 attackType 分流）"),
    ("Unstable", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"static void CleanupDeaths\(", "RuleCore.CleanupDeaths",
     "不稳定：死亡时自爆（卡面没有正文，效果完全由规则定义 ⇒ 不进修辞触发表）"),
    ("Ecstasy", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"static void CheckEcstasyCrossing\(", "RuleCore.CheckEcstasyCrossing",
     "狂喜：血量**跨过**下限的那一瞬触发（不是「血少了就触发」）；`KwValue(Ecstasy)` 是阈值"),
    ("Destroyer", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"public static int IsValidTarget\(", "RuleCore.IsValidTarget",
     "毁灭者（原版 `frenzied` / 卡面印 `Destroyer`）：攻击目标的额外限制那一支"),
    ("Sabotage", "MyGame/Assets/RuleEngine/Core/EffectResolver.cs",
     r"static bool DoCreate\(", "EffectResolver.DoCreate",
     "破坏：**卡类标记**（不是单位机制）—— 造牌进手牌时按 `SpellType==230` 广播 `CreatesSabotage`"),
    ("Cruelty", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"static int HurtCore\(", "RuleCore.HurtCore",
     "残忍：受伤后对**本方**广播（`FireTriggerOnSide(..., Cruelty)`）"),
    ("Ability", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"public static int CanStartAbility\(", "RuleCore.CanStartAbility",
     "主动技能（`ability` 这个词本身）：启动闸；结算在 `UseAbilityCore` / `UseActiveAbilityCore`"),
    ("Waystone", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"static void CleanupDeaths\(", "RuleCore.CleanupDeaths",
     "路标石：死亡后留可收集的灵族残骸体（与 `Remnant` 同一支）；表现侧 `CardFeel.IsRemnantFamily`"),
    ("Artifice", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
    r"static void FireTriggerOnSide\(", "RuleCore.FireTriggerOnSide",
     "工匠（战术卡触发）：出战术卡时对同侧广播（调用点 `EffectResolver.PlayTacticBodyDeferred`）"),
    ("Agenda", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"public static readonly string\[\] AlternativeActions = \{", "RuleCore.AlternativeActions",
     "议程：四个「替代行动」关键词之一（`Duty/Pray/Ferocity/Agenda` 同一张表）；结算在 `UseAlternativeCore`"),
    ("Ferocity", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"static int UseAlternativeCore\(", "RuleCore.UseAlternativeCore",
     "凶残：替代行动之一（原文 `Ferocity`）；另有「部署当回合就能行动」的豁免 `HasDeployExemption`"),
    ("Oath", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"static int AbilityCostGate\(", "RuleCore.AbilityCostGate",
     "誓言：主动技能费按 trait 1275 的数值付（`PayActiveAbilityCostOath` 的等价物）；⛔ 别切到 `ActiveAbilityManaCost`"),
    ("BloodThirst", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"public static int ActionQuota\(", "RuleCore.ActionQuota",
     "嗜血：每回合行动配额 2（本方法）；广播/演出在 `EmitBloodThirst`"),
    ("Pray", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"static int UseAlternativeCore\(", "RuleCore.UseAlternativeCore",
     "祈祷（修女会）：置 `Prayed` + 挂 `Pray Enchantment`（`Enchantment.AttachPray`）+ 走唯一触发出口；trigger 655"),
    ("Duty", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"public static int CanUseAlternative\(", "RuleCore.CanUseAlternative",
     "职责（星界军）：一次性能力，可被「装填」再次使用（`reloadduty` 动词 + `DutyUsed` 闸）"),
    ("Codex", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"public static void CheckCodex\(", "RuleCore.CheckCodex",
     "圣典（极限战士）：能量为 0 时触发；出牌侧闸 = `CanPlayWithCodexNow`"),
    ("SpiritStone", "MyGame/Assets/RuleEngine/Core/EffectResolver.cs",
     r"static bool DoTriggerAbility\(", "EffectResolver.DoTriggerAbility",
     "灵魂石（灵族）：`N [Spirit Stone]:` 前缀的主动能力走这里（由 `SpiritOps` 结算、不付普通费用）"),
    ("Uprising", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"static int PlayCardCore\(", "RuleCore.PlayCardCore",
     "起义：部署时触发（`FireTriggerOnSummon` 那一族）"),
    ("Teleport", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"static int PlayCardCore\(", "RuleCore.PlayCardCore",
     "传送：本回合抽到 + 带这个词 ⇒ 可直接放到任意空位（`DrawnThisTurn` 闸），随后 `FireTriggerAt(..., Teleport, ...)`"),
    ("Stimulation", "MyGame/Assets/RuleEngine/Core/EffectResolver.cs",
     r"static int PlayTacticCore\(", "EffectResolver.PlayTacticCore",
     "刺激：战术卡打到带这个词的单位上 ⇒ 再触发一次它的能力（`FireTriggerAt(..., Stimulation, ...)`）"),
    ("Ambush", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"static int DeclareAttackCore\(", "RuleCore.DeclareAttackCore",
     "伏击：面朝下部署（`PlaysFaceDown`），被攻击时先翻开并撤掉这个词，再触发伏击正文"),
    ("SpentAllMana", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"static void FireSpentAllMana\(", "RuleCore.FireSpentAllMana",
     "花光法力（原版 trigger 641）：能量花到 0 时触发；池内 0 张"),
    ("TriggerEnchantment", "MyGame/Assets/RuleEngine/Core/Enchantment.cs",
     r"public static bool TriggerEnchantment\(", "Enchantment.TriggerEnchantment",
     "附魔触发（原版 trigger 500）：跑附魔卡的 `TriggerOps`；发射口 `EffectResolver.EmitEnchantmentTrigger`"),
    ("Companion", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"static void PlayCompanions\(", "RuleCore.PlayCompanions",
     "伙伴：从**卡池外**拉一张指定卡进场；计数器在 `UnitState.SetupTideAndCompanionCounters`"),
    ("Jam", "MyGame/Assets/RuleEngine/Core/UnitState.cs",
     r"public bool ApplyJam\(", "UnitState.ApplyJam",
     "干扰：状态侧的组合口（照原版 `CardScript.ReactToUnitJammed` 逐句搬）；读点散在 `RuleCore.CheckUnitsCancellingAttack` 等"),
    ("Requiem", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"public static bool ResolveTriggerRequiem\(", "RuleCore.ResolveTriggerRequiem",
     "安魂（trigger 430）：本卡或友军死亡时触发；池内 0 张（休眠件）"),
    ("Swarm", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"static void TrySwarmMerge\(", "RuleCore.TrySwarmMerge",
     "虫群：**合并**进同格宿主（`IsSwarmHost` 判宿主），随后 `FireTriggerAlways(..., Swarm, ...)`"),
    ("Synapse", "MyGame/Assets/RuleEngine/Core/EffectResolver.cs",
     r"static void RepeatTacticOnAdjacent\(", "EffectResolver.RepeatTacticOnAdjacent",
     "突触节点：相邻单位身上把这张战术**再跑一遍**；`SynapseExtraDeferred` 负责「多触发一次」"),
    ("Tide", "MyGame/Assets/RuleEngine/Core/UnitState.cs",
     r"public void SetupTideAndCompanionCounters\(", "UnitState.SetupTideAndCompanionCounters",
     "潮汐：进场时把关键词值**快照**进 `TideCounter`（不是每次现读 —— 见 `B102CARD707` 断言）"),
    ("Remnant", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"static void CleanupDeaths\(", "RuleCore.CleanupDeaths",
     "残余：死亡后翻成**残骸体**（1 血、留场、不吃护甲）；表现侧 `BattleDriver.RemnantPrefabOf`"),
    ("Landing", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"static bool OpenDropPod\(", "RuleCore.OpenDropPod",
     "落地（trigger 440）：开舱后触发；`DeclareAttackCore` 里还有「开了舱且带 landing」那一支"),
    ("AboutToAttack", "MyGame/Assets/RuleEngine/Core/RuleCore.cs",
     r"static bool TryCancelAttackByOwnAboutToAttackCore\(", "RuleCore.TryCancelAttackByOwnAboutToAttackCore",
     "即将攻击（trigger 280）：「取消这一记」那一条支路；池内 0 张"),
    ("AboutToAttackSpaced", "MyGame/Assets/RuleEngine/Core/CardDef.cs",
     r'new\[\] \{ "about to attack"', "KeywordTable.Prefixes",
     "`about to attack` 的**带空格写法**别名 ⇒ 归一化成 `abouttoattack`（⚠️ 别名表里它指向 `AboutToAttack`，不是 Spaced 常量）"),
]

# 关键词中文/英文名（**官方本地化** `Card_Trait/*`，现读；没有词条的留空 ⇒ 输出「查不到」）。
LOC_13LANG = os.path.join(BASE, "数据", "游戏数据", "官方本地化_en+zh-CN.json")

# 本地化 key ≠ 我们的常量值 的三个（**逐条核过**，⛔ 别按名字猜别处）：
#   · `SneakAttack`  → `Card_Trait/sneak`（显示名 `Sneak Attack`）
#   · `Penitence`    → `Card_Trait/rage`（显示名 `Penitence` —— key 叫 rage，词叫 penitence）
#   · `Destroyer`    → `Card_Trait/frenzied`（显示名 `Destroyer` —— 同上，key ≠ 词）
KW_LOC_KEY = {"sneakattack": "sneak", "penitence": "rage", "destroyer": "frenzied"}


def _src_lines(cache, relpath):
    """按二进制读 + 自己按 `\\n` 切（⛔ 不用文本模式：CRLF/裸 CR 都会让行号算错，见 CLAUDE.md）。"""
    if relpath not in cache:
        p = os.path.join(BASE, relpath)
        b = io.open(p, "rb").read()
        cache[relpath] = b.decode("utf-8", "replace").replace("\r\n", "\n").split("\n")
    return cache[relpath]


def _looks_like_decl(text):
    """落点行像不像**声明**（注释/空行/语句中间都不算）。与 `check_ledger_points` 同一条口径的轻量版。"""
    s = (text or "").strip()
    if not s or s.startswith("//") or s.startswith("*") or s.startswith("/*"):
        return False
    if re.match(r'^(?:\[[^\]]*\]\s*)?(?:public|private|internal|protected|static|readonly|sealed|'
                r'abstract|partial|override|virtual|const|new)\b', s):
        return True
    return bool(re.match(r'^(?:case\s|new\[\]\s*\{|[A-Za-z_][\w<>\[\],\. ]*\s+\w+\s*\()', s))


def _find_decl(cache, relpath, pattern):
    """在文件里找**第一条**匹配 `pattern` 的行；返回 (行号, 原文) 或 None。"""
    rx = re.compile(pattern)
    for i, ln in enumerate(_src_lines(cache, relpath)):
        if rx.search(ln) and _looks_like_decl(ln):
            return i + 1, ln.strip()
    return None


def _enclosing_decl(lines, idx):
    """从 idx（0 基）往上找最近的声明行（找声明行的唯一实现；`--kwindex` 与动词产出点共用）。"""
    ref_indent = len(lines[idx]) - len(lines[idx].lstrip())
    for j in range(idx, max(-1, idx - 400), -1):
        ln = lines[j]
        if _looks_like_decl(ln):
            indent = len(ln) - len(ln.lstrip())
            if indent <= ref_indent + 4:
                return j + 1, ln.strip()
    return None


def kw_dispatch(cache):
    """`EffectDispatch` → [(verb, 表行号, handler 名, (声明文件, 声明行) | None)]。

    ⚠️ handler **可能声明在同目录的 partial 文件里**（例：`DoDiscardFromHand` 在
    `EffectResolverPartial_B88E2.cs`）⇒ 声明行要扫**整个 `RuleEngine/Core` 目录**，
    ⛔ 只在 `EffectResolver.cs` 里找会把它们误报成「查不到」。
    """
    relp = "MyGame/Assets/RuleEngine/Core/EffectResolver.cs"
    lines = _src_lines(cache, relp)
    start = None
    for i, ln in enumerate(lines):
        if re.search(r"Dictionary<string,\s*EffectHandler>\s+EffectDispatch\s*=", ln):
            start = i
            break
    if start is None:
        return []
    # handler → (文件, 声明行)：扫整个 Core 目录
    hdecl = {}
    core_dir = os.path.join(BASE, "MyGame", "Assets", "RuleEngine", "Core")
    for fn in sorted(os.listdir(core_dir)):
        if not fn.endswith(".cs"):
            continue
        r = "MyGame/Assets/RuleEngine/Core/" + fn
        for i, ln in enumerate(_src_lines(cache, r)):
            m = re.match(r"\s*(?:public |private |internal |protected )?static bool (\w+)\s*\(", ln)
            if m:
                hdecl.setdefault(m.group(1), (r, i + 1))
    out = []
    for i in range(start, min(len(lines), start + 400)):
        ln = lines[i]
        if ln.strip() == "};":
            break
        for m in re.finditer(r'\{\s*"(\w+)"\s*,\s*\([^)]*\)\s*=>\s*(\w+)\(', ln):
            out.append((m.group(1), i + 1, m.group(2), hdecl.get(m.group(2))))
    return out


def kw_trigger_map(cache):
    """`TriggerNameOfKeyword` → [(关键词常量名, trigger 名, trigger 数值, case 行号)]。"""
    relp = "MyGame/Assets/RuleEngine/Core/ActionPump.cs"
    out = []
    for i, ln in enumerate(_src_lines(cache, relp)):
        m = re.search(r'case KeywordTable\.(\w+):\s*return "([^"]+)";\s*(?://\s*(\d+))?', ln)
        if m:
            out.append((m.group(1), m.group(2), m.group(3), i + 1))
    return out


def kw_verb_sites(cache):
    """`EffectText.cs` 里 `Verb = "<x>"` 的产出点 → {verb: [(方法标签:行, 行号)]}（取前 3 处）。

    标签带**方法名**（`EffectText.TryDeal:6816`）—— 这样在功能点索引里搜 `Deal` / `Heal`
    也能命中「这个动词是哪段代码产出的」。
    """
    relp = "MyGame/Assets/RuleEngine/Core/EffectText.cs"
    lines = _src_lines(cache, relp)
    out = {}
    for i, ln in enumerate(lines):
        if ln.strip().startswith("//"):
            continue
        for m in re.finditer(r'Verb\s*=\s*"(\w+)"', ln):
            d = _enclosing_decl(lines, i)
            if not d:
                continue
            mm = re.findall(r"(\w+)\s*\(", d[1])
            meth = mm[-1] if mm else "?"
            tag = "EffectText.%s:%d" % (meth, d[0])
            lst = out.setdefault(m.group(1), [])
            if len(lst) < 3 and d[0] not in [x[1] for x in lst]:
                lst.append((tag, d[0]))
    return out


def kw_keyword_consts(cache):
    """`KeywordTable` 的常量 → [(常量名, 值, 定义行号, 原版 trait id 或 None)]。"""
    relp = "MyGame/Assets/RuleEngine/Core/CardDef.cs"
    lines = _src_lines(cache, relp)
    out = []
    prev = 0
    for i, ln in enumerate(lines):
        m = re.match(r'\s*public const string (\w+) = "([^"]*)";', ln)
        if not m:
            continue
        # 原版 trait id：只认**本常量自己的 doc 块**里、且**名字与常量值对得上**的那条（⛔ 别捡邻居的）
        tid = None
        val = m.group(2).replace(" ", "").lower()
        for j in range(prev, i):
            mm = re.search(r"DefinedTrait\.(\w+)\s*=\s*\*{0,2}(\d+)", lines[j])
            if mm and mm.group(1).replace(" ", "").lower() == val:
                tid = "%s=%s" % (mm.group(1), mm.group(2))
        out.append((m.group(1), m.group(2), i + 1, tid))
        prev = i
    return out


def kw_loc_names():
    """`Card_Trait/<词>` → (英文, 中文)。现读官方本地化；读不到返回空表（调用方写「查不到」）。"""
    try:
        terms = read_json(LOC_13LANG).get("terms") or {}
    except Exception:
        return {}
    out = {}
    for k, v in terms.items():
        if not k.startswith("Card_Trait/") or not isinstance(v, list) or len(v) < 2:
            continue
        out[k.split("/", 1)[1].lower()] = (v[0], v[1])
    return out


def kw_pool_counts(D):
    """关键词 → 池内带它的卡数（`keywords`+`cardTraits` 前缀匹配，宽松口径 —— 只用于「有几张」，别当判据）。"""
    cnt = defaultdict(set)
    for c in D.engine:
        toks = list(c.get("keywords") or []) + list(c.get("cardTraits") or [])
        for t in toks:
            s = str(t).split(":")[0].strip().lower().replace(" ", "")
            if s:
                cnt[s].add(c["id"])
    return cnt


def build_kwindex(D):
    """出 `--kwindex` 的全部行（list[list[str]]，含列名行）。"""
    cache = {}
    consts = kw_keyword_consts(cache)
    trig = {k: (name, tid, line) for (k, name, tid, line) in kw_trigger_map(cache)}
    loc = kw_loc_names()
    pool = kw_pool_counts(D) if D is not None else {}
    rows = []
    warn = []

    # ---- ① 关键词
    landing = {t[0]: t for t in KW_LANDING}
    for (name, value, defline, tid) in consts:
        vk = value.replace(" ", "").lower()
        zh = loc.get(KW_LOC_KEY.get(vk, vk))
        disp = ("%s（%s / %s）" % (name, zh[0], zh[1])) if zh else ("%s（官方 Card_Trait 无此词条 ⇒ 中文名查不到）" % name)
        t = landing.get(name)
        if t is None:
            rows.append([disp, "关键词", "查不到", "查不到", "CardDef.cs:%d KeywordTable.%s" % (defline, name),
                         "本表未裁定落点（⛔ 不编）"])
            warn.append("关键词 %s：KW_LANDING 里没有条目" % name)
            continue
        _, relp, pat, label, note = t
        d = _find_decl(cache, relp, pat)
        if d is None:
            rows.append([disp, "关键词", label, "查不到", "CardDef.cs:%d KeywordTable.%s" % (defline, name),
                         "符号锚没命中：`%s` in %s（源码改了就重跑本工具）" % (pat, relp.split("/")[-1])])
            warn.append("关键词 %s：锚 %r 在 %s 里 0 命中" % (name, pat, relp))
            continue
        line, text = d
        if not _looks_like_decl(text):
            warn.append("关键词 %s：落点行不像声明（%s:%d）" % (name, relp.split("/")[-1], line))
        src = "CardDef.cs:%d KeywordTable.%s" % (defline, name)
        if tid:
            src += " · 原版 DefinedTrait.%s" % tid
        if name in trig:
            src += " · trigger %s(%s) @ActionPump.cs:%d" % (trig[name][1], trig[name][0], trig[name][2])
        extra = []
        if name in trig:
            extra.append("trigger=%s(%s)" % (trig[name][1], trig[name][0]))
        if pool:
            n = len(pool.get(value.replace(" ", "").lower(), ()))
            extra.append("池内 %d 张" % n)
        rows.append([disp, "关键词", label, "%s:%d" % (relp, line), src,
                     "%s%s" % (note, ("｜" + " · ".join(extra)) if extra else "")])

    # ---- ② 卡面动词（EffectDispatch = **唯一**的「动词 → 结算函数」表）
    vsites = kw_verb_sites(cache)
    for (verb, dline, handler, hdecl) in kw_dispatch(cache):
        if hdecl is None:
            rows.append([verb, "卡面动词", handler, "查不到", "EffectResolver.cs:%d EffectDispatch" % dline,
                         "handler `%s` 的声明行没找到（⛔ 不写猜的行号）" % handler])
            warn.append("动词 %s：handler %s 声明行没找到" % (verb, handler))
            continue
        hfile, hline = hdecl
        sites = vsites.get(verb) or []
        note = "解析产出点：" + ("、".join(s[0] for s in sites) if sites else "**EffectText 里没有字面量产出点**（可能由别的动词合成/或从数据直读）")
        rows.append([verb, "卡面动词", "EffectResolver.%s" % handler, "%s:%d" % (hfile, hline),
                     "EffectResolver.cs:%d EffectDispatch[\"%s\"]" % (dline, verb), note])

    # ---- ③ trigger（关键词 → 原版 trigger 名/数值；发射点见对应关键词行）
    for (k, tname, tid, line) in kw_trigger_map(cache):
        rows.append(["%s(%s)" % (tname, tid), "trigger", "ActionPump.TriggerNameOfKeyword",
                     "MyGame/Assets/RuleEngine/Core/ActionPump.cs:%d" % line,
                     "ActionPump.cs:%d case KeywordTable.%s" % (line, k),
                     "关键词 `%s` → 原版 trigger 名；**发射点**见本表「关键词」段那一行" % k])
    return rows, warn


def kwindex_text(D):
    rows, warn = build_kwindex(D)
    lines = []
    lines.append("# 关键词/效果词 → 代码落点索引（生成产物，⛔ 别手改：重跑会覆盖）")
    lines.append("# 生成器：python -I -X utf8 d:/4/Unity/工具/gen_card_dossier.py --kwindex --write")
    lines.append("# 数据源：CardDef.cs KeywordTable/Prefixes · EffectResolver.cs EffectDispatch · "
                 "EffectText.cs Verb 产出点 · ActionPump.cs TriggerNameOfKeyword · 官方本地化 Card_Trait/*（中文名）")
    lines.append("# 行号**每次现读**（符号锚），源码一动就重跑；⛔ 别拿旧行号当判据。查不到的行写「查不到」，⛔ 不是编的。")
    lines.append("# 类别口径：关键词 = 卡面/数据里的词（规则机制）；卡面动词 = 卡面正文的动词 → EffectDispatch handler；"
                 "trigger = 原版 AbilityTrigger 名/数值")
    lines.append("# 备注里的「池内 N 张」= cards_engine.json 的 keywords+cardTraits **前缀匹配**（宽松口径，"
                 "只说明有几张卡带它，⛔ 不是判据）；中文名取自官方本地化 `Card_Trait/*`，没有词条的写「查不到」，⛔ 不编")
    lines.append("\t".join(KW_COLS))
    for r in rows:
        lines.append("\t".join("" if c is None else str(c).replace("\t", " ").replace("\n", " ") for c in r))
    # 统计尾巴（也走 '#' 元信息，消费方跳过）
    cats = Counter(r[1] for r in rows)
    n_hit = sum(1 for r in rows if r[3] != "查不到")
    lines.append("# 统计：共 %d 行（%s）；有落点 %d 行 · 查不到 %d 行" %
                 (len(rows), " · ".join("%s %d" % (k, v) for k, v in sorted(cats.items())),
                  n_hit, len(rows) - n_hit))
    for w in warn:
        lines.append("# ⚠ %s" % w)
    return "\n".join(lines) + "\n", rows, warn


def run_kwindex(mode):
    """`--kwindex` 的唯一入口：dry / check / write。返回退出码（与 `--all` 同口径）。"""
    D = Data()
    text, rows, warn = kwindex_text(D)
    cats = Counter(r[1] for r in rows)
    n_hit = sum(1 for r in rows if r[3] != "查不到")
    print("关键词/效果词索引：%d 行（%s）· 有落点 %d · 查不到 %d"
          % (len(rows), " · ".join("%s %d" % (k, v) for k, v in sorted(cats.items())), n_hit, len(rows) - n_hit))
    for w in warn:
        print("  ⚠ " + w)
    if mode == "--dry":
        print("  零写盘（--dry）；要落盘用 --kwindex --write")
        return 0
    if mode == "--check":
        if not os.path.exists(P_KWINDEX):
            print("❌ 盘上没有 %s —— 先跑 --kwindex --write" % rel(P_KWINDEX))
            return 1
        cur = io.open(P_KWINDEX, "r", encoding="utf-8", newline="").read()
        if cur == text:
            print("✅ 逐字节相同（%s）" % rel(P_KWINDEX))
            return 0
        import difflib
        d = list(difflib.unified_diff(cur.split("\n"), text.split("\n"),
                                      fromfile="盘上", tofile="本次生成", lineterm="", n=0))
        print("❌ 不同：共 %d 行差异，前 40 行：" % len(d))
        for ln in d[:40]:
            print("   " + one_line(ln, 240))
        return 1
    if warn:
        print("⚠️ 有 %d 条告警（见上）；落盘会把这些告警一起写进文件头 '#' 行" % len(warn))
    io.open(P_KWINDEX, "w", encoding="utf-8", newline="\n").write(text)
    print("✅ 已写 %s（%d 行数据 + 表头/元信息）" % (rel(P_KWINDEX), len(rows)))
    return 0


# ======================================================================================
# 逐卡的「效果处理入口」两列（`code_verbs` / `code_abil`；2026-11-03 `B121-WF`）
#
# 为什么加：`code_entry` 是**全池同值**的通用管线（1,129 张卡只有 1 个取值），读者会误以为
# 「这就是这张卡的代码」（`报告/B121_CMT_…` §七·1）。这两列补的是**这张卡自己的**入口：
#
#   `code_verbs` = 该卡 `desc` **解析出来的动词** → `EffectDispatch` 的 handler 行号。
#     · 来源 = `工具/ruleprobe/基线/out_baseline.txt`（**ruleprobe 的解析基线**：它编的是
#       同一份 `RuleEngine/Core`，逐卡打 `op.Verb/...`）—— **这是我们真实的执行路径**。
#     · ⚠️ 基线是**某个时刻的快照**（文件头会写它的 mtime 与张数）：过期不等于错，
#       但「这条 op 是哪次加的」要看基线 mtime，⛔ 别拿它当「今天解析也是这个」的证明。
#
#   `code_abil` = 该卡**每个 ability**（`cards_truth.json` 的 SO 原始数据）→ ① 它的 trigger
#     （名字现读原版 `AbilityTrigger` 枚举）② 载荷形状 ③ **我们的触发口**（关键词↔trigger 表 + 通用口）。
#     · ⚠️ `cards_truth.json` **不是运行时数据**（全仓 `.cs` 只有注释引用它）：运行时吃的是
#       `cards_engine.json` + `desc` 文本。所以这一列答的是「**原版这条 ability 对应我们的哪个触发口**」，
#       而**不是**「这条 ability 的数据行被谁读」—— 后者在本工程不存在（我们按 `desc` 文本解析）。
# ======================================================================================

P_PARSE_BASE = os.path.join(TOOL_DIR, "ruleprobe", "基线", "out_baseline.txt")

# 非关键词 trigger 的触发口（`TriggerNameOfKeyword` 只覆盖 22 个关键词，其余走这条兜底）
EXTRA_TRIGGER_LANDING = [
    ("ThisUnitDeath", "MyGame/Assets/RuleEngine/Core/RuleCore.cs", r"static void CleanupDeaths\(",
     "RuleCore.CleanupDeaths"),
    ("Unstable", "MyGame/Assets/RuleEngine/Core/RuleCore.cs", r"static void CleanupDeaths\(",
     "RuleCore.CleanupDeaths"),
    ("TriggerEnchantment", "MyGame/Assets/RuleEngine/Core/Enchantment.cs",
     r"public static bool TriggerEnchantment\(", "Enchantment.TriggerEnchantment"),
]

_PARSE_VERBS = {}       # {card_id: Counter(verb)} —— 进程内只读一次
_PARSE_META = {}


def load_parse_verbs():
    """读 ruleprobe 解析基线 → ({id: Counter(verb)}, meta)。读不到返回 ({}, {'error': …})。"""
    if _PARSE_VERBS or _PARSE_META.get("error"):
        return _PARSE_VERBS, _PARSE_META
    try:
        raw = io.open(P_PARSE_BASE, "rb").read().decode("utf-8", "replace")
    except Exception as e:
        _PARSE_META["error"] = "读不到 %s（%s）" % (rel(P_PARSE_BASE), e)
        return _PARSE_VERBS, _PARSE_META
    import datetime
    _PARSE_META["mtime"] = datetime.datetime.fromtimestamp(os.path.getmtime(P_PARSE_BASE)).strftime("%Y-%m-%d %H:%M")
    for ln in raw.split("\n"):
        if not ln.strip():
            continue
        f = ln.split("|")
        if len(f) < 5:
            continue
        c = Counter()
        for op in f[4].split(";"):
            if op:
                c[op.split("/")[0]] += 1
        _PARSE_VERBS[f[0]] = c
    _PARSE_META["n"] = len(_PARSE_VERBS)
    return _PARSE_VERBS, _PARSE_META


def verb_handler_map():
    """verb → (方法标签, 文件, 行)（现读 `EffectDispatch`）。"""
    if "_m" not in _PARSE_META:
        cache = {}
        m = {}
        for (verb, dline, handler, hdecl) in kw_dispatch(cache):
            if hdecl:
                m[verb] = ("EffectResolver.%s" % handler, hdecl[0], hdecl[1])
        _PARSE_META["_m"] = m
    return _PARSE_META["_m"]


def trigger_landing_map():
    """trigger 名 → (方法标签, 文件:行)（= 关键词↔trigger 表 + 三条非关键词兜底）。"""
    if "_t" not in _PARSE_META:
        cache = {}
        landing = {t[0]: t for t in KW_LANDING}
        m = {}
        for (k, name, tid, line) in kw_trigger_map(cache):
            t = landing.get(k)
            if not t:
                continue
            d = _find_decl(cache, t[1], t[2])
            if d:
                m.setdefault(name, ("%s（关键词 `%s`）" % (t[3], k), "%s:%d" % (t[1], d[0])))
        for (name, relp, pat, label) in EXTRA_TRIGGER_LANDING:
            d = _find_decl(cache, relp, pat)
            if d:
                m.setdefault(name, (label, "%s:%d" % (relp, d[0])))
        # 通用兜底：所有 trigger 都从 `FireTriggerAt` 进
        d = _find_decl(cache, "MyGame/Assets/RuleEngine/Core/RuleCore.cs", r"static bool FireTriggerAt\(")
        m["__generic__"] = ("RuleCore.FireTriggerAt（通用触发口）", "%s:%d" % (
            "MyGame/Assets/RuleEngine/Core/RuleCore.cs", d[0]) if d else "查不到")
        _PARSE_META["_t"] = m
    return _PARSE_META["_t"]


def code_verbs_cell(cid):
    """该卡 desc 解析出的动词 → handler 行号。"""
    verbs, meta = load_parse_verbs()
    if meta.get("error"):
        return "查不到:" + meta["error"]
    c = verbs.get(cid)
    if c is None:
        return "查不到:ruleprobe 解析基线（%s 张 · %s）里没有这张卡" % (meta.get("n"), meta.get("mtime"))
    if not c:
        # 13 张 `TRANSFORM_<FACTION>`（变身战术卡）：`desc` 是说明文字「Transform your Warlord
        # into another Warlord of the same faction, chosen at random.」，文本解析器不产 op
        # ⇒ 基线里该卡 op 列为空。但运行时 op 由 `RuleCore.TacticOpsOf` **直接构造**一条
        # `warlordtransform`（`EffectResolver.cs:1096` 已注册该动词，唯一取值口 `:2046`/`:2622`）
        # ⇒ ⛔ 别把它们当「白板卡」。判据 → `判据/B124_对账_C_工具索引.md` §一·1 / §二·步骤 2。
        if cid.startswith("TRANSFORM_"):
            h = verb_handler_map().get("warlordtransform")
            if h:
                return "warlordtransform→%s:%d ×1" % (h[0], h[2])
            return "warlordtransform→查不到（EffectDispatch 里没有这个动词） ×1"
        return "无:desc 解析出 0 个 op（白板卡 / 只有被动 trait）"
    hm = verb_handler_map()
    parts = []
    for verb, n in c.most_common():
        h = hm.get(verb)
        if h:
            parts.append("%s→%s:%d ×%d" % (verb, h[0], h[2], n))
        else:
            parts.append("%s→查不到（EffectDispatch 里没有这个动词） ×%d" % (verb, n))
    return "有(%d 个动词 / %d 条 op): %s" % (len(c), sum(c.values()), " ; ".join(parts))


def code_abil_cell(rec, enums):
    """逐 ability：trigger 名 + 载荷形状 + 我们的触发口。"""
    abil = rec.get("abil_raw") or []
    if not abil:
        return tri(rec.get("abilities_state", "无"), rec.get("abilities_note", ""))
    tm = trigger_landing_map()
    gen = tm.get("__generic__")
    out = []
    for (label, trig, kinds) in abil:
        tn = enums.get(trig) if trig is not None else None
        head = "%s(trigger %s%s)" % (label, trig if trig is not None else "?", ("/" + tn) if tn else
                                     ("/名字查不到" if trig is not None else ""))
        where = tm.get(tn) if tn else None
        if where:
            w = "%s @%s" % where
        elif gen:
            w = "%s @%s（该 trigger 名不在关键词映射表里）" % gen
        else:
            w = "查不到"
        out.append("%s 载荷=%s → 触发口 %s" % (head, "/".join(kinds) if kinds else "（无 payload 行）", w))
    return "有(%d 个 ability): %s" % (len(abil), " ;; ".join(out))


# ---------------------------------------------------------------- 基础
def read_json(path):
    return json.load(io.open(path, "r", encoding="utf-8"))


def read_tsv(path):
    """TSV：UTF-8，按 '\\n' 切（⛔ 不用 csv 模块，见 CLAUDE.md），去行尾 '\\r'。"""
    raw = io.open(path, "r", encoding="utf-8", newline="").read()
    lines = raw.split("\n")
    if lines and lines[-1] == "":
        lines.pop()
    return [ln.rstrip("\r").split("\t") for ln in lines]


def rel(p):
    """绝对路径 → 相对 D:/4/Unity 的正斜杠路径（输出里统一这么写）。"""
    return os.path.relpath(p, BASE).replace("\\", "/")


def one_line(s, cap=400):
    if s is None:
        return ""
    s = str(s).replace("\t", " ").replace("\r", " ").replace("\n", " ")
    s = re.sub(r"\s+", " ", s).strip()
    return s if len(s) <= cap else s[:cap] + "…"


CELL_CAP = 20000      # 结构化列的单元格上限：只防病态，不做业务截断（⛔ 400 那种会静默吃掉半张卡的能力）


def one_line_c(s):
    """结构化单元格：只去 tab/换行，其余原样（供 mod 脚本按分隔符自己切）。"""
    return one_line(s, CELL_CAP)


def tri(state, text):
    """三态前缀：有 / 无 / 查不到（⛔ 不许用空串糊过去）。"""
    return "%s:%s" % (state, text)


# ---------------------------------------------------------------- 原版枚举（现读）
def load_enum(name):
    """从 `D:/2/Warpforge_code/Scripts/Assembly-CSharp/<name>.cs` 读 `name = 值` 对。
    读不到 ⇒ 返回空表（调用方会把裸数字打出来 + 标「查不到枚举名」）。"""
    p = os.path.join(ENUM_SRC, name + ".cs")
    if not os.path.exists(p):
        return {}
    txt = io.open(p, "r", encoding="utf-8", errors="replace").read()
    out = {}
    for m in re.finditer(r"^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(-?\d+)\s*,?\s*$", txt, re.M):
        out[int(m.group(2))] = m.group(1)
    return out


# ---------------------------------------------------------------- 载入
class Data(object):
    def __init__(self):
        self.engine_raw = read_json(P_ENGINE)
        self.engine = self.engine_raw["cards"]
        self.eidx = {c["id"]: c for c in self.engine}
        self.engine_pos = {c["id"]: i for i, c in enumerate(self.engine)}

        t = read_json(P_TRUTH)
        self.truth_meta = {k: t.get(k) for k in ("count", "generatedAt", "source")}
        self.truth = {c["id"]: c for c in t["cards"]}

        v = read_json(P_VFX)
        self.vfx_meta = {k: v.get(k) for k in ("generatedBy", "generatedAt", "cardCount", "entryCount")}
        self.vfx = {c["id"]: c for c in v["cards"]}
        self.vfx_notplayed = defaultdict(list)
        for r in v.get("notPlayed") or []:
            self.vfx_notplayed[r.get("id")].append(r)

        c = read_json(P_CLIPS)
        self.clips = {r["a"]: r for r in c["rows"]}

        vo = read_json(P_VOICE)
        self.voice = {c0["id"]: c0 for c0 in vo["cards"]}

        # 「在工程 / 在消费表」判定：只读复用（⛔ 不另算一套）
        self.evt = defaultdict(list)          # (id, 维) -> rows
        ev_rows = read_tsv(P_EVT)
        self.evt_header = ev_rows[0]
        for r in ev_rows[1:]:
            if len(r) >= 9:
                self.evt[(r[0], r[1])].append(r)

        self.verify = {}
        for r in read_tsv(P_VERIFY)[1:]:
            if len(r) >= 12:
                self.verify[r[0]] = r

        # trait id → 名（全池 cardTraits 汇总）
        self.trait_name = {}
        for c0 in self.truth.values():
            for tr in c0.get("cardTraits") or []:
                tid = tr.get("_trait")
                if tid is not None and tid not in self.trait_name:
                    self.trait_name[tid] = tr.get("name") or "?"

        self.vo_files = set(os.listdir(VO_DIR)) if os.path.isdir(VO_DIR) else set()

        self.trig = load_enum("AbilityTrigger")
        self.ta = load_enum("TargetsAffected")
        self.pa = load_enum("PlayersAffected")
        self.buff = load_enum("BuffType")
        self.eff = load_enum("AbilityEffect")
        self.enum_missing = [n for n, tbl in (("AbilityTrigger", self.trig), ("TargetsAffected", self.ta),
                                              ("PlayersAffected", self.pa), ("BuffType", self.buff),
                                              ("AbilityEffect", self.eff)) if not tbl]

        self.bulk_tables = []       # 被整表排除的代码数据表（懒加载时填）
        self._entry = None          # 代码落点行号（懒加载）
        self._special = None        # 逐卡特判（懒加载）
        self._crit = None           # 判据文档索引（懒加载）

    # ---------------------------------------------------------- 枚举渲染
    def e(self, tbl, val, fallback="?"):
        if val is None:
            return "—"
        name = tbl.get(val)
        return "%s(%d)" % (name, val) if name else "%d(枚举名查不到)" % val

    # ---------------------------------------------------------- 代码落点
    def code_entries(self):
        if self._entry is not None:
            return self._entry
        out = []
        for label, relp, pat in CODE_ENTRIES:
            p = os.path.join(BASE, relp.replace("/", os.sep))
            line = None
            if os.path.exists(p):
                try:
                    txt = io.open(p, "r", encoding="utf-8", errors="replace").read()
                    m = re.search(pat, txt)
                    if m:
                        line = txt.count("\n", 0, m.start()) + 1
                except Exception:
                    line = None
            out.append((label, relp, line))
        self._entry = out
        return out

    # ---------------------------------------------------------- 逐卡特判
    def special_cases(self):
        """扫 RuleEngine/{Core,Data} 的 .cs，找「引用了这个卡 id」的地方。
        返回 (idx, bulk_files)：idx[cardId] = [(文件, 行号, 行文本)]；bulk_files = 被整表排掉的数据表。"""
        if self._special is not None:
            return self._special
        tokre = re.compile(r'"([^"\\\n]{1,64})"')
        per_file_ids = {}
        for d in SRC_DIRS:
            for dp, _, fs in os.walk(d):
                for fn in fs:
                    if not fn.endswith(".cs"):
                        continue
                    p = os.path.join(dp, fn)
                    try:
                        txt = io.open(p, "r", encoding="utf-8", errors="replace").read()
                    except Exception:
                        continue
                    ids = set(t for t in tokre.findall(txt) if t in self.eidx)
                    if ids:
                        per_file_ids[p] = ids
        bulk = set(p for p, ids in per_file_ids.items() if len(ids) >= BULK_TABLE_MIN)
        idx = defaultdict(list)
        for p, ids in per_file_ids.items():
            if p in bulk:
                continue
            txt = io.open(p, "r", encoding="utf-8", errors="replace").read()
            for i, line in enumerate(txt.split("\n"), 1):
                if '"' not in line:
                    continue
                hit = [t for t in tokre.findall(line) if t in ids]
                if not hit:
                    continue
                for cid in hit:
                    idx[cid].append((rel(p), i, one_line(line.strip(), 150)))
        self.bulk_tables = [rel(p) for p in sorted(bulk)]
        self._special = (idx, self.bulk_tables)
        return self._special

    # ---------------------------------------------------------- 判据文档
    def criteria_docs(self):
        """一遍扫 `判据/` 全部文本，按「文件里出现了这个卡的 id 原文」建反查表。
        ⛔ 不做模糊匹配 —— 只认 id 字面量（大小写敏感、前后不能接字母数字）。
        🆕 2026-11-03（批 B124 · 账 `B123-IDXPOLL` 窄档）：**文件名含「对账」的文档不进本列** ——
        对账报告只是「顺手提到某个卡 id 的证据」，不是该卡的专项判据；否则每写一份对账报告，
        `卡牌总索引.tsv` 的 `criteria_docs` 列就跟着变（`--all --check` 永不稳定）。
        ⚠️ 只排「对账」这一档：`B88_R1_90档_逐卡施工单` / `B117_逐卡核验_*` 这类**真·逐卡判据**必须留着。"""
        if self._crit is not None:
            return self._crit
        tokre = re.compile(r"(?<![0-9A-Za-z_])([A-Z]{2,4}[0-9]{1,3}|[0-9a-f]{32})(?![0-9A-Za-z_])")
        per_file = {}
        for dp, _, fs in os.walk(CRIT_DIR):
            for fn in fs:
                if not (fn.endswith(".md") or fn.endswith(".tsv") or fn.endswith(".txt")):
                    continue
                if "对账" in fn:
                    continue          # 窄档：对账报告不当卡牌专项判据（见上）
                p = os.path.join(dp, fn)
                if os.path.abspath(p) == os.path.abspath(P_OUT):
                    continue          # ⛔ 自己的产物不进判据索引（否则第一次写盘后自指）
                try:
                    txt = io.open(p, "r", encoding="utf-8", errors="replace").read()
                except Exception:
                    continue
                ids = set(t for t in tokre.findall(txt) if t in self.eidx)
                if ids:
                    per_file[p] = ids
        # 「全池表」= 一个文件里出现 ≥BULK_TABLE_MIN 个不同卡 id（全池对拍/覆盖/核验那类）
        # ⇒ 它对每张卡都成立、不是「这张卡的专项文档」⇒ 单列一份名单，⛔ 不糊进每张卡。
        self._crit_bulk = sorted(rel(p) for p, ids in per_file.items() if len(ids) >= BULK_TABLE_MIN)
        idx = defaultdict(list)
        for p, ids in per_file.items():
            if len(ids) >= BULK_TABLE_MIN:
                continue
            for t in ids:
                idx[t].append(rel(p))
        self._crit = {k: sorted(set(v)) for k, v in idx.items()}
        return self._crit

    def criteria_bulk(self):
        self.criteria_docs()
        return self._crit_bulk

    # ---------------------------------------------------------- ops（能力 → op 行）
    def trait_of(self, tid):
        if not tid:
            return "—"
        return "%s(%s)" % (self.trait_name.get(tid, "?"), tid)

    def buff_desc(self, b):
        parts = []
        bt = b.get("buffType")
        parts.append("buffType " + (self.e(self.buff, bt) if bt is not None else "缺键（原版这行没写 buffType）"))
        for k, lab in (("meleeAttackChange", "melee"), ("rangeAttackChange", "ranged"),
                       ("healthChange", "health"), ("armourChange", "armour"),
                       ("costChange", "cost")):
            if b.get(k):
                parts.append("%s%+d" % (lab, b[k]))
        if b.get("trait") or b.get("traitValue"):
            parts.append("trait %s v%s" % (self.trait_of(b.get("trait")), b.get("traitValue", 0)))
        dur = [k for k in ("untilEndOfTurn", "untilPlayerTurnStart", "untilEnemyTurnStart",
                           "untilNextAttack", "untilFollowingPlayerTurnStart", "permanentBuff")
               if b.get(k)]
        if dur:
            parts.append("时长=" + "/".join(dur))
        if b.get("enchantment"):
            parts.append("enchantment PPtr(附魔模板)")
        if b.get("effectName"):
            parts.append("effectName=%s" % one_line(b.get("effectName"), 40))
        if b.get("doNotStack"):
            parts.append("doNotStack")
        return " ".join(parts)

    def op_line(self, lg):
        """一条 `abilityLogic[j]` → 一行「这行数据被解析成什么 op」。"""
        parts = []
        if "chosenAbility" in lg:
            parts.append("chosenAbility " + self.e(self.eff, lg.get("chosenAbility")))
        if lg.get("playersAffected") is not None:
            parts.append("playersAffected " + self.e(self.pa, lg.get("playersAffected")))
        tc = lg.get("targetCriteria")
        if tc:
            ta = tc.get("targetsAffected")
            extra = ["%s=%s" % (k, v) for k, v in tc.items() if k != "targetsAffected" and v not in (0, [], None)]
            parts.append("ta " + self.e(self.ta, ta) + ((" [" + ",".join(extra) + "]") if extra else ""))
        if "damage" in lg:
            d = lg["damage"] or {}
            parts.append("damage %s-%s type=%s opt=%s" % (d.get("minDamage"), d.get("maxDamage"),
                                                          d.get("damageType"), d.get("valueOptions")))
        if "heal" in lg:
            h = lg["heal"] or {}
            parts.append("heal %s-%s" % (h.get("minValue"), h.get("maxValue")))
        if "buff" in lg:
            parts.append("buff " + self.buff_desc(lg["buff"] or {}))
        if "handBuff" in lg:
            hb = lg["handBuff"] or {}
            ce = hb.get("cardEffect") or {}
            parts.append("handBuff " + self.buff_desc(ce) +
                         (" uses=%s/%s" % (hb.get("numberOfUses"), hb.get("limitedUses")) if hb.get("limitedUses") else ""))
        if "manaBuff" in lg:
            mb = lg["manaBuff"] or {}
            flags = [k for k in ("friendlyPlayer", "enemyPlayer", "createMana", "refillMana",
                                 "spiritStoneMana", "questPoints", "faith") if mb.get(k)]
            parts.append("manaBuff +%s %s" % (mb.get("manaAmount"), "/".join(flags)))
        if "summon" in lg:
            s = lg["summon"] or {}
            us = s.get("unitsToSummon") or []
            names = [("%s=%s" % (u, self.truth.get(u, {}).get("name") or self.eidx.get(u, {}).get("name") or "?"))
                     for u in us]
            parts.append("summon " + ",".join(names))
        if lg.get("multiplier") is not None:
            parts.append("multiplier=%s" % lg.get("multiplier"))
        if lg.get("furtherMultiplier") is not None:
            parts.append("furtherMultiplier=%s" % lg.get("furtherMultiplier"))
        if lg.get("chanceOfHappening") is not None:
            parts.append("chanceOfHappening=%s%%" % lg.get("chanceOfHappening"))
        return " · ".join(parts) if parts else "（空行：原版这行没有 payload）"

    def ability_block(self, ab, label):
        """一个 ability 对象 → [(标题, [op 行])]。"""
        trig = ab.get("trigger")
        head = "%s trigger %s" % (label, self.e(self.trig, trig) if trig is not None else "(该 ability 无 trigger 键)")
        if ab.get("triggerValue") is not None:
            head += " triggerValue=%s" % ab.get("triggerValue")
        if ab.get("abilityContext") is not None:
            head += " abilityContext=%s" % ab.get("abilityContext")
        for k in ("manaCost", "plasmaCost", "psychicCost"):
            if ab.get(k):
                head += " %s=%s" % (k, ab[k])
        if ab.get("targeted"):
            head += " targeted=1"
        cond = []
        for k in ("actingCardCriteria", "boardCriteria", "multiCountCriteria"):
            v = ab.get(k)
            if isinstance(v, dict):
                nz = ["%s=%s" % (a, b) for a, b in v.items() if b not in (0, [], None)]
                if nz:
                    cond.append(k + "[" + ",".join(nz) + "]")
        if cond:
            head += " 条件:" + " ".join(cond)
        return head, [self.op_line(lg) for lg in (ab.get("logic") or [])]


# ---------------------------------------------------------------- 一张卡的档案
def ability_kinds(ab):
    """一个 ability 的**载荷形状**（只取有语义的那几种键）—— 给 `code_abil` 列用。

    ⚠️ 只报「有哪些键」，**不解释成动词**：`damage` 行不等于一定走 `deal`（数据驱动的能力由
    原版 `AbilityLogic` 跑，而我们按 `desc` 文本解析）⇒ 解释性结论一律去 `code_verbs` 列看。
    """
    kinds = []
    for lg in (ab.get("logic") or []):
        for k in ("damage", "heal", "summon", "buff", "handBuff", "manaBuff"):
            if k in lg and k not in kinds:
                kinds.append(k)
        ca = lg.get("chosenAbility")
        if ca and ca not in (0,):
            tag = "chosenAbility %s" % ca
            if tag not in kinds:
                kinds.append(tag)
    return kinds


def build(D, cid):
    eng = D.eidx.get(cid) or {}
    tru = D.truth.get(cid) or {}
    rec = {"id": cid, "gaps": []}

    # ---- 1 身份
    rec["name"] = eng.get("name") or tru.get("name") or "?"
    rec["nameZh"] = eng.get("nameZh") or ""
    if not rec["nameZh"]:
        rec["gaps"].append("中文名：cards_engine.json 该卡无 nameZh（全池仅 2 张如此）")
    rec["faction"] = eng.get("faction") or "?"
    rec["subtype"] = eng.get("subtype") or tri("无", "cards_engine.subtype 空串（现池 13 张：tactic ×12 + enchantment ×1）")
    rec["cardType"] = eng.get("type") or "?"
    rec["rarity"] = eng.get("rarity") or ""
    rec["cost"] = eng.get("cost")
    rec["melee"] = eng.get("attack")
    rec["ranged"] = eng.get("ranged")
    rec["armour"] = eng.get("armour")
    rec["health"] = eng.get("health")
    kws = list(eng.get("keywords") or [])
    if eng.get("cardTraits"):
        for t in eng["cardTraits"]:
            if t not in kws:
                kws.append(t)
    rec["keywords"] = kws
    traits = []
    for tr in (tru.get("cardTraits") or []):
        traits.append("%s(%s)" % (tr.get("name"), tr.get("_trait")))
    rec["traits"] = traits
    _nod = tri("无", "cards_engine.desc 空串 = 原版这张卡没有效果文字（现池 8 张，全是白板单位/附魔）")
    rec["desc"] = eng.get("desc") or _nod
    rec["descZh"] = eng.get("descZh") or eng.get("desc") or _nod

    # ---- 2 能力
    abilities = []
    if not tru:
        rec["gaps"].append("效果树：cards_truth.json 无该 id（已查 cards_truth 的 1,351 张）")
        rec["abilities"] = []
        rec["abilities_state"] = "查不到"
        rec["abilities_note"] = "cards_truth.json 无该 id（该表覆盖 1,351 张卡 SO）"
    else:
        abil_raw = []
        for label, key in (("abilities", "abilities"), ("activeAbility", "activeAbility"),
                           ("alternativeAbility", "alternativeAbility")):
            v = tru.get(key)
            if not v:
                continue
            if isinstance(v, dict):
                v = [v]
            for k, ab in enumerate(v):
                if isinstance(ab, dict):
                    abilities.append(D.ability_block(ab, "%s[%d]" % (label, k)))
                    abil_raw.append(("%s[%d]" % (label, k), ab.get("trigger"), ability_kinds(ab)))
        rec["abilities"] = abilities
        rec["abil_raw"] = abil_raw
        if not abilities:
            rec["abilities_state"] = "无"
            rec["abilities_note"] = "cards_truth.json 该卡 abilities/activeAbility/alternativeAbility 全为空（原版数据如此）"
            rec["gaps"].append("能力：cards_truth.json 该卡三处能力全为空 ⇒ 无能力（原版数据如此）")

    # ---- 3 特效
    vfx_rows = D.evt.get((cid, "特效"), [])
    vfx_decl = []
    ve = D.vfx.get(cid)
    if ve:
        for evd in ve.get("events") or []:
            ev = evd.get("ev")
            for es in evd.get("es") or []:
                vfx_decl.append((ev, es.get("n"), es.get("c"), es.get("a")))
    if vfx_decl:
        rec["vfx_state"] = "有"
        rec["vfx"] = vfx_decl
        rec["vfx_rows"] = vfx_rows
    elif ve:
        rec["vfx_state"] = "无"
        rec["vfx_note"] = "card_vfx_by_card.json 有这张卡但 events 为空（原版没声明逐卡特效）"
        rec["vfx"] = []
        rec["vfx_rows"] = []
    else:
        rec["vfx_state"] = "查不到"
        rec["vfx_note"] = ("card_vfx_by_card.json 未收录该 id（已查该表 %d 张卡的 events 键）；"
                           "已查 全池事件覆盖_静态.tsv 的 维=特效 行（该表覆盖 %d 张卡）"
                           % (len(D.vfx), len(set(k[0] for k in D.evt))))
        rec["vfx"] = []
        rec["vfx_rows"] = vfx_rows
    rec["vfx_notplayed"] = D.vfx_notplayed.get(cid, [])

    # ---- 4 动画
    anim = eng.get("animTimes") or []
    rec["anim"] = anim
    if anim:
        rec["anim_state"] = "有"
    else:
        rec["anim_state"] = "无"
        rec["anim_note"] = "cards_engine.json 的 animTimes 为空数组（原版该卡没声明动画）"
    rec["anim_rows"] = D.evt.get((cid, "动画"), [])

    # ---- 5 语音
    vl = D.voice.get(cid)
    lines = (vl or {}).get("lines") or []
    rec["voice"] = lines
    rec["voice_rows"] = D.evt.get((cid, "语音"), [])
    if lines:
        rec["voice_state"] = "有"
    else:
        vaud = (ve or {}).get("audio") or []
        if vaud:
            rec["voice_state"] = "有"
            rec["voice"] = [{"ev": a.get("ev"), "file": a.get("n"), "src": "card_vfx_by_card.audio"} for a in vaud]
        else:
            rec["voice_state"] = "无"
            rec["voice_note"] = ("voice_lines.json 无该 id 的登记（该表 %d 张卡有行）；"
                                 "card_vfx_by_card.json 该卡的 audio 也为空" % len(D.voice))

    # ---- 6 代码落点 / 特判
    sidx, bulk = D.special_cases()
    rec["special"] = sidx.get(cid, [])
    rec["bulk_tables"] = bulk

    # ---- 7 判据文档
    rec["crit_docs"] = D.criteria_docs().get(cid, [])

    # ---- 8 核验判定
    vr = D.verify.get(cid)
    rec["verify"] = {"引擎": vr[4], "特效": vr[6], "动画": vr[8], "语音": vr[10]} if vr else None
    if vr is None:
        rec["gaps"].append("四维核验：全卡核验_判定.tsv 无该 id")

    return rec


def verify_tsv_cell(rec):
    if not rec.get("verify"):
        return tri("查不到", "全卡核验_判定.tsv 无该行")
    v = rec["verify"]
    return "引擎%s/特效%s/动画%s/语音%s" % (v["引擎"], v["特效"], v["动画"], v["语音"])


def vfx_cell(rec):
    if rec["vfx_state"] != "有":
        return tri(rec["vfx_state"], rec.get("vfx_note", ""))
    rows = {}
    for r in rec["vfx_rows"]:
        rows[(r[2], r[3])] = r
    out = []
    for ev, n, conf, anim in rec["vfx"]:
        r = rows.get((ev, n))
        st = ("[工程%s/消费表%s/参数%s]" % (r[4], r[5], r[7])) if r else "[该表无此事件行]"
        out.append("%s→%s(%s)%s" % (ev, n, conf, st))
    if not rec["vfx_rows"]:
        out.append("⚠ 全池事件覆盖_静态.tsv 无该卡的特效行（该表覆盖 1,100 张卡）")
    return "有(" + str(len(rec["vfx"])) + "条): " + " ; ".join(out)


def anim_cell(rec):
    """覆盖表的「动画」行**不是按 CardAnim 名连的** —— 列序是
    `卡id | 维=动画 | 事件=<trigger 名 = animTimes.tn> | 资源名=<prefab = animTimes.n> | …`
    （⛔ 别拿 `animTimes.a` 去和它 join，那会一条都配不上）。"""
    if rec["anim_state"] != "有":
        return tri("无", rec.get("anim_note", ""))
    clips = rec.get("_clips") or {}
    rows = {}
    for r in rec["anim_rows"]:
        rows[(r[2], r[3])] = r
    out = []
    miss = 0
    for a in rec["anim"]:
        clip = a.get("a") or ""
        prefab = a.get("n") or ""
        tg = a.get("tg")
        # 覆盖表「动画」行的资源名列 = prefab 名；`Wait_*` 那种纯延时占位没有 prefab，
        # 那张表把 CardAnim 名写进同一列 ⇒ 连接键取 `n or a`（⛔ 别只拿 n）。
        r = rows.get((a.get("tn") or "", prefab or clip))
        if r is None:
            miss += 1
            cov = "覆盖表无行"
        else:
            cov = "工程%s/消费表%s/参数%s" % (r[4], r[5], r[7])
        if clip.startswith("Wait_"):
            mark = "纯延时占位（原版 animInfo.animAdressable 的 GUID 为空、无 prefab）"
        else:
            mark = ("✓" if clip in clips else "✗不在 cardanim_clips") + "CardAnim " + clip
        out.append("%s(tg%s:%s)→%s prefab[%s]{%s}" %
                   (clip, tg, a.get("tn") or "?", mark, prefab or "—", cov))
    return "有(" + str(len(rec["anim"])) + "条): " + " ; ".join(out) + \
        ("" if miss == 0 else " ; ⚠%d 条在覆盖表里配不上" % miss)


def voice_cell(rec):
    if rec["voice_state"] != "有":
        return tri("无", rec.get("voice_note", ""))
    out = []
    for l in rec["voice"]:
        f = l.get("file") or ""
        out.append("%s→%s[src=%s,so=%s]" % (l.get("ev"), f, l.get("src", ""), l.get("so", "—")))
    return "有(" + str(len(rec["voice"])) + "条): " + " ; ".join(out)


COLS = ["id", "name", "nameZh", "faction", "subtype", "cardType", "rarity",
        "cost", "melee", "ranged", "armour", "health", "keywords", "traits",
        "desc", "descZh", "abilities", "vfx", "anim", "voice",
        "code_entry", "code_special", "criteria_docs", "verify", "gaps",
        # 🆕 2026-11-03（`B121-WF`）：**逐卡**的效果处理入口（`code_entry` 是全池同值的通用管线）。
        # 追加在**末尾**（⛔ 不插中间）：按列名取值的消费方不受影响，按位置取的也只多两列。
        "code_verbs", "code_abil"]


def abilities_cell(rec):
    if not rec.get("abilities"):
        return tri(rec.get("abilities_state", "无"), rec.get("abilities_note", ""))
    out = []
    for head, ops in rec["abilities"]:
        out.append(head + " ⇒ " + " | ".join(ops))
    return "有(%d 个 ability): " % len(rec["abilities"]) + " ;; ".join(out)


def code_entry_cell(D):
    out = []
    for label, relp, line in D.code_entries():
        out.append("%s=%s:%s" % (label, relp, line if line else "❓符号没找到"))
    # 🆕 2026-11-03（`B121-WF`）：加前缀，明说这一列**不是这张卡专属**（全池同值）。
    # 真想查「这张卡的效果在哪个方法」→ 看末尾的 `code_verbs` / `code_abil` 两列。
    return "通用管线（全池同值，不是这张卡专属；逐卡入口见 code_verbs / code_abil）: " + " ; ".join(out)


def row_of(D, rec, clips, bulk_crit):
    rec["_clips"] = clips
    return {
        "id": rec["id"],
        "name": rec["name"],
        "nameZh": rec["nameZh"] or tri("查不到", "cards_engine.nameZh 空"),
        "faction": rec["faction"],
        "subtype": rec["subtype"],
        "cardType": rec["cardType"],
        "rarity": rec["rarity"],
        "cost": rec["cost"],
        "melee": rec["melee"],
        "ranged": rec["ranged"],
        "armour": rec["armour"],
        "health": rec["health"],
        "keywords": ",".join(rec["keywords"]) or "无",
        "traits": ",".join(rec["traits"]) or "无",
        "desc": one_line(rec["desc"], CELL_CAP),
        "descZh": one_line(rec["descZh"], CELL_CAP),
        "abilities": one_line_c(abilities_cell(rec)),
        "vfx": one_line_c(vfx_cell(rec)),
        "anim": one_line_c(anim_cell(rec)),
        "voice": one_line_c(voice_cell(rec)),
        "code_entry": one_line_c(code_entry_cell(D)),
        "code_special": one_line_c(" ; ".join("%s:%d %s" % (f, l, t) for f, l, t in rec["special"])
                                 or "无:RuleEngine/{Core,Data} 的 116 个 .cs 里没有以字面量引用该 id 的行"),
        "criteria_docs": one_line_c(",".join(rec["crit_docs"]) or
                                    ("无:判据/ 除了全池表（%d 份）之外没有出现该 id 字面量的专项文档" % len(bulk_crit))),
        "verify": one_line_c(verify_tsv_cell(rec)),
        "gaps": one_line_c(" ; ".join(rec["gaps"]) or "无"),
        "code_verbs": one_line_c(code_verbs_cell(rec["id"])),
        "code_abil": one_line_c(code_abil_cell(rec, D.trig)),
    }


# ---------------------------------------------------------------- 单卡人读输出
def print_card(D, cid):
    rec = build(D, cid)
    clips = D.clips
    line = "═" * 78
    print(line)
    print("%s · %s · %s" % (rec["id"], rec["name"], rec["nameZh"] or "（中文名查不到）"))
    print(line)

    print("\n【1 身份】 数据源 = MyGame/Assets/RuleEngine/Resources/cards_engine.json:cards[%s]"
          % D.engine_pos.get(cid, "?"))
    print("  阵营=%s  兵种=%s  卡类=%s  稀有度=%s" % (rec["faction"], rec["subtype"], rec["cardType"], rec["rarity"]))
    print("  费用=%s  近战=%s  远程=%s  护甲=%s  生命=%s" % (rec["cost"], rec["melee"], rec["ranged"], rec["armour"], rec["health"]))
    print("  关键词=%s" % (",".join(rec["keywords"]) or "无"))
    print("  traits=%s" % (",".join(rec["traits"]) or "无"))
    print("  desc  = %s" % one_line(rec["desc"], 300))
    print("  descZh= %s" % one_line(rec["descZh"], 300))

    print("\n【2 能力】 数据源 = cards_truth.json:cards[\"%s\"] 的 abilities/activeAbility/alternativeAbility" % cid)
    if not rec["abilities"]:
        print("  " + (rec["gaps"][-1] if rec["gaps"] else "无"))
    for head, ops in rec["abilities"]:
        print("  ▸ " + head)
        for op in ops:
            print("      - " + op)

    print("\n【3 特效】 数据源 = CardPresentation/Resources/card_vfx_by_card.json:cards[id=%s].events" % cid)
    print("  状态：%s" % rec["vfx_state"])
    if rec["vfx_state"] == "有":
        rows = {(r[2], r[3]): r for r in rec["vfx_rows"]}
        for ev, n, conf, anim in rec["vfx"]:
            r = rows.get((ev, n))
            st = ("在工程=%s 在消费表=%s 参数=%s 出处=%s" % (r[4], r[5], r[7], r[8])) if r else "（全池事件覆盖_静态.tsv 无此事件行）"
            print("  %-14s → prefab %-34s [%s]" % (ev, n, conf))
            print("      %s" % st)
        if not rec["vfx_rows"]:
            print("  ⚠ 全池事件覆盖_静态.tsv 无该卡的特效行")
    else:
        print("  " + rec.get("vfx_note", ""))
    for np in rec["vfx_notplayed"]:
        print("  ⛔不播 %s→%s（outcome=%s）: %s" % (np.get("ev"), np.get("n"), np.get("outcome"), one_line(np.get("why"), 120)))

    print("\n【4 动画】 数据源 = cards_engine.json:cards[id=%s].animTimes + cardanim_clips.json" % cid)
    if not rec["anim"]:
        print("  " + rec.get("anim_note", ""))
    rows = {(r[2], r[3]): r for r in rec["anim_rows"]}   # 键 = (trigger 名 tn, prefab n)
    for a in rec["anim"]:
        a_name = a.get("a") or ""
        if a_name.startswith("Wait_"):
            clip = "纯延时占位（原版无 prefab/无 clip）"
        else:
            clip = "✓在 cardanim_clips.json" if a_name in clips else "✗不在 cardanim_clips.json"
        r = rows.get((a.get("tn") or "", (a.get("n") or "") or a_name))
        cov = ("覆盖表: 在工程=%s 在消费表=%s 参数=%s" % (r[4], r[5], r[7])) if r else "覆盖表: ⚠无此动画行"
        print("  tg%s(%s) → CardAnim %s [%s] prefab %s" % (a.get("tg"), a.get("tn"), a_name, clip, a.get("n")))
        print("      d=%s sd=%s s=%s m=%s e=%s u=%s t=%s · %s" %
              (a.get("d"), a.get("sd"), a.get("s"), a.get("m"), a.get("e"), a.get("u"), a.get("t"), cov))

    print("\n【5 语音】 数据源 = CardPresentation/Resources/voice_lines.json + 覆盖表(维=语音)")
    if rec["voice_state"] != "有":
        print("  " + rec.get("voice_note", ""))
    for l in rec["voice"]:
        f = l.get("file") or ""
        exists = "✓文件在" if f in D.vo_files else "✗文件不在 Resources/Art/audio/vo"
        print("  %-10s → %s [%s] src=%s so=%s" % (l.get("ev"), f, exists, l.get("src", ""), l.get("so", "—")))

    print("\n【6 代码落点】 行号现读（符号锚，见本工具 CODE_ENTRIES）")
    for label, relp, ln in D.code_entries():
        print("  %-30s %s:%s" % (label, relp, ln if ln else "❓符号没找到"))
    # 🆕 2026-11-03（`B121-WF`）：上面那批是**全池同值**的通用管线；这两行才是**这张卡自己**的入口。
    print("  ⤷ 这张卡的 desc 解析动词 → 结算方法：")
    print("     " + code_verbs_cell(rec["id"]))
    print("  ⤷ 逐 ability → 触发口（trigger 名现读原版 AbilityTrigger 枚举）：")
    print("     " + code_abil_cell(rec, D.trig))
    print("  专项特判：")
    if rec["special"]:
        for f, l, t in rec["special"]:
            print("    %s:%d  %s" % (f, l, t))
    else:
        print("    无（扫过 RuleEngine/Core + RuleEngine/Data 的 .cs，按「引号字面量恰好等于该 id」匹配）")
    if rec["bulk_tables"]:
        print("    （整表排除的批量数据表：%s）" % ", ".join(rec["bulk_tables"]))

    print("\n【7 判据文档】 判据/ 里出现该 id 字面量的**专项**文件（全池表已单列）")
    if rec["crit_docs"]:
        for f in rec["crit_docs"][:40]:
            print("    " + f)
        if len(rec["crit_docs"]) > 40:
            print("    …共 %d 份" % len(rec["crit_docs"]))
    else:
        print("    无（扫过 判据/ 764 个 .md/.tsv/.txt，按 id 字面量精确匹配）")
    bulkc = D.criteria_bulk()
    if bulkc:
        print("    全池表（提到全池、不算这张卡的专项）：%s" % ", ".join(bulkc))

    print("\n【8 四维核验】 数据源 = 判据/全卡核验_判定.tsv")
    print("  " + verify_tsv_cell(rec))

    print("\n【9 三态体检】")
    print("  特效=%s  动画=%s  语音=%s" % (rec["vfx_state"], rec["anim_state"], rec["voice_state"]))
    if rec["gaps"]:
        for g in rec["gaps"]:
            print("  ⚠ " + g)
    print("")


# ---------------------------------------------------------------- 全池
def header_lines(D):
    ev_mtime = None
    try:
        import datetime
        ev_mtime = datetime.datetime.fromtimestamp(os.path.getmtime(P_EVT)).strftime("%Y-%m-%d %H:%M")
    except Exception:
        ev_mtime = "?"
    return [
        "# 卡牌总索引 —— 由 工具/gen_card_dossier.py 生成，⛔ 别手改（重跑会覆盖）",
        "# 复跑：python -I -X utf8 d:/4/Unity/工具/gen_card_dossier.py --all --write",
        "# 数据源版本：cards_engine.json v%s count=%s | cards_truth.json count=%s generatedAt=%s"
        % (D.engine_raw.get("version"), D.engine_raw.get("count"),
           D.truth_meta.get("count"), one_line(D.truth_meta.get("generatedAt"), 40)),
        "#            card_vfx_by_card.json cardCount=%s entryCount=%s generatedAt=%s"
        % (D.vfx_meta.get("cardCount"), D.vfx_meta.get("entryCount"), one_line(D.vfx_meta.get("generatedAt"), 40)),
        "#            全池事件覆盖_静态.tsv mtime=%s（「在工程/在消费表/参数判定」列原样复用，本工具不重算）"
        % ev_mtime,
        "# 口径：本表以 cards_engine.json 的 **%s** 张为准（张数**现读 `count`**，⛔ 别写死；"
        "`全卡核验总表.tsv` 的骨架是历史快照，可能比它少）；"
        "⛔ 不用 cards_raw.json（每卡两份同构技能树，递归扫会翻倍）。" % (D.engine_raw.get("count"),),
        "# 消费：跳过 '#' 开头的行后，第一行是列名行；按列名取值（列名稳定，改动会同步报告）。",
        "# 判据全池表（≥%d 个不同卡 id 的文件，不算任何一张卡的专项文档，故 criteria_docs 列里不出现）：%s"
        % (BULK_TABLE_MIN, ", ".join(D.criteria_bulk()) or "无"),
        "# 代码批量数据表（同规则，code_special 列里不出现）：%s" % (", ".join(D.bulk_tables) or "无"),
        "# 🆕 逐卡效果入口两列（2026-11-03 B121-WF）：`code_verbs` = 该卡 desc 的**解析动词 → EffectDispatch handler:行**"
        "（来源 ruleprobe 解析基线 %s，%s 张 · mtime %s）；"
        "`code_abil` = 每个 ability 的 trigger/载荷 → **我们的触发口**（`cards_truth.json` 是 SO 原始数据、**不是运行时数据**，"
        "运行时吃 desc 文本）⇒ 想知道「效果在哪个方法」看 code_verbs，想知道「什么时候触发」看 code_abil。"
        % (rel(P_PARSE_BASE), _PARSE_META.get("n", "?"), _PARSE_META.get("mtime", "?")),
    ]


def gen_all(D):
    clips = D.clips
    bulk_crit = D.criteria_bulk()
    rows = [row_of(D, build(D, c["id"]), clips, bulk_crit) for c in D.engine]
    return rows


def tsv_text(D, rows):
    out = list(header_lines(D))
    out.append("\t".join(COLS))
    for r in rows:
        out.append("\t".join(str(r.get(c, "")) for c in COLS))
    return "\n".join(out) + "\n"


def _state(cell):
    """从 `有(...):` / `无:...` / `查不到:...` 里取三态档。"""
    for s in ("查不到", "有", "无"):
        if cell.startswith(s):
            return s
    return "?"


def coverage_stats(D, rows):
    st = Counter()
    for r in rows:
        st["vfx_" + _state(r["vfx"])] += 1
        st["anim_" + _state(r["anim"])] += 1
        st["voice_" + _state(r["voice"])] += 1
        st["abilities_" + _state(r["abilities"])] += 1
    return st


def gen_json(D, rows):
    cards = []
    for r in rows:
        cards.append({k: r[k] for k in COLS})
    return {
        "generatedBy": "d:/4/Unity/工具/gen_card_dossier.py",
        "sources": {
            "engine": rel(P_ENGINE), "engineVersion": D.engine_raw.get("version"),
            "engineCount": D.engine_raw.get("count"),
            "truth": rel(P_TRUTH), "truthCount": D.truth_meta.get("count"),
            "vfx": rel(P_VFX), "clips": rel(P_CLIPS), "voice": rel(P_VOICE),
            "eventCoverage": rel(P_EVT), "verify": rel(P_VERIFY),
        },
        "columns": COLS,
        "count": len(cards),
        "cards": cards,
    }


# ---------------------------------------------------------------- main
def print_usage(code):
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print(USAGE)
    sys.exit(code)


def main(argv):
    if not argv:
        print_usage(2)
    if argv[0] in ("-h", "--help"):
        print_usage(0)

    card = None
    all_ = False
    kwindex = False
    mode = None
    out_path = None
    do_json = False
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == "--card":
            i += 1
            if i >= len(argv):
                print_usage(2)
            card = argv[i]
        elif a == "--all":
            all_ = True
        elif a == "--kwindex":
            kwindex = True
        elif a in ("--dry", "--check", "--write"):
            if mode is not None:
                print("⛔ 只能给一个动作档（%s / %s）" % (mode, a))
                print_usage(2)
            mode = a
        elif a == "--json":
            do_json = True
        elif a == "--out":
            i += 1
            if i >= len(argv):
                print_usage(2)
            out_path = argv[i]
        else:
            print("⛔ 不认识的参数：%s" % a)
            print_usage(2)
        i += 1

    # `--kwindex`：关键词/效果词索引（与 `--all` 同一个动作档语义；⛔ 不跑的话它一行都不做）
    if kwindex:
        if card or all_ or do_json:
            print("⛔ --kwindex 不能和 --card / --all / --json 混用（一个是关键词表，一个是卡表）")
            print_usage(2)
        if mode is None:
            print_usage(2)
        return run_kwindex(mode)

    D = Data()

    # 单卡
    if card:
        if card in D.eidx:
            cid = card
        else:   # 名字兜底（精确、忽略大小写）
            hit = [c["id"] for c in D.engine if (c.get("name") or "").lower() == card.lower()]
            if not hit:
                print("⛔ 卡池里没有 id 或英文名 = %r 的卡（现池 %d 张；名字匹配为精确、忽略大小写）" % (card, len(D.engine)))
                sys.exit(1)
            cid = hit[0]
        print_card(D, cid)
        return 0

    if not (all_ or do_json):
        print_usage(2)

    rows = gen_all(D)

    if do_json:
        payload = gen_json(D, rows)
        text = json.dumps(payload, ensure_ascii=False, indent=1)
        if out_path:
            d = os.path.dirname(os.path.abspath(out_path))
            if d and not os.path.isdir(d):
                os.makedirs(d)
            io.open(out_path, "w", encoding="utf-8", newline="\n").write(text + "\n")
            print("✅ 已写 %s（%d 张卡 · %d 列）" % (out_path, len(rows), len(COLS)))
        else:
            print(text)
        if mode is None:
            return 0

    if mode is None:
        print_usage(2)

    text = tsv_text(D, rows)

    if mode == "--dry":
        stats = coverage_stats(D, rows)
        print("卡数 = %d（cards_engine.json v%s）" % (len(rows), D.engine_raw.get("version")))
        for dim in ("vfx", "anim", "voice"):
            print("  %-5s 有=%-5d 无=%-5d 查不到=%d" % (dim, stats[dim + "_有"], stats[dim + "_无"], stats[dim + "_查不到"]))
        print("  能力   有=%d 无=%d" % (stats["abilities_有"], stats["abilities_无"]))
        # 🆕 逐卡入口两列的覆盖（2026-11-03 B121-WF）
        cv_hit = sum(1 for r in rows if str(r.get("code_verbs", "")).startswith("有("))
        cv_none = sum(1 for r in rows if str(r.get("code_verbs", "")).startswith("无:"))
        cv_miss = sum(1 for r in rows if str(r.get("code_verbs", "")).startswith("查不到:"))
        # `TRANSFORM_<FACTION>` 的 13 张：报 `warlordtransform→…`（既不是「有(」也不是「无:」）
        # ⇒ 单列一档，⛔ 别让这 13 张在三档合计里静默消失（有+无+查不到 ≠ 卡数）。
        cv_tf = len(rows) - cv_hit - cv_none - cv_miss
        print("  code_verbs 有=%d 无(白板)=%d 查不到(基线无此卡)=%d 变身(TRANSFORM_*)=%d  基线 mtime=%s"
              % (cv_hit, cv_none, cv_miss, cv_tf, _PARSE_META.get("mtime", "?")))
        ab_hit = sum(1 for r in rows if str(r.get("code_abil", "")).startswith("有("))
        print("  code_abil  有=%d（逐 ability → 触发口）" % ab_hit)
        print("  枚举名缺失: %s" % (",".join(D.enum_missing) or "无（5 个原版枚举全部现读成功）"))
        print("  TSV 行数 = %d（含 %d 行 '#' 元信息 + 1 行列名）" % (text.count("\n"), len(header_lines(D))))
        print("  零写盘（--dry）")
        return 0

    if mode == "--check":
        if not os.path.exists(P_OUT):
            print("❌ 盘上没有 %s —— 先跑 --all --write" % rel(P_OUT))
            return 1
        cur = io.open(P_OUT, "r", encoding="utf-8", newline="").read()
        if cur == text:
            print("✅ 逐字节相同（%s）" % rel(P_OUT))
            return 0
        import difflib
        d = list(difflib.unified_diff(cur.split("\n"), text.split("\n"),
                                      fromfile="盘上", tofile="本次生成", lineterm="", n=0))
        print("❌ 不同（盘上 vs 本次生成）：共 %d 行差异，前 40 行：" % len(d))
        for ln in d[:40]:
            print("   " + one_line(ln, 240))
        return 1

    # --write
    io.open(P_OUT, "w", encoding="utf-8", newline="\n").write(text)
    print("✅ 已写 %s（%d 张卡 · %d 列 · %d 行）" % (rel(P_OUT), len(rows), len(COLS), text.count("\n")))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
