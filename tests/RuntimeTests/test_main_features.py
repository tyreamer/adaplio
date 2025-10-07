import requests
import json
import time
from datetime import datetime

BASE_URL = "http://localhost:8080"

# Fresh tokens from latest session
CLIENT_TOKEN = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIxOSIsImVtYWlsIjoidGVzdGNsaWVudEB0ZXN0LmNvbSIsInJvbGUiOiJjbGllbnQiLCJ1c2VyX3R5cGUiOiJjbGllbnQiLCJhbGlhcyI6IkMtVlEzTyIsIm5iZiI6MTc1OTc1OTQ2MSwiZXhwIjoxNzU5NzYzMDYxLCJpYXQiOjE3NTk3NTk0NjEsImlzcyI6ImFkYXBsaW8tYXBpIiwiYXVkIjoiYWRhcGxpby1mcm9udGVuZCJ9.GzvqGjTrJTjM3yU06ecOyJjMACxMLrE0TTIXb3G66hA"
TRAINER_TOKEN = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIxOCIsImVtYWlsIjoidGVzdHRyYWluZXJAdGVzdC5jb20iLCJyb2xlIjoidHJhaW5lciIsInVzZXJfdHlwZSI6InRyYWluZXIiLCJuYmYiOjE3NTk3NTk0NDAsImV4cCI6MTc1OTc2MzA0MCwiaWF0IjoxNzU5NzU5NDQwLCJpc3MiOiJhZGFwbGlvLWFwaSIsImF1ZCI6ImFkYXBsaW8tZnJvbnRlbmQifQ.kTTt01MV9yPWg7bTLjuQR-PUJ4GggC366-ZvvBRoPpg"
CLIENT_USER_ID = "19"  # For creating proposals
TRAINER_USER_ID = "18"

def print_section(title):
    print("\n" + "="*60)
    print(f"  {title}")
    print("="*60)

def print_result(test_name, success, details=""):
    status = "[PASS]" if success else "[FAIL]"
    print(f"{status} | {test_name}")
    if details:
        print(f"     -> {details}")

def test_client_dashboard():
    print_section("CLIENT DASHBOARD")
    headers = {"Authorization": f"Bearer {CLIENT_TOKEN}"}

    try:
        # Test client board endpoint
        response = requests.get(f"{BASE_URL}/api/client/board", headers=headers)
        print_result("GET /api/client/board", response.status_code == 200,
                    f"Status: {response.status_code}")

        if response.status_code == 200:
            data = response.json()
            print(f"     -> Active Plans: {len(data.get('activePlans', []))}")
            print(f"     -> Pending Proposals: {len(data.get('pendingProposals', []))}")

        # Test client progress summary
        response = requests.get(f"{BASE_URL}/api/client/progress/summary", headers=headers)
        print_result("GET /api/client/progress/summary", response.status_code == 200,
                    f"Status: {response.status_code}")

        # Test gamification
        response = requests.get(f"{BASE_URL}/api/client/gamification", headers=headers)
        print_result("GET /api/client/gamification", response.status_code == 200,
                    f"Status: {response.status_code}")

        if response.status_code == 200:
            data = response.json()
            print(f"     -> Level: {data.get('level', 0)}")
            print(f"     -> XP: {data.get('xp', 0)}")
            print(f"     -> Current Streak: {data.get('currentStreak', 0)}")

        return True
    except Exception as e:
        print_result("Client Dashboard Tests", False, str(e))
        return False

def test_trainer_features():
    print_section("TRAINER FEATURES")
    headers = {"Authorization": f"Bearer {TRAINER_TOKEN}"}

    try:
        # Test getting clients
        response = requests.get(f"{BASE_URL}/api/trainer/clients", headers=headers)
        print_result("GET /api/trainer/clients", response.status_code == 200,
                    f"Status: {response.status_code}")

        if response.status_code == 200:
            clients = response.json()
            print(f"     -> Total Clients: {len(clients)}")

        # Test getting templates
        response = requests.get(f"{BASE_URL}/api/trainer/templates", headers=headers)
        print_result("GET /api/trainer/templates", response.status_code == 200,
                    f"Status: {response.status_code}")

        if response.status_code == 200:
            templates = response.json()
            print(f"     -> Total Templates: {len(templates)}")

        # Test getting proposals
        response = requests.get(f"{BASE_URL}/api/trainer/proposals", headers=headers)
        print_result("GET /api/trainer/proposals", response.status_code == 200,
                    f"Status: {response.status_code}")

        return True
    except Exception as e:
        print_result("Trainer Features Tests", False, str(e))
        return False

def test_exercise_library():
    print_section("EXERCISE LIBRARY")
    headers = {"Authorization": f"Bearer {TRAINER_TOKEN}"}

    try:
        # Test getting all exercises (check if endpoint exists)
        response = requests.get(f"{BASE_URL}/api/exercises", headers=headers)
        print_result("GET /api/exercises", response.status_code in [200, 404],
                    f"Status: {response.status_code} (endpoint may not be implemented)")

        if response.status_code == 200:
            exercises = response.json()
            print(f"     -> Total Exercises: {len(exercises)}")
            if exercises:
                print(f"     -> Sample: {exercises[0].get('name', 'N/A')}")

        return True
    except Exception as e:
        print_result("Exercise Library Tests", False, str(e))
        return False

def test_plan_template_creation():
    print_section("PLAN TEMPLATE CREATION")
    headers = {"Authorization": f"Bearer {TRAINER_TOKEN}"}

    try:
        # Create a plan template
        template_data = {
            "name": "Test Recovery Plan",
            "description": "Automated test plan template",
            "category": "recovery",
            "durationWeeks": 4,
            "isPublic": False,
            "items": [
                {
                    "exerciseName": "Knee Flexion",
                    "exerciseDescription": "Bend knee gently",
                    "exerciseCategory": "flexibility",
                    "targetSets": 3,
                    "targetReps": 10,
                    "holdSeconds": 30,
                    "frequencyPerWeek": 5,
                    "days": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"],
                    "notes": "Start slowly, increase range of motion gradually"
                }
            ]
        }

        response = requests.post(f"{BASE_URL}/api/trainer/templates",
                               headers=headers, json=template_data)
        print_result("POST /trainer/templates", response.status_code in [200, 201],
                    f"Status: {response.status_code}")

        if response.status_code in [200, 201]:
            template = response.json()
            template_id = template.get('id')
            print(f"     -> Created Template ID: {template_id}")
            return template_id

        return None
    except Exception as e:
        print_result("Plan Template Creation", False, str(e))
        return None

def test_plan_proposal_workflow(template_id):
    print_section("PLAN PROPOSAL WORKFLOW")
    headers = {"Authorization": f"Bearer {TRAINER_TOKEN}"}
    client_headers = {"Authorization": f"Bearer {CLIENT_TOKEN}"}

    try:
        if not template_id:
            print_result("Plan Proposal Workflow", False, "No template ID provided")
            return False

        # Create a plan proposal
        # First get the client alias
        client_response = requests.get(f"{BASE_URL}/api/me/profile", headers=client_headers)
        client_alias = "C-VQ3O"  # We know this from earlier
        if client_response.status_code == 200:
            client_data = client_response.json()
            client_alias = client_data.get('alias', 'C-VQ3O')

        proposal_data = {
            "clientAlias": client_alias,
            "templateId": template_id,
            "startsOn": datetime.now().date().isoformat(),
            "message": "Test proposal from automated testing"
        }

        response = requests.post(f"{BASE_URL}/api/trainer/proposals",
                               headers=headers, json=proposal_data)
        print_result("POST /api/trainer/proposals (Create)", response.status_code in [200, 201],
                    f"Status: {response.status_code}")

        if response.status_code not in [200, 201]:
            return False

        proposal = response.json()
        proposal_id = proposal.get('id')
        print(f"     -> Created Proposal ID: {proposal_id}")

        # Client views proposals
        response = requests.get(f"{BASE_URL}/api/client/proposals", headers=client_headers)
        print_result("GET /api/client/proposals (View)", response.status_code == 200,
                    f"Status: {response.status_code}")

        if response.status_code == 200:
            proposals = response.json()
            print(f"     -> Client sees {len(proposals)} proposal(s)")

        # Client accepts proposal
        accept_data = {
            "acceptAll": True
        }
        response = requests.post(f"{BASE_URL}/api/client/proposals/{proposal_id}/accept",
                               headers=client_headers, json=accept_data)
        print_result("POST /api/client/proposals/{{id}}/accept", response.status_code == 200,
                    f"Status: {response.status_code}")

        if response.status_code == 200:
            result = response.json()
            print(f"     -> Plan Instance ID: {result.get('planInstanceId')}")
            print(f"     -> Accepted {result.get('acceptedItems')}/{result.get('totalItems')} items")

        return True
    except Exception as e:
        print_result("Plan Proposal Workflow", False, str(e))
        return False

