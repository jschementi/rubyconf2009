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

using AeroGlass;
using System.Diagnostics;
using SThread = System.Threading;
using Microsoft.Scripting.Hosting;
using IronRuby;

namespace SketchScript {

    public partial class MainWindow : Window {

        #region Running code
        public TextBoxBuffer OutputBuffer { get; internal set; }
        private bool _isCtrlPressed;
        private bool _isOutputRedirected;

        private ScriptEngine _rubyEngine;
        private IronRuby.Runtime.RubyContext _rubyContext;
        private dynamic _scope;
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
                OutputBuffer = new TextBoxBuffer(_output);

                // Initialize IronRuby
                var runtime = ScriptRuntime.CreateFromConfiguration();
                _rubyEngine = Ruby.GetEngine(runtime);
                _rubyContext = Ruby.GetExecutionContext(_rubyEngine);
                _scope = _rubyEngine.CreateScope();

                // redirect stdout to the output window
                _rubyContext.StandardOutput = OutputBuffer;

                KeyBindings();
            };
        }

        /// <summary>
        /// Runs all code from a TextBox if there is no selection, otherwise
        /// just runs the selection.
        /// </summary>
        /// <param name="t"></param>
        public void RunCode(TextBox t) {
            string code = t.SelectionLength > 0 ? t.SelectedText : t.Text;

            // Run the code
            var result = _rubyEngine.Execute(code, _scope);

            // write the result to the output window

            var output = string.Format("=> {0}\n", _rubyContext.Inspect(result));
            OutputBuffer.write(output);

            // add the code to the history
            _history.AppendText(string.Format("{0}\n# {1}", code, output));
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
            EachFrame = null;
            EachObject = null;
            foreach (UIElement element in _canvas.Children) {
                element.SetValue(EachObjectProperty, null);
            }
        }

        /// <summary>
        /// Grabs a hold of any callbacks defined by user code
        /// </summary>
        internal void CaptureAnimationCallbacks() {
            EachFrame = null;
            // TODO: get the "EachFrame" callback

            EachObject = null;
            // TODO: get the "EachObject" callback
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
                        OutputBuffer.write("Error during callback: " + e.ToString());
                        _timer.Stop();
                        _areCallbacksRegistered = false;
                    }
                }));
                _timer.Interval = 1000 / 30;
                _timer.Start();
                _areCallbacksRegistered = true;
            }
        }

        /// <summary>
        /// When Ctrl-Enter is pressed, run the script code
        /// </summary>
        private void KeyBindings() {
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
                    }
                }
            }
        }

        private void RedirectOutput() {
            if (!_isOutputRedirected) {
                OutputBuffer = new TextBoxBuffer(Output);
                // TODO: tell IronRuby to use _window.TextBoxBuffer for output redirection
                _isOutputRedirected = true;
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

    /// <summary>
    /// Simple TextBox Buffer class
    /// </summary>
    public class TextBoxBuffer {
        private TextBox box;

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
    public interface IObjectUpdater {
        void update(object target);
    }
#endif
}