#
# render "count" random square
#
def rand_squares(count = 100)
  count.times{ |i| rand_square }
end




$dim = [canvas.actual_width - $offset, canvas.actual_height - $offset].min / 2
#
# create a circle of smaller squares
# 
def large_circle
  (0..360).step(10) do |i|
    rect = Rectangle.new
    rect.width, rect.height, rect.fill = $rect_size, $rect_size, Brushes.random
    canvas.children.add rect
    Canvas.set_top  rect, $dim * Math.sin(i * Math::PI*2/360) + $dim
    Canvas.set_left rect, $dim * Math.cos(i * Math::PI*2/360) + $dim
  end
end




#
# Given all objects on the canvas, snap them into a circle and
# rotate them. This method is called 30 times per second.
#
def callback
  canvas.children.each do |child|
    top, left = Canvas.get_top(child), Canvas.get_left(child)
    run = (left - $dim) / $dim
    rise = (top - $dim) / $dim
    angle = (Math.atan2 rise, run) + (Math::PI / 100)
    Canvas.set_top  child, $dim * Math.sin(angle) + $dim
    Canvas.set_left child, $dim * Math.cos(angle) + $dim
  end
end




# 
# IObjectUpdater implementation: calls the "update" method for each
# element in the canvas. "tracker" is called 30 times a second for
# EACH object on the canvas.
#
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

def tracker target
  Tracker.new rand(10) - 5, rand(10) - 5
end




#
# Enable an object to be dragged
#
def drag obj
  require 'dragger'
  d = Dragger.new(obj, canvas)
  d.enable!
end




#
# Render a clock for a given time
#
def clock time = Time.now
  require 'clock'
  clock = Clock.new canvas
  clock.load('clock.xaml')
  canvas.children.add clock.canvas
  clock.set_hands time
  clock
end


#
# Adds a bunch of buttons to fill out the interface
#
def add_rest_of_buttons
  as_button "100 Random Squares", window.canvas_controls do |s,e|
    rand_squares
  end
  as_button "Large Circle", window.canvas_controls do |s,e|
    large_circle
  end
  as_button "Clear", window.output_controls do |s,e|
    window.output.text = ''
  end
end




#
# Adds a bunch of buttons to fill out the interface
#
def add_rest_of_buttons
  as_button "100 Random Squares", window.canvas_controls do |s,e|
    rand_squares
  end
  as_button "Large Circle", window.canvas_controls do |s,e|
    large_circle
  end
  as_button "Clear", window.output_controls do |s,e|
    window.output.text = ''
  end
end

#
# ruby-processor demo 
#

class Processor
  def initialize klass
    @obj = klass.new
    @obj.setup($canvas)
    $canvas.mouse_left_button_down.add @obj.method(:mouse_pressed) if @obj.respond_to? :mouse_pressed
    $canvas.mouse_left_button_up.add @obj.method(:mouse_released) if @obj.respond_to? :mouse_released
    $canvas.mouse_move.add @obj.method(:mouse_dragged) if @obj.respond_to? :mouse_dragged
  end
    
  def update
    @obj.draw
  end
end

require 'circles'
$p = Processor.new Circles
def callback
  $p.update
end
