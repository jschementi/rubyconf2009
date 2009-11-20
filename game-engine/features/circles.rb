class Circles
  def setup(container)
    @container = container
    @draw_on = false
    @d = @d_start = 10
    @color_set = [[0,113,118],
                  [0,173,239],
                  [68,199,244],
                  [157,220,249],
                  [255,235,149]]
  end
 
  def mouse_pressed(s,e)
    @draw_on = true
    @mouse = e
  end
 
  def mouse_released(s,e)
    @draw_on = false
    @d = @d_start
  end
 
  def mouse_dragged(s,e)
    @mouse = e
    @d += 1
  end
   
  def random_color
    @color_set[rand(@color_set.size)]
  end
 
  def random_transparency
    rand
  end
 
  def draw
    if @draw_on
      mouse_pos = @mouse.get_position @container
      my_circle(mouse_pos.x, mouse_pos.y, @d)
    end
  end
 
  def my_circle(x,y,d)
    cir = Ellipse.new
    cir.height = cir.width = d
    brush = SolidColorBrush.new(Color.from_rgb(*random_color))
    brush.opacity = random_transparency
    cir.fill = brush
    stroke_brush = SolidColorBrush.new(Color.from_rgb(*random_color))
    stroke_brush.opacity = random_transparency
    cir.stroke = stroke_brush
    cir.stroke_thickness = 10
    @container.children.add cir
    Canvas.set_left cir, x-d/2
    Canvas.set_top cir, y-d/2
  end
end
