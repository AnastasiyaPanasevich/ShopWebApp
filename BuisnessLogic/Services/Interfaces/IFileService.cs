using Microsoft.AspNetCore.Http;


namespace BuisnessLogic.Services.Interfaces
{
    public interface IFileService
    {
        Task SaveFilesAsync(List<IFormFile> files, string destination);
        void DeleteFile(string filePath);
    }
}