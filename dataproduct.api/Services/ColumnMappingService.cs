using dataproduct.api.DTOs.NMLG_Dto;
using dataproduct.api.Models;
using dataproduct.api.Repositories;

namespace dataproduct.api.Services
{
    public class ColumnMappingService
    {
        private readonly IColumnMappingRepository _repo;
        private readonly IColumnMappingNhomRepository _nhomRepo;

        public ColumnMappingService(IColumnMappingRepository repo, IColumnMappingNhomRepository nhomRepo)
        {
            _repo = repo;
            _nhomRepo = nhomRepo;
        }

        // ── Nhom ──────────────────────────────────────────────

        public async Task<List<BM_ColumnMappingNhom>> GetAllNhom(int? loCao)
        {
            return await _nhomRepo.GetAllAsync(loCao);
        }

        public async Task<BM_ColumnMappingNhom?> GetNhomById(int id)
        {
            return await _nhomRepo.GetByIdAsync(id);
        }

        public async Task<BM_ColumnMappingNhom> CreateNhom(ColumnMappingNhomCreateDto dto)
        {
            var entity = new BM_ColumnMappingNhom
            {
                LoCao = dto.LoCao,
                TenNhom = dto.TenNhom,
                ThuTu = dto.ThuTu,
                IsVisible = dto.IsVisible,
                SourceField = dto.SourceField,
                DataIndex = dto.DataIndex,
                Format = dto.Format
            };

            await _nhomRepo.AddAsync(entity);
            await _nhomRepo.SaveChangesAsync();

            return entity;
        }

        public async Task<BM_ColumnMappingNhom?> UpdateNhom(ColumnMappingNhomUpdateDto dto)
        {
            var entity = await _nhomRepo.GetByIdAsync(dto.Id);
            if (entity == null) return null;

            if (!string.IsNullOrEmpty(dto.DataIndex) && await _repo.HasChildrenAsync(dto.Id))
                throw new Exception($"Nhóm \"{entity.TenNhom}\" đã có cột con, không thể set DataIndex làm cột độc lập");

            entity.LoCao = dto.LoCao;
            entity.TenNhom = dto.TenNhom;
            entity.ThuTu = dto.ThuTu;
            entity.IsVisible = dto.IsVisible;
            entity.SourceField = dto.SourceField;
            entity.DataIndex = dto.DataIndex;
            entity.Format = dto.Format;

            await _nhomRepo.UpdateAsync(entity);
            await _nhomRepo.SaveChangesAsync();

            return entity;
        }

        public async Task<BM_ColumnMappingNhom?> ToggleVisibleNhom(int id)
        {
            var entity = await _nhomRepo.GetByIdAsync(id);
            if (entity == null) return null;

            entity.IsVisible = !entity.IsVisible;

            await _nhomRepo.UpdateAsync(entity);
            await _nhomRepo.SaveChangesAsync();

            return entity;
        }

        public async Task<bool> DeleteNhom(int id)
        {
            var entity = await _nhomRepo.GetByIdAsync(id);
            if (entity == null) return false;

            await _nhomRepo.DeleteAsync(entity);
            await _nhomRepo.SaveChangesAsync();

            return true;
        }

        // ── Column ────────────────────────────────────────────

        public async Task<List<BM_ColumnMapping>> GetAll(int? loCao)
        {
            return await _repo.GetAllAsync(loCao);
        }

        public async Task<List<ColumnDto>> GetColumns(int loCao)
        {
            var nhoms = await _nhomRepo.GetAllWithColumnsAsync(loCao);

            return nhoms.Select(nhom =>
            {
                var visibleCols = nhom.Columns.ToList();

                // Nhóm CÓ cột con → group column
                if (visibleCols.Count > 0)
                    return new ColumnDto
                    {
                        title = nhom.TenNhom,
                        children = visibleCols.Select(c => new ColumnChildDto
                        {
                            title = c.TenCot,
                            dataIndex = c.DataIndex,
                            format = c.Format
                        }).ToList()
                    };

                // Nhóm KHÔNG có cột con nhưng có DataIndex → leaf column
                if (!string.IsNullOrEmpty(nhom.DataIndex))
                    return new ColumnDto
                    {
                        title = nhom.TenNhom,
                        dataIndex = nhom.DataIndex,
                        format = nhom.Format
                    };

                return null;
            })
            .Where(x => x != null)
            .ToList()!;
        }

        public async Task<BM_ColumnMapping?> GetById(int id)
        {
            return await _repo.GetByIdAsync(id);
        }

        public async Task<BM_ColumnMapping> Create(ColumnMappingCreateDto dto)
        {
            var nhom = await _nhomRepo.GetByIdAsync(dto.NhomId);
            if (nhom == null)
                throw new Exception("Nhóm không tồn tại");

            if (!string.IsNullOrEmpty(nhom.DataIndex))
                throw new Exception($"Nhóm \"{nhom.TenNhom}\" đang dùng làm cột độc lập (có DataIndex), không thể thêm cột con");

            var exists = await _repo.ExistsDataIndexAsync(dto.NhomId, dto.DataIndex);

            if (exists)
                throw new Exception("DataIndex đã tồn tại trong nhóm này");

            var entity = new BM_ColumnMapping
            {
                NhomId = dto.NhomId,
                TenCot = dto.TenCot,
                DataIndex = dto.DataIndex,
                SourceField = dto.SourceField,
                ThuTu = dto.ThuTu,
                IsVisible = dto.IsVisible,
                Format = dto.Format
            };

            await _repo.AddAsync(entity);
            await _repo.SaveChangesAsync();

            return entity;
        }

        public async Task<BM_ColumnMapping?> Update(ColumnMappingUpdateDto dto)
        {
            var entity = await _repo.GetByIdAsync(dto.Id);
            if (entity == null) return null;

            var exists = await _repo.ExistsDataIndexAsync(dto.NhomId, dto.DataIndex, dto.Id);

            if (exists)
                throw new Exception("DataIndex bị trùng trong nhóm này");

            entity.NhomId = dto.NhomId;
            entity.TenCot = dto.TenCot;
            entity.DataIndex = dto.DataIndex;
            entity.SourceField = dto.SourceField;
            entity.ThuTu = dto.ThuTu;
            entity.IsVisible = dto.IsVisible;
            entity.Format = dto.Format;

            await _repo.UpdateAsync(entity);
            await _repo.SaveChangesAsync();

            return entity;
        }

        public async Task<bool> Delete(int id)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return false;

            await _repo.DeleteAsync(entity);
            await _repo.SaveChangesAsync();

            return true;
        }

        public async Task SaveChanges()
        {
            await _repo.SaveChangesAsync();
        }
    }
}
