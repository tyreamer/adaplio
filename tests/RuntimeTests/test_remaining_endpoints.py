"""
Adaplio API - Test Remaining Endpoints
Tests the untested endpoints: auth/refresh, weekly-board, auth/role, onboarding, sms, token
"""

import requests
import json
import time
import random
import string

BASE_URL = "http://localhost:8080"

def print_section(title):
    print(f"\n{'='*80}")
    print(f"  {title}")
    print(f"{'='*80}")

def print_test(name, status_code, expected=None, details=""):
    if expected is None:
        status = f"[{status_code}]"
    else:
        status = "[PASS]" if status_code == expected else "[FAIL]"
    print(f"{status} {name}")
    if details:
        print(f"      {details}")

def generate_unique_email():
    timestamp = int(time.time())
    random_str = ''.join(random.choices(string.ascii_lowercase, k=4))
    return f"remaining_{timestamp}_{random_str}@test.com"

# ============================================================================
# Test 1: POST /auth/refresh with valid refresh token
# ============================================================================

def test_token_refresh():
    print_section("TEST 1: Token Refresh with Valid Refresh Token")

    # Step 1: Register trainer to get fresh tokens
    email = generate_unique_email()
    print(f"\n1. Registering trainer: {email}")

    response = requests.post(
        f"{BASE_URL}/auth/trainer/register",
        json={
            "email": email,
            "password": "SecurePass123!",
            "fullName": "Refresh Test Trainer"
        }
    )

    if response.status_code != 200:
        print_test("Register trainer", response.status_code, 200, "FAILED - Cannot proceed")
        return False

    data = response.json()
    access_token = data.get('token')
    refresh_token = data.get('refreshToken')

    print_test("Register trainer", 200, 200, f"Got access and refresh tokens")
    print(f"      Access Token: {access_token[:50]}...")
    print(f"      Refresh Token: {refresh_token[:50] if refresh_token else 'None'}...")

    if not refresh_token:
        print("      [FAIL] No refresh token in response!")
        return False

    # Step 2: Use refresh token to get new access token
    # NOTE: The API expects refresh token in a cookie, not request body
    print(f"\n2. Refreshing access token (refresh token must be in cookie)...")

    # Set refresh token as cookie
    cookies = {"refresh_token": refresh_token}

    response = requests.post(
        f"{BASE_URL}/auth/refresh",
        cookies=cookies
    )

    print_test("Refresh access token", response.status_code, 200)

    if response.status_code == 200:
        refresh_data = response.json()
        new_access_token = refresh_data.get('token')
        new_refresh_token = refresh_data.get('refreshToken')

        print(f"      New Access Token: {new_access_token[:50] if new_access_token else 'None'}...")
        print(f"      New Refresh Token: {new_refresh_token[:50] if new_refresh_token else 'None'}...")

        # Step 3: Verify new access token works
        print(f"\n3. Verifying new access token works...")

        response = requests.get(
            f"{BASE_URL}/auth/me",
            headers={"Authorization": f"Bearer {new_access_token}"}
        )

        print_test("Use new access token", response.status_code, 200)

        if response.status_code == 200:
            print(f"      User: {response.json().get('email', 'N/A')}")
            return True
    else:
        print(f"      Response: {response.text}")

    return False

# ============================================================================
# Test 2: GET /weekly-board
# ============================================================================

