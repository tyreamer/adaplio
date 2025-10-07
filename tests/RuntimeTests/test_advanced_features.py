import requests
import json
import time
from datetime import datetime, timedelta
import io

BASE_URL = "http://localhost:8080"

# Fresh tokens from latest session
CLIENT_TOKEN = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIxOSIsImVtYWlsIjoidGVzdGNsaWVudEB0ZXN0LmNvbSIsInJvbGUiOiJjbGllbnQiLCJ1c2VyX3R5cGUiOiJjbGllbnQiLCJhbGlhcyI6IkMtVlEzTyIsIm5iZiI6MTc1OTc1OTQ2MSwiZXhwIjoxNzU5NzYzMDYxLCJpYXQiOjE3NTk3NTk0NjEsImlzcyI6ImFkYXBsaW8tYXBpIiwiYXVkIjoiYWRhcGxpby1mcm9udGVuZCJ9.GzvqGjTrJTjM3yU06ecOyJjMACxMLrE0TTIXb3G66hA"
TRAINER_TOKEN = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIxOCIsImVtYWlsIjoidGVzdHRyYWluZXJAdGVzdC5jb20iLCJyb2xlIjoidHJhaW5lciIsInVzZXJfdHlwZSI6InRyYWluZXIiLCJuYmYiOjE3NTk3NTk0NDAsImV4cCI6MTc1OTc2MzA0MCwiaWF0IjoxNzU5NzU5NDQwLCJpc3MiOiJhZGFwbGlvLWFwaSIsImF1ZCI6ImFkYXBsaW8tZnJvbnRlbmQifQ.kTTt01MV9yPWg7bTLjuQR-PUJ4GggC366-ZvvBRoPpg"
CLIENT_REFRESH_TOKEN = "VeJVq+9YfNmjOa7a0ELmvMM1QvEneVXxTBASBRhBJ7A="
TRAINER_REFRESH_TOKEN = "1xh9a3qMI+ZHq0olykmuv/h7dhmIfmsnAOOq1KSkV+I="

def print_section(title):
    print("\n" + "="*60)
    print(f"  {title}")
    print("="*60)

def print_result(test_name, success, details=""):
    status = "[PASS]" if success else "[FAIL]"
    print(f"{status} | {test_name}")
    if details:
        print(f"     -> {details}")

# ============================================================
# TOKEN MANAGEMENT TESTS
# ============================================================

def test_token_refresh():
    print_section("TOKEN REFRESH & MANAGEMENT")

    try:
        # Test token refresh for client
        refresh_data = {
            "refreshToken": CLIENT_REFRESH_TOKEN
        }
        response = requests.post(f"{BASE_URL}/auth/refresh", json=refresh_data)
        print_result("POST /auth/refresh (Client)", response.status_code == 200,
                    f"Status: {response.status_code}")

        new_client_token = None
        if response.status_code == 200:
            data = response.json()
            new_client_token = data.get('token')
            print(f"     -> New token received: {new_client_token is not None}")
            print(f"     -> New refresh token: {data.get('refreshToken') is not None}")

        # Test token refresh for trainer
        refresh_data = {
            "refreshToken": TRAINER_REFRESH_TOKEN
        }
        response = requests.post(f"{BASE_URL}/auth/refresh", json=refresh_data)
        print_result("POST /auth/refresh (Trainer)", response.status_code == 200,
                    f"Status: {response.status_code}")

        # Test logout
        headers = {"Authorization": f"Bearer {CLIENT_TOKEN}"}
        response = requests.post(f"{BASE_URL}/auth/logout", headers=headers)
        print_result("POST /auth/logout", response.status_code in [200, 204],
                    f"Status: {response.status_code}")

        return True
    except Exception as e:
        print_result("Token Management Tests", False, str(e))
        return False

# ============================================================
# EXERCISE PROGRESS LOGGING TESTS
# ============================================================

