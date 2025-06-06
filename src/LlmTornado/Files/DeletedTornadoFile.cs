namespace LlmTornado.Files;

/// <summary>
/// Represents deleted <see cref="TornadoFile"/>
/// </summary>
public class DeletedTornadoFile
{
    /// <summary>
    /// Whether the file is deleted.
    /// </summary>
    public bool Deleted { get; set; }
    
    /// <summary>
    /// ID of the file.
    /// </summary>
    public string Id { get; set; }
}