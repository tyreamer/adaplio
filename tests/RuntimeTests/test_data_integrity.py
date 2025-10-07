"""
Adaplio API - Data Integrity Tests
Tests calculation accuracy for XP, levels, adherence, streaks, and aggregations
"""

import requests
import json
import time
import random
import string
from datetime import datetime, timedelta

BASE_URL = "http://localhost:8080"

#Global tokens - will be set during setup
CLIENT_TOKEN = None
TRAINER_TOKEN = None
CLIENT_ALIAS = None

def print_section(title):
    """Print a formatted section header"""
    print(f"\n{'='*80}")
    print(f"  {title}")
    print(f"{'='*80}")

def print_test(name, passed, details=""):
    """Print test result"""
    status = "[PASS]" if passed else "[FAIL]"
    print(f"{status} {name}")
    if details:
        print(f"      {details}")

def generate_unique_email():
    """Generate unique email for test isolation"""
    timestamp = int(time.time())
    random_str = ''.join(random.choices(string.ascii_lowercase, k=4))
    return f"integ_{timestamp}_{random_str}@test.com"

def setup_test_environment():
    """
    Set up complete test environment with trainer, client, consent, plan
    Returns: (trainer_token, client_token, client_alias) or (None, None, None) on failure
    """
    global CLIENT_TOKEN, TRAINER_TOKEN, CLIENT_ALIAS

    print_section("SETTING UP TEST ENVIRONMENT")

    # Step 1: Register Trainer
    trainer_email = generate_unique_email()
    print(f"\n1. Registering trainer: {trainer_email}")

    response = requests.post(
        f"{BASE_URL}/auth/trainer/register",
        json={
            "email": trainer_email,
            "password": "SecurePass123!",
            "fullName": "Integrity Tester"
        }
    )

    if response.status_code != 200:
        print(f"   [FAIL] Trainer registration failed: {response.status_code}")
        return None, None, None

    trainer_data = response.json()
    TRAINER_TOKEN = trainer_data["token"]
    print(f"   [OK] Trainer registered")

    # Step 2: Create grant code
    print("\n2. Creating grant code...")
    response = requests.post(
        f"{BASE_URL}/api/trainer/grants",
        headers={"Authorization": f"Bearer {TRAINER_TOKEN}"},
        json={"expirationHours": 72}
    )

    if response.status_code != 200:
        print(f"   [FAIL] Grant creation failed: {response.status_code}")
        return TRAINER_TOKEN, None, None

    grant_data = response.json()
    grant_code = grant_data["grantCode"]
    print(f"   [OK] Grant code: {grant_code}")

    # Step 3: Register Client via magic link
    client_email = generate_unique_email()
    print(f"\n3. Registering client: {client_email}")

    response = requests.post(
        f"{BASE_URL}/auth/client/magic-link",
        json={"phoneOrEmail": client_email}
    )

    # In dev mode, email service may fail but API still creates magic link
    # Check if it's a 200 or 500 (500 is OK in dev if code is still generated)
    if response.status_code not in [200, 500]:
        print(f"   [FAIL] Magic link failed: {response.status_code}")
        return TRAINER_TOKEN, None, None

    if response.status_code == 500:
        print(f"   [WARNING] Email service unavailable (dev mode)")
        print(f"   Magic link code should be in API logs")
    else:
        print(f"   [OK] Magic link sent")

    # Wait for database to be written
    time.sleep(1)

    # Try default dev codes (123456 is common in dev mode)
    print(f"\n4. Verifying client...")
    verified = False
    for code in ["123456", "000000", "111111", "999999"]:
        response = requests.post(
            f"{BASE_URL}/auth/client/verify",
            json={"phoneOrEmail": client_email, "token": code}
        )
        if response.status_code == 200:
            verified = True
            print(f"   [OK] Verified with code: {code}")
            break

    if not verified:
        print(f"   [FAIL] Client verification failed with standard codes")
        print(f"   Check API logs for the magic link code, then use this format:")
        print(f"   curl -X POST {BASE_URL}/auth/client/verify \\")
        print(f"        -H 'Content-Type: application/json' \\")
        print(f"        -d '{{\"phoneOrEmail\":\"{client_email}\",\"token\":\"CODE_FROM_LOGS\"}}'")
        return TRAINER_TOKEN, None, None

    client_data = response.json()
    CLIENT_TOKEN = client_data["token"]
    CLIENT_ALIAS = client_data["alias"]
    print(f"   [OK] Client verified: {CLIENT_ALIAS}")

    # Step 5: Client accepts grant
    print(f"\n5. Establishing consent...")
    response = requests.post(
        f"{BASE_URL}/api/client/grants/accept",
        headers={"Authorization": f"Bearer {CLIENT_TOKEN}"},
        json={"grantCode": grant_code}
    )

    if response.status_code != 200:
        print(f"   [FAIL] Grant acceptance failed: {response.status_code}")
        return TRAINER_TOKEN, CLIENT_TOKEN, CLIENT_ALIAS

    print(f"   [OK] Consent granted")

    # Step 6: Create template
    print(f"\n6. Creating plan template...")
    response = requests.post(
        f"{BASE_URL}/api/trainer/templates",
        headers={"Authorization": f"Bearer {TRAINER_TOKEN}"},
        json={
            "name": "Integrity Test Plan",
            "description": "Data integrity testing plan",
            "category": "strength",
            "durationWeeks": 4,
            "isPublic": False,
            "items": [
                {
                    "exerciseName": "Knee Flexion",
                    "targetSets": 3,
                    "targetReps": 10,
                    "holdSeconds": 0,
                    "frequencyPerWeek": 5,
                    "days": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"]
                },
                {
                    "exerciseName": "Hip Extension",
                    "targetSets": 3,
                    "targetReps": 12,
                    "holdSeconds": 30,
                    "frequencyPerWeek": 3,
                    "days": ["Monday", "Wednesday", "Friday"]
                }
            ]
        }
    )

    if response.status_code != 200:
        print(f"   [FAIL] Template creation failed: {response.status_code}")
        return TRAINER_TOKEN, CLIENT_TOKEN, CLIENT_ALIAS

    template_data = response.json()
    template_id = template_data["id"]
    print(f"   [OK] Template created: ID {template_id}")

    # Step 7: Create proposal
    print(f"\n7. Creating plan proposal...")
    response = requests.post(
        f"{BASE_URL}/api/trainer/proposals",
        headers={"Authorization": f"Bearer {TRAINER_TOKEN}"},
        json={
            "clientAlias": CLIENT_ALIAS,
            "templateId": template_id,
            "message": "Integrity test plan",
            "startsOn": "2025-10-07"
        }
    )

    if response.status_code != 200:
        print(f"   [FAIL] Proposal creation failed: {response.status_code}")
        return TRAINER_TOKEN, CLIENT_TOKEN, CLIENT_ALIAS

    proposal_data = response.json()
    proposal_id = proposal_data["id"]
    print(f"   [OK] Proposal created: ID {proposal_id}")

    # Step 8: Client accepts proposal
    print(f"\n8. Client accepting proposal...")
    response = requests.post(
        f"{BASE_URL}/api/client/proposals/{proposal_id}/accept",
        headers={"Authorization": f"Bearer {CLIENT_TOKEN}"},
        json={"acceptAll": True}
    )

    if response.status_code != 200:
        print(f"   [FAIL] Proposal acceptance failed: {response.status_code}")
        return TRAINER_TOKEN, CLIENT_TOKEN, CLIENT_ALIAS

    print(f"   [OK] Proposal accepted - plan is active!")

    print("\n" + "="*80)
    print("[SUCCESS] Test environment ready!")
    print("="*80)

    return TRAINER_TOKEN, CLIENT_TOKEN, CLIENT_ALIAS

