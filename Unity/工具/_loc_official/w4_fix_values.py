# -*- coding: utf-8 -*-
"""W4：对 `Core/Loc.cs` 做「换成官方值」的定点替换（每条都先 assert 旧串在、且只出现一次）。

⛔ 不用 `sed -i`（会把行尾翻成 LF）；本脚本用 `wb` 写、并先算出完整内容再落盘。
⛔ 不写行号锚点 —— 每条按**整行内容**匹配（铁律：脚本里别写死行号）。
用法：python -I -X utf8 工具/_loc_official/w4_fix_values.py [--write]
"""
import io
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import w4_lib as L  # noqa: E402

DO = "--write" in sys.argv

# (旧串, 新串, 说明)
EDITS = [
    # ---- ① 优先级 3 的「6 条：Loc 中文还不是官方值」----
    ('{ "Settings/Support/FAQText",          new Entry("对游戏有疑问？请先看常见问题解答",\n'
     '                                                               "Questions about the game? Visit the Frequent Asked Questions") },',
     '{ "Settings/Support/FAQText",          new Entry("对游戏有疑问？请访问常见问题解答",\n'
     '                                                               "Questions about the game? Visit the Frequent Asked Questions") },',
     "Settings/Support/FAQText ZH -> 官方「请访问」"),

    ('{ "Settings/Support/ContactText",      new Entry("也可以用 support@everguild.com 联系我们\\n我们会尽力帮你！",\n'
     '                                                               "You can also contact us at support@everguild.com\\nWe\'ll do our best to help you!") },',
     '{ "Settings/Support/ContactText",      new Entry("您也可以通过support@everguild.com与我们联系 我们将尽力帮助您！",\n'
     '                                                               "You can also contact us at support@everguild.com\\nWe\'ll do our best to help you!") },',
     "Settings/Support/ContactText ZH -> 官方"),

    ('{ "Settings/Support/FAQTextMobile",    new Entry("对游戏有疑问？请看常见问题解答，或联系客服",\n'
     '                                                               "Questions about the game? Check out the Frequent Asked Questions or contact Support") },',
     '{ "Settings/Support/FAQTextMobile",    new Entry("对游戏有疑问？查看常见问题或联系客服",\n'
     '                                                               "Questions about the game? Check out the Frequent Asked Questions or contact Support") },',
     "Settings/Support/FAQTextMobile ZH -> 官方"),

    ('{ "MenuShop/ExtraLegendaryWarning", new Entry("你已经有 1 张传奇品质的这一件了。\\n确定还要再买一张吗？",\n'
     '                                                          "You already own 1 legendary copy of this item.\\nAre you sure you want to buy another one?") },',
     '{ "MenuShop/ExtraLegendaryWarning", new Entry("请记住，您的卡组中只能使用每张传奇牌的一份副本。您仍然可以创建此张牌的更多副本，但它们只能用于升级该张牌。您想要继续吗？",\n'
     '                                                          "You can only use one copy of each legendary card in your deck. You can buy or craft more copies of this card, but they are only usable to level up the card. Do you wish to continue?") },',
     "MenuShop/ExtraLegendaryWarning 两列 -> 官方（🔴 语义完全不同：原来那句是「已有 1 张」，官方是「每张只能用 1 份」）"),

    ('{ "Battle/AlliancePanel/NotInAnAlliance",  new Entry("这名玩家还没有加入任何联盟",\n'
     '                                                                  "This player is is still not part of an Alliance") },',
     '{ "Battle/AlliancePanel/NotInAnAlliance",  new Entry("该玩家仍未加入联盟",\n'
     '                                                                  "This player is still not part of an Alliance") },',
     "Battle/AlliancePanel/NotInAnAlliance -> 官方（EN 里那个重复的 `is is` 也一起修掉）"),

    # ---- ② 同族里「我们有值、与官方不同」的那 17 条 ----
    ('{ "Battle/HUD/CardsInHand",            new Entry("手牌", "Cards left") },   // EN = 节点静态 TMP；中文**自拟**',
     '{ "Battle/HUD/CardsInHand",            new Entry("手牌", "Cards in hand") },   // 🔴 2026-10-14（`A1803`）EN 改官方；'
     '原写 `Cards left` = **与同族 `CardsLeft` 抄串了**（节点静态 TMP 是 `Cards left: XX`，但**词条真值**才是权威）',
     "Battle/HUD/CardsInHand EN -> 官方 `Cards in hand`（原值是同族 CardsLeft 的抄串）"),

    ('{ "Battle/HUD/EndTurn",                new Entry("结束回合", "END TURN") },     // ZH = `zh_CN.csv:34`（`END TURN,~,结束回合`，**精确命中**）',
     '{ "Battle/HUD/EndTurn",                new Entry("结束回合", "End Turn") },     // 🔴 2026-10-14（`A1803`）EN 改官方；'
     '⚠️ 卡面那条路走 `CardText.Phrase("END TURN")`（**英文档照原样回大写原文**）⇒ 改这里不影响卡面/`END TURN` 钮的断言',
     "Battle/HUD/EndTurn EN -> 官方 `End Turn`"),

    ('{ "Battle/HUD/EnemyTurn",              new Entry("敌方回合", "ENEMY TURN") },   // EN/ZH **都自拟**（`zh_CN.csv:446` 是**近邻**：`Enemy turn...,~,敌方回合……`）',
     '{ "Battle/HUD/EnemyTurn",              new Entry("敌方回合", "Enemy turn") },   // 🔴 2026-10-14（`A1803`）**EN 与 ZH 都改成官方值**'
     '（原注写「EN/ZH 都自拟」—— 官方 zh-CN 表里 ZH 逐字就是「敌方回合」，故 ZH 未动）',
     "Battle/HUD/EnemyTurn EN -> 官方 `Enemy turn`"),

    ('{ "Battle/HUD/YourTurn",               new Entry("你的回合", "YOUR TURN") },    // ZH = `zh_CN.csv:450`（`Your turn,~,你的回合`，**值相同**、键大小写不同 ⇒ 近邻）；EN 自拟（键名本地读得到、值在远端）',
     '{ "Battle/HUD/YourTurn",               new Entry("你的回合", "Your turn") },    // 🔴 2026-10-14（`A1803`）EN 改官方'
     '（原注「值在远端」**作废** —— 官方 zh-CN 表已到手，两列都逐字对上了）',
     "Battle/HUD/YourTurn EN -> 官方 `Your turn`"),

    ('{ "Battle/Tips/DragToTarget",          new Entry("拖动到有效目标", "Drag to a target") },',
     '{ "Battle/Tips/DragToTarget",          new Entry("拖动到有效目标", "Drag to a valid target") },   // 🔴 EN 2026-10-14（`A1803`）改官方',
     "Battle/Tips/DragToTarget EN -> 官方 `Drag to a valid target`"),

    ('{ "Battle/Tips/UnitNotReady",          new Entry("单位未准备好", "This unit is not ready") },',
     '{ "Battle/Tips/UnitNotReady",          new Entry("单位未准备好", "Unit isn\'t ready") },   // 🔴 EN 2026-10-14（`A1803`）改官方',
     "Battle/Tips/UnitNotReady EN -> 官方 `Unit isn't ready`"),

    ('{ "Battle/Tips/HealthPoints",          new Entry("生命值",   "Health Points") },// TMP 原文；中文**自拟**（csv 无 `Health Points`；`Health` 那条 csv:31 是另一个串）// TMP 原文；中文**自拟**（csv 无 `Health Points`；`Health` 那条 csv:31 是另一个串）',
     '{ "Battle/Tips/HealthPoints",          new Entry("生命点数", "Health Points") },   // TMP 原文；🔴 ZH 2026-10-14（`A1803`）改官方'
     '（原「中文自拟」作废）',
     "Battle/Tips/HealthPoints ZH -> 官方「生命点数」"),

    ('{ "Battle/Tips/HandFull",              new Entry("手牌已满", "Hand full - the drawn card goes to the discard pile") },',
     '{ "Battle/Tips/HandFull",              new Entry("手牌已满", "Hand is full") },   // 🔴 EN 2026-10-14（`A1803`）改官方；'
     '⚠️ 我们自写的那句「…goes to the discard pile」现在只剩 `BattleDriver.HandFullText()` 里的兜底副本（那份要跟着改）',
     "Battle/Tips/HandFull EN -> 官方 `Hand is full`"),

    ('{ "Battle/Effect/ChangeMeleeAttackOneTurn",  new Entry("获得 {0} 近战攻击一回合",  "{0} Melee Attack") },',
     '{ "Battle/Effect/ChangeMeleeAttackOneTurn",  new Entry("获得 {0} 近战攻击一回合",  "Get {0} Melee Attack one turn") },   // 🔴 EN 2026-10-14（`A1803`）改官方',
     "Battle/Effect/ChangeMeleeAttackOneTurn EN -> 官方"),

    ('{ "Battle/Effect/ChangeRangedAttackOneTurn", new Entry("获得 {0} 远程攻击一回合",  "{0} Ranged Attack") },',
     '{ "Battle/Effect/ChangeRangedAttackOneTurn", new Entry("获得 {0} 远程攻击一回合",  "Get {0} Ranged Attack one turn") },   // 🔴 EN 2026-10-14（`A1803`）改官方',
     "Battle/Effect/ChangeRangedAttackOneTurn EN -> 官方"),

    ('{ "Battle/Effect/ChangeHealthOneTurn",       new Entry("获得 {0} 生命值一回合",  "{0} Health") },',
     '{ "Battle/Effect/ChangeHealthOneTurn",       new Entry("获得 {0} 生命值一回合",  "Get {0} Health one turn") },   // 🔴 EN 2026-10-14（`A1803`）改官方',
     "Battle/Effect/ChangeHealthOneTurn EN -> 官方"),

    ('{ "Battle/Effect/ChangeArmourOneTurn",       new Entry("获得 {0} <sprite name=Atlas_trait_icon_armour> 盔甲一回合",  "{0} Armour") },',
     '{ "Battle/Effect/ChangeArmourOneTurn",       new Entry("获得 {0} <sprite name=Atlas_trait_icon_armour> 盔甲一回合",  "Get {0} <sprite name=Atlas_trait_icon_armour> Armour one turn") },   // 🔴 EN 2026-10-14（`A1803`）改官方；'
     '那个 `<sprite>` 名在 TMP 图集里**存在**（`Resources/Fonts/Warpforge Trait TextSprites.asset`）⇒ 能解析',
     "Battle/Effect/ChangeArmourOneTurn EN -> 官方（含 Atlas 精灵名）"),

    ('{ "Battle/Tips/NotEnoughMana",     new Entry("能量不足",                     "Not enough mana") },   // EN 自拟；ZH = `Describe(ErrCost)` 原话',
     '{ "Battle/Tips/NotEnoughMana",     new Entry("能量不足",                     "Not enough Energy") },   // 🔴 EN 2026-10-14（`A1803`）改官方'
     '（原「EN 自拟」作废；ZH 与官方逐字相同，未动）',
     "Battle/Tips/NotEnoughMana EN -> 官方 `Not enough Energy`"),

    ('{ "Battle/Tips/NoTargetAvailable", new Entry("没有有效的目标", "No valid target for this tactic") }, // EN 自拟；ZH = `Describe(ErrNoTargetAvailable)` 原话',
     '{ "Battle/Tips/NoTargetAvailable", new Entry("没有有效的目标", "No valid target available") }, // 🔴 EN 2026-10-14（`A1803`）改官方；原「EN 自拟」作废',
     "Battle/Tips/NoTargetAvailable EN -> 官方"),

    ('{ "Battle/Tips/NotEnoughRoom",     new Entry("战场上没有空位",           "No room left on the board") }, // EN 自拟；ZH = `Describe(ErrNotEnoughRoom)` 原话',
     '{ "Battle/Tips/NotEnoughRoom",     new Entry("战场上没有空位",           "No room on the battlefield") }, // 🔴 EN 2026-10-14（`A1803`）改官方；原「EN 自拟」作废',
     "Battle/Tips/NotEnoughRoom EN -> 官方"),

    ('{ "Battle/Chat/WellPlayed",                new Entry("打得好", "Well Played") },',
     '{ "Battle/Chat/WellPlayed",                new Entry("打得好", "Well played") },   // 🔴 EN 2026-10-14（`A1803`）改官方（小写 p）',
     "Battle/Chat/WellPlayed EN -> 官方 `Well played`"),

    # ---- ③ `Battle/Chat` 那 6 条的键名：官方是 `Greetings`（我们写 `Greet`）----
    ('{ "Battle/Chat/Greet",                     new Entry("问候", "Greet") },\n',
     '',
     "删掉写错键名的 `Battle/Chat/Greet`（官方键 = `Greetings`，已随 105 条那一块进表；"
     "⚠️ 钮上的标签今天走 `ChatPopupPanel` 里那颗**硬编码数组**，⛔ 还没接本表 —— 见 W4 报告）"),
]