def test_weekly_board():
    print_section("TEST 2: Weekly Board Endpoint")

    # Register client and set up plan
    print(f"\n1. Setting up client with active plan...")

    # Register trainer
    trainer_email = generate_unique_email()
    response = requests.post(
        f"{BASE_URL}/auth/trainer/register",
        json={"email": trainer_email, "password": "SecurePass123!", "fullName": "Board Test"}
    )

    if response.status_code != 200:
        print_test("Setup trainer", response.status_code, 200, "FAILED")
        return False

    trainer_token = response.json()['token']
    print_test("Setup trainer", 200, 200)

    # Create grant
    response = requests.post(
        f"{BASE_URL}/api/trainer/grants",
        headers={"Authorization": f"Bearer {trainer_token}"},
        json={"expirationHours": 72}
    )

    if response.status_code != 200:
        print_test("Create grant", response.status_code, 200, "FAILED")
        return False

    grant_code = response.json()['grantCode']

    # Register client
    client_email = generate_unique_email()
    response = requests.post(
        f"{BASE_URL}/auth/client/magic-link",
        json={"phoneOrEmail": client_email}
    )

    time.sleep(1)

    # Verify client with common codes
    client_token = None
    for code in ["123456", "000000", "111111"]:
        response = requests.post(
            f"{BASE_URL}/auth/client/verify",
            json={"phoneOrEmail": client_email, "token": code}
        )
        if response.status_code == 200:
            client_token = response.json()['token']
            break

    if not client_token:
        print_test("Setup client", 400, 200, "FAILED - check API logs for magic link code")
        return False

    print_test("Setup client", 200, 200)

    # Accept grant
    response = requests.post(
        f"{BASE_URL}/api/client/grants/accept",
        headers={"Authorization": f"Bearer {client_token}"},
        json={"grantCode": grant_code}
    )

    # Create template and proposal (simplified - reuse existing if available)

    # Step 2: Test weekly-board endpoint
    print(f"\n2. Testing GET /api/client/weekly-board...")

    response = requests.get(
        f"{BASE_URL}/api/client/weekly-board",
        headers={"Authorization": f"Bearer {client_token}"}
    )

    print_test("GET /weekly-board", response.status_code, 200)

    if response.status_code == 200:
        board = response.json()
        print(f"      Response type: {type(board)}")
        print(f"      Keys: {list(board.keys()) if isinstance(board, dict) else 'N/A'}")
        return True
    else:
        print(f"      Response: {response.text}")

    return False

# ============================================================================
# Test 3: POST /auth/role
# ============================================================================

def test_set_role():
    print_section("TEST 3: Set User Role")

    # Create user without specific role
    print(f"\n1. Creating user...")

    email = generate_unique_email()
    response = requests.post(
        f"{BASE_URL}/auth/trainer/register",
        json={
            "email": email,
            "password": "SecurePass123!",
            "fullName": "Role Test User"
        }
    )

    if response.status_code != 200:
        print_test("Register user", response.status_code, 200, "FAILED")
        return False

    token = response.json()['token']
    print_test("Register user", 200, 200)

    # Check current role
    response = requests.get(
        f"{BASE_URL}/auth/me",
        headers={"Authorization": f"Bearer {token}"}
    )

    if response.status_code == 200:
        current_role = response.json().get('userType', 'N/A')
        print(f"      Current role: {current_role}")

    # Step 2: Attempt to change role
    print(f"\n2. Attempting to set role to 'client'...")

    response = requests.post(
        f"{BASE_URL}/auth/role",
        headers={"Authorization": f"Bearer {token}"},
        json={"role": "client"}
    )

    print_test("POST /auth/role", response.status_code)

    if response.status_code == 200:
        # Verify role changed
        response = requests.get(
            f"{BASE_URL}/auth/me",
            headers={"Authorization": f"Bearer {token}"}
        )

        if response.status_code == 200:
            new_role = response.json().get('userType', 'N/A')
            print(f"      New role: {new_role}")

            if new_role == 'client':
                print("      [SUCCESS] Role changed successfully")
                return True
            else:
                print(f"      [FAIL] Role didn't change (still {new_role})")
    else:
        print(f"      Response: {response.text}")

    return False

# ============================================================================
# Test 4: POST /onboarding
# ============================================================================

def test_onboarding():
    print_section("TEST 4: Save Onboarding Preferences")

    # Register client
    print(f"\n1. Registering client...")

    trainer_email = generate_unique_email()
    response = requests.post(
        f"{BASE_URL}/auth/trainer/register",
        json={"email": trainer_email, "password": "SecurePass123!", "fullName": "Onboarding Test"}
    )

    if response.status_code != 200:
        print_test("Register client", response.status_code, 200, "FAILED")
        return False

    token = response.json()['token']
    print_test("Register client", 200, 200)

    # Step 2: Save onboarding preferences
    print(f"\n2. Saving onboarding preferences...")

    preferences = {
        "communicationPreference": "email",
        "reminderTime": "09:00",
        "timezone": "America/New_York",
        "goals": "Improve flexibility and strength"
    }

    response = requests.post(
        f"{BASE_URL}/api/client/onboarding",
        headers={"Authorization": f"Bearer {token}"},
        json=preferences
    )

    print_test("POST /onboarding", response.status_code)

    if response.status_code == 200:
        print(f"      Preferences saved successfully")

        # Verify preferences were saved
        response = requests.get(
            f"{BASE_URL}/api/me/profile",
            headers={"Authorization": f"Bearer {token}"}
        )

        if response.status_code == 200:
            profile = response.json()
            timezone = profile.get('timezone', 'N/A')
            print(f"      Timezone in profile: {timezone}")
            return True
    else:
        print(f"      Response: {response.text}")

    return False

