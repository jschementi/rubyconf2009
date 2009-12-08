#
# wpf.rb gives nice helpers for gui apps
#
require 'wpf'

#
# GameEngine is the .NET namespace of the host app
#
include GameEngine 

#
# define horrible ugly globals since I'm a script writer and
# I don't know any better
#
$window = window
$canvas = canvas
$offset = 20
$rect_size = 20

$pretty_colors = [
  [0, 113, 118],
  [0,173,239],
  [68,199,244],
  [157,220,249],
  [255,235,149]
].map do |c|
  SolidColorBrush.new(Color.from_rgb(*c))
end

def random_pretty_color
  $pretty_colors[rand($pretty_colors.size)]
end

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
  rect.width, rect.height, rect.fill = $rect_size, $rect_size, random_pretty_color 
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
  b
end

##############################################################################
### Initialization Code
##############################################################################

#
# add a "show" and "hide" method to the "type", which hides/shows row "i"
#
def __generate_show_and_hide_methods(type, i)
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

def reset_host_state
  window.canvas_controls.children.clear
  window.output_controls.children.clear
  canvas.children.clear
  window.editor_toggle.mouse_down.remove @_mouse_down if @_mouse_down
  window.editor_toggle.mouse_enter.remove @_mouse_enter if @_mouse_enter
  window.editor_toggle.mouse_leave.remove @_mouse_leave if @_mouse_leave
end

#
# called by host automatically
#
def setup
  
  # give the canvas and output control areas show and hide methods
  __generate_show_and_hide_methods :canvas, 0
  __generate_show_and_hide_methods :output, 3

  reset_host_state
  
  # "light up" the editor toggle area -- so people know that
  # it actually does something
  @_mouse_down = lambda do |s, e|
    column = window.content.column_definitions[2]
    column.width = (column.width == GridLength.new(0)) ?
      GridLength.new(10, GridUnitType.star) :
      GridLength.new(0)
  end
  window.editor_toggle.mouse_down &@_mouse_down
  @_mouse_enter = lambda do |s, e|
    @__tempbrush = s.background
    s.background = SolidColorBrush.new(Colors.red)
  end
  window.editor_toggle.mouse_enter &@_mouse_enter
  @_mouse_leave = lambda do |s, e|
    s.background = @__tempbrush
  end
  window.editor_toggle.mouse_leave &@_mouse_leave
  window.editor_toggle.background = window.content.background

  # add some default buttons
  @default_canvas_buttons.each do |b|
    window.canvas_controls.children.remove b
  end if @default_canvas_controls
  @default_output_buttons.each do |b|
    window.output_controls.children.remove b
  end if @default_output_controls
  @default_canvas_buttons = []
  @default_output_buttons = []
  @default_canvas_buttons << (as_button("Random Square", window.canvas_controls) { |s,e|
    rand_square
  })
  @default_canvas_buttons << (as_button("Clear", window.canvas_controls) { |s,e|
    canvas.children.clear
  })
  @default_output_buttons << (as_button("Reload", window.output_controls) { |s,e|
    reload
  })

  # load the "totype" file into the interactive textbox
  window.code.text = File.read("../../../features/interactive.rb")

  as_button("Run next", window.output_controls) do |s,e|
    select_upto_next_pause
    window.run_code window.code
  end
end

def reload
  load 'basic.rb'
  setup
end

#
# selects code up to the next %pause comment
#

def select_upto_next_pause(indicatortxt = '#%pause')
  offset = window.code.selection_length == 0 ?
    0 : 
    window.code.selection_start + window.code.selection_length

  puts "offset:#{offset}"

  sections = window.code.text[offset..-1].split(/^#{indicatortxt}/)

  puts "num sections #{sections.length}"

  length = sections.first.length + indicatortxt.length

  puts "section length: #{length}"

  window.code.selection_start = offset
  window.code.selection_length = length
end
