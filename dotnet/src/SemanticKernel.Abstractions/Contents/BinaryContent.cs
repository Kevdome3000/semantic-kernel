// Copyright (c) Microsoft.All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.SemanticKernel.Text;

#pragma warning disable CA1056 // URI-like properties should not be strings
#pragma warning disable CA1055 // URI-like parameters should not be strings
#pragma warning disable CA1054 // URI-like parameters should not be strings

namespace Microsoft.SemanticKernel;
/// <summary>
/// Provides access to binary content.
/// </summary>
[Experimental("SKEXP0001")]
public class BinaryContent : KernelContent
{
    private string? _dataUri;

    private ReadOnlyMemory<byte>? _data;

    private Uri? _referencedUri;

    /// <summary>
    /// The binary content.
    /// </summary>
    [JsonIgnore, Obsolete("Use Data instead")]
    public ReadOnlyMemory<byte>? Content => Data;

    /// <summary>
    /// Gets the referenced Uri of the content.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Uri? Uri
    {
        get => GetUri();
        set => SetUri(value);
    }

    /// <summary>
    /// Gets the DataUri of the content.
    /// </summary>
    [JsonIgnore]
    public string? DataUri
    {
        get => GetDataUri();
        set => SetDataUri(value);
    }

    /// <summary>
    /// Gets the byte array data of the content.
    /// </summary>
    [JsonPropertyOrder(100), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] // Ensuring Data Uri is serialized last for better visibility of other properties.
    public ReadOnlyMemory<byte>? Data
    {
        get => GetData();
        set => SetData(value);
    }

    /// <summary>
    /// Indicates whether the content contains binary data in either <see cref="Data"/> or <see cref="DataUri"/> properties.
    /// </summary>
    /// <returns>True if the content has binary data, false otherwise.</returns>
    [JsonIgnore]
    public bool CanRead
        => _data is not null
            || _dataUri is not null;

