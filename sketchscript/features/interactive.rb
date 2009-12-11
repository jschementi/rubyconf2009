# Whoa, what just happened here?
# basic.rb ran, that's what. To see what
# exactly happened, run:
#
# open "basic.rb"
# 
# To walk through *this* file in demo-mode,
# just press the "Run Next" button to run 
# and highlight the code up until the next 
# %pause comment.

def reset_interactive
  @interactive_canvas_ctrls.each do |icc|
    window.canvas_controls.children.remove icc
  end if @interactive_canvas_ctrls
  @interactive_output_ctrls.each do |ioc|
    window.output_controls.children.remove ioc
  end if @interactive_output_ctrls
  @interactive_canvas_ctrls = []
  @interactive_output_ctrls = []
  window.clear_animations
  cls
end

reset_interactive

#
# Add a clear button for the output window
#
@interactive_output_ctrls << (as_button("Clear", window.output_controls) { |s,e|
  window.output.text = ''
})

#
# render "count" random squares
#
def random_squares(count = 200)
  count.times{ |i| random_square }
end

@interactive_canvas_ctrls << (as_button("Squares", window.canvas_controls) { |s,e|
  random_squares
})

random_squares

#%pause

#
# create a circle of smaller squares
# 
cls
@dim = [canvas.actual_width - @offset, canvas.actual_height - @offset].min / 2

def large_circle
  (0..360).step(10) do |i|
    rect = Rectangle.new
    rect.width, rect.height, rect.fill = @rect_size, @rect_size, random_pretty_color
    canvas.children.add rect
    Canvas.set_top  rect, @dim * Math.sin(i * Math::PI*2/360) + @dim
    Canvas.set_left rect, @dim * Math.cos(i * Math::PI*2/360) + @dim
  end
end

large_circle

@interactive_canvas_ctrls << (as_button("Circle", window.canvas_controls) { |s,e|
  large_circle
})

#%pause

#
# Given all objects on the canvas, snap them into a circle and
# rotate them. This method is called 30 times per second.
#
def each_frame
  canvas.children.each do |child|
    top, left = Canvas.get_top(child), Canvas.get_left(child)
    run = (left - @dim) / @dim
    rise = (top - @dim) / @dim
    angle = (Math.atan2 rise, run) + (Math::PI / 100)
    Canvas.set_top  child, @dim * Math.sin(angle) + @dim
    Canvas.set_left child, @dim * Math.cos(angle) + @dim
  end
end

#
# Add animation controls
#
@interactive_canvas_ctrls << (as_button(@_paused ? "Resume" : "Pause", window.canvas_controls) { |s,e|
  @_paused = !@_paused
  s.content = @_paused ? "Resume" : "Pause"
  @_paused ? window.stop_animations : window.start_animations
})
@interactive_canvas_ctrls << (as_button("Stop", window.canvas_controls) { |s,e|
  window.clear_animations
  @_paused = nil
  @interactive_canvas_ctrls[-2].content = "Pause"
})

#%pause

def each_frame; end
class Bouncer
  # Uncomment if host requires this interface to be implemented
  #include IObjectUpdater

  def initialize xvelocity, yvelocity, canvas, offset
    @xvelocity = xvelocity
    @yvelocity = yvelocity
    @canvas = canvas
    @offset = offset
  end

  def update target
    if (Canvas.get_left(target) + @xvelocity) >= (@canvas.actual_width - @offset)  or (Canvas.get_left(target) + @xvelocity) <= 0
      @xvelocity = -@xvelocity
    end
    if (Canvas.get_top(target)  + @yvelocity) >= (@canvas.actual_height - @offset) or (Canvas.get_top(target)  + @yvelocity) <= 0
      @yvelocity = -@yvelocity
    end
    Canvas.set_top  target, Canvas.get_top(target)  + @yvelocity
    Canvas.set_left target, Canvas.get_left(target) + @xvelocity
  end
end

def each_frame_and_object target
  Bouncer.new rand(10) - 5, rand(10) - 5, @canvas, @offset
end

#%pause

window.clear_animations
cls

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

drag clock.canvas

@interactive_canvas_ctrls << (as_button("Clock", window.canvas_controls) { |s,e|
  drag clock.canvas
})

#%pause

#
# ruby-processing demo 
#

cls

class Processing
  def initialize klass, canvas
    @obj = klass.new
    @obj.setup(canvas)
    canvas.mouse_left_button_down.add @obj.method(:mouse_pressed) if @obj.respond_to? :mouse_pressed
    canvas.mouse_left_button_up.add @obj.method(:mouse_released) if @obj.respond_to? :mouse_released
    canvas.mouse_move.add @obj.method(:mouse_dragged) if @obj.respond_to? :mouse_dragged
  end
    
  def update
    @obj.draw
  end
end

require 'circles'

@processing = Processing.new Circles, @canvas

def each_frame
  @processing.update
end
