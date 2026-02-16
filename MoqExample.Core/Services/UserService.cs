using MoqExample.Core.Interfaces;
using MoqExample.Core.Models;

namespace MoqExample.Core.Services;

public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailService _emailService;

    public UserService(IUserRepository userRepository, IEmailService emailService)
    {
        _userRepository = userRepository;
        _emailService = emailService;
    }

    public User? GetUser(int id)
    {
        return _userRepository.GetById(id);
    }

    public IEnumerable<User> GetActiveUsers()
    {
        return _userRepository.GetAll().Where(u => u.IsActive);
    }

    public bool CreateUser(User user)
    {
        if (_userRepository.Exists(user.Id))
        {
            return false;
        }

        _userRepository.Add(user);
        _emailService.SendEmail(user.Email, "Welcome!", $"Hello {user.Name}, welcome to our platform!");
        return true;
    }

    public bool DeactivateUser(int id)
    {
        var user = _userRepository.GetById(id);
        if (user == null)
        {
            return false;
        }

        user.IsActive = false;
        _userRepository.Update(user);
        return true;
    }

    public async Task<bool> CreateUserAsync(User user)
    {
        if (_userRepository.Exists(user.Id))
        {
            return false;
        }

        _userRepository.Add(user);
        await _emailService.SendEmailAsync(user.Email, "Welcome!", $"Hello {user.Name}, welcome to our platform!");
        return true;
    }

    public User GetUserOrThrow(int id)
    {
        var user = _userRepository.GetById(id);
        return user ?? throw new InvalidOperationException($"User {id} not found");
    }

    public int GetUserRetryCount(int id, int maxRetries)
    {
        int attempts = 0;
        while (attempts < maxRetries)
        {
            attempts++;
            var user = _userRepository.GetById(id);
            if (user != null)
                return attempts;
        }
        return attempts;
    }
}
