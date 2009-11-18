include System::Windows::Controls
include System::Windows::Shapes
include System::Windows::Media
include GameEngine 

class System::Windows::Media::Brushes
  def self.random
    send colors[rand(colors.size)]
  end

  def self.colors
    public_methods(false) - Object.public_methods - ['rand', 'colors']
  end
end

$offset = 20
$rect_size = 20

def random_square
  rect = Rectangle.new
  rect.width, rect.height, rect.fill = $rect_size, $rect_size, Brushes.random
  canvas.children.add rect
  Canvas.set_left rect, rand(canvas.actual_width - $offset)
  Canvas.set_top  rect, rand(canvas.actual_height - $offset)
end

def random_squares(count = 100)
  count.times{ |i| random_square }
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
