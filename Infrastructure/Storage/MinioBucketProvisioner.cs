using Amazon.S3;
using Amazon.S3.Model;

namespace PMIS.Infrastructure.Storage;

public class MinioBucketProvisioner(IAmazonS3 s3)
{
    private readonly HashSet<string> _ensuredBuckets = [];
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task EnsureExistsAsync(string bucketName, CancellationToken cancellationToken = default)
    {
        if (_ensuredBuckets.Contains(bucketName)) return;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_ensuredBuckets.Contains(bucketName)) return;

            if (!await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(s3, bucketName))
            {
                await s3.PutBucketAsync(new PutBucketRequest { BucketName = bucketName }, cancellationToken);
            }

            _ensuredBuckets.Add(bucketName);
        }
        finally
        {
            _lock.Release();
        }
    }
}
