# -*- coding: utf-8 -*-
"""救回 `OvertimeStart`（AudioClip，48000 Hz 立体声，FSB5/Vorbis 裸流）。

原理（三步，每步都可复现）：
  1. 数据在**原始 bundle** 的 `.resource` 流里：`soundcollection_assets_all.bundle`
     → 内部节点 `CAB-62d1945b7005c115fb946f0d264a205b.resource`，是一串 618 个 FSB5 bank，
     AudioClip 的 `m_Resource.m_Offset/m_Size` 指向其中一个完整 bank。
  2. FSB5 里**没有 vorbis 头**（整个 36 MB 资源里 `vorbis` 字面量 0 次，`m_AudioData` 也是空）。
     但 FMOD 用的是 libvorbis 1.3.7（vendor `Xiph.Org libVorbis I 20200704`），
     其 setup 头**只由 (声道数, 采样率, 质量) 决定** —— 质量经实测：单声道 q=0.1、立体声 q=0.33
     （用 libsndfile 1.2.2 内嵌的同一版 libvorbis 反向复现，逐字节命中游戏自带的 setup）。
  3. 按 (2ch, 48000, q=0.33) 生成 setup 头，配 48000 的 identification 头 +
     从 FSB5 数据区解出的 326 个 u16 前缀包 → 组成完整 ogg。
块结构自检：包首字节 bit0 = 包类型（必须 0），bit1 = 模式（1 bit，0=短块 256 / 1=长块 2048）；
按 libvorbis 的 granulepos 规则累加，必须**逐点命中 FSB5 自带 seek 表**的 PCM 偏移（误差 0）。
"""
import os, sys, io, ctypes, struct, hashlib
sys.path.insert(0, r"d:/2/Warpforge_tools/scripts")
sys.path.insert(0, r"d:/4/_tmp_view")
import UnityPy, fsb_audio as FA, ogg_build as OB, av
from _vorbis_q_sweep import (lib, SF_INFO, SFM_WRITE, SF_FORMAT_OGG, SF_FORMAT_VORBIS,
                             SFC_SET_VBR_ENCODING_QUALITY)

BUNDLE = r"D:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/soundcollection_assets_all.bundle"
RES_NODE = "CAB-62d1945b7005c115fb946f0d264a205b.resource"
CLIP, QUALITY = "OvertimeStart", 0.33
OUT = r"d:/4/_tmp_view"

def vorbis_headers(rate, ch, q):
    """用 libsndfile 内嵌的 libvorbis 1.3.7 生成 (id, comment, setup) 三个包。"""
    p = os.path.join(OUT, "_hdr_probe.ogg")
    info = SF_INFO(0, rate, ch, SF_FORMAT_OGG | SF_FORMAT_VORBIS, 0, 0)
    h = lib.sf_open(p.encode(), SFM_WRITE, ctypes.byref(info))
    assert h, "sf_open failed"
    qv = ctypes.c_double(q)
    lib.sf_command(h, SFC_SET_VBR_ENCODING_QUALITY, ctypes.byref(qv), ctypes.sizeof(qv))
    import numpy as np
    n = rate  # 1 秒即可（setup 与音频内容无关）
    t = np.arange(n) / rate
    inter = np.tile((0.3*np.sin(2*np.pi*220*t)).astype(np.float64), ch)
    lib.sf_writef_double(h, inter.ctypes.data_as(ctypes.POINTER(ctypes.c_double)), n)
    lib.sf_close(h)
    pk = OB.parse_ogg_packets(open(p, "rb").read())
    os.remove(p)
    return pk[0], pk[1], pk[2]

