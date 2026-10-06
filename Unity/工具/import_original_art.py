#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""import_original_art.py — 把**原版美术**按我们的卡名/阵营拷进 Unity 工程（原版复刻用）

产出（全部落在 `MyGame/Assets/CardPresentation/Resources/Art/`）：

    cards/frame_<阵营小写>.png      阵营卡框（原版的**空框**，数值/名字由我们画在上面）
    cards/back_<阵营小写>.png       卡背（牌堆用）
    cards/art_<卡 id>.png           每张卡的立绘（原版插画，662×1024，干净无字）
    ⚠️ **2026-10-06 订正（铁律 5）**：这行原写 `art_<卡名小写下划线>.png` —— **2026-09-15 起实际按【id】命名**（`art_<id>`，依据本文件里那段「按 id 命名」的注释），文档没跟上。
    ui/<原切片名>.png               战斗 UI 图（HUD / 攻击方式按钮 / 选目标准星 / 高亮光圈）

⚠️ **版权** —— 🔴 **2026-09-22 更正：本节原来写「发布前整个 `Resources/Art/` 必须删掉」，那条口径已作废**
   （用户 2026-09-18：「取消掉什么版权红线，这是个人学习使用的」⇒ `CLAUDE.md` §四）。
   `Resources/Art/` 照旧进 `.gitignore`，但**理由是「别把大件塞进 git」、不是版权**。
   兜底机制仍在（和版权无关）：`CardArt.Available` 判空 —— 删掉整个目录游戏照样能跑，退回程序生成的占位美术。

用法：
    python import_original_art.py            # 拷贝
    python import_original_art.py --check    # 只检查源文件在不在，不写

数据来源（2026-09-12 起**统一走新解包**）：
  · 卡框/卡背  `d:/2/新解包资源/assets_full/bundle_<包名>/Texture2D/`
  · 立绘       同上，按**卡名**在包里找；再按同名的 `Sprite/*.json` 里的 `textureRect` **裁成 sprite**
               —— 原版画的就是这块（纹理 1024²、sprite 670.5×1024），不裁的话四周是空白/别的东西
  · 卡背       仍然是 `d:/4/Unity/素材/Warpforge原版/卡背/`（那批是切好的，新解包里没有）

⚠️ **为什么不再用 `d:/2/解包整理/`**：那边有 650 张同名图内容不对（卡的插图被裁成 660×1024、
   而且没有 sprite 的 `textureRect`）。见 `资料/资源使用手册.md` 顶部那条更正。
⚠️ **插图 alpha —— 2026-09-13 更正：原来写「是解码残渣、统一写 255」是错的。**
   真相反过来了：**alpha 就是角色的抠图轮廓**，是原版卡面立体感的关键。
   · 单位卡：alpha 有 91–97% 是透明的，亮的那块**正好是角色的形状**（光环/肩甲/武器/旗帜）
     —— 用户 2026-09-13 指出「角色的一部分越出卡框、但仍在方形画布里」，查实就是这个通道做的
   · 战术卡：alpha 全不透明（整幅矩形插画）→ 没有越界效果
   原版画法：**同一张贴图用两次** —— 底层忽略 alpha（完整插图，垫在卡框下、补上拱窗里的背景），
   前景层用真 alpha（角色，盖在**卡框上面**）⇒ 角色越出卡框。见 `Shaders/ArtOpaque.shader`。
   所以现在**原样保留 alpha**，并把「哪些卡有抠图」记进 `card_cutouts.json` 供运行时用。
"""
import argparse
import io
import os
import shutil
import sys

sys.stdout.reconfigure(encoding='utf-8')

UNPACK   = 'd:/2/新解包资源/assets_full'      # ← 解包资源的**唯一来源**（2026-09-12 起）
BACK_SRC = 'd:/4/Unity/素材/Warpforge原版/卡背'
OUT      = 'd:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/cards'


def card_bundle(faction: str) -> str:
    """我们的阵营名 → 新解包里的卡牌资源包目录。出处：`assets_full/` 下的目录名（13 个阵营各一个）。"""
    return {
        'Ultramarines':     'bundle_spacemarinesultramarinescardassets_assets_all',
        'Goff':             'bundle_orksgoffcardassets_assets_all',
        'SaimHann':         'bundle_aeldarisaimhanncardassets_assets_all',
        'Genestealers':     'bundle_genestealercultscardassets_assets_all',
        'DarkAngels':       'bundle_spacemarinesdarkangelscardassets_assets_all',
        'BlackLegion':      'bundle_chaosspacemarinesblacklegioncardassets_assets_all',
        'Sautekh':          'bundle_necronssautekhcardassets_assets_all',
        'TauEmpire':        'bundle_tauempirecardassets_assets_all',
        'Leviathan':        'bundle_tyranidsleviathancardassets_assets_all',
        'AstraMilitarum':   'bundle_astramilitarumcardassets_assets_all',
        'Sororitas':        'bundle_sororitascardassets_assets_all',
        'EmperorsChildren': 'bundle_chaosspacemarinesemperorschildrencardassets_assets_all',
        'SpaceWolves':      'bundle_spacemarinesspacewolvescardassets_assets_all',
    }.get(faction)


def frame_src(prefix: str) -> str:
    """原版卡框文件名 → 新解包里的完整路径。`prefix` 形如 `40k_Cardframe_troop_Ultramarines_tier1`。"""
    import glob as _glob
    hits = _glob.glob(f'{UNPACK}/*cardassets_assets_all/Texture2D/{prefix}.png')
    return hits[0] if hits else ''


# 稀有度 → 卡框 tier（**实测确认**，不是猜的）：
# 拿四张不同稀有度的原版卡面（Primaris Reiver=common / Suppressor=rare / Valtus=epic /
# Vico Therbeus=legendary，都在 `Warpforge部队卡片/Ultramarines/3部队/`）和四张 tier 框逐一比对 ——
# tier1 素框、tier2 加尖塔、tier3 加两侧骑士像、tier4 金蓝华丽，四个档次一一对上。
# 对照图：`资料/留档_排查证据/卡面组装_0912/tier_map.png`。
# 🔴 **原这里有一张 `RARITY_TIER = {...}` 表 —— 2026-09-18 已删。**
#    它是**死代码**：全仓**没有任何引用**（导出脚本对每个阵营**无条件导全部 4 档**，
#    见下面 `RARITY_OF`/`_FRAME_OF` 那段），改它**不改变任何产物**。
#    ⚠️ 多处文档曾写「`CardArt.TierOf` 与 `RARITY_TIER` 是同一张表，**改要两边一起改**」——
#    **那条是错的**，照它改等于只改了个没人读的常量。**生效路径只有 `CardArt.TierOf()` 一处。**
# 🔴 另外 `special` 那一档**不能按 rarity 查表**：2026-09-14 逐张开卡图实测（39 张，
#    两套独立方法 19/19 一致）判出 `tier1` 24 / `tier2` 15，**差异落在阵营上**（= 印刷批次边界）。
#    落地在 `CardPresentation/Core/CardArt.cs` 的 `TierOf(rarity, faction)`；
#    出处 `资料/卡表核对_卡图提取/_裁定_special卡框.md` §二 / §2.1。

# 卡框：我们的阵营 → 原版哪个阵营的框。
#
# ⚠️ 原版卡框是**按 tier 分四张**的（tier1..4），稀有度说了算 —— 每档都导。
#    `CardArt.Frame(阵营, 稀有度)` 按稀有度取；取不到退回 tier1。
# ⚠️ 新解包里每个阵营还有一套 `_SDF` 变体（SoftMask 用的软边版），**我们没用**。
# ⚠️ `ember` / `tide` 是我们自己设计的两套卡，**借**了原版两个阵营的框（配色对得上）；
#    其余 11 个是原版阵营本身、用它们自己的框。
# 原版每个阵营有**两套**框：`troop`（部队/单位/督军）和 `stratagem`（战术/计策卡），各 4 档。
# 我们牌里 `type=tactic` 的走 stratagem 框（`CardArt.Frame(阵营, 稀有度, tactic: true)`）。
_FRAME_OF = {
    'ember':            ('40k_Cardframe_troop_BlackLegion_tier%d',       '40k_Cardframe_stratagem_BlackLegion_tier%d'),
    'tide':             ('40k_Cardframe_troop_SaimHann_tier%d',          '40k_Cardframe_stratagem_SaimHann_tier%d'),
    'ultramarines':     ('40k_Cardframe_troop_Ultramarines_tier%d',      '40k_Cardframe_stratagem_Ultramarines_tier%d'),
    'goff':             ('40k_Cardframe_troop_Orks_tier%d',              '40k_Cardframe_stratagem_Orks_tier%d'),
    'saimhann':         ('40k_Cardframe_troop_SaimHann_tier%d',          '40k_Cardframe_stratagem_SaimHann_tier%d'),
    'genestealers':     ('40k_Cardframe_troop_GSC_tier%d',               '40k_Cardframe_stratagem_GSC_tier%d'),
    'blacklegion':      ('40k_Cardframe_troop_BlackLegion_tier%d',       '40k_Cardframe_stratagem_BlackLegion_tier%d'),
    'sautekh':          ('40k_Cardframe_troop_Sautekh_tier%d',           '40k_Cardframe_stratagem_Sautekh_tier%d'),
    'tauempire':        ('40k_Cardframe_troop_Tau_tier%d',               '40k_Cardframe_stratagem_Tau_tier%d'),
    'leviathan':        ('40k_Cardframe_troop_Leviathan_tier%d',         '40k_Cardframe_stratagem_Leviathan_tier%d'),
    'sororitas':        ('40k_Cardframe_troop_Sororitas_tier%d',         '40k_Cardframe_stratagem_Sororitas_tier%d'),
    'darkangels':       ('40k_Cardframes_Troop_DarkAngels_tier%d',       '40k_Cardframes_stratagem_DarkAngels_tier%d'),
    'astramilitarum':   ('40k_Cardframes_Troop_Astra Militarum_tier%d',  '40k_Cardframes_stratagem_Astra Militarum_tier%d'),
    'emperorschildren': ('40k_Cardframes_Troop_Emperor Children_tier%d', '40k_Cardframes_stratagem_Emperor Children_tier%d'),
    'spacewolves':      ('40k_Cardframes_Troop_Space Wolves_tier%d',     '40k_Cardframes_stratagem_Space Wolves_tier%d'),
}

# (阵营, tier) → 源路径。tier1 额外存一份**不带 tier 后缀**的老名字（`frame_<阵营>.png`），
# 这样 `CardArt` 的旧取法在有 tier 的图之前也不会断。
FRAMES = {}
for _fac, (_troop, _strat) in _FRAME_OF.items():
    for _t in (1, 2, 3, 4):
        FRAMES[(_fac, _t)] = frame_src(_troop % _t)
        FRAMES[(_fac, _t, 'strat')] = frame_src(_strat % _t)

# 卡背
BACKS = {
    'ember': 'Cardback_BL_Premium_Eye of Horus_Main.png',
    'tide':  'Cardback_ASH_Asuryani Path_Main.png',
    # 原版阵营用它们自己的卡背（UM = Ultramarines，GOF = Goff）
    'ultramarines': 'Cardback_UM_Astartes_Main.png',
    'goff':         'Cardback_GOF_Presale_Goff_Main.png',
}

# 立绘：我们的卡名 → 原版立绘（前缀 + 名字）。**按角色挑的，纯占位**，换自己的画时覆盖同名文件即可
PORTRAITS = [
    # ---- Ember Legion → 黑色军团（Chaos Space Marines）----
    ('Ember Warlord',   'BlackLegion_黑色军团/Texture2D/CSM_BlackLegion_warlord_Ghallaron the Pious'),
    ('Scavenger',       'BlackLegion_黑色军团/Texture2D/CSM_BlackLegion_inf_Chaos Legionary'),
    ('Bulwark',         'BlackLegion_黑色军团/Texture2D/CSM_BlackLegion_inf_Plague Marine'),
    ('Falcon',          'BlackLegion_黑色军团/Texture2D/CSM_BlackLegion_inf_Warp Talon'),
    ('Ember Archer',    'BlackLegion_黑色军团/Texture2D/CSM_BlackLegion_inf_Meltagun Legionary'),
    ('Veteran',         'BlackLegion_黑色军团/Texture2D/CSM_BlackLegion_inf_Veteran Legionary'),
    ('Ironclad',        'BlackLegion_黑色军团/Texture2D/CSM_BlackLegion_inf_Chaos Terminator'),
    ('Longbowman',      'BlackLegion_黑色军团/Texture2D/CSM_BlackLegion_inf_Havoc'),
    ('Flamecaller',     'BlackLegion_黑色军团/Texture2D/CSM_BlackLegion_inf_Rubric Marine'),
    ('Shadowblade',     'BlackLegion_黑色军团/Texture2D/CSM_BlackLegion_inf_Chosen'),
    ('Battering Ram',   'BlackLegion_黑色军团/Texture2D/CSM_BlackLegion_veh_Accursed Helbrute'),
    ('War Drake',       'BlackLegion_黑色军团/Texture2D/CSM_BlackLegion_veh_Maulerfiend'),
    ('Molten Colossus', 'BlackLegion_黑色军团/Texture2D/CSM_BlackLegion_veh_Venomcrawler'),
    # ---- Tide Swarm → Saim-Hann（灵族）----
    ('Tide Warlord',  'Aeldari_灵族/Texture2D/Craftworld_SaimHann_warlord_Jain Zar'),
    ('Tide Minion',   'Aeldari_灵族/Texture2D/Craftworld_SaimHann_inf_Guardian Defender'),
    ('Wave Rider',    'Aeldari_灵族/Texture2D/Craftworld_SaimHann_veh_Windrider'),
    ('Reef Guard',    'Aeldari_灵族/Texture2D/Craftworld_SaimHann_inf_Wraithblade'),
    ('Siren',         'Aeldari_灵族/Texture2D/Craftworld_SaimHann_inf_Swooping Hawk'),
    ('Coral Archer',  'Aeldari_灵族/Texture2D/Craftworld_SaimHann_inf_Dire Avenger'),
    ('Shellback',     'Aeldari_灵族/Texture2D/Craftworld_SaimHann_inf_Wraithguard'),
    ('Ballista',      'Aeldari_灵族/Texture2D/Craftworld_SaimHann_inf_Weapons Platform'),
    ('Deep Hunter',   'Aeldari_灵族/Texture2D/Craftworld_SaimHann_inf_Striking Scorpion'),
    ('Iron Shell',    'Aeldari_灵族/Texture2D/Craftworld_SaimHann_inf_Vengeful Wraithblade'),
    ('Storm Priest',  'Aeldari_灵族/Texture2D/Craftworld_SaimHann_inf_Spiritseer'),
    ('Leviathan',     'Aeldari_灵族/Texture2D/Craftworld_SaimHann_monster_Avatar of Khaine'),
    ('Abyss Titan',   'Aeldari_灵族/Texture2D/Craftworld_SaimHann_veh_Wraithknight'),
]


# 卡面名 ↔ 游戏内贴图名**不一致的个例**（贴图名是原版自己起的，改不了）。
# 加一条 = 让匹配时**多试一个名字**；没有别名的卡走原名。
# ⚠️ **这张表按「卡名」查，所以对同名跨阵营的卡用不了** —— 那种情况走下面的 `ART_ID_ALIAS`。
ART_NAME_ALIAS = {
    "Fire Warrior Marksman": "Fire Warrior Sniper",   # Tau_Empire_inf_Fire Warrior Sniper.png
}


# 🔴 **按卡 `id` 点名的贴图表**（值 = `Texture2D/` 下的文件名，**不带 .png**）。
# 为什么单开一张按 id 的表：
#   ① 原版**改过名**，资产里留的是旧名 —— 卡表是新名，子串匹配自然落空
#      （`Dark Pact of Excess` 在原版里叫 `Mark of Chaos_Slaanesh`）；
#   ② 卡池里有 **5 组同名跨阵营**的卡（`Bladeguard Veteran` 在 UM 和 DA 各一张），
#      **按卡名补别名会把另一张一起改掉** —— 必须按 id 才能精确点。
# 命中 ⓪ 这一轮的卡**不受 `taken` 限制**，可以和别的卡**共用同一张图** ——
#   原版自己就这么干：`SM_SpaceWolves_inf_Aggressor.png` 与
#   `SM_SpaceWolves_inf_Blackmane Aggressor.png` **MD5 完全相同**（2026-09-15 实测）。
# 出处（逐张目视核对，含 `文件:行号` 级证据）：`资料/PnP卡图_逐张对账_0915.md` §五 / §六。
# ⚠️ 加条目之前**先验 PNG 和同目录 `Sprite/<同名>.json` 都在**，否则卡面会缺 rect 裁歪。
ART_ID_ALIAS = {
    # ---- 黑军团：4 张黑暗契约 + 天赋，原版叫「Mark of Chaos_<神>」/「Warmaster」 ----
    'BL16': 'CSM_BlackLegion_strat_Mark of Chaos_Tzeench',      # Dark Pact of Fate
    'BL18': 'CSM_BlackLegion_strat_Mark of Chaos_Khorne',       # Dark Pact of Blood
    'BL20': 'CSM_BlackLegion_strat_Mark of Chaos_Slaanesh',     # Dark Pact of Excess
    'BL22': 'CSM_BlackLegion_strat_Mark of Chaos_Nurgle',       # Dark Pact of Resilience
    'BL2':  'CSM_BlackLegion_strat_Warmaster',                  # Chosen of the Four
    # ---- 黑军团：这两张是**卡池重复行**把图抢走了（`BL69/BL71` 见对账文档 §六）----
    # ---- 黑军团：这两张的卡名拼写（`Hellfire` 双 l）和原版贴图名（`Helfire` 单 l）对不上 ----
    # ⚠️ 2026-09-15 改：原来这里点的是**卡池重复行** `BL_Helfire_Pit` / `BL_Helfire_Torch`；
    #    那两行已作为幽灵卡删掉（`card_stats.json` 的 `noise`），**存活的是编号型的 `BL69` / `BL71`**，
    #    所以点名表要跟着挪到它们身上，否则它们会掉到相似度兜底（0.96）去碰运气。
    'BL69': 'CSM_BlackLegion_strat_Helfire Torch',   # Hellfire Torch
    'BL71': 'CSM_BlackLegion_strat_Helfire Pit',     # Hellfire Pit
    # ---- 极限战士：原版把 Bladeguard Lieutenant 改名成了 Veteran ----
    'UM34': 'SM_UM_inf_Bladeguard Lieutenant',
    # ⚠️ 与现存卡 `Righteous Fury` **共用同一张图**（原版 00a/00b 两张天赋同一幅画，
    #    插图区指纹 2/256 vs 无关对照 90/256，2026-09-15 实测）
    'UM_Exemplary_Warrior': 'SM_UM_strat_Righteous Fury',
    'UM_Predator_Annihilator': 'SM_UM_veh_Predator Destructor',
    # ⚠️ 它的图在两个**重复行**之间抢：`Terminator`(UM82) 的子串也命中 `2nd Company Terminator`，
    #    按 id 钉死才稳（卡表把 2nd 改名成了 1st，贴图名留的还是 2nd）
    'UM83': 'SM_UM_inf_2nd Company Terminator',
    'AM_Lord_Commander': 'AstraMilitarum_strat_Righteous Gaze',
    'DA2': 'DarkAngels_strat_Exemplar of Hate',
    # ---- 帝皇之子：4 张全是原版旧名 ----
    'EC1':  'CSM_EmperorsChildren_warlord_Lord Exultant',
    # ⚠️ 别写成 `Threnodic Choir Flawless` 的裸子串：那个前缀还命中
    #    `..._inf_Threnodic Choir Flawless Blade.png`（另一张卡），这里是**全名点名**才安全
    'EC24': 'CSM_EmperorsChildren_inf_Threnodic Choir Flawless',
    'EC62': 'CSM_EmperorsChildren_strat_Tools of Torture',
    'EC63': 'CSM_EmperorsChildren_strat_Dark Prince´s Throne',  # ´ 是 U+00B4，原版就长这样
    # ---- 死灵：这两张都有**重复行**（`SAU_Awakening_Obelisk` / `TL_Sporecaster_Biostructure`）----
    'SAU61': 'Necron_Sautekh_strat_Annihilation Command',
    'SAU65': 'Necron_Sautekh_strat_Awakening Obelisk',
    'TL65':  'Tyranid_Leviathan_strat_Sporecaster Biostructure',
    # ---- 钛：按卡面改过名（`ART_NAME_ALIAS` 那条也覆盖它，这里按 id 再钉一遍）----
    'TAU23': 'Tau_Empire_inf_Fire Warrior Sniper',
    # ---- 修女会：**原版旧名和现名错开了一位**（2026-09-15 用户指出后实测）----
    #   贴图 `strat_Righteous Repugnance` 画的是「红头巾负伤修女 + 枪口焰」= 现卡 `Moment of Grace`（PnP 47）
    #   贴图 `strat_Purgator Mirabilis`    画的是「金色赎罪引擎」      = 现卡 `Righteous Repugnance`（PnP 06）
    # ⚠️ 教训：**贴图文件名是原版的旧名，不能拿它当现卡名的索引** —— 这正是那 21 张里 15 张「缺图」的根因。
    #   名字对不上时必须**开图看**（铁律 7），不能拿「没有同名文件」当成「原版没有这张图」。
    'SOR6':  'Sororitas_strat_Purgator Mirabilis',       # Righteous Repugnance（原名对应的是别人）
    'SOR47': 'Sororitas_strat_Righteous Repugnance',     # Moment of Grace（原名被 SOR6 占了）
    # ---- 星界军：**原版把这张督军画了两张图**，子串匹配分不出哪张是主立绘（2026-09-16）----
    #   目录里同时有 `AstraMilitarum_warlord_Ursula Creed.png`（灰金发、年老女性、持动力锤）
    #   与 `AA_HB_AstraMilitarum_war_Ursula Creed.png`（举剑嘶吼、扛 CADIA 旗的**另一个人**）。
    #   按卡名 `ursulacreed` 归一后**两张都是全命中**、排序键也**平分**（都是 `(0, 32)`），
    #   于是 `files` 里先出现的 `AA_HB_…` 赢了 —— 而那是**替身画**。
    #   判据：`D:/2/Warpforge部队卡片/AstraMilitarum/1督军/Warpforge_05_Ursula-Creed.png` 亲读
    #   （原版印的是持锤的老妇人），两张贴图逐处比对见 `资料/PnP卡图_逐张对账_0915.md` §六 B。
    'AM5':   'AstraMilitarum_warlord_Ursula Creed',
}


def slug(name: str) -> str:
    """'Ember Archer' → 'ember_archer'（和 C# 的 CardArt.Slug 保持一致）"""
    return ''.join(c if c.isalnum() else '_' for c in name.lower())


