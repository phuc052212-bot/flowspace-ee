using System;
using System.Threading.Tasks;

namespace FlowSpace.Application.Interfaces
{
    public interface IPermissionService
    {
        /// <summary>
        /// Checks if a user has the specified permission.
        /// </summary>
        /// <param name="userId">The user's Id.</param>
        /// <param name="permissionName">The name of the permission to check.</param>
        /// <returns>True if the user possesses the permission.</returns>
        Task<bool> HasPermissionAsync(Guid userId, string permissionName);
    }
}
