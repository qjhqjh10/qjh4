#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""read_itemdrawerconfig.py — 从**原始字节**里读出原版 `ItemDrawerConfig` 这张映射表
（每个 `ObtainableItem` 子类型 → 用哪个 `ItemDrawer` prefab）。

为什么要它（2026-10-04 立）
--------------------------
`ItemDrawer.Draw(parent, item, qty, DrawerOverride)` 是原版**所有「奖励/商品小格子」的
统一入口**：商店货架、开包、每日奖励、对战奖励窗……全都走它。
「某个 item 长什么样」不是由调用方决定的，而是由这张表决定的：

    ItemDrawerConfig.GetReference(type, override)   # 先精确 Type 匹配，再 IsAssignableFrom 兜底
        → ItemDrawerReference.GetDrawer(override)   # override == 0 用 base；否则去
                                                    # customDrawerOverrides 里找 Key == override
                                                    # **找不到仍然回落到 base**
        → (ComponentReference<ItemDrawer>, ItemDrawerOptions)

消费点实证：`总反编译 GeneralOfferPopupDrawer__DrawRewards.c:319` 调
`ItemDrawer.GetDrawerConfig(…, 0x1e)`，`0x1e = 30 = DrawerOverride.OfferPopups`；
商店那条 = `DrawerOverride.Shop = 20`。
⇒ 我们要复刻商店/开包/奖励窗，**必须知道 20 和 30 各指向哪个 prefab**，否则只能凭截图猜。

资源在哪 / 为什么以前读不出
---------------------------
* 这个 SO **不在任何 bundle 里**，在 `Warpforge_Data/sharedassets0.assets` 里
  （MonoBehaviour pathID **453**，`m_Script` = {m_FileID 1, m_PathID 2956}）。
* `globalgamemanagers.assets` 里那个 `ItemDrawerConfig` 串是 **MonoScript**（`m_ClassName`），
  **不是** SO 本体 —— 别拿文件名/第一个命中下结论，要按 `m_Script` 的 pathID 反查。
* 🔴 这些 `.assets` **没有 type tree** ⇒ UnityPy `read_typetree()` 读不出字段名，
  「grep 字段名查不到」对它们是**无效否定**（CLAUDE.md 同族坑）。
  只能**照签名桩的声明顺序手工切字节**。

字段顺序（= 序列化顺序，出处 `D:/2/Warpforge_code/Scripts/Assembly-CSharp/`）
--------------------------------------------------------------------------
    ItemDrawerConfig            : List<ItemDrawerReference> drawers
    ItemDrawerReference         : TypeReference TypeReference
                                  ComponentReference<ItemDrawer> drawerReference
                                  List<KeyDrawerPair> customDrawerOverrides
                                  ItemDrawerOptions options
    KeyDrawerPair               : DrawerOverride Key ; ComponentReference<ItemDrawer> DrawerReference
    TypeReference (TypeReferences 包)
                                : bool GuidAssignmentFailed ; string GUID ;
                                  string _typeNameAndAssembly ; bool _suppressLogs
    ComponentReference<T> : AssetReference (Unity.Addressables)
                                : string m_AssetGUID ; string m_SubObjectName ; string m_SubObjectType
    ItemDrawerOptions           : bool stackable ; bool showName ; string typeString

字节规则：`int32` / `float` 4 对齐；`bool` 占 1 字节**但按 4 对齐**（写出来是 4 字节）；
`string` = `int32 长度` + 字节 + **补齐到 4**；`List<T>` = `int32 条数` + 条数×T。

GUID → prefab 名字怎么解（这是本脚本的第二半）
--------------------------------------------
`ComponentReference<T>` 只给一个 Addressables **GUID**（`m_SubObjectName` / `m_SubObjectType` 都是空串），
所以光看这个 SO 只知道「指向 743e8454…」。
把 GUID 换成名字走的是 **`AssetBundle.m_Container`**：
`d:/2/新解包资源/assets_full/<bundle>/AssetBundle/AssetBundle_*.json` 里的 `m_Container`
就是 Addressables 的地址表 —— **键 = 资产 GUID，值 = {asset:{m_FileID, m_PathID}}**，
再用 UnityPy 重读**原始 `.bundle`** 把 `m_PathID` 反解成 (类型, 名字)。
（这条 join 办法是 `gen_anim_address_map.py` 铺出来的，出处见它文件头。）

实测（2026-10-04）：36 个 GUID **全部**命中 `bundle_menus_assets_all` 的 `m_Container`，
36 个 `m_PathID` **全部**反解成 GameObject，**36 个名字互不重复、无空名**；
名字与类型语义逐条对得上（`Currency` → "Currency Drawer"，`CosmeticItemCardback` → "Cardback Drawer"…）。

