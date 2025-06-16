# tests/test_integration.py

import pytest
import requests
import time

API_URL = "http://localhost"

@pytest.fixture(scope="module")
def user_id():
    return "22222222-2222-2222-2222-222222222222"

def test_create_account_and_initial_balance(user_id):
    # 1) Создать счёт
    r = requests.post(f"{API_URL}/payments/create", params={"userId": user_id})
    assert r.status_code == 201

    # 2) Баланс должен быть 0
    r = requests.get(f"{API_URL}/payments/balance", params={"userId": user_id})
    assert r.status_code == 200
    assert r.json()["balance"] == 0

def test_duplicate_account_fails(user_id):
    # Повторная попытка создать тот же счёт
    r = requests.post(f"{API_URL}/payments/create", params={"userId": user_id})
    assert r.status_code == 400

def test_topup_and_balance(user_id):
    # Пополняем на 100
    r = requests.post(f"{API_URL}/payments/topup", params={"userId": user_id, "amount": 100})
    assert r.status_code == 200
    assert r.json()["balance"] == 100

    # Проверяем баланс через endpoint
    r = requests.get(f"{API_URL}/payments/balance", params={"userId": user_id})
    assert r.status_code == 200
    assert r.json()["balance"] == 100

def create_and_wait_order(user_id, amount, expected_status, description):
    # Создаём заказ
    r = requests.post(f"{API_URL}/orders", params={
        "userId": user_id,
        "amount": amount,
        "description": description
    })
    assert r.status_code == 202
    order = r.json()
    order_id = order["id"]

    # Ожидаем, что статус перейдёт в expected_status
    for _ in range(10):
        r = requests.get(f"{API_URL}/orders/{order_id}")
        assert r.status_code == 200
        status = r.json()["status"]
        if status == expected_status:
            return order_id
        time.sleep(1)
    pytest.fail(f"Order {order_id} status is {status}, expected {expected_status}")

def test_order_auto_payment_success(user_id):
    # Баланс у нас 100, создаём заказ на 50 → FINISHED
    oid = create_and_wait_order(user_id, 50, "FINISHED", "cheap order")
    # Заказ появился в списке
    r = requests.get(f"{API_URL}/orders", params={"userId": user_id})
    assert r.status_code == 200
    orders = r.json()
    assert any(o["id"] == oid for o in orders)

def test_order_auto_payment_fail(user_id):
    # Остаток на счёте теперь 50, создаём заказ на 100 → CANCELLED
    oid = create_and_wait_order(user_id, 100, "CANCELLED", "expensive order")
    # Заказ появился в списке
    r = requests.get(f"{API_URL}/orders", params={"userId": user_id})
    assert r.status_code == 200
    orders = r.json()
    assert any(o["id"] == oid for o in orders)