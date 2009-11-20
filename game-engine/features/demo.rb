require 'wpf'

include GameEngine 

#
# define horrible ugly globals since I'm a script writer and
# I don't know any better
#
$window = window
$canvas = canvas
$offset = 20
$rect_size = 20

#
# clear all objects from the canvas
#
def cls
  canvas.children.clear
end

#
# render a radom colored square on the canvas in a random position
#
def rand_square
  rect = Rectangle.new
  rect.width, rect.height, rect.fill = $rect_size, $rect_size, Brushes.random
  canvas.children.add rect
  Canvas.set_left rect, rand(canvas.actual_width - $offset)
  Canvas.set_top  rect, rand(canvas.actual_height - $offset)
  rect
end

#
# Add a button with "name" to a "container".
# OnClick, run the block
#
def as_button name, container, &block
  b = Button.new
  b.content = name
  b.click &block
  container.children.add b
  container.show
end

##############################################################################
### Initialization Code
##############################################################################

#
# add a "show" and "hide" method to the "type", which hides/shows row "i"
#
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

#
# Enable the area between the editor and canvas to toggle the
# visibility of the editor
#
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

def add_default_buttons
  as_button "Random Square", window.canvas_controls do |s,e|
    rand_square
  end
  as_button "Clear", window.canvas_controls do |s,e|
    canvas.children.clear
  end
end

#
# Initialize the interface; called from app by name
#
def setup_interface
  window.canvas_controls.children.clear
  window.output_controls.children.clear
  generate_show_and_hide_methods(:canvas, 0)
  generate_show_and_hide_methods(:output, 3)
  enable_toggle_editor
  add_default_buttons
end

