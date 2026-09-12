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
⚠️ **插图 alpha**：新解包里这批卡图的 **alpha 通道是解码残渣**（97% 的像素 alpha≈0，只有一小块是亮的，
   形状不像任何有意义的东西）。用 UnityPy 直接读 bundle 也是同样的结果，所以不是导出工具的锅。
   **卡图是满幅不透明的**（RGB 铺满整张方图、没有透明边），所以这里**统一把 alpha 写成 255**。
"""
import argparse
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
]

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


def write_portrait(src, dst):
    """卡牌插图：**裁到 sprite rect** + alpha 补成 255，存成 dst。

    ⚠️ 两个都是必须的，理由见文件头：
      · 不裁 → 四周是纹理里多余的内容、比例也不对（原版画的只是 sprite 那块）
      · 不补 alpha → 整张卡会变成全透明（这个通道是解码残渣）
    """
    from PIL import Image
    im = Image.open(src).convert('RGBA')
    r = sprite_rect(src)
    if r:
        im = im.crop(r)
    im.putalpha(Image.new('L', im.size, 255))
    im.save(dst)


def portrait_jobs():
    """原版 13 阵营的卡牌插图 → `art_<卡名>.png`（`CardArt.Portrait()` 按这个取）。

    来源：**新解包** `D:/2/新解包资源/assets_full/bundle_<阵营>cardassets_assets_all/Texture2D/`，
    文件名形如 `SM_UM_inf_Heavy Intercessor.png` —— 前缀是「系列_阵营_类型_」，**卡名在后面**，
    所以**按卡名反查最稳**：把卡名归一化（只留小写字母数字）后在文件名里找子串，取最长命中。

    ⚠️ 有 20 来张卡面文件名的拼写和卡表对不上（`predator anihilator` 少个 n、
    `Scion-Medic` 词序反、`Hellsfire` vs `Hellfire`），所以再加一层 `difflib` 相似度 ≥ 0.86 兜底。
    兜不住的**会打出来**（不静默少图）。

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
        pool = [c['name'] for c in cards if c['faction'] == fac]
        nf = {p: norm(os.path.basename(p)[:-4]) for p in files}
        taken = set()
        # ⚠️ **按卡名从长到短处理**：短名字会把长名字的图抢走 ——
        #    `Deffkopta` 也匹配得上 `Orks_Goff_veh_Mega Blasta Deffkopta.png`，
        #    按卡表顺序轮的话它先到先得，`Mega Blasta Deffkopta` 反而配不上
        #    （2026-09-12 撞到：18 张「配不上」里至少有一张是这个原因）。
        #    先处理长名字 = 更具体的先认领。
        for name in sorted(pool, key=lambda s: -len(norm(s))):
            n = norm(name)
            if not n:
                continue
            hit = None
            for p in files:                       # ① 归一化子串（取最长命中）
                if p in taken:
                    continue
                if n in nf[p] and (hit is None or len(n) > len(norm(os.path.basename(hit)[:-4]))):
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
            jobs.append((hit, f'art_{slug(name)}.png'))

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

    if not args.check:
        os.makedirs(OUT, exist_ok=True)
        os.makedirs(UI_OUT, exist_ok=True)

    ok, miss = 0, []
    for src, dst, crop in jobs:
        if not src or not os.path.exists(src):
            miss.append(src or '(空路径)')
            continue
        if not args.check:
            if crop:
                write_portrait(src, dst)            # 裁到 sprite rect + alpha 补 255
            else:
                shutil.copyfile(src, dst)
        ok += 1

    print(f'{"检查" if args.check else "拷贝"}完成：{ok} / {len(jobs)}')
    for m in miss:
        print('  缺:', m)
    if not args.check and ok:
        print('目标目录：', OUT)
        print('           ', UI_OUT)
        print('⚠️ 版权：这是原版资产，发布前整个 Resources/Art/ 要删掉（.gitignore 已排除）')
    return 1 if miss else 0


if __name__ == '__main__':
    sys.exit(main())
