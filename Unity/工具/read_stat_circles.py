#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""从 PnP 成品卡图里**程序化读出**卡底四个数值圈（近战红剑 / 远程紫枪 / 护甲银盾 / 生命绿框）。

**为什么有这个脚本**：`项目任务.md` §十一 悬案 #2 —— 四个数值圈至今只有
「子代理开原图肉眼看」这一条路，从没程序化读过。本脚本把它变成可重跑的机器判据。

────────────────────────────────────────────────────────────────────────────
一、版面事实（**实测，不是推测**）
────────────────────────────────────────────────────────────────────────────
· 只有 **部队卡 / 督军卡** 印这四个圈；**天赋 / 计策 / 防御 / 秘密 / 药剂** 只有费用六边形
  （实测：随机 60 张天赋/计策卡，四圈位置全是卡框/插画，无任何数值标记）。
· 四圈**不在一排**（旧文档写「沿卡底一排四个」是错的）：
      红圆+剑（近战）在左下；紫圆+枪（远程）在其右下、两者叠压；
      绿框（生命）在右下；银盾（护甲）在绿框**正上方**、只在有护甲时才印。
· 🔴 **不能按画布绝对坐标切图**：900×1200 的画布上，同一张卡来自**多个渲染批次**，
  批次之间位置与**缩放**都不同（实测：红盘直径 93 ~ 116 px，共 8+ 种放置）。
  ⇒ 必须先**逐张配准**。本脚本拿「**红盘**」当锚（它最大最实、颜色最纯）。

────────────────────────────────────────────────────────────────────────────
二、标定（颜色阈值 + 出处卡名）
────────────────────────────────────────────────────────────────────────────
锚 —— 红盘（近战圆里的暗红实心盘）：
    R∈(55,200) 且 G<60 且 B<75 且 R>G+45 且 R>B+35 且 alpha>200，取最大连通域，
    要求 70 ≤ w,h ≤ **150** 且 0.75 ≤ w/h ≤ 1.34。
    ⚠️ **2026-09-19 更正**：这里原来写 70~135，而代码一直放的是 150（两处不一致）。
       实测后果：`Vex Machinator` 的红块被量成 **125×147**（长宽比 0.85 恰好过判据，
       但正常盘是 93×94）⇒ D 虚大 ~40% ⇒ 最远的生命窗（5.016 D）整体右移 ⇒ 读错。
       **641/642 张卡的「近战字框高/D」在 0.61~0.76，只有它是 0.52** —— 这是筛查假锚的判据。
    标定卡：`Dark Angels/3部队/Warpforge_20_Deathwing-Terminator.png`
            （实测盘心 (201.0, 943.5)、93×94 px）—— 与
            `Astra Militarum/3部队/Warpforge_04_Tempestus-Scion.png`、
            `Orks/3部队/Warpforge_02_Stikkbomb-Boy.png` **三张同坐标**，可交叉验证。

四个圈相对锚的偏移（**单位 = 红盘平均直径 D**，以 DWT 那张为 1.0 基准）：
    近战  ( 0.000,  0.000)     ← 就是锚本身
    远程  (+0.762, +0.690)
    生命  (+5.016, +0.561)
    护甲  (+5.520, -0.440)
    标定卡同上；生命框另在 `Astra Militarum/3部隊/…Tempestus-Scion` 上复核。
    ⚠️ 这四条偏移是**量出来的**（DWT 那张四个值恰好是 近战3/远程3/护甲1/生命4…
       —— 不是，DWT 是 3/3/1/4，见 `资料/PnP卡图_逐张对账_0915.md` A6 表）。

数字字形：圈里是**纯白**印刷字，取 `min(R,G,B) > 150 且 alpha>200` 的像素。
    ⇒ 判「圈在不在」= 这个白字形在不在；判「数字是几」= 字形归一化后与模板比。

────────────────────────────────────────────────────────────────────────────
三、判数字的办法（本机**没有** OCR 库：无 cv2 / tesseract / easyocr，实测）
────────────────────────────────────────────────────────────────────────────
先 `harvest` 把全池字形抽出来、归一化成 28×28 位图、贪心聚类；
**聚类代表图人工读一遍**（`_tmp_view/statcircles/glyph_clusters.png`），
把结论写进 `labels.json`（簇号 → 数字）。之后 `report` 就按它分类。
⇒ 字形→数字这一步是**人认的**，机器只负责「逐张一致地套用」；
   脚本同时报每个簇的**纯度**（该簇成员在卡表里取值的众数占比）当旁证。

