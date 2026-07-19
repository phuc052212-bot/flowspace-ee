using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using FlowSpace.Application.Common.Dtos;
using FlowSpace.Application.Interfaces;
using FlowSpace.Domain.Entities;
using FlowSpace.Domain.Enums;
using FlowSpace.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlowSpace.Application.Services
{
    public class WorkflowService : IWorkflowService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public WorkflowService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<RequestResponse>> GetRequestsAsync(Guid? requesterId = null, string? status = null)
        {
            var query = _unitOfWork.Repository<Request>().GetQueryable()
                .Include(r => r.Requester)
                .Include(r => r.Approvals).ThenInclude(a => a.Approver)
                .AsNoTracking();

            if (requesterId.HasValue)
            {
                query = query.Where(r => r.RequesterId == requesterId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<RequestStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(r => r.Status == parsedStatus);
            }

            var requests = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
            return _mapper.Map<IEnumerable<RequestResponse>>(requests);
        }

        public async Task<RequestResponse?> GetRequestByIdAsync(Guid id)
        {
            var request = await _unitOfWork.Repository<Request>().GetQueryable()
                .Include(r => r.Requester)
                .Include(r => r.Approvals).ThenInclude(a => a.Approver)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return null;
            return _mapper.Map<RequestResponse>(request);
        }

        public async Task<IEnumerable<RequestResponse>> GetPendingApprovalsForUserAsync(Guid userId, string userRole)
        {
            var query = _unitOfWork.Repository<Request>().GetQueryable()
                .Include(r => r.Requester)
                .Include(r => r.Approvals).ThenInclude(a => a.Approver)
                .Where(r => r.Status == RequestStatus.Pending)
                .AsNoTracking();

            var requests = await query.ToListAsync();

            // Filter requests where the current active approval step matches userRole or user's scope
            var filtered = requests.Where(r =>
            {
                var activeApproval = r.Approvals
                    .OrderBy(a => a.Level)
                    .FirstOrDefault(a => a.Status == ApprovalStatus.Pending);

                if (activeApproval == null) return false;

                // Role level check
                return activeApproval.Role.Equals(userRole, StringComparison.OrdinalIgnoreCase) ||
                       userRole.Equals("director", StringComparison.OrdinalIgnoreCase) ||
                       (userRole.Equals("manager", StringComparison.OrdinalIgnoreCase) && activeApproval.Role.Equals("team_lead", StringComparison.OrdinalIgnoreCase));
            }).OrderByDescending(r => r.CreatedAt);

            return _mapper.Map<IEnumerable<RequestResponse>>(filtered);
        }

        public async Task<RequestResponse> CreateRequestAsync(CreateRequestInput input, Guid requesterId)
        {
            var request = new Request
            {
                Id = Guid.NewGuid(),
                Type = Enum.TryParse<RequestType>(input.Type, true, out var type) ? type : RequestType.Leave,
                Title = input.Title,
                Description = input.Description,
                RequesterId = requesterId,
                Status = RequestStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Workflow engine: Create 4 approval levels
            var roles = new[] { "team_lead", "manager", "manager", "director" };
            for (int i = 0; i < roles.Length; i++)
            {
                request.Approvals.Add(new Approval
                {
                    Id = Guid.NewGuid(),
                    RequestId = request.Id,
                    Level = i + 1,
                    Role = roles[i],
                    Status = ApprovalStatus.Pending,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _unitOfWork.Repository<Request>().AddAsync(request);
            await _unitOfWork.SaveChangesAsync();

            return (await GetRequestByIdAsync(request.Id))!;
        }

        public async Task<RequestResponse?> ProcessApprovalAsync(Guid approvalId, ProcessApprovalInput input, Guid approverId)
        {
            var approval = await _unitOfWork.Repository<Approval>().GetByIdAsync(approvalId);
            if (approval == null) return null;

            var request = await _unitOfWork.Repository<Request>().GetQueryable()
                .Include(r => r.Approvals)
                .FirstOrDefaultAsync(r => r.Id == approval.RequestId);

            if (request == null) return null;

            var parsedStatus = Enum.TryParse<ApprovalStatus>(input.Status, true, out var status) ? status : ApprovalStatus.Approved;

            approval.ApproverId = approverId;
            approval.Status = parsedStatus;
            approval.Note = input.Note;
            approval.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Repository<Approval>().Update(approval);

            // Workflow state machine update
            if (parsedStatus == ApprovalStatus.Rejected)
            {
                request.Status = RequestStatus.Rejected;
            }
            else if (request.Approvals.All(a => a.Status == ApprovalStatus.Approved))
            {
                request.Status = RequestStatus.Approved;
            }

            request.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Repository<Request>().Update(request);

            await _unitOfWork.SaveChangesAsync();

            return await GetRequestByIdAsync(request.Id);
        }
    }
}