# ---- 战斗 UI 图（HUD / 攻击方式按钮 / 选目标准星）----------------------------
# 来源是**已经切好的图集切片**（`slice_battle_atlas.py` 的产物），不是原始 bundle ——
# 所以这里只做拷贝。要加新图：确认它已经在切片库里，然后把名字加进这张表。
# ⚠️ 目标文件名 = 切片库里的文件名，**不改**：`CardArt.Ui("...")` 按这个名字找。
UI_SRC = 'd:/4/Unity/素材/Warpforge原版/UI图集/图集/battleatlasui/sliced'
UI_OUT = 'd:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/ui'

# ---- 替代行动按钮（2026-09-25 加）------------------------------------------------
# 这五张**不在 `battleatlasui` 那个图集里**，走的是另一条来源：
#   `assets_full/bundle_battleprefabs_vfxandmisc_assets_all/Texture2D/Attack type button <X>.png`
#   —— 每张 **128×128 的独立贴图**（同目录 `Sprite/*.json` 的 `textureRect` 就是整张 0,0,128,128）。
# 为什么需要：原版**每个阵营的主动技能按钮**就是这个阵营的**替代行动**
#   （`Attack type button {Pray,Duty,Ferocity,Agenda,Oath}`），场景里那格 `m_Sprite` 是空的、
#   icon 由 `BattleCardUI` 运行时赋 —— 所以没有这五张时，有替代行动的卡只能亮我们挑的占位图。
# 阵营 ↔ 关键词映射见 `资料/特殊行动_五件_原版规格.md` §二。
# 命名同 `UI_IMAGES`：**名字里的空格换成下划线**，其余一字不动。
ALT_ACTION_SRC = 'd:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/Texture2D'
ALT_ACTION_IMAGES = [
    'Attack type button Pray', 'Attack type button Duty', 'Attack type button Ferocity',
    'Attack type button Agenda', 'Attack type button Oath',
]

UI_IMAGES = [
    # HUD
    'UI_Button_End_Turn_Normal_wide', 'UI_Button_End_Turn_Hover_wide', 'UI_Button_End_Turn_Pressed_wide',
    'UI_Energy_Eldar', 'UI_Player_Frame', 'UI_Deck_Background',
    '40k_battle_energy_empty', '40k_battle_energy_full',
    '40k_DeckHolder_light_green', '40k_DeckHolder_light_red',
    '40k_Combat_Icon_Cross',
    # 攻击方式选择器（原版 `Drag Attack Selector` 的六个按钮对象）
    'Attack_type_button_Melee', 'Attack_type_button_Ranged',
    'Attack_type_button_highlight', 'Attack_type_Generic_Foreground',
    # 选目标反馈：准星（原版 `NoCanvas2D/Attack Target Reticle/Crosshair` 的 sprite）
    # ⚠️ 原资产名拼错了（`Atack` 少个 t），**照抄别改** —— 改了就在切片库里找不到
    'Atack_Icon_BW',
    # 右侧能量区那一竖排的零件（2026-09-12 加，出处 dump `Energy And turn holder` 子树）
    'UI_Energy_Holder_big',          # 大底板 302.1×480.8（右侧出血 156 px）
    'UI_Quest_Points', 'UI_Quest_Points_Joint',   # 任务点 97.7×97.7 + 接线头 35.2×23.9
    'UI_PlayerFrame_TitleBackground',             # 名牌上那条标题底 311×42
    # 单位高亮光圈（原版 `Highlight` / `Highlight ranged` 用的图）
    '40K_melee_glow', '40K_ranged_glow',
    # 阵营资源（2026-09-13 第三十三轮，出处 dump `Energy And turn holder/{Player,Enemy}Mana` 子树）：
    #   · 信仰 `FaithHolder` 117.9×149.3（sprite `40k_Battle_Display_Faith`）+ 子 `FaithText`
    #   · 灵魂石 `SpiritStoneHolder` 112.1×116.6（sprite `UI_Energy_Eldar`；**这张早就在用**）
    #     + 子 `SpiritStone` 51.0×63.0（sprite `UI_Gem_Eldar`）+ 子 `SpiritStoneText`
    '40k_Battle_Display_Faith', 'UI_Gem_Eldar',
]