ADD_BLOCK = """            // ============================================================ 🆕 **2026-10-14（第二十八会话 · `A1803`）：
            //   「代码引用了、表里没值」那 10 条（原账写 12 条，逐条现核后是 10 条 —— 见下）**
            //
            // 🔴 **现核订正（铁律 5）**：账 `I2_全表可挖盘点.md:103-107` 写「代码**已经引用**了这个键、表里却没值 = 12 条」，
            //   逐条 grep 之后**不成立**：那 12 条里
            //     · `Battle/Tips/DamageFatigue` —— **真有活引用**（`BattleDriver.DamageFatigueTerm`）⇒ 已随上面 `Battle/` 那 105 条一起进表；
            //     · `MainMenu/PurchasePremium/Description` —— **本来就在表里**（本表 `:1624`，**多行写法** ⇒
            //       上一轮那个「表里没有」的普查脚本按单行正则扫的，**漏读了它**）；
            //     · 剩下 **10 条**：全仓 `grep` 下来，它们**只出现在文档注释里**
            //       （`TutorialModePopup.cs:182` / `ForgeTab.cs:549` / `CampaignRewardWindow.cs:42` /
            //        `ForgeData.cs:18,168` / `ItemDrawer.cs:940` / `BoosterInfoPopup.cs:532` /
            //        `ReferralPopupWindow.cs:44-45` / `DailyData.cs:1083`）——
            //       **一个运行期取值点都没有**（`grep '"<键>"' Assets --include=*.cs` 逐条现读）。
            //   ⇒ 本块的口径 = **「值备好」**：键与两列都照官方表逐字录入，⛔ **不是「已接」**
            //     （接线要动上面那些文件，⛔ 不在本笔白名单）。
            //   ⚠️ 副作用：其中 `ForgeTab.HelpTipBody = " "`（**故意留空**，配一条 `ShownBody.Trim() == ""` 的断言）
            //     一旦改成读本表，那条断言会红 —— 接线时一起办。
            // 判据逐条 → `资料/普查产出_第二十八会话/W4_官方文案族.md`。
            { "Demo/Tutorial",                          new Entry("教程", "Tutorial") },   // 原版 `String.Format(词条, 完成数, 总关数)`
            { "MainMenu/Forge/Help",                    new Entry("在你的收藏中升级张牌，通过每个军队的锻造厂轨迹进行进步并获得大奖。", "Upgrade cards in your Collection to progress through the Forge track for each faction and earn great rewards.") },
            { "MainMenu/General/Claim",                 new Entry("索取", "Claim") },
            { "MainMenu/Level",                         new Entry("等级", "Level") },      // 原版拼法 `"{0} {1}/{2}"` 套它
            { "MainMenu/RewardWindow/AlreadyOwned",     new Entry("已拥有", "Already Owned") },
            { "MenuShop/BoosterInfo/LegendaryTooltip",  new Entry("在最多每30个打开的增强包中，都保证有一张传奇张牌。", "A legendary card is guaranteed in at most every 30 opened booster packs.") },
            { "MenuShop/referral/counter",              new Entry("你已收集了{0}个推荐奖励！", "You have collected {0} referral rewards!") },
            { "MenuShop/referral/counterMaximum",       new Entry("您已收集到最多的推荐奖励。", "You have collected the maximum number of referral rewards.") },
            { "Missions/CollectingRewards",             new Entry("正在收集奖励...", "Collecting Rewards...") },
            { "Rewards Menu/Claimed",                   new Entry("已认领", "Claimed") },
"""


