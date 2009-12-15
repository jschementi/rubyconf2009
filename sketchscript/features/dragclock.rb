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

open 'interactive.rb'
reset_interactive