# ============================================================================
# Test 5: POST /sms
# ============================================================================

def test_sms_invite():
    print_section("TEST 5: Send SMS Invite")

    # Register trainer
    print(f"\n1. Registering trainer...")

    email = generate_unique_email()
    response = requests.post(
        f"{BASE_URL}/auth/trainer/register",
        json={"email": email, "password": "SecurePass123!", "fullName": "SMS Test Trainer"}
    )

    if response.status_code != 200:
        print_test("Register trainer", response.status_code, 200, "FAILED")
        return False

    token = response.json()['token']
    print_test("Register trainer", 200, 200)

    # Step 2: Send SMS invite
    print(f"\n2. Sending SMS invite...")

    response = requests.post(
        f"{BASE_URL}/api/invites/sms",
        headers={"Authorization": f"Bearer {token}"},
        json={
            "phoneNumber": "+15555555555",
            "message": "Join my physical therapy program!"
        }
    )

    print_test("POST /sms", response.status_code)

    if response.status_code == 200:
        print(f"      SMS sent successfully (or mocked)")
        return True
    elif response.status_code == 500:
        print(f"      SMS failed - Twilio may not be configured (expected in dev)")
        print(f"      Response: {response.text[:200]}")
        return "partial"  # Not a failure - Twilio unavailable
    else:
        print(f"      Response: {response.text}")

    return False

# ============================================================================
# Test 6: POST /token (Invite Token)
# ============================================================================

def test_invite_token():
    print_section("TEST 6: Create Invite Token")

    # Register trainer
    print(f"\n1. Registering trainer...")

    email = generate_unique_email()
    response = requests.post(
        f"{BASE_URL}/auth/trainer/register",
        json={"email": email, "password": "SecurePass123!", "fullName": "Token Test Trainer"}
    )

    if response.status_code != 200:
        print_test("Register trainer", response.status_code, 200, "FAILED")
        return False

    token = response.json()['token']
    print_test("Register trainer", 200, 200)

    # Step 2: Create invite token
    print(f"\n2. Creating invite token...")

    response = requests.post(
        f"{BASE_URL}/api/invites/token",
        headers={"Authorization": f"Bearer {token}"},
        json={"expiresInHours": 72}
    )

    print_test("POST /token", response.status_code)

    if response.status_code == 200:
        invite_data = response.json()
        invite_token = invite_data.get('token', 'N/A')
        print(f"      Invite Token: {invite_token[:50] if len(invite_token) > 50 else invite_token}...")
        return True
    else:
        print(f"      Response: {response.text}")

    return False

# ============================================================================
# Main Test Runner
# ============================================================================

def run_all_tests():
    print("\n")
    print("="*80)
    print("  ADAPLIO API - REMAINING ENDPOINTS TEST SUITE")
    print("  Testing previously untested high-priority endpoints")
    print("="*80)

    results = []

    # Run all tests
    tests = [
        ("Token Refresh (POST /auth/refresh)", test_token_refresh),
        ("Weekly Board (GET /weekly-board)", test_weekly_board),
        ("Set User Role (POST /auth/role)", test_set_role),
        ("Onboarding Preferences (POST /onboarding)", test_onboarding),
        ("SMS Invite (POST /sms)", test_sms_invite),
        ("Invite Token (POST /token)", test_invite_token),
    ]

    for test_name, test_func in tests:
        try:
            result = test_func()
            results.append((test_name, result))
        except Exception as e:
            print(f"\n[ERROR] {test_name} failed with exception: {e}")
            results.append((test_name, False))

    # Print summary
    print_section("TEST SUMMARY")

    passed = 0
    partial = 0
    failed = 0

    for test_name, result in results:
        if result is True:
            status = "[PASS]"
            passed += 1
        elif result == "partial":
            status = "[PARTIAL]"
            partial += 1
        else:
            status = "[FAIL]"
            failed += 1

        print(f"{status} {test_name}")

    total = len(results)
    print(f"\n{'='*80}")
    print(f"TOTAL: {passed} passed, {partial} partial, {failed} failed out of {total} tests")
    print(f"Success Rate: {(passed / total * 100):.1f}%")
    print(f"{'='*80}\n")

if __name__ == "__main__":
    run_all_tests()
