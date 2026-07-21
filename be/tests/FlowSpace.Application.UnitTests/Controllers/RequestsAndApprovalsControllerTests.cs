using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FlowSpace.Api.Controllers;
using FlowSpace.Application.Common.Dtos;
using FlowSpace.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace FlowSpace.Application.UnitTests.Controllers
{
    public class RequestsAndApprovalsControllerTests
    {
        private readonly Mock<IWorkflowService> _mockWorkflowService;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly RequestsController _requestsController;
        private readonly ApprovalsController _approvalsController;

        public RequestsAndApprovalsControllerTests()
        {
            _mockWorkflowService = new Mock<IWorkflowService>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();

            _requestsController = new RequestsController(_mockWorkflowService.Object, _mockCurrentUserService.Object);
            _approvalsController = new ApprovalsController(_mockWorkflowService.Object, _mockCurrentUserService.Object);
        }

        [Fact]
        public async Task CreateRequest_HappyPath_ReturnsCreated()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockCurrentUserService.Setup(c => c.UserId).Returns(userId.ToString());

            var input = new CreateRequestInput
            {
                Type = "leave",
                Title = "Vacation",
                Description = "Taking 3 days off"
            };

            var expectedResponse = new RequestResponse
            {
                Id = Guid.NewGuid(),
                Title = input.Title,
                Status = "Pending"
            };

            _mockWorkflowService.Setup(s => s.CreateRequestAsync(input, userId))
                .ReturnsAsync(expectedResponse);

            // Act
            var actionResult = await _requestsController.Create(input);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
            var apiResponse = Assert.IsType<ApiResponse<RequestResponse>>(createdResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(expectedResponse.Id, apiResponse.Data.Id);
        }

        [Fact]
        public async Task ProcessApproval_HappyPath_ReturnsOk()
        {
            // Arrange
            var approverId = Guid.NewGuid();
            _mockCurrentUserService.Setup(c => c.UserId).Returns(approverId.ToString());
            _mockCurrentUserService.Setup(c => c.Role).Returns("manager");

            var approvalId = Guid.NewGuid();
            var input = new ProcessApprovalInput
            {
                Status = "approved",
                Note = "Approved by manager"
            };

            var expectedResponse = new RequestResponse
            {
                Id = Guid.NewGuid(),
                Status = "Approved"
            };

            _mockWorkflowService.Setup(s => s.ProcessApprovalAsync(approvalId, input, approverId))
                .ReturnsAsync(expectedResponse);

            // Act
            var actionResult = await _approvalsController.ProcessApproval(approvalId, input);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var apiResponse = Assert.IsType<ApiResponse<RequestResponse>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal("Approved", apiResponse.Data.Status);
        }

        [Fact]
        public async Task ProcessApproval_UnauthenticatedUser_ReturnsUnauthorized()
        {
            // Arrange
            _mockCurrentUserService.Setup(c => c.UserId).Returns((string?)null);

            var input = new ProcessApprovalInput { Status = "approved" };

            // Act
            var actionResult = await _approvalsController.ProcessApproval(Guid.NewGuid(), input);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(actionResult.Result);
            Assert.Equal(StatusCodes.Status401Unauthorized, objectResult.StatusCode);
        }
    }
}
