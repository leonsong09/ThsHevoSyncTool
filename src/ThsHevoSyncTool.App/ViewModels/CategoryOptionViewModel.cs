using ThsHevoSyncTool.Formatting;

namespace ThsHevoSyncTool.ViewModels;

public sealed class CategoryOptionViewModel : ObservableObject
{
    private bool _isSelected;
    private bool _isAvailable = true;
    private int _fileCount;
    private long _totalBytes;

    public CategoryOptionViewModel(
        string id,
        string name,
        string description,
        bool isCoreDefault,
        IReadOnlyList<string> pathRules)
    {
        Id = id;
        Name = name;
        Description = description;
        IsCoreDefault = isCoreDefault;
        PathRules = pathRules;
    }

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public bool IsCoreDefault { get; }
    public IReadOnlyList<string> PathRules { get; }

    public bool IsAvailable
    {
        get => _isAvailable;
        set
        {
            if (SetProperty(ref _isAvailable, value))
            {
                if (!_isAvailable)
                {
                    IsSelected = false;
                }

                OnPropertyChanged(nameof(IsSelectable));
            }
        }
    }

    public bool IsSelectable => IsAvailable;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public int FileCount
    {
        get => _fileCount;
        set
        {
            if (SetProperty(ref _fileCount, value))
            {
                OnPropertyChanged(nameof(FileCountText));
            }
        }
    }

    public string FileCountText => FileCount.ToString();

    public long TotalBytes
    {
        get => _totalBytes;
        set
        {
            if (SetProperty(ref _totalBytes, value))
            {
                OnPropertyChanged(nameof(TotalSizeText));
            }
        }
    }

    public string TotalSizeText => ByteFormatter.Format(TotalBytes);
}

