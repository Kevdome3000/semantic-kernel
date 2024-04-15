// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.Plugins.Grpc.Model;

using System.Collections.Generic;


/// <summary>
/// The gRPC operation data contract.
/// </summary>
internal sealed class GrpcOperationDataContractType(string name, IList<GrpcOperationDataContractTypeFiled> fields)
{

    /// <summary>
    /// Data contract name
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// List of fields
    /// </summary>
    public IList<GrpcOperationDataContractTypeFiled> Fields { get; } = fields;

}
