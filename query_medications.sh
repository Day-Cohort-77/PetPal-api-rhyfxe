#!/bin/bash

echo "=== PetPal Database Query: Medications and Reminders by Pet ==="
echo ""

# Get all pets with their medications and reminders
echo "Querying all pets and their medications..."
curl -s "http://localhost:5000/api/pets" | python3 -m json.tool > pets_data.json 2>/dev/null

if [ $? -eq 0 ]; then
    echo "✅ Successfully retrieved pets data"
    echo ""
    
    # Extract pet IDs and names
    python3 -c "
import json
import requests
import sys

try:
    # Read pets data
    with open('pets_data.json', 'r') as f:
        pets = json.load(f)
    
    print('📋 PETS AND THEIR MEDICATIONS:')
    print('=' * 50)
    
    for pet in pets:
        pet_id = pet['id']
        pet_name = pet['name']
        pet_species = pet['species']
        pet_breed = pet['breed']
        
        print(f'🐾 Pet: {pet_name} (ID: {pet_id})')
        print(f'   Species: {pet_species} | Breed: {pet_breed}')
        print()
        
        # Get medications for this pet
        try:
            med_response = requests.get(f'http://localhost:5000/api/medications/pet/{pet_id}')
            if med_response.status_code == 200:
                medications = med_response.json()
                
                if medications:
                    print(f'   💊 MEDICATIONS ({len(medications)}):')
                    for med in medications:
                        print(f'   • {med[\"name\"]} - {med[\"dosage\"]} - {med[\"frequency\"]}')
                        print(f'     Prescribed by: {med[\"prescriber\"]}')
                        print(f'     Start: {med[\"startDate\"][:10]} | End: {med.get(\"endDate\", \"N/A\")[:10] if med.get(\"endDate\") else \"Ongoing\"}')
                        print(f'     Active: {\"Yes\" if med[\"isActive\"] else \"No\"}')
                        
                        # Get reminders for this medication
                        try:
                            rem_response = requests.get(f'http://localhost:5000/api/medication-reminders/medication/{med[\"id\"]}')
                            if rem_response.status_code == 200:
                                reminders = rem_response.json()
                                if reminders:
                                    print(f'     ⏰ REMINDERS ({len(reminders)}):')
                                    for reminder in reminders:
                                        status = \"✅ Enabled\" if reminder[\"enabled\"] else \"❌ Disabled\"
                                        methods = \", \".join(reminder[\"notificationMethods\"])
                                        print(f'       - Time: {reminder[\"time\"]} | {status} | Methods: {methods}')
                                else:
                                    print(f'     ⏰ No reminders set')
                            elif rem_response.status_code == 403:
                                print(f'     ⏰ Access denied to reminders (need authentication)')
                            else:
                                print(f'     ⏰ Error getting reminders: {rem_response.status_code}')
                        except Exception as e:
                            print(f'     ⏰ Error fetching reminders: {str(e)}')
                        print()
                else:
                    print(f'   💊 No medications found for {pet_name}')
            elif med_response.status_code == 403:
                print(f'   💊 Access denied to medications (need authentication)')
            else:
                print(f'   💊 Error getting medications: {med_response.status_code}')
        except Exception as e:
            print(f'   💊 Error fetching medications: {str(e)}')
        
        print('-' * 50)
        print()

except Exception as e:
    print(f'❌ Error: {str(e)}')
    sys.exit(1)
"
else
    echo "❌ Failed to retrieve pets data. The API might require authentication."
    echo "Let's try to get some basic database info instead..."
    
    # Try a simple health check
    curl -s "http://localhost:5000/" || echo "API not responding"
fi

# Clean up
rm -f pets_data.json