def load(bundle=BUNDLE, clip=CLIP):
    env = UnityPy.load(bundle)
    node = env.file.files[RES_NODE]; node.Position = 0; R = node.read_bytes(node.Length)
    tt = next(o.read_typetree() for o in env.objects
              if o.type.name == "AudioClip" and o.read_typetree().get("m_Name") == clip)
    r = tt["m_Resource"]; c = R[r["m_Offset"]:r["m_Offset"]+r["m_Size"]]
    assert c[:4] == b"FSB5", c[:4]
    lay = FA.get_sample_layout(c)
    # FSB5 seek 表（chunk type 11）
    raw = struct.unpack_from("<Q", c, 60)[0]; pos, nxt, seek = 68, raw & 1, None
    while nxt:
        w = struct.unpack_from("<I", c, pos)[0]; pos += 4; nxt = w & 1; size = (w >> 1) & 0xFFFFFF
        if seek is None:
            body = c[pos:pos+size]
            seek = [struct.unpack_from("<II", body, 8+8*i) for i in range((size-8)//8)]
        pos += size
    return tt, lay, seek, FA.extract_vorbis_packets(c[lay["data_offset"]:])

def build(rate, ch, bs_exp, pkts, n_samples, q=QUALITY):
    idp, comp, setup = vorbis_headers(rate, ch, q)
    info = OB.vorbis_id_info(idp)
    assert (info[0], info[1]) == (ch, rate) and (info[2], info[3]) == bs_exp, (info, rate, ch)
    BS = [1 << bs_exp[0], 1 << bs_exp[1]]
    gp, prev, gps = 0, 0, []
    for p in pkts:
        assert p[0] & 1 == 0, "vorbis packet type bit must be 0"
        cur = BS[(p[0] >> 1) & 1]
        if prev: gp += (cur + prev) // 4
        gps.append(gp); prev = cur
    serial, pageno, pages, i, cq, segs = 1, 0, [], 0, [], 0
    pages.append(OB.make_ogg_page([idp], serial, pageno, bos=True)); pageno += 1
    pages.append(OB.make_ogg_page([comp, setup], serial, pageno)); pageno += 1
    while i < len(pkts):
        need = len(pkts[i]) // 255 + 1
        if cq and (segs + need > 250 or sum(map(len, cq)) + len(pkts[i]) > 65000):
            pages.append(OB.make_ogg_page(cq, serial, pageno, granulepos=[gps[i-1]])); pageno += 1
            cq, segs = [], 0
        cq.append(pkts[i]); segs += need; i += 1
    pages.append(OB.make_ogg_page(cq, serial, pageno, granulepos=[n_samples], eos=True))
    return b"".join(pages), gp, setup, idp

if __name__ == "__main__":
    tt, lay, seek, pkts = load()
    bs_exp = OB.vorbis_id_info(vorbis_headers(lay["freq"], lay["channels"], QUALITY)[0])[2:]
    ogg, gp, setup, idp = build(lay["freq"], lay["channels"], bs_exp, pkts, lay["n_samples"])
    # --- 自检 1：块结构必须逐点命中 FSB5 自带的 seek 表
    BS = [1 << bs_exp[0], 1 << bs_exp[1]]; g, prev, gps = 0, 0, []
    for p in pkts:
        cur = BS[(p[0] >> 1) & 1]
        if prev: g += (cur + prev) // 4
        gps.append(g); prev = cur
    cum = [0]
    for p in pkts: cum.append(cum[-1] + len(p) + 2)
    idx = [min(range(len(cum)), key=lambda j: abs(cum[j] - bo)) for po, bo in seek]
    err = sum(abs(gps[i] - po) for (po, bo), i in zip(seek, idx))
    assert err == 0, f"seek-table mismatch err={err}"
    print(f"自检1 OK 块结构命中 FSB5 seek 表（{len(seek)} 个点，err=0）；granulepos 累计 {gp}，末端裁剪到 {lay['n_samples']}")
    # --- 自检 2：解码长度必须 = m_Length × rate
    c = av.open(io.BytesIO(ogg)); st = c.streams[0]
    n = sum(f.samples for f in c.decode(st)); c.close()
    dur = n / st.rate
    assert n == lay["n_samples"], (n, lay["n_samples"])
    print(f"自检2 OK 解出 {n} 帧 @ {st.rate} Hz {st.channels}ch = {dur:.4f}s，"
          f"m_Length = {tt['m_Length']:.4f}s（差 {abs(dur-tt['m_Length'])*1000:.2f} ms）")
    p = os.path.join(OUT, "OvertimeStart.ogg"); open(p, "wb").write(ogg)
    print(f"产物：{p}  {len(ogg)} B  md5={hashlib.md5(ogg).hexdigest()}")
    print(f"      setup 头 {len(setup)} B md5={hashlib.md5(setup).hexdigest()[:12]}（libvorbis 1.3.7 @ 2ch/48000/q={QUALITY}）")