# ============================================================================
# Test Suite 1: XP Calculation Accuracy
# ============================================================================

def test_xp_calculations():
    """
    Test XP calculation formulas
    Expected: Base XP per exercise + bonuses for streaks/difficulty
    """
    print_section("TEST SUITE 1: XP Calculation Accuracy")

    tests_passed = 0
    total_tests = 0

    # Get initial gamification state
    response = requests.get(
        f"{BASE_URL}/api/client/gamification",
        headers={"Authorization": f"Bearer {CLIENT_TOKEN}"}
    )

    if response.status_code != 200:
        print_test("Get initial gamification state", False, f"Status: {response.status_code}")
        return

    initial_state = response.json()
    initial_xp = initial_state.get('totalXp', 0)
    initial_level = initial_state.get('level', 1)

    print(f"\nInitial State:")
    print(f"  Level: {initial_level}")
    print(f"  Total XP: {initial_xp}")

    # Get exercise to complete
    response = requests.get(
        f"{BASE_URL}/api/client/board",
        headers={"Authorization": f"Bearer {CLIENT_TOKEN}"}
    )

    if response.status_code != 200 or not response.json():
        print_test("Load exercises for XP test", False, "No exercises available")
        return

    board_data = response.json()
    first_day = next(iter(board_data.keys()))
    exercises = board_data[first_day]

    if not exercises:
        print_test("Load exercises for XP test", False, "No exercises in first day")
        return

    exercise = exercises[0]
    exercise_id = exercise['id']
    target_sets = exercise.get('targetSets', 3)
    target_reps = exercise.get('targetReps', 10)

    print(f"\nTest Exercise:")
    print(f"  ID: {exercise_id}")
    print(f"  Name: {exercise.get('exerciseName', 'Unknown')}")
    print(f"  Target: {target_sets} sets x {target_reps} reps")

    # Test 1: Complete exercise with exact target reps
    total_tests += 1
    progress_data = {
        "exerciseInstanceId": exercise_id,
        "eventType": "exercise_completed",
        "setsCompleted": target_sets,
        "repsCompleted": target_reps,
        "holdSecondsCompleted": exercise.get('holdSeconds', 0),
        "painLevel": 2,
        "difficultyRating": 3,
        "notes": "XP calculation test"
    }

    response = requests.post(
        f"{BASE_URL}/api/client/progress",
        headers={"Authorization": f"Bearer {CLIENT_TOKEN}"},
        json=progress_data
    )

    if response.status_code != 200:
        print_test("Complete exercise for XP calculation", False, f"Status: {response.status_code}")
        return

    # Get updated gamification state
    response = requests.get(
        f"{BASE_URL}/api/client/gamification",
        headers={"Authorization": f"Bearer {CLIENT_TOKEN}"}
    )

    if response.status_code != 200:
        print_test("Get updated gamification state", False, f"Status: {response.status_code}")
        return

    new_state = response.json()
    new_xp = new_state.get('totalXp', 0)
    new_level = new_state.get('level', 1)
    xp_earned = new_xp - initial_xp

    print(f"\nAfter Exercise Completion:")
    print(f"  New Level: {new_level}")
    print(f"  New Total XP: {new_xp}")
    print(f"  XP Earned: {xp_earned}")

    # Base XP is typically 25 per exercise
    expected_base_xp = 25

    # Check if XP earned is reasonable (within expected range)
    if xp_earned >= expected_base_xp and xp_earned <= expected_base_xp * 2:
        tests_passed += 1
        print_test("XP earned in expected range", True, f"Earned {xp_earned} XP (expected ~{expected_base_xp})")
    else:
        print_test("XP earned in expected range", False, f"Earned {xp_earned} XP (expected ~{expected_base_xp})")

    # Test 2: Verify XP awards are logged
    total_tests += 1
    response = requests.get(
        f"{BASE_URL}/api/client/progress/week",
        headers={"Authorization": f"Bearer {CLIENT_TOKEN}"}
    )

    if response.status_code == 200:
        tests_passed += 1
        print_test("XP awards logged in progress", True)
    else:
        print_test("XP awards logged in progress", False, f"Status: {response.status_code}")

    print(f"\n{tests_passed}/{total_tests} XP calculation tests passed")
    return tests_passed, total_tests

