using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FlowSpace.Application.Common.Dtos;
using FlowSpace.Application.Services;
using FlowSpace.Domain.Entities;
using FlowSpace.Domain.Enums;
using FlowSpace.Domain.Interfaces;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FlowSpace.Application.UnitTests.Workflows
{
    public class WorkflowServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IGenericRepository<Request>> _mockRequestRepo;
        private readonly Mock<IGenericRepository<Approval>> _mockApprovalRepo;
        private readonly Mock<IGenericRepository<WorkflowRule>> _mockRuleRepo;
        private readonly Mock<IGenericRepository<User>> _mockUserRepo;
        private readonly WorkflowService _workflowService;

        public WorkflowServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockRequestRepo = new Mock<IGenericRepository<Request>>();
            _mockApprovalRepo = new Mock<IGenericRepository<Approval>>();
            _mockRuleRepo = new Mock<IGenericRepository<WorkflowRule>>();
            _mockUserRepo = new Mock<IGenericRepository<User>>();

            _mockUnitOfWork.Setup(u => u.Repository<Request>()).Returns(_mockRequestRepo.Object);
            _mockUnitOfWork.Setup(u => u.Repository<Approval>()).Returns(_mockApprovalRepo.Object);
            _mockUnitOfWork.Setup(u => u.Repository<WorkflowRule>()).Returns(_mockRuleRepo.Object);
            _mockUnitOfWork.Setup(u => u.Repository<User>()).Returns(_mockUserRepo.Object);

            _workflowService = new WorkflowService(_mockUnitOfWork.Object, _mockMapper.Object);
        }

        [Fact]
        public async Task CreateRequestAsync_ShouldCreateRequestAndGenerateApprovals()
        {
            // Arrange
            var requesterId = Guid.NewGuid();
            var input = new CreateRequestInput
            {
                Type = "Leave",
                Title = "Annual Leave 2 Days",
                Description = "Taking 2 days leave"
            };

            var rules = new List<WorkflowRule>
            {
                new WorkflowRule
                {
                    Id = Guid.NewGuid(),
                    Name = "Leave Rule",
                    RequestType = "leave",
                    SequenceSteps = "team_lead,manager",
                    IsActive = true
                }
            };

            var mockRulesQuery = rules.AsQueryable().BuildMock();
            _mockRuleRepo.Setup(r => r.GetQueryable()).Returns(mockRulesQuery);

            var addedRequests = new List<Request>();
            _mockRequestRepo.Setup(r => r.AddAsync(It.IsAny<Request>()))
                .Callback<Request>(req => addedRequests.Add(req))
                .Returns(Task.CompletedTask);

            _mockRequestRepo.Setup(r => r.GetQueryable())
                .Returns(() => addedRequests.AsQueryable().BuildMock());

            var approvers = new List<User>
            {
                new User { Id = Guid.NewGuid(), Role = "team_lead" },
                new User { Id = Guid.NewGuid(), Role = "manager" }
            };
            var mockUsersQuery = approvers.AsQueryable().BuildMock();
            _mockUserRepo.Setup(u => u.GetQueryable()).Returns(mockUsersQuery);

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _mockMapper.Setup(m => m.Map<RequestResponse>(It.IsAny<Request>()))
                .Returns((Request req) => new RequestResponse { Id = req.Id, Title = req.Title, Status = req.Status.ToString() });

            // Act
            var result = await _workflowService.CreateRequestAsync(input, requesterId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Pending", result.Status);
            _mockRequestRepo.Verify(r => r.AddAsync(It.IsAny<Request>()), Times.Once);
            Assert.NotEmpty(addedRequests);
            Assert.Equal(2, addedRequests[0].Approvals.Count);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessApprovalAsync_WhenApprovedByFinalRole_ShouldApproveRequest()
        {
            // Arrange
            var requestId = Guid.NewGuid();
            var approverId = Guid.NewGuid();
            var approvalId = Guid.NewGuid();

            var request = new Request
            {
                Id = requestId,
                Status = RequestStatus.Pending,
                Approvals = new List<Approval>
                {
                    new Approval { Id = approvalId, RequestId = requestId, Level = 1, Role = "manager", Status = ApprovalStatus.Pending }
                }
            };

            var input = new ProcessApprovalInput
            {
                Status = "approved",
                Note = "Looks good"
            };

            var mockRequestQuery = new List<Request> { request }.AsQueryable().BuildMock();
            _mockRequestRepo.Setup(r => r.GetQueryable()).Returns(mockRequestQuery);
            _mockApprovalRepo.Setup(r => r.GetByIdAsync(approvalId)).ReturnsAsync(request.Approvals.First());

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mockMapper.Setup(m => m.Map<RequestResponse>(It.IsAny<Request>()))
                .Returns(new RequestResponse { Id = requestId, Status = "Approved" });

            // Act
            var result = await _workflowService.ProcessApprovalAsync(approvalId, input, approverId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(RequestStatus.Approved, request.Status);
            Assert.Equal(ApprovalStatus.Approved, request.Approvals.First().Status);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