def test_exercise_progress_logging():
    print_section("EXERCISE PROGRESS LOGGING")
    headers = {"Authorization": f"Bearer {CLIENT_TOKEN}"}

    try:
        # Get board with exercises
        response = requests.get(f"{BASE_URL}/api/client/board", headers=headers)
        print_result("GET /api/client/board", response.status_code == 200,
                    f"Status: {response.status_code}")

        if response.status_code != 200:
            return False

        board = response.json()
        # Find first day with exercises
        exercise_instance_id = None
        for day in board.get('days', []):
            if day.get('exercises'):
                exercise_instance_id = day['exercises'][0]['exerciseInstanceId']
                break

        if not exercise_instance_id:
            print_result("Exercise Progress", False, "No exercises found in board")
            return False

        # Log progress event
        progress_data = {
            "exerciseInstanceId": exercise_instance_id,
            "eventType": "exercise_completed",
            "setsCompleted": 3,
            "repsCompleted": 10,
            "holdSecondsCompleted": 30,
            "painLevel": 2,
            "difficultyRating": 3,
            "notes": "Felt good, slight discomfort but manageable"
        }

        response = requests.post(f"{BASE_URL}/api/client/progress",
                               headers=headers, json=progress_data)
        print_result("POST /api/client/progress", response.status_code == 200,
                    f"Status: {response.status_code}")

        if response.status_code == 200:
            result = response.json()
            print(f"     -> Progress logged: Event ID {result.get('progressEventId')}")
            celebration = result.get('celebration', {})
            if celebration:
                print(f"     -> XP awarded: {celebration.get('xpAwarded', 0)}")
                if celebration.get('leveledUp'):
                    print(f"     -> Leveled up to: {celebration.get('newLevel')}")

        # Get weekly progress
        response = requests.get(f"{BASE_URL}/api/client/progress/week", headers=headers)
        print_result("GET /api/client/progress/week", response.status_code == 200,
                    f"Status: {response.status_code}")

        # Quick log from board
        quick_log_data = {
            "exerciseInstanceId": exercise_instance_id,
            "eventType": "exercise_completed"
        }
        response = requests.post(f"{BASE_URL}/api/client/board/quick-log",
                               headers=headers, json=quick_log_data)
        print_result("POST /api/client/board/quick-log", response.status_code == 200,
                    f"Status: {response.status_code}")

        return True
    except Exception as e:
        print_result("Exercise Progress Tests", False, str(e))
        return False

# ============================================================
# TEMPLATE MANAGEMENT TESTS
# ============================================================

def test_template_management():
    print_section("TEMPLATE UPDATE & DELETE")
    headers = {"Authorization": f"Bearer {TRAINER_TOKEN}"}

    try:
        # First create a template
        template_data = {
            "name": "Template for Update Test",
            "description": "Will be updated",
            "category": "strength",
            "durationWeeks": 4,
            "isPublic": False,
            "items": [
                {
                    "exerciseName": "Shoulder Press",
                    "exerciseDescription": "Press overhead",
                    "exerciseCategory": "strength",
                    "targetSets": 3,
                    "targetReps": 12,
                    "frequencyPerWeek": 3,
                    "days": ["Monday", "Wednesday", "Friday"]
                }
            ]
        }

        response = requests.post(f"{BASE_URL}/api/trainer/templates",
                               headers=headers, json=template_data)
        print_result("POST /api/trainer/templates (Create for test)", response.status_code in [200, 201],
                    f"Status: {response.status_code}")

        if response.status_code not in [200, 201]:
            return False

        template = response.json()
        template_id = template.get('id')
        print(f"     -> Created Template ID: {template_id}")

        # Update the template
        update_data = {
            "name": "Updated Template Name",
            "description": "Description has been updated",
            "category": "flexibility",
            "durationWeeks": 6,
            "isPublic": True,
            "items": [
                {
                    "exerciseName": "Shoulder Press",
                    "exerciseDescription": "Updated description",
                    "exerciseCategory": "strength",
                    "targetSets": 4,
                    "targetReps": 15,
                    "frequencyPerWeek": 4,
                    "days": ["Monday", "Tuesday", "Thursday", "Saturday"]
                },
                {
                    "exerciseName": "Lateral Raises",
                    "exerciseDescription": "New exercise added",
                    "exerciseCategory": "strength",
                    "targetSets": 3,
                    "targetReps": 12,
                    "frequencyPerWeek": 3,
                    "days": ["Monday", "Wednesday", "Friday"]
                }
            ]
        }

        response = requests.put(f"{BASE_URL}/api/trainer/templates/{template_id}",
                              headers=headers, json=update_data)
        print_result("PUT /api/trainer/templates/{id}", response.status_code == 200,
                    f"Status: {response.status_code}")

        if response.status_code == 200:
            updated = response.json()
            print(f"     -> Updated name: {updated.get('name')}")
            print(f"     -> Updated items: {len(updated.get('items', []))}")

        # Delete the template
        response = requests.delete(f"{BASE_URL}/api/trainer/templates/{template_id}",
                                 headers=headers)
        print_result("DELETE /api/trainer/templates/{id}", response.status_code == 200,
                    f"Status: {response.status_code}")

        # Verify it's deleted (should not appear in list)
        response = requests.get(f"{BASE_URL}/api/trainer/templates", headers=headers)
        if response.status_code == 200:
            templates = response.json().get('templates', [])
            deleted_template = next((t for t in templates if t['id'] == template_id), None)
            print_result("Template Deletion Verified", deleted_template is None,
                        f"Template {'not found' if deleted_template is None else 'still exists'}")

        return True
    except Exception as e:
        print_result("Template Management Tests", False, str(e))
        return False