# ---- 关键词（trait）图标 —— **另一个图集**：`40ktraiticonatlas` ---------------------
# 2026-09-13 第三十二轮加。**为什么需要**：临时卡（Ephemeral）的卡面标记要它 ——
#   规则书 `:183`/`:229` 说临时卡「回合结束若在手牌则移除」，玩家得**看得出哪张是临时的**；
#   原版是 `BattleCardUI.ShowEphemeral()` + `Card2DController.ToggleGlitch()`（换 glitch 材质），
#   但 **glitch 素材本地没有**（`d:/2` 全盘 `*glitch*` 零命中）。
#   ⇒ 改用**原版真有的那张关键词图标**（比自绘的图形更接近原版，而且它本来就在本地）。
#
# 命名规律：切片文件名就是 `Atlas_trait_icon_<英文关键词>.png`，**共 78 个**（80×80 RGBA）。
# ⚠️ 全导（78 张，每张 ~10 KB）而不是只导 `ephemeral` 一张：这张表是**卡面组装的零件库**，
#   以后做「卡面上把关键词画成图标」时要用一整套（`资料/关键词图标/关键词与图标_对照表.md` 就是为它准备的）。
#   总量不到 1 MB，且在 `.gitignore` 里（原版美术不进仓库）。
# ---- 菜单 UI 图（阶段二「游戏外壳」，2026-09-22 加）--------------------------
# 来源 = **已经切好的图集切片缓存**（5557 张，按 bundle 分目录）：
#   `d:/2/Warpforge_tools/data/ui_extract/<bundle>/Sprite/<切片名>.png`
# 目标目录单独一个 `Resources/Art/ui_menu/` —— 战斗那批在 `ui/`、卡组那批在 `ui_deck/`，
# 三批来自**三个不同的图集**，混在一起会分不清谁是谁（和 `DeckUi` 分开的理由一样）。
# 命名约定同 `UI_IMAGES`：**切片名里的空格换成下划线**，其余一字不动 —— `CardArt.MenuUi("...")` 按这个名字找。
# 逐张的出处见 `资料/主菜单_原版规格.md` §二（表里 `Image[<图名>]` 那几列）。
MENU_SRC = 'd:/2/Warpforge_tools/data/ui_extract'
MENU_OUT = 'd:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/ui_menu'
MENU_IMAGES = [
    # (切片名, 它落在哪个 bundle 目录下)
    ('UI_Main_Upper bar',                     'atlasindividual_assets_0_mainmenu'),   # 顶栏底
    ('40K_notification',                      'atlasindividual_assets_0_mainmenu'),   # 收件箱
    ('40K_notification_number',               'duplicateassetisolation_assets_all'),  # 红点底
    ('40K_icon_feedback',                     'duplicateassetisolation_assets_all'),  # 反馈（出厂 inactive）
    ('40K_icon_duel',                         'atlasgroup_assets_all'),               # 挑战
    ('40k_main_player frame',                 'atlasindividual_assets_0_mainmenu'),   # 玩家信息块底
    ('40k_UI_bt_back',                         'atlasindividual_assets_0_mainmenu'),   # 每日奖励窗左下关闭圆钮（正本 §四）
    ('40k_topmarquee_currency_display BW',    'duplicateassetisolation_assets_all'),  # 名字条底
    ('40k_topmarquee_currency_gold',          'boosterpacks_assets_all'),             # 等级角标
    ('Avatar_UM_Intercessor',                 'cosmeticavatarsimages_assets_all'),    # 头像立绘
    ('Player_Avatar_selected',                'cosmeticavatarsimages_assets_all'),    # 头像选中（出厂 inactive）
    ('Smooth background lateral',             'duplicateassetisolation_assets_all'),  # 侧栏投影
    # ---- 🆕 2026-09-27：四个排行榜（`RankedRankingWindow` 那一族）的**页签图标** --------------
    # 判据 → `资料/普查产出_0927/排行榜_{遭遇战,经典,轮抽}.md`：三个全屏榜的 `Tab Buttons` 页签
    #   `Player` / `Armies` / `Alliances` 的 `Icon` 就是这两张 + `40K_Profile_icon_title`（已有）。
    # ⚠️ `40K_Chat_icon_Alliance v2` **名字里带空格** ⇒ 落盘名由 `name.replace(' ', '_')` 定
    #   （= `40K_Chat_icon_Alliance_v2`），与 `CardArt.MenuUi` 的查法一致。
    ('40K_Chat_icon_Global',                  'menus_assets_all_sprites'),            # Player 页签
    ('40K_Chat_icon_Alliance v2',             'menus_assets_all_sprites'),            # Alliances 页签
    ('40k_Separator Fade Sides Vertical',     'duplicateassetisolation_assets_all'),  # 侧栏分隔线
    ('40k_main_bt_play',                      'atlasindividual_assets_0_mainmenu'),   # 五个导航钮
    ('40k_main_bt_collection',                'atlasindividual_assets_0_mainmenu'),
    ('40k_main_bt_shop',                      'atlasindividual_assets_0_mainmenu'),
    ('40k_main_bt_rewards',                   'atlasindividual_assets_0_mainmenu'),
    ('40k_main_bt_friends',                   'atlasindividual_assets_0_mainmenu'),
    ('Closed-Chat_background',                'atlasindividual_assets_0_mainmenu'),   # 聊天预览底
    ('40K_icon_menu_chat',                    'atlasindividual_assets_0_mainmenu'),   # 聊天入口
    ('Tutorial Highlight',                    'duplicateassetisolation_assets_all'),  # 教程光罩（出厂 inactive）
    ('40K_menu_loading',                      'duplicateassetisolation_assets_all'),  # 载入转圈（`BlockingOverlay` 的 Spinner 要用）
    # 🆕 2026-09-24：卡组格的**金色选中框**（`MenuDraw.DeckCell` 一直在用它，但图从来没进 `Resources/`
    #   —— 所以收藏窗 Deck 页 / 选卡组窗的「选中」那一层一直是**静默不画**的。找茬子代理抓到）
    ('Highlight Rounded Square',              'liveopsicons_assets_all_sprites'),
    # 模式卡那一族（2026-09-22 补，结构见 `资料/主菜单_原版规格.md` §九）
    ('Container Image Tutorial',              'liveopsmenuimages_assets_all'),        # Tutorial 卡图
    ('Container Image Draft',                 'liveopsmenuimages_assets_all'),        # Draft 卡图
    ('WF_icon_clock for shader',              'liveopsicons_assets_all_sprites'),     # 倒计时时钟图标
    ('40k_square_border',                     'atlasindividual_assets_0_mainmenu'),   # 卡的外框（Sliced/ppu5/fillCenter=0）
    ('40k_Generic Smooth line',               'duplicateassetisolation_assets_all'),  # 通用分隔线（Sliced）
    ('UI_Army_Selection_Featured',            'atlasindividual_assets_0_mainmenu'),   # `Feature Badge`
    # 🆕 2026-10-07（A118②）：阵营格那三态 + 进度条填充。判据 = `bundle_menus_assets_all` 的
    #   `Ranked Army Selector Container V2`（原版 item prefab，`ArmySelectorRanked__Initialize.c:286` 实读）：
    #   `Background` 的 Image 用 `_Back`、悬停 `_Hover`、选中/按下 `_Pressed`（`EverguildToggle` 的
    #   `offSprite` / `m_HighlightedSprite` / `onSprite`+`m_PressedSprite`），进度条 `Fill` 用 `_Progression`。
    #   ⚠️ 四张**此前都不在 `Resources/`**（只有 `_Featured`）—— 不导的话 `MissingArt` 会响、那几层静默不画。
    ('UI_Army_Selection_Back',                'atlasindividual_assets_0_mainmenu'),   # 格底（`Background`）
    ('UI_Army_Selection_Back_Hover',          'atlasindividual_assets_0_mainmenu'),   # 悬停（`m_HighlightedSprite`）
    ('UI_Army_Selection_Back_Pressed',        'atlasindividual_assets_0_mainmenu'),   # 选中 / 按下
    ('UI_Army_Selection_Back_Progression',    'atlasindividual_assets_0_mainmenu'),   # 进度条填充（九宫 20,0,20,0）
    ('Rank Skull',                            'duplicateassetisolation_assets_all'),  # Draft 卡的 `Victory Counter Icon`
    ('40k_gamemode_icon_skirmish',            'armyicons_assets_all'),                # 遭遇战按钮图标
    # ---- 阶段二「日常」这一层（2026-09-23 加，出处 `资料/日常_原版规格.md` §九）----
    # ⚠️ 每张的**用在哪**见那份规格；下面只标「不查就不知道」的那几条。
    ('40K_missions_display_Daily vertical',    'atlasgroup_assets_all'),        # 每日任务**竖**卡底（登录卡 / 骷髅卡）
    ('40K_missions_display_Daily horizontal',  'atlasgroup_assets_all'),        # 每日任务**横**条底（三行那条）
    ('40K_missions_display_Weekly',            'atlasgroup_assets_all'),        # 周常条底
    #   ⚠️ 这张的 `m_PixelsToUnits` = **33.544**（全库唯一不是 100 的菜单图）—— 画的时候不能按 100 算
    ('40K_missions_icon_Daily skulls',         'atlasgroup_assets_all'),        # 骷髅徽标（222×198）
    ('40K_missions_icon_login bonus',          'atlasgroup_assets_all'),        # 登录奖励徽标（262×212）
    ('40k_missions_milestone_on',              'atlasgroup_assets_all'),        # 里程碑（已达成）67×66
    ('40k_missions_milestone_off',             'atlasgroup_assets_all'),        # 里程碑（未达成）67×67
    ('40k_generial_bar_empty',                 'duplicateassetisolation_assets_all'),  # 通用进度条底 12×12
    ('40k_generial_bar_fill',                  'duplicateassetisolation_assets_all'),  # ⚠️ 两张都是**九宫格 (4,4,4,4)**
    ('UI_Login_Tracker',                       'atlasgroup_assets_all'),        # 每日奖励弹窗左轨底 264×828
    ('40k_icon_DailyReward',                   'liveopsicons_assets_all_sprites'),
    ('40k_icon_DailyReward_Premium',           'liveopsicons_assets_all_sprites'),
    ('40K_Profile_icon_title',                 'atlasindividual_assets_0_mainmenu'),   # Free Track 图标
    ('WF_Login_CornerBanner',                  'atlasindividual_assets_0_mainmenu'),   # Premium 角旗
    ('40K_rewards_bt_missions',                'liveopsicons_assets_all_sprites'),     # 本窗左栏页签
    ('40K_rewards_bt_forge',                   'atlasindividual_assets_0_mainmenu'),
    #   ⚠️ **本窗 Campaign 钮用的就是主菜单那张导航图** —— `40K_rewards_bt_campaign` **不存在**（别去找）
    ('40k_main_bt_campaign',                   'atlasindividual_assets_0_mainmenu'),
    ('40K_shop_bt_boosters',                   'liveopsicons_assets_all'),             # 第 4 键 Booster Packs
    ('40k_Achievements_icon_medal1',           'duplicateassetisolation_assets_all'),  # 成就奖章 1–5（**第 3 层**用，先导着）
    ('40k_Achievements_icon_medal2',           'duplicateassetisolation_assets_all'),
    ('40k_Achievements_icon_medal3',           'duplicateassetisolation_assets_all'),
    ('40k_Achievements_icon_medal4',           'duplicateassetisolation_assets_all'),
    ('40k_Achievements_icon_medal5',           'duplicateassetisolation_assets_all'),
    ('40k_Achievements_icon_seal points',      'atlasindividual_assets_0_mainmenu'),
    ('40k_campaign_bar_bg',                    'atlasgroup_assets_all'),        # 成就/奖杯进度条（**bg 与 outline 是九宫格 (20,0,20,0)/(…)**）
    ('40k_campaign_bar_fill',                  'atlasgroup_assets_all'),
    ('40k_campaign_bar_end',                   'atlasgroup_assets_all'),
    ('40k_campaign_bar_outline',               'atlasgroup_assets_all'),
    # ---- `GenericPromptWindow`（`资料/日常_原版规格.md` §七；它是「暂无服务器」的唯一宿主）----
    ('40k_dropdown_bg',                        'duplicateassetisolation_assets_all'),  # 输入框底 119×102（九宫格 23,20,23,20）
    ('40K_button_hover',                       'duplicateassetisolation_assets_all'),  # 按钮三态是 **SpriteSwap**（换图不是换色）
    ('40K_button_pressed',                     'duplicateassetisolation_assets_all'),
    ('UI_Deck_Information_submenu_Back',       'atlasindividual_assets_0_mainmenu'),   # 收件箱/成就条目底
    ('UI_Deck_Information_submenu_Back_opaque','atlasindividual_assets_0_mainmenu'),   # 每日奖励奖格底
    ('WF Lock Icon Simple',                    'duplicateassetisolation_assets_all'),  # ⚠️ 名字里是**空格**不是下划线
    ('UI_Button_Menu_Back',                    'atlasindividual_assets_0_mainmenu'),   # 连登窗返回钮
    ('UI_Button_Round_background',             'duplicateassetisolation_assets_all'),  # 收件箱关闭钮底
    ('WF_Campaign_Info_Background',            'atlasindividual_assets_0_mainmenu'),   # 弹窗 Header 底
    ('40k_generic_bt_info',                    'duplicateassetisolation_assets_all'),  # 41×41 信息圆钮（本层出现 5 次以上）
    ('40k_menu_scroll bar_bg',                 'duplicateassetisolation_assets_all'),  # 收件箱滚动条
    ('WF_icon_clock',                          'atlasindividual_assets_0_mainmenu'),  # 任务卡上的时钟图标
    #   ⚠️ 与已有的 `WF_icon_clock for shader`（`WF_icon_clock_for_shader`）**不是同一张** —— 那张是 shader 变体
    # ---- 阶段二第 3 层 · 锻造厂页（`Forge Tab`）（2026-09-23 加，出处 `资料/阶段二_锻造厂与战役页_原版规格.md` §二）----
    #   ⚠️ 每张的「用在哪」见那份规格；括号里的尺寸都是**原版节点自己就是那么大**，别按别的尺寸画。
    ('40K_ArmyTrack_bg',                          'atlasgroup_assets_all'),            # 旋涡传送门大图 767×974
    ('40k_rewards_forge_decoration_Column_top',   'atlasgroup_assets_all'),            # 石柱顶 319×460
    ('40k_rewards_forge_decoration_Column_mid',   'atlasgroup_assets_all'),            # 石柱中 206×461
    ('40k_rewards_forge_decoration_Column_down',  'atlasgroup_assets_all'),            # 石柱底 213×474
    ('40k_rewards_forge_decoration 1_candle',     'atlasgroup_assets_all'),            # 蜡烛 40×59
    ('40k_rewards_forge_decoration 1_candle_light','atlasgroup_assets_all'),           # 烛光 137×169（节点再乘 scl 0.962）
    ('40k_rewards_forge_decoration 2',            'atlasgroup_assets_all'),            # 顶部装饰 273×210
    ('Glow UI W40K',                              'duplicateassetisolation_assets_all'), # 可升级光晕；节点色 **#FF2DDF**（拿洋红给白光晕染色）
    ('40k_main_line purple',                      'atlasindividual_assets_0_mainmenu'),# 阵营选择条的紫分隔线 171×6
    ('40K_general_icon_Forge points',             'atlasindividual_assets_0_mainmenu'),# 锻造点图标（**不带阵营后缀的那一张**）88×85
    #   ⚠️ 下面这几张是**奖励格那一格**（`Forge Menu Reward Button`）要用的，出处同上 §五。
    ('40K_button',                                'duplicateassetisolation_assets_all'), # 格里的 Claim 按钮底（节点 tint (1,.363,.919,1)）
    ('OctagonUI Border SDF',                      'liveopsicons_assets_all_sprites'),  # 按钮的 Highlight 描边（Sliced）
    ('40K_ArmyTrack_bar',                         'atlasgroup_assets_all'),            # 格内进度条底
    ('40K_ArmyTrack_bar_fill',                    'atlasgroup_assets_all'),            # 格内进度条填充
    ('40K_ArmyTrack_bar_position',                'liveopsicons_assets_all_sprites'),  # 格内进度条末端小件
    ('40k_ArmyTrack_milestone_on',                'atlasgroup_assets_all'),            # 等级圆牌（**可领/已领**态）
    ('40k_ArmyTrack_milestone_off',               'atlasgroup_assets_all'),            # 等级圆牌（**锁着/进行中**态）
    ('Border Line FX',                            'liveopsicons_assets_all_sprites'),  # 领奖光效（出厂 inactive）
    #   13 张「锻造点」阵营图 —— 格里的 `Army` 槽按阵营换图（`ForgePointIconDrawer.DrawForArmy`）
    ('40K_general_icon_Forge points_AstraMilitarum',          'atlasgroup_assets_all'),
    ('40K_general_icon_Forge points_BlackLegion',             'atlasgroup_assets_all'),
    ('40K_general_icon_Forge points_Dark Angels',             'atlasgroup_assets_all'),
    ('40K_general_icon_Forge points_Emperors Children',       'atlasgroup_assets_all'),
    ('40K_general_icon_Forge points_Genestealers',            'atlasgroup_assets_all'),
    ('40K_general_icon_Forge points_Goff',                    'atlasgroup_assets_all'),
    ('40K_general_icon_Forge points_Leviathan',               'atlasgroup_assets_all'),
    ('40K_general_icon_Forge points_SaimHann',                'atlasgroup_assets_all'),
    ('40K_general_icon_Forge points_Sautekh',                 'atlasgroup_assets_all'),
    ('40K_general_icon_Forge points_Sororitas',               'atlasgroup_assets_all'),
    ('40K_general_icon_Forge points_SpaceWolves',             'atlasgroup_assets_all'),
    ('40K_general_icon_Forge points_TauEmpire',               'atlasgroup_assets_all'),
    ('40K_general_icon_Forge points_Ultramarines',            'atlasgroup_assets_all'),
    #   战役页 / 战役奖励窗（出处同上 §十三 / §十四）
    ('WF_Campaign_Levelspot-Premium',             'liveopsicons_assets_all_sprites'),  # 节点的高级角标 256²（`localScale (2,2,2)`）
    ('Border Thick Circle FX',                    'liveopsicons_assets_all_sprites'),  # 节点「可领」光圈（出厂 inactive，`UIBorderGlow`）
    ('40k_general_bt_yellow_delete',              'duplicateassetisolation_assets_all'), # 节点上的 `icon`（`Setup()` 一来就关，**照建但关着**）
    #   阵营选择条上的**一格**（`Forge Army Item Button` / `Army Item Button`，出处同上 §六）
    ('40K_settings_button_selected',              'menus_assets_all_sprites'),         # 选中态的底（**橙色**；Forge 页那份原版改成**品红** (1,.2784,.902)）
    ('40K_ArmyTrack_chosen faction',              'atlasindividual_assets_0_mainmenu'),# 选中态的小箭头 102.38×30.71
    # ---- 阶段二第 3 层 · 战役页（`Campaign Tab`）（2026-09-23 加，出处同上 §四）----
    #   ⚠️ `WF_Campaign_Info_Background` / `40k_main_bt_nametag` / `WF_icon_clock` / `40k_topmarquee_currency_gold`
    #      / `40K_generic_bt_info` / `40k_campaign_bar_*` **上面已经有了**，不要重复加。
    ('40k_campaign_Premium-icon',                 'liveopsicons_assets_all_sprites'),  # Header 的「已购 Premium」角标 54²
    ('40K_genearl_icon_Campaign points',          'liveopsicons_assets_all_sprites'),  # 战役点小图标（⚠️ 原版拼写就是 genearl）
    ('40K_genearl_icon_Campaign points_big',      'liveopsicons_assets_all_sprites'),  # 战役点大图标（Premium Panel 的光晕）
    ('WF_UI_Ranked_Background_Gold',              'atlasindividual_assets_0_mainmenu'),# Premium Panel 底（Sliced + UIFlippable）
    ('UI_Button_Mulligan',                        'duplicateassetisolation_assets_all'), # 本页三种按钮的底（都 Sliced）
    ('40k_popup',                                 'duplicateassetisolation_assets_all'), # 教程气泡底（Sliced）
    ('40k_popup_texture',                         'duplicateassetisolation_assets_all'), # 教程气泡内层（Type=2 平铺）
    ('Border Line Only Horizontal FX',            'staticgeneralassets_assets_all_sprites'), # 气泡上下两条高亮（Sliced + UIBorderGlow）
    ('WF_Campaign_Levelspot',                     'atlasgroup_assets_all'),            # 战役轨道上的节点底盘
    # ---- 阶段二第 3 层 · 战役奖励窗（`Campaign Reward Window`）（2026-09-23 加，出处同上 §十四）----
    #   ⚠️ `UI_Button_Mulligan`（`Unlock Button` 的底）/ `40K_genearl_icon_Campaign points`（按钮里的点数图标）
    #      / `40k_campaign_Premium-icon`（高级列 `Badge`）**上面已经有了**，不要重复加。
    ('40k_general_popup_simple red',              'atlasindividual_assets_0_mainmenu'),# `Reward Background Get Reward`（Sliced）
    ('40k_general_popup_simple greyscale',        'atlasindividual_assets_0_mainmenu'),# `Reward Background Preview Reward`（Sliced，**出厂显这张**）
    #   奖励物品的图标 —— ⚠️ **只有两项有据可依**，见 `CampaignData.ItemIcon` 的注释：
    #     ① 这一条是**原版 SO 字段原文**（`Booster Pack Ultramarines.json` 的 `containerPreviewImage.m_SubObjectName`）；
    #     ② `WildcardUltramarines1..4` 走 `40k_general_wildcard_{common,rare,epic,legendary}_small`
    #        （那四张**工程里早就有**，在 `ui_deck/`）—— 那条映射**是我们建的**。
    ('40K_shop_offer_booster_UM',                 'boosterpacks_assets_all'),          # 卡包物品的图标（`ItemIcon` 第 1 条）
    # ---- 阶段二第 3 层 · 商店（`Shop Menu Variant`）（2026-09-23 加）----
    #   左栏页签的图标。**全库只有这 7 个** `*_shop_bt_*`（`bundle_liveopsicons_assets_all/Sprite/`）：
    #   boosters / campaigns / cards / cards_vip / cosmetics / gold / ticket
    #   —— ⚠️ **没有 "daily" 或 "item"**，所以商店左栏的键名不能凭「有哪几个页 prefab」去反推。
    ('40K_shop_bt_campaigns',                     'liveopsicons_assets_all_sprites'),
    ('40K_shop_bt_cards',                         'liveopsicons_assets_all_sprites'),
    ('40K_shop_bt_cards_vip',                     'liveopsicons_assets_all_sprites'),
    ('40K_shop_bt_cosmetics',                     'liveopsicons_assets_all_sprites'),
    ('40k_shop_bt_gold',                          'liveopsicons_assets_all_sprites'),
    ('40k_shop_bt_ticket',                        'liveopsicons_assets_all_sprites'),
    #   商品主图（原版 `Catalog Item Shop Container` 的 `background` 槽，运行期由服务端赋图）
    #   —— 同族一共 16 张 `40K_shop_offer_booster_*`，这里只导 `ShopData` 用到的那 4 张 + 通用那张。
    ('40K_shop_offer_booster_Sautekh',            'boosterpacks_assets_all'),
    ('40K_shop_offer_booster_Space Wolves',       'boosterpacks_assets_all'),
    ('40K_shop_offer_booster_leviathan',          'boosterpacks_assets_all'),
    ('40K_shop_offer_booster_generic',            'boosterpacks_assets_all'),
    ('40K_main_deck_card counter',                'atlasindividual_assets_0_mainmenu'), # 拥有数角标（Catalog 卡）
    ('40K_Icon_Discount_Gold',                    'atlasindividual_assets_0_mainmenu'), # WebShop 角标的折扣金币（出厂 inactive）
    ('40K_button_square',                         'duplicateassetisolation_assets_all'), # WebShop 方按钮底（出厂 inactive）
    # ---- 阶段二第 3 层 · 卡组线的 Cards 页「完整筛选面板」（2026-09-23 加）----
    #   Type Filter 那 3 个格子的 `Background`（原版 `CardTypeFilter.options` 的 `background` 槽，运行时赋图）。
    #   ⚠️ `..._warlord` **工程里早就有**（在 `ui_deck/`），只补另外两张。
    ('40k_menu_search_icon_troop',                'atlasindividual_assets_0_mainmenu'),
    ('40k_menu_search_icon_stratagem',            'atlasindividual_assets_0_mainmenu'),
    # ---- 阶段二第 3 层 · 卡组线 `Deck info Popup` 的 `Deck Options` 五个圆钮（2026-09-23 加）----
    #   `_duplicate` / `_share` / `_delete` / `_close` **上面已经有了**，只补这两张。
    #   ⚠️ `share in chat` 那个名字**带空格** ⇒ 输出会转成下划线（脚本的既有约定）。
    ('40k_general_bt_yellow_seedeck',             'duplicateassetisolation_assets_all'),
    ('40k_general_bt_yellow_share in chat',       'duplicateassetisolation_assets_all'),
    # ---- 阶段二第 3 层第 6 件「战斗入口」：**主菜单的模式卡图**（2026-09-24 加）----
    # 🔴 **用户 2026-09-24 拍板：模式卡就是入口**（点卡 ⇒ 进对应模式的界面）。
    #    原版「哪个模式 → 哪张图」的映射在 **liveop 服务端**（`资料/主菜单_原版规格.md` §9·3 明写「本地查不到、别自己编」）
    #    ⇒ **这三条是我们按名字对上的，不是复刻**（Tutorial / Draft 那两张先例同样如此）。
    #    `Container Image Practice 1x2` 也是练习的（1x2 变体）⇒ **遭遇战没有专属卡图**，用 `40k_main_GameMode_Skirmish`
    #    （它是全族里唯一带 Skirmish 字样的 1024² 卡图）。
    ('Container Image Practice 1x1',              'liveopsmenuimages_assets_all'),
    ('Container Image Ranked',                    'liveopsmenuimages_assets_all'),
    ('40k_main_GameMode_Skirmish',                'liveopsmenuimages_assets_all'),
    # ---- 🆕 2026-09-24：第 3 层第 6 件「战斗入口」四窗 + Styles 页要用的（逐张 `test -f` 验过源文件）----
    # 出处 = `资料/阶段二_战斗入口_原版规格.md` §四（逐图对账那一节）。⚠️ 那张表里另外 8 张标了 ✗
    # 其实**工程里已经有**（`40k_popup_texture` / `40K_generic_bt_info` / `UI_Button_Menu_Back` /
    # `WF_icon_clock` / `40k_UI_bt_back` / `40k_bt_underbutton` / `Smooth background square` / `Rank Skull`）
    # ⇒ **别再导一遍**（重复导不会报错，只会白覆盖）。
    ('40K_icon_searching_skull',                  'duplicateassetisolation_assets_all'),  # 搜对手：骷髅
    ('40K_icon_searching_cog',                    'atlasindividual_assets_0_mainmenu'),   # 搜对手：齿轮
    ('UI_icon_shield',                            'atlasindividual_assets_0_mainmenu'),   # 遭遇战 `Battle!` 左图标
    ('UI_Background faction buttons',             'atlasindividual_assets_0_mainmenu'),   # 练习窗阵营纵列底
    ('40k_gamemode_icon_classic',                 'armyicons_assets_all'),                # 练习窗 `Game mode` 开关
    # 🆕 2026-09-26 **难度角标**（3 张 = 1/2/3 条金 V 杠）—— `CollectionDeck` 的 `easyMark/normalMark/hardMark`。
    # 映射：`0/5 → _1` · `10 → _2` · `15 → _3`（`CollectionDeck__Config.c:117-136`）；
    # 只在「预组卡组」页显示（开关 `DeckCollectionDisplay.displayDifficultyLabel`）。出处 → `资料/预组卡组_原版规格.md` §六。
    ('Menu_Icon_Gallons_1',                       'atlasindividual_assets_0_mainmenu'),   # 难度角标：1 条杠（easyMark）
    ('Menu_Icon_Gallons_2',                       'atlasindividual_assets_0_mainmenu'),   # 难度角标：2 条杠（normalMark）
    ('Menu_Icon_Gallons_3',                       'atlasindividual_assets_0_mainmenu'),   # 难度角标：3 条杠（hardMark）
    ('40k_bt_eye',                                'duplicateassetisolation_assets_all'),  # ⚠️ 88×87 的**另一张**，别和 `40k_UI_bt_eye` 混
    ('MiniBar_01',                                'atlasindividual_assets_0_mainmenu'),   # 遭遇战计分条 Slider
    ('Crate Border Highlight',                    'menus_assets_all'),                    # 奖励箱高亮
    ('40k_Crate_Tier1_Iron',                      'boosterpacks_assets_all'),
    ('40k_Crate_Tier2_Copper',                    'boosterpacks_assets_all'),
    ('40k_Crate_Tier3_Silver',                    'boosterpacks_assets_all'),
    ('40k_Crate_Tier4_Gold',                      'boosterpacks_assets_all'),
    ('40k_Crate_Tier5_Warp',                      'boosterpacks_assets_all'),
    # 🆕 2026-10-13（A389）：上面那 5 张是**闭合**态，这 5 张是**开启**态 —— 周常里程碑那 6 格的
    #   `holder/CheckMark` 就画它们（达成 → `activeCheckmark` = `_open`；未达成 → `disabledCheckmark` = 闭合）。
    #   判据 = `Weekly Mission Milestone T1..T5` 各自 MB 的这两个字段（pid 经 `_tmp_view/sprite_pids_ALL.json`
    #   解出名字，逐条见 `Shell/MissionsTab.cs` 的 `BuildMilestone` summary）。
    ('40k_Crate_Tier1_Iron_open',                 'boosterpacks_assets_all'),
    ('40k_Crate_Tier2_Copper_open',               'boosterpacks_assets_all'),
    ('40k_Crate_Tier3_Silver_open',               'boosterpacks_assets_all'),
    ('40k_Crate_Tier4_Gold_open',                 'boosterpacks_assets_all'),
    ('40k_Crate_Tier5_Warp_open',                 'boosterpacks_assets_all'),
    ('40K_main_rank_display',                     'duplicateassetisolation_assets_all'),  # 排位：段位条
    ('UI Dirt And Noise skratches',               'liveopsicons_assets_all_sprites'),     # 排位：红底上的脏污
    ('Roman V',                                   'rankeddivisionicons_assets_all'),      # 排位：段位罗马数字（I–VI 全族同在）
    ('40k_ranking_icon_trophy Plus',              'atlasindividual_assets_0_mainmenu'),   # 排位 `Battle!` 右图标
    ('UI_Deck_Warlord_Uriel Ventris',             'uiwarlords_assets_all'),               # 搜对手窗的督军立绘（`Found` 态）
    ('40k_general_bt_arrow',                      'duplicateassetisolation_assets_all'),  # Styles 页两个换风格圆钮里的箭头（左钮镜像复用）
    # ---- 🆕 2026-09-24：**卡片详情窗**要用的（出处 `资料/阶段二_卡片详情窗_原版规格.md` §五）----
    ('40K_main_deck_card counter',                'atlasindividual_assets_0_mainmenu'),   # 「x2 / 88」那条计数底板
    ('40k_main_collection_icon',                  'atlasindividual_assets_0_mainmenu'),   # 创建副本按钮上的通配符图标
    ('40k_general_icon_card amount',              'atlasindividual_assets_0_mainmenu'),   # 副本数小图标（计数条右端那枚）
    ('40k_topmarquee_currency_display BW',        'duplicateassetisolation_assets_all'),  # 通配符条底
    ('WF Lock Icon Simple',                       'duplicateassetisolation_assets_all'),  # 异画面板的锁
    # ---- 🆕 2026-09-26：**主菜单设置窗**（联机线的宿主窗）（出处 `资料/联机P2P_设计与交接.md` §3·5）----
    #   页签底三态 + 五个页签图标。⚠️ **Media 页用的就是 `_quality` 那张**（原版就复用同一张）。
    ('40K_settings_button',                       'duplicateassetisolation_assets_all'),  # 页签底（普态）
    ('40K_settings_button_hover',                 'duplicateassetisolation_assets_all'),  # 页签底（悬停；General 默认选中用的就是它）
    ('40K_settings_button_selected',              'duplicateassetisolation_assets_all'),  # 页签底（选中）
    # 🆕 2026-10-07（A118①）：**按下**那一档。它是 `Orange Tab Toggle` 的 `m_SpriteState.m_PressedSprite`
    #   （聊天窗频道键四态里的第 4 张）—— 此前**不在表里也不在 `Resources/`**，于是按下只能退回悬停图
    #   （`WindowButton.Press()` 的 `?? _hoverTex` 兜底 ⇒ **静默**，不报错）。同尺寸 168×156。
    ('40K_settings_button_pressed',               'duplicateassetisolation_assets_all'),  # 页签底（按下）
    ('40K_settings_button_general',               'atlasindividual_assets_0_mainmenu'),   # 页签图标 · General
    ('40K_settings_button_quality',               'atlasindividual_assets_0_mainmenu'),   # 页签图标 · **Media 页用的就是它**
    ('40K_settings_button_account',               'atlasindividual_assets_0_mainmenu'),   # 页签图标 · Account（我们没建那一页，先备着）
    ('40K_settings_button_graphics',              'atlasindividual_assets_0_mainmenu'),   # 页签图标 · Graphics
    ('40K_settings_button_support',               'atlasindividual_assets_0_mainmenu'),   # 页签图标 · Support（同上，先备着）
    # ---- 🆕 2026-09-27：**玩家档案窗**（6 页签）要用的（出处 `资料/阶段二_多人界面_原版规格.md` §2·1）----
    #   ⚠️ 页签底**复用设置窗那两张**（`40K_settings_button` 普态 / `_hover` 选中）—— 已在上面导过，
    #      原版 `EverguildToggle` 的 `offSprite` = 普态底、`onSprite` = hover 那张（按 PathID 对齐查出来的）。
    #   下面四张 = 六个页签的图标里**工程里还没有**的那几个；`_title`（第 3 键）与
    #   `40k_UI_icon_ranked_Skirmish`（第 6 键 `Ranked`）**已经在表里/在 `ui_deck/`**，不重复导。
    ('40K_Profile_icon_profile',                  'atlasindividual_assets_0_mainmenu'),   # 第 1 键 Profile
    ('40K_Profile_icon_avatar',                   'atlasindividual_assets_0_mainmenu'),   # 第 2 键 Avatar
    ('40K_Profile_icon_battlelog',                'atlasindividual_assets_0_mainmenu'),   # 第 4 键 Battle Log
    ('40K_Profile_icon_Trophies',                 'atlasindividual_assets_0_mainmenu'),   # 第 5 键（文案是 Achievements）
    # ---- 🆕 2026-09-27（下半场）：**档案窗「Profile」那一页的内容** + 段位图全族 ----------
    #   出处：`资料/普查产出_0927/档案窗_Profile页.md` §A（层 × 参数表里逐个点名的 sprite 名）。
    #   🔴 这 7 张**之前一直没进 `Resources/`** —— `ProfileTab` 用到它们，而自检有一条
    #      「这一扇用到的图一张都不缺」（`pp.MissingArt.Count == 0`）⇒ 少了它们那条会红。
    ('40k_profile_icon_copy',                     'atlasindividual_assets_0_mainmenu'),   # `PlayerId` 那行的复制图标 27×34
    ('40k_menu_bt_general_bg',                    'duplicateassetisolation_assets_all'),  # 通用小钮底（51×52 · 九宫 15,15,15,15）
    ('40k_menu_bt__general_outline',              'duplicateassetisolation_assets_all'),  # 通用小钮描边（同上；`Invite to alliance` / `Edit Name Button`）
    ('40K_profile_ForgeLevel_bg',                 'atlasindividual_assets_0_mainmenu'),   # `Events` 里「锻造厂」那格底（631×194 · Simple）
    ('07-Legend',                                 'rankeddivisionicons_assets_all'),      # 传奇段位图（512×512 · **ppu=50，显示尺寸 ×2**）
    ('WF_UI_Ranked_Background_Silver',            'atlasindividual_assets_0_mainmenu'),   # 传奇卡里的银奖杯条底（352×116 · 九宫 20,20,20,20）
    ('WF_UI_Ranked_Background_Bronze',            'atlasindividual_assets_0_mainmenu'),   # 铜奖杯条底（同上）
    #   ⚠️ `WF_UI_Ranked_Background_Gold` 与 `WF_UI_Trophy_Gold` **已经在**（`ui_menu/` / `ui/`），别重复导。
    #   ⚠️ 三档奖杯的 `Icon` 原版**都用 `WF_UI_Trophy_Gold` 那一张**（预制体真值，不是我们抄错）。
    # 段位图**全族 13 张**（建成 2026-09-27：原来只导了 `Roman V` 一张）——
    #   「档案窗 Ranking 页」与战斗结算都要按段位取图 ⇒ 一次导齐（出处 `资料/普查产出_0927/档案窗_Ranking页与图名表.md`）。
    ('Roman I',                                   'rankeddivisionicons_assets_all'),
    ('Roman II',                                  'rankeddivisionicons_assets_all'),
    ('Roman III',                                 'rankeddivisionicons_assets_all'),
    ('Roman IV',                                  'rankeddivisionicons_assets_all'),
    ('Roman VI',                                  'rankeddivisionicons_assets_all'),
    ('01-Rook',                                   'rankeddivisionicons_assets_all'),
    ('02-Veteran',                                'rankeddivisionicons_assets_all'),
    ('03-Commander',                              'rankeddivisionicons_assets_all'),
    ('04-Admiral',                                'rankeddivisionicons_assets_all'),
    ('05-Conqueror',                              'rankeddivisionicons_assets_all'),
    ('06-Galactic Threat',                        'rankeddivisionicons_assets_all'),
    # ---- 🆕 2026-09-27（下半场）：**档案窗「Battle Log」那一页**的行要用的两张 ------------
    #   出处：`资料/普查产出_0927/档案窗_BattleLog与页签按钮.md` §A·1（行里 `ReplayButton` / `PinButton` 的子图）。
    #   ⚠️ 第二张的名字**中间有空格** ⇒ 落盘成 `40k_general_bt_yellow_pin_replay.png`（照本表规矩「空格换下划线」）。
    ('40k_general_bt_yellow_replay',              'atlasindividual_assets_0_mainmenu'),   # 回放钮上的图标 71×71
    ('40k_general_bt_yellow_pin replay',          'atlasindividual_assets_0_mainmenu'),   # 钉住钮上的图标 71×71
    # ---- 🆕 2026-09-27（下半场）：**档案窗「Trophies」那一页**要用的 ---------------------
    #   出处：`资料/普查产出_0927/档案窗_Trophies页.md` §A·1 与 §增补 1。
    #   其余几张（`40k_Achievements_icon_medal1..5` / `_seal points` / `40k_campaign_bar_{bg,outline,fill,end}`）
    #   **已经在 `ui_menu/` 里**（日常那一层导过），不重复。
    ('Feedback Scoring Button',                   'atlasindividual_assets_0_mainmenu'),   # `Counter` 计数条底板 96×60 · 九宫 38,20,38,20 · 色 (0.65283,0.06775,0.06775,1)
    # ---- 🆕 2026-09-27（下半场）：**档案窗「Ranking」那一页**要用的 -------------------------
    #   出处：`资料/普查产出_0927/档案窗_Ranking页与图名表.md` §A·2 末（「最高分那一行用哪张」）与 §B·2。
    #   ⚠️ **别和 `Menu_Icon_Gallons_1/2/3`（难度角标那三张）混** —— 这是**另一张**，8 处 `MaxRating`/
    #      `Alliance Rating Display (1)` 的 `Main Icon` 用它。其余图（`40k_menu_bt_general_bg` /
    #      `40k_menu_bt__general_outline` / 段位族 13 张 / `40k_generic_bt_info`）**本轮已经在表里/在工程里**。
    ('Menu_Icon_Galon',                           'atlasindividual_assets_0_mainmenu'),   # 最高阵营分那一行的图标 64×64
    # ---- 🆕 2026-09-27（晚场）：**多人界面 · 社交 / 好友那一件**要用的 -----------------------
    #   出处：`资料/普查产出_0927/社交_联盟与好友页.md` §A·1 / §A·2 / §B（逐件的 RT PathID 在那边）。
    #   ⚠️ 两张页签图标与三张行内图标的名字**都不在** `ui_menu/`（本表原来只导了 `40k_main_bt_friends`＝**左栏导航**那张，
    #      不是社交窗里的页签）⇒ 下面这五张是**新导**，别以为「名字像就是同一张」。
    ('40k_main_bt_alliances',                     'atlasindividual_assets_0_mainmenu'),   # 社交窗左栏第 1 键（`Alliances Tab Button`）图标 252×251
    ('40k_alliances_bt_friends v2',               'atlasindividual_assets_0_mainmenu'),   # 左栏第 2 键（`Friends Tab Button`）图标 252×251
    ('40k_alliances_icon_chat',                   'atlasindividual_assets_0_mainmenu'),   # 联盟成员页 `ChatPreview` 右侧那颗钮上的图标
    ('40K_bt_addFriend',                          'atlasgroup_assets_all'),               # 好友页 `Add Friend Button` 的图标 103×76
    ('40K_bt_challenge1',                         'atlasgroup_assets_all'),               # 好友页 `Instant duel Button` 的图标 88×89
    #   ---- 下面三张是**通用件**，社交那一屏里反复出现（分隔线 8 处 / 按钮底 6 处 / 圆钮底 3 处）----
    ('40k_Separator Fade Sides Horizontal',       'duplicateassetisolation_assets_all'),  # 横向渐隐分隔线 128×4 · 九宫 63,0,63,0
    ('40K_button',                                'duplicateassetisolation_assets_all'),  # 通用按钮底 489×107 · 九宫 234,46,234,46（`Join`/`Dismiss` 那些）
    ('UI_Button_Organe_Square_Normal',            'duplicateassetisolation_assets_all'),  # 方形图标钮底 145×124 · 九宫 75,51,63,57
    #   ---- 好友行（`Friend Info Item` 独立根，§A·2·4）里的三颗钮 + 两个在线状态点 ----
    #   ⚠️ `40K_bt_View Friend` **名字里是空格** ⇒ 落盘成 `40K_bt_View_Friend.png`（照本表规矩「空格换下划线」）。
    ('40K_bt_challenge2',                         'atlasgroup_assets_all'),               # 挑战钮 104×105（好友行 502.04,−5.43→570.68,63.89）
    ('40K_bt_deleteFriend',                       'atlasgroup_assets_all'),               # 删好友钮 104×105
    ('40K_bt_View Friend',                        'atlasgroup_assets_all'),               # 看档案钮 104×105
    ('40K_icon_status_online',                    'atlasgroup_assets_all'),               # 在线点 50×50（行里 25.2² · 染绿 (0,1,0.0736,1)）
    ('40K_icon_status_offline',                   'atlasgroup_assets_all'),               # 离线点 50×50（行里 25.2² · 染色 (0.84,0.494,0.44,1)）
    #   ---- 建盟页那行「价格」用的水晶图标（`Create Alliance Text>Price Display Button>…>Price Display>icon`）----
    ('40k_general_icon_currency_crystal',          'boosterpacks_assets_all'),            # 水晶 512×512（原版节点里占 46.91²）
    # ---- 🆕 2026-09-27（晚场）：**多人界面 · 聊天窗 / 好友挑战弹窗**要用的 -------------------
    #   出处：`资料/普查产出_0927/聊天窗与挑战弹窗.md` §A / §B（逐件的 RT PathID 在那边）。
    ('Chat_background',                           'atlasindividual_assets_0_mainmenu'),   # 聊天框底 613×828 · 九宫 138,113,137,107（染色 α0.867）
    ('Chat_text_background',                      'atlasindividual_assets_0_mainmenu'),   # 输入行底 536×115 · 九宫 53,43,52,43 · ppuMul 2.0
    ('40k_UI_Chat_send',                          'atlasindividual_assets_0_mainmenu'),   # 发送钮 119×119（节点里 40²）
    ('WF_9Sliced',                                'atlasindividual_assets_0_mainmenu'),   # 消息行底 124×124 · 九宫 62,62,62,62（ppuMul 4.82）
    # ---- 🆕 2026-10-03：**两扇「卡包」窗**要用的三张（`项目任务.md` §三 第 29 条 A6/A7）------
    #   出处：`资料/阶段二_商店_原版规格.md` §五·二 / §五·三 的依赖摸底（2026-10-03）。
    #   这三张是那两扇窗**唯一缺的**，其余十几张早就进 `Resources/` 了。
    ('OctagonUI Filled Fade SDF',                 'menus_assets_all'),                    # `Booster Info Popup` 的 WebShop 高亮
    ('Card Ready For Level Up',                   'duplicateassetisolation_assets_all'),  # `Booster Pack Open Window` 的「可升级」角标
    ('40k_Cross_icon_cross_big Banned card',      'duplicateassetisolation_assets_all'),  # `Booster Pack Open Window` 的禁用叉
    # ---- 🆕 2026-10-03：**按钮悬停换图**（`项目任务.md` §三 第 29 条 **A17**）------------------
    #   判据与逐颗映射 → `资料/普查产出_1003/按钮悬停图_普查.md`（普查产出，一份）。
    #   原版这批按钮是 `m_Transition = 2 (SpriteSwap)` ⇒ **悬停换的是【图】不是色**；
    #   我们原来只有统一的色偏兜底（那是 505 颗 ColorTint 的行为）⇒ 这是一处**已知偏离**。
    #   ⚠️ 命名规律绝大多数是 `<常态图>_hover`，**两处例外**：`40K_settings_button`→`…_selected` ·
    #      `40K_dropdown_field_closed`→`…_opened`（表在那份普查文档 §一）。
    #   🔴 下面这些**全部逐个核过**：源 PNG 都在 `MENU_SRC/<bundle>/Sprite/` 里现成可取。
    ('UI_Button_Mulligan_hover',                  'duplicateassetisolation_assets_all'),
    ('UI_Button_Mulligan_Pressed',                'duplicateassetisolation_assets_all'),
    ('40k_general_bt_yellow_hover',               'duplicateassetisolation_assets_all'),
    ('40k_general_bt_yellow_pressed',             'duplicateassetisolation_assets_all'),
    ('40k_bt_close_hover',                        'duplicateassetisolation_assets_all'),
    ('40k_bt_close_pressed',                      'duplicateassetisolation_assets_all'),
    ('40k_UI_bt_back_hover',                      'atlasindividual_assets_0_mainmenu'),
    ('40k_UI_bt_back_hover_back',                 'duplicateassetisolation_assets_all'),  # 奖励窗左下那颗「返回」
    ('UI_Button_Menu_Back_Hover',                 'atlasindividual_assets_0_mainmenu'),   # ⚠️ 大写 H
    ('UI_Button_Menu_Back_Pressed',               'atlasindividual_assets_0_mainmenu'),
    ('40k_UI_bt_deck_change_hover',               'atlasindividual_assets_0_mainmenu'),
    ('40k_bt_eye_hover',                          'duplicateassetisolation_assets_all'),
    ('40K_button_square_hover',                   'duplicateassetisolation_assets_all'),
    # ▶ ▶ 2026-10-04（A34-F5）：按下态那张一直缺着——
    #    原版 19 份 OfferContainer 的 `WebShop Button Square Variant` 实读 `P=40K_button_square_pressed`，
    #    而我们工程只有 `_hover`（源切片在 `ui_extract` 缓存里现成可取）
    #    ⇒ `WindowButton._pressedTex` 为 null（按下态静默不生效）。
    ('40K_button_square_pressed',                 'duplicateassetisolation_assets_all'),
    ('40K_dropdown_field_closed',                 'duplicateassetisolation_assets_all'),
    ('40K_dropdown_field_opened',                 'duplicateassetisolation_assets_all'),  # 下拉「展开」那一态
    ('40k_menu_bt',                               'duplicateassetisolation_assets_all'),
    ('40k_menu_bt_pressed',                       'duplicateassetisolation_assets_all'),
    ('UI_Button_Organe_Square_Hover',             'duplicateassetisolation_assets_all'),  # 社交页两颗
    # 🆕 2026-10-03 晚补（A17 接线时发现）：卡牌详情窗那两颗圆钮
    # （直接读原版 prefab：`bundle_scenes_scenes_mainmenuwarpforge > Card Displayer Menu For Menu` 的
    #  `Voice Over Button` / `Show Card Text`，`trans=2`）—— 普查表把 `Show Card Text` 那两张写成了
    # `40k_bt_eye*`（**读错了**，`40k_bt_eye` 是 88×87 的另一张），实测是下面这两组。
    ('40k_UI_bt_eye_hover',                       'duplicateassetisolation_assets_all'),
    ('40k_UI_bt_eye_pressed',                     'duplicateassetisolation_assets_all'),
    ('40k_UI_bt_voicelines_hover',                'duplicateassetisolation_assets_all'),
    ('40k_UI_bt_voicelines_pressed',              'duplicateassetisolation_assets_all'),
    # ---- 🆕 2026-10-14（A808）：**把手拷进来的那批登记成 job** ----------------------------------
    # 判据 → `资料/普查产出_1013/D1013_诊断_块4_Shop与Rewards.md` §六·1（原话「那 18 张手拷图
    #   两边导入器都没登记 ⇒ **下次一跑导入器就没了**」）。18 张 = 2026-10-06 12:31 被**手工拷**进
    #   `Resources/Art/ui_menu/` 的（`ls --time-style` 逐张核过），其中 5 张 `40k_Crate_*_open`
    #   当轮已登记 ⇒ **这里补的是剩下 13 张里的 9 张**；另 4 张的源**只存在于 `Texture2D/`**、
    #   而本表拼路径写死了 `Sprite/` ⇒ 见本表末尾（`MENU_IMAGES` 收尾）那一段「没登记的 4 张」。
    # 🔴 **为什么必须登记**：`Resources/Art/**` 整个在 `.gitignore`（构建产物）⇒ 谁跑一次导入器、
    #   或按「删目录退回占位美术」清一次，这几张就没了，而那 4 条断言会**静默翻回红**（= A808）。
    # 🔴 **逐张验过「源对了」**：盘上那份与下面点名的源 **md5 逐字节相同**（同名副本在解包里不总是
    #   同一张图 —— 见下面 `40k_shop_popup_info_bg` 那条）。上面这 9 张实测**5 个包各一份、内容一致**，
    #   取哪一份都对；这里按 `资料/主菜单_原版规格.md` §二 与既有的同族条目（`..._gold` /
    #   `OctagonUI Border SDF`）挑的出处写，与新拷进来的那张**逐字节相同**。
    ('40k_topmarquee_currency_blackstone',        'boosterpacks_assets_all'),  # 币种图标 · 黑石
    ('40k_topmarquee_currency_crystal',           'boosterpacks_assets_all'),  # 币种图标 · 水晶（登录卡占位图用它）
    ('40k_topmarquee_currency_energy',            'boosterpacks_assets_all'),  # 币种图标 · 能量（`MainMenuScene.cs:8555` 那一格的期望图）
    ('40k_topmarquee_currency_ticket',            'boosterpacks_assets_all'),  # 币种图标 · 票券
    #   ⚠️ 这四个与表里早有的 `40k_topmarquee_currency_gold` / `..._display BW` 是**同一套**；
    #      `MainMenuScene.cs:985` 那句「本地还差 4 张币种小图标」说的就是它们（那句现在过期了）。
    # ---- 🆕 2026-10-15（A544）：币种的 **big 族** 三张（`Shell/ShopData.cs` 三件商品的奖励图）-----
    # 判据 → `资料/普查产出_1015/R6_A544商品图标判据.md` §二（三个币种 SO 的 `smallIcon`/`bigIcon`
    #   两列**逐字实读**）+ §四（**口径 = 路 B**：我们这一格只画一张图 ⇒ 取**主图那一档 = `bigIcon`**；
    #   三条互证 = `CurrencyDrawer__Draw.c` 的 `GetIcon(item, 1)` / `Currency__GetIcon.c` 的 `+0x58` /
    #   `dump.cs` 的 `bigIcon // 0x58` + `enum IconSize{Small=0, Large=1}`）。
    #   ⇒ 上面那四张是 **small 族**（90×90：顶栏计数器 / 价签行那一档），**这一族是 big 族**（512×512）。
    # 🔴 第一张的名字**带空格**（原版原名就是 `40K_general_icon_currency blackstone`）⇒ 落盘名由本表
    #   既有惯例 `name.replace(' ', '_')` 定 = **`40K_general_icon_currency_blackstone.png`**；
    #   而 `Core/CardArt.cs` 的 `MenuUi(name)` **不做**这个转换 ⇒ `Shell/ShopData.cs` 里必须填**落盘名**。
    # ⚠️ 同族的 `40k_general_icon_currency_crystal`（水晶 big）**上面已经有了**（建盟页那一格）—— 别重复加。
    ('40K_general_icon_currency blackstone',      'boosterpacks_assets_all'),  # 黑石 big（原版 `Blackstone` SO 的 `bigIcon` 直读值）
    ('40K_icon_ticket_bundle',                    'boosterpacks_assets_all'),  # 票券 big（原版 `Gacha tickets` SO 的 `bigIcon` 直读值）
    ('40k_general_icon_currency_gold',            'boosterpacks_assets_all'),  # 金币 big（⚠️ `gold` SO 本地没有 ⇒ 见 R6 §五·4，是「同族 + 文件存在」推的）
    ('40k_UI_Banner BW',                          'atlasindividual_assets_0_mainmenu'),  # 战役奖励窗 `Bonus points` 横幅（A702）
    ('UI_HIghlight Internal',                     'atlasindividual_assets_0_mainmenu'),  # `PurchasePremiumWindow` 的 `Hightlight`（A702）
    #   ⚠️ 大小写照原样：`HIghlight`（大写 I）—— 写成 `Highlight` 就在缓存里 0 命中。
    ('OctagonUI Filled SDF',                      'duplicateassetisolation_assets_all'), # 卡包窗 `bg shadow`（A702）
    ('OctagonUI Border SDF 2',                    'liveopsicons_assets_all_sprites'),    # 与 `OctagonUI Border SDF` 同族（出处在上面那一条）
    # 粒子贴图（`RewardWindow.RewardClaimFx.TexTable` 要它俩；表里那 6 张的另外 4 张见本表末尾）
    ('Glow',                                      'duplicateassetisolation_assets_all'), # 取奖励的辉光
    ('Up Rays',                                   'duplicateassetisolation_assets_all'), # 取奖励的上射光
    # ---- 🆕 2026-10-14（A629）：两扇「卡包/报价」窗缺的两张（`BaseOfferPopup.cs:78-82` 记过）--------
    #   原来只在 `Art/原版/0_mainmenu/`（`40k_OfferBadge`）或**哪儿都不在**（`40k_shop_popup_info_bg`，
    #   只在 `素材/Warpforge原版/游戏数据/卡包/`）⇒ `CardArt.MenuUi` 三目录全 MISSING、那两处只建节点不画。
    # 🔴 **`40k_shop_popup_info_bg` 在缓存里有两份、内容是【两张不同的图】**：`boosterpacks_assets_all`
    #   那份与 `素材/游戏数据/卡包/Texture2D/` 那张 mean|Δ|=0.018（= 同一张的重编码）；
    #   `liveopsmenuimages_assets_all` 那份 mean|Δ|=**2.26** ⇒ **别换包**（这里取 boosterpacks）。
    ('40k_OfferBadge',                            'atlasindividual_assets_0_mainmenu'),  # 价签上的角标（md5 与 `Art/原版/0_mainmenu/` 那张同）
    ('40k_shop_popup_info_bg',                    'boosterpacks_assets_all'),            # `Artwork/background` 的底图
]

