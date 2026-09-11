#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
sync_from_d2.py —— 从档案库 d:/2 复制「自研卡牌游戏需要」的资源到 d:/4/Unity/ 项目区。

原则：
  * 只复制，不移动。d:/2 是档案库，保持原样。
  * 目标已存在同名文件 → 跳过并记冲突（绝不覆盖）。
  * 每批复完打印来源/数量/体积，产出 _同步日志.json 供写手册引用。

用法：
  D:/2/Warpforge_tools/py312/python.exe "d:/4/Unity/工具/sync_from_d2.py" --list
  D:/2/Warpforge_tools/py312/python.exe "d:/4/Unity/工具/sync_from_d2.py" --batch docs
"""
import argparse
import json
import os
import shutil
import sys

sys.stdout.reconfigure(encoding='utf-8')

D2 = 'd:/2'
D4 = 'd:/4/Unity'

# --------------------------------------------------------------------------
# 批次表： (batch, label, src, dst, kind, exclude_dirs)
#   kind: dir = 整目录复制（递归）; files = src 是文件列表
# --------------------------------------------------------------------------
B = []


def add(batch, label, src, dst, kind='dir', exclude=(), skip_file=None, as_file=False):
    """skip_file: 可调用对象，收到文件名（basename）返回 True 则跳过
    as_file: kind='files' 时，dst 是**目标文件全路径**（而不是目标目录）"""
    B.append(dict(batch=batch, label=label, src=src, dst=dst, kind=kind,
                  exclude=set(exclude), skip_file=skip_file, as_file=as_file))


def only_subdirs(names):
    """只复制这些一级子目录，其余一级子目录整体跳过（保留根目录散件）"""
    return set(names)


def scene_tex_entry(scene, batch='assets_big'):
    add(batch, f'战场贴图 {scene}', f'{D2}/解包整理/07_场景/{scene}/Texture2D',
        f'{D4}/素材/Warpforge原版/战场贴图/{scene}')


# ============================== docs 资料 ==================================
add('docs', '说明书（84 份，11 类）', f'{D2}/解包整理/说明书', f'{D4}/资料/说明书')

add('docs', '项目索引：解包资源使用地图', f'{D2}/解包资源使用地图.md', f'{D4}/资料/索引与盘点', 'files')
add('docs', '项目索引：按阵营资源索引', f'{D2}/按阵营资源索引.md', f'{D4}/资料/索引与盘点', 'files')
add('docs', '项目索引：解包资源列表清单', f'{D2}/解包资源列表清单.md', f'{D4}/资料/索引与盘点', 'files')
add('docs', '项目索引：原版游戏结构（复刻蓝图）', f'{D2}/原版游戏结构.md', f'{D4}/资料/索引与盘点', 'files')
add('docs', '项目索引：d:/2 CLAUDE.md（项目准则）', f'{D2}/CLAUDE.md', f'{D4}/资料/索引与盘点', 'files')
add('docs', '盘点报告 0910', f'{D2}/_资产盘点_0910.md', f'{D4}/资料/索引与盘点', 'files')
add('docs', '解包整理 README', f'{D2}/解包整理/README.md', f'{D4}/资料/索引与盘点', 'files')
add('docs', '解包审计报告 0910', f'{D2}/解包整理/_审计报告_0910.md', f'{D4}/资料/索引与盘点', 'files')
add('docs', '解包修复记录 0910', f'{D2}/解包整理/_修复记录_0910.md', f'{D4}/资料/索引与盘点', 'files')

add('docs', '战斗重建权威包（Step0-4 全部规格）', f'{D2}/战斗重建_0827', f'{D4}/资料/战斗规格/战斗重建_0827')
add('docs', '战斗缺口清单', f'{D2}/战斗缺口清单_0827.md', f'{D4}/资料/战斗规格', 'files')
add('docs', '战斗优化方案', f'{D2}/战斗优化方案_0828.md', f'{D4}/资料/战斗规格', 'files')
add('docs', '战斗视觉验收证据', f'{D2}/战斗优化证据_0828', f'{D4}/资料/战斗规格/战斗优化证据_0828')
add('docs', '卡面分层定稿演示', f'{D2}/分层卡演示_0827', f'{D4}/资料/卡面规格/分层卡演示_0827')

add('docs', '官方规则书 PDF', f'{D2}/Warpforge部队卡片/Warpforge_Offline_Rulebook_1_5-3.pdf', f'{D4}/资料/规则书', 'files')
add('docs', '官方规则书 中文翻译', f'{D2}/Warpforge部队卡片/Warpforge_Offline_Rulebook_1_5-3_中文翻译.md', f'{D4}/资料/规则书', 'files')
add('docs', '官方规则书 英文原文', f'{D2}/Warpforge部队卡片/Warpforge_Offline_Rulebook_1_5-3_英文原版.md', f'{D4}/资料/规则书', 'files')
add('docs', '规则书术语表（246 对）', f'{D2}/Warpforge部队卡片/_tmp_rulebook_terms.json', f'{D4}/资料/规则书', 'files')

add('docs', '卡牌数据总表（1136 张 OCR）', f'{D2}/Warpforge部队卡片/卡牌数据总表.md', f'{D4}/资料/卡牌数据表', 'files')
add('docs', '卡牌信息权威表（分类裁决）', f'{D2}/Warpforge部队卡片/卡牌信息权威表_0824.md', f'{D4}/资料/卡牌数据表', 'files')
add('docs', '卡牌完整信息库（合并宽表）', f'{D2}/Warpforge部队卡片/卡牌完整信息库_0824.md', f'{D4}/资料/卡牌数据表', 'files')
add('docs', '卡牌宝石稀有度映射', f'{D2}/Warpforge部队卡片/卡牌宝石稀有度_0824.md', f'{D4}/资料/卡牌数据表', 'files')
add('docs', 'PnP 卡面渲染模板', f'{D2}/Warpforge部队卡片/card.html', f'{D4}/资料/卡牌数据表', 'files')

add('docs', '原版真渲参照管线（228 张 + mod 源码）', f'{D2}/Unity参照管线_0825',
    f'{D4}/资料/原版参照图/Unity参照管线_0825',
    exclude=('tools/downloads', 'logs'))


# ============================== data 数据 ==================================
GDATA = f'{D2}/../warpforge/data'  # d:/warpforge/data
GDATA = 'd:/warpforge/data'

# 顶层 JSON 进 数据/游戏数据/；5 个子目录由下面的独立条目落到各自的 数据/<分类>/
# （所以这里必须排除，否则重跑会把它们又造回 游戏数据/ 下）
add('data', '游戏数据 JSON（卡池/卡组/教程/稀有度/动画映射）', GDATA, f'{D4}/数据/游戏数据',
    exclude=('particles', 'particles3d', 'arena_particles', 'i18n', 'ui_layout'),
    skip_file=lambda n: '.bak' in n.lower())
add('data', '粒子数据（2D 194 键 / 3D 863 / 逐战场）', f'{GDATA}/particles', f'{D4}/数据/粒子/particles')
add('data', '粒子数据 3D', f'{GDATA}/particles3d', f'{D4}/数据/粒子/particles3d')
add('data', '战场环境粒子', f'{GDATA}/arena_particles', f'{D4}/数据/粒子/arena_particles')
add('data', '中文本地化 zh_CN.csv', f'{GDATA}/i18n', f'{D4}/数据/本地化/i18n')
add('data', 'UI 布局 JSON', f'{GDATA}/ui_layout', f'{D4}/数据/界面布局/ui_layout')
add('data', '卡牌媒体索引', f'{D2}/解包整理/card_index.json', f'{D4}/数据/索引', 'files')
add('data', '贴图全局索引（3160 名字 → 路径）',
    f'{D2}/Warpforge_tools/data/texture_index.json', f'{D4}/数据/索引', 'files')
add('data', '逐场景贴图引用清单',
    f'{D2}/Warpforge_tools/data/scene_tex_refs.json', f'{D4}/数据/索引', 'files')
add('data', '场景贴图复制记录（197 条）',
    f'{D2}/Warpforge_tools/data/texture_copy_report.json', f'{D4}/数据/索引', 'files')
add('data', 'GUID → 对象名映射表（10325 条）',
    f'{D2}/Unity参照管线_0825/data/guid_map.tsv', f'{D4}/数据/索引', 'files')
add('data', '动画信息查找表（418 条）',
    f'{D2}/Warpforge_tools/data/animinfo_0824.json', f'{D4}/数据/索引', 'files')
add('data', '动画 GOID 映射',
    f'{D2}/Warpforge_tools/data/anim_goid_map_0824.json', f'{D4}/数据/索引', 'files')
add('data', '中文卡牌翻译（7 组）+ 术语',
    f'{D2}/Warpforge_tools/data/zh_cards_0827', f'{D4}/数据/卡牌翻译')
add('data', '枚举名表（IL2CPP）',
    f'{D2}/Warpforge_tools/data/enum_names.json', f'{D4}/数据/索引', 'files')
add('data', 'bundle → cab 映射',
    f'{D2}/Warpforge_tools/data/cab_bundle_map.json', f'{D4}/数据/索引', 'files')


# ============================== tools 工具 =================================
TOOL_SNAPSHOT = [
    'gen_unity_arena_manifest.py',      # ★ Unity 战场导入主工具（需扩到 13 战场）
    'unity_scene_to_godot.py',          # ★ 上面的依赖库（Assembler / 材质解析）
    'gltf_export.py',                   # ★ unity_scene_to_godot 的硬依赖
    'chain_rect.py',                    # ★ RectTransform 链式换算 → 屏幕绝对坐标（权威）
    'dump_scene_tree.py',               # 场景全树
    'dump_go_tree.py',                  # 按 GO 名 dump 子树
    'build_texture_index.py',           # 建全局贴图索引 / 补齐场景贴图
    'restore_fonts.py',                 # 从 Font JSON 还原 TTF
    'slice_ui_atlas.py',                # UI 图集切片
    'extract_scene_bundle.py',          # 单场景 bundle 提取
    'convert_unity_particles.py',       # 粒子 → 归一化 JSON
    'convert_unity_anim.py',            # 动画曲线 → JSON
    'extract_rulebook_md.py',           # 规则书提取
]
for t in TOOL_SNAPSHOT:
    add('tools', f'工具快照 {t}', f'{D2}/Warpforge_tools/scripts/{t}', f'{D4}/工具/scripts快照', 'files')
add('tools', '工具链 README（改名留档，避免覆盖本目录 README.md）',
    f'{D2}/Warpforge_tools/README.md', f'{D4}/工具/Warpforge_tools原始README.md', 'files', as_file=True)


# ============================== assets 素材 ================================
AW = f'{D4}/素材/Warpforge原版'

add('assets_small', '卡框（阵营 × tier1-4，104 张）', f'{D2}/Warpforge cardframes', f'{AW}/卡框')
add('assets_small', '卡背（233 张 1024²）', f'{D2}/Warpforge Cardbacks', f'{AW}/卡背')
add('assets_small', '字体 TTF（Noto CJK SC / Liberation / DOSVGA）', f'{D2}/解包整理/10_字体/fonts',
    f'{AW}/字体/fonts', skip_file=lambda n: n.lower().endswith('.json'))
add('assets_small', '字体 TTF + 材质', f'{D2}/解包整理/10_字体/resources', f'{AW}/字体/resources',
    skip_file=lambda n: n.lower().endswith('.json'))
add('assets_small', '字体配套材质贴图', f'{D2}/解包整理/10_字体/字体资源', f'{AW}/字体/字体资源')
add('assets_small', '视频（结算 Victory/Defeat/Draw、开包、开场）', f'{D2}/解包整理/05_视频/videos', f'{AW}/视频/videos')
add('assets_small', '视频（开包）', f'{D2}/解包整理/05_视频/menus/Gacha Crate Opening.mp4', f'{AW}/视频', 'files')
add('assets_small', '视频（开场）', f'{D2}/解包整理/05_视频/sharedassets0/Warpforge Intro.mp4', f'{AW}/视频', 'files')

add('assets_small', 'UI 图集（含 40k 关键词图标 atlas）', f'{D2}/解包整理/03_界面UI/图集', f'{AW}/UI图集/图集')
add('assets_small', 'UI 去重资源', f'{D2}/解包整理/03_界面UI/去重资源', f'{AW}/UI图集/去重资源')
add('assets_small', 'UI 通用窗口', f'{D2}/解包整理/03_界面UI/通用窗口', f'{AW}/UI图集/通用窗口')
add('assets_small', 'UI 通用静态资源', f'{D2}/解包整理/03_界面UI/通用静态资源', f'{AW}/UI图集/通用静态资源')
add('assets_small', 'UI 军队图标', f'{D2}/解包整理/03_界面UI/军队图标', f'{AW}/UI图集/军队图标')
add('assets_small', 'UI 卡组选择按钮', f'{D2}/解包整理/03_界面UI/卡组选择按钮', f'{AW}/UI图集/卡组选择按钮')
add('assets_small', 'UI 排位图标', f'{D2}/解包整理/03_界面UI/排位图标', f'{AW}/UI图集/排位图标')
add('assets_small', 'UI 运营图标', f'{D2}/解包整理/03_界面UI/运营图标', f'{AW}/UI图集/运营图标')
add('assets_small', 'UI 光标', f'{D2}/解包整理/03_界面UI/光标', f'{AW}/UI图集/光标')
add('assets_small', 'UI 共享资源', f'{D2}/解包整理/03_界面UI/共享资源', f'{AW}/UI图集/共享资源')
add('assets_small', 'UI 主菜单', f'{D2}/解包整理/03_界面UI/主菜单', f'{AW}/UI图集/主菜单')
add('assets_small', 'UI 特惠内容', f'{D2}/解包整理/03_界面UI/特惠内容', f'{AW}/UI图集/特惠内容')

add('assets_small', '装饰品定义数据（2522 份 MonoBehaviour）', f'{D2}/解包整理/02_装饰品/定义数据', f'{AW}/装饰品/定义数据')
add('assets_small', '边框', f'{D2}/解包整理/02_装饰品/边框', f'{AW}/装饰品/边框')
add('assets_small', '联盟徽章', f'{D2}/解包整理/02_装饰品/联盟徽章', f'{AW}/装饰品/联盟徽章')
add('assets_small', '战役奖励背景', f'{D2}/解包整理/02_装饰品/战役奖励背景', f'{AW}/装饰品/战役奖励背景')
add('assets_small', '头像', f'{D2}/解包整理/02_装饰品/头像', f'{AW}/装饰品/头像')
add('assets_small', '督军立绘', f'{D2}/解包整理/02_装饰品/督军立绘', f'{AW}/装饰品/督军立绘')

add('assets_small', '卡牌动画定义（1196 份）', f'{D2}/解包整理/01_卡牌/卡牌动画', f'{AW}/卡牌/卡牌动画')
add('assets_small', '卡组数据（2212 份）', f'{D2}/解包整理/01_卡牌/卡组数据', f'{AW}/卡牌/卡组数据')

add('assets_big', '游戏数据（卡包/教程/动画曲线/脚本定义）', f'{D2}/解包整理/09_游戏数据', f'{AW}/游戏数据')
add('assets_big', '战场模型 OBJ（1030 个 / 16 来源包）', f'{D2}/解包整理/06_模型', f'{AW}/战场模型')
for _s in ('battlearena1', 'battlearena2', 'battlearena3', 'battlearenaaeldari',
           'battlearenaastramilitarum', 'battlearenablacklegion', 'battlearenadarkangels',
           'battlearenaemperorschildren', 'battlearenagenestealers', 'battlearenaleviathan',
           'battlearenasororitas', 'battlearenaspacewolves', 'battlearenatauviorla',
           'mainmenuwarpforge'):
    scene_tex_entry(_s)
add('assets_big', '特效共享资源（贴图/材质/网格，gen_unity_arena_manifest 兜底目录）',
    f'{D2}/解包整理/08_预制体特效/共享资源', f'{AW}/特效共享资源')
add('assets_big', '音频（618 音效 + BGM + 督军语音 + mixer）', f'{D2}/解包整理/04_音频', f'{AW}/音频')


# --------------------------------------------------------------------------
def iter_src(entry):
    """产出 (src_file, rel_path)。rel_path 是相对 dst 的落点。"""
    src, kind, exc = entry['src'], entry['kind'], entry['exclude']
    skipf = entry.get('skip_file') or (lambda n: False)
    if kind == 'files':
        if os.path.isfile(src):
            yield src, os.path.basename(src)
        return
    if not os.path.isdir(src):
        return
    base = src.rstrip('/\\')
    for r, dirs, fs in os.walk(base):
        rel = os.path.relpath(r, base)
        relp = '' if rel == '.' else rel.replace('\\', '/')
        # 排除目录（相对 base 的 posix 路径前缀）
        if relp:
            if any(relp == e or relp.startswith(e + '/') for e in exc):
                dirs[:] = []
                continue
        keep = []
        for d in dirs:
            dp = d if not relp else relp + '/' + d
            if any(dp == e or dp.startswith(e + '/') or e.startswith(dp + '/') for e in exc):
                continue
            keep.append(d)
        dirs[:] = keep
        for f in fs:
            if skipf(f):
                continue
            yield (os.path.join(r, f), f) if not relp else (os.path.join(r, f), relp + '/' + f)


def run(batch):
    log = {'copied': [], 'skipped_exist': [], 'missing': []}
    for e in B:
        if e['batch'] != batch:
            continue
        dst_root = e['dst']
        n = 0
        npre = 0
        byts = 0
        for sf, rel in iter_src(e):
            df = dst_root if e.get('as_file') else os.path.join(dst_root, rel)
            if os.path.exists(df):
                log['skipped_exist'].append(df)
                npre += 1
                continue
            os.makedirs(os.path.dirname(df), exist_ok=True)
            try:
                shutil.copy2(sf, df)
                n += 1
                byts += os.path.getsize(sf)
            except Exception as ex:
                log['missing'].append({'dst': df, 'err': str(ex)})
        # 单文件源若不存在也要报
        if e['kind'] == 'files' and not os.path.exists(e['src']):
            log['missing'].append({'src': e['src'], 'err': 'not found'})
        print('  [%-8s] %-52s 新复制 %5d / 已存在 %6d 个文件  %8.1f MB'
              % (batch, e['label'], n, npre, byts / 1048576.0))
        log.setdefault('entries', []).append(
            {'label': e['label'], 'src': e['src'], 'dst': e['dst'],
             'files': n, 'present': npre, 'bytes': byts})
    return log


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--batch', default='all')
    ap.add_argument('--list', action='store_true')
    args = ap.parse_args()

    if args.list:
        cur = None
        for e in B:
            if e['batch'] != cur:
                cur = e['batch']
                print('== %s' % cur)
            print('   %-52s %s' % (e['label'], e['src']))
        print('\n批次: %s' % sorted({e['batch'] for e in B}))
        return 0

    batches = sorted({e['batch'] for e in B}) if args.batch == 'all' else [args.batch]
    alllog = []
    for b in batches:
        print('--- 批次 %s ---' % b)
        alllog.append(run(b))
    os.makedirs(f'{D4}/工具', exist_ok=True)
    outp = f'{D4}/工具/_同步日志_{"_".join(batches)}.json'
    with open(outp, 'w', encoding='utf-8') as f:
        json.dump(alllog, f, ensure_ascii=False, indent=1)
    tot_f = sum(en['files'] for lg in alllog for en in lg.get('entries', []))
    tot_b = sum(en['bytes'] for lg in alllog for en in lg.get('entries', []))
    nskip = sum(len(lg['skipped_exist']) for lg in alllog)
    nmiss = sum(len(lg['missing']) for lg in alllog)
    print('\n合计复制 %d 个文件 / %.2f GB；跳过已存在 %d；异常 %d' % (tot_f, tot_b / 2**30, nskip, nmiss))
    print('日志 → %s' % outp)
    return 0


if __name__ == '__main__':
    sys.exit(main())
