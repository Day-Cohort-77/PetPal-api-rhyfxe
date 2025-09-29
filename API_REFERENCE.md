# PetPal API Reference Guide

This document provides a quick reference for all available endpoints in the PetPal API.

## Authentication Endpoints

### Register a New User
- **URL**: `/auth/register`
- **Method**: `POST`
- **Request Body**:
  ```json
  {
    "email": "user@example.com",
    "password": "Password123!",
    "firstName": "John",
    "lastName": "Doe"
  }
  ```
- **Response**: User details with 201 Created status

### Login
- **URL**: `/auth/login`
- **Method**: `POST`
- **Request Body**:
  ```json
  {
    "email": "user@example.com",
    "password": "Password123!"
  }
  ```
- **Response**: User details with authentication cookie

### Logout
- **URL**: `/auth/logout`
- **Method**: `POST`
- **Authentication**: Required
- **Response**: 200 OK

## Pet Endpoints

### Get All Pets (Admin Only)
- **URL**: `/pets`
- **Method**: `GET`
- **Authentication**: Required (Admin role)
- **Response**: Array of all pets

### Get Current User's Pets
- **URL**: `/user/pets`
- **Method**: `GET`
- **Authentication**: Required
- **Response**: Array of pets owned by the current user

### Get Pet by ID
- **URL**: `/pets/{id}`
- **Method**: `GET`
- **Authentication**: Required (Pet owner or Admin)
- **Response**: Pet details

### Create New Pet
- **URL**: `/pets`
- **Method**: `POST`
- **Authentication**: Required
- **Request Body**:
  ```json
  {
    "name": "Fluffy",
    "species": "Cat",
    "breed": "Persian",
    "dateOfBirth": "2020-01-15T00:00:00Z",
    "weight": 4.5,
    "color": "White",
    "imageUrl": "https://example.com/fluffy.jpg",
    "microchipNumber": "123456789012345"
  }
  ```
- **Response**: Created pet details with 201 Created status

### Update Pet
- **URL**: `/pets/{id}`
- **Method**: `PUT`
- **Authentication**: Required (Primary pet owner or Admin)
- **Request Body**:
  ```json
  {
    "name": "Fluffy",
    "species": "Cat",
    "breed": "Persian",
    "dateOfBirth": "2020-01-15T00:00:00Z",
    "weight": 5.0,
    "color": "White",
    "imageUrl": "https://example.com/fluffy.jpg",
    "microchipNumber": "123456789012345"
  }
  ```
- **Response**: Updated pet details

### Delete Pet
- **URL**: `/pets/{id}`
- **Method**: `DELETE`
- **Authentication**: Required (Primary pet owner or Admin)
- **Response**: 204 No Content

### Add Owner to Pet
- **URL**: `/pets/{petId}/owners`
- **Method**: `POST`
- **Authentication**: Required (Primary pet owner or Admin)
- **Request Body**:
  ```json
  {
    "userProfileId": 2,
    "isPrimaryOwner": false
  }
  ```
- **Response**: Owner details with 201 Created status

### Remove Owner from Pet
- **URL**: `/pets/{petId}/owners/{ownerId}`
- **Method**: `DELETE`
- **Authentication**: Required (Primary pet owner or Admin)
- **Response**: 204 No Content

## Health Record Endpoints

### Get Pet's Health Records
- **URL**: `/pets/{petId}/healthrecords`
- **Method**: `GET`
- **Authentication**: Required (Pet owner or Admin)
- **Response**: Array of health records

### Get Health Record by ID
- **URL**: `/healthrecords/{id}`
- **Method**: `GET`
- **Authentication**: Required (Pet owner or Admin)
- **Response**: Health record details

### Create Health Record
- **URL**: `/pets/{petId}/healthrecords`
- **Method**: `POST`
- **Authentication**: Required (Pet owner or Admin)
- **Request Body**:
  ```json
  {
    "visitDate": "2023-05-15T10:30:00Z",
    "veterinarianId": 1,
    "diagnosis": "Annual checkup",
    "treatment": "Vaccinations updated",
    "notes": "Overall health is good"
  }
  ```
- **Response**: Created health record with 201 Created status

### Update Health Record
- **URL**: `/healthrecords/{id}`
- **Method**: `PUT`
- **Authentication**: Required (Pet owner or Admin)
- **Request Body**:
  ```json
  {
    "visitDate": "2023-05-15T10:30:00Z",
    "veterinarianId": 1,
    "diagnosis": "Annual checkup and dental cleaning",
    "treatment": "Vaccinations updated, teeth cleaned",
    "notes": "Overall health is good"
  }
  ```
- **Response**: Updated health record

### Delete Health Record
- **URL**: `/healthrecords/{id}`
- **Method**: `DELETE`
- **Authentication**: Required (Pet owner or Admin)
- **Response**: 204 No Content

## Medication Endpoints

### Get Pet's Medications
- **URL**: `/medications/pet/{petId}`
- **Method**: `GET`
- **Authentication**: Required (Pet owner, Admin, or Veterinarian)
- **Response**: Array of medications for the specified pet
- **Query Parameters**:
  - `activeOnly` (boolean): Filter for active medications only
  - `sortBy` (string): Sort by "startDate", "name", or "endDate"