# ---- 🔴 上面那批里**还差 4 张没登记**（A808 未清的那一格，2026-10-14 实读）--------------------
#
# `Resources/Art/ui_menu/` 里仍有 **4 张「盘上有、本表产出不了」**的图（同日逐张 md5 核过）：
#
#   | 落盘名 | 源（**md5 与盘上那份逐字节相同**） |
#   |---|---|
#   | `Laser_Wave_2.png`   | `ui_extract/menus_assets_all/Texture2D/Laser_Wave_2.png` |
#   | `LightningTrail.png` | `ui_extract/duplicateassetisolation_assets_all/Texture2D/LightningTrail.png` |
#   | `Noise_Combined.png` | `ui_extract/duplicateassetisolation_assets_all/Texture2D/Noise Combined.png` |
#   | `Shine_trail.png`    | `assets_full/bundle_battlesharedresources_assets_all/Texture2D/Shine trail.png`（= `素材/Warpforge原版/特效共享资源/Texture2D/` 那份，三处 md5 同为 `e7ba39201e15…`） |
#
# 🔴 **它们进不了本表，是机制问题、不是「找不到源」**：那 4 张在整个 `ui_extract` 里
#   **只存在于 `<bundle>/Texture2D/`**（`find -ipath '*/Sprite/*' -iname '*shine*'` 等四条全 0 命中），
#   而本表拼路径那一行写死了 `'Sprite'`：
#       `os.path.join(MENU_SRC, bundle, 'Sprite', name + '.png')`
#   ⇒ 把名字加进来只会多出 4 条 `缺:`（**静默失败的反面**：它会出声，但图还是没有）。
#   ⚠️ 别用「名字里塞 `../`」绕过 —— 落盘名也会跟着变成 `../Texture2D/…`，写到 `ui_menu/` 外面去。
#
# 它们**确实是运行时需要的**：`Shell/RewardWindow.cs:1570-1587` 的 `RewardClaimFx.TexTable`
#   要这 4 张 + 上面已登记的 `Glow` / `Up Rays`（共 6 张，走 `CardArt.MenuUi` ⇒ 三目录兜底都能命中）。
#   `RewardWindow.cs:1575-1578` 那条注释说「要补就往 `MENU_IMAGES` 加」——**那条是错的**：
#   本表读的是 `ui_extract/<bundle>/Sprite/`，**不是** `assets_full/bundle_<包>/Texture2D/`。
#
# 处置 = **请调度台裁**（本件白名单只允许「往 `MENU_IMAGES` 加条目」）。两条候选，判据都齐：
#   ① 本文件里给「非图集切片」再开一张小表（照 `FX_TEXTURES` 那条先例：那把就是「从 bundle 里
#      单独抽出来的特效贴图」），条目照 `Texture2D/` 取 —— ⚠️ 但 `FX_TEXTURES` 落的是 `Art/ui/`，
#      与本批所在的 `ui_menu/` **不是同一层**，换层会改 `CardArt.MenuUi` 的兜底命中项（见
#      `工具/sync_battle_ui_art.py:246-248` 记的那条先例：`40k_dropdown_bg` 大小写两份）。
#   ② 把 4 张 PNG 抽进 `素材/Warpforge原版/特效贴图/`（`FX_SRC`，`Shine trail` 那份**已经在**
#      `素材/Warpforge原版/特效共享资源/Texture2D/`）再挂 `FX_TEXTURES` —— 同样有 ① 的换层问题。
#   （两条都要动本文件 `MENU_IMAGES` 之外的代码 ⇒ 不在本件白名单内，如实停在报告里。）