def main():
    p = L.LOC
    data = io.open(p, "rb").read()
    assert data.count(b"\r\n") == 0, "Loc.cs 不是纯 LF —— 停下"
    s = data.decode("utf-8")
    n = 0
    for old, new, why in EDITS:
        c = s.count(old)
        if c != 1:
            raise SystemExit("❌ 命中 %d 次（应为 1）：%s\n--- %r" % (c, why, old[:90]))
        s = s.replace(old, new)
        n += 1
        print("  ✓ %s" % why)

    # 追加那 10 条（插在 Table 的收尾 `};` 之前）
    tail = '            { "CardEffect/Sentence/Damage", new Entry("对{1}造成 {0} 点伤害", "Deal {0} damage to {1}") },\n'
    if s.count(tail) != 1:
        raise SystemExit("❌ 收尾锚点命中 %d 次" % s.count(tail))
    s = s.replace(tail, tail + ADD_BLOCK)
    n += 1
    print("  ✓ 追加 10 条「值备好」")

    out = s.encode("utf-8")
    if DO:
        io.open(p, "wb").write(out)
        print("落盘 %s（%d 处改动，%d -> %d 字节）" % (p, n, len(data), len(out)))
    else:
        print("（干跑，没写文件；%d 处改动）" % n)


if __name__ == "__main__":
    main()
