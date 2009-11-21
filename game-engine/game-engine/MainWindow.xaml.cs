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
using Microsoft.Scripting.Hosting;
#endif

namespace GameEngine {
    public partial class MainWindow : Window {
#if HOSTING
        // Hosting fields
        public ScriptRuntime Runtime { get; private set; }
        public ScriptScope CodeScope { get; private set; }
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
        public Rectangle EditorToggle { get { return _editorToggle; } }

        public MainWindow() {
            InitializeComponent();

            InitializeHosting();
#if HOSTING
            // Get the Ruby engine by name.
            var engine = Runtime.GetEngine("Ruby");
#endif

            // When Ctrl-Enter is pressed, run the script code
            _code.KeyDown += (sender, args) => {
                if (args.Key == Key.LeftCtrl || args.Key == Key.RightCtrl) {
                    _isCtrlPressed = true;
                }

                if (_isCtrlPressed && args.Key == Key.Enter) {
#if HOSTING
                    try {
                        // Redirects all script output to the output textbox.
                        if (!_isOutputRedirected) {
                            RedirectOutput(engine);
                            _isOutputRedirected = true;
                        }

                        // Registers the callbacks that are fired 30 times a second.
                        if (!_areCallbacksRegistered) {
                            RegisterCallbacks();
                            _areCallbacksRegistered = true;
                        }
                        
                        // Executes the script code against the shared scope to persist
                        // variables between executions.
                        object result = engine.Execute(_code.Text, CodeScope);
                        
                        // Write the code and results to the history and output areas
                        _history.AppendText(_code.Text);
                        CodeScope.SetVariable("_", result);
                        string repr = engine.Execute<string>("_.inspect", CodeScope);
                        _textboxBuffer.write("=> " + repr + "\n");
                        _history.AppendText("\n# => " + repr + "\n");

                    } catch (Exception ex) {

                        string formattedEx = engine.GetService<ExceptionOperations>().FormatException(ex);
                        if (_isOutputRedirected) {
                            _textboxBuffer.write(formattedEx);
                        } else {
                            System.Windows.MessageBox.Show(formattedEx);
                        }
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
            };
            _code.KeyUp += (sender, args) => {
                if (args.Key == Key.LeftCtrl || args.Key == Key.RightCtrl) {
                    _isCtrlPressed = false;
                }
            };

#if HOSTING
            engine.CreateScriptSourceFromString(File.ReadAllText("../../../features/demo.rb")).Execute(CodeScope);
            Action setup = null;
            CodeScope.TryGetVariable<Action>("setup_interface", out setup);
            if (setup != null) {
                setup();
            }
#endif
        }

        private void RegisterCallbacks() {
            // Run animations
            _timer.Elapsed += (sender, args) => {
                _canvas.Dispatcher.BeginInvoke((Action) (() => {
                    try {
                        if (_callback != null) {
                            _callback();
                        }
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
            if (_output.IsLoaded) {
                redirect();
            } else {
                _output.Loaded += (sender, args) => redirect();
            }
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
