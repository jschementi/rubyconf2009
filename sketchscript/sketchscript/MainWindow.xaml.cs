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

#region Usings for running code
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using IronRuby.Builtins;
#endregion

using AeroGlass;
using System.Diagnostics;
using SThread = System.Threading;

namespace SketchScript {

    public partial class MainWindow : Window {

        #region Running code
        public Scripting _scripting { get; private set; }
        public TextBoxBuffer TextBoxBuffer { get; internal set; }
        private bool _isCtrlPressed;
        #endregion

        #region Animations
        public Action EachFrame { get; internal set; }
        public Func<object,
#if CLR2
            IObjectUpdater
#else
            dynamic
#endif
            > EachObject { get; internal set; }
        private static DependencyProperty EachObjectProperty = DependencyProperty.RegisterAttached("EachObjectProperty", typeof(object), typeof(UIElement));
        private readonly Timer _timer = new Timer();
        private bool _areCallbacksRegistered;
        #endregion

        #region UI accessors
        public TextBox History { get { return _history; } }
        public TextBox Output { get { return _output; } }
        public TextBox Code { get { return _code; } }
        public StackPanel CanvasControls { get { return _canvasControls; } }
        public StackPanel OutputControls { get { return _outputControls; } }
        public GridSplitter EditorToggle { get { return _editorToggle; } }
        public GridSplitter ConsoleSplitter { get { return _consoleSplitter; } }
        #endregion

        public MainWindow() {
            InitializeComponent();

            this.SourceInitialized += (sender, e) => {
                GlassHelper.ExtendGlassFrame(this, new Thickness(-1));
            };

            this.Loaded += (s,e) => {
                #region Wecome Text
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

# Check out the About tab for more information.

#
# Need to make fonts bigger for a demo?
#
DEMO = true
window.font_size = 16
window.width = 1024
window.height = 600
window.code.font_size = 18
window.history.font_size = 16
window.output.font_size = 18
window.find_name('_tabs').items.each do |i|
  i.font_size = 16
end
window.output.clear";
                #endregion
                KeyBindings();
                InitializeScripting();
            };
        }

        /// <summary>
        /// Runs all code from a TextBox if there is no selection, otherwise
        /// just runs the selection.
        /// </summary>
        /// <param name="t"></param>
        public void RunCode(TextBox t) {
            string code = t.SelectionLength > 0 ? t.SelectedText : t.Text;
            _scripting.RunCode(code);
        }

        /// <summary>
        /// Stops the animations managed by the animation timer
        /// </summary>
        public void StopAnimations() {
            _timer.Stop();
        }

        /// <summary>
        /// Starts the animations managed by the animation timer
        /// </summary>
        public void StartAnimations() {
            _timer.Start();
        }

        /// <summary>
        /// Clears the animations managed by the animation timer, which
        /// also gets rid of the animation callbacks, and the dependency
        /// properties placed on the canvas's children.
        /// </summary>
        public void ClearAnimations() {
            _scripting.ClearAnimations();
            EachFrame = null;
            EachObject = null;
            foreach (UIElement element in _canvas.Children) {
                element.SetValue(EachObjectProperty, null);
            }
        }

        /// <summary>
        /// Registers the callbacks that are fired 30 times a second.
        /// </summary>
        internal void RegisterCallbacks() {
            if (!_areCallbacksRegistered) {
                _timer.Elapsed += (sender, args) => _canvas.Dispatcher.BeginInvoke((Action) (() => {
                    try {
                        CallEachFrame();
                        CallEachObject();
                    }
                    catch (Exception e) {
                        EachFrame = null;
                        EachObject = null;
                        TextBoxBuffer.write("Error during callback: " + e.ToString());
                        _timer.Stop();
                        _areCallbacksRegistered = false;
                    }
                }));
                _timer.Interval = 1000 / 30;
                _timer.Start();
                _areCallbacksRegistered = true;
            }
        }

        private void InitializeScripting() {
            _scripting = new Scripting(this);

            // Get the Ruby engine by name.
            _scripting.SetCurrentEngine("Ruby");

            // Cute little trick: warm up the Ruby engine by running some code on another thread:
            new SThread.Thread(new SThread.ThreadStart(() => _scripting.CurrentEngine.Execute("2 + 2"))).Start();
        }

        private void KeyBindings() {
            // When Ctrl-Enter is pressed, run the script code
            _code.KeyDown += (se, args) =>
            {
                if (args.Key == Key.LeftCtrl || args.Key == Key.RightCtrl)
                    _isCtrlPressed = true;
                if (_isCtrlPressed && args.Key == Key.Enter)
                    RunCode(_code);
            };
            _code.KeyUp += (se, args) =>
            {
                if (args.Key == Key.LeftCtrl || args.Key == Key.RightCtrl)
                    _isCtrlPressed = false;
            };
        }

        /// <summary>
        /// Calls the EachFrame callback
        /// </summary>
        private void CallEachFrame() {
            if (EachFrame != null) EachFrame();
        }