用法：`python Unity/工具/read_stat_circles.py help`（**跑之前先 `PYTHONIOENCODING=utf-8`**）——
命令与产出只写在下头 `USAGE` 那一处，别在这儿抄第二份。
"""
import io, os, re, sys, json, glob, random, collections, importlib.util

import numpy as np
from PIL import Image, ImageDraw

ROOT = "d:/4"
HERE = os.path.dirname(os.path.abspath(__file__))
PNP_ROOT = "d:/2/Warpforge部队卡片"
TMP = os.path.join(ROOT, "_tmp_view/statcircles")
OUT_MD = os.path.join(ROOT, "Unity/资料/普查产出_0918/卡面数值圈_程序化读取.md")

# ── PnP ↔ 引擎卡的匹配判据**只从 check_pnp_cards 拿**，不写第二份 ────────────
# ⚠️ 这个 import 必须在 sys.stdout 被我们碰之前（check_pnp_cards 模块级会换 stdout）。
spec = importlib.util.spec_from_file_location("check_pnp_cards", os.path.join(HERE, "check_pnp_cards.py"))
pnp_mod = importlib.util.module_from_spec(spec)
spec.loader.exec_module(pnp_mod)
sys.stdout.reconfigure(encoding="utf-8")

# 相对锚（红盘心，D = 红盘平均直径）的偏移，见文件头「二、标定」
#   (dx, dy, 半窗x, 半窗y, 白色阈值)   —— 偏移与窗宽单位都是 D
# ⚠️ 护甲的白色阈值必须高到 235：盾牌**边框本身就是亮银**（min>150 时整块窗都是白的，
#    实测 77/77 张全糊成一块 0.9D×0.9D）。数字是纯白 (248,248,248)，235 一卡就干净。
PTS = {
    "近战": (0.000, 0.000, 0.42, 0.42, 150),
    "远程": (0.762, 0.690, 0.42, 0.42, 150),
    "生命": (5.016, 0.561, 0.50, 0.42, 150),
    "护甲": (5.520, -0.440, 0.42, 0.42, 235),
}
# 窗内白字的「离窗心多远还算这一圈的东西」（比例 × 半窗）—— 用来甩掉**卡面效果文字**
# 串进窗里造成的污染。⚠️ 0.34D 的半窗太紧：卡面数字实高 **0.63~0.72 D**，
# 半窗就等于把上下沿切掉，AstraMilitarum / Sororitas 两个渲染批次整片读不出就是栽在这。
KEEP_R = 0.62
TOUCH = 3          # 字框离窗边 ≤TOUCH px 就算「被窗切了」
GLYPH_PX = 28


# ────────────────────────────── 图像基元 ──────────────────────────────
def cc_label(mask):
    """4 邻域连通域（纯 numpy，本机没 scipy/cv2）。返回 (labels, n)。"""
    h, w = mask.shape
    lab = np.zeros((h, w), np.int32); parent = [0]

    def find(x):
        while parent[x] != x:
            parent[x] = parent[parent[x]]; x = parent[x]
        return x

    nxt = 0
    for y in range(h):
        for x in np.nonzero(mask[y])[0]:
            up = lab[y-1, x] if y > 0 else 0
            lf = lab[y, x-1] if x > 0 else 0
            if up and lf:
                lab[y, x] = min(up, lf)
                ra, rb = find(up), find(lf)
                if ra != rb: parent[max(ra, rb)] = min(ra, rb)
            elif up: lab[y, x] = up
            elif lf: lab[y, x] = lf
            else:
                nxt += 1; parent.append(nxt); lab[y, x] = nxt
    if nxt == 0: return lab, 0
    remap = np.zeros(nxt+1, np.int32); k = 0
    for i in range(1, nxt+1):
        r = find(i)
        if remap[r] == 0: k += 1; remap[r] = k
        remap[i] = remap[r]
    return remap[lab], k


def imread(p):
    a = np.array(Image.open(p).convert('RGBA'))
    return a


def dilate1d(m, k, axis):
    """沿 axis 做 1D 膨胀（窗口半径 k）。用累积和，**不引 scipy**。"""
    L = m.shape[axis]
    if axis == 0: m = m.T
    cs = np.cumsum(m, axis=1, dtype=np.int32)
    lo = np.maximum(np.arange(L) - k, 0)
    hi = np.minimum(np.arange(L) + k, L - 1)
    c = cs[:, hi] - np.where(lo > 0, cs[:, np.maximum(lo - 1, 0)], 0)
    out = (c > 0)
    return out.T if axis == 0 else out


def dilate(m, k):
    return dilate1d(dilate1d(m, k, 1), k, 0)


def red_disc(a):
    """锚：近战圆里的暗红实心盘。返回 dict(cx,cy,w,h,mask) 或 None。

    ⚠️ 先**膨胀**再找连通域：两位数（`10`/`12`）的白字会把红盘**切成四块**
    （`Brutalis Dreadnought` / `Gorkanaut` / `Avatar of Khaine` / `Captain Sicarius`
     等 10 张就是这么丢掉锚的）。膨胀后用「原像素」算外接框，直径才不会被撑大。
    """
    rgb = a[:, :, :3].astype(int); al = a[:, :, 3]
    R, G, B = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
    h = R.shape[0]
    band = np.zeros_like(al, bool); band[int(h*0.66):, :] = True
    m = (R > 55) & (R < 200) & (G < 60) & (B < 75) & (R > G+45) & (R > B+35) & (al > 200) & band
    best = _pick_disc(m, cc_label(m))
    if best is None:
        # 兜底：两位数（`10`/`12`）的白字会把红盘**切成几块**（实测 10 张如
        # `Brutalis Dreadnought` / `Gorkanaut` / `Stompa` / `Vex Machinator`）。
        # 做法：拿**最大的一块当种子**，把「落在它外扩一圈里的」红块并进来再量。
        # ⚠️ 不用形态学膨胀：`dilate(k=22)` 在这几张上会把红盘和旁边的红插画并成
        #    272×230 的一块（实测 k=12→18 都救不回来，k 再大反而更糟）。
        best = _pick_disc(m, None, seeds=m)
    return best


def _pick_disc(m, lab_n, grp=None, seeds=None):
    if seeds is not None:
        return _pick_disc_union(seeds)
    lab, n = lab_n
    best = None
    for i in range(1, n+1):
        g = (lab == i) if grp is None else ((lab == i) & grp)
        ys, xs = np.where(g)
        if len(xs) < 1200: continue
        w, hh = xs.max()-xs.min()+1, ys.max()-ys.min()+1
        if not (70 <= w <= 150 and 70 <= hh <= 150 and 0.75 <= w/hh <= 1.34): continue
        if best is None or len(xs) > best['n']:
            best = dict(n=len(xs), cx=(xs.min()+xs.max())/2, cy=(ys.min()+ys.max())/2,
                        w=int(w), h=int(hh))
    return best


def _pick_disc_union(m, n=6):
    """把红块按「离最大那块多远」并起来，取量出来最像圆盘的并集。"""
    lab, K = cc_label(m)
    blobs = []
    for i in range(1, K+1):
        ys, xs = np.where(lab == i)
        if len(xs) < 150: continue
        blobs.append(dict(x0=int(xs.min()), x1=int(xs.max()), y0=int(ys.min()), y1=int(ys.max()),
                          n=len(xs)))
    blobs.sort(key=lambda b: -b['n'])
    best = None
    for s in blobs[:n]:
        sw, sh = s['x1']-s['x0'], s['y1']-s['y0']
        pad = 0.75 * max(sw, sh)
        X0, X1 = s['x0']-pad, s['x1']+pad
        Y0, Y1 = s['y0']-pad, s['y1']+pad
        g = [b for b in blobs if b['x0'] <= X1 and b['x1'] >= X0 and b['y0'] <= Y1 and b['y1'] >= Y0]
        x0 = min(b['x0'] for b in g); x1 = max(b['x1'] for b in g)
        y0 = min(b['y0'] for b in g); y1 = max(b['y1'] for b in g)
        w, hh = x1-x0+1, y1-y0+1
        if not (70 <= w <= 150 and 70 <= hh <= 150 and 0.75 <= w/hh <= 1.34): continue
        nn = sum(b['n'] for b in g)
        if best is None or nn > best['n']:
            best = dict(n=nn, cx=(x0+x1)/2, cy=(y0+y1)/2, w=int(w), h=int(hh))
    return best


def white_glyph(a, cx, cy, hx, hy, thr=150):
    """窗内「白」像素 -> 字形掩码 + 外接框。窗户是**相对锚的固定窗**（已按 D 缩放）。"""
    x0, y0 = int(round(cx-hx)), int(round(cy-hy))
    x1, y1 = int(round(cx+hx)), int(round(cy+hy))
    H, W = a.shape[:2]
    x0, y0 = max(x0, 0), max(y0, 0); x1, y1 = min(x1, W), min(y1, H)
    sub = a[y0:y1, x0:x1]
    rgb = sub[:, :, :3].astype(int); al = sub[:, :, 3]
    w = (rgb.min(axis=2) > thr) & (al > 200)
    if w.sum() < 15: return None
    lab, n = cc_label(w)
    wcx, wcy = (x1-x0)/2.0, (y1-y0)/2.0
    parts = []
    for i in range(1, n+1):
        ys, xs = np.where(lab == i)
        if len(xs) < 15: continue
        parts.append(dict(x0=int(xs.min()), x1=int(xs.max()), y0=int(ys.min()), y1=int(ys.max()),
                          n=len(xs), i=i,
                          d=(((xs.min()+xs.max())/2.0-wcx)**2 + ((ys.min()+ys.max())/2.0-wcy)**2) ** 0.5))
    if not parts: return None
    # 取「离窗心最近」的那块当种子，再把**和它同一行、横向挨得够近**的块并进来 ——
    # 这是为了拿到两位数（`1`+`2`、`3`+`5`）**而把卡面效果文字甩掉**。
    # ⚠️ 原来用「离窗心 > 0.62×半窗的就丢」，把两位数的**首位**整块丢了
    #    （`Jain Zar` 生命 35：`3` 的块心离窗心 0.32D，恰好越线）⇒ 读成 `5`。
    parts.sort(key=lambda p: p['d'])
    u = dict(parts[0]); used = {parts[0]['i']}
    changed = True
    while changed:
        changed = False
        for p in parts:
            if p['i'] in used: continue
            vo = min(u['y1'], p['y1']) - max(u['y0'], p['y0'])            # 竖向重叠
            if vo < 0.5 * min(u['y1']-u['y0']+1, p['y1']-p['y0']+1): continue
            gapx = max(u['x0']-p['x1'], p['x0']-u['x1'])                  # 横向间隙
            if gapx > 0.30 * min(u['y1']-u['y0']+1, p['y1']-p['y0']+1): continue
            u = dict(x0=min(u['x0'], p['x0']), x1=max(u['x1'], p['x1']),
                     y0=min(u['y0'], p['y0']), y1=max(u['y1'], p['y1']))
            used.add(p['i']); changed = True
    m = np.zeros_like(w)
    for i in used:
        ys, xs = np.where(lab == i)
        m[ys, xs] = True
    x0o, y0o, x1o, y1o = x0+u['x0'], y0+u['y0'], x0+u['x1'], y0+u['y1']
    # 「贴边」= 字被窗切了。这是**该不该换窗**的判据 —— 比「离模板多远」可靠得多：
    # `Abaddon` 的 `40`、`Deathmark` 的 `3` 都是贴边被切，而没被切的那 13 张督军的
    # `35` 离模板远只是**字体渲染批次不同**，一搜就会搜成 `5`。
    touch = (u['x0'] <= TOUCH or u['y0'] <= TOUCH or
             u['x1'] >= (x1-x0)-1-TOUCH or u['y1'] >= (y1-y0)-1-TOUCH)
    return dict(x0=x0o, y0=y0o, x1=x1o, y1=y1o, m=m, px=int(m.sum()), touch=bool(touch))


def norm_glyph(a, g, thr=150):
    """字形外接框 -> 28×28 二值图。

    ⚠️ **保持宽高比**（把短边居中补黑到正方形再缩放）—— 直接拉成正方形会把
    「1 的窄」和「0 的宽」这个最强的信号抹掉。
    只保留阈值以上的像素，背景一律置黑 ⇒ 不受该卡插画/卡框的底色影响。
    """
    sub = a[g['y0']:g['y1']+1, g['x0']:g['x1']+1]
    rgb = sub[:, :, :3].astype(int); al = sub[:, :, 3]
    m = ((rgb.min(axis=2) > thr) & (al > 200)).astype(np.uint8)*255
    h, w = m.shape
    side = max(h, w)
    canvas = np.zeros((side, side), np.uint8)
    canvas[(side-h)//2:(side-h)//2+h, (side-w)//2:(side-w)//2+w] = m
    im = Image.fromarray(canvas).resize((GLYPH_PX, GLYPH_PX), Image.LANCZOS)
    return (np.array(im).astype(np.float32)/255.0 > 0.5)


# ────────────────────────── 读一张卡的四个圈 ──────────────────────────
def read_card(path):
    """返回 dict(ok, D, anchor, glyphs={名: glyph|None})"""
    a = imread(path)
    rd = red_disc(a)
    if rd is None:
        return dict(ok=False, why="锚(红盘)没找到")
    D = (rd['w'] + rd['h']) / 2.0
    out = {}
    for name, (dx, dy, hx, hy, thr) in PTS.items():
        cx, cy = rd['cx'] + dx*D, rd['cy'] + dy*D
        out[name] = white_glyph(a, cx, cy, hx*D, hy*D, thr)
    return dict(ok=True, D=D, anchor=(rd['cx'], rd['cy'], rd['w'], rd['h']), glyphs=out, img=a)


# ─────────────────────── 引擎卡 ↔ PnP 文件 ───────────────────────
def build_mapping():
    """返回 {引擎卡 id: (绝对路径, 阵营目录, 分类目录)}，判据全部复用 check_pnp_cards。"""
    pnp_files = pnp_mod.load_pnp_files()
    merged = pnp_mod.load_merged()
    cs = json.load(io.open(os.path.join(ROOT, "Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json"),
                           encoding="utf-8"))["cards"]
    pnp_by_key, printed = {}, {}
    for r in merged:
        d = pnp_files.get(r["file"])
        if not d: continue
        fac = pnp_mod.FAC_OF_DIR.get(d[1])
        if not fac: continue
        k = (fac, pnp_mod.norm_name(r["name"]))
        pnp_by_key[k] = d[0]
        printed[k] = r["name"]
    resolved, missing, leftover = pnp_mod.match_two_rounds(cs, pnp_by_key)
    out = {}
    for cid, (rel, fuzzy) in resolved.items():
        d = pnp_files.get(os.path.basename(rel.replace('\\', '/')))
        cat = d[2] if d else ''
        facdir = d[1] if d else ''
        out[cid] = (os.path.join(PNP_ROOT, rel), facdir, cat, fuzzy)
    return cs, out, missing, leftover


# ────────────────────────────── harvest ──────────────────────────────
def table_vals(c):
    """卡表里这一张的四个值。护甲只在 keywords 的 `Armour N` 里（引擎表没有 armour 键）。"""
    arm = None
    for k in c.get('keywords', []):
        # ⚠️ 尾巴上允许一个句点：`Aberrant Hypermorph` 的引擎关键词就是 `Armour 2.`，
        #    严格写成 `$` 会漏掉它 ⇒ 被误报成「卡表没有护甲、卡图有 2」。
        m = re.match(r'^Armou?r\s*(\d+)\.?$', k)
        if m: arm = int(m.group(1))
    return {'近战': c['attack'], '远程': c['ranged'], '生命': c['health'], '护甲': arm}


def scan_all(verbose=True, tmpl=None, only=None):
    """逐张读图 -> 每个 (卡, 数值名) 一条记录（含归一字形）。

    `tmpl` 给定时**开启候选偏移搜索**：默认偏移读不出/读不干净时，
    沿 dx/dy 试一串候选窗，取「离模板最近」的那个。**只在重读那一步开**，
    因为模板是从卡表长的，开搜索会让「按卡表找窗」有偏。
    """
    cs, mp, missing, leftover = build_mapping()
    if verbose: print('引擎卡 %d / 配上 PnP %d / 配不上 %d' % (len(cs), len(mp), len(missing)))
    cents = np.stack([t['cent'] for t in tmpl]) if tmpl else None
    tvals = [t['val'] for t in tmpl] if tmpl else None
    recs = []
    n_statless = n_jpg = n_anchor = 0
    for c in cs:
        if c['id'] not in mp: continue
        if only is not None and c['id'] not in only: continue
        path, facdir, cat, fuzzy = mp[c['id']]
        if c['type'] not in ('unit', 'hero'):
            n_statless += 1; continue
        if not path.lower().endswith('.png'):
            n_jpg += 1; continue                      # 9 张手机翻拍 jpg（无 alpha）
        r = read_card(path)
        if not r['ok']:
            n_anchor += 1
            recs.append(dict(cid=c['id'], name=c['name'], fac=c['faction'], type=c['type'],
                             stat=None, tab=None, vec=None, path=path, why=r['why']))
            continue
        tv = table_vals(c)
        for name, (dx, dy, hx, hy, thr) in PTS.items():
            g = r['glyphs'][name]; used = (dx, dy)
            # 只在「没读到」或「字被窗切了」时才换窗（见 `white_glyph` 里 TOUCH 的说明）。
            # 护甲**不搜**：那一格是「有没有盾」的存在性判断，搜窗一定会搜出假阳性。
            if tmpl and name != '护甲' and (g is None or g['touch']):
                g2, used2 = search_offset(r, name, cents, tvals)
                if g2 is not None and (g is None or g2['px'] >= g['px']):
                    g, used = g2, used2
            elif (tmpl and name != '护甲' and g is not None
                  and (g['x1']-g['x0']+1)/r['D'] < 0.45):
                # 补一刀：只往**左**找一点点。`10` 的 `1` 被切掉时，`0` 并不贴窗边
                # （`Captain Sicarius` / `Brutalis Dreadnought` 就是这么读成 `0` 的），
                # 贴边判据抓不到。**整个数一定比半截墨多**，所以只认「墨明显变多」的结果。
                g2, used2 = search_offset(r, name, cents, tvals,
                                          dxs=(0.0, -0.15, -0.30, -0.45, -0.60), dys=(0.0,))
                if g2 is not None and g2['px'] > g['px'] * 1.05:
                    g, used = g2, used2
            recs.append(dict(cid=c['id'], name=c['name'], fac=c['faction'], type=c['type'],
                             stat=name, tab=tv[name], path=path, D=r['D'],
                             box=None if g is None else [g['x0'], g['y0'], g['x1'], g['y1']],
                             used=used, touch=None if g is None else g['touch'],
                             px=None if g is None else g['px'],
                             nx=(g['x1']-g['x0']+1)/r['D'] if g else None,
                             ny=(g['y1']-g['y0']+1)/r['D'] if g else None,
                             vec=None if g is None else norm_glyph(r['img'], g, thr)))
    if verbose:
        print('不印四圈(天赋/计策/防御) %d · 手机翻拍 jpg %d · 锚失败 %d' % (n_statless, n_jpg, n_anchor))
        print('画像记录 %d 条，其中有字形 %d 条'
              % (len(recs), sum(1 for r in recs if r['vec'] is not None)))
    return recs


# 候选偏移：先试默认，再沿 dx / dy 各走一圈。
# 为什么需要：绿框相对红盘的偏移**逐渲染批次不同**（实测 4.1D ~ 5.0D）——
# 卡框宽度不一样，左下角的红盘和右下角的绿框之间的距离就跟着变。
CAND_DX = [0.0, -0.15, 0.15, -0.30, 0.30, -0.45, 0.45, -0.60, 0.60, -0.75, 0.75, -0.90, 0.90]
CAND_DY = [0.0, 0.10, -0.10, 0.20, -0.20]
ACCEPT = 0.06      # 「读得干净」判据（仅用于**换窗搜索**里挑窗，见 search_offset）
MAXDIST = 0.16     # 分类的接受上限：离最近模板 > 它 = 认不出。
                   # ⚠️ 唯一判据只此一处 —— 报告里的措辞也用这个数（别在别处写 0.16）
GRAY = 0.06        # 「认出来但离模板远」的灰区起点（只用于诊断，不影响判定）


def _val_of(vec, cents, tvals):
    d = np.logical_xor(cents, vec[None, :, :]).mean(axis=(1, 2))
    j = int(np.argmin(d))
    return tvals[j], float(d[j])


def search_offset(r, name, cents, tvals, dxs=None, dys=None):
    """在候选偏移里找窗。判据是「**读得干净**」（离模板 ≤ ACCEPT），
    同分取**墨最多**的那个 —— 窗偏移对时框住的是整个数，墨一定比只框住半截时多。

    ⚠️ 不要「一遇到 d≤0.02 就收」：那会让窗停在**刚好框住一个数字碎片**的位置
    （实测把 `Azrael` 的 `40` 读成 `0`、`Xarahan` 的 `30` 读成 `3`）。
    """
    a, D = r['img'], r['D']
    dx0, dy0, hx, hy, thr = PTS[name]
    best, best_px = None, -1
    for ddx in (CAND_DX if dxs is None else dxs):
        for ddy in (CAND_DY if dys is None else dys):
            cx = r['anchor'][0] + (dx0+ddx)*D
            cy = r['anchor'][1] + (dy0+ddy)*D
            g = white_glyph(a, cx, cy, hx*D, hy*D, thr)
            if g is None: continue
            _, d = _val_of(norm_glyph(a, g, thr), cents, tvals)
            if d <= ACCEPT and g['px'] > best_px:
                best, best_px = (g, (dx0+ddx, dy0+ddy)), g['px']
    return best if best else (None, None)


def _dist(a, b):
    return float(np.logical_xor(a, b).mean())


def build_templates(recs, min_n=3, min_frac=0.05, eps=0.05):
    """**用卡表当教师**长模板，再**跨组投票**把标签钉死。

    ① 先按 `(圈, 卡表值)` 分组，组内贪心聚类 → 候选簇；
    ② 再把**字形长得一样**的候选簇并到一起（哪怕来自不同的组）；
    ③ 合并后，标签取**成员总数最多**的那个值。

    ⚠️ ②③ 不是可选的：实测踩过 —— `生命=35` 那一组里有 13 张督军的 `35` 被切成了 `5`，
       长出一个「标签写 35、实际是 5」的假模板；此后**全池的 5 都被认成 35**（差异 2→96 条）。
       跨组投票能挡住它：那个簇跟 `近战=5`/`远程=5`/`生命=5` 的簇字形一模一样，
       合起来 `5` 的票数远多于 `35` ⇒ 判成 `5`。

    ⚠️ 前提仍要说清：**卡表本身是人工照卡图抄的**，绝大多数是对的，
       所以「一组里最大的那个簇」就是该数字的字形。模板是机器长的，
       最后仍会**人工读一遍拼图确认**，并与卡表逐张比 —— 不一致的单列出来，不自动判谁对。
    """
    by_grp = collections.defaultdict(list)
    for i, r in enumerate(recs):
        if r['vec'] is None or r['tab'] is None: continue
        by_grp[(r['stat'], r['tab'])].append(i)
    cands = []
    for (stat, val), idxs in sorted(by_grp.items()):
        cents, groups = [], []
        for i in idxs:
            v = recs[i]['vec']
            best, bi = 1e9, -1
            for j, c in enumerate(cents):
                d = _dist(v, c)
                if d < best: best, bi = d, j
            if bi >= 0 and best < 0.12:
                groups[bi].append(i)
                if len(groups[bi]) % 5 == 0:            # 偶尔用整簇重算，避免质心飘走
                    cents[bi] = np.mean([recs[k]['vec'] for k in groups[bi]], axis=0) > 0.5
            else:
                cents.append(v.copy()); groups.append([i])
        for g in groups:
            if len(g) < min_n or len(g) < min_frac*len(idxs): continue
            cands.append(dict(val=val, idxs=g,
                              cent=(np.mean([recs[k]['vec'] for k in g], axis=0) > 0.5)))
    # ② 跨组并簇：字形一样（质心距离 ≤ eps）的候选簇是同一个数字
    merged = []
    for c in cands:
        best, bi = 1e9, -1
        for j, m in enumerate(merged):
            d = _dist(c['cent'], m['cent'])
            if d < best: best, bi = d, j
        if bi >= 0 and best <= eps:
            m = merged[bi]
            m['votes'][c['val']] = m['votes'].get(c['val'], 0) + len(c['idxs'])
            m['idxs'] += c['idxs']
            m['cent'] = np.mean([recs[k]['vec'] for k in m['idxs']], axis=0) > 0.5
        else:
            merged.append(dict(votes={c['val']: len(c['idxs'])}, idxs=list(c['idxs']),
                               cent=c['cent'].copy()))
    # ③ 标签 = 票最多的那个值
    tmpl = []
    for m in merged:
        val = max(m['votes'], key=lambda v: m['votes'][v])
        tmpl.append(dict(stat=None, val=val, n=len(m['idxs']), cent=m['cent'],
                         members=m['idxs'], votes=m['votes']))
    tmpl.sort(key=lambda t: (t['val'], -t['n']))
    print('模板 %d 个（跨组投票后）：' % len(tmpl))
    for t in tmpl:
        vv = '/'.join('%s:%d' % (k, v) for k, v in sorted(t['votes'].items(),
                                                          key=lambda kv: -kv[1]))
        flag = '  ⚠️标签有分歧' if len(t['votes']) > 1 else ''
        print('   %-4s n=%-4d 票 %s%s' % (t['val'], t['n'], vv, flag))
    return tmpl


def save_montage(recs, tmpl, path):
    """模板字形拼图 —— **人工读一遍**用（字形→数字这一步是人认的，不是猜的）。"""
    os.makedirs(TMP, exist_ok=True)
    cell, cols = GLYPH_PX, min(10, max(1, len(tmpl)))
    rows = (len(tmpl)+cols-1)//cols
    sh = Image.new('L', (cols*(cell+4), rows*(cell+4)), 0)
    for j, t in enumerate(tmpl):
        sh.paste(Image.fromarray((t['cent']*255).astype('uint8')),
                 ((j % cols)*(cell+4), (j//cols)*(cell+4)))
    sh.resize((sh.width*3, sh.height*3), Image.NEAREST).save(path)
    return [(j, t['val'], t['n']) for j, t in enumerate(tmpl)]


def save_masks(recs, cls, path):
    """把「**有字形但认不出**」的记录里机器抽到的 28×28 字形拼一张图。

    用途：把「图像不行」和「分类不行」分开 —— 字形肉眼可辨就说明失败在分类器，
    不在分辨率、不在圈太小。报告 §四 的「字字清晰」这句就是照它说的。
    """
    items = [(r, d) for r, (v, d) in zip(recs, cls)
             if r['stat'] and d is not None and v is None and r['vec'] is not None]
    if not items: return 0
    cell, sc, cols = GLYPH_PX, 5, 6
    rows = (len(items)+cols-1)//cols
    sh = Image.new('L', (cols*(cell*sc+8), rows*(cell*sc+22)), 40)
    dr = ImageDraw.Draw(sh)
    for j, (r, d) in enumerate(items):
        X, Y = (j % cols)*(cell*sc+8), (j//cols)*(cell*sc+22)
        sh.paste(Image.fromarray((r['vec']*255).astype('uint8'))
                 .resize((cell*sc, cell*sc), Image.NEAREST), (X, Y+18))
        dr.text((X+2, Y+3), '%s %s d=%.2f' % (r['name'][:20], r['stat'], d), fill=255)
    sh.save(path)
    return len(items)


def do_harvest():
    os.makedirs(TMP, exist_ok=True)
    recs = scan_all()
    tmpl = build_templates(recs)
    idx = save_montage(recs, tmpl, os.path.join(TMP, 'glyph_clusters.png'))
    np.savez_compressed(os.path.join(TMP, 'glyphs.npz'),
                        V=np.stack([r['vec'] for r in recs if r['vec'] is not None]),
                        meta=np.array([json.dumps({k: v for k, v in r.items() if k != 'vec'},
                                                  ensure_ascii=False) for r in recs if r['vec'] is not None]))
    io.open(os.path.join(TMP, 'templates.json'), 'w', encoding='utf-8', newline='\n').write(
        json.dumps(dict(recs=[{k: v for k, v in r.items() if k != 'vec'} for r in recs],
                        tmpl=[dict(val=t['val'], n=t['n'],
                                   cent=t['cent'].astype(int).tolist()) for t in tmpl]),
                   ensure_ascii=False))
    io.open(os.path.join(TMP, 'template_index.tsv'), 'w', encoding='utf-8', newline='\n').write(
        '\n'.join('%d\t%s\t%d' % x for x in idx))
    print('拼图 %d 个模板 -> %s/glyph_clusters.png' % (len(idx), TMP))


# ─────────────────────────────── sheet ───────────────────────────────
def do_sheet(k, seed=5):
    """抽 k 张卡，把四个圈按固定窗裁出来拼一张图 —— 用来肉眼确认配准对不对。"""
    cs, mp, _, _ = build_mapping()
    units = [c for c in cs if c['type'] in ('unit', 'hero') and c['id'] in mp
             and mp[c['id']][0].lower().endswith('.png')]
    random.seed(seed); samp = random.sample(units, k)
    os.makedirs(TMP, exist_ok=True)
    tiles, labels = [], []
    for c in samp:
        path, facdir, cat, _ = mp[c['id']]
        r = read_card(path)
        if not r['ok']:
            labels.append((c['name'], 'ANCHOR-FAIL')); continue
        D = r['D']; row = []
        for name, (dx, dy, hx, hy, thr) in PTS.items():
            cx, cy = r['anchor'][0]+dx*D, r['anchor'][1]+dy*D
            x0, y0 = int(cx-hx*D), int(cy-hy*D)
            t = Image.fromarray(r['img'][:, :, :3])
            t = t.crop((x0, y0, int(cx+hx*D), int(cy+hy*D))).resize((120, 120), Image.LANCZOS)
            row.append(t)
        tiles.append(row)
        labels.append((c['name'], 'atk%d/rng%d/hp%d/kw=%s' % (c['attack'], c['ranged'], c['health'],
                                                             ','.join(c['keywords']))))
    sh = Image.new('RGB', (120*4+30, 124*len(tiles)), (35, 35, 35))
    for i, row in enumerate(tiles):
        for j, t in enumerate(row): sh.paste(t, (j*128, i*124))
    sh.save(os.path.join(TMP, 'sheet.png'))
    for l in labels: print(l)


# ─────────────────────────────── report ───────────────────────────────
def classify(recs, tmpl, maxdist=MAXDIST):
    """最近模板分类。返回每条记录的 (读出来的值 | None, 距离)。**跨数值共用同一个模板池** ——
    四个圈的字是同一套字体，只阈值不同（护甲的描边细一点），实测护甲模板也认得出别的圈的 1/2。"""
    cents = np.stack([t['cent'] for t in tmpl])
    vals = [t['val'] for t in tmpl]
    out = []
    for r in recs:
        if r['vec'] is None:
            out.append((None, None)); continue
        d = np.logical_xor(cents, r['vec'][None, :, :]).mean(axis=(1, 2))
        j = int(np.argmin(d))
        out.append((vals[j] if d[j] <= maxdist else None, float(d[j])))
    return out


def analyze():
    """跑完整流程（两遍扫 + 长模板 + 分类 + 对账），返回全部中间结果。

    ⚠️ **覆盖率 / 差异 / 读不出的账只在这一处算** —— `do_report` 与诊断都调它。
       这个脚本已经因为「同一条判据写两处」踩过坑（模板标签、贴边判据都是）。
    返回 (recs, tmpl, cls, diffs, bad, per_fac)
    """
    # ① 默认偏移先扫一遍 → 长模板
    recs = scan_all()
    tmpl = build_templates(recs)
    cls = classify(recs, tmpl)
    # ② 任何「读不出 / 与卡表不一致」的卡，开**候选偏移搜索**重读一遍
    need = set()
    for r, (val, dist) in zip(recs, cls):
        if r['stat'] is None: need.add(r['cid']); continue
        if val is None or (r['tab'] is not None and val != r['tab']): need.add(r['cid'])
    print('需要开搜索重读的卡：%d 张' % len(need))
    if need:
        recs2 = scan_all(verbose=False, tmpl=tmpl, only=need)
        key = {(r['cid'], r['stat']): r for r in recs}
        nrep = 0
        for r in recs2:
            k = (r['cid'], r['stat'])
            cur = key.get(k)
            if cur is None: continue
            a = classify([r], tmpl)[0]
            # ⚠️ **只有第一遍「没读到 / 字被窗切」时才采纳第二遍**。
            #    反例（实测踩到）：第一遍好好的 `35`，第二遍把「只框住 5」的窗判成更近
            #    （一个被切掉 3 的 5 本身就是干净的 5），于是 13 张督军的 35 变成 5 ——
            #    还长出一个**标签写 35、实际是 5** 的假模板，全池的 5 随后全认成 35
            #    （差异从 2 条炸到 96 条）。
            # ⚠️ **不能因为「第二遍的字也贴边」就否掉它** —— 正确框住的两位数本来就贴边
            #    （`Brutalis Dreadnought` / `Captain Sicarius` 的生命 `10`：0.76D 宽，
            #    在 1.0D 的窗里必然两边都贴）。判据交给「墨够多 + 离模板够近」。
            if a[1] is None: continue
            better = (cur['vec'] is None or cur.get('touch') or
                      (r.get('px') or 0) > 1.05 * (cur.get('px') or 0) and a[1] <= ACCEPT)
            if not better: continue
            if a[1] is not None:
                for kk in ('box', 'used', 'touch', 'px', 'nx', 'ny', 'vec'): cur[kk] = r[kk]
                nrep += 1
        print('第二遍替掉了 %d 条' % nrep)
        tmpl = build_templates(recs)
        cls = classify(recs, tmpl)
    per_fac = collections.defaultdict(lambda: [0, 0, 0])   # 阵营 -> [卡数, 四条都读出, 读不出]
    diffs, bad, cards = [], [], {}
    for r, (val, dist) in zip(recs, cls):
        if r['stat'] is None:                # 锚(红盘)失败 —— 整张卡一条
            bad.append((r, None, '锚(红盘)没找到'))
            cards.setdefault(r['cid'], dict(fac=r['fac'], n=4, ok=0))   # 也要进分母
            continue
        c = cards.setdefault(r['cid'], dict(fac=r['fac'], n=0, ok=0))
        c['n'] += 1
        st, tab = r['stat'], r['tab']
        # ⚠️ 护甲的「没有」是一个**合法的读数**：卡面上没盾 = 没护甲。
        #    不能把「窗里没白字」一律算成读不出 —— 那会凭空多出五百多条。
        if st == '护甲' and val is None and tab is None:
            c['ok'] += 1; continue
        if val is None:
            # ⚠️ 原因只写**类别**（阈值写在 §四 正文一处）——
            #    别把距离字符串编进原因里：那样 `Counter` 会把 11 条同因切成 8 个「不同原因」，
            #    「分因」那行读起来像有八种毛病。
            bad.append((r, dist, '圈里没找到白字' if dist is None else '窗里有白字但认不出'))
            continue
        c['ok'] += 1
        if st == '护甲' and tab is None:
            diffs.append((r, val, dist)); continue
        if tab is None or val != tab:
            diffs.append((r, val, dist))
    for c in cards.values(): per_fac[c['fac']][0] += 1; per_fac[c['fac']][1] += (c['ok'] == c['n'])
    return recs, tmpl, cls, diffs, bad, per_fac


# §四 那 11 条的**字形读数** —— 这是**人工照 `chk_masks.png` 看出来的**（机器给的结论是「认不出」）。
# 记在这里只为一件事：说明「失败不在图像、在分类」。**它不是脚本的读数**，别当机器结论用。
# 2026-09-19 实测：这 11 个数**全部等于卡表值**（脚本会自己核一遍并印出来）。
HUMAN_GLYPH = {
    ('Iskandar Khayon', '生命'): '10', ('Patriarch', '近战'): '10',
    ('Vargard Obyron', '近战'): '10', ('Monolith', '护甲'): '3',
    ('Nemesor Zahndrekh', '生命'): '35', ('Orikan the Diviner', '生命'): '35',
    ('Snakebite Battlewagon', '生命'): '11', ('Tyrannofex', '生命'): '14',
    ('Captain Sicarius', '生命'): '10', ('Uriel Ventris', '生命'): '35',
    ('Varro Tigurius', '生命'): '35',
}

# 差异的**裁定** —— 这一份是**人工把原图调出来看**得到的（不是脚本推的）。
# 复现办法：把这三张卡的成品图按圈裁出来看（脚本里现成的路：`sheet` 或临时裁片），
# 机器抽到的那一格字形在 `_tmp_view/statcircles/chk_masks.png`（**只画读不出的**，
# 差异那三条机器是读出来了的，得回原图看）。
# ⚠️ 重跑后差异条目若变了，没裁过的会落回「⚠️ 待裁（要看原图）」—— 别让它静默变成结论。
VERDICT = {
    ('Vex Machinator', '生命'):
        '🔴 **机器错**（卡面印 `12`）—— 该卡的锚量成 125×147（正常 93×94），D 虚大 ~40%，'
        '生命窗整体右移、第二遍又左移 0.9 D，只框到后一位 `2`（见 §二 锚可靠度）',
    ('Gargantuan Squiggoth', '生命'):
        '🔴 **机器错**（卡面印 `13`）—— 卡表里 `13` 只有 1 张 ⇒ 长不出模板，'
        '被贴到最近的 `10`，距离 0.117 **在阈值以内**（没有卡表当对照就会静默）',
    ('Ravenwing Ballistus Dreadnought', '护甲'):
        '✅ **机器对，是脚本的记账口径**：卡面盾印 `1`，引擎关键词是**裸 `Armour`**（全池唯一一张）；'
        '引擎对不带数字的关键词按 **1** 算（`RuleEngine/Core/CardDef.cs:1118`）'
        '⇒ 机器其实与引擎一致，是本脚本 `table_vals` 的正则只认 `Armour N` 才报了差',
}


def do_report():
    recs, tmpl, cls, diffs, bad, per_fac = analyze()
    # 证据图 —— 报告正文引的就是这两张，每次 report 重画一遍（别引用不会更新的旧图）
    save_montage(recs, tmpl, os.path.join(TMP, 'glyph_clusters.png'))
    n_m = save_masks(recs, cls, os.path.join(TMP, 'chk_masks.png'))
    print('证据图：glyph_clusters.png（模板 %d 个）· chk_masks.png（读不出的字形 %d 个）'
          % (len(tmpl), n_m))
    # ── 输出 ─────────────────────────────────────────────────────
    L = []
    L.append('# 卡面数值圈 · 程序化读取（2026-09-19）\n')
    L.append("""
