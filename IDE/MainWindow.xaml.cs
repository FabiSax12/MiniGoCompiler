using System.Diagnostics;
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
    // ── RoutedCommands for F5/F6 key bindings declared in XAML ──────────────
    public static readonly RoutedCommand BuildCommand = new();
    public static readonly RoutedCommand RunCommand   = new();

    private string? _currentFilePath;
    private string? _rootFolder;
    private DispatcherTimer _compileDebounceTimer = null!;
    private ErrorHighlighter? _errorHighlighter;
    private Process? _currentBuildProcess;

    // ── Panel collapse / drag state ───────────────────────────────────────────
    private bool   _errorListCollapsed = false;
    private bool   _outputCollapsed    = false;
    private double _savedErrorsHeight  = 130;
    private double _savedOutputHeight  = 110;

    // drag state for errors handle
    private bool   _errorsDragging    = false;
    private double _errorsDragStartY  = 0;
    private double _errorsDragStartH  = 0;

    // drag state for output handle
    private bool   _outputDragging    = false;
    private double _outputDragStartY  = 0;
    private double _outputDragStartH  = 0;

    public MainWindow()
    {
        InitializeComponent();
        LoadSyntaxHighlighting();
        SetupErrorHighlighter();
        SetupDebounce();
        RegisterBuildRunCommands();
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
            welcomeOverlay.Visibility = Visibility.Collapsed;
            explorerOpenFolderButton.Visibility = Visibility.Hidden;
            explorerCloseFolderButton.Visibility = Visibility.Visible;
            menuCloseFolder.IsEnabled = true;
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
                Filter = "MiniGo Files (*.go;*.g;*.txt)|*.go;*.g;*.txt|All Files (*.*)|*.*"
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

    #region Panel Collapse/Expand

    private void OnToggleErrorListClick(object sender, RoutedEventArgs e)
    {
        if (_errorListCollapsed)
        {
            _errorListCollapsed = false;
            errorsContentRow.Height = new GridLength(1, GridUnitType.Star);
            errorsPanelRow.Height   = new GridLength(_savedErrorsHeight, GridUnitType.Pixel);
            toggleErrorListButton.Content = "▼";
        }
        else
        {
            _savedErrorsHeight = errorsPanelRow.ActualHeight > 0
                ? errorsPanelRow.ActualHeight : _savedErrorsHeight;
            _errorListCollapsed = true;
            errorsContentRow.Height = new GridLength(0, GridUnitType.Pixel);
            errorsPanelRow.Height   = new GridLength(1, GridUnitType.Auto);
            toggleErrorListButton.Content = "▶";
        }
    }

    private void OnToggleOutputClick(object sender, RoutedEventArgs e)
    {
        if (_outputCollapsed)
        {
            _outputCollapsed = false;
            outputContentRow.Height = new GridLength(1, GridUnitType.Star);
            outputPanelRow.Height   = new GridLength(_savedOutputHeight, GridUnitType.Pixel);
            toggleOutputButton.Content = "▼";
        }
        else
        {
            _savedOutputHeight = outputPanelRow.ActualHeight > 0
                ? outputPanelRow.ActualHeight : _savedOutputHeight;
            _outputCollapsed = true;
            outputContentRow.Height = new GridLength(0, GridUnitType.Pixel);
            outputPanelRow.Height   = new GridLength(1, GridUnitType.Auto);
            toggleOutputButton.Content = "▶";
        }
    }

    #endregion

    #region Panel Drag-to-Resize

    /// <summary>
    /// Returns the maximum px the two panels can occupy combined so that
    /// the editor row (*) keeps at least 25% of the editor-column grid height.
    /// </summary>
    private double MaxPanelsHeight()
    {
        // The parent of errorsPanelGrid is the Grid in Column 2.
        // Its ActualHeight = TabBar + Editor + ErrorsPanel + OutputPanel + StatusBar.
        double totalH = errorsPanelGrid.Parent is FrameworkElement col2
            ? col2.ActualHeight
            : ActualHeight;

        // Fixed rows: TabBar (row 0) and StatusBar (row 4) — read actual rendered heights.
        double tabBarH    = fileNameLabel.ActualHeight + 8 + 8; // text + top/bottom padding
        double statusBarH = statusLabel.ActualHeight   + 8 + 8;

        // Editor must keep at least 25% of the total column height.
        double minEditorH = totalH * 0.25;

        double maxPanels = totalH - tabBarH - statusBarH - minEditorH;
        return Math.Max(0, maxPanels);
    }

    // ── Errors drag handle ────────────────────────────────────────────────────
    private void OnErrorsDragHandleMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_errorListCollapsed) return;
        _errorsDragging   = true;
        _errorsDragStartY = e.GetPosition(this).Y;
        _errorsDragStartH = errorsPanelRow.ActualHeight;
        errorsDragHandle.CaptureMouse();
        e.Handled = true;
    }

    private void OnErrorsDragHandleMouseMove(object sender, MouseEventArgs e)
    {
        if (!_errorsDragging) return;
        double delta  = _errorsDragStartY - e.GetPosition(this).Y;
        double newH   = Math.Max(32, _errorsDragStartH + delta);

        // Clamp: errors + output combined must not starve the editor below 25%
        double maxErrors = MaxPanelsHeight() - outputPanelRow.ActualHeight;
        newH = Math.Min(newH, Math.Max(32, maxErrors));

        errorsPanelRow.Height = new GridLength(newH, GridUnitType.Pixel);
        e.Handled = true;
    }

    private void OnErrorsDragHandleMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_errorsDragging) return;
        _errorsDragging = false;
        errorsDragHandle.ReleaseMouseCapture();
        e.Handled = true;
    }

    // ── Output drag handle ────────────────────────────────────────────────────
    private void OnOutputDragHandleMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_outputCollapsed) return;
        _outputDragging   = true;
        _outputDragStartY = e.GetPosition(this).Y;
        _outputDragStartH = outputPanelRow.ActualHeight;
        outputDragHandle.CaptureMouse();
        e.Handled = true;
    }

    private void OnOutputDragHandleMouseMove(object sender, MouseEventArgs e)
    {
        if (!_outputDragging) return;
        double delta = _outputDragStartY - e.GetPosition(this).Y;
        double newH  = Math.Max(32, _outputDragStartH + delta);

        // Clamp: errors + output combined must not starve the editor below 25%
        double maxOutput = MaxPanelsHeight() - errorsPanelRow.ActualHeight;
        newH = Math.Min(newH, Math.Max(32, maxOutput));

        outputPanelRow.Height = new GridLength(newH, GridUnitType.Pixel);
        e.Handled = true;
    }

    private void OnOutputDragHandleMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_outputDragging) return;
        _outputDragging = false;
        outputDragHandle.ReleaseMouseCapture();
        e.Handled = true;
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

        var errors = collector.GetSortedErrors();
        _errorHighlighter?.UpdateErrors(textEditor.Document, errors);
        var errorViewModels = errors
            .Select(e => new ErrorViewModel(e))
            .ToList();
        errorList.ItemsSource = errorViewModels;

        // Scroll to the last error if there are any
        if (errorViewModels.Count > 0)
        {
            errorList.ScrollIntoView(errorViewModels[errorViewModels.Count - 1]);
        }

        if (collector.HasErrors)
            UpdateStatus($"{collector.ErrorCount} error(s)");
        else
            UpdateStatus("Compilation successful");
    }

    private void OnErrorListDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (errorList.SelectedItem is not ErrorViewModel vm) return;

        int lineNumber = Math.Max(1, vm.Line);
        if (lineNumber > textEditor.Document.LineCount) return;

        var docLine = textEditor.Document.GetLineByNumber(lineNumber);
        textEditor.ScrollToLine(lineNumber);
        textEditor.CaretOffset = docLine.Offset + Math.Max(0, Math.Min(vm.Column, docLine.Length));
        textEditor.Focus();
    }

    private void UpdateStatus(string message)
    {
        statusLabel.Text = message;
    }

    #endregion

    #region Build and Run

    /// <summary>
    /// Wires the F5/F6 RoutedCommands declared as static fields to their click handlers.
    /// This lets the toolbar buttons and the key bindings share the same code path.
    /// </summary>
    private void RegisterBuildRunCommands()
    {
        CommandBindings.Add(new CommandBinding(BuildCommand, async (_, _) => await ExecuteBuildAsync()));
        CommandBindings.Add(new CommandBinding(RunCommand,   async (_, _) => await ExecuteRunAsync()));
    }

    private async void OnBuildClick(object sender, RoutedEventArgs e) => await ExecuteBuildAsync();
    private async void OnRunClick(object sender,  RoutedEventArgs e)  => await ExecuteRunAsync();

    private void OnStopClick(object sender, RoutedEventArgs e)
    {
        if (_currentBuildProcess != null && !_currentBuildProcess.HasExited)
        {
            try
            {
                _currentBuildProcess.Kill();
                SetOutput("Build process terminated");
                UpdateStatus("Build stopped");
            }
            catch (Exception ex)
            {
                SetOutput($"Error stopping build: {ex.Message}");
            }
        }
    }

    private void OnCloseFolderClick(object sender, RoutedEventArgs e)
    {
        _rootFolder = null;
        _currentFilePath = null;
        fileTree.ItemsSource = null;
        folderPathLabel.Text = "";
        textEditor.Text = "";
        fileNameLabel.Text = "No file open";
        errorList.ItemsSource = null;
        SetOutput("");
        _errorHighlighter?.ClearErrors();

        welcomeOverlay.Visibility = Visibility.Visible;
        explorerOpenFolderButton.Visibility = Visibility.Visible;
        explorerCloseFolderButton.Visibility = Visibility.Hidden;
        menuCloseFolder.IsEnabled = false;

        UpdateStatus("Folder closed");
    }

    /// <summary>
    /// Resolves the MiniGo.Compiler .csproj path relative to the IDE output directory.
    /// Layout: IDE/bin/Debug/net8.0-windows/ → up 4 levels → MiniGoCompiler/ → MiniGo.Compiler/
    /// </summary>
    private static string CompilerProjectPath()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.GetFullPath(
            Path.Combine(baseDir, "..", "..", "..", "..", "MiniGo.Compiler", "MiniGo.Compiler.csproj"));
    }

    private async Task ExecuteBuildAsync()
    {
        if (string.IsNullOrEmpty(_currentFilePath))
        {
            AppendOutput("No file open. Open a .go file before building.");
            return;
        }

        string compilerProject = CompilerProjectPath();
        if (!File.Exists(compilerProject))
        {
            AppendOutput($"Compiler project not found at:\n  {compilerProject}");
            return;
        }

        SetOutput("Building...");
        UpdateStatus("Building…");
        buildButton.IsEnabled = false;
        stopButton.IsEnabled = true;
        stopButton.Visibility = Visibility.Visible;

        try
        {
            var psi = new ProcessStartInfo("dotnet",
                $"run --project \"{compilerProject}\" --no-restore -- \"{_currentFilePath}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };

            _currentBuildProcess = Process.Start(psi)!;
            string stdout = await _currentBuildProcess.StandardOutput.ReadToEndAsync();
            string stderr = await _currentBuildProcess.StandardError.ReadToEndAsync();
            await _currentBuildProcess.WaitForExitAsync();

            string combined = (stdout + stderr).Trim();
            SetOutput(combined.Length > 0 ? combined : "(no output)");

            UpdateStatus(_currentBuildProcess.ExitCode == 0
                ? "Build succeeded"
                : $"Build failed (exit {_currentBuildProcess.ExitCode})");
        }
        catch (OperationCanceledException)
        {
            SetOutput("Build cancelled by user");
            UpdateStatus("Build cancelled");
        }
        catch (Exception ex)
        {
            SetOutput($"Failed to start compiler:\n{ex.Message}");
            UpdateStatus("Build error");
        }
        finally
        {
            buildButton.IsEnabled = true;
            stopButton.IsEnabled = false;
            stopButton.Visibility = Visibility.Hidden;
            _currentBuildProcess = null;
        }
    }

    private async Task ExecuteRunAsync()
    {
        if (string.IsNullOrEmpty(_currentFilePath))
        {
            AppendOutput("No file open.");
            return;
        }

        string llPath = Path.ChangeExtension(_currentFilePath, ".ll");
        if (!File.Exists(llPath))
        {
            SetOutput($"No .ll file found at:\n  {llPath}\nRun Build (F6) first.");
            return;
        }

        SetOutput($"Running {Path.GetFileName(llPath)}...\n");
        UpdateStatus("Running…");
        runButton.IsEnabled = false;

        string lliExe = ResolveLliPath();
        if (lliExe == null)
        {
            SetOutput(
                "Failed to start lli: executable not found.\n\n" +
                "Make sure LLVM is installed and 'lli' is on your PATH.\n" +
                "Common install locations:\n" +
                "  • winget install LLVM.LLVM  (adds C:\\Program Files\\LLVM\\bin to PATH)\n" +
                "  • https://releases.llvm.org/download.html  (Windows installer)\n\n" +
                "After installing, restart the IDE or add the LLVM bin folder to your system PATH.");
            UpdateStatus("Run error — lli not found");
            runButton.IsEnabled = true;
            return;
        }

        try
        {
            var psi = new ProcessStartInfo(lliExe, $"\"{llPath}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };

            using var proc = Process.Start(psi)!;
            string stdout = await proc.StandardOutput.ReadToEndAsync();
            string stderr = await proc.StandardError.ReadToEndAsync();
            await proc.WaitForExitAsync();

            string output = (stdout + (stderr.Length > 0 ? "\n[stderr]\n" + stderr : "")).TrimEnd();
            AppendOutput(output.Length > 0 ? output : "(no output)");
            UpdateStatus($"Exited with code {proc.ExitCode}");
        }
        catch (Exception ex)
        {
            SetOutput($"Failed to start lli:\n{ex.Message}\n\nMake sure LLVM is installed and 'lli' is on your PATH.");
            UpdateStatus("Run error");
        }
        finally
        {
            runButton.IsEnabled = true;
        }
    }

    private void SetOutput(string text)
    {
        outputPanel.Text = text + "\n";
        outputPanel.ScrollToEnd();
    }

    private void AppendOutput(string text)
    {
        outputPanel.Text += text + "\n";
        outputPanel.ScrollToEnd();
    }

    /// <summary>
    /// Resolves the path to the lli executable.
    /// First checks PATH via where/which, then probes common LLVM install locations on Windows.
    /// Returns null if lli cannot be found.
    /// </summary>
    private static string? ResolveLliPath()
    {
        // 1. Check if "lli" resolves on the system PATH.
        try
        {
            var probe = new ProcessStartInfo("lli", "--version")
            {
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };
            using var p = Process.Start(probe);
            p?.WaitForExit(2000);
            if (p?.ExitCode == 0 || p?.ExitCode == 1) // lli --version exits 0 or 1
                return "lli";
        }
        catch { /* not on PATH */ }

        // 2. Probe common Windows install locations.
        var candidates = new[]
        {
            @"C:\Program Files\LLVM\bin\lli.exe",
            @"C:\LLVM\bin\lli.exe",
        };

        foreach (var path in candidates)
            if (File.Exists(path))
                return path;

        return null;
    }

    #endregion
}

/// <summary>
/// Row model for the error list DataGrid.
/// Wraps a <see cref="CompilationError"/> into bindable flat properties.
/// </summary>
public sealed class ErrorViewModel
{
    public string Severity { get; }
    public int    Line     { get; }
    public int    Column   { get; }
    public string Message  { get; }

    public ErrorViewModel(CompilationError error)
    {
        Severity = error.Severity.ToString();
        Line     = error.Span.Line;
        Column   = error.Span.Column;
        Message  = error.Message;
    }
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