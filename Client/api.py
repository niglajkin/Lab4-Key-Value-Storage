import requests, json, os

PORT = 5034                      
BASE  = f"http://localhost:{PORT}/kv"
BULK  = f"{BASE}/bulk"
DUMP  = f"{BASE}/dump"
LOAD  = f"{BASE}/load"

def _req(method, url, **k):
    r = requests.request(method, url, **k)
    r.raise_for_status(); return r

def set_one(k,v):       _req("post", BASE, json={"key":k,"value":v})
def change_one(k,v):    _req("put" , BASE, json={"key":k,"value":v})
def get_one(k):         r = requests.get(f"{BASE}/{k}"); return r.text if r.ok else None
def del_one(k):         return requests.delete(f"{BASE}/{k}").ok

def set_many(d: dict):
    return _req("post", BULK, json=d)     
def change_many(d: dict):
    return _req("put", BULK, json=d)
def del_many(keys: list):
    return _req("delete", BULK, json=keys)



def get_all():          return requests.get(BASE).json()
def del_all():          return requests.delete(BASE).status_code

def dump_to(path):      _req("post", DUMP, json={"path":path})
def load_from(path):    _req("post", LOAD, json={"path":path})

