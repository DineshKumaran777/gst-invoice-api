// =============================================================================
// Copyright © 2024 DK (Freelancer)
// All rights reserved.
//
// Product:     DK GST Billing Platform
// Company:     DK (Freelancer)
// Website:     www.dkgstbilling.com
// Email:       support@dkgstbilling.com
//
// NOTICE: All information contained herein is, and remains the property of
// DK (Freelancer). The intellectual and technical
// concepts contained herein are proprietary to DK (Freelancer)
// and may be covered by Indian and International Patents,
// patents in process, and are protected by trade secret or copyright law.
//
// Unauthorized copying, modification, distribution, or use of this software,
// via any medium, is strictly prohibited without the prior written permission
// of DK (Freelancer).
// =============================================================================
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using GSTInvoice.API.Options;
using Microsoft.Extensions.Options;

namespace GSTInvoice.API.Services;

public class StorageService(IOptions<AzureBlobOptions> options) : IStorageService
{
    private readonly AzureBlobOptions blobOptions = options.Value;

    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(blobOptions.ConnectionString))
        {
            return string.Empty;
        }

        var serviceClient = new BlobServiceClient(blobOptions.ConnectionString);
        var containerClient = serviceClient.GetBlobContainerClient(blobOptions.ContainerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        var blobName = $"{DateTime.UtcNow:yyyyMMddHHmmss}-{fileName}";
        var blobClient = containerClient.GetBlobClient(blobName);

        await blobClient.UploadAsync(content, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: cancellationToken);
        return blobClient.Uri.ToString();
    }
}

