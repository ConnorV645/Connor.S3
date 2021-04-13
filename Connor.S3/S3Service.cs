using System;
using System.IO;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

namespace Connor.S3
{
    public class S3Service : IDisposable
    {
        protected readonly AmazonS3Client s3Client;
        public string DefaultBucket { get; set; }
        public S3Service()
        {
            var s3Access = Environment.GetEnvironmentVariable("S3Access");
            var s3Secret = Environment.GetEnvironmentVariable("S3Secret");
            var s3Region = Environment.GetEnvironmentVariable("S3Region");

            if (string.IsNullOrEmpty(s3Access))
            {
                throw new Exception("S3Access Environment Variable is not set");
            }
            if (string.IsNullOrEmpty(s3Secret))
            {
                throw new Exception("S3Secret Environment Variable is not set");
            }
            if (string.IsNullOrEmpty(s3Region))
            {
                throw new Exception("S3Region Environment Variable is not set");
            }

            s3Client = new AmazonS3Client(s3Access, s3Secret, GetRegionFromString(s3Region));
        }

        public S3Service(string access, string secret, string region)
        {
            s3Client = new AmazonS3Client(access, secret, GetRegionFromString(region));
        }

        public S3Service(string region)
        {
            s3Client = new AmazonS3Client(Amazon.Runtime.FallbackCredentialsFactory.GetCredentials(), GetRegionFromString(region));
        }

        private static RegionEndpoint GetRegionFromString(string region)
        {
            var endpoint = RegionEndpoint.GetBySystemName(region);
            if (endpoint == null)
            {
                throw new Exception("Region Not Found");
            }
            return endpoint;
        }

        public async Task<Stream> StreamFileDownload(string path, string bucket = null)
        {
            var finalBucket = bucket ?? DefaultBucket;
            if (string.IsNullOrWhiteSpace(finalBucket))
            {
                throw new Exception("S3 Bucket Is Required");
            }
            using var utility = new TransferUtility(s3Client);
            var request = new TransferUtilityOpenStreamRequest
            {
                BucketName = finalBucket,
                Key = path
            };
            return await utility.OpenStreamAsync(request);
        }

        public async Task<Stream> GetPartialFile(string path, long start, long end, string bucket = null)
        {
            var finalBucket = bucket ?? DefaultBucket;
            if (string.IsNullOrWhiteSpace(finalBucket))
            {
                throw new Exception("S3 Bucket Is Required");
            }
            var request = new GetObjectRequest
            {
                BucketName = finalBucket,
                Key = path,
                ByteRange = new ByteRange(start, end)
            };
            var response = await s3Client.GetObjectAsync(request);
            return response.ResponseStream;
        }

        public async Task StreamFileUpload(Stream source, string path, string bucket = null, bool isPublic = false)
        {
            var finalBucket = bucket ?? DefaultBucket;
            if (string.IsNullOrWhiteSpace(finalBucket))
            {
                throw new Exception("S3 Bucket Is Required");
            }
            using var utility = new TransferUtility(s3Client);
            var request = new TransferUtilityUploadRequest
            {
                InputStream = source,
                BucketName = finalBucket,
                Key = path
            };
            if (isPublic)
            {
                request.CannedACL = S3CannedACL.PublicRead;
            }
            await utility.UploadAsync(request);
        }

        public async Task StreamFileUploadWithContentType(Stream source, string path, string contentType, string bucket = null, bool isPublic = false)
        {
            var finalBucket = bucket ?? DefaultBucket;
            if (string.IsNullOrWhiteSpace(finalBucket))
            {
                throw new Exception("S3 Bucket Is Required");
            }
            using var utility = new TransferUtility(s3Client);
            var request = new TransferUtilityUploadRequest
            {
                InputStream = source,
                BucketName = finalBucket,
                Key = path,
                ContentType = contentType
            };
            if (isPublic)
            {
                request.CannedACL = S3CannedACL.PublicRead;
            }
            await utility.UploadAsync(request);
        }

        public async Task DeleteFileAsync(string path, string bucket = null)
        {
            var finalBucket = bucket ?? DefaultBucket;
            if (string.IsNullOrWhiteSpace(finalBucket))
            {
                throw new Exception("S3 Bucket Is Required");
            }
            await s3Client.DeleteObjectAsync(finalBucket, path);
        }

        public void Dispose()
        {
            s3Client.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