用法
----
    PYTHONIOENCODING=utf-8 python d:/4/Unity/工具/read_itemdrawerconfig.py
    PYTHONIOENCODING=utf-8 python d:/4/Unity/工具/read_itemdrawerconfig.py --md
    # 本机两个解释器都装了 UnityPy，任选：
    #   C:/Users/qjh36/AppData/Local/Programs/Python/Python314/python.exe   (UnityPy 1.25.3)
    #   D:/2/Warpforge_tools/py312/python.exe                               (UnityPy 1.24.2)

退出码：0 = 全部自检通过；1 = 有自检没过（**会逐条打印是哪一条**，不静默）。

⚠️ 只读 `d:/2`（铁律 2：那是档案库，不写）。
"""

import argparse
import io
import json
import os
import re
import struct
import sys

# ---------------------------------------------------------------- 常量

DATA = 'd:/2/unity_run_ref/Warpforge_Data'
SO_ASSET = os.path.join(DATA, 'sharedassets0.assets')         # 这个 SO 的宿主文件
GGM_ASSET = os.path.join(DATA, 'globalgamemanagers.assets')   # sharedassets0 的 externals[0]
ASSETS_FULL = 'd:/2/新解包资源/assets_full'
MONOSCRIPTS_DUMP = os.path.join(ASSETS_FULL, 'bundle_Waprforge_monoscripts', 'MonoScript')
BUNDLE_SRC_CANDIDATES = [
    os.path.join(DATA, 'StreamingAssets/aa/StandaloneWindows64'),
    'd:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64',
]
STUB_DIR = 'd:/2/Warpforge_code/Scripts/Assembly-CSharp'      # 签名桩（声明顺序的来源）

SO_CLASS = 'ItemDrawerConfig'
MONO_SCRIPT_PID = 2956        # sharedassets0.assets 里的引用值（本脚本会**实测核对**，不是硬信）

# DrawerOverride（出处 D:/2/Warpforge_code/Scripts/Assembly-CSharp/DrawerOverride.cs）
OVERRIDE = {0: 'Default', 10: 'Icon', 15: 'Horizontal', 20: 'Shop', 30: 'OfferPopups'}

# 扫签名桩里的**所有**继承关系（`class X : Base`），用来把「间接子类」也接上
# （`TitleDrawerHorizontal : TitleDrawer`、`PremiumIconDrawer : PremiumDrawer`、
#   `DeckAndCardbackDrawer : DeckDrawer` 这些**不直接**写 `ItemDrawer<T>`）
CLASS_DECL_RE = re.compile(
    r'\bclass\s+([A-Za-z_]\w*)\s*(?:<[^>]*>)?\s*:\s*([A-Za-z_][\w\.]*)\s*(?:<([A-Za-z_][\w\.]*)\s*>)?')

# 反查 dump 文件名用的非法字符表（抄 extract_full.py，与 gen_anim_address_map.py 一致）
SAFE_RE = re.compile(r'[\\/:*?"<>|\x00-\x1f]')


# ---------------------------------------------------------------- 原始字节读取原语

class Reader(object):
    """在 raw 上按 Unity 序列化规则前进，**每一步都记下它从哪个偏移读的**。"""

    def __init__(self, raw, base):
        self.raw = raw
        self.p = 0
        self.base = base      # 该对象在**文件里**的绝对起始偏移（obj.byte_start）

    def off(self):
        return self.base + self.p

    def i32(self):
        o = self.off()
        v = struct.unpack_from('<i', self.raw, self.p)[0]
        self.p += 4
        return v, o

    def boolean(self):
        # Unity 写 bool 是 1 字节，但按 4 对齐 ⇒ 实际占 4（高 3 字节是 padding）
        o = self.off()
        v = self.raw[self.p]
        self.p += 4
        return bool(v), o

    def string(self):
        n, o = self.i32()
        if n < 0 or self.p + n > len(self.raw):
            raise ValueError('字符串长度 %d 越界（@0x%X）—— 说明前面就已经错位了'
                             % (n, o))
        s = self.raw[self.p:self.p + n].decode('utf-8', 'replace')
        self.p += (n + 3) // 4 * 4
        return s, o

    def asset_ref(self):
        """ComponentReference<T> : AssetReference → m_AssetGUID / m_SubObjectName / m_SubObjectType"""
        guid, o = self.string()
        sub, _ = self.string()
        subty, _ = self.string()
        return {'guid': guid, 'subObjectName': sub, 'subObjectType': subty}, o

    def type_ref(self):
        """TypeReferences.TypeReference → GuidAssignmentFailed / GUID / _typeNameAndAssembly / _suppressLogs"""
        gaf, _ = self.boolean()
        guid, _ = self.string()
        tna, _ = self.string()
        sup, _ = self.boolean()
        return {'guidAssignmentFailed': gaf, 'guid': guid,
                'typeNameAndAssembly': tna, 'suppressLogs': sup}


# ---------------------------------------------------------------- ① 定位并解析 SO

def find_monobehaviour(env, script_pid):
    """按 `m_Script` 的 pathID 找 MonoBehaviour（**别按名字找**，名字只写在 m_Name 里）"""
    hits = []
    for o in env.objects:
        if o.type.name != 'MonoBehaviour':
            continue
        raw = o.get_raw_data()
        if len(raw) < 32:
            continue
        fid, pid = struct.unpack_from('<iq', raw, 16)
        if pid == script_pid:
            hits.append((o, fid))
    return hits


def read_config(verbose=True):
    """解出整张表。返回 dict（含每条记录的**绝对字节偏移**）。"""
    import UnityPy

    env = UnityPy.load(SO_ASSET)
    hits = find_monobehaviour(env, MONO_SCRIPT_PID)
    if not hits:
        raise SystemExit('!! %s 里没有 m_Script.pathID == %d 的 MonoBehaviour' % (SO_ASSET, MONO_SCRIPT_PID))
    if len(hits) > 1:
        raise SystemExit('!! 命中 %d 个候选（%s），不敢猜哪个是本体'
                         % (len(hits), [o.path_id for o, _ in hits]))
    obj, script_fid = hits[0]

    # ①a 自证：m_Script 的 (fileID, pathID) 真的解得回「类名 == ItemDrawerConfig」的 MonoScript
    ext_path = '?'
    sf = env.files[[k for k in env.files if k.endswith('sharedassets0.assets')][0]]
    externals = [e.path for e in sf.externals]      # m_FileID 1..N → externals[0..N-1]（0 = 本文件）
    if 1 <= script_fid <= len(externals):
        ext_path = externals[script_fid - 1]
    ggm = UnityPy.load(os.path.join(os.path.dirname(SO_ASSET), os.path.basename(ext_path)))
    ggm_byid = {o.path_id: o for o in ggm.objects}
    ms = ggm_byid.get(MONO_SCRIPT_PID)
    ms_class = None
    if ms is not None:
        try:
            ms_class = (ms.read_typetree() or {}).get('m_ClassName')
        except Exception:
            pass
    chain_ok = (ms_class == SO_CLASS)

    raw = obj.get_raw_data()
    base = obj.byte_start                       # 对象在文件里的绝对偏移
    r = Reader(raw, base)

    # ①b MB 头：m_GameObject(12) + m_Enabled(4) + m_Script(12) + m_Name(4 + 对齐串)
    #     🔴 头**不是固定 32 字节**，m_Name 是对齐后变长的（别用固定 32 切）
    r.p = 28
    name, name_off = r.string()
    head_end = r.p

    drawers = []
    n, list_off = r.i32()
    for i in range(n):
        rec_off = r.off()
        tr = r.type_ref()
        drawer, guid_off = r.asset_ref()
        m, _ = r.i32()
        ovs = []
        for _j in range(m):
            ov_rec_off = r.off()
            key, key_off = r.i32()
            ref, ov_guid_off = r.asset_ref()
            ovs.append({'key': key, 'keyName': OVERRIDE.get(key, '?(未知枚举值)'),
                        'guid': ref['guid'], 'subObjectName': ref['subObjectName'],
                        'subObjectType': ref['subObjectType'],
                        'recOff': ov_rec_off, 'keyOff': key_off, 'guidOff': ov_guid_off})
        stackable, _ = r.boolean()
        show_name, _ = r.boolean()
        type_string, _ = r.string()
        drawers.append({
            'recOff': rec_off,
            'typeName': (tr['typeNameAndAssembly'] or '').split(',')[0].strip(),
            'typeNameAndAssembly': tr['typeNameAndAssembly'],
            'typeGuid': tr['guid'],
            'drawerGuid': drawer['guid'],
            'drawerSubObjectName': drawer['subObjectName'],
            'drawerSubObjectType': drawer['subObjectType'],
            'drawerGuidOff': guid_off,
            'overrides': ovs,
            'options': {'stackable': stackable, 'showName': show_name,
                        'typeString': type_string},
        })

    exact_end = (r.p == len(raw))

    if verbose:
        print('① 解析 SO')
        print('   宿主      %s' % SO_ASSET)
        print('   对象        MonoBehaviour pathID %d（数据起点 0x%X，长 %d 字节）'
              % (obj.path_id, base, len(raw)))
        print('   m_Script    {m_FileID %d, m_PathID %d} → externals[%d] = %s'
              % (script_fid, MONO_SCRIPT_PID, script_fid - 1, ext_path))
        print('   MonoScript  pathID %d m_ClassName = %r   %s'
              % (MONO_SCRIPT_PID, ms_class, '✓ 对上' if chain_ok else '✗ 对不上'))
        print('   m_Name      %r（@0x%X，头结束 @0x%X —— **不是固定 32**）'
              % (name, name_off, base + head_end))
        print('   drawers     %d 条（count @0x%X）' % (n, list_off))
        print('   解析终点    0x%X，对象末尾 0x%X ⇒ %s'
              % (base + r.p, base + len(raw),
                 '✓ 正好吃掉整个对象（**这是最强的一条自证：一个字节不多不少**）'
                 if exact_end else '✗ 差 %d 字节 —— 有字段没解对' % (len(raw) - r.p)))
        print()

    return {
        'asset': SO_ASSET,
        'objectPathId': obj.path_id,
        'dataStart': base,
        'byteSize': len(raw),
        'name': name,
        'monoScript': {'fileId': script_fid, 'pathId': MONO_SCRIPT_PID,
                       'externalPath': ext_path, 'className': ms_class},
        'drawers': drawers,
        'chainOk': chain_ok,
        'exactEnd': exact_end,
    }


# ---------------------------------------------------------------- ② GUID → prefab

def build_container_index():
    """扫 assets_full 下所有 bundle 的 `AssetBundle/*.json` → {GUID: [(bundleDir, pathId)]}。

    `m_Container` 就是 Addressables 的地址表：键 = 资产 GUID，值 = {asset:{m_FileID, m_PathID}}。
    """
    idx = {}
    dirs = sorted(os.listdir(ASSETS_FULL)) if os.path.isdir(ASSETS_FULL) else []
    for d in dirs:
        ab = os.path.join(ASSETS_FULL, d, 'AssetBundle')
        if not os.path.isdir(ab):
            continue
        for f in sorted(os.listdir(ab)):
            if not f.endswith('.json'):
                continue
            try:
                with io.open(os.path.join(ab, f), encoding='utf-8') as fh:
                    j = json.load(fh)
            except Exception:
                continue
            for item in (j.get('m_Container') or []):
                if not (isinstance(item, list) and len(item) == 2):
                    continue
                key, val = item
                asset = (val or {}).get('asset') or {}
                pid = asset.get('m_PathID')
                if key is None or pid is None:
                    continue
                idx.setdefault(str(key), []).append(
                    {'bundleDir': d, 'pathId': int(pid),
                     'fileId': int(asset.get('m_FileID', 0)),
                     'containerJson': '%s/AssetBundle/%s' % (d, f)})
    return idx, len(dirs)


def find_bundle_src():
    for d in BUNDLE_SRC_CANDIDATES:
        if os.path.isdir(d):
            return d
    return None


def object_name(obj):
    """拿对象的 m_Name。先走 typetree（不触发贴图/音频解码），失败再退 read()。"""
    try:
        tt = obj.read_typetree()
        if isinstance(tt, dict) and tt.get('m_Name'):
            return tt['m_Name']
    except Exception:
        pass
    try:
        nm = getattr(obj.read(), 'm_Name', None)
        if nm:
            return nm
    except Exception:
        pass
    return None


class ScriptNames(object):
    """`m_Script` 的 pathID → 类名。

    那些 pathID 是**大 64 位数**（addressables 里 MonoScript 的对象号），它们住在
    `assets_full/bundle_Waprforge_monoscripts/MonoScript/MonoScript_<pathID>.json`（722 个）。
    ⚠️ 文件名用的是**有符号** int64（负的要写成负号形式），所以两个形式都试。
    """

    def __init__(self, root=MONOSCRIPTS_DUMP):
        self.root = root
        self._cache = {}

    def class_of(self, pid):
        if pid in self._cache:
            return self._cache[pid]
        out = None
        for v in (pid, pid - (1 << 64) if pid >= 0 else pid + (1 << 64)):
            p = os.path.join(self.root, 'MonoScript_%d.json' % v)
            if not os.path.isfile(p):
                continue
            try:
                d = json.load(io.open(p, encoding='utf-8'))
                if isinstance(d, list):
                    d = d[0]
                out = d.get('m_ClassName')
            except Exception:
                out = None
            break
        self._cache[pid] = out
        return out


def prefab_classes(guid_info, verbose=True):
    """每个命中对象所在的 bundle 里，把 GameObject 的组件 `m_Script` 解成**类名**。

    这是**第三条独立证据链**：SO 里的 GUID →(m_Container)→ GameObject →(m_Component →
    MonoBehaviour 的 m_Script，raw 偏移 16)→ MonoScript.m_ClassName。
    它顺带证伪一件事：**prefab 名不可靠地暗示类名** ——
    `Icon Random Card Drawer Variant` 身上挂的是 `RandomCardDrawer`，**不是** `RandomCardIconDrawer`。
    """
    by_bundle = {}
    for g, info in guid_info.items():
        if info.get('name') and info.get('bundleDir'):
            by_bundle.setdefault(info['bundleDir'], []).append((g, info['pathId']))
    src = find_bundle_src()
    if src is None:
        return {}, '找不到 bundle 源目录'
    names = ScriptNames()
    out = {}
    for bdir, items in by_bundle.items():
        bf = os.path.join(src, bdir[len('bundle_'):] + '.bundle')
        if not os.path.isfile(bf):
            for g, _pid in items:
                out[g] = {'unresolved': '源 bundle 不存在: %s' % bf}
            continue
        import UnityPy
        env = UnityPy.load(bf)
        byid = {o.path_id: o for o in env.objects}
        for g, pid in items:
            o = byid.get(pid)
            if o is None:
                out[g] = {'unresolved': 'pathID %d 不在该 bundle 里' % pid}
                continue
            try:
                tt = o.read_typetree()
            except Exception as e:
                out[g] = {'unresolved': 'read_typetree 失败: %s' % e}
                continue
            comps, drawers, unresolved = [], [], []
            for comp in (tt.get('m_Component') or []):
                cp = comp.get('component') or {}
                if cp.get('m_FileID', 0) != 0:
                    unresolved.append('组件 m_FileID=%s 指向外部文件' % cp.get('m_FileID'))
                    continue
                co = byid.get(cp.get('m_PathID'))
                if co is None:
                    unresolved.append('组件 pathID %s 不在本包' % cp.get('m_PathID'))
                    continue
                if co.type.name != 'MonoBehaviour':
                    comps.append(co.type.name)
                    continue
                # MonoBehaviour 的 m_Script 在 raw 偏移 16（m_GameObject 12 + m_Enabled 4）
                raw = co.get_raw_data()
                if len(raw) < 32:
                    unresolved.append('组件 raw 太短'); continue
                _fid, spid = struct.unpack_from('<iq', raw, 16)
                cn = names.class_of(spid)
                if cn is None:
                    unresolved.append('m_Script pathID %d 在 monoscripts dump 里找不到' % spid)
                comps.append(cn or ('?%d' % spid))
                # `ItemDrawerComponents` 是 `[RequireComponent(typeof(ItemDrawerComponents))]`
                # 拉进来的**共用零件**（出处 ItemDrawer.cs 里 `ItemDrawer<T>` 的标注），不是抽屉类
                if cn and cn != 'ItemDrawerComponents' and ('Drawer' in cn or 'Display' in cn):
                    drawers.append(cn)
            out[g] = {'name': tt.get('m_Name'), 'components': comps,
                      'drawerClasses': drawers, 'unresolved': unresolved}
    if verbose:
        n_ok = sum(1 for v in out.values() if v.get('drawerClasses'))
        all_un = sorted(set(u for v in out.values() for u in v.get('unresolved') or []))
        print('②b prefab 身上的 ItemDrawer 子类：%d/%d 个 prefab 解出了抽屉类'
              % (n_ok, len(by_bundle and out)))
        for u in all_un[:8]:
            print('      ! %s' % u)
        print()
    return out, None


def resolve_guids(guids, verbose=True):
    """{guid: {'bundle','pathId','type','name','containerJson'}}；解不出的**明写原因**。"""
    idx, n_dirs = build_container_index()
    src = find_bundle_src()

    out = {}
    for g in guids:
        ents = idx.get(g)
        if not ents:
            out[g] = {'unresolved': '不在任何 bundle 的 m_Container 里'
                                    '（扫了 %d 个 bundle 目录的 AssetBundle/*.json）' % n_dirs}
            continue
        if len(ents) > 1:
            out[g] = {'unresolved': '在 %d 个 bundle 的 m_Container 里都有（%s）—— 不敢猜'
                                    % (len(ents), [e['bundleDir'] for e in ents])}
            continue
        e = ents[0]
        rec = {'bundleDir': e['bundleDir'], 'pathId': e['pathId'],
               'containerJson': e['containerJson']}
        if src is None:
            rec['unresolved'] = '找到容器项，但找不到 bundle 源目录（%s）' % BUNDLE_SRC_CANDIDATES
            out[g] = rec
            continue
        bundle_file = os.path.join(src, e['bundleDir'][len('bundle_'):] + '.bundle')
        if not os.path.isfile(bundle_file):
            rec['unresolved'] = '源 bundle 不存在: %s' % bundle_file
            out[g] = rec
            continue
        import UnityPy
        env = UnityPy.load(bundle_file)
        byid = {o.path_id: o for o in env.objects}
        o = byid.get(e['pathId'])
        if o is None:
            rec['unresolved'] = 'pathID %d 在该 bundle 里找不到' % e['pathId']
            out[g] = rec
            continue
        nm = object_name(o)
        rec['type'] = o.type.name
        rec['name'] = nm
        if not nm:
            rec['unresolved'] = '对象存在（类型 %s）但取不到 m_Name' % o.type.name
        out[g] = rec
    if verbose:
        n_ok = sum(1 for v in out.values() if v.get('name'))
        print('② GUID → prefab：%d 个 GUID 里解出 %d 个（%d 个在 m_Container 找到、%d 个失败）'
              % (len(guids), n_ok,
                 sum(1 for v in out.values() if 'unresolved' not in v),
                 sum(1 for v in out.values() if 'unresolved' in v)))
        print()
    return out


# ---------------------------------------------------------------- ③ 旁证：签名桩里的 ItemDrawer<T> 子类

def drawer_classes():
    """扫签名桩 → {ObtainableItem 子类型名: [drawer 类名]}。

    `ItemDrawer<T>` 的泛型参数 T **就是**这个 drawer 负责的 ObtainableItem 子类型 ——
    这是**独立于本 SO 的第二个判据**，用来交叉验「我们解出来的 prefab 名字对不对」。

    两趟：① 收全部 `class X : Base`；② 从直接写 `ItemDrawer<T>` 的那些出发做不动点，
    把**间接子类**（`TitleDrawerHorizontal : TitleDrawer` 之类）也接到同一个 T 上。
    T 与查表都用**短名**（去掉命名空间），因为签名桩里的 T 有的带命名空间有的不带。
    """
    base_of, direct = {}, {}          # class → 基类短名 ; class → T（只记直接写 ItemDrawer<T> 的）
    if not os.path.isdir(STUB_DIR):
        return {}
    for root, _dirs, files in os.walk(STUB_DIR):
        for f in files:
            if not f.endswith('.cs'):
                continue
            try:
                txt = io.open(os.path.join(root, f), encoding='utf-8', errors='replace').read()
            except OSError:
                continue
            for m in CLASS_DECL_RE.finditer(txt):
                cls, bas, gen = m.group(1), m.group(2).rsplit('.', 1)[-1], m.group(3)
                if cls in base_of:
                    continue
                base_of[cls] = bas
                t = gen.rsplit('.', 1)[-1] if gen else ('' if bas == 'ItemDrawer' else None)
                if bas == 'ItemDrawer' and gen and not gen.startswith('T'):
                    direct[cls] = t
                elif bas == 'ItemDrawer':
                    direct[cls] = '(非泛型 ItemDrawer)'
    t_of = dict(direct)
    for _ in range(6):                # 不限层数地往上接，六轮足够（继承链最长 2～3 层）
        changed = 0
        for cls, bas in base_of.items():
            if cls not in t_of and bas in t_of:
                t_of[cls] = t_of[bas]
                changed += 1
        if not changed:
            break
    t_of.pop('ItemDrawer', None)      # 抽象基类自己不算「类候选」
    out = {}
    for cls, t in t_of.items():
        if t and t != 'T':
            out.setdefault(t, set()).add(cls)
    return {k: sorted(v) for k, v in out.items()}


def _tokens(s):
    """把名字切成小写词集合。**两边用同一个切法**（`XSollaOfferDrawer` 与 `XSolla Offer Drawer`
    要切出同一组词），再把词尾的复数 s 折掉（`Points` ↔ `Point`）。"""
    parts = re.findall(r'[A-Z]+(?![a-z])|[A-Z][a-z0-9]*|[a-z0-9]+', s or '')
    out = []
    for p in parts:
        p = p.lower()
        if len(p) > 3 and p.endswith('s'):
            p = p[:-1]
        out.append(p)
    return set(out)


def class_for_prefab(prefab_name, classes):
    """启发式：类名切出的词集 ⊆ prefab 名切出的词集 ⇒ 候选；词多的（更具体）优先。

    ⚠️ **这是「猜」、不是判据**，只用来在没有权威答案时给个提示。
    实测已经被证伪过一次：`Icon Random Card Drawer Variant` 身上挂的是 `RandomCardDrawer`，
    而这套启发式会先推 `RandomCardIconDrawer`（因为"icon"这个词在名字里）。
    ⇒ **prefab 名不能用来推类名**；要类名请走 `prefab_classes()`（组件 m_Script 那条链）。
    """
    if not prefab_name:
        return []
    want = _tokens(prefab_name)
    cands = [c for c in classes if _tokens(c) <= want and _tokens(c)]
    return sorted(cands, key=lambda c: (-len(_tokens(c)), c))


# ---------------------------------------------------------------- 主流程

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--md', action='store_true', help='额外打印一张 markdown 表（贴文档用）')
    ap.add_argument('--json', dest='json_out', default=None, help='把整表写进这个 json（可选）')
    args = ap.parse_args()

    cfg = read_config()
    rows = cfg['drawers']

    guids = []
    for rec in rows:
        guids.append(rec['drawerGuid'])
        for ov in rec['overrides']:
            guids.append(ov['guid'])
    guids = sorted(set(g for g in guids if g))
    guid_info = resolve_guids(guids)
    pcs, pc_err = prefab_classes(guid_info)
    if pc_err:
        print('   !! prefab 组件类解不出：%s' % pc_err)
    for g, v in pcs.items():
        gi = guid_info.setdefault(g, {})
        gi['components'] = v.get('components')
        gi['drawerClasses'] = v.get('drawerClasses')
        if v.get('unresolved'):
            gi['componentUnresolved'] = v['unresolved']

    classes = drawer_classes()

    # ---- 打印主表
    print('③ 映射表（每条带**绝对字节偏移**，起点 = 数据起点 0x%X）' % cfg['dataStart'])
    print('   %-40s %-8s %-38s %s' % ('ObtainableItem 子类型', 'override', 'prefab (GameObject m_Name)', 'GUID @偏移'))
    print('   ' + '-' * 150)
    unresolved = []
    for rec in rows:
        base = guid_info.get(rec['drawerGuid'], {})
        bname = base.get('name') or ('*解不出* ' + (base.get('unresolved') or '?'))
        print('   %-40s %-8s %-38s %s @0x%X'
              % (rec['typeName'], 'Default', bname, rec['drawerGuid'], rec['drawerGuidOff']))
        if not base.get('name'):
            unresolved.append((rec['typeName'], 'Default', base.get('unresolved')))
        for ov in rec['overrides']:
            oi = guid_info.get(ov['guid'], {})
            onm = oi.get('name') or ('*解不出* ' + (oi.get('unresolved') or '?'))
            print('   %-40s %-8s %-38s %s @0x%X'
                  % ('', ov['keyName'] + '(%d)' % ov['key'], onm, ov['guid'], ov['guidOff']))
            if not oi.get('name'):
                unresolved.append((rec['typeName'], ov['keyName'], oi.get('unresolved')))
        opt = rec['options']
        print('   %-40s %-8s options: stackable=%s showName=%s typeString=%r   [记录 @0x%X]'
              % ('', '', int(opt['stackable']), int(opt['showName']), opt['typeString'], rec['recOff']))
        # 第三条证据链：prefab 身上的抽屉类（权威，来自组件 m_Script → MonoScript.m_ClassName）
        for g in [rec['drawerGuid']] + [o['guid'] for o in rec['overrides']]:
            gi = guid_info.get(g, {})
            nm = gi.get('name')
            if not nm:                       # 同一条 override 出现两次时只打一次
                continue
            print('   %-40s %-8s   抽屉类 @ %-38s → %s'
                  % ('', '', nm, ', '.join(gi.get('drawerClasses') or []) or '（解不出）'))
        short = rec['typeName'].rsplit('.', 1)[-1]
        cand = classes.get(rec['typeName']) or classes.get(short) or []
        print('   %-40s %-8s   旁证: 签名桩 ItemDrawer<%s> 的类 = %s'
              % ('', '', short, ', '.join(cand) if cand else '（签名桩里没有，走 IsAssignableFrom 兜底）'))
        print()

    # ---- 旁证表
    print('④ 旁证：签名桩里 `ItemDrawer<T>` 的 T（**独立第二判据**）')
    hit = miss = 0
    for rec in rows:
        short = rec['typeName'].rsplit('.', 1)[-1]
        cand = classes.get(rec['typeName']) or classes.get(short)
        if cand:
            hit += 1
            print('   %-40s → %s' % (rec['typeName'], ', '.join(cand)))
        else:
            miss += 1
            print('   %-40s → ✗ 签名桩里没有 ItemDrawer<%s> 的子类'
                  '（这是**正常**的：表里可以写「具体类型」，而类只写到「抽象基类」，'
                  '运行时走 GetReference 的 IsAssignableFrom 兜底。两处实证 ——'
                  'ContainerDrawer : ItemDrawer<ShopContainerBase> 而表里写具体类 ShopContainer；'
                  'ExpansionPassPremiumDrawer : ItemDrawer<ExpansionPremiumItem> 而表里另有 VIPPremiumItem : ExpansionPremiumItem）'
                  % (rec['typeName'], short))
    print('   合计 %d/%d 条能在签名桩里找到对应的 drawer 类' % (hit, hit + miss))
    print()

    # ---- 自检
    print('⑤ 自检')
    checks = []
    checks.append(('m_Script 链解得回 MonoScript m_ClassName == "ItemDrawerConfig"', cfg['chainOk']))
    checks.append(('按声明顺序解析**正好**吃掉整个对象（一个字节不多不少）', cfg['exactEnd']))
    all_guid_ok = all(v.get('name') for v in guid_info.values())
    checks.append(('全部 %d 个 GUID 都解出了对象名' % len(guids), all_guid_ok))
    names = [v.get('name') for v in guid_info.values() if v.get('name')]
    checks.append(('解出的 %d 个名字互不重复' % len(names), len(names) == len(set(names))))
    n_drawer = sum(1 for v in guid_info.values() if v.get('drawerClasses'))
    checks.append(('每个 prefab 身上都解出了 ItemDrawer 子类（%d/%d，组件 m_Script → MonoScript.m_ClassName）'
                   % (n_drawer, len(guids)), n_drawer == len(guids)))
    # 抽屉类名必须真的在签名桩的 ItemDrawer 家族里（否则说明类名解错了）
    known = set(c for lst in classes.values() for c in lst)
    unknown = sorted(set(c for v in guid_info.values() for c in (v.get('drawerClasses') or []))
                     - known)
    checks.append(('解出的抽屉类名都在签名桩的 ItemDrawer 家族里（陌生的 %d 个：%s）'
                   % (len(unknown), unknown or '无'), not unknown))
    row_type_ok = all(r['typeName'] for r in rows)
    checks.append(('每条的 TypeReference 都读出了非空类名', row_type_ok))
    for label, ok in checks:
        print('   %s %s' % ('✓' if ok else '✗', label))
    if unresolved:
        print('   ✗ **没解出来的**：')
        for t, ov, why in unresolved:
            print('        %s / %s —— %s' % (t, ov, why))
    n_bad = sum(1 for _l, ok in checks if not ok) + len(unresolved)
    print('   ⇒ %s（自检失败 %d 条）' % ('全部通过' if n_bad == 0 else '有失败', n_bad))

    # ---- markdown
    if args.md:
        print()
        print('| `ObtainableItem` 子类型 | override | prefab（GameObject `m_Name`） | prefab 上的抽屉类 | GUID | 字节偏移(GUID) |')
        print('|---|---|---|---|---|---|')
        for rec in rows:
            g = guid_info.get(rec['drawerGuid'], {})
            print('| `%s` | `Default`(0) | `%s` | `%s` | `%s` | `0x%X` |'
                  % (rec['typeName'], g.get('name') or '**解不出**',
                     ', '.join(g.get('drawerClasses') or []) or '—',
                     rec['drawerGuid'], rec['drawerGuidOff']))
            for ov in rec['overrides']:
                oi = guid_info.get(ov['guid'], {})
                print('| ↑ | `%s`(%d) | `%s` | `%s` | `%s` | `0x%X` |'
                      % (ov['keyName'], ov['key'], oi.get('name') or '**解不出**',
                         ', '.join(oi.get('drawerClasses') or []) or '—',
                         ov['guid'], ov['guidOff']))

    if args.json_out:
        for rec in rows:
            g = guid_info.get(rec['drawerGuid'], {})
            rec['drawerName'] = g.get('name')
            rec['drawerBundle'] = g.get('bundleDir')
            rec['drawerPathId'] = g.get('pathId')
            for ov in rec['overrides']:
                oi = guid_info.get(ov['guid'], {})
                ov['name'] = oi.get('name')
                ov['bundle'] = oi.get('bundleDir')
                ov['pathId'] = oi.get('pathId')
        payload = {'config': cfg, 'guidInfo': guid_info, 'overrideEnum': OVERRIDE}
        with io.open(args.json_out, 'w', encoding='utf-8', newline='\n') as fh:
            json.dump(payload, fh, ensure_ascii=False, indent=1)
        print('\n   写 %s' % args.json_out)

    return 1 if n_bad else 0


if __name__ == '__main__':
    sys.exit(main())
