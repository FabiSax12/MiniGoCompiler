using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace IDE;

/// <summary>
/// ViewModel for a single node in the file explorer tree.
/// Supports lazy-loading of subdirectories.
/// </summary>
public class FileSystemNode : INotifyPropertyChanged
{
    private bool _isExpanded;
    private bool _isSelected;

    public string Name { get; }
    public string FullPath { get; }
    public bool IsDirectory { get; }
    public bool IsDirectoryDummy { get; protected set; }

    public ObservableCollection<FileSystemNode> Children { get; } = new();

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (SetField(ref _isExpanded, value))
                OnPropertyChanged(nameof(IconText));
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    /// <summary>
    /// Emoji icon based on node type and state.
    /// Directories: 📁 (closed) / 📂 (expanded).
    /// .g / .go files: 🔹, other files: 📄.
    /// </summary>
    public string IconText => IsDirectory
        ? (IsExpanded ? "📂" : "📁")
        : IsGoFile
            ? "🔹"
            : "📄";

    /// <summary>
    /// True for .g (MiniGo source) and .go (standard Go) file nodes.
    /// </summary>
    public bool IsGoFile => !IsDirectory &&
        (Name.EndsWith(".g", StringComparison.OrdinalIgnoreCase) ||
         Name.EndsWith(".go", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Creates a node. For directory nodes, adds a placeholder child until expanded.
    /// </summary>
    public FileSystemNode(string fullPath, bool isDirectory)
    {
        FullPath = fullPath;
        Name = isDirectory ? Path.GetFileName(fullPath) : Path.GetFileName(fullPath);
        IsDirectory = isDirectory;

        if (isDirectory)
        {
            Children.Add(new FileSystemNode.DummyNode(this));
        }
    }

    /// <summary>
    /// Constructor used by DummyNode to avoid infinite recursion.
    /// </summary>
    private protected FileSystemNode(FileSystemNode parent, bool isDirectoryDummy)
    {
        FullPath = string.Empty;
        Name = string.Empty;
        IsDirectory = true;
        IsDirectoryDummy = isDirectoryDummy;
    }

    public void EnsureLoaded()
    {
        if (!IsDirectory || Children.Count > 0 && !Children[0].IsDirectoryDummy)
            return;

        Children.Clear();

        string[] directories;
        string[] files;

        try
        {
            directories = Directory.GetDirectories(FullPath);
            files = Directory.GetFiles(FullPath);
        }
        catch
        {
            return;
        }

        foreach (var dir in directories.OrderBy(d => d))
            Children.Add(new FileSystemNode(dir, isDirectory: true));

        // Show .txt, .g (MiniGo) and .go (standard Go) files
        foreach (var file in files.OrderBy(f => f)
            .Where(f => Path.GetExtension(f) is ".txt" or ".g" or ".go"))
        {
            Children.Add(new FileSystemNode(file, isDirectory: false));
        }
    }

    /// <summary>
    /// Placeholder used to show the expand arrow before actual children are loaded.
    /// </summary>
    private sealed class DummyNode : FileSystemNode
    {
        public DummyNode(FileSystemNode parent) : base(parent, isDirectoryDummy: true) { }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}