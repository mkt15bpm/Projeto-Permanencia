using System.IO;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace PermanenciaWpf;
public static class UI {
 public static Brush Ink=new SolidColorBrush(Color.FromRgb(22,50,83));
 public static Brush SaveInk=new SolidColorBrush(Color.FromRgb(43,125,205));
 public static TextBlock Text(string text,int size=14,bool bold=false)=>new(){Text=text,FontSize=size,FontWeight=bold?FontWeights.SemiBold:FontWeights.Normal,Foreground=Ink,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,4,0,8)};
 public static Button Button(string text,Action action,bool primary=false){var b=new Button{Content=text};if(primary){b.Background=text.StartsWith("Salvar",StringComparison.OrdinalIgnoreCase)?SaveInk:Ink;b.Foreground=Brushes.White;}b.Click+=(_,_)=>Try(action);return b;}
 public static void Try(Action action){try{action();}catch(Exception e){MessageBox.Show(e.Message,"Projeto Permanência",MessageBoxButton.OK,MessageBoxImage.Warning);}}
 public static bool Confirm(string message)=>MessageBox.Show(message,"Confirmar",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes;
 public static StackPanel Stack()=>new(){Margin=new Thickness(24)};
 public static Border Card(UIElement body)=>new(){Background=Brushes.White,CornerRadius=new CornerRadius(10),Padding=new Thickness(20),Margin=new Thickness(0,0,0,16),Child=body};
 public static TextBox Input(Panel p,string label,string value="",bool multi=false){p.Children.Add(Text(label));var t=new TextBox{Text=value,AcceptsReturn=multi,TextWrapping=multi?TextWrapping.Wrap:TextWrapping.NoWrap,MinHeight=multi?85:38,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};p.Children.Add(t);return t;}
 public static ComboBox Combo(Panel p,string label,IEnumerable<string> values,string value=""){p.Children.Add(Text(label));var c=new ComboBox{ItemsSource=values.ToArray(),SelectedItem=value};if(c.SelectedIndex<0)c.SelectedIndex=0;p.Children.Add(c);return c;}
 public static string Value(ComboBox c)=>c.SelectedItem?.ToString()??"";
 public static WrapPanel Actions(Panel p){var w=new WrapPanel();p.Children.Add(w);return w;}
 public static void KeepDialogOnTop(Window dialog,Window owner){dialog.Owner=owner;dialog.ShowInTaskbar=false;void BringForward(){if(!dialog.IsVisible)return;if(dialog.WindowState==WindowState.Minimized)dialog.WindowState=WindowState.Normal;dialog.Activate();dialog.Topmost=true;dialog.Topmost=false;dialog.Focus();}EventHandler ownerActivated=(_,_)=>BringForward();EventHandler stateChanged=(_,_)=>{if(dialog.WindowState==WindowState.Minimized)dialog.Dispatcher.BeginInvoke((Action)BringForward);};owner.Activated+=ownerActivated;dialog.StateChanged+=stateChanged;dialog.Closed+=(_,_)=>{owner.Activated-=ownerActivated;dialog.StateChanged-=stateChanged;};}
 public static Window Dialog(Window owner,string title,double width=680,double height=650){var dialog=new Window{Title=title,Width=width,Height=height,MinWidth=400,MinHeight=300,WindowStartupLocation=WindowStartupLocation.CenterOwner};KeepDialogOnTop(dialog,owner);return dialog;}
 public static Image Crest(double height=100){var b=new BitmapImage();b.BeginInit();b.UriSource=new Uri(Path.Combine(AppContext.BaseDirectory,"Assets","brasao-original.jpeg"));b.CacheOption=BitmapCacheOption.OnLoad;b.EndInit();return new Image{Source=new CroppedBitmap(b,new Int32Rect(0,369,720,742)),Height=height,Stretch=Stretch.Uniform,Margin=new Thickness(0,0,0,12)};}
 public static Image MenuCrest(){var b=new BitmapImage();b.BeginInit();b.UriSource=new Uri(Path.Combine(AppContext.BaseDirectory,"Assets","brasao-menu.png"));b.CacheOption=BitmapCacheOption.OnLoad;b.EndInit();return new Image{Source=b,Width=210,Height=210,Stretch=Stretch.Uniform,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(-15,0,-15,12)};}
 public static DataGrid Grid(params (string label,string key)[] cols){var g=new DataGrid();foreach(var (l,k) in cols)g.Columns.Add(new DataGridTextColumn{Header=l,Binding=new System.Windows.Data.Binding("["+k+"]"),Width=new DataGridLength(1,DataGridLengthUnitType.Star)});return g;}
 public static List<Dictionary<string,string>> Table(IEnumerable<JsonObject> rows)=>rows.Select(o=>o.ToDictionary(x=>x.Key,x=>x.Value?.ToString()??"")).ToList();
 public static int SelectedId(DataGrid g)=>g.SelectedItem is Dictionary<string,string> r&&r.TryGetValue("id",out var v)?int.Parse(v):throw new Exception("Selecione um registro.");
 public static void ShowText(Window owner,string title,string text){var w=Dialog(owner,title,830,730);var t=new TextBox{Text=text,IsReadOnly=true,AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,BorderBrush=Ink,BorderThickness=new Thickness(1),Padding=new Thickness(18),Margin=new Thickness(0,0,0,14)};var dock=new DockPanel{Margin=new Thickness(28)};var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center};actions.Children.Add(Button("Copiar texto",()=>Clipboard.SetText(text),true));DockPanel.SetDock(actions,Dock.Bottom);dock.Children.Add(actions);dock.Children.Add(t);w.Content=dock;w.ShowDialog();}
}
public sealed class QuantityPicker:StackPanel {
 public const string OtherOption="Outro – digite a quantidade";
 public ComboBox Choice{get;} public TextBox Other{get;} public event EventHandler? ValueChanged;
 public int Value=>Choice.SelectedItem?.ToString()==OtherOption?(int.TryParse(Other.Text,out var otherValue)&&otherValue>=0?otherValue:0):(int.TryParse(Choice.SelectedItem?.ToString(),out var selectedValue)?selectedValue:0);
 public QuantityPicker(int value=0){Orientation=Orientation.Vertical;Choice=new ComboBox{ItemsSource=Enumerable.Range(0,51).Select(n=>n.ToString()).Append(OtherOption).ToArray(),MinWidth=120};Other=new TextBox{MinHeight=38,Margin=new Thickness(0,3,0,10),ToolTip="Digite a quantidade"};Children.Add(Choice);Children.Add(Other);if(value<=50)Choice.SelectedItem=value.ToString();else{Choice.SelectedItem=OtherOption;Other.Text=value.ToString();}UpdateOther();Choice.SelectionChanged+=(_,_)=>{UpdateOther();ValueChanged?.Invoke(this,EventArgs.Empty);};Other.TextChanged+=(_,_)=>ValueChanged?.Invoke(this,EventArgs.Empty);}
 void UpdateOther(){Other.Visibility=Choice.SelectedItem?.ToString()==OtherOption?Visibility.Visible:Visibility.Collapsed;}
}
public sealed class PolicePicker:Window {
 public List<JsonObject> Result{get;private set;}=[];
 public PolicePicker(Window owner,Database db,string unit,IEnumerable<JsonObject> selected,bool single=false,IEnumerable<JsonObject>? scheduled=null){
  Title="Selecionar policiais";Width=970;Height=690;MinWidth=750;MinHeight=500;WindowStartupLocation=WindowStartupLocation.CenterOwner;UI.KeepDialogOnTop(this,owner);
  var chosen=selected.Select(p=>p.Copy()).ToList();var all=db.Catalog("Policiais");var priority=(scheduled??[]).Select(p=>p.S("rgpm")).ToHashSet();var root=new DockPanel{Margin=new Thickness(24)};
  var header=new StackPanel();header.Children.Add(UI.Text("Selecionar policiais",25,true));header.Children.Add(UI.Text("Pesquise por QRA, RGPM ou quartel. Os selecionados são mantidos durante a busca."));var search=new TextBox();header.Children.Add(search);DockPanel.SetDock(header,Dock.Top);root.Children.Add(header);
  var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};actions.Children.Add(UI.Button("Cancelar",()=>DialogResult=false));actions.Children.Add(UI.Button("Salvar",()=>{Result=Rules.Ordered(chosen).Select(p=>p.Copy()).ToList();DialogResult=true;},true));DockPanel.SetDock(actions,Dock.Bottom);root.Children.Add(actions);
  var columns=new Grid();columns.ColumnDefinitions.Add(new());columns.ColumnDefinitions.Add(new(){Width=new GridLength(145)});columns.ColumnDefinitions.Add(new());var available=new TreeView{BorderThickness=new Thickness(0)};var selectedList=new ListBox{DisplayMemberPath="Label",SelectionMode=SelectionMode.Extended};columns.Children.Add(available);Grid.SetColumn(selectedList,2);columns.Children.Add(selectedList);var buttons=new StackPanel{VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(12)};Grid.SetColumn(buttons,1);columns.Children.Add(buttons);
  void Refresh(){available.Items.Clear();string q=Rules.Norm(search.Text);foreach(var un in Rules.Units.Keys.OrderByDescending(x=>x==unit)){var group=new TreeViewItem{Header=un,IsExpanded=un==unit||q!="",FontWeight=FontWeights.SemiBold};foreach(var p in all.Where(p=>p.S("unit")==un&&!chosen.Any(c=>c.S("rgpm")==p.S("rgpm"))&&Rules.Norm(Rules.Label(p)+p.S("rgpm")+un).Contains(q)).OrderByDescending(p=>priority.Contains(p.S("rgpm"))))group.Items.Add(new TreeViewItem{Header=Rules.Label(p)+"  ·  "+p.S("rgpm")+(priority.Contains(p.S("rgpm"))?"  [escalado]":""),Tag=p,FontWeight=FontWeights.Normal,Padding=new Thickness(5)});if(group.Items.Count>0)available.Items.Add(group);}selectedList.ItemsSource=Rules.Ordered(chosen).Select(p=>new PoliceRow(p)).ToList();}
  void Add(){if(available.SelectedItem is TreeViewItem{Tag:JsonObject p}){if(single)chosen.Clear();chosen.Add(p.Copy());Refresh();}}
  buttons.Children.Add(UI.Button("Adicionar →",Add));buttons.Children.Add(UI.Button("← Remover",()=>{foreach(var row in selectedList.SelectedItems.Cast<PoliceRow>().ToArray())chosen.RemoveAll(p=>p.S("rgpm")==row.Data.S("rgpm"));Refresh();}));available.MouseDoubleClick+=(_,_)=>Add();search.TextChanged+=(_,_)=>Refresh();root.Children.Add(columns);Content=root;Refresh();
 }
 private record PoliceRow(JsonObject Data){public string Label=>Rules.Label(Data)+" · "+Data.S("unit");}
}