# ---- 只在**工程自己的图集切片库**里有的那几张（2026-09-23 加）--------------------------
# 判据：先在 `MENU_SRC/*/Sprite/` 全目录里按名字 glob，**0 命中**的那些才进这里
#（实测整个 extract 缓存里 48 张里只有这 1 张查不到）。
#   `WF_Special offer_Value`（324×87，九宫格 162,0,162,0）属于 **`0_GeneralUI Atlas`**，
#   而那张图集的**切片产物在工程里**（`Art/原版/去重资源/`，由 `工具/slice_ui_atlas.py` 切的，带 `_atlas_rects.json`）。
# ⚠️ 源文件名里空格已换成下划线，和 `CardArt.MenuUi` 的查找名一致。
MENU_FROM_ART_DIR = 'd:/4/Unity/MyGame/Assets/CardPresentation/Art/原版/去重资源'
MENU_FROM_ART = [
    'WF_Special offer_Value',                 # 连登窗的 `Collect` 底 / 每日奖励的 `Gacha Reward Claimed` 底
    # 🆕 2026-09-27：段位块 `Ranked Division Info/Content` 里 `RankedSealStep` 的**未填充态**
    #   （`Rank Skull Empty`）。两个 `RankedSealStep` 自身的 Image 是 `Rank Skull` 但 **a=0（不画）**，
    #   看得见的就是 `Empty`（`Rank Skull Empty`）与 `Fill`（`Rank Skull`）——
    #   所以我们至少要有 `Empty` 那一张。extract 缓存里 glob 不到名字 ⇒ 走工程切片库。
    #   判据 → `资料/普查产出_0927/段位块_RankedDivisionInfo.md` §A 第 84-89 行。
    'Rank_Skull_Empty',
    # 🆕 2026-10-12（A424）：对战内设置面板那一行的**勾**
    #   （原版 `BattleSettingsPanel/Auto Zoom Toggle/Toggle/CheckMark` 的 `40K_settings_icon_checkmark`，
    #   66×51、无 border）。extract 缓存里 glob 不到名字 ⇒ 同上走工程切片库。
    #   底图 `40K_dropdown_bg` **不用加**：`ui_menu/40k_dropdown_bg.png` 早就在
    #   （与切片库那张 `40K_dropdown_bg.png` **逐字节相同**，md5 `2e40bc4d1923680c93fef6d42445aea4`，2026-10-12 核过）。
    #   判据 → `Battle/SettingsPanel.cs` 那组 `Az*` 常量。
    '40K_settings_icon_checkmark',
]