### Get Medication by ID
- **URL**: `/medications/{id}`
- **Method**: `GET`
- **Authentication**: Required (Pet owner, Admin, or Veterinarian)
- **Response**: Single medication details

### Create Medication
- **URL**: `/medications`
- **Method**: `POST`
- **Authentication**: Required (Admin or Veterinarian roles only)
- **Request Body**:
  ```json
  {
    "petId": 1,
    "name": "Rimadyl",
    "dosage": "25mg",
    "frequency": "Twice daily with meals",
    "startDate": "2025-09-26T00:00:00Z",
    "endDate": "2025-12-26T00:00:00Z",
    "instructions": "Give with food to reduce stomach upset",
    "prescriber": "Dr. Sarah Smith, DVM"
  }
  ```
- **Response**: Created medication with 201 Created status

### Update Medication
- **URL**: `/medications/{id}`
- **Method**: `PUT`
- **Authentication**: Required (Admin or Veterinarian roles only)
- **Request Body**:
  ```json
  {
    "name": "Rimadyl",
    "dosage": "25mg",
    "frequency": "Once daily with meals",
    "startDate": "2025-09-26T00:00:00Z",
    "endDate": "2025-12-26T00:00:00Z",
    "instructions": "Reduced dosage - give with food",
    "prescriber": "Dr. Sarah Smith, DVM",
    "isActive": true
  }
  ```
- **Response**: Updated medication details

### Delete Medication
- **URL**: `/medications/{id}`
- **Method**: `DELETE`
- **Authentication**: Required (Admin or Veterinarian roles only)
- **Response**: 204 No Content

### Role-Based Access Control
- **Users**: Can view medications for their own pets only
- **Veterinarians**: Full CRUD access to all medications
- **Admins**: Full system access including medication management

## Appointment Endpoints

### Get Pet's Appointments
- **URL**: `/pets/{petId}/appointments`
- **Method**: `GET`
- **Authentication**: Required (Pet owner or Admin)
- **Response**: Array of appointments

### Get Appointment by ID
- **URL**: `/appointments/{id}`
- **Method**: `GET`
- **Authentication**: Required (Pet owner or Admin)
- **Response**: Appointment details

### Create Appointment
- **URL**: `/pets/{petId}/appointments`
- **Method**: `POST`
- **Authentication**: Required (Pet owner or Admin)
- **Request Body**:
  ```json
  {
    "appointmentDate": "2023-06-15T14:00:00Z",
    "veterinarianId": 1,
    "reason": "Annual checkup",
    "notes": "Please bring vaccination records"
  }
  ```
- **Response**: Created appointment with 201 Created status

### Update Appointment
- **URL**: `/appointments/{id}`
- **Method**: `PUT`
- **Authentication**: Required (Pet owner or Admin)
- **Request Body**:
  ```json
  {
    "appointmentDate": "2023-06-16T15:00:00Z",
    "veterinarianId": 1,
    "reason": "Annual checkup",
    "notes": "Rescheduled from previous day"
  }
  ```
- **Response**: Updated appointment

### Delete Appointment
- **URL**: `/appointments/{id}`
- **Method**: `DELETE`
- **Authentication**: Required (Pet owner or Admin)
- **Response**: 204 No Content

## Testing the API

You can test the API using tools like:

1. **Postman**: A popular API client for sending requests and viewing responses
2. **curl**: A command-line tool for making HTTP requests
3. **The included .http file**: Open `PetPal.API.http` in Visual Studio Code with the REST Client extension installed

### Example curl Commands

#### Register a User
```bash
curl -X POST https://localhost:7000/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"Password123!","firstName":"John","lastName":"Doe"}'
```

#### Login
```bash
curl -X POST https://localhost:7000/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"Password123!"}' \
  -c cookies.txt
```

#### Get Current User's Pets
```bash
curl -X GET https://localhost:7000/user/pets \
  -H "Content-Type: application/json" \
  -b cookies.txt
```

#### Create a Pet
```bash
curl -X POST https://localhost:7000/pets \
  -H "Content-Type: application/json" \
  -b cookies.txt \
  -d '{"name":"Fluffy","species":"Cat","breed":"Persian","dateOfBirth":"2020-01-15T00:00:00Z","weight":4.5,"color":"White","microchipNumber":"123456789012345"}'
```

## Status Codes

The API uses standard HTTP status codes:

- `200 OK`: The request was successful
- `201 Created`: A new resource was successfully created
- `204 No Content`: The request was successful but there is no content to return
- `400 Bad Request`: The request was malformed or invalid
- `401 Unauthorized`: Authentication is required or failed
- `403 Forbidden`: The authenticated user does not have permission
- `404 Not Found`: The requested resource was not found
- `409 Conflict`: The request conflicts with the current state of the server
- `500 Internal Server Error`: An error occurred on the server

## Authentication Notes

- The API uses cookie-based authentication
- Cookies are automatically included in requests when using a browser or tools that support cookies
- For tools that don't automatically handle cookies, you need to manually include the cookie in subsequent requests
- The cookie is named `PetPalAuth` and is set to expire after 8 hours