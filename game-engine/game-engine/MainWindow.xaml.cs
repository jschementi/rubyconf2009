#define HOSTING
#define CLR2

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Timers;
using System.IO;

#if HOSTING
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using IronRuby.Builtins;
#endif

namespace GameEngine {
    public partial class MainWindow : Window {
#if HOSTING
        // Hosting fields
        public ScriptRuntime Runtime { get; private set; }
        public 
#if CLR2
            ScriptScope
#else
            dynamic
#endif
            CodeScope { get; private set; }
        private ScriptEngine _currentEngine;
#endif
        // IDE fields
        private bool _isCtrlPressed;
        private bool _isOutputRedirected;
        private TextBoxBuffer _textboxBuffer;

        // callback fields
        private readonly Timer _timer = new Timer();
        private bool _areCallbacksRegistered;
        private Action _callback;
        private Func<object,
#if CLR2
            IObjectUpdater
#else
            dynamic
#endif
            > _trackerMaker;
        private static DependencyProperty TrackerProperty = DependencyProperty.RegisterAttached("TrackerProperty", typeof(object), typeof(UIElement));

        // UI fields
        public TextBox History { get { return _history; } }
        public TextBox Output { get { return _output; } }
        public TextBox Code { get { return _code; } }
        public StackPanel CanvasControls { get { return _canvasControls; } }
        public StackPanel OutputControls { get { return _outputControls; } }
        public GridSplitter EditorToggle { get { return _editorToggle; } }

        public MainWindow() {
            InitializeComponent();

            this.Loaded += (s,e) => {
                InitializeHosting();
#if HOSTING
                // Get the Ruby engine by name.
                _currentEngine = Runtime.GetEngine("Ruby");
#endif

                // When Ctrl-Enter is pressed, run the script code
                _code.KeyDown += (sender, args) => {
                    if (args.Key == Key.LeftCtrl || args.Key == Key.RightCtrl)
                        _isCtrlPressed = true;

                    if (_isCtrlPressed && args.Key == Key.Enter) {
#if HOSTING
                        RunCode(_code);
                    }
                };
                _code.KeyUp += (sender, args) => {
                    if (args.Key == Key.LeftCtrl || args.Key == Key.RightCtrl)
                        _isCtrlPressed = false;
                };

#if HOSTING
                string path = "../../../features/basic.rb";
                string code = File.ReadAllText(path);
                RunCode(code, path);
#if CLR2
                Action setup = null;
                CodeScope.TryGetVariable<Action>("setup", out setup);
                if (setup != null) setup();
#else
                CodeScope.setup();
#endif
#endif
            };
        }

        public void RunCode(TextBox t) {
            string code = t.SelectionLength > 0 ? t.SelectedText : t.Text;
            RunCode(code);
        }

        public void RunCode(string codeToRun) {
            RunCode(codeToRun, null);
        }

        public void RunCode(string codeToRun, string path) {
            try {
                // Redirects all script output to the output textbox.
                if (!_isOutputRedirected) {
                    RedirectOutput(_currentEngine);
                    _isOutputRedirected = true;
                }

                // Registers the callbacks that are fired 30 times a second.
                if (!_areCallbacksRegistered) {
                    RegisterCallbacks();
                    _areCallbacksRegistered = true;
                }

                // Executes the script code against the shared scope to persist
                // variables between executions.
                object result = null;
                if (path == null)
                    result = _currentEngine.Execute(codeToRun, CodeScope);
                else
                    result = _currentEngine.CreateScriptSourceFromString(codeToRun, path, SourceCodeKind.File).Execute(CodeScope);

                // Write the code and results to the history and output areas
                _history.AppendText(codeToRun);
#if CLR2
                CodeScope.SetVariable("_", result);
#else
                        CodeScope._ = result;
#endif
                string repr = _currentEngine.Execute<string>("_.inspect", CodeScope);
                _textboxBuffer.write("=> " + repr + "\n");
                _history.AppendText("\n# => " + repr + "\n");
            } catch (Exception ex) {
                string formattedEx = _currentEngine.GetService<ExceptionOperations>().FormatException(ex);
                if (_isOutputRedirected)
                    this.Dispatcher.BeginInvoke((Action)(() => _textboxBuffer.write(formattedEx)));
                else
                    System.Windows.MessageBox.Show(formattedEx);
            }

            // "callback" is called 30-times a second without any arguments.
            Action callbackAction = null;
            CodeScope.TryGetVariable<Action>("callback", out callbackAction);
            _callback = callbackAction;

            // "tracker" is called 30-times a second with one "object" argument,
            // is expected to return a IObjectUpdater
            Func<object,
#if CLR2
                        IObjectUpdater
#else
                        dynamic
#endif
                > tracker = null;
            CodeScope.TryGetVariable<Func<object,
#if CLR2
                        IObjectUpdater
#else
                        dynamic
#endif
                >>("tracker", out tracker);
            _trackerMaker = tracker;
#endif
        }