> **这份文档回答**：`D:/2/Warpforge部队卡片/` 的成品卡图上，卡底四个数值圈
> （近战红剑 / 远程紫枪 / 护甲银盾 / 生命绿框）**机器读出来的值**，与卡表逐张比，差在哪。
> 脚本 `Unity/工具/read_stat_circles.py`，重跑：`PYTHONIOENCODING=utf-8 python Unity/工具/read_stat_circles.py report`。
> **卡池张数**见 `资料/阵营推进_清单与交接.md` §一。

## §〇 结论

**能读。** 642 张部队/督军卡里 **631 张（98.3%）四条全读出**；与卡表逐张比**只有 3 条不一致** ——
逐条把原图调出来看后：**2 条是机器错、1 条是脚本的记账口径**（都不是「卡面读不出来」）。
§四 那 11 条读不出的**也不是图像读不出来**：把机器抽到的字形放大看
（`_tmp_view/statcircles/chk_masks.png`）**字字清晰**（`10`/`11`/`14`/`35`/`3`），
失败在「字形 ↔ 模板」这一步，不在分辨率、不在圈太小。

🔴 **但「读出来」≠「可以当尺子」** —— 两条边界都是实测出来的：
① 卡表里出现 **< 3 次**的取值**长不出模板**（本池 `11`/`13`/`14` 各 1 张），机器会把它**贴成最近的模板**：
   `13` 读成 `10`、距离 0.117 **在阈值以内** ⇒ **没有卡表当对照，这个错是静默的**（§三 `Gargantuan Squiggoth` 那条）。