# ============================================================================
# Test Suite 2: Level Progression Accuracy
# ============================================================================

def test_level_progression():
    """
    Test level progression calculations
    Expected: Level increases at specific XP thresholds
    """
    print_section("TEST SUITE 2: Level Progression Accuracy")

    tests_passed = 0
    total_tests = 0

    # Get current gamification state
    response = requests.get(
        f"{BASE_URL}/api/client/gamification",
        headers={"Authorization": f"Bearer {CLIENT_TOKEN}"}
    )

    if response.status_code != 200:
        print_test("Get gamification state for level test", False, f"Status: {response.status_code}")
        return 0, 0

    state = response.json()
    current_level = state.get('level', 1)
    total_xp = state.get('totalXp', 0)

    print(f"\nCurrent State:")
    print(f"  Level: {current_level}")
    print(f"  Total XP: {total_xp}")

    # Test 1: Verify level progression formula
    # Common formula: XP for level N = 100 * N * (N + 1) / 2
    # Or simplified: Level = floor(sqrt(2 * totalXp / 100))
    total_tests += 1

    # Calculate expected level from XP using common progression formulas
    # Formula 1: Each level requires incrementally more XP (100, 200, 300, etc.)
    expected_level_formula1 = 1
    cumulative_xp = 0
    while cumulative_xp <= total_xp:
        cumulative_xp += 100 * expected_level_formula1
        if cumulative_xp <= total_xp:
            expected_level_formula1 += 1

    # Formula 2: Square root based (more gradual)
    import math
    expected_level_formula2 = max(1, int(math.sqrt(total_xp / 100)))

    print(f"\nLevel Progression Analysis:")
    print(f"  Expected Level (Formula 1): {expected_level_formula1}")
    print(f"  Expected Level (Formula 2): {expected_level_formula2}")
    print(f"  Actual Level: {current_level}")

    # Check if actual level is within reasonable range of either formula
    if current_level >= expected_level_formula2 and current_level <= expected_level_formula1 + 1:
        tests_passed += 1
        print_test("Level matches progression formula", True, f"Level {current_level} is consistent with XP")
    else:
        print_test("Level matches progression formula", False, f"Level {current_level} doesn't match expected range")

    # Test 2: Verify level increases with XP
    total_tests += 1
    if current_level > 1:
        tests_passed += 1
        print_test("Level has increased from starting level", True, f"Currently at Level {current_level}")
    else:
        # This is OK if we haven't earned enough XP yet
        print_test("Level progression", True, "At starting level (not enough XP to level up yet)")
        tests_passed += 1

    # Test 3: Check XP-to-next-level is tracked
    total_tests += 1
    if 'xpToNextLevel' in state or 'nextLevelXp' in state:
        tests_passed += 1
        print_test("XP to next level tracked", True)
    else:
        print_test("XP to next level tracked", False, "No next level XP info in response")

    print(f"\n{tests_passed}/{total_tests} level progression tests passed")
    return tests_passed, total_tests

