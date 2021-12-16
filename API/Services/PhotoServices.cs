using System.Threading.Tasks;
using API._Helpers;
using API.Interfaces;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace API.Services
{
    public class PhotoServices : IPhotoService
    {
        private readonly Cloudinary _cloud;
        public PhotoServices(IOptions<CloudinarySettings> config)
        {
            var acc=new Account(
              config.Value.CLoudName,
              config.Value.ApiKey,
              config.Value.ApiSecret  
            );
            _cloud=new Cloudinary(acc);
        }

        public async Task<ImageUploadResult> AddphotoAsync(IFormFile file)
        {
            var uploadresult=new ImageUploadResult();
            if(file.Length>0)
            {
                using var stream=file.OpenReadStream();
                var  UploadParams=new ImageUploadParams{
                    File=new FileDescription(file.FileName,stream),
                    Transformation=new Transformation().Height(500).Width(500).Crop("fill").Gravity("face")
                };
                uploadresult= await  _cloud.UploadAsync(UploadParams);
            }
            return uploadresult;
        }

        public async Task<DeletionResult> DeletePhotoAsync(string PublicId)
        {
            var Delete=new DeletionParams(PublicId);
            var result=await _cloud.DestroyAsync(Delete);
            return result;

        }

        
    }
}