# R6 只读探针 v6（终版）：6 个币种图标 guid -> 资产名，全链可复现
# 链路：SO 的 m_AssetGUID -> 真包 m_Container(guid -> PathID) -> 真包内 pid -> 对象名
import UnityPy
from UnityPy.files import BundleFile, SerializedFile

BUNDLE = r'D:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/boosterpacks_assets_all.bundle'
GUIDS = {
    # Blackstone（type 20）
    'e7af3237ed78b3c44a4ad914a7bafc8d': 'Blackstone.smallIcon',
    'e70ffaeb2b6324bfda7d25e7608a908e': 'Blackstone.bigIcon',
    # Crystals（type 10）
    '1eaee2ed6b7e34146a92a2d886660889': 'Crystals.smallIcon',
    '65ed84b9693e142caa3bf45bda952471': 'Crystals.bigIcon',
    # Gacha tickets（type 70）
    'a6372675b606846798904ab2a1b63a13': 'GachaTickets.smallIcon',
    '024f9d0226abf40de8d3902412da08b2': 'GachaTickets.bigIcon',
}

env = UnityPy.load(BUNDLE)


def walk(c):
    out = []
    for k, v in c.items():
        if isinstance(v, SerializedFile):
            out.append((str(k), v))
        elif isinstance(v, BundleFile):
            out += walk(v.files)
    return out


for name, sf in walk(env.files):
    print('内层文件', name, '对象', len(sf.objects))
    names = {}
    for pid, r in sf.objects.items():
        try:
            d = r.read()
            nm = getattr(d, 'm_Name', None)
        except Exception:
            nm = None
        names[pid] = (str(getattr(getattr(r, 'type', None), 'name', r.type)), nm)
    cont = getattr(sf, 'container', None) or {}
    print('container 条数', len(cont))
    for key, ptr in cont.items():
        for g, tag in GUIDS.items():
            if g in key:
                pid = ptr if isinstance(ptr, int) else getattr(ptr, 'm_PathID', None)
                info = names.get(pid) or names.get(abs(pid)) or names.get(-abs(pid))
                print('  %-24s pid=%-22s -> %s' % (tag, pid, info))
                break