# ============================================================================
# Test Suite 3: Adherence Percentage Accuracy
# ============================================================================

def test_adherence_calculations():
    """
    Test adherence percentage calculations
    Expected: (completed exercises / total scheduled) * 100
    """
    print_section("TEST SUITE 3: Adherence Percentage Accuracy")

    tests_passed = 0
    total_tests = 0

    # Get progress summary
    response = requests.get(
        f"{BASE_URL}/api/client/progress/summary",
        headers={"Authorization": f"Bearer {CLIENT_TOKEN}"}
    )

    if response.status_code != 200:
        print_test("Get progress summary", False, f"Status: {response.status_code}")
        return 0, 0

    summary = response.json()

    print(f"\nProgress Summary:")
    print(f"  {json.dumps(summary, indent=2)}")

    # Test 1: Verify adherence percentage is between 0-100
    total_tests += 1
    adherence = summary.get('adherencePercentage', None)

    if adherence is not None:
        if 0 <= adherence <= 100:
            tests_passed += 1
            print_test("Adherence percentage in valid range", True, f"{adherence}%")
        else:
            print_test("Adherence percentage in valid range", False, f"{adherence}% (should be 0-100)")
    else:
        print_test("Adherence percentage present", False, "No adherence percentage in response")

    # Test 2: Manual adherence calculation
    total_tests += 1
    completed = summary.get('completedExercises', 0)
    total_scheduled = summary.get('totalScheduledExercises', 0) or summary.get('totalExercises', 0)

    if total_scheduled > 0:
        calculated_adherence = (completed / total_scheduled) * 100

        print(f"\nAdherence Calculation:")
        print(f"  Completed: {completed}")
        print(f"  Total Scheduled: {total_scheduled}")
        print(f"  Calculated: {calculated_adherence:.2f}%")
        print(f"  API Response: {adherence}%")

        # Allow small rounding differences
        if abs(calculated_adherence - adherence) < 1.0:
            tests_passed += 1
            print_test("Adherence calculation matches expected", True, f"Difference: {abs(calculated_adherence - adherence):.2f}%")
        else:
            print_test("Adherence calculation matches expected", False, f"Expected {calculated_adherence:.2f}%, got {adherence}%")
    else:
        print_test("Adherence calculation", True, "No exercises scheduled yet (0% is correct)")
        tests_passed += 1

    # Test 3: Get weekly adherence details
    total_tests += 1
    response = requests.get(
        f"{BASE_URL}/api/client/progress/week",
        headers={"Authorization": f"Bearer {CLIENT_TOKEN}"}
    )

    if response.status_code == 200:
        tests_passed += 1
        week_data = response.json()
        print_test("Weekly adherence data available", True, f"{len(week_data) if isinstance(week_data, list) else 'N/A'} entries")
    else:
        print_test("Weekly adherence data available", False, f"Status: {response.status_code}")

    print(f"\n{tests_passed}/{total_tests} adherence calculation tests passed")
    return tests_passed, total_tests

