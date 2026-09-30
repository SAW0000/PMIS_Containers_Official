using PMIS.Application.Services;
using PMIS.Domain.Enums;

namespace PMIS.Tests;

public class UserPhotoReadServiceTests
{
    private static readonly Guid Self = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Other = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Theory]
    [InlineData(UserRole.ContractorConsultant)]
    [InlineData(UserRole.P3OOfficer)]
    [InlineData(UserRole.SystemAdministrator)]
    public void CanView_AllowsOwnPhoto_ForEveryRole(UserRole role)
    {
        Assert.True(UserPhotoReadService.CanView(Self, Self, role));
    }

    [Theory]
    [InlineData(UserRole.ContractorConsultant)]
    [InlineData(UserRole.P3OOfficer)]
    public void CanView_DeniesOtherUsersPhoto_ForNonAdminRoles(UserRole role)
    {
        Assert.False(UserPhotoReadService.CanView(Other, Self, role));
    }

    [Fact]
    public void CanView_AllowsOtherUsersPhoto_ForSystemAdministrator()
    {
        Assert.True(UserPhotoReadService.CanView(Other, Self, UserRole.SystemAdministrator));
    }

    [Fact]
    public void CanView_DeniesEmptyRequesterId_AgainstEmptyTarget()
    {
        Assert.False(UserPhotoReadService.CanView(Guid.Empty, Guid.Empty, UserRole.ContractorConsultant));
    }
}
