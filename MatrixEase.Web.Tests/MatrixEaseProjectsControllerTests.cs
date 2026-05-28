using MatrixEase.Manga.Manga;
using MatrixEase.Manga.Manga.Serialization;
using MatrixEase.Manga.Utility;
using MatrixEase.Web;
using MatrixEase.Web.Common;
using MatrixEase.Web.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MatrixEase.Web.Tests;

public class MatrixEaseProjectsControllerTests
{
    [Fact]
    public void ProjectsRequiresSupabaseIdentity()
    {
        var controller = CreateController(new RequestContextAccessor());

        IActionResult result = controller.Projects();

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void ProjectsReturnsSupabaseUserCatalog()
    {
        string root = Path.Combine(Path.GetTempPath(), "matrixease-web-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            SecretProtector.Configure("test-protection-key-0123456789");
            MangaRoot.SetRootFolder(root);

            string userId = "supabase-user-123";
            var mangaInfo = new MangaInfo(
                "sales.csv",
                "Sales Matrix",
                1,
                1,
                500,
                true,
                true,
                true,
                true,
                "",
                "csv",
                new Dictionary<string, string>());
            mangaInfo.SetCounts(42, 4, 168);

            MangaState.SetUserMangaCatalog("unused-access-token", userId, "joe@example.com", MangaAuthType.Email);
            MangaState.SaveManga(userId, mangaInfo, new DataManga());

            var requestContextAccessor = new RequestContextAccessor();
            var controller = CreateController(requestContextAccessor);
            requestContextAccessor.SetSupabaseIdentity(controller.HttpContext, new SupabaseIdentity
            {
                ExternalIdentity = userId,
                EmailAddress = "joe@example.com"
            });

            IActionResult result = controller.Projects();

            var ok = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<MatrixEaseProjectsResponse>(ok.Value);
            MatrixEaseProjectDto project = Assert.Single(response.Projects);

            Assert.True(response.Success);
            Assert.Equal("Sales Matrix", project.Name);
            Assert.Equal("sales.csv", project.OriginalName);
            Assert.Equal("csv", project.SheetType);
            Assert.Equal(500, project.MaxRows);
            Assert.Equal(42, project.TotalRows);
            Assert.Equal("Complete", project.Status);
            Assert.False(project.IsPending);
            Assert.False(string.IsNullOrWhiteSpace(project.ProjectId));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    [Fact]
    public void ProjectDataRejectsProjectOwnedByDifferentSupabaseUser()
    {
        string root = Path.Combine(Path.GetTempPath(), "matrixease-web-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            SecretProtector.Configure("test-protection-key-0123456789");
            MangaRoot.SetRootFolder(root);

            string ownerUserId = "supabase-owner-123";
            var mangaInfo = new MangaInfo(
                "sales.csv",
                "Sales Matrix",
                1,
                1,
                500,
                true,
                true,
                true,
                true,
                "",
                "csv",
                new Dictionary<string, string>());

            MangaState.SetUserMangaCatalog("unused-access-token", ownerUserId, "owner@example.com", MangaAuthType.Email);
            MangaState.SaveManga(ownerUserId, mangaInfo, new DataManga());

            var ownerAccessor = new RequestContextAccessor();
            var ownerController = CreateController(ownerAccessor);
            ownerAccessor.SetSupabaseIdentity(ownerController.HttpContext, new SupabaseIdentity
            {
                ExternalIdentity = ownerUserId,
                EmailAddress = "owner@example.com"
            });
            var ownerProjects = Assert.IsType<OkObjectResult>(ownerController.Projects());
            var ownerResponse = Assert.IsType<MatrixEaseProjectsResponse>(ownerProjects.Value);
            string projectId = Assert.Single(ownerResponse.Projects).ProjectId;

            var otherAccessor = new RequestContextAccessor();
            var otherController = CreateController(otherAccessor);
            otherAccessor.SetSupabaseIdentity(otherController.HttpContext, new SupabaseIdentity
            {
                ExternalIdentity = "supabase-other-456",
                EmailAddress = "other@example.com"
            });

            object result = otherController.Get(projectId);

            Assert.IsType<UnauthorizedObjectResult>(result);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    private static MatrixEaseController CreateController(RequestContextAccessor requestContextAccessor)
    {
        var controller = new MatrixEaseController(
            NullLogger<MatrixEaseController>.Instance,
            new BackgroundTaskQueue(),
            requestContextAccessor);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }
}
