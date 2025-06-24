import re, json

def pretty_print_kvs(kvs: dict):
    if not kvs:
        print("Storage is empty"); return
    w = max(len(k) for k in kvs)+2
    for k,v in kvs.items(): print(f"{k:<{w}}: {v}")

def parse_pair_block(block: str) -> dict[str,str]:
    inner = block.strip().lstrip("{[").rstrip("]}").strip()
    if not inner: return {}
    out={}
    for tok in re.split(r"[,\s]+", inner):
        if ":" in tok:
            k,v=tok.split(":",1); out[k]=v
    return out

def parse_key_block(block: str) -> list[str]:
    inner = block.strip().lstrip("{[").rstrip("]}").strip()
    return re.split(r"[,\s]+", inner) if inner else []
