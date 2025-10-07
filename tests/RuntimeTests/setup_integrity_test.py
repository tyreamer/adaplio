"""
Quick setup script for data integrity tests
Creates trainer, client, consent grant, template, proposal, and plan
"""

import requests
import json
import time

BASE_URL = "http://localhost:8080"

def main():
    # Step 1: Register Trainer
    timestamp = int(time.time())
    trainer_email = f"integ_trainer_{timestamp}@test.com"

    print("Step 1: Registering trainer...")
    response = requests.post(
        f"{BASE_URL}/auth/trainer/register",
        json={
            "email": trainer_email,
            "password": "SecurePass123!",
            "fullName": "Integrity Tester"
        }
    )

    if response.status_code != 200:
        print(f"Failed to register trainer: {response.status_code}")
        return None, None

    trainer_data = response.json()
    trainer_token = trainer_data["token"]
    print(f"Trainer registered successfully!")
    print(f'TRAINER_TOKEN="{trainer_token}"')

    # Step 2: Create grant code
    print("\nStep 2: Creating grant code...")
    response = requests.post(
        f"{BASE_URL}/api/trainer/grants",
        headers={"Authorization": f"Bearer {trainer_token}"},
        json={"expirationHours": 72}
    )

    if response.status_code != 200:
        print(f"Failed to create grant: {response.status_code}")
        return trainer_token, None

    grant_data = response.json()
    grant_code = grant_data["grantCode"]
    print(f"Grant code created: {grant_code}")

    # Step 3: Register Client via magic link
    client_email = f"integ_client_{timestamp}@test.com"

    print("\nStep 3: Sending magic link to client...")
    response = requests.post(
        f"{BASE_URL}/auth/client/magic-link",
        json={"phoneOrEmail": client_email}
    )

    # Check API logs for the code (email sending fails in dev)
    print("Check API logs for magic link code...")
    print("Waiting 2 seconds...")
    time.sleep(2)

    # Try with default dev code
    print("\nStep 4: Verifying client with code...")
    # The code is printed in logs, but let's try common dev codes
    for code in ["123456", "000000"]:
        response = requests.post(
            f"{BASE_URL}/auth/client/verify",
            json={"phoneOrEmail": client_email, "token": code}
        )
        if response.status_code == 200:
            break

    if response.status_code != 200:
        print(f"Failed to verify client: {response.status_code}")
        print("Please check API logs for the magic link code and run setup manually")
        return trainer_token, None

    client_data = response.json()
    client_token = client_data["token"]
    client_alias = client_data["alias"]
    print(f"Client verified: {client_alias}")
    print(f'CLIENT_TOKEN="{client_token}"')

    # Step 5: Client accepts grant
    print("\nStep 5: Client accepting grant...")
    response = requests.post(
        f"{BASE_URL}/api/client/grants/accept",
        headers={"Authorization": f"Bearer {client_token}"},
        json={"grantCode": grant_code}
    )

    if response.status_code != 200:
        print(f"Failed to accept grant: {response.status_code}")
        return trainer_token, client_token

    print("Grant accepted - consent established!")

    # Step 6: Create template
    print("\nStep 6: Creating plan template...")
    response = requests.post(
        f"{BASE_URL}/api/trainer/templates",
        headers={"Authorization": f"Bearer {trainer_token}"},
        json={
            "name": "Integrity Test Plan",
            "description": "Plan for data integrity testing",
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
        print(f"Failed to create template: {response.status_code}")
        print(response.text)
        return trainer_token, client_token

    template_data = response.json()
    template_id = template_data["id"]
    print(f"Template created: ID {template_id}")

    # Step 7: Create proposal
    print("\nStep 7: Creating plan proposal...")
    response = requests.post(
        f"{BASE_URL}/api/trainer/proposals",
        headers={"Authorization": f"Bearer {trainer_token}"},
        json={
            "clientAlias": client_alias,
            "templateId": template_id,
            "message": "Here's your personalized plan!",
            "startsOn": "2025-10-07"
        }
    )

    if response.status_code != 200:
        print(f"Failed to create proposal: {response.status_code}")
        print(response.text)
        return trainer_token, client_token

    proposal_data = response.json()
    proposal_id = proposal_data["id"]
    print(f"Proposal created: ID {proposal_id}")

    # Step 8: Client accepts proposal
    print("\nStep 8: Client accepting proposal...")
    response = requests.post(
        f"{BASE_URL}/api/client/proposals/{proposal_id}/accept",
        headers={"Authorization": f"Bearer {client_token}"},
        json={"acceptAll": True}
    )

    if response.status_code != 200:
        print(f"Failed to accept proposal: {response.status_code}")
        print(response.text)
        return trainer_token, client_token

    print("Proposal accepted - plan is now active!")

    # Step 9: Verify exercises are available
    print("\nStep 9: Loading exercise board...")
    response = requests.get(
        f"{BASE_URL}/api/client/board",
        headers={"Authorization": f"Bearer {client_token}"}
    )

    if response.status_code == 200:
        board = response.json()
        total_exercises = sum(len(exercises) for exercises in board.values())
        print(f"Board loaded successfully: {total_exercises} exercises available")
    else:
        print(f"Failed to load board: {response.status_code}")

    print("\n" + "="*80)
    print("SETUP COMPLETE!")
    print("="*80)
    print(f'\nTRAINER_TOKEN="{trainer_token}"')
    print(f'CLIENT_TOKEN="{client_token}"')
    print("\nYou can now run data integrity tests with these tokens.")

    return trainer_token, client_token

if __name__ == "__main__":
    trainer_token, client_token = main()

    if trainer_token and client_token:
        # Write tokens to file
        with open("test_tokens.txt", "w") as f:
            f.write(f'TRAINER_TOKEN="{trainer_token}"\n')
            f.write(f'CLIENT_TOKEN="{client_token}"\n')
        print("\nTokens saved to test_tokens.txt")
