using System.Net.Sockets;
using Amazon.S3;
using Amazon.S3.Model;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace ChatBE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ManagerController : ControllerBase
    {
        public IConfiguration Configuration { get; }

        private string connectionString;
        private string containerName;
        private BlobServiceClient blobServiceClient;
        private BlobContainerClient containerClient;

        private string accessKey = string.Empty;
        private string secretKey = string.Empty;
        private string endpoint = string.Empty;

        AmazonS3Config? s3Config = null;
        AmazonS3Client? s3Client = null;

        public ManagerController(IConfiguration configuration)
        {
            Configuration = configuration;

            connectionString = configuration["ChatStorageConnectionString"]!;
            containerName = configuration["ChatStorageContainerName"]!;

            blobServiceClient = new BlobServiceClient(connectionString);
            containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            accessKey = configuration["R2AccessKeyID"]!;
            secretKey = configuration["R2SecretAccessKey"]!;
            endpoint = configuration["R2Url"]!;

            s3Config = new AmazonS3Config { ServiceURL = endpoint, ForcePathStyle = true };

            s3Client = new AmazonS3Client(accessKey, secretKey, s3Config);
        }

        [Route("down/{charcId}/{animationId}")]
        public async Task<IActionResult> downAsync(string charcId, string animationId)
        {
            string objectKey = $"{animationId}.glb";

            try
            {
                var response = await s3Client!.GetObjectAsync(
                    new GetObjectRequest { BucketName = "chars", Key = objectKey }
                );

                return File(response.ResponseStream, "model/gltf-binary", $"{animationId}.glb");
            }
            catch (AmazonS3Exception ex)
            {
                return StatusCode((int)ex.StatusCode, $"Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal error: {ex.Message}");
            }
        }

        [Route("getfn")]
        public IActionResult getfn()
        {
            var results = new List<string>()
            {
                "Charc100__BrokenHeart__",
                "Charc101__Hug__",
                "Charc102__Heart__",
                "Charc103__Kiss__",
                "Charc104__Ring__",
                "Charc105__Calling__",
                "Charc106__Engine__",
                "Charc10__Dancing__",
                "Charc11__Dancing__",
                "Charc12__Dancing__",
                "Charc13__Walking__",
                "Charc14__Dancing__",
                "Charc15__Walking__",
                "Charc16__Dancing__",
                "Charc17__Dancing__",
                "Charc18__Dancing__",
                "Charc19__Dancing__",
                "Charc1__Running__",
                "Charc20__Walking__",
                "Charc21__Dancing__",
                "Charc22__Dancing__",
                "Charc23__Dancing__",
                "Charc24__Walking__",
                "Charc25__Walking__",
                "Charc26__Dancing__",
                "Charc27__Dancing__",
                "Charc28__Dancing__",
                "Charc29__Walking__",
                "Charc2__Dancing__",
                "Charc30__Walking__",
                "Charc31__Dancing__",
                "Charc32__Dancing__",
                "Charc33__Standing__",
                "Charc34__Dancing__",
                "Charc35__Dancing__",
                "Charc36__Dancing__",
                "Charc37__Dancing__",
                "Charc38__Dancing__",
                "Charc39__Dancing__",
                "Charc3__Dancing__",
                "Charc40__Angry__",
                "Charc41__Defeated__",
                "Charc42__Dying__",
                "Charc43__Jab__",
                "Charc44__Jogging__",
                "Charc45__Praying__",
                "Charc46__Swimming__",
                "Charc47__Taunt__",
                "Charc48__Angry__",
                "Charc49__Drunk__",
                "Charc4__Dancing__",
                "Charc50__Dying__",
                "Charc51__Falling__",
                "Charc52__Jump__",
                "Charc53__Nervously__",
                "Charc54__Swimming__",
                "Charc55__Falling__",
                "Charc56__Kiss__",
                "Charc57__Swimming___",
                "Charc58__Texting__",
                "Charc59__Dying__",
                "Charc5__Walking__",
                "Charc60__Falling__",
                "Charc61__Jab__",
                "Charc62__KnockedOut__",
                "Charc63__Praying__",
                "Charc64__RightTurn__",
                "Charc65__Standing__",
                "Charc66__Swimming__",
                "Charc67__Taunt__",
                "Charc68__Angry__",
                "Charc69__Drunk__",
                "Charc6__Dancing__",
                "Charc70__Dwarf__",
                "Charc71__Laying__",
                "Charc72__Idle__",
                "Charc73__Standing__",
                "Charc74__Texting__",
                "Charc75__Fax__",
                "Charc76__Angry__",
                "Charc77__Gesture__",
                "Charc78__Drunk__",
                "Charc79__Excited__",
                "Charc7__Dancing__",
                "Charc80__Stand__",
                "Charc81__Stand__",
                "Charc82__Walk__",
                "Charc83__Golf__",
                "Charc84__Side__",
                "Charc85__Dancing__",
                "Charc86__Villain__",
                "Charc87__Jogging__",
                "Charc88__Girl__",
                "Charc89__Girl__",
                "Charc8__Sick__",
                "Charc90__Run__",
                "Charc91__Swimming__",
                "Charc92__Fax__",
                "Charc93__Walk__",
                "Charc94__Walk__",
                "Charc95__Rose__",
                "Charc96__Heart__",
                "Charc97__Hug__",
                "Charc98__Hug__",
                "Charc99__Heart__",
                "Charc9__Dancing__",
            };

            return Ok(results);
        }
    }
}
