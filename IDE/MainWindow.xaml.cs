using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using Antlr4.Runtime;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Rendering;
using Generated;
using MiniGo.Compiler.Errors;
using MiniGo.Compiler.Errors.Listeners;
using MiniGo.Compiler.Semantic;

namespace IDE;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private string? _currentFilePath;
    private string? _rootFolder;
    private DispatcherTimer _compileDebounceTimer = null!;
    private ErrorHighlighter? _errorHighlighter;

    public MainWindow()
    {
        InitializeComponent();
        LoadSyntaxHighlighting();
        SetupErrorHighlighter();
        SetupDebounce();
        textEditor.TextChanged += OnEditorTextChanged;
        textEditor.TextArea.KeyDown += OnEditorKeyDown;
        fileTree.MouseDoubleClick += OnFileTreeMouseDoubleClick;
        fileTree.AddHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler(OnTreeViewItemExpanded), true);
        UpdateStatus("Open a folder to get started");
    }

    #region Syntax Highlighting

    private void LoadSyntaxHighlighting()
    {
        const string xshdFile = "MiniGoHighlighting.xshd";
        if (!File.Exists(xshdFile)) return;

        using var reader = XmlReader.Create(xshdFile);
        var highlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
        textEditor.SyntaxHighlighting = highlighting;
    }

    #endregion

    #region File Tree

    private void OpenFolder(string path)
    {
        try
        {
            _rootFolder = path;
            var roots = new System.Collections.ObjectModel.ObservableCollection<FileSystemNode>();
            roots.Add(new FileSystemNode(path, isDirectory: true));
            fileTree.ItemsSource = roots;
            folderPathLabel.Text = Path.GetFileName(path);
            UpdateStatus($"Opened: {path}");
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error opening folder: {ex.Message}");
        }
    }

    private void OnOpenFolderClick(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select a folder to open",
            Multiselect = false
        };
        if (dialog.ShowDialog() == true)
        {
            OpenFolder(dialog.FolderName);
        }
    }

    private void OnExitClick(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void OnFileTreeMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (fileTree.SelectedItem is FileSystemNode node && !node.IsDirectory)
        {
            OpenFile(node.FullPath);
            e.Handled = true;
        }
        else if (fileTree.SelectedItem is FileSystemNode dirNode && dirNode.IsDirectory)
        {
            dirNode.IsExpanded = !dirNode.IsExpanded;
        }
    }

    private void OnTreeViewItemExpanded(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is TreeViewItem item && item.DataContext is FileSystemNode node)
        {
            node.EnsureLoaded();
        }
    }

    #endregion

    #region File Operations

    private void OpenFile(string path)
    {
        try
        {
            string content = File.ReadAllText(path);
            _currentFilePath = path;
            textEditor.Text = content;
            fileNameLabel.Text = Path.GetFileName(path);
            _errorHighlighter?.ClearErrors();
            TriggerCompilation();
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error opening file: {ex.Message}");
        }
    }

    private void OnEditorKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        // Ctrl+O to open file
        if (e.Key == Key.O && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "MiniGo Files (*.txt;*.g)|*.txt;*.g|All Files (*.*)|*.*"
            };
            if (dialog.ShowDialog() == true)
            {
                OpenFile(dialog.FileName);
            }
            e.Handled = true;
        }
    }

    #endregion

    #region Error Highlighting

    private void SetupErrorHighlighter()
    {
        _errorHighlighter = new ErrorHighlighter();
        textEditor.TextArea.TextView.BackgroundRenderers.Add(_errorHighlighter);
    }

    #endregion

    #region Compilation & Errors

    private void SetupDebounce()
    {
        _compileDebounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _compileDebounceTimer.Tick += (s, e) =>
        {
            _compileDebounceTimer.Stop();
            CompileCurrentFile();
        };
    }

    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
        _compileDebounceTimer.Stop();
        _compileDebounceTimer.Start();
    }

    private void TriggerCompilation()
    {
        _compileDebounceTimer.Stop();
        _compileDebounceTimer.Start();
    }

    private void CompileCurrentFile()
    {
        if (string.IsNullOrEmpty(_currentFilePath))
            return;

        var collector = new ErrorCollector(_currentFilePath);

        try
        {
            ICharStream stream = CharStreams.fromString(textEditor.Text);
            var lexer = new MiniGoLexer(stream);
            lexer.RemoveErrorListeners();
            lexer.AddErrorListener(new LexerErrorListener(collector));

            CommonTokenStream tokens = new CommonTokenStream(lexer);
            var parser = new MiniGoParser(tokens);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new ParserErrorListener(collector));
            parser.ErrorHandler = new MiniGoErrorStrategy();

            var tree = parser.root();

            var typeChecker = new TypeChecker(collector, _currentFilePath);
            typeChecker.Visit(tree);
        }
        catch (Exception ex)
        {
            collector.AddLexerError($"Internal compiler error: {ex.Message}", 1, 0);
        }

        _errorHighlighter?.UpdateErrors(textEditor.Document, collector.GetSortedErrors());

        if (collector.HasErrors)
        {
            UpdateStatus($"{collector.ErrorCount} error(s)");
        }
        else
        {
            UpdateStatus("Compilation successful");
        }
    }

    private void UpdateStatus(string message)
    {
        statusLabel.Text = message;
    }

    #endregion
}

/// <summary>
/// Draws wavy red underlines under error spans in the editor.
/// </summary>
public sealed class ErrorHighlighter : IBackgroundRenderer
{
    private TextDocument? _document;
    private List<(int Offset, int Length, string Message)> _errors = new();

    public KnownLayer Layer => KnownLayer.Selection;

    public void UpdateErrors(TextDocument document, IReadOnlyList<CompilationError> errors)
    {
        _document = document;
        _errors = errors
            .Select(e => (
                Offset: GetOffset(document, e.Span),
                Length: Math.Max(1, e.Span.Length),
                Message: e.Message
            ))
            .Where(e => e.Offset >= 0)
            .ToList();
    }

    public void ClearErrors()
    {
        _errors.Clear();
    }

    private int GetOffset(TextDocument doc, SourceSpan span)
    {
        int lineIndex = span.Line - 1;
        if (lineIndex < 0 || lineIndex >= doc.LineCount)
            return -1;

        DocumentLine line = doc.GetLineByNumber(lineIndex + 1);
        int offset = line.Offset + Math.Min(span.Column, line.Length);
        return Math.Min(offset, doc.TextLength);
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (_document == null || _errors.Count == 0)
            return;

        textView.EnsureVisualLines();

        foreach (var (offset, length, _) in _errors)
        {
            if (offset < 0 || offset >= _document.TextLength)
                continue;

            var segment = new ErrorSegment(offset, length);
            foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
            {
                DrawWavyUnderline(drawingContext, rect);
            }
        }
    }

    private void DrawWavyUnderline(DrawingContext dc, Rect rect)
    {
        if (rect.Width <= 0)
            return;

        var pen = new Pen(new SolidColorBrush(Color.FromRgb(255, 80, 80)), 1.5)
        {
            DashStyle = new DashStyle(new double[] { 1, 3 }, 0)
        };

        dc.DrawLine(pen,
            new Point(rect.Left, rect.Bottom + 1),
            new Point(rect.Right, rect.Bottom + 1));
    }

    private readonly struct ErrorSegment : ICSharpCode.AvalonEdit.Document.ISegment
    {
        public int Offset { get; init; }
        public int Length { get; init; }
        public int EndOffset => Offset + Length;

        public ErrorSegment(int offset, int length)
        {
            Offset = offset;
            Length = length;
        }
    }
}