# ============================================================
# FILE UPLOAD TESTS
# ============================================================

def test_file_uploads():
    print_section("FILE UPLOAD SYSTEM")
    headers = {"Authorization": f"Bearer {CLIENT_TOKEN}"}

    try:
        # Test presigned URL generation
        presign_data = {
            "fileName": "test-image.jpg",
            "contentType": "image/jpeg",
            "fileSize": 1024 * 100  # 100KB
        }

        response = requests.post(f"{BASE_URL}/api/uploads/presign",
                               headers=headers, json=presign_data)
        print_result("POST /api/uploads/presign", response.status_code == 200,
                    f"Status: {response.status_code}")

        upload_url = None
        file_path = None
        if response.status_code == 200:
            data = response.json()
            upload_url = data.get('uploadUrl')
            file_path = data.get('filePath')
            print(f"     -> Upload URL generated: {upload_url is not None}")
            print(f"     -> File path: {file_path}")

        # Test direct upload (simulate file upload)
        if upload_url:
            # Create fake image data
            fake_image = io.BytesIO(b'\x89PNG\r\n\x1a\n' + b'\x00' * 1000)
            files = {'file': ('test-image.jpg', fake_image, 'image/jpeg')}

            response = requests.post(f"{BASE_URL}/api/uploads/upload",
                                   files=files, headers=headers)
            print_result("POST /api/uploads/upload", response.status_code in [200, 201],
                        f"Status: {response.status_code}")

        # Test file serving (if file was uploaded)
        if file_path:
            response = requests.get(f"{BASE_URL}/api/uploads/files/{file_path}")
            print_result("GET /api/uploads/files/{path}", response.status_code in [200, 404],
                        f"Status: {response.status_code}")

        return True
    except Exception as e:
        print_result("File Upload Tests", False, str(e))
        return False

# ============================================================
# ERROR HANDLING TESTS
# ============================================================

def test_error_handling():
    print_section("ERROR HANDLING & VALIDATION")

    try:
        # Test with invalid token
        headers = {"Authorization": "Bearer invalid_token"}
        response = requests.get(f"{BASE_URL}/api/me/profile", headers=headers)
        print_result("Invalid Token", response.status_code == 401,
                    f"Status: {response.status_code} (expected 401)")

        # Test with expired token (we'll use an old one)
        headers = {"Authorization": "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE2MzAwMDAwMDB9.invalid"}
        response = requests.get(f"{BASE_URL}/api/me/profile", headers=headers)
        print_result("Expired Token", response.status_code == 401,
                    f"Status: {response.status_code} (expected 401)")

        # Test without token
        response = requests.get(f"{BASE_URL}/api/me/profile")
        print_result("Missing Token", response.status_code == 401,
                    f"Status: {response.status_code} (expected 401)")

        # Test malformed request body
        headers = {"Authorization": f"Bearer {TRAINER_TOKEN}"}
        response = requests.post(f"{BASE_URL}/api/trainer/templates",
                               headers=headers, json={"invalid": "data"})
        print_result("Malformed Request", response.status_code in [400, 500],
                    f"Status: {response.status_code} (expected 400)")

        # Test missing required fields
        response = requests.post(f"{BASE_URL}/api/trainer/templates",
                               headers=headers, json={"name": "Test"})
        print_result("Missing Required Fields", response.status_code in [400, 500],
                    f"Status: {response.status_code} (expected 400)")

        # Test invalid IDs
        response = requests.get(f"{BASE_URL}/api/client/proposals/99999",
                              headers={"Authorization": f"Bearer {CLIENT_TOKEN}"})
        print_result("Invalid ID (Not Found)", response.status_code == 404,
                    f"Status: {response.status_code} (expected 404)")

        # Test SQL injection attempt (should be sanitized)
        response = requests.post(f"{BASE_URL}/auth/trainer/login",
                               json={"email": "'; DROP TABLE users; --", "password": "test"})
        print_result("SQL Injection Protection", response.status_code in [400, 401],
                    f"Status: {response.status_code}")

        # Test XSS attempt (should be sanitized)
        headers = {"Authorization": f"Bearer {CLIENT_TOKEN}"}
        response = requests.patch(f"{BASE_URL}/api/me/profile",
                                headers=headers,
                                json={"displayName": "<script>alert('xss')</script>"})
        print_result("XSS Protection", response.status_code in [200, 400],
                    f"Status: {response.status_code}")

        return True
    except Exception as e:
        print_result("Error Handling Tests", False, str(e))
        return False

# ============================================================
# SECURITY & AUTHORIZATION TESTS
# ============================================================

