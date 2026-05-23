using System.Net;
using System.Net.Http;
using System.Security.Claims;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using ChatBE.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static System.Reflection.Metadata.BlobBuilder;

namespace ChatBE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlobController : ControllerBase
    {
        public IConfiguration Configuration { get; }

        BlobServiceClient blobServiceClient;
        BlobContainerClient containerClient;

        private long MaxFileSize = 20 * 1024 * 1024;

        private string accountConnectionStrings;
        private string accountContainerName;

        public BlobController(IConfiguration configuration)
        {
            Configuration = configuration;

            accountConnectionStrings = configuration["UploadStorageConnectionString"]!;
            accountContainerName = configuration["UploadStorageContainerName"]!;

            blobServiceClient = new BlobServiceClient(accountConnectionStrings);

            containerClient = blobServiceClient.GetBlobContainerClient(accountContainerName);
        }

        [Authorize(AuthenticationSchemes = "UserClaims")]
        [HttpGet("DownloadFile/{filename}")]
        public async Task<IActionResult> DownloadFile(string filename)
        {
            try
            {
                // Create a BlobServiceClient
                BlobServiceClient blobServiceClient = new BlobServiceClient(
                    accountConnectionStrings
                );

                // Get the container client
                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(
                    accountContainerName
                );

                // Get the blob client
                BlobClient blobClient = containerClient.GetBlobClient(filename);

                // Download the blob to a memory stream
                var memoryStream = new MemoryStream();
                await blobClient.DownloadToAsync(memoryStream);

                // Reset the memory stream's position
                memoryStream.Position = 0;

                // Return the file as a FileStreamResult
                return File(memoryStream, "application/octet-stream", filename);
            }
            catch (Exception ex)
            {
                // Handle errors
                return BadRequest(new { message = $"An error occurred: {ex.Message}" });
            }
        }

        [Authorize(AuthenticationSchemes = "UserClaims")]
        [HttpGet("DownloadBlobAsync/{filename}")]
        public async Task<byte[]> DownloadBlobAsync(string filename)
        {
            BlobClient blobClient = containerClient.GetBlobClient(filename);
            using (MemoryStream memoryStream = new MemoryStream())
            {
                await blobClient.DownloadToAsync(memoryStream);
                return memoryStream.ToArray();
            }
        }

        [Authorize(AuthenticationSchemes = "UserClaims")]
        [HttpGet("GetBlobUrl2/{fileName}")]
        public string GetBlobUrl2(string fileName)
        {
            // Create a BlobContainerClient to interact with the container
            BlobContainerClient containerClient = new BlobContainerClient(
                accountConnectionStrings,
                accountContainerName
            );

            // Get a reference to the blob
            BlobClient blobClient = containerClient.GetBlobClient(fileName);

            // Get the Blob URI (URL)
            Uri blobUri = blobClient.Uri;

            // Return the URL as a string
            return blobUri.ToString();
        }

        [Authorize(AuthenticationSchemes = "UserClaims")]
        [HttpGet("GetBlobUrl/{fileName}")]
        public IActionResult GetBlobUrl(string fileName)
        {
            // Replace with your container client
            BlobContainerClient containerClient = new BlobContainerClient(
                accountConnectionStrings,
                accountContainerName
            );

            // Get a reference to the blob
            BlobClient blobClient = containerClient.GetBlobClient(fileName);

            // Check if the blob exists
            if (!blobClient.Exists())
            {
                return NotFound("Blob not found");
            }

            // Generate a SAS URL (if necessary)
            var sasUri = blobClient.GenerateSasUri(
                BlobSasPermissions.Read,
                DateTimeOffset.UtcNow.AddYears(1)
            );

            return Ok(sasUri.ToString());
        }

        [Authorize(AuthenticationSchemes = "UserClaims")]
        [HttpPost("UploadFiles")]
        public async Task<IActionResult> UploadFiles()
        {
            var files = Request.Form.Files;
            if (files == null || files.Count == 0)
                return BadRequest("No files were received.");

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    if (file.Length > MaxFileSize)
                    {
                        return BadRequest("File size exceeds the 20 MB limit.");
                    }

                    BlobClient blobClient = containerClient.GetBlobClient(file.FileName);
                    var metadata = new Dictionary<string, string>();
                    metadata = new Dictionary<string, string>
                    {
                        { "blobtype", "image" },
                        { "uploadDate", DateTime.UtcNow.ToString("o") },
                    };

                    using (var stream = file.OpenReadStream())
                    {
                        await blobClient.UploadAsync(stream, overwrite: true);
                        await blobClient.SetMetadataAsync(metadata);
                    }
                }
            }

            return Ok();
        }
    }
}
