using Newtonsoft.Json;
using System;

namespace SaveToCosmos;

// This can easily be modified to be BaseEntity<T> and public T Id to support different key types.
// Using non-generic integer types for simplicity and to ease caching logic
public abstract class BaseEntity
{
    [JsonProperty(PropertyName = "id")]
    public virtual string Id { get; protected set; }

    public BaseEntity()
    {
        Id = Guid.NewGuid().ToString();
    }
}
