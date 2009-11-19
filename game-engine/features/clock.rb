require 'wpf'

include System::Windows::Markup

class Clock 
  def initialize(root)
    @root = root
    @canvas = Canvas.new
  end
  
  def load(xaml)
    path = "#{$:.first}#{xaml}"
    data = File.open(path, 'r'){|f| f.read}
    stringReader = System::IO::StringReader.new(data)
    load_assembly 'System.Xml'
    xmlReader = System::Xml::XmlReader.create(stringReader)
    @canvas = XamlReader.load xmlReader
  end
  
  def self.show
    @clock = Clock.new(Object.canvas)
    @clock.load 'clock.xaml'
    @clock.root.children.add @clock.canvas
    @clock.set_hands Time.now
    @clock
  end
  
  def canvas
    @canvas
  end
  
  def root
    @root
  end
  
  def left=(x)
    Canvas.set_left @canvas, x
  end
  def top=(y)
    Canvas.set_top  @canvas, y
  end
  
  def set_hands(d)
    hour_animation.from    = from_angle  d.hour, 1, d.min/2
    hour_animation.to      = to_angle    d.hour
    minute_animation.from  = from_angle  d.min
    minute_animation.to    = to_angle    d.min
    second_animation.from  = from_angle  d.sec
    second_animation.to    = to_angle    d.sec
  end

  def from_angle(time, divisor = 5, offset = 0)
    ((time / (12.0 * divisor)) * 360) + offset + 180
  end

  def to_angle(time)
    from_angle(time) + 360
  end
  
  def move(x,y)
    root.left, root.top = x, y
  end
  
  def method_missing(m)
    canvas.send(m)
  end
end
