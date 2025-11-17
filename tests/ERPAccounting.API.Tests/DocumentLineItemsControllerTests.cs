using ERPAccounting.API.Controllers;
using ERPAccounting.Application.DTOs;
using ERPAccounting.Application.Services;
using ERPAccounting.Common.Constants;
using ERPAccounting.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ERPAccounting.API.Tests;

public class DocumentLineItemsControllerTests
{
    [Fact]
    public async Task UpdateItem_WhenIfMatchHeaderMissing_ReturnsProblemDetails()
    {
        var serviceMock = new Mock<IDocumentLineItemService>(MockBehavior.Strict);
        var controller = CreateController(serviceMock);

        var result = await controller.UpdateItem(1, 10, new PatchLineItemDto());

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetailsDto>(badRequest.Value);
        Assert.Equal(ErrorMessages.MissingIfMatchHeader, problem.Detail);
        Assert.Equal(ErrorCodes.MissingIfMatchHeader, problem.ErrorCode);
        serviceMock.Verify(s => s.UpdateAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<byte[]>(), It.IsAny<PatchLineItemDto>()), Times.Never);
    }

    [Fact]
    public async Task UpdateItem_WhenIfMatchHeaderInvalid_ReturnsProblemDetails()
    {
        var serviceMock = new Mock<IDocumentLineItemService>(MockBehavior.Strict);
        var controller = CreateController(serviceMock);
        controller.Request.Headers["If-Match"] = "not-base64";

        var result = await controller.UpdateItem(1, 10, new PatchLineItemDto());

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problem = Assert.IsType<ProblemDetailsDto>(badRequest.Value);
        Assert.Equal(ErrorMessages.InvalidIfMatchHeader, problem.Detail);
        Assert.Equal(ErrorCodes.MissingIfMatchHeader, problem.ErrorCode);
        serviceMock.Verify(s => s.UpdateAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<byte[]>(), It.IsAny<PatchLineItemDto>()), Times.Never);
    }

    [Fact]
    public async Task GetItem_WhenServiceReturnsNull_ThrowsNotFoundException()
    {
        var serviceMock = new Mock<IDocumentLineItemService>();
        serviceMock
            .Setup(s => s.GetAsync(1, 99))
            .ReturnsAsync((DocumentLineItemDto?)null);

        var controller = CreateController(serviceMock);

        await Assert.ThrowsAsync<ERPAccounting.Common.Exceptions.NotFoundException>(() => controller.GetItem(1, 99));
    }

    private static DocumentLineItemsController CreateController(Mock<IDocumentLineItemService>? serviceMock = null)
    {
        serviceMock ??= new Mock<IDocumentLineItemService>();
        var loggerMock = new Mock<ILogger<DocumentLineItemsController>>();
        var controller = new DocumentLineItemsController(serviceMock.Object, loggerMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        controller.ControllerContext.HttpContext.TraceIdentifier = Guid.NewGuid().ToString();
        return controller;
    }
}
