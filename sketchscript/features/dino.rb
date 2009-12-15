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
require 'bouncer'
def each_object target
  Bouncer.new rand(10) - 5, rand(10) - 5, canvas
end

#%pause

open 'interactive.rb'
window.clear_animations
cls
