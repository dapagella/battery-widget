using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace BatteryWidget
{
    /// <summary>Design tokens + control styles. One neutral surface, color only where it carries meaning (battery level).</summary>
    public static class Theme
    {
        // ---- color tokens ----
        public static readonly Color Text = Color.FromRgb(0xF2, 0xF4, 0xF7);
        public static readonly Color TextDim = Color.FromRgb(0xA6, 0xAD, 0xBA);
        public static readonly Color Good = Color.FromRgb(0x3D, 0xD6, 0x8C);
        public static readonly Color Warn = Color.FromRgb(0xF5, 0xB9, 0x42);
        public static readonly Color Bad = Color.FromRgb(0xFF, 0x5A, 0x5F);
        public static readonly Color Neutral = Color.FromRgb(0x8B, 0x93, 0xA1);
        public static readonly Color Hairline = Color.FromArgb(0x26, 0xFF, 0xFF, 0xFF);

        public static Color Level(int p)
        {
            if (p < 0) return Neutral;
            if (p > 60) return Good;
            if (p >= 25) return Warn;
            return Bad;
        }

        public static SolidColorBrush Brush(Color c) { var b = new SolidColorBrush(c); b.Freeze(); return b; }
        public static Color WithAlpha(Color c, byte a) { return Color.FromArgb(a, c.R, c.G, c.B); }

        // ---- timing tokens ----
        public static readonly TimeSpan Fast = TimeSpan.FromMilliseconds(120);
        public static readonly TimeSpan Medium = TimeSpan.FromMilliseconds(180);

        const string Xaml = @"
<ResourceDictionary xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>

 <Style x:Key='Ctx' TargetType='ContextMenu'>
  <Setter Property='OverridesDefaultStyle' Value='True'/>
  <Setter Property='SnapsToDevicePixels' Value='True'/>
  <Setter Property='Template'><Setter.Value>
   <ControlTemplate TargetType='ContextMenu'>
    <Border Background='#F51B1E26' BorderBrush='#33FFFFFF' BorderThickness='1' CornerRadius='10' Padding='4'>
     <ItemsPresenter/>
    </Border>
   </ControlTemplate>
  </Setter.Value></Setter>
 </Style>

 <Style x:Key='Item' TargetType='MenuItem'>
  <Setter Property='OverridesDefaultStyle' Value='True'/>
  <Setter Property='SnapsToDevicePixels' Value='True'/>
  <Setter Property='FontFamily' Value='Segoe UI Variable Text, Segoe UI'/>
  <Setter Property='FontSize' Value='12.5'/>
  <Setter Property='Template'><Setter.Value>
   <ControlTemplate TargetType='MenuItem'>
    <Grid>
     <Border x:Name='Bd' Background='Transparent' CornerRadius='6' Margin='0,1' Padding='8,7,10,7'>
      <Grid>
       <Grid.ColumnDefinitions>
        <ColumnDefinition Width='22'/><ColumnDefinition Width='*' MinWidth='140'/><ColumnDefinition Width='16'/>
       </Grid.ColumnDefinitions>
       <Path x:Name='Chk' Data='M1,5.5 L4.5,9 L11,1.5' Stroke='#F2F4F7' StrokeThickness='1.7' StrokeStartLineCap='Round' StrokeEndLineCap='Round' Visibility='Collapsed' VerticalAlignment='Center' HorizontalAlignment='Left' Margin='2,0,0,0'/>
       <ContentPresenter Grid.Column='1' ContentSource='Header' RecognizesAccessKey='True' TextElement.Foreground='#F2F4F7' VerticalAlignment='Center'/>
       <Path x:Name='Arr' Grid.Column='2' Data='M0,0 L4,4 L0,8' Stroke='#99F2F4F7' StrokeThickness='1.4' Visibility='Collapsed' VerticalAlignment='Center' HorizontalAlignment='Right'/>
      </Grid>
     </Border>
     <Popup x:Name='PART_Popup' Placement='Right' HorizontalOffset='-3' VerticalOffset='-5' IsOpen='{TemplateBinding IsSubmenuOpen}' AllowsTransparency='True' Focusable='False' PopupAnimation='Fade'>
      <Border Background='#F51B1E26' BorderBrush='#33FFFFFF' BorderThickness='1' CornerRadius='10' Padding='4'>
       <ItemsPresenter KeyboardNavigation.DirectionalNavigation='Cycle'/>
      </Border>
     </Popup>
    </Grid>
    <ControlTemplate.Triggers>
     <Trigger Property='Role' Value='SubmenuHeader'><Setter TargetName='Arr' Property='Visibility' Value='Visible'/></Trigger>
     <Trigger Property='IsChecked' Value='True'><Setter TargetName='Chk' Property='Visibility' Value='Visible'/></Trigger>
     <Trigger Property='IsHighlighted' Value='True'><Setter TargetName='Bd' Property='Background' Value='#26FFFFFF'/></Trigger>
     <Trigger Property='IsEnabled' Value='False'><Setter Property='Opacity' Value='0.4'/></Trigger>
    </ControlTemplate.Triggers>
   </ControlTemplate>
  </Setter.Value></Setter>
 </Style>

 <Style x:Key='Sep' TargetType='Separator'>
  <Setter Property='Template'><Setter.Value>
   <ControlTemplate TargetType='Separator'><Border Height='1' Background='#22FFFFFF' Margin='8,4'/></ControlTemplate>
  </Setter.Value></Setter>
 </Style>

 <Style x:Key='Sld' TargetType='Slider'>
  <Setter Property='OverridesDefaultStyle' Value='True'/>
  <Setter Property='Focusable' Value='False'/>
  <Setter Property='Template'><Setter.Value>
   <ControlTemplate TargetType='Slider'>
    <Grid Height='22'>
     <Border Height='4' CornerRadius='2' Background='#33FFFFFF' VerticalAlignment='Center'/>
     <Track x:Name='PART_Track'>
      <Track.DecreaseRepeatButton>
       <RepeatButton Command='Slider.DecreaseLarge' Focusable='False'>
        <RepeatButton.Template><ControlTemplate TargetType='RepeatButton'><Border Height='4' CornerRadius='2' Background='#FFF2F4F7' VerticalAlignment='Center'/></ControlTemplate></RepeatButton.Template>
       </RepeatButton>
      </Track.DecreaseRepeatButton>
      <Track.IncreaseRepeatButton>
       <RepeatButton Command='Slider.IncreaseLarge' Focusable='False'>
        <RepeatButton.Template><ControlTemplate TargetType='RepeatButton'><Border Background='Transparent' Height='22'/></ControlTemplate></RepeatButton.Template>
       </RepeatButton>
      </Track.IncreaseRepeatButton>
      <Track.Thumb>
       <Thumb Focusable='False'>
        <Thumb.Template><ControlTemplate TargetType='Thumb'><Ellipse Width='16' Height='16' Fill='#FFF2F4F7' Stroke='#40000000' StrokeThickness='1'/></ControlTemplate></Thumb.Template>
       </Thumb>
      </Track.Thumb>
     </Track>
    </Grid>
   </ControlTemplate>
  </Setter.Value></Setter>
 </Style>

 <Style x:Key='Ghost' TargetType='Button'>
  <Setter Property='OverridesDefaultStyle' Value='True'/>
  <Setter Property='Focusable' Value='False'/>
  <Setter Property='Cursor' Value='Hand'/>
  <Setter Property='Template'><Setter.Value>
   <ControlTemplate TargetType='Button'>
    <Border x:Name='Bd' CornerRadius='6' Padding='8,4' Background='Transparent'>
     <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center' TextElement.Foreground='#F2F4F7'/>
    </Border>
    <ControlTemplate.Triggers>
     <Trigger Property='IsMouseOver' Value='True'><Setter TargetName='Bd' Property='Background' Value='#22FFFFFF'/></Trigger>
     <Trigger Property='IsPressed' Value='True'><Setter TargetName='Bd' Property='Background' Value='#38FFFFFF'/></Trigger>
    </ControlTemplate.Triggers>
   </ControlTemplate>
  </Setter.Value></Setter>
 </Style>

 <ControlTemplate x:Key='Tip' TargetType='ToolTip'>
  <Border Background='#F51B1E26' BorderBrush='#33FFFFFF' BorderThickness='1' CornerRadius='9' Padding='12,9'><ContentPresenter/></Border>
 </ControlTemplate>
</ResourceDictionary>";

        static ResourceDictionary dict;
        static ResourceDictionary D { get { if (dict == null) dict = (ResourceDictionary)XamlReader.Parse(Xaml); return dict; } }

        public static Style ContextMenuStyle { get { return (Style)D["Ctx"]; } }
        public static Style MenuItemStyle { get { return (Style)D["Item"]; } }
        public static Style SeparatorStyle { get { return (Style)D["Sep"]; } }
        public static Style SliderStyle { get { return (Style)D["Sld"]; } }
        public static Style GhostButtonStyle { get { return (Style)D["Ghost"]; } }
        public static ControlTemplate TipTemplate { get { return (ControlTemplate)D["Tip"]; } }
    }
}
