// Copyright (c) Microsoft.All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Serialization;

#pragma warning disable CA1710 // Identifiers should have correct suffix

namespace Microsoft.SemanticKernel;
/// <summary>
/// Provides a collection of arguments for operations such as <see cref="KernelFunction"/>'s InvokeAsync
/// and <see cref="IPromptTemplate"/>'s RenderAsync.
/// </summary>
/// <remarks>
/// A <see cref="KernelArguments"/> is a dictionary of argument names and values. It also carries a
/// <see cref="PromptExecutionSettings"/>, accessible via the <see cref="ExecutionSettings"/> property.
/// </remarks>
public sealed class KernelArguments : IDictionary<string, object?>, IReadOnlyDictionary<string, object?>
{
    /// <summary>Dictionary of name/values for all the arguments in the instance.</summary>
    private readonly Dictionary<string, object?> _arguments;

    private IReadOnlyDictionary<string, PromptExecutionSettings>? _executionSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="KernelArguments"/> class with the specified AI execution settings.
    /// </summary>
    [JsonConstructor]
    public KernelArguments()
    {
        _arguments = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KernelArguments"/> class with the specified AI execution settings.
    /// </summary>
    /// <param name="executionSettings">The prompt execution settings.</param>
    public KernelArguments(PromptExecutionSettings? executionSettings)
        : this(executionSettings is null
            ? null
            : [executionSettings])
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KernelArguments"/> class with the specified AI execution settings.
    /// </summary>
    /// <param name="executionSettings">The prompt execution settings.</param>
    public KernelArguments(IEnumerable<PromptExecutionSettings>? executionSettings)
    {
        _arguments = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        if (executionSettings is not null)
        {
            var newExecutionSettings = new Dictionary<string, PromptExecutionSettings>();

            foreach (var settings in executionSettings)
            {
                var targetServiceId = settings.ServiceId ?? PromptExecutionSettings.DefaultServiceId;

                if (newExecutionSettings.ContainsKey(targetServiceId))
                {
                    var exceptionMessage = (targetServiceId == PromptExecutionSettings.DefaultServiceId)
                        ? $"Multiple prompt execution settings with the default service id '{PromptExecutionSettings.DefaultServiceId}' or no service id have been provided. Specify a single default prompt execution settings and provide a unique service id for all other instances."
                        : $"Multiple prompt execution settings with the service id '{targetServiceId}' have been provided. Provide a unique service id for all instances.";

                    throw new ArgumentException(exceptionMessage, nameof(executionSettings));
                }

                newExecutionSettings[targetServiceId] = settings;
            }

            ExecutionSettings = newExecutionSettings;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KernelArguments"/> class that contains elements copied from the specified <see cref="IDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <param name="source">The <see cref="IDictionary{TKey, TValue}"/> whose elements are copied the new <see cref="KernelArguments"/>.</param>
    /// <param name="executionSettings">The prompt execution settings.</param>
    /// <remarks>
    /// If <paramref name="executionSettings"/> is non-null, it is used as the <see cref="ExecutionSettings"/> for this new instance.
    /// Otherwise, if the source is a <see cref="KernelArguments"/>, its <see cref="ExecutionSettings"/> are used.
    /// </remarks>
    public KernelArguments(IDictionary<string, object?> source, Dictionary<string, PromptExecutionSettings>? executionSettings = null)
    {
        Verify.NotNull(source);

        _arguments = new Dictionary<string, object?>(source, StringComparer.OrdinalIgnoreCase);
        ExecutionSettings = executionSettings ?? (source as KernelArguments)?.ExecutionSettings;
    }

    /// <summary>
    /// Gets or sets the prompt execution settings.
    /// </summary>
    /// <remarks>
    /// The settings dictionary is keyed by the service ID, or <see cref="PromptExecutionSettings.DefaultServiceId"/> for the default execution settings.
    /// When setting, the service id of each <see cref="PromptExecutionSettings"/> must match the key in the dictionary.
    /// </remarks>
    public IReadOnlyDictionary<string, PromptExecutionSettings>? ExecutionSettings
    {
        get => _executionSettings;
        set
        {
            if (value is { Count: > 0 })
            {
                foreach (var kv in value!)
                {
                    // Ensures that if a service id is specified it needs to match to the current key in the dictionary.
                    if (!string.IsNullOrWhiteSpace(kv.Value.ServiceId) && kv.Key != kv.Value.ServiceId)
                    {
                        throw new ArgumentException($"Service id '{kv.Value.ServiceId}' must match the key '{kv.Key}'.", nameof(ExecutionSettings));
                    }
                }
            }

            _executionSettings = value;
        }
    }

    /// <summary>
    /// Gets the number of arguments contained in the <see cref="KernelArguments"/>.
    /// </summary>
    public int Count => _arguments.Count;

    /// <summary>Adds the specified argument name and value to the <see cref="KernelArguments"/>.</summary>
    /// <param name="name">The name of the argument to add.</param>
    /// <param name="value">The value of the argument to add.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException">An argument with the same name already exists in the <see cref="KernelArguments"/>.</exception>
    public void Add(string name, object? value)
    {
        Verify.NotNull(name);
        _arguments.Add(name, value);
    }

    /// <summary>Removes the argument value with the specified name from the <see cref="KernelArguments"/>.</summary>
    /// <param name="name">The name of the argument value to remove.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    public bool Remove(string name)
    {
        Verify.NotNull(name);

        return _arguments.Remove(name);
    }

    /// <summary>Removes all arguments names and values from the <see cref="KernelArguments"/>.</summary>
    /// <remarks>
    /// This does not affect the <see cref="ExecutionSettings"/> property. To clear it as well, set it to null.
    /// </remarks>
    public void Clear()
    {
        _arguments.Clear();
    }


    /// <summary>Determines whether the <see cref="KernelArguments"/> contains an argument with the specified name.</summary>
    /// <param name="name">The name of the argument to locate.</param>
    /// <returns>true if the arguments contains an argument with the specified named; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    public bool ContainsName(string name)
    {
        Verify.NotNull(name);

        return _arguments.ContainsKey(name);
    }

    /// <summary>Gets the value associated with the specified argument name.</summary>
    /// <param name="name">The name of the argument value to get.</param>
    /// <param name="value">
    /// When this method returns, contains the value associated with the specified name,
    /// if the name is found; otherwise, null.
    /// </param>
    /// <returns>true if the arguments contains an argument with the specified name; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    public bool TryGetValue(string name, out object? value)
    {
        Verify.NotNull(name);

        return _arguments.TryGetValue(name, out value);
    }

    /// <summary>Gets or sets the value associated with the specified argument name.</summary>
    /// <param name="name">The name of the argument value to get or set.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
    public object? this[string name]
    {
        get
        {
            Verify.NotNull(name);

            return _arguments[name];
        }
        set
        {
            Verify.NotNull(name);
            _arguments[name] = value;
        }
    }

    /// <summary>Gets an <see cref="ICollection{String}"/> of all of the arguments' names.</summary>
    public ICollection<string> Names => _arguments.Keys;

    /// <summary>Gets an <see cref="ICollection{String}"/> of all of the arguments' values.</summary>
    public ICollection<object?> Values => _arguments.Values;

    #region Interface implementations

    /// <inheritdoc/>
    ICollection<string> IDictionary<string, object?>.Keys => _arguments.Keys;

    /// <inheritdoc/>
    IEnumerable<string> IReadOnlyDictionary<string, object?>.Keys => _arguments.Keys;

    /// <inheritdoc/>
    IEnumerable<object?> IReadOnlyDictionary<string, object?>.Values => _arguments.Values;

    /// <inheritdoc/>
    bool ICollection<KeyValuePair<string, object?>>.IsReadOnly => false;

    /// <inheritdoc/>
    object? IReadOnlyDictionary<string, object?>.this[string key] => _arguments[key];

    /// <inheritdoc/>
    object? IDictionary<string, object?>.this[string key]
    {
        get => _arguments[key];
        set => _arguments[key] = value;
    }

    /// <inheritdoc/>
    void IDictionary<string, object?>.Add(string key, object? value)
    {
        _arguments.Add(key, value);
    }


    /// <inheritdoc/>
    bool IDictionary<string, object?>.ContainsKey(string key)
    {
        return _arguments.ContainsKey(key);
    }


    /// <inheritdoc/>
    bool IDictionary<string, object?>.Remove(string key)
    {
        return _arguments.Remove(key);
    }


    /// <inheritdoc/>
    bool IDictionary<string, object?>.TryGetValue(string key, out object? value)
    {
        return _arguments.TryGetValue(key, out value);
    }


    /// <inheritdoc/>
    void ICollection<KeyValuePair<string, object?>>.Add(KeyValuePair<string, object?> item)
    {
        _arguments.Add(item.Key, item.Value);
    }


    /// <inheritdoc/>
    bool ICollection<KeyValuePair<string, object?>>.Contains(KeyValuePair<string, object?> item)
    {
        return ((ICollection<KeyValuePair<string, object?>>)_arguments).Contains(item);
    }


    /// <inheritdoc/>
    void ICollection<KeyValuePair<string, object?>>.CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
    {
        ((ICollection<KeyValuePair<string, object?>>)_arguments).CopyTo(array, arrayIndex);
    }


    /// <inheritdoc/>
    bool ICollection<KeyValuePair<string, object?>>.Remove(KeyValuePair<string, object?> item)
    {
        return _arguments.Remove(item.Key);
    }


    /// <inheritdoc/>
    IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator()
    {
        return _arguments.GetEnumerator();
    }


    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return _arguments.GetEnumerator();
    }


    /// <inheritdoc/>
    bool IReadOnlyDictionary<string, object?>.ContainsKey(string key)
    {
        return _arguments.ContainsKey(key);
    }


    /// <inheritdoc/>
    bool IReadOnlyDictionary<string, object?>.TryGetValue(string key, out object? value)
    {
        return _arguments.TryGetValue(key, out value);
    }

    #endregion

}