# ---- 督军**异画**（Alternate Art）7 张 —— 2026-09-24 加（阶段二第 3 层「卡组线」Styles 页）--------
#
# 🔴 **命名坑（先读这条）**：原版这批文件名用的是 **`AA_HB_` / `_AA_`** 前缀/后缀
#   （`AA` = Alternate Art · `HB` = **Hammer and Bolter** 这个风格 ID）。
#   2026-09-23 那次普查搜的是 `alternate` / `altart` / `variant` / `skin` ⇒ **一个都不命中**，
#   于是把整页判成「本机零副本」—— 是铁律 5「翻过一个镜像目录就写本地没有 = 一定会错」的又一例。
#   用户 2026-09-24 拿文件名来问才搜到（`资料/阶段二_卡组线_原版规格.md` §七 ③b）。
#
# **绑定规则**（反编译印证）：一件异画 **绑死在一张卡上**，不能给别的督军用
#   （`AlternateArtCard.GetIdForClonedCard()` = 对原卡资产 id 做一次 `String.Replace` 得到克隆 id；
#    `AlternateArtInventory.OnFinishUnpack` 再 `AssetLocator.GetAsset(该 id)`）⇒ **一一对应**。
#
# **本机只有这 7 张 · 只覆盖 2 种风格**：`AA_HB`（6 张）+ `v2`（Azrael 那张）。
#   其余风格在**远端 CCD 的 `alternateartstyles` 包**（本机从未下载）。
# 目标目录 `Resources/Art/altarts/alt_<卡 id 小写>.png` —— **按 id 命名**（同 `art_<id>.png` 的理由：
#   卡名跨阵营会撞车）。源图与普通立绘**同一裁法**（`Sprite/textureRect` = x176.5 y0 w670.5 h1024）。
ALT_OUT = 'd:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/altarts'
ALT_ART = [
    # (引擎卡 id, 风格, assets_full 下的相对路径)
    ('AM5',                   'AA_HB', 'bundle_astramilitarumcardassets_assets_all/Texture2D/AA_HB_AstraMilitarum_war_Ursula Creed.png'),
    ('BL1',                   'AA_HB', 'bundle_chaosspacemarinesblacklegioncardassets_assets_all/Texture2D/AA_HB__BlackLegion_war_Abaddon the Despoiler.png'),
    ('DA3',                   'v2',    'bundle_spacemarinesdarkangelscardassets_assets_all/Texture2D/DarkAngels_AA_warlord_Azrael_v2.png'),
    ('SAU1',                  'AA_HB', 'bundle_necronssautekhcardassets_assets_all/Texture2D/Necron_Sautekh_warlord_Imotekh the Stormlord_AA_HB.png'),
    ('GOF3',                  'AA_HB', 'bundle_orksgoffcardassets_assets_all/Texture2D/AA_HB_Ork_Goff_war_Ghazghkull Thraka.png'),
    ('SW1',                   'AA_HB', 'bundle_spacemarinesspacewolvescardassets_assets_all/Texture2D/AA_HB_SpaceWolves_war_Logan Grimnar.png'),
    ('UM_Lieutenant_Titus',   'AA_HB', 'bundle_spacemarinesultramarinescardassets_assets_all/Texture2D/AA_HB_Ultramarines_war_Lieutenant Titus.png'),
]


def altart_jobs():
    """7 张督军异画 → `altarts/alt_<id>.png`。**只取真实存在的那几张**，缺哪张打出来（不静默）。"""
    out = []
    for cid, _style, rel in ALT_ART:
        # 🔴 **必须用 `os.path.join` 拼**，不能直接把带 `/` 的 rel 交给 `os.path.join(UNPACK, rel)`：
        #    `sprite_rect()` 是按 **`os.sep`（Windows = `\`）**去找同名 `Sprite\<名>.json` 的
        #    ⇒ 路径里是 `/` 时那句 `replace` **一个都不命中**、静默返回 None、**整张 1024² 拷过去**
        #    （实测：第一次导出来的 7 张全是 1024×1024，而正常的立绘是裁到 670.5×1024 的）。
        #    **症状很安静**：图还是在、卡面照画，只是四周多一圈空、比例也不对。
        src = os.path.join(UNPACK, *rel.split('/'))
        if not os.path.exists(src):
            print(f'⚠️ 异画源文件不在（跳过）：{rel}')
            continue
        out.append((src, os.path.join(ALT_OUT, f'alt_{cid.lower()}.png'), True))
    return out


# ---- 战役阵营背景（13 张 1024²，**另一个 bundle**）—— 2026-09-23 加（阶段二第 3 层「战役页」）--------
# 判据：`Ultramarines Campaign.json` 的 `Background.m_AssetGUID` = `2cca2c1f…` → `Campaign_Faction_Bck_Ultramarines`
#   ⇒ **GUID ↔ 阵营 已闭环**（13 组的映射表见 `资料/阶段二_锻造厂与战役页_原版规格.md` §十二）。
# ⚠️ 13 张都是 **1024×1024 整图**（`m_Rect = 0,0,1024,1024`）⇒ **整图即 sprite，不用按 textureRect 裁**。
CAMPAIGN_BG_SRC = 'd:/2/新解包资源/assets_full/bundle_campaignrewardbackgrounds_assets_all/Texture2D'
CAMPAIGN_BG_OUT = 'd:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/ui_menu'   # 与菜单图同目录（`CardArt.MenuUi` 已按多批兜底，不必新增目录）
CAMPAIGN_BGS = [
    'Campaign_Faction_Bck_AstraMilitarum', 'Campaign_Faction_Bck_BlackLegion',
    'Campaign_Faction_Bck_Dark Angels',    'Campaign_Faction_Bck_Emperors Children',
    'Campaign_Faction_Bck_Genestealers',   'Campaign_Faction_Bck_Leviathan',
    'Campaign_Faction_Bck_Orks',           'Campaign_Faction_Bck_Saim-Hann',
    'Campaign_Faction_Bck_Sautekh',        'Campaign_Faction_Bck_Sororitas',
    'Campaign_Faction_Bck_Space Wolves',   'Campaign_Faction_Bck_Tau_Empire',
    'Campaign_Faction_Bck_Ultramarines',
]

# ---- 🆕 2026-09-27：装饰品头像（玩家档案窗 Avatar 页）------------------------------------
# 出处：`资料/普查产出_0927/` 那批普查 + `d:/4/Unity/素材/Warpforge原版/装饰品/`（整包镜像）。
# 🔴 **清单与图必须同名**：`工具/gen_profile_cosmetics.py` 把 SO 的 `m_Name` 当 `art` 字段写进
#    `Resources/profile_cosmetics.json`，而 SO 的 `m_Name` 就是这里的文件名（实测逐条同名）。
# ⚠️ 含**空格**（`Avatar_UM_Attack Bike.png`）—— 与 `MENU_IMAGES` 那批「空格换下划线」的规矩**不同**，
#    别顺手替换（替换了 `CardArt.Cosmetics` 就找不着）。
COSMETIC_AVATAR_SRC = 'd:/4/Unity/素材/Warpforge原版/装饰品/头像/Texture2D'
COSMETIC_AVATAR_OUT = 'd:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/avatars'

# ---- Unity **内置** UI 图（`bundle_Warpforge_unitybuiltinassets`，2026-09-23 加）------------
# 🔴 为什么单列一份：这批图**不在任何游戏图集里**，而是 Unity 自带 UI skin 的贴图，
#    被原版预制体直接引用。extract 缓存（`MENU_SRC`）里**一张都没有** ⇒ 只能从
#    `assets_full` 的**内置资源包**按 Texture2D 取。
#    ⚠️ 别去 `Library/PackageCache/com.unity.ugui/...` 里翻 —— 那里只有文档图。
# 判据：卡组线 Cards 页筛选栏第 1 行的搜索框 = `Img[InputFieldBackground] type=Sliced col=(0.0627,0,0,1)`
#   （`menu_dump.py bundle_menus_assets_all -8460121208602172715`）；
#   实测该 sprite = **32×32、`m_Border=(10,10,10,10)`**（九宫格），PNG 与 `m_Rect` 同大小 ⇒ 整图即图。
BUILTIN_SRC = 'd:/2/新解包资源/assets_full/bundle_Warpforge_unitybuiltinassets/Texture2D'
BUILTIN_IMAGES = [
    'InputFieldBackground',      # 搜索框 / 导入框的底（九宫格）
    # 🆕 2026-09-27：**排行榜行底**（`PlayerRankingRow/Background` 与 `/BackgroundHighlight`）。
    #   判据 → `资料/普查产出_0927/排行榜_轮抽.md` §A·4：两个 pid（1660267235368898380）
    #   在 `Warpforge_unitybuiltinassets.bundle` 里 = Unity 内建那张 `Background`
    #   （32×32 · 九宫 10,10,10,10 · ppu 200），**不是**原版美术 ⇒ 只能从内置包取。
    #   两行用的是**同一张图**，只靠 `m_Color` 区分（0.83,0.192,0.428,0.165 / 0.978,1,0,0.165）。
    'Background',
]

# ---- 卡背 233 张 → `Resources/Art/cardbacks/`（2026-09-23 加）------------------------------
# 🔴 **这一段是补的** —— 原来**根本没有导卡背的代码路径**：`资料/阶段二_卡组线_原版规格.md` §七 ③
#    写着「先导 233 张卡背（用本脚本）」，但脚本里只有 `BACKS` 那 **4 张**「阵营默认背」（进 `Art/cards/back_*.png`）。
# 判据（2026-09-23 普查 + 本机实测）：
#   · 源 = `bundle_cosmeticscardbacksimages_assets_all/Texture2D/Cardback_<族>_<名字>.png` ——
#     **233 张 1024² 图集**（`Sprite/` 另有 466 个 json = 233 个 `_Main` + 233 个 `_SDF`）
#   · 卡背 = **`_Main` 那个 sprite 的 `textureRect`**（**每张都不一样**！实测三张分别 707×995.9 / 707×1016.9 / 707×966.9，
#     x 都是 158 —— ⚠️ **别照抄某一个值当全部**，铁律 5·c）⇒ **逐张读 `Sprite/<名>_Main.json` 再裁**
#   · 裁完与工程外的Pre-cut 版（`素材/Warpforge原版/卡背/*.png`）**逐像素一致**（本机抽样 3 张核过）
#   · `_SDF` sprite 本轮**不导**（Cosmetics 格只用 `Cardback` 那张）
#   · 目标目录**新开** `Resources/Art/cardbacks/` —— 别写进 `Art/cards/`：
#     那里已有 4 张 `back_<阵营>.png`（**语义不同**：牌堆用的阵营默认背），会撞名/混义
CARDBACK_SRC = 'd:/2/新解包资源/assets_full/bundle_cosmeticscardbacksimages_assets_all'
CARDBACK_OUT = 'd:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/cardbacks'

TRAIT_SRC = 'd:/4/Unity/素材/Warpforge原版/UI图集/图集/40ktraiticonatlas/slices'
TRAIT_OUT = 'd:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/traits'
# 目标文件名 = 源文件名**去掉这个前缀**。图集里两类图共用 `Atlas_` 打头：
#   `Atlas_trait_icon_<英文关键词>.png` 73 张（关键词图标）
#   `Atlas_SpiritStone_<1..5>.png`       5 张（灵族灵魂石，2026-09-15 补 —— 见下面 `TRAIT_PREFIXES`）
TRAIT_PREFIX = 'Atlas_trait_icon_'
# ⚠️ 2026-09-15 更正：原来只认 `Atlas_trait_icon_` 一个前缀 ⇒ **5 张 `Atlas_SpiritStone_*`
#    被静默跳过**（脚本不报错，`traits/` 里就是没有它们），而 `资料/卡面图标_现状与缺口.md:48`
#    与 `资料/关键词图标/关键词与图标_对照表.md:127-131` 两边都把它们算进「78 张」了
#    ⇒ 文档说 78、盘上 73，差的就是这 5 张。现在两个前缀都收。
#    ⚠️ **别把 `Atlas_SpiritStone_` 也写进来** —— 那会剥成 `1.png`..`5.png`（实测踩过：
#    目录里多出五个没名字的 `1.png`，`CardArt.Trait("SpiritStone_1")` 反而取不到）。
#    规矩：**长的先试，剥掉的那个前缀就是文件名里多余的整段**。
TRAIT_PREFIXES = ('Atlas_trait_icon_', 'Atlas_')

# ---- 特效贴图（不是 UI 图集的切片，是从 bundle 里单独抽出来的）------------------
# ⚠️ 这几张在**源 bundle** 里，不在 `slice_battle_atlas.py` 的产物里，所以要**先抽到备查库**：
#   `素材/Warpforge原版/特效贴图/`（用 UnityPy 从
#   `battlesharedresources_assets_all.bundle` 按 PathID 抽，见 `资料/规则引擎_进度与交接.md`）
FX_SRC = 'd:/4/Unity/素材/Warpforge原版/特效贴图'
FX_TEXTURES = [
    # 准星弧线的拖尾贴图（材质 `CroshairTrail` 的 `_MainTex`，64×64 横向亮度渐变）
    'CrosshairTrail',
]


def find_card_texture(basename: str) -> str:
    """按**贴图名**（不带扩展名）在新解包里找。查不到返回 ''（调用方当缺文件报出来）。"""
    import glob as _glob
    hits = _glob.glob(f'{UNPACK}/*/Texture2D/{basename}.png')
    return hits[0] if hits else ''


def sprite_rect(png_path):
    """同名 `Sprite/<名字>.json` 里的 `m_RD.textureRect`（sprite 在纹理里的真实矩形）。
    查不到就返回 None —— 调用方整张拷，不猜。"""
    import json
    sp = png_path.replace(os.sep + 'Texture2D' + os.sep, os.sep + 'Sprite' + os.sep)[:-4] + '.json'
    if not os.path.exists(sp):
        return None
    try:
        with open(sp, encoding='utf-8') as f:
            tr = json.load(f)['m_RD']['textureRect']
        return (int(round(tr['x'])), int(round(tr['y'])),
                int(round(tr['x'] + tr['width'])), int(round(tr['y'] + tr['height'])))
    except Exception:
        return None


def bleed_edge_rgb(im, ring=8):
    """把「角色轮廓外一圈」的 RGB 换成**角色自己的颜色**（再往外不动），alpha 原样保留。

    ⚠️ **2026-09-13 第二次更正：别再二值化 alpha 了。** 二值化（>127→255）虽然压掉了
    「同一张图叠两次糊成马赛克」，但它把那圈**半透明带里带着背景残渣色**的像素变成了**不透明**，
    于是角色轮廓外面多出**一圈亮边**（用户报的「黑边/白边」）。
    实测（`art_heavy_intercessor.png`）：实心边界像素 RGB 均值 **(98,101,112)**，内部只有 (52,52,67) ——
    边界亮了近一倍；而**原始 bundle 贴图**里 alpha=255 的边界是 (45,48,64) ≈ 内部，**没有亮边** ⇒ 亮边是我们处理出来的。

    根因为什么是残渣色：那张贴图的 alpha 是**软遮罩**（1024² 里有 255 档），而**透明/半透明那片的 RGB
    是背景**（实测 alpha 1–64 那档 RGB 均值 (167,167,169)，是天空/火光）。按 alpha 混色时它会被混出来。

    所以正确做法是：**保留软 alpha**（角色边才自然、也才不会有硬边）
    + 把边上一圈的颜色**渗成角色自己的颜色**（残渣色被替换 → 马赛克和白边一起消失）。
    ⚠️ **只渗 `ring` 像素**：再往外是完整插图，`ArtOpaque` 底层要拿它补拱窗里的背景，不能动。
    """
    import numpy as np
    from PIL import Image
    a = np.asarray(im).astype(np.float32)
    rgb, alpha = a[:, :, :3].copy(), a[:, :, 3]
    known = alpha >= 250                      # 「确定属于角色」的核心像素
    out = rgb.copy()
    for _ in range(ring):
        acc = np.zeros_like(out)
        cnt = np.zeros(known.shape, np.float32)
        for dy in (-1, 0, 1):
            for dx in (-1, 0, 1):
                if dx == 0 and dy == 0:
                    continue
                k = np.roll(np.roll(known, dy, 0), dx, 1)
                acc += np.roll(np.roll(out, dy, 0), dx, 1) * k[:, :, None]
                cnt += k
        newly = (~known) & (cnt > 0)
        if not newly.any():
            break
        out[newly] = acc[newly] / cnt[newly][:, None]
        known = known | newly
    a[:, :, :3] = np.clip(out, 0, 255)
    return Image.fromarray(a.astype(np.uint8))


def fix_art_meta(dst):
    """把插图的 `.meta` 里 `alphaIsTransparency` **关掉**。

    ⚠️ **为什么必须关**（2026-09-13 实测）：Unity 开着它时会把**透明区的 RGB 用「最近邻填充」补上** ——
    而最近邻填充的划分边界正好是**多边形格子**，看起来就是一片**彩色马赛克**。
    软 alpha 的插图一导进去，整张卡面就变成马赛克（二值 alpha 时不触发，所以以前没发现）。
    而我们**恰恰要用透明区的 RGB** —— 底层 `ArtOpaque` 拿它补卡框拱窗里的背景；被 Unity 填掉之后底层就是马赛克。
    Alpha 的语义由我们自己的两个 shader（`ArtOpaque` + `Sprites/Default`）控制，**不需要 Unity 插手**。

    ⚠️ `.meta` 是 Unity 生成的：**第一次导入（还没开过 Unity）时它不存在** → 这里跳过。
    开过一次 Unity 之后再跑一遍本脚本即可（脚本是幂等的）。
    """
    meta = dst + '.meta'
    if not os.path.exists(meta):
        return False
    s = io.open(meta, encoding='utf-8').read()
    if 'alphaIsTransparency: 1' not in s:
        return False
    io.open(meta, 'w', encoding='utf-8', newline='').write(
        s.replace('alphaIsTransparency: 1', 'alphaIsTransparency: 0'))
    return True


def write_cardback(src, dst):
    """卡背：按同名 `Sprite/<名>_Main.json` 的 `textureRect` 裁到 `dst`。

    ⚠️ **每一张的 rect 都不一样**（实测三张：707×995.9 / 707×1016.9 / 707×966.9，x 恒 158）
       —— 别按某一张的值常量裁（铁律 5·c）。
    ⚠️ Unity 的 `m_Rect.y` 是**从底边**量的 ⇒ 换成 PIL 的上边距 = `H − (y + h)`。
       本机抽样 3 张核过：裁出来的尺寸与工程外那份 pre-cut 版**逐像素一致**。
    查不到 json 就**整张拷**（不猜矩形），返回 False。
    """
    import json as _json
    from PIL import Image
    im = Image.open(src).convert('RGBA')
    sp = os.path.join(CARDBACK_SRC, 'Sprite', os.path.basename(src)[:-4] + '_Main.json')
    if not os.path.exists(sp):
        im.save(dst)
        return False
    with open(sp, encoding='utf-8') as f:
        tr = _json.load(f)['m_RD']['textureRect']
    W, _H = im.size
    x, y, w, h = (int(round(tr[k])) for k in ('x', 'y', 'width', 'height'))
    top = _H - (y + h)
    im.crop((x, top, x + w, top + h)).save(dst)
    return True


