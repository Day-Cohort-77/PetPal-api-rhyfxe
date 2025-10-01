# Medication Reminder API Documentation

This document describes the new medication reminder endpoints that allow users to set up reminders for pet medications and track administration history.

## Overview

The medication reminder system provides the following functionality:
- Set up multiple daily reminders for medications
- Configure notification methods (app, email, SMS)
- Log medication administration with status (administered, skipped, missed)
- View medication administration history
- Enable/disable reminders

## API Endpoints

### 1. Set Medication Reminders

**POST** `/api/medication-reminders/`

Sets up reminders for a specific medication. This replaces any existing reminders for the medication.

#### Request Body
```json
{
  "medicationId": 1,
  "petId": 2,
  "enabled": true,
  "times": ["08:00", "20:00"],
  "notificationMethods": ["app", "email"]
}
```

#### Response
```json
{
  "success": true,
  "message": "Medication reminders set successfully",
  "reminders": [
    {
      "id": 1,
      "medicationId": 1,
      "petId": 2,
      "time": "08:00",
      "enabled": true,
      "notificationMethods": ["app", "email"]
    },
    {
      "id": 2,
      "medicationId": 1,
      "petId": 2,
      "time": "20:00",
      "enabled": true,
      "notificationMethods": ["app", "email"]
    }
  ]
}
```

### 2. Get Medication Reminders

**GET** `/api/medication-reminders/medication/{medicationId}`

Retrieves all reminders for a specific medication.

#### Response
```json
[
  {
    "id": 1,
    "medicationId": 1,
    "petId": 2,
    "time": "08:00",
    "enabled": true,
    "notificationMethods": ["app", "email"]
  },
  {
    "id": 2,
    "medicationId": 1,
    "petId": 2,
    "time": "20:00",
    "enabled": true,
    "notificationMethods": ["app", "email"]
  }
]
```

### 3. Get Pet Reminders

**GET** `/api/medication-reminders/pet/{petId}`

Retrieves all medication reminders for a specific pet.

#### Response
```json
[
  {
    "id": 1,
    "medicationId": 1,
    "petId": 2,
    "time": "08:00",
    "enabled": true,
    "notificationMethods": ["app", "email"]
  },
  {
    "id": 3,
    "medicationId": 5,
    "petId": 2,
    "time": "12:00",
    "enabled": true,
    "notificationMethods": ["app"]
  }
]
```

### 4. Update Reminder

**PUT** `/api/medication-reminders/{reminderId}`

Updates a specific reminder.

#### Request Body
```json
{
  "id": 1,
  "medicationId": 1,
  "petId": 2,
  "time": "08:30",
  "enabled": false,
  "notificationMethods": ["email"]
}
```

#### Response
```json
{
  "id": 1,
  "medicationId": 1,
  "petId": 2,
  "time": "08:30",
  "enabled": false,
  "notificationMethods": ["email"]
}
```

### 5. Delete Reminder

**DELETE** `/api/medication-reminders/{reminderId}`

Deletes a specific reminder.

#### Response
```json
{
  "success": true,
  "message": "Reminder deleted successfully"
}
```

### 6. Log Medication Administration

**POST** `/api/medication-reminders/log`

Logs when a medication was administered, skipped, or missed.

#### Request Body
```json
{
  "medicationId": 1,
  "petId": 2,
  "reminderId": 1,
  "status": "administered",
  "administeredAt": "2025-10-01T08:05:00Z",
  "notes": "Pet took medication easily"
}
```

#### Response
```json
{
  "success": true,
  "message": "Medication administration logged",
  "log": {
    "id": 15,
    "medicationId": 1,
    "petId": 2,
    "reminderId": 1,
    "status": "administered",
    "administeredAt": "2025-10-01T08:05:00Z",
    "notes": "Pet took medication easily",
    "loggedAt": "2025-10-01T08:05:30Z"
  }
}
```

### 7. Get Medication History

**GET** `/api/medication-reminders/history/medication/{medicationId}`

Retrieves administration history for a specific medication.

#### Response
```json
{
  "medicationId": 1,
  "medicationName": "Rimadyl",
  "petId": 2,
  "petName": "Buddy",
  "administrationHistory": [
    {
      "id": 15,
      "medicationId": 1,
      "petId": 2,
      "reminderId": 1,
      "status": "administered",
      "administeredAt": "2025-10-01T08:05:00Z",
      "notes": "Pet took medication easily",
      "loggedAt": "2025-10-01T08:05:30Z"
    },
    {
      "id": 14,
      "medicationId": 1,
      "petId": 2,
      "reminderId": 2,
      "status": "skipped",
      "administeredAt": "2025-09-30T20:00:00Z",
      "notes": "Pet refused medication",
      "loggedAt": "2025-09-30T20:02:00Z"
    }
  ]
}
```

