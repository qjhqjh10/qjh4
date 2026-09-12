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

数据来源：
  · 卡框/卡背  `d:/4/Unity/素材/Warpforge原版/{卡框,卡背}/`
  · 立绘       `d:/2/解包整理/01_卡牌/<阵营>/Texture2D/`（`*_inf_/ _veh_/ _war_/ _monster_` 那几个前缀）
               —— 注意**卡框和插画是两份资产**，插画那批才是无字的立绘
"""
import argparse
import os
import shutil
import sys

sys.stdout.reconfigure(encoding='utf-8')

ART_SRC  = 'd:/2/解包整理/01_卡牌'
BACK_SRC = 'd:/4/Unity/素材/Warpforge原版/卡背'
OUT      = 'd:/4/Unity/MyGame/Assets/CardPresentation/Resources/Art/cards'

# 卡框：我们的阵营 → 原版哪个阵营的框（挑的是配色对得上的）
FRAMES = {
    'ember': 'd:/4/Unity/素材/Warpforge原版/卡框/40k_Cardframe_troop_BlackLegion_tier1.png',
    'tide':  'd:/4/Unity/素材/Warpforge原版/卡框/40k_Cardframe_troop_SaimHann_tier1.png',
}

# 卡背
BACKS = {
    'ember': 'Cardback_BL_Premium_Eye of Horus_Main.png',
    'tide':  'Cardback_ASH_Asuryani Path_Main.png',
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


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument('--check', action='store_true', help='只检查源文件在不在，不写')
    args = ap.parse_args()

    jobs = []                                   # (源路径, 目标文件名)
    for fac, src in FRAMES.items():
        jobs.append((src, f'frame_{fac}.png'))
    for fac, name in BACKS.items():
        jobs.append((os.path.join(BACK_SRC, name), f'back_{fac}.png'))
    for ours, rel in PORTRAITS:
        jobs.append((os.path.join(ART_SRC, rel + '.png'), f'art_{slug(ours)}.png'))

    # UI 图单独一个目标目录，所以先把路径拼完整
    # （原来这 17 张是**手工拷的**，重建路径其实是断的 —— 2026-09-12 补上）
    jobs = [(src, os.path.join(OUT, name)) for src, name in jobs]
    jobs += [(os.path.join(UI_SRC, n + '.png'), os.path.join(UI_OUT, n + '.png')) for n in UI_IMAGES]
    jobs += [(os.path.join(FX_SRC, n + '.png'), os.path.join(UI_OUT, n + '.png')) for n in FX_TEXTURES]

    if not args.check:
        os.makedirs(OUT, exist_ok=True)
        os.makedirs(UI_OUT, exist_ok=True)

    ok, miss = 0, []
    for src, dst in jobs:
        if not os.path.exists(src):
            miss.append(src)
            continue
        if not args.check:
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
