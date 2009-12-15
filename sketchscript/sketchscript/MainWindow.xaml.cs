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

        private ScriptEngine _rubyEngine;
        public ScriptEngine RubyEngine { get { return _rubyEngine; } }
        private IronRuby.Runtime.RubyContext _rubyContext;
        private ScriptScope _scope;
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

                OutputBuffer = new TextBoxBuffer(_output);

                // Initialize IronRuby
                var runtime = ScriptRuntime.CreateFromConfiguration();
                _rubyEngine = Ruby.GetEngine(runtime);
                _rubyContext = Ruby.GetExecutionContext(_rubyEngine);
                _scope = _rubyEngine.CreateScope();

                runtime.LoadAssembly(typeof(Canvas).Assembly);  // loads PresentationFramework
                runtime.LoadAssembly(typeof(Brushes).Assembly); // loads PresentationCore
                runtime.LoadAssembly(GetType().Assembly);       // loads this exe

                dynamic scope = _scope;
                scope.canvas = _canvas;
                scope.window = this;

                // Cute little trick: warm up the Ruby engine by running some code on another thread:
                new SThread.Thread(new SThread.ThreadStart(() => _rubyEngine.Execute("2 + 2", _scope))).Start();

                // redirect stdout to the output window
                _rubyContext.StandardOutput = OutputBuffer;

                RegisterCallbacks();

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

            CaptureAnimationCallbacks();
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
            _scope.RemoveVariable("each_frame");
            _scope.RemoveVariable("each_object");
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
            Action eachFrame = null;
            _scope.TryGetVariable<Action>("each_frame", out eachFrame);
            EachFrame = eachFrame;

            Func<object, dynamic> eachObject = null;
            _scope.TryGetVariable<Func<object, dynamic>>("each_object", out eachObject);
            EachObject = eachObject;
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
                        // Note: if "tracker" was not dynamic, or not a IObjectUpdater, then you'd have to write this:
                        // _rubyEngine.Operations.InvokeMember(eachFrameAndObject, "update", element);
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