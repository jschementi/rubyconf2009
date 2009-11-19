require 'wpf'

include GameEngine 

$window = window

def cls
  canvas.children.clear
end

def generate_show_and_hide_methods(type, i)
  self.instance_eval %{
    class << window.#{type}_controls
      def show
        $window.content.row_definitions[#{i}].height = GridLength.new(35)
      end
      def hide
        $window.content.row_definitions[#{i}].height = GridLength.new(12)
      end
    end
  }
end

def enable_toggle_editor
  window.editor_toggle.mouse_down do |s, e|
    column = window.content.column_definitions[2]
    column.width = (column.width == GridLength.new(0)) ?
      GridLength.new(9, GridUnitType.star) :
      GridLength.new(0)
  end
  window.editor_toggle.mouse_enter do |s, e|
    @__tempbrush = s.fill
    s.fill = SolidColorBrush.new(Colors.red)
  end
  window.editor_toggle.mouse_leave do |s, e|
    s.fill = @__tempbrush
  end
  window.editor_toggle.fill = window.content.background
  true
end

$offset = 20
$rect_size = 20

def rand_square
  rect = Rectangle.new
  rect.width, rect.height, rect.fill = $rect_size, $rect_size, Brushes.random
  canvas.children.add rect
  Canvas.set_left rect, rand(canvas.actual_width - $offset)
  Canvas.set_top  rect, rand(canvas.actual_height - $offset)
  rect
end

def rand_squares(count = 100)
  count.times{ |i| rand_square }
end

$dim = [canvas.actual_width - $offset, canvas.actual_height - $offset].min / 2

def large_circle
  (0..360).step(10) do |i|
    rect = Rectangle.new
    rect.width, rect.height, rect.fill = $rect_size, $rect_size, Brushes.random
    canvas.children.add rect
    Canvas.set_top  rect, $dim * Math.sin(i * Math::PI*2/360) + $dim
    Canvas.set_left rect, $dim * Math.cos(i * Math::PI*2/360) + $dim
  end
end

def rotate_one_step
  canvas.children.each do |child|
    top, left = Canvas.get_top(child), Canvas.get_left(child)
    run = (left - $dim) / $dim
    rise = (top - $dim) / $dim
    angle = (Math.atan2 rise, run) + (Math::PI / 100)
    Canvas.set_top  child, $dim * Math.sin(angle) + $dim
    Canvas.set_left child, $dim * Math.cos(angle) + $dim
  end
end

$canvas = canvas

class Tracker
  include IObjectUpdater

  def initialize xvelocity, yvelocity
    @xvelocity = xvelocity
    @yvelocity = yvelocity
  end

  def update target
    if (Canvas.get_left(target) + @xvelocity) >= ($canvas.actual_width - $offset)  or (Canvas.get_left(target) + @xvelocity) <= 0
      @xvelocity = -@xvelocity
    end
    if (Canvas.get_top(target)  + @yvelocity) >= ($canvas.actual_height - $offset) or (Canvas.get_top(target)  + @yvelocity) <= 0
      @yvelocity = -@yvelocity
    end
    Canvas.set_top  target, Canvas.get_top(target)  + @yvelocity
    Canvas.set_left target, Canvas.get_left(target) + @xvelocity
  end
end

def bounce target
  Tracker.new rand(10) - 5, rand(10) - 5
end

def drag obj
  require 'dragger'
  d = Dragger.new(obj, canvas)
  d.enable!
end

def clock time = Time.now
  require 'clock'
  clock = Clock.new canvas
  clock.load('clock.xaml')
  canvas.children.add clock.canvas
  clock.set_hands time
  clock
end

def as_button name, container, &block
  b = Button.new
  b.content = name
  b.click &block
  container.children.add b
  container.show
end

def setup_interface
  window.canvas_controls.children.clear
  window.output_controls.children.clear
  generate_show_and_hide_methods(:canvas, 0)
  generate_show_and_hide_methods(:output, 3)
  enable_toggle_editor
  as_button "Random Square", window.canvas_controls do |s,e|
    rand_squares
  end
  as_button "Large Circle", window.canvas_controls do |s,e|
    large_circle
  end
  as_button "Clear", window.canvas_controls do |s,e|
    canvas.children.clear
  end
  as_button "Clear", window.output_controls do |s,e|
    window.output.text = ''
  end
end
