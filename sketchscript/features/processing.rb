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

#%pause

open 'interactive.rb'
reset_interactive