        /// <summary>
        /// Calls the EachObject callback, which must "respond to" the "update" method.
        /// </summary>
        private void CallEachObject() {
            if (EachObject != null) {
                foreach (UIElement element in _canvas.Children) {
#if CLR2
                    IObjectUpdater eachObject = element.GetValue(EachObjectProperty) as IObjectUpdater;
#else
                    dynamic eachObject = element.GetValue(EachObjectProperty);
#endif
                    if (eachObject == null && EachObject != null) {
                        eachObject = EachObject(element);
                        element.SetValue(EachObjectProperty, eachObject);
                    }

                    if (eachObject != null) {
                        eachObject.update(element);
                        // Note: if "tracker" was not dynamic, or not a IObjectUpdater, then you'd have to write this:
                        // IronRuby.Ruby.GetEngine(Runtime).Operations.InvokeMember(eachFrameAndObject, "update", element);
                    }
                }
            }
        }

        /// <summary>
        /// When a hyperlink is clicked on, open it in the default browser.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e) {
            Uri uri = ((Hyperlink)sender).NavigateUri;
            if (uri != null) {
                if (!uri.IsAbsoluteUri)
                    throw new InvalidOperationException("An absolute URI is required.");
                System.Diagnostics.Process.Start(uri.ToString());
            }
        }
    }

#region Running code helpers
    /// <summary>
    /// Simple TextBox Buffer class
    /// </summary>
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

    /// <summary>
    /// Helper class for running code against the window
    /// </summary>
    public class Scripting {

        public ScriptRuntime Runtime { get; private set; }
        public 
#if CLR2
            ScriptScope 
#else
            dynamic
#endif
            CodeScope { get; private set; }
        public ScriptEngine CurrentEngine { get; private set; }

        private MainWindow _window;
        private bool _isOutputRedirected;
        private const string _eachFrameName = "each_frame";
        private const string _eachObjectName = "each_object";

        public Scripting(MainWindow window) {
            _window = window;
            InitializeHosting();
        }

        public void SetCurrentEngine(string engineName) {
            CurrentEngine = Runtime.GetEngine(engineName);
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
                RedirectOutput();
                _window.RegisterCallbacks();
                var result = Execute(codeToRun, path);
                ReportResult(codeToRun, result);
            } catch (Exception ex) {
                FormatDynamicException(ex);
            }
            CaptureAnimationCallbacks();
        }

        public void ClearAnimations() {
            CodeScope.RemoveVariable(_eachFrameName);
            CodeScope.RemoveVariable(_eachObjectName);
        }
        
        /// <summary>
        /// Loads assemblies and creates a scope for everything to run in
        /// </summary>
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

        private object Execute(string codeToRun, string path) {
            if (path == null) return CurrentEngine.Execute(codeToRun, CodeScope);
            return CurrentEngine.CreateScriptSourceFromString(codeToRun, path, SourceCodeKind.File).Execute(CodeScope);
        }

        /// <summary>
        /// Write the code and results to the history and output areas
        /// </summary>
        private void ReportResult(string codeToRun, object result) {
            _window.History.AppendText(codeToRun);
#if CLR2
            CodeScope.SetVariable("_", result);
#else
            CodeScope._ = result;
#endif
            string repr = CurrentEngine.Execute<string>("_.inspect", CodeScope);
            _window.TextBoxBuffer.write("=> " + repr + "\n");
            _window.History.AppendText("\n# => " + repr + "\n");
        }

        private void FormatDynamicException(Exception ex) {
            string formattedEx = CurrentEngine.GetService<ExceptionOperations>().FormatException(ex);
            if (_isOutputRedirected)
                _window.Dispatcher.BeginInvoke((Action)(() => _window.TextBoxBuffer.write(formattedEx)));
            else
                System.Windows.MessageBox.Show(formattedEx);
        }

        private void CaptureAnimationCallbacks() {
            // called 30-times a second without any arguments.
            Action eachFrame = null;
            CodeScope.TryGetVariable<Action>(_eachFrameName, out eachFrame);
            _window.EachFrame = eachFrame;

            // called once to get a IObjectUpdater (or dynamic)
            // which has an "update" method, which is called 30-times a second
            // for each object in the canvas.
            Func<object,
#if CLR2
                IObjectUpdater
#else
                dynamic
#endif
                > eachObject = null;
            CodeScope.TryGetVariable<Func<object,
#if CLR2
                IObjectUpdater
#else
                dynamic
#endif
                >>(_eachObjectName, out eachObject);
            _window.EachObject = eachObject;
            #endregion
        }

        private void RedirectOutput() {
            if (!_isOutputRedirected) {
                Action redirect = () => {
                    var outputScope = CurrentEngine.CreateScope();
                    _window.TextBoxBuffer = new TextBoxBuffer(_window.Output);
                    outputScope.SetVariable("__output__", _window.TextBoxBuffer);
                    CurrentEngine.Execute("$stdout = __output__", outputScope);
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