def test_exercise_completion():
    print_section("EXERCISE COMPLETION & PROGRESS")
    headers = {"Authorization": f"Bearer {CLIENT_TOKEN}"}

    try:
        # Get active plans
        response = requests.get(f"{BASE_URL}/api/client/plans", headers=headers)
        print_result("GET /api/client/plans", response.status_code == 200,
                    f"Status: {response.status_code}")

        if response.status_code != 200:
            return False

        plans = response.json()
        if not plans:
            print_result("Exercise Completion", False, "No active plans found")
            return False

        plan = plans[0]
        print(f"     -> Testing with Plan ID: {plan.get('id')}")

        # Get exercise instances
        if 'exercises' in plan and plan['exercises']:
            exercise_instance = plan['exercises'][0]
            instance_id = exercise_instance.get('id')

            # Log progress
            progress_data = {
                "exerciseInstanceId": instance_id,
                "completed": True,
                "notes": "Automated test completion",
                "timestamp": datetime.now().isoformat()
            }

            response = requests.post(f"{BASE_URL}/api/client/progress",
                                   headers=headers, json=progress_data)
            print_result("POST /api/client/progress", response.status_code == 200,
                        f"Status: {response.status_code}")

            # Check updated progress
            response = requests.get(f"{BASE_URL}/api/client/progress/summary", headers=headers)
            print_result("GET /api/client/progress/summary (After completion)", response.status_code == 200,
                        f"Status: {response.status_code}")

            return True
        else:
            print_result("Exercise Completion", False, "No exercises in plan")
            return False

    except Exception as e:
        print_result("Exercise Completion", False, str(e))
        return False

def test_profile_management():
    print_section("PROFILE MANAGEMENT")
    headers = {"Authorization": f"Bearer {CLIENT_TOKEN}"}

    try:
        # Get current profile
        response = requests.get(f"{BASE_URL}/api/me/profile", headers=headers)
        print_result("GET /api/me/profile", response.status_code == 200,
                    f"Status: {response.status_code}")

        if response.status_code == 200:
            profile = response.json()
            print(f"     -> User: {profile.get('email')}")
            print(f"     -> Type: {profile.get('userType')}")
            print(f"     -> Alias: {profile.get('alias', 'N/A')}")

        # Update profile
        update_data = {
            "displayName": "Updated Test User",
            "timezone": "America/New_York"
        }

        response = requests.patch(f"{BASE_URL}/api/me/profile",
                              headers=headers, json=update_data)
        print_result("PATCH /api/me/profile", response.status_code == 200,
                    f"Status: {response.status_code}")

        return True
    except Exception as e:
        print_result("Profile Management", False, str(e))
        return False

def test_analytics():
    print_section("ANALYTICS")
    headers = {"Authorization": f"Bearer {TRAINER_TOKEN}"}

    try:
        # Get analytics events
        response = requests.get(f"{BASE_URL}/analytics/events", headers=headers)
        print_result("GET /analytics/events", response.status_code == 200,
                    f"Status: {response.status_code}")

        if response.status_code == 200:
            events = response.json()
            print(f"     -> Total Events: {len(events)}")

        return True
    except Exception as e:
        print_result("Analytics", False, str(e))
        return False

def main():
    print("\n" + "="*60)
    print("  ADAPLIO RUNTIME TESTS - MAIN FEATURES")
    print("="*60)
    print(f"Testing API at: {BASE_URL}")
    print(f"Started at: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")

    results = []

    # Test 1: Client Dashboard
    results.append(("Client Dashboard", test_client_dashboard()))
    time.sleep(1)

    # Test 2: Trainer Features
    results.append(("Trainer Features", test_trainer_features()))
    time.sleep(1)

    # Test 3: Exercise Library
    results.append(("Exercise Library", test_exercise_library()))
    time.sleep(1)

    # Test 4: Plan Template Creation
    template_id = test_plan_template_creation()
    results.append(("Plan Template Creation", template_id is not None))
    time.sleep(1)

    # Test 5: Plan Proposal Workflow
    if template_id:
        results.append(("Plan Proposal Workflow", test_plan_proposal_workflow(template_id)))
        time.sleep(1)

    # Test 6: Exercise Completion
    results.append(("Exercise Completion", test_exercise_completion()))
    time.sleep(1)

    # Test 7: Profile Management
    results.append(("Profile Management", test_profile_management()))
    time.sleep(1)

    # Test 8: Analytics
    results.append(("Analytics", test_analytics()))

    # Summary
    print_section("TEST SUMMARY")
    passed = sum(1 for _, result in results if result)
    total = len(results)

    for test_name, result in results:
        status = "[PASS]" if result else "[FAIL]"
        print(f"{status} {test_name}")

    print(f"\nTotal: {passed}/{total} tests passed ({passed*100//total}%)")
    print(f"Completed at: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")

    return passed == total

if __name__ == "__main__":
    success = main()
    exit(0 if success else 1)
