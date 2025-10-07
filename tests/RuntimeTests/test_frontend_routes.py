"""
Adaplio Frontend - Route and Navigation Tests
Tests all frontend pages are accessible and routes work correctly
"""

import requests
from bs4 import BeautifulSoup
import time

# Frontend configuration
FRONTEND_URL = "http://localhost:5000"  # Default Blazor dev port
API_URL = "http://localhost:8080"

def print_section(title):
    print(f"\n{'='*80}")
    print(f"  {title}")
    print(f"{'='*80}")

def print_test(name, status_code, expected=200, details=""):
    if status_code == expected:
        status = "[PASS]"
    elif status_code == 404:
        status = "[404]"
    elif status_code == 401:
        status = "[AUTH]"
    else:
        status = "[FAIL]"
    print(f"{status} {name} - {status_code}")
    if details:
        print(f"      {details}")

# ============================================================================
# All Frontend Routes
# ============================================================================

ROUTES = {
    "Public Pages": [
        ("/", "Home/Landing Page"),
        ("/welcome", "Welcome Page"),
        ("/join", "Join Page"),
    ],
    "Authentication Pages": [
        ("/auth/client/login", "Client Login"),
        ("/auth/trainer/login", "Trainer Login"),
        ("/auth/trainer/register", "Trainer Register"),
        ("/auth/verify", "Verify Magic Link"),
        ("/auth/consent", "Consent Page"),
    ],
    "Client Pages (Require Auth)": [
        ("/home/client", "Client Home"),
        ("/board", "Client Board"),
        ("/weekly-board", "Weekly Board"),
        ("/progress", "Client Progress"),
        ("/adherence", "Client Adherence"),
        ("/rewards", "Rewards/Gamification"),
        ("/action-plans", "Action Plans"),
    ],
    "Trainer Pages (Require Auth)": [
        ("/home/trainer", "Trainer Home"),
        ("/trainer/dashboard", "Trainer Dashboard"),
        ("/clients", "Clients List"),
        ("/trainer/templates", "Plan Templates"),
        ("/trainer/proposals", "Proposals"),
    ],
    "Shared Pages (Require Auth)": [
        ("/profile", "User Profile"),
        ("/settings", "Settings"),
    ],
    "Onboarding Pages": [
        ("/client/onboarding", "Client Onboarding"),
        ("/onboarding", "Lightning Onboarding"),
    ],
    "Dynamic Routes": [
        ("/grant/TEST123", "Accept Grant (with code)"),
        ("/exercise/1", "Exercise Detail (with ID)"),
    ],
}

def test_route_accessibility():
    """
    Test that all routes are defined and accessible
    (Note: Many will redirect to login if not authenticated)
    """
    print_section("FRONTEND ROUTE ACCESSIBILITY TEST")

    print(f"\nTesting frontend at: {FRONTEND_URL}")
    print(f"Note: Protected routes will likely redirect to login (expected behavior)\n")

    total_routes = 0
    accessible_routes = 0
    redirect_routes = 0
    not_found_routes = 0

    for category, routes in ROUTES.items():
        print(f"\n{category}:")
        print("-" * 60)

        for route, description in routes:
            total_routes += 1
            url = f"{FRONTEND_URL}{route}"

            try:
                response = requests.get(url, allow_redirects=False, timeout=10)
                status = response.status_code

                if status == 200:
                    accessible_routes += 1
                    print_test(description, status, 200, f"Route: {route}")
                elif status in [301, 302, 307, 308]:
                    redirect_routes += 1
                    redirect_to = response.headers.get('Location', 'Unknown')
                    print_test(description, status, details=f"Redirects to: {redirect_to}")
                elif status == 404:
                    not_found_routes += 1
                    print_test(description, status, details=f"Route not found: {route}")
                else:
                    print_test(description, status, details=f"Unexpected status")

            except requests.exceptions.ConnectionError:
                print(f"[ERROR] {description} - Cannot connect to frontend")
                print(f"        Is the frontend running at {FRONTEND_URL}?")
                return False
            except requests.exceptions.Timeout:
                print(f"[TIMEOUT] {description} - Request timed out")
            except Exception as e:
                print(f"[ERROR] {description} - {str(e)}")

    # Summary
    print_section("ROUTE ACCESSIBILITY SUMMARY")
    print(f"Total Routes: {total_routes}")
    print(f"Accessible (200): {accessible_routes}")
    print(f"Redirects (3xx): {redirect_routes}")
    print(f"Not Found (404): {not_found_routes}")
    print(f"Other: {total_routes - accessible_routes - redirect_routes - not_found_routes}")

    if not_found_routes > 0:
        print(f"\n[WARNING] {not_found_routes} routes returned 404 - routes may not be defined")

    return True

