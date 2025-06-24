import shlex, sys, requests
from api import (
    set_one, change_one, get_one, del_one,
    set_many, change_many, del_many,
    get_all, del_all, dump_to, load_from
)
from helpers import pretty_print_kvs, parse_pair_block, parse_key_block

BANNER = """
SET key value
CHANGE key value
SETMULT {a:1 b:2}
CHANGEMULT {a:10 b:20}
GET key
GETALL
DELETE key
DELMULT {a b c}
DELETEALL
DUMP path.json
LOAD path.json
EXIT
"""
print(BANNER.strip())

while True:
    try:
        parts = shlex.split(input("> "), posix=False)
    except EOFError:
        parts = ["EXIT"]

    if not parts:
        continue

    cmd, *args = parts
    cmd = cmd.upper()

    try:
        if cmd == "SET" and len(args) >= 2:
            k, v = args[0], " ".join(args[1:])
            if get_one(k) is not None:
                print("Pair with such key already present, use CHANGE.")
            else:
                set_one(k, v); print("OK")

        elif cmd == "CHANGE" and len(args) >= 2:
            k, v = args[0], " ".join(args[1:])
            if get_one(k) is None:
                print("Key not found.")
            else:
                change_one(k, v); print("UPDATED")

        elif cmd == "SETMULT":
            d = parse_pair_block(" ".join(args))
            if d:
                res = set_many(d).json()
                msg = f"Inserted {res['added']}."
                if res["skipped"]:
                    msg += " Skipped: " + ", ".join(res["skipped"])
                print(msg)
            else:
                print("No pairs parsed.")

        elif cmd == "CHANGEMULT":
            d = parse_pair_block(" ".join(args))
            if d:
                res = change_many(d).json()
                msg = f"Updated {res['updated']}."
                if res["absent"]:
                    msg += " Absent: " + ", ".join(res["absent"])
                print(msg)
            else:
                print("No pairs parsed.")

        elif cmd == "GET" and len(args) == 1:
            v = get_one(args[0]); print(v if v else "NOT FOUND")

        elif cmd == "GETALL":
            pretty_print_kvs(get_all())

        elif cmd == "DELETE" and len(args) == 1:
            print("DELETED" if del_one(args[0]) else "NOT FOUND")

        elif cmd == "DELMULT":
            keys = parse_key_block(" ".join(args))
            if keys:
                res = del_many(keys).json()
                msg = f"Removed {res['removed']}."
                if res["absent"]:
                    msg += " Absent: " + ", ".join(res["absent"])
                print(msg)
            else:
                print("No keys parsed.")

        elif cmd == "DELETEALL":
            print("CLEARED" if del_all()==200 else "Storage is already empty")

        elif cmd == "DUMP" and len(args)==1:
            dump_to(args[0]); print("DUMPED")

        elif cmd == "LOAD" and len(args)==1:
            if get_all():
                if input("Current pairs will be lost. Dump first? [Y/N]: ").strip().upper()=="Y":
                    p=input("Path to dump: ").strip()
                    dump_to(p); print("Dumped.")
            load_from(args[0]); print("LOADED")

        elif cmd == "EXIT":
            if get_all():
                if input("Dump before exit? [Y/N]: ").strip().upper()=="Y":
                    p=input("Path: ").strip()
                    dump_to(p); print("Dumped.")
            print("Bye!"); break

        else:
            print("Unknown or malformed command.")

    except requests.HTTPError as e:
        print("Server:", e.response.text.strip())
    except Exception as e:
        print("Error:", e, file=sys.stderr)

