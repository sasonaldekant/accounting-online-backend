using ERPAccounting.API.Controllers;
using ERPAccounting.Application.DTOs;
using ERPAccounting.Application.DTOs.Documents;
using ERPAccounting.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ERPAccounting.API.Tests.Controllers;

public class DocumentsControllerTests
{
    [Fact]
    public async Task GetDocument_AttachesEtagHeader()
    {
        var serviceMock = new Mock<IDocumentService>();
        serviceMock.Setup(s => s.GetDocumentByIdAsync(1))
            .ReturnsAsync(new DocumentDto(1, "UR-1", DateTime.UtcNow, 1, 1, null, 0, 0, null, false, false, DateTime.UtcNow, DateTime.UtcNow, null, null, "etag-value"));

        var controller = CreateController(serviceMock.Object);

        var result = await controller.GetDocument(1);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(okResult.Value);
        Assert.Equal("\"etag-value\"", controller.Response.Headers["ETag"].ToString());
    }

    [Fact]
    public async Task UpdateDocument_WithoutIfMatch_ReturnsBadRequest()
    {
        var controller = CreateController(new Mock<IDocumentService>().Object);

        var response = await controller.UpdateDocument(1, new UpdateDocumentDto("UR-1", DateTime.UtcNow, 1, 1, null, 0, 0, null, false, false));

        Assert.IsType<BadRequestObjectResult>(response.Result);
    }

    private static DocumentsController CreateController(IDocumentService service)
    {
        var controller = new DocumentsController(service, NullLogger<DocumentsController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        return controller;
    }
}