def cardback_jobs():
    """233 张卡背 → `cardbacks/`（**逐张裁** `_Main` 的 textureRect，见 `write_cardback`）。"""
    tex_dir = os.path.join(CARDBACK_SRC, 'Texture2D')
    if not os.path.isdir(tex_dir):
        return []
    return [(os.path.join(tex_dir, fn), os.path.join(CARDBACK_OUT, fn))
            for fn in sorted(os.listdir(tex_dir)) if fn.endswith('.png')]


# ---- 🆕 2026-09-26：卡背的 **SDF 掩码**（`Cardback_*_SDF`）---------------------------------
# 为什么单开一段（`项目任务.md` §三 第 8b 条）：
#   · 原版 `CosmeticItemCardback.GetCardBackSprites()` **成对返回（主卡背, SDF）**
#     —— 喂的就是**同一张卡背自己的 `_SDF`**（`CollectionCosmetic__Config.c:23-32`）。
#   · 🔴 **它用在两处、不是一处**：**① 收藏窗的卡背格**（`CollectionCosmetic`：主 `+0x48` / SDF `+0x50`）·
#     **② 战斗牌堆**（`DeckManager`：主 `+0x50` / SDF `+0x58`，字段名就叫 `cardbackShadow`）。
#   · 🔴 **`_SDF` 不是 `_Main` 的缩放版**：`_Main` 707×996（PPU 50）、`_SDF` **100×130.5**，
#     **宽高比都不一样**（0.7099 vs 0.7663）—— 两者挤在**同一张 1024² 图集**里（实测
#     `Cardback_UM_Astartes_SDF` 的 textureRect = x900 y447.25 w100 h130.5）
#     ⇒ **必须按各自的 `textureRect` 裁**，别拿 `_Main` 的矩形去切（铁律 5·c：一个值 ≠ 全部情况）。
#   · 低分辨率是 SDF 的本意（距离场就是要糊的），**不是缩略图**。
def write_cardback_sdf(src, dst):
    """按同名 `Sprite/<名>_SDF.json` 的 `textureRect` 裁出 SDF 掩码。查不到 json 就返回 False（**不猜矩形**）。"""
    import json as _json
    from PIL import Image
    im = Image.open(src).convert('RGBA')
    sp = os.path.join(CARDBACK_SRC, 'Sprite', os.path.basename(src)[:-4] + '_SDF.json')
    if not os.path.exists(sp):
        return False
    with open(sp, encoding='utf-8') as f:
        tr = _json.load(f)['m_RD']['textureRect']
    W, _H = im.size
    x, y, w, h = (int(round(tr[k])) for k in ('x', 'y', 'width', 'height'))
    top = _H - (y + h)                      # Unity 的 rect.y 从**底边**量
    im.crop((x, top, x + w, top + h)).save(dst)
    return True


def cardback_sdf_jobs():
    """233 张卡背的 `_SDF` → `cardbacks/<原名>_sdf.png`（与 `_Main` 那张**同名加后缀**，好对）。"""
    tex_dir = os.path.join(CARDBACK_SRC, 'Texture2D')
    if not os.path.isdir(tex_dir):
        return []
    return [(os.path.join(tex_dir, fn), os.path.join(CARDBACK_OUT, fn[:-4] + '_sdf.png'))
            for fn in sorted(os.listdir(tex_dir)) if fn.endswith('.png')]


def write_portrait(src, dst):
    """卡牌插图：**裁到 sprite rect** 后存 dst（**保留 alpha**，但把边上一圈的颜色渗成角色的）。返回「这张卡有没有抠图」。

    ⚠️ 2026-09-13 更正：原来这里把 alpha **强行写成 255**（理由「解码残渣」）是错的 ——
       那个通道就是**角色抠图**，写掉之后「角色越出卡框」的立体感就没了（用户看出来的）。详见文件头。
    ⚠️ 2026-09-13 第二次更正：后来改成**二值化**（>127→255）也不对 —— 那会给角色描一圈**亮边**
       （用户报的「黑边/白边」）。现在改成：**软 alpha 原样留 + 只把边上 `ring` 像素的颜色渗成角色的**。
       见 `bleed_edge_rgb` 的注释（含实测数字）。
    ⚠️ 裁 sprite rect 仍然是必须的（原版画的只是 sprite 那块，不裁四周是纹理里多余的内容）。
    """
    from PIL import Image
    im = Image.open(src).convert('RGBA')
    r = sprite_rect(src)
    if r:
        im = im.crop(r)
    im = bleed_edge_rgb(im)
    im.save(dst)
    a = im.getchannel('A')
    hist = a.histogram()
    return hist[0] / float(sum(hist) or 1) > 0.05


def portrait_jobs():
    """原版 13 阵营的卡牌插图 → `art_<卡名>.png`（`CardArt.Portrait()` 按这个取）。

    来源：**新解包** `D:/2/新解包资源/assets_full/bundle_<阵营>cardassets_assets_all/Texture2D/`，
    文件名形如 `SM_UM_inf_Heavy Intercessor.png` —— 前缀是「系列_阵营_类型_」，**卡名在后面**，
    所以**按卡名反查最稳**：把卡名归一化（只留小写字母数字）后在文件名里找子串，取最长命中。

    ⚠️ 有 20 来张卡面文件名的拼写和卡表对不上（`predator anihilator` 少个 n、
    `Scion-Medic` 词序反、`Hellsfire` vs `Hellfire`），所以再加一层 `difflib` 相似度 ≥ 0.86 兜底。
    兜不住的**会打出来**（不静默少图）。

    🔴 **输出文件名用卡 `id`**（`art_<id 小写下划线>.png`），**不是卡名** —— 同名跨阵营会互相覆盖，
    见下面那段注释。⚠️ 我们自己设计的那 26 张（`PORTRAITS`）没有引擎 id，仍按**卡名**命名。
    ⚠️ 裁 rect + 补 alpha 的细节见 <see cref="write_portrait"/>。
    ⚠️ 原版资产，进 `.gitignore` 掉的 `Resources/Art/`（理由是**别把大件塞进 git**）；🔴 2026-09-22 更正：~~发布前整个删~~ 这条口径已作废。

    🔴 **2026-10-07 契约修复（A159）**：本函数**返回三元组 `(jobs, unmatched, no_rect)`** ——
    原来只 `return jobs`、另两份读数**只 print**（旧 `:1163-1168`）⇒ 任何下游都只能去解析 stdout
    （`工具/collect_unmatched_art.py` 2026-10-06 就是这么绕的）。现在两份清单也**从返回值给出去**，
    stdout 照旧原样打印（**格式没动**，老读法仍然能用）。
    ⚠️ **`jobs` 为空有两种含义，调用方必须自己判**：① 真的没有可配的卡（今天不可能：卡池 1126 张）；
    ② **卡表或解包资源不在** —— 那种「0」是**假读数**（上面那两句 ⚠️ 之后直接 `return []`）。
    ⇒ `main()` 已按这条加了闸（`jobs` 空 ⇒ 出声 **且不写盘**）；别的调用方照抄那条闸，
    **别把 0 当结论**（照写下去会把 `card_cutouts.json` 的 1126 条静默清空）。
    """
    cards_json = 'd:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json'
    if not os.path.exists(cards_json):
        print('⚠️ 找不到卡表，跳过插图同步：', cards_json)
        return []
    if not os.path.isdir(UNPACK):
        print('⚠️ 找不到新解包资源：', UNPACK)
        return []

    import json, re, glob, difflib
    with open(cards_json, encoding='utf-8') as f:
        cards = json.load(f)['cards']

    def norm(s):
        return re.sub(r'[^a-z0-9]', '', (s or '').lower())

    def best_window_ratio(name_norm, file_norm):
        """卡名 vs 文件名里**最像的一段**（同长度的连续窗口）。

        ⚠️ 不能拿卡名直接跟整个文件名比：文件名带前缀（`CSM_BlackLegion_strat_Helfire Torch`），
        整串比下来相似度只有 0.55 —— 「Hellfire Torch」和「Helfire Torch」这种**只差一个字母**的
        也会被判成配不上（2026-09-12 就是这么漏掉 18 张的）。
        窗口比法：在文件名里滑一个跟卡名等长（±30%）的窗，取最高分。
        """
        if not name_norm or not file_norm:
            return 0.0
        n = len(name_norm)
        lo, hi = max(1, int(n * 0.7)), min(len(file_norm), int(n * 1.3))
        best = 0.0
        for w in range(lo, hi + 1):
            for i in range(0, len(file_norm) - w + 1):
                r = difflib.SequenceMatcher(None, name_norm, file_norm[i:i + w]).ratio()
                if r > best:
                    best = r
                    if best >= 0.99:
                        return best
        return best

    jobs, unmatched, no_rect = [], [], []
    for fac in card_bundle_map():
        d = card_bundle(fac)
        folder = os.path.join(UNPACK, d, 'Texture2D')
        if not os.path.isdir(folder):
            # 🔴 2026-10-07（A159）：这一档**会静默少图** —— 该阵营的卡既不在 `jobs` 也不在 `unmatched`，
            #   下游（`main()` 写 `card_cutouts.json` 那一步）会照写一份**少掉它们的全量清单**。
            #   原来只是一句 ⚠️（夹在几十行输出里看不见）⇒ 现在点名说清后果。
            #   ⛔ 不在这条路上硬退出：那会把「只想补一个阵营」的正常用法也堵死；总闸在 `main()`。
            print(f'🔴 缺目录：{folder} ⇒ **{fac} 这一整个阵营的插图这次全都没导**'
                  f'（它们也不会进 `配不上` 清单 —— 下游的 `card_cutouts.json` 会少掉它们）')
            continue
        files = [p for p in glob.glob(os.path.join(folder, '*.png'))
                 if 'Cardframe' not in os.path.basename(p)]
        # ⚠️ 池子里放**整条卡**而不是只放名字 —— 文件名要用 `id`（见下面那段注释）
        pool = [c for c in cards if c['faction'] == fac and c.get('id')]
        nf = {p: norm(os.path.basename(p)[:-4]) for p in files}
        taken = set()
        # ⚠️ **按卡名从长到短处理**：短名字会把长名字的图抢走 ——
        #    `Deffkopta` 也匹配得上 `Orks_Goff_veh_Mega Blasta Deffkopta.png`，
        #    按卡表顺序轮的话它先到先得，`Mega Blasta Deffkopta` 反而配不上
        #    （2026-09-12 撞到：18 张「配不上」里至少有一张是这个原因）。
        #    先处理长名字 = 更具体的先认领。
        # 🔴 **按 `id` 命名输出文件**（2026-09-15）：原来按**卡名**命名（`art_<卡名>.png`），
        #    而卡池里有 **5 组同名跨阵营**的卡（`Terminator`/`Terminator Champion`/`Aggressor`/
        #    `Maulerfiend`/`Bladeguard Veteran`）⇒ 后写的那张**直接覆盖**前一张。
        #    实测：`art_aggressor.png` 与 `art_blackmane_aggressor.png` **逐字节相同**（太空野狼那张），
        #    而暗黑天使的 `DA12 Aggressor` 卡面是**深绿甲 + 兜帽红眼**的另一张画。
        #    出处：`资料/PnP卡图_逐张对账_0915.md` §五。
        #    C# 侧配套：`CardData.artId`（= 引擎卡 id）+ `CardArt.Portrait(artId)`。
        ordered = sorted(pool, key=lambda c: -len(norm(c['name'])))
        picked = {}                       # 卡 id → 命中的贴图路径

        # ⓪ **按 id 点名的**（`ART_ID_ALIAS`）：最优先，且**允许与别的卡共用同一张图**
        #    （原版自己就共用，见那张表上面的注释）。⚠️ 必须按 id：同名跨阵营的卡按卡名点会连带改错。
        # ⚠️ **这里故意不写进 `taken`** —— 点名的那张图往往还有**另一张同名/孪生的卡**要一起用
        #    （`Righteous Fury` 与 `Exemplary Warrior` 共用一幅画；
        #     `Hellfire Torch` 与 `Helfire Torch` 是卡池里的重复行，等等）。
        #    占住它反而会把孪生卡挤成「配不上」（2026-09-15 第一版就是这么写的，实测挤掉 5 张）。
        for card in ordered:
            tex = ART_ID_ALIAS.get(card['id'])
            if not tex:
                continue
            p = os.path.join(folder, tex + '.png')
            if p in nf:
                picked[card['id']] = p
            else:
                unmatched.append(f'{fac}/{card["name"]}（点名表写的 {tex}.png 不存在）')

        # ① **精确子串**：**全部卡先跑完这一轮**，跑完才进 ② 兜底。
        #    🔴 2026-09-15 修：原来是「一张卡先跑①再跑②」，于是**模糊兜底会抢先认领**别人能精确命中的图 ——
        #    `Hellfire Torch`（卡池里的重复行）名字更长、先跑，精确匹配落空后用相似度 0.96 认领了
        #    `CSM_BlackLegion_strat_Helfire Torch.png`，**真正同名的那张反而「配不上」**。
        #    改名/重复行一多这类错会成片出现，所以拆成两轮。
        for card in ordered:
            if card['id'] in picked or not norm(card['name']):
                continue
            name = card['name']
            n = norm(name)
            # 卡面名和贴图名不一致的（个例表），匹配时**多试一个名字**
            n_alias = norm(ART_NAME_ALIAS.get(name, ""))
            hit, hit_key = None, None
            is_de = (card.get('type') == 'defence')
            for p in files:                       # 归一化子串
                if p in taken:
                    continue
                if n in nf[p] or (n_alias and n_alias in nf[p]):
                    # 排序键：**先看类型前缀对不对，再看文件名短不短**（短的 = 更具体的）。
                    # ⚠️ 类型这一条是必需的：原版对同一张图给了 `defence_` 和 `strat_` **两个变体**
                    #    （`GenestealerCults_defence_Alien Idol` vs `..._strat_Alien Idol`），
                    #    光按「谁短」会挑成 `strat_` —— 而 PnP 把 Alien Idol 印在 `5防御卡/` 里，
                    #    它是**防御卡**（2026-09-15 实测：改成只看长度会让 GSC66/67 配错变体）。
                    key = (1 if is_de != ('defence' in nf[p]) else 0, len(nf[p]))
                    if hit is None or key < hit_key:
                        hit, hit_key = p, key
            if hit is not None:
                picked[card['id']] = hit
                taken.add(hit)

        # ② **相似度兜底**（拼写/词序有出入、且 ⓪① 都没认领的那批）
        for card in ordered:
            if card['id'] in picked:
                continue
            n = norm(card['name'])
            if not n:
                continue
            best, score = None, 0.0
            for p in files:
                if p in taken:
                    continue
                r = best_window_ratio(n, nf[p])
                if r > score:
                    best, score = p, r
            if best is not None and score >= 0.86:
                picked[card['id']] = best
                taken.add(best)

        for card in ordered:
            if not norm(card['name']):        # 无名卡照旧静默跳过（与原来一致）
                continue
            hit = picked.get(card['id'])
            if hit is None:
                unmatched.append(f'{fac}/{card["name"]}')
                continue
            if sprite_rect(hit) is None:
                no_rect.append(os.path.basename(hit))
            jobs.append((hit, f'art_{slug(card["id"])}.png'))

    print(f'插图：配上 {len(jobs)} 张，配不上 {len(unmatched)} 张，缺 sprite rect {len(no_rect)} 张')
    for m in unmatched:
        print('   配不上:', m)
    for m in no_rect[:6]:
        print('   没裁（缺 textureRect，整张 1024² 直接拷）:', m)
    # 🔴 2026-10-07（A159）：**契约 = 返回三元组**（原来只 `return jobs`，这两份清单下游拿不到、只能解析 stdout）。
    #    ⚠️ 调用方 `main():1277` 已同步改成 3 元组解包 —— 老写法 `for src, name in portrait_jobs()` 会当场 ValueError。
    return jobs, unmatched, no_rect


