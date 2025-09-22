# Automated Testing and CI/CD Setup

This document outlines the automated testing and continuous integration setup for the PetPal API project.

## Overview

The project uses GitHub Actions to enforce code quality by running automated tests on every pull request and push to the main branches. This ensures that:

- All xUnit tests pass before code can be merged
- Code coverage is tracked and reported
- Test results are easily accessible for debugging
- Branch protection rules prevent broken code from reaching main/develop branches

## Workflow Files

### 1. `.github/workflows/run-tests.yml`
**Purpose**: Core testing workflow that runs on every PR and push
**Key Features**:
- Sets up PostgreSQL database for integration tests
- Runs database migrations
- Executes all xUnit tests
- Publishes test results
- Blocks merge if tests fail

### 2. `.github/workflows/code-quality.yml`
**Purpose**: Extended quality checks with code coverage
**Key Features**:
- Generates code coverage reports
- Uploads coverage to Codecov
- Comments coverage metrics on PRs
- Enforces minimum coverage thresholds (70% overall, 80% for changed files)

## Branch Protection Setup

To enforce test requirements, configure branch protection rules in GitHub:

### Steps to Configure Branch Protection:

1. **Navigate to Repository Settings**
   - Go to your repository on GitHub
   - Click on "Settings" tab
   - Select "Branches" from the left sidebar

2. **Add Branch Protection Rule**
   - Click "Add rule"
   - Branch name pattern: `main` (and repeat for `develop`)

3. **Configure Protection Settings**
   ```
   ☑️ Require a pull request before merging
   ☑️ Require status checks to pass before merging
       ☑️ Require branches to be up to date before merging
       ☑️ Status checks found in the last week for this repository:
           - test / test (run-tests.yml)
           - code-quality / code-quality (code-quality.yml)
   ☑️ Require conversation resolution before merging
   ☑️ Restrict pushes that create files larger than 100 MB
   ☑️ Do not allow bypassing the above settings
   ```

4. **Save the Rule**

## Test Infrastructure

### Test Project Structure
```
PetPal.Tests/
├── PetPal.Tests.csproj          # Test project configuration
├── UnitTest1.cs                 # Sample database tests
└── [Additional test files]      # Add your test files here
```

### Key Dependencies
- **xUnit**: Testing framework
- **FluentAssertions**: Readable test assertions
- **Microsoft.EntityFrameworkCore.InMemory**: In-memory database for unit tests
- **Microsoft.AspNetCore.Mvc.Testing**: Integration testing support
- **Moq**: Mocking framework
- **coverlet.collector**: Code coverage collection

### Writing Tests

#### Unit Test Example:
```csharp
[Fact]
public void CanCreatePetInDatabase()
{
    // Arrange
    using var context = GetInMemoryDbContext();
    var pet = new Pet
    {
        Name = "Buddy",
        Species = "Dog",
        // ... other properties
    };

    // Act
    context.Pets.Add(pet);
    context.SaveChanges();

    // Assert
    var savedPet = context.Pets.First();
    savedPet.Name.Should().Be("Buddy");
}
```

#### Integration Test Example:
```csharp
[Fact]
public async Task GetPets_ReturnsListOfPets()
{
    // Arrange
    var factory = new WebApplicationFactory<Program>();
    var client = factory.CreateClient();

    // Act
    var response = await client.GetAsync("/api/pets");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

## Local Development

### Running Tests Locally
```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test PetPal.Tests/PetPal.Tests.csproj

# Run tests with detailed output
dotnet test --verbosity normal
```

### Database Setup for Tests
The CI pipeline automatically sets up PostgreSQL, but for local development:

```bash
# Set up user secrets (one-time setup)
dotnet user-secrets init --project PetPal.API/PetPal.API.csproj
dotnet user-secrets set 'ConnectionStrings:PetPalDbConnectionString' 'Host=localhost;Port=5432;Username=postgres;Password=devpassword;Database=PetPal' --project PetPal.API/PetPal.API.csproj

# Run migrations
dotnet ef database update --project PetPal.API/PetPal.API.csproj
```

## Troubleshooting

### Common Issues

#### 1. Tests Fail in CI but Pass Locally
**Possible Causes**:
- Database connection differences
- Environment-specific configurations
- Race conditions in tests

**Solutions**:
- Use in-memory database for unit tests
- Ensure proper test isolation
- Check GitHub Actions logs for specific error messages

#### 2. Branch Protection Not Working
**Check**:
- Status checks are properly configured
- Workflow names match exactly
- Branch protection rules are applied to correct branches

#### 3. Coverage Reports Not Generating
**Verify**:
- `coverlet.runsettings` file is present
- Coverage collection is enabled in test command
- Paths in workflow file are correct

#### 4. Database Migration Failures
**Common Fixes**:
- Ensure PostgreSQL service is healthy before running migrations
- Check connection string format
- Verify database name matches in all configurations

### Debugging Failed Tests

1. **Check Workflow Logs**
   - Go to "Actions" tab in GitHub
   - Click on failed workflow run
   - Expand failed step to see detailed logs

2. **Download Test Artifacts**
   - Failed runs upload test results as artifacts
   - Download and open `.trx` files for detailed test results

3. **Run Tests Locally**
   - Reproduce the failing environment locally
   - Use debugger to step through failing tests

## Best Practices

### Test Writing
- Use descriptive test names that explain what is being tested
- Follow Arrange-Act-Assert pattern
- Keep tests focused and independent
- Use appropriate assertion libraries (FluentAssertions)

### Code Organization
- Separate unit tests from integration tests
- Use test categories/traits for different test types
- Mock external dependencies appropriately

### CI/CD
- Keep workflows fast (under 5 minutes when possible)
- Use parallel execution for independent test suites
- Cache dependencies to improve build times

## Monitoring and Maintenance

### Regular Tasks
- Monitor test execution times and optimize slow tests
- Review and update coverage thresholds quarterly
- Keep testing dependencies up to date
- Review failed test patterns and improve test reliability

### Metrics to Track
- Test execution time trends
- Code coverage trends
- Test failure rates
- Time to fix broken builds

## Support

For issues with the testing setup:
1. Check this documentation first
2. Review GitHub Actions logs
3. Check with team leads for configuration questions
4. Update this documentation when solutions are found

---

**Last Updated**: September 2025
**Maintained By**: Development Team