using SWP391_CareSkin_BE.DTOS.Requests;
using SWP391_CareSkin_BE.DTOS.Responses;
using SWP391_CareSkin_BE.Exceptions;
using SWP391_CareSkin_BE.Mappers;
using SWP391_CareSkin_BE.Repositories.Interfaces;
using SWP391_CareSkin_BE.Services.Interfaces;

namespace SWP391_CareSkin_BE.Services.Implementations
{
    public class SkinTypeService : ISkinTypeService
    {
        private readonly ISkinTypeRepository _skinTypeRepository;

        public SkinTypeService(ISkinTypeRepository skinTypeRepository)
        {
            _skinTypeRepository = skinTypeRepository;
        }

        public async Task<List<SkinTypeDTO>> GetAllAsync()
        {
            var skinTypes = await _skinTypeRepository.GetAllAsync();
            return skinTypes.Select(SkinTypeMapper.ToDTO).ToList();
        }

        public async Task<SkinTypeDTO> GetByIdAsync(int id)
        {
            var skinType = await _skinTypeRepository.GetByIdAsync(id);
            if (skinType == null)
            {
                throw new NotFoundException($"SkinType with ID {id} not found");
            }

            return SkinTypeMapper.ToDTO(skinType);
        }

        public async Task<SkinTypeDTO> CreateAsync(SkinTypeCreateRequestDTO request)
        {
            // Kiểm tra xem SkinType với tên này đã tồn tại và đang active chưa
            var existingSkinType = await _skinTypeRepository.GetByNameAsync(request.TypeName);
            
            if (existingSkinType != null && existingSkinType.IsActive)
            {
                throw new Exception($"Loại da với tên '{request.TypeName}' đã tồn tại.");
            }

            var skinType = SkinTypeMapper.ToEntity(request);
            await _skinTypeRepository.CreateAsync(skinType);

            return SkinTypeMapper.ToDTO(skinType);
        }

        public async Task<SkinTypeDTO> UpdateAsync(int id, SkinTypeUpdateRequestDTO request)
        {
            var skinType = await _skinTypeRepository.GetByIdAsync(id);
            if (skinType == null)
            {
                throw new NotFoundException($"SkinType with ID {id} not found");
            }

            SkinTypeMapper.UpdateEntity(skinType, request);
            await _skinTypeRepository.UpdateAsync(skinType);

            return SkinTypeMapper.ToDTO(skinType);
        }

        public async Task DeleteAsync(int id)
        {
            var skinType = await _skinTypeRepository.GetByIdAsync(id);
            if (skinType == null)
            {
                throw new NotFoundException($"SkinType with ID {id} not found");
            }

            // Implement soft delete by setting IsActive to false
            skinType.IsActive = false;
            await _skinTypeRepository.UpdateAsync(skinType);
        }

        public async Task<List<SkinTypeDTO>> GetActiveSkinTypesAsync()
        {
            var skinTypes = await _skinTypeRepository.GetActiveSkinTypesAsync();
            return skinTypes.Select(SkinTypeMapper.ToDTO).ToList();
        }

        public async Task<List<SkinTypeDTO>> GetInactiveSkinTypesAsync()
        {
            var skinTypes = await _skinTypeRepository.GetInactiveSkinTypesAsync();
            return skinTypes.Select(SkinTypeMapper.ToDTO).ToList();
        }
    }
}
