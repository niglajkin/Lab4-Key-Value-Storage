import responses, pytest
import sys, pathlib

CLIENT_DIR = pathlib.Path(__file__).resolve().parents[1]
if str(CLIENT_DIR) not in sys.path:
    sys.path.insert(0, str(CLIENT_DIR))

import api                                   

BASE = api.BASE
BULK = api.BULK
DUMP = api.DUMP
LOAD = api.LOAD



@responses.activate
def test_set_and_duplicate_conflict():
    responses.post(BASE, status=200)
    api.set_one("a", "1")

    responses.post(BASE, status=409)
    with raises_status(409):
        api.set_one("a", "x")


@responses.activate
def test_change_get_delete_happy_path():
    responses.put(BASE, status=200)
    api.change_one("k", "v")

    responses.get(f"{BASE}/k", status=200, body="v")
    assert api.get_one("k") == "v"

    responses.delete(f"{BASE}/k", status=200)
    assert api.del_one("k") is True


@responses.activate
def test_get_and_delete_not_found():
    responses.get(f"{BASE}/none", status=404)
    assert api.get_one("none") is None

    responses.delete(f"{BASE}/none", status=404)
    assert api.del_one("none") is False


@responses.activate
def test_bulk_set_and_conflict():
    responses.post(BULK, status=200)
    api.set_many({"x": "1", "y": "2"})

    responses.post(BULK, status=409)
    with raises_status(409):
        api.set_many({"x": "dup"})


@responses.activate
def test_bulk_change_and_bulk_delete():
    responses.put(BULK, status=200)
    api.change_many({"a": "9"})

    responses.delete(BULK, status=200)
    api.del_many(["a", "b"])


@responses.activate
def test_dump_and_load_roundtrip(tmp_path):
    path = tmp_path / "d.json"

    responses.post(DUMP, status=200)
    api.dump_to(str(path))

    responses.post(LOAD, status=200)
    api.load_from(str(path))


from contextlib import contextmanager

@contextmanager
def raises_status(code):
    with pytest.raises(Exception) as e:
        yield
    assert e.value.response.status_code == code