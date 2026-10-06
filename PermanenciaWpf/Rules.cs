using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
namespace PermanenciaWpf;
public static class J {
 public static string S(this JsonNode? n,string k){var value=n?[k];if(value is JsonValue scalar&&scalar.TryGetValue<string>(out var text))return text;return value?.ToString()??"";}
 public static int I(this JsonNode? n,string k)=>int.TryParse(n.S(k),out var v)?v:0;
 public static bool B(this JsonNode? n,string k)=>bool.TryParse(n.S(k),out var b)?b:n.S(k)=="1";
 public static JsonObject Copy(this JsonNode n)=>(JsonObject)n.DeepClone();
 public static JsonArray A(this JsonNode n,string k)=>n[k] as JsonArray??new();
 public static IEnumerable<JsonObject> Objects(this JsonArray n)=>n.OfType<JsonObject>();
}
public static class Rules {
 public static readonly Dictionary<string,string> Units=new(){["SEDE"]="15º BPM",["ALTO GARÇAS"]="1º PELOTÃO DE ALTO GARÇAS",["ALTO TAQUARI"]="2º PELOTÃO DE ALTO TAQUARI",["ARAGUAINHA"]="NPM ARAGUAINHA",["PONTE BRANCA"]="NPM PONTE BRANCA"};
 public static readonly string[] Ranks=["CEL PM","TEN CEL PM","MAJ PM","CAP PM","1º TEN PM","2º TEN PM","SUB TEN PM","1º SGT PM","2º SGT PM","3º SGT PM","CB PM","SD PM"];
 public static readonly string[] Prod=["Barreiras","Ponto Base","Visita Comunitária","Pessoas abordadas","Veículos abordados"];
 public static readonly string[] ZapCounts=["Motocicletas","Automóveis","Pessoas conduzidas","Starts realizadas","Estabelecimentos abordados","Visitas a vítimas de violência doméstica","Visitas a autores do fato (agressores)"];
 public static readonly string[] Texts=["Material Carga","Pernoite","Alterações Diversas"];
 public static readonly string[] Natures=["Maria da Penha","Roubo","Furto","Lesão Corporal","Ameaça","Dano","Tráfico de Drogas","Posse de Drogas","Perturbação do Sossego","Desobediência","OUTROS"];
 public static string Norm(string s)=>new(s.ToUpperInvariant().Normalize(NormalizationForm.FormD).Where(c=>CharUnicodeInfo.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark).ToArray());
 public static IEnumerable<JsonObject> Ordered(IEnumerable<JsonObject> ps)=>ps.OrderBy(p=>Array.IndexOf(Ranks,p.S("rank")) is var i&&i>=0?i:99).ThenBy(p=>Norm(p.S("qra")),StringComparer.Ordinal);
 public static string Label(JsonNode? p)=>p==null?"":p.S("rank")+" "+p.S("qra");
 public static string Names(IEnumerable<JsonObject> ps)=>string.Join(", ",Ordered(ps).Select(Label));
 public static string Date(string d)=>DateTime.TryParseExact(d,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var dt)?dt.ToString("dd/MM/yyyy"):d;
 public static string Nature(JsonNode o)=>o.S("nature")=="OUTROS"?o.S("other"):o.S("nature");
 public static bool Editable(JsonObject u,JsonObject d,DateTime? now=null){if(d.S("status")=="FINALIZADO")return false;if(!DateTime.TryParse(d.S("created"),out var created))return true;return (now??DateTime.Now)<created.AddHours(24);}
 public static List<string> Validate(JsonObject d){
  List<string> e=[];
  foreach(var (k,t) in new[]{("operation","Operação"),("cpu","CPU geral"),("received","Recebimento do Serviço"),("incoming","Passagem do Serviço"),("outgoing","CPU que sai")}) if(d[k]==null||string.IsNullOrWhiteSpace(d[k]!.ToString()))e.Add(t+": preenchimento obrigatório.");
  if(d.S("unit")=="SEDE"&&(d["permanence"]==null||string.IsNullOrWhiteSpace(d["permanence"]!.ToString())))e.Add("Policial da Permanência: preenchimento obrigatório na SEDE.");
  foreach(var t in Texts)if(string.IsNullOrWhiteSpace(d["texts"].S(t)))e.Add(t+": preenchimento obrigatório.");
  foreach(var (k,a,t) in new[]{("notifications","notification_numbers","Notificações"),("seized","seizure_numbers","Veículos apreendidos")}){
   if(!int.TryParse(d.S(k),out int n)||n<0)e.Add(t+": quantidade inteira obrigatória, inclusive zero.");
   else if(d.A(a).Count!=n||d.A(a).Any(v=>string.IsNullOrWhiteSpace(v?.ToString())))e.Add(t+": informe todos os números correspondentes à quantidade.");
  }
  foreach(var t in Prod.Concat(ZapCounts))if(!int.TryParse(d["counts"].S(t),out var n)||n<0)e.Add(t+": quantidade obrigatória, inclusive zero.");
  if(d["counts"].I("Veículos abordados")!=d["counts"].I("Motocicletas")+d["counts"].I("Automóveis"))e.Add("Veículos abordados: o total deve ser a soma de Motocicletas e Automóveis.");
  var vtrs=d.A("vtrs").Objects().ToList();
  if(vtrs.Count==0)e.Add("VTR: inclua pelo menos uma viatura de serviço.");
  foreach(var (vtr,index) in vtrs.Select((value,index)=>(value,index))){
   if(vtr["vehicle"]==null)e.Add("VTR: escolha uma viatura cadastrada.");
   else if(vtr["vehicle"].S("unit")!=d.S("unit"))e.Add("VTR: a viatura deve pertencer ao quartel do Relatório diário.");
   if(index==0&&vtr["driver"]==null)e.Add("Primeira VTR: selecione o motorista.");
   if(vtr.A("police").Count==0)e.Add("VTR "+vtr["vehicle"].S("name")+": informe a guarnição.");
   else if(index==0&&vtr["driver"]!=null&&!vtr.A("police").Objects().Any(p=>p.S("rgpm")==vtr["driver"].S("rgpm")))e.Add("Primeira VTR: o motorista deve fazer parte dos policiais selecionados.");
  }
  if(d["cpu"]!=null&&vtrs.Count>0&&!vtrs.SelectMany(v=>v.A("police").Objects()).Any(p=>p.S("rgpm")==d["cpu"].S("rgpm")))e.Add("CPU geral: inclua-o em uma das guarnições de VTR.");
  foreach(var s in d.A("services").Objects()){
   if(s.A("police").Count==0)e.Add(s.S("name")+": selecione a guarnição.");
   if(s.S("category")=="Serviço em Diária"){if(d.S("unit")!="SEDE")e.Add("Serviços em Diária são exclusivos da SEDE.");if(!s.B("permanent")&&(!DateTime.TryParse(s.S("start_day"),out var start)||!DateTime.TryParse(s.S("end_day"),out var end)||end.Date<start.Date))e.Add(s.S("name")+": informe um período de datas válido.");}
   else if(s.S("category")!="Ordinário"&&new[]{"start","end"}.Any(k=>!TimeOnly.TryParseExact(s.S(k),"HH:mm",out _)))e.Add(s.S("name")+": horário inválido.");
  }
  int ix=0;foreach(var o in d.A("occurrences").Objects()){
   ix++;if(new[]{"number","day","time","nature"}.Any(k=>string.IsNullOrWhiteSpace(o.S(k)))||o.A("police").Count==0)e.Add($"Ocorrência {ix}: número, data, hora, natureza e guarnição obrigatórios.");
   if(!DateTime.TryParseExact(o.S("day"),"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out _)||!TimeOnly.TryParseExact(o.S("time"),"HH:mm",out _))e.Add($"Ocorrência {ix}: data/hora inválida.");
   if(o.S("nature")=="OUTROS"&&string.IsNullOrWhiteSpace(o.S("other")))e.Add($"Ocorrência {ix}: descreva OUTROS.");
  }return e;
 }
 public static string Zap(JsonObject d){
  var ss=d.A("services").Objects().ToList();var vtrs=d.A("vtrs").Objects().ToList();var os=d.A("occurrences").Objects().ToList();var c=d["counts"]!;
  List<string> lines=["ESTADO DE MATO GROSSO","POLÍCIA MILITAR","4° CR",Units[d.S("unit")],$"OPERAÇÃO {d.S("operation")} 🚨🚔⚡","",$"DATA: {Date(d.S("day"))}","","Serviço de 24h",""];
  void Count(string t,int n)=>lines.Add($"✓ {t}: {n:00}");
  Count("Efetivo empregado",ss.Where(s=>!s.B("exclude_zap")).SelectMany(s=>s.A("police").Objects()).Concat(vtrs.SelectMany(v=>v.A("police").Objects())).Select(p=>p.S("rgpm")).Distinct().Count());
  Count("Viaturas empregadas",vtrs.Where(v=>v["vehicle"]!=null).Select(v=>v["vehicle"].S("id")).Distinct().Count());
  foreach(var t in new[]{"Pessoas abordadas","Veículos abordados","Motocicletas","Automóveis"})Count(t,c.I(t));Count("Ocorrências confeccionadas",os.Count);
  foreach(var (typ,title) in new[]{("B.O.","B.Os"),("TCO","TCOs")}){
   var occ=os.Where(o=>o.S("type")==typ).ToList();Count(title,occ.Count);
   foreach(var o in occ){string place=d.S("unit")=="SEDE"?"ALTO ARAGUAIA":d.S("unit");lines.AddRange(["",(typ=="B.O."?"•B.O SROP N°":"•T.C.O SROP N° ")+o.S("number")+" – "+place+(typ=="B.O."?"-MT":""),"DATA: "+Date(o.S("day")),"HORA: "+(typ=="B.O."?o.S("time"):o.S("time").Replace(":","H")),"NATUREZA: "+Nature(o),""]);}
  }
  Count("Notificações aplicadas",d.I("notifications"));Count("Pessoas conduzidas",c.I("Pessoas conduzidas"));Count("Veículos apreendidos",d.I("seized"));Count("Starts realizadas",c.I("Starts realizadas"));Count("Barreiras realizadas",c.I("Barreiras"));
  foreach(var t in ZapCounts.Skip(4))Count(t,c.I(t));return string.Join("\n",lines);
 }
}
