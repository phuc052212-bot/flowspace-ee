using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FlowSpace.Application.Common.Dtos;
using FlowSpace.Application.Services;
using FlowSpace.Domain.Entities;
using FlowSpace.Domain.Interfaces;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace FlowSpace.Application.UnitTests.TimeTracking
{
    public class TimeLogServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IGenericRepository<TimeLog>> _mockTimeLogRepo;
        private readonly Mock<IGenericRepository<TaskItem>> _mockTaskRepo;
        private readonly TimeLogService _timeLogService;

        public TimeLogServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockTimeLogRepo = new Mock<IGenericRepository<TimeLog>>();
            _mockTaskRepo = new Mock<IGenericRepository<TaskItem>>();

            _mockUnitOfWork.Setup(u => u.Repository<TimeLog>()).Returns(_mockTimeLogRepo.Object);
            _mockUnitOfWork.Setup(u => u.Repository<TaskItem>()).Returns(_mockTaskRepo.Object);

            _timeLogService = new TimeLogService(_mockUnitOfWork.Object, _mockMapper.Object);
        }

        [Fact]
        public async Task CreateTimeLogAsync_ShouldAccumulateLoggedHoursOnTask()
        {
            // Arrange
            var taskId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var taskEntity = new TaskItem
            {
                Id = taskId,
                Title = "Develop Feature",
                LoggedHours = 5.0m
            };

            var request = new CreateTimeLogRequest
            {
                TaskId = taskId,
                Hours = 3.5m,
                Description = "Working on unit tests"
            };

            var timeLogEntity = new TimeLog
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                UserId = userId,
                Hours = request.Hours,
                Date = DateTime.UtcNow,
                Note = request.Description
            };

            var mockTimeLogsQuery = new List<TimeLog> { timeLogEntity }.AsQueryable().BuildMock();
            _mockTimeLogRepo.Setup(r => r.GetQueryable()).Returns(mockTimeLogsQuery);

            _mockTaskRepo.Setup(r => r.GetByIdAsync(taskId)).ReturnsAsync(taskEntity);
            _mockTimeLogRepo.Setup(r => r.AddAsync(It.IsAny<TimeLog>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _mockMapper.Setup(m => m.Map<TimeLogDto>(It.IsAny<TimeLog>())).Returns(new TimeLogDto { Id = timeLogEntity.Id, Hours = 3.5m });

            // Act
            var result = await _timeLogService.CreateTimeLogAsync(request, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(8.5m, taskEntity.LoggedHours);
            _mockTimeLogRepo.Verify(r => r.AddAsync(It.IsAny<TimeLog>()), Times.Once);
            _mockTaskRepo.Verify(r => r.Update(taskEntity), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