    /// <summary>
    /// Initializes a new instance of the <see cref="BinaryContent"/> class with no content.
    /// </summary>
    /// <remarks>
    /// Should be used only for serialization purposes.
    /// </remarks>
    [JsonConstructor]
    public BinaryContent()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BinaryContent"/> class referring to an external uri.
    /// </summary>
    public BinaryContent(Uri uri)
    {
        Uri = uri;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BinaryContent"/> class for a UriData or Uri referred content.
    /// </summary>
    /// <param name="dataUri">The Uri of the content.</param>
    public BinaryContent(
        // Uri type has a ushort size limit check which inviabilizes its usage in DataUri scenarios.
        // <see href="https://github.com/dotnet/runtime/issues/96544"/>
        string dataUri)
    {
        DataUri = dataUri;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BinaryContent"/> class from a byte array.
    /// </summary>
    /// <param name="data">Byte array content</param>
    /// <param name="mimeType">The mime type of the content</param>
    public BinaryContent(
        ReadOnlyMemory<byte> data,
        string? mimeType)
    {
        Verify.NotNull(data, nameof(data));

        if (data.IsEmpty)
        {
            throw new ArgumentException(@"Data cannot be empty", nameof(data));
        }

        MimeType = mimeType;
        Data = data;
    }

    #region Private

    /// <summary>
    /// Sets the Uri of the content.
    /// </summary>
    /// <param name="uri">Target Uri</param>
    private void SetUri(Uri? uri)
    {
        if (uri?.Scheme == "data")
        {
            throw new InvalidOperationException("For DataUri contents, use DataUri property.");
        }

        _referencedUri = uri;
    }

    /// <summary>
    /// Gets the Uri of the content.
    /// </summary>
    /// <returns>Uri of the content</returns>
    private Uri? GetUri()
    {
        return _referencedUri;
    }


    /// <summary>
    /// Sets the DataUri of the content.
    /// </summary>
    /// <param name="dataUri">DataUri of the content</param>
    private void SetDataUri(string? dataUri)
    {
        if (dataUri is null)
        {
            _dataUri = null;

            // Invalidate the current bytearray
            _data = null;

            return;
        }

        var isDataUri = dataUri!.StartsWith("data:", StringComparison.OrdinalIgnoreCase) == true;

        if (!isDataUri)
        {
            throw new UriFormatException("Invalid data uri. Scheme should start with \"data:\"");
        }

        // Validate the dataUri format
        var parsedDataUri = DataUriParser.Parse(dataUri);

        // Overwrite the mimetype to the DataUri.
        MimeType = parsedDataUri.MimeType;

        // If parameters where provided in the data uri, updates the content metadata.
        if (parsedDataUri.Parameters.Count != 0)
        {
            // According to the RFC 2397, the data uri supports custom parameters
            // This method ensures that if parameter is provided those will be added
            // to the content metadata with a "data-uri-" prefix.
            UpdateDataUriParametersToMetadata(parsedDataUri);
        }

        _dataUri = dataUri;

        // Invalidate the current bytearray
        _data = null;
    }

    private void UpdateDataUriParametersToMetadata(DataUriParser.DataUri parsedDataUri)
    {
        if (parsedDataUri.Parameters.Count == 0)
        {
            return;
        }

        var newMetadata = Metadata as Dictionary<string, object?>;

        if (newMetadata is null)
        {
            newMetadata = [];

            if (Metadata is not null)
            {
                foreach (var property in Metadata!)
                {
                    newMetadata[property.Key] = property.Value;
                }
            }
        }

        // Overwrite any properties if already defined
        foreach (var property in parsedDataUri.Parameters)
        {
            // Set the properties from the DataUri metadata
            newMetadata[$"data-uri-{property.Key}"] = property.Value;
        }

        Metadata = newMetadata;
    }

    private string GetDataUriParametersFromMetadata()
    {
        var metadata = Metadata;

        if (metadata is null || metadata.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder parameters = new();

        foreach (var property in metadata)
        {
            if (property.Key.StartsWith("data-uri-", StringComparison.OrdinalIgnoreCase))
            {
                parameters.Append($";{property.Key.AsSpan(9).ToString()}={property.Value}");
            }
        }

        return parameters.ToString();
    }

    /// <summary>
    /// Sets the byte array data content.
    /// </summary>
    /// <param name="data">Byte array data content</param>
    private void SetData(ReadOnlyMemory<byte>? data)
    {
        // Overriding the content will invalidate the previous dataUri
        _dataUri = null;
        _data = data;
    }

    /// <summary>
    /// Gets the byte array data content.
    /// </summary>
    /// <returns>The byte array data content</returns>
    private ReadOnlyMemory<byte>? GetData()
    {
        return GetCachedByteArrayContent();
    }

    /// <summary>
    /// Gets the DataUri of the content.
    /// </summary>
    /// <returns></returns>
    private string? GetDataUri()
    {
        if (!CanRead)
        {
            return null;
        }

        if (_dataUri is not null)
        {
            // Double check if the set MimeType matches the current dataUri.
            var parsedDataUri = DataUriParser.Parse(_dataUri);

            if (string.Equals(parsedDataUri.MimeType, MimeType, StringComparison.OrdinalIgnoreCase))
            {
                return _dataUri;
            }
        }

        // If the Uri is not a DataUri, then we need to get from byteArray (caching if needed) to generate it.
        return GetCachedUriDataFromByteArray(GetCachedByteArrayContent());
    }

    private string? GetCachedUriDataFromByteArray(ReadOnlyMemory<byte>? cachedByteArray)
    {
        if (cachedByteArray is null)
        {
            return null;
        }

        if (MimeType is null)
        {
            // May consider defaulting to application/octet-stream if not provided.
            throw new InvalidOperationException("Can't get the data uri without a mime type defined for the content.");
        }

        // Ensure that if any data-uri-parameter defined in the metadata those will be added to the data uri.
        _dataUri = $"data:{MimeType}{GetDataUriParametersFromMetadata()};base64," + Convert.ToBase64String(cachedByteArray.Value.ToArray());

        return _dataUri;
    }

    private ReadOnlyMemory<byte>? GetCachedByteArrayContent()
    {
        if (_data is null && _dataUri is not null)
        {
            var parsedDataUri = DataUriParser.Parse(_dataUri);

            if (string.Equals(parsedDataUri.DataFormat, "base64", StringComparison.OrdinalIgnoreCase))
            {
                _data = Convert.FromBase64String(parsedDataUri.Data!);
            }
            else
            {
                // Defaults to UTF8 encoding if format is not provided.
                _data = Encoding.UTF8.GetBytes(parsedDataUri.Data!);
            }
        }

        return _data;
    }

    #endregion

}