def card_bundle_map():
    """13 个阵营名（`cards_engine.json` 里 `faction` 的取值）。"""
    return ['Ultramarines', 'Goff', 'SaimHann', 'Genestealers', 'DarkAngels', 'BlackLegion',
            'Sautekh', 'TauEmpire', 'Leviathan', 'AstraMilitarum', 'Sororitas',
            'EmperorsChildren', 'SpaceWolves']


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument('--check', action='store_true', help='只检查源文件在不在，不写')
    ap.add_argument('--only-menu', action='store_true',
                    help='只导**菜单 UI 图**（阶段二各层用）—— 跳过卡牌插图那一段（1126 张，最慢）。'
                         '⚠️ 这一模式**不重写** card_cutouts.json（那是全量产物，半量跑会把它清空）')
    ap.add_argument('--only-altart', action='store_true',
                    help='只导**督军异画那 7 张**（Styles 页用；秒回）—— 其余一段都不跑')
    ap.add_argument('--only-cardback-sdf', action='store_true',
                    help='只导**卡背那 233 张 SDF 掩码**（收藏窗卡背格 + 战斗牌堆那层用；秒回）—— 其余一段都不跑')
    args = ap.parse_args()

    # ---- 🆕 2026-09-26：`--only-cardback-sdf` —— 卡背的 **SDF 掩码**（自包含早退路径）----
    # 🔴 **必须早退、不能并进下面的 `jobs`** —— 同 `--only-altart` 那条已有的警告：
    #    那一趟会**重写 `card_cutouts.json`**（1126 张的全量清单）⇒ 半量跑会把全量清单**静默清空**。
    if args.only_cardback_sdf:
        js = cardback_sdf_jobs()
        print(f'卡背 SDF：待导 {len(js)} 张（按**各自的** `_SDF.json` 的 textureRect 裁）← {CARDBACK_SRC}/Sprite/')
        if not args.check:
            os.makedirs(CARDBACK_OUT, exist_ok=True)
        ok, cut, norect = 0, 0, []
        for src, dst in js:
            if not os.path.exists(src):
                continue
            if not args.check:
                if write_cardback_sdf(src, dst):
                    cut += 1
                else:
                    norect.append(os.path.basename(src))
            ok += 1
        print(f'{"检查" if args.check else "拷贝"}完成：{ok} / {len(js)}'
              f'（裁出来的 {cut} 张' + ('；`--check` 不写盘，所以这里恒 0' if args.check else '')
              + f'） → {CARDBACK_OUT}')
        if norect:
            print(f'  ⚠️ {len(norect)} 张找不到 `_SDF.json` ⇒ **没导**（不猜矩形）：', norect[:5])
        print('⚠️ 导完在 Unity 里跑一次 `ArtBaker.ApplyImportSettings`')
        return 0

    # ---- `--only-altart`：一条**自包含的早退路径**（2026-09-24 加）----
    # 🔴 **必须早退、不能并进下面的 `jobs`** —— 理由两条，各踩一次就够：
    #   ① 下面 `for src,dst,crop in jobs` 那个循环里，`crop=True` 的分支会把**每一个**产物塞进 `cutouts`，
    #      而收尾那句会**重写 `card_cutouts.json`**（1126 张的全量清单）⇒ 一趟只导 7 张的跑，
    #      会把全量清单**静默清空**（同 `--only-menu` 那条已有的警告）。
    #   ② 菜单图/战役背景/内置图/关键词图标那几段是无条件加的，不早退就全跟着跑。
    if args.only_altart:
        aj = altart_jobs()
        print(f'督军异画：源 {len(aj)} 张 ← {UNPACK} 的 `bundle_*cardassets_assets_all/Texture2D/`'
              f'（ALT_ART 表里共 {len(ALT_ART)} 条）')
        if not args.check:
            os.makedirs(ALT_OUT, exist_ok=True)
        ok, miss, cuts = 0, [], []
        for src, dst, _crop in aj:
            if not os.path.exists(src):
                miss.append(src); continue
            if not args.check:
                # 返回「这张有没有角色抠图 alpha」⇒ 写一份**只属于异画**的清单
                # （**不能并进 `card_cutouts.json`** —— 那是全量产物，半量跑会把它清空）
                if write_portrait(src, dst):
                    cuts.append(os.path.splitext(os.path.basename(dst))[0])
            ok += 1
        print(f'{"检查" if args.check else "拷贝"}完成：{ok} / {len(aj)} → {ALT_OUT}')
        if not args.check and cuts:
            import json as _json
            man = os.path.join(ALT_OUT, 'alt_cutouts.json')
            with open(man, 'w', encoding='utf-8') as f:
                _json.dump({'note': '有「角色抠图 alpha」的异画（文件名 = alt_<卡 id 小写>）。'
                                    '由 工具/import_original_art.py --only-altart 生成，不要手改。',
                            'count': len(cuts), 'cards': sorted(cuts)},
                           f, ensure_ascii=False, indent=0)
            print(f'异画抠图清单：{len(cuts)} 张 → {man}')
        for m in miss:
            print('  缺:', m)
        print('⚠️ 这一模式**不碰** `card_cutouts.json`（那是全量产物，半量跑会把它清空）')
        print('⚠️ 导完记得跑 `ArtBaker.ApplyImportSettings`（否则新 PNG 没开 Read/Write）')
        return 0

    jobs = []                                   # (源路径, 目标文件名, 要不要裁成 sprite)
    if not args.only_menu:
        for key, src in FRAMES.items():
            if len(key) == 3:                      # (阵营, tier, 'strat') = 战术卡的框
                fac, tier, _ = key
                jobs.append((src, f'frame_{fac}_strat_tier{tier}.png', False))
                if tier == 1:
                    # 战术卡的默认框（不分 tier）也留一份，`CardArt` 取不到那一档时兜底
                    jobs.append((src, f'frame_{fac}_strat.png', False))
                continue
            fac, tier = key
            jobs.append((src, f'frame_{fac}_tier{tier}.png', False))
            if tier == 1:
                # 老名字（不带 tier）也留一份：`CardArt.Frame(阵营)` 那条旧取法不至于断
                jobs.append((src, f'frame_{fac}.png', False))
        for fac, name in BACKS.items():
            jobs.append((os.path.join(BACK_SRC, name), f'back_{fac}.png', False))
        # 我们自己设计的那 26 张卡**借**原版立绘（纯占位），同样按 sprite rect 裁
        for ours, rel in PORTRAITS:
            jobs.append((find_card_texture(os.path.basename(rel)), f'art_{slug(ours)}.png', True))

        # ---- 原版 13 阵营的**卡牌插图**（2026-09-12 加）----
        # 🔴 2026-10-07（A159）：`portrait_jobs()` 现在**返回三元组** `(jobs, unmatched, no_rect)`
        #    （原来只 `return jobs`，另两份只 print ⇒ 下游只能解析 stdout）。下面这道闸补的正是
        #    「一张都没配上**看着很正常**」那个洞：卡表/解包资源不在时它打完 ⚠️ 就 `return []`，
        #    照跑下去 `card_cutouts.json`（1126 条全量清单）会被写成少 1126 条，**且不报错**。
        pj_jobs, pj_unmatched, pj_no_rect = portrait_jobs()
        if not pj_jobs:
            print('🔴 一张插图都没配上 ⇒ 这是**假读数**（多半是卡表或新解包资源不在 —— 上面那两行 ⚠️ 说了是哪个）。')
            print('   ⇒ **本次不写盘**（`card_cutouts.json` 是全量清单，照写会把 1126 条**静默清空**）。')
            print('   （只想导菜单 UI 图 / 异画 / 卡背 SDF：走 `--only-menu` / `--only-altart` / `--only-cardback-sdf`）')
            return 2
        # ⚠️ 这三行是**返回值的用法示例**：`unmatched` / `no_rect` 的**逐条清单**在上面由
        #    `portrait_jobs()` 自己打印（那边才是判据源）；下游要**拿到清单本身**（点名/写文件）
        #    就从返回值取，**别再解析 stdout**。
        print(f'插图读数（返回值）：配上 {len(pj_jobs)} 张 · 配不上 {len(pj_unmatched)} 张 · '
              f'缺 sprite rect {len(pj_no_rect)} 张')
        if pj_unmatched:
            print(f'   ⚠️ {len(pj_unmatched)} 张配不上 —— 逐条清单在上面（`portrait_jobs()` 打的）')
        if pj_no_rect:
            print(f'   ⚠️ {len(pj_no_rect)} 张缺 sprite rect（整张 1024² 直接拷，没裁）')
        jobs += [(src, name, True) for src, name in pj_jobs]

        # UI 图单独一个目标目录，所以先把路径拼完整
        # （原来这 17 张是**手工拷的**，重建路径其实是断的 —— 2026-09-12 补上）
        jobs = [(src, os.path.join(OUT, name), crop) for src, name, crop in jobs]
        jobs += [(os.path.join(UI_SRC, n + '.png'), os.path.join(UI_OUT, n + '.png'), False) for n in UI_IMAGES]
        # 替代行动那五张走**另一个来源**（不在 battleatlasui 图集里，见上面那段注释）
        jobs += [(os.path.join(ALT_ACTION_SRC, n + '.png'),
                  os.path.join(UI_OUT, n.replace(' ', '_') + '.png'), False) for n in ALT_ACTION_IMAGES]
        jobs += [(os.path.join(FX_SRC, n + '.png'), os.path.join(UI_OUT, n + '.png'), False) for n in FX_TEXTURES]

    # ---- 菜单 UI 图（阶段二外壳）---- 空格 → 下划线，其余照抄切片名
    menu_jobs = [(os.path.join(MENU_SRC, bundle, 'Sprite', name + '.png'),
                  os.path.join(MENU_OUT, name.replace(' ', '_') + '.png'), False)
                 for name, bundle in MENU_IMAGES]
    print(f'菜单 UI 图：源 {len(menu_jobs)} 张（来自 {len(set(b for _n, b in MENU_IMAGES))} 个 bundle 目录）')
    jobs += menu_jobs

    # 战役阵营背景（13 张，另一个 bundle）—— 空格→下划线、落到 `ui_campaign/`
    bg_jobs = [(os.path.join(CAMPAIGN_BG_SRC, n + '.png'),
                os.path.join(CAMPAIGN_BG_OUT, n.replace(' ', '_') + '.png'))
               for n in CAMPAIGN_BGS]
    print(f'战役阵营背景：源 {len(bg_jobs)} 张 ← {CAMPAIGN_BG_SRC}')
    jobs += [(a, b, False) for a, b in bg_jobs]

    # ---- 🆕 2026-09-27：**装饰品头像**（玩家档案窗 Avatar 页要列的那一批）----
    # 🔴 为什么要整批导：那一页要**列出全部可选头像**（用户 2026-09-27 拍板「照填」），
    #    而普查实测 `Resources/` 里**只有 1 张**（`Avatar_UM_Intercessor`）。
    #    清单（470 条）由 `工具/gen_profile_cosmetics.py` 从同一批 SO 抽，**两边名字必须一致**：
    #    SO 的 `m_Name` 就是这里的文件名（实测逐条同名，例 `Avatar_UM_Attack Bike`）。
    # ⚠️ 落到**独立目录** `Art/avatars/`（别混进 `ui_menu/`）—— `CardArt.Cosmetics()` 只找它。
    # ⚠️ 名字里的**空格原样保留**（与 SO 的 `m_Name` 一致）；读的时候走 `CardArt.Cosmetics(art)`。
    if os.path.isdir(COSMETIC_AVATAR_SRC):
        cos_jobs = [(os.path.join(COSMETIC_AVATAR_SRC, fn),
                     os.path.join(COSMETIC_AVATAR_OUT, fn), False)
                    for fn in sorted(os.listdir(COSMETIC_AVATAR_SRC)) if fn.endswith('.png')]
        print(f'装饰品头像：源 {len(cos_jobs)} 张 ← {COSMETIC_AVATAR_SRC}')
        jobs += cos_jobs
    else:
        print(f'⚠️ 找不到装饰品头像目录 {COSMETIC_AVATAR_SRC} ⇒ 这一批没导（Avatar 页会缺图）')

    # ---- Unity 内置 UI 图（2026-09-23）—— 不在 extract 缓存里，只能从 assets_full 的内置包取
    builtin_jobs = [(os.path.join(BUILTIN_SRC, n + '.png'), os.path.join(MENU_OUT, n + '.png'), False)
                    for n in BUILTIN_IMAGES]
    print(f'Unity 内置 UI 图：源 {len(builtin_jobs)} 张 ← {BUILTIN_SRC}')
    jobs += builtin_jobs

    # 只在工程图集切片库里有的那几张（见 `MENU_FROM_ART` 的注释：extract 缓存里 glob 不到）
    for n in MENU_FROM_ART:
        fn = n.replace(' ', '_') + '.png'
        jobs.append((os.path.join(MENU_FROM_ART_DIR, fn),
                     os.path.join(MENU_OUT, fn), False))
    if MENU_FROM_ART:
        print(f'菜单 UI 图（工程图集切片库另补）：{len(MENU_FROM_ART)} 张 ← {MENU_FROM_ART_DIR}')

    # ---- 关键词图标（另一个图集，78 张全导）—— 2026-09-13 第三十二轮 ----
    # 目标文件名**去掉 `Atlas_trait_icon_` 前缀**：`CardArt.Trait("ephemeral")` 要按短名找
    # （和 `UI_IMAGES` 那条「文件名就是切片库里的名字」的约定不同 —— 这里的图集前缀是冗余的）。
    trait_jobs = []
    if not args.only_menu:
        if os.path.isdir(TRAIT_SRC):
            for fn in sorted(os.listdir(TRAIT_SRC)):
                if not fn.endswith('.png'):
                    continue
                # 前缀**从长到短**试，第一个命中的就是它 —— 别写成「只认 TRAIT_PREFIX」
                # （那正是 5 张 SpiritStone 被漏掉的原因，见文件头 `TRAIT_PREFIXES` 的注释）
                pre = next((p for p in TRAIT_PREFIXES if fn.startswith(p)), None)
                if pre is None:
                    print(f'  ⚠️ 图集里这张图不认识（不导）: {fn}')
                    continue
                trait_jobs.append((os.path.join(TRAIT_SRC, fn),
                                   os.path.join(TRAIT_OUT, fn[len(pre):]), False))
            print(f'关键词/灵魂石图标：源 {len(trait_jobs)} 张'
                  f'（其中 SpiritStone {sum(1 for _, d, _c in trait_jobs if "SpiritStone" in os.path.basename(d))} 张）')
        else:
            print(f'⚠️ 找不到关键词图标图集切片 {TRAIT_SRC} —— 这次**不导**关键词图标')
    jobs += trait_jobs

    if not args.check:
        os.makedirs(OUT, exist_ok=True)
        os.makedirs(UI_OUT, exist_ok=True)
        os.makedirs(MENU_OUT, exist_ok=True)
        os.makedirs(CAMPAIGN_BG_OUT, exist_ok=True)
        os.makedirs(COSMETIC_AVATAR_OUT, exist_ok=True)   # 🆕 2026-09-27 装饰品头像（漏了这句就会 `FileNotFoundError`）
        os.makedirs(CARDBACK_OUT, exist_ok=True)
        if trait_jobs:
            os.makedirs(TRAIT_OUT, exist_ok=True)

    ok, miss = 0, []
    cutouts = []                     # 有「角色抠图」的卡（slug），写进 card_cutouts.json
    meta_fixed = 0                   # ⚠️ 初值必须在这儿：`--check` 那条路不会进下面的 `if not args.check`
    #    （2026-09-15 修：原来它在 `if not args.check` 里初始化，`--check` 跑到底就 UnboundLocalError）
    for src, dst, crop in jobs:
        if not src or not os.path.exists(src):
            miss.append(src or '(空路径)')
            continue
        if not args.check:
            if crop:
                # 裁到 sprite rect（**保留 alpha**）+ 记下这张卡有没有抠图
                stem = os.path.splitext(os.path.basename(dst))[0]   # ⚠️ 别叫 slug —— 会和模块级的 slug() 撞名
                if write_portrait(src, dst):
                    cutouts.append(stem[len('art_'):] if stem.startswith('art_') else stem)
                    if fix_art_meta(dst):
                        meta_fixed += 1
            else:
                shutil.copyfile(src, dst)
        ok += 1

    print(f'{"检查" if args.check else "拷贝"}完成：{ok} / {len(jobs)}')

    # ---- 卡背 233 张（**逐张裁**，所以不走上面那个「整张拷」的循环）----
    cbs = cardback_jobs()
    if cbs:
        cb_ok, cb_cropped, cb_miss = 0, 0, []
        for src, dst in cbs:
            if not os.path.exists(src):
                cb_miss.append(src)
                continue
            if not args.check:
                if write_cardback(src, dst):
                    cb_cropped += 1
            cb_ok += 1
        print(f'卡背：{"检查" if args.check else "拷贝"} {cb_ok} / {len(cbs)}'
              f'（其中**按 textureRect 裁过的** {cb_cropped} 张'
              + ('；`--check` 不写盘，所以这里恒 0' if args.check else '；没裁的 = 找不到 `_Main.json`，整张拷了')
              + f'） → {CARDBACK_OUT}')
        for m in cb_miss:
            print('  缺:', m)
    else:
        print(f'⚠️ 卡背源目录不在（{CARDBACK_SRC}/Texture2D）—— 这次**没导卡背**')

    # ---- 🆕 2026-09-26：卡背的 **SDF 掩码** 233 张（同上，逐张按 `_SDF` 自己的 rect 裁）----
    cbs_sdf = cardback_sdf_jobs()
    if cbs_sdf:
        sd_ok, sd_cropped, sd_norect = 0, 0, []
        for src, dst in cbs_sdf:
            if not os.path.exists(src):
                continue
            if not args.check:
                if write_cardback_sdf(src, dst):
                    sd_cropped += 1
                else:
                    sd_norect.append(os.path.basename(src))
            sd_ok += 1
        print(f'卡背 SDF：{"检查" if args.check else "拷贝"} {sd_ok} / {len(cbs_sdf)}'
              f'（按 `_SDF` 自己的 textureRect 裁了 {sd_cropped} 张'
              + ('；`--check` 不写盘，所以这里恒 0' if args.check else '')
              + f'） → {CARDBACK_OUT}')
        if sd_norect:
            print(f'  ⚠️ {len(sd_norect)} 张找不到 `_SDF.json` ⇒ **没导**（不猜矩形）：', sd_norect[:5])
    elif not args.check:
        print('⚠️ 卡背 SDF：源目录不在 ⇒ 这次没导')

    if meta_fixed:
        print(f'插图的 .meta：关了 {meta_fixed} 个 alphaIsTransparency（不关的话透明区会被 Unity 填成马赛克）')
    elif not args.check:
        print('插图 .meta 没动（要么已经是关的，要么还没生成 —— **没生成的话开过一次 Unity 再跑一遍本脚本**）')

    if not args.check and not args.only_menu:
        # 抠图清单：运行时靠它决定「要不要画前景层（角色越出卡框）」
        # ⚠️ **`--only-menu` 时必须跳过** —— 那一趟 `cutouts` 是空的，写下去会把全量清单**静默清空**
        import json as _json
        man = os.path.join(OUT, 'card_cutouts.json')
        with open(man, 'w', encoding='utf-8') as f:
            _json.dump({'note': '有「角色抠图 alpha」的卡（slug）。由 工具/import_original_art.py 生成，'
                                '不要手改。判据：全透明像素 > 5%（战术卡 0%、单位卡 91–97%）。',
                        'count': len(cutouts), 'cards': sorted(cutouts)},
                       f, ensure_ascii=False, indent=0)
        print(f'抠图清单：{len(cutouts)} 张有角色抠图 → {man}')
    for m in miss:
        print('  缺:', m)
    if not args.check and ok:
        print('目标目录：', OUT)
        print('           ', UI_OUT)
        print('⚠️ 目标在 gitignore 的 Resources/Art/ 下（理由是**别把大件塞进 git**，不是版权 —— 2026-09-18 用户已取消版权红线）')
    return 1 if miss else 0


if __name__ == '__main__':
    sys.exit(main())
