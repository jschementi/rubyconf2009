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

#if HOSTING
using Microsoft.Scripting.Hosting;
#endif

namespace GameEngine {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
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
        private Brush _tempBrush;

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

            /*
            _editorToggle.MouseDown += (s, e) => {
                var column = ((Grid)this.Content).ColumnDefinitions[2];
                if (column.Width == new GridLength(0)) {
                    column.Width = new GridLength(9, GridUnitType.Star);
                } else {
                    column.Width = new GridLength(0);
                }
            };
            _editorToggle.MouseEnter += (s, e) => {
                _tempBrush = _editorToggle.Fill;
                _editorToggle.Fill = new SolidColorBrush(Colors.Red);
            };
            _editorToggle.MouseLeave += (s, e) => {
                _editorToggle.Fill = _tempBrush;
            };
            _editorToggle.Fill = ((Grid) this.Content).Background;
            */

            InitializeHosting();
#if HOSTING
            var engine = Runtime.GetEngine("Ruby");
#endif

            // When Ctrl-Enter is pressed, run the code:
            _code.KeyDown += (sender, args) => {
                if (args.Key == Key.LeftCtrl || args.Key == Key.RightCtrl) {
                    _isCtrlPressed = true;
                }

                if (_isCtrlPressed && args.Key == Key.Enter) {
#if HOSTING
                    try {
                        if (!_isOutputRedirected) {
                            RedirectOutput(engine);
                            _isOutputRedirected = true;
                        }
                        if (!_areCallbacksRegistered) {
                            RegisterCallbacks();
                            _areCallbacksRegistered = true;
                        }
                        
                        object result = engine.Execute(_code.Text, CodeScope);
                        
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

                    Action callbackAction = null;
                    CodeScope.TryGetVariable<Action>("callback", out callbackAction);
                    _callback = callbackAction;

                    Func<object, IObjectUpdater> tracker = null;
                    CodeScope.TryGetVariable<Func<object, IObjectUpdater>>("tracker", out tracker);
                    _trackerMaker = tracker;
#endif
                }
            };
            _code.KeyUp += (sender, args) => {
                if (args.Key == Key.LeftCtrl || args.Key == Key.RightCtrl) {
                    _isCtrlPressed = false;
                }
            };
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
                                    tracker.Update(element);
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

    // Implement this interface, and the Update method gets called 30 times a second
    public interface IObjectUpdater {
        void Update(object target);
    }
}
