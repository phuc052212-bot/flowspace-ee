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
using TaskStatus = FlowSpace.Domain.Enums.TaskStatus;

namespace FlowSpace.Application.UnitTests.Tasks
{
    public class TaskServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IGenericRepository<TaskItem>> _mockRepo;
        private readonly TaskService _taskService;

        public TaskServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockRepo = new Mock<IGenericRepository<TaskItem>>();

            _mockUnitOfWork.Setup(u => u.Repository<TaskItem>()).Returns(_mockRepo.Object);
            _taskService = new TaskService(_mockUnitOfWork.Object, _mockMapper.Object);
        }

        [Fact]
        public async Task CreateTaskAsync_ShouldSetDefaultsAndSave()
        {
            // Arrange
            var createdById = Guid.NewGuid();
            var request = new CreateTaskRequest
            {
                Code = "T-101",
                Title = "New Task",
                ProjectId = Guid.NewGuid(),
                Priority = "High"
            };

            var addedTasks = new List<TaskItem>();
            _mockRepo.Setup(r => r.AddAsync(It.IsAny<TaskItem>()))
                .Callback<TaskItem>(t => addedTasks.Add(t))
                .Returns(Task.CompletedTask);

            _mockRepo.Setup(r => r.GetQueryable())
                .Returns(() => addedTasks.AsQueryable().BuildMock());

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _mockMapper.Setup(m => m.Map<TaskResponse>(It.IsAny<TaskItem>()))
                .Returns((TaskItem src) => new TaskResponse { Id = src.Id, Code = src.Code, Title = src.Title, Status = src.Status.ToString() });

            // Act
            var result = await _taskService.CreateTaskAsync(request, createdById);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Task", result.Title);
            _mockRepo.Verify(r => r.AddAsync(It.IsAny<TaskItem>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task UpdateStatusAsync_WhenSetToDone_ShouldSetCompletedAt()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var taskEntity = new TaskItem
            {
                Id = taskId,
                Title = "Test Task",
                Status = TaskStatus.InProgress,
                CompletedAt = null
            };

            var mockQuery = new List<TaskItem> { taskEntity }.AsQueryable().BuildMock();
            _mockRepo.Setup(r => r.GetQueryable()).Returns(mockQuery);
            _mockRepo.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(taskEntity);
            _mockRepo.Setup(r => r.Update(It.IsAny<TaskItem>()));
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mockMapper.Setup(m => m.Map<TaskResponse>(It.IsAny<TaskItem>())).Returns(new TaskResponse { Id = taskId, Status = "Done" });

            // Act
            var result = await _taskService.UpdateTaskStatusAsync(taskId, "Done");

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(taskEntity.CompletedAt);
            Assert.Equal(TaskStatus.Done, taskEntity.Status);
            _mockRepo.Verify(r => r.Update(taskEntity), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }
    }
}