        private void RegisterCallbacks() {
            // Run animations
            _timer.Elapsed += (sender, args) => {
                _canvas.Dispatcher.BeginInvoke((Action) (() => {
                    try {
                        if (_callback != null)
                            _callback();

                        if (_trackerMaker != null) {
                            foreach (UIElement element in _canvas.Children) {
#if CLR2
                                IObjectUpdater tracker = element.GetValue(TrackerProperty) as IObjectUpdater;
#else
                                dynamic tracker = element.GetValue(TrackerProperty);
#endif

                                if (tracker == null && _trackerMaker != null) {
                                    tracker = _trackerMaker(element);
                                    element.SetValue(TrackerProperty, tracker);
                                }
                                if (tracker != null) {
                                    tracker.update(element);
                                    // IronRuby.Ruby.GetEngine(Runtime).Operations.InvokeMember(tracker, "update", element);
                                }
                            }
                        }
                    }
                    catch (Exception e) {
                        _callback = null;
                        _trackerMaker = null;
                        _textboxBuffer.write("Error during callback: " + e.ToString());
                        _timer.Stop();
                        _areCallbacksRegistered = false;
                    }
                }));
            };
            _timer.Interval = 1000 / 30;
            _timer.Start();
        }

        public void StopAnimations() {
            _timer.Stop();
        }

        public void StartAnimations() {
            _timer.Start();
        }

        public void ClearAnimations() {
            CodeScope.RemoveVariable("tracker");
            CodeScope.RemoveVariable("callback");
            _callback = null;
            _trackerMaker = null;
            foreach (UIElement element in _canvas.Children) {
                element.SetValue(TrackerProperty, null);
            }
        }

        // Loads assemblies and creates a scope for everything to run in
        private void InitializeHosting() {
#if HOSTING
            Runtime = ScriptRuntime.CreateFromConfiguration();
            Runtime.LoadAssembly(typeof(Canvas).Assembly);
            Runtime.LoadAssembly(typeof(Brushes).Assembly);
            Runtime.LoadAssembly(GetType().Assembly);
            CreateScope();
        }
        private void CreateScope() {
            CodeScope = Runtime.CreateScope();
#if CLR2
            CodeScope.SetVariable("canvas", _canvas);
            CodeScope.SetVariable("window", this);
#else
            dynamic codeScope = CodeScope;
            codeScope.window = this;
            codeScope.canvas = _canvas;
#endif
#endif
        }

#if HOSTING
        // Redirects Ruby's output to the _output TextBox
        private void RedirectOutput(ScriptEngine engine) {
            Action redirect = () => {
                var outputScope = engine.CreateScope();
                _textboxBuffer = new TextBoxBuffer(_output);
                outputScope.SetVariable("__output__", _textboxBuffer);
                engine.Execute("$stdout = __output__", outputScope);
            };
            if (_output.IsLoaded)
                redirect();
            else
                _output.Loaded += (sender, args) => redirect();
        }
#endif
    }

    // Simple TextBox Buffer class
    public class TextBoxBuffer {
        TextBox box;
        public TextBoxBuffer(TextBox t) {
            box = t;
        }
        public void write(string str) {
            box.Dispatcher.BeginInvoke((Action) (() => {
                box.AppendText(str);
                box.ScrollToEnd();
            }));
        }
    }

#if CLR2
    // Implement this interface, and the Update method gets called 30 times a second
    public interface IObjectUpdater {
        void update(object target);
    }
#endif
}