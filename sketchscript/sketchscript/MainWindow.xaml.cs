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

using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using IronRuby.Builtins;

using AeroGlass;
using System.Diagnostics;

namespace SketchScript {

    public partial class MainWindow : Window {

        public Scripting _scripting { get; private set; }

        private bool _isCtrlPressed;
        public TextBoxBuffer TextBoxBuffer { get; internal set; }

        private readonly Timer _timer = new Timer();
        private bool _areCallbacksRegistered;
        public Action EachFrame { get; internal set; }
        public Func<object,
#if CLR2
            IObjectUpdater
#else
            dynamic
#endif

            > EachFrameAndObject { get; internal set; }
        private static DependencyProperty EachFrameAndObjectProperty = DependencyProperty.RegisterAttached("EachFrameAndObjectProperty", typeof(object), typeof(UIElement));

        public TextBox History { get { return _history; } }
        public TextBox Output { get { return _output; } }
        public TextBox Code { get { return _code; } }
        public StackPanel CanvasControls { get { return _canvasControls; } }
        public StackPanel OutputControls { get { return _outputControls; } }
        public GridSplitter EditorToggle { get { return _editorToggle; } }
        public GridSplitter ConsoleSplitter { get { return _consoleSplitter; } }

        public MainWindow() {
            InitializeComponent();

            this.SourceInitialized += (sender, e) => {
                GlassHelper.ExtendGlassFrame(this, new Thickness(-1));
            };

            this.Loaded += (s,e) => {

                _code.Text = @"# Welcome to SketchScript!

# All Ruby code typed here can be run by pressing
# Ctrl-Enter. If you don't want to run everything,
# just select the text you wan to run and press
# the same key combination.

# This is nothing more than a Ruby interpreter:
# Try the following; it will print to the output
# window below the code:

10.times{|i| puts i * i}

# basic.rb will reset this environment for some
# cool demos:

require 'basic'

# Check out the About tab for more information.";

                // When Ctrl-Enter is pressed, run the script code
                _code.KeyDown += (se, args) => {
                    if (args.Key == Key.LeftCtrl || args.Key == Key.RightCtrl)
                        _isCtrlPressed = true;
                    if (_isCtrlPressed && args.Key == Key.Enter)
                        RunCode(_code);
                };
                _code.KeyUp += (se, args) => {
                    if (args.Key == Key.LeftCtrl || args.Key == Key.RightCtrl)
                        _isCtrlPressed = false;
                };

                _scripting = new Scripting(this);

                // Get the Ruby engine by name.
                _scripting.SetCurrentEngine("Ruby");

                // Require a setup file and call the "setup" method
//                _scripting.RunFile("../../../features/basic.rb");
//#if CLR2
//                Action setup = null;
//                _scripting.CodeScope.TryGetVariable<Action>("setup", out setup);
//                if (setup != null) setup();
//#else
//                _scripting.CodeScope.setup();
//#endif
            };
        }

        public void RunCode(TextBox t) {
            string code = t.SelectionLength > 0 ? t.SelectedText : t.Text;
            _scripting.RunCode(code);
        }

        internal void RegisterCallbacks() {
            // Registers the callbacks that are fired 30 times a second.
            if (!_areCallbacksRegistered) {    
                // Run animations
                _timer.Elapsed += (sender, args) => {
                    _canvas.Dispatcher.BeginInvoke((Action) (() => {
                        try {
                            if (EachFrame != null)
                                EachFrame();

                            if (EachFrameAndObject != null) {
                                foreach (UIElement element in _canvas.Children) {
#if CLR2
                                    IObjectUpdater eachFrameAndObject = element.GetValue(EachFrameAndObjectProperty) as IObjectUpdater;
#else
                                    dynamic eachFrameAndObject = element.GetValue(EachFrameAndObjectProperty);
#endif
                                    if (eachFrameAndObject == null && EachFrameAndObject != null) {
                                        eachFrameAndObject = EachFrameAndObject(element);
                                        element.SetValue(EachFrameAndObjectProperty, eachFrameAndObject);
                                    }
                                    if (eachFrameAndObject != null) {
                                        eachFrameAndObject.update(element);
                                        // Note: if "tracker" was not dynamic, or not a IObjectUpdater, then you'd have to write this:
                                        // IronRuby.Ruby.GetEngine(Runtime).Operations.InvokeMember(eachFrameAndObject, "update", element);
                                    }
                                }
                            }
                        }
                        catch (Exception e) {
                            EachFrame = null;
                            EachFrameAndObject = null;
                            TextBoxBuffer.write("Error during callback: " + e.ToString());
                            _timer.Stop();
                            _areCallbacksRegistered = false;
                        }
                    }));
                };
                _timer.Interval = 1000 / 30;
                _timer.Start();
                _areCallbacksRegistered = true;
            }
        }

        public void StopAnimations() {
            _timer.Stop();
        }

        public void StartAnimations() {
            _timer.Start();
        }