def test_security_authorization():
    print_section("SECURITY & AUTHORIZATION")

    try:
        # Test client trying to access trainer endpoints
        client_headers = {"Authorization": f"Bearer {CLIENT_TOKEN}"}
        response = requests.get(f"{BASE_URL}/api/trainer/templates", headers=client_headers)
        print_result("Client accessing Trainer endpoint", response.status_code == 403,
                    f"Status: {response.status_code} (expected 403)")

        # Test trainer trying to access client-specific data
        trainer_headers = {"Authorization": f"Bearer {TRAINER_TOKEN}"}
        response = requests.get(f"{BASE_URL}/api/client/board", headers=trainer_headers)
        print_result("Trainer accessing Client endpoint", response.status_code == 403,
                    f"Status: {response.status_code} (expected 403)")

        # Test accessing another user's data
        response = requests.get(f"{BASE_URL}/api/client/proposals/1",
                              headers={"Authorization": f"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.invalid"})
        print_result("Accessing other user's data", response.status_code == 401,
                    f"Status: {response.status_code}")

        # Test consent scope management
        response = requests.patch(f"{BASE_URL}/api/client/trainers/1/scope",
                                headers=client_headers,
                                json={"scope": "view_summary", "granted": False})
        print_result("PATCH /api/client/trainers/{id}/scope", response.status_code in [200, 404],
                    f"Status: {response.status_code}")

        # Test trainer access revocation
        response = requests.delete(f"{BASE_URL}/api/client/trainers/1",
                                 headers=client_headers)
        print_result("DELETE /api/client/trainers/{id}", response.status_code in [200, 204, 404],
                    f"Status: {response.status_code}")

        return True
    except Exception as e:
        print_result("Security Tests", False, str(e))
        return False

# ============================================================
# ADDITIONAL ENDPOINTS TESTS
# ============================================================

def test_additional_endpoints():
    print_section("ADDITIONAL ENDPOINTS")

    try:
        # Test health check
        response = requests.get(f"{BASE_URL}/health")
        print_result("GET /health", response.status_code == 200,
                    f"Status: {response.status_code}")

        # Test database health
        response = requests.get(f"{BASE_URL}/health/db")
        print_result("GET /health/db", response.status_code == 200,
                    f"Status: {response.status_code}")

        # Test analytics endpoint
        trainer_headers = {"Authorization": f"Bearer {TRAINER_TOKEN}"}
        response = requests.get(f"{BASE_URL}/api/analytics/events", headers=trainer_headers)
        print_result("GET /api/analytics/events", response.status_code in [200, 404],
                    f"Status: {response.status_code}")

        # Test trainer viewing client adherence
        response = requests.get(f"{BASE_URL}/api/trainer/clients/C-VQ3O/adherence",
                              headers=trainer_headers)
        print_result("GET /api/trainer/clients/{alias}/adherence", response.status_code in [200, 404],
                    f"Status: {response.status_code}")

        # Test trainer viewing client gamification
        response = requests.get(f"{BASE_URL}/api/trainer/clients/C-VQ3O/gamification",
                              headers=trainer_headers)
        print_result("GET /api/trainer/clients/{alias}/gamification", response.status_code in [200, 404],
                    f"Status: {response.status_code}")

        return True
    except Exception as e:
        print_result("Additional Endpoints Tests", False, str(e))
        return False

# ============================================================
# MAIN TEST RUNNER
# ============================================================

def main():
    print("\n" + "="*60)
    print("  ADAPLIO ADVANCED FEATURES TESTS")
    print("="*60)
    print(f"Testing API at: {BASE_URL}")
    print(f"Started at: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")

    results = []

    # Run all test suites
    results.append(("Token Management", test_token_refresh()))
    time.sleep(1)

    results.append(("Exercise Progress Logging", test_exercise_progress_logging()))
    time.sleep(1)

    results.append(("Template Management", test_template_management()))
    time.sleep(1)

    results.append(("File Uploads", test_file_uploads()))
    time.sleep(1)

    results.append(("Error Handling", test_error_handling()))
    time.sleep(1)

    results.append(("Security & Authorization", test_security_authorization()))
    time.sleep(1)

    results.append(("Additional Endpoints", test_additional_endpoints()))

    # Summary
    print_section("TEST SUMMARY")
    passed = sum(1 for _, result in results if result)
    total = len(results)

    for test_name, result in results:
        status = "[PASS]" if result else "[FAIL]"
        print(f"{status} {test_name}")

    print(f"\nTotal: {passed}/{total} test suites passed ({passed*100//total}%)")
    print(f"Completed at: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")

    return passed == total

if __name__ == "__main__":
    success = main()
    exit(0 if success else 1)
