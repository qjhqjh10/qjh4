#!/usr/bin/env python
# W58-K12 step 5: full static read-scan for RawCardScript.cardShortId (+0x40).
# Tracks: this/param typed as a class with RawCardScript fields (inheritance aware),
# RawCardScript-typed params, raw-returning getters, list/array/dict iteration.
import io, os, re, collections

DUMP = r"d:/2/tools/il2cpp_out/dump.cs"
DEC  = r"d:/2/tools/decomp_full"
def read(p): return io.open(p, "r", encoding="utf-8", errors="replace").read().splitlines()

lines = read(DUMP)
cls_re = re.compile(r'^(?:public |private |internal |protected )?(?:static |sealed |abstract )*(?:class|struct|interface|enum)\s+([A-Za-z0-9_\.<>`]+)\s*(?::\s*([A-Za-z0-9_\.<>`]+))?')
field_re = re.compile(r'^\s*(?:public|private|internal|protected)\s+(?:static\s+)?(?:readonly\s+)?RawCardScript\s+([A-Za-z0-9_<>]+)\s*;\s*//\s*0x([0-9A-Fa-f]+)')

class_base={}; class_fields=collections.defaultdict(dict); methods=[]
cur=None
for i,ln in enumerate(lines,1):
    m=cls_re.match(ln)
    if m:
        cur=m.group(1); class_base[cur]=m.group(2) or ""; continue
    if not cur: continue
    fm=field_re.search(ln)
    if fm:
        class_fields[cur][int(fm.group(2),16)]=fm.group(1); continue
    s=ln.strip()
    if s.endswith("{ }") and "(" in s and not s.startswith("//"):
        nm=re.search(r'([A-Za-z0-9_<>\.`]+)\s*\(', s)
        if nm: methods.append((cur,nm.group(1),s,i))

def base_chain(c):
    seen=set()
    while c and c not in seen:
        seen.add(c); c=class_base.get(c,"")
    return seen

def raw_fields_of(c):
    out={}
    for k in base_chain(c): out.update(class_fields.get(k,{}))
    return out

def class_of_type(t):
    t=t.strip()
    t=re.sub(r'^(?:out|in|ref|params)\s+','',t)
    t=re.sub(r'<.*','',t)            # generic wrapper
    t=t.rstrip('[]?')
    return t.split('.')[-1]

def params_of(sig):
    m=re.search(r'\(([^()]*(?:\([^()]*\)[^()]*)*)\)', sig)
    if not m: return []
    raw=m.group(1)
    # split top-level commas
    out=[]; depth=0; cur=''
    for ch in raw:
        if ch=='<': depth+=1
        elif ch=='>': depth-=1
        if ch==',' and depth==0:
            out.append(cur); cur=''
        else: cur+=ch
    if cur.strip(): out.append(cur)
    res=[]
    for p in out:
        p=p.strip()
        if not p: continue
        p=re.sub(r'\s*=\s*.*$','',p)   # default
        toks=p.split()
        res.append(' '.join(toks[:-1]) if len(toks)>1 else p)  # drop param name
    return res

sig_methods=[]; ret_raw=set()
for c,n,s,i in methods:
    nm=re.search(r'([A-Za-z0-9_<>\.`]+)\s*\(', s)
    head=s[:nm.start()] if nm else ''
    if "RawCardScript" in head and nm: ret_raw.add(nm.group(1))
    if "RawCardScript" in s or class_of_type(c)=="RawCardScript":
        sig_methods.append((c,n,s,i))

print("methods:",len(methods),"scanned methods:",len(sig_methods),"raw-returning:",len(ret_raw))
files=set(os.listdir(DEC))
def mangle(x): return x.replace("<","_").replace(">","_").replace("+",".")
def cands(cls,name):
    c=mangle(cls); n=mangle(name)
    return [x for x in (c+"__"+n+".c", c.replace(".","__")+"__"+n+".c") if x in files]

pat40=re.compile(r'\+\s*0x40\b')
ld=re.compile(r'^\s*(\w+)\s*=\s*(.+)$')
hits=[]; mapped=0
for cls,name,sig,ln in sig_methods:
    cf=cands(cls,name)
    if cf: mapped+=1
    fields=raw_fields_of(cls)
    ptypes=params_of(sig)
    is_inst = not re.search(r'\bstatic\b', sig)
    coll_raw = bool(re.search(r'(?:List|IList|IEnumerable|Dictionary|HashSet)<\s*RawCardScript|RawCardScript\s*\[', sig))
    for fn in cf:
        body=read(os.path.join(DEC,fn))
        rawloc=set()
        if class_of_type(cls)=="RawCardScript": rawloc.add("param_1")
        for k,t in enumerate(ptypes):
            if class_of_type(t)=="RawCardScript":
                rawloc.add("param_%d"%(k+2 if is_inst else k+1))
        for j,bl in enumerate(body,1):
            # locals loaded from RawCardScript fields
            for pidx in range(1,10):
                pn="param_%d"%pidx
                if pn not in bl: continue
                for off in fields:
                    for form in ("%s + 0x%x"%(pn,off),"%s + 0x%X"%(pn,off),"%s + %d"%(pn,off)):
                        if form in bl:
                            lm=ld.match(bl)
                            if lm: rawloc.add(lm.group(1))
                            break
            # locals from raw-returning getter
            for g in ret_raw:
                if g+"(" in bl:
                    lm=ld.match(bl)
                    if lm: rawloc.add(lm.group(1))
            # collection element iteration when method has a RawCardScript collection
            if coll_raw and re.search(r'(get_Current|get_Item|GetEnumerator|__Current)\b', bl):
                lm=ld.match(bl)
                if lm: rawloc.add(lm.group(1))
            if pat40.search(bl):
                m2=re.search(r'\*\s*\(\s*(?:int|uint|longlong|undefined8|undefined4|char|undefined1)\s*\*\s*\)\s*\(\s*(\w+)\s*\+\s*0x40', bl)
                if m2 and m2.group(1) in rawloc:
                    hits.append((fn,j,bl.strip(),ln,sig))
for h in hits:
    print("HIT %s:%d  %s\n    sig(dump.cs:%d): %s"%h)
print("mapped:",mapped,"/",len(sig_methods),"TOTAL reads:",len(hits))