        public void ClearAnimations() {
            _scripting.CodeScope.RemoveVariable("each_frame_and_object");
            _scripting.CodeScope.RemoveVariable("each_frame");
            EachFrame = null;
            EachFrameAndObject = null;
            foreach (UIElement element in _canvas.Children) {
                element.SetValue(EachFrameAndObjectProperty, null);
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e) {
            Uri uri = ((Hyperlink)sender).NavigateUri;
            if (uri != null) {
                if (!uri.IsAbsoluteUri)
                    throw new InvalidOperationException("An absolute URI is required.");
                System.Diagnostics.Process.Start(uri.ToString());
            }
        }

        //private bool neverRendered = true;

        //protected override void OnContentRendered(EventArgs e) {
        //    if (this.neverRendered) {
        //        // The window takes the size of its content because SizeToContent
        //        // is set to WidthAndHeight in the markup. We then allow
        //        // it to be set by the user, and have the content take the size
        //        // of the window.
        //        this.SizeToContent = SizeToContent.Manual;

        //        FrameworkElement root = this.Content as FrameworkElement;
        //        if (root != null) {
        //            root.Width = double.NaN;
        //            root.Height = double.NaN;
        //        }

        //        this.neverRendered = false;
        //    }

        //    base.OnContentRendered(e);
        //}
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

    public class Scripting {

        public ScriptRuntime Runtime { get; private set; }
        public 
#if CLR2
            ScriptScope 
#else
            dynamic
#endif
            CodeScope { get; private set; }
        private ScriptEngine _currentEngine;

        private MainWindow _window;
        private bool _isOutputRedirected;

        public Scripting(MainWindow window) {
            _window = window;
            InitializeHosting();
        }

        public void SetCurrentEngine(string engineName) {
            _currentEngine = Runtime.GetEngine(engineName);
        }

        public void RunFile(string path) {
            string code = File.ReadAllText(path);
            RunCode(code, path);
        }

        public void RunCode(string codeToRun) {
            RunCode(codeToRun, null);
        }

        public void RunCode(string codeToRun, string path) {
            try {
                RedirectOutput(_currentEngine);
                _window.RegisterCallbacks();

                // Executes the script code against the shared scope to persist
                // variables between executions.
                object result = null;
                if (path == null)
                    result = _currentEngine.Execute(codeToRun, CodeScope);
                else
                    result = _currentEngine.CreateScriptSourceFromString(codeToRun, path, SourceCodeKind.File).Execute(CodeScope);

                // Write the code and results to the history and output areas
                _window.History.AppendText(codeToRun);
#if CLR2
                CodeScope.SetVariable("_", result);
#else
                CodeScope._ = result;
#endif
                string repr = _currentEngine.Execute<string>("_.inspect", CodeScope);
                _window.TextBoxBuffer.write("=> " + repr + "\n");
                _window.History.AppendText("\n# => " + repr + "\n");
            } catch (Exception ex) {
                string formattedEx = _currentEngine.GetService<ExceptionOperations>().FormatException(ex);
                if (_isOutputRedirected)
                    _window.Dispatcher.BeginInvoke((Action)(() => _window.TextBoxBuffer.write(formattedEx)));
                else
                    System.Windows.MessageBox.Show(formattedEx);
            }

            // "each_frame" is called 30-times a second without any arguments.
            Action eachFrame = null;
            CodeScope.TryGetVariable<Action>("each_frame", out eachFrame);
            _window.EachFrame = eachFrame;

            // "each_frame_and_object" is called 30-times a second with one "object" argument,
            // is expected to return a IObjectUpdater
            Func<object,
#if CLR2
                IObjectUpdater
#else
                dynamic
#endif
                > eachFrameAndObject = null;
            CodeScope.TryGetVariable<Func<object,
#if CLR2
                IObjectUpdater
#else
                dynamic
#endif
                >>("each_frame_and_object", out eachFrameAndObject);
            _window.EachFrameAndObject = eachFrameAndObject;
        }

        // Loads assemblies and creates a scope for everything to run in
        private void InitializeHosting() {
            Runtime = ScriptRuntime.CreateFromConfiguration();
            Runtime.LoadAssembly(typeof(Canvas).Assembly);
            Runtime.LoadAssembly(typeof(Brushes).Assembly);
            Runtime.LoadAssembly(GetType().Assembly);
            CodeScope = Runtime.CreateScope();
#if CLR2
            CodeScope.SetVariable("canvas", _canvas);
            CodeScope.SetVariable("window", this);
#else
            CodeScope.window = _window;
            CodeScope.canvas = _window._canvas;
#endif
        }

        // Redirects Ruby's output to the _output TextBox
        private void RedirectOutput(ScriptEngine engine) {
            if (!_isOutputRedirected) {
                Action redirect = () => {
                    var outputScope = engine.CreateScope();
                    _window.TextBoxBuffer = new TextBoxBuffer(_window.Output);
                    outputScope.SetVariable("__output__", _window.TextBoxBuffer);
                    engine.Execute("$stdout = __output__", outputScope);
                };
                if (_window.Output.IsLoaded)
                    redirect();
                else
                    _window.Output.Loaded += (sender, args) => redirect();

                _isOutputRedirected = true;
            }
        }
    }

#if CLR2
    public interface IObjectUpdater {
        void update(object target);
    }
#endif
}