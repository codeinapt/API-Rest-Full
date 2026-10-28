using Inventory.Application.Common.Exceptions;
using Inventory.Application.Common.Interfaces;
using Inventory.Domain.Common;

namespace Inventory.Application.Common.Authorization;

public sealed class AreaAccessPolicy(ICurrentUser currentUser)
{
    public void EnsureCanSupervise(string areaCode)
    {
        if (!currentUser.AreaCodes.Contains(areaCode, StringComparer.Ordinal))
        {
            throw new ForbiddenException(ErrorCode.AreaAccessDenied, areaCode);
        }
    }
}