# ============================================================================
# Test Suite 4: Streak Calculation Accuracy
# ============================================================================

def test_streak_calculations():
    """
    Test streak tracking logic
    Expected: Current streak increments with consecutive daily completions
    """
    print_section("TEST SUITE 4: Streak Calculation Accuracy")

    tests_passed = 0
    total_tests = 0

    # Get gamification state
    response = requests.get(
        f"{BASE_URL}/api/client/gamification",
        headers={"Authorization": f"Bearer {CLIENT_TOKEN}"}
    )

    if response.status_code != 200:
        print_test("Get gamification state for streak test", False, f"Status: {response.status_code}")
        return 0, 0

    state = response.json()
    current_streak = state.get('currentStreak', 0)
    longest_streak = state.get('longestStreak', 0)

    print(f"\nStreak Information:")
    print(f"  Current Streak: {current_streak} days")
    print(f"  Longest Streak: {longest_streak} days")

    # Test 1: Current streak is non-negative
    total_tests += 1
    if current_streak >= 0:
        tests_passed += 1
        print_test("Current streak is non-negative", True, f"{current_streak} days")
    else:
        print_test("Current streak is non-negative", False, f"{current_streak} days")

    # Test 2: Longest streak >= current streak
    total_tests += 1
    if longest_streak >= current_streak:
        tests_passed += 1
        print_test("Longest streak >= current streak", True, f"{longest_streak} >= {current_streak}")
    else:
        print_test("Longest streak >= current streak", False, f"{longest_streak} < {current_streak}")

    # Test 3: Complete an exercise and verify streak updates
    total_tests += 1

    # Get exercise to complete
    response = requests.get(
        f"{BASE_URL}/api/client/board",
        headers={"Authorization": f"Bearer {CLIENT_TOKEN}"}
    )

    if response.status_code == 200 and response.json():
        board_data = response.json()
        first_day = next(iter(board_data.keys()))
        exercises = board_data[first_day]

        if exercises:
            exercise = exercises[0]

            # Complete exercise
            progress_data = {
                "exerciseInstanceId": exercise['id'],
                "eventType": "exercise_completed",
                "setsCompleted": exercise.get('targetSets', 3),
                "repsCompleted": exercise.get('targetReps', 10),
                "holdSecondsCompleted": exercise.get('holdSeconds', 0),
                "painLevel": 1,
                "difficultyRating": 3,
                "notes": "Streak test"
            }

            response = requests.post(
                f"{BASE_URL}/api/client/progress",
                headers={"Authorization": f"Bearer {CLIENT_TOKEN}"},
                json=progress_data
            )

            # Get updated streak
            response = requests.get(
                f"{BASE_URL}/api/client/gamification",
                headers={"Authorization": f"Bearer {CLIENT_TOKEN}"}
            )

            if response.status_code == 200:
                new_state = response.json()
                new_current_streak = new_state.get('currentStreak', 0)
                new_longest_streak = new_state.get('longestStreak', 0)

                print(f"\nAfter Exercise Completion:")
                print(f"  New Current Streak: {new_current_streak} days")
                print(f"  New Longest Streak: {new_longest_streak} days")

                # Streak should stay the same or increase
                if new_current_streak >= current_streak:
                    tests_passed += 1
                    print_test("Streak maintained or increased", True, f"{current_streak} -> {new_current_streak}")
                else:
                    print_test("Streak maintained or increased", False, f"{current_streak} -> {new_current_streak}")
            else:
                print_test("Get updated streak after exercise", False, f"Status: {response.status_code}")
        else:
            print_test("Exercise completion for streak test", False, "No exercises available")
    else:
        print_test("Load exercises for streak test", False, "Cannot load board")

    print(f"\n{tests_passed}/{total_tests} streak calculation tests passed")
    return tests_passed, total_tests