### 8. Get Pet Medication History

**GET** `/api/medication-reminders/history/pet/{petId}`

Retrieves all medication administration history for a pet.

#### Response
```json
[
  {
    "medicationId": 1,
    "medicationName": "Rimadyl",
    "petId": 2,
    "petName": "Buddy",
    "administrationHistory": [
      {
        "id": 15,
        "medicationId": 1,
        "petId": 2,
        "reminderId": 1,
        "status": "administered",
        "administeredAt": "2025-10-01T08:05:00Z",
        "notes": "Pet took medication easily",
        "loggedAt": "2025-10-01T08:05:30Z"
      }
    ]
  },
  {
    "medicationId": 5,
    "medicationName": "Antibiotics",
    "petId": 2,
    "petName": "Buddy",
    "administrationHistory": [
      {
        "id": 20,
        "medicationId": 5,
        "petId": 2,
        "reminderId": 3,
        "status": "administered",
        "administeredAt": "2025-10-01T12:00:00Z",
        "notes": null,
        "loggedAt": "2025-10-01T12:01:00Z"
      }
    ]
  }
]
```

## Data Models

### MedicationReminder
- `id`: Unique identifier
- `medicationId`: ID of the associated medication
- `petId`: ID of the associated pet
- `time`: Reminder time in HH:mm format (e.g., "08:00")
- `enabled`: Whether the reminder is active
- `notificationMethods`: Array of notification methods ["app", "email", "sms"]

### MedicationAdministrationLog
- `id`: Unique identifier
- `medicationId`: ID of the associated medication
- `petId`: ID of the associated pet
- `reminderId`: ID of the associated reminder (optional)
- `status`: "administered", "skipped", or "missed"
- `administeredAt`: When the medication was supposed to be given
- `notes`: Optional notes about the administration
- `loggedAt`: When the log entry was created

## Status Values

### Administration Status
- `administered`: Medication was given successfully
- `skipped`: Medication was intentionally not given
- `missed`: Medication was not given (unintentional)

### Notification Methods
- `app`: Push notification in the mobile/web app
- `email`: Email notification
- `sms`: SMS text message (if implemented)

## Authentication

All endpoints require authentication. Include the authentication cookie or token in your requests.

## Error Responses

### 401 Unauthorized
```json
{
  "title": "Unauthorized",
  "status": 401
}
```

### 403 Forbidden
```json
{
  "title": "Forbidden",
  "status": 403
}
```

### 404 Not Found
```json
{
  "title": "Not Found",
  "status": 404
}
```

### 400 Bad Request
```json
{
  "title": "Bad Request",
  "status": 400,
  "errors": {
    "medicationId": ["The medicationId field is required."]
  }
}
```

## Usage Examples

### Frontend Implementation Example

```javascript
// Set up morning and evening reminders for a medication
async function setMedicationReminders(medicationId, petId) {
  const response = await fetch('/api/medication-reminders/', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    credentials: 'include', // Include cookies for authentication
    body: JSON.stringify({
      medicationId: medicationId,
      petId: petId,
      enabled: true,
      times: ['08:00', '20:00'],
      notificationMethods: ['app', 'email']
    })
  });
  
  if (response.ok) {
    const result = await response.json();
    console.log('Reminders set:', result.reminders);
  } else {
    console.error('Failed to set reminders');
  }
}

// Log that medication was given
async function logMedicationGiven(medicationId, petId, reminderId) {
  const response = await fetch('/api/medication-reminders/log', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    credentials: 'include',
    body: JSON.stringify({
      medicationId: medicationId,
      petId: petId,
      reminderId: reminderId,
      status: 'administered',
      administeredAt: new Date().toISOString(),
      notes: 'Given with breakfast'
    })
  });
  
  if (response.ok) {
    const result = await response.json();
    console.log('Administration logged:', result.log);
  } else {
    console.error('Failed to log administration');
  }
}

// Get medication history for display
async function getMedicationHistory(medicationId) {
  const response = await fetch(`/api/medication-reminders/history/medication/${medicationId}`, {
    credentials: 'include'
  });
  
  if (response.ok) {
    const history = await response.json();
    displayMedicationHistory(history);
  } else {
    console.error('Failed to get medication history');
  }
}
```

This API provides comprehensive medication reminder and tracking functionality that integrates seamlessly with the existing PetPal API architecture.