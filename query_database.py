#!/usr/bin/env python3
import os
import json
import subprocess
import sys

def get_connection_string():
    """Get the connection string from user secrets"""
    try:
        result = subprocess.run([
            'dotnet', 'user-secrets', 'list', 
            '--project', 'PetPal.API/PetPal.API.csproj'
        ], capture_output=True, text=True, cwd='.')
        
        for line in result.stdout.split('\n'):
            if 'ConnectionStrings:PetPalDbConnectionString' in line:
                # Extract the connection string value
                return line.split(' = ', 1)[1].strip()
        
        # Fallback to default local connection
        return "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=PetPal"
    except:
        return "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=PetPal"

def parse_connection_string(conn_str):
    """Parse PostgreSQL connection string to get individual components"""
    parts = {}
    for part in conn_str.split(';'):
        if '=' in part:
            key, value = part.split('=', 1)
            parts[key.strip().lower()] = value.strip()
    return parts

def query_medications_and_reminders():
    """Query medications and their reminders using psql"""
    conn_str = get_connection_string()
    conn_parts = parse_connection_string(conn_str)
    
    query = """
    SELECT 
        p."Name" as pet_name,
        p."Species" as pet_species,
        m."Name" as medication_name,
        m."Dosage" as dosage,
        m."Frequency" as frequency,
        m."StartDate" as start_date,
        m."EndDate" as end_date,
        mr."ReminderTime" as reminder_time,
        mr."IsEnabled" as reminder_enabled,
        mr."NotificationMethods" as notification_methods
    FROM "Pets" p
    LEFT JOIN "Medications" m ON p."Id" = m."PetId"
    LEFT JOIN "MedicationReminders" mr ON m."Id" = mr."MedicationId"
    ORDER BY p."Name", m."Name", mr."ReminderTime";
    """
    
    try:
        # Build psql command
        host = conn_parts.get('host', 'localhost')
        port = conn_parts.get('port', '5432')
        username = conn_parts.get('username', 'postgres')
        database = conn_parts.get('database', 'PetPal')
        
        # Set PGPASSWORD environment variable if password is provided
        env = os.environ.copy()
        if 'password' in conn_parts:
            env['PGPASSWORD'] = conn_parts['password']
        
        cmd = [
            'psql',
            f'-h', host,
            f'-p', port,
            f'-U', username,
            f'-d', database,
            '-c', query,
            '--csv'
        ]
        
        result = subprocess.run(cmd, capture_output=True, text=True, env=env)
        
        if result.returncode == 0:
            print("Medications and Reminders by Pet:")
            print("=" * 80)
            
            lines = result.stdout.strip().split('\n')
            if len(lines) > 1:
                headers = lines[0].split(',')
                print(f"{'Pet Name':<15} {'Species':<10} {'Medication':<20} {'Dosage':<15} {'Reminder Time':<12} {'Enabled':<8}")
                print("-" * 80)
                
                for line in lines[1:]:
                    if line.strip():
                        values = line.split(',')
                        if len(values) >= 10:
                            pet_name = values[0] or 'N/A'
                            species = values[1] or 'N/A'
                            medication = values[2] or 'No medications'
                            dosage = values[3] or 'N/A'
                            reminder_time = values[7] or 'No reminders'
                            enabled = values[8] or 'N/A'
                            
                            print(f"{pet_name:<15} {species:<10} {medication:<20} {dosage:<15} {reminder_time:<12} {enabled:<8}")
            else:
                print("No data found in the database.")
                
        else:
            print(f"Error querying database: {result.stderr}")
            return False
            
    except Exception as e:
        print(f"Error: {e}")
        return False
    
    return True

if __name__ == "__main__":
    print("Querying PetPal database for medications and reminders...")
    success = query_medications_and_reminders()
    if not success:
        print("\nNote: Make sure PostgreSQL is running and the database exists.")
        print("You may need to adjust the connection string in user secrets.")