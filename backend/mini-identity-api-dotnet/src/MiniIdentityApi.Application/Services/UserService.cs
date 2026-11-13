using MiniIdentityApi.Application.DTOs.Users;
using MiniIdentityApi.Application.Interfaces;
using MiniIdentityApi.Domain.Entities;

namespace MiniIdentityApi.Application.Services;

public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;

    public UserService(IUserRepository userRepository, IRoleRepository roleRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
    }

    // API responses use UserDto so credentials (hash/salt) never leave the domain.
    public List<UserDto> GetAll() => _userRepository.GetAll().Select(UserDto.FromEntity).ToList();

    public UserDto? GetById(Guid id)
    {
        var user = _userRepository.FindById(id);
        return user is null ? null : UserDto.FromEntity(user);
    }

    public void Activate(Guid id)
    {
        var user = _userRepository.FindById(id) ?? throw new KeyNotFoundException("User not found.");
        user.Activate();
    }

    public void Deactivate(Guid id)
    {
        var user = _userRepository.FindById(id) ?? throw new KeyNotFoundException("User not found.");
        user.Deactivate();
    }

    public void AssignRole(Guid userId, string roleName)
    {
        var user = _userRepository.FindById(userId) ?? throw new KeyNotFoundException("User not found.");
        var role = _roleRepository.FindByName(roleName) ?? throw new KeyNotFoundException("Role not found.");

        user.AddRole(role);
    }
}