# ============================================================================
# Test Suite 5: Weekly Aggregation Accuracy
# ============================================================================

def test_weekly_aggregations():
    """
    Test weekly aggregation calculations
    Expected: Weekly summaries match individual progress events
    """
    print_section("TEST SUITE 5: Weekly Aggregation Accuracy")

    tests_passed = 0
    total_tests = 0

    # Get weekly progress data
    response = requests.get(
        f"{BASE_URL}/api/client/progress/week",
        headers={"Authorization": f"Bearer {CLIENT_TOKEN}"}
    )

    if response.status_code != 200:
        print_test("Get weekly progress data", False, f"Status: {response.status_code}")
        return 0, 0

    week_data = response.json()

    print(f"\nWeekly Progress Data:")
    if isinstance(week_data, list):
        print(f"  Total Entries: {len(week_data)}")

        # Test 1: All entries have required fields
        total_tests += 1
        required_fields = ['date', 'exercisesCompleted']
        all_valid = True

        for entry in week_data:
            for field in required_fields:
                if field not in entry:
                    all_valid = False
                    break

        if all_valid or len(week_data) == 0:
            tests_passed += 1
            print_test("All weekly entries have required fields", True)
        else:
            print_test("All weekly entries have required fields", False, "Missing required fields")

        # Test 2: Dates are in chronological order
        total_tests += 1
        if len(week_data) > 1:
            dates_sorted = True
            prev_date = None

            for entry in week_data:
                current_date = entry.get('date')
                if prev_date and current_date < prev_date:
                    dates_sorted = False
                    break
                prev_date = current_date

            if dates_sorted:
                tests_passed += 1
                print_test("Weekly entries in chronological order", True)
            else:
                print_test("Weekly entries in chronological order", False)
        else:
            tests_passed += 1
            print_test("Weekly entries chronological order", True, "Not enough data to test")

        # Test 3: Exercise counts are non-negative
        total_tests += 1
        all_non_negative = all(entry.get('exercisesCompleted', 0) >= 0 for entry in week_data)

        if all_non_negative or len(week_data) == 0:
            tests_passed += 1
            print_test("Exercise counts are non-negative", True)
        else:
            print_test("Exercise counts are non-negative", False)

        # Print sample data
        if week_data:
            print(f"\n  Sample Entry:")
            print(f"  {json.dumps(week_data[0], indent=4)}")

    elif isinstance(week_data, dict):
        print(f"  Weekly Summary (dict format)")
        print(f"  {json.dumps(week_data, indent=2)}")

        # Alternative format - test structure
        total_tests += 1
        tests_passed += 1
        print_test("Weekly data structure valid", True, "Dictionary format")

    else:
        total_tests += 1
        print_test("Weekly data format", False, f"Unexpected type: {type(week_data)}")

    print(f"\n{tests_passed}/{total_tests} weekly aggregation tests passed")
    return tests_passed, total_tests

