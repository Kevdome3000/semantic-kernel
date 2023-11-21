// Copyright (c) Microsoft. All rights reserved.

namespace RepoUtils;

using System.Text.Json;


public static class ObjectExtensions
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    public static string AsJson(this object obj) => JsonSerializer.Serialize(obj, s_jsonOptions);
}
