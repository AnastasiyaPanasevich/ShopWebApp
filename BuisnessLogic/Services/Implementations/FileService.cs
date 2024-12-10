using BuisnessLogic.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace BuisnessLogic.Services.Implementations
{
    public class FileService : IFileService
    {
        public async Task SaveFilesAsync(List<IFormFile> files, string destination)
        {
            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var filePath = Path.Combine(destination, Path.GetFileName(file.FileName));
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                }
            }
        }

        public void DeleteFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

}
