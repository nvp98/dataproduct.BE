using dataproduct.api.Models;
using dataproduct.api.Repositories;
using dataproduct.api.DTOs;
using System;

namespace dataproduct.api.Services
{
    public class CtdPhoiNongService
    {
        private readonly ICtdPhoiNongRepository _repo;

        public CtdPhoiNongService(ICtdPhoiNongRepository repo)
        {
            _repo = repo;
        }

        public Task<IEnumerable<CtdPhoiNong>> GetAllAsync(DateOnly? NgaySX, int? Ca, string? Kip)
        {
            return _repo.GetAllAsync(NgaySX, Ca, Kip);
        }

        public Task<CtdPhoiNong?> GetByIdAsync(int id)
        {
            return _repo.GetByIdAsync(id);
        }

        public async Task<CtdPhoiNong> CreateAsync(CtdPhoiNong entity)
        {
            await _repo.AddAsync(entity);
            return entity;
        }

        public async Task<IEnumerable<CtdPhoiNong>> CreateListAsync(List<CtdPhoiNong> entities)
        {
            await _repo.AddListAsync(entities);
            return entities;
        }

        public async Task<bool> UpdateAsync(int id, CtdPhoiNong entity)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return false;
            entity.Id = id;
            await _repo.UpdateAsync(entity);
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return false;
            await _repo.DeleteAsync(id);
            return true;
        }

        public Task<int> UpdateStatusesAsync(List<CtdPhoiNongStatusUpdate> items)
        {
            return _repo.UpdateStatusRangeAsync(items);
        }

        public Task<IEnumerable<CtdPhoiNong>> GetByPhieuIdAsync(Guid phieuId)
        {
            return _repo.GetByPhieuIdAsync(phieuId);
        }

        public Task<(int Created, int Updated)> UpsertListAsync(List<CtdPhoiNong> entities)
        {
            return _repo.UpsertListAsync(entities);
        }
    }
}