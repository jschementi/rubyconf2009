class Dragger
  def initialize(obj, container)
    @click = nil
    @obj = obj
    @container = container
  end
  
  def enable!
    @obj.mouse_left_button_down do |s,e| 
      @click = e.get_position @obj
    end
    @container.mouse_left_button_up do |s,e|
      @click = nil
    end
    @obj.mouse_move do |s,e|
      if @click
        mouse_pos = e.get_position @container 
        Canvas.set_left @obj, mouse_pos.x - @click.x
        Canvas.set_top  @obj, mouse_pos.y - @click.y
      end
    end
    self
  end
end
