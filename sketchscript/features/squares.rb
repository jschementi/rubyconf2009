# create a ton of random sized, colored, and stroked rectangles

include System::Windows::Shapes
include System::Windows::Media
include System::Windows::Controls

canvas.children.clear
colors = %W(red orange yellow lime blue purple violet)
brushes = colors.map{|c| SolidColorBrush.new(Colors.send(c)) }
canvas_dim = lambda{ [canvas.actual_width, canvas.actual_height] }

500.times do
  size = rand(canvas_dim[].max / 10)
  shape = Rectangle.new
  shape.width, shape.height = size, size
  shape.fill = brushes[rand brushes.size]
  shape.stroke = brushes[rand brushes.size]
  shape.stroke_thickness = rand(canvas_dim[].max / 70) + 4
  canvas.children.add shape
  Canvas.set_left shape, rand(canvas_dim[].first - size)
  Canvas.set_top  shape, rand(canvas_dim[].last  - size)
end

#%pause

require 'bouncer'
def each_object target
  Bouncer.new rand(10)-5, rand(10)-5, canvas
end

#%pause

open 'interactive.rb'
reset_interactive
