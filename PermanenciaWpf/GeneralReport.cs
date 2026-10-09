using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace PermanenciaWpf;

public sealed record GeneralOccurrence(string Type,string Number,string Place,string Day,string Time,string Nature);
public sealed record ParsedZap(string Unit,DateTime Day,Dictionary<string,int> Counts,List<GeneralOccurrence> Occurrences,string Source);

public sealed class GeneralReportResult {
 public DateTime Day {get;}
 public IReadOnlyList<ParsedZap> Reports {get;}
 public GeneralReportResult(IEnumerable<ParsedZap> reports){Reports=reports.ToList();Day=Reports[0].Day;}
 public JsonObject ToJson()=>new(){["text"]=Text(),["sources"]=new JsonArray(Reports.Select(r=>(JsonNode)new JsonObject{{"unit",r.Unit},{"source",r.Source}}).ToArray())};
 public string Text(){
  var totals=GeneralReport.Fields.ToDictionary(f=>f, f=>Reports.Sum(r=>r.Counts[f]));
  var occurrences=Reports.SelectMany(r=>r.Occurrences.Select(o=>(r.Unit,o))).ToList();
  var lines=new List<string>{"ESTADO DE MATO GROSSO","POLÍCIA MILITAR","4° CR","15º BPM","","RELATÓRIO GERAL",$"DATA: {Day:dd/MM/yyyy}","","Serviço de 24h",""};
  void Count(string name)=>lines.Add($"✓ {name}: {totals[name]:00}");
  foreach(var name in GeneralReport.HeaderFields)Count(name);
  foreach(var type in new[]{"B.O.","T.C.O."})foreach(var (unit,o) in occurrences.Where(x=>x.o.Type==type)){
   lines.Add("");lines.Add($"• {type} — {Rules.Units[unit]}");lines.Add($"N° {o.Number} — {o.Place}");lines.Add("DATA: "+o.Day);lines.Add("HORA: "+o.Time);lines.Add("NATUREZA: "+o.Nature);
  }
  lines.Add("");foreach(var name in GeneralReport.FooterFields)Count(name);
  return string.Join("\n",lines);
 }
}

public static class GeneralReport {
 public static readonly string[] Fields=["Efetivo empregado","Viaturas empregadas","Pessoas abordadas","Veículos abordados","Motocicletas","Automóveis","Ocorrências confeccionadas","B.Os","TCOs","Notificações aplicadas","Pessoas conduzidas","Veículos apreendidos","Starts realizadas","Barreiras realizadas","Estabelecimentos abordados","Visitas a vítimas de violência doméstica","Visitas a autores do fato (agressores)"];
 public static readonly string[] HeaderFields=["Efetivo empregado","Viaturas empregadas","Pessoas abordadas","Veículos abordados","Motocicletas","Automóveis","Ocorrências confeccionadas","B.Os","TCOs"];
 public static readonly string[] FooterFields=Fields.Skip(HeaderFields.Length).ToArray();
 private static readonly Dictionary<string,string[]> Aliases=new(){["B.Os"]=["B.Os","B.O.s","B.O","BOs","BO"],["TCOs"]=["TCOs","T.C.Os","T.C.O","TCO"]};
 private static string Normal(string value)=>Rules.Norm(value).Replace("✓","").Trim();
 private static Match? MatchField(string normalized,string field){
  var aliases=Aliases.TryGetValue(field,out var values)?values.Append(field):new[]{field};
  foreach(var alias in aliases){var match=Regex.Match(normalized,@"(?m)^\s*"+Regex.Escape(Normal(alias))+@"\s*:\s*(?<n>\d+)\b");if(match.Success)return match;}
  return null;
 }
 public static ParsedZap Parse(string expectedUnit,string source,DateTime? expectedDay=null){
  var errors=new List<string>();var normalized=Normal(source);var values=new Dictionary<string,int>();
  foreach(var field in Fields){var match=MatchField(normalized,field);if(match==null)errors.Add(field+": não encontrado.");else values[field]=int.Parse(match.Groups["n"].Value,CultureInfo.InvariantCulture);}
  var dayMatch=Regex.Match(normalized,@"(?m)^\s*DATA\s*:\s*(\d{2}/\d{2}/\d{4})");DateTime day=default;
  if(!dayMatch.Success||!DateTime.TryParseExact(dayMatch.Groups[1].Value,"dd/MM/yyyy",CultureInfo.InvariantCulture,DateTimeStyles.None,out day))errors.Add("DATA: informe uma data válida no cabeçalho.");
  else if(expectedDay.HasValue&&day.Date!=expectedDay.Value.Date)errors.Add("DATA: deve ser "+expectedDay.Value.ToString("dd/MM/yyyy")+".");
  if(!normalized.Contains(Normal(Rules.Units[expectedUnit])))errors.Add("Este texto não corresponde a "+Rules.Units[expectedUnit]+".");
  if(values.Count==Fields.Length){
   if(values["Veículos abordados"]!=values["Motocicletas"]+values["Automóveis"])errors.Add("Veículos abordados: deve ser igual a Motocicletas + Automóveis.");
   if(values["Ocorrências confeccionadas"]!=values["B.Os"]+values["TCOs"])errors.Add("Ocorrências confeccionadas: deve ser igual a B.Os + TCOs.");
  }
  var occurrences=ReadOccurrences(source);
  if(values.TryGetValue("B.Os",out var bos)&&occurrences.Count(o=>o.Type=="B.O.")!=bos)errors.Add("B.Os: o total informado não corresponde aos detalhes encontrados.");
  if(values.TryGetValue("TCOs",out var tcos)&&occurrences.Count(o=>o.Type=="T.C.O.")!=tcos)errors.Add("TCOs: o total informado não corresponde aos detalhes encontrados.");
  if(errors.Count>0)throw new Exception("Não foi possível avançar:\n• "+string.Join("\n• ",errors));
  return new ParsedZap(expectedUnit,day,values,occurrences,source);
 }
 private static List<GeneralOccurrence> ReadOccurrences(string source){
  const string block=@"(?ms)^\s*•\s*(?<type>B\.?\s*O\.?|T\.?\s*C\.?\s*O\.?)\s*(?:SROP\s*)?N[°ºO]?\s*(?<number>[^\r\n–-]+?)\s*[–-]\s*(?<place>[^\r\n]+)\s*\r?\n\s*DATA\s*:\s*(?<day>[^\r\n]+)\s*\r?\n\s*HORA\s*:\s*(?<time>[^\r\n]+)\s*\r?\n\s*NATUREZA\s*:\s*(?<nature>.*?)(?=\r?\n\s*(?:•|✓)|\z)";
  var result=new List<GeneralOccurrence>();foreach(Match match in Regex.Matches(source,block)){var raw=Normal(match.Groups["type"].Value);string type=raw.StartsWith("B")?"B.O.":"T.C.O.";result.Add(new GeneralOccurrence(type,match.Groups["number"].Value.Trim(),match.Groups["place"].Value.Trim(),match.Groups["day"].Value.Trim(),match.Groups["time"].Value.Trim(),Regex.Replace(match.Groups["nature"].Value,"\\s+"," ").Trim()));}return result;
 }
 public static void Open(MainWindow owner)=>new GeneralReportWizard(owner).ShowDialog();
}

