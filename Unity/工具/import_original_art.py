#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""import_original_art.py — 把**原版美术**按我们的卡名/阵营拷进 Unity 工程（原版复刻用）

产出（全部落在 `MyGame/Assets/CardPresentation/Resources/Art/`）：

    cards/frame_<阵营小写>.png      阵营卡框（原版的**空框**，数值/名字由我们画在上面）
    cards/back_<阵营小写>.png       卡背（牌堆用）
    cards/art_<卡名小写下划线>.png  每张卡的立绘（原版插画，662×1024，干净无字）
    ui/<原切片名>.png               战斗 UI 图（HUD / 攻击方式按钮 / 选目标准星 / 高亮光圈）

⚠️ **版权**：这些是 Everguild / Games Workshop 的资产，只做「原版复刻」的参照，
   发布前整个 `Resources/Art/` 必须删掉（代码那边有 `CardArt.Available` 兜底，
   没有美术会退回程序生成的占位卡面）。`.gitignore` 已经把它排除了。

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
# ⚠️ `special`（39 张）**没有实测**，先按 tier4 处理（原版 `CardFramesSO` 只有 4 档，没有第 5 档）。
RARITY_TIER = {'common': 1, 'rare': 2, 'epic': 3, 'legendary': 4, 'special': 4, '': 1}

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
ART_NAME_ALIAS = {
    "Fire Warrior Marksman": "Fire Warrior Sniper",   # Tau_Empire_inf_Fire Warrior Sniper.png
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
    ⚠️ 原版资产，和卡框/卡背一个待遇：进 gitignore 掉的 `Resources/Art/`，发布前整个删。
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
            print(f'⚠️ 缺目录：{folder}')
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
        for card in sorted(pool, key=lambda c: -len(norm(c['name']))):
            name = card['name']
            n = norm(name)
            if not n:
                continue
            # 卡面名和贴图名不一致的（个例表），匹配时**多试一个名字**
            n_alias = norm(ART_NAME_ALIAS.get(name, ""))
            hit = None
            for p in files:                       # ① 归一化子串（取最长命中）
                if p in taken:
                    continue
                if (n in nf[p] or (n_alias and n_alias in nf[p])) and                    (hit is None or len(n) > len(norm(os.path.basename(hit)[:-4]))):
                    hit = p
            if hit is None:                       # ② 相似度兜底（拼写/词序有出入的那批）
                best, score = None, 0.0
                for p in files:
                    if p in taken:
                        continue
                    r = best_window_ratio(n, nf[p])
                    if r > score:
                        best, score = p, r
                if best is not None and score >= 0.86:
                    hit = best
            if hit is None:
                unmatched.append(f'{fac}/{name}')
                continue
            taken.add(hit)
            if sprite_rect(hit) is None:
                no_rect.append(os.path.basename(hit))
            jobs.append((hit, f'art_{slug(card["id"])}.png'))

    print(f'插图：配上 {len(jobs)} 张，配不上 {len(unmatched)} 张，缺 sprite rect {len(no_rect)} 张')
    for m in unmatched:
        print('   配不上:', m)
    for m in no_rect[:6]:
        print('   没裁（缺 textureRect，整张 1024² 直接拷）:', m)
    return jobs


def card_bundle_map():
    """13 个阵营名（`cards_engine.json` 里 `faction` 的取值）。"""
    return ['Ultramarines', 'Goff', 'SaimHann', 'Genestealers', 'DarkAngels', 'BlackLegion',
            'Sautekh', 'TauEmpire', 'Leviathan', 'AstraMilitarum', 'Sororitas',
            'EmperorsChildren', 'SpaceWolves']


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument('--check', action='store_true', help='只检查源文件在不在，不写')
    args = ap.parse_args()

    jobs = []                                   # (源路径, 目标文件名, 要不要裁成 sprite)
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
    jobs += [(src, name, True) for src, name in portrait_jobs()]

    # UI 图单独一个目标目录，所以先把路径拼完整
    # （原来这 17 张是**手工拷的**，重建路径其实是断的 —— 2026-09-12 补上）
    jobs = [(src, os.path.join(OUT, name), crop) for src, name, crop in jobs]
    jobs += [(os.path.join(UI_SRC, n + '.png'), os.path.join(UI_OUT, n + '.png'), False) for n in UI_IMAGES]
    jobs += [(os.path.join(FX_SRC, n + '.png'), os.path.join(UI_OUT, n + '.png'), False) for n in FX_TEXTURES]

    # ---- 关键词图标（另一个图集，78 张全导）—— 2026-09-13 第三十二轮 ----
    # 目标文件名**去掉 `Atlas_trait_icon_` 前缀**：`CardArt.Trait("ephemeral")` 要按短名找
    # （和 `UI_IMAGES` 那条「文件名就是切片库里的名字」的约定不同 —— 这里的图集前缀是冗余的）。
    trait_jobs = []
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
    if meta_fixed:
        print(f'插图的 .meta：关了 {meta_fixed} 个 alphaIsTransparency（不关的话透明区会被 Unity 填成马赛克）')
    elif not args.check:
        print('插图 .meta 没动（要么已经是关的，要么还没生成 —— **没生成的话开过一次 Unity 再跑一遍本脚本**）')

    if not args.check:
        # 抠图清单：运行时靠它决定「要不要画前景层（角色越出卡框）」
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
        print('⚠️ 版权：这是原版资产，发布前整个 Resources/Art/ 要删掉（.gitignore 已排除）')
    return 1 if miss else 0


if __name__ == '__main__':
    sys.exit(main())
