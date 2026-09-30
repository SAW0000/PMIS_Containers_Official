using Microsoft.EntityFrameworkCore;
using Moq;
using PMIS.Application.DTOs;
using PMIS.Application.Interfaces;
using PMIS.Application.Services;
using PMIS.Domain.Entities;
using PMIS.Domain.Enums;
using PMIS.Infrastructure.Persistence;

namespace PMIS.Tests;

public class AdminUserServiceUpdateTests
{
    private static readonly RequestActor Actor = new(
        Guid.NewGuid(), "Admin Actor", UserRole.SystemAdministrator, "127.0.0.1");

    private static (AdminUserService Service, PmisDbContext Context) BuildService()
    {
        var options = new DbContextOptionsBuilder<PmisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new PmisDbContext(options);
        var unitOfWork = new PMIS.Infrastructure.Repositories.UnitOfWork(context);

        var auditLogRepository = new Mock<IAuditLogRepository>();
        var notificationService = new Mock<INotificationService>();
        var photoUploadService = new Mock<IUserPhotoUploadService>();

        var service = new AdminUserService(
            context, unitOfWork, auditLogRepository.Object,
            notificationService.Object, photoUploadService.Object);

        return (service, context);
    }

    private static AdminUserUpdateDto ValidDto(Guid? excludeUserIdFromUniqueness = null) => new()
    {
        Name = "Updated Name",
        Email = "updated@example.com",
        Mobile = "0512345678",
        NationalId = "1234567890",
        ArabicFirstName = "أحمد",
        ArabicFatherName = "علي",
        ArabicGrandFatherName = "محمد",
        ArabicFamilyName = "الرحيلي",
        ArabicNationality = "سعودي",
        Gender = "ذكر",
        Roles = ["P3OOfficer"],
        Active = true
    };

    private static async Task<User> SeedUserAsync(PmisDbContext context, string email = "old@example.com", string nationalId = "1111111111")
    {
        var user = new User
        {
            FullName = "Old Name",
            Email = email,
            Mobile = "0511111111",
            NationalId = nationalId,
            IsActive = false
        };
        user.RoleAssignments.Add(new UserRoleAssignment { UserId = user.Id, User = user, Role = UserRole.ContractorConsultant });
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task UpdateAsync_WithFullValidPayload_WritesEveryField()
    {
        var (service, context) = BuildService();
        var user = await SeedUserAsync(context);

        var error = await service.UpdateAsync(user.Id, ValidDto(), Actor);

        Assert.Null(error);
        var reloaded = await context.Users.AsNoTracking()
            .Include(u => u.RoleAssignments)
            .FirstAsync(u => u.Id == user.Id);
        Assert.Equal("Updated Name", reloaded.FullName);
        Assert.Equal("updated@example.com", reloaded.Email);
        Assert.Equal("0512345678", reloaded.Mobile);
        Assert.Equal("1234567890", reloaded.NationalId);
        Assert.Equal("أحمد", reloaded.ArabicFirstName);
        Assert.Equal("علي", reloaded.ArabicFatherName);
        Assert.Equal("محمد", reloaded.ArabicGrandFatherName);
        Assert.Equal("الرحيلي", reloaded.ArabicFamilyName);
        Assert.Equal("سعودي", reloaded.ArabicNationality);
        Assert.Equal(Gender.Male, reloaded.Gender);
        Assert.True(reloaded.IsActive);
        Assert.Equal(UserRole.P3OOfficer, Assert.Single(reloaded.RoleAssignments).Role);
    }

    [Fact]
    public async Task UpdateAsync_WithBlankOptionalIdentityFields_StoresNullNotEmptyString()
    {
        var (service, context) = BuildService();
        var user = await SeedUserAsync(context);
        var dto = ValidDto();
        dto.ArabicFirstName = "   ";
        dto.ArabicNationality = "";
        dto.Gender = "";

        var error = await service.UpdateAsync(user.Id, dto, Actor);

        Assert.Null(error);
        var reloaded = await context.Users.AsNoTracking().FirstAsync(u => u.Id == user.Id);
        Assert.Null(reloaded.ArabicFirstName);
        Assert.Null(reloaded.ArabicNationality);
        Assert.Null(reloaded.Gender);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("5512345678")]
    [InlineData("abcdefghij")]
    public async Task UpdateAsync_RejectsInvalidMobileFormat(string invalidMobile)
    {
        var (service, context) = BuildService();
        var user = await SeedUserAsync(context);
        var dto = ValidDto();
        dto.Mobile = invalidMobile;

        var error = await service.UpdateAsync(user.Id, dto, Actor);

        Assert.NotNull(error);
        var reloaded = await context.Users.AsNoTracking().FirstAsync(u => u.Id == user.Id);
        Assert.Equal("Old Name", reloaded.FullName); 
    }

    [Theory]
    [InlineData("3234567890")] 
    [InlineData("12345")]      
    public async Task UpdateAsync_RejectsInvalidNationalIdFormat(string invalidNationalId)
    {
        var (service, context) = BuildService();
        var user = await SeedUserAsync(context);
        var dto = ValidDto();
        dto.NationalId = invalidNationalId;

        var error = await service.UpdateAsync(user.Id, dto, Actor);

        Assert.NotNull(error);
    }

    [Fact]
    public async Task UpdateAsync_RejectsGenderOutsideTheKnownDropdownValues()
    {
        var (service, context) = BuildService();
        var user = await SeedUserAsync(context);
        var dto = ValidDto();
        dto.Gender = "male"; 

        var error = await service.UpdateAsync(user.Id, dto, Actor);

        Assert.NotNull(error);
    }

    [Fact]
    public async Task UpdateAsync_RejectsNationalIdAlreadyUsedByAnotherAccount()
    {
        var (service, context) = BuildService();
        await SeedUserAsync(context, email: "other@example.com", nationalId: "1234567890");
        var user = await SeedUserAsync(context, email: "target@example.com", nationalId: "2222222222");
        var dto = ValidDto();
        dto.NationalId = "1234567890"; 

        var error = await service.UpdateAsync(user.Id, dto, Actor);

        Assert.Equal("رقم الهوية الوطنية مستخدم بالفعل", error);
    }

    [Fact]
    public async Task UpdateAsync_AllowsKeepingTheAccountsOwnUnchangedNationalId()
    {
        var (service, context) = BuildService();
        var user = await SeedUserAsync(context, nationalId: "1234567890");
        var dto = ValidDto();
        dto.NationalId = "1234567890"; 

        var error = await service.UpdateAsync(user.Id, dto, Actor);

        Assert.Null(error);
    }
}