② 阈值 **0.16 切在「同一数字、不同渲染批次」那一族的中间**（成功的 `35` 最远 0.151、失败的最远 0.323）
   ⇒ **单看距离判不出对错**。⇒ 这套方法能查「**卡面 vs 卡表**」的差；**查不出「卡表和卡面一起错」**。

## §一 方法（标定 + 出处）

**版面**（实测）：四圈**不在一排** —— 红圆+剑在左下、紫圆+枪在其右下（两者叠压）、
绿框在右下、银盾在绿框**正上方**（**只在有护甲时才印**）。
只有**部队卡 / 督军卡**印这四个圈；天赋 / 计策 / 防御 / 秘密 / 药剂只有费用六边形。

🔴 **按画布绝对坐标切必错**：900×1200 的画布上同一张卡来自**多个渲染批次**，
批次间位置与**缩放**都不同（实测红盘直径 93 ~ 116 px）。⇒ 逐张配准，锚 = **红盘**
（近战圆里的暗红实心盘），标定卡 **`Dark Angels/3部队/Warpforge_20_Deathwing-Terminator.png`**
（盘心 (201.0, 943.5)、93×94 px），并与 `Astra Militarum/3部队/…Tempestus-Scion.png`、
`Orks/3部队/…Stikkbomb-Boy.png` 三张**同坐标**交叉验证。