public sealed class GeneralReportWizard:Window {
 private readonly MainWindow owner;private readonly Dictionary<string,ParsedZap> reports=new();private readonly TextBox source=new(){AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,MinHeight=390,Margin=new Thickness(0,12,0,12)};private readonly TextBlock title=UI.Text("",22,true);private readonly TextBlock detail=UI.Text("");private readonly Button back;private readonly Button advance;private int step;private bool completed;
 public GeneralReportWizard(MainWindow owner){this.owner=owner;Title="Relatório Geral";Width=900;Height=760;MinWidth=680;MinHeight=600;WindowStartupLocation=WindowStartupLocation.CenterOwner;UI.KeepDialogOnTop(this,owner);Closing+=(_,e)=>{if(!completed&&!UI.Confirm("Esses dados são temporários, deseja fechar a janela?"))e.Cancel=true;};var root=new Grid{Margin=new Thickness(28)};root.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});root.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});root.RowDefinitions.Add(new RowDefinition());root.RowDefinitions.Add(new RowDefinition{Height=GridLength.Auto});root.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(15,GridUnitType.Star)});root.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(70,GridUnitType.Star)});root.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(15,GridUnitType.Star)});var header=new StackPanel();header.Children.Add(title);header.Children.Add(detail);header.Children.Add(UI.Text("Cole o Relatório WhatsApp completo do quartel abaixo."));Grid.SetColumn(header,1);root.Children.Add(header);Grid.SetRow(source,2);Grid.SetColumn(source,1);root.Children.Add(source);var actions=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Center,Margin=new Thickness(0,4,0,0)};back=UI.Button("Voltar",Back);advance=UI.Button("Avaliar e avançar",Advance,true);actions.Children.Add(back);actions.Children.Add(advance);Grid.SetRow(actions,3);Grid.SetColumn(actions,1);root.Children.Add(actions);Content=root;Refresh();}
 private string Unit=>Rules.Units.Keys.ElementAt(step);
 private void Refresh(){title.Text=$"{step+1} - {Rules.Units.Count} - INFORME OS DADOS DO {Rules.Units[Unit]}";detail.Text=reports.Count==0?"Insira os cinco relatórios para gerar o consolidado.":$"{reports.Count} de {Rules.Units.Count} quartéis validados.";source.Text=reports.TryGetValue(Unit,out var saved)?saved.Source:"";back.IsEnabled=step>0;advance.Content=step==Rules.Units.Count-1?"Gerar relatório geral":"Avaliar e avançar";}
 private void Back(){if(step==0)return;step--;Refresh();}
 private void Advance(){var day=reports.Values.FirstOrDefault()?.Day;var parsed=GeneralReport.Parse(Unit,source.Text,day);reports[Unit]=parsed;if(step<Rules.Units.Count-1){step++;Refresh();return;}var result=new GeneralReportResult(Rules.Units.Keys.Select(unit=>reports[unit]));owner.Db!.SaveGeneral(owner.User!,result);completed=true;Close();UI.ShowText(owner,"Relatório WhatsApp Geral",result.Text());}
}
