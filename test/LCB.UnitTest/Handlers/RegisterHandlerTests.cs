using System.Net;
using System.Threading.Tasks;
using LCB.Application.Commands.Register;
using LCB.Domain.Entities;
using LCB.Domain.Models.Config;
using LCB.Domain.Services;
using LCB.Infrastructure.Repositories;
using LCB.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using static LCB.UnitTest.Factories.RepositoryTestDbFactory;

namespace LCB.UnitTest.Handlers;

public class RegisterHandlerTests
{
    private static readonly PasswordPolicy StrictPolicy = new()
    {
        MinLength = 8,
        RequireUppercase = true,
        RequireLowercase = true,
        RequireDigit = true,
        RequireSpecialCharacter = true
    };

    private static readonly PasswordValidator DefaultValidator = new(StrictPolicy);

    private static (RegisterHandler handler, DbScope db) CreateRealHandler()
    {
        var db = CreateContext();
        var repo = new UserRepository(db.Context, new NullLogger<UserRepository>());
        var hasher = new PasswordHasher();
        return (new RegisterHandler(repo, hasher, DefaultValidator, new NullLogger<RegisterHandler>()), db);
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_AndPersistsHashedPassword_WhenRequestIsValid()
    {
        var (handler, db) = CreateRealHandler();
        using (db)
        {
            var repo = new UserRepository(db.Context, new NullLogger<UserRepository>());
            var hasher = new PasswordHasher();
            var request = new RegisterRequest(" Alice@Example.com ", "StrongP@ss1", "StrongP@ss1");

            var result = await handler.Handle(request);

            Assert.True(result.Success);
            Assert.NotNull(result.Data);
            Assert.Equal("Account created successfully", result.Data!.Message);
            Assert.Equal("alice@example.com", result.Data.Email);

            var persisted = await repo.GetByEmailAsync("alice@example.com");
            Assert.NotNull(persisted);
            Assert.Equal("alice@example.com", persisted!.Email);
            Assert.True(hasher.Verify("StrongP@ss1", persisted.PasswordHash));
        }
    }

    [Fact]
    public async Task Handle_ReturnsConflict_WhenEmailAlreadyExists()
    {
        var (handler, db) = CreateRealHandler();
        using (db)
        {
            var repo = new UserRepository(db.Context, new NullLogger<UserRepository>());
            var hasher = new PasswordHasher();
            await repo.CreateAsync([UserEntity.Create("alice@example.com", hasher.Hash("OldP@ss1"))]);

            var result = await handler.Handle(new RegisterRequest("alice@example.com", "StrongP@ss1", "StrongP@ss1"));

            Assert.False(result.Success);
            Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
            Assert.Equal("Email already registered", result.Error);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    [InlineData("missing-domain@")]
    public async Task Handle_ReturnsBadRequest_WhenEmailIsInvalid(string email)
    {
        var (handler, db) = CreateRealHandler();
        using (db)
        {
            var result = await handler.Handle(new RegisterRequest(email, "StrongP@ss1", "StrongP@ss1"));

            Assert.False(result.Success);
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.Equal("Invalid email format", result.Error);
        }
    }

    [Fact]
    public async Task Handle_ReturnsBadRequest_WhenConfirmPasswordIsMissing()
    {
        var (handler, db) = CreateRealHandler();
        using (db)
        {
            var result = await handler.Handle(new RegisterRequest("alice@example.com", "StrongP@ss1", ""));

            Assert.False(result.Success);
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.Equal("Confirm password is required", result.Error);
        }
    }

    [Fact]
    public async Task Handle_ReturnsBadRequest_WhenPasswordsDoNotMatch()
    {
        var (handler, db) = CreateRealHandler();
        using (db)
        {
            var result = await handler.Handle(new RegisterRequest("alice@example.com", "StrongP@ss1", "StrongP@ss2"));

            Assert.False(result.Success);
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.Equal("Passwords do not match", result.Error);
        }
    }

    [Theory]
    [InlineData("Aa1$")]         // muito curta
    [InlineData("lowercase1$")]  // sem maiúscula
    [InlineData("UPPERCASE1$")]  // sem minúscula
    [InlineData("NoNumber$$")]   // sem dígito
    [InlineData("NoSpecial11")]  // sem caractere especial
    public async Task Handle_ReturnsBadRequest_WhenPasswordViolatesPolicy(string password)
    {
        var (handler, db) = CreateRealHandler();
        using (db)
        {
            var result = await handler.Handle(new RegisterRequest("alice@example.com", password, password));

            Assert.False(result.Success);
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
            Assert.Equal(DefaultValidator.GetPasswordErrorMessage(), result.Error);
        }
    }
}
