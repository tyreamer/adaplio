"""
Adaplio - Frontend to Backend Integration Tests
Verifies that all frontend pages can successfully communicate with backend API
Tests that the workflows tested in backend are accessible from frontend
"""

import requests
import json
import time

FRONTEND_URL = "http://localhost:5000"
API_URL = "http://localhost:8080"

def print_section(title):
    print(f"\n{'='*80}")
    print(f"  {title}")
    print(f"{'='*80}")

def print_test(name, passed, details=""):
    status = "[PASS]" if passed else "[FAIL]"
    print(f"{status} {name}")
    if details:
        print(f"      {details}")

# ============================================================================
# Test: Frontend Can Reach Backend
# ============================================================================

def test_frontend_backend_connectivity():
    """Test that frontend pages are configured to reach backend"""
    print_section("FRONTEND-BACKEND CONNECTIVITY")

    tests_passed = 0
    total_tests = 0

    # Test 1: Frontend loads
    total_tests += 1
    print("\n1. Testing frontend is running...")
    try:
        response = requests.get(f"{FRONTEND_URL}/", timeout=5)
        if response.status_code == 200:
            tests_passed += 1
            print_test("Frontend accessible", True, f"{FRONTEND_URL}")
        else:
            print_test("Frontend accessible", False, f"Status: {response.status_code}")
    except:
        print_test("Frontend accessible", False, "Cannot connect")
        return 0, total_tests

    # Test 2: Backend API is running
    total_tests += 1
    print("\n2. Testing backend API is running...")
    try:
        response = requests.get(f"{API_URL}/health", timeout=5)
        if response.status_code == 200:
            tests_passed += 1
            print_test("Backend API accessible", True, f"{API_URL}")
        else:
            print_test("Backend API accessible", False, f"Status: {response.status_code}")
    except:
        print_test("Backend API accessible", False, "Cannot connect")
        return tests_passed, total_tests

    # Test 3: CORS Configuration
    total_tests += 1
    print("\n3. Testing CORS configuration...")
    try:
        # Make a request with Origin header (simulating frontend)
        headers = {"Origin": FRONTEND_URL}
        response = requests.options(f"{API_URL}/health", headers=headers, timeout=5)

        # Check for CORS headers
        cors_header = response.headers.get("Access-Control-Allow-Origin")

        if cors_header:
            tests_passed += 1
            print_test("CORS configured", True, f"Allowed origin: {cors_header}")
        else:
            print_test("CORS configured", False, "No CORS headers found")
    except Exception as e:
        print_test("CORS configured", False, str(e))

    print(f"\n{tests_passed}/{total_tests} connectivity tests passed")
    return tests_passed, total_tests

# ============================================================================
# Test: Critical User Workflows Accessible from Frontend
# ============================================================================

def test_workflow_pages_exist():
    """Test that frontend pages exist for all tested backend workflows"""
    print_section("WORKFLOW PAGES VERIFICATION")

    workflows = [
        {
            "name": "Trainer Authentication",
            "pages": [
                ("/auth/trainer/register", "Registration page"),
                ("/auth/trainer/login", "Login page"),
            ]
        },
        {
            "name": "Client Authentication",
            "pages": [
                ("/auth/client/login", "Magic link login"),
                ("/auth/verify", "Verification page"),
            ]
        },
        {
            "name": "Plan Templates (Tested in Backend)",
            "pages": [
                ("/trainer/templates", "Templates list"),
            ]
        },
        {
            "name": "Plan Proposals (Tested in Backend)",
            "pages": [
                ("/trainer/proposals", "Create proposals"),
            ]
        },
        {
            "name": "Client Dashboard (Tested in Backend)",
            "pages": [
                ("/home/client", "Client home"),
                ("/board", "Exercise board"),
                ("/progress", "Progress tracking"),
            ]
        },
        {
            "name": "Gamification (Tested in Backend)",
            "pages": [
                ("/rewards", "Rewards/XP page"),
            ]
        },
        {
            "name": "Trainer Dashboard (Tested in Backend)",
            "pages": [
                ("/home/trainer", "Trainer home"),
                ("/clients", "Client list"),
                ("/trainer/dashboard", "Dashboard"),
            ]
        },
        {
            "name": "Profile Management (Tested in Backend)",
            "pages": [
                ("/profile", "User profile"),
                ("/settings", "Settings"),
            ]
        },
    ]

    total_workflows = len(workflows)
    workflows_complete = 0

    for workflow in workflows:
        print(f"\n{workflow['name']}:")
        print("-" * 60)

        all_pages_exist = True

        for route, description in workflow['pages']:
            url = f"{FRONTEND_URL}{route}"
            try:
                response = requests.get(url, timeout=5, allow_redirects=False)

                if response.status_code == 200:
                    print(f"  [OK] {description} - {route}")
                else:
                    print(f"  [WARN] {description} - {route} (Status: {response.status_code})")
                    all_pages_exist = False
            except:
                print(f"  [FAIL] {description} - {route} (Cannot access)")
                all_pages_exist = False

        if all_pages_exist:
            workflows_complete += 1

    print(f"\n{workflows_complete}/{total_workflows} workflows have complete frontend pages")
    return workflows_complete, total_workflows

