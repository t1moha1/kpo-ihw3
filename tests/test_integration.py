import pytest
import requests
import time

API_URL = "http://localhost"

@pytest.fixture(scope="module")
def user_id():
    return "3fa85f64-5717-4567-b3fc-2c963f66afa6"


def test_create_account_and_initial_balance(user_id):
    r = requests.post(f"{API_URL}/payments/create", params={"userId": user_id})
    assert r.status_code == 201

    r = requests.get(f"{API_URL}/payments/balance", params={"userId": user_id})
    assert r.status_code == 200
    assert r.json()["balance"] == 0


def test_duplicate_account_fails(user_id):
    r = requests.post(f"{API_URL}/payments/create", params={"userId": user_id})
    assert r.status_code == 400


def test_topup_and_balance(user_id):
    r = requests.post(
        f"{API_URL}/payments/topup", params={"userId": user_id, "amount": 100}
    )
    assert r.status_code == 200
    assert r.json()["balance"] == 100

    r = requests.get(f"{API_URL}/payments/balance", params={"userId": user_id})
    assert r.status_code == 200
    assert r.json()["balance"] == 100


def create_and_wait_order(user_id, amount, expected_status, description):
    r = requests.post(
        f"{API_URL}/orders", params={
            "userId": user_id,
            "amount": amount,
            "description": description
        }
    )
    assert r.status_code == 202
    order = r.json()
    order_id = order["id"]

    for _ in range(10):
        r = requests.get(f"{API_URL}/orders/{order_id}")
        assert r.status_code == 200
        status = r.json()["status"]
        if status == expected_status:
            return order_id
        time.sleep(1)
    pytest.fail(f"Order {order_id} status is {status}, expected {expected_status}")


def test_order_auto_payment_success(user_id):
    oid = create_and_wait_order(user_id, 50, "FINISHED", "cheap order")
    r = requests.get(f"{API_URL}/orders", params={"userId": user_id})
    assert r.status_code == 200
    orders = r.json()
    assert any(o["id"] == oid for o in orders)


def test_order_auto_payment_fail(user_id):
    oid = create_and_wait_order(user_id, 100, "CANCELLED", "expensive order")
    r = requests.get(f"{API_URL}/orders", params={"userId": user_id})
    assert r.status_code == 200
    orders = r.json()
    assert any(o["id"] == oid for o in orders)


def test_balance_before_account_is_not_found():
    new_user = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
    r = requests.get(f"{API_URL}/payments/balance", params={"userId": new_user})
    assert r.status_code == 404


def test_topup_before_account_not_found():
    new_user = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"
    r = requests.post(
        f"{API_URL}/payments/topup", params={"userId": new_user, "amount": 10}
    )
    assert r.status_code == 404


def test_order_without_any_account_results_in_cancelled():
    fresh_user = "cccccccc-cccc-cccc-cccc-cccccccccccc"
    oid = create_and_wait_order(fresh_user, 20, "CANCELLED", "no account order")
    r = requests.get(f"{API_URL}/orders", params={"userId": fresh_user})
    assert r.status_code == 200
    orders = r.json()
    assert any(o["id"] == oid for o in orders)


def test_get_nonexistent_order_returns_not_found(user_id):
    fake_order = "dddddddd-dddd-dddd-dddd-dddddddddddd"
    r = requests.get(f"{API_URL}/orders/{fake_order}")
    assert r.status_code == 404


def test_list_orders_empty_initially(user_id):
    brand_new = "eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"
    r = requests.get(f"{API_URL}/orders", params={"userId": brand_new})
    assert r.status_code == 200
    assert r.json() == []


def test_multiple_orders_and_balances(user_id):
    r = requests.post(
        f"{API_URL}/payments/topup", params={"userId": user_id, "amount": 200}
    )
    assert r.status_code == 200
    assert r.json()["balance"] == 250

    oid1 = create_and_wait_order(user_id, 100, "FINISHED", "first bulk order")
    oid2 = create_and_wait_order(user_id, 100, "FINISHED", "second bulk order")

    r = requests.get(f"{API_URL}/payments/balance", params={"userId": user_id})
    assert r.status_code == 200
    assert r.json()["balance"] == 50

    r = requests.get(f"{API_URL}/orders", params={"userId": user_id})
    assert r.status_code == 200
    orders = r.json()
    ids = [o["id"] for o in orders]
    assert oid1 in ids and oid2 in ids
