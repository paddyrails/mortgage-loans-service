# Loans Service

Microservice for managing loans in the Mortgage Application system.

## ⚠️ Dependencies

This service depends on:
- **Customer Service** (http://localhost:5001) - For borrower information
- **Property Service** (http://localhost:5002) - For collateral information

**Start dependent services first!**

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/loans | Get all loans |
| GET | /api/loans/{id} | Get loan by ID (with enriched data) |
| GET | /api/loans/number/{number} | Get loan by number |
| GET | /api/loans/customer/{id} | Get loans by customer |
| GET | /api/loans/property/{id} | Get loans by property |
| POST | /api/loans | Create loan |
| PUT | /api/loans/{id} | Update loan |
| POST | /api/loans/{id}/fund | Fund a loan |
| DELETE | /api/loans/{id} | Cancel loan |
| GET | /api/loans/{id}/balance | Get loan balance |
| GET | /api/loans/{id}/schedule | Get amortization schedule |

## Running

```bash
# Start dependencies first!
cd ../mortgage-customer-service/src/Customer.API && dotnet run &
cd ../mortgage-property-service/src/Property.API && dotnet run &

# Then start Loans service
cd src/Loans.API
dotnet run
```

Swagger UI: http://localhost:5003/swagger

## Configuration

Service URLs can be configured in `appsettings.json`:
```json
{
  "ServiceUrls": {
    "CustomerService": "http://localhost:5001",
    "PropertyService": "http://localhost:5002"
  }
}
```
