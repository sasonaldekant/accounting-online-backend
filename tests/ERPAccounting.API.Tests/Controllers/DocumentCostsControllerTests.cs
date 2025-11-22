using System;
using ERPAccounting.API.Controllers;
using ERPAccounting.Application.DTOs.Costs;
using ERPAccounting.Application.Services;
using ERPAccounting.Common.Exceptions;
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
            .ReturnsAsync(new DocumentCostDto(2, 1, 10, "ZT", 100, 20, DateTime.UtcNow, null, "etag-cost"));

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

    [Fact]
    public async Task UpdateCost_WithValidIfMatch_ReturnsOkAndSetsEtag()
    {
        var rowVersion = new byte[] { 1, 2, 3, 4 };
        var encodedEtag = Convert.ToBase64String(rowVersion);
        var expectedDto = new DocumentCostDto(5, 1, 10, "ZT", 100, 20, DateTime.UtcNow, "note", "new-etag");

        var serviceMock = new Mock<IDocumentCostService>();
        serviceMock.Setup(s => s.UpdateCostAsync(1, 5, It.Is<byte[]>(b => b.AsSpan().SequenceEqual(rowVersion)), It.IsAny<UpdateDocumentCostDto>()))
            .ReturnsAsync(expectedDto);

        var controller = CreateController(serviceMock.Object);
        controller.Request.Headers["If-Match"] = $"\"{encodedEtag}\"";

        var result = await controller.UpdateCost(1, 5, new UpdateDocumentCostDto(1, 2, DateTime.UtcNow, 10, 20, "desc"));

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expectedDto, okResult.Value);
        Assert.Equal("\"new-etag\"", controller.Response.Headers["ETag"].ToString());
    }

    [Fact]
    public async Task UpdateCostItem_WithValidIfMatch_ReturnsOkAndSetsEtag()
    {
        var rowVersion = new byte[] { 9, 8, 7, 6 };
        var encodedEtag = Convert.ToBase64String(rowVersion);
        var expectedDto = new DocumentCostItemDto(3, 2, 1, 10, 100, 20, 5, "note", "etag-new");

        var serviceMock = new Mock<IDocumentCostService>();
        serviceMock
            .Setup(s => s.UpdateCostItemAsync(1, 2, 3, It.Is<byte[]>(b => b.AsSpan().SequenceEqual(rowVersion)), It.IsAny<PatchDocumentCostItemDto>()))
            .ReturnsAsync(expectedDto);

        var controller = CreateController(serviceMock.Object);
        controller.Request.Headers["If-Match"] = $"\"{encodedEtag}\"";

        var result = await controller.UpdateCostItem(1, 2, 3, new PatchDocumentCostItemDto(1, 2, 3, 4, 5));

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expectedDto, okResult.Value);
        Assert.Equal("\"etag-new\"", controller.Response.Headers["ETag"].ToString());
    }

    [Fact]
    public async Task UpdateCostItem_WhenConflict_ReturnsConflictObjectResult()
    {
        var rowVersion = new byte[] { 4, 4, 4, 4 };
        var encodedEtag = Convert.ToBase64String(rowVersion);

        var serviceMock = new Mock<IDocumentCostService>();
        serviceMock
            .Setup(s => s.UpdateCostItemAsync(1, 2, 3, It.Is<byte[]>(b => b.AsSpan().SequenceEqual(rowVersion)), It.IsAny<PatchDocumentCostItemDto>()))
            .ThrowsAsync(new ConflictException("conflict"));

        var controller = CreateController(serviceMock.Object);
        controller.Request.Headers["If-Match"] = $"\"{encodedEtag}\"";

        var result = await controller.UpdateCostItem(1, 2, 3, new PatchDocumentCostItemDto(1, 2, 3, 4, 5));

        Assert.IsType<ConflictObjectResult>(result.Result);
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