# ============================================================================
# Test: API Endpoints Called by Frontend
# ============================================================================

def test_api_endpoints_reachable():
    """Test that key API endpoints are reachable (same ones frontend will call)"""
    print_section("API ENDPOINTS REACHABILITY FROM FRONTEND CONTEXT")

    endpoints = [
        # Authentication
        ("POST", "/auth/trainer/register", "Trainer registration"),
        ("POST", "/auth/trainer/login", "Trainer login"),
        ("POST", "/auth/client/magic-link", "Client magic link"),
        ("POST", "/auth/client/verify", "Client verification"),

        # Profile
        ("GET", "/auth/me", "Get current user (requires auth)"),

        # Plans & Templates
        ("GET", "/api/trainer/templates", "Get templates (requires auth)"),
        ("GET", "/api/trainer/proposals", "Get proposals (requires auth)"),

        # Client Dashboard
        ("GET", "/api/client/board", "Get exercise board (requires auth)"),
        ("GET", "/api/client/progress/summary", "Get progress summary (requires auth)"),
        ("GET", "/api/client/gamification", "Get gamification (requires auth)"),

        # Trainer Dashboard
        ("GET", "/api/trainer/clients", "Get clients (requires auth)"),

        # Health
        ("GET", "/health", "Health check"),
    ]

    tests_passed = 0
    total_tests = len(endpoints)

    print("\nTesting API endpoints accessibility...")
    print("(401/403 expected for protected endpoints without auth)\n")

    for method, endpoint, description in endpoints:
        url = f"{API_URL}{endpoint}"

        try:
            if method == "GET":
                response = requests.get(url, timeout=5)
            elif method == "POST":
                response = requests.post(url, json={}, timeout=5)

            # For endpoints requiring auth, 401 is expected and OK
            # For health check, 200 is expected
            if endpoint == "/health":
                expected = response.status_code == 200
            else:
                # Auth endpoints might return 400 (bad request) or 401 (unauthorized)
                # Both mean the endpoint exists
                expected = response.status_code in [200, 400, 401, 403, 500]

            if expected:
                tests_passed += 1
                print(f"  [OK] {method} {endpoint} - {response.status_code}")
            else:
                print(f"  [FAIL] {method} {endpoint} - {response.status_code}")

        except Exception as e:
            print(f"  [FAIL] {method} {endpoint} - {str(e)}")

    print(f"\n{tests_passed}/{total_tests} API endpoints reachable")
    return tests_passed, total_tests

# ============================================================================
# Test: Critical Page-to-API Mappings
# ============================================================================

def test_page_api_mappings():
    """Verify that pages calling specific APIs can reach them"""
    print_section("PAGE-TO-API MAPPING VERIFICATION")

    mappings = [
        {
            "page": "/auth/trainer/register",
            "api": "/auth/trainer/register",
            "description": "Registration page -> Registration API"
        },
        {
            "page": "/auth/client/login",
            "api": "/auth/client/magic-link",
            "description": "Client login page -> Magic link API"
        },
        {
            "page": "/board",
            "api": "/api/client/board",
            "description": "Exercise board page -> Board API"
        },
        {
            "page": "/trainer/templates",
            "api": "/api/trainer/templates",
            "description": "Templates page -> Templates API"
        },
        {
            "page": "/profile",
            "api": "/auth/me",
            "description": "Profile page -> User info API"
        },
        {
            "page": "/rewards",
            "api": "/api/client/gamification",
            "description": "Rewards page -> Gamification API"
        },
    ]

    tests_passed = 0
    total_tests = len(mappings)

    print("\nVerifying page-to-API mappings...\n")

    for mapping in mappings:
        page_url = f"{FRONTEND_URL}{mapping['page']}"
        api_url = f"{API_URL}{mapping['api']}"

        try:
            # Check page exists
            page_response = requests.get(page_url, timeout=5, allow_redirects=False)
            page_exists = page_response.status_code == 200

            # Check API endpoint exists
            api_response = requests.get(api_url, timeout=5)
            # Any status except 404 means endpoint exists
            api_exists = api_response.status_code != 404

            if page_exists and api_exists:
                tests_passed += 1
                print(f"  [OK] {mapping['description']}")
                print(f"       Page: {page_response.status_code}, API: {api_response.status_code}")
            elif not page_exists:
                print(f"  [FAIL] {mapping['description']}")
                print(f"         Page not found: {mapping['page']}")
            elif not api_exists:
                print(f"  [FAIL] {mapping['description']}")
                print(f"         API not found: {mapping['api']}")

        except Exception as e:
            print(f"  [FAIL] {mapping['description']}")
            print(f"         Error: {str(e)}")

    print(f"\n{tests_passed}/{total_tests} page-to-API mappings verified")
    return tests_passed, total_tests

# ============================================================================
# Main Test Runner
# ============================================================================

def run_all_integration_tests():
    """Run all frontend-backend integration tests"""
    print("\n")
    print("="*80)
    print("  ADAPLIO - FRONTEND-BACKEND INTEGRATION TESTS")
    print("  Verifying frontend can communicate with backend")
    print("="*80)

    all_results = []

    tests = [
        ("Frontend-Backend Connectivity", test_frontend_backend_connectivity),
        ("Workflow Pages Exist", test_workflow_pages_exist),
        ("API Endpoints Reachable", test_api_endpoints_reachable),
        ("Page-to-API Mappings", test_page_api_mappings),
    ]

    for test_name, test_func in tests:
        try:
            passed, total = test_func()
            all_results.append((test_name, passed, total))
        except Exception as e:
            print(f"\n[ERROR] {test_name} failed with exception: {e}")
            all_results.append((test_name, 0, 1))

    # Print final summary
    print_section("INTEGRATION TEST SUMMARY")

    total_passed = sum(passed for _, passed, _ in all_results)
    total_tests = sum(total for _, _, total in all_results)

    for test_name, passed, total in all_results:
        percentage = (passed / total * 100) if total > 0 else 0
        status = "[PASS]" if passed == total else "[PARTIAL]" if passed > 0 else "[FAIL]"
        print(f"{status} {test_name}: {passed}/{total} ({percentage:.1f}%)")

    print(f"\n{'='*80}")
    overall_percentage = (total_passed / total_tests * 100) if total_tests > 0 else 0
    print(f"OVERALL: {total_passed}/{total_tests} tests passed ({overall_percentage:.1f}%)")
    print(f"{'='*80}")

    if overall_percentage >= 90:
        print("\n[SUCCESS] Frontend-backend integration is excellent!")
    elif overall_percentage >= 75:
        print("\n[GOOD] Frontend-backend integration is good with minor issues")
    elif overall_percentage >= 50:
        print("\n[WARNING] Frontend-backend integration has some issues")
    else:
        print("\n[CRITICAL] Frontend-backend integration has significant problems")

    print("\n")

if __name__ == "__main__":
    run_all_integration_tests()
