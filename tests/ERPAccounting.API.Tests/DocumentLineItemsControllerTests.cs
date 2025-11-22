using System.Linq;
using System.Net;
using ERPAccounting.API.Controllers;
using ERPAccounting.Common.Exceptions;
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
    public async Task UpdateItem_WhenIfMatchHeaderValid_ReturnsOkAndSetsEtag()
    {
        var rowVersion = new byte[] { 1, 2, 3, 4 };
        var etag = Convert.ToBase64String(rowVersion);
        var timestamp = DateTime.UtcNow;
        static bool MatchesRowVersion(byte[] actual, byte[] expected) => actual.SequenceEqual(expected);
        var updatedItem = new DocumentLineItemDto(
            10,
            1,
            1,
            1m,
            2m,
            null,
            null,
            null,
            null,
            null,
            null,
            false,
            true,
            null,
            etag,
            timestamp,
            timestamp,
            null,
            null);
        var serviceMock = new Mock<IDocumentLineItemService>(MockBehavior.Strict);
        serviceMock
            // Keep matcher inline so Moq captures it in the expression tree
            .Setup(s => s.UpdateAsync(1, 10, It.Is<byte[]>(b => MatchesRowVersion(b, rowVersion)), It.IsAny<PatchLineItemDto>()))
            .ReturnsAsync(updatedItem);

        var controller = CreateController(serviceMock);
        controller.Request.Headers["If-Match"] = $"\"{etag}\"";

        var result = await controller.UpdateItem(1, 10, new PatchLineItemDto());

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<DocumentLineItemDto>(okResult.Value);
        Assert.Equal(updatedItem, dto);
        Assert.Equal($"\"{etag}\"", controller.Response.Headers["ETag"].ToString());
        serviceMock.Verify(s => s.UpdateAsync(1, 10, It.Is<byte[]>(b => MatchesRowVersion(b, rowVersion)), It.IsAny<PatchLineItemDto>()), Times.Once);
    }

    [Fact]
    public async Task UpdateItem_WhenServiceThrowsConflictException_ReturnsConflictResult()
    {
        var rowVersion = new byte[] { 5, 6, 7, 8 };
        var etag = Convert.ToBase64String(rowVersion);
        static bool MatchesRowVersion(byte[] actual, byte[] expected) => actual.SequenceEqual(expected);
        var serviceMock = new Mock<IDocumentLineItemService>(MockBehavior.Strict);
        serviceMock
            // Keep matcher inline so Moq captures it in the expression tree
            .Setup(s => s.UpdateAsync(1, 10, It.Is<byte[]>(b => MatchesRowVersion(b, rowVersion)), It.IsAny<PatchLineItemDto>()))
            .ThrowsAsync(new ConflictException("Row version mismatch"));

        var controller = CreateController(serviceMock);
        controller.Request.Headers["If-Match"] = $"\"{etag}\"";

        var exception = await Assert.ThrowsAsync<ConflictException>(() => controller.UpdateItem(1, 10, new PatchLineItemDto()));

        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
        serviceMock.Verify(s => s.UpdateAsync(1, 10, It.Is<byte[]>(b => MatchesRowVersion(b, rowVersion)), It.IsAny<PatchLineItemDto>()), Times.Once);
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
