using ERPAccounting.API.Controllers;
using ERPAccounting.Application.DTOs.Costs;
using ERPAccounting.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ERPAccounting.API.Tests.Controllers;

public class DocumentCostsControllerTests
{
    [Fact]
    public async Task GetCost_AttachesEtagHeader()
    {
        var serviceMock = new Mock<IDocumentCostService>();
        serviceMock.Setup(s => s.GetCostByIdAsync(1, 2))
            .ReturnsAsync(new DocumentCostDto(2, 1, 10, "Transport", 100, 20, DateTime.UtcNow, null, "etag-cost"));

        var controller = CreateController(serviceMock.Object);

        var result = await controller.GetCost(1, 2);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
        Assert.Equal("\"etag-cost\"", controller.Response.Headers["ETag"].ToString());
    }

    [Fact]
    public async Task PatchCostItem_WithoutIfMatch_ReturnsBadRequest()
    {
        var controller = CreateController(new Mock<IDocumentCostService>().Object);
        var response = await controller.UpdateCostItem(1, 2, 3, new PatchDocumentCostItemDto(null, null, null, null, null));

        Assert.IsType<BadRequestObjectResult>(response.Result);
    }

    private static DocumentCostsController CreateController(IDocumentCostService service)
    {
        var controller = new DocumentCostsController(service, NullLogger<DocumentCostsController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        return controller;
    }
}
