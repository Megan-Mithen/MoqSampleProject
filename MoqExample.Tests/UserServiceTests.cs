using Moq;
using MoqExample.Core.Interfaces;
using MoqExample.Core.Models;
using MoqExample.Core.Services;

namespace MoqExample.Tests;

public class UserServiceTests
{
    // =========================================
    // Test Setup - create mocks and inject them
    // =========================================
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockEmailService = new Mock<IEmailService>();
        _userService = new UserService(_mockUserRepository.Object, _mockEmailService.Object);
    }

    // ========================================
    // Setup with Returns - basic mocking
    // ========================================
    [Fact]
    public void GetUser_WhenUserExists_ReturnsUser()
    {
        // Arrange
        var expectedUser = new User { Id = 1, Name = "John", Email = "john@example.com" };
        _mockUserRepository.Setup(r => r.GetById(1)).Returns(expectedUser);

        // Act
        var result = _userService.GetUser(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John", result.Name);
    }

    // ========================================
    // It.IsAny<T> - match any argument value
    // ========================================
    [Fact]
    public void GetUser_WhenUserDoesNotExist_ReturnsNull()
    {
        // Arrange
        _mockUserRepository.Setup(r => r.GetById(It.IsAny<int>())).Returns((User?)null);  // <-- matches any int

        // Act
        var result = _userService.GetUser(999);

        // Assert
        Assert.Null(result);
    }

    // ========================================
    // Returns with collections
    // ========================================
    [Fact]
    public void GetActiveUsers_ReturnsOnlyActiveUsers()
    {
        // Arrange
        var users = new List<User>
        {
            new() { Id = 1, Name = "Active User", IsActive = true },
            new() { Id = 2, Name = "Inactive User", IsActive = false },
            new() { Id = 3, Name = "Another Active", IsActive = true }
        };
        _mockUserRepository.Setup(r => r.GetAll()).Returns(users);

        // Act
        var result = _userService.GetActiveUsers().ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, u => Assert.True(u.IsActive));
    }
    
    // ============================================
    // Verify and Times - check methods were called
    // ============================================
    [Fact]
    public void CreateUser_WhenUserDoesNotExist_AddsUserAndSendsEmail()
    {
        // Arrange
        var newUser = new User { Id = 1, Name = "Jane", Email = "jane@example.com" };
        _mockUserRepository.Setup(r => r.Exists(1)).Returns(false);
        _mockEmailService.Setup(e => e.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        // Act
        var result = _userService.CreateUser(newUser);

        // Assert
        Assert.True(result);
        _mockUserRepository.Verify(r => r.Add(newUser), Times.Once);  // Verify that Add(newUser) was called exactly once.
        _mockEmailService.Verify(e => e.SendEmail(
            "jane@example.com",
            "Welcome!",
            It.Is<string>(s => s.Contains("Jane"))), Times.Once);  // Verify that SendEmail was called exactly once, and includes expected strings.
    }

    // ==========================================
    // Times.Never - verify method was NOT called
    // ==========================================
    [Fact]
    public void CreateUser_WhenUserAlreadyExists_ReturnsFalseAndDoesNotSendEmail()
    {
        // Arrange
        var existingUser = new User { Id = 1, Name = "Jane", Email = "jane@example.com" };
        _mockUserRepository.Setup(r => r.Exists(1)).Returns(true);

        // Act
        var result = _userService.CreateUser(existingUser);

        // Assert
        Assert.False(result);
        _mockUserRepository.Verify(r => r.Add(It.IsAny<User>()), Times.Never);  // <-- never called
        _mockEmailService.Verify(e => e.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never); // <-- never called
    }

    // ========================================
    // It.Is<T> - match argument with predicate
    // ========================================
    [Fact]
    public void DeactivateUser_WhenUserExists_UpdatesUserAndReturnsTrue()
    {
        // Arrange
        var user = new User { Id = 1, Name = "John", IsActive = true };
        _mockUserRepository.Setup(r => r.GetById(1)).Returns(user);

        // Act
        var result = _userService.DeactivateUser(1);

        // Assert
        Assert.True(result);
        Assert.False(user.IsActive);
        _mockUserRepository.Verify(r => r.Update(It.Is<User>(u => u.Id == 1 && !u.IsActive)), Times.Once);  // <-- predicate -- Verify that Update was called with a User where the Id is 1 and IsActive is false
    }

    [Fact]
    public void DeactivateUser_WhenUserDoesNotExist_ReturnsFalse()
    {
        // Arrange
        _mockUserRepository.Setup(r => r.GetById(It.IsAny<int>())).Returns((User?)null);

        // Act
        var result = _userService.DeactivateUser(999);

        // Assert
        Assert.False(result);
        _mockUserRepository.Verify(r => r.Update(It.IsAny<User>()), Times.Never);
    }

    // ========================================
    // ReturnsAsync - for async methods
    // ========================================
    [Fact]
    public async Task CreateUserAsync_SendsEmailAsync()
    {
        // Arrange
        var newUser = new User { Id = 1, Name = "Jane", Email = "jane@example.com" };
        _mockUserRepository.Setup(r => r.Exists(1)).Returns(false);
        _mockEmailService
            .Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);  // <-- async version of Returns

        // Act
        var result = await _userService.CreateUserAsync(newUser);

        // Assert
        Assert.True(result);
        _mockEmailService.Verify(e => e.SendEmailAsync("jane@example.com", "Welcome!", It.IsAny<string>()), Times.Once);
    }

    // ========================================
    // Throws - setup to throw exceptions
    // ========================================
    [Fact]
    public void GetUser_WhenRepositoryThrows_ExceptionPropagates()
    {
        // Arrange
        _mockUserRepository
            .Setup(r => r.GetById(It.IsAny<int>()))
            .Throws(new InvalidOperationException("Database connection failed"));

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => _userService.GetUser(1));
        Assert.Equal("Database connection failed", ex.Message);
    }

    [Fact]
    public async Task CreateUserAsync_WhenEmailThrows_ExceptionPropagates()
    {
        // Arrange
        var newUser = new User { Id = 1, Name = "Jane", Email = "jane@example.com" };
        _mockUserRepository.Setup(r => r.Exists(1)).Returns(false);
        _mockEmailService
            .Setup(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("SMTP server unavailable"));  // <-- async version of Throws

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => _userService.CreateUserAsync(newUser));
        Assert.Equal("SMTP server unavailable", ex.Message);
    }

    // ===========================================================
    // Callback - run code when mock is called / capture arguments
    // ===========================================================
    [Fact]
    public void CreateUser_Callback_CapturesEmailArguments()
    {
        // Arrange
        var newUser = new User { Id = 1, Name = "Jane", Email = "jane@example.com" };
        _mockUserRepository.Setup(r => r.Exists(1)).Returns(false);

        string? capturedTo = null;
        string? capturedSubject = null;
        string? capturedBody = null;

        _mockEmailService
            .Setup(e => e.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string, string>((to, subject, body) =>
            {
                // Capture the arguments for inspection
                capturedTo = to;
                capturedSubject = subject;
                capturedBody = body;
            })
            .Returns(true);

        // Act
        _userService.CreateUser(newUser);

        // Assert - inspect what was actually passed
        Assert.Equal("jane@example.com", capturedTo);
        Assert.Equal("Welcome!", capturedSubject);
        Assert.Contains("Jane", capturedBody);
    }

    // ====================================================
    // SetupSequence - return different values on each call
    // ====================================================
    [Fact]
    public void GetUserRetryCount_SetupSequence_SimulatesRetry()
    {
        // Arrange - first two calls return null, third call returns user
        _mockUserRepository
            .SetupSequence(r => r.GetById(1))
            .Returns((User?)null)      // 1st call
            .Returns((User?)null)      // 2nd call
            .Returns(new User { Id = 1, Name = "John" });  // 3rd call

        // Act
        var attempts = _userService.GetUserRetryCount(1, maxRetries: 5);

        // Assert - should succeed on 3rd attempt
        Assert.Equal(3, attempts);
    }

    // ==============================================
    // MockBehavior.Strict - fail on unexpected calls
    // ==============================================
    [Fact]
    public void StrictMock_ThrowsOnUnexpectedCall()
    {
        // Arrange - strict mock requires ALL calls to be setup
        var strictMock = new Mock<IUserRepository>(MockBehavior.Strict);
        var emailMock = new Mock<IEmailService>();
        var service = new UserService(strictMock.Object, emailMock.Object);

        // No setup for GetById - strict mock will throw
        // Test confirms that calling GetUser without setting up the repository will fail — which proves GetUser  depends on GetById.

        // Act & Assert
        Assert.Throws<Moq.MockException>(() => service.GetUser(1));
    }

    [Fact]
    public void StrictMock_WorksWhenAllCallsSetup()
    {
        // Arrange
        var strictMock = new Mock<IUserRepository>(MockBehavior.Strict);
        var emailMock = new Mock<IEmailService>();
        strictMock.Setup(r => r.GetById(1)).Returns(new User { Id = 1, Name = "John" });
        var service = new UserService(strictMock.Object, emailMock.Object);

        // Act
        var result = service.GetUser(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John", result.Name);
    }

    // ============================================
    // Verifiable - mark setups that must be called
    // ============================================
    [Fact]
    public void Verifiable_EnsuresSetupWasCalled()
    {
        // Arrange
        var newUser = new User { Id = 1, Name = "Jane", Email = "jane@example.com" };
        _mockUserRepository.Setup(r => r.Exists(1)).Returns(false);
        _mockUserRepository.Setup(r => r.Add(It.IsAny<User>())).Verifiable();  // <-- mark as required
        _mockEmailService.Setup(e => e.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true)
            .Verifiable();  // <-- mark as required

        // Act
        _userService.CreateUser(newUser);

        // Assert - verify ALL setups marked as Verifiable were called
        _mockUserRepository.Verify();
        _mockEmailService.Verify();
    }
}