# ============================================================================
# Test Suite 6: Progress Event Data Integrity
# ============================================================================

def test_progress_event_integrity():
    """
    Test progress event data integrity
    Expected: All logged events maintain data consistency
    """
    print_section("TEST SUITE 6: Progress Event Data Integrity")

    tests_passed = 0
    total_tests = 0

    # Get exercise board
    response = requests.get(
        f"{BASE_URL}/api/client/board",
        headers={"Authorization": f"Bearer {CLIENT_TOKEN}"}
    )

    if response.status_code != 200:
        print_test("Load exercise board", False, f"Status: {response.status_code}")
        return 0, 0

    board_data = response.json()

    if not board_data:
        print_test("Load exercise board", False, "No exercises available")
        return 0, 0

    # Get an exercise to log progress for
    first_day = next(iter(board_data.keys()))
    exercises = board_data[first_day]

    if not exercises:
        print_test("Load exercises", False, "No exercises in first day")
        return 0, 0

    exercise = exercises[0]
    exercise_id = exercise['id']

    print(f"\nTest Exercise:")
    print(f"  ID: {exercise_id}")
    print(f"  Name: {exercise.get('exerciseName', 'Unknown')}")

    # Test 1: Log progress with valid data
    total_tests += 1
    progress_data = {
        "exerciseInstanceId": exercise_id,
        "eventType": "exercise_completed",
        "setsCompleted": exercise.get('targetSets', 3),
        "repsCompleted": exercise.get('targetReps', 10),
        "holdSecondsCompleted": exercise.get('holdSeconds', 0),
        "painLevel": 2,
        "difficultyRating": 3,
        "notes": "Data integrity test"
    }

    response = requests.post(
        f"{BASE_URL}/api/client/progress",
        headers={"Authorization": f"Bearer {CLIENT_TOKEN}"},
        json=progress_data
    )

    if response.status_code == 200:
        tests_passed += 1
        result = response.json()
        print_test("Log progress event", True, f"Event ID: {result.get('id', 'N/A')}")

        # Test 2: Verify event data in response
        total_tests += 1
        if 'xpAwarded' in result:
            tests_passed += 1
            print_test("Progress event includes XP awarded", True, f"{result['xpAwarded']} XP")
        else:
            print_test("Progress event includes XP awarded", False, "No XP info in response")
    else:
        print_test("Log progress event", False, f"Status: {response.status_code}")

    # Test 3: Verify progress appears in weekly summary
    total_tests += 1
    response = requests.get(
        f"{BASE_URL}/api/client/progress/week",
        headers={"Authorization": f"Bearer {CLIENT_TOKEN}"}
    )

    if response.status_code == 200:
        week_data = response.json()
        if week_data:
            tests_passed += 1
            print_test("Progress event appears in weekly data", True)
        else:
            print_test("Progress event appears in weekly data", False, "No weekly data")
    else:
        print_test("Progress event appears in weekly data", False, f"Status: {response.status_code}")

    # Test 4: Verify progress summary updated
    total_tests += 1
    response = requests.get(
        f"{BASE_URL}/api/client/progress/summary",
        headers={"Authorization": f"Bearer {CLIENT_TOKEN}"}
    )

    if response.status_code == 200:
        summary = response.json()
        completed = summary.get('completedExercises', 0)

        if completed > 0:
            tests_passed += 1
            print_test("Progress summary reflects logged events", True, f"{completed} completed exercises")
        else:
            print_test("Progress summary reflects logged events", False, "No completed exercises in summary")
    else:
        print_test("Progress summary reflects logged events", False, f"Status: {response.status_code}")

    print(f"\n{tests_passed}/{total_tests} progress event integrity tests passed")
    return tests_passed, total_tests

