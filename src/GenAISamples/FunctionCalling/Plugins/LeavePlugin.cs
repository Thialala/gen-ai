using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace FunctionCalling.Plugins;

public class LeavePlugin
{
    [KernelFunction, Description("Retrieve current user infos.")]
    public UserInfos GetCurrentUserInfos()
    {
        return new UserInfos()
        {
            Id = Guid.NewGuid(),
            FullName = "John Doe",
            Email = "john.doe@mail.com",
            ManagerEmail = "jane.doe@mail.com",
            ManagerFullName = "Jane Doe "
        };
    }

    [KernelFunction, Description("Request leave for current user.")]
    public string RequestLeave(
               [Description("Current user id")] string userId,
               [Description("User full name")] string fullName,
               [Description("User email")] string email,
               [Description("User manager email")] string managerEmail,
               [Description("Leave start date")] string startDate,
               [Description("Leave end date")] string endDate,
               CancellationToken cancellationToken = default)
    {
        return $"Leave request for {userId}:{fullName}:{email} from {startDate} to {endDate} has been sent to {managerEmail}";
    }
}

public class UserInfos
{
    public Guid Id { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string ManagerEmail { get; set; }
    public string ManagerFullName { get; set; }
}