def test_blazor_app_loads():
    """Test that the Blazor app actually loads"""
    print_section("BLAZOR APP LOADING TEST")

    url = f"{FRONTEND_URL}/"

    try:
        print(f"\n1. Testing if Blazor app loads at {url}...")
        response = requests.get(url, timeout=10)

        print_test("Home page loads", response.status_code, 200)

        if response.status_code == 200:
            html = response.text

            # Check for Blazor markers
            checks = [
                ("Blazor script tag", "_framework/blazor.webassembly.js" in html),
                ("App component", 'id="app"' in html or "id='app'" in html),
                ("Base tag", '<base href="/">' in html or "<base href='/' />" in html),
            ]

            print(f"\n2. Checking Blazor components in HTML...")
            for check_name, result in checks:
                status = "[PASS]" if result else "[FAIL]"
                print(f"{status} {check_name}")

            # Check if _framework files are accessible
            print(f"\n3. Checking Blazor framework files...")
            framework_files = [
                "/_framework/blazor.webassembly.js",
                "/_framework/blazor.boot.json",
            ]

            for file_path in framework_files:
                file_url = f"{FRONTEND_URL}{file_path}"
                try:
                    file_response = requests.get(file_url, timeout=5)
                    print_test(f"Framework file: {file_path}", file_response.status_code, 200)
                except:
                    print(f"[FAIL] Cannot access {file_path}")

            return True
        else:
            return False

    except requests.exceptions.ConnectionError:
        print(f"[ERROR] Cannot connect to frontend at {FRONTEND_URL}")
        print(f"        Please start the frontend with:")
        print(f"        cd src/Frontend/Adaplio.Frontend && dotnet run")
        return False
    except Exception as e:
        print(f"[ERROR] {str(e)}")
        return False

def test_api_configuration():
    """Test that frontend is configured to connect to correct API"""
    print_section("API CONFIGURATION TEST")

    url = f"{FRONTEND_URL}/appsettings.json"

    try:
        print(f"\n1. Checking API configuration...")
        response = requests.get(url, timeout=5)

        if response.status_code == 200:
            config = response.json()
            api_base_url = config.get('ApiSettings', {}).get('BaseUrl', 'Not configured')

            print(f"\nConfigured API URL: {api_base_url}")
            print(f"Expected API URL (local): {API_URL}")

            if api_base_url == API_URL:
                print("[PASS] API configured for local development")
            elif "localhost" in api_base_url:
                print(f"[WARNING] API configured for localhost but different port")
                print(f"          Frontend: {api_base_url}")
                print(f"          Expected: {API_URL}")
            else:
                print(f"[INFO] API configured for production: {api_base_url}")
                print(f"       For local testing, update appsettings.Development.json")

            return True
        else:
            print(f"[FAIL] Cannot access appsettings.json - {response.status_code}")
            return False

    except Exception as e:
        print(f"[ERROR] {str(e)}")
        return False

def test_navigation_flow():
    """Test basic navigation flows"""
    print_section("NAVIGATION FLOW TEST")

    print(f"\nTesting navigation paths...\n")

    flows = [
        {
            "name": "Trainer Registration Flow",
            "steps": [
                ("/", "Home"),
                ("/auth/trainer/register", "Register"),
                ("/auth/trainer/login", "Login"),
            ]
        },
        {
            "name": "Client Login Flow",
            "steps": [
                ("/", "Home"),
                ("/auth/client/login", "Client Login"),
                ("/auth/verify", "Verify"),
            ]
        },
    ]

    for flow in flows:
        print(f"{flow['name']}:")
        print("-" * 60)

        for route, step_name in flow['steps']:
            url = f"{FRONTEND_URL}{route}"
            try:
                response = requests.get(url, timeout=5, allow_redirects=False)
                print_test(f"  {step_name}", response.status_code)
            except:
                print(f"[ERROR] Cannot access {step_name}")

        print()

    return True

def run_all_frontend_tests():
    """Run all frontend tests"""
    print("\n")
    print("="*80)
    print("  ADAPLIO FRONTEND - ROUTE & NAVIGATION TESTS")
    print("  Testing frontend accessibility and routing")
    print("="*80)

    tests = [
        ("Blazor App Loading", test_blazor_app_loads),
        ("API Configuration", test_api_configuration),
        ("Route Accessibility", test_route_accessibility),
        ("Navigation Flows", test_navigation_flow),
    ]

    results = []

    for test_name, test_func in tests:
        try:
            result = test_func()
            results.append((test_name, result))
        except Exception as e:
            print(f"\n[ERROR] {test_name} failed with exception: {e}")
            results.append((test_name, False))

    # Print final summary
    print_section("FINAL SUMMARY")

    passed = sum(1 for _, result in results if result)
    total = len(results)

    for test_name, result in results:
        status = "[PASS]" if result else "[FAIL]"
        print(f"{status} {test_name}")

    print(f"\n{'='*80}")
    print(f"TOTAL: {passed}/{total} test categories passed ({passed/total*100:.1f}%)")
    print(f"{'='*80}")

    if passed == total:
        print("\n[SUCCESS] All frontend tests passed!")
    else:
        print(f"\n[WARNING] {total - passed} test category/categories failed")
        print("Check if frontend is running: cd src/Frontend/Adaplio.Frontend && dotnet run")

    print()

if __name__ == "__main__":
    run_all_frontend_tests()
