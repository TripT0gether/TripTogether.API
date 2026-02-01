// BlobService.cs
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

public class BlobService : IBlobService
{
    private readonly string _bucketName = "triptogether-bucket";
    private readonly ILogger _loggerService;
    private readonly IMinioClient _minioClient;

    public BlobService(ILogger<BlobService> logger)
    {
        _loggerService = logger;

        // Get key từ docker-compose
        var endpoint = Environment.GetEnvironmentVariable("MINIO_ENDPOINT") ?? "103.211.201.162:9000";
        var accessKey = Environment.GetEnvironmentVariable("MINIO_ACCESS_KEY");
        var secretKey = Environment.GetEnvironmentVariable("MINIO_SECRET_KEY");

        _loggerService.LogInformation("Initializing BlobService...");
        _loggerService.LogInformation($"Connecting to MinIO at: {endpoint}");

        try
        {
            // Kết nối MinIO không dùng SSL (vì đang dùng IP:port hoặc HTTP)
            _minioClient = new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(accessKey, secretKey)
                .WithSSL(false)
                .Build();

            _loggerService.LogInformation("MinIO client initialized successfully.");
        }
        catch (Exception ex)
        {
            _loggerService.LogError($"Failed to initialize MinIO client: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    ///     Check xem bucket đã tồn tại trên Minio chưa (creates nếu bucket ko tồn tai ).
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task EnsureBucketExistsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucketName), cancellationToken);

            if (!exists)
            {
                _loggerService.LogWarning($"Bucket '{_bucketName}' not found. Creating...");
                await _minioClient.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(_bucketName),
                    cancellationToken);
                _loggerService.LogInformation($"Bucket '{_bucketName}' created.");
            }
            else
            {
                _loggerService.LogInformation($"Bucket '{_bucketName}' already exists.");
            }
        }
        catch (MinioException mex)
        {
            _loggerService.LogError($"MinIO error in EnsureBucketExists: {mex.Message}");
            throw;
        }
        catch (OperationCanceledException)
        {
            _loggerService.LogWarning("Bucket creation cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            _loggerService.LogError($"Unexpected error in EnsureBucketExists: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    ///     Upload file lên MinIO bucket
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="fileStream"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task UploadFileAsync(string fileName, Stream fileStream, string? folder = null, CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(cancellationToken);

        string objectName = string.IsNullOrWhiteSpace(folder)
            ? fileName
            : $"{folder.TrimEnd('/')}/{fileName}";

        var contentType = GetContentType(fileName);

        var putArgs = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectName)
            .WithStreamData(fileStream)
            .WithObjectSize(fileStream.Length)
            .WithContentType(contentType);

        _loggerService.LogInformation($"Uploading '{objectName}' (type={contentType})...");
        await _minioClient.PutObjectAsync(putArgs, cancellationToken);
        _loggerService.LogInformation($"Upload completed: {objectName}");
    }

    /// <summary>
    ///     Tạo 1 cái preview URL cho file đã upload lên MinIO.
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public Task<string> GetPreviewUrlAsync(string fileName)
    {
        var minioHost = Environment.GetEnvironmentVariable("MINIO_HOST")
                        ?? "https://minio.fpt-devteam.fun/";

        _loggerService.LogInformation($"Generating preview URL for: {fileName}");
        var previewUrl = $"{minioHost}/api/v1/buckets/{_bucketName}/objects/download?"
                       + $"preview=true&prefix={fileName}&version_id=null";

        _loggerService.LogInformation($"Preview URL: {previewUrl}");
        return Task.FromResult(previewUrl);
    }

    /// <summary>
    ///     Generates a presigned URL for downloading a file from MinIO.
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<string> GetFileUrlAsync(string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            _loggerService.LogInformation($"Generating presigned URL for: {fileName}");

            var args = new PresignedGetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(fileName)
                .WithExpiry(7 * 24 * 60 * 60);

            var presignTask = _minioClient.PresignedGetObjectAsync(args);

            string url = cancellationToken == default
                ? await presignTask
                : await presignTask.WaitAsync(cancellationToken);

            // Replace both http and https with the public domain
            var minioHost = Environment.GetEnvironmentVariable("MINIO_HOST") ?? "https://minio.fpt-devteam.fun";
            url = url.Replace("http://103.211.201.162:9000", minioHost)
                     .Replace("https://103.211.201.162:9000", minioHost);

            _loggerService.LogInformation($"Presigned URL: {url}");
            return url;
        }
        catch (OperationCanceledException)
        {
            _loggerService.LogWarning($"Presigned URL generation cancelled: {fileName}");
            throw;
        }
        catch (Exception ex)
        {
            _loggerService.LogError($"Error generating presigned URL: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    ///     Check content type dựa trên file extension.
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    private string GetContentType(string fileName)
    {
        _loggerService.LogInformation($"Determining content type for: {fileName}");
        var ext = Path.GetExtension(fileName)?.ToLowerInvariant();

        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf",
            ".mp4" => "video/mp4",
            _ => "application/octet-stream"     // Fallback nếu định dạng ko xác định
        };
    }

    /// <summary>
    ///     Delete a file from MinIO storage.
    /// </summary>
    /// <param name="fileUrl">The full URL or object path of the file to delete.</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(fileUrl))
        {
            _loggerService.LogWarning("DeleteFileAsync called with null or empty fileUrl.");
            return;
        }

        try
        {
            // Extract object name from URL
            // URL format: https://minio.fpt-devteam.fun/triptogether-bucket/avatars/user123/file.jpg?...
            var uri = new Uri(fileUrl);
            var path = uri.AbsolutePath;
            
            // Remove leading /bucket-name/ if present
            var objectName = path.StartsWith($"/{_bucketName}/") 
                ? path.Substring($"/{_bucketName}/".Length)
                : path.TrimStart('/');

            _loggerService.LogInformation("Deleting file from MinIO: {ObjectName}", objectName);

            var args = new RemoveObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName);

            await _minioClient.RemoveObjectAsync(args, cancellationToken);

            _loggerService.LogInformation("File deleted successfully: {ObjectName}", objectName);
        }
        catch (OperationCanceledException)
        {
            _loggerService.LogWarning("File deletion cancelled: {FileUrl}", fileUrl);
            throw;
        }
        catch (Exception ex)
        {
            _loggerService.LogError(ex, "Error deleting file from MinIO: {FileUrl}", fileUrl);
            // Don't throw - old file deletion failure shouldn't block new upload
        }
    }
}