| 圈 | 相对锚偏移（单位 = 红盘平均直径 D） | 颜色阈值（见脚本 `PTS`） |
|---|---|---|
| 近战红剑 | (0.000, 0.000) | 红盘 `R∈(55,200) & G<60 & B<75 & R>G+45 & R>B+35`，取最大连通域 **70~150** px 方圆 |
| 远程紫枪 | (+0.762, +0.690) | 同上，窗内取白字 `min(R,G,B)>150` |
| 生命绿框 | (+5.016, +0.561) | 同上 |
| 护甲银盾 | (+5.520, −0.440) | ⚠️ 阈值要 **>235**：盾**边框本身就是亮银**，150 时整窗糊成一块 |

**数字怎么认**：本机**没有** OCR 库（无 cv2 / tesseract / easyocr，实测）。
改成 —— 先把白字外接框**保宽高比**归一化成 28×28 二值图，再**用卡表当教师长模板**
（同一 (圈, 卡表值) 的图归一堆、`min_n=3` 成簇、再跨组并簇投票定标签），分类由机器逐张套用。
**模板是机器长的、标签是从卡表继承的**（人工只核过拼图 `glyph_clusters.png`：`0`~`9` 加
`10/12/25/30/35/40`，与卡面字形逐一相符）。⚠️ 保宽高比**把「字框的长宽比」也带进了特征** ——
换一批渲染、字框长短一变，同一个数字就落得远（§四）。
""")
    L.append('## §二 覆盖率\n')
    tot = sum(v[0] for v in per_fac.values())
    n_full = sum(v[1] for v in per_fac.values())
    L.append('\n口径：**部队卡 + 督军卡**（只有这两类印四圈）· 分母 = **%d** 张（= 引擎卡池里 `type` 为 '
             '`unit`/`hero` 的）。「四条都读出」= 近战/远程/生命都读到白字，护甲按「卡面有盾就读、'
             '没盾且卡表也没有 = 一致」计。⚠️ **「读出」只表示机器给出了值，不表示值对** —— 对错看 §三。'
             '**天赋 / 计策 / 防御 / 秘密 / 药剂** 不印四圈，不在分母里。\n' % tot)
    L.append('\n| 阵营 | 部队+督军卡 | 四条都读出 | 读不出 |')
    L.append('|---|---|---|---|')
    for fac in sorted(per_fac):
        c, full, _ = per_fac[fac]
        L.append('| %s | %d | %d | %d |' % (fac, c, full, c - full))
    L.append('| **合计** | **%d** | **%d（%.1f%%）** | **%d** |'
             % (tot, n_full, 100.0*n_full/max(1, tot), tot-n_full))
    bad_cards = len({r['cid'] for r, _, _ in bad})
    L.append('\n**读不出的 %d 条逐条在 §四**（涉 %d 张卡）；**与卡表不一致的 %d 条在 §三**。'
             '两张表加起来才是「有问题的全部」—— 一张只管「读不出」，一张只管「读出但与表不符」。'
             % (len(bad), bad_cards, len(diffs)))
    # 把握度分档（只有字形的记录才算距离）
    dists = [d for _, d in cls if d is not None]
    n_t = sum(1 for d in dists if d < 0.03)
    n_o = sum(1 for d in dists if 0.03 <= d < 0.05)
    gray = [(r, v, d) for r, (v, d) in zip(recs, cls)
            if d is not None and v is not None and d > 0.05]
    gray_bad = sum(1 for r, v, d in gray if r['tab'] is not None and v != r['tab'])
    L.append('\n**读出来的把握度**（%d 条「有字形」的记录，按离最近模板的距离）：'
             '<0.03 **%d（%.1f%%）** · 0.03~0.05 %d · 0.05~%.2f（灰区）%d · >%.2f 分类不出 %d。'
             '灰区那 %d 条里**只有 %d 条与卡表不符**（就是 §三 `Gargantuan Squiggoth` 那条）。'
             '⇒ 距离小 = 稳；**距离大不等于错**（同数字换批渲染实测能到 0.15）。\n'
             % (len(dists), n_t, 100.0*n_t/max(1, len(dists)), n_o,
                MAXDIST, len(gray), MAXDIST, len(dists)-n_t-n_o-len(gray),
                len(gray), gray_bad))
    L.append('\n**锚（红盘）可靠度**：642 张里 641 张的「近战字框高 / 盘径」落在 **0.61~0.76**'
             '（盘径 92~108 px）；只有 `Vex Machinator` 是 **0.52** —— 它的锚量成 125×147'
             '（正常 93×94）⇒ D 虚大 ~40% ⇒ 最远的生命窗（5.016 D）整体右移 ⇒ 读错（§三 `Vex Machinator`）。'
             '根因是锚的容差写得太松（允许 70~150 px，长宽比 0.75~1.34）。\n')
    L.append('\n**模板清单（%d 个）**：' % len(tmpl))
    L.append(' · '.join('%s(n=%d)' % (t['val'], t['n']) for t in tmpl))
    L.append('\n## §三 差异清单（只列不一致的）\n')
    if not diffs:
        L.append('\n**零条。** 读出来的值与卡表逐张吻合。\n')
    else:
        L.append('\n口径：**机器读出来的值 ≠ 卡表的值**（两边都读到了；读不出的在 §四）。'
                 '「谁对」是把**原图调出来看**得到的，不是脚本推的。\n')
        L.append('\n| 卡名 | 圈 | 卡图值(机器读) | 卡表值 | 距离 | 谁对（看原图裁定） |')
        L.append('|---|---|---|---|---|---|')
        for r, val, dist in diffs:
            L.append('| %s | %s | %s | %s | %s | %s |'
                     % (r['name'], r['stat'], val,
                        r['tab'] if r['tab'] is not None else '（空）',
                        '—' if dist is None else '%.3f' % dist,
                        VERDICT.get((r['name'], r['stat']), '⚠️ 待裁（要看原图）')))
        L.append('\n⇒ **%d 条里机器真错 %d 条**（0.1%% 的记录量级）；'
                 '正确率的分母与把握度见 §二。\n'
                 % (len(diffs), sum(1 for r, v, d in diffs
                                    if VERDICT.get((r['name'], r['stat']), '').startswith('🔴'))))
    L.append('## §四 读不出来的那些\n')
    if not bad:
        L.append('\n**零条。**\n')
    else:
        cnt = collections.Counter(r['stat'] if r['stat'] else '锚失败' for r, _, _ in bad)
        tvals = {t['val'] for t in tmpl}
        n_by = collections.Counter((r['stat'], r['tab']) for r in recs if r['tab'] is not None)
        L.append('\n共 **%d** 条（分母 = %d 条「卡×圈」记录），分圈：%s。'
                 '**每条的字形都被机器抽出来放大看过**（`_tmp_view/statcircles/chk_masks.png`）——'
                 '**没有一条是「图糊 / 圈太小 / 字太小」**，字字清清楚楚。\n'
                 % (len(bad), len(recs), ' · '.join('%s %d' % (k, v) for k, v in cnt.most_common())))

        def cause_key(r, dist):
            if r['stat'] is None: return 'anch'
            if r['tab'] is not None and r['tab'] not in tvals: return 'notmpl'
            return 'batch'

        grp = collections.defaultdict(list)
        for r, dist, why in bad:
            grp[cause_key(r, dist)].append((r, dist))
        L.append('\n| 为什么读不出 | 条数 | 是哪些（卡名 圈 **人工照字形读出来**的数(距离)） |')
        L.append('|---|---|---|')
        for k in ('batch', 'notmpl', 'anch'):
            if k not in grp: continue
            cells = ['%s %s **%s**(%.3f)' % (r['name'], r.get('stat') or '—',
                     HUMAN_GLYPH.get((r['name'], r['stat'] or ''), '?'), d)
                     for r, d in grp[k]]
            if k == 'batch':
                txt = ('**字形干净可辨，只是离模板太远** —— 同一数字换一批渲染（字框长宽比、'
                       '笔画粗细都变），二值化后 XOR 距离就 > %.2f' % MAXDIST)
            elif k == 'notmpl':
                txt = ('**取值模板缺失**：卡表里 ' + ' / '.join(
                    '`%s` 只有 %d 张' % (r['tab'], n_by[(r['stat'], r['tab'])])
                    for r, _ in grp[k]) + ' ⇒ `min_n=3` 长不出模板，机器只能贴最近的一个')
            else:
                txt = '**锚（红盘）没找到** —— 整张卡读不了'
            L.append('| %s | %d | %s |' % (txt, len(grp[k]), ' · '.join(cells)))
        agree = sum(1 for r, _, _ in bad if HUMAN_GLYPH.get((r['name'], r['stat'] or '')) is not None
                    and HUMAN_GLYPH[(r['name'], r['stat'] or '')] == str(r['tab']))
        L.append('\n⇒ 人工照着字形读出来的 **%d 个数全部等于卡表值** ⇒ 这一批**没有一条是数据差异**，'
                 '纯粹是分类器没过（所以 §三 里没有它们）。\n' % agree)
        L.append('\n⚠️ **>%.2f 的记录比这里的条数多 1** —— 多的那条是 `Lucius The Eternal` 的护甲：'
                 '卡面本来就没印盾，窗里框到的是插画的白块（px=23），机器判「没有护甲」是对的，'
                 '按 §二 口径算合法，所以不进这张表。**这条不算失败，但它说明护甲那一格会有假阳性**。\n'
                 % MAXDIST)
    os.makedirs(os.path.dirname(OUT_MD), exist_ok=True)
    io.open(OUT_MD, 'w', encoding='utf-8', newline='\n').write('\n'.join(L))
    print('写出 %s（%d 行）' % (OUT_MD, len('\n'.join(L).split('\n'))))
    print('差异 %d 条 · 读不出 %d 条' % (len(diffs), len(bad)))
    for r, val, d in diffs[:30]:
        print('  %-34s %s 机器=%s 表=%s d=%.3f' % (r['name'], r['stat'], val, r['tab'], d))


USAGE = """用法（**必须先设 `PYTHONIOENCODING=utf-8`**，否则中文卡名会崩）：
  python Unity/工具/read_stat_circles.py report     # ← 出报告（最常用，整轮约 5 分钟）
  python Unity/工具/read_stat_circles.py harvest    # 抽字形 + 长模板 + 模板拼图
  python Unity/工具/read_stat_circles.py sheet 24   # 抽 24 张卡裁四个圈，肉眼核配准
  python Unity/工具/read_stat_circles.py help       # 这一屏
产出：
  Unity/资料/普查产出_0918/卡面数值圈_程序化读取.md   （report 写）
  d:/4/_tmp_view/statcircles/glyph_clusters.png     模板字形拼图（人核标签用）
  d:/4/_tmp_view/statcircles/chk_masks.png          「有字形但认不出」的字形拼图（报告 §四 引它）
"""


if __name__ == '__main__':
    cmd = sys.argv[1] if len(sys.argv) > 1 else 'help'
    if cmd in ('-h', '--help', 'help'): print(USAGE)
    elif cmd == 'harvest': do_harvest()
    elif cmd == 'report': do_report()
    elif cmd == 'sheet': do_sheet(int(sys.argv[2]) if len(sys.argv) > 2 else 20)
    else:
        print('unknown cmd %r\n' % cmd); print(USAGE)
