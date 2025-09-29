# PetPal API Reference for React Frontend

## Base Configuration

**Base URL:** `http://localhost:5000`
**Authentication:** Cookie-based (httpOnly cookie named "PetPalAuth")
**CORS:** Configured for `http://localhost:3000` and `http://localhost:5173`

### Important Frontend Setup
```javascript
// All API calls must include credentials to send cookies
const apiCall = async (url, options = {}) => {
  const response = await fetch(`http://localhost:5000${url}`, {
    ...options,
    credentials: 'include', // CRITICAL: This sends cookies
    headers: {
      'Content-Type': 'application/json',
      ...options.headers,
    },
  });
  return response;
};
```

## Test Credentials

| Email | Password | Role | Pets |
|-------|----------|------|------|
| `admin@petpal.com` | `Admin123!` | Admin | All pets access |
| `user@petpal.com` | `User123!` | User | Buddy (5 medications), Whiskers (4 medications) |
| `jane@petpal.com` | `User123!` | User | Max, Luna, Tweety |
| `frontend@petpal.com` | `Test123!` | User | TestPet (10 medications, comprehensive data) |

## Authentication Endpoints

### POST /auth/register
Register a new user account.
```json
{
  "firstName": "John",
  "lastName": "Doe", 
  "email": "john@example.com",
  "password": "Password123!",
  "address": {
    "street": "123 Main St",
    "city": "Anytown",
    "state": "CA",
    "zipCode": "12345"
  },
  "phone": "555-1234",
  "preferredContactMethod": "Email"
}
```
**Response:** `201 Created` with user profile + automatic login

### POST /auth/login
Login with email and password.
```json
{
  "email": "user@petpal.com",
  "password": "User123!"
}
```
**Response:** `200 OK` with user profile + sets authentication cookie

### POST /auth/logout
Logout current user.
**Response:** `204 No Content` + clears authentication cookie

### GET /auth/me
Get current authenticated user's profile.
**Auth Required:** Yes
**Response:**
```json
{
  "id": 1,
  "firstName": "Sample",
  "lastName": "User", 
  "email": "user@petpal.com",
  "address": {
    "street": "456 User Ave",
    "city": "Sample City",
    "state": "NY",
    "zipCode": "10001"
  },
  "phone": "555-987-6543",
  "preferredContactMethod": "Email",
  "roles": ["User"],
  "createdAt": "2024-01-01T00:00:00Z",
  "updatedAt": "2024-01-01T00:00:00Z"
}
```

### PUT /auth/profile
Update current user's profile.
**Auth Required:** Yes
```json
{
  "firstName": "Updated",
  "lastName": "Name",
  "address": {
    "street": "New Address",
    "city": "New City", 
    "state": "CA",
    "zipCode": "90210"
  },
  "phone": "555-NEW-PHONE",
  "preferredContactMethod": "Phone"
}
```

## Pet Management Endpoints

### GET /user/pets
Get all pets owned by the current user.
**Auth Required:** Yes
**Response:** Array of pet objects
```json
[
  {
    "id": 1,
    "name": "Buddy",
    "species": "Dog",
    "breed": "Golden Retriever",
    "dateOfBirth": "2021-09-25T00:00:00Z",
    "weight": 70.5,
    "color": "Golden",
    "imageUrl": "https://example.com/buddy.jpg",
    "microchipNumber": "CHIP123456",
    "owners": [
      {
        "userProfileId": 1,
        "isPrimaryOwner": true,
        "userProfile": {
          "firstName": "Sample",
          "lastName": "User"
        }
      }
    ]
  }
]
```

### GET /pets/{id}
Get specific pet by ID (owner, vet, or admin access).
**Auth Required:** Yes
**Response:** Single pet object

### POST /pets
Create a new pet.
**Auth Required:** Yes
```json
{
  "name": "Rex",
  "species": "Dog",
  "breed": "German Shepherd",
  "dateOfBirth": "2022-01-15T00:00:00Z",
  "weight": 65.0,
  "color": "Black and Tan",
  "imageUrl": "https://example.com/rex.jpg",
  "microchipNumber": "CHIP999888"
}
```

### PUT /pets/{id}
Update existing pet.
**Auth Required:** Yes (owner only)

### DELETE /pets/{id}
Delete a pet.
**Auth Required:** Yes (owner only)

## Medication Endpoints

### GET /medications/pet/{petId}
Get all medications for a specific pet.
**Auth Required:** Yes
**Query Parameters:**
- `isActive` (optional): Filter by active status (true/false)
- `sortBy` (optional): Sort field (name, startDate, etc.)
- `sortOrder` (optional): asc/desc

**Response:**
```json
[
  {
    "id": 1,
    "petId": 1,
    "name": "Heartworm Prevention",
    "dosage": "1 tablet",
    "frequency": "Monthly",
    "startDate": "2024-06-25T00:00:00Z",
    "endDate": null,
    "instructions": "Give with food",
    "prescriber": "John Smith",
    "isActive": true
  }
]
```

### GET /medications/{id}
Get specific medication by ID.

### POST /medications
Add new medication for a pet.
```json
{
  "name": "Antibiotic",
  "dosage": "250mg",
  "frequency": "Twice daily", 
  "startDate": "2024-09-25T00:00:00Z",
  "endDate": "2024-10-05T00:00:00Z",
  "instructions": "Give with food",
  "prescriber": "Dr. Smith",
  "isActive": true
}
```

### PUT /medications/{id}
Update existing medication.

### DELETE /medications/{id}
Delete a medication.

## Appointment Endpoints

### GET /user/appointments
Get all appointments for current user's pets.
**Auth Required:** Yes
**Response:**
```json
[
  {
    "id": 1,
    "petId": 1,
    "veterinarianId": 1,
    "appointmentDate": "2024-10-02T00:00:00Z",
    "appointmentTime": "14:30:00",
    "appointmentType": "Check-up",
    "notes": "Annual check-up",
    "status": "Scheduled",
    "pet": {
      "id": 1,
      "name": "Buddy"
    },
    "veterinarian": {
      "id": 1,
      "firstName": "John",
      "lastName": "Smith",
      "specialty": "General"
    }
  }
]
```

### GET /pets/{petId}/appointments
Get appointments for specific pet.

### GET /appointments/{id}
Get specific appointment by ID.

### POST /appointments
Create new appointment.
```json
{
  "petId": 1,
  "veterinarianId": 1,
  "appointmentDate": "2024-10-15T00:00:00Z",
  "appointmentTime": "10:00:00",
  "appointmentType": "Vaccination",
  "notes": "Rabies booster",
  "status": "Scheduled"
}
```

### PUT /appointments/{id}
Update existing appointment.

### DELETE /appointments/{id}
Cancel/delete appointment.

## Health Records Endpoints

### GET /pets/{petId}/healthrecords
Get all health records for a pet.
**Auth Required:** Yes
**Response:**
```json
[
  {
    "id": 1,
    "petId": 1,
    "recordType": "Vaccination",
    "description": "Rabies Vaccination",
    "recordDate": "2024-03-25T00:00:00Z",
    "notes": "Annual vaccination completed",
    "attachments": "",
    "veterinarianId": 1,
    "veterinarian": {
      "firstName": "John",
      "lastName": "Smith"
    }
  }
]
```

### GET /healthrecords/{id}
Get specific health record.

### POST /pets/{petId}/healthrecords
Create new health record.
```json
{
  "recordType": "Check-up",
  "description": "Annual Physical",
  "recordDate": "2024-09-25T00:00:00Z",
  "notes": "All vitals normal",
  "attachments": "",
  "veterinarianId": 1
}
```

### PUT /healthrecords/{id}
Update health record.

### DELETE /healthrecords/{id}
Delete health record.

## Veterinarian Endpoints

### GET /veterinarians
Get all veterinarians.
**Response:**
```json
[
  {
    "id": 1,
    "firstName": "John",
    "lastName": "Smith",
    "email": "john.smith@petpal.com",
    "phone": "555-111-2222",
    "specialty": "General",
    "clinicName": "PetPal Clinic",
    "address": "123 Main St",
    "licenseNumber": "VET12345"
  }
]
```

## Sample Data Structure

### user@petpal.com pets:
- **Buddy** (Golden Retriever): 5 medications including heartworm prevention, joint supplements, antibiotics (completed), allergy medication (active), eye drops (future)
- **Whiskers** (Siamese): 4 medications including flea treatment, deworming, dental supplement, thyroid medication (completed)

### frontend@petpal.com pets:
- **TestPet** (Border Collie): 10+ comprehensive medications with various statuses, frequencies, and types for thorough frontend testing

## Error Responses

| Status Code | Meaning |
|-------------|---------|
| `200` | Success |
| `201` | Created |
| `204` | No Content |
| `400` | Bad Request |
| `401` | Unauthorized (not logged in) |
| `403` | Forbidden (logged in but no access) |
| `404` | Not Found |
| `409` | Conflict (duplicate email on registration) |

## Cookie Authentication Notes

1. **All requests must include `credentials: 'include'`**
2. Cookie is automatically set on login/register
3. Cookie is automatically cleared on logout
4. Cookie expires after 8 hours of inactivity
5. Cookie uses `SameSite=None` and `Secure=true` for cross-origin requests
6. No need to manually handle tokens - browser handles cookie automatically

## Development Server
- API runs on: `http://localhost:5000`
- Database: PostgreSQL with comprehensive test data
- All endpoints return JSON responses
- CORS enabled for React dev servers on ports 3000 and 5173