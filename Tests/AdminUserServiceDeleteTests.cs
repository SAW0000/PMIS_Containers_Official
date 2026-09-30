using Microsoft.EntityFrameworkCore;
using Moq;
using PMIS.Application.Interfaces;
using PMIS.Application.Services;
using PMIS.Domain.Entities;
using PMIS.Domain.Enums;
using PMIS.Infrastructure.Persistence;

namespace PMIS.Tests;

public class AdminUserServiceDeleteTests
{
    private static readonly RequestActor Actor = new(
        Guid.NewGuid(), "Admin Actor", UserRole.SystemAdministrator, "127.0.0.1");

    private static (AdminUserService Service, PmisDbContext Context, Mock<IUserPhotoUploadService> PhotoUploadService) BuildService()
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

        return (service, context, photoUploadService);
    }

    [Fact]
    public async Task DeleteAsync_WithProfilePhoto_DeletesTheSupersededObjectAndClearsTheReference()
    {
        var (service, context, photoUploadService) = BuildService();

        var user = new User
        {
            FullName = "Test User",
            Email = "test@example.com",
            NationalId = "1010101010",
            PhotoFileRef = "users/some-id/photo.png",
            IsActive = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var error = await service.DeleteAsync(user.Id, Actor);

        Assert.Null(error);

        var reloaded = await context.Users.AsNoTracking().FirstAsync(u => u.Id == user.Id);
        Assert.True(reloaded.DeletedAccount);
        Assert.False(reloaded.IsActive);
        Assert.Null(reloaded.PhotoFileRef);

        photoUploadService.Verify(
            p => p.DeleteSupersededAsync("users/some-id/photo.png", user.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNoProfilePhoto_NeverCallsDeleteSuperseded()
    {
        var (service, context, photoUploadService) = BuildService();

        var user = new User
        {
            FullName = "Test User",
            Email = "test2@example.com",
            NationalId = "2020202020",
            PhotoFileRef = null,
            IsActive = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var error = await service.DeleteAsync(user.Id, Actor);

        Assert.Null(error);
        photoUploadService.Verify(
            p => p.DeleteSupersededAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
