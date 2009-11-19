include System::Windows
include System::Windows::Controls
include System::Windows::Media
include System::Windows::Shapes

class FrameworkElement
  def method_missing(m)
    find_name(m.to_s.to_clr_string)
  end
end

class System::Windows::Media::Brushes
  def self.random
    send colors[rand(colors.size)]
  end

  def self.colors
    public_methods(false) - Object.public_methods - ['rand', 'colors']
  end
end