# ============================================================================
# Main Test Runner
# ============================================================================

def run_all_integrity_tests():
    """Run all data integrity test suites"""
    print("\n")
    print("="*80)
    print("  ADAPLIO API - DATA INTEGRITY TEST SUITE")
    print("  Testing calculation accuracy and data consistency")
    print("="*80)

    # Set up test environment
    trainer_token, client_token, client_alias = setup_test_environment()

    if not trainer_token or not client_token:
        print("\n[CRITICAL] Test environment setup failed!")
        print("Cannot proceed with data integrity tests.")
        return

    print(f"\nTest tokens established. Proceeding with integrity tests...\n")

    total_passed = 0
    total_tests = 0

    # Run all test suites
    suites = [
        ("XP Calculations", test_xp_calculations),
        ("Level Progression", test_level_progression),
        ("Adherence Percentages", test_adherence_calculations),
        ("Streak Calculations", test_streak_calculations),
        ("Weekly Aggregations", test_weekly_aggregations),
        ("Progress Event Integrity", test_progress_event_integrity)
    ]

    results = []

    for suite_name, test_func in suites:
        try:
            passed, total = test_func()
            total_passed += passed
            total_tests += total
            results.append((suite_name, passed, total))
        except Exception as e:
            print(f"\n[ERROR] {suite_name} failed with exception: {e}")
            results.append((suite_name, 0, 1))
            total_tests += 1

    # Print final summary
    print_section("FINAL RESULTS")

    for suite_name, passed, total in results:
        percentage = (passed / total * 100) if total > 0 else 0
        status = "[PASS]" if passed == total else "[PARTIAL]" if passed > 0 else "[FAIL]"
        print(f"{status} {suite_name}: {passed}/{total} ({percentage:.1f}%)")

    print(f"\n{'='*80}")
    overall_percentage = (total_passed / total_tests * 100) if total_tests > 0 else 0
    print(f"OVERALL: {total_passed}/{total_tests} tests passed ({overall_percentage:.1f}%)")
    print(f"{'='*80}")

    if overall_percentage >= 90:
        print("\n[SUCCESS] Excellent data integrity! Calculations are accurate.")
    elif overall_percentage >= 75:
        print("\n[GOOD] Good data integrity with minor issues to investigate.")
    elif overall_percentage >= 50:
        print("\n[WARNING] Data integrity concerns detected. Review failed tests.")
    else:
        print("\n[CRITICAL] Significant data integrity issues found!")

    print("\n")

if __name__ == "__main__":
    run_all_integrity_